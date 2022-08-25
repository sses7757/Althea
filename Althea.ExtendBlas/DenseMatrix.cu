#include "extblas.h"
using namespace extblas;


#pragma region template
template <typename TIn, typename TOut, typename Func>
inline int matrixConvertInner(const TIn* src, TOut* dst, const size_t m, const size_t n, const size_t ldSrc, const size_t ldDst, Func func)
{
	auto ssrc = make_leadDim_range(src, m, n, ldSrc);
	auto sdst = make_leadDim_range(dst, m, n, ldDst);
	if (ldSrc == m && ldDst == m)
	{
		thrust::transform(THRUST_PAR, src, src + m * n, dst, func);
	}
	else if (ldSrc == m && ldDst != m)
	{
		thrust::transform(THRUST_PAR, src, src + m * n, sdst.begin(), func);
	}
	else if (ldSrc != m && ldDst == m)
	{
		thrust::transform(THRUST_PAR, ssrc.begin(), ssrc.end(), dst, func);
	}
	else
	{
		thrust::transform(THRUST_PAR, ssrc.begin(), ssrc.end(), sdst.begin(), func);
	}
	return 0;
}

template <typename T1, typename T2, typename TOut, typename Func>
inline int matrixConvertInner(const T1* a, const T2* b, TOut* dst, const size_t m, const size_t n, const size_t ldA, const size_t ldB, const size_t ldDst, Func func)
{
	auto sa = make_leadDim_range(a, m, n, ldA);
	auto sb = make_leadDim_range(b, m, n, ldB);
	auto sdst = make_leadDim_range(dst, m, n, ldDst);
	if (ldA == m && ldB == m && ldDst == m)
		thrust::transform(THRUST_PAR, a, a + m * n, b, dst, func);
	else if (ldA == m && ldB == m && ldDst != m)
		thrust::transform(THRUST_PAR, a, a + m * n, b, sdst.begin(), func);
	else if (ldA == m && ldB != m && ldDst == m)
		thrust::transform(THRUST_PAR, a, a + m * n, sb.begin(), sdst.begin(), func);
	else if (ldA == m && ldB != m && ldDst != m)
		thrust::transform(THRUST_PAR, a, a + m * n, sb.begin(), sdst.begin(), func);
	else if (ldA != m && ldB == m && ldDst == m)
		thrust::transform(THRUST_PAR, sa.begin(), sa.end(), b, dst, func);
	else if (ldA != m && ldB == m && ldDst != m)
		thrust::transform(THRUST_PAR, sa.begin(), sa.end(), b, sdst.begin(), func);
	else if (ldA != m && ldB != m && ldDst == m)
		thrust::transform(THRUST_PAR, sa.begin(), sa.end(), sb.begin(), dst, func);
	else
		thrust::transform(THRUST_PAR, sa.begin(), sa.end(), sb.begin(), sdst.begin(), func);
	return 0;
}

template <typename T, typename Ret, typename Func>
inline int matrixReduceInner(const T* src, const size_t m, const size_t n, const size_t ld, Ret* init, Func func)
{
	auto ssrc = make_leadDim_range(src, m, n, ld);
	if (ld == m)
	{
		*init = thrust::reduce(THRUST_PAR, src, src + m * n, *init, func);
	}
	else
	{
		*init = thrust::reduce(THRUST_PAR, ssrc.begin(), ssrc.end(), *init, func);
	}
	return 0;
}

template <typename TIn, typename TOut, typename Func>
inline int matrixScanInner(bool inclusive, const TIn* src, TOut* dst, const size_t m, const size_t n, const size_t ldSrc, const size_t ldDst, TOut* init, Func func)
{
	auto ssrc = make_leadDim_range(src, m, n, ldSrc);
	auto sdst = make_leadDim_range(dst, m, n, ldDst);
	if (ldSrc == m && ldDst == m)
	{
		if (inclusive)
			thrust::inclusive_scan(THRUST_PAR, src, src + m * n, dst, func);
		else
			thrust::exclusive_scan(THRUST_PAR, src, src + m * n, dst, *init, func);
	}
	else if (ldSrc == m && ldDst != m)
	{
		if (inclusive)
			thrust::inclusive_scan(THRUST_PAR, src, src + m * n, sdst.begin(), func);
		else
			thrust::exclusive_scan(THRUST_PAR, src, src + m * n, sdst.begin(), *init, func);
	}
	else if (ldSrc != m && ldDst == m)
	{
		if (inclusive)
			thrust::inclusive_scan(THRUST_PAR, ssrc.begin(), ssrc.end(), dst, func);
		else
			thrust::exclusive_scan(THRUST_PAR, ssrc.begin(), ssrc.end(), dst, *init, func);
	}
	else
	{
		if (inclusive)
			thrust::inclusive_scan(THRUST_PAR, ssrc.begin(), ssrc.end(), sdst.begin(), func);
		else
			thrust::exclusive_scan(THRUST_PAR, ssrc.begin(), ssrc.end(), sdst.begin(), *init, func);
	}
	return 0;
}

#pragma endregion


