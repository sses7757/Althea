#include "extblas.h"
using namespace extblas;


#pragma region data type cast
template <typename RealIn, typename RealOut, bool hasStrideSrc, bool hasStrideDst>
struct realTypeConvert_functor
{
	const RealIn* src;
	RealOut* dst;
	const size_t strideSrc, strideDst;

	realTypeConvert_functor(const void* src, void* dst, const size_t strideSrc, const size_t strideDst) :
		src((const RealIn*)src),
		dst((RealOut*)dst),
		strideSrc(strideSrc), strideDst(strideDst) {}

	__host__ __device__ void operator()(const size_t& ind) const
	{
		if constexpr (hasStrideSrc && hasStrideDst)
		{
			size_t indA = ind * strideSrc, indB = ind * strideDst;
			dst[indB] = (RealOut)src[indA];
		}
		else if constexpr (hasStrideSrc && !hasStrideDst)
		{
			size_t indA = ind * strideSrc;
			dst[ind] = (RealOut)src[indA];
		}
		else if constexpr (!hasStrideSrc && hasStrideDst)
		{
			size_t indB = ind * strideDst;
			dst[indB] = (RealOut)src[ind];
		}
		else
		{
			dst[ind] = (RealOut)src[ind];
		}
	}
};
template <typename RealIn, typename RealOut, bool hasStrideSrc, bool hasStrideDst>
struct realToComplex_functor
{
	const RealIn* src;
	BlasSupp::complex<RealOut>* dst;
	const size_t strideSrc, strideDst;

	realToComplex_functor(const void* src, void* dst, const size_t strideSrc, const size_t strideDst) :
		src((const RealIn*)src),
		dst((BlasSupp::complex<RealOut>*)dst),
		strideSrc(strideSrc), strideDst(strideDst) {}

	__host__ __device__ void operator()(const size_t& ind) const
	{
		if constexpr (hasStrideSrc && hasStrideDst)
		{
			size_t indA = ind * strideSrc, indB = ind * strideDst;
			dst[indB] = BlasSupp::complex<RealOut>((RealOut)src[indA]);
		}
		else if constexpr (hasStrideSrc && !hasStrideDst)
		{
			size_t indA = ind * strideSrc;
			dst[ind] = BlasSupp::complex<RealOut>((RealOut)src[indA]);
		}
		else if constexpr (!hasStrideSrc && hasStrideDst)
		{
			size_t indB = ind * strideDst;
			dst[indB] = BlasSupp::complex<RealOut>((RealOut)src[ind]);
		}
		else
		{
			dst[ind] = BlasSupp::complex<RealOut>((RealOut)src[ind]);
		}
	}
};
template <typename RealIn, typename RealOut, bool hasStrideSrc, bool hasStrideDst, bool byAbs>
struct complexToReal_functor
{
	const BlasSupp::complex<RealIn>* src;
	RealOut* dst;
	const size_t strideSrc, strideDst;

	complexToReal_functor(const void* src, void* dst, const size_t strideSrc, const size_t strideDst) :
		src((const BlasSupp::complex<RealIn>*)src),
		dst((RealOut*)dst),
		strideSrc(strideSrc), strideDst(strideDst) {}

	__host__ __device__ void operator()(const size_t& ind) const
	{
		if constexpr (hasStrideSrc && hasStrideDst)
		{
			size_t indA = ind * strideSrc, indB = ind * strideDst;
			if constexpr (byAbs)
				dst[indB] = (RealOut)std::abs(src[indA]);
			else
				dst[indB] = (RealOut)src[indA].real();
		}
		else if constexpr (hasStrideSrc && !hasStrideDst)
		{
			size_t indA = ind * strideSrc;
			if constexpr (byAbs)
				dst[ind] = (RealOut)std::abs(src[indA]);
			else
				dst[ind] = (RealOut)src[indA].real();
		}
		else if constexpr (!hasStrideSrc && hasStrideDst)
		{
			size_t indB = ind * strideDst;
			if constexpr (byAbs)
				dst[indB] = (RealOut)std::abs(src[ind]);
			else
				dst[indB] = (RealOut)src[ind].real();
		}
		else
		{
			if constexpr (byAbs)
				dst[ind] = (RealOut)std::abs(src[ind]);
			else
				dst[ind] = (RealOut)src[ind].real();
		}
	}
};

template <typename RealIn, typename RealOut>
inline ERROR_RETURN vectorComplexToReal(const void* srcv, void* dstv, const size_t n, const unsigned int s1, const unsigned int s2, bool toRealByAbs)
{
	if (s1 == 1 && s2 == 1)
	{
		if (toRealByAbs)
			thrust::for_each_n(THRUST_PAR, counting0, n, complexToReal_functor<RealIn, RealOut, false, false, true>(srcv, dstv, s1, s2));
		else
			thrust::for_each_n(THRUST_PAR, counting0, n, complexToReal_functor<RealIn, RealOut, false, false, false>(srcv, dstv, s1, s2));
	}
	else if (s1 == 1 && s2 != 1)
	{
		if (toRealByAbs)
			thrust::for_each_n(THRUST_PAR, counting0, n, complexToReal_functor<RealIn, RealOut, false, true, true>(srcv, dstv, s1, s2));
		else
			thrust::for_each_n(THRUST_PAR, counting0, n, complexToReal_functor<RealIn, RealOut, false, true, false>(srcv, dstv, s1, s2));
	}
	else if (s1 != 1 && s2 == 1)
	{
		if (toRealByAbs)
			thrust::for_each_n(THRUST_PAR, counting0, n, complexToReal_functor<RealIn, RealOut, true, false, true>(srcv, dstv, s1, s2));
		else
			thrust::for_each_n(THRUST_PAR, counting0, n, complexToReal_functor<RealIn, RealOut, true, false, false>(srcv, dstv, s1, s2));
	}
	else
	{
		if (toRealByAbs)
			thrust::for_each_n(THRUST_PAR, counting0, n, complexToReal_functor<RealIn, RealOut, true, true, true>(srcv, dstv, s1, s2));
		else
			thrust::for_each_n(THRUST_PAR, counting0, n, complexToReal_functor<RealIn, RealOut, true, true, false>(srcv, dstv, s1, s2));
	}
	return ERROR_RETURN();
}

