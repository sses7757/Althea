using System.Runtime.CompilerServices;

using Althea.Numerics;


namespace Althea.LinearAlgebra
{
	#region interface
	/// <summary>
	/// The interface for basic vector metrics
	/// </summary>
	public interface IVectorMetric
	{
		/// <summary>
		/// When implemented by a derived class, get the presenting length of this vector.
		/// </summary>
		long Length { get; }
	}

	/// <summary>
	/// The interface for basic matrix metrics
	/// </summary>
	public interface IMatrixMetric
	{
		/// <summary>
		/// When implemented by a derived class, get the presenting number of rows of this matrix.
		/// </summary>
		long NRows { get; }

		/// <summary>
		/// When implemented by a derived class, get the presenting number of columns of this matrix.
		/// </summary>
		long NCols { get; }
	}
	#endregion


	#region exception
	/// <summary>
	/// The exception to be thrown when a matrix solving method of a LAPACK or LAPACK-like library failed due to several reasons
	/// </summary>
	[Serializable]
	public class MatrixSolveAlgorithmException : Exception
	{
		private static string GetDescription(SolveMethodKind kind, long info)
		{
			if (info <= 0)
				return string.Empty; // not this error
			string? message = kind switch
			{
				SolveMethodKind.Cholesky => Resources.OtherError.MatrixSolveCholesky,
				SolveMethodKind.LU => Resources.OtherError.MatrixSolveLU,
				SolveMethodKind.BunchKaufman => Resources.OtherError.MatrixSolveBunchKaufman,
				SolveMethodKind.SVD => Resources.OtherError.MatrixSolveSVD,
				SolveMethodKind.Eigenvalue => Resources.OtherError.MatrixSolveEigen,
				_ => null,
			};
			if (message is not null)
				return string.Format(message, info);
			return kind switch
			{
				SolveMethodKind.QR => Resources.OtherError.MatrixSolveQR,
				SolveMethodKind.Schur => Resources.OtherError.MatrixSolveSchur,
				SolveMethodKind.SchurReorder => Resources.OtherError.MatrixSolveSchurReorder,
				SolveMethodKind.GeneralEigen => Resources.OtherError.MatrixSolveGeneralEigen,
				SolveMethodKind.NonSymmetricEigenvalue => Resources.OtherError.MatrixSolveNonSymmEigen,
				SolveMethodKind.NonSymmetricGeneralEigenvalue => Resources.OtherError.MatrixSolveNonSymmGeneralEigen,
				////SolveMethodKind.GeneralSchur => throw new NotImplementedException(),
				////SolveMethodKind.GeneralSVD => throw new NotImplementedException(),
				_ => string.Format(Resources.OtherError.MatrixSolveOther, info),
			};
		}

		/// <summary>
		/// Create a new <see cref="MatrixSolveAlgorithmException"/> by given <see cref="SolveMethodKind"/> and <see cref="long"/> info.
		/// </summary>
		/// <param name="kind">Which kind of LAPACK-like solver is used (in <see cref="SolveMethodKind"/>)</param>
		/// <param name="info">The returned LAPACK-like solver information</param>
		public MatrixSolveAlgorithmException(SolveMethodKind kind, long info) : base(GetDescription(kind, info)) { }

		/// <summary>
		/// Empty <see cref="MatrixSolveAlgorithmException"/>
		/// </summary>
		public MatrixSolveAlgorithmException() { }

		/// <summary>
		/// <see cref="MatrixSolveAlgorithmException"/> with custom message
		/// </summary>
		public MatrixSolveAlgorithmException(string message) : base(message) { }

		/// <summary>
		/// <see cref="MatrixSolveAlgorithmException"/> with custom message and inner exception
		/// </summary>
		public MatrixSolveAlgorithmException(string message, Exception innerException) : base(message, innerException) { }
	}
	#endregion


