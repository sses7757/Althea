#include "macro.h"


#ifdef CPU

#else

#pragma region matrix Kronecker (GPU version)
// TODO: the Kronecker product of two matrices can be achieved by
//	1. outer product of two matrices' column vectors
//	2. reshape the matrix to a proper rank-4 tensor
//	3. permute the tensor [3,1,4,2] (may be)
//	4. reshape the tensor to the output matrix
// Test this

// TODO: can improve by avoid bank conflict, etc.

template <typename T, bool hasA, bool hasB>
__global__ void kroneckerKernel(const T* A, const unsigned int ldA, const unsigned int rowsA, const unsigned int colsA,
								const T* B, const unsigned int ldB, const unsigned int rowsB, const unsigned int colsB,
								T* dest,	const unsigned int ldD, const unsigned int rowsD, const unsigned int colsD,
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
	// constants
	int N = rowsA * colsA * rowsB * colsB;
	if (N < 0)
		N = INT_MAX;

	auto ker = kroneckerKernel<T, true, true>;
	if (alpha == 1 && beta == 0)
	{
		ker = kroneckerKernel<T, false, false>;
	}
	else if (alpha == 1)
	{
		ker = kroneckerKernel<T, false, true>;
	}
	else if (beta == 0)
	{
		ker = kroneckerKernel<T, true, false>;
	}

	int blockSize, minGridSize;
	cudaError err = cudaOccupancyMaxPotentialBlockSize(&minGridSize, &blockSize, ker, 0, N);
	if (err != 0) return err;

	const int rowsD = rowsA * rowsB;
	const int colsD = colsA * colsB;
	dim3 dimBlock((int)sqrt(blockSize), (int)sqrt(blockSize));
	dim3 dimGrid;
	dimGrid.x = (rowsD + dimBlock.x - 1) / dimBlock.x;
	dimGrid.y = (colsD + dimBlock.y - 1) / dimBlock.y;

	err = cudaDeviceSynchronize();
	if (err != 0) return err;

	ker << < dimGrid, dimBlock >> > (A, ldA, rowsA, colsA, B, ldB, rowsB, colsB, dest, ldD, rows, cols, alpha, beta);

	return cudaError::cudaSuccess;
}

EXTERN_C
DLLEXP cudaError matKronS(const float* A, const int ldA, const int rowsA, const int colsA, const float* B, const int ldB, const int rowsB, const int colsB, float* dest, const int ldD, const float alpha, const float beta)
{
	return matricesKronecker(A, ldA, rowsA, colsA, B, ldB, rowsB, colsB, dest, ldD, alpha, beta);
}
DLLEXP cudaError matKronD(const double* A, const int ldA, const int rowsA, const int colsA, const double* B, const int ldB, const int rowsB, const int colsB, double* dest, const int ldD, const double alpha, const double beta)
{
	return matricesKronecker(A, ldA, rowsA, colsA, B, ldB, rowsB, colsB, dest, ldD, alpha, beta);
}
DLLEXP cudaError matKronC(const cuFloatComplex* A, const int ldA, const int rowsA, const int colsA, const cuFloatComplex* B, const int ldB, const int rowsB, const int colsB, cuFloatComplex* dest, const int ldD, const cuFloatComplex alpha, const cuFloatComplex beta)
{
	return matricesKronecker(A, ldA, rowsA, colsA, B, ldB, rowsB, colsB, dest, ldD, alpha, beta);
}
DLLEXP cudaError matKronZ(const cuDoubleComplex* A, const int ldA, const int rowsA, const int colsA, const cuDoubleComplex* B, const int ldB, const int rowsB, const int colsB, cuDoubleComplex* dest, const int ldD, const cuDoubleComplex alpha, const cuDoubleComplex beta)
{
	return matricesKronecker(A, ldA, rowsA, colsA, B, ldB, rowsB, colsB, dest, ldD, alpha, beta);
}

END_EXTERN_C
#pragma endregion

#endif // CPU


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

