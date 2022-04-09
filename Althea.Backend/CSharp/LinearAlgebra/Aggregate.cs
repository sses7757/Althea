using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

using Althea.Helpers;
using Althea.Linq;
using Althea.NativeTypes;
using Althea.Storage;


namespace Althea.Backend.CSharp.LinearAlgebra
{
	public unsafe partial class Api
	{
		#region (absolute) sum product
		// Ignore spelling: uint ulong
		//// Test == int, uint, long, ulong   for   AbsSum, AbsProd, Sum, Prod
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool VectorAbsSumProdManaged<T, Test>(T* x, int length, out T aggregate) where T : unmanaged, INumber<T>
		{
			aggregate = T.Zero;
			bool doSum = typeof(Test) == typeof(int) || typeof(Test) == typeof(long);
			bool doAbs = typeof(Test) == typeof(int) || typeof(Test) == typeof(uint);
			if (doAbs)
			{
				aggregate = doSum ? T.Zero : T.One;
				for (int i = 0; i < length; i++)
				{
					T v = T.Abs(x[i]);
					if (doSum)
						aggregate += v;
					else
						aggregate *= v;
				}
			}
			else
			{
				aggregate = doSum ? T.Zero : T.One;
				for (int i = 0; i < length; i++)
				{
					T v = x[i];
					// some frequent type speedups
					aggregate = doSum ? aggregate + v : aggregate * v;
				}
			}
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void VectorAbsSumProdReal<T, Test>(T* x, int length, out T aggregation) where T : unmanaged, INumber<T>
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
				VectorAbsSumProdManaged<T, Test>(x + offset, lengthLeft, out aggregation);
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
		private static void VectorAbsSumProdCompexSingle<Test>(Complex<float>* x, int length, out Complex<float> aggregation)
		{
			aggregation = default;
			bool doSum = typeof(Test) == typeof(int) || typeof(Test) == typeof(long);
			bool doAbs = typeof(Test) == typeof(int) || typeof(Test) == typeof(uint);
			if (doAbs)
			{
				// initialize
				Vector256<float> aggregate = ComplexSquareAbsNoOrder(x);
				if (doSum)
					aggregate = Avx.Sqrt(aggregate);
				// loop
				int lengthLeft = length - Vector256<float>.Count, offset = Vector256<float>.Count;
				while (lengthLeft >= Vector256<float>.Count) // Vector256<Complex<float>>.Count * 2
				{
					Vector256<float> squares = ComplexSquareAbsNoOrder(x + offset);
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
				aggregation = result;
			}
			else
			{
				// initialize
				Vector256<float> aggregate1 = LoadVector256<float>(x), aggregate2 = LoadVector256<float>(x + Vector256<float>.Count / 2);
				// loop
				int lengthLeft = length - Vector256<float>.Count, offset = Vector256<float>.Count;
				while (lengthLeft >= Vector256<float>.Count) // Vector256<Complex<float>>.Count * 2
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
				Complex<float> result;
				if (doSum)
				{
					result = ((Complex<float>*)&aggregate1)[0];
					for (int i = 1; i < Vector256<float>.Count / 2; i++)
					{
						result += ((Complex<float>*)&aggregate1)[i];
					}
					for (int i = 0; i < Vector256<float>.Count / 2; i++)
					{
						result += ((Complex<float>*)&aggregate2)[i];
					}
				}
				else
				{
					result = new(*(float*)&aggregate1, *(float*)&aggregate2);
					for (int i = 1; i < Vector256<float>.Count / 2; i++)
					{
						result *= new Complex<float>(((float*)&aggregate1)[i], ((float*)&aggregate2)[i]);
					}
				}
				// reduce left
				if (lengthLeft > 0)
				{
					for (; offset < length; offset++)
					{
						Complex<float> v = x[offset];
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
		private static void VectorAbsSumProdCompexDouble<Test>(Complex<double>* x, int length, out Complex<double> aggregation)
		{
			aggregation = default;
			bool doSum = typeof(Test) == typeof(int) || typeof(Test) == typeof(long);
			bool doAbs = typeof(Test) == typeof(int) || typeof(Test) == typeof(uint);
			if (doAbs)
			{
				// initialize
				Vector256<double> aggregate = ComplexSquareAbsNoOrder(x);
				if (doSum)
					aggregate = Avx.Sqrt(aggregate);
				// loop
				int lengthLeft = length - Vector256<double>.Count, offset = Vector256<double>.Count;
				while (lengthLeft >= Vector256<double>.Count) // Vector256<Complex<double>>.Count * 2
				{
					Vector256<double> squares = ComplexSquareAbsNoOrder(x + offset);
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
				aggregation = result;
			}
			else
			{
				// initialize
				Vector256<double> aggregate1 = LoadVector256<double>(x), aggregate2 = LoadVector256<double>(x + Vector256<double>.Count / 2);
				// loop
				int lengthLeft = length - Vector256<double>.Count, offset = Vector256<double>.Count;
				while (lengthLeft >= Vector256<double>.Count) // Vector256<Complex<double>>.Count * 2
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
				Complex<double> result;
				if (doSum)
				{
					result = ((Complex<double>*)&aggregate1)[0];
					for (int i = 1; i < Vector256<double>.Count / 2; i++)
					{
						result += ((Complex<double>*)&aggregate1)[i];
					}
					for (int i = 0; i < Vector256<double>.Count / 2; i++)
					{
						result += ((Complex<double>*)&aggregate2)[i];
					}
				}
				else
				{
					result = new(*(double*)&aggregate1, *(double*)&aggregate2);
					for (int i = 1; i < Vector256<double>.Count / 2; i++)
					{
						result *= new Complex<double>(((double*)&aggregate1)[i], ((double*)&aggregate2)[i]);
					}
				}
				// reduce left
				if (lengthLeft > 0)
				{
					for (; offset < length; offset++)
					{
						Complex<double> v = x[offset];
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
		internal static bool AbsSumProd<T, TS, Test>(TS x, long stride, out T aggregate) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>
		{
			aggregate = T.Zero;
			if (!GetPointer(x, stride, out T* px, out int length))
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
			if (!Vector.IsHardwareAccelerated || length <= (Vector<byte>.Count / sizeof(T) * 4)) // no SIMD or too short
				return VectorAbsSumProdManaged<T, Test>(px, length, out aggregate);

			if (NumberType<T>.IsComplex)
			{
				if (Unmanaged<T>.DataType.IsInteger() || !Avx.IsSupported) // no AVX's HorizontalAdd and Unpack (Vector<T> has not corresponding implementation yet)
					return VectorAbsSumProdManaged<T, Test>(px, length, out aggregate);
				if (typeof(T) == typeof(Complex<float>) || typeof(T) == typeof(Complex<float>))
				{
					VectorAbsSumProdCompexSingle<Test>((Complex<float>*)px, length, out var temp);
					aggregate = *(T*)&temp;
				}
				else // double
				{
					VectorAbsSumProdCompexDouble<Test>((Complex<double>*)px, length, out var temp);
					aggregate = *(T*)&temp;
				}
			}
			else
			{
				VectorAbsSumProdReal<T, Test>(px, length, out aggregate);
			}
			return true;
		}

		public virtual partial bool AbsoluteValueSum<T, TS>(TS x, long strideX, out T sum) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS> => AbsSumProd<T, TS, int>(x, strideX, out sum);

		/// <summary>
		/// Compute the product of the absolute values of the elements of vector <paramref name="x"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS">The actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="x">The vector of type <typeparamref name="T"/></param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="product">Output the result as a <typeparamref name="T"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> ≤ 0</exception>
		public virtual bool AbsoluteValueProduct<T, TS>(TS x, long strideX, out T product) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS> => AbsSumProd<T, TS, uint>(x, strideX, out product);

		public virtual partial bool AggregateSum<T, TS>(TS x, long stride, out T sum) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS> => AbsSumProd<T, TS, long>(x, stride, out sum);

		public virtual partial bool AggregateProduct<T, TS>(TS x, long stride, out T product) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS> => AbsSumProd<T, TS, ulong>(x, stride, out product);
		#endregion


		#region vector argument (absolute) min / max
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static int VectorArgMinMaxReal<T, TInd, Test>(T* x, int length) where T : unmanaged, INumber<T> where TInd : unmanaged, INumber<TInd>
		{
			bool doMax = typeof(Test) == typeof(int) || typeof(Test) == typeof(long);
			bool doAbs = typeof(Test) == typeof(int) || typeof(Test) == typeof(uint);
			// maximize with stride == Vector<T>.Count
			// initial
			Vector<T> extremes = LoadVector(x);
			if (doAbs)
				extremes = Vector.Abs(extremes);
			Vector<TInd> indices = new(stackalloc TInd[Vector<T>.Count].FillWithRange(TInd.Zero));
			Vector<TInd> extremeIndices = indices;
			Vector<TInd> increment = new(TInd.Create(Vector<T>.Count));
			// loop
			int lengthLeft = length - Vector<T>.Count, offset = Vector<T>.Count;
			while (lengthLeft >= Vector<T>.Count)
			{
				indices += increment;
				Vector<TInd> compare;
				// JIT shall optimize the branches and type converts to some code as if they do not exist
				Vector<T> current = doAbs ? Vector.Abs(LoadVector(x + offset)) : LoadVector(x + offset);
				if (doMax)
				{   // abs max || max
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
				if (doMax && x[newIndex] > extremes[index])
					index = newIndex;
				if (!doMax && x[newIndex] < extremes[index])
					index = newIndex;
			}
			return index;
		}

		//// Test == int, uint   for   AbsMax, AbsMin
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static int VectorArgMinMaxCompexSingle<Test>(Complex<float>* x, int length)
		{
			bool doMax = typeof(Test) == typeof(int);
			// initialize
			Vector256<float> extremes = ComplexSquareAbsNoOrder(x);
			Vector256<int> indices = new Vector<int>(stackalloc int[] { 0, 1, 4, 5, 2, 3, 6, 7 }).AsVector256();
			Vector256<int> extremeIndices = indices;
			Vector256<int> increment = new Vector<int>(Vector256<int>.Count).AsVector256();
			// loop
			int lengthLeft = length - Vector256<float>.Count, offset = Vector256<float>.Count;
			while (lengthLeft >= Vector256<float>.Count) // Vector256<Complex<float>>.Count * 2
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
		private static long VectorArgMinMaxCompexDouble<Test>(Complex<double>* x, int length)
		{
			bool doMax = typeof(Test) == typeof(int);
			// initialize
			Vector256<double> extremes = ComplexSquareAbsNoOrder(x);
			Vector256<long> indices = new Vector<long>(stackalloc long[] { 0, 2, 1, 3 }).AsVector256();
			Vector256<long> extremeIndices = indices;
			Vector256<long> increment = new Vector<long>(Vector256<long>.Count).AsVector256();
			// loop
			int lengthLeft = length - Vector256<double>.Count, offset = Vector256<double>.Count;
			while (lengthLeft >= Vector256<double>.Count) // Vector256<Complex<double>>.Count * 2
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
		private static bool VectorArgMinMaxManaged<T, Test>(T* x, int length, out long index) where T : unmanaged, INumber<T>
		{
			index = -1;
			bool doMax = typeof(Test) == typeof(int) || typeof(Test) == typeof(long);
			bool doAbs = typeof(Test) == typeof(int) || typeof(Test) == typeof(uint);
			if (doAbs)
			{
				T extreme = T.Abs(x[0]); int extremeIndex = 0;
				for (int i = 1; i < length; i++)
				{
					T v = T.Abs(x[i]);
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
		internal static bool ArgMinMax<T, TS, Test>(TS x, long strideX, out long index) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>
		{
			index = -1;
			if ((typeof(Test) == typeof(long) || typeof(Test) == typeof(ulong)) && NumberType<T>.IsComplex)
				throw new InvalidOperationException(string.Format(Resource.CompareComplex, typeof(T).GetGenericString()));
			if (!GetPointer(x, strideX, out T* px, out int length))
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

			if (NumberType<T>.IsComplex)
			{
				if (Unmanaged<T>.DataType.IsInteger() || !Avx2.IsSupported)
					return VectorArgMinMaxManaged<T, Test>(px, length, out index);
				index = typeof(T) == typeof(Complex<float>) || typeof(T) == typeof(Complex<float>)
					? VectorArgMinMaxCompexSingle<Test>((Complex<float>*)px, length)
					: VectorArgMinMaxCompexDouble<Test>((Complex<double>*)px, length);
			}
			else
			{
				if (typeof(T) == typeof(float))
				{
					index = VectorArgMinMaxReal<float, int, Test>((float*)px, length);
				}
				else
				{
					index = typeof(T) == typeof(double) ?
							VectorArgMinMaxReal<double, long, Test>((double*)px, length) 
							: (long)VectorArgMinMaxReal<T, T, Test>(px, length);
				}
			}
			return true;
		}

		public virtual partial bool AbsoluteValueArgMax<T, TS>(TS x, long strideX, out long index) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS> => ArgMinMax<T, TS, int>(x, strideX, out index);

		public virtual partial bool AbsoluteValueArgMin<T, TS>(TS x, long strideX, out long index) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS> => ArgMinMax<T, TS, uint>(x, strideX, out index);

		/// <summary>
		/// Find the (smallest) index of the element with the maximum value.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS">The actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="x">The vector of type <typeparamref name="T"/></param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="index">Output the resulting index</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> ≤ 0</exception>
		public virtual bool ArgMax<T, TS>(TS x, long strideX, out long index) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS> => ArgMinMax<T, TS, long>(x, strideX, out index);

		/// <summary>
		/// Find the (smallest) index of the element with the minimum value.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS">The actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="x">The vector of type <typeparamref name="T"/></param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="index">Output the resulting index</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> ≤ 0</exception>
		public virtual bool ArgMin<T, TS>(TS x, long strideX, out long index) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS> => ArgMinMax<T, TS, ulong>(x, strideX, out index);
		#endregion


		#region vector (absolute) min / max
		//// Test == int, uint, long, ulong   for   AbsMax, AbsMin, Max, Min
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static T VectorMinMaxReal<T, Test>(T* x, int length) where T : unmanaged, INumber<T>
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
				extremes = doMax ? Vector.Max(current, extremes) : Vector.Min(current, extremes);
				lengthLeft -= Vector<T>.Count;
				offset += Vector<T>.Count;
			}
			// reduce main
			VectorMinMaxManaged<T, Test>((T*)&extremes, Vector<T>.Count, out T extreme);
			// reduce left
			if (lengthLeft > 0)
			{
				VectorMinMaxManaged<T, Test>(x + offset, lengthLeft, out T restExtreme);
				if (doMax && restExtreme > extreme)
					extreme = restExtreme;
				if (!doMax && restExtreme < extreme)
					extreme = restExtreme;
			}
			return extreme;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Complex<float> VectorAbsMinMaxCompexSingle<Test>(Complex<float>* x, int length)
		{
			bool doMax = typeof(Test) == typeof(int);
			// initialize
			Vector256<float> extremes = ComplexSquareAbsNoOrder(x);
			// loop
			int lengthLeft = length - Vector256<float>.Count, offset = Vector256<float>.Count;
			while (lengthLeft >= Vector256<float>.Count) // Vector256<Complex<float>>.Count * 2
			{
				Vector256<float> squares = ComplexSquareAbsNoOrder(x + offset);
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
			return extreme;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Complex<double> VectorAbsMinMaxCompexDouble<Test>(Complex<double>* x, int length)
		{
			bool doMax = typeof(Test) == typeof(int);
			// initialize
			Vector256<double> extremes = ComplexSquareAbsNoOrder(x);
			// loop
			int lengthLeft = length - Vector256<double>.Count, offset = Vector256<double>.Count;
			while (lengthLeft >= Vector256<double>.Count) // Vector256<Complex<double>>.Count * 2
			{
				Vector256<double> squares = ComplexSquareAbsNoOrder(x + offset);
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
			return extreme;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool VectorMinMaxManaged<T, Test>(T* x, int length, out T extreme) where T : unmanaged, INumber<T>
		{
			bool doMax = typeof(Test) == typeof(int) || typeof(Test) == typeof(long);
			bool doAbs = typeof(Test) == typeof(int) || typeof(Test) == typeof(uint);
			if (doAbs)
			{
				extreme = x[0];
				for (int i = 1; i < length; i++)
				{
					T v = T.Abs(x[i]);
					if ((doMax && v > extreme) || (!doMax && v < extreme))
					{
						extreme = v;
					}
				}
			}
			else
			{
				extreme = x[0];
				for (int i = 1; i < length; i++)
				{
					T v = x[i];
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
		internal static bool MinMax<T, TS, Test>(TS x, long strideX, out T extreme) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>
		{
			extreme = default;
			if ((typeof(Test) == typeof(long) || typeof(Test) == typeof(ulong)) && NumberType<T>.IsComplex)
				throw new InvalidOperationException(string.Format(Resource.CompareComplex, typeof(T).GetGenericString()));
			if (!GetPointer(x, strideX, out T* px, out int length))
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

			if (NumberType<T>.IsComplex)
			{
				if (Unmanaged<T>.DataType.IsInteger() || !Avx2.IsSupported)
					return VectorMinMaxManaged<T, Test>(px, length, out extreme);
				if (typeof(T) == typeof(Complex<float>) || typeof(T) == typeof(Complex<float>))
				{
					var temp = VectorAbsMinMaxCompexSingle<Test>((Complex<float>*)px, length);
					extreme = *(T*)&temp;
				}
				else // double
				{
					var temp = VectorAbsMinMaxCompexDouble<Test>((Complex<double>*)px, length);
					extreme = *(T*)&temp;
				}
			}
			else
			{
				extreme = VectorMinMaxReal<T, Test>(px, length);
			}
			return true;
		}

		/// <summary>
		/// Find the element with the maximum absolute value.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS">The actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="x">The vector of type <typeparamref name="T"/></param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="absMax">Output the result</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> ≤ 0</exception>
		public virtual bool AbsoluteValueMax<T, TS>(TS x, long strideX, out T absMax) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS> => MinMax<T, TS, int>(x, strideX, out absMax);

		/// <summary>
		/// Find the element with the minimum absolute value.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS">The actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="x">The vector of type <typeparamref name="T"/></param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="absMin">Output the result</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> ≤ 0</exception>
		public virtual bool AbsoluteValueMin<T, TS>(TS x, long strideX, out T absMin) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS> => MinMax<T, TS, uint>(x, strideX, out absMin);

		/// <summary>
		/// Find the element with the maximum value.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS">The actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="x">The vector of type <typeparamref name="T"/></param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="max">Output the result</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> ≤ 0</exception>
		public virtual bool Max<T, TS>(TS x, long strideX, out T max) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS> => MinMax<T, TS, long>(x, strideX, out max);

		/// <summary>
		/// Find the element with the minimum value.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS">The actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="x">The vector of type <typeparamref name="T"/></param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="min">Output the result</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> ≤ 0</exception>
		public virtual bool Min<T, TS>(TS x, long strideX, out T min) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS> => MinMax<T, TS, ulong>(x, strideX, out min);
		#endregion


		#region inner
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static T VectorInnerManaged<T, Dot, Conj>(T* x, T* y, int length) where T : unmanaged, INumber<T>
		{
			T result = default;
			bool doDot = typeof(Dot) == typeof(bool);
			bool doCon = typeof(Conj) == typeof(bool);

			for (int i = 0; i < length; i++)
			{
				T a = x[i], b;
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
				if (typeof(T) == typeof(Complex<float>))
				{
					Complex<float> vv = doDot && doCon
						? Complex<float>.FusedMultiplyAdd((*(Complex<float>*)&a).Conjugate, *(Complex<float>*)&b, *(Complex<float>*)&result)
						: doDot && !doCon
						? Complex<float>.FusedMultiplyAdd(*(Complex<float>*)&a, *(Complex<float>*)&b, *(Complex<float>*)&result)
						: (*(Complex<float>*)&result) + (*(Complex<float>*)&a).MagnitudeSquared;
					result = *(T*)&vv;
					continue;
				}
				if (typeof(T) == typeof(Complex<double>))
				{
					Complex<double> vv = doDot && doCon
						? Complex<double>.FusedMultiplyAdd((*(Complex<double>*)&a).Conjugate, *(Complex<double>*)&b, *(Complex<double>*)&result)
						: doDot && !doCon
						? Complex<double>.FusedMultiplyAdd(*(Complex<double>*)&a, *(Complex<double>*)&b, *(Complex<double>*)&result)
						: (*(Complex<double>*)&result) + (*(Complex<double>*)&a).MagnitudeSquared;
					result = *(T*)&vv;
					continue;
				}
				// normal case
				if (doDot)
				{
					b = y[i];
					result = doCon ? result + a.Conjugate() * b : result + a * b;
				}
				else
				{
					result += a * a;
				}
			}
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static T VectorInnerReal<T, Dot>(T* x, T* y, int length) where T : unmanaged, INumber<T>
		{
			bool doDot = typeof(Dot) == typeof(bool);
			// reduce to Vector<T>.Count sums
			Vector<T> sum = Vector<T>.Zero;
			int lengthLeft = length, offset = 0;
			while (lengthLeft >= Vector<T>.Count)
			{
				Vector<T> a = LoadVector(x + offset), b = LoadVector(y + offset);
				if (!doDot)
					b = a;
				if (typeof(T) == typeof(float) && Fma.IsSupported)
				{
					if (Vector<T>.Count == Vector256<T>.Count)
					{
						sum = Fma.MultiplyAdd(a.AsVector256().AsSingle(), b.AsVector256().AsSingle(), sum.AsVector256().AsSingle()).As<float, T>().AsVector();
						goto CONTINUE;
					}
					else if (Vector<T>.Count == Vector128<T>.Count)
					{
						sum = Fma.MultiplyAdd(a.AsVector128().AsSingle(), b.AsVector128().AsSingle(), sum.AsVector128().AsSingle()).As<float, T>().AsVector();
						goto CONTINUE;
					}
				}
				if (typeof(T) == typeof(double) && Fma.IsSupported)
				{
					if (Vector<T>.Count == Vector256<T>.Count)
					{
						sum = Fma.MultiplyAdd(a.AsVector256().AsDouble(), b.AsVector256().AsDouble(), sum.AsVector256().AsDouble()).As<double, T>().AsVector();
						goto CONTINUE;
					}
					else if (Vector<T>.Count == Vector128<T>.Count)
					{
						sum = Fma.MultiplyAdd(a.AsVector128().AsDouble(), b.AsVector128().AsDouble(), sum.AsVector128().AsDouble()).As<double, T>().AsVector();
						goto CONTINUE;
					}
				}
				// no FMA
				sum += a * b;
			CONTINUE:
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
			return dotMain + dotLeft;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Complex<float> VectorInnerCompex<Dot, Conj>(Complex<float>* x, Complex<float>* y, int length)
		{
			Complex<float> innerResult;
			bool doDot = typeof(Dot) == typeof(bool);
			bool doConj = typeof(Conj) == typeof(bool);
			if (!doDot)
			{
				// initialize
				Vector256<float> aggregate = ComplexSquareAbsNoOrder(x);
				// loop
				int lengthLeft = length - Vector256<float>.Count, offset = Vector256<float>.Count;
				while (lengthLeft >= Vector256<float>.Count) // Vector256<Complex<float>>.Count * 2
				{
					Vector256<float> squares = ComplexSquareAbsNoOrder(x + offset);
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
				innerResult = result;
			}
			else
			{
				// initialize
				Vector256<float> aggregateReal = Vector<float>.Zero.AsVector256(), aggregateImag = Vector<float>.Zero.AsVector256();
				ComplexMultiplyAdd<Conj>(x, y, ref aggregateReal, ref aggregateImag);
				// loop
				int lengthLeft = length - Vector256<float>.Count, offset = Vector256<float>.Count;
				while (lengthLeft >= Vector256<float>.Count) // Vector256<Complex<float>>.Count * 2
				{
					ComplexMultiplyAdd<Conj>(x + offset, y + offset, ref aggregateReal, ref aggregateImag);
					lengthLeft -= Vector256<float>.Count;
					offset += Vector256<float>.Count;
				}
				// reduce main
				float* reals = (float*)&aggregateReal, imags = (float*)&aggregateImag;
				Complex<float> result = new(reals[0], imags[0]);
				for (int i = 1; i < Vector256<float>.Count; i++)
				{
					Complex<float> v = new(reals[i], imags[i]);
					result += v;
				}
				// reduce left
				if (lengthLeft > 0)
				{
					for (; offset < length; offset++)
					{
						Complex<float> v1 = x[offset], v2 = y[offset];
						result += v1 * v2;
					}
				}
				innerResult = result;
			}
			return innerResult;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Complex<double> VectorInnerComplex<Dot, Conj>(Complex<double>* x, Complex<double>* y, int length)
		{
			Complex<double> innerResult;
			bool doDot = typeof(Dot) == typeof(bool);
			if (!doDot)
			{
				// initialize
				Vector256<double> aggregate = ComplexSquareAbsNoOrder(x);
				// loop
				int lengthLeft = length - Vector256<double>.Count, offset = Vector256<double>.Count;
				while (lengthLeft >= Vector256<double>.Count) // Vector256<Complex<double>>.Count * 2
				{
					Vector256<double> squares = ComplexSquareAbsNoOrder(x + offset);
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
				innerResult = result;
			}
			else
			{
				// initialize
				Vector256<double> aggregateReal = Vector<double>.Zero.AsVector256(), aggregateImag = Vector<double>.Zero.AsVector256();
				ComplexMultiplyAdd<Conj>(x, y, ref aggregateReal, ref aggregateImag);
				// loop
				int lengthLeft = length - Vector256<double>.Count, offset = Vector256<double>.Count;
				while (lengthLeft >= Vector256<double>.Count) // Vector256<Complex<float>>.Count * 2
				{
					ComplexMultiplyAdd<Conj>(x + offset, y + offset, ref aggregateReal, ref aggregateImag);
					lengthLeft -= Vector256<double>.Count;
					offset += Vector256<double>.Count;
				}
				// reduce main
				double* reals = (double*)&aggregateReal, imags = (double*)&aggregateImag;
				Complex<double> result = new(reals[0], imags[0]);
				for (int i = 1; i < Vector256<double>.Count; i++)
				{
					Complex<double> v = new(reals[i], imags[i]);
					result += v;
				}
				// reduce left
				if (lengthLeft > 0)
				{
					for (; offset < length; offset++)
					{
						Complex<double> v1 = x[offset], v2 = y[offset];
						result += v1 * v2;
					}
				}
				innerResult = result;
			}
			return innerResult;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool Inner<T, TS1, TS2, Dot>(bool conjX, TS1 x, long strideX, TS2 y, long strideY, out T dot) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
		{
			dot = default;
			if (!GetPointer(x, strideX, out T* px, out int lenx))
				return false;
			if (!GetPointer(y, strideY, out T* py, out int leny))
				return false;
			int length = Math.Min(lenx, leny);
			if (length == 0)
				return true;

			if (!Vector.IsHardwareAccelerated || length <= (Vector<byte>.Count / sizeof(T) * 4) ||
				(NumberType<T>.IsComplex && (Unmanaged<T>.DataType.IsInteger() || !Avx.IsSupported)))
			{   // no SIMD or too short
				// no AVX's HorizontalAdd and Unpack (Vector<T> has not corresponding implementation yet)
				dot = conjX ? VectorInnerManaged<T, Dot, bool>(px, py, length) : VectorInnerManaged<T, Dot, byte>(px, py, length);
				return true;
			}

			if (NumberType<T>.IsComplex)
			{
				if (typeof(T) == typeof(Complex<float>) || typeof(T) == typeof(Complex<float>))
				{
					var xx = (Complex<float>*)px; var yy = (Complex<float>*)py;
					Complex<float> temp;
					if (conjX)
						VectorInnerCompex<Dot, bool>(xx, yy, length);
					else
						VectorInnerCompex<Dot, byte>(xx, yy, length);
					dot = *(T*)&temp;
				}
				else // double
				{
					var xx = (Complex<double>*)px; var yy = (Complex<double>*)py;
					Complex<double> temp;
					if (conjX)
						VectorInnerComplex<Dot, bool>(xx, yy, length);
					else
						VectorInnerComplex<Dot, byte>(xx, yy, length);
					dot = *(T*)&temp;
				}
			}
			else
			{
				dot = VectorInnerReal<T, Dot>(px, py, length);
			}
			return true;
		}

		public virtual partial bool Norm<T, TS>(TS x, long strideX, out T norm) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>
		{
			norm = default;
			if (!Inner<T, TS, TS, byte>(conjX: true, x, strideX, x, strideX, out T dot))
				return false;
			norm = Math.Sqrt(dot.As<T, double>()).As<double, T>();
			return true;
		}

		public virtual partial bool Dot<T, TS1, TS2>(bool conjX, TS1 x, long strideX, TS2 y, long strideY, out T dot) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> => Inner<T, TS1, TS2, bool>(conjX, x, strideX, y, strideY, out dot);
		#endregion


		#region partial sum product
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void VectorParSumProdManaged<T, Sum, HasPre>(T* x, T* y, int length) where T : unmanaged, INumber<T>
		{
			bool doSum = typeof(Sum) == typeof(bool);
			bool hasPre = typeof(HasPre) == typeof(bool);

			T result = hasPre ? y[-1] : doSum ? default : T.One;
			for (int i = 0; i < length; i++)
			{
				T v = x[i];
				y[i] = doSum ? result + v : result * v;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool ParSumProd<T, TS1, TS2, Sum>(TS1 x, long strideX, TS2 y, long strideY, bool inclusive) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
		{
			if (!GetPointer(x, strideX, out T* px, out int lenx))
				return false;
			if (!GetPointer(y, strideY, out T* py, out int leny))
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
			VectorParSumProdManaged<T, Sum, byte>(px, py, length);
			return true;
		}

		public virtual partial bool PartialSum<T, TS1, TS2>(TS1 x, long strideX, TS2 y, long strideY, bool inclusive) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> => ParSumProd<T, TS1, TS2, bool>(x, strideX, y, strideY, inclusive);

		public virtual partial bool PartialProduct<T, TS1, TS2>(TS1 x, long strideX, TS2 y, long strideY, bool inclusive) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> => ParSumProd<T, TS1, TS2, int>(x, strideX, y, strideY, inclusive);
		#endregion
	}
}
