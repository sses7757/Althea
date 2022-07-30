using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

using Althea.Resources;


namespace Althea.Numerics;

#region interface
/// <summary>
/// The base interface for complex numbers
/// </summary>
/// <typeparam name="TSelf">The actual type of implemented complex number struct/class</typeparam>
public interface IComplexNumber<TSelf> : IBaseNumber<TSelf> where TSelf : unmanaged, IComplexNumber<TSelf>
{
	/// <summary>
	/// Abstract static get imaginary one for <typeparamref name="TSelf"/>
	/// </summary>
	abstract static TSelf ImaginaryOne { get; }
}

/// <summary>
/// The base interface for complex numbers with real type
/// </summary>
/// <typeparam name="TSelf">The actual type of implemented complex number struct/class</typeparam>
/// <typeparam name="T">The type of corresponding real number</typeparam>
public interface IComplexNumber<TSelf, T> : IComplexNumber<TSelf> where TSelf : unmanaged, IComplexNumber<TSelf, T> where T : unmanaged, IBaseNumber<T>
{
	/// <summary>
	/// Get the real part of this complex number
	/// </summary>
	T Real { get; }
	/// <summary>
	/// Get the imaginary part of this complex number
	/// </summary>
	T Imaginary { get; }
	/// <summary>
	/// Get the square of the magnitude or absolute value of this complex number.
	/// </summary>
	T MagnitudeSquared { get; }

	/// <summary>
	/// Get the complex conjugate of this complex number
	/// </summary>
	new TSelf Conjugate { get; }

	/// <summary>
	/// Implicitly convert a real number of <typeparamref name="T"/> to complex <typeparamref name="TSelf"/>
	/// </summary>
	/// <param name="real">The input real number of type <typeparamref name="T"/></param>
	abstract static implicit operator TSelf(T real);

	/// <summary>
	/// Implicitly convert a pair of real number of <typeparamref name="T"/> to complex <typeparamref name="TSelf"/>
	/// </summary>
	/// <param name="v">The input real and imaginary parts of type <typeparamref name="T"/></param>
	abstract static implicit operator TSelf((T Real, T Imag) v);
}

/// <summary>
/// The base interface for complex float numbers
/// </summary>
/// <typeparam name="TSelf">The actual type of implemented complex number struct/class</typeparam>
public interface IComplexFloatNumber<TSelf> : IComplexNumber<TSelf>, IBinaryFloat<TSelf>
	where TSelf : unmanaged, IComplexFloatNumber<TSelf>
{
	/// <summary>
	/// Abstract static get negative imaginary one for <typeparamref name="TSelf"/>
	/// </summary>
	abstract static TSelf ImaginaryNegativeOne { get; }
}

/// <summary>
/// The base interface for complex float numbers with real type
/// </summary>
/// <typeparam name="TSelf">The actual type of implemented complex number struct/class</typeparam>
/// <typeparam name="T">The type of corresponding real number</typeparam>
public interface IComplexFloatNumber<TSelf, T> : IComplexNumber<TSelf, T>, IComplexFloatNumber<TSelf>
	where TSelf : unmanaged, IComplexFloatNumber<TSelf, T>
	where T : unmanaged, IBinaryFloat<T>
{
	/// <summary>
	/// Get the magnitude or absolute value of this complex number.
	/// </summary>
	T Magnitude { get; }

	/// <summary>
	/// Get the phase of this complex number of range [0, 2π).
	/// </summary>
	T Phase { get; }

	/// <summary>
	/// Statically return the complex power of the given <paramref name="complex"/> number and a real power <paramref name="p"/>
	/// </summary>
	/// <param name="complex">The complex number of type <typeparamref name="TSelf"/></param>
	/// <param name="p">The power as a real number of type <typeparamref name="T"/></param>
	/// <returns>The complex power of <paramref name="complex"/> to <paramref name="p"/></returns>
	abstract static TSelf Pow(TSelf complex, T p);
}

/// <summary>
/// The base interface for complex integer numbers
/// </summary>
/// <typeparam name="TSelf">The actual type of implemented complex number struct/class</typeparam>
/// <typeparam name="T">The type of corresponding real number</typeparam>
public interface IComplexIntegerNumber<TSelf, T> : IComplexNumber<TSelf, T>, IBinaryInt<TSelf>
	where TSelf : unmanaged, IComplexIntegerNumber<TSelf, T>
	where T : unmanaged, IBinaryInt<T>
{

}
#endregion


