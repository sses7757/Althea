#pragma once
#include <string>
#include <stdint.h>

#include "complex.h"

namespace extblas
{
#pragma region operation enum
	namespace binaryOp
	{
		/// <summary>
		/// Binary operations used by array point-wise binary operations.
		/// </summary>
		enum BinaryOperation
		{
			/// <summary>
			/// Operation that returns the addition of two input parameters
			/// </summary>
			Add = -1,
			/// <summary>
			/// Operation that returns the multiplication of two input parameters
			/// </summary>
			Multiply = -2,
			/// <summary>
			/// Operation that returns the division of two input parameters
			/// </summary>
			Divide = -3,
			/// <summary>
			/// Operation that returns the power of the first input parameter to the second one
			/// </summary>
			Power = -4,
			/// <summary>
			/// Operation that returns the maximum of two input parameters
			/// </summary>
			Maximum = -5,
			/// <summary>
			/// Operation that returns the minimum of two input parameters
			/// </summary>
			Mininum = -6,
			/// <summary>
			/// Operation that returns the maximum of the absolute values two input parameters
			/// </summary>
			AbsoluteMaximum = -7,
			/// <summary>
			/// Operation that returns the minimum of the absolute values two input parameters
			/// </summary>
			AbsoluteMininum = -8,
			/// <summary>
			/// Operation that simply returns the second input parameter
			/// </summary>
			Fill = -9,
			/// <summary>
			/// Operation that returns 0 if the first input parameter's absolute value is smaller than the second one; otherwise, returns the first input parameter itself 
			/// </summary>
			Truncate = -10,
		};

	}

	namespace reduceOp
	{

		/// <summary>
		/// Binary reduce operations used by array point-wise reduce operations whose first input is the element in array and the second one is the partial reduction result.
		/// </summary>
		/// <remarks>All implementations shall support these pre-defined binary operations, but a implementation can add support for more binary operations.</remarks>
		enum ReduceOperation
		{
			/// <summary>
			/// Operation that returns the addition of two input parameters
			/// </summary>
			Add = -2,
			/// <summary>
			/// Operation that returns the addition of the absolute value of the first input parameter and the second parameter
			/// </summary>
			AddAbsolute = -3,
			/// <summary>
			/// Operation that returns the multiplication of two input parameters
			/// </summary>
			Multiply = -4,
			/// <summary>
			/// Operation that returns the multiplication of the absolute value of the first input parameter and the second parameter
			/// </summary>
			MultiplyAbsolute = -5,
			/// <summary>
			/// Operation that returns the addition of the square of the first input parameter and the second parameter; and sqrt the result before exit
			/// </summary>
			Norm = -6,
			/// <summary>
			/// Operation that returns the maximum of two input parameters
			/// </summary>
			Maximum = -7,
			/// <summary>
			/// Operation that returns the minimum of two input parameters
			/// </summary>
			Mininum = -8,
			/// <summary>
			/// Operation that returns the maximum of the absolute values two input parameters
			/// </summary>
			AbsoluteMaximum = -9,
			/// <summary>
			/// Operation that returns the minimum of the absolute values two input parameters
			/// </summary>
			AbsoluteMininum = -10,
		};
	}

	namespace unaryOp
	{
		/// <summary>
		/// Unitary operations of array point-wise unary operations.
		/// </summary>
		/// <remarks>All implementations shall support these pre-defined unary operations, but a implementation can add support for more unary operations.</remarks>
		enum UnaryOperation
		{
			/// <summary>
			/// Identity operator (i.e., elements are not changed)
			/// </summary>
			Identity = 0,
			/// <summary>
			/// Complex conjugate operator (real-typed elements are not changed)
			/// </summary>
			Conjugate = -1,
			/// <summary>
			/// Negation operator
			/// </summary>
			Negate = -2,
			/// <summary>
			/// Absolute operator
			/// </summary>
			AbsoluteValue = -3
		};
	}
#pragma endregion


#pragma region data type enum
	/// <summary>
	/// The enumeration for number of sub-elements in one element
	/// </summary>
	enum DataTypeTuple : uint8_t
	{
		/// <summary>
		/// Tuple size of 1, i.e. real numbers
		/// </summary>
		Real = 1 << 0,
		/// <summary>
		/// Tuple size of 2, i.e. complex numbers
		/// </summary>
		Complex = 1 << 1
	};

