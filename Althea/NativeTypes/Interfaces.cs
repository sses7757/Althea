using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;


// This file is added since the generic math feature is not currently available in .NET 6.0
namespace System
{
	#region base
	/// <summary>
	/// The interface for a parseable number <typeparamref name="TSelf"/>
	/// </summary>
	/// <typeparam name="TSelf">The type of the parseable number</typeparam>
	public interface IParseable<TSelf> where TSelf : IParseable<TSelf>
	{
		/// <summary>
		/// Converts the string representation of a number in a specified culture-specific format to its <typeparamref name="TSelf"/> number equivalent.
		/// </summary>
		/// <param name="s">A string that contains a number to convert.</param>
		/// <param name="provider">An object that supplies culture-specific formatting information about <paramref name="s"/>.</param>
		/// <returns>A <typeparamref name="TSelf"/> number that is equivalent to the numeric value or symbol specified in <paramref name="s"/>.</returns>
		abstract static TSelf Parse(string? s, IFormatProvider? provider);

		/// <summary>
		/// Converts the string representation of a number to its <typeparamref name="TSelf"/> number equivalent.
		/// </summary>
		/// <param name="s">A string that contains a number to convert.</param>
		/// <returns>A <typeparamref name="TSelf"/> number that is equivalent to the numeric value or symbol specified in <paramref name="s"/>.</returns>
		abstract static TSelf Parse(string? s);

		/// <summary>
		/// Converts the string representation of a number in a culture-specific format to its <typeparamref name="TSelf"/> number equivalent.
		/// </summary>
		/// <param name="s">A string that contains a number to convert.</param>
		/// <param name="provider">An object that supplies culture-specific formatting information about <paramref name="s"/>.</param>
		/// <param name="result">A <typeparamref name="TSelf"/> number that is equivalent to the numeric value or symbol specified in <paramref name="s"/>.</param>
		/// <returns>A return value indicates whether the conversion succeeded or failed.</returns>
		abstract static bool TryParse(string? s, IFormatProvider? provider, out TSelf result);

		/// <summary>
		/// Converts the string representation of a number to its <typeparamref name="TSelf"/> number equivalent.
		/// </summary>
		/// <param name="s">A string that contains a number to convert.</param>
		/// <param name="result">A <typeparamref name="TSelf"/> number that is equivalent to the numeric value or symbol specified in <paramref name="s"/>.</param>
		/// <returns>A return value indicates whether the conversion succeeded or failed.</returns>
		abstract static bool TryParse(string? s, out TSelf result);
	}

	/// <summary>
	/// The interface for equality operator between <typeparamref name="TSelf"/> and <typeparamref name="TOther"/>
	/// </summary>
	/// <typeparam name="TSelf">The actual type of implemented struct/class</typeparam>
	/// <typeparam name="TOther">The type of the other thing to compare</typeparam>
	public interface IEqualityOperators<TSelf, TOther> : IEquatable<TOther> where TSelf : IEqualityOperators<TSelf, TOther>, IEquatable<TOther>
	{
		/// <summary>
		/// Abstract static equality operator between <typeparamref name="TSelf"/> and <typeparamref name="TOther"/>
		/// </summary>
		/// <param name="left">The first operand to compare of type <typeparamref name="TSelf"/></param>
		/// <param name="right">The second operand to compare of type <typeparamref name="TOther"/></param>
		/// <returns><paramref name="left"/> == <paramref name="right"/></returns>
		abstract static bool operator ==(TSelf left, TOther right);

		/// <summary>
		/// Abstract static inequality operator between <typeparamref name="TSelf"/> and <typeparamref name="TOther"/>
		/// </summary>
		/// <param name="left">The first operand to compare of type <typeparamref name="TSelf"/></param>
		/// <param name="right">The second operand to compare of type <typeparamref name="TOther"/></param>
		/// <returns><paramref name="left"/> != <paramref name="right"/></returns>
		abstract static bool operator !=(TSelf left, TOther right);
	}