/// <summary>
/// The general complex type for any real floating point numeric number type including <see cref="float"/> and <see cref="double"/>
/// </summary>
/// <typeparam name="T">The data type of corresponding real number</typeparam>
[StructLayout(LayoutKind.Sequential)]
public readonly partial struct Complex<T> : IComplexFloatNumber<Complex<T>, T> where T : unmanaged, IBinaryFloat<T>
{
	#region basic
	private readonly T real, imag;

	/// <summary>
	/// Get the real part
	/// </summary>
	public T Real
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => this.real;
	}

	/// <summary>
	/// Get the imaginary part
	/// </summary>
	public T Imaginary
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => this.imag;
	}

	/// <summary>
	/// Construct a <see cref="Complex{T}"/> from real and imaginary parts
	/// </summary>
	/// <param name="re">The real part</param>
	/// <param name="im">The imaginary part, default value is 0</param>
	public Complex(T re, T im = default)
	{
		this.real = re;
		this.imag = im;
	}
	#endregion

	#region static information
	/// <inheritdoc/>
	public static DataType Type => DataTypeExtension.MakeDataType(true, DataTypeClassification.BinaryFloat_IEEE754, T.Size);
	/// <inheritdoc/>
	public static Complex<T> MachinePrecision => T.MachinePrecision;
	/// <inheritdoc/>
	public static bool IsComplexType => true;
	/// <inheritdoc/>
	public static unsafe int Size => sizeof(Complex<T>);

	static Complex()
	{
		// generic type check
		if (typeof(T).IsGenericType)
			throw new InvalidOperationException(ArithmeticError.DataTypeNotAllow);
	}
	#endregion

	#region constant values
	/// <inheritdoc/>
	public static Complex<T> Zero => new(T.Zero);
	/// <inheritdoc/>
	public static Complex<T> NegativeZero => new(T.NegativeZero);
	/// <inheritdoc/>
	public static Complex<T> One => new(T.One);
	/// <inheritdoc/>
	public static Complex<T> NegativeOne => new(T.NegativeOne);
	/// <inheritdoc/>
	public static Complex<T> ImaginaryOne => new(T.Zero, T.One);
	/// <inheritdoc/>
	public static Complex<T> ImaginaryNegativeOne => new(T.Zero, T.NegativeOne);

	static Complex<T> IAdditiveIdentity<Complex<T>, Complex<T>>.AdditiveIdentity => Zero;
	static Complex<T> IMultiplicativeIdentity<Complex<T>, Complex<T>>.MultiplicativeIdentity => One;

	/// <inheritdoc/>
	public static Complex<T> PositiveInfinity => new(T.PositiveInfinity);
	/// <inheritdoc/>
	public static Complex<T> NegativeInfinity => new(T.NegativeInfinity);
	/// <inheritdoc/>
	public static Complex<T> NaN => new(T.NaN);

	/// <inheritdoc/>
	public static Complex<T> Tau => new(T.Tau);
	/// <inheritdoc/>
	public static Complex<T> Pi => new(T.Pi);
	/// <inheritdoc/>
	public static Complex<T> Epsilon => new(T.Epsilon);
	/// <inheritdoc/>
	public static Complex<T> E => new(T.E);

	/// <inheritdoc/>
	public static bool IsFinite(Complex<T> value) => T.IsFinite(value.real) && T.IsFinite(value.imag);
	/// <inheritdoc/>
	public static bool IsNaN(Complex<T> value) => T.IsNaN(value.real) || T.IsNaN(value.imag);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsReal(Complex<T> value) => value.imag == T.Zero;
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsComplex(Complex<T> value) => value.imag != T.Zero;
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsImaginaryNumber(Complex<T> value) => value.real == T.Zero;
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsPositive(Complex<T> value) => value.imag == T.Zero && value.real > T.Zero;
	/// <inheritdoc/>
	public static bool IsNegative(Complex<T> value) => value.imag == T.Zero && value.real < T.Zero;
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsInteger(Complex<T> value) => value.imag == T.Zero && T.IsInteger(value.real);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsOddInteger(Complex<T> value) => value.imag == T.Zero && T.IsOddInteger(value.real);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsZero(Complex<T> value) => value == Zero;
	#endregion

	#region parser
	const string floatPattern =
		// (?:[any_thing]) for a non capturing group
		@"(?:" +
			// plus or minus and a X(.XX)? form number
			@"[-+]?\d+(?:\.\d+)?" +
			// -or-
			@"|" +
			// plus or minus and a X?.XX form number
			@"[-+]?\d*\.?\d+" +
		// group end
		@")" +
		// non capturing group for [e|E][+|-|empty]XXX scientific notation
		@"(?:[eE][\+\-]?\d+)?";
	const string floatPattern2 =
		// (?:[any_thing]) for a non capturing group
		@"(?:" +
			// a X(.XX)? form number
			@"\d+(?:\.\d+)?" +
			// -or-
			@"|" +
			// a X?.XX form number
			@"\d*\.?\d+" +
		// group end
		@")" +
		// non capturing group for [e|E][+|-|empty]XXX scientific notation
		@"(?:[eE][\+\-]?\d+)?";

	const string regexPattern1 =
		// Match any float, negative or positive, group it
		@"(" + floatPattern + @")" +
		// ... possibly following that with whitespace
		@"\s*" +
		// start imaginary part group
		@"(" +
			// ... followed by a plus or a minus
			@"[\+\-]" +
			// and possibly more whitespace:
			@"\s*" +
			// Match any other float
			@"(?:" + floatPattern2 + @")" +
			// ... followed by 'i' or 'I'
			@"\s?[iI]" +
		// end group
		")?";
	const string regexPattern2 =
		// imaginary part group
		@"(" +
			// Match any float, negative or positive
			floatPattern +
			// ... followed by 'i' or 'I'
			@"\s?[iI]" +
		// end group
		")" +
		// ... possibly following that with whitespace
		@"\s*" +
		// real part group
		@"(" + floatPattern2 + ")";

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static unsafe bool TryParseAny(delegate*<string, NumberStyles, IFormatProvider?, out T, bool> parseFunc, string str!!, NumberStyles style, IFormatProvider? provider, out T real, out T imag)
	{
		real = imag = default;

		Regex regex = MyRegex();
		Match match = regex.Match(str);
		bool success = match.Success;
		if (!success)
			goto SecondTry;
		success = parseFunc(match.Groups[1].Value, style, provider, out real);
		if (!success)
			goto SecondTry;
		string imagStr = match.Groups[2].Value.Replace(" ", "");
		if (imagStr.Length > 0)
		{
			imagStr = imagStr[..(imagStr.Length - 1)];
			success = parseFunc(imagStr, style, provider, out imag);
		}
		else
		{
			success = true;
		}
		if (!success)
			goto SecondTry;
		else
			return true;

		SecondTry:
		regex = MyRegex1();
		match = regex.Match(str);
		success = match.Success;
		if (!success)
			return false;
		success = parseFunc(match.Groups[1].Value, style, provider, out imag);
		if (!success)
			return false;
		success = parseFunc(match.Groups[2].Value, style, provider, out real);
		if (!success)
			return false;
		else
			return true;
	}

	/// <summary>
	/// Try to parse a <see cref="string"/> to a <see cref="Complex{T}"/>
	/// </summary>
	/// <param name="str">The string to parse of form "a + b<c>i</c>", "a - b<c>i</c>", "a", "b<c>i</c>" or "-b<c>i</c>" where both 'a' and 'b' are float point numbers</param>
	/// <param name="style">The <see cref="NumberStyles"/> of given <paramref name="str"/></param>
	/// <param name="provider">The <see cref="IFormatProvider"/> which can be null</param>
	/// <param name="complex">The output <see cref="Complex{T}"/></param>
	/// <returns>Success or not</returns>
	public unsafe static bool TryParse(string? str, NumberStyles style, IFormatProvider? provider, out Complex<T> complex)
	{
		complex = default;
		if (str is null || str.Length == 0)
			return false;
		bool success = TryParseAny(&T.TryParse, str, style, provider, out T real, out T imag);
		if (success)
			complex = new(real, imag);
		return success;
	}

	/// <summary>
	/// Parse a <see cref="string"/> to a <see cref="Complex{T}"/>
	/// </summary>
	/// <param name="str">The string to parse of form "a + b<c>i</c>", "a - b<c>i</c>", "a", "b<c>i</c>" or "-b<c>i</c>" where both 'a' and 'b' are float point numbers</param>
	/// <param name="style">The <see cref="NumberStyles"/> of given <paramref name="str"/></param>
	/// <param name="provider">The <see cref="IFormatProvider"/> which can be null</param>
	/// <returns>The parsed <see cref="Complex{T}"/></returns>
	public static Complex<T> Parse(string? str, NumberStyles style, IFormatProvider? provider)
	{
		if (str is null || str.Length == 0)
			throw new ArgumentNullException(nameof(str));
		bool success = TryParse(str, style, provider, out Complex<T> result);
		if (!success)
			throw new ArgumentException(string.Format(ArithmeticError.CannotParseComplex, str, typeof(T).Name), nameof(str));
		return result;
	}

	/// <inheritdoc/>
	public static Complex<T> Parse(string? s, IFormatProvider? provider) => Parse(s, NumberStyles.Any, provider);
	/// <inheritdoc/>
	public static bool TryParse(string? s, IFormatProvider? provider, out Complex<T> result) => TryParse(s, NumberStyles.Any, provider, out result);
	/// <inheritdoc/>
	public static Complex<T> Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider) => Parse(new string(s), style, provider);
	/// <inheritdoc/>
	public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out Complex<T> result) => TryParse(new string(s), style, provider, out result);
	/// <inheritdoc/>
	public static Complex<T> Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s, NumberStyles.Any, provider);
	/// <inheritdoc/>
	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Complex<T> result) => TryParse(s, NumberStyles.Any, provider, out result);
	#endregion

	#region converter
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator Complex<T>(T real) => new(real);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator Complex<T>((T Real, T Imag) val) => new(val.Real, val.Imag);
	#endregion

	#region equality
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator ==(Complex<T> a, Complex<T> b) => a.Equals(b);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator !=(Complex<T> a, Complex<T> b) => !(a == b);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Equals(Complex<T> other) => this.real == other.real && this.imag == other.imag;

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override int GetHashCode() => HashCode.Combine(this.real, this.imag);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override bool Equals(object? obj)
	{
		Complex<T> a;
		if (obj is T real)
			a = real;
		else if (obj is Complex<T> complex)
			a = complex;
		else
			return false;
		return this.Equals(a);
	}

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator <(Complex<T> left, Complex<T> right)
	{
		if (left.imag == T.Zero && right.imag == T.Zero)
			return left.real < right.real;
		throw new InvalidOperationException();
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator >(Complex<T> left, Complex<T> right)
	{
		if (left.imag == T.Zero && right.imag == T.Zero)
			return left.real > right.real;
		throw new InvalidOperationException();
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator <=(Complex<T> left, Complex<T> right)
	{
		if (left.imag == T.Zero && right.imag == T.Zero)
			return left.real <= right.real;
		throw new InvalidOperationException();
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator >=(Complex<T> left, Complex<T> right)
	{
		if (left.imag == T.Zero && right.imag == T.Zero)
			return left.real >= right.real;
		throw new InvalidOperationException();
	}
	/// <inheritdoc/>
	public sealed class ComplexRealPartComparer : IComparer<Complex<T>>
	{
		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int Compare(Complex<T> x, Complex<T> y) => x.real.CompareTo(y.real);
	}

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> Max(Complex<T> x, Complex<T> y) => x < y ? y : x;
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> Min(Complex<T> x, Complex<T> y) => x < y ? x : y;

	int IComparable.CompareTo(object? obj) => obj is Complex<T> c ? this.CompareTo(c) : throw new InvalidOperationException();

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int CompareTo(Complex<T> other)
	{
		if (this.imag == T.Zero && other.imag == T.Zero)
			return this.real.CompareTo(other.real);
		throw new InvalidOperationException();
	}
	#endregion

	#region arithmetic operators
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> operator -(Complex<T> a) => new(-a.real, -a.imag);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> operator +(Complex<T> a, Complex<T> b) => new(a.real + b.real, a.imag + b.imag);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> operator -(Complex<T> a, Complex<T> b) => new(a.real - b.real, a.imag - b.imag);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> operator +(Complex<T> a, T b) => new(a.real - b, a.imag);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> operator +(T b, Complex<T> a) => new(a.real - b, a.imag);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> operator -(Complex<T> a, T b) => new(a.real - b, a.imag);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> operator -(T b, Complex<T> a) => new(b - a.real, -a.imag);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> operator *(Complex<T> x, Complex<T> y)
	{
		T real = x.real * y.real - x.imag * y.imag;
		T imag = x.real * y.imag + x.imag * y.real;
		return new Complex<T>(real, imag);
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> operator /(Complex<T> x, Complex<T> y)
	{
		T squareAbsY = y.MagnitudeSquared;
		T acbd = x.real * y.real + x.imag * y.imag;
		T bcad = x.imag * y.real - x.real * y.imag;
		return new(acbd / squareAbsY, bcad / squareAbsY);
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> operator *(Complex<T> a, T b) => new(a.real * b, a.imag * b);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> operator *(T b, Complex<T> a) => new(a.real * b, a.imag * b);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> operator /(Complex<T> a, T b) => new(a.real / b, a.imag / b);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> operator /(T a, Complex<T> b)
	{
		T squareAbsY = b.MagnitudeSquared;
		return new(a * b.real / squareAbsY, -a * b.imag / squareAbsY);
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> operator +(Complex<T> value) => value;
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> operator --(Complex<T> value)
	{
		T r = value.real;
		r--;
		return new(r, value.imag);
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> operator ++(Complex<T> value)
	{
		T r = value.real;
		r++;
		return new(r, value.imag);
	}

	/*
	static Complex<T> IAdditionOperators<Complex<T>, Complex<T>, Complex<T>>.op_CheckedAddition(Complex<T> left, Complex<T> right) => new(checked(left.real + right.real), checked(left.imag + right.imag));
	static Complex<T> ISubtractionOperators<Complex<T>, Complex<T>, Complex<T>>.op_CheckedSubtraction(Complex<T> left, Complex<T> right) => new(checked(left.real - right.real), checked(left.imag- right.imag));
	static Complex<T> IMultiplyOperators<Complex<T>, Complex<T>, Complex<T>>.op_CheckedMultiply(Complex<T> x, Complex<T> y)
	{
		T real = checked(x.real * y.real - x.imag * y.imag);
		T imag = checked(x.real * y.imag + x.imag * y.real);
		return new Complex<T>(real, imag);
	}
	static Complex<T> IDivisionOperators<Complex<T>, Complex<T>, Complex<T>>.op_CheckedDivision(Complex<T> x, Complex<T> y)
	{
		T squareAbsY = y.MagnitudeSquaredChecked;
		T acbd = checked(x.real * y.real + x.imag * y.imag);
		T bcad = checked(x.imag * y.real - x.real * y.imag);
		return new(checked(acbd / squareAbsY), checked(bcad / squareAbsY));
	}
	static Complex<T> IUnaryNegationOperators<Complex<T>, Complex<T>>.op_CheckedUnaryNegation(Complex<T> value) => -value;
	static Complex<T> IIncrementOperators<Complex<T>>.op_CheckedIncrement(Complex<T> value)
	{
		T r = value.real;
		r = checked(r + T.One);
		return new(r, value.imag);
	}
	static Complex<T> IDecrementOperators<Complex<T>>.op_CheckedDecrement(Complex<T> value)
	{
		T r = value.real;
		r = checked(r - T.One);
		return new(r, value.imag);
	}
	*/
	#endregion

	#region other arithmetics
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> ReciprocalEstimate(Complex<T> x)
	{
		T squareAbs = T.ReciprocalEstimate(x.MagnitudeSquared);
		return new(x.real * squareAbs, -x.imag * squareAbs);
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> ReciprocalSqrtEstimate(Complex<T> x)
	{
		T arg = RealHalf * T.Atan2(-x.imag, x.real);
		T scale = T.ReciprocalSqrtEstimate(x.Magnitude);
		var (imag, real) = T.SinCos(arg);
		return new(scale * real, scale * imag);
	}
	/// <inheritdoc/>
	public T Magnitude
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => T.Sqrt(this.MagnitudeSquared);
	}
	/// <inheritdoc/>
	public T MagnitudeSquared
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => this.real * this.real + this.imag * this.imag;
	}
	private T MagnitudeSquaredChecked
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => checked(this.real * this.real + this.imag * this.imag);
	}
	/// <inheritdoc/>
	public T Phase
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => T.Atan2(this.imag, this.real);
	}
	/// <inheritdoc/>
	public Complex<T> Conjugate
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(this.real, -this.imag);
	}

	static Complex<T> IBaseNumber<Complex<T>>.Conjugate(Complex<T> value) => value.Conjugate;

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> Abs(Complex<T> number) => number.Magnitude;
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> Exp(Complex<T> x)
	{
		T scale = T.Exp(x.real);
		var (imag, real) = T.SinCos(x.imag);
		return new(scale * real, scale * imag);
	}

	private static readonly T RealTwo = T.One + T.One;
	private static readonly T RealFour = RealTwo + RealTwo;
	private static readonly T RealHalf = T.One / RealTwo;
	private static readonly T RealQuarter = T.One / RealFour;
	private static readonly T RealOneThird = T.One / (T.One + RealTwo);
	private static readonly T RealLog2 = T.Log(RealTwo);
	private static readonly T RealLog10 = T.Log(RealTwo + RealFour + RealFour);
	private static readonly T RealOneOverLog2 = T.Log2(T.E);
	private static readonly T RealOneOverLog10 = T.Log10(T.E);
	private static readonly T RealHalfPi = T.Pi / RealTwo;

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> Hypot(Complex<T> x, Complex<T> y) => Sqrt(x * x + y * y);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> Root(Complex<T> x, int n) => n == 1 ? x : n == 2 ? Sqrt(x) : n == 3 ? Cbrt(x) : PowReal(x, T.One / n.As<T>());
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> Log(Complex<T> x)
	{
		T real = RealHalf * T.Log(x.MagnitudeSquared);
		T imag = x.Phase;
		return new(real, imag);
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> Log(Complex<T> x, Complex<T> b)
	{
		T squareAbsB = b.MagnitudeSquared;
		T realA = x.Phase * RealTwo, realB = b.Phase * RealTwo;
		T imagA = T.Log(x.MagnitudeSquared), imagB = T.Log(squareAbsB);
		T acbd = realA * realB + imagA * imagB;
		T bcad = realA * imagB - imagA * realB;
		return new(acbd / squareAbsB, bcad / squareAbsB);
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> Log2(Complex<T> x) => Log(x) * RealOneOverLog2;
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> Log10(Complex<T> x) => Log(x) * RealOneOverLog10;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Complex<T> PowReal(Complex<T> c, T p)
	{
		if (c.imag == T.Zero)
		{
			return new Complex<T>(T.Pow(c.real, p));
		}
		else
		{
			T absC = c.Magnitude;
			T argC = c.Phase;
			T phase = p * argC;
			T scale = T.Pow(absC, p);
			var (imag, real) = T.SinCos(phase);
			return new(scale * real, scale * imag);
		}
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> Pow(Complex<T> c, T p)
	{
		if ((c.real == T.Zero || c.real == T.One) && c.imag == T.Zero)
			return c;
		return PowReal(c, p);
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> Pow(Complex<T> c, Complex<T> p)
	{
		if ((c.real == T.Zero || c.real == T.One) && c.imag == T.Zero)
			return c;
		if (p.imag == T.Zero)
			return PowReal(c, p.real);
		T absC = c.Magnitude;
		T argC = c.Phase;
		T phase = p.real * argC + p.imag * T.Log(absC);
		T scale = T.Pow(absC, p.real) * T.Exp(-p.imag * argC);
		var (imag, real) = T.SinCos(phase);
		return new(scale * real, scale * imag);
	}

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> Sqrt(Complex<T> c)
	{
		T arg = RealHalf * c.Phase;
		T scale = T.Sqrt(c.Magnitude);
		var (imag, real) = T.SinCos(arg);
		return new(scale * real, scale * imag);
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> Cbrt(Complex<T> x)
	{
		T arg = RealOneThird * x.Phase;
		T scale = T.Cbrt(x.Magnitude);
		var (imag, real) = T.SinCos(arg);
		return new(scale * real, scale * imag);
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> FusedMultiplyAdd(Complex<T> a, Complex<T> b, Complex<T> c)
	{
		// a.r * b.r - (a.i * b.i - c.r)
		T temp1 = T.FusedMultiplyAdd(a.imag, b.imag, -c.real);
		T real = T.FusedMultiplyAdd(a.real, b.real, -temp1);
		// a.r * b.i + (a.i * b.r + c.i)
		T temp2 = T.FusedMultiplyAdd(a.imag, b.real, c.real);
		T imag = T.FusedMultiplyAdd(a.real, b.imag, temp2);
		// totally 4 FMA (naive is 2*FMA + 2*MUL + 2*ADD)
		return new(real, imag);
	}

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> Acos(Complex<T> x)
	{
		Complex<T> w = Sqrt(new(T.One + x.real, -x.imag));
		Complex<T> z = Sqrt(new(T.One - x.real, -x.imag));
		T real = RealTwo * T.Atan2(z.real, w.real);
		T imag = w.real * z.imag + w.imag * z.real;
		imag = T.Asinh(imag);
		return new(real, imag);
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> Acosh(Complex<T> x)
	{
		Complex<T> w = Sqrt(new(x.real + T.One, -x.imag));
		Complex<T> z = Sqrt(new(x.real - T.One, x.imag));
		T real = w.real * z.real - w.imag * z.imag;
		real = T.Asinh(real);
		T imag = RealTwo * T.Atan2(Sqrt(w.Conjugate).imag, z.real);
		return new(real, imag);
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> Asin(Complex<T> x)
	{
		Complex<T> asinh = Asinh(new(-x.imag, x.real));
		return new(asinh.imag, -asinh.real);
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> Asinh(Complex<T> x)
	{
		Complex<T> w = Sqrt(new(T.One - x.imag, x.real));
		Complex<T> z = Sqrt(new(T.One + x.imag, -x.real));
		T real = w.imag * z.real - w.real * z.imag;
		real = T.Asinh(real);
		T wzReal = x.real * x.real - x.imag * x.imag + T.One;
		T imag = T.Atan2(x.imag, wzReal);
		return new(real, imag);
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> Atan(Complex<T> x)
	{
		Complex<T> atanh = Atanh(new(-x.imag, x.real));
		return new(atanh.imag, -atanh.real);
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> Atan2(Complex<T> y, Complex<T> x) => Atan(y / x);

	private static T Log1p(T val)
	{
		if (val < T.Zero)
			return T.NaN;
		if (val == T.Zero)
			return T.Zero;
		else
		{ // compute log(1 + val) with fix-up for small val
			T p1 = T.One + val;
			return T.Log(p1) - (p1 - T.One - val) / p1;
		}
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> Atanh(Complex<T> x)
	{
		T absRe = T.Abs(x.real), absIm = T.Abs(x.imag);
		T real, imag;
		if (absRe != T.One)
		{
			T reFrom1 = T.One - absRe;
			T imSquared = absIm * absIm;
			real = RealQuarter * Log1p(RealFour * absRe / (reFrom1 * reFrom1 + imSquared));
			imag = RealHalf * T.Atan2(RealTwo * x.imag, (reFrom1 * (absRe + T.One) - imSquared));
		}
		else if (x.imag == T.Zero)
		{ // (±1, 0)
			real = T.PositiveInfinity;
			imag = T.PositiveInfinity;
		}
		else
		{ // (±1, nonzero)
			real = T.Log(T.Sqrt(T.Sqrt(x.imag * x.imag + RealFour)) / T.Sqrt(absIm));
			imag = T.CopySign(RealHalf * (RealHalfPi + T.Atan2(absIm, RealTwo)), x.imag);
		}
		real = T.CopySign(real, x.real);
		return new(real, imag);
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> Cos(Complex<T> x)
	{
		var (sin, cos) = T.SinCos(x.real);
		T real = T.Cosh(x.imag) * cos;
		T imag = -T.Sinh(x.imag) * sin;
		return new(real, imag);
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> Cosh(Complex<T> x)
	{
		var (sin, cos) = T.SinCos(x.imag);
		T real = T.Cosh(x.real) * cos;
		T imag = T.Sinh(x.real) * sin;
		return new(real, imag);
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> Sin(Complex<T> x)
	{
		var (sin, cos) = T.SinCos(x.real);
		T real = T.Cosh(x.imag) * sin;
		T imag = T.Sinh(x.imag) * cos;
		return new(real, imag);
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> Sinh(Complex<T> x)
	{
		var (sin, cos) = T.SinCos(x.imag);
		T real = T.Sinh(x.real) * cos;
		T imag = T.Cosh(x.real) * sin;
		return new(real, imag);
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> Tan(Complex<T> x)
	{
		throw new NotImplementedException();
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> Tanh(Complex<T> x)
	{
		T imag = T.Tan(x.imag), s = T.Sinh(x.real);
		T b = s * (imag * imag + T.One);
		T scale = T.One / (b * s + T.One);
		T real = T.Sqrt(s * s + T.One) * b;
		return new(real * scale, imag * scale);
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> Log10P1(Complex<T> x) => Log10(x + T.One);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> Log2P1(Complex<T> x) => Log2(x + T.One);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> LogP1(Complex<T> x) => Log(x + T.One);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static (Complex<T> Sin, Complex<T> Cos) SinCos(Complex<T> x)
	{
		var (sinr, cosr) = T.SinCos(x.real);
		T coshi = T.Cosh(x.imag), sinhi = T.Sinh(x.imag);
		T real = coshi * sinr;
		T imag = sinhi * cosr;
		Complex<T> sin = new(real, imag);
		real = coshi * cosr;
		imag = -sinhi * sinr;
		Complex<T> cos = new(real, imag);
		return (sin, cos);
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> Exp10(Complex<T> x)
	{
		T scale = T.Exp10(x.real);
		var (imag, real) = T.SinCos(x.imag * RealLog10);
		return new(scale * real, scale * imag);
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> Exp10M1(Complex<T> x) => Exp10(x) - T.One;
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> Exp2(Complex<T> x)
	{
		T scale = T.Exp2(x.real);
		var (imag, real) = T.SinCos(x.imag * RealLog2);
		return new(scale * real, scale * imag);
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> Exp2M1(Complex<T> x) => Exp2(x) - T.One;
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> ExpM1(Complex<T> x) => Exp(x) - T.One;

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> CopySign(Complex<T> x, Complex<T> y)
	{
		return new(T.CopySign(x.real, y.real), T.CopySign(x.imag, y.imag));
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> Ceiling(Complex<T> x)
	{
		return new(T.Ceiling(x.real), T.Ceiling(x.imag));
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> Floor(Complex<T> x)
	{
		return new(T.Floor(x.real), T.Floor(x.imag));
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> Round(Complex<T> x)
	{
		return new(T.Round(x.real), T.Round(x.imag));
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> Round(Complex<T> x, int digits)
	{
		return new(T.Round(x.real, digits), T.Round(x.imag, digits));
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> Round(Complex<T> x, MidpointRounding mode)
	{
		return new(T.Round(x.real, mode), T.Round(x.imag, mode));
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> Round(Complex<T> x, int digits, MidpointRounding mode)
	{
		return new(T.Round(x.real, digits, mode), T.Round(x.imag, digits, mode));
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> Truncate(Complex<T> x)
	{
		return new(T.Truncate(x.real), T.Truncate(x.imag));
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> Sign(Complex<T> x)
	{
		return new(T.Sign(x.real), T.Sign(x.imag));
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> MaxMagnitudeNumber(Complex<T> x, Complex<T> y)
	{
		return x.MagnitudeSquared >= y.MagnitudeSquared ? x : y;
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex<T> MinMagnitudeNumber(Complex<T> x, Complex<T> y)
	{
		return x.MagnitudeSquared <= y.MagnitudeSquared ? x : y;
	}
	#endregion

	#region string representation
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override string ToString()
	{
		return this.ToString(null, Print.Culture);
	}
	/// <summary>
	/// String representation of this complex number
	/// </summary>
	/// <param name="format">format of output</param>
	public string ToString([StringSyntax("NumericFormat")] string? format)
	{
		return this.ToString(format, Print.Culture);
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string ToString([StringSyntax("NumericFormat")] string? format, IFormatProvider? provider = null)
	{
		Span<char> chars = stackalloc char[100];
		if (!TryFormat(chars, out int charsWritten, format.AsSpan(), provider))
			return string.Empty;
		return new(chars[..charsWritten]);
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
	{
		destination[0] = '('; charsWritten = 1;
		if (!this.real.TryFormat(destination[1..], out int cw1, format, provider))
			return false;
		charsWritten += cw1;
		if (charsWritten + 1 >= destination.Length)
			return false;
		destination[charsWritten] = ','; charsWritten++;
		if (!this.real.TryFormat(destination[charsWritten..], out int cw2, format, provider))
			return false;
		charsWritten += cw2;
		if (charsWritten + 1 >= destination.Length)
			return false;
		destination[charsWritten] = ')'; charsWritten++;
		return true;
	}

	[RegexGenerator("((?:[-+]?\\d+(?:\\.\\d+)?|[-+]?\\d*\\.?\\d+)(?:[eE][\\+\\-]?\\d+)?)\\s*([\\+\\-]\\s*(?:(?:\\d+(?:\\.\\d+)?|\\d*\\.?\\d+)(?:[eE][\\+\\-]?\\d+)?)\\s?[iI])?")]
	private static partial Regex MyRegex();
	[RegexGenerator("((?:[-+]?\\d+(?:\\.\\d+)?|[-+]?\\d*\\.?\\d+)(?:[eE][\\+\\-]?\\d+)?\\s?[iI])\\s*((?:\\d+(?:\\.\\d+)?|\\d*\\.?\\d+)(?:[eE][\\+\\-]?\\d+)?)")]
	private static partial Regex MyRegex1();
	#endregion
}

/// <summary>
/// The general complex type for any real integral numeric number type including <see cref="float"/> and <see cref="double"/>
/// </summary>
/// <typeparam name="T">The data type of corresponding real number</typeparam>
[StructLayout(LayoutKind.Sequential)]
public readonly partial struct ComplexInteger<T> : IComplexIntegerNumber<ComplexInteger<T>, T> where T : unmanaged, IBinaryInt<T>
{
	#region basic
	private readonly T real, imag;

	/// <summary>
	/// Get the real part
	/// </summary>
	public T Real
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => this.real;
	}

	/// <summary>
	/// Get the imaginary part
	/// </summary>
	public T Imaginary
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => this.imag;
	}

	/// <summary>
	/// Construct a <see cref="ComplexInteger{T}"/> from real and imaginary parts
	/// </summary>
	/// <param name="re">The real part</param>
	/// <param name="im">The imaginary part, default value is 0</param>
	public ComplexInteger(T re, T im = default)
	{
		this.real = re;
		this.imag = im;
	}
	#endregion

	#region static information
	/// <inheritdoc/>
	public static DataType Type => T.Type.ComplexCorrespond();
	/// <inheritdoc/>
	public static ComplexInteger<T> MachinePrecision => T.MachinePrecision;
	/// <inheritdoc/>
	public static bool IsComplexType => true;
	/// <inheritdoc/>
	public static unsafe int Size => sizeof(ComplexInteger<T>);

	static ComplexInteger()
	{
		// generic type check
		if (typeof(T).IsGenericType)
			throw new InvalidOperationException(ArithmeticError.DataTypeNotAllow);
	}
	#endregion

	#region constant values
	/// <inheritdoc/>
	public static ComplexInteger<T> Zero => new(T.Zero);
	/// <inheritdoc/>
	public static ComplexInteger<T> One => new(T.One);
	/// <inheritdoc/>
	public static ComplexInteger<T> ImaginaryOne => new(T.Zero, T.One);

	static ComplexInteger<T> IAdditiveIdentity<ComplexInteger<T>, ComplexInteger<T>>.AdditiveIdentity => Zero;
	static ComplexInteger<T> IMultiplicativeIdentity<ComplexInteger<T>, ComplexInteger<T>>.MultiplicativeIdentity => One;

	/// <inheritdoc/>
	public static bool IsFinite(ComplexInteger<T> value) => true;
	/// <inheritdoc/>
	public static bool IsNaN(ComplexInteger<T> value) => false;
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsReal(ComplexInteger<T> value) => value.imag == T.Zero;
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsComplex(ComplexInteger<T> value) => value.imag != T.Zero;
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsImaginaryNumber(ComplexInteger<T> value) => value.real == T.Zero;
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsPositive(ComplexInteger<T> value) => value.imag == T.Zero && value.real > T.Zero;
	/// <inheritdoc/>
	public static bool IsNegative(ComplexInteger<T> value) => value.imag == T.Zero && value.real < T.Zero;
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsInteger(ComplexInteger<T> value) => value.imag == T.Zero && T.IsInteger(value.real);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsOddInteger(ComplexInteger<T> value) => value.imag == T.Zero && T.IsOddInteger(value.real);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsZero(ComplexInteger<T> value) => value == Zero;
	#endregion

	#region parser
	/// <summary>
	/// Try to parse a <see cref="string"/> to a <see cref="ComplexInteger{T}"/>
	/// </summary>
	/// <param name="str">The string to parse of form "a + b<c>i</c>", "a - b<c>i</c>", "a", "b<c>i</c>" or "-b<c>i</c>" where both 'a' and 'b' are float point numbers</param>
	/// <param name="style">The <see cref="NumberStyles"/> of given <paramref name="str"/></param>
	/// <param name="provider">The <see cref="IFormatProvider"/> which can be null</param>
	/// <param name="complex">The output <see cref="ComplexInteger{T}"/></param>
	/// <returns>Success or not</returns>
	public static bool TryParse(string? str, NumberStyles style, IFormatProvider? provider, out ComplexInteger<T> complex)
	{
		complex = default;
		if (!Complex<Float64>.TryParse(str, style, provider, out var c))
			return false;
		if (Complex<Float64>.Round(c) != c)
			return false;
		return NumberConvert.TryConvert(c, out complex);
	}

	/// <summary>
	/// Parse a <see cref="string"/> to a <see cref="ComplexInteger{T}"/>
	/// </summary>
	/// <param name="str">The string to parse of form "a + b<c>i</c>", "a - b<c>i</c>", "a", "b<c>i</c>" or "-b<c>i</c>" where both 'a' and 'b' are float point numbers</param>
	/// <param name="style">The <see cref="NumberStyles"/> of given <paramref name="str"/></param>
	/// <param name="provider">The <see cref="IFormatProvider"/> which can be null</param>
	/// <returns>The parsed <see cref="ComplexInteger{T}"/></returns>
	public static ComplexInteger<T> Parse(string? str, NumberStyles style, IFormatProvider? provider)
	{
		if (str is null || str.Length == 0)
			throw new ArgumentNullException(nameof(str));
		bool success = TryParse(str, style, provider, out ComplexInteger<T> result);
		if (!success)
			throw new ArgumentException(string.Format(ArithmeticError.CannotParseComplex, str, typeof(T).Name), nameof(str));
		return result;
	}

	/// <inheritdoc/>
	public static ComplexInteger<T> Parse(string? s, IFormatProvider? provider) => Parse(s, NumberStyles.Any, provider);
	/// <inheritdoc/>
	public static bool TryParse(string? s, IFormatProvider? provider, out ComplexInteger<T> result) => TryParse(s, NumberStyles.Any, provider, out result);
	/// <inheritdoc/>
	public static ComplexInteger<T> Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider) => Parse(new string(s), style, provider);
	/// <inheritdoc/>
	public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out ComplexInteger<T> result) => TryParse(new string(s), style, provider, out result);
	/// <inheritdoc/>
	public static ComplexInteger<T> Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s, NumberStyles.Any, provider);
	/// <inheritdoc/>
	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out ComplexInteger<T> result) => TryParse(s, NumberStyles.Any, provider, out result);
	#endregion

	#region converter
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator ComplexInteger<T>(T real) => new(real);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator ComplexInteger<T>((T Real, T Imag) val) => new(val.Real, val.Imag);
	#endregion

	#region equality
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator ==(ComplexInteger<T> a, ComplexInteger<T> b) => a.Equals(b);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator !=(ComplexInteger<T> a, ComplexInteger<T> b) => !(a == b);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Equals(ComplexInteger<T> other) => this.real == other.real && this.imag == other.imag;

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override int GetHashCode() => HashCode.Combine(this.real, this.imag);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override bool Equals(object? obj)
	{
		ComplexInteger<T> a;
		if (obj is T real)
			a = real;
		else if (obj is ComplexInteger<T> complex)
			a = complex;
		else
			return false;
		return this.Equals(a);
	}

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator <(ComplexInteger<T> left, ComplexInteger<T> right)
	{
		if (left.imag == T.Zero && right.imag == T.Zero)
			return left.real < right.real;
		throw new InvalidOperationException();
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator >(ComplexInteger<T> left, ComplexInteger<T> right)
	{
		if (left.imag == T.Zero && right.imag == T.Zero)
			return left.real > right.real;
		throw new InvalidOperationException();
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator <=(ComplexInteger<T> left, ComplexInteger<T> right)
	{
		if (left.imag == T.Zero && right.imag == T.Zero)
			return left.real <= right.real;
		throw new InvalidOperationException();
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator >=(ComplexInteger<T> left, ComplexInteger<T> right)
	{
		if (left.imag == T.Zero && right.imag == T.Zero)
			return left.real >= right.real;
		throw new InvalidOperationException();
	}
	/// <inheritdoc/>
	public sealed class ComplexRealPartComparer : IComparer<ComplexInteger<T>>
	{
		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int Compare(ComplexInteger<T> x, ComplexInteger<T> y) => x.real.CompareTo(y.real);
	}

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ComplexInteger<T> Max(ComplexInteger<T> x, ComplexInteger<T> y) => x < y ? y : x;
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ComplexInteger<T> Min(ComplexInteger<T> x, ComplexInteger<T> y) => x < y ? x : y;

	int IComparable.CompareTo(object? obj) => obj is ComplexInteger<T> c ? this.CompareTo(c) : throw new InvalidOperationException();

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int CompareTo(ComplexInteger<T> other)
	{
		if (this.imag == T.Zero && other.imag == T.Zero)
			return this.real.CompareTo(other.real);
		throw new InvalidOperationException();
	}
	#endregion

	#region arithmetic operators
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ComplexInteger<T> operator -(ComplexInteger<T> a) => new(-a.real, -a.imag);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ComplexInteger<T> operator +(ComplexInteger<T> a, ComplexInteger<T> b) => new(a.real + b.real, a.imag + b.imag);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ComplexInteger<T> operator -(ComplexInteger<T> a, ComplexInteger<T> b) => new(a.real - b.real, a.imag - b.imag);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ComplexInteger<T> operator +(ComplexInteger<T> a, T b) => new(a.real - b, a.imag);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ComplexInteger<T> operator +(T b, ComplexInteger<T> a) => new(a.real - b, a.imag);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ComplexInteger<T> operator -(ComplexInteger<T> a, T b) => new(a.real - b, a.imag);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ComplexInteger<T> operator -(T b, ComplexInteger<T> a) => new(b - a.real, -a.imag);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ComplexInteger<T> operator *(ComplexInteger<T> x, ComplexInteger<T> y)
	{
		T real = x.real * y.real - x.imag * y.imag;
		T imag = x.real * y.imag + x.imag * y.real;
		return new ComplexInteger<T>(real, imag);
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ComplexInteger<T> operator /(ComplexInteger<T> x, ComplexInteger<T> y)
	{
		T squareAbsY = y.MagnitudeSquared;
		T acbd = x.real * y.real + x.imag * y.imag;
		T bcad = x.imag * y.real - x.real * y.imag;
		return new(acbd / squareAbsY, bcad / squareAbsY);
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ComplexInteger<T> operator *(ComplexInteger<T> a, T b) => new(a.real * b, a.imag * b);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ComplexInteger<T> operator *(T b, ComplexInteger<T> a) => new(a.real * b, a.imag * b);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ComplexInteger<T> operator /(ComplexInteger<T> a, T b) => new(a.real / b, a.imag / b);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ComplexInteger<T> operator /(T a, ComplexInteger<T> b)
	{
		T squareAbsY = b.MagnitudeSquared;
		return new(a * b.real / squareAbsY, -a * b.imag / squareAbsY);
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ComplexInteger<T> operator +(ComplexInteger<T> value) => value;
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ComplexInteger<T> operator --(ComplexInteger<T> value)
	{
		T r = value.real;
		r--;
		return new(r, value.imag);
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ComplexInteger<T> operator ++(ComplexInteger<T> value)
	{
		T r = value.real;
		r++;
		return new(r, value.imag);
	}

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ComplexInteger<T> operator &(ComplexInteger<T> left, ComplexInteger<T> right) => new(left.real & right.real, left.imag & right.imag);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ComplexInteger<T> operator |(ComplexInteger<T> left, ComplexInteger<T> right) => new(left.real | right.real, left.imag | right.imag);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ComplexInteger<T> operator ^(ComplexInteger<T> left, ComplexInteger<T> right) => new(left.real ^ right.real, left.imag ^ right.imag);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ComplexInteger<T> operator ~(ComplexInteger<T> value) => new(~value.real, ~value.imag);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ComplexInteger<T> operator %(ComplexInteger<T> left, ComplexInteger<T> right) => new(left.real % right.real, left.imag % right.imag);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ComplexInteger<T> operator <<(ComplexInteger<T> value, int shiftAmount) => new(value.real << shiftAmount, value.imag << shiftAmount);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ComplexInteger<T> operator >>(ComplexInteger<T> value, int shiftAmount) => new(value.real >> shiftAmount, value.imag >> shiftAmount);

	/*
	static ComplexInteger<T> IAdditionOperators<ComplexInteger<T>, ComplexInteger<T>, ComplexInteger<T>>.op_CheckedAddition(ComplexInteger<T> left, ComplexInteger<T> right) => new(checked(left.real + right.real), checked(left.imag + right.imag));
	static ComplexInteger<T> ISubtractionOperators<ComplexInteger<T>, ComplexInteger<T>, ComplexInteger<T>>.op_CheckedSubtraction(ComplexInteger<T> left, ComplexInteger<T> right) => new(checked(left.real - right.real), checked(left.imag - right.imag));
	static ComplexInteger<T> IMultiplyOperators<ComplexInteger<T>, ComplexInteger<T>, ComplexInteger<T>>.op_CheckedMultiply(ComplexInteger<T> x, ComplexInteger<T> y)
	{
		T real = checked(x.real * y.real - x.imag * y.imag);
		T imag = checked(x.real * y.imag + x.imag * y.real);
		return new ComplexInteger<T>(real, imag);
	}
	static ComplexInteger<T> IDivisionOperators<ComplexInteger<T>, ComplexInteger<T>, ComplexInteger<T>>.op_CheckedDivision(ComplexInteger<T> x, ComplexInteger<T> y)
	{
		T squareAbsY = y.MagnitudeSquaredChecked;
		T acbd = checked(x.real * y.real + x.imag * y.imag);
		T bcad = checked(x.imag * y.real - x.real * y.imag);
		return new(checked(acbd / squareAbsY), checked(bcad / squareAbsY));
	}
	static ComplexInteger<T> IUnaryNegationOperators<ComplexInteger<T>, ComplexInteger<T>>.op_CheckedUnaryNegation(ComplexInteger<T> value) => -value;
	static ComplexInteger<T> IIncrementOperators<ComplexInteger<T>>.op_CheckedIncrement(ComplexInteger<T> value)
	{
		T r = value.real;
		r = T.op_CheckedIncrement(r);
		return new(r, value.imag);
	}
	static ComplexInteger<T> IDecrementOperators<ComplexInteger<T>>.op_CheckedDecrement(ComplexInteger<T> value)
	{
		T r = value.real;
		r = T.op_CheckedDecrement(r);
		return new(r, value.imag);
	}
	*/
	static ComplexInteger<T> IShiftOperators<ComplexInteger<T>, ComplexInteger<T>>.op_UnsignedRightShift(ComplexInteger<T> value, int shiftAmount) => new(T.op_UnsignedRightShift(value.real, shiftAmount), T.op_UnsignedRightShift(value.imag, shiftAmount));
	#endregion

	#region other arithmetics
	/// <inheritdoc/>
	public T MagnitudeSquared
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => this.real * this.real + this.imag * this.imag;
	}
	private T MagnitudeSquaredChecked
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => checked(this.real * this.real + this.imag * this.imag);
	}
	private T Magnitude
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			NumberConvert.TryConvert(this, out Complex<Float64> c);
			return c.Magnitude.As<Float64, T>();
		}
	}
	/// <inheritdoc/>
	public ComplexInteger<T> Conjugate
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(this.real, -this.imag);
	}
	static ComplexInteger<T> IBaseNumber<ComplexInteger<T>>.Conjugate(ComplexInteger<T> value) => value.Conjugate;

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ComplexInteger<T> Abs(ComplexInteger<T> number) => number.Magnitude;
	
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ComplexInteger<T> CopySign(ComplexInteger<T> x, ComplexInteger<T> y)
	{
		return new(T.CopySign(x.real, y.real), T.CopySign(x.imag, y.imag));
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ComplexInteger<T> Sign(ComplexInteger<T> x)
	{
		return new(T.Sign(x.real), T.Sign(x.imag));
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ComplexInteger<T> MaxMagnitudeNumber(ComplexInteger<T> x, ComplexInteger<T> y)
	{
		return x.MagnitudeSquared >= y.MagnitudeSquared ? x : y;
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ComplexInteger<T> MinMagnitudeNumber(ComplexInteger<T> x, ComplexInteger<T> y)
	{
		return x.MagnitudeSquared <= y.MagnitudeSquared ? x : y;
	}

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsPow2(ComplexInteger<T> value) => T.IsPow2(value.real) && T.IsPow2(value.imag);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ComplexInteger<T> Log2(ComplexInteger<T> value) => new(T.Log2(value.real), T.Log2(value.imag));
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static (ComplexInteger<T> Quotient, ComplexInteger<T> Remainder) DivRem(ComplexInteger<T> left, ComplexInteger<T> right)
	{
		var (divR, remR) = T.DivRem(left.real, right.real);
		var (divI, remI) = T.DivRem(left.imag, right.imag);
		return (new(divR, divI), new(remR, remI));
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ComplexInteger<T> RotateLeft(ComplexInteger<T> value, int rotateAmount) => new(T.RotateLeft(value.real, rotateAmount), T.RotateLeft(value.imag, rotateAmount));
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ComplexInteger<T> RotateRight(ComplexInteger<T> value, int rotateAmount) => new(T.RotateRight(value.real, rotateAmount), T.RotateRight(value.imag, rotateAmount));
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ComplexInteger<T> PopCount(ComplexInteger<T> value) => new(T.PopCount(value.real), T.PopCount(value.imag));
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ComplexInteger<T> LeadingZeroCount(ComplexInteger<T> value) => new(T.LeadingZeroCount(value.real), T.LeadingZeroCount(value.imag));
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ComplexInteger<T> TrailingZeroCount(ComplexInteger<T> value) => new(T.TrailingZeroCount(value.real), T.TrailingZeroCount(value.imag));
	#endregion

	#region string representation
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override string ToString()
	{
		return this.ToString(null, Print.Culture);
	}
	/// <summary>
	/// String representation of this complex number
	/// </summary>
	/// <param name="format">format of output</param>
	public string ToString([StringSyntax("NumericFormat")] string? format)
	{
		return this.ToString(format, Print.Culture);
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string ToString([StringSyntax("NumericFormat")] string? format, IFormatProvider? provider = null)
	{
		Span<char> chars = stackalloc char[100];
		if (!TryFormat(chars, out int charsWritten, format.AsSpan(), provider))
			return string.Empty;
		return new(chars[..charsWritten]);
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
	{
		destination[0] = '('; charsWritten = 1;
		if (!this.real.TryFormat(destination[1..], out int cw1, format, provider))
			return false;
		charsWritten += cw1;
		if (charsWritten + 1 >= destination.Length)
			return false;
		destination[charsWritten] = ','; charsWritten++;
		if (!this.real.TryFormat(destination[charsWritten..], out int cw2, format, provider))
			return false;
		charsWritten += cw2;
		if (charsWritten + 1 >= destination.Length)
			return false;
		destination[charsWritten] = ')'; charsWritten++;
		return true;
	}
	#endregion
}