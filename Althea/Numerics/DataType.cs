using System.Runtime.CompilerServices;


namespace Althea.Numerics
{
	#region data type enum
	/// <summary>
	/// The enumeration for number of sub-elements in one element
	/// </summary>
	[Flags]
	public enum DataTypeTuple : byte
	{
		/// <summary>
		/// Tuple size of 1, i.e. real numbers
		/// </summary>
		Real = 1 << 0,
		/// <summary>
		/// Tuple size of 2, i.e. complex numbers
		/// </summary>
		Complex = 1 << 1,
		/// <summary>
		/// Tuple size of 3, i.e. ternary numbers
		/// </summary>
		/// <remarks>NOT supported by most applications</remarks>
		Ternary = 1 << 2,
		/// <summary>
		/// Tuple size of 4, i.e. quaternions
		/// </summary>
		/// <remarks>NOT supported by most applications</remarks>
		Quaternion = 1 << 3,
	}

	/// <summary>
	/// The general classification of data types supported, values ≤ 0 are treaded as not supported
	/// </summary>
	[Flags]
	public enum DataTypeClassification : byte
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
		UnsignedInteger = 1 << 2,
		/// <summary>
		/// The decimal floating point numbers defined in the "IEEE Standard 754 for Decimal Floating-Point Arithmetic"
		/// </summary>
		/// <remarks>NOT supported by most applications</remarks>
		DecimalFloat_IEEE754 = 1 << 3,
	}

	/// <summary>
	/// The enumeration for size of a sub-element in bytes
	/// </summary>
	[Flags]
	public enum DataTypeSize : short
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
		Byte16 = 1 << 4,
	}

	/// <summary>
	/// The enumeration for general data types.
	/// </summary>
	[Flags]
	public enum DataType : int
	{
		/// <summary>
		/// <see cref="Half"/> = <see cref="DataTypeTuple.Real"/> + <see cref="DataTypeClassification.BinaryFloat_IEEE754"/> + <see cref="DataTypeSize.Byte2"/>
		/// </summary>
		RealFloat16 = DataTypeTuple.Real + (DataTypeClassification.BinaryFloat_IEEE754 << 8) + (DataTypeSize.Byte2 << 16),
		/// <summary>
		/// <see cref="float"/> = <see cref="DataTypeTuple.Real"/> + <see cref="DataTypeClassification.BinaryFloat_IEEE754"/> + <see cref="DataTypeSize.Byte4"/>
		/// </summary>
		RealFloat32 = DataTypeTuple.Real + (DataTypeClassification.BinaryFloat_IEEE754 << 8) + (DataTypeSize.Byte8 << 16),
		/// <summary>
		/// <see cref="double"/> = <see cref="DataTypeTuple.Real"/> + <see cref="DataTypeClassification.BinaryFloat_IEEE754"/> + <see cref="DataTypeSize.Byte8"/>
		/// </summary>
		RealFloat64 = DataTypeTuple.Real + (DataTypeClassification.BinaryFloat_IEEE754 << 8) + (DataTypeSize.Byte4 << 16),

		/// <summary>
		/// <see cref="Complex{T}"/> of <see cref="Half"/> = <see cref="DataTypeTuple.Real"/> + <see cref="DataTypeClassification.BinaryFloat_IEEE754"/> + <see cref="DataTypeSize.Byte2"/>
		/// </summary>
		ComplexHalf = DataTypeTuple.Complex + (DataTypeClassification.BinaryFloat_IEEE754 << 8) + (DataTypeSize.Byte2 << 16),
		/// <summary>
		/// <see cref="Complex{T}"/> of <see cref="float"/> = <see cref="DataTypeTuple.Real"/> + <see cref="DataTypeClassification.BinaryFloat_IEEE754"/> + <see cref="DataTypeSize.Byte4"/>
		/// </summary>
		ComplexSingle = DataTypeTuple.Complex + (DataTypeClassification.BinaryFloat_IEEE754 << 8) + (DataTypeSize.Byte4 << 16),
		/// <summary>
		/// <see cref="Complex{T}"/> of <see cref="double"/> = <see cref="DataTypeTuple.Real"/> + <see cref="DataTypeClassification.BinaryFloat_IEEE754"/> + <see cref="DataTypeSize.Byte8"/>
		/// </summary>
		ComplexDouble = DataTypeTuple.Complex + (DataTypeClassification.BinaryFloat_IEEE754 << 8) + (DataTypeSize.Byte8 << 16),

		/// <summary>
		/// <see cref="sbyte"/> = <see cref="DataTypeTuple.Real"/> + <see cref="DataTypeClassification.SignedInteger"/> + <see cref="DataTypeSize.Byte1"/>
		/// </summary>
		RealInt8 = DataTypeTuple.Real + (DataTypeClassification.SignedInteger << 8) + (DataTypeSize.Byte1 << 16),
		/// <summary>
		/// <see cref="short"/> = <see cref="DataTypeTuple.Real"/> + <see cref="DataTypeClassification.SignedInteger"/> + <see cref="DataTypeSize.Byte2"/>
		/// </summary>
		RealInt16 = DataTypeTuple.Real + (DataTypeClassification.SignedInteger << 8) + (DataTypeSize.Byte2 << 16),
		/// <summary>
		/// <see cref="int"/> = <see cref="DataTypeTuple.Real"/> + <see cref="DataTypeClassification.SignedInteger"/> + <see cref="DataTypeSize.Byte4"/>
		/// </summary>
		RealInt32 = DataTypeTuple.Real + (DataTypeClassification.SignedInteger << 8) + (DataTypeSize.Byte4 << 16),
		/// <summary>
		/// <see cref="long"/> = <see cref="DataTypeTuple.Real"/> + <see cref="DataTypeClassification.SignedInteger"/> + <see cref="DataTypeSize.Byte8"/>
		/// </summary>
		RealInt64 = DataTypeTuple.Real + (DataTypeClassification.SignedInteger << 8) + (DataTypeSize.Byte8 << 16),
		/// <summary>
		/// <see cref="System.Int128"/> = <see cref="DataTypeTuple.Real"/> + <see cref="DataTypeClassification.SignedInteger"/> + <see cref="DataTypeSize.Byte16"/>
		/// </summary>
		RealInt128 = DataTypeTuple.Real + (DataTypeClassification.SignedInteger << 8) + (DataTypeSize.Byte16 << 16),

		/// <summary>
		/// <see cref="byte"/> = <see cref="DataTypeTuple.Real"/> + <see cref="DataTypeClassification.UnsignedInteger"/> + <see cref="DataTypeSize.Byte1"/>
		/// </summary>
		RealUInt8 = DataTypeTuple.Real + (DataTypeClassification.UnsignedInteger << 8) + (DataTypeSize.Byte1 << 16),
		/// <summary>
		/// <see cref="ushort"/> = <see cref="DataTypeTuple.Real"/> + <see cref="DataTypeClassification.UnsignedInteger"/> + <see cref="DataTypeSize.Byte2"/>
		/// </summary>
		RealUInt16 = DataTypeTuple.Real + (DataTypeClassification.UnsignedInteger << 8) + (DataTypeSize.Byte2 << 16),
		/// <summary>
		/// <see cref="uint"/> = <see cref="DataTypeTuple.Real"/> + <see cref="DataTypeClassification.UnsignedInteger"/> + <see cref="DataTypeSize.Byte4"/>
		/// </summary>
		RealUInt32 = DataTypeTuple.Real + (DataTypeClassification.UnsignedInteger << 8) + (DataTypeSize.Byte4 << 16),
		/// <summary>
		/// <see cref="ulong"/> = <see cref="DataTypeTuple.Real"/> + <see cref="DataTypeClassification.UnsignedInteger"/> + <see cref="DataTypeSize.Byte8"/>
		/// </summary>
		RealUInt64 = DataTypeTuple.Real + (DataTypeClassification.UnsignedInteger << 8) + (DataTypeSize.Byte8 << 16),
		/// <summary>
		/// <see cref="System.UInt128"/> = <see cref="DataTypeTuple.Real"/> + <see cref="DataTypeClassification.UnsignedInteger"/> + <see cref="DataTypeSize.Byte16"/>
		/// </summary>
		RealUInt128 = DataTypeTuple.Real + (DataTypeClassification.UnsignedInteger << 8) + (DataTypeSize.Byte16 << 16),

		/// <summary>
		/// <see cref="ComplexInteger{T}"/> of <see cref="sbyte"/> = <see cref="DataTypeTuple.Complex"/> + <see cref="DataTypeClassification.SignedInteger"/> + <see cref="DataTypeSize.Byte1"/>
		/// </summary>
		ComplexInt8 = DataTypeTuple.Complex + (DataTypeClassification.SignedInteger << 8) + (DataTypeSize.Byte1 << 16),
		/// <summary>
		/// <see cref="ComplexInteger{T}"/> of <see cref="short"/> = <see cref="DataTypeTuple.Complex"/> + <see cref="DataTypeClassification.SignedInteger"/> + <see cref="DataTypeSize.Byte2"/>
		/// </summary>
		ComplexInt16 = DataTypeTuple.Complex + (DataTypeClassification.SignedInteger << 8) + (DataTypeSize.Byte2 << 16),
		/// <summary>
		/// <see cref="ComplexInteger{T}"/> of <see cref="int"/> = <see cref="DataTypeTuple.Complex"/> + <see cref="DataTypeClassification.SignedInteger"/> + <see cref="DataTypeSize.Byte4"/>
		/// </summary>
		ComplexInt32 = DataTypeTuple.Complex + (DataTypeClassification.SignedInteger << 8) + (DataTypeSize.Byte4 << 16),
		/// <summary>
		/// <see cref="ComplexInteger{T}"/> of <see cref="long"/> = <see cref="DataTypeTuple.Complex"/> + <see cref="DataTypeClassification.SignedInteger"/> + <see cref="DataTypeSize.Byte8"/>
		/// </summary>
		ComplexInt64 = DataTypeTuple.Complex + (DataTypeClassification.SignedInteger << 8) + (DataTypeSize.Byte8 << 16),
		/// <summary>
		/// <see cref="ComplexInteger{T}"/> of <see cref="System.Int128"/> = <see cref="DataTypeTuple.Complex"/> + <see cref="DataTypeClassification.SignedInteger"/> + <see cref="DataTypeSize.Byte16"/>
		/// </summary>
		ComplexInt128 = DataTypeTuple.Complex + (DataTypeClassification.SignedInteger << 8) + (DataTypeSize.Byte16 << 16),

		/// <summary>
		/// <see cref="ComplexInteger{T}"/> of <see cref="sbyte"/> = <see cref="DataTypeTuple.Complex"/> + <see cref="DataTypeClassification.UnsignedInteger"/> + <see cref="DataTypeSize.Byte1"/>
		/// </summary>
		ComplexUInt8 = DataTypeTuple.Complex + (DataTypeClassification.UnsignedInteger << 8) + (DataTypeSize.Byte1 << 16),
		/// <summary>
		/// <see cref="ComplexInteger{T}"/> of <see cref="short"/> = <see cref="DataTypeTuple.Complex"/> + <see cref="DataTypeClassification.UnsignedInteger"/> + <see cref="DataTypeSize.Byte2"/>
		/// </summary>
		ComplexUInt16 = DataTypeTuple.Complex + (DataTypeClassification.UnsignedInteger << 8) + (DataTypeSize.Byte2 << 16),
		/// <summary>
		/// <see cref="ComplexInteger{T}"/> of <see cref="int"/> = <see cref="DataTypeTuple.Complex"/> + <see cref="DataTypeClassification.UnsignedInteger"/> + <see cref="DataTypeSize.Byte4"/>
		/// </summary>
		ComplexUInt32 = DataTypeTuple.Complex + (DataTypeClassification.UnsignedInteger << 8) + (DataTypeSize.Byte4 << 16),
		/// <summary>
		/// <see cref="ComplexInteger{T}"/> of <see cref="long"/> = <see cref="DataTypeTuple.Complex"/> + <see cref="DataTypeClassification.UnsignedInteger"/> + <see cref="DataTypeSize.Byte8"/>
		/// </summary>
		ComplexUInt64 = DataTypeTuple.Complex + (DataTypeClassification.UnsignedInteger << 8) + (DataTypeSize.Byte8 << 16),
		/// <summary>
		/// <see cref="ComplexInteger{T}"/> of <see cref="System.UInt128"/> = <see cref="DataTypeTuple.Complex"/> + <see cref="DataTypeClassification.UnsignedInteger"/> + <see cref="DataTypeSize.Byte16"/>
		/// </summary>
		ComplexUInt128 = DataTypeTuple.Complex + (DataTypeClassification.UnsignedInteger << 8) + (DataTypeSize.Byte16 << 16),
	}
	#endregion


	#region extension methods
	/// <summary>
	/// The extension methods for <see cref="DataType"/>
	/// </summary>
	public static class DataTypeExtension
	{
		#region DataType extension
		/// <summary>
		/// Check whether the given <see cref="DataTypeClassification"/> has only one flag or not.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsAtomic(this DataTypeClassification type) => int.PopCount((int)type) == 1;

		/// <summary>
		/// Check whether the given <see cref="DataTypeTuple"/> has only one flag or not.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsAtomic(this DataTypeTuple type) => int.PopCount((int)type) == 1;

		/// <summary>
		/// Check whether the given <see cref="DataTypeSize"/> has only one flag or not.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsAtomic(this DataTypeSize type) => int.PopCount((int)type) == 1;

		/// <summary>
		/// Check whether the given <see cref="DataType"/> has only one flag or not.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsAtomic(this DataType type) => byte.PopCount((byte)type) == 1 && int.PopCount((short)type >> 8) == 1 && int.PopCount((int)type >> 16) == 1;

		/// <summary>
		/// Construct an atomic <see cref="DataType"/> from given parameters.
		/// </summary>
		/// <param name="complex">Whether the constructed <see cref="DataType"/> is a complex type</param>
		/// <param name="type">The <see cref="DataTypeClassification"/> the constructed <see cref="DataType"/> is a floating point type</param>
		/// <param name="size">The size in bytes of the constructed <see cref="DataType"/>; if <paramref name="complex"/> is true, this size shall be the <b>total</b> size of the complex struct in bytes</param>
		/// <returns>The constructed <see cref="DataType"/> or the default value if <paramref name="type"/> or <paramref name="size"/> is not supported</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static DataType MakeDataType(bool complex, DataTypeClassification type, int size)
		{
			if (type <= 0)
				return default;
			int sizeB = 1 << int.Log2(size);
			if (sizeB != size)
				return default;
			if (complex)
				return (DataType)((int)DataTypeTuple.Complex + ((int)type << 8) + sizeB << 16);
			else
				return (DataType)((int)DataTypeTuple.Real + ((int)type << 8) + sizeB << 16);
		}

		/// <summary>
		/// Construct a non-atomic <see cref="DataType"/> from given parameters.
		/// </summary>
		/// <returns>The constructed <see cref="DataType"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static DataType MakeDataType(DataTypeTuple tuple, DataTypeClassification type, DataTypeSize size) => (DataType)tuple + ((int)type << 8) + ((int)size << 16);

		/// <summary>
		/// Check if <paramref name="dataType"/> is a real type.
		/// </summary>
		/// <param name="dataType">The <see cref="DataType"/> to check</param>
		/// <returns>True if <paramref name="dataType"/> is a real type.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsReal(this DataType dataType) => (DataTypeTuple)dataType == DataTypeTuple.Real;

		/// <summary>
		/// Check if <paramref name="dataType"/> is an integer type.
		/// </summary>
		/// <param name="dataType">The <see cref="DataType"/> to check</param>
		/// <returns>True if <paramref name="dataType"/> is an integer type.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsInteger(this DataType dataType)
		{
			var c = (DataTypeClassification)((int)dataType >> 8);
			return c == DataTypeClassification.SignedInteger || c == DataTypeClassification.UnsignedInteger;
		}

		/// <summary>
		/// Check if <paramref name="dataType"/> is a signed integer type.
		/// </summary>
		/// <param name="dataType">The <see cref="DataType"/> to check</param>
		/// <returns>True if <paramref name="dataType"/> is a signed integer type.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsSignedInteger(this DataType dataType) => (DataTypeClassification)((int)dataType >> 8) == DataTypeClassification.SignedInteger;

		/// <summary>
		/// Check if <paramref name="dataType"/> is an unsigned integer type.
		/// </summary>
		/// <param name="dataType">The <see cref="DataType"/> to check</param>
		/// <returns>True if <paramref name="dataType"/> is an unsigned integer type.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsUnsignedInteger(this DataType dataType) => (DataTypeClassification)((int)dataType >> 8) == DataTypeClassification.UnsignedInteger;

		/// <summary>
		/// Get the corresponding real type of input <paramref name="type"/>
		/// </summary>
		/// <param name="type">input <see cref="DataType"/></param>
		/// <returns>the corresponding real type</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static DataType RealCorrespond(this DataType type) => (DataType)((((uint)type >> 8) << 8) + (uint)DataTypeTuple.Real);

		/// <summary>
		/// Get the corresponding complex type of input <paramref name="type"/>
		/// </summary>
		/// <param name="type">input <see cref="DataType"/></param>
		/// <returns>the corresponding complex type</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static DataType ComplexCorrespond(this DataType type) => (DataType)((((uint)type >> 8) << 8) + (uint)DataTypeTuple.Complex);

		/// <summary>
		/// Get the <see cref="DataTypeClassification"/> of <paramref name="dataType"/>.
		/// </summary>
		/// <param name="dataType">The <see cref="DataType"/> to get</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static DataTypeClassification Class(this DataType dataType) => (DataTypeClassification)((int)dataType >> 8);

		/// <summary>
		/// Get the <see cref="DataTypeTuple"/> of <paramref name="dataType"/>.
		/// </summary>
		/// <param name="dataType">The <see cref="DataType"/> to get</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static DataTypeTuple Tuple(this DataType dataType) => (DataTypeTuple)((((uint)dataType >> 8) << 8);

		/// <summary>
		/// Get the number of bytes (or real part's bytes if it is a complex type) of <paramref name="dataType"/>.
		/// </summary>
		/// <param name="dataType">The <see cref="DataType"/> to get</param>
		/// <returns>The number of bytes (or real part's bytes if it is a complex type) of <paramref name="dataType"/>.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Bytes(this DataType dataType) => (int)dataType >> 16;
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
				var t = typeof(IBaseNumber<>).MakeGenericType(type);
				if (!type.IsAssignableTo(t))
					throw new NotSupportedException();
				v = type.GetProperty(nameof(IBaseNumber<Float16>.Type), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)?.GetGetMethod()?.Invoke(null, null);
			}
			catch (Exception)
			{
				throw new NotSupportedException(Resources.ArithmeticError.DataTypeNotAllow);
			}
			if (v is not DataType d)
				throw new NotSupportedException(Resources.ArithmeticError.DataTypeNotAllow);
			return d;
		}
		#endregion
	}
	#endregion
}
