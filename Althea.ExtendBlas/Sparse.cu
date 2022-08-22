#include "extblas.h"
using namespace extblas;

#ifdef MKL_INT
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
int vecSetValAt(const DataType type, void* a, const void* value, const MKL_INT* pos, const size_t posN)
{
	AUTO_ALLTYPE_FUNC(vectorSetValuesAt, type, MKL_INT, a, value, pos, posN);
}
#pragma endregion

#pragma region dense vector prune to sparse vector
// dense vector prune to sparse vector -- get buffer size
template <typename T>
inline ptrdiff_t vecPruneBuffer(const size_t N)
{
	size_t res = sizeof(MKL_INT) * N; // max size for possible indices
	res += sizeof(T) * N; // size for temporary values
	return res;
}

DLLEXP
ptrdiff_t vecPruneBuffer(const DataType type, const size_t N)
{
	AUTO_ALLTYPE_FUNC(vecPruneBuffer, type, ptrdiff_t, N);
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
	if constexpr (std::is_scalar_v<T>)
	{
		resultEnd = thrust::copy_if(THRUST_PAR, zipBegin, zipBegin + N, a, resultBegin, [=] PREFIX(const T a) { return a > thre; });
	}
	else
	{
		const typename real_type<T>::type threAbs = std::abs(*((const T*)threshold));
		resultEnd = thrust::copy_if(THRUST_PAR, zipBegin, zipBegin + N, a, resultBegin, [=] PREFIX(const T a) { return a > threAbs; });
	}
	return resultEnd - resultBegin;
}

DLLEXP
size_t vecPruneNnz(const DataType type, const void* a, const void* threshold, const size_t N, void* buffer)
{
	AUTO_ALLTYPE_FUNC(vectorPruneNonZeros, type, size_t, a, threshold, N, buffer);
}

// dense vector prune to sparse vector -- no buffer
template <typename T>
inline ptrdiff_t vectorPruneDirect(const void* av, const void* threshold, const size_t N, MKL_INT* idxOut, void* valOutv, const bool safeOut, const size_t outN)
{
	const T* a = (const T*)av;
	const T thre = std::abs(*((const T*)threshold));

	// create result container
	T* valOut = (T*)valOutv;

	// make zip
	auto zipBegin = thrust::make_zip_iterator(thrust::make_tuple(thrust::make_counting_iterator(0), a));
	auto resultBegin = thrust::make_zip_iterator(thrust::make_tuple(idxOut, valOut));

	// copy_if to get sparse indexes
	auto tempBegin = thrust::make_zip_iterator(thrust::make_tuple(thrust::make_discard_iterator((size_t)0), thrust::make_discard_iterator((T)0)));
	if constexpr (std::is_scalar_v<T>)
	{
		if (safeOut)
		{
			auto diff = thrust::copy_if(THRUST_PAR, zipBegin, zipBegin + N, a, tempBegin, [=] PREFIX(const T a) { return a > thre; }) - tempBegin;
			if (diff > outN)
				return diff - outN;
		}
		thrust::copy_if(THRUST_PAR, zipBegin, zipBegin + N, a, resultBegin, [=] PREFIX(const T a) { return a > thre; });
	}
	else
	{
		if (safeOut)
		{
			auto diff = thrust::copy_if(THRUST_PAR, zipBegin, zipBegin + N, a, tempBegin, aboveThreshold_functor<T, typename T::value_type>(std::abs(thre))) - tempBegin;
			if (diff > outN)
				return diff - outN;
		}
		const typename real_type<T>::type threAbs = std::abs(*((const T*)threshold));
		thrust::copy_if(THRUST_PAR, zipBegin, zipBegin + N, a, resultBegin, [=] PREFIX(const T a) { return a > threAbs; });
	}
	return 0;
}

DLLEXP
ptrdiff_t vecPruneDirect(const DataType type, const void* a, const void* threshold, const size_t N, MKL_INT* idxOut, void* valOut, const bool safe, const size_t nnz)
{
	AUTO_ALLTYPE_FUNC(vectorPruneDirect, type, ptrdiff_t, a, threshold, N, idxOut, valOut, safe, nnz);
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
ERROR_RETURN vecPruneCal(const DataType type, const size_t N, const void* buffer, size_t nnz, MKL_INT* indexOut, void* valueOut)
{
	AUTO_ALLTYPE_FUNC(vecPruneCalculate, type, ERROR_RETURN, buffer, N, nnz, indexOut, valueOut);
}
#pragma endregion

#pragma region sparse vector add sparse vector
// sparse vector add another sparse vector -- get buffer size
template <typename T>
inline ptrdiff_t vectorSpAddBuffer(const size_t nnzA, const size_t nnzB)
{
	size_t N = nnzA + nnzB;
	size_t res = sizeof(MKL_INT) * N; // size for temporary indices
	res += sizeof(T) * N; // size for temporary values
	return res;
}

DLLEXP
ptrdiff_t vecSpAddBuffer(const DataType type, const size_t nnzA, const size_t nnzB)
{
	AUTO_ALLTYPE_FUNC(vectorSpAddBuffer, type, ptrdiff_t, nnzA, nnzB);
}

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
		auto alphaMultiplyB = thrust::make_transform_iterator(valB, [=] PREFIX(const T a) { return a * alpha; });
		thrust::merge_by_key(THRUST_PAR, indA, indA + nnzA, indB, indB + nnzB, valA, alphaMultiplyB, temp_index, temp_value);
	}

	// compute number of unique indices, must larger than 0
	auto func = [] PREFIX(const T a, const T b) -> MKL_INT { return a == b ? 0 : 1; };
	size_t nnzC = thrust::inner_product(THRUST_PAR, temp_index, temp_index + nnz - 1, temp_index + 1, MKL_INT(0), plus_functor<MKL_INT>(), func);
	nnzC += 1;

	// return
	return nnzC;
}