EXTERN_C
DLLEXP void spVecIndToCooInds(const int* index, int* rowIdx, int* colIdx, const size_t N, const int ld)
{
	thrust::transform(THRUST_PAR, index, index + N, rowIdx, intModulus_functor(ld));
	thrust::transform(THRUST_PAR, index, index + N, colIdx, intDivide_functor(ld));
}

DLLEXP void CooIndxToSpVecInd(int* index, const int* rowIdx, const int* colIdx, const size_t N, const int ld)
{
	thrust::transform(THRUST_PAR, rowIdx, rowIdx + N, colIdx, index, intFMA_functor(ld));
}
END_EXTERN_C
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

EXTERN_C
DLLEXP size_t CSRGetNerBuffer(const int rows)
{
	return sizeof(int) * rows;
}

DLLEXP size_t CSRGetNerNnz(const int* csrRowPtr, const int rows, int* buffer)
{
	const int N = rows - 1;

	// get indexes
	thrust::sequence(THRUST_PAR, buffer, buffer + N);
	auto begin = thrust::make_zip_iterator(thrust::make_tuple(csrRowPtr, csrRowPtr + 1, buffer));
	auto end = thrust::make_zip_iterator(thrust::make_tuple(csrRowPtr + N, csrRowPtr + N + 1, buffer + N));
	thrust::transform(THRUST_PAR, begin, end, buffer, intCSRGetNER_functor());

	// remove negative indexes
	int* tempEnd = thrust::remove_if(THRUST_PAR, buffer, buffer + N, intLessThanZero_functor());
	size_t nnz = tempEnd - buffer;
	return nnz;
}

DLLEXP ERROR_RETURN CSRGetNerCal(size_t nnz, const int* buffer, int* nerOut)
{
#ifdef CPU
	memcpy(nerOut, buffer, sizeof(int) * nnz);
#else
	cudaError err = cudaMemcpy(nerOut, buffer, sizeof(int) * nnz, cudaMemcpyDeviceToDevice);
	return err;
#endif // CPU
}
END_EXTERN_C
#pragma endregion


#pragma region upper copy to lower matUpCpyLow (GPU version)
template<typename T>
__global__ void KerUpCpyLowS(T* a, const unsigned int ld, const unsigned int rows)
{
	unsigned int n = blockIdx.x * blockDim.x + threadIdx.x;
	unsigned int m = blockIdx.y * blockDim.y + threadIdx.y;
	if (n < rows && m < rows && n < m)
	{
		if ()
		a[n * ld + m] = a[m * ld + n];
	}
}
__global__ void KerUpCpyLowD(double* a, const unsigned int ld, const unsigned int rows)
{
	unsigned int n = blockIdx.x * blockDim.x + threadIdx.x;
	unsigned int m = blockIdx.y * blockDim.y + threadIdx.y;
	if (n < rows && m < rows && n < m)
	{
		a[n * ld + m] = a[m * ld + n];
	}
}
__global__ void KerUpCpyLowC(cuFloatComplex* a, const unsigned int ld, const unsigned int rows)
{
	unsigned int n = blockIdx.x * blockDim.x + threadIdx.x;
	unsigned int m = blockIdx.y * blockDim.y + threadIdx.y;
	if (n < rows && m < rows)
	{
		if (n < m)
			a[n * ld + m] = cuConjf(a[m * ld + n]);
		else if (n == m)
			a[n * ld + m] = make_cuFloatComplex(cuCrealf(a[n * ld + m]), 0);
	}
}
__global__ void KerUpCpyLowZ(cuDoubleComplex* a, const unsigned int ld, const unsigned int rows)
{
	unsigned int n = blockIdx.x * blockDim.x + threadIdx.x;
	unsigned int m = blockIdx.y * blockDim.y + threadIdx.y;
	if (n < rows && m < rows)
	{
		if (n < m)
			a[n * ld + m] = cuConj(a[m * ld + n]);
		else if (n == m)
			a[n * ld + m] = make_cuDoubleComplex(cuCreal(a[n * ld + m]), 0);
	}
}