	/// <summary>
	/// The interface for comparison operators between <typeparamref name="TSelf"/> and <typeparamref name="TOther"/>
	/// </summary>
	/// <typeparam name="TSelf">The actual type of implemented struct/class</typeparam>
	/// <typeparam name="TOther">The type of the other thing to compare</typeparam>
	public interface IComparisonOperators<TSelf, TOther> : IComparable, IComparable<TOther>, IEqualityOperators<TSelf, TOther>
		where TSelf : IComparisonOperators<TSelf, TOther>, IComparable, IComparable<TOther>, IEqualityOperators<TSelf, TOther>
	{
		/// <summary>
		/// Abstract static less-than operator between <typeparamref name="TSelf"/> and <typeparamref name="TOther"/>
		/// </summary>
		/// <param name="left">The first operand to compare of type <typeparamref name="TSelf"/></param>
		/// <param name="right">The second operand to compare of type <typeparamref name="TOther"/></param>
		/// <returns><paramref name="left"/> &lt; <paramref name="right"/></returns>
		abstract static bool operator <(TSelf left, TOther right);

		/// <summary>
		/// Abstract static less-than-or-equals-to operator between <typeparamref name="TSelf"/> and <typeparamref name="TOther"/>
		/// </summary>
		/// <param name="left">The first operand to compare of type <typeparamref name="TSelf"/></param>
		/// <param name="right">The second operand to compare of type <typeparamref name="TOther"/></param>
		/// <returns><paramref name="left"/> &lt;= <paramref name="right"/></returns>
		abstract static bool operator <=(TSelf left, TOther right);

		/// <summary>
		/// Abstract static greater-than operator between <typeparamref name="TSelf"/> and <typeparamref name="TOther"/>
		/// </summary>
		/// <param name="left">The first operand to compare of type <typeparamref name="TSelf"/></param>
		/// <param name="right">The second operand to compare of type <typeparamref name="TOther"/></param>
		/// <returns><paramref name="left"/> &gt; <paramref name="right"/></returns>
		abstract static bool operator >(TSelf left, TOther right);

		/// <summary>
		/// Abstract static greater-than-or-equals-to operator between <typeparamref name="TSelf"/> and <typeparamref name="TOther"/>
		/// </summary>
		/// <param name="left">The first operand to compare of type <typeparamref name="TSelf"/></param>
		/// <param name="right">The second operand to compare of type <typeparamref name="TOther"/></param>
		/// <returns><paramref name="left"/> &gt;= <paramref name="right"/></returns>
		abstract static bool operator >=(TSelf left, TOther right);
	}

	/// <summary>
	/// The interface for types with additive identity
	/// </summary>
	/// <typeparam name="TSelf">The actual type of implemented struct/class</typeparam>
	/// <typeparam name="TResult">The type of the additive identity</typeparam>
	public interface IAdditiveIdentity<TSelf, TResult> where TSelf : IAdditiveIdentity<TSelf, TResult>
	{
		/// <summary>
		/// Abstract static get the additive identity of a <typeparamref name="TSelf"/> as a <typeparamref name="TResult"/>
		/// </summary>
		abstract static TResult AdditiveIdentity { get; }
	}

	/// <summary>
	/// The interface for types with multiplicative identity
	/// </summary>
	/// <typeparam name="TSelf">The actual type of implemented struct/class</typeparam>
	/// <typeparam name="TResult">The type of the multiplicative identity</typeparam>
	public interface IMultiplicativeIdentity<TSelf, TResult> where TSelf : IAdditiveIdentity<TSelf, TResult>
	{
		/// <summary>
		/// Abstract static get the multiplicative identity of a <typeparamref name="TSelf"/> as a <typeparamref name="TResult"/>
		/// </summary>
		abstract static TResult MultiplicativeIdentity { get; }
	}

	/// <summary>
	/// The interface for addition operator(s)
	/// </summary>
	/// <typeparam name="TSelf">The actual type of implemented struct/class</typeparam>
	/// <typeparam name="TOther">The type of the other operand</typeparam>
	/// <typeparam name="TResult">The type of the result</typeparam>
	public interface IAdditionOperators<TSelf, TOther, TResult> where TSelf : IAdditionOperators<TSelf, TOther, TResult>
	{
		/// <summary>
		/// Abstract static addition operator for two operands <paramref name="left"/> and <paramref name="right"/>
		/// </summary>
		/// <param name="left">The first operand of type <typeparamref name="TSelf"/></param>
		/// <param name="right">The second operand of type <typeparamref name="TOther"/></param>
		/// <returns>The addition result of type <typeparamref name="TResult"/></returns>
		abstract static TResult operator +(TSelf left, TOther right);

