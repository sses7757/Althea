using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.Arm;
using System.Text;

using Althea.Linq;
using Althea.Resources;


namespace Althea.Helpers
{
	/// <summary>
	/// A static class that contains general purposed extension helper methods
	/// </summary>
	public static class ExtensionHelper
	{
		#region integer related
		// Ignore Spelling: nd
		/// <summary>
		/// Output an integer as a cardinality number, e.g. 0 -> 1st, 51 -> 52nd
		/// </summary>
		/// <param name="a">The input number</param>
		/// <returns>The ordinal representation string</returns>
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
		/// Create a number of type <typeparamref name="T2"/> from the given number of type <typeparamref name="T1"/>
		/// </summary>
		/// <typeparam name="T1">The input number type</typeparam>
		/// <typeparam name="T2">The output number type</typeparam>
		/// <param name="x">The input number</param>
		/// <returns><paramref name="x"/> as <typeparamref name="T2"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T2 As<T1, T2>(this T1 x) where T1 : INumber<T1> where T2 : INumber<T2> => T2.Create(x);

		/// <summary>
		/// Reverse the bits of <paramref name="a"/>
		/// </summary>
		/// <param name="a">The <see cref="int"/> whose bits shall be reversed</param>
		/// <returns>The <paramref name="a"/> after reversing bits</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int ReverseBits(this int a)
		{
			if (ArmBase.IsSupported)
			{
				return ArmBase.ReverseElementBits(a);
			}
			if (a == 0)
			{
				return 0;
			}
			// software fall-back
			uint n = (uint)a;
			n = (n >> 1) & 0x55555555U | (n << 1) & 0xaaaaaaaaU;
			n = (n >> 2) & 0x33333333U | (n << 2) & 0xccccccccU;
			n = (n >> 4) & 0x0f0f0f0fU | (n << 4) & 0xf0f0f0f0U;
			n = (n >> 8) & 0x00ff00ffU | (n << 8) & 0xff00ff00U;
			n = (n >> 16) & 0x0000ffffU | (n << 16) & 0xffff0000U;
			return (int)n;
		}

		/// <summary>
		/// Reverse the bits of <paramref name="a"/>
		/// </summary>
		/// <param name="a">The <see cref="long"/> whose bits shall be reversed</param>
		/// <returns>The <paramref name="a"/> after reversing bits</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static long ReverseBits(this long a)
		{
			if (ArmBase.Arm64.IsSupported)
			{
				return ArmBase.Arm64.ReverseElementBits(a);
			}
			if (a == 0)
			{
				return 0;
			}
			// software fall-back
			ulong n = (ulong)a;
			n = (n >> 1) & 0x5555555555555555UL | (n << 1) & 0xaaaaaaaaaaaaaaaaUL;
			n = (n >> 2) & 0x3333333333333333UL | (n << 2) & 0xccccccccccccccccUL;
			n = (n >> 4) & 0x0f0f0f0f0f0f0f0fUL | (n << 4) & 0xf0f0f0f0f0f0f0f0UL;
			n = (n >> 8) & 0x00ff00ff00ff00ffUL | (n << 8) & 0xff00ff00ff00ff00UL;
			n = (n >> 16) & 0x0000ffff0000ffffUL | (n << 16) & 0xffff0000ffff0000UL;
			n = (n >> 32) & 0xffffffff00000000UL | (n << 32) & 0xffffffff00000000UL;
			return (long)n;
		}

		/// <summary>
		/// Is the input integer a perfect square or not.
		/// </summary>
		/// <param name="input">The input integer</param>
		/// <returns>Whether <paramref name="input"/> is perfect square or not.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
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
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsPowerOfTwo<T>(this T x) where T : IBinaryInteger<T> => T.IsPow2(x);

		/// <summary>
		/// Get the nearest power of 2 integer which is not larger than the input integer
		/// </summary>
		/// <param name="x">The input integer</param>
		/// <returns><paramref name="x"/>'s the nearest power of 2</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T FloorPowerOfTwo<T>(this T x) where T : IBinaryInteger<T> => T.One << T.Log2(x).As<T, int>();

		/// <summary>
		/// Get the nearest power of 2 integer which is not less than the input integer
		/// </summary>
		/// <param name="x">The input integer</param>
		/// <returns><paramref name="x"/>'s the nearest power of 2</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T CeilPowerOfTwo<T>(this T x) where T : IBinaryInteger<T> => T.One << (T.Log2(x - T.One).As<T, int>() + 1);

		/// <summary>
		/// Get the floor round of log2(<paramref name="input"/>)
		/// </summary>
		/// <param name="input">input number</param>
		/// <returns>The nearest log2 of <paramref name="input"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T Log2<T>(this T input) where T : IBinaryInteger<T> => T.Log2(input);

