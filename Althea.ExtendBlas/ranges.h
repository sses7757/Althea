#pragma once

#include <thrust/iterator/counting_iterator.h>
#include <thrust/iterator/transform_iterator.h>
#include <thrust/iterator/permutation_iterator.h>
#include <thrust/functional.h>

namespace extblas
{
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

			PREFIX difference_type operator()(const difference_type& i) const
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
	class LeadDimRange
	{
	public:

		typedef typename thrust::iterator_difference<Iterator>::type difference_type;
		typedef typename thrust::iterator_value<Iterator>::type value_type;

		struct leadDim_functor : public thrust::unary_function<difference_type, difference_type>
		{
			const difference_type m, ld;
			leadDim_functor(const difference_type m_, const difference_type ld_) : m(m_), ld(ld_) {}

			PREFIX difference_type operator()(const difference_type& i) const
			{
				return i % m + i / m * ld;
			}
		};

		typedef typename thrust::counting_iterator<difference_type>                    CountingIterator;
		typedef typename thrust::transform_iterator<leadDim_functor, CountingIterator> TransformIterator;
		typedef typename thrust::permutation_iterator<Iterator, TransformIterator>     PermutationIterator;

		// type of the strided_range iterator
		typedef PermutationIterator iterator;

		// construct strided_range for the range [first,last)
		LeadDimRange(Iterator first, difference_type m_, difference_type n_, difference_type ld_)
			: first(first), m(m_), n(n_), ld(ld_) {}

		iterator begin(void) const
		{
			return PermutationIterator(first, TransformIterator(CountingIterator(0), leadDim_functor(m, ld)));
		}

		iterator end(void) const
		{
			return begin() + m * n;
		}

	protected:
		Iterator first;
		difference_type m, n, ld;
	};
}


template <typename Iterator>
inline static extblas::StridedRange<Iterator> make_strided_range(Iterator it, size_t N, size_t stride)
{
	return extblas::StridedRange<Iterator>(it, it + N * stride, stride);
}

template <typename Iterator>
inline static extblas::LeadDimRange<Iterator> make_leadDim_range(Iterator it, size_t m, size_t n, size_t ld)
{
	return extblas::LeadDimRange<Iterator>(it, m, n, ld);
}