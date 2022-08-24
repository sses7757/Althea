#include "extblas.h"
using namespace extblas;


#pragma region template

// automatically generate float and integer type switch functions
#define AUTO_ALLTYPE_IND_FUNC(funcName, dataType, indType, returnType, ...) do { \
		switch (dataType) \
		{ \
		case DataType::RealFloat32: \
			return funcName<float, indType>(__VA_ARGS__); \
		case DataType::RealFloat64: \
			return funcName<double, indType>(__VA_ARGS__); \
		case DataType::ComplexFloat32: \
			return funcName<extblas::complex<float>, indType>(__VA_ARGS__); \
		case DataType::ComplexFloat64: \
			return funcName<extblas::complex<double>, indType>(__VA_ARGS__); \
		case DataType::RealInt8: \
			return funcName<int8_t, indType>(__VA_ARGS__); \
		case DataType::RealInt16: \
			return funcName<int16_t, indType>(__VA_ARGS__); \
		case DataType::RealInt32: \
			return funcName<int32_t, indType>(__VA_ARGS__); \
		case DataType::RealInt64: \
			return funcName<int64_t, indType>(__VA_ARGS__); \
		case DataType::RealUInt8: \
			return funcName<uint8_t, indType>(__VA_ARGS__); \
		case DataType::RealUInt16: \
			return funcName<uint16_t, indType>(__VA_ARGS__); \
		case DataType::RealUInt32: \
			return funcName<uint32_t, indType>(__VA_ARGS__); \
		case DataType::RealUInt64: \
			return funcName<uint64_t, indType>(__VA_ARGS__); \
		case DataType::ComplexInt8: \
			return funcName<extblas::complex<int8_t>, indType>(__VA_ARGS__); \
		case DataType::ComplexInt16: \
			return funcName<extblas::complex<int16_t>, indType>(__VA_ARGS__); \
		case DataType::ComplexInt32: \
			return funcName<extblas::complex<int32_t>, indType>(__VA_ARGS__); \
		case DataType::ComplexInt64: \
			return funcName<extblas::complex<int64_t>, indType>(__VA_ARGS__); \
		case DataType::ComplexUInt8: \
			return funcName<extblas::complex<uint8_t>, indType>(__VA_ARGS__); \
		case DataType::ComplexUInt16: \
			return funcName<extblas::complex<uint16_t>, indType>(__VA_ARGS__); \
		case DataType::ComplexUInt32: \
			return funcName<extblas::complex<uint32_t>, indType>(__VA_ARGS__); \
		case DataType::ComplexUInt64: \
			return funcName<extblas::complex<uint64_t>, indType>(__VA_ARGS__); \
		default: \
			UNSUPPORT(funcName, dataType, returnType); \
		} \
	} while (0)

#pragma region set av values at positions
template <typename T, typename TInd>
inline int vectorSetValuesAt(void* dst, const void* value, const TInd* pos, const size_t posN)
{
	T* a = (T*)dst;
	const T v = *((const T*)value);
	auto iter = thrust::make_permutation_iterator(a, pos);
	thrust::fill(THRUST_PAR, iter, iter + posN, v);
	return 0;
}
#pragma endregion

#pragma region dense vector prune to sparse vector
// dense vector prune to sparse vector -- get buffer size
template <typename T, typename TInd>
inline ptrdiff_t vecPruneBuffer(const size_t N)
{
	size_t res = sizeof(TInd) * N; // max size for possible indices
	res += sizeof(T) * N; // size for temporary values
	return res;
}