#pragma region strided copy
template <typename T>
inline int matrixStridedCopy(const void* srcv, void* dstv, const size_t m, const size_t n, const size_t ldSrc, const size_t ldDst)
{
	if (ldSrc == m && ldDst == m)
	{
#ifdef CPU
		memcpy(dstv, srcv, n * sizeof(T));
		return 0;
#else
		auto err = cudaMemcpy(dstv, srcv, n * sizeof(T), cudaMemcpyDeviceToDevice);
		if (err == cudaError::cudaErrorNotSupported)
			return -1;
		else
			return (int)err;
#endif // CPU
	}
	const T* src = (const T*)srcv;
	T* dst = (T*)dstv;
	auto ssrc = make_leadDim_range(src, m, n, ldDst);
	auto sdst = make_leadDim_range(dst, m, n, ldDst);
	if (ldSrc == m && ldDst != m)
	{
		thrust::copy_n(THRUST_PAR, src, m * n, sdst.begin());
	}
	else if (ldSrc != m && ldDst == m)
	{
		thrust::copy_n(THRUST_PAR, ssrc.begin(), m * n, dst);
	}
	else
	{
		thrust::copy_n(THRUST_PAR, ssrc.begin(), m * n, sdst.begin());
	}
	return ERROR_RETURN{};
}

DLLEXP
int matStridedCopy(const DataType type, const size_t m, const size_t n, const void* src, const size_t ldSrc, void* dst, const size_t ldDst)
{
	AUTO_ALLTYPE_FUNC(matrixStridedCopy, type, int, src, dst, m, n, ldSrc, ldDst);
}
#pragma endregion

#pragma region data type cast
template <typename RealIn, typename RealOut>
inline int matrixComplexToReal(const void* srcv, void* dstv, const size_t m, const size_t n, const size_t ldSrc, const size_t ldDst, bool toRealByAbs)
{
	const complex<RealIn>* src = (const complex<RealIn>*)srcv;
	RealOut* dst = (RealOut*)dstv;
	if (toRealByAbs)
	{
		auto func = [] PREFIX(const complex<RealIn> s) { return (RealOut)std::abs(s); };
		return matrixConvertInner(src, dst, m, n, ldSrc, ldDst, func);
	}
	else
	{
		auto func = [] PREFIX(const complex<RealIn> s) { return (RealOut)s.real(); };
		return matrixConvertInner(src, dst, m, n, ldSrc, ldDst, func);
	}
}

template <typename RealIn, typename RealOut>
inline int matrixRealToComplex(const void* srcv, void* dstv, const size_t m, const size_t n, const size_t ldSrc, const size_t ldDst, bool toRealByAbs)
{
	const RealIn* src = (const RealIn*)srcv;
	complex<RealOut>* dst = (complex<RealOut>*)dstv;
	auto func = [] PREFIX(const RealIn s) { return complex<RealOut>{(RealOut)s}; };
	return matrixConvertInner(src, dst, m, n, ldSrc, ldDst, func);
}

template <typename RealIn, typename RealOut>
inline int matrixRealConvert(const void* srcv, void* dstv, const size_t m, const size_t n, const size_t ldSrc, const size_t ldDst, bool toRealByAbs)
{
	const RealIn* src = (const RealIn*)srcv;
	RealOut* dst = (RealOut*)dstv;
	auto func = [] PREFIX(const RealIn s) { return (RealOut)s; };
	return matrixConvertInner(src, dst, m, n, ldSrc, ldDst, func);
}

template <typename RealIn, typename RealOut>
inline int matrixComplexConvert(const void* srcv, void* dstv, const size_t m, const size_t n, const size_t ldSrc, const size_t ldDst, bool toRealByAbs)
{
	const complex<RealIn>* src = (const complex<RealIn>*)srcv;
	complex<RealOut>* dst = (complex<RealOut>*)dstv;
	auto func = [] PREFIX(const complex<RealIn> s) { return complex<RealOut>{(RealOut)s.real(), (RealOut)s.imag()}; };
	return matrixConvertInner(src, dst, m, n, ldSrc, ldDst, func);
}

DLLEXP
int matDataConvert(const DataType srcType, const DataType dstType, bool toRealByAbs, const size_t m, const size_t n, const void* src, const size_t ldSrc, void* dst, const size_t ldDst)
{
	// copy if no data conversion
	if (srcType == dstType)
	{
		AUTO_ALLTYPE_FUNC(matrixStridedCopy, srcType, int, src, dst, m, n, ldSrc, ldDst);
		// return is inside the auto generated switch
	}

	// define inner switch
#define CONVERT_INNER_SWITCH(type, convert) do { \
		switch (dstType) \
		{ \
		case RealFloat32: \
		case ComplexFloat32: \
			convertFunc = convert<type, float>; break; \
		case RealFloat64: \
		case ComplexFloat64: \
			convertFunc = convert<type, double>; break; \
		case RealInt8: \
		case ComplexInt8: \
			convertFunc = convert<type, char>; break; \
		case RealInt16: \
		case ComplexInt16: \
			convertFunc = convert<type, short>; break; \
		case RealInt32: \
		case ComplexInt32: \
			convertFunc = convert<type, int>; break; \
		case RealInt64: \
		case ComplexInt64: \
			convertFunc = convert<type, long long>; break; \
		case RealUInt8: \
		case ComplexUInt8: \
			convertFunc = convert<type, unsigned char>; break; \
		case RealUInt16: \
		case ComplexUInt16: \
			convertFunc = convert<type, unsigned short>; break; \
		case RealUInt32: \
		case ComplexUInt32: \
			convertFunc = convert<type, size_t>; break; \
		case RealUInt64: \
		case ComplexUInt64: \
			convertFunc = convert<type, unsigned long long>; break; \
		default: \
			UNSUPPORT(matDataConvert, dstType, int); \
		} \
	} while (0)