	/// <summary>
	/// The general classification of data types supported, values ¡Ü 0 are treaded as not supported
	/// </summary>
	enum DataTypeClassification : uint8_t
	{
		/// <summary>
		/// The binary floating point numbers defined in the "IEEE Standard 754 for Binary Floating-Point Arithmetic"
		/// </summary>
		BinaryFloat_IEEE754 = 1 << 0,
		/// <summary>
		/// The signed integer numbers
		/// </summary>
		SignedInteger = 1 << 1,
		/// <summary>
		/// The unsigned integer numbers
		/// </summary>
		UnsignedInteger = 1 << 2
	};

	/// <summary>
	/// The enumeration for size of a sub-element in bytes
	/// </summary>
	enum DataTypeSize : uint16_t
	{
		/// <summary>
		/// 1 byte
		/// </summary>
		Byte1 = 1 << 0,
		/// <summary>
		/// 2 byte
		/// </summary>
		Byte2 = 1 << 1,
		/// <summary>
		/// 4 byte
		/// </summary>
		Byte4 = 1 << 2,
		/// <summary>
		/// 8 byte
		/// </summary>
		Byte8 = 1 << 3,
		/// <summary>
		/// 16 byte
		/// </summary>
		Byte16 = 1 << 4
	};

	/// <summary>
	/// The enumeration for general data types.
	/// </summary>
	enum DataType : uint32_t
	{
		/// <summary>
		/// <see cref="Float16"/> = <see cref="DataTypeTuple::Real"/> + <see cref="DataTypeClassification::BinaryFloat_IEEE754"/> + <see cref="DataTypeSize::Byte2"/>
		/// </summary>
		RealFloat16 = DataTypeTuple::Real + (DataTypeClassification::BinaryFloat_IEEE754 << 8) + (DataTypeSize::Byte2 << 16),
		/// <summary>
		/// <see cref="float"/> = <see cref="DataTypeTuple::Real"/> + <see cref="DataTypeClassification::BinaryFloat_IEEE754"/> + <see cref="DataTypeSize::Byte4"/>
		/// </summary>
		RealFloat32 = DataTypeTuple::Real + (DataTypeClassification::BinaryFloat_IEEE754 << 8) + (DataTypeSize::Byte8 << 16),
		/// <summary>
		/// <see cref="double"/> = <see cref="DataTypeTuple::Real"/> + <see cref="DataTypeClassification::BinaryFloat_IEEE754"/> + <see cref="DataTypeSize::Byte8"/>
		/// </summary>
		RealFloat64 = DataTypeTuple::Real + (DataTypeClassification::BinaryFloat_IEEE754 << 8) + (DataTypeSize::Byte4 << 16),

		/// <summary>
		/// <see cref="Complex{T}"/> of <see cref="Float16"/> = <see cref="DataTypeTuple::Real"/> + <see cref="DataTypeClassification::BinaryFloat_IEEE754"/> + <see cref="DataTypeSize::Byte2"/>
		/// </summary>
		ComplexFloat16 = DataTypeTuple::Complex + (DataTypeClassification::BinaryFloat_IEEE754 << 8) + (DataTypeSize::Byte2 << 16),
		/// <summary>
		/// <see cref="Complex{T}"/> of <see cref="float"/> = <see cref="DataTypeTuple::Real"/> + <see cref="DataTypeClassification::BinaryFloat_IEEE754"/> + <see cref="DataTypeSize::Byte4"/>
		/// </summary>
		ComplexFloat32 = DataTypeTuple::Complex + (DataTypeClassification::BinaryFloat_IEEE754 << 8) + (DataTypeSize::Byte4 << 16),
		/// <summary>
		/// <see cref="Complex{T}"/> of <see cref="double"/> = <see cref="DataTypeTuple::Real"/> + <see cref="DataTypeClassification::BinaryFloat_IEEE754"/> + <see cref="DataTypeSize::Byte8"/>
		/// </summary>
		ComplexFloat64 = DataTypeTuple::Complex + (DataTypeClassification::BinaryFloat_IEEE754 << 8) + (DataTypeSize::Byte8 << 16),

