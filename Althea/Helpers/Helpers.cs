using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Althea.Linq;
using Althea.NativeTypes;
using Althea.Resources;


namespace Althea.Helpers
{
	#region internal helpers
	internal static class InternalHelper
	{
		internal static bool IsWindows {
			get {
				int p = (int)Environment.OSVersion.Platform;
				return (p != 4) && (p != 6) && (p != 128);
			}
		}

		// TODO: move to Althea.LinearAlgebra
		internal static MatrixOperation CheckOP<T>(this MatrixOperation input, Arrays.IMatrix<T> mat) where T : unmanaged, IFormattable, IEquatable<T>
		{
			if (mat is null)
				return default;
			bool isComplex = default(T).IsComplex();
			switch (input)
			{
				case MatrixOperation.Transpose:
					if (mat.Hermitian && !isComplex)
						return MatrixOperation.None;
					else
						return MatrixOperation.Transpose;
				case MatrixOperation.ConjugateTranspose:
					if (mat.Hermitian)
						return MatrixOperation.None;
					else if (!isComplex)
						return MatrixOperation.Transpose;
					else
						return MatrixOperation.ConjugateTranspose;
				case MatrixOperation.Conjugate:
					if (!isComplex)
						return MatrixOperation.None;
					else if (mat.Hermitian)
						return MatrixOperation.Transpose;
					else
						return MatrixOperation.Conjugate;
				default:
					return default;
			}
		}


		// TODO: move to native codes?
		private static readonly double	doublePrecision13 = Math.Pow(General.Common.DoubleMachinePrecision, 1.0 / 3),
										singlePrecision23 = Math.Pow(General.Common.SingleMachinePrecision, 2.0 / 3);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static int ApproxIndexOfSingle(this Complex<double>[] array, Complex<double> value)
		{
			for (int i = 0; i < array.Length; i++)
			{
				var diff = array[i] - value;
				double diffMax = Math.Max(Math.Abs(diff.Real), Math.Abs(diff.Imag));
				double max = Math.Max(Math.Abs(array[i].Real), Math.Abs(array[i].Imag));
				if (diffMax / max < singlePrecision23)
					return i;
			}
			return -1;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static int ApproxIndexOfDouble(this Complex<double>[] array, Complex<double> value)
		{
			for (int i = 0; i < array.Length; i++)
			{
				var diff = array[i] - value;
				double diffMax = Math.Max(Math.Abs(diff.Real), Math.Abs(diff.Imag));
				double max = Math.Max(Math.Abs(array[i].Real), Math.Abs(array[i].Imag));
				if (diffMax / max < doublePrecision13)
					return i;
			}
			return -1;
		}
	}
	#endregion

	#region reflection helpers
	/// <summary>
	/// A static class that contains helper functions using reflections
	/// </summary>
	public static class ReflectionHelper
	{
		private static readonly Dictionary<(Type t1, Type t2), Delegate> _conversionCache = new Dictionary<(Type t1, Type t2), Delegate>();

		/// <summary>
		/// Generic convert <paramref name="obj"/> of <typeparamref name="T1"/> to <typeparamref name="T2"/> by finding possible explicit or implicit conversion operators.
		/// </summary>
		/// <typeparam name="T1">input type</typeparam>
		/// <typeparam name="T2">output type</typeparam>
		/// <param name="obj">input object</param>
		/// <returns>object converted by explicit or implicit operators</returns>
		public static T2 ReflectionConvert<T1, T2>(T1 obj)
		{
			static bool predicator(System.Reflection.MethodInfo m) => (m.Name == "op_Explicit" || m.Name == "op_Implicit") &&
																		m.ReturnType == typeof(T2) && m.GetParameters().Length == 1 &&
																		m.GetParameters()[0].ParameterType.IsAssignableFrom(typeof(T1));

			Type t1 = typeof(T1), t2 = typeof(T2);
			if (!_conversionCache.ContainsKey((t1, t2)))
			{
				var conversionOperator = t1.GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
												 .Where(predicator).FirstOrDefault();
				conversionOperator ??= t2.GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
												 .Where(predicator).FirstOrDefault();
				if (conversionOperator is null)
					throw new MethodAccessException(Other.CannotFindMethod);
				_conversionCache.Add((t1, t2), conversionOperator.CreateDelegate<Converter<T1, T2>>());
			}
			return ((Converter<T1, T2>)_conversionCache[(t1, t2)]).Invoke(obj);
		}
	}
	#endregion

	/// <summary>
	/// A static class that contains general purposed extension helper methods
	/// </summary>
	public static class ExtensionHelper
	{
		#region integer related
		/// <summary>
		/// Get the combination number of given integers
		/// </summary>
		/// <param name="n">The length of any combination</param>
		/// <param name="N">The number of all potential values</param>
		/// <returns>0 if <paramref name="n"/> &gt; <paramref name="N"/> or <paramref name="n"/> == 0 or <paramref name="N"/> == 0, the binomial (<paramref name="N"/>, <paramref name="n"/>) otherwise</returns>
		/// <exception cref="OverflowException">If an overflow happened during the calculation</exception>
		public static ulong CombinationNumber(this uint n, uint N)
		{
			if (n < N || n == 0 || N == 0)
				return 0;
			if (n == 1)
				return N;
			if (n == N)
				return 1;
			// otherwise
			ulong ret = 1;
			uint c = N - n;
			c = Math.Min(c, n);
			for (uint i = N - c + 1; i <= N; i++)
				ret = checked(ret * i);
			for (uint i = 2; i <= c; i++)
				ret /= i;
			return ret;
		}

