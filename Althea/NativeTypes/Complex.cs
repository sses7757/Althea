using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Althea.Resources;
using Althea.NativeTypes;


namespace Althea.NativeTypes
{
	#region complex interface
	/// <summary>
	/// The complex interface for any possible real data type
	/// </summary>
	/// <typeparam name="T">the data type of corresponding real number, usually an unmanaged struct that implements <see cref="ICustomNativeType{T}"/></typeparam>
	public interface IComplex<T> : IFormattable where T : unmanaged, IFormattable, IEquatable<T>, IComparable<T>
	{
		/// <summary>
		/// Get the real part
		/// </summary>
		T Real { get; }

		/// <summary>
		/// Get the imaginary part
		/// </summary>
		T Imag { get; }
	}
	#endregion

	#region generic complex type
	/// <summary>
	/// The general complex type for built-in types
	/// </summary>
	/// <typeparam name="T">the data type of corresponding real number</typeparam>
	/// <remarks>This is an <c>unmanaged</c> type since C# 8.0.<br/>
	/// I do not recommend one to use any data type conversions or arithmetic operations in heavy load like loop over a <c><see cref="Complex{T}"/>[]</c> even though the dynamic functions will be optimized by the JIT to have performance way better than boxing and unboxing <typeparamref name="T"/>, they may still perform a lot worse than operations with compile-time-known <typeparamref name="T"/>.</remarks>
	[StructLayout(LayoutKind.Sequential)]
	public struct Complex<T> : IComplex<T>, ICustomNativeType<Complex<T>>, IEquatable<Complex<T>> where T : unmanaged, IFormattable, IEquatable<T>, IComparable<T>
	{
		#region basic
		private readonly T real, imag;

		/// <summary>
		/// Get the real part
		/// </summary>
		public T Real => this.real;

		/// <summary>
		/// Get the imaginary part
		/// </summary>
		public T Imag => this.imag;

		/// <summary>
		/// Constructor from real and imaginary parts
		/// </summary>
		/// <param name="re">real part</param>
		/// <param name="im">imaginary part, default value is <c>default(<typeparamref name="T"/>)</c></param>
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
			_Classification = default(T).GetClassification();
			if (_Classification == DataTypeClassification.NotSupported)
				throw new InvalidOperationException(Support.DataType);
		}

		private static unsafe readonly int _sizeofT = sizeof(T);
		#endregion

		#region constant values
		private static readonly T _oneT = (T)(dynamic)1;
		private static readonly T _minusOneT = (T)(dynamic)(-1);

