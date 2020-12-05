#include "macro.h"


#include <thrust/device_ptr.h>
#include <thrust/device_malloc.h>
#include <thrust/device_free.h>

#include <thrust/sequence.h>
#include <thrust/reduce.h>
#include <thrust/extrema.h>

#include <thrust/copy.h>
#include <thrust/remove.h>
#include <thrust/count.h>
#include <thrust/binary_search.h>
#include <thrust/set_operations.h>
#include <thrust/inner_product.h>



#pragma region set array values at positions
template <typename T>
__inline__ void vectorSetValuesAt(T* dst, const T value, const int* pos, const size_t posN)
{
	thrust::fill(thrust::cuda::par, thrust::make_permutation_iterator(dst, pos), thrust::make_permutation_iterator(dst, pos + posN), value);
}

EXTERN_C
DLLEXP void vecSetValAtS(float* a, const float v, const int* pos, const size_t posN)
{
	vectorSetValuesAt(a, v, pos, posN);
}
DLLEXP void vecSetValAtD(double* a, const double v, const int* pos, const size_t posN)
{
	vectorSetValuesAt(a, v, pos, posN);
}
DLLEXP void vecSetValAtC(cuFloatComplex* a, const cuFloatComplex v, const int* pos, const size_t posN)
{
	vectorSetValuesAt(a, v, pos, posN);
}
DLLEXP void vecSetValAtZ(cuDoubleComplex* a, const cuDoubleComplex v, const int* pos, const size_t posN)
{
	vectorSetValuesAt(a, v, pos, posN);
}
END_EXTERN_C
#pragma endregion


#pragma region operators for thrust
struct floatAxpy
{
	const float alpha;
	floatAxpy(float t) : alpha(t) {}

	__host__ __device__ float operator()(const float x, const float y) const
	{
		return x + alpha * y;
	}
};

struct doubleAxpy
{
	const double alpha;
	doubleAxpy(double t) : alpha(t) {}

	__host__ __device__ double operator()(const double x, const double y) const
	{
		return x + alpha * y;
	}
};

struct floatComplexAxpy
{
	const cuFloatComplex alpha;
	floatComplexAxpy(cuFloatComplex t) : alpha(t) {}

	__host__ __device__ cuFloatComplex operator()(const cuFloatComplex x, const cuFloatComplex y) const
	{
		return cuCaddf(x, cuCmulf(alpha, y));
	}
};

struct doubleComplexAxpy
{
	const cuDoubleComplex alpha;
	doubleComplexAxpy(cuDoubleComplex t) : alpha(t) {}

	__host__ __device__ cuDoubleComplex operator()(const cuDoubleComplex x, const cuDoubleComplex y) const
	{
		return cuCadd(x, cuCmul(alpha, y));
	}
};

struct intNotZero
{
	__host__ __device__ bool operator()(const int x) const
	{
		return x != 0;
	}
};
intNotZero notZero;

struct intNegative
{
	__host__ __device__ bool operator()(const int x) const
	{
		return x < 0;
	}
};
intNegative intNeg;

struct floatAboveThreshold
{
	const float threshold;
	floatAboveThreshold(float t) : threshold(t) {}

	__host__ __device__ bool operator()(const float x) const
	{
		return fabsf(x) > threshold;
	}
};

struct doubleAboveThreshold
{
	const double threshold;
	doubleAboveThreshold(double t) : threshold(t) {}

	__host__ __device__ bool operator()(const double x) const
	{
		return abs(x) > threshold;
	}
};

struct floatComplexAboveThreshold
{
	const float threshold;
	floatComplexAboveThreshold(float t) : threshold(t) {}

	__host__ __device__ bool operator()(const cuFloatComplex x) const
	{
		return cuCabsf(x) > threshold;
	}
};

struct doubleComplexAboveThreshold
{
	const double threshold;
	doubleComplexAboveThreshold(double t) : threshold(t) {}