DLLEXP
size_t vecSpAddNnz(const DataType type,
	const MKL_INT* indA, const void* valA, const size_t nnzA,
	const MKL_INT* indB, const void* valB, const size_t nnzB,
	const void* alpha, void* buffer)
{
	AUTO_ALLTYPE_FUNC(vectorSparseAddGetNonzero, type, size_t, indA, valA, nnzA, indB, valB, nnzB, alpha, buffer);
}

// sparse vector add another sparse vector -- calculate
template <typename T>
inline int vectorSparseAddCalculate(const void* buffer, size_t nnzAB, size_t nnzC, MKL_INT* C_indexOut, void* C_valueOut)
{
	// cast
	T* C_value = (T*)C_valueOut;
	// get storage from buffer for the combined contents of sparse vectors A and B
	const MKL_INT* temp_index = (const MKL_INT*)buffer;
	const T* temp_value = (const T*)(nnzAB + (const MKL_INT*)buffer);

	// sum values with the same index
	thrust::reduce_by_key(THRUST_PAR, temp_index, temp_index + nnzAB, temp_value, C_indexOut, C_value, thrust::equal_to<MKL_INT>(), plus_functor<T>());
	return 0;
}

DLLEXP
int vecSpAddCal(const DataType type, const void* buffer, size_t nnzAB, size_t nnzC, MKL_INT* C_index, void* C_value)
{
	AUTO_ALLTYPE_FUNC(vectorSparseAddCalculate, type, MKL_INT, buffer, nnzAB, nnzC, C_index, C_value);
}
#pragma endregion

#pragma region dense vector added by sparse
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
		thrust::transform(THRUST_PAR, sparse, sparse + nnz, densePerm, densePerm, [=] PREFIX(const T x, const T y) { return alpha * x + y; });
	}
}

DLLEXP
void vecDnAddSp(const DataType type, void* dense, const void* sparse, const MKL_INT* index, const size_t nnz, const void* alpha)
{
	AUTO_ALLTYPE_FUNC(vectorDenseAddBySparse, type, void, dense, sparse, index, nnz, alpha);
}
#pragma endregion

#pragma region sparse vector to/from COO matrix
DLLEXP
void spVecIdxToCooIdxs(const MKL_INT* index, MKL_INT* rowIdx, MKL_INT* colIdx, const size_t N, const MKL_INT ld)
{
	const MKL_INT lld = ld;
	thrust::transform(THRUST_PAR, index, index + N, rowIdx, [=] PREFIX(const MKL_INT x) { return x % lld; });
	thrust::transform(THRUST_PAR, index, index + N, colIdx, [=] PREFIX(const MKL_INT x) { return x / lld; });
}

DLLEXP
void cooIdxsToSpVecIdx(MKL_INT* index, const MKL_INT* rowIdx, const MKL_INT* colIdx, const size_t N, const MKL_INT ld)
{
	const MKL_INT lld = ld;
	thrust::transform(THRUST_PAR, rowIdx, rowIdx + N, colIdx, index, [=] PREFIX(const MKL_INT x, const MKL_INT y) { return x + y * lld; });
}
#pragma endregion

#pragma region sparse vectors outer product to COOC matrix
auto count_iter = thrust::make_counting_iterator((size_t)0);

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
inline int sparseVectorsOuterCheck()
{
	return 0;
}

DLLEXP int spVecOuterCheck(const extblas::DataType type)
{
	AUTO_ALLTYPE_FUNC(sparseVectorsOuterCheck, type, MKL_INT);
}

