#include "extblas.h"
using namespace extblas;

#pragma region template
template <typename TIn, typename TOut, typename Func>
inline int vectorConvert(const TIn* src, TOut* dst, const size_t n, const size_t s1, const size_t s2, Func func)
{
	auto ssrc = make_strided_range(src, n, s1);
	auto sdst = make_strided_range(dst, n, s2);
	if (s1 == 1 && s2 == 1)
	{
		thrust::transform(THRUST_PAR, src, src + n, dst, func);
	}
	else if (s1 == 1 && s2 != 1)
	{
		thrust::transform(THRUST_PAR, src, src + n, sdst.begin(), func);
	}
	else if (s1 != 1 && s2 == 1)
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
inline int vectorConvert(const T1* a, const T2* b, TOut* dst, const size_t n, const size_t s1, const size_t s2, const size_t sd, Func func)
{
	auto sa = make_strided_range(a, n, s1);
	auto sb = make_strided_range(b, n, s2);
	auto sdst = make_strided_range(dst, n, sd);
	if (s1 == 1 && s2 == 1 && sd == 1)
		thrust::transform(THRUST_PAR, a, a + n, b, dst, func);
	else if (s1 == 1 && s2 == 1 && sd != 1)
		thrust::transform(THRUST_PAR, a, a + n, b, sdst.begin(), func);
	else if (s1 == 1 && s2 != 1 && sd == 1)
		thrust::transform(THRUST_PAR, a, a + n, sb.begin(), sdst.begin(), func);
	else if (s1 == 1 && s2 != 1 && sd != 1)
		thrust::transform(THRUST_PAR, a, a + n, sb.begin(), sdst.begin(), func);
	else if (s1 != 1 && s2 == 1 && sd == 1)
		thrust::transform(THRUST_PAR, sa.begin(), sa.end(), b, dst, func);
	else if (s1 != 1 && s2 == 1 && sd != 1)
		thrust::transform(THRUST_PAR, sa.begin(), sa.end(), b, sdst.begin(), func);
	else if (s1 != 1 && s2 != 1 && sd == 1)
		thrust::transform(THRUST_PAR, sa.begin(), sa.end(), sb.begin(), dst, func);
	else
		thrust::transform(THRUST_PAR, sa.begin(), sa.end(), sb.begin(), sdst.begin(), func);
	return 0;
}

template <typename T, typename Ret, typename Func>
inline Ret vectorReduce(const T* src, const size_t n, const size_t stride, const T init, Func func)
{
	auto ssrc = make_strided_range(src, n, stride);
	if (stride == 1)
	{
		return thrust::reduce(THRUST_PAR, src, src + n, init, func);
	}
	else
	{
		return thrust::reduce(THRUST_PAR, ssrc.begin(), ssrc.end(), init, func);
	}
	return 0;
}

template <typename TIn, typename TOut, typename Func>
inline int vectorScan(const bool inclusive, const TIn* src, TOut* dst, const size_t n, const size_t s1, const size_t s2, const TOut init, Func func)
{
	auto ssrc = make_strided_range(src, n, s1);
	auto sdst = make_strided_range(dst, n, s2);
	if (s1 == 1 && s2 == 1)
	{
		if (inclusive)
			thrust::inclusive_scan(THRUST_PAR, src, src + n, dst, func);
		else
			thrust::exclusive_scan(THRUST_PAR, src, src + n, dst, init, func);
	}
	else if (s1 == 1 && s2 != 1)
	{
		if (inclusive)
			thrust::inclusive_scan(THRUST_PAR, src, src + n, sdst.begin(), func);
		else
			thrust::exclusive_scan(THRUST_PAR, src, src + n, sdst.begin(), init, func);
	}
	else if (s1 != 1 && s2 == 1)
	{
		if (inclusive)
			thrust::inclusive_scan(THRUST_PAR, ssrc.begin(), ssrc.end(), dst, func);
		else
			thrust::exclusive_scan(THRUST_PAR, ssrc.begin(), ssrc.end(), dst, init, func);
	}
	else
	{
		if (inclusive)
			thrust::inclusive_scan(THRUST_PAR, ssrc.begin(), ssrc.end(), sdst.begin(), func);
		else
			thrust::exclusive_scan(THRUST_PAR, ssrc.begin(), ssrc.end(), sdst.begin(), init, func);
	}
	return 0;
}

#pragma endregion


#pragma region strided copy
template <typename T>
inline int vectorStridedCopy(const void* srcv, void* dstv, const size_t n, const size_t strideSrc, const size_t strideDst)
{
	if (strideSrc == 1 && strideDst == 1)
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
	auto ssrc = make_strided_range(src, n, strideDst);
	auto sdst = make_strided_range(dst, n, strideDst);
	if (strideSrc == 1 && strideDst != 1)
	{
		thrust::copy_n(THRUST_PAR, src, n, sdst.begin());
	}
	else if (strideSrc != 1 && strideDst == 1)
	{
		thrust::copy_n(THRUST_PAR, ssrc.begin(), n, dst);
	}
	else
	{
		thrust::copy_n(THRUST_PAR, ssrc.begin(), n, sdst.begin());
	}
	return ERROR_RETURN{};
}

DLLEXP
int vecStridedCopy(const DataType type, const size_t n, const void* src, const size_t strideSrc, void* dst, const size_t strideDst)
{
	AUTO_ALLTYPE_FUNC(vectorStridedCopy, type, int, src, dst, n, strideSrc, strideDst);
}
#pragma endregion

#pragma region data type cast
template <typename RealIn, typename RealOut>
inline int vectorComplexToReal(const void* srcv, void* dstv, const size_t n, const size_t s1, const size_t s2, const bool toRealByAbs)
{
	const complex<RealIn>* src = (const complex<RealIn>*)srcv;
	RealOut* dst = (RealOut*)dstv;
	if (toRealByAbs)
	{
		auto func = [] PREFIX(const complex<RealIn> s) { return (RealOut)std::abs(s); };
		return vectorConvert(src, dst, n, s1, s2, func);
	}
	else
	{
		auto func = [] PREFIX(const complex<RealIn> s) { return (RealOut)s.real(); };
		return vectorConvert(src, dst, n, s1, s2, func);
	}
}

template <typename RealIn, typename RealOut>
inline int vectorRealToComplex(const void* srcv, void* dstv, const size_t n, const size_t s1, const size_t s2, const bool toRealByAbs)
{
	const RealIn* src = (const RealIn*)srcv;
	complex<RealOut>* dst = (complex<RealOut>*)dstv;
	auto func = [] PREFIX(const RealIn s) { return complex<RealOut>{(RealOut)s}; };
	return vectorConvert(src, dst, n, s1, s2, func);
}

template <typename RealIn, typename RealOut>
inline int vectorRealConvert(const void* srcv, void* dstv, const size_t n, const size_t s1, const size_t s2, const bool toRealByAbs)
{
	const RealIn* src = (const RealIn*)srcv;
	RealOut* dst = (RealOut*)dstv;
	auto func = [] PREFIX(const RealIn s) { return (RealOut)s; };
	return vectorConvert(src, dst, n, s1, s2, func);
}

DLLEXP
int vecDataConvert(const DataType srcType, const DataType dstType, const bool toRealByAbs, const size_t n, const void* src, const size_t strideSrc, void* dst, const size_t strideDst)
{
	// copy if no data conversion
	if (srcType == dstType)
	{
		AUTO_ALLTYPE_FUNC(vectorStridedCopy, srcType, int, src, dst, n, strideSrc, strideDst);
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
			UNSUPPORT(vecDataConvert, dstType, int); \
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
			UNSUPPORT(vecDataConvert, srcType, int); \
		} \
	} while (0)

	// the convert function
	int (*convertFunc)(const void* src, void* dst, const size_t n, const size_t strideSrc, const size_t strideDst, const bool toRealByAbs);
	if (is_real(srcType) && is_real(dstType))
	{	// real convert
		CONVERT_OUTER_SWITCH(vectorRealConvert);
	}
	else if (is_real(srcType))
	{	// real to complex
		CONVERT_OUTER_SWITCH(vectorRealToComplex);
	}
	else if (is_real(dstType))
	{	// complex to real, 'toRealByAbs' is only used here
		CONVERT_OUTER_SWITCH(vectorComplexToReal);
	}
	else
	{	// all complex, use the real convert of each part instead
		if (strideSrc == 1 && strideDst == 1)
		{
			return vecDataConvert(real_correspond(srcType), real_correspond(dstType), true, n * 2, src, 1, dst, 1);
		}
		else
		{
			// the real parts
			auto ret1 = vecDataConvert(real_correspond(srcType), real_correspond(dstType), true, n * 2, src, strideSrc * 2, dst, strideDst * 2);
			if (ret1 != 0)
				return ret1;
			// increase pointers
			const int sizeSrc = size(srcType), sizeDst = size(dstType);
			const void* srcInc = (const char*)src + sizeSrc;
			void* dstInc = (char*)dst + sizeDst;
			// the imaginary parts
			return vecDataConvert(real_correspond(srcType), real_correspond(dstType), true, n * 2, srcInc, strideSrc * 2, dstInc, strideDst * 2);
		}
	}
#undef CONVERT_OUTER_SWITCH
#undef CONVERT_INNER_SWITCH
	// calculate
	convertFunc(src, dst, n, strideSrc, strideDst, toRealByAbs);
}
#pragma endregion