template <typename RealIn, typename RealOut>
inline ERROR_RETURN vectorRealToComplex(const void* srcv, void* dstv, const size_t n, const unsigned int s1, const unsigned int s2, const bool toRealByAbs)
{
	if (s1 == 1 && s2 == 1)
	{
		thrust::for_each_n(THRUST_PAR, counting0, n, realToComplex_functor<RealIn, RealOut, false, false>(srcv, dstv, s1, s2));
	}
	else if (s1 == 1 && s2 != 1)
	{
		thrust::for_each_n(THRUST_PAR, counting0, n, realToComplex_functor<RealIn, RealOut, false, true>(srcv, dstv, s1, s2));
	}
	else if (s1 != 1 && s2 == 1)
	{
		thrust::for_each_n(THRUST_PAR, counting0, n, realToComplex_functor<RealIn, RealOut, true, false>(srcv, dstv, s1, s2));
	}
	else
	{
		thrust::for_each_n(THRUST_PAR, counting0, n, realToComplex_functor<RealIn, RealOut, true, true>(srcv, dstv, s1, s2));
	}
	return ERROR_RETURN();
}

template <typename RealIn, typename RealOut>
inline ERROR_RETURN vectorRealConvert(const void* srcv, void* dstv, const size_t n, const unsigned int s1, const unsigned int s2, const bool toRealByAbs)
{
	if (s1 == 1 && s2 == 1)
	{
		thrust::for_each_n(THRUST_PAR, counting0, n, realTypeConvert_functor<RealIn, RealOut, false, false>(srcv, dstv, s1, s2));
	}
	else if (s1 == 1 && s2 != 1)
	{
		thrust::for_each_n(THRUST_PAR, counting0, n, realTypeConvert_functor<RealIn, RealOut, false, true>(srcv, dstv, s1, s2));
	}
	else if (s1 != 1 && s2 == 1)
	{
		thrust::for_each_n(THRUST_PAR, counting0, n, realTypeConvert_functor<RealIn, RealOut, true, false>(srcv, dstv, s1, s2));
	}
	else
	{
		thrust::for_each_n(THRUST_PAR, counting0, n, realTypeConvert_functor<RealIn, RealOut, true, true>(srcv, dstv, s1, s2));
	}
	return ERROR_RETURN();
}

DLLEXP
ERROR_RETURN vecDataConvert(const DataType srcType, const DataType dstType, const void* src, void* dst, const size_t n, const size_t strideSrc, const size_t strideDst, const bool toRealByAbs)
{
	// copy if no data conversion
	if (srcType == dstType)
	{
		AUTO_ALLTYPE_FUNC(vectorStridedCopy, srcType, ERROR_RETURN, src, dst, n, strideSrc, strideDst);
		// return is inside the auto generated switch
	}

	// define inner switch
#ifndef HAS_LDBL
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
			convertFunc = convert<type, unsigned int>; break; \
		case RealUInt64: \
		case ComplexUInt64: \
			convertFunc = convert<type, unsigned long long>; break; \
		default: \
			UNSUPPORT(vecDataConvert, dstType, ERROR_RETURN); \
		} \
	} while (0)
#else
#define CONVERT_INNER_SWITCH(type, convert) do { \
		switch (dstType) \
		{ \
		case RealFloat32: \
		case ComplexFloat32: \
			convertFunc = convert<type, float>; break; \
		case RealFloat64: \
		case ComplexFloat64: \
			convertFunc = convert<type, double>; break; \
		case RealLongFloat64: \
		case ComplexLongFloat64: \
			convertFunc = convert<type, long double>; break; \
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
			convertFunc = convert<type, unsigned int>; break; \
		case RealUInt64: \
		case ComplexUInt64: \
			convertFunc = convert<type, unsigned long long>; break; \
		default: \
			UNSUPPORT(vecDataConvert, dstType, ERROR_RETURN); \
		} \
	} while (0)
#endif

// define outer switch
#ifndef HAS_LDBL
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
			UNSUPPORT(vecDataConvert, srcType, ERROR_RETURN); \
		} \
	} while (0)
#else
#define CONVERT_OUTER_SWITCH(convert) do { \
		switch (srcType) \
		{ \
		case RealFloat32: \
		case ComplexFloat32: \
			CONVERT_INNER_SWITCH(float, convert); break; \
		case RealFloat64: \
		case ComplexFloat64: \
			CONVERT_INNER_SWITCH(double, convert); break; \
		case RealLongFloat64: \
		case ComplexLongFloat64: \
			CONVERT_INNER_SWITCH(long double, convert); break; \
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
			UNSUPPORT(vecDataConvert, srcType, ERROR_RETURN); \
		} \
	} while (0)
#endif

	// the convert function
	ERROR_RETURN(*convertFunc)(const void* src, void* dst, const size_t n, const size_t strideSrc, const size_t strideDst, const bool toRealByAbs);
	if (isreal(srcType) && isreal(dstType))
	{	// real convert
		CONVERT_OUTER_SWITCH(vectorRealConvert);
	}
	else if (isreal(srcType))
	{	// real to complex
		CONVERT_OUTER_SWITCH(vectorRealToComplex);
	}
	else if (isreal(dstType))
	{	// complex to real, 'toRealByAbs' is only used here
		CONVERT_OUTER_SWITCH(vectorComplexToReal);
	}
	else
	{	// all complex, use the real convert of each part instead
		if (strideSrc == 1 && strideDst == 1)
		{
			return vecDataConvert(realCorrespond(srcType), realCorrespond(dstType), src, dst, n * 2, 1, 1, true);
		}
		else
		{
			// the real parts
			auto ret1 = vecDataConvert(realCorrespond(srcType), realCorrespond(dstType), src, dst, n * 2, strideSrc * 2, strideDst * 2, true);
			if (ret1 != ERROR_RETURN())
				return ret1;
			// increase pointers
			const int sizeSrc = size(srcType), sizeDst = size(dstType);
			const void* srcInc = (const char*)src + sizeSrc;
			void* dstInc = (char*)dst + sizeDst;
			// the imaginary parts
			return vecDataConvert(realCorrespond(srcType), realCorrespond(dstType), srcInc, dstInc, n * 2, strideSrc * 2, strideDst * 2, true);
		}
	}
