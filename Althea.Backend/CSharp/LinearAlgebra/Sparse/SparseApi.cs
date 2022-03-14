using System.Runtime.CompilerServices;

using Althea.Arrays;
using Althea.LinearAlgebra;
using Althea.LinearAlgebra.Sparse;
using Althea.NativeTypes;


namespace Althea.Backend.CSharp.LinearAlgebra
{
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释
	public class SparseApi : IAbstractApi
	{
		#region basic
		public SparseApi()
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
		protected override bool IsSupportedIndexVectorBinary(CombinationOfLocations location1, CombinationOfLocations location2) => Supported(location1) && Supported(location2);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedIndexVectorUnary(CombinationOfLocations location) => Supported(location);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedMatrixBinary(CombinationOfLocations location1, CombinationOfLocations location2) => false;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedMatrixIndexType(DataType indexType) => false;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedMatrixTrinary(CombinationOfLocations location1, CombinationOfLocations location2, CombinationOfLocations location3) => false;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedMatrixUnary(CombinationOfLocations location) => false;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedSparseMatrix<T>(ISparseMatrix<T> matrix) => false;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedSparseVector<T>(ISparseVector<T> vector) => false;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedVectorBinary(CombinationOfLocations location1, CombinationOfLocations location2) => false;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedVectorBinaryMatrixUnary(CombinationOfLocations vector1, CombinationOfLocations vector2, CombinationOfLocations matrix) => false;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedVectorIndexType(DataType indexType) => false;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedVectorMatrixIndexType(DataType vectorIndex, DataType matrixIndex) => false;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedVectorUnary(CombinationOfLocations location) => false;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedVectorUnaryMatrixBinary(CombinationOfLocations vector, CombinationOfLocations matrix1, CombinationOfLocations matrix2) => false;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedVectorUnaryMatrixUnary(CombinationOfLocations vector, CombinationOfLocations matrix) => false;
		#endregion

		#region index related
		protected override bool IndexBound_<TInd>(Storage<TInd> array, TInd value, bool lowerBound, out long index)
		{
			return DenseApi.IndexBound(array, value, lowerBound, out index);
		}

		protected override bool IndexFind_<TInd>(bool sorted, Storage<TInd> array, TInd value, out long find)
		{
			return DenseApi.IndexFind(sorted, array, value, out find);
		}

		protected override bool IndexGenerateFromBounds_<TInd, TIndOut>(Storage<TInd> bounds, Storage<TIndOut> target, bool lowerBound, TIndOut start = default)
		{
			return DenseApi.IndexGenerateFromBounds(bounds, target, lowerBound, start);
		}

		protected override bool IndexGetAllBounds_<TInd, TIndOut>(Storage<TInd> array, Storage<TIndOut> target, TInd start, TInd end, bool lowerBound)
		{
			return DenseApi.IndexGetAllBounds(array, target, start, end, lowerBound);
		}

		protected override bool IndexMax_<TInd>(Storage<TInd> array, out TInd max)
		{
			return DenseApi.Max(array, out max);
		}

		protected override bool IndexMin_<TInd>(Storage<TInd> array, out TInd min)
		{
			return DenseApi.Min(array, out min);
		}
		#endregion

