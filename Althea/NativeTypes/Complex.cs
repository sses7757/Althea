using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Althea.NativeTypes;
using Althea.Resources;


namespace Althea.NativeTypes
{
	#region complex interface
	/// <summary>
	/// The complex interface for any possible real data type
	/// </summary>
	/// <typeparam name="T">The data type of corresponding real number, usually an unmanaged struct that implements <see cref="ICustomNativeType{T}"/></typeparam>
	public interface IComplex<T> : IFormattable where T : unmanaged
	{
		/// <summary>
		/// Get the real part
		/// </summary>
		T Real { get; }

		/// <summary>
		/// Get the imaginary part
		/// </summary>
		T Imag { get; }

		/// <summary>
		/// Compute the absolute value of this complex
		/// </summary>
		/// <returns>The absolute value of this complex</returns>
		T Abs();

		/// <summary>
		/// Compute the argument of this complex
		/// </summary>
		/// <returns>The argument of this complex</returns>
		T Arg();
	}
	#endregion

	#region generic complex type
	/// <summary>
	/// The general complex type for built-in types
	/// </summary>
	/// <typeparam name="T">The data type of corresponding real number</typeparam>
	/// <remarks>This is an <c>unmanaged</c> type since C# 8.0.<br/>
	/// I do not recommend one to use any data type conversions or arithmetic operations in heavy load like loop over a <c><see cref="Complex{T}"/>[]</c> even though the dynamic functions will be optimized by the JIT to have performance way better than boxing and unboxing <typeparamref name="T"/>, they may still perform a lot worse than operations with compile-time-known <typeparamref name="T"/>.</remarks>
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
		/// Constructor from real and imaginary parts
		/// </summary>
		/// <param name="re">real part</param>
		/// <param name="im">imaginary part, default value is <c>default(<typeparamref name="T"/>)</c></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Complex(T re, T im = default)
		{
			this.real = re;
			this.imag = im;
		}
		#endregion

		#region static information
		DataTypeClassification ICustomNativeType<Complex<T>>.Classification_Internal()
		{
			return _Classification;
		}

		private static readonly DataTypeClassification _Classification;

		static Complex()
		{
			// generic type check
			if (typeof(T).IsGenericType)
				throw new InvalidOperationException(Support.DataType);
			// native type check
			_Classification = Const<T>.DataTypeClass;
			if (_Classification == DataTypeClassification.NotSupported)
				throw new InvalidOperationException(Support.DataType);
		}

		private static unsafe readonly int _sizeT = sizeof(T);

		private static readonly Converter<T, double> _toDouble = Const<T>.ToDoubleDelegate;

		private static readonly Converter<double, T> _fromDouble = Const<T>.FromDoubleDelegate;

		private static readonly Func<T, T> _negate = Const<T>.NegateDelegate;

		private static readonly Func<T, T, T> _add = Const<T>.AddDelegate;

		private static readonly Func<T, T, T> _sub = Const<T>.SubtractDelegate;

		private static readonly Func<T, T, T> _mul = Const<T>.MultiplyDelegate;

		private static readonly Func<T, T, T> _div = Const<T>.DivideDelegate;
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

		private const StringComparison _StrCmp = StringComparison.OrdinalIgnoreCase;

		private const string _StrAdd = " + ", _StrSub = " - ", _StrImag = "i";
		private const char _CharNeg = '-', _CharImag = 'i';

		/// <summary>
		/// Try to parse part of complex string <paramref name="s"/>
		/// </summary>
		/// <returns>null for unsuccessful, value for parsed value and real part or imaginary part</returns>
		private static (T part, bool real)? TryParsePart(string s)
		{
			int find = s.IndexOf(_CharImag, comparisonType: _StrCmp);
			bool real;
			if (find < 0)
			{
				real = true;
			}
			else
			{
				real = false;
				if (find >= 0 && find < s.Length - 1)
				{
					int find2 = s.IndexOf(_StrImag, startIndex: find + 1, comparisonType: _StrCmp);
					if (find2 >= 0)
					{
						return null;
					}
				}
				if (find == 0 && (s[1] < '0' || s[1] > '9'))
				{
					return null;
				}
				if (find == 1 && (s[0] != '-' || s[0] != '+'))
				{
					return null;
				}
				if (find != 0 && find != 1 && find != s.Length - 1)
				{
					return null;
				}
			}
			// parse
			bool success = s.TryParseNativeType(out T part);
			if (success)
				return (part, real);
			else
				return null;
		}

