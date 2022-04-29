using System.Buffers;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

using Althea.Array;
using Althea.Backend.Storage;
using Althea.LinearAlgebra;
using Althea.LinearAlgebra.Dense;
using Althea.LinearAlgebra.Sparse;
using Althea.NativeTypes;


namespace Althea.Backend.CSharp.LinearAlgebra
{
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
		private static bool GetPointer<T, TS>(TS s, long stride, out T* pointer, out int length, out int inc) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>
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
		private static bool GetPointer<T, TS>(TS? s, long m, long n, long ld, out T* pointer, out int mm, out int nn, out int ldd) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>
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
		private static Vector<T> LoadVector<T>(T* r) where T : unmanaged, INumber<T> => Unsafe.ReadUnaligned<Vector<T>>(r);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Vector<T> LoadVector<T>(void* r) where T : unmanaged, INumber<T> => Unsafe.ReadUnaligned<Vector<T>>(r);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Vector256<T> LoadVector256<T>(T* r) where T : unmanaged, INumber<T> => Unsafe.ReadUnaligned<Vector256<T>>(r);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Vector256<T> LoadVector256<T>(void* r) where T : unmanaged, INumber<T> => Unsafe.ReadUnaligned<Vector256<T>>(r);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void LoadVector<T>(T* r, out Vector<T> v1, out Vector<T> v2) where T : unmanaged, INumber<T>
		{
			v1 = Unsafe.ReadUnaligned<Vector<T>>(r);
			v2 = Unsafe.ReadUnaligned<Vector<T>>(r + Vector<T>.Count);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void LoadVector<T>(T* r, out Vector<T> v1, out Vector<T> v2, out Vector<T> v3, out Vector<T> v4) where T : unmanaged, INumber<T>
		{
			v1 = Unsafe.ReadUnaligned<Vector<T>>(r);
			v2 = Unsafe.ReadUnaligned<Vector<T>>(r + Vector<T>.Count);
			v3 = Unsafe.ReadUnaligned<Vector<T>>(r + Vector<T>.Count * 2);
			v4 = Unsafe.ReadUnaligned<Vector<T>>(r + Vector<T>.Count * 3);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void LoadVector<T>(T* r, out Vector<T> v1, out Vector<T> v2, out Vector<T> v3, out Vector<T> v4, out Vector<T> v5, out Vector<T> v6, out Vector<T> v7, out Vector<T> v8) where T : unmanaged, INumber<T>
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
		private static void StoreVector<T>(Vector<T> v, void* r) where T : unmanaged, INumber<T> => Unsafe.WriteUnaligned(r, v);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void StoreVector256<T>(Vector256<T> v, void* r) where T : unmanaged, INumber<T> => Unsafe.WriteUnaligned(r, v);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void StoreVector<T>(Vector<T> v1, Vector<T> v2, T* r) where T : unmanaged, INumber<T>
		{
			Unsafe.WriteUnaligned(r, v1); Unsafe.WriteUnaligned(r + Vector<T>.Count, v2);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void StoreVector<T>(Vector<T> v1, Vector<T> v2, Vector<T> v3, Vector<T> v4, T* r) where T : unmanaged, INumber<T>
		{
			Unsafe.WriteUnaligned(r + Vector<T>.Count * 0, v1);
			Unsafe.WriteUnaligned(r + Vector<T>.Count * 1, v2);
			Unsafe.WriteUnaligned(r + Vector<T>.Count * 2, v3);
			Unsafe.WriteUnaligned(r + Vector<T>.Count * 3, v4);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void StoreVector<T>(Vector<T> v1, Vector<T> v2, Vector<T> v3, Vector<T> v4, Vector<T> v5, Vector<T> v6, Vector<T> v7, Vector<T> v8, T* r) where T : unmanaged, INumber<T>
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
			current1 = Avx.Multiply(current1, current1);
			current2 = Avx.Multiply(current2, current2);
			Vector256<float> squares = Avx.HorizontalAdd(current1, current2);
			return squares;
			// abs of {0, 1, 4, 5, 2, 3, 6, 7}-th complex
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Vector256<double> ComplexSquareAbsNoOrder(Vector256<double> current1, Vector256<double> current2)
		{
			current1 = Avx.Multiply(current1, current1);
			current2 = Avx.Multiply(current2, current2);
			Vector256<double> squares = Avx.HorizontalAdd(current1, current2);
			return squares;
			// abs of {0, 2, 1, 3}-th complex
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Vector256<float> ComplexSquareAbsNoOrder(Complex<float>* p)
		{
			Vector256<float> current1 = LoadVector256<float>(p);
			Vector256<float> current2 = LoadVector256<float>(p + Vector256<float>.Count / 2);
			return ComplexSquareAbsNoOrder(current1, current2);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Vector256<double> ComplexSquareAbsNoOrder(Complex<double>* p)
		{
			Vector256<double> current1 = LoadVector256<double>(p);
			Vector256<double> current2 = LoadVector256<double>(p + Vector256<double>.Count / 2);
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
		private static Vector256<float> ComplexSquareAbsOrder(Complex<float>* p)
		{
			Vector256<float> current1 = LoadVector256<float>(p);
			Vector256<float> current2 = LoadVector256<float>(p + Vector256<float>.Count / 2);
			return ComplexSquareAbsOrder(current1, current2);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Vector256<double> ComplexSquareAbsOrder(Complex<double>* p)
		{
			Vector256<double> current1 = LoadVector256<double>(p);
			Vector256<double> current2 = LoadVector256<double>(p + Vector256<double>.Count / 2);
			return ComplexSquareAbsOrder(current1, current2);
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
			if (Fma.IsSupported)
			{
				if (!conj)
				{
					// get the output real parts
					realC = Fma.MultiplySubtract(imagA, imagB, realC);
					realC = Fma.MultiplySubtract(realA, realB, realC);
					// get the output imaginary parts
					imagC = Fma.MultiplyAdd(imagA, realB, imagC);
					imagC = Fma.MultiplyAdd(realA, imagB, imagC);
				}
				else
				{
					realC = Fma.MultiplyAdd(imagA, imagB, realC);
					realC = Fma.MultiplyAdd(realA, realB, realC);
					imagC = Fma.MultiplySubtract(imagA, realB, imagC);
					imagC = Fma.MultiplySubtract(realA, imagB, imagC);
				}
			}
			else
			{
				var ArBr = Avx.Multiply(realA, realB);
				var AiBi = Avx.Multiply(imagA, imagB);
				var ArBi = Avx.Multiply(realA, imagB);
				var AiBr = Avx.Multiply(imagA, realB);
				realC = Avx.Add(realC, ArBr);
				imagC = Avx.Add(imagC, ArBi);
				if (!conj)
				{
					realC = Avx.Subtract(realC, AiBi);
					imagC = Avx.Add(imagC, AiBr);
				}
				else
				{
					realC = Avx.Add(realC, AiBi);
					imagC = Avx.Subtract(imagC, AiBr);
				}
			}
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void UnpackComplexMultiplyAdd<Conj>(Vector256<double> realA, Vector256<double> imagA, Vector256<double> realB, Vector256<double> imagB, ref Vector256<double> realC, ref Vector256<double> imagC)
		{
			bool conj = typeof(Conj) == typeof(bool);
			// multiply
			// the branch shall be eliminated by JIT
			if (Fma.IsSupported)
			{
				if (!conj)
				{
					// get the output real parts
					realC = Fma.MultiplySubtract(imagA, imagB, realC);
					realC = Fma.MultiplySubtract(realA, realB, realC);
					// get the output imaginary parts
					imagC = Fma.MultiplyAdd(imagA, realB, imagC);
					imagC = Fma.MultiplyAdd(realA, imagB, imagC);
				}
				else
				{
					realC = Fma.MultiplyAdd(imagA, imagB, realC);
					realC = Fma.MultiplyAdd(realA, realB, realC);
					imagC = Fma.MultiplySubtract(imagA, realB, imagC);
					imagC = Fma.MultiplySubtract(realA, imagB, imagC);
				}
			}
			else
			{
				var ArBr = Avx.Multiply(realA, realB);
				var AiBi = Avx.Multiply(imagA, imagB);
				var ArBi = Avx.Multiply(realA, imagB);
				var AiBr = Avx.Multiply(imagA, realB);
				realC = Avx.Add(realC, ArBr);
				imagC = Avx.Add(imagC, ArBi);
				if (!conj)
				{
					realC = Avx.Subtract(realC, AiBi);
					imagC = Avx.Add(imagC, AiBr);
				}
				else
				{
					realC = Avx.Add(realC, AiBi);
					imagC = Avx.Subtract(imagC, AiBr);
				}
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

			Vector256<float> AiBi = Avx.Multiply(imagA, imagB);
			Vector256<float> AiBr = Avx.Multiply(imagA, realB);
			Vector256<float> real, imag;
			if (Fma.IsSupported)
			{
				if (!conj)
				{
					real = Fma.MultiplySubtract(realA, realB, AiBi);
					imag = Fma.MultiplyAdd(realA, imagB, AiBr);
				}
				else
				{
					real = Fma.MultiplyAdd(realA, realB, AiBi);
					imag = Fma.MultiplySubtract(realA, imagB, AiBr);
				}
			}
			else
			{
				var ArBr = Avx.Multiply(realA, realB);
				var ArBi = Avx.Multiply(realA, imagB);
				if (!conj)
				{
					real = Avx.Subtract(ArBr, AiBi);
					imag = Avx.Add(ArBi, AiBr);
				}
				else
				{
					real = Avx.Add(ArBr, AiBi);
					imag = Avx.Subtract(ArBi, AiBr);
				}
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

			Vector256<double> AiBi = Avx.Multiply(imagA, imagB);
			Vector256<double> AiBr = Avx.Multiply(imagA, realB);
			Vector256<double> real, imag;
			if (Fma.IsSupported)
			{
				if (!conj)
				{
					real = Fma.MultiplySubtract(realA, realB, AiBi);
					imag = Fma.MultiplyAdd(realA, imagB, AiBr);
				}
				else
				{
					real = Fma.MultiplyAdd(realA, realB, AiBi);
					imag = Fma.MultiplySubtract(realA, imagB, AiBr);
				}
			}
			else
			{
				var ArBr = Avx.Multiply(realA, realB);
				var ArBi = Avx.Multiply(realA, imagB);
				if (!conj)
				{
					real = Avx.Subtract(ArBr, AiBi);
					imag = Avx.Add(ArBi, AiBr);
				}
				else
				{
					real = Avx.Add(ArBr, AiBi);
					imag = Avx.Subtract(ArBi, AiBr);
				}
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
		private static void ComplexMultiplyAdd<Conj>(Complex<float>* a, Complex<float>* b, ref Vector256<float> realC, ref Vector256<float> imagC)
		{
			Vector256<float> a0 = LoadVector256<float>(a);
			Vector256<float> a1 = LoadVector256<float>(a + Vector256<float>.Count / 2);
			Vector256<float> b0 = LoadVector256<float>(b);
			Vector256<float> b1 = LoadVector256<float>(b + Vector256<float>.Count / 2);

			ComplexMultiplyAdd<Conj>(a0, a1, b0, b1, ref realC, ref imagC);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void ComplexMultiplyAdd<Conj>(Complex<double>* a, Complex<double>* b, ref Vector256<double> realC, ref Vector256<double> imagC)
		{
			Vector256<double> a0 = LoadVector256<double>(a);
			Vector256<double> a1 = LoadVector256<double>(a + Vector256<double>.Count / 2);
			Vector256<double> b0 = LoadVector256<double>(b);
			Vector256<double> b1 = LoadVector256<double>(b + Vector256<double>.Count / 2);

			ComplexMultiplyAdd<Conj>(a0, a1, b0, b1, ref realC, ref imagC);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void ComplexMultiply<Conj>(Complex<float>* a, Complex<float>* b, out Vector256<float> left, out Vector256<float> right)
		{
			Vector256<float> a0 = LoadVector256<float>(a);
			Vector256<float> a1 = LoadVector256<float>(a + Vector256<float>.Count / 2);
			Vector256<float> b0 = LoadVector256<float>(b);
			Vector256<float> b1 = LoadVector256<float>(b + Vector256<float>.Count / 2);

			ComplexMultiply<Conj, bool>(a0, a1, b0, b1, out left, out right);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void ComplexMultiply<Conj>(Complex<double>* a, Complex<double>* b, out Vector256<double> left, out Vector256<double> right)
		{
			Vector256<double> a0 = LoadVector256<double>(a);
			Vector256<double> a1 = LoadVector256<double>(a + Vector256<double>.Count / 2);
			Vector256<double> b0 = LoadVector256<double>(b);
			Vector256<double> b1 = LoadVector256<double>(b + Vector256<double>.Count / 2);

			ComplexMultiply<Conj, bool>(a0, a1, b0, b1, out left, out right);
		}
		#endregion

		#region complex divide
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void ComplexDivide(Vector256<float> a0, Vector256<float> a1, Vector256<float> b0, Vector256<float> b1, out Vector256<float> c0, out Vector256<float> c1)
		{
			ComplexUnpack(a0, a1, b0, b1, out var realA, out var imagA, out var realB, out var imagB);

			// get the squares of the absolute values of b
			b0 = Avx.Multiply(b0, b0);
			b1 = Avx.Multiply(b1, b1);
			Vector256<float> squareAbsB = Avx.HorizontalAdd(b0, b1);

			Vector256<float> AiBi = Avx.Multiply(imagA, imagB);
			Vector256<float> ArBi = Avx.Multiply(realA, imagB);
			Vector256<float> real, imag;
			if (Fma.IsSupported)
			{
				real = Fma.MultiplySubtract(realA, realB, AiBi);
				imag = Fma.MultiplyAdd(realA, imagB, ArBi);
			}
			else
			{
				real = Avx.Multiply(realA, realB);
				real = Avx.Subtract(real, AiBi);
				imag = Avx.Multiply(realA, imagB);
				imag = Avx.Add(imag, ArBi);
			}
			// divide by the squares of the absolute values of b
			real = Avx.Divide(real, squareAbsB);
			imag = Avx.Divide(imag, squareAbsB);

			ComplexPack(real, imag, out c0, out c1);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void ComplexDivide(Vector256<double> a0, Vector256<double> a1, Vector256<double> b0, Vector256<double> b1, out Vector256<double> c0, out Vector256<double> c1)
		{
			ComplexUnpack(a0, a1, b0, b1, out var realA, out var imagA, out var realB, out var imagB);

			// get the squares of the absolute values of b
			b0 = Avx.Multiply(b0, b0);
			b1 = Avx.Multiply(b1, b1);
			Vector256<double> squareAbsB = Avx.HorizontalAdd(b0, b1);

			Vector256<double> imagProd = Avx.Multiply(imagA, imagB);
			Vector256<double> ArBi = Avx.Multiply(realA, imagB);
			Vector256<double> real, imag;
			if (Fma.IsSupported)
			{
				real = Fma.MultiplySubtract(realA, realB, imagProd);
				imag = Fma.MultiplyAdd(realA, imagB, ArBi);
			}
			else
			{
				real = Avx.Multiply(realA, realB);
				real = Avx.Subtract(real, imagProd);
				imag = Avx.Multiply(realA, imagB);
				imag = Avx.Add(imag, ArBi);
			}
			// divide by the squares of the absolute values of b
			real = Avx.Divide(real, squareAbsB);
			imag = Avx.Divide(imag, squareAbsB);

			ComplexPack(real, imag, out c0, out c1);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void ComplexDivide(Complex<float>* a, Complex<float>* b, out Vector256<float> left, out Vector256<float> right)
		{
			Vector256<float> a0 = LoadVector256<float>(a);
			Vector256<float> a1 = LoadVector256<float>(a + Vector256<float>.Count / 2);
			Vector256<float> b0 = LoadVector256<float>(b);
			Vector256<float> b1 = LoadVector256<float>(b + Vector256<float>.Count / 2);

			ComplexDivide(a0, a1, b0, b1, out left, out right);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void ComplexDivide(Complex<double>* a, Complex<double>* b, out Vector256<double> left, out Vector256<double> right)
		{
			Vector256<double> a0 = LoadVector256<double>(a);
			Vector256<double> a1 = LoadVector256<double>(a + Vector256<double>.Count / 2);
			Vector256<double> b0 = LoadVector256<double>(b);
			Vector256<double> b1 = LoadVector256<double>(b + Vector256<double>.Count / 2);

			ComplexDivide(a0, a1, b0, b1, out left, out right);
		}
		#endregion
		#endregion


		#region operations
		/// <inheritdoc/>
		public virtual partial bool AbsoluteValueArgMax<T, TS>(TS x, long strideX, out long index) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <inheritdoc/>
		public virtual partial bool AbsoluteValueArgMin<T, TS>(TS x, long strideX, out long index) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <inheritdoc/>
		public virtual partial bool AbsoluteValueSum<T, TS>(TS x, long strideX, out T sum) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <inheritdoc/>
		public virtual partial bool Norm<T, TS>(TS x, long strideX, out T norm) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <inheritdoc/>
		public virtual partial bool Scale<T, TS>(TS x, long strideX, T scalar) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <inheritdoc/>
		public virtual partial bool Add<T, TS1, TS2>(T α, TS1 x, long strideX, TS2 y, long strideY) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

		/// <inheritdoc/>
		public virtual partial bool Dot<T, TS1, TS2>(bool conjX, TS1 x, long strideX, TS2 y, long strideY, out T dot) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

		/// <inheritdoc/>
		public virtual partial bool GeneralVectorsEqual<T, TS1, TS2>(TS1 x, long strideX, TS2 y, long strideY, out bool equals) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

		/// <inheritdoc/>
		public virtual partial bool GeneralVectorUnary<T, TS1, TS2>(UnaryOperation op, TS1 x, long strideX, TS2 y, long strideY) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

		/// <inheritdoc/>
		public virtual partial bool GeneralVectorBinaryScalar<T, TS1, TS2>(BinaryScalarOperation op, T scalar, TS1 x, long strideX, TS2 y, long strideY) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

		/// <inheritdoc/>
		public virtual partial bool GeneralVectorReduce<T, TS>(ReduceOperation op, TS x, long strideX, out T result) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <inheritdoc/>
		public virtual partial bool GeneralVectorArgReduce<T, TS>(ReduceOperation op, TS x, long strideX, out long index) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <inheritdoc/>
		public virtual partial bool GeneralVectorsBinary<T, TS1, TS2, TS3>(BinaryOperation op, TS1 x, long strideX, TS2 y, long strideY, TS3 z, long strideZ) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>;

		/// <inheritdoc/>
		public virtual partial bool GeneralVectorsScan<T, TS1, TS2>(BinaryOperation op, TS1 x, long strideX, TS2 y, long strideY, bool inclusive) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

		/// <inheritdoc/>
		public virtual partial bool GeneralVectorsCast<TIn, TOut, TSIn, TSOut>(TSIn source, long strideSource, TSOut destination, long strideDestination) where TIn : unmanaged, INumber<TIn> where TOut : unmanaged, INumber<TOut> where TSIn : class, IStorage<TIn, TSIn> where TSOut : class, IStorage<TOut, TSOut>;

		/// <inheritdoc/>
		public virtual partial bool Sort<T, TS>(TS array, long stride) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <inheritdoc/>
		public virtual partial bool Sort<T, TOther, TS, TS2>(TS keys, long strideKeys, TS2 values, long strideValues) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS> where TOther : unmanaged, INumber<TOther> where TS2 : class, IStorage<TOther, TS2>;

		/// <inheritdoc/>
		public virtual partial bool MinMax<T, TS>(TS array, long stride, out (T Min, T Max) minmax) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <inheritdoc/>
		public virtual partial bool IndexOf<T, TS>(TS array, long stride, bool sorted, T value, out long find) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <inheritdoc/>
		public virtual partial bool IndexBound<T, TS>(TS array, long stride, T value, bool lowerBound, out long index) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <inheritdoc/>
		public virtual partial bool IndexGetAllBounds<T, TOut, TS, TSOut>(TS array, TSOut target, T start, T end, bool lowerBound) where T : unmanaged, IBinaryInteger<T> where TS : class, IStorage<T, TS> where TOut : unmanaged, IBinaryInteger<TOut> where TSOut : class, IStorage<TOut, TSOut>;

		/// <inheritdoc/>
		public virtual partial bool IndexGenerateFromBounds<T, TOut, TS, TSOut>(TS bounds, TSOut target, bool lowerBound, TOut start) where T : unmanaged, IBinaryInteger<T> where TOut : unmanaged, IBinaryInteger<TOut> where TS : class, IStorage<T, TS> where TSOut : class, IStorage<TOut, TSOut>;

		/// <inheritdoc/>
		public virtual partial bool VectorSetValuesAt<T, TInd, TS, TSInd>(TS x, T value, TSInd positions) where T : unmanaged, INumber<T> where TInd : unmanaged, IBinaryInteger<TInd> where TS : class, IStorage<T, TS> where TSInd : class, IStorage<TInd, TSInd>;

		/// <inheritdoc/>
		public virtual partial bool VectorSetValuesAt<T, TInd, TS1, TS2, TSInd>(TS1 x, TS2 values, TSInd positions) where T : unmanaged, INumber<T> where TInd : unmanaged, IBinaryInteger<TInd> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TSInd : class, IStorage<TInd, TSInd>;

		/// <inheritdoc/>
		public virtual partial bool VectorGatherValuesAt<T, TInd, TS1, TS2, TSInd>(TS1 x, TS2 values, TSInd positions) where T : unmanaged, INumber<T> where TInd : unmanaged, IBinaryInteger<TInd> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TSInd : class, IStorage<TInd, TSInd>;

		/// <inheritdoc/>
		public virtual partial bool VectorSparseToDense<T, TInd, TS1, TS2, TSInd>(ISparseArray<T, TInd, TS1, TSInd> x, TS2 y, long strideY) where T : unmanaged, INumber<T> where TInd : unmanaged, IBinaryInteger<TInd> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TSInd : class, IStorage<TInd, TSInd>;

		/// <inheritdoc/>
		public virtual partial bool VectorDenseToSparse<T, TInd, TS1, TS2, TSInd>(TS1 x, long strideX, ref SparseArrayWrapper<T, TInd, TS2, TSInd> y, double threshold) where T : unmanaged, INumber<T> where TInd : unmanaged, IBinaryInteger<TInd> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TSInd : class, IStorage<TInd, TSInd>;
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
		/// <inheritdoc/>
		public virtual bool EigenStandardMatrixHermitian<T, TS1, TS2, TS3>(long n, bool upper, TS1 A, long lda, TS2 valOut, TS3? vecOut, long ldvec) where T : unmanaged, IFloatingPoint<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>
		{
			if (NumberType<T>.IsComplex)
				return false;
			if (!GetPointer(A, n, n, lda, out T* pA, out int nn, out _, out int ld))
				return false;
			if (!GetPointer(vecOut, n, n, ldvec, out T* pV, out _, out _, out int ldv) || pV == null)
				return false;
			if (!GetPointer(valOut, 1, out T* px, out int nx, out _))
				return false;
			if (nx < nn)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(valOut));
			if (n == lda && n == ldvec)
			{
				Unsafe.CopyBlockUnaligned(pV, pA, (uint)(nn * nn * sizeof(T)));
			}
			else
			{
				for (int i = 0; i < nn; i++)
				{
					Unsafe.CopyBlockUnaligned(pV + i * ldv, pA + i * ld, (uint)(nn * sizeof(T)));
				}
			}
			var buffer = ArrayPool<byte>.Shared.Rent(nn * sizeof(T));
			try
			{
				fixed (byte* offDiag = buffer)
				{
					MatrixSolvers.SymmetricMatrixToTridiagonal(nn, pV, ldv, px, (T*)offDiag);
					if (!MatrixSolvers.SymmetricTridiagonalMatrixEigensolve(nn, px, (T*)offDiag, pV, ldv))
						throw new MatrixSolveAlgorithmException(SolveMethodKind.QR, 1);
				}
				return true;
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(buffer);
			}
		}

#pragma warning disable CS8769
		bool ILapackAbstractApi.EigenGeneralMatrixHermitian<T, TS1, TS2, TS3, TS4>(GeneralEigenType type, long n, bool upper, TS1 A, long lda, TS1 B, long ldb, TS2 valOut, TS3 vecOut, long ldvec, TS4 LUOut, long ldLU) => false;

		bool ILapackAbstractApi.EigenStandardMatrixGeneral<T, TS1, TS2, TS3, TS4>(long n, TS1 A, long lda, TS2 valOut, TS2 valImagOut, TS3 leftVec, long ldvl, TS4 rightVec, long ldvr) => false;

		bool ILapackAbstractApi.EigenGeneralMatrixGeneral<T, TS1, TS2, TS3, TS4>(GeneralEigenType type, long n, TS1 A, long lda, TS1 B, long ldb, TS2 valOut, TS2 valImagOut, TS2 valDenomOut, TS3 leftVec, long ldvl, TS4 rightVec, long ldvr) => false;

		bool ILapackAbstractApi.SingularValues<T, TS1, TS2, TS3, TS4>(bool fullU, bool fullV, long m, long n, TS1 A, long lda, TS2 U, long ldu, TS3 Vct, long ldvct, TS4 S) => false;

		bool ILapackAbstractApi.SchurDecomposition<T, TS1, TS2, TS3>(long n, TS1 A, long lda, TS2 U, long ldu, TS3 valOut, TS3 valImagOut) => false;

		bool ILapackAbstractApi.SchurReorder<T, TInd, TS1, TS2, TS3, TSInd>(long n, TS1 A, long lda, TS2 U, long ldu, TS3 vals, TS3 valsImag, TSInd select) => false;

		bool ILapackAbstractApi.LinearSolveGeneral<T, TS1, TS2>(long n, long nrhs, TS1 A, long lda, TS2 B, long ldb) => false;

		bool ILapackAbstractApi.QRDecomposition<T, TS1, TS2>(bool full, long m, long n, TS1 A, long lda, TS2 Q, long ldq) => false;

		bool ILapackAbstractApi.LeastSquareSolve<T, TS1, TS2>(long m, long n, long nrhs, TS1 A, long lda, TS2 B, long ldb) => false;
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
}
