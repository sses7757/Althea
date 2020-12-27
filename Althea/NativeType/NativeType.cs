using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;


namespace Althea.NativeType
{
	#region custom native type interface
	/// <summary>
	/// The interface for custom native types such as <c>long double</c> in C++ on some platforms.
	/// </summary>
	/// <typeparam name="T">the type of actual struct that implement this interface</typeparam>
	public interface ICustomNativeType<T> : IFormattable where T : unmanaged, ICustomNativeType<T>
	{
		/// <summary>
		/// A in-fact <b>static</b> method to be implemented to indicate whether this type is a floating point type or a integral type
		/// </summary>
		/// <returns>Whether this type is a floating point type or a integral type</returns>
		protected bool IsFloatPoint_Internal();

		/// <summary>
		/// A in-fact <b>static</b> method to be implemented to parse a string <paramref name="str"/> to <typeparamref name="T"/>
		/// </summary>
		/// <param name="str">the <see cref="string"/> to be parsed</param>
		/// <param name="result">the output result of type <typeparamref name="T"/></param>
		/// <returns>success or not</returns>
		protected bool TryParse_Internal(string str, out T result);

		/// <summary>
		/// A static method to be implemented to parse a string <paramref name="str"/> to <typeparamref name="T"/>
		/// </summary>
		/// <param name="str">the <see cref="string"/> to be parsed</param>
		/// <returns>the output result of type <typeparamref name="T"/>, null means unsuccessful parse</returns>
		public static object TryParse(string str)
		{
			bool success = default(T).TryParse_Internal(str, out T result);
			return success ? result : null;
		}

		/// <summary>
		/// Whether this type is a floating point type or a integral type
		/// </summary>
		public static bool FloatPoint => default(T).IsFloatPoint_Internal();

		/// <summary>
		/// The size of <typeparamref name="T"/> in bytes
		/// </summary>
		public static unsafe int SizeOfT => sizeof(T);

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
	[StructLayout(LayoutKind.Sequential, Size = 12)]
	internal struct CustomTypeTest : ICustomNativeType<CustomTypeTest>, IFormattable, IEquatable<CustomTypeTest>
	{
		private readonly double low;
		private readonly float high;

		bool ICustomNativeType<CustomTypeTest>.IsFloatPoint_Internal() => true;
		bool ICustomNativeType<CustomTypeTest>.TryParse_Internal(string str, out CustomTypeTest result) => throw new NotImplementedException();

		public bool Equals(CustomTypeTest other) => this.low == other.low && this.high == other.high;

		public override bool Equals(object obj)
		{
			return obj is CustomTypeTest @double && this.Equals(@double);
		}

		public override int GetHashCode() => HashCode.Combine(low, high);

		public CustomTypeTest Add(CustomTypeTest another) => throw new NotImplementedException();
		public CustomTypeTest Subtract(CustomTypeTest another) => throw new NotImplementedException();
		public CustomTypeTest Multiply(CustomTypeTest another) => throw new NotImplementedException();
		public CustomTypeTest Divide(CustomTypeTest another) => throw new NotImplementedException();

		public string ToString(string format, IFormatProvider formatProvider) => throw new NotImplementedException();
	}
	#endregion

	#region extension methods
	/// <summary>
	/// A static class containing some extension methods for native types
	/// </summary>
	public static class NativeTypeExtension
	{
		private static readonly Type _customNativeType = typeof(ICustomNativeType<CustomTypeTest>);

		private delegate object _parseFunc(string str);

		private static readonly Dictionary<Type, _parseFunc> _parseCache = new Dictionary<Type, _parseFunc>();

