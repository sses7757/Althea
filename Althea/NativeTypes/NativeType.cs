using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;


namespace Althea.NativeTypes
{
	#region custom native type interface
	/// <summary>
	/// The interface for custom native types such as <c>long double</c> in C++ on some platforms.
	/// </summary>
	/// <typeparam name="T">The type of actual struct that implement this interface</typeparam>
	public interface ICustomNativeType<T> : IFormattable where T : unmanaged, ICustomNativeType<T>
	{
		/// <summary>
		/// A in-fact <b>static</b> method to be implemented to indicate whether this type is a floating point type or a integral type
		/// </summary>
		/// <returns>The <see cref="DataTypeClassification"/> of <typeparamref name="T"/></returns>
		protected DataTypeClassification Classification_Internal();

		/// <summary>
		/// A in-fact <b>static</b> method to be implemented to parse a string <paramref name="str"/> to <typeparamref name="T"/>
		/// </summary>
		/// <param name="str">The <see cref="string"/> to be parsed</param>
		/// <param name="result">The output result of type <typeparamref name="T"/></param>
		/// <returns>success or not</returns>
		protected bool TryParse_Internal(string str, out T result);

		/// <summary>
		/// A static method to be implemented to parse a string <paramref name="str"/> to <typeparamref name="T"/>
		/// </summary>
		/// <param name="str">The <see cref="string"/> to be parsed</param>
		/// <returns>the output result of type <typeparamref name="T"/>, null means unsuccessful parse</returns>
		public static object? TryParse(string str)
		{
			bool success = default(T).TryParse_Internal(str, out T result);
			return success ? result : null;
		}

		/// <summary>
		/// The <see cref="DataTypeClassification"/> of <typeparamref name="T"/>
		/// </summary>
		public static DataTypeClassification Classification => default(T).Classification_Internal();

		/// <summary>
		/// Out-of-place add <paramref name="another"/> value of <typeparamref name="T"/>
		/// </summary>
		/// <param name="another">another value to be added</param>
		/// <returns>The addition result</returns>
		T Add(T another);

		/// <summary>
		/// Out-of-place subtract <paramref name="another"/> value of <typeparamref name="T"/>
		/// </summary>
		/// <param name="another">another value to be subtracted</param>
		/// <returns>The subtraction result</returns>
		T Subtract(T another);

		/// <summary>
		/// Out-of-place multiply <paramref name="another"/> value of <typeparamref name="T"/>
		/// </summary>
		/// <param name="another">another value to be multiplied</param>
		/// <returns>The multiplication result</returns>
		T Multiply(T another);

		/// <summary>
		/// Out-of-place divide <paramref name="another"/> value of <typeparamref name="T"/>
		/// </summary>
		/// <param name="another">another value to be divided</param>
		/// <returns>The division result</returns>
		T Divide(T another);
	}
	#endregion

	#region example case
	/// <summary>
	/// This struct servers as an example of creating a new native type which will be supported by this framework such as <see cref="Complex{T}"/> and methods in <see cref="NativeTypeExtension"/>.<br/>
	/// This struct shall implement <see cref="ICustomNativeType{T}"/> of itself, <see cref="IFormattable"/>, <see cref="IEquatable{T}"/> of itself and possibly the <see cref="IComparable{T}"/> of itself. It shall also override the <see cref="ValueType.GetHashCode"/> and the arithmetic operators.
	/// </summary>
	/// <remarks><b>DO NOT</b> use this struct</remarks>
	[StructLayout(LayoutKind.Sequential)]
	struct CustomTypeTest : ICustomNativeType<CustomTypeTest>, IFormattable, IEquatable<CustomTypeTest>, IComparable<CustomTypeTest>
	{
		private readonly double low;
		private readonly float high;

		DataTypeClassification ICustomNativeType<CustomTypeTest>.Classification_Internal() => DataTypeClassification.FloatPoint_IEEE754;
		bool ICustomNativeType<CustomTypeTest>.TryParse_Internal(string str, out CustomTypeTest result) => throw new NotImplementedException();

