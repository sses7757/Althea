using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;

using Althea.Linq;
using Althea.Helpers;
using Althea.LinearAlgebra;
using Althea.LinearAlgebra.Sparse;
using Althea.Storage;
using Althea.NativeTypes;

using Blas = Althea.LinearAlgebra.Dense.BlasApiSelector;
using ExtBlas = Althea.LinearAlgebra.Dense.ExtendBlasApiSelector;
using SpComp = Althea.LinearAlgebra.Sparse.ComputationApiSelector;
using SpConv = Althea.LinearAlgebra.Sparse.ConversionApiSelector;


namespace Althea.Arrays
{
	/// <summary>
	/// The static class for sparse linear algebra and tensor algebra operations of same data type and storage type.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TS">The storage type used by the value storage</typeparam>
	/// <typeparam name="TInd">Any unmanaged integer number as the index type</typeparam>
	/// <typeparam name="TSInd">The index storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
	public sealed class SparseOperation<T, TInd, TS, TSInd> :
		IVectorOperations<T, DenseVector<T, TS>, SparseVector<T, TInd, TS, TSInd>>,

		IMatrixVectorMultiplyOperations<T, SparseVector<T, TInd, TS, TSInd>, DenseVector<T, TS>, DenseMatrix<T, TS>>,
		IMatrixVectorMultiplyOperations<T, DenseVector<T, TS>, DenseVector<T, TS>, SparseMatrix<T, TInd, TS, TSInd>>,

		IMatrixSetDiagonalVector<T, SparseVector<T, TInd, TS, TSInd>, DenseMatrix<T, TS>>,
		IMatrixGetDiagonalVector<T, SparseVector<T, TInd, TS, TSInd>, SparseMatrix<T, TInd, TS, TSInd>>,
		IMatrixGetDiagonalVectorVariant<T, SparseVector<T, TInd, TS, TSInd>, SparseMatrix<T, TInd, TS, TSInd>>,
		IMatrixSetDiagonalVector<T, SparseVector<T, TInd, TS, TSInd>, SparseMatrix<T, TInd, TS, TSInd>>,

		IMatrixOperations<T, DenseMatrix<T, TS>, SparseMatrix<T, TInd, TS, TSInd>, DenseMatrix<T, TS>>,
		IMatrixOperations<T, SparseMatrix<T, TInd, TS, TSInd>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>,
		IMatrixOperations<T, SparseMatrix<T, TInd, TS, TSInd>, SparseMatrix<T, TInd, TS, TSInd>, SparseMatrix<T, TInd, TS, TSInd>>

		where T : unmanaged, INumber<T> where TInd : unmanaged, IBinaryInteger<TInd>
		where TS : class, IStorage<T, TS> where TSInd : class, IStorage<TInd, TSInd>
	{

	}
}
