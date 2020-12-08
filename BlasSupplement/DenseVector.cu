// self defined macro
#include "macro.h"


#pragma region get properties
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


#pragma region stride range
// stride range iterator class from NVIDIA/thrust/examples/strided_range.cu
template <typename Iterator>
class StridedRange
{
public:

	typedef typename thrust::iterator_difference<Iterator>::type difference_type;

	struct stride_functor : public thrust::unary_function<difference_type, difference_type>
	{
		const difference_type stride;
		stride_functor(const difference_type stride) : stride(stride) {}

		__host__ __device__ difference_type operator()(const difference_type& i) const
		{
			return stride * i;
		}
	};

	typedef typename thrust::counting_iterator<difference_type>                   CountingIterator;
	typedef typename thrust::transform_iterator<stride_functor, CountingIterator> TransformIterator;
	typedef typename thrust::permutation_iterator<Iterator, TransformIterator>    PermutationIterator;

	// type of the strided_range iterator
	typedef PermutationIterator iterator;

	// construct strided_range for the range [first,last)
	StridedRange(Iterator first, Iterator last, difference_type stride)
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

template <typename Iterator>
inline static StridedRange<Iterator> make_strided_range(Iterator it, size_t N, const StridedRange<Iterator>::difference_type stride)
{
	return StridedRange<Iterator>(it, it + N * stride, stride);
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
	AUTO_ALL_SIGNED_TYPE_FUNC(vecConjugate, type, a, N, stride);
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

// TODO: could support integer types
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

	// otherwise
	if (!Datatype::isfloat(srcType) || !Datatype::isfloat(dstType))
	{
		printf("[vecDataConvert] only supports float types!");
		return;
	}
	const int sizeSrc = Datatype::size(srcType), sizeDst = Datatype::size(dstType);
	auto convertFunc = vectorRealConvert<float, float>; // default convert function
	if (Datatype::isreal(srcType) && Datatype::isreal(dstType))
	{	// real convert
		if (sizeSrc == sizeof(float))
		{
			if (sizeDst == sizeof(double))
				convertFunc = vectorRealConvert<float, double>;
#ifdef HAS_LDBL
			else if (sizeDst == sizeof(long double))
				convertFunc = vectorRealConvert<float, long double>;
#endif // HAS_LDBL
			else
				UNSUPPORT(vecDataConvert, dstType);
		}
		else if (sizeSrc == sizeof(double))
		{
			if (sizeDst == sizeof(float))
				convertFunc = vectorRealConvert<double, float>;
#ifdef HAS_LDBL
			else if (sizeDst == sizeof(long double))
				convertFunc = vectorRealConvert<double, long double>;
#endif // HAS_LDBL
			else
				UNSUPPORT(vecDataConvert, dstType);
		}
#ifdef HAS_LDBL
		else if (sizeSrc == sizeof(long double))
		{
			if (sizeDst == sizeof(float))
				convertFunc = vectorRealConvert<long double, float>;
			else if (sizeDst == sizeof(double))
				convertFunc = vectorRealConvert<long double, double>;
			else
				UNSUPPORT(vecDataConvert, dstType);
		}
#endif // HAS_LDBL
		else
			UNSUPPORT(vecDataConvert, srcType);
	}
	else if (Datatype::isreal(srcType))
	{	// real to complex
		if (sizeSrc == sizeof(float))
		{
			if (sizeDst == sizeof(float))
				convertFunc = vectorRealToComplex<float, float>;
			else if (sizeDst == sizeof(double))
				convertFunc = vectorRealToComplex<float, double>;
#ifdef HAS_LDBL
			else if (sizeDst == sizeof(long double))
				convertFunc = vectorRealToComplex<float, long double>;
#endif // HAS_LDBL
			else
				UNSUPPORT(vecDataConvert, dstType);
		}
		else if (sizeSrc == sizeof(double))
		{
			if (sizeDst == sizeof(float))
				convertFunc = vectorRealToComplex<double, float>;
			if (sizeDst == sizeof(double))
				convertFunc = vectorRealToComplex<double, double>;
#ifdef HAS_LDBL
			else if (sizeDst == sizeof(long double))
				convertFunc = vectorRealToComplex<double, long double>;
#endif // HAS_LDBL
			else
				UNSUPPORT(vecDataConvert, dstType);
		}
#ifdef HAS_LDBL
		else if (sizeSrc == sizeof(long double))
		{
			if (sizeDst == sizeof(float))
				convertFunc = vectorRealToComplex<long double, float>;
			else if (sizeDst == sizeof(double))
				convertFunc = vectorRealToComplex<long double, double>;
			else if (sizeDst == sizeof(long double))
				convertFunc = vectorRealToComplex<long double, long double>;
			else
				UNSUPPORT(vecDataConvert, dstType);
		}
#endif // HAS_LDBL
		else
			UNSUPPORT(vecDataConvert, srcType);
	}
	else if (Datatype::isreal(dstType))
	{	// complex to real, 'toRealByAbs' is only used here
		if (sizeSrc == sizeof(float))
		{
			if (sizeDst == sizeof(float))
				convertFunc = vectorComplexToReal<float, float>;
			else if (sizeDst == sizeof(double))
				convertFunc = vectorComplexToReal<float, double>;
#ifdef HAS_LDBL
			else if (sizeDst == sizeof(long double))
				convertFunc = vectorComplexToReal<float, long double>;
#endif // HAS_LDBL
			else
				UNSUPPORT(vecDataConvert, dstType);
		}
		else if (sizeSrc == sizeof(double))
		{
			if (sizeDst == sizeof(float))
				convertFunc = vectorComplexToReal<double, float>;
			if (sizeDst == sizeof(double))
				convertFunc = vectorComplexToReal<double, double>;
#ifdef HAS_LDBL
			else if (sizeDst == sizeof(long double))
				convertFunc = vectorComplexToReal<double, long double>;
#endif // HAS_LDBL
			else
				UNSUPPORT(vecDataConvert, dstType);
		}
#ifdef HAS_LDBL
		else if (sizeSrc == sizeof(long double))
		{
			if (sizeDst == sizeof(float))
				convertFunc = vectorComplexToReal<long double, float>;
			else if (sizeDst == sizeof(double))
				convertFunc = vectorComplexToReal<long double, double>;
			else if (sizeDst == sizeof(long double))
				convertFunc = vectorComplexToReal<long double, long double>;
			else
				UNSUPPORT(vecDataConvert, dstType);
		}
#endif // HAS_LDBL
		else
			UNSUPPORT(vecDataConvert, srcType);
	}
	else
	{
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
inline T vectorSum(const void* av, const size_t N, const unsigned int stride)
{
	const T* a = (const T*)av;
	if (stride == 1)
	{
		return thrust::reduce(THRUST_PAR, a, a + N, T());
	}
	else
	{
		auto strideA = make_strided_range(a, N, stride);
		return thrust::reduce(THRUST_PAR, strideA.begin(), strideA.end(), T());
	}
}

DLLEXP
void vecSum(const Datatype::DataType type, void* a, const size_t N, const unsigned int stride)
{
	AUTO_ALLTYPE_FUNC(vectorSum, type, a, N, stride);
}
#pragma endregion


#pragma region vector aggregate -- product
template<typename T>
inline T vectorAccumulateProduct(const void* av, const size_t N, const unsigned int stride)
{
	const T* a = (const T*)av;
	if (stride == 1)
	{
		return thrust::reduce(THRUST_PAR, a, a + N, T(), thrust::multiplies<T>());
	}
	else
	{
		auto strideA = make_strided_range(a, N, stride);
		return thrust::reduce(THRUST_PAR, strideA.begin(), strideA.end(), T(), thrust::multiplies<T>());
	}
}

DLLEXP
void vecProd(const Datatype::DataType type, void* a, const size_t N, const unsigned int stride)
{
	AUTO_ALLTYPE_FUNC(vectorAccumulateProduct, type, a, N, stride);
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
