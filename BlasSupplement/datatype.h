#pragma once
#include <string>
#include <stdint.h>

#include "complex.h"


namespace Datatype
{
// check if this compiler has defined a real long double
#if LDBL_MANT_DIG != DBL_MANT_DIG
#define HAS_LDBL
#else
#undef HAS_LDBL
#endif

#pragma region data type enum
	/// <summary>
	/// The general data types defined by flags and masks.
	/// </summary>
	enum DataType
	{
		/// <summary>
		/// The right-most bit that represents the real base type, equals to zero, cannot be used separately.
		/// </summary>
		Real = 0,
		/// <summary>
		/// The right-most bit that represents the complex base type, cannot be used separately. If the value does not have this bit, it is a real type.
		/// </summary>
		Complex = 1 << 0,

		/// <summary>
		/// The type mask (from 1st bit to 7nd bit), cannot be used separately.<br/>
		/// <c>(value &amp; <see cref="TypeMask"/>) &gt;&gt; <see cref="TypeMaskStart"/> = </c> the actual data type used.<br/>
		/// See <see cref="TypeFloat"/>, <see cref="TypeSignedInteger"/>, <see cref="TypeUnsignedInteger"/>.
		/// </summary>
		TypeMask = 0xfe,
		/// <summary>
		/// The start bit of <see cref="TypeMask"/>.
		/// </summary>
		TypeMaskStart = 1,

		// actual types
		/// <summary>
		/// The float base type, cannot be used separately.
		/// </summary>
		TypeFloat = 1 << TypeMaskStart,
		/// <summary>
		/// The signed integer base type, cannot be used separately.
		/// </summary>
		TypeSignedInteger = 2 << TypeMaskStart,
		/// <summary>
		/// The unsigned integer base type, cannot be used separately.
		/// </summary>
		TypeUnsignedInteger = 3 << TypeMaskStart,

		/// <summary>
		/// The number of bytes mask (from 8th bit to 15th bit), cannot be used separately.<br/>
		/// <c>(value &amp; <see cref="ByteMask"/>) &gt;&gt; <see cref="ByteMaskStart"/> = </c> the bytes used (only half of <see cref="Complex"/>'s bytes shall be counted).
		/// </summary>
		ByteMask = 0xff00,
		/// <summary>
		/// The start bit of <see cref="ByteMask"/>.
		/// </summary>
		ByteMaskStart = 8,

		// actual bytes
		/// <summary>
		/// The 1-byte base type, cannot be used separately.
		/// </summary>
		Byte1 = 1 << ByteMaskStart,
		/// <summary>
		/// The 2-byte base type, cannot be used separately.
		/// </summary>
		Byte2 = 2 << ByteMaskStart,
		/// <summary>
		/// The 4-byte base type, cannot be used separately.
		/// </summary>
		Byte4 = 4 << ByteMaskStart,
		/// <summary>
		/// The 8-byte base type, cannot be used separately.
		/// </summary>
		Byte8 = 8 << ByteMaskStart,

		// concrete types
		/// <summary>
		/// <see cref="float"/> = <see cref="Real"/> + <see cref="TypeFloat"/> + <see cref="Byte4"/>
		/// </summary>
		RealSingle = Real | TypeFloat | Byte4,
		/// <summary>
		/// <see cref="double"/> = <see cref="Real"/> + <see cref="TypeFloat"/> + <see cref="Byte8"/>
		/// </summary>
		RealDouble = Real | TypeFloat | Byte8,
		/// <summary>
		/// <see cref="FloatComplex"/> = <see cref="Complex"/> + <see cref="TypeFloat"/> + <see cref="Byte4"/>
		/// </summary>
		ComplexSingle = Complex | TypeFloat | Byte4,
		/// <summary>
		/// <see cref="DoubleComplex"/> = <see cref="Complex"/> + <see cref="TypeFloat"/> + <see cref="Byte8"/>
		/// </summary>
		ComplexDouble = Complex | TypeFloat | Byte8,

