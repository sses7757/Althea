// CUDA includes
#include "cuda_runtime.h"
#include "device_launch_parameters.h"
#include "cublas.h"
#include "math.h"
#include "cuComplex.h"

#include <thrust/remove.h>
#include "macro.h"


#pragma region matrix Kronecker
// TODO: can improve by avoid bank conflict, etc.
__global__ void KerKronS(const float* A, const unsigned int ldA, const unsigned int rowsA, const unsigned int colsA,
						 const float* B, const unsigned int ldB,  const unsigned int rowsB, const unsigned int colsB,
						 float* dest, const unsigned int ldD, const unsigned int rowsD, const unsigned int colsD)
{
	unsigned int n = blockIdx.x * blockDim.x + threadIdx.x;
	unsigned int m = blockIdx.y * blockDim.y + threadIdx.y;
	if (n < rowsD && m < colsD)
	{ // m for sd, n for ld
		unsigned int xb = n % ldB, yb = m % colsB;
		unsigned int xa = n / ldB, ya = m / colsB;
		if (xa < rowsA && xb < rowsB && ya < colsA && yb < colsB)
			dest[m * ldD + n] = A[ya * ldA + xa] * B[yb * ldB + xb];
	}
}

__global__ void KerKronD(const double* A, const unsigned int ldA, const unsigned int rowsA, const unsigned int colsA,
						 const double* B, const unsigned int ldB,  const unsigned int rowsB, const unsigned int colsB,
						 double* dest, const unsigned int ldD, const unsigned int rowsD, const unsigned int colsD)
{
	unsigned int n = blockIdx.x * blockDim.x + threadIdx.x;
	unsigned int m = blockIdx.y * blockDim.y + threadIdx.y;
	if (n < rowsD && m < colsD)
	{ // m for sd, n for ld
		unsigned int xb = n % ldB, yb = m % colsB;
		unsigned int xa = n / ldB, ya = m / colsB;
		if (xa < rowsA && xb < rowsB && ya < colsA && yb < colsB)
			dest[m * ldD + n] = A[ya * ldA + xa] * B[yb * ldB + xb];
	}
}

__global__ void KerKronC(const cuFloatComplex* A, const unsigned int ldA, const unsigned int rowsA, const unsigned int colsA,
						 const cuFloatComplex* B, const unsigned int ldB,  const unsigned int rowsB, const unsigned int colsB,
						 cuFloatComplex* dest, const unsigned int ldD, const unsigned int rowsD, const unsigned int colsD)
{
	unsigned int n = blockIdx.x * blockDim.x + threadIdx.x;
	unsigned int m = blockIdx.y * blockDim.y + threadIdx.y;
	if (n < rowsD && m < colsD)
	{ // m for sd, n for ld
		unsigned int xb = n % ldB, yb = m % colsB;
		unsigned int xa = n / ldB, ya = m / colsB;
		if (xa < rowsA && xb < rowsB && ya < colsA && yb < colsB)
			dest[m * ldD + n] = cuCmulf(A[ya * ldA + xa], B[yb * ldB + xb]);
	}
}

__global__ void KerKronZ(const cuDoubleComplex* A, const unsigned int ldA, const unsigned int rowsA, const unsigned int colsA,
						 const cuDoubleComplex* B, const unsigned int ldB,  const unsigned int rowsB, const unsigned int colsB,
						 cuDoubleComplex* dest, const unsigned int ldD, const unsigned int rowsD, const unsigned int colsD)
{
	unsigned int n = blockIdx.x * blockDim.x + threadIdx.x;
	unsigned int m = blockIdx.y * blockDim.y + threadIdx.y;
	if (n < rowsD && m < colsD)
	{ // m for sd, n for ld
		unsigned int xb = n % ldB, yb = m % colsB;
		unsigned int xa = n / ldB, ya = m / colsB;
		if (xa < rowsA && xb < rowsB && ya < colsA && yb < colsB)
			dest[m * ldD + n] = cuCmul(A[ya * ldA + xa], B[yb * ldB + xb]);
	}
}


