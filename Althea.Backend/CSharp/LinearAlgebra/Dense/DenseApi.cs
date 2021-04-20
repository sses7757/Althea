using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

using Althea.Backend.Storage;
using Althea.LinearAlgebra;
using Althea.LinearAlgebra.Dense;
using Althea.NativeTypes;

namespace Althea.Backend.CSharp.LinearAlgebra
{
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释
	/// <summary>
	/// The C# back-end of <see cref="AbstractApi"/> that utilizes <see cref="System.Runtime.Intrinsics"/> and <see cref="Vector{T}"/>.<br/>
	/// Only supports storages on CPU memory of primitive and pre-defined types and single-threaded vector operations.
	/// </summary>
	/// <remarks>Vector operations of real types and <see cref="ComplexSingle"/> and <see cref="ComplexDouble"/> (and <see cref="Complex{T}"/> of <see cref="float"/> and <see cref="double"/>) are accelerated if possible while other types utilize scalar operations.</remarks>
	public partial class DenseApi : AbstractApi
	{
		#region basic
		// AVX optimization is not used unless horizontal add and/or reordering is necessary (for complex types).
		// Since during the test (on Windows 10, .NET 5.0, i7-8700K),
		// System.Numerics.Vector<T> utilizes the AVX instruction directly by the JIT.
		// Therefore the System.Numerics.Vector<T> and AVX assembly codes are almost the same,
		// and their performance difference is less than negligible for large vectors.
		// Both of them outperforms the scalar implementation for around 3 times (shall be 4 if there were no loop-related operation).

		// Also, ARM SIMD is never used since it does not provide horizontal add or reordering instruction
		// and therefore can be utilized by System.Numerics.Vector<T> automatically.

		public DenseApi()
		{
			// do nothing
		}

		protected override void Dispose(bool disposeManaged)
		{
			// do nothing
		}
		#endregion


		#region support
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool Supported(CombinationOfLocations location) => location.Count == 1 && location[0].Type == LocationType.CpuRam;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedMatrixBinary(CombinationOfLocations location1, CombinationOfLocations location2) => false;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedMatrixTrinary(CombinationOfLocations location1, CombinationOfLocations location2, CombinationOfLocations location3) => false;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedMatrixUnary(CombinationOfLocations location) => false;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedNormalTypeComplexType(Span<CombinationOfLocations> normals, Span<CombinationOfLocations> complexes) => false;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedNormalTypeRealType(Span<CombinationOfLocations> normals, Span<CombinationOfLocations> reals) => false;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedVectorBinary(CombinationOfLocations location1, CombinationOfLocations location2) => Supported(location1) && Supported(location2);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedVectorBinaryMatrixUnary(CombinationOfLocations vector1, CombinationOfLocations vector2, CombinationOfLocations matrix) => false;
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedVectorUnary(CombinationOfLocations location) => Supported(location);
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedVectorUnaryMatrixBinary(CombinationOfLocations vector, CombinationOfLocations matrix1, CombinationOfLocations matrix2) => false;
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedVectorUnaryMatrixUnary(CombinationOfLocations vector, CombinationOfLocations matrix) => false;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedMatrixUnaryIndexUnary(CombinationOfLocations matrix, CombinationOfLocations index, DataType indexType) => false;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedMatrixBinaryIndexUnary(CombinationOfLocations matrix1, CombinationOfLocations matrix2, CombinationOfLocations index, DataType indexType) => false;
		#endregion