	__host__ __device__ bool operator()(const cuDoubleComplex x) const
	{
		return cuCabs(x) > threshold;
	}
};

struct floatComplexDotC
{
	__host__ __device__ cuFloatComplex operator()(const cuFloatComplex x, const cuFloatComplex y) const
	{
		return cuCmulf(cuConjf(x), y);
	}
};
floatComplexDotC dotC;

struct doubleComplexDotZ
{
	__host__ __device__ cuDoubleComplex operator()(const cuDoubleComplex x, const cuDoubleComplex y) const
	{
		return cuCmul(cuConj(x), y);
	}
};
doubleComplexDotZ dotZ;
#pragma endregion


#pragma region dense vector prune to sparse vecPrune
// TODO: split to buffer, non-zeros, calculate
template <typename T, typename Predicate>
cudaError vecPrune(const T* v, const size_t N, const Predicate threshold, void* buffer, size_t& nnz, int*& indexOut, T*& valueOut)
{
	// create range sequence
	int* index = (int*)buffer;
	thrust::sequence(thrust::cuda::par, index, index + N);

	// create result container
	int* idxOut = N + (int*)buffer;
	T* valOut = (T*)(2 * N + (int*)buffer);
	
	// make zip
	auto zipBegin = thrust::make_zip_iterator(thrust::make_tuple(index, v));
	auto zipEnd = thrust::make_zip_iterator(thrust::make_tuple(index + N, v + N));
	auto resultBegin = thrust::make_zip_iterator(thrust::make_tuple(idxOut, valOut));

	// copy_if to get sparse indexes
	auto resultEnd = thrust::copy_if(thrust::cuda::par, zipBegin, zipEnd, v, resultBegin, threshold);
	nnz = resultEnd - resultBegin;

	// resize and get out arrays
	cudaError err = cudaMalloc(&indexOut, sizeof(int) * nnz);
	if (err != 0) return err;
	err = cudaMemcpy(indexOut, idxOut, sizeof(int) * nnz, cudaMemcpyDeviceToDevice);
	if (err != 0) return err;
	err = cudaMalloc(&valueOut, sizeof(T) * nnz);
	if (err != 0) return err;
	err = cudaMemcpy(valueOut, valOut, sizeof(T) * nnz, cudaMemcpyDeviceToDevice);
	if (err != 0) return err;

	// return
	return err;
}

EXTERN_C
DLLEXP cudaError vecPruneBuffer(const size_t N, const cudaDataType type, size_t& bufferSize)
{
	size_t res;
	switch (type)
	{
	case CUDA_R_32F:
		res = sizeof(float) * N + sizeof(int) * N * 2;
		break;
	case CUDA_C_32F:
		res = sizeof(cuFloatComplex) * N + sizeof(int) * N * 2;
		break;
	case CUDA_R_64F:
		res = sizeof(double) * N + sizeof(int) * N * 2;
		break;
	case CUDA_C_64F:
		res = sizeof(cuDoubleComplex) * N + sizeof(int) * N * 2;
		break;
	default:
		return cudaErrorNotSupported;
	}
	bufferSize = res;
	return cudaSuccess;
}

DLLEXP cudaError vecPruneS (const float* v, const size_t N, const float threshold, void* buffer,
							size_t& nnz, int*& indexOut, float*& valueOut)
{
	return vecPrune(v, N, floatAboveThreshold(threshold), buffer, nnz, indexOut, valueOut);
}
DLLEXP cudaError vecPruneD (const double* v, const size_t N, const float threshold, void* buffer,
							size_t& nnz, int*& indexOut, double*& valueOut)
{
	return vecPrune(v, N, doubleAboveThreshold(threshold), buffer, nnz, indexOut, valueOut);
}
DLLEXP cudaError vecPruneC (const cuFloatComplex* v, const size_t N, const float threshold, void* buffer,
							size_t& nnz, int*& indexOut, cuFloatComplex*& valueOut)
{
	return vecPrune<cuFloatComplex>(v, N, floatComplexAboveThreshold(threshold), buffer, nnz, indexOut, valueOut);
}
DLLEXP cudaError vecPruneZ (const cuDoubleComplex* v, const size_t N, const float threshold, void* buffer,
							size_t& nnz, int*& indexOut, cuDoubleComplex*& valueOut)
{
	return vecPrune<cuDoubleComplex>(v, N, doubleComplexAboveThreshold(threshold), buffer, nnz, indexOut, valueOut);
}
END_EXTERN_C
#pragma endregion