		public bool Equals(CustomTypeTest other) => this.low == other.low && this.high == other.high;

		public override bool Equals(object? obj)
		{
			return obj is CustomTypeTest @double && this.Equals(@double);
		}

		public override int GetHashCode() => HashCode.Combine(low, high);

		public CustomTypeTest Add(CustomTypeTest another) => throw new NotImplementedException();
		public CustomTypeTest Subtract(CustomTypeTest another) => throw new NotImplementedException();
		public CustomTypeTest Multiply(CustomTypeTest another) => throw new NotImplementedException();
		public CustomTypeTest Divide(CustomTypeTest another) => throw new NotImplementedException();

		public string ToString(string? format, IFormatProvider? formatProvider) => throw new NotImplementedException();
		public int CompareTo(CustomTypeTest other) => throw new NotImplementedException();

		public static CustomTypeTest operator +(CustomTypeTest left, CustomTypeTest right) => throw new NotImplementedException();
		public static CustomTypeTest operator -(CustomTypeTest left, CustomTypeTest right) => throw new NotImplementedException();
		public static CustomTypeTest operator *(CustomTypeTest left, CustomTypeTest right) => throw new NotImplementedException();
		public static CustomTypeTest operator /(CustomTypeTest left, CustomTypeTest right) => throw new NotImplementedException();
	}
	#endregion

	#region extension methods
	/// <summary>
	/// A static class containing some extension methods for native types
	/// </summary>
	/// <remarks>Data type supported by this framework must be "native", i.e., it must be an <b>unmanaged</b> type which is either primitive or implementing <see cref="ICustomNativeType{T}"/>.</remarks>
	public static class NativeTypeExtension
	{
		#region internal
		// not null return means T is a ICustomNativeType<T>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static Type? MakeCustomNativeType(Type T)
		{
			try
			{
				return typeof(ICustomNativeType<CustomTypeTest>).MakeGenericType(T);
			}
			catch (Exception)
			{
				return null;
			}
		}
		#endregion

		#region predicator
		/// <summary>
		/// Check whether the two given generic-typed numbers are bit-wise equal
		/// </summary>
		/// <typeparam name="T">The supported data type</typeparam>
		/// <param name="a">The first input number</param>
		/// <param name="b">The second input number</param>
		/// <returns><c><paramref name="a"/> == <paramref name="b"/></c> or not</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static bool IsEqual<T>(this T a, T b) where T : unmanaged
		{
			byte* aa = (byte*)&a, bb = (byte*)&b;
			int n = sizeof(T);
			for (int i = 0; i < n; i++)
			{
				if (aa[i] != bb[i])
					return false;
			}
			return true;
		}

		/// <summary>
		/// Check whether the given generic-typed number bit-wisely equals to 0
		/// </summary>
		/// <typeparam name="T">The supported data type</typeparam>
		/// <param name="a">The input number to check</param>
		/// <returns><c><paramref name="a"/> == 0</c> or not</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsZero<T>(this T a) where T : unmanaged => IsEqual(a, default);

		/// <summary>
		/// Check whether the given generic-typed number bit-wisely equals to 1 (of type <typeparamref name="T"/>)
		/// </summary>
		/// <typeparam name="T">The supported data type</typeparam>
		/// <param name="a">The input number to check</param>
		/// <returns><c><paramref name="a"/> == 1</c> or not</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static bool IsOne<T>(this T a) where T : unmanaged => IsEqual(a, Const<T>.One);
		#endregion

		#region parse native type values
		private delegate object _parseFunc(string str);

		private static readonly Dictionary<Type, _parseFunc> _parseCache = new();

