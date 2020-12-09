// self defined macro
#include "macro.h"


#pragma region get GPU properties
DLLEXP
cudaError getDeviceComputeCapability(int deviceID, int& major, int& minor)
{
	cudaDeviceProp prop;
	cudaError err;
	err = cudaGetDeviceProperties(&prop, deviceID);
	major = prop.major; minor = prop.minor;
	return err;
}
#pragma endregion



#pragma region element-wise multiply and divide
template<typename T>
inline void vectorsElementWiseMultiplyDivide(void* av, const void* bv, const size_t N, const unsigned int stride, bool multiply)
{
	T* a = (T*)av;
	const T* b = (const T*)bv;
	if (stride == 1)
	{
		if (multiply)
			thrust::transform(THRUST_PAR, a, a + N, b, a, thrust::multiplies<T>());
		else
			thrust::transform(THRUST_PAR, a, a + N, b, a, thrust::divides<T>());
	}
	else
	{
		auto strideA = make_strided_range(a, N, stride);
		auto strideB = make_strided_range(b, N, stride);
		if (multiply)
			thrust::transform(THRUST_PAR, strideA.begin(), strideA.end(), strideB.begin(), strideA.begin(), thrust::multiplies<T>());
		else
			thrust::transform(THRUST_PAR, strideA.begin(), strideA.end(), strideB.begin(), strideA.begin(), thrust::divides<T>());
	}
}

DLLEXP
void vecEWMulDiv(const Datatype::DataType type, void* a, const void* b, const size_t N, const unsigned int stride, bool multiply)
{
	AUTO_ALLTYPE_FUNC(vectorsElementWiseMultiplyDivide, type, a, b, N, stride, multiply);
}
#pragma endregion


#pragma region element-wise power
template<typename T>
struct floatPower_functor
{
	const T p;

	floatPower_functor(const T pow) : p(pow) {}

	__host__ __device__ T operator()(const T x) const
	{
		return std::pow(x, p);
	}
};

template<typename T>
inline void vectorElementWisePower(void* av, const void* pv, const size_t N, const unsigned int stride)
{
	T* a = (T*)av;
	const T p = *(T*)pv;
	if (stride == 1)
	{
		thrust::transform(THRUST_PAR, a, a + N, a, floatPower_functor<T>(p));
	}
	else
	{
		auto strideA = make_strided_range(a, N, stride);
		thrust::transform(THRUST_PAR, strideA.begin(), strideA.end(), strideA.begin(), floatPower_functor<T>(p));
	}
}

DLLEXP
void vecEWPow(const Datatype::DataType type, void* a, const void* p, const size_t N, const unsigned int stride)
{
	AUTO_FLOAT_FUNC(vectorElementWisePower, type, a, p, N, stride);
}
#pragma endregion


#pragma region fill array with value
template<typename T>
inline void vectorFillWith(void* av, const void* val, const size_t N, const unsigned int stride)
{
	T* a = (T*)av;
	T v = *(T*)val;
	if (stride == 1)
	{
		thrust::fill_n(THRUST_PAR, a, N, v);
	}
	else
	{
		auto strideA = make_strided_range(a, N, stride);
		thrust::fill(THRUST_PAR, strideA.begin(), strideA.end(), val);
	}
}

DLLEXP
void fillVal(const Datatype::DataType type, void* a, const void* val, const size_t N, const unsigned int stride)
{
	AUTO_ALLTYPE_FUNC(vectorFillWith, type, a, val, N, stride);
}
#pragma endregion


#pragma region array conjugate
template<typename T>
struct floatConjugate_functor
{
	__host__ __device__ T operator()(const T x) const
	{
		return std::conjAllCase(x);
	}
};

template<typename T>
inline void vecConjugate(void* av, const size_t N, const unsigned int stride)
{
	T* a = (T*)av;
	if (stride == 1)
	{
		thrust::transform(THRUST_PAR, a, a + N, a, floatConjugate_functor<T>());
	}
	else
	{
		auto strideA = make_strided_range(a, N, stride);
		thrust::transform(THRUST_PAR, strideA.begin(), strideA.end(), strideA.begin(), floatConjugate_functor<T>());
	}
}