		// Ignore Spelling: nd
		/// <summary>
		/// Output an integer under 100 as a cardinality number, e.g. 0 -> 1st, 51 -> 52nd
		/// </summary>
		/// <param name="a">the input number</param>
		/// <returns>the ordinal representation string</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string ToOrdinal(this int a)
		{
			if (a >= 100)
				throw new ArgumentOutOfRangeException(nameof(a));
			a++;
			int b = a % 100 / 10, c = a % 10;
			if (c <= 3 && b != 1)
			{
				return c switch
				{
					0 => $"{a}th",
					1 => $"{a}st",
					2 => $"{a}nd",
					3 => $"{a}rd",
					_ => "",
				};
			}
			else
			{
				return $"{a}th";
			}
		}

		/// <summary>
		/// Is the input integer a perfect square or not.
		/// </summary>
		/// <param name="input">the input integer</param>
		/// <returns>Whether <paramref name="input"/> is perfect square or not.</returns>
		public static bool IsPerfectSquare(this long input)
		{
			long closestRoot = (long)Math.Sqrt(input);
			return input == closestRoot * closestRoot;
		}

		/// <summary>
		/// Whether the input integer is a power of 2
		/// </summary>
		/// <param name="x">the input integer</param>
		/// <returns>Whether <paramref name="x"/> is a power of 2</returns>
		public static bool IsPowerOfTwo(this ulong x)
		{
			if (x == 1) return true;
			return (x > 1) && ((x & (x - 1)) == 0);
		}

		/// <summary>
		/// Whether the input integer is a power of 2
		/// </summary>
		/// <param name="x">the input integer</param>
		/// <returns>Whether <paramref name="x"/> is a power of 2</returns>
		public static bool IsPowerOfTwo(this long x)
		{
			if (x == 1) return true;
			return (x > 1) && ((x & (x - 1)) == 0);
		}

		/// <summary>
		/// Whether the input integer is a power of 2
		/// </summary>
		/// <param name="x">the input integer</param>
		/// <returns>Whether <paramref name="x"/> is a power of 2</returns>
		public static bool IsPowerOfTwo(this uint x)
		{
			if (x == 1) return true;
			return (x > 1) && ((x & (x - 1)) == 0);
		}

		/// <summary>
		/// Whether the input integer is a power of 2
		/// </summary>
		/// <param name="x">the input integer</param>
		/// <returns>Whether <paramref name="x"/> is a power of 2</returns>
		public static bool IsPowerOfTwo(this int x)
		{
			if (x == 1) return true;
			return (x > 1) && ((x & (x - 1)) == 0);
		}

		/// <summary>
		/// Whether the input integer is a power of 2
		/// </summary>
		/// <param name="x">the input integer</param>
		/// <returns>Whether <paramref name="x"/> is a power of 2</returns>
		public static bool IsPowerOfTwo(this ushort x)
		{
			if (x == 1) return true;
			return (x > 1) && ((x & (x - 1)) == 0);
		}

		/// <summary>
		/// Whether the input integer is a power of 2
		/// </summary>
		/// <param name="x">the input integer</param>
		/// <returns>Whether <paramref name="x"/> is a power of 2</returns>
		public static bool IsPowerOfTwo(this short x)
		{
			if (x == 1) return true;
			return (x > 1) && ((x & (x - 1)) == 0);
		}

		/// <summary>
		/// Get the nearest power of 2 integer of the input integer
		/// </summary>
		/// <param name="x">the input integer</param>
		/// <returns><paramref name="x"/>'s the nearest power of 2</returns>
		public static uint NearestPowerOfTwo(this uint x)
		{
			x--;
			x |= x >> 1;
			x |= x >> 2;
			x |= x >> 4;
			x |= x >> 8;
			x |= x >> 16;
			return x + 1;
		}

		/// <summary>
		/// Get the nearest power of 2 integer of the input integer
		/// </summary>
		/// <param name="x">the input integer</param>
		/// <returns><paramref name="x"/>'s the nearest power of 2</returns>
		public static int NearestPowerOfTwo(this int x)
		{
			uint xx = unchecked((uint)x);
			return Convert.ToInt32(xx.NearestPowerOfTwo());
		}

		/// <summary>
		/// Get the nearest power of 2 integer of the input integer
		/// </summary>
		/// <param name="x">the input integer</param>
		/// <returns><paramref name="x"/>'s the nearest power of 2</returns>
		public static ulong NearestPowerOfTwo(this ulong x)
		{
			x--;
			x |= x >> 1;
			x |= x >> 2;
			x |= x >> 4;
			x |= x >> 8;
			x |= x >> 16;
			x |= x >> 32;
			return x + 1;
		}

		/// <summary>
		/// Get the nearest power of 2 integer of the input integer
		/// </summary>
		/// <param name="x">the input integer</param>
		/// <returns><paramref name="x"/>'s the nearest power of 2</returns>
		public static long NearestPowerOfTwo(this long x)
		{
			ulong xx = unchecked((ulong)x);
			return Convert.ToInt64(xx.NearestPowerOfTwo());
		}

		/// <summary>
		/// Get the floor round of log2(<paramref name="input"/>)
		/// </summary>
		/// <param name="input">input number</param>
		/// <returns>the nearest log2 of <paramref name="input"/></returns>
		public static sbyte Log2(this short input)
		{
			if (input <= 0)
				return -1;
			sbyte targetlevel = 0;
			while ((input >>= 1) != 0)
				++targetlevel;
			return targetlevel;
		}

		/// <summary>
		/// Get the floor round of log2(<paramref name="input"/>)
		/// </summary>
		/// <param name="input">input number</param>
		/// <returns>the nearest log2 of <paramref name="input"/></returns>
		public static sbyte Log2(this ushort input)
		{
			if (input <= 0)
				return -1;
			sbyte targetlevel = 0;
			while ((input >>= 1) != 0)
				++targetlevel;
			return targetlevel;
		}