template<typename T>
inline int sparseVectorsOuter(
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

DLLEXP int spVecOuter(const extblas::DataType type,
	const void* valA, const MKL_INT* indA, const size_t nnzA,
	const void* valB, const MKL_INT* indB, const size_t nnzB,
	void* valC, MKL_INT* rowC, MKL_INT* colC, const bool conj)
{
	AUTO_ALLTYPE_FUNC(sparseVectorsOuter, type, MKL_INT, valA, indA, nnzA, valB, indB, nnzB, valC, rowC, colC, conj);
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
inline int cooMatricesKronecker(
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
	return 0;
}

DLLEXP int cooMatKron(const extblas::DataType type,
	const void* valA, const MKL_INT* rowA, const MKL_INT* colA, const size_t nnzA,
	const void* valB, const MKL_INT* rowB, const MKL_INT* colB, const size_t nnzB, const size_t rowsB, const size_t colsB,
	void* valC, MKL_INT* rowC, MKL_INT* colC)
{
	AUTO_ALLTYPE_FUNC(cooMatricesKronecker, type, MKL_INT, valA, rowA, colA, nnzA, valB, rowB, colB, nnzB, rowsB, colsB, valC, rowC, colC);
}
#pragma endregion
#else

#pragma region set av values at positions
template <typename T>
inline int vectorSetValuesAt(void* dst, const void* value, const int* pos, const size_t posN)
{
	T* a = (T*)dst;
	const T v = *((const T*)value);
	auto iter = thrust::make_permutation_iterator(a, pos);
	thrust::fill(THRUST_PAR, iter, iter + posN, v);
	return 0;
}

DLLEXP int vecSetValAt_i32(const DataType type, void* a, const void* value, const int* pos, const size_t posN)
{
	AUTO_ALLTYPE_FUNC(vectorSetValuesAt, type, int, a, value, pos, posN);
}
#pragma endregion

#pragma region dense vector prune to sparse vector
// dense vector prune to sparse vector -- get buffer size
template <typename T>
inline ptrdiff_t vecPruneBuffer(const size_t N)
{
	size_t res = sizeof(int) * N; // max size for possible indices
	res += sizeof(T) * N; // size for temporary values
	return res;
}

DLLEXP ptrdiff_t vecPruneBuffer_i32(const DataType type, const size_t N)
{
	AUTO_ALLTYPE_FUNC(vecPruneBuffer, type, ptrdiff_t, N);
}

// dense vector prune to sparse vector -- get non-zeros
template <typename T>
inline size_t vectorPruneNonZeros(const void* av, const void* threshold, const size_t N, void* buffer)
{
	const T* a = (const T*)av;
	const T thre = std::abs(*((const T*)threshold));

	// create result container
	int* idxOut = (int*)buffer;
	T* valOut = (T*)(N + (int*)buffer);

	// make zip
	auto zipBegin = thrust::make_zip_iterator(thrust::make_tuple(thrust::make_counting_iterator(0), a));
	auto resultBegin = thrust::make_zip_iterator(thrust::make_tuple(idxOut, valOut));

	// copy_if to get sparse indexes
	auto resultEnd = resultBegin;
	if constexpr (std::is_scalar_v<T>)
	{
		resultEnd = thrust::copy_if(THRUST_PAR, zipBegin, zipBegin + N, a, resultBegin, [=] PREFIX(const T a) { return a > thre; });
	}
	else
	{
		const typename real_type<T>::type threAbs = std::abs(*((const T*)threshold));
		resultEnd = thrust::copy_if(THRUST_PAR, zipBegin, zipBegin + N, a, resultBegin, [=] PREFIX(const T a) { return a > threAbs; });
	}
	return resultEnd - resultBegin;
}

DLLEXP size_t vecPruneNnz_i32(const DataType type, const void* a, const void* threshold, const size_t N, void* buffer)
{
	AUTO_ALLTYPE_FUNC(vectorPruneNonZeros, type, size_t, a, threshold, N, buffer);
}

// dense vector prune to sparse vector -- no buffer
template <typename T>
inline ptrdiff_t vectorPruneDirect(const void* av, const void* threshold, const size_t N, int* idxOut, void* valOutv, const bool safeOut, const size_t outN)
{
	const T* a = (const T*)av;
	const T thre = std::abs(*((const T*)threshold));

	// create result container
	T* valOut = (T*)valOutv;

	// make zip
	auto zipBegin = thrust::make_zip_iterator(thrust::make_tuple(thrust::make_counting_iterator(0), a));
	auto resultBegin = thrust::make_zip_iterator(thrust::make_tuple(idxOut, valOut));

	// copy_if to get sparse indexes
	auto tempBegin = thrust::make_zip_iterator(thrust::make_tuple(thrust::make_discard_iterator((size_t)0), thrust::make_discard_iterator((T)0)));
	if constexpr (std::is_scalar_v<T>)
	{
		if (safeOut)
		{
			auto diff = thrust::copy_if(THRUST_PAR, zipBegin, zipBegin + N, a, tempBegin, [=] PREFIX(const T a) { return a > thre; }) - tempBegin;
			if (diff > outN)
				return diff - outN;
		}
		thrust::copy_if(THRUST_PAR, zipBegin, zipBegin + N, a, resultBegin, [=] PREFIX(const T a) { return a > thre; });
	}
	else
	{
		if (safeOut)
		{
			auto diff = thrust::copy_if(THRUST_PAR, zipBegin, zipBegin + N, a, tempBegin, aboveThreshold_functor<T, typename T::value_type>(std::abs(thre))) - tempBegin;
			if (diff > outN)
				return diff - outN;
		}
		const typename real_type<T>::type threAbs = std::abs(*((const T*)threshold));
		thrust::copy_if(THRUST_PAR, zipBegin, zipBegin + N, a, resultBegin, [=] PREFIX(const T a) { return a > threAbs; });
	}
	return 0;
}

DLLEXP ptrdiff_t vecPruneDirect_i32(const DataType type, const void* a, const void* threshold, const size_t N, int* idxOut, void* valOut, const bool safe, const size_t nnz)
{
	AUTO_ALLTYPE_FUNC(vectorPruneDirect, type, ptrdiff_t, a, threshold, N, idxOut, valOut, safe, nnz);
}

// dense vector prune to sparse vector -- calculate
template <typename T>
inline ERROR_RETURN vecPruneCalculate(const void* buffer, const size_t N, size_t nnz, int* indexOut, void* valueOut)
{
	// get result container from buffer
	const int* idxOut = (int*)buffer;
	const T* valOut = (const T*)(N + (const int*)buffer);

	// copy to output arrays
#ifdef CPU
	memcpy(indexOut, idxOut, sizeof(int) * nnz);
	memcpy(valueOut, valOut, sizeof(T) * nnz);
	return 0;
#else
	cudaError err = cudaMemcpy(indexOut, idxOut, sizeof(int) * nnz, cudaMemcpyDeviceToDevice);
	if (err != 0) return err;
	err = cudaMemcpy(valueOut, valOut, sizeof(T) * nnz, cudaMemcpyDeviceToDevice);
	if (err != 0) return err;
	// return
	return err;
#endif // CPU
}

DLLEXP ERROR_RETURN vecPruneCal_i32(const DataType type, const size_t N, const void* buffer, size_t nnz, int* indexOut, void* valueOut)
{
	AUTO_ALLTYPE_FUNC(vecPruneCalculate, type, ERROR_RETURN, buffer, N, nnz, indexOut, valueOut);
}
#pragma endregion

#pragma region sparse vector add sparse vector
// sparse vector add another sparse vector -- get buffer size
template <typename T>
inline ptrdiff_t vectorSpAddBuffer(const size_t nnzA, const size_t nnzB)
{
	size_t N = nnzA + nnzB;
	size_t res = sizeof(int) * N; // size for temporary indices
	res += sizeof(T) * N; // size for temporary values
	return res;
}

DLLEXP ptrdiff_t vecSpAddBuffer_i32(const DataType type, const size_t nnzA, const size_t nnzB)
{
	AUTO_ALLTYPE_FUNC(vectorSpAddBuffer, type, ptrdiff_t, nnzA, nnzB);
}

// sparse vector add another sparse vector -- get non-zeros, 'alpha' is the number to multiply to each value of B
template <typename T>
inline size_t vectorSparseAddGetNonzero(const int* indA, const void* valAv, const size_t nnzA, const int* indB, const void* valBv, const size_t nnzB, const void* alphav, void* buffer)
{
	// cast
	const T* valA = (const T*)valAv;
	const T* valB = (const T*)valBv;
	const T alpha = *((const T*)alphav);

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
		auto alphaMultiplyB = thrust::make_transform_iterator(valB, [=] PREFIX(const T a) { return a * alpha; });
		thrust::merge_by_key(THRUST_PAR, indA, indA + nnzA, indB, indB + nnzB, valA, alphaMultiplyB, temp_index, temp_value);
	}

	// compute number of unique indices, must larger than 0
	auto func = [] PREFIX(const T a, const T b) -> int { return a == b ? 0 : 1; };
	size_t nnzC = thrust::inner_product(THRUST_PAR, temp_index, temp_index + nnz - 1, temp_index + 1, int(0), plus_functor<int>(), func);
	nnzC += 1;

	// return
	return nnzC;
}

DLLEXP size_t vecSpAddNnz_i32(const DataType type,
	const int* indA, const void* valA, const size_t nnzA,
	const int* indB, const void* valB, const size_t nnzB,
	const void* alpha, void* buffer)
{
	AUTO_ALLTYPE_FUNC(vectorSparseAddGetNonzero, type, size_t, indA, valA, nnzA, indB, valB, nnzB, alpha, buffer);
}

// sparse vector add another sparse vector -- calculate
template <typename T>
inline int vectorSparseAddCalculate(const void* buffer, size_t nnzAB, size_t nnzC, int* C_indexOut, void* C_valueOut)
{
	// cast
	T* C_value = (T*)C_valueOut;
	// get storage from buffer for the combined contents of sparse vectors A and B
	const int* temp_index = (const int*)buffer;
	const T* temp_value = (const T*)(nnzAB + (const int*)buffer);

	// sum values with the same index
	thrust::reduce_by_key(THRUST_PAR, temp_index, temp_index + nnzAB, temp_value, C_indexOut, C_value, thrust::equal_to<int>(), plus_functor<T>());
	return 0;
}

DLLEXP int vecSpAddCal_i32(const DataType type, const void* buffer, size_t nnzAB, size_t nnzC, int* C_index, void* C_value)
{
	AUTO_ALLTYPE_FUNC(vectorSparseAddCalculate, type, int, buffer, nnzAB, nnzC, C_index, C_value);
}
#pragma endregion

#pragma region dense vector added by sparse
// dense[index[i]] = sparse[i] * alpha + dense[index[i]]
template <typename T>
inline void vectorDenseAddBySparse(void* densev, const void* sparsev, const int* index, const size_t nnz, const void* alphav)
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
		thrust::transform(THRUST_PAR, sparse, sparse + nnz, densePerm, densePerm, [=] PREFIX(const T x, const T y) { return alpha * x + y; });
	}
}