	#region enum
	/// <summary>
	/// The <see cref="SolveMethodKind"/> enum indicates the classification of a matrix-solving method of a LAPACK or LAPACK-like library
	/// </summary>
	public enum SolveMethodKind
	{
		// Ignore Spelling: potr getrf geqrf sytrf gesvd sygv syev syevj sygvj gesvdj geev ggev gges ggsvd trsen
		/// <summary>
		/// Cholesky factorization, [S|D|C|Z]potr[f|i]
		/// </summary>
		Cholesky,
		/// <summary>
		/// LU factorization, [S|D|C|Z]getrf
		/// </summary>
		LU,
		/// <summary>
		/// QR factorization, [S|D|C|Z]geqrf
		/// </summary>
		QR,
		/// <summary>
		/// Schur decomposition, [S|D|C|Z]gees
		/// </summary>
		Schur,
		/// <summary>
		/// Reorder Schur decomposition, [S|D|C|Z]trsen
		/// </summary>
		SchurReorder,
		/// <summary>
		/// General Schur decomposition, [S|D|C|Z]gges
		/// </summary>
		GeneralSchur,
		/// <summary>
		/// Bunch Kaufman factorization, [S|D|C|Z]sytrf
		/// </summary>
		BunchKaufman,
		/// <summary>
		/// Singular value decomposition, [S|D|C|Z]gesvd
		/// </summary>
		SVD,
		/// <summary>
		/// General singular value decomposition, [S|D|C|Z]ggsvd
		/// </summary>
		GeneralSVD,
		/// <summary>
		/// Eigenvalue decomposition (for symmetric matrices), [S|D|C|Z]syev[x]
		/// </summary>
		Eigenvalue,
		/// <summary>
		/// Non-symmetric eigenvalue decomposition, [S|D|C|Z]geev[x]
		/// </summary>
		NonSymmetricEigenvalue,
		/// <summary>
		/// General eigenvalue decomposition (for symmetric matrices), [S|D|C|Z]sygv[x]
		/// </summary>
		GeneralEigen,
		/// <summary>
		/// Non-symmetric eigenvalue decomposition, [S|D|C|Z]ggev[x]
		/// </summary>
		NonSymmetricGeneralEigenvalue,
	}

	/// <summary>
	/// The <see cref="GeneralEigenType"/> enum indicates which kind of general eigenvalue problem is provided
	/// </summary>
	public enum GeneralEigenType
	{
		/// <summary>
		/// No general eigen
		/// </summary>
		None = 0,
		//tex: $A x = \lambda B x$

		/// <summary>
		/// A*x = λ*B*x
		/// </summary>
		Type1 = 1,

		//tex: $A B x = \lambda x$

		/// <summary>
		/// A*B*x = λ*x
		/// </summary>
		Type2 = 2,

		//tex: $B A x = \lambda x$

		/// <summary>
		/// B*A*x = λ*x
		/// </summary>
		Type3 = 3
	}

	/// <summary>
	/// The <see cref="MatrixOperation"/> enum indicates which simple operation shall be performed to the matrix before some complicated operation being executed to reduce time or space complexity.
	/// </summary>
	/// <remarks>There are two independent (orthogonal) cyclic unary operations:<br/>
	/// add <see cref="Transpose"/>: <c>unchecked(<see cref="None"/> + <see cref="Transpose"/> + <see cref="Transpose"/>) == <see cref="None"/></c><br/>
	/// bit-wise not: <c>~<see cref="None"/> == <see cref="Conjugate"/>, ~(~<see cref="Conjugate"/>) == <see cref="None"/></c><br/>
	/// As a result, the <see cref="MatrixOperation"/> enum and the unchecked 32-bit integer addition forms an (algebraic) group.</remarks>
	public enum MatrixOperation : int
	{
		/// <summary>
		/// No operation
		/// </summary>
		None = 0,
		/// <summary>
		/// The transpose only operation
		/// </summary>
		/// <remarks><c>unchecked(<see cref="Transpose"/> + <see cref="Transpose"/>) == <see cref="None"/></c></remarks>
		Transpose = int.MinValue,
		/// <summary>
		/// The conjugate only operation
		/// </summary>
		/// <remarks><c>~<see cref="Conjugate"/> == <see cref="None"/></c></remarks>
		Conjugate = -1,
		/// <summary>
		/// The conjugate transpose operation
		/// </summary>
		/// <remarks><c><see cref="ConjugateTranspose"/> == unchecked(<see cref="Conjugate"/> + <see cref="Transpose"/>) == ~<see cref="Transpose"/></c></remarks>
		ConjugateTranspose = int.MaxValue,
	}

