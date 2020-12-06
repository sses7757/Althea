// self defined macro
#include "macro.h"


#pragma region get properties
EXTERN_C
DLLEXP cudaError getDeviceComputeCapability(int deviceID, int& major, int& minor)
{
	cudaDeviceProp prop;
	cudaError err;
	err = cudaGetDeviceProperties(&prop, deviceID);
	major = prop.major; minor = prop.minor;
	return err;
}
END_EXTERN_C
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
#pragma endregion


#pragma region element-wise multiply and divide
template<typename T>
__inline__ void vectorsElementWiseMultiplyDivide(T* a, const T* b, const size_t N, const unsigned int stride, bool multiply)
{
	if (stride == 1)
	{
		if (multiply)
			thrust::transform(THRUST_PAR, a, a + N, b, a, thrust::multiplies<T>());
		else
			thrust::transform(THRUST_PAR, a, a + N, b, a, thrust::divides<T>());
	}
	else
	{
		StridedRange<const T*> strideA(a, a + N * stride, stride);
		StridedRange<const T*> strideB(b, b + N * stride, stride);
		if (multiply)
			thrust::transform(THRUST_PAR, strideA.begin(), strideA.end(), strideB.begin(), strideA.begin(), thrust::multiplies<T>());
		else
			thrust::transform(THRUST_PAR, strideA.begin(), strideA.end(), strideB.begin(), strideA.begin(), thrust::divides<T>());
	}
}

EXTERN_C
DLLEXP void vecEWMulDivS(float* a, const float* b, const size_t N, const unsigned int stride, bool multiply)
{
	vectorsElementWiseMultiplyDivide(a, b, N, stride, multiply);
}
DLLEXP void vecEWMulDivD(double* a, const double* b, const size_t N, const unsigned int stride, bool multiply)
{
	vectorsElementWiseMultiplyDivide(a, b, N, stride, multiply);
}
DLLEXP void vecEWMulDivC(complexFloat* a, const complexFloat* b, const size_t N, const unsigned int stride, bool multiply)
{
	vectorsElementWiseMultiplyDivide(a, b, N, stride, multiply);
}
DLLEXP void vecEWMulDivZ(complexDouble* a, const complexDouble* b, const size_t N, const unsigned int stride, bool multiply)
{
	vectorsElementWiseMultiplyDivide(a, b, N, stride, multiply);
}
END_EXTERN_C
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
void vectorElementWisePower(T* a, const T p, const size_t N, const unsigned int stride)
{
	if (stride == 1)
	{
		thrust::transform(THRUST_PAR, a, a + N, a, floatPower_functor<T>(p));
	}
	else
	{
		StridedRange<const T*> strideA(a, a + N * stride, stride);
		thrust::transform(THRUST_PAR, strideA.begin(), strideA.end(), strideA.begin(), floatPower_functor<T>(p));
	}
}

EXTERN_C
DLLEXP void vecEWPowS(float* a, const float p, const size_t N, const unsigned int stride)
{
	vectorElementWisePower(a, p, N, stride);
}
DLLEXP void vecEWPowD(double* a, const double p, const size_t N, const unsigned int stride)
{
	vectorElementWisePower(a, p, N, stride);
}
DLLEXP void vecEWPowC(complexFloat* a, const complexFloat p, const size_t N, const unsigned int stride)
{
	vectorElementWisePower(a, p, N, stride);
}
DLLEXP void vecEWPowZ(complexDouble* a, const complexDouble p, const size_t N, const unsigned int stride)
{
	vectorElementWisePower(a, p, N, stride);
}
END_EXTERN_C
#pragma endregion


#pragma region fill array with ones
template<typename T>
void vectorFillWith(T* a, const T val, const size_t N, const unsigned int stride)
{
	if (stride == 1)
	{
		thrust::fill_n(THRUST_PAR, a, N, val);
	}
	else
	{
		StridedRange<const T*> strideA(a, a + N * stride, stride);
		thrust::fill(THRUST_PAR, strideA.begin(), strideA.end(), val);
	}
}

EXTERN_C
DLLEXP void fillValS(float* a, const float val, const size_t N, const unsigned int stride)
{
	vectorFillWith(a, val, N, stride);
}
DLLEXP void fillValD(double* a, const double val, const size_t N, const unsigned int stride)
{
	vectorFillWith(a, val, N, stride);
}
DLLEXP void fillValC(complexFloat* a, const complexFloat val, const size_t N, const unsigned int stride)
{
	vectorFillWith(a, val, N, stride);
}
DLLEXP void fillValZ(complexDouble* a, const complexDouble val, const size_t N, const unsigned int stride)
{
	vectorFillWith(a, val, N, stride);
}
END_EXTERN_C
#pragma endregion


