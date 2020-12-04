// CUDA includes
#include "cuda_runtime.h"
#include "device_launch_parameters.h"
#include "cublas.h"
#include "math.h"
#include "cuComplex.h"

#include "cusparse.h"
#include "curand.h"

#include <iostream>
#include <chrono>

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
#include "macro.h"



#pragma region vector sum sumVec
struct floatComplexAdd
{
	__host__ __device__ cuFloatComplex operator()(const cuFloatComplex x, const cuFloatComplex y) const
	{
		return cuCaddf(x, y);
	}
};
floatComplexAdd addC;
cuFloatComplex zeroC = make_cuFloatComplex(0, 0);

struct doubleComplexAdd
{
	__host__ __device__ cuDoubleComplex operator()(const cuDoubleComplex x, const cuDoubleComplex y) const
	{
		return cuCadd(x, y);
	}
};
doubleComplexAdd addZ;
cuDoubleComplex zeroZ = make_cuDoubleComplex(0, 0);


template <typename Iterator>
class strided_range
{
public:

	typedef typename thrust::iterator_difference<Iterator>::type difference_type;

	struct stride_functor : public thrust::unary_function<difference_type, difference_type>
	{
		difference_type stride;

		stride_functor(difference_type stride)
			: stride(stride) {}

		__host__ __device__ difference_type operator()(const difference_type& i) const
		{
			return stride * i;
		}
	};

	typedef typename thrust::counting_iterator<difference_type>                   CountingIterator;
	typedef typename thrust::transform_iterator<stride_functor, CountingIterator> TransformIterator;
	typedef typename thrust::permutation_iterator<Iterator, TransformIterator>     PermutationIterator;

	// type of the strided_range iterator
	typedef PermutationIterator iterator;

	// construct strided_range for the range [first,last)
	strided_range(Iterator first, Iterator last, difference_type stride)
		: first(first), last(last), stride(stride) {}

	iterator begin(void) const
	{
		return PermutationIterator(first, TransformIterator(CountingIterator(0), stride_functor(stride)));
	}

	iterator end(void) const
	{
		return begin() + ((last - first) + (stride - 1)) / stride;
	}

protected:
	Iterator first;
	Iterator last;
	difference_type stride;
};

EXTERN_C
DLLEXP float sumVecS(const float* v, const size_t len, const unsigned int stride)
{
	if (stride == 1)
		return thrust::reduce(thrust::cuda::par, v, v + len);
	strided_range<const float*> s(v, v + len * stride, stride);
	return thrust::reduce(thrust::cuda::par, s.begin(), s.end());
}

DLLEXP double sumVecD(const double* v, const size_t len, const unsigned int stride)
{
	if (stride == 1)
		return thrust::reduce(thrust::cuda::par, v, v + len);
	strided_range<const double*> s(v, v + len * stride, stride);
	return thrust::reduce(thrust::cuda::par, s.begin(), s.end());
}

DLLEXP cuFloatComplex sumVecC(const cuFloatComplex* v, const size_t len, const unsigned int stride)
{
	if (stride == 1)
		return thrust::reduce(thrust::cuda::par, v, v + len, zeroC, addC);
	strided_range<const cuFloatComplex*> s(v, v + len * stride, stride);
	return thrust::reduce(thrust::cuda::par, s.begin(), s.end(), zeroC, addC);
}

DLLEXP cuDoubleComplex sumVecZ(const cuDoubleComplex* v, const size_t len, const unsigned int stride)
{
	if (stride == 1)
		return thrust::reduce(thrust::cuda::par, v, v + len, zeroZ, addZ);
	strided_range<const cuDoubleComplex*> s(v, v + len * stride, stride);
	return thrust::reduce(thrust::cuda::par, s.begin(), s.end(), zeroZ, addZ);
}
END_EXTERN_C
#pragma endregion


#pragma region set array values at positions setArrOne
__global__ void KerSetOneS(float* arr, const float v, const int* pos, size_t posN)
{
	size_t x = threadIdx.x + (size_t)blockIdx.x * (size_t)blockDim.x;
	if (x < posN)
		arr[pos[x]] = v;
}
__global__ void KerSetOneD(double* arr, const double v, const int* pos, size_t posN)
{
	size_t x = threadIdx.x + (size_t)blockIdx.x * (size_t)blockDim.x;
	if (x < posN)
		arr[pos[x]] = v;
}
__global__ void KerSetOneC(cuFloatComplex* arr, const cuFloatComplex v, const int* pos, size_t posN)
{
	size_t x = threadIdx.x + (size_t)blockIdx.x * (size_t)blockDim.x;
	if (x < posN)
		arr[pos[x]] = v;
}
__global__ void KerSetOneZ(cuDoubleComplex* arr, const cuDoubleComplex v, const int* pos, size_t posN)
{
	size_t x = threadIdx.x + (size_t)blockIdx.x * (size_t)blockDim.x;
	if (x < posN)
		arr[pos[x]] = v;
}


