#pragma once

#include <cuda_runtime.h>
#include <cmath>

// TODO: if constexpr has problem that do not return values correctly?
namespace BlasSupp
{
	template <typename T>
	__host__ __device__ inline static T inf()
	{
		if constexpr (std::is_floating_point<T>::value)
			return (T)(INFINITY);
		else if constexpr (std::is_integral<T>::value)
			return (T)(LLONG_MAX);
		else
			return T();
	}

	template <typename T>
	__host__ __device__ inline static bool isinf(const T a)
	{
		if constexpr (std::is_floating_point<T>::value)
			return a == inf<T>() || a == -inf<T>();
		else
			return false;
	}

	template <typename T>
	__host__ __device__ inline static T nan()
	{
		if constexpr (std::is_floating_point<T>::value)
			return (T)(NAN);
		else if constexpr (std::is_integral<T>::value)
			return (T)(LLONG_MAX);
		else
			return T();
	}

	template <typename T>
	__host__ __device__ inline static bool isnan(const T a)
	{
		if constexpr (std::is_floating_point<T>::value)
			return (float)a == NAN;
		else
			return false;
	}

	template <typename T>
	struct complex
	{
		using value_type = T;

		T _real, _imag;

		__host__ __device__ constexpr complex(const T& real = T(), const T& imag = T()) : _real(real), _imag(imag) {}

		__host__ __device__ constexpr inline T real() const
		{
			return this->_real;
		}

		__host__ __device__ constexpr inline T imag() const
		{
			return this->_imag;
		}

		__host__ __device__ inline complex<T> conj() const
		{
			if constexpr (std::is_signed<T>::value)
				return complex<T>(this->_real, -this->_imag);
			else
				return *this; // not allowed
		}
	};
}


#pragma region integer type float-like operations
namespace std
{
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


	__host__ __device__ static inline constexpr signed char hypot(const char a, const char b) { return (char)hypotf(a, b); }
	__host__ __device__ static inline constexpr signed short hypot(const short a, const short b) { return (short)hypotf(a, b); }
	__host__ __device__ static inline constexpr signed int hypot(const int a, const int b) { return (int)hypot((double)a, (double)b); }
	__host__ __device__ static inline constexpr signed long hypot(const long a, const long b) { return (long)hypot((double)a, (double)b); }
	__host__ __device__ static inline constexpr signed long long hypot(const long long a, const long long b) { return (long long)hypot((double)a, (double)b); }
	__host__ __device__ static inline constexpr unsigned char hypot(const unsigned char a, const unsigned char b) { return (unsigned char)hypotf(a, b); }
	__host__ __device__ static inline constexpr unsigned short hypot(const unsigned short a, const unsigned short b) { return (unsigned short)hypotf(a, b); }
	__host__ __device__ static inline constexpr unsigned int hypot(const unsigned int a, const unsigned int b) { return (unsigned int)hypot((double)a, (double)b); }
	__host__ __device__ static inline constexpr unsigned long hypot(const unsigned long a, const unsigned long b) { return (unsigned long)hypot((double)a, (double)b); }
	__host__ __device__ static inline constexpr unsigned long long hypot(const unsigned long long a, const unsigned long long b) { return (unsigned long long)hypot((double)a, (double)b); }

	__host__ __device__ static inline constexpr signed char pow(const char a, const char b) { return (char)powf(a, b); }
	__host__ __device__ static inline constexpr signed short pow(const short a, const short b) { return (short)powf(a, b); }
	__host__ __device__ static inline constexpr signed int pow(const int a, const int b) { return (int)pow((double)a, (double)b); }
	__host__ __device__ static inline constexpr signed long pow(const long a, const long b) { return (long)pow((double)a, (double)b); }
	__host__ __device__ static inline constexpr signed long long pow(const long long a, const long long b) { return (long long)pow((double)a, (double)b); }
	__host__ __device__ static inline constexpr unsigned char pow(const unsigned char a, const unsigned char b) { return (unsigned char)powf(a, b); }
	__host__ __device__ static inline constexpr unsigned short pow(const unsigned short a, const unsigned short b) { return (unsigned short)powf(a, b); }
	__host__ __device__ static inline constexpr unsigned int pow(const unsigned int a, const unsigned int b) { return (unsigned int)pow((double)a, (double)b); }
	__host__ __device__ static inline constexpr unsigned long pow(const unsigned long a, const unsigned long b) { return (unsigned long)pow((double)a, (double)b); }
	__host__ __device__ static inline constexpr unsigned long long pow(const unsigned long long a, const unsigned long long b) { return (unsigned long long)pow((double)a, (double)b); }