DLLEXP void vecDnAddSp_i32(const DataType type, void* dense, const void* sparse, const int* index, const size_t nnz, const void* alpha)
{
	AUTO_ALLTYPE_FUNC(vectorDenseAddBySparse, type, void, dense, sparse, index, nnz, alpha);
}
#pragma endregion

#pragma region sparse vector to/from COO matrix
DLLEXP void spVecIdxToCooIdxs_i32(const int* index, int* rowIdx, int* colIdx, const size_t N, const int ld)
{
	const int lld = ld;
	thrust::transform(THRUST_PAR, index, index + N, rowIdx, [=] PREFIX(const int x) { return x % lld; });
	thrust::transform(THRUST_PAR, index, index + N, colIdx, [=] PREFIX(const int x) { return x / lld; });
}

DLLEXP void cooIdxsToSpVecIdx_i32(int* index, const int* rowIdx, const int* colIdx, const size_t N, const int ld)
{
	const int lld = ld;
	thrust::transform(THRUST_PAR, rowIdx, rowIdx + N, colIdx, index, [=] PREFIX(const int x, const int y) { return x + y * lld; });
}
#pragma endregion

#pragma region sparse vectors outer product to COOC matrix
auto count_iter = thrust::make_counting_iterator((size_t)0);

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
inline int sparseVectorsOuterCheck()
{
	return 0;
}