		/// <summary>
		/// <see cref="sbyte"/> = <see cref="DataTypeTuple::Real"/> + <see cref="DataTypeClassification::SignedInteger"/> + <see cref="DataTypeSize::Byte1"/>
		/// </summary>
		RealInt8 = DataTypeTuple::Real + (DataTypeClassification::SignedInteger << 8) + (DataTypeSize::Byte1 << 16),
		/// <summary>
		/// <see cref="short"/> = <see cref="DataTypeTuple::Real"/> + <see cref="DataTypeClassification::SignedInteger"/> + <see cref="DataTypeSize::Byte2"/>
		/// </summary>
		RealInt16 = DataTypeTuple::Real + (DataTypeClassification::SignedInteger << 8) + (DataTypeSize::Byte2 << 16),
		/// <summary>
		/// <see cref="int"/> = <see cref="DataTypeTuple::Real"/> + <see cref="DataTypeClassification::SignedInteger"/> + <see cref="DataTypeSize::Byte4"/>
		/// </summary>
		RealInt32 = DataTypeTuple::Real + (DataTypeClassification::SignedInteger << 8) + (DataTypeSize::Byte4 << 16),
		/// <summary>
		/// <see cref="long"/> = <see cref="DataTypeTuple::Real"/> + <see cref="DataTypeClassification::SignedInteger"/> + <see cref="DataTypeSize::Byte8"/>
		/// </summary>
		RealInt64 = DataTypeTuple::Real + (DataTypeClassification::SignedInteger << 8) + (DataTypeSize::Byte8 << 16),
		/// <summary>
		/// <see cref="System::Int128"/> = <see cref="DataTypeTuple::Real"/> + <see cref="DataTypeClassification::SignedInteger"/> + <see cref="DataTypeSize::Byte16"/>
		/// </summary>
		RealInt128 = DataTypeTuple::Real + (DataTypeClassification::SignedInteger << 8) + (DataTypeSize::Byte16 << 16),

		/// <summary>
		/// <see cref="byte"/> = <see cref="DataTypeTuple::Real"/> + <see cref="DataTypeClassification::UnsignedInteger"/> + <see cref="DataTypeSize::Byte1"/>
		/// </summary>
		RealUInt8 = DataTypeTuple::Real + (DataTypeClassification::UnsignedInteger << 8) + (DataTypeSize::Byte1 << 16),
		/// <summary>
		/// <see cref="ushort"/> = <see cref="DataTypeTuple::Real"/> + <see cref="DataTypeClassification::UnsignedInteger"/> + <see cref="DataTypeSize::Byte2"/>
		/// </summary>
		RealUInt16 = DataTypeTuple::Real + (DataTypeClassification::UnsignedInteger << 8) + (DataTypeSize::Byte2 << 16),
		/// <summary>
		/// <see cref="uint"/> = <see cref="DataTypeTuple::Real"/> + <see cref="DataTypeClassification::UnsignedInteger"/> + <see cref="DataTypeSize::Byte4"/>
		/// </summary>
		RealUInt32 = DataTypeTuple::Real + (DataTypeClassification::UnsignedInteger << 8) + (DataTypeSize::Byte4 << 16),
		/// <summary>
		/// <see cref="ulong"/> = <see cref="DataTypeTuple::Real"/> + <see cref="DataTypeClassification::UnsignedInteger"/> + <see cref="DataTypeSize::Byte8"/>
		/// </summary>
		RealUInt64 = DataTypeTuple::Real + (DataTypeClassification::UnsignedInteger << 8) + (DataTypeSize::Byte8 << 16),
		/// <summary>
		/// <see cref="System::UInt128"/> = <see cref="DataTypeTuple::Real"/> + <see cref="DataTypeClassification::UnsignedInteger"/> + <see cref="DataTypeSize::Byte16"/>
		/// </summary>
		RealUInt128 = DataTypeTuple::Real + (DataTypeClassification::UnsignedInteger << 8) + (DataTypeSize::Byte16 << 16),