#pragma region array conjugate
template<typename T>
struct floatConjugate_functor
{
	__host__ __device__ T operator()(const T x) const
	{
		T conj = T(x);
		conj.x = -conj.x;
		return conj;
	}
};

template<typename T>
void vecConjugate(T* a, const size_t N, const unsigned int stride)
{
	if (stride == 1)
	{
		thrust::transform(THRUST_PAR, a, a + N, a, floatConjugate_functor<T>());
	}
	else
	{
		StridedRange<const T*> strideA(a, a + N * stride, stride);
		thrust::transform(THRUST_PAR, strideA.begin(), strideA.end(), strideA.begin(), floatConjugate_functor<T>());
	}
}

EXTERN_C
DLLEXP void vecConjC(complexFloat* a, const size_t N, const unsigned int stride)
{
	vecConjugate(a, N, stride);
}
DLLEXP void vecConjZ(complexDouble* a, const size_t N, const unsigned int stride)
{
	vecConjugate(a, N, stride);
}
END_EXTERN_C
#pragma endregion


#pragma region up-cast and down-cast
struct singleToDouble_functor
{
	__host__ __device__ double operator()(const float x) const
	{
		return x;
	}
};
struct doubleToSingle_functor
{
	__host__ __device__ float operator()(const double x) const
	{
		return (float)x;
	}
};
template <typename Complex, typename Real>
struct complexToRealPart_functor
{
	__host__ __device__ Real operator()(const Complex x) const
	{
		return x.x;
	}
};
template <typename Complex, typename Real>
struct complexToRealAbs_functor
{
	__host__ __device__ Real operator()(const Complex x) const
	{
		return std::abs(x);
	}
};
// real to complex can be done by strided copies

template <typename Complex, typename Real>
void vecComplexToReal(Real* dest, const Complex* src, const size_t N, const unsigned int stride)
{
	if (stride == 1)
	{
		thrust::transform(THRUST_PAR, src, src + N, dest, complexToRealAbs_functor<Complex, Real>());
	}
	else
	{
		StridedRange<const Complex*> strideSrc(src, src + N * stride, stride);
		StridedRange<const Real*> strideDst(dest, dest + N * stride, stride);
		thrust::transform(THRUST_PAR, strideSrc.begin(), strideSrc.end(), strideDst.begin(), complexToRealAbs_functor<Complex, Real>());
	}
}

EXTERN_C
DLLEXP void vecSingleToDouble(double* dest, const float* src, const size_t N, const unsigned int stride)
{
	if (stride == 1)
	{
		thrust::transform(THRUST_PAR, src, src + N, dest, singleToDouble_functor());
	}
	else
	{
		StridedRange<const float*> strideSrc(src, src + N * stride, stride);
		StridedRange<const double*> strideDst(dest, dest + N * stride, stride);
		thrust::transform(THRUST_PAR, strideSrc.begin(), strideSrc.end(), strideDst.begin(), singleToDouble_functor());
	}
}

DLLEXP void vecDoubleToSingle(float* dest, const double* src, const size_t N, const unsigned int stride)
{
	if (stride == 1)
	{
		thrust::transform(THRUST_PAR, src, src + N, dest, doubleToSingle_functor());
	}
	else
	{
		StridedRange<const double*> strideSrc(src, src + N * stride, stride);
		StridedRange<const float*> strideDst(dest, dest + N * stride, stride);
		thrust::transform(THRUST_PAR, strideSrc.begin(), strideSrc.end(), strideDst.begin(), doubleToSingle_functor());
	}
}

DLLEXP void vecComplexDoubleToReal(double* dest, const complexDouble* src, const size_t N, const unsigned int stride)
{
	vecComplexToReal(dest, src, N, stride);
}
DLLEXP void vecComplexSingleToReal(float* dest, const complexFloat* src, const size_t N, const unsigned int stride)
{
	vecComplexToReal(dest, src, N, stride);
}
END_EXTERN_C
#pragma endregion


#pragma region dense vector set values with small absolutes to zero
template<typename T, typename Bound>
struct clipAbs_functor
{
	const Bound b;

	clipAbs_functor(Bound bound) : b(bound) {}

	__host__ __device__ T operator()(const T x) const
	{
		return std::abs(x) < b ? T() : x;
	}
};

template<typename T, typename Bound>
__inline__ void vectorClip(T* a, const Bound threshold, const size_t N, const unsigned int stride)
{
	if (stride == 1)
	{
		thrust::transform(THRUST_PAR, a, a + N, a, clipAbs_functor<T, Bound>(threshold));
	}
	else
	{
		StridedRange<const T*> strideA(a, a + N * stride, stride);
		thrust::transform(THRUST_PAR, strideA.begin(), strideA.end(), strideA.begin(), clipAbs_functor<T, Bound>(threshold));
	}
}