	/// <summary>
	/// Static class of extension methods of <see cref="MatrixOperation"/>
	/// </summary>
	public static class MatrixOperationExtension
	{
		/// <summary>
		/// Check whether the given <paramref name="operation"/> can be performed in-place, i.e., no transposition involved.
		/// </summary>
		/// <param name="operation">The <see cref="MatrixOperation"/> to check</param>
		/// <returns>Whether <paramref name="operation"/> can be performed in-place.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool CanInPlace(this MatrixOperation operation) => operation == MatrixOperation.None || operation == MatrixOperation.Conjugate;

		/// <summary>
		/// Check whether the given <paramref name="operation"/> has conjugate operation.
		/// </summary>
		/// <param name="operation">The <see cref="MatrixOperation"/> to check</param>
		/// <returns>Whether <paramref name="operation"/> has conjugate operation or not.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool HasConjugate(this MatrixOperation operation) => operation == MatrixOperation.ConjugateTranspose || operation == MatrixOperation.Conjugate;

		/// <summary>
		/// Transpose the given <paramref name="operation"/>
		/// </summary>
		/// <param name="operation">The <see cref="MatrixOperation"/> to be transposed</param>
		/// <returns>The result <see cref="MatrixOperation"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static MatrixOperation Transpose(this MatrixOperation operation) => unchecked(operation + (int)MatrixOperation.Transpose);

		/// <summary>
		/// Conjugate the given <paramref name="operation"/>
		/// </summary>
		/// <param name="operation">The <see cref="MatrixOperation"/> to be conjugated</param>
		/// <returns>The result <see cref="MatrixOperation"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static MatrixOperation Conjugate(this MatrixOperation operation) => ~operation;

		/// <summary>
		/// Simplify the given <paramref name="input"/> <see cref="MatrixOperation"/> with type <typeparamref name="T"/> and <paramref name="hermitian"/>
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="input">The input <see cref="MatrixOperation"/> to be simplified</param>
		/// <param name="hermitian">Whether the target matrix is neither symmetric nor hermitian (null) or simply symmetric (false) or hermitian (true)</param>
		/// <returns>The simplified <paramref name="input"/> as a <see cref="MatrixOperation"/>. If <paramref name="hermitian"/> is not null, only <see cref="MatrixOperation.None"/> and <see cref="MatrixOperation.Conjugate"/> are possible outputs.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static MatrixOperation Simplify<T>(this MatrixOperation input, bool? hermitian = null) where T : unmanaged, INumber<T>
		{
			bool isComplex = NumberType<T>.IsComplex;
			bool symm = hermitian.HasValue && !hermitian.Value, herm = isComplex && hermitian.HasValue && hermitian.Value;
			switch (input)
			{
				case MatrixOperation.Transpose:
					if (symm)
						return MatrixOperation.None;
					else if (herm)
						return MatrixOperation.Conjugate;
					else
						return MatrixOperation.Transpose;
				case MatrixOperation.ConjugateTranspose:
					if (herm)
						return MatrixOperation.None;
					else if (symm)
						return MatrixOperation.Conjugate;
					else if (!isComplex)
						return MatrixOperation.Transpose;
					else
						return MatrixOperation.ConjugateTranspose;
				case MatrixOperation.Conjugate:
					if (!isComplex)
						return MatrixOperation.None;
					else if (herm)
						return MatrixOperation.Conjugate;
					else
						return MatrixOperation.Conjugate;
				default:
					return MatrixOperation.None;
			}
		}
	}
	#endregion


