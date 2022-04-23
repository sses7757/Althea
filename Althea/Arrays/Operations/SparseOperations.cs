using Althea.LinearAlgebra;
using Althea.Linq;
using Althea.Storage;
using Althea.TensorAlgebra;
using Althea.TensorAlgebra.Dense;

using SpComp = Althea.LinearAlgebra.Sparse.ComputationApiSelector;
using SpTen = Althea.TensorAlgebra.Sparse.ApiSelector;


namespace Althea.Array
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
		IMatrixSetDiagonalVector<T, SparseVector<T, TInd, TS, TSInd>, TriangularMatrix<T, TS>>,
		IMatrixSetDiagonalVector<T, SparseVector<T, TInd, TS, TSInd>, SymmetricMatrix<T, TS>>,
		IMatrixDiagonalVector<T, SparseVector<T, TInd, TS, TSInd>, SparseMatrix<T, TInd, TS, TSInd>>,

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
		#region vector
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

		#region matrix diag
		/// <inheritdoc/>
		public static void SetDiag(DenseMatrix<T, TS> matrix, long k, SparseVector<T, TInd, TS, TSInd> value)
		{
			var diag = DenseOperation<T, TS>.GetDiag(matrix, k);
			diag.FillWith(T.Zero);
			AddBy(diag, value, T.One);
		}

		/// <inheritdoc/>
		public static void SetDiag(TriangularMatrix<T, TS> matrix, long k, SparseVector<T, TInd, TS, TSInd> value)
		{
			var diag = DenseOperation<T, TS>.GetDiag(matrix, k);
			diag.FillWith(T.Zero);
			AddBy(diag, value, T.One);
		}

		/// <inheritdoc/>
		public static void SetDiag(SymmetricMatrix<T, TS> matrix, long k, SparseVector<T, TInd, TS, TSInd> value)
		{
			var diag = DenseOperation<T, TS>.GetDiagRaw(matrix, k);
			diag.FillWith(T.Zero);
			AddBy(diag, value, T.One);
			if (!matrix.Hermitian || !((matrix.Upper && k < 0) || (!matrix.Upper && k > 0)))
				return;
			diag.Conjugate();
		}

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
		#endregion

		#region matrix vector multiply
		/// <inheritdoc/>
		public static void MatrixMultiplyVector(DenseMatrix<T, TS> matrix, SparseVector<T, TInd, TS, TSInd> vector, DenseVector<T, TS> vectorOut, T α, T β = default, MatrixOperation operation = MatrixOperation.None)
		{
			IMatrixVectorMultiplyOperations<T, SparseVector<T, TInd, TS, TSInd>, DenseVector<T, TS>, DenseMatrix<T, TS>>.CheckMatMulVec(matrix, vector, vectorOut, α, operation);
			SpComp.MatrixDenseMultiplyVectorSparse(operation, α, operation.CanInPlace() ? matrix.NRows : matrix.NCols, matrix.Storage, matrix.LeadDim, vector, β, vectorOut.Storage, vectorOut.Stride);
		}

		/// <inheritdoc/>
		public static void VectorMultiplyMatrix(SparseVector<T, TInd, TS, TSInd> vector, DenseMatrix<T, TS> matrix, DenseVector<T, TS> vectorOut, T α, T β = default, MatrixOperation operation = MatrixOperation.None) => MatrixMultiplyVector(matrix, vector, vectorOut, α, β, operation.Transpose());

		/// <inheritdoc/>
		public static DenseVector<T, TS> MatrixMultiplyVector(DenseMatrix<T, TS> matrix, SparseVector<T, TInd, TS, TSInd> vector, T α, MatrixOperation operation = MatrixOperation.None)
		{
			var output = IMatrixVectorMultiplyOperations<T, SparseVector<T, TInd, TS, TSInd>, DenseVector<T, TS>, DenseMatrix<T, TS>>.CheckMatMulVec(matrix, vector, vector.Storage, α, operation);
			try
			{
				SpComp.MatrixDenseMultiplyVectorSparse(operation, α, operation.CanInPlace() ? matrix.NRows : matrix.NCols, matrix.Storage, matrix.LeadDim, vector, T.Zero, output, 1);
				return new(output, output.Length);
			}
			catch (Exception)
			{
				output.Dispose();
				throw;
			}
		}

		/// <inheritdoc/>
		public static DenseVector<T, TS> VectorMultiplyMatrix(SparseVector<T, TInd, TS, TSInd> vector, DenseMatrix<T, TS> matrix, T α, MatrixOperation operation = MatrixOperation.None) => MatrixMultiplyVector(matrix, vector, α, operation.Transpose());

		/// <inheritdoc/>
		public static void MatrixMultiplyVector(SparseMatrix<T, TInd, TS, TSInd> matrix, DenseVector<T, TS> vector, DenseVector<T, TS> vectorOut, T α, T β = default, MatrixOperation operation = MatrixOperation.None)
		{
			IMatrixVectorMultiplyOperations<T, DenseVector<T, TS>, DenseVector<T, TS>, SparseMatrix<T, TInd, TS, TSInd>>.CheckMatMulVec(matrix, vector, vectorOut, α, operation);
			SpComp.MatrixSparseMultiplyVectorDense(operation, α, matrix, vector.Storage, vector.Stride, β, vectorOut.Storage, vectorOut.Stride);
		}

		/// <inheritdoc/>
		public static void VectorMultiplyMatrix(DenseVector<T, TS> vector, SparseMatrix<T, TInd, TS, TSInd> matrix, DenseVector<T, TS> vectorOut, T α, T β, MatrixOperation operation = MatrixOperation.None) => MatrixMultiplyVector(matrix, vector, vectorOut, α, β, operation.Transpose());

		/// <inheritdoc/>
		public static DenseVector<T, TS> MatrixMultiplyVector(SparseMatrix<T, TInd, TS, TSInd> matrix, DenseVector<T, TS> vector, T α, MatrixOperation operation = MatrixOperation.None)
		{
			var output = IMatrixVectorMultiplyOperations<T, DenseVector<T, TS>, DenseVector<T, TS>, SparseMatrix<T, TInd, TS, TSInd>>.CheckMatMulVec(matrix, vector, vector.Storage, α, operation);
			try
			{
				SpComp.MatrixSparseMultiplyVectorDense(operation, α, matrix, vector.Storage, vector.Stride, T.Zero, output, 1);
				return new(output, output.Length);
			}
			catch (Exception)
			{
				output.Dispose();
				throw;
			}
		}

		/// <inheritdoc/>
		public static DenseVector<T, TS> VectorMultiplyMatrix(DenseVector<T, TS> vector, SparseMatrix<T, TInd, TS, TSInd> matrix, T α, MatrixOperation operation = MatrixOperation.None) => MatrixMultiplyVector(matrix, vector, α, operation.Transpose());
		#endregion

		#region matrix out-of-place add multiply
		/// <inheritdoc/>
		public static DenseMatrix<T, TS> AddMatrices(DenseMatrix<T, TS>? A, T scalarA, SparseMatrix<T, TInd, TS, TSInd>? B, T scalarB, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			if (B is null || scalarB == T.Zero)
			{
				return DenseOperation<T, TS>.AddMatrices(A, scalarA, (DenseMatrix<T, TS>?)null, default, opA, default);
			}
			var output = IMatrixOperations<T, DenseMatrix<T, TS>, SparseMatrix<T, TInd, TS, TSInd>, DenseMatrix<T, TS>>.CheckAdd(A, scalarA, B, scalarB, opA, opB, A?.Storage ?? B.Storage, out long m, out long n);
			try
			{
				SpComp.MatrixDenseAddSparse(opA, opB, scalarA, A?.Storage, A?.LeadDim ?? 1, scalarB, B, output, m);
				return new(output, m, n);
			}
			catch (Exception)
			{
				output.Dispose();
				throw;
			}
		}

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> MultiplyMatries(DenseMatrix<T, TS> A, SparseMatrix<T, TInd, TS, TSInd> B, T α, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			var output = IMatrixOperations<T, DenseMatrix<T, TS>, SparseMatrix<T, TInd, TS, TSInd>, DenseMatrix<T, TS>>.CheckMul(α, A, B, opA, opB, A.Storage, out long m, out long n, out _);
			try
			{
				SpComp.MatrixDenseMultiplySparse(opA, opB, m, α, A.Storage, A.LeadDim, B, T.Zero, output, m);
				return new(output, m, n);
			}
			catch (Exception)
			{
				output.Dispose();
				throw;
			}
		}

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> AddMatrices(SparseMatrix<T, TInd, TS, TSInd>? A, T scalarA, DenseMatrix<T, TS>? B, T scalarB, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None) => AddMatrices(B, scalarB, A, scalarA, opB, opA);

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> MultiplyMatries(SparseMatrix<T, TInd, TS, TSInd> A, DenseMatrix<T, TS> B, T α, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			var output = IMatrixOperations<T, SparseMatrix<T, TInd, TS, TSInd >, DenseMatrix<T, TS>, DenseMatrix<T, TS>>.CheckMul(α, A, B, opA, opB, A.Storage, out long m, out long n, out _);
			try
			{
				SpComp.MatrixSparseMultiplyDense(opA, opB, n, α, A, B.Storage, B.LeadDim, T.Zero, output, m);
				return new(output, m, n);
			}
			catch (Exception)
			{
				output.Dispose();
				throw;
			}
		}

		/// <inheritdoc/>
		public static SparseMatrix<T, TInd, TS, TSInd> AddMatrices(SparseMatrix<T, TInd, TS, TSInd>? A, T scalarA, SparseMatrix<T, TInd, TS, TSInd>? B, T scalarB, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			IMatrixOperations<T, SparseMatrix<T, TInd, TS, TSInd>, SparseMatrix<T, TInd, TS, TSInd>, SparseMatrix<T, TInd, TS, TSInd>>.CheckAdd(A, scalarA, B, scalarB, opA, opB, (TS?)null, out _, out _);
			var target = new SparseArrayWrapper<T, TInd, TS, TSInd>(A?.DefaultValue ?? T.Zero + B?.DefaultValue ?? T.Zero, SparseFormat.Any);
			SpComp.MatrixSparseAddSparse(opA, opB, scalarA, A, scalarB, B, ref target);
			return SparseMatrix<T, TInd, TS, TSInd>.Create(in target);
		}

		/// <inheritdoc/>
		/// <exception cref="ArgumentException">If <paramref name="A"/> or <paramref name="B"/>'s default is not zero</exception>
		public static SparseMatrix<T, TInd, TS, TSInd> MultiplyMatries(SparseMatrix<T, TInd, TS, TSInd> A, SparseMatrix<T, TInd, TS, TSInd> B, T α, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			IMatrixOperations<T, SparseMatrix<T, TInd, TS, TSInd>, SparseMatrix<T, TInd, TS, TSInd>, SparseMatrix<T, TInd, TS, TSInd>>.CheckMul(α, A, B, opA, opB, (TS?)null, out _, out _, out _);
			if (A.DefaultValue != T.Zero)
				throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(A));
			if (B.DefaultValue != T.Zero)
				throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(B));
			var target = new SparseArrayWrapper<T, TInd, TS, TSInd>(T.Zero, SparseFormat.Any);
			SpComp.MatrixSparseMultiplySparse(opA, opB, α, A, B, T.Zero, null, ref target);
			return SparseMatrix<T, TInd, TS, TSInd>.Create(in target);
		}
		#endregion

		#region matrix in-place add multiply
		/// <inheritdoc/>
		public static void AddMatrices(DenseMatrix<T, TS>? A, T scalarA, SparseMatrix<T, TInd, TS, TSInd>? B, T scalarB, DenseMatrix<T, TS> C, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			if (B is null || scalarB == T.Zero)
			{
				DenseOperation<T, TS>.AddMatrices(A, scalarA, (DenseMatrix<T, TS>?)null, default, C, opA, default);
				return;
			}
			IMatrixOperations<T, DenseMatrix<T, TS>, SparseMatrix<T, TInd, TS, TSInd>, DenseMatrix<T, TS>>.CheckAdd(A, scalarA, B, scalarB, C, opA, opB);
			SpComp.MatrixDenseAddSparse(opA, opB, scalarA, A?.Storage, A?.LeadDim ?? 1, scalarB, B, C.Storage, C.LeadDim);
		}

		/// <inheritdoc/>
		public static void MultiplyMatries(DenseMatrix<T, TS> A, SparseMatrix<T, TInd, TS, TSInd> B, T α, T β, DenseMatrix<T, TS> C, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			var (m, _, _) = IMatrixOperations<T, DenseMatrix<T, TS>, SparseMatrix<T, TInd, TS, TSInd>, DenseMatrix<T, TS>>.CheckMul(α, A, B, C, opA, opB);
			SpComp.MatrixDenseMultiplySparse(opA, opB, m, α, A.Storage, A.LeadDim, B, β, C.Storage, C.LeadDim);
		}

		/// <inheritdoc/>
		public static void AddMatrices(SparseMatrix<T, TInd, TS, TSInd>? A, T scalarA, DenseMatrix<T, TS>? B, T scalarB, DenseMatrix<T, TS> C, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None) => AddMatrices(B, scalarB, A, scalarA, C, opB, opA);

		/// <inheritdoc/>
		public static void MultiplyMatries(SparseMatrix<T, TInd, TS, TSInd> A, DenseMatrix<T, TS> B, T α, T β, DenseMatrix<T, TS> C, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			var (_, n, _) = IMatrixOperations<T, SparseMatrix<T, TInd, TS, TSInd>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>.CheckMul(α, A, B, C, opA, opB);
			SpComp.MatrixSparseMultiplyDense(opA, opB, n, α, A, B.Storage, B.LeadDim, β, C.Storage, C.LeadDim);
		}

		/// <inheritdoc/>
		public static void AddMatrices(SparseMatrix<T, TInd, TS, TSInd>? A, T scalarA, SparseMatrix<T, TInd, TS, TSInd>? B, T scalarB, SparseMatrix<T, TInd, TS, TSInd> C, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			IMatrixOperations<T, SparseMatrix<T, TInd, TS, TSInd>, SparseMatrix<T, TInd, TS, TSInd>, SparseMatrix<T, TInd, TS, TSInd>>.CheckAdd(A, scalarA, B, scalarB, C, opA, opB);
			var target = SparseArrayWrapper<T, TInd, TS, TSInd>.Create(C);
			SpComp.MatrixSparseAddSparse(opA, opB, scalarA, A, scalarB, B, ref target);
		}

		/// <inheritdoc/>
		public static void MultiplyMatries(SparseMatrix<T, TInd, TS, TSInd> A, SparseMatrix<T, TInd, TS, TSInd> B, T α, T β, SparseMatrix<T, TInd, TS, TSInd> C, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			IMatrixOperations<T, SparseMatrix<T, TInd, TS, TSInd>, SparseMatrix<T, TInd, TS, TSInd>, SparseMatrix<T, TInd, TS, TSInd>>.CheckMul(α, A, B, C, opA, opB);
			var target = SparseArrayWrapper<T, TInd, TS, TSInd>.Create(C);
			SpComp.MatrixSparseMultiplySparse(opA, opB, α, A, B, β, C, ref target);
		}
		#endregion

		#region tensor out-of-place operations
		/// <inheritdoc/>
		/// <exception cref="ArgumentException">If <paramref name="A"/>'s default value is not zero</exception>
		static SparseTensor<T, TInd, TS, TSInd> ITensorOperations<T, SparseTensor<T, TInd, TS, TSInd>, SparseTensor<T, TInd, TS, TSInd>>.Reduce(SparseTensor<T, TInd, TS, TSInd> A!!, TensorOrder order, T scalar, UnaryOperation opA, BinaryOperation reduce)
		{
			if (A.DefaultValue != T.Zero)
				throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(A));
			Span<int> reduceInds = stackalloc int[A.Rank];
			Span<long> sizeB = stackalloc long[A.Rank];
			ITensorOperations<T, SparseTensor<T, TInd, TS, TSInd>, SparseTensor<T, TInd, TS, TSInd>>.CheckReduce(A, order, scalar, ref reduceInds, ref sizeB);
			var target = new SparseArrayWrapper<T, TInd, TS, TSInd>(T.Zero, SparseFormat.Any, sizeB);
			SpTen.Reduce(reduce, A, scalar, opA, reduceInds, ref target);
			return SparseTensor<T, TInd, TS, TSInd>.Create(in target);
		}

		/// <inheritdoc/>
		public static SparseTensor<T, TInd, TS, TSInd> Permute(SparseTensor<T, TInd, TS, TSInd> A!!, TensorOrder order, T scalar, UnaryOperation opA = UnaryOperation.Identity)
		{
			Span<int> perm = stackalloc int[A.Rank];
			Span<long> sizeB = stackalloc long[A.Rank];
			ITensorOperations<T, SparseTensor<T, TInd, TS, TSInd>, SparseTensor<T, TInd, TS, TSInd>>.CheckPermute(A, order, scalar, perm, sizeB);
			var target = new SparseArrayWrapper<T, TInd, TS, TSInd>(A.DefaultValue, SparseFormat.Any, sizeB);
			SpTen.Permute(A, scalar, opA, perm, ref target);
			return SparseTensor<T, TInd, TS, TSInd>.Create(in target);
		}

		/// <inheritdoc/>
		public static DenseTensor<T, TS> Reduce(SparseTensor<T, TInd, TS, TSInd> A!!, TensorOrder order, T α,  UnaryOperation opA = UnaryOperation.Identity, BinaryOperation reduce = BinaryOperation.Add)
		{
			Span<int> reduceInds = stackalloc int[A.Rank];
			Span<long> sizeB = stackalloc long[A.Rank];
			ITensorOperations<T, SparseTensor<T, TInd, TS, TSInd>, DenseTensor<T, TS>>.CheckReduce(A, order, α, ref reduceInds, ref sizeB);
			var output = A.Storage.ResizeAlike(sizeB.Prod());
			try
			{
				SpTen.Reduce(reduce, A, α, opA, reduceInds, new DenseTensorWrapper<T, TS>(output, sizeB));
				return new(output, sizeB);
			}
			catch (Exception)
			{
				output.Dispose();
				throw;
			}
		}

		/// <inheritdoc/>
		static DenseTensor<T, TS> ITensorOperations<T, SparseTensor<T, TInd, TS, TSInd>, DenseTensor<T, TS>>.Permute(SparseTensor<T, TInd, TS, TSInd> A!!, TensorOrder order, T scalar, UnaryOperation op)
		{
			Span<int> perm = stackalloc int[A.Rank];
			Span<long> sizeB = stackalloc long[A.Rank];
			ITensorOperations<T, SparseTensor<T, TInd, TS, TSInd>, DenseTensor<T, TS>>.CheckPermute(A, order, scalar, perm, sizeB);
			var output = A.Storage.ResizeAlike(sizeB.Prod());
			try
			{
				SpTen.Permute(A, scalar, op, perm, new DenseArrayWrapper<T, TS>(output, sizeB));
				return new(output, sizeB);
			}
			catch (Exception)
			{
				output.Dispose();
				throw;
			}
		}

		/// <inheritdoc/>
		/// <exception cref="ArgumentException">If <paramref name="A"/> or <paramref name="B"/>'s default value is not zero</exception>
		public static SparseTensor<T, TInd, TS, TSInd> Contract(SparseTensor<T, TInd, TS, TSInd> A!!, UnaryOperation opA, SparseTensor<T, TInd, TS, TSInd> B!!, UnaryOperation opB, T α)
		{
			if (A.DefaultValue != T.Zero)
				throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(A));
			if (B.DefaultValue != T.Zero)
				throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(B));
			Span<long> sizeC = stackalloc long[A.Rank + B.Rank];
			Span<char> labelC = stackalloc char[A.Rank + B.Rank];
			var info = ITensorOperations<T, SparseTensor<T, TInd, TS, TSInd>, SparseTensor<T, TInd, TS, TSInd>, SparseTensor<T, TInd, TS, TSInd>>.CheckContract(A, B, α, ref sizeC, ref labelC);
			var target = new SparseArrayWrapper<T, TInd, TS, TSInd>(T.Zero, SparseFormat.Any, sizeC);
			SpTen.Contract(α, A, opA, B, opB, in info, T.Zero, ref target, default);
			return SparseTensor<T, TInd, TS, TSInd>.Create(in target);
		}

		/// <inheritdoc/>
		public static SparseTensor<T, TInd, TS, TSInd> TensorsBinaryOperation(SparseTensor<T, TInd, TS, TSInd>? A, TensorOrder orderA, UnaryOperation opA, T α, SparseTensor<T, TInd, TS, TSInd>? B, TensorOrder orderB, UnaryOperation opB, T β, BinaryOperation binary)
		{
			Span<int> permA = stackalloc int[A?.Rank ?? 0], permB = stackalloc int[B?.Rank ?? 0];
			Span<long> sizeC = stackalloc long[A?.Rank ?? B?.Rank ?? 0];
			ITensorOperations<T, SparseTensor<T, TInd, TS, TSInd>, SparseTensor<T, TInd, TS, TSInd>, SparseTensor<T, TInd, TS, TSInd>>.CheckBinary(A, orderA, α, B, orderB, β, permA, permB, sizeC, (TS?)null);
			var target = new SparseArrayWrapper<T, TInd, TS, TSInd>(default, SparseFormat.Any, sizeC);
			SpTen.OperationBinary(binary, A, permA, α, opA, B, permB, β, opB, ref target);
			return SparseTensor<T, TInd, TS, TSInd>.Create(in target);
		}

		/// <inheritdoc/>
		public static DenseTensor<T, TS> Contract(SparseTensor<T, TInd, TS, TSInd> A, UnaryOperation opA, DenseTensor<T, TS> B, UnaryOperation opB, T α)
		{
			if (α == T.Zero)
				throw new ArgumentOutOfRangeException(nameof(α), α, Resources.ParameterError.CannotZero);
			int rank = TensorContractInfo.GetContractRank(A, B);
			Span<int> leftConc = stackalloc int[rank];
			Span<int> rightConc = stackalloc int[rank];
			Span<int> leftFree = stackalloc int[A.Rank - rank];
			Span<int> rightFree = stackalloc int[B.Rank - rank];
			Span<long> sizeC = stackalloc long[leftFree.Length + rightFree.Length];
			Span<char> labelC = stackalloc char[sizeC.Length];
			var info = TensorContractInfo.Create(A, B, null, leftConc, rightConc, leftFree, rightFree, sizeC, labelC);
			var output = B.Storage.ResizeAlike(sizeC.Prod());
			try
			{
				SpTen.Contract(A, opA, new DenseTensorWrapper<T, TS>(B, opB, α), info, new DenseTensorWrapper<T, TS>(output, sizeC));
				return new(output, sizeC, default, labelC);
			}
			catch (Exception)
			{
				output.Dispose();
				throw;
			}
		}

		/// <inheritdoc/>
		public static DenseTensor<T, TS> TensorsBinaryOperation(SparseTensor<T, TInd, TS, TSInd>? A, TensorOrder orderA, UnaryOperation opA, T α, DenseTensor<T, TS>? B, TensorOrder orderB, UnaryOperation opB, T β, BinaryOperation binary)
		{
			Span<int> permA = stackalloc int[A?.Rank ?? 0], permB = stackalloc int[B?.Rank ?? 0];
			Span<long> sizeC = stackalloc long[A?.Rank ?? B?.Rank ?? 0];
			var output = ITensorOperations<T, SparseTensor<T, TInd, TS, TSInd>, DenseTensor<T, TS>, DenseTensor<T, TS>>.CheckBinary(A, orderA, α, B, orderB, β, permA, permB, sizeC, B?.Storage ?? A?.Storage);
			try
			{
				SpTen.OperationBinary(binary, A, permA, α, opA, B is null ? default : new DenseTensorWrapper<T, TS>(B, opB, β), permB, new DenseArrayWrapper<T, TS>(output, sizeC));
				return new(output, sizeC);
			}
			catch (Exception)
			{
				output.Dispose();
				throw;
			}
		}

		/// <inheritdoc/>
		public static DenseTensor<T, TS> Contract(DenseTensor<T, TS> A, UnaryOperation opA, SparseTensor<T, TInd, TS, TSInd> B, UnaryOperation opB, T α) => Contract(B, opB, A, opA, α);

		/// <inheritdoc/>
		public static DenseTensor<T, TS> TensorsBinaryOperation(DenseTensor<T, TS>? A, TensorOrder orderA, UnaryOperation opA, T α, SparseTensor<T, TInd, TS, TSInd>? B, TensorOrder orderB, UnaryOperation opB, T β, BinaryOperation binary) => TensorsBinaryOperation(B, orderB, opB, β, A, orderA, opA, α, binary);
		#endregion

		#region tensor out-of-place operations
		/// <inheritdoc/>
		public static void Reduce(SparseTensor<T, TInd, TS, TSInd> A, TensorOrder order, T α, SparseTensor<T, TInd, TS, TSInd> B, T β = default, UnaryOperation opA = UnaryOperation.Identity, UnaryOperation opB = UnaryOperation.Identity, BinaryOperation reduce = BinaryOperation.Add)
		{
			Span<int> reduceInds = stackalloc int[A.Rank];
			reduceInds = ITensorOperations<T, SparseTensor<T, TInd, TS, TSInd>, SparseTensor<T, TInd, TS, TSInd>>.CheckReduce(A, order, α, B, reduceInds);
			var target = SparseArrayWrapper<T, TInd, TS, TSInd>.Create(B);
			SpTen.Reduce(reduce, A, α, opA, reduceInds, ref target);
		}

		/// <inheritdoc/>
		public static void Permute(SparseTensor<T, TInd, TS, TSInd> A, TensorOrder order, T scalar, SparseTensor<T, TInd, TS, TSInd> B, UnaryOperation opA = UnaryOperation.Identity)
		{
			Span<int> perm = stackalloc int[A.Rank];
			ITensorOperations<T, SparseTensor<T, TInd, TS, TSInd>, SparseTensor<T, TInd, TS, TSInd>>.CheckPermute(A, order, scalar, B, perm);
			var target = SparseArrayWrapper<T, TInd, TS, TSInd>.Create(B);
			SpTen.Permute(A, scalar, opA, perm, ref target);
		}

		/// <inheritdoc/>
		public static void Reduce(SparseTensor<T, TInd, TS, TSInd> A, TensorOrder order, T α, DenseTensor<T, TS> B, T β = default, UnaryOperation opA = UnaryOperation.Identity, UnaryOperation opB = UnaryOperation.Identity, BinaryOperation reduce = BinaryOperation.Add)
		{
			Span<int> reduceInds = stackalloc int[A.Rank];
			reduceInds = ITensorOperations<T, SparseTensor<T, TInd, TS, TSInd>, DenseTensor<T, TS>>.CheckReduce(A, order, α, B, reduceInds);
			SpTen.Reduce(reduce, A, α, opA, reduceInds, new DenseTensorWrapper<T, TS>(B, opB, β));
		}

		/// <inheritdoc/>
		public static void Permute(SparseTensor<T, TInd, TS, TSInd> A, TensorOrder order, T scalar, DenseTensor<T, TS> B, UnaryOperation op = UnaryOperation.Identity)
		{
			Span<int> perm = stackalloc int[A.Rank];
			ITensorOperations<T, SparseTensor<T, TInd, TS, TSInd>, DenseTensor<T, TS>>.CheckPermute(A, order, scalar, B, perm);
			SpTen.Permute(A, scalar, op, perm, new DenseArrayWrapper<T, TS>(B));
		}

		/// <inheritdoc/>
		public static void Contract(SparseTensor<T, TInd, TS, TSInd> A, UnaryOperation opA, SparseTensor<T, TInd, TS, TSInd> B, UnaryOperation opB, T α, SparseTensor<T, TInd, TS, TSInd> C, UnaryOperation opC, T β)
		{
			var info = ITensorOperations<T, SparseTensor<T, TInd, TS, TSInd>, SparseTensor<T, TInd, TS, TSInd>, SparseTensor<T, TInd, TS, TSInd>>.CheckContract(A, B, α, C);
			var target = SparseArrayWrapper<T, TInd, TS, TSInd>.Create(C);
			SpTen.Contract(α, A, opA, B, opB, in info, β, ref target, opC);
		}

		/// <inheritdoc/>
		public static void TensorsBinaryOperation(SparseTensor<T, TInd, TS, TSInd>? A, TensorOrder orderA, UnaryOperation opA, T α, SparseTensor<T, TInd, TS, TSInd>? B, TensorOrder orderB, UnaryOperation opB, T β, SparseTensor<T, TInd, TS, TSInd> C, BinaryOperation binary)
		{
			Span<int> permA = stackalloc int[A?.Rank ?? 0], permB = stackalloc int[B?.Rank ?? 0];
			ITensorOperations<T, SparseTensor<T, TInd, TS, TSInd>, SparseTensor<T, TInd, TS, TSInd>, SparseTensor<T, TInd, TS, TSInd>>.CheckBinary(A, orderA, α, B, orderB, β, C, permA, permB);
			var target = SparseArrayWrapper<T, TInd, TS, TSInd>.Create(C);
			SpTen.OperationBinary(binary, A, permA, α, opA, B, permB, β, opB, ref target);
		}

		/// <inheritdoc/>
		public static void Contract(SparseTensor<T, TInd, TS, TSInd> A, UnaryOperation opA, DenseTensor<T, TS> B, UnaryOperation opB, T α, DenseTensor<T, TS> C, UnaryOperation opC, T β)
		{
			if (α == T.Zero)
				throw new ArgumentOutOfRangeException(nameof(α), α, Resources.ParameterError.CannotZero);
			int rank = TensorContractInfo.GetContractRank(A, B);
			Span<int> leftConc = stackalloc int[rank];
			Span<int> rightConc = stackalloc int[rank];
			Span<int> leftFree = stackalloc int[A.Rank - rank];
			Span<int> rightFree = stackalloc int[B.Rank - rank];
			var info = TensorContractInfo.Create(A, B, C, leftConc, rightConc, leftFree, rightFree);
			SpTen.Contract(A, opA, new DenseTensorWrapper<T, TS>(B, opB, α), info, new DenseTensorWrapper<T, TS>(C, opC));
		}

		/// <inheritdoc/>
		public static void TensorsBinaryOperation(SparseTensor<T, TInd, TS, TSInd>? A, TensorOrder orderA, UnaryOperation opA, T α, DenseTensor<T, TS>? B, TensorOrder orderB, UnaryOperation opB, T β, DenseTensor<T, TS> C, BinaryOperation binary)
		{
			Span<int> permA = stackalloc int[A?.Rank ?? 0], permB = stackalloc int[B?.Rank ?? 0];
			ITensorOperations<T, SparseTensor<T, TInd, TS, TSInd>, DenseTensor<T, TS>, DenseTensor<T, TS>>.CheckBinary(A, orderA, α, B, orderB, β, C, permA, permB);
			SpTen.OperationBinary(binary, A, permA, α, opA, B is null ? default : new DenseTensorWrapper<T, TS>(B, opB, β), permB, new DenseArrayWrapper<T, TS>(C));
		}

		/// <inheritdoc/>
		public static void Contract(DenseTensor<T, TS> A, UnaryOperation opA, SparseTensor<T, TInd, TS, TSInd> B, UnaryOperation opB, T α, DenseTensor<T, TS> C, UnaryOperation opC, T β) => Contract(B, opB, A, opA, α, C, opC, β);

		/// <inheritdoc/>
		public static void TensorsBinaryOperation(DenseTensor<T, TS>? A, TensorOrder orderA, UnaryOperation opA, T α, SparseTensor<T, TInd, TS, TSInd>? B, TensorOrder orderB, UnaryOperation opB, T β, DenseTensor<T, TS> C, BinaryOperation binary) => TensorsBinaryOperation(B, orderB, opB, β, A, orderA, opA, α, C, binary);
		#endregion
	}
}