		/// <summary>
		/// <see cref="sbyte"/> = <see cref="Real"/> + <see cref="TypeSignedInteger"/> + <see cref="Byte1"/>
		/// </summary>
		RealInt8 = Real | TypeSignedInteger | Byte1,
		/// <summary>
		/// <see cref="short"/> = <see cref="Real"/> + <see cref="TypeSignedInteger"/> + <see cref="Byte2"/>
		/// </summary>
		RealInt16 = Real | TypeSignedInteger | Byte2,
		/// <summary>
		/// <see cref="int"/> = <see cref="Real"/> + <see cref="TypeSignedInteger"/> + <see cref="Byte4"/>
		/// </summary>
		RealInt32 = Real | TypeSignedInteger | Byte4,
		/// <summary>
		/// <see cref="long"/> = <see cref="Real"/> + <see cref="TypeSignedInteger"/> + <see cref="Byte8"/>
		/// </summary>
		RealInt64 = Real | TypeSignedInteger | Byte8,

		/// <summary>
		/// <see cref="byte"/> = <see cref="Real"/> + <see cref="TypeUnsignedInteger"/> + <see cref="Byte1"/>
		/// </summary>
		RealUInt8 = Real | TypeUnsignedInteger | Byte1,
		/// <summary>
		/// <see cref="ushort"/> = <see cref="Real"/> + <see cref="TypeUnsignedInteger"/> + <see cref="Byte2"/>
		/// </summary>
		RealUInt16 = Real | TypeUnsignedInteger | Byte2,
		/// <summary>
		/// <see cref="uint"/> = <see cref="Real"/> + <see cref="TypeUnsignedInteger"/> + <see cref="Byte4"/>
		/// </summary>
		RealUInt32 = Real | TypeUnsignedInteger | Byte4,
		/// <summary>
		/// <see cref="ulong"/> = <see cref="Real"/> + <see cref="TypeUnsignedInteger"/> + <see cref="Byte8"/>
		/// </summary>
		RealUInt64 = Real | TypeUnsignedInteger | Byte8,

		/// <summary>
		/// <see cref="sbyte"/> = <see cref="Complex"/> + <see cref="TypeSignedInteger"/> + <see cref="Byte1"/>
		/// </summary>
		ComplexInt8 = Complex | TypeSignedInteger | Byte1,
		/// <summary>
		/// <see cref="short"/> = <see cref="Complex"/> + <see cref="TypeSignedInteger"/> + <see cref="Byte2"/>
		/// </summary>
		ComplexInt16 = Complex | TypeSignedInteger | Byte2,
		/// <summary>
		/// <see cref="int"/> = <see cref="Complex"/> + <see cref="TypeSignedInteger"/> + <see cref="Byte4"/>
		/// </summary>
		ComplexInt32 = Complex | TypeSignedInteger | Byte4,
		/// <summary>
		/// <see cref="long"/> = <see cref="Complex"/> + <see cref="TypeSignedInteger"/> + <see cref="Byte8"/>
		/// </summary>
		ComplexInt64 = Complex | TypeSignedInteger | Byte8,

		/// <summary>
		/// <see cref="byte"/> = <see cref="Complex"/> + <see cref="TypeUnsignedInteger"/> + <see cref="Byte1"/>
		/// </summary>
		ComplexUInt8 = Complex | TypeUnsignedInteger | Byte1,
		/// <summary>
		/// <see cref="ushort"/> = <see cref="Complex"/> + <see cref="TypeUnsignedInteger"/> + <see cref="Byte2"/>
		/// </summary>
		ComplexUInt16 = Complex | TypeUnsignedInteger | Byte2,
		/// <summary>
		/// <see cref="uint"/> = <see cref="Complex"/> + <see cref="TypeUnsignedInteger"/> + <see cref="Byte4"/>
		/// </summary>
		ComplexUInt32 = Complex | TypeUnsignedInteger | Byte4,
		/// <summary>
		/// <see cref="ulong"/> = <see cref="Complex"/> + <see cref="TypeUnsignedInteger"/> + <see cref="Byte8"/>
		/// </summary>
		ComplexUInt64 = Complex | TypeUnsignedInteger | Byte8,
#ifdef HAS_LDBL
		/// <summary>
		/// <see cref="long double"/> = <see cref="Real"/> + <see cref="TypeFloat"/> + <see cref="Byte8"/>
		/// </summary>
		RealLongDouble = make_floatType(sizeof(long double), false),
		/// <summary>
		/// <see cref="long double"/> = <see cref="Complex"/> + <see cref="TypeFloat"/> + <see cref="Byte8"/>
		/// </summary>
		ComplexLongDouble = make_floatType(sizeof(long double), true),
#endif
	};