		/// <summary>
		/// Get the ceiling round of log2(<paramref name="input"/>)
		/// </summary>
		/// <param name="input">input number</param>
		/// <returns>The ceiling round of log2 of <paramref name="input"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T CeilLog2<T>(this T input) where T : IBinaryInteger<T> => T.Log2(input - T.One) + T.One;

		/// <summary>
		/// Get the division and remainder of <paramref name="denominator"/> / <paramref name="numerator"/>
		/// </summary>
		/// <param name="denominator">The input denominator</param>
		/// <param name="numerator">The input numerator</param>
		/// <returns>The quotient and the remainder of <paramref name="denominator"/> / <paramref name="numerator"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static (T Quotient, T Remainder) DivRem<T>(this T denominator, T numerator) where T : IBinaryInteger<T> => T.DivRem(denominator, numerator);

		/// <summary>
		/// Count the <paramref name="input"/>'s bits which are set to 1
		/// </summary>
		/// <param name="input">input integer</param>
		/// <returns>The number <paramref name="input"/>'s bits set</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T PopCount<T>(this T input) where T : IBinaryInteger<T> => T.PopCount(input);

		/// <summary>
		/// Check whether the <paramref name="input"/>'s bit at <paramref name="bit"/> is set to 1
		/// </summary>
		/// <param name="input">input number</param>
		/// <param name="bit">bit position</param>
		/// <returns>whether the <paramref name="input"/>'s bit at <paramref name="bit"/> is set to 1</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsBitSet<T>(this T input, byte bit) where T : IBinaryInteger<T> => (input & T.Create(1 << bit)) == T.Zero;

		/// <summary>
		/// Check whether the <paramref name="input"/>'s bit at <paramref name="bit"/> is set to 1
		/// </summary>
		/// <param name="input">input number</param>
		/// <param name="bit">bit position</param>
		/// <returns>whether the <paramref name="input"/>'s bit at <paramref name="bit"/> is set to 1</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T SetBit<T>(this T input, byte bit) where T : IBinaryInteger<T> => input | T.Create(1 << bit);

		/// <summary>
		/// Check whether the <paramref name="input"/>'s bit at <paramref name="bit"/> is set to 1
		/// </summary>
		/// <param name="input">input number</param>
		/// <param name="bit">bit position</param>
		/// <returns>whether the <paramref name="input"/>'s bit at <paramref name="bit"/> is set to 1</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T ResetBit<T>(this T input, byte bit) where T : IBinaryInteger<T> => input & ~T.Create(1 << bit);
		#endregion

		#region time related
		// Ignore Spelling: ss
		/// <summary>
		/// Convert a <see cref="TimeSpan"/> into a string representation of total minutes and rest of them (seconds and smaller ones).
		/// </summary>
		/// <param name="span">The time span</param>
		/// <param name="restFormat">an optional format string of the rest, see <see cref="TimeSpan.ToString(string)"/></param>
		/// <returns>The string representation</returns>
		public static string TotalMinutesString(this TimeSpan span, string restFormat = @"ss\.ff")
		{
			return $"{(int)span.TotalMinutes}:{span.ToString(restFormat)}s";
		}

		/// <summary>
		/// Convert a <see cref="TimeSpan"/> into a string representation of total hours and rest of them (minutes, seconds and smaller ones).
		/// </summary>
		/// <param name="span">The time span</param>
		/// <param name="restFormat">an optional format string of the rest, see <see cref="TimeSpan.ToString(string)"/></param>
		/// <returns>The string representation</returns>
		public static string TotalHoursString(this TimeSpan span, string restFormat = @"mm:ss\.ff")
		{
			return $"{(int)span.TotalHours}:{span.ToString(restFormat)}s";
		}
		#endregion

		#region string related
		/// <summary>
		/// Repeat the given <see cref="string"/> <paramref name="s"/> for <paramref name="n"/> times
		/// </summary>
		/// <param name="s">The given <see cref="string"/> to repeat</param>
		/// <param name="n">The repeat count</param>
		/// <returns><see cref="string.Empty"/> if <paramref name="n"/> == 0; otherwise, the repeated string</returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="n"/> is less than 0</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="s"/> is null or empty</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string RepeatString(this string s, int n)
		{
			if (n < 0)
				throw new ArgumentOutOfRangeException(nameof(n), n, Parameter.CannotNegative);
			if (string.IsNullOrEmpty(s))
				throw new ArgumentNullException(nameof(s));
			if (n == 0)
				return string.Empty;
			return new StringBuilder(s.Length * n).AppendJoin(s, new string[n + 1]).ToString();
		}

		/// <summary>
		/// Concatenates the members of a span, using the specified separator between each member.
		/// </summary>
		/// <typeparam name="T">The type of the members of values.</typeparam>
		/// <param name="span">A span that contains the objects to concatenate.</param>
		/// <param name="seperator">The character to use as a separator</param>
		/// <returns>A string that consists of the members of values delimited by the separator character.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string SpanJoin<T>(this Span<T> span, char seperator) where T : notnull
		{
			return SpanJoin((ReadOnlySpan<T>)span, seperator);
		}

