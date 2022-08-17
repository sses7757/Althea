using System.Buffers;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;
using System.Text;

using Althea.Linq;
using Althea.Resources;


namespace Althea.Helpers;

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
	public static string ToOrdinal(this int a) => ToOrdinal((long)a);

	/// <summary>
	/// Output an integer as a cardinality number, e.g. 0 -> 1st, 51 -> 52nd
	/// </summary>
	/// <param name="a">The input number</param>
	/// <returns>The ordinal representation string</returns>
	/// <exception cref="ArgumentOutOfRangeException">If <paramref name="a"/> is smaller than 0</exception>
	public static string ToOrdinal(this long a)
	{
		if (a < 0)
			throw new ArgumentOutOfRangeException(nameof(a), a, ParameterError.CannotNegative);
		a++;
		long c = a % 10;
		return c switch
		{
			1 => $"{a}-st",
			2 => $"{a}-nd",
			3 => $"{a}-rd",
			_ => $"{a}-th",
		};
	}

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
	/// Check whether the <paramref name="input"/>'s bit at <paramref name="bit"/> is set to 1
	/// </summary>
	/// <param name="input">input number</param>
	/// <param name="bit">bit position</param>
	/// <returns>Whether the <paramref name="input"/>'s bit at <paramref name="bit"/> is set to 1</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsBitSet(this int input, byte bit) => (input & (1 << bit)) == 0;

	/// <summary>
	/// Check whether the <paramref name="input"/>'s bit at <paramref name="bit"/> is set to 0
	/// </summary>
	/// <param name="input">input number</param>
	/// <param name="bit">bit position</param>
	/// <returns>Whether the <paramref name="input"/>'s bit at <paramref name="bit"/> is set to 0</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsBitNotSet(this int input, byte bit) => (input & (1 << bit)) != 0;

	/// <summary>
	/// Set the <paramref name="input"/>'s bit at <paramref name="bit"/> to 1
	/// </summary>
	/// <param name="input">input number</param>
	/// <param name="bit">bit position</param>
	/// <returns>The <paramref name="input"/> with bit at <paramref name="bit"/> set to 1</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int SetBit(this int input, byte bit) => input | (1 << bit);

	/// <summary>
	/// Set the <paramref name="input"/>'s bit at <paramref name="bit"/> to 0
	/// </summary>
	/// <param name="input">input number</param>
	/// <param name="bit">bit position</param>
	/// <returns>The <paramref name="input"/> with bit at <paramref name="bit"/> set to 0</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int ResetBit(this int input, byte bit) => input & ~(1 << bit);

	/// <summary>
	/// Set the <paramref name="input"/>'s bit at <paramref name="bit"/> to 1
	/// </summary>
	/// <param name="input">input number</param>
	/// <param name="bit">bit position</param>
	/// <returns>The <paramref name="input"/> with bit at <paramref name="bit"/> set to 1</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static byte SetBit(this byte input, byte bit) => (byte)(input | (1 << bit));

	/// <summary>
	/// Set the <paramref name="input"/>'s bit at <paramref name="bit"/> to 0
	/// </summary>
	/// <param name="input">input number</param>
	/// <param name="bit">bit position</param>
	/// <returns>The <paramref name="input"/> with bit at <paramref name="bit"/> set to 0</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static byte ResetBit(this byte input, byte bit) => (byte)(input & ~(1 << bit));
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

	#region string relatedW
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
		StringBuilder sb = new("[");
		for (int i = 0; i < len; i++)
		{
			sb.Append(span[i].ToString()).Append(seperator);
		}
		return sb.Append(span[len].ToString()).Append(']').ToString();
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
		StringBuilder sb = new("[");
		for (int i = 0; i < len; i++)
		{
			sb.Append(span[i].ToString()).Append(seperator);
		}
		return sb.Append(span[len].ToString()).Append(']').ToString();
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
			throw new ArgumentException(ParameterError.CannotAllNull);
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
				throw new ArgumentException(ParameterError.NotSameSize, nameof(strings));
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
				throw new FormatException();
			str = str[numberStrWidth..];
			pos.CopyTo(str); str = str[pos.Length..];
		}
		return new(new ReadOnlySpan<char>(chars, 0, maxLength - str.Length));
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
	public static string ToSparseVectorString<TVal, TInd>(this Span<TVal> values, ReadOnlySpan<TInd> indices, int precision = -1, string? prefix = null, string? postfix = null, IFormatProvider? provider = null) where TVal : ISpanFormattable, IAdditiveIdentity<TVal, TVal>, IEquatable<TVal> where TInd : ISpanFormattable
	{
		return ToSparseVectorString((ReadOnlySpan<TVal>)values, indices, precision, prefix, postfix, provider);
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
			throw new ArgumentException(ParameterError.NotSameSize);
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
				throw new FormatException();
			str = str[numberStrWidth..];
			mid.CopyTo(str); str = str[mid.Length..];
			if (!GetNumberString(values[i], str, out _, provider, precision))
				throw new FormatException();
			pos.CopyTo(str); str = str[pos.Length..];
		}
		return new(new ReadOnlySpan<char>(chars, 0, maxLength - str.Length));
	}

	/// <summary>
	/// Print out 2D array by <see cref="Settings"/> or the override <paramref name="precision"/> settings.
	/// </summary>
	/// <typeparam name="T">The supported data type</typeparam>
	/// <param name="matrix">The column-major values of the dense matrix to print</param>
	/// <param name="leadDim">The leading dimension of the given matrix</param>
	/// <param name="rowMajor">Whether <paramref name="leadDim"/> is number of rows or number of columns</param>
	/// <param name="precision">If <paramref name="precision"/> ≤ 0, the global setting is used</param>
	/// <param name="more">The neglected number of elements of each row/column of <paramref name="matrix"/>, ≤ 0 means no more elements</param>
	/// <param name="prefix">The prefix <see cref="string"/> to add at each line</param>
	/// <param name="postfix">The postfix <see cref="string"/> to add at each line</param>
	/// <param name="provider">The <see cref="IFormatProvider"/> used in formatting</param>
	/// <returns>The string representation of dense matrix <paramref name="matrix"/> at <paramref name="precision"/></returns>
	/// <exception cref="ArgumentOutOfRangeException">If <paramref name="leadDim"/> is not a positive number</exception>
	/// <exception cref="ArgumentException">If the length of <paramref name="matrix"/> cannot be divided by <paramref name="leadDim"/></exception>
	/// <exception cref="FormatException">If any value in <paramref name="matrix"/> cannot be formatted</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static string ToMatrixString<T>(this Span<T> matrix, int leadDim, bool rowMajor = false, long more = 0, int precision = -1, string? prefix = null, string? postfix = null, IFormatProvider? provider = null) where T : ISpanFormattable, IAdditiveIdentity<T, T>, IEquatable<T>
	{
		return ToMatrixString((ReadOnlySpan<T>)matrix, leadDim, rowMajor, more, precision, prefix, postfix, provider);
	}

	/// <summary>
	/// Print out 2D array by <see cref="Settings"/> or the override <paramref name="precision"/> settings.
	/// </summary>
	/// <typeparam name="T">The supported data type</typeparam>
	/// <param name="matrix">The column-major values of the dense matrix to print</param>
	/// <param name="leadDim">The leading dimension of the given matrix</param>
	/// <param name="rowMajor">Whether <paramref name="leadDim"/> is number of rows or number of columns</param>
	/// <param name="precision">If <paramref name="precision"/> ≤ 0, the global setting is used</param>
	/// <param name="more">The neglected number of elements of each row/column of <paramref name="matrix"/>, ≤ 0 means no more elements</param>
	/// <param name="prefix">The prefix <see cref="string"/> to add at each line</param>
	/// <param name="postfix">The postfix <see cref="string"/> to add at each line</param>
	/// <param name="provider">The <see cref="IFormatProvider"/> used in formatting</param>
	/// <returns>The string representation of dense matrix <paramref name="matrix"/> at <paramref name="precision"/></returns>
	/// <exception cref="ArgumentOutOfRangeException">If <paramref name="leadDim"/> is not a positive number</exception>
	/// <exception cref="ArgumentException">If the length of <paramref name="matrix"/> cannot be divided by <paramref name="leadDim"/></exception>
	/// <exception cref="FormatException">If any value in <paramref name="matrix"/> cannot be formatted</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static string ToMatrixString<T>(this ReadOnlySpan<T> matrix, int leadDim, bool rowMajor = false, long more = 0, int precision = -1, string? prefix = null, string? postfix = null, IFormatProvider? provider = null) where T : ISpanFormattable, IAdditiveIdentity<T, T>, IEquatable<T>
	{
		if (matrix.IsEmpty)
			return string.Empty;
		if (leadDim <= 0)
			throw new ArgumentOutOfRangeException(nameof(leadDim), leadDim, ParameterError.MustPositive);
		if (matrix.Length % leadDim != 0)
			throw new ArgumentException(ArithmeticError.CannotDivide);

		int cols = matrix.Length / leadDim;
		string moreStr = more > 0 ? string.Format("  " + Print.RowMore, more) : "  ";
		ReadOnlySpan<char> pre = prefix, pos = (postfix ?? string.Empty) + moreStr + Environment.NewLine;
		int maxLength = matrix.Length * (precision + 8) + leadDim * (pre.Length + pos.Length);
		char[] chars = new char[maxLength];
		Span<char> str = chars;
		for (int i = 0; i < leadDim; i++)
		{
			pre.CopyTo(str); str = str[pre.Length..];
			for (int j = 0; j < cols; j++)
			{
				if (!GetNumberString(rowMajor ? matrix[i * leadDim + j] : matrix[i + j * leadDim], str, out int numberStrWidth, provider, precision))
					throw new FormatException();
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
			throw new ArgumentException(ParameterError.NotSameSize);
		if (values.Length != indy.Length)
			throw new ArgumentException(ParameterError.NotSameSize);
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
				throw new FormatException();
			str = str[numberStrWidth..];
			com.CopyTo(str); str = str[com.Length..];
			if (!GetIntegerString(indy[i], str, out numberStrWidth, provider))
				throw new FormatException();
			str = str[numberStrWidth..];
			mid.CopyTo(str); str = str[mid.Length..];
			if (!GetNumberString(values[i], str, out _, provider, precision))
				throw new FormatException();
			pos.CopyTo(str); str = str[pos.Length..];
		}
		return new(new ReadOnlySpan<char>(chars, 0, maxLength - str.Length));
	}
	#endregion

	#region clone related
	/// <summary>
	/// Safely apply <paramref name="action"/> to a clone of <paramref name="array"/>. When <paramref name="action"/> throws error, the new copied array will be safely disposed.
	/// </summary>
	/// <typeparam name="T">The array that is <see cref="ICloneable{T}"/> and <see cref="IDisposable"/></typeparam>
	/// <param name="array">The array to be acted by <paramref name="action"/></param>
	/// <param name="action">The <see cref="Action{T}"/> to apply</param>
	/// <returns>The cloned <paramref name="array"/> after applying <paramref name="action"/></returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T ApplyToClone<T>(this T array, Action<T> action) where T : IDisposable, ICloneable<T>
	{
		var clone = array.Clone();
		try
		{
			var t = clone;
			action.Invoke(t);
			return t;
		}
		catch (Exception)
		{
			clone?.Dispose();
			throw;
		}
	}

	/// <summary>
	/// Safely apply <paramref name="action"/> to a new alike one created from <paramref name="array"/>. When <paramref name="action"/> throws error, the new copied array will be safely disposed.
	/// </summary>
	/// <typeparam name="T">The array that is <see cref="ICreateAlike{T}"/> and <see cref="IDisposable"/></typeparam>
	/// <param name="array">The array to be acted by <paramref name="action"/></param>
	/// <param name="action">The <see cref="Action{T}"/> to apply</param>
	/// <returns>The alike <paramref name="array"/> after applying <paramref name="action"/></returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T ApplyToAlike<T>(this T array, Action<T> action) where T : IDisposable, ICreateAlike<T>
	{
		var alike = array.CreateAlike();
		try
		{
			var t = alike;
			action.Invoke(t);
			return t;
		}
		catch (Exception)
		{
			alike?.Dispose();
			throw;
		}
	}

	/// <summary>
	/// Safely apply <paramref name="action"/> to a clone of <paramref name="array"/>. When <paramref name="action"/> throws error, the new copied array will be safely disposed.
	/// </summary>
	/// <typeparam name="T">The array that is <see cref="ICloneable{T}"/> and <see cref="IDisposable"/></typeparam>
	/// <param name="array">The array to be acted by <paramref name="action"/></param>
	/// <param name="action">The <see cref="Action{T, T}"/> whose first input is <paramref name="array"/> and second input is its clone</param>
	/// <returns>The cloned <paramref name="array"/> after applying <paramref name="action"/></returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T ApplyToClone<T>(this T array, Action<T, T> action) where T : IDisposable, ICloneable<T>
	{
		var clone = array.Clone();
		try
		{
			var t = clone;
			action.Invoke(array, t);
			return t;
		}
		catch (Exception)
		{
			clone?.Dispose();
			throw;
		}
	}

	/// <summary>
	/// Safely apply <paramref name="action"/> to a new alike one created from <paramref name="array"/>. When <paramref name="action"/> throws error, the new copied array will be safely disposed.
	/// </summary>
	/// <typeparam name="T">The array that is <see cref="ICreateAlike{T}"/> and <see cref="IDisposable"/></typeparam>
	/// <param name="array">The array to be acted by <paramref name="action"/></param>
	/// <param name="action">The <see cref="Action{T, T}"/> whose first input is <paramref name="array"/> and second input is its alike one</param>
	/// <returns>The alike <paramref name="array"/> after applying <paramref name="action"/></returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T ApplyToAlike<T>(this T array, Action<T, T> action) where T : IDisposable, ICreateAlike<T>
	{
		var alike = array.CreateAlike();
		try
		{
			var t = alike;
			action.Invoke(array, t);
			return t;
		}
		catch (Exception)
		{
			alike?.Dispose();
			throw;
		}
	}
	#endregion

	#region index and range related
	/// <summary>
	/// Remove the last occurrence of <paramref name="value"/> in <paramref name="list"/> if present by swapping it with the last element in <paramref name="list"/>.
	/// </summary>
	/// <typeparam name="T">The data type of <paramref name="list"/></typeparam>
	/// <param name="list">The <see cref="List{T}"/> whose last <paramref name="value"/> will be removed</param>
	/// <param name="value">The value to remove</param>
	/// <returns>Success or not.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool SwapRemove<T>(this List<T> list, T value)
	{
		int find = list.LastIndexOf(value);
		if (find < 0)
			return false;
		if (find != list.Count - 1)
			list[find] = list[^1];
		list.RemoveAt(list.Count - 1);
		return true;
	}

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
			throw new ArgumentOutOfRangeException(nameof(length), length, ParameterError.MustPositive);
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
			throw new ArgumentOutOfRangeException(nameof(index), index, ParameterError.InvalidValue);
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
			throw new ArgumentOutOfRangeException(nameof(length), length, ParameterError.MustPositive);
		long start = range.Start.GetPosition(length, check: false), end = range.End.GetPosition(length, check: false);
		if (check && (end <= start || start >= length || end < 0 || end > length))
			throw new ArgumentOutOfRangeException(nameof(range), range, ParameterError.InvalidValue);
		return (start, end - start);
	}
	#endregion

	#region disposable array extends
	/// <summary>
	/// Dispose and clear a general span
	/// </summary>
	/// <typeparam name="T">The disposable type</typeparam>
	/// <param name="span">The span to clear</param>
	public static void ClearList<T>(this Span<T> span) where T : IDisposable
	{
		for (int i = 0; i < span.Length; i++)
		{
			span[i]?.Dispose();
		}
		span.Clear();
	}

	/// <summary>
	/// Dispose and clear a general span
	/// </summary>
	/// <typeparam name="T">The disposable type</typeparam>
	/// <param name="span">The span to clear</param>
	public static void ClearList<T>(this ReadOnlySpan<T> span) where T : IDisposable
	{
		for (int i = 0; i < span.Length; i++)
		{
			span[i]?.Dispose();
		}
	}

	/// <summary>
	/// Dispose and clear a general array
	/// </summary>
	/// <typeparam name="T">The disposable type</typeparam>
	/// <param name="array">The array to clear</param>
	public static void ClearList<T>(this T[] array) where T : IDisposable
	{
		if (array is null)
			return;
		for (int i = 0; i < array.Length; i++)
		{
			array[i]?.Dispose();
		}
		System.Array.Clear(array, 0, array.Length);
	}

	/// <summary>
	/// Dispose and clear a general list
	/// </summary>
	/// <typeparam name="T">The disposable type</typeparam>
	/// <param name="list">The list to clear</param>
	public static void ClearList<T>(this List<T> list) where T : IDisposable
	{
		if (list is null)
			return;
		for (int i = 0; i < list.Count; i++)
		{
			list[i]?.Dispose();
		}
		list.ForEach(l => l?.Dispose());
		list.Clear();
	}

	/// <summary>
	/// Dispose a general read-only list
	/// </summary>
	/// <typeparam name="T">The disposable type</typeparam>
	/// <param name="list">The read-only list to dispose</param>
	public static void ClearList<T>(this IReadOnlyList<T> list) where T : IDisposable
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
	/// <typeparam name="TD">The disposable type</typeparam>
	/// <param name="dict">The dictionary to dispose</param>
	public static void ClearDict<T, TD>(this IReadOnlyDictionary<T, TD> dict) where TD : IDisposable
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