#pragma region fill with value
template<typename T>
inline int vectorFillWith(void* av, const void* valv, const size_t n, const size_t stride)
{
	const T val = *(const T*)valv;
	T* a = (T*)av;
	if (stride == 1)
	{
		thrust::fill_n(THRUST_PAR, a, n, val);
	}
	else
	{
		auto sa = make_strided_range(a, n, stride);
		thrust::fill_n(THRUST_PAR, sa.begin(), n, val);
	}
	return 0;
}

DLLEXP
int vecFillVal(const DataType type, const size_t n, const void* val, void* a, const size_t stride)
{
	AUTO_ALLTYPE_FUNC(vectorFillWith, type, int, a, val, n, stride);
}
#pragma endregion

#pragma region equal
template<typename T>
inline int vectorsEqual(const void* av, const void* bv, const size_t n, const size_t sa, const size_t sb, bool& eqs)
{
	if (av == bv && sa == sb)
	{
		eqs = true;
		return 0;
	}
	const T* a = (const T*)av;
	const T* b = (const T*)bv;
	auto strideA = make_strided_range(a, n, sa);
	auto strideB = make_strided_range(b, n, sb);
	if (sa == 1 && sb == 1)
	{
		eqs = thrust::equal(THRUST_PAR, a, a + n, b, [] PREFIX(const T x, const T y) { return x == y; });
	}
	else if (sa == 1 && sb != 1)
	{
		eqs = thrust::equal(THRUST_PAR, a, a + n, strideB.begin(), [] PREFIX(const T x, const T y) { return x == y; });
	}
	else if (sa != 1 && sb == 1)
	{
		eqs = thrust::equal(THRUST_PAR, strideA.begin(), strideA.end(), b, [] PREFIX(const T x, const T y) { return x == y; });
	}
	else
	{
		eqs = thrust::equal(THRUST_PAR, strideA.begin(), strideA.end(), strideB.begin(), [] PREFIX(const T x, const T y) { return x == y; });
	}
	return 0;
}