		/// <summary>
		/// <see cref="Complex{T}"/> 0
		/// </summary>
		public static readonly Complex<T> Zero = new Complex<T>(default);
		/// <summary>
		/// <see cref="Complex{T}"/> 1
		/// </summary>
		public static readonly Complex<T> One = new Complex<T>(_oneT);
		/// <summary>
		/// <see cref="Complex{T}"/> -1
		/// </summary>
		public static readonly Complex<T> MinusOne = new Complex<T>(_minusOneT);
		/// <summary>
		/// <see cref="Complex{T}"/> i
		/// </summary>
		public static readonly Complex<T> ImOne = new Complex<T>(default, _oneT);
		/// <summary>
		/// <see cref="Complex{T}"/> -1
		/// </summary>
		public static readonly Complex<T> MinusImOne = new Complex<T>(default, _minusOneT);
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
				throw new ArgumentException(string.Format(Arithmetic.CannotParseComplex, str, typeof(T).Name), nameof(str));
			return result;
		}
		#endregion

		#region converter
		/// <summary>
		/// Convert from int
		/// </summary>
		/// <param name="a">a int</param>
		public static implicit operator Complex<T>(int a) => new Complex<T>((T)(dynamic)a);
		/// <summary>
		/// Convert from T
		/// </summary>
		/// <param name="a">a T</param>
		public static implicit operator Complex<T>(T a) => new Complex<T>(a);
		/// <summary>
		/// Convert from int tuple
		/// </summary>
		/// <param name="a">a int tuple</param>
		public static implicit operator Complex<T>((int r, int i) a) => new Complex<T>((T)(dynamic)a.r, (T)(dynamic)a.i);
		/// <summary>
		/// Convert from T tuple
		/// </summary>
		/// <param name="a">a T tuple</param>
		public static implicit operator Complex<T>((T r, T i) a) => new Complex<T>(a.r, a.i);

		/// <summary>
		/// Convert to <see cref="double"/> typed complex
		/// </summary>
		public static explicit operator Complex<double>(Complex<T> v) => v switch {
			Complex<double> vv => vv,
			_ => new Complex<double>((double)(dynamic)v.real, (double)(dynamic)v.imag),
		};

		/// <summary>
		/// Convert from <see cref="double"/> typed complex
		/// </summary>
		public static explicit operator Complex<T>(Complex<double> v) => v switch
		{
			Complex<T> vv => vv,
			_ => new Complex<T>((T)(dynamic)v.real, (T)(dynamic)v.imag),
		};

		/// <summary>
		/// Convert to <typeparamref name="T"/>
		/// </summary>
		public static explicit operator T(Complex<T> v) => v.Abs();
		#endregion

		#region equality
		/// <summary>
		/// Equal operator
		/// </summary>
		public static bool operator ==(Complex<T> a, Complex<T> b) => a.real.Equals(b.real) && a.imag.Equals(b.imag);

		/// <summary>
		/// Not-equal operator
		/// </summary>
		public static bool operator !=(Complex<T> a, Complex<T> b) => !(a == b);

		/// <summary>
		/// Equality operator
		/// </summary>
		/// <param name="another">another <see cref="Complex{T}"/></param>
		/// <returns>this == <paramref name="another"/></returns>
		public bool Equals(Complex<T> another)
		{
			return this == another;
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
		public override bool Equals(object obj)
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
			return this == a;
		}
		#endregion

		#region arithmetic operators
		/// <summary>
		/// Complex negate
		/// </summary>
		public static Complex<T> operator -(Complex<T> a) => new Complex<T>(-(dynamic)a.real, -(dynamic)a.imag);

		/// <summary>
		/// Complex add
		/// </summary>
		public static Complex<T> operator +(Complex<T> a, Complex<T> b) => new Complex<T>(a.real + (dynamic)b.real, a.imag + (dynamic)b.imag);
		/// <summary>
		/// Complex subtract
		/// </summary>
		public static Complex<T> operator -(Complex<T> a, Complex<T> b) => new Complex<T>(a.real - (dynamic)b.real, a.imag - (dynamic)b.imag);
		/// <summary>
		/// Complex add real
		/// </summary>
		public static Complex<T> operator +(Complex<T> a, T b) => new Complex<T>(a.real + (dynamic)b, a.imag);
		/// <summary>
		/// Complex add real
		/// </summary>
		public static Complex<T> operator +(T b, Complex<T> a) => new Complex<T>(a.real + (dynamic)b, a.imag);
		/// <summary>
		/// Complex subtract real
		/// </summary>
		public static Complex<T> operator -(Complex<T> a, T b) => new Complex<T>(a.real - (dynamic)b, a.imag);
		/// <summary>
		/// Real subtract complex
		/// </summary>
		public static Complex<T> operator -(T b, Complex<T> a) => new Complex<T>(b - (dynamic)a.real, -(dynamic)a.imag);

		/// <summary>
		/// Complex multiply
		/// </summary>
		public static Complex<T> operator *(Complex<T> a, Complex<T> b)
		{
			T real = a.real * (dynamic)b.real - a.imag * (dynamic)b.imag;
			T imag = a.real * (dynamic)b.imag + a.imag * (dynamic)b.real;
			return new Complex<T>(real, imag);
		}
		/// <summary>
		/// Complex division, guards against intermediate underflow and overflow by scaling
		/// </summary>
		public static Complex<T> operator /(Complex<T> x, Complex<T> y)
		{
			dynamic dyr = (dynamic)y.real, dyi = (dynamic)y.imag;
			dynamic squareAbsY = dyr * dyr + dyi * dyi;
			dynamic acbd = x.real * dyr + x.imag * dyi;
			dynamic bcad = x.imag * dyr - x.real * dyi;
			return new Complex<T>(acbd / squareAbsY, bcad / squareAbsY);
		}
		/// <summary>
		/// Complex multiply real number
		/// </summary>
		public static Complex<T> operator *(Complex<T> a, T b) => new Complex<T>((dynamic)a.real * b, (dynamic)a.imag * b);
		/// <summary>
		/// Complex multiply real number
		/// </summary>
		public static Complex<T> operator *(T b, Complex<T> a) => new Complex<T>((dynamic)a.real * b, (dynamic)a.imag * b);
		/// <summary>
		/// Complex divide real number
		/// </summary>
		public static Complex<T> operator /(Complex<T> a, T b) => new Complex<T>((dynamic)a.real / b, (dynamic)a.imag / b);
		/// <summary>
		/// Real number divide complex 
		/// </summary>
		public static Complex<T> operator /(T b, Complex<T> a) => new Complex<T>(b) / a;

		/// <summary>
		/// Complex absolute value
		/// </summary>
		public T Abs()
		{
			dynamic r = this.real, i = this.imag;
			return (T)(dynamic)Math.Sqrt((double)(r * r + i * i));
		}

		/// <summary>
		/// Complex conjugate
		/// </summary>
		public Complex<T> Conjugate() => new Complex<T>(this.real, -(dynamic)this.imag);

		private static Complex<double> Exp(Complex<double> c)
		{
			double exp = Math.Exp(c.real);
			double cos = Math.Cos(c.imag);
			double sin = Math.Sin(c.imag);
			return new Complex<double>(exp * cos, exp * sin);
		}

		/// <summary>
		/// Complex exponential (of base <c>e</c>)
		/// </summary>
		public Complex<T> Exp()
		{
			var doubleResult = Exp((Complex<double>)this);
			return (Complex<T>)doubleResult;
		}

		private static Complex<double> Log(Complex<double> c)
		{
			double real = 0.5 * Math.Log(c.real * c.real + c.imag * c.imag);
			double imag = Math.Atan2(c.real, c.imag);
			return new Complex<double>(real, imag);
		}

		/// <summary>
		/// Complex logarithm (of base <c>e</c>)
		/// </summary>
		public Complex<T> Log()
		{
			var doubleResult = Log((Complex<double>)this);
			return (Complex<T>)doubleResult;
		}

		/// <summary>
		/// Complex number power
		/// </summary>
		/// <param name="p">the power of real type <typeparamref name="T"/></param>
		public Complex<T> Pow(T p)
		{
			if (this.imag.Equals(default))
			{
				return new Complex<T>(Math.Pow((dynamic)this.real, (dynamic)p));
			}
			else
			{
				double dp = (double)(dynamic)p;
				var doubleResult = Log((Complex<double>)this);
				doubleResult *= dp;
				doubleResult = Exp(doubleResult);
				return (Complex<T>)doubleResult;
			}
		}

		/// <summary>
		/// Complex  number power
		/// </summary>
		/// <param name="p">the power of complex type <see cref="Complex{T}"/></param>
		public Complex<T> Pow(Complex<T> p)
		{
			if (p.imag.Equals(default))
			{
				return this.Pow(p.real);
			}
			Complex<double> result;
			if (this.imag.Equals(default) && this.real.CompareTo(default) > 0)
			{
				result = Math.Log((double)(dynamic)this.real) * (Complex<double>)p;
			}
			else
			{
				result = Log((Complex<double>)this) * (Complex<double>)p;
			}
			result = Exp(result);
			return (Complex<T>)result;
		}

		/// <summary>
		/// Out-of-place add <paramref name="another"/> value of <see cref="Complex{T}"/>
		/// </summary>
		/// <param name="another">another value to be added</param>
		/// <returns>The addition result</returns>
		public Complex<T> Add(Complex<T> another) => this + another;

		/// <summary>
		/// Out-of-place subtract <paramref name="another"/> value of <see cref="Complex{T}"/>
		/// </summary>
		/// <param name="another">another value to be subtracted</param>
		/// <returns>The subtraction result</returns>
		public Complex<T> Subtract(Complex<T> another) => this - another;

		/// <summary>
		/// Out-of-place multiply <paramref name="another"/> value of <see cref="Complex{T}"/>
		/// </summary>
		/// <param name="another">another value to be multiplied</param>
		/// <returns>The multiplication result</returns>
		public Complex<T> Multiply(Complex<T> another) => this * another;

		/// <summary>
		/// Out-of-place divide <paramref name="another"/> value of <see cref="Complex{T}"/>
		/// </summary>
		/// <param name="another">another value to be divided</param>
		/// <returns>The division result</returns>
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
		public string ToString(string format)
		{
			return this.ToString(format, Resource.Culture);
		}

		/// <summary>
		/// Realization of <see cref="IFormattable.ToString(string, IFormatProvider)"/>
		/// </summary>
		/// <param name="format">format of output</param>
		/// <param name="formatProvider">The provider to use to format the value.</param>
		public string ToString(string format, IFormatProvider formatProvider = null)
		{
			formatProvider ??= Resource.Culture;
			string r = this.real.ToString(format, formatProvider);
			string i = this.imag.ToString(format, formatProvider);
			return $"({r},{i})";
		}
		#endregion
	}
	#endregion
}