DLLEXP
void vecConj(const Datatype::DataType type, void* a, const size_t N, const unsigned int stride)
{
	AUTO_SIGNED_TYPE_FUNC(vecConjugate, type, a, N, stride);
}
#pragma endregion


#pragma region data type cast
template <typename RealIn, typename RealOut>
struct realTypeConvert_functor
{
	__host__ __device__ RealOut operator()(const RealIn x) const
	{
		return (RealOut)x;
	}
};
template <typename RealIn, typename RealOut>
struct realToComplex_functor
{
	__host__ __device__ std::complex<RealOut> operator()(const RealIn x) const
	{
		return std::complex<RealOut>((RealOut)x);
	}
};
template <typename RealIn, typename RealOut>
struct complexToRealAbs_functor
{
	__host__ __device__ RealOut operator()(const std::complex<RealIn> x) const
	{
		return (RealOut)std::abs(x);
	}
};
template <typename RealIn, typename RealOut>
struct complexToRealPart_functor
{
	__host__ __device__ RealOut operator()(const std::complex<RealIn> x) const
	{
		return (RealOut)x.real();
	}
};

template <typename RealIn, typename RealOut>
inline void vectorComplexToReal(const void* srcv, void* dstv, const size_t N, const unsigned int stride, const bool toRealByAbs)
{
	const std::complex<RealIn>* src = (const std::complex<RealIn>*)srcv;
	RealOut* dst = (RealOut*)dstv;
	if (stride == 1)
	{
		if (toRealByAbs)
			thrust::transform(THRUST_PAR, src, src + N, dst, complexToRealAbs_functor<RealIn, RealOut>());
		else
			thrust::transform(THRUST_PAR, src, src + N, dst, complexToRealPart_functor<RealIn, RealOut>());
	}
	else
	{
		auto strideSrc = make_strided_range(src, N, stride);
		auto strideDst = make_strided_range(dst, N, stride);
		if (toRealByAbs)
			thrust::transform(THRUST_PAR, strideSrc.begin(), strideSrc.end(), strideDst.begin(), complexToRealAbs_functor<RealIn, RealOut>());
		else
			thrust::transform(THRUST_PAR, strideSrc.begin(), strideSrc.end(), strideDst.begin(), complexToRealPart_functor<RealIn, RealOut>());
	}
}

template <typename RealIn, typename RealOut>
inline void vectorRealToComplex(const void* srcv, void* dstv, const size_t N, const unsigned int stride, const bool toRealByAbs)
{
	const RealIn* src = (const RealIn*)srcv;
	std::complex<RealOut>* dst = (std::complex<RealOut>*)dstv;
	if (stride == 1)
	{
		thrust::transform(THRUST_PAR, src, src + N, dst, realToComplex_functor<RealIn, RealOut>());
	}
	else
	{
		auto strideSrc = make_strided_range(src, N, stride);
		auto strideDst = make_strided_range(dst, N, stride);
		thrust::transform(THRUST_PAR, strideSrc.begin(), strideSrc.end(), strideDst.begin(), realToComplex_functor<RealIn, RealOut>());
	}
}

template <typename RealIn, typename RealOut>
inline void vectorRealConvert(const void* srcv, void* dstv, const size_t N, const unsigned int stride, const bool toRealByAbs)
{
	const RealIn* src = (const RealIn*)srcv;
	RealOut* dst = (RealOut*)dstv;
	if (stride == 1)
	{
		thrust::transform(THRUST_PAR, src, src + N, dst, realTypeConvert_functor<RealIn, RealOut>());
	}
	else
	{
		auto strideSrc = make_strided_range(src, N, stride);
		auto strideDst = make_strided_range(dst, N, stride);
		thrust::transform(THRUST_PAR, strideSrc.begin(), strideSrc.end(), strideDst.begin(), realTypeConvert_functor<RealIn, RealOut>());
	}
}