DLLEXP
int vecsEq(const DataType type, const size_t n, const void* a, const size_t strideA, const void* b, const size_t strideB, bool& eqs)
{
	AUTO_ALLTYPE_FUNC(vectorsEqual, type, int, a, b, n, strideA, strideB, eqs);
}
#pragma endregion

#pragma region unary without scalar
template <typename T>
inline int vectorUnary(const unaryOp::UnaryOperation op, const void* srcv, void* dstv, const size_t n, const size_t strideSrc, const size_t strideDst)
{
	FUNC<T(T)> func;
	switch (op)
	{
	case unaryOp::UnaryOperation::AbsoluteValue:
		func = [] PREFIX(const T v) { return (T)std::abs(v); };
		break;
	case unaryOp::UnaryOperation::Conjugate:
		if constexpr (std::is_scalar_v<T>)
			return -1;
		func = [] PREFIX(const T v) { return std::conj(v); };
		break;
	case unaryOp::UnaryOperation::Negate:
		func = [] PREFIX(const T v) { return -v; };
		break;
	default:
		return -1;
	}
	const T* src = (const T*)srcv;
	T* dst = (T*)dstv;
	return vectorConvert(src, dst, n, strideSrc, strideDst, func);
}

DLLEXP
int vecUnary(const DataType type, const unaryOp::UnaryOperation op, const size_t n, const void* src, const size_t strideSrc, void* dst, const size_t strideDst)
{
	AUTO_ALLTYPE_FUNC(vectorUnary, type, int, op, src, dst, n, strideSrc, strideDst);
}
#pragma endregion