// define outer switch
#define CONVERT_OUTER_SWITCH(convert) do { \
		switch (srcType) \
		{ \
		case RealFloat32: \
		case ComplexFloat32: \
			CONVERT_INNER_SWITCH(float, convert); break; \
		case RealFloat64: \
		case ComplexFloat64: \
			CONVERT_INNER_SWITCH(double, convert); break; \
		case RealInt8: \
		case ComplexInt8: \
			CONVERT_INNER_SWITCH(char, convert); break; \
		case RealInt16: \
		case ComplexInt16: \
			CONVERT_INNER_SWITCH(short, convert); break; \
		case RealInt32: \
		case ComplexInt32: \
			CONVERT_INNER_SWITCH(short, convert); break; \
		case RealInt64: \
		case ComplexInt64: \
			CONVERT_INNER_SWITCH(short, convert); break; \
		case RealUInt8: \
		case ComplexUInt8: \
			CONVERT_INNER_SWITCH(short, convert); break; \
		case RealUInt16: \
		case ComplexUInt16: \
			CONVERT_INNER_SWITCH(short, convert); break; \
		case RealUInt32: \
		case ComplexUInt32: \
			CONVERT_INNER_SWITCH(short, convert); break; \
		case RealUInt64: \
		case ComplexUInt64: \
			CONVERT_INNER_SWITCH(short, convert); break; \
		default: \
			UNSUPPORT(matDataConvert, srcType, int); \
		} \
	} while (0)

	// the convert function
	int (*convertFunc)(const void* src, void* dst, const size_t m, const size_t n, const size_t ldSrc, const size_t ldDst, bool toRealByAbs);
	if (is_real(srcType) && is_real(dstType))
	{	// real convert
		CONVERT_OUTER_SWITCH(matrixRealConvert);
	}
	else if (is_real(srcType))
	{	// real to complex
		CONVERT_OUTER_SWITCH(matrixRealToComplex);
	}
	else if (is_real(dstType))
	{	// complex to real, 'toRealByAbs' is only used here
		CONVERT_OUTER_SWITCH(matrixComplexToReal);
	}
	else
	{	// all complex
		CONVERT_OUTER_SWITCH(matrixComplexConvert);
	}
#undef CONVERT_OUTER_SWITCH
#undef CONVERT_INNER_SWITCH
	// calculate
	convertFunc(src, dst, m, n, ldSrc, ldDst, toRealByAbs);
}
#pragma endregion

#pragma region fill with value
template<typename T>
inline int matrixFillWith(void* av, const void* valv, const size_t m, const size_t n, const size_t ld)
{
	const T val = *(const T*)valv;
	T* a = (T*)av;
	if (ld == m)
	{
		thrust::fill_n(THRUST_PAR, a, m * n, val);
	}
	else
	{
		auto sa = make_leadDim_range(a, m, n, ld);
		thrust::fill_n(THRUST_PAR, sa.begin(), m * n, val);
	}
	return 0;
}

DLLEXP
int matFillVal(const DataType type, const size_t m, const size_t n, const void* val, void* a, const size_t ld)
{
	AUTO_ALLTYPE_FUNC(matrixFillWith, type, int, a, val, m, n, ld);
}
#pragma endregion

#pragma region equal
template<typename T>
inline int matrixsEqual(const void* av, const void* bv, const size_t m, const size_t n, const size_t ldA, const size_t ldB, bool& eqs)
{
	if (av == bv && ldA == ldB)
	{
		eqs = true;
		return 0;
	}
	const T* a = (const T*)av;
	const T* b = (const T*)bv;
	auto sA = make_leadDim_range(a, m, n, ldA);
	auto sB = make_leadDim_range(b, m, n, ldB);
	auto eqfunc = equals_functor<T>();
	if (ldA == m && ldB == m)
	{
		eqs = thrust::equal(THRUST_PAR, a, a + m * n, b, eqfunc);
	}
	else if (ldA == m && ldB != m)
	{
		eqs = thrust::equal(THRUST_PAR, a, a + m * n, sB.begin(), eqfunc);
	}
	else if (ldA != m && ldB == m)
	{
		eqs = thrust::equal(THRUST_PAR, sA.begin(), sA.end(), b, eqfunc);
	}
	else
	{
		eqs = thrust::equal(THRUST_PAR, sA.begin(), sA.end(), sB.begin(), eqfunc);
	}
	return 0;
}

DLLEXP
int matsEq(const DataType type, const size_t m, const size_t n, const void* a, const size_t ldA, const void* b, const size_t ldB, bool& eqs)
{
	AUTO_ALLTYPE_FUNC(matrixsEqual, type, int, a, b, m, n, ldA, ldB, eqs);
}
#pragma endregion

#pragma region unary without scalar
template <typename T>
inline int matrixUnary(const unaryOp::UnaryOperation op, const void* srcv, void* dstv, const size_t m, const size_t n, const size_t ldSrc, const size_t ldDst)
{
	const T* src = (const T*)srcv;
	T* dst = (T*)dstv;
#define __SWITCH(invoke, ...) do { \
	switch (op) \
	{ \
	case unaryOp::UnaryOperation::AbsoluteValue: \
	{ \
		auto func = [] PREFIX(const T v) { return (T)std::abs(v); }; \
		return invoke(__VA_ARGS__, func); \
	} \
	case unaryOp::UnaryOperation::Conjugate: \
	{ \
		if constexpr (std::is_scalar_v<T>) \
			return -1; \
		auto func = [] PREFIX(const T v) { return std::conj(v); }; \
		return invoke(__VA_ARGS__, func); \
	} \
	case unaryOp::UnaryOperation::Negate: \
	{ \
		auto func = [] PREFIX(const T v) { return -v; }; \
		return invoke(__VA_ARGS__, func); \
	} \
	default: \
		return -1; \
	} \
} while (0)

	__SWITCH(matrixConvertInner, src, dst, m, n, ldSrc, ldDst);
