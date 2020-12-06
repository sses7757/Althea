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



#ifdef CPU

#else

template <typename Func>
__inline__ static cudaError calc2DKernelPara(size_t nx, size_t ny, Func ker, dim3& dimBlock, dim3& dimGrid)
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
	const int rowsD = rowsA * rowsB;
	const int colsD = colsA * colsB;
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

	dim3 dimBlock, dimGrid;
	cudaError err = calc2DKernelPara(rowsD, colsD, ker, dimBlock, dimGrid);
	if (err != cudaError::cudaSuccess)
		return err;

	ker<<<dimGrid, dimBlock>>>(A, ldA, rowsA, colsA, B, ldB, rowsB, colsB, dest, ldD, rowsD, colsD, alpha, beta);

	return err;
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
DLLEXP cudaError matKronC(const complexFloat* A, const int ldA, const int rowsA, const int colsA, const complexFloat* B, const int ldB, const int rowsB, const int colsB, complexFloat* dest, const int ldD, const complexFloat alpha, const complexFloat beta)
{
	return matricesKronecker(A, ldA, rowsA, colsA, B, ldB, rowsB, colsB, dest, ldD, alpha, beta);
}
DLLEXP cudaError matKronZ(const complexDouble* A, const int ldA, const int rowsA, const int colsA, const complexDouble* B, const int ldB, const int rowsB, const int colsB, complexDouble* dest, const int ldD, const complexDouble alpha, const complexDouble beta)
{
	return matricesKronecker(A, ldA, rowsA, colsA, B, ldB, rowsB, colsB, dest, ldD, alpha, beta);
}

END_EXTERN_C
#pragma endregion


#pragma region upper copy to lower matUpCpyLow (GPU version)
template<typename T>
__global__ void upperCopyToLowerKernel(T* a, const unsigned int ld, const unsigned int rows)
{
	const unsigned int n = blockIdx.x * blockDim.x + threadIdx.x;
	const unsigned int m = blockIdx.y * blockDim.y + threadIdx.y;
	if (n < rows && m < rows && n < m)
	{
		if constexpr (std::is_scalar_v<T>)
		{
			a[n * ld + m] = std::conjAllCase(a[m * ld + n]);
		}
		else
		{
			if (n < m)
			{
				a[n * ld + m] = std::conjAllCase(a[m * ld + n]);
			}
			else if (n == m)
			{
				const unsigned int idx = n * ld + m;
				a[idx] = std::conjAllCase(a[idx]);
			}
		}
	}
}

template<typename T>
cudaError matrixUpperCopyToLower(T* A, const unsigned int ld, const unsigned int rows)
{
	auto ker = upperCopyToLowerKernel<T>;

	dim3 dimBlock, dimGrid;
	cudaError err = calc2DKernelPara(ld, ld, ker, dimBlock, dimGrid);
	if (err != cudaError::cudaSuccess)
		return err;

	ker<<<dimGrid, dimBlock>>>(A, ld, rows);

	return err;
}

EXTERN_C
DLLEXP cudaError matUpCpyLowS(float* A, const int ld, const int rows)
{
	return matrixUpperCopyToLower(A, ld, rows);
}
DLLEXP cudaError matUpCpyLowD(double* A, const int ld, const int rows)
{
	return matrixUpperCopyToLower(A, ld, rows);
}
DLLEXP cudaError matUpCpyLowC(complexFloat* A, const int ld, const int rows)
{
	return matrixUpperCopyToLower(A, ld, rows);
}
DLLEXP cudaError matUpCpyLowZ(complexDouble* A, const int ld, const int rows)
{
	return matrixUpperCopyToLower(A, ld, rows);
}
END_EXTERN_C
#pragma endregion


#pragma region sparse vectors outer product to COOC matrix (GPU version)
template <typename T, bool conj>
__global__ void KerSpVecOuter(
	const T* valA, const int* indA, const size_t nnzA,
	const T* valB, const int* indB, const size_t nnzB,
	T* C, int* rowC, int* colC)
{
	const unsigned int n = blockIdx.x * blockDim.x + threadIdx.x;
	const unsigned int m = blockIdx.y * blockDim.y + threadIdx.y;
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

EXTERN_C
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
	const complexFloat* valA, const int* indA, const size_t nnzA,
	const complexFloat* valB, const int* indB, const size_t nnzB,
	complexFloat* C, int* rowC, int* colC, const bool conj)
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
END_EXTERN_C
#pragma endregion


#pragma region sparse COO matrix Kronecker (GPU version)
template <typename T>
__global__ void cooMatricesKroneckerKernel(
	const T* valA, const int* rowA, const int* colA, const size_t nnzA,
	const T* valB, const int* rowB, const int* colB, const size_t nnzB, const size_t ldB, const size_t sdB,
	T* valC, int* rowC, int* colC)
{
	const unsigned int n = blockIdx.x * blockDim.x + threadIdx.x;
	const unsigned int m = blockIdx.y * blockDim.y + threadIdx.y;
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

EXTERN_C
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
	const complexFloat* valA, const int* rowA, const int* colA, const size_t nnzA,
	const complexFloat* valB, const int* rowB, const int* colB, const size_t nnzB, const size_t ldB, const size_t sdB,
	complexFloat* valC, int* rowC, int* colC)
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
END_EXTERN_C
#pragma endregion

#endif // CPU