DLLEXP
void vecDataConvert(const Datatype::DataType srcType, const Datatype::DataType dstType, const void* src, void* dst, const size_t N, const unsigned int stride, const bool toRealByAbs)
{
	// copy if no data conversion
	if (srcType == dstType)
	{
		if (stride == 1)
		{
			thrust::copy_n(THRUST_PAR, (const char*)src, N * Datatype::size(srcType), (char*)dst);
		}
		else
		{
			size_t NN = N * Datatype::size(srcType);
			auto strideSrc = make_strided_range((const char*)src, NN, stride);
			auto strideDst = make_strided_range((char*)dst, NN, stride);
			thrust::copy(THRUST_PAR, strideSrc.begin(), strideSrc.end(), strideDst);
		}
		return;
	}

// define inner switch
#ifndef HAS_LDBL
#define CONVERT_INNER_SWITCH(type, convert) do { \
		switch (dstType) \
		{ \
		case Datatype::RealSingle: \
		case Datatype::ComplexSingle: \
			convertFunc = convert<type, float>; break; \
		case Datatype::RealDouble: \
		case Datatype::ComplexDouble: \
			convertFunc = convert<type, double>; break; \
		case Datatype::RealInt8: \
		case Datatype::ComplexInt8: \
			convertFunc = convert<type, char>; break; \
		case Datatype::RealInt16: \
		case Datatype::ComplexInt16: \
			convertFunc = convert<type, short>; break; \
		case Datatype::RealInt32: \
		case Datatype::ComplexInt32: \
			convertFunc = convert<type, int>; break; \
		case Datatype::RealInt64: \
		case Datatype::ComplexInt64: \
			convertFunc = convert<type, long long>; break; \
		case Datatype::RealUInt8: \
		case Datatype::ComplexUInt8: \
			convertFunc = convert<type, unsigned char>; break; \
		case Datatype::RealUInt16: \
		case Datatype::ComplexUInt16: \
			convertFunc = convert<type, unsigned short>; break; \
		case Datatype::RealUInt32: \
		case Datatype::ComplexUInt32: \
			convertFunc = convert<type, unsigned int>; break; \
		case Datatype::RealUInt64: \
		case Datatype::ComplexUInt64: \
			convertFunc = convert<type, unsigned long long>; break; \
		default: \
			UNSUPPORT(vecDataConvert, dstType); \
		} \
	} while (0)
#else
#define CONVERT_INNER_SWITCH(type, convert) do { \
		switch (dstType) \
		{ \
		case Datatype::RealSingle: \
		case Datatype::ComplexSingle: \
			convertFunc = convert<type, float>; break; \
		case Datatype::RealDouble: \
		case Datatype::ComplexDouble: \
			convertFunc = convert<type, double>; break; \
		case Datatype::RealLongDouble: \
		case Datatype::ComplexLongDouble: \
			convertFunc = convert<type, long double>; break; \
		case Datatype::RealInt8: \
		case Datatype::ComplexInt8: \
			convertFunc = convert<type, char>; break; \
		case Datatype::RealInt16: \
		case Datatype::ComplexInt16: \
			convertFunc = convert<type, short>; break; \
		case Datatype::RealInt32: \
		case Datatype::ComplexInt32: \
			convertFunc = convert<type, int>; break; \
		case Datatype::RealInt64: \
		case Datatype::ComplexInt64: \
			convertFunc = convert<type, long long>; break; \
		case Datatype::RealUInt8: \
		case Datatype::ComplexUInt8: \
			convertFunc = convert<type, unsigned char>; break; \
		case Datatype::RealUInt16: \
		case Datatype::ComplexUInt16: \
			convertFunc = convert<type, unsigned short>; break; \
		case Datatype::RealUInt32: \
		case Datatype::ComplexUInt32: \
			convertFunc = convert<type, unsigned int>; break; \
		case Datatype::RealUInt64: \
		case Datatype::ComplexUInt64: \
			convertFunc = convert<type, unsigned long long>; break; \
		default: \
			UNSUPPORT(vecDataConvert, dstType); \
		} \
	} while (0)
#endif