#pragma region binary scalar
template <typename T>
inline int vectorBinaryScalar(const binaryOp::BinaryOperation op, const void* scalarv, const void* srcv, void* dstv, const size_t n, const size_t strideSrc, const size_t strideDst)
{
	const T scalar = *(const T*)scalarv;
	FUNC<T(T)> func;
	switch (op)
	{
	case binaryOp::BinaryOperation::Add:
		func = [=] PREFIX(const T v) { return v + scalar; };
		break;
	case binaryOp::BinaryOperation::Multiply:
		func = [=] PREFIX(const T v) { return v * scalar; };
		break;
	case binaryOp::BinaryOperation::Divide:
		func = [=] PREFIX(const T v) { return v / scalar; };
		break;
	case binaryOp::BinaryOperation::Power:
		func = [=] PREFIX(const T v) { return std::pow(v, scalar); };
		break;
	}
	if constexpr (std::is_scalar_v<T>)
	{
		const T scalarAbs = std::abs(scalar);
		switch (op)
		{
		case binaryOp::BinaryOperation::AbsoluteMaximum:
			func = abslarger_functor<T, T>(scalarAbs);
			break;
		case binaryOp::BinaryOperation::AbsoluteMininum:
			func = abssmaller_functor<T, T>(scalarAbs);
			break;
		case binaryOp::BinaryOperation::Maximum:
			func = larger_functor(scalar);
			break;
		case binaryOp::BinaryOperation::Mininum:
			func = smaller_functor(scalar);
			break;
		case binaryOp::BinaryOperation::Truncate:
			func = truncate_functor<T, T>(scalarAbs);
			break;
		default:
			return -1;
		}
	}
	else
	{
		const typename T::value_type scalarAbs = std::abs(scalar);
		switch (op)
		{
		case binaryOp::BinaryOperation::AbsoluteMaximum:
			func = abslarger_functor<T, typename T::value_type>(scalarAbs);
			break;
		case binaryOp::BinaryOperation::AbsoluteMininum:
			func = abssmaller_functor<T, typename T::value_type>(scalarAbs);
			break;
		case binaryOp::BinaryOperation::Truncate:
			func = truncate_functor<T, typename T::value_type>(scalarAbs);
			break;
		default:
			return -1;
		}
	}
	const T* src = (const T*)srcv;
	T* dst = (T*)dstv;
	return vectorConvert(src, dst, n, strideSrc, strideDst, func);
}

DLLEXP
int vecBinaryScalar(const DataType type, const binaryOp::BinaryOperation op, const void* scalar, const size_t n, const void* src, const size_t strideSrc, void* dst, const size_t strideDst)
{
	AUTO_ALLTYPE_FUNC(vectorBinaryScalar, type, int, op, scalar, src, dst, n, strideSrc, strideDst);
}
#pragma endregion

#pragma region binary
template <typename T>
inline int vectorsBinary(const binaryOp::BinaryOperation op, const void* av, const void* bv, void* dstv, const size_t n, const size_t strideA, const size_t strideB, const size_t strideDst)
{
	FUNC<T(T, T)> func;
	switch (op)
	{
	case binaryOp::BinaryOperation::AbsoluteMaximum:
		func = [=] PREFIX(const T v1, const T v2) { return std::abs(v1) > std::abs(v2) ? (T)std::abs(v1) : (T)std::abs(v2); };
		break;
	case binaryOp::BinaryOperation::AbsoluteMininum:
		func = [=] PREFIX(const T v1, const T v2) { return std::abs(v1) < std::abs(v2) ? (T)std::abs(v1) : (T)std::abs(v2); };
		break;
	case binaryOp::BinaryOperation::Add:
		func = [=] PREFIX(const T v1, const T v2) { return v1 + v2; };
		break;
	case binaryOp::BinaryOperation::Maximum:
		if constexpr (!std::is_scalar_v<T>)
			return -1;
		else
			func = largerOne_functor<T>();
		break;
	case binaryOp::BinaryOperation::Mininum:
		if constexpr (!std::is_scalar_v<T>)
			return -1;
		else
			func = smallerOne_functor<T>();
		break;
	case binaryOp::BinaryOperation::Multiply:
		func = [=] PREFIX(const T v1, const T v2) { return v1 * v2; };
		break;
	case binaryOp::BinaryOperation::Power:
		func = [=] PREFIX(const T v, const T p) { return std::pow(v, p); };
		break;
	case binaryOp::BinaryOperation::Divide:
		func = [=] PREFIX(const T v1, const T v2) { return v1 / v2; };
		break;
	default:
		return -1;
	}
	const T* a = (const T*)av, * b = (const T*)bv;
	T* dst = (T*)dstv;
	return vectorConvert(a, b, dst, n, strideA, strideB, strideDst, func);
}

