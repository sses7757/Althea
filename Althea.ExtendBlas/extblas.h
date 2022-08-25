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


#include <cuda_runtime.h>
#include <device_launch_parameters.h>

#define PREFIX __host__ __device__

#include "complex.h"
#include "datatype.h"
#include "ranges.h"

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
#include <thrust/iterator/discard_iterator.h>



// compile options
// ignore spelling: nvcc Xcompiler bigobj openmp
// nvcc -o extblasCUDA.dll --shared DenseVector.cu --shared DenseMatrix.cu --shared Sparse.cu -std=c++17 -Xcompiler "-bigobj" -extended-lambda
// nvcc -o extblasOMP.dll -DCPU --shared DenseVector.cu --shared SparseVector.cu --shared Matrix.cu -std=c++17 -Xcompiler "-bigobj -openmp"
#undef THRUST_DEVICE_SYSTEM
#ifdef CPU
#include <thrust/system/tbb/execution_policy.h>
#define THRUST_PAR thrust::tbb::par
#define ERROR_RETURN int
#define THRUST_DEVICE_SYSTEM THRUST_DEVICE_SYSTEM_OMP
#ifdef MKL_ILP64
#define MKL_INT long long
#else
#define MKL_INT int
#endif // MKL_ILP64
#else
#define THRUST_PAR thrust::cuda::par
#define ERROR_RETURN cudaError
#define THRUST_DEVICE_SYSTEM THRUST_DEVICE_SYSTEM_CUDA
#endif // CPU


#pragma region functor
// thrust::plus<T> have bug? Use this instead.
template <typename T>
struct plus_functor
{
	PREFIX const T operator()(const T& x, const T& y) const
	{
		return x + y;
	}
};
template <typename T>
struct equals_functor
{
	PREFIX bool operator()(const T& x, const T& y) const
	{
		return x == y;
	}
};

template <typename T>
struct larger_functor
{
	const T v;

	PREFIX larger_functor(const T& val) : v(val){}

	PREFIX bool operator()(const T& x) const
	{
		return x > v;
	}
};
template <typename T>
struct smaller_functor
{
	const T v;

	PREFIX smaller_functor(const T& val) : v(val) {}

	PREFIX bool operator()(const T& x) const
	{
		return x < v;
	}
};

template <typename T, typename U>
struct abslarger_functor
{
	const U v;

	PREFIX abslarger_functor(const U& val) : v(val) {}

	PREFIX const T operator()(const T& x) const
	{
		return std::abs(x) > v ? (T)std::abs(x) : (T)v;
	}
};
template <typename T, typename U>
struct abssmaller_functor
{
	const U v;

	PREFIX abssmaller_functor(const U& val) : v(val) {}

	PREFIX const T operator()(const T& x) const
	{
		return std::abs(x) < v ? (T)std::abs(x) : (T)v;
	}
};
template <typename T, typename U>
struct truncate_functor
{
	const U v;

	PREFIX truncate_functor(const U& val) : v(val) {}

	PREFIX const T operator()(const T& x) const
	{
		return std::abs(x) <= v ? T{} : x;
	}
};

template <typename T, typename U>
struct truncateGT_functor
{
	const U v;

	PREFIX truncateGT_functor(const U& val) : v(val) {}

	PREFIX bool operator()(const T& x) const
	{
		return std::abs(x) > v;
	}
};

template <typename T>
struct largerOne_functor
{
	PREFIX const T& operator()(const T& x, const T& y) const
	{
		return x > y ? x : y;
	}
};
template <typename T>
struct smallerOne_functor
{
	PREFIX const T& operator()(const T& x, const T& y) const
	{
		return x < y ? x : y;
	}
};

template <typename T>
struct largerThan_functor
{
	PREFIX bool operator()(const T& x, const T& y) const
	{
		return x > y;
	}
};
template <typename T>
struct smallerThan_functor
{
	PREFIX bool operator()(const T& x, const T& y) const
	{
		return x < y;
	}
};

template <typename T>
struct norm_functor
{
	PREFIX const typename T::value_type operator()(const T& x, const T& y) const
	{
		return x.absSquare();
	}
};
#pragma endregion