		/// <summary>
		/// Concatenates the members of a span, using the specified separator between each member.
		/// </summary>
		/// <typeparam name="T">The type of the members of values.</typeparam>
		/// <param name="span">A span that contains the objects to concatenate.</param>
		/// <param name="seperator">The character to use as a separator</param>
		/// <returns>A string that consists of the members of values delimited by the separator character.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string SpanJoin<T>(this Span<T> span, string seperator) where T : notnull
		{
			return SpanJoin((ReadOnlySpan<T>)span, seperator);
		}

		/// <summary>
		/// Concatenates the members of a span, using the specified separator between each member.
		/// </summary>
		/// <typeparam name="T">The type of the members of values.</typeparam>
		/// <param name="span">A span that contains the objects to concatenate.</param>
		/// <param name="seperator">The character to use as a separator</param>
		/// <returns>A string that consists of the members of values delimited by the separator character.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string SpanJoin<T>(this ReadOnlySpan<T> span, char seperator) where T : notnull
		{
			if (span.IsEmpty)
				return string.Empty;
			int len = span.Length - 1;
			StringBuilder sb = new();
			for (int i = 0; i < len; i++)
			{
				sb.Append(span[i].ToString()).Append(seperator);
			}
			return sb.Append(span[len].ToString()).ToString();
		}

		/// <summary>
		/// Concatenates the members of a span, using the specified separator between each member.
		/// </summary>
		/// <typeparam name="T">The type of the members of values.</typeparam>
		/// <param name="span">A span that contains the objects to concatenate.</param>
		/// <param name="seperator">The character to use as a separator</param>
		/// <returns>A string that consists of the members of values delimited by the separator character.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string SpanJoin<T>(this ReadOnlySpan<T> span, string seperator) where T : notnull
		{
			if (span.IsEmpty)
				return string.Empty;
			int len = span.Length - 1;
			StringBuilder sb = new();
			for (int i = 0; i < len; i++)
			{
				sb.Append(span[i].ToString()).Append(seperator);
			}
			return sb.Append(span[len].ToString()).ToString();
		}

		/// <summary>
		/// Concatenates the multi-line <see cref="string"/> <paramref name="s"/> with <paramref name="prefix"/> and <paramref name="postfix"/> line-by-line
		/// </summary>
		/// <param name="s">The <see cref="string"/> to be concatenated with pre- and post- fix</param>
		/// <param name="prefix">The prefix <see cref="string"/> to be added at the start of each line</param>
		/// <param name="postfix">The postfix <see cref="string"/> to be added at the end of each line</param>
		/// <returns>The concatenated <see cref="string"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="s"/> is null or empty</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string MultilinePrePostfix(this string s, string? prefix = null, string? postfix = null)
		{
			if (string.IsNullOrEmpty(s))
				throw new ArgumentNullException(nameof(s));
			if (string.IsNullOrEmpty(prefix) && string.IsNullOrEmpty(postfix))
				return s;

			string[] ss = s.Split(Environment.NewLine);
			return string.Join(postfix + Environment.NewLine + prefix, ss);
		}

		/// <summary>
		/// Concatenates the multi-line <see cref="string"/>s <paramref name="left"/> and <paramref name="right"/> line-by-line
		/// </summary>
		/// <param name="left">The <see cref="string"/> to be concatenated at left</param>
		/// <param name="right">The <see cref="string"/> to be concatenated at right</param>
		/// <param name="prefix">The prefix <see cref="string"/> to be added at the start of each line</param>
		/// <param name="midfix">The mid-fix <see cref="string"/> to be added between each line of <paramref name="left"/> and <paramref name="right"/>, not used for lines with only <paramref name="left"/> or <paramref name="right"/></param>
		/// <param name="postfix">The postfix <see cref="string"/> to be added at the end of each line</param>
		/// <returns>The concatenated <see cref="string"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="left"/> or <paramref name="right"/> is null or empty</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string MultilineConcat(this string left, string right, string? prefix = null, string? midfix = null, string? postfix = null)
		{
			if (string.IsNullOrEmpty(left) && string.IsNullOrEmpty(right))
				throw new ArgumentException(Parameter.CannotAllNull);
			if (string.IsNullOrEmpty(left))
				return MultilinePrePostfix(right, prefix, postfix);
			if (string.IsNullOrEmpty(right))
				return MultilinePrePostfix(left, prefix, postfix);

			string[] ls = left.Split(Environment.NewLine), rs = right.Split(Environment.NewLine);
			StringBuilder sb = new(left.Length + rs.Length);
			int len = Math.Min(ls.Length, rs.Length);
			for (int i = 0; i < len; i++)
			{
				sb.Append(prefix).Append(ls[i]).Append(midfix).Append(rs[i]).AppendLine(postfix);
			}
			if ((ls.Length > len ? ls : rs.Length > len ? rs : null) is string[] ss)
			{
				string join = postfix + Environment.NewLine + prefix;
				sb.AppendJoin(join, ss[len..]);
			}
			else
			{
				sb.Remove(sb.Length - Environment.NewLine.Length, Environment.NewLine.Length);
			}
			return sb.ToString();
		}

