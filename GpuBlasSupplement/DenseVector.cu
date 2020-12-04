// nvcc -o kernels.dll -lcublas --shared DenseVector.cu --shared Matrix.cu --shared SparseVector.cu

// CUDA includes
#include "cuda_runtime.h"
#include "device_launch_parameters.h"
#include "cublas.h"
#include "math.h"
#include "cuComplex.h"
#include <complex>

//#include <thrust/system/omp/execution_policy.h> thrust::omp::par
#include <thrust/fill.h>
#include <thrust/reduce.h>
#include <thrust/extrema.h>
#include <thrust/binary_search.h>
#include <thrust/sequence.h>
#include <thrust/find.h>
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


#pragma region element-wise multiply and divide
template<typename T>
struct multiplyTwo
{
	__host__ __device__ T operator()(const T x, const T y) const
	{
		return x * y;
	}
};
template<typename T>
struct divideTwo
{
	__host__ __device__ T operator()(const T x, const T y) const
	{
		return x / y;
	}
};

template<typename T>
DLLEXP void inline vectorsElementWiseMultiplyDivide(T* a, const T* b, const size_t N, bool multiply)
{
	if (multiply)
		thrust::transform(thrust::cuda::par, a, a + N, b, a, multiplyTwo<T>());
	else
		thrust::transform(thrust::cuda::par, a, a + N, b, a, divideTwo<T>());
}

EXTERN_C
DLLEXP void vecEWMulS(float* a, const float* b, const size_t N)
{
	vectorsElementWiseMultiplyDivide(a, b, N, true);
}
DLLEXP void vecEWMulD(double* a, const double* b, const size_t N)
{
	vectorsElementWiseMultiplyDivide(a, b, N, true);
}
DLLEXP void vecEWMulC(cuFloatComplex* a, const cuFloatComplex* b, const size_t N)
{
	vectorsElementWiseMultiplyDivide(a, b, N, true);
}
DLLEXP void vecEWMulZ(cuDoubleComplex* a, const cuDoubleComplex* b, const size_t N)
{
	vectorsElementWiseMultiplyDivide(a, b, N, true);
}

DLLEXP void vecEWDivS(float* a, const float* b, const size_t N)
{
	vectorsElementWiseMultiplyDivide(a, b, N, false);
}
DLLEXP void vecEWDivD(double* a, const double* b, const size_t N)
{
	vectorsElementWiseMultiplyDivide(a, b, N, false);
}
DLLEXP void vecEWDivC(cuFloatComplex* a, const cuFloatComplex* b, const size_t N)
{
	vectorsElementWiseMultiplyDivide(a, b, N, false);
}
DLLEXP void vecEWDivZ(cuDoubleComplex* a, const cuDoubleComplex* b, const size_t N)
{
	vectorsElementWiseMultiplyDivide(a, b, N, false);
}
END_EXTERN_C
#pragma endregion


#pragma region element-wise power
namespace std
{
	__host__ __device__ cuDoubleComplex pow(cuDoubleComplex a, const double p)
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
	__host__ __device__ cuFloatComplex pow(cuFloatComplex a, const float p)
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
}

template<typename T, typename P>
struct floatPower
{
	P p;

	floatPower(P pow) : p(pow) {}

	__host__ __device__ T operator()(const T x) const
	{
		return std::pow(x, p);
	}
};


template<typename T, typename P>
void vectorElementWisePower(T* a, const P p, const size_t N)
{
	thrust::transform(thrust::cuda::par, a, a + N, a, floatPower<T, P>(p));
}

EXTERN_C
DLLEXP void vecEWPowS(float* a, const float p, const size_t N)
{
	vectorElementWisePower(a, p, N);
}
DLLEXP void vecEWPowD(double* a, const double p, const size_t N)
{
	vectorElementWisePower(a, p, N);
}
DLLEXP void vecEWPowC(cuFloatComplex* a, const float p, const size_t N)
{
	vectorElementWisePower(a, p, N);
}
DLLEXP void vecEWPowZ(cuDoubleComplex* a, const double p, const size_t N)
{
	vectorElementWisePower(a, p, N);
}
END_EXTERN_C
#pragma endregion


#pragma region fill array with ones
EXTERN_C
DLLEXP void fillOneS(float* a, const size_t N)
{
	thrust::fill(thrust::cuda::par, a, a + N, 1.0f);
}
DLLEXP void fillOneD(double* a, const size_t N)
{
	thrust::fill(thrust::cuda::par, a, a + N, 1.0);
}
DLLEXP void fillOneC(cuFloatComplex* a, const size_t N)
{
	thrust::fill(thrust::cuda::par, a, a + N, make_cuFloatComplex(1.0f, 0.0f));
}
DLLEXP void fillOneZ(cuDoubleComplex* a, const size_t N)
{
	thrust::fill(thrust::cuda::par, a, a + N, make_cuDoubleComplex(1.0, 0.0));
}
END_EXTERN_C
#pragma endregion