DLLEXP int spVecOuterCheck_i32(const extblas::DataType type)
{
	AUTO_ALLTYPE_FUNC(sparseVectorsOuterCheck, type, int);
}

template<typename T>
inline int sparseVectorsOuter(
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

	return 0;
}

DLLEXP int spVecOuter_i32(const extblas::DataType type,
	const void* valA, const int* indA, const size_t nnzA,
	const void* valB, const int* indB, const size_t nnzB,
	void* valC, int* rowC, int* colC, const bool conj)
{
	AUTO_ALLTYPE_FUNC(sparseVectorsOuter, type, int, valA, indA, nnzA, valB, indB, nnzB, valC, rowC, colC, conj);
}
#pragma endregion

#pragma region sparse COO format matrices Kronecker
template <typename T>
struct CooMatricesKronecker_functor
{
	const T* valA; const int* rowA; const int* colA;
	const T* valB; const int* rowB; const int* colB; const size_t nnzB; const size_t rowsB; const size_t colsB;
	T* valC; int* rowC; int* colC;

	CooMatricesKronecker_functor(
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
inline int cooMatricesKronecker(
	const void* valAv, const int* rowA, const int* colA, const size_t nnzA,
	const void* valBv, const int* rowB, const int* colB, const size_t nnzB, const size_t rowsB, const size_t colsB,
	void* valCv, int* rowC, int* colC)
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
	return 0;
}

DLLEXP int cooMatKron_i32(const extblas::DataType type,
	const void* valA, const int* rowA, const int* colA, const size_t nnzA,
	const void* valB, const int* rowB, const int* colB, const size_t nnzB, const size_t rowsB, const size_t colsB,
	void* valC, int* rowC, int* colC)
{
	AUTO_ALLTYPE_FUNC(cooMatricesKronecker, type, int, valA, rowA, colA, nnzA, valB, rowB, colB, nnzB, rowsB, colsB, valC, rowC, colC);
}
#pragma endregion




#pragma region set av values at positions
template <typename T>
inline int vectorSetValuesAt(void* dst, const void* value, const long long* pos, const size_t posN)
{
	T* a = (T*)dst;
	const T v = *((const T*)value);
	auto iter = thrust::make_permutation_iterator(a, pos);
	thrust::fill(THRUST_PAR, iter, iter + posN, v);
	return 0;
}

DLLEXP int vecSetValAt_i64(const DataType type, void* a, const void* value, const long long* pos, const size_t posN)
{
	AUTO_ALLTYPE_FUNC(vectorSetValuesAt, type, long long, a, value, pos, posN);
}
#pragma endregion

#pragma region dense vector prune to sparse vector
// dense vector prune to sparse vector -- get buffer size
template <typename T>
inline ptrdiff_t vecPruneBuffer(const size_t N)
{
	size_t res = sizeof(long long) * N; // max size for possible indices
	res += sizeof(T) * N; // size for temporary values
	return res;
}

DLLEXP ptrdiff_t vecPruneBuffer_i64(const DataType type, const size_t N)
{
	AUTO_ALLTYPE_FUNC(vecPruneBuffer, type, ptrdiff_t, N);
}

// dense vector prune to sparse vector -- get non-zeros
template <typename T>
inline size_t vectorPruneNonZeros(const void* av, const void* threshold, const size_t N, void* buffer)
{
	const T* a = (const T*)av;
	const T thre = std::abs(*((const T*)threshold));

	// create result container
	long long* idxOut = (long long*)buffer;
	T* valOut = (T*)(N + (long long*)buffer);

	// make zip
	auto zipBegin = thrust::make_zip_iterator(thrust::make_tuple(thrust::make_counting_iterator(0), a));
	auto resultBegin = thrust::make_zip_iterator(thrust::make_tuple(idxOut, valOut));

	// copy_if to get sparse indexes
	auto resultEnd = resultBegin;
	if constexpr (std::is_scalar_v<T>)
	{
		resultEnd = thrust::copy_if(THRUST_PAR, zipBegin, zipBegin + N, a, resultBegin, [=] PREFIX(const T a) { return a > thre; });
	}
	else
	{
		const typename real_type<T>::type threAbs = std::abs(*((const T*)threshold));
		resultEnd = thrust::copy_if(THRUST_PAR, zipBegin, zipBegin + N, a, resultBegin, [=] PREFIX(const T a) { return a > threAbs; });
	}
	return resultEnd - resultBegin;
}

DLLEXP size_t vecPruneNnz_i64(const DataType type, const void* a, const void* threshold, const size_t N, void* buffer)
{
	AUTO_ALLTYPE_FUNC(vectorPruneNonZeros, type, size_t, a, threshold, N, buffer);
}

// dense vector prune to sparse vector -- no buffer
template <typename T>
inline ptrdiff_t vectorPruneDirect(const void* av, const void* threshold, const size_t N, long long* idxOut, void* valOutv, const bool safeOut, const size_t outN)
{
	const T* a = (const T*)av;
	const T thre = std::abs(*((const T*)threshold));

	// create result container
	T* valOut = (T*)valOutv;

	// make zip
	auto zipBegin = thrust::make_zip_iterator(thrust::make_tuple(thrust::make_counting_iterator(0), a));
	auto resultBegin = thrust::make_zip_iterator(thrust::make_tuple(idxOut, valOut));

	// copy_if to get sparse indexes
	auto tempBegin = thrust::make_zip_iterator(thrust::make_tuple(thrust::make_discard_iterator((size_t)0), thrust::make_discard_iterator((T)0)));
	if constexpr (std::is_scalar_v<T>)
	{
		if (safeOut)
		{
			auto diff = thrust::copy_if(THRUST_PAR, zipBegin, zipBegin + N, a, tempBegin, [=] PREFIX(const T a) { return a > thre; }) - tempBegin;
			if (diff > outN)
				return diff - outN;
		}
		thrust::copy_if(THRUST_PAR, zipBegin, zipBegin + N, a, resultBegin, [=] PREFIX(const T a) { return a > thre; });
	}
	else
	{
		if (safeOut)
		{
			auto diff = thrust::copy_if(THRUST_PAR, zipBegin, zipBegin + N, a, tempBegin, aboveThreshold_functor<T, typename T::value_type>(std::abs(thre))) - tempBegin;
			if (diff > outN)
				return diff - outN;
		}
		const typename real_type<T>::type threAbs = std::abs(*((const T*)threshold));
		thrust::copy_if(THRUST_PAR, zipBegin, zipBegin + N, a, resultBegin, [=] PREFIX(const T a) { return a > threAbs; });
	}
	return 0;
}

DLLEXP ptrdiff_t vecPruneDirect_i64(const DataType type, const void* a, const void* threshold, const size_t N, long long* idxOut, void* valOut, const bool safe, const size_t nnz)
{
	AUTO_ALLTYPE_FUNC(vectorPruneDirect, type, ptrdiff_t, a, threshold, N, idxOut, valOut, safe, nnz);
}

// dense vector prune to sparse vector -- calculate
template <typename T>
inline ERROR_RETURN vecPruneCalculate(const void* buffer, const size_t N, size_t nnz, long long* indexOut, void* valueOut)
{
	// get result container from buffer
	const long long* idxOut = (long long*)buffer;
	const T* valOut = (const T*)(N + (const long long*)buffer);

	// copy to output arrays
#ifdef CPU
	memcpy(indexOut, idxOut, sizeof(long long) * nnz);
	memcpy(valueOut, valOut, sizeof(T) * nnz);
	return 0;
#else
	cudaError err = cudaMemcpy(indexOut, idxOut, sizeof(long long) * nnz, cudaMemcpyDeviceToDevice);
	if (err != 0) return err;
	err = cudaMemcpy(valueOut, valOut, sizeof(T) * nnz, cudaMemcpyDeviceToDevice);
	if (err != 0) return err;
	// return
	return err;
#endif // CPU
}

DLLEXP ERROR_RETURN vecPruneCal_i64(const DataType type, const size_t N, const void* buffer, size_t nnz, long long* indexOut, void* valueOut)
{
	AUTO_ALLTYPE_FUNC(vecPruneCalculate, type, ERROR_RETURN, buffer, N, nnz, indexOut, valueOut);
}
#pragma endregion

#pragma region sparse vector add sparse vector
// sparse vector add another sparse vector -- get buffer size
template <typename T>
inline ptrdiff_t vectorSpAddBuffer(const size_t nnzA, const size_t nnzB)
{
	size_t N = nnzA + nnzB;
	size_t res = sizeof(long long) * N; // size for temporary indices
	res += sizeof(T) * N; // size for temporary values
	return res;
}

DLLEXP ptrdiff_t vecSpAddBuffer_i64(const DataType type, const size_t nnzA, const size_t nnzB)
{
	AUTO_ALLTYPE_FUNC(vectorSpAddBuffer, type, ptrdiff_t, nnzA, nnzB);
}

// sparse vector add another sparse vector -- get non-zeros, 'alpha' is the number to multiply to each value of B
template <typename T>
inline size_t vectorSparseAddGetNonzero(const long long* indA, const void* valAv, const size_t nnzA, const long long* indB, const void* valBv, const size_t nnzB, const void* alphav, void* buffer)
{
	// cast
	const T* valA = (const T*)valAv;
	const T* valB = (const T*)valBv;
	const T alpha = *((const T*)alphav);

	size_t nnz = nnzA + nnzB;
	// get storage from buffer for the combined contents of sparse vectors A and B
	long long* temp_index = (long long*)buffer;
	T* temp_value = (T*)(nnz + (long long*)buffer);

	// merge A and B by index
	if (alpha == 1)
	{
		thrust::merge_by_key(THRUST_PAR, indA, indA + nnzA, indB, indB + nnzB, valA, valB, temp_index, temp_value);
	}
	else
	{
		auto alphaMultiplyB = thrust::make_transform_iterator(valB, [=] PREFIX(const T a) { return a * alpha; });
		thrust::merge_by_key(THRUST_PAR, indA, indA + nnzA, indB, indB + nnzB, valA, alphaMultiplyB, temp_index, temp_value);
	}

	// compute number of unique indices, must larger than 0
	auto func = [] PREFIX(const T a, const T b) -> long long { return a == b ? 0 : 1; };
	size_t nnzC = thrust::inner_product(THRUST_PAR, temp_index, temp_index + nnz - 1, temp_index + 1, long long(0), plus_functor<long long>(), func);
	nnzC += 1;

	// return
	return nnzC;
}

DLLEXP size_t vecSpAddNnz_i64(const DataType type,
	const long long* indA, const void* valA, const size_t nnzA,
	const long long* indB, const void* valB, const size_t nnzB,
	const void* alpha, void* buffer)
{
	AUTO_ALLTYPE_FUNC(vectorSparseAddGetNonzero, type, size_t, indA, valA, nnzA, indB, valB, nnzB, alpha, buffer);
}

// sparse vector add another sparse vector -- calculate
template <typename T>
inline int vectorSparseAddCalculate(const void* buffer, size_t nnzAB, size_t nnzC, long long* C_indexOut, void* C_valueOut)
{
	// cast
	T* C_value = (T*)C_valueOut;
	// get storage from buffer for the combined contents of sparse vectors A and B
	const long long* temp_index = (const long long*)buffer;
	const T* temp_value = (const T*)(nnzAB + (const long long*)buffer);

	// sum values with the same index
	thrust::reduce_by_key(THRUST_PAR, temp_index, temp_index + nnzAB, temp_value, C_indexOut, C_value, thrust::equal_to<long long>(), plus_functor<T>());
	return 0;
}

DLLEXP int vecSpAddCal_i64(const DataType type, const void* buffer, size_t nnzAB, size_t nnzC, long long* C_index, void* C_value)
{
	AUTO_ALLTYPE_FUNC(vectorSparseAddCalculate, type, long long, buffer, nnzAB, nnzC, C_index, C_value);
}
#pragma endregion

#pragma region dense vector added by sparse
// dense[index[i]] = sparse[i] * alpha + dense[index[i]]
template <typename T>
inline void vectorDenseAddBySparse(void* densev, const void* sparsev, const long long* index, const size_t nnz, const void* alphav)
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
		thrust::transform(THRUST_PAR, sparse, sparse + nnz, densePerm, densePerm, [=] PREFIX(const T x, const T y) { return alpha * x + y; });
	}
}

