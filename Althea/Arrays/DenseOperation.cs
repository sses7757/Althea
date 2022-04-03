using System.Runtime.CompilerServices;

using Althea.Helpers;
using Althea.LinearAlgebra;
using Althea.LinearAlgebra.Dense;
using Althea.Storage;

using Blas = Althea.LinearAlgebra.Dense.BlasApiSelector;
using ExtBlas = Althea.LinearAlgebra.Dense.ExtendBlasApiSelector;
using HalfBlas = Althea.LinearAlgebra.Dense.HalfMatrixBlasApiSelector;
using Lapack = Althea.LinearAlgebra.Dense.LapackApiSelector;


namespace Althea.Arrays
{
	/// <summary>
	/// The static class for dense linear algebra and tensor algebra operations of same data type and storage type.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TS">The storage type used by the value storage</typeparam>
	public sealed class DenseOperation<T, TS> :
		IVectorOperations<T, DenseVector<T, TS>, DenseVector<T, TS>>,

		IMatrixGetDiagonalVector<T, DenseVector<T, TS>, DenseMatrix<T, TS>>,
		IMatrixSetDiagonalVector<T, DenseVector<T, TS>, DenseMatrix<T, TS>>,
		IMatrixGetDiagonalVectorVariant<T, DenseVector<T, TS>, DenseMatrix<T, TS>>,
		IMatrixGetDiagonalVector<T, DenseVector<T, TS>, TriangularMatrix<T, TS>>,
		IMatrixSetDiagonalVector<T, DenseVector<T, TS>, TriangularMatrix<T, TS>>,
		IMatrixGetDiagonalVectorVariant<T, DenseVector<T, TS>, TriangularMatrix<T, TS>>,

		IMatrixVectorMultiplyOperations<T, DenseVector<T, TS>, DenseVector<T, TS>, DenseMatrix<T, TS>>,
		IMatrixVectorMultiplyOperations<T, DenseVector<T, TS>, DenseVector<T, TS>, TriangularMatrix<T, TS>>,

		IMatrixOperations<T, DenseMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>,
		IMatrixOperations<T, TriangularMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>,
		IMatrixOperations<T, DenseMatrix<T, TS>, TriangularMatrix<T, TS>, DenseMatrix<T, TS>>,
		IMatrixOperations<T, TriangularMatrix<T, TS>, TriangularMatrix<T, TS>, TriangularMatrix<T, TS>>,

		IMatrixLinearSolve<T, DenseMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>,
		IMatrixLeastSolve<T, DenseMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>,
		IMatrixQRSolve<T, DenseMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>,
		IMatrixLinearSolve<T, TriangularMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>,
		IMatrixQRSolve<T, DenseMatrix<T, TS>, TriangularMatrix<T, TS>, DenseMatrix<T, TS>>

