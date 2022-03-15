using System;
using System.Runtime.CompilerServices;


namespace Althea.NativeTypes
{
	#region data type enum
	/// <summary>
	/// The general classification of data types supported, values ≤ 0 are treaded as not supported
	/// </summary>
	public enum DataTypeClassification : short
	{
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
		// concrete types
		/// <summary>
		/// <see cref="Half"/> = <see cref="DataTypeExtension.Real"/> + <see cref="DataTypeExtension.TypeFloatPoint_IEEE754"/> + <see cref="DataTypeExtension.Byte2"/>
		/// </summary>
		RealHalf = DataTypeExtension.Real | DataTypeExtension.TypeFloatPoint_IEEE754 | DataTypeExtension.Byte2,
		/// <summary>
		/// <see cref="float"/> = <see cref="DataTypeExtension.Real"/> + <see cref="DataTypeExtension.TypeFloatPoint_IEEE754"/> + <see cref="DataTypeExtension.Byte4"/>
		/// </summary>
		RealSingle = DataTypeExtension.Real | DataTypeExtension.TypeFloatPoint_IEEE754 | DataTypeExtension.Byte4,
		/// <summary>
		/// <see cref="double"/> = <see cref="DataTypeExtension.Real"/> + <see cref="DataTypeExtension.TypeFloatPoint_IEEE754"/> + <see cref="DataTypeExtension.Byte8"/>
		/// </summary>
		RealDouble = DataTypeExtension.Real | DataTypeExtension.TypeFloatPoint_IEEE754 | DataTypeExtension.Byte8,
		/// <summary>
		/// <see cref="Complex{T}"/> of <see cref="Half"/> = <see cref="DataTypeExtension.Complex"/> + <see cref="DataTypeExtension.TypeFloatPoint_IEEE754"/> + <see cref="DataTypeExtension.Byte2"/>
		/// </summary>
		ComplexHalf = DataTypeExtension.Complex | DataTypeExtension.TypeFloatPoint_IEEE754 | DataTypeExtension.Byte2,
		/// <summary>
		/// <see cref="Complex{T}"/> of <see cref="float"/> = <see cref="DataTypeExtension.Complex"/> + <see cref="DataTypeExtension.TypeFloatPoint_IEEE754"/> + <see cref="DataTypeExtension.Byte4"/>
		/// </summary>
		ComplexSingle = DataTypeExtension.Complex | DataTypeExtension.TypeFloatPoint_IEEE754 | DataTypeExtension.Byte4,
		/// <summary>
		/// <see cref="Complex{T}"/> of <see cref="double"/> = <see cref="DataTypeExtension.Complex"/> + <see cref="DataTypeExtension.TypeFloatPoint_IEEE754"/> + <see cref="DataTypeExtension.Byte8"/>
		/// </summary>
		ComplexDouble = DataTypeExtension.Complex | DataTypeExtension.TypeFloatPoint_IEEE754 | DataTypeExtension.Byte8,

		/// <summary>
		/// <see cref="sbyte"/> = <see cref="DataTypeExtension.Real"/> + <see cref="DataTypeExtension.TypeSignedInteger"/> + <see cref="DataTypeExtension.Byte1"/>
		/// </summary>
		RealInt8 = DataTypeExtension.Real | DataTypeExtension.TypeSignedInteger | DataTypeExtension.Byte1,
		/// <summary>
		/// <see cref="short"/> = <see cref="DataTypeExtension.Real"/> + <see cref="DataTypeExtension.TypeSignedInteger"/> + <see cref="DataTypeExtension.Byte2"/>
		/// </summary>
		RealInt16 = DataTypeExtension.Real | DataTypeExtension.TypeSignedInteger | DataTypeExtension.Byte2,
		/// <summary>
		/// <see cref="int"/> = <see cref="DataTypeExtension.Real"/> + <see cref="DataTypeExtension.TypeSignedInteger"/> + <see cref="DataTypeExtension.Byte4"/>
		/// </summary>
		RealInt32 = DataTypeExtension.Real | DataTypeExtension.TypeSignedInteger | DataTypeExtension.Byte4,
		/// <summary>
		/// <see cref="long"/> = <see cref="DataTypeExtension.Real"/> + <see cref="DataTypeExtension.TypeSignedInteger"/> + <see cref="DataTypeExtension.Byte8"/>
		/// </summary>
		RealInt64 = DataTypeExtension.Real | DataTypeExtension.TypeSignedInteger | DataTypeExtension.Byte8,