// dense vector prune to sparse vector -- get non-zeros
template <typename T, typename TInd>
inline size_t vectorPruneNonZeros(const void* av, const void* threshold, const size_t N, void* buffer)
{
	const T* a = (const T*)av;

	// create result container
	TInd* idxOut = (TInd*)buffer;
	T* valOut = (T*)(N + (TInd*)buffer);

	// make zip
	auto zipBegin = thrust::make_zip_iterator(thrust::make_tuple(thrust::make_counting_iterator(0), a));
	auto resultBegin = thrust::make_zip_iterator(thrust::make_tuple(idxOut, valOut));

	// copy_if to get sparse indexes
	auto resultEnd = resultBegin;
	if constexpr (std::is_scalar_v<T>)
	{
		const T thre = std::abs(*((const T*)threshold));
		resultEnd = thrust::copy_if(THRUST_PAR, zipBegin, zipBegin + N, a, resultBegin, [=] PREFIX(const T a) { return a > thre; });
	}
	else
	{
		const typename real_type<T>::type threAbs = std::abs(*((const T*)threshold));
		resultEnd = thrust::copy_if(THRUST_PAR, zipBegin, zipBegin + N, a, resultBegin, [=] PREFIX(const T a) { return a > threAbs; });
	}
	return resultEnd - resultBegin;
}

// dense vector prune to sparse vector -- no buffer
template <typename T, typename TInd>
inline ptrdiff_t vectorPruneDirect(const void* av, const void* threshold, const size_t N, TInd* idxOut, void* valOutv, const bool safeOut, const size_t outN)
{
	const T* a = (const T*)av;

	// create result container
	T* valOut = (T*)valOutv;

	// make zip
	auto zipBegin = thrust::make_zip_iterator(thrust::make_tuple(thrust::make_counting_iterator((size_t)0), a));
	auto resultBegin = thrust::make_zip_iterator(thrust::make_tuple(idxOut, valOut));

	// copy_if to get sparse indexes
	auto tempBegin = thrust::make_zip_iterator(thrust::make_tuple(thrust::make_discard_iterator(), thrust::make_discard_iterator()));
	if constexpr (std::is_scalar_v<T>)
	{
		const T thre = std::abs(*((const T*)threshold));
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
		const typename real_type<T>::type threAbs = std::abs(*((const T*)threshold));
		if (safeOut)
		{
			auto diff = thrust::copy_if(THRUST_PAR, zipBegin, zipBegin + N, a, tempBegin, [=] PREFIX(const T a) { return a > threAbs; }) - tempBegin;
			if (diff > outN)
				return diff - outN;
		}
		thrust::copy_if(THRUST_PAR, zipBegin, zipBegin + N, a, resultBegin, [=] PREFIX(const T a) { return a > threAbs; });
	}
	return 0;
}

// dense vector prune to sparse vector -- calculate
template <typename T, typename TInd>
inline ERROR_RETURN vecPruneCalculate(const void* buffer, const size_t N, size_t nnz, TInd* indexOut, void* valueOut)
{
	// get result container from buffer
	const TInd* idxOut = (TInd*)buffer;
	const T* valOut = (const T*)(N + (const TInd*)buffer);

	// copy to output arrays
#ifdef CPU
	memcpy(indexOut, idxOut, sizeof(TInd) * nnz);
	memcpy(valueOut, valOut, sizeof(T) * nnz);
	return 0;
#else
	cudaError err = cudaMemcpy(indexOut, idxOut, sizeof(TInd) * nnz, cudaMemcpyDeviceToDevice);
	if (err != 0) return err;
	err = cudaMemcpy(valueOut, valOut, sizeof(T) * nnz, cudaMemcpyDeviceToDevice);
	if (err != 0) return err;
	// return
	return err;
#endif // CPU
}

#pragma endregion

#pragma region sparse vector add sparse vector
// sparse vector add another sparse vector -- get buffer size
template <typename T, typename TInd>
inline ptrdiff_t vectorSpAddBuffer(const size_t nnzA, const size_t nnzB)
{
	size_t N = nnzA + nnzB;
	size_t res = sizeof(TInd) * N; // size for temporary indices
	res += sizeof(T) * N; // size for temporary values
	return res;
}

