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
		public static T? TryParse(string str)
		{
			bool success = default(T).TryParse_Internal(str, out T result);
			return success ? result : null;
		}

		/// <summary>
		/// A in-fact <b>static</b> method to be implemented to indicate whether this type is a floating point type or a integral type
		/// </summary>
		/// <returns>The <see cref="DataTypeClassification"/> of <typeparamref name="T"/></returns>
		protected DataTypeClassification Classification_Internal();

		/// <summary>
		/// The <see cref="DataTypeClassification"/> of <typeparamref name="T"/>
		/// </summary>
		public static DataTypeClassification Classification => default(T).Classification_Internal();

		/// <summary>
		/// A in-fact <b>static</b> method to be implemented to get the machine precision of this type
		/// </summary>
		/// <returns>The machine precision of <typeparamref name="T"/></returns>
		protected double MachinePrecision_Internal();

		/// <summary>
		/// The machine precision of <typeparamref name="T"/>
		/// </summary>
		public static double MachinePrecision => default(T).MachinePrecision_Internal();

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

		DataTypeClassification ICustomNativeType<CustomTypeTest>.Classification_Internal() => 0;

		double ICustomNativeType<CustomTypeTest>.MachinePrecision_Internal() => 0;

		bool ICustomNativeType<CustomTypeTest>.TryParse_Internal(string str, out CustomTypeTest result) => throw new NotImplementedException();

		public bool Equals(CustomTypeTest other) => this.low == other.low && this.high == other.high;

		public override bool Equals(object? obj)
		{
			return obj is CustomTypeTest @double && this.Equals(@double);
		}

		public override int GetHashCode() => HashCode.Combine(low, high);

		public string ToString(string? format, IFormatProvider? formatProvider) => throw new NotImplementedException();
		public int CompareTo(CustomTypeTest other) => throw new NotImplementedException();

		public static CustomTypeTest operator +(CustomTypeTest left, CustomTypeTest right) => throw new NotImplementedException();
		public static CustomTypeTest operator -(CustomTypeTest left, CustomTypeTest right) => throw new NotImplementedException();
		public static CustomTypeTest operator *(CustomTypeTest left, CustomTypeTest right) => throw new NotImplementedException();
		public static CustomTypeTest operator /(CustomTypeTest left, CustomTypeTest right) => throw new NotImplementedException();
	}
	#endregion

	#region caching
	internal static class Cacher<T> where T : unmanaged
	{
		// not null return means T is a ICustomNativeType<T>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Type? MakeCustomNativeType(Type T)
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

		internal static readonly bool IsSupported;

		internal static readonly Func<string, T?>? ParseDelegate;

		internal static readonly bool? IsComplex;

		internal static readonly DataTypeClassification Classification;

		internal static readonly double? MachinePrecision;

		static Cacher()
		{
			IsSupported = GetIsSupported();
			ParseDelegate = GetParse();
			IsComplex = GetIsComplex();
			Classification = GetClassification();
			MachinePrecision = GetMachinePrecision();
		}

		private static bool GetIsSupported()
		{
			Type? custom = MakeCustomNativeType(typeof(T));
			return custom is not null;
		}

		private static Func<string, T?>? GetParse()
		{
			try
			{
				Type type = typeof(T);
				Type? custom = MakeCustomNativeType(type);
				var func = custom?.GetMethod(nameof(ICustomNativeType<CustomTypeTest>.TryParse), System.Reflection.BindingFlags.Static)?
								  .CreateDelegate<Func<string, T?>>();
				return func;
			}
			catch (Exception)
			{
				return null;
			}
		}

		private static bool? GetIsComplex()
		{
			Type type = typeof(T);
			if (MakeCustomNativeType(type) is null)
				return null;
			bool isComplex = type.GenericTypeArguments.Length == 1;
			try
			{
				isComplex = isComplex && typeof(IComplex<float>).MakeGenericType(type.GenericTypeArguments).IsAssignableFrom(type);
				return isComplex;
			}
			catch (Exception)
			{
				return null;
			}
		}

		private static DataTypeClassification GetClassification()
		{
			try
			{
				Type? custom = MakeCustomNativeType(typeof(T));
				var result = (DataTypeClassification?)custom?.GetProperty(nameof(ICustomNativeType<CustomTypeTest>.Classification))?.GetValue(null);
				return result ?? 0;
			}
			catch (Exception)
			{
				return 0;
			}
		}

		private static double? GetMachinePrecision()
		{
			try
			{
				Type? custom = MakeCustomNativeType(typeof(T));
				var result = (double?)custom?.GetProperty(nameof(ICustomNativeType<CustomTypeTest>.MachinePrecision))?.GetValue(null);
				return result;
			}
			catch (Exception)
			{
				return null;
			}
		}
	}
	#endregion

	#region extension methods
	/// <summary>
	/// A static class containing some extension methods for native types
	/// </summary>
	/// <remarks>Data type supported by this framework must be "native", i.e., it must be an <b>unmanaged</b> type which is either primitive or implementing <see cref="ICustomNativeType{T}"/>.</remarks>
	public static class NativeTypeExtension
	{
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
			return new ReadOnlySpan<byte>(&a, sizeof(T)).SequenceEqual(new ReadOnlySpan<byte>(&b, sizeof(T)));
			////byte* aa = (byte*)&a, bb = (byte*)&b;
			////int n = sizeof(T);
			////for (int i = 0; i < n; i++)
			////{
			////	if (aa[i] != bb[i])
			////		return false;
			////}
			////return true;
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
					float or double => double.Parse(str).FromDouble<T>(),
					// built-in integer types
					sbyte or short or int or long => long.Parse(str).FromLong<T>(),
					byte or ushort or uint or ulong => long.Parse(str).FromLong<T>(),
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
			var parseResult = Cacher<T>.ParseDelegate?.Invoke(str);
			if (parseResult == null)
			{
				result = default;
				return false;
			}
			else
			{
				result = parseResult.Value;
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
		/// <summary>
		/// Check whether <typeparamref name="T"/> is a complex data type.
		/// </summary>
		/// <typeparam name="T">The type to check</typeparam>
		/// <returns>True for complex type</returns>
		internal static bool IsComplex<T>() where T : unmanaged
		{
			return default(T) switch
			{
				// built-in float types
				float or double => false,
				// built-in integer types
				sbyte or short or int or long => false,
				byte or ushort or uint or ulong => false,
				// built-in complex float types
				IComplex<float> or IComplex<double> => true,
				// built-in complex integer types
				IComplex<sbyte> or IComplex<short> or IComplex<int> or IComplex<long> => true,
				IComplex<byte> or IComplex<ushort> or IComplex<int> or IComplex<long> => true,
				// otherwise
				_ => Cacher<T>.IsComplex ?? throw new NotSupportedException(Resources.Support.DataType),
			};
		}
		#endregion

		#region check whether native types are supported
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
				IComplex<float> or IComplex<double> => true,
				// built-in complex integer types
				IComplex<sbyte> or IComplex<short> or IComplex<int> or IComplex<long> => true,
				IComplex<byte> or IComplex<ushort> or IComplex<int> or IComplex<long> => true,
				// otherwise
				_ => Cacher<T>.IsSupported,
			};
		}

		/// <summary>
		/// Check whether <typeparamref name="T"/> is a supported data type.
		/// </summary>
		/// <typeparam name="T">The type to check</typeparam>
		/// <returns>true for supported type</returns>
		public static bool IsSupported<T>() where T : unmanaged => IsSupported(default(T));
		#endregion

		#region get classification native types
		/// <summary>
		/// Check whether <typeparamref name="T"/> is a floating point type or a integral type.
		/// </summary>
		/// <typeparam name="T">The type to check</typeparam>
		/// <returns>0 for not supported data type</returns>
		internal static DataTypeClassification GetClassification<T>() where T : unmanaged
		{
			return default(T) switch
			{
				// built-in float types
				float or double => DataTypeClassification.FloatPoint_IEEE754,
				// built-in integer types
				sbyte or short or int or long => DataTypeClassification.SignedInteger,
				byte or ushort or uint or ulong => DataTypeClassification.UnsignedInteger,
				// built-in complex float types
				IComplex<float> or IComplex<double> => DataTypeClassification.FloatPoint_IEEE754,
				// built-in complex integer types
				IComplex<sbyte> or IComplex<short> or IComplex<int> or IComplex<long> => DataTypeClassification.SignedInteger,
				IComplex<byte> or IComplex<ushort> or IComplex<int> or IComplex<long> => DataTypeClassification.FloatPoint_IEEE754,
				// otherwise
				_ => Cacher<T>.Classification,
			};
		}
		#endregion

		#region get data type machine precision
		/// <summary>
		/// Get the machine precision of <typeparamref name="T"/>
		/// </summary>
		/// <typeparam name="T">The type to check</typeparam>
		/// <returns>The machine precision of <typeparamref name="T"/></returns>
		internal static double GetMachinePrecision<T>() where T : unmanaged
		{
			return default(T) switch
			{
				// built-in float types
				float or IComplex<float> => 1.1920928955078125E-07,
				double or IComplex<double> => 2.220446049250313E-16D,
				// built-in integer types
				sbyte or short or int or long => 1,
				byte or ushort or uint or ulong => 1,
				// built-in complex integer types
				IComplex<sbyte> or IComplex<short> or IComplex<int> or IComplex<long> => 1,
				IComplex<byte> or IComplex<ushort> or IComplex<int> or IComplex<long> => 1,
				// otherwise
				_ => Cacher<T>.MachinePrecision ?? throw new NotSupportedException(Resources.Support.DataType),
			};
		}
		#endregion
	}
	#endregion
}