		/// <summary>
		/// <see cref="byte"/> = <see cref="DataTypeExtension.Real"/> + <see cref="DataTypeExtension.TypeUnsignedInteger"/> + <see cref="DataTypeExtension.Byte1"/>
		/// </summary>
		RealUInt8 = DataTypeExtension.Real | DataTypeExtension.TypeUnsignedInteger | DataTypeExtension.Byte1,
		/// <summary>
		/// <see cref="ushort"/> = <see cref="DataTypeExtension.Real"/> + <see cref="DataTypeExtension.TypeUnsignedInteger"/> + <see cref="DataTypeExtension.Byte2"/>
		/// </summary>
		RealUInt16 = DataTypeExtension.Real | DataTypeExtension.TypeUnsignedInteger | DataTypeExtension.Byte2,
		/// <summary>
		/// <see cref="int"/> = <see cref="DataTypeExtension.Real"/> + <see cref="DataTypeExtension.TypeUnsignedInteger"/> + <see cref="DataTypeExtension.Byte4"/>
		/// </summary>
		RealUInt32 = DataTypeExtension.Real | DataTypeExtension.TypeUnsignedInteger | DataTypeExtension.Byte4,
		/// <summary>
		/// <see cref="long"/> = <see cref="DataTypeExtension.Real"/> + <see cref="DataTypeExtension.TypeUnsignedInteger"/> + <see cref="DataTypeExtension.Byte8"/>
		/// </summary>
		RealUInt64 = DataTypeExtension.Real | DataTypeExtension.TypeUnsignedInteger | DataTypeExtension.Byte8,

		/// <summary>
		/// <see cref="ComplexInteger{T}"/> of <see cref="sbyte"/>  = <see cref="DataTypeExtension.Complex"/> + <see cref="DataTypeExtension.TypeSignedInteger"/> + <see cref="DataTypeExtension.Byte1"/>
		/// </summary>
		ComplexInt8 = DataTypeExtension.Complex | DataTypeExtension.TypeSignedInteger | DataTypeExtension.Byte1,
		/// <summary>
		/// <see cref="ComplexInteger{T}"/> of <see cref="short"/> = <see cref="DataTypeExtension.Complex"/> + <see cref="DataTypeExtension.TypeSignedInteger"/> + <see cref="DataTypeExtension.Byte2"/>
		/// </summary>
		ComplexInt16 = DataTypeExtension.Complex | DataTypeExtension.TypeSignedInteger | DataTypeExtension.Byte2,
		/// <summary>
		/// <see cref="ComplexInteger{T}"/> of <see cref="int"/> = <see cref="DataTypeExtension.Complex"/> + <see cref="DataTypeExtension.TypeSignedInteger"/> + <see cref="DataTypeExtension.Byte4"/>
		/// </summary>
		ComplexInt32 = DataTypeExtension.Complex | DataTypeExtension.TypeSignedInteger | DataTypeExtension.Byte4,
		/// <summary>
		/// <see cref="ComplexInteger{T}"/> of <see cref="long"/> = <see cref="DataTypeExtension.Complex"/> + <see cref="DataTypeExtension.TypeSignedInteger"/> + <see cref="DataTypeExtension.Byte8"/>
		/// </summary>
		ComplexInt64 = DataTypeExtension.Complex | DataTypeExtension.TypeSignedInteger | DataTypeExtension.Byte8,

		/// <summary>
		/// <see cref="ComplexInteger{T}"/> of <see cref="byte"/> = <see cref="DataTypeExtension.Complex"/> + <see cref="DataTypeExtension.TypeUnsignedInteger"/> + <see cref="DataTypeExtension.Byte1"/>
		/// </summary>
		ComplexUInt8 = DataTypeExtension.Complex | DataTypeExtension.TypeUnsignedInteger | DataTypeExtension.Byte1,
		/// <summary>
		/// <see cref="ComplexInteger{T}"/> of <see cref="ushort"/> = <see cref="DataTypeExtension.Complex"/> + <see cref="DataTypeExtension.TypeUnsignedInteger"/> + <see cref="DataTypeExtension.Byte2"/>
		/// </summary>
		ComplexUInt16 = DataTypeExtension.Complex | DataTypeExtension.TypeUnsignedInteger | DataTypeExtension.Byte2,
		/// <summary>
		/// <see cref="ComplexInteger{T}"/> of <see cref="uint"/> = <see cref="DataTypeExtension.Complex"/> + <see cref="DataTypeExtension.TypeUnsignedInteger"/> + <see cref="DataTypeExtension.Byte4"/>
		/// </summary>
		ComplexUInt32 = DataTypeExtension.Complex | DataTypeExtension.TypeUnsignedInteger | DataTypeExtension.Byte4,
		/// <summary>
		/// <see cref="ComplexInteger{T}"/> of <see cref="ulong"/> = <see cref="DataTypeExtension.Complex"/> + <see cref="DataTypeExtension.TypeUnsignedInteger"/> + <see cref="DataTypeExtension.Byte8"/>
		/// </summary>
		ComplexUInt64 = DataTypeExtension.Complex | DataTypeExtension.TypeUnsignedInteger | DataTypeExtension.Byte8,
	}
	#endregion