		/// <summary>
		/// Concatenates the multi-line <paramref name="strings"/> line-by-line
		/// </summary>
		/// <param name="strings">The array of <see cref="string"/>s to be concatenated in order</param>
		/// <param name="prefix">The prefix <see cref="string"/> to be added at the start of each line</param>
		/// <param name="midfix">The mid-fix <see cref="string"/> to be added in between of each line</param>
		/// <param name="postfix">The postfix <see cref="string"/> to be added at the end of each line</param>
		/// <returns>The concatenated <see cref="string"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="strings"/> is null or empty</exception>
		/// <exception cref="ArgumentException">If <paramref name="strings"/> have different number of lines</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string MultilineConcat(this string[] strings, string? prefix = null, string? midfix = null, string? postfix = null)
		{
			if (strings is null || strings.Length == 0 || strings.Any(static s => string.IsNullOrEmpty(s)))
				throw new ArgumentNullException(nameof(strings));
			if (strings.Length == 1)
				return MultilinePrePostfix(strings[0], prefix, postfix);
			if (strings.Length == 2)
				return MultilineConcat(strings[0], strings[1], prefix, midfix, postfix);
			// otherwise
			int nStrings = strings.Length;
			string[][] ss = new string[strings.Length][];
			ss[0] = strings[0].Split(Environment.NewLine);
			int lines = ss[0].Length;
			int allLength = ss[0].Length;
			for (int i = 1; i < nStrings; i++)
			{
				ss[i] = strings[i].Split(Environment.NewLine);
				if (ss[i].Length != lines)
					throw new ArgumentException(Parameter.NotSameSize, nameof(strings));
				allLength = checked(allLength + ss[i].Length);
			}
			nStrings--;
			StringBuilder sb = new(allLength);
			for (int i = 0; i < lines; i++)
			{
				sb.Append(prefix);
				for (int j = 0; j < nStrings; j++)
				{
					sb.Append(ss[j][i]).Append(midfix);
				}
				sb.Append(ss[nStrings][i]);
				sb.AppendLine(postfix);
			}
			sb.Remove(sb.Length - Environment.NewLine.Length, Environment.NewLine.Length);
			return sb.ToString();
		}

		/// <summary>
		/// Pad some characters indicated by <paramref name="padding"/> to the left of <paramref name="str"/> and push its original content to the right.
		/// </summary>
		/// <param name="str">The input/output string as a <see cref="Span{T}"/> of <see cref="char"/></param>
		/// <param name="totalWidth">The total number of chars in <paramref name="str"/></param>
		/// <param name="currentWidth">The current number of chars in <paramref name="str"/>. If it is larger than <paramref name="totalWidth"/>, this method simply returns true.</param>
		/// <param name="padding">The character to pad, default space</param>
		/// <returns>The padded string of length <paramref name="totalWidth"/> if padded, or <paramref name="str"/> if not.</returns>
		public static Span<char> PadLeft(this Span<char> str, int totalWidth, int currentWidth, char padding = ' ')
		{
			if (totalWidth <= currentWidth)
				return str;
			if (currentWidth > 128)
			{ // exceeds desired stack limit
				new string(str[..currentWidth]).PadLeft(totalWidth, padding).CopyTo(str);
				return str;
			}
			int pad = totalWidth - currentWidth;
			Span<char> temp = stackalloc char[currentWidth];
			str[..currentWidth].CopyTo(temp);
			str[..pad].Fill(padding);
			temp.CopyTo(str[pad..]);
			return str[..totalWidth];
		}

		/// <summary>
		/// Pad some characters indicated by <paramref name="padding"/> to the right of <paramref name="str"/>.
		/// </summary>
		/// <param name="str">The input/output string as a <see cref="Span{T}"/> of <see cref="char"/></param>
		/// <param name="totalWidth">The total number of chars in <paramref name="str"/></param>
		/// <param name="currentWidth">The current number of chars in <paramref name="str"/>. If it is larger than <paramref name="totalWidth"/>, this method simply returns true.</param>
		/// <param name="padding">The character to pad, default space</param>
		/// <returns>The padded string of length <paramref name="totalWidth"/> if padded, or <paramref name="str"/> if not.</returns>
		public static Span<char> PadRight(this Span<char> str, int totalWidth, int currentWidth, char padding = ' ')
		{
			if (totalWidth <= currentWidth)
				return str;
			str[currentWidth..totalWidth].Fill(padding);
			return str[..totalWidth];
		}
		#endregion