/// <summary>
/// A static class that contains helper functions for <see cref="Span{T}"/>s and <see cref="ReadOnlySpan{T}"/>s.
/// </summary>
public static class SpanHelper
{
	#region create
	/// <summary>
	/// Convert an array of <see cref="byte"/> to a <see cref="Span{T}"/> of <see cref="UnsignedInt8"/>.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Span<UnsignedInt8> AsAux(this byte[]? array) => array is null || array.Length == 0 ? default : MemoryMarshal.CreateSpan(ref Unsafe.As<byte, UnsignedInt8>(ref array[0]), array.Length);

	/// <summary>
	/// Creates a new read-only span over a portion of a regular managed object.
	/// </summary>
	/// <typeparam name="T">The data type</typeparam>
	/// <param name="value">The reference to the first element</param>
	/// <param name="length">The number of elements in <paramref name="value"/></param>
	/// <returns>A <see cref="ReadOnlySpan{T}"/> on <paramref name="value"/>.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ReadOnlySpan<T> CreateReadOnlySpan<T>(in T value, int length) => MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in value), length);

	/// <summary>
	/// Creates a new read-only span over a portion of a regular managed object.
	/// </summary>
	/// <typeparam name="T">The data type</typeparam>
	/// <param name="value">The reference to the first element</param>
	/// <param name="length">The number of elements in <paramref name="value"/></param>
	/// <returns>A <see cref="Span{T}"/> on <paramref name="value"/>.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Span<T> CreateSpan<T>(ref T value, int length) => MemoryMarshal.CreateSpan(ref value, length);
	#endregion

