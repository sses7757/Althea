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
	public interface IComplexNumber<TSelf> : INumber<TSelf> where TSelf : IComplexNumber<TSelf>
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
	public interface IComplexNumber<TSelf, T> : IComplexNumber<TSelf> where TSelf : IComplexNumber<TSelf, T> where T : unmanaged, INumber<T>
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
	public interface IComplexFloatNumber<TSelf> : IComplexNumber<TSelf>, IFloatingPoint<TSelf>
		where TSelf : IComplexFloatNumber<TSelf>
	{
		/// <summary>
		/// Abstract static get negative imaginary one for <typeparamref name="TSelf"/>
		/// </summary>
		abstract static TSelf ImaginaryNegativeOne { get; }

		/// <summary>
		/// Get the complex conjugate of this complex number
		/// </summary>
		TSelf Conjugate { get; }
	}

	/// <summary>
	/// The base interface for complex float numbers with real type
	/// </summary>
	/// <typeparam name="TSelf">The actual type of implemented complex number struct/class</typeparam>
	/// <typeparam name="T">The type of corresponding real number</typeparam>
	public interface IComplexFloatNumber<TSelf, T> : IComplexNumber<TSelf, T>, IComplexFloatNumber<TSelf>
	where TSelf : IComplexFloatNumber<TSelf, T>
	where T : unmanaged, IFloatingPoint<T>
	{
		/// <summary>
		/// Get the magnitude or absolute value of this complex number
		/// </summary>
		T Magnitude { get; }

		/// <summary>
		/// Get the phase of this complex number
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
	public interface IComplexIntegerNumber<TSelf, T> : IComplexNumber<TSelf, T>, IBinaryInteger<TSelf>
		where TSelf : IComplexIntegerNumber<TSelf, T>
		where T : unmanaged, IBinaryInteger<T>
	{

	}
	#endregion

	#region generic complex type
	/// <summary>
	/// The general complex type for any real floating point numeric number type including <see cref="float"/> and <see cref="double"/>
	/// </summary>
	/// <typeparam name="T">The data type of corresponding real number</typeparam>
	/// <remarks>Arithmetic overflows are not checked in the methods of <see cref="Complex{T}"/> since they shall not be used for actual computation tasks,<br/>
	/// although JIT will probably inline the static functions inside them for better performance if it detected that they are hot paths.</remarks>
	[StructLayout(LayoutKind.Sequential)]
	public struct Complex<T> : IComplexFloatNumber<Complex<T>, T>, ICustomNumberType<Complex<T>> where T : unmanaged, IFloatingPoint<T>
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
		/// <summary>
		/// Statically get the <see cref="DataTypeClassification"/> of <see cref="Complex{T}"/>
		/// </summary>
		public static DataTypeClassification Classification => NumberType<T>.Classification;

		/// <summary>
		/// Statically get the machine precision of <see cref="Complex{T}"/>
		/// </summary>
		public static double MachinePrecision => NumberType<T>.MachinePrecision;

		/// <summary>
		/// Always return true
		/// </summary>
		public static bool IsComplex => true;

		static Complex()
		{
			// generic type check
			if (typeof(T).IsGenericType)
				throw new InvalidOperationException(Support.DataType);
			// native type check
			if (NumberType<T>.Classification < DataTypeClassification.FloatPoint_IEEE754 ||
				NumberType<T>.Classification == DataTypeClassification.SignedInteger ||
				NumberType<T>.Classification == DataTypeClassification.UnsignedInteger)
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
		/// Convert from a real number of type <typeparamref name="T"/>
		/// </summary>
		/// <param name="real">A real number of type <typeparamref name="T"/></param>
		public static implicit operator Complex<T>(T real) => new(real);
		/// <summary>
		/// Convert from a pair of real numbers as real and imaginary parts of type <typeparamref name="T"/>
		/// </summary>
		/// <param name="val">A pair of real numbers as real and imaginary parts of type <typeparamref name="T"/></param>
		public static implicit operator Complex<T>((T real, T imag) val) => new(val.real, val.imag);

		/// <summary>
		/// Convert to <typeparamref name="T"/> by taking absolute value
		/// </summary>
		public static explicit operator T(Complex<T> v) => v.Magnitude;

		/// <summary>
		/// Tries to create a complex from the given <paramref name="value"/>
		/// </summary>
		/// <typeparam name="TOther">The other type to create from</typeparam>
		/// <param name="value">The value to create from of type <typeparamref name="TOther"/></param>
		/// <param name="result">A <see cref="Complex{T}"/> created from <paramref name="value"/></param>
		/// <returns>Success or not</returns>
		public static bool TryCreate<TOther>(TOther value, out Complex<T> result) where TOther : INumber<TOther>
		{
			result = default;
			if (!typeof(TOther).IsValueType)
				return false;
			// complex
			if (value is Complex<T> c)
			{
				result = c;
				return true;
			}
			if (NumberType<TOther>.IsComplex)
			{
				return ComplexConverter.Converter<Complex<T>, TOther>.Default?.Invoke(value, out result) ?? false;
			}
			// real
			if (!T.TryCreate(value, out T real))
				return false;
			result = new(real);
			return true;
		}
		/// <summary>
		/// Create a complex from the given <paramref name="value"/>
		/// </summary>
		/// <typeparam name="TOther">The other type to create from</typeparam>
		/// <param name="value">The value to create from of type <typeparamref name="TOther"/></param>
		/// <returns>A <see cref="Complex{T}"/> created from <paramref name="value"/></returns>
		public static Complex<T> Create<TOther>(TOther value) where TOther : INumber<TOther>
		{
			if (!TryCreate(value, out var c))
				throw new NotSupportedException(Support.DataType);
			return c;
		}
		/// <summary>
		/// Create a complex from the given <paramref name="value"/>
		/// </summary>
		/// <typeparam name="TOther">The other type to create from</typeparam>
		/// <param name="value">The value to create from of type <typeparamref name="TOther"/></param>
		/// <returns>A <see cref="Complex{T}"/> created from <paramref name="value"/></returns>
		public static Complex<T> CreateSaturating<TOther>(TOther value) where TOther : INumber<TOther> => Create(value);
		/// <summary>
		/// Create a complex from the given <paramref name="value"/>
		/// </summary>
		/// <typeparam name="TOther">The other type to create from</typeparam>
		/// <param name="value">The value to create from of type <typeparamref name="TOther"/></param>
		/// <returns>A <see cref="Complex{T}"/> created from <paramref name="value"/></returns>
		public static Complex<T> CreateTruncating<TOther>(TOther value) where TOther : INumber<TOther> => Create(value);

		/// <summary>
		/// Statically try to create a number of type <typeparamref name="TOther"/> from a number of type <see cref="Complex{T}"/>.
		/// </summary>
		/// <typeparam name="TOther">The other number type to create to</typeparam>
		/// <param name="from">The input number to convert from of type <see cref="Complex{T}"/></param>
		/// <param name="to">The output number to convert to of type <typeparamref name="TOther"/></param>
		/// <returns>Conversion success or not.</returns>
		public static unsafe bool TryCreateOther<TOther>(Complex<T> from, out TOther to) where TOther : unmanaged, INumber<TOther>
		{
			to = default;
			// complex
			if (to is Complex<T>)
			{
				to = *(TOther*)(&from);
				return true;
			}
			if (NumberType<TOther>.IsComplex)
			{
				return ComplexConverter.Converter<TOther, Complex<T>>.Default?.Invoke(from, out to) ?? false;
			}
			// real
			return TOther.TryCreate(from.Magnitude, out to);
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

		/// <summary>
		/// Complex multiply
		/// </summary>
		public static Complex<T> operator *(Complex<T> x, Complex<T> y)
		{
			T real = T.FusedMultiplyAdd(x.real, y.real, -x.imag * y.imag);
			T imag = T.FusedMultiplyAdd(x.real, y.imag, x.imag * y.real);
			return new Complex<T>(real, imag);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Complex<T> AMinusXY(Complex<T> a, Complex<T> x, Complex<T> y)
		{
			T temp1 = T.FusedMultiplyAdd(x.real, y.real, -a.real);
			T real = T.FusedMultiplyAdd(x.imag, y.imag, -temp1);
			T temp2 = T.FusedMultiplyAdd(x.real, y.imag, -a.imag);
			T imag = T.FusedMultiplyAdd(x.imag, y.real, -temp2);
			return new Complex<T>(real, imag);
		}

		/// <summary>
		/// Complex division, guards against intermediate underflow and overflow by scaling
		/// </summary>
		public static Complex<T> operator /(Complex<T> x, Complex<T> y)
		{
			T squareAbsY = y.MagnitudeSquared;
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
		public static Complex<T> operator /(T a, Complex<T> b)
		{
			T squareAbsY = b.MagnitudeSquared;
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
		public T Magnitude => T.Sqrt(this.MagnitudeSquared);

		/// <summary>
		/// Get the squared magnitude or absolute value of this <see cref="Complex{T}"/>
		/// </summary>
		public T MagnitudeSquared => this.real * this.real + this.imag * this.imag;

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

		private static readonly T RealFour = T.Create(4);
		private static readonly T RealQuarter = T.Create(0.25);
		/// <summary>
		/// Complex logarithm (of base <c>e</c>)
		/// </summary>
		public static Complex<T> Log(Complex<T> c)
		{
			T real = RealQuarter * T.Log(c.MagnitudeSquared);
			T imag = T.Atan2(c.imag, c.real);
			return new(real, imag);
		}

		private static readonly T RealTwo = T.Create(2);
		/// <summary>
		/// Complex logarithm (of base <paramref name="b"/>)
		/// </summary>
		public static Complex<T> Log(Complex<T> x, Complex<T> b)
		{
			T squareAbsB = b.MagnitudeSquared;
			T realA = x.Phase * RealTwo, realB = b.Phase * RealTwo;
			T imagA = T.Log(x.MagnitudeSquared), imagB = T.Log(squareAbsB);
			T acbd = T.FusedMultiplyAdd(realA, realB, imagA * imagB);
			T bcad = T.FusedMultiplyAdd(-imagA, realB, realA * imagB);
			return new(acbd / squareAbsB, bcad / squareAbsB);
		}

		private static readonly T RealOneOverLog2 = T.One / T.Log(RealTwo);
		private static readonly T RealOneOverLog10 = T.One / T.Log(T.Create(10));
		/// <summary>
		/// Complex logarithm (of base <c>2</c>)
		/// </summary>
		public static Complex<T> Log2(Complex<T> x) => Log(x) * RealOneOverLog2;
		/// <summary>
		/// Complex logarithm (of base <c>10</c>)
		/// </summary>
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

		private static readonly T RealHalf = T.Create(0.5);
		/// <summary>
		/// Complex number square root
		/// </summary>
		public static Complex<T> Sqrt(Complex<T> c)
		{
			T arg = RealHalf * c.Phase;
			T scale = T.Sqrt(c.Magnitude);
			T real = T.Cos(arg);
			T imag = T.Sin(arg);
			return new(scale * real, scale * imag);
		}
		private static readonly T RealOneThird = T.Create(1.0M / 3.0M);
		/// <summary>
		/// Complex number cubic root
		/// </summary>
		public static Complex<T> Cbrt(Complex<T> x)
		{
			T arg = RealOneThird * x.Phase;
			T scale = T.Cbrt(x.Magnitude);
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

		/// <summary>
		/// Complex arc cosine
		/// </summary>
		public static Complex<T> Acos(Complex<T> x)
		{
			Complex<T> w = Sqrt(new(T.One + x.real, -x.imag));
			Complex<T> z = Sqrt(new(T.One - x.real, -x.imag));
			T real = RealTwo * T.Atan2(z.real, w.real);
			T imag = T.FusedMultiplyAdd(w.real, z.imag, w.imag * z.real);
			imag = T.Asinh(imag);
			return new(real, imag);
		}
		/// <summary>
		/// Complex arc hyperbolic cosine
		/// </summary>
		public static Complex<T> Acosh(Complex<T> x)
		{
			Complex<T> w = Sqrt(new(x.real + T.One, -x.imag));
			Complex<T> z = Sqrt(new(x.real - T.One, x.imag));
			T real = T.FusedMultiplyAdd(w.real, z.real, -w.imag * z.imag);
			real = T.Asinh(real);
			T imag = RealTwo * T.Atan2(Sqrt(w.Conjugate).imag, z.real);
			return new(real, imag);
		}
		/// <summary>
		/// Complex arc sine
		/// </summary>
		public static Complex<T> Asin(Complex<T> x)
		{
			Complex<T> asinh = Asinh(new(-x.imag, x.real));
			return new(asinh.imag, -asinh.real);
		}
		/// <summary>
		/// Complex arc hyperbolic sine
		/// </summary>
		public static Complex<T> Asinh(Complex<T> x)
		{
			Complex<T> w = Sqrt(new(T.One - x.imag, x.real));
			Complex<T> z = Sqrt(new(T.One + x.imag, -x.real));
			T real = T.FusedMultiplyAdd(w.imag, z.real, -w.real * z.imag);
			real = T.Asinh(real);
			T wzReal = T.FusedMultiplyAdd(x.real, x.real, -T.FusedMultiplyAdd(x.imag, x.imag, T.NegativeOne));
			T imag = T.Atan2(x.imag, wzReal);
			return new(real, imag);
		}
		/// <summary>
		/// Complex arc tangent
		/// </summary>
		public static Complex<T> Atan(Complex<T> x)
		{
			Complex<T> atanh = Atanh(new(-x.imag, x.real));
			return new(atanh.imag, -atanh.real);
		}
		/// <summary>
		/// Complex arc tangent of <paramref name="y"/>/<paramref name="x"/>
		/// </summary>
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
				return T.Log(p1) - ((p1 - T.One) - val) / p1;
			}
		}
		private static readonly T RealPiDiv2 = T.Create(1.5707963267948966192313216916397514M);
		/// <summary>
		/// Complex arc hyperbolic tangent
		/// </summary>
		public static Complex<T> Atanh(Complex<T> x)
		{
			T absRe = T.Abs(x.real), absIm = T.Abs(x.imag);
			T real, imag;
			if (absRe != T.One)
			{
				T reFrom1 = T.One - absRe;
				T imSquared = absIm * absIm;
				real = RealQuarter * Log1p(RealFour * absRe / T.FusedMultiplyAdd(reFrom1, reFrom1, imSquared));
				imag = RealHalf * T.Atan2(RealTwo * x.imag, T.FusedMultiplyAdd(reFrom1, absRe + T.One, -imSquared));
			}
			else if (x.imag == T.Zero)
			{ // (±1, 0)
				real = T.PositiveInfinity;
				imag = T.PositiveInfinity;
			}
			else
			{ // (±1, nonzero)
				real = T.Log(T.Sqrt(T.Sqrt(T.FusedMultiplyAdd(x.imag, x.imag, RealFour))) / T.Sqrt(absIm));
				imag = T.CopySign(RealHalf * (RealPiDiv2 + T.Atan2(absIm, RealTwo)), x.imag);
			}
			real = T.CopySign(real, x.real);
			return new(real, imag);
		}
		/// <summary>
		/// Complex cosine
		/// </summary>
		public static Complex<T> Cos(Complex<T> x)
		{
			T real = T.Cosh(x.imag) * T.Cos(x.real);
			T imag = -T.Sinh(x.imag) * T.Sin(x.real);
			return new(real, imag);
		}
		/// <summary>
		/// Complex hyperbolic cosine
		/// </summary>
		public static Complex<T> Cosh(Complex<T> x)
		{
			T real = T.Cosh(x.real) * T.Cos(x.imag);
			T imag = T.Sinh(x.real) * T.Sin(x.imag);
			return new(real, imag);
		}
		/// <summary>
		/// Complex sine
		/// </summary>
		public static Complex<T> Sin(Complex<T> x)
		{
			T real = T.Cosh(x.imag) * T.Sin(x.real);
			T imag = T.Sinh(x.imag) * T.Cos(x.real);
			return new(real, imag);
		}
		/// <summary>
		/// Complex hyperbolic sine
		/// </summary>
		public static Complex<T> Sinh(Complex<T> x)
		{
			T real = T.Sinh(x.real) * T.Cos(x.imag);
			T imag = T.Cosh(x.real) * T.Sin(x.imag);
			return new(real, imag);
		}
		/// <summary>
		/// Complex tangent
		/// </summary>
		public static Complex<T> Tan(Complex<T> x)
		{
			throw new NotImplementedException();
		}
		/// <summary>
		/// Complex hyperbolic tangent
		/// </summary>
		public static Complex<T> Tanh(Complex<T> x)
		{
			T imag = T.Tan(x.imag), s = T.Sinh(x.real);
			T b = s * T.FusedMultiplyAdd(imag, imag, T.One);
			T scale = T.One / T.FusedMultiplyAdd(b, s, T.One);
			T real = T.Sqrt(T.FusedMultiplyAdd(s, s, T.One)) * b;
			return new(real * scale, imag * scale);
		}

		/// <summary>
		/// Complex number copy signs of both parts from <paramref name="y"/> to <paramref name="x"/>
		/// </summary>
		public static Complex<T> CopySign(Complex<T> x, Complex<T> y)
		{
			return new(T.CopySign(x.real, y.real), T.CopySign(x.imag, y.imag));
		}
		/// <summary>
		/// Complex number ceiling for both parts
		/// </summary>
		public static Complex<T> Ceiling(Complex<T> x)
		{
			return new(T.Ceiling(x.real), T.Ceiling(x.imag));
		}
		/// <summary>
		/// Complex number floor for both parts
		/// </summary>
		public static Complex<T> Floor(Complex<T> x)
		{
			return new(T.Floor(x.real), T.Floor(x.imag));
		}
		/// <summary>
		/// Complex number round for both parts
		/// </summary>
		public static Complex<T> Round(Complex<T> x)
		{
			return new(T.Round(x.real), T.Round(x.imag));
		}
		/// <summary>
		/// Complex number round by <paramref name="digits"/> for both parts
		/// </summary>
		public static Complex<T> Round<TInteger>(Complex<T> x, TInteger digits) where TInteger : IBinaryInteger<TInteger>
		{
			return new(T.Round(x.real, digits), T.Round(x.imag, digits));
		}
		/// <summary>
		/// Complex number round by <paramref name="mode"/> for both parts
		/// </summary>
		public static Complex<T> Round(Complex<T> x, MidpointRounding mode)
		{
			return new(T.Round(x.real, mode), T.Round(x.imag, mode));
		}
		/// <summary>
		/// Complex number round by <paramref name="digits"/> and <paramref name="mode"/> for both parts
		/// </summary>
		public static Complex<T> Round<TInteger>(Complex<T> x, TInteger digits, MidpointRounding mode) where TInteger : IBinaryInteger<TInteger>
		{
			return new(T.Round(x.real, digits, mode), T.Round(x.imag, digits, mode));
		}
		/// <summary>
		/// Complex number scale by <c>2^<paramref name="n"/></c> for both parts of <paramref name="x"/>
		/// </summary>
		public static Complex<T> ScaleB<TInteger>(Complex<T> x, TInteger n) where TInteger : IBinaryInteger<TInteger>
		{
			return new(T.ScaleB(x.real, n), T.ScaleB(x.imag, n));
		}
		/// <summary>
		/// Complex number truncate for both parts
		/// </summary>
		public static Complex<T> Truncate(Complex<T> x)
		{
			return new(T.Truncate(x.real), T.Truncate(x.imag));
		}
		/// <summary>
		/// Complex number clamp for both parts of <paramref name="x"/> individually by <paramref name="min"/> and <paramref name="max"/>
		/// </summary>
		public static Complex<T> Clamp(Complex<T> x, Complex<T> min, Complex<T> max)
		{
			return new(T.Clamp(x.real, min.real, max.real), T.Clamp(x.imag, min.imag, max.imag));
		}
		/// <summary>
		/// Complex number IEEE remainder for both parts
		/// </summary>
		public static Complex<T> IEEERemainder(Complex<T> x, Complex<T> y)
		{
			return new(T.IEEERemainder(x.real, y.real), T.IEEERemainder(x.imag, y.imag));
		}
		/// <summary>
		/// Complex number compute quotients and remainders for both parts individually
		/// </summary>
		public static (Complex<T> Quotient, Complex<T> Remainder) DivRem(Complex<T> left, Complex<T> right)
		{
			(T qr, T rr) = T.DivRem(left.real, right.real);
			(T qi, T ri) = T.DivRem(left.imag, right.imag);
			return (new(qr, qi), new(rr, ri));
		}
		/// <summary>
		/// Complex number get signs for both parts individually
		/// </summary>
		public static Complex<T> Sign(Complex<T> x)
		{
			return new(T.Sign(x.real), T.Sign(x.imag));
		}

		/// <summary>
		/// Complex number max magnitude of <paramref name="x"/> and <paramref name="y"/>
		/// </summary>
		public static Complex<T> MaxMagnitude(Complex<T> x, Complex<T> y)
		{
			return new(T.Sqrt(T.MaxMagnitude(x.MagnitudeSquared, y.MagnitudeSquared)));
		}
		/// <summary>
		/// Complex number min magnitude of <paramref name="x"/> and <paramref name="y"/>
		/// </summary>
		public static Complex<T> MinMagnitude(Complex<T> x, Complex<T> y)
		{
			return new(T.Sqrt(T.MinMagnitude(x.MagnitudeSquared, y.MagnitudeSquared)));
		}

		static Complex<T> IFloatingPoint<Complex<T>>.BitIncrement(Complex<T> x) => throw new InvalidOperationException();
		static Complex<T> IFloatingPoint<Complex<T>>.BitDecrement(Complex<T> x) => throw new InvalidOperationException();
		static TInteger IFloatingPoint<Complex<T>>.ILogB<TInteger>(Complex<T> x) => throw new InvalidOperationException();
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
		/// <param name="provider">The <see cref="IFormatProvider"/> to use to format the value</param>
		public string ToString(string? format, IFormatProvider? provider = null)
		{
			Span<char> chars = stackalloc char[100];
			if (!TryFormat(chars, out int charsWritten, format.AsSpan(), provider))
				return string.Empty;
			return new(chars[..charsWritten]);
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

	/// <summary>
	/// The general complex type for any real integral numeric number type including <see cref="float"/> and <see cref="double"/>
	/// </summary>
	/// <typeparam name="T">The data type of corresponding real number</typeparam>
	[StructLayout(LayoutKind.Sequential)]
	public struct ComplexInteger<T> : IComplexIntegerNumber<ComplexInteger<T>, T>, ICustomNumberType<ComplexInteger<T>> where T : unmanaged, IBinaryInteger<T>
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
		/// <summary>
		/// Statically get the <see cref="DataTypeClassification"/> of <see cref="ComplexInteger{T}"/>
		/// </summary>
		public static DataTypeClassification Classification => NumberType<T>.Classification;

		/// <summary>
		/// Statically get the machine precision of <see cref="ComplexInteger{T}"/>
		/// </summary>
		public static double MachinePrecision => 1;

		/// <summary>
		/// Always return true
		/// </summary>
		public static bool IsComplex => true;

		static ComplexInteger()
		{
			// generic type check
			if (typeof(T).IsGenericType)
				throw new InvalidOperationException(Support.DataType);
			// native type check
			if (NumberType<T>.Classification != DataTypeClassification.SignedInteger &&
				NumberType<T>.Classification != DataTypeClassification.UnsignedInteger)
				throw new InvalidOperationException(Support.DataType);
		}
		#endregion

		#region constant values
		/// <summary>
		/// <see cref="ComplexInteger{T}"/> 0
		/// </summary>
		public static ComplexInteger<T> Zero => new(T.Zero);
		/// <summary>
		/// <see cref="ComplexInteger{T}"/> 1
		/// </summary>
		public static ComplexInteger<T> One => new(T.One);
		/// <summary>
		/// <see cref="ComplexInteger{T}"/> i
		/// </summary>
		public static ComplexInteger<T> ImaginaryOne => new(T.Zero, T.One);

		static ComplexInteger<T> IAdditiveIdentity<ComplexInteger<T>, ComplexInteger<T>>.AdditiveIdentity => Zero;
		static ComplexInteger<T> IMultiplicativeIdentity<ComplexInteger<T>, ComplexInteger<T>>.MultiplicativeIdentity => One;
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
		public unsafe static bool TryParse(string? str, NumberStyles style, IFormatProvider? provider, out ComplexInteger<T> complex)
		{
			complex = default;
			if (!Complex<double>.TryParse(str, style, provider, out var c))
				return false;
			if (c != Complex<double>.Round(c))
				return false;
			T re = T.Create(c.Real), im = T.Create(c.Imaginary);
			complex = new(re, im);
			return true;
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
				throw new ArgumentException(string.Format(Other.CannotParseComplex, str, typeof(T).Name), nameof(str));
			return result;
		}

		/// <summary>
		/// See <see cref="Parse(string?, NumberStyles, IFormatProvider?)"/>
		/// </summary>
		public static ComplexInteger<T> Parse(string? s, IFormatProvider? provider) => Parse(s, NumberStyles.Any, provider);
		/// <summary>
		/// See <see cref="TryParse(string?, NumberStyles, IFormatProvider, out ComplexInteger{T})"/>
		/// </summary>
		public static bool TryParse(string? s, IFormatProvider? provider, out ComplexInteger<T> result) => TryParse(s, NumberStyles.Any, provider, out result);

		/// <summary>
		/// See <see cref="Parse(string?, NumberStyles, IFormatProvider?)"/>
		/// </summary>
		public static ComplexInteger<T> Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider) => Parse(new string(s), style, provider);
		/// <summary>
		/// See <see cref="TryParse(string?, NumberStyles, IFormatProvider?, out ComplexInteger{T})"/>
		/// </summary>
		public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out ComplexInteger<T> result) => TryParse(new string(s), style, provider, out result);
		/// <summary>
		/// See <see cref="Parse(ReadOnlySpan{char}, NumberStyles, IFormatProvider?)"/>
		/// </summary>
		public static ComplexInteger<T> Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s, NumberStyles.Any, provider);
		/// <summary>
		/// See <see cref="TryParse(ReadOnlySpan{char}, NumberStyles, IFormatProvider?, out ComplexInteger{T})"/>
		/// </summary>
		public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out ComplexInteger<T> result) => TryParse(s, NumberStyles.Any, provider, out result);
		#endregion

		#region converter
		/// <summary>
		/// Convert from a real number of type <typeparamref name="T"/>
		/// </summary>
		/// <param name="real">A real number of type <typeparamref name="T"/></param>
		public static implicit operator ComplexInteger<T>(T real) => new(real);
		/// <summary>
		/// Convert from a pair of real numbers as real and imaginary parts of type <typeparamref name="T"/>
		/// </summary>
		/// <param name="val">A pair of real numbers as real and imaginary parts of type <typeparamref name="T"/></param>
		public static implicit operator ComplexInteger<T>((T real, T imag) val) => new(val.real, val.imag);

		/// <summary>
		/// Tries to create a complex from the given <paramref name="value"/>
		/// </summary>
		/// <typeparam name="TOther">The other type to create from</typeparam>
		/// <param name="value">The value to create from of type <typeparamref name="TOther"/></param>
		/// <param name="result">A <see cref="ComplexInteger{T}"/> created from <paramref name="value"/></param>
		/// <returns>Success or not</returns>
		public static bool TryCreate<TOther>(TOther value, out ComplexInteger<T> result) where TOther : INumber<TOther>
		{
			result = default;
			if (!typeof(TOther).IsValueType)
				return false;
			// complex
			if (value is ComplexInteger<T> c)
			{
				result = c;
				return true;
			}
			if (NumberType<TOther>.IsComplex)
			{
				return ComplexConverter.Converter<ComplexInteger<T>, TOther>.Default?.Invoke(value, out result) ?? false;
			}
			// real
			if (!T.TryCreate(value, out T real))
				return false;
			result = new(real);
			return true;
		}
		/// <summary>
		/// Create a complex from the given <paramref name="value"/>
		/// </summary>
		/// <typeparam name="TOther">The other type to create from</typeparam>
		/// <param name="value">The value to create from of type <typeparamref name="TOther"/></param>
		/// <returns>A <see cref="ComplexInteger{T}"/> created from <paramref name="value"/></returns>
		public static ComplexInteger<T> Create<TOther>(TOther value) where TOther : INumber<TOther>
		{
			if (!TryCreate(value, out var c))
				throw new NotSupportedException(Support.DataType);
			return c;
		}
		/// <summary>
		/// Create a complex from the given <paramref name="value"/>
		/// </summary>
		/// <typeparam name="TOther">The other type to create from</typeparam>
		/// <param name="value">The value to create from of type <typeparamref name="TOther"/></param>
		/// <returns>A <see cref="ComplexInteger{T}"/> created from <paramref name="value"/></returns>
		public static ComplexInteger<T> CreateSaturating<TOther>(TOther value) where TOther : INumber<TOther>
		{
			if (!typeof(TOther).IsValueType)
				throw new NotSupportedException(Support.DataType);
			// complex
			if (value is ComplexInteger<T> c)
			{
				return c;
			}
			if (NumberType<TOther>.IsComplex)
			{
				return ComplexConverter.Converter<ComplexInteger<T>, TOther>.Saturating?.Invoke(value) ?? throw new NotSupportedException(Support.DataType);
			}
			// real
			return new(T.CreateSaturating(value));
		}
		/// <summary>
		/// Create a complex from the given <paramref name="value"/>
		/// </summary>
		/// <typeparam name="TOther">The other type to create from</typeparam>
		/// <param name="value">The value to create from of type <typeparamref name="TOther"/></param>
		/// <returns>A <see cref="ComplexInteger{T}"/> created from <paramref name="value"/></returns>
		public static ComplexInteger<T> CreateTruncating<TOther>(TOther value) where TOther : INumber<TOther>
		{
			if (!typeof(TOther).IsValueType)
				throw new NotSupportedException(Support.DataType);
			// complex
			if (value is ComplexInteger<T> c)
			{
				return c;
			}
			if (NumberType<TOther>.IsComplex)
			{
				return ComplexConverter.Converter<ComplexInteger<T>, TOther>.Truncating?.Invoke(value) ?? throw new NotSupportedException(Support.DataType);
			}
			// real
			return new(T.CreateTruncating(value));
		}

		/// <summary>
		/// Statically try to create a number of type <typeparamref name="TOther"/> from a number of type <see cref="ComplexInteger{T}"/>.
		/// </summary>
		/// <typeparam name="TOther">The other number type to create to</typeparam>
		/// <param name="from">The input number to convert from of type <see cref="ComplexInteger{T}"/></param>
		/// <param name="to">The output number to convert to of type <typeparamref name="TOther"/></param>
		/// <returns>Conversion success or not.</returns>
		public static unsafe bool TryCreateOther<TOther>(ComplexInteger<T> from, out TOther to) where TOther : unmanaged, INumber<TOther>
		{
			to = default;
			// complex
			if (to is ComplexInteger<T>)
			{
				to = *(TOther*)(&from);
				return true;
			}
			if (NumberType<TOther>.IsComplex)
			{
				return ComplexConverter.Converter<TOther, ComplexInteger<T>>.Default?.Invoke(from, out to) ?? false;
			}
			// real
			return TOther.TryCreate(Math.Sqrt(from.MagnitudeSquared.As<T, double>()), out to);
		}
		#endregion

		#region equality
		/// <summary>
		/// Equality operator
		/// </summary>
		public static bool operator ==(ComplexInteger<T> a, ComplexInteger<T> b) => a.Equals(b);

		/// <summary>
		/// Inequality operator
		/// </summary>
		public static bool operator !=(ComplexInteger<T> a, ComplexInteger<T> b) => !(a == b);

		/// <summary>
		/// Equality operator
		/// </summary>
		/// <param name="other">The other <see cref="ComplexInteger{T}"/> to compare</param>
		/// <returns>this == <paramref name="other"/></returns>
		public bool Equals(ComplexInteger<T> other) => this.real == other.real && this.imag == other.imag;

		/// <summary>
		/// Override <see cref="object.GetHashCode"/>
		/// </summary>
		public override int GetHashCode() => HashCode.Combine(this.real, this.imag);

		/// <summary>
		/// Override <see cref="object.Equals(object)"/>
		/// </summary>
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

		static bool IComparisonOperators<ComplexInteger<T>, ComplexInteger<T>>.operator <(ComplexInteger<T> left, ComplexInteger<T> right) => throw new InvalidOperationException();
		static bool IComparisonOperators<ComplexInteger<T>, ComplexInteger<T>>.operator <=(ComplexInteger<T> left, ComplexInteger<T> right) => throw new InvalidOperationException();
		static bool IComparisonOperators<ComplexInteger<T>, ComplexInteger<T>>.operator >(ComplexInteger<T> left, ComplexInteger<T> right) => throw new InvalidOperationException();
		static bool IComparisonOperators<ComplexInteger<T>, ComplexInteger<T>>.operator >=(ComplexInteger<T> left, ComplexInteger<T> right) => throw new InvalidOperationException();

		static ComplexInteger<T> IModulusOperators<ComplexInteger<T>, ComplexInteger<T>, ComplexInteger<T>>.operator %(ComplexInteger<T> left, ComplexInteger<T> right) => throw new InvalidOperationException();

		static ComplexInteger<T> INumber<ComplexInteger<T>>.Max(ComplexInteger<T> x, ComplexInteger<T> y) => throw new InvalidOperationException();
		static ComplexInteger<T> INumber<ComplexInteger<T>>.Min(ComplexInteger<T> x, ComplexInteger<T> y) => throw new InvalidOperationException();

		int IComparable.CompareTo(object? obj) => throw new InvalidOperationException();
		int IComparable<ComplexInteger<T>>.CompareTo(ComplexInteger<T> other) => throw new InvalidOperationException();
		#endregion

		#region arithmetic operators
		/// <summary>
		/// ComplexInteger negate
		/// </summary>
		public static ComplexInteger<T> operator -(ComplexInteger<T> a) => new(-a.real, -a.imag);
		/// <summary>
		/// ComplexInteger add
		/// </summary>
		public static ComplexInteger<T> operator +(ComplexInteger<T> a, ComplexInteger<T> b) => new(a.real + b.real, a.imag + b.imag);
		/// <summary>
		/// ComplexInteger subtract
		/// </summary>
		public static ComplexInteger<T> operator -(ComplexInteger<T> a, ComplexInteger<T> b) => new(a.real - b.real, a.imag - b.imag);
		/// <summary>
		/// ComplexInteger add real
		/// </summary>
		public static ComplexInteger<T> operator +(ComplexInteger<T> a, T b) => new(a.real - b, a.imag);
		/// <summary>
		/// ComplexInteger add real
		/// </summary>
		public static ComplexInteger<T> operator +(T b, ComplexInteger<T> a) => new(a.real - b, a.imag);
		/// <summary>
		/// ComplexInteger subtract real
		/// </summary>
		public static ComplexInteger<T> operator -(ComplexInteger<T> a, T b) => new(a.real - b, a.imag);
		/// <summary>
		/// Real subtract complex
		/// </summary>
		public static ComplexInteger<T> operator -(T b, ComplexInteger<T> a) => new(b - a.real, -a.imag);

		/// <summary>
		/// ComplexInteger multiply
		/// </summary>
		public static ComplexInteger<T> operator *(ComplexInteger<T> x, ComplexInteger<T> y)
		{
			T real = x.real * y.real - x.imag * y.imag;
			T imag = x.real * y.imag + x.imag * y.real;
			return new ComplexInteger<T>(real, imag);
		}

		/// <summary>
		/// ComplexInteger division, guards against intermediate underflow and overflow by scaling
		/// </summary>
		public static ComplexInteger<T> operator /(ComplexInteger<T> x, ComplexInteger<T> y)
		{
			T squareAbsY = y.MagnitudeSquared;
			T acbd = x.real * y.real + x.imag * y.imag;
			T bcad = x.imag * y.real - x.real * y.imag;
			return new(acbd / squareAbsY, bcad / squareAbsY);
		}

		/// <summary>
		/// ComplexInteger multiply real number
		/// </summary>
		public static ComplexInteger<T> operator *(ComplexInteger<T> a, T b) => new(a.real * b, a.imag * b);
		/// <summary>
		/// ComplexInteger multiply real number
		/// </summary>
		public static ComplexInteger<T> operator *(T b, ComplexInteger<T> a) => new(a.real * b, a.imag * b);
		/// <summary>
		/// ComplexInteger divide real number
		/// </summary>
		public static ComplexInteger<T> operator /(ComplexInteger<T> a, T b) => new(a.real / b, a.imag / b);

		/// <summary>
		/// Real number divide complex 
		/// </summary>
		public static ComplexInteger<T> operator /(T a, ComplexInteger<T> b)
		{
			T squareAbsY = b.MagnitudeSquared;
			return new(a * b.real / squareAbsY, -a * b.imag / squareAbsY);
		}

		/// <summary>
		/// Static unary plus operator for one operand <paramref name="value"/>
		/// </summary>
		/// <param name="value">The operand to be incremented of type <see cref="ComplexInteger{T}"/></param>
		/// <returns>The unary plus result of type <see cref="ComplexInteger{T}"/></returns>
		public static ComplexInteger<T> operator +(ComplexInteger<T> value) => value;

		/// <summary>
		/// Static unary decrement operator for one operand <paramref name="value"/>
		/// </summary>
		/// <param name="value">The operand to be incremented of type <see cref="ComplexInteger{T}"/></param>
		/// <returns>The unary decrement result of type <see cref="ComplexInteger{T}"/></returns>
		public static ComplexInteger<T> operator --(ComplexInteger<T> value)
		{
			T r = value.real;
			r--;
			return new(r, value.imag);
		}

		/// <summary>
		/// Static unary increment operator for one operand <paramref name="value"/>
		/// </summary>
		/// <param name="value">The operand to be incremented of type <see cref="ComplexInteger{T}"/></param>
		/// <returns>The unary increment result of type <see cref="ComplexInteger{T}"/></returns>
		public static ComplexInteger<T> operator ++(ComplexInteger<T> value)
		{
			T r = value.real;
			r++;
			return new(r, value.imag);
		}

		/// <summary>
		/// Get the squared magnitude or absolute value of this <see cref="ComplexInteger{T}"/>
		/// </summary>
		public T MagnitudeSquared => this.real * this.real + this.imag * this.imag;

		/// <summary>
		/// ComplexInteger conjugate
		/// </summary>
		public ComplexInteger<T> Conjugate => new(this.real, -this.imag);

		/// <summary>
		/// ComplexInteger number absolute value for each part
		/// </summary>
		public static ComplexInteger<T> Abs(ComplexInteger<T> number) => new(T.Abs(number.real), T.Abs(number.imag));
		/// <summary>
		/// ComplexInteger number clamp for both parts of <paramref name="x"/> individually by <paramref name="min"/> and <paramref name="max"/>
		/// </summary>
		public static ComplexInteger<T> Clamp(ComplexInteger<T> x, ComplexInteger<T> min, ComplexInteger<T> max)
		{
			return new(T.Clamp(x.real, min.real, max.real), T.Clamp(x.imag, min.imag, max.imag));
		}
		/// <summary>
		/// ComplexInteger number compute quotients and remainders for both parts individually
		/// </summary>
		public static (ComplexInteger<T> Quotient, ComplexInteger<T> Remainder) DivRem(ComplexInteger<T> left, ComplexInteger<T> right)
		{
			(T qr, T rr) = T.DivRem(left.real, right.real);
			(T qi, T ri) = T.DivRem(left.imag, right.imag);
			return (new(qr, qi), new(rr, ri));
		}
		/// <summary>
		/// ComplexInteger number get signs for both parts individually
		/// </summary>
		public static ComplexInteger<T> Sign(ComplexInteger<T> x)
		{
			return new(T.Sign(x.real), T.Sign(x.imag));
		}
		/// <summary>
		/// ComplexInteger number log2 for both parts individually
		/// </summary>
		public static ComplexInteger<T> Log2(ComplexInteger<T> x)
		{
			return new(T.Log2(x.real), T.Log2(x.imag));
		}
		/// <summary>
		/// ComplexInteger check power of 2 for both parts
		/// </summary>
		public static bool IsPow2(ComplexInteger<T> x)
		{
			return T.IsPow2(x.real) && T.IsPow2(x.imag);
		}
		/// <summary>
		/// ComplexInteger get leading zero counts for each part
		/// </summary>
		public static ComplexInteger<T> LeadingZeroCount(ComplexInteger<T> value)
		{
			return new(T.LeadingZeroCount(value.real), T.LeadingZeroCount(value.imag));
		}
		/// <summary>
		/// ComplexInteger get trailing zero counts for each part
		/// </summary>
		public static ComplexInteger<T> TrailingZeroCount(ComplexInteger<T> value)
		{
			return new(T.TrailingZeroCount(value.real), T.TrailingZeroCount(value.imag));
		}
		/// <summary>
		/// ComplexInteger get pop counts for each part
		/// </summary>
		public static ComplexInteger<T> PopCount(ComplexInteger<T> value)
		{
			return new(T.PopCount(value.real), T.PopCount(value.imag));
		}
		/// <summary>
		/// ComplexInteger rotate left for each part
		/// </summary>
		public static ComplexInteger<T> RotateLeft(ComplexInteger<T> value, int rotateAmount)
		{
			return new(T.RotateLeft(value.real, rotateAmount), T.RotateLeft(value.imag, rotateAmount));
		}
		/// <summary>
		/// ComplexInteger rotate right for each part
		/// </summary>
		public static ComplexInteger<T> RotateRight(ComplexInteger<T> value, int rotateAmount)
		{
			return new(T.RotateRight(value.real, rotateAmount), T.RotateRight(value.imag, rotateAmount));
		}

		/// <summary>
		/// ComplexInteger bitwise AND for both parts
		/// </summary>
		public static ComplexInteger<T> operator &(ComplexInteger<T> left, ComplexInteger<T> right)
		{
			return new(left.real & right.real, left.imag & right.imag);
		}
		/// <summary>
		/// ComplexInteger bitwise OR for both parts
		/// </summary>
		public static ComplexInteger<T> operator |(ComplexInteger<T> left, ComplexInteger<T> right)
		{
			return new(left.real | right.real, left.imag | right.imag);
		}
		/// <summary>
		/// ComplexInteger bitwise XOR for both parts
		/// </summary>
		public static ComplexInteger<T> operator ^(ComplexInteger<T> left, ComplexInteger<T> right)
		{
			return new(left.real ^ right.real, left.imag ^ right.imag);
		}
		/// <summary>
		/// ComplexInteger bitwise NOT for both parts
		/// </summary>
		public static ComplexInteger<T> operator ~(ComplexInteger<T> left)
		{
			return new(~left.real, ~left.imag);
		}
		/// <summary>
		/// ComplexInteger left shift
		/// </summary>
		public static ComplexInteger<T> operator <<(ComplexInteger<T> value, int shiftAmount)
		{
			return new(value.real << shiftAmount, value.imag << shiftAmount);
		}
		/// <summary>
		/// ComplexInteger right shift
		/// </summary>
		public static ComplexInteger<T> operator >>(ComplexInteger<T> value, int shiftAmount)
		{
			return new(value.real >> shiftAmount, value.imag >> shiftAmount);
		}

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
		/// <param name="provider">The <see cref="IFormatProvider"/> to use to format the value</param>
		public string ToString(string? format, IFormatProvider? provider = null)
		{
			Span<char> chars = stackalloc char[100];
			if (!TryFormat(chars, out int charsWritten, format.AsSpan(), provider))
				return string.Empty;
			return new(chars[..charsWritten]);
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