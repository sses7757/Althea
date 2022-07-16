#include "blasSupp.h"


#pragma region set av values at positions
template <typename T>
inline int vectorSetValuesAt(void* dst, const void* value, const MKL_INT* pos, const size_t posN)
{
	T* a = (T*)dst;
	const T v = *((const T*)value);
	auto iter = thrust::make_permutation_iterator(a, pos);
	thrust::fill(THRUST_PAR, iter, iter + posN, v);
	return 0;
}

DLLEXP
int vecSetValAt(const Datatype::DataType type, void* a, const void* value, const MKL_INT* pos, const size_t posN)
{
	AUTO_ALLTYPE_FUNC(vectorSetValuesAt, type, int, a, value, pos, posN);
}
#pragma endregion


#pragma region dense vector prune to sparse vector
template <typename T, typename U>
struct aboveThreshold_functor
{
	const U threshold;
	aboveThreshold_functor(const U t) : threshold(std::abs(t)) {}

	__host__ __device__ bool operator()(const T x) const
	{
		return std::abs(x) > threshold;
	}
};

// dense vector prune to sparse vector -- get buffer size
template <typename T>
inline size_t vecPruneBuffer(const size_t N)
{
	size_t res = sizeof(MKL_INT) * N; // max size for possible indices
	res += sizeof(T) * N; // size for temporary values
	return res;
}

DLLEXP
size_t vecPruneBuffer(const size_t N, const Datatype::DataType type)
{
	AUTO_ALLTYPE_FUNC(vecPruneBuffer, type, size_t, N);
}

// dense vector prune to sparse vector -- get non-zeros
template <typename T>
inline size_t vectorPruneNonZeros(const void* av, const void* threshold, const size_t N, void* buffer)
{
	const T* a = (const T*)av;
	const T thre = std::abs(*((const T*)threshold));

	// create result container
	MKL_INT* idxOut = (MKL_INT*)buffer;
	T* valOut = (T*)(N + (MKL_INT*)buffer);

	// make zip
	auto zipBegin = thrust::make_zip_iterator(thrust::make_tuple(thrust::make_counting_iterator(0), a));
	auto resultBegin = thrust::make_zip_iterator(thrust::make_tuple(idxOut, valOut));

	// copy_if to get sparse indexes
	auto resultEnd = resultBegin;
	if constexpr (std::is_scalar<T>::value)
	{
		resultEnd = thrust::copy_if(THRUST_PAR, zipBegin, zipBegin + N, a, resultBegin, aboveThreshold_functor<T, T>(thre));
	}
	else
	{
		resultEnd = thrust::copy_if(THRUST_PAR, zipBegin, zipBegin + N, a, resultBegin, aboveThreshold_functor<T, typename T::value_type>(std::abs(thre)));
	}
	return resultEnd - resultBegin;
}

DLLEXP
size_t vecPruneNnz(const Datatype::DataType type, const void* a, const void* threshold, const size_t N, void* buffer)
{
	AUTO_ALLTYPE_FUNC(vectorPruneNonZeros, type, size_t, a, threshold, N, buffer);
}

// dense vector prune to sparse vector -- calculate
template <typename T>
inline ERROR_RETURN vecPruneCalculate(const void* buffer, const size_t N, size_t nnz, MKL_INT* indexOut, void* valueOut)
{
	// get result container from buffer
	const MKL_INT* idxOut = (MKL_INT*)buffer;
	const T* valOut = (const T*)(N + (const MKL_INT*)buffer);

	// copy to output arrays
#ifdef CPU
	memcpy(indexOut, idxOut, sizeof(MKL_INT) * nnz);
	memcpy(valueOut, valOut, sizeof(T) * nnz);
	return 0;
#else
	cudaError err = cudaMemcpy(indexOut, idxOut, sizeof(MKL_INT) * nnz, cudaMemcpyDeviceToDevice);
	if (err != 0) return err;
	err = cudaMemcpy(valueOut, valOut, sizeof(T) * nnz, cudaMemcpyDeviceToDevice);
	if (err != 0) return err;
	// return
	return err;
#endif // CPU
}

DLLEXP
ERROR_RETURN vecPruneCal(const Datatype::DataType type, const size_t N, const void* buffer, size_t nnz, MKL_INT* indexOut, void* valueOut)
{
	AUTO_ALLTYPE_FUNC(vecPruneCalculate, type, ERROR_RETURN, buffer, N, nnz, indexOut, valueOut);
}
#pragma endregion


#pragma region sparse vector element-wise multipilied or divided by dense vector
template<typename T>
inline void vectorSparseMultipliedDividedByDense(void* sparsev, const MKL_INT* index, const size_t nnz, const void* densev, bool multiply)
{
	T* sparse = (T*)sparsev;
	const T* dense = (const T*)densev;
	if (multiply)
	{
		thrust::transform(THRUST_PAR, sparse, sparse + nnz, thrust::make_permutation_iterator(dense, index), sparse, thrust::multiplies<T>());
	}
	else
	{
		thrust::transform(THRUST_PAR, sparse, sparse + nnz, thrust::make_permutation_iterator(dense, index), sparse, thrust::divides<T>());
	}
}