		/// <summary>
		/// Get the floor round of log2(<paramref name="input"/>)
		/// </summary>
		/// <param name="input">input number</param>
		/// <returns>the nearest log2 of <paramref name="input"/></returns>
		public static sbyte Log2(this int input)
		{
			if (input <= 0)
				return -1;
			sbyte targetlevel = 0;
			while ((input >>= 1) != 0)
				++targetlevel;
			return targetlevel;
		}

		/// <summary>
		/// Get the floor round of log2(<paramref name="input"/>)
		/// </summary>
		/// <param name="input">input number</param>
		/// <returns>the nearest log2 of <paramref name="input"/></returns>
		public static sbyte Log2(this uint input)
		{
			if (input <= 0)
				return -1;
			sbyte targetlevel = 0;
			while ((input >>= 1) != 0)
				++targetlevel;
			return targetlevel;
		}

		/// <summary>
		/// Get the floor round of log2(<paramref name="input"/>)
		/// </summary>
		/// <param name="input">input number</param>
		/// <returns>the nearest log2 of <paramref name="input"/></returns>
		public static sbyte Log2(this long input)
		{
			if (input <= 0)
				return -1;
			sbyte targetlevel = 0;
			while ((input >>= 1) != 0)
				++targetlevel;
			return targetlevel;
		}

		/// <summary>
		/// Get the floor round of log2(<paramref name="input"/>)
		/// </summary>
		/// <param name="input">input number</param>
		/// <returns>the nearest log2 of <paramref name="input"/></returns>
		public static sbyte Log2(this ulong input)
		{
			if (input <= 0)
				return -1;
			sbyte targetlevel = 0;
			while ((input >>= 1) != 0)
				++targetlevel;
			return targetlevel;
		}

		/// <summary>
		/// Count the <paramref name="input"/>'s bits which are set to 1
		/// </summary>
		/// <param name="input">input integer</param>
		/// <returns>the number <paramref name="input"/>'s bits set</returns>
		public static byte CountBitSet(this short input)
		{
			byte count = 0;
			int i = input;
			for (; i != 0; count++)
			{
				i &= i - 1;
			}
			return count;
		}

		/// <summary>
		/// Count the <paramref name="input"/>'s bits which are set to 1
		/// </summary>
		/// <param name="input">input integer</param>
		/// <returns>the number <paramref name="input"/>'s bits set</returns>
		public static byte CountBitSet(this ushort input)
		{
			byte count = 0;
			uint i = input;
			for (; i != 0; count++)
			{
				i &= i - 1;
			}
			return count;
		}

		/// <summary>
		/// Count the <paramref name="input"/>'s bits which are set to 1
		/// </summary>
		/// <param name="input">input integer</param>
		/// <returns>the number <paramref name="input"/>'s bits set</returns>
		public static byte CountBitSet(this int input)
		{
			return unchecked((uint)input).CountBitSet();
		}

		/// <summary>
		/// Count the <paramref name="input"/>'s bits which are set to 1
		/// </summary>
		/// <param name="input">input integer</param>
		/// <returns>the number <paramref name="input"/>'s bits set</returns>
		public static byte CountBitSet(this uint input)
		{
			input -= (input >> 1) & 0x5555_5555U;
			input = (input & 0x3333_3333U) + ((input >> 2) & 0x3333_3333U);
			input = (input + (input >> 4)) & 0x0F0F_0F0FU;
			return (byte)((input * 0x0101_0101U) >> 24);
		}

		/// <summary>
		/// Count the <paramref name="input"/>'s bits which are set to 1
		/// </summary>
		/// <param name="input">input integer</param>
		/// <returns>the number <paramref name="input"/>'s bits set</returns>
		public static byte CountBitSet(this long input)
		{
			return unchecked((ulong)input).CountBitSet();
		}

		/// <summary>
		/// Count the <paramref name="input"/>'s bits which are set to 1
		/// </summary>
		/// <param name="input">input integer</param>
		/// <returns>the number <paramref name="input"/>'s bits set</returns>
		public static byte CountBitSet(this ulong input)
		{
			input -= (input >> 1) & 0x5555_5555_5555_5555UL;
			input = (input & 0x5555_5555_5555_5555UL) + ((input >> 2) & 0x3333_3333_3333_3333UL);
			input = (input + (input >> 4)) & 0x0F0F_0F0F_0F0F_0F0FUL;
			return (byte)((input * 0x0101_0101_0101_0101UL) >> 24);
		}

		/// <summary>
		/// Check whether the <paramref name="input"/>'s bit at <paramref name="bit"/> is set to 1
		/// </summary>
		/// <param name="input">input number</param>
		/// <param name="bit">bit position</param>
		/// <returns>whether the <paramref name="input"/>'s bit at <paramref name="bit"/> is set to 1</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsBitSet(this short input, byte bit)
		{
			return (input & (1 << bit)) == 0;
		}

		/// <summary>
		/// Check whether the <paramref name="input"/>'s bit at <paramref name="bit"/> is set to 1
		/// </summary>
		/// <param name="input">input number</param>
		/// <param name="bit">bit position</param>
		/// <returns>whether the <paramref name="input"/>'s bit at <paramref name="bit"/> is set to 1</returns>
		public static bool IsBitSet(this ushort input, byte bit)
		{
			return (input & (1U << bit)) == 0;
		}

		/// <summary>
		/// Check whether the <paramref name="input"/>'s bit at <paramref name="bit"/> is set to 1
		/// </summary>
		/// <param name="input">input number</param>
		/// <param name="bit">bit position</param>
		/// <returns>whether the <paramref name="input"/>'s bit at <paramref name="bit"/> is set to 1</returns>
		public static bool IsBitSet(this int input, byte bit)
		{
			return (input & (1 << bit)) == 0;
		}