DLLEXP
int vecsBinary(const DataType type, const binaryOp::BinaryOperation op, const size_t n, const void* a, const size_t strideA, const void* b, const size_t strideB, void* dst, const size_t strideDst)
{
	AUTO_ALLTYPE_FUNC(vectorsBinary, type, int, op, a, b, dst, n, strideA, strideB, strideDst);
}
#pragma endregion

#pragma region norm
template<typename T>
inline int vectorNorm(const void* av, const size_t n, const size_t sa, void* result)
{
	const T* a = (const T*)av;
	auto strideA = make_strided_range(a, n, sa);
	if (sa == 1)
	{
		if constexpr (std::is_scalar_v<T>)
			*((T*)result) = thrust::inner_product(THRUST_PAR, a, a + n, a, T{});
		else
			*((typename T::value_type*)result) = thrust::inner_product(THRUST_PAR, a, a + n, a, typename T::value_type{}, plus_functor<typename T::value_type>(), norm_functor<T>());
	}
	else
	{
		if constexpr (std::is_scalar_v<T>)
			*((T*)result) = thrust::inner_product(THRUST_PAR, strideA.begin(), strideA.end(), strideA.begin(), T{});
		else
			*((typename T::value_type*)result) = thrust::inner_product(THRUST_PAR, strideA.begin(), strideA.end(), strideA.begin(), typename T::value_type{}, plus_functor<typename T::value_type>(), norm_functor<T>());
	}
	if constexpr (std::is_scalar_v<T>)
		*((T*)result) = std::sqrt(*((T*)result));
	else
		*((typename T::value_type*)result) = std::sqrt(*((typename T::value_type*)result));
	return 0;
}
#pragma endregion

#pragma region unary aggregate
template <typename T>
inline int getReduceFunction(const reduceOp::ReduceOperation op, T& init, FUNC<T(T, T)>& func)
{
	init = T{};
	switch (op)
	{
	case reduceOp::ReduceOperation::AbsoluteMaximum:
		func = [=] PREFIX(const T v1, const T v2) { return std::abs(v1) > std::abs(v2) ? (T)std::abs(v1) : (T)std::abs(v2); };
		init = neginf<T>();
		break;
	case reduceOp::ReduceOperation::AbsoluteMininum:
		func = [=] PREFIX(const T v1, const T v2) { return std::abs(v1) < std::abs(v2) ? (T)std::abs(v1) : (T)std::abs(v2); };
		init = inf<T>();
		break;
	case reduceOp::ReduceOperation::Add:
		func = [=] PREFIX(const T v1, const T v2) { return v1 + v2; };
		break;
	case reduceOp::ReduceOperation::AddAbsolute:
		func = [=] PREFIX(const T v1, const T v2) { return std::abs(v1) + std::abs(v2); };
		break;
	case reduceOp::ReduceOperation::Maximum:
		if constexpr (!std::is_scalar_v<T>)
			return -1;
		else
			func = largerOne_functor<T>();
		init = neginf<T>();
		break;
	case reduceOp::ReduceOperation::Mininum:
		if constexpr (!std::is_scalar_v<T>)
			return -1;
		else
			func = smallerOne_functor<T>();
		init = inf<T>();
		break;
	case reduceOp::ReduceOperation::Multiply:
		func = [=] PREFIX(const T v1, const T v2) { return v1 * v2; };
		init = T{1};
		break;
	case reduceOp::ReduceOperation::MultiplyAbsolute:
		func = [=] PREFIX(const T v1, const T v2) { return std::abs(v1) * std::abs(v2); };
		init = T{1};
		break;
	default:
		return -1;
	}
}

