using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

using Althea.Helpers;
using Althea.LinearAlgebra;
using Althea.Linq;

using static Althea.Backend.CSharp.MemoryPointerChecker;


namespace Althea.Backend.CSharp.LinearAlgebra
{
	public unsafe partial class Api
	{
		#region vector argument (absolute) min / max
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static int VectorArgMinMaxReal<T, TInd, U, UInd, Test>(void* xx, int length) where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBaseNumber<TInd> where U : unmanaged, INumber<U> where UInd : unmanaged, INumber<UInd>
		{
			bool doMax = typeof(Test) == typeof(int) || typeof(Test) == typeof(long);
			bool doAbs = typeof(Test) == typeof(int) || typeof(Test) == typeof(uint);
			// maximize with stride == Vector<T>.Count
			// initial
			U* x = (U*)xx;
			Vector<U> extremes = LoadVector(x);
			if (doAbs)
				extremes = Vector.Abs(extremes);
			Vector<UInd> indices = new(stackalloc UInd[Vector<U>.Count].FillWithRange(UInd.Zero));
			Vector<UInd> extremeIndices = indices;
			Vector<UInd> increment = new(UInd.CreateSaturating(Vector<U>.Count));
			// loop
			U* end = x + length;
			while (x + Vector<U>.Count <= end)
			{
				indices += increment;
				Vector<UInd> compare;
				// JIT shall optimize the branches and type converts to some code as if they do not exist
				Vector<U> current = doAbs ? Vector.Abs(LoadVector(x)) : LoadVector(x);
				if (doMax)
				{   // abs max || max
					if (typeof(U) == typeof(float))
					{   // T is float and TInd is int
						Vector<int> temp = Vector.GreaterThan(*(Vector<float>*)&current, *(Vector<float>*)&extremes);
						compare = *(Vector<UInd>*)&temp;
					}
					else if (typeof(U) == typeof(double))
					{   // T is double and TInd is long
						Vector<long> temp = Vector.GreaterThan(*(Vector<double>*)&current, *(Vector<double>*)&extremes);
						compare = *(Vector<UInd>*)&temp;
					}
					else
					{   // T == TInd
						Vector<U> temp = Vector.GreaterThan(current, extremes);
						compare = *(Vector<UInd>*)&temp;
					}
					extremes = Vector.Max(current, extremes);
				}
				else
				{   // abs min || min
					if (typeof(U) == typeof(float))
					{   // T is float and TInd is int
						Vector<int> temp = Vector.LessThan(*(Vector<float>*)&current, *(Vector<float>*)&extremes);
						compare = *(Vector<UInd>*)&temp;
					}
					else if (typeof(U) == typeof(double))
					{   // T is double and TInd is long
						Vector<long> temp = Vector.LessThan(*(Vector<double>*)&current, *(Vector<double>*)&extremes);
						compare = *(Vector<UInd>*)&temp;
					}
					else
					{   // T == TInd
						Vector<U> temp = Vector.LessThan(current, extremes);
						compare = *(Vector<UInd>*)&temp;
					}
					extremes = Vector.Min(current, extremes);
				}
				extremeIndices = Vector.ConditionalSelect(compare, indices, extremeIndices);
				x += Vector<U>.Count;
			}
			// reduce main
			VectorArgMinMaxManaged<T, Test>((T*)&extremes, 1, Vector<U>.Count, out long extremeIndex);
			int index = (int)extremeIndex;
			// reduce left
			if (x < end)
			{
				VectorArgMinMaxManaged<T, Test>((T*)x, 1, (int)(end - x), out long restExtreme);
				int newIndex = (int)((T*)x - (T*)xx + restExtreme);
				if (doMax && x[newIndex] > extremes[index])
					index = newIndex;
				if (!doMax && x[newIndex] < extremes[index])
					index = newIndex;
			}
			return index;
		}