		/// <summary>
		/// Check whether the <paramref name="input"/>'s bit at <paramref name="bit"/> is set to 1
		/// </summary>
		/// <param name="input">input number</param>
		/// <param name="bit">bit position</param>
		/// <returns>whether the <paramref name="input"/>'s bit at <paramref name="bit"/> is set to 1</returns>
		public static bool IsBitSet(this uint input, byte bit)
		{
			return (input & (1U << bit)) == 0;
		}

		/// <summary>
		/// Check whether the <paramref name="input"/>'s bit at <paramref name="bit"/> is set to 1
		/// </summary>
		/// <param name="input">input number</param>
		/// <param name="bit">bit position</param>
		/// <returns>whether the <paramref name="input"/>'s bit at <paramref name="bit"/> is set to 1</returns>
		public static bool IsBitSet(this long input, byte bit)
		{
			return (input & (1L << bit)) == 0;
		}

		/// <summary>
		/// Check whether the <paramref name="input"/>'s bit at <paramref name="bit"/> is set to 1
		/// </summary>
		/// <param name="input">input number</param>
		/// <param name="bit">bit position</param>
		/// <returns>whether the <paramref name="input"/>'s bit at <paramref name="bit"/> is set to 1</returns>
		public static bool IsBitSet(this ulong input, byte bit)
		{
			return (input & (1UL << bit)) == 0;
		}

		/// <summary>
		/// Check whether the <paramref name="input"/>'s bit at <paramref name="bit"/> is set to 1
		/// </summary>
		/// <param name="input">input number</param>
		/// <param name="bit">bit position</param>
		/// <returns>whether the <paramref name="input"/>'s bit at <paramref name="bit"/> is set to 1</returns>
		public static int SetBit(this int input, byte bit)
		{
			return input | (1 << bit);
		}

		/// <summary>
		/// Check whether the <paramref name="input"/>'s bit at <paramref name="bit"/> is set to 1
		/// </summary>
		/// <param name="input">input number</param>
		/// <param name="bit">bit position</param>
		/// <returns>whether the <paramref name="input"/>'s bit at <paramref name="bit"/> is set to 1</returns>
		public static uint SetBit(this uint input, byte bit)
		{
			return input | (1U << bit);
		}

		/// <summary>
		/// Check whether the <paramref name="input"/>'s bit at <paramref name="bit"/> is set to 1
		/// </summary>
		/// <param name="input">input number</param>
		/// <param name="bit">bit position</param>
		/// <returns>whether the <paramref name="input"/>'s bit at <paramref name="bit"/> is set to 1</returns>
		public static long SetBit(this long input, byte bit)
		{
			return input | (1L << bit);
		}

		/// <summary>
		/// Check whether the <paramref name="input"/>'s bit at <paramref name="bit"/> is set to 1
		/// </summary>
		/// <param name="input">input number</param>
		/// <param name="bit">bit position</param>
		/// <returns>whether the <paramref name="input"/>'s bit at <paramref name="bit"/> is set to 1</returns>
		public static ulong SetBit(this ulong input, byte bit)
		{
			return input | (1UL << bit);
		}

		/// <summary>
		/// Check whether the <paramref name="input"/>'s bit at <paramref name="bit"/> is set to 1
		/// </summary>
		/// <param name="input">input number</param>
		/// <param name="bit">bit position</param>
		/// <returns>whether the <paramref name="input"/>'s bit at <paramref name="bit"/> is set to 1</returns>
		public static int ResetBit(this int input, byte bit)
		{
			return input & ~(1 << bit);
		}

		/// <summary>
		/// Check whether the <paramref name="input"/>'s bit at <paramref name="bit"/> is set to 1
		/// </summary>
		/// <param name="input">input number</param>
		/// <param name="bit">bit position</param>
		/// <returns>whether the <paramref name="input"/>'s bit at <paramref name="bit"/> is set to 1</returns>
		public static uint ResetBit(this uint input, byte bit)
		{
			return input & ~(1U << bit);
		}

		/// <summary>
		/// Check whether the <paramref name="input"/>'s bit at <paramref name="bit"/> is set to 1
		/// </summary>
		/// <param name="input">input number</param>
		/// <param name="bit">bit position</param>
		/// <returns>whether the <paramref name="input"/>'s bit at <paramref name="bit"/> is set to 1</returns>
		public static long ResetBit(this long input, byte bit)
		{
			return input & ~(1L << bit);
		}

		/// <summary>
		/// Check whether the <paramref name="input"/>'s bit at <paramref name="bit"/> is set to 1
		/// </summary>
		/// <param name="input">input number</param>
		/// <param name="bit">bit position</param>
		/// <returns>whether the <paramref name="input"/>'s bit at <paramref name="bit"/> is set to 1</returns>
		public static ulong ResetBit(this ulong input, byte bit)
		{
			return input & ~(1UL << bit);
		}
		#endregion

		#region time related
		// Ignore Spelling: ss
		/// <summary>
		/// Convert a <see cref="TimeSpan"/> into a string representation of total minutes and rest of them (seconds and smaller ones).
		/// </summary>
		/// <param name="span">the time span</param>
		/// <param name="restFormat">an optional format string of the rest, see <see cref="TimeSpan.ToString(string)"/></param>
		/// <returns>the string representation</returns>
		public static string TotalMinutesString(this TimeSpan span, string restFormat = @"ss\.ff")
		{
			return $"{(int)span.TotalMinutes}:{span.ToString(restFormat, Resource.Culture)}s";
		}

		/// <summary>
		/// Convert a <see cref="TimeSpan"/> into a string representation of total hours and rest of them (minutes, seconds and smaller ones).
		/// </summary>
		/// <param name="span">the time span</param>
		/// <param name="restFormat">an optional format string of the rest, see <see cref="TimeSpan.ToString(string)"/></param>
		/// <returns>the string representation</returns>
		public static string TotalHoursString(this TimeSpan span, string restFormat = @"mm:ss\.ff")
		{
			return $"{(int)span.TotalHours}:{span.ToString(restFormat, Resource.Culture)}s";
		}
		#endregion