EXTERN_C
DLLEXP void vecClipS(float* a, const float threshold, const size_t N, const unsigned int stride)
{
	vectorClip(a, threshold, N, stride);
}
DLLEXP void vecClipD(double* a, const float threshold, const size_t N, const unsigned int stride)
{
	vectorClip(a, (double)threshold, N, stride);
}
DLLEXP void vecClipC(complexFloat* a, const float threshold, const size_t N, const unsigned int stride)
{
	vectorClip(a, threshold, N, stride);
}
DLLEXP void vecClipZ(complexDouble* a, const float threshold, const size_t N, const unsigned int stride)
{
	vectorClip(a, (double)threshold, N, stride);
}
END_EXTERN_C
#pragma endregion


#pragma region int operations
EXTERN_C
DLLEXP cudaError intMinMax(const int* v, const size_t N, int& min, int& max)
{
	thrust::pair<const int*, const int*> res = thrust::minmax_element(THRUST_PAR, v, v + N);
	cudaError err = cudaMemcpy(&min, res.first, sizeof(int), cudaMemcpyDeviceToHost);
	if (err != 0) return err;
	err = cudaMemcpy(&max, res.second, sizeof(int), cudaMemcpyDeviceToHost);
	return err;
}

DLLEXP cudaError intMax(const int* v, const size_t N, int& max)
{
	const int* res = thrust::max_element(THRUST_PAR, v, v + N);
	cudaError err = cudaMemcpy(&max, res, sizeof(int), cudaMemcpyDeviceToHost);
	return err;
}

DLLEXP int intLowerBound(const int* v, const size_t N, const int lower)
{
	return thrust::lower_bound(THRUST_PAR, v, v + N, lower) - v;
}

DLLEXP int intUpperBound(const int* v, const size_t N, const int upper)
{
	return thrust::upper_bound(THRUST_PAR, v, v + N, upper) - v;
}

DLLEXP int intFind(const int* v, const size_t N, const int toFind)
{
	return thrust::find(THRUST_PAR, v, v + N, toFind) - v;
}

DLLEXP void intFillRange(int* v, const size_t N, const int start, const int step)
{
	thrust::sequence(THRUST_PAR, v, v + N, start, step);
}
END_EXTERN_C
#pragma endregion


#pragma region int add scalar
struct intAddScalar_functor
{
	const int scalar;

	intAddScalar_functor(const int s) : scalar(s) {}

	__host__ __device__ int operator()(const int x) const
	{
		return x + scalar;
	}
};

EXTERN_C
DLLEXP void intAddScalar(int* a, const int scalar, const size_t N, const unsigned int stride)
{
	if (stride == 1)
	{
		thrust::transform(THRUST_PAR, a, a + N, a, intAddScalar_functor(scalar));
	}
	else
	{
		StridedRange<const int*> strideA(a, a + N * stride, stride);
		thrust::transform(THRUST_PAR, strideA.begin(), strideA.end(), strideA.begin(), intAddScalar_functor(scalar));
	}
}
END_EXTERN_C
#pragma endregion


#pragma region vector aggregate -- sum
template<typename T>
__inline__ T vectorSum(const T* a, const size_t N, const unsigned int stride)
{	// the thrust::plus<T> is enough since we have defined operator+ for complex types
	if (stride == 1)
	{
		return thrust::reduce(THRUST_PAR, a, a + N, T());
	}
	else
	{
		StridedRange<const T*> strideA(a, a + N * stride, stride);
		return thrust::reduce(THRUST_PAR, strideA.begin(), strideA.end(), T());
	}
}

EXTERN_C
DLLEXP float vecSumS(const float* a, const size_t N, const unsigned int stride)
{
	return vectorSum(a, N, stride);
}
DLLEXP double vecSumD(const double* a, const size_t N, const unsigned int stride)
{
	return vectorSum(a, N, stride);
}
DLLEXP complexFloat vecSumC(const complexFloat* a, const size_t N, const unsigned int stride)
{
	return vectorSum(a, N, stride);
}
DLLEXP complexDouble vecSumZ(const complexDouble* a, const size_t N, const unsigned int stride)
{
	return vectorSum(a, N, stride);
}
END_EXTERN_C
#pragma endregion


#pragma region vector aggregate -- product
template<typename T>
__inline__ T vectorAccumulateProduct(const T* a, const size_t N, const unsigned int stride)
{	// the thrust::multiplies<T> is enough since we have defined operator* for complex types
	if (stride == 1)
	{
		return thrust::reduce(THRUST_PAR, a, a + N, T(), thrust::multiplies<T>());
	}
	else
	{
		StridedRange<const T*> strideA(a, a + N * stride, stride);
		return thrust::reduce(THRUST_PAR, strideA.begin(), strideA.end(), T(), thrust::multiplies<T>());
	}
}

