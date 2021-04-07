using System;
using System.Runtime.CompilerServices;
using System.Numerics;
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
	/// The C# back-end of <see cref="AbstractApi"/> that utilizes <see cref="System.Runtime.Intrinsics"/>.<br/>
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
		private static Vector256<T> LoadVector256<T>(ref T r) where T : unmanaged
		{
			return Unsafe.ReadUnaligned<Vector256<T>>(ref Unsafe.As<T, byte>(ref r));
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Vector128<T> LoadVector128<T>(ref T r) where T : unmanaged
		{
			return Unsafe.ReadUnaligned<Vector128<T>>(ref Unsafe.As<T, byte>(ref r));
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Vector<T> LoadVector<T>(ref T r) where T : unmanaged
		{
			return Unsafe.ReadUnaligned<Vector<T>>(ref Unsafe.As<T, byte>(ref r));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe double ReduceVector256(Vector256<double> input)
		{
			Vector256<double> temp = Avx.HorizontalAdd(input, input);
			Vector128<double> sum_high = Avx.ExtractVector128(temp, 1);
			Vector128<double> result = Sse2.Add(sum_high, LoadVector128(ref Unsafe.As<Vector256<double>, double>(ref temp)));
			return *(double*)&result;
		}
		#endregion

		#region static
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool AbsoluteValueArgMax<T>(Storage<T> x, out long index) where T : unmanaged
		{
			
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool AbsoluteValueArgMin<T>(Storage<T> x, out long index) where T : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool AbsoluteValueSum<T>(Storage<T> x, out double sum) where T : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool AggregateProduct<T>(Storage<T> x, out T product) where T : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool AggregateSum<T>(Storage<T> x, out T sum) where T : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static unsafe bool Dot<T>(bool conjX, Storage<T> x, Storage<T> y, out T dot) where T : unmanaged
		{
			dot = default;
			if (!GetSpan(x, out void* px, out int lenx))
				return false;
			if (!GetSpan(y, out void* py, out int leny))
				return false;

			int length = Math.Min(lenx, leny);
			// sample case
			if (Const<T>.DataType == DataType.RealDouble)
			{
				double result;
				Span<double> xx = new(px, length), yy = new(py, length);
				if (Sse2.IsSupported)
				{
					if (Avx.IsSupported && length >= Vector256<double>.Count)
					{
						#region AVX
						// reduce to 4 doubles
						Vector256<double> multiplyResult;
						Vector256<double> sum = Vector256<double>.Zero;
						int lengthLeft = length, offset = 0;
						while (lengthLeft >= Vector256<double>.Count)
						{
							multiplyResult = Avx.Multiply(LoadVector256(ref xx[offset]), LoadVector256(ref yy[offset]));
							sum = Avx.Add(sum, multiplyResult);
							lengthLeft -= Vector256<double>.Count;
							offset += Vector256<double>.Count;
						}
						// reduce left
						double left = 0;
						if (lengthLeft > 0)
						{
							for (; offset < length; offset++)
							{
								left += xx[offset] * yy[offset];
							}
						}
						result = left + ReduceVector256(sum);
						#endregion
					}
					else if (length >= Vector128<double>.Count)
					{
						#region SSE2
						// reduce to 4 doubles
						Vector128<double> multiplyResult;
						Vector128<double> sum = Vector128<double>.Zero;
						int lengthLeft = length, offset = 0;
						while (lengthLeft >= Vector128<double>.Count)
						{
							multiplyResult = Sse2.Multiply(LoadVector128(ref xx[offset]), LoadVector128(ref yy[offset]));
							sum = Sse2.Add(sum, multiplyResult);
							lengthLeft -= Vector128<double>.Count;
							offset += Vector128<double>.Count;
						}
						// reduce left
						double left = 0;
						if (lengthLeft > 0)
						{
							left += xx[offset] * yy[offset];
						}
						double* temp = (double*)&sum;
						result = left + temp[0] + temp[1];
						#endregion
					}
				}
				else if (Vector.IsHardwareAccelerated && length >= Vector<T>.Count)
				{
					#region Numerics.Vector

					#endregion
				}
				dot = *(T*)&result;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool Norm<T>(Storage<T> x, out double norm) where T : unmanaged
		{
			norm = 0;
			if (!Dot(conjX: true, x, x, out T dot))
				return false;
			norm = dot.ToDouble();
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool PartialProduct<T>(Storage<T> x, Storage<T> y, bool inclusive) where T : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool PartialSum<T>(Storage<T> x, Storage<T> y, bool inclusive) where T : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool PointWiseAddScalar<T>(Storage<T> x, T scalr) where T : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool PointWiseModular<T>(Storage<T> x, T mod) where T : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool PointWiseCast<T, TOut>(Storage<T> source, Storage<TOut> destination) where T : unmanaged where TOut : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool PointWiseConjugate<T>(Storage<T> x) where T : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool PointWiseDivide<T>(Storage<T> x, Storage<T> y) where T : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool PointWiseEquals<T>(Storage<T> x, Storage<T> y, out bool equals) where T : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool PointWiseMultiply<T>(Storage<T> x, Storage<T> y) where T : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool PointWisePower<T>(Storage<T> x, double p) where T : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool PointWisePower<T>(Storage<T> x, T p) where T : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool Scale<T>(Storage<T> x, T scalar) where T : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal new static bool TruncateArray<T>(Storage<T> x, double threshold) where T : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool VectorGeneralAdd<T>(T α, Storage<T> x, Storage<T> y) where T : unmanaged
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