		where T : unmanaged, INumber<T>
		where TS : class, IStorage<T, TS>
	{
		#region vector
		/// <inheritdoc/>
		public static T Dot(DenseVector<T, TS> left!!, DenseVector<T, TS> right!!, bool conjugateLeft = true) => Blas.Dot<T, TS, TS>(conjugateLeft, left.Storage, left.Stride, right.Storage, right.Stride);

		/// <inheritdoc/>
		public static void AddBy(DenseVector<T, TS> left!!, DenseVector<T, TS> right!!, T scalar) => Blas.Add(scalar, right.Storage, right.Stride, left.Storage, left.Stride);
		#endregion

		#region matrix diag
		/// <inheritdoc/>
		public static DenseVector<T, TS> GetDiag(DenseMatrix<T, TS> matrix!!, long k)
		{
			if (matrix.NRows >= matrix.NCols)
			{
				if (k >= 0)
					return new(matrix.Storage, matrix.NCols - k, matrix.LeadDim + 1);
				else
					return new(matrix.Storage, matrix.NCols - k <= matrix.NRows ? matrix.NCols : matrix.NRows + k, matrix.LeadDim + 1);
			}
			else
			{
				if (k < 0)
					return new(matrix.Storage, matrix.NRows + k, matrix.LeadDim + 1);
				else
					return new(matrix.Storage, matrix.NRows + k <= matrix.NCols ? matrix.NRows : matrix.NCols - k, matrix.LeadDim + 1);
			}
		}

		/// <inheritdoc/>
		public static void GetDiag(DenseMatrix<T, TS> matrix!!, long k, DenseVector<T, TS> overwrite!!) => GetDiag(matrix, k).CopyTo(overwrite);

		/// <inheritdoc/>
		public static void SetDiag(DenseMatrix<T, TS> matrix!!, long k, DenseVector<T, TS> value!!) => value.CopyTo(GetDiag(matrix, k));

		/// <inheritdoc/>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="k"/> refers to diagonal elements not used</exception>
		public static DenseVector<T, TS> GetDiag(TriangularMatrix<T, TS> matrix!!, long k)
		{
			if ((matrix.Upper && k < 0) || (!matrix.Upper && k > 0) || (matrix.UnitDiagonal && k == 0))
				throw new ArgumentOutOfRangeException(nameof(k));
			if (matrix.NRows >= matrix.NCols)
			{
				if (k >= 0)
					return new(matrix.Storage, matrix.NCols - k, matrix.LeadDim + 1);
				else
					return new(matrix.Storage, matrix.NCols - k <= matrix.NRows ? matrix.NCols : matrix.NRows + k, matrix.LeadDim + 1);
			}
			else
			{
				if (k < 0)
					return new(matrix.Storage, matrix.NRows + k, matrix.LeadDim + 1);
				else
					return new(matrix.Storage, matrix.NRows + k <= matrix.NCols ? matrix.NRows : matrix.NCols - k, matrix.LeadDim + 1);
			}
		}

		/// <inheritdoc/>
		public static void GetDiag(TriangularMatrix<T, TS> matrix!!, long k, DenseVector<T, TS> overwrite!!) => GetDiag(matrix, k).CopyTo(overwrite);

		/// <inheritdoc/>
		public static void SetDiag(TriangularMatrix<T, TS> matrix!!, long k, DenseVector<T, TS> value!!) => value.CopyTo(GetDiag(matrix, k));
		#endregion

		#region matrix vector
		/// <summary>
		/// Create a new <see cref="DenseVector{T, TS}"/> by multiplying a dense <paramref name="matrix"/> (after <paramref name="operation"/>) with dense <paramref name="vector"/>.
		/// </summary>
		/// <param name="matrix">The <see cref="DenseMatrix{T, TS}"/> to multiply</param>
		/// <param name="vector">The <see cref="DenseVector{T, TS}"/> to multiply</param>
		/// <param name="α">The scalar to multiply to <paramref name="matrix"/></param>
		/// <param name="operation">The <see cref="MatrixOperation"/> to be applied to <paramref name="matrix"/></param>
		/// <returns>A created <see cref="DenseVector{T, TS}"/> as the multiplication result.</returns>
		/// <exception cref="ArgumentException">If the sizes mismatch</exception>
		public static DenseVector<T, TS> MatrixMultiplyVector(DenseMatrix<T, TS> matrix!!, DenseVector<T, TS> vector!!, T α, MatrixOperation operation = MatrixOperation.None)
		{
			if ((operation.CanInPlace() ? matrix.NCols : matrix.NRows) != vector.Length)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(vector));
			var output = vector.Storage.ResizeAlike((operation.CanInPlace() ? matrix.NRows : matrix.NCols));
			try
			{
				Blas.GeneralMatrixMultiplyVector(MatrixOperation.None, matrix.NRows, matrix.NCols, α, matrix.Storage, matrix.LeadDim, vector.Storage, vector.Stride, T.Zero, output, 1);
				return new(output, output.Length);
			}
			catch (Exception)
			{
				output.Dispose();
				throw;
			}
		}

		/// <inheritdoc/>
		public static void MatrixMultiplyVector(DenseMatrix<T, TS> matrix!!, DenseVector<T, TS> vector!!, DenseVector<T, TS> vectorOut!!, T α, T β = default, MatrixOperation operation = MatrixOperation.None)
		{
			IMatrixVectorMultiplyOperations<T, DenseVector<T, TS>, DenseVector<T, TS>, DenseMatrix<T, TS>>.CheckMatMulVec(matrix, vector, vectorOut, α, operation);
			Blas.GeneralMatrixMultiplyVector(operation, matrix.NRows, matrix.NCols, α, matrix.Storage, matrix.LeadDim, vector.Storage, vector.Stride, β, vectorOut.Storage, vectorOut.Stride);
		}

		/// <inheritdoc/>
		public static void VectorMultiplyMatrix(DenseVector<T, TS> vector!!, DenseMatrix<T, TS> matrix!!, DenseVector<T, TS> vectorOut!!, T α, T β = default, MatrixOperation operation = MatrixOperation.None) => MatrixMultiplyVector(matrix, vector, vectorOut, α, β, operation.Transpose());

