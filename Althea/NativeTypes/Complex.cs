using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

using Althea.Resources;


namespace Althea.NativeTypes
{
	#region complex interface
	/// <summary>
	/// The complex interface for any possible real data type
	/// </summary>
	/// <typeparam name="T">The data type of corresponding real number, usually a primitive type or an unmanaged struct that implements <see cref="ICustomNativeType{T}"/></typeparam>
	public interface IComplex<T> : IFormattable where T : unmanaged
	{
		/// <summary>
		/// Get the real part of this complex
		/// </summary>
		T Real { get; }

		/// <summary>
		/// Get the imaginary part of this complex
		/// </summary>
		T Imag { get; }

		/// <summary>
		/// Compute the absolute value of this complex
		/// </summary>
		/// <returns>The absolute value of this complex as a <typeparamref name="T"/></returns>
		T Abs();

		/// <summary>
		/// Compute the argument of this complex
		/// </summary>
		/// <returns>The argument of this complex as a <typeparamref name="T"/></returns>
		T Arg();
	}
	#endregion


	#region single and double complex types
	/// <summary>
	/// The double precision float complex type
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct ComplexDouble : IComplex<double>, ICustomNativeType<ComplexDouble>, IEquatable<ComplexDouble>
	{
		#region basic
		private readonly double real, imag;

		/// <summary>
		/// Get the real part
		/// </summary>
		public double Real {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.real;
		}

		/// <summary>
		/// Get the imaginary part
		/// </summary>
		public double Imag {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.imag;
		}

		/// <summary>
		/// Construct a <see cref="ComplexDouble"/> from real and imaginary parts
		/// </summary>
		/// <param name="re">The real part</param>
		/// <param name="im">The imaginary part, default value is 0</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ComplexDouble(double re, double im = 0)
		{
			this.real = re;
			this.imag = im;
		}
		#endregion

		#region static information
		DataTypeClassification ICustomNativeType<ComplexDouble>.Classification_Internal() => Const<double>.DataTypeClass;

		double ICustomNativeType<ComplexDouble>.MachinePrecision_Internal() => Const<double>.MachinePrecision;
		#endregion

		#region constant values
		/// <summary>
		/// <see cref="ComplexDouble"/> 0
		/// </summary>
		public static readonly ComplexDouble Zero = new(0);
		/// <summary>
		/// <see cref="ComplexDouble"/> 1
		/// </summary>
		public static readonly ComplexDouble One = new(1);
		/// <summary>
		/// <see cref="ComplexDouble"/> -1
		/// </summary>
		public static readonly ComplexDouble MinusOne = new(-1);
		/// <summary>
		/// <see cref="ComplexDouble"/> i
		/// </summary>
		public static readonly ComplexDouble ImOne = new(0, 1);
		/// <summary>
		/// <see cref="ComplexDouble"/> -1
		/// </summary>
		public static readonly ComplexDouble MinusImOne = new(0, -1);
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

		unsafe bool ICustomNativeType<ComplexDouble>.TryParse_Internal(string str, out ComplexDouble result)
		{
			bool success = TryParseAny(str, &double.TryParse, out double real, out double imag);
			result = new(real, imag);
			return success;
		}

		/// <summary>
		/// Try to parse a <see cref="string"/> to a new <see cref="ComplexDouble"/>
		/// </summary>
		/// <param name="s">The string to parse of form "a + b<c>i</c>", "a - b<c>i</c>", "a", "b<c>i</c>" or "-b<c>i</c>" where both 'a' and 'b' are float point numbers</param>
		/// <param name="complex">The output <see cref="ComplexDouble"/></param>
		/// <returns>success or not</returns>
		public unsafe static bool TryParse(string s, out ComplexDouble complex)
		{
			complex = default;
			if (s is null || s.Length == 0)
				return false;
			bool success = TryParseAny(s, &double.TryParse, out double real, out double imag);
			complex = new(real, imag);
			return success;
		}

		/// <summary>
		/// Parse a <see cref="string"/> to a <see cref="ComplexDouble"/>
		/// </summary>
		/// <param name="str">The string to parse of form "a + b<c>i</c>", "a - b<c>i</c>", "a", "b<c>i</c>" or "-b<c>i</c>" where both 'a' and 'b' are float point numbers</param>
		/// <returns>The parsed <see cref="ComplexDouble"/></returns>
		public static ComplexDouble Parse(string str)
		{
			if (str is null || str.Length == 0)
				throw new ArgumentNullException(nameof(str));
			bool success = TryParse(str, out ComplexDouble result);
			if (!success)
				throw new ArgumentException(string.Format(Other.CannotParseComplex, str, typeof(double).Name), nameof(str));
			return result;
		}
		#endregion

		#region converter
		/// <summary>
		/// Implicitly convert a int to a <see cref="ComplexDouble"/>
		/// </summary>
		/// <param name="a">a int</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator ComplexDouble(int a) => new(a);
		/// <summary>
		/// Implicitly convert a double to a <see cref="ComplexDouble"/>
		/// </summary>
		/// <param name="a">a double</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator ComplexDouble(double a) => new(a);
		/// <summary>
		/// Implicitly convert a int tuple to a <see cref="ComplexDouble"/>
		/// </summary>
		/// <param name="a">a int tuple</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator ComplexDouble((int r, int i) a) => new(a.r, a.i);
		/// <summary>
		/// Implicitly convert a double tuple to a <see cref="ComplexDouble"/>
		/// </summary>
		/// <param name="a">a double tuple</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator ComplexDouble((double r, double i) a) => new(a.r, a.i);

		/// <summary>
		/// Explicitly convert a <see cref="ComplexDouble"/> to a <see cref="double"/> by getting its absolute value
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static explicit operator double(ComplexDouble v) => v.Abs();

		/// <summary>
		/// Explicitly convert a <see cref="ComplexDouble"/> to a <see cref="ComplexSingle"/>
		/// </summary>
		/// <param name="a">The input <see cref="ComplexDouble"/> to be converted</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static explicit operator ComplexSingle(ComplexDouble a) => new((float)a.real, (float)a.imag);
		#endregion

		#region equality
		/// <summary>
		/// Equality operator
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(ComplexDouble a, ComplexDouble b) => a.Equals(b);

		/// <summary>
		/// Inequality operator
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(ComplexDouble a, ComplexDouble b) => !(a == b);

		/// <summary>
		/// Equality operator
		/// </summary>
		/// <param name="other">The other <see cref="ComplexDouble"/> to compare</param>
		/// <returns>this == <paramref name="other"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(ComplexDouble other)
		{
			return this.real.IsEqual(other.real) && this.imag.IsEqual(other.imag);
		}

		/// <summary>
		/// Override <see cref="object.GetHashCode"/>
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode()
		{
			return HashCode.Combine(this.real, this.imag);
		}

		/// <summary>
		/// Override <see cref="object.Equals(object)"/>
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override bool Equals(object? obj)
		{
			ComplexDouble a;
			if (obj is int @int)
				a = @int;
			else if (obj is double real)
				a = real;
			else if (obj is ComplexDouble complex)
				a = complex;
			else
				return false;
			return this.Equals(a);
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

		#region arithmetic operators
		/// <summary>
		/// Complex negate
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ComplexDouble operator -(ComplexDouble a) => new(-a.real, -a.imag);
		/// <summary>
		/// Complex add
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ComplexDouble operator +(ComplexDouble a, ComplexDouble b) => new(a.real + b.real, a.imag + b.imag);
		/// <summary>
		/// Complex subtract
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ComplexDouble operator -(ComplexDouble a, ComplexDouble b) => new(a.real - b.real, a.imag - b.imag);
		/// <summary>
		/// Complex add real
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ComplexDouble operator +(ComplexDouble a, double b) => new(a.real + b, a.imag);
		/// <summary>
		/// Complex add real
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ComplexDouble operator +(double b, ComplexDouble a) => new(a.real + b, a.imag);
		/// <summary>
		/// Complex subtract real
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ComplexDouble operator -(ComplexDouble a, double b) => new(a.real - b, a.imag);
		/// <summary>
		/// Real subtract complex
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ComplexDouble operator -(double b, ComplexDouble a) => new(b - a.real, -a.imag);

		/// <summary>
		/// Complex multiply
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ComplexDouble operator *(ComplexDouble a, ComplexDouble b)
		{
			return DoubleMul(a, b);
		}
		/// <summary>
		/// Complex division, guards against intermediate underflow and overflow by scaling
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ComplexDouble operator /(ComplexDouble a, ComplexDouble b)
		{
			return DoubleDiv(a, b);
		}
		/// <summary>
		/// Complex multiply real number
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ComplexDouble operator *(ComplexDouble a, double b) => new(a.real * b, a.imag * b);
		/// <summary>
		/// Complex multiply real number
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ComplexDouble operator *(double b, ComplexDouble a) => new(a.real * b, a.imag * b);
		/// <summary>
		/// Complex divide real number
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ComplexDouble operator /(ComplexDouble a, double b) => new(a.real / b, a.imag / b);
		/// <summary>
		/// Real number divide complex 
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ComplexDouble operator /(double a, ComplexDouble b) => DoubleRealDiv(a, b);

		/// <summary>
		/// Complex absolute value of this complex
		/// </summary>
		/// <returns>The absolute value of this complex</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public double Abs()
		{
			return DoubleAbs(this);
		}

		/// <summary>
		/// Square of complex absolute value of this complex
		/// </summary>
		/// <returns>The absolute value of this complex</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public double SquareAbs()
		{
			return DoubleSquareAbs(this);
		}

		/// <summary>
		/// Compute the argument of this complex
		/// </summary>
		/// <returns>The argument of this complex</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public double Arg()
		{
			return DoubleArg(this);
		}

		/// <summary>
		/// Complex conjugate
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ComplexDouble Conjugate() => new(this.real, -this.imag);


		/// <summary>
		/// Complex exponential (of base <c>e</c>)
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ComplexDouble Exp()
		{
			var doubleResult = DoubleExp(this);
			return doubleResult;
		}

		/// <summary>
		/// Complex logarithm (of base <c>e</c>)
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ComplexDouble Log()
		{
			return DoubleLog(this);
		}

		/// <summary>
		/// Complex number power
		/// </summary>
		/// <param name="p">The power of real type</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ComplexDouble Pow(double p)
		{
			return DoublePow(this, p);
		}

		/// <summary>
		/// Complex  number power
		/// </summary>
		/// <param name="p">The power of complex type <see cref="ComplexDouble"/></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ComplexDouble Pow(ComplexDouble p)
		{
			return DoublePow(this, p);
		}

		/// <summary>
		/// Get the complex square root of this complex
		/// </summary>
		/// <returns>The complex square root of this complex</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ComplexDouble Sqrt()
		{
			return DoubleSqrt(this);
		}

		/// <summary>
		/// Out-of-place add <paramref name="another"/> value of <see cref="ComplexDouble"/>
		/// </summary>
		/// <param name="another">Another <see cref="ComplexDouble"/> to be added</param>
		/// <returns>The addition result</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ComplexDouble Add(ComplexDouble another) => this + another;

		/// <summary>
		/// Out-of-place subtract <paramref name="another"/> value of <see cref="ComplexDouble"/>
		/// </summary>
		/// <param name="another">Another <see cref="ComplexDouble"/> to be subtracted</param>
		/// <returns>The subtraction result</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ComplexDouble Subtract(ComplexDouble another) => this - another;

		/// <summary>
		/// Out-of-place multiply <paramref name="another"/> value of <see cref="ComplexDouble"/>
		/// </summary>
		/// <param name="another">Another <see cref="ComplexDouble"/> to be multiplied</param>
		/// <returns>The multiplication result</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ComplexDouble Multiply(ComplexDouble another) => this * another;
		/// <summary>
		/// Out-of-place divide <paramref name="another"/> value of <see cref="ComplexDouble"/>
		/// </summary>
		/// <param name="another">Another <see cref="ComplexDouble"/> to be divided</param>
		/// <returns>The division result</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ComplexDouble Divide(ComplexDouble another) => this / another;

		/// <summary>
		/// Out-of-place add the square of the absolute value of <paramref name="another"/> <see cref="ComplexDouble"/> to this complex
		/// </summary>
		/// <param name="another">Another <see cref="ComplexDouble"/> whose absolute value's square is to be added</param>
		/// <returns>The addition result</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ComplexDouble AddSquareAbs(ComplexDouble another) => DoubleAddSquareAbs(this, another);

		/// <summary>
		/// Out-of-place multiply <paramref name="another"/> value of <see cref="ComplexDouble"/> after conjugating this complex
		/// </summary>
		/// <param name="another">Another <see cref="ComplexDouble"/> to be multiplied</param>
		/// <returns>The conjugate multiplication result</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ComplexDouble ConjugateMultiply(ComplexDouble another) => DoubleMulConjA(this, another);

		/// <summary>
		/// Out-of-place add the product result of <see cref="ComplexDouble"/>s <paramref name="a"/> and <paramref name="b"/> to this complex
		/// </summary>
		/// <param name="a">The first <see cref="ComplexDouble"/> value to be multiplied</param>
		/// <param name="b">The second <see cref="ComplexDouble"/> value to be multiplied</param>
		/// <returns>The fused multiplication addition (FMA) result</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ComplexDouble AddProduct(ComplexDouble a, ComplexDouble b) => DoubleFMA(a, b, this);

		/// <summary>
		/// Out-of-place add the product result of <see cref="ComplexDouble"/>s <paramref name="a"/>'s conjugate and <paramref name="b"/> to this complex
		/// </summary>
		/// <param name="a">The first <see cref="ComplexDouble"/> value to be conjugated and multiplied</param>
		/// <param name="b">The second <see cref="ComplexDouble"/> value to be multiplied</param>
		/// <returns>The fused multiplication addition (FMA) result</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ComplexDouble AddConjugateProduct(ComplexDouble a, ComplexDouble b) => DoubleFMAConjA(a, b, this);

		/// <summary>
		/// Out-of-place add the product result of <see cref="ComplexDouble"/>s <paramref name="a"/> and <paramref name="b"/> to the negation of this complex
		/// </summary>
		/// <param name="a">The first <see cref="ComplexDouble"/> value to be multiplied</param>
		/// <param name="b">The second <see cref="ComplexDouble"/> value to be multiplied</param>
		/// <returns>The fused multiplication subtraction (FMS) result</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ComplexDouble NegateAddProduct(ComplexDouble a, ComplexDouble b) => DoubleFMS(a, b, this);

		/// <summary>
		/// Out-of-place add the product result of <see cref="ComplexDouble"/>s <paramref name="a"/>'s conjugate and <paramref name="b"/> to the negation of this complex
		/// </summary>
		/// <param name="a">The first <see cref="ComplexDouble"/> value to be conjugated and multiplied</param>
		/// <param name="b">The second <see cref="ComplexDouble"/> value to be multiplied</param>
		/// <returns>The fused multiplication subtraction (FMS) result</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ComplexDouble NegateAddConjugateProduct(ComplexDouble a, ComplexDouble b) => DoubleFMSConjA(a, b, this);
		#endregion

		#region string representation
		/// <summary>
		/// Override <see cref="object.ToString"/>
		/// </summary>
		public override string ToString()
		{
			return this.ToString(null, Resource.Culture);
		}

		/// <summary>
		/// String representation of this complex number
		/// </summary>
		/// <param name="format">format of output</param>
		public string ToString(string? format)
		{
			return this.ToString(format, Resource.Culture);
		}

		/// <summary>
		/// Implementation of <see cref="IFormattable.ToString(string, IFormatProvider)"/> that formats the value of the current instance using the specified format.
		/// </summary>
		/// <param name="format">The format to use</param>
		/// <param name="formatProvider">The provider to use to format the value</param>
		public string ToString(string? format, IFormatProvider? formatProvider = null)
		{
			formatProvider ??= Resource.Culture;
			string r, i;
			r = this.real.ToString(format, formatProvider);
			i = this.imag.ToString(format, formatProvider);
			return $"({r},{i})";
		}
		#endregion
	}

	/// <summary>
	/// The single precision float complex type
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct ComplexSingle : IComplex<float>, ICustomNativeType<ComplexSingle>, IEquatable<ComplexSingle>
	{
		#region basic
		private readonly float real, imag;

		/// <summary>
		/// Get the real part
		/// </summary>
		public float Real {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.real;
		}

		/// <summary>
		/// Get the imaginary part
		/// </summary>
		public float Imag {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.imag;
		}

		/// <summary>
		/// Construct a <see cref="ComplexSingle"/> from real and imaginary parts
		/// </summary>
		/// <param name="re">The real part</param>
		/// <param name="im">The imaginary part, default value is 0</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ComplexSingle(float re, float im = 0)
		{
			this.real = re;
			this.imag = im;
		}
		#endregion

		#region static information
		DataTypeClassification ICustomNativeType<ComplexSingle>.Classification_Internal() => Const<float>.DataTypeClass;

		double ICustomNativeType<ComplexSingle>.MachinePrecision_Internal() => Const<float>.MachinePrecision;
		#endregion

		#region constant values
		/// <summary>
		/// <see cref="ComplexSingle"/> 0
		/// </summary>
		public static readonly ComplexSingle Zero = new(0);
		/// <summary>
		/// <see cref="ComplexSingle"/> 1
		/// </summary>
		public static readonly ComplexSingle One = new(1);
		/// <summary>
		/// <see cref="ComplexSingle"/> -1
		/// </summary>
		public static readonly ComplexSingle MinusOne = new(-1);
		/// <summary>
		/// <see cref="ComplexSingle"/> i
		/// </summary>
		public static readonly ComplexSingle ImOne = new(0, 1);
		/// <summary>
		/// <see cref="ComplexSingle"/> -1
		/// </summary>
		public static readonly ComplexSingle MinusImOne = new(0, -1);
		#endregion

		#region parser
		unsafe bool ICustomNativeType<ComplexSingle>.TryParse_Internal(string str, out ComplexSingle result)
		{
			bool success = ComplexDouble.TryParseAny(str, &float.TryParse, out float real, out float imag);
			result = new(real, imag);
			return success;
		}

		/// <summary>
		/// Try to parse a <see cref="string"/> to a new <see cref="ComplexSingle"/>
		/// </summary>
		/// <param name="s">The string to parse of form "a + b<c>i</c>", "a - b<c>i</c>", "a", "b<c>i</c>" or "-b<c>i</c>" where both 'a' and 'b' are float point numbers</param>
		/// <param name="complex">The output <see cref="ComplexSingle"/></param>
		/// <returns>success or not</returns>
		public unsafe static bool TryParse(string s, out ComplexSingle complex)
		{
			complex = default;
			if (s is null || s.Length == 0)
				return false;
			bool success = ComplexDouble.TryParseAny(s, &float.TryParse, out float real, out float imag);
			complex = new(real, imag);
			return success;
		}

		/// <summary>
		/// Parse a <see cref="string"/> to a <see cref="ComplexSingle"/>
		/// </summary>
		/// <param name="str">The string to parse of form "a + b<c>i</c>", "a - b<c>i</c>", "a", "b<c>i</c>" or "-b<c>i</c>" where both 'a' and 'b' are float point numbers</param>
		/// <returns>The parsed <see cref="ComplexSingle"/></returns>
		public static ComplexSingle Parse(string str)
		{
			if (str is null || str.Length == 0)
				throw new ArgumentNullException(nameof(str));
			bool success = TryParse(str, out ComplexSingle result);
			if (!success)
				throw new ArgumentException(string.Format(Other.CannotParseComplex, str, typeof(float).Name), nameof(str));
			return result;
		}
		#endregion

		#region converter
		/// <summary>
		/// Implicitly convert a int to a <see cref="ComplexSingle"/>
		/// </summary>
		/// <param name="a">a int</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator ComplexSingle(int a) => new(a);
		/// <summary>
		/// Implicitly convert a float to a <see cref="ComplexSingle"/>
		/// </summary>
		/// <param name="a">a float</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator ComplexSingle(float a) => new(a);
		/// <summary>
		/// Implicitly convert a int tuple to a <see cref="ComplexSingle"/>
		/// </summary>
		/// <param name="a">a int tuple</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator ComplexSingle((int r, int i) a) => new(a.r, a.i);
		/// <summary>
		/// Implicitly convert a float tuple to a <see cref="ComplexSingle"/>
		/// </summary>
		/// <param name="a">a float tuple</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator ComplexSingle((float r, float i) a) => new(a.r, a.i);

		/// <summary>
		/// Convert to <see cref="float"/> by getting absolute value
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static explicit operator float(ComplexSingle v) => v.Abs();

		/// <summary>
		/// Implicitly convert a <see cref="ComplexSingle"/> to a <see cref="ComplexDouble"/>
		/// </summary>
		/// <param name="a">The input <see cref="ComplexSingle"/> to be converted</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator ComplexDouble(ComplexSingle a) => new(a.real, a.imag);
		#endregion

		#region equality
		/// <summary>
		/// Equality operator
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(ComplexSingle a, ComplexSingle b) => a.Equals(b);

		/// <summary>
		/// Inequality operator
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(ComplexSingle a, ComplexSingle b) => !(a == b);

		/// <summary>
		/// Equality operator
		/// </summary>
		/// <param name="other">The other <see cref="ComplexSingle"/> to compare</param>
		/// <returns>this == <paramref name="other"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(ComplexSingle other)
		{
			return this.real.IsEqual(other.real) && this.imag.IsEqual(other.imag);
		}

		/// <summary>
		/// Override <see cref="object.GetHashCode"/>
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode()
		{
			return HashCode.Combine(this.real, this.imag);
		}

		/// <summary>
		/// Override <see cref="object.Equals(object)"/>
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override bool Equals(object? obj)
		{
			ComplexSingle a;
			if (obj is int @int)
				a = @int;
			else if (obj is float real)
				a = real;
			else if (obj is ComplexSingle complex)
				a = complex;
			else
				return false;
			return this.Equals(a);
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
			float real = MathF.FusedMultiplyAdd(a.real, b.real, -a.imag * b.imag); // vfmsub213ss
			float imag = MathF.FusedMultiplyAdd(a.real, b.imag,  a.imag * b.real); // vfmadd213ss
			return new ComplexSingle(real, imag);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ComplexSingle SingleDiv(ComplexSingle x, ComplexSingle y)
		{
			float squareAbsY = SingleSquareAbs(y);
			float acbd = MathF.FusedMultiplyAdd(x.real, y.real,  x.imag * y.imag); // vfmadd213ss
			float bcad = MathF.FusedMultiplyAdd(x.imag, y.real, -x.real * y.imag); // vfmsub213ss
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
			float squareAbs = MathF.FusedMultiplyAdd(a.real, a.real, a.imag * a.imag); // vfmadd213ss
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
			float real = MathF.FusedMultiplyAdd(a.real, b.real,  a.imag * b.imag); // vfmadd213ss
			float imag = MathF.FusedMultiplyAdd(a.real, b.imag, -a.imag * b.real); // vfmsub213ss
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

		#region arithmetic operators
		/// <summary>
		/// Complex negate
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ComplexSingle operator -(ComplexSingle a) => new(-a.real, -a.imag);
		/// <summary>
		/// Complex add
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ComplexSingle operator +(ComplexSingle a, ComplexSingle b) => new(a.real + b.real, a.imag + b.imag);
		/// <summary>
		/// Complex subtract
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ComplexSingle operator -(ComplexSingle a, ComplexSingle b) => new(a.real - b.real, a.imag - b.imag);
		/// <summary>
		/// Complex add real
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ComplexSingle operator +(ComplexSingle a, float b) => new(a.real + b, a.imag);
		/// <summary>
		/// Complex add real
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ComplexSingle operator +(float b, ComplexSingle a) => new(a.real + b, a.imag);
		/// <summary>
		/// Complex subtract real
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ComplexSingle operator -(ComplexSingle a, float b) => new(a.real - b, a.imag);
		/// <summary>
		/// Real subtract complex
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ComplexSingle operator -(float b, ComplexSingle a) => new(b - a.real, -a.imag);

		/// <summary>
		/// Complex multiply
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ComplexSingle operator *(ComplexSingle a, ComplexSingle b)
		{
			return SingleMul(a, b);
		}
		/// <summary>
		/// Complex division, guards against intermediate underflow and overflow by scaling
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ComplexSingle operator /(ComplexSingle a, ComplexSingle b)
		{
			return SingleDiv(a, b);
		}
		/// <summary>
		/// Complex multiply real number
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ComplexSingle operator *(ComplexSingle a, float b) => new(a.real * b, a.imag * b);
		/// <summary>
		/// Complex multiply real number
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ComplexSingle operator *(float b, ComplexSingle a) => new(a.real * b, a.imag * b);
		/// <summary>
		/// Complex divide real number
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ComplexSingle operator /(ComplexSingle a, float b) => new(a.real / b, a.imag / b);
		/// <summary>
		/// Real number divide complex 
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ComplexSingle operator /(float a, ComplexSingle b) => SingleRealDiv(a, b);

		/// <summary>
		/// Complex absolute value of this complex
		/// </summary>
		/// <returns>The absolute value of this complex</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float Abs()
		{
			return SingleAbs(this);
		}

		/// <summary>
		/// Square of complex absolute value of this complex
		/// </summary>
		/// <returns>The absolute value of this complex</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float SquareAbs()
		{
			return SingleSquareAbs(this);
		}

		/// <summary>
		/// Compute the argument of this complex
		/// </summary>
		/// <returns>The argument of this complex</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float Arg()
		{
			return SingleArg(this);
		}

		/// <summary>
		/// Complex conjugate
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ComplexSingle Conjugate() => new(this.real, -this.imag);


		/// <summary>
		/// Complex exponential (of base <c>e</c>)
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ComplexSingle Exp()
		{
			var doubleResult = SingleExp(this);
			return doubleResult;
		}

		/// <summary>
		/// Complex logarithm (of base <c>e</c>)
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ComplexSingle Log()
		{
			return SingleLog(this);
		}

		/// <summary>
		/// Complex number power
		/// </summary>
		/// <param name="p">The power of real type</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ComplexSingle Pow(float p)
		{
			return SinglePow(this, p);
		}

		/// <summary>
		/// Complex  number power
		/// </summary>
		/// <param name="p">The power of complex type <see cref="ComplexSingle"/></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ComplexSingle Pow(ComplexSingle p)
		{
			return SinglePow(this, p);
		}

		/// <summary>
		/// Get the complex square root of this complex
		/// </summary>
		/// <returns>The complex square root of this complex</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ComplexSingle Sqrt()
		{
			return SingleSqrt(this);
		}

		/// <summary>
		/// Out-of-place add <paramref name="another"/> value of <see cref="ComplexSingle"/>
		/// </summary>
		/// <param name="another">Another <see cref="ComplexSingle"/> to be added</param>
		/// <returns>The addition result</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ComplexSingle Add(ComplexSingle another) => this + another;

		/// <summary>
		/// Out-of-place subtract <paramref name="another"/> value of <see cref="ComplexSingle"/>
		/// </summary>
		/// <param name="another">Another <see cref="ComplexSingle"/> to be subtracted</param>
		/// <returns>The subtraction result</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ComplexSingle Subtract(ComplexSingle another) => this - another;

		/// <summary>
		/// Out-of-place multiply <paramref name="another"/> value of <see cref="ComplexSingle"/>
		/// </summary>
		/// <param name="another">Another <see cref="ComplexSingle"/> to be multiplied</param>
		/// <returns>The multiplication result</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ComplexSingle Multiply(ComplexSingle another) => this * another;
		/// <summary>
		/// Out-of-place divide <paramref name="another"/> value of <see cref="ComplexSingle"/>
		/// </summary>
		/// <param name="another">Another <see cref="ComplexSingle"/> to be divided</param>
		/// <returns>The division result</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ComplexSingle Divide(ComplexSingle another) => this / another;

		/// <summary>
		/// Out-of-place add the square of the absolute value of <paramref name="another"/> <see cref="ComplexSingle"/> to this complex
		/// </summary>
		/// <param name="another">Another <see cref="ComplexSingle"/> whose absolute value's square is to be added</param>
		/// <returns>The addition result</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ComplexSingle AddSquareAbs(ComplexSingle another) => SingleAddSquareAbs(this, another);

		/// <summary>
		/// Out-of-place multiply <paramref name="another"/> value of <see cref="ComplexSingle"/> after conjugating this complex
		/// </summary>
		/// <param name="another">Another <see cref="ComplexSingle"/> to be multiplied</param>
		/// <returns>The conjugate multiplication result</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ComplexSingle ConjugateMultiply(ComplexSingle another) => SingleMulConjA(this, another);

		/// <summary>
		/// Out-of-place add the product result of <see cref="ComplexSingle"/>s <paramref name="a"/> and <paramref name="b"/> to this complex
		/// </summary>
		/// <param name="a">The first <see cref="ComplexSingle"/> value to be multiplied</param>
		/// <param name="b">The second <see cref="ComplexSingle"/> value to be multiplied</param>
		/// <returns>The fused multiplication addition (FMA) result</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ComplexSingle AddProduct(ComplexSingle a, ComplexSingle b) => SingleFMA(a, b, this);
		
		/// <summary>
		/// Out-of-place add the product result of <see cref="ComplexSingle"/>s <paramref name="a"/>'s conjugate and <paramref name="b"/> to this complex
		/// </summary>
		/// <param name="a">The first <see cref="ComplexSingle"/> value to be conjugated and multiplied</param>
		/// <param name="b">The second <see cref="ComplexSingle"/> value to be multiplied</param>
		/// <returns>The fused multiplication addition (FMA) result</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ComplexSingle AddConjugateProduct(ComplexSingle a, ComplexSingle b) => SingleFMAConjA(a, b, this);

		/// <summary>
		/// Out-of-place add the product result of <see cref="ComplexSingle"/>s <paramref name="a"/> and <paramref name="b"/> to the negation of this complex
		/// </summary>
		/// <param name="a">The first <see cref="ComplexSingle"/> value to be multiplied</param>
		/// <param name="b">The second <see cref="ComplexSingle"/> value to be multiplied</param>
		/// <returns>The fused multiplication subtraction (FMS) result</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ComplexSingle NegateAddProduct(ComplexSingle a, ComplexSingle b) => SingleFMS(a, b, this);

		/// <summary>
		/// Out-of-place add the product result of <see cref="ComplexSingle"/>s <paramref name="a"/>'s conjugate and <paramref name="b"/> to the negation of this complex
		/// </summary>
		/// <param name="a">The first <see cref="ComplexSingle"/> value to be conjugated and multiplied</param>
		/// <param name="b">The second <see cref="ComplexSingle"/> value to be multiplied</param>
		/// <returns>The fused multiplication subtraction (FMS) result</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ComplexSingle NegateAddConjugateProduct(ComplexSingle a, ComplexSingle b) => SingleFMSConjA(a, b, this);
		#endregion

		#region string representation
		/// <summary>
		/// Override <see cref="object.ToString"/>
		/// </summary>
		public override string ToString()
		{
			return this.ToString(null, Resource.Culture);
		}

		/// <summary>
		/// String representation of this complex number
		/// </summary>
		/// <param name="format">format of output</param>
		public string ToString(string? format)
		{
			return this.ToString(format, Resource.Culture);
		}

		/// <summary>
		/// Implementation of <see cref="IFormattable.ToString(string, IFormatProvider)"/> that formats the value of the current instance using the specified format.
		/// </summary>
		/// <param name="format">The format to use</param>
		/// <param name="formatProvider">The provider to use to format the value</param>
		public string ToString(string? format, IFormatProvider? formatProvider = null)
		{
			formatProvider ??= Resource.Culture;
			string r, i;
			r = this.real.ToString(format, formatProvider);
			i = this.imag.ToString(format, formatProvider);
			return $"({r},{i})";
		}
		#endregion
	}
	#endregion


	#region generic complex type
	/// <summary>
	/// The general complex type for any real numeric number type including <see cref="float"/> and <see cref="double"/>
	/// </summary>
	/// <typeparam name="T">The data type of corresponding real number</typeparam>
	/// <remarks>This is an <c>unmanaged</c> type since C# 8.0.<br/>
	/// If <typeparamref name="T"/> is <see cref="float"/> or <see cref="double"/>, the corresponding <see cref="ComplexSingle"/> or <see cref="ComplexDouble"/> is recommended since their operations are much faster.</remarks>
	[StructLayout(LayoutKind.Sequential)]
	public struct Complex<T> : IComplex<T>, ICustomNativeType<Complex<T>>, IEquatable<Complex<T>> where T : unmanaged
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
		public T Imag {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.imag;
		}

		/// <summary>
		/// Construct a <see cref="Complex{T}"/> from real and imaginary parts
		/// </summary>
		/// <param name="re">The real part</param>
		/// <param name="im">The imaginary part, default value is 0</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Complex(T re, T im = default)
		{
			this.real = re;
			this.imag = im;
		}
		#endregion

		#region static information
		DataTypeClassification ICustomNativeType<Complex<T>>.Classification_Internal() => Const<T>.DataTypeClass;

		double ICustomNativeType<Complex<T>>.MachinePrecision_Internal() => Const<T>.MachinePrecision;

		static Complex()
		{
			// generic type check
			if (typeof(T).IsGenericType)
				throw new InvalidOperationException(Support.DataType);
			// native type check
			if (Const<T>.DataTypeClass < DataTypeClassification.FloatPoint_IEEE754)
				throw new InvalidOperationException(Support.DataType);
		}
		#endregion

		#region constant values
		/// <summary>
		/// <see cref="Complex{T}"/> 0
		/// </summary>
		public static readonly Complex<T> Zero = new(default);
		/// <summary>
		/// <see cref="Complex{T}"/> 1
		/// </summary>
		public static readonly Complex<T> One = new(Const<T>.One);
		/// <summary>
		/// <see cref="Complex{T}"/> -1
		/// </summary>
		public static readonly Complex<T> MinusOne = new(Const<T>.MinusOne);
		/// <summary>
		/// <see cref="Complex{T}"/> i
		/// </summary>
		public static readonly Complex<T> ImOne = new(default, Const<T>.One);
		/// <summary>
		/// <see cref="Complex{T}"/> -1
		/// </summary>
		public static readonly Complex<T> MinusImOne = new(default, Const<T>.MinusOne);
		#endregion

		#region parser
		bool ICustomNativeType<Complex<T>>.TryParse_Internal(string str, out Complex<T> result) => TryParse(str, out result);

		/// <summary>
		/// Try to parse a <see cref="string"/> to a new <see cref="Complex{T}"/>
		/// </summary>
		/// <param name="s">string to parse of form "a + b<c>i</c>", "a - b<c>i</c>", "a", "b<c>i</c>" or "-b<c>i</c>" where both 'a' and 'b' are float point numbers</param>
		/// <param name="complex">output <see cref="Complex{T}"/></param>
		/// <returns>success or not</returns>
		public unsafe static bool TryParse(string s, out Complex<T> complex)
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
		public static Complex<T> Parse(string str)
		{
			if (str is null || str.Length == 0)
				throw new ArgumentNullException(nameof(str));
			bool success = TryParse(str, out Complex<T> result);
			if (!success)
				throw new ArgumentException(string.Format(Other.CannotParseComplex, str, typeof(T).Name), nameof(str));
			return result;
		}
		#endregion

		#region converter
		/// <summary>
		/// Convert from int
		/// </summary>
		/// <param name="a">a int</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator Complex<T>(int a) => new(a.NativeConvert<int, T>());
		/// <summary>
		/// Convert from T
		/// </summary>
		/// <param name="a">a T</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator Complex<T>(T a) => new(a);
		/// <summary>
		/// Convert from int tuple
		/// </summary>
		/// <param name="a">a int tuple</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator Complex<T>((int r, int i) a) => new(a.r.NativeConvert<int, T>(), a.i.NativeConvert<int, T>());
		/// <summary>
		/// Convert from T tuple
		/// </summary>
		/// <param name="a">a T tuple</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator Complex<T>((T r, T i) a) => new(a.r, a.i);

		/// <summary>
		/// Convert to <see cref="ComplexDouble"/>
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe explicit operator ComplexDouble(Complex<T> v)
		{
			if (typeof(T) == typeof(float))
				return *(ComplexSingle*)&v;
			else if (typeof(T) == typeof(double))
				return *(ComplexDouble*)&v;
			else
				return new(v.real.ToDouble(), v.imag.ToDouble());
		}

		/// <summary>
		/// Convert from <see cref="ComplexDouble"/>
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe explicit operator Complex<T>(ComplexDouble v)
		{
			if (typeof(T) == typeof(double))
				return *(Complex<T>*)&v;
			else
				return new(v.Real.FromDouble<T>(), v.Imag.FromDouble<T>());
		}

		/// <summary>
		/// Convert to <typeparamref name="T"/>
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static explicit operator T(Complex<T> v) => v.Abs();
		#endregion

		#region equality
		/// <summary>
		/// Equality operator
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(Complex<T> a, Complex<T> b) => a.Equals(b);

		/// <summary>
		/// Inequality operator
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(Complex<T> a, Complex<T> b) => !(a == b);

		/// <summary>
		/// Equality operator
		/// </summary>
		/// <param name="other">The other <see cref="Complex{T}"/> to compare</param>
		/// <returns>this == <paramref name="other"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(Complex<T> other)
		{
			return this.real.IsEqual(other.real) && this.imag.IsEqual(other.imag);
		}

		/// <summary>
		/// Override <see cref="object.GetHashCode"/>
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode()
		{
			return HashCode.Combine(this.real, this.imag);
		}

		/// <summary>
		/// Override <see cref="object.Equals(object)"/>
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
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
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Complex<T> operator -(Complex<T> a) => new(a.real.NativeNegate(), a.imag.NativeNegate());
		/// <summary>
		/// Complex add
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Complex<T> operator +(Complex<T> a, Complex<T> b) => new(a.real.NativeAdd(b.real), a.imag.NativeAdd(b.imag));
		/// <summary>
		/// Complex subtract
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Complex<T> operator -(Complex<T> a, Complex<T> b) => new(a.real.NativeSub(b.real), a.imag.NativeSub(b.imag));
		/// <summary>
		/// Complex add real
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Complex<T> operator +(Complex<T> a, T b) => new(a.real.NativeAdd(b), a.imag);
		/// <summary>
		/// Complex add real
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Complex<T> operator +(T b, Complex<T> a) => new(a.real.NativeAdd(b), a.imag);
		/// <summary>
		/// Complex subtract real
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Complex<T> operator -(Complex<T> a, T b) => new(a.real.NativeSub(b), a.imag);
		/// <summary>
		/// Real subtract complex
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Complex<T> operator -(T b, Complex<T> a) => new(b.NativeSub(a.real), a.imag.NativeNegate());

		/// <summary>
		/// Complex multiply
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
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
			T real = a.real.NativeMultiply(b.real);
			T temp = a.imag.NativeMultiply(b.imag);
			real = real.NativeSub(temp);
			T imag = a.real.NativeMultiply(b.imag);
			temp = a.imag.NativeMultiply(b.real);
			imag = imag.NativeAdd(temp);
			return new Complex<T>(real, imag);
		}

		/// <summary>
		/// Complex division, guards against intermediate underflow and overflow by scaling
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe Complex<T> operator /(Complex<T> a, Complex<T> b)
		{
			if (typeof(T) == typeof(float))
			{
				ComplexSingle v = (*(ComplexSingle*)&a) / (*(ComplexSingle*)&b);
				return *(Complex<T>*)&v;
			}
			if (typeof(T) == typeof(double))
			{
				ComplexDouble v = (*(ComplexSingle*)&a) * (*(ComplexSingle*)&b);
				return *(Complex<T>*)&v;
			}
			// else
			return (Complex<T>)((ComplexDouble)a / (ComplexDouble)b);
		}

		/// <summary>
		/// Complex multiply real number
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Complex<T> operator *(Complex<T> a, T b) => new(a.real.NativeMultiply(b), a.imag.NativeMultiply(b));
		/// <summary>
		/// Complex multiply real number
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Complex<T> operator *(T b, Complex<T> a) => new(a.real.NativeMultiply(b), a.imag.NativeMultiply(b));
		/// <summary>
		/// Complex divide real number
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Complex<T> operator /(Complex<T> a, T b) => new(a.real.NativeDivide(b), a.imag.NativeDivide(b));

		/// <summary>
		/// Real number divide complex 
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
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
			return (Complex<T>)(a.ToDouble() / (ComplexDouble)b);
		}

		/// <summary>
		/// Complex absolute value of this complex
		/// </summary>
		/// <returns>The absolute value of this complex</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe T Abs()
		{
			if (typeof(T) == typeof(float))
			{
				Complex<T> p = this;
				float v = (*(ComplexSingle*)&p).Abs();
				return *(T*)&v;
			}
			if (typeof(T) == typeof(double))
			{
				Complex<T> p = this;
				double v = (*(ComplexDouble*)&p).Abs();
				return *(T*)&v;
			}
			// else
			return ((ComplexDouble)this).Abs().FromDouble<T>();
		}

		/// <summary>
		/// Complex absolute value of this complex
		/// </summary>
		/// <returns>The absolute value of this complex as a <see cref="float"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal unsafe double AbsSingle()
		{
			if (typeof(T) == typeof(float))
			{
				Complex<T> p = this;
				return (*(ComplexSingle*)&p).Abs();
			}
			if (typeof(T) == typeof(double))
			{
				Complex<T> p = this;
				return (float)(*(ComplexDouble*)&p).Abs();
			}
			// else
			return (float)((ComplexDouble)this).Abs();
		}

		/// <summary>
		/// Complex absolute value of this complex
		/// </summary>
		/// <returns>The absolute value of this complex as a <see cref="double"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal unsafe double AbsDouble()
		{
			if (typeof(T) == typeof(float))
			{
				Complex<T> p = this;
				return (*(ComplexSingle*)&p).Abs();
			}
			if (typeof(T) == typeof(double))
			{
				Complex<T> p = this;
				return (*(ComplexDouble*)&p).Abs();
			}
			// else
			return ((ComplexDouble)this).Abs();
		}

		/// <summary>
		/// Square of complex absolute value of this complex
		/// </summary>
		/// <returns>The absolute value of this complex</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe T SquareAbs()
		{
			if (typeof(T) == typeof(float))
			{
				Complex<T> p = this;
				float v = (*(ComplexSingle*)&p).SquareAbs();
				return *(T*)&v;
			}
			if (typeof(T) == typeof(double))
			{
				Complex<T> p = this;
				double v = (*(ComplexDouble*)&p).SquareAbs();
				return *(T*)&v;
			}
			// else
			return ((ComplexDouble)this).SquareAbs().FromDouble<T>();
		}

		/// <summary>
		/// Compute the argument of this complex
		/// </summary>
		/// <returns>The argument of this complex</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe T Arg()
		{
			if (typeof(T) == typeof(float))
			{
				Complex<T> p = this;
				float v = (*(ComplexSingle*)&p).Arg();
				return *(T*)&v;
			}
			if (typeof(T) == typeof(double))
			{
				Complex<T> p = this;
				double v = (*(ComplexDouble*)&p).Arg();
				return *(T*)&v;
			}
			// else
			return ((ComplexDouble)this).Arg().FromDouble<T>();
		}

		/// <summary>
		/// Complex conjugate
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Complex<T> Conjugate() => new(this.real, this.imag.NativeNegate());

		/// <summary>
		/// Complex exponential (of base <c>e</c>)
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe Complex<T> Exp()
		{
			if (typeof(T) == typeof(float))
			{
				Complex<T> p = this;
				ComplexSingle v = (*(ComplexSingle*)&p).Exp();
				return *(Complex<T>*)&v;
			}
			if (typeof(T) == typeof(double))
			{
				Complex<T> p = this;
				ComplexDouble v = (*(ComplexDouble*)&p).Exp();
				return *(Complex<T>*)&v;
			}
			// else
			var doubleResult = ((ComplexDouble)this).Exp();
			return (Complex<T>)doubleResult;
		}

		/// <summary>
		/// Complex logarithm (of base <c>e</c>)
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe Complex<T> Log()
		{
			if (typeof(T) == typeof(float))
			{
				Complex<T> p = this;
				ComplexSingle v = (*(ComplexSingle*)&p).Log();
				return *(Complex<T>*)&v;
			}
			if (typeof(T) == typeof(double))
			{
				Complex<T> p = this;
				ComplexDouble v = (*(ComplexDouble*)&p).Log();
				return *(Complex<T>*)&v;
			}
			// else
			return (Complex<T>)((ComplexDouble)this).Log();
		}

		/// <summary>
		/// Complex number power
		/// </summary>
		/// <param name="p">The power of real type <typeparamref name="T"/></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe Complex<T> Pow(T p)
		{
			if (typeof(T) == typeof(float))
			{
				Complex<T> a = this;
				ComplexSingle v = (*(ComplexSingle*)&a).Pow(*(float*)&p);
				return *(Complex<T>*)&v;
			}
			if (typeof(T) == typeof(double))
			{
				Complex<T> a = this;
				ComplexDouble v = (*(ComplexDouble*)&a).Pow(*(double*)&p);
				return *(Complex<T>*)&v;
			}
			// else
			return (Complex<T>)((ComplexDouble)this).Pow(p.ToDouble());
		}

		internal unsafe Complex<T> PowDouble(double p)
		{
			if (typeof(T) == typeof(float))
			{
				Complex<T> a = this;
				ComplexSingle v = (*(ComplexSingle*)&a).Pow((float)p);
				return *(Complex<T>*)&v;
			}
			if (typeof(T) == typeof(double))
			{
				Complex<T> a = this;
				ComplexDouble v = (*(ComplexDouble*)&a).Pow(p);
				return *(Complex<T>*)&v;
			}
			// else
			return (Complex<T>)((ComplexDouble)this).Pow(p);
		}


		/// <summary>
		/// Complex  number power
		/// </summary>
		/// <param name="p">The power of complex type <see cref="Complex{T}"/></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe Complex<T> Pow(Complex<T> p)
		{
			if (typeof(T) == typeof(float))
			{
				Complex<T> a = this;
				ComplexSingle v = (*(ComplexSingle*)&a).Pow(*(ComplexSingle*)&p);
				return *(Complex<T>*)&v;
			}
			if (typeof(T) == typeof(double))
			{
				Complex<T> a = this;
				ComplexDouble v = (*(ComplexDouble*)&a).Pow(*(ComplexDouble*)&p);
				return *(Complex<T>*)&v;
			}
			// else
			return (Complex<T>)((ComplexDouble)this).Pow((ComplexDouble)p);
		}

		/// <summary>
		/// Get the complex square root of this complex
		/// </summary>
		/// <returns>The complex square root of this complex</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe Complex<T> Sqrt()
		{
			if (typeof(T) == typeof(float))
			{
				Complex<T> a = this;
				ComplexSingle v = (*(ComplexSingle*)&a).Sqrt();
				return *(Complex<T>*)&v;
			}
			if (typeof(T) == typeof(double))
			{
				Complex<T> a = this;
				ComplexDouble v = (*(ComplexDouble*)&a).Sqrt();
				return *(Complex<T>*)&v;
			}
			// else
			return (Complex<T>)((ComplexDouble)this).Sqrt();
		}

		/// <summary>
		/// Out-of-place add <paramref name="another"/> value of <see cref="Complex{T}"/>
		/// </summary>
		/// <param name="another">another value to be added</param>
		/// <returns>The addition result</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Complex<T> Add(Complex<T> another) => this + another;

		/// <summary>
		/// Out-of-place subtract <paramref name="another"/> value of <see cref="Complex{T}"/>
		/// </summary>
		/// <param name="another">another value to be subtracted</param>
		/// <returns>The subtraction result</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Complex<T> Subtract(Complex<T> another) => this - another;

		/// <summary>
		/// Out-of-place multiply <paramref name="another"/> value of <see cref="Complex{T}"/>
		/// </summary>
		/// <param name="another">another value to be multiplied</param>
		/// <returns>The multiplication result</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Complex<T> Multiply(Complex<T> another) => this * another;

		/// <summary>
		/// Out-of-place divide <paramref name="another"/> value of <see cref="Complex{T}"/>
		/// </summary>
		/// <param name="another">another value to be divided</param>
		/// <returns>The division result</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Complex<T> Divide(Complex<T> another) => this / another;
		#endregion

		#region string representation
		/// <summary>
		/// Override <see cref="object.ToString"/>
		/// </summary>
		public override string ToString()
		{
			return this.ToString(null, Resource.Culture);
		}

		/// <summary>
		/// String representation of this complex number
		/// </summary>
		/// <param name="format">format of output</param>
		public string ToString(string? format)
		{
			return this.ToString(format, Resource.Culture);
		}

		/// <summary>
		/// Implementation of <see cref="IFormattable.ToString(string, IFormatProvider)"/> that formats the value of the current instance using the specified format.
		/// </summary>
		/// <param name="format">The format to use</param>
		/// <param name="formatProvider">The provider to use to format the value</param>
		public string ToString(string? format, IFormatProvider? formatProvider = null)
		{
			formatProvider ??= Resource.Culture;
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