template <typename T>
inline int vectorUnaryReduce(const reduceOp::ReduceOperation op, const void* srcv, const size_t n, const size_t strideSrc, void* result)
{
	T init{};
	FUNC<T(T, T)> func;
	if (getReduceFunction(op, init, func) < 0)
		return -1;
	const T* src = (const T*)srcv;
	*(T*)result = vectorReduce<T, T, decltype(func)>(src, n, strideSrc, init, func);
	return 0;
}

DLLEXP
int vecUnaryReduce(const DataType type, const reduceOp::ReduceOperation op, const size_t n, const void* src, const size_t strideSrc, void* result)
{
	if (op == reduceOp::ReduceOperation::Norm)
	{
		AUTO_ALLTYPE_FUNC(vectorNorm, type, int, src, n, strideSrc, result);
	}
	AUTO_ALLTYPE_FUNC(vectorUnaryReduce, type, int, op, src, n, strideSrc, result);
}
#pragma endregion

#pragma region arg reduce
template <typename T>
inline int vectorArgReduce(const reduceOp::ReduceOperation op, const void* srcv, const size_t n, const size_t strideSrc, size_t& result)
{
	FUNC<bool(T, T)> func;
	switch (op)
	{
	case reduceOp::ReduceOperation::AbsoluteMaximum:
		func = [=] PREFIX(const T v1, const T v2) { return std::abs(v1) > std::abs(v2); };
		break;
	case reduceOp::ReduceOperation::AbsoluteMininum:
		func = [=] PREFIX(const T v1, const T v2) { return std::abs(v1) < std::abs(v2); };
		break;
	case reduceOp::ReduceOperation::Maximum:
		if constexpr (!std::is_scalar_v<T>)
			return -1;
		else
			func = largerThan_functor<T>();
		break;
	case reduceOp::ReduceOperation::Mininum:
		if constexpr (!std::is_scalar_v<T>)
			return -1;
		else
			func = smallerThan_functor<T>();
		break;
	default:
		return -1;
	}
	const T* src = (const T*)srcv;
	auto ssrc = make_strided_range(src, n, strideSrc);
	if (strideSrc == 1)
	{
		result = thrust::max_element(THRUST_PAR, src, src + n, func) - src;
	}
	else
	{
		result = thrust::max_element(THRUST_PAR, ssrc.begin(), ssrc.end(), func) - ssrc.begin();
	}
	return 0;
}

DLLEXP
int vecArgReduce(const DataType type, const reduceOp::ReduceOperation op, const size_t n, const void* src, const size_t strideSrc, size_t& result)
{
	AUTO_ALLTYPE_FUNC(vectorArgReduce, type, int, op, src, n, strideSrc, result);
}
#pragma endregion

#pragma region scan
template<typename T>
inline int vectorScan(const reduceOp::ReduceOperation op, const bool inclusive, const void* srcv, void* dstv, const size_t n, const size_t s1, const size_t s2)
{
	T init{};
	FUNC<T(T, T)> func;
	if (getReduceFunction(op, init, func) < 0)
		return -1;
	const T* src = (const T*)srcv;
	T* dst = (T*)dstv;
	return vectorScan(inclusive, src, dst, n, s1, s2, init, func);
}

DLLEXP
int vecScan(const DataType type, const reduceOp::ReduceOperation op, const bool inclusive, const size_t n, const void* src, const size_t strideSrc, void* dst, const size_t strideDst)
{
	AUTO_ALLTYPE_FUNC(vectorScan, type, int, op, inclusive, src, dst, n, strideSrc, strideDst);
}
#pragma endregion


#pragma region int operations
template<typename T>
inline int vectorSort(void* vec, const size_t n, const size_t stride)
{
	T* v = (T*)vec;
	if (stride == 1)
	{
		thrust::sort(THRUST_PAR, v, v + n);
	}
	else
	{
		auto stridedVec = make_strided_range(v, n, stride);
		thrust::sort(THRUST_PAR, stridedVec.begin(), stridedVec.end());
	}
	return 0;
}

DLLEXP
int vecSort(const DataType type, const size_t n, void* vec, const size_t stride)
{
	AUTO_REALTYPE_FUNC(vectorSort, type, int, vec, n, stride);
}