template <typename T, typename Kernel>
cudaError setArrOne(T* dst, const T value, const int* pos, const size_t posN, Kernel ker)
{
	int blockSize, minGridSize, gridSize;
	cudaError err = cudaOccupancyMaxPotentialBlockSize(&minGridSize, &blockSize, ker, 0, posN);
	if (err != 0) return err;
	// Round up according to array size
	gridSize = (posN + blockSize - 1) / blockSize;
	ker<<<gridSize, blockSize>>>(dst, value, pos, posN);
	err = cudaDeviceSynchronize();
	return err;
}

EXTERN_C
DLLEXP cudaError setArrOneS(float* a, const float v, const int* pos, const size_t posN)
{
	return setArrOne(a, v, pos, posN, KerSetOneS);
}
DLLEXP cudaError setArrOneD(double* a, const double v, const int* pos, const size_t posN)
{
	return setArrOne(a, v, pos, posN, KerSetOneD);
}
DLLEXP cudaError setArrOneC(cuFloatComplex* a, const cuFloatComplex v, const int* pos, const size_t posN)
{
	return setArrOne(a, v, pos, posN, KerSetOneC);
}
DLLEXP cudaError setArrOneZ(cuDoubleComplex* a, const cuDoubleComplex v, const int* pos, const size_t posN)
{
	return setArrOne(a, v, pos, posN, KerSetOneZ);
}
END_EXTERN_C
#pragma endregion


