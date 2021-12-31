using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

using Althea.Resources;


namespace Althea.NativeTypes
{
	#region single and double complex types
	/// <summary>
	/// The double precision float complex type
	/// </summary>
	/// <remarks>This is a separate struct since the native <see cref="double"/> has not implemented <see cref="IFloatNumber{TSelf}"/> yet</remarks>
	[StructLayout(LayoutKind.Sequential)]
	public struct ComplexDouble : IComplexNumber<ComplexDouble, double>, ICustomNativeType<ComplexDouble>
	{
		#region basic
		private readonly double real, imag;

		/// <summary>
		/// Get the real part
		/// </summary>
		public double Real
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.real;
		}

		/// <summary>
		/// Get the imaginary part
		/// </summary>
		public double Imaginary
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.imag;
		}

		/// <summary>
		/// Construct a <see cref="ComplexDouble"/> from real and imaginary parts
		/// </summary>
		/// <param name="re">The real part</param>
		/// <param name="im">The imaginary part, default value is 0</param>
		public ComplexDouble(double re, double im = default)
		{
			this.real = re;
			this.imag = im;
		}
		#endregion

		#region static information
		/// <summary>
		/// Statically get the <see cref="DataTypeClassification"/> of <see cref="ComplexDouble"/>
		/// </summary>
		public static DataTypeClassification Classification => DataTypeClassification.FloatPoint_IEEE754;

		/// <summary>
		/// Statically get the machine precision of <see cref="ComplexDouble"/>
		/// </summary>
		public static double MachinePrecision => 2.220446049250313E-16D;
		#endregion

		#region constant values
		/// <summary>
		/// <see cref="ComplexDouble"/> 0
		/// </summary>
		public static ComplexDouble Zero => new(0);
		/// <summary>
		/// <see cref="ComplexDouble"/> 1
		/// </summary>
		public static ComplexDouble One => new(1);
		/// <summary>
		/// <see cref="ComplexDouble"/> -1
		/// </summary>
		public static ComplexDouble NegativeOne => new(-1);
		/// <summary>
		/// <see cref="ComplexDouble"/> i
		/// </summary>
		public static ComplexDouble ImaginaryOne => new(0, 1);
		/// <summary>
		/// <see cref="ComplexDouble"/> -1
		/// </summary>
		public static ComplexDouble ImaginaryNegativeOne => new(0, -1);
		/// <summary>
		/// <see cref="ComplexDouble"/> 0
		/// </summary>
		public static ComplexDouble AdditiveIdentity => Zero;
		/// <summary>
		/// <see cref="ComplexDouble"/> 1
		/// </summary>
		public static ComplexDouble MultiplicativeIdentity => One;
		/// <summary>
		/// <see cref="ComplexDouble"/> infinity
		/// </summary>
		public static ComplexDouble Infinity => new(double.PositiveInfinity);
		/// <summary>
		/// <see cref="ComplexDouble"/> NaN
		/// </summary>
		public static ComplexDouble Nan => new(double.NaN);

		/// <summary>
		/// Statically check whether the given <paramref name="value"/> is finite or not
		/// </summary>
		/// <param name="value">The given number to check</param>
		/// <returns>Whether <paramref name="value"/> is finite or not</returns>
		public static bool IsFinite(ComplexDouble value) => double.IsFinite(value.real) && double.IsFinite(value.imag);

		/// <summary>
		/// Statically check whether the given <paramref name="value"/> is infinity or not
		/// </summary>
		/// <param name="value">The given number to check</param>
		/// <returns>Whether <paramref name="value"/> is infinity or not</returns>
		public static bool IsInfinity(ComplexDouble value) => double.IsInfinity(value.real) || double.IsInfinity(value.imag);

		/// <summary>
		/// Statically check whether the given <paramref name="value"/> is NaN or not
		/// </summary>
		/// <param name="value">The given number to check</param>
		/// <returns>Whether <paramref name="value"/> is NaN or not</returns>
		public static bool IsNan(ComplexDouble value) => double.IsNaN(value.real) && double.IsNaN(value.imag);
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

		internal static unsafe bool TryParseAny<T>(string str, delegate*<string, out T, bool> parseFunc, out T real, out T imag) where T : unmanaged
		{
			real = imag = default;

			Regex regex = new(regexPattern1);
			Match match = regex.Match(str);
			bool success = match.Success;
			if (!success)
				goto SecondTry;
			success = parseFunc(match.Groups[1].Value, out real);
			if (!success)
				goto SecondTry;
			string imagStr = match.Groups[2].Value.Replace(" ", "");
			if (imagStr.Length > 0)
			{
				imagStr = imagStr[..(imagStr.Length - 1)];
				success = parseFunc(imagStr, out imag);
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
			success = parseFunc(match.Groups[1].Value, out imag);
			if (!success)
				return false;
			success = parseFunc(match.Groups[2].Value, out real);
			if (!success)
				return false;
			else
				return true;
		}

		/// <summary>
		/// Try to parse a <see cref="string"/> to a new <see cref="ComplexDouble"/>
		/// </summary>
		/// <param name="s">string to parse of form "a + b<c>i</c>", "a - b<c>i</c>", "a", "b<c>i</c>" or "-b<c>i</c>" where both 'a' and 'b' are float point numbers</param>
		/// <param name="complex">output <see cref="ComplexDouble"/></param>
		/// <returns>success or not</returns>
		public unsafe static bool TryParse(string? s, out ComplexDouble complex)
		{
			complex = default;
			if (s is null || s.Length == 0)
				return false;
			bool success = TryParseAny(s, &double.TryParse, out double real, out double imag);
			if (success)
				complex = new(real, imag);
			return success;
		}

		/// <summary>
		/// Parse a <see cref="string"/> to a <see cref="ComplexDouble"/>
		/// </summary>
		/// <param name="str">The string to parse of form "a + b<c>i</c>", "a - b<c>i</c>", "a", "b<c>i</c>" or "-b<c>i</c>" where both 'a' and 'b' are float point numbers</param>
		/// <returns>The parsed <see cref="ComplexDouble"/></returns>
		public static ComplexDouble Parse(string? str)
		{
			if (str is null || str.Length == 0)
				throw new ArgumentNullException(nameof(str));
			bool success = TryParse(str, out ComplexDouble result);
			if (!success)
				throw new ArgumentException(string.Format(Other.CannotParseComplex, str, typeof(double).Name), nameof(str));
			return result;
		}

		/// <summary>
		/// See <see cref="Parse(string?)"/>
		/// </summary>
		public static ComplexDouble Parse(string? s, IFormatProvider? provider) => Parse(s);
		/// <summary>
		/// See <see cref="TryParse(string?, out ComplexDouble)"/>
		/// </summary>
		public static bool TryParse(string? s, IFormatProvider? provider, out ComplexDouble result) => TryParse(s, out result);
		#endregion

		#region converter
		/// <summary>
		/// Convert from int
		/// </summary>
		/// <param name="a">a int</param>
		public static implicit operator ComplexDouble(int a) => new(a);
		/// <summary>
		/// Convert from double
		/// </summary>
		/// <param name="a">a double</param>
		public static implicit operator ComplexDouble(double a) => new(a);
		/// <summary>
		/// Convert from int tuple
		/// </summary>
		/// <param name="a">a int tuple</param>
		public static implicit operator ComplexDouble((int r, int i) a) => new(a.r, a.i);
		/// <summary>
		/// Convert from double tuple
		/// </summary>
		/// <param name="a">a double tuple</param>
		public static implicit operator ComplexDouble((double real, double imag) a) => new(a.real, a.imag);

		/// <summary>
		/// Convert to <see cref="double"/> by taking abs
		/// </summary>
		public static explicit operator double(ComplexDouble v) => DoubleAbs(v);
		#endregion

		#region equality
		/// <summary>
		/// Equality operator
		/// </summary>
		public static bool operator ==(ComplexDouble a, ComplexDouble b) => a.Equals(b);

		/// <summary>
		/// Inequality operator
		/// </summary>
		public static bool operator !=(ComplexDouble a, ComplexDouble b) => !(a == b);

		/// <summary>
		/// Equality operator
		/// </summary>
		/// <param name="other">The other <see cref="ComplexDouble"/> to compare</param>
		/// <returns>this == <paramref name="other"/></returns>
		public bool Equals(ComplexDouble other)
		{
			return this.real.IsEqual(other.real) && this.imag.IsEqual(other.imag);
		}

		/// <summary>
		/// Override <see cref="object.GetHashCode"/>
		/// </summary>
		public override int GetHashCode()
		{
			return HashCode.Combine(this.real, this.imag);
		}

		/// <summary>
		/// Override <see cref="object.Equals(object)"/>
		/// </summary>
		public override bool Equals(object? obj)
		{
			ComplexDouble a;
			if (obj is int @int)
				a = @int;
			else if (obj is double single)
				a = single;
			else if (obj is ComplexDouble complex)
				a = complex;
			else
				return false;
			return this.Equals(a);
		}
		#endregion

		#region arithmetic operators
		/// <summary>
		/// Complex negate
		/// </summary>
		public static ComplexDouble operator -(ComplexDouble a) => new(-a.real, -a.imag);
		/// <summary>
		/// Complex add
		/// </summary>
		public static ComplexDouble operator +(ComplexDouble a, ComplexDouble b) => new(a.real + b.real, a.imag + b.imag);
		/// <summary>
		/// Complex subtract
		/// </summary>
		public static ComplexDouble operator -(ComplexDouble a, ComplexDouble b) => new(a.real - b.real, a.imag - b.imag);
		/// <summary>
		/// Complex add real
		/// </summary>
		public static ComplexDouble operator +(ComplexDouble a, double b) => new(a.real + b, a.imag);
		/// <summary>
		/// Complex add real
		/// </summary>
		public static ComplexDouble operator +(double b, ComplexDouble a) => new(a.real + b, a.imag);
		/// <summary>
		/// Complex subtract real
		/// </summary>
		public static ComplexDouble operator -(ComplexDouble a, double b) => new(a.real - b, a.imag);
		/// <summary>
		/// Real subtract complex
		/// </summary>
		public static ComplexDouble operator -(double b, ComplexDouble a) => new(b - a.real, -a.imag);

		/// <summary>
		/// Complex multiply
		/// </summary>
		public static unsafe ComplexDouble operator *(ComplexDouble a, ComplexDouble b) => DoubleMul(a, b);

		/// <summary>
		/// Complex division, guards against intermediate underflow and overflow by scaling
		/// </summary>
		public static unsafe ComplexDouble operator /(ComplexDouble a, ComplexDouble b) => DoubleDiv(a, b);

		/// <summary>
		/// Complex multiply real number
		/// </summary>
		public static ComplexDouble operator *(ComplexDouble a, double b) => new(a.real * b, a.imag *b);
		/// <summary>
		/// Complex multiply real number
		/// </summary>
		public static ComplexDouble operator *(double b, ComplexDouble a) => new(a.real * b, a.imag * b);
		/// <summary>
		/// Complex divide real number
		/// </summary>
		public static ComplexDouble operator /(ComplexDouble a, double b) => new(a.real / b, a.imag / b);

		/// <summary>
		/// Real number divide complex 
		/// </summary>
		public static unsafe ComplexDouble operator /(double a, ComplexDouble b) => DoubleRealDiv(a, b);

		/// <summary>
		/// Static unary plus operator for one operand <paramref name="value"/>
		/// </summary>
		/// <param name="value">The operand to be incremented of type <see cref="ComplexDouble"/></param>
		/// <returns>The unary plus result of type <see cref="ComplexDouble"/></returns>
		public static ComplexDouble operator +(ComplexDouble value) => value;

		/// <summary>
		/// Static unary decrement operator for one operand <paramref name="value"/>
		/// </summary>
		/// <param name="value">The operand to be incremented of type <see cref="ComplexDouble"/></param>
		/// <returns>The unary decrement result of type <see cref="ComplexDouble"/></returns>
		public static ComplexDouble operator --(ComplexDouble value)
		{
			double r = value.real;
			r--;
			return new(r, value.imag);
		}

		/// <summary>
		/// Static unary increment operator for one operand <paramref name="value"/>
		/// </summary>
		/// <param name="value">The operand to be incremented of type <see cref="ComplexDouble"/></param>
		/// <returns>The unary increment result of type <see cref="ComplexDouble"/></returns>
		public static ComplexDouble operator ++(ComplexDouble value)
		{
			double r = value.real;
			r++;
			return new(r, value.imag);
		}
		#endregion

		#region other arithmetics
		/// <summary>
		/// Get the magnitude or absolute value of this <see cref="ComplexDouble"/>
		/// </summary>
		public double Magnitude => DoubleAbs(this);

		/// <summary>
		/// Get the squared magnitude or absolute value of this <see cref="ComplexDouble"/>
		/// </summary>
		public double MagnitudeSquare => DoubleSquareAbs(this);

		/// <summary>
		/// Get the phase of this <see cref="ComplexDouble"/>
		/// </summary>
		public double Phase => DoubleArg(this);

		/// <summary>
		/// Complex conjugate
		/// </summary>
		public ComplexDouble Conj => new(this.real, -this.imag);

		/// <summary>
		/// Complex number absolute value
		/// </summary>
		public static ComplexDouble Abs(ComplexDouble number) => DoubleAbs(number);

		/// <summary>
		/// Complex exponential (of base <c>e</c>)
		/// </summary>
		public static ComplexDouble Exp(ComplexDouble number) => DoubleExp(number);
		/// <summary>
		/// Complex logarithm (of base <c>e</c>)
		/// </summary>
		public static ComplexDouble Log(ComplexDouble number) => DoubleLog(number);
		/// <summary>
		/// Complex number power of real type
		/// </summary>
		public static ComplexDouble Pow(ComplexDouble complex, double p) => DoublePow(complex, p);
		/// <summary>
		/// Complex number power of complex type
		/// </summary>
		public static ComplexDouble Pow(ComplexDouble number, ComplexDouble power) => DoublePow(number, power);
		/// <summary>
		/// Complex number reciprocal
		/// </summary>
		public static ComplexDouble Reciprocal(ComplexDouble number) => 1 / number;
		/// <summary>
		/// Complex number square root
		/// </summary>
		public static ComplexDouble Sqrt(ComplexDouble number) => DoubleSqrt(number);
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
		#endregion

		#region complex double arithmetic
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
		#endregion
	}

	/// <summary>
	/// The single precision float complex type
	/// </summary>
	/// <remarks>This is a separate struct since the native <see cref="float"/> has not implemented <see cref="IFloatNumber{TSelf}"/> yet</remarks>
	[StructLayout(LayoutKind.Sequential)]
	public struct ComplexSingle : IComplexNumber<ComplexSingle, float>, ICustomNativeType<ComplexSingle>
	{
		#region basic
		private readonly float real, imag;

		/// <summary>
		/// Get the real part
		/// </summary>
		public float Real
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.real;
		}

		/// <summary>
		/// Get the imaginary part
		/// </summary>
		public float Imaginary
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.imag;
		}

		/// <summary>
		/// Construct a <see cref="ComplexSingle"/> from real and imaginary parts
		/// </summary>
		/// <param name="re">The real part</param>
		/// <param name="im">The imaginary part, default value is 0</param>
		public ComplexSingle(float re, float im = default)
		{
			this.real = re;
			this.imag = im;
		}
		#endregion

		#region static information
		/// <summary>
		/// Statically get the <see cref="DataTypeClassification"/> of <see cref="ComplexSingle"/>
		/// </summary>
		public static DataTypeClassification Classification => DataTypeClassification.FloatPoint_IEEE754;

		/// <summary>
		/// Statically get the machine precision of <see cref="ComplexSingle"/>
		/// </summary>
		public static double MachinePrecision => 1.1920928955078125E-07D;
		#endregion

		#region constant values
		/// <summary>
		/// <see cref="ComplexSingle"/> 0
		/// </summary>
		public static ComplexSingle Zero => new(0);
		/// <summary>
		/// <see cref="ComplexSingle"/> 1
		/// </summary>
		public static ComplexSingle One => new(1);
		/// <summary>
		/// <see cref="ComplexSingle"/> -1
		/// </summary>
		public static ComplexSingle NegativeOne => new(-1);
		/// <summary>
		/// <see cref="ComplexSingle"/> i
		/// </summary>
		public static ComplexSingle ImaginaryOne => new(0, 1);
		/// <summary>
		/// <see cref="ComplexSingle"/> -1
		/// </summary>
		public static ComplexSingle ImaginaryNegativeOne => new(0, -1);
		/// <summary>
		/// <see cref="ComplexSingle"/> 0
		/// </summary>
		public static ComplexSingle AdditiveIdentity => Zero;
		/// <summary>
		/// <see cref="ComplexSingle"/> 1
		/// </summary>
		public static ComplexSingle MultiplicativeIdentity => One;
		/// <summary>
		/// <see cref="ComplexSingle"/> infinity
		/// </summary>
		public static ComplexSingle Infinity => new(float.PositiveInfinity);
		/// <summary>
		/// <see cref="ComplexSingle"/> NaN
		/// </summary>
		public static ComplexSingle Nan => new(float.NaN);

		/// <summary>
		/// Statically check whether the given <paramref name="value"/> is finite or not
		/// </summary>
		/// <param name="value">The given number to check</param>
		/// <returns>Whether <paramref name="value"/> is finite or not</returns>
		public static bool IsFinite(ComplexSingle value) => float.IsFinite(value.real) && float.IsFinite(value.imag);

		/// <summary>
		/// Statically check whether the given <paramref name="value"/> is infinity or not
		/// </summary>
		/// <param name="value">The given number to check</param>
		/// <returns>Whether <paramref name="value"/> is infinity or not</returns>
		public static bool IsInfinity(ComplexSingle value) => float.IsInfinity(value.real) || float.IsInfinity(value.imag);

		/// <summary>
		/// Statically check whether the given <paramref name="value"/> is NaN or not
		/// </summary>
		/// <param name="value">The given number to check</param>
		/// <returns>Whether <paramref name="value"/> is NaN or not</returns>
		public static bool IsNan(ComplexSingle value) => float.IsNaN(value.real) && float.IsNaN(value.imag);
		#endregion

		#region parser
		/// <summary>
		/// Try to parse a <see cref="string"/> to a new <see cref="ComplexSingle"/>
		/// </summary>
		/// <param name="s">string to parse of form "a + b<c>i</c>", "a - b<c>i</c>", "a", "b<c>i</c>" or "-b<c>i</c>" where both 'a' and 'b' are float point numbers</param>
		/// <param name="complex">output <see cref="ComplexSingle"/></param>
		/// <returns>success or not</returns>
		public unsafe static bool TryParse(string? s, out ComplexSingle complex)
		{
			complex = default;
			if (s is null || s.Length == 0)
				return false;
			bool success = ComplexDouble.TryParseAny(s, &double.TryParse, out double real, out double imag);
			if (success)
				complex = new((float)real, (float)imag);
			return success;
		}

		/// <summary>
		/// Parse a <see cref="string"/> to a <see cref="ComplexSingle"/>
		/// </summary>
		/// <param name="str">The string to parse of form "a + b<c>i</c>", "a - b<c>i</c>", "a", "b<c>i</c>" or "-b<c>i</c>" where both 'a' and 'b' are float point numbers</param>
		/// <returns>The parsed <see cref="ComplexSingle"/></returns>
		public static ComplexSingle Parse(string? str)
		{
			if (str is null || str.Length == 0)
				throw new ArgumentNullException(nameof(str));
			bool success = TryParse(str, out ComplexSingle result);
			if (!success)
				throw new ArgumentException(string.Format(Other.CannotParseComplex, str, typeof(float).Name), nameof(str));
			return result;
		}

		/// <summary>
		/// See <see cref="Parse(string?)"/>
		/// </summary>
		public static ComplexSingle Parse(string? s, IFormatProvider? provider) => Parse(s);
		/// <summary>
		/// See <see cref="TryParse(string?, out ComplexSingle)"/>
		/// </summary>
		public static bool TryParse(string? s, IFormatProvider? provider, out ComplexSingle result) => TryParse(s, out result);
		#endregion

		#region converter
		/// <summary>
		/// Convert from int
		/// </summary>
		/// <param name="a">a int</param>
		public static implicit operator ComplexSingle(int a) => new(a);
		/// <summary>
		/// Convert from float
		/// </summary>
		/// <param name="a">a float</param>
		public static implicit operator ComplexSingle(float a) => new(a);
		/// <summary>
		/// Convert from int tuple
		/// </summary>
		/// <param name="a">a int tuple</param>
		public static implicit operator ComplexSingle((int r, int i) a) => new(a.r, a.i);
		/// <summary>
		/// Convert from float tuple
		/// </summary>
		/// <param name="a">a float tuple</param>
		public static implicit operator ComplexSingle((float real, float imag) a) => new(a.real, a.imag);

		/// <summary>
		/// Convert to <see cref="float"/> by taking abs
		/// </summary>
		public static explicit operator float(ComplexSingle v) => SingleAbs(v);
		#endregion

		#region equality
		/// <summary>
		/// Equality operator
		/// </summary>
		public static bool operator ==(ComplexSingle a, ComplexSingle b) => a.Equals(b);

		/// <summary>
		/// Inequality operator
		/// </summary>
		public static bool operator !=(ComplexSingle a, ComplexSingle b) => !(a == b);

		/// <summary>
		/// Equality operator
		/// </summary>
		/// <param name="other">The other <see cref="ComplexSingle"/> to compare</param>
		/// <returns>this == <paramref name="other"/></returns>
		public bool Equals(ComplexSingle other)
		{
			return this.real.IsEqual(other.real) && this.imag.IsEqual(other.imag);
		}

		/// <summary>
		/// Override <see cref="object.GetHashCode"/>
		/// </summary>
		public override int GetHashCode()
		{
			return HashCode.Combine(this.real, this.imag);
		}

		/// <summary>
		/// Override <see cref="object.Equals(object)"/>
		/// </summary>
		public override bool Equals(object? obj)
		{
			ComplexSingle a;
			if (obj is int @int)
				a = @int;
			else if (obj is float single)
				a = single;
			else if (obj is ComplexSingle complex)
				a = complex;
			else
				return false;
			return this.Equals(a);
		}
		#endregion

		#region arithmetic operators
		/// <summary>
		/// Complex negate
		/// </summary>
		public static ComplexSingle operator -(ComplexSingle a) => new(-a.real, -a.imag);
		/// <summary>
		/// Complex add
		/// </summary>
		public static ComplexSingle operator +(ComplexSingle a, ComplexSingle b) => new(a.real + b.real, a.imag + b.imag);
		/// <summary>
		/// Complex subtract
		/// </summary>
		public static ComplexSingle operator -(ComplexSingle a, ComplexSingle b) => new(a.real - b.real, a.imag - b.imag);
		/// <summary>
		/// Complex add real
		/// </summary>
		public static ComplexSingle operator +(ComplexSingle a, float b) => new(a.real + b, a.imag);
		/// <summary>
		/// Complex add real
		/// </summary>
		public static ComplexSingle operator +(float b, ComplexSingle a) => new(a.real + b, a.imag);
		/// <summary>
		/// Complex subtract real
		/// </summary>
		public static ComplexSingle operator -(ComplexSingle a, float b) => new(a.real - b, a.imag);
		/// <summary>
		/// Real subtract complex
		/// </summary>
		public static ComplexSingle operator -(float b, ComplexSingle a) => new(b - a.real, -a.imag);

		/// <summary>
		/// Complex multiply
		/// </summary>
		public static unsafe ComplexSingle operator *(ComplexSingle a, ComplexSingle b) => SingleMul(a, b);

		/// <summary>
		/// Complex division, guards against intermediate underflow and overflow by scaling
		/// </summary>
		public static unsafe ComplexSingle operator /(ComplexSingle a, ComplexSingle b) => SingleDiv(a, b);

		/// <summary>
		/// Complex multiply real number
		/// </summary>
		public static ComplexSingle operator *(ComplexSingle a, float b) => new(a.real * b, a.imag * b);
		/// <summary>
		/// Complex multiply real number
		/// </summary>
		public static ComplexSingle operator *(float b, ComplexSingle a) => new(a.real * b, a.imag * b);
		/// <summary>
		/// Complex divide real number
		/// </summary>
		public static ComplexSingle operator /(ComplexSingle a, float b) => new(a.real / b, a.imag / b);

		/// <summary>
		/// Real number divide complex 
		/// </summary>
		public static unsafe ComplexSingle operator /(float a, ComplexSingle b) => SingleRealDiv(a, b);

		/// <summary>
		/// Static unary plus operator for one operand <paramref name="value"/>
		/// </summary>
		/// <param name="value">The operand to be incremented of type <see cref="ComplexSingle"/></param>
		/// <returns>The unary plus result of type <see cref="ComplexSingle"/></returns>
		public static ComplexSingle operator +(ComplexSingle value) => value;

		/// <summary>
		/// Static unary decrement operator for one operand <paramref name="value"/>
		/// </summary>
		/// <param name="value">The operand to be incremented of type <see cref="ComplexSingle"/></param>
		/// <returns>The unary decrement result of type <see cref="ComplexSingle"/></returns>
		public static ComplexSingle operator --(ComplexSingle value)
		{
			float r = value.real;
			r--;
			return new(r, value.imag);
		}

		/// <summary>
		/// Static unary increment operator for one operand <paramref name="value"/>
		/// </summary>
		/// <param name="value">The operand to be incremented of type <see cref="ComplexSingle"/></param>
		/// <returns>The unary increment result of type <see cref="ComplexSingle"/></returns>
		public static ComplexSingle operator ++(ComplexSingle value)
		{
			float r = value.real;
			r++;
			return new(r, value.imag);
		}
		#endregion

		#region other arithmetics
		/// <summary>
		/// Get the magnitude or absolute value of this <see cref="ComplexSingle"/>
		/// </summary>
		public float Magnitude => SingleAbs(this);

		/// <summary>
		/// Get the squared magnitude or absolute value of this <see cref="ComplexSingle"/>
		/// </summary>
		public float MagnitudeSquare => SingleSquareAbs(this);

		/// <summary>
		/// Get the phase of this <see cref="ComplexSingle"/>
		/// </summary>
		public float Phase => SingleArg(this);

		/// <summary>
		/// Complex conjugate
		/// </summary>
		public ComplexSingle Conj => new(this.real, -this.imag);

		/// <summary>
		/// Complex number absolute value
		/// </summary>
		public static ComplexSingle Abs(ComplexSingle number) => SingleAbs(number);

		/// <summary>
		/// Complex exponential (of base <c>e</c>)
		/// </summary>
		public static ComplexSingle Exp(ComplexSingle number) => SingleExp(number);
		/// <summary>
		/// Complex logarithm (of base <c>e</c>)
		/// </summary>
		public static ComplexSingle Log(ComplexSingle number) => SingleLog(number);
		/// <summary>
		/// Complex number power of real type
		/// </summary>
		public static ComplexSingle Pow(ComplexSingle complex, float p) => SinglePow(complex, p);
		/// <summary>
		/// Complex number power of complex type
		/// </summary>
		public static ComplexSingle Pow(ComplexSingle number, ComplexSingle power) => SinglePow(number, power);
		/// <summary>
		/// Complex number reciprocal
		/// </summary>
		public static ComplexSingle Reciprocal(ComplexSingle number) => 1 / number;
		/// <summary>
		/// Complex number square root
		/// </summary>
		public static ComplexSingle Sqrt(ComplexSingle number) => SingleSqrt(number);
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
		#endregion

		#region complex float arithmetic
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ComplexSingle SingleMul(ComplexSingle a, float b)
		{
			return new ComplexSingle(a.real * b, a.imag * b);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ComplexSingle SingleMul(ComplexSingle a, ComplexSingle b)
		{
			float real = MathF.FusedMultiplyAdd(a.real, b.real, -a.imag * b.imag); // vfmsub213sd
			float imag = MathF.FusedMultiplyAdd(a.real, b.imag, a.imag * b.real); // vfmadd213sd
			return new ComplexSingle(real, imag);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ComplexSingle SingleDiv(ComplexSingle x, ComplexSingle y)
		{
			float squareAbsY = SingleSquareAbs(y);
			float acbd = MathF.FusedMultiplyAdd(x.real, y.real, x.imag * y.imag); // vfmadd213sd
			float bcad = MathF.FusedMultiplyAdd(x.imag, y.real, -x.real * y.imag); // vfmsub213sd
			return new ComplexSingle(acbd / squareAbsY, bcad / squareAbsY);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ComplexSingle SingleRealDiv(float x, ComplexSingle y)
		{
			float squareAbsY = SingleSquareAbs(y);
			return new ComplexSingle(x * y.real / squareAbsY, -x * y.imag / squareAbsY);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static float SingleSquareAbs(ComplexSingle a)
		{
			float squareAbs = MathF.FusedMultiplyAdd(a.real, a.real, a.imag * a.imag); // vfmadd213sd
			return squareAbs;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ComplexSingle SingleAddSquareAbs(ComplexSingle a, ComplexSingle b)
		{
			// a.r += b.r*b.r + b.i*b.i
			float real = MathF.FusedMultiplyAdd(b.real, b.real, a.real);
			real = MathF.FusedMultiplyAdd(b.imag, b.imag, real);
			// totally 2 FMA (naive is 1*FMA + 1*MUL + 1*ADD)
			return new(real, a.imag);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ComplexSingle SingleMulConjA(ComplexSingle a, ComplexSingle b)
		{
			float real = MathF.FusedMultiplyAdd(a.real, b.real, a.imag * b.imag); // vfmadd213sd
			float imag = MathF.FusedMultiplyAdd(a.real, b.imag, -a.imag * b.real); // vfmsub213sd
			return new ComplexSingle(real, imag);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ComplexSingle SingleFMA(ComplexSingle a, ComplexSingle b, ComplexSingle c)
		{
			// a.r * b.r - (a.i * b.i - c.r)
			float temp1 = MathF.FusedMultiplyAdd(a.imag, b.imag, -c.real);
			float real = MathF.FusedMultiplyAdd(a.real, b.real, -temp1);
			// a.r * b.i + (a.i * b.r + c.i)
			float temp2 = MathF.FusedMultiplyAdd(a.imag, b.real, c.real);
			float imag = MathF.FusedMultiplyAdd(a.real, b.imag, temp2);
			// totally 4 FMA (naive is 2*FMA + 2*MUL + 2*ADD)
			return new ComplexSingle(real, imag);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ComplexSingle SingleFMAConjA(ComplexSingle a, ComplexSingle b, ComplexSingle c)
		{
			// a.r * b.r + (a.i * b.i + c.r)
			float temp1 = MathF.FusedMultiplyAdd(a.imag, b.imag, c.real);
			float real = MathF.FusedMultiplyAdd(a.real, b.real, temp1);
			// a.r * b.i - (a.i * b.r - c.i)
			float temp2 = MathF.FusedMultiplyAdd(a.imag, b.real, -c.real);
			float imag = MathF.FusedMultiplyAdd(a.real, b.imag, -temp2);
			// totally 4 FMA (naive is 2*FMA + 2*MUL + 2*ADD)
			return new ComplexSingle(real, imag);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ComplexSingle SingleFMS(ComplexSingle a, ComplexSingle b, ComplexSingle c)
		{
			// a.r * b.r - (a.i * b.i + c.r)
			float temp1 = MathF.FusedMultiplyAdd(a.imag, b.imag, c.real);
			float real = MathF.FusedMultiplyAdd(a.real, b.real, -temp1);
			// a.r * b.i + (a.i * b.r - c.i)
			float temp2 = MathF.FusedMultiplyAdd(a.imag, b.real, -c.real);
			float imag = MathF.FusedMultiplyAdd(a.real, b.imag, temp2);
			return new ComplexSingle(real, imag);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ComplexSingle SingleFMSConjA(ComplexSingle a, ComplexSingle b, ComplexSingle c)
		{
			// a.r * b.r + (a.i * b.i - c.r)
			float temp1 = MathF.FusedMultiplyAdd(a.imag, b.imag, -c.real);
			float real = MathF.FusedMultiplyAdd(a.real, b.real, temp1);
			// a.r * b.i - (a.i * b.r + c.i)
			float temp2 = MathF.FusedMultiplyAdd(a.imag, b.real, c.real);
			float imag = MathF.FusedMultiplyAdd(a.real, b.imag, -temp2);
			// totally 4 FMA (naive is 2*FMA + 2*MUL + 2*ADD)
			return new ComplexSingle(real, imag);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static float SingleAbs(ComplexSingle a)
		{
			return MathF.Sqrt(SingleSquareAbs(a));
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static float SingleArg(ComplexSingle a)
		{
			return MathF.Atan2(a.imag, a.real);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ComplexSingle SingleExp(ComplexSingle c)
		{
			float exp = MathF.Exp(c.real);
			float cos = MathF.Cos(c.imag);
			float sin = MathF.Sin(c.imag);
			return new ComplexSingle(exp * cos, exp * sin);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ComplexSingle SingleLog(ComplexSingle c)
		{
			float real = 0.5F * MathF.Log(SingleAbs(c));
			float imag = MathF.Atan2(c.imag, c.real);
			return new ComplexSingle(real, imag);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ComplexSingle SinglePowReal(ComplexSingle c, float p)
		{
			if (c.imag == 0)
			{
				return new ComplexSingle(MathF.Pow(c.real, p));
			}
			else
			{
				float absC = SingleAbs(c);
				float argC = MathF.Atan2(c.imag, c.real);
				float phase = p * argC;
				float scale = MathF.Pow(absC, p);
				return new ComplexSingle(scale * MathF.Cos(phase), scale * MathF.Sin(phase));
			}
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ComplexSingle SinglePow(ComplexSingle c, float p)
		{
			if ((c.real == 0 || c.real == 1) && c.imag == 0)
				return c;
			return SinglePowReal(c, p);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ComplexSingle SinglePow(ComplexSingle c, ComplexSingle p)
		{
			if ((c.real == 0 || c.real == 1) && c.imag == 0)
				return c;
			if (p.imag == 0)
			{
				return SinglePowReal(c, p.real);
			}
			// else
			float absC = SingleAbs(c);
			float argC = MathF.Atan2(c.imag, c.real);
			float phase = p.real * argC + p.imag * MathF.Log(absC);
			float scale = MathF.Pow(absC, p.real) * MathF.Exp(-p.imag * argC);
			return new ComplexSingle(scale * MathF.Cos(phase), scale * MathF.Sin(phase));
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ComplexSingle SingleSqrt(ComplexSingle c)
		{
			float arg = 0.5F * SingleArg(c);
			float scale = MathF.Sqrt(SingleAbs(c));
			float real = MathF.Cos(arg);
			float imag = MathF.Sin(arg);
			return new ComplexSingle(scale * real, scale * imag);
		}
		#endregion
	}
	#endregion


	#region generic complex type
	/// <summary>
	/// The general complex type for any real numeric number type including <see cref="float"/> and <see cref="double"/>
	/// </summary>
	/// <typeparam name="T">The data type of corresponding real number</typeparam>
	[StructLayout(LayoutKind.Sequential)]
	public struct Complex<T> : IComplexNumber<Complex<T>, T>, ICustomNativeType<Complex<T>>
		where T : unmanaged, IFloatNumber<T>, INativeConvertibleNumber<T>, ICustomNativeType<T>
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
		public static DataTypeClassification Classification => T.Classification;

		/// <summary>
		/// Statically get the machine precision of <see cref="Complex{T}"/>
		/// </summary>
		public static double MachinePrecision => T.MachinePrecision;

		static Complex()
		{
			// generic type check
			if (typeof(T).IsGenericType)
				throw new InvalidOperationException(Support.DataType);
			// native type check
			if (T.Classification < DataTypeClassification.FloatPoint_IEEE754)
				throw new InvalidOperationException(Support.DataType);
		}
		#endregion

		#region constant values
		/// <summary>
		/// <see cref="Complex{T}"/> 0
		/// </summary>
		public static Complex<T> Zero => new(T.Zero);
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
		/// <summary>
		/// <see cref="Complex{T}"/> 0
		/// </summary>
		public static Complex<T> AdditiveIdentity => Zero;
		/// <summary>
		/// <see cref="Complex{T}"/> 1
		/// </summary>
		public static Complex<T> MultiplicativeIdentity => One;
		/// <summary>
		/// <see cref="Complex{T}"/> infinity
		/// </summary>
		public static Complex<T> Infinity => new(T.Infinity);
		/// <summary>
		/// <see cref="Complex{T}"/> NaN
		/// </summary>
		public static Complex<T> Nan => new(T.Nan);

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
		public static bool IsNan(Complex<T> value) => T.IsNan(value.real) && T.IsNan(value.imag);
		#endregion

		#region parser
		/// <summary>
		/// Try to parse a <see cref="string"/> to a new <see cref="Complex{T}"/>
		/// </summary>
		/// <param name="s">string to parse of form "a + b<c>i</c>", "a - b<c>i</c>", "a", "b<c>i</c>" or "-b<c>i</c>" where both 'a' and 'b' are float point numbers</param>
		/// <param name="complex">output <see cref="Complex{T}"/></param>
		/// <returns>success or not</returns>
		public unsafe static bool TryParse(string? s, out Complex<T> complex)
		{
			complex = default;
			if (s is null || s.Length == 0)
				return false;
			bool success = ComplexDouble.TryParseAny(s, &NativeTypeExtension.TryParseNativeType, out T real, out T imag);
			if (success)
				complex = new(real, imag);
			return success;
		}

		/// <summary>
		/// Parse a <see cref="string"/> to a <see cref="Complex{T}"/>
		/// </summary>
		/// <param name="str">The string to parse of form "a + b<c>i</c>", "a - b<c>i</c>", "a", "b<c>i</c>" or "-b<c>i</c>" where both 'a' and 'b' are float point numbers</param>
		/// <returns>The parsed <see cref="Complex{T}"/></returns>
		public static Complex<T> Parse(string? str)
		{
			if (str is null || str.Length == 0)
				throw new ArgumentNullException(nameof(str));
			bool success = TryParse(str, out Complex<T> result);
			if (!success)
				throw new ArgumentException(string.Format(Other.CannotParseComplex, str, typeof(T).Name), nameof(str));
			return result;
		}

		/// <summary>
		/// See <see cref="Parse(string?)"/>
		/// </summary>
		public static Complex<T> Parse(string? s, IFormatProvider? provider) => Parse(s);
		/// <summary>
		/// See <see cref="TryParse(string?, out Complex{T})"/>
		/// </summary>
		public static bool TryParse(string? s, IFormatProvider? provider, out Complex<T> result) => TryParse(s, out result);
		#endregion

		#region converter
		/// <summary>
		/// Convert from int
		/// </summary>
		/// <param name="a">a int</param>
		public static implicit operator Complex<T>(int a) => new((T)a);
		/// <summary>
		/// Convert from T
		/// </summary>
		/// <param name="a">a T</param>
		public static implicit operator Complex<T>(T a) => new(a);
		/// <summary>
		/// Convert from int tuple
		/// </summary>
		/// <param name="a">a int tuple</param>
		public static implicit operator Complex<T>((int r, int i) a) => new((T)a.r, (T)a.i);
		/// <summary>
		/// Convert from T tuple
		/// </summary>
		/// <param name="a">a T tuple</param>
		public static implicit operator Complex<T>((T real, T imag) a) => new(a.real, a.imag);

		/// <summary>
		/// Convert to <see cref="ComplexDouble"/>
		/// </summary>
		public static unsafe explicit operator ComplexDouble(Complex<T> v)
		{
			if (typeof(T) == typeof(double))
				return *(ComplexDouble*)&v;
			else
				return new((double)v.real, (double)v.imag);
		}

		/// <summary>
		/// Convert from <see cref="ComplexDouble"/>
		/// </summary>
		public static unsafe explicit operator Complex<T>(ComplexDouble v)
		{
			if (typeof(T) == typeof(double))
				return *(Complex<T>*)&v;
			else
				return new((T)v.Real, (T)v.Imaginary);
		}

		/// <summary>
		/// Convert to <typeparamref name="T"/> by taking abs
		/// </summary>
		public static explicit operator T(Complex<T> v) => v.Magnitude;
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
		public bool Equals(Complex<T> other)
		{
			return this.real == other.real && this.imag == other.imag;
		}

		/// <summary>
		/// Override <see cref="object.GetHashCode"/>
		/// </summary>
		public override int GetHashCode()
		{
			return HashCode.Combine(this.real, this.imag);
		}

		/// <summary>
		/// Override <see cref="object.Equals(object)"/>
		/// </summary>
		public override bool Equals(object? obj)
		{
			Complex<T> a;
			if (obj is int @int)
				a = @int;
			else if (obj is T single)
				a = single;
			else if (obj is Complex<T> complex)
				a = complex;
			else
				return false;
			return this.Equals(a);
		}
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
		public static unsafe Complex<T> operator *(Complex<T> a, Complex<T> b)
		{
			if (typeof(T) == typeof(float))
			{
				ComplexSingle v = (*(ComplexSingle*)&a) * (*(ComplexSingle*)&b);
				return *(Complex<T>*)&v;
			}
			if (typeof(T) == typeof(double))
			{
				ComplexDouble v = (*(ComplexDouble*)&a) * (*(ComplexDouble*)&b);
				return *(Complex<T>*)&v;
			}
			// else
			T real = a.real * b.real;
			T temp = a.imag * b.imag;
			real -= temp;
			T imag = a.real * b.imag;
			temp = a.imag * b.real;
			imag -= temp;
			return new Complex<T>(real, imag);
		}

		/// <summary>
		/// Complex division, guards against intermediate underflow and overflow by scaling
		/// </summary>
		public static unsafe Complex<T> operator /(Complex<T> a, Complex<T> b)
		{
			if (typeof(T) == typeof(float))
			{
				ComplexSingle v = (*(ComplexSingle*)&a) / (*(ComplexSingle*)&b);
				return *(Complex<T>*)&v;
			}
			if (typeof(T) == typeof(double))
			{
				ComplexDouble v = (*(ComplexDouble*)&a) * (*(ComplexDouble*)&b);
				return *(Complex<T>*)&v;
			}
			// else
			T squareAbsY = b.MagnitudeSquare;
			T acbd = a.real * b.real + a.imag * b.imag;
			T bcad = a.imag * b.real - a.real * b.imag;
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
			if (typeof(T) == typeof(float))
			{
				ComplexSingle v = (*(float*)&a) / (*(ComplexSingle*)&b);
				return *(Complex<T>*)&v;
			}
			if (typeof(T) == typeof(double))
			{
				ComplexDouble v = (*(double*)&a) * (*(ComplexDouble*)&b);
				return *(Complex<T>*)&v;
			}
			// else
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
		public T Magnitude => (T)((ComplexDouble)this).Magnitude;

		/// <summary>
		/// Get the squared magnitude or absolute value of this <see cref="Complex{T}"/>
		/// </summary>
		public T MagnitudeSquare => (T)((ComplexDouble)this).MagnitudeSquare;

		/// <summary>
		/// Get the phase of this <see cref="Complex{T}"/>
		/// </summary>
		public T Phase => (T)((ComplexDouble)this).Phase;

		/// <summary>
		/// Complex conjugate
		/// </summary>
		public Complex<T> Conj=> new(this.real, this.imag.NativeNegate());

		/// <summary>
		/// Complex number absolute value
		/// </summary>
		public static Complex<T> Abs(Complex<T> number) => (T)((ComplexDouble)number).Magnitude;

		/// <summary>
		/// Complex exponential (of base <c>e</c>)
		/// </summary>
		public static Complex<T> Exp(Complex<T> number) => (Complex<T>)ComplexDouble.Exp((ComplexDouble)number);
		/// <summary>
		/// Complex logarithm (of base <c>e</c>)
		/// </summary>
		public static Complex<T> Log(Complex<T> number) => (Complex<T>)ComplexDouble.Log((ComplexDouble)number);
		/// <summary>
		/// Complex number power of real type
		/// </summary>
		public static Complex<T> Pow(Complex<T> complex, T p) => (Complex<T>)ComplexDouble.Pow((ComplexDouble)complex, (double)p);
		/// <summary>
		/// Complex number power of complex type
		/// </summary>
		public static Complex<T> Pow(Complex<T> number, Complex<T> power) => (Complex<T>)ComplexDouble.Pow((ComplexDouble)number, (ComplexDouble)power);
		/// <summary>
		/// Complex number reciprocal
		/// </summary>
		public static Complex<T> Reciprocal(Complex<T> number) => T.One / number;
		/// <summary>
		/// Complex number square root
		/// </summary>
		public static Complex<T> Sqrt(Complex<T> number) => (Complex<T>)ComplexDouble.Sqrt((ComplexDouble)number);
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
		#endregion
	}
	#endregion
}