template<typename TKey, typename TVal>
inline int vectorSortBy(void* keys, void* vals, const size_t n, const size_t strideKey, const size_t strideVal)
{
	TKey* k = (TKey*)keys;
	TVal* v = (TVal*)vals;
	if (strideKey == 1 && strideVal == 1)
	{
		thrust::sort_by_key(THRUST_PAR, k, k + n, v);
	}
	else if (strideKey == 1 && strideVal != 1)
	{
		auto stridedVal = make_strided_range(v, n, strideVal);
		thrust::sort_by_key(THRUST_PAR, k, k + n, stridedVal.begin());
	}
	else if (strideKey != 1 && strideVal == 1)
	{
		auto stridedKey = make_strided_range(k, n, strideKey);
		thrust::sort_by_key(THRUST_PAR, stridedKey.begin(), stridedKey.end(), v);
	}
	else
	{
		auto stridedKey = make_strided_range(k, n, strideKey);
		auto stridedVal = make_strided_range(v, n, strideVal);
		thrust::sort_by_key(THRUST_PAR, stridedKey.begin(), stridedKey.end(), stridedVal.begin());
	}
	return 0;
}

template<typename T>
inline int vectorSortBy(const DataType valType, void* keys, void* vals, const size_t n, const size_t strideKey, const size_t strideVal)
{
#define __ALLTYPE_FUNC(dataType, ...) do { \
		switch (dataType) \
		{ \
		case RealFloat32: \
			return vectorSortBy<T, float>(__VA_ARGS__); \
		case RealFloat64: \
			return vectorSortBy<T, double>(__VA_ARGS__); \
		case ComplexFloat32: \
			return vectorSortBy<T, complex<float>>(__VA_ARGS__); \
		case ComplexFloat64: \
			return vectorSortBy<T, complex<double>>(__VA_ARGS__); \
		case RealInt8: \
			return vectorSortBy<T, int8_t>(__VA_ARGS__); \
		case RealInt16: \
			return vectorSortBy<T, int16_t>(__VA_ARGS__); \
		case RealInt32: \
			return vectorSortBy<T, int32_t>(__VA_ARGS__); \
		case RealInt64: \
			return vectorSortBy<T, int64_t>(__VA_ARGS__); \
		case RealUInt8: \
			return vectorSortBy<T, uint8_t>(__VA_ARGS__); \
		case RealUInt16: \
			return vectorSortBy<T, uint16_t>(__VA_ARGS__); \
		case RealUInt32: \
			return vectorSortBy<T, uint32_t>(__VA_ARGS__); \
		case RealUInt64: \
			return vectorSortBy<T, uint64_t>(__VA_ARGS__); \
		case ComplexInt8: \
			return vectorSortBy<T, complex<int8_t>>(__VA_ARGS__); \
		case ComplexInt16: \
			return vectorSortBy<T, complex<int16_t>>(__VA_ARGS__); \
		case ComplexInt32: \
			return vectorSortBy<T, complex<int32_t>>(__VA_ARGS__); \
		case ComplexInt64: \
			return vectorSortBy<T, complex<int64_t>>(__VA_ARGS__); \
		case ComplexUInt8: \
			return vectorSortBy<T, complex<uint8_t>>(__VA_ARGS__); \
		case ComplexUInt16: \
			return vectorSortBy<T, complex<uint16_t>>(__VA_ARGS__); \
		case ComplexUInt32: \
			return vectorSortBy<T, complex<uint32_t>>(__VA_ARGS__); \
		case ComplexUInt64: \
			return vectorSortBy<T, complex<uint64_t>>(__VA_ARGS__); \
		default: \
			UNSUPPORT(vectorSortBy, dataType, int); \
		} \
	} while (0)
	
	__ALLTYPE_FUNC(valType, keys, vals, n, strideKey, strideVal);

#undef __ALLTYPE_FUNC
}

DLLEXP
int vecSortBy(const DataType keyType, const DataType valType, const size_t n, void* keys, const size_t strideKey, void* vals, const size_t strideVal)
{
	AUTO_REALTYPE_FUNC(vectorSortBy, keyType, int, valType, keys, vals, n, strideKey, strideVal);
}