DLLEXP void vecDnAddSp_i64(const DataType type, void* dense, const void* sparse, const long long* index, const size_t nnz, const void* alpha)
{
	AUTO_ALLTYPE_FUNC(vectorDenseAddBySparse, type, void, dense, sparse, index, nnz, alpha);
}
#pragma endregion

#pragma region sparse vector to/from COO matrix
DLLEXP void spVecIdxToCooIdxs_i64(const long long* index, long long* rowIdx, long long* colIdx, const size_t N, const long long ld)
{
	const long long lld = ld;
	thrust::transform(THRUST_PAR, index, index + N, rowIdx, [=] PREFIX(const long long x) { return x % lld; });
	thrust::transform(THRUST_PAR, index, index + N, colIdx, [=] PREFIX(const long long x) { return x / lld; });
}

DLLEXP void cooIdxsToSpVecIdx_i64(long long* index, const long long* rowIdx, const long long* colIdx, const size_t N, const long long ld)
{
	const long long lld = ld;
	thrust::transform(THRUST_PAR, rowIdx, rowIdx + N, colIdx, index, [=] PREFIX(const long long x, const long long y) { return x + y * lld; });
}
#pragma endregion

#pragma region sparse vectors outer product to COOC matrix
auto count_iter = thrust::make_counting_iterator((size_t)0);