	__host__ __device__ static inline constexpr signed char sqrt(const char a) { return (char)sqrtf(a); }
	__host__ __device__ static inline constexpr signed short sqrt(const short a, const short b) { return (short)sqrtf(a); }
	__host__ __device__ static inline constexpr signed int sqrt(const int a, const int b) { return (int)sqrt((double)a); }
	__host__ __device__ static inline constexpr signed long sqrt(const long a, const long b) { return (long)sqrt((double)a); }
	__host__ __device__ static inline constexpr signed long long sqrt(const long long a, const long long b) { return (long long)sqrt((double)a); }
	__host__ __device__ static inline constexpr unsigned char sqrt(const unsigned char a, const unsigned char b) { return (unsigned char)sqrtf(a); }
	__host__ __device__ static inline constexpr unsigned short sqrt(const unsigned short a, const unsigned short b) { return (unsigned short)sqrtf(a); }
	__host__ __device__ static inline constexpr unsigned int sqrt(const unsigned int a, const unsigned int b) { return (unsigned int)sqrt((double)a); }
	__host__ __device__ static inline constexpr unsigned long sqrt(const unsigned long a, const unsigned long b) { return (unsigned long)sqrt((double)a); }
	__host__ __device__ static inline constexpr unsigned long long sqrt(const unsigned long long a, const unsigned long long b) { return (unsigned long long)sqrt((double)a); }
}
#pragma endregion


#pragma region define complex operators
template <typename T>
__host__ __device__ inline static BlasSupp::complex<T> operator*(const BlasSupp::complex<T> left, const BlasSupp::complex<T> right)
{
	const T real = left._real * right._real - left._imag * right._imag;
	const T imag = left._real * right._imag + left._imag * right._real;
	return BlasSupp::complex<T>(real, imag);
}

template <typename T>
__host__ __device__ inline static BlasSupp::complex<T> operator*(const BlasSupp::complex<T> left, const T right)
{
	const T real = left._real * right;
	const T imag = left._imag * right;
	return BlasSupp::complex<T>(real, imag);
}

template <typename T>
__host__ __device__ inline static BlasSupp::complex<T> operator*(const T left, const BlasSupp::complex<T> right)
{
	const T real = right._real * left;
	const T imag = right._imag * left;
	return BlasSupp::complex<T>(real, imag);
}

template <typename T>
__host__ __device__ inline static BlasSupp::complex<T> operator/(const BlasSupp::complex<T> left, const T right)
{
	const T real = left._real / right;
	const T imag = left._imag / right;
	return BlasSupp::complex<T>(real, imag);
}

template <typename T>
__host__ __device__ inline static BlasSupp::complex<T> operator/(const T left, const BlasSupp::complex<T> right)
{
	const T real = right._real / left;
	const T imag = right._imag / left;
	return BlasSupp::complex<T>(real, imag);
}