#undef CONVERT_OUTER_SWITCH
#undef CONVERT_INNER_SWITCH
	// calculate
	convertFunc(src, dst, n, strideSrc, strideDst, toRealByAbs);
}
#pragma endregion

#pragma region fill array with value
template<typename T>
inline int vectorFillWith(void* av, const void* valv, const size_t n, const size_t stride)
{
	T val = *(T*)valv;
	T* a = (T*)av;
	if (stride == 1)
	{
		thrust::fill_n(THRUST_PAR, a, n, val);
	}
	else
	{
		auto sa = make_strided_range(a, n, stride);
		thrust::fill_n(THRUST_PAR, sa, n, val);
	}
	return 0;
}

DLLEXP
int vecFillVal(const DataType type, const size_t n, const void* val, void* a, const size_t stride)
{
	AUTO_ALLTYPE_FUNC(vectorFillWith, type, int, a, val, n, stride);
}
#pragma endregion

#pragma region strided copy
template <typename T>
inline ERROR_RETURN vectorStridedCopy(const void* srcv, void* dstv, const size_t n, const size_t strideSrc, const size_t strideDst)
{
	if (strideSrc == 1 && strideDst == 1)
	{
#ifdef CPU
		memcpy(dstv, srcv, n * sizeof(T));
		return 0;
#else
		return cudaMemcpy(dstv, srcv, n * sizeof(T), cudaMemcpyDeviceToDevice);
#endif // CPU
	}
	T* src = (T*)srcv, * dst = (T*)dstv;
	auto ssrc = make_strided_range(src, n, strideDst);
	auto sdst = make_strided_range(dst, n, strideDst);
	if (strideSrc == 1 && strideDst != 1)
	{
		thrust::copy_n(THRUST_PAR, src, n, sdst);
	}
	else if (strideSrc != 1 && strideDst == 1)
	{
		thrust::copy_n(THRUST_PAR, ssrc, n, dst);
	}
	else
	{
		thrust::copy_n(THRUST_PAR, ssrc, n, sdst);
	}
	return ERROR_RETURN();
}

DLLEXP
ERROR_RETURN vecStridedCopy(const DataType type, const size_t n, const void* src, const size_t strideSrc, void* dst, const size_t strideDst)
{
	AUTO_ALLTYPE_FUNC(vectorStridedCopy, type, ERROR_RETURN, src, dst, n, strideSrc, strideDst);
}
#pragma endregion





#pragma region element-wise multiply and divide
auto counting0 = thrust::make_counting_iterator((size_t)0);

template<typename T, bool multiply, bool hasStrideSrc, bool hasStrideDst>
struct foreachMulDiv_functor
{
	T* a;
	const T* b;
	const size_t strideA, strideB;

	foreachMulDiv_functor(void* a, const void* b, const size_t strideA, const size_t strideB) :
		a((T*)a), b((const T*)b),
		strideA(strideA), strideB(strideB) {}

	__host__ __device__ void operator()(const size_t& ind) const
	{
		if constexpr (hasStrideSrc && hasStrideDst)
		{
			size_t indA = ind * strideA, indB = ind * strideB;
			if constexpr (multiply)
				a[indA] = a[indA] * b[indB];
			else
				a[indA] = a[indA] / b[indB];
		}
		else if constexpr (hasStrideSrc && !hasStrideDst)
		{
			size_t indA = ind * strideA;
			if constexpr (multiply)
				a[indA] = a[indA] * b[ind];
			else
				a[indA] = a[indA] / b[ind];
		}
		else if constexpr (!hasStrideSrc && hasStrideDst)
		{
			size_t indB = ind * strideB;
			if constexpr (multiply)
				a[ind] = a[ind] * b[indB];
			else
				a[ind] = a[ind] / b[indB];
		}
		else
		{
			if constexpr (multiply)
				a[ind] = a[ind] * b[ind];
			else
				a[ind] = a[ind] / b[ind];
		}
	}
};

template<typename T>
inline int vectorsElementWiseMultiplyDivide(void* av, const void* bv, const size_t n, const unsigned int sa, const unsigned int sb, bool multiply)
{
	if (sa == 1 && sb == 1)
	{
		if (multiply)
			thrust::for_each_n(THRUST_PAR, counting0, n, foreachMulDiv_functor<T, true, false, false>(av, bv, sa, sb));
		else
			thrust::for_each_n(THRUST_PAR, counting0, n, foreachMulDiv_functor<T, false, false, false>(av, bv, sa, sb));
	}
	else if(sa == 1 && sb != 1)
	{
		if (multiply)
			thrust::for_each_n(THRUST_PAR, counting0, n, foreachMulDiv_functor<T, true, false, true>(av, bv, sa, sb));
		else
			thrust::for_each_n(THRUST_PAR, counting0, n, foreachMulDiv_functor<T, false, false, true>(av, bv, sa, sb));
	}
	else if (sa != 1 && sb == 1)
	{
		if (multiply)
			thrust::for_each_n(THRUST_PAR, counting0, n, foreachMulDiv_functor<T, true, true, false>(av, bv, sa, sb));
		else
			thrust::for_each_n(THRUST_PAR, counting0, n, foreachMulDiv_functor<T, false, true, false>(av, bv, sa, sb));
	}
	else
	{
		if (multiply)
			thrust::for_each_n(THRUST_PAR, counting0, n, foreachMulDiv_functor<T, true, true, true>(av, bv, sa, sb));
		else
			thrust::for_each_n(THRUST_PAR, counting0, n, foreachMulDiv_functor<T, false, true, true>(av, bv, sa, sb));
	}
	return 0;
}

DLLEXP
int vecsMulDiv(const DataType type, void* a, const void* b, const size_t n, const size_t strideA, const size_t strideB, bool multiply)
{
	AUTO_ALLTYPE_FUNC(vectorsElementWiseMultiplyDivide, type, int, a, b, n, strideA, strideB, multiply);
}
#pragma endregion


#pragma region vectors add
template<typename T, bool hasStrideSrc, bool hasStrideDst, bool hasScalar>
struct foreachAddTwo_functor
{
	const T* a;
	T* b;
	const T scalar;
	const size_t strideA, strideB;

	foreachAddTwo_functor(const void* a, void* b, const void* scalar, const size_t strideA, const size_t strideB) :
		a((const T*)a), b((T*)b),
		scalar(*(const T*)scalar),
		strideA(strideA), strideB(strideB) {}

