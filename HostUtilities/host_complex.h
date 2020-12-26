#pragma once

#include <cmath>
#include <type_traits>
#include <stdint.h>


namespace complex
{
	template <typename T>
	inline static T inf()
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
	inline static bool isinf(const T a)
	{
		if constexpr (std::is_floating_point<T>::value)
			return a == inf<T>() || a == -inf<T>();
		else
			return false;
		// false return at end to suppress NVCC problem
		return true;
	}

	template <typename T>
	inline static T nan()
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
	inline static bool isnan(const T a)
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

		constexpr complex(const T& real = T(), const T& imag = T()) : _real(real), _imag(imag) {}

		constexpr inline T real() const
		{
			return _real;
		}

		constexpr inline T imag() const
		{
			return _imag;
		}

		inline complex<T> conj() const
		{
			if constexpr (std::is_signed<T>::value)
				return complex<T>(_real, -_imag);
			else
				return *this; // not allowed
			// false return at end to suppress NVCC problem
			return *this;
		}
		
		inline T absSquare() const
		{
			return _real * _real + _imag * _imag;
		}
	};
}


#pragma region integer type float-like operations
namespace std
{
	static inline constexpr int8_t fma(const int8_t x, const int8_t y, const int8_t d) { return x * y + d; }
	static inline constexpr int16_t fma(const int16_t x, const int16_t y, const int16_t d) { return x * y + d; }
	static inline constexpr int32_t fma(const int32_t x, const int32_t y, const int32_t d) { return x * y + d; }
	static inline constexpr int64_t fma(const int64_t x, const int64_t y, const int64_t d) { return x * y + d; }
	static inline constexpr uint8_t fma(const uint8_t x, const uint8_t y, const uint8_t d) { return x * y + d; }
	static inline constexpr uint16_t fma(const uint16_t x, const uint16_t y, const uint16_t d) { return x * y + d; }
	static inline constexpr uint32_t fma(const uint32_t x, const uint32_t y, const uint32_t d) { return x * y + d; }
	static inline constexpr uint64_t fma(const uint64_t x, const uint64_t y, const uint64_t d) { return x * y + d; }

	static inline constexpr int8_t abs(const int8_t a) { return a < 0I8 ? -a : a; }
	static inline constexpr int16_t abs(const int16_t a) { return a < 0I16 ? -a : a; }
	static inline constexpr uint8_t abs(const uint8_t a) { return a; }
	static inline constexpr uint16_t abs(const uint16_t a) { return a; }
	static inline constexpr uint32_t abs(const uint32_t a) { return a; }
	static inline constexpr uint64_t abs(const uint64_t a) { return a; }

	static inline int8_t hypot(const int8_t a, const int8_t b) { return (int8_t)hypotf(a, b); }
	static inline int16_t hypot(const int16_t a, const int16_t b) { return (int16_t)hypotf(a, b); }
	static inline int32_t hypot(const int32_t a, const int32_t b) { return (int32_t)hypot((double)a, (double)b); }
	static inline int64_t hypot(const int64_t a, const int64_t b) { return (int64_t)hypot((double)a, (double)b); }
	static inline uint8_t hypot(const uint8_t a, const uint8_t b) { return (uint8_t)hypotf(a, b); }
	static inline uint16_t hypot(const uint16_t a, const uint16_t b) { return (uint16_t)hypotf(a, b); }
	static inline uint32_t hypot(const uint32_t a, const uint32_t b) { return (uint32_t)hypot((double)a, (double)b); }
	static inline uint64_t hypot(const uint64_t a, const uint64_t b) { return (uint64_t)hypot((double)a, (double)b); }