#undef __SWITCH
}

DLLEXP
int matUnary(const DataType type, const unaryOp::UnaryOperation op, const size_t m, const size_t n, const void* src, const size_t ldSrc, void* dst, const size_t ldDst)
{
	AUTO_ALLTYPE_FUNC(matrixUnary, type, int, op, src, dst, m, n, ldSrc, ldDst);
}
#pragma endregion

#pragma region binary scalar
template <typename T>
inline int matrixBinaryScalar(const binaryOp::BinaryOperation op, const void* scalarv, const void* srcv, void* dstv, const size_t m, const size_t n, const size_t ldSrc, const size_t ldDst)
{
	const T scalar = *(const T*)scalarv;
	const T* src = (const T*)srcv;
	T* dst = (T*)dstv;
#define __SWITCH(invoke, ...) do { \
	switch (op) \
	{ \
	case binaryOp::BinaryOperation::Add: \
	{ \
		auto func = [=] PREFIX(const T v) { return v + scalar; }; \
		return invoke(__VA_ARGS__, func); \
	} \
	case binaryOp::BinaryOperation::Multiply: \
	{ \
		auto func = [=] PREFIX(const T v) { return v * scalar; }; \
		return invoke(__VA_ARGS__, func); \
	} \
	case binaryOp::BinaryOperation::Divide: \
	{ \
		auto func = [=] PREFIX(const T v) { return v / scalar; }; \
		return invoke(__VA_ARGS__, func); \
	} \
	case binaryOp::BinaryOperation::Power: \
	{ \
		auto func = [=] PREFIX(const T v) { return std::pow(v, scalar); }; \
		return invoke(__VA_ARGS__, func); \
	} \
	} \
	if constexpr (std::is_scalar_v<T>) \
	{ \
		const T scalarAbs = std::abs(scalar); \
		switch (op) \
		{ \
		case binaryOp::BinaryOperation::AbsoluteMaximum: \
		{ \
			auto func = abslarger_functor<T, T>(scalarAbs); \
			return invoke(__VA_ARGS__, func); \
		} \
		case binaryOp::BinaryOperation::AbsoluteMininum: \
		{ \
			auto func = abssmaller_functor<T, T>(scalarAbs); \
			return invoke(__VA_ARGS__, func); \
		} \
		case binaryOp::BinaryOperation::Maximum: \
		{ \
			auto func = larger_functor<T>(scalar); \
			return invoke(__VA_ARGS__, func); \
		} \
		case binaryOp::BinaryOperation::Mininum: \
		{ \
			auto func = smaller_functor<T>(scalar); \
			return invoke(__VA_ARGS__, func); \
		} \
		case binaryOp::BinaryOperation::Truncate: \
		{ \
			auto func = truncate_functor<T, T>(scalarAbs); \
			return invoke(__VA_ARGS__, func); \
		} \
		default: \
			return -1; \
		} \
	} \
	else \
	{ \
		const typename T::value_type scalarAbs = std::abs(scalar); \
		switch (op) \
		{ \
		case binaryOp::BinaryOperation::AbsoluteMaximum: \
		{ \
			auto func = abslarger_functor<T, typename T::value_type>(scalarAbs); \
			return invoke(__VA_ARGS__, func); \
		} \
		case binaryOp::BinaryOperation::AbsoluteMininum: \
		{ \
			auto func = abssmaller_functor<T, typename T::value_type>(scalarAbs); \
			return invoke(__VA_ARGS__, func); \
		} \
		case binaryOp::BinaryOperation::Truncate: \
		{ \
			auto func = truncate_functor<T, typename T::value_type>(scalarAbs); \
			return invoke(__VA_ARGS__, func); \
		} \
		default: \
			return -1; \
		} \
	} \
} while (0)

	__SWITCH(matrixConvertInner, src, dst, m, n, ldSrc, ldDst);
#undef __SWITCH
}

DLLEXP
int matBinaryScalar(const DataType type, const binaryOp::BinaryOperation op, const void* scalar, const size_t m, const size_t n, const void* src, const size_t ldSrc, void* dst, const size_t ldDst)
{
	AUTO_ALLTYPE_FUNC(matrixBinaryScalar, type, int, op, scalar, src, dst, m, n, ldSrc, ldDst);
}
#pragma endregion