#pragma region dense sparse vector element-wise vecSpDivMulDn
__global__ void KerSpVecDivDnS(const float* dense, const size_t nnz, float* sparse, const int* index)
{
	unsigned int idx = threadIdx.x + blockIdx.x * blockDim.x;
	if (idx < nnz)
	{
		sparse[idx] /= dense[index[idx]];
	}
}
__global__ void KerSpVecDivDnD(const double* dense, const size_t nnz, double* sparse, const int* index)
{
	unsigned int idx = threadIdx.x + blockIdx.x * blockDim.x;
	if (idx < nnz)
	{
		sparse[idx] /= dense[index[idx]];
	}
}
__global__ void KerSpVecDivDnC(const cuFloatComplex* dense, const size_t nnz, cuFloatComplex* sparse, const int* index)
{
	unsigned int idx = threadIdx.x + blockIdx.x * blockDim.x;
	if (idx < nnz)
	{
		sparse[idx] = cuCdivf(sparse[idx], dense[index[idx]]);
	}
}
__global__ void KerSpVecDivDnZ(const cuDoubleComplex* dense, const size_t nnz, cuDoubleComplex* sparse, const int* index)
{
	unsigned int idx = threadIdx.x + blockIdx.x * blockDim.x;
	if (idx < nnz)
	{
		sparse[idx] = cuCdiv(sparse[idx], dense[index[idx]]);
	}
}

__global__ void KerSpVecMulDnS(const float* dense, const size_t nnz, float* sparse, const int* index)
{
	unsigned int idx = threadIdx.x + blockIdx.x * blockDim.x;
	if (idx < nnz)
	{
		sparse[idx] *= dense[index[idx]];
	}
}
__global__ void KerSpVecMulDnD(const double* dense, const size_t nnz, double* sparse, const int* index)
{
	unsigned int idx = threadIdx.x + blockIdx.x * blockDim.x;
	if (idx < nnz)
	{
		sparse[idx] *= dense[index[idx]];
	}
}
__global__ void KerSpVecMulDnC(const cuFloatComplex* dense, const size_t nnz, cuFloatComplex* sparse, const int* index)
{
	unsigned int idx = threadIdx.x + blockIdx.x * blockDim.x;
	if (idx < nnz)
	{
		sparse[idx] = cuCmulf(sparse[idx], dense[index[idx]]);
	}
}
__global__ void KerSpVecMulDnZ(const cuDoubleComplex* dense, const size_t nnz, cuDoubleComplex* sparse, const int* index)
{
	unsigned int idx = threadIdx.x + blockIdx.x * blockDim.x;
	if (idx < nnz)
	{
		sparse[idx] = cuCmul(sparse[idx], dense[index[idx]]);
	}
}

template<typename T, typename Kernel>
cudaError vecSpDivMulDn(const T* dense, const size_t nnz, T* sparse, const int* index, Kernel ker)
{
	int blockSize, minGridSize, gridSize;
	cudaError err = cudaOccupancyMaxPotentialBlockSize(&minGridSize, &blockSize, ker, 0, nnz);
	if (err != 0) return err;
	// Round up according to array size
	gridSize = (nnz + blockSize - 1) / blockSize;
	ker<<<gridSize, blockSize>>>(dense, nnz, sparse, index);
	err = cudaDeviceSynchronize();
	return err;
}

