using System;


namespace Althea.LinearAlgebra
{
	#region exception
	/// <summary>
	/// The exception class when a matrix solve method failed due to several reasons
	/// </summary>
	public sealed class MatrixSolveAlgorithmException : Exception
	{
		private static string GetDescription(SolveMethodKind kind)
		{
			return kind switch
			{
				SolveMethodKind.Cholesky => "Cholesky factorization failed since leading minor of order {0} is not positive definite.",
				SolveMethodKind.LU => "LU factorization failed since matrix A (U) is singular, U({0},{0}) = 0.",
				SolveMethodKind.QR => "QR factorization failed for unknown reasons.",
				SolveMethodKind.BunchKaufman => "Bunch-Kaufman factorization failed since matrix A is singular, D({0},{0}) = 0.",
				SolveMethodKind.SVD => "SVD failed to converge, {0} super-diagonal elements of an upper bidiagonal matrix not converged.",
				SolveMethodKind.Eigenvalue => "Eigenvalue decomposition failed since {0} off-diagonal elements of an intermediate tridiagonal form did not converge to zero",
				SolveMethodKind.Schur => "Schur decomposition failed to compute all the eigenvalues, or the eigenvalues could not be reordered because some eigenvalues were too close to separate, or after reordering, round-off changed values of some complex eigenvalues so that leading eigenvalues in the Schur form no longer satisfy the input.",
				// Ignore Spelling: potrf
				SolveMethodKind.GeneralEigen => "General eigenvalue decomposition failed since either `potrf` or `syevd` is wrong",
				SolveMethodKind.Jacobi => "Jacobi method does not converge under given tolerance and maximum sweeps.",
				_ => "",
			};
		}

		/// <summary>
		/// Constructor of <see cref="MatrixSolveAlgorithmException"/>
		/// </summary>
		/// <param name="kind">which kind of CUDA solver is used (in <see cref="SolveMethodKind"/>)</param>
		/// <param name="i">returned device info</param>
		public MatrixSolveAlgorithmException(SolveMethodKind kind, int i) : base(string.Format(GetDescription(kind), i)) { }

		/// <summary>
		/// Empty <see cref="MatrixSolveAlgorithmException"/>
		/// </summary>
		public MatrixSolveAlgorithmException()
		{ }

		/// <summary>
		/// <see cref="MatrixSolveAlgorithmException"/> with custom message
		/// </summary>
		/// <param name="message"></param>
		public MatrixSolveAlgorithmException(string message) : base(message)
		{ }

		/// <summary>
		/// <see cref="MatrixSolveAlgorithmException"/> with custom message and inner exception
		/// </summary>
		/// <param name="message"></param>
		/// <param name="innerException"></param>
		public MatrixSolveAlgorithmException(string message, Exception innerException) : base(message, innerException)
		{ }
	}
	#endregion

	#region enum
	/// <summary>
	/// The <see cref="SolveMethodKind"/> enum indicates the classification of a matrix-solving method
	/// </summary>
	public enum SolveMethodKind
	{
		// Ignore Spelling: potr getrf geqrf sytrf gesvd sygvd syevd syevj sygvj gesvdj
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
		/// Bunch Kaufman factorization, [S|D|C|Z]sytrf
		/// </summary>
		BunchKaufman,
		/// <summary>
		/// Singular value decomposition, [S|D|C|Z]gesvd
		/// </summary>
		SVD,
		/// <summary>
		/// Eigenvalue decomposition, [S|D|C|Z]syevd[x]
		/// </summary>
		Eigenvalue,
		/// <summary>
		/// General eigenvalue decomposition, [S|D|C|Z]sygvd[x]
		/// </summary>
		GeneralEigen,
		/// <summary>
		/// Singular value decomposition via Jacobi method, [S|D|C|Z]syevj, [S|D|C|Z]sygvj, [S|D|C|Z]gesvdj
		/// </summary>
		Jacobi
	}

	/// <summary>
	/// The <see cref="GeneralEigenType"/> enum indicates which kind of general eigenvalue problem is provided
	/// </summary>
	public enum GeneralEigenType
	{
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
	/// The <see cref="EigenSolveMode"/> enum indicates which eigenvector matrices obtained from standard or general eigenvalue solver shall be stored
	/// </summary>
	public enum EigenSolveMode
	{
		/// <summary>
		/// Do not compute the eigenvectors
		/// </summary>
		NoVector = 0,
		/// <summary>
		/// Compute the eigenvectors (both left and right)
		/// </summary>
		Vector = 1,
		/// <summary>
		/// Compute only the left eigenvectors
		/// </summary>
		LeftOnly = 2,
		/// <summary>
		/// Compute only the right eigenvectors
		/// </summary>
		RightOnly = 3
	}

	/// <summary>
	/// The <see cref="SVDStore"/> enum indicates which of the vectors of a singular value decomposition shall be stored and where it shall be stored
	/// </summary>
	public enum SVDStore
	{
		/// <summary>
		/// All the columns / rows are stored even if some of them are not necessary
		/// </summary>
		All = 0,
		/// <summary>
		/// Economical storage -- only the columns / rows which are necessary are stored
		/// </summary>
		Economic = 1,
		/// <summary>
		/// Economical overwrite storage -- only the columns / rows which are necessary are stored into the original matrix
		/// </summary>
		Overwrite = 2,
		/// <summary>
		/// None of the columns / rows are stored
		/// </summary>
		None = 3
	}

	/// <summary>
	/// The <see cref="DiagType"/> enum indicates whether the main diagonal of the dense matrix is unity and consequently should not be touched or modified by the function.
	/// </summary>
	public enum DiagType
	{
		/// <summary>
		/// the matrix diagonal has non-unit elements
		/// </summary>
		NonUnit = 0,
		/// <summary>
		/// the matrix diagonal has unit elements
		/// </summary>
		Unit = 1
	}

	/// <summary>
	/// The <see cref="MatrixOperation"/> enum indicates which simple operation shall be performed to the matrix before the required complicated operation being executed. This may help reduce time or space complexity.
	/// </summary>
	public enum MatrixOperation
	{
		/// <summary>
		/// the non-transpose operation
		/// </summary>
		None = 0,
		/// <summary>
		/// the transpose operation
		/// </summary>
		Transpose = 1,
		/// <summary>
		/// the conjugate transpose operation
		/// </summary>
		ConjugateTranspose = 2,
		/// <summary>
		/// the conjugate only operation
		/// </summary>
		Conjugate = 3,
	}

	// TODO: ????
	/// <summary>
	/// Used in overloading operators
	/// </summary>
	public enum PowerOperation
	{
		/// <summary>
		/// Nothing
		/// </summary>
		None = 0,
		/// <summary>
		/// Transpose only
		/// </summary>
		Transpose = ~0, // -1
		/// <summary>
		/// Conjugate only
		/// </summary>
		Conjugate = int.MaxValue,
		/// <summary>
		/// conjugate transpose
		/// </summary>
		Dagger = ~int.MaxValue // int.MinValue
	}
	#endregion


	#region converters
	// TODO: move to Backend.Cuda.Blas
	internal static class TypeConverters
	{
		internal static sbyte ToChar(this SVDStore store)
		{
			return store switch
			{
				SVDStore.All => (sbyte)'A',
				SVDStore.Economic => (sbyte)'S',
				SVDStore.Overwrite => (sbyte)'O',
				SVDStore.None => (sbyte)'N',
				_ => throw new NotSupportedException(),
			};
		}
	}
	#endregion
}