	#region extension methods
	/// <summary>
	/// The extension methods for <see cref="DataType"/>
	/// </summary>
	public static class DataTypeExtension
	{
		#region constants
		/// <summary>
		/// The right-most bit that represents the real base type, equals to zero, cannot be used separately.
		/// </summary>
		public const int Real = 0;
		/// <summary>
		/// The right-most bit that represents the complex base type, cannot be used separately. If the value does not have this bit, it is a real type.
		/// </summary>
		public const int Complex = 1;

		/// <summary>
		/// The type mask (from 1st bit to 7th bit), cannot be used separately.<br/>
		/// <c>(value &amp; <see cref="TypeMask"/>) &gt;&gt; <see cref="TypeMaskStart"/> = </c> the actual data type classification as a <see cref="DataTypeClassification"/>.
		/// </summary>
		public const int TypeMask = 0b1111_1110;
		/// <summary>
		/// The start bit of <see cref="TypeMask"/>.
		/// </summary>
		public const int TypeMaskStart = 1;

		/// <summary>
		/// The number of bytes mask (from 8th bit to 15th bit), cannot be used separately.<br/>
		/// <c>(value &amp; <see cref="ByteMask"/>) &gt;&gt; <see cref="ByteMaskStart"/> = </c> the bytes used (only half of a complex type's size shall be counted).
		/// </summary>
		public const int ByteMask = 0b1111_1111_0000_0000;
		/// <summary>
		/// The start bit of <see cref="ByteMask"/>.
		/// </summary>
		public const int ByteMaskStart = 8;

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
		#endregion

		#region DataType extension
		/// <summary>
		/// Construct a <see cref="DataType"/> from given parameters
		/// </summary>
		/// <param name="complex">Whether the constructed <see cref="DataType"/> is a complex type</param>
		/// <param name="type">The <see cref="DataTypeClassification"/> the constructed <see cref="DataType"/> is a floating point type</param>
		/// <param name="size">The size in bytes of the constructed <see cref="DataType"/>; if <paramref name="complex"/> is true, this size shall be the <b>total</b> size of the complex struct in bytes</param>
		/// <returns>The constructed <see cref="DataType"/> or the default value if <paramref name="type"/> is not supported</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static DataType MakeDataType(bool complex, DataTypeClassification type, int size)
		{
			if (type <= 0)
				return default;
			if (complex)
				return (DataType)Complex | (DataType)((short)type << TypeMaskStart) | (DataType)((size / 2) << ByteMaskStart);
			else
				return (DataType)Real | (DataType)((short)type << TypeMaskStart) | (DataType)(size << ByteMaskStart);
		}

		/// <summary>
		/// Check if <paramref name="dataType"/> is a real type.
		/// </summary>
		/// <param name="dataType">The <see cref="DataType"/> to check</param>
		/// <returns>True if <paramref name="dataType"/> is a real type.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsReal(this DataType dataType) => ((int)dataType & Complex) == 0;

		/// <summary>
		/// Check if <paramref name="dataType"/> is an integer type.
		/// </summary>
		/// <param name="dataType">The <see cref="DataType"/> to check</param>
		/// <returns>True if <paramref name="dataType"/> is an integer type.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsInteger(this DataType dataType)
		{
			int t = ((int)dataType & TypeMask) << TypeMaskStart;
			return t == TypeSignedInteger || t == TypeUnsignedInteger;
		}

		/// <summary>
		/// Check if <paramref name="dataType"/> is a signed integer type.
		/// </summary>
		/// <param name="dataType">The <see cref="DataType"/> to check</param>
		/// <returns>True if <paramref name="dataType"/> is a signed integer type.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsSignedInteger(this DataType dataType) => ((int)dataType & TypeMask) == (TypeSignedInteger >> TypeMaskStart);

