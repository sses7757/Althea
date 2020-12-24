#include "macro.h"


#pragma region sparse vector to/from COO matrix
struct intModulus_functor
{
	const int mod;
	intModulus_functor(const int m) : mod(m) {}

	__host__ __device__ int operator()(const int x) const
	{
		return x % mod;
	}
};
struct intDivide_functor
{
	const int div;
	intDivide_functor(const int d) : div(d) {}

	__host__ __device__ int operator()(const int x) const
	{
		return x / div;
	}
};
struct intFMA_functor
{
	const int mul;
	intFMA_functor(const int m) : mul(m) {}

	__host__ __device__ int operator()(const int x, const int y) const
	{
		return x + y * mul;
	}
};

DLLEXP
void spVecIndToCooInds(const int* index, int* rowIdx, int* colIdx, const size_t N, const int ld)
{
	thrust::transform(THRUST_PAR, index, index + N, rowIdx, intModulus_functor(ld));
	thrust::transform(THRUST_PAR, index, index + N, colIdx, intDivide_functor(ld));
}

DLLEXP
void CooIndxToSpVecInd(int* index, const int* rowIdx, const int* colIdx, const size_t N, const int ld)
{
	thrust::transform(THRUST_PAR, rowIdx, rowIdx + N, colIdx, index, intFMA_functor(ld));
}
#pragma endregion


#pragma region CSR matrix get non-empty row indexes
struct intLessThanZero_functor
{
	__host__ __device__ bool operator()(const int x) const
	{
		return x < 0;
	}
};
struct intCSRGetNER_functor
{
	__host__ __device__ int operator()(const thrust::tuple<int, int, int> t) const
	{
		return t.get<1>() == t.get<2>() ? -1 : t.get<0>();
	}
};

DLLEXP
size_t CSRGetNerBuffer(const int rows)
{
	return sizeof(int) * ((size_t)rows - 1);
}

DLLEXP
size_t CSRGetNerNnz(const int* csrRowPtr, const int rows, int* buffer)
{
	const int N = rows - 1;

	// get indexes
	auto zip = thrust::make_zip_iterator(thrust::make_tuple(csrRowPtr, csrRowPtr + 1, thrust::make_counting_iterator(0)));
	thrust::transform(THRUST_PAR, zip, zip + N, buffer, intCSRGetNER_functor());

	// remove negative indexes
	int* tempEnd = thrust::remove_if(THRUST_PAR, buffer, buffer + N, intLessThanZero_functor());
	size_t nnz = tempEnd - buffer;
	return nnz;
}

DLLEXP ERROR_RETURN CSRGetNerCal(const int* buffer, size_t nnz, int* nerOut)
{
#ifdef CPU
	memcpy(nerOut, buffer, sizeof(int) * nnz);
#else
	cudaError err = cudaMemcpy(nerOut, buffer, sizeof(int) * nnz, cudaMemcpyDeviceToDevice);
	return err;
#endif // CPU
}
#pragma endregion

const auto count_iter = thrust::counting_iterator<size_t>(0);

#pragma region dense matrices Kronecker
// Ignore spelling: mathbb
//tex: The number of cache miss for $A\in \mathbb{R}^{N\times N} \otimes B\in \mathbb{R}^{N\times N} = C\in \mathbb{R}^{N^2\times N^2}$ is: $\\$
// 1. $O(N^2+N)$ for contiguously access $C$ $\\$
// 2. $O(N^2)$ for contiguously access $B$ $\\$ 

// The Kronecker product of two matrices can be achieved by
//	1. outer product of two matrices' column vectors
//	2. reshape the matrix to a proper rank-4 tensor
//	3. permute the tensor [3,1,4,2] (may be)
//	4. reshape the tensor to the output matrix

template <typename T, bool largerLeadDim, bool hasAlpha, bool hasBeta>
struct kronecker_functor
{
	const T alpha, beta;
	const size_t ldA, ldB, colsB, ldD, rowsD;
	const T* A;
	const T* B;
	T* D;