		/// <summary>
		/// <see cref="ComplexInteger{T}"/> of <see cref="sbyte"/> = <see cref="DataTypeTuple::Complex"/> + <see cref="DataTypeClassification::SignedInteger"/> + <see cref="DataTypeSize::Byte1"/>
		/// </summary>
		ComplexInt8 = DataTypeTuple::Complex + (DataTypeClassification::SignedInteger << 8) + (DataTypeSize::Byte1 << 16),
		/// <summary>
		/// <see cref="ComplexInteger{T}"/> of <see cref="short"/> = <see cref="DataTypeTuple::Complex"/> + <see cref="DataTypeClassification::SignedInteger"/> + <see cref="DataTypeSize::Byte2"/>
		/// </summary>
		ComplexInt16 = DataTypeTuple::Complex + (DataTypeClassification::SignedInteger << 8) + (DataTypeSize::Byte2 << 16),
		/// <summary>
		/// <see cref="ComplexInteger{T}"/> of <see cref="int"/> = <see cref="DataTypeTuple::Complex"/> + <see cref="DataTypeClassification::SignedInteger"/> + <see cref="DataTypeSize::Byte4"/>
		/// </summary>
		ComplexInt32 = DataTypeTuple::Complex + (DataTypeClassification::SignedInteger << 8) + (DataTypeSize::Byte4 << 16),
		/// <summary>
		/// <see cref="ComplexInteger{T}"/> of <see cref="long"/> = <see cref="DataTypeTuple::Complex"/> + <see cref="DataTypeClassification::SignedInteger"/> + <see cref="DataTypeSize::Byte8"/>
		/// </summary>
		ComplexInt64 = DataTypeTuple::Complex + (DataTypeClassification::SignedInteger << 8) + (DataTypeSize::Byte8 << 16),
		/// <summary>
		/// <see cref="ComplexInteger{T}"/> of <see cref="System::Int128"/> = <see cref="DataTypeTuple::Complex"/> + <see cref="DataTypeClassification::SignedInteger"/> + <see cref="DataTypeSize::Byte16"/>
		/// </summary>
		ComplexInt128 = DataTypeTuple::Complex + (DataTypeClassification::SignedInteger << 8) + (DataTypeSize::Byte16 << 16),

		/// <summary>
		/// <see cref="ComplexInteger{T}"/> of <see cref="sbyte"/> = <see cref="DataTypeTuple::Complex"/> + <see cref="DataTypeClassification::UnsignedInteger"/> + <see cref="DataTypeSize::Byte1"/>
		/// </summary>
		ComplexUInt8 = DataTypeTuple::Complex + (DataTypeClassification::UnsignedInteger << 8) + (DataTypeSize::Byte1 << 16),
		/// <summary>
		/// <see cref="ComplexInteger{T}"/> of <see cref="short"/> = <see cref="DataTypeTuple::Complex"/> + <see cref="DataTypeClassification::UnsignedInteger"/> + <see cref="DataTypeSize::Byte2"/>
		/// </summary>
		ComplexUInt16 = DataTypeTuple::Complex + (DataTypeClassification::UnsignedInteger << 8) + (DataTypeSize::Byte2 << 16),
		/// <summary>
		/// <see cref="ComplexInteger{T}"/> of <see cref="int"/> = <see cref="DataTypeTuple::Complex"/> + <see cref="DataTypeClassification::UnsignedInteger"/> + <see cref="DataTypeSize::Byte4"/>
		/// </summary>
		ComplexUInt32 = DataTypeTuple::Complex + (DataTypeClassification::UnsignedInteger << 8) + (DataTypeSize::Byte4 << 16),
		/// <summary>
		/// <see cref="ComplexInteger{T}"/> of <see cref="long"/> = <see cref="DataTypeTuple::Complex"/> + <see cref="DataTypeClassification::UnsignedInteger"/> + <see cref="DataTypeSize::Byte8"/>
		/// </summary>
		ComplexUInt64 = DataTypeTuple::Complex + (DataTypeClassification::UnsignedInteger << 8) + (DataTypeSize::Byte8 << 16),
		/// <summary>
		/// <see cref="ComplexInteger{T}"/> of <see cref="System::UInt128"/> = <see cref="DataTypeTuple::Complex"/> + <see cref="DataTypeClassification::UnsignedInteger"/> + <see cref="DataTypeSize::Byte16"/>
		/// </summary>
		ComplexUInt128 = DataTypeTuple::Complex + (DataTypeClassification::UnsignedInteger << 8) + (DataTypeSize::Byte16 << 16)
	};

	// functions

	// is a DataType a real type
	inline static constexpr bool is_real(const DataType type)
	{
		return (DataTypeTuple)type == DataTypeTuple::Real;
	}

	// size of a DataType
	inline static constexpr int size(const DataType type)
	{
		int size = (int)type >> 16;
		return is_real(type) ? size : size * 2;
	}

	// is a DataType a float type
	inline static constexpr bool is_float(const DataType type)
	{
		auto c = (DataTypeClassification)((int)type >> 8);
		return c == DataTypeClassification::BinaryFloat_IEEE754;
	}

	// is a DataType a integer type
	inline static constexpr bool is_integer(const DataType type)
	{
		return !is_float(type);
	}