template<typename T>
inline int vectorFind(const void* vec, const size_t n, const size_t stride, const void* find, ptrdiff_t& index)
{
	const T* v = (const T*)vec;
	const T f = *(const T*)find;
	if (stride == 1)
	{
		index = thrust::find(THRUST_PAR, v, v + n, f) - v;
		if (index == n)
			index = -1;
	}
	else
	{
		auto stridedVec = make_strided_range(v, n, stride);
		index = thrust::find(THRUST_PAR, stridedVec.begin(), stridedVec.end(), f) - stridedVec.begin();
		if (index == n)
			index = -1;
	}
	return 0;
}
template<typename T>
inline int vectorSortedFind(const void* vec, const size_t n, const size_t stride, const void* find, ptrdiff_t& index)
{
	const T* v = (const T*)vec;
	const T f = *(const T*)find;
	if (stride == 1)
	{
		bool found = thrust::binary_search(THRUST_PAR, v, v + n, f);
		index = thrust::upper_bound(THRUST_PAR, v, v + n, f) - v;
		if (!found)
			index = ~index;
	}
	else
	{
		auto stridedVec = make_strided_range(v, n, stride);
		bool found = thrust::binary_search(THRUST_PAR, stridedVec.begin(), stridedVec.end(), f);
		index = thrust::upper_bound(THRUST_PAR, stridedVec.begin(), stridedVec.end(), f) - stridedVec.begin();
		if (!found)
			index = ~index;
	}
	return 0;
}

DLLEXP
int vecFind(const DataType type, const bool sorted, const size_t n, const void* v, const size_t stride, const void* toFind, ptrdiff_t& index)
{
	if (sorted)
	{
		AUTO_REALTYPE_FUNC(vectorSortedFind, type, int, v, n, stride, toFind, index);
	}
	else
	{
		AUTO_REALTYPE_FUNC(vectorFind, type, int, v, n, stride, toFind, index);
	}
}


template<typename T>
inline int vectorFillRange(void* vec, const size_t n, const size_t stride, const void* start, const void* step)
{
	T* v = (T*)vec;
	const T s = *(const T*)start, d = *(const T*)step;
	if (stride == 1)
	{
		thrust::sequence(THRUST_PAR, v, v + n, s, d);
	}
	else
	{
		auto stridedVec = make_strided_range(v, n, stride);
		thrust::sequence(THRUST_PAR, stridedVec.begin(), stridedVec.end(), s, d);
	}
	return 0;
}

DLLEXP
int vecFillRange(const DataType type, const size_t n, void* v, const size_t stride, const void* start, const void* step)
{
	AUTO_REALTYPE_FUNC(vectorFillRange, type, int, v, n, stride, start, step);
}


template<typename T>
inline int vectorUpperBound(const void* vec, const size_t n, const size_t stride, const void* find, ptrdiff_t& index)
{
	const T* v = (const T*)vec;
	const T f = *(const T*)find;
	if (stride == 1)
	{
		index = thrust::upper_bound(THRUST_PAR, v, v + n, f) - v;
	}
	else
	{
		auto stridedVec = make_strided_range(v, n, stride);
		index = thrust::upper_bound(THRUST_PAR, stridedVec.begin(), stridedVec.end(), f) - stridedVec.begin();
	}
	return 0;
}
template<typename T>
inline int vectorLowerBound(const void* vec, const size_t n, const size_t stride, const void* find, ptrdiff_t& index)
{
	const T* v = (const T*)vec;
	const T f = *(const T*)find;
	if (stride == 1)
	{
		index = thrust::lower_bound(THRUST_PAR, v, v + n, f) - v;
	}
	else
	{
		auto stridedVec = make_strided_range(v, n, stride);
		index = thrust::lower_bound(THRUST_PAR, stridedVec.begin(), stridedVec.end(), f) - stridedVec.begin();
	}
	return 0;
}

DLLEXP
int vecBound(const DataType type, const bool lower, const size_t n, const void* v, const size_t stride, const void* find, ptrdiff_t& index)
{
	if (lower)
	{
		AUTO_REALTYPE_FUNC(vectorLowerBound, type, int, v, n, stride, find, index);
	}
	else
	{
		AUTO_REALTYPE_FUNC(vectorUpperBound, type, int, v, n, stride, find, index);
	}
}
#pragma endregion