#include "blasSupp.h"


#pragma region sparse vector to/from COO matrix
struct intModulus_functor
{
	const MKL_INT mod;
	intModulus_functor(const MKL_INT m) : mod(m) {}

	__host__ __device__ MKL_INT operator()(const MKL_INT x) const
	{
		return x % mod;
	}
};
struct intDivide_functor
{
	const MKL_INT div;
	intDivide_functor(const MKL_INT d) : div(d) {}

	__host__ __device__ MKL_INT operator()(const MKL_INT x) const
	{
		return x / div;
	}
};
struct intFMA_functor
{
	const MKL_INT mul;
	intFMA_functor(const MKL_INT m) : mul(m) {}

	__host__ __device__ MKL_INT operator()(const MKL_INT x, const MKL_INT y) const
	{
		return x + y * mul;
	}
};

DLLEXP
void spVecIdxToCooIdxs(const MKL_INT* index, MKL_INT* rowIdx, MKL_INT* colIdx, const size_t N, const MKL_INT ld)
{
	thrust::transform(THRUST_PAR, index, index + N, rowIdx, intModulus_functor(ld));
	thrust::transform(THRUST_PAR, index, index + N, colIdx, intDivide_functor(ld));
}

DLLEXP
void cooIdxsToSpVecIdx(MKL_INT* index, const MKL_INT* rowIdx, const MKL_INT* colIdx, const size_t N, const MKL_INT ld)
{
	thrust::transform(THRUST_PAR, rowIdx, rowIdx + N, colIdx, index, intFMA_functor(ld));
}
#pragma endregion


#pragma region CSR matrix get non-empty row indexes
struct intLessThanZero_functor
{
	__host__ __device__ bool operator()(const MKL_INT x) const
	{
		return x < 0;
	}
};
struct intCSRGetNER_functor
{
	__host__ __device__ MKL_INT operator()(const thrust::tuple<MKL_INT, MKL_INT, MKL_INT> t) const
	{
		return t.get<1>() == t.get<2>() ? -1 : t.get<0>();
	}
};

DLLEXP
size_t CSRGetNerBuffer(const MKL_INT rows)
{
	return sizeof(MKL_INT) * ((size_t)rows - 1);
}

DLLEXP
size_t CSRGetNerNnz(const MKL_INT* csrRowPtr, const MKL_INT rows, MKL_INT* buffer)
{
	const MKL_INT N = rows - 1;

	// get indexes
	auto zip = thrust::make_zip_iterator(thrust::make_tuple(csrRowPtr, csrRowPtr + 1, thrust::make_counting_iterator(0)));
	thrust::transform(THRUST_PAR, zip, zip + N, buffer, intCSRGetNER_functor());

	// remove negative indexes
	MKL_INT* tempEnd = thrust::remove_if(THRUST_PAR, buffer, buffer + N, intLessThanZero_functor());
	size_t nnz = tempEnd - buffer;
	return nnz;
}

DLLEXP ERROR_RETURN CSRGetNerCal(const MKL_INT* buffer, const size_t nnz, MKL_INT* nerOut)
{
#ifdef CPU
	memcpy(nerOut, buffer, sizeof(MKL_INT) * nnz);
#else
	cudaError err = cudaMemcpy(nerOut, buffer, sizeof(MKL_INT) * nnz, cudaMemcpyDeviceToDevice);
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
	const void* Av, const size_t ldA, const size_t rowsA, const size_t colsA,
	const void* Bv, const size_t ldB, const size_t rowsB, const size_t colsB,
	void* destv, const size_t ldD, const void* alphav, const void* betav)
{
	// cast
	const T* A = (const T*)Av;
	const T* B = (const T*)Bv;
	T* D = (T*)destv;
	const T alpha = *((const T*)alphav);
	const T beta = *((const T*)betav);

	const unsigned MKL_INT rowsD = rowsA * rowsB;
	const unsigned MKL_INT colsD = colsA * colsB;

#define KRON_CODE(bool1, bool2, bool3) thrust::for_each_n(THRUST_PAR, count_iter, rowsD * colsD, kronecker_functor<T, bool1, bool2, bool3>(alpha, beta, ldA, ldB, colsB, ldD, rowsD, A, B, D))

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
	const void* A, const size_t ldA, const size_t rowsA, const size_t colsA,
	const void* B, const size_t ldB, const size_t rowsB, const size_t colsB,
	void* dest, const size_t ldD, const void* alpha, const void* beta)
{
	AUTO_ALLTYPE_FUNC(matricesKronecker, type, void, A, ldA, rowsA, colsA, B, ldB, rowsB, colsB, dest, ldD, alpha, beta);
}
#pragma endregion


