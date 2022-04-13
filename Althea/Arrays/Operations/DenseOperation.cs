using System.Runtime.CompilerServices;

using Althea.Helpers;
using Althea.LinearAlgebra;
using Althea.LinearAlgebra.Dense;
using Althea.Linq;
using Althea.NativeTypes;
using Althea.Storage;
using Althea.TensorAlgebra;

using Blas = Althea.LinearAlgebra.Dense.BlasApiSelector;
using ExtBlas = Althea.LinearAlgebra.Dense.ExtendBlasApiSelector;
using HalfBlas = Althea.LinearAlgebra.Dense.HalfMatrixBlasApiSelector;
using Lapack = Althea.LinearAlgebra.Dense.LapackApiSelector;
using Ten = Althea.TensorAlgebra.Dense.BaseApiSelector;


namespace Althea.Array
{
	/// <summary>
	/// The static class for dense linear algebra and tensor algebra operations of same data type and storage type.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TS">The storage type used by the value storage</typeparam>
	public sealed class DenseOperation<T, TS> :
		IVectorOperations<T, DenseVector<T, TS>, DenseVector<T, TS>>,

		IMatrixDiagonalVector<T, DenseVector<T, TS>, DenseMatrix<T, TS>>,
		IMatrixDiagonalVector<T, DenseVector<T, TS>, TriangularMatrix<T, TS>>,
		IMatrixDiagonalVector<T, DenseVector<T, TS>, SymmetricMatrix<T, TS>>,

		IMatrixVectorMultiplyOperations<T, DenseVector<T, TS>, DenseVector<T, TS>, DenseMatrix<T, TS>>,
		IMatrixVectorMultiplyOperations<T, DenseVector<T, TS>, DenseVector<T, TS>, TriangularMatrix<T, TS>>,
		IMatrixVectorMultiplyOperations<T, DenseVector<T, TS>, DenseVector<T, TS>, SymmetricMatrix<T, TS>>,

		IMatrixOperations<T, DenseMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>,
		IMatrixOperations<T, TriangularMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>,
		IMatrixOperations<T, DenseMatrix<T, TS>, TriangularMatrix<T, TS>, DenseMatrix<T, TS>>,
		IMatrixOperations<T, SymmetricMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>,
		IMatrixOperations<T, DenseMatrix<T, TS>, SymmetricMatrix<T, TS>, DenseMatrix<T, TS>>,
		IMatrixOperations<T, TriangularMatrix<T, TS>, TriangularMatrix<T, TS>, TriangularMatrix<T, TS>>,
		IMatrixAddOperations<T, SymmetricMatrix<T, TS>, SymmetricMatrix<T, TS>, SymmetricMatrix<T, TS>>,
		IMatrixMultiplyOperations<T, SymmetricMatrix<T, TS>, SymmetricMatrix<T, TS>, DenseMatrix<T, TS>>,
		IMatrixMultiplyOperations<T, DiagonalMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>,
		IMatrixMultiplyOperations<T, DenseMatrix<T, TS>, DiagonalMatrix<T, TS>, DenseMatrix<T, TS>>,

		IMatrixLinearSolve<T, DenseMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>,
		IMatrixLinearSolve<T, TriangularMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>,

		ITensorOperations<T, DenseTensor<T, TS>, DenseTensor<T, TS>>,
		ITensorOperations<T, DenseTensor<T, TS>, DenseTensor<T, TS>, DenseTensor<T, TS>>