template<typename T, typename Kernel>
cudaError matKron(const T* A, const int ldA, const int rowsA, const int colsA, const T* B, const int ldB,  const int rowsB, const int colsB, T* dest, const int ldD, Kernel ker)
{
	// constants
	int N = rowsA * colsA * rowsB * colsB;
	if (N < 0)
		N = INT_MAX;
	const int rows = rowsA * rowsB;
	const int cols = colsA * colsB;

	int blockSize, minGridSize;
	cudaError err = cudaOccupancyMaxPotentialBlockSize(&minGridSize, &blockSize, ker, 0, N);
	if (err != 0)
		return err;
	dim3 dimBlock((int)sqrt(blockSize), (int)sqrt(blockSize));
	dim3 dimGrid;
	dimGrid.x = (rows + dimBlock.x - 1) / dimBlock.x;
	dimGrid.y = (cols + dimBlock.y - 1) / dimBlock.y;

	ker<<<dimGrid, dimBlock>>>(A, ldA, rowsA, colsA, B, ldB, rowsB, colsB, dest, ldD, rows, cols);
	
	err = cudaDeviceSynchronize();
	return err;
}

EXTERN_C
DLLEXP cudaError matKronS(const float* A, const int ldA, const int rowsA, const int colsA,
						 const float* B, const int ldB, const int rowsB, const int colsB,
						 float* dest, const int ldD)
{
	return matKron(A, ldA, rowsA, colsA, B, ldB, rowsB, colsB, dest, ldD, KerKronS);
}

DLLEXP cudaError matKronD(const double* A, const int ldA, const int rowsA, const int colsA,
						 const double* B, const int ldB, const int rowsB, const int colsB,
						 double* dest, const int ldD)
{
	return matKron(A, ldA, rowsA, colsA, B, ldB, rowsB, colsB, dest, ldD, KerKronD);
}

DLLEXP cudaError matKronC(const cuFloatComplex* A, const int ldA, const int rowsA, const int colsA,
						 const cuFloatComplex* B, const int ldB, const int rowsB, const int colsB,
						 cuFloatComplex* dest, const int ldD)
{
	return matKron(A, ldA, rowsA, colsA, B, ldB, rowsB, colsB, dest, ldD, KerKronC);
}

DLLEXP cudaError matKronZ(const cuDoubleComplex* A, const int ldA, const int rowsA, const int colsA,
						 const cuDoubleComplex* B, const int ldB, const int rowsB, const int colsB,
						 cuDoubleComplex* dest, const int ldD)
{
	return matKron(A, ldA, rowsA, colsA, B, ldB, rowsB, colsB, dest, ldD, KerKronZ);
}

END_EXTERN_C
#pragma endregion


#pragma region index to COO and back
__global__ void KerIndexToCOO(const int* index, int* rowIdx, int* colIdx, const size_t N, const int ld)
{
	unsigned int p = blockDim.x * blockIdx.x + threadIdx.x;
	if (p < N)
	{
		size_t idx = index[p];
		rowIdx[p] = idx % ld;
		colIdx[p] = idx / ld;
	}
}

__global__ void KerCOOToIndex(int* index, const int* rowIdx, const int* colIdx, const size_t N, const int ld)
{
	unsigned int p = blockDim.x * blockIdx.x + threadIdx.x;
	if (p < N)
	{
		index[p] = rowIdx[p] + colIdx[p] * ld;
	}
}

EXTERN_C
DLLEXP cudaError indexToCOO(const int* index, int* rowIdx, int* colIdx, const size_t N, const int ld)
{
	int blockSize, minGridSize, gridSize;
	cudaError err = cudaOccupancyMaxPotentialBlockSize(&minGridSize, &blockSize, (void*)KerIndexToCOO, 0, N * N);
	if (err != 0) return err;
	gridSize = (N + blockSize - 1) / blockSize;
	KerIndexToCOO << <gridSize, blockSize >> > (index, rowIdx, colIdx, N, ld);
	err = cudaDeviceSynchronize();
	return err;
}

DLLEXP cudaError COOToIndex(int* index, const int* rowIdx, const int* colIdx, const size_t N, const int ld)
{
	int blockSize, minGridSize, gridSize;
	cudaError err = cudaOccupancyMaxPotentialBlockSize(&minGridSize, &blockSize, (void*)KerCOOToIndex, 0, N * N);
	if (err != 0) return err;
	gridSize = (N + blockSize - 1) / blockSize;
	KerCOOToIndex << <gridSize, blockSize >> > (index, rowIdx, colIdx, N, ld);
	err = cudaDeviceSynchronize();
	return err;
}
END_EXTERN_C
#pragma endregion


#pragma region CSR matrix get non-empty row indexes
struct intNegative2
{
	__host__ __device__ bool operator()(const int x) const
	{
		return x < 0;
	}
};
intNegative2 intNeg2;

