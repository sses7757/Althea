#pragma once

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


// CUDA includes
#include "cuda_runtime.h"
#include "device_launch_parameters.h"
#include "math.h"
#include "cuComplex.h" // for complex math

//#include <thrust/system/omp/execution_policy.h>
#include <thrust/fill.h>
#include <thrust/extrema.h>
#include <thrust/sequence.h>
#include <thrust/binary_search.h>
#include <thrust/find.h>

// self-defined data type
#include "datatype.h"

// compile options
// ignore spelling: nvcc
// nvcc -o kernels.dll [-DCPU] --shared DenseVector.cu --shared SparseVector.cu --shared Matrix.cu
#ifdef CPU
#include <thrust/system/omp/execution_policy.h>
#define THRUST_PAR thrust::omp::par
#define ERROR_RETURN void
#else
#define THRUST_PAR thrust::cuda::par
#define ERROR_RETURN cudaError
#endif // CPU



// self defined operators for complex
__host__ __device__ static __inline__ cuFloatComplex operator+(cuFloatComplex x, cuFloatComplex y)
{
	return cuCaddf(x, y);
}

__host__ __device__ static __inline__ cuDoubleComplex operator+(cuDoubleComplex x, cuDoubleComplex y)
{
	return cuCadd(x, y);
}

// direct multiply
__host__ __device__ static __inline__ cuFloatComplex operator*(cuFloatComplex x, cuFloatComplex y)
{
	return cuCmulf(x, y);
}

// direct multiply
__host__ __device__ static __inline__ cuDoubleComplex operator*(cuDoubleComplex x, cuDoubleComplex y)
{
	return cuCmul(x, y);
}

// self defined methods for complex
namespace std
{
	__host__ __device__ static __inline__ cuDoubleComplex pow(cuDoubleComplex a, const double p)
	{
		if (p == 1.0)
			return a;
		if (p == 2.0)
			return cuCmul(a, a);
		if (cuCimag(a) == 0.0)
			return make_cuDoubleComplex(std::pow(cuCreal(a), p), 0.0);

		double rho = cuCabs(a);
		double phi = atan2(cuCimag(a), cuCreal(a));
		return make_cuDoubleComplex(pow(rho, p) * pow(cos(phi), p), pow(rho, p) * pow(sin(phi), p));
	}

	__host__ __device__ static __inline__ cuFloatComplex pow(cuFloatComplex a, const float p)
	{
		if (p == 1.0f)
			return a;
		if (p == 2.0f)
			return cuCmulf(a, a);
		if (cuCimagf(a) == 0.0f)
			return make_cuFloatComplex(std::pow(cuCrealf(a), p), 0.0f);

		float rho = cuCabsf(a);
		float phi = atan2f(cuCimagf(a), cuCrealf(a));
		return make_cuFloatComplex(powf(rho, p) * powf(cosf(phi), p), powf(rho, p) * powf(sin(phi), p));
	}

	__host__ __device__ static __inline__ float abs(cuFloatComplex a)
	{
		return cuCabsf(a);
	}

	__host__ __device__ static __inline__ double abs(cuDoubleComplex a)
	{
		return cuCabs(a);
	}

	__host__ __device__ static __inline__ cuFloatComplex fma(cuFloatComplex x, cuFloatComplex y, cuFloatComplex z)
	{
		return cuCfmaf(x, y, z);
	}

	__host__ __device__ static __inline__ cuDoubleComplex fma(cuDoubleComplex x, cuDoubleComplex y, cuDoubleComplex z)
	{
		return cuCfma(x, y, z);
	}
}


#define EXTERN_C extern "C" {
#define END_EXTERN_C }