template<typename T, typename Kernel>
cudaError matUpCpyLow(T* A, const unsigned int ld, const unsigned int rows, Kernel ker)
{
	// constants
	int N = ld * ld;
	if (N < 0)
		N = INT_MAX;

	int blockSize, minGridSize;
	cudaError err = cudaOccupancyMaxPotentialBlockSize(&minGridSize, &blockSize, ker, 0, N);
	if (err != 0)
		return err;
	dim3 dimBlock((int)sqrt(blockSize), (int)sqrt(blockSize));
	dim3 dimGrid;
	dimGrid.x = (ld + dimBlock.x - 1) / dimBlock.x;
	dimGrid.y = (ld + dimBlock.y - 1) / dimBlock.y;

	ker<<<dimGrid, dimBlock>>>(A, ld, rowsD);

	err = cudaDeviceSynchronize();
	return err;
}

EXTERN_C
DLLEXP cudaError matUpCpyLowS(float* A, const int ld, const int rows)
{
	return matUpCpyLow(A, ld, rows, KerUpCpyLowS);
}
DLLEXP cudaError matUpCpyLowD(double* A, const int ld, const int rows)
{
	return matUpCpyLow(A, ld, rows, KerUpCpyLowD);
}
DLLEXP cudaError matUpCpyLowC(cuFloatComplex* A, const int ld, const int rows)
{
	return matUpCpyLow(A, ld, rows, KerUpCpyLowC);
}
DLLEXP cudaError matUpCpyLowZ(cuDoubleComplex* A, const int ld, const int rows)
{
	return matUpCpyLow(A, ld, rows, KerUpCpyLowZ);
}
END_EXTERN_C
#pragma endregion


#pragma region sparse vectors outer product to COOC matrix (GPU version)
template <typename T, bool conj>
__global__ void KerSpVecOuter(const float* valA, const int* indA, const size_t nnzA, const float* valB, const int* indB, const size_t nnzB, float* C, int* rowC, int* colC)
{
	const unsigned int n = blockIdx.x * blockDim.x + threadIdx.x;
	const unsigned int m = blockIdx.y * blockDim.y + threadIdx.y;
	if (n < nnzA && m < nnzB)
	{ // m for column (second dim), n for row (lead dim)
		size_t idx = n + m * nnzA;
		rowC[idx] = indA[n];
		colC[idx] = indB[m];
		if constexpr (conj)
			C[idx] = valA[n] * valB[m];
		else
			C[idx] = valA[n] * valB[m];
	}
}
__global__ void KerSpVecOuterD
	(const double* valA, const int* indA, const size_t nnzA,
	 const double* valB, const int* indB, const size_t nnzB,
	 double* C, int* rowC, int* colC)
{
	size_t n = blockIdx.x * blockDim.x + threadIdx.x;
	size_t m = blockIdx.y * blockDim.y + threadIdx.y;
	if (n < nnzA && m < nnzB)
	{ // m for column (second dim), n for row (lead dim)
		size_t idx = n + m * nnzA;
		rowC[idx] = indA[n];
		colC[idx] = indB[m];
		C[idx] = valA[n] * valB[m];
	}
}
__global__ void KerSpVecOuterC
	(const cuFloatComplex* valA, const int* indA, const size_t nnzA,
	 const cuFloatComplex* valB, const int* indB, const size_t nnzB,
	 cuFloatComplex* C, int* rowC, int* colC)
{
	size_t n = blockIdx.x * blockDim.x + threadIdx.x;
	size_t m = blockIdx.y * blockDim.y + threadIdx.y;
	if (n < nnzA && m < nnzB)
	{ // m for column (second dim), n for row (lead dim)
		size_t idx = n + m * nnzA;
		rowC[idx] = indA[n];
		colC[idx] = indB[m];
		C[idx] = valA[n] * cuConjf(valB[m]);
	}
}
__global__ void KerSpVecOuterZ
	(const cuDoubleComplex* valA, const int* indA, const size_t nnzA,
	 const cuDoubleComplex* valB, const int* indB, const size_t nnzB,
	 cuDoubleComplex* C, int* rowC, int* colC)
{
	size_t n = blockIdx.x * blockDim.x + threadIdx.x;
	size_t m = blockIdx.y * blockDim.y + threadIdx.y;
	if (n < nnzA && m < nnzB)
	{ // m for column (second dim), n for row (lead dim)
		size_t idx = n + m * nnzA;
		rowC[idx] = indA[n];
		colC[idx] = indB[m];
		C[idx] = valA[n] * cuConj(valB[m]);
	}
}
__global__ void KerSpVecOuterNonconjC
(const cuFloatComplex* valA, const int* indA, const size_t nnzA,
	const cuFloatComplex* valB, const int* indB, const size_t nnzB,
	cuFloatComplex* C, int* rowC, int* colC)
{
	size_t n = blockIdx.x * blockDim.x + threadIdx.x;
	size_t m = blockIdx.y * blockDim.y + threadIdx.y;
	if (n < nnzA && m < nnzB)
	{ // m for column (second dim), n for row (lead dim)
		size_t idx = n + m * nnzA;
		rowC[idx] = indA[n];
		colC[idx] = indB[m];
		C[idx] = valA[n] * valB[m];
	}
}
__global__ void KerSpVecOuterNonconjZ
(const cuDoubleComplex* valA, const int* indA, const size_t nnzA,
	const cuDoubleComplex* valB, const int* indB, const size_t nnzB,
	cuDoubleComplex* C, int* rowC, int* colC)
{
	size_t n = blockIdx.x * blockDim.x + threadIdx.x;
	size_t m = blockIdx.y * blockDim.y + threadIdx.y;
	if (n < nnzA && m < nnzB)
	{ // m for column (second dim), n for row (lead dim)
		size_t idx = n + m * nnzA;
		rowC[idx] = indA[n];
		colC[idx] = indB[m];
		C[idx] = cuCmul(valA[n], valB[m]);
	}
}