// define outer switch
#ifndef HAS_LDBL
#define CONVERT_OUTER_SWITCH(convert) do { \
		switch (srcType) \
		{ \
		case Datatype::RealSingle: \
		case Datatype::ComplexSingle: \
			CONVERT_INNER_SWITCH(float, convert); break; \
		case Datatype::RealDouble: \
		case Datatype::ComplexDouble: \
			CONVERT_INNER_SWITCH(double, convert); break; \
		case Datatype::RealInt8: \
		case Datatype::ComplexInt8: \
			CONVERT_INNER_SWITCH(char, convert); break; \
		case Datatype::RealInt16: \
		case Datatype::ComplexInt16: \
			CONVERT_INNER_SWITCH(short, convert); break; \
		case Datatype::RealInt32: \
		case Datatype::ComplexInt32: \
			CONVERT_INNER_SWITCH(short, convert); break; \
		case Datatype::RealInt64: \
		case Datatype::ComplexInt64: \
			CONVERT_INNER_SWITCH(short, convert); break; \
		case Datatype::RealUInt8: \
		case Datatype::ComplexUInt8: \
			CONVERT_INNER_SWITCH(short, convert); break; \
		case Datatype::RealUInt16: \
		case Datatype::ComplexUInt16: \
			CONVERT_INNER_SWITCH(short, convert); break; \
		case Datatype::RealUInt32: \
		case Datatype::ComplexUInt32: \
			CONVERT_INNER_SWITCH(short, convert); break; \
		case Datatype::RealUInt64: \
		case Datatype::ComplexUInt64: \
			CONVERT_INNER_SWITCH(short, convert); break; \
		default: \
			UNSUPPORT(vecDataConvert, srcType); \
		} \
	} while (0)
#else
#define CONVERT_OUTER_SWITCH(convert) do { \
		switch (srcType) \
		{ \
		case Datatype::RealSingle: \
		case Datatype::ComplexSingle: \
			CONVERT_INNER_SWITCH(float, convert); break; \
		case Datatype::RealDouble: \
		case Datatype::ComplexDouble: \
			CONVERT_INNER_SWITCH(double, convert); break; \
		case Datatype::RealLongDouble: \
		case Datatype::ComplexLongDouble: \
			CONVERT_INNER_SWITCH(long double, convert); break; \
		case Datatype::RealInt8: \
		case Datatype::ComplexInt8: \
			CONVERT_INNER_SWITCH(char, convert); break; \
		case Datatype::RealInt16: \
		case Datatype::ComplexInt16: \
			CONVERT_INNER_SWITCH(short, convert); break; \
		case Datatype::RealInt32: \
		case Datatype::ComplexInt32: \
			CONVERT_INNER_SWITCH(short, convert); break; \
		case Datatype::RealInt64: \
		case Datatype::ComplexInt64: \
			CONVERT_INNER_SWITCH(short, convert); break; \
		case Datatype::RealUInt8: \
		case Datatype::ComplexUInt8: \
			CONVERT_INNER_SWITCH(short, convert); break; \
		case Datatype::RealUInt16: \
		case Datatype::ComplexUInt16: \
			CONVERT_INNER_SWITCH(short, convert); break; \
		case Datatype::RealUInt32: \
		case Datatype::ComplexUInt32: \
			CONVERT_INNER_SWITCH(short, convert); break; \
		case Datatype::RealUInt64: \
		case Datatype::ComplexUInt64: \
			CONVERT_INNER_SWITCH(short, convert); break; \
		default: \
			UNSUPPORT(vecDataConvert, srcType); \
		} \
	} while (0)
#endif

	// otherwise
	auto convertFunc = vectorRealConvert<float, float>; // default convert function
	if (Datatype::isreal(srcType) && Datatype::isreal(dstType))
	{	// real convert
		CONVERT_OUTER_SWITCH(vectorRealConvert);
	}
	else if (Datatype::isreal(srcType))
	{	// real to complex
		CONVERT_OUTER_SWITCH(vectorRealToComplex);
	}
	else if (Datatype::isreal(dstType))
	{	// complex to real, 'toRealByAbs' is only used here
		CONVERT_OUTER_SWITCH(vectorComplexToReal);
	}
	else
	{	// all complex, use the real convert of each part instead
		vecDataConvert(Datatype::realCorrespond(srcType), Datatype::realCorrespond(dstType), src, dst, N * 2, stride == 1 ? 1 : (2 * stride), true);
	}
}
#pragma endregion


#pragma region dense vector set values with small absolutes to zero
template<typename T, typename U>
struct clipAbs_functor
{
	const U b;

	clipAbs_functor(U bound) : b(std::abs(bound)) {}

	__host__ __device__ T operator()(const T x) const
	{
		return std::abs(x) < b ? T() : x;
	}
};