		#region print related
		private static readonly string[] PrecisionDict;

		static ExtensionHelper()
		{
			PrecisionDict = new string[40];
			for (int i = 0; i < PrecisionDict.Length; i++)
			{
				PrecisionDict[i] = "G" + i;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool GetNumberString<T>(T input, Span<char> chars, out int charsWritten, IFormatProvider? provider, int precision) where T : ISpanFormattable, IAdditiveIdentity<T, T>, IEquatable<T>
		{
			if (!input.TryFormat(chars, out charsWritten, PrecisionDict[precision].AsSpan(), provider))
				return false;
			int totalLength = precision + 2;
			var padded = chars.PadLeft(totalLength, charsWritten);
			if (padded.Length > totalLength)
			{
				precision = 2 * precision - padded.Length + 2;
				if (!input.TryFormat(chars, out charsWritten, PrecisionDict[precision].AsSpan(), provider))
					return false;
				while (charsWritten > totalLength)
				{
					if (!input.TryFormat(chars, out charsWritten, PrecisionDict[--precision].AsSpan(), provider))
						return false;
				}
				chars.PadLeft(totalLength, charsWritten);
			}
			return true;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool GetIntegerString<T>(T input, Span<char> chars, out int charsWritten, IFormatProvider? provider) where T : ISpanFormattable
		{
			return input.TryFormat(chars, out charsWritten, string.Empty, provider);
		}

		/// <summary>
		/// Print out 1D array by <see cref="Settings"/> or the override <paramref name="precision"/> settings.
		/// </summary>
		/// <typeparam name="T">The supported data type</typeparam>
		/// <param name="input">The dense vector to print</param>
		/// <param name="precision">If <paramref name="precision"/> ≤ 0, the global setting is used</param>
		/// <param name="prefix">The prefix <see cref="string"/> to add at each line</param>
		/// <param name="postfix">The postfix <see cref="string"/> to add at each line</param>
		/// <param name="provider">The <see cref="IFormatProvider"/> used in formatting</param>
		/// <returns>The string representation of dense vector <paramref name="input"/> at <paramref name="precision"/></returns>
		/// <exception cref="FormatException">If any value in <paramref name="input"/> cannot be formatted</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string ToVectorString<T>(this Span<T> input, int precision = -1, string? prefix = null, string? postfix = null, IFormatProvider? provider = null) where T : ISpanFormattable, IAdditiveIdentity<T, T>, IEquatable<T>
		{
			return ToVectorString((ReadOnlySpan<T>)input, precision, prefix, postfix, provider);
		}

		/// <summary>
		/// Print out 1D array by <see cref="Settings"/> or the override <paramref name="precision"/> settings.
		/// </summary>
		/// <typeparam name="T">The supported data type</typeparam>
		/// <param name="input">The dense vector to print</param>
		/// <param name="precision">If <paramref name="precision"/> ≤ 0, the global setting is used</param>
		/// <param name="prefix">The prefix <see cref="string"/> to add at each line</param>
		/// <param name="postfix">The postfix <see cref="string"/> to add at each line</param>
		/// <param name="provider">The <see cref="IFormatProvider"/> used in formatting</param>
		/// <returns>The string representation of dense vector <paramref name="input"/> at <paramref name="precision"/></returns>
		/// <exception cref="FormatException">If any value in <paramref name="input"/> cannot be formatted</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string ToVectorString<T>(this ReadOnlySpan<T> input, int precision = -1, string? prefix = null, string? postfix = null, IFormatProvider? provider = null) where T : ISpanFormattable, IAdditiveIdentity<T, T>, IEquatable<T>
		{
			if (input.IsEmpty)
				return string.Empty;
			if (precision <= 0)
				precision = Settings.PrintPrecision;

			ReadOnlySpan<char> pre = prefix, pos = (postfix ?? string.Empty) + Environment.NewLine;
			int maxLength = input.Length * (precision + 8 + pre.Length + pos.Length);
			char[] chars = new char[maxLength];
			Span<char> str = chars;
			for (int i = 0; i < input.Length; i++)
			{
				pre.CopyTo(str); str = str[pre.Length..];
				if (!GetNumberString(input[i], str, out int numberStrWidth, provider, precision))
					throw new FormatException(Support.Format);
				str = str[numberStrWidth..];
				pos.CopyTo(str); str = str[pos.Length..];
			}
			return new(new ReadOnlySpan<char>(chars, 0, maxLength - str.Length));
		}

		/// <summary>
		/// Print out 1D sparse array by <see cref="Settings"/> or the override <paramref name="precision"/> settings.
		/// </summary>
		/// <typeparam name="T">The supported data type</typeparam>
		/// <param name="values">The values of the sparse vector to print</param>
		/// <param name="indices">The indices of the sparse vector to print</param>
		/// <param name="precision">If <paramref name="precision"/> ≤ 0, the global setting is used</param>
		/// <param name="prefix">The prefix <see cref="string"/> to add at each line</param>
		/// <param name="postfix">The postfix <see cref="string"/> to add at each line</param>
		/// <param name="provider">The <see cref="IFormatProvider"/> used in formatting</param>
		/// <returns>The string representation of sparse vector (<paramref name="values"/>, <paramref name="indices"/>) at <paramref name="precision"/></returns>
		/// <exception cref="ArgumentException">If <paramref name="values"/> and <paramref name="indices"/> have different lengths</exception>
		/// <exception cref="FormatException">If any value in <paramref name="values"/> or <paramref name="indices"/> cannot be formatted</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string ToSparseVectorString<T>(this Span<T> values, ReadOnlySpan<long> indices, int precision = -1, string? prefix = null, string? postfix = null, IFormatProvider? provider = null) where T : ISpanFormattable, IAdditiveIdentity<T, T>, IEquatable<T>
		{
			return ToSparseVectorString((ReadOnlySpan<T>)values, indices, precision, prefix, postfix, provider);
		}

		/// <summary>
		/// Print out 1D sparse array by <see cref="Settings"/> or the override <paramref name="precision"/> settings.
		/// </summary>
		/// <typeparam name="TVal">The supported data type</typeparam>
		/// <typeparam name="TInd">The supported index type</typeparam>
		/// <param name="values">The values of the sparse vector to print</param>
		/// <param name="indices">The indices of the sparse vector to print</param>
		/// <param name="precision">If <paramref name="precision"/> ≤ 0, the global setting is used</param>
		/// <param name="prefix">The prefix <see cref="string"/> to add at each line</param>
		/// <param name="postfix">The postfix <see cref="string"/> to add at each line</param>
		/// <param name="provider">The <see cref="IFormatProvider"/> used in formatting</param>
		/// <returns>The string representation of sparse vector (<paramref name="values"/>, <paramref name="indices"/>) at <paramref name="precision"/></returns>
		/// <exception cref="ArgumentException">If <paramref name="values"/> and <paramref name="indices"/> have different lengths</exception>
		/// <exception cref="FormatException">If any value in <paramref name="values"/> or <paramref name="indices"/> cannot be formatted</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string ToSparseVectorString<TVal, TInd>(this ReadOnlySpan<TVal> values, ReadOnlySpan<TInd> indices, int precision = -1, string? prefix = null, string? postfix = null, IFormatProvider? provider = null) where TVal : ISpanFormattable, IAdditiveIdentity<TVal, TVal>, IEquatable<TVal> where TInd : ISpanFormattable
		{
			if (values.Length != indices.Length)
				throw new ArgumentException(Parameter.NotSameSize);
			if (values.IsEmpty)
				return string.Empty;

			ReadOnlySpan<char> pre = prefix, pos = (postfix ?? string.Empty) + Environment.NewLine, mid = " -> ";
			int maxLength = values.Length * (precision + 8 + pre.Length + pos.Length + mid.Length);
			char[] chars = new char[maxLength];
			Span<char> str = chars;
			for (int i = 0; i < values.Length; i++)
			{
				pre.CopyTo(str); str = str[pre.Length..];
				if (!GetIntegerString(indices[i], str, out int numberStrWidth, provider))
					throw new FormatException(Support.Format);
				str = str[numberStrWidth..];
				mid.CopyTo(str); str = str[mid.Length..];
				if (!GetNumberString(values[i], str, out numberStrWidth, provider, precision))
					throw new FormatException(Support.Format);
				pos.CopyTo(str); str = str[pos.Length..];
			}
			return new(new ReadOnlySpan<char>(chars, 0, maxLength - str.Length));
		}

		/// <summary>
		/// Print out 2D array by <see cref="Settings"/> or the override <paramref name="precision"/> settings.
		/// </summary>
		/// <typeparam name="T">The supported data type</typeparam>
		/// <param name="matrix">The column-major values of the dense matrix to print</param>
		/// <param name="rows">The number of rows of the given matrix</param>
		/// <param name="precision">If <paramref name="precision"/> ≤ 0, the global setting is used</param>
		/// <param name="more">The neglected number of elements of each row of <paramref name="matrix"/>, less than 1 means no more elements</param>
		/// <param name="prefix">The prefix <see cref="string"/> to add at each line</param>
		/// <param name="postfix">The postfix <see cref="string"/> to add at each line</param>
		/// <param name="provider">The <see cref="IFormatProvider"/> used in formatting</param>
		/// <returns>The string representation of dense matrix <paramref name="matrix"/> at <paramref name="precision"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="rows"/> is not a positive number</exception>
		/// <exception cref="ArgumentException">If the length of <paramref name="matrix"/> cannot be divided by <paramref name="rows"/></exception>
		/// <exception cref="FormatException">If any value in <paramref name="matrix"/> cannot be formatted</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string ToMatrixString<T>(this Span<T> matrix, int rows, long more = 0, int precision = -1, string? prefix = null, string? postfix = null, IFormatProvider? provider = null) where T : ISpanFormattable, IAdditiveIdentity<T, T>, IEquatable<T>
		{
			return ToMatrixString((ReadOnlySpan<T>)matrix, rows, more, precision, prefix, postfix, provider);
		}

		/// <summary>
		/// Print out 2D array by <see cref="Settings"/> or the override <paramref name="precision"/> settings.
		/// </summary>
		/// <typeparam name="T">The supported data type</typeparam>
		/// <param name="matrix">The column-major values of the dense matrix to print</param>
		/// <param name="rows">The number of rows of the given matrix</param>
		/// <param name="precision">If <paramref name="precision"/> ≤ 0, the global setting is used</param>
		/// <param name="more">The neglected number of elements of each row of <paramref name="matrix"/>, less than 1 means no more elements</param>
		/// <param name="prefix">The prefix <see cref="string"/> to add at each line</param>
		/// <param name="postfix">The postfix <see cref="string"/> to add at each line</param>
		/// <param name="provider">The <see cref="IFormatProvider"/> used in formatting</param>
		/// <returns>The string representation of dense matrix <paramref name="matrix"/> at <paramref name="precision"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="rows"/> is not a positive number</exception>
		/// <exception cref="ArgumentException">If the length of <paramref name="matrix"/> cannot be divided by <paramref name="rows"/></exception>
		/// <exception cref="FormatException">If any value in <paramref name="matrix"/> cannot be formatted</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string ToMatrixString<T>(this ReadOnlySpan<T> matrix, int rows, long more = 0, int precision = -1, string? prefix = null, string? postfix = null, IFormatProvider? provider = null) where T : ISpanFormattable, IAdditiveIdentity<T, T>, IEquatable<T>
		{
			if (matrix.IsEmpty)
				return string.Empty;
			if (rows <= 0)
				throw new ArgumentOutOfRangeException(nameof(rows), rows, Parameter.MustPositive);
			if (matrix.Length % rows != 0)
				throw new ArgumentException(Other.CannotDivide);

			int cols = matrix.Length / rows;
			string moreStr = more > 0 ? string.Format("  " + Print.RowMore, more) : "  ";
			ReadOnlySpan<char> pre = prefix, pos = (postfix ?? string.Empty) + moreStr + Environment.NewLine;
			int maxLength = matrix.Length * (precision + 8) + rows * (pre.Length + pos.Length);
			char[] chars = new char[maxLength];
			Span<char> str = chars;
			for (int i = 0; i < rows; i++)
			{
				pre.CopyTo(str); str = str[pre.Length..];
				for (int j = 0; j < cols; j++)
				{
					if (!GetNumberString(matrix[i + j * rows], str, out int numberStrWidth, provider, precision))
						throw new FormatException(Support.Format);
					str = str[numberStrWidth..];
				}
				pos.CopyTo(str); str = str[pos.Length..];
			}
			return new(new ReadOnlySpan<char>(chars, 0, maxLength - str.Length));
		}

		/// <summary>
		/// Print out 2D sparse array by <see cref="Settings"/> or the override <paramref name="precision"/> settings.
		/// </summary>
		/// <typeparam name="TVal">The supported data type</typeparam>
		/// <typeparam name="TInd">The supported index type</typeparam>
		/// <param name="values">The values of the sparse matrix to print</param>
		/// <param name="indx">The row indices of the sparse matrix to print</param>
		/// <param name="indy">The column indices of the sparse matrix to print</param>
		/// <param name="precision">If <paramref name="precision"/> ≤ 0, the global setting is used</param>
		/// <param name="prefix">The prefix <see cref="string"/> to add at each line</param>
		/// <param name="postfix">The postfix <see cref="string"/> to add at each line</param>
		/// <param name="provider">The <see cref="IFormatProvider"/> used in formatting</param>
		/// <returns>The string representation of sparse matrix (<paramref name="values"/>, <paramref name="indx"/>, <paramref name="indy"/>) at <paramref name="precision"/></returns>
		/// <exception cref="ArgumentException">If <paramref name="values"/> and <paramref name="indx"/> and <paramref name="indy"/> have different lengths</exception>
		/// <exception cref="FormatException">If any value in <paramref name="values"/> or <paramref name="indx"/> or <paramref name="indy"/> cannot be formatted</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string ToSparseMatrixString<TVal, TInd>(this Span<TVal> values, ReadOnlySpan<TInd> indx, ReadOnlySpan<TInd> indy, int precision = -1, string? prefix = null, string? postfix = null, IFormatProvider? provider = null) where TVal : ISpanFormattable, IAdditiveIdentity<TVal, TVal>, IEquatable<TVal> where TInd : ISpanFormattable
		{
			return ToSparseMatrixString((ReadOnlySpan<TVal>)values, indx, indy, precision, prefix, postfix, provider);
		}

		/// <summary>
		/// Print out 2D sparse array by <see cref="Settings"/> or the override <paramref name="precision"/> settings.
		/// </summary>
		/// <typeparam name="TVal">The supported data type</typeparam>
		/// <typeparam name="TInd">The supported index type</typeparam>
		/// <param name="values">The values of the sparse matrix to print</param>
		/// <param name="indx">The row indices of the sparse matrix to print</param>
		/// <param name="indy">The column indices of the sparse matrix to print</param>
		/// <param name="precision">If <paramref name="precision"/> ≤ 0, the global setting is used</param>
		/// <param name="prefix">The prefix <see cref="string"/> to add at each line</param>
		/// <param name="postfix">The postfix <see cref="string"/> to add at each line</param>
		/// <param name="provider">The <see cref="IFormatProvider"/> used in formatting</param>
		/// <returns>The string representation of sparse matrix (<paramref name="values"/>, <paramref name="indx"/>, <paramref name="indy"/>) at <paramref name="precision"/></returns>
		/// <exception cref="ArgumentException">If <paramref name="values"/> and <paramref name="indx"/> and <paramref name="indy"/> have different lengths</exception>
		/// <exception cref="FormatException">If any value in <paramref name="values"/> or <paramref name="indx"/> or <paramref name="indy"/> cannot be formatted</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string ToSparseMatrixString<TVal, TInd>(this ReadOnlySpan<TVal> values, ReadOnlySpan<TInd> indx, ReadOnlySpan<TInd> indy, int precision = -1, string? prefix = null, string? postfix = null, IFormatProvider? provider = null) where TVal : ISpanFormattable, IAdditiveIdentity<TVal, TVal>, IEquatable<TVal> where TInd : ISpanFormattable
		{
			if (values.Length != indx.Length)
				throw new ArgumentException(Parameter.NotSameSize);
			if (values.Length != indy.Length)
				throw new ArgumentException(Parameter.NotSameSize);
			if (values.IsEmpty)
				return string.Empty;

			ReadOnlySpan<char> pre = prefix, pos = (postfix ?? string.Empty) + Environment.NewLine, mid = ") -> ", com = ", ";
			int maxLength = values.Length * (precision + 8 + pre.Length + pos.Length + mid.Length);
			char[] chars = new char[maxLength];
			Span<char> str = chars;
			for (int i = 0; i < values.Length; i++)
			{
				pre.CopyTo(str); str = str[pre.Length..];
				str[0] = '('; str = str[1..];
				if (!GetIntegerString(indx[i], str, out int numberStrWidth, provider))
					throw new FormatException(Support.Format);
				str = str[numberStrWidth..];
				com.CopyTo(str); str = str[com.Length..];
				if (!GetIntegerString(indy[i], str, out numberStrWidth, provider))
					throw new FormatException(Support.Format);
				str = str[numberStrWidth..];
				mid.CopyTo(str); str = str[mid.Length..];
				if (!GetNumberString(values[i], str, out numberStrWidth, provider, precision))
					throw new FormatException(Support.Format);
				pos.CopyTo(str); str = str[pos.Length..];
			}
			return new(new ReadOnlySpan<char>(chars, 0, maxLength - str.Length));
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
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
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
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static TArr ApplyToAlike<TArr, T>(this TArr array, Action<TArr> action)
			where TArr : Arrays.ValueArray<T>
			where T : unmanaged, INumber<T>
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
			if (check && (val < 0 || val >= length))
				throw new ArgumentOutOfRangeException(nameof(index), index, Parameter.InvalidValue);
			return val;
		}

		/// <summary>
		/// Calculate the start offset and length of range object using a collection length.
		/// </summary>
		/// <param name="range">The <see cref="Range"/></param>
		/// <param name="length">The length of the collection that the range will be used with. It has to be a positive value.</param>
		/// <param name="check">check parameters and result or not</param>
		/// <returns>The offset and length of <paramref name="range"/> under <paramref name="length"/></returns>
		/// <remarks>This is a <see cref="long"/> version of <see cref="Range.GetOffsetAndLength(int)"/> at x64 platforms.</remarks>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="range"/> is out of [0, <paramref name="length"/>)</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static (long Offset, long Length) GetOffsetAndCount(this Range range, long length, bool check = true)
		{
			if (check && length <= 0)
				throw new ArgumentOutOfRangeException(nameof(length), length, Parameter.MustPositive);
			long start = range.Start.GetPosition(length, check: false), end = range.End.GetPosition(length, check: false);
			if (check && (end <= start || start >= length || end < 0 || end > length))
				throw new ArgumentOutOfRangeException(nameof(range), range, Parameter.InvalidValue);
			return (start, end - start);
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