	#region temporary span
	/// <summary>
	/// The ref struct used as a temporary <see cref="Span{T}"/>
	/// </summary>
	/// <typeparam name="T">The data type</typeparam>
	public readonly ref struct TemporarySpan<T> where T : notnull
	{
		/// <summary>
		/// Get the underlying <see cref="Span{T}"/>.
		/// </summary>
		public Span<T> Data { get; }

		/// <summary>
		/// Check whether this <see cref="TemporarySpan{T}"/> is empty or not.
		/// </summary>
		public bool IsEmpty => Data.IsEmpty;

		private readonly byte[]? array;

		internal TemporarySpan(int length)
		{
			this.array = ArrayPool<byte>.Shared.Rent(length * Unsafe.SizeOf<T>());
			this.Data = CreateSpan(ref Unsafe.As<byte, T>(ref this.array[0]), length);
		}

		/// <summary>
		/// Return this temporary span's underlying array if it is rented from <see cref="ArrayPool{T}"/>.
		/// </summary>
		public void Dispose()
		{
			if (this.array is not null)
				ArrayPool<byte>.Shared.Return(this.array);
		}
	}

	/// <summary>
	/// Check whether the given <paramref name="length"/> and type <typeparamref name="T"/> fits the <see cref="Settings.StackAllocLimit"/> or not.<br/>
	/// If the size is small, array of <typeparamref name="T"/> will not be created and you shall <c>stackalloc <typeparamref name="T"/>[<paramref name="length"/>]</c> yourself.
	/// </summary>
	/// <typeparam name="T">The data type</typeparam>
	/// <param name="length">The desired length to allocate</param>
	/// <returns>The allocated or rented array inside a disposable <see cref="TemporarySpan{T}"/> or a empty one.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TemporarySpan<T> CheckStackLimit<T>(this int length) where T : notnull
	{
		if (length * Unsafe.SizeOf<T>() > Settings.StackAllocLimit)
			return new(length);
		else
			return default;
	}
	#endregion