EXTERN_C
DLLEXP cudaError vecSpDivMulDnS(const float* dense, const size_t nnz, float* sparse, const int* index, const bool mul)
{
	return  mul ? vecSpDivMulDn(dense, nnz, sparse, index, KerSpVecMulDnS) :
				  vecSpDivMulDn(dense, nnz, sparse, index, KerSpVecDivDnS);
}
DLLEXP cudaError vecSpDivMulDnD(const double* dense, const size_t nnz, double* sparse, const int* index, const bool mul)
{
	return  mul ? vecSpDivMulDn(dense, nnz, sparse, index, KerSpVecMulDnD) :
				  vecSpDivMulDn(dense, nnz, sparse, index, KerSpVecDivDnD);
}
DLLEXP cudaError vecSpDivMulDnC(const cuFloatComplex* dense, const size_t nnz, cuFloatComplex* sparse, const int* index, const bool mul)
{
	return  mul ? vecSpDivMulDn(dense, nnz, sparse, index, KerSpVecMulDnC) :
				  vecSpDivMulDn(dense, nnz, sparse, index, KerSpVecDivDnC);
}
DLLEXP cudaError vecSpDivMulDnZ(const cuDoubleComplex* dense, const size_t nnz, cuDoubleComplex* sparse, const int* index, const bool mul)
{
	return  mul ? vecSpDivMulDn(dense, nnz, sparse, index, KerSpVecMulDnZ) :
				  vecSpDivMulDn(dense, nnz, sparse, index, KerSpVecDivDnZ);
}
END_EXTERN_C
#pragma endregion


#pragma region sparse vector add sparse vector vecSpAdd
// TODO: split to buffer, non-zeros, calculate
template <typename T, typename BinaryFunction>
cudaError vecSpAdd (const int* A_index, const T* A_value, const size_t nnzA,
					const int* B_index, const T* B_value, const size_t nnzB,
					void* buffer, BinaryFunction func, size_t& nnzC, int*& C_index, T*& C_value)
{
	int nnz = nnzA + nnzB;
	// get storage from buffer for the combined contents of sparse vectors A and B
	int* temp_index = (int*)buffer;
	T* temp_value = (T*)(nnz + (int*)buffer);

	// merge A and B by index
	thrust::merge_by_key(thrust::cuda::par, A_index, A_index + nnzA, B_index, B_index + nnzB, A_value, B_value, temp_index, temp_value);

	// compute number of unique indices, must larger than 0
	nnzC = thrust::inner_product(thrust::cuda::par, temp_index, temp_index + nnz - 1, temp_index + 1,
		int(0), thrust::plus<int>(), thrust::not_equal_to<int>()) + 1;

	// allocate space for output
	cudaError err = cudaMalloc(&C_index, sizeof(int) * nnzC);
	if (err != 0) return err;
	err = cudaMalloc(&C_value, sizeof(T) * nnzC);
	if (err != 0) return err;

	// sum values with the same index
	thrust::reduce_by_key(thrust::cuda::par, temp_index, temp_index + nnz, temp_value, C_index, C_value,
						  thrust::equal_to<int>(), func);

	return err;
}

EXTERN_C
DLLEXP cudaError vecSpAddBuffer(const size_t nnzA, const size_t nnzB, const cudaDataType type, size_t& bufferSize)
{
	size_t N = nnzA + nnzB;
	size_t res;
	switch (type)
	{
	case CUDA_R_32F:
		res = sizeof(float) * N + sizeof(int) * N;
		break;
	case CUDA_C_32F:
		res = sizeof(cuFloatComplex) * N + sizeof(int) * N;
		break;
	case CUDA_R_64F:
		res = sizeof(double) * N + sizeof(int) * N;
		break;
	case CUDA_C_64F:
		res = sizeof(cuDoubleComplex) * N + sizeof(int) * N;
		break;
	default:
		return cudaErrorNotSupported;
	}
	bufferSize = res;
	return cudaSuccess;
}

