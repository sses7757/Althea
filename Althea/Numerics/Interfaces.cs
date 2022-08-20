using System.Globalization;
using System.Numerics;


namespace Althea.Numerics;

#pragma warning disable CS1591
/// <summary>
/// The base interface for all numbers used by <see cref="Althea"/> including complex numbers.
/// </summary>
/// <remarks>
/// The official <see cref="INumberBase{TSelf}"/> is not used since it has too little interface members while <see cref="INumber{TSelf}"/> is not suitable for complex numbers.
/// </remarks>
/// <typeparam name="TSelf">The actual type that implements this <see cref="IBaseNumber{TSelf}"/></typeparam>
public interface IBaseNumber<TSelf> :
	IComparisonOperators<TSelf, TSelf, bool>, IEquatable<TSelf>, IComparable<TSelf>,
	IAdditionOperators<TSelf, TSelf, TSelf>, ISubtractionOperators<TSelf, TSelf, TSelf>,
	IMultiplyOperators<TSelf, TSelf, TSelf>, IDivisionOperators<TSelf, TSelf, TSelf>,
	IAdditiveIdentity<TSelf, TSelf>, IMultiplicativeIdentity<TSelf, TSelf>,
	IUnaryNegationOperators<TSelf, TSelf>, IUnaryPlusOperators<TSelf, TSelf>,
	IIncrementOperators<TSelf>, IDecrementOperators<TSelf>,
	ISpanParsable<TSelf>, ISpanFormattable
	where TSelf : unmanaged, IBaseNumber<TSelf>
{
	#region meta
	/// <summary>
	/// Statically get the <see cref="DataType"/> of <typeparamref name="TSelf"/>.
	/// </summary>
	abstract static DataType Type { get; }

	/// <summary>
	/// Statically get the machine precision of <typeparamref name="TSelf"/>.
	/// </summary>
	abstract static TSelf MachinePrecision { get; }

	/// <summary>
	/// Statically get the size of type <typeparamref name="TSelf"/> (in bytes).
	/// </summary>
	abstract static int Size { get; }

	/// <summary>
	/// Statically get whether type <typeparamref name="TSelf"/> is a complex type or not.
	/// </summary>
	abstract static bool IsComplexType { get; }
	#endregion

	#region constants
	abstract static TSelf One { get; }

	abstract static TSelf Zero { get; }
	#endregion

	#region predicates
	abstract static bool IsReal(TSelf value);

	abstract static bool IsComplex(TSelf value);

	abstract static bool IsImaginaryNumber(TSelf value);

	abstract static bool IsFinite(TSelf value);

	abstract static bool IsNaN(TSelf value);

	abstract static bool IsNegative(TSelf value);

	abstract static bool IsPositive(TSelf value);

	abstract static bool IsInteger(TSelf value);

	abstract static bool IsOddInteger(TSelf value);

	abstract static bool IsZero(TSelf value);
	#endregion

	#region computation
	abstract static TSelf Conjugate(TSelf value);

	abstract static TSelf Abs(TSelf value);

	abstract static TSelf MaxMagnitudeNumber(TSelf x, TSelf y);

	abstract static TSelf MinMagnitudeNumber(TSelf x, TSelf y);

	abstract static TSelf CopySign(TSelf value, TSelf sign);

	abstract static TSelf Max(TSelf x, TSelf y);

	abstract static TSelf Min(TSelf x, TSelf y);

	abstract static TSelf Sign(TSelf value);
	#endregion

	#region conversion
	abstract static bool TryConvertFromChecked<TOther>(TOther value, out TSelf result) where TOther : unmanaged, IBaseNumber<TOther>;

	abstract static bool TryConvertFrom<TOther>(TOther value, out TSelf result) where TOther : unmanaged, IBaseNumber<TOther>;

	abstract static bool TryConvertToChecked<TOther>(TSelf value, out TOther result) where TOther : unmanaged, IBaseNumber<TOther>;

	abstract static bool TryConvertTo<TOther>(TSelf value, out TOther result) where TOther : unmanaged, IBaseNumber<TOther>;
	#endregion

	#region parse
	abstract static TSelf Parse(string s, NumberStyles style, IFormatProvider? provider);

	abstract static TSelf Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider);

	abstract static bool TryParse(string s, NumberStyles style, IFormatProvider? provider, out TSelf result);

	abstract static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out TSelf result);
	#endregion
}