		/// <summary>
		/// Add this number as a <typeparamref name="TSelf"/> with the <paramref name="other"/> number of type <typeparamref name="TOther"/>
		/// </summary>
		/// <param name="other">The second operand of type <typeparamref name="TOther"/></param>
		/// <returns>The addition result of type <typeparamref name="TResult"/></returns>
		public TResult Add(TOther other) => (TSelf)this + other;
	}

	/// <summary>
	/// The interface for subtraction operator(s)
	/// </summary>
	/// <typeparam name="TSelf">The actual type of implemented struct/class</typeparam>
	/// <typeparam name="TOther">The type of the other operand</typeparam>
	/// <typeparam name="TResult">The type of the result</typeparam>
	public interface ISubtractionOperators<TSelf, TOther, TResult> where TSelf : ISubtractionOperators<TSelf, TOther, TResult>
	{
		/// <summary>
		/// Abstract static subtraction operator for two operands <paramref name="left"/> and <paramref name="right"/>
		/// </summary>
		/// <param name="left">The first operand of type <typeparamref name="TSelf"/></param>
		/// <param name="right">The second operand of type <typeparamref name="TOther"/></param>
		/// <returns>The subtraction result of type <typeparamref name="TResult"/></returns>
		abstract static TResult operator -(TSelf left, TOther right);

		/// <summary>
		/// Subtract this number as a <typeparamref name="TSelf"/> with the <paramref name="other"/> number of type <typeparamref name="TOther"/>
		/// </summary>
		/// <param name="other">The second operand of type <typeparamref name="TOther"/></param>
		/// <returns>The subtraction result of type <typeparamref name="TResult"/></returns>
		public TResult Subtract(TOther other) => (TSelf)this - other;
	}

	/// <summary>
	/// The interface for multiply operator(s)
	/// </summary>
	/// <typeparam name="TSelf">The actual type of implemented struct/class</typeparam>
	/// <typeparam name="TOther">The type of the other operand</typeparam>
	/// <typeparam name="TResult">The type of the result</typeparam>
	public interface IMultiplyOperators<TSelf, TOther, TResult> where TSelf : IMultiplyOperators<TSelf, TOther, TResult>
	{
		/// <summary>
		/// Abstract static multiply operator for two operands <paramref name="left"/> and <paramref name="right"/>
		/// </summary>
		/// <param name="left">The first operand of type <typeparamref name="TSelf"/></param>
		/// <param name="right">The second operand of type <typeparamref name="TOther"/></param>
		/// <returns>The multiplication result of type <typeparamref name="TResult"/></returns>
		abstract static TResult operator *(TSelf left, TOther right);

		/// <summary>
		/// Multiply this number as a <typeparamref name="TSelf"/> with the <paramref name="other"/> number of type <typeparamref name="TOther"/>
		/// </summary>
		/// <param name="other">The second operand of type <typeparamref name="TOther"/></param>
		/// <returns>The multiplication result of type <typeparamref name="TResult"/></returns>
		public TResult Multiply(TOther other) => (TSelf)this * other;
	}

	/// <summary>
	/// The interface for division operator(s)
	/// </summary>
	/// <typeparam name="TSelf">The actual type of implemented struct/class</typeparam>
	/// <typeparam name="TOther">The type of the other operand</typeparam>
	/// <typeparam name="TResult">The type of the result</typeparam>
	public interface IDivisionOperators<TSelf, TOther, TResult> where TSelf : IDivisionOperators<TSelf, TOther, TResult>
	{
		/// <summary>
		/// Abstract static division operator for two operands <paramref name="left"/> and <paramref name="right"/>
		/// </summary>
		/// <param name="left">The first operand of type <typeparamref name="TSelf"/></param>
		/// <param name="right">The second operand of type <typeparamref name="TOther"/></param>
		/// <returns>The division result of type <typeparamref name="TResult"/></returns>
		abstract static TResult operator /(TSelf left, TOther right);