__global__ void KerCsrGerNer(const int* index, const int N, int* out)
{
	unsigned int idx = threadIdx.x + blockIdx.x * blockDim.x;
	if (idx < N)
	{
		if (index[idx] != index[idx + 1])
		{
			out[idx] = idx;
		}
		else
		{
			out[idx] = -1;
		}
	}
}

EXTERN_C
DLLEXP size_t CSRGetNerBuffer(const int rows)
{
	return sizeof(int) * rows;
}

DLLEXP cudaError CSRGetNer(const int* csrRowPtr, const int rows, int& nnz, int* buffer, int*& nerOut)
{
	const int N = rows;

	// kernel to get indexes
	int blockSize, minGridSize, gridSize;
	cudaError err = cudaOccupancyMaxPotentialBlockSize(&minGridSize, &blockSize, KerCsrGerNer, 0, N);
	if (err != 0) return err;
	gridSize = (N + blockSize - 1) / blockSize;
	KerCsrGerNer << <gridSize, blockSize >> > (csrRowPtr, N, buffer);

	// remove negative indexes
	int* tempEnd = thrust::remove_if(THRUST_PAR, buffer, buffer + N, intNeg2);
	nnz = tempEnd - buffer;

	// copy to output
	err = cudaMalloc(&nerOut, sizeof(int) * nnz);
	if (err != 0) return err;
	err = cudaMemcpy(nerOut, buffer, sizeof(int) * nnz, cudaMemcpyDeviceToDevice);

	return err;
}
END_EXTERN_C
#pragma endregion


#pragma region upper copy to lower matUpCpyLow
__global__ void KerUpCpyLowS(float* a, const unsigned int ld, const unsigned int rows)
{
	unsigned int n = blockIdx.x * blockDim.x + threadIdx.x;
	unsigned int m = blockIdx.y * blockDim.y + threadIdx.y;
	if (n < rows && m < rows && n < m)
	{ // m for sd, n for ld
		a[n * ld + m] = a[m * ld + n];
	}
}
__global__ void KerUpCpyLowD(double* a, const unsigned int ld, const unsigned int rows)
{
	unsigned int n = blockIdx.x * blockDim.x + threadIdx.x;
	unsigned int m = blockIdx.y * blockDim.y + threadIdx.y;
	if (n < rows && m < rows && n < m)
	{ // m for sd, n for ld
		a[n * ld + m] = a[m * ld + n];
	}
}
__global__ void KerUpCpyLowC(cuFloatComplex* a, const unsigned int ld, const unsigned int rows)
{
	unsigned int n = blockIdx.x * blockDim.x + threadIdx.x;
	unsigned int m = blockIdx.y * blockDim.y + threadIdx.y;
	if (n < rows && m < rows)
	{ // m for sd, n for ld
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
	{ // m for sd, n for ld
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

	ker<<<dimGrid, dimBlock>>>(A, ld, rows);

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


#pragma region sparse vector outer to COOC matrix
__global__ void KerSpVecOuterS
	(const float* valA, const int* indA, const size_t nnzA,
	 const float* valB, const int* indB, const size_t nnzB,
	 float* C, int* rowC, int* colC)
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
		C[idx] = cuCmulf(valA[n], cuConjf(valB[m]));
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
		C[idx] = cuCmul(valA[n], cuConj(valB[m]));
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
		C[idx] = cuCmulf(valA[n], valB[m]);
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
	T* C, int* rowC, int* colC, Kernel ker)
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
DLLEXP cudaError spVecOuterS(const float* valA, const int* indA, const size_t nnzA,
	const float* valB, const int* indB, const size_t nnzB,
	float* C, int* rowC, int* colC)
{
	return spVecOuter(valA, indA, nnzA, valB, indB, nnzB, C, rowC, colC, KerSpVecOuterS);
}
DLLEXP cudaError spVecOuterD(const double* valA, const int* indA, const size_t nnzA,
	const double* valB, const int* indB, const size_t nnzB,
	double* C, int* rowC, int* colC)
{
	return spVecOuter(valA, indA, nnzA, valB, indB, nnzB, C, rowC, colC, KerSpVecOuterD);
}
DLLEXP cudaError spVecOuterC(const cuFloatComplex* valA, const int* indA, const size_t nnzA,
	const cuFloatComplex* valB, const int* indB, const size_t nnzB,
	cuFloatComplex* C, int* rowC, int* colC)
{
	return spVecOuter(valA, indA, nnzA, valB, indB, nnzB, C, rowC, colC, KerSpVecOuterC);
}
DLLEXP cudaError spVecOuterZ(const cuDoubleComplex* valA, const int* indA, const size_t nnzA,
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