#region math functions
public interface IExponentialFunctions<TSelf> where TSelf : unmanaged, IExponentialFunctions<TSelf>, IBaseNumber<TSelf>
{
	abstract static TSelf Exp(TSelf x);

	static TSelf ExpM1(TSelf x)
	{
		return TSelf.Exp(x) - TSelf.One;
	}

	abstract static TSelf Exp2(TSelf x);

	static TSelf Exp2M1(TSelf x)
	{
		return TSelf.Exp2(x) - TSelf.One;
	}

	abstract static TSelf Exp10(TSelf x);

	static TSelf Exp10M1(TSelf x)
	{
		return TSelf.Exp10(x) - TSelf.One;
	}
}

public interface IHyperbolicFunctions<TSelf> where TSelf : unmanaged, IHyperbolicFunctions<TSelf>, IBaseNumber<TSelf>
{
	abstract static TSelf Acosh(TSelf x);

	abstract static TSelf Asinh(TSelf x);

	abstract static TSelf Atanh(TSelf x);

	abstract static TSelf Cosh(TSelf x);

	abstract static TSelf Sinh(TSelf x);

	abstract static TSelf Tanh(TSelf x);
}

public interface ILogarithmicFunctions<TSelf> where TSelf : unmanaged, ILogarithmicFunctions<TSelf>, IBaseNumber<TSelf>
{
	abstract static TSelf Log(TSelf x);

	abstract static TSelf Log(TSelf x, TSelf newBase);

	static TSelf LogP1(TSelf x)
	{
		return TSelf.Log(x + TSelf.One);
	}

	abstract static TSelf Log2(TSelf x);

	static TSelf Log2P1(TSelf x)
	{
		return TSelf.Log2(x + TSelf.One);
	}

	abstract static TSelf Log10(TSelf x);

	static TSelf Log10P1(TSelf x)
	{
		return TSelf.Log10(x + TSelf.One);
	}
}

public interface IPowerFunctions<TSelf> where TSelf : unmanaged, IPowerFunctions<TSelf>, IBaseNumber<TSelf>
{
	abstract static TSelf Pow(TSelf x, TSelf y);
}

public interface IRootFunctions<TSelf> where TSelf : unmanaged, IRootFunctions<TSelf>, IBaseNumber<TSelf>
{
	abstract static TSelf Cbrt(TSelf x);

	abstract static TSelf Hypot(TSelf x, TSelf y);

	abstract static TSelf RootN(TSelf x, int n);

	abstract static TSelf Sqrt(TSelf x);
}

public interface ITrigonometricFunctions<TSelf> where TSelf : unmanaged, ITrigonometricFunctions<TSelf>, IBinaryFloat<TSelf>
{
	abstract static TSelf Acos(TSelf x);

	static TSelf AcosPi(TSelf x) => TSelf.Acos(x) / TSelf.Pi;

	abstract static TSelf Asin(TSelf x);

	static TSelf AsinPi(TSelf x) => TSelf.Asin(x) / TSelf.Pi;

	abstract static TSelf Atan(TSelf x);

	abstract static TSelf Atan2(TSelf y, TSelf x);

	static TSelf Atan2Pi(TSelf y, TSelf x) => TSelf.Atan2(y, x) / TSelf.Pi;

	static TSelf AtanPi(TSelf x) => TSelf.Atan(x) / TSelf.Pi;

	abstract static TSelf Cos(TSelf x);