EXTERN_C
DLLEXP float vecProdS(const float* a, const size_t N, const unsigned int stride)
{
	return vectorAccumulateProduct(a, N, stride);
}
DLLEXP double vecProdD(const double* a, const size_t N, const unsigned int stride)
{
	return vectorAccumulateProduct(a, N, stride);
}
DLLEXP complexFloat vecProdC(const complexFloat* a, const size_t N, const unsigned int stride)
{
	return vectorAccumulateProduct(a, N, stride);
}
DLLEXP complexDouble vecProdZ(const complexDouble* a, const size_t N, const unsigned int stride)
{
	return vectorAccumulateProduct(a, N, stride);
}
END_EXTERN_C
#pragma endregion


#pragma region vector aggregate -- partial sum
template<typename T>
__inline__ void vectorPartialSum(const T* a, T* dst, const size_t N, const unsigned int stride, const bool inclusive)
{	// the thrust::plus<T> is enough since we have defined operator+ for complex types
	if (stride == 1)
	{
		if (inclusive)
			thrust::inclusive_scan(THRUST_PAR, a, a + N, dst);
		else
			thrust::exclusive_scan(THRUST_PAR, a, a + N, dst);
	}
	else
	{
		StridedRange<const T*> strideA(a, a + N * stride, stride);
		StridedRange<const T*> strideDst(dst, dst + N * stride, stride);
		if (inclusive)
			thrust::inclusive_scan(THRUST_PAR, strideA.begin(), strideA.end(), strideDst.begin());
		else
			thrust::exclusive_scan(THRUST_PAR, strideA.begin(), strideA.end(), strideDst.begin());
	}
}

EXTERN_C
DLLEXP void vecParSumS(const float* a, float* dst, const size_t N, const unsigned int stride, const bool inclusive)
{
	vectorPartialSum(a, dst, N, stride, inclusive);
}
DLLEXP void vecParSumD(const double* a, double* dst, const size_t N, const unsigned int stride, const bool inclusive)
{
	vectorPartialSum(a, dst, N, stride, inclusive);
}
DLLEXP void vecParSumC(const complexFloat* a, complexFloat* dst, const size_t N, const unsigned int stride, const bool inclusive)
{
	vectorPartialSum(a, dst, N, stride, inclusive);
}
DLLEXP void vecParSumZ(const complexDouble* a, complexDouble* dst, const size_t N, const unsigned int stride, const bool inclusive)
{
	vectorPartialSum(a, dst, N, stride, inclusive);
}
END_EXTERN_C
#pragma endregion


#pragma region vector aggregate -- partial product
template<typename T>
__inline__ void vectorPartialProduct(const T* a, T* dst, const size_t N, const unsigned int stride, const bool inclusive)
{	// the thrust::plus<T> is enough since we have defined operator+ for complex types
	if (stride == 1)
	{
		if (inclusive)
			thrust::inclusive_scan(THRUST_PAR, a, a + N, dst, thrust::multiplies<T>());
		else
			thrust::exclusive_scan(THRUST_PAR, a, a + N, dst, T(1), thrust::multiplies<T>());
	}
	else
	{
		StridedRange<const T*> strideA(a, a + N * stride, stride);
		StridedRange<const T*> strideDst(dst, dst + N * stride, stride);
		if (inclusive)
			thrust::inclusive_scan(THRUST_PAR, strideA.begin(), strideA.end(), strideDst.begin(), thrust::multiplies<T>());
		else
			thrust::exclusive_scan(THRUST_PAR, strideA.begin(), strideA.end(), strideDst.begin(), T(1), thrust::multiplies<T>());
	}
}

EXTERN_C
DLLEXP void vecParProdS(const float* a, float* dst, const size_t N, const unsigned int stride, const bool inclusive)
{
	vectorPartialProduct(a, dst, N, stride, inclusive);
}
DLLEXP void vecParProdD(const double* a, double* dst, const size_t N, const unsigned int stride, const bool inclusive)
{
	vectorPartialProduct(a, dst, N, stride, inclusive);
}
DLLEXP void vecParProdC(const complexFloat* a, complexFloat* dst, const size_t N, const unsigned int stride, const bool inclusive)
{
	vectorPartialProduct(a, dst, N, stride, inclusive);
}
DLLEXP void vecParProdZ(const complexDouble* a, complexDouble* dst, const size_t N, const unsigned int stride, const bool inclusive)
{
	vectorPartialProduct(a, dst, N, stride, inclusive);
}
END_EXTERN_C
#pragma endregion
