using System;


namespace Althea.NativeTypes
{
	#region data type enum
	/// <summary>
	/// The general classification of data types supported
	/// </summary>
	public enum DataTypeClassification
	{
		/// <summary>
		/// A not supported type
		/// </summary>
		NotSupported = 0,
		/// <summary>
		/// The floating point numbers defined in the "IEEE Standard 754 for Binary Floating-Point Arithmetic"
		/// </summary>
		FloatPoint_IEEE754 = 1,
		/// <summary>
		/// The signed integer numbers
		/// </summary>
		SignedInteger = 2,
		/// <summary>
		/// The unsigned integer numbers
		/// </summary>
		UnsignedInteger = 3,
	}

	/// <summary>
	/// The general data types defined by flags and masks.
	/// </summary>
	public enum DataType : int
	{
		/// <summary>
		/// The right-most bit that represents the real base type, equals to zero, cannot be used separately.
		/// </summary>
		Real = 0,
		/// <summary>
		/// The right-most bit that represents the complex base type, cannot be used separately. If the value does not have this bit, it is a real type.
		/// </summary>
		Complex = 1 << 0,

		// concrete types
		/// <summary>
		/// <see cref="float"/> = <see cref="Real"/> + <see cref="DataTypeExtension.TypeFloatPoint_IEEE754"/> + <see cref="DataTypeExtension.Byte4"/>
		/// </summary>
		RealSingle = Real | DataTypeExtension.TypeFloatPoint_IEEE754 | DataTypeExtension.Byte4,
		/// <summary>
		/// <see cref="double"/> = <see cref="Real"/> + <see cref="DataTypeExtension.TypeFloatPoint_IEEE754"/> + <see cref="DataTypeExtension.Byte8"/>
		/// </summary>
		RealDouble = Real | DataTypeExtension.TypeFloatPoint_IEEE754 | DataTypeExtension.Byte8,
		/// <summary>
		/// <see cref="Complex{Single}"/> = <see cref="Complex"/> + <see cref="DataTypeExtension.TypeFloatPoint_IEEE754"/> + <see cref="DataTypeExtension.Byte4"/>
		/// </summary>
		ComplexSingle = Complex | DataTypeExtension.TypeFloatPoint_IEEE754 | DataTypeExtension.Byte4,
		/// <summary>
		/// <see cref="Complex{Double}"/> = <see cref="Complex"/> + <see cref="DataTypeExtension.TypeFloatPoint_IEEE754"/> + <see cref="DataTypeExtension.Byte8"/>
		/// </summary>
		ComplexDouble = Complex | DataTypeExtension.TypeFloatPoint_IEEE754 | DataTypeExtension.Byte8,

		/// <summary>
		/// <see cref="sbyte"/> = <see cref="Real"/> + <see cref="DataTypeExtension.TypeSignedInteger"/> + <see cref="DataTypeExtension.Byte1"/>
		/// </summary>
		RealInt8 = Real | DataTypeExtension.TypeSignedInteger | DataTypeExtension.Byte1,
		/// <summary>
		/// <see cref="short"/> = <see cref="Real"/> + <see cref="DataTypeExtension.TypeSignedInteger"/> + <see cref="DataTypeExtension.Byte2"/>
		/// </summary>
		RealInt16 = Real | DataTypeExtension.TypeSignedInteger | DataTypeExtension.Byte2,
		/// <summary>
		/// <see cref="int"/> = <see cref="Real"/> + <see cref="DataTypeExtension.TypeSignedInteger"/> + <see cref="DataTypeExtension.Byte4"/>
		/// </summary>
		RealInt32 = Real | DataTypeExtension.TypeSignedInteger | DataTypeExtension.Byte4,
		/// <summary>
		/// <see cref="long"/> = <see cref="Real"/> + <see cref="DataTypeExtension.TypeSignedInteger"/> + <see cref="DataTypeExtension.Byte8"/>
		/// </summary>
		RealInt64 = Real | DataTypeExtension.TypeSignedInteger | DataTypeExtension.Byte8,