	static TSelf CosPi(TSelf x) => TSelf.Cos(x * TSelf.Pi);

	abstract static TSelf Sin(TSelf x);

	abstract static (TSelf Sin, TSelf Cos) SinCos(TSelf x);

	static TSelf SinPi(TSelf x) => TSelf.Sin(x * TSelf.Pi);

	abstract static TSelf Tan(TSelf x);

	static TSelf TanPi(TSelf x) => TSelf.Tan(x * TSelf.Pi);
}
#endregion

/// <summary>
/// The base interface for all binary floating point numbers used by <see cref="Althea"/> including binary floating point complex numbers.
/// </summary>
/// <remarks>
/// The official <see cref="IBinaryFloatingPointIeee754{TSelf}"/> is not used since it is not suitable for complex numbers.
/// </remarks>
/// <typeparam name="TSelf">The actual type that implements this <see cref="IBinaryFloat{TSelf}"/></typeparam>
public interface IBinaryFloat<TSelf> : IBaseNumber<TSelf>,
	IHyperbolicFunctions<TSelf>, ILogarithmicFunctions<TSelf>,
	IPowerFunctions<TSelf>, IRootFunctions<TSelf>,
	ITrigonometricFunctions<TSelf>, IExponentialFunctions<TSelf>
	where TSelf : unmanaged, IBinaryFloat<TSelf>
{
	#region constants
	abstract static TSelf NegativeOne { get; }

	abstract static TSelf NegativeZero { get; }

	abstract static TSelf NaN { get; }

	abstract static TSelf NegativeInfinity { get; }

	abstract static TSelf PositiveInfinity { get; }
	#endregion

	#region rounding
	abstract static TSelf Ceiling(TSelf x);

	abstract static TSelf Floor(TSelf x);

	abstract static TSelf Round(TSelf x);

	abstract static TSelf Round(TSelf x, int digits);

	abstract static TSelf Round(TSelf x, MidpointRounding mode);

	abstract static TSelf Round(TSelf x, int digits, MidpointRounding mode);

	abstract static TSelf Truncate(TSelf x);
	#endregion

	#region math
	abstract static TSelf E { get; }

	abstract static TSelf Epsilon { get; }

	abstract static TSelf Pi { get; }

	abstract static TSelf Tau { get; }

	abstract static TSelf FusedMultiplyAdd(TSelf left, TSelf right, TSelf addend);

	abstract static TSelf ReciprocalEstimate(TSelf x);

	abstract static TSelf ReciprocalSqrtEstimate(TSelf x);
	#endregion
}

/// <summary>
/// The base interface for all binary integer numbers used by <see cref="Althea"/> including integral complex numbers.
/// </summary>
/// <remarks>
/// The official <see cref="IBinaryInteger{TSelf}"/> is not used since it is not suitable for complex numbers.
/// </remarks>
/// <typeparam name="TSelf">The actual type that implements this <see cref="IBinaryInt{TSelf}"/></typeparam>
public interface IBinaryInt<TSelf> : IBaseNumber<TSelf>,
	IBitwiseOperators<TSelf, TSelf, TSelf>, IModulusOperators<TSelf, TSelf, TSelf>, IShiftOperators<TSelf, int, TSelf>
	where TSelf : unmanaged, IBinaryInt<TSelf>
{
	#region math
	abstract static bool IsPow2(TSelf value);

	abstract static TSelf Log2(TSelf value);

	abstract static (TSelf Quotient, TSelf Remainder) DivRem(TSelf left, TSelf right);

	abstract static TSelf RotateLeft(TSelf value, int rotateAmount);

	abstract static TSelf RotateRight(TSelf value, int rotateAmount);

	abstract static TSelf PopCount(TSelf value);

	abstract static TSelf LeadingZeroCount(TSelf value);

	abstract static TSelf TrailingZeroCount(TSelf value);
	#endregion
}