using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Althea.Linq;
using Althea.NativeTypes;


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
	}
	#endregion

	#region reflection
	/// <summary>
	/// A static class that contains helper functions using reflections
	/// </summary>
	public static class ReflectionHelper
	{
		private static readonly Dictionary<(Type t1, Type t2), Delegate> _conversionCache = new Dictionary<(Type t1, Type t2), Delegate>();

		private delegate T2 conversionDelegate<in T1, out T2>(T1 obj);

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
				_conversionCache.Add((t1, t2), conversionOperator?.CreateDelegate<conversionDelegate<T1, T2>>());
			}
			if (_conversionCache[(t1, t2)] is null)
				throw new MethodAccessException();
			else
				return ((conversionDelegate<T1, T2>)_conversionCache[(t1, t2)]).Invoke(obj);
		}
	}
	#endregion

	/// <summary>
	/// A static class that contains general purposed extension helper methods
	/// </summary>
	public static class ExtensionHelper
	{
		#region integer related
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
		/// <param name="input"></param>
		/// <returns>perfect square or not</returns>
		/// <remarks>extend method</remarks>
		public static bool IsPerfectSquare(this long input)
		{
			long closestRoot = (long)Math.Sqrt(input);
			return input == closestRoot * closestRoot;
		}

		/// <summary>
		/// Whether the number is a power of 2
		/// </summary>
		/// <param name="x">input number</param>
		/// <returns>the number is a power of 2</returns>
		/// <remarks>extend method</remarks>
		public static bool IsPowerOfTwo(this ulong x)
		{
			if (x == 1) return true;
			return (x > 1) && ((x & (x - 1)) == 0);
		}
		/// <summary>
		/// Whether the number is a power of 2
		/// </summary>
		/// <param name="x">input number</param>
		/// <returns>the number is a power of 2</returns>
		/// <remarks>extend method</remarks>
		public static bool IsPowerOfTwo(this long x)
		{
			if (x == 1) return true;
			return (x > 1) && ((x & (x - 1)) == 0);
		}
		/// <summary>
		/// Whether the number is a power of 2
		/// </summary>
		/// <param name="x">input number</param>
		/// <returns>the number is a power of 2</returns>
		/// <remarks>extend method</remarks>
		public static bool IsPowerOfTwo(this uint x)
		{
			if (x == 1) return true;
			return (x > 1) && ((x & (x - 1)) == 0);
		}
		/// <summary>
		/// Whether the number is a power of 2
		/// </summary>
		/// <param name="x">input number</param>
		/// <returns>the number is a power of 2</returns>
		/// <remarks>extend method</remarks>
		public static bool IsPowerOfTwo(this int x)
		{
			if (x == 1) return true;
			return (x > 1) && ((x & (x - 1)) == 0);
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
		/// <remarks>extend method of <paramref name="span"/></remarks>
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
		/// <remarks>extend method of <paramref name="span"/></remarks>
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
		internal static string GetNumberString<T>(this T input, string format, IFormatProvider formatProvider) where T : unmanaged, IEquatable<T>, IFormattable
		{
			return input.GetNumberString(format, formatProvider, Convert.ToInt32(format[1..]));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static string GetNumberString<T>(this T input, string format, int precision) where T : unmanaged, IEquatable<T>, IFormattable
		{
			return input.GetNumberString(format, Resource.Culture, precision);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static string GetNumberStringComplex<T>(this Complex<T> input, string format, int precision) where T : unmanaged, IEquatable<T>, IComparable<T>, IFormattable
		{	// TODO: separate real and complex
			string r = input.Real.GetNumberString(format, Resource.Culture, precision);
			string i = input.Imag.GetNumberString(format, Resource.Culture, precision);
			return $"({r},{i})";
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static string GetFormatString(ref int precision)
		{
			// TODO: edit way of settings
			precision = precision <= 0 ? GlobalSettings.PrintConfig[PrintSetting.Precision] : precision;
			return "G" + precision;
		}

		/// <summary>
		/// Print out 1D array by <see cref="GlobalSettings"/> or the override <paramref name="precision"/> settings.
		/// </summary>
		/// <param name="input">array to print</param>
		/// <param name="precision">if precision &lt;= 0, the global setting is used</param>
		/// <returns>string representation</returns>
		/// <remarks>extend method of <paramref name="input"/></remarks>
		/// <typeparam name="T">the supported data types must be a <see cref="ValueType"/> and <see cref="IComparable"/></typeparam>
		/// <remarks>extend method of <paramref name="input"/></remarks>
		public static string ToVectorString<T>(this T[] input, int precision = -1) where T : unmanaged, IEquatable<T>, IFormattable
		{
			string format = GetFormatString(ref precision);
			return string.Join(Environment.NewLine, input.Select(a => a.GetNumberString(format, precision)));
		}

		/// <summary>
		/// Print out 1D sparse array by <see cref="GlobalSettings"/> or the override <paramref name="precision"/> settings.
		/// </summary>
		/// <param name="input">values of the vector to print</param>
		/// <param name="ind">indices of the values</param>
		/// <param name="precision">if precision &lt;= 0, the global setting is used</param>
		/// <returns>string representation</returns>
		/// <remarks>extend method of <paramref name="input"/></remarks>
		/// <typeparam name="T">the supported data types must be a <see cref="ValueType"/> and <see cref="IComparable"/></typeparam>
		/// <remarks>extend method of <paramref name="input"/></remarks>
		public static string ToSparseVectorString<T>(this T[] input, int[] ind, int precision = -1) where T : unmanaged, IEquatable<T>, IFormattable
		{
			string format = GetFormatString(ref precision);
			string func(int i, T a) => string.Format(Resource.Culture, "{0} -> {1}", i, a.GetNumberString(format, precision));
			return string.Join(Environment.NewLine, ind.Zip(input, func));
		}

		/// <summary>
		/// Print out 2D array by <see cref="GlobalSettings"/> or the override <paramref name="precision"/> settings.
		/// </summary>
		/// <param name="arr">array to print</param>
		/// <param name="hasMore">if the row is complete or not</param>
		/// <param name="precision">if precision &lt;= 0, the global setting is used</param>
		/// <returns>string representation</returns>
		/// <remarks>extend method of <paramref name="arr"/></remarks>
		/// <typeparam name="T">the supported data types must be a <see cref="ValueType"/> and <see cref="IComparable"/></typeparam>
		/// <remarks>extend method of <paramref name="arr"/></remarks>
		public static string ToMatrixString<T>(this T[,] arr, bool hasMore, int precision = -1) where T : unmanaged, IEquatable<T>, IFormattable
		{
			if (arr is null)
				throw new ArgumentNullException(nameof(arr), Resource.ArrayCannotNull);

			string format = GetFormatString(ref precision);
			StringBuilder sb = new StringBuilder();
			var (rows, cols) = arr.GetRowColumns();
			for (long i = 0; i < rows; i++)
			{
				string line = "";
				for (long j = 0; j < cols; j++)
				{
					line += arr[i, j].GetNumberString(format, precision);
					line += "  ";
				}
				if (hasMore)
					line += "...";
				sb.AppendLine(line.TrimEnd());
			}
			return sb.ToString();
		}

		/// <summary>
		/// Print out 2D sparse array by <see cref="GlobalSettings"/> or the override <paramref name="precision"/> settings.
		/// </summary>
		/// <param name="input">values of the vector to print</param>
		/// <param name="indx">row indices of the values</param>
		/// <param name="indy">column indices of the values</param>
		/// <param name="precision">if precision &lt;= 0, the global setting is used</param>
		/// <returns>string representation</returns>
		/// <remarks>extend method of <paramref name="input"/></remarks>
		/// <typeparam name="T">the supported data types must be a <see cref="ValueType"/> and <see cref="IComparable"/></typeparam>
		/// <remarks>extend method of <paramref name="input"/></remarks>
		public static string ToSparseMatrixString<T>(this T[] input, int[] indx, int[] indy, int precision = -1) where T : unmanaged, IEquatable<T>, IFormattable
		{
			if (input is null)
				throw new ArgumentNullException(nameof(input), Resource.ArrayCannotNull);
			if (indx is null)
				throw new ArgumentNullException(nameof(indx), Resource.ArrayCannotNull);
			if (indy is null)
				throw new ArgumentNullException(nameof(indy), Resource.ArrayCannotNull);

			string format = GetFormatString(ref precision);
			string func(int ix, int iy, T val) => string.Format(Resource.Culture, "({0}, {1}) -> {2}", ix, iy, val.GetNumberString(format, precision));
			return string.Join(Environment.NewLine, indx.Zip(indy, input, func));
		}
		#endregion
	}
}