namespace Althea.Linq
{
	// complex type array LINQ
	public static partial class ArrayLinq
	{
		/// <summary>
		/// Convert a 1D <typeparamref name="T"/> array to <see cref="Complex{T}"/> array by taking two consecutive real values to form one complex value.
		/// </summary>
		/// <typeparam name="T">the real type</typeparam>
		/// <param name="input">input array of type <typeparamref name="T"/></param>
		/// <returns>a new <see cref="Complex{T}"/> array made out of <paramref name="input"/></returns>
		/// <remarks>extend method of <paramref name="input"/></remarks>
		public static Complex<T>[] FormComplexArray<T>(this T[] input) where T : unmanaged, IFormattable, IEquatable<T>, IComparable<T>
		{
			if (input is null)
				throw new ArgumentNullException(nameof(input));
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
		/// <typeparam name="T">the real type</typeparam>
		/// <param name="input">input array of type <typeparamref name="T"/></param>
		/// <returns>a new <see cref="Complex{T}"/> array made out of <paramref name="input"/></returns>
		/// <remarks>extend method of <paramref name="input"/></remarks>
		public static Complex<T>[] ToComplexArray<T>(this T[] input) where T : unmanaged, IFormattable, IEquatable<T>, IComparable<T>
		{
			if (input is null)
				throw new ArgumentNullException(nameof(input));
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
		public static Complex<T> Prod<T>(this IReadOnlyList<Complex<T>> list) where T : unmanaged, IFormattable, IEquatable<T>, IComparable<T>
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
		public static Complex<T> Sum<T>(this IReadOnlyList<Complex<T>> list) where T : unmanaged, IFormattable, IEquatable<T>, IComparable<T>
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
		/// <typeparam name="T">the complex type's real type</typeparam>
		/// <typeparam name="TFrom">the conversion from type</typeparam>
		/// <param name="list"></param>
		/// <param name="selector">the selector to apply to each element</param>
		/// <returns>Summation result, 0 if <paramref name="list"/> is null</returns>
		/// <remarks>extend method of <paramref name="list"/></remarks>
		public static Complex<T> Sum<T, TFrom>(this IReadOnlyList<TFrom> list, Converter<TFrom, Complex<T>> selector) where T : unmanaged, IFormattable, IEquatable<T>, IComparable<T>
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
		/// <typeparam name="T">the complex type's real type</typeparam>
		/// <typeparam name="TFrom">the conversion from type</typeparam>
		/// <param name="list"></param>
		/// <param name="selector">the selector to apply to each element</param>
		/// <returns>Product result, 0 if <paramref name="list"/> is null</returns>
		/// <remarks>extend method of <paramref name="list"/></remarks>
		public static Complex<T> Prod<T, TFrom>(this IReadOnlyList<TFrom> list, Converter<TFrom, Complex<T>> selector) where T : unmanaged, IFormattable, IEquatable<T>, IComparable<T>
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
}