		where T : unmanaged, INumber<T>
		where TS : class, IStorage<T, TS>
	{
		#region vector
		/// <inheritdoc/>
		public static T Dot(DenseVector<T, TS> left, DenseVector<T, TS> right, bool conjugateLeft = true) => Blas.Dot<T, TS, TS>(conjugateLeft, left.Storage, left.Stride, right.Storage, right.Stride);

		/// <inheritdoc/>
		public static void AddBy(DenseVector<T, TS> left, DenseVector<T, TS> right, T scalar) => Blas.Add(scalar, right.Storage, right.Stride, left.Storage, left.Stride);
		#endregion

		#region matrix diag
		/// <inheritdoc/>
		public static DenseVector<T, TS> GetDiag(DenseMatrix<T, TS> matrix, long k)
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
		public static void GetDiag(DenseMatrix<T, TS> matrix, long k, DenseVector<T, TS> overwrite) => GetDiag(matrix, k).CopyTo(overwrite);

		/// <inheritdoc/>
		public static void SetDiag(DenseMatrix<T, TS> matrix, long k, DenseVector<T, TS> value) => value.CopyTo(GetDiag(matrix, k));

		/// <inheritdoc/>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="k"/> refers to diagonal elements not used</exception>
		public static DenseVector<T, TS> GetDiag(TriangularMatrix<T, TS> matrix, long k)
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
		public static void GetDiag(TriangularMatrix<T, TS> matrix, long k, DenseVector<T, TS> overwrite) => GetDiag(matrix, k).CopyTo(overwrite);

		/// <inheritdoc/>
		public static void SetDiag(TriangularMatrix<T, TS> matrix, long k, DenseVector<T, TS> value) => value.CopyTo(GetDiag(matrix, k));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static DenseVector<T, TS> GetDiagRaw(SymmetricMatrix<T, TS> matrix, long k)
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
		/// <remarks>If <paramref name="k"/> refers to values not stored, a <b>new</b> <see cref="DenseVector{T, TS}"/> will be returned.</remarks>
		public static DenseVector<T, TS> GetDiag(SymmetricMatrix<T, TS> matrix, long k)
		{
			if ((matrix.Upper && k < 0) || (!matrix.Upper && k > 0))
			{
				var vec = GetDiagRaw(matrix, -k);
				if (!matrix.Hermitian)
					return vec;
				var output = vec.ToCompact();
				try
				{
					ExtBlas.PointWiseConjugate<T, TS>(output, 1);
					return new(output, output.Length);
				}
				catch (Exception)
				{
					output.Dispose();
					throw;
				}
			}
			return GetDiagRaw(matrix, k);
		}

		/// <inheritdoc/>
		public static void GetDiag(SymmetricMatrix<T, TS> matrix, long k, DenseVector<T, TS> overwrite)
		{
			GetDiagRaw(matrix, k).CopyTo(overwrite);
			if (!matrix.Hermitian || !((matrix.Upper && k < 0) || (!matrix.Upper && k > 0)))
				return;
			overwrite.Conjugate();
		}

		/// <inheritdoc/>
		public static void SetDiag(SymmetricMatrix<T, TS> matrix, long k, DenseVector<T, TS> value)
		{
			var vec = GetDiagRaw(matrix, k);
			value.CopyTo(vec);
			if (!matrix.Hermitian || !((matrix.Upper && k < 0) || (!matrix.Upper && k > 0)))
				return;
			vec.Conjugate();
		}
		#endregion

		#region matrix vector
		/// <inheritdoc/>
		public static DenseVector<T, TS> MatrixMultiplyVector(DenseMatrix<T, TS> matrix, DenseVector<T, TS> vector, T α, MatrixOperation operation = MatrixOperation.None)
		{
			var output = IMatrixVectorMultiplyOperations<T, DenseVector<T, TS>, DenseVector<T, TS>, DenseMatrix<T, TS>>.CheckMatMulVec(matrix, vector, vector.Storage, α, operation);
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
		public static void MatrixMultiplyVector(DenseMatrix<T, TS> matrix, DenseVector<T, TS> vector, DenseVector<T, TS> vectorOut, T α, T β = default, MatrixOperation operation = MatrixOperation.None)
		{
			IMatrixVectorMultiplyOperations<T, DenseVector<T, TS>, DenseVector<T, TS>, DenseMatrix<T, TS>>.CheckMatMulVec(matrix, vector, vectorOut, α, operation);
			Blas.GeneralMatrixMultiplyVector(operation, matrix.NRows, matrix.NCols, α, matrix.Storage, matrix.LeadDim, vector.Storage, vector.Stride, β, vectorOut.Storage, vectorOut.Stride);
		}

		/// <inheritdoc/>
		public static DenseVector<T, TS> MatrixMultiplyVector(TriangularMatrix<T, TS> matrix, DenseVector<T, TS> vector, T α, MatrixOperation operation = MatrixOperation.None)
		{
			var output = IMatrixVectorMultiplyOperations<T, DenseVector<T, TS>, DenseVector<T, TS>, TriangularMatrix<T, TS>>.CheckMatMulVec(matrix, vector, vector.Storage, α, operation);
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
		public static void MatrixMultiplyVector(TriangularMatrix<T, TS> matrix, DenseVector<T, TS> vector, DenseVector<T, TS> vectorOut, T α, T β = default, MatrixOperation operation = MatrixOperation.None)
		{
			IMatrixVectorMultiplyOperations<T, DenseVector<T, TS>, DenseVector<T, TS>, TriangularMatrix<T, TS>>.CheckMatMulVec(matrix, vector, vectorOut, α, operation);
			Blas.TriangularMatrixMultiplyVector(matrix.Upper, matrix.UnitDiagonal, operation, matrix.NRows, matrix.NCols, matrix.Storage, matrix.LeadDim, α, vector.Storage, vector.Stride, β, vectorOut.Storage, vectorOut.Stride);
		}

		/// <inheritdoc/>
		public static DenseVector<T, TS> MatrixMultiplyVector(SymmetricMatrix<T, TS> matrix, DenseVector<T, TS> vector, T α, MatrixOperation operation = MatrixOperation.None)
		{
			var output = IMatrixVectorMultiplyOperations<T, DenseVector<T, TS>, DenseVector<T, TS>, SymmetricMatrix<T, TS>>.CheckMatMulVec(matrix, vector, vector.Storage, α, operation);
			try
			{
				Blas.SymmetricMatrixMultiplyVector(matrix.Upper, matrix.Hermitian, matrix.NRows, α, matrix.Storage, matrix.LeadDim, vector.Storage, vector.Stride, T.Zero, output, 1);
				return new(output, output.Length);
			}
			catch (Exception)
			{
				output.Dispose();
				throw;
			}
		}

		/// <inheritdoc/>
		public static void MatrixMultiplyVector(SymmetricMatrix<T, TS> matrix, DenseVector<T, TS> vector, DenseVector<T, TS> vectorOut, T α, T β = default, MatrixOperation operation = MatrixOperation.None)
		{
			IMatrixVectorMultiplyOperations<T, DenseVector<T, TS>, DenseVector<T, TS>, SymmetricMatrix<T, TS>>.CheckMatMulVec(matrix, vector, vectorOut, α, operation);
			Blas.SymmetricMatrixMultiplyVector(matrix.Upper, matrix.Hermitian, matrix.NRows, α, matrix.Storage, matrix.LeadDim, vector.Storage, vector.Stride, T.Zero, vectorOut.Storage, vectorOut.Stride);
		}
		#endregion

		#region matrix out-of-place add multiply
		/// <inheritdoc/>
		public static DenseMatrix<T, TS> AddMatrices(DenseMatrix<T, TS>? A, T scalarA, DenseMatrix<T, TS>? B, T scalarB, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			var output = IMatrixOperations<T, DenseMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>.CheckAdd(A, scalarA, B, scalarB, opA, opB, A?.Storage ?? B?.Storage, out long m, out long n);
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
		public static DenseMatrix<T, TS> MultiplyMatries(DenseMatrix<T, TS> A, DenseMatrix<T, TS> B, T α, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			var output = IMatrixOperations<T, DenseMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>.CheckMul(α, A, B, opA, opB, A.Storage, out long m, out long n, out long k);
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
		public static DenseMatrix<T, TS> AddMatrices(TriangularMatrix<T, TS>? A, T scalarA, DenseMatrix<T, TS>? B, T scalarB, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			var temp = IMatrixOperations<T, TriangularMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>.CheckAdd(A, scalarA, B, scalarB, opA, opB, A?.Storage ?? B?.Storage, out long m, out long n);
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
		public static DenseMatrix<T, TS> AddMatrices(DenseMatrix<T, TS>? A, T scalarA, TriangularMatrix<T, TS>? B, T scalarB, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None) => AddMatrices(B, scalarB, A, scalarA, opB, opA);

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> MultiplyMatries(TriangularMatrix<T, TS> A, DenseMatrix<T, TS> B, T α, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			var output = IMatrixOperations<T, TriangularMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>.CheckMul(α, A, B, opA, opB, B.Storage, out long m, out long n, out long k);
			try
			{
				Blas.TriangularMatrixMultiply(true, A.Upper, A.UnitDiagonal, opA, opB, m, n, k, α, A.Storage, A.LeadDim, B.Storage, B.LeadDim, T.Zero, output, m);
				return new(output, m, n);
			}
			catch (Exception)
			{
				output.Dispose();
				throw;
			}
		}

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> MultiplyMatries(DenseMatrix<T, TS> A, TriangularMatrix<T, TS> B, T α, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			var output = IMatrixOperations<T, DenseMatrix<T, TS>, TriangularMatrix<T, TS>, DenseMatrix<T, TS>>.CheckMul(α, A, B, opA, opB, A.Storage, out long m, out long n, out long k);
			try
			{
				Blas.TriangularMatrixMultiply(false, B.Upper, B.UnitDiagonal, opB, opA, m, n, k, α, B.Storage, B.LeadDim, A.Storage, A.LeadDim, T.Zero, output, m);
				return new(output, m, n);
			}
			catch (Exception)
			{
				output.Dispose();
				throw;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static (bool upper, bool unitDiag) CheckUpper(TriangularMatrix<T, TS>? A, TriangularMatrix<T, TS>? B, MatrixOperation opA, MatrixOperation opB)
		{
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
			if (upperA != upperB || (unitA && unitB))
				throw new ArgumentException(Resources.ParameterError.InvalidValue);
			if ((A is not null || B is not null) && (unitA || unitB))
				throw new ArgumentException(Resources.ParameterError.InvalidValue);
			return (upperA, unitA || unitB);
		}

		/// <inheritdoc/>
		public static TriangularMatrix<T, TS> AddMatrices(TriangularMatrix<T, TS>? A, T scalarA, TriangularMatrix<T, TS>? B, T scalarB, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
#pragma warning disable CS8604
			if (A is null || scalarA == T.Zero)
				return OutOfPlaceOp(B, scalarB, opB);
			if (B is null || scalarB == T.Zero)
				return OutOfPlaceOp(A, scalarA, opA);
#pragma warning restore CS8604
			var (upper, unit) = CheckUpper(A, B, opA, opB);
			var output = IMatrixOperations<T, TriangularMatrix<T, TS>, TriangularMatrix<T, TS>, TriangularMatrix<T, TS>>.CheckAdd(A, scalarA, B, scalarB, opA, opB, A?.Storage ?? B?.Storage, out long m, out long n);
			try
			{
				HalfBlas.TriangularMatricesAdd(unit, upper, opA, opB, m, n, scalarA, A?.Storage, A?.LeadDim ?? 1, scalarB, B?.Storage, B?.LeadDim ?? 1, output, m);
				return new TriangularMatrix<T, TS>(true, output, m, n, 0, unit);
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
				return new(!matrix.Upper, output, matrix.NCols, matrix.NRows, 0, matrix.UnitDiagonal);
			}
			catch (Exception)
			{
				output.Dispose();
				throw;
			}
		}

		/// <inheritdoc/>
		public static TriangularMatrix<T, TS> MultiplyMatries(TriangularMatrix<T, TS> A, TriangularMatrix<T, TS> B, T α, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			bool upperA = A.Upper == opA.CanInPlace();
			bool upperB = B.Upper == opB.CanInPlace();
			if (upperA != upperB || A.UnitDiagonal != B.UnitDiagonal)
				throw new ArgumentException(Resources.ParameterError.InvalidValue);
			var output = IMatrixOperations<T, TriangularMatrix<T, TS>, TriangularMatrix<T, TS>, TriangularMatrix<T, TS>>.CheckMul(α, A, B, opA, opB, A.Storage, out long m, out long n, out long k);
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
		public static DenseMatrix<T, TS> AddMatrices(SymmetricMatrix<T, TS>? A, T scalarA, DenseMatrix<T, TS>? B, T scalarB, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			var temp = IMatrixOperations<T, SymmetricMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>.CheckAdd(A, scalarA, B, scalarB, opA, opB, A?.Storage ?? B?.Storage, out long m, out long n);
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
		public static DenseMatrix<T, TS> AddMatrices(DenseMatrix<T, TS>? A, T scalarA, SymmetricMatrix<T, TS>? B, T scalarB, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None) => AddMatrices(B, scalarB, A, scalarA, opB, opA);

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> MultiplyMatries(SymmetricMatrix<T, TS> A, DenseMatrix<T, TS> B, T α, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			var output = IMatrixOperations<T, SymmetricMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>.CheckMul(α, A, B, opA, opB, B.Storage, out long m, out long n, out _);
			try
			{
				Blas.SymmetricMatrixMultiplyGeneral(A.Upper, true, A.Hermitian, opA, opB, m, n, α, A.Storage, A.LeadDim, B.Storage, B.LeadDim, T.Zero, output, m);
				return new(output, m, n);
			}
			catch (Exception)
			{
				output.Dispose();
				throw;
			}
		}

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> MultiplyMatries(DenseMatrix<T, TS> A, SymmetricMatrix<T, TS> B, T α, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			var output = IMatrixOperations<T, DenseMatrix<T, TS>, SymmetricMatrix<T, TS>, DenseMatrix<T, TS>>.CheckMul(α, A, B, opA, opB, B.Storage, out long m, out long n, out _);
			try
			{
				Blas.SymmetricMatrixMultiplyGeneral(B.Upper, false, B.Hermitian, opB, opA, m, n, α, B.Storage, B.LeadDim, A.Storage, A.LeadDim, T.Zero, output, m);
				return new(output, m, n);
			}
			catch (Exception)
			{
				output.Dispose();
				throw;
			}
		}

		/// <inheritdoc/>
		public static SymmetricMatrix<T, TS> AddMatrices(SymmetricMatrix<T, TS>? A, T scalarA, SymmetricMatrix<T, TS>? B, T scalarB, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			opA = opA.Simplify<T>(A?.Hermitian);
			opB = opB.Simplify<T>(B?.Hermitian);
			var output = IMatrixOperations<T, SymmetricMatrix<T, TS>, SymmetricMatrix<T, TS>, SymmetricMatrix<T, TS>>.CheckAdd(A, scalarA, B, scalarB, opA, opB, A?.Storage ?? B?.Storage, out _, out long n);
			try
			{
				HalfBlas.SymmetricMatricesAdd(A?.Upper ?? true, B?.Upper ?? true, true, opA, opB, n, scalarA, A?.Storage, A?.LeadDim ?? 1, scalarB, B?.Storage, B?.LeadDim ?? 1, output, n);
				return new(true, output, n);
			}
			catch (Exception)
			{
				output.Dispose();
				throw;
			}
		}

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> MultiplyMatries(SymmetricMatrix<T, TS> A, SymmetricMatrix<T, TS> B, T α, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			var output = IMatrixOperations<T, SymmetricMatrix<T, TS>, SymmetricMatrix<T, TS>, DenseMatrix<T, TS>>.CheckMul(α, A, B, opA, opB, A.Storage, out _, out long n, out _);
			opA = opA.Simplify<T>(A.Hermitian);
			opB = opB.Simplify<T>(B.Hermitian);
			try
			{
				HalfBlas.SymmetricMatricesMultiply(A.Upper, B.Upper, A.Hermitian, B.Hermitian, opA, opB, n, α, A.Storage, A.LeadDim, B.Storage, B.LeadDim, T.Zero, output, n);
				return new(output, n, n);
			}
			catch (Exception)
			{
				output.Dispose();
				throw;
			}
		}

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> MultiplyMatries(DiagonalMatrix<T, TS> A!!, DenseMatrix<T, TS> B!!, T α, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			var output = IMatrixOperations<T, DiagonalMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>.CheckMul(α, A, B, opA, opB, B.Storage, out long m, out long n, out _);
			opA = opA.Simplify<T>(false);
			try
			{
				ExtBlas.DiagonalMatrixMultiplyGeneral(false, opB, opA == MatrixOperation.Conjugate, m, n, α, B.Storage, B.LeadDim, A.Storage, A.Stride, T.Zero, output, m);
				return new(output, m, n);
			}
			catch (Exception)
			{
				output.Dispose();
				throw;
			}
		}

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> MultiplyMatries(DenseMatrix<T, TS> A!!, DiagonalMatrix<T, TS> B!!, T α, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			var output = IMatrixOperations<T, DenseMatrix<T, TS>, DiagonalMatrix<T, TS>, DenseMatrix<T, TS>>.CheckMul(α, A, B, opA, opB, A.Storage, out long m, out long n, out _);
			opB = opB.Simplify<T>(false);
			try
			{
				ExtBlas.DiagonalMatrixMultiplyGeneral(true, opA, opB == MatrixOperation.Conjugate, m, n, α, A.Storage, A.LeadDim, B.Storage, B.Stride, T.Zero, output, m);
				return new(output, m, n);
			}
			catch (Exception)
			{
				output.Dispose();
				throw;
			}
		}
		#endregion

		#region matrix in-place add multiply
		/// <inheritdoc/>
		public static void AddMatrices(DenseMatrix<T, TS>? A, T scalarA, DenseMatrix<T, TS>? B, T scalarB, DenseMatrix<T, TS> C, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			var (m, n) = IMatrixOperations<T, DenseMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>.CheckAdd(A, scalarA, B, scalarB, C, opA, opB);
			ExtBlas.GeneralMatricesAdd(opA, opB, m, n, scalarA, A?.Storage, A?.LeadDim ?? 1, scalarB, B?.Storage, B?.LeadDim ?? 1, C.Storage, C.LeadDim);
		}

		/// <inheritdoc/>
		public static void MultiplyMatries(DenseMatrix<T, TS> A, DenseMatrix<T, TS> B, T α, T β, DenseMatrix<T, TS> C, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			var (m, n, k) = IMatrixOperations<T, DenseMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>.CheckMul(α, A, B, C, opA, opB);
			Blas.GeneralMatricesMultiply(opA, opB, m, n, k, α, A.Storage, A.LeadDim, B.Storage, B.LeadDim, β, C.Storage, C.LeadDim);
		}

		/// <inheritdoc/>
		public static void AddMatrices(TriangularMatrix<T, TS>? A, T scalarA, DenseMatrix<T, TS>? B, T scalarB, DenseMatrix<T, TS> C, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			if (A is null || scalarA == T.Zero)
			{
				AddMatrices((DenseMatrix<T, TS>?)null, T.Zero, B, scalarB, C, opA, opB);
				return;
			}
			var (m, n) = IMatrixOperations<T, TriangularMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>.CheckAdd(A, scalarA, B, scalarB, C, opA, opB);
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
				ExtBlas.FillWithValue(C.Storage, C.LeadDim + 1, T.One);
			AddMatrices(C, scalarA, B, scalarB, C, opA, opB);
		}

		/// <inheritdoc/>
		public static void MultiplyMatries(TriangularMatrix<T, TS> A, DenseMatrix<T, TS> B, T α, T β, DenseMatrix<T, TS> C, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			var (m, n, k) = IMatrixOperations<T, TriangularMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>.CheckMul(α, A, B, C, opA, opB);
			Blas.TriangularMatrixMultiply(true, A.Upper, A.UnitDiagonal, opA, opB, m, n, k, α, A.Storage, A.LeadDim, B.Storage, B.LeadDim, β, C.Storage, C.LeadDim);
		}

		/// <inheritdoc/>
		public static void AddMatrices(DenseMatrix<T, TS>? A, T scalarA, TriangularMatrix<T, TS>? B, T scalarB, DenseMatrix<T, TS> C, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None) => AddMatrices(B, scalarB, A, scalarA, C, opA, opB);

		/// <inheritdoc/>
		public static void MultiplyMatries(DenseMatrix<T, TS> A, TriangularMatrix<T, TS> B, T α, T β, DenseMatrix<T, TS> C, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			var (m, n, k) = IMatrixOperations<T, DenseMatrix<T, TS>, TriangularMatrix<T, TS>, DenseMatrix<T, TS>>.CheckMul(α, A, B, C, opA, opB);
			Blas.TriangularMatrixMultiply(false, B.Upper, B.UnitDiagonal, opB, opA, m, n, k, α, B.Storage, B.LeadDim, A.Storage, A.LeadDim, β, C.Storage, C.LeadDim);
		}

		/// <inheritdoc/>
		/// <exception cref="ArgumentException">If the <see cref="TriangularMatrix{T, TS}.Upper"/>s or <see cref="TriangularMatrix{T, TS}.UnitDiagonal"/>s are incompatible</exception>
		public static void AddMatrices(TriangularMatrix<T, TS>? A, T scalarA, TriangularMatrix<T, TS>? B, T scalarB, TriangularMatrix<T, TS> C, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			var (m, n) = IMatrixOperations<T, TriangularMatrix<T, TS>, TriangularMatrix<T, TS>, TriangularMatrix<T, TS>>.CheckAdd(A, scalarA, B, scalarB, C, opA, opB);
			var (upper, unit) = CheckUpper(A, B, opA, opB);
			if (upper != C.Upper || unit != C.UnitDiagonal)
				throw new ArgumentException(Resources.ParameterError.InvalidValue);
			HalfBlas.TriangularMatricesAdd(unit, upper, opA, opB, m, n, scalarA, A?.Storage, A?.LeadDim ?? 1, scalarB, B?.Storage, B?.LeadDim ?? 1, C.Storage, C.LeadDim);
		}

		/// <inheritdoc/>
		/// <exception cref="ArgumentException">If the <see cref="TriangularMatrix{T, TS}.Upper"/>s or <see cref="TriangularMatrix{T, TS}.UnitDiagonal"/>s are incompatible</exception>
		public static void MultiplyMatries(TriangularMatrix<T, TS> A, TriangularMatrix<T, TS> B, T α, T β, TriangularMatrix<T, TS> C, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			var (m, n, k) = IMatrixOperations<T, TriangularMatrix<T, TS>, TriangularMatrix<T, TS>, TriangularMatrix<T, TS>>.CheckMul(α, A, B, C, opA, opB);
			bool upperA = A.Upper == opA.CanInPlace();
			bool upperB = B.Upper == opB.CanInPlace();
			if (upperA != upperB || upperA != C.Upper || A.UnitDiagonal != B.UnitDiagonal || A.UnitDiagonal != C.UnitDiagonal)
				throw new ArgumentException(Resources.ParameterError.InvalidValue);
			HalfBlas.TriangularMatricesMultiply(A.UnitDiagonal, upperA, opA, opB, m, n, k, α, A.Storage, A.LeadDim, B.Storage, B.LeadDim, β, C.Storage, C.LeadDim);
		}


		/// <inheritdoc/>
		public static void AddMatrices(SymmetricMatrix<T, TS>? A, T scalarA, DenseMatrix<T, TS>? B, T scalarB, DenseMatrix<T, TS> C, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			if (A is null || scalarA == T.Zero)
			{
				AddMatrices((DenseMatrix<T, TS>?)null, T.Zero, B, scalarB, C, opA, opB);
				return;
			}
			var (m, n) = IMatrixOperations<T, SymmetricMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>.CheckAdd(A, scalarA, B, scalarB, C, opA, opB);
			if (opA.CanInPlace())
			{
				A.Storage.Copy2DTo<T, TS, TS>(A.LeadDim, C.Storage, C.LeadDim, m, n);
			}
			else
			{
				HalfBlas.HalfMatrixCopy<T, TS, TS>(A.Upper, true, MatrixOperation.Transpose, m, n, A.Storage, A.LeadDim, C.Storage, C.LeadDim);
				opA = opA.Transpose();
			}
			HalfBlas.SymmetricMatrixToNormal<T, TS>(A.Upper, A.Hermitian, n, C.Storage, C.LeadDim);
			AddMatrices(C, scalarA, B, scalarB, C, opA, opB);
		}

		/// <inheritdoc/>
		public static void MultiplyMatries(SymmetricMatrix<T, TS> A, DenseMatrix<T, TS> B, T α, T β, DenseMatrix<T, TS> C, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			var (m, n, _) = IMatrixOperations<T, SymmetricMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>.CheckMul(α, A, B, C, opA, opB);
			Blas.SymmetricMatrixMultiplyGeneral(A.Upper, true, A.Hermitian, opA, opB, m, n, α, A.Storage, A.LeadDim, B.Storage, B.LeadDim, β, C.Storage, C.LeadDim);
		}

		/// <inheritdoc/>
		public static void AddMatrices(DenseMatrix<T, TS>? A, T scalarA, SymmetricMatrix<T, TS>? B, T scalarB, DenseMatrix<T, TS> C, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None) => AddMatrices(B, scalarB, A, scalarA, C, opA, opB);

		/// <inheritdoc/>
		public static void MultiplyMatries(DenseMatrix<T, TS> A, SymmetricMatrix<T, TS> B, T α, T β, DenseMatrix<T, TS> C, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			var (m, n, _) = IMatrixOperations<T, DenseMatrix<T, TS>, SymmetricMatrix<T, TS>, DenseMatrix<T, TS>>.CheckMul(α, A, B, C, opA, opB);
			Blas.SymmetricMatrixMultiplyGeneral(B.Upper, false, B.Hermitian, opB, opA, m, n, α, B.Storage, B.LeadDim, A.Storage, A.LeadDim, β, C.Storage, C.LeadDim);
		}

		/// <inheritdoc/>
		public static void AddMatrices(SymmetricMatrix<T, TS>? A, T scalarA, SymmetricMatrix<T, TS>? B, T scalarB, SymmetricMatrix<T, TS> C, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None) => AddMatrices(B, scalarB, A, scalarA, C, opA, opB);

		/// <inheritdoc/>
		public static void MultiplyMatries(SymmetricMatrix<T, TS> A, SymmetricMatrix<T, TS> B, T α, T β, DenseMatrix<T, TS> C, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			var (_, n, _) = IMatrixOperations<T, SymmetricMatrix<T, TS>, SymmetricMatrix<T, TS>, DenseMatrix<T, TS>>.CheckMul(α, A, B, C, opA, opB);
			HalfBlas.SymmetricMatricesMultiply(A.Upper, B.Upper, A.Hermitian, B.Hermitian, opA, opB, n, α, A.Storage, A.LeadDim, B.Storage, B.LeadDim, β, C.Storage, C.LeadDim);
		}

		/// <inheritdoc/>
		public static void MultiplyMatries(DiagonalMatrix<T, TS> A!!, DenseMatrix<T, TS> B!!, T α, T β, DenseMatrix<T, TS> C!!, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			var (m, n, _) = IMatrixOperations<T, DiagonalMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>.CheckMul(α, A, B, C, opA, opB);
			opA = opA.Simplify<T>(false);
			ExtBlas.DiagonalMatrixMultiplyGeneral(false, opB, opA == MatrixOperation.Conjugate, m, n, α, B.Storage, B.LeadDim, A.Storage, A.Stride, β, C.Storage, C.LeadDim);
		}

		/// <inheritdoc/>
		public static void MultiplyMatries(DenseMatrix<T, TS> A!!, DiagonalMatrix<T, TS> B!!, T α, T β, DenseMatrix<T, TS> C!!, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			var (m, n, _) = IMatrixOperations<T, DenseMatrix<T, TS>, DiagonalMatrix<T, TS>, DenseMatrix<T, TS>>.CheckMul(α, A, B, C, opA, opB);
			opB = opB.Simplify<T>(false);
			ExtBlas.DiagonalMatrixMultiplyGeneral(true, opA, opB == MatrixOperation.Conjugate, m, n, α, A.Storage, A.LeadDim, B.Storage, B.Stride, β, C.Storage, C.LeadDim);
		}
		#endregion

		#region matrix update
		/// <summary>
		/// Update the <paramref name="matrix"/> by adding the outer product of two vectors <paramref name="left"/> and <paramref name="right"/>.
		/// </summary>
		/// <param name="matrix">The matrix to be updated</param>
		/// <param name="left">The left vector to outer product</param>
		/// <param name="right">The right vector to outer product</param>
		/// <param name="α">The scalar to multiply to <paramref name="left"/> or <paramref name="right"/></param>
		/// <param name="β">The scalar to multiply to <paramref name="matrix"/></param>
		/// <param name="conjugateRight">Whether to use the conjugate transpose of <paramref name="right"/> or simply transpose.</param>
		public static void RankOneUpdate(DenseMatrix<T, TS> matrix!!, DenseVector<T, TS> left!!, DenseVector<T, TS> right!!, T α, T β = default, bool conjugateRight = true)
		{
			if (matrix.NRows != left.Length || matrix.NCols != right.Length)
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(matrix));
			if (α == T.Zero)
				throw new ArgumentOutOfRangeException(nameof(α), Resources.ParameterError.CannotZero);
			if (!NumberType<T>.IsComplex)
				conjugateRight = false;
			Blas.GeneralRankOneUpdate(conjugateRight, matrix.NRows, matrix.NCols, α, left.Storage, left.Stride, right.Storage, right.Stride, β, matrix.Storage, matrix.LeadDim);
		}

		/// <summary>
		/// Symmetrically update the <paramref name="matrix"/> by adding the outer product of the <paramref name="vector"/>.
		/// </summary>
		/// <param name="matrix">The matrix to be updated</param>
		/// <param name="vector">The left and right vector to outer product</param>
		/// <param name="α">The scalar to multiply to <paramref name="vector"/></param>
		/// <param name="β">The scalar to multiply to <paramref name="matrix"/></param>
		public static void RankOneUpdate(SymmetricMatrix<T, TS> matrix!!, DenseVector<T, TS> vector!!, T α, T β = default)
		{
			if (matrix.NRows != vector.Length)
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(matrix));
			if (α == T.Zero)
				throw new ArgumentOutOfRangeException(nameof(α), Resources.ParameterError.CannotZero);
			Blas.SymmetricRankOneUpdate(matrix.Upper, matrix.Hermitian, matrix.NRows, α, vector.Storage, vector.Stride, β, matrix.Storage, matrix.LeadDim);
		}

		/// <summary>
		/// Update the <paramref name="matrix"/> by adding the sum of two outer products: <c><paramref name="α"/> * (<paramref name="x"/> * <paramref name="y"/>^T + <paramref name="y"/> * <paramref name="x"/>^T)</c>.
		/// </summary>
		/// <param name="matrix">The matrix to be updated</param>
		/// <param name="x">The vector to outer product</param>
		/// <param name="y">The vector to outer product</param>
		/// <param name="α">The scalar to multiply to <paramref name="x"/> or <paramref name="y"/></param>
		/// <param name="β">The scalar to multiply to <paramref name="matrix"/></param>
		public static void RankTwoUpdate(SymmetricMatrix<T, TS> matrix!!, DenseVector<T, TS> x!!, DenseVector<T, TS> y!!, T α, T β = default)
		{
			if (matrix.NRows != x.Length)
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(matrix));
			if (y.Length != x.Length)
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(y));
			if (α == T.Zero)
				throw new ArgumentOutOfRangeException(nameof(α), Resources.ParameterError.CannotZero);
			Blas.SymmetricRankTwoUpdate(matrix.Upper, matrix.Hermitian, matrix.NRows, α, x.Storage, x.Stride, y.Storage, y.Stride, β, matrix.Storage, matrix.LeadDim);
		}

		/// <summary>
		/// Symmetrically update the matrix <paramref name="A"/> by adding the positive-definite product of the matrix <paramref name="B"/>.
		/// </summary>
		/// <param name="A">The matrix to be updated</param>
		/// <param name="B">The left and right matrix to positive-definite product</param>
		/// <param name="α">The scalar to multiply to <paramref name="B"/></param>
		/// <param name="β">The scalar to multiply to <paramref name="A"/></param>
		/// <param name="opB">The operation to be applied to <paramref name="B"/></param>
		public static void RankKUpdate(SymmetricMatrix<T, TS> A!!, DenseMatrix<T, TS> B!!, T α, T β = default, MatrixOperation opB = MatrixOperation.None)
		{
			var (n, k) = (B.NRows, B.NCols);
			if (!opB.CanInPlace())
				(n, k) = (k, n);
			if (A.NRows != n)
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(A));
			if (α == T.Zero)
				throw new ArgumentOutOfRangeException(nameof(α), Resources.ParameterError.CannotZero);
			Blas.SymmetricRankKUpdate(A.Upper, opB, A.Hermitian, n, k, α, B.Storage, B.LeadDim, β, A.Storage, A.LeadDim);
		}

		/// <summary>
		/// Symmetrically update the matrix <paramref name="A"/> by adding the positive-definite products of the matrices: <c><paramref name="α"/> * (<paramref name="B"/> * <paramref name="C"/>^T + <paramref name="C"/> * <paramref name="B"/>^T)</c>.
		/// </summary>
		/// <param name="A">The matrix to be updated</param>
		/// <param name="B">The left and right matrix to positive-definite product</param>
		/// <param name="C">Another left and right matrix to positive-definite product</param>
		/// <param name="α">The scalar to multiply to <paramref name="B"/> or <paramref name="C"/></param>
		/// <param name="β">The scalar to multiply to <paramref name="A"/></param>
		/// <param name="op">The operation to be applied to both <paramref name="B"/> and <paramref name="C"/></param>
		public static void RankTwoKUpdate(SymmetricMatrix<T, TS> A!!, DenseMatrix<T, TS> B!!, DenseMatrix<T, TS> C!!, T α, T β = default, MatrixOperation op = MatrixOperation.None)
		{
			var (n, k) = (B.NRows, B.NCols);
			if (!op.CanInPlace())
				(n, k) = (k, n);
			if (A.NRows != n)
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(A));
			if (C.NRows != B.NRows || C.NCols != B.NCols)
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(C));
			if (α == T.Zero)
				throw new ArgumentOutOfRangeException(nameof(α), Resources.ParameterError.CannotZero);
			Blas.SymmetricRankTwoKUpdate(A.Upper, op, A.Hermitian, n, k, α, B.Storage, B.LeadDim, C.Storage, C.LeadDim, β, A.Storage, A.LeadDim);
		}

		/// <summary>
		/// Update the square matrix <paramref name="A"/> by adding the product of matrices <paramref name="left"/> and <paramref name="right"/>.
		/// </summary>
		/// <param name="A">The matrix to be updated</param>
		/// <param name="left">The left matrix to product</param>
		/// <param name="right">The right matrix to product</param>
		/// <param name="α">The scalar to multiply to <paramref name="left"/> or <paramref name="right"/></param>
		/// <param name="β">The scalar to multiply to <paramref name="A"/></param>
		/// <param name="op">The operation to be applied to both <paramref name="left"/> and <paramref name="right"/></param>
		/// <param name="conjugateRight">Whether to use the conjugate transpose of <paramref name="right"/> or simply transpose.</param>
		public static void RankKUpdate(DenseMatrix<T, TS> A!!, DenseMatrix<T, TS> left!!, DenseMatrix<T, TS> right!!, T α, T β = default, MatrixOperation op = MatrixOperation.None, bool conjugateRight = true)
		{
			if (!NumberType<T>.IsComplex)
				conjugateRight = false;
			var (n, k) = (left.NRows, left.NCols);
			if (!op.CanInPlace())
				(n, k) = (k, n);
			if (A.NRows != n || A.NCols != n)
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(A));
			if (right.NRows != left.NRows || right.NCols != left.NCols)
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(right));
			if (α == T.Zero)
				throw new ArgumentOutOfRangeException(nameof(α), Resources.ParameterError.CannotZero);
			Blas.GeneralRankKUpdate(op, conjugateRight, n, k, α, left.Storage, left.LeadDim, right.Storage, right.LeadDim, β, A.Storage, A.LeadDim);
		}
		#endregion

		#region matrix solve
		/// <inheritdoc/>
		public static void LinearSolve(DenseMatrix<T, TS> coefficients, DenseMatrix<T, TS> rightHandSides, DenseMatrix<T, TS> outSolves, MatrixOperation opCoef = MatrixOperation.None)
		{
			IMatrixLinearSolve<T, DenseMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>.CheckLinear(coefficients, rightHandSides, outSolves);
			if (rightHandSides != outSolves)
				rightHandSides.Storage.Copy2DTo<T, TS, TS>(rightHandSides.LeadDim, outSolves.Storage, outSolves.LeadDim, outSolves.NRows, outSolves.NCols);
			using var coef = coefficients.ToCompact();
			Lapack.LinearSolveGeneral<T, TS, TS>(opCoef, coefficients.NRows, outSolves.NCols, coef, coefficients.NRows, outSolves.Storage, outSolves.LeadDim);
		}

		/// <inheritdoc/>
		public static void LinearSolve(TriangularMatrix<T, TS> coefficients, DenseMatrix<T, TS> rightHandSides, DenseMatrix<T, TS> outSolves, MatrixOperation opCoef = MatrixOperation.None)
		{
			if (coefficients.NRows != coefficients.NCols)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(coefficients));
			if (coefficients.NRows != outSolves.NRows)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(outSolves));
			Blas.TriangularMatrixSolve(true, coefficients.Upper, coefficients.UnitDiagonal, opCoef, coefficients.NRows, outSolves.NCols, T.One, coefficients.Storage, coefficients.LeadDim, outSolves.Storage, outSolves.LeadDim);
		}
		#endregion

		#region tensor
		/// <inheritdoc/>
		public static void Reduce(DenseTensor<T, TS> A!!, TensorOrder order, T α, DenseTensor<T, TS> B!!, T β = default, UnaryOperation opA = UnaryOperation.Identity, UnaryOperation opB = UnaryOperation.Identity, BinaryOperation reduce = BinaryOperation.Addition)
		{
			Span<int> reduceInd = stackalloc int[A.Rank];
			reduceInd = ITensorOperations<T, DenseTensor<T, TS>, DenseTensor<T, TS>>.CheckReduce(A, order, α, B, reduceInd);
			Ten.Reduce<T, TS, TS>(reduce, new(A, opA, α), new(B, opB, β), reduceInd);
		}

		/// <inheritdoc/>
		public static void Permute(DenseTensor<T, TS> A!!, TensorOrder order, T scalar, DenseTensor<T, TS> B!!, UnaryOperation op = UnaryOperation.Identity)
		{
			Span<int> perm = stackalloc int[A.Rank];
			ITensorOperations<T, DenseTensor<T, TS>, DenseTensor<T, TS>>.CheckPermute(A, order, scalar, B, perm);
			Ten.Permute<T, TS, TS>(new(A, op, scalar), new(B), perm);
		}

		/// <inheritdoc/>
		public static DenseTensor<T, TS> Reduce(DenseTensor<T, TS> A, TensorOrder order, T scalar, UnaryOperation opA = UnaryOperation.Identity, BinaryOperation reduce = BinaryOperation.Addition)
		{
			Span<int> reduceInd = stackalloc int[A.Rank];
			Span<long> sizeB = stackalloc long[A.Rank];
			ITensorOperations<T, DenseTensor<T, TS>, DenseTensor<T, TS>>.CheckReduce(A, order, scalar, ref reduceInd, ref sizeB);
			var output = A.Storage.ResizeAlike(sizeB.Prod());
			try
			{
				Ten.Reduce<T, TS, TS>(reduce, new(A, opA, scalar), new(output, sizeB), reduceInd);
				return new(output, sizeB);
			}
			catch (Exception)
			{
				output.Dispose();
				throw;
			}
		}

		/// <inheritdoc/>
		public static DenseTensor<T, TS> Permute(DenseTensor<T, TS> A, TensorOrder order, T scalar, UnaryOperation opA = UnaryOperation.Identity)
		{
			Span<int> perm = stackalloc int[A.Rank];
			Span<long> sizeB = stackalloc long[A.Rank];
			ITensorOperations<T, DenseTensor<T, TS>, DenseTensor<T, TS>>.CheckPermute(A, order, scalar, perm, sizeB);
			var output = A.Storage.ResizeAlike(A.Length);
			try
			{
				Ten.Permute<T, TS, TS>(new(A, opA, scalar), new(output, sizeB), perm);
				return new(output, sizeB);
			}
			catch (Exception)
			{
				output.Dispose();
				throw;
			}
		}

		/// <inheritdoc/>
		public static void Contract(DenseTensor<T, TS> A!!, UnaryOperation opA, DenseTensor<T, TS> B!!, UnaryOperation opB, T α, DenseTensor<T, TS> C!!, UnaryOperation opC, T β)
		{
			if (α == T.Zero)
				throw new ArgumentOutOfRangeException(nameof(α), α, Resources.ParameterError.CannotZero);
			int rank = TensorContractInfo.GetContractRank(A, B);
			Span<int> leftConc = stackalloc int[rank];
			Span<int> rightConc = stackalloc int[rank];
			Span<int> leftFree = stackalloc int[A.Rank - rank];
			Span<int> rightFree = stackalloc int[B.Rank - rank];
			var info = TensorContractInfo.Create(A, B, C, leftConc, rightConc, leftFree, rightFree);
			Ten.Contract<T, TS, TS, TS>(new(A, opA, α), new(B, opB), new(C, opC), info);
		}

		/// <inheritdoc/>
		public static void TensorsBinaryOperation(DenseTensor<T, TS>? A, TensorOrder orderA, UnaryOperation opA, T α, DenseTensor<T, TS>? B, TensorOrder orderB, UnaryOperation opB, T β, DenseTensor<T, TS> C!!, BinaryOperation binary)
		{
			Span<int> permA = stackalloc int[A?.Rank ?? 0], permB = stackalloc int[B?.Rank ?? 0];
			ITensorOperations<T, DenseTensor<T, TS>, DenseTensor<T, TS>, DenseTensor<T, TS>>.CheckBinary(A, orderA, α, B, orderB, β, C, permA, permB);
			Ten.OperationBinary<T, TS, TS, TS>(binary, A is null ? default : new(A, opA, α), permA, B is null ? default : new(B, opB, β), permB, new(C));
		}

		/// <inheritdoc/>
		public static DenseTensor<T, TS> Contract(DenseTensor<T, TS> A, UnaryOperation opA, DenseTensor<T, TS> B, UnaryOperation opB, T α)
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
			var output = A.Storage.ResizeAlike(sizeC.Prod());
			try
			{
				Ten.Contract<T, TS, TS, TS>(new(A, opA, α), new(B, opB), new(output, sizeC), info);
				return new(output, sizeC, default, labelC);
			}
			catch (Exception)
			{
				output.Dispose();
				throw;
			}
		}

		/// <inheritdoc/>
		public static DenseTensor<T, TS> TensorsBinaryOperation(DenseTensor<T, TS>? A, TensorOrder orderA, UnaryOperation opA, T scalarA, DenseTensor<T, TS>? B, TensorOrder orderB, UnaryOperation opB, T scalarB, BinaryOperation binary)
		{
			Span<int> permA = stackalloc int[A?.Rank ?? 0], permB = stackalloc int[B?.Rank ?? 0];
			Span<long> sizeC = stackalloc long[A?.Rank ?? B?.Rank ?? 0];
			var output = ITensorOperations<T, DenseTensor<T, TS>, DenseTensor<T, TS>, DenseTensor<T, TS>>.CheckBinary(A, orderA, scalarA, B, orderB, scalarB, permA, permB, sizeC, A?.Storage ?? B?.Storage);
			try
			{
				Ten.OperationBinary<T, TS, TS, TS>(binary, A is null ? default : new(A, opA, scalarA), permA, B is null ? default : new(B, opB, scalarB), permB, new(output, sizeC));
				return new(output, sizeC);
			}
			catch (Exception)
			{
				output.Dispose();
				throw;
			}
		}
		#endregion
	}


	/// <summary>
	/// The static class for dense linear algebra and tensor algebra solving operations of same data type and storage type.
	/// </summary>
	/// <typeparam name="T">Any unmanaged floating point number as the data type</typeparam>
	/// <typeparam name="TS">The storage type used by the value storage</typeparam>
	public sealed class DenseSolvers<T, TS> :
		IMatrixLeastSolve<T, DenseMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>,
		IMatrixQRSolve<T, DenseMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>,
		IMatrixQRSolve<T, DenseMatrix<T, TS>, TriangularMatrix<T, TS>, DenseMatrix<T, TS>>,
		IMatrixEigenSolve<T, DenseMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>, DenseVector<T, TS>>,
		IMatrixEigenSolve<T, SymmetricMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>, DenseVector<T, TS>>,
		IMatrixSchurDecompose<T, DenseMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>, DenseVector<T, TS>>,
		IMatrixSVD<T, DenseMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>, DenseVector<T, TS>>
		where T : unmanaged, IFloatingPoint<T>
		where TS : class, IStorage<T, TS>
	{
		#region matrix QR and least square solve
		/// <inheritdoc/>
		public static void LeastSquareSolve(DenseMatrix<T, TS> coefficients, DenseMatrix<T, TS> rightHandSides, DenseMatrix<T, TS> outSolves)
		{
			IMatrixLeastSolve<T, DenseMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>.CheckLeast(coefficients, rightHandSides, outSolves);
			if (rightHandSides != outSolves)
				rightHandSides.Storage.Copy2DTo<T, TS, TS>(rightHandSides.LeadDim, outSolves.Storage, outSolves.LeadDim, outSolves.NRows, outSolves.NCols);
			using var coef = coefficients.ToCompact();
			Lapack.LeastSquareSolve<T, TS, TS>(coefficients.NRows, coefficients.NCols, outSolves.NCols, coef, coefficients.NRows, outSolves.Storage, outSolves.LeadDim);
		}

		/// <inheritdoc/>
		public static void QRDecomposition(DenseMatrix<T, TS> matrix, DenseMatrix<T, TS> outTriangular, DenseMatrix<T, TS>? outUnary, bool full = false)
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
		#endregion


		#region matrix eigen solve
		private static SolveVectorMode CheckStandardEigen(AbstractDenseMatrix<T, TS> matrix!!, DenseVector<T, TS> outValues!!, DenseVector<T, TS>? outValuesImag, DenseMatrix<T, TS>? outLeftVectors, DenseMatrix<T, TS>? outRightVectors)
		{
			if (matrix.NRows != matrix.NCols)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(matrix));
			if (matrix.NRows != outValues.Length)
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(outValues));
			if (outValues.Stride != 1)
				throw new NotSupportedException();
			if (matrix is DenseMatrix<T, TS> && !NumberType<T>.IsComplex)
			{
				if (outValuesImag is null)
					throw new ArgumentNullException(nameof(outValuesImag));
				if (outValuesImag.Length != outValues.Length)
					throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(outValuesImag));
				if (outValuesImag.Stride != 1)
					throw new NotSupportedException();
			}
			SolveVectorMode mode = SolveVectorMode.NoVector;
			if (outLeftVectors is not null)
			{
				if (matrix.NRows != outLeftVectors.NRows || matrix.NCols != outLeftVectors.NCols)
					throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(outLeftVectors));
				mode |= SolveVectorMode.Left;
			}
			if (outRightVectors is not null)
			{
				if (matrix.NRows != outRightVectors.NRows || matrix.NCols != outRightVectors.NCols)
					throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(outRightVectors));
				if (outLeftVectors is not null && outLeftVectors.Storage == outRightVectors.Storage)
					throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(outRightVectors));
				mode |= SolveVectorMode.Right;
			}
			return mode;
		}

		private static SolveVectorMode CheckGeneralEigen(AbstractDenseMatrix<T, TS> matrix!!, AbstractDenseMatrix<T, TS> otherMatrix!!, DenseVector<T, TS> outValues!!, DenseVector<T, TS>? outValuesImag, DenseVector<T, TS>? outValuesDenominator, DenseMatrix<T, TS>? outLeftVectors, DenseMatrix<T, TS>? outRightVectors)
		{
			if (matrix.NRows != matrix.NCols)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(matrix));
			if (otherMatrix.NRows != otherMatrix.NCols)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(otherMatrix));
			if (matrix.NRows != otherMatrix.NRows)
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(otherMatrix));
			if (matrix.NRows != outValues.Length)
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(outValues));
			if (outValues.Stride != 1)
				throw new NotSupportedException();
			if (matrix is DenseMatrix<T, TS> && !NumberType<T>.IsComplex)
			{
				if (outValuesImag is null)
					throw new ArgumentNullException(nameof(outValuesImag));
				if (outValuesImag.Length != outValues.Length)
					throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(outValuesImag));
				if (outValuesImag.Stride != 1)
					throw new NotSupportedException();
				if (outValuesDenominator is null)
					throw new ArgumentNullException(nameof(outValuesDenominator));
				if (outValuesDenominator.Length != outValues.Length)
					throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(outValuesDenominator));
				if (outValuesDenominator.Stride != 1)
					throw new NotSupportedException();
			}
			SolveVectorMode mode = SolveVectorMode.NoVector;
			if (outLeftVectors is not null)
			{
				if (matrix.NRows != outLeftVectors.NRows || matrix.NCols != outLeftVectors.NCols)
					throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(outLeftVectors));
				mode |= SolveVectorMode.Left;
			}
			if (outRightVectors is not null)
			{
				if (matrix.NRows != outRightVectors.NRows || matrix.NCols != outRightVectors.NCols)
					throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(outRightVectors));
				if (outLeftVectors is not null && outLeftVectors.Storage == outRightVectors.Storage)
					throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(outRightVectors));
				mode |= SolveVectorMode.Right;
			}
			return mode;
		}

		private static void CheckStandardSchur(DenseMatrix<T, TS> matrix!!, DenseVector<T, TS> outValues!!, DenseVector<T, TS>? outValuesImag, DenseMatrix<T, TS> outSchurForm!!, DenseMatrix<T, TS>? outVectors)
		{
			if (matrix.NRows != matrix.NCols)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(matrix));
			if (matrix.NRows != outValues.Length)
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(outValues));
			if (outSchurForm.NRows != matrix.NRows || outSchurForm.NCols != matrix.NCols)
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(outSchurForm));
			if (outVectors is not null && (outVectors.NRows != matrix.NRows || outVectors.NCols != matrix.NCols))
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(outVectors));
			if (outValues.Stride != 1)
				throw new NotSupportedException();
			if (!NumberType<T>.IsComplex)
			{
				if (outValuesImag is null)
					throw new ArgumentNullException(nameof(outValuesImag));
				if (outValuesImag.Length != outValues.Length)
					throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(outValuesImag));
				if (outValuesImag.Stride != 1)
					throw new NotSupportedException();
			}
			matrix.CopyTo(outSchurForm);
		}

		private static SolveVectorMode CheckGeneralSchur(DenseMatrix<T, TS> matrix!!, DenseMatrix<T, TS> otherMatrix!!, DenseVector<T, TS> outValues!!, DenseVector<T, TS>? outValuesImag, DenseVector<T, TS> outValuesDenominator!!, DenseMatrix<T, TS> outSchurForm!!, DenseMatrix<T, TS> outSchurFormOther!!, DenseMatrix<T, TS>? outLeftVectors, DenseMatrix<T, TS>? outRightVectors)
		{
			if (matrix.NRows != matrix.NCols)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(matrix));
			if (otherMatrix.NRows != matrix.NRows || otherMatrix.NCols != matrix.NCols)
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(otherMatrix));
			if (outSchurForm.NRows != matrix.NRows || outSchurForm.NCols != matrix.NCols)
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(outSchurForm));
			if (outSchurFormOther.NRows != matrix.NRows || outSchurFormOther.NCols != matrix.NCols)
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(outSchurFormOther));
			if (matrix.NRows != outValues.Length)
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(outValues));
			if (outValues.Stride != 1)
				throw new NotSupportedException();
			if (outValuesDenominator.Length != outValues.Length)
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(outValuesDenominator));
			if (outValuesDenominator.Stride != 1)
				throw new NotSupportedException();
			if (!NumberType<T>.IsComplex)
			{
				if (outValuesImag is null)
					throw new ArgumentNullException(nameof(outValuesImag));
				if (outValuesImag.Length != outValues.Length)
					throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(outValuesImag));
				if (outValuesImag.Stride != 1)
					throw new NotSupportedException();
			}
			SolveVectorMode mode = SolveVectorMode.NoVector;
			if (outLeftVectors is not null)
			{
				if (outLeftVectors.NRows != matrix.NRows || outLeftVectors.NCols != matrix.NCols)
					throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(outLeftVectors));
				mode |= SolveVectorMode.Left;
			}
			if (outRightVectors is not null)
			{
				if (outRightVectors.NRows != matrix.NRows || outRightVectors.NCols != matrix.NCols)
					throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(outRightVectors));
				if (outLeftVectors is not null && outLeftVectors.Storage == outRightVectors.Storage)
					throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(outRightVectors));
				mode |= SolveVectorMode.Right;
			}
			matrix.CopyTo(outSchurForm);
			otherMatrix.CopyTo(outSchurFormOther);
			return mode;
		}

		/// <inheritdoc/>
		public static void StandardEigenSolve(DenseMatrix<T, TS> matrix, DenseVector<T, TS> outValues, DenseVector<T, TS>? outValuesImag, DenseMatrix<T, TS>? outLeftVectors, DenseMatrix<T, TS>? outRightVectors)
		{
			var mode = CheckStandardEigen(matrix, outValues, outValuesImag, outLeftVectors, outRightVectors);
			Lapack.EigenStandardMatrixGeneral<T, TS, TS, TS, TS>(mode, matrix.NRows, matrix.Storage, matrix.LeadDim, outValues.Storage, outValuesImag?.Storage, outLeftVectors?.Storage, outLeftVectors?.LeadDim ?? 1, outRightVectors?.Storage, outRightVectors?.LeadDim ?? 1);
		}

		/// <inheritdoc/>
		public static void StandardEigenSolve(SymmetricMatrix<T, TS> matrix, DenseVector<T, TS> outValues, DenseVector<T, TS>? outValuesImag, DenseMatrix<T, TS>? outLeftVectors, DenseMatrix<T, TS>? outRightVectors)
		{
			var mode = CheckStandardEigen(matrix, outValues, outValuesImag, outLeftVectors, outRightVectors);
			Lapack.EigenStandardMatrixHermitian<T, TS, TS, TS>(mode, matrix.NRows, matrix.Storage, matrix.LeadDim, outValues.Storage, outLeftVectors?.Storage ?? outRightVectors?.Storage, outLeftVectors?.LeadDim ?? outRightVectors?.LeadDim ?? 1);
		}

		/// <inheritdoc/>
		public static void StandardSchurSolve(DenseMatrix<T, TS> matrix, DenseVector<T, TS> outValues, DenseVector<T, TS>? outValuesImag, DenseMatrix<T, TS> outSchurForm, DenseMatrix<T, TS>? outVectors)
		{
			CheckStandardSchur(matrix, outValues, outValuesImag, outSchurForm, outVectors);
			Lapack.StandardSchurDecomposition<T, TS, TS, TS>(outVectors is null ? SolveVectorMode.NoVector : SolveVectorMode.Vector, matrix.NRows, outSchurForm.Storage, outSchurForm.LeadDim, outVectors?.Storage, outVectors?.LeadDim ?? 1, outValues.Storage, outValuesImag?.Storage);
		}

		/// <inheritdoc/>
		public static void StandardSchurReorder<TInd, TSInd>(DenseMatrix<T, TS> schurForm!!, DenseMatrix<T, TS>? schurVectors, TSInd order!!) where TInd : unmanaged, IBinaryInteger<TInd> where TSInd : class, IStorage<TInd, TSInd>
		{
			if (schurForm.NRows != schurForm.NCols)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(schurForm));
			if (schurVectors is not null && (schurVectors.NRows != schurForm.NRows || schurVectors.NCols != schurForm.NCols))
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(schurVectors));
			if (schurForm.NRows != order.Length)
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(order));
			Lapack.StandardSchurReorder<T, TInd, TS, TS, TSInd>(schurForm.NRows, schurForm.Storage, schurForm.LeadDim, schurVectors?.Storage, schurVectors?.LeadDim ?? 1, order);
		}

		/// <inheritdoc/>
		public static void SingularValueSolve(DenseMatrix<T, TS> matrix!!, DenseVector<T, TS> outValues!!, DenseMatrix<T, TS>? outLeftVectors, DenseMatrix<T, TS>? outRightVectors)
		{
			if (Math.Min(matrix.NRows, matrix.NCols) != outValues.Length)
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(outValues));
			if (outValues.Stride != 1)
				throw new NotSupportedException();
			SVDStore left = SVDStore.None, right = SVDStore.None;
			if (matrix.NRows >= matrix.NCols)
			{
				if (outLeftVectors is not null)
				{
					if (matrix.NRows == outLeftVectors.NRows && matrix.NCols == outLeftVectors.NCols)
						left = matrix.Storage == outLeftVectors.Storage ? SVDStore.Overwrite : SVDStore.Economic;
					else if (matrix.NRows == outLeftVectors.NRows && matrix.NRows == outLeftVectors.NCols)
						left = SVDStore.All;
					else
						throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(outLeftVectors));
				}
				if (outRightVectors is not null)
				{
					if (matrix.NCols == outRightVectors.NRows && matrix.NCols == outRightVectors.NCols)
						left = matrix.Storage == outRightVectors.Storage ? SVDStore.Overwrite : SVDStore.All;
					else
						throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(outRightVectors));
				}
			}
			else
			{
				if (outLeftVectors is not null)
				{
					if (matrix.NRows == outLeftVectors.NRows && matrix.NRows == outLeftVectors.NCols)
						left = matrix.Storage == outLeftVectors.Storage ? SVDStore.Overwrite : SVDStore.All;
					else
						throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(outLeftVectors));
				}
				if (outRightVectors is not null)
				{
					if (matrix.NRows == outRightVectors.NRows && matrix.NCols == outRightVectors.NCols)
						left = matrix.Storage == outRightVectors.Storage ? SVDStore.Overwrite : SVDStore.Economic;
					else if (matrix.NRows == outRightVectors.NRows && matrix.NRows == outRightVectors.NCols)
						left = SVDStore.All;
					else
						throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(outRightVectors));
				}
			}
			Lapack.SingularValues<T, TS, TS, TS, TS>(left, right, matrix.NRows, matrix.NCols, matrix.Storage, matrix.LeadDim, outLeftVectors?.Storage, outLeftVectors?.LeadDim ?? 1, outRightVectors?.Storage, outRightVectors?.LeadDim ?? 1, outValues.Storage);
		}

		/// <inheritdoc/>
		public static void GeneralEigenSolve(GeneralEigenType type, DenseMatrix<T, TS> matrix!!, DenseMatrix<T, TS> otherMatrix!!, DenseVector<T, TS> outValues!!, DenseVector<T, TS>? outValuesImag, DenseVector<T, TS>? outValuesDenominator, DenseMatrix<T, TS>? outLeftVectors, DenseMatrix<T, TS>? outRightVectors)
		{
			if (type == GeneralEigenType.None)
				throw new ArgumentOutOfRangeException(nameof(type));
			var mode = CheckGeneralEigen(matrix, otherMatrix, outValues, outValuesImag, outValuesDenominator, outLeftVectors, outRightVectors);
			Lapack.EigenGeneralMatrixGeneral<T, TS, TS, TS, TS>(type, mode, matrix.NRows, matrix.Storage, matrix.LeadDim, otherMatrix.Storage, otherMatrix.LeadDim, outValues.Storage, outValuesImag?.Storage, outValuesDenominator?.Storage ?? TS.Empty, outLeftVectors?.Storage, outLeftVectors?.LeadDim ?? 1, outRightVectors?.Storage, outRightVectors?.LeadDim ?? 1);
		}

		/// <inheritdoc/>
		public static void GeneralEigenSolve(GeneralEigenType type, SymmetricMatrix<T, TS> matrix, SymmetricMatrix<T, TS> otherMatrix, DenseVector<T, TS> outValues, DenseVector<T, TS>? outValuesImag, DenseVector<T, TS>? outValuesDenominator, DenseMatrix<T, TS>? outLeftVectors, DenseMatrix<T, TS>? outRightVectors)
		{
			if (type == GeneralEigenType.None)
				throw new ArgumentOutOfRangeException(nameof(type));
			var mode = CheckGeneralEigen(matrix, otherMatrix, outValues, outValuesImag, outValuesDenominator, outLeftVectors, outRightVectors);
			Lapack.EigenGeneralMatrixHermitian<T, TS, TS, TS>(type, mode, matrix.NRows, matrix.Storage, matrix.LeadDim, otherMatrix.Storage, otherMatrix.LeadDim, outValues.Storage, outLeftVectors?.Storage ?? outRightVectors?.Storage, outLeftVectors?.LeadDim ?? outRightVectors?.LeadDim ?? 1);
		}

		/// <inheritdoc/>
		public static void GeneralSchurSolve(DenseMatrix<T, TS> matrix, DenseMatrix<T, TS> otherMatrix, DenseVector<T, TS> outValues, DenseVector<T, TS>? outValuesImag, DenseVector<T, TS> outValuesDenominator, DenseMatrix<T, TS> outSchurForm, DenseMatrix<T, TS> outSchurFormOther, DenseMatrix<T, TS>? outLeftVectors, DenseMatrix<T, TS>? outRightVectors)
		{
			var mode = CheckGeneralSchur(matrix, otherMatrix, outValues, outValuesImag, outValuesDenominator, outSchurForm, outSchurFormOther, outLeftVectors, outRightVectors);
			Lapack.GeneralSchurDecomposition<T, TS, TS, TS, TS>(mode, matrix.NRows, outSchurForm.Storage, outSchurForm.LeadDim, outSchurFormOther.Storage, outSchurFormOther.LeadDim, outLeftVectors?.Storage, outLeftVectors?.LeadDim ?? 1, outRightVectors?.Storage, outRightVectors?.LeadDim ?? 1, outValues.Storage, outValuesImag?.Storage, outValuesDenominator.Storage);
		}

		/// <inheritdoc/>
		public static void GeneralSchurReorder<TInd, TSInd>(DenseMatrix<T, TS> schurForm!!, DenseMatrix<T, TS> schurFormOther!!, DenseMatrix<T, TS>? schurLeftVectors, DenseMatrix<T, TS>? schurRightVectors, TSInd order!!) where TInd : unmanaged, IBinaryInteger<TInd> where TSInd : class, IStorage<TInd, TSInd>
		{
			if (schurForm.NRows != schurForm.NCols)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(schurForm));
			if (schurFormOther.NRows != schurFormOther.NCols)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(schurFormOther));
			if (schurFormOther.NRows != schurForm.NRows || schurFormOther.NCols != schurForm.NCols)
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(schurFormOther));
			if (schurLeftVectors is not null && (schurLeftVectors.NRows != schurForm.NRows || schurLeftVectors.NCols != schurForm.NCols))
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(schurLeftVectors));
			if (schurRightVectors is not null && (schurRightVectors.NRows != schurForm.NRows || schurRightVectors.NCols != schurForm.NCols))
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(schurRightVectors));
			if (schurForm.NRows != order.Length)
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(order));
			Lapack.GeneralSchurReorder<T, TInd, TS, TS, TS, TSInd>(schurForm.NRows, schurForm.Storage, schurForm.LeadDim, schurFormOther.Storage, schurFormOther.LeadDim, schurLeftVectors?.Storage, schurLeftVectors?.LeadDim ?? 1, schurRightVectors?.Storage, schurRightVectors?.LeadDim ?? 1, order);
		}
		#endregion
	}
}