template <typename T, bool conj>
struct sparseVectorsOuter_functor
{
	const T* valA; const long long* indA; const size_t nnzA;
	const T* valB; const long long* indB;
	T* valC; long long* rowC; long long* colC;

	sparseVectorsOuter_functor(
		const T* valA, const long long* indA, const size_t nnzA,
		const T* valB, const long long* indB,
		T* valC, long long* rowC, long long* colC) :
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
inline int sparseVectorsOuterCheck()
{
	return 0;
}

DLLEXP int spVecOuterCheck_i64(const extblas::DataType type)
{
	AUTO_ALLTYPE_FUNC(sparseVectorsOuterCheck, type, long long);
}

template<typename T>
inline int sparseVectorsOuter(
	const void* valAv, const long long* indA, const size_t nnzA,
	const void* valBv, const long long* indB, const size_t nnzB,
	void* valCv, long long* rowC, long long* colC, const bool conj)
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

DLLEXP int spVecOuter_i64(const extblas::DataType type,
	const void* valA, const long long* indA, const size_t nnzA,
	const void* valB, const long long* indB, const size_t nnzB,
	void* valC, long long* rowC, long long* colC, const bool conj)
{
	AUTO_ALLTYPE_FUNC(sparseVectorsOuter, type, long long, valA, indA, nnzA, valB, indB, nnzB, valC, rowC, colC, conj);
}
#pragma endregion

#pragma region sparse COO format matrices Kronecker
template <typename T>
struct CooMatricesKronecker_functor
{
	const T* valA; const long long* rowA; const long long* colA;
	const T* valB; const long long* rowB; const long long* colB; const size_t nnzB; const size_t rowsB; const size_t colsB;
	T* valC; long long* rowC; long long* colC;