	// generate a float type of given size
	inline static constexpr DataType makeFloatType(const int size, const bool complex)
	{
		if (complex)
			return (DataType)((int)DataTypeTuple::Complex + ((int)DataTypeClassification::BinaryFloat_IEEE754 << 8) + size << 16);
		else
			return (DataType)((int)DataTypeTuple::Real + ((int)DataTypeClassification::BinaryFloat_IEEE754 << 8) + size << 16);
	}

	// generate a real type of given complex type
	inline static constexpr DataType real_correspond(const DataType complex)
	{
		if (is_real(complex))
			return complex;
		constexpr int mask = 0b0001;
		return (DataType)(complex ^ mask);
	}

	// to string
	inline static std::string to_string(const DataType type)
	{
		std::string str(is_real(type) ? "Real" : "Complex");
		str = str + " Byte" + std::to_string(size(type)) + (is_float(type) ? "Float" : "Integer");
		return str;
	}
#pragma endregion


#pragma region automatically generate type switch functions
	// automatically generate float type switch functions
#define UNSUPPORT(funcName, dataType, returnType) do { \
		if constexpr (std::is_same<returnType, void>::value) \
		{ \
			printf("[%s] does not support [%s]!", #funcName, extblas::to_string(dataType).c_str()); \
			return returnType(); \
		} \
		else \
		{ \
			return returnType(-1); \
		} \
	} while(0)

#define AUTO_FLOAT_FUNC(funcName, dataType, returnType, ...) do { \
		switch (dataType) \
		{ \
		case DataType::RealFloat32: \
			return funcName<float>(__VA_ARGS__); \
		case DataType::RealFloat64: \
			return funcName<double>(__VA_ARGS__); \
		case DataType::ComplexFloat32: \
			return funcName<extblas::complex<float>>(__VA_ARGS__); \
		case DataType::ComplexFloat64: \
			return funcName<extblas::complex<double>>(__VA_ARGS__); \
		default: \
			UNSUPPORT(funcName, dataType, returnType); \
		} \
	} while (0)

// automatically generate float and integer type switch functions
#define AUTO_ALLTYPE_FUNC(funcName, dataType, returnType, ...) do { \
		switch (dataType) \
		{ \
		case DataType::RealFloat32: \
			return funcName<float>(__VA_ARGS__); \
		case DataType::RealFloat64: \
			return funcName<double>(__VA_ARGS__); \
		case DataType::ComplexFloat32: \
			return funcName<extblas::complex<float>>(__VA_ARGS__); \
		case DataType::ComplexFloat64: \
			return funcName<extblas::complex<double>>(__VA_ARGS__); \
		case DataType::RealInt8: \
			return funcName<int8_t>(__VA_ARGS__); \
		case DataType::RealInt16: \
			return funcName<int16_t>(__VA_ARGS__); \
		case DataType::RealInt32: \
			return funcName<int32_t>(__VA_ARGS__); \
		case DataType::RealInt64: \
			return funcName<int64_t>(__VA_ARGS__); \
		case DataType::RealUInt8: \
			return funcName<uint8_t>(__VA_ARGS__); \
		case DataType::RealUInt16: \
			return funcName<uint16_t>(__VA_ARGS__); \
		case DataType::RealUInt32: \
			return funcName<uint32_t>(__VA_ARGS__); \
		case DataType::RealUInt64: \
			return funcName<uint64_t>(__VA_ARGS__); \
		case DataType::ComplexInt8: \
			return funcName<extblas::complex<int8_t>>(__VA_ARGS__); \
		case DataType::ComplexInt16: \
			return funcName<extblas::complex<int16_t>>(__VA_ARGS__); \
		case DataType::ComplexInt32: \
			return funcName<extblas::complex<int32_t>>(__VA_ARGS__); \
		case DataType::ComplexInt64: \
			return funcName<extblas::complex<int64_t>>(__VA_ARGS__); \
		case DataType::ComplexUInt8: \
			return funcName<extblas::complex<uint8_t>>(__VA_ARGS__); \
		case DataType::ComplexUInt16: \
			return funcName<extblas::complex<uint16_t>>(__VA_ARGS__); \
		case DataType::ComplexUInt32: \
			return funcName<extblas::complex<uint32_t>>(__VA_ARGS__); \
		case DataType::ComplexUInt64: \
			return funcName<extblas::complex<uint64_t>>(__VA_ARGS__); \
		default: \
			UNSUPPORT(funcName, dataType, returnType); \
		} \
	} while (0)