		/// <summary>
		/// <see cref="byte"/> = <see cref="Real"/> + <see cref="DataTypeExtension.TypeUnsignedInteger"/> + <see cref="DataTypeExtension.Byte1"/>
		/// </summary>
		RealUInt8 = Real | DataTypeExtension.TypeUnsignedInteger | DataTypeExtension.Byte1,
		/// <summary>
		/// <see cref="ushort"/> = <see cref="Real"/> + <see cref="DataTypeExtension.TypeUnsignedInteger"/> + <see cref="DataTypeExtension.Byte2"/>
		/// </summary>
		RealUInt16 = Real | DataTypeExtension.TypeUnsignedInteger | DataTypeExtension.Byte2,
		/// <summary>
		/// <see cref="int"/> = <see cref="Real"/> + <see cref="DataTypeExtension.TypeUnsignedInteger"/> + <see cref="DataTypeExtension.Byte4"/>
		/// </summary>
		RealUInt32 = Real | DataTypeExtension.TypeUnsignedInteger | DataTypeExtension.Byte4,
		/// <summary>
		/// <see cref="long"/> = <see cref="Real"/> + <see cref="DataTypeExtension.TypeUnsignedInteger"/> + <see cref="DataTypeExtension.Byte8"/>
		/// </summary>
		RealUInt64 = Real | DataTypeExtension.TypeUnsignedInteger | DataTypeExtension.Byte8,

		/// <summary>
		/// <see cref="Complex{SByte}"/> = <see cref="Complex"/> + <see cref="DataTypeExtension.TypeSignedInteger"/> + <see cref="DataTypeExtension.Byte1"/>
		/// </summary>
		ComplexInt8 = Complex | DataTypeExtension.TypeSignedInteger | DataTypeExtension.Byte1,
		/// <summary>
		/// <see cref="Complex{Int16}"/> = <see cref="Complex"/> + <see cref="DataTypeExtension.TypeSignedInteger"/> + <see cref="DataTypeExtension.Byte2"/>
		/// </summary>
		ComplexInt16 = Complex | DataTypeExtension.TypeSignedInteger | DataTypeExtension.Byte2,
		/// <summary>
		/// <see cref="Complex{Int32}"/> = <see cref="Complex"/> + <see cref="DataTypeExtension.TypeSignedInteger"/> + <see cref="DataTypeExtension.Byte4"/>
		/// </summary>
		ComplexInt32 = Complex | DataTypeExtension.TypeSignedInteger | DataTypeExtension.Byte4,
		/// <summary>
		/// <see cref="Complex{Int64}"/> = <see cref="Complex"/> + <see cref="DataTypeExtension.TypeSignedInteger"/> + <see cref="DataTypeExtension.Byte8"/>
		/// </summary>
		ComplexInt64 = Complex | DataTypeExtension.TypeSignedInteger | DataTypeExtension.Byte8,

		/// <summary>
		/// <see cref="Complex{Byte}"/> = <see cref="Complex"/> + <see cref="DataTypeExtension.TypeUnsignedInteger"/> + <see cref="DataTypeExtension.Byte1"/>
		/// </summary>
		ComplexUInt8 = Complex | DataTypeExtension.TypeUnsignedInteger | DataTypeExtension.Byte1,
		/// <summary>
		/// <see cref="Complex{UInt16}"/> = <see cref="Complex"/> + <see cref="DataTypeExtension.TypeUnsignedInteger"/> + <see cref="DataTypeExtension.Byte2"/>
		/// </summary>
		ComplexUInt16 = Complex | DataTypeExtension.TypeUnsignedInteger | DataTypeExtension.Byte2,
		/// <summary>
		/// <see cref="Complex{UInt32}"/> = <see cref="Complex"/> + <see cref="DataTypeExtension.TypeUnsignedInteger"/> + <see cref="DataTypeExtension.Byte4"/>
		/// </summary>
		ComplexUInt32 = Complex | DataTypeExtension.TypeUnsignedInteger | DataTypeExtension.Byte4,
		/// <summary>
		/// <see cref="Complex{UInt64}"/> = <see cref="Complex"/> + <see cref="DataTypeExtension.TypeUnsignedInteger"/> + <see cref="DataTypeExtension.Byte8"/>
		/// </summary>
		ComplexUInt64 = Complex | DataTypeExtension.TypeUnsignedInteger | DataTypeExtension.Byte8,
	}
	#endregion


	#region extension methods
	/// <summary>
	/// The extension methods for <see cref="DataType"/>
	/// </summary>
	public static class DataTypeExtension
	{
		/// <summary>
		/// The type mask (from 1st bit to 2nd bit), cannot be used separately.<br/>
		/// <c>(value &amp; <see cref="TypeMask"/>) &gt;&gt; <see cref="TypeMaskStart"/> = </c> the actual data type classification as a <see cref="DataTypeClassification"/>.
		/// </summary>
		public const int TypeMask = 0b1111_1110;
		/// <summary>
		/// The start bit of <see cref="TypeMask"/>.
		/// </summary>
		public const int TypeMaskStart = 1;