/* 
#pragma region vector scatter (sparse to dense or set at positions) vecScatter
__global__ void KerSetArrS(float* arr, const float* vs, const int* pos, size_t N)
{
	size_t x = threadIdx.x + (size_t)blockIdx.x * (size_t)blockDim.x;
	if (x < N)
		arr[pos[x]] = vs[x];
}
__global__ void KerSetArrD(double* arr, const double* vs, const int* pos, size_t N)
{
	size_t x = threadIdx.x + (size_t)blockIdx.x * (size_t)blockDim.x;
	if (x < N)
		arr[pos[x]] = vs[x];
}
__global__ void KerSetArrC(cuFloatComplex* arr, const cuFloatComplex* vs, const int* pos, size_t N)
{
	size_t x = threadIdx.x + (size_t)blockIdx.x * (size_t)blockDim.x;
	if (x < N)
		arr[pos[x]] = vs[x];
}
__global__ void KerSetArrZ(cuDoubleComplex* arr, const cuDoubleComplex* vs, const int* pos, size_t N)
{
	size_t x = threadIdx.x + (size_t)blockIdx.x * (size_t)blockDim.x;
	if (x < N)
		arr[pos[x]] = vs[x];
}

template <typename T, typename Kernel>
cudaError vecScatter(T* dst, const T* value, const int* pos, const size_t posN, Kernel ker)
{
	int blockSize, minGridSize, gridSize;
	cudaError err = cudaOccupancyMaxPotentialBlockSize(&minGridSize, &blockSize, ker, 0, posN);
	if (err != 0) return err;
	// Round up according to array size
	gridSize = (posN + blockSize - 1) / blockSize;
	ker << <gridSize, blockSize >> > (dst, value, pos, posN);
	err = cudaDeviceSynchronize();
	return err;
}


EXTERN_C
DLLEXP cudaError vecScatterS(float* a, const float* v, const int* pos, const size_t posN)
{
	return vecScatter(a, v, pos, posN, KerSetArrS);
}
DLLEXP cudaError vecScatterD(double* a, const double* v, const int* pos, const size_t posN)
{
	return vecScatter(a, v, pos, posN, KerSetArrD);
}
DLLEXP cudaError vecScatterC(cuFloatComplex* a, const cuFloatComplex* v, const int* pos, const size_t posN)
{
	return vecScatter(a, v, pos, posN, KerSetArrC);
}
DLLEXP cudaError vecScatterZ(cuDoubleComplex* a, const cuDoubleComplex* v, const int* pos, const size_t posN)
{
	return vecScatter(a, v, pos, posN, KerSetArrZ);
}
END_EXTERN_C
#pragma endregion


#pragma region vector gather (dense to sparse or permutation) vecGather
__global__ void KerCpyArrS(float* dst, const float* src, const int* pos, size_t N)
{
	size_t x = threadIdx.x + (size_t)blockIdx.x * (size_t)blockDim.x;
	if (x < N)
		dst[x] = src[pos[x]];
}

__global__ void KerCpyArrD(double* dst, const double* src, const int* pos, size_t N)
{
	size_t x = threadIdx.x + (size_t)blockIdx.x * (size_t)blockDim.x;
	if (x < N)
		dst[x] = src[pos[x]];
}

__global__ void KerCpyArrC(cuFloatComplex* dst, const cuFloatComplex* src, const int* pos, size_t N)
{
	size_t x = threadIdx.x + (size_t)blockIdx.x * (size_t)blockDim.x;
	if (x < N)
		dst[x] = src[pos[x]];
}

__global__ void KerCpyArrZ(cuDoubleComplex* dst, const cuDoubleComplex* src, const int* pos, size_t N)
{
	size_t x = threadIdx.x + (size_t)blockIdx.x * (size_t)blockDim.x;
	if (x < N)
		dst[x] = src[pos[x]];
}


template <typename T, typename Kernel>
cudaError vecGather(T* dst, const T* src, const int* pos, const size_t posN, Kernel ker)
{
	int blockSize, minGridSize, gridSize;
	cudaError err = cudaOccupancyMaxPotentialBlockSize(&minGridSize, &blockSize, ker, 0, posN);
	if (err != 0) return err;
	// Round up according to array size
	gridSize = (posN + blockSize - 1) / blockSize;
	ker << <gridSize, blockSize >> > (dst, src, pos, posN);
	err = cudaDeviceSynchronize();
	return err;
}


EXTERN_C
DLLEXP cudaError vecGatherS(float* dst, const float* src, const int* pos, const size_t posN)
{
	return vecGather(dst, src, pos, posN, KerCpyArrS);
}
DLLEXP cudaError vecGatherD(double* dst, const double* src, const int* pos, const size_t posN)
{
	return vecGather(dst, src, pos, posN, KerCpyArrD);
}
DLLEXP cudaError vecGatherC(cuFloatComplex* dst, const cuFloatComplex* src, const int* pos, const size_t posN)
{
	return vecGather(dst, src, pos, posN, KerCpyArrC);
}
DLLEXP cudaError vecGatherZ(cuDoubleComplex* dst, const cuDoubleComplex* src, const int* pos, const size_t posN)
{
	return vecGather(dst, src, pos, posN, KerCpyArrZ);
}
END_EXTERN_C
#pragma endregion
*/

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


/*
// thrust dot is 7%~9% slower
int main()
{
	cublasHandle_t handle = NULL;
	cublasCreate_v2(&handle);
	curandGenerator_t generator = NULL;
	curandCreateGenerator(&generator, CURAND_RNG_PSEUDO_DEFAULT);

	const size_t N = 0x1000000;
	cuDoubleComplex* v;
	cuDoubleComplex* w;
	cudaMalloc(&v, N * sizeof(cuDoubleComplex));
	curandGenerateUniformDouble(generator, (double*)v, N * 2);
	cudaMalloc(&w, N * sizeof(cuDoubleComplex));
	curandGenerateUniformDouble(generator, (double*)w, N * 2);

	auto t1 = std::chrono::high_resolution_clock::now();
	cuDoubleComplex res;
	for (size_t i = 0; i < 100; i++)
		cublasZdotc_v2(handle, (int)N, v, 1, w, 1, &res);
	auto t2 = std::chrono::high_resolution_clock::now();
	std::cout << cuCabs(res) << " took "
		<< std::chrono::duration_cast<std::chrono::microseconds>(t2 - t1).count()
		<< " microseconds" << std::endl;

	t1 = std::chrono::high_resolution_clock::now();
	for (size_t i = 0; i < 100; i++)
		res = thrust::inner_product(thrust::cuda::par, v, v + N, w, cuDoubleComplex(), addZ, dotZ);
	t2 = std::chrono::high_resolution_clock::now();
	std::cout << cuCabs(res) << " took "
		<< std::chrono::duration_cast<std::chrono::microseconds>(t2 - t1).count()
		<< " microseconds" << std::endl;

	return 0;
}
*/