		/// <summary>
		/// Try to parse a <see cref="string"/> to a new <see cref="Complex{T}"/>
		/// </summary>
		/// <param name="s">string to parse of form "a + b<c>i</c>", "a - b<c>i</c>", "a", "b<c>i</c>" or "-b<c>i</c>" where both 'a' and 'b' are float point numbers</param>
		/// <param name="complex">output <see cref="Complex{T}"/></param>
		/// <returns>success or not</returns>
		public static bool TryParse(string s, out Complex<T> complex)
		{
			complex = default;
			if (s is null || s.Length == 0)
				return false;
			// index
			int findAdd = s.IndexOf(_StrAdd), findSub = s.IndexOf(_StrSub);
			if (findAdd < 0 && findSub < 0)
			{   // only one part
				int findNeg = s.IndexOf(_CharNeg);
				if (findNeg >=0 && findNeg < s.Length - 1 && s.IndexOf(_CharNeg, findNeg + 1) > 0)
				{
					return false;
				}
				(T part, bool real)? value = TryParsePart(s);
				if (!value.HasValue)
				{
					return false;
				}
				complex = value.Value.real ? new Complex<T>(value.Value.part, default) : new Complex<T>(default, value.Value.part);
				return true;
			}
			if ((findAdd >= 0 && findSub >= 0) || (findAdd >= s.Length - _StrAdd.Length) || (findSub >= s.Length - _StrSub.Length))
			{
				return false;
			}
			// check multiple plus or minus operators
			if (findAdd >= 0)
			{
				int find2 = s.IndexOf(_StrAdd, findAdd + _StrAdd.Length);
				if (find2 >= 0)
					return false;
			}
			else
			{
				int find2 = s.IndexOf(_StrSub, findSub + _StrSub.Length);
				if (find2 >= 0)
					return false;
			}
			// have both parts
			string firstPart = s[..Math.Max(findAdd, findSub)];
			string lastPart = s[(Math.Max(findAdd, findSub) + _StrAdd.Length)..];
			(T part, bool real)? parseFirst = TryParsePart(firstPart);
			if (!parseFirst.HasValue)
			{
				return false;
			}
			(T part, bool real)? parseLast = TryParsePart(lastPart);
			if (!parseLast.HasValue)
			{
				return false;
			}
			if (!(parseFirst.Value.real ^ parseLast.Value.real))
			{
				return false;
			}
			complex = parseFirst.Value.real ? new Complex<T>(parseFirst.Value.part, parseLast.Value.part) : new Complex<T>(parseLast.Value.part, parseFirst.Value.part);
			return true;
		}

		/// <summary>
		/// Parse a <see cref="string"/> to a new <see cref="Complex{T}"/>
		/// </summary>
		/// <param name="str">string to parse of form "a + b<c>i</c>", "a - b<c>i</c>", "a", "b<c>i</c>" or "-b<c>i</c>" where both 'a' and 'b' are float point numbers</param>
		/// <returns>the parsed <see cref="Complex{T}"/></returns>
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
		public static implicit operator Complex<T>(int a) => new(a.GenericConvert<int, T>());
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
		public static implicit operator Complex<T>((int r, int i) a) => new(a.r.GenericConvert<int, T>(), a.i.GenericConvert<int, T>());
		/// <summary>
		/// Convert from T tuple
		/// </summary>
		/// <param name="a">a T tuple</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator Complex<T>((T r, T i) a) => new(a.r, a.i);

