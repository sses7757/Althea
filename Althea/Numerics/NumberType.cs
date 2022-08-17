using System.Runtime.CompilerServices;


namespace Althea.Numerics;

#region native types
/// <summary>
/// The static class for primitive and custom number types' conversion
/// </summary>
public static class NumberConvert
{
	/// <summary>
	/// Create a number of type <typeparamref name="T2"/> from the given number of type <typeparamref name="T1"/>.
	/// </summary>
	/// <typeparam name="T1">The input number type</typeparam>
	/// <typeparam name="T2">The output number type</typeparam>
	/// <param name="x">The input number</param>
	/// <param name="y">The output number</param>
	/// <returns>Success or not.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryConvert<T1, T2>(T1 x, out T2 y) where T1 : unmanaged, IBaseNumber<T1> where T2 : unmanaged, IBaseNumber<T2>
	{
		return T1.TryConvertTo(x, out y) || T2.TryConvertFrom(x, out y);
	}

	/// <summary>
	/// Create a number of type <typeparamref name="T2"/> from the given number of type <typeparamref name="T1"/>.
	/// </summary>
	/// <typeparam name="T1">The input number type</typeparam>
	/// <typeparam name="T2">The output number type</typeparam>
	/// <param name="x">The input number</param>
	/// <param name="y">The output number</param>
	/// <returns>Success or not.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryConvertChecked<T1, T2>(T1 x, out T2 y) where T1 : unmanaged, IBaseNumber<T1> where T2 : unmanaged, IBaseNumber<T2>
	{
		return T1.TryConvertToChecked(x, out y) || T2.TryConvertFromChecked(x, out y);
	}

	/// <summary>
	/// Create a number of type <typeparamref name="T2"/> from the given number of type <typeparamref name="T1"/>.
	/// </summary>
	/// <typeparam name="T1">The input number type</typeparam>
	/// <typeparam name="T2">The output number type</typeparam>
	/// <param name="x">The input number</param>
	/// <returns><paramref name="x"/> as <typeparamref name="T2"/></returns>
	/// <exception cref="NotSupportedException">If the conversion is not available</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T2 As<T1, T2>(this T1 x) where T1 : unmanaged, IBaseNumber<T1> where T2 : unmanaged, IBaseNumber<T2>
	{
		if (TryConvert(x, out T2 y))
			return y;
		else
			throw new NotSupportedException(Resources.ArithmeticError.DataTypeNotAllow);
	}

	/// <summary>
	/// Create a number of type <typeparamref name="T2"/> from the given number of type <see cref="int"/>.
	/// </summary>
	/// <typeparam name="T2">The output number type</typeparam>
	/// <param name="x">The input number</param>
	/// <returns><paramref name="x"/> as <typeparamref name="T2"/></returns>
	/// <exception cref="NotSupportedException">If the conversion is not available</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T2 As<T2>(this int x) where T2 : unmanaged, IBaseNumber<T2> => As<SignedInt32, T2>(x);

	/// <summary>
	/// Create a number of type <typeparamref name="T2"/> from the given number of type <see cref="long"/>.
	/// </summary>
	/// <typeparam name="T2">The output number type</typeparam>
	/// <param name="x">The input number</param>
	/// <returns><paramref name="x"/> as <typeparamref name="T2"/></returns>
	/// <exception cref="NotSupportedException">If the conversion is not available</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T2 As<T2>(this long x) where T2 : unmanaged, IBaseNumber<T2> => As<SignedInt64, T2>(x);

	/// <summary>
	/// Create a number of type <typeparamref name="T2"/> from the given number of type <see cref="double"/>.
	/// </summary>
	/// <typeparam name="T2">The output number type</typeparam>
	/// <param name="x">The input number</param>
	/// <returns><paramref name="x"/> as <typeparamref name="T2"/></returns>
	/// <exception cref="NotSupportedException">If the conversion is not available</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T2 As<T2>(this double x) where T2 : unmanaged, IBaseNumber<T2> => As<Float64, T2>(x);

	/// <summary>
	/// Create a number of type <see cref="int"/> from the given number of type <typeparamref name="T1"/>.
	/// </summary>
	/// <typeparam name="T1">The input number type</typeparam>
	/// <param name="x">The input number</param>
	/// <returns><paramref name="x"/> as <see cref="int"/></returns>
	/// <exception cref="NotSupportedException">If the conversion is not available</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int AsInt32<T1>(this T1 x) where T1 : unmanaged, IBaseNumber<T1> => As<T1, SignedInt32>(x);

	/// <summary>
	/// Create a number of type <see cref="long"/> from the given number of type <typeparamref name="T1"/>.
	/// </summary>
	/// <typeparam name="T1">The input number type</typeparam>
	/// <param name="x">The input number</param>
	/// <returns><paramref name="x"/> as <see cref="long"/></returns>
	/// <exception cref="NotSupportedException">If the conversion is not available</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static long AsInt64<T1>(this T1 x) where T1 : unmanaged, IBaseNumber<T1> => As<T1, SignedInt64>(x);

	/// <summary>
	/// Create a number of type <see cref="double"/> from the given number of type <typeparamref name="T1"/>.
	/// </summary>
	/// <typeparam name="T1">The input number type</typeparam>
	/// <param name="x">The input number</param>
	/// <returns><paramref name="x"/> as <see cref="double"/></returns>
	/// <exception cref="NotSupportedException">If the conversion is not available</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static double AsDouble<T1>(this T1 x) where T1 : unmanaged, IBaseNumber<T1> => As<T1, Float64>(x);

	/// <summary>
	/// Get the real part of a number <paramref name="x"/> of any number type if it is a complex type, otherwise <paramref name="x"/> itself.
	/// </summary>
	/// <typeparam name="T">The number type</typeparam>
	/// <param name="x">The number to get real part</param>
	/// <returns>The real part of <paramref name="x"/> if it is a complex type, otherwise <paramref name="x"/> itself.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T ToReal<T>(this T x) where T : unmanaged, IBaseNumber<T> => (x + T.Conjugate(x)) / (T.One + T.One);

	/// <summary>
	/// Get the imaginary part of a number <paramref name="x"/> of any number type if it is a complex type, otherwise 0.
	/// </summary>
	/// <typeparam name="T">The number type</typeparam>
	/// <param name="x">The number to get real part</param>
	/// <returns>The imaginary part of <paramref name="x"/> if it is a complex type, otherwise 0.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T ToImag<T>(this T x) where T : unmanaged, IBaseNumber<T> => (x - T.Conjugate(x)) / (T.One + T.One);
}
#endregion