		/// <summary>
		/// Try to parse a <see cref="string"/> to a native type (including types that implements <see cref="ICustomNativeType{T}"/>)
		/// </summary>
		/// <typeparam name="T">The native type</typeparam>
		/// <param name="str">The <see cref="string"/> to parse</param>
		/// <param name="result">The output result</param>
		/// <returns>success or not</returns>
		public static bool TryParseNativeType<T>(this string str, out T result) where T : unmanaged
		{
			try
			{
				T? res = default(T) switch
				{
					// built-in float types
					float or double => (T)(dynamic)double.Parse(str),
					// built-in integer types
					sbyte or short or int or long => (T)(dynamic)long.Parse(str),
					byte or ushort or uint or ulong => (T)(dynamic)long.Parse(str),
					// otherwise
					_ => null,
				};
				if (res.HasValue)
				{
					result = res.Value;
					return true;
				}
			}
			catch (Exception)
			{
				result = default;
				return false;
			}
			// other case
			Type type = typeof(T);
			if (!_parseCache.ContainsKey(type))
			{
				Type? custom = MakeCustomNativeType(type);
				var func = custom?.GetMethod(nameof(ICustomNativeType<CustomTypeTest>.TryParse))?.CreateDelegate<_parseFunc>();
				if (func is null)
					throw new ArgumentException(string.Format(Resources.Other.CannotParseComplex, str, typeof(T).Name), nameof(str));
				_parseCache.Add(type, func);
			}
			object parseResult = _parseCache[type].Invoke(str);
			if (parseResult == null)
			{
				result = default;
				return false;
			}
			else
			{
				result = (T)parseResult;
				return true;
			}
		}
		#endregion

		#region check whether native types are integer types
		/// <summary>
		/// Check whether the given type <typeparamref name="T"/> is a real integral type or not
		/// </summary>
		/// <typeparam name="T">Any unmanaged type to check</typeparam>
		/// <returns>Whether <typeparamref name="T"/> is an integral type or not</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool IsIntegralType<T>() where T : unmanaged
		{
			var type = Const<T>.DataTypeClass;
			return !Const<T>.IsComplex && (type == DataTypeClassification.SignedInteger || type == DataTypeClassification.UnsignedInteger);
		}
		#endregion

		#region check whether native types are complex types
		private static readonly Dictionary<Type, bool> _complexCache = new();

		internal static bool IsComplexDirect(this Type type)
		{
			if (!type.IsValueType || type.IsEnum || type.IsPointer || type.IsPrimitive)
			{
				return false;
			}
			// cache
			if (!_complexCache.ContainsKey(type))
			{
				bool isComplex = type.GenericTypeArguments.Length == 1;
				try
				{
					isComplex = isComplex && typeof(IComplex<float>).MakeGenericType(type.GenericTypeArguments).IsAssignableFrom(type);
				}
				catch (Exception)
				{
					isComplex = false;
				}
				_complexCache.Add(type, isComplex);
			}
			return _complexCache[type];
		}

		/// <summary>
		/// Check whether <paramref name="type"/> is a complex data type.
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>true for complex type</returns>
		public static bool IsComplex(this Type type)
		{
			if (!type.IsValueType)
				return false;
			// built-in float types
			if (type == typeof(double) || type == typeof(float))
				return false;
			// built-in integer types
			else if (type == typeof(sbyte) || type == typeof(short) || type == typeof(int) || type == typeof(long))
				return false;
			else if (type == typeof(byte) || type == typeof(ushort) || type == typeof(int) || type == typeof(long))
				return false;
			// complex float types
			if (type == typeof(Complex<double>) || type == typeof(Complex<float>))
				return true;
			// complex integer types
			else if (type == typeof(Complex<sbyte>) || type == typeof(Complex<short>) || type == typeof(Complex<int>) || type == typeof(Complex<long>))
				return true;
			else if (type == typeof(Complex<byte>) || type == typeof(Complex<ushort>) || type == typeof(Complex<int>) || type == typeof(Complex<long>))
				return true;
			// other primitive types are null
			return IsComplexDirect(type);
		}