	// functions

	// size of a DataType
	inline static constexpr int size(const DataType type)
	{
		return (int)((type & DataType::ByteMask) >> DataType::ByteMaskStart);
	}

	// type code of a DataType
	inline static constexpr int typecode(const DataType type)
	{
		return (int)((type & DataType::TypeMask) >> DataType::TypeMaskStart);
	}

	// is a DataType a real type
	inline static constexpr bool isreal(const DataType type)
	{
		return (type & DataType::Complex) == 0;
	}

	// is a DataType a float type
	inline static constexpr bool isfloat(const DataType type)
	{
		return (type & DataType::TypeFloat) != 0;
	}

	// is a DataType a integer type
	inline static constexpr bool isinteger(const DataType type)
	{
		return (type & DataType::TypeSignedInteger) != 0 || (type & DataType::TypeUnsignedInteger) != 0;
	}

	// generate a float type of given size
	inline static constexpr DataType makeFloatType(const int size, const bool complex)
	{
		return (DataType)(DataType::TypeFloat | (size << DataType::ByteMaskStart) | (complex ? DataType::Complex : DataType::Real));
	}

	// generate a real type of given complex type
	inline static constexpr DataType realCorrespond(const DataType complex)
	{
		if (isreal(complex))
			return complex;
		constexpr int mask = 0b0001;
		return (DataType)(complex ^ mask);
	}

	// to string
	inline static std::string tostring(const DataType type)
	{
		std::string str(isreal(type) ? "Real" : "Complex");
		str = str + " Byte" + std::to_string(size(type)) + (isfloat(type) ? "Float" : "Integer");
		return str;
	}
#pragma endregion



// automatically generate float type switch functions
#define UNSUPPORT(funcName, dataType, returnType) do { \
		if constexpr (std::is_same<returnType, void>::value) \
		{ \
			printf("[%s] does not support [%s]!", #funcName, Datatype::tostring(dataType).c_str()); \
			return returnType(); \
		} \
		else \
		{ \
			return returnType(1); \
		} \
	} while(0)

#ifndef HAS_LDBL
#define AUTO_FLOAT_FUNC(funcName, dataType, returnType, ...) do { \
		switch (dataType) \
		{ \
		case Datatype::RealSingle: \
			return funcName<float>(__VA_ARGS__); \
		case Datatype::RealDouble: \
			return funcName<double>(__VA_ARGS__); \
		case Datatype::ComplexSingle: \
			return funcName<BlasSupp::complex<float>>(__VA_ARGS__); \
		case Datatype::ComplexDouble: \
			return funcName<BlasSupp::complex<double>>(__VA_ARGS__); \
		default: \
			UNSUPPORT(funcName, dataType, returnType); \
		} \
	} while (0)
#else
#define AUTO_FLOAT_FUNC(funcName, dataType, returnType, ...) do { \
		switch (dataType) \
		{ \
		case Datatype::RealSingle: \
			return funcName<float>(__VA_ARGS__); \
		case Datatype::RealDouble: \
			return funcName<double>(__VA_ARGS__); \
		case Datatype::RealLongDouble: \
			return funcName<long double>(__VA_ARGS__); \
		case Datatype::ComplexSingle: \
			return funcName<BlasSupp::complex<float>>(__VA_ARGS__); \
		case Datatype::ComplexDouble: \
			return funcName<BlasSupp::complex<double>>(__VA_ARGS__); \
		case Datatype::ComplexLongDouble: \
			return funcName<BlasSupp::complex<long double>>(__VA_ARGS__); \
		default: \
			UNSUPPORT(funcName, dataType, returnType); \
		} \
	} while (0)