	static inline int8_t pow(const int8_t a, const int8_t b) { return (int8_t)powf(a, b); }
	static inline int16_t pow(const int16_t a, const int16_t b) { return (int16_t)powf(a, b); }
	static inline int32_t pow(const int32_t a, const int32_t b) { return (int32_t)pow((double)a, (double)b); }
	static inline int64_t pow(const int64_t a, const int64_t b) { return (int64_t)pow((double)a, (double)b); }
	static inline uint8_t pow(const uint8_t a, const uint8_t b) { return (uint8_t)powf(a, b); }
	static inline uint16_t pow(const uint16_t a, const uint16_t b) { return (uint16_t)powf(a, b); }
	static inline uint32_t pow(const uint32_t a, const uint32_t b) { return (uint32_t)pow((double)a, (double)b); }
	static inline uint64_t pow(const uint64_t a, const uint64_t b) { return (uint64_t)pow((double)a, (double)b); }

	static inline int8_t sqrt(const int8_t a) { return (int8_t)sqrtf(a); }
	static inline int16_t sqrt(const int16_t a, const int16_t b) { return (int16_t)sqrtf(a); }
	static inline int32_t sqrt(const int32_t a, const int32_t b) { return (int32_t)sqrt((double)a); }
	static inline int64_t sqrt(const int64_t a, const int64_t b) { return (int64_t)sqrt((double)a); }
	static inline uint8_t sqrt(const uint8_t a, const uint8_t b) { return (uint8_t)sqrtf(a); }
	static inline uint16_t sqrt(const uint16_t a, const uint16_t b) { return (uint16_t)sqrtf(a); }
	static inline uint32_t sqrt(const uint32_t a, const uint32_t b) { return (uint32_t)sqrt((double)a); }
	static inline uint64_t sqrt(const uint64_t a, const uint64_t b) { return (uint64_t)sqrt((double)a); }
}
#pragma endregion


#pragma region define complex operators
template <typename T>
inline static complex::complex<T> operator*(const complex::complex<T> left, const complex::complex<T> right)
{
	const T real = left.real() * right.real() - left.imag() * right.imag();
	const T imag = left.real() * right.imag() + left.imag() * right.real();
	return complex::complex<T>(real, imag);
}

template <typename T>
inline static complex::complex<T> operator*(const complex::complex<T> left, const T right)
{
	const T real = left.real() * right;
	const T imag = left.imag() * right;
	return complex::complex<T>(real, imag);
}

template <typename T>
inline static complex::complex<T> operator*(const T left, const complex::complex<T> right)
{
	const T real = right.real() * left;
	const T imag = right.imag() * left;
	return complex::complex<T>(real, imag);
}

template <typename T>
inline static complex::complex<T> operator/(const complex::complex<T> left, const T right)
{
	const T real = left.real() / right;
	const T imag = left.imag() / right;
	return complex::complex<T>(real, imag);
}

template <typename T>
inline static complex::complex<T> operator/(const T left, const complex::complex<T> right)
{
	const T real = right.real() / left;
	const T imag = right.imag() / left;
	return complex::complex<T>(real, imag);
}

// direct approach of div operator
template <typename T>
inline static complex::complex<T> operator/(const complex::complex<T> x, const complex::complex<T> y)
{
	const T squareAbsY = y.absSquare();
	const T acbd = x.real() * y.real() + x.imag() * y.imag();
	const T bcad = x.imag() * y.real() - x.real() * y.imag();
	return complex::complex<T>(acbd / squareAbsY, bcad / squareAbsY);
}

template <typename T>
inline static complex::complex<T> operator+(const complex::complex<T> left, const complex::complex<T> right)
{
	const T real = left._real + right._real;
	const T imag = left._imag + right._imag;
	return complex::complex<T>(real, imag);
}

template <typename T>
inline static complex::complex<T> operator-(const complex::complex<T> left, const complex::complex<T> right)
{
	const T real = left._real - right._real;
	const T imag = left._imag - right._imag;
	return complex::complex<T>(real, imag);
}

template <typename T>
inline static bool operator==(const complex::complex<T> left, const complex::complex<T> right)
{
	return left._real == right._real && left._imag == right._imag;
}

template <typename T>
inline static bool operator==(const complex::complex<T> left, const T right)
{
	return left.imag() == 0 && left.real() == right;
}

