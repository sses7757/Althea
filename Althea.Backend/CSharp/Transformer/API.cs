using System.Runtime.CompilerServices;

using Althea.Array;
using Althea.Helpers;


namespace Althea.Backend.CSharp.Transformer;

/// <summary>
/// The C# back-end of <see cref="Althea.Transformer.IAbstractApi"/> that supports storage locations of CPU memory.
/// </summary>
public class Api : Althea.Transformer.IAbstractApi
{
	#region basic
	void IDisposable.Dispose()
	{
		this.Disposed = true;
		GC.SuppressFinalize(this);
	}

	/// <inheritdoc/>
	public bool Disposed { get; set; } = false;

	/// <summary>
	/// Get the default <see cref="Api"/>.
	/// </summary>
	internal protected static readonly Api Default = new();
	#endregion

	#region operations
	/// <inheritdoc/>
	public virtual bool FourierTransform<T, TS1, TS2>(bool forward, DenseArrayWrapper<T, TS1> input, DenseArrayWrapper<T, TS2> output) where T : unmanaged, IBinaryFloat<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

	/// <inheritdoc/>
	public virtual bool FourierTransform<T, TS1, TS2>(DenseArrayWrapper<T, TS1> input, DenseArrayWrapper<Complex<T>, TS2> output) where T : unmanaged, IBinaryFloat<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<Complex<T>, TS2>;

	/// <inheritdoc/>
	public virtual bool FourierTransform<T, TS1, TS2>(DenseArrayWrapper<Complex<T>, TS1> input, DenseArrayWrapper<T, TS2> output) where T : unmanaged, IBinaryFloat<T> where TS1 : class, IStorage<Complex<T>, TS1> where TS2 : class, IStorage<T, TS2>;
	#endregion

	#region algorithm
	/// <summary>
	/// Compute the fast (inverse) Fourier transformation of <paramref name="array"/>.
	/// </summary>
	/// <typeparam name="T">The actual real floating point type</typeparam>
	/// <param name="array">The array to be in-place transformed whose size must be power of 2</param>
	/// <param name="forward">Whether to calculate the forward FFT or the inverse FFT</param>
	/// <exception cref="NotSupportedException">If <paramref name="array"/>'s length is not a power of 2</exception>
	public static void FFT<T>(Span<Complex<T>> array, bool forward) where T : unmanaged, IBinaryFloat<T>
	{
		// checks
		if (array.Length <= 1)
			return;
		if (!int.IsPow2(array.Length))
			throw new NotSupportedException();
		int n = array.Length;
		T inv = forward ? T.One : T.NegativeOne;

		// main computation
		for (int i = 0; i < n; ++i)
		{	// change array elements to final positions
			int rev = i.ReverseBits();
			if (i < rev)
				(array[i], array[rev]) = (array[rev], array[i]);
		}
		for (int i = 1; i < n; i <<= 1)
		{   // combine ranges
			var (sin, cos) = T.SinCos(T.Pi / i.As<T>());
			// get complex root of x^n == 0
			Complex<T> wn = new(cos, inv * sin);
			for (int j = 0; j < n; j += i << 1)
			{   // enumerate ranges
				Complex<T> w0 = new(T.One);
				for (int k = 0; k < i; ++k, w0 *= wn)
				{   // combine
					Complex<T> x = array[j + k], y = w0 * array[i + j + k];
					array[j + k] = x + y;
					array[i + j + k] = x - y;
				}
			}
		}
	}

	/// <summary>
	/// Compute the fast (inverse) Fourier transformation of <paramref name="array"/>.
	/// </summary>
	/// <typeparam name="T">The actual real floating point type</typeparam>
	/// <param name="array">The array to be in-place transformed whose size must be power of 2</param>
	/// <param name="forward">Whether to calculate the forward FFT or the inverse FFT</param>
	/// <exception cref="NotSupportedException">If <paramref name="array"/>'s length is not a power of 2 or <typeparamref name="T"/> is not a complex type</exception>
	public static unsafe void FFT<T>(Span<T> array, bool forward) where T : unmanaged, IBinaryFloat<T>
	{
		// checks
		if (!T.IsComplexType)
			throw new NotSupportedException();
		if (array.Length <= 1)
			return;
		if (!int.IsPow2(array.Length))
			throw new NotSupportedException();
		int n = array.Length;
		T inv = forward ? T.One : T.NegativeOne;

		// main computation
		for (int i = 0; i < n; ++i)
		{   // change array elements to final positions
			int rev = i.ReverseBits();
			if (i < rev)
				(array[i], array[rev]) = (array[rev], array[i]);
		}
		for (int i = 1; i < n; i <<= 1)
		{   // combine ranges
			var (sin, cos) = T.SinCos(T.Pi / i.As<T>());
			// get complex root of x^n == 0
			sin *= inv;
			T wn = default;
			ref byte wnr = ref Unsafe.As<T, byte>(ref wn);
			Unsafe.CopyBlockUnaligned(ref wnr, ref Unsafe.As<T, byte>(ref cos), (uint)(sizeof(T) / 2));
			Unsafe.CopyBlockUnaligned(ref Unsafe.Add(ref wnr, sizeof(T) / 2), ref Unsafe.As<T, byte>(ref sin), (uint)(sizeof(T) / 2));
			for (int j = 0; j < n; j += i << 1)
			{   // enumerate ranges
				T w0 = T.One;
				for (int k = 0; k < i; ++k, w0 *= wn)
				{   // combine
					T x = array[j + k], y = w0 * array[i + j + k];
					array[j + k] = x + y;
					array[i + j + k] = x - y;
				}
			}
		}
	}
	#endregion
}