template <typename T>
__host__ __device__ inline static BlasSupp::complex<T> operator/(const BlasSupp::complex<T> left, const BlasSupp::complex<T> right)
{
	T real, imag;

	if (BlasSupp::isnan(right._real) || BlasSupp::isnan(right._imag))
	{ // set NaN result
		real = BlasSupp::nan<T>();
		imag = left._real;
	}
	else if (std::abs(right._imag) < std::abs(right._real))
	{ // |_Right.imag()| < |_Right.real()|
		T _Wr = right._imag / right._real;
		T _Wd = right._real + _Wr * right._imag;

		if (BlasSupp::isnan(_Wd) || _Wd == 0)
		{ // set NaN result
			real = BlasSupp::nan<T>();
			imag = left._real;
		}
		else
		{ // compute representable result
			real = (left._real + left._imag * _Wr) / _Wd;
			imag = (left._imag - left._real * _Wr) / _Wd;
		}
	}
	else if (right._imag == 0)
	{ // set NaN result
		real = BlasSupp::nan<T>();
		imag = left._real;
	}
	else
	{ // 0 < |_Right.real()| <= |_Right.imag()|
		T _Wr = right._real / right._imag;
		T _Wd = right._imag + _Wr * right._real;

		if (BlasSupp::isnan(_Wd) || _Wd == 0)
		{ // set NaN result
			real = BlasSupp::nan<T>();
			imag = left._real;
		}
		else
		{ // compute representable result
			real = (left._real * _Wr + left._imag) / _Wd;
			imag = (left._imag * _Wr - left._real) / _Wd;
		}
	}

	return BlasSupp::complex<T>(real, imag);
}

template <typename T>
__host__ __device__ inline static BlasSupp::complex<T> operator+(const BlasSupp::complex<T> left, const BlasSupp::complex<T> right)
{
	const T real = left._real + right._real;
	const T imag = left._imag + right._imag;
	return BlasSupp::complex<T>(real, imag);
}

template <typename T>
__host__ __device__ inline static BlasSupp::complex<T> operator-(const BlasSupp::complex<T> left, const BlasSupp::complex<T> right)
{
	const T real = left._real - right._real;
	const T imag = left._imag - right._imag;
	return BlasSupp::complex<T>(real, imag);
}

template <typename T>
__host__ __device__ inline static bool operator==(const BlasSupp::complex<T> left, const BlasSupp::complex<T> right)
{
	return left._real == right._real && left._imag == right._imag;
}

template <typename T>
__host__ __device__ inline static bool operator==(const BlasSupp::complex<T> left, const T right)
{
	return left == BlasSupp::complex<T>(right);
}

template <typename T, typename U>
__host__ __device__ inline static bool operator==(const BlasSupp::complex<T> left, const U right)
{
	if constexpr (std::is_scalar<U>::value)
		return left == BlasSupp::complex<T>((T)(right));
	else
		return false;
}
#pragma endregion



// self defined methods for complex
namespace std
{
	template <typename T>
	__host__ __device__ static inline T conj(const T a)
	{
		if constexpr (std::is_scalar<T>::value)
			return a;
		else
			return a.conj();
	}

	template <typename T>
	__host__ __device__ static inline T abs(const BlasSupp::complex<T> x)
	{
		return hypot(x.real(), x.imag());
	}

	// FABS implementation from MSVC
	template <class T>
	__host__ __device__ static inline T _Fabs(const BlasSupp::complex<T> comp, int* _Pexp)
	{ // return magnitude and scale factor
		*_Pexp = 0;
		T _Av = comp.real();
		T _Bv = comp.imag();

		if (BlasSupp::isinf(_Av) || BlasSupp::isinf(_Bv)) {
			return BlasSupp::inf<T>(); // at least one component is INF
		}
		else if (BlasSupp::isnan(_Av)) {
			return _Av; // real component is NaN
		}
		else if (BlasSupp::isnan(_Bv)) {
			return _Bv; // imaginary component is NaN
		}
		else { // neither component is NaN or INF
			_Av = abs(_Av);
			_Bv = abs(_Bv);

			if (_Av < _Bv) { // ensure that |_Bv| <= |_Av|
				T _Tmp = _Av;
				_Av = _Bv;
				_Bv = _Tmp;
			}

			if (_Av == 0) {
				return _Av; // |0| == 0
			}

			if (1 <= _Av) {
				*_Pexp = 2;
				_Av = _Av * static_cast<T>(0.25);
				_Bv = _Bv * static_cast<T>(0.25);
			}
			else {
				*_Pexp = -2;
				_Av = _Av * 4;
				_Bv = _Bv * 4;
			}

			T _Tmp = _Av - _Bv;
			if (_Tmp == _Av) {
				return _Av; // _Bv unimportant
			}
			else if (_Bv < _Tmp) { // use simple approximation
				const T _Qv = _Av / _Bv;
				return _Av + _Bv / (_Qv + sqrt(_Qv * _Qv + 1));
			}
			else { // use 1 1/2 precision to preserve bits
				constexpr T _Root2 = static_cast<T>(1.4142135623730950488016887242096981L);
				constexpr T _Oneplusroot2high = static_cast<T>(10125945.0 / 4194304.0); // exact if precision >= 24 bits
				constexpr T _Oneplusroot2low = static_cast<T>(1.4341252375973918872420969807856967e-7L);

				const T _Qv = _Tmp / _Bv;
				const T _Rv = (_Qv + 2) * _Qv;
				const T _Sv = _Rv / (_Root2 + sqrt(_Rv + 2)) + _Oneplusroot2low + _Qv + _Oneplusroot2high;
				return _Av + _Bv / _Sv;
			}
		}
	}

