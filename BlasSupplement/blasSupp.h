#pragma once

// platform specific INLINE and export DLL
#if defined(_MSC_VER)
#define DLLEXP extern "C" __declspec(dllexport)
#elif defined(__ICC) || defined(__INTEL_COMPILER) || defined(__GNUC__) || defined(__GNUG__)
#define DLLEXP extern "C" __attribute__((visibility("default")))
#else
//  do nothing and hope for the best?
#define DLLEXP
#pragma warning Unknown dynamic link import/export semantics.
#endif


// CUDA includes

#include <cuda_runtime.h>
#include <device_launch_parameters.h>
// math and complex
#include <math.h>
#include "complex.h"

#include <thrust/for_each.h>
#include <thrust/fill.h>
#include <thrust/extrema.h>
#include <thrust/sequence.h>
#include <thrust/binary_search.h>
#include <thrust/find.h>
#include <thrust/reduce.h>
#include <thrust/copy.h>
#include <thrust/merge.h>
#include <thrust/inner_product.h>
#include <thrust/count.h>
#include <thrust/remove.h>
#include <thrust/scan.h>
#include <thrust/sort.h>
#include <thrust/equal.h>

// self-defined data type
#include "datatype.h"


// compile options
// ignore spelling: nvcc Xcompiler bigobj openmp
// nvcc -o SupplementCUDA.dll --shared DenseVector.cu --shared SparseVector.cu --shared Matrix.cu -std=c++17 -Xcompiler "-bigobj"
// nvcc -o SupplementOMP.dll -DCPU --shared DenseVector.cu --shared SparseVector.cu --shared Matrix.cu -std=c++17 -Xcompiler "-bigobj -openmp"
#undef THRUST_DEVICE_SYSTEM
#ifdef CPU
#include <thrust/system/omp/execution_policy.h>
#define THRUST_PAR thrust::omp::par
#define ERROR_RETURN void
#define THRUST_DEVICE_SYSTEM THRUST_DEVICE_SYSTEM_OMP
#else
#define THRUST_PAR thrust::cuda::par
#define ERROR_RETURN cudaError
#define THRUST_DEVICE_SYSTEM THRUST_DEVICE_SYSTEM_CUDA
#endif // CPU


#pragma region stride range
// stride range iterator class from NVIDIA/thrust/examples/strided_range.cu
template <typename Iterator>
class StridedRange
{
public:

	typedef typename thrust::iterator_difference<Iterator>::type difference_type;
	typedef typename thrust::iterator_value<Iterator>::type value_type;

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
inline static StridedRange<Iterator> make_strided_range(Iterator it, size_t N, const typename StridedRange<Iterator>::difference_type stride)
{
	return StridedRange<Iterator>(it, it + N * stride, stride);
}
#pragma endregion


#pragma region plus functor
// thrust::plus<T> have bug? Use this instead.
template <typename T>
struct plus_functor
{
	__host__ __device__ const T operator()(const T& x, const T& y) const
	{
		return x + y;
	}
};
#pragma endregion
