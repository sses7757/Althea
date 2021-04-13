using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Althea.Helpers;


namespace Althea.NativeTypes
{
	#region half float arithmetics
	internal static class HalfFloatExtension
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe bool IsNegative(this Half a)
		{
			return (*(short*)&a) < 0;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe sbyte ExpPart(this Half a)
		{
			return (sbyte)((*(ushort*)&a & 0x7C00) >> 10);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe ushort FracPart(this Half a)
		{
			return (ushort)(*(ushort*)&a & 0x3FFu);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe Half FromParts(ushort frac, sbyte exp, bool neg)
		{
			var v = (ushort)(((neg ? 1 : 0) << 15) + (exp << 10) + frac);
			return *(Half*)&v;
		}

		private static readonly Half One = FromParts(0, BIAS, neg: false);

		private const byte BITS_MANTISSA = 10;
		private const byte BITS_EXPONENT = 5;

		private const sbyte MAX_EXPONENT_VALUE = 31; // 2^5 - 1
		private const sbyte BIAS = MAX_EXPONENT_VALUE / 2;

		private const sbyte MAX_EXPONENT = BIAS;
		private const sbyte MIN_EXPONENT = -BIAS;

		/// <summary>
		/// Unary negate the given <see cref="Half"/> float value <paramref name="v"/>
		/// </summary>
		/// <param name="v">The given <see cref="Half"/> float value</param>
		/// <returns><c>-<paramref name="v"/></c></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe Half Negate(this Half v)
		{
			ushort vv = (ushort)(*(ushort*)&v ^ 0x8000u);
			return *(Half*)&vv;
		}

		/// <summary>
		/// Unary reciprocate the given <see cref="Half"/> float value <paramref name="v"/>
		/// </summary>
		/// <param name="v">The given <see cref="Half"/> float value</param>
		/// <returns><c>1 / <paramref name="v"/></c></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Half Reciprocal(this Half v)
		{
			return (Half)(1.0f / (float)v);
		}

		/// <summary>
		/// Get the square root of the given <see cref="Half"/> float value <paramref name="v"/>
		/// </summary>
		/// <param name="v">The given <see cref="Half"/> float value</param>
		/// <returns><c>√<paramref name="v"/></c></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Half Sqrt(this Half v)
		{
			return (Half)MathF.Sqrt((float)v);
		}

		/// <summary>
		/// Unary increase the given <see cref="Half"/> float value <paramref name="v"/> by 1
		/// </summary>
		/// <param name="v">The given <see cref="Half"/> float value</param>
		/// <returns><c>1 + <paramref name="v"/></c></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Half Inc(this Half v)
		{
			return Add(v, One);
		}

		/// <summary>
		/// Binary add the given <see cref="Half"/> float value <paramref name="a"/> and <paramref name="b"/>
		/// </summary>
		/// <param name="a">The first given <see cref="Half"/> float value</param>
		/// <param name="b">The second given <see cref="Half"/> float value</param>
		/// <returns><c><paramref name="a"/> + <paramref name="b"/></c></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe Half Add(this Half a, Half b)
		{
#if DEBUG
			sbyte expA = a.ExpPart(), expB = b.ExpPart();
			ushort fracA = a.FracPart(), fracB = b.FracPart();
			bool negA = a.IsNegative(), negB = b.IsNegative();
			// abnormal input
			if (expA == MAX_EXPONENT_VALUE)
			{
				// if a of the components is NaN the result becomes NaN, too.
				if (0 != fracA || Half.IsNaN(b))
					return Half.NaN;
				// otherwise this must be infinity
				return negA == negB ? Half.PositiveInfinity : Half.NegativeInfinity;
			}
			else if (expB == MAX_EXPONENT_VALUE)
			{
				if (Half.IsNaN(a) || 0 != fracB)
					return Half.NaN;
				return negA == negB ? Half.PositiveInfinity : Half.NegativeInfinity;
			}

			// normal input
			bool resultNeg;
			int m1, m2, temp;
			// compute the difference between the two exponents, shifts with negative numbers are undefined, thus we need two code paths
			int expDiff = expA - expB;
			if (0 == expDiff)
			{
				// the exponents are equal, thus we must just add the hidden bit
				temp = expB;

				if (0 == expA)
					m1 = fracA;
				else
					m1 = ((int)fracA) | (1 << BITS_MANTISSA);

				if (0 == expB)
					m2 = fracB;
				else
					m2 = ((int)fracB) | (1 << BITS_MANTISSA);
			}
			else
			{
				if (expDiff < 0)
				{
					expDiff = -expDiff;
					expA = b.ExpPart(); expB = a.ExpPart();
					fracA = b.FracPart(); fracB = a.FracPart();
					negA = b.IsNegative(); negB = a.IsNegative();
				}

				m1 = ((int)fracA) | (1 << BITS_MANTISSA);

				if (0 == expB)
					m2 = fracB;
				else
					m2 = ((int)fracB) | (1 << BITS_MANTISSA);

				if (expDiff < ((sizeof(long) << 3) - (BITS_MANTISSA + 1)))
				{
					m1 <<= expDiff;
					temp = fracB;
				}
				else
				{
					if (0 != expB)
					{
						// arithmetic underflow
						if (expDiff > BITS_MANTISSA)
							return FromParts(0, 0, false);
						else
						{
							m2 >>= expDiff;
						}
					}
					temp = expA;
				}
			}

			// convert from sign-bit to b's complement representation
			if (negA) m1 = -m1;
			if (negB) m2 = -m2;
			m1 += m2;
			if (m1 < 0)
			{
				resultNeg = true;
				m1 = -m1;
			}
			else
			{
				resultNeg = false;
			}
			// and re-normalize the result to fit in a half
			if (0 == m1)
				return FromParts(0, 0, false);

			m2 = m1.ReverseBits();
			expDiff = m2 - BITS_MANTISSA;
			temp += expDiff;
			if (expDiff >= MAX_EXPONENT_VALUE)
			{
				// arithmetic overflow. return INF and keep the sign
				return FromParts(0, MAX_EXPONENT_VALUE, resultNeg);
			}
			else if (temp <= 0)
			{
				// Ignore Spelling: denorm
				// this maps to a denorm
				m1 <<= (-expDiff - 1);
				temp = 0;
			}
			else
			{
				// rebuild the normalized representation, take care of the hidden bit
				if (expDiff < 0)
					m1 <<= (-expDiff);
				else
					m1 >>= expDiff; // m1 >= 0
			}
			return FromParts((ushort)m1, (sbyte)temp, resultNeg);
#else
			return (Half)((float)a + (float)b);
#endif
		}

		/// <summary>
		/// Binary subtract the given <see cref="Half"/> float value <paramref name="a"/> by <paramref name="b"/>
		/// </summary>
		/// <param name="a">The first given <see cref="Half"/> float value</param>
		/// <param name="b">The second given <see cref="Half"/> float value</param>
		/// <returns><c><paramref name="a"/> - <paramref name="b"/></c></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe Half Sub(this Half a, Half b)
		{
			ushort neg = (ushort)(*(ushort*)&b ^ 0x8000u);
			return Add(a, *(Half*)&neg);
		}

		/// <summary>
		/// Binary multiply the given <see cref="Half"/> float value <paramref name="a"/> and <paramref name="b"/>
		/// </summary>
		/// <param name="a">The first given <see cref="Half"/> float value</param>
		/// <param name="b">The second given <see cref="Half"/> float value</param>
		/// <returns><c><paramref name="a"/> * <paramref name="b"/></c></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Half Mul(this Half a, Half b)
		{
			return (Half)((float)a * (float)b);
		}

		/// <summary>
		/// Binary divide the given <see cref="Half"/> float value <paramref name="a"/> by <paramref name="b"/>
		/// </summary>
		/// <param name="a">The first given <see cref="Half"/> float value</param>
		/// <param name="b">The second given <see cref="Half"/> float value</param>
		/// <returns><c><paramref name="a"/> / <paramref name="b"/></c></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe Half Div(this Half a, Half b)
		{
			return (Half)((float)a / (float)b);
		}

		/// <summary>
		/// Binary exponentiate the given <see cref="Half"/> float value <paramref name="a"/> by <paramref name="b"/>
		/// </summary>
		/// <param name="a">The first given <see cref="Half"/> float value</param>
		/// <param name="b">The second given <see cref="Half"/> float value</param>
		/// <returns><c><paramref name="a"/> ^ <paramref name="b"/></c></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Half Pow(this Half a, Half b)
		{
			return (Half)MathF.Pow((float)a, (float)b);
		}

		/// <summary>
		/// Binary add exponentiate given <see cref="Half"/> float value <paramref name="a"/> by a <see cref="double"/> <paramref name="b"/>
		/// </summary>
		/// <param name="a">The first given <see cref="Half"/> float value</param>
		/// <param name="b">The second given <see cref="double"/></param>
		/// <returns><c><paramref name="a"/> ^ <paramref name="b"/></c></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Half Pow(this Half a, double b)
		{
			return (Half)Math.Pow((double)a, b);
		}

		public static Half FromParts(int v, sbyte expRes, bool negV) => throw new NotImplementedException();
	}
	#endregion

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
				isComplex = isComplex && typeof(IComplex<>).MakeGenericType(type.GenericTypeArguments).IsAssignableFrom(type);
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
					float or double or Half => double.Parse(str).FromDouble<T>(),
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
			if (parseResult is null)
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
				float or double or Half => false,
				// built-in integer types
				sbyte or short or int or long => false,
				byte or ushort or uint or ulong => false,
				// built-in complex float types
				IComplex<float> or IComplex<double> or IComplex<Half> => true,
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
				float or double or Half => true,
				// built-in integer types
				sbyte or short or int or long => true,
				byte or ushort or uint or ulong => true,
				// built-in complex float types
				IComplex<float> or IComplex<double> or IComplex<Half> => true,
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
				float or double or Half => DataTypeClassification.FloatPoint_IEEE754,
				// built-in integer types
				sbyte or short or int or long => DataTypeClassification.SignedInteger,
				byte or ushort or uint or ulong => DataTypeClassification.UnsignedInteger,
				// built-in complex float types
				IComplex<float> or IComplex<double> or IComplex<Half> => DataTypeClassification.FloatPoint_IEEE754,
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
				Half or IComplex<Half> => 0.0009765625,
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

		#region native operations
		#region arithmetic
		/// <summary>
		/// Negate the given generic number <paramref name="value"/>
		/// </summary>
		/// <typeparam name="T">A supported data type</typeparam>
		/// <param name="value">The generic number to be negated</param>
		/// <returns><c>-<paramref name="value"/></c> if <typeparamref name="T"/> is signed type; or <paramref name="value"/> otherwise.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe T NativeNegate<T>(this T value) where T : unmanaged
		{
			// JIT shall optimize the branches and type converts to some code as if they do not exist
			if (typeof(T) == typeof(byte) || typeof(T) == typeof(ushort) || typeof(T) == typeof(char) ||
				typeof(T) == typeof(uint) || typeof(T) == typeof(ulong) ||
				typeof(T) == typeof(Complex<byte>) || typeof(T) == typeof(Complex<ushort>) || typeof(T) == typeof(Complex<char>) ||
				typeof(T) == typeof(Complex<uint>) || typeof(T) == typeof(Complex<ulong>))
			{
				return value;
			}
			else if (typeof(T) == typeof(sbyte))
			{
				sbyte v = (sbyte)(-(*(sbyte*)&value));
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(short))
			{
				short v = (short)(-(*(short*)&value));
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(int))
			{
				int v = -(*(int*)&value);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(long))
			{
				long v = -(*(long*)&value);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Half))
			{
				Half v = HalfFloatExtension.Negate(*(Half*)&value);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(float))
			{
				float v = -(*(float*)&value);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(double))
			{
				double v = -(*(double*)&value);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<sbyte>))
			{
				Complex<sbyte> v = -(*(Complex<sbyte>*)&value);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<short>))
			{
				Complex<short> v = -(*(Complex<short>*)&value);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<int>))
			{
				Complex<int> v = -(*(Complex<int>*)&value);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<long>))
			{
				Complex<long> v = -(*(Complex<long>*)&value);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<Half>))
			{
				Complex<Half> v = -(*(Complex<Half>*)&value);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<float>) || typeof(T) == typeof(ComplexSingle))
			{
				ComplexSingle v = -(*(ComplexSingle*)&value);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<double>) || typeof(T) == typeof(ComplexDouble))
			{
				ComplexDouble v = -(*(ComplexDouble*)&value);
				return *(T*)&v;
			}
			else
			{
				return Const<T>.NegateDelegate.Invoke(value);
			}
		}

		/// <summary>
		/// Reciprocate the given generic number <paramref name="value"/>
		/// </summary>
		/// <typeparam name="T">A supported data type</typeparam>
		/// <param name="value">The generic number to be reciprocated</param>
		/// <returns><c>1 / <paramref name="value"/></c></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe T NativeReciprocal<T>(this T value) where T : unmanaged
		{
			// JIT shall optimize the branches and type converts to some code as if they do not exist
			if (typeof(T) == typeof(byte))
			{
				byte v = (byte)(1 / (*(byte*)&value));
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(ushort) || typeof(T) == typeof(char))
			{
				ushort v = (ushort)(1 / (*(ushort*)&value));
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(uint))
			{
				uint v = 1 / (*(uint*)&value);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(ulong))
			{
				ulong v = 1 / (*(ulong*)&value);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<byte>))
			{
				Complex<byte> v = 1 / (*(Complex<byte>*)&value);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<ushort>) || typeof(T) == typeof(Complex<char>))
			{
				Complex<ushort> v = 1 / (*(Complex<ushort>*)&value);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<uint>))
			{
				Complex<uint> v = 1 / (*(Complex<uint>*)&value);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<ulong>))
			{
				Complex<ulong> v = 1 / (*(Complex<ulong>*)&value);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(sbyte))
			{
				sbyte v = (sbyte)(1 / (*(sbyte*)&value));
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(short))
			{
				short v = (short)(1 / (*(short*)&value));
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(int))
			{
				int v = 1 / (*(int*)&value);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(long))
			{
				long v = 1 / (*(long*)&value);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Half))
			{
				Half v = HalfFloatExtension.Reciprocal(*(Half*)&value);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(float))
			{
				float v = 1 / (*(float*)&value);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(double))
			{
				double v = 1 / (*(double*)&value);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<sbyte>))
			{
				Complex<sbyte> v = 1 / (*(Complex<sbyte>*)&value);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<short>))
			{
				Complex<short> v = 1 / (*(Complex<short>*)&value);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<int>))
			{
				Complex<int> v = 1 / (*(Complex<int>*)&value);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<long>))
			{
				Complex<long> v = 1 / (*(Complex<long>*)&value);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<Half>))
			{
				Complex<Half> v = 1 / (*(Complex<Half>*)&value);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<float>) || typeof(T) == typeof(ComplexSingle))
			{
				ComplexSingle v = 1 / (*(ComplexSingle*)&value);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<double>) || typeof(T) == typeof(ComplexDouble))
			{
				ComplexDouble v = 1 / (*(ComplexDouble*)&value);
				return *(T*)&v;
			}
			else
			{
				return Const<T>.ReciprocalDelegate.Invoke(value);
			}
		}

		/// <summary>
		/// Increase the given generic number <paramref name="value"/> by 1
		/// </summary>
		/// <typeparam name="T">A supported data type</typeparam>
		/// <param name="value">The generic number to be added by 1</param>
		/// <returns><c><paramref name="value"/> + 1</c></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe T NativeIncrement<T>(this T value) where T : unmanaged
		{
			// JIT shall optimize the branches and type converts to some code as if they do not exist
			if (typeof(T) == typeof(byte))
			{
				byte v = (byte)((*(byte*)&value) + 1);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(sbyte))
			{
				sbyte v = (sbyte)((*(sbyte*)&value) + 1);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(short))
			{
				short v = (short)((*(short*)&value) + 1);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(ushort) || typeof(T) == typeof(char))
			{
				ushort v = (ushort)((*(ushort*)&value) + 1);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(int))
			{
				int v = (*(int*)&value) + 1;
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(uint))
			{
				uint v = (*(uint*)&value) + 1;
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(long))
			{
				long v = (*(long*)&value) + 1;
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(ulong))
			{
				ulong v = (*(ulong*)&value) + 1;
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Half))
			{
				Half v = HalfFloatExtension.Inc(*(Half*)&value);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(float))
			{
				float v = (*(float*)&value) + 1;
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(double))
			{
				double v = (*(double*)&value) + 1;
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<byte>))
			{
				Complex<byte> v = (*(Complex<byte>*)&value) + 1;
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<sbyte>))
			{
				Complex<sbyte> v = (*(Complex<sbyte>*)&value) + 1;
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<short>))
			{
				Complex<short> v = (*(Complex<short>*)&value) + 1;
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<ushort>) || typeof(T) == typeof(Complex<char>))
			{
				Complex<ushort> v = (*(Complex<ushort>*)&value) + 1;
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<int>))
			{
				Complex<int> v = (*(Complex<int>*)&value) + 1;
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<uint>))
			{
				Complex<uint> v = (*(Complex<uint>*)&value) + 1;
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<long>))
			{
				Complex<long> v = (*(Complex<long>*)&value) + 1;
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<ulong>))
			{
				Complex<ulong> v = (*(Complex<ulong>*)&value) + 1;
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<Half>))
			{
				Complex<Half> v = (*(Complex<Half>*)&value) + 1;
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<float>) || typeof(T) == typeof(ComplexSingle))
			{
				ComplexSingle v = (*(ComplexSingle*)&value) + 1;
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<double>) || typeof(T) == typeof(ComplexDouble))
			{
				ComplexDouble v = (*(ComplexDouble*)&value) + 1;
				return *(T*)&v;
			}
			else
			{
				return Const<T>.AddDelegate.Invoke(value, Const<T>.One);
			}
		}

		/// <summary>
		/// Try to natively add the given generic number <paramref name="a"/> and <paramref name="b"/>
		/// </summary>
		/// <typeparam name="T">A supported data type</typeparam>
		/// <param name="a">The left generic number to be added</param>
		/// <param name="b">The right generic number to be added</param>
		/// <returns><c><paramref name="a"/> + <paramref name="b"/></c></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe T NativeAdd<T>(this T a, T b) where T : unmanaged
		{
			// JIT shall optimize the branches and type converts to some code as if they do not exist
			if (typeof(T) == typeof(byte))
			{
				byte v = (byte)((*(byte*)&a) + (*(byte*)&b));
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(sbyte))
			{
				sbyte v = (sbyte)((*(sbyte*)&a) + (*(sbyte*)&b));
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(short))
			{
				short v = (short)((*(short*)&a) + (*(short*)&b));
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(ushort) || typeof(T) == typeof(char))
			{
				ushort v = (ushort)((*(ushort*)&a) + (*(ushort*)&b));
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(int))
			{
				int v = (*(int*)&a) + (*(int*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(uint))
			{
				uint v = (*(uint*)&a) + (*(uint*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(long))
			{
				long v = (*(long*)&a) + (*(long*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(ulong))
			{
				ulong v = (*(ulong*)&a) + (*(ulong*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(float))
			{
				float v = (*(float*)&a) + (*(float*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Half))
			{
				Half v = HalfFloatExtension.Add(*(Half*)&a, *(Half*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(double))
			{
				double v = (*(double*)&a) + (*(double*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<byte>))
			{
				Complex<byte> v = (*(Complex<byte>*)&a) + (*(Complex<byte>*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<sbyte>))
			{
				Complex<sbyte> v = (*(Complex<sbyte>*)&a) + (*(Complex<sbyte>*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<short>))
			{
				Complex<short> v = (*(Complex<short>*)&a) + (*(Complex<short>*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<ushort>) || typeof(T) == typeof(Complex<char>))
			{
				Complex<ushort> v = (*(Complex<ushort>*)&a) + (*(Complex<ushort>*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<char>))
			{
				Complex<char> v = (*(Complex<char>*)&a) + (*(Complex<char>*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<int>))
			{
				Complex<int> v = (*(Complex<int>*)&a) + (*(Complex<int>*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<uint>))
			{
				Complex<uint> v = (*(Complex<uint>*)&a) + (*(Complex<uint>*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<long>))
			{
				Complex<long> v = (*(Complex<long>*)&a) + (*(Complex<long>*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<ulong>))
			{
				Complex<ulong> v = (*(Complex<ulong>*)&a) + (*(Complex<ulong>*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<Half>))
			{
				Complex<Half> v = (*(Complex<Half>*)&a) + (*(Complex<Half>*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<float>) || typeof(T) == typeof(ComplexSingle))
			{
				ComplexSingle v = (*(ComplexSingle*)&a) + (*(ComplexSingle*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<double>) || typeof(T) == typeof(ComplexDouble))
			{
				ComplexDouble v = (*(ComplexDouble*)&a) + (*(ComplexDouble*)&b);
				return *(T*)&v;
			}
			else
			{
				return Const<T>.AddDelegate.Invoke(a, b);
			}
		}

		/// <summary>
		/// Try to natively subtract the given generic number <paramref name="a"/> by <paramref name="b"/>
		/// </summary>
		/// <typeparam name="T">A supported data type</typeparam>
		/// <param name="a">The generic number to be subtracted by <paramref name="b"/></param>
		/// <param name="b">The generic number to subtract from <paramref name="a"/></param>
		/// <returns><c><paramref name="a"/> - <paramref name="b"/></c></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe T NativeSub<T>(this T a, T b) where T : unmanaged
		{
			// JIT shall optimize the branches and type converts to some code as if they do not exist
			if (typeof(T) == typeof(byte))
			{
				byte v = (byte)((*(byte*)&a) - (*(byte*)&b));
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(sbyte))
			{
				sbyte v = (sbyte)((*(sbyte*)&a) - (*(sbyte*)&b));
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(short))
			{
				short v = (short)((*(short*)&a) - (*(short*)&b));
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(ushort) || typeof(T) == typeof(char))
			{
				ushort v = (ushort)((*(ushort*)&a) - (*(ushort*)&b));
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(int))
			{
				int v = (*(int*)&a) - (*(int*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(uint))
			{
				uint v = (*(uint*)&a) - (*(uint*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(long))
			{
				long v = (*(long*)&a) - (*(long*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(ulong))
			{
				ulong v = (*(ulong*)&a) - (*(ulong*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Half))
			{
				Half v = HalfFloatExtension.Sub(*(Half*)&a, *(Half*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(float))
			{
				float v = (*(float*)&a) - (*(float*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(double))
			{
				double v = (*(double*)&a) - (*(double*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<byte>))
			{
				Complex<byte> v = (*(Complex<byte>*)&a) - (*(Complex<byte>*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<sbyte>))
			{
				Complex<sbyte> v = (*(Complex<sbyte>*)&a) - (*(Complex<sbyte>*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<short>))
			{
				Complex<short> v = (*(Complex<short>*)&a) - (*(Complex<short>*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<ushort>) || typeof(T) == typeof(Complex<char>))
			{
				Complex<ushort> v = (*(Complex<ushort>*)&a) - (*(Complex<ushort>*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<char>))
			{
				Complex<char> v = (*(Complex<char>*)&a) - (*(Complex<char>*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<int>))
			{
				Complex<int> v = (*(Complex<int>*)&a) - (*(Complex<int>*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<uint>))
			{
				Complex<uint> v = (*(Complex<uint>*)&a) - (*(Complex<uint>*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<long>))
			{
				Complex<long> v = (*(Complex<long>*)&a) - (*(Complex<long>*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<ulong>))
			{
				Complex<ulong> v = (*(Complex<ulong>*)&a) - (*(Complex<ulong>*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<Half>))
			{
				Complex<Half> v = (*(Complex<Half>*)&a) - (*(Complex<Half>*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<float>) || typeof(T) == typeof(ComplexSingle))
			{
				ComplexSingle v = (*(ComplexSingle*)&a) - (*(ComplexSingle*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<double>) || typeof(T) == typeof(ComplexDouble))
			{
				ComplexDouble v = (*(ComplexDouble*)&a) - (*(ComplexDouble*)&b);
				return *(T*)&v;
			}
			else
			{
				return Const<T>.AddDelegate.Invoke(a, b);
			}
		}

		/// <summary>
		/// Try to natively multiply the given generic number <paramref name="a"/> and <paramref name="b"/>
		/// </summary>
		/// <typeparam name="T">A supported data type</typeparam>
		/// <param name="a">The left generic number to be multiplied</param>
		/// <param name="b">The right generic number to be multiplied</param>
		/// <returns><c><paramref name="a"/> * <paramref name="b"/></c></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe T NativeMultiply<T>(this T a, T b) where T : unmanaged
		{
			// JIT shall optimize the branches and type converts to some code as if they do not exist
			if (typeof(T) == typeof(byte))
			{
				byte v = (byte)((*(byte*)&a) * (*(byte*)&b));
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(sbyte))
			{
				sbyte v = (sbyte)((*(sbyte*)&a) * (*(sbyte*)&b));
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(short))
			{
				short v = (short)((*(short*)&a) * (*(short*)&b));
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(ushort) || typeof(T) == typeof(char))
			{
				ushort v = (ushort)((*(ushort*)&a) * (*(ushort*)&b));
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(int))
			{
				int v = (*(int*)&a) * (*(int*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(uint))
			{
				uint v = (*(uint*)&a) * (*(uint*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(long))
			{
				long v = (*(long*)&a) * (*(long*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(ulong))
			{
				ulong v = (*(ulong*)&a) * (*(ulong*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Half))
			{
				Half v = HalfFloatExtension.Mul(*(Half*)&a, *(Half*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(float))
			{
				float v = (*(float*)&a) * (*(float*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(double))
			{
				double v = (*(double*)&a) * (*(double*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<byte>))
			{
				Complex<byte> v = (*(Complex<byte>*)&a) * (*(Complex<byte>*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<sbyte>))
			{
				Complex<sbyte> v = (*(Complex<sbyte>*)&a) * (*(Complex<sbyte>*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<short>))
			{
				Complex<short> v = (*(Complex<short>*)&a) * (*(Complex<short>*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<ushort>) || typeof(T) == typeof(Complex<char>))
			{
				Complex<ushort> v = (*(Complex<ushort>*)&a) * (*(Complex<ushort>*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<char>))
			{
				Complex<char> v = (*(Complex<char>*)&a) * (*(Complex<char>*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<int>))
			{
				Complex<int> v = (*(Complex<int>*)&a) * (*(Complex<int>*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<uint>))
			{
				Complex<uint> v = (*(Complex<uint>*)&a) * (*(Complex<uint>*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<long>))
			{
				Complex<long> v = (*(Complex<long>*)&a) * (*(Complex<long>*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<ulong>))
			{
				Complex<ulong> v = (*(Complex<ulong>*)&a) * (*(Complex<ulong>*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<Half>))
			{
				Complex<Half> v = (*(Complex<Half>*)&a) * (*(Complex<Half>*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<float>) || typeof(T) == typeof(ComplexSingle))
			{
				ComplexSingle v = (*(ComplexSingle*)&a) * (*(ComplexSingle*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<double>) || typeof(T) == typeof(ComplexDouble))
			{
				ComplexDouble v = (*(ComplexDouble*)&a) * (*(ComplexDouble*)&b);
				return *(T*)&v;
			}
			else
			{
				return Const<T>.MultiplyDelegate.Invoke(a, b);
			}
		}

		/// <summary>
		/// Try to natively divide the given generic number <paramref name="a"/> by <paramref name="b"/>
		/// </summary>
		/// <typeparam name="T">A supported data type</typeparam>
		/// <param name="a">The generic number to be divided by <paramref name="b"/></param>
		/// <param name="b">The generic number to divide <paramref name="a"/></param>
		/// <returns><c><paramref name="a"/> / <paramref name="b"/></c></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe T NativeDivide<T>(this T a, T b) where T : unmanaged
		{
			// JIT shall optimize the branches and type converts to some code as if they do not exist
			if (typeof(T) == typeof(byte))
			{
				byte v = (byte)((*(byte*)&a) / (*(byte*)&b));
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(sbyte))
			{
				sbyte v = (sbyte)((*(sbyte*)&a) / (*(sbyte*)&b));
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(short))
			{
				short v = (short)((*(short*)&a) / (*(short*)&b));
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(ushort) || typeof(T) == typeof(char))
			{
				ushort v = (ushort)((*(ushort*)&a) / (*(ushort*)&b));
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(int))
			{
				int v = (*(int*)&a) / (*(int*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(uint))
			{
				uint v = (*(uint*)&a) / (*(uint*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(long))
			{
				long v = (*(long*)&a) / (*(long*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(ulong))
			{
				ulong v = (*(ulong*)&a) / (*(ulong*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Half))
			{
				Half v = HalfFloatExtension.Div(*(Half*)&a, *(Half*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(float))
			{
				float v = (*(float*)&a) / (*(float*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(double))
			{
				double v = (*(double*)&a) / (*(double*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<byte>))
			{
				Complex<byte> v = (*(Complex<byte>*)&a) / (*(Complex<byte>*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<sbyte>))
			{
				Complex<sbyte> v = (*(Complex<sbyte>*)&a) / (*(Complex<sbyte>*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<short>))
			{
				Complex<short> v = (*(Complex<short>*)&a) / (*(Complex<short>*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<ushort>) || typeof(T) == typeof(Complex<char>))
			{
				Complex<ushort> v = (*(Complex<ushort>*)&a) / (*(Complex<ushort>*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<char>))
			{
				Complex<char> v = (*(Complex<char>*)&a) / (*(Complex<char>*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<int>))
			{
				Complex<int> v = (*(Complex<int>*)&a) / (*(Complex<int>*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<uint>))
			{
				Complex<uint> v = (*(Complex<uint>*)&a) / (*(Complex<uint>*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<long>))
			{
				Complex<long> v = (*(Complex<long>*)&a) / (*(Complex<long>*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<ulong>))
			{
				Complex<ulong> v = (*(Complex<ulong>*)&a) / (*(Complex<ulong>*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<Half>))
			{
				Complex<Half> v = (*(Complex<Half>*)&a) / (*(Complex<Half>*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<float>) || typeof(T) == typeof(ComplexSingle))
			{
				ComplexSingle v = (*(ComplexSingle*)&a) / (*(ComplexSingle*)&b);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<double>) || typeof(T) == typeof(ComplexDouble))
			{
				ComplexDouble v = (*(ComplexDouble*)&a) / (*(ComplexDouble*)&b);
				return *(T*)&v;
			}
			else
			{
				return Const<T>.DivideDelegate.Invoke(a, b);
			}
		}

		/// <summary>
		/// Try to natively find the square root of the given generic number <paramref name="a"/> 
		/// </summary>
		/// <typeparam name="T">A supported data type</typeparam>
		/// <param name="a">The generic number to get square root</param>
		/// <returns>The square root of <paramref name="a"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe T NativeSqrt<T>(this T a) where T : unmanaged
		{
			if (a.IsZero() || a.IsOne())
				return a;
			if (typeof(T) == typeof(byte))
			{
				byte v = (byte)Math.Sqrt(*(byte*)&a);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(ushort) || typeof(T) == typeof(char))
			{
				ushort v = (ushort)Math.Sqrt(*(ushort*)&a);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(uint))
			{
				uint v = (uint)Math.Sqrt(*(uint*)&a);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(ulong))
			{
				ulong v = (uint)Math.Sqrt(*(ulong*)&a);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<byte>))
			{
				Complex<byte> v = (*(Complex<byte>*)&a).Sqrt();
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<ushort>) || typeof(T) == typeof(Complex<char>))
			{
				Complex<ushort> v = (*(Complex<ushort>*)&a).Sqrt();
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<uint>))
			{
				Complex<uint> v = (*(Complex<uint>*)&a).Sqrt();
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<ulong>))
			{
				Complex<ulong> v = (*(Complex<ulong>*)&a).Sqrt();
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(sbyte))
			{
				sbyte v = (sbyte)Math.Sqrt(*(sbyte*)&a);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(short))
			{
				short v = (short)Math.Sqrt(*(short*)&a);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(int))
			{
				int v = (int)Math.Sqrt(*(int*)&a);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(long))
			{
				long v = (long)Math.Sqrt(*(long*)&a);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Half))
			{
				Half v = HalfFloatExtension.Sqrt(*(Half*)&a);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(float))
			{
				float v = MathF.Sqrt(*(float*)&a);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(double))
			{
				double v = Math.Sqrt(*(double*)&a);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<sbyte>))
			{
				Complex<sbyte> v = (*(Complex<sbyte>*)&a).Sqrt();
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<short>))
			{
				Complex<short> v = (*(Complex<short>*)&a).Sqrt();
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<int>))
			{
				Complex<int> v = (*(Complex<int>*)&a).Sqrt();
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<long>))
			{
				Complex<long> v = (*(Complex<long>*)&a).Sqrt();
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<Half>))
			{
				Complex<Half> v = (*(Complex<Half>*)&a).Sqrt();
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<float>) || typeof(T) == typeof(ComplexSingle))
			{
				ComplexSingle v = (*(ComplexSingle*)&a).Sqrt();
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<double>) || typeof(T) == typeof(ComplexDouble))
			{
				ComplexDouble v = (*(ComplexDouble*)&a).Sqrt();
				return *(T*)&v;
			}
			else
			{
				return Const<T>.SqrtDelegate.Invoke(a);
			}
		}

		/// <summary>
		/// Try to natively exponentiate the given generic number <paramref name="basis"/> by the given <paramref name="power"/> of type <see cref="double"/>
		/// </summary>
		/// <typeparam name="T">A supported data type</typeparam>
		/// <param name="basis">The generic number to be exponentiated as the base</param>
		/// <param name="power">The <see cref="double"/> to be exponentiated as the exponent</param>
		/// <returns><c><paramref name="basis"/> ^ <paramref name="power"/></c></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe T NativePower<T>(this T basis, double power) where T : unmanaged
		{
			if (power == 0)
				return Const<T>.One;
			if (power == 1 || basis.IsZero() || basis.IsOne())
				return basis;
			if (typeof(T) == typeof(byte))
			{
				byte v = (byte)Math.Pow(*(byte*)&basis, power);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(ushort) || typeof(T) == typeof(char))
			{
				ushort v = (ushort)Math.Pow(*(ushort*)&basis, power);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(uint))
			{
				uint v = (uint)Math.Pow(*(uint*)&basis, power);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(ulong))
			{
				ulong v = (uint)Math.Pow(*(ulong*)&basis, power);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<byte>))
			{
				Complex<byte> v = (*(Complex<byte>*)&basis).PowDouble(power);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<ushort>) || typeof(T) == typeof(Complex<char>))
			{
				Complex<ushort> v = (*(Complex<ushort>*)&basis).PowDouble(power);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<uint>))
			{
				Complex<uint> v = (*(Complex<uint>*)&basis).PowDouble(power);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<ulong>))
			{
				Complex<ulong> v = (*(Complex<ulong>*)&basis).PowDouble(power);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(sbyte))
			{
				sbyte v = (sbyte)Math.Pow(*(sbyte*)&basis, power);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(short))
			{
				short v = (short)Math.Pow(*(short*)&basis, power);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(int))
			{
				int v = (int)Math.Pow(*(int*)&basis, power);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(long))
			{
				long v = (long)Math.Pow(*(long*)&basis, power);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(float))
			{
				float v = MathF.Sqrt(*(float*)&basis);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(double))
			{
				double v = Math.Pow(*(double*)&basis, power);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Half))
			{
				Half v = HalfFloatExtension.Pow(*(Half*)&basis, power);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<sbyte>))
			{
				Complex<sbyte> v = (*(Complex<sbyte>*)&basis).PowDouble(power);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<short>))
			{
				Complex<short> v = (*(Complex<short>*)&basis).PowDouble(power);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<int>))
			{
				Complex<int> v = (*(Complex<int>*)&basis).PowDouble(power);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<long>))
			{
				Complex<long> v = (*(Complex<long>*)&basis).PowDouble(power);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<Half>))
			{
				Complex<Half> v = (*(Complex<Half>*)&basis).PowDouble(power);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<float>) || typeof(T) == typeof(ComplexSingle))
			{
				ComplexSingle v = (*(ComplexSingle*)&basis).Pow((float)power);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<double>) || typeof(T) == typeof(ComplexDouble))
			{
				ComplexDouble v = (*(ComplexDouble*)&basis).Pow(power);
				return *(T*)&v;
			}
			else
			{
				return Const<T>.PowerDelegate1.Invoke(basis, power);
			}
		}

		/// <summary>
		/// Try to natively exponentiate the given generic number <paramref name="basis"/> by the given <paramref name="power"/> of type <typeparamref name="T"/>
		/// </summary>
		/// <typeparam name="T">A supported data type</typeparam>
		/// <param name="basis">The generic number to be exponentiated as the base</param>
		/// <param name="power">The generic number to be exponentiated as the exponent</param>
		/// <returns><c><paramref name="basis"/> ^ <paramref name="power"/></c></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe T NativePower<T>(this T basis, T power) where T : unmanaged
		{
			if (power.IsZero())
				return Const<T>.One;
			if (power.IsZero() || basis.IsZero() || basis.IsOne())
				return basis;
			if (typeof(T) == typeof(byte))
			{
				byte v = (byte)Math.Pow(*(byte*)&basis, *(byte*)&power);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(ushort) || typeof(T) == typeof(char))
			{
				ushort v = (ushort)Math.Pow(*(ushort*)&basis, *(ushort*)&power);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(uint))
			{
				uint v = (uint)Math.Pow(*(uint*)&basis, *(uint*)&power);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(ulong))
			{
				ulong v = (uint)Math.Pow(*(ulong*)&basis, *(ulong*)&power);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<byte>))
			{
				Complex<byte> v = (*(Complex<byte>*)&basis).Pow(*(Complex<byte>*)&power);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<ushort>) || typeof(T) == typeof(Complex<char>))
			{
				Complex<ushort> v = (*(Complex<ushort>*)&basis).Pow(*(Complex<ushort>*)&power);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<uint>))
			{
				Complex<uint> v = (*(Complex<uint>*)&basis).Pow(*(Complex<uint>*)&power);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<ulong>))
			{
				Complex<ulong> v = (*(Complex<ulong>*)&basis).Pow(*(Complex<ulong>*)&power);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(sbyte))
			{
				sbyte v = (sbyte)Math.Pow(*(sbyte*)&basis, *(sbyte*)&power);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(short))
			{
				short v = (short)Math.Pow(*(short*)&basis, *(short*)&power);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(int))
			{
				int v = (int)Math.Pow(*(int*)&basis, *(int*)&power);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(long))
			{
				long v = (long)Math.Pow(*(long*)&basis, *(long*)&power);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Half))
			{
				Half v = HalfFloatExtension.Pow(*(Half*)&basis, *(Half*)&power);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(float))
			{
				float v = MathF.Sqrt(*(float*)&basis);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(double))
			{
				double v = Math.Pow(*(double*)&basis, *(double*)&power);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<sbyte>))
			{
				Complex<sbyte> v = (*(Complex<sbyte>*)&basis).Pow(*(Complex<sbyte>*)&power);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<short>))
			{
				Complex<short> v = (*(Complex<short>*)&basis).Pow(*(Complex<short>*)&power);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<int>))
			{
				Complex<int> v = (*(Complex<int>*)&basis).Pow(*(Complex<int>*)&power);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<long>))
			{
				Complex<long> v = (*(Complex<long>*)&basis).Pow(*(Complex<long>*)&power);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<Half>))
			{
				Complex<Half> v = (*(Complex<Half>*)&basis).Pow(*(Complex<Half>*)&power);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<float>) || typeof(T) == typeof(ComplexSingle))
			{
				ComplexSingle v = (*(ComplexSingle*)&basis).Pow(*(ComplexSingle*)&power);
				return *(T*)&v;
			}
			else if (typeof(T) == typeof(Complex<double>) || typeof(T) == typeof(ComplexDouble))
			{
				ComplexDouble v = (*(ComplexDouble*)&basis).Pow(*(ComplexDouble*)&power);
				return *(T*)&v;
			}
			else
			{
				return Const<T>.PowerDelegate2.Invoke(basis, power);
			}
		}
		#endregion

		#region predicator
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
		public static bool IsOne<T>(this T a) where T : unmanaged => IsEqual(a, Const<T>.One);

		/// <summary>
		/// Try to natively compare whether the given generic numbers <paramref name="a"/> == <paramref name="b"/>. If there is no pre-defined equality comparer, their bit-wise equality will be returned.
		/// </summary>
		/// <typeparam name="T">A supported data type</typeparam>
		/// <param name="a">The generic number to be compared at left</param>
		/// <param name="b">The generic number to be compared at right</param>
		/// <returns><c><paramref name="a"/> == <paramref name="b"/></c></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe bool IsEqual<T>(this T a, T b) where T : unmanaged
		{
			if (typeof(T) == typeof(sbyte)) return (*(sbyte*)&a) == (*(sbyte*)&b);
			if (typeof(T) == typeof(ushort) || typeof(T) == typeof(char)) return (*(ushort*)&a) == (*(ushort*)&b);
			if (typeof(T) == typeof(uint)) return (*(uint*)&a) == (*(uint*)&b);
			if (typeof(T) == typeof(ulong)) return (*(ulong*)&a) == (*(ulong*)&b);
			if (typeof(T) == typeof(byte)) return (*(byte*)&a) == (*(byte*)&b);
			if (typeof(T) == typeof(short)) return (*(short*)&a) == (*(short*)&b);
			if (typeof(T) == typeof(int)) return (*(int*)&a) == (*(int*)&b);
			if (typeof(T) == typeof(long)) return (*(long*)&a) == (*(long*)&b);
			if (typeof(T) == typeof(Half)) return (*(Half*)&a) == (*(Half*)&b);
			if (typeof(T) == typeof(float)) return (*(float*)&a) == (*(float*)&b);
			if (typeof(T) == typeof(double)) return (*(double*)&a) == (*(double*)&b);
			if (typeof(T) == typeof(Complex<sbyte>)) return (*(Complex<sbyte>*)&a) == (*(Complex<sbyte>*)&b);
			if (typeof(T) == typeof(Complex<ushort>) || typeof(T) == typeof(Complex<char>)) return (*(Complex<ushort>*)&a) == (*(Complex<ushort>*)&b);
			if (typeof(T) == typeof(Complex<uint>)) return (*(Complex<uint>*)&a) == (*(Complex<uint>*)&b);
			if (typeof(T) == typeof(Complex<ulong>)) return (*(Complex<ulong>*)&a) == (*(Complex<ulong>*)&b);
			if (typeof(T) == typeof(Complex<byte>)) return (*(Complex<byte>*)&a) == (*(Complex<byte>*)&b);
			if (typeof(T) == typeof(Complex<short>)) return (*(Complex<short>*)&a) == (*(Complex<short>*)&b);
			if (typeof(T) == typeof(Complex<int>)) return (*(Complex<int>*)&a) == (*(Complex<int>*)&b);
			if (typeof(T) == typeof(Complex<long>)) return (*(Complex<long>*)&a) == (*(Complex<long>*)&b);
			if (typeof(T) == typeof(Complex<Half>)) return (*(Complex<Half>*)&a) == (*(Complex<Half>*)&b);
			if (typeof(T) == typeof(Complex<float>) || typeof(T) == typeof(ComplexSingle)) return (*(ComplexSingle*)&a) == (*(ComplexSingle*)&b);
			if (typeof(T) == typeof(Complex<double>) || typeof(T) == typeof(ComplexDouble)) return (*(ComplexDouble*)&a) == (*(ComplexDouble*)&b);
			if (typeof(T) == typeof(ComplexSingle)) return (*(ComplexSingle*)&a) == (*(ComplexSingle*)&b);
			if (typeof(T) == typeof(ComplexDouble)) return (*(ComplexDouble*)&a) == (*(ComplexDouble*)&b);
			// else
			return Const<T>.EqualityDelegate?.Invoke(a, b) ?? new ReadOnlySpan<byte>(&a, sizeof(T)).SequenceEqual(new ReadOnlySpan<byte>(&b, sizeof(T)));
		}

		/// <summary>
		/// Try to natively compare the given real-typed generic numbers <paramref name="a"/> &gt; <paramref name="b"/>
		/// </summary>
		/// <typeparam name="T">A supported data type</typeparam>
		/// <param name="a">The generic number to be compared at left</param>
		/// <param name="b">The generic number to be compared at right</param>
		/// <returns><c><paramref name="a"/> &gt; <paramref name="b"/></c> or false if <typeparamref name="T"/> is not a real type</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe bool NativeGreaterThan<T>(this T a, T b) where T : unmanaged
		{
			if (typeof(T) == typeof(sbyte)) return (*(sbyte*)&a) > (*(sbyte*)&b);
			if (typeof(T) == typeof(ushort) || typeof(T) == typeof(char)) return (*(ushort*)&a) > (*(ushort*)&b);
			if (typeof(T) == typeof(uint)) return (*(uint*)&a) > (*(uint*)&b);
			if (typeof(T) == typeof(ulong)) return (*(ulong*)&a) > (*(ulong*)&b);
			if (typeof(T) == typeof(byte)) return (*(byte*)&a) > (*(byte*)&b);
			if (typeof(T) == typeof(short)) return (*(short*)&a) > (*(short*)&b);
			if (typeof(T) == typeof(int)) return (*(int*)&a) > (*(int*)&b);
			if (typeof(T) == typeof(long)) return (*(long*)&a) > (*(long*)&b);
			if (typeof(T) == typeof(Half)) return (*(Half*)&a) > (*(Half*)&b);
			if (typeof(T) == typeof(float)) return (*(float*)&a) > (*(float*)&b);
			if (typeof(T) == typeof(double)) return (*(double*)&a) > (*(double*)&b);
			// else
			return Const<T>.GreaterThanDelegate?.Invoke(a, b) ?? false;
		}

		/// <summary>
		/// Try to natively compare the given real-typed generic numbers <paramref name="a"/> ≥ <paramref name="b"/>
		/// </summary>
		/// <typeparam name="T">A supported data type</typeparam>
		/// <param name="a">The generic number to be compared at left</param>
		/// <param name="b">The generic number to be compared at right</param>
		/// <returns><c><paramref name="a"/> ≥ <paramref name="b"/></c> or false if <typeparamref name="T"/> is not a real type</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe bool NativeGreaterThanOrEqual<T>(this T a, T b) where T : unmanaged
		{
			if (typeof(T) == typeof(sbyte)) return (*(sbyte*)&a) >= (*(sbyte*)&b);
			if (typeof(T) == typeof(ushort) || typeof(T) == typeof(char)) return (*(ushort*)&a) >= (*(ushort*)&b);
			if (typeof(T) == typeof(uint)) return (*(uint*)&a) >= (*(uint*)&b);
			if (typeof(T) == typeof(ulong)) return (*(ulong*)&a) >= (*(ulong*)&b);
			if (typeof(T) == typeof(byte)) return (*(byte*)&a) >= (*(byte*)&b);
			if (typeof(T) == typeof(short)) return (*(short*)&a) >= (*(short*)&b);
			if (typeof(T) == typeof(int)) return (*(int*)&a) >= (*(int*)&b);
			if (typeof(T) == typeof(long)) return (*(long*)&a) >= (*(long*)&b);
			if (typeof(T) == typeof(Half)) return (*(Half*)&a) >= (*(Half*)&b);
			if (typeof(T) == typeof(float)) return (*(float*)&a) >= (*(float*)&b);
			if (typeof(T) == typeof(double)) return (*(double*)&a) >= (*(double*)&b);
			// else
			return Const<T>.GreaterThanOrEqualDelegate?.Invoke(a, b) ?? false;
		}

		/// <summary>
		/// Try to natively compare the given real-typed generic numbers <paramref name="a"/> &lt; <paramref name="b"/>
		/// </summary>
		/// <typeparam name="T">A supported data type</typeparam>
		/// <param name="a">The generic number to be compared at left</param>
		/// <param name="b">The generic number to be compared at right</param>
		/// <returns><c><paramref name="a"/> &lt; <paramref name="b"/></c> or false if <typeparamref name="T"/> is not a real type</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe bool NativeLessThan<T>(this T a, T b) where T : unmanaged
		{
			if (typeof(T) == typeof(sbyte)) return (*(sbyte*)&a) < (*(sbyte*)&b);
			if (typeof(T) == typeof(ushort) || typeof(T) == typeof(char)) return (*(ushort*)&a) < (*(ushort*)&b);
			if (typeof(T) == typeof(uint)) return (*(uint*)&a) < (*(uint*)&b);
			if (typeof(T) == typeof(ulong)) return (*(ulong*)&a) < (*(ulong*)&b);
			if (typeof(T) == typeof(byte)) return (*(byte*)&a) < (*(byte*)&b);
			if (typeof(T) == typeof(short)) return (*(short*)&a) < (*(short*)&b);
			if (typeof(T) == typeof(int)) return (*(int*)&a) < (*(int*)&b);
			if (typeof(T) == typeof(long)) return (*(long*)&a) < (*(long*)&b);
			if (typeof(T) == typeof(Half)) return (*(Half*)&a) < (*(Half*)&b);
			if (typeof(T) == typeof(float)) return (*(float*)&a) < (*(float*)&b);
			if (typeof(T) == typeof(double)) return (*(double*)&a) < (*(double*)&b);
			// else
			return Const<T>.LessThanDelegate?.Invoke(a, b) ?? false;
		}

		/// <summary>
		/// Try to natively compare the given real-typed generic numbers <paramref name="a"/> ≤ <paramref name="b"/>
		/// </summary>
		/// <typeparam name="T">A supported data type</typeparam>
		/// <param name="a">The generic number to be compared at left</param>
		/// <param name="b">The generic number to be compared at right</param>
		/// <returns><c><paramref name="a"/> ≤ <paramref name="b"/></c> or false if <typeparamref name="T"/> is not a real type</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe bool NativeLessThanOrEqual<T>(this T a, T b) where T : unmanaged
		{
			if (typeof(T) == typeof(sbyte)) return (*(sbyte*)&a) <= (*(sbyte*)&b);
			if (typeof(T) == typeof(ushort) || typeof(T) == typeof(char)) return (*(ushort*)&a) <= (*(ushort*)&b);
			if (typeof(T) == typeof(uint)) return (*(uint*)&a) <= (*(uint*)&b);
			if (typeof(T) == typeof(ulong)) return (*(ulong*)&a) <= (*(ulong*)&b);
			if (typeof(T) == typeof(byte)) return (*(byte*)&a) <= (*(byte*)&b);
			if (typeof(T) == typeof(short)) return (*(short*)&a) <= (*(short*)&b);
			if (typeof(T) == typeof(int)) return (*(int*)&a) <= (*(int*)&b);
			if (typeof(T) == typeof(long)) return (*(long*)&a) <= (*(long*)&b);
			if (typeof(T) == typeof(Half)) return (*(Half*)&a) <= (*(Half*)&b);
			if (typeof(T) == typeof(float)) return (*(float*)&a) <= (*(float*)&b);
			if (typeof(T) == typeof(double)) return (*(double*)&a) <= (*(double*)&b);
			// else
			return Const<T>.LessThanOrEqualDelegate?.Invoke(a, b) ?? false;
		}
		#endregion

		#region convert
		/// <summary>
		/// Generically convert the input number <paramref name="a"/> of type <typeparamref name="T1"/> to type <typeparamref name="T2"/> by using loaded explicit or implicit conversion operators or by utilizing default primitive or pre-defined types' converters.
		/// </summary>
		/// <typeparam name="T1">The input data type</typeparam>
		/// <typeparam name="T2">The output data type</typeparam>
		/// <param name="a">The generic number of type <typeparamref name="T1"/> to be converted</param>
		/// <returns><c>(<typeparamref name="T2"/>)a</c> if <typeparamref name="T1"/> is a real type;<br/>
		/// or <c>new <typeparamref name="T2"/>((<typeparamref name="T2"/>.T)a.Real, (<typeparamref name="T2"/>.T)a.Imag)</c> if both <typeparamref name="T1"/> and <typeparamref name="T2"/> are complex types;<br/>
		/// or <c>(<typeparamref name="T2"/>)<paramref name="a"/>.Abs()</c> if <typeparamref name="T1"/> is a complex type and <typeparamref name="T2"/> is a real type</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe T2 NativeConvert<T1, T2>(this T1 a) where T1 : unmanaged where T2 : unmanaged
		{
			if (typeof(T1) == typeof(T2))
				return *(T2*)&a;
			if (typeof(T2) == typeof(Complex<T1>))
			{	// real to complex
				Complex<T1> v = new(a);
				return *(T2*)&v;
			}
			if (typeof(T1) == typeof(Complex<T2>))
			{   // complex to real
				return (*(Complex<T2>*)&a).Abs();
			}
			// pre-defined real to real / complex
			if (typeof(T1) == typeof(sbyte))
			{
				sbyte b = *(sbyte*)&a;
				if (typeof(T2) == typeof(sbyte)) { sbyte v = (sbyte)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(ushort) || typeof(T2) == typeof(char)) { ushort v = (ushort)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(uint)) { uint v = (uint)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(ulong)) { ulong v = (ulong)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(byte)) { byte v = (byte)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(short)) { short v = (short)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(int)) { int v = (int)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(long)) { long v = (long)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Half)) { Half v = (Half)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(float)) { float v = (float)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(double)) { double v = (double)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<sbyte>)) { Complex<sbyte> v = (sbyte)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<ushort>)) { Complex<ushort> v = (ushort)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<uint>)) { Complex<uint> v = (uint)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<ulong>)) { Complex<ulong> v = (ulong)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<byte>)) { Complex<byte> v = (byte)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<short>)) { Complex<short> v = (short)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<int>)) { Complex<int> v = new((int)b); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<long>)) { Complex<long> v = (long)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<Half>)) { Complex<Half> v = (Half)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<float>) || typeof(T2) == typeof(ComplexSingle)) { ComplexSingle v = (float)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<double>) || typeof(T2) == typeof(ComplexDouble)) { ComplexDouble v = (double)b; return *(T2*)&v; }
			}
			if (typeof(T1) == typeof(ushort) || typeof(T1) == typeof(char))
			{
				ushort b = *(ushort*)&a;
				if (typeof(T2) == typeof(sbyte)) { sbyte v = (sbyte)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(ushort) || typeof(T2) == typeof(char)) { ushort v = (ushort)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(uint)) { uint v = (uint)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(ulong)) { ulong v = (ulong)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(byte)) { byte v = (byte)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(short)) { short v = (short)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(int)) { int v = (int)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(long)) { long v = (long)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Half)) { Half v = (Half)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(float)) { float v = (float)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(double)) { double v = (double)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<sbyte>)) { Complex<sbyte> v = (sbyte)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<ushort>)) { Complex<ushort> v = (ushort)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<uint>)) { Complex<uint> v = (uint)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<ulong>)) { Complex<ulong> v = (ulong)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<byte>)) { Complex<byte> v = (byte)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<short>)) { Complex<short> v = (short)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<int>)) { Complex<int> v = new((int)b); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<long>)) { Complex<long> v = (long)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<Half>)) { Complex<Half> v = (Half)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<float>) || typeof(T2) == typeof(ComplexSingle)) { ComplexSingle v = (float)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<double>) || typeof(T2) == typeof(ComplexDouble)) { ComplexDouble v = (double)b; return *(T2*)&v; }
			}
			if (typeof(T1) == typeof(uint))
			{
				uint b = *(uint*)&a;
				if (typeof(T2) == typeof(sbyte)) { sbyte v = (sbyte)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(ushort) || typeof(T2) == typeof(char)) { ushort v = (ushort)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(uint)) { uint v = (uint)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(ulong)) { ulong v = (ulong)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(byte)) { byte v = (byte)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(short)) { short v = (short)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(int)) { int v = (int)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(long)) { long v = (long)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Half)) { Half v = (Half)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(float)) { float v = (float)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(double)) { double v = (double)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<sbyte>)) { Complex<sbyte> v = (sbyte)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<ushort>)) { Complex<ushort> v = (ushort)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<uint>)) { Complex<uint> v = (uint)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<ulong>)) { Complex<ulong> v = (ulong)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<byte>)) { Complex<byte> v = (byte)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<short>)) { Complex<short> v = (short)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<int>)) { Complex<int> v = new((int)b); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<long>)) { Complex<long> v = (long)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<Half>)) { Complex<Half> v = (Half)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<float>) || typeof(T2) == typeof(ComplexSingle)) { ComplexSingle v = (float)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<double>) || typeof(T2) == typeof(ComplexDouble)) { ComplexDouble v = (double)b; return *(T2*)&v; }
			}
			if (typeof(T1) == typeof(uint))
			{
				uint b = *(uint*)&a;
				if (typeof(T2) == typeof(sbyte)) { sbyte v = (sbyte)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(ushort) || typeof(T2) == typeof(char)) { ushort v = (ushort)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(uint)) { uint v = (uint)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(ulong)) { ulong v = (ulong)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(byte)) { byte v = (byte)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(short)) { short v = (short)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(int)) { int v = (int)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(long)) { long v = (long)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Half)) { Half v = (Half)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(float)) { float v = (float)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(double)) { double v = (double)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<sbyte>)) { Complex<sbyte> v = (sbyte)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<ushort>)) { Complex<ushort> v = (ushort)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<uint>)) { Complex<uint> v = (uint)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<ulong>)) { Complex<ulong> v = (ulong)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<byte>)) { Complex<byte> v = (byte)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<short>)) { Complex<short> v = (short)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<int>)) { Complex<int> v = new((int)b); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<long>)) { Complex<long> v = (long)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<Half>)) { Complex<Half> v = (Half)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<float>) || typeof(T2) == typeof(ComplexSingle)) { ComplexSingle v = (float)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<double>) || typeof(T2) == typeof(ComplexDouble)) { ComplexDouble v = (double)b; return *(T2*)&v; }
			}
			if (typeof(T1) == typeof(ulong))
			{
				ulong b = *(ulong*)&a;
				if (typeof(T2) == typeof(sbyte)) { sbyte v = (sbyte)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(ushort) || typeof(T2) == typeof(char)) { ushort v = (ushort)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(uint)) { uint v = (uint)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(ulong)) { ulong v = (ulong)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(byte)) { byte v = (byte)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(short)) { short v = (short)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(int)) { int v = (int)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(long)) { long v = (long)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Half)) { Half v = (Half)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(float)) { float v = (float)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(double)) { double v = (double)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<sbyte>)) { Complex<sbyte> v = (sbyte)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<ushort>)) { Complex<ushort> v = (ushort)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<uint>)) { Complex<uint> v = (uint)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<ulong>)) { Complex<ulong> v = (ulong)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<byte>)) { Complex<byte> v = (byte)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<short>)) { Complex<short> v = (short)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<int>)) { Complex<int> v = new((int)b); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<long>)) { Complex<long> v = (long)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<Half>)) { Complex<Half> v = (Half)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<float>) || typeof(T2) == typeof(ComplexSingle)) { ComplexSingle v = (float)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<double>) || typeof(T2) == typeof(ComplexDouble)) { ComplexDouble v = (double)b; return *(T2*)&v; }
			}
			if (typeof(T1) == typeof(byte))
			{
				byte b = *(byte*)&a;
				if (typeof(T2) == typeof(sbyte)) { sbyte v = (sbyte)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(ushort) || typeof(T2) == typeof(char)) { ushort v = (ushort)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(uint)) { uint v = (uint)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(ulong)) { ulong v = (ulong)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(byte)) { byte v = (byte)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(short)) { short v = (short)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(int)) { int v = (int)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(long)) { long v = (long)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Half)) { Half v = (Half)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(float)) { float v = (float)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(double)) { double v = (double)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<sbyte>)) { Complex<sbyte> v = (sbyte)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<ushort>)) { Complex<ushort> v = (ushort)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<uint>)) { Complex<uint> v = (uint)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<ulong>)) { Complex<ulong> v = (ulong)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<byte>)) { Complex<byte> v = (byte)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<short>)) { Complex<short> v = (short)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<int>)) { Complex<int> v = new((int)b); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<long>)) { Complex<long> v = (long)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<Half>)) { Complex<Half> v = (Half)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<float>) || typeof(T2) == typeof(ComplexSingle)) { ComplexSingle v = (float)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<double>) || typeof(T2) == typeof(ComplexDouble)) { ComplexDouble v = (double)b; return *(T2*)&v; }
			}
			if (typeof(T1) == typeof(short))
			{
				short b = *(short*)&a;
				if (typeof(T2) == typeof(sbyte)) { sbyte v = (sbyte)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(ushort) || typeof(T2) == typeof(char)) { ushort v = (ushort)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(uint)) { uint v = (uint)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(ulong)) { ulong v = (ulong)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(byte)) { byte v = (byte)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(short)) { short v = (short)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(int)) { int v = (int)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(long)) { long v = (long)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Half)) { Half v = (Half)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(float)) { float v = (float)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(double)) { double v = (double)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<sbyte>)) { Complex<sbyte> v = (sbyte)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<ushort>)) { Complex<ushort> v = (ushort)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<uint>)) { Complex<uint> v = (uint)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<ulong>)) { Complex<ulong> v = (ulong)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<byte>)) { Complex<byte> v = (byte)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<short>)) { Complex<short> v = (short)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<int>)) { Complex<int> v = new((int)b); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<long>)) { Complex<long> v = (long)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<Half>)) { Complex<Half> v = (Half)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<float>) || typeof(T2) == typeof(ComplexSingle)) { ComplexSingle v = (float)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<double>) || typeof(T2) == typeof(ComplexDouble)) { ComplexDouble v = (double)b; return *(T2*)&v; }
			}
			if (typeof(T1) == typeof(int))
			{
				int b = *(int*)&a;
				if (typeof(T2) == typeof(sbyte)) { sbyte v = (sbyte)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(ushort) || typeof(T2) == typeof(char)) { ushort v = (ushort)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(uint)) { uint v = (uint)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(ulong)) { ulong v = (ulong)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(byte)) { byte v = (byte)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(short)) { short v = (short)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(int)) { int v = (int)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(long)) { long v = (long)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Half)) { Half v = (Half)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(float)) { float v = (float)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(double)) { double v = (double)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<sbyte>)) { Complex<sbyte> v = (sbyte)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<ushort>)) { Complex<ushort> v = (ushort)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<uint>)) { Complex<uint> v = (uint)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<ulong>)) { Complex<ulong> v = (ulong)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<byte>)) { Complex<byte> v = (byte)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<short>)) { Complex<short> v = (short)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<int>)) { Complex<int> v = new((int)b); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<long>)) { Complex<long> v = (long)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<Half>)) { Complex<Half> v = (Half)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<float>) || typeof(T2) == typeof(ComplexSingle)) { ComplexSingle v = (float)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<double>) || typeof(T2) == typeof(ComplexDouble)) { ComplexDouble v = (double)b; return *(T2*)&v; }
			}
			if (typeof(T1) == typeof(long))
			{
				long b = *(long*)&a;
				if (typeof(T2) == typeof(sbyte)) { sbyte v = (sbyte)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(ushort) || typeof(T2) == typeof(char)) { ushort v = (ushort)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(uint)) { uint v = (uint)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(ulong)) { ulong v = (ulong)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(byte)) { byte v = (byte)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(short)) { short v = (short)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(int)) { int v = (int)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(long)) { long v = (long)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Half)) { Half v = (Half)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(float)) { float v = (float)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(double)) { double v = (double)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<sbyte>)) { Complex<sbyte> v = (sbyte)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<ushort>)) { Complex<ushort> v = (ushort)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<uint>)) { Complex<uint> v = (uint)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<ulong>)) { Complex<ulong> v = (ulong)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<byte>)) { Complex<byte> v = (byte)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<short>)) { Complex<short> v = (short)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<int>)) { Complex<int> v = new((int)b); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<long>)) { Complex<long> v = (long)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<Half>)) { Complex<Half> v = (Half)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<float>) || typeof(T2) == typeof(ComplexSingle)) { ComplexSingle v = (float)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<double>) || typeof(T2) == typeof(ComplexDouble)) { ComplexDouble v = (double)b; return *(T2*)&v; }
			}
			if (typeof(T1) == typeof(float))
			{
				float b = *(float*)&a;
				if (typeof(T2) == typeof(sbyte)) { sbyte v = (sbyte)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(ushort) || typeof(T2) == typeof(char)) { ushort v = (ushort)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(uint)) { uint v = (uint)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(ulong)) { ulong v = (ulong)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(byte)) { byte v = (byte)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(short)) { short v = (short)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(int)) { int v = (int)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(long)) { long v = (long)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Half)) { Half v = (Half)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(float)) { float v = (float)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(double)) { double v = (double)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<sbyte>)) { Complex<sbyte> v = (sbyte)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<ushort>)) { Complex<ushort> v = (ushort)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<uint>)) { Complex<uint> v = (uint)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<ulong>)) { Complex<ulong> v = (ulong)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<byte>)) { Complex<byte> v = (byte)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<short>)) { Complex<short> v = (short)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<int>)) { Complex<int> v = new((int)b); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<long>)) { Complex<long> v = (long)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<Half>)) { Complex<Half> v = (Half)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<float>) || typeof(T2) == typeof(ComplexSingle)) { ComplexSingle v = (float)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<double>) || typeof(T2) == typeof(ComplexDouble)) { ComplexDouble v = (double)b; return *(T2*)&v; }
			}
			if (typeof(T1) == typeof(double))
			{
				double b = *(double*)&a;
				if (typeof(T2) == typeof(sbyte)) { sbyte v = (sbyte)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(ushort) || typeof(T2) == typeof(char)) { ushort v = (ushort)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(uint)) { uint v = (uint)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(ulong)) { ulong v = (ulong)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(byte)) { byte v = (byte)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(short)) { short v = (short)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(int)) { int v = (int)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(long)) { long v = (long)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Half)) { Half v = (Half)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(float)) { float v = (float)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(double)) { double v = (double)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<sbyte>)) { Complex<sbyte> v = (sbyte)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<ushort>)) { Complex<ushort> v = (ushort)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<uint>)) { Complex<uint> v = (uint)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<ulong>)) { Complex<ulong> v = (ulong)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<byte>)) { Complex<byte> v = (byte)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<short>)) { Complex<short> v = (short)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<int>)) { Complex<int> v = new((int)b); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<long>)) { Complex<long> v = (long)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<Half>)) { Complex<Half> v = (Half)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<float>) || typeof(T2) == typeof(ComplexSingle)) { ComplexSingle v = (float)b; return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<double>) || typeof(T2) == typeof(ComplexDouble)) { ComplexDouble v = (double)b; return *(T2*)&v; }
			}
			// pre-defined complex to real / complex
			if (typeof(T1) == typeof(Complex<sbyte>))
			{
				Complex<sbyte> b = *(Complex<sbyte>*)&a;
				if (typeof(T2) == typeof(sbyte)) { sbyte v = (sbyte)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(ushort) || typeof(T2) == typeof(char)) { ushort v = (ushort)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(uint)) { uint v = (uint)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(ulong)) { ulong v = (ulong)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(byte)) { byte v = (byte)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(short)) { short v = (short)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(int)) { int v = (int)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(long)) { long v = (long)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(Half)) { Half v = (Half)b.AbsSingle(); return *(T2*)&v; }
				if (typeof(T2) == typeof(float)) { float v = (float)b.AbsSingle(); return *(T2*)&v; }
				if (typeof(T2) == typeof(double)) { double v = (double)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<sbyte>)) { Complex<sbyte> v = new((sbyte)b.Real, (sbyte)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<ushort>)) { Complex<ushort> v = new((ushort)b.Real, (ushort)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<uint>)) { Complex<uint> v = new((uint)b.Real, (uint)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<ulong>)) { Complex<ulong> v = new((ulong)b.Real, (ulong)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<byte>)) { Complex<byte> v = new((byte)b.Real, (byte)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<short>)) { Complex<short> v = new((short)b.Real, (short)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<int>)) { Complex<int> v = new((int)b.Real, (int)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<long>)) { Complex<long> v = new((long)b.Real, (long)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<Half>)) { Complex<Half> v = new((Half)b.Real, (Half)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<float>) || typeof(T2) == typeof(ComplexSingle))
				{ ComplexSingle v = new((float)b.Real, (float)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<double>) || typeof(T2) == typeof(ComplexDouble))
				{ ComplexDouble v = new((double)b.Real, (double)b.Imag); return *(T2*)&v; }
			}
			if (typeof(T1) == typeof(Complex<ushort>) || typeof(T1) == typeof(Complex<char>))
			{
				Complex<ushort> b = *(Complex<ushort>*)&a;
				if (typeof(T2) == typeof(sbyte)) { sbyte v = (sbyte)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(ushort) || typeof(T2) == typeof(char)) { ushort v = (ushort)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(uint)) { uint v = (uint)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(ulong)) { ulong v = (ulong)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(byte)) { byte v = (byte)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(short)) { short v = (short)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(int)) { int v = (int)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(long)) { long v = (long)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(Half)) { Half v = (Half)b.AbsSingle(); return *(T2*)&v; }
				if (typeof(T2) == typeof(float)) { float v = (float)b.AbsSingle(); return *(T2*)&v; }
				if (typeof(T2) == typeof(double)) { double v = (double)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<sbyte>)) { Complex<sbyte> v = new((sbyte)b.Real, (sbyte)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<ushort>)) { Complex<ushort> v = new((ushort)b.Real, (ushort)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<uint>)) { Complex<uint> v = new((uint)b.Real, (uint)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<ulong>)) { Complex<ulong> v = new((ulong)b.Real, (ulong)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<byte>)) { Complex<byte> v = new((byte)b.Real, (byte)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<short>)) { Complex<short> v = new((short)b.Real, (short)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<int>)) { Complex<int> v = new((int)b.Real, (int)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<long>)) { Complex<long> v = new((long)b.Real, (long)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<Half>)) { Complex<Half> v = new((Half)b.Real, (Half)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<float>) || typeof(T2) == typeof(ComplexSingle))
				{ ComplexSingle v = new((float)b.Real, (float)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<double>) || typeof(T2) == typeof(ComplexDouble))
				{ ComplexDouble v = new((double)b.Real, (double)b.Imag); return *(T2*)&v; }
			}
			if (typeof(T1) == typeof(Complex<uint>))
			{
				Complex<uint> b = *(Complex<uint>*)&a;
				if (typeof(T2) == typeof(sbyte)) { sbyte v = (sbyte)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(ushort) || typeof(T2) == typeof(char)) { ushort v = (ushort)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(uint)) { uint v = (uint)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(ulong)) { ulong v = (ulong)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(byte)) { byte v = (byte)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(short)) { short v = (short)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(int)) { int v = (int)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(long)) { long v = (long)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(Half)) { Half v = (Half)b.AbsSingle(); return *(T2*)&v; }
				if (typeof(T2) == typeof(float)) { float v = (float)b.AbsSingle(); return *(T2*)&v; }
				if (typeof(T2) == typeof(double)) { double v = (double)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<sbyte>)) { Complex<sbyte> v = new((sbyte)b.Real, (sbyte)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<ushort>)) { Complex<ushort> v = new((ushort)b.Real, (ushort)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<uint>)) { Complex<uint> v = new((uint)b.Real, (uint)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<ulong>)) { Complex<ulong> v = new((ulong)b.Real, (ulong)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<byte>)) { Complex<byte> v = new((byte)b.Real, (byte)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<short>)) { Complex<short> v = new((short)b.Real, (short)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<int>)) { Complex<int> v = new((int)b.Real, (int)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<long>)) { Complex<long> v = new((long)b.Real, (long)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<Half>)) { Complex<Half> v = new((Half)b.Real, (Half)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<float>) || typeof(T2) == typeof(ComplexSingle))
				{ ComplexSingle v = new((float)b.Real, (float)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<double>) || typeof(T2) == typeof(ComplexDouble))
				{ ComplexDouble v = new((double)b.Real, (double)b.Imag); return *(T2*)&v; }
			}
			if (typeof(T1) == typeof(Complex<ulong>))
			{
				Complex<ulong> b = *(Complex<ulong>*)&a;
				if (typeof(T2) == typeof(sbyte)) { sbyte v = (sbyte)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(ushort) || typeof(T2) == typeof(char)) { ushort v = (ushort)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(uint)) { uint v = (uint)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(ulong)) { ulong v = (ulong)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(byte)) { byte v = (byte)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(short)) { short v = (short)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(int)) { int v = (int)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(long)) { long v = (long)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(Half)) { Half v = (Half)b.AbsSingle(); return *(T2*)&v; }
				if (typeof(T2) == typeof(float)) { float v = (float)b.AbsSingle(); return *(T2*)&v; }
				if (typeof(T2) == typeof(double)) { double v = (double)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<sbyte>)) { Complex<sbyte> v = new((sbyte)b.Real, (sbyte)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<ushort>)) { Complex<ushort> v = new((ushort)b.Real, (ushort)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<uint>)) { Complex<uint> v = new((uint)b.Real, (uint)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<ulong>)) { Complex<ulong> v = new((ulong)b.Real, (ulong)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<byte>)) { Complex<byte> v = new((byte)b.Real, (byte)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<short>)) { Complex<short> v = new((short)b.Real, (short)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<int>)) { Complex<int> v = new((int)b.Real, (int)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<long>)) { Complex<long> v = new((long)b.Real, (long)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<Half>)) { Complex<Half> v = new((Half)b.Real, (Half)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<float>) || typeof(T2) == typeof(ComplexSingle))
				{ ComplexSingle v = new((float)b.Real, (float)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<double>) || typeof(T2) == typeof(ComplexDouble))
				{ ComplexDouble v = new((double)b.Real, (double)b.Imag); return *(T2*)&v; }
			}
			if (typeof(T1) == typeof(Complex<byte>))
			{
				Complex<byte> b = *(Complex<byte>*)&a;
				if (typeof(T2) == typeof(sbyte)) { sbyte v = (sbyte)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(ushort) || typeof(T2) == typeof(char)) { ushort v = (ushort)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(uint)) { uint v = (uint)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(ulong)) { ulong v = (ulong)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(byte)) { byte v = (byte)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(short)) { short v = (short)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(int)) { int v = (int)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(long)) { long v = (long)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(Half)) { Half v = (Half)b.AbsSingle(); return *(T2*)&v; }
				if (typeof(T2) == typeof(float)) { float v = (float)b.AbsSingle(); return *(T2*)&v; }
				if (typeof(T2) == typeof(double)) { double v = (double)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<sbyte>)) { Complex<sbyte> v = new((sbyte)b.Real, (sbyte)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<ushort>)) { Complex<ushort> v = new((ushort)b.Real, (ushort)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<uint>)) { Complex<uint> v = new((uint)b.Real, (uint)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<ulong>)) { Complex<ulong> v = new((ulong)b.Real, (ulong)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<byte>)) { Complex<byte> v = new((byte)b.Real, (byte)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<short>)) { Complex<short> v = new((short)b.Real, (short)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<int>)) { Complex<int> v = new((int)b.Real, (int)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<long>)) { Complex<long> v = new((long)b.Real, (long)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<Half>)) { Complex<Half> v = new((Half)b.Real, (Half)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<float>) || typeof(T2) == typeof(ComplexSingle))
				{ ComplexSingle v = new((float)b.Real, (float)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<double>) || typeof(T2) == typeof(ComplexDouble))
				{ ComplexDouble v = new((double)b.Real, (double)b.Imag); return *(T2*)&v; }
			}
			if (typeof(T1) == typeof(Complex<short>))
			{
				Complex<short> b = *(Complex<short>*)&a;
				if (typeof(T2) == typeof(sbyte)) { sbyte v = (sbyte)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(ushort) || typeof(T2) == typeof(char)) { ushort v = (ushort)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(uint)) { uint v = (uint)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(ulong)) { ulong v = (ulong)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(byte)) { byte v = (byte)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(short)) { short v = (short)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(int)) { int v = (int)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(long)) { long v = (long)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(Half)) { Half v = (Half)b.AbsSingle(); return *(T2*)&v; }
				if (typeof(T2) == typeof(float)) { float v = (float)b.AbsSingle(); return *(T2*)&v; }
				if (typeof(T2) == typeof(double)) { double v = (double)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<sbyte>)) { Complex<sbyte> v = new((sbyte)b.Real, (sbyte)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<ushort>)) { Complex<ushort> v = new((ushort)b.Real, (ushort)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<uint>)) { Complex<uint> v = new((uint)b.Real, (uint)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<ulong>)) { Complex<ulong> v = new((ulong)b.Real, (ulong)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<byte>)) { Complex<byte> v = new((byte)b.Real, (byte)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<short>)) { Complex<short> v = new((short)b.Real, (short)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<int>)) { Complex<int> v = new((int)b.Real, (int)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<long>)) { Complex<long> v = new((long)b.Real, (long)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<Half>)) { Complex<Half> v = new((Half)b.Real, (Half)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<float>) || typeof(T2) == typeof(ComplexSingle))
				{ ComplexSingle v = new((float)b.Real, (float)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<double>) || typeof(T2) == typeof(ComplexDouble))
				{ ComplexDouble v = new((double)b.Real, (double)b.Imag); return *(T2*)&v; }
			}
			if (typeof(T1) == typeof(Complex<int>))
			{
				Complex<int> b = *(Complex<int>*)&a;
				if (typeof(T2) == typeof(sbyte)) { sbyte v = (sbyte)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(ushort) || typeof(T2) == typeof(char)) { ushort v = (ushort)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(uint)) { uint v = (uint)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(ulong)) { ulong v = (ulong)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(byte)) { byte v = (byte)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(short)) { short v = (short)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(int)) { int v = (int)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(long)) { long v = (long)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(Half)) { Half v = (Half)b.AbsSingle(); return *(T2*)&v; }
				if (typeof(T2) == typeof(float)) { float v = (float)b.AbsSingle(); return *(T2*)&v; }
				if (typeof(T2) == typeof(double)) { double v = (double)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<sbyte>)) { Complex<sbyte> v = new((sbyte)b.Real, (sbyte)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<ushort>)) { Complex<ushort> v = new((ushort)b.Real, (ushort)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<uint>)) { Complex<uint> v = new((uint)b.Real, (uint)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<ulong>)) { Complex<ulong> v = new((ulong)b.Real, (ulong)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<byte>)) { Complex<byte> v = new((byte)b.Real, (byte)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<short>)) { Complex<short> v = new((short)b.Real, (short)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<int>)) { Complex<int> v = new((int)b.Real, (int)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<long>)) { Complex<long> v = new((long)b.Real, (long)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<Half>)) { Complex<Half> v = new((Half)b.Real, (Half)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<float>) || typeof(T2) == typeof(ComplexSingle))
				{ ComplexSingle v = new((float)b.Real, (float)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<double>) || typeof(T2) == typeof(ComplexDouble))
				{ ComplexDouble v = new((double)b.Real, (double)b.Imag); return *(T2*)&v; }
			}
			if (typeof(T1) == typeof(Complex<long>))
			{
				Complex<long> b = *(Complex<long>*)&a;
				if (typeof(T2) == typeof(sbyte)) { sbyte v = (sbyte)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(ushort) || typeof(T2) == typeof(char)) { ushort v = (ushort)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(uint)) { uint v = (uint)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(ulong)) { ulong v = (ulong)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(byte)) { byte v = (byte)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(short)) { short v = (short)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(int)) { int v = (int)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(long)) { long v = (long)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(Half)) { Half v = (Half)b.AbsSingle(); return *(T2*)&v; }
				if (typeof(T2) == typeof(float)) { float v = (float)b.AbsSingle(); return *(T2*)&v; }
				if (typeof(T2) == typeof(double)) { double v = (double)b.AbsDouble(); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<sbyte>)) { Complex<sbyte> v = new((sbyte)b.Real, (sbyte)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<ushort>)) { Complex<ushort> v = new((ushort)b.Real, (ushort)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<uint>)) { Complex<uint> v = new((uint)b.Real, (uint)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<ulong>)) { Complex<ulong> v = new((ulong)b.Real, (ulong)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<byte>)) { Complex<byte> v = new((byte)b.Real, (byte)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<short>)) { Complex<short> v = new((short)b.Real, (short)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<int>)) { Complex<int> v = new((int)b.Real, (int)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<long>)) { Complex<long> v = new((long)b.Real, (long)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<Half>)) { Complex<Half> v = new((Half)b.Real, (Half)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<float>) || typeof(T2) == typeof(ComplexSingle))
				{ ComplexSingle v = new((float)b.Real, (float)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<double>) || typeof(T2) == typeof(ComplexDouble))
				{ ComplexDouble v = new((double)b.Real, (double)b.Imag); return *(T2*)&v; }
			}
			if (typeof(T1) == typeof(Complex<float>) || typeof(T1) == typeof(ComplexSingle))
			{
				ComplexSingle b = *(ComplexSingle*)&a;
				if (typeof(T2) == typeof(sbyte)) { sbyte v = (sbyte)b.Abs(); return *(T2*)&v; }
				if (typeof(T2) == typeof(ushort) || typeof(T2) == typeof(char)) { ushort v = (ushort)b.Abs(); return *(T2*)&v; }
				if (typeof(T2) == typeof(uint)) { uint v = (uint)b.Abs(); return *(T2*)&v; }
				if (typeof(T2) == typeof(ulong)) { ulong v = (ulong)b.Abs(); return *(T2*)&v; }
				if (typeof(T2) == typeof(byte)) { byte v = (byte)b.Abs(); return *(T2*)&v; }
				if (typeof(T2) == typeof(short)) { short v = (short)b.Abs(); return *(T2*)&v; }
				if (typeof(T2) == typeof(int)) { int v = (int)b.Abs(); return *(T2*)&v; }
				if (typeof(T2) == typeof(long)) { long v = (long)b.Abs(); return *(T2*)&v; }
				if (typeof(T2) == typeof(Half)) { Half v = (Half)b.Abs(); return *(T2*)&v; }
				if (typeof(T2) == typeof(float)) { float v = (float)b.Abs(); return *(T2*)&v; }
				if (typeof(T2) == typeof(double)) { double v = (double)b.Abs(); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<sbyte>)) { Complex<sbyte> v = new((sbyte)b.Real, (sbyte)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<ushort>)) { Complex<ushort> v = new((ushort)b.Real, (ushort)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<uint>)) { Complex<uint> v = new((uint)b.Real, (uint)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<ulong>)) { Complex<ulong> v = new((ulong)b.Real, (ulong)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<byte>)) { Complex<byte> v = new((byte)b.Real, (byte)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<short>)) { Complex<short> v = new((short)b.Real, (short)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<int>)) { Complex<int> v = new((int)b.Real, (int)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<long>)) { Complex<long> v = new((long)b.Real, (long)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<Half>)) { Complex<Half> v = new((Half)b.Real, (Half)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<float>) || typeof(T2) == typeof(ComplexSingle))
				{ ComplexSingle v = new((float)b.Real, (float)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<double>) || typeof(T2) == typeof(ComplexDouble))
				{ ComplexDouble v = new((double)b.Real, (double)b.Imag); return *(T2*)&v; }
			}
			if (typeof(T1) == typeof(Complex<double>) || typeof(T1) == typeof(ComplexDouble))
			{
				ComplexDouble b = *(ComplexDouble*)&a;
				if (typeof(T2) == typeof(sbyte)) { sbyte v = (sbyte)b.Abs(); return *(T2*)&v; }
				if (typeof(T2) == typeof(ushort) || typeof(T2) == typeof(char)) { ushort v = (ushort)b.Abs(); return *(T2*)&v; }
				if (typeof(T2) == typeof(uint)) { uint v = (uint)b.Abs(); return *(T2*)&v; }
				if (typeof(T2) == typeof(ulong)) { ulong v = (ulong)b.Abs(); return *(T2*)&v; }
				if (typeof(T2) == typeof(byte)) { byte v = (byte)b.Abs(); return *(T2*)&v; }
				if (typeof(T2) == typeof(short)) { short v = (short)b.Abs(); return *(T2*)&v; }
				if (typeof(T2) == typeof(int)) { int v = (int)b.Abs(); return *(T2*)&v; }
				if (typeof(T2) == typeof(long)) { long v = (long)b.Abs(); return *(T2*)&v; }
				if (typeof(T2) == typeof(Half)) { Half v = (Half)b.Abs(); return *(T2*)&v; }
				if (typeof(T2) == typeof(float)) { float v = (float)b.Abs(); return *(T2*)&v; }
				if (typeof(T2) == typeof(double)) { double v = (double)b.Abs(); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<sbyte>)) { Complex<sbyte> v = new((sbyte)b.Real, (sbyte)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<ushort>)) { Complex<ushort> v = new((ushort)b.Real, (ushort)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<uint>)) { Complex<uint> v = new((uint)b.Real, (uint)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<ulong>)) { Complex<ulong> v = new((ulong)b.Real, (ulong)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<byte>)) { Complex<byte> v = new((byte)b.Real, (byte)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<short>)) { Complex<short> v = new((short)b.Real, (short)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<int>)) { Complex<int> v = new((int)b.Real, (int)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<long>)) { Complex<long> v = new((long)b.Real, (long)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<Half>)) { Complex<Half> v = new((Half)b.Real, (Half)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<float>) || typeof(T2) == typeof(ComplexSingle))
				{ ComplexSingle v = new((float)b.Real, (float)b.Imag); return *(T2*)&v; }
				if (typeof(T2) == typeof(Complex<double>) || typeof(T2) == typeof(ComplexDouble))
				{ ComplexDouble v = new((double)b.Real, (double)b.Imag); return *(T2*)&v; }
			}

			// otherwise
			return ConstConvert<T1, T2>.ConvertDelegate.Invoke(a);
		}

		/// <summary>
		/// Generically convert the input number <paramref name="a"/> of type <typeparamref name="T"/> to <see cref="double"/>.
		/// </summary>
		/// <typeparam name="T">The convert source type</typeparam>
		/// <param name="a">The number to be converted to <see cref="double"/></param>
		/// <returns>The converted number as a <see cref="double"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static double ToDouble<T>(this T a) where T : unmanaged => NativeConvert<T, double>(a);

		/// <summary>
		/// Generically convert the input number <paramref name="a"/> of type <see cref="double"/> to type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The convert target type</typeparam>
		/// <param name="a">The number to be converted to <typeparamref name="T"/></param>
		/// <returns>The converted number as a <typeparamref name="T"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T FromDouble<T>(this double a) where T : unmanaged => NativeConvert<double, T>(a);

		/// <summary>
		/// Generically convert the input number <paramref name="a"/> of type <typeparamref name="T"/> to <see cref="long"/>.
		/// </summary>
		/// <typeparam name="T">The convert source type</typeparam>
		/// <param name="a">The number to be converted to <see cref="long"/></param>
		/// <returns>The converted number as a <see cref="long"/></returns>
		/// <exception cref="TypeMismatchException">If <typeparamref name="T"/> is not an integral type</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static long ToLong<T>(this T a) where T : unmanaged => Const<T>.IsIntegralType ? NativeConvert<T, long>(a) : throw new TypeMismatchException(typeof(T), TypeMismatchException.MismatchReason.NotInteger);

		/// <summary>
		/// Generically convert the input number <paramref name="a"/> of type <typeparamref name="T"/> to <see cref="int"/>.
		/// </summary>
		/// <typeparam name="T">The convert source type</typeparam>
		/// <param name="a">The number to be converted to <see cref="int"/></param>
		/// <returns>The converted number as a <see cref="int"/></returns>
		/// <exception cref="TypeMismatchException">If <typeparamref name="T"/> is not an integral type</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int ToInt<T>(this T a) where T : unmanaged => Const<T>.IsIntegralType ? NativeConvert<T, int>(a) : throw new TypeMismatchException(typeof(T), TypeMismatchException.MismatchReason.NotInteger);

		/// <summary>
		/// Generically convert the input number <paramref name="a"/> of type <see cref="long"/> to type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The convert target type</typeparam>
		/// <param name="a">The number to be converted to <typeparamref name="T"/></param>
		/// <returns>The converted number as a <typeparamref name="T"/></returns>
		/// <exception cref="TypeMismatchException">If <typeparamref name="T"/> is not an integral type</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T FromLong<T>(this long a) where T : unmanaged => Const<T>.IsIntegralType ? NativeConvert<long, T>(a) : throw new TypeMismatchException(typeof(T), TypeMismatchException.MismatchReason.NotInteger);
		
		/// <summary>
		/// Get the real part (or itself if it is not a complex) of the given generic numeric value <paramref name="a"/>.
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="a">The number to get real part</param>
		/// <returns>The real part of <paramref name="a"/> as a <see cref="double"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe double NativeRealPart<T>(this T a) where T : unmanaged
		{
			if (typeof(T) == typeof(sbyte)) return (double)(*(sbyte*)&a);
			if (typeof(T) == typeof(ushort) || typeof(T) == typeof(char)) return (double)(*(ushort*)&a);
			if (typeof(T) == typeof(uint)) return (double)(*(uint*)&a);
			if (typeof(T) == typeof(ulong)) return (double)(*(ulong*)&a);
			if (typeof(T) == typeof(byte)) return (double)(*(byte*)&a);
			if (typeof(T) == typeof(short)) return (double)(*(short*)&a);
			if (typeof(T) == typeof(int)) return (double)(*(int*)&a);
			if (typeof(T) == typeof(long)) return (double)(*(long*)&a);
			if (typeof(T) == typeof(Half)) return (double)(*(Half*)&a);
			if (typeof(T) == typeof(float)) return (double)(*(float*)&a);
			if (typeof(T) == typeof(double)) return (double)(*(double*)&a);
			if (typeof(T) == typeof(Complex<sbyte>)) return (double)(*(Complex<sbyte>*)&a).Real;
			if (typeof(T) == typeof(Complex<ushort>) || typeof(T) == typeof(Complex<char>)) return (double)(*(Complex<ushort>*)&a).Real;
			if (typeof(T) == typeof(Complex<uint>)) return (double)(*(Complex<uint>*)&a).Real;
			if (typeof(T) == typeof(Complex<ulong>)) return (double)(*(Complex<ulong>*)&a).Real;
			if (typeof(T) == typeof(Complex<byte>)) return (double)(*(Complex<byte>*)&a).Real;
			if (typeof(T) == typeof(Complex<short>)) return (double)(*(Complex<short>*)&a).Real;
			if (typeof(T) == typeof(Complex<int>)) return (double)(*(Complex<int>*)&a).Real;
			if (typeof(T) == typeof(Complex<long>)) return (double)(*(Complex<long>*)&a).Real;
			if (typeof(T) == typeof(Complex<Half>)) return (double)(*(Complex<Half>*)&a).Real;
			if (typeof(T) == typeof(Complex<float>) || typeof(T) == typeof(ComplexSingle)) return (double)(*(ComplexSingle*)&a).Real;
			if (typeof(T) == typeof(Complex<double>) || typeof(T) == typeof(ComplexDouble)) return (double)(*(ComplexDouble*)&a).Real;
			// else
			if (!Const<T>.IsComplex)
				return Const<T>.ToDoubleDelegate.Invoke(a);
			else
				return Const<T>.RealPartDelegate.Invoke(a);
		}

		/// <summary>
		/// Get the imaginary part (or 0 if it is not a complex) of the given generic numeric value <paramref name="a"/>.
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="a">The number to get real part</param>
		/// <returns>The imaginary part of <paramref name="a"/> as a <see cref="double"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe double NativeImagPart<T>(this T a) where T : unmanaged
		{
			if (typeof(T) == typeof(sbyte) ||
				typeof(T) == typeof(ushort) ||
				typeof(T) == typeof(char) ||
				typeof(T) == typeof(uint) ||
				typeof(T) == typeof(ulong) ||
				typeof(T) == typeof(byte) ||
				typeof(T) == typeof(short) ||
				typeof(T) == typeof(int) ||
				typeof(T) == typeof(long) ||
				typeof(T) == typeof(Half) ||
				typeof(T) == typeof(float) ||
				typeof(T) == typeof(double))
				return 0;
			if (typeof(T) == typeof(Complex<sbyte>)) return (double)(*(Complex<sbyte>*)&a).Imag;
			if (typeof(T) == typeof(Complex<ushort>) || typeof(T) == typeof(Complex<char>)) return (double)(*(Complex<ushort>*)&a).Imag;
			if (typeof(T) == typeof(Complex<uint>)) return (double)(*(Complex<uint>*)&a).Imag;
			if (typeof(T) == typeof(Complex<ulong>)) return (double)(*(Complex<ulong>*)&a).Imag;
			if (typeof(T) == typeof(Complex<byte>)) return (double)(*(Complex<byte>*)&a).Imag;
			if (typeof(T) == typeof(Complex<short>)) return (double)(*(Complex<short>*)&a).Imag;
			if (typeof(T) == typeof(Complex<int>)) return (double)(*(Complex<int>*)&a).Imag;
			if (typeof(T) == typeof(Complex<long>)) return (double)(*(Complex<long>*)&a).Imag;
			if (typeof(T) == typeof(Complex<Half>)) return (double)(*(Complex<Half>*)&a).Real;
			if (typeof(T) == typeof(Complex<float>) || typeof(T) == typeof(ComplexSingle)) return (double)(*(ComplexSingle*)&a).Imag;
			if (typeof(T) == typeof(Complex<double>) || typeof(T) == typeof(ComplexDouble)) return (double)(*(ComplexDouble*)&a).Imag;
			// else
			if (!Const<T>.IsComplex)
				return 0;
			else
				return Const<T>.RealPartDelegate.Invoke(a);
		}

		/// <summary>
		/// Get the complex conjugate (or 0 if it is not a complex) of the given generic numeric value <paramref name="a"/>.
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="a">The number to be conjugated</param>
		/// <returns>The complex conjugate of <paramref name="a"/> as a <typeparamref name="T"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe T NativeConjugate<T>(this T a) where T : unmanaged
		{
			if (typeof(T) == typeof(sbyte) ||
				typeof(T) == typeof(ushort) ||
				typeof(T) == typeof(char) ||
				typeof(T) == typeof(uint) ||
				typeof(T) == typeof(ulong) ||
				typeof(T) == typeof(byte) ||
				typeof(T) == typeof(short) ||
				typeof(T) == typeof(int) ||
				typeof(T) == typeof(long) ||
				typeof(T) == typeof(Half) ||
				typeof(T) == typeof(float) ||
				typeof(T) == typeof(double))
				return a;
			if (typeof(T) == typeof(Complex<sbyte>)) { var v = (*(Complex<sbyte>*)&a).Conjugate(); return *(T*)&v; }
			if (typeof(T) == typeof(Complex<ushort>) || typeof(T) == typeof(Complex<char>)) { var v = (*(Complex<ushort>*)&a).Conjugate(); return *(T*)&v; }
			if (typeof(T) == typeof(Complex<uint>)) { var v = (*(Complex<uint>*)&a).Conjugate(); return *(T*)&v; }
			if (typeof(T) == typeof(Complex<ulong>)) { var v = (*(Complex<ulong>*)&a).Conjugate(); return *(T*)&v; }
			if (typeof(T) == typeof(Complex<byte>)) { var v = (*(Complex<byte>*)&a).Conjugate(); return *(T*)&v; }
			if (typeof(T) == typeof(Complex<short>)) { var v = (*(Complex<short>*)&a).Conjugate(); return *(T*)&v; }
			if (typeof(T) == typeof(Complex<int>)) { var v = (*(Complex<int>*)&a).Conjugate(); return *(T*)&v; }
			if (typeof(T) == typeof(Complex<long>)) { var v = (*(Complex<long>*)&a).Conjugate(); return *(T*)&v; }
			if (typeof(T) == typeof(Complex<Half>)) { var v = (*(Complex<Half>*)&a).Conjugate(); return *(T*)&v; }
			if (typeof(T) == typeof(Complex<float>) || typeof(T) == typeof(ComplexSingle)) { var v = (*(ComplexSingle*)&a).Conjugate(); return *(T*)&v; }
			if (typeof(T) == typeof(Complex<double>) || typeof(T) == typeof(ComplexDouble)) { var v = (*(ComplexDouble*)&a).Conjugate(); return *(T*)&v; }
			// else
			if (!Const<T>.IsComplex)
				return a;
			else
				return Const<T>.ConjugateDelegate.Invoke(a);
		}

		/// <summary>
		/// Get the real part (or itself if it is not a complex) of the given generic numeric value <paramref name="a"/>.
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="a">The number to get real part</param>
		/// <returns>The real part of <paramref name="a"/> as a <see cref="double"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe double NativeAbsolute<T>(this T a) where T : unmanaged
		{
			if (typeof(T) == typeof(byte)) return (double)(*(byte*)&a);
			if (typeof(T) == typeof(ushort) || typeof(T) == typeof(char)) return (double)(*(ushort*)&a);
			if (typeof(T) == typeof(uint)) return (double)(*(uint*)&a);
			if (typeof(T) == typeof(ulong)) return (double)(*(ulong*)&a);
			if (typeof(T) == typeof(sbyte)) return Math.Abs(*(sbyte*)&a);
			if (typeof(T) == typeof(short)) return Math.Abs(*(short*)&a);
			if (typeof(T) == typeof(int)) return Math.Abs(*(int*)&a);
			if (typeof(T) == typeof(long)) return Math.Abs(*(long*)&a);
			if (typeof(T) == typeof(Half)) return Math.Abs((double)(*(Half*)&a));
			if (typeof(T) == typeof(float)) return Math.Abs(*(float*)&a);
			if (typeof(T) == typeof(double)) return Math.Abs(*(double*)&a);
			if (typeof(T) == typeof(Complex<sbyte>)) return (*(Complex<sbyte>*)&a).AbsDouble();
			if (typeof(T) == typeof(Complex<ushort>) || typeof(T) == typeof(Complex<char>)) return (*(Complex<ushort>*)&a).AbsDouble();
			if (typeof(T) == typeof(Complex<uint>)) return (*(Complex<uint>*)&a).AbsDouble();
			if (typeof(T) == typeof(Complex<ulong>)) return (*(Complex<ulong>*)&a).AbsDouble();
			if (typeof(T) == typeof(Complex<byte>)) return (*(Complex<byte>*)&a).AbsDouble();
			if (typeof(T) == typeof(Complex<short>)) return (*(Complex<short>*)&a).AbsDouble();
			if (typeof(T) == typeof(Complex<int>)) return (*(Complex<int>*)&a).AbsDouble();
			if (typeof(T) == typeof(Complex<long>)) return (*(Complex<long>*)&a).AbsDouble();
			if (typeof(T) == typeof(Complex<Half>)) return (*(Complex<Half>*)&a).AbsDouble();
			if (typeof(T) == typeof(Complex<float>) || typeof(T) == typeof(ComplexSingle)) return (*(ComplexSingle*)&a).Abs();
			if (typeof(T) == typeof(Complex<double>) || typeof(T) == typeof(ComplexDouble)) return (*(ComplexDouble*)&a).Abs();
			// else
			return Const<T>.AbsoluteDelegate.Invoke(a);
		}
		#endregion
		#endregion
	}
	#endregion
}