		/// <summary>
		/// Check if <paramref name="dataType"/> is an unsigned integer type.
		/// </summary>
		/// <param name="dataType">The <see cref="DataType"/> to check</param>
		/// <returns>True if <paramref name="dataType"/> is an unsigned integer type.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsUnsignedInteger(this DataType dataType) => ((int)dataType & TypeMask) == (TypeUnsignedInteger >> TypeMaskStart);

		/// <summary>
		/// Get the number of bytes (or real part's bytes if it is a complex type) of <paramref name="dataType"/>.
		/// </summary>
		/// <param name="dataType">The <see cref="DataType"/> to get</param>
		/// <returns>The number of bytes (or real part's bytes if it is a complex type) of <paramref name="dataType"/>.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Bytes(this DataType dataType) => ((int)dataType & ByteMask) >> ByteMaskStart;

		/// <summary>
		/// Get the corresponding real type of input <paramref name="type"/>
		/// </summary>
		/// <param name="type">input <see cref="DataType"/></param>
		/// <returns>the corresponding real type</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
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
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
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
			return (dataType.IsReal() ? "Real" : "Complex") + $" Byte-{dataType.Bytes()} " + (dataType.IsInteger() ? "Integer" : "Float");
		}
		#endregion

		#region to DataType
		/// <summary>
		/// Convert the <paramref name="type"/> to the <see cref="DataType"/>
		/// </summary>
		/// <param name="type">The <see cref="Type"/> to be converted</param>
		/// <returns>The corresponding <see cref="DataType"/> of  <paramref name="type"/></returns>
		/// <exception cref="NotSupportedException">If <paramref name="type"/> is not a supported data type</exception>
		public static DataType ToDataType(this Type type)
		{
			object? v;
			try
			{
				v = typeof(DataTypeExtension).GetMethod(nameof(ToDataType), 1,
														System.Reflection.BindingFlags.Static |
														System.Reflection.BindingFlags.NonPublic,
														null, Type.EmptyTypes, null)?
											 .MakeGenericMethod(type)?
											 .Invoke(null, null);
			}
			catch (Exception)
			{
				throw new NotSupportedException(Resources.Support.DataType);
			}
			if (v is not DataType d)
				throw new NotSupportedException(Resources.Support.DataType);
			return d;
		}

		/// <summary>
		/// Convert the <typeparamref name="T"/> to the <see cref="DataType"/>
		/// </summary>
		/// <typeparam name="T">The generic type to get its <see cref="DataType"/></typeparam>
		/// <returns>The corresponding <see cref="DataType"/> of <typeparamref name="T"/></returns>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not a supported data type</exception>
		internal static DataType ToDataType<T>() where T : unmanaged, INumber<T>
		{
			return default(T) switch
			{
				// built-in float types
				Half => DataType.RealHalf,
				float => DataType.RealSingle,
				double => DataType.RealDouble,
				// built-in integer types
				sbyte => DataType.RealInt8,
				short => DataType.RealInt16,
				int => DataType.RealInt32,
				long => DataType.RealInt64,
				byte => DataType.RealUInt8,
				ushort => DataType.RealUInt16,
				uint => DataType.RealUInt32,
				ulong => DataType.RealUInt64,
				// complex types
				Complex<Half> => DataType.ComplexHalf,
				Complex<float> => DataType.ComplexSingle,
				Complex<double> => DataType.ComplexDouble,
				ComplexInteger<sbyte> => DataType.ComplexInt8,
				ComplexInteger<short> => DataType.ComplexInt16,
				ComplexInteger<int> => DataType.ComplexInt32,
				ComplexInteger<long> => DataType.ComplexInt64,
				ComplexInteger<byte> => DataType.ComplexUInt8,
				ComplexInteger<ushort> => DataType.ComplexUInt16,
				ComplexInteger<uint> => DataType.ComplexUInt32,
				ComplexInteger<ulong> => DataType.ComplexUInt64,
				// otherwise
				_ => NumberType<T>.Classification == 0 ? throw new NotSupportedException(Resources.Support.DataType) : 
					MakeDataType(NumberType<T>.IsComplex, NumberType<T>.Classification, Unmanaged<T>.Size),
			};
		}
		#endregion
	}
	#endregion
}