		/// <summary>
		/// Divide this number as a <typeparamref name="TSelf"/> with the <paramref name="other"/> number of type <typeparamref name="TOther"/>
		/// </summary>
		/// <param name="other">The second operand of type <typeparamref name="TOther"/></param>
		/// <returns>The division result of type <typeparamref name="TResult"/></returns>
		public TResult Divide(TOther other) => (TSelf)this / other;
	}

	/// <summary>
	/// The interface for modulus operator(s)
	/// </summary>
	/// <typeparam name="TSelf">The actual type of implemented struct/class</typeparam>
	/// <typeparam name="TOther">The type of the other operand</typeparam>
	/// <typeparam name="TResult">The type of the result</typeparam>
	/// <remarks>This operator does not necessarily behaves the same as remainder for negative numbers</remarks>
	public interface IModulusOperators<TSelf, TOther, TResult> where TSelf : IModulusOperators<TSelf, TOther, TResult>
	{
		/// <summary>
		/// Abstract static modulus operator for two operands <paramref name="left"/> and <paramref name="right"/>
		/// </summary>
		/// <param name="left">The first operand of type <typeparamref name="TSelf"/></param>
		/// <param name="right">The second operand of type <typeparamref name="TOther"/></param>
		/// <returns>The modulus result of type <typeparamref name="TResult"/></returns>
		abstract static TResult operator %(TSelf left, TOther right);

		/// <summary>
		/// Mod this number as a <typeparamref name="TSelf"/> with the <paramref name="other"/> number of type <typeparamref name="TOther"/>
		/// </summary>
		/// <param name="other">The second operand of type <typeparamref name="TOther"/></param>
		/// <returns>The modulus result of type <typeparamref name="TResult"/></returns>
		public TResult Mod(TOther other) => (TSelf)this % other;
	}

	/// <summary>
	/// The interface for increment operator(s)
	/// </summary>
	/// <typeparam name="TSelf">The actual type of implemented struct/class</typeparam>
	public interface IIncrementOperators<TSelf> where TSelf : IIncrementOperators<TSelf>
	{
		/// <summary>
		/// Abstract static increment operator for one operand <paramref name="value"/>
		/// </summary>
		/// <param name="value">The operand to be incremented of type <typeparamref name="TSelf"/></param>
		/// <returns>The increment result of type <typeparamref name="TSelf"/></returns>
		abstract static TSelf operator ++(TSelf value);
	}

	/// <summary>
	/// The interface for decrement operator(s)
	/// </summary>
	/// <typeparam name="TSelf">The actual type of implemented struct/class</typeparam>
	public interface IDecrementOperators<TSelf> where TSelf : IDecrementOperators<TSelf>
	{
		/// <summary>
		/// Abstract static decrement operator for one operand <paramref name="value"/>
		/// </summary>
		/// <param name="value">The operand to be incremented of type <typeparamref name="TSelf"/></param>
		/// <returns>The decrement result of type <typeparamref name="TSelf"/></returns>
		abstract static TSelf operator --(TSelf value);
	}

	/// <summary>
	/// The interface for unary negation operator(s)
	/// </summary>
	/// <typeparam name="TSelf">The actual type of implemented struct/class</typeparam>
	/// <typeparam name="TResult">The type of the result</typeparam>
	public interface IUnaryNegationOperators<TSelf, TResult> where TSelf : IUnaryNegationOperators<TSelf, TResult>
	{
		/// <summary>
		/// Abstract static unary negation operator for one operand <paramref name="value"/>
		/// </summary>
		/// <param name="value">The operand to be incremented of type <typeparamref name="TSelf"/></param>
		/// <returns>The unary negation result of type <typeparamref name="TResult"/></returns>
		abstract static TResult operator -(TSelf value);

		/// <summary>
		/// Negate this number as a <typeparamref name="TSelf"/> with
		/// </summary>
		/// <returns>The unary negation result of type <typeparamref name="TResult"/></returns>
		public TResult Negate() => -(TSelf)this;
	}