#pragma region binary
template <typename T>
inline int matrixsBinary(const binaryOp::BinaryOperation op, const void* av, const void* bv, void* dstv, const size_t m, const size_t n, const size_t ldA, const size_t ldB, const size_t ldDst)
{
	const T* a = (const T*)av, * b = (const T*)bv;
	T* dst = (T*)dstv;
#define __SWITCH(invoke, ...) do { \
	switch (op) \
	{ \
	case binaryOp::BinaryOperation::AbsoluteMaximum: \
	{ \
		auto func = [=] PREFIX(const T v1, const T v2) { return std::abs(v1) > std::abs(v2) ? (T)std::abs(v1) : (T)std::abs(v2); }; \
		return invoke(__VA_ARGS__, func); \
	} \
	case binaryOp::BinaryOperation::AbsoluteMininum: \
	{ \
		auto func = [=] PREFIX(const T v1, const T v2) { return std::abs(v1) < std::abs(v2) ? (T)std::abs(v1) : (T)std::abs(v2); }; \
		return invoke(__VA_ARGS__, func); \
	} \
	case binaryOp::BinaryOperation::Add: \
	{ \
		auto func = [=] PREFIX(const T v1, const T v2) { return v1 + v2; }; \
		return invoke(__VA_ARGS__, func); \
	} \
	case binaryOp::BinaryOperation::Maximum: \
	{ \
		if constexpr (!std::is_scalar_v<T>) \
			return -1; \
		else \
		{ \
			auto func = largerOne_functor<T>(); \
			return invoke(__VA_ARGS__, func); \
		} \
	} \
	case binaryOp::BinaryOperation::Mininum: \
	{ \
		if constexpr (!std::is_scalar_v<T>) \
			return -1; \
		else \
		{ \
			auto func = smallerOne_functor<T>(); \
			return invoke(__VA_ARGS__, func); \
		} \
	} \
	case binaryOp::BinaryOperation::Multiply: \
	{ \
		auto func = [=] PREFIX(const T v1, const T v2) { return v1 * v2; }; \
		return invoke(__VA_ARGS__, func); \
	} \
	case binaryOp::BinaryOperation::Power: \
	{ \
		auto func = [=] PREFIX(const T v, const T p) { return std::pow(v, p); }; \
		return invoke(__VA_ARGS__, func); \
	} \
	case binaryOp::BinaryOperation::Divide: \
	{ \
		auto func = [=] PREFIX(const T v1, const T v2) { return v1 / v2; }; \
		return invoke(__VA_ARGS__, func); \
	} \
	default: \
		return -1; \
	} \
} while (0)
	__SWITCH(matrixConvertInner, a, b, dst, m, n, ldA, ldB, ldDst);
#undef __SWITCH
}

DLLEXP
int matsBinary(const DataType type, const binaryOp::BinaryOperation op, const size_t m, const size_t n, const void* a, const size_t ldA, const void* b, const size_t ldB, void* dst, const size_t ldDst)
{
	AUTO_ALLTYPE_FUNC(matrixsBinary, type, int, op, a, b, dst, m, n, ldA, ldB, ldDst);
}
#pragma endregion

#pragma region norm
template<typename T>
inline int matrixNorm(const void* srcv, const size_t m, const size_t n, const size_t ld, void* result)
{
	const T* src = (const T*)srcv;
	auto ssrc = make_leadDim_range(src, m, n, ld);
	if (ld == m)
	{
		if constexpr (std::is_scalar_v<T>)
			*((T*)result) = thrust::inner_product(THRUST_PAR, src, src + m * n, src, T{});
		else
			*((typename T::value_type*)result) = thrust::inner_product(THRUST_PAR, src, src + m * n, src, typename T::value_type{}, plus_functor<typename T::value_type>(), norm_functor<T>());
	}
	else
	{
		if constexpr (std::is_scalar_v<T>)
			*((T*)result) = thrust::inner_product(THRUST_PAR, ssrc.begin(), ssrc.end(), ssrc.begin(), T{});
		else
			*((typename T::value_type*)result) = thrust::inner_product(THRUST_PAR, ssrc.begin(), ssrc.end(), ssrc.begin(), typename T::value_type{}, plus_functor<typename T::value_type>(), norm_functor<T>());
	}
	if constexpr (std::is_scalar_v<T>)
		*((T*)result) = std::sqrt(*((T*)result));
	else
		*((typename T::value_type*)result) = std::sqrt(*((typename T::value_type*)result));
	return 0;
}
#pragma endregion

#pragma region arg reduce
template <typename T>
inline int matrixArgReduce(const reduceOp::ReduceOperation op, const void* srcv, const size_t m, const size_t n, const size_t ldSrc, size_t& result)
{
#define __SWITCH(invoke, srcbeg, ...) do { \
	switch (op) \
	{ \
	case reduceOp::ReduceOperation::AbsoluteMaximum: \
	{ \
		auto func = [=] PREFIX(const T v1, const T v2) { return std::abs(v1) > std::abs(v2); }; \
		result = invoke(__VA_ARGS__, func) - srcbeg; \
		break; \
	} \
	case reduceOp::ReduceOperation::AbsoluteMininum: \
	{ \
		auto func = [=] PREFIX(const T v1, const T v2) { return std::abs(v1) < std::abs(v2); }; \
		result = invoke(__VA_ARGS__, func) - srcbeg; \
		break; \
	} \
	case reduceOp::ReduceOperation::Maximum: \
	{ \
		if constexpr (!std::is_scalar_v<T>) \
			return -1; \
		else \
		{ \
			auto func = largerThan_functor<T>(); \
			result = invoke(__VA_ARGS__, func) - srcbeg; \
		} \
		break; \
	} \
	case reduceOp::ReduceOperation::Mininum: \
	{ \
		if constexpr (!std::is_scalar_v<T>) \
			return -1; \
		else \
		{ \
			auto func = smallerThan_functor<T>(); \
			result = invoke(__VA_ARGS__, func) - srcbeg; \
		} \
		break; \
	} \
	default: \
		return -1; \
	} \
} while (0)

	const T* src = (const T*)srcv;
	auto ssrc = make_leadDim_range(src, m, n, ldSrc);
	if (ldSrc == m)
	{
		__SWITCH(thrust::max_element, src, THRUST_PAR, src, src + m * n);
	}
	else
	{
		__SWITCH(thrust::max_element, ssrc.begin(), THRUST_PAR, ssrc.begin(), ssrc.end());
	}
	return 0;