#endif

// automatically generate float and integer type switch functions
#ifndef HAS_LDBL
#define AUTO_ALLTYPE_FUNC(funcName, dataType, returnType, ...) do { \
		switch (dataType) \
		{ \
		case Datatype::RealSingle: \
			return funcName<float>(__VA_ARGS__); \
		case Datatype::RealDouble: \
			return funcName<double>(__VA_ARGS__); \
		case Datatype::ComplexSingle: \
			return funcName<BlasSupp::complex<float>>(__VA_ARGS__); \
		case Datatype::ComplexDouble: \
			return funcName<BlasSupp::complex<double>>(__VA_ARGS__); \
		case Datatype::RealInt8: \
			return funcName<int8_t>(__VA_ARGS__); \
		case Datatype::RealInt16: \
			return funcName<int16_t>(__VA_ARGS__); \
		case Datatype::RealInt32: \
			return funcName<int32_t>(__VA_ARGS__); \
		case Datatype::RealInt64: \
			return funcName<int64_t>(__VA_ARGS__); \
		case Datatype::RealUInt8: \
			return funcName<uint8_t>(__VA_ARGS__); \
		case Datatype::RealUInt16: \
			return funcName<uint16_t>(__VA_ARGS__); \
		case Datatype::RealUInt32: \
			return funcName<uint32_t>(__VA_ARGS__); \
		case Datatype::RealUInt64: \
			return funcName<uint64_t>(__VA_ARGS__); \
		case Datatype::ComplexInt8: \
			return funcName<BlasSupp::complex<int8_t>>(__VA_ARGS__); \
		case Datatype::ComplexInt16: \
			return funcName<BlasSupp::complex<int16_t>>(__VA_ARGS__); \
		case Datatype::ComplexInt32: \
			return funcName<BlasSupp::complex<int32_t>>(__VA_ARGS__); \
		case Datatype::ComplexInt64: \
			return funcName<BlasSupp::complex<int64_t>>(__VA_ARGS__); \
		case Datatype::ComplexUInt8: \
			return funcName<BlasSupp::complex<uint8_t>>(__VA_ARGS__); \
		case Datatype::ComplexUInt16: \
			return funcName<BlasSupp::complex<uint16_t>>(__VA_ARGS__); \
		case Datatype::ComplexUInt32: \
			return funcName<BlasSupp::complex<uint32_t>>(__VA_ARGS__); \
		case Datatype::ComplexUInt64: \
			return funcName<BlasSupp::complex<uint64_t>>(__VA_ARGS__); \
		default: \
			UNSUPPORT(funcName, dataType, returnType); \
		} \
	} while (0)
#else
#define AUTO_ALLTYPE_FUNC(funcName, dataType, returnType, ...) do { \
		switch (dataType) \
		{ \
		case Datatype::RealSingle: \
			return funcName<float>(__VA_ARGS__); \
		case Datatype::RealDouble: \
			return funcName<double>(__VA_ARGS__); \
		case Datatype::RealLongDouble: \
			return funcName<long double>(__VA_ARGS__); \
		case Datatype::ComplexSingle: \
			return funcName<BlasSupp::complex<float>>(__VA_ARGS__); \
		case Datatype::ComplexDouble: \
			return funcName<BlasSupp::complex<double>>(__VA_ARGS__); \
		case Datatype::ComplexLongDouble: \
			return funcName<BlasSupp::complex<long double>>(__VA_ARGS__); \
		case Datatype::RealInt8: \
			return funcName<int8_t>(__VA_ARGS__); \
		case Datatype::RealInt16: \
			return funcName<int16_t>(__VA_ARGS__); \
		case Datatype::RealInt32: \
			return funcName<int32_t>(__VA_ARGS__); \
		case Datatype::RealInt64: \
			return funcName<int64_t>(__VA_ARGS__); \
		case Datatype::RealUInt8: \
			return funcName<uint8_t>(__VA_ARGS__); \
		case Datatype::RealUInt16: \
			return funcName<uint16_t>(__VA_ARGS__); \
		case Datatype::RealUInt32: \
			return funcName<uint32_t>(__VA_ARGS__); \
		case Datatype::RealUInt64: \
			return funcName<uint64_t>(__VA_ARGS__); \
		case Datatype::ComplexInt8: \
			return funcName<BlasSupp::complex<int8_t>>(__VA_ARGS__); \
		case Datatype::ComplexInt16: \
			return funcName<BlasSupp::complex<int16_t>>(__VA_ARGS__); \
		case Datatype::ComplexInt32: \
			return funcName<BlasSupp::complex<int32_t>>(__VA_ARGS__); \
		case Datatype::ComplexInt64: \
			return funcName<BlasSupp::complex<int64_t>>(__VA_ARGS__); \
		case Datatype::ComplexUInt8: \
			return funcName<BlasSupp::complex<uint8_t>>(__VA_ARGS__); \
		case Datatype::ComplexUInt16: \
			return funcName<BlasSupp::complex<uint16_t>>(__VA_ARGS__); \
		case Datatype::ComplexUInt32: \
			return funcName<BlasSupp::complex<uint32_t>>(__VA_ARGS__); \
		case Datatype::ComplexUInt64: \
			return funcName<BlasSupp::complex<uint64_t>>(__VA_ARGS__); \
		default: \
			UNSUPPORT(funcName, dataType, returnType); \
		} \
	} while (0)