template<typename T, typename Kernel>
cudaError spVecOuter(const T* valA, const int* indA, const size_t nnzA,
	const T* valB, const int* indB, const size_t nnzB,
	T* C, int* rowC, int* colC, const Kernel ker)
{
	// constants
	int N = (int)(nnzA * nnzB);
	if (N < 0)
		N = INT_MAX;
	int blockSize, minGridSize;
	cudaError err = cudaOccupancyMaxPotentialBlockSize(&minGridSize, &blockSize, ker, 0, N);
	if (err != 0)
		return err;
	dim3 dimBlock((int)sqrt(blockSize), (int)sqrt(blockSize));
	dim3 dimGrid;
	dimGrid.x = (nnzA + dimBlock.x - 1) / dimBlock.x;
	dimGrid.y = (nnzB + dimBlock.y - 1) / dimBlock.y;

	ker<<<dimGrid, dimBlock>>>(valA, indA, nnzA, valB, indB, nnzB, C, rowC, colC);

	err = cudaDeviceSynchronize();
	return err;
}

EXTERN_C
DLLEXP cudaError spVecOuterS(
	const float* valA, const int* indA, const size_t nnzA,
	const float* valB, const int* indB, const size_t nnzB,
	float* C, int* rowC, int* colC)
{
	return spVecOuter(valA, indA, nnzA, valB, indB, nnzB, C, rowC, colC, KerSpVecOuterS);
}
DLLEXP cudaError spVecOuterD(
	const double* valA, const int* indA, const size_t nnzA,
	const double* valB, const int* indB, const size_t nnzB,
	double* C, int* rowC, int* colC)
{
	return spVecOuter(valA, indA, nnzA, valB, indB, nnzB, C, rowC, colC, KerSpVecOuterD);
}
DLLEXP cudaError spVecOuterC(
	const cuFloatComplex* valA, const int* indA, const size_t nnzA,
	const cuFloatComplex* valB, const int* indB, const size_t nnzB,
	cuFloatComplex* C, int* rowC, int* colC)
{
	return spVecOuter(valA, indA, nnzA, valB, indB, nnzB, C, rowC, colC, KerSpVecOuterC);
}
DLLEXP cudaError spVecOuterZ(
	const cuDoubleComplex* valA, const int* indA, const size_t nnzA,
	const cuDoubleComplex* valB, const int* indB, const size_t nnzB,
	cuDoubleComplex* C, int* rowC, int* colC)
{
	return spVecOuter(valA, indA, nnzA, valB, indB, nnzB, C, rowC, colC, KerSpVecOuterZ);
}
DLLEXP cudaError spVecOuterNonconjC(const cuFloatComplex* valA, const int* indA, const size_t nnzA,
	const cuFloatComplex* valB, const int* indB, const size_t nnzB,
	cuFloatComplex* C, int* rowC, int* colC)
{
	return spVecOuter(valA, indA, nnzA, valB, indB, nnzB, C, rowC, colC, KerSpVecOuterNonconjC);
}
DLLEXP cudaError spVecOuterNonconjZ(const cuDoubleComplex* valA, const int* indA, const size_t nnzA,
	const cuDoubleComplex* valB, const int* indB, const size_t nnzB,
	cuDoubleComplex* C, int* rowC, int* colC)
{
	return spVecOuter(valA, indA, nnzA, valB, indB, nnzB, C, rowC, colC, KerSpVecOuterNonconjZ);
}
END_EXTERN_C
#pragma endregion