	/// <summary>
	/// The interface for unary plus operator(s)
	/// </summary>
	/// <typeparam name="TSelf">The actual type of implemented struct/class</typeparam>
	/// <typeparam name="TResult">The type of the result</typeparam>
	public interface IUnaryPlusOperators<TSelf, TResult> where TSelf : IUnaryPlusOperators<TSelf, TResult>
	{
		/// <summary>
		/// Abstract static unary plus operator for one operand <paramref name="value"/>
		/// </summary>
		/// <param name="value">The operand to be incremented of type <typeparamref name="TSelf"/></param>
		/// <returns>The unary plus result of type <typeparamref name="TResult"/></returns>
		abstract static TResult operator +(TSelf value);

		/// <summary>
		/// Unary plus this number as a <typeparamref name="TSelf"/> with
		/// </summary>
		/// <returns>The unary plus result of type <typeparamref name="TResult"/></returns>
		public TResult UnaryPlus() => +(TSelf)this;
	}

	/// <summary>
	/// The interface for bitwise operators
	/// </summary>
	/// <typeparam name="TSelf">The actual type of implemented struct/class</typeparam>
	/// <typeparam name="TOther">The type of the other operand</typeparam>
	/// <typeparam name="TResult">The type of the result</typeparam>
	public interface IBitwiseOperators<TSelf, TOther, TResult> where TSelf : IBitwiseOperators<TSelf, TOther, TResult>
	{
		/// <summary>
		/// Abstract static bitwise AND operator for two operands <paramref name="left"/> and <paramref name="right"/>
		/// </summary>
		/// <param name="left">The first operand of type <typeparamref name="TSelf"/></param>
		/// <param name="right">The second operand of type <typeparamref name="TOther"/></param>
		/// <returns>The bitwise AND result of type <typeparamref name="TResult"/></returns>
		abstract static TResult operator &(TSelf left, TOther right);

		/// <summary>
		/// Abstract static bitwise OR operator for two operands <paramref name="left"/> and <paramref name="right"/>
		/// </summary>
		/// <param name="left">The first operand of type <typeparamref name="TSelf"/></param>
		/// <param name="right">The second operand of type <typeparamref name="TOther"/></param>
		/// <returns>The bitwise OR result of type <typeparamref name="TResult"/></returns>
		abstract static TResult operator |(TSelf left, TOther right);

		/// <summary>
		/// Abstract static bitwise XOR operator for two operands <paramref name="left"/> and <paramref name="right"/>
		/// </summary>
		/// <param name="left">The first operand of type <typeparamref name="TSelf"/></param>
		/// <param name="right">The second operand of type <typeparamref name="TOther"/></param>
		/// <returns>The bitwise XOR result of type <typeparamref name="TResult"/></returns>
		abstract static TResult operator ^(TSelf left, TOther right);

		/// <summary>
		/// Abstract static bitwise NOT operator for one operand <paramref name="value"/>
		/// </summary>
		/// <param name="value">The operand of type <typeparamref name="TSelf"/></param>
		/// <returns>The bitwise NOT result of type <typeparamref name="TResult"/></returns>
		abstract static TResult operator ~(TSelf value);

		/// <summary>
		/// Bitwise AND this number as a <typeparamref name="TSelf"/> with the <paramref name="other"/> number of type <typeparamref name="TOther"/>
		/// </summary>
		/// <param name="other">The second operand of type <typeparamref name="TOther"/></param>
		/// <returns>The bitwise AND result of type <typeparamref name="TResult"/></returns>
		public TResult BitwiseAnd(TOther other) => (TSelf)this & other;

		/// <summary>
		/// Bitwise OR this number as a <typeparamref name="TSelf"/> with the <paramref name="other"/> number of type <typeparamref name="TOther"/>
		/// </summary>
		/// <param name="other">The second operand of type <typeparamref name="TOther"/></param>
		/// <returns>The bitwise OR result of type <typeparamref name="TResult"/></returns>
		public TResult BitwiseOr(TOther other) => (TSelf)this | other;

		/// <summary>
		/// Bitwise XOR this number as a <typeparamref name="TSelf"/> with the <paramref name="other"/> number of type <typeparamref name="TOther"/>
		/// </summary>
		/// <param name="other">The second operand of type <typeparamref name="TOther"/></param>
		/// <returns>The bitwise XOR result of type <typeparamref name="TResult"/></returns>
		public TResult BitwiseXor(TOther other) => (TSelf)this ^ other;

