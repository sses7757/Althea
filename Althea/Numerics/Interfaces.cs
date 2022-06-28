using System.Globalization;
using System.Numerics;


namespace Althea.Numerics;

#pragma warning disable CS1591
/// <summary>
/// The base interface for all numbers used by <see cref="Althea"/> including complex numbers.
/// </summary>
/// <remarks>
/// The official <see cref="INumberBase{TSelf}"/> is not used since it has too little interface members while <see cref="System.Numerics.INumber{TSelf}"/> is not suitable for complex numbers.
/// </remarks>
/// <typeparam name="TSelf">The actual type that implements this <see cref="INumber{TSelf}"/></typeparam>
public interface INumber<TSelf> :
	IComparisonOperators<TSelf, TSelf>, IEqualityOperators<TSelf, TSelf>,
	IAdditionOperators<TSelf, TSelf, TSelf>, ISubtractionOperators<TSelf, TSelf, TSelf>,
	IMultiplyOperators<TSelf, TSelf, TSelf>, IDivisionOperators<TSelf, TSelf, TSelf>,
	IAdditiveIdentity<TSelf, TSelf>, IMultiplicativeIdentity<TSelf, TSelf>,
	IUnaryNegationOperators<TSelf, TSelf>, IUnaryPlusOperators<TSelf, TSelf>,
	IIncrementOperators<TSelf>, IDecrementOperators<TSelf>,
	ISpanParsable<TSelf>, ISpanFormattable
	where TSelf : unmanaged, INumber<TSelf>
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
	abstract static TSelf Abs(TSelf value);

	abstract static TSelf MaxMagnitude(TSelf x, TSelf y);

	abstract static TSelf MaxMagnitudeNumber(TSelf x, TSelf y);

	abstract static TSelf MinMagnitude(TSelf x, TSelf y);

	abstract static TSelf MinMagnitudeNumber(TSelf x, TSelf y);

	abstract static TSelf CopySign(TSelf value, TSelf sign);

	abstract static TSelf Max(TSelf x, TSelf y);

	abstract static TSelf Min(TSelf x, TSelf y);

	abstract static TSelf Sign(TSelf value);
	#endregion

	#region conversion
	abstract static bool TryConvertFromChecked<TOther>(TOther value, out TSelf result) where TOther : unmanaged, INumber<TOther>;

	abstract static bool TryConvertFrom<TOther>(TOther value, out TSelf result) where TOther : unmanaged, INumber<TOther>;

	abstract static bool TryConvertToChecked<TOther>(TSelf value, out TOther result) where TOther : unmanaged, INumber<TOther>;

	abstract static bool TryConvertTo<TOther>(TSelf value, out TOther result) where TOther : unmanaged, INumber<TOther>;
	#endregion

	#region parse
	abstract static TSelf Parse(string s, NumberStyles style, IFormatProvider? provider);

	abstract static TSelf Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider);

	abstract static bool TryParse(string s, NumberStyles style, IFormatProvider? provider, out TSelf result);

	abstract static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out TSelf result);
	#endregion
}

/// <summary>
/// The base interface for all binary floating point numbers used by <see cref="Althea"/> including binary floating point complex numbers.
/// </summary>
/// <remarks>
/// The official <see cref="IBinaryFloatingPointIeee754{TSelf}"/> is not used since it is not suitable for complex numbers.
/// </remarks>
/// <typeparam name="TSelf">The actual type that implements this <see cref="IBinaryFloat{TSelf}"/></typeparam>
public interface IBinaryFloat<TSelf> : INumber<TSelf>,
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
/// <typeparam name="TSelf">The actual type that implements this <see cref="IBinaryInteger{TSelf}"/></typeparam>
public interface IBinaryInteger<TSelf> : INumber<TSelf>,
	IBitwiseOperators<TSelf, TSelf, TSelf>, IModulusOperators<TSelf, TSelf, TSelf>, IShiftOperators<TSelf, TSelf>
	where TSelf : unmanaged, IBinaryInteger<TSelf>
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