// sparse vector add another sparse vector -- get non-zeros, 'alpha' is the number to multiply to each value of B
template <typename T, typename TInd>
inline size_t vectorSparseAddGetNonzero(const TInd* indA, const void* valAv, const size_t nnzA, const TInd* indB, const void* valBv, const size_t nnzB, const void* alphav, void* buffer)
{
	// cast
	const T* valA = (const T*)valAv;
	const T* valB = (const T*)valBv;
	const T alpha = *((const T*)alphav);

	size_t nnz = nnzA + nnzB;
	// get storage from buffer for the combined contents of sparse vectors A and B
	TInd* temp_index = (TInd*)buffer;
	T* temp_value = (T*)(nnz + (TInd*)buffer);

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
	auto func = [] PREFIX(const T a, const T b) -> TInd { return a == b ? 0 : 1; };
	size_t nnzC = thrust::inner_product(THRUST_PAR, temp_index, temp_index + nnz - 1, temp_index + 1, TInd(0), plus_functor<TInd>(), func);
	nnzC += 1;

	// return
	return nnzC;
}

// sparse vector add another sparse vector -- calculate
template <typename T, typename TInd>
inline int vectorSparseAddCalculate(const void* buffer, size_t nnzAB, size_t nnzC, TInd* C_indexOut, void* C_valueOut)
{
	// cast
	T* C_value = (T*)C_valueOut;
	// get storage from buffer for the combined contents of sparse vectors A and B
	const TInd* temp_index = (const TInd*)buffer;
	const T* temp_value = (const T*)(nnzAB + (const TInd*)buffer);

	// sum values with the same index
	thrust::reduce_by_key(THRUST_PAR, temp_index, temp_index + nnzAB, temp_value, C_indexOut, C_value, thrust::equal_to<TInd>(), plus_functor<T>());
	return 0;
}
#pragma endregion

#pragma region dense vector added by sparse
// dense[index[i]] = sparse[i] * alpha + dense[index[i]]
template <typename T, typename TInd>
inline void vectorDenseAddBySparse(void* densev, const void* sparsev, const TInd* index, const size_t nnz, const void* alphav)
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
#pragma endregion

#pragma region sparse vectors outer product to COOC matrix
auto count_iter = thrust::make_counting_iterator((size_t)0);

template <typename T, typename TInd, bool conj>
struct sparseVectorsOuter_functor
{
	const T* valA; const TInd* indA; const size_t nnzA;
	const T* valB; const TInd* indB;
	T* valC; TInd* rowC; TInd* colC;

	sparseVectorsOuter_functor(
		const T* valA, const TInd* indA, const size_t nnzA,
		const T* valB, const TInd* indB,
		T* valC, TInd* rowC, TInd* colC) :
		valA(valA), indA(indA), nnzA(nnzA),
		valB(valB), indB(indB),
		valC(valC), rowC(rowC), colC(colC)
	{}

	PREFIX void operator()(const size_t idx) const
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

template <typename T, typename TInd>
inline int sparseVectorsOuterCheck()
{
	return 0;
}

template <typename T, typename TInd>
inline int sparseVectorsOuter(
	const void* valAv, const TInd* indA, const size_t nnzA,
	const void* valBv, const TInd* indB, const size_t nnzB,
	void* valCv, TInd* rowC, TInd* colC, const bool conj)
{
	const T* valA = (const T*)valAv;
	const T* valB = (const T*)valBv;
	T* valC = (T*)valCv;

#define SPARSE_VECTOR_OUTER_CODE(bool1) thrust::for_each_n(THRUST_PAR, count_iter, nnzA * nnzB, sparseVectorsOuter_functor<T, TInd, bool1>(valA, indA, nnzA, valB, indB, valC, rowC, colC))

	if (conj)
		SPARSE_VECTOR_OUTER_CODE(true);
	else
		SPARSE_VECTOR_OUTER_CODE(false);

	return 0;
}
#pragma endregion

#pragma region sparse COO format matrices Kronecker
template <typename T, typename TInd>
struct CooMatricesKronecker_functor
{
	const T* valA; const TInd* rowA; const TInd* colA;
	const T* valB; const TInd* rowB; const TInd* colB; const size_t nnzB; const size_t rowsB; const size_t colsB;
	T* valC; TInd* rowC; TInd* colC;

	CooMatricesKronecker_functor(
		const T* valA, const TInd* rowA, const TInd* colA,
		const T* valB, const TInd* rowB, const TInd* colB, const size_t nnzB, const size_t rowsB, const size_t colsB,
		T* valC, TInd* rowC, TInd* colC) :
		valA(valA), rowA(rowA), colA(colA),
		valB(valA), rowB(rowA), colB(colB), nnzB(nnzB), rowsB(rowsB), colsB(colsB),
		valC(valC), rowC(rowC), colC(colC)
	{}