template<typename T>
inline void vectorClip(void* av, const void* threshold, const size_t N, const unsigned int stride)
{
	T* a = (T*)av;
	const T thre = *((const T*)threshold);
	if constexpr (std::is_scalar_v<T>)
	{
		if (stride == 1)
		{
			thrust::transform(THRUST_PAR, a, a + N, a, clipAbs_functor<T, T>(thre));
		}
		else
		{
			auto strideA = make_strided_range(a, N, stride);
			thrust::transform(THRUST_PAR, strideA.begin(), strideA.end(), strideA.begin(), clipAbs_functor<T, T>(thre));
		}
	}
	else
	{
		if (stride == 1)
		{
			thrust::transform(THRUST_PAR, a, a + N, a, clipAbs_functor<T, T::value_type>(std::abs(thre)));
		}
		else
		{
			auto strideA = make_strided_range(a, N, stride);
			thrust::transform(THRUST_PAR, strideA.begin(), strideA.end(), strideA.begin(), clipAbs_functor<T, T::value_type>(std::abs(thre)));
		}
	}
}

DLLEXP
void vecClip(const Datatype::DataType type, void* a, const void* threshold, const size_t N, const unsigned int stride)
{
	AUTO_ALLTYPE_FUNC(vectorClip, type, a, threshold, N, stride);
}
#pragma endregion


#pragma region int operations
DLLEXP
ERROR_RETURN intMinMax(const int* v, const size_t N, int& min, int& max)
{
	auto result = thrust::minmax_element(THRUST_PAR, v, v + N);
#ifdef CPU
	memcpy(&min, result.first, sizeof(int));
	memcpy(&max, result.second, sizeof(int));
#else
	cudaError err = cudaMemcpy(&min, result.first, sizeof(int), cudaMemcpyDeviceToHost);
	if (err != 0) return err;
	err = cudaMemcpy(&max, result.second, sizeof(int), cudaMemcpyDeviceToHost);
	return err;
#endif // CPU
}

DLLEXP
ERROR_RETURN intMax(const int* v, const size_t N, int& max)
{
	const int* result = thrust::max_element(THRUST_PAR, v, v + N);
#ifdef CPU
	memcpy(&max, result, sizeof(int));
#else
	cudaError err = cudaMemcpy(&max, result, sizeof(int), cudaMemcpyDeviceToHost);
	return err;
#endif // CPU
}

DLLEXP
int intLowerBound(const int* v, const size_t N, const int lower)
{
	return thrust::lower_bound(THRUST_PAR, v, v + N, lower) - v;
}

DLLEXP
int intUpperBound(const int* v, const size_t N, const int upper)
{
	return thrust::upper_bound(THRUST_PAR, v, v + N, upper) - v;
}

DLLEXP
int intFind(const int* v, const size_t N, const int toFind)
{
	return thrust::find(THRUST_PAR, v, v + N, toFind) - v;
}

DLLEXP
void intFillRange(int* v, const size_t N, const int start, const int step)
{
	thrust::sequence(THRUST_PAR, v, v + N, start, step);
}
#pragma endregion


#pragma region vector add scalar
template<typename T>
struct addScalar_functor
{
	const T scalar;

	addScalar_functor(const T s) : scalar(s) {}

	__host__ __device__ T operator()(const T x) const
	{
		return x + scalar;
	}
};

template<typename T>
inline void vectorAddedByScalar(void* av, const void* scalar, const size_t N, const unsigned int stride)
{
	T* a = (T*)av;
	const T s = *((const T*)scalar);
	if (stride == 1)
	{
		thrust::transform(THRUST_PAR, a, a + N, a, addScalar_functor<T>(s));
	}
	else
	{
		auto strideA = make_strided_range(a, N, stride);
		thrust::transform(THRUST_PAR, strideA.begin(), strideA.end(), strideA.begin(), addScalar_functor<T>(s));
	}
}

DLLEXP
void vecAddScalar(const Datatype::DataType type, void* a, const void* scalar, const size_t N, const unsigned int stride)
{
	AUTO_ALLTYPE_FUNC(vectorAddedByScalar, type, a, scalar, N, stride);
}
#pragma endregion