#endif

// automatically generate float and integer complex type switch functions
#ifndef HAS_LDBL
#define AUTO_COMPLEX_TYPE_FUNC(funcName, dataType, returnType, ...) do { \
		switch (dataType) \
		{ \
		case Datatype::ComplexSingle: \
			return funcName<BlasSupp::complex<float>>(__VA_ARGS__); \
		case Datatype::ComplexDouble: \
			return funcName<BlasSupp::complex<double>>(__VA_ARGS__); \
		case Datatype::ComplexInt8: \
			return funcName<BlasSupp::complex<int8_t>>(__VA_ARGS__); \
		case Datatype::ComplexInt16: \
			return funcName<BlasSupp::complex<int16_t>>(__VA_ARGS__); \
		case Datatype::ComplexInt32: \
			return funcName<BlasSupp::complex<int32_t>>(__VA_ARGS__); \
		case Datatype::ComplexInt64: \
			return funcName<BlasSupp::complex<int64_t>>(__VA_ARGS__); \
		case Datatype::ComplexUInt8: \
			return funcName<BlasSupp::complex<uint8_t>>(__VA_ARGS__); \
		case Datatype::ComplexUInt16: \
			return funcName<BlasSupp::complex<uint16_t>>(__VA_ARGS__); \
		case Datatype::ComplexUInt32: \
			return funcName<BlasSupp::complex<uint32_t>>(__VA_ARGS__); \
		case Datatype::ComplexUInt64: \
			return funcName<BlasSupp::complex<uint64_t>>(__VA_ARGS__); \
		default: \
			UNSUPPORT(funcName, dataType, returnType); \
		} \
	} while (0)
#else
#define AUTO_ALLTYPE_FUNC(funcName, dataType, returnType, ...) do { \
		switch (dataType) \
		{ \
		case Datatype::ComplexSingle: \
			return funcName<BlasSupp::complex<float>>(__VA_ARGS__); \
		case Datatype::ComplexDouble: \
			return funcName<BlasSupp::complex<double>>(__VA_ARGS__); \
		case Datatype::ComplexLongDouble: \
			return funcName<BlasSupp::complex<long double>>(__VA_ARGS__); \
		case Datatype::ComplexInt8: \
			return funcName<BlasSupp::complex<int8_t>>(__VA_ARGS__); \
		case Datatype::ComplexInt16: \
			return funcName<BlasSupp::complex<int16_t>>(__VA_ARGS__); \
		case Datatype::ComplexInt32: \
			return funcName<BlasSupp::complex<int32_t>>(__VA_ARGS__); \
		case Datatype::ComplexInt64: \
			return funcName<BlasSupp::complex<int64_t>>(__VA_ARGS__); \
		case Datatype::ComplexUInt8: \
			return funcName<BlasSupp::complex<uint8_t>>(__VA_ARGS__); \
		case Datatype::ComplexUInt16: \
			return funcName<BlasSupp::complex<uint16_t>>(__VA_ARGS__); \
		case Datatype::ComplexUInt32: \
			return funcName<BlasSupp::complex<uint32_t>>(__VA_ARGS__); \
		case Datatype::ComplexUInt64: \
			return funcName<BlasSupp::complex<uint64_t>>(__VA_ARGS__); \
		default: \
			UNSUPPORT(funcName, dataType, returnType); \
		} \
	} while (0)
