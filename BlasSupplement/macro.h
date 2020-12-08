#pragma once

// platform specific INLINE and export DLL
#if defined(_MSC_VER)
#define INLINE __forceinline
#define DLLEXP __declspec(dllexport)
#elif defined(__ICC) || defined(__INTEL_COMPILER) || defined(__GNUC__) || defined(__GNUG__)
#define INLINE __attribute__((always_inline)) inline
#define DLLEXP __attribute__((visibility("default")))
#else
#define INLINE
//  do nothing and hope for the best?
#define DLLEXP
#pragma warning Unknown inline semantics.
#pragma warning Unknown dynamic link import/export semantics.
#endif

// extern "C"
#define EXTERN_C extern "C"


// CUDA includes
#include "cuda_runtime.h"
#include "device_launch_parameters.h"
// math and complex
#include <math.h>
#include <complex>
#include "cuComplex.h"

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

// self-defined data type
#include "datatype.h"


// compile options
// ignore spelling: nvcc
// nvcc -o kernels.dll [-DCPU] --shared DenseVector.cu --shared SparseVector.cu --shared Matrix.cu --shared host_util.cpp
#ifdef CPU
#include <thrust/system/omp/execution_policy.h>
#define THRUST_PAR thrust::omp::par
#define ERROR_RETURN void
#else
#define THRUST_PAR thrust::cuda::par
#define ERROR_RETURN cudaError
#endif // CPU


// complex type alias
using complexSingle = std::complex<float>;
// complex type alias
using complexDouble = std::complex<double>;
#ifdef HAS_LDBL
// complex type alias
using complexLongDouble = std::complex<long double>;
#endif


// self defined methods for complex
namespace std
{
	template <typename T>
	__host__ __device__ static __inline__ T conjAllCase(const T a)
	{
		if constexpr (std::is_scalar_v<T>)
			return a;
		else
			return std::conj(a);
	}


	template <typename T>
	__host__ __device__ static __inline__ std::complex<T> fma(const std::complex<T> x, const std::complex<T> y, const std::complex<T> d)
	{
		T real_res;
		T imag_res;

		real_res = std::fma(x.real(), y.real(), d.real());
		imag_res = std::fma(x.imag(), y.imag(), d.imag());

		real_res = std::fma(-x.imag(), y.imag(), real_res);
		imag_res = std::fma(x.imag(), y.real(), imag_res);

		return std::complex<T>(real_res, imag_res);
	}

	__host__ __device__ static __inline__ constexpr char abs(const char a) { return a < 0I8 ? -a : a; }
	__host__ __device__ static __inline__ constexpr short abs(const short a) { return a < 0I16 ? -a : a; }
	__host__ __device__ static __inline__ constexpr unsigned char abs(const unsigned char a) { return a; }
	__host__ __device__ static __inline__ constexpr unsigned short abs(const unsigned short a) { return a; }
	__host__ __device__ static __inline__ constexpr unsigned int abs(const unsigned int a) { return a; }
	__host__ __device__ static __inline__ constexpr unsigned long abs(const unsigned long a) { return a; }
	__host__ __device__ static __inline__ constexpr unsigned long long abs(const unsigned long long a) { return a; }
}