		/// <summary>
		/// Convert to <see cref="double"/> typed complex
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static explicit operator Complex<double>(Complex<T> v) =>
			v switch
			{
				Complex<double> vv => vv,
				_ => new Complex<double>(_toDouble(v.real), _toDouble(v.imag)),
			};

		/// <summary>
		/// Convert from <see cref="double"/> typed complex
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static explicit operator Complex<T>(Complex<double> v) =>
			v switch
			{
				Complex<T> vv => vv,
				_ => new Complex<T>(_fromDouble(v.real), _fromDouble(v.imag)),
			};

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

		#region complex double arithmetic
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Complex<double> DoubleMul(Complex<double> a, double b)
		{
			return new Complex<double>(a.real * b, a.imag * b);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Complex<double> DoubleMul(Complex<double> a, Complex<double> b)
		{
			double real = a.real * b.real - a.imag * b.imag;
			double imag = a.real * b.imag + a.imag * b.real;
			return new Complex<double>(real, imag);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Complex<double> DoubleDiv(Complex<double> a, Complex<double> b)
		{
			double squareAbsY = b.real * b.real + b.imag * b.imag;
			double acbd = a.real * b.real + a.imag * b.imag;
			double bcad = a.imag * b.real - a.real * b.imag;
			return new Complex<double>(acbd /squareAbsY, bcad / squareAbsY);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static double DoubleAbs(Complex<double> a)
		{
			double x = a.real, y = a.imag;
			double squareAbsY = x * x + y * y;
			return Math.Sqrt(squareAbsY);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static double DoubleArg(Complex<double> a)
		{
			return Math.Atan2(a.imag, a.real);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Complex<double> DoubleExp(Complex<double> c)
		{
			double exp = Math.Exp(c.real);
			double cos = Math.Cos(c.imag);
			double sin = Math.Sin(c.imag);
			return new Complex<double>(exp * cos, exp * sin);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Complex<double> DoubleLog(Complex<double> c)
		{
			double real = 0.5 * Math.Log(c.real * c.real + c.imag * c.imag);
			double imag = Math.Atan2(c.imag, c.real);
			return new Complex<double>(real, imag);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Complex<double> DoublePowReal(Complex<double> c, double p)
		{
			if (c.imag == 0)
			{
				return new Complex<double>(Math.Pow(c.real, p));
			}
			else
			{
				double absC = DoubleAbs(c);
				double argC = Math.Atan2(c.imag, c.real);
				double phase = p * argC;
				double scale = Math.Pow(absC, p);
				return new Complex<double>(scale * Math.Cos(phase), scale * Math.Sin(phase));
			}
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Complex<double> DoublePow(Complex<double> c, double p)
		{
			if ((c.real == 0 || c.real == 1) && c.imag == 0)
				return c;
			return DoublePowReal(c, p);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Complex<double> DoublePow(Complex<double> c, Complex<double> p)
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
			return new Complex<double>(scale * Math.Cos(phase), scale * Math.Sin(phase));
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Complex<double> DoubleSqrt(Complex<double> c)
		{
			double arg = 0.5 * DoubleArg(c);
			double scale = Math.Sqrt(DoubleAbs(c));
			double real = Math.Cos(arg);
			double imag = Math.Sin(arg);
			return new Complex<double>(scale * real, scale * imag);
		}
		#endregion

		#region arithmetic operators
		/// <summary>
		/// Complex negate
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Complex<T> operator -(Complex<T> a) => new(_negate(a.real), _negate(a.imag));
		/// <summary>
		/// Complex add
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Complex<T> operator +(Complex<T> a, Complex<T> b) => new(_add(a.real, b.real), _add(a.imag, b.imag));
		/// <summary>
		/// Complex subtract
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Complex<T> operator -(Complex<T> a, Complex<T> b) => new(_sub(a.real, b.real), _sub(a.imag, b.imag));
		/// <summary>
		/// Complex add real
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Complex<T> operator +(Complex<T> a, T b) => new(_add(a.real, b), a.imag);
		/// <summary>
		/// Complex add real
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Complex<T> operator +(T b, Complex<T> a) => new(_add(a.real, b), a.imag);
		/// <summary>
		/// Complex subtract real
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Complex<T> operator -(Complex<T> a, T b) => new(_sub(a.real, b), a.imag);
		/// <summary>
		/// Real subtract complex
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Complex<T> operator -(T b, Complex<T> a) => new(_sub(b, a.real), _negate(a.imag));

		/// <summary>
		/// Complex multiply
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Complex<T> operator *(Complex<T> a, Complex<T> b)
		{
			return (Complex<T>)DoubleMul((Complex<double>)a, (Complex<double>)b);
		}
		/// <summary>
		/// Complex division, guards against intermediate underflow and overflow by scaling
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Complex<T> operator /(Complex<T> a, Complex<T> b)
		{
			return (Complex<T>)DoubleDiv((Complex<double>)a, (Complex<double>)b);
		}
		/// <summary>
		/// Complex multiply real number
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Complex<T> operator *(Complex<T> a, T b) => new(_mul(a.real, b), _mul(a.imag, b));
		/// <summary>
		/// Complex multiply real number
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Complex<T> operator *(T b, Complex<T> a) => new(_mul(a.real, b), _mul(a.imag, b));
		/// <summary>
		/// Complex divide real number
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Complex<T> operator /(Complex<T> a, T b) => new(_div(a.real, b), _div(a.imag, b));
		/// <summary>
		/// Real number divide complex 
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Complex<T> operator /(T b, Complex<T> a) => new Complex<T>(b) / a;

		/// <summary>
		/// Complex absolute value of this complex
		/// </summary>
		/// <returns>The absolute value of this complex</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T Abs()
		{
			return _fromDouble(DoubleAbs((Complex<double>)this));
		}

		/// <summary>
		/// Compute the argument of this complex
		/// </summary>
		/// <returns>The argument of this complex</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T Arg()
		{
			return _fromDouble(DoubleArg((Complex<double>)this));
		}

		/// <summary>
		/// Complex conjugate
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Complex<T> Conjugate() => new(this.real, _negate(this.imag));


		/// <summary>
		/// Complex exponential (of base <c>e</c>)
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Complex<T> Exp()
		{
			var doubleResult = DoubleExp((Complex<double>)this);
			return (Complex<T>)doubleResult;
		}

		/// <summary>
		/// Complex logarithm (of base <c>e</c>)
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Complex<T> Log()
		{
			return (Complex<T>)DoubleLog((Complex<double>)this);
		}

		/// <summary>
		/// Complex number power
		/// </summary>
		/// <param name="p">The power of real type <typeparamref name="T"/></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Complex<T> Pow(T p)
		{
			return (Complex<T>)DoublePow((Complex<double>)this, _toDouble(p));
		}

		/// <summary>
		/// Complex  number power
		/// </summary>
		/// <param name="p">The power of complex type <see cref="Complex{T}"/></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Complex<T> Pow(Complex<T> p)
		{
			return (Complex<T>)DoublePow((Complex<double>)this, (Complex<double>)p);
		}

		/// <summary>
		/// Get the complex square root of this complex
		/// </summary>
		/// <returns>The complex square root of this complex</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Complex<T> Sqrt()
		{
			return (Complex<T>)DoubleSqrt((Complex<double>)this);
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


namespace Althea.Linq
{
	#region complex type array LINQ
	public static partial class ArrayLinq
	{
		/// <summary>
		/// Convert a 1D <typeparamref name="T"/> array to <see cref="Complex{T}"/> array by taking two consecutive real values to form one complex value.
		/// </summary>
		/// <typeparam name="T">The real type</typeparam>
		/// <param name="input">input array of type <typeparamref name="T"/></param>
		/// <returns>a new <see cref="Complex{T}"/> array made out of <paramref name="input"/></returns>
		/// <remarks>extend method of <paramref name="input"/></remarks>
		public static Complex<T>[] FormComplexArray<T>(this T[] input) where T : unmanaged
		{
			long length = input.LongLength / 2;
			var complexArray = new Complex<T>[length];
			for (long i = 0; i < length; i++)
			{
				complexArray[i] = new Complex<T>(input[i * 2], input[i * 2 + 1]);
			}
			return complexArray;
		}

		/// <summary>
		/// Convert a 1D <typeparamref name="T"/> array to <see cref="Complex{T}"/> array by creating complex values with only real parts.
		/// </summary>
		/// <typeparam name="T">The real type</typeparam>
		/// <param name="input">input array of type <typeparamref name="T"/></param>
		/// <returns>a new <see cref="Complex{T}"/> array made out of <paramref name="input"/></returns>
		/// <remarks>extend method of <paramref name="input"/></remarks>
		public static Complex<T>[] ToComplexArray<T>(this T[] input) where T : unmanaged
		{
			var complexArray = new Complex<T>[input.LongLength];
			for (long i = 0; i < input.LongLength; i++)
			{
				complexArray[i] = new Complex<T>(input[i]);
			}
			return complexArray;
		}

		/// <summary>
		/// Complex list product
		/// </summary>
		/// <param name="list"></param>
		/// <returns>Product result, 0 if <paramref name="list"/> is null</returns>
		/// <remarks>extend method of <paramref name="list"/></remarks>
		public static Complex<T> Prod<T>(this IReadOnlyList<Complex<T>> list) where T : unmanaged
		{
			if (list is null || list.Count == 0)
				return 1;
			Complex<T> prod = 1;
			for (int i = 0; i < list.Count; i++)
			{
				prod *= list[i];
			}
			return prod;
		}

		/// <summary>
		/// Complex list summation
		/// </summary>
		/// <param name="list"></param>
		/// <returns>Summation result, 0 if <paramref name="list"/> is null</returns>
		/// <remarks>extend method of <paramref name="list"/></remarks>
		public static Complex<T> Sum<T>(this IReadOnlyList<Complex<T>> list) where T : unmanaged
		{
			if (list is null || list.Count == 0)
				return default;
			Complex<T> sum = default;
			for (int i = 0; i < list.Count; i++)
			{
				sum += list[i];
			}
			return sum;
		}

		/// <summary>
		/// Complex list summation by <paramref name="selector"/>
		/// </summary>
		/// <typeparam name="T">The complex type's real type</typeparam>
		/// <typeparam name="TFrom">The conversion from type</typeparam>
		/// <param name="list"></param>
		/// <param name="selector">The selector to apply to each element</param>
		/// <returns>Summation result, 0 if <paramref name="list"/> is null</returns>
		/// <remarks>extend method of <paramref name="list"/></remarks>
		public static Complex<T> Sum<T, TFrom>(this IReadOnlyList<TFrom> list, Converter<TFrom, Complex<T>> selector) where T : unmanaged
		{
			if (list is null || list.Count == 0)
				return default;
			Complex<T> sum = default;
			for (int i = 0; i < list.Count; i++)
			{
				sum += selector(list[i]);
			}
			return sum;
		}

		/// <summary>
		/// Complex list product by <paramref name="selector"/>
		/// </summary>
		/// <typeparam name="T">The complex type's real type</typeparam>
		/// <typeparam name="TFrom">The conversion from type</typeparam>
		/// <param name="list"></param>
		/// <param name="selector">The selector to apply to each element</param>
		/// <returns>Product result, 0 if <paramref name="list"/> is null</returns>
		/// <remarks>extend method of <paramref name="list"/></remarks>
		public static Complex<T> Prod<T, TFrom>(this IReadOnlyList<TFrom> list, Converter<TFrom, Complex<T>> selector) where T : unmanaged
		{
			if (list is null || list.Count == 0)
				return default;
			Complex<T> sum = default;
			for (int i = 0; i < list.Count; i++)
			{
				sum += selector(list[i]);
			}
			return sum;
		}
	}
	#endregion
}