	kronecker_functor(const T alpha, const T beta, const size_t ldA, const size_t ldB, const size_t colsB, const size_t ldD, const size_t rowsD, const T* A, const T* B, T* D) :
		alpha(alpha), beta(beta), ldA(ldA), ldB(ldB), colsB(colsB), ldD(ldD), rowsD(rowsD), A(A), B(B), D(D) {}

	__host__ __device__ void operator()(const size_t indD) const
	{
		// get offsets
		const size_t rowD = indD / rowsD, colD = indD % rowsD;
		const size_t offsetA = (rowD / ldB) + (colD / colsB) * ldA,
			offsetB = (rowD % ldB) + (colD % colsB) * ldB;
		size_t offsetD;
		if constexpr (largerLeadDim)
		{
			offsetD = ldD * rowD + colD;
		}
		else
		{
			offsetD = indD;
		}
		// multiply
		if constexpr (hasAlpha && hasBeta)
			D[offsetD] = alpha * A[offsetA] * B[offsetB] + beta * D[offsetD];
		if constexpr (hasAlpha && !hasBeta)
			D[offsetD] = alpha * A[offsetA] * B[offsetB];
		if constexpr (!hasAlpha && hasBeta)
			D[offsetD] = A[offsetA] * B[offsetB] + beta * D[offsetD];
		if constexpr (!hasAlpha && !hasBeta)
			D[offsetD] = A[offsetA] * B[offsetB];
	}
};

template<typename T>
inline void matricesKronecker(
	const void* Av, const unsigned int ldA, const unsigned int rowsA, const unsigned int colsA,
	const void* Bv, const unsigned int ldB, const unsigned int rowsB, const unsigned int colsB,
	void* destv, const unsigned int ldD, const void* alphav, const void* betav)
{
	// cast
	const T* A = (const T*)Av;
	const T* B = (const T*)Bv;
	T* D = (T*)destv;
	const T alpha = *((const T*)alphav);
	const T beta = *((const T*)betav);

	const unsigned int rowsD = rowsA * rowsB;
	const unsigned int colsD = colsA * colsB;

#define KRON_CODE(bool1, bool2, bool3) thrust::for_each_n(THRUST_PAR, count_iter, (size_t)rowsD * colsD, kronecker_functor<T, bool1, bool2, bool3>(alpha, beta, ldA, ldB, colsB, ldD, rowsD, A, B, D))

	if (rowsD == ldD)
	{
		if (alpha == T(1) && beta == T(0))
			KRON_CODE(false, false, false);
		else if (alpha == T(1))
			KRON_CODE(false, false, true);
		else if (beta == T(0))
			KRON_CODE(false, true, false);
		else
			KRON_CODE(false, true, true);
	}
	else
	{
		if (alpha == T(1) && beta == T(0))
			KRON_CODE(true, false, false);
		else if (alpha == T(1))
			KRON_CODE(true, false, true);
		else if (beta == T(0))
			KRON_CODE(true, true, false);
		else
			KRON_CODE(true, true, true);
	}
}

DLLEXP
void matKron(const Datatype::DataType type,
	const void* A, const unsigned int ldA, const unsigned int rowsA, const unsigned int colsA,
	const void* B, const unsigned int ldB, const unsigned int rowsB, const unsigned int colsB,
	void* dest, const unsigned int ldD, const void* alpha, const void* beta)
{
	AUTO_ALLTYPE_FUNC(matricesKronecker, type, void, A, ldA, rowsA, colsA, B, ldB, rowsB, colsB, dest, ldD, alpha, beta);
}
#pragma endregion


#pragma region make matrix Hermitian by copying its upper part to its lower part
template <typename T, bool largerLeadDim>
struct makeHerm_functor
{
	const size_t ld, rows;
	T* A;