#endif

// automatically generate float and signed integer type switch functions
#ifndef HAS_LDBL
#define AUTO_SIGNED_TYPE_FUNC(funcName, dataType, returnType, ...) do { \
		switch (dataType) \
		{ \
		case Datatype::RealSingle: \
			return funcName<float>(__VA_ARGS__); \
		case Datatype::RealDouble: \
			return funcName<double>(__VA_ARGS__); \
		case Datatype::ComplexSingle: \
			return funcName<BlasSupp::complex<float>>(__VA_ARGS__); \
		case Datatype::ComplexDouble: \
			return funcName<BlasSupp::complex<double>>(__VA_ARGS__); \
		case Datatype::RealInt8: \
			return funcName<int8_t>(__VA_ARGS__); \
		case Datatype::RealInt16: \
			return funcName<int16_t>(__VA_ARGS__); \
		case Datatype::RealInt32: \
			return funcName<int32_t>(__VA_ARGS__); \
		case Datatype::RealInt64: \
			return funcName<int64_t>(__VA_ARGS__); \
		case Datatype::ComplexInt8: \
			return funcName<BlasSupp::complex<int8_t>>(__VA_ARGS__); \
		case Datatype::ComplexInt16: \
			return funcName<BlasSupp::complex<int16_t>>(__VA_ARGS__); \
		case Datatype::ComplexInt32: \
			return funcName<BlasSupp::complex<int32_t>>(__VA_ARGS__); \
		case Datatype::ComplexInt64: \
			return funcName<BlasSupp::complex<int64_t>>(__VA_ARGS__); \
		default: \
			UNSUPPORT(funcName, dataType, returnType); \
		} \
	} while (0)
#else
#define AUTO_ALL_SIGNED_TYPE_FUNC(funcName, dataType, returnType, ...) do { \
		switch (dataType) \
		{ \
		case Datatype::RealSingle: \
			return funcName<float>(__VA_ARGS__); \
		case Datatype::RealDouble: \
			return funcName<double>(__VA_ARGS__); \
		case Datatype::RealLongDouble: \
			return funcName<long double>(__VA_ARGS__); \
		case Datatype::ComplexSingle: \
			return funcName<BlasSupp::complex<float>>(__VA_ARGS__); \
		case Datatype::ComplexDouble: \
			return funcName<BlasSupp::complex<double>>(__VA_ARGS__); \
		case Datatype::ComplexLongDouble: \
			return funcName<BlasSupp::complex<long double>>(__VA_ARGS__); \
		case Datatype::RealInt8: \
			return funcName<int8_t>(__VA_ARGS__); \
		case Datatype::RealInt16: \
			return funcName<int16_t>(__VA_ARGS__); \
		case Datatype::RealInt32: \
			return funcName<int32_t>(__VA_ARGS__); \
		case Datatype::RealInt64: \
			return funcName<int64_t>(__VA_ARGS__); \
		case Datatype::ComplexInt8: \
			return funcName<BlasSupp::complex<int8_t>>(__VA_ARGS__); \
		case Datatype::ComplexInt16: \
			return funcName<BlasSupp::complex<int16_t>>(__VA_ARGS__); \
		case Datatype::ComplexInt32: \
			return funcName<BlasSupp::complex<int32_t>>(__VA_ARGS__); \
		case Datatype::ComplexInt64: \
			return funcName<BlasSupp::complex<int64_t>>(__VA_ARGS__); \
		default: \
			UNSUPPORT(funcName, dataType, returnType); \
		} \
	} while (0)
#endif
}