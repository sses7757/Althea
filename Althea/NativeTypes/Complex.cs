using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

using Althea.Resources;


namespace Althea.NativeTypes
{
	#region interface
	/// <summary>
	/// The base interface for complex numbers
	/// </summary>
	/// <typeparam name="TSelf">The actual type of implemented complex number struct/class</typeparam>
	/// <typeparam name="T">The type of corresponding real number</typeparam>
	public interface IComplexNumber<TSelf, T> : INumber<TSelf> where TSelf : IComplexNumber<TSelf, T> where T : unmanaged, INumber<T>
	{
		/// <summary>
		/// Abstract static get imaginary one for <typeparamref name="TSelf"/>
		/// </summary>
		abstract static TSelf ImaginaryOne { get; }
		/// <summary>
		/// Abstract static get negative imaginary one for <typeparamref name="TSelf"/>
		/// </summary>
		abstract static TSelf ImaginaryNegativeOne { get; }

		/// <summary>
		/// Get the real part of this complex number
		/// </summary>
		T Real { get; }
		/// <summary>
		/// Get the imaginary part of this complex number
		/// </summary>
		T Imaginary { get; }
		/// <summary>
		/// Get the magnitude or absolute value of this complex number
		/// </summary>
		T Magnitude { get; }
		/// <summary>
		/// Get the phase of this complex number
		/// </summary>
		T Phase { get; }

		/// <summary>
		/// Statically return the absolute value of the given <paramref name="complex"/> number
		/// </summary>
		/// <param name="complex">The complex number of type <typeparamref name="TSelf"/></param>
		/// <returns>The magnitude or absolute value of the given <paramref name="complex"/> number</returns>
		new static T Abs(TSelf complex) => complex.Magnitude;

		/// <summary>
		/// Get the complex conjugate of this complex number
		/// </summary>
		TSelf Conjugate { get; }

		/// <summary>
		/// Implicitly convert a real number of <typeparamref name="T"/> to complex <typeparamref name="TSelf"/>
		/// </summary>
		/// <param name="real">The input real number of type <typeparamref name="T"/></param>
		abstract static implicit operator TSelf(T real);

		/// <summary>
		/// Implicitly convert a pair of real number of <typeparamref name="T"/> to complex <typeparamref name="TSelf"/>
		/// </summary>
		/// <param name="v">The input real and imaginary parts of type <typeparamref name="T"/></param>
		abstract static implicit operator TSelf((T real, T imag) v);
	}

	/// <summary>
	/// The base interface for complex float numbers
	/// </summary>
	/// <typeparam name="TSelf">The actual type of implemented complex number struct/class</typeparam>
	/// <typeparam name="T">The type of corresponding real number</typeparam>
	public interface IComplexFloatNumber<TSelf, T> : IComplexNumber<TSelf, T>, IFloatingPoint<TSelf>
		where TSelf : IComplexFloatNumber<TSelf, T>
		where T : unmanaged, IFloatingPoint<T>
	{
		/// <summary>
		/// Statically return the complex power of the given <paramref name="complex"/> number and a real power <paramref name="p"/>
		/// </summary>
		/// <param name="complex">The complex number of type <typeparamref name="TSelf"/></param>
		/// <param name="p">The power as a real number of type <typeparamref name="T"/></param>
		/// <returns>The complex power of <paramref name="complex"/> to <paramref name="p"/></returns>
		abstract static TSelf Pow(TSelf complex, T p);
	}
	#endregion

	#region generic complex type
	/// <summary>
	/// The general complex type for any real floating point numeric number type including <see cref="float"/> and <see cref="double"/>
	/// </summary>
	/// <typeparam name="T">The data type of corresponding real number</typeparam>
	[StructLayout(LayoutKind.Sequential)]
	public struct Complex<T> : IComplexFloatNumber<Complex<T>, T>, ICustomNativeType<Complex<T>> where T : unmanaged, IFloatingPoint<T>
	{
		#region basic
		private readonly T real, imag;

		/// <summary>
		/// Get the real part
		/// </summary>
		public T Real {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.real;
		}