	__host__ __device__ void operator()(const size_t& ind) const
	{
		if constexpr (hasStrideSrc && hasStrideDst)
		{
			size_t indA = ind * strideA, indB = ind * strideB;
			if constexpr (hasScalar)
				b[indB] = std::fma(a[indA], scalar, b[indB]);
			else
				b[indB] = a[indA] + b[indB];
		}
		else if constexpr (hasStrideSrc && !hasStrideDst)
		{
			size_t indA = ind * strideA;
			if constexpr (hasScalar)
				b[ind] = std::fma(a[indA], scalar, b[ind]);
			else
				b[ind] = a[indA] + b[ind];
		}
		else if constexpr (!hasStrideSrc && hasStrideDst)
		{
			size_t indB = ind * strideB;
			if constexpr (hasScalar)
				b[indB] = std::fma(a[ind], scalar, b[indB]);
			else
				b[indB] = a[ind] + b[indB];
		}
		else
		{
			if constexpr (hasScalar)
				b[ind] = std::fma(a[ind], scalar, b[ind]);
			else
				b[ind] = a[ind] + b[ind];
		}
	}
};

template<typename T>
inline int vectorsGeneralAdd(const void* scalar, const void* av, void* bv, const size_t n, const unsigned int sa, const unsigned int sb)
{
	bool scalarNotOne = T(1) != *(const T*)scalar;
	if (sa == 1 && sb == 1)
	{
		if (scalarNotOne)
			thrust::for_each_n(THRUST_PAR, counting0, n, foreachAddTwo_functor<T, false, false, true>(av, bv, scalar, sa, sb));
		else
			thrust::for_each_n(THRUST_PAR, counting0, n, foreachAddTwo_functor<T, false, false, false>(av, bv, scalar, sa, sb));
	}
	else if (sa == 1 && sb != 1)
	{
		if (scalarNotOne)
			thrust::for_each_n(THRUST_PAR, counting0, n, foreachAddTwo_functor<T, false, true, true>(av, bv, scalar, sa, sb));
		else
			thrust::for_each_n(THRUST_PAR, counting0, n, foreachAddTwo_functor<T, false, true, false>(av, bv, scalar, sa, sb));
	}
	else if (sa != 1 && sb == 1)
	{
		if (scalarNotOne)
			thrust::for_each_n(THRUST_PAR, counting0, n, foreachAddTwo_functor<T, true, false, true>(av, bv, scalar, sa, sb));
		else
			thrust::for_each_n(THRUST_PAR, counting0, n, foreachAddTwo_functor<T, true, false, false>(av, bv, scalar, sa, sb));
	}
	else
	{
		if (scalarNotOne)
			thrust::for_each_n(THRUST_PAR, counting0, n, foreachAddTwo_functor<T, true, true, true>(av, bv, scalar, sa, sb));
		else
			thrust::for_each_n(THRUST_PAR, counting0, n, foreachAddTwo_functor<T, true, true, false>(av, bv, scalar, sa, sb));
	}
	return 0;
}

DLLEXP
int vecsAdd(const DataType type, const void* scalar, const void* a, void* b, const size_t n, const size_t strideA, const size_t strideB)
{
	AUTO_ALLTYPE_FUNC(vectorsGeneralAdd, type, int, scalar, a, b, n, strideA, strideB);
}
#pragma endregion


#pragma region vectors equal
template<typename T>
inline bool vectorsEqual(const void* av, const void* bv, const size_t n, const unsigned int sa, const unsigned int sb)
{
	if (av == bv && sa == sb)
		return true;
	const T* a = (const T*)av;
	const T* b = (const T*)bv;
	if (sa == 1 && sb == 1)
	{
		return thrust::equal(THRUST_PAR, a, a + n, b);
	}
	else if (sa == 1 && sb != 1)
	{
		auto strideB = make_strided_range(b, n, sb);
		return thrust::equal(THRUST_PAR, a, a + n, strideB.begin());
	}
	else if (sa != 1 && sb == 1)
	{
		auto strideA = make_strided_range(a, n, sa);
		return thrust::equal(THRUST_PAR, strideA.begin(), strideA.end(), b);
	}
	else
	{
		auto strideA = make_strided_range(a, n, sa);
		auto strideB = make_strided_range(b, n, sb);
		return thrust::equal(THRUST_PAR, strideA.begin(), strideA.end(), strideB.begin());
	}
	// fake return for NVCC
	return false;
}

DLLEXP
bool vecsEq(const DataType type, const void* a, const void* b, const size_t n, const size_t strideA, const size_t strideB)
{
	AUTO_ALLTYPE_FUNC(vectorsEqual, type, bool, a, b, n, strideA, strideB);
}
#pragma endregion


#pragma region element-wise power
template<typename T, typename U>
struct foreachPower_functor
{
	T* a;
	const U p;

	foreachPower_functor(void* a, const void* p) : a((T*)a), p(*(const U*)p) {}

	__host__ __device__ void operator()(const size_t& ind) const
	{
		a[ind] = std::pow(a[ind], p);
	}
};
template<typename T, typename U>
struct foreachPower_strided_functor
{
	T* a;
	const U p;
	const size_t stride;

	foreachPower_strided_functor(void* a, const void* p, const size_t stride) : a((T*)a), p(*(const U*)p), stride(stride) {}

	__host__ __device__ void operator()(const size_t& ind) const
	{
		size_t indA = ind * stride;
		a[indA] = std::pow(a[indA], p);
	}
};

template<typename T>
inline int vectorElementWisePowerSameType(void* av, const void* pv, const size_t n, const size_t stride)
{
	if (stride == 1)
	{
		thrust::for_each_n(THRUST_PAR, counting0, n, foreachPower_functor<T, T>(av, pv));
	}
	else
	{
		thrust::for_each_n(THRUST_PAR, counting0, n, foreachPower_strided_functor<T, T>(av, pv, stride));
	}
	return 0;
}

template<typename T>
inline int vectorElementWiseRealPower(void* av, const void* pv, const size_t n, const size_t stride)
{
	using realT = typename T::value_type;
	if (stride == 1)
	{
		thrust::for_each_n(THRUST_PAR, counting0, n, foreachPower_functor<T, realT>(av, pv));
	}
	else
	{
		thrust::for_each_n(THRUST_PAR, counting0, n, foreachPower_strided_functor<T, realT>(av, pv, stride));
	}
	return 0;
}