	#region reference
	/// <summary>
	/// Cast the given struct <paramref name="value"/>'s reference to type <typeparamref name="TTo"/> directly.
	/// </summary>
	/// <typeparam name="TFrom">The from type to convert</typeparam>
	/// <typeparam name="TTo">The destination type to convert</typeparam>
	/// <param name="value">The read-only reference to the value</param>
	/// <returns>The reference to <paramref name="value"/> with type <typeparamref name="TTo"/>.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ref TTo As<TFrom, TTo>(in TFrom value) where TFrom : struct where TTo : struct => ref Unsafe.As<TFrom, TTo>(ref Unsafe.AsRef(in value));

	/// <summary>
	/// Returns a reference to the element of the span at index 0.
	/// </summary>
	/// <typeparam name="T">The type of items in the span.</typeparam>
	/// <param name="span">The <see cref="Span{T}"/> from which the reference is retrieved.</param>
	/// <returns>A reference to the element at index 0.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ref T Ref<T>(this Span<T> span)
	{
		return ref MemoryMarshal.GetReference(span);
	}

	/// <summary>
	/// Returns a reference to the element of the span at index 0.
	/// </summary>
	/// <typeparam name="T">The type of items in the span.</typeparam>
	/// <param name="span">The <see cref="ReadOnlySpan{T}"/> from which the reference is retrieved.</param>
	/// <returns>A reference to the element at index 0.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ref T Ref<T>(this ReadOnlySpan<T> span)
	{
		return ref MemoryMarshal.GetReference(span);
	}

