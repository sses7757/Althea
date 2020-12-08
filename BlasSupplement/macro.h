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
	__host__ __device__ static inline T conjAllCase(const T a)
	{
		if constexpr (std::is_scalar_v<T>)
			return a;
		else
			return std::conj(a);
	}


	template <typename T>
	__host__ __device__ static inline std::complex<T> fma(const std::complex<T> x, const std::complex<T> y, const std::complex<T> d)
	{
		T real_res;
		T imag_res;

		real_res = std::fma(x.real(), y.real(), d.real());
		imag_res = std::fma(x.imag(), y.imag(), d.imag());

		real_res = std::fma(-x.imag(), y.imag(), real_res);
		imag_res = std::fma(x.imag(), y.real(), imag_res);

		return std::complex<T>(real_res, imag_res);
	}

	__host__ __device__ static inline constexpr char fma(const char x, const char y, const char d) { return x * y + d; }
	__host__ __device__ static inline constexpr short fma(const short x, const short y, const short d) { return x * y + d; }
	__host__ __device__ static inline constexpr int fma(const int x, const int y, const int d) { return x * y + d; }
	__host__ __device__ static inline constexpr long fma(const long x, const long y, const long d) { return x * y + d; }
	__host__ __device__ static inline constexpr long long fma(const long long x, const long long y, const long long d) { return x * y + d; }
	__host__ __device__ static inline constexpr unsigned char fma(const unsigned char x, const unsigned char y, const unsigned char d) { return x * y + d; }
	__host__ __device__ static inline constexpr unsigned short fma(const unsigned short x, const unsigned short y, const unsigned short d) { return x * y + d; }
	__host__ __device__ static inline constexpr unsigned int fma(const unsigned int x, const unsigned int y, const unsigned int d) { return x * y + d; }
	__host__ __device__ static inline constexpr unsigned long fma(const unsigned long x, const unsigned long y, const unsigned long d) { return x * y + d; }
	__host__ __device__ static inline constexpr unsigned long long fma(const unsigned long long x, const unsigned long long y, const unsigned long long d) { return x * y + d; }

	__host__ __device__ static inline constexpr char abs(const char a) { return a < 0I8 ? -a : a; }
	__host__ __device__ static inline constexpr short abs(const short a) { return a < 0I16 ? -a : a; }
	__host__ __device__ static inline constexpr unsigned char abs(const unsigned char a) { return a; }
	__host__ __device__ static inline constexpr unsigned short abs(const unsigned short a) { return a; }
	__host__ __device__ static inline constexpr unsigned int abs(const unsigned int a) { return a; }
	__host__ __device__ static inline constexpr unsigned long abs(const unsigned long a) { return a; }
	__host__ __device__ static inline constexpr unsigned long long abs(const unsigned long long a) { return a; }
}
