using System;
using System.Runtime.CompilerServices;

using Althea.Arrays;
using Althea.LinearAlgebra;
using Althea.LinearAlgebra.Sparse;
using Althea.NativeTypes;


#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释
namespace Althea.Backend.Cuda.LinearAlgebra.Sparse
{
	/// <summary>
	/// The CUDA back-end of the sparse linear algebra <see cref="IAbstractApi"/> that utilizes cuSPARSE. Since the cuSPARSE APIs and underlying types vary drastically from CUDA 10.1 to 11.3 and probably will still be. Therefore, the <see cref="SparseApi"/> is currently not available.
	/// </summary>
	/// <remarks>Older versions are not supported since the cuSPARSE library's APIs are updating rapidly and the current APIs may not last long.</remarks>
	public class SparseApi : IAbstractApi
	{
		#region basic

		protected override void Dispose(bool disposeManaged) => throw new NotImplementedException();
		#endregion

		protected override bool IndexBound_<TInd>(Storage<TInd> array, TInd value, bool lowerBound, out long index) => throw new NotImplementedException();
		protected override bool IndexFind_<TInd>(bool sorted, Storage<TInd> array, TInd value, out long find) => throw new NotImplementedException();
		protected override bool IndexGenerateFromBounds_<TInd, TIndOut>(Storage<TInd> bounds, Storage<TIndOut> target, bool lowerBound, TIndOut start = default) => throw new NotImplementedException();
		protected override bool IndexGetAllBounds_<TInd, TIndOut>(Storage<TInd> array, Storage<TIndOut> target, TInd start, TInd end, bool lowerBound) => throw new NotImplementedException();
		protected override bool IndexMax_<TInd>(Storage<TInd> array, out TInd max) => throw new NotImplementedException();
		protected override bool IndexMin_<TInd>(Storage<TInd> array, out TInd min) => throw new NotImplementedException();
		protected override bool IsSupportedIndexVectorBinary(CombinationOfLocations location1, CombinationOfLocations location2) => throw new NotImplementedException();
		protected override bool IsSupportedIndexVectorUnary(CombinationOfLocations location) => throw new NotImplementedException();
		protected override bool IsSupportedMatrixBinary(CombinationOfLocations location1, CombinationOfLocations location2) => throw new NotImplementedException();
		protected override bool IsSupportedMatrixIndexType(DataType indexType) => throw new NotImplementedException();
		protected override bool IsSupportedMatrixTrinary(CombinationOfLocations location1, CombinationOfLocations location2, CombinationOfLocations location3) => throw new NotImplementedException();
		protected override bool IsSupportedMatrixUnary(CombinationOfLocations location) => throw new NotImplementedException();
		protected override bool IsSupportedSparseMatrix<T>(ISparseMatrix<T> matrix) => throw new NotImplementedException();
		protected override bool IsSupportedSparseVector<T>(ISparseVector<T> vector) => throw new NotImplementedException();
		protected override bool IsSupportedVectorBinary(CombinationOfLocations location1, CombinationOfLocations location2) => throw new NotImplementedException();
		protected override bool IsSupportedVectorBinaryMatrixUnary(CombinationOfLocations vector1, CombinationOfLocations vector2, CombinationOfLocations matrix) => throw new NotImplementedException();
		protected override bool IsSupportedVectorIndexType(DataType indexType) => throw new NotImplementedException();
		protected override bool IsSupportedVectorMatrixIndexType(DataType vectorIndex, DataType matrixIndex) => throw new NotImplementedException();
		protected override bool IsSupportedVectorUnary(CombinationOfLocations location) => throw new NotImplementedException();
		protected override bool IsSupportedVectorUnaryMatrixBinary(CombinationOfLocations vector, CombinationOfLocations matrix1, CombinationOfLocations matrix2) => throw new NotImplementedException();
		protected override bool IsSupportedVectorUnaryMatrixUnary(CombinationOfLocations vector, CombinationOfLocations matrix) => throw new NotImplementedException();
		protected override bool MatrixDenseAddSparse_<T>(MatrixOperation opA, MatrixOperation opB, T α, Storage<T> A, long lda, T β, ISparseMatrix<T> B, Storage<T> C, long ldc) => throw new NotImplementedException();
		protected override bool MatrixDenseMultiplySparse_<T>(MatrixOperation opA, MatrixOperation opB, long m, T α, Storage<T> A, long lda, ISparseMatrix<T> B, T β, Storage<T> C, long ldc) => throw new NotImplementedException();
		protected override bool MatrixDenseMultiplyVectorSparse_<T>(MatrixOperation op, T α, long m, Storage<T> M, long ldm, ISparseVector<T> x, T β, Storage<T> y) => throw new NotImplementedException();
		protected override bool MatrixDenseToSparse_<T>(long m, long n, Storage<T> source, long ld, SparseMatrixFormat format, out SparseArrayWrapper<T> target, float threshold = 0) => throw new NotImplementedException();
		protected override bool MatrixSparseAddSparse_<T>(MatrixOperation opA, MatrixOperation opB, T α, ISparseMatrix<T>? A, T β, ISparseMatrix<T>? B, out SparseArrayWrapper<T> target, SparseMatrixFormat format = (SparseMatrixFormat)(-1)) => throw new NotImplementedException();
		protected override bool MatrixSparseFormatConvert_<T>(ISparseMatrix<T> source, SparseMatrixFormat format, out SparseArrayWrapper<T> target, IOtherInfo? otherInfo = null) => throw new NotImplementedException();
		protected override bool MatrixSparseKronecker_<T>(ISparseMatrix<T> A, ISparseMatrix<T> B, out SparseArrayWrapper<T> target, SparseMatrixFormat format = (SparseMatrixFormat)(-1)) => throw new NotImplementedException();
		protected override bool MatrixSparseMultiplyDense_<T>(MatrixOperation opA, MatrixOperation opB, long n, T α, ISparseMatrix<T> A, Storage<T> B, long ldb, T β, Storage<T> C, long ldc) => throw new NotImplementedException();
		protected override bool MatrixSparseMultiplySparse_<T>(MatrixOperation opA, MatrixOperation opB, T α, ISparseMatrix<T> A, ISparseMatrix<T> B, T β, ISparseMatrix<T>? C, out SparseArrayWrapper<T> target, SparseMatrixFormat format = (SparseMatrixFormat)(-1)) => throw new NotImplementedException();
		protected override bool MatrixSparseMultiplyVectorDense_<T>(MatrixOperation op, T α, ISparseMatrix<T> M, Storage<T> x, T β, Storage<T> y) => throw new NotImplementedException();
		protected override bool MatrixSparsePrune_<T>(ISparseMatrix<T> source, float threshold, out SparseArrayWrapper<T> target) => throw new NotImplementedException();
		protected override bool MatrixSparseReshape_<T>(ISparseMatrix<T> source, long newNRows, out SparseArrayWrapper<T> target) => throw new NotImplementedException();
		protected override bool MatrixSparseToDense_<T>(ISparseMatrix<T> source, Storage<T> destination, long ld) => throw new NotImplementedException();
		protected override bool SparseMatrixSetSlice_<T>(ISparseMatrix<T> matrix, MatrixSliceWrapper slice, ISparseMatrix<T> sub) => throw new NotImplementedException();
		protected override bool SparseMatrixSlice_<T>(ISparseMatrix<T> matrix, MatrixSliceWrapper slice, out SparseArrayWrapper<T> sub) => throw new NotImplementedException();
		protected override bool SparseMatrixSlice_<T>(ISparseMatrix<T> matrix, MatrixSliceWrapper slice, ISparseMatrix<T> sub) => throw new NotImplementedException();
		protected override bool SparseMatrixSlice_<T>(ISparseMatrix<T> matrix, MatrixSliceWrapper slice, Storage<T> sub, long subLD) => throw new NotImplementedException();
		protected override bool SparseMatrixToVector_<T>(ISparseMatrix<T> matrix, SparseVectorFormat format, out SparseArrayWrapper<T> target) => throw new NotImplementedException();
		protected override bool SparseVectorToMatrix_<T>(ISparseVector<T> vector, long rows, SparseMatrixFormat format, out SparseArrayWrapper<T> target) => throw new NotImplementedException();
		protected override bool VectorDenseToSparse_<T>(Storage<T> x, SparseVectorFormat format, out SparseArrayWrapper<T> target, float threshold = 0) => throw new NotImplementedException();
		protected override bool VectorGatherValuesAt_<T>(Storage<T> x, ISparseVector<T> y) => throw new NotImplementedException();
		protected override bool VectorSetValuesAt_<T, TInd>(Storage<T> x, T value, Storage<TInd> positions) => throw new NotImplementedException();
		protected override bool VectorSparseAddSparse_<T>(ISparseVector<T> x, ISparseVector<T> y, out SparseArrayWrapper<T> target, SparseVectorFormat format = (SparseVectorFormat)(-1)) => throw new NotImplementedException();
		protected override bool VectorSparseAddToDense_<T>(T α, ISparseVector<T> x, Storage<T> y) => throw new NotImplementedException();
		protected override bool VectorSparseDotDense_<T>(bool conjX, ISparseVector<T> x, Storage<T> y, out T dot) => throw new NotImplementedException();
		protected override bool VectorSparseDotSparse_<T>(bool conjX, ISparseVector<T> x, ISparseVector<T> y, out T dot) => throw new NotImplementedException();
		protected override bool VectorSparseOuter_<T>(bool conjY, ISparseVector<T> x, ISparseVector<T> y, out SparseArrayWrapper<T> target, SparseMatrixFormat format = (SparseMatrixFormat)(-1)) => throw new NotImplementedException();
		protected override bool VectorSparsePointWiseDivideDense_<T>(ISparseVector<T> x, Storage<T> y) => throw new NotImplementedException();
		protected override bool VectorSparsePointWiseMultiplyDense_<T>(ISparseVector<T> x, Storage<T> y) => throw new NotImplementedException();
		protected override bool VectorSparseToDense_<T>(ISparseVector<T> x, Storage<T> y) => throw new NotImplementedException();
	}
}