		/// <summary>
		/// Bitwise NOT this number as a <typeparamref name="TSelf"/>
		/// </summary>
		/// <returns>The bitwise NOT result of type <typeparamref name="TResult"/></returns>
		public TResult BitwiseNot() => ~(TSelf)this;
	}

	/// <summary>
	/// The interface for shift operators
	/// </summary>
	/// <typeparam name="TSelf">The actual type of implemented struct/class</typeparam>
	/// <typeparam name="TResult">The type of the result</typeparam>
	public interface IShiftOperators<TSelf, TResult> where TSelf : IShiftOperators<TSelf, TResult>
	{
		/// <summary>
		/// Abstract static left-shift operator for two operands <paramref name="value"/> and <paramref name="shiftAmount"/>
		/// </summary>
		/// <param name="value">The first operand of type <typeparamref name="TSelf"/></param>
		/// <param name="shiftAmount">The second operand of type <see cref="int"/></param>
		/// <returns>The left shift result of type <typeparamref name="TResult"/></returns>
		abstract static TResult operator <<(TSelf value, int shiftAmount);

		/// <summary>
		/// Abstract static right-shift operator for two operands <paramref name="value"/> and <paramref name="shiftAmount"/>
		/// </summary>
		/// <param name="value">The first operand of type <typeparamref name="TSelf"/></param>
		/// <param name="shiftAmount">The second operand of type <see cref="int"/></param>
		/// <returns>The right shift result of type <typeparamref name="TResult"/></returns>
		abstract static TResult operator >>(TSelf value, int shiftAmount);

		/// <summary>
		/// Left shift this number as a <typeparamref name="TSelf"/> by a <paramref name="shiftAmount"/>
		/// </summary>
		/// <param name="shiftAmount">The second operand of type <see cref="int"/></param>
		/// <returns>The left shift result of type <typeparamref name="TResult"/></returns>
		public TResult LeftShift(int shiftAmount) => (TSelf)this << shiftAmount;

		/// <summary>
		/// Right shift this number as a <typeparamref name="TSelf"/> by a <paramref name="shiftAmount"/>
		/// </summary>
		/// <param name="shiftAmount">The second operand of type <see cref="int"/></param>
		/// <returns>The right shift result of type <typeparamref name="TResult"/></returns>
		public TResult RightShift(int shiftAmount) => (TSelf)this >> shiftAmount;
	}

	/// <summary>
	/// The interface for min and max values
	/// </summary>
	/// <typeparam name="TSelf">The actual type of implemented struct/class</typeparam>
	public interface IMinMaxValue<TSelf> where TSelf : IMinMaxValue<TSelf>
	{
		/// <summary>
		/// Abstract static get the min value of <typeparamref name="TSelf"/>
		/// </summary>
		abstract static TSelf MinValue { get; }

		/// <summary>
		/// Abstract static get the max value of <typeparamref name="TSelf"/>
		/// </summary>
		abstract static TSelf MaxValue { get; }
	}

	/// <summary>
	/// The interface for convertible-to types
	/// </summary>
	/// <typeparam name="TSelf">The actual type of implemented struct/class</typeparam>
	/// <typeparam name="TResult">The type to convert to</typeparam>
	public interface IConvertibleTo<in TSelf, out TResult> where TSelf : IConvertibleTo<TSelf, TResult>
	{
		/// <summary>
		/// Abstract static explicitly convert a <typeparamref name="TSelf"/> <paramref name="value"/> to a <typeparamref name="TResult"/>
		/// </summary>
		/// <param name="value">The value to be converted of type <typeparamref name="TSelf"/></param>
		abstract static explicit operator TResult(TSelf value);
	}

	/// <summary>
	/// The interface for convertible-from types
	/// </summary>
	/// <typeparam name="TSelf">The actual type of implemented struct/class</typeparam>
	/// <typeparam name="TFrom">The type to convert from</typeparam>
	public interface IConvertibleFrom<out TSelf, in TFrom> where TSelf : IConvertibleFrom<TSelf, TFrom>
	{
		/// <summary>
		/// Abstract static explicitly convert a <typeparamref name="TFrom"/> <paramref name="value"/> to a <typeparamref name="TSelf"/>
		/// </summary>
		/// <param name="value">The value to be converted of type <typeparamref name="TFrom"/></param>
		abstract static explicit operator TSelf(TFrom value);
	}
	#endregion


