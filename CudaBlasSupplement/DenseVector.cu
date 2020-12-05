// nvcc -o kernels.dll -lcublas --shared DenseVector.cu --shared Matrix.cu --shared SparseVector.cu

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
class strided_range
{
public:

	typedef typename thrust::iterator_difference<Iterator>::type difference_type;

	struct stride_functor : public thrust::unary_function<difference_type, difference_type>
	{
		difference_type stride;

		stride_functor(difference_type stride)
			: stride(stride) {}

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
	strided_range(Iterator first, Iterator last, difference_type stride)
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
struct multiplyTwo_functor
{
	__host__ __device__ T operator()(const T x, const T y) const
	{
		return x * y;
	}
};
template<typename T>
struct divideTwo_functor
{
	__host__ __device__ T operator()(const T x, const T y) const
	{
		return x / y;
	}
};

template<typename T>
__inline__ void vectorsElementWiseMultiplyDivide(T* a, const T* b, const size_t N, const unsigned int stride, bool multiply)
{
	if (stride == 1)
	{
		if (multiply)
			thrust::transform(thrust::cuda::par, a, a + N, b, a, multiplyTwo_functor<T>());
		else
			thrust::transform(thrust::cuda::par, a, a + N, b, a, divideTwo_functor<T>());
	}
	else
	{
		strided_range<const T*> strideA(a, a + N * stride, stride);
		strided_range<const T*> strideB(b, b + N * stride, stride);
		if (multiply)
			thrust::transform(thrust::cuda::par, strideA.begin(), strideA.end(), strideB.begin(), strideA.begin(), multiplyTwo_functor<T>());
		else
			thrust::transform(thrust::cuda::par, strideA.begin(), strideA.end(), strideB.begin(), strideA.begin(), divideTwo_functor<T>());
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
DLLEXP void vecEWMulDivC(cuFloatComplex* a, const cuFloatComplex* b, const size_t N, const unsigned int stride, bool multiply)
{
	vectorsElementWiseMultiplyDivide(a, b, N, stride, multiply);
}
DLLEXP void vecEWMulDivZ(cuDoubleComplex* a, const cuDoubleComplex* b, const size_t N, const unsigned int stride, bool multiply)
{
	vectorsElementWiseMultiplyDivide(a, b, N, stride, multiply);
}
END_EXTERN_C
#pragma endregion


#pragma region element-wise power
template<typename T, typename P>
struct floatPower_functor
{
	P p;

	floatPower_functor(P pow) : p(pow) {}

	__host__ __device__ T operator()(const T x) const
	{
		return std::pow(x, p);
	}
};

template<typename T, typename P>
void vectorElementWisePower(T* a, const P p, const size_t N, const unsigned int stride)
{
	if (stride == 1)
	{
		thrust::transform(thrust::cuda::par, a, a + N, a, floatPower_functor<T, P>(p));
	}
	else
	{
		strided_range<const T*> strideA(a, a + N * stride, stride);
		thrust::transform(thrust::cuda::par, strideA.begin(), strideA.end(), strideA.begin(), floatPower_functor<T, P>(p));
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
DLLEXP void vecEWPowC(cuFloatComplex* a, const float p, const size_t N, const unsigned int stride)
{
	vectorElementWisePower(a, p, N, stride);
}
DLLEXP void vecEWPowZ(cuDoubleComplex* a, const double p, const size_t N, const unsigned int stride)
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
		thrust::fill_n(thrust::cuda::par, a, N, val);
	}
	else
	{
		strided_range<const T*> strideA(a, a + N * stride, stride);
		thrust::fill(thrust::cuda::par, strideA.begin(), strideA.end(), val);
	}
}

EXTERN_C
DLLEXP void fillOneS(float* a, const size_t N, const unsigned int stride)
{
	vectorFillWith(a, 1.0f, N, stride);
}
DLLEXP void fillOneD(double* a, const size_t N, const unsigned int stride)
{
	vectorFillWith(a, 1.0, N, stride);
}
DLLEXP void fillOneC(cuFloatComplex* a, const size_t N, const unsigned int stride)
{
	vectorFillWith(a, make_cuFloatComplex(1.0f, 0.0f), N, stride);
}
DLLEXP void fillOneZ(cuDoubleComplex* a, const size_t N, const unsigned int stride)
{
	vectorFillWith(a, make_cuDoubleComplex(1.0, 0.0), N, stride);
}
END_EXTERN_C
#pragma endregion


#pragma region array conjugate
template<typename T>
struct floatConjugate_functor
{
	__host__ __device__ T operator()(T x) const
	{
		x.y = -x.y;
		return x;
	}
};

template<typename T>
void vecConjugate(T* a, const size_t N, const unsigned int stride)
{
	if (stride == 1)
	{
		thrust::transform(thrust::cuda::par, a, a + N, a, floatConjugate_functor<T>());
	}
	else
	{
		strided_range<const T*> strideA(a, a + N * stride, stride);
		thrust::transform(thrust::cuda::par, strideA.begin(), strideA.end(), strideA.begin(), floatConjugate_functor<T>());
	}
}

EXTERN_C
DLLEXP void vecConjC(cuFloatComplex* a, const size_t N, const unsigned int stride)
{
	vecConjugate(a, N, stride);
}
DLLEXP void vecConjZ(cuDoubleComplex* a, const size_t N, const unsigned int stride)
{
	vecConjugate(a, N, stride);
}
END_EXTERN_C
#pragma endregion


#pragma region float to double up-cast and float from double down-cast
struct floatToDouble_functor
{
	__host__ __device__ double operator()(const float x) const
	{
		return x;
	}
};
struct doubleToFloat_functor
{
	__host__ __device__ float operator()(const double x) const
	{
		return (float)x;
	}
};


EXTERN_C
DLLEXP void vecSingleToDouble(double* dest, const float* src, const size_t N, const unsigned int stride)
{
	if (stride == 1)
	{
		thrust::transform(thrust::cuda::par, src, src + N, dest, floatToDouble_functor());
	}
	else
	{
		strided_range<const float*> strideSrc(src, src + N * stride, stride);
		strided_range<const double*> strideDst(dest, dest + N * stride, stride);
		thrust::transform(thrust::cuda::par, strideSrc.begin(), strideSrc.end(), strideDst.begin(), floatToDouble_functor());
	}
}

DLLEXP void vecDoubleToSingle(float* dest, const double* src, const size_t N, const unsigned int stride)
{
	if (stride == 1)
	{
		thrust::transform(thrust::cuda::par, src, src + N, dest, doubleToFloat_functor());
	}
	else
	{
		strided_range<const double*> strideSrc(src, src + N * stride, stride);
		strided_range<const float*> strideDst(dest, dest + N * stride, stride);
		thrust::transform(thrust::cuda::par, strideSrc.begin(), strideSrc.end(), strideDst.begin(), doubleToFloat_functor());
	}
}
END_EXTERN_C
#pragma endregion


#pragma region dense vector set values with small absolutes to zero
template<typename T, typename Bound>
struct trimAbs_functor
{
	Bound b;

	trimAbs_functor(Bound bound) : b(bound) {}

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
		thrust::transform(thrust::cuda::par, a, a + N, a, trimAbs_functor<T, Bound>(threshold));
	}
	else
	{
		strided_range<const T*> strideA(a, a + N * stride, stride);
		thrust::transform(thrust::cuda::par, strideA.begin(), strideA.end(), strideA.begin(), trimAbs_functor<T, Bound>(threshold));
	}
}

EXTERN_C
DLLEXP void vecClipS(float* a, const float threshold, const size_t N, const unsigned int stride)
{
	vectorClip(a, threshold, N, stride);
}
DLLEXP void arrTrimD(double* a, const float threshold, const size_t N, const unsigned int stride)
{
	vectorClip(a, (double)threshold, N, stride);
}
DLLEXP void arrTrimC(cuFloatComplex* a, const float threshold, const size_t N, const unsigned int stride)
{
	vectorClip(a, threshold, N, stride);
}
DLLEXP void arrTrimZ(cuDoubleComplex* a, const float threshold, const size_t N, const unsigned int stride)
{
	vectorClip(a, (double)threshold, N, stride);
}
END_EXTERN_C
#pragma endregion


#pragma region int operations
EXTERN_C
DLLEXP cudaError intMinMax(const int* v, const size_t N, int& min, int& max)
{
	thrust::pair<const int*, const int*> res = thrust::minmax_element(thrust::cuda::par, v, v + N);
	cudaError err = cudaMemcpy(&min, res.first, sizeof(int), cudaMemcpyDeviceToHost);
	if (err != 0) return err;
	err = cudaMemcpy(&max, res.second, sizeof(int), cudaMemcpyDeviceToHost);
	return err;
}

DLLEXP cudaError intMax(const int* v, const size_t N, int& max)
{
	const int* res = thrust::max_element(thrust::cuda::par, v, v + N);
	cudaError err = cudaMemcpy(&max, res, sizeof(int), cudaMemcpyDeviceToHost);
	return err;
}

DLLEXP int intLowerBound(const int* v, const size_t N, const int lower)
{
	return thrust::lower_bound(thrust::cuda::par, v, v + N, lower) - v;
}

DLLEXP int intUpperBound(const int* v, const size_t N, const int upper)
{
	return thrust::upper_bound(thrust::cuda::par, v, v + N, upper) - v;
}

DLLEXP int intFind(const int* v, const size_t N, const int toFind)
{
	return thrust::find(thrust::cuda::par, v, v + N, toFind) - v;
}

DLLEXP void intFillRange(int* v, const size_t N, const int start, const int step)
{
	thrust::sequence(thrust::cuda::par, v, v + N, start, step);
}
END_EXTERN_C
#pragma endregion


#pragma region int add scalar
struct intAddScalar_functor
{
	int scalar;

	intAddScalar_functor(int s) : scalar(s) {}

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
		thrust::transform(thrust::cuda::par, a, a + N, a, intAddScalar_functor(scalar));
	}
	else
	{
		strided_range<const int*> strideA(a, a + N * stride, stride);
		thrust::transform(thrust::cuda::par, strideA.begin(), strideA.end(), strideA.begin(), intAddScalar_functor(scalar));
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
		return thrust::reduce(thrust::cuda::par, a, a + N, T());
	}
	else
	{
		strided_range<const T*> strideA(a, a + N * stride, stride);
		return thrust::reduce(thrust::cuda::par, strideA.begin(), strideA.end(), T());
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
DLLEXP cuFloatComplex vecSumC(const cuFloatComplex* a, const size_t N, const unsigned int stride)
{
	return vectorSum(a, N, stride);
}
DLLEXP cuDoubleComplex vecSumZ(const cuDoubleComplex* a, const size_t N, const unsigned int stride)
{
	return vectorSum(a, N, stride);
}
END_EXTERN_C
#pragma endregion