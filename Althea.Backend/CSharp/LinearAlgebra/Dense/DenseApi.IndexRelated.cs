using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

using Althea.Helpers;
using Althea.LinearAlgebra.Dense;
using Althea.Linq;
using Althea.NativeTypes;


namespace Althea.Backend.CSharp.LinearAlgebra
{
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释
	public partial class DenseApi : AbstractApi
	{
		#region vector argument (absolute) min / max
		//// Test == int, uint, long, ulong   for   AbsMax, AbsMin, Max, Min
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe int VectorArgMinMaxReal<T, TInd, Test>(T* x, int length) where T : unmanaged where TInd : unmanaged
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
			Vector<TInd> increment = new(Vector<T>.Count.NativeConvert<int, TInd>());
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
			VectorArgMinMaxManaged<T, Test>((T*)&extremes, Vector<T>.Count, out long extremeIndex);
			int index = (int)extremeIndex;
			// reduce left
			if (lengthLeft > 0)
			{
				VectorArgMinMaxManaged<T, Test>(x + offset, lengthLeft, out long restExtreme);
				int newIndex = (int)(offset + restExtreme);
				if (doMax && x[newIndex].NativeGreaterThan(extremes[index]))
					index = newIndex;
				if (!doMax && x[newIndex].NativeLessThan(extremes[index]))
					index = newIndex;
			}
			return index;
		}