		/// <summary>
		/// Create a new <see cref="DenseVector{T, TS}"/> by multiplying a dense <paramref name="matrix"/> (after <paramref name="operation"/>) with dense <paramref name="vector"/>.
		/// </summary>
		/// <param name="matrix">The <see cref="TriangularMatrix{T, TS}"/> to multiply</param>
		/// <param name="vector">The <see cref="DenseVector{T, TS}"/> to multiply</param>
		/// <param name="α">The scalar to multiply to <paramref name="matrix"/></param>
		/// <param name="operation">The <see cref="MatrixOperation"/> to be applied to <paramref name="matrix"/></param>
		/// <returns>A created <see cref="DenseVector{T, TS}"/> as the multiplication result.</returns>
		/// <exception cref="ArgumentException">If the sizes mismatch</exception>
		public static DenseVector<T, TS> MatrixMultiplyVector(TriangularMatrix<T, TS> matrix!!, DenseVector<T, TS> vector!!, T α, MatrixOperation operation = MatrixOperation.None)
		{
			if ((operation.CanInPlace() ? matrix.NCols : matrix.NRows) != vector.Length)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(vector));
			var output = vector.Storage.ResizeAlike((operation.CanInPlace() ? matrix.NRows : matrix.NCols));
			try
			{
				Blas.TriangularMatrixMultiplyVector(matrix.Upper, matrix.UnitDiagonal, operation, matrix.NRows, matrix.NCols, matrix.Storage, matrix.LeadDim, α, vector.Storage, vector.Stride, T.Zero, output, 1);
				return new(output, output.Length);
			}
			catch (Exception)
			{
				output.Dispose();
				throw;
			}
		}

		/// <inheritdoc/>
		public static void MatrixMultiplyVector(TriangularMatrix<T, TS> matrix!!, DenseVector<T, TS> vector!!, DenseVector<T, TS> vectorOut!!, T α, T β = default, MatrixOperation operation = MatrixOperation.None)
		{
			IMatrixVectorMultiplyOperations<T, DenseVector<T, TS>, DenseVector<T, TS>, TriangularMatrix<T, TS>>.CheckMatMulVec(matrix, vector, vectorOut, α, operation);
			Blas.TriangularMatrixMultiplyVector(matrix.Upper, matrix.UnitDiagonal, operation, matrix.NRows, matrix.NCols, matrix.Storage, matrix.LeadDim, α, vector.Storage, vector.Stride, β, vectorOut.Storage, vectorOut.Stride);
		}

		/// <inheritdoc/>
		public static void VectorMultiplyMatrix(DenseVector<T, TS> vector!!, TriangularMatrix<T, TS> matrix!!, DenseVector<T, TS> vectorOut!!, T α, T β = default, MatrixOperation operation = MatrixOperation.None) => MatrixMultiplyVector(matrix, vector, vectorOut, α, β, operation.Transpose());
		#endregion

		#region matrix add multiply
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static TS CheckOutOfPlaceAdd(IMatrixMetric? A, T scalarA, IMatrixMetric? B, T scalarB, MatrixOperation opA, MatrixOperation opB, TS? storage, out long m, out long n)
		{
			bool nullA = A is null || scalarA == T.Zero;
			bool nullB = B is null || scalarB == T.Zero;
			if (nullA && nullB)
				throw new ArgumentException(Resources.ParameterError.CannotAllNull);
			m = 0; n = 0;
			if (!nullA)
			{
#pragma warning disable CS8602
				(m, n) = (A.NRows, A.NCols);
#pragma warning restore CS8602
				if (!opA.CanInPlace())
					(m, n) = (n, m);
			}
			if (!nullB)
			{
#pragma warning disable CS8602
				var (m1, n1) = (B.NRows, B.NCols);
#pragma warning restore CS8602
				if (!opB.CanInPlace())
					(m1, n1) = (n1, m1);
				if (m1 != m || n1 != n)
					throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(B));
			}
			return storage?.ResizeAlike(m * n) ?? TS.Empty;
		}

		/// <summary>
		/// Create a new <see cref="DenseMatrix{T, TS}"/> as the addition of <c><paramref name="opA"/>(<paramref name="A"/>) + <paramref name="opB"/>(<paramref name="B"/>)</c>.
		/// </summary>
		/// <param name="scalarA">The scalar to multiply to matrix <paramref name="A"/> before addition</param>
		/// <param name="scalarB">The scalar to multiply to matrix <paramref name="B"/> before addition</param>
		/// <param name="A">The input left matrix to add</param>
		/// <param name="B">The input right matrix to add</param>
		/// <param name="opA">The <see cref="MatrixOperation"/> to apply to matrix <paramref name="A"/> before addition</param>
		/// <param name="opB">The <see cref="MatrixOperation"/> to apply to matrix <paramref name="B"/> before addition</param>
		/// <exception cref="ArgumentException">If both <paramref name="A"/> and <paramref name="B"/> are null or empty; or both <paramref name="scalarA"/> and <paramref name="scalarB"/> are 0; or the addition cannot be performed due to incompatible sizes</exception>
		/// <returns>A created <see cref="DenseMatrix{T, TS}"/> as the addition result.</returns>
		public static DenseMatrix<T, TS> AddMatrices(DenseMatrix<T, TS>? A, T scalarA, DenseMatrix<T, TS>? B, T scalarB, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			var output = CheckOutOfPlaceAdd(A, scalarA, B, scalarB, opA, opB, A?.Storage ?? B?.Storage, out long m, out long n);
			try
			{
				ExtBlas.GeneralMatricesAdd(opA, opB, m, n, scalarA, A?.Storage, A?.LeadDim ?? 1, scalarB, B?.Storage, B?.LeadDim ?? 1, output, m);
				return new(output, m, n);
			}
			catch (Exception)
			{
				output.Dispose();
				throw;
			}
		}

		/// <inheritdoc/>
		public static void AddMatrices(DenseMatrix<T, TS>? A, T scalarA, DenseMatrix<T, TS>? B, T scalarB, DenseMatrix<T, TS> C!!, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			var (m, n) = IMatrixOperations<T, DenseMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>.CheckMatAdd(A, scalarA, B, scalarB, C, opA, opB);
			ExtBlas.GeneralMatricesAdd(opA, opB, m, n, scalarA, A?.Storage, A?.LeadDim ?? 1, scalarB, B?.Storage, B?.LeadDim ?? 1, C.Storage, C.LeadDim);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static TS CheckOutOfPlaceMul(T α, IMatrixMetric A, IMatrixMetric B, MatrixOperation opA, MatrixOperation opB, TS storage, out long m, out long n, out long k)
		{
			if (α == T.Zero)
				throw new ArgumentException(Resources.ParameterError.CannotZero, nameof(α));
			(m, k) = (A.NRows, A.NCols);
			if (!opA.CanInPlace())
				(m, k) = (k, m);
			var (s, t) = (B.NRows, B.NCols);
			if (!opB.CanInPlace())
				(s, t) = (t, s);
			if (k != s)
				throw new ArgumentException(Resources.ParameterError.NotSameSize);
			n = t;
			return storage.ResizeAlike(m * n);
		}

		/// <summary>
		/// Create a new <see cref="DenseMatrix{T, TS}"/> as the multiplication result of <c><paramref name="α"/> * <paramref name="opA"/>(<paramref name="A"/>) * <paramref name="opB"/>(<paramref name="B"/>)</c>.
		/// </summary>
		/// <param name="α">The scalar to multiply to matrix multiplication result</param>
		/// <param name="A">The input left matrix to multiply</param>
		/// <param name="B">The input right matrix to multiply</param>
		/// <param name="opA">The <see cref="MatrixOperation"/> to apply to matrix <paramref name="A"/> before multiplication</param>
		/// <param name="opB">The <see cref="MatrixOperation"/> to apply to matrix <paramref name="B"/> before multiplication</param>
		/// <returns>A created <see cref="DenseMatrix{T, TS}"/> as the multiplication result.</returns>
		/// <exception cref="ArgumentException">If any of the matrices is null or empty; or the multiplication cannot be performed due to incompatible sizes</exception>
		public static DenseMatrix<T, TS> MultiplyMatries(T α, DenseMatrix<T, TS> A!!, DenseMatrix<T, TS> B!!, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			var output = CheckOutOfPlaceMul(α, A, B, opA, opB, A.Storage, out long m, out long n, out long k);
			try
			{
				Blas.GeneralMatricesMultiply(opA, opB, m, n, k, α, A.Storage, A.LeadDim, B.Storage, B.LeadDim, T.Zero, output, m);
				return new(output, m, n);
			}
			catch (Exception)
			{
				output.Dispose();
				throw;
			}
		}

		/// <inheritdoc/>
		public static void MultiplyMatries(T α, DenseMatrix<T, TS> A!!, DenseMatrix<T, TS> B!!, T β, DenseMatrix<T, TS> C!!, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			var (m, n, k) = IMatrixOperations<T, DenseMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>.CheckMatMul(α, A, B, C, opA, opB);
			Blas.GeneralMatricesMultiply(opA, opB, m, n, k, α, A.Storage, A.LeadDim, B.Storage, B.LeadDim, β, C.Storage, C.LeadDim);
		}

		/// <summary>
		/// Create a new <see cref="DenseMatrix{T, TS}"/> as the addition of <c><paramref name="opA"/>(<paramref name="A"/>) + <paramref name="opB"/>(<paramref name="B"/>)</c>.
		/// </summary>
		/// <param name="scalarA">The scalar to multiply to matrix <paramref name="A"/> before addition</param>
		/// <param name="scalarB">The scalar to multiply to matrix <paramref name="B"/> before addition</param>
		/// <param name="A">The input left matrix to add</param>
		/// <param name="B">The input right matrix to add</param>
		/// <param name="opA">The <see cref="MatrixOperation"/> to apply to matrix <paramref name="A"/> before addition</param>
		/// <param name="opB">The <see cref="MatrixOperation"/> to apply to matrix <paramref name="B"/> before addition</param>
		/// <exception cref="ArgumentException">If both <paramref name="A"/> and <paramref name="B"/> are null or empty; or both <paramref name="scalarA"/> and <paramref name="scalarB"/> are 0; or the addition cannot be performed due to incompatible sizes</exception>
		/// <returns>A created <see cref="DenseMatrix{T, TS}"/> as the addition result.</returns>
		public static DenseMatrix<T, TS> AddMatrices(TriangularMatrix<T, TS>? A, T scalarA, DenseMatrix<T, TS>? B, T scalarB, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			var temp = CheckOutOfPlaceAdd(A, scalarA, B, scalarB, opA, opB, A?.Storage ?? B?.Storage, out long m, out long n);
			var output = new DenseMatrix<T, TS>(temp, m, n);
			try
			{
				AddMatrices(A, scalarA, B, scalarB, output, opA, opB);
				return output;
			}
			catch (Exception)
			{
				output.Dispose();
				throw;
			}
		}

		/// <inheritdoc/>
		public static void AddMatrices(TriangularMatrix<T, TS>? A, T scalarA, DenseMatrix<T, TS>? B, T scalarB, DenseMatrix<T, TS> C!!, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			if (A is null || scalarA == T.Zero)
			{
				AddMatrices((DenseMatrix<T, TS>?)null, T.Zero, B, scalarB, C, opA, opB);
				return;
			}
			var (m, n) = IMatrixOperations<T, TriangularMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>.CheckMatAdd(A, scalarA, B, scalarB, C, opA, opB);
			if (opA.CanInPlace())
			{
				A.Storage.Copy2DTo<T, TS, TS>(A.LeadDim, C.Storage, C.LeadDim, m, n);
			}
			else
			{
				HalfBlas.HalfMatrixCopy<T, TS, TS>(A.Upper, !A.UnitDiagonal, MatrixOperation.Transpose, m, n, A.Storage, A.LeadDim, C.Storage, C.LeadDim);
				opA = opA.Transpose();
			}
			HalfBlas.HalfMatrixClearPart<T, TS>(A.UnitDiagonal, A.Upper, m, n, C.Storage, C.LeadDim);
			if (A.UnitDiagonal)
				ExtBlas.FillWithValue(C.Storage, T.One, C.LeadDim + 1);
			AddMatrices(C, scalarA, B, scalarB, C, opA, opB);
		}

		/// <summary>
		/// Create a new <see cref="DenseMatrix{T, TS}"/> as the multiplication result of <c><paramref name="α"/> * <paramref name="opA"/>(<paramref name="A"/>) * <paramref name="opB"/>(<paramref name="B"/>)</c>.
		/// </summary>
		/// <param name="α">The scalar to multiply to matrix multiplication result</param>
		/// <param name="A">The input left matrix to multiply</param>
		/// <param name="B">The input right matrix to multiply</param>
		/// <param name="opA">The <see cref="MatrixOperation"/> to apply to matrix <paramref name="A"/> before multiplication</param>
		/// <param name="opB">The <see cref="MatrixOperation"/> to apply to matrix <paramref name="B"/> before multiplication</param>
		/// <returns>A created <see cref="DenseMatrix{T, TS}"/> as the multiplication result.</returns>
		/// <exception cref="ArgumentException">If any of the matrices is null or empty; or the multiplication cannot be performed due to incompatible sizes</exception>
		public static DenseMatrix<T, TS> MultiplyMatries(T α, TriangularMatrix<T, TS> A!!, DenseMatrix<T, TS> B!!, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			var output = CheckOutOfPlaceMul(α, A, B, opA, opB, B.Storage, out long m, out long n, out long k);
			try
			{
				Blas.TriangularMatrixMultiply(true, A.Upper, A.UnitDiagonal, opA, opB, m, n, k, α, A.Storage, A.LeadDim, B.Storage, B.LeadDim, output, m);
				return new(output, m, n);
			}
			catch (Exception)
			{
				output.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Create a new <see cref="DenseMatrix{T, TS}"/> as the multiplication result of <c><paramref name="α"/> * <paramref name="opA"/>(<paramref name="A"/>) * <paramref name="opB"/>(<paramref name="B"/>)</c>.
		/// </summary>
		/// <param name="α">The scalar to multiply to matrix multiplication result</param>
		/// <param name="A">The input left matrix to multiply</param>
		/// <param name="B">The input right matrix to multiply</param>
		/// <param name="opA">The <see cref="MatrixOperation"/> to apply to matrix <paramref name="A"/> before multiplication</param>
		/// <param name="opB">The <see cref="MatrixOperation"/> to apply to matrix <paramref name="B"/> before multiplication</param>
		/// <returns>A created <see cref="DenseMatrix{T, TS}"/> as the multiplication result.</returns>
		/// <exception cref="ArgumentException">If any of the matrices is null or empty; or the multiplication cannot be performed due to incompatible sizes</exception>
		public static DenseMatrix<T, TS> MultiplyMatries(T α, DenseMatrix<T, TS> A!!, TriangularMatrix<T, TS> B!!, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			var output = CheckOutOfPlaceMul(α, A, B, opA, opB, A.Storage, out long m, out long n, out long k);
			try
			{
				Blas.TriangularMatrixMultiply(false, B.Upper, B.UnitDiagonal, opB, opA, m, n, k, α, B.Storage, B.LeadDim, A.Storage, A.LeadDim, output, m);
				return new(output, m, n);
			}
			catch (Exception)
			{
				output.Dispose();
				throw;
			}
		}

		/// <inheritdoc/>
		public static void MultiplyMatries(T α, TriangularMatrix<T, TS> A!!, DenseMatrix<T, TS> B!!, T β, DenseMatrix<T, TS> C!!, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			var (m, n, k) = IMatrixOperations<T, TriangularMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>.CheckMatMul(α, A, B, C, opA, opB);
			Blas.TriangularMatrixMultiply(true, A.Upper, A.UnitDiagonal, opA, opB, m, n, k, α, A.Storage, A.LeadDim, B.Storage, B.LeadDim, C.Storage, C.LeadDim);
		}

		/// <inheritdoc/>
		public static void AddMatrices(DenseMatrix<T, TS>? A, T scalarA, TriangularMatrix<T, TS>? B, T scalarB, DenseMatrix<T, TS> C!!, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None) => AddMatrices(B, scalarB, A, scalarA, C, opA, opB);

		/// <inheritdoc/>
		public static void MultiplyMatries(T α, DenseMatrix<T, TS> A!!, TriangularMatrix<T, TS> B!!, T β, DenseMatrix<T, TS> C!!, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			var (m, n, k) = IMatrixOperations<T, DenseMatrix<T, TS>, TriangularMatrix<T, TS>, DenseMatrix<T, TS>>.CheckMatMul(α, A, B, C, opA, opB);
			Blas.TriangularMatrixMultiply(false, B.Upper, B.UnitDiagonal, opB, opA, m, n, k, α, B.Storage, B.LeadDim, A.Storage, A.LeadDim, C.Storage, C.LeadDim);
		}

		/// <summary>
		/// Create a new <see cref="TriangularMatrix{T, TS}"/> as the addition of <c><paramref name="opA"/>(<paramref name="A"/>) + <paramref name="opB"/>(<paramref name="B"/>)</c>.
		/// </summary>
		/// <param name="scalarA">The scalar to multiply to matrix <paramref name="A"/> before addition</param>
		/// <param name="scalarB">The scalar to multiply to matrix <paramref name="B"/> before addition</param>
		/// <param name="A">The input left matrix to add</param>
		/// <param name="B">The input right matrix to add</param>
		/// <param name="opA">The <see cref="MatrixOperation"/> to apply to matrix <paramref name="A"/> before addition</param>
		/// <param name="opB">The <see cref="MatrixOperation"/> to apply to matrix <paramref name="B"/> before addition</param>
		/// <exception cref="ArgumentException">If both <paramref name="A"/> and <paramref name="B"/> are null or empty; or both <paramref name="scalarA"/> and <paramref name="scalarB"/> are 0; or the addition cannot be performed due to incompatible sizes</exception>
		/// <returns>A created <see cref="TriangularMatrix{T, TS}"/> as the addition result.</returns>
		public static TriangularMatrix<T, TS> AddMatrices(TriangularMatrix<T, TS>? A, T scalarA, TriangularMatrix<T, TS>? B, T scalarB, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
#pragma warning disable CS8604
			if (A is null || scalarA == T.Zero)
				return OutOfPlaceOp(B, scalarB, opB);
			if (B is null || scalarB == T.Zero)
				return OutOfPlaceOp(A, scalarA, opA);
#pragma warning restore CS8604
			bool upperA = false, unitA = false;
			if (A is not null)
			{
				upperA = A.Upper == opA.CanInPlace(); unitA = A.UnitDiagonal;
			}
			bool upperB = upperA, unitB = false;
			if (B is not null)
			{
				upperB = B.Upper == opB.CanInPlace(); unitB = B.UnitDiagonal;
			}
			if (upperA != upperB || (unitA & unitB))
				throw new ArgumentException(Resources.ParameterError.InvalidValue);
			var output = CheckOutOfPlaceAdd(A, scalarA, B, scalarB, opA, opB, A?.Storage ?? B?.Storage, out long m, out long n);
			try
			{
				HalfBlas.HalfMatricesAdd(!unitA || !unitB, upperA, opA, opB, m, n, scalarA, A?.Storage, A?.LeadDim ?? 1, scalarB, B?.Storage, B?.LeadDim ?? 1, output, m);
				return new TriangularMatrix<T, TS>(upperA, output, m, n, 0, unitA);
			}
			catch (Exception)
			{
				output.Dispose();
				throw;
			}
		}

		private static TriangularMatrix<T, TS> OutOfPlaceOp(TriangularMatrix<T, TS> matrix, T scalar, MatrixOperation operation)
		{
			if (operation == MatrixOperation.None)
				return matrix.ApplyToClone(m => m.Scale(scalar));
			if (operation == MatrixOperation.Conjugate)
				return matrix.ApplyToClone(m => { ExtBlas.PointWiseConjugate<T, TS>(m.Storage, 1); m.Scale(scalar); });
			var output = matrix.Storage.ResizeAlike(matrix.NRows * matrix.NCols);
			try
			{
				HalfBlas.HalfMatrixCopy<T, TS, TS>(matrix.Upper, !matrix.UnitDiagonal, operation, matrix.NRows, matrix.NCols, matrix.Storage, matrix.LeadDim, output, matrix.NCols);
				return new (!matrix.Upper, output, matrix.NCols, matrix.NRows, 0, matrix.UnitDiagonal);
			}
			catch (Exception)
			{
				output.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Create a new <see cref="TriangularMatrix{T, TS}"/> as the multiplication result of <c><paramref name="α"/> * <paramref name="opA"/>(<paramref name="A"/>) * <paramref name="opB"/>(<paramref name="B"/>)</c>.
		/// </summary>
		/// <param name="α">The scalar to multiply to matrix multiplication result</param>
		/// <param name="A">The input left matrix to multiply</param>
		/// <param name="B">The input right matrix to multiply</param>
		/// <param name="opA">The <see cref="MatrixOperation"/> to apply to matrix <paramref name="A"/> before multiplication</param>
		/// <param name="opB">The <see cref="MatrixOperation"/> to apply to matrix <paramref name="B"/> before multiplication</param>
		/// <returns>A created <see cref="TriangularMatrix{T, TS}"/> as the multiplication result.</returns>
		/// <exception cref="ArgumentException">If any of the matrices is null or empty; or the multiplication cannot be performed due to incompatible sizes</exception>
		public static TriangularMatrix<T, TS> MultiplyMatries(T α, TriangularMatrix<T, TS> A!!, TriangularMatrix<T, TS> B!!, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			bool upperA = A.Upper == opA.CanInPlace();
			bool upperB = B.Upper == opB.CanInPlace();
			if (upperA != upperB || A.UnitDiagonal != B.UnitDiagonal)
				throw new ArgumentException(Resources.ParameterError.InvalidValue);
			var output = CheckOutOfPlaceMul(α, A, B, opA, opB, A.Storage, out long m, out long n, out long k);
			try
			{
				HalfBlas.TriangularMatricesMultiply(A.UnitDiagonal, upperA, opA, opB, m, n, k, α, A.Storage, A.LeadDim, B.Storage, B.LeadDim, T.Zero, output, m);
				return new(upperA, output, m, n, 0, A.UnitDiagonal);
			}
			catch (Exception)
			{
				output.Dispose();
				throw;
			}
		}
		/// <inheritdoc/>
		/// <exception cref="ArgumentException">If the <see cref="TriangularMatrix{T, TS}.Upper"/>s or <see cref="TriangularMatrix{T, TS}.UnitDiagonal"/>s are incompatible</exception>
		public static void AddMatrices(TriangularMatrix<T, TS>? A, T scalarA, TriangularMatrix<T, TS>? B, T scalarB, TriangularMatrix<T, TS> C!!, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			var (m, n) = IMatrixOperations<T, TriangularMatrix<T, TS>, TriangularMatrix<T, TS>, TriangularMatrix<T, TS>>.CheckMatAdd(A, scalarA, B, scalarB, C, opA, opB);
			bool upperA = C.Upper, unitA = false;
			if (A is not null)
			{
				upperA = A.Upper == opA.CanInPlace(); unitA = A.UnitDiagonal;
			}
			bool upperB = C.Upper, unitB = false;
			if (B is not null)
			{
				upperB = B.Upper == opB.CanInPlace(); unitB = B.UnitDiagonal;
			}
			if (upperA != upperB || upperA != C.Upper || (unitA && unitB) || (unitA || unitB) != C.UnitDiagonal)
				throw new ArgumentException(Resources.ParameterError.InvalidValue);
			HalfBlas.HalfMatricesAdd(!unitA || !unitB, upperA, opA, opB, m, n, scalarA, A?.Storage, A?.LeadDim ?? 1, scalarB, B?.Storage, B?.LeadDim ?? 1, C.Storage, C.LeadDim);
		}

		/// <inheritdoc/>
		/// <exception cref="ArgumentException">If the <see cref="TriangularMatrix{T, TS}.Upper"/>s or <see cref="TriangularMatrix{T, TS}.UnitDiagonal"/>s are incompatible</exception>
		public static void MultiplyMatries(T α, TriangularMatrix<T, TS> A!!, TriangularMatrix<T, TS> B!!, T β, TriangularMatrix<T, TS> C!!, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			var (m, n, k) = IMatrixOperations<T, TriangularMatrix<T, TS>, TriangularMatrix<T, TS>, TriangularMatrix<T, TS>>.CheckMatMul(α, A, B, C, opA, opB);
			bool upperA = A.Upper == opA.CanInPlace();
			bool upperB = B.Upper == opB.CanInPlace();
			if (upperA != upperB || upperA != C.Upper || A.UnitDiagonal != B.UnitDiagonal || A.UnitDiagonal != C.UnitDiagonal)
				throw new ArgumentException(Resources.ParameterError.InvalidValue);
			HalfBlas.TriangularMatricesMultiply(A.UnitDiagonal, upperA, opA, opB, m, n, k, α, A.Storage, A.LeadDim, B.Storage, B.LeadDim, β, C.Storage, C.LeadDim);
		}
		#endregion

		#region matrix solve
		/// <inheritdoc/>
		public static void LinearSolve(DenseMatrix<T, TS> coefficients!!, DenseMatrix<T, TS> rightHandSides!!, DenseMatrix<T, TS> outSolves!!, MatrixOperation opCoef = MatrixOperation.None)
		{
			IMatrixLinearSolve<T, DenseMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>.CheckLinear(coefficients, rightHandSides, outSolves);
			if (rightHandSides != outSolves)
				rightHandSides.Storage.Copy2DTo<T, TS, TS>(rightHandSides.LeadDim, outSolves.Storage, outSolves.LeadDim, outSolves.NRows, outSolves.NCols);
			using var coef = coefficients.ToCompact();
			Lapack.LinearSolveGeneral<T, TS, TS>(opCoef, coefficients.NRows, outSolves.NCols, coef, coefficients.NRows, outSolves.Storage, outSolves.LeadDim);
		}

		/// <inheritdoc/>
		public static void LeastSquareSolve(DenseMatrix<T, TS> coefficients!!, DenseMatrix<T, TS> rightHandSides!!, DenseMatrix<T, TS> outSolves!!)
		{
			IMatrixLeastSolve<T, DenseMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>.CheckLeast(coefficients, rightHandSides, outSolves);
			if (rightHandSides != outSolves)
				rightHandSides.Storage.Copy2DTo<T, TS, TS>(rightHandSides.LeadDim, outSolves.Storage, outSolves.LeadDim, outSolves.NRows, outSolves.NCols);
			using var coef = coefficients.ToCompact();
			Lapack.LeastSquareSolve<T, TS, TS>(coefficients.NRows, coefficients.NCols, outSolves.NCols, coef, coefficients.NRows, outSolves.Storage, outSolves.LeadDim);
		}

		/// <inheritdoc/>
		public static void QRDecomposition(DenseMatrix<T, TS> matrix!!, DenseMatrix<T, TS> outTriangular!!, DenseMatrix<T, TS>? outUnary, bool full = false)
		{
			IMatrixQRSolve<T, DenseMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>.CheckQR(matrix, outTriangular, outUnary, full);
			if (matrix.NRows <= matrix.NCols)
			{
				matrix.CopyTo(outTriangular);
				Lapack.QRDecomposition<T, TS, TS>(true, matrix.NRows, matrix.NCols, outTriangular.Storage, outTriangular.LeadDim, outUnary?.Storage, outUnary?.LeadDim ?? 1);
			}
			else //if (matrix.NRows > matrix.NCols)
			{
				if (matrix.Storage == outTriangular.Storage)
				{
					Lapack.QRDecomposition<T, TS, TS>(full, matrix.NRows, matrix.NCols, matrix.Storage, matrix.NRows, outUnary?.Storage, outUnary?.LeadDim ?? 1);
				}
				else
				{
					using var temp = matrix.ToCompact();
					Lapack.QRDecomposition<T, TS, TS>(full, matrix.NRows, matrix.NCols, temp, matrix.NRows, outUnary?.Storage, outUnary?.LeadDim ?? 1);
					temp.Copy2DTo<T, TS, TS>(matrix.NRows, outTriangular.Storage, outTriangular.LeadDim, matrix.NCols, matrix.NCols);
					HalfBlas.HalfMatrixClearPart<T, TS>(false, true, matrix.NRows, matrix.NCols, outTriangular.Storage, outTriangular.LeadDim);
				}
			}
		}

		/// <inheritdoc/>
		public static void QRDecomposition(DenseMatrix<T, TS> matrix, TriangularMatrix<T, TS> outTriangular, DenseMatrix<T, TS>? outUnary, bool full = false)
		{
			IMatrixQRSolve<T, DenseMatrix<T, TS>, TriangularMatrix<T, TS>, DenseMatrix<T, TS>>.CheckQR(matrix, outTriangular, outUnary, full);
			if (matrix.Storage == outTriangular.Storage)
			{
				Lapack.QRDecomposition<T, TS, TS>(true, matrix.NRows, matrix.NCols, matrix.Storage, matrix.LeadDim, outUnary?.Storage, outUnary?.LeadDim ?? 1);
			}
			else
			{
				using var temp = matrix.ToCompact();
				Lapack.QRDecomposition<T, TS, TS>(true, matrix.NRows, matrix.NCols, temp, matrix.NRows, outUnary?.Storage, outUnary?.LeadDim ?? 1);
				using var tempTri = new TriangularMatrix<T, TS>(true, temp, matrix.NRows, matrix.NCols, matrix.NRows);
				tempTri.CopyTo(outTriangular);
			}
		}

		/// <inheritdoc/>
		public static void LinearSolve(TriangularMatrix<T, TS> coefficients!!, DenseMatrix<T, TS> rightHandSides!!, DenseMatrix<T, TS> outSolves!!, MatrixOperation opCoef = MatrixOperation.None)
		{
			if (coefficients.NRows != coefficients.NCols)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(coefficients));
			if (coefficients.NRows != outSolves.NRows)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(outSolves));
			Blas.TriangularMatrixSolve(true, coefficients.Upper, coefficients.UnitDiagonal, opCoef, coefficients.NRows, outSolves.NCols, T.One, coefficients.Storage, coefficients.LeadDim, outSolves.Storage, outSolves.LeadDim);
		}
		#endregion
	}
}