#pragma region make matrix Hermitian by copying its upper part to/from its lower part
template <typename T, bool upper>
struct makeHerm_functor2
{
	const size_t ld;
	T* A;

	// used for compute the actual row and column position
	const double onePlus2NHalf, onePlus2NSquare;
	const size_t TwoNMinusOne;
	// Ignore Spelling: lfloor
	//tex:Since for number of rows $n$, column index $c$ and iteration index $i$: $$\sum_{i=0}^c (n - i) = \frac12 (1 + c)(2n - c)$$
	//We have $$c = \left\lfloor \frac{1}{2} \left( 2n+1 - \sqrt{(2 n+1)^2-8 i} \right) \right\rfloor = \left\lfloor \frac{1}{2} (1+2 n) \left(1-\sqrt{1-\frac{8 i}{(1+2 n)^2}}\right)\right\rfloor$$
	//The latter one is better for float point computation and will be correct if $n < $ (1 << 27 = 134,217,728) (half the precision of double).$\\$
	//I use the float point instead of integer square root since "the fastest ISQRT() algorithm by far is to go through the FPU."$\\$
	//The row index is then:
	//$$r = \frac12(c^2-2 c n+c+2 i-2) = i - 1 - \frac12 c (2n - 1 - c)$$

	makeHerm_functor2(const size_t ld, T* A) :
		ld(ld), A(A),
		TwoNMinusOne(2 * ld - 1),
		onePlus2NHalf(0.5 * (2 * ld + 1)),
		onePlus2NSquare((2 * ld + 1) * (double)(2 * ld + 1))
	{}

	__host__ __device__ void operator()(const size_t ind) const
	{
		// get offset
		const size_t col = (size_t)(onePlus2NHalf * (1.0 - std::sqrt(1.0 - 8 * ind / onePlus2NSquare)));
		const size_t row = ind - 1 - (col * (TwoNMinusOne - col)) / 2;
		const size_t offsetLower = row + col * ld, offsetUpper = col + row * ld;
		// copy
		if constexpr (std::is_scalar<T>::value)
		{
			if constexpr (upper)
				A[offsetLower] = A[offsetUpper];
			else
				A[offsetUpper] = A[offsetLower];
		}
		else
		{
			if (row == col)
			{
				A[offsetLower] = T(A[offsetLower].real());
			}
			else
			{
				if constexpr (upper)
					A[offsetLower] = std::conj(A[offsetUpper]);
				else
					A[offsetUpper] = std::conj(A[offsetLower]);
			}
		}
	}
};

template <typename T, bool upper, bool makeHerm>
struct makeHerm_functor
{
	const size_t ld, rows;
	T* A;

	makeHerm_functor(const size_t ld, const size_t rows, T* A) :
		ld(ld), rows(rows), A(A)
	{}

	__host__ __device__ void operator()(const size_t ind) const
	{
		// get offset
		const lldiv_t div = std::lldiv(ind, rows);
		const size_t row = div.rem, col = div.quot;
		const size_t offsetLower = row + col * ld, offsetUpper = col + row * ld;
		// copy
		if constexpr (upper)
		{
			if (row > col)
				return;
		}
		else
		{
			if (row < col)
				return;
		}
		if constexpr (makeHerm)
		{
			if (row == col)
			{
				if constexpr (std::is_scalar<T>::value)
					A[offsetLower] = T(A[offsetLower].real());
				return;
			}
			if constexpr (upper)
				A[offsetLower] = std::conj(A[offsetUpper]);
			else
				A[offsetUpper] = std::conj(A[offsetLower]);
		}
		else
		{
			if constexpr (upper)
				A[offsetLower] = A[offsetUpper];
			else
				A[offsetUpper] = A[offsetLower];
		}
	}
};