		/// <summary>
		/// Try to parse a <see cref="string"/> to a native type (including types that implements <see cref="ICustomNativeType{T}"/>)
		/// </summary>
		/// <typeparam name="T">the native type</typeparam>
		/// <param name="str">the <see cref="string"/> to parse</param>
		/// <param name="result">the output result</param>
		/// <returns>success or not</returns>
		public static bool TryParseNativeType<T>(this string str, out T result) where T : unmanaged
		{
			try
			{
				T? res = default(T) switch
				{
					// built-in float types
					float _ => (T)(dynamic)float.Parse(str),
					double _ => (T)(dynamic)double.Parse(str),
					// built-in integer types
					sbyte _ => (T)(dynamic)sbyte.Parse(str),
					short _ => (T)(dynamic)short.Parse(str),
					int _ => (T)(dynamic)int.Parse(str),
					long _ => (T)(dynamic)long.Parse(str),
					byte _ => (T)(dynamic)byte.Parse(str),
					ushort _ => (T)(dynamic)ushort.Parse(str),
					uint _ => (T)(dynamic)uint.Parse(str),
					ulong _ => (T)(dynamic)ulong.Parse(str),
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
				_parseFunc func;
				Type t = _customNativeType.MakeGenericType(type);
				if (t.IsAssignableFrom(type))
				{
					func = t.GetMethod(nameof(ICustomNativeType<CustomTypeTest>.TryParse)).CreateDelegate<_parseFunc>();
				}
				else
				{
					func = null;
				}
			}
			object parseResult = _parseCache[type]?.Invoke(str);
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


		private static readonly Dictionary<Type, bool?> _floatPointCache = new Dictionary<Type, bool?>();

		private static bool? CacheFloatPointOrIntegral(Type type)
		{
			if (type.IsEnum || type.IsPointer || type.IsPrimitive)
			{
				return null;
			}
			// cache
			if (!_floatPointCache.ContainsKey(type))
			{
				bool? result;
				Type t = _customNativeType.MakeGenericType(type);
				if (t.IsAssignableFrom(type))
				{
					result = (bool)t.GetProperty(nameof(ICustomNativeType<CustomTypeTest>.FloatPoint)).GetValue(null);
				}
				else
				{
					result = null;
				}
				_floatPointCache.Add(type, result);
				return result;
			}
			return _floatPointCache[type];
		}

		/// <summary>
		/// Check whether <paramref name="type"/> is a floating point type or a integral type.
		/// </summary>
		/// <param name="type">the type</param>
		/// <returns>null for none, true for floating point type, false for integral type</returns>
		public static bool? FloatPointOrIntegral(this Type type)
		{
			// built-in float types
			if (type == typeof(double))
				return true;
			else if (type == typeof(float))
				return true;
			// built-in integer types
			else if (type == typeof(int))
				return false;
			else if (type == typeof(long))
				return false;
			else if (type == typeof(sbyte))
				return false;
			else if (type == typeof(short))
				return false;
			else if (type == typeof(uint))
				return false;
			else if (type == typeof(ulong))
				return false;
			else if (type == typeof(byte))
				return false;
			else if (type == typeof(ushort))
				return false;
			// other primitive types are null
			return CacheFloatPointOrIntegral(type);
		}

		/// <summary>
		/// Check whether <typeparamref name="T"/> is a floating point type or a integral type.
		/// </summary>
		/// <typeparam name="T">the type to check</typeparam>
		/// <param name="value">an instance of <typeparamref name="T"/></param>
		/// <returns>null for none, true for floating point type, false for integral type</returns>
		public static bool? FloatPointOrIntegral<T>(this T value) where T : unmanaged
		{
			bool? result = value switch
			{
				// built-in float types
				float _ => true,
				double _ => true,
				// built-in integer types
				sbyte _ => false,
				short _ => false,
				int _ => false,
				long _ => false,
				byte _ => false,
				ushort _ => false,
				uint _ => false,
				ulong _ => false,
				// otherwise
				_ => null,
			};
			if (result.HasValue)
			{
				return result;
			}
			return CacheFloatPointOrIntegral(typeof(T));
		}

		/// <summary>
		/// Check whether <typeparamref name="T"/> is a floating point type or a integral type.
		/// </summary>
		/// <typeparam name="T">the type to check</typeparam>
		/// <returns>null for none, true for floating point type, false for integral type</returns>
		public static bool? FloatPointOrIntegral<T>() where T : unmanaged => FloatPointOrIntegral(default(T));
	}
	#endregion
}
