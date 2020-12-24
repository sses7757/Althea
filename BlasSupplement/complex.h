#pragma once

#include <cuda_runtime.h>
#include <cmath>
#include <type_traits>


// TODO: CUDA has a problem that gives false warnings for CONSTEXPR IF
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
		// false return at end to suppress NVCC problem
		return T();
	}

	template <typename T>
	__host__ __device__ inline static bool isinf(const T a)
	{
		if constexpr (std::is_floating_point<T>::value)
			return a == inf<T>() || a == -inf<T>();
		else
			return false;
		// false return at end to suppress NVCC problem
		return true;
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
		// false return at end to suppress NVCC problem
		return T();
	}

	template <typename T>
	__host__ __device__ inline static bool isnan(const T a)
	{
		if constexpr (std::is_floating_point<T>::value)
			return (float)a == NAN;
		else
			return false;
		// false return at end to suppress NVCC problem
		return true;
	}

	template <typename T>
	struct complex
	{
		typedef typename T value_type;

		T _real, _imag;

		__host__ __device__ constexpr complex(const T& real = T(), const T& imag = T()) : _real(real), _imag(imag) {}

		__host__ __device__ constexpr inline T real() const
		{
			return _real;
		}

		__host__ __device__ constexpr inline T imag() const
		{
			return _imag;
		}

		__host__ __device__ inline complex<T> conj() const
		{
			if constexpr (std::is_signed<T>::value)
				return complex<T>(_real, -_imag);
			else
				return *this; // not allowed
			// false return at end to suppress NVCC problem
			return *this;
		}
		
		__host__ __device__ inline T absSquare() const
		{
			return _real * _real + _imag * _imag;
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

	__host__ __device__ static inline signed char hypot(const char a, const char b) { return (char)hypotf(a, b); }
	__host__ __device__ static inline signed short hypot(const short a, const short b) { return (short)hypotf(a, b); }
	__host__ __device__ static inline signed int hypot(const int a, const int b) { return (int)hypot((double)a, (double)b); }
	__host__ __device__ static inline signed long hypot(const long a, const long b) { return (long)hypot((double)a, (double)b); }
	__host__ __device__ static inline signed long long hypot(const long long a, const long long b) { return (long long)hypot((double)a, (double)b); }
	__host__ __device__ static inline unsigned char hypot(const unsigned char a, const unsigned char b) { return (unsigned char)hypotf(a, b); }
	__host__ __device__ static inline unsigned short hypot(const unsigned short a, const unsigned short b) { return (unsigned short)hypotf(a, b); }
	__host__ __device__ static inline unsigned int hypot(const unsigned int a, const unsigned int b) { return (unsigned int)hypot((double)a, (double)b); }
	__host__ __device__ static inline unsigned long hypot(const unsigned long a, const unsigned long b) { return (unsigned long)hypot((double)a, (double)b); }
	__host__ __device__ static inline unsigned long long hypot(const unsigned long long a, const unsigned long long b) { return (unsigned long long)hypot((double)a, (double)b); }

	__host__ __device__ static inline signed char pow(const char a, const char b) { return (char)powf(a, b); }
	__host__ __device__ static inline signed short pow(const short a, const short b) { return (short)powf(a, b); }
	__host__ __device__ static inline signed int pow(const int a, const int b) { return (int)pow((double)a, (double)b); }
	__host__ __device__ static inline signed long pow(const long a, const long b) { return (long)pow((double)a, (double)b); }
	__host__ __device__ static inline signed long long pow(const long long a, const long long b) { return (long long)pow((double)a, (double)b); }
	__host__ __device__ static inline unsigned char pow(const unsigned char a, const unsigned char b) { return (unsigned char)powf(a, b); }
	__host__ __device__ static inline unsigned short pow(const unsigned short a, const unsigned short b) { return (unsigned short)powf(a, b); }
	__host__ __device__ static inline unsigned int pow(const unsigned int a, const unsigned int b) { return (unsigned int)pow((double)a, (double)b); }
	__host__ __device__ static inline unsigned long pow(const unsigned long a, const unsigned long b) { return (unsigned long)pow((double)a, (double)b); }
	__host__ __device__ static inline unsigned long long pow(const unsigned long long a, const unsigned long long b) { return (unsigned long long)pow((double)a, (double)b); }

	__host__ __device__ static inline signed char sqrt(const char a) { return (char)sqrtf(a); }
	__host__ __device__ static inline signed short sqrt(const short a, const short b) { return (short)sqrtf(a); }
	__host__ __device__ static inline signed int sqrt(const int a, const int b) { return (int)sqrt((double)a); }
	__host__ __device__ static inline signed long sqrt(const long a, const long b) { return (long)sqrt((double)a); }
	__host__ __device__ static inline signed long long sqrt(const long long a, const long long b) { return (long long)sqrt((double)a); }
	__host__ __device__ static inline unsigned char sqrt(const unsigned char a, const unsigned char b) { return (unsigned char)sqrtf(a); }
	__host__ __device__ static inline unsigned short sqrt(const unsigned short a, const unsigned short b) { return (unsigned short)sqrtf(a); }
	__host__ __device__ static inline unsigned int sqrt(const unsigned int a, const unsigned int b) { return (unsigned int)sqrt((double)a); }
	__host__ __device__ static inline unsigned long sqrt(const unsigned long a, const unsigned long b) { return (unsigned long)sqrt((double)a); }
	__host__ __device__ static inline unsigned long long sqrt(const unsigned long long a, const unsigned long long b) { return (unsigned long long)sqrt((double)a); }
}
#pragma endregion


#pragma region define complex operators
template <typename T>
__host__ __device__ inline static BlasSupp::complex<T> operator*(const BlasSupp::complex<T> left, const BlasSupp::complex<T> right)
{
	const T real = left.real() * right.real() - left.imag() * right.imag();
	const T imag = left.real() * right.imag() + left.imag() * right.real();
	return BlasSupp::complex<T>(real, imag);
}

template <typename T>
__host__ __device__ inline static BlasSupp::complex<T> operator*(const BlasSupp::complex<T> left, const T right)
{
	const T real = left.real() * right;
	const T imag = left.imag() * right;
	return BlasSupp::complex<T>(real, imag);
}

template <typename T>
__host__ __device__ inline static BlasSupp::complex<T> operator*(const T left, const BlasSupp::complex<T> right)
{
	const T real = right.real() * left;
	const T imag = right.imag() * left;
	return BlasSupp::complex<T>(real, imag);
}

template <typename T>
__host__ __device__ inline static BlasSupp::complex<T> operator/(const BlasSupp::complex<T> left, const T right)
{
	const T real = left.real() / right;
	const T imag = left.imag() / right;
	return BlasSupp::complex<T>(real, imag);
}

template <typename T>
__host__ __device__ inline static BlasSupp::complex<T> operator/(const T left, const BlasSupp::complex<T> right)
{
	const T real = right.real() / left;
	const T imag = right.imag() / left;
	return BlasSupp::complex<T>(real, imag);
}

// direct approach of div operator
template <typename T>
__host__ __device__ inline static BlasSupp::complex<T> operator/(const BlasSupp::complex<T> x, const BlasSupp::complex<T> y)
{
	const T squareAbsY = y.absSquare();
	const T acbd = x.real() * y.real() + x.imag() * y.imag();
	const T bcad = x.imag() * y.real() - x.real() * y.imag();
	return BlasSupp::complex<T>(acbd / squareAbsY, bcad / squareAbsY);
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
	return left.imag() == 0 && left.real() == right;
}

template <typename T, typename U>
__host__ __device__ inline static bool operator==(const BlasSupp::complex<T> left, const U right)
{
	if constexpr (std::is_scalar<U>::value)
		return left == BlasSupp::complex<T>((T)right);
	else
		return false;
	// false return at end to suppress NVCC problem
	return true;
}
#pragma endregion


#pragma region self defined methods for complex
namespace std
{
	template <typename T>
	__host__ __device__ static inline T conj(const T a)
	{
		if constexpr (std::is_scalar<T>::value)
			return a;
		else
			return a.conj();
		// false return at end to suppress NVCC problem
		return T();
	}

	template <typename T>
	__host__ __device__ static inline T abs(const BlasSupp::complex<T> x)
	{
		return hypot(x.real(), x.imag());
	}
	
	#pragma region integer type conversions
	template <typename T, typename U>
	__host__ __device__ static inline BlasSupp::complex<T> _interger_op(BlasSupp::complex<U>(*func)(BlasSupp::complex<U>), BlasSupp::complex<T> left)
	{
		const BlasSupp::complex<U> result = func(BlasSupp::complex<U>((U)left.real(), (U)left.imag()));
		return BlasSupp::complex<T>((T)result.real(), (T)result.imag());
	}

	template <typename T, typename U>
	__host__ __device__ static inline BlasSupp::complex<T> _interger_op(BlasSupp::complex<U>(*func)(BlasSupp::complex<U>, U), BlasSupp::complex<T> left, T right)
	{
		const BlasSupp::complex<U> result = func(BlasSupp::complex<U>((U)left.real(), (U)left.imag()), (U)right);
		return BlasSupp::complex<T>((T)result.real(), (T)result.imag());
	}

	template <typename T, typename U>
	__host__ __device__ static inline BlasSupp::complex<T> _interger_op(BlasSupp::complex<U>(*func)(BlasSupp::complex<U>, BlasSupp::complex<U>), BlasSupp::complex<T> left, BlasSupp::complex<T> right)
	{
		const BlasSupp::complex<U> result = func(BlasSupp::complex<U>((U)left.real(), (U)left.imag()), BlasSupp::complex<U>((U)right.real(), (U)right.imag()));
		return BlasSupp::complex<T>((T)result.real(), (T)result.imag());
	}
	#pragma endregion

	// direct log implementation
	template <typename T>
	__host__ __device__ static inline BlasSupp::complex<T> log(const BlasSupp::complex<T> comp)
	{
		// Ignore Spelling: mathtt hypot
		//tex:$\log(a+b\mathtt{i}) = \log(\mathrm{hypot}(a,b))+\mathtt{i}\cdot\mathrm{atan2}(a,b)$

		if constexpr (!is_floating_point<T>::value)
		{	// integer types need conversions
			if constexpr (sizeof(T) < 4)
				return _interger_op(log<float>, comp);
			else
				return _interger_op(log<double>, comp);
			// false return at end to suppress NVCC problem
			return BlasSupp::complex<T>();
		}
		else
		{
			const T real = log(abs(comp));
			const T imag = atan2(comp.real(), comp.imag());
			return BlasSupp::complex<T>(real, imag);
		}
		// false return at end to suppress NVCC problem
		return BlasSupp::complex<T>();
	}

	// direct exp implementation
	template <typename T>
	__host__ __device__ static inline BlasSupp::complex<T> exp(const BlasSupp::complex<T> comp)
	{
		if constexpr (!is_floating_point<T>::value)
		{	// integer types need conversions
			if constexpr (sizeof(T) < 4)
				return _interger_op(exp<float>, comp);
			else
				return _interger_op(exp<double>, comp);
			// false return at end to suppress NVCC problem
			return BlasSupp::complex<T>();
		}
		else
		{
			const T real = exp(comp.real()), imag = comp.imag();
			return BlasSupp::complex<T>(real * cos(imag), real * sin(imag));
		}
		// false return at end to suppress NVCC problem
		return BlasSupp::complex<T>();
	}

	// pow implementation from MSVC
	template <typename T>
	__host__ __device__ static inline BlasSupp::complex<T> pow(const BlasSupp::complex<T> base, const T p)
	{
		if constexpr (!is_floating_point<T>::value)
		{	// integer types need conversions
			if constexpr (sizeof(T) < 4)
				return _interger_op(pow<float>, base, p);
			else
				return _interger_op(pow<double>, base, p);
			// false return at end to suppress NVCC problem
			return BlasSupp::complex<T>();
		}
		else
		{
			if (base.imag() == 0)
			{
				T real = std::pow(base.real(), p);
				return BlasSupp::complex<T>(real, base.imag());
			}
			else
			{
				return exp(p * log(base));
			}
		}
		// false return at end to suppress NVCC problem
		return BlasSupp::complex<T>();
	}

	// pow implementation from MSVC
	template <typename T>
	__host__ __device__ static inline BlasSupp::complex<T> pow(const BlasSupp::complex<T> base, const BlasSupp::complex<T> p)
	{
		if constexpr (!is_floating_point<T>::value)
		{	// integer types need conversions
			if constexpr (sizeof(T) < 4)
				return _interger_op(pow<float>, base, p);
			else
				return _interger_op(pow<double>, base, p);
			// false return at end to suppress NVCC problem
			return BlasSupp::complex<T>();
		}
		else
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
		// false return at end to suppress NVCC problem
		return BlasSupp::complex<T>();
	}
}
#pragma endregion