template <typename T, bool clearLower>
struct clearPart_functor
{
	const size_t ld, rows;
	T* A;

	clearPart_functor(const size_t ld, const size_t rows, T* A) :
		ld(ld), rows(rows), A(A)
	{}

	__host__ __device__ void operator()(const size_t ind) const
	{
		// get offset
		const lldiv_t div = std::lldiv(ind, rows);
		const size_t row = div.rem, col = div.quot;
		// set
		if constexpr (clearLower)
		{
			if (row >= col)
				return;
		}
		else
		{
			if (row <= col)
				return;
		}
		A[row + col * ld] = T();
	}
};

template<typename T>
void matrixMakeHermitian2(void* Av, const size_t ld, const size_t rows, const bool upperStored)
{
	T* A = (T*)Av;
#define MAKE_HERM_CODE(bool1) thrust::for_each_n(THRUST_PAR, count_iter, (rows * (rows + 1)) / 2, makeHerm_functor2<T, bool1>(ld, A))
	if (upperStored)
		MAKE_HERM_CODE(true);
	else
		MAKE_HERM_CODE(false);
}

template<typename T>
void matrixMakeHermitian(void* Av, const size_t ld, const size_t rows, const bool upperStored, const bool hermA)
{
	T* A = (T*)Av;
#define MAKE_HERM_CODE(bool1, bool2) thrust::for_each_n(THRUST_PAR, count_iter, rows * rows, makeHerm_functor<T, bool1, bool2>(ld, rows, A))
	if (upperStored && hermA)
		MAKE_HERM_CODE(true, true);
	else if (upperStored && !hermA)
		MAKE_HERM_CODE(true, false);
	else if (!upperStored && hermA)
		MAKE_HERM_CODE(false, true);
	else
		MAKE_HERM_CODE(false, false);
}

template<typename T>
void matrixClearTriangular(void* Av, const size_t ld, const size_t rows, const bool clearLower)
{
	T* A = (T*)Av;

#define MAKE_HERM_CODE(bool1) thrust::for_each_n(THRUST_PAR, count_iter, rows * rows, clearPart_functor<T, bool1>(ld, rows, A))
	if (clearLower)
		MAKE_HERM_CODE(true);
	else
		MAKE_HERM_CODE(false);
}

DLLEXP
void matMakeHerm(const Datatype::DataType type, void* A, const size_t ld, const size_t rows, const bool upperStored, const bool hermA)
{
	AUTO_SIGNED_TYPE_FUNC(matrixMakeHermitian, type, void, A, ld, rows, upperStored, hermA);
}

DLLEXP
void matTriClear(const Datatype::DataType type, void* A, const size_t ld, const size_t rows, const bool clearLower)
{
	AUTO_SIGNED_TYPE_FUNC(matrixClearTriangular, type, void, A, ld, rows, clearLower);
}
#pragma endregion


#pragma region sparse vectors outer product to COOC matrix
template <typename T, bool conj>
struct sparseVectorsOuter_functor
{
	const T* valA; const MKL_INT* indA; const size_t nnzA;
	const T* valB; const MKL_INT* indB;
	T* valC; MKL_INT* rowC; MKL_INT* colC;

	sparseVectorsOuter_functor(
		const T* valA, const MKL_INT* indA, const size_t nnzA,
		const T* valB, const MKL_INT* indB,
		T* valC, MKL_INT* rowC, MKL_INT* colC) :
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
int sparseVectorsOuterCheck()
{
	return 0;
}

DLLEXP int spVecOuterCheck(const Datatype::DataType type)
{
	AUTO_ALLTYPE_FUNC(sparseVectorsOuterCheck, type, int);
}

template<typename T>
int sparseVectorsOuter(
	const void* valAv, const MKL_INT* indA, const size_t nnzA,
	const void* valBv, const MKL_INT* indB, const size_t nnzB,
	void* valCv, MKL_INT* rowC, MKL_INT* colC, const bool conj)
{
	const T* valA = (const T*)valAv;
	const T* valB = (const T*)valBv;
	T* valC = (T*)valCv;

#define SPARSE_VECTOR_OUTER_CODE(bool1) thrust::for_each_n(THRUST_PAR, count_iter, nnzA * nnzB, sparseVectorsOuter_functor<T, bool1>(valA, indA, nnzA, valB, indB, valC, rowC, colC))

	if (conj)
		SPARSE_VECTOR_OUTER_CODE(true);
	else
		SPARSE_VECTOR_OUTER_CODE(false);

	return 0;
}

DLLEXP int spVecOuter(const Datatype::DataType type,
	const void* valA, const MKL_INT* indA, const size_t nnzA,
	const void* valB, const MKL_INT* indB, const size_t nnzB,
	void* valC, MKL_INT* rowC, MKL_INT* colC, const bool conj)
{
	AUTO_ALLTYPE_FUNC(sparseVectorsOuter, type, int, valA, indA, nnzA, valB, indB, nnzB, valC, rowC, colC, conj);
}
#pragma endregion


#pragma region sparse COO format matrices Kronecker
template <typename T>
struct CooMatricesKronecker_functor
{
	const T* valA; const MKL_INT* rowA; const MKL_INT* colA;
	const T* valB; const MKL_INT* rowB; const MKL_INT* colB; const size_t nnzB; const size_t rowsB; const size_t colsB;
	T* valC; MKL_INT* rowC; MKL_INT* colC;

	CooMatricesKronecker_functor(
		const T* valA, const MKL_INT* rowA, const MKL_INT* colA,
		const T* valB, const MKL_INT* rowB, const MKL_INT* colB, const size_t nnzB, const size_t rowsB, const size_t colsB,
		T* valC, MKL_INT* rowC, MKL_INT* colC) :
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
	__host__ __device__ bool operator()(const thrust::tuple<MKL_INT, MKL_INT> lhs, const thrust::tuple<MKL_INT, MKL_INT> rhs) const
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
void CooMatricesKronecker(
	const void* valAv, const MKL_INT* rowA, const MKL_INT* colA, const size_t nnzA,
	const void* valBv, const MKL_INT* rowB, const MKL_INT* colB, const size_t nnzB, const size_t rowsB, const size_t colsB,
	void* valCv, MKL_INT* rowC, MKL_INT* colC)
{
	const T* valA = (const T*)valAv;
	const T* valB = (const T*)valBv;
	T* valC = (T*)valCv;
	const size_t nnzC = nnzA * nnzB;

	// outer
	thrust::for_each_n(THRUST_PAR, count_iter, nnzC, CooMatricesKronecker_functor<T>(valA, rowA, colA, valB, rowB, colB, nnzB, rowsB, colsB, valC, rowC, colC));
	// sort column wise
	auto rowColC = thrust::make_zip_iterator(thrust::make_tuple(rowC, colC));
	thrust::sort_by_key(THRUST_PAR, rowColC, rowColC + nnzC, valC, cooMatrixSortByColumn_functor());
}

DLLEXP void COOMatKron(const Datatype::DataType type,
	const void* valA, const MKL_INT* rowA, const MKL_INT* colA, const size_t nnzA,
	const void* valB, const MKL_INT* rowB, const MKL_INT* colB, const size_t nnzB, const size_t rowsB, const size_t colsB,
	void* valC, MKL_INT* rowC, MKL_INT* colC)
{
	AUTO_ALLTYPE_FUNC(CooMatricesKronecker, type, void, valA, rowA, colA, nnzA, valB, rowB, colB, nnzB, rowsB, colsB, valC, rowC, colC);
}
#pragma endregion