DLLEXP
int vecPowSameType(const DataType type, void* a, const void* p, const size_t n, const size_t stride)
{
	AUTO_ALLTYPE_FUNC(vectorElementWisePowerSameType, type, int, a, p, n, stride);
}

DLLEXP
int vecPowRealType(const DataType type, void* a, const void* p, const size_t n, const size_t stride)
{
	AUTO_COMPLEX_TYPE_FUNC(vectorElementWiseRealPower, type, int, a, p, n, stride);
}
#pragma endregion
#pragma region array conjugate
template<typename T>
struct foreachConj_strided_functor
{
	T* a;
	const size_t stride;

	foreachConj_strided_functor(void* a, const size_t stride) : a((T*)a), stride(stride) {}

	__host__ __device__ void operator()(const size_t& ind) const
	{
		size_t indA = ind * stride;
		a[indA] = std::conj(a[indA]);
	}
};
template<typename T>
struct foreachConj_functor
{
	T* a;

	foreachConj_functor(void* a) : a((T*)a) {}

	__host__ __device__ void operator()(const size_t& ind) const
	{
		a[ind] = std::conj(a[ind]);
	}
};

template<typename T>
inline int vecConjugate(void* av, const size_t n, const size_t stride)
{
	if constexpr (std::is_scalar<T>::value)
		return;
	if (stride == 1)
	{
		thrust::for_each_n(THRUST_PAR, counting0, n, foreachConj_functor<T>(av));
	}
	else
	{
		thrust::for_each_n(THRUST_PAR, counting0, n, foreachConj_strided_functor<T>(av, stride));
	}
	return 0;
}

DLLEXP
int vecConj(const DataType type, void* a, const size_t n, const size_t stride)
{
	AUTO_SIGNED_TYPE_FUNC(vecConjugate, type, int, a, n, stride);
}
#pragma endregion


#pragma region dense vector set values with small absolutes to zero
template<typename T, typename U>
struct foreachClip_strided_functor
{
	T* a;
	const U val;
	const size_t stride;

	foreachClip_strided_functor(void* a, const void* val, const size_t stride) : a((T*)a), val(*(const U*)val), stride(stride) {}

	__host__ __device__ void operator()(const size_t& ind) const
	{
		size_t indA = ind * stride;
		a[indA] = std::abs(a[indA]) < val ? T() : a[indA];
	}
};
template<typename T, typename U>
struct foreachClip_functor
{
	T* a;
	const U val;

	foreachClip_functor(void* a, const void* val) : a((T*)a), val(*(const U*)val) {}

	__host__ __device__ void operator()(const size_t& ind) const
	{
		a[ind] = std::abs(a[ind]) < val ? T() : a[ind];
	}
};

template<typename T>
inline int vectorClip(void* av, const void* threshold, const size_t n, const size_t stride)
{
	if constexpr (std::is_scalar<T>::value)
	{
		if (stride == 1)
		{
			thrust::for_each_n(THRUST_PAR, counting0, n, foreachClip_functor<T, T>(av, threshold));
		}
		else
		{
			thrust::for_each_n(THRUST_PAR, counting0, n, foreachClip_strided_functor<T, T>(av, threshold, stride));
		}
	}
	else
	{
		using U = typename T::value_type;
		if (stride == 1)
		{
			thrust::for_each_n(THRUST_PAR, counting0, n, foreachClip_functor<T, U>(av, threshold));
		}
		else
		{
			thrust::for_each_n(THRUST_PAR, counting0, n, foreachClip_strided_functor<T, U>(av, threshold, stride));
		}
	}
	return 0;
}

DLLEXP
int vecClip(const DataType type, void* a, const void* threshold, const size_t n, const size_t stride)
{
	AUTO_ALLTYPE_FUNC(vectorClip, type, int, a, threshold, n, stride);
}
#pragma endregion


#pragma region vector add scalar
template<typename T>
struct foreachAdd_strided_functor
{
	T* a;
	const T val;
	const size_t stride;

	foreachAdd_strided_functor(void* a, const void* val, const size_t stride) : a((T*)a), val(*(const T*)val), stride(stride) {}

	__host__ __device__ void operator()(const size_t& ind) const
	{
		size_t indA = ind * stride;
		a[indA] = a[indA] + val;
	}
};
template<typename T>
struct foreachAdd_functor
{
	T* a;
	const T val;

	foreachAdd_functor(void* a, const void* val) : a((T*)a), val(*(const T*)val) {}

	__host__ __device__ void operator()(const size_t& ind) const
	{
		a[ind] = a[ind] + val;
	}
};

template<typename T>
inline int vectorAddedByScalar(void* av, const void* scalar, const size_t n, const size_t stride)
{
	if (stride == 1)
	{
		thrust::for_each_n(THRUST_PAR, counting0, n, foreachAdd_functor<T>(av, scalar));
	}
	else
	{
		thrust::for_each_n(THRUST_PAR, counting0, n, foreachAdd_strided_functor<T>(av, scalar, stride));
	}
	return 0;
}

DLLEXP
int vecAddScalar(const DataType type, void* a, const void* scalar, const size_t n, const size_t stride)
{
	AUTO_ALLTYPE_FUNC(vectorAddedByScalar, type, int, a, scalar, n, stride);
}
#pragma endregion


#pragma region vector aggregate -- abs sum
template<typename T>
struct realAbsPlus_functor
{
	__host__ __device__ const T operator()(const T& x, const T& y) const
	{
		return x + std::abs(y);
	}
};

template<typename T>
struct compAbsPlus_functor
{
	__host__ __device__ const T operator()(const T& x, const BlasSupp::complex<T>& y) const
	{
		return x + std::abs(y);
	}
};

template<typename T>
struct realSquarePlus_functor
{
	__host__ __device__ const T operator()(const T& x, const T& y) const
	{
		return std::fma(y, y, x);
	}
};

template<typename T>
struct compSquarePlus_functor
{
	__host__ __device__ const T operator()(const T& x, const BlasSupp::complex<T>& y) const
	{
		return x + y.absSquare();
	}
};

