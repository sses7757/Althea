using Althea.LinearAlgebra;
using Althea.LinearAlgebra.Sparse;
using Althea.Storage;
using Althea.TensorAlgebra;
using Althea.TensorAlgebra.Dense;

using SpComp = Althea.LinearAlgebra.Sparse.ComputationApiSelector;
using SpTen = Althea.TensorAlgebra.Sparse.ApiSelector;


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
		IMatrixOperations<T, SparseMatrix<T, TInd, TS, TSInd>, SparseMatrix<T, TInd, TS, TSInd>, SparseMatrix<T, TInd, TS, TSInd>>,

		ITensorOperations<T, SparseTensor<T, TInd, TS, TSInd>, DenseTensor<T, TS>>,
		ITensorOperations<T, SparseTensor<T, TInd, TS, TSInd>, SparseTensor<T, TInd, TS, TSInd>>,
		ITensorOperations<T, SparseTensor<T, TInd, TS, TSInd>, DenseTensor<T, TS>, DenseTensor<T, TS>>,
		ITensorOperations<T, SparseTensor<T, TInd, TS, TSInd>, SparseTensor<T, TInd, TS, TSInd>, SparseTensor<T, TInd, TS, TSInd>>,
		ITensorOperations<T, DenseTensor<T, TS>, SparseTensor<T, TInd, TS, TSInd>, DenseTensor<T, TS>>

		where T : unmanaged, INumber<T> where TInd : unmanaged, IBinaryInteger<TInd>
		where TS : class, IStorage<T, TS> where TSInd : class, IStorage<TInd, TSInd>
	{
		#region operations
		/// <inheritdoc/>
		public static T Dot(DenseVector<T, TS> left, SparseVector<T, TInd, TS, TSInd> right, bool conjugateLeft = true)
		{
			if (left.Length != right.Length)
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(right));
			return SpComp.VectorSparseDotDense(conjugateLeft, right, left.Storage, left.Stride);
		}

		/// <summary>
		/// Statically compute the dot (inner) product of <paramref name="left"/> and <paramref name="right"/>.
		/// </summary>
		/// <param name="left">The left vector to perform the dot product</param>
		/// <param name="right">The right vector to perform the dot product</param>
		/// <param name="conjugateLeft">Whether the dot product is performed on the conjugation of <paramref name="left"/> or directly.</param>
		/// <returns>The dot (inner) product result as a <typeparamref name="T"/></returns>
		public static T Dot(SparseVector<T, TInd, TS, TSInd> left, SparseVector<T, TInd, TS, TSInd> right, bool conjugateLeft = true)
		{
			if (left.Length != right.Length)
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(right));
			return SpComp.VectorSparseDotSparse(conjugateLeft, right, left);
		}

		/// <inheritdoc/>
		public static void AddBy(DenseVector<T, TS> left, SparseVector<T, TInd, TS, TSInd> right, T scalar)
		{
			if (left.Length != right.Length)
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(right));
			SpComp.VectorSparseAddToDense(scalar, right, left.Storage, left.Stride);
		}

		/// <inheritdoc/>
		public static void SetDiag(DenseMatrix<T, TS> matrix, long k, SparseVector<T, TInd, TS, TSInd> value)
		{
			var diag = DenseOperation<T, TS>.GetDiag(matrix, k);
			diag.FillWith(T.Zero);
			AddBy(diag, value, T.One);
		}

		/// <inheritdoc/>
		public static void MatrixMultiplyVector(DenseMatrix<T, TS> matrix, SparseVector<T, TInd, TS, TSInd> vector, DenseVector<T, TS> vectorOut, T α, T β = default, MatrixOperation operation = MatrixOperation.None)
		{
			IMatrixVectorMultiplyOperations<T, SparseVector<T, TInd, TS, TSInd>, DenseVector<T, TS>, DenseMatrix<T, TS>>.CheckMatMulVec(matrix, vector, vectorOut, α, operation);
			SpComp.MatrixDenseMultiplyVectorSparse(operation, α, operation.CanInPlace() ? matrix.NRows : matrix.NCols, matrix.Storage, matrix.LeadDim, vector, β, vectorOut.Storage, vectorOut.Stride);
		}

		/// <inheritdoc/>
		public static void VectorMultiplyMatrix(SparseVector<T, TInd, TS, TSInd> vector, DenseMatrix<T, TS> matrix, DenseVector<T, TS> vectorOut, T α, T β = default, MatrixOperation operation = MatrixOperation.None) => MatrixMultiplyVector(matrix, vector, vectorOut, α, β, operation.Transpose());

		/// <summary>
		/// Statically compute the out-of-place addition for two <see cref="SparseVector{T, TInd, TS, TSInd}"/>s.
		/// </summary>
		/// <param name="scalarLeft">The scalar to multiply to <paramref name="left"/> during computation</param>
		/// <param name="left">The input left sparse vector</param>
		/// <param name="right">The input right sparse vector</param>
		/// <returns>The created new sparse vector as the addition result.</returns>
		/// <exception cref="ArgumentException">If <paramref name="scalarLeft"/> == 0 or <paramref name="left"/> and <paramref name="right"/> have different lengths</exception>
		public static SparseVector<T, TInd, TS, TSInd> VectorsAdd(T scalarLeft, SparseVector<T, TInd, TS, TSInd> left, SparseVector<T, TInd, TS, TSInd> right)
		{
			if (scalarLeft == T.Zero)
				throw new ArgumentException(Resources.ParameterError.CannotZero, nameof(scalarLeft));
			if (left.Length != right.Length)
				throw new ArgumentException(Resources.ParameterError.NotSameSize);
			var wrapper = new SparseArrayWrapper<T, TInd, TS, TSInd>(left.DefaultValue + right.DefaultValue, SparseFormat.Any);
			SpComp.VectorSparseAddSparse(scalarLeft, left, right, ref wrapper);
			return SparseVector<T, TInd, TS, TSInd>.Create(wrapper);
		}
		#endregion


		#region operations
		/// <inheritdoc/>
		public static void MatrixMultiplyVector(SparseMatrix<T, TInd, TS, TSInd> matrix, DenseVector<T, TS> vector, DenseVector<T, TS> vectorOut, T α, T β = default, MatrixOperation operation = MatrixOperation.None)
		{
			IMatrixVectorMultiplyOperations<T, DenseVector<T, TS>, DenseVector<T, TS>, SparseMatrix<T, TInd, TS, TSInd>>.CheckMatMulVec(matrix, vector, vectorOut, α, operation);
			SpComp.MatrixSparseMultiplyVectorDense(operation, α, matrix, vector.Storage, vector.Stride, β, vectorOut.Storage, vectorOut.Stride);
		}

		/// <inheritdoc/>
		public static void VectorMultiplyMatrix(DenseVector<T, TS> vector, SparseMatrix<T, TInd, TS, TSInd> matrix, DenseVector<T, TS> vectorOut, T α, T β = default, MatrixOperation operation = MatrixOperation.None) => MatrixMultiplyVector(matrix, vector, vectorOut, α, β, operation.Transpose());

		/// <inheritdoc/>
		public static SparseVector<T, TInd, TS, TSInd> GetDiag(SparseMatrix<T, TInd, TS, TSInd> matrix, long k)
		{
			var vector = new SparseArrayWrapper<T, TInd, TS, TSInd>(matrix.DefaultValue, SparseFormat.Any);
			SpComp.SparseMatrixGetDiag(matrix, k, ref vector);
			return SparseVector<T, TInd, TS, TSInd>.Create(in vector);
		}

		/// <inheritdoc/>
		public static void GetDiag(SparseMatrix<T, TInd, TS, TSInd> matrix, long k, SparseVector<T, TInd, TS, TSInd> overwrite)
		{
			var vector = SparseArrayWrapper<T, TInd, TS, TSInd>.Create(overwrite);
			SpComp.SparseMatrixGetDiag(matrix, k, ref vector);
		}

		/// <inheritdoc/>
		public static void SetDiag(SparseMatrix<T, TInd, TS, TSInd> matrix, long k, SparseVector<T, TInd, TS, TSInd> value)
		{
			SpComp.SparseMatrixSetDiag(matrix, k, value);
		}

		/// <inheritdoc/>
		public static void AddMatrices(DenseMatrix<T, TS>? A, T scalarA, SparseMatrix<T, TInd, TS, TSInd>? B, T scalarB, DenseMatrix<T, TS> C, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			IMatrixOperations<T, DenseMatrix<T, TS>, SparseMatrix<T, TInd, TS, TSInd>, DenseMatrix<T, TS>>.CheckMatAdd(A, scalarA, B, scalarB, C, opA, opB);
			if (B is null || scalarB == T.Zero)
				throw new ArgumentNullException(nameof(B));
			SpComp.MatrixDenseAddSparse(opA, opB, scalarA, A?.Storage, A?.LeadDim ?? 1, scalarB, B, C.Storage, C.LeadDim);
		}

		/// <inheritdoc/>
		public static void MultiplyMatries(T α, DenseMatrix<T, TS> A, SparseMatrix<T, TInd, TS, TSInd> B, T β, DenseMatrix<T, TS> C, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			var (m, _, _) = IMatrixOperations<T, DenseMatrix<T, TS>, SparseMatrix<T, TInd, TS, TSInd>, DenseMatrix<T, TS>>.CheckMatMul(α, A, B, C, opA, opB);
			SpComp.MatrixDenseMultiplySparse(opA, opB, m, α, A.Storage, A.LeadDim, B, β, C.Storage, C.LeadDim);
		}

		/// <inheritdoc/>
		public static void AddMatrices(SparseMatrix<T, TInd, TS, TSInd>? A, T scalarA, DenseMatrix<T, TS>? B, T scalarB, DenseMatrix<T, TS> C, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None) => AddMatrices(B, scalarB, A, scalarA, C, opB, opA);

		/// <inheritdoc/>
		public static void MultiplyMatries(T α, SparseMatrix<T, TInd, TS, TSInd> A, DenseMatrix<T, TS> B, T β, DenseMatrix<T, TS> C, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			var (_, n, _) = IMatrixOperations<T, SparseMatrix<T, TInd, TS, TSInd>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>.CheckMatMul(α, A, B, C, opA, opB);
			SpComp.MatrixSparseMultiplyDense(opA, opB, n, α, A, B.Storage, B.LeadDim, β, C.Storage, C.LeadDim);
		}

		/// <inheritdoc/>
		public static void AddMatrices(SparseMatrix<T, TInd, TS, TSInd>? A, T scalarA, SparseMatrix<T, TInd, TS, TSInd>? B, T scalarB, SparseMatrix<T, TInd, TS, TSInd> C, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			IMatrixOperations<T, SparseMatrix<T, TInd, TS, TSInd>, SparseMatrix<T, TInd, TS, TSInd>, SparseMatrix<T, TInd, TS, TSInd>>.CheckMatAdd(A, scalarA, B, scalarB, C, opA, opB);
			var target = SparseArrayWrapper<T, TInd, TS, TSInd>.Create(C);
			SpComp.MatrixSparseAddSparse(opA, opB, scalarA, A, scalarB, B, ref target);
		}

		/// <inheritdoc/>
		public static void MultiplyMatries(T α, SparseMatrix<T, TInd, TS, TSInd> A, SparseMatrix<T, TInd, TS, TSInd> B, T β, SparseMatrix<T, TInd, TS, TSInd> C, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			IMatrixOperations<T, SparseMatrix<T, TInd, TS, TSInd>, SparseMatrix<T, TInd, TS, TSInd>, SparseMatrix<T, TInd, TS, TSInd>>.CheckMatMul(α, A, B, C, opA, opB);
			var target = SparseArrayWrapper<T, TInd, TS, TSInd>.Create(C);
			SpComp.MatrixSparseMultiplySparse(opA, opB, α, A, B, β, C, ref target);
		}
		#endregion


		#region operations
		/// <inheritdoc/>
		public static void Reduce(SparseTensor<T, TInd, TS, TSInd> A!!, TensorOrder order, T scalar, SparseTensor<T, TInd, TS, TSInd> B!!, UnaryOperation opA = UnaryOperation.Identity, BinaryOperation reduce = BinaryOperation.Addition)
		{
			Span<int> reduceInds = stackalloc int[A.Rank];
			reduceInds = ITensorOperations<T, SparseTensor<T, TInd, TS, TSInd>, SparseTensor<T, TInd, TS, TSInd>>.CheckReduce(A, order, scalar, B, reduceInds);
			var target = SparseArrayWrapper<T, TInd, TS, TSInd>.Create(B);
			SpTen.Reduce(reduce, A, scalar, opA, reduceInds, ref target);
		}

		/// <inheritdoc/>
		public static void Permute(SparseTensor<T, TInd, TS, TSInd> A!!, TensorOrder order, T scalar, SparseTensor<T, TInd, TS, TSInd> B!!, UnaryOperation opA = UnaryOperation.Identity)
		{
			Span<int> perm = stackalloc int[A.Rank];
			ITensorOperations<T, SparseTensor<T, TInd, TS, TSInd>, SparseTensor<T, TInd, TS, TSInd>>.CheckPermute(A, order, scalar, B, perm);
			var target = SparseArrayWrapper<T, TInd, TS, TSInd>.Create(B);
			SpTen.Permute(A, scalar, opA, perm, ref target);
		}

		/// <inheritdoc/>
		public static void Reduce(SparseTensor<T, TInd, TS, TSInd> A!!, TensorOrder order, T scalar, DenseTensor<T, TS> B!!, UnaryOperation opA = UnaryOperation.Identity, BinaryOperation reduce = BinaryOperation.Addition)
		{
			Span<int> reduceInds = stackalloc int[A.Rank];
			reduceInds = ITensorOperations<T, SparseTensor<T, TInd, TS, TSInd>, DenseTensor<T, TS>>.CheckReduce(A, order, scalar, B, reduceInds);
			SpTen.Reduce(reduce, A, scalar, opA, reduceInds, new DenseTensorWrapper<T, TS>(B, B.Storage));
		}

		/// <inheritdoc/>
		public static void Permute(SparseTensor<T, TInd, TS, TSInd> A!!, TensorOrder order, T scalar, DenseTensor<T, TS> B!!, UnaryOperation op = UnaryOperation.Identity)
		{
			Span<int> perm = stackalloc int[A.Rank];
			ITensorOperations<T, SparseTensor<T, TInd, TS, TSInd>, DenseTensor<T, TS>>.CheckPermute(A, order, scalar, B, perm);
			SpTen.Permute(A, scalar, op, perm, new DenseTensorWrapper<T, TS>(B, B.Storage));
		}

		/// <inheritdoc/>
		public static void Contract(SparseTensor<T, TInd, TS, TSInd> A!!, UnaryOperation opA, SparseTensor<T, TInd, TS, TSInd> B!!, UnaryOperation opB, T α, SparseTensor<T, TInd, TS, TSInd> C, UnaryOperation opC, T β)
		{
			var info = ITensorOperations<T, SparseTensor<T, TInd, TS, TSInd>, SparseTensor<T, TInd, TS, TSInd>, SparseTensor<T, TInd, TS, TSInd>>.CheckContract(A, B, α, C);
			var target = SparseArrayWrapper<T, TInd, TS, TSInd>.Create(C);
			SpTen.Contract(α, A, opA, B, opB, in info, β, ref target, opC);
		}

		/// <inheritdoc/>
		public static void TensorsBinaryOperation(SparseTensor<T, TInd, TS, TSInd>? A, TensorOrder orderA, UnaryOperation opA, T α, SparseTensor<T, TInd, TS, TSInd>? B, TensorOrder orderB, UnaryOperation opB, T β, SparseTensor<T, TInd, TS, TSInd> C!!, BinaryOperation binary)
		{
			Span<int> permA = stackalloc int[A?.Rank ?? 0], permB = stackalloc int[B?.Rank ?? 0];
			ITensorOperations<T, SparseTensor<T, TInd, TS, TSInd>, SparseTensor<T, TInd, TS, TSInd>, SparseTensor<T, TInd, TS, TSInd>>.CheckBinary(A, orderA, α, B, orderB, β, C, permA, permB);
			var target = SparseArrayWrapper<T, TInd, TS, TSInd>.Create(C);
			SpTen.OperationBinary(binary, A, permA, α, opA, B, permB, β, opB, ref target);
		}

		/// <inheritdoc/>
		public static void Contract(SparseTensor<T, TInd, TS, TSInd> A!!, UnaryOperation opA, DenseTensor<T, TS> B!!, UnaryOperation opB, T α, DenseTensor<T, TS> C!!, UnaryOperation opC, T β)
		{
			if (α == T.Zero)
				throw new ArgumentOutOfRangeException(nameof(α), α, Resources.ParameterError.CannotZero);
			int rank = TensorContractInfo.GetContractRank(A, B);
			Span<int> leftConc = stackalloc int[rank];
			Span<int> rightConc = stackalloc int[rank];
			Span<int> leftFree = stackalloc int[A.Rank - rank];
			Span<int> rightFree = stackalloc int[B.Rank - rank];
			var info = TensorContractInfo.Create(A, B, C, leftConc, rightConc, leftFree, rightFree);
			SpTen.Contract(A, opA, new DenseTensorWrapper<T, TS>(B, B.Storage, opB, α), info, new DenseTensorWrapper<T, TS>(C, C.Storage, opC));
		}

		/// <inheritdoc/>
		public static void TensorsBinaryOperation(SparseTensor<T, TInd, TS, TSInd>? A, TensorOrder orderA, UnaryOperation opA, T α, DenseTensor<T, TS>? B, TensorOrder orderB, UnaryOperation opB, T β, DenseTensor<T, TS> C!!, BinaryOperation binary)
		{
			Span<int> permA = stackalloc int[A?.Rank ?? 0], permB = stackalloc int[B?.Rank ?? 0];
			ITensorOperations<T, SparseTensor<T, TInd, TS, TSInd>, DenseTensor<T, TS>, DenseTensor<T, TS>>.CheckBinary(A, orderA, α, B, orderB, β, C, permA, permB);
			SpTen.OperationBinary(binary, A, permA, α, opA, B is null ? default : new DenseTensorWrapper<T, TS>(B, B.Storage, opB, β), permB, new DenseTensorWrapper<T, TS>(C, C.Storage));
		}

		/// <inheritdoc/>
		public static void Contract(DenseTensor<T, TS> A, UnaryOperation opA, SparseTensor<T, TInd, TS, TSInd> B, UnaryOperation opB, T α, DenseTensor<T, TS> C, UnaryOperation opC, T β) => Contract(B, opB, A, opA, α, C, opC, β);

		/// <inheritdoc/>
		public static void TensorsBinaryOperation(DenseTensor<T, TS>? A, TensorOrder orderA, UnaryOperation opA, T α, SparseTensor<T, TInd, TS, TSInd>? B, TensorOrder orderB, UnaryOperation opB, T β, DenseTensor<T, TS> C, BinaryOperation binary) => TensorsBinaryOperation(B, orderB, opB, β, A, orderA, opA, α, C, binary);
		#endregion
	}
}