DLLEXP cudaError vecSpAddS (const int* A_index, const float* A_value, const size_t nnzA,
							const int* B_index, const float* B_value, const size_t nnzB,
							void* buffer, size_t& nnzC, int*& C_index, float*& C_value)
{
	return vecSpAdd(A_index, A_value, nnzA, B_index, B_value, nnzB, buffer, thrust::plus<float>(), nnzC, C_index, C_value);
}
DLLEXP cudaError vecSpAddD (const int* A_index, const double* A_value, const size_t nnzA,
							const int* B_index, const double* B_value, const size_t nnzB,
							void* buffer, size_t& nnzC, int*& C_index, double*& C_value)
{
	return vecSpAdd(A_index, A_value, nnzA, B_index, B_value, nnzB, buffer, thrust::plus<double>(), nnzC, C_index, C_value);
}
DLLEXP cudaError vecSpAddC (const int* A_index, const cuFloatComplex* A_value, const size_t nnzA,
							const int* B_index, const cuFloatComplex* B_value, const size_t nnzB,
							void* buffer, size_t& nnzC, int*& C_index, cuFloatComplex*& C_value)
{
	return vecSpAdd(A_index, A_value, nnzA, B_index, B_value, nnzB, buffer, addC, nnzC, C_index, C_value);
}
DLLEXP cudaError vecSpAddZ (const int* A_index, const cuDoubleComplex* A_value, const size_t nnzA,
							const int* B_index, const cuDoubleComplex* B_value, const size_t nnzB,
							void* buffer, size_t& nnzC, int*& C_index, cuDoubleComplex*& C_value)
{
	return vecSpAdd(A_index, A_value, nnzA, B_index, B_value, nnzB, buffer, addZ, nnzC, C_index, C_value);
}

DLLEXP cudaError vecSpAxpyS(const int* A_index, const float* A_value, const size_t nnzA,
							const float alpha, const int* B_index, const float* B_value, const size_t nnzB,
							void* buffer, size_t& nnzC, int*& C_index, float*& C_value)
{
	return vecSpAdd(A_index, A_value, nnzA, B_index, B_value, nnzB, buffer, floatAxpy(alpha), nnzC, C_index, C_value);
}
DLLEXP cudaError vecSpAxpyD(const int* A_index, const double* A_value, const size_t nnzA,
							const double alpha, const int* B_index, const double* B_value, const size_t nnzB,
							void* buffer, size_t& nnzC, int*& C_index, double*& C_value)
{
	return vecSpAdd(A_index, A_value, nnzA, B_index, B_value, nnzB, buffer, doubleAxpy(alpha), nnzC, C_index, C_value);
}
DLLEXP cudaError vecSpAxpyC(const int* A_index, const cuFloatComplex* A_value, const size_t nnzA,
							const cuFloatComplex alpha, const int* B_index, const cuFloatComplex* B_value, const size_t nnzB,
							void* buffer, size_t& nnzC, int*& C_index, cuFloatComplex*& C_value)
{
	return vecSpAdd(A_index, A_value, nnzA, B_index, B_value, nnzB, buffer, floatComplexAxpy(alpha), nnzC, C_index, C_value);
}
DLLEXP cudaError vecSpAxpyZ(const int* A_index, const cuDoubleComplex* A_value, const size_t nnzA,
							const cuDoubleComplex alpha, const int* B_index, const cuDoubleComplex* B_value, const size_t nnzB,
							void* buffer, size_t& nnzC, int*& C_index, cuDoubleComplex*& C_value)
{
	return vecSpAdd(A_index, A_value, nnzA, B_index, B_value, nnzB, buffer, doubleComplexAxpy(alpha), nnzC, C_index, C_value);
}
END_EXTERN_C
#pragma endregion