	// used for compute the actual row and column position
	double onePlus2NHalf, onePlus2NSquare;
	size_t TwoNMinusOne;
	// Ignore Spelling: lfloor
	//tex:Since for number of rows $n$, column index $c$ and iteration index $i$: $$\sum_{i=0}^c (n - i) = \frac12 (1 + c)(2n - c)$$
	//We have $$c = \left\lfloor \frac{1}{2} \left( 2n+1 - \sqrt{(2 n+1)^2-8 i} \right) \right\rfloor = \left\lfloor \frac{1}{2} (1+2 n) \left(1-\sqrt{1-\frac{8 i}{(1+2 n)^2}}\right)\right\rfloor$$
	//The latter one is better for float point computation and will be correct if $n < $ (1 << 27 = 134,217,728) (half the precision of double).$\\$
	//I use the float point instead of integer square root since "the fastest ISQRT() algorithm by far is to go through the FPU."$\\$
	//The row index is then:
	//$$r = \frac12(c^2-2 c n+c+2 i-2) = i - 1 - \frac12 c (2n - 1 - c)$$

	makeHerm_functor(const size_t ld, const size_t rows, T* A) :
		ld(ld), rows(rows), A(A)
	{
		const size_t onePlus2N = 2 * ld + 1;
		TwoNMinusOne = onePlus2N - 2;
		onePlus2NHalf = onePlus2N * 0.5;
		onePlus2NSquare = onePlus2N * onePlus2N;
	}

	__host__ __device__ void operator()(const size_t ind) const
	{
		// get offset
		const size_t col = (size_t)(onePlus2NHalf * (1.0 - std::sqrt(1.0 - 8 * ind / onePlus2NSquare)));
		const size_t row = ind - 1 - (col * (TwoNMinusOne - col)) / 2;
		const size_t offset = row + col * ld, offsetUpper = col + row * ld;
		// copy
		if constexpr (std::is_scalar<T>::value)
		{
			A[offset] = A[offsetUpper];
			return;
		}
		else
		{
			if (row == col)
			{
				A[offset] = T(A[offset].real());
			}
			else
			{
				A[offset] = std::conj(A[offsetUpper]);
			}
		}
	}
};

template<typename T>
void matrixMakeHermitian(void* Av, const unsigned int ld, const unsigned int rows)
{
	T* A = (T*)Av;
	const size_t Nrows = (size_t)rows;
#define MAKE_HERM_CODE(bool1) thrust::for_each_n(THRUST_PAR, count_iter, (Nrows * (Nrows + 1)) / 2, makeHerm_functor<T, bool1>(ld, Nrows, A))
	if (ld == rows)
		MAKE_HERM_CODE(false);
	else
		MAKE_HERM_CODE(true);
}


DLLEXP
void matMakeHerm(const Datatype::DataType type, void* A, const unsigned int ld, const unsigned int rows)
{
	AUTO_SIGNED_TYPE_FUNC(matrixMakeHermitian, type, void, A, ld, rows);
}
#pragma endregion


#pragma region sparse vectors outer product to COOC matrix
template <typename T, bool conj>
struct sparseVectorsOuter_functor
{
	const T* valA; const int* indA; const size_t nnzA;
	const T* valB; const int* indB;
	T* valC; int* rowC; int* colC;

	sparseVectorsOuter_functor(
		const T* valA, const int* indA, const size_t nnzA,
		const T* valB, const int* indB,
		T* valC, int* rowC, int* colC) :
		valA(valA), indA(indA), nnzA(nnzA),
		valB(valB), indB(indB),
		valC(valC), rowC(rowC), colC(colC)
	{}

	__host__ __device__ void operator()(const size_t idx) const
	{
		const size_t n = idx % nnzA, m = idx / nnzA;
		rowC[idx] = indA[n];
		colC[idx] = indB[m];
		if constexpr (conj)
			valC[idx] = valA[n] * std::conj(valB[m]);
		else
			valC[idx] = valA[n] * valB[m];
	}
};