	#region number
	/// <summary>
	/// The interface for native-type convertible types
	/// </summary>
	/// <typeparam name="TSelf">The actual type of implemented struct/class</typeparam>
	public interface INativeConvertibleNumber<TSelf> :
		IConvertibleTo<TSelf, byte>, IConvertibleFrom<TSelf, byte>,
		IConvertibleTo<TSelf, short>, IConvertibleFrom<TSelf, short>,
		IConvertibleTo<TSelf, int>, IConvertibleFrom<TSelf, int>,
		IConvertibleTo<TSelf, long>, IConvertibleFrom<TSelf, long>,
		IConvertibleTo<TSelf, nint>, IConvertibleFrom<TSelf, nint>,
		IConvertibleTo<TSelf, Half>, IConvertibleFrom<TSelf, Half>,
		IConvertibleTo<TSelf, float>, IConvertibleFrom<TSelf, float>,
		IConvertibleTo<TSelf, double>, IConvertibleFrom<TSelf, double>
		where TSelf : INativeConvertibleNumber<TSelf>
	{
	}

	/// <summary>
	/// The base interface for numbers
	/// </summary>
	/// <typeparam name="TSelf">The actual type of implemented number struct/class</typeparam>
	public interface INumber<TSelf> :
		IParseable<TSelf>,
		IEqualityOperators<TSelf, TSelf>,     // implies IEquatable<TSelf>
		IAdditiveIdentity<TSelf, TSelf>,
		IMultiplicativeIdentity<TSelf, TSelf>,
		IAdditionOperators<TSelf, TSelf, TSelf>,
		ISubtractionOperators<TSelf, TSelf, TSelf>,
		IMultiplyOperators<TSelf, TSelf, TSelf>,
		IDivisionOperators<TSelf, TSelf, TSelf>,
		IUnaryPlusOperators<TSelf, TSelf>,
		IUnaryNegationOperators<TSelf, TSelf>,
		IDecrementOperators<TSelf>,
		IIncrementOperators<TSelf>,
		where TSelf : INumber<TSelf>
	{
		/// <summary>
		/// Alias for <see cref="IAdditiveIdentity{TSelf, TResult}.AdditiveIdentity"/>
		/// </summary>
		abstract static TSelf Zero { get; }

		/// <summary>
		/// Alias for <see cref="IMultiplicativeIdentity{TSelf, TResult}.MultiplicativeIdentity"/>
		/// </summary>
		abstract static TSelf One { get; }
	}

	/// <summary>
	/// The base interface for signed numbers
	/// </summary>
	/// <typeparam name="TSelf">The actual type of implemented number struct/class</typeparam>
	public interface ISignedNumber<TSelf> : INumber<TSelf> where TSelf : ISignedNumber<TSelf>
	{
		/// <summary>
		/// Abstract static get negative one for <typeparamref name="TSelf"/>
		/// </summary>
		abstract static TSelf NegativeOne { get; }
	}

	/// <summary>
	/// The base interface for unsigned numbers
	/// </summary>
	/// <typeparam name="TSelf">The actual type of implemented number struct/class</typeparam>
	public interface IUnsignedNumber<TSelf> : INumber<TSelf> where TSelf : IUnsignedNumber<TSelf>
	{
		// It's not possible to check for lack of an interface in a constraint, so IUnsignedNumberBase<TSelf> is likely required
	}

	/// <summary>
	/// The base interface for floating point numbers
	/// </summary>
	/// <typeparam name="TSelf">The actual type of implemented number struct/class</typeparam>
	public interface IFloatNumber<TSelf> : ISignedNumber<TSelf> where TSelf : IFloatNumber<TSelf>
	{
		/// <summary>
		/// Abstract static get infinity for <typeparamref name="TSelf"/>
		/// </summary>
		abstract static TSelf Infinity { get; }

		/// <summary>
		/// Abstract static get NaN for <typeparamref name="TSelf"/>
		/// </summary>
		abstract static TSelf Nan { get; }

