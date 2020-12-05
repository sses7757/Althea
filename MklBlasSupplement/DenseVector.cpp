// nvcc -o kernels_cpu.dll --shared DenseVector.cpp --shared Matrix.cpp --shared util.cpp

#include <algorithm>
#include <complex>
#include <functional>
#include <thread>
#include <numeric>
#include <valarray>
#include "macro.h"



#pragma region fill
CUSTOM_EXTERN_C
DLLEXP void fillOneS(float* a, const size_t N) {
	std::fill(a, a + N, 1.0f);
}
DLLEXP void fillOneD(double* a, const size_t N) {
	std::fill(a, a + N, 1.0);
}
DLLEXP void fillOneC(std::complex<float>* a, const size_t N) {
	std::fill(a, a + N, std::complex<float>(1.0f, 0.0f));
}
DLLEXP void fillOneZ(std::complex<double>* a, const size_t N) {
	std::fill(a, a + N, std::complex<double>(1.0, 0.0));
}
END_CUSTOM_EXTERN_C
#pragma endregion


#pragma region point-wise divide
template<typename T> struct divides
{
	T operator() (const T& x, const T& y) const { return x / y; }
};

CUSTOM_EXTERN_C
DLLEXP void ewDivS(float* a, const float* b, const size_t N) {
	std::transform(a, a + N, b, a, divides<float>());
}
DLLEXP void ewDivD(double* a, const double* b, const size_t N) {
	std::transform(a, a + N, b, a, divides<double>());
}
DLLEXP void ewDivC(std::complex<float>* a, const std::complex<float>* b, const size_t N) {
	std::transform(a, a + N, b, a, divides<std::complex<float>>());
}
DLLEXP void ewDivZ(std::complex<double>* a, const std::complex<double>* b, const size_t N) {
	std::transform(a, a + N, b, a, divides<std::complex<double>>());
}
END_CUSTOM_EXTERN_C
#pragma endregion


#pragma region point-wise multiply
template<typename T> struct multiplies
{
	T operator() (const T& x, const T& y) const { return x * y; }
};

CUSTOM_EXTERN_C
DLLEXP void ewMulS(float* a, const float* b, const size_t N) {
	std::transform(a, a + N, b, a, multiplies<float>());
}
DLLEXP void ewMulD(double* a, const double* b, const size_t N) {
	std::transform(a, a + N, b, a, multiplies<double>());
}
DLLEXP void ewMulC(std::complex<float>* a, const std::complex<float>* b, const size_t N) {
	std::transform(a, a + N, b, a, multiplies<std::complex<float>>());
}
DLLEXP void ewMulZ(std::complex<double>* a, const std::complex<double>* b, const size_t N) {
	std::transform(a, a + N, b, a, multiplies<std::complex<double>>());
}
END_CUSTOM_EXTERN_C
#pragma endregion


#pragma region power
template<typename T, typename R> inline void ewPow(T* a, R p, const size_t N) {
	const T* last = a + N;
	for (; a < last; ++a) {
		*a = std::pow(*a, p);
	}
}

CUSTOM_EXTERN_C
DLLEXP void ewPowS(float* a, const float p, const size_t N) {
	ewPow(a, p, N);
}
DLLEXP void ewPowD(double* a, const double p, const size_t N) {
	ewPow(a, p, N);
}
DLLEXP void ewPowC(std::complex<float>* a, const float p, const size_t N) {
	ewPow(a, p, N);
}
DLLEXP void ewPowZ(std::complex<double>* a, const double p, const size_t N) {
	ewPow(a, p, N);
}
END_CUSTOM_EXTERN_C
#pragma endregion


#pragma region conjuagte
CUSTOM_EXTERN_C
DLLEXP void arrConjC(std::complex<float>* a, const size_t N) {
	const std::complex<float>* last = a + N;
	for (; a < last; ++a) {
		*a = std::complex<float>((*a).real(), -(*a).imag());
	}
}
DLLEXP void arrConjZ(std::complex<double>* a, const size_t N) {
	const std::complex<double>* last = a + N;
	for (; a < last; ++a) {
		*a = std::complex<double>((*a).real(), -(*a).imag());
	}
}
END_CUSTOM_EXTERN_C
#pragma endregion


