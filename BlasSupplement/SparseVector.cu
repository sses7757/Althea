#include "macro.h"


#pragma region set array values at positions
template <typename T>
__inline__ void vectorSetValuesAt(T* dst, const T value, const int* pos, const size_t posN)
{
	auto iter = thrust::make_permutation_iterator(dst, pos);
	thrust::fill(THRUST_PAR, iter, iter + posN, value);
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
DLLEXP void vecSetValAtC(complexFloat* a, const complexFloat v, const int* pos, const size_t posN)
{
	vectorSetValuesAt(a, v, pos, posN);
}
DLLEXP void vecSetValAtZ(complexDouble* a, const complexDouble v, const int* pos, const size_t posN)
{
	vectorSetValuesAt(a, v, pos, posN);
}
END_EXTERN_C
#pragma endregion


#pragma region dense vector prune to sparse vector
template <typename T, typename U>
struct floatAboveThreshold_functor
{
	const U threshold;
	floatAboveThreshold_functor(const U t) : threshold(t) {}

	__host__ __device__ bool operator()(const T x) const
	{
		return std::abs(x) > threshold;
	}
};

// dense vector prune to sparse vector -- get buffer size
extern "C" DLLEXP size_t vecPruneBuffer(const size_t N, const DataType type)
{
	size_t res = sizeof(int) * N; // max size for possible indices
	int sizeofType = (int)((type & DataType::ByteMask) >> DataType::ByteMaskStart);
	res += sizeofType * N; // size for temporary values
	return res;
}

// dense vector prune to sparse vector -- get non-zeros
template <typename T, typename U>
size_t vecPruneNonZeros(const T* v, const U threshold, const size_t N, void* buffer)
{
	// create result container
	int* idxOut = (int*)buffer;
	T* valOut = (T*)(N + (int*)buffer);

	// make zip
	auto zipBegin = thrust::make_zip_iterator(thrust::make_tuple(thrust::make_counting_iterator(0), v));
	auto resultBegin = thrust::make_zip_iterator(thrust::make_tuple(idxOut, valOut));

	// copy_if to get sparse indexes
	auto resultEnd = thrust::copy_if(THRUST_PAR, zipBegin, zipBegin + N, v, resultBegin, floatAboveThreshold_functor<T, U>(threshold));
	return resultEnd - resultBegin;
}

EXTERN_C
DLLEXP size_t vecPruneNnzS(const float* v, const float threshold, const size_t N, void* buffer)
{
	return vecPruneNonZeros(v, threshold, N, buffer);
}
DLLEXP size_t vecPruneNnzD(const double* v, const float threshold, const size_t N, void* buffer)
{
	return vecPruneNonZeros(v, (double)threshold, N, buffer);
}
DLLEXP size_t vecPruneNnzC(const complexFloat* v, const float threshold, const size_t N, void* buffer)
{
	return vecPruneNonZeros(v, threshold, N, buffer);
}
DLLEXP size_t vecPruneNnzZ(const complexDouble* v, const float threshold, const size_t N, void* buffer)
{
	return vecPruneNonZeros(v, (double)threshold, N, buffer);
}
END_EXTERN_C

// dense vector prune to sparse vector -- calculate
template <typename T>
ERROR_RETURN vecPruneCalculate(const size_t N, const void* buffer, size_t nnz, int* indexOut, T* valueOut)
{
	// get result container from buffer
	int* idxOut = N + (int*)buffer;
	T* valOut = (T*)(2 * N + (int*)buffer);

	// copy to output arrays
#ifdef CPU
	memcpy(indexOut, idxOut, sizeof(int) * nnz);
	memcpy(valueOut, valOut, sizeof(T) * nnz);
#else
	cudaError err = cudaMemcpy(indexOut, idxOut, sizeof(int) * nnz, cudaMemcpyDeviceToDevice);
	if (err != 0) return err;
	err = cudaMemcpy(valueOut, valOut, sizeof(T) * nnz, cudaMemcpyDeviceToDevice);
	if (err != 0) return err;
	// return
	return err;
#endif // CPU
}

EXTERN_C
DLLEXP ERROR_RETURN vecPruneCalS(const size_t N, void* buffer, size_t nnz, int* indexOut, float* valueOut)
{
	return vecPruneCalculate(N, buffer, nnz, indexOut, valueOut);
}
DLLEXP ERROR_RETURN vecPruneCalD(const size_t N, void* buffer, size_t nnz, int* indexOut, double* valueOut)
{
	return vecPruneCalculate(N, buffer, nnz, indexOut, valueOut);
}
DLLEXP ERROR_RETURN vecPruneCalC(const size_t N, void* buffer, size_t nnz, int* indexOut, complexFloat* valueOut)
{
	return vecPruneCalculate(N, buffer, nnz, indexOut, valueOut);
}
DLLEXP ERROR_RETURN vecPruneCalZ(const size_t N, void* buffer, size_t nnz, int* indexOut, complexDouble* valueOut)
{
	return vecPruneCalculate(N, buffer, nnz, indexOut, valueOut);
}
END_EXTERN_C
#pragma endregion


#pragma region sparse vector element-wise multipilied or divided by dense vector
template<typename T>
void vectorSparseMultipliedDividedByDense(T* sparse, const int* index, const size_t nnz, const T* dense, bool multiply)
{
	if (multiply)
	{
		thrust::transform(THRUST_PAR, sparse, sparse + nnz, thrust::make_permutation_iterator(dense, index), sparse, thrust::multiplies<T>());
	}
	else
	{
		thrust::transform(THRUST_PAR, sparse, sparse + nnz, thrust::make_permutation_iterator(dense, index), sparse, thrust::divides<T>());
	}
}

EXTERN_C
DLLEXP void vecSpMulDivDnS(float* sparse, const int* index, const size_t nnz, const float* dense, bool multiply)
{
	vectorSparseMultipliedDividedByDense(sparse, index, nnz, dense, multiply);
}
DLLEXP void vecSpMulDivDnD(double* sparse, const int* index, const size_t nnz, const double* dense, bool multiply)
{
	vectorSparseMultipliedDividedByDense(sparse, index, nnz, dense, multiply);
}
DLLEXP void vecSpMulDivDnC(complexFloat* sparse, const int* index, const size_t nnz, const complexFloat* dense, bool multiply)
{
	vectorSparseMultipliedDividedByDense(sparse, index, nnz, dense, multiply);
}
DLLEXP void vecSpMulDivDnZ(complexDouble* sparse, const int* index, const size_t nnz, const complexDouble* dense, bool multiply)
{
	vectorSparseMultipliedDividedByDense(sparse, index, nnz, dense, multiply);
}
END_EXTERN_C
#pragma endregion


#pragma region sparse vector add sparse vector vecSpAdd
// sparse vector add another sparse vector -- get buffer size
extern "C" DLLEXP size_t vecSpAddBuffer(const size_t nnzA, const size_t nnzB, const DataType type)
{
	size_t N = nnzA + nnzB;
	size_t res = sizeof(int) * N; // size for temporary indices
	int sizeofType = (int)((type & DataType::ByteMask) >> DataType::ByteMaskStart);
	res += sizeofType * N; // size for temporary values
	return res;
}

template <typename T>
struct floatMultiplyScalar_functor
{
	const T scalar;
	floatMultiplyScalar_functor(const T s) : scalar(s) {}

	__host__ __device__ T operator()(const T x) const
	{
		return x * scalar;
	}
};

template<typename T>
struct notEqualAsInt_functor
{
	__host__ __device__ int operator()(const T& lhs, const T& rhs) const { return lhs == rhs ? 0 : 1; }
};

// sparse vector add another sparse vector -- get non-zeros, 'alpha' is the number to multiply to each value of B
template <typename T>
size_t vectorSparseAddGetNonzero(const int* indA, const T* valA, const size_t nnzA, const int* indB, const T* valB, const size_t nnzB, const T alpha, void* buffer)
{
	size_t nnz = nnzA + nnzB;
	// get storage from buffer for the combined contents of sparse vectors A and B
	int* temp_index = (int*)buffer;
	T* temp_value = (T*)(nnz + (int*)buffer);

	// merge A and B by index
	if (alpha == 1)
	{
		thrust::merge_by_key(THRUST_PAR, indA, indA + nnzA, indB, indB + nnzB, valA, valB, temp_index, temp_value);
	}
	else
	{
		auto alphaMultiplyB = thrust::make_transform_iterator(valB, floatMultiplyScalar_functor<T>(alpha));
		thrust::merge_by_key(THRUST_PAR, indA, indA + nnzA, indB, indB + nnzB, valA, alphaMultiplyB, temp_index, temp_value);
	}

	// compute number of unique indices, must larger than 0
	size_t nnzC = thrust::inner_product(THRUST_PAR, temp_index, temp_index + nnz - 1, temp_index + 1, int(0), thrust::plus<int>(), notEqualAsInt_functor<int>());
	nnzC += 1;

	// return
	return nnzC;
}

EXTERN_C
DLLEXP size_t vecSpAddNnzS(const int* indA, const float* valA, const size_t nnzA, const int* indB, const float* valB, const size_t nnzB, const float alpha, void* buffer)
{
	return vectorSparseAddGetNonzero(indA, valA, nnzA, indB, valB, nnzB, alpha, buffer);
}
DLLEXP size_t vecSpAddNnzD(const int* indA, const double* valA, const size_t nnzA, const int* indB, const double* valB, const size_t nnzB, const double alpha, void* buffer)
{
	return vectorSparseAddGetNonzero(indA, valA, nnzA, indB, valB, nnzB, alpha, buffer);
}
DLLEXP size_t vecSpAddNnzC(const int* indA, const complexFloat* valA, const size_t nnzA, const int* indB, const complexFloat* valB, const size_t nnzB, const complexFloat alpha, void* buffer)
{
	return vectorSparseAddGetNonzero(indA, valA, nnzA, indB, valB, nnzB, alpha, buffer);
}
DLLEXP size_t vecSpAddNnzZ(const int* indA, const complexDouble* valA, const size_t nnzA, const int* indB, const complexDouble* valB, const size_t nnzB, const complexDouble alpha, void* buffer)
{
	return vectorSparseAddGetNonzero(indA, valA, nnzA, indB, valB, nnzB, alpha, buffer);
}
END_EXTERN_C

// sparse vector add another sparse vector -- calculate
template <typename T>
void vectorSparseAddCalculate(size_t nnzAB, void* buffer, size_t nnzC, int* C_index, T* C_value)
{
	// get storage from buffer for the combined contents of sparse vectors A and B
	int* temp_index = (int*)buffer;
	T* temp_value = (T*)(nnzAB + (int*)buffer);

	// sum values with the same index
	thrust::reduce_by_key(THRUST_PAR, temp_index, temp_index + nnzAB, temp_value, C_index, C_value, thrust::equal_to<int>(), thrust::plus<T>());
}

EXTERN_C
DLLEXP void vecSpAddCalS(size_t nnzAB, void* buffer, size_t nnzC, int* C_index, float* C_value)
{
	return vectorSparseAddCalculate(nnzAB, buffer, nnzC, C_index, C_value);
}
DLLEXP void vecSpAddCalS(size_t nnzAB, void* buffer, size_t nnzC, int* C_index, float* C_value)
{
	return vectorSparseAddCalculate(nnzAB, buffer, nnzC, C_index, C_value);
}
DLLEXP void vecSpAddCalS(size_t nnzAB, void* buffer, size_t nnzC, int* C_index, float* C_value)
{
	return vectorSparseAddCalculate(nnzAB, buffer, nnzC, C_index, C_value);
}
DLLEXP void vecSpAddCalS(size_t nnzAB, void* buffer, size_t nnzC, int* C_index, float* C_value)
{
	return vectorSparseAddCalculate(nnzAB, buffer, nnzC, C_index, C_value);
}
END_EXTERN_C
#pragma endregion


#pragma region dense vector added by sparse
// return alpha * x + y
template <typename T>
struct floatFMA_functor
{
	const T alpha;
	floatFMA_functor(const T a) : alpha(a) {}

	__host__ __device__ T operator()(const T x, const T y) const
	{
		return std::fma(alpha, x, y);
	}
};

// dense[index[i]] = sparse[i] * alpha + dense[index[i]]
template <typename T>
void vectorDenseAddBySparse(T* dense, const T* sparse, const int* index, const size_t nnz, const T alpha)
{
	auto densePerm = thrust::make_permutation_iterator(dense, index);
	if (alpha == 1)
	{
		thrust::transform(THRUST_PAR, sparse, sparse + nnz, densePerm, densePerm, thrust::plus<T>());
	}
	else
	{
		thrust::transform(THRUST_PAR, sparse, sparse + nnz, densePerm, densePerm, floatFMA_functor<T>(alpha));
	}
}

EXTERN_C
DLLEXP void vecDnAddSpS(float* dense, const float* sparse, const int* index, const size_t nnz, const float alpha)
{
	vectorDenseAddBySparse(dense, sparse, index, nnz, alpha);
}
DLLEXP void vecDnAddSpD(double* dense, const double* sparse, const int* index, const size_t nnz, const double alpha)
{
	vectorDenseAddBySparse(dense, sparse, index, nnz, alpha);
}
DLLEXP void vecDnAddSpC(complexFloat* dense, const complexFloat* sparse, const int* index, const size_t nnz, const complexFloat alpha)
{
	vectorDenseAddBySparse(dense, sparse, index, nnz, alpha);
}
DLLEXP void vecDnAddSpZ(complexDouble* dense, const complexDouble* sparse, const int* index, const size_t nnz, const complexDouble alpha)
{
	vectorDenseAddBySparse(dense, sparse, index, nnz, alpha);
}
END_EXTERN_C
#pragma endregion
