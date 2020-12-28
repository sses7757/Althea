using System;


namespace Althea.NativeType
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
		/// The floating point
		/// </summary>
		FloatPoint = 1,
		/// <summary>
		/// The signed integer
		/// </summary>
		SignedInteger = 2,
		/// <summary>
		/// The unsigned integer
		/// </summary>
		UnsignedInteger = 3,
	}

	/// <summary>
	/// The general data types defined by flags and masks.
	/// </summary>
	public enum DataType
	{
		/// <summary>
		/// The right-most bit that represents the real base type, equals to zero, cannot be used separately.
		/// </summary>
		Real = 0,
		/// <summary>
		/// The right-most bit that represents the complex base type, cannot be used separately. If the value does not have this bit, it is a real type.
		/// </summary>
		Complex = 1 << 0,

		// actual types
		/// <summary>
		/// The float base type, cannot be used separately.
		/// </summary>
		TypeFloatPoint = DataTypeClassification.FloatPoint << DataTypeExtension.TypeMaskStart,
		/// <summary>
		/// The signed integer base type, cannot be used separately.
		/// </summary>
		TypeSignedInteger = DataTypeClassification.SignedInteger << DataTypeExtension.TypeMaskStart,
		/// <summary>
		/// The unsigned integer base type, cannot be used separately.
		/// </summary>
		TypeUnsignedInteger = DataTypeClassification.UnsignedInteger << DataTypeExtension.TypeMaskStart,

		// actual bytes
		/// <summary>
		/// The 1-byte base type, cannot be used separately.
		/// </summary>
		Byte1 = 1 << DataTypeExtension.ByteMaskStart,
		/// <summary>
		/// The 2-byte base type, cannot be used separately.
		/// </summary>
		Byte2 = 2 << DataTypeExtension.ByteMaskStart,
		/// <summary>
		/// The 4-byte base type, cannot be used separately.
		/// </summary>
		Byte4 = 4 << DataTypeExtension.ByteMaskStart,
		/// <summary>
		/// The 8-byte base type, cannot be used separately.
		/// </summary>
		Byte8 = 8 << DataTypeExtension.ByteMaskStart,

		// concrete types
		/// <summary>
		/// <see cref="float"/> = <see cref="Real"/> + <see cref="TypeFloatPoint"/> + <see cref="Byte4"/>
		/// </summary>
		RealSingle = Real | TypeFloatPoint | Byte4,
		/// <summary>
		/// <see cref="double"/> = <see cref="Real"/> + <see cref="TypeFloatPoint"/> + <see cref="Byte8"/>
		/// </summary>
		RealDouble = Real | TypeFloatPoint | Byte8,
		/// <summary>
		/// <see cref="Complex{Single}"/> = <see cref="Complex"/> + <see cref="TypeFloatPoint"/> + <see cref="Byte4"/>
		/// </summary>
		ComplexSingle = Complex | TypeFloatPoint | Byte4,
		/// <summary>
		/// <see cref="Complex{Double}"/> = <see cref="Complex"/> + <see cref="TypeFloatPoint"/> + <see cref="Byte8"/>
		/// </summary>
		ComplexDouble = Complex | TypeFloatPoint | Byte8,

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
		/// <see cref="Complex{SByte}"/> = <see cref="Complex"/> + <see cref="TypeSignedInteger"/> + <see cref="Byte1"/>
		/// </summary>
		ComplexInt8 = Complex | TypeSignedInteger | Byte1,
		/// <summary>
		/// <see cref="Complex{Int16}"/> = <see cref="Complex"/> + <see cref="TypeSignedInteger"/> + <see cref="Byte2"/>
		/// </summary>
		ComplexInt16 = Complex | TypeSignedInteger | Byte2,
		/// <summary>
		/// <see cref="Complex{Int32}"/> = <see cref="Complex"/> + <see cref="TypeSignedInteger"/> + <see cref="Byte4"/>
		/// </summary>
		ComplexInt32 = Complex | TypeSignedInteger | Byte4,
		/// <summary>
		/// <see cref="Complex{Int64}"/> = <see cref="Complex"/> + <see cref="TypeSignedInteger"/> + <see cref="Byte8"/>
		/// </summary>
		ComplexInt64 = Complex | TypeSignedInteger | Byte8,

		/// <summary>
		/// <see cref="Complex{Byte}"/> = <see cref="Complex"/> + <see cref="TypeUnsignedInteger"/> + <see cref="Byte1"/>
		/// </summary>
		ComplexUInt8 = Complex | TypeUnsignedInteger | Byte1,
		/// <summary>
		/// <see cref="Complex{UInt16}"/> = <see cref="Complex"/> + <see cref="TypeUnsignedInteger"/> + <see cref="Byte2"/>
		/// </summary>
		ComplexUInt16 = Complex | TypeUnsignedInteger | Byte2,
		/// <summary>
		/// <see cref="Complex{UInt32}"/> = <see cref="Complex"/> + <see cref="TypeUnsignedInteger"/> + <see cref="Byte4"/>
		/// </summary>
		ComplexUInt32 = Complex | TypeUnsignedInteger | Byte4,
		/// <summary>
		/// <see cref="Complex{UInt64}"/> = <see cref="Complex"/> + <see cref="TypeUnsignedInteger"/> + <see cref="Byte8"/>
		/// </summary>
		ComplexUInt64 = Complex | TypeUnsignedInteger | Byte8,
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
		/// <c>(value &amp; <see cref="TypeMask"/>) &gt;&gt; <see cref="TypeMaskStart"/> = </c> the actual data type used.<br/>
		/// See <see cref="DataType.TypeFloatPoint"/>, <see cref="DataType.TypeSignedInteger"/>, <see cref="DataType.TypeUnsignedInteger"/>.
		/// </summary>
		public const int TypeMask = 0b0110;
		/// <summary>
		/// The start bit of <see cref="TypeMask"/>.
		/// </summary>
		public const int TypeMaskStart = 1;

		/// <summary>
		/// The number of bytes mask (from 4th bit to 7th bit), cannot be used separately.<br/>
		/// <c>(value &amp; <see cref="ByteMask"/>) &gt;&gt; <see cref="ByteMaskStart"/> = </c> the bytes used (only half of a complex type's size shall be counted).
		/// </summary>
		public const int ByteMask = 0b1111_0000;
		/// <summary>
		/// The start bit of <see cref="ByteMask"/>.
		/// </summary>
		public const int ByteMaskStart = 4;

		/// <summary>
		/// Construct a <see cref="DataType"/> from given parameters
		/// </summary>
		/// <param name="complex">whether the constructed <see cref="DataType"/> is a complex type</param>
		/// <param name="type">the <see cref="DataTypeClassification"/> the constructed <see cref="DataType"/> is a floating point type</param>
		/// <param name="size">the size in bytes of the constructed <see cref="DataType"/></param>
		/// <returns>The constructed <see cref="DataType"/></returns>
		public static DataType MakeDataType(bool complex, DataTypeClassification type, int size)
		{
			return (complex ? DataType.Complex : DataType.Real) | (DataType)((int)type << TypeMaskStart) | (DataType)(size << ByteMaskStart);
		}

		/// <summary>
		/// Check if <paramref name="dataType"/> is a real type.
		/// </summary>
		/// <param name="dataType">the <see cref="DataType"/> to check</param>
		/// <returns>True if <paramref name="dataType"/> is a real type.</returns>
		public static bool IsReal(this DataType dataType) => (dataType & DataType.Complex) == 0;

		/// <summary>
		/// Check if <paramref name="dataType"/> is a float type.
		/// </summary>
		/// <param name="dataType">the <see cref="DataType"/> to check</param>
		/// <returns>True if <paramref name="dataType"/> is a float type.</returns>
		public static bool IsFloat(this DataType dataType) => ((int)dataType & TypeMask) == ((int)DataType.TypeFloatPoint >> TypeMaskStart);

		/// <summary>
		/// Check if <paramref name="dataType"/> is a signed integer type.
		/// </summary>
		/// <param name="dataType">the <see cref="DataType"/> to check</param>
		/// <returns>True if <paramref name="dataType"/> is a signed integer type.</returns>
		public static bool IsSignedInteger(this DataType dataType) => ((int)dataType & TypeMask) == ((int)DataType.TypeSignedInteger >> TypeMaskStart);

		/// <summary>
		/// Check if <paramref name="dataType"/> is an unsigned integer type.
		/// </summary>
		/// <param name="dataType">the <see cref="DataType"/> to check</param>
		/// <returns>True if <paramref name="dataType"/> is an unsigned integer type.</returns>
		public static bool IsUnsignedInteger(this DataType dataType) => ((int)dataType & TypeMask) == ((int)DataType.TypeUnsignedInteger >> TypeMaskStart);

		/// <summary>
		/// Get the number of bytes (or real part's bytes if it is a complex type) of <paramref name="dataType"/>.
		/// </summary>
		/// <param name="dataType">the <see cref="DataType"/> to get</param>
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
		/// <param name="dataType">the <see cref="DataType"/> to get string representation</param>
		/// <returns>The string representation of <paramref name="dataType"/></returns>
		public static string GetStringRepr(this DataType dataType)
		{
			return (dataType.IsReal() ? "Real" : "Complex") + " Byte" + dataType.Bytes() + (dataType.IsFloat() ? "Float" : "Integer");
		}


		/// <summary>
		/// Convert the <typeparamref name="T"/> to the <see cref="DataType"/>
		/// </summary>
		/// <typeparam name="T">the data type to convert</typeparam>
		/// <param name="value">a instance value of type <typeparamref name="T"/></param>
		/// <returns>the corresponding <see cref="DataType"/></returns>
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
				_ => !typeof(T).IsSupportedDirect() ? throw new NotSupportedException(Resource.DataTypeNotSupport)
						: MakeDataType(typeof(T).IsComplexDirect(), typeof(T).GetClassificationDirect(), sizeof(T)),
			};
		}

		/// <summary>
		/// Convert the <typeparamref name="T"/> to the <see cref="DataType"/>
		/// </summary>
		/// <typeparam name="T">the data type to convert</typeparam>
		/// <returns>the corresponding <see cref="DataType"/></returns>
		public static DataType ToDataType<T>() where T : unmanaged => default(T).ToDataType();
	}
	#endregion
}