#define AUTO_REALTYPE_FUNC(funcName, dataType, returnType, ...) do { \
		switch (dataType) \
		{ \
		case DataType::RealFloat32: \
			return funcName<float>(__VA_ARGS__); \
		case DataType::RealFloat64: \
			return funcName<double>(__VA_ARGS__); \
		case DataType::RealInt8: \
			return funcName<int8_t>(__VA_ARGS__); \
		case DataType::RealInt16: \
			return funcName<int16_t>(__VA_ARGS__); \
		case DataType::RealInt32: \
			return funcName<int32_t>(__VA_ARGS__); \
		case DataType::RealInt64: \
			return funcName<int64_t>(__VA_ARGS__); \
		case DataType::RealUInt8: \
			return funcName<uint8_t>(__VA_ARGS__); \
		case DataType::RealUInt16: \
			return funcName<uint16_t>(__VA_ARGS__); \
		case DataType::RealUInt32: \
			return funcName<uint32_t>(__VA_ARGS__); \
		case DataType::RealUInt64: \
			return funcName<uint64_t>(__VA_ARGS__); \
		default: \
			UNSUPPORT(funcName, dataType, returnType); \
		} \
	} while (0)

// automatically generate float and integer complex type switch functions
#define AUTO_COMPLEX_TYPE_FUNC(funcName, dataType, returnType, ...) do { \
		switch (dataType) \
		{ \
		case DataType::ComplexFloat32: \
			return funcName<extblas::complex<float>>(__VA_ARGS__); \
		case DataType::ComplexFloat64: \
			return funcName<extblas::complex<double>>(__VA_ARGS__); \
		case DataType::ComplexInt8: \
			return funcName<extblas::complex<int8_t>>(__VA_ARGS__); \
		case DataType::ComplexInt16: \
			return funcName<extblas::complex<int16_t>>(__VA_ARGS__); \
		case DataType::ComplexInt32: \
			return funcName<extblas::complex<int32_t>>(__VA_ARGS__); \
		case DataType::ComplexInt64: \
			return funcName<extblas::complex<int64_t>>(__VA_ARGS__); \
		case DataType::ComplexUInt8: \
			return funcName<extblas::complex<uint8_t>>(__VA_ARGS__); \
		case DataType::ComplexUInt16: \
			return funcName<extblas::complex<uint16_t>>(__VA_ARGS__); \
		case DataType::ComplexUInt32: \
			return funcName<extblas::complex<uint32_t>>(__VA_ARGS__); \
		case DataType::ComplexUInt64: \
			return funcName<extblas::complex<uint64_t>>(__VA_ARGS__); \
		default: \
			UNSUPPORT(funcName, dataType, returnType); \
		} \
	} while (0)

// automatically generate float and signed integer type switch functions
#define AUTO_SIGNED_TYPE_FUNC(funcName, dataType, returnType, ...) do { \
		switch (dataType) \
		{ \
		case DataType::RealFloat32: \
			return funcName<float>(__VA_ARGS__); \
		case DataType::RealFloat64: \
			return funcName<double>(__VA_ARGS__); \
		case DataType::ComplexFloat32: \
			return funcName<extblas::complex<float>>(__VA_ARGS__); \
		case DataType::ComplexFloat64: \
			return funcName<extblas::complex<double>>(__VA_ARGS__); \
		case DataType::RealInt8: \
			return funcName<int8_t>(__VA_ARGS__); \
		case DataType::RealInt16: \
			return funcName<int16_t>(__VA_ARGS__); \
		case DataType::RealInt32: \
			return funcName<int32_t>(__VA_ARGS__); \
		case DataType::RealInt64: \
			return funcName<int64_t>(__VA_ARGS__); \
		case DataType::ComplexInt8: \
			return funcName<extblas::complex<int8_t>>(__VA_ARGS__); \
		case DataType::ComplexInt16: \
			return funcName<extblas::complex<int16_t>>(__VA_ARGS__); \
		case DataType::ComplexInt32: \
			return funcName<extblas::complex<int32_t>>(__VA_ARGS__); \
		case DataType::ComplexInt64: \
			return funcName<extblas::complex<int64_t>>(__VA_ARGS__); \
		default: \
			UNSUPPORT(funcName, dataType, returnType); \
		} \
	} while (0)
#pragma endregion
}