template<typename T>
inline double vectorAbsoluteSum(const void* av, const size_t n, const size_t stride)
{
	const T* a = (const T*)av;
	if constexpr (std::is_scalar<T>::value)
	{
		T outSum;
		if (stride == 1)
		{
			outSum = thrust::reduce(THRUST_PAR, a, a + n, T(), realAbsPlus_functor<T>());
		}
		else
		{
			auto strideA = make_strided_range(a, n, stride);
			outSum = thrust::reduce(THRUST_PAR, strideA.begin(), strideA.end(), T(), realAbsPlus_functor<T>());
		}
		return (double)outSum;
	}
	else
	{
		using realT = typename T::value_type;
		realT outSum;
		if (stride == 1)
		{
			outSum = thrust::reduce(THRUST_PAR, a, a + n, realT(), compAbsPlus_functor<realT>());
		}
		else
		{
			auto strideA = make_strided_range(a, n, stride);
			outSum = thrust::reduce(THRUST_PAR, strideA.begin(), strideA.end(), realT(), compAbsPlus_functor<realT>());
		}
		return (double)outSum;
	}
}

template<typename T>
inline double vectorNorm(const void* av, const size_t n, const size_t stride)
{
	const T* a = (const T*)av;
	if constexpr (std::is_scalar<T>::value)
	{
		T outSum;
		if (stride == 1)
		{
			outSum = thrust::reduce(THRUST_PAR, a, a + n, T(), realSquarePlus_functor<T>());
		}
		else
		{
			auto strideA = make_strided_range(a, n, stride);
			outSum = thrust::reduce(THRUST_PAR, strideA.begin(), strideA.end(), T(), realSquarePlus_functor<T>());
		}
		return std::sqrt((double)outSum);
	}
	else
	{
		using realT = typename T::value_type;
		realT outSum;
		if (stride == 1)
		{
			outSum = thrust::reduce(THRUST_PAR, a, a + n, realT(), compSquarePlus_functor<realT>());
		}
		else
		{
			auto strideA = make_strided_range(a, n, stride);
			outSum = thrust::reduce(THRUST_PAR, strideA.begin(), strideA.end(), realT(), compSquarePlus_functor<realT>());
		}
		return std::sqrt((double)outSum);
	}
}

DLLEXP
double vecAbsSum(const DataType type, const void* a, const size_t n, const size_t stride)
{
	AUTO_ALLTYPE_FUNC(vectorAbsoluteSum, type, double, a, n, stride);
}

DLLEXP
double vecNorm(const DataType type, const void* a, const size_t n, const size_t stride)
{
	AUTO_ALLTYPE_FUNC(vectorNorm, type, double, a, n, stride);
}
#pragma endregion


#pragma region vector dot
template<typename T>
inline int vectorsInner(const void* av, const void* bv, const size_t n, const unsigned int sa, const unsigned int sb, void* result)
{
	const T* a = (const T*)av;
	const T* b = (const T*)bv;
	if (sa == 1 && sb == 1)
	{
		*((T*)result) = thrust::inner_product(THRUST_PAR, a, a + n, b, T());
	}
	else if (sa == 1 && sb != 1)
	{
		auto strideB = make_strided_range(b, n, sb);
		*((T*)result) = thrust::inner_product(THRUST_PAR, a, a + n, strideB.begin(), T());
	}
	else if (sa != 1 && sb == 1)
	{
		auto strideA = make_strided_range(a, n, sa);
		*((T*)result) = thrust::inner_product(THRUST_PAR, strideA.begin(), strideA.end(), b, T());
	}
	else
	{
		auto strideA = make_strided_range(a, n, sa);
		auto strideB = make_strided_range(b, n, sb);
		*((T*)result) = thrust::inner_product(THRUST_PAR, strideA.begin(), strideA.end(), strideB.begin(), T());
	}
	return 0;
}

template <typename T>
struct conjMultiply_functor
{
	__host__ __device__ const T operator()(const T& x, const T& y) const
	{
		return std::conj(x) * y;
	}
};

template<typename T>
inline int vectorsInnerConjugateA(const void* av, const void* bv, const size_t n, const unsigned int sa, const unsigned int sb, void* result)
{
	const T* a = (const T*)av;
	const T* b = (const T*)bv;
	if (sa == 1 && sb == 1)
	{
		*((T*)result) = thrust::inner_product(THRUST_PAR, a, a + n, b, T(), plus_functor<T>(), conjMultiply_functor<T>());
	}
	else if (sa == 1 && sb != 1)
	{
		auto strideB = make_strided_range(b, n, sb);
		*((T*)result) = thrust::inner_product(THRUST_PAR, a, a + n, strideB.begin(), T(), plus_functor<T>(), conjMultiply_functor<T>());
	}
	else if (sa != 1 && sb == 1)
	{
		auto strideA = make_strided_range(a, n, sa);
		*((T*)result) = thrust::inner_product(THRUST_PAR, strideA.begin(), strideA.end(), b, T(), plus_functor<T>(), conjMultiply_functor<T>());
	}
	else
	{
		auto strideA = make_strided_range(a, n, sa);
		auto strideB = make_strided_range(b, n, sb);
		*((T*)result) = thrust::inner_product(THRUST_PAR, strideA.begin(), strideA.end(), strideB.begin(), T(), plus_functor<T>(), conjMultiply_functor<T>());
	}
	return 0;
}

DLLEXP
int vecDot(const DataType type, const void* a, const void* b, const size_t n, const size_t strideA, const size_t strideB, void* result)
{
	AUTO_ALLTYPE_FUNC(vectorsInner, type, int, a, b, n, strideA, strideB, result);
}

DLLEXP
int vecDotc(const DataType type, const void* a, const void* b, const size_t n, const size_t strideA, const size_t strideB, void* result)
{
	AUTO_COMPLEX_TYPE_FUNC(vectorsInnerConjugateA, type, int, a, b, n, strideA, strideB, result);
}
#pragma endregion


#pragma region vector min max
template<typename T>
struct absCompare_functor
{
	__host__ __device__ const bool operator()(const T& x, const T& y) const
	{
		return std::abs(x) < std::abs(y);
	}
};