#pragma region array conjugate
template<typename T>
struct floatConjugate
{
	__host__ __device__ T operator()(T x) const
	{
		x.y = -x.y;
		return x;
	}
};

EXTERN_C
DLLEXP void vecConjC(cuFloatComplex* a, const size_t N)
{
	thrust::transform(thrust::cuda::par, a, a + N, a, floatConjugate<cuFloatComplex>());
}
DLLEXP void vecConjZ(cuDoubleComplex* a, const size_t N)
{
	thrust::transform(thrust::cuda::par, a, a + N, a, floatConjugate<cuDoubleComplex>());
}
END_EXTERN_C
#pragma endregion


#pragma region float to double up-cast
struct floatToDouble
{
	__host__ __device__ double operator()(const float x) const
	{
		return x;
	}
};

EXTERN_C
DLLEXP void vecSingleToDouble(double* dest, const float* src, const size_t N)
{
	thrust::transform(thrust::cuda::par, src, src + N, dest, floatToDouble());
}
END_EXTERN_C
#pragma endregion


#pragma region dense vector trim values with small absolutes to zero
namespace std
{
	__host__ __device__ float abs(cuFloatComplex a)
	{
		return cuCabsf(a);
	}
	__host__ __device__ double abs(cuDoubleComplex a)
	{
		return cuCabs(a);
	}
}


template<typename T, typename Kernel>
cudaError vecTrim(T* arr, const float threshold, const size_t N, Kernel ker)
{
	int blockSize, minGridSize, gridSize;
	cudaError err = cudaOccupancyMaxPotentialBlockSize(&minGridSize, &blockSize, ker, 0, N);
	if (err != 0) return err;
	// Round up according to array size
	gridSize = (N + blockSize - 1) / blockSize;
	ker<<<gridSize, blockSize>>>(arr, threshold, N);
	err = cudaDeviceSynchronize();
	return err;
}

EXTERN_C
DLLEXP cudaError arrTrimS(float* arr, const float thre, const size_t N)
{
	return vecTrim(arr, thre, N, KerTrimS);
}
DLLEXP cudaError arrTrimD(double* arr, const float thre, const size_t N)
{
	return vecTrim(arr, thre, N, KerTrimD);
}
DLLEXP cudaError arrTrimC(cuFloatComplex* arr, const float thre, const size_t N)
{
	return vecTrim(arr, thre, N, KerTrimC);
}
DLLEXP cudaError arrTrimZ(cuDoubleComplex* arr, const float thre, const size_t N)
{
	return vecTrim(arr, thre, N, KerTrimZ);
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


DLLEXP cudaError longMinMax(const long long* v, const size_t N, long long& min, long long& max)
{
	thrust::pair<const long long*, const long long*> res = thrust::minmax_element(thrust::cuda::par, v, v + N);
	cudaError err = cudaMemcpy(&min, res.first, sizeof(size_t), cudaMemcpyDeviceToHost);
	if (err != 0) return err;
	err = cudaMemcpy(&max, res.second, sizeof(size_t), cudaMemcpyDeviceToHost);
	return err;
}
DLLEXP void intFillRange(int* v, const size_t N, const int start, const int step)
{
	thrust::sequence(thrust::cuda::par, v, v + N, start, step);
}
END_EXTERN_C
#pragma endregion


#pragma region int add scalar
__global__ void KerIntAddScalar(int* arr, const int scalar, const size_t N)
{
	unsigned int x = threadIdx.x + blockIdx.x * blockDim.x;
	if (x < N)
		arr[x] = arr[x] + scalar;
}

EXTERN_C
DLLEXP cudaError intAddScalar(int* arr, const int scalar, const size_t N)
{
	int blockSize, minGridSize, gridSize;
	cudaError err = cudaOccupancyMaxPotentialBlockSize(&minGridSize, &blockSize, KerIntAddScalar, 0, N);
	if (err != 0) return err;
	// Round up according to array size
	gridSize = (N + blockSize - 1) / blockSize;
	KerIntAddScalar << <gridSize, blockSize >> > (arr, scalar, N);
	err = cudaDeviceSynchronize();
	return err;
}
END_EXTERN_C
#pragma endregion