#pragma region dense vector added by sparse
__global__ void KerDnVecAddSpS(float* dense, const size_t nnz, const float* sparse, const int* index)
{
	unsigned int idx = threadIdx.x + blockIdx.x * blockDim.x;
	if (idx < nnz)
	{
		dense[index[idx]] += sparse[idx];
	}
}
__global__ void KerDnVecAddSpD(double* dense, const size_t nnz, const double* sparse, const int* index)
{
	unsigned int idx = threadIdx.x + blockIdx.x * blockDim.x;
	if (idx < nnz)
	{
		dense[index[idx]] += sparse[idx];
	}
}
__global__ void KerDnVecAddSpC(cuFloatComplex* dense, const size_t nnz, const cuFloatComplex* sparse, const int* index)
{
	unsigned int idx = threadIdx.x + blockIdx.x * blockDim.x;
	if (idx < nnz)
	{
		size_t pos = index[idx];
		dense[pos] = cuCaddf(dense[pos], sparse[idx]);
	}
}
__global__ void KerDnVecAddSpZ(cuDoubleComplex* dense, const size_t nnz, const cuDoubleComplex* sparse, const int* index)
{
	unsigned int idx = threadIdx.x + blockIdx.x * blockDim.x;
	if (idx < nnz)
	{
		size_t pos = index[idx];
		dense[pos] = cuCadd(dense[pos], sparse[idx]);
	}
}

__global__ void KerAxpyiS(float* dense, const size_t nnz, const float a, const float* sparse, const int* index)
{
	unsigned int idx = threadIdx.x + blockIdx.x * blockDim.x;
	if (idx < nnz)
	{
		dense[index[idx]] += a * sparse[idx];
	}
}
__global__ void KerAxpyiD(double* dense, const size_t nnz, const double a, const double* sparse, const int* index)
{
	unsigned int idx = threadIdx.x + blockIdx.x * blockDim.x;
	if (idx < nnz)
	{
		dense[index[idx]] += a * sparse[idx];
	}
}
__global__ void KerAxpyiC(cuFloatComplex* dense, const size_t nnz, const cuFloatComplex a,
					const cuFloatComplex* sparse, const int* index)
{
	unsigned int idx = threadIdx.x + blockIdx.x * blockDim.x;
	if (idx < nnz)
	{
		size_t pos = index[idx];
		dense[pos] = cuCaddf(dense[pos], cuCmulf(a, sparse[idx]));
	}
}
__global__ void KerAxpyiZ(cuDoubleComplex* dense, const size_t nnz, const cuDoubleComplex a,
					const cuDoubleComplex* sparse, const int* index)
{
	unsigned int idx = threadIdx.x + blockIdx.x * blockDim.x;
	if (idx < nnz)
	{
		size_t pos = index[idx];
		dense[pos] = cuCadd(dense[pos], cuCmul(a, sparse[idx]));
	}
}

template <typename T, typename Kernel>
cudaError vecDnAddSp(T* dense, const size_t nnz, const T* sparse, const int* index, Kernel ker)
{
	int blockSize, minGridSize, gridSize;
	cudaError err = cudaOccupancyMaxPotentialBlockSize(&minGridSize, &blockSize, ker, 0, nnz);
	if (err != 0) return err;
	// Round up according to array size
	gridSize = (nnz + blockSize - 1) / blockSize;
	ker<<<gridSize, blockSize>>>(dense, nnz, sparse, index);
	err = cudaDeviceSynchronize();
	return err;
}

template <typename T, typename Kernel>
cudaError vecAxpyi(T* dense, const size_t nnz, const T alpha, const T* sparse, const int* index, Kernel ker)
{
	int blockSize, minGridSize, gridSize;
	cudaError err = cudaOccupancyMaxPotentialBlockSize(&minGridSize, &blockSize, ker, 0, nnz);
	if (err != 0) return err;
	// Round up according to array size
	gridSize = (nnz + blockSize - 1) / blockSize;
	ker<<<gridSize, blockSize>>>(dense, nnz, alpha, sparse, index);
	err = cudaDeviceSynchronize();
	return err;
}