	#region operation enum
	/// <summary>
	/// Binary operations used by array point-wise binary operations.
	/// </summary>
	/// <remarks>All implementations shall support these pre-defined binary operations, but a implementation can add support for more binary operations.</remarks>
	public enum BinaryOperation
	{
		/// <summary>
		/// Operation that returns the addition of two input parameters
		/// </summary>
		Add,
		/// <summary>
		/// Operation that returns the multiplication of two input parameters
		/// </summary>
		Multiply,
		/// <summary>
		/// Operation that returns the division of two input parameters
		/// </summary>
		Divide,
		/// <summary>
		/// Operation that returns the power of the first input parameter to the second one
		/// </summary>
		Power,
		/// <summary>
		/// Operation that returns the maximum of two input parameters
		/// </summary>
		Maximum,
		/// <summary>
		/// Operation that returns the minimum of two input parameters
		/// </summary>
		Mininum,
		/// <summary>
		/// Operation that returns the maximum of the absolute values two input parameters
		/// </summary>
		AbsoluteMaximum,
		/// <summary>
		/// Operation that returns the minimum of the absolute values two input parameters
		/// </summary>
		AbsoluteMininum,
	}

	/// <summary>
	/// Binary operations used by array point-wise binary operations with a scalar.
	/// </summary>
	/// <remarks>All implementations shall support these pre-defined binary operations, but a implementation can add support for more binary operations.</remarks>
	public enum BinaryScalarOperation
	{
		/// <summary>
		/// Operation that returns the addition of two input parameters
		/// </summary>
		Add,
		/// <summary>
		/// Operation that returns the multiplication of two input parameters
		/// </summary>
		Multiply,
		/// <summary>
		/// Operation that returns the power of the first input parameter to the second one
		/// </summary>
		Power,
		/// <summary>
		/// Operation that simply returns the second input parameter
		/// </summary>
		Fill,
		/// <summary>
		/// Operation that returns 0 if the first input parameter's absolute value is smaller than the second one; otherwise, returns the first input parameter itself 
		/// </summary>
		Truncate,
		/// <summary>
		/// Operation that returns the maximum of two input parameters
		/// </summary>
		Maximum,
		/// <summary>
		/// Operation that returns the minimum of two input parameters
		/// </summary>
		Mininum,
		/// <summary>
		/// Operation that returns the maximum of the absolute values two input parameters
		/// </summary>
		AbsoluteMaximum,
		/// <summary>
		/// Operation that returns the minimum of the absolute values two input parameters
		/// </summary>
		AbsoluteMininum,
	}

	/// <summary>
	/// Binary reduce operations used by array point-wise reduce operations whose first input is the element in array and the second one is the partial reduction result.
	/// </summary>
	/// <remarks>All implementations shall support these pre-defined binary operations, but a implementation can add support for more binary operations.</remarks>
	public enum ReduceOperation
	{
		/// <summary>
		/// Operation that returns the addition of two input parameters
		/// </summary>
		Add,
		/// <summary>
		/// Operation that returns the addition of the absolute value of the first input parameter and the second parameter
		/// </summary>
		AddAbsolute,
		/// <summary>
		/// Operation that returns the multiplication of two input parameters
		/// </summary>
		Multiply,
		/// <summary>
		/// Operation that returns the multiplication of the absolute value of the first input parameter and the second parameter
		/// </summary>
		MultiplyAbsolute,
		/// <summary>
		/// Operation that returns the addition of the square of the first input parameter and the second parameter; and sqrt the result before exit
		/// </summary>
		Norm,
		/// <summary>
		/// Operation that returns the maximum of two input parameters
		/// </summary>
		Maximum,
		/// <summary>
		/// Operation that returns the minimum of two input parameters
		/// </summary>
		Mininum,
		/// <summary>
		/// Operation that returns the maximum of the absolute values two input parameters
		/// </summary>
		AbsoluteMaximum,
		/// <summary>
		/// Operation that returns the minimum of the absolute values two input parameters
		/// </summary>
		AbsoluteMininum,
	}