	CooMatricesKronecker_functor(
		const T* valA, const long long* rowA, const long long* colA,
		const T* valB, const long long* rowB, const long long* colB, const size_t nnzB, const size_t rowsB, const size_t colsB,
		T* valC, long long* rowC, long long* colC) :
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
	__host__ __device__ bool operator()(const thrust::tuple<long long, long long> lhs, const thrust::tuple<long long, long long> rhs) const
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
inline int cooMatricesKronecker(
	const void* valAv, const long long* rowA, const long long* colA, const size_t nnzA,
	const void* valBv, const long long* rowB, const long long* colB, const size_t nnzB, const size_t rowsB, const size_t colsB,
	void* valCv, long long* rowC, long long* colC)
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
	return 0;
}

DLLEXP int cooMatKron_i64(const extblas::DataType type,
	const void* valA, const long long* rowA, const long long* colA, const size_t nnzA,
	const void* valB, const long long* rowB, const long long* colB, const size_t nnzB, const size_t rowsB, const size_t colsB,
	void* valC, long long* rowC, long long* colC)
{
	AUTO_ALLTYPE_FUNC(cooMatricesKronecker, type, long long, valA, rowA, colA, nnzA, valB, rowB, colB, nnzB, rowsB, colsB, valC, rowC, colC);
}
#pragma endregion

#endif // MKL_INT