	// log implementation from MSVC
	template <class T>
	__host__ __device__ static inline BlasSupp::complex<T> log(const BlasSupp::complex<T> comp)
	{
		// integer types need conversions
		if constexpr (is_integral<T>::value)
		{
			const auto doubleVal = log(BlasSupp::complex<double>((double)(comp.real()), (double)(comp.imag())));
			return BlasSupp::complex<double>((T)(doubleVal.real()), (T)(doubleVal.imag()));
		}

		T _Theta = (T)(atan2((double)(comp.real()), (double)(comp.imag()))); // get phase

		if (BlasSupp::isnan(_Theta))
		{
			return BlasSupp::complex<T>(_Theta, _Theta); // real or imaginary is NaN
		}
		else
		{ // use 1 1/2 precision to preserve bits
			constexpr T _Cm = static_cast<T>(22713.0L / 32768.0L);
			constexpr T _Cl = static_cast<T>(1.4286068203094172321214581765680755e-6L);
			int compexp;
			T _Rho = _Fabs(comp, &compexp); // get magnitude and scale factor

			T compn = static_cast<T>(compexp);

			T _Real;
			if (_Rho == 0)
			{
				_Real = -BlasSupp::inf<T>(); // log(0) == -INF
			}
			else if (BlasSupp::isinf(_Rho))
			{
				_Real = _Rho; // log(INF) == INF
			}
			else
			{
				_Real = static_cast<T>(log(_Rho)); // These casts are TRANSITION, DevCom-1093507
				_Real += static_cast<T>(compn * _Cl);
				_Real += static_cast<T>(compn * _Cm);
			}

			return BlasSupp::complex<T>(_Real, _Theta);
		}
	}

	// exp implementation from MSVC
	template <typename T>
	__host__ __device__ static inline BlasSupp::complex<T> exp(const BlasSupp::complex<T> comp)
	{
		if constexpr (is_floating_point<T>::value)
		{
			T real = comp.real(), imag = comp.imag();
			real = exp(real);
			return BlasSupp::complex<T>(real * cos(imag), real * sin(imag));
		}
		else
		{
			// integer types need conversions
			double real = (double)(comp.real()), imag = (double)(comp.imag());
			real = exp(real);
			return BlasSupp::complex<T>((T)(real * cos(imag)), (T)(real * sin(imag)));
		}
	}

	// pow implementation from MSVC
	template <typename T>
	__host__ __device__ static inline BlasSupp::complex<T> pow(const BlasSupp::complex<T> base, const T p)
	{
		if (base.imag() == 0)
		{
			T real = std::pow(base.real(), p);
			return BlasSupp::complex<T>(real, base.imag());
		}
		else
		{
			return exp(log(base) * p);
		}
	}

	// pow implementation from MSVC
	template <typename T>
	__host__ __device__ static inline BlasSupp::complex<T> pow(const BlasSupp::complex<T> base, const BlasSupp::complex<T> p)
	{
		if (p.imag() == 0)
		{
			return pow(base, p.real());
		}
		else if (base.imag() == 0 && 0 < base.real())
		{
			return exp(p * log(base.real()));
		}
		else
		{
			return exp(p * log(base));
		}
	}
}