		#region not supported sparse vector and matrix related
		protected override bool MatrixDenseAddSparse_<T>(MatrixOperation opA, MatrixOperation opB, T α, Storage<T> A, long lda, T β, ISparseMatrix<T> B, Storage<T> C, long ldc) => false;
		protected override bool MatrixDenseMultiplySparse_<T>(MatrixOperation opA, MatrixOperation opB, long m, T α, Storage<T> A, long lda, ISparseMatrix<T> B, T β, Storage<T> C, long ldc) => false;
		protected override bool MatrixDenseMultiplyVectorSparse_<T>(MatrixOperation op, T α, long m, Storage<T> M, long ldm, ISparseVector<T> x, T β, Storage<T> y) => false;
		protected override bool MatrixDenseToSparse_<T>(long m, long n, Storage<T> source, long ld, SparseMatrixFormat format, out SparseArrayWrapper<T> target, float threshold = 0) {
			target = default;
			return false;
		}
		protected override bool MatrixSparseAddSparse_<T>(MatrixOperation opA, MatrixOperation opB, T α, ISparseMatrix<T>? A, T β, ISparseMatrix<T>? B, out SparseArrayWrapper<T> target, SparseMatrixFormat format = (SparseMatrixFormat)(-1))
		{
			target = default;
			return false;
		}
		protected override bool MatrixSparseFormatConvert_<T>(ISparseMatrix<T> source, SparseMatrixFormat format, out SparseArrayWrapper<T> target, IOtherInfo? otherInfo = null)
		{
			target = default;
			return false;
		}
		protected override bool MatrixSparseKronecker_<T>(ISparseMatrix<T> A, ISparseMatrix<T> B, out SparseArrayWrapper<T> target, SparseMatrixFormat format = (SparseMatrixFormat)(-1))
		{
			target = default;
			return false;
		}
		protected override bool MatrixSparseMultiplyDense_<T>(MatrixOperation opA, MatrixOperation opB, long n, T α, ISparseMatrix<T> A, Storage<T> B, long ldb, T β, Storage<T> C, long ldc) => false;
		protected override bool MatrixSparseMultiplySparse_<T>(MatrixOperation opA, MatrixOperation opB, T α, ISparseMatrix<T> A, ISparseMatrix<T> B, T β, ISparseMatrix<T>? C, out SparseArrayWrapper<T> target, SparseMatrixFormat format = (SparseMatrixFormat)(-1))
		{
			target = default;
			return false;
		}
		protected override bool MatrixSparseMultiplyVectorDense_<T>(MatrixOperation op, T α, ISparseMatrix<T> M, Storage<T> x, T β, Storage<T> y) => false;
		protected override bool MatrixSparsePrune_<T>(ISparseMatrix<T> source, float threshold, out SparseArrayWrapper<T> target)
		{
			target = default;
			return false;
		}
		protected override bool MatrixSparseReshape_<T>(ISparseMatrix<T> source, long newNRows, out SparseArrayWrapper<T> target)
		{
			target = default;
			return false;
		}
		protected override bool MatrixSparseToDense_<T>(ISparseMatrix<T> source, Storage<T> destination, long ld) => false;
		protected override bool SparseMatrixSetSlice_<T>(ISparseMatrix<T> matrix, MatrixSliceWrapper slice, ISparseMatrix<T> sub) => false;
		protected override bool SparseMatrixSlice_<T>(ISparseMatrix<T> matrix, MatrixSliceWrapper slice, out SparseArrayWrapper<T> sub)
		{
			sub = default;
			return false;
		}
		protected override bool SparseMatrixSlice_<T>(ISparseMatrix<T> matrix, MatrixSliceWrapper slice, ISparseMatrix<T> sub) => false;
		protected override bool SparseMatrixSlice_<T>(ISparseMatrix<T> matrix, MatrixSliceWrapper slice, Storage<T> sub, long subLD) => false;
		protected override bool SparseMatrixToVector_<T>(ISparseMatrix<T> matrix, SparseVectorFormat format, out SparseArrayWrapper<T> target)
		{
			target = default;
			return false;
		}
		protected override bool SparseVectorToMatrix_<T>(ISparseVector<T> vector, long rows, SparseMatrixFormat format, out SparseArrayWrapper<T> target)
		{
			target = default;
			return false;
		}
		protected override bool VectorDenseToSparse_<T>(Storage<T> x, SparseVectorFormat format, out SparseArrayWrapper<T> target, float threshold = 0)
		{
			target = default;
			return false;
		}
		protected override bool VectorGatherValuesAt_<T>(Storage<T> x, ISparseVector<T> y) => false;
		protected override bool VectorSetValuesAt_<T, TInd>(Storage<T> x, T value, Storage<TInd> positions) => false;
		protected override bool VectorSparseAddSparse_<T>(ISparseVector<T> x, ISparseVector<T> y, out SparseArrayWrapper<T> target, SparseVectorFormat format = (SparseVectorFormat)(-1))
		{
			target = default;
			return false;
		}
		protected override bool VectorSparseAddToDense_<T>(T α, ISparseVector<T> x, Storage<T> y) => false;
		protected override bool VectorSparseDotDense_<T>(bool conjX, ISparseVector<T> x, Storage<T> y, out T dot)
		{
			dot = default;
			return false;
		}
		protected override bool VectorSparseDotSparse_<T>(bool conjX, ISparseVector<T> x, ISparseVector<T> y, out T dot)
		{
			dot = default;
			return false;
		}
		protected override bool VectorSparseOuter_<T>(bool conjY, ISparseVector<T> x, ISparseVector<T> y, out SparseArrayWrapper<T> target, SparseMatrixFormat format = (SparseMatrixFormat)(-1))
		{
			target = default;
			return false;
		}
		protected override bool VectorSparsePointWiseDivideDense_<T>(ISparseVector<T> x, Storage<T> y) => false;
		protected override bool VectorSparsePointWiseMultiplyDense_<T>(ISparseVector<T> x, Storage<T> y) => false;
		protected override bool VectorSparseToDense_<T>(ISparseVector<T> x, Storage<T> y) => false;
		#endregion
	}
#pragma warning restore CS1591 // 缺少对公共可见类型或成员的 XML 注释
}