template<typename T>
void sparseVectorsOuter(
	const void* valAv, const int* indA, const size_t nnzA,
	const void* valBv, const int* indB, const size_t nnzB,
	void* valCv, int* rowC, int* colC, const bool conj)
{
	const T* valA = (const T*)valAv;
	const T* valB = (const T*)valBv;
	T* valC = (T*)valCv;

#define SPARSE_VECTOR_OUTER_CODE(bool1) thrust::for_each_n(THRUST_PAR, count_iter, nnzA * nnzB, sparseVectorsOuter_functor<T, bool1>(valA, indA, nnzA, valB, indB, valC, rowC, colC))

	if (conj)
		SPARSE_VECTOR_OUTER_CODE(true);
	else
		SPARSE_VECTOR_OUTER_CODE(false);
}

DLLEXP void spVecOuter(const Datatype::DataType type,
	const void* valA, const int* indA, const size_t nnzA,
	const void* valB, const int* indB, const size_t nnzB,
	void* valC, int* rowC, int* colC, const bool conj)
{
	AUTO_ALLTYPE_FUNC(sparseVectorsOuter, type, void, valA, indA, nnzA, valB, indB, nnzB, valC, rowC, colC, conj);
}
#pragma endregion


#pragma region sparse COO format matrices Kronecker
template <typename T>
struct cooMatricesKronecker_functor
{
	const T* valA; const int* rowA; const int* colA;
	const T* valB; const int* rowB; const int* colB; const size_t nnzB; const size_t rowsB; const size_t colsB;
	T* valC; int* rowC; int* colC;

	cooMatricesKronecker_functor(
		const T* valA, const int* rowA, const int* colA,
		const T* valB, const int* rowB, const int* colB, const size_t nnzB, const size_t rowsB, const size_t colsB,
		T* valC, int* rowC, int* colC) :
		valA(valA), rowA(rowA), colA(colA),
		valB(valA), rowB(rowA), colB(colB), nnzB(nnzB), rowsB(rowsB), colsB(colsB),
		valC(valC), rowC(rowC), colC(colC)
	{}

	__host__ __device__ void operator()(const size_t idx) const
	{
		const size_t n = idx / nnzB, m = idx % nnzB;
		rowC[idx] = rowA[n] * rowsB + rowB[m];
		colC[idx] = colA[n] * colsB + colB[m];
		valC[idx] = valA[n] * valB[m];
	}
};

struct cooMatrixSortByColumn_functor
{
	__host__ __device__ bool operator()(const thrust::tuple<int, int> lhs, const thrust::tuple<int, int> rhs) const
	{
		if (lhs.get<1>() < rhs.get<1>())
			return true;
		else if (lhs.get<1>() == rhs.get<1>())
			return lhs.get<0>() < rhs.get<0>();
		else
			return false;
	}
};

template<typename T>
void cooMatricesKronecker(
	const void* valAv, const int* rowA, const int* colA, const size_t nnzA,
	const void* valBv, const int* rowB, const int* colB, const size_t nnzB, const size_t rowsB, const size_t colsB,
	void* valCv, int* rowC, int* colC)
{
	const T* valA = (const T*)valAv;
	const T* valB = (const T*)valBv;
	T* valC = (T*)valCv;
	const size_t nnzC = nnzA * nnzB;

	// outer
	thrust::for_each_n(THRUST_PAR, count_iter, nnzC, cooMatricesKronecker_functor<T>(valA, rowA, colA, valB, rowB, colB, nnzB, rowsB, colsB, valC, rowC, colC));
	// sort column wise
	auto rowColC = thrust::make_zip_iterator(thrust::make_tuple(rowC, colC));
	thrust::sort_by_key(THRUST_PAR, rowColC, rowColC + nnzC, valC, cooMatrixSortByColumn_functor());
}

DLLEXP void cooMatKron(const Datatype::DataType type,
	const void* valA, const int* rowA, const int* colA, const size_t nnzA,
	const void* valB, const int* rowB, const int* colB, const size_t nnzB, const size_t rowsB, const size_t colsB,
	void* valC, int* rowC, int* colC)
{
	AUTO_ALLTYPE_FUNC(cooMatricesKronecker, type, void, valA, rowA, colA, nnzA, valB, rowB, colB, nnzB, rowsB, colsB, valC, rowC, colC);
}
#pragma endregion