	/// <summary>
	/// Cast the given <paramref name="span"/> from <typeparamref name="TFrom"/> to <typeparamref name="TTo"/> without checking by directly view the underlying memory in a different way, i.e., the <see cref="ReadOnlySpan{T}.Length"/> will change accordingly.
	/// </summary>
	/// <typeparam name="TFrom">The conversion from type, must be a struct</typeparam>
	/// <typeparam name="TTo">The conversion to type, must be a struct</typeparam>
	/// <param name="span">The <see cref="ReadOnlySpan{TFrom}"/> to be converted</param>
	/// <returns>The converted <see cref="ReadOnlySpan{TTo}"/> with changed <see cref="ReadOnlySpan{T}.Length"/> if <typeparamref name="TTo"/> is not <typeparamref name="TFrom"/></returns>
	/// <exception cref="ArgumentException">If <c><paramref name="span"/>.<see cref="ReadOnlySpan{T}.Length">Length</see> * <typeparamref name="TFrom"/> / <typeparamref name="TTo"/></c> is not an integer</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static ReadOnlySpan<TTo> UncheckAs<TFrom, TTo>(this ReadOnlySpan<TFrom> span) where TFrom : unmanaged where TTo : unmanaged
	{
		if (sizeof(TTo) == sizeof(TFrom))
		{
			return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<TFrom, TTo>(ref span.Ref()), span.Length);
		}
		long size = (long)span.Length * sizeof(TFrom);
		if (size % sizeof(TTo) != 0)
			throw new ArgumentException(ArithmeticError.CannotDivide);
		return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<TFrom, TTo>(ref span.Ref()), (int)(size / sizeof(TTo)));
	}

	/// <summary>
	/// Cast the given <paramref name="span"/> from <typeparamref name="TFrom"/> to <typeparamref name="TTo"/> without checking by directly view the underlying memory in a different way, i.e., the <see cref="Span{T}.Length"/> will change accordingly.
	/// </summary>
	/// <typeparam name="TFrom">The conversion from type, must be a struct</typeparam>
	/// <typeparam name="TTo">The conversion to type, must be a struct</typeparam>
	/// <param name="span">The <see cref="Span{TFrom}"/> to be converted</param>
	/// <returns>The converted <see cref="Span{TTo}"/> with changed <see cref="Span{T}.Length"/> if <typeparamref name="TTo"/> is not <typeparamref name="TFrom"/></returns>
	/// <exception cref="ArgumentException">If <c><paramref name="span"/>.<see cref="ReadOnlySpan{T}.Length">Length</see> * <typeparamref name="TFrom"/> / <typeparamref name="TTo"/></c> is not an integer</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static Span<TTo> UncheckAs<TFrom, TTo>(this Span<TFrom> span) where TFrom : unmanaged where TTo : unmanaged
	{
		if (sizeof(TTo) == sizeof(TFrom))
		{
			return MemoryMarshal.CreateSpan(ref Unsafe.As<TFrom, TTo>(ref span.Ref()), span.Length);
		}
		long size = (long)span.Length * sizeof(TFrom);
		if (size % sizeof(TTo) != 0)
			throw new ArgumentException(ArithmeticError.CannotDivide);
		return MemoryMarshal.CreateSpan(ref Unsafe.As<TFrom, TTo>(ref span.Ref()), (int)(size / sizeof(TTo)));
	}

	/// <summary>
	/// Cast the given <paramref name="span"/> from <typeparamref name="TFrom"/> to <typeparamref name="TTo"/> by directly view the underlying memory in a different way, i.e., the <see cref="ReadOnlySpan{T}.Length"/> will change accordingly.
	/// </summary>
	/// <typeparam name="TFrom">The conversion from type, must be a struct</typeparam>
	/// <typeparam name="TTo">The conversion to type, must be a struct</typeparam>
	/// <param name="span">The <see cref="ReadOnlySpan{TFrom}"/> to be casted</param>
	/// <returns>The converted <see cref="ReadOnlySpan{TTo}"/> with changed <see cref="ReadOnlySpan{T}.Length"/> if <typeparamref name="TTo"/> is not <typeparamref name="TFrom"/></returns>
	/// <exception cref="ArgumentException">If <typeparamref name="TFrom"/> or <typeparamref name="TTo"/> contains references or pointers.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ReadOnlySpan<TTo> As<TFrom, TTo>(this ReadOnlySpan<TFrom> span) where TFrom : struct where TTo : struct
	{
		return MemoryMarshal.Cast<TFrom, TTo>(span);
	}

	/// <summary>
	/// Cast the given <paramref name="span"/> from <typeparamref name="TFrom"/> to <typeparamref name="TTo"/> by directly view the underlying memory in a different way, i.e., the <see cref="Span{T}.Length"/> will change accordingly.
	/// </summary>
	/// <typeparam name="TFrom">The conversion from type, must be a struct</typeparam>
	/// <typeparam name="TTo">The conversion to type, must be a struct</typeparam>
	/// <param name="span">The <see cref="Span{TFrom}"/> to be casted</param>
	/// <returns>The converted <see cref="Span{TTo}"/> with changed <see cref="Span{T}.Length"/> if <typeparamref name="TTo"/> is not <typeparamref name="TFrom"/></returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Span<TTo> As<TFrom, TTo>(this Span<TFrom> span) where TFrom : struct where TTo : struct
	{
		return MemoryMarshal.Cast<TFrom, TTo>(span);
	}

	/// <summary>
	/// Cast the given <paramref name="span"/> from <see cref="IntPtr"/> type to any class type <typeparamref name="T"/> by directly view the underlying memory in a different way, i.e., the <see cref="ReadOnlySpan{T}.Length"/> will not be changed.
	/// </summary>
	/// <typeparam name="T">Any class type as the output type</typeparam>
	/// <param name="span">The <see cref="ReadOnlySpan{T}"/> of <see cref="IntPtr"/> to be casted</param>
	/// <returns>The casted <see cref="ReadOnlySpan{T}"/> of <typeparamref name="T"/></returns>
	/// <remarks>The <paramref name="span"/> shall NOT point to a memory block which is not fixed during the usage of the returned span. Usually, the <paramref name="span"/> can be generated by stack allocation.</remarks>
	public static ReadOnlySpan<T> AsClassType<T>(this ReadOnlySpan<IntPtr> span) where T : class
	{
		if (span.IsEmpty)
			return default;
		else
			return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<IntPtr, T>(ref span.Ref()), span.Length);
	}

	/// <summary>
	/// Cast the given <paramref name="span"/> from <see cref="IntPtr"/> type to any class type <typeparamref name="T"/> by directly view the underlying memory in a different way, i.e., the <see cref="Span{T}.Length"/> will not be changed.
	/// </summary>
	/// <typeparam name="T">Any class type as the output type</typeparam>
	/// <param name="span">The <see cref="Span{T}"/> of <see cref="IntPtr"/> to be casted</param>
	/// <returns>The casted <see cref="Span{T}"/> of <typeparamref name="T"/></returns>
	/// <remarks>The <paramref name="span"/> shall NOT point to a memory block which is not fixed during the usage of the returned span. Usually, the <paramref name="span"/> can be generated by stack allocation.</remarks>
	public static Span<T> AsClassType<T>(this Span<IntPtr> span) where T : class
	{
		if (span.IsEmpty)
			return default;
		else
			return MemoryMarshal.CreateSpan(ref Unsafe.As<IntPtr, T>(ref span.Ref()), span.Length);
	}
	#endregion

	#region generic type string
	/// <summary>
	/// Get the name string representation of given <paramref name="type"/> together with its generic parameters
	/// </summary>
	/// <param name="type">The given <see cref="Type"/> to get name</param>
	/// <returns>The name string representation of given <paramref name="type"/> or null if the given <paramref name="type"/>'s name cannot be obtained.</returns>
	public static string GetGenericString(this Type type)
	{
		string name = type.Name;
		if (type.IsGenericType)
		{
			var args = type.GenericTypeArguments;
			name += $"<{string.Join(", ", args.Select(a => a.GetGenericString()).ToArray())}>";
		}
		return name;
	}

	internal static Type? GetTypeWithPostfix(this Type type, string postfix, int skipGeneric = 0)
	{
		Type[] generics = type.GenericTypeArguments;
		string fullName = type.AssemblyQualifiedName ?? throw new ArgumentException(ParameterError.UnexpectedValue, nameof(type));
		int genericStart = fullName.IndexOf('`');
		string postfixedName;
		if (genericStart >= 0)
		{
			int genericEnd = fullName.IndexOf("]]");
			if (genericEnd < 0)
				throw new ArgumentException(ParameterError.UnexpectedValue, nameof(type));
			genericEnd += 2;
			if (generics.Length > skipGeneric)
			{
				generics = generics[skipGeneric..];
				var genericNames = generics.Select(static g => g.AssemblyQualifiedName).ToArray();
				postfixedName = fullName[..genericStart] + $"`{generics.Length}[[{string.Join("],[", genericNames)}]]" + fullName[genericEnd..];
			}
			else
			{
				postfixedName = fullName[..genericStart] + fullName[genericEnd..];
			}
		}
		else
		{
			postfixedName = fullName;
		}
		postfixedName += postfix;
		// return
		return Type.GetType(postfixedName) ?? throw new TypeAccessException();
	}
	#endregion
}