/* not a good option
#pragma region sparse vector dot vecSpDot
EXTERN_C
DLLEXP size_t vecSpOverlapN(const size_t nnzA, const size_t nnzB, size_t& bufferSize)
{
	return sizeof(size_t) * (nnzA > nnzB ? nnzA : nnzB);
}

__host__ __device__ DLLEXP
size_t vecSpOverlapN(const int* A_index, const size_t nnzA,
					 const int* B_index, const size_t nnzB, int* buffer)
{
	if (nnzA == 0 || nnzB == 0)
		return 0;
	thrust::device_ptr<size_t> temp = thrust::device_pointer_cast<size_t>(buffer);
	auto tempEnd = thrust::set_intersection(thrust::cuda::par, A_index, A_index + nnzA, B_index, B_index + nnzB, temp);
	return tempEnd - temp;
}
END_EXTERN_C


template <typename T, typename MulFunc, typename AddFunc>
__host__ __device__ cudaError
vecSpDot(const int* A_index, const T* A_value, const size_t nnzA,
		 const int* B_index, const T* B_value, const size_t nnzB,
		 void* buffer, MulFunc mul, AddFunc add, const T zeroT, T& dot)
{
	// early return
	if (nnzA == 0 || nnzB == 0) return T(); // 0

	// actual calculation
	const int nnz = nnzA + nnzB;

	// allocate storage for the combined contents of sparse vectors A and B
	thrust::device_ptr<size_t> temp_index = thrust::device_pointer_cast<size_t>((int*)buffer);
	thrust::device_ptr<T> temp_value = thrust::device_pointer_cast<T>((T*)(nnz + (int*)buffer));

	// merge A and B by index
	thrust::merge_by_key(thrust::cuda::par, A_index, A_index + nnzA, B_index, B_index + nnzB, A_value, B_value, temp_index, temp_value);

	// make zip
	auto begin = thrust::make_zip_iterator(thrust::make_tuple(temp_value, temp_index));
	auto end = thrust::make_zip_iterator(thrust::make_tuple(temp_value + nnz - 1, temp_index + nnz - 1));
	auto begin2 = thrust::make_zip_iterator(thrust::make_tuple(temp_value + 1, temp_index + 1));

	// inner product
	dot = thrust::inner_product(thrust::cuda::par, begin, end, begin2, zeroT, add, mul);

	return err;
}

EXTERN_C
DLLEXP cudaError vecSpDotBuffer(const size_t nnzA, const size_t nnzB, const cudaDataType type, size_t& bufferSize)
{
	size_t N = nnzA + nnzB;
	size_t res;
	switch (type)
	{
	case CUDA_R_32F:
		res = sizeof(float) * N + sizeof(size_t) * N;
	case CUDA_C_32F:
		res = sizeof(cuFloatComplex) * N + sizeof(size_t) * N;
	case CUDA_R_64F:
		res = sizeof(double) * N + sizeof(size_t) * N;
	case CUDA_C_64F:
		res = sizeof(cuDoubleComplex) * N + sizeof(size_t) * N;
	default:
		return cudaErrorNotSupported;
	}
	bufferSize = res;
	return cudaSuccess;
}

DLLEXP cudaError vecSpDotS(const int* A_index, const float* A_value, const int nnzA,
	const int* B_index, const float* B_value, const int nnzB, float& dot)
{
	return vecSpDot<float>(A_index, A_value, nnzA, B_index, B_value, nnzB, dot);
}
DLLEXP cudaError vecSpDotD(const int* A_index, const double* A_value, const int nnzA,
	const int* B_index, const double* B_value, const int nnzB, double& dot)
{
	return vecSpDot<double>(A_index, A_value, nnzA, B_index, B_value, nnzB, dot);
}
DLLEXP cudaError vecSpDotC(const int* A_index, const cuFloatComplex* A_value, const int nnzA,
	const int* B_index, const cuFloatComplex* B_value, const int nnzB, cuFloatComplex& dot)
{
	return vecSpDot<cuFloatComplex>(A_index, A_value, nnzA, B_index, B_value, nnzB, dot);
}
DLLEXP cudaError vecSpDotZ(const int* A_index, const cuDoubleComplex* A_value, const int nnzA,
	const int* B_index, const cuDoubleComplex* B_value, const int nnzB, cuDoubleComplex& dot)
{
	return vecSpDot<cuDoubleComplex>(A_index, A_value, nnzA, B_index, B_value, nnzB, dot);
}
END_EXTERN_C
#pragma endregion
*/