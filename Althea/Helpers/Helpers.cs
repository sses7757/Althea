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
		private static readonly Dictionary<(RuntimeTypeHandle t1, RuntimeTypeHandle t2), Delegate?> _conversionCache = new();

		/// <summary>
		/// Generic convert <paramref name="obj"/> of <typeparamref name="T1"/> to <typeparamref name="T2"/> by finding possible explicit or implicit conversion operators.
		/// </summary>
		/// <typeparam name="T1">input type</typeparam>
		/// <typeparam name="T2">output type</typeparam>
		/// <param name="obj">input object</param>
		/// <returns>The <typeparamref name="T2"/> object converted by explicit or implicit operators</returns>
		/// <remarks>Since this method has internal caching, this is better for repetitive usage and worse for simple usage than <see cref="NativeTypeExtension.GenericConvert{TOut, TIn}(TIn)"/> which utilizes dynamic conversion.</remarks>
		public static T2 ReflectionConvert<T1, T2>(this T1 obj) where T1 : notnull where T2 : notnull
		{
			static bool predicator(System.Reflection.MethodInfo m) => (m.Name == "op_Explicit" || m.Name == "op_Implicit") &&
																		m.ReturnType == typeof(T2) && m.GetParameters().Length == 1 &&
																		m.GetParameters()[0].ParameterType.IsAssignableFrom(typeof(T1));

			if (obj is T2 a)
				return a;
			Type t1 = typeof(T1), t2 = typeof(T2);
			var key = (t1.TypeHandle, t2.TypeHandle);
			if (!_conversionCache.ContainsKey(key))
			{
				var conversionOperator = t1.GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
												 .Where(predicator).FirstOrDefault();
				conversionOperator ??= t2.GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
												 .Where(predicator).FirstOrDefault();
				if (conversionOperator is null)
					_conversionCache.Add(key, null);
				else
					_conversionCache.Add(key, conversionOperator.CreateDelegate<Converter<T1, T2>>());
			}
			if (_conversionCache[key] is not Converter<T1, T2> converter)
				return (T2)(dynamic)obj; // default dynamic converter
			else
				return converter.Invoke(obj);
		}

		/// <summary>
		/// Get the name string representation of given <paramref name="type"/> together with its generic parameters
		/// </summary>
		/// <param name="type">The given <see cref="Type"/> to get name</param>
		/// <param name="full">Whether to use <see cref="Type.FullName"/> or only <see cref="System.Reflection.MemberInfo.Name"/></param>
		/// <returns>The name string representation of given <paramref name="type"/> or null if the given <paramref name="type"/>'s name cannot be obtained.</returns>
		public static string? GetGenericString(this Type type, bool full = false)
		{
			string? name = full ? type.FullName : type.Name;
			if (name is null)
				return null;
			if (type.IsGenericType)
			{
				var args = type.GenericTypeArguments;
				name += $"<{string.Join(", ", args.Select(a => a.GetGenericString()).ToArray())}>";
			}
			return name;
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
		public static long CombinationNumber(int n, int N)
		{
			if (n < N || n == 0 || N == 0)
				return 0;
			if (n == 1)
				return N;
			if (n == N)
				return 1;
			// otherwise
			long ret = 1;
			int c = N - n;
			c = Math.Min(c, n);
			for (int i = N - c + 1; i <= N; i++)
				ret = checked(ret * i);
			for (int i = 2; i <= c; i++)
				ret /= i;
			return ret;
		}

		// Ignore Spelling: nd
		/// <summary>
		/// Output an integer as a cardinality number, e.g. 0 -> 1st, 51 -> 52nd
		/// </summary>
		/// <param name="a">The input number</param>
		/// <returns>the ordinal representation string</returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="a"/> is smaller than 0</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string ToOrdinal(this int a)
		{
			if (a < 0)
				throw new ArgumentOutOfRangeException(nameof(a), a, Parameter.CannotNegative);
			a++;
			int c = a % 10;
			return c switch
			{
				1 => $"{a}-st",
				2 => $"{a}-nd",
				3 => $"{a}-rd",
				_ => $"{a}-th",
			};
		}

		/// <summary>
		/// Is the input integer a perfect square or not.
		/// </summary>
		/// <param name="input">The input integer</param>
		/// <returns>Whether <paramref name="input"/> is perfect square or not.</returns>
		public static bool IsPerfectSquare(this long input)
		{
			long closestRoot = (long)Math.Sqrt(input);
			return input == closestRoot * closestRoot;
		}

		/// <summary>
		/// Whether the input integer is a power of 2
		/// </summary>
		/// <param name="x">The input integer</param>
		/// <returns>Whether <paramref name="x"/> is a power of 2</returns>
		public static bool IsPowerOfTwo(this long x)
		{
			if (x == 1) return true;
			return (x > 1) && ((x & (x - 1)) == 0);
		}

		/// <summary>
		/// Whether the input integer is a power of 2
		/// </summary>
		/// <param name="x">The input integer</param>
		/// <returns>Whether <paramref name="x"/> is a power of 2</returns>
		public static bool IsPowerOfTwo(this int x)
		{
			if (x == 1) return true;
			return (x > 1) && ((x & (x - 1)) == 0);
		}

		/// <summary>
		/// Whether the input integer is a power of 2
		/// </summary>
		/// <param name="x">The input integer</param>
		/// <returns>Whether <paramref name="x"/> is a power of 2</returns>
		public static bool IsPowerOfTwo(this short x)
		{
			if (x == 1) return true;
			return (x > 1) && ((x & (x - 1)) == 0);
		}

		/// <summary>
		/// Get the nearest power of 2 integer of the input integer
		/// </summary>
		/// <param name="x">The input integer</param>
		/// <returns><paramref name="x"/>'s the nearest power of 2</returns>
		public static int NearestPowerOfTwo(this int x)
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
		/// <param name="x">The input integer</param>
		/// <returns><paramref name="x"/>'s the nearest power of 2</returns>
		public static long NearestPowerOfTwo(this long x)
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
		/// Get the floor round of log2(<paramref name="input"/>)
		/// </summary>
		/// <param name="input">input number</param>
		/// <returns>the nearest log2 of <paramref name="input"/></returns>
		public static short Log2(this short input)
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
		public static short Log2(this int input)
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
		public static short Log2(this long input)
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
		public static byte CountBitSet(this int input)
		{
			input -= (input >> 1) & 0x5555_5555;
			input = (input & 0x3333_3333) + ((input >> 2) & 0x3333_3333);
			input = (input + (input >> 4)) & 0x0F0F_0F0F;
			return (byte)((input * 0x0101_0101) >> 24);
		}

		/// <summary>
		/// Count the <paramref name="input"/>'s bits which are set to 1
		/// </summary>
		/// <param name="input">input integer</param>
		/// <returns>the number <paramref name="input"/>'s bits set</returns>
		public static byte CountBitSet(this long input)
		{
			input -= (input >> 1) & 0x5555_5555_5555_5555L;
			input = (input & 0x5555_5555_5555_5555L) + ((input >> 2) & 0x3333_3333_3333_3333L);
			input = (input + (input >> 4)) & 0x0F0F_0F0F_0F0F_0F0FL;
			return (byte)((input * 0x0101_0101_0101_0101L) >> 24);
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
		public static long ResetBit(this long input, byte bit)
		{
			return input & ~(1L << bit);
		}
		#endregion

		#region time related
		// Ignore Spelling: ss
		/// <summary>
		/// Convert a <see cref="TimeSpan"/> into a string representation of total minutes and rest of them (seconds and smaller ones).
		/// </summary>
		/// <param name="span">The time span</param>
		/// <param name="restFormat">an optional format string of the rest, see <see cref="TimeSpan.ToString(string)"/></param>
		/// <returns>the string representation</returns>
		public static string TotalMinutesString(this TimeSpan span, string restFormat = @"ss\.ff")
		{
			return $"{(int)span.TotalMinutes}:{span.ToString(restFormat, Resource.Culture)}s";
		}

		/// <summary>
		/// Convert a <see cref="TimeSpan"/> into a string representation of total hours and rest of them (minutes, seconds and smaller ones).
		/// </summary>
		/// <param name="span">The time span</param>
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
			int totalLength = precision + 2;
			normal = normal.PadLeft(totalLength);
			if (normal.Length > totalLength)
			{
				int newPre = 2 * precision - normal.Length + 2;
				normal = input.ToString("G" + newPre, formatProvider);
				while (normal.Length > totalLength)
					normal = input.ToString("G" + (--newPre), formatProvider);
				normal = normal.PadLeft(totalLength);
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

		private static readonly Dictionary<Type, Delegate> cache_getNumberString = new();

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
			precision = precision <= 0 ? Settings.PrintPrecision : precision;
			return "G" + precision;
		}

		/// <summary>
		/// Print out 1D array by <see cref="Settings"/> or the override <paramref name="precision"/> settings.
		/// </summary>
		/// <typeparam name="T">The supported data type</typeparam>
		/// <param name="input">The dense vector to print</param>
		/// <param name="precision">If <paramref name="precision"/> ≤ 0, the global setting is used</param>
		/// <returns>The string representation of dense vector <paramref name="input"/> at <paramref name="precision"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string ToVectorString<T>(this Span<T> input, int precision = -1) where T : unmanaged, IEquatable<T>, IFormattable
		{
			if (input.IsEmpty)
				return string.Empty;
			string format = GetFormatString(ref precision);
			getNumberStringDelegate<T> toStringFunc = GetDelegateOfGetNumberString<T>();
			StringBuilder sb = new();
			for (int i = 0; i < input.Length; i++)
			{
				sb.AppendLine(toStringFunc.Invoke(input[i], format, precision));
			}
			return sb.Remove(sb.Length - Environment.NewLine.Length, Environment.NewLine.Length).ToString();
		}

		/// <summary>
		/// Print out 1D sparse array by <see cref="Settings"/> or the override <paramref name="precision"/> settings.
		/// </summary>
		/// <typeparam name="T">The supported data type</typeparam>
		/// <param name="values">The values of the sparse vector to print</param>
		/// <param name="indices">The indices of the sparse vector to print</param>
		/// <param name="precision">If <paramref name="precision"/> ≤ 0, the global setting is used</param>
		/// <returns>The string representation of sparse vector (<paramref name="values"/>, <paramref name="indices"/>) at <paramref name="precision"/></returns>
		/// <exception cref="ArgumentException">If <paramref name="values"/> and <paramref name="indices"/> have different lengths</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string ToSparseVectorString<T>(this Span<T> values, Span<long> indices, int precision = -1) where T : unmanaged, IEquatable<T>, IFormattable
		{
			if (values.Length != indices.Length)
				throw new ArgumentException(Parameter.NotSameSize);
			if (values.IsEmpty)
				return string.Empty;

			string format = GetFormatString(ref precision);
			getNumberStringDelegate<T> toStringFunc = GetDelegateOfGetNumberString<T>();
			StringBuilder sb = new();
			for (int i = 0; i < values.Length; i++)
			{
				sb.AppendLine($"{indices[i]} -> {toStringFunc.Invoke(values[i], format, precision)}");
			}
			return sb.Remove(sb.Length - Environment.NewLine.Length, Environment.NewLine.Length).ToString();
		}

		/// <summary>
		/// Print out 2D array by <see cref="Settings"/> or the override <paramref name="precision"/> settings.
		/// </summary>
		/// <typeparam name="T">The supported data type</typeparam>
		/// <param name="matrix">The column-major values of the dense matrix to print</param>
		/// <param name="rows">The number of rows of the given matrix</param>
		/// <param name="precision">If <paramref name="precision"/> ≤ 0, the global setting is used</param>
		/// <param name="more">The neglected number of elements of each row of <paramref name="matrix"/>, less than 1 means no more elements</param>
		/// <returns>The string representation of dense matrix <paramref name="matrix"/> at <paramref name="precision"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="rows"/> is not a positive number</exception>
		/// <exception cref="ArgumentException">If the length of <paramref name="matrix"/> cannot be divided by <paramref name="rows"/></exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string ToMatrixString<T>(this Span<T> matrix, int rows, long more = 0, int precision = -1) where T : unmanaged, IEquatable<T>, IFormattable
		{
			if (matrix.IsEmpty)
				return string.Empty;
			if (rows <= 0)
				throw new ArgumentOutOfRangeException(nameof(rows), rows, Parameter.MustPositive);
			if (matrix.Length % rows != 0)
				throw new ArgumentException(Other.CannotDivide);

			int cols = matrix.Length / rows;
			string format = GetFormatString(ref precision);
			getNumberStringDelegate<T> toStringFunc = GetDelegateOfGetNumberString<T>();
			string moreStr = more > 0 ? string.Format("  " + Print.RowMore, more) : "  ";
			StringBuilder sb = new();
			for (int i = 0; i < rows; i++)
			{
				StringBuilder line = new();
				for (int j = 0; j < cols; j++)
				{
					line.Append(toStringFunc.Invoke(matrix[i + j * rows], format, precision));
				}
				line.Append(moreStr);
				sb.AppendLine(line.ToString());
			}
			return sb.ToString();
		}

		/// <summary>
		/// Print out 2D sparse array by <see cref="Settings"/> or the override <paramref name="precision"/> settings.
		/// </summary>
		/// <typeparam name="T">The supported data type</typeparam>
		/// <param name="values">The values of the sparse matrix to print</param>
		/// <param name="indx">The row indices of the sparse matrix to print</param>
		/// <param name="indy">The column indices of the sparse matrix to print</param>
		/// <param name="precision">If <paramref name="precision"/> ≤ 0, the global setting is used</param>
		/// <returns>The string representation of sparse matrix (<paramref name="values"/>, <paramref name="indx"/>, <paramref name="indy"/>) at <paramref name="precision"/></returns>
		/// <exception cref="ArgumentException">If <paramref name="values"/> and <paramref name="indx"/> and <paramref name="indy"/> have different lengths</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string ToSparseMatrixString<T>(this Span<T> values, Span<long> indx, Span<long> indy, int precision = -1) where T : unmanaged, IEquatable<T>, IFormattable
		{
			if (values.Length != indx.Length)
				throw new ArgumentException(Parameter.NotSameSize);
			if (values.Length != indy.Length)
				throw new ArgumentException(Parameter.NotSameSize);
			if (values.IsEmpty)
				return string.Empty;

			string format = GetFormatString(ref precision);
			getNumberStringDelegate<T> toStringFunc = GetDelegateOfGetNumberString<T>();
			StringBuilder sb = new();
			for (int i = 0; i < values.Length; i++)
			{
				sb.AppendLine($"({indx[i]}, {indy[i]}) -> {toStringFunc.Invoke(values[i], format, precision)}");
			}
			return sb.Remove(sb.Length - Environment.NewLine.Length, Environment.NewLine.Length).ToString();
		}
		#endregion

		#region clone related
		/// <summary>
		/// Safely apply <paramref name="action"/> to a clone of <paramref name="array"/>. When <paramref name="action"/> throws error, the new copied array will be safely disposed.
		/// </summary>
		/// <typeparam name="T">The array that is <see cref="ICloneable"/> and <see cref="IDisposable"/></typeparam>
		/// <param name="array">The array to be acted by <paramref name="action"/></param>
		/// <param name="action">The <see cref="Action{T}"/> to apply</param>
		/// <returns>The cloned <paramref name="array"/> after applying <paramref name="action"/></returns>
		public static T ApplyToClone<T>(this T array, Action<T> action) where T : IDisposable, ICloneable
		{
			var clone = array.Clone();
			try
			{
				var t = (T)clone;
				action.Invoke(t);
				return t;
			}
			catch (System.Exception)
			{
				(clone as IDisposable)?.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Safely apply <paramref name="action"/> to a new array alike <paramref name="array"/>. When <paramref name="action"/> throws error, the new array will be safely disposed.
		/// </summary>
		/// <typeparam name="TArr">The array that is <see cref="Arrays.ValueArray{T}"/></typeparam>
		/// <typeparam name="T">The data type used by <typeparamref name="TArr"/></typeparam>
		/// <param name="array">The array to be acted by <paramref name="action"/></param>
		/// <param name="action">The <see cref="Action{T}"/> to apply</param>
		/// <returns>The new array alike <paramref name="array"/> after applying <paramref name="action"/></returns>
		internal static TArr ApplyToAlike<TArr, T>(this TArr array, Action<TArr> action)
			where TArr : Arrays.ValueArray<T>
			where T : unmanaged, IFormattable, IEquatable<T>
		{
			var clone = array.NewArrayAlike();
			try
			{
				var t = (TArr)clone;
				action.Invoke(t);
				return t;
			}
			catch (System.Exception)
			{
				clone?.Dispose();
				throw;
			}
		}
		#endregion

		#region inner product related
		/// <summary>
		/// Perform general inner product of two matrices <paramref name="left"/> and <paramref name="right"/> with <paramref name="multiply"/> as general multiplication
		/// </summary>
		/// <typeparam name="TL">The left matrix's data type</typeparam>
		/// <typeparam name="TR">The right matrix's data type</typeparam>
		/// <typeparam name="TO">The output matrix's data type</typeparam>
		/// <param name="m">number of rows of <paramref name="left"/></param>
		/// <param name="n">number of columns of <paramref name="right"/></param>
		/// <param name="k">number of columns of <paramref name="left"/> and rows of <paramref name="right"/></param>
		/// <param name="left">left matrix as an function whose inputs are (<c>x</c> coordinate, <c>y</c> coordinate) and output is a <typeparamref name="TL"/></param>
		/// <param name="right">right matrix as an function whose inputs are (<c>x</c> coordinate, <c>y</c> coordinate) and output is a <typeparamref name="TR"/></param>
		/// <param name="multiply">general multiply function whose inputs are two elements with type <typeparamref name="TL"/> &amp; <typeparamref name="TR"/> and output is a <typeparamref name="TO"/></param>
		/// <param name="newZero">The function used to create a new output element with value of a general zero</param>
		/// <param name="inPlaceAdd">The function used to in-place add the first parameter by the second one</param>
		/// <returns>the result matrix as a <c><typeparamref name="TO"/>[,]</c></returns>
		public static TO[,] InnerProduct<TL, TR, TO>(int m, int n, int k, Func<int, int, TL> left, Func<int, int, TR> right, Func<TL, TR, TO> multiply, Func<TO> newZero, Action<TO, TO> inPlaceAdd)
		{
			if (m <= 0) throw new ArgumentOutOfRangeException(nameof(m), m, Parameter.MustPositive);
			if (n <= 0) throw new ArgumentOutOfRangeException(nameof(n), n, Parameter.MustPositive);
			if (k <= 0) throw new ArgumentOutOfRangeException(nameof(k), k, Parameter.MustPositive);
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
		/// <typeparam name="TL">The left matrix's data type</typeparam>
		/// <typeparam name="TR">The right matrix's data type</typeparam>
		/// <typeparam name="TO">The output matrix's data type</typeparam>
		/// <param name="m">number of rows of <paramref name="leftMat"/></param>
		/// <param name="k">number of columns of <paramref name="leftMat"/> and rows of <paramref name="rightVec"/></param>
		/// <param name="leftMat">left matrix as an function whose inputs are (<c>x</c> coordinate, <c>y</c> coordinate) and output is a <typeparamref name="TL"/></param>
		/// <param name="rightVec">right vector as an function whose input is the index <c>i</c> and output is a <typeparamref name="TR"/></param>
		/// <param name="multiply">general multiply function whose inputs are two elements with type <typeparamref name="TL"/> &amp; <typeparamref name="TR"/> and output is a <typeparamref name="TO"/></param>
		/// <param name="newZero">The function used to create a new output element with value of a general zero</param>
		/// <param name="inPlaceAdd">The function used to in-place add the first parameter by the second one</param>
		/// <returns>the result matrix as a <c><typeparamref name="TO"/>[,]</c></returns>
		public static TO[] InnerProduct<TL, TR, TO>(int m, int k, Func<int, int, TL> leftMat, Func<int, TR> rightVec, Func<TL, TR, TO> multiply, Func<TO> newZero, Action<TO, TO> inPlaceAdd)
		{
			if (m <= 0) throw new ArgumentOutOfRangeException(nameof(m), m, Parameter.MustPositive);
			if (k <= 0) throw new ArgumentOutOfRangeException(nameof(k), k, Parameter.MustPositive);
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
		/// <typeparam name="TL">The left matrix's data type</typeparam>
		/// <typeparam name="TR">The right matrix's data type</typeparam>
		/// <typeparam name="TO">The output matrix's data type</typeparam>
		/// <param name="n">number of columns of <paramref name="leftVec"/> and rows of <paramref name="rightMat"/></param>
		/// <param name="k">number of columns of <paramref name="rightMat"/></param>
		/// <param name="rightMat">left matrix as an function whose inputs are (<c>x</c> coordinate, <c>y</c> coordinate) and output is a <typeparamref name="TL"/></param>
		/// <param name="leftVec">right vector as an function whose input is the index <c>i</c> and output is a <typeparamref name="TR"/></param>
		/// <param name="multiply">general multiply function whose inputs are two elements with type <typeparamref name="TL"/> &amp; <typeparamref name="TR"/> and output is a <typeparamref name="TO"/></param>
		/// <param name="newZero">The function used to create a new output element with value of a general zero</param>
		/// <param name="inPlaceAdd">The function used to in-place add the first parameter by the second one</param>
		/// <returns>the result matrix as a <c><typeparamref name="TO"/>[,]</c></returns>
		public static TO[] InnerProduct<TL, TR, TO>(int n, int k, Func<int, TL> leftVec, Func<int, int, TR> rightMat, Func<TL, TR, TO> multiply, Func<TO> newZero, Action<TO, TO> inPlaceAdd)
		{
			if (n <= 0) throw new ArgumentOutOfRangeException(nameof(n), n, Parameter.MustPositive);
			if (k <= 0) throw new ArgumentOutOfRangeException(nameof(k), k, Parameter.MustPositive);
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
		/// <param name="index">The <see cref="Index"/></param>
		/// <param name="length">The length of the collection that the Index will be used with. It has to be a positive value</param>
		/// <param name="check">check parameters and result or not</param>
		/// <remarks>This is a <see cref="long"/> version of <see cref="Index.GetOffset(int)"/>.</remarks>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="index"/> is out of [0, <paramref name="length"/>)</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static long GetPosition(this Index index, long length, bool check = true)
		{
			if (check && length <= 0)
				throw new ArgumentOutOfRangeException(nameof(length), length, Parameter.MustPositive);
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
				throw new ArgumentOutOfRangeException(nameof(index), index, Parameter.InvalidValue);
			return val;
		}

		/// <summary>
		/// Calculate the start offset and length of range object using a collection length.
		/// </summary>
		/// <param name="range">The <see cref="Range"/></param>
		/// <param name="length">The length of the collection that the range will be used with. It has to be a positive value.</param>
		/// <param name="check">check parameters and result or not</param>
		/// <returns>the offset and length of <paramref name="range"/> under <paramref name="length"/></returns>
		/// <remarks>This is a <see cref="long"/> version of <see cref="Range.GetOffsetAndLength(int)"/> at x64 platforms.</remarks>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="range"/> is out of [0, <paramref name="length"/>)</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static (long Offset, long Length) GetOffsetAndCount(this Range range, long length, bool check = true)
		{
			if (check && length <= 0)
				throw new ArgumentOutOfRangeException(nameof(length), length, Parameter.MustPositive);
			long start = range.Start.GetPosition(length), end = range.End.GetPosition(length);
			if (check && (end <= start || start >= length || end < 0 || end > length))
				throw new ArgumentOutOfRangeException(nameof(range), range, Parameter.InvalidValue);
			return (start, end - start);
		}
		#endregion

		#region 1D to 2D array extends
		/// <summary>
		/// Convert a 1D array to a 2D jagged array
		/// </summary>
		/// <typeparam name="T">any type</typeparam>
		/// <param name="array">The array to convert</param>
		/// <param name="innerSize">The size of inner dimension of the 2D jagged array</param>
		/// <returns>the 2D jagged array</returns>
		public static T[][] ToJagged<T>(this T[] array, long innerSize)
		{
			if (array is null || array.Length == 0)
				throw new ArgumentNullException(nameof(array));
			if (array.Length % innerSize != 0)
				throw new ArgumentOutOfRangeException(nameof(array), array, nameof(array.Length));

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
		/// <typeparam name="T">The supported data type</typeparam>
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
		/// <typeparam name="TArr">The array type</typeparam>
		/// <param name="array">The array to clear</param>
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
		/// <typeparam name="TArr">The array type</typeparam>
		/// <param name="list">The list to clear</param>
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
		/// <typeparam name="TArr">The array type</typeparam>
		/// <param name="list">The read-only list to dispose</param>
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
		/// <typeparam name="T">The dictionary key type</typeparam>
		/// <typeparam name="TArr">The array type</typeparam>
		/// <param name="dict">The dictionary to dispose</param>
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