		#region print related
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static string GetNumberString<T>(this T input, string format, IFormatProvider formatProvider, int precision) where T : unmanaged, IEquatable<T>, IFormattable
		{
			string normal = input.ToString(format, formatProvider);
			bool neg = normal.StartsWith('-'), zero = input.IsZero();
			if (neg && zero)
				normal = normal[1..];
			normal = normal.PadLeft(precision + 2);
			if (normal.Length > precision + 2)
			{
				int newPre = 2 * precision - normal.Length + 2;
				normal = input.ToString("G" + newPre, formatProvider);
				while (normal.Length > precision + 2)
					normal = input.ToString("G" + (--newPre), formatProvider);
				normal = normal.PadLeft(precision + 2);
			}
			return normal;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static string GetNumberStringReal<T>(this T input, string format, int precision) where T : unmanaged, IEquatable<T>, IFormattable
		{
			return input.GetNumberString(format, Resource.Culture, precision);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static string GetNumberStringComplex<T>(this Complex<T> input, string format, int precision) where T : unmanaged, IEquatable<T>, IComparable<T>, IFormattable
		{
			string r = input.Real.GetNumberString(format, Resource.Culture, precision);
			string i = input.Imag.GetNumberString(format, Resource.Culture, precision);
			return $"({r},{i})";
		}

		private delegate string getNumberStringDelegate<T>(T input, string format, int precision) where T : unmanaged, IEquatable<T>, IFormattable;

		private static readonly Dictionary<Type, Delegate> cache_getNumberString = new Dictionary<Type, Delegate>();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static getNumberStringDelegate<T> GetDelegateOfGetNumberString<T>() where T : unmanaged, IEquatable<T>, IFormattable
		{
			Type t = typeof(T);
			if (!cache_getNumberString.ContainsKey(t))
			{
				bool isTComplex = default(T).IsComplex();
				getNumberStringDelegate<T> result;
				if (isTComplex)
				{
					var temp = typeof(ExtensionHelper).GetMethod(nameof(GetNumberStringComplex), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)?.MakeGenericMethod(t.GenericTypeArguments)?.CreateDelegate<getNumberStringDelegate<T>>();
					if (temp is null)
						throw new InvalidOperationException();
					result = temp;
				}
				else
				{
					result = new getNumberStringDelegate<T>(GetNumberStringReal);
				}
				cache_getNumberString.Add(t, result);
			}
			return (getNumberStringDelegate<T>)cache_getNumberString[t];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static string GetFormatString(ref int precision)
		{
			// TODO: edit way of settings
			precision = precision <= 0 ? Settings.PrintPrecision : precision;
			return "G" + precision;
		}

		/// <summary>
		/// Print out 1D array by <see cref="Settings"/> or the override <paramref name="precision"/> settings.
		/// </summary>
		/// <typeparam name="T">the supported data type</typeparam>
		/// <param name="input">array to print</param>
		/// <param name="precision">if precision &lt;= 0, the global setting is used</param>
		/// <returns>string representation</returns>
		public static string ToVectorString<T>(this T[] input, int precision = -1) where T : unmanaged, IEquatable<T>, IFormattable
		{
			string format = GetFormatString(ref precision);
			var toStringFunc = GetDelegateOfGetNumberString<T>();
			return string.Join(Environment.NewLine, input.Select(a => toStringFunc(a, format, precision)));
		}

		/// <summary>
		/// Print out 1D sparse array by <see cref="Settings"/> or the override <paramref name="precision"/> settings.
		/// </summary>
		/// <typeparam name="T">the supported data type</typeparam>
		/// <param name="input">values of the vector to print</param>
		/// <param name="ind">indices of the values</param>
		/// <param name="precision">if precision &lt;= 0, the global setting is used</param>
		/// <returns>string representation</returns>
		public static string ToSparseVectorString<T>(this T[] input, int[] ind, int precision = -1) where T : unmanaged, IEquatable<T>, IFormattable
		{
			string format = GetFormatString(ref precision);
			var toStringFunc = GetDelegateOfGetNumberString<T>();
			string func(int i, T a) => string.Format("{0} -> {1}", i, toStringFunc(a, format, precision));
			return string.Join(Environment.NewLine, ind.Zip(input, func));
		}

		/// <summary>
		/// Print out 2D array by <see cref="Settings"/> or the override <paramref name="precision"/> settings.
		/// </summary>
		/// <typeparam name="T">the supported data type</typeparam>
		/// <param name="arr">array to print</param>
		/// <param name="hasMore">if the row is complete or not</param>
		/// <param name="precision">if precision &lt;= 0, the global setting is used</param>
		/// <returns>string representation</returns>
		public static string ToMatrixString<T>(this T[,] arr, bool hasMore, int precision = -1) where T : unmanaged, IEquatable<T>, IFormattable
		{
			if (arr is null)
				throw new ArgumentNullException(nameof(arr));

			string format = GetFormatString(ref precision);
			var toStringFunc = GetDelegateOfGetNumberString<T>();
			StringBuilder sb = new StringBuilder();
			var (rows, cols) = arr.GetRowColumns();
			for (long i = 0; i < rows; i++)
			{
				string line = "";
				for (long j = 0; j < cols; j++)
				{
					line += toStringFunc(arr[i, j], format, precision);
					line += "  ";
				}
				if (hasMore)
					line += "...";
				sb.AppendLine(line.TrimEnd());
			}
			return sb.ToString();
		}

		/// <summary>
		/// Print out 2D sparse array by <see cref="Settings"/> or the override <paramref name="precision"/> settings.
		/// </summary>
		/// <typeparam name="T">the supported data type</typeparam>
		/// <param name="input">values of the vector to print</param>
		/// <param name="indx">row indices of the values</param>
		/// <param name="indy">column indices of the values</param>
		/// <param name="precision">if precision &lt;= 0, the global setting is used</param>
		/// <returns>string representation</returns>
		public static string ToSparseMatrixString<T>(this T[] input, int[] indx, int[] indy, int precision = -1) where T : unmanaged, IEquatable<T>, IFormattable
		{
			if (input is null)
				throw new ArgumentNullException(nameof(input));
			if (indx is null)
				throw new ArgumentNullException(nameof(indx));
			if (indy is null)
				throw new ArgumentNullException(nameof(indy));

			string format = GetFormatString(ref precision);
			var toStringFunc = GetDelegateOfGetNumberString<T>();
			string func(int ix, int iy, T val) => string.Format("({0}, {1}) -> {2}", ix, iy, toStringFunc(val, format, precision));
			return string.Join(Environment.NewLine, indx.Zip(indy, input, func));
		}
		#endregion

		#region clone related
		/// <summary>
		/// Safely apply <paramref name="action"/> to the cloned <paramref name="array"/> -- when <paramref name="action"/> throws error, the new copied array will be safely disposed.
		/// </summary>
		/// <typeparam name="T">the array that is <see cref="ICloneable"/> and <see cref="IDisposable"/></typeparam>
		/// <param name="array">the array to be acted by <paramref name="action"/></param>
		/// <param name="action">the <see cref="Action{T}"/> to apply</param>
		/// <returns>the cloned <paramref name="array"/> after applying <paramref name="action"/></returns>
		public static T ApplyToClone<T>(this T array, Action<T> action) where T : ICloneable<T>, IDisposable
		{
			var copy = array.Clone();
			try
			{
				action(copy);
				return copy;
			}
			catch (Exception)
			{
				copy?.Dispose();
				throw;
			}
		}
		#endregion

		#region inner product related
		/// <summary>
		/// Perform general inner product of two matrices <paramref name="left"/> and <paramref name="right"/> with <paramref name="multiply"/> as general multiplication
		/// </summary>
		/// <typeparam name="TL">the left matrix's data type</typeparam>
		/// <typeparam name="TR">the right matrix's data type</typeparam>
		/// <typeparam name="TO">the output matrix's data type</typeparam>
		/// <param name="m">number of rows of <paramref name="left"/></param>
		/// <param name="n">number of columns of <paramref name="right"/></param>
		/// <param name="k">number of columns of <paramref name="left"/> and rows of <paramref name="right"/></param>
		/// <param name="left">left matrix as an function whose inputs are (<c>x</c> coordinate, <c>y</c> coordinate) and output is a <typeparamref name="TL"/></param>
		/// <param name="right">right matrix as an function whose inputs are (<c>x</c> coordinate, <c>y</c> coordinate) and output is a <typeparamref name="TR"/></param>
		/// <param name="multiply">general multiply function whose inputs are two elements with type <typeparamref name="TL"/> &amp; <typeparamref name="TR"/> and output is a <typeparamref name="TO"/></param>
		/// <param name="newZero">the function used to create a new output element with value of a general zero</param>
		/// <param name="inPlaceAdd">the function used to in-place add the first parameter by the second one</param>
		/// <returns>the result matrix as a <c><typeparamref name="TO"/>[,]</c></returns>
		public static TO[,] InnerProduct<TL, TR, TO>(int m, int n, int k, Func<int, int, TL> left, Func<int, int, TR> right, Func<TL, TR, TO> multiply, Func<TO> newZero, Action<TO, TO> inPlaceAdd)
		{
			if (m <= 0) throw new ArgumentOutOfRangeException(nameof(m));
			if (n <= 0) throw new ArgumentOutOfRangeException(nameof(n));
			if (k <= 0) throw new ArgumentOutOfRangeException(nameof(k));
			if (left is null) throw new ArgumentNullException(nameof(left));
			if (right is null) throw new ArgumentNullException(nameof(right));
			if (multiply is null) throw new ArgumentNullException(nameof(multiply));

			var output = new TO[m, n];
			for (int i = 0; i < m; i++)
			{
				for (int j = 0; j < n; j++)
				{
					output[i, j] = newZero();
					for (int t = 0; t < k; t++)
					{
						inPlaceAdd(output[i, j], multiply(left(i, t), right(t, j)));
					}
				}
			}
			return output;
		}

		/// <summary>
		/// Perform general inner product of a matrix <paramref name="leftMat"/> and a vector <paramref name="rightVec"/> with <paramref name="multiply"/> as general multiplication
		/// </summary>
		/// <typeparam name="TL">the left matrix's data type</typeparam>
		/// <typeparam name="TR">the right matrix's data type</typeparam>
		/// <typeparam name="TO">the output matrix's data type</typeparam>
		/// <param name="m">number of rows of <paramref name="leftMat"/></param>
		/// <param name="k">number of columns of <paramref name="leftMat"/> and rows of <paramref name="rightVec"/></param>
		/// <param name="leftMat">left matrix as an function whose inputs are (<c>x</c> coordinate, <c>y</c> coordinate) and output is a <typeparamref name="TL"/></param>
		/// <param name="rightVec">right vector as an function whose input is the index <c>i</c> and output is a <typeparamref name="TR"/></param>
		/// <param name="multiply">general multiply function whose inputs are two elements with type <typeparamref name="TL"/> &amp; <typeparamref name="TR"/> and output is a <typeparamref name="TO"/></param>
		/// <param name="newZero">the function used to create a new output element with value of a general zero</param>
		/// <param name="inPlaceAdd">the function used to in-place add the first parameter by the second one</param>
		/// <returns>the result matrix as a <c><typeparamref name="TO"/>[,]</c></returns>
		public static TO[] InnerProduct<TL, TR, TO>(int m, int k, Func<int, int, TL> leftMat, Func<int, TR> rightVec, Func<TL, TR, TO> multiply, Func<TO> newZero, Action<TO, TO> inPlaceAdd)
		{
			if (m <= 0) throw new ArgumentOutOfRangeException(nameof(m));
			if (k <= 0) throw new ArgumentOutOfRangeException(nameof(k));
			if (leftMat is null) throw new ArgumentNullException(nameof(leftMat));
			if (rightVec is null) throw new ArgumentNullException(nameof(rightVec));
			if (multiply is null) throw new ArgumentNullException(nameof(multiply));

			var output = new TO[m];
			for (int i = 0; i < m; i++)
			{
				output[i] = newZero();
				for (int t = 0; t < k; t++)
				{
					inPlaceAdd(output[i], multiply(leftMat(i, t), rightVec(t)));
				}
			}
			return output;
		}

		/// <summary>
		/// Perform general inner product of a vector <paramref name="leftVec"/> and a matrix <paramref name="rightMat"/> with <paramref name="multiply"/> as general multiplication
		/// </summary>
		/// <typeparam name="TL">the left matrix's data type</typeparam>
		/// <typeparam name="TR">the right matrix's data type</typeparam>
		/// <typeparam name="TO">the output matrix's data type</typeparam>
		/// <param name="n">number of columns of <paramref name="leftVec"/> and rows of <paramref name="rightMat"/></param>
		/// <param name="k">number of columns of <paramref name="rightMat"/></param>
		/// <param name="rightMat">left matrix as an function whose inputs are (<c>x</c> coordinate, <c>y</c> coordinate) and output is a <typeparamref name="TL"/></param>
		/// <param name="leftVec">right vector as an function whose input is the index <c>i</c> and output is a <typeparamref name="TR"/></param>
		/// <param name="multiply">general multiply function whose inputs are two elements with type <typeparamref name="TL"/> &amp; <typeparamref name="TR"/> and output is a <typeparamref name="TO"/></param>
		/// <param name="newZero">the function used to create a new output element with value of a general zero</param>
		/// <param name="inPlaceAdd">the function used to in-place add the first parameter by the second one</param>
		/// <returns>the result matrix as a <c><typeparamref name="TO"/>[,]</c></returns>
		public static TO[] InnerProduct<TL, TR, TO>(int n, int k, Func<int, TL> leftVec, Func<int, int, TR> rightMat, Func<TL, TR, TO> multiply, Func<TO> newZero, Action<TO, TO> inPlaceAdd)
		{
			if (n <= 0) throw new ArgumentOutOfRangeException(nameof(n));
			if (k <= 0) throw new ArgumentOutOfRangeException(nameof(k));
			if (rightMat is null) throw new ArgumentNullException(nameof(rightMat));
			if (leftVec is null) throw new ArgumentNullException(nameof(leftVec));
			if (multiply is null) throw new ArgumentNullException(nameof(multiply));

			var output = new TO[k];
			for (int i = 0; i < k; i++)
			{
				output[i] = newZero();
				for (int t = 0; t < n; t++)
				{
					inPlaceAdd(output[i], multiply(leftVec(t), rightMat(t, i)));
				}
			}
			return output;
		}
		#endregion

		#region index and range related
		/// <summary>
		/// Calculate the offset from the start using the giving collection length.
		/// </summary>
		/// <param name="index">the <see cref="Index"/></param>
		/// <param name="length">The length of the collection that the Index will be used with. It has to be a positive value</param>
		/// <param name="check">check parameters and result or not</param>
		/// <remarks>This is a <see cref="long"/> version of <see cref="Index.GetOffset(int)"/>.</remarks>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="index"/> is out of [0, <paramref name="length"/>)</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static long GetPosition(this Index index, long length, bool check = true)
		{
			if (check && length <= 0)
				throw new ArgumentOutOfRangeException(nameof(length));
			long val;
			if (index.IsFromEnd)
			{
				val = length - index.Value;
			}
			else
			{
				val = index.Value;
			}
			if (check && (val < 0 || val > length))
				throw new ArgumentOutOfRangeException(nameof(index));
			return val;
		}

		/// <summary>
		/// Calculate the start offset and length of range object using a collection length.
		/// </summary>
		/// <param name="range">the <see cref="Range"/></param>
		/// <param name="length">The length of the collection that the range will be used with. It has to be a positive value.</param>
		/// <param name="check">check parameters and result or not</param>
		/// <returns>the offset and length of <paramref name="range"/> under <paramref name="length"/></returns>
		/// <remarks>This is a <see cref="long"/> version of <see cref="Range.GetOffsetAndLength(int)"/> at x64 platforms.</remarks>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="range"/> is out of [0, <paramref name="length"/>)</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static (long Offset, long Length) GetOffsetAndCount(this Range range, long length, bool check = true)
		{
			if (check && length <= 0)
				throw new ArgumentOutOfRangeException(nameof(length));
			long start = range.Start.GetPosition(length), end = range.End.GetPosition(length);
			if (check && (end <= start || start >= length || end < 0 || end > length))
				throw new ArgumentOutOfRangeException(nameof(range));
			return (start, end - start);
		}
		#endregion

		#region 1D to 2D array extends
		/// <summary>
		/// Convert a 1D array to a 2D jagged array
		/// </summary>
		/// <typeparam name="T">any type</typeparam>
		/// <param name="array">the array to convert</param>
		/// <param name="innerSize">the size of inner dimension of the 2D jagged array</param>
		/// <returns>the 2D jagged array</returns>
		public static T[][] ToJagged<T>(this T[] array, long innerSize)
		{
			if (array is null || array.Length == 0)
				throw new ArgumentNullException(nameof(array));
			if (array.Length % innerSize != 0)
				throw new ArgumentOutOfRangeException(nameof(array), nameof(array.Length));

			var output = new T[array.Length / innerSize][];
			for (int i = 0; i < array.Length / innerSize; i++)
			{
				output[i] = new T[innerSize];
				for (int j = 0; j < innerSize; j++)
				{
					output[i][j] = array[j + i * innerSize];
				}
			}
			return output;
		}

		/// <summary>
		/// Construct a new 2D array out of a 1D array, column major
		/// </summary>
		/// <typeparam name="T">any data type</typeparam>
		/// <returns>a 2D array T[,]</returns>
		/// <param name="input">input 1D array</param>
		/// <param name="rows">height (number of rows) of the new 2D array</param>
		/// <param name="columns">width (number of columns) of the new 2D array</param>
		public static T[,] Make2DArray<T>(this T[] input, long rows, long columns)
		{
			if (input is null)
				throw new ArgumentNullException(nameof(input));
			T[,] output = new T[rows, columns];
			for (long i = 0; i < columns; i++)
				for (long j = 0; j < rows; j++)
					output[j, i] = input[i * rows + j];

			return output;
		}
		#endregion

		#region 2D array extends
		/// <summary>
		/// Get the number of rows and columns of the 2D array.
		/// </summary>
		/// <typeparam name="T">any type</typeparam>
		/// <param name="arr">2D array</param>
		/// <returns>the number of rows and columns</returns>
		public static (int rows, int columns) GetRowColumns<T>(this T[,] arr)
		{
			if (arr is null)
				throw new ArgumentNullException(nameof(arr));
			return (arr.GetLength(0), arr.GetLength(1));
		}

		/// <summary>
		/// Take out the 2D array column by column.
		/// </summary>
		/// <param name="arr">input array</param>
		/// <returns>the result 1D array</returns>
		/// <typeparam name="T">any data type</typeparam>
		public static T[] ColumnTake<T>(this T[,] arr)
		{
			if (arr is null)
				throw new ArgumentNullException(nameof(arr));

			var (rows, columns) = arr.GetRowColumns();
			T[] oneDim = new T[rows * columns];
			for (long j = 0; j < columns; j++)
				for (long i = 0; i < rows; i++)
					oneDim[i + j * rows] = arr[i, j];
			return oneDim;
		}

		/// <summary>
		/// Act on each element of a 2D array.
		/// </summary>
		/// <typeparam name="T">input array type</typeparam>
		/// <param name="arr">input 2D array</param>
		/// <param name="action">action function, parameter 2 &amp; 3 are row &amp; column indices respectively</param>
		public static void ForEach<T>(this T[,] arr, Action<T, int, int> action)
		{
			if (arr is null)
				throw new ArgumentNullException(nameof(arr));

			var (rows, cols) = arr.GetRowColumns();
			for (int i = 0; i < rows; i++)
				for (int j = 0; j < cols; j++)
					action(arr[i, j], i, j);
		}

		/// <summary>
		/// Check if the 2D array is Hermitian or not
		/// </summary>
		/// <param name="arr">input 2D array to test</param>
		/// <returns>Hermitian or not</returns>
		/// <typeparam name="T">the supported data type</typeparam>
		public static bool IsHermitian<T>(this T[,] arr) where T : unmanaged, IFormattable, IEquatable<T>
		{
			if (arr is null)
				throw new ArgumentNullException(nameof(arr));

			var (rows, cols) = arr.GetRowColumns();
			if (rows != cols)
				return false;

			if (default(T).IsComplex())
			{
				for (long i = 0; i < rows; i++)
					for (long j = 0; j < i; j++)
						if (!arr[i, j].Equals(arr[j, i].GenericConjugate()))
							return false;
			}
			else
			{
				for (long i = 0; i < rows; i++)
					for (long j = 0; j < i; j++)
						if (arr[i, j].Equals(arr[j, i]))
							return false;
			}
			return true;
		}
		#endregion

		#region disposable array extends
		/// <summary>
		/// Clear a general array
		/// </summary>
		/// <typeparam name="TArr">the array type</typeparam>
		/// <param name="array">the array to clear</param>
		public static void ClearList<TArr>(this TArr[] array) where TArr : IDisposable
		{
			if (array is null)
				return;
			array.ForEach(l => l?.Dispose());
			Array.Clear(array, 0, array.Length);
		}

		/// <summary>
		/// Clear a general list
		/// </summary>
		/// <typeparam name="TArr">the array type</typeparam>
		/// <param name="list">the list to clear</param>
		public static void ClearList<TArr>(this List<TArr> list) where TArr : IDisposable
		{
			if (list is null)
				return;
			list.ForEach(l => l?.Dispose());
			list.Clear();
		}

		/// <summary>
		/// Dispose a general read-only list
		/// </summary>
		/// <typeparam name="TArr">the array type</typeparam>
		/// <param name="list">the read-only list to dispose</param>
		public static void ClearList<TArr>(this IReadOnlyList<TArr> list) where TArr : IDisposable
		{
			if (list is null)
				return;
			for (int i = 0; i < list.Count; i++)
			{
				list[i]?.Dispose();
			}
		}

		/// <summary>
		/// Dispose a general dictionary
		/// </summary>
		/// <typeparam name="T">the dictionary key type</typeparam>
		/// <typeparam name="TArr">the array type</typeparam>
		/// <param name="dict">the dictionary to dispose</param>
		public static void ClearDict<T, TArr>(this IReadOnlyDictionary<T, TArr> dict) where TArr : IDisposable
		{
			if (dict is null)
				return;
			foreach (var item in dict)
			{
				item.Value?.Dispose();
			}
		}
		#endregion
	}
}