#pragma region sparse COO matrix Kronecker
__global__ void KerCooMatKronS(const float* valA, const int* rowA, const int* colA, const size_t nnzA,
	const float* valB, const int* rowB, const int* colB, const size_t nnzB, const size_t ldB, const size_t sdB,
	float* valC, int* rowC, int* colC)
{
	size_t n = blockIdx.x * blockDim.x + threadIdx.x;
	size_t m = blockIdx.y * blockDim.y + threadIdx.y;
	if (n < nnzA && m < nnzB)
	{ // m for column (second dim), n for row (lead dim)
		size_t idx = m + n * nnzB;
		rowC[idx] = rowA[n] * ldB + rowB[m];
		colC[idx] = colA[n] * sdB + colB[m];
		valC[idx] = valA[n] * valB[m];
	}
}
__global__ void KerCooMatKronD(const double* valA, const int* rowA, const int* colA, const size_t nnzA,
	const double* valB, const int* rowB, const int* colB, const size_t nnzB, const size_t ldB, const size_t sdB,
	double* valC, int* rowC, int* colC)
{
	size_t n = blockIdx.x * blockDim.x + threadIdx.x;
	size_t m = blockIdx.y * blockDim.y + threadIdx.y;
	if (n < nnzA && m < nnzB)
	{ // m for column (second dim), n for row (lead dim)
		size_t idx = m + n * nnzB;
		rowC[idx] = rowA[n] * ldB + rowB[m];
		colC[idx] = colA[n] * sdB + colB[m];
		valC[idx] = valA[n] * valB[m];
	}
}
__global__ void KerCooMatKronC(const cuFloatComplex* valA, const int* rowA, const int* colA, const size_t nnzA,
	const cuFloatComplex* valB, const int* rowB, const int* colB, const size_t nnzB, const size_t ldB, const size_t sdB,
	cuFloatComplex* valC, int* rowC, int* colC)
{
	size_t n = blockIdx.x * blockDim.x + threadIdx.x;
	size_t m = blockIdx.y * blockDim.y + threadIdx.y;
	if (n < nnzA && m < nnzB)
	{ // m for column (second dim), n for row (lead dim)
		size_t idx = m + n * nnzB;
		rowC[idx] = rowA[n] * ldB + rowB[m];
		colC[idx] = colA[n] * sdB + colB[m];
		valC[idx] = cuCmulf(valA[n], valB[m]);
	}
}
__global__ void KerCooMatKronZ(const cuDoubleComplex* valA, const int* rowA, const int* colA, const size_t nnzA,
	const cuDoubleComplex* valB, const int* rowB, const int* colB, const size_t nnzB, const size_t ldB, const size_t sdB,
	cuDoubleComplex* valC, int* rowC, int* colC)
{
	size_t n = blockIdx.x * blockDim.x + threadIdx.x;
	size_t m = blockIdx.y * blockDim.y + threadIdx.y;
	if (n < nnzA && m < nnzB)
	{ // m for column (second dim), n for row (lead dim)
		size_t idx = m + n * nnzB;
		rowC[idx] = rowA[n] * ldB + rowB[m];
		colC[idx] = colA[n] * sdB + colB[m];
		valC[idx] = cuCmul(valA[n], valB[m]);
	}
}