#pragma region vector aggregate -- sum
template<typename T>
inline void vectorSum(const void* av, const size_t N, const unsigned int stride, void* outv)
{
	const T* a = (const T*)av;
	T* outSum = (T*)outv;
	if (stride == 1)
	{
		*outSum = thrust::reduce(THRUST_PAR, a, a + N, T());
	}
	else
	{
		auto strideA = make_strided_range(a, N, stride);
		*outSum = thrust::reduce(THRUST_PAR, strideA.begin(), strideA.end(), T());
	}
}

DLLEXP
void vecSum(const Datatype::DataType type, void* a, const size_t N, const unsigned int stride, void* outSum)
{
	AUTO_ALLTYPE_FUNC(vectorSum, type, a, N, stride, outSum);
}
#pragma endregion


#pragma region vector aggregate -- product
template<typename T>
inline void vectorAccumulateProduct(const void* av, const size_t N, const unsigned int stride, void* outv)
{
	const T* a = (const T*)av;
	T* outProd = (T*)outv;
	if (stride == 1)
	{
		*outProd = thrust::reduce(THRUST_PAR, a, a + N, T(), thrust::multiplies<T>());
	}
	else
	{
		auto strideA = make_strided_range(a, N, stride);
		*outProd = thrust::reduce(THRUST_PAR, strideA.begin(), strideA.end(), T(), thrust::multiplies<T>());
	}
}

DLLEXP
void vecProd(const Datatype::DataType type, void* a, const size_t N, const unsigned int stride, void* outProd)
{
	AUTO_ALLTYPE_FUNC(vectorAccumulateProduct, type, a, N, stride, outProd);
}
#pragma endregion


#pragma region vector aggregate -- partial sum
template<typename T>
inline void vectorPartialSum(const void* srcv, void* dstv, const size_t N, const unsigned int stride, const bool inclusive)
{
	const T* src = (const T*)srcv;
	T* dst = (T*)dstv;
	if (stride == 1)
	{
		if (inclusive)
			thrust::inclusive_scan(THRUST_PAR, src, src + N, dst);
		else
			thrust::exclusive_scan(THRUST_PAR, src, src + N, dst);
	}
	else
	{
		auto strideSrc = make_strided_range(src, N, stride);
		auto strideDst = make_strided_range(dst, N, stride);
		if (inclusive)
			thrust::inclusive_scan(THRUST_PAR, strideSrc.begin(), strideSrc.end(), strideDst.begin());
		else
			thrust::exclusive_scan(THRUST_PAR, strideSrc.begin(), strideSrc.end(), strideDst.begin());
	}
}

DLLEXP
void vecParSum(const Datatype::DataType type, const void* src, void* dst, const size_t N, const unsigned int stride, const bool inclusive)
{
	AUTO_ALLTYPE_FUNC(vectorPartialSum, type, src, dst, N, stride, inclusive);
}
#pragma endregion


#pragma region vector aggregate -- partial product
template<typename T>
inline void vectorPartialProduct(const void* srcv, void* dstv, const size_t N, const unsigned int stride, const bool inclusive)
{
	const T* src = (const T*)srcv;
	T* dst = (T*)dstv;
	if (stride == 1)
	{
		if (inclusive)
			thrust::inclusive_scan(THRUST_PAR, src, src + N, dst, thrust::multiplies<T>());
		else
			thrust::exclusive_scan(THRUST_PAR, src, src + N, dst, T(1), thrust::multiplies<T>());
	}
	else
	{
		auto strideSrc = make_strided_range(src, N, stride);
		auto strideDst = make_strided_range(dst, N, stride);
		if (inclusive)
			thrust::inclusive_scan(THRUST_PAR, strideA.begin(), strideA.end(), strideDst.begin(), thrust::multiplies<T>());
		else
			thrust::exclusive_scan(THRUST_PAR, strideA.begin(), strideA.end(), strideDst.begin(), T(1), thrust::multiplies<T>());
	}
}

DLLEXP
void vecParProd(const Datatype::DataType type, const void* src, void* dst, const size_t N, const unsigned int stride, const bool inclusive)
{
	AUTO_ALLTYPE_FUNC(vectorPartialProduct, type, src, dst, N, stride, inclusive);
}
#pragma endregion