	PREFIX void operator()(const size_t idx) const
	{
		const size_t n = idx / nnzB, m = idx % nnzB;
		rowC[idx] = rowA[n] * rowsB + rowB[m];
		colC[idx] = colA[n] * colsB + colB[m];
		valC[idx] = valA[n] * valB[m];
	}
};

template <typename TInd>
struct cooMatrixSortByColumn_functor
{
	PREFIX bool operator()(const thrust::tuple<TInd, TInd> lhs, const thrust::tuple<TInd, TInd> rhs) const
	{
		if (lhs.get<1>() < rhs.get<1>())
			return true;
		else if (lhs.get<1>() == rhs.get<1>())
			return lhs.get<0>() < rhs.get<0>();
		else
			return false;
	}
};

template <typename T, typename TInd>
inline int cooMatricesKronecker(
	const void* valAv, const TInd* rowA, const TInd* colA, const size_t nnzA,
	const void* valBv, const TInd* rowB, const TInd* colB, const size_t nnzB, const size_t rowsB, const size_t colsB,
	void* valCv, TInd* rowC, TInd* colC)
{
	const T* valA = (const T*)valAv;
	const T* valB = (const T*)valBv;
	T* valC = (T*)valCv;
	const size_t nnzC = nnzA * nnzB;

	// outer
	thrust::for_each_n(THRUST_PAR, count_iter, nnzC, CooMatricesKronecker_functor<T, TInd>(valA, rowA, colA, valB, rowB, colB, nnzB, rowsB, colsB, valC, rowC, colC));
	// sort column wise
	auto rowColC = thrust::make_zip_iterator(thrust::make_tuple(rowC, colC));
	thrust::sort_by_key(THRUST_PAR, rowColC, rowColC + nnzC, valC, cooMatrixSortByColumn_functor<TInd>());
	return 0;
}
#pragma endregion

#pragma endregion


#ifdef TInd
#pragma region export int 32 functions
DLLEXP int vecSetValAt(const DataType type, void* a, const void* value, const MKL_INT* pos, const size_t posN)
{
	AUTO_ALLTYPE_IND_FUNC(vectorSetValuesAt, type, MKL_INT, int, a, value, pos, posN);
}

DLLEXP ptrdiff_t vecPruneBuffer(const DataType type, const size_t N)
{
	AUTO_ALLTYPE_IND_FUNC(vecPruneBuffer, type, MKL_INT, ptrdiff_t, N);
}

DLLEXP size_t vecPruneNnz(const DataType type, const void* a, const void* threshold, const size_t N, void* buffer)
{
	AUTO_ALLTYPE_IND_FUNC(vectorPruneNonZeros, type, MKL_INT, size_t, a, threshold, N, buffer);
}

DLLEXP ptrdiff_t vecPruneDirect(const DataType type, const void* a, const void* threshold, const size_t N, MKL_INT* idxOut, void* valOut, const bool safe, const size_t nnz)
{
	AUTO_ALLTYPE_IND_FUNC(vectorPruneDirect, type, MKL_INT, ptrdiff_t, a, threshold, N, idxOut, valOut, safe, nnz);
}

DLLEXP ERROR_RETURN vecPruneCal(const DataType type, const size_t N, const void* buffer, size_t nnz, MKL_INT* indexOut, void* valueOut)
{
	AUTO_ALLTYPE_IND_FUNC(vecPruneCalculate, type, MKL_INT, ERROR_RETURN, buffer, N, nnz, indexOut, valueOut);
}

DLLEXP ptrdiff_t vecSpAddBuffer(const DataType type, const size_t nnzA, const size_t nnzB)
{
	AUTO_ALLTYPE_IND_FUNC(vectorSpAddBuffer, type, MKL_INT, ptrdiff_t, nnzA, nnzB);
}

DLLEXP size_t vecSpAddNnz(const DataType type,
	const MKL_INT* indA, const void* valA, const size_t nnzA,
	const MKL_INT* indB, const void* valB, const size_t nnzB,
	const void* alpha, void* buffer)
{
	AUTO_ALLTYPE_IND_FUNC(vectorSparseAddGetNonzero, type, MKL_INT, size_t, indA, valA, nnzA, indB, valB, nnzB, alpha, buffer);
}

DLLEXP int vecSpAddCal(const DataType type, const void* buffer, size_t nnzAB, size_t nnzC, MKL_INT* C_index, void* C_value)
{
	AUTO_ALLTYPE_IND_FUNC(vectorSparseAddCalculate, type, MKL_INT, int, buffer, nnzAB, nnzC, C_index, C_value);
}

DLLEXP void vecDnAddSp(const DataType type, void* dense, const void* sparse, const MKL_INT* index, const size_t nnz, const void* alpha)
{
	AUTO_ALLTYPE_IND_FUNC(vectorDenseAddBySparse, type, MKL_INT, void, dense, sparse, index, nnz, alpha);
}

DLLEXP void spVecIdxToCooIdxs(const MKL_INT* index, MKL_INT* rowIdx, MKL_INT* colIdx, const size_t N, const int ld)
{
	const int lld = ld;
	thrust::transform(THRUST_PAR, index, index + N, rowIdx, [=] PREFIX(const int x) { return x % lld; });
	thrust::transform(THRUST_PAR, index, index + N, colIdx, [=] PREFIX(const int x) { return x / lld; });
}

DLLEXP void cooIdxsToSpVecIdx(MKL_INT* index, const MKL_INT* rowIdx, const MKL_INT* colIdx, const size_t N, const int ld)
{
	const int lld = ld;
	thrust::transform(THRUST_PAR, rowIdx, rowIdx + N, colIdx, index, [=] PREFIX(const int x, const int y) { return x + y * lld; });
}

DLLEXP int spVecOuterCheck(const extblas::DataType type)
{
	AUTO_ALLTYPE_IND_FUNC(sparseVectorsOuterCheck, type, MKL_INT, int);
}

DLLEXP int spVecOuter(const extblas::DataType type,
	const void* valA, const MKL_INT* indA, const size_t nnzA,
	const void* valB, const MKL_INT* indB, const size_t nnzB,
	void* valC, MKL_INT* rowC, MKL_INT* colC, const bool conj)
{
	AUTO_ALLTYPE_IND_FUNC(sparseVectorsOuter, type, MKL_INT, int, valA, indA, nnzA, valB, indB, nnzB, valC, rowC, colC, conj);
}
DLLEXP int cooMatKron(const extblas::DataType type,
	const void* valA, const MKL_INT* rowA, const MKL_INT* colA, const size_t nnzA,
	const void* valB, const MKL_INT* rowB, const MKL_INT* colB, const size_t nnzB, const size_t rowsB, const size_t colsB,
	void* valC, MKL_INT* rowC, MKL_INT* colC)
{
	AUTO_ALLTYPE_IND_FUNC(cooMatricesKronecker, type, MKL_INT, int, valA, rowA, colA, nnzA, valB, rowB, colB, nnzB, rowsB, colsB, valC, rowC, colC);
}
#pragma endregion
#else

#pragma region export int 32 functions
DLLEXP int vecSetValAt_i32(const DataType type, void* a, const void* value, const int* pos, const size_t posN)
{
	AUTO_ALLTYPE_IND_FUNC(vectorSetValuesAt, type, int, int, a, value, pos, posN);
}

DLLEXP ptrdiff_t vecPruneBuffer_i32(const DataType type, const size_t N)
{
	AUTO_ALLTYPE_IND_FUNC(vecPruneBuffer, type, int, ptrdiff_t, N);
}

DLLEXP size_t vecPruneNnz_i32(const DataType type, const void* a, const void* threshold, const size_t N, void* buffer)
{
	AUTO_ALLTYPE_IND_FUNC(vectorPruneNonZeros, type, int, size_t, a, threshold, N, buffer);
}

DLLEXP ptrdiff_t vecPruneDirect_i32(const DataType type, const void* a, const void* threshold, const size_t N, int* idxOut, void* valOut, const bool safe, const size_t nnz)
{
	AUTO_ALLTYPE_IND_FUNC(vectorPruneDirect, type, int, ptrdiff_t, a, threshold, N, idxOut, valOut, safe, nnz);
}

DLLEXP ERROR_RETURN vecPruneCal_i32(const DataType type, const size_t N, const void* buffer, size_t nnz, int* indexOut, void* valueOut)
{
	AUTO_ALLTYPE_IND_FUNC(vecPruneCalculate, type, int, ERROR_RETURN, buffer, N, nnz, indexOut, valueOut);
}

DLLEXP ptrdiff_t vecSpAddBuffer_i32(const DataType type, const size_t nnzA, const size_t nnzB)
{
	AUTO_ALLTYPE_IND_FUNC(vectorSpAddBuffer, type, int, ptrdiff_t, nnzA, nnzB);
}

DLLEXP size_t vecSpAddNnz_i32(const DataType type,
	const int* indA, const void* valA, const size_t nnzA,
	const int* indB, const void* valB, const size_t nnzB,
	const void* alpha, void* buffer)
{
	AUTO_ALLTYPE_IND_FUNC(vectorSparseAddGetNonzero, type, int, size_t, indA, valA, nnzA, indB, valB, nnzB, alpha, buffer);
}

DLLEXP int vecSpAddCal_i32(const DataType type, const void* buffer, size_t nnzAB, size_t nnzC, int* C_index, void* C_value)
{
	AUTO_ALLTYPE_IND_FUNC(vectorSparseAddCalculate, type, int, int, buffer, nnzAB, nnzC, C_index, C_value);
}

DLLEXP void vecDnAddSp_i32(const DataType type, void* dense, const void* sparse, const int* index, const size_t nnz, const void* alpha)
{
	AUTO_ALLTYPE_IND_FUNC(vectorDenseAddBySparse, type, int, void, dense, sparse, index, nnz, alpha);
}

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

DLLEXP int spVecOuterCheck_i32(const extblas::DataType type)
{
	AUTO_ALLTYPE_IND_FUNC(sparseVectorsOuterCheck, type, int, int);
}

DLLEXP int spVecOuter_i32(const extblas::DataType type,
	const void* valA, const int* indA, const size_t nnzA,
	const void* valB, const int* indB, const size_t nnzB,
	void* valC, int* rowC, int* colC, const bool conj)
{
	AUTO_ALLTYPE_IND_FUNC(sparseVectorsOuter, type, int, int, valA, indA, nnzA, valB, indB, nnzB, valC, rowC, colC, conj);
}
DLLEXP int cooMatKron_i32(const extblas::DataType type,
	const void* valA, const int* rowA, const int* colA, const size_t nnzA,
	const void* valB, const int* rowB, const int* colB, const size_t nnzB, const size_t rowsB, const size_t colsB,
	void* valC, int* rowC, int* colC)
{
	AUTO_ALLTYPE_IND_FUNC(cooMatricesKronecker, type, int, int, valA, rowA, colA, nnzA, valB, rowB, colB, nnzB, rowsB, colsB, valC, rowC, colC);
}
#pragma endregion


#pragma region export int 64 functions
DLLEXP int vecSetValAt_i64(const DataType type, void* a, const void* value, const long long* pos, const size_t posN)
{
	AUTO_ALLTYPE_IND_FUNC(vectorSetValuesAt, type, long long, int, a, value, pos, posN);
}

DLLEXP ptrdiff_t vecPruneBuffer_i64(const DataType type, const size_t N)
{
	AUTO_ALLTYPE_IND_FUNC(vecPruneBuffer, type, long long, ptrdiff_t, N);
}

DLLEXP size_t vecPruneNnz_i64(const DataType type, const void* a, const void* threshold, const size_t N, void* buffer)
{
	AUTO_ALLTYPE_IND_FUNC(vectorPruneNonZeros, type, long long, size_t, a, threshold, N, buffer);
}

DLLEXP ptrdiff_t vecPruneDirect_i64(const DataType type, const void* a, const void* threshold, const size_t N, long long* idxOut, void* valOut, const bool safe, const size_t nnz)
{
	AUTO_ALLTYPE_IND_FUNC(vectorPruneDirect, type, long long, ptrdiff_t, a, threshold, N, idxOut, valOut, safe, nnz);
}

DLLEXP ERROR_RETURN vecPruneCal_i64(const DataType type, const size_t N, const void* buffer, size_t nnz, long long* indexOut, void* valueOut)
{
	AUTO_ALLTYPE_IND_FUNC(vecPruneCalculate, type, long long, ERROR_RETURN, buffer, N, nnz, indexOut, valueOut);
}

DLLEXP ptrdiff_t vecSpAddBuffer_i64(const DataType type, const size_t nnzA, const size_t nnzB)
{
	AUTO_ALLTYPE_IND_FUNC(vectorSpAddBuffer, type, long long, ptrdiff_t, nnzA, nnzB);
}

DLLEXP size_t vecSpAddNnz_i64(const DataType type,
	const long long* indA, const void* valA, const size_t nnzA,
	const long long* indB, const void* valB, const size_t nnzB,
	const void* alpha, void* buffer)
{
	AUTO_ALLTYPE_IND_FUNC(vectorSparseAddGetNonzero, type, long long, size_t, indA, valA, nnzA, indB, valB, nnzB, alpha, buffer);
}

DLLEXP int vecSpAddCal_i64(const DataType type, const void* buffer, size_t nnzAB, size_t nnzC, long long* C_index, void* C_value)
{
	AUTO_ALLTYPE_IND_FUNC(vectorSparseAddCalculate, type, long long, int, buffer, nnzAB, nnzC, C_index, C_value);
}

DLLEXP void vecDnAddSp_i64(const DataType type, void* dense, const void* sparse, const long long* index, const size_t nnz, const void* alpha)
{
	AUTO_ALLTYPE_IND_FUNC(vectorDenseAddBySparse, type, long long, void, dense, sparse, index, nnz, alpha);
}

DLLEXP void spVecIdxToCooIdxs_i64(const long long* index, long long* rowIdx, long long* colIdx, const size_t N, const int ld)
{
	const int lld = ld;
	thrust::transform(THRUST_PAR, index, index + N, rowIdx, [=] PREFIX(const int x) { return x % lld; });
	thrust::transform(THRUST_PAR, index, index + N, colIdx, [=] PREFIX(const int x) { return x / lld; });
}

DLLEXP void cooIdxsToSpVecIdx_i64(long long* index, const long long* rowIdx, const long long* colIdx, const size_t N, const int ld)
{
	const int lld = ld;
	thrust::transform(THRUST_PAR, rowIdx, rowIdx + N, colIdx, index, [=] PREFIX(const int x, const int y) { return x + y * lld; });
}

DLLEXP int spVecOuterCheck_i64(const extblas::DataType type)
{
	AUTO_ALLTYPE_IND_FUNC(sparseVectorsOuterCheck, type, long long, int);
}

DLLEXP int spVecOuter_i64(const extblas::DataType type,
	const void* valA, const long long* indA, const size_t nnzA,
	const void* valB, const long long* indB, const size_t nnzB,
	void* valC, long long* rowC, long long* colC, const bool conj)
{
	AUTO_ALLTYPE_IND_FUNC(sparseVectorsOuter, type, long long, int, valA, indA, nnzA, valB, indB, nnzB, valC, rowC, colC, conj);
}
DLLEXP int cooMatKron_i64(const extblas::DataType type,
	const void* valA, const long long* rowA, const long long* colA, const size_t nnzA,
	const void* valB, const long long* rowB, const long long* colB, const size_t nnzB, const size_t rowsB, const size_t colsB,
	void* valC, long long* rowC, long long* colC)
{
	AUTO_ALLTYPE_IND_FUNC(cooMatricesKronecker, type, long long, int, valA, rowA, colA, nnzA, valB, rowB, colB, nnzB, rowsB, colsB, valC, rowC, colC);
}
#pragma endregion

#endif // MKL_INT
