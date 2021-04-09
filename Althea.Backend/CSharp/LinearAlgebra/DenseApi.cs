using System;
using System.Numerics;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

using Althea.Backend.Storage;
using Althea.LinearAlgebra;
using Althea.LinearAlgebra.Dense;
using Althea.Linq;
using Althea.NativeTypes;
using Althea.Helpers;


namespace Althea.Backend.CSharp.LinearAlgebra
{
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释
	/// <summary>
	/// The C# back-end of <see cref="AbstractApi"/> that utilizes <see cref="System.Runtime.Intrinsics"/> and <see cref="Vector{T}"/>.<br/>
	/// Only supports storages on CPU memory of primitive and pre-defined types and single-threaded vector operations.
	/// </summary>
	public class DenseApi : AbstractApi
	{
		#region basic
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
		#endregion

		#region helpers
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe bool GetSpan<T>(Storage<T> s, out T* pointer, out int length) where T : unmanaged
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
		private static unsafe Vector256<float> ComplexSquareAbs(ComplexSingle* p)
		{
			Vector256<float> current1 = LoadVector256<float>(p);
			Vector256<float> current2 = LoadVector256<float>(p + Vector256<float>.Count / 2);
			current1 = Avx.Multiply(current1, current1);
			current2 = Avx.Multiply(current2, current2);
			Vector256<float> squares = Avx.HorizontalAdd(current1, current2);
			return squares;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe Vector256<double> ComplexSquareAbs(ComplexDouble* p)
		{
			Vector256<double> current1 = LoadVector256<double>(p);
			Vector256<double> current2 = LoadVector256<double>(p + Vector256<double>.Count / 2);
			current1 = Avx.Multiply(current1, current1);
			current2 = Avx.Multiply(current2, current2);
			Vector256<double> squares = Avx.HorizontalAdd(current1, current2);
			return squares;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe Vector256<float> ComplexAbs(ComplexSingle* p)
		{
			Vector256<float> squares = ComplexSquareAbs(p);
			return Avx.Sqrt(squares);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe Vector256<double> ComplexAbs(ComplexDouble* p)
		{
			Vector256<double> squares = ComplexSquareAbs(p);
			return Avx.Sqrt(squares);
		}


		// The main bottleneck of complex multiply and division is 6 shuffles (unpacks)
		//	which can be eliminated by using separate complex storing approach
		// It can be done by using a special version of MixedStroage<Complex<T>> of two identical storage locations,
		//	and implement special APIs for that type of storage.

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void ComplexMultiply(Vector256<float> a0, Vector256<float> a1, Vector256<float> b0, Vector256<float> b1, out Vector256<float> left, out Vector256<float> right)
		{
			// {a[0].r, a[1].r, a[2].r, a[3].r, ...}
			Vector256<float> realA = Avx.UnpackLow(a0, a1);
			// {a[0].i, a[1].i, a[2].i, a[3].i, ...}
			Vector256<float> imagA = Avx.UnpackHigh(a0, a1);
			// {b[0].r, b[1].r, b[2].r, b[3].r, ...}
			Vector256<float> realB = Avx.UnpackLow(b0, b1);
			// {b[0].i, b[1].i, b[2].i, b[3].i, ...}
			Vector256<float> imagB = Avx.UnpackHigh(b0, b1);

			// multiply
			Vector256<float> imagProd = Avx.Multiply(imagA, imagB);
			Vector256<float> BrAi = Avx.Multiply(realB, imagA);
			Vector256<float> real, imag;
			// the branch shall be eliminated by JIT
			if (Fma.IsSupported)
			{
				// get the output real parts
				real = Fma.MultiplySubtract(realA, realB, imagProd);
				// get the output imaginary parts
				imag = Fma.MultiplyAdd(realA, imagB, BrAi);
			}
			else
			{
				// get the output real parts
				real = Avx.Multiply(realA, realB); // real prod
				real = Avx.Subtract(real, imagProd);
				// get the output imaginary parts
				imag = Avx.Multiply(realA, imagB); // ArBi
				imag = Avx.Add(imag, BrAi);
			}
			left = Avx.UnpackLow(real, imag);
			right = Avx.UnpackHigh(real, imag);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void ComplexMultiply(Vector256<double> a0, Vector256<double> a1, Vector256<double> b0, Vector256<double> b1, out Vector256<double> left, out Vector256<double> right)
		{
			// {a[0].r, a[1].r, a[2].r, a[3].r, ...}
			Vector256<double> realA = Avx.UnpackLow(a0, a1);
			// {a[0].i, a[1].i, a[2].i, a[3].i, ...}
			Vector256<double> imagA = Avx.UnpackHigh(a0, a1);
			// {b[0].r, b[1].r, b[2].r, b[3].r, ...}
			Vector256<double> realB = Avx.UnpackLow(b0, b1);
			// {b[0].i, b[1].i, b[2].i, b[3].i, ...}
			Vector256<double> imagB = Avx.UnpackHigh(b0, b1);

			// multiply
			Vector256<double> imagProd = Avx.Multiply(imagA, imagB);
			Vector256<double> BrAi = Avx.Multiply(realB, imagA);
			Vector256<double> real, imag;
			// the branch shall be eliminated by JIT
			if (Fma.IsSupported)
			{
				// get the output real parts
				real = Fma.MultiplySubtract(realA, realB, imagProd);
				// get the output imaginary parts
				imag = Fma.MultiplyAdd(realA, imagB, BrAi);
			}
			else
			{
				// get the output real parts
				real = Avx.Multiply(realA, realB); // real prod
				real = Avx.Subtract(real, imagProd);
				// get the output imaginary parts
				imag = Avx.Multiply(realA, imagB); // ArBi
				imag = Avx.Add(imag, BrAi);
			}
			left = Avx.UnpackLow(real, imag);
			right = Avx.UnpackHigh(real, imag);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void ComplexMultiplyConjugateA(Vector256<float> a0, Vector256<float> a1, Vector256<float> b0, Vector256<float> b1, out Vector256<float> left, out Vector256<float> right)
		{
			Vector256<float> realA = Avx.UnpackLow(a0, a1);
			Vector256<float> imagA = Avx.UnpackHigh(a0, a1); // this actually has a negative sign
			Vector256<float> realB = Avx.UnpackLow(b0, b1);
			Vector256<float> imagB = Avx.UnpackHigh(b0, b1);

			Vector256<float> imagProd = Avx.Multiply(imagA, imagB); // this actually has a negative sign
			Vector256<float> BrAi = Avx.Multiply(realB, imagA); // this actually has a negative sign
			Vector256<float> real, imag;
			if (Fma.IsSupported)
			{
				real = Fma.MultiplyAdd(realA, realB, imagProd); // change from subtract to add
				imag = Fma.MultiplySubtract(realA, imagB, BrAi); // change from add to subtract
			}
			else
			{
				real = Avx.Multiply(realA, realB);
				real = Avx.Add(real, imagProd); // change from subtract to add
				imag = Avx.Multiply(realA, imagB); 
				imag = Avx.Subtract(imag, BrAi); // change from add to subtract
			}
			left = Avx.UnpackLow(real, imag);
			right = Avx.UnpackHigh(real, imag);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void ComplexMultiplyConjugateA(Vector256<double> a0, Vector256<double> a1, Vector256<double> b0, Vector256<double> b1, out Vector256<double> left, out Vector256<double> right)
		{
			Vector256<double> realA = Avx.UnpackLow(a0, a1);
			Vector256<double> imagA = Avx.UnpackHigh(a0, a1); // this actually has a negative sign
			Vector256<double> realB = Avx.UnpackLow(b0, b1);
			Vector256<double> imagB = Avx.UnpackHigh(b0, b1);

			Vector256<double> imagProd = Avx.Multiply(imagA, imagB); // this actually has a negative sign
			Vector256<double> BrAi = Avx.Multiply(realB, imagA); // this actually has a negative sign
			Vector256<double> real, imag;
			if (Fma.IsSupported)
			{
				real = Fma.MultiplyAdd(realA, realB, imagProd); // change from subtract to add
				imag = Fma.MultiplySubtract(realA, imagB, BrAi); // change from add to subtract
			}
			else
			{
				real = Avx.Multiply(realA, realB);
				real = Avx.Add(real, imagProd); // change from subtract to add
				imag = Avx.Multiply(realA, imagB);
				imag = Avx.Subtract(imag, BrAi); // change from add to subtract
			}
			left = Avx.UnpackLow(real, imag);
			right = Avx.UnpackHigh(real, imag);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe void ComplexMultiply<Conj>(ComplexSingle* a, ComplexSingle* b, out Vector256<float> left, out Vector256<float> right)
		{
			// {a[0].r, a[0].i, a[1].r, a[1].i, ...}
			Vector256<float> a0 = LoadVector256<float>(a);
			// {a[c].r, a[c].i, a[c+1].r, a[c+1].i, ...}, c == Vector256<T>.Count / 2
			Vector256<float> a1 = LoadVector256<float>(a + Vector256<float>.Count / 2);
			// {b[0].r, b[0].i, b[1].r, b[1].i, ...}
			Vector256<float> b0 = LoadVector256<float>(b);
			// {b[c].r, b[c].i, b[c+1].r, b[c+1].i, ...}, c == Vector256<T>.Count / 2
			Vector256<float> b1 = LoadVector256<float>(b + Vector256<float>.Count / 2);

			if (typeof(Conj) == typeof(bool))
				ComplexMultiplyConjugateA(a0, a1, b0, b1, out left, out right);
			else
				ComplexMultiply(a0, a1, b0, b1, out left, out right);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe void ComplexMultiply<Conj>(ComplexDouble* a, ComplexDouble* b, out Vector256<double> left, out Vector256<double> right)
		{
			// {a[0].r, a[0].i, a[1].r, a[1].i, ...}
			Vector256<double> a0 = LoadVector256<double>(a);
			// {a[c].r, a[c].i, a[c+1].r, a[c+1].i, ...}, c == Vector256<T>.Count / 2
			Vector256<double> a1 = LoadVector256<double>(a + Vector256<double>.Count / 2);
			// {b[0].r, b[0].i, b[1].r, b[1].i, ...}
			Vector256<double> b0 = LoadVector256<double>(b);
			// {b[c].r, b[c].i, b[c+1].r, b[c+1].i, ...}, c == Vector256<T>.Count / 2
			Vector256<double> b1 = LoadVector256<double>(b + Vector256<double>.Count / 2);

			if (typeof(Conj) == typeof(bool))
				ComplexMultiplyConjugateA(a0, a1, b0, b1, out left, out right);
			else
				ComplexMultiply(a0, a1, b0, b1, out left, out right);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void ComplexDivide(Vector256<float> a0, Vector256<float> a1, Vector256<float> b0, Vector256<float> b1, out Vector256<float> left, out Vector256<float> right)
		{
			Vector256<float> realA = Avx.UnpackLow(a0, a1);
			Vector256<float> imagA = Avx.UnpackHigh(a0, a1);
			Vector256<float> realB = Avx.UnpackLow(b0, b1);
			Vector256<float> imagB = Avx.UnpackHigh(b0, b1);

			// get the squares of the absolute values of b
			b0 = Avx.Multiply(b0, b0);
			b1 = Avx.Multiply(b1, b1);
			Vector256<float> squareAbsB = Avx.HorizontalAdd(b0, b1);

			Vector256<float> imagProd = Avx.Multiply(imagA, imagB);
			Vector256<float> ArBi = Avx.Multiply(realA, imagB);
			Vector256<float> real, imag;
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

			left = Avx.UnpackLow(real, imag);
			right = Avx.UnpackHigh(real, imag);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void ComplexDivide(Vector256<double> a0, Vector256<double> a1, Vector256<double> b0, Vector256<double> b1, out Vector256<double> left, out Vector256<double> right)
		{
			Vector256<double> realA = Avx.UnpackLow(a0, a1);
			Vector256<double> imagA = Avx.UnpackHigh(a0, a1);
			Vector256<double> realB = Avx.UnpackLow(b0, b1);
			Vector256<double> imagB = Avx.UnpackHigh(b0, b1);

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

			left = Avx.UnpackLow(real, imag);
			right = Avx.UnpackHigh(real, imag);
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

		#region static
		// AVX optimization is not used unless horizontal add and/or reordering is necessary (for complex types).
		// Since during the test (on Windows 10, .NET 5.0, i7-8700K),
		// System.Numerics.Vector<T> utilizes the AVX instruction directly by the JIT.
		// Therefore the difference between Vector<T> and AVX assembly codes are almost the same,
		// and their performance difference is less than 3% (basically comes from the unoptimized final operations)
		// Both of them outperforms the scalar implementation for around 3 times (this number shall be 4 without any loop-related operation).

		// Also, ARM SIMD is never used since it provides no horizontal add and/or reordering instruction
		// and therefore can be utilized by System.Numerics.Vector<T> automatically.


		#region vector argument (absolute) min / max
		//// Test == int, uint, long, ulong   for   AbsMax, AbsMin, Max, Min
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe int VectorMinMaxReal<T, TInd, Test>(T* x, int length) where T : unmanaged where TInd : unmanaged
		{
			bool doMax = typeof(Test) == typeof(int) || typeof(Test) == typeof(long);
			bool doAbs = typeof(Test) == typeof(int) || typeof(Test) == typeof(uint);
			// maximize with stride == Vector<T>.Count
			// initial
			Vector<T> extremes = LoadVector(x);
			if (doAbs)
				extremes = Vector.Abs(extremes);
			Vector<TInd> indices = new(stackalloc TInd[Vector<T>.Count].FillWithRange(default));
			Vector<TInd> extremeIndices = indices;
			Vector<TInd> increment = new(Vector<T>.Count.GenericConvert<int, TInd>());
			// loop
			int lengthLeft = length - Vector<T>.Count, offset = Vector<T>.Count;
			while (lengthLeft >= Vector<T>.Count)
			{
				indices += increment;
				Vector<TInd> compare;
				// JIT shall optimize the branches and type converts to some code as if they do not exist
				Vector<T> current;
				if (doAbs)
					current = Vector.Abs(LoadVector(x + offset));
				else
					current = LoadVector(x + offset);
				if (doMax)
				{	// abs max || max
					if (typeof(T) == typeof(float))
					{   // T is float and U is int
						Vector<int> temp = Vector.GreaterThan(*(Vector<float>*)&current, *(Vector<float>*)&extremes);
						compare = *(Vector<TInd>*)&temp;
					}
					else if (typeof(T) == typeof(double))
					{   // T is double and U is long
						Vector<long> temp = Vector.GreaterThan(*(Vector<double>*)&current, *(Vector<double>*)&extremes);
						compare = *(Vector<TInd>*)&temp;
					}
					else
					{   // T == U
						Vector<T> temp = Vector.GreaterThan(current, extremes);
						compare = *(Vector<TInd>*)&temp;
					}
					extremes = Vector.Max(current, extremes);
				}
				else
				{   // abs min || min
					if (typeof(T) == typeof(float))
					{   // T is float and U is int
						Vector<int> temp = Vector.LessThan(*(Vector<float>*)&current, *(Vector<float>*)&extremes);
						compare = *(Vector<TInd>*)&temp;
					}
					else if (typeof(T) == typeof(double))
					{   // T is double and U is long
						Vector<long> temp = Vector.LessThan(*(Vector<double>*)&current, *(Vector<double>*)&extremes);
						compare = *(Vector<TInd>*)&temp;
					}
					else
					{   // T == U
						Vector<T> temp = Vector.LessThan(current, extremes);
						compare = *(Vector<TInd>*)&temp;
					}
					extremes = Vector.Min(current, extremes);
				}
				extremeIndices = Vector.ConditionalSelect(compare, indices, extremeIndices);
				lengthLeft -= Vector<T>.Count;
				offset += Vector<T>.Count;
			}
			// reduce main
			VectorMinMaxManaged<T, Test>((T*)&extremes, Vector<T>.Count, out long extremeIndex);
			int index = (int)extremeIndex;
			// reduce left
			if (lengthLeft > 0)
			{
				VectorMinMaxManaged<T, Test>(x + offset, lengthLeft, out long restExtreme);
				int newIndex = (int)(offset + restExtreme);
				if (doMax && x[newIndex].GenericLargerThan(extremes[index]))
					index = newIndex;
				if (!doMax && x[newIndex].GenericLessThan(extremes[index]))
					index = newIndex;
			}
			return index;
		}

		//// Test == int, uint   for   AbsMax, AbsMin
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe int VectorMinMaxCompexSingle<Test>(ComplexSingle* x, int length)
		{
			bool doMax = typeof(Test) == typeof(int);
			// initialize
			Vector256<float> extremes = ComplexSquareAbs(x);
			Vector256<int> indices = new Vector<int>(stackalloc int[Vector256<int>.Count].FillWithRange(0)).AsVector256();
			Vector256<int> extremeIndices = indices;
			Vector256<int> increment = new Vector<int>(Vector256<int>.Count).AsVector256();
			// loop
			int lengthLeft = length - Vector256<float>.Count, offset = Vector256<float>.Count;
			while (lengthLeft >= Vector256<float>.Count) // Vector256<ComplexSingle>.Count * 2
			{
				indices = Avx2.Add(indices, increment);
				Vector256<float> squares = ComplexSquareAbs(x + offset);
				Vector256<float> compare;
				if (doMax)
				{   // abs max
					compare = Avx.CompareNotGreaterThan(squares, extremes); // not since Avx2.BlendVariable is reversed
					extremes = Avx.Max(squares, extremes);
				}
				else
				{   // abs min
					compare = Avx.CompareNotLessThan(squares, extremes); // not since Avx2.BlendVariable is reversed
					extremes = Avx.Max(squares, extremes);
				}
				extremeIndices = Avx2.BlendVariable(indices, extremeIndices, compare.AsInt32());
				lengthLeft -= Vector256<float>.Count;
				offset += Vector256<float>.Count;
			}
			// reduce main
			float extreme = ((float*)&extremes)[0]; int extremeIndex = ((int*)&extremeIndices)[0];
			for (int i = 1; i < Vector256<float>.Count; i++)
			{
				float v = ((float*)&extremes)[i];
				if (doMax && v > extreme)
				{
					extreme = v; extremeIndex = ((int*)&extremeIndices)[i];
				}
				if (!doMax && v < extreme)
				{
					extreme = v; extremeIndex = ((int*)&extremeIndices)[i];
				}
			}
			// reduce left
			if (lengthLeft > 0)
			{
				for (; offset < length; offset++)
				{
					var v = x[offset].Abs();
					if (doMax && v > extreme)
					{
						extreme = v; extremeIndex = offset;
					}
					if (!doMax && v < extreme)
					{
						extreme = v; extremeIndex = offset;
					}
				}
			}
			return extremeIndex;
		}

		//// Test == int, uint   for   AbsMax, AbsMin
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe long VectorMinMaxCompexDouble<Test>(ComplexDouble* x, int length)
		{
			bool doMax = typeof(Test) == typeof(int);
			// initialize
			Vector256<double> extremes = ComplexSquareAbs(x);
			Vector256<long> indices = new Vector<long>(stackalloc long[Vector256<long>.Count].FillWithRange(0)).AsVector256();
			Vector256<long> extremeIndices = indices;
			Vector256<long> increment = new Vector<long>(Vector256<long>.Count).AsVector256();
			// loop
			int lengthLeft = length - Vector256<double>.Count, offset = Vector256<double>.Count;
			while (lengthLeft >= Vector256<double>.Count) // Vector256<ComplexDouble>.Count * 2
			{
				indices = Avx2.Add(indices, increment);
				Vector256<double> squares = ComplexSquareAbs(x + offset);
				Vector256<double> compare;
				if (doMax)
				{   // abs max
					compare = Avx.CompareNotGreaterThan(squares, extremes); // not since Avx2.BlendVariable is reversed
					extremes = Avx.Max(squares, extremes);
				}
				else
				{   // abs min
					compare = Avx.CompareNotLessThan(squares, extremes); // not since Avx2.BlendVariable is reversed
					extremes = Avx.Max(squares, extremes);
				}
				extremeIndices = Avx2.BlendVariable(indices, extremeIndices, compare.AsInt64());
				lengthLeft -= Vector256<double>.Count;
				offset += Vector256<double>.Count;
			}
			// reduce main
			double extreme = ((double*)&extremes)[0]; long extremeIndex = ((long*)&extremeIndices)[0];
			for (int i = 1; i < Vector256<double>.Count; i++)
			{
				double v = ((double*)&extremes)[i];
				if (doMax && v > extreme)
				{
					extreme = v; extremeIndex = ((long*)&extremeIndices)[i];
				}
				if (!doMax && v < extreme)
				{
					extreme = v; extremeIndex = ((long*)&extremeIndices)[i];
				}
			}
			// reduce left
			if (lengthLeft > 0)
			{
				for (; offset < length; offset++)
				{
					double v = x[offset].SquareAbs();
					if (doMax && v > extreme)
					{
						extreme = v; extremeIndex = offset;
					}
					if (!doMax && v < extreme)
					{
						extreme = v; extremeIndex = offset;
					}
				}
			}
			return extremeIndex;
		}

		//// Test == int, uint, long, ulong   for   AbsMax, AbsMin, Max, Min
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe bool VectorMinMaxManaged<T, Test>(T* x, int length, out long index) where T : unmanaged
		{
			index = -1;
			bool doMax = typeof(Test) == typeof(int) || typeof(Test) == typeof(long);
			bool doAbs = typeof(Test) == typeof(int) || typeof(Test) == typeof(uint);
			if (doAbs)
			{
				Func<T, double> abs = ConstExtension.GetAbsoluteGetter<T>();
				double extreme = doMax ? double.MinValue : double.MaxValue;
				int extremeIndex = 0;
				for (int i = 0; i < length; i++)
				{
					double v;
					// some frequent type speedups
					if (typeof(T) == typeof(byte))
						v = ((byte*)x)[i];
					if (typeof(T) == typeof(ushort))
						v = ((ushort*)x)[i];
					if (typeof(T) == typeof(char))
						v = ((char*)x)[i];
					if (typeof(T) == typeof(uint))
						v = ((uint*)x)[i];
					if (typeof(T) == typeof(ulong))
						v = ((ulong*)x)[i];
					if (typeof(T) == typeof(sbyte))
						v = Math.Abs(((sbyte*)x)[i]);
					if (typeof(T) == typeof(short))
						v = Math.Abs(((short*)x)[i]);
					if (typeof(T) == typeof(int))
						v = Math.Abs(((int*)x)[i]);
					if (typeof(T) == typeof(long))
						v = Math.Abs(((long*)x)[i]);
					if (typeof(T) == typeof(float))
						v = MathF.Abs(((float*)x)[i]);
					if (typeof(T) == typeof(double))
						v = Math.Abs(((double*)x)[i]);
					if (typeof(T) == typeof(ComplexSingle) || typeof(T) == typeof(Complex<float>))
						v = ((ComplexSingle*)x)[i].Abs();
					if (typeof(T) == typeof(ComplexDouble) || typeof(T) == typeof(Complex<double>))
						v = ((ComplexDouble*)x)[i].Abs();
					else
						v = abs(x[i]);
					if ((doMax && v > extreme) || (!doMax && v < extreme))
					{
						extreme = v; extremeIndex = i;
					}
				}
				index = extremeIndex;
			}
			else
			{
				Func<T, T, bool>? compare;
				if (doMax)
					compare = CompareOperation.GreaterThan.GetComparer<T>();
				else
					compare = CompareOperation.LessThan.GetComparer<T>();
				if (compare is null)
					return false; // not support

				T extreme = x[0]; int extremeIndex = 0;
				for (int i = 1; i < length; i++)
				{
					T v = x[i];
					// some frequent type speedups, complex is not possible here
					if (doMax)
					{
						if ((typeof(T) == typeof(sbyte) && ((sbyte*)&x)[i] > *(sbyte*)&extreme) ||
							(typeof(T) == typeof(ushort) && ((ushort*)&x)[i] > *(ushort*)&extreme) ||
							(typeof(T) == typeof(uint) && ((uint*)&x)[i] > *(uint*)&extreme) ||
							(typeof(T) == typeof(ulong) && ((ulong*)&x)[i] > *(ulong*)&extreme) ||
							(typeof(T) == typeof(byte) && ((byte*)&x)[i] > *(byte*)&extreme) ||
							(typeof(T) == typeof(short) && ((short*)&x)[i] > *(short*)&extreme) || 
							(typeof(T) == typeof(int) && ((int*)&x)[i] > *(int*)&extreme) ||
							(typeof(T) == typeof(long) && ((long*)&x)[i] > *(long*)&extreme) ||
							(typeof(T) == typeof(float) && ((float*)&x)[i] > *(float*)&extreme) ||
							(typeof(T) == typeof(double) && ((double*)&x)[i] > *(double*)&extreme) ||
							compare(v, extreme))
						{
							extreme = v; extremeIndex = i;
						}
					}
					else
					{
						if ((typeof(T) == typeof(sbyte) && ((sbyte*)&x)[i] < *(sbyte*)&extreme) ||
							(typeof(T) == typeof(ushort) && ((ushort*)&x)[i] < *(ushort*)&extreme) ||
							(typeof(T) == typeof(uint) && ((uint*)&x)[i] < *(uint*)&extreme) ||
							(typeof(T) == typeof(ulong) && ((ulong*)&x)[i] < *(ulong*)&extreme) ||
							(typeof(T) == typeof(byte) && ((byte*)&x)[i] < *(byte*)&extreme) ||
							(typeof(T) == typeof(short) && ((short*)&x)[i] < *(short*)&extreme) ||
							(typeof(T) == typeof(int) && ((int*)&x)[i] < *(int*)&extreme) ||
							(typeof(T) == typeof(long) && ((long*)&x)[i] < *(long*)&extreme) ||
							(typeof(T) == typeof(float) && ((float*)&x)[i] < *(float*)&extreme) ||
							(typeof(T) == typeof(double) && ((double*)&x)[i] < *(double*)&extreme) ||
							compare(v, extreme))
						{
							extreme = v; extremeIndex = i;
						}
					}
				}
				index = extremeIndex;
			}
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static unsafe bool ArgMinMax<T, Test>(Storage<T> x, out long index) where T : unmanaged
		{
			index = -1;
			if ((typeof(Test) == typeof(long) || typeof(Test) == typeof(ulong)) && Const<T>.IsComplex)
				throw new InvalidOperationException(string.Format(Resource.CompareComplex, typeof(T).GetGenericString()));
			if (!GetSpan(x, out T* px, out int length))
				return false;
			if (length == 0)
				return true;
			if (length == 1)
			{
				index = 0; return true;
			}
			if (!Vector.IsHardwareAccelerated || length <= (Vector<byte>.Count / sizeof(T) * 4))
				return VectorMinMaxManaged<T, Test>(px, length, out index); // no SIMD or too short
			if ((sizeof(T) <= sizeof(byte) && length > sbyte.MaxValue) || (sizeof(T) <= sizeof(short) && length > short.MaxValue))
				return VectorMinMaxManaged<T, Test>(px, length, out index);

			if (Const<T>.IsComplex)
			{
				if (Const<T>.IsIntegralType || !Avx2.IsSupported)
					return VectorMinMaxManaged<T, Test>(px, length, out index);
				if (typeof(T) == typeof(float))
				{
					index = VectorMinMaxCompexSingle<Test>((ComplexSingle*)px, length);
				}
				else // double
				{
					index = VectorMinMaxCompexDouble<Test>((ComplexDouble*)px, length);
				}
			}
			else
			{
				if (typeof(T) == typeof(float))
				{
					index = VectorMinMaxReal<float, int, Test>((float*)px, length);
				}
				else if (typeof(T) == typeof(double))
				{
					index = VectorMinMaxReal<double, long, Test>((double*)px, length);
				}
				else
				{   // integral type
					index = VectorMinMaxReal<T, T, Test>(px, length);
				}
			}
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static bool AbsoluteValueArgMax<T>(Storage<T> x, out long index) where T : unmanaged
		{
			return ArgMinMax<T, int>(x, out index);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static unsafe bool AbsoluteValueArgMin<T>(Storage<T> x, out long index) where T : unmanaged
		{
			return ArgMinMax<T, uint>(x, out index);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static unsafe bool ArgMax<T>(Storage<T> x, out long index) where T : unmanaged
		{
			return ArgMinMax<T, long>(x, out index);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static unsafe bool ArgMin<T>(Storage<T> x, out long index) where T : unmanaged
		{
			return ArgMinMax<T, ulong>(x, out index);
		}
		#endregion


		#region (absolute) sum product
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe bool VectorAbsSumProdManaged<T, Test>(T* x, int length, out T aggregateNoAbs, out double aggregateAbs) where T : unmanaged
		{
			aggregateNoAbs = default; aggregateAbs = 0;
			bool doSum = typeof(Test) == typeof(int) || typeof(Test) == typeof(long);
			bool doAbs = typeof(Test) == typeof(int) || typeof(Test) == typeof(uint);
			if (doAbs)
			{
				Func<T, double> abs = ConstExtension.GetAbsoluteGetter<T>();
				aggregateAbs = doSum ? 0 : 1;
				for (int i = 0; i < length; i++)
				{
					double v;
					// some frequent type speedups
					if (typeof(T) == typeof(byte))
						v = ((byte*)x)[i];
					if (typeof(T) == typeof(ushort))
						v = ((ushort*)x)[i];
					if (typeof(T) == typeof(char))
						v = ((char*)x)[i];
					if (typeof(T) == typeof(uint))
						v = ((uint*)x)[i];
					if (typeof(T) == typeof(ulong))
						v = ((ulong*)x)[i];
					if (typeof(T) == typeof(sbyte))
						v = Math.Abs(((sbyte*)x)[i]);
					if (typeof(T) == typeof(short))
						v = Math.Abs(((short*)x)[i]);
					if (typeof(T) == typeof(int))
						v = Math.Abs(((int*)x)[i]);
					if (typeof(T) == typeof(long))
						v = Math.Abs(((long*)x)[i]);
					if (typeof(T) == typeof(float))
						v = MathF.Abs(((float*)x)[i]);
					if (typeof(T) == typeof(double))
						v = Math.Abs(((double*)x)[i]);
					if (typeof(T) == typeof(ComplexSingle) || typeof(T) == typeof(Complex<float>))
						v = ((ComplexSingle*)x)[i].Abs();
					if (typeof(T) == typeof(ComplexDouble) || typeof(T) == typeof(Complex<double>))
						v = ((ComplexDouble*)x)[i].Abs();
					else
						v = abs(x[i]);
					if (doSum)
						aggregateAbs += v;
					else
						aggregateAbs *= v;
				}
			}
			else
			{
				Func<T, T, T> op = doSum ? BinaryArithmeticOperation.Addition.GetArithmeticOperation<T>() : BinaryArithmeticOperation.Multiply.GetArithmeticOperation<T>();

				T result = doSum ? default : Const<T>.One;
				for (int i = 0; i < length; i++)
				{
					T v = x[i];
					// some frequent type speedups
					if (doSum)
					{
						if (typeof(T) == typeof(uint))
						{
							var vv = *(uint*)&result + *(uint*)&v;
							result = *(T*)&vv;
						}
						if (typeof(T) == typeof(ulong))
						{
							var vv = *(ulong*)&result + *(ulong*)&v;
							result = *(T*)&vv;
						}
						if (typeof(T) == typeof(int))
						{
							var vv = *(int*)&result + *(int*)&v;
							result = *(T*)&vv;
						}
						if (typeof(T) == typeof(long))
						{
							var vv = *(long*)&result + *(long*)&v;
							result = *(T*)&vv;
						}
						if (typeof(T) == typeof(float))
						{
							var vv = *(float*)&result + *(float*)&v;
							result = *(T*)&vv;
						}
						if (typeof(T) == typeof(double))
						{
							var vv = *(double*)&result + *(double*)&v;
							result = *(T*)&vv;
						}
						if (typeof(T) == typeof(ComplexSingle) || typeof(T) == typeof(Complex<float>))
						{
							var vv = *(ComplexSingle*)&result + *(ComplexSingle*)&v;
							result = *(T*)&vv;
						}
						if (typeof(T) == typeof(ComplexDouble) || typeof(T) == typeof(Complex<double>))
						{
							var vv = *(ComplexDouble*)&result + *(ComplexDouble*)&v;
							result = *(T*)&vv;
						}
						else
							result = op(result, v);
					}
					else
					{
						if (typeof(T) == typeof(uint))
						{
							var vv = *(uint*)&result * *(uint*)&v;
							result = *(T*)&vv;
						}
						if (typeof(T) == typeof(ulong))
						{
							var vv = *(ulong*)&result * *(ulong*)&v;
							result = *(T*)&vv;
						}
						if (typeof(T) == typeof(int))
						{
							var vv = *(int*)&result * *(int*)&v;
							result = *(T*)&vv;
						}
						if (typeof(T) == typeof(long))
						{
							var vv = *(long*)&result * *(long*)&v;
							result = *(T*)&vv;
						}
						if (typeof(T) == typeof(float))
						{
							var vv = *(float*)&result * *(float*)&v;
							result = *(T*)&vv;
						}
						if (typeof(T) == typeof(double))
						{
							var vv = *(double*)&result * *(double*)&v;
							result = *(T*)&vv;
						}
						if (typeof(T) == typeof(ComplexSingle) || typeof(T) == typeof(Complex<float>))
						{
							var vv = *(ComplexSingle*)&result * *(ComplexSingle*)&v;
							result = *(T*)&vv;
						}
						if (typeof(T) == typeof(ComplexDouble) || typeof(T) == typeof(Complex<double>))
						{
							var vv = *(ComplexDouble*)&result * *(ComplexDouble*)&v;
							result = *(T*)&vv;
						}
						else
							result = op(result, v);
					}
				}
			}
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe void VectorAbsSumProdReal<T, Test>(T* x, int length, out T aggregateNoAbs, out double aggregateAbs) where T : unmanaged
		{
			aggregateNoAbs = default; aggregateAbs = 0;
			bool doSum = typeof(Test) == typeof(int) || typeof(Test) == typeof(long);
			bool doAbs = typeof(Test) == typeof(int) || typeof(Test) == typeof(uint);
			// initial
			Vector<T> aggregate = LoadVector(x);
			if (doAbs)
				aggregate = Vector.Abs(aggregate);
			// loop
			int lengthLeft = length - Vector<T>.Count, offset = Vector<T>.Count;
			while (lengthLeft >= Vector<T>.Count)
			{
				Vector<T> current = LoadVector(x + offset);
				if (doAbs)
					current = Vector.Abs(current);
				if (doSum)
					aggregate += current;
				else
					aggregate *= current;
				lengthLeft -= Vector<T>.Count;
				offset += Vector<T>.Count;
			}
			// reduce left
			if (lengthLeft > 0)
			{
				VectorAbsSumProdManaged<T, Test>(x + offset, lengthLeft, out aggregateNoAbs, out aggregateAbs);
			}
			else if (!doSum)
			{
				aggregateNoAbs = Const<T>.One; aggregateAbs = 1;
			}
			// return
			if (doSum)
			{
				double result = Vector.Dot(aggregate, Vector<T>.One).ToDouble();
				aggregateAbs += result;
			}
			else
			{
				Func<T, T, T> mul = BinaryArithmeticOperation.Multiply.GetArithmeticOperation<T>();
				T result = aggregate[0];
				for (int i = 1; i < Vector<T>.Count; i++)
				{
					result = mul(result, aggregate[i]);
				}
				aggregateNoAbs = mul(result, aggregateNoAbs);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe void VectorAbsSumProdCompexSingle<Test>(ComplexSingle* x, int length, out ComplexSingle aggregateNoAbs, out double aggregateAbs)
		{
			aggregateNoAbs = default; aggregateAbs = 0;
			bool doSum = typeof(Test) == typeof(int) || typeof(Test) == typeof(long);
			bool doAbs = typeof(Test) == typeof(int) || typeof(Test) == typeof(uint);
			if (doAbs)
			{
				// initialize
				Vector256<float> aggregate = ComplexSquareAbs(x);
				if (doSum)
					aggregate = Avx.Sqrt(aggregate);
				// loop
				int lengthLeft = length - Vector256<float>.Count, offset = Vector256<float>.Count;
				while (lengthLeft >= Vector256<float>.Count) // Vector256<ComplexSingle>.Count * 2
				{
					Vector256<float> squares = ComplexSquareAbs(x + offset);
					if (doSum)
					{
						squares = Avx.Sqrt(squares);
						aggregate = Avx.Add(aggregate, squares);
					}
					else
					{
						aggregate = Avx.Multiply(aggregate, squares);
					}
					lengthLeft -= Vector256<float>.Count;
					offset += Vector256<float>.Count;
				}
				// reduce main
				float result = ((float*)&aggregate)[0];
				for (int i = 1; i < Vector256<float>.Count; i++)
				{
					float v = ((float*)&aggregate)[i];
					if (doSum)
					{
						result += v;
					}
					else
					{
						result *= v;
					}
				}
				if (!doSum)
				{   // sqrt
					result = MathF.Sqrt(result);
				}
				// reduce left
				if (lengthLeft > 0)
				{
					for (; offset < length; offset++)
					{
						float v = x[offset].Abs();
						if (doSum)
						{
							result += v;
						}
						else
						{
							result *= v;
						}
					}
				}
				aggregateAbs = result;
			}
			else
			{
				// initialize
				Vector256<float> aggregate1 = LoadVector256<float>(x), aggregate2 = LoadVector256<float>(x + Vector256<float>.Count / 2);
				// loop
				int lengthLeft = length - Vector256<float>.Count, offset = Vector256<float>.Count;
				while (lengthLeft >= Vector256<float>.Count) // Vector256<ComplexSingle>.Count * 2
				{
					Vector256<float> current1 = LoadVector256<float>(x + offset);
					Vector256<float> current2 = LoadVector256<float>(x + offset + Vector256<float>.Count / 2);
					if (doSum)
					{
						aggregate1 = Avx.Add(aggregate1, current1);
						aggregate2 = Avx.Add(aggregate2, current2);
					}
					else
					{
						ComplexMultiply(aggregate1, aggregate2, current1, current2, out aggregate1, out aggregate2);
					}
					lengthLeft -= Vector256<float>.Count;
					offset += Vector256<float>.Count;
				}
				// reduce main
				ComplexSingle result = ((ComplexSingle*)&aggregate1)[0];
				for (int i = 1; i < Vector256<float>.Count / 2; i++)
				{
					ComplexSingle v = ((ComplexSingle*)&aggregate1)[i];
					if (doSum)
					{
						result += v;
					}
					else
					{
						result *= v;
					}
				}
				for (int i = 0; i < Vector256<float>.Count / 2; i++)
				{
					ComplexSingle v = ((ComplexSingle*)&aggregate2)[i];
					if (doSum)
					{
						result += v;
					}
					else
					{
						result *= v;
					}
				}
				// reduce left
				if (lengthLeft > 0)
				{
					for (; offset < length; offset++)
					{
						ComplexSingle v = x[offset];
						if (doSum)
						{
							result += v;
						}
						else
						{
							result *= v;
						}
					}
				}
				aggregateNoAbs = result;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe void VectorAbsSumProdCompexDouble<Test>(ComplexDouble* x, int length, out ComplexDouble aggregateNoAbs, out double aggregateAbs)
		{
			aggregateNoAbs = default; aggregateAbs = 0;
			bool doSum = typeof(Test) == typeof(int) || typeof(Test) == typeof(long);
			bool doAbs = typeof(Test) == typeof(int) || typeof(Test) == typeof(uint);
			if (doAbs)
			{
				// initialize
				Vector256<double> aggregate = ComplexSquareAbs(x);
				if (doSum)
					aggregate = Avx.Sqrt(aggregate);
				// loop
				int lengthLeft = length - Vector256<double>.Count, offset = Vector256<double>.Count;
				while (lengthLeft >= Vector256<double>.Count) // Vector256<ComplexDouble>.Count * 2
				{
					Vector256<double> squares = ComplexSquareAbs(x + offset);
					if (doSum)
					{
						squares = Avx.Sqrt(squares);
						aggregate = Avx.Add(aggregate, squares);
					}
					else
					{
						aggregate = Avx.Multiply(aggregate, squares);
					}
					lengthLeft -= Vector256<double>.Count;
					offset += Vector256<double>.Count;
				}
				// reduce main
				double result = ((double*)&aggregate)[0];
				for (int i = 1; i < Vector256<double>.Count; i++)
				{
					double v = ((double*)&aggregate)[i];
					if (doSum)
					{
						result += v;
					}
					else
					{
						result *= v;
					}
				}
				if (!doSum)
				{   // sqrt
					result = Math.Sqrt(result);
				}
				// reduce left
				if (lengthLeft > 0)
				{
					for (; offset < length; offset++)
					{
						double v = x[offset].Abs();
						if (doSum)
						{
							result += v;
						}
						else
						{
							result *= v;
						}
					}
				}
				aggregateAbs = result;
			}
			else
			{
				// initialize
				Vector256<double> aggregate1 = LoadVector256<double>(x), aggregate2 = LoadVector256<double>(x + Vector256<double>.Count / 2);
				// loop
				int lengthLeft = length - Vector256<double>.Count, offset = Vector256<double>.Count;
				while (lengthLeft >= Vector256<double>.Count) // Vector256<ComplexDouble>.Count * 2
				{
					Vector256<double> current1 = LoadVector256<double>(x + offset);
					Vector256<double> current2 = LoadVector256<double>(x + offset + Vector256<double>.Count / 2);
					if (doSum)
					{
						aggregate1 = Avx.Add(aggregate1, current1);
						aggregate2 = Avx.Add(aggregate2, current2);
					}
					else
					{
						ComplexMultiply(aggregate1, aggregate2, current1, current2, out aggregate1, out aggregate2);
					}
					lengthLeft -= Vector256<double>.Count;
					offset += Vector256<double>.Count;
				}
				// reduce main
				ComplexDouble result = ((ComplexDouble*)&aggregate1)[0];
				for (int i = 1; i < Vector256<double>.Count / 2; i++)
				{
					ComplexDouble v = ((ComplexDouble*)&aggregate1)[i];
					if (doSum)
					{
						result += v;
					}
					else
					{
						result *= v;
					}
				}
				for (int i = 0; i < Vector256<double>.Count / 2; i++)
				{
					ComplexDouble v = ((ComplexDouble*)&aggregate2)[i];
					if (doSum)
					{
						result += v;
					}
					else
					{
						result *= v;
					}
				}
				// reduce left
				if (lengthLeft > 0)
				{
					for (; offset < length; offset++)
					{
						ComplexDouble v = x[offset];
						if (doSum)
						{
							result += v;
						}
						else
						{
							result *= v;
						}
					}
				}
				aggregateNoAbs = result;
			}
		}

		//// Test == int, uint, long, ulong   for   AbsSum, AbsProd, Sum, Prod
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static unsafe bool AbsSumProd<T, Test>(Storage<T> x, out T aggregateNoAbs, out double aggregateAbs) where T : unmanaged
		{
			aggregateNoAbs = default; aggregateAbs = 0;
			if (!GetSpan(x, out T* px, out int length))
				return false;
			if (length == 0)
			{
				if (typeof(Test) == typeof(uint) || typeof(Test) == typeof(ulong))
				{
					aggregateAbs = 1; aggregateNoAbs = Const<T>.One;
				}
				return true;
			}
			if (length == 1)
			{
				aggregateNoAbs = px[0]; aggregateAbs = aggregateNoAbs.ToDouble();
				return true;
			}
			if (!Vector.IsHardwareAccelerated || length <= (Vector<byte>.Count / sizeof(T) * 4)) // no SIMD or too short
				return VectorAbsSumProdManaged<T, Test>(px, length, out aggregateNoAbs, out aggregateAbs);

			if (Const<T>.IsComplex)
			{
				if (Const<T>.IsIntegralType || !Avx.IsSupported) // no AVX's HorizontalAdd and Unpack (Vector<T> has not corresponding implementation yet)
					return VectorAbsSumProdManaged<T, Test>(px, length, out aggregateNoAbs, out aggregateAbs);
				if (typeof(T) == typeof(float))
				{
					VectorAbsSumProdCompexSingle<Test>((ComplexSingle*)px, length, out var temp, out aggregateAbs);
					aggregateNoAbs = *(T*)&temp;
				}
				else // double
				{
					VectorAbsSumProdCompexDouble<Test>((ComplexDouble*)px, length, out var temp, out aggregateAbs);
					aggregateNoAbs = *(T*)&temp;
				}
			}
			else
			{
				VectorAbsSumProdReal<T, Test>(px, length, out aggregateNoAbs, out aggregateAbs);
			}
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static bool AbsoluteValueSum<T>(Storage<T> x, out double sum) where T : unmanaged
		{
			return AbsSumProd<T, int>(x, out _, out sum);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static bool AbsoluteValueProduct<T>(Storage<T> x, out double product) where T : unmanaged
		{
			return AbsSumProd<T, uint>(x, out _, out product);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static bool AggregateSum<T>(Storage<T> x, out T sum) where T : unmanaged
		{
			return AbsSumProd<T, long>(x, out sum, out _);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static bool AggregateProduct<T>(Storage<T> x, out T product) where T : unmanaged
		{
			return AbsSumProd<T, ulong>(x, out product, out _);
		}
		#endregion


		#region inner
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe T VectorInnerManaged<T, Dot, Conj>(T* x, T* y, int length) where T : unmanaged
		{
			T result = default;
			bool doDot = typeof(Dot) == typeof(bool);
			bool doCon = typeof(Conj) == typeof(bool);

			Func<T, T, T> multiply = BinaryArithmeticOperation.Multiply.GetArithmeticOperation<T>();
			Func<T, T, T> add = BinaryArithmeticOperation.Addition.GetArithmeticOperation<T>();
			Func<T, T> conj = UnaryArithmeticOperation.Conjugate.GetArithmeticOperation<T>();

			for (int i = 0; i < length; i++)
			{
				T a = x[i], b = y[i];
				if (typeof(T) == typeof(uint))
				{
					uint vv;
					if (doDot)
						vv = (*(uint*)&result) + (*(uint*)&a) * (*(uint*)&b);
					else
						vv = (*(uint*)&result) + (*(uint*)&a) * (*(uint*)&a);
					result = *(T*)&vv;
				}
				if (typeof(T) == typeof(ulong))
				{
					ulong vv;
					if (doDot)
						vv = (*(ulong*)&result) + (*(ulong*)&a) * (*(ulong*)&b);
					else
						vv = (*(ulong*)&result) + (*(ulong*)&a) * (*(ulong*)&a);
					result = *(T*)&vv;
				}
				if (typeof(T) == typeof(int))
				{
					int vv;
					if (doDot)
						vv = (*(int*)&result) + (*(int*)&a) * (*(int*)&b);
					else
						vv = (*(int*)&result) + (*(int*)&a) * (*(int*)&a);
					result = *(T*)&vv;
				}
				if (typeof(T) == typeof(long))
				{
					long vv;
					if (doDot)
						vv = (*(long*)&result) + (*(long*)&a) * (*(long*)&b);
					else
						vv = (*(long*)&result) + (*(long*)&a) * (*(long*)&a);
					result = *(T*)&vv;
				}
				if (typeof(T) == typeof(float))
				{
					float vv;
					if (doDot)
						vv = (*(float*)&result) + (*(float*)&a) * (*(float*)&b);
					else
						vv = (*(float*)&result) + (*(float*)&a) * (*(float*)&a);
					result = *(T*)&vv;
				}
				if (typeof(T) == typeof(double))
				{
					double vv;
					if (doDot)
						vv = (*(double*)&result) + (*(double*)&a) * (*(double*)&b);
					else
						vv = (*(double*)&result) + (*(double*)&a) * (*(double*)&a);
					result = *(T*)&vv;
				}
				if (typeof(T) == typeof(ComplexSingle) || typeof(T) == typeof(Complex<float>))
				{
					ComplexSingle vv;
					if (doDot && doCon)
						vv = (*(ComplexSingle*)&result) + (*(ComplexSingle*)&a).Conjugate() * (*(ComplexSingle*)&b);
					else if (doDot && !doCon)
						vv = (*(ComplexSingle*)&result) + (*(ComplexSingle*)&a) * (*(ComplexSingle*)&b);
					else if (!doDot && doCon)
						vv = (*(ComplexSingle*)&result) + (*(ComplexSingle*)&a).SquareAbs();
					else
						vv = (*(ComplexSingle*)&result) + (*(ComplexSingle*)&a) * (*(ComplexSingle*)&a);
					result = *(T*)&vv;
				}
				if (typeof(T) == typeof(ComplexDouble) || typeof(T) == typeof(Complex<double>))
				{
					ComplexDouble vv;
					if (doDot && doCon)
						vv = (*(ComplexDouble*)&result) + (*(ComplexDouble*)&a).Conjugate() * (*(ComplexDouble*)&b);
					else if (doDot && !doCon)
						vv = (*(ComplexDouble*)&result) + (*(ComplexDouble*)&a) * (*(ComplexDouble*)&b);
					else if (!doDot && doCon)
						vv = (*(ComplexDouble*)&result) + (*(ComplexDouble*)&a).SquareAbs();
					else
						vv = (*(ComplexDouble*)&result) + (*(ComplexDouble*)&a) * (*(ComplexDouble*)&a);
					result = *(T*)&vv;
				}
				else
				{
					if (doDot && doCon)
						result = add(result, multiply(conj(a), b));
					else if (doDot && !doCon)
						result = add(result, multiply(a, b));
					else if (!doDot && doCon)
						result = add(result, multiply(conj(a), a));
					else
						result = add(result, multiply(a, a));
				}
			}
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe T VectorInnerReal<T, Dot>(T* x, T* y, int length) where T : unmanaged
		{
			bool doDot = typeof(Dot) == typeof(bool);
			// reduce to Vector<T>.Count sums
			Vector<T> sum = Vector<T>.Zero;
			int lengthLeft = length, offset = 0;
			while (lengthLeft >= Vector<T>.Count)
			{
				if (Fma.IsSupported && Vector<byte>.Count == Vector256<byte>.Count && typeof(T) == typeof(double))
				{
					Vector256<double> temp;
					if (doDot)
						temp = Fma.MultiplyAdd(LoadVector256<double>(x + offset), LoadVector256<double>(y + offset), sum.AsVector256().AsDouble());
					else
						temp = Fma.MultiplyAdd(LoadVector256<double>(x + offset), LoadVector256<double>(x + offset), sum.AsVector256().AsDouble());
					sum = *(Vector<T>*)&temp;
				}
				else if (Fma.IsSupported && Vector<byte>.Count == Vector256<byte>.Count && typeof(T) == typeof(float))
				{
					Vector256<float> temp;
					if (doDot)
						temp = Fma.MultiplyAdd(LoadVector256<float>(x + offset), LoadVector256<float>(y + offset), sum.AsVector256().AsSingle());
					else
						temp = Fma.MultiplyAdd(LoadVector256<float>(x + offset), LoadVector256<float>(x + offset), sum.AsVector256().AsSingle());
					sum = *(Vector<T>*)&temp;
				}
				else
				{
					if (doDot)
						sum += LoadVector(x + offset) * LoadVector(y + offset);
					else
						sum += LoadVector(x + offset) * LoadVector(x + offset);
				}
				lengthLeft -= Vector<T>.Count;
				offset += Vector<T>.Count;
			}
			// reduce left
			T dotLeft = default;
			if (lengthLeft > 0)
			{
				Vector<T> leftA = Vector<T>.Zero, leftB = Vector<T>.Zero;
				// the following two lines shall be unrolled by JIT at runtime
				Unsafe.CopyBlock(&leftA, x + offset, (uint)(lengthLeft * sizeof(T)));
				Unsafe.CopyBlock(&leftB, y + offset, (uint)(lengthLeft * sizeof(T)));
				dotLeft = Vector.Dot(leftA, leftB);
				// this implementation has some performance loss compare to the direct dot
				// but it is suitable for all generic type T that Vector<T> supports
			}
			// this implementation has some performance loss, same reason as above
			T dotMain = Vector.Dot(sum, Vector<T>.One);
			// return
			return dotMain.GenericAdd(dotLeft);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe ComplexSingle VectorInnerCompexSingle<Dot, Conj>(ComplexSingle* x, ComplexSingle* y, int length)
		{
			ComplexSingle innerResult;
			bool doDot = typeof(Dot) == typeof(bool);
			bool doCon = typeof(Conj) == typeof(bool);
			if (!doDot)
			{
				// initialize
				Vector256<float> aggregate = ComplexSquareAbs(x);
				// loop
				int lengthLeft = length - Vector256<float>.Count, offset = Vector256<float>.Count;
				while (lengthLeft >= Vector256<float>.Count) // Vector256<ComplexSingle>.Count * 2
				{
					Vector256<float> squares = ComplexSquareAbs(x + offset);
					aggregate = Avx.Add(aggregate, squares);
					lengthLeft -= Vector256<float>.Count;
					offset += Vector256<float>.Count;
				}
				// reduce main
				float result = ((float*)&aggregate)[0];
				for (int i = 1; i < Vector256<float>.Count; i++)
				{
					float v = ((float*)&aggregate)[i];
					result += v;
				}
				// reduce left
				if (lengthLeft > 0)
				{
					for (; offset < length; offset++)
					{
						float v = x[offset].SquareAbs();
						result += v;
					}
				}
				innerResult = result;
			}
			else
			{
				// initialize
				ComplexMultiply<Conj>(x, y, out var aggregate1, out var aggregate2);
				// loop
				int lengthLeft = length - Vector256<float>.Count, offset = Vector256<float>.Count;
				while (lengthLeft >= Vector256<float>.Count) // Vector256<ComplexSingle>.Count * 2
				{
					ComplexMultiply<Conj>(x + offset, y + offset, out var current1, out var current2);
					aggregate1 = Avx.Add(aggregate1, current1);
					aggregate2 = Avx.Add(aggregate2, current2);
					lengthLeft -= Vector256<float>.Count;
					offset += Vector256<float>.Count;
				}
				// reduce main
				ComplexSingle result = 0;
				for (int i = 0; i < Vector256<float>.Count / 2; i += 2)
				{
					ComplexSingle v = ((ComplexSingle*)&aggregate1)[i] * ((ComplexSingle*)&aggregate1)[i + 1];
					result += v;
				}
				for (int i = 0; i < Vector256<float>.Count / 2; i++)
				{
					ComplexSingle v = ((ComplexSingle*)&aggregate2)[i] * ((ComplexSingle*)&aggregate2)[i + 1];
					result += v;
				}
				// reduce left
				if (lengthLeft > 0)
				{
					for (; offset < length; offset++)
					{
						ComplexSingle v1 = x[offset], v2 = y[offset];
						result += v1 * v2;
					}
				}
				innerResult = result;
			}
			return innerResult;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe ComplexDouble VectorInnerComplexDouble<Dot, Conj>(ComplexDouble* x, ComplexDouble* y, int length)
		{
			ComplexDouble innerResult;
			bool doDot = typeof(Dot) == typeof(bool);
			bool doCon = typeof(Conj) == typeof(bool);
			if (!doDot)
			{
				// initialize
				Vector256<double> aggregate = ComplexSquareAbs(x);
				// loop
				int lengthLeft = length - Vector256<double>.Count, offset = Vector256<double>.Count;
				while (lengthLeft >= Vector256<double>.Count) // Vector256<ComplexDouble>.Count * 2
				{
					Vector256<double> squares = ComplexSquareAbs(x + offset);
					aggregate = Avx.Add(aggregate, squares);
					lengthLeft -= Vector256<double>.Count;
					offset += Vector256<double>.Count;
				}
				// reduce main
				double result = ((double*)&aggregate)[0];
				for (int i = 1; i < Vector256<double>.Count; i++)
				{
					double v = ((double*)&aggregate)[i];
					result += v;
				}
				// reduce left
				if (lengthLeft > 0)
				{
					for (; offset < length; offset++)
					{
						double v = x[offset].SquareAbs();
						result += v;
					}
				}
				innerResult = result;
			}
			else
			{
				// initialize
				ComplexMultiply<Conj>(x, y, out var aggregate1, out var aggregate2);
				// loop
				int lengthLeft = length - Vector256<double>.Count, offset = Vector256<double>.Count;
				while (lengthLeft >= Vector256<double>.Count) // Vector256<ComplexDouble>.Count * 2
				{
					ComplexMultiply<Conj>(x + offset, y + offset, out var current1, out var current2);
					aggregate1 = Avx.Add(aggregate1, current1);
					aggregate2 = Avx.Add(aggregate2, current2);
					lengthLeft -= Vector256<double>.Count;
					offset += Vector256<double>.Count;
				}
				// reduce main
				ComplexDouble result = 0;
				for (int i = 0; i < Vector256<double>.Count / 2; i += 2)
				{
					ComplexDouble v = ((ComplexDouble*)&aggregate1)[i] * ((ComplexDouble*)&aggregate1)[i + 1];
					result += v;
				}
				for (int i = 0; i < Vector256<double>.Count / 2; i++)
				{
					ComplexDouble v = ((ComplexDouble*)&aggregate2)[i] * ((ComplexDouble*)&aggregate2)[i + 1];
					result += v;
				}
				// reduce left
				if (lengthLeft > 0)
				{
					for (; offset < length; offset++)
					{
						ComplexDouble v1 = x[offset], v2 = y[offset];
						result += v1 * v2;
					}
				}
				innerResult = result;
			}
			return innerResult;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static unsafe bool Inner<T, Dot>(bool conjX, Storage<T> x, Storage<T> y, out T dot) where T : unmanaged
		{
			dot = default;
			if (!GetSpan(x, out T* px, out int lenx))
				return false;
			if (!GetSpan(y, out T* py, out int leny))
				return false;
			int length = Math.Min(lenx, leny);
			if (length == 0)
				return true;

			if (!Vector.IsHardwareAccelerated || length <= (Vector<byte>.Count / sizeof(T) * 4) ||
				(Const<T>.IsComplex && (Const<T>.IsIntegralType || !Avx.IsSupported)))
			{ // no SIMD or too short
			  // no AVX's HorizontalAdd and Unpack (Vector<T> has not corresponding implementation yet)
				if (conjX)
					dot = VectorInnerManaged<T, Dot, bool>(px, py, length);
				else
					dot = VectorInnerManaged<T, Dot, byte>(px, py, length);
				return true;
			}

			if (Const<T>.IsComplex)
			{
				if (typeof(T) == typeof(float))
				{
					var xx = (ComplexSingle*)px; var yy = (ComplexSingle*)py;
					ComplexSingle temp;
					if (conjX)
						VectorInnerCompexSingle<Dot, bool>(xx, yy, length);
					else
						VectorInnerCompexSingle<Dot, byte>(xx, yy, length);
					dot = *(T*)&temp;
				}
				else // double
				{
					var xx = (ComplexDouble*)px; var yy = (ComplexDouble*)py;
					ComplexDouble temp;
					if (conjX)
						VectorInnerComplexDouble<Dot, bool>(xx, yy, length);
					else
						VectorInnerComplexDouble<Dot, byte>(xx, yy, length);
					dot = *(T*)&temp;
				}
			}
			else
			{
				dot = VectorInnerReal<T, Dot>(px, py, length);
			}
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static unsafe bool Dot<T>(bool conjX, Storage<T> x, Storage<T> y, out T dot) where T : unmanaged
		{
			return Inner<T, bool>(conjX, x, y, out dot);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static bool Norm<T>(Storage<T> x, out double norm) where T : unmanaged
		{
			norm = 0;
			if (!Inner<T, byte>(conjX: true, x, x, out T dot))
				return false;
			norm = dot.ToDouble();
			return true;
		}
		#endregion


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static bool PartialProduct<T>(Storage<T> x, Storage<T> y, bool inclusive) where T : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static bool PartialSum<T>(Storage<T> x, Storage<T> y, bool inclusive) where T : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static bool PointWiseAddScalar<T>(Storage<T> x, T scalr) where T : unmanaged
		{
			
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static bool PointWiseModulo<T>(Storage<T> x, T mod) where T : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static bool PointWiseCast<T, TOut>(Storage<T> source, Storage<TOut> destination) where T : unmanaged where TOut : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static bool PointWiseConjugate<T>(Storage<T> x) where T : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static bool PointWiseDivide<T>(Storage<T> x, Storage<T> y) where T : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static bool PointWiseEquals<T>(Storage<T> x, Storage<T> y, out bool equals) where T : unmanaged
		{
			equals = true;
			if (x == y)
				return true;

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static bool PointWiseMultiply<T>(Storage<T> x, Storage<T> y) where T : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static bool PointWisePower<T>(Storage<T> x, double p) where T : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static bool PointWisePower<T>(Storage<T> x, T p) where T : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static bool Scale<T>(Storage<T> x, T scalar) where T : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal new static bool TruncateArray<T>(Storage<T> x, double threshold) where T : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static bool VectorGeneralAdd<T>(T α, Storage<T> x, Storage<T> y) where T : unmanaged
		{

		}
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

		#region vector
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

		protected override bool TruncateArray_<T>(Storage<T> x, double threshold)
		{
			return TruncateArray(x, threshold);
		}

		protected override bool VectorGeneralAdd_<T>(T α, Storage<T> x, int strideX, Storage<T> y, int strideY)
		{
			if (strideX != 1 || strideY != 1)
				return false;
			return VectorGeneralAdd(α, x, y);
		}
		#endregion

		#region matrix related
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
		protected override bool LinearSolve_<T>(long n, long nrhs, Storage<T> A, long lda, Storage<T> B, long ldb) => false;
		protected override bool LuDecomposition_<T>(long n, Storage<T> A, long lda) => false;
		protected override bool MatrixCopyUpperLowerParts_<T>(bool storedUpper, bool hermitian, long n, Storage<T> A, long lda) => false;
		protected override bool MatrixKronecker_<T>(long ma, long na, long mb, long nb, T α, Storage<T> A, long lda, Storage<T> B, long ldb, T β, Storage<T> C, long ldc) => false;
		protected override bool QRDecomposition_<T>(bool full, long m, long n, Storage<T> A, long lda, Storage<T> Q, long ldq) => false;
		protected override bool RankKUpdate_<T>(bool fillUpper, MatrixOperation op, bool conjA, long n, long k, T α, Storage<T> A, long lda, T β, Storage<T> C, long ldc) => false;
		protected override bool SingularValues_<T, TReal>(SVDStore storeU, SVDStore storeV, long m, long n, Storage<T> A, long lda, Storage<TReal> S, Storage<T>? U, long ldu, Storage<T>? Vct, long ldvct) => false;
		protected override bool SymmHermMatrixMultiplyGeneral_<T>(bool fillUpper, bool leftA, bool hermA, long m, long n, T α, Storage<T> A, long lda, Storage<T> B, long ldb, T β, Storage<T> C, long ldc) => false;
		protected override bool SymmHermMatrixMultiplyVector_<T>(bool fillUpper, bool hermA, long n, T α, Storage<T> A, long lda, Storage<T> x, int strideX, T β, Storage<T> y, int strideY) => false;
		protected override bool SymmHermRankOneUpdate_<T>(bool fillUpper, bool conjX, long n, T α, Storage<T> x, int strideX, T β, Storage<T> A, long lda) => false;
		protected override bool TriangularMatrixSolve_<T>(bool leftA, bool fillUpper, bool unitDiag, MatrixOperation op, long m, long n, T α, Storage<T> A, long lda, Storage<T> B, long ldb) => false;
		#endregion
	}
#pragma warning restore CS1591 // 缺少对公共可见类型或成员的 XML 注释
}