		//// Test == int, uint   for   AbsMax, AbsMin
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe int VectorArgMinMaxCompexSingle<Test>(ComplexSingle* x, int length)
		{
			bool doMax = typeof(Test) == typeof(int);
			// initialize
			Vector256<float> extremes = ComplexSquareAbsNoOrder(x);
			Vector256<int> indices = new Vector<int>(stackalloc int[] { 0, 1, 4, 5, 2, 3, 6, 7 }).AsVector256();
			Vector256<int> extremeIndices = indices;
			Vector256<int> increment = new Vector<int>(Vector256<int>.Count).AsVector256();
			// loop
			int lengthLeft = length - Vector256<float>.Count, offset = Vector256<float>.Count;
			while (lengthLeft >= Vector256<float>.Count) // Vector256<ComplexSingle>.Count * 2
			{
				indices = Avx2.Add(indices, increment);
				Vector256<float> squares = ComplexSquareAbsNoOrder(x + offset);
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
		private static unsafe long VectorArgMinMaxCompexDouble<Test>(ComplexDouble* x, int length)
		{
			bool doMax = typeof(Test) == typeof(int);
			// initialize
			Vector256<double> extremes = ComplexSquareAbsNoOrder(x);
			Vector256<long> indices = new Vector<long>(stackalloc long[] { 0, 2, 1, 3 }).AsVector256();
			Vector256<long> extremeIndices = indices;
			Vector256<long> increment = new Vector<long>(Vector256<long>.Count).AsVector256();
			// loop
			int lengthLeft = length - Vector256<double>.Count, offset = Vector256<double>.Count;
			while (lengthLeft >= Vector256<double>.Count) // Vector256<ComplexDouble>.Count * 2
			{
				indices = Avx2.Add(indices, increment);
				Vector256<double> squares = ComplexSquareAbsNoOrder(x + offset);
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
		private static unsafe bool VectorArgMinMaxManaged<T, Test>(T* x, int length, out long index) where T : unmanaged
		{
			index = -1;
			bool doMax = typeof(Test) == typeof(int) || typeof(Test) == typeof(long);
			bool doAbs = typeof(Test) == typeof(int) || typeof(Test) == typeof(uint);
			if (doAbs)
			{
				double extreme = doMax ? double.MinValue : double.MaxValue;
				int extremeIndex = 0;
				for (int i = 0; i < length; i++)
				{
					double v = x[i].NativeAbsolute();
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
				for (int i = 1; i < length; i++)
				{
					T v = x[i];
					// some frequent type speedups, complex is not possible here
					if (doMax)
					{
						if (v.NativeGreaterThan(extreme))
						{
							extreme = v; extremeIndex = i;
						}
					}
					else
					{
						if (v.NativeLessThan(extreme))
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
			if (!GetPointer(x, out T* px, out int length))
				return false;
			if (length == 0)
				return true;
			if (length == 1)
			{
				index = 0; return true;
			}
			if (!Vector.IsHardwareAccelerated || length <= (Vector<byte>.Count / sizeof(T) * 4))
				return VectorArgMinMaxManaged<T, Test>(px, length, out index); // no SIMD or too short
			if ((sizeof(T) <= sizeof(byte) && length > sbyte.MaxValue) || (sizeof(T) <= sizeof(short) && length > short.MaxValue))
				return VectorArgMinMaxManaged<T, Test>(px, length, out index);

			if (Const<T>.IsComplex)
			{
				if (Const<T>.IsIntegralType || !Avx2.IsSupported)
					return VectorArgMinMaxManaged<T, Test>(px, length, out index);
				if (typeof(T) == typeof(Complex<float>) || typeof(T) == typeof(ComplexSingle))
				{
					index = VectorArgMinMaxCompexSingle<Test>((ComplexSingle*)px, length);
				}
				else // double
				{
					index = VectorArgMinMaxCompexDouble<Test>((ComplexDouble*)px, length);
				}
			}
			else
			{
				if (typeof(T) == typeof(float))
				{
					index = VectorArgMinMaxReal<float, int, Test>((float*)px, length);
				}
				else if (typeof(T) == typeof(double))
				{
					index = VectorArgMinMaxReal<double, long, Test>((double*)px, length);
				}
				else
				{   // integral type
					index = VectorArgMinMaxReal<T, T, Test>(px, length);
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


		#region vector (absolute) min / max
		//// Test == int, uint, long, ulong   for   AbsMax, AbsMin, Max, Min
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe T VectorMinMaxReal<T, Test>(T* x, int length) where T : unmanaged
		{
			bool doMax = typeof(Test) == typeof(int) || typeof(Test) == typeof(long);
			bool doAbs = typeof(Test) == typeof(int) || typeof(Test) == typeof(uint);
			// initial
			Vector<T> extremes = LoadVector(x);
			if (doAbs)
				extremes = Vector.Abs(extremes);
			// loop
			int lengthLeft = length - Vector<T>.Count, offset = Vector<T>.Count;
			while (lengthLeft >= Vector<T>.Count)
			{
				Vector<T> current = LoadVector(x + offset);
				if (doAbs)
					current = Vector.Abs(current);
				if (doMax)
				{
					extremes = Vector.Max(current, extremes);
				}
				else
				{
					extremes = Vector.Min(current, extremes);
				}
				lengthLeft -= Vector<T>.Count;
				offset += Vector<T>.Count;
			}
			// reduce main
			VectorMinMaxManaged<T, Test>((T*)&extremes, Vector<T>.Count, out T extreme);
			// reduce left
			if (lengthLeft > 0)
			{
				VectorMinMaxManaged<T, Test>(x + offset, lengthLeft, out T restExtreme);
				if (doMax && restExtreme.NativeGreaterThan(extreme))
					extreme = restExtreme;
				if (!doMax && restExtreme.NativeLessThan(extreme))
					extreme = restExtreme;
			}
			return extreme;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe ComplexSingle VectorAbsMinMaxCompexSingle<Test>(ComplexSingle* x, int length)
		{
			bool doMax = typeof(Test) == typeof(int);
			// initialize
			Vector256<float> extremes = ComplexSquareAbsNoOrder(x);
			// loop
			int lengthLeft = length - Vector256<float>.Count, offset = Vector256<float>.Count;
			while (lengthLeft >= Vector256<float>.Count) // Vector256<ComplexSingle>.Count * 2
			{
				Vector256<float> squares = ComplexSquareAbsNoOrder(x + offset);
				if (doMax)
				{
					extremes = Avx.Max(squares, extremes);
				}
				else
				{
					extremes = Avx.Max(squares, extremes);
				}
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
					var v = x[offset].Abs();
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
			return extreme;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe ComplexDouble VectorAbsMinMaxCompexDouble<Test>(ComplexDouble* x, int length)
		{
			bool doMax = typeof(Test) == typeof(int);
			// initialize
			Vector256<double> extremes = ComplexSquareAbsNoOrder(x);
			// loop
			int lengthLeft = length - Vector256<double>.Count, offset = Vector256<double>.Count;
			while (lengthLeft >= Vector256<double>.Count) // Vector256<ComplexDouble>.Count * 2
			{
				Vector256<double> squares = ComplexSquareAbsNoOrder(x + offset);
				if (doMax)
				{
					extremes = Avx.Max(squares, extremes);
				}
				else
				{
					extremes = Avx.Max(squares, extremes);
				}
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
					var v = x[offset].Abs();
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
			return extreme;
		}

		//// Test == int, uint, long, ulong   for   AbsMax, AbsMin, Max, Min
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe bool VectorMinMaxManaged<T, Test>(T* x, int length, out T extreme) where T : unmanaged
		{
			bool doMax = typeof(Test) == typeof(int) || typeof(Test) == typeof(long);
			bool doAbs = typeof(Test) == typeof(int) || typeof(Test) == typeof(uint);
			if (doAbs)
			{
				double extremeD = doMax ? double.MinValue : double.MaxValue;
				for (int i = 0; i < length; i++)
				{
					double v = x[i].NativeAbsolute();
					if ((doMax && v > extremeD) || (!doMax && v < extremeD))
					{
						extremeD = v;
					}
				}
				extreme = extremeD.FromDouble<T>();
			}
			else
			{
				T extremeT = x[0];
				for (int i = 1; i < length; i++)
				{
					T v = x[i];
					// some frequent type speedups, complex is not possible here
					if (doMax)
					{
						if (v.NativeGreaterThan(extremeT))
						{
							extremeT = v;
						}
					}
					else
					{
						if (v.NativeLessThan(extremeT))
						{
							extremeT = v;
						}
					}
				}
				extreme = extremeT;
			}
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static unsafe bool MinMax<T, Test>(Storage<T> x, out T extreme) where T : unmanaged
		{
			extreme = default;
			if ((typeof(Test) == typeof(long) || typeof(Test) == typeof(ulong)) && Const<T>.IsComplex)
				throw new InvalidOperationException(string.Format(Resource.CompareComplex, typeof(T).GetGenericString()));
			if (!GetPointer(x, out T* px, out int length))
				return false;
			if (length == 0)
				return true;
			if (length == 1)
			{
				extreme = px[0]; return true;
			}
			if (!Vector.IsHardwareAccelerated || length <= (Vector<byte>.Count / sizeof(T) * 4))
				return VectorMinMaxManaged<T, Test>(px, length, out extreme); // no SIMD or too short
			if ((sizeof(T) <= sizeof(byte) && length > sbyte.MaxValue) || (sizeof(T) <= sizeof(short) && length > short.MaxValue))
				return VectorMinMaxManaged<T, Test>(px, length, out extreme);

			if (Const<T>.IsComplex)
			{
				if (Const<T>.IsIntegralType || !Avx2.IsSupported)
					return VectorMinMaxManaged<T, Test>(px, length, out extreme);
				if (typeof(T) == typeof(Complex<float>) || typeof(T) == typeof(ComplexSingle))
				{
					var temp = VectorAbsMinMaxCompexSingle<Test>((ComplexSingle*)px, length);
					extreme = *(T*)&temp;
				}
				else // double
				{
					var temp = VectorAbsMinMaxCompexDouble<Test>((ComplexDouble*)px, length);
					extreme = *(T*)&temp;
				}
			}
			else
			{
				extreme = VectorMinMaxReal<T, Test>(px, length);
			}
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static bool AbsoluteValueMax<T>(Storage<T> x, out T absMax) where T : unmanaged
		{
			return MinMax<T, int>(x, out absMax);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static unsafe bool AbsoluteValueMin<T>(Storage<T> x, out T absMin) where T : unmanaged
		{
			return MinMax<T, uint>(x, out absMin);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static unsafe bool Max<T>(Storage<T> x, out T max) where T : unmanaged
		{
			return MinMax<T, long>(x, out max);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static unsafe bool Min<T>(Storage<T> x, out T min) where T : unmanaged
		{
			return MinMax<T, ulong>(x, out min);
		}
		#endregion


		#region index APIs
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe bool GetPointerIndexType<T>(Storage<T> s, out T* pointer, out int length) where T : unmanaged
		{
			if (Const<T>.IsIntegralType)
			{
				pointer = default; length = 0;
				throw new TypeMismatchException(typeof(T), TypeMismatchException.MismatchReason.NotInteger);
			}
			return GetPointer(s, out pointer, out length);
		}

		#region bound
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe int VectorBound<T, Lower>(T* x, int length, T value) where T : unmanaged
		{
			bool lower = typeof(Lower) == typeof(bool);
			Vector<T> values = new(value);
			Vector<T> current;
			int lengthLeft = length, offset = 0;
			bool found = false;
			while (lengthLeft >= Vector<T>.Count)
			{
				current = LoadVector(x + offset);
				if ((lower && Vector.GreaterThanOrEqualAny(current, values)) ||
					(!lower && Vector.GreaterThanAny(current, values)))
				{
					found = true;
					break;
				}
				lengthLeft -= Vector<T>.Count;
				offset += Vector<T>.Count;
			}
			if (found || lengthLeft > 0)
			{
				int len = found ? Vector<T>.Count : lengthLeft;
				int find = VectorBoundManaged<T, Lower>(x + offset, len, value);
				if (found)
				{
					return find + offset;
				}
				if (find < 0)
					return -1;
				if (find >= len)
					return length;
				return find + offset;
			}
			else
			{
				return lower ? -1 : length;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe int VectorBoundManaged<T, Lower>(T* x, int length, T value) where T : unmanaged
		{
			bool lower = typeof(Lower) == typeof(bool);
			for (int i = 0; i < length; i++)
			{
				T current = x[i];
				if ((lower && current.NativeGreaterThanOrEqual(value)) ||
					(!lower && current.NativeGreaterThan(value)))
				{
					return i;
				}
			}
			// not found
			return lower ? -1 : length;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static unsafe bool IndexBound<T>(Storage<T> array, T value, bool lowerBound, out long index) where T : unmanaged
		{
			index = -1;
			if (!GetPointerIndexType(array, out T* x, out int length))
				return false;
			if (length == 0)
				return true;

			if (Vector.IsHardwareAccelerated && length > Vector<T>.Count * 4)
			{
				if (lowerBound)
					index = VectorBound<T, bool>(x, length, value);
				else
					index = VectorBound<T, byte>(x, length, value);
			}
			else
			{
				if (lowerBound)
					index = VectorBoundManaged<T, bool>(x, length, value);
				else
					index = VectorBoundManaged<T, byte>(x, length, value);
			}
			return true;
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe void VectorAllBoundsManaged<T, U, Lower>(T* x, int length, T start, T end, U* y) where T : unmanaged where U : unmanaged
		{
			bool lower = typeof(Lower) == typeof(bool);
			T value = start;
			for (int i = 0; i < length; i++)
			{
				T current = x[i];
				if ((lower && current.NativeGreaterThanOrEqual(value)) ||
					(!lower && current.NativeGreaterThan(value)))
				{
					// direct convert is OK here
					y[0] = *(U*)&i;
					// increase pointer
					y++;
					// increase value
					value = value.NativeIncrement();
					if (value.IsEqual(end))
						break;
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static unsafe bool IndexGetAllBounds<T, TOut>(Storage<T> array, Storage<TOut> target, T start, T end, bool lowerBound) where T : unmanaged where TOut : unmanaged
		{
			if (!GetPointerIndexType(array, out T* x, out int lenx))
				return false;
			if (!GetPointerIndexType(target, out TOut* y, out int leny))
				return false;
			if (leny < end.NativeSub(start).ToLong())
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(target));
			if ((typeof(TOut) == typeof(byte) && leny > byte.MaxValue) ||
				(typeof(TOut) == typeof(sbyte) && leny > sbyte.MaxValue) ||
				(typeof(TOut) == typeof(short) && leny > short.MaxValue) ||
				(typeof(TOut) == typeof(ushort) && leny > ushort.MaxValue))
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(target));

			if (lowerBound)
				VectorAllBoundsManaged<T, TOut, bool>(x, lenx, start, end, y);
			else
				VectorAllBoundsManaged<T, TOut, byte>(x, lenx, start, end, y);
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static unsafe bool IndexGenerateFromBounds<T, TOut>(Storage<T> bounds, Storage<TOut> target, bool lowerBound, TOut start) where T : unmanaged where TOut : unmanaged
		{
			if (!GetPointerIndexType(bounds, out T* x, out int lenx))
				return false;
			if (!GetPointerIndexType(target, out TOut* y, out int leny))
				return false;
			if (lowerBound)
			{	// the 'lower' bound array has to contain the length information as well
				x++; lenx--;
			}
			int length = x[lenx - 1].ToInt();
			if (length > leny)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(target));
			long startL = start.ToLong();
			if ((typeof(TOut) == typeof(byte) && length + startL > byte.MaxValue) ||
				(typeof(TOut) == typeof(sbyte) && length + startL > sbyte.MaxValue) ||
				(typeof(TOut) == typeof(short) && length + startL > short.MaxValue) ||
				(typeof(TOut) == typeof(ushort) && length + startL > ushort.MaxValue) ||
				(typeof(TOut) == typeof(int) && length + startL > int.MaxValue) ||
				(typeof(TOut) == typeof(uint) && length + startL > uint.MaxValue))
				throw new ArgumentOutOfRangeException(nameof(start), start, Resources.Parameter.InvalidValue);

			TOut value = start;
			int xPre = x[0].ToInt(), xNow;
			new Span<TOut>(y, x[0].ToInt()).Fill(value);
			for (int i = 1; i < lenx; i++)
			{
				value.NativeIncrement();
				xNow = x[i].ToInt();
				new Span<TOut>(y + xPre, xNow).Fill(value);
				xPre = xNow;
			}
			return true;
		}
		#endregion

		#region find
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe int VectorFindManaged<T>(T* x, int length, T value) where T : unmanaged
		{
			return sizeof(T) switch
			{
				sizeof(byte) => new ReadOnlySpan<byte>(x, length).IndexOf(*(byte*)&value),
				sizeof(short) => new ReadOnlySpan<short>(x, length).IndexOf(*(short*)&value),
				sizeof(int) => new ReadOnlySpan<int>(x, length).IndexOf(*(int*)&value),
				sizeof(long) => new ReadOnlySpan<long>(x, length).BinarySearch(*(long*)&value),
				_ => -1,
			};
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe int VectorFind<T>(T* x, int length, T value) where T : unmanaged
		{
			Vector<T> values = new(value);
			Vector<T> current;
			int lengthLeft = length, offset = 0;
			bool found = false;
			while (lengthLeft >= Vector<T>.Count)
			{
				current = LoadVector(x + offset);
				if (Vector.EqualsAny(current, values))
				{
					found = true;
					break;
				}
				lengthLeft -= Vector<T>.Count;
				offset += Vector<T>.Count;
			}
			if (found || lengthLeft > 0)
			{
				int len = found ? Vector<T>.Count : lengthLeft;
				int find = VectorFindManaged(x + offset, len, value);
				return find >= 0 ? (find + offset) : -1;
			}
			else
			{
				return -1;
			}	
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static unsafe bool IndexFind<T>(bool sorted, Storage<T> array, T value, out long find) where T : unmanaged
		{
			find = -1;
			if (!GetPointerIndexType(array, out T* x, out int length))
				return false;
			if (length == 0)
				return true;

			if (sorted)
			{
				find = sizeof(T) switch
				{
					sizeof(byte) => new ReadOnlySpan<byte>(x, length).BinarySearch(*(byte*)&value),
					sizeof(short) => new ReadOnlySpan<short>(x, length).BinarySearch(*(short*)&value),
					sizeof(int) => new ReadOnlySpan<int>(x, length).BinarySearch(*(int*)&value),
					sizeof(long) => new ReadOnlySpan<long>(x, length).BinarySearch(*(long*)&value),
					_ => -1,
				};
			}
			else
			{
				if (Vector.IsHardwareAccelerated && length > Vector<T>.Count * 4)
				{
					find = VectorFind(x, length, value);
				}
				else
				{
					find = VectorFindManaged(x, length, value);
				}
			}
			return true;
		}
		#endregion
		#endregion
	}
#pragma warning restore CS1591 // 缺少对公共可见类型或成员的 XML 注释
}