#undef __SWITCH
}

DLLEXP
int matArgReduce(const DataType type, const reduceOp::ReduceOperation op, const size_t m, const size_t n, const void* src, const size_t ldSrc, size_t& result)
{
	AUTO_ALLTYPE_FUNC(matrixArgReduce, type, int, op, src, m, n, ldSrc, result);
}
#pragma endregion

#pragma region unary aggregate
#define REDUCE_FUNC(invoke, init, ...) do { \
	*init = T{}; \
	switch (op) \
	{ \
	case reduceOp::ReduceOperation::AbsoluteMaximum: \
	{ \
		auto func = [=] PREFIX(const T v1, const T v2) { return std::abs(v1) > std::abs(v2) ? (T)std::abs(v1) : (T)std::abs(v2); }; \
		*init = neginf<T>(); \
		return invoke(__VA_ARGS__, init, func); \
	} \
	case reduceOp::ReduceOperation::AbsoluteMininum: \
	{ \
		auto func = [=] PREFIX(const T v1, const T v2) { return std::abs(v1) < std::abs(v2) ? (T)std::abs(v1) : (T)std::abs(v2); }; \
		*init = inf<T>(); \
		return invoke(__VA_ARGS__, init, func); \
	} \
	case reduceOp::ReduceOperation::Add: \
	{ \
		auto func = [=] PREFIX(const T v1, const T v2) { return v1 + v2; }; \
		return invoke(__VA_ARGS__, init, func); \
	} \
	case reduceOp::ReduceOperation::AddAbsolute: \
	{ \
		auto func = [=] PREFIX(const T v1, const T v2) { return std::abs(v1) + std::abs(v2); }; \
		return invoke(__VA_ARGS__, init, func); \
	} \
	case reduceOp::ReduceOperation::Maximum: \
	{ \
		if constexpr (!std::is_scalar_v<T>) \
			return -1; \
		else \
		{ \
			auto func = largerOne_functor<T>(); \
			*init = neginf<T>(); \
			return invoke(__VA_ARGS__, init, func); \
		} \
	} \
	case reduceOp::ReduceOperation::Mininum: \
	{ \
		if constexpr (!std::is_scalar_v<T>) \
			return -1; \
		else \
		{ \
			auto func = smallerOne_functor<T>(); \
			*init = inf<T>(); \
			return invoke(__VA_ARGS__, init, func); \
		} \
	} \
	case reduceOp::ReduceOperation::Multiply: \
	{ \
		auto func = [=] PREFIX(const T v1, const T v2) { return v1 * v2; }; \
		*init = T{ 1 }; \
		return invoke(__VA_ARGS__, init, func); \
	} \
	case reduceOp::ReduceOperation::MultiplyAbsolute: \
	{ \
		auto func = [=] PREFIX(const T v1, const T v2) { return std::abs(v1) * std::abs(v2); }; \
		*init = T{ 1 }; \
		return invoke(__VA_ARGS__, init, func); \
	} \
	default: \
		return -1; \
	} \
} while (0)

template <typename T>
inline int matrixUnaryReduce(const reduceOp::ReduceOperation op, const void* srcv, const size_t m, const size_t n, const size_t ldSrc, void* result)
{
	const T* src = (const T*)srcv;
	T* res = (T*)result;
	REDUCE_FUNC(matrixReduceInner, res, src, m, n, ldSrc);
}

DLLEXP
int matUnaryReduce(const DataType type, const reduceOp::ReduceOperation op, const size_t m, const size_t n, const void* src, const size_t ldSrc, void* result)
{
	if (op == reduceOp::ReduceOperation::Norm)
	{
		AUTO_ALLTYPE_FUNC(matrixNorm, type, int, src, m, n, ldSrc, result);
	}
	AUTO_ALLTYPE_FUNC(matrixUnaryReduce, type, int, op, src, m, n, ldSrc, result);
}
#pragma endregion

#pragma region scan
template<typename T>
inline int matrixScan(const reduceOp::ReduceOperation op, bool inclusive, const void* srcv, void* dstv, const size_t m, const size_t n, const size_t ldSrc, const size_t ldDst)
{
	const T* src = (const T*)srcv;
	T* dst = (T*)dstv;
	T init__{};
	T* init = &init__;
	REDUCE_FUNC(matrixScanInner, init, inclusive, src, dst, m, n, ldSrc, ldDst);
}

DLLEXP
int matScan(const DataType type, const reduceOp::ReduceOperation op, bool inclusive, const size_t m, const size_t n, const void* src, const size_t ldSrc, void* dst, const size_t ldDst)
{
	AUTO_ALLTYPE_FUNC(matrixScan, type, int, op, inclusive, src, dst, m, n, ldSrc, ldDst);
}
#pragma endregion


#pragma region dense matrices Kronecker
// Ignore spelling: \mathbb \times \otimes
//tex: The number of cache miss for $A\in \mathbb{R}^{N\times N} \otimes B\in \mathbb{R}^{N\times N} = C\in \mathbb{R}^{N^2\times N^2}$ is: $\\$
// 1. $O(N^2+N)$ for contiguously access $C$ $\\$
// 2. $O(N^2)$ for contiguously access $B$ $\\$ 