template<typename T>
inline size_t vectorArgAbsMin(const void* av, const size_t n, const size_t stride)
{
	const T* a = (const T*)av;
	if (stride == 1)
	{
		const T* elemPtr = thrust::min_element(THRUST_PAR, a, a + n, absCompare_functor<T>());
		return elemPtr - a;
	}
	else
	{
		auto strideA = make_strided_range(a, n, stride);
		auto elemPtr = thrust::min_element(THRUST_PAR, strideA.begin(), strideA.end(), absCompare_functor<T>());
		return stride * (elemPtr - strideA.begin());
	}
	// fake return for NVCC
	return 0;
}

template<typename T>
inline size_t vectorArgAbsMax(const void* av, const size_t n, const size_t stride)
{
	const T* a = (const T*)av;
	if (stride == 1)
	{
		const T* elemPtr = thrust::max_element(THRUST_PAR, a, a + n, absCompare_functor<T>());
		return elemPtr - a;
	}
	else
	{
		auto strideA = make_strided_range(a, n, stride);
		auto elemPtr = thrust::max_element(THRUST_PAR, strideA.begin(), strideA.end(), absCompare_functor<T>());
		return stride * (elemPtr - strideA.begin());
	}
	// fake return for NVCC
	return 0;
}

DLLEXP
size_t vecArgAbsMin(const DataType type, void* a, const size_t n, const size_t stride)
{
	AUTO_ALLTYPE_FUNC(vectorArgAbsMin, type, size_t, a, n, stride);
}

DLLEXP
size_t vecArgAbsMax(const DataType type, void* a, const size_t n, const size_t stride)
{
	AUTO_ALLTYPE_FUNC(vectorArgAbsMax, type, size_t, a, n, stride);
}
#pragma endregion


#pragma region vector aggregate -- sum
template<typename T>
inline int vectorSum(const void* av, const size_t n, const size_t stride, void* outv)
{
	const T* a = (const T*)av;
	T* outSum = (T*)outv;
	if (stride == 1)
	{
		*outSum = thrust::reduce(THRUST_PAR, a, a + n, T(), plus_functor<T>());
	}
	else
	{
		auto strideA = make_strided_range(a, n, stride);
		*outSum = thrust::reduce(THRUST_PAR, strideA.begin(), strideA.end(), T(), plus_functor<T>());
	}
	return 0;
}

DLLEXP
int vecSum(const DataType type, void* a, const size_t n, const size_t stride, void* outSum)
{
	AUTO_ALLTYPE_FUNC(vectorSum, type, int, a, n, stride, outSum);
}
#pragma endregion


#pragma region vector aggregate -- product
template<typename T>
inline int vectorAccumulateProduct(const void* av, const size_t n, const size_t stride, void* outv)
{
	const T* a = (const T*)av;
	T* outProd = (T*)outv;
	if (stride == 1)
	{
		*outProd = thrust::reduce(THRUST_PAR, a, a + n, T(1), thrust::multiplies<T>());
	}
	else
	{
		auto strideA = make_strided_range(a, n, stride);
		*outProd = thrust::reduce(THRUST_PAR, strideA.begin(), strideA.end(), T(1), thrust::multiplies<T>());
	}
	return 0;
}

DLLEXP
int vecProd(const DataType type, void* a, const size_t n, const size_t stride, void* outProd)
{
	AUTO_ALLTYPE_FUNC(vectorAccumulateProduct, type, int, a, n, stride, outProd);
}
#pragma endregion


#pragma region vector aggregate -- partial sum
template<typename T>
inline int vectorPartialSum(const void* srcv, void* dstv, const size_t n, const bool inclusive, const size_t strideSrc, const size_t strideDst)
{
	const T* src = (const T*)srcv;
	T* dst = (T*)dstv;
	if (strideSrc == 1 && strideDst == 1)
	{
		if (inclusive)
			thrust::inclusive_scan(THRUST_PAR, src, src + n, dst, plus_functor<T>());
		else
			thrust::exclusive_scan(THRUST_PAR, src, src + n, dst, T(), plus_functor<T>());
	}
	else if (strideSrc == 1 && strideDst != 1)
	{
		auto stridedDst = make_strided_range(dst, n, strideDst);
		if (inclusive)
			thrust::inclusive_scan(THRUST_PAR, src, src + n, stridedDst.begin(), plus_functor<T>());
		else
			thrust::exclusive_scan(THRUST_PAR, src, src + n, stridedDst.begin(), T(), plus_functor<T>());
	}
	else if (strideSrc != 1 && strideDst == 1)
	{
		auto stridedSrc = make_strided_range(src, n, strideSrc);
		if (inclusive)
			thrust::inclusive_scan(THRUST_PAR, stridedSrc.begin(), stridedSrc.end(), dst, plus_functor<T>());
		else
			thrust::exclusive_scan(THRUST_PAR, stridedSrc.begin(), stridedSrc.end(), dst, T(), plus_functor<T>());
	}
	else
	{
		auto stridedSrc = make_strided_range(src, n, strideSrc);
		auto stridedDst = make_strided_range(dst, n, strideDst);
		if (inclusive)
			thrust::inclusive_scan(THRUST_PAR, stridedSrc.begin(), stridedSrc.end(), stridedDst.begin(), plus_functor<T>());
		else
			thrust::exclusive_scan(THRUST_PAR, stridedSrc.begin(), stridedSrc.end(), stridedDst.begin(), T(), plus_functor<T>());
	}
	return 0;
}

DLLEXP
int vecParSum(const DataType type, const void* src, void* dst, const size_t n, const bool inclusive, const size_t strideSrc, const size_t strideDst)
{
	AUTO_ALLTYPE_FUNC(vectorPartialSum, type, int, src, dst, n, inclusive, strideSrc, strideDst);
}
#pragma endregion