		/// <summary>
		/// The number of bytes mask (from 4th bit to 7th bit), cannot be used separately.<br/>
		/// <c>(value &amp; <see cref="ByteMask"/>) &gt;&gt; <see cref="ByteMaskStart"/> = </c> the bytes used (only half of a complex type's size shall be counted).
		/// </summary>
		public const int ByteMask = 0b1111_1111_0000_0000;
		/// <summary>
		/// The start bit of <see cref="ByteMask"/>.
		/// </summary>
		public const int ByteMaskStart = 4;

		/// <summary>
		/// The float base type; cannot be used separately.
		/// </summary>
		public const int TypeFloatPoint_IEEE754 = (int)DataTypeClassification.FloatPoint_IEEE754 << TypeMaskStart;
		/// <summary>
		/// The signed integer base type; cannot be used separately.
		/// </summary>
		public const int TypeSignedInteger = (int)DataTypeClassification.SignedInteger << TypeMaskStart;
		/// <summary>
		/// The unsigned integer base type; cannot be used separately.
		/// </summary>
		public const int TypeUnsignedInteger = (int)DataTypeClassification.UnsignedInteger << TypeMaskStart;

		/// <summary>
		/// The 1-byte base type; cannot be used separately.
		/// </summary>
		public const int Byte1 = 1 << ByteMaskStart;
		/// <summary>
		/// The 2-byte base type; cannot be used separately.
		/// </summary>
		public const int Byte2 = 2 << ByteMaskStart;
		/// <summary>
		/// The 4-byte base type; cannot be used separately.
		/// </summary>
		public const int Byte4 = 4 << ByteMaskStart;
		/// <summary>
		/// The 8-byte base type; cannot be used separately.
		/// </summary>
		public const int Byte8 = 8 << ByteMaskStart;


		/// <summary>
		/// Construct a <see cref="DataType"/> from given parameters
		/// </summary>
		/// <param name="complex">Whether the constructed <see cref="DataType"/> is a complex type</param>
		/// <param name="type">The <see cref="DataTypeClassification"/> the constructed <see cref="DataType"/> is a floating point type</param>
		/// <param name="size">The size in bytes of the constructed <see cref="DataType"/>; if <paramref name="complex"/> is true, this size shall be the <b>total</b> size of the complex struct in bytes</param>
		/// <returns>The constructed <see cref="DataType"/> or the default value if <paramref name="type"/> is <see cref="DataTypeClassification.NotSupported"/></returns>
		public static DataType MakeDataType(bool complex, DataTypeClassification type, int size)
		{
			if (type == DataTypeClassification.NotSupported)
				return default;
			if (complex)
				return DataType.Complex | (DataType)((int)type << TypeMaskStart) | (DataType)((size / 2) << ByteMaskStart);
			else
				return DataType.Real | (DataType)((int)type << TypeMaskStart) | (DataType)(size << ByteMaskStart);
		}

		/// <summary>
		/// Check if <paramref name="dataType"/> is a real type.
		/// </summary>
		/// <param name="dataType">The <see cref="DataType"/> to check</param>
		/// <returns>True if <paramref name="dataType"/> is a real type.</returns>
		public static bool IsReal(this DataType dataType) => (dataType & DataType.Complex) == 0;

		/// <summary>
		/// Check if <paramref name="dataType"/> is a float type.
		/// </summary>
		/// <param name="dataType">The <see cref="DataType"/> to check</param>
		/// <returns>True if <paramref name="dataType"/> is a float type.</returns>
		public static bool IsFloat(this DataType dataType) => ((int)dataType & TypeMask) == (TypeFloatPoint_IEEE754 >> TypeMaskStart);

		/// <summary>
		/// Check if <paramref name="dataType"/> is a signed integer type.
		/// </summary>
		/// <param name="dataType">The <see cref="DataType"/> to check</param>
		/// <returns>True if <paramref name="dataType"/> is a signed integer type.</returns>
		public static bool IsSignedInteger(this DataType dataType) => ((int)dataType & TypeMask) == (TypeSignedInteger >> TypeMaskStart);