		//// Test == int, uint   for   AbsMax, AbsMin
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static int VectorArgMinMaxCompexSingle<Test>(Complex<Float32>* x, int length)
		{
			bool doMax = typeof(Test) == typeof(int);
			// initialize
			Vector256<float> extremes = ComplexSquareAbsNoOrderSingle(x);
			Vector256<int> indices = new Vector<int>(stackalloc int[] { 0, 1, 4, 5, 2, 3, 6, 7 }).AsVector256();
			Vector256<int> extremeIndices = indices;
			Vector256<int> increment = new Vector<int>(Vector256<int>.Count).AsVector256();
			// loop
			int lengthLeft = length - Vector256<float>.Count, offset = Vector256<float>.Count;
			while (lengthLeft >= Vector256<float>.Count) // Vector256<Complex<Float32>>.Count * 2
			{
				indices = Avx2.Add(indices, increment);
				Vector256<float> squares = ComplexSquareAbsNoOrderSingle(x + offset);
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
					var v = x[offset].Magnitude;
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
		private static long VectorArgMinMaxCompexDouble<Test>(Complex<Float64>* x, int length)
		{
			bool doMax = typeof(Test) == typeof(int);
			// initialize
			Vector256<double> extremes = ComplexSquareAbsNoOrderDouble(x);
			Vector256<long> indices = new Vector<long>(stackalloc long[] { 0, 2, 1, 3 }).AsVector256();
			Vector256<long> extremeIndices = indices;
			Vector256<long> increment = new Vector<long>(Vector256<long>.Count).AsVector256();
			// loop
			int lengthLeft = length - Vector256<double>.Count, offset = Vector256<double>.Count;
			while (lengthLeft >= Vector256<double>.Count) // Vector256<Complex<Float64>>.Count * 2
			{
				indices = Avx2.Add(indices, increment);
				Vector256<double> squares = ComplexSquareAbsNoOrderDouble(x + offset);
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
					double v = x[offset].MagnitudeSquared;
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
		private static bool VectorArgMinMaxManaged<T, Test>(T* x, int inc, int length, out long index) where T : unmanaged, IBaseNumber<T>
		{
			index = -1;
			bool doMax = typeof(Test) == typeof(int) || typeof(Test) == typeof(long);
			bool doAbs = typeof(Test) == typeof(int) || typeof(Test) == typeof(uint);
			if (doAbs)
			{
				T extreme = T.Abs(x[0]); int extremeIndex = 0;
				for (int i = 0, ix = 0; i < length; i++, ix += inc)
				{
					T v = T.Abs(x[ix]);
					if ((doMax && v > extreme) || (!doMax && v < extreme))
					{
						extreme = v; extremeIndex = i;
					}
				}
				index = extremeIndex;
			}
			else
			{
				T extreme = x[0]; int extremeIndex = 0;
				for (int i = 0, ix = 0; i < length; i++, ix += inc)
				{
					T v = x[ix];
					// some frequent type speedups, complex is not possible here
					if (doMax)
					{
						if (v > extreme)
						{
							extreme = v; extremeIndex = i;
						}
					}
					else
					{
						if (v < extreme)
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
		internal static bool ArgMinMax<T, TS, Test>(TS x, long strideX, out long index) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
		{
			index = -1;
			if ((typeof(Test) == typeof(long) || typeof(Test) == typeof(ulong)) && T.IsComplexType)
				throw new TypeMismatchException(typeof(T), TypeMismatchException.MismatchReason.NotReal);
			if (!GetPointer(x, strideX, out T* px, out int length, out int inc))
				return false;
			if (length == 0)
				return true;
			if (length == 1)
			{
				index = 0; return true;
			}
			if (inc != 1 || !Vector.IsHardwareAccelerated || length <= (Vector<byte>.Count / sizeof(T) * 4))
				return VectorArgMinMaxManaged<T, Test>(px, inc, length, out index); // no SIMD or too short
			if ((sizeof(T) <= sizeof(byte) && length > sbyte.MaxValue) || (sizeof(T) <= sizeof(short) && length > short.MaxValue))
				return VectorArgMinMaxManaged<T, Test>(px, inc, length, out index);

			if (T.IsComplexType)
			{
				if (T.Type.IsInteger() || !Avx2.IsSupported)
					return VectorArgMinMaxManaged<T, Test>(px, inc, length, out index);
				index = typeof(T) == typeof(Complex<Float32>) || typeof(T) == typeof(Complex<Float32>)
					? VectorArgMinMaxCompexSingle<Test>((Complex<Float32>*)px, length)
					: VectorArgMinMaxCompexDouble<Test>((Complex<Float64>*)px, length);
			}
			else
			{
				delegate*<void*, int, int> func = default(T) switch
				{
					Float64 => &VectorArgMinMaxReal<Float64, SignedInt64, double, long, Test>,
					Float32 => &VectorArgMinMaxReal<Float32, SignedInt32, float, int, Test>,
					SignedInt8 => &VectorArgMinMaxReal<SignedInt8, SignedInt8, sbyte, sbyte, Test>,
					SignedInt16 => &VectorArgMinMaxReal<SignedInt16, SignedInt16, short, short, Test>,
					SignedInt32 => &VectorArgMinMaxReal<SignedInt32, SignedInt32, int, int, Test>,
					SignedInt64 => &VectorArgMinMaxReal<SignedInt64, SignedInt64, long, long, Test>,
					UnsignedInt8 => &VectorArgMinMaxReal<UnsignedInt8, UnsignedInt8, byte, byte, Test>,
					UnsignedInt16 => &VectorArgMinMaxReal<UnsignedInt16, UnsignedInt16, ushort, ushort, Test>,
					UnsignedInt32 => &VectorArgMinMaxReal<UnsignedInt32, UnsignedInt32, uint, uint, Test>,
					UnsignedInt64 => &VectorArgMinMaxReal<UnsignedInt64, UnsignedInt64, ulong, ulong, Test>,
					_ => null,
				};
				index = func(px, length);
			}
			return true;
		}

		public virtual partial bool AbsoluteValueArgMax<T, TS>(TS x, long strideX, out long index) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS> => ArgMinMax<T, TS, int>(x, strideX, out index);

		public virtual partial bool AbsoluteValueArgMin<T, TS>(TS x, long strideX, out long index) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS> => ArgMinMax<T, TS, uint>(x, strideX, out index);
		#endregion


		#region vector (absolute) min / max
		//// Test == int, uint, long, ulong   for   AbsMax, AbsMin, Max, Min
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void VectorMinMaxReal<T, U, Test>(void* xx, int length, void* result) where T : unmanaged, IBaseNumber<T> where U : unmanaged, INumber<U>
		{
			bool doMax = typeof(Test) == typeof(int) || typeof(Test) == typeof(long);
			bool doAbs = typeof(Test) == typeof(int) || typeof(Test) == typeof(uint);
			// initial
			U* x = (U*)xx;
			Vector<U> extremes = LoadVector(x);
			if (doAbs)
				extremes = Vector.Abs(extremes);
			// loop
			int lengthLeft = length - Vector<U>.Count, offset = Vector<U>.Count;
			while (lengthLeft >= Vector<U>.Count)
			{
				Vector<U> current = LoadVector(x + offset);
				if (doAbs)
					current = Vector.Abs(current);
				extremes = doMax ? Vector.Max(current, extremes) : Vector.Min(current, extremes);
				lengthLeft -= Vector<U>.Count;
				offset += Vector<U>.Count;
			}
			// reduce main
			VectorMinMaxManaged<T, Test>((T*)&extremes, 1, Vector<U>.Count, out T extreme);
			// reduce left
			if (lengthLeft > 0)
			{
				VectorMinMaxManaged<T, Test>((T*)x + offset, 1, lengthLeft, out T restExtreme);
				if (doMax && restExtreme > extreme)
					extreme = restExtreme;
				if (!doMax && restExtreme < extreme)
					extreme = restExtreme;
			}
			*(T*)result = extreme;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Complex<Float32> VectorAbsMinMaxCompexSingle<Test>(Complex<Float32>* x, int length)
		{
			bool doMax = typeof(Test) == typeof(int);
			// initialize
			Vector256<float> extremes = ComplexSquareAbsNoOrderSingle(x);
			// loop
			int lengthLeft = length - Vector256<float>.Count, offset = Vector256<float>.Count;
			while (lengthLeft >= Vector256<float>.Count) // Vector256<Complex<Float32>>.Count * 2
			{
				Vector256<float> squares = ComplexSquareAbsNoOrderSingle(x + offset);
				extremes = doMax ? Avx.Max(squares, extremes) : Avx.Max(squares, extremes);
				lengthLeft -= Vector256<float>.Count;
				offset += Vector256<float>.Count;
			}
			// reduce main
			float extreme = ((float*)&extremes)[0];
			for (int i = 1; i < Vector256<float>.Count; i++)
			{
				float v = ((float*)&extremes)[i];
				if (doMax && v > extreme)
				{
					extreme = v;
				}
				if (!doMax && v < extreme)
				{
					extreme = v;
				}
			}
			// reduce left
			if (lengthLeft > 0)
			{
				for (; offset < length; offset++)
				{
					var v = x[offset].Magnitude;
					if (doMax && v > extreme)
					{
						extreme = v;
					}
					if (!doMax && v < extreme)
					{
						extreme = v;
					}
				}
			}
			return (Float32)extreme;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Complex<Float64> VectorAbsMinMaxCompexDouble<Test>(Complex<Float64>* x, int length)
		{
			bool doMax = typeof(Test) == typeof(int);
			// initialize
			Vector256<double> extremes = ComplexSquareAbsNoOrderDouble(x);
			// loop
			int lengthLeft = length - Vector256<double>.Count, offset = Vector256<double>.Count;
			while (lengthLeft >= Vector256<double>.Count) // Vector256<Complex<Float64>>.Count * 2
			{
				Vector256<double> squares = ComplexSquareAbsNoOrderDouble(x + offset);
				extremes = doMax ? Avx.Max(squares, extremes) : Avx.Max(squares, extremes);
				lengthLeft -= Vector256<double>.Count;
				offset += Vector256<double>.Count;
			}
			// reduce main
			double extreme = ((double*)&extremes)[0];
			for (int i = 1; i < Vector256<double>.Count; i++)
			{
				double v = ((double*)&extremes)[i];
				if (doMax && v > extreme)
				{
					extreme = v;
				}
				if (!doMax && v < extreme)
				{
					extreme = v;
				}
			}
			// reduce left
			if (lengthLeft > 0)
			{
				for (; offset < length; offset++)
				{
					var v = x[offset].Magnitude;
					if (doMax && v > extreme)
					{
						extreme = v;
					}
					if (!doMax && v < extreme)
					{
						extreme = v;
					}
				}
			}
			return (Float64)extreme;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool VectorMinMaxManaged<T, Test>(T* x, int inc, int length, out T extreme) where T : unmanaged, IBaseNumber<T>
		{
			bool doMax = typeof(Test) == typeof(int) || typeof(Test) == typeof(long);
			bool doAbs = typeof(Test) == typeof(int) || typeof(Test) == typeof(uint);
			if (doAbs)
			{
				extreme = x[0];
				for (int i = 0, ix = 0; i < length; i++, ix += inc)
				{
					T v = T.Abs(x[ix]);
					if ((doMax && v > extreme) || (!doMax && v < extreme))
					{
						extreme = v;
					}
				}
			}
			else
			{
				extreme = x[0];
				for (int i = 0, ix = 0; i < length; i++, ix += inc)
				{
					T v = x[ix];
					// some frequent type speedups, complex is not possible here
					if (doMax)
					{
						if (v > extreme)
						{
							extreme = v;
						}
					}
					else
					{
						if (v < extreme)
						{
							extreme = v;
						}
					}
				}
			}
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool MinMax<T, TS, Test>(TS x, long strideX, out T extreme) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
		{
			extreme = default;
			if ((typeof(Test) == typeof(long) || typeof(Test) == typeof(ulong)) && T.IsComplexType)
				throw new TypeMismatchException(typeof(T), TypeMismatchException.MismatchReason.NotReal);
			if (!GetPointer(x, strideX, out T* px, out int length, out int inc))
				return false;
			if (length == 0)
				return true;
			if (length == 1)
			{
				extreme = px[0]; return true;
			}
			if (inc != 1 || !Vector.IsHardwareAccelerated || length <= (Vector<byte>.Count / sizeof(T) * 4))
				return VectorMinMaxManaged<T, Test>(px, inc, length, out extreme); // no SIMD or too short
			if ((sizeof(T) <= sizeof(byte) && length > sbyte.MaxValue) || (sizeof(T) <= sizeof(short) && length > short.MaxValue))
				return VectorMinMaxManaged<T, Test>(px, inc, length, out extreme);

			if (T.IsComplexType)
			{
				if (T.Type.IsInteger() || !Avx2.IsSupported)
					return VectorMinMaxManaged<T, Test>(px, inc, length, out extreme);
				if (typeof(T) == typeof(Complex<Float32>) || typeof(T) == typeof(Complex<Float32>))
				{
					var temp = VectorAbsMinMaxCompexSingle<Test>((Complex<Float32>*)px, length);
					extreme = *(T*)&temp;
				}
				else // double
				{
					var temp = VectorAbsMinMaxCompexDouble<Test>((Complex<Float64>*)px, length);
					extreme = *(T*)&temp;
				}
			}
			else
			{
				delegate*<void*, int, void*, void> func = default(T) switch
				{
					Float64 => &VectorMinMaxReal<Float64, double, Test>,
					Float32 => &VectorMinMaxReal<Float32, float, Test>,
					SignedInt8 => &VectorMinMaxReal<SignedInt8, sbyte, Test>,
					SignedInt16 => &VectorMinMaxReal<SignedInt16, short, Test>,
					SignedInt32 => &VectorMinMaxReal<SignedInt32, int, Test>,
					SignedInt64 => &VectorMinMaxReal<SignedInt64, long, Test>,
					UnsignedInt8 => &VectorMinMaxReal<UnsignedInt8, byte, Test>,
					UnsignedInt16 => &VectorMinMaxReal<UnsignedInt16, ushort, Test>,
					UnsignedInt32 => &VectorMinMaxReal<UnsignedInt32, uint, Test>,
					UnsignedInt64 => &VectorMinMaxReal<UnsignedInt64, ulong, Test>,
					_ => null,
				};
				T ex = default;
				func(px, length, &ex);
				extreme = ex;
			}
			return true;
		}
		#endregion


		#region inner
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static T VectorInnerManaged<T, Dot, Conj>(T* x, int incx, T* y, int incy, int length) where T : unmanaged, IBaseNumber<T>
		{
			T result = default;
			bool doDot = typeof(Dot) == typeof(bool);
			bool doCon = typeof(Conj) == typeof(bool);

			for (int i = 0, ix = 0, iy = 0; i < length; i++, ix += incx, iy += incy)
			{
				T a = x[ix], b;
				// float type FMA acceleration
				if (typeof(T) == typeof(float))
				{
					float vv = doDot
						? MathF.FusedMultiplyAdd(*(float*)&a, *(float*)&b, *(float*)&result)
						: MathF.FusedMultiplyAdd(*(float*)&a, *(float*)&a, *(float*)&result);
					result = *(T*)&vv;
					continue;
				}
				if (typeof(T) == typeof(double))
				{
					double vv = doDot
						? Math.FusedMultiplyAdd(*(double*)&a, *(double*)&b, *(double*)&result)
						: Math.FusedMultiplyAdd(*(double*)&a, *(double*)&a, *(double*)&result);
					result = *(T*)&vv;
					continue;
				}
				if (typeof(T) == typeof(Complex<Float32>))
				{
					Complex<Float32> vv = doDot && doCon
						? Complex<Float32>.FusedMultiplyAdd((*(Complex<Float32>*)&a).Conjugate, *(Complex<Float32>*)&b, *(Complex<Float32>*)&result)
						: doDot && !doCon
						? Complex<Float32>.FusedMultiplyAdd(*(Complex<Float32>*)&a, *(Complex<Float32>*)&b, *(Complex<Float32>*)&result)
						: (*(Complex<Float32>*)&result) + (*(Complex<Float32>*)&a).MagnitudeSquared;
					result = *(T*)&vv;
					continue;
				}
				if (typeof(T) == typeof(Complex<Float64>))
				{
					Complex<Float64> vv = doDot && doCon
						? Complex<Float64>.FusedMultiplyAdd((*(Complex<Float64>*)&a).Conjugate, *(Complex<Float64>*)&b, *(Complex<Float64>*)&result)
						: doDot && !doCon
						? Complex<Float64>.FusedMultiplyAdd(*(Complex<Float64>*)&a, *(Complex<Float64>*)&b, *(Complex<Float64>*)&result)
						: (*(Complex<Float64>*)&result) + (*(Complex<Float64>*)&a).MagnitudeSquared;
					result = *(T*)&vv;
					continue;
				}
				// normal case
				if (doDot)
				{
					b = y[iy];
					result = doCon ? result + T.Conjugate(a) * b : result + a * b;
				}
				else
				{
					result += a * a;
				}
			}
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void VectorInnerReal<T, U, Dot>(void* xx, void* yy, int length, void* result) where T : unmanaged, IBaseNumber<T> where U : unmanaged, INumber<U>
		{
			bool doDot = typeof(Dot) == typeof(bool);
			U* x = (U*)xx, y = (U*)yy;
			// reduce to Vector<T>.Count sums
			Vector<U> sum = Vector<U>.Zero;
			int lengthLeft = length, offset = 0;
			while (lengthLeft >= Vector<U>.Count)
			{
				Vector<U> a = LoadVector(x + offset), b = LoadVector(y + offset);
				if (!doDot)
					b = a;
				sum += a * b;
				lengthLeft -= Vector<U>.Count;
				offset += Vector<U>.Count;
			}
			// reduce left
			U dotLeft = default;
			if (lengthLeft > 0)
			{
				Vector<U> leftA = Vector<U>.Zero, leftB = Vector<U>.Zero;
				// the following two lines shall be unrolled by JIT at runtime
				Buffer.MemoryCopy( x + offset, &leftA,lengthLeft * sizeof(T), lengthLeft * sizeof(T));
				Buffer.MemoryCopy(y + offset, &leftB, lengthLeft * sizeof(T), lengthLeft * sizeof(T));
				dotLeft = Vector.Dot(leftA, leftB);
				// this implementation has some performance loss compare to the direct dot
				// but it is suitable for all generic type T that Vector<U> supports
			}
			// this implementation has some performance loss, same reason as above
			U dotMain = dotLeft + Vector.Sum(sum);
			// return
			*(T*)result = *(T*)&dotMain;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Complex<Float32> VectorInnerCompex<Dot, Conj>(Complex<Float32>* x, Complex<Float32>* y, int length)
		{
			Complex<Float32> innerResult;
			bool doDot = typeof(Dot) == typeof(bool);
			bool doConj = typeof(Conj) == typeof(bool);
			if (!doDot)
			{
				// initialize
				Vector256<float> aggregate = ComplexSquareAbsNoOrderSingle(x);
				// loop
				int lengthLeft = length - Vector256<float>.Count, offset = Vector256<float>.Count;
				while (lengthLeft >= Vector256<float>.Count) // Vector256<Complex<Float32>>.Count * 2
				{
					Vector256<float> squares = ComplexSquareAbsNoOrderSingle(x + offset);
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
						float v = x[offset].MagnitudeSquared;
						result += v;
					}
				}
				innerResult = (Float32)result;
			}
			else
			{
				// initialize
				Vector256<float> aggregateReal = Vector<float>.Zero.AsVector256(), aggregateImag = Vector<float>.Zero.AsVector256();
				ComplexMultiplyAddSingle<Conj>(x, y, ref aggregateReal, ref aggregateImag);
				// loop
				int lengthLeft = length - Vector256<float>.Count, offset = Vector256<float>.Count;
				while (lengthLeft >= Vector256<float>.Count) // Vector256<Complex<Float32>>.Count * 2
				{
					ComplexMultiplyAddSingle<Conj>(x + offset, y + offset, ref aggregateReal, ref aggregateImag);
					lengthLeft -= Vector256<float>.Count;
					offset += Vector256<float>.Count;
				}
				// reduce main
				float* reals = (float*)&aggregateReal, imags = (float*)&aggregateImag;
				Complex<Float32> result = new(reals[0], imags[0]);
				for (int i = 1; i < Vector256<float>.Count; i++)
				{
					Complex<Float32> v = new(reals[i], imags[i]);
					result += v;
				}
				// reduce left
				if (lengthLeft > 0)
				{
					for (; offset < length; offset++)
					{
						Complex<Float32> v1 = x[offset], v2 = y[offset];
						result += v1 * v2;
					}
				}
				innerResult = result;
			}
			return innerResult;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Complex<Float64> VectorInnerComplex<Dot, Conj>(Complex<Float64>* x, Complex<Float64>* y, int length)
		{
			Complex<Float64> innerResult;
			bool doDot = typeof(Dot) == typeof(bool);
			if (!doDot)
			{
				// initialize
				Vector256<double> aggregate = ComplexSquareAbsNoOrderDouble(x);
				// loop
				int lengthLeft = length - Vector256<double>.Count, offset = Vector256<double>.Count;
				while (lengthLeft >= Vector256<double>.Count) // Vector256<Complex<Float64>>.Count * 2
				{
					Vector256<double> squares = ComplexSquareAbsNoOrderDouble(x + offset);
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
						double v = x[offset].MagnitudeSquared;
						result += v;
					}
				}
				innerResult = (Float64)result;
			}
			else
			{
				// initialize
				Vector256<double> aggregateReal = Vector<double>.Zero.AsVector256(), aggregateImag = Vector<double>.Zero.AsVector256();
				ComplexMultiplyAddDouble<Conj>(x, y, ref aggregateReal, ref aggregateImag);
				// loop
				int lengthLeft = length - Vector256<double>.Count, offset = Vector256<double>.Count;
				while (lengthLeft >= Vector256<double>.Count) // Vector256<Complex<Float32>>.Count * 2
				{
					ComplexMultiplyAddDouble<Conj>(x + offset, y + offset, ref aggregateReal, ref aggregateImag);
					lengthLeft -= Vector256<double>.Count;
					offset += Vector256<double>.Count;
				}
				// reduce main
				double* reals = (double*)&aggregateReal, imags = (double*)&aggregateImag;
				Complex<Float64> result = new(reals[0], imags[0]);
				for (int i = 1; i < Vector256<double>.Count; i++)
				{
					Complex<Float64> v = new(reals[i], imags[i]);
					result += v;
				}
				// reduce left
				if (lengthLeft > 0)
				{
					for (; offset < length; offset++)
					{
						Complex<Float64> v1 = x[offset], v2 = y[offset];
						result += v1 * v2;
					}
				}
				innerResult = result;
			}
			return innerResult;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool Inner<T, Dot>(bool conjX, T* px, int incx, T* py, int incy, int length, out T dot) where T : unmanaged, IBaseNumber<T>
		{
			if (incx != 1 || incy != 1 || !Vector.IsHardwareAccelerated || length <= (Vector<byte>.Count / sizeof(T) * 4) ||
				(T.IsComplexType && (T.Type.IsInteger() || !Avx.IsSupported)))
			{   // no SIMD or too short
				// no AVX's HorizontalAdd and Unpack (Vector<T> has not corresponding implementation yet)
				dot = conjX ? VectorInnerManaged<T, Dot, bool>(px, incx, py, incy, length) : VectorInnerManaged<T, Dot, byte>(px, incx, py, incy, length);
				return true;
			}

			if (T.IsComplexType)
			{
				if (typeof(T) == typeof(Complex<Float32>))
				{
					var xx = (Complex<Float32>*)px; var yy = (Complex<Float32>*)py;
					Complex<Float32> temp;
					if (conjX)
						VectorInnerCompex<Dot, bool>(xx, yy, length);
					else
						VectorInnerCompex<Dot, byte>(xx, yy, length);
					dot = *(T*)&temp;
				}
				else if (typeof(T) == typeof(Complex<Float64>))
				{
					var xx = (Complex<Float64>*)px; var yy = (Complex<Float64>*)py;
					Complex<Float64> temp;
					if (conjX)
						VectorInnerComplex<Dot, bool>(xx, yy, length);
					else
						VectorInnerComplex<Dot, byte>(xx, yy, length);
					dot = *(T*)&temp;
				}
				else
				{
					dot = default;
					return false;
				}
			}
			else
			{
				delegate*<void*, void*, int, void*, void> func = default(T) switch
				{
					Float64 => &VectorInnerReal<Float64, double, Dot>,
					Float32 => &VectorInnerReal<Float32, float, Dot>,
					SignedInt8 => &VectorInnerReal<SignedInt8, sbyte, Dot>,
					SignedInt16 => &VectorInnerReal<SignedInt16, short, Dot>,
					SignedInt32 => &VectorInnerReal<SignedInt32, int, Dot>,
					SignedInt64 => &VectorInnerReal<SignedInt64, long, Dot>,
					UnsignedInt8 => &VectorInnerReal<UnsignedInt8, byte, Dot>,
					UnsignedInt16 => &VectorInnerReal<UnsignedInt16, ushort, Dot>,
					UnsignedInt32 => &VectorInnerReal<UnsignedInt32, uint, Dot>,
					UnsignedInt64 => &VectorInnerReal<UnsignedInt64, ulong, Dot>,
					_ => null,
				};
				T d = default;
				func(px, py, length, &d);
				dot = d;
			}
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool Inner<T, TS1, TS2, Dot>(bool conjX, TS1 x, long strideX, TS2 y, long strideY, out T dot) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
		{
			dot = default;
			if (!GetPointer(x, strideX, out T* px, out int lenx, out int incx))
				return false;
			if (!GetPointer(y, strideY, out T* py, out int leny, out int incy))
				return false;
			int length = Math.Min(lenx, leny);
			if (length == 0)
				return true;
			return Inner<T, Dot>(conjX, px, incx, py, incy, length, out dot);
		}

		public virtual partial bool Norm<T, TS>(TS x, long strideX, out T norm) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
		{
			norm = default;
			if (!Inner<T, TS, TS, byte>(true, x, strideX, x, strideX, out T dot))
				return false;
			norm = Math.Sqrt(dot.AsDouble()).As<T>();
			return true;
		}

		public virtual partial bool Dot<T, TS1, TS2>(bool conjX, TS1 x, long strideX, TS2 y, long strideY, out T dot) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> => Inner<T, TS1, TS2, bool>(conjX, x, strideX, y, strideY, out dot);
		#endregion


		#region (absolute) sum product
		// Ignore spelling: uint ulong
		//// Test == int, uint, long, ulong   for   AbsSum, AbsProd, Sum, Prod
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool VectorAbsSumProdManaged<T, Test>(T* x, int inc, int length, out T aggregate) where T : unmanaged, IBaseNumber<T>
		{
			aggregate = T.Zero;
			bool doSum = typeof(Test) == typeof(int) || typeof(Test) == typeof(long);
			bool doAbs = typeof(Test) == typeof(int) || typeof(Test) == typeof(uint);
			if (doAbs)
			{
				aggregate = doSum ? T.Zero : T.One;
				for (int i = 0, ix = 0; i < length; i++, ix += inc)
				{
					T v = T.Abs(x[ix]);
					aggregate = doSum ? aggregate + v : aggregate * v;
				}
			}
			else
			{
				aggregate = doSum ? T.Zero : T.One;
				for (int i = 0, ix = 0; i < length; i++, ix += inc)
				{
					T v = x[ix];
					aggregate = doSum ? aggregate + v : aggregate * v;
				}
			}
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void VectorAbsSumProdReal<T, Test>(T* x, int length, out T aggregation) where T : unmanaged, IBaseNumber<T>
		{
			aggregation = T.Zero;
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
				VectorAbsSumProdManaged<T, Test>(x + offset, 1, lengthLeft, out aggregation);
			}
			else if (!doSum)
			{
				aggregation = T.One;
			}
			// return
			if (doSum)
			{
				aggregation += Vector.Dot(aggregate, Vector<T>.One);
			}
			else
			{
				T result = aggregate[0];
				for (int i = 1; i < Vector<T>.Count; i++)
				{
					result *= aggregate[i];
				}
				aggregation *= result;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void VectorAbsSumProdCompexSingle<Test>(Complex<Float32>* x, int length, out Complex<Float32> aggregation)
		{
			aggregation = default;
			bool doSum = typeof(Test) == typeof(int) || typeof(Test) == typeof(long);
			bool doAbs = typeof(Test) == typeof(int) || typeof(Test) == typeof(uint);
			if (doAbs)
			{
				// initialize
				Vector256<float> aggregate = ComplexSquareAbsNoOrderSingle(x);
				if (doSum)
					aggregate = Avx.Sqrt(aggregate);
				// loop
				int lengthLeft = length - Vector256<float>.Count, offset = Vector256<float>.Count;
				while (lengthLeft >= Vector256<float>.Count) // Vector256<Complex<Float32>>.Count * 2
				{
					Vector256<float> squares = ComplexSquareAbsNoOrderSingle(x + offset);
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
						float v = x[offset].Magnitude;
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
				aggregation = (Float32)result;
			}
			else
			{
				// initialize
				Vector256<float> aggregate1 = LoadVector256<float>(x), aggregate2 = LoadVector256<float>(x + Vector256<float>.Count / 2);
				// loop
				int lengthLeft = length - Vector256<float>.Count, offset = Vector256<float>.Count;
				while (lengthLeft >= Vector256<float>.Count) // Vector256<Complex<Float32>>.Count * 2
				{
					Vector256<float> current1 = LoadVector256<float>(x + offset);
					Vector256<float> current2 = LoadVector256<float>(x + offset + Vector256<float>.Count / 2);
					if (doSum)
					{
						aggregate1 = Avx.Add(aggregate1, current1);
						aggregate2 = Avx.Add(aggregate2, current2);
					}
					else
					{   // aggregate1 = reals, aggregate2 = imaginaries
						ComplexMultiply<byte, byte>(aggregate1, aggregate2, current1, current2, out aggregate1, out aggregate2);
					}
					lengthLeft -= Vector256<float>.Count;
					offset += Vector256<float>.Count;
				}
				// reduce main
				Complex<Float32> result;
				if (doSum)
				{
					result = ((Complex<Float32>*)&aggregate1)[0];
					for (int i = 1; i < Vector256<float>.Count / 2; i++)
					{
						result += ((Complex<Float32>*)&aggregate1)[i];
					}
					for (int i = 0; i < Vector256<float>.Count / 2; i++)
					{
						result += ((Complex<Float32>*)&aggregate2)[i];
					}
				}
				else
				{
					result = new(*(float*)&aggregate1, *(float*)&aggregate2);
					for (int i = 1; i < Vector256<float>.Count / 2; i++)
					{
						result *= new Complex<Float32>(((float*)&aggregate1)[i], ((float*)&aggregate2)[i]);
					}
				}
				// reduce left
				if (lengthLeft > 0)
				{
					for (; offset < length; offset++)
					{
						Complex<Float32> v = x[offset];
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
				aggregation = result;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void VectorAbsSumProdCompexDouble<Test>(Complex<Float64>* x, int length, out Complex<Float64> aggregation)
		{
			aggregation = default;
			bool doSum = typeof(Test) == typeof(int) || typeof(Test) == typeof(long);
			bool doAbs = typeof(Test) == typeof(int) || typeof(Test) == typeof(uint);
			if (doAbs)
			{
				// initialize
				Vector256<double> aggregate = ComplexSquareAbsNoOrderDouble(x);
				if (doSum)
					aggregate = Avx.Sqrt(aggregate);
				// loop
				int lengthLeft = length - Vector256<double>.Count, offset = Vector256<double>.Count;
				while (lengthLeft >= Vector256<double>.Count) // Vector256<Complex<Float64>>.Count * 2
				{
					Vector256<double> squares = ComplexSquareAbsNoOrderDouble(x + offset);
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
						double v = x[offset].Magnitude;
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
				aggregation = (Float64)result;
			}
			else
			{
				// initialize
				Vector256<double> aggregate1 = LoadVector256<double>(x), aggregate2 = LoadVector256<double>(x + Vector256<double>.Count / 2);
				// loop
				int lengthLeft = length - Vector256<double>.Count, offset = Vector256<double>.Count;
				while (lengthLeft >= Vector256<double>.Count) // Vector256<Complex<Float64>>.Count * 2
				{
					Vector256<double> current1 = LoadVector256<double>(x + offset);
					Vector256<double> current2 = LoadVector256<double>(x + offset + Vector256<double>.Count / 2);
					if (doSum)
					{
						aggregate1 = Avx.Add(aggregate1, current1);
						aggregate2 = Avx.Add(aggregate2, current2);
					}
					else
					{   // aggregate1 = reals, aggregate2 = imaginaries
						ComplexMultiply<byte, byte>(aggregate1, aggregate2, current1, current2, out aggregate1, out aggregate2);
					}
					lengthLeft -= Vector256<double>.Count;
					offset += Vector256<double>.Count;
				}
				// reduce main
				Complex<Float64> result;
				if (doSum)
				{
					result = ((Complex<Float64>*)&aggregate1)[0];
					for (int i = 1; i < Vector256<double>.Count / 2; i++)
					{
						result += ((Complex<Float64>*)&aggregate1)[i];
					}
					for (int i = 0; i < Vector256<double>.Count / 2; i++)
					{
						result += ((Complex<Float64>*)&aggregate2)[i];
					}
				}
				else
				{
					result = new(*(double*)&aggregate1, *(double*)&aggregate2);
					for (int i = 1; i < Vector256<double>.Count / 2; i++)
					{
						result *= new Complex<Float64>(((double*)&aggregate1)[i], ((double*)&aggregate2)[i]);
					}
				}
				// reduce left
				if (lengthLeft > 0)
				{
					for (; offset < length; offset++)
					{
						Complex<Float64> v = x[offset];
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
				aggregation = result;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool AbsSumProd<T, TS, Test>(TS x, long stride, out T aggregate) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
		{
			aggregate = T.Zero;
			if (!GetPointer(x, stride, out T* px, out int length, out int inc))
				return false;
			if (length == 0)
			{
				if (typeof(Test) == typeof(uint) || typeof(Test) == typeof(ulong))
				{
					aggregate = T.One;
				}
				return true;
			}
			if (length == 1)
			{
				aggregate = px[0]; aggregate = T.Abs(aggregate);
				return true;
			}
			if (inc != 1 || !Vector.IsHardwareAccelerated || length <= (Vector<byte>.Count / sizeof(T) * 4)) // no SIMD or too short
				return VectorAbsSumProdManaged<T, Test>(px, inc, length, out aggregate);

			if (T.IsComplexType)
			{
				if (T.Type.IsInteger() || !Avx.IsSupported) // no AVX's HorizontalAdd and Unpack (Vector<T> has not corresponding implementation yet)
					return VectorAbsSumProdManaged<T, Test>(px, inc, length, out aggregate);
				if (typeof(T) == typeof(Complex<Float32>) || typeof(T) == typeof(Complex<Float32>))
				{
					VectorAbsSumProdCompexSingle<Test>((Complex<Float32>*)px, length, out var temp);
					aggregate = *(T*)&temp;
				}
				else // double
				{
					VectorAbsSumProdCompexDouble<Test>((Complex<Float64>*)px, length, out var temp);
					aggregate = *(T*)&temp;
				}
			}
			else
			{
				VectorAbsSumProdReal<T, Test>(px, length, out aggregate);
			}
			return true;
		}

		public virtual partial bool AbsoluteValueSum<T, TS>(TS x, long strideX, out T sum) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS> => AbsSumProd<T, TS, int>(x, strideX, out sum);
		#endregion


		#region partial sum product
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void VectorParSumProdManaged<T, Sum, HasPre>(T* x, int incx, T* y, int incy, int length) where T : unmanaged, IBaseNumber<T>
		{
			bool doSum = typeof(Sum) == typeof(bool);
			bool hasPre = typeof(HasPre) == typeof(bool);

			T result = hasPre ? y[-incy] : doSum ? default : T.One;
			for (int i = 0, ix = 0, iy = 0; i < length; i++, ix += incx, iy += incy)
			{
				T v = x[ix];
				y[iy] = doSum ? result + v : result * v;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool ParSumProd<T, TS1, TS2, Sum>(TS1 x, long strideX, TS2 y, long strideY, bool inclusive) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
		{
			if (!GetPointer(x, strideX, out T* px, out int lenx, out int incx))
				return false;
			if (!GetPointer(y, strideY, out T* py, out int leny, out int incy))
				return false;
			int length = Math.Min(lenx, leny);
			// shortcuts
			if (length == 0)
				return true;
			py[0] = inclusive ? px[0] : typeof(Sum) == typeof(bool) ? default : T.One;
			if (length == 1)
				return true;
			// normal case
			if (!inclusive)
			{
				py++; length--;
			}
			VectorParSumProdManaged<T, Sum, byte>(px, incx, py, incy, length);
			return true;
		}
		#endregion


		public virtual partial bool GeneralVectorReduce<T, TS>(ReduceOperation op, TS x, long strideX, out T result) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
		{
			result = default;
			return (op) switch
			{
				ReduceOperation.Add => AbsSumProd<T, TS, long>(x, strideX, out result),
				ReduceOperation.AddAbsolute => AbsSumProd<T, TS, int>(x, strideX, out result),
				ReduceOperation.Multiply => AbsSumProd<T, TS, ulong>(x, strideX, out result),
				ReduceOperation.MultiplyAbsolute => AbsSumProd<T, TS, uint>(x, strideX, out result),
				ReduceOperation.Norm => this.Norm(x, strideX, out result),
				ReduceOperation.Maximum => MinMax<T, TS, long>(x, strideX, out result),
				ReduceOperation.Mininum => MinMax<T, TS, ulong>(x, strideX, out result),
				ReduceOperation.AbsoluteMaximum => MinMax<T, TS, int>(x, strideX, out result),
				ReduceOperation.AbsoluteMininum => MinMax<T, TS, uint>(x, strideX, out result),
				_ => false
			};
		}

		public virtual partial bool GeneralVectorArgReduce<T, TS>(ReduceOperation op, TS x, long strideX, out long index) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
		{
			index = default;
			return (op) switch
			{
				ReduceOperation.Add or ReduceOperation.AddAbsolute or ReduceOperation.Multiply or ReduceOperation.MultiplyAbsolute or ReduceOperation.Norm => throw new ArgumentOutOfRangeException(nameof(op), op, Resources.ParameterError.InvalidValue),
				ReduceOperation.Maximum => ArgMinMax<T, TS, long>(x, strideX, out index),
				ReduceOperation.Mininum => ArgMinMax<T, TS, ulong>(x, strideX, out index),
				ReduceOperation.AbsoluteMaximum => ArgMinMax<T, TS, int>(x, strideX, out index),
				ReduceOperation.AbsoluteMininum => ArgMinMax<T, TS, uint>(x, strideX, out index),
				_ => false
			};
		}

		public virtual partial bool GeneralVectorsScan<T, TS1, TS2>(BinaryOperation op, TS1 x, long strideX, TS2 y, long strideY, bool inclusive) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
		{
			return (op) switch
			{
				BinaryOperation.Add => ParSumProd<T, TS1, TS2, bool>(x, strideX, y, strideY, inclusive),
				BinaryOperation.Multiply => ParSumProd<T, TS1, TS2, byte>(x, strideX, y, strideY, inclusive),
				_ => false
			};
		}
	}
}