// The Kronecker product of two matrices can be achieved by
//	1. outer product of two matrices' column matrices
//	2. reshape the matrix to a proper rank-4 tensor
//	3. permute the tensor [3,1,4,2] (may be)
//	4. reshape the tensor to the output matrix

template <typename T, bool largerLeadDim, bool hasAlpha, bool hasBeta>
struct kronecker_functor
{
	const T alpha, beta;
	const size_t ldA, ldB, colsB, ldD, rowsD;
	const T* A;
	const T* B;
	T* D;

	kronecker_functor(const T alpha, const T beta, const size_t ldA, const size_t ldB, const size_t colsB, const size_t ldD, const size_t rowsD, const T* A, const T* B, T* D) :
		alpha(alpha), beta(beta), ldA(ldA), ldB(ldB), colsB(colsB), ldD(ldD), rowsD(rowsD), A(A), B(B), D(D) {}

	PREFIX void operator()(const size_t indD) const
	{
		// get offsets
		const size_t rowD = indD / rowsD, colD = indD % rowsD;
		const size_t offsetA = (rowD / ldB) + (colD / colsB) * ldA,
			offsetB = (rowD % ldB) + (colD % colsB) * ldB;
		size_t offsetD;
		if constexpr (largerLeadDim)
		{
			offsetD = ldD * rowD + colD;
		}
		else
		{
			offsetD = indD;
		}
		// multiply
		if constexpr (hasAlpha && hasBeta)
			D[offsetD] = alpha * A[offsetA] * B[offsetB] + beta * D[offsetD];
		if constexpr (hasAlpha && !hasBeta)
			D[offsetD] = alpha * A[offsetA] * B[offsetB];
		if constexpr (!hasAlpha && hasBeta)
			D[offsetD] = A[offsetA] * B[offsetB] + beta * D[offsetD];
		if constexpr (!hasAlpha && !hasBeta)
			D[offsetD] = A[offsetA] * B[offsetB];
	}
};

template<typename T>
inline void matricesKronecker(
	const void* Av, const size_t ldA, const size_t rowsA, const size_t colsA,
	const void* Bv, const size_t ldB, const size_t rowsB, const size_t colsB,
	void* destv, const size_t ldD, const void* alphav, const void* betav)
{
	// cast
	const T* A = (const T*)Av;
	const T* B = (const T*)Bv;
	T* D = (T*)destv;
	const T alpha = *((const T*)alphav);
	const T beta = *((const T*)betav);

	const unsigned int rowsD = rowsA * rowsB;
	const unsigned int colsD = colsA * colsB;

#define KRON_CODE(bool1, bool2, bool3) thrust::for_each_n(THRUST_PAR, thurst::make_counting_iterator((size_t)0), rowsD * colsD, kronecker_functor<T, bool1, bool2, bool3>(alpha, beta, ldA, ldB, colsB, ldD, rowsD, A, B, D))

	if (rowsD == ldD)
	{
		if (alpha == T{1} && beta == T{0})
			KRON_CODE(false, false, false);
		else if (alpha == T{1})
			KRON_CODE(false, false, true);
		else if (beta == T{0})
			KRON_CODE(false, true, false);
		else
			KRON_CODE(false, true, true);
	}
	else
	{
		if (alpha == T{1} && beta == T{0})
			KRON_CODE(true, false, false);
		else if (alpha == T{1})
			KRON_CODE(true, false, true);
		else if (beta == T{0})
			KRON_CODE(true, true, false);
		else
			KRON_CODE(true, true, true);
	}
}

DLLEXP
void matKron(const extblas::DataType type,
	const void* A, const size_t ldA, const size_t rowsA, const size_t colsA,
	const void* B, const size_t ldB, const size_t rowsB, const size_t colsB,
	void* dest, const size_t ldD, const void* alpha, const void* beta)
{
	AUTO_ALLTYPE_FUNC(matricesKronecker, type, void, A, ldA, rowsA, colsA, B, ldB, rowsB, colsB, dest, ldD, alpha, beta);
}
#pragma endregion