		/// <summary>
		/// Check if <paramref name="dataType"/> is an unsigned integer type.
		/// </summary>
		/// <param name="dataType">The <see cref="DataType"/> to check</param>
		/// <returns>True if <paramref name="dataType"/> is an unsigned integer type.</returns>
		public static bool IsUnsignedInteger(this DataType dataType) => ((int)dataType & TypeMask) == (TypeUnsignedInteger >> TypeMaskStart);

		/// <summary>
		/// Get the number of bytes (or real part's bytes if it is a complex type) of <paramref name="dataType"/>.
		/// </summary>
		/// <param name="dataType">The <see cref="DataType"/> to get</param>
		/// <returns>The number of bytes (or real part's bytes if it is a complex type) of <paramref name="dataType"/>.</returns>
		public static int Bytes(this DataType dataType) => ((int)dataType & ByteMask) >> ByteMaskStart;

		/// <summary>
		/// Get the corresponding real type of input <paramref name="type"/>
		/// </summary>
		/// <param name="type">input <see cref="DataType"/></param>
		/// <returns>the corresponding real type</returns>
		public static DataType RealCorrespond(this DataType type)
		{
			if (type.IsReal())
				return type;
			const int mask = 0b0001;
			return (DataType)((int)type ^ mask);
		}

		/// <summary>
		/// Get the corresponding complex type of input <paramref name="type"/>
		/// </summary>
		/// <param name="type">input <see cref="DataType"/></param>
		/// <returns>the corresponding complex type</returns>
		public static DataType ComplexCorrespond(this DataType type)
		{
			if (!type.IsReal())
				return type;
			const int mask = 0b0001;
			return (DataType)((int)type ^ mask);
		}

		/// <summary>
		/// Get the string representation of a given <see cref="DataType"/>
		/// </summary>
		/// <param name="dataType">The <see cref="DataType"/> to get string representation</param>
		/// <returns>The string representation of <paramref name="dataType"/></returns>
		public static string GetStringRepr(this DataType dataType)
		{
			return (dataType.IsReal() ? "Real" : "Complex") + $" Byte-{dataType.Bytes()} " + (dataType.IsFloat() ? "Float" : "Integer");
		}


		/// <summary>
		/// Convert the <typeparamref name="T"/> to the <see cref="DataType"/>
		/// </summary>
		/// <typeparam name="T">The generic type to get its <see cref="DataType"/></typeparam>
		/// <param name="value">An instance value of type <typeparamref name="T"/></param>
		/// <returns>The corresponding <see cref="DataType"/> of <typeparamref name="T"/></returns>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not a supported data type</exception>
		public unsafe static DataType ToDataType<T>(this T value) where T : unmanaged
		{
			return value switch
			{
				// built-in float types
				float _ => DataType.RealSingle,
				double _ => DataType.RealDouble,
				// built-in integer types
				int _ => DataType.RealInt32,
				long _ => DataType.RealInt64,
				sbyte _ => DataType.RealInt8,
				short _ => DataType.RealInt16,
				uint _ => DataType.RealUInt32,
				ulong _ => DataType.RealUInt64,
				byte _ => DataType.RealUInt8,
				ushort _ => DataType.RealUInt16,
				// complex types
				Complex<float> _ => DataType.ComplexSingle,
				Complex<double> _ => DataType.ComplexDouble,
				Complex<sbyte> _ => DataType.ComplexInt8,
				Complex<short> _ => DataType.ComplexInt16,
				Complex<int> _ => DataType.ComplexInt32,
				Complex<long> _ => DataType.ComplexInt64,
				Complex<byte> _ => DataType.ComplexUInt8,
				Complex<ushort> _ => DataType.ComplexUInt16,
				Complex<uint> _ => DataType.ComplexUInt32,
				Complex<ulong> _ => DataType.ComplexUInt64,
				// otherwise
				_ => !typeof(T).IsSupportedDirect() ? throw new NotSupportedException(Resources.Support.DataType)
						: MakeDataType(typeof(T).IsComplexDirect(), typeof(T).GetClassificationDirect(), sizeof(T)),
			};
		}

		/// <summary>
		/// Convert the <typeparamref name="T"/> to the <see cref="DataType"/>
		/// </summary>
		/// <typeparam name="T">The data type to convert</typeparam>
		/// <returns>the corresponding <see cref="DataType"/></returns>
		public static DataType ToDataType<T>() where T : unmanaged => default(T).ToDataType();
	}
	#endregion
}