	/// <summary>
	/// Unitary operations of array point-wise unary operations.
	/// </summary>
	/// <remarks>All implementations shall support these pre-defined unary operations, but a implementation can add support for more unary operations.</remarks>
	public enum UnaryOperation
	{
		/// <summary>
		/// Identity operator (i.e., elements are not changed)
		/// </summary>
		Identity,
		/// <summary>
		/// Complex conjugate operator (real-typed elements are not changed)
		/// </summary>
		Conjugate,
		/// <summary>
		/// Negation operator
		/// </summary>
		Negate,
		/// <summary>
		/// Absolute operator
		/// </summary>
		AbsoluteValue
	}
	#endregion


	#region wrapper
	/// <summary>
	/// The wrapper struct used to store the matrix slicing parameters
	/// </summary>
	public readonly ref struct MatrixSliceWrapper
	{
		/// <summary>
		/// Get the starting offset of the row to take
		/// </summary>
		public long OffsetRow { get; }
		/// <summary>
		/// Get the starting offset of the column to take
		/// </summary>
		public long OffsetCol { get; }
		/// <summary>
		/// Get the number of the rows to take
		/// </summary>
		public long CountRow { get; }
		/// <summary>
		/// Get the number of the rows to take
		/// </summary>
		public long CountCol { get; }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private MatrixSliceWrapper(long offsetRow, long countRow, long offsetCol, long countCol)
		{
			this.OffsetRow = offsetRow; this.CountRow = countRow; this.OffsetCol = offsetCol; this.CountCol = countCol;
		}

		/// <summary>
		/// Create a <see cref="MatrixSliceWrapper"/> after checking parameters with the given <paramref name="matrix"/> and <paramref name="sub"/> matrix
		/// </summary>
		/// <param name="offsetRow">The starting offset of the row to take</param>
		/// <param name="countRow">The number of the rows to take</param>
		/// <param name="offsetCol">The starting offset of the columns to take</param>
		/// <param name="countCol">The number of the columns to take</param>
		/// <param name="matrix">The matrix to take slice from</param>
		/// <param name="sub">The sub-matrix to be overwritten by the sliced <paramref name="matrix"/> or to overwrite the <paramref name="matrix"/>'s slice</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static MatrixSliceWrapper Create(long offsetRow, long countRow, long offsetCol, long countCol, IMatrixMetric matrix!!, IMatrixMetric? sub = null)
		{
			if (offsetRow < 0)
				throw new ArgumentOutOfRangeException(nameof(offsetRow), offsetRow, Resources.ParameterError.CannotNegative);
			if (offsetRow >= matrix.NRows)
				throw new ArgumentOutOfRangeException(nameof(offsetRow), offsetRow, Resources.ParameterError.InvalidValue);
			if (countRow <= 0)
				throw new ArgumentOutOfRangeException(nameof(countRow), countRow, Resources.ParameterError.CannotNegative);
			if (countRow + offsetRow > matrix.NRows)
				throw new ArgumentOutOfRangeException(nameof(countRow), countRow, Resources.ParameterError.InvalidValue);
			if (offsetCol < 0)
				throw new ArgumentOutOfRangeException(nameof(offsetCol), offsetCol, Resources.ParameterError.CannotNegative);
			if (offsetCol >= matrix.NCols)
				throw new ArgumentOutOfRangeException(nameof(offsetCol), offsetCol, Resources.ParameterError.InvalidValue);
			if (countCol <= 0)
				throw new ArgumentOutOfRangeException(nameof(countCol), countCol, Resources.ParameterError.CannotNegative);
			if (countCol + offsetCol > matrix.NCols)
				throw new ArgumentOutOfRangeException(nameof(countCol), countCol, Resources.ParameterError.InvalidValue);
			// check sub
			if (sub is not null)
			{
				if (countRow != sub.NRows)
					throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(sub));
				if (countCol != sub.NCols)
					throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(sub));
			}
			// return
			return new(offsetRow, countRow, offsetCol, countCol);
		}
	}
	#endregion
}