EXTERN_C
DLLEXP cudaError vecDnAddSpS(float* dense, const size_t nnz, const float* sparse, const int* index)
{
	return vecDnAddSp(dense, nnz, sparse, index, KerDnVecAddSpS);
}
DLLEXP cudaError vecDnAddSpD(double* dense, const size_t nnz, const double* sparse, const int* index)
{
	return vecDnAddSp(dense, nnz, sparse, index, KerDnVecAddSpD);
}
DLLEXP cudaError vecDnAddSpC(cuFloatComplex* dense, const size_t nnz, const cuFloatComplex* sparse, const int* index)
{
	return vecDnAddSp(dense, nnz, sparse, index, KerDnVecAddSpC);
}
DLLEXP cudaError vecDnAddSpZ(cuDoubleComplex* dense, const size_t nnz, const cuDoubleComplex* sparse, const int* index)
{
	return vecDnAddSp(dense, nnz, sparse, index, KerDnVecAddSpZ);
}

DLLEXP cudaError vecAxpyiS(float* dense, const size_t nnz, const float a, const float* sparse, const int* index)
{
	return vecAxpyi(dense, nnz, a, sparse, index, KerAxpyiS);
}
DLLEXP cudaError vecAxpyiD(double* dense, const size_t nnz, const double a, const double* sparse, const int* index)
{
	return vecAxpyi(dense, nnz, a, sparse, index, KerAxpyiD);
}
DLLEXP cudaError vecAxpyiC(cuFloatComplex* dense, const size_t nnz, const cuFloatComplex a,
					const cuFloatComplex* sparse, const int* index)
{
	return vecAxpyi(dense, nnz, a, sparse, index, KerAxpyiC);
}
DLLEXP cudaError vecAxpyiZ(cuDoubleComplex* dense, const size_t nnz, const cuDoubleComplex a,
					const cuDoubleComplex* sparse, const int* index)
{
	return vecAxpyi(dense, nnz, a, sparse, index, KerAxpyiZ);
}
END_EXTERN_C
#pragma endregion



#pragma region size_t array to or from int array
__global__ void Kerlong2int(const long long* index, int* outIdx, const size_t nnz)
{
	unsigned int idx = threadIdx.x + blockIdx.x * blockDim.x;
	if (idx < nnz)
		outIdx[idx] = (int)index[idx];
}

__global__ void Kerint2long(long long* outIdx, const int* index, const size_t nnz)
{
	unsigned int idx = threadIdx.x + blockIdx.x * blockDim.x;
	if (idx < nnz)
		outIdx[idx] = index[idx];
}

EXTERN_C
DLLEXP cudaError long2int(const long long* sizeTIdx, int* intIdx, const size_t N)
{
	int blockSize, minGridSize, gridSize;
	cudaError err = cudaOccupancyMaxPotentialBlockSize(&minGridSize, &blockSize, Kerlong2int, 0, N);
	if (err != 0) return err;
	// Round up according to array size
	gridSize = (N + blockSize - 1) / blockSize;
	Kerlong2int << <gridSize, blockSize >> > (sizeTIdx, intIdx, N);
	err = cudaDeviceSynchronize();
	return err;
}
DLLEXP cudaError int2long(long long* sizeTIdx, const int* intIdx, const size_t N)
{
	int blockSize, minGridSize, gridSize;
	cudaError err = cudaOccupancyMaxPotentialBlockSize(&minGridSize, &blockSize, Kerint2long, 0, N);
	if (err != 0) return err;
	// Round up according to array size
	gridSize = (N + blockSize - 1) / blockSize;
	Kerint2long << <gridSize, blockSize >> > (sizeTIdx, intIdx, N);
	err = cudaDeviceSynchronize();
	return err;
}
END_EXTERN_C
#pragma endregion