DLLEXP
void vecSpMulDivDn(const Datatype::DataType type, void* sparse, const MKL_INT* index, const size_t nnz, const void* dense, bool multiply)
{
	AUTO_ALLTYPE_FUNC(vectorSparseMultipliedDividedByDense, type, void, sparse, index, nnz, dense, multiply);
}
#pragma endregion


#pragma region sparse vector add sparse vector
// sparse vector add another sparse vector -- get buffer size
DLLEXP
size_t vecSpAddBuffer(const size_t nnzA, const size_t nnzB, const Datatype::DataType type)
{
	size_t N = nnzA + nnzB;
	size_t res = sizeof(MKL_INT) * N; // size for temporary indices
	res += Datatype::size(type) * N; // size for temporary values
	return res;
}

template <typename T>
struct multiplyScalar_functor
{
	const T scalar;
	multiplyScalar_functor(const T s) : scalar(s) {}

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
inline size_t vectorSparseAddGetNonzero(const MKL_INT* indA, const void* valAv, const size_t nnzA, const MKL_INT* indB, const void* valBv, const size_t nnzB, const void* alphav, void* buffer)
{
	// cast
	const T* valA = (const T*)valAv;
	const T* valB = (const T*)valBv;
	const T alpha = *((const T*)alphav);

	size_t nnz = nnzA + nnzB;
	// get storage from buffer for the combined contents of sparse vectors A and B
	MKL_INT* temp_index = (MKL_INT*)buffer;
	T* temp_value = (T*)(nnz + (MKL_INT*)buffer);

	// merge A and B by index
	if (alpha == 1)
	{
		thrust::merge_by_key(THRUST_PAR, indA, indA + nnzA, indB, indB + nnzB, valA, valB, temp_index, temp_value);
	}
	else
	{
		auto alphaMultiplyB = thrust::make_transform_iterator(valB, multiplyScalar_functor<T>(alpha));
		thrust::merge_by_key(THRUST_PAR, indA, indA + nnzA, indB, indB + nnzB, valA, alphaMultiplyB, temp_index, temp_value);
	}

	// compute number of unique indices, must larger than 0
	size_t nnzC = thrust::inner_product(THRUST_PAR, temp_index, temp_index + nnz - 1, temp_index + 1, int(0), plus_functor<int>(), notEqualAsInt_functor<int>());
	nnzC += 1;

	// return
	return nnzC;
}

DLLEXP
size_t vecSpAddNnz(const Datatype::DataType type,
	const MKL_INT* indA, const void* valA, const size_t nnzA,
	const MKL_INT* indB, const void* valB, const size_t nnzB,
	const void* alpha, void* buffer)
{
	AUTO_ALLTYPE_FUNC(vectorSparseAddGetNonzero, type, size_t, indA, valA, nnzA, indB, valB, nnzB, alpha, buffer);
}

// sparse vector add another sparse vector -- calculate
template <typename T>
inline void vectorSparseAddCalculate(const void* buffer, size_t nnzAB, size_t nnzC, MKL_INT* C_indexOut, void* C_valueOut)
{
	// cast
	T* C_value = (T*)C_valueOut;
	// get storage from buffer for the combined contents of sparse vectors A and B
	const MKL_INT* temp_index = (const MKL_INT*)buffer;
	const T* temp_value = (const T*)(nnzAB + (const MKL_INT*)buffer);

	// sum values with the same index
	thrust::reduce_by_key(THRUST_PAR, temp_index, temp_index + nnzAB, temp_value, C_indexOut, C_value, thrust::equal_to<int>(), plus_functor<T>());
}

DLLEXP
void vecSpAddCal(const Datatype::DataType type, const void* buffer, size_t nnzAB, size_t nnzC, MKL_INT* C_index, void* C_value)
{
	AUTO_ALLTYPE_FUNC(vectorSparseAddCalculate, type, void, buffer, nnzAB, nnzC, C_index, C_value);
}
#pragma endregion


#pragma region dense vector added by sparse
// return alpha * x + y
template <typename T>
struct FMA_functor
{
	const T alpha;
	FMA_functor(const T a) : alpha(a) {}

	__host__ __device__ T operator()(const T x, const T y) const
	{
		return alpha * x + y;
	}
};

// dense[index[i]] = sparse[i] * alpha + dense[index[i]]
template <typename T>
inline void vectorDenseAddBySparse(void* densev, const void* sparsev, const MKL_INT* index, const size_t nnz, const void* alphav)
{
	T* dense = (T*)densev;
	const T* sparse = (const T*)sparsev;
	const T alpha = *((const T*)alphav);

	auto densePerm = thrust::make_permutation_iterator(dense, index);
	if (alpha == 1)
	{
		thrust::transform(THRUST_PAR, sparse, sparse + nnz, densePerm, densePerm, plus_functor<T>());
	}
	else
	{
		thrust::transform(THRUST_PAR, sparse, sparse + nnz, densePerm, densePerm, FMA_functor<T>(alpha));
	}
}

DLLEXP
void vecDnAddSp(const Datatype::DataType type, void* dense, const void* sparse, const MKL_INT* index, const size_t nnz, const void* alpha)
{
	AUTO_ALLTYPE_FUNC(vectorDenseAddBySparse, type, void, dense, sparse, index, nnz, alpha);
}
#pragma endregion
