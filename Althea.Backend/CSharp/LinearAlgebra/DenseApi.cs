using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

using Althea.Backend.Storage;
using Althea.LinearAlgebra;
using Althea.LinearAlgebra.Dense;
using Althea.Linq;
using Althea.NativeTypes;


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
		private static unsafe bool GetSpan<T>(Storage<T> s, out void* pointer, out int length) where T : unmanaged
		{
			pointer = default; length = 0;
			if (s is null || !s.IsValid())
				throw new ArgumentNullException(nameof(s));
			if (s.Count != 1 || s[0].Pointer is not IMemoryPointer m)
				return false; // not support
			if (!Const<T>.IsPreDefined)
				return false; // not support
			pointer = m.Pointer.ToPointer();
			if (pointer == default)
				return false; // not support
			long l = m.LengthInBytes / Const<T>.SizeT;
			if (l > int.MaxValue)
				return false; // not support
			length = (int)l;
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Vector<T> LoadVector<T>(ref T r) where T : unmanaged
		{
			return Unsafe.ReadUnaligned<Vector<T>>(ref Unsafe.As<T, byte>(ref r));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Vector256<T> LoadVector256<T>(ref T r) where T : unmanaged
		{
			return Unsafe.ReadUnaligned<Vector256<T>>(ref Unsafe.As<T, byte>(ref r));
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Vector256<T> LoadVector256<T, U>(ref U r) where T : unmanaged where U : unmanaged
		{
			return Unsafe.ReadUnaligned<Vector256<T>>(ref Unsafe.As<U, byte>(ref r));
		}
		#endregion

		#region static
		// AVX optimization is not used since during the test (on Windows 10, .NET 5.0, i7-8700K),
		// System.Numerics.Vector<T> utilizes the AVX instruction directly by the JIT.
		// Therefore the difference between Vector<T> and AVX assembly codes are almost the same,
		// and their performance difference is less than 3% (basically comes from the unoptimized final operations)
		// Both of them outperforms the scalar implementation for around 3 times (this number shall be 4 without any loop-related operation).

		#region vector argument (absolute) min / max
		//// Test == int, uint, long, ulong   for   AbsMax, AbsMin, Max, Min
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe int VectorMinMaxReal<T, TInd, Test>(void* px, int length) where T : unmanaged where TInd : unmanaged
		{
			Span<T> a = new(px, length);
			bool doMax = typeof(Test) == typeof(int) || typeof(Test) == typeof(long);
			// maximize with stride == Vector<T>.Count
			#region initial
			int lengthLeft = length, offset = 0;
			Vector<T> extremes;
			if (typeof(T) == typeof(sbyte))
			{
				sbyte v = doMax ? sbyte.MinValue : sbyte.MaxValue; extremes = new(*(T*)&v);
			}
			else if (typeof(T) == typeof(short))
			{
				short v = doMax ? short.MinValue : short.MaxValue; extremes = new(*(T*)&v);
			}
			else if (typeof(T) == typeof(int))
			{
				int v = doMax ? int.MinValue : int.MaxValue; extremes = new(*(T*)&v);
			}
			else if (typeof(T) == typeof(long))
			{
				long v = doMax ? long.MinValue : long.MaxValue; extremes = new(*(T*)&v);
			}
			else if (typeof(T) == typeof(byte))
			{
				byte v = doMax ? byte.MinValue : byte.MaxValue; extremes = new(*(T*)&v);
			}
			else if (typeof(T) == typeof(ushort))
			{
				ushort v = doMax ? ushort.MinValue : ushort.MaxValue; extremes = new(*(T*)&v);
			}
			else if (typeof(T) == typeof(uint))
			{
				uint v = doMax ? uint.MinValue : uint.MaxValue; extremes = new(*(T*)&v);
			}
			else if (typeof(T) == typeof(ulong))
			{
				ulong v = doMax ? ulong.MinValue : ulong.MaxValue; extremes = new(*(T*)&v);
			}
			else if (typeof(T) == typeof(float))
			{
				float v = doMax ? float.MinValue : float.MaxValue; extremes = new(*(T*)&v);
			}
			else if (typeof(T) == typeof(double))
			{
				double v = doMax ? double.MinValue : double.MaxValue; extremes = new(*(T*)&v);
			}
			else
			{
				extremes = Vector<T>.Zero;
			}
			Vector<TInd> indices = new(stackalloc TInd[Vector<T>.Count].FillWithRange(default));
			Vector<TInd> extremeIndices = indices;
			Vector<TInd> increment = new(Vector<T>.Count.GenericConvert<int, TInd>());
			#endregion
			while (lengthLeft >= Vector<T>.Count)
			{
				#region loop
				indices += increment;
				Vector<TInd> compare;
				// JIT shall optimize the branches and type converts to some code as if they do not exist
				if (doMax)
				{	// abs max || max
					Vector<T> current;
					if (typeof(Test) == typeof(int))
						current = Vector.Abs(LoadVector(ref a[offset]));
					else
						current = LoadVector(ref a[offset]);
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
				else//// if (typeof(Test) == typeof(uint) || typeof(Test) == typeof(ulong))
				{   // abs min || min
					Vector<T> current;
					if (typeof(Test) == typeof(uint))
						current = Vector.Abs(LoadVector(ref a[offset]));
					else
						current = LoadVector(ref a[offset]);
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
				#endregion
				lengthLeft -= Vector<T>.Count;
				offset += Vector<T>.Count;
			}
			#region reduce main
			T extreme = extremes[0]; TInd extremeIndex = extremeIndices[0];
			for (int i = 1; i < Vector<T>.Count; i++)
			{
				if (doMax)
				{
					if ((typeof(T) == typeof(byte) && ((byte*)&extremes)[i] > *(byte*)&extreme) ||
						(typeof(T) == typeof(sbyte) && ((sbyte*)&extremes)[i] > *(sbyte*)&extreme) ||
						(typeof(T) == typeof(short) && ((short*)&extremes)[i] > *(short*)&extreme) ||
						(typeof(T) == typeof(ushort) && ((ushort*)&extremes)[i] > *(ushort*)&extreme) ||
						(typeof(T) == typeof(int) && ((int*)&extremes)[i] > *(int*)&extreme) ||
						(typeof(T) == typeof(uint) && ((uint*)&extremes)[i] > *(uint*)&extreme) ||
						(typeof(T) == typeof(long) && ((long*)&extremes)[i] > *(long*)&extreme) ||
						(typeof(T) == typeof(ulong) && ((ulong*)&extremes)[i] > *(ulong*)&extreme) ||
						(typeof(T) == typeof(float) && ((float*)&extremes)[i] > *(float*)&extreme) ||
						(typeof(T) == typeof(double) && ((double*)&extremes)[i] > *(double*)&extreme))
					{
						extreme = extremes[i]; extremeIndex = extremeIndices[i];
					}
				}
				else
				{
					if ((typeof(T) == typeof(byte) && ((byte*)&extremes)[i] < *(byte*)&extreme) ||
						(typeof(T) == typeof(sbyte) && ((sbyte*)&extremes)[i] < *(sbyte*)&extreme) ||
						(typeof(T) == typeof(short) && ((short*)&extremes)[i] < *(short*)&extreme) ||
						(typeof(T) == typeof(ushort) && ((ushort*)&extremes)[i] < *(ushort*)&extreme) ||
						(typeof(T) == typeof(int) && ((int*)&extremes)[i] < *(int*)&extreme) ||
						(typeof(T) == typeof(uint) && ((uint*)&extremes)[i] < *(uint*)&extreme) ||
						(typeof(T) == typeof(long) && ((long*)&extremes)[i] < *(long*)&extreme) ||
						(typeof(T) == typeof(ulong) && ((ulong*)&extremes)[i] < *(ulong*)&extreme) ||
						(typeof(T) == typeof(float) && ((float*)&extremes)[i] < *(float*)&extreme) ||
						(typeof(T) == typeof(double) && ((double*)&extremes)[i] < *(double*)&extreme))
					{
						extreme = extremes[i]; extremeIndex = extremeIndices[i];
					}
				}
			}
			int index = extremeIndex.GenericConvert<TInd, int>();
			#endregion

			#region reduce left
			if (lengthLeft > 0)
			{
				for (; offset < length; offset++)
				{
					if (doMax)
					{
						if ((typeof(T) == typeof(byte) && ((byte*)&px)[offset] > *(byte*)&extreme) ||
							(typeof(T) == typeof(sbyte) && ((sbyte*)&px)[offset] > *(sbyte*)&extreme) ||
							(typeof(T) == typeof(short) && ((short*)&px)[offset] > *(short*)&extreme) ||
							(typeof(T) == typeof(ushort) && ((ushort*)&px)[offset] > *(ushort*)&extreme) ||
							(typeof(T) == typeof(int) && ((int*)&px)[offset] > *(int*)&extreme) ||
							(typeof(T) == typeof(uint) && ((uint*)&px)[offset] > *(uint*)&extreme) ||
							(typeof(T) == typeof(long) && ((long*)&px)[offset] > *(long*)&extreme) ||
							(typeof(T) == typeof(ulong) && ((ulong*)&px)[offset] > *(ulong*)&extreme) ||
							(typeof(T) == typeof(float) && ((float*)&px)[offset] > *(float*)&extreme) ||
							(typeof(T) == typeof(double) && ((double*)&px)[offset] > *(double*)&extreme))
						{
							extreme = a[offset]; index = offset;
						}
					}
					else
					{
						if ((typeof(T) == typeof(byte) && Unsafe.As<T, byte>(ref a[offset]) < *(byte*)&extreme) ||
							(typeof(T) == typeof(sbyte) && Unsafe.As<T, sbyte>(ref a[offset]) < *(sbyte*)&extreme) ||
							(typeof(T) == typeof(short) && Unsafe.As<T, short>(ref a[offset]) < *(short*)&extreme) ||
							(typeof(T) == typeof(ushort) && Unsafe.As<T, ushort>(ref a[offset]) < *(ushort*)&extreme) ||
							(typeof(T) == typeof(int) && Unsafe.As<T, int>(ref a[offset]) < *(int*)&extreme) ||
							(typeof(T) == typeof(uint) && Unsafe.As<T, uint>(ref a[offset]) < *(uint*)&extreme) ||
							(typeof(T) == typeof(long) && Unsafe.As<T, long>(ref a[offset]) < *(long*)&extreme) ||
							(typeof(T) == typeof(ulong) && Unsafe.As<T, ulong>(ref a[offset]) < *(ulong*)&extreme) ||
							(typeof(T) == typeof(float) && Unsafe.As<T, float>(ref a[offset]) < *(float*)&extreme) ||
							(typeof(T) == typeof(double) && Unsafe.As<T, double>(ref a[offset]) < *(double*)&extreme))
						{
							extreme = a[offset]; index = offset;
						}
					}
				}
			}
			#endregion
			return index;
		}

		//// Test == int, uint   for   AbsMax, AbsMin
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe int VectorMinMaxCompexFloat<Test>(void* px, int length)
		{
			Span<Complex<float>> a = new(px, length);
			bool doMax = typeof(Test) == typeof(int);
			// initialize
			int lengthLeft = length, offset = 0;
			Vector256<float> extremes;
			extremes = new Vector<float>(doMax ? float.MinValue : float.MaxValue).AsVector256();
			Vector256<int> indices = new Vector<int>(stackalloc int[Vector256<int>.Count].FillWithRange(0)).AsVector256();
			Vector256<int> extremeIndices = indices;
			Vector256<int> increment = new Vector<int>(Vector256<int>.Count).AsVector256();
			// loop
			while (lengthLeft >= Vector256<int>.Count)
			{
				indices = Avx2.Add(indices, increment);
				Vector256<float> current1 = LoadVector256<float, Complex<float>>(ref a[offset]);
				Vector256<float> current2 = LoadVector256<float, Complex<float>>(ref a[offset + Vector256<float>.Count]);
				current1 = Avx.Multiply(current1, current1);
				current2 = Avx.Multiply(current2, current2);
				Vector256<float> squares = Avx.HorizontalAdd(current1, current2);
				/*
				// This is for complex multiply
				Vector256<float> realParts, imagParts;
				realParts = Avx.UnpackLow(current1, current2); // vunpcklpd       ymm2, ymm0, ymm1
				imagParts = Avx.UnpackHigh(current1, current2);// vunpcklpd       ymm3, ymm0, ymm1
				current1 = Avx.Multiply(realParts, realParts); // vmulpd          ymm0, ymm2, ymm2
				current2 = Avx.Multiply(imagParts, imagParts); // vmulpd          ymm1, ymm3, ymm3

				vfmsub231pd     ymm1, ymm2, ymm2   # real*real - imag*imag
				vaddpd          ymm0, ymm0, ymm0   # imag+imag = 2*imag
				vmulpd          ymm0, ymm2, ymm0   # 2*imag * real
				vunpcklpd       ymm2, ymm1, ymm0
				vunpckhpd       ymm0, ymm1, ymm0
				*/
				Vector256<float> compare;
				if (doMax)
				{   // abs max
					compare = Avx.CompareGreaterThan(squares, extremes);
					extremes = Avx.Max(squares, extremes);
				}
				else
				{   // abs min
					compare = Avx.CompareLessThan(squares, extremes);
					extremes = Avx.Max(squares, extremes);
				}
				extremeIndices = Avx2.BlendVariable(indices, extremeIndices, compare.AsInt32());
				lengthLeft -= Vector<float>.Count;
				offset += Vector<float>.Count;
			}
			// reduce main
			float extreme = ((float*)&extremes)[0]; int extremeIndex = ((int*)&extremeIndices)[0];
			for (int i = 1; i < Vector<float>.Count; i++)
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
					float v = ((float*)px)[offset];
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
		private static unsafe long VectorMinMaxCompexDouble<Test>(void* px, int length)
		{
			Span<Complex<double>> a = new(px, length);
			bool doMax = typeof(Test) == typeof(int);
			// initialize
			int lengthLeft = length, offset = 0;
			Vector256<double> extremes;
			extremes = new Vector<double>(doMax ? double.MinValue : double.MaxValue).AsVector256();
			Vector256<long> indices = new Vector<long>(stackalloc long[Vector256<long>.Count].FillWithRange(0)).AsVector256();
			Vector256<long> extremeIndices = indices;
			Vector256<long> increment = new Vector<long>(Vector256<long>.Count).AsVector256();
			// loop
			while (lengthLeft >= Vector256<long>.Count)
			{
				indices = Avx2.Add(indices, increment);
				Vector256<double> current1 = LoadVector256<double, Complex<double>>(ref a[offset]);
				Vector256<double> current2 = LoadVector256<double, Complex<double>>(ref a[offset + Vector256<double>.Count]);
				current1 = Avx.Multiply(current1, current1);
				current2 = Avx.Multiply(current2, current2);
				Vector256<double> squares = Avx.HorizontalAdd(current1, current2);
				Vector256<double> compare;
				if (doMax)
				{   // abs max
					compare = Avx.CompareGreaterThan(squares, extremes);
					extremes = Avx.Max(squares, extremes);
				}
				else
				{   // abs min
					compare = Avx.CompareLessThan(squares, extremes);
					extremes = Avx.Max(squares, extremes);
				}
				extremeIndices = Avx2.BlendVariable(indices, extremeIndices, compare.AsInt64());
				lengthLeft -= Vector<double>.Count;
				offset += Vector<double>.Count;
			}
			// reduce main
			double extreme = ((double*)&extremes)[0]; long extremeIndex = ((long*)&extremeIndices)[0];
			for (int i = 1; i < Vector<double>.Count; i++)
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
					double v = ((double*)px)[offset];
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
		private static unsafe bool VectorMinMaxManaged<Test>(void* px, int length, out long index)
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static unsafe bool AbsoluteValueArgMax<T>(Storage<T> x, out long index) where T : unmanaged
		{
			index = -1;
			if (!GetSpan(x, out void* px, out int length))
				return false;
			if (length == 0)
				return true;
			if (length == 1)
			{
				index = 0; return true;
			}
			if (!Vector.IsHardwareAccelerated)
				return VectorMinMaxManaged<int>(px, length, out index);
			if ((sizeof(T) <= sizeof(byte) && length > sbyte.MaxValue) || (sizeof(T) <= sizeof(short) && length > short.MaxValue))
				return VectorMinMaxManaged<int>(px, length, out index);

			if (Const<T>.IsComplex)
			{
				if (Const<T>.IsIntegralType || !Avx2.IsSupported)
					return VectorMinMaxManaged<int>(px, length, out index);
				if (typeof(T) == typeof(float))
				{
					index = VectorMinMaxCompexFloat<int>(px, length);
				}
				else // double
				{
					index = VectorMinMaxCompexDouble<int>(px, length);
				}
			}
			else
			{
				if (typeof(T) == typeof(float))
				{
					index = VectorMinMaxReal<float, int, int>(px, length);
				}
				else if (typeof(T) == typeof(double))
				{
					index = VectorMinMaxReal<double, long, int>(px, length);
				}
				else
				{   // integral type
					index = VectorMinMaxReal<T, T, int>(px, length);
				}
			}
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static bool AbsoluteValueArgMin<T>(Storage<T> x, out long index) where T : unmanaged
		{

		}
		#endregion

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static bool AbsoluteValueSum<T>(Storage<T> x, out double sum) where T : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static bool AggregateProduct<T>(Storage<T> x, out T product) where T : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static bool AggregateSum<T>(Storage<T> x, out T sum) where T : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static unsafe bool Dot<T>(bool conjX, Storage<T> x, Storage<T> y, out T dot) where T : unmanaged
		{
			dot = default;
			if (!GetSpan(x, out void* px, out int lenx))
				return false;
			if (!GetSpan(y, out void* py, out int leny))
				return false;

			int length = Math.Min(lenx, leny);
			if (length == 0)
				return true;
			if (!Vector.IsHardwareAccelerated)
				return false; // not support
			// create span
			Span<T> a = new(px, length), b = new(py, length);
			// reduce to Vector<T>.Count doubles
			Vector<T> multiplyResult;
			Vector<T> sum = Vector<T>.Zero;
			int lengthLeft = length, offset = 0;
			while (lengthLeft >= Vector<T>.Count)
			{
				multiplyResult = LoadVector(ref a[offset]) * LoadVector(ref b[offset]);
				sum += multiplyResult;
				lengthLeft -= Vector<T>.Count;
				offset += Vector<T>.Count;
			}
			// reduce left
			T dotLeft = default;
			if (lengthLeft > 0)
			{
				Vector<T> leftA = Vector<T>.Zero, leftB = Vector<T>.Zero;
				// the following two lines shall be unrolled by JIT at runtime
				a[offset..].CopyTo(new((T*)&leftA, Vector<T>.Count));
				b[offset..].CopyTo(new((T*)&leftB, Vector<T>.Count));
				dotLeft = Vector.Dot(leftA, leftB);
				// this implementation has some performance loss compare to the direct dot
				// but it is suitable for all generic type T that Vector<T> supports
			}
			// this implementation has some performance loss, same reason as above
			T dotMain = Vector.Dot(sum, Vector<T>.One);
			// return
			dot = dotMain.GenericAdd(dotLeft);
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static bool Norm<T>(Storage<T> x, out double norm) where T : unmanaged
		{
			norm = 0;
			if (!Dot(conjX: true, x, x, out T dot))
				return false;
			norm = dot.ToDouble();
			return true;
		}

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
		internal protected static bool PointWiseModular<T>(Storage<T> x, T mod) where T : unmanaged
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
			if (methodInfo.Name == nameof(PointWiseModular) && inputParams.Length == 2)
			{
				if (inputParams[0] is IStorage s && s.GetType() is { IsGenericType: true } ts)
				{
					var t = ts.GenericTypeArguments[0];
					if (methodInfo[1].Equals(t.TypeHandle) && t.IsPrimitive)
					{
						// invoke method
						return Type.GetTypeCode(t) switch
						{
							TypeCode.Char => PointWiseModular((Storage<char>)s, (char)inputParams[1]),
							TypeCode.SByte => PointWiseModular((Storage<sbyte>)s, (sbyte)inputParams[1]),
							TypeCode.Byte => PointWiseModular((Storage<byte>)s, (byte)inputParams[1]),
							TypeCode.Int16 => PointWiseModular((Storage<short>)s, (short)inputParams[1]),
							TypeCode.UInt16 => PointWiseModular((Storage<ushort>)s, (ushort)inputParams[1]),
							TypeCode.Int32 => PointWiseModular((Storage<int>)s, (int)inputParams[1]),
							TypeCode.UInt32 => PointWiseModular((Storage<uint>)s, (uint)inputParams[1]),
							TypeCode.Int64 => PointWiseModular((Storage<long>)s, (long)inputParams[1]),
							TypeCode.UInt64 => PointWiseModular((Storage<ulong>)s, (ulong)inputParams[1]),
							_ => false,
						};
					}
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
		public override bool SchurDecomposition_<T>(SolveVectorMode jobu, long n, Storage<T> A, long lda, Storage<T>? U, long ldu, out long actualNumber, Storage<ComplexDouble>? orderVal = null) { actualNumber = 0; return false; }
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
