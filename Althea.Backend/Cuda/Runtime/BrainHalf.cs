using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;


namespace Althea.Backend.Cuda
{
	#region constants
	/// <summary>
	/// The static class for constants about <see cref="BrainHalf"/>
	/// </summary>
	public static class BrainFloatConst
	{
		/// <summary>
		/// The Brain Floating Point floating-point format.
		/// </summary>
		public const DataTypeClassification BrainFloat = (DataTypeClassification)(1 << 4);

		/// <summary>
		/// The <see cref="DataType"/> of <see cref="BrainHalf"/>
		/// </summary>
		public const DataType RealBrainFloat16 = (DataType)((int)DataTypeTuple.Real + ((int)BrainFloat << 8) + 2 << 16);

		/// <summary>
		/// The <see cref="DataType"/> of <see cref="Complex{T}"/> of <see cref="BrainHalf"/>
		/// </summary>
		public const DataType ComplexBrainFloat16 = (DataType)((int)DataTypeTuple.Complex + ((int)BrainFloat << 8) + 2 << 16);

		/// <summary>
		/// The machine precision of <see cref="BrainHalf"/>
		/// </summary>
		public const double BrainHalfPrecision = 0.0078125;
	}
	#endregion


	/// <summary>
	/// The Brain Floating Point floating-point format with total number of bits = 16.
	/// </summary>
	/// <remarks>Do not use the methods during heavy loads since they are all software implemented.</remarks>
	/// <seealso ref="https://www.nextplatform.com/2018/05/10/tearing-apart-googles-tpu-3-0-ai-coprocessor/"/>
	public readonly partial struct BrainHalf : IBinaryFloat<BrainHalf>
	{
		#region basic
		private const byte START_EXP = 7;
		private const byte START_SIGN = 15;

		private const ushort FRAC_MASK = 0x007F;
		private const ushort EXP_MASK = 0x7F80;
		private const ushort SIGN_MASK = 0x8000;

		private const byte BIAS = 127; // (2^8 - 1) / 2

		private const byte MAX_EXPONENT = byte.MaxValue - 1; // (2^8 - 1) - 1 //- BIAS
		private const byte ABNORMAL_EXP = byte.MaxValue;
		private const byte ONE_EXP = BIAS;

		private readonly ushort _data;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private BrainHalf(ushort data) => this._data = data;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private BrainHalf(bool neg, byte exp, byte frac)
		{
			this._data = (ushort)(frac + (exp << START_EXP) + ((neg ? 1 : 0) << START_SIGN));
		}

		private readonly bool IsNeg {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => (this._data & SIGN_MASK) == SIGN_MASK;
		}
		private readonly byte ExpPart {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => (byte)((this._data & EXP_MASK) >> START_EXP);
		}
		private readonly byte FracPart {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => (byte)(this._data & FRAC_MASK);
		}

		static DataType IBaseNumber<BrainHalf>.Type => BrainFloatConst.RealBrainFloat16;

		static int IBaseNumber<BrainHalf>.Size => sizeof(ushort);

		static bool IBaseNumber<BrainHalf>.IsComplexType => false;

		static BrainHalf IBaseNumber<BrainHalf>.MachinePrecision => (BrainHalf)BrainFloatConst.BrainHalfPrecision;
		#endregion

		#region constants
		/// <inheritdoc/>
		public static BrainHalf One => new(false, ONE_EXP, 0);
		/// <inheritdoc/>
		public static BrainHalf Zero => default;
		static BrainHalf IAdditiveIdentity<BrainHalf, BrainHalf>.AdditiveIdentity => default;
		static BrainHalf IMultiplicativeIdentity<BrainHalf, BrainHalf>.MultiplicativeIdentity => One;
		/// <inheritdoc/>
		public static BrainHalf NegativeOne => new(true, ONE_EXP, 0);
		/// <inheritdoc/>
		public static BrainHalf NegativeZero => new(true, 0, 0);
		/// <inheritdoc/>
		public static BrainHalf PositiveInfinity => new(false, ABNORMAL_EXP, 0);
		/// <inheritdoc/>
		public static BrainHalf NegativeInfinity => new(true, ABNORMAL_EXP, 0);
		/// <inheritdoc/>
		public static BrainHalf NaN => new(false, ABNORMAL_EXP, 1);
		/// <inheritdoc/>
		public static BrainHalf MaxValue => new(false, MAX_EXPONENT, 127);
		/// <inheritdoc/>
		public static BrainHalf MinValue => new(true, MAX_EXPONENT, 127);

		/// <inheritdoc/>
		public static BrainHalf E => (BrainHalf)float.E;

		/// <inheritdoc/>
		public static BrainHalf Epsilon => new(true, 0, 1);

		/// <inheritdoc/>
		public static BrainHalf Pi => (BrainHalf)float.Pi;

		/// <inheritdoc/>
		public static BrainHalf Tau => (BrainHalf)float.Tau;
		#endregion

		#region predicates
		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsZero(BrainHalf value) => (value._data | SIGN_MASK) == SIGN_MASK;

		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsNaN(BrainHalf value) => ((value._data & EXP_MASK) == EXP_MASK) && value.FracPart != 0;

		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsInfinite(BrainHalf value) => ((value._data & EXP_MASK) == EXP_MASK) && value.FracPart == 0;

		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsSubnormal(BrainHalf value) => value.ExpPart == 0 && value.FracPart != 0;

		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsFinite(BrainHalf value) => (value._data & EXP_MASK) != EXP_MASK;

		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsNegative(BrainHalf value) => value.IsNeg;

		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsPositive(BrainHalf value) => !value.IsNeg && !IsZero(value);

		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsInteger(BrainHalf value) => value.ExpPart >= 7 + BIAS || value.ExpPart >= BIAS + 7 - byte.TrailingZeroCount(value.FracPart);

		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsOddInteger(BrainHalf value) => value.ExpPart <= BIAS + 7 - byte.TrailingZeroCount(value.FracPart);

		static bool IBaseNumber<BrainHalf>.IsReal(BrainHalf value) => true;

		static bool IBaseNumber<BrainHalf>.IsComplex(BrainHalf value) => false;

		static bool IBaseNumber<BrainHalf>.IsImaginaryNumber(BrainHalf value) => false;
		#endregion

		#region convert
		const byte SINGLE_SIGN_START = 31, SINGLE_EXP_START = 23, SINGLE_EXP_MAX = byte.MaxValue;
		const uint SINGLE_SIGN_MASK = 0x80000000u;
		const uint SINGLE_EXP_MASK  = 0x7F800000u;
		const uint SINGLE_FRAC_MASK = 0x007FFFFFu;

		/// <summary>
		/// Convert a <see cref="float"/> to a <see cref="BrainHalf"/>
		/// </summary>
		/// <param name="value">The <see cref="float"/> to be converted</param>
		/// <returns>The converted <see cref="BrainHalf"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static explicit operator BrainHalf(float value)
		{
			const byte FRAC_SHIFT = SINGLE_EXP_START - START_EXP;
			// get 3 parts
			uint floatInt = (uint)BitConverter.SingleToInt32Bits(value);
			bool negVal = (floatInt & SINGLE_SIGN_MASK) >> SINGLE_SIGN_START != 0;
			int expVal = (int)((floatInt & SINGLE_EXP_MASK) >> SINGLE_EXP_START);
			uint fracVal = floatInt & SINGLE_FRAC_MASK;
			// check abnormal
			if (expVal == SINGLE_EXP_MAX)
			{
				if (fracVal != 0)
				{
					return new(negVal, ABNORMAL_EXP, (byte)(fracVal >> FRAC_SHIFT));
				}
				return negVal ? NegativeInfinity : PositiveInfinity;
			}
			// normal case
			fracVal >>= FRAC_SHIFT - 1;
			return Round(negVal, expVal - BIAS, fracVal);
		}

		const byte DOUBLE_SIGN_START = 63, DOUBLE_EXP_START = 52;
		const ushort DOUBLE_EXP_MAX = 2047;
		const ulong DOUBLE_SIGN_MASK = 0x8000_0000_0000_0000ul;
		const ulong DOUBLE_EXP_MASK  = 0x7FF0_0000_0000_0000ul;
		const ulong DOUBLE_FRAC_MASK = 0x000F_FFFF_FFFF_FFFFul;

		/// <summary>
		/// Convert a <see cref="double"/> to a <see cref="BrainHalf"/>
		/// </summary>
		/// <param name="value">The <see cref="double"/> to be converted</param>
		/// <returns>The converted <see cref="BrainHalf"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static explicit operator BrainHalf(double value)
		{
			const byte FRAC_SHIFT = DOUBLE_EXP_START - START_EXP;
			// get 3 parts
			ulong doubleInt = (ulong)BitConverter.DoubleToInt64Bits(value);
			bool negVal = (doubleInt & DOUBLE_SIGN_MASK) >> DOUBLE_SIGN_START != 0;
			int expVal = (int)((doubleInt & DOUBLE_EXP_MASK) >> DOUBLE_EXP_START);
			ulong fracVal = doubleInt & DOUBLE_FRAC_MASK;
			// check abnormal
			if (expVal == DOUBLE_EXP_MAX)
			{
				if (fracVal != 0)
				{
					return new(negVal, ABNORMAL_EXP, (byte)(fracVal >> FRAC_SHIFT));
				}
				return negVal ? NegativeInfinity : PositiveInfinity;
			}
			// normal case
			fracVal >>= FRAC_SHIFT - 1;
			return Round(negVal, expVal - DOUBLE_EXP_MAX / 2, (uint)fracVal);
		}

		const byte HALF_SIGN_START = 15, HALF_EXP_START = 10;
		const byte HALF_EXP_MAX = 31;
		const ushort HALF_SIGN_MASK = 0x8000;
		const ushort HALF_EXP_MASK  = 0x7C00;
		const ushort HALF_FRAC_MASK = 0x3FF;

		/// <summary>
		/// Convert a <see cref="Half"/> to a <see cref="BrainHalf"/>
		/// </summary>
		/// <param name="value">The <see cref="Half"/> to be converted</param>
		/// <returns>The converted <see cref="BrainHalf"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static explicit operator BrainHalf(Half value)
		{
			const byte FRAC_SHIFT = HALF_EXP_START - START_EXP;
			// get 3 parts
			ushort halfInt = Unsafe.As<Half, ushort>(ref value);
			bool negVal = (halfInt & HALF_SIGN_MASK) >> HALF_SIGN_START != 0;
			int expVal = ((halfInt & HALF_EXP_MASK) >> HALF_EXP_START);
			int fracVal = halfInt & HALF_FRAC_MASK;
			// check abnormal
			if (expVal == HALF_EXP_MAX)
			{
				if (fracVal != 0)
				{
					return new(negVal, ABNORMAL_EXP, (byte)(fracVal >> FRAC_SHIFT));
				}
				return negVal ? NegativeInfinity : PositiveInfinity;
			}
			// normal case
			fracVal >>= FRAC_SHIFT - 1;
			return Round(negVal, expVal - HALF_EXP_MAX / 2, (uint)fracVal);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static BrainHalf Round(bool neg, int exp, uint frac)
		{
			exp += BIAS;
			byte resFrac;
			if ((frac & 1) == 0u)
			{	// last bit not set, down rounding
				resFrac = (byte)(frac >> 1);
			}
			else
			{	// up rounding
				resFrac = (byte)(1 + (frac >> 1));
				if (resFrac == 0)
				{   // 1.11...1 * 2^5 => 2.00..0 * 2^5 == 1.00..0 * 2^6
					exp += 1;
				}
			}
			if (exp > MAX_EXPONENT)
			{   // overflow
				return neg ? NegativeInfinity : PositiveInfinity;
			}
			if (exp < 0)
			{   // underflow, return 0
				return new(neg, 0, 0);
			}
			return new(neg, (byte)exp, resFrac);
		}

		/// <summary>
		/// Convert a <see cref="BrainHalf"/> to a <see cref="float"/>
		/// </summary>
		/// <param name="value">The <see cref="BrainHalf"/> to be converted</param>
		/// <returns>The converted <see cref="float"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator float(BrainHalf value)
		{
			const byte FRAC_SHIFT = SINGLE_EXP_START - START_EXP;
			// get 3 parts
			bool negV = value.IsNeg;
			byte expV = value.ExpPart;
			byte fracV = value.FracPart;
			// check abnormal is not necessary
			int resInt = ((negV ? 1 : 0) << SINGLE_SIGN_START) + (expV << SINGLE_EXP_START) + (fracV << FRAC_SHIFT);
			return BitConverter.Int32BitsToSingle(resInt);
		}

		/// <summary>
		/// Convert a <see cref="BrainHalf"/> to a <see cref="double"/>
		/// </summary>
		/// <param name="value">The <see cref="BrainHalf"/> to be converted</param>
		/// <returns>The converted <see cref="double"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator double(BrainHalf value)
		{
			const byte FRAC_SHIFT = DOUBLE_EXP_START - START_EXP;
			// get 3 parts
			bool negV = value.IsNeg;
			byte expV = value.ExpPart;
			byte fracV = value.FracPart;
			// check abnormal
			if (expV == ABNORMAL_EXP)
			{
				if (fracV != 0)
				{
					return double.NaN;
				}
				return negV ? double.NegativeInfinity : double.PositiveInfinity;
			}
			// normal case
			ulong expRes = (ulong)(expV - BIAS) + DOUBLE_EXP_MAX / 2;
			ulong resInt = ((negV ? 1ul : 0ul) << DOUBLE_SIGN_START) + (expRes << DOUBLE_EXP_START) + (ulong)(fracV << FRAC_SHIFT);
			return BitConverter.Int64BitsToDouble((long)resInt);
		}

		/// <summary>
		/// Convert a <see cref="BrainHalf"/> to a <see cref="Half"/>
		/// </summary>
		/// <param name="value">The <see cref="BrainHalf"/> to be converted</param>
		/// <returns>The converted <see cref="Half"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static explicit operator Half(BrainHalf value)
		{
			const byte FRAC_SHIFT = DOUBLE_EXP_START - START_EXP;
			// get 3 parts
			bool negV = value.IsNeg;
			byte expV = value.ExpPart;
			byte fracV = value.FracPart;
			// check abnormal
			if (expV == ABNORMAL_EXP)
			{
				if (fracV != 0)
				{
					return Half.NaN;
				}
				return negV ? Half.NegativeInfinity : Half.PositiveInfinity;
			}
			// normal case
			sbyte expRes = (sbyte)(expV - BIAS + HALF_EXP_MAX / 2);
			ushort result = (ushort)(((negV ? 1 : 0) << 15) + (expRes << 10) + fracV << FRAC_SHIFT);
			return Unsafe.As<ushort, Half>(ref result);
		}
		#endregion

		#region arithmetic
		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static BrainHalf Abs(BrainHalf value) => new((ushort)((value._data | SIGN_MASK) ^ SIGN_MASK));
		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static BrainHalf Sqrt(BrainHalf value) => (BrainHalf)MathF.Sqrt(value);
		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static BrainHalf Cbrt(BrainHalf value) => (BrainHalf)MathF.Cbrt(value);
		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static BrainHalf Root(BrainHalf value, int n) => (BrainHalf)float.Root(value, n);
		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static BrainHalf Pow(BrainHalf basis, BrainHalf power) => (BrainHalf)Math.Pow(basis, power);

		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static BrainHalf operator +(BrainHalf value) => value;
		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static BrainHalf operator -(BrainHalf value) => new((ushort)(value._data ^ SIGN_MASK));
		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static BrainHalf operator ++(BrainHalf value) => (BrainHalf)(value + 1.0f);
		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static BrainHalf operator --(BrainHalf value) => (BrainHalf)(value - 1.0f);
		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static BrainHalf operator +(BrainHalf left, BrainHalf right) => (BrainHalf)((float)left + right);
		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static BrainHalf operator -(BrainHalf left, BrainHalf right) => (BrainHalf)((float)left - right);
		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static BrainHalf operator *(BrainHalf left, BrainHalf right) => (BrainHalf)((float)left * right);
		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static BrainHalf operator /(BrainHalf left, BrainHalf right) => (BrainHalf)((float)left / right);
		#endregion

		#region compare
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool AreZero(BrainHalf left, BrainHalf right) => (ushort)((left._data | right._data) & ~SIGN_MASK) == 0;

		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(BrainHalf left, BrainHalf right)
		{
			if (IsNaN(left) || IsNaN(right))
				return false;
			if (left._data == right._data)
				return true;
			if (AreZero(left, right))
				return true;
			return false;
		}
		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(BrainHalf left, BrainHalf right) => !(left == right);
		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator <(BrainHalf left, BrainHalf right)
		{
			if (IsNaN(left) || IsNaN(right))
				return false;
			bool leftNeg = left.IsNeg;
			if (leftNeg != right.IsNeg)
			{
				return leftNeg && !AreZero(left, right);
			}
			return (left._data < right._data) ^ leftNeg;
		}
		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator >(BrainHalf left, BrainHalf right) => right < left;
		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator <=(BrainHalf left, BrainHalf right)
		{
			if (IsNaN(left) || IsNaN(right))
				return false;
			bool leftNeg = left.IsNeg;
			if (leftNeg != right.IsNeg)
			{
				return leftNeg || AreZero(left, right);
			}
			return (left._data <= right._data) ^ leftNeg;
		}
		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator >=(BrainHalf left, BrainHalf right) => right <= left;

		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode()
		{
			if (((this._data - 1) & ~SIGN_MASK) >= EXP_MASK)
				return this._data & EXP_MASK;
			else
				return this._data;
		}
		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override bool Equals(object? obj) => obj is BrainHalf f && this.Equals(f);
		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(BrainHalf other) => this == other;
		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int CompareTo(BrainHalf other)
		{
			if (this < other)
			{
				return -1;
			}
			if (this > other)
			{
				return 1;
			}
			if (this == other)
			{
				return 0;
			}
			if (IsNaN(this))
			{
				return IsNaN(other) ? 0 : -1;
			}
			return 1;
		}
		int IComparable.CompareTo(object? obj) => obj is BrainHalf bh ? this.CompareTo(bh) : throw new NotSupportedException();
		#endregion

		#region string
		/// <inheritdoc/>
		public override string ToString() => this.ToString(null, null);
		/// <inheritdoc/>
		public string ToString(string? format, IFormatProvider? formatProvider) => ((float)this).ToString(format, formatProvider);
		bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => ((float)this).TryFormat(destination, out charsWritten, format, provider);

		/// <inheritdoc/>
		public static BrainHalf Parse(string s, IFormatProvider? provider) => (BrainHalf)float.Parse(s, provider);
		/// <inheritdoc/>
		public static bool TryParse(string? s, IFormatProvider? provider, out BrainHalf result)
		{
			result = default;
			if (!float.TryParse(s, provider, out var f))
				return false;
			result = (BrainHalf)f;
			return true;
		}
		/// <inheritdoc/>
		public static BrainHalf Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => (BrainHalf)float.Parse(s, provider);
		/// <inheritdoc/>
		public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out BrainHalf result)
		{
			result = default;
			if (!float.TryParse(s, provider, out var f))
				return false;
			result = (BrainHalf)f;
			return true;
		}

		/// <inheritdoc/>
		public static BrainHalf Parse(string s, NumberStyles style, IFormatProvider? provider) => (BrainHalf)float.Parse(s, style, provider);
		/// <inheritdoc/>
		public static BrainHalf Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider) => (BrainHalf)float.Parse(s, style, provider);
		/// <inheritdoc/>
		public static bool TryParse(string s, NumberStyles style, IFormatProvider? provider, out BrainHalf result)
		{
			result = default;
			if (!float.TryParse(s, style, provider, out var f))
				return false;
			result = (BrainHalf)f;
			return true;
		}
		/// <inheritdoc/>
		public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out BrainHalf result)
		{
			result = default;
			if (!float.TryParse(s, style, provider, out var f))
				return false;
			result = (BrainHalf)f;
			return true;
		}
		#endregion
	}
}