		/// <summary>
		/// Statically check whether the given <paramref name="value"/> is finite or not
		/// </summary>
		/// <param name="value">The given number to check</param>
		/// <returns>Whether <paramref name="value"/> is finite or not</returns>
		abstract static bool IsFinite(TSelf value);

		/// <summary>
		/// Statically check whether the given <paramref name="value"/> is infinity or not
		/// </summary>
		/// <param name="value">The given number to check</param>
		/// <returns>Whether <paramref name="value"/> is infinity or not</returns>
		abstract static bool IsInfinity(TSelf value);

		/// <summary>
		/// Statically check whether the given <paramref name="value"/> is NaN or not
		/// </summary>
		/// <param name="value">The given number to check</param>
		/// <returns>Whether <paramref name="value"/> is NaN or not</returns>
		abstract static bool IsNan(TSelf value);

		/// <summary>
		/// Statically return the exponential of the given <paramref name="number"/>
		/// </summary>
		/// <param name="number">The number of type <typeparamref name="TSelf"/></param>
		/// <returns>The exponential of the given <paramref name="number"/></returns>
		abstract static TSelf Exp(TSelf number);

		/// <summary>
		/// Statically return the logarithm of the given <paramref name="number"/>
		/// </summary>
		/// <param name="number">The number of type <typeparamref name="TSelf"/></param>
		/// <returns>The logarithm of the given <paramref name="number"/></returns>
		abstract static TSelf Log(TSelf number);

		/// <summary>
		/// Statically return the power of the given <paramref name="number"/> to <paramref name="power"/>
		/// </summary>
		/// <param name="number">The number of type <typeparamref name="TSelf"/></param>
		/// <param name="power">The power of type <typeparamref name="TSelf"/></param>
		/// <returns>The power of <paramref name="number"/> to <paramref name="power"/></returns>
		abstract static TSelf Pow(TSelf number, TSelf power);

		/// <summary>
		/// Statically return the reciprocal of the given <paramref name="number"/>
		/// </summary>
		/// <param name="number">The number of type <typeparamref name="TSelf"/></param>
		/// <returns>The reciprocal of the given <paramref name="number"/></returns>
		abstract static TSelf Reciprocal(TSelf number);

		/// <summary>
		/// Statically return the sqrt of the given <paramref name="number"/>
		/// </summary>
		/// <param name="number">The number of type <typeparamref name="TSelf"/></param>
		/// <returns>The sqrt of the given <paramref name="number"/></returns>
		abstract static TSelf Sqrt(TSelf number);

		/// <summary>
		/// Statically return the absolute value of the given <paramref name="number"/>
		/// </summary>
		/// <param name="number">The number of type <typeparamref name="TSelf"/></param>
		/// <returns>The absolute value of the given <paramref name="number"/></returns>
		abstract static TSelf Abs(TSelf number);
	}

	/// <summary>
	/// The base interface for complex numbers
	/// </summary>
	/// <typeparam name="TSelf">The actual type of implemented complex number struct/class</typeparam>
	/// <typeparam name="T">The type of corresponding real number</typeparam>
	public interface IComplexNumber<TSelf, T> : IFloatNumber<TSelf> where TSelf : IComplexNumber<TSelf, T> ////where T : IFloatNumber<T>
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
		TSelf Conj { get; }

		/// <summary>
		/// Statically return the complex conjugate of the given <paramref name="complex"/> number
		/// </summary>
		/// <param name="complex">The complex number of type <typeparamref name="TSelf"/></param>
		/// <returns>The complex conjugate of the given <paramref name="complex"/> number</returns>
		static TSelf Conjugate(TSelf complex) => complex.Magnitude;

		/// <summary>
		/// Statically return the complex power of the given <paramref name="complex"/> number and a real power <paramref name="p"/>
		/// </summary>
		/// <param name="complex">The complex number of type <typeparamref name="TSelf"/></param>
		/// <param name="p">The power as a real number of type <typeparamref name="T"/></param>
		/// <returns>The complex power of <paramref name="complex"/> to <paramref name="p"/></returns>
		abstract static TSelf Pow(TSelf complex, T p);

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
	#endregion
}