#pragma region up cast
CUSTOM_EXTERN_C
DLLEXP void arrUpS2D(double* dest, const float* src, const size_t N) {
	const float* last = src + N;
	for (; src < last; ++src, ++dest) {
		*dest = *src;
	}
}
END_CUSTOM_EXTERN_C
#pragma endregion


#pragma region set at positions
template<typename T> inline void setArrOne(T* a, T v, const int* pos, const size_t N) {
	const int* last = pos + N;
	for (; pos < last; ++pos) {
		a[*pos] = v;
	}
}

CUSTOM_EXTERN_C
DLLEXP void setArrOneS(float* a, const float v, const int* pos, const size_t N) {
	setArrOne(a, v, pos, N);
}
DLLEXP void setArrOneD(double* a, const double v, const int* pos, const size_t N) {
	setArrOne(a, v, pos, N);
}
DLLEXP void setArrOneC(std::complex<float>* a, const std::complex<float> v, const int* pos, const size_t N) {
	setArrOne(a, v, pos, N);
}
DLLEXP void setArrOneZ(std::complex<double>* a, const std::complex<double> v, const int* pos, const size_t N) {
	setArrOne(a, v, pos, N);
}
END_CUSTOM_EXTERN_C
#pragma endregion


#pragma region trim
template<typename T> inline void arrTrimReal(T* a, float thre, const size_t N) {
	const T* last = a + N;
	for (; a < last; ++a) {
		if (std::abs(*a) <= thre)
			*a = 0;
	}
}
template<typename T, typename R> inline void arrTrimComp(T* a, float thre, const size_t N) {
	const T* last = a + N;
	for (; a < last; ++a) {
		if (std::abs(std::complex<R>(*a)) <= thre)
			*a = T();
	}
}

CUSTOM_EXTERN_C
DLLEXP void arrTrimS(float* a, const float thre, const size_t N) {
	arrTrimReal(a, thre, N);
}
DLLEXP void arrTrimD(double* a, const float thre, const size_t N) {
	arrTrimReal(a, thre, N);
}
DLLEXP void arrTrimC(std::complex<float>* a, const float thre, const size_t N) {
	arrTrimComp<std::complex<float>, float>(a, thre, N);
}
DLLEXP void arrTrimZ(std::complex<double>* a, const float thre, const size_t N) {
	arrTrimComp<std::complex<double>, double>(a, thre, N);
}
END_CUSTOM_EXTERN_C
#pragma endregion


#pragma region vector sum
struct float2
{
	float a, b;
};
struct double2
{
	double a, b;
};

CUSTOM_EXTERN_C
DLLEXP float sumVecS(const float* v, const size_t len, const unsigned int stride)
{
	if (stride == 1)
		return std::accumulate(v, v + len, 0.0F);
	const auto slice = std::slice(0, len, stride);
	const auto arr = std::valarray<float>(v, len * stride);
	return arr[slice].sum();
}

DLLEXP double sumVecD(const double* v, const size_t len, const unsigned int stride)
{
	if (stride == 1)
		return std::accumulate(v, v + len, 0.0);
	const auto slice = std::slice(0, len, stride);
	const auto arr = std::valarray<double>(v, len * stride);
	return arr[slice].sum();
}

DLLEXP float2 sumVecC(const std::complex<float>* v, const size_t len, const unsigned int stride)
{
	if (stride == 1)
	{
		auto comp = std::accumulate(v, v + len, std::complex<float>(0.0F, 0.0F));
		return { comp.real(), comp.imag() };
	}
	const auto slice = std::slice(0, len, stride);
	const auto arr = std::valarray<std::complex<float>>(v, len * stride);
	auto res = arr[slice].sum();
	return { res.real(), res.imag() };
}

DLLEXP double2 sumVecZ(const std::complex<double>* v, const size_t len, const unsigned int stride)
{
	if (stride == 1)
	{
		auto comp = std::accumulate(v, v + len, std::complex<double>(0.0, 0.0));
		return { comp.real(), comp.imag() };
	}
	const auto slice = std::slice(0, len, stride);
	const auto arr = std::valarray<std::complex<double>>(v, len * stride);
	auto res = arr[slice].sum();
	return { res.real(), res.imag() };
}
END_CUSTOM_EXTERN_C
#pragma endregion