		/// <summary>
		/// Check whether <typeparamref name="T"/> is a complex data type.
		/// </summary>
		/// <typeparam name="T">The type to check</typeparam>
		/// <param name="value">an instance of <typeparamref name="T"/></param>
		/// <returns>true for complex type</returns>
		public static bool IsComplex<T>(this T value) where T : unmanaged
		{
			return value switch
			{
				// built-in float types
				float or double => false,
				// built-in integer types
				sbyte or short or int or long => false,
				byte or ushort or uint or ulong => false,
				// built-in complex float types
				Complex<float> or Complex<double> => true,
				// built-in complex integer types
				Complex<sbyte> or Complex<short> or Complex<int> or Complex<long> => true,
				Complex<byte> or Complex<ushort> or Complex<int> or Complex<long> => true,
				// otherwise
				_ => IsComplexDirect(typeof(T)),
			};
		}

		/// <summary>
		/// Check whether <typeparamref name="T"/> is a complex data type.
		/// </summary>
		/// <typeparam name="T">The type to check</typeparam>
		/// <returns>true for complex type</returns>
		public static bool IsComplex<T>() where T : unmanaged => IsComplex(default(T));
		#endregion

		#region check whether native types are supported
		private static readonly Dictionary<Type, bool> _supportCache = new();

		internal static bool IsSupportedDirect(this Type type)
		{
			if (!type.IsValueType || type.IsEnum || type.IsPointer || type.IsPrimitive)
			{
				return false;
			}
			// cache
			if (!_supportCache.ContainsKey(type))
			{
				Type? custom = MakeCustomNativeType(type);
				_supportCache.Add(type, custom is not null);
			}
			return _supportCache[type];
		}

		/// <summary>
		/// Check whether <paramref name="type"/> is a supported data type.
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>true for supported type</returns>
		public static bool IsSupported(this Type type)
		{
			if (!type.IsValueType)
				return false;
			// built-in float types
			if (type == typeof(double) || type == typeof(float))
				return true;
			// built-in integer types
			else if (type == typeof(sbyte) || type == typeof(short) || type == typeof(int) || type == typeof(long))
				return true;
			else if (type == typeof(byte) || type == typeof(ushort) || type == typeof(int) || type == typeof(long))
				return true;
			// complex float types
			if (type == typeof(Complex<double>) || type == typeof(Complex<float>))
				return true;
			// complex integer types
			else if (type == typeof(Complex<sbyte>) || type == typeof(Complex<short>) || type == typeof(Complex<int>) || type == typeof(Complex<long>))
				return true;
			else if (type == typeof(Complex<byte>) || type == typeof(Complex<ushort>) || type == typeof(Complex<int>) || type == typeof(Complex<long>))
				return true;
			// other primitive types are null
			return IsSupportedDirect(type);
		}

		/// <summary>
		/// Check whether <typeparamref name="T"/> is a supported data type.
		/// </summary>
		/// <typeparam name="T">The type to check</typeparam>
		/// <param name="value">an instance of <typeparamref name="T"/></param>
		/// <returns>true for supported type</returns>
		public static bool IsSupported<T>(this T value) where T : unmanaged
		{
			return value switch
			{
				// built-in float types
				float or double => true,
				// built-in integer types
				sbyte or short or int or long => true,
				byte or ushort or uint or ulong => true,
				// built-in complex float types
				Complex<float> or Complex<double> => true,
				// built-in complex integer types
				Complex<sbyte> or Complex<short> or Complex<int> or Complex<long> => true,
				Complex<byte> or Complex<ushort> or Complex<int> or Complex<long> => true,
				// otherwise
				_ => IsSupportedDirect(typeof(T)),
			};
		}

		/// <summary>
		/// Check whether <typeparamref name="T"/> is a supported data type.
		/// </summary>
		/// <typeparam name="T">The type to check</typeparam>
		/// <returns>true for supported type</returns>
		public static bool IsSupported<T>() where T : unmanaged => IsSupported(default(T));
		#endregion
	}
	#endregion
}