template<typename T, typename Kernel>
cudaError cooMatKron(const T* valA, const int* rowA, const int* colA, const size_t nnzA,
	const T* valB, const int* rowB, const int* colB, const size_t nnzB, const size_t ldB, const size_t sdB,
	T* valC, int* rowC, int* colC, Kernel ker)
{
	// constants
	int N = (int)(nnzA * nnzB);
	if (N < 0)
		N = INT_MAX;
	int blockSize, minGridSize;
	cudaError err = cudaOccupancyMaxPotentialBlockSize(&minGridSize, &blockSize, ker, 0, N);
	if (err != 0)
		return err;
	dim3 dimBlock((int)sqrt(blockSize), (int)sqrt(blockSize));
	dim3 dimGrid;
	dimGrid.x = (nnzA + dimBlock.x - 1) / dimBlock.x;
	dimGrid.y = (nnzB + dimBlock.y - 1) / dimBlock.y;

	ker<<<dimGrid, dimBlock>>>(valA, rowA, colA, nnzA, valB, rowB, colB, nnzB, ldB, sdB, valC, rowC, colC);

	err = cudaDeviceSynchronize();
	return err;
}

EXTERN_C
DLLEXP cudaError cooMatKronS(const float* valA, const int* rowA, const int* colA, const size_t nnzA,
	const float* valB, const int* rowB, const int* colB, const size_t nnzB, const size_t ldB, const size_t sdB,
	float* valC, int* rowC, int* colC)
{
	return cooMatKron(valA, rowA, colA, nnzA, valB, rowB, colB, nnzB, ldB, sdB, valC, rowC, colC, KerCooMatKronS);
}
DLLEXP cudaError cooMatKronD(const double* valA, const int* rowA, const int* colA, const size_t nnzA,
	const double* valB, const int* rowB, const int* colB, const size_t nnzB, const size_t ldB, const size_t sdB,
	double* valC, int* rowC, int* colC)
{
	return cooMatKron(valA, rowA, colA, nnzA, valB, rowB, colB, nnzB, ldB, sdB, valC, rowC, colC, KerCooMatKronD);
}
DLLEXP cudaError cooMatKronC(const cuFloatComplex* valA, const int* rowA, const int* colA, const size_t nnzA,
	const cuFloatComplex* valB, const int* rowB, const int* colB, const size_t nnzB, const size_t ldB, const size_t sdB,
	cuFloatComplex* valC, int* rowC, int* colC)
{
	return cooMatKron(valA, rowA, colA, nnzA, valB, rowB, colB, nnzB, ldB, sdB, valC, rowC, colC, KerCooMatKronC);
}
DLLEXP cudaError cooMatKronZ(const cuDoubleComplex* valA, const int* rowA, const int* colA, const size_t nnzA,
	const cuDoubleComplex* valB, const int* rowB, const int* colB, const size_t nnzB, const size_t ldB, const size_t sdB,
	cuDoubleComplex* valC, int* rowC, int* colC)
{
	return cooMatKron(valA, rowA, colA, nnzA, valB, rowB, colB, nnzB, ldB, sdB, valC, rowC, colC, KerCooMatKronZ);
}
END_EXTERN_C
#pragma endregion
