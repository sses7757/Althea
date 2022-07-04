using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

using Althea.Array;
using Althea.Backend.CSharp.Storage;
using Althea.Backend.Storage;
using Althea.Helpers;
using Althea.LinearAlgebra;
using Althea.LinearAlgebra.Dense;
using Althea.LinearAlgebra.Sparse;


namespace Althea.Backend.CSharp.LinearAlgebra;

/// <summary>
/// The C# back-end of <see cref="IBlasAbstractApi"/> that utilizes <see cref="System.Runtime.Intrinsics"/> and <see cref="Vector{T}"/>.<br/>
/// Only supports storages on CPU memory of primitive and pre-defined types and single-threaded vector operations.
/// </summary>
public unsafe partial class Api : IBlasAbstractApi, IExtendBlasAbstractApi, IConversionAbstractApi, ILapackAbstractApi
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


	#region helpers
	#region load and simple op
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool GetPointer<T, TS>(TS s, long stride, out T* pointer, out int length, out int inc) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
	{
		pointer = null; length = inc = 0;
		if (s is null || !s.IsValid())
			throw new ArgumentNullException(nameof(s));
		if (stride <= 0)
			throw new ArgumentOutOfRangeException(nameof(stride), stride, Resources.ParameterError.MustPositive);
		if (s is not PureStorage<T, CpuMemoryPointer> ps)
			return false; // not support
		pointer = ps.Pointer.Pointer.UnmangedPointer<T>(ps.Pointer.OffsetInBytes);
		if (pointer == default)
			return false; // not support
		length = (int)ps.Length;
		inc = (int)stride;
		length = (length - 1) / inc + 1;
		return true;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool GetPointer<T, TS>(TS? s, long m, long n, long ld, out T* pointer, out int mm, out int nn, out int ldd) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
	{
		pointer = null; mm = nn = ldd = 0;
		if (s is null || !s.IsValid())
			return true;
		if (m <= 0)
			throw new ArgumentOutOfRangeException(nameof(m), m, Resources.ParameterError.MustPositive);
		if (n <= 0)
			throw new ArgumentOutOfRangeException(nameof(n), n, Resources.ParameterError.MustPositive);
		if (ld < m)
			throw new ArgumentOutOfRangeException(nameof(ld), ld, Resources.ParameterError.InvalidValue);
		if (s is not PureStorage<T, CpuMemoryPointer> ps)
			return false; // not support
		pointer = ps.Pointer.Pointer.UnmangedPointer<T>(ps.Pointer.OffsetInBytes);
		if (pointer == default)
			return false; // not support
		if (ps.Length < (n - 1) * ld + m)
			throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(s));
		mm = (int)m; nn = (int)n; ldd = (int)ld;
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Vector<T> LoadVector<T>(T* r) where T : unmanaged => Unsafe.ReadUnaligned<Vector<T>>(r);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Vector<T> LoadVector<T>(void* r) where T : unmanaged => Unsafe.ReadUnaligned<Vector<T>>(r);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static Vector256<T> LoadVector256<T>(T* r) where T : unmanaged => Unsafe.ReadUnaligned<Vector256<T>>(r);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Vector256<T> LoadVector256<T>(void* r) where T : unmanaged => Unsafe.ReadUnaligned<Vector256<T>>(r);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void LoadVector<T>(T* r, out Vector<T> v1, out Vector<T> v2) where T : unmanaged
	{
		v1 = Unsafe.ReadUnaligned<Vector<T>>(r);
		v2 = Unsafe.ReadUnaligned<Vector<T>>(r + Vector<T>.Count);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void LoadVector<T>(T* r, out Vector<T> v1, out Vector<T> v2, out Vector<T> v3, out Vector<T> v4) where T : unmanaged
	{
		v1 = Unsafe.ReadUnaligned<Vector<T>>(r);
		v2 = Unsafe.ReadUnaligned<Vector<T>>(r + Vector<T>.Count);
		v3 = Unsafe.ReadUnaligned<Vector<T>>(r + Vector<T>.Count * 2);
		v4 = Unsafe.ReadUnaligned<Vector<T>>(r + Vector<T>.Count * 3);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void LoadVector<T>(T* r, out Vector<T> v1, out Vector<T> v2, out Vector<T> v3, out Vector<T> v4, out Vector<T> v5, out Vector<T> v6, out Vector<T> v7, out Vector<T> v8) where T : unmanaged
	{
		v1 = Unsafe.ReadUnaligned<Vector<T>>(r);
		v2 = Unsafe.ReadUnaligned<Vector<T>>(r + Vector<T>.Count);
		v3 = Unsafe.ReadUnaligned<Vector<T>>(r + Vector<T>.Count * 2);
		v4 = Unsafe.ReadUnaligned<Vector<T>>(r + Vector<T>.Count * 3);
		v5 = Unsafe.ReadUnaligned<Vector<T>>(r + Vector<T>.Count * 4);
		v6 = Unsafe.ReadUnaligned<Vector<T>>(r + Vector<T>.Count * 5);
		v7 = Unsafe.ReadUnaligned<Vector<T>>(r + Vector<T>.Count * 6);
		v8 = Unsafe.ReadUnaligned<Vector<T>>(r + Vector<T>.Count * 7);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void StoreVector<T>(Vector<T> v, void* r) where T : unmanaged => Unsafe.WriteUnaligned(r, v);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void StoreVector256<T>(Vector256<T> v, void* r) where T : unmanaged => Unsafe.WriteUnaligned(r, v);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void StoreVector<T>(Vector<T> v1, Vector<T> v2, T* r) where T : unmanaged
	{
		Unsafe.WriteUnaligned(r, v1); Unsafe.WriteUnaligned(r + Vector<T>.Count, v2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void StoreVector<T>(Vector<T> v1, Vector<T> v2, Vector<T> v3, Vector<T> v4, T* r) where T : unmanaged
	{
		Unsafe.WriteUnaligned(r + Vector<T>.Count * 0, v1);
		Unsafe.WriteUnaligned(r + Vector<T>.Count * 1, v2);
		Unsafe.WriteUnaligned(r + Vector<T>.Count * 2, v3);
		Unsafe.WriteUnaligned(r + Vector<T>.Count * 3, v4);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void StoreVector<T>(Vector<T> v1, Vector<T> v2, Vector<T> v3, Vector<T> v4, Vector<T> v5, Vector<T> v6, Vector<T> v7, Vector<T> v8, T* r) where T : unmanaged, IBaseNumber<T>
	{
		Unsafe.WriteUnaligned(r + Vector<T>.Count * 0, v1);
		Unsafe.WriteUnaligned(r + Vector<T>.Count * 1, v2);
		Unsafe.WriteUnaligned(r + Vector<T>.Count * 2, v3);
		Unsafe.WriteUnaligned(r + Vector<T>.Count * 3, v4);
		Unsafe.WriteUnaligned(r + Vector<T>.Count * 4, v5);
		Unsafe.WriteUnaligned(r + Vector<T>.Count * 5, v6);
		Unsafe.WriteUnaligned(r + Vector<T>.Count * 6, v7);
		Unsafe.WriteUnaligned(r + Vector<T>.Count * 7, v8);
	}


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Vector256<float> ComplexSquareAbsNoOrder(Vector256<float> current1, Vector256<float> current2)
	{
		current1 *= current1;
		current2 *= current2;
		Vector256<float> squares = Avx.HorizontalAdd(current1, current2);
		return squares;
		// abs of {0, 1, 4, 5, 2, 3, 6, 7}-th complex
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Vector256<double> ComplexSquareAbsNoOrder(Vector256<double> current1, Vector256<double> current2)
	{
		current1 *= current1;
		current2 *= current2;
		Vector256<double> squares = Avx.HorizontalAdd(current1, current2);
		return squares;
		// abs of {0, 2, 1, 3}-th complex
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Vector256<float> ComplexSquareAbsNoOrderSingle(void* p)
	{
		Vector256<float> current1 = LoadVector256<float>(p);
		Vector256<float> current2 = LoadVector256((float*)p + Vector256<float>.Count);
		return ComplexSquareAbsNoOrder(current1, current2);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Vector256<double> ComplexSquareAbsNoOrderDouble(void* p)
	{
		Vector256<double> current1 = LoadVector256<double>(p);
		Vector256<double> current2 = LoadVector256((double*)p + Vector256<double>.Count);
		return ComplexSquareAbsNoOrder(current1, current2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Vector256<float> ComplexSquareAbsOrder(Vector256<float> current1, Vector256<float> current2)
	{
		Vector256<float> squares = ComplexSquareAbsNoOrder(current1, current2);
		squares = Avx2.Permute4x64(squares.AsDouble(), 0b11_01_10_00).AsSingle();
		return squares;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Vector256<double> ComplexSquareAbsOrder(Vector256<double> current1, Vector256<double> current2)
	{
		Vector256<double> squares = ComplexSquareAbsNoOrder(current1, current2);
		squares = Avx2.Permute4x64(squares, 0b11_01_10_00);
		return squares;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Vector256<float> ComplexSquareAbsOrder(float* p)
	{
		Vector256<float> current1 = LoadVector256(p);
		Vector256<float> current2 = LoadVector256(p + Vector256<float>.Count);
		return ComplexSquareAbsOrder(current1, current2);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Vector256<double> ComplexSquareAbsOrder(double* p)
	{
		Vector256<double> current1 = LoadVector256(p);
		Vector256<double> current2 = LoadVector256(p + Vector256<double>.Count);
		return ComplexSquareAbsOrder(current1, current2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void ComplexUnpack<T>(Vector256<T> a0, Vector256<T> a1, out Vector256<T> realA, out Vector256<T> imagA) where T : unmanaged
	{
		if (typeof(T) == typeof(float))
		{
			ComplexUnpack(*(Vector256<float>*)&a0, *(Vector256<float>*)&a1, out var re, out var im);
			realA = *(Vector256<T>*)&re; imagA = *(Vector256<T>*)&im;
		}
		else
		{
			ComplexUnpack(*(Vector256<double>*)&a0, *(Vector256<double>*)&a1, out var re, out var im);
			realA = *(Vector256<T>*)&re; imagA = *(Vector256<T>*)&im;
		}
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void ComplexUnpack(Vector256<float> a0, Vector256<float> a1, out Vector256<float> realA, out Vector256<float> imagA)
	{
		// {a[0].r, a[4].r, a[0].i, a[4].i, a[2].r, a[6].r, a[2].i, a[6].i}
		Vector256<float> tempA0 = Avx.UnpackLow(a0, a1);
		// {a[1].r, a[5].r, a[1].i, a[5].i, a[3].r, a[7].r, a[3].i, a[7].i}
		Vector256<float> tempA1 = Avx.UnpackHigh(a0, a1);
		// {a[0].r, a[1].r, a[4].r, a[5].r, a[2].r, a[3].r, a[6].r, a[7].r}
		realA = Avx.UnpackLow(tempA0, tempA1);
		// {a[0].i, a[1].i, a[4].i, a[5].i, a[2].i, a[3].i, a[6].i, a[7].i}
		imagA = Avx.UnpackHigh(tempA0, tempA1);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void ComplexUnpack(Vector256<float> a0, Vector256<float> a1, Vector256<float> b0, Vector256<float> b1, out Vector256<float> realA, out Vector256<float> imagA, out Vector256<float> realB, out Vector256<float> imagB)
	{
		ComplexUnpack(a0, a1, out realA, out imagA);
		ComplexUnpack(b0, b1, out realB, out imagB);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void ComplexUnpack(Vector256<double> a0, Vector256<double> a1, out Vector256<double> realA, out Vector256<double> imagA)
	{
		// {a[0].r, a[2].r, a[1].r, a[3].r}
		realA = Avx.UnpackLow(a0, a1);
		// {a[0].i, a[2].i, a[1].i, a[3].i}
		imagA = Avx.UnpackHigh(a0, a1);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void ComplexUnpack(Vector256<double> a0, Vector256<double> a1, Vector256<double> b0, Vector256<double> b1, out Vector256<double> realA, out Vector256<double> imagA, out Vector256<double> realB, out Vector256<double> imagB)
	{
		ComplexUnpack(a0, a1, out realA, out imagA);
		ComplexUnpack(b0, b1, out realB, out imagB);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void ComplexPack<T>(Vector256<T> real, Vector256<T> imag, out Vector256<T> c0, out Vector256<T> c1) where T : unmanaged
	{
		if (typeof(T) == typeof(float))
		{
			ComplexPack(*(Vector256<float>*)&real, *(Vector256<float>*)&imag, out var a0, out var a1);
			c0 = *(Vector256<T>*)&a0; c1 = *(Vector256<T>*)&a1;
		}
		else
		{
			ComplexPack(*(Vector256<double>*)&real, *(Vector256<double>*)&imag, out var a0, out var a1);
			c0 = *(Vector256<T>*)&a0; c1 = *(Vector256<T>*)&a1;
		}
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void ComplexPack(Vector256<float> real, Vector256<float> imag, out Vector256<float> c0, out Vector256<float> c1)
	{
		c0 = Avx.UnpackLow(real, imag);
		c1 = Avx.UnpackHigh(real, imag);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void ComplexPack(Vector256<double> real, Vector256<double> imag, out Vector256<double> c0, out Vector256<double> c1)
	{
		c0 = Avx.UnpackLow(real, imag);
		c1 = Avx.UnpackHigh(real, imag);
	}
	#endregion

	#region complex multiply
	// The main bottleneck of complex multiply and division is 4/6/8/10 shuffles (unpacks)
	//	which can be eliminated by using separate complex storing approach
	// It can be done by using a special version of MixedStroage<Complex<T>> of two identical storage locations,
	//	and implement special APIs for that type of storage.

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void UnpackComplexMultiplyAdd<Conj>(Vector256<float> realA, Vector256<float> imagA, Vector256<float> realB, Vector256<float> imagB, ref Vector256<float> realC, ref Vector256<float> imagC)
	{
		bool conj = typeof(Conj) == typeof(bool);
		// multiply
		// the branch shall be eliminated by JIT
		var ArBr = realA * realB;
		var AiBi = imagA * imagB;
		var ArBi = realA * imagB;
		var AiBr = imagA * realB;
		realC += ArBr;
		imagC += ArBi;
		if (!conj)
		{
			realC -= AiBi;
			imagC += AiBr;
		}
		else
		{
			realC += AiBi;
			imagC -= AiBr;
		}
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void UnpackComplexMultiplyAdd<Conj>(Vector256<double> realA, Vector256<double> imagA, Vector256<double> realB, Vector256<double> imagB, ref Vector256<double> realC, ref Vector256<double> imagC)
	{
		bool conj = typeof(Conj) == typeof(bool);
		// multiply
		// the branch shall be eliminated by JIT
		var ArBr = realA * realB;
		var AiBi = imagA * imagB;
		var ArBi = realA * imagB;
		var AiBr = imagA * realB;
		realC += ArBr;
		imagC += ArBi;
		if (!conj)
		{
			realC -= AiBi;
			imagC += AiBr;
		}
		else
		{
			realC += AiBi;
			imagC -= AiBr;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void UnpackComplexMultiply<T>(Vector256<T> realA, Vector256<T> imagA, Vector256<T> realB, Vector256<T> imagB, ref Vector256<T> realC, ref Vector256<T> imagC) where T : unmanaged
	{
		if (typeof(T) == typeof(float))
		{
			UnpackComplexMultiply<byte>(*(Vector256<float>*)&realA, *(Vector256<float>*)&imagA, *(Vector256<float>*)&realB, *(Vector256<float>*)&imagB, ref Unsafe.As<Vector256<T>, Vector256<float>>(ref realC), ref Unsafe.As<Vector256<T>, Vector256<float>>(ref imagC));
		}
		else
		{
			UnpackComplexMultiply<byte>(*(Vector256<double>*)&realA, *(Vector256<double>*)&imagA, *(Vector256<double>*)&realB, *(Vector256<double>*)&imagB, ref Unsafe.As<Vector256<T>, Vector256<double>>(ref realC), ref Unsafe.As<Vector256<T>, Vector256<double>>(ref imagC));
		}
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void UnpackComplexMultiply<Conj>(Vector256<float> realA, Vector256<float> imagA, Vector256<float> realB, Vector256<float> imagB, ref Vector256<float> realC, ref Vector256<float> imagC)
	{
		bool conj = typeof(Conj) == typeof(bool);
		// multiply
		// the branch shall be eliminated by JIT
		var ArBr = realA * realB;
		var AiBi = imagA * imagB;
		var ArBi = realA * imagB;
		var AiBr = imagA * realB;
		if (!conj)
		{
			realC = ArBr - AiBi;
			imagC = ArBi + AiBr;
		}
		else
		{
			realC = ArBr + AiBi;
			imagC = ArBi - AiBr;
		}
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void UnpackComplexMultiply<Conj>(Vector256<double> realA, Vector256<double> imagA, Vector256<double> realB, Vector256<double> imagB, ref Vector256<double> realC, ref Vector256<double> imagC)
	{
		bool conj = typeof(Conj) == typeof(bool);
		// multiply
		// the branch shall be eliminated by JIT
		var ArBr = realA * realB;
		var AiBi = imagA * imagB;
		var ArBi = realA * imagB;
		var AiBr = imagA * realB;
		if (!conj)
		{
			realC = ArBr - AiBi;
			imagC = ArBi + AiBr;
		}
		else
		{
			realC = ArBr + AiBi;
			imagC = ArBi - AiBr;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void UnpackComplexMultiply<T>(Vector256<T> realA, Vector256<T> imagA, T realB, T imagB, ref Vector256<T> realC, ref Vector256<T> imagC) where T : unmanaged
	{
		if (typeof(T) == typeof(float))
		{
			UnpackComplexMultiply<byte>(*(Vector256<float>*)&realA, *(Vector256<float>*)&imagA, *(float*)&realB, *(float*)&imagB, ref Unsafe.As<Vector256<T>, Vector256<float>>(ref realC), ref Unsafe.As<Vector256<T>, Vector256<float>>(ref imagC));
		}
		else
		{
			UnpackComplexMultiply<byte>(*(Vector256<double>*)&realA, *(Vector256<double>*)&imagA, *(double*)&realB, *(double*)&imagB, ref Unsafe.As<Vector256<T>, Vector256<double>>(ref realC), ref Unsafe.As<Vector256<T>, Vector256<double>>(ref imagC));
		}
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void UnpackComplexMultiply<Conj>(Vector256<float> realA, Vector256<float> imagA, float realB, float imagB, ref Vector256<float> realC, ref Vector256<float> imagC)
	{
		bool conj = typeof(Conj) == typeof(bool);
		// multiply
		// the branch shall be eliminated by JIT
		var ArBr = realA * realB;
		var AiBi = imagA * imagB;
		var ArBi = realA * imagB;
		var AiBr = imagA * realB;
		if (!conj)
		{
			realC = ArBr - AiBi;
			imagC = ArBi + AiBr;
		}
		else
		{
			realC = ArBr + AiBi;
			imagC = ArBi - AiBr;
		}
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void UnpackComplexMultiply<Conj>(Vector256<double> realA, Vector256<double> imagA, double realB, double imagB, ref Vector256<double> realC, ref Vector256<double> imagC)
	{
		bool conj = typeof(Conj) == typeof(bool);
		// multiply
		// the branch shall be eliminated by JIT
		var ArBr = realA * realB;
		var AiBi = imagA * imagB;
		var ArBi = realA * imagB;
		var AiBr = imagA * realB;
		if (!conj)
		{
			realC = ArBr - AiBi;
			imagC = ArBi + AiBr;
		}
		else
		{
			realC = ArBr + AiBi;
			imagC = ArBi - AiBr;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void ComplexMultiplyAdd<Conj>(Vector256<float> a0, Vector256<float> a1, Vector256<float> b0, Vector256<float> b1, ref Vector256<float> realC, ref Vector256<float> imagC)
	{
		ComplexUnpack(a0, a1, b0, b1, out var realA, out var imagA, out var realB, out var imagB);
		UnpackComplexMultiplyAdd<Conj>(realA, imagA, realB, imagB, ref realC, ref imagC);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void ComplexMultiplyAdd<Conj>(Vector256<double> a0, Vector256<double> a1, Vector256<double> b0, Vector256<double> b1, ref Vector256<double> realC, ref Vector256<double> imagC)
	{
		ComplexUnpack(a0, a1, b0, b1, out var realA, out var imagA, out var realB, out var imagB);
		UnpackComplexMultiplyAdd<Conj>(realA, imagA, realB, imagB, ref realC, ref imagC);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void ComplexMultiply<Conj, PackOut>(Vector256<float> a0, Vector256<float> a1, Vector256<float> b0, Vector256<float> b1, out Vector256<float> c0, out Vector256<float> c1)
	{
		bool conj = typeof(Conj) == typeof(bool);
		bool packOutput = typeof(PackOut) == typeof(bool);
		ComplexUnpack(a0, a1, b0, b1, out var realA, out var imagA, out var realB, out var imagB);

		Vector256<float> AiBi = imagA * imagB;
		Vector256<float> AiBr = imagA * realB;
		Vector256<float> real, imag;
		var ArBr = realA * realB;
		var ArBi = realA * imagB;
		if (!conj)
		{
			real = ArBr - AiBi;
			imag = ArBi + AiBr;
		}
		else
		{
			real = ArBr + AiBi;
			imag = ArBi - AiBr;
		}
		if (packOutput)
		{
			ComplexPack(real, imag, out c0, out c1);
		}
		else
		{
			c0 = real; c1 = imag;
		}
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void ComplexMultiply<Conj, PackOut>(Vector256<double> a0, Vector256<double> a1, Vector256<double> b0, Vector256<double> b1, out Vector256<double> c0, out Vector256<double> c1)
	{
		bool conj = typeof(Conj) == typeof(bool);
		bool packOutput = typeof(PackOut) == typeof(bool);
		ComplexUnpack(a0, a1, b0, b1, out var realA, out var imagA, out var realB, out var imagB);

		Vector256<double> AiBi = imagA * imagB;
		Vector256<double> AiBr = imagA * realB;
		Vector256<double> real, imag;
		var ArBr = realA * realB;
		var ArBi = realA * imagB;
		if (!conj)
		{
			real = ArBr - AiBi;
			imag = ArBi + AiBr;
		}
		else
		{
			real = ArBr + AiBi;
			imag = ArBi - AiBr;
		}
		if (packOutput)
		{
			ComplexPack(real, imag, out c0, out c1);
		}
		else
		{
			c0 = real; c1 = imag;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void ComplexMultiplyAddSingle<Conj>(void* a, void* b, ref Vector256<float> realC, ref Vector256<float> imagC)
	{
		Vector256<float> a0 = LoadVector256((float*)a);
		Vector256<float> a1 = LoadVector256((float*)a + Vector256<float>.Count);
		Vector256<float> b0 = LoadVector256((float*)b);
		Vector256<float> b1 = LoadVector256((float*)b + Vector256<float>.Count);

		ComplexMultiplyAdd<Conj>(a0, a1, b0, b1, ref realC, ref imagC);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void ComplexMultiplyAddDouble<Conj>(void* a, void* b, ref Vector256<double> realC, ref Vector256<double> imagC)
	{
		Vector256<double> a0 = LoadVector256((double*)a);
		Vector256<double> a1 = LoadVector256((double*)a + Vector256<double>.Count);
		Vector256<double> b0 = LoadVector256((double*)b);
		Vector256<double> b1 = LoadVector256((double*)b + Vector256<double>.Count);

		ComplexMultiplyAdd<Conj>(a0, a1, b0, b1, ref realC, ref imagC);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void ComplexMultiply<Conj>(float* a, float* b, out Vector256<float> left, out Vector256<float> right)
	{
		Vector256<float> a0 = LoadVector256(a);
		Vector256<float> a1 = LoadVector256(a + Vector256<float>.Count);
		Vector256<float> b0 = LoadVector256(b);
		Vector256<float> b1 = LoadVector256(b + Vector256<float>.Count);

		ComplexMultiply<Conj, bool>(a0, a1, b0, b1, out left, out right);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void ComplexMultiply<Conj>(double* a, double* b, out Vector256<double> left, out Vector256<double> right)
	{
		Vector256<double> a0 = LoadVector256(a);
		Vector256<double> a1 = LoadVector256(a + Vector256<double>.Count / 2);
		Vector256<double> b0 = LoadVector256(b);
		Vector256<double> b1 = LoadVector256(b + Vector256<double>.Count / 2);

		ComplexMultiply<Conj, bool>(a0, a1, b0, b1, out left, out right);
	}
	#endregion

	#region complex divide
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void ComplexDivide(Vector256<float> a0, Vector256<float> a1, Vector256<float> b0, Vector256<float> b1, out Vector256<float> c0, out Vector256<float> c1)
	{
		ComplexUnpack(a0, a1, b0, b1, out var realA, out var imagA, out var realB, out var imagB);

		// get the squares of the absolute values of b
		b0 *= b0;
		b1 *= b1;
		Vector256<float> squareAbsB = Avx.HorizontalAdd(b0, b1);

		Vector256<float> AiBi = imagA * imagB;
		Vector256<float> ArBi = realA * imagB;
		Vector256<float> real, imag;
		real = realA * realB;
		real -= AiBi;
		imag = realA * imagB;
		imag += ArBi;
		// divide by the squares of the absolute values of b
		real /= squareAbsB;
		imag /= squareAbsB;

		ComplexPack(real, imag, out c0, out c1);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void ComplexDivide(Vector256<double> a0, Vector256<double> a1, Vector256<double> b0, Vector256<double> b1, out Vector256<double> c0, out Vector256<double> c1)
	{
		ComplexUnpack(a0, a1, b0, b1, out var realA, out var imagA, out var realB, out var imagB);

		// get the squares of the absolute values of b
		b0 *= b0;
		b1 *= b1;
		Vector256<double> squareAbsB = Avx.HorizontalAdd(b0, b1);

		Vector256<double> imagProd = imagA * imagB;
		Vector256<double> ArBi = realA * imagB;
		Vector256<double> real, imag;
		real = realA * realB;
		real -= imagProd;
		imag = realA * imagB;
		imag += ArBi;
		// divide by the squares of the absolute values of b
		real /= squareAbsB;
		imag /= squareAbsB;

		ComplexPack(real, imag, out c0, out c1);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void ComplexDivide(float* a, float* b, out Vector256<float> left, out Vector256<float> right)
	{
		Vector256<float> a0 = LoadVector256(a);
		Vector256<float> a1 = LoadVector256(a + Vector256<float>.Count);
		Vector256<float> b0 = LoadVector256(b);
		Vector256<float> b1 = LoadVector256(b + Vector256<float>.Count);

		ComplexDivide(a0, a1, b0, b1, out left, out right);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void ComplexDivide(double* a, double* b, out Vector256<double> left, out Vector256<double> right)
	{
		Vector256<double> a0 = LoadVector256(a);
		Vector256<double> a1 = LoadVector256(a + Vector256<double>.Count);
		Vector256<double> b0 = LoadVector256(b);
		Vector256<double> b1 = LoadVector256(b + Vector256<double>.Count);

		ComplexDivide(a0, a1, b0, b1, out left, out right);
	}
	#endregion
	#endregion


	#region operations
	/// <inheritdoc/>
	public virtual partial bool AbsoluteValueArgMax<T, TS>(TS x, long strideX, out long index) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>;

	/// <inheritdoc/>
	public virtual partial bool AbsoluteValueArgMin<T, TS>(TS x, long strideX, out long index) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>;

	/// <inheritdoc/>
	public virtual partial bool AbsoluteValueSum<T, TS>(TS x, long strideX, out T sum) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>;

	/// <inheritdoc/>
	public virtual partial bool Norm<T, TS>(TS x, long strideX, out T norm) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>;

	/// <inheritdoc/>
	public virtual partial bool Scale<T, TS>(TS x, long strideX, T scalar) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>;

	/// <inheritdoc/>
	public virtual partial bool Add<T, TS1, TS2>(T α, TS1 x, long strideX, TS2 y, long strideY) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

	/// <inheritdoc/>
	public virtual partial bool Dot<T, TS1, TS2>(bool conjX, TS1 x, long strideX, TS2 y, long strideY, out T dot) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

	/// <inheritdoc/>
	public virtual partial bool GeneralVectorsEqual<T, TS1, TS2>(TS1 x, long strideX, TS2 y, long strideY, out bool equals) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

	/// <inheritdoc/>
	public virtual partial bool GeneralVectorUnary<T, TS1, TS2>(UnaryOperation op, TS1 x, long strideX, TS2 y, long strideY) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

	/// <inheritdoc/>
	public virtual partial bool GeneralVectorBinaryScalar<T, TS1, TS2>(BinaryScalarOperation op, T scalar, TS1 x, long strideX, TS2 y, long strideY) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

	/// <inheritdoc/>
	public virtual partial bool GeneralVectorReduce<T, TS>(ReduceOperation op, TS x, long strideX, out T result) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>;

	/// <inheritdoc/>
	public virtual partial bool GeneralVectorArgReduce<T, TS>(ReduceOperation op, TS x, long strideX, out long index) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>;

	/// <inheritdoc/>
	public virtual partial bool GeneralVectorsBinary<T, TS1, TS2, TS3>(BinaryOperation op, TS1 x, long strideX, TS2 y, long strideY, TS3 z, long strideZ) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>;

	/// <inheritdoc/>
	public virtual partial bool GeneralVectorsScan<T, TS1, TS2>(BinaryOperation op, TS1 x, long strideX, TS2 y, long strideY, bool inclusive) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

	/// <inheritdoc/>
	public virtual partial bool GeneralVectorsCast<TIn, TOut, TSIn, TSOut>(TSIn source, long strideSource, TSOut destination, long strideDestination) where TIn : unmanaged, IBaseNumber<TIn> where TOut : unmanaged, IBaseNumber<TOut> where TSIn : class, IStorage<TIn, TSIn> where TSOut : class, IStorage<TOut, TSOut>;

	/// <inheritdoc/>
	public virtual partial bool Sort<T, TS>(TS array, long stride) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>;

	/// <inheritdoc/>
	public virtual partial bool Sort<T, TOther, TS, TS2>(TS keys, long strideKeys, TS2 values, long strideValues) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS> where TOther : unmanaged, IBaseNumber<TOther> where TS2 : class, IStorage<TOther, TS2>;

	/// <inheritdoc/>
	public virtual partial bool MinMax<T, TS>(TS array, long stride, out (T Min, T Max) minmax) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>;

	/// <inheritdoc/>
	public virtual partial bool IndexOf<T, TS>(TS array, long stride, bool sorted, T value, out long find) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>;

	/// <inheritdoc/>
	public virtual partial bool IndexBound<T, TS>(TS array, long stride, T value, bool lowerBound, out long index) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>;

	/// <inheritdoc/>
	public virtual partial bool IndexGetAllBounds<T, TOut, TS, TSOut>(TS array, TSOut target, T start, T end, bool lowerBound) where T : unmanaged, IBinaryInt<T> where TS : class, IStorage<T, TS> where TOut : unmanaged, IBinaryInt<TOut> where TSOut : class, IStorage<TOut, TSOut>;

	/// <inheritdoc/>
	public virtual partial bool IndexGenerateFromBounds<T, TOut, TS, TSOut>(TS bounds, TSOut target, bool lowerBound, TOut start) where T : unmanaged, IBinaryInt<T> where TOut : unmanaged, IBinaryInt<TOut> where TS : class, IStorage<T, TS> where TSOut : class, IStorage<TOut, TSOut>;

	/// <inheritdoc/>
	public virtual partial bool VectorSetValuesAt<T, TInd, TS, TSInd>(TS x, T value, TSInd positions) where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd> where TS : class, IStorage<T, TS> where TSInd : class, IStorage<TInd, TSInd>;

	/// <inheritdoc/>
	public virtual partial bool VectorSetValuesAt<T, TInd, TS1, TS2, TSInd>(TS1 x, TS2 values, TSInd positions) where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TSInd : class, IStorage<TInd, TSInd>;

	/// <inheritdoc/>
	public virtual partial bool VectorGatherValuesAt<T, TInd, TS1, TS2, TSInd>(TS1 x, TS2 values, TSInd positions) where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TSInd : class, IStorage<TInd, TSInd>;

	/// <inheritdoc/>
	public virtual partial bool VectorSparseToDense<T, TInd, TS1, TS2, TSInd>(ISparseArray<T, TInd, TS1, TSInd> x, TS2 y, long strideY) where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TSInd : class, IStorage<TInd, TSInd>;

	/// <inheritdoc/>
	public virtual partial bool VectorDenseToSparse<T, TInd, TS1, TS2, TSInd>(TS1 x, long strideX, ref SparseArrayWrapper<T, TInd, TS2, TSInd> y, double threshold) where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TSInd : class, IStorage<TInd, TSInd>;
	#endregion


	#region matrix compact operations
	bool IExtendBlasAbstractApi.GeneralMatrixUnary<T, TS1, TS2>(UnaryOperation op, long rows, long cols, TS1 A, long lda, TS2 B, long ldb)
	{
		if (rows == lda && rows == ldb)
			return this.GeneralVectorUnary<T, TS1, TS2>(op, A.MakeReference(0, rows * cols), 1, B, 1);
		return false;
	}

	bool IExtendBlasAbstractApi.GeneralMatrixReduce<T, TS>(ReduceOperation op, long rows, long cols, TS A, long lda, out T result)
	{
		result = default;
		if (rows == lda)
			return this.GeneralVectorReduce(op, A.MakeReference(0, rows * cols), 1, out result);
		return false;
	}

	bool IExtendBlasAbstractApi.GeneralMatrixArgReduce<T, TS>(ReduceOperation op, long rows, long cols, TS A, long lda, out long index)
	{
		index = -1;
		if (rows == lda)
			return this.GeneralVectorArgReduce<T, TS>(op, A.MakeReference(0, rows * cols), 1, out index);
		return false;
	}

	bool IExtendBlasAbstractApi.GeneralMatrixColumnReduce<T, TS1, TS2>(ReduceOperation op, long rows, long cols, TS1 A, long lda, TS2 x, long strideX) => false;

	bool IExtendBlasAbstractApi.GeneralMatrixBinaryScalar<T, TS1, TS2>(BinaryScalarOperation op, long rows, long cols, T scalar, TS1 A, long lda, TS2 B, long ldb)
	{
		if (rows == lda && rows == ldb)
			return this.GeneralVectorBinaryScalar(op, scalar, A.MakeReference(0, rows * cols), 1, B, 1);
		return false;
	}

	bool IExtendBlasAbstractApi.GeneralMatricesBinary<T, TS1, TS2, TS3>(BinaryOperation op, long rows, long cols, TS1 A, long lda, TS2 B, long ldb, TS3 C, long ldc)
	{
		if (rows == lda && rows == ldb && rows == ldc)
			return this.GeneralVectorsBinary<T, TS1, TS2, TS3>(op, A.MakeReference(0, rows * cols), 1, B, 1, C, 1);
		return false;
	}

	bool IExtendBlasAbstractApi.GeneralMatrixColumnScan<T, TS1, TS2>(BinaryOperation op, bool inclusive, long rows, long cols, TS1 A, long lda, TS2 B, long ldb) => false;

	bool IExtendBlasAbstractApi.GeneralMatricesEqual<T, TS1, TS2>(long rows, long cols, TS1 A, long lda, TS2 B, long ldb, out bool equals)
	{
		equals = false;
		if (rows == lda && rows == ldb)
			return this.GeneralVectorsEqual<T, TS1, TS2>(A.MakeReference(0, rows * cols), 1, B, 1, out equals);
		return false;
	}

	bool IExtendBlasAbstractApi.GeneralMatrixCast<TIn, TOut, TSIn, TSOut>(long rows, long cols, TSIn source, long lds, TSOut destination, long ldd)
	{
		if (rows == lds && rows == ldd)
			return this.GeneralVectorsCast<TIn, TOut, TSIn, TSOut>(source.MakeReference(0, rows * cols), 1, destination, 1);
		return false;
	}
	#endregion


	#region eigen
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void Copy2D<T>(T* dst, int ldd, T* src, int lds, int m, int n) where T : unmanaged
	{
		if (m == ldd && m == lds)
		{
			Unsafe.CopyBlockUnaligned(dst, src, (uint)(m * n * sizeof(T)));
		}
		else
		{
			for (int i = 0; i < n; i++)
			{
				Unsafe.CopyBlockUnaligned(dst + i * ldd, src + i * lds, (uint)(m * sizeof(T)));
			}
		}
	}

	/// <inheritdoc/>
	public virtual bool EigenStandardMatrixHermitian<T, TS1, TS2, TS3>(long n, bool upper, TS1 A, long lda, TS2 valOut, TS3? vecOut, long ldvec, bool allowDestroy) where T : unmanaged, IBinaryFloat<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>
	{
		if (!GetPointer(A, n, n, lda, out T* pA, out int nn, out _, out int ld))
			return false;
		if (!GetPointer(vecOut, n, n, ldvec, out T* pV, out _, out _, out int ldv))
			return false;
		if (!GetPointer(valOut, 1, out T* px, out int nx, out _))
			return false;
		if (nx < nn)
			throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(valOut));
		if (pV == null && allowDestroy)
		{
			pV = pA; ldv = ld;
		}
		using var buffer = Buffers.Create<T>((nn + (pV == null ? nn * nn : 0)) * sizeof(T));
		if (pV == null)
		{
			pV = (T*)buffer + nn; ldv = nn;
		}
		if (pA != pV)
		{
			Copy2D(pV, ldv, pA, ld, nn, nn);
		}
		MatrixSolvers.HermitianMatrixToTridiagonal<T>(new(new(pV, ldv * nn), nn, ldv), new(px, nn), new(buffer, nn));
		var info = MatrixSolvers.HermitianTridiagonalEigensolve<T>(new(px, nn), new(buffer, nn), vecOut is null ? default : new(new(pV, ldv * nn), nn, ldv));
		if (info != 0)
			throw new MatrixSolveAlgorithmException(SolveMethodKind.Eigenvalue, info);
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool SchurCheck<T, TS1, TS2, TS3>(long n, TS1 A, long lda, TS2? U, long ldu, TS3 vals, TS3? valsImag, out T* pA, out T* pU, out T* px, out T* pxIm, out int nn, out int ld, out int ldv) where T : unmanaged, IBinaryFloat<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>
	{
		pU = px = pxIm = null;
		ldv = 0;
		if (!GetPointer(A, n, n, lda, out pA, out nn, out _, out ld))
			return false;
		if (!GetPointer(U, n, n, ldu, out pU, out _, out _, out ldv))
			return false;
		if (!GetPointer(vals, 1, out px, out int nx, out _))
			return false;
		if (!T.IsComplexType && valsImag is null)
			throw new ArgumentNullException(nameof(valsImag));
		int nxx = 0;
		if (valsImag is not null && !T.IsComplexType && !GetPointer(valsImag, 1, out pxIm, out nxx, out _))
			return false;
		if (nx < nn)
			throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(vals));
		if (nxx != 0 && nxx < nn)
			throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(valsImag));
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int SchurDecompose<T>(T* pA, T* pU, T* px, T* pxIm, int nn, int ld, int ldv) where T : unmanaged, IBinaryFloat<T>
	{
		if (pU == null)
			ldv = nn;
		int info;
		using var buffer = Buffers.Create<T>((pU == null ? 3 : T.IsComplexType ? 2 : 0) * nn * sizeof(T));
		SpanMatrix<T> matA = new(new(pA, ld * nn), ld);
		SpanMatrix<T> matU = new(new(pU == null ? (T*)buffer : pU, ldv * nn), ldv);
		MatrixSolvers.MatrixToHessenberg(matA, matU);
		if (pU == null)
			matU = default;
		if (T.IsComplexType)
		{
			info = MatrixSolvers.HessenbergSchurFactorize(matA, matU, new(px, nn), default, new((T*)buffer, 2 * nn));
		}
		else
		{
			info = MatrixSolvers.HessenbergSchurFactorize(matA, matU, new(px, nn), new(pxIm, nn));
		}
		return info;
	}

	/// <inheritdoc/>
	public virtual bool SchurDecomposition<T, TS1, TS2, TS3>(long n, TS1 A, long lda, TS2? U, long ldu, TS3 valsOut, TS3? valsOutImag) where T : unmanaged, IBinaryFloat<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>
	{
		if (!SchurCheck(n, A, lda, U, ldu, valsOut, valsOutImag, out T* pA, out T* pU, out T* px, out T* pxIm, out int nn, out int ld, out int ldv))
			return false;
		var info = SchurDecompose(pA, pU, px, pxIm, nn, ld, ldv);
		if (info < 0)
			throw new MatrixSolveAlgorithmException(SolveMethodKind.Schur, info);
		return true;
	}

	/// <inheritdoc/>
	public virtual bool SchurReorder<T, TInd, TS1, TS2, TS3, TSInd>(long n, TS1 A, long lda, TS2? U, long ldu, TS3 vals, TS3? valsImag, TSInd select) where T : unmanaged, IBinaryFloat<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3> where TInd : unmanaged, IBaseNumber<TInd> where TSInd : class, IStorage<TInd, TSInd>
	{
		if (!SchurCheck(n, A, lda, U, ldu, vals, valsImag, out T* pA, out T* pU, out T* px, out T* pxIm, out int nn, out int ld, out int ldvec))
			return false;
		if (!GetPointer(select, 1, out TInd* ps, out int ns, out _))
			return false;
		if (ns != nn)
			throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(select));

		SpanMatrix<T> matA = new(new(pA, ld * nn), ld),
					  matU = new(new(pU, ldvec * nn), ldvec);
		using var buffer = T.IsComplexType ? Buffers.Create<T>(3 * nn * sizeof(T)) : default;
		MatrixSolvers.ReorderSchurForm<T, TInd>(new(ps, nn), matA, matU, new(px, nn), new(pxIm, pxIm == null ? 0 : nn), new((T*)buffer, (T*)buffer == null ? 0 : 3 * nn));
		return true;
	}

	/// <inheritdoc/>
	public virtual bool EigenStandardMatrixGeneral<T, TS1, TS2, TS3, TS4>(long n, TS1 A, long lda, TS2 valsOut, TS2? valsOutImag, TS3? leftVec, long ldvl, TS4? rightVec, long ldvr, bool allowDestroy) where T : unmanaged, IBinaryFloat<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3> where TS4 : class, IStorage<T, TS4>
	{
		if (leftVec is not null)
			return false;
		if (!SchurCheck(n, A, lda, rightVec, ldvr, valsOut, valsOutImag, out T* pA, out T* pU, out T* px, out T* pxIm, out int nn, out int ld, out int ldv))
			return false;

		using var buffer = Buffers.Create<T>(((allowDestroy ? 0 : nn * nn) + (pU == null ? 0 : 9 * nn)) * sizeof(T));
		T* temp = buffer;
		if (!allowDestroy)
		{
			Copy2D(temp, nn, pA, ld, nn, nn);
			pA = temp; ld = nn;
			temp += nn * nn;
		}
		var info = SchurDecompose(pA, pU, px, pxIm, nn, ld, ldv);
		if (info < 0)
			throw new MatrixSolveAlgorithmException(SolveMethodKind.Schur, info);
		if (pU == null)
			return true;
		SpanMatrix<T> matA = new(new(pA, ld * nn), ld),
					  matU = new(new(pU, ldv * nn), ldv);
		MatrixSolvers.SchurFormEigensolve(matA, matU, matU, new(temp, 9 * nn), new(px, nn), new(pxIm, pxIm == null ? 0 : nn));
		return true;
	}

	/// <inheritdoc/>
	public virtual bool LinearSolveGeneral<T, TS1, TS2>(long n, long nrhs, TS1 A, long lda, TS2 B, long ldb, bool allowDestroy) where T : unmanaged, IBinaryFloat<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
	{
		if (!GetPointer(A, n, n, lda, out T* pA, out int nn, out _, out int ld))
			return false;
		if (!GetPointer(B, n, nrhs, ldb, out T* pB, out _, out int nr, out int ldr))
			return false;

		using var buffer = Buffers.Create<T>(((allowDestroy ? 0 : nn * nn) + nn) * sizeof(T));
		T* temp = buffer;
		if (!allowDestroy)
		{
			Copy2D(temp, nn, pA, ld, nn, nn);
			pA = temp; ld = nn; temp += nn * nn;
		}
		SpanMatrix<T> matA = new(new(pA, ld * nn), ld),
					  matB = new(new(pB, ldr * nr), ldr);
		MatrixSolvers.QrFactorize(matA, new(temp, nn));
		MatrixSolvers.QrQtMultiply(matA, matB);
		MatrixSolvers.QrLinearSolve(matA, new(temp, nn), matB);
		return true;
	}

	/// <inheritdoc/>
	public virtual bool QRDecomposition<T, TS1, TS2>(bool full, long m, long n, TS1 A, long lda, TS2? Q, long ldq) where T : unmanaged, IBinaryFloat<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
	{
		if (!GetPointer(A, m, n, lda, out T* pA, out int mm, out int nn, out int ldaa))
			return false;
		if (!GetPointer(Q, m, full ? m : Math.Min(m, n), ldq, out T* pQ, out _, out _, out int ldqq))
			return false;

		using var buffer = Buffers.Create<T>(nn * sizeof(T));
		SpanMatrix<T> matA = new(new(pA, ldaa * nn), ldaa),
					  matQ = new(new(pQ, ldqq * nn), ldqq);
		MatrixSolvers.QrFactorize(matA, new(buffer, nn));
		if (pQ != null)
		{
			Copy2D(pQ, ldqq, pA, ldaa, mm, Math.Min(mm, nn));
			MatrixSolvers.QrGenerateQ(0, full ? mm : Math.Min(mm, nn), matA, new(buffer, nn));
		}
		return true;
	}

	/// <inheritdoc/>
	public virtual bool LeastSquareSolve<T, TS1, TS2>(long m, long n, long nrhs, TS1 A, long lda, TS2 B, long ldb, bool allowDestroy) where T : unmanaged, IBinaryFloat<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
	{
		if (!GetPointer(A, m, n, lda, out T* pA, out int mm, out int nn, out int ld))
			return false;
		if (!GetPointer(B, n, nrhs, ldb, out T* pB, out _, out int nr, out int ldr))
			return false;

		using var buffer = Buffers.Create<T>(((allowDestroy ? 0 : mm * nn) + mm) * sizeof(T));
		T* temp = buffer;
		if (!allowDestroy)
		{
			Copy2D(temp, mm, pA, ld, mm, nn);
			pA = temp; ld = nn; temp += mm * nn;
		}
		SpanMatrix<T> matA = new(new(pA, ld * nn), ld),
					  matB = new(new(pB, ldr * nr), ldr);
		MatrixSolvers.QrFactorize(matA, new(temp, nn));
		MatrixSolvers.QrQtMultiply(matA, matB);
		MatrixSolvers.QrLinearSolve(matA[..nn, ..], new(temp, nn), matB);
		return true;
	}

#pragma warning disable CS8769
	bool ILapackAbstractApi.EigenGeneralMatrixHermitian<T, TS1, TS2, TS3, TS4>(GeneralEigenType type, long n, bool upper, TS1 A, long lda, TS1 B, long ldb, TS2 valOut, TS3 vecOut, long ldvec, TS4 LUOut, long ldLU, bool allowDestroy) => false;

	bool ILapackAbstractApi.EigenGeneralMatrixGeneral<T, TS1, TS2, TS3, TS4>(GeneralEigenType type, long n, TS1 A, long lda, TS1 B, long ldb, TS2 valOut, TS2 valImagOut, TS2 valDenomOut, TS3 leftVec, long ldvl, TS4 rightVec, long ldvr, bool allowDestroy) => false;

	bool ILapackAbstractApi.SingularValues<T, TS1, TS2, TS3, TS4>(bool fullU, bool fullV, long m, long n, TS1 A, long lda, TS2 U, long ldu, TS3 Vct, long ldvct, TS4 S, bool allowDestroy) => false;
#pragma warning restore CS8769
	#endregion


	#region not supported
	bool IBlasAbstractApi.GeneralMatrixMultiplyVector<T, TSM, TSV1, TSV2>(MatrixOperation op, long m, long n, T α, TSM A, long lda, TSV1 x, long strideX, T β, TSV2 y, long strideY) => false;

	bool IBlasAbstractApi.SymmetricMatrixMultiplyVector<T, TSM, TSV1, TSV2>(bool fillUpper, bool hermA, long n, T α, TSM A, long lda, TSV1 x, long strideX, T β, TSV2 y, long strideY) => false;

	bool IBlasAbstractApi.TriangularMatrixMultiplyVector<T, TSM, TSV1, TSV2>(bool fillUpper, bool unitDiag, MatrixOperation op, long m, long n, TSM A, long lda, T α, TSV1 x, long strideX, T β, TSV2 y, long strideY) => false;

	bool IBlasAbstractApi.GeneralRankOneUpdate<T, TSM, TSV1, TSV2>(bool conjY, long m, long n, T α, TSV1 x, long strideX, TSV2 y, long strideY, T β, TSM A, long lda) => false;

	bool IBlasAbstractApi.SymmetricRankOneUpdate<T, TSM, TSV>(bool fillUpper, bool conjX, long n, T α, TSV x, long strideX, T β, TSM A, long lda) => false;

	bool IBlasAbstractApi.SymmetricRankTwoUpdate<T, TSM, TSV1, TSV2>(bool fillUpper, bool conjugate, long n, T α, TSV1 x, long strideX, TSV2 y, long strideY, T β, TSM A, long lda) => false;

	bool IBlasAbstractApi.GeneralMatricesMultiply<T, TS1, TS2, TS3>(MatrixOperation opA, MatrixOperation opB, long m, long n, long k, T α, TS1 A, long lda, TS2 B, long ldb, T β, TS3 C, long ldc) => false;

	bool IBlasAbstractApi.SymmetricMatrixMultiplyGeneral<T, TS1, TS2, TS3>(bool fillUpper, bool leftA, bool hermA, MatrixOperation opA, MatrixOperation opB, long m, long n, T α, TS1 A, long lda, TS2 B, long ldb, T β, TS3 C, long ldc) => false;

	bool IBlasAbstractApi.TriangularMatrixSolve<T, TS1, TS2>(bool leftA, bool fillUpper, bool unitDiag, MatrixOperation op, long m, long n, T α, TS1 A, long lda, TS2 B, long ldb) => false;

	bool IBlasAbstractApi.TriangularMatrixMultiplyGeneral<T, TS1, TS2, TS3>(bool leftA, bool fillUpper, bool unitDiag, MatrixOperation opA, MatrixOperation opB, long m, long n, long k, T α, TS1 A, long lda, TS2 B, long ldb, T β, TS3 C, long ldc) => false;

	bool IBlasAbstractApi.SymmetricRankKUpdate<T, TS1, TS2>(bool fillUpper, MatrixOperation op, bool conjA, long n, long k, T α, TS1 A, long lda, T β, TS2 C, long ldc) => false;

	bool IBlasAbstractApi.SymmetricRankTwoKUpdate<T, TS1, TS2, TS3>(bool fillUpper, MatrixOperation op, bool conjugate, long n, long k, T α, TS1 A, long lda, TS2 B, long ldb, T β, TS3 C, long ldc) => false;

#pragma warning disable CS8769
	bool IExtendBlasAbstractApi.GeneralMatricesAdd<T, TS1, TS2, TS3>(MatrixOperation opA, MatrixOperation opB, long m, long n, T α, TS1 A, long lda, T β, TS2 B, long ldb, TS3 C, long ldc) => false;
#pragma warning restore CS8769

	bool IExtendBlasAbstractApi.DiagonalMatrixMultiplyGeneral<T, TS1, TS2, TS3>(bool leftA, MatrixOperation opA, bool conjX, long m, long n, T α, TS1 A, long lda, TS2 x, long strideX, T β, TS3 C, long ldc) => false;

	bool IExtendBlasAbstractApi.MatrixKronecker<T, TS1, TS2, TS3>(long ma, long na, long mb, long nb, T α, TS1 A, long lda, TS2 B, long ldb, T β, TS3 C, long ldc) => false;

	bool IConversionAbstractApi.SparseVectorToMatrix<T, TInd1, TInd2, TS1, TS2, TSInd1, TSInd2>(ISparseArray<T, TInd1, TS1, TSInd1> vector, ref SparseArrayWrapper<T, TInd2, TS2, TSInd2> target) => false;

	bool IConversionAbstractApi.SparseMatrixToVector<T, TInd1, TInd2, TS1, TS2, TSInd1, TSInd2>(ISparseArray<T, TInd1, TS1, TSInd1> matrix, ref SparseArrayWrapper<T, TInd2, TS2, TSInd2> target) => false;

	bool IConversionAbstractApi.SparseMatrixGetSlice<T, TInd1, TInd2, TS1, TS2, TSInd1, TSInd2>(ISparseArray<T, TInd1, TS1, TSInd1> source, MatrixSliceWrapper slice, ref SparseArrayWrapper<T, TInd2, TS2, TSInd2> sub) => false;

	bool IConversionAbstractApi.SparseMatrixSetSlice<T, TInd1, TInd2, TS1, TS2, TSInd1, TSInd2>(ISparseArray<T, TInd2, TS2, TSInd2> matrix, MatrixSliceWrapper slice, ISparseArray<T, TInd1, TS1, TSInd1> sub) => false;

	bool IConversionAbstractApi.MatrixSparseToDense<T, TInd, TS1, TS2, TSInd>(ISparseArray<T, TInd, TS1, TSInd> source, TS2 destination, long ld) => false;

	bool IConversionAbstractApi.MatrixDenseToSparse<T, TInd, TS1, TS2, TSInd>(TS1 source, long ld, ref SparseArrayWrapper<T, TInd, TS2, TSInd> target, double threshold) => false;

	bool IConversionAbstractApi.MatrixSparsePrune<T, TInd1, TInd2, TS1, TS2, TSInd1, TSInd2>(ISparseArray<T, TInd1, TS1, TSInd1> source, double threshold, ref SparseArrayWrapper<T, TInd2, TS2, TSInd2> target) => false;

	bool IConversionAbstractApi.MatrixSparseFormatConvert<T, TInd1, TInd2, TS1, TS2, TSInd1, TSInd2>(ISparseArray<T, TInd1, TS1, TSInd1> source, ref SparseArrayWrapper<T, TInd2, TS2, TSInd2> target) => false;

	bool IConversionAbstractApi.MatrixSparseReshape<T, TInd1, TInd2, TS1, TS2, TSInd1, TSInd2>(ISparseArray<T, TInd1, TS1, TSInd1> source, ref SparseArrayWrapper<T, TInd2, TS2, TSInd2> target) => false;
	#endregion
}