/*
#pragma region make matrix Hermitian by copying its upper part to/from its lower part
template <typename T, bool upper>
struct makeHerm_functor2
{
	const size_t ld;
	T* A;

	// used for compute the actual row and column position
	const double onePlus2NFloat16, onePlus2NSquare;
	const size_t TwoNMinusOne;
	// Ignore Spelling: lfloor
	//tex:Since for number of rows $n$, column index $c$ and iteration index $i$: $$\sum_{i=0}^c (n - i) = \frac12 (1 + c)(2n - c)$$
	//We have $$c = \left\lfloor \frac{1}{2} \left( 2n+1 - \sqrt{(2 n+1)^2-8 i} \right) \right\rfloor = \left\lfloor \frac{1}{2} (1+2 n) \left(1-\sqrt{1-\frac{8 i}{(1+2 n)^2}}\right)\right\rfloor$$
	//The latter one is better for float point computation and will be correct if $n < $ (1 << 27 = 134,217,728) (half the precision of double).$\\$
	//I use the float point instead of integer square root since "the fastest ISQRT() algorithm by far is to go through the FPU."$\\$
	//The row index is then:
	//$$r = \frac12(c^2-2 c n+c+2 i-2) = i - 1 - \frac12 c (2n - 1 - c)$$

	makeHerm_functor2(const size_t ld, T* A) :
		ld(ld), A(A),
		TwoNMinusOne(2 * ld - 1),
		onePlus2NFloat16(0.5 * (2 * ld + 1)),
		onePlus2NSquare((2 * ld + 1) * (double)(2 * ld + 1))
	{}

	PREFIX void operator()(const size_t ind) const
	{
		// get offset
		const size_t col = (size_t)(onePlus2NFloat16 * (1.0 - std::sqrt(1.0 - 8 * ind / onePlus2NSquare)));
		const size_t row = ind - 1 - (col * (TwoNMinusOne - col)) / 2;
		const size_t offsetLower = row + col * ld, offsetUpper = col + row * ld;
		// copy
		if constexpr (std::is_scalar<T>::value)
		{
			if constexpr (upper)
				A[offsetLower] = A[offsetUpper];
			else
				A[offsetUpper] = A[offsetLower];
		}
		else
		{
			if (row == col)
			{
				A[offsetLower] = T(A[offsetLower].real());
			}
			else
			{
				if constexpr (upper)
					A[offsetLower] = std::conj(A[offsetUpper]);
				else
					A[offsetUpper] = std::conj(A[offsetLower]);
			}
		}
	}
};

template <typename T, bool upper, bool makeHerm>
struct makeHerm_functor
{
	const size_t ld, rows;
	T* A;

	makeHerm_functor(const size_t ld, const size_t rows, T* A) :
		ld(ld), rows(rows), A(A)
	{}

	PREFIX void operator()(const size_t ind) const
	{
		// get offset
		const lldiv_t div = std::lldiv(ind, rows);
		const size_t row = div.rem, col = div.quot;
		const size_t offsetLower = row + col * ld, offsetUpper = col + row * ld;
		// copy
		if constexpr (upper)
		{
			if (row > col)
				return;
		}
		else
		{
			if (row < col)
				return;
		}
		if constexpr (makeHerm)
		{
			if (row == col)
			{
				if constexpr (std::is_scalar<T>::value)
					A[offsetLower] = T(A[offsetLower].real());
				return;
			}
			if constexpr (upper)
				A[offsetLower] = std::conj(A[offsetUpper]);
			else
				A[offsetUpper] = std::conj(A[offsetLower]);
		}
		else
		{
			if constexpr (upper)
				A[offsetLower] = A[offsetUpper];
			else
				A[offsetUpper] = A[offsetLower];
		}
	}
};

template <typename T, bool clearLower>
struct clearPart_functor
{
	const size_t ld, rows;
	T* A;

	clearPart_functor(const size_t ld, const size_t rows, T* A) :
		ld(ld), rows(rows), A(A)
	{}

	PREFIX void operator()(const size_t ind) const
	{
		// get offset
		const lldiv_t div = std::lldiv(ind, rows);
		const size_t row = div.rem, col = div.quot;
		// set
		if constexpr (clearLower)
		{
			if (row >= col)
				return;
		}
		else
		{
			if (row <= col)
				return;
		}
		A[row + col * ld] = T{};
	}
};

template<typename T>
void matrixMakeHermitian2(void* Av, const size_t ld, const size_t rows, bool upperStored)
{
	T* A = (T*)Av;
#define MAKE_HERM_CODE(bool1) thrust::for_each_n(THRUST_PAR, count_iter, (rows * (rows + 1)) / 2, makeHerm_functor2<T, bool1>(ld, A))
	if (upperStored)
		MAKE_HERM_CODE(true);
	else
		MAKE_HERM_CODE(false);
#undef MAKE_HERM_CODE
}

template<typename T>
void matrixMakeHermitian(void* Av, const size_t ld, const size_t rows, bool upperStored, bool hermA)
{
	T* A = (T*)Av;
#define MAKE_HERM_CODE(bool1, bool2) thrust::for_each_n(THRUST_PAR, count_iter, rows * rows, makeHerm_functor<T, bool1, bool2>(ld, rows, A))
	if (upperStored && hermA)
		MAKE_HERM_CODE(true, true);
	else if (upperStored && !hermA)
		MAKE_HERM_CODE(true, false);
	else if (!upperStored && hermA)
		MAKE_HERM_CODE(false, true);
	else
		MAKE_HERM_CODE(false, false);
#undef MAKE_HERM_CODE
}

template<typename T>
void matrixClearTriangular(void* Av, const size_t ld, const size_t rows, bool clearLower)
{
	T* A = (T*)Av;

#define MAKE_HERM_CODE(bool1) thrust::for_each_n(THRUST_PAR, count_iter, rows * rows, clearPart_functor<T, bool1>(ld, rows, A))
	if (clearLower)
		MAKE_HERM_CODE(true);
	else
		MAKE_HERM_CODE(false);
#undef MAKE_HERM_CODE
}

DLLEXP
void matMakeHerm(const extblas::DataType type, void* A, const size_t ld, const size_t rows, bool upperStored, bool hermA)
{
	AUTO_SIGNED_TYPE_FUNC(matrixMakeHermitian, type, void, A, ld, rows, upperStored, hermA);
}

DLLEXP
void matTriClear(const extblas::DataType type, void* A, const size_t ld, const size_t rows, bool clearLower)
{
	AUTO_SIGNED_TYPE_FUNC(matrixClearTriangular, type, void, A, ld, rows, clearLower);
}
#pragma endregion
*/