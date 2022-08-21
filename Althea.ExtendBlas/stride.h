#pragma once

#include <thrust/iterator/counting_iterator.h>
#include <thrust/iterator/transform_iterator.h>
#include <thrust/iterator/permutation_iterator.h>
#include <thrust/functional.h>


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
		stride_functor(const difference_type stride_) : stride(stride_) {}

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