template <typename T, typename U>
inline static bool operator==(const complex::complex<T> left, const U right)
{
	if constexpr (std::is_scalar<U>::value)
		return left == complex::complex<T>((T)right);
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
	static inline T conj(const T a)
	{
		if constexpr (std::is_scalar<T>::value)
			return a;
		else
			return a.conj();
		// false return at end to suppress NVCC problem
		return T();
	}

	template <typename T>
	static inline T abs(const complex::complex<T> x)
	{
		return hypot(x.real(), x.imag());
	}
	
	#pragma region integer type conversions
	template <typename T, typename U>
	static inline complex::complex<T> _interger_op(complex::complex<U>(*func)(complex::complex<U>), complex::complex<T> left)
	{
		const complex::complex<U> result = func(complex::complex<U>((U)left.real(), (U)left.imag()));
		return complex::complex<T>((T)result.real(), (T)result.imag());
	}

	template <typename T, typename U>
	static inline complex::complex<T> _interger_op(complex::complex<U>(*func)(complex::complex<U>, U), complex::complex<T> left, T right)
	{
		const complex::complex<U> result = func(complex::complex<U>((U)left.real(), (U)left.imag()), (U)right);
		return complex::complex<T>((T)result.real(), (T)result.imag());
	}

	template <typename T, typename U>
	static inline complex::complex<T> _interger_op(complex::complex<U>(*func)(complex::complex<U>, complex::complex<U>), complex::complex<T> left, complex::complex<T> right)
	{
		const complex::complex<U> result = func(complex::complex<U>((U)left.real(), (U)left.imag()), complex::complex<U>((U)right.real(), (U)right.imag()));
		return complex::complex<T>((T)result.real(), (T)result.imag());
	}
	#pragma endregion

	// direct log implementation
	template <typename T>
	static inline complex::complex<T> log(const complex::complex<T> comp)
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
			return complex::complex<T>();
		}
		else
		{
			const T real = log(abs(comp));
			const T imag = atan2(comp.real(), comp.imag());
			return complex::complex<T>(real, imag);
		}
		// false return at end to suppress NVCC problem
		return complex::complex<T>();
	}

	// direct exp implementation
	template <typename T>
	static inline complex::complex<T> exp(const complex::complex<T> comp)
	{
		if constexpr (!is_floating_point<T>::value)
		{	// integer types need conversions
			if constexpr (sizeof(T) < 4)
				return _interger_op(exp<float>, comp);
			else
				return _interger_op(exp<double>, comp);
			// false return at end to suppress NVCC problem
			return complex::complex<T>();
		}
		else
		{
			const T real = exp(comp.real()), imag = comp.imag();
			return complex::complex<T>(real * cos(imag), real * sin(imag));
		}
		// false return at end to suppress NVCC problem
		return complex::complex<T>();
	}

	// pow implementation from MSVC
	template <typename T>
	static inline complex::complex<T> pow(const complex::complex<T> base, const T p)
	{
		if constexpr (!is_floating_point<T>::value)
		{	// integer types need conversions
			if constexpr (sizeof(T) < 4)
				return _interger_op(pow<float>, base, p);
			else
				return _interger_op(pow<double>, base, p);
			// false return at end to suppress NVCC problem
			return complex::complex<T>();
		}
		else
		{
			if (base.imag() == 0)
			{
				T real = std::pow(base.real(), p);
				return complex::complex<T>(real, base.imag());
			}
			else
			{
				return exp(p * log(base));
			}
		}
		// false return at end to suppress NVCC problem
		return complex::complex<T>();
	}

	// pow implementation from MSVC
	template <typename T>
	static inline complex::complex<T> pow(const complex::complex<T> base, const complex::complex<T> p)
	{
		if constexpr (!is_floating_point<T>::value)
		{	// integer types need conversions
			if constexpr (sizeof(T) < 4)
				return _interger_op(pow<float>, base, p);
			else
				return _interger_op(pow<double>, base, p);
			// false return at end to suppress NVCC problem
			return complex::complex<T>();
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
		return complex::complex<T>();
	}
}
#pragma endregion