		#region helpers
		#region load and simple op
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe bool GetPointer<T>(Storage<T> s, out T* pointer, out int length) where T : unmanaged
		{
			pointer = default; length = 0;
			if (s is null || !s.IsValid())
				throw new ArgumentNullException(nameof(s));
			if (s.Count != 1 || s[0].Pointer is not IMemoryPointer m)
				return false; // not support
			if (!Const<T>.IsPreDefined)
				return false; // not support
			pointer = (T*)m.Pointer.ToPointer();
			if (pointer == default)
				return false; // not support
			long l = m.LengthInBytes / Const<T>.SizeT;
			if (l > int.MaxValue)
				return false; // not support
			length = (int)l;
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe Vector<T> LoadVector<T>(T* r) where T : unmanaged
		{
			return Unsafe.ReadUnaligned<Vector<T>>(r);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe Vector<T> LoadVector<T>(void* r) where T : unmanaged
		{
			return Unsafe.ReadUnaligned<Vector<T>>(r);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe Vector256<T> LoadVector256<T>(T* r) where T : unmanaged
		{
			return Unsafe.ReadUnaligned<Vector256<T>>(r);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe Vector256<T> LoadVector256<T>(void* r) where T : unmanaged
		{
			return Unsafe.ReadUnaligned<Vector256<T>>(r);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe void LoadVector<T>(T* r, out Vector<T> v1, out Vector<T> v2) where T : unmanaged
		{
			v1 = Unsafe.ReadUnaligned<Vector<T>>(r);
			v2 = Unsafe.ReadUnaligned<Vector<T>>(r + Vector<T>.Count);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe void LoadVector<T>(T* r, out Vector<T> v1, out Vector<T> v2, out Vector<T> v3, out Vector<T> v4) where T : unmanaged
		{
			v1 = Unsafe.ReadUnaligned<Vector<T>>(r);
			v2 = Unsafe.ReadUnaligned<Vector<T>>(r + Vector<T>.Count);
			v3 = Unsafe.ReadUnaligned<Vector<T>>(r + Vector<T>.Count * 2);
			v4 = Unsafe.ReadUnaligned<Vector<T>>(r + Vector<T>.Count * 3);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe void LoadVector<T>(T* r, out Vector<T> v1, out Vector<T> v2, out Vector<T> v3, out Vector<T> v4, out Vector<T> v5, out Vector<T> v6, out Vector<T> v7, out Vector<T> v8) where T : unmanaged
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
		private static unsafe void StoreVector<T>(Vector<T> v, void* r) where T : unmanaged
		{
			Unsafe.WriteUnaligned(r, v);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe void StoreVector256<T>(Vector256<T> v, void* r) where T : unmanaged
		{
			Unsafe.WriteUnaligned(r, v);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe void StoreVector<T>(Vector<T> v1, Vector<T> v2, T* r) where T : unmanaged
		{
			Unsafe.WriteUnaligned(r, v1); Unsafe.WriteUnaligned(r + Vector<T>.Count, v2);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe void StoreVector<T>(Vector<T> v1, Vector<T> v2, Vector<T> v3, Vector<T> v4, T* r) where T : unmanaged
		{
			Unsafe.WriteUnaligned(r, v1);
			Unsafe.WriteUnaligned(r + Vector<T>.Count * 1, v2);
			Unsafe.WriteUnaligned(r + Vector<T>.Count * 2, v3);
			Unsafe.WriteUnaligned(r + Vector<T>.Count * 3, v4);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe void StoreVector<T>(Vector<T> v1, Vector<T> v2, Vector<T> v3, Vector<T> v4, Vector<T> v5, Vector<T> v6, Vector<T> v7, Vector<T> v8, T* r) where T : unmanaged
		{
			Unsafe.WriteUnaligned(r, v1);
			Unsafe.WriteUnaligned(r + Vector<T>.Count * 1, v2);
			Unsafe.WriteUnaligned(r + Vector<T>.Count * 2, v3);
			Unsafe.WriteUnaligned(r + Vector<T>.Count * 3, v4);
			Unsafe.WriteUnaligned(r + Vector<T>.Count * 4, v1);
			Unsafe.WriteUnaligned(r + Vector<T>.Count * 5, v2);
			Unsafe.WriteUnaligned(r + Vector<T>.Count * 6, v3);
			Unsafe.WriteUnaligned(r + Vector<T>.Count * 7, v4);
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
		private static unsafe Vector256<float> ComplexSquareAbsNoOrder(ComplexSingle* p)
		{
			Vector256<float> current1 = LoadVector256<float>(p);
			Vector256<float> current2 = LoadVector256<float>(p + Vector256<float>.Count / 2);
			return ComplexSquareAbsNoOrder(current1, current2);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe Vector256<double> ComplexSquareAbsNoOrder(ComplexDouble* p)
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
		private static unsafe Vector256<float> ComplexSquareAbsOrder(ComplexSingle* p)
		{
			Vector256<float> current1 = LoadVector256<float>(p);
			Vector256<float> current2 = LoadVector256<float>(p + Vector256<float>.Count / 2);
			return ComplexSquareAbsOrder(current1, current2);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe Vector256<double> ComplexSquareAbsOrder(ComplexDouble* p)
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
		private static void ComplexMultiply<Conj>(Vector256<float> a0, Vector256<float> a1, Vector256<float> b0, Vector256<float> b1, out Vector256<float> c0, out Vector256<float> c1)
		{
			bool conj = typeof(Conj) == typeof(bool);
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
			ComplexPack(real, imag, out c0, out c1);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void ComplexMultiply<Conj>(Vector256<double> a0, Vector256<double> a1, Vector256<double> b0, Vector256<double> b1, out Vector256<double> c0, out Vector256<double> c1)
		{
			bool conj = typeof(Conj) == typeof(bool);
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
			ComplexPack(real, imag, out c0, out c1);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe void ComplexMultiplyAdd<Conj>(ComplexSingle* a, ComplexSingle* b, ref Vector256<float> realC, ref Vector256<float> imagC)
		{
			Vector256<float> a0 = LoadVector256<float>(a);
			Vector256<float> a1 = LoadVector256<float>(a + Vector256<float>.Count / 2);
			Vector256<float> b0 = LoadVector256<float>(b);
			Vector256<float> b1 = LoadVector256<float>(b + Vector256<float>.Count / 2);

			ComplexMultiplyAdd<Conj>(a0, a1, b0, b1, ref realC, ref imagC);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe void ComplexMultiplyAdd<Conj>(ComplexDouble* a, ComplexDouble* b, ref Vector256<double> realC, ref Vector256<double> imagC)
		{
			Vector256<double> a0 = LoadVector256<double>(a);
			Vector256<double> a1 = LoadVector256<double>(a + Vector256<double>.Count / 2);
			Vector256<double> b0 = LoadVector256<double>(b);
			Vector256<double> b1 = LoadVector256<double>(b + Vector256<double>.Count / 2);

			ComplexMultiplyAdd<Conj>(a0, a1, b0, b1, ref realC, ref imagC);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe void ComplexMultiply<Conj>(ComplexSingle* a, ComplexSingle* b, out Vector256<float> left, out Vector256<float> right)
		{
			Vector256<float> a0 = LoadVector256<float>(a);
			Vector256<float> a1 = LoadVector256<float>(a + Vector256<float>.Count / 2);
			Vector256<float> b0 = LoadVector256<float>(b);
			Vector256<float> b1 = LoadVector256<float>(b + Vector256<float>.Count / 2);

			ComplexMultiply<Conj>(a0, a1, b0, b1, out left, out right);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe void ComplexMultiply<Conj>(ComplexDouble* a, ComplexDouble* b, out Vector256<double> left, out Vector256<double> right)
		{
			Vector256<double> a0 = LoadVector256<double>(a);
			Vector256<double> a1 = LoadVector256<double>(a + Vector256<double>.Count / 2);
			Vector256<double> b0 = LoadVector256<double>(b);
			Vector256<double> b1 = LoadVector256<double>(b + Vector256<double>.Count / 2);

			ComplexMultiply<Conj>(a0, a1, b0, b1, out left, out right);
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
		private static unsafe void ComplexDivide(ComplexSingle* a, ComplexSingle* b, out Vector256<float> left, out Vector256<float> right)
		{
			Vector256<float> a0 = LoadVector256<float>(a);
			Vector256<float> a1 = LoadVector256<float>(a + Vector256<float>.Count / 2);
			Vector256<float> b0 = LoadVector256<float>(b);
			Vector256<float> b1 = LoadVector256<float>(b + Vector256<float>.Count / 2);

			ComplexDivide(a0, a1, b0, b1, out left, out right);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe void ComplexDivide(ComplexDouble* a, ComplexDouble* b, out Vector256<double> left, out Vector256<double> right)
		{
			Vector256<double> a0 = LoadVector256<double>(a);
			Vector256<double> a1 = LoadVector256<double>(a + Vector256<double>.Count / 2);
			Vector256<double> b0 = LoadVector256<double>(b);
			Vector256<double> b1 = LoadVector256<double>(b + Vector256<double>.Count / 2);

			ComplexDivide(a0, a1, b0, b1, out left, out right);
		}
		#endregion
		#endregion


		#region dynamic invoke
		protected override bool InvokeExtraMethod(ExtraMethodInfo methodInfo, out object? outParam, object[] inputParams)
		{
			outParam = null;
			if (methodInfo.Name == nameof(PointWiseModulo) && inputParams.Length == 2)
			{
				if (inputParams[0] is IStorage s && s.GetType() is { IsGenericType: true } ts)
				{
					var t = ts.GenericTypeArguments[0];
					if (methodInfo[1].Equals(t.TypeHandle) && t.IsPrimitive)
					{
						// invoke method
						return Type.GetTypeCode(t) switch
						{
							TypeCode.Char => PointWiseModulo(s.As<char>(), (char)inputParams[1]),
							TypeCode.SByte => PointWiseModulo(s.As<sbyte>(), (sbyte)inputParams[1]),
							TypeCode.Byte => PointWiseModulo(s.As<byte>(), (byte)inputParams[1]),
							TypeCode.Int16 => PointWiseModulo(s.As<short>(), (short)inputParams[1]),
							TypeCode.UInt16 => PointWiseModulo(s.As<ushort>(), (ushort)inputParams[1]),
							TypeCode.Int32 => PointWiseModulo(s.As<int>(), (int)inputParams[1]),
							TypeCode.UInt32 => PointWiseModulo(s.As<uint>(), (uint)inputParams[1]),
							TypeCode.Int64 => PointWiseModulo(s.As<long>(), (long)inputParams[1]),
							TypeCode.UInt64 => PointWiseModulo(s.As<ulong>(), (ulong)inputParams[1]),
							_ => false,
						};
					}
				}
			}
			else if ((methodInfo.Name == nameof(ArgMax) || methodInfo.Name == nameof(ArgMin)) && inputParams.Length == 1)
			{
				if (inputParams[0] is IStorage s && s.GetType() is { IsGenericType: true } ts)
				{
					var t = ts.GenericTypeArguments[0];
					bool max = methodInfo.Name == nameof(ArgMax);
					long result = -1;
					// invoke method
					bool success = Type.GetTypeCode(t) switch
					{
						TypeCode.Char => max ? ArgMax(s.As<char>(), out result) : ArgMin(s.As<char>(), out result),
						TypeCode.SByte => max ? ArgMax(s.As<sbyte>(), out result) : ArgMin(s.As<sbyte>(), out result),
						TypeCode.Byte => max ? ArgMax(s.As<byte>(), out result) : ArgMin(s.As<byte>(), out result),
						TypeCode.Int16 => max ? ArgMax(s.As<short>(), out result) : ArgMin(s.As<short>(), out result),
						TypeCode.UInt16 => max ? ArgMax(s.As<ushort>(), out result) : ArgMin(s.As<ushort>(), out result),
						TypeCode.Int32 => max ? ArgMax(s.As<int>(), out result) : ArgMin(s.As<int>(), out result),
						TypeCode.UInt32 => max ? ArgMax(s.As<uint>(), out result) : ArgMin(s.As<uint>(), out result),
						TypeCode.Int64 => max ? ArgMax(s.As<long>(), out result) : ArgMin(s.As<long>(), out result),
						TypeCode.UInt64 => max ? ArgMax(s.As<ulong>(), out result) : ArgMin(s.As<ulong>(), out result),
						TypeCode.Single => max ? ArgMax(s.As<float>(), out result) : ArgMin(s.As<float>(), out result),
						TypeCode.Double => max ? ArgMax(s.As<double>(), out result) : ArgMin(s.As<double>(), out result),
						_ => false,
					};
					if (success)
						outParam = result;
					return success;
				}
			}
			else if (methodInfo.Name == nameof(AbsoluteValueProduct) && inputParams.Length == 1)
			{
				if (inputParams[0] is IStorage s && s.GetType() is { IsGenericType: true } ts)
				{
					var t = ts.GenericTypeArguments[0];
					double result = 1;
					// invoke method
					bool success = Type.GetTypeCode(t) switch
					{
						TypeCode.Char => AbsoluteValueProduct(s.As<char>(), out result),
						TypeCode.SByte => AbsoluteValueProduct(s.As<sbyte>(), out result),
						TypeCode.Byte => AbsoluteValueProduct(s.As<byte>(), out result),
						TypeCode.Int16 => AbsoluteValueProduct(s.As<short>(), out result),
						TypeCode.UInt16 => AbsoluteValueProduct(s.As<ushort>(), out result),
						TypeCode.Int32 => AbsoluteValueProduct(s.As<int>(), out result),
						TypeCode.UInt32 => AbsoluteValueProduct(s.As<uint>(), out result),
						TypeCode.Int64 => AbsoluteValueProduct(s.As<long>(), out result),
						TypeCode.UInt64 => AbsoluteValueProduct(s.As<ulong>(), out result),
						TypeCode.Single => AbsoluteValueProduct(s.As<float>(), out result),
						TypeCode.Double => AbsoluteValueProduct(s.As<double>(), out result),
						_ => false,
					};
					if (success || !t.IsGenericType)
					{
						outParam = result;
						return success;
					}
					// else
					success = Type.GetTypeCode(t.GenericTypeArguments[0]) switch
					{
						TypeCode.Char => AbsoluteValueProduct(s.As<Complex<char>>(), out result),
						TypeCode.SByte => AbsoluteValueProduct(s.As<Complex<sbyte>>(), out result),
						TypeCode.Byte => AbsoluteValueProduct(s.As<Complex<byte>>(), out result),
						TypeCode.Int16 => AbsoluteValueProduct(s.As<Complex<short>>(), out result),
						TypeCode.UInt16 => AbsoluteValueProduct(s.As<Complex<ushort>>(), out result),
						TypeCode.Int32 => AbsoluteValueProduct(s.As<Complex<int>>(), out result),
						TypeCode.UInt32 => AbsoluteValueProduct(s.As<Complex<uint>>(), out result),
						TypeCode.Int64 => AbsoluteValueProduct(s.As<Complex<long>>(), out result),
						TypeCode.UInt64 => AbsoluteValueProduct(s.As<Complex<ulong>>(), out result),
						TypeCode.Single => AbsoluteValueProduct(s.As<Complex<float>>(), out result),
						TypeCode.Double => AbsoluteValueProduct(s.As<Complex<double>>(), out result),
						_ => false,
					};
					outParam = result;
					return success;
				}
			}
			return false;
		}
		#endregion


		#region vector API
		protected override bool AbsoluteValueArgMax_<T>(Storage<T> x, int strideX, out long index)
		{
			index = -1;
			if (strideX != 1)
				return false;
			return AbsoluteValueArgMax(x, out index);
		}

		protected override bool AbsoluteValueArgMin_<T>(Storage<T> x, int strideX, out long index)
		{
			index = -1;
			if (strideX != 1)
				return false;
			return AbsoluteValueArgMin(x, out index);
		}

		protected override bool AbsoluteValueSum_<T>(Storage<T> x, int strideX, out double sum)
		{
			sum = 0;
			if (strideX != 1)
				return false;
			return AbsoluteValueSum(x, out sum);
		}

		protected override bool AggregateProduct_<T>(Storage<T> x, int stride, out T product)
		{
			product = default;
			if (stride != 1)
				return false;
			return AggregateProduct(x, out product);
		}

		protected override bool AggregateSum_<T>(Storage<T> x, int stride, out T sum)
		{
			sum = default;
			if (stride != 1)
				return false;
			return AggregateSum(x, out sum);
		}

		protected override bool Dot_<T>(bool conjX, Storage<T> x, int strideX, Storage<T> y, int strideY, out T dot)
		{
			dot = default;
			if (strideX != 1 || strideY != 1)
				return false;
			return Dot(conjX, x, y, out dot);
		}

		protected override bool Norm_<T>(Storage<T> x, int strideX, out double norm)
		{
			norm = default;
			if (strideX != 1)
				return false;
			return Norm(x, out norm);
		}

		protected override bool PartialProduct_<T>(Storage<T> x, int strideX, Storage<T> y, int strideY, bool inclusive)
		{
			if (strideX != 1 || strideY != 1)
				return false;
			return PartialProduct(x, y, inclusive);
		}

		protected override bool PartialSum_<T>(Storage<T> x, int strideX, Storage<T> y, int strideY, bool inclusive)
		{
			if (strideX != 1 || strideY != 1)
				return false;
			return PartialSum(x, y, inclusive);
		}

		protected override bool PointWiseAddScalar_<T>(Storage<T> x, int stride, T scalr)
		{
			if (stride != 1)
				return false;
			return PointWiseAddScalar(x, scalr);
		}

		protected override bool PointWiseCast_<T, TOut>(Storage<T> source, int incSrc, Storage<TOut> destination, int incDst)
		{
			if (incSrc != 1 || incDst != 1)
				return false;
			return PointWiseCast(source, destination);
		}

		protected override bool PointWiseConjugate_<T>(Storage<T> x, int stride)
		{
			if (stride != 1)
				return false;
			return PointWiseConjugate(x);
		}

		protected override bool PointWiseDivide_<T>(Storage<T> x, int strideX, Storage<T> y, int strideY)
		{
			if (strideX != 1 || strideY != 1)
				return false;
			return PointWiseDivide(x, y);
		}

		protected override bool PointWiseEquals_<T>(Storage<T> x, int strideX, Storage<T> y, int strideY, out bool equals)
		{
			equals = false;
			if (strideX != 1 || strideY != 1)
				return false;
			return PointWiseEquals(x, y, out equals);
		}

		protected override bool PointWiseMultiply_<T>(Storage<T> x, int strideX, Storage<T> y, int strideY)
		{
			if (strideX != 1 || strideY != 1)
				return false;
			return PointWiseMultiply(x, y);
		}

		protected override bool PointWisePower_<T>(Storage<T> x, int stride, double p)
		{
			if (stride != 1)
				return false;
			return PointWisePower(x, p);
		}

		protected override bool PointWisePower_<T>(Storage<T> x, int stride, T p)
		{
			if (stride != 1)
				return false;
			return PointWisePower(x, p);
		}

		protected override bool Scale_<T>(Storage<T> x, int strideX, T scalar)
		{
			if (strideX != 1)
				return false;
			return Scale(x, scalar);
		}

		protected override bool TruncateArray_<T>(Storage<T> x, int stride, double threshold)
		{
			if (stride != 1)
				return false;
			return TruncateArray(x, threshold);
		}

		protected override bool VectorGeneralAdd_<T>(T α, Storage<T> x, int strideX, Storage<T> y, int strideY)
		{
			if (strideX != 1 || strideY != 1)
				return false;
			return VectorGeneralAdd(α, x, y);
		}
		#endregion


		#region not supported matrix related
		protected override bool SchurDecomposition_<T>(SolveVectorMode jobu, long n, Storage<T> A, long lda, Storage<T>? U, long ldu, out long actualNumber, Storage<ComplexDouble>? orderVal = null) { actualNumber = 0; return false; }
		protected override bool DiagonalMatrixMultiplyGeneral_<T>(bool leftA, long m, long n, T α, Storage<T> A, long lda, Storage<T> x, int strideX, T β, Storage<T> C, long ldc) => false;
		protected override bool EigenGeneralMatrixGeneral_<T, TComplex>(GeneralEigenType type, SolveVectorMode mode, long n, Storage<TComplex> valOut, Storage<TComplex>? leftVec, long ldvl, Storage<TComplex>? rightVec, long ldvr, Storage<T> A, long lda, Storage<T> B, long ldb) => false;
		protected override bool EigenGeneralMatrixHermitian_<T, TReal>(GeneralEigenType type, SolveVectorMode mode, long n, Storage<TReal> valOut, Storage<T> A, long lda, Storage<T> B, long ldb) => false;
		protected override bool EigenSpecialMatrixGeneral_<T, TComplex>(SolveVectorMode mode, long n, Storage<TComplex> valOut, Storage<TComplex>? leftVec, long ldvl, Storage<TComplex>? rightVec, long ldvr, Storage<T> A, long lda) => false;
		protected override bool EigenSpecialMatrixHermitian_<T, TReal>(SolveVectorMode mode, long n, Storage<TReal> valOut, Storage<T> A, long lda) => false;
		protected override bool GeneralMatricesAdd_<T>(MatrixOperation opA, MatrixOperation opB, long m, long n, T α, Storage<T>? A, long lda, T β, Storage<T>? B, long ldb, Storage<T> C, long ldc) => false;
		protected override bool GeneralMatricesMultiply_<T>(MatrixOperation opA, MatrixOperation opB, long m, long n, long k, T α, Storage<T> A, long lda, Storage<T> B, long ldb, T β, Storage<T> C, long ldc) => false;
		protected override bool GeneralMatrixMultiplyVector_<T>(MatrixOperation op, long m, long n, T α, Storage<T> A, long lda, Storage<T> x, int strideX, T β, Storage<T> y, int strideY) => false;
		protected override bool GenralRankOneUpdate_<T>(bool conjY, long m, long n, T α, Storage<T> x, int strideX, Storage<T> y, int strideY, T β, Storage<T> A, long lda) => false;
		protected override bool MatrixCopyUpperLowerParts_<T>(bool storedUpper, bool hermitian, long n, Storage<T> A, long lda) => false;
		protected override bool MatrixKronecker_<T>(long ma, long na, long mb, long nb, T α, Storage<T> A, long lda, Storage<T> B, long ldb, T β, Storage<T> C, long ldc) => false;
		protected override bool RankKUpdate_<T>(bool fillUpper, MatrixOperation op, bool conjA, long n, long k, T α, Storage<T> A, long lda, T β, Storage<T> C, long ldc) => false;
		protected override bool SingularValues_<T, TReal>(SVDStore storeU, SVDStore storeV, long m, long n, Storage<T> A, long lda, Storage<TReal> S, Storage<T>? U, long ldu, Storage<T>? Vct, long ldvct) => false;
		protected override bool SymmHermMatrixMultiplyGeneral_<T>(bool fillUpper, bool leftA, bool hermA, long m, long n, T α, Storage<T> A, long lda, Storage<T> B, long ldb, T β, Storage<T> C, long ldc) => false;
		protected override bool SymmHermMatrixMultiplyVector_<T>(bool fillUpper, bool hermA, long n, T α, Storage<T> A, long lda, Storage<T> x, int strideX, T β, Storage<T> y, int strideY) => false;
		protected override bool SymmHermRankOneUpdate_<T>(bool fillUpper, bool conjX, long n, T α, Storage<T> x, int strideX, T β, Storage<T> A, long lda) => false;
		protected override bool TriangularMatrixSolve_<T>(bool leftA, bool fillUpper, bool unitDiag, MatrixOperation op, long m, long n, T α, Storage<T> A, long lda, Storage<T> B, long ldb) => false;
		protected override bool SymmHermRankTwoUpdate_<T>(bool fillUpper, bool conjugate, long n, T α, Storage<T> x, int strideX, Storage<T> y, int strideY, T β, Storage<T> A, long lda) => false;
		protected override bool TriangularMatrixMultiplyVector_<T>(bool fillUpper, bool unitDiag, MatrixOperation op, long n, Storage<T> A, long lda, Storage<T> x, int strideX) => false;
		protected override bool RankTwoKUpdate_<T>(bool fillUpper, MatrixOperation op, bool conjugate, long n, long k, T α, Storage<T> A, long lda, Storage<T> B, long ldb, T β, Storage<T> C, long ldc) => false;
		protected override bool RankKUpdateVariant_<T>(bool fillUpper, MatrixOperation op, bool conjB, long n, long k, T α, Storage<T> A, long lda, Storage<T> B, long ldb, T β, Storage<T> C, long ldc) => false;
		protected override bool LUDecomposition_<T, TInd>(long n, Storage<T> A, long lda, Storage<TInd> pivot) => false;
		protected override bool LinearSolveByLU_<T, TInd>(MatrixOperation op, long n, long nrhs, Storage<T> A, long lda, Storage<TInd> pivot, Storage<T> B, long ldb) => false;
		protected override bool ImplicitQR_<T>(long m, long n, Storage<T> A, long lda, Storage<T> τ) => false;
		protected override bool ImplicitQRFormQ_<T>(long m, long n, long k, Storage<T> Q, long ldq, Storage<T> τ) => false;
		protected override bool ImplicitQRMultiplyQ_<T>(bool leftQ, MatrixOperation op, long m, long n, long k, Storage<T> A, long lda, Storage<T> τ, Storage<T> C, long ldc) => false;
		protected override bool AllQRSupport<T>(long m, long n, long nrhs, Storage<T> A, long lda, Storage<T> B, long ldb, Storage<T>? work) => false;
		protected override bool MatrixClearUpperLowerPart_<T>(bool clearLower, long n, Storage<T> A, long lda) => false;
		protected override bool TriangularMatrixMultiply_<T>(bool leftA, bool fillUpper, bool unitDiag, MatrixOperation op, long m, long n, T α, Storage<T> A, long lda, Storage<T> B, long ldb, Storage<T> C, long ldc) => false;
		#endregion
	}
#pragma warning restore CS1591 // 缺少对公共可见类型或成员的 XML 注释
}