		/// <summary>
		/// Get the imaginary part
		/// </summary>
		public T Imaginary {
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
		/// <summary>
		/// Statically get the <see cref="DataTypeClassification"/> of <see cref="Complex{T}"/>
		/// </summary>
		public static DataTypeClassification Classification => NumberTypes<T>.Classification;

		/// <summary>
		/// Statically get the machine precision of <see cref="Complex{T}"/>
		/// </summary>
		public static double MachinePrecision => NumberTypes<T>.MachinePrecision;

		static Complex()
		{
			// generic type check
			if (typeof(T).IsGenericType)
				throw new InvalidOperationException(Support.DataType);
			// native type check
			if (NumberTypes<T>.Classification < DataTypeClassification.FloatPoint_IEEE754)
				throw new InvalidOperationException(Support.DataType);
		}
		#endregion

		#region constant values
		/// <summary>
		/// <see cref="Complex{T}"/> 0
		/// </summary>
		public static Complex<T> Zero => new(T.Zero);
		/// <summary>
		/// <see cref="Complex{T}"/> -0
		/// </summary>
		public static Complex<T> NegativeZero => new(T.NegativeZero);
		/// <summary>
		/// <see cref="Complex{T}"/> 1
		/// </summary>
		public static Complex<T> One => new(T.One);
		/// <summary>
		/// <see cref="Complex{T}"/> -1
		/// </summary>
		public static Complex<T> NegativeOne => new(T.NegativeOne);
		/// <summary>
		/// <see cref="Complex{T}"/> i
		/// </summary>
		public static Complex<T> ImaginaryOne => new(T.Zero, T.One);
		/// <summary>
		/// <see cref="Complex{T}"/> -1
		/// </summary>
		public static Complex<T> ImaginaryNegativeOne => new(T.Zero, T.NegativeOne);

		static Complex<T> IAdditiveIdentity<Complex<T>, Complex<T>>.AdditiveIdentity => Zero;
		static Complex<T> IMultiplicativeIdentity<Complex<T>, Complex<T>>.MultiplicativeIdentity => One;

		/// <summary>
		/// <see cref="Complex{T}"/> +∞
		/// </summary>
		public static Complex<T> PositiveInfinity => new(T.PositiveInfinity);
		/// <summary>
		/// <see cref="Complex{T}"/> -∞
		/// </summary>
		public static Complex<T> NegativeInfinity => new(T.NegativeInfinity);
		/// <summary>
		/// <see cref="Complex{T}"/> NaN
		/// </summary>
		public static Complex<T> NaN => new(T.NaN);

		/// <summary>
		/// <see cref="Complex{T}"/> τ
		/// </summary>
		public static Complex<T> Tau => new(T.Tau);
		/// <summary>
		/// <see cref="Complex{T}"/> π
		/// </summary>
		public static Complex<T> Pi => new(T.Pi);
		/// <summary>
		/// <see cref="Complex{T}"/> ε
		/// </summary>
		public static Complex<T> Epsilon => new(T.Epsilon);
		/// <summary>
		/// <see cref="Complex{T}"/> e
		/// </summary>
		public static Complex<T> E => new(T.E);

		/// <summary>
		/// Statically check whether the given <paramref name="value"/> is finite or not
		/// </summary>
		/// <param name="value">The given number to check</param>
		/// <returns>Whether <paramref name="value"/> is finite or not</returns>
		public static bool IsFinite(Complex<T> value) => T.IsFinite(value.real) && T.IsFinite(value.imag);
		/// <summary>
		/// Statically check whether the given <paramref name="value"/> is infinity or not
		/// </summary>
		/// <param name="value">The given number to check</param>
		/// <returns>Whether <paramref name="value"/> is infinity or not</returns>
		public static bool IsInfinity(Complex<T> value) => T.IsInfinity(value.real) || T.IsInfinity(value.imag);
		/// <summary>
		/// Statically check whether the given <paramref name="value"/> is NaN or not
		/// </summary>
		/// <param name="value">The given number to check</param>
		/// <returns>Whether <paramref name="value"/> is NaN or not</returns>
		public static bool IsNaN(Complex<T> value) => T.IsNaN(value.real) && T.IsNaN(value.imag);
		/// <summary>
		/// Statically check whether the given <paramref name="value"/> is negative or not
		/// </summary>
		/// <param name="value">The given number to check</param>
		/// <returns>Whether <paramref name="value"/> is negative or not</returns>
		public static bool IsNegative(Complex<T> value) => T.IsNegative(value.real) && value.imag == T.Zero;
		/// <summary>
		/// Statically check whether the given <paramref name="value"/> is negative infinity or not
		/// </summary>
		/// <param name="value">The given number to check</param>
		/// <returns>Whether <paramref name="value"/> is negative infinity or not</returns>
		public static bool IsNegativeInfinity(Complex<T> value) => T.IsNegativeInfinity(value.real) && value.imag == T.Zero;
		/// <summary>
		/// Statically check whether the given <paramref name="value"/> is positive infinity or not
		/// </summary>
		/// <param name="value">The given number to check</param>
		/// <returns>Whether <paramref name="value"/> is positive infinity or not</returns>
		public static bool IsPositiveInfinity(Complex<T> value) => T.IsPositiveInfinity(value.real) && value.imag == T.Zero;
		/// <summary>
		/// Statically check whether the given <paramref name="value"/> is normal or not
		/// </summary>
		/// <param name="value">The given number to check</param>
		/// <returns>Whether <paramref name="value"/> is normal or not</returns>
		public static bool IsNormal(Complex<T> value) => T.IsNormal(value.real) && T.IsNormal(value.imag);
		/// <summary>
		/// Statically check whether the given <paramref name="value"/> is subnormal or not
		/// </summary>
		/// <param name="value">The given number to check</param>
		/// <returns>Whether <paramref name="value"/> is subnormal or not</returns>
		public static bool IsSubnormal(Complex<T> value) => T.IsSubnormal(value.real) && T.IsSubnormal(value.imag);
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
		private static unsafe bool TryParseAny(delegate*<string, NumberStyles, IFormatProvider?, out T, bool> parseFunc, string? str, NumberStyles style, IFormatProvider? provider, out T real, out T imag)
		{
			if (str is null)
				throw new ArgumentNullException(nameof(str));

			real = imag = default;

			Regex regex = new(regexPattern1);
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
			regex = new(regexPattern2);
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
				throw new ArgumentException(string.Format(Other.CannotParseComplex, str, typeof(T).Name), nameof(str));
			return result;
		}

		/// <summary>
		/// See <see cref="Parse(string?, NumberStyles, IFormatProvider?)"/>
		/// </summary>
		public static Complex<T> Parse(string? s, IFormatProvider? provider) => Parse(s, NumberStyles.Any, provider);
		/// <summary>
		/// See <see cref="TryParse(string?, NumberStyles, IFormatProvider, out Complex{T})"/>
		/// </summary>
		public static bool TryParse(string? s, IFormatProvider? provider, out Complex<T> result) => TryParse(s, NumberStyles.Any, provider, out result);

		/// <summary>
		/// See <see cref="Parse(string?, NumberStyles, IFormatProvider?)"/>
		/// </summary>
		public static Complex<T> Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider) => Parse(new string(s), style, provider);
		/// <summary>
		/// See <see cref="TryParse(string?, NumberStyles, IFormatProvider?, out Complex{T})"/>
		/// </summary>
		public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out Complex<T> result) => TryParse(new string(s), style, provider, out result);
		/// <summary>
		/// See <see cref="Parse(ReadOnlySpan{char}, NumberStyles, IFormatProvider?)"/>
		/// </summary>
		public static Complex<T> Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s, NumberStyles.Any, provider);
		/// <summary>
		/// See <see cref="TryParse(ReadOnlySpan{char}, NumberStyles, IFormatProvider?, out Complex{T})"/>
		/// </summary>
		public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Complex<T> result) => TryParse(s, NumberStyles.Any, provider, out result);
		#endregion

		#region converter
		/// <summary>
		/// Convert from T
		/// </summary>
		/// <param name="a">a T</param>
		public static implicit operator Complex<T>(T a) => new(a);
		/// <summary>
		/// Convert from T tuple
		/// </summary>
		/// <param name="a">a T tuple</param>
		public static implicit operator Complex<T>((T real, T imag) a) => new(a.real, a.imag);

		/// <summary>
		/// Convert to <typeparamref name="T"/> by taking abs
		/// </summary>
		public static explicit operator T(Complex<T> v) => v.Magnitude;


		public static Complex<T> Create<TOther>(TOther value) where TOther : INumber<TOther>
		{

		}

		public static Complex<T> CreateSaturating<TOther>(TOther value) where TOther : INumber<TOther>
		{

		}

		public static Complex<T> CreateTruncating<TOther>(TOther value) where TOther : INumber<TOther>
		{
			throw new NotImplementedException();
		}

		public static bool TryCreate<TOther>(TOther value, out Complex<T> result) where TOther : INumber<TOther>
		{
			throw new NotImplementedException();
		}
		#endregion

		#region equality
		/// <summary>
		/// Equality operator
		/// </summary>
		public static bool operator ==(Complex<T> a, Complex<T> b) => a.Equals(b);

		/// <summary>
		/// Inequality operator
		/// </summary>
		public static bool operator !=(Complex<T> a, Complex<T> b) => !(a == b);

		/// <summary>
		/// Equality operator
		/// </summary>
		/// <param name="other">The other <see cref="Complex{T}"/> to compare</param>
		/// <returns>this == <paramref name="other"/></returns>
		public bool Equals(Complex<T> other) => this.real == other.real && this.imag == other.imag;

		/// <summary>
		/// Override <see cref="object.GetHashCode"/>
		/// </summary>
		public override int GetHashCode() => HashCode.Combine(this.real, this.imag);

		/// <summary>
		/// Override <see cref="object.Equals(object)"/>
		/// </summary>
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

		static bool IComparisonOperators<Complex<T>, Complex<T>>.operator <(Complex<T> left, Complex<T> right) => throw new InvalidOperationException();
		static bool IComparisonOperators<Complex<T>, Complex<T>>.operator <=(Complex<T> left, Complex<T> right) => throw new InvalidOperationException();
		static bool IComparisonOperators<Complex<T>, Complex<T>>.operator >(Complex<T> left, Complex<T> right) => throw new InvalidOperationException();
		static bool IComparisonOperators<Complex<T>, Complex<T>>.operator >=(Complex<T> left, Complex<T> right) => throw new InvalidOperationException();

		static Complex<T> IModulusOperators<Complex<T>, Complex<T>, Complex<T>>.operator %(Complex<T> left, Complex<T> right) => throw new InvalidOperationException();

		static Complex<T> INumber<Complex<T>>.Max(Complex<T> x, Complex<T> y) => throw new InvalidOperationException();
		static Complex<T> INumber<Complex<T>>.Min(Complex<T> x, Complex<T> y) => throw new InvalidOperationException();

		int IComparable.CompareTo(object? obj) => throw new InvalidOperationException();
		int IComparable<Complex<T>>.CompareTo(Complex<T> other) => throw new InvalidOperationException();
		#endregion

		#region arithmetic operators
		/// <summary>
		/// Complex negate
		/// </summary>
		public static Complex<T> operator -(Complex<T> a) => new(-a.real, -a.imag);
		/// <summary>
		/// Complex add
		/// </summary>
		public static Complex<T> operator +(Complex<T> a, Complex<T> b) => new(a.real + b.real, a.imag + b.imag);
		/// <summary>
		/// Complex subtract
		/// </summary>
		public static Complex<T> operator -(Complex<T> a, Complex<T> b) => new(a.real - b.real, a.imag - b.imag);
		/// <summary>
		/// Complex add real
		/// </summary>
		public static Complex<T> operator +(Complex<T> a, T b) => new(a.real - b, a.imag);
		/// <summary>
		/// Complex add real
		/// </summary>
		public static Complex<T> operator +(T b, Complex<T> a) => new(a.real - b, a.imag);
		/// <summary>
		/// Complex subtract real
		/// </summary>
		public static Complex<T> operator -(Complex<T> a, T b) => new(a.real - b, a.imag);
		/// <summary>
		/// Real subtract complex
		/// </summary>
		public static Complex<T> operator -(T b, Complex<T> a) => new(b - a.real, -a.imag);

		/*
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ComplexDouble DoubleMul(ComplexDouble a, double b)
		{
			return new ComplexDouble(a.real * b, a.imag * b);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ComplexDouble DoubleMul(ComplexDouble a, ComplexDouble b)
		{
			double real = Math.FusedMultiplyAdd(a.real, b.real, -a.imag * b.imag); // vfmsub213sd
			double imag = Math.FusedMultiplyAdd(a.real, b.imag, a.imag * b.real); // vfmadd213sd
			return new ComplexDouble(real, imag);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ComplexDouble DoubleDiv(ComplexDouble x, ComplexDouble y)
		{
			double squareAbsY = DoubleSquareAbs(y);
			double acbd = Math.FusedMultiplyAdd(x.real, y.real, x.imag * y.imag); // vfmadd213sd
			double bcad = Math.FusedMultiplyAdd(x.imag, y.real, -x.real * y.imag); // vfmsub213sd
			return new ComplexDouble(acbd / squareAbsY, bcad / squareAbsY);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ComplexDouble DoubleRealDiv(double x, ComplexDouble y)
		{
			double squareAbsY = DoubleSquareAbs(y);
			return new ComplexDouble(x * y.real / squareAbsY, -x * y.imag / squareAbsY);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static double DoubleSquareAbs(ComplexDouble a)
		{
			double squareAbs = Math.FusedMultiplyAdd(a.real, a.real, a.imag * a.imag); // vfmadd213sd
			return squareAbs;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ComplexDouble DoubleAddSquareAbs(ComplexDouble a, ComplexDouble b)
		{
			// a.r += b.r*b.r + b.i*b.i
			double real = Math.FusedMultiplyAdd(b.real, b.real, a.real);
			real = Math.FusedMultiplyAdd(b.imag, b.imag, real);
			// totally 2 FMA (naive is 1*FMA + 1*MUL + 1*ADD)
			return new(real, a.imag);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ComplexDouble DoubleMulConjA(ComplexDouble a, ComplexDouble b)
		{
			double real = Math.FusedMultiplyAdd(a.real, b.real, a.imag * b.imag); // vfmadd213sd
			double imag = Math.FusedMultiplyAdd(a.real, b.imag, -a.imag * b.real); // vfmsub213sd
			return new ComplexDouble(real, imag);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ComplexDouble DoubleFMA(ComplexDouble a, ComplexDouble b, ComplexDouble c)
		{
			// a.r * b.r - (a.i * b.i - c.r)
			double temp1 = Math.FusedMultiplyAdd(a.imag, b.imag, -c.real);
			double real = Math.FusedMultiplyAdd(a.real, b.real, -temp1);
			// a.r * b.i + (a.i * b.r + c.i)
			double temp2 = Math.FusedMultiplyAdd(a.imag, b.real, c.real);
			double imag = Math.FusedMultiplyAdd(a.real, b.imag, temp2);
			// totally 4 FMA (naive is 2*FMA + 2*MUL + 2*ADD)
			return new ComplexDouble(real, imag);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ComplexDouble DoubleFMAConjA(ComplexDouble a, ComplexDouble b, ComplexDouble c)
		{
			// a.r * b.r + (a.i * b.i + c.r)
			double temp1 = Math.FusedMultiplyAdd(a.imag, b.imag, c.real);
			double real = Math.FusedMultiplyAdd(a.real, b.real, temp1);
			// a.r * b.i - (a.i * b.r - c.i)
			double temp2 = Math.FusedMultiplyAdd(a.imag, b.real, -c.real);
			double imag = Math.FusedMultiplyAdd(a.real, b.imag, -temp2);
			// totally 4 FMA (naive is 2*FMA + 2*MUL + 2*ADD)
			return new ComplexDouble(real, imag);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ComplexDouble DoubleFMS(ComplexDouble a, ComplexDouble b, ComplexDouble c)
		{
			// a.r * b.r - (a.i * b.i + c.r)
			double temp1 = Math.FusedMultiplyAdd(a.imag, b.imag, c.real);
			double real = Math.FusedMultiplyAdd(a.real, b.real, -temp1);
			// a.r * b.i + (a.i * b.r - c.i)
			double temp2 = Math.FusedMultiplyAdd(a.imag, b.real, -c.real);
			double imag = Math.FusedMultiplyAdd(a.real, b.imag, temp2);
			return new ComplexDouble(real, imag);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ComplexDouble DoubleFMSConjA(ComplexDouble a, ComplexDouble b, ComplexDouble c)
		{
			// a.r * b.r + (a.i * b.i - c.r)
			double temp1 = Math.FusedMultiplyAdd(a.imag, b.imag, -c.real);
			double real = Math.FusedMultiplyAdd(a.real, b.real, temp1);
			// a.r * b.i - (a.i * b.r + c.i)
			double temp2 = Math.FusedMultiplyAdd(a.imag, b.real, c.real);
			double imag = Math.FusedMultiplyAdd(a.real, b.imag, -temp2);
			// totally 4 FMA (naive is 2*FMA + 2*MUL + 2*ADD)
			return new ComplexDouble(real, imag);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static double DoubleAbs(ComplexDouble a)
		{
			return Math.Sqrt(DoubleSquareAbs(a));
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static double DoubleArg(ComplexDouble a)
		{
			return Math.Atan2(a.imag, a.real);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ComplexDouble DoubleExp(ComplexDouble c)
		{
			double exp = Math.Exp(c.real);
			double cos = Math.Cos(c.imag);
			double sin = Math.Sin(c.imag);
			return new ComplexDouble(exp * cos, exp * sin);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ComplexDouble DoubleLog(ComplexDouble c)
		{
			double real = 0.5F * Math.Log(DoubleAbs(c));
			double imag = Math.Atan2(c.imag, c.real);
			return new ComplexDouble(real, imag);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ComplexDouble DoublePowReal(ComplexDouble c, double p)
		{
			if (c.imag == 0)
			{
				return new ComplexDouble(Math.Pow(c.real, p));
			}
			else
			{
				double absC = DoubleAbs(c);
				double argC = Math.Atan2(c.imag, c.real);
				double phase = p * argC;
				double scale = Math.Pow(absC, p);
				return new ComplexDouble(scale * Math.Cos(phase), scale * Math.Sin(phase));
			}
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ComplexDouble DoublePow(ComplexDouble c, double p)
		{
			if ((c.real == 0 || c.real == 1) && c.imag == 0)
				return c;
			return DoublePowReal(c, p);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ComplexDouble DoublePow(ComplexDouble c, ComplexDouble p)
		{
			if ((c.real == 0 || c.real == 1) && c.imag == 0)
				return c;
			if (p.imag == 0)
			{
				return DoublePowReal(c, p.real);
			}
			// else
			double absC = DoubleAbs(c);
			double argC = Math.Atan2(c.imag, c.real);
			double phase = p.real * argC + p.imag * Math.Log(absC);
			double scale = Math.Pow(absC, p.real) * Math.Exp(-p.imag * argC);
			return new ComplexDouble(scale * Math.Cos(phase), scale * Math.Sin(phase));
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ComplexDouble DoubleSqrt(ComplexDouble c)
		{
			double arg = 0.5F * DoubleArg(c);
			double scale = Math.Sqrt(DoubleAbs(c));
			double real = Math.Cos(arg);
			double imag = Math.Sin(arg);
			return new ComplexDouble(scale * real, scale * imag);
		}
		 */

		/// <summary>
		/// Complex multiply
		/// </summary>
		public static unsafe Complex<T> operator *(Complex<T> x, Complex<T> y)
		{
			T real = T.FusedMultiplyAdd(x.real, y.real, -x.imag * y.imag);
			T imag = T.FusedMultiplyAdd(x.real, y.imag, x.imag * y.real);
			return new Complex<T>(real, imag);
		}

		/// <summary>
		/// Complex division, guards against intermediate underflow and overflow by scaling
		/// </summary>
		public static unsafe Complex<T> operator /(Complex<T> x, Complex<T> y)
		{
			T squareAbsY = y.MagnitudeSquare;
			T acbd = T.FusedMultiplyAdd(x.real, y.real, x.imag * y.imag);
			T bcad = T.FusedMultiplyAdd(x.imag, y.real, -x.real * y.imag);
			return new(acbd / squareAbsY, bcad / squareAbsY);
		}

		/// <summary>
		/// Complex multiply real number
		/// </summary>
		public static Complex<T> operator *(Complex<T> a, T b) => new(a.real * b, a.imag * b);
		/// <summary>
		/// Complex multiply real number
		/// </summary>
		public static Complex<T> operator *(T b, Complex<T> a) => new(a.real * b, a.imag * b);
		/// <summary>
		/// Complex divide real number
		/// </summary>
		public static Complex<T> operator /(Complex<T> a, T b) => new(a.real / b, a.imag / b);

		/// <summary>
		/// Real number divide complex 
		/// </summary>
		public static unsafe Complex<T> operator /(T a, Complex<T> b)
		{
			T squareAbsY = b.MagnitudeSquare;
			return new(a * b.real / squareAbsY, -a * b.imag / squareAbsY);
		}

		/// <summary>
		/// Static unary plus operator for one operand <paramref name="value"/>
		/// </summary>
		/// <param name="value">The operand to be incremented of type <see cref="Complex{T}"/></param>
		/// <returns>The unary plus result of type <see cref="Complex{T}"/></returns>
		public static Complex<T> operator +(Complex<T> value) => value;

		/// <summary>
		/// Static unary decrement operator for one operand <paramref name="value"/>
		/// </summary>
		/// <param name="value">The operand to be incremented of type <see cref="Complex{T}"/></param>
		/// <returns>The unary decrement result of type <see cref="Complex{T}"/></returns>
		public static Complex<T> operator --(Complex<T> value)
		{
			T r = value.real;
			r--;
			return new(r, value.imag);
		}

		/// <summary>
		/// Static unary increment operator for one operand <paramref name="value"/>
		/// </summary>
		/// <param name="value">The operand to be incremented of type <see cref="Complex{T}"/></param>
		/// <returns>The unary increment result of type <see cref="Complex{T}"/></returns>
		public static Complex<T> operator ++(Complex<T> value)
		{
			T r = value.real;
			r++;
			return new(r, value.imag);
		}
		#endregion

		#region other arithmetics
		/// <summary>
		/// Get the magnitude or absolute value of this <see cref="Complex{T}"/>
		/// </summary>
		public T Magnitude => T.Sqrt(this.MagnitudeSquare);

		/// <summary>
		/// Get the squared magnitude or absolute value of this <see cref="Complex{T}"/>
		/// </summary>
		public T MagnitudeSquare => T.FusedMultiplyAdd(this.real, this.real, this.imag * this.imag);

		/// <summary>
		/// Get the phase of this <see cref="Complex{T}"/>
		/// </summary>
		public T Phase => T.Atan2(this.imag, this.real);

		/// <summary>
		/// Complex conjugate
		/// </summary>
		public Complex<T> Conjugate => new(this.real, -this.imag);

		/// <summary>
		/// Complex number absolute value
		/// </summary>
		public static Complex<T> Abs(Complex<T> number) => number.Magnitude;

		/// <summary>
		/// Complex exponential (of base <c>e</c>)
		/// </summary>
		public static Complex<T> Exp(Complex<T> c)
		{
			T exp = T.Exp(c.real);
			T cos = T.Cos(c.imag);
			T sin = T.Sin(c.imag);
			return new(exp * cos, exp * sin);
		}

		private static readonly T OneFourth = T.One / (T.One + T.One + T.One + T.One);
		/// <summary>
		/// Complex logarithm (of base <c>e</c>)
		/// </summary>
		public static Complex<T> Log(Complex<T> c)
		{
			T real = OneFourth * T.Log(c.MagnitudeSquare);
			T imag = T.Atan2(c.imag, c.real);
			return new(real, imag);
		}

		private static readonly T RealTwo = T.One + T.One;
		/// <summary>
		/// Complex logarithm (of base <paramref name="b"/>)
		/// </summary>
		public static Complex<T> Log(Complex<T> x, Complex<T> b)
		{
			T squareAbsB = b.MagnitudeSquare;
			T realA = x.Phase * RealTwo, realB = b.Phase * RealTwo;
			T imagA = T.Log(x.MagnitudeSquare), imagB = T.Log(squareAbsB);
			T acbd = T.FusedMultiplyAdd(realA, realB, imagA * imagB);
			T bcad = T.FusedMultiplyAdd(-imagA, realB, realA * imagB);
			return new(acbd / squareAbsB, bcad / squareAbsB);
		}

		private static readonly T OneOverLog2 = T.Log(T.One + T.One);
		private static readonly T OneOverLog10 = T.Log((T.One + T.One) * (T.One + T.One) * (T.One + T.One) + (T.One + T.One));
		/// <summary>
		/// Complex logarithm (of base <c>2</c>)
		/// </summary>
		public static Complex<T> Log2(Complex<T> x) => Log(x) * OneOverLog2;
		/// <summary>
		/// Complex logarithm (of base <c>10</c>)
		/// </summary>
		public static Complex<T> Log10(Complex<T> x) => Log(x) * OneOverLog10;

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
				return new(scale * T.Cos(phase), scale * T.Sin(phase));
			}
		}
		/// <summary>
		/// Complex number power of real type
		/// </summary>
		public static Complex<T> Pow(Complex<T> c, T p)
		{
			if ((c.real == T.Zero || c.real == T.One) && c.imag == T.Zero)
				return c;
			return PowReal(c, p);
		}
		/// <summary>
		/// Complex number power of complex type
		/// </summary>
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
			return new(scale * T.Cos(phase), scale * T.Sin(phase));
		}

		/// <summary>
		/// Complex number reciprocal
		/// </summary>
		public static Complex<T> Reciprocal(Complex<T> number) => T.One / number;

		private static readonly T Half = T.One / (T.One + T.One);
		/// <summary>
		/// Complex number square root
		/// </summary>
		public static Complex<T> Sqrt(Complex<T> c)
		{
			T arg = Half * c.Phase;
			T scale = T.Sqrt(c.Magnitude);
			T real = T.Cos(arg);
			T imag = T.Sin(arg);
			return new(scale * real, scale * imag);
		}

		/// <summary>
		/// Complex number FMA: <c><paramref name="a"/> * <paramref name="b"/> + <paramref name="c"/></c>
		/// </summary>
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

		public static Complex<T> Acos(Complex<T> x)
		{
			throw new NotImplementedException();
		}
		public static Complex<T> Acosh(Complex<T> x)
		{
			throw new NotImplementedException();
		}
		public static Complex<T> Asin(Complex<T> x)
		{
			throw new NotImplementedException();
		}
		public static Complex<T> Asinh(Complex<T> x)
		{
			throw new NotImplementedException();
		}
		public static Complex<T> Atan(Complex<T> x)
		{
			throw new NotImplementedException();
		}
		public static Complex<T> Atan2(Complex<T> y, Complex<T> x)
		{
			throw new NotImplementedException();
		}
		public static Complex<T> Atanh(Complex<T> x)
		{
			throw new NotImplementedException();
		}
		public static Complex<T> Cos(Complex<T> x)
		{
			throw new NotImplementedException();
		}
		public static Complex<T> Cosh(Complex<T> x)
		{
			throw new NotImplementedException();
		}
		public static Complex<T> Sin(Complex<T> x)
		{
			throw new NotImplementedException();
		}
		public static Complex<T> Sinh(Complex<T> x)
		{
			throw new NotImplementedException();
		}
		public static Complex<T> Tan(Complex<T> x)
		{
			throw new NotImplementedException();
		}
		public static Complex<T> Tanh(Complex<T> x)
		{
			throw new NotImplementedException();
		}



		public static Complex<T> Cbrt(Complex<T> x)
		{
			throw new NotImplementedException();
		}
		public static Complex<T> CopySign(Complex<T> x, Complex<T> y)
		{
			throw new NotImplementedException();
		}
		public static Complex<T> Ceiling(Complex<T> x)
		{
			throw new NotImplementedException();
		}
		public static Complex<T> Floor(Complex<T> x)
		{
			throw new NotImplementedException();
		}


		public static Complex<T> IEEERemainder(Complex<T> a, Complex<T> b)
		{
			throw new NotImplementedException();
		}

		public static TInteger ILogB<TInteger>(Complex<T> x) where TInteger : IBinaryInteger<TInteger>
		{
			throw new NotImplementedException();
		}

		public static Complex<T> MaxMagnitude(Complex<T> x, Complex<T> y)
		{
			throw new NotImplementedException();
		}

		public static Complex<T> MinMagnitude(Complex<T> x, Complex<T> y)
		{
			throw new NotImplementedException();
		}

		public static Complex<T> Round(Complex<T> x)
		{
			throw new NotImplementedException();
		}

		public static Complex<T> Round<TInteger>(Complex<T> x, TInteger digits) where TInteger : IBinaryInteger<TInteger>
		{
			throw new NotImplementedException();
		}

		public static Complex<T> Round(Complex<T> x, MidpointRounding mode)
		{
			throw new NotImplementedException();
		}

		public static Complex<T> Round<TInteger>(Complex<T> x, TInteger digits, MidpointRounding mode) where TInteger : IBinaryInteger<TInteger>
		{
			throw new NotImplementedException();
		}

		public static Complex<T> ScaleB<TInteger>(Complex<T> x, TInteger n) where TInteger : IBinaryInteger<TInteger>
		{
			throw new NotImplementedException();
		}

		public static Complex<T> Truncate(Complex<T> x)
		{
			throw new NotImplementedException();
		}

		public static Complex<T> Clamp(Complex<T> value, Complex<T> min, Complex<T> max)
		{
			throw new NotImplementedException();
		}

		public static (Complex<T> Quotient, Complex<T> Remainder) DivRem(Complex<T> left, Complex<T> right)
		{
			throw new NotImplementedException();
		}

		public static Complex<T> Sign(Complex<T> value)
		{
			throw new NotImplementedException();
		}


		static Complex<T> IFloatingPoint<Complex<T>>.BitIncrement(Complex<T> x) => throw new InvalidOperationException();
		static Complex<T> IFloatingPoint<Complex<T>>.BitDecrement(Complex<T> x) => throw new InvalidOperationException();
		#endregion

		#region string representation
		/// <summary>
		/// Override <see cref="object.ToString"/>
		/// </summary>
		public override string ToString()
		{
			return this.ToString(null, Print.Culture);
		}

		/// <summary>
		/// String representation of this complex number
		/// </summary>
		/// <param name="format">format of output</param>
		public string ToString(string? format)
		{
			return this.ToString(format, Print.Culture);
		}

		/// <summary>
		/// Implementation of <see cref="IFormattable.ToString(string, IFormatProvider)"/> that formats the value of the current instance using the specified format.
		/// </summary>
		/// <param name="format">The format to use</param>
		/// <param name="formatProvider">The provider to use to format the value</param>
		public string ToString(string? format, IFormatProvider? formatProvider = null)
		{
			formatProvider ??= Print.Culture;
			string r, i;
			if (this.real is IFormattable f && this.imag is IFormattable g)
			{
				r = f.ToString(format, formatProvider);
				i = g.ToString(format, formatProvider);
			}
			else
			{
				r = string.Format(formatProvider, $"{{0:{format}}}", this.real);
				i = string.Format(formatProvider, $"{{0:{format}}}", this.imag);
			}
			return $"({r},{i})";
		}

		/// <summary>
		/// Try to format this complex number to the <paramref name="destination"/> <see cref="Span{T}"/> of <see cref="char"/>
		/// </summary>
		/// <param name="destination">The output string as a <see cref="Span{T}"/> of <see cref="char"/></param>
		/// <param name="charsWritten">Output the number of chars written in <paramref name="destination"/></param>
		/// <param name="format">The format string as a <see cref="ReadOnlySpan{T}"/> of <see cref="char"/></param>
		/// <param name="provider">The <see cref="IFormatProvider"/></param>
		/// <returns>Success or not</returns>
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
	#endregion
}