#pragma region vector aggregate -- partial product
template<typename T>
inline int vectorPartialProduct(const void* srcv, void* dstv, const size_t n, const bool inclusive, const size_t strideSrc, const size_t strideDst)
{
	const T* src = (const T*)srcv;
	T* dst = (T*)dstv;
	if (strideSrc == 1 && strideDst == 1)
	{
		if (inclusive)
			thrust::inclusive_scan(THRUST_PAR, src, src + n, dst, thrust::multiplies<T>());
		else
			thrust::exclusive_scan(THRUST_PAR, src, src + n, dst, T(1), thrust::multiplies<T>());
	}
	else if (strideSrc == 1 && strideDst != 1)
	{
		auto stridedDst = make_strided_range(dst, n, strideDst);
		if (inclusive)
			thrust::inclusive_scan(THRUST_PAR, src, src + n, stridedDst.begin(), thrust::multiplies<T>());
		else
			thrust::exclusive_scan(THRUST_PAR, src, src + n, stridedDst.begin(), T(1), thrust::multiplies<T>());
	}
	else if (strideSrc != 1 && strideDst == 1)
	{
		auto stridedSrc = make_strided_range(src, n, strideSrc);
		if (inclusive)
			thrust::inclusive_scan(THRUST_PAR, stridedSrc.begin(), stridedSrc.end(), dst, thrust::multiplies<T>());
		else
			thrust::exclusive_scan(THRUST_PAR, stridedSrc.begin(), stridedSrc.end(), dst, T(1), thrust::multiplies<T>());
	}
	else
	{
		auto stridedSrc = make_strided_range(src, n, strideSrc);
		auto stridedDst = make_strided_range(dst, n, strideDst);
		if (inclusive)
			thrust::inclusive_scan(THRUST_PAR, stridedSrc.begin(), stridedSrc.end(), stridedDst.begin(), thrust::multiplies<T>());
		else
			thrust::exclusive_scan(THRUST_PAR, stridedSrc.begin(), stridedSrc.end(), stridedDst.begin(), T(1), thrust::multiplies<T>());
	}
	return 0;
}

DLLEXP
int vecParProd(const DataType type, const void* src, void* dst, const size_t n,const bool inclusive, const size_t strideSrc, const size_t strideDst)
{
	AUTO_ALLTYPE_FUNC(vectorPartialProduct, type, int, src, dst, n, inclusive, strideSrc, strideDst);
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
int vecSort(const DataType type, void* vec, const size_t n, const size_t stride)
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
#ifndef HAS_LDBL
#define __ALLTYPE_FUNC(dataType, ...) do { \
		switch (dataType) \
		{ \
		case RealFloat32: \
			return vectorSortBy<T, float>(__VA_ARGS__); \
		case RealFloat64: \
			return vectorSortBy<T, double>(__VA_ARGS__); \
		case ComplexFloat32: \
			return vectorSortBy<T, BlasSupp::complex<float>>(__VA_ARGS__); \
		case ComplexFloat64: \
			return vectorSortBy<T, BlasSupp::complex<double>>(__VA_ARGS__); \
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
			return vectorSortBy<T, BlasSupp::complex<int8_t>>(__VA_ARGS__); \
		case ComplexInt16: \
			return vectorSortBy<T, BlasSupp::complex<int16_t>>(__VA_ARGS__); \
		case ComplexInt32: \
			return vectorSortBy<T, BlasSupp::complex<int32_t>>(__VA_ARGS__); \
		case ComplexInt64: \
			return vectorSortBy<T, BlasSupp::complex<int64_t>>(__VA_ARGS__); \
		case ComplexUInt8: \
			return vectorSortBy<T, BlasSupp::complex<uint8_t>>(__VA_ARGS__); \
		case ComplexUInt16: \
			return vectorSortBy<T, BlasSupp::complex<uint16_t>>(__VA_ARGS__); \
		case ComplexUInt32: \
			return vectorSortBy<T, BlasSupp::complex<uint32_t>>(__VA_ARGS__); \
		case ComplexUInt64: \
			return vectorSortBy<T, BlasSupp::complex<uint64_t>>(__VA_ARGS__); \
		default: \
			UNSUPPORT(vectorSortBy, dataType, int); \
		} \
	} while (0)
#else
#define __ALLTYPE_FUNC(funcName, dataType, returnType, ...) do { \
		switch (dataType) \
		{ \
		case RealFloat32: \
			return vectorSortBy<T, float>(__VA_ARGS__); \
		case RealFloat64: \
			return vectorSortBy<T, double>(__VA_ARGS__); \
		case RealLongFloat64: \
			return vectorSortBy<T, long double>(__VA_ARGS__); \
		case ComplexFloat32: \
			return vectorSortBy<T, BlasSupp::complex<float>>(__VA_ARGS__); \
		case ComplexFloat64: \
			return vectorSortBy<T, BlasSupp::complex<double>>(__VA_ARGS__); \
		case ComplexLongFloat64: \
			return vectorSortBy<T, BlasSupp::complex<long double>>(__VA_ARGS__); \
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
			return vectorSortBy<T, BlasSupp::complex<int8_t>>(__VA_ARGS__); \
		case ComplexInt16: \
			return vectorSortBy<T, BlasSupp::complex<int16_t>>(__VA_ARGS__); \
		case ComplexInt32: \
			return vectorSortBy<T, BlasSupp::complex<int32_t>>(__VA_ARGS__); \
		case ComplexInt64: \
			return vectorSortBy<T, BlasSupp::complex<int64_t>>(__VA_ARGS__); \
		case ComplexUInt8: \
			return vectorSortBy<T, BlasSupp::complex<uint8_t>>(__VA_ARGS__); \
		case ComplexUInt16: \
			return vectorSortBy<T, BlasSupp::complex<uint16_t>>(__VA_ARGS__); \
		case ComplexUInt32: \
			return vectorSortBy<T, BlasSupp::complex<uint32_t>>(__VA_ARGS__); \
		case ComplexUInt64: \
			return vectorSortBy<T, BlasSupp::complex<uint64_t>>(__VA_ARGS__); \
		default: \
			UNSUPPORT(vectorSortBy, dataType, int); \
		} \
	} while (0)
#endif
	
__ALLTYPE_FUNC(valType, keys, vals, n, strideKey, strideVal);

#undef __ALLTYPE_FUNC
}

DLLEXP
int vecSortBy(const DataType keyType, const DataType valType, void* keys, void* vals, const size_t n, const size_t strideKey, const size_t strideVal)
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
int vecFind(const DataType type, const bool sorted, const void* v, const size_t n, const size_t stride, const void* toFind, ptrdiff_t& index)
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
int vecFillRange(const DataType type, void* v, const size_t n, const size_t stride, const void* start, const void* step)
{
	AUTO_ALLTYPE_FUNC(vectorFillRange, type, int, v, n, stride, start, step);
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
int vecBound(const DataType type, const bool lower, const void* v, const size_t n, const size_t stride, const void* find, ptrdiff_t& index)
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