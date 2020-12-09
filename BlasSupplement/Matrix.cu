#include "macro.h"


#pragma region sparse vector's index array to COO format sparse matrix's index arrays and back
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


// TODO: test speed of outer in https://stackoverflow.com/questions/41794068/call-functor-for-all-combinations-in-cuda-thrust compared with CUDA


template <typename Func>
inline static cudaError calc2DKernelPara(size_t nx, size_t ny, Func ker, dim3& dimBlock, dim3& dimGrid)
{
	int N = (int)(nx + ny);
	if (N < 0)
		N = INT_MAX;

	int blockSize, minGridSize;
	cudaError err = cudaOccupancyMaxPotentialBlockSize(&minGridSize, &blockSize, ker, 0, N);
	if (err != 0)
		return err;
	dimBlock = dim3((int)sqrt(blockSize), (int)sqrt(blockSize));
	dimGrid.x = (nx + dimBlock.x - 1) / dimBlock.x;
	dimGrid.y = (ny + dimBlock.y - 1) / dimBlock.y;

	err = cudaDeviceSynchronize();
	if (err != 0)
		return err;
}


#pragma region matrix Kronecker (GPU version)
namespace GPUVersion
{
	// TODO: the Kronecker product of two matrices can be achieved by
	//	1. outer product of two matrices' column vectors
	//	2. reshape the matrix to a proper rank-4 tensor
	//	3. permute the tensor [3,1,4,2] (may be)
	//	4. reshape the tensor to the output matrix
	// Test this

	// TODO: can improve by avoid bank conflict, etc.

	template <typename T, bool hasA, bool hasB>
	__global__ void kroneckerKernel(const T* A, const int ldA, const int rowsA, const int colsA,
		const T* B, const int ldB, const int rowsB, const int colsB,
		T* dest, const int ldD, const int rowsD, const int colsD,
		const T alpha, const T beta)
	{
		unsigned int n = blockIdx.x * blockDim.x + threadIdx.x;
		unsigned int m = blockIdx.y * blockDim.y + threadIdx.y;
		if (n < rowsD && m < colsD)
		{
			unsigned int xb = n % ldB, yb = m % colsB;
			unsigned int xa = n / ldB, ya = m / colsB;
			if (xa < rowsA && xb < rowsB && ya < colsA && yb < colsB)
			{
				unsigned int destInd = m * ldD + n;
				if constexpr (hasA && hasB)
					dest[destInd] = std::fma(alpha, A[ya * ldA + xa] * B[yb * ldB + xb], beta * dest[destInd]);
				else if constexpr (hasA)
					dest[destInd] = alpha * A[ya * ldA + xa] * B[yb * ldB + xb];
				else if constexpr (hasB)
					dest[destInd] = std::fma(A[ya * ldA + xa], B[yb * ldB + xb], beta * dest[destInd]);
				else
					dest[destInd] = A[ya * ldA + xa] * B[yb * ldB + xb];
			}
		}
	}

	template<typename T>
	cudaError matricesKronecker(const T* A, const int ldA, const int rowsA, const int colsA, const T* B, const int ldB, const int rowsB, const int colsB, T* dest, const int ldD, const T alpha, const T beta)
	{
		const int rowsD = rowsA * rowsB;
		const int colsD = colsA * colsB;
		auto ker = kroneckerKernel<T, true, true>;
		if (alpha == T(1) && beta == T(0))
		{
			ker = kroneckerKernel<T, false, false>;
		}
		else if (alpha == T(1))
		{
			ker = kroneckerKernel<T, false, true>;
		}
		else if (beta == T(0))
		{
			ker = kroneckerKernel<T, true, false>;
		}

		dim3 dimBlock, dimGrid;
		cudaError err = calc2DKernelPara(rowsD, colsD, ker, dimBlock, dimGrid);
		if (err != cudaError::cudaSuccess)
			return err;

		ker << <dimGrid, dimBlock >> > (A, ldA, rowsA, colsA, B, ldB, rowsB, colsB, dest, ldD, rowsD, colsD, alpha, beta);

		return err;
	}

	DLLEXP cudaError matKronS(const float* A, const int ldA, const int rowsA, const int colsA, const float* B, const int ldB, const int rowsB, const int colsB, float* dest, const int ldD, const float alpha, const float beta)
	{
		return GPUVersion::matricesKronecker(A, ldA, rowsA, colsA, B, ldB, rowsB, colsB, dest, ldD, alpha, beta);
	}
	DLLEXP cudaError matKronD(const double* A, const int ldA, const int rowsA, const int colsA, const double* B, const int ldB, const int rowsB, const int colsB, double* dest, const int ldD, const double alpha, const double beta)
	{
		return GPUVersion::matricesKronecker(A, ldA, rowsA, colsA, B, ldB, rowsB, colsB, dest, ldD, alpha, beta);
	}
	DLLEXP cudaError matKronC(const complexSingle* A, const int ldA, const int rowsA, const int colsA, const complexSingle* B, const int ldB, const int rowsB, const int colsB, complexSingle* dest, const int ldD, const complexSingle alpha, const complexSingle beta)
	{
		return GPUVersion::matricesKronecker(A, ldA, rowsA, colsA, B, ldB, rowsB, colsB, dest, ldD, alpha, beta);
	}
	DLLEXP cudaError matKronZ(const complexDouble* A, const int ldA, const int rowsA, const int colsA, const complexDouble* B, const int ldB, const int rowsB, const int colsB, complexDouble* dest, const int ldD, const complexDouble alpha, const complexDouble beta)
	{
		return GPUVersion::matricesKronecker(A, ldA, rowsA, colsA, B, ldB, rowsB, colsB, dest, ldD, alpha, beta);
	}
}
#pragma endregion


#pragma region make matrix Hermitian by copying its upper part to its lower part (GPU version)
#ifdef CPU


#else

template<typename T>
__global__ void upperCopyToLowerKernel(T* a, const int ld, const int rows)
{
	constexpr int TILE_DIM = 256 / sizeof(T);

	const int x = blockIdx.x * TILE_DIM + threadIdx.x;
	const int y = blockIdx.y * TILE_DIM + threadIdx.y;

	if (x >= rows || x >= y)
		return;

	__shared__ T tile[TILE_DIM][TILE_DIM + 1];

	for (int j = 0; j < TILE_DIM; j++)
		tile[threadIdx.y][threadIdx.x + j] = a[y * ld + (x + j)];

	__syncthreads(); // the tile is now filled by different threads

	for (int j = 0; j < TILE_DIM; j++)
		a[(x + j) * ld + y] = std::conjAllCase(tile[threadIdx.x + j][threadIdx.y]);
}



template <typename T>
struct complexOnlyRealPart_functor
{
	__host__ __device__ T operator()(const T x) const
	{
		return T(x.real());
	}
};


template<typename T>
cudaError matrixMakeHermitianGPU(void* Av, const int ld, const int rows)
{
	T* A = (T*)Av;
	auto ker = upperCopyToLowerKernel<T>;

	dim3 dimBlock, dimGrid;
	cudaError err = calc2DKernelPara(ld, ld, ker, dimBlock, dimGrid);
	if (err != cudaError::cudaSuccess)
		return err;

	ker<<<dimGrid, dimBlock>>>(A, ld, rows);
	err = cudaDeviceSynchronize();
	if (err != cudaError::cudaSuccess)
		return err;

	if constexpr (!std::is_scalar_v<T>)
	{	// set the diagonal elements' imaginary parts to zero
		auto strideA = make_strided_range(A, rows, ld + 1);
		thrust::transform(THRUST_PAR, strideA.begin(), strideA.end(), strideA.begin(), complexOnlyRealPart_functor<T>());
	}

	return err;
}
#endif // CPU


DLLEXP
ERROR_RETURN matMakeHerm(const Datatype::DataType type, void* A, const int ld, const int rows)
{
	AUTO_SIGNED_TYPE_FUNC(matrixMakeHermitianGPU, type, A, ld, rows);
}
#pragma endregion


#pragma region sparse vectors outer product to COOC matrix (GPU version)
template <typename T, bool conj>
__global__ void KerSpVecOuter(
	const T* valA, const int* indA, const size_t nnzA,
	const T* valB, const int* indB, const size_t nnzB,
	T* C, int* rowC, int* colC)
{
	const int n = blockIdx.x * blockDim.x + threadIdx.x;
	const int m = blockIdx.y * blockDim.y + threadIdx.y;
	if (n < nnzA && m < nnzB)
	{ // m for column (second dim), n for row (lead dim)
		size_t idx = n + m * nnzA;
		rowC[idx] = indA[n];
		colC[idx] = indB[m];
		if constexpr (conj)
			C[idx] = valA[n] * std::conjAllCase(valB[m]);
		else
			C[idx] = valA[n] * valB[m];
	}
}

template<typename T>
cudaError sparseVectorsOuter(
	const T* valA, const int* indA, const size_t nnzA,
	const T* valB, const int* indB, const size_t nnzB,
	T* C, int* rowC, int* colC, const bool conj)
{
	auto ker = KerSpVecOuter<T, false>;
	if (conj)
		ker = KerSpVecOuter<T, true>;

	dim3 dimBlock, dimGrid;
	cudaError err = calc2DKernelPara(nnzA, nnzB, ker, dimBlock, dimGrid);
	if (err != cudaError::cudaSuccess)
		return err;

	ker<<<dimGrid, dimBlock>>>(valA, indA, nnzA, valB, indB, nnzB, C, rowC, colC);

	return err;
}

DLLEXP cudaError spVecOuterS(
	const float* valA, const int* indA, const size_t nnzA,
	const float* valB, const int* indB, const size_t nnzB,
	float* C, int* rowC, int* colC, const bool conj)
{
	return sparseVectorsOuter(valA, indA, nnzA, valB, indB, nnzB, C, rowC, colC, conj);
}
DLLEXP cudaError spVecOuterD(
	const double* valA, const int* indA, const size_t nnzA,
	const double* valB, const int* indB, const size_t nnzB,
	double* C, int* rowC, int* colC, const bool conj)
{
	return sparseVectorsOuter(valA, indA, nnzA, valB, indB, nnzB, C, rowC, colC, conj);
}
DLLEXP cudaError spVecOuterC(
	const complexSingle* valA, const int* indA, const size_t nnzA,
	const complexSingle* valB, const int* indB, const size_t nnzB,
	complexSingle* C, int* rowC, int* colC, const bool conj)
{
	return sparseVectorsOuter(valA, indA, nnzA, valB, indB, nnzB, C, rowC, colC, conj);
}
DLLEXP cudaError spVecOuterZ(
	const complexDouble* valA, const int* indA, const size_t nnzA,
	const complexDouble* valB, const int* indB, const size_t nnzB,
	complexDouble* C, int* rowC, int* colC, const bool conj)
{
	return sparseVectorsOuter(valA, indA, nnzA, valB, indB, nnzB, C, rowC, colC, conj);
}
#pragma endregion


#pragma region sparse COO matrix Kronecker (GPU version)
template <typename T>
__global__ void cooMatricesKroneckerKernel(
	const T* valA, const int* rowA, const int* colA, const size_t nnzA,
	const T* valB, const int* rowB, const int* colB, const size_t nnzB, const size_t ldB, const size_t sdB,
	T* valC, int* rowC, int* colC)
{
	const int n = blockIdx.x * blockDim.x + threadIdx.x;
	const int m = blockIdx.y * blockDim.y + threadIdx.y;
	if (n < nnzA && m < nnzB)
	{ // m for column (second dim), n for row (lead dim)
		size_t idx = m + n * nnzB;
		rowC[idx] = rowA[n] * ldB + rowB[m];
		colC[idx] = colA[n] * sdB + colB[m];
		valC[idx] = valA[n] * valB[m];
	}
}

template<typename T>
cudaError cooMatricesKronecker(
	const T* valA, const int* rowA, const int* colA, const size_t nnzA,
	const T* valB, const int* rowB, const int* colB, const size_t nnzB, const size_t ldB, const size_t sdB,
	T* valC, int* rowC, int* colC)
{
	auto ker = cooMatricesKroneckerKernel<T>;

	dim3 dimBlock, dimGrid;
	cudaError err = calc2DKernelPara(nnzA, nnzB, ker, dimBlock, dimGrid);
	if (err != cudaError::cudaSuccess)
		return err;

	ker<<<dimGrid, dimBlock>>>(valA, rowA, colA, nnzA, valB, rowB, colB, nnzB, ldB, sdB, valC, rowC, colC);

	return err;
}

DLLEXP cudaError cooMatKronS(
	const float* valA, const int* rowA, const int* colA, const size_t nnzA,
	const float* valB, const int* rowB, const int* colB, const size_t nnzB, const size_t ldB, const size_t sdB,
	float* valC, int* rowC, int* colC)
{
	return cooMatricesKronecker(valA, rowA, colA, nnzA, valB, rowB, colB, nnzB, ldB, sdB, valC, rowC, colC);
}
DLLEXP cudaError cooMatKronD(
	const double* valA, const int* rowA, const int* colA, const size_t nnzA,
	const double* valB, const int* rowB, const int* colB, const size_t nnzB, const size_t ldB, const size_t sdB,
	double* valC, int* rowC, int* colC)
{
	return cooMatricesKronecker(valA, rowA, colA, nnzA, valB, rowB, colB, nnzB, ldB, sdB, valC, rowC, colC);
}
DLLEXP cudaError cooMatKronC(
	const complexSingle* valA, const int* rowA, const int* colA, const size_t nnzA,
	const complexSingle* valB, const int* rowB, const int* colB, const size_t nnzB, const size_t ldB, const size_t sdB,
	complexSingle* valC, int* rowC, int* colC)
{
	return cooMatricesKronecker(valA, rowA, colA, nnzA, valB, rowB, colB, nnzB, ldB, sdB, valC, rowC, colC);
}
DLLEXP cudaError cooMatKronZ(
	const complexDouble* valA, const int* rowA, const int* colA, const size_t nnzA,
	const complexDouble* valB, const int* rowB, const int* colB, const size_t nnzB, const size_t ldB, const size_t sdB,
	complexDouble* valC, int* rowC, int* colC)
{
	return cooMatricesKronecker(valA, rowA, colA, nnzA, valB, rowB, colB, nnzB, ldB, sdB, valC, rowC, colC);
}
#pragma endregion




#pragma region matrix Kronecker
namespace kronecker
{
	typedef const int uint;

	template <bool left, bool largerLeadDim>
	struct iter_functor
	{
		uint ldB, colsB, ldDst, rowsDst;
		uint ldThis, rowsThis;
		iter_functor(uint ldB, uint colsB, uint ldDst, uint rowsDst, uint ldThis, uint rowsThis) :
			ldB(ldB), colsB(colsB), ldDst(ldDst), rowsDst(rowsDst), ldThis(ldThis), rowsThis(rowsThis) {}

		__host__ __device__ uint operator()(const uint posDst) const
		{
			div_t pos = std::div(posDst, ldDst);
			if constexpr (largerLeadDim)
			{	// return 0 immediately if out of range
				if (pos.rem >= rowsDst)
					return 0;
			}
			int x, y;
			if constexpr (left)
			{
				x = pos.rem / ldB;
				y = pos.quot / colsB;
			}
			else
			{
				x = pos.rem % ldB;
				y = pos.quot % colsB;
			}
			return y * ldThis + x;
		}
	};

	template <typename T, bool largerLeadDim, bool hasAlpha, bool hasBeta>
	struct multiply_functor
	{
		const T alpha, beta;
		uint ldD, rowsD;
		multiply_functor(const T alpha, const T beta, uint ldD, uint rowsD) :
			alpha(alpha), beta(beta), ldD(ldD), rowsD(rowsD) {}

		// A, B, D, position of D
		typedef typename thrust::tuple<const T, const T, const T, uint> Tuple;

		__host__ __device__ T operator()(const Tuple t) const
		{
			if constexpr (largerLeadDim)
			{	// return D immediately if out of range
				if ((t.get<3>() % ldD) >= rowsD)
				{
					return t.get<2>();
				}
			}
			if constexpr (hasAlpha && hasBeta)
				return std::fma(alpha, t.get<0>() * t.get<1>(), beta * t.get<2>());
			else if constexpr (hasAlpha)
				return alpha * t.get<0>() * t.get<1>();
			else if constexpr (hasBeta)
				return std::fma(t.get<0>(), t.get<1>(), beta * t.get<2>());
			else
				return t.get<0>() * t.get<1>();
		}
	};
}

template<typename T>
inline void matricesKronecker(
	const void* Av, const int ldA, const int rowsA, const int colsA,
	const void* Bv, const int ldB, const int rowsB, const int colsB,
	void* destv, const int ldD, const void* alphav, const void* betav)
{
	// cast
	const T* A = (const T*)Av;
	const T* B = (const T*)Bv;
	T* dest = (T*)destv;
	const T alpha = *((const T*)alphav);
	const T beta = *((const T*)betav);

	const int rowsD = rowsA * rowsB;
	const int colsD = colsA * colsB;
	
#define KRON_CODE(bool1, bool2, bool3) do { \
		/*make iterators of A and B*/ \
		auto count = thrust::make_counting_iterator(0); \
		auto permA = thrust::make_transform_iterator(count, kronecker::iter_functor<true,  bool1>(ldB, colsB, ldD, rowsD, ldA, rowsA)); \
		auto permB = thrust::make_transform_iterator(count, kronecker::iter_functor<false, bool1>(ldB, colsB, ldD, rowsD, ldB, rowsB)); \
		auto iterA = thrust::make_permutation_iterator(A, permA); \
		auto iterB = thrust::make_permutation_iterator(B, permB); \
		/*make zip iterator of A, B, D and position of D*/ \
		auto zip = thrust::make_zip_iterator(thrust::make_tuple(iterA, iterB, dest, count)); \
		/*calculate*/ \
		thrust::transform(THRUST_PAR, zip, zip + ldD * colsD, dest, kronecker::multiply_functor<T, bool1, bool2, bool3>(alpha, beta, ldD, rowsD)); \
	} while (0)

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
	const void* A, const int ldA, const int rowsA, const int colsA,
	const void* B, const int ldB, const int rowsB, const int colsB,
	void* dest, const int ldD, const void* alpha, const void* beta)
{
	AUTO_ALLTYPE_FUNC(matricesKronecker, type, A, ldA, rowsA, colsA, B, ldB, rowsB, colsB, dest, ldD, alpha, beta);
}
#pragma endregion