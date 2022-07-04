using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

using Althea.Array;
using Althea.Helpers;

using LA = Althea.Backend.CSharp.LinearAlgebra.Api;


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
	// TODO: real to real

	/// <summary>
	/// Compute the fast (inverse) Fourier transformation of a real <paramref name="array"/> to a conjugate-even <paramref name="output"/> array.
	/// </summary>
	/// <remarks>If FFT will be performed, <paramref name="output"/> is stored as <c>f[0].Real, f[1].Real, ..., f[n/2].Real, f[1].Imag, ..., f[n/2 - 1].Imag</c> where <c>f</c> is the complex Fourier transformation result; otherwise, for IFFT, <paramref name="array"/> must be of such form.</remarks>
	/// <typeparam name="T">The actual real floating point type</typeparam>
	/// <param name="array">The input array to be transformed whose size must be power of 2</param>
	/// <param name="output">The output array to store the transformation result</param>
	/// <param name="forward">Whether to calculate the forward FFT or the inverse FFT</param>
	/// <exception cref="ArgumentException">If <paramref name="array"/> and <paramref name="output"/> have different sizes or they overlap</exception>
	/// <exception cref="NotSupportedException">If <paramref name="array"/>'s length is not a power of 2</exception>
	/// <exception cref="TypeMismatchException">If <typeparamref name="T"/> is not a real type</exception>
	public static unsafe void FFT<T>(ReadOnlySpan<T> array, Span<T> output, bool forward) where T : unmanaged, IBinaryFloat<T>
	{
		if (array.Length < 1)
			return;
		if (array.Length == 1)
		{
			output[0] = array[0];
			return;
		}
		if (T.IsComplexType)
			throw new TypeMismatchException(typeof(T), TypeMismatchException.MismatchReason.NotReal);
		int n = array.Length;
		if (output.Length != n)
			throw new ArgumentException(Resources.ParameterError.NotSameSize);
		if (array.Overlaps(output))
			throw new ArgumentException(Resources.ParameterError.InvalidValue);
		if (!int.IsPow2(n))
			throw new NotSupportedException();
		int bits = 32 - int.Log2(n);

		array.CopyTo(output);
		fixed (T* a = array, b = output)
		{
			if (forward)
			{
				T* bre = b, bim = b + (n / 2 + 1);
				// change array elements to final positions
				for (int i = 0; i < n; ++i)
				{
					int rev = (int)((uint)i.ReverseBits() >> bits);
					if (i < rev)
						(b[i], b[rev]) = (b[rev], b[i]);
				}
				// combine ranges
				for (int i = 1; i < n; i <<= 1)
				{
					var (sin, cos) = T.SinCos(T.Pi / i.As<T>());
					// get 1st complex root of x^i == 0
					Complex<T> wn = new(cos, sin);
					for (int j = 0; j < n; j += i << 1)
					{   // enumerate ranges
						Complex<T> w0 = T.One;
						Complex<T>* aj = a + j, aij = a + (i + j), end = aij;
						for (; aj < end; aj++, aij++, w0 *= wn)
						{   // combine
							Complex<T> x = *aj, y = w0 * *aij;
							*aj = x + y;
							*aij = x - y;
						}
					}
				}
			}
			else
			{

			}
		}
	}

	/// <summary>
	/// Compute the fast (inverse) Fourier transformation of <paramref name="array"/>.
	/// </summary>
	/// <typeparam name="T">The actual real floating point type</typeparam>
	/// <param name="array">The array to be in-place transformed whose size must be power of 2</param>
	/// <param name="forward">Whether to calculate the forward FFT or the inverse FFT</param>
	/// <exception cref="NotSupportedException">If <paramref name="array"/>'s length is not a power of 2</exception>
	/// <exception cref="TypeMismatchException">If <typeparamref name="T"/> is not a real type</exception>
	public static unsafe void FFT<T>(Span<Complex<T>> array, bool forward) where T : unmanaged, IBinaryFloat<T>
	{
		// checks
		if (array.Length <= 1)
			return;
		if (!int.IsPow2(array.Length))
			throw new NotSupportedException();
		if (T.IsComplexType)
			throw new TypeMismatchException(typeof(T), TypeMismatchException.MismatchReason.NotReal);
		int n = array.Length, bits = 32 - int.Log2(n);
		T inv = forward ? T.One : T.NegativeOne;

		// main computation
		int COUNT = Vector256<byte>.Count / T.Size;
		T* rootsRe = stackalloc T[COUNT], rootsIm = stackalloc T[COUNT];
		rootsRe[0] = T.One; rootsIm[0] = T.Zero;
		fixed (Complex<T>* a = array)
		{
			// change array elements to final positions
			for (int i = 0; i < n; ++i)
			{
				int rev = (int)((uint)i.ReverseBits() >> bits);
				if (i < rev)
					(a[i], a[rev]) = (a[rev], a[i]);
			}
			// combine ranges
			for (int i = 1; i < n; i <<= 1)
			{
				var (sin, cos) = T.SinCos(T.Pi / i.As<T>());
				sin *= inv;
				if (!Vector.IsHardwareAccelerated || !Avx.IsSupported || (typeof(T) != typeof(Float32) && typeof(T) != typeof(Float64)) || i < 4)
				{
					// get 1st complex root of x^i == 0
					Complex<T> wn = new(cos, sin);
					for (int j = 0; j < n; j += i << 1)
					{   // enumerate ranges
						Complex<T> w0 = T.One;
						Complex<T>* aj = a + j, aij = a + (i + j), end = aij;
						for (; aj < end; aj++, aij++, w0 *= wn)
						{   // combine
							Complex<T> x = *aj, y = w0 * *aij;
							*aj = x + y;
							*aij = x - y;
						}
					}
				}
				else
				{
					// get first few complex root of x^i == 0
					for (int ii = 1; ii < 4; ii++)
					{
						rootsRe[ii] = rootsRe[ii - 1] * cos - rootsIm[ii - 1] * sin;
						rootsIm[ii] = rootsIm[ii - 1] * cos + rootsRe[ii - 1] * sin;
					}
					T cosN = cos, sinN = sin;
					for (int ii = 1; ii < 4; ii++)
					{
						(cosN, sinN) = (cosN * cos - sinN * sin, sinN * cos + cosN * sin);
					}
					// permute to fit unpacked complex order
					(rootsRe[1], rootsRe[2]) = (rootsRe[2], rootsRe[1]);
					(rootsIm[1], rootsIm[2]) = (rootsIm[2], rootsIm[1]);
					// enumerate ranges
					for (int j = 0; j < n; j += i << 1)
					{
						[MethodImpl(MethodImplOptions.AggressiveOptimization)]
						void Exec<U>(Complex<T>* a, U cos, U sin) where U : unmanaged, System.Numerics.INumber<U>
						{
							U* aj = (U*)(a + j), aij = (U*)(a + i + j), end = aij;
							Vector256<U> wre = LA.LoadVector256((U*)rootsRe), wim = LA.LoadVector256((U*)rootsIm);
							while (aj < end)
							{   // combine
								// Complex x = a[j + k];
								var xs1 = LA.LoadVector256(aj);
								var xs2 = LA.LoadVector256(aj + Vector256<U>.Count);
								LA.ComplexUnpack(xs1, xs2, out var xre, out var xim);
								// Complex y = w0 * a[i + j + k];
								var ys1 = LA.LoadVector256(aij);
								var ys2 = LA.LoadVector256(aij + Vector256<U>.Count);
								LA.ComplexUnpack(ys1, ys2, out var yre, out var yim);
								LA.UnpackComplexMultiply(wre, wim, yre, yim, ref yre, ref yim);
								// a[j + k] = x + y;
								Vector256<U> xyRe = xre + yre, xyIm = xim + yim;
								LA.ComplexPack(xyRe, xyIm, out var xy1, out var xy2);
								LA.StoreVector256(xy1, aj);
								LA.StoreVector256(xy2, aj + Vector256<U>.Count);
								// a[i + j + k] = x - y;
								xyRe = xre - yre; xyIm = xim - yim;
								LA.ComplexPack(xyRe, xyIm, out xy1, out xy2);
								LA.StoreVector256(xy1, aij);
								LA.StoreVector256(xy2, aij + Vector256<U>.Count);
								// increment
								LA.UnpackComplexMultiply(wre, wim, cos, sin, ref wre, ref wim);
								aj += Vector256<U>.Count * 2; aij += Vector256<U>.Count * 2;
							}
						}
						if (typeof(T) == typeof(Float64))
							Exec(a, *(double*)&cosN, *(double*)&sinN);
						else
							Exec(a, *(float*)&cosN, *(float*)&sinN);
					}
				}
			}
			T scale = T.ReciprocalSqrtEstimate(n.As<T>());
			for (int i = 0; i < n; ++i)
				a[i] *= scale;
		}
	}

	/// <summary>
	/// Compute the fast Fourier transformation of a real <paramref name="array"/> to a complex one.
	/// </summary>
	/// <typeparam name="T">The actual real floating point type</typeparam>
	/// <param name="array">The array to be out-of-place transformed whose size must be power of 2</param>
	/// <param name="output">The array to store the Fourier transformation result of <paramref name="array"/></param>
	/// <exception cref="NotSupportedException">If <paramref name="array"/>'s length is not a power of 2</exception>
	/// <exception cref="ArgumentException">If <paramref name="output"/>'s length != <paramref name="array"/>'s length</exception>
	public static unsafe void FFT<T>(ReadOnlySpan<T> array, Span<Complex<T>> output) where T : unmanaged, IBinaryFloat<T>
	{
		// checks
		if (array.Length < 1)
			return;
		if (array.Length == 1)
		{
			output[0] = array[0];
			return;
		}
		if (T.IsComplexType)
			throw new TypeMismatchException(typeof(T), TypeMismatchException.MismatchReason.NotReal);
		if (output.Length != array.Length)
			throw new ArgumentException(Resources.ParameterError.NotSameSize);
		if (!int.IsPow2(array.Length))
			throw new NotSupportedException();

		output.Fill(T.Zero);
		fixed (T* src = array)
		fixed (Complex<T>* dst = output)
		{
			Storage.Api.StridedCopy(src, (T*)dst, 1, 2, array.Length);
		}
		FFT(output, true);
	}

	/// <summary>
	/// Compute the fast inverse Fourier transformation of a complex <paramref name="array"/> to a real one.
	/// </summary>
	/// <remarks>This method does NOT check whether <paramref name="array"/> is a conjugate-even one so that <paramref name="output"/> is pure real.</remarks>
	/// <typeparam name="T">The actual real floating point type</typeparam>
	/// <param name="array">The array to be out-of-place transformed whose size must be power of 2</param>
	/// <param name="output">The array to store the Fourier transformation result of <paramref name="array"/></param>
	/// <exception cref="NotSupportedException">If <paramref name="array"/>'s length is not a power of 2</exception>
	/// <exception cref="ArgumentException">If <paramref name="output"/>'s length != <paramref name="array"/>'s length</exception>
	public static unsafe void IFFT<T>(ReadOnlySpan<Complex<T>> array, Span<T> output) where T : unmanaged, IBinaryFloat<T>
	{
		// checks
		if (array.Length < 1)
			return;
		if (array.Length == 1)
		{
			output[0] = array[0].Real;
			return;
		}
		if (T.IsComplexType)
			throw new TypeMismatchException(typeof(T), TypeMismatchException.MismatchReason.NotReal);
		int n = array.Length;
		if (output.Length != n)
			throw new ArgumentException(Resources.ParameterError.NotSameSize);
		if (!int.IsPow2(n))
			throw new NotSupportedException();

		using var buffer = Storage.Buffers.Create<Complex<T>>(n * T.Size * 2);
		fixed (Complex<T>* src = array)
		fixed (T* dst = output)
		{
			Unsafe.CopyBlockUnaligned(buffer, src, (uint)(n * T.Size * 2));
			FFT(new Span<Complex<T>>(buffer, n), false);
			Storage.Api.StridedCopy((T*)(void*)buffer, dst, 2, 1, n);
		}
	}
	#endregion
}
