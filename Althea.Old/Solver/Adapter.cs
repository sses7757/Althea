using System;

using Althea.Storage;

using RT = Althea.Runtime.API;
using static Althea.Blas.Mkl.MklBlasExtension;


namespace Althea.Solver
{
	#region exception
	/// <summary>
	/// The exception class for matrix Solver algorithms
	/// </summary>
	public sealed class MatrixAlgorithmException : Exception
	{
		private static string GetDescription(MethodKind kind)
		{
			return kind switch
			{
				MethodKind.Cholesky => "Cholesky factorization failed since leading minor of order {0} is not positive definite.",
				MethodKind.LU => "LU factorization failed since matrix A (U) is singular, U({0},{0}) = 0.",
				MethodKind.QR => "QR factorization failed for unknown reasons.",
				MethodKind.Bunch_Kaufman => "Bunch-Kaufman factorization failed since matrix A is singular, D({0},{0}) = 0.",
				MethodKind.SVD => "SVD failed to converge, {0} super-diagonal elements of an upper bidiagonal matrix not converged.",
				MethodKind.Eigenvalue => "Eigenvalue decomposition failed since {0} off-diagonal elements of an intermediate tridiagonal form did not converge to zero",
				MethodKind.Schur => "Schur decomposition failed to compute all the eigenvalues, or the eigenvalues could not be reordered because some eigenvalues were too close to separate, or after reordering, round-off changed values of some complex eigenvalues so that leading eigenvalues in the Schur form no longer satisfy the input.",
				// Ignore Spelling: potrf
				MethodKind.GeneralEigen => "General eigenvalue decomposition failed since either `potrf` or `syevd` is wrong",
				MethodKind.Jacobi => "Jacobi method does not converge under given tolerance and maximum sweeps.",
				_ => "",
			};
		}

		/// <summary>
		/// Constructor of <see cref="MatrixAlgorithmException"/>
		/// </summary>
		/// <param name="kind">which kind of CUDA solver is used (in <see cref="MethodKind"/>)</param>
		/// <param name="i">returned device info</param>
		public MatrixAlgorithmException(MethodKind kind, int i) : base(string.Format(Resource.Culture, GetDescription(kind), i)) { }

		/// <summary>
		/// Empty <see cref="MatrixAlgorithmException"/>
		/// </summary>
		public MatrixAlgorithmException()
		{
		}

		/// <summary>
		/// <see cref="MatrixAlgorithmException"/> with custom message
		/// </summary>
		/// <param name="message"></param>
		public MatrixAlgorithmException(string message) : base(message)
		{
		}

		/// <summary>
		/// <see cref="MatrixAlgorithmException"/> with custom message and inner exception
		/// </summary>
		/// <param name="message"></param>
		/// <param name="innerException"></param>
		public MatrixAlgorithmException(string message, Exception innerException) : base(message, innerException)
		{
		}
	}
	#endregion

	#region enum
	/// <summary>
	/// The kind of matrix solver methods
	/// </summary>
	public enum MethodKind
	{
		// Ignore Spelling: potr getrf geqrf sytrf gesvd sygvd syevd syevj sygvj gesvdj
		/// <summary>
		/// [S|D|C|Z]potr[f|i]
		/// </summary>
		Cholesky,
		/// <summary>
		/// [S|D|C|Z]getrf
		/// </summary>
		LU,
		/// <summary>
		/// [S|D|C|Z]geqrf
		/// </summary>
		QR,
		/// <summary>
		/// [S|D|C|Z]gees
		/// </summary>
		Schur,
		/// <summary>
		/// [S|D|C|Z]sytrf
		/// </summary>
		Bunch_Kaufman,
		/// <summary>
		/// [S|D|C|Z]gesvd
		/// </summary>
		SVD,
		/// <summary>
		/// [S|D|C|Z]syevd[x]
		/// </summary>
		Eigenvalue,
		/// <summary>
		/// [S|D|C|Z]sygvd[x]
		/// </summary>
		GeneralEigen,
		/// <summary>
		/// [S|D|C|Z]syevj, [S|D|C|Z]sygvj, [S|D|C|Z]gesvdj
		/// </summary>
		Jacobi
	}

	/// <summary>
	/// Used for general symmetric eigenvalue solver
	/// </summary>
	public enum EigType
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
	/// Used for both standard and general symmetric eigenvalue solver
	/// </summary>
	public enum EigMode
	{
		/// <summary>
		/// Do not compute the eigenvectors
		/// </summary>
		NoVector = 0,
		/// <summary>
		/// Compute the eigenvectors (both left and right is possible)
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
	/// The storage indicator of SVD unitary matrices <c>S</c> and <c>V<sup>H</sup></c>
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



	/// <summary>
	/// The Solver routine interface
	/// </summary>
	public interface ISolver : IDisposable
	{
		#region eigen-problem
		/// <summary>
		/// Calculate the eigenvalues (and eigenvectors) of given Hermitian matrix <paramref name="A"/> for the special eigen-problem.
		/// </summary>
		/// <param name="n">number of rows and columns of <paramref name="A"/></param>
		/// <param name="valOut">The output eigenvalues, must be preallocated, of corresponding real type</param>
		/// <param name="A">The input/output matrix to calculate eigensystem; destroyed during the calculation if <paramref name="mode"/> is <see cref="EigMode.NoVector"/> or replaced by the eigenvectors otherwise</param>
		/// <param name="lda">leading dimension of <paramref name="A"/></param>
		/// <param name="mode">The <see cref="EigMode"/> to indicate whether the eigenvectors should be calculated</param>
		void EigenSpecialHermitianMatrix<T, TReal>(int n, Storage<TReal> valOut, Storage<T> A, int lda, EigMode mode) where T : struct, IComparable<T> where TReal : struct, IComparable<TReal>;

		/// <summary>
		/// Calculate the eigenvalues (and eigenvectors) of given Hermitian matrix <paramref name="A"/> for the special eigen-problem.
		/// </summary>
		/// <param name="n">number of rows and columns of <paramref name="A"/></param>
		/// <param name="valOut">The output eigenvalues, must be preallocated, of corresponding real type</param>
		/// <param name="A">The input/output matrix to calculate eigensystem; destroyed during the calculation if <paramref name="mode"/> is <see cref="EigMode.NoVector"/> or replaced by the eigenvectors otherwise</param>
		/// <param name="lda">leading dimension of <paramref name="A"/></param>
		/// <param name="mode">The <see cref="EigMode"/> to indicate whether the eigenvectors should be calculated</param>
		public delegate void DelegateEigenSpecialHermitianMatrix<T, TReal>(int n, Storage<TReal> valOut, Storage<T> A, int lda, EigMode mode) where T : struct, IComparable<T> where TReal : struct, IComparable<TReal>;

		/// <summary>
		/// Calculate the eigenvalues (and eigenvectors) of given Hermitian matrix pair <paramref name="A"/>, <paramref name="B"/> for the general one.
		/// </summary>
		/// <param name="n">number of rows and columns of <paramref name="A"/> and <paramref name="B"/></param>
		/// <param name="valOut">The output eigenvalues, must be preallocated, of corresponding real type</param>
		/// <param name="A">The input/output matrix to calculate eigensystem; destroyed during the calculation if <paramref name="mode"/> is <see cref="EigMode.NoVector"/> or replaced by the eigenvectors otherwise</param>
		/// <param name="lda">leading dimension of <paramref name="A"/></param>
		/// <param name="B">The input matrix to calculate general eigen-problem; if <c><paramref name="B"/> is null</c>, the normal eigen is performed and <paramref name="eigType"/> is not used; otherwise, the general one is performed</param>
		/// <param name="ldb">leading dimension of <paramref name="B"/>d</param>
		/// <param name="mode">The <see cref="EigMode"/> to indicate whether the eigenvectors should be calculated</param>
		/// <param name="eigType">The <see cref="EigType"/> to indicate positions of <paramref name="A"/> and <paramref name="B"/></param>
		void EigenGeneralHermitianMatrix<T, TReal>(int n, Storage<TReal> valOut, Storage<T> A, int lda, Storage<T> B, int ldb, EigType eigType, EigMode mode) where T : struct, IComparable<T> where TReal : struct, IComparable<TReal>;

		/// <summary>
		/// Calculate the eigenvalues (and eigenvectors) of given Hermitian matrix pair <paramref name="A"/>, <paramref name="B"/> for the general one.
		/// </summary>
		/// <param name="n">number of rows and columns of <paramref name="A"/> and <paramref name="B"/></param>
		/// <param name="valOut">The output eigenvalues, must be preallocated, of corresponding real type</param>
		/// <param name="A">The input/output matrix to calculate eigensystem; destroyed during the calculation if <paramref name="mode"/> is <see cref="EigMode.NoVector"/> or replaced by the eigenvectors otherwise</param>
		/// <param name="lda">leading dimension of <paramref name="A"/></param>
		/// <param name="B">The input matrix to calculate general eigen-problem; if <c><paramref name="B"/> is null</c>, the normal eigen is performed and <paramref name="eigType"/> is not used; otherwise, the general one is performed</param>
		/// <param name="ldb">leading dimension of <paramref name="B"/>d</param>
		/// <param name="mode">The <see cref="EigMode"/> to indicate whether the eigenvectors should be calculated</param>
		/// <param name="eigType">The <see cref="EigType"/> to indicate positions of <paramref name="A"/> and <paramref name="B"/></param>
		public delegate void DelegateEigenGeneralHermitianMatrix<T, TReal>(int n, Storage<TReal> valOut, Storage<T> A, int lda, Storage<T> B, int ldb, EigType eigType, EigMode mode) where T : struct, IComparable<T> where TReal : struct, IComparable<TReal>;

		/// <summary>
		/// Calculate the eigenvalues (and eigenvectors) of given general matrix <paramref name="A"/> for the special eigen-problem.
		/// </summary>
		/// <param name="n">number of rows and columns of <paramref name="A"/></param>
		/// <param name="valOut">The output eigenvalues, must be preallocated, of corresponding complex type</param>
		/// <param name="leftVec">The output left eigenvectors, must be preallocated, of corresponding complex type</param>
		/// <param name="ldvl">The leading dimension of <paramref name="leftVec"/></param>
		/// <param name="ldvr">The leading dimension of <paramref name="rightVec"/></param>
		/// <param name="rightVec">The output right eigenvectors, must be preallocated, of corresponding complex type</param>
		/// <param name="A">The input/output matrix to calculate eigensystem; destroyed during the calculation if <paramref name="mode"/> is <see cref="EigMode.NoVector"/> or replaced by the eigenvectors otherwise</param>
		/// <param name="lda">leading dimension of <paramref name="A"/></param>
		/// <param name="mode">The <see cref="EigMode"/> to indicate whether the eigenvectors should be calculated</param>
		void EigenSpecialGeneralMatrix<T, TComplex>(int n, Storage<TComplex> valOut, Storage<TComplex> leftVec, int ldvl, Storage<TComplex> rightVec, int ldvr, Storage<T> A, int lda, EigMode mode) where T : struct, IComparable<T> where TComplex : struct, IComparable<TComplex>;

		/// <summary>
		/// Calculate the eigenvalues (and eigenvectors) of given general matrix <paramref name="A"/> for the special eigen-problem.
		/// </summary>
		/// <param name="n">number of rows and columns of <paramref name="A"/></param>
		/// <param name="valOut">The output eigenvalues, must be preallocated, of corresponding complex type</param>
		/// <param name="leftVec">The output left eigenvectors, must be preallocated, of corresponding complex type</param>
		/// <param name="ldvl">The leading dimension of <paramref name="leftVec"/></param>
		/// <param name="ldvr">The leading dimension of <paramref name="rightVec"/></param>
		/// <param name="rightVec">The output right eigenvectors, must be preallocated, of corresponding complex type</param>
		/// <param name="A">The input/output matrix to calculate eigensystem; destroyed during the calculation if <paramref name="mode"/> is <see cref="EigMode.NoVector"/> or replaced by the eigenvectors otherwise</param>
		/// <param name="lda">leading dimension of <paramref name="A"/></param>
		/// <param name="mode">The <see cref="EigMode"/> to indicate whether the eigenvectors should be calculated</param>
		public delegate void DelegateEigenSpecialGeneralMatrix<T, TComplex>(int n, Storage<TComplex> valOut, Storage<TComplex> leftVec, int ldvl, Storage<TComplex> rightVec, int ldvr, Storage<T> A, int lda, EigMode mode) where T : struct, IComparable<T> where TComplex : struct, IComparable<TComplex>;

		/// <summary>
		/// Calculate the eigenvalues (and eigenvectors) of given general matrix pair <paramref name="A"/>, <paramref name="B"/> for the general one.
		/// </summary>
		/// <param name="n">number of rows and columns of <paramref name="A"/> and <paramref name="B"/></param>
		/// <param name="valOut">The output eigenvalues, must be preallocated, of corresponding complex type</param>
		/// <param name="leftVec">The output left eigenvectors, must be preallocated, of corresponding complex type</param>
		/// <param name="ldvl">The leading dimension of <paramref name="leftVec"/></param>
		/// <param name="ldvr">The leading dimension of <paramref name="rightVec"/></param>
		/// <param name="rightVec">The output right eigenvectors, must be preallocated, of corresponding complex type</param>
		/// <param name="A">The input/output matrix to calculate eigensystem; destroyed during the calculation if <paramref name="mode"/> is <see cref="EigMode.NoVector"/> or replaced by the eigenvectors otherwise</param>
		/// <param name="lda">leading dimension of <paramref name="A"/></param>
		/// <param name="B">The input matrix to calculate general eigen-problem; if <c><paramref name="B"/> is null</c>, the normal eigen is performed and <paramref name="eigType"/> is not used; otherwise, the general one is performed</param>
		/// <param name="ldb">leading dimension of <paramref name="B"/>d</param>
		/// <param name="mode">The <see cref="EigMode"/> to indicate whether the eigenvectors should be calculated</param>
		/// <param name="eigType">The <see cref="EigType"/> to indicate positions of <paramref name="A"/> and <paramref name="B"/></param>
		void EigenGeneralGeneralMatrix<T, TComplex>(int n, Storage<TComplex> valOut, Storage<TComplex> leftVec, int ldvl, Storage<TComplex> rightVec, int ldvr, Storage<T> A, int lda, Storage<T> B, int ldb, EigType eigType, EigMode mode) where T : struct, IComparable<T> where TComplex : struct, IComparable<TComplex>;

		/// <summary>
		/// Calculate the eigenvalues (and eigenvectors) of given general matrix pair <paramref name="A"/>, <paramref name="B"/> for the general one.
		/// </summary>
		/// <param name="n">number of rows and columns of <paramref name="A"/> and <paramref name="B"/></param>
		/// <param name="valOut">The output eigenvalues, must be preallocated, of corresponding complex type</param>
		/// <param name="leftVec">The output left eigenvectors, must be preallocated, of corresponding complex type</param>
		/// <param name="ldvl">The leading dimension of <paramref name="leftVec"/></param>
		/// <param name="ldvr">The leading dimension of <paramref name="rightVec"/></param>
		/// <param name="rightVec">The output right eigenvectors, must be preallocated, of corresponding complex type</param>
		/// <param name="A">The input/output matrix to calculate eigensystem; destroyed during the calculation if <paramref name="mode"/> is <see cref="EigMode.NoVector"/> or replaced by the eigenvectors otherwise</param>
		/// <param name="lda">leading dimension of <paramref name="A"/></param>
		/// <param name="B">The input matrix to calculate general eigen-problem; if <c><paramref name="B"/> is null</c>, the normal eigen is performed and <paramref name="eigType"/> is not used; otherwise, the general one is performed</param>
		/// <param name="ldb">leading dimension of <paramref name="B"/>d</param>
		/// <param name="mode">The <see cref="EigMode"/> to indicate whether the eigenvectors should be calculated</param>
		/// <param name="eigType">The <see cref="EigType"/> to indicate positions of <paramref name="A"/> and <paramref name="B"/></param>
		public delegate void DelegateEigenGeneralGeneralMatrix<T, TComplex>(int n, Storage<TComplex> valOut, Storage<TComplex> leftVec, int ldvl, Storage<TComplex> rightVec, int ldvr, Storage<T> A, int lda, Storage<T> B, int ldb, EigType eigType, EigMode mode) where T : struct, IComparable<T> where TComplex : struct, IComparable<TComplex>;
		#endregion

		#region SVD
		/// <summary>
		/// This function computes the singular value decomposition (SVD) of a matrix <paramref name="A"/> and corresponding the left and/or right singular vectors: $A = U S V^*$.
		/// </summary>
		/// <param name="jobu">specifies options for computing all or part of the matrix <paramref name="U"/></param>
		/// <param name="jobvt">specifies options for computing all or part of the matrix <paramref name="Vct"/>, same as <paramref name="jobu"/></param>
		/// <param name="m">number of rows of matrix</param>
		/// <param name="n">number of columns of matrix</param>
		/// <param name="A">matrix with size <paramref name="m"/>×<paramref name="n"/> and leading dimension <paramref name="lda"/></param>
		/// <param name="lda">leading dimension of <paramref name="A"/></param>
		/// <param name="S">output singular values of size <c>min(<paramref name="m"/>, <paramref name="n"/>)</c>, must be pre-allocated and of corresponding real type</param>
		/// <param name="U">left unitary matrix with size <paramref name="ldu"/>×<paramref name="m"/>, must be pre-allocated</param>
		/// <param name="ldu">leading dimension of <paramref name="U"/></param>
		/// <param name="Vct">right unitary ($V^*$) matrix with size <paramref name="ldVct"/>×<paramref name="n"/>, must be pre-allocated</param>
		/// <param name="ldVct">leading dimension of <paramref name="Vct"/></param>
		void SingularValues<T, TReal>(SVDStore jobu, SVDStore jobvt, int m, int n, Storage<T> A, int lda, Storage<TReal> S, Storage<T> U, int ldu, Storage<T> Vct, int ldVct) where T : struct, IComparable<T> where TReal : struct, IComparable<TReal>;

		/// <summary>
		/// This function computes the singular value decomposition (SVD) of a matrix <paramref name="A"/> and corresponding the left and/or right singular vectors: $A = U S V^*$.
		/// </summary>
		/// <param name="jobu">specifies options for computing all or part of the matrix <paramref name="U"/></param>
		/// <param name="jobvt">specifies options for computing all or part of the matrix <paramref name="Vct"/>, same as <paramref name="jobu"/></param>
		/// <param name="m">number of rows of matrix</param>
		/// <param name="n">number of columns of matrix</param>
		/// <param name="A">matrix with size <paramref name="m"/>×<paramref name="n"/> and leading dimension <paramref name="lda"/></param>
		/// <param name="lda">leading dimension of <paramref name="A"/></param>
		/// <param name="S">output singular values of size <c>min(<paramref name="m"/>, <paramref name="n"/>)</c>, must be pre-allocated and of corresponding real type</param>
		/// <param name="U">left unitary matrix with size <paramref name="ldu"/>×<paramref name="m"/>, must be pre-allocated</param>
		/// <param name="ldu">leading dimension of <paramref name="U"/></param>
		/// <param name="Vct">right unitary ($V^*$) matrix with size <paramref name="ldVct"/>×<paramref name="n"/>, must be pre-allocated</param>
		/// <param name="ldVct">leading dimension of <paramref name="Vct"/></param>
		public delegate void DelegateSingularValues<T, TReal>(SVDStore jobu, SVDStore jobvt, int m, int n, Storage<T> A, int lda, Storage<TReal> S, Storage<T> U, int ldu, Storage<T> Vct, int ldVct) where T : struct, IComparable<T> where TReal : struct, IComparable<TReal>;
		#endregion

		#region solve
		/// <summary>
		/// Solve a series of linear systems: $A X = B$, where each column pair of X and <paramref name="B"/> is a linear system. <br/>
		/// Both <paramref name="A"/> and <paramref name="B"/> are in-place where <paramref name="A"/> is replaced by its LU decomposition and <paramref name="B"/> the solution X.
		/// </summary>
		/// <param name="n">number of rows and columns of matrix <paramref name="A"/></param>
		/// <param name="nrhs">number of right-hand sides.</param>
		/// <param name="A">The coefficient matrix</param>
		/// <param name="lda">leading dimension of <paramref name="A"/></param>
		/// <param name="B">each column of this matrix is the vector at right; overwritten by solution X in the end</param>
		/// <param name="ldb">leading dimension of <paramref name="B"/></param>
		void LinearSolve<T>(int n, int nrhs, Storage<T> A, int lda, Storage<T> B, int ldb) where T : struct, IComparable<T>;

		/// <summary>
		/// Solve a series of linear systems: $A X = B$, where each column pair of X and <paramref name="B"/> is a linear system. <br/>
		/// Both <paramref name="A"/> and <paramref name="B"/> are in-place where <paramref name="A"/> is replaced by its LU decomposition and <paramref name="B"/> the solution X.
		/// </summary>
		/// <param name="n">number of rows and columns of matrix <paramref name="A"/></param>
		/// <param name="nrhs">number of right-hand sides.</param>
		/// <param name="A">The coefficient matrix</param>
		/// <param name="lda">leading dimension of <paramref name="A"/></param>
		/// <param name="B">each column of this matrix is the vector at right; overwritten by solution X in the end</param>
		/// <param name="ldb">leading dimension of <paramref name="B"/></param>
		public delegate void DelegateLinearSolve<T>(int n, int nrhs, Storage<T> A, int lda, Storage<T> B, int ldb) where T : struct, IComparable<T>;
		#endregion

		#region decomposition
		/// <summary>
		/// QR factorize the given matrix <paramref name="A"/>.
		/// </summary>
		/// <param name="full">perform full factorization or not</param>
		/// <param name="m">number of rows of matrix <paramref name="A"/></param>
		/// <param name="n">number of columns of matrix <paramref name="A"/></param>
		/// <param name="A">matrix to be factorized, overwritten by the unitary matrix after return</param>
		/// <param name="lda">leading dimension of <paramref name="A"/></param>
		/// <param name="tri">The output triangular matrix</param>
		/// <param name="ldt">leading dimension of <paramref name="tri"/></param>
		void QRDecomposition<T>(bool full, int m, int n, Storage<T> A, int lda, Storage<T> tri, int ldt) where T : struct, IComparable<T>;

		/// <summary>
		/// QR factorize the given matrix <paramref name="A"/>.
		/// </summary>
		/// <param name="full">perform full factorization or not</param>
		/// <param name="m">number of rows of matrix <paramref name="A"/></param>
		/// <param name="n">number of columns of matrix <paramref name="A"/></param>
		/// <param name="A">matrix to be factorized, overwritten by the unitary matrix after return</param>
		/// <param name="lda">leading dimension of <paramref name="A"/></param>
		/// <param name="tri">The output triangular matrix</param>
		/// <param name="ldt">leading dimension of <paramref name="tri"/></param>
		public delegate void DelegateQRDecomposition<T>(bool full, int m, int n, Storage<T> A, int lda, Storage<T> tri, int ldt) where T : struct, IComparable<T>;

		/// <summary>
		/// Compute the Schur decomposition of given matrix <paramref name="A"/>.
		/// </summary>
		/// <param name="n">number of rows and columns of matrix <paramref name="A"/></param>
		/// <param name="A">matrix to be decomposed, overwritten by the triangular matrix at exit</param>
		/// <param name="lda">leading dimension of <paramref name="A"/></param>
		/// <param name="U">The output unitary matrix</param>
		/// <param name="ldu">leading dimension of <paramref name="U"/></param>
		/// <param name="jobu">calculate Schur vectors or not</param>
		/// <param name="orderVal">The value order of the factorization so that selected eigenvalues are at the top left of Schur form. Default null means use default order</param>
		/// <returns>the actual number of eigenvalues returned</returns>
		int SchurDecomposition<T>(int n, Storage<T> A, int lda, Storage<T> U, int ldu, EigMode jobu,  DoubleComplex[] orderVal = null) where T : struct, IComparable<T>;

		/// <summary>
		/// Compute the Schur decomposition of given matrix <paramref name="A"/>.
		/// </summary>
		/// <param name="n">number of rows and columns of matrix <paramref name="A"/></param>
		/// <param name="A">matrix to be decomposed, overwritten by the triangular matrix at exit</param>
		/// <param name="lda">leading dimension of <paramref name="A"/></param>
		/// <param name="U">The output unitary matrix</param>
		/// <param name="ldu">leading dimension of <paramref name="U"/></param>
		/// <param name="jobu">calculate Schur vectors or not</param>
		/// <param name="orderVal">The value order of the factorization so that selected eigenvalues are at the top left of Schur form. Default null means use default order</param>
		/// <returns>the actual number of eigenvalues returned</returns>
		public delegate int DelegateSchurDecomposition<T>(int n, Storage<T> A, int lda, Storage<T> U, int ldu, EigMode jobu, DoubleComplex[] orderVal = null) where T : struct, IComparable<T>;
		#endregion
	}
}


namespace Althea.Solver.Cuda
{
	internal sealed class CudaSolver : ISolver
	{
		#region base
		private readonly IntPtr _denseHandle = default;
		private readonly IntPtr _sparseHandle = default;

		public CudaSolver()
		{
			Dense.NativeMethods.cusolverDnCreate(ref this._denseHandle).Check();
			Sparse.NativeMethods.cusolverSpCreate(ref this._sparseHandle).Check();
		}

		public void Dispose()
		{
			var err1 = Dense.NativeMethods.cusolverDnDestroy(this._denseHandle);
			var err2 = Sparse.NativeMethods.cusolverSpDestroy(this._sparseHandle);
			if (err1 != Status.Success || err2 != Status.Success)
				throw new StatusException(System.Reflection.MethodBase.GetCurrentMethod(), err1, err2);
			GC.SuppressFinalize(this);
		}
		#endregion

		#region checks
		private static void CheckInfo(Status s, Storage<int> info, MethodKind kind, int matSize)
		{
			if (s != Status.Success)
			{
				throw new StatusException(s, new System.Diagnostics.StackTrace(skipFrames: 1));
			}

			int i = RT.CopyOut(info);
			if (i == 0)
			{
				return;
			}
			else if (i < 0)
			{
				throw new ArgumentException($"The {(i - 1).ToOrdinal()} parameter is wrong.");
			}
			else if (kind == MethodKind.Jacobi && i != matSize + 1)
			{
				return;
			}
			else
			{
				throw new MatrixAlgorithmException(kind, i);
			}
		}
		#endregion

		#region eigen solve
		public void EigenGeneralHermitianMatrix<T, TReal>(int n, Storage<TReal> valOut, Storage<T> A, int lda, Storage<T> B, int ldb, EigType eigType, EigMode mode) where T : struct, IComparable<T> where TReal : struct, IComparable<TReal>
		{
			Dense.NativeMethods.sygvdBufFunc buffer;
			Dense.NativeMethods.sygvdFunc gvd;
			switch (default(T).ToDataType())
			{
				case DataType.RealSingle:
					buffer = Dense.NativeMethods.cusolverDnSsygvd_bufferSize;
					gvd = Dense.NativeMethods.cusolverDnSsygvd;
					break;
				case DataType.RealDouble:
					buffer = Dense.NativeMethods.cusolverDnDsygvd_bufferSize;
					gvd = Dense.NativeMethods.cusolverDnDsygvd;
					break;
				case DataType.ComplexSingle:
					buffer = Dense.NativeMethods.cusolverDnChegvd_bufferSize;
					gvd = Dense.NativeMethods.cusolverDnChegvd;
					break;
				case DataType.ComplexDouble:
					buffer = Dense.NativeMethods.cusolverDnZhegvd_bufferSize;
					gvd = Dense.NativeMethods.cusolverDnZhegvd;
					break;
				default:
					throw new NotSupportedException(Resource.DataTypeNotSupport);
			}
			int lengthWork = 0;
			buffer(this._denseHandle, eigType, mode, MatrixFillMode.Upper, n, A, lda, B, ldb, valOut, ref lengthWork).Check();
			using var workBuf = Storage<T>.Create(lengthWork, onHost: false);
			using var devInfo = Storage<int>.Create(1, onHost: false);
			var status = gvd(this._denseHandle, eigType, mode, MatrixFillMode.Upper, n, A, lda, B, ldb, valOut, workBuf, lengthWork, devInfo);
			CheckInfo(status, devInfo, MethodKind.GeneralEigen, n);
		}

		public void EigenSpecialHermitianMatrix<T, TReal>(int n, Storage<TReal> valOut, Storage<T> A, int lda, EigMode mode) where T : struct, IComparable<T> where TReal : struct, IComparable<TReal>
		{
			Dense.NativeMethods.syevdBufFunc buffer;
			Dense.NativeMethods.syevdFunc evd;
			switch (default(T).ToDataType())
			{
				case DataType.RealSingle:
					buffer = Dense.NativeMethods.cusolverDnSsyevd_bufferSize;
					evd = Dense.NativeMethods.cusolverDnSsyevd;
					break;
				case DataType.RealDouble:
					buffer = Dense.NativeMethods.cusolverDnDsyevd_bufferSize;
					evd = Dense.NativeMethods.cusolverDnDsyevd;
					break;
				case DataType.ComplexSingle:
					buffer = Dense.NativeMethods.cusolverDnCheevd_bufferSize;
					evd = Dense.NativeMethods.cusolverDnCheevd;
					break;
				case DataType.ComplexDouble:
					buffer = Dense.NativeMethods.cusolverDnZheevd_bufferSize;
					evd = Dense.NativeMethods.cusolverDnZheevd;
					break;
				default:
					throw new NotSupportedException(Resource.DataTypeNotSupport);
			}
			int lengthWork = 0;
			buffer(this._denseHandle, mode, MatrixFillMode.Upper, n, A, lda, valOut, ref lengthWork).Check();
			using var workBuf = Storage<T>.Create(lengthWork, onHost: false);
			using var devInfo = Storage<int>.Create(1, onHost: false);
			var status = evd(this._denseHandle, mode, MatrixFillMode.Upper, n, A, lda, valOut, workBuf, lengthWork, devInfo);
			CheckInfo(status, devInfo, MethodKind.Eigenvalue, n);
		}

		public void EigenSpecialGeneralMatrix<T, TComplex>(int n, Storage<TComplex> valOut, Storage<TComplex> leftVec, int ldvl, Storage<TComplex> rightVec, int ldvr, Storage<T> A, int lda, EigMode mode)
			where T : struct, IComparable<T>
			where TComplex : struct, IComparable<TComplex>
		{
			throw new NotImplementedException();
		}

		public void EigenGeneralGeneralMatrix<T, TComplex>(int n, Storage<TComplex> valOut, Storage<TComplex> leftVec, int ldvl, Storage<TComplex> rightVec, int ldvr, Storage<T> A, int lda, Storage<T> B, int ldb, EigType eigType, EigMode mode)
			where T : struct, IComparable<T>
			where TComplex : struct, IComparable<TComplex>
		{
			throw new NotImplementedException();
		}
		#endregion

		#region SVD
		public void SingularValues<T, TReal>(SVDStore jobu, SVDStore jobv, int m, int n, Storage<T> A, int lda, Storage<TReal> S, Storage<T> U, int ldu, Storage<T> Vct, int ldVct) where T : struct, IComparable<T> where TReal : struct, IComparable<TReal>
		{
			Dense.NativeMethods.svdBufFunc buffer;
			Dense.NativeMethods.svdFunc svd;
			switch (default(T).ToDataType())
			{
				case DataType.RealSingle:
					buffer = Dense.NativeMethods.cusolverDnSgesvd_bufferSize;
					svd = Dense.NativeMethods.cusolverDnSgesvd;
					break;
				case DataType.RealDouble:
					buffer = Dense.NativeMethods.cusolverDnDgesvd_bufferSize;
					svd = Dense.NativeMethods.cusolverDnDgesvd;
					break;
				case DataType.ComplexSingle:
					buffer = Dense.NativeMethods.cusolverDnCgesvd_bufferSize;
					svd = Dense.NativeMethods.cusolverDnCgesvd;
					break;
				case DataType.ComplexDouble:
					buffer = Dense.NativeMethods.cusolverDnZgesvd_bufferSize;
					svd = Dense.NativeMethods.cusolverDnZgesvd;
					break;
				default:
					throw new NotSupportedException(Resource.DataTypeNotSupport);
			}
			int lengthWork = 0;
			buffer(this._denseHandle, m, n, ref lengthWork).Check();
			using var workBuf = Storage<T>.Create(lengthWork, onHost: false);
			using var realWorkBuf = Storage<TReal>.Create(Math.Min(m, n) - 1, onHost: false);
			using var devInfo = Storage<int>.Create(1, onHost: false);
			var status = svd(this._denseHandle, jobu.ToChar(), jobv.ToChar(), m, n, A, lda, S, U, ldu, Vct, ldVct, workBuf, lengthWork, realWorkBuf, devInfo);
			CheckInfo(status, devInfo, MethodKind.SVD, n);
		}
		#endregion

		#region solve
		public void LinearSolve<T>(int n, int nrhs, Storage<T> A, int lda, Storage<T> B, int ldb) where T : struct, IComparable<T>
		{
			// initialize delegate
			Dense.NativeMethods.getrfBufFunc buf;
			Dense.NativeMethods.getrfFunc trf; // factor delegate
			Dense.NativeMethods.getrsFunc trs; // solve delegate
			switch (default(T).ToDataType())
			{
				case DataType.RealSingle:
					buf = Dense.NativeMethods.cusolverDnSgetrf_bufferSize;
					trf = Dense.NativeMethods.cusolverDnSgetrf;
					trs = Dense.NativeMethods.cusolverDnSgetrs;
					break;
				case DataType.RealDouble:
					buf = Dense.NativeMethods.cusolverDnDgetrf_bufferSize;
					trf = Dense.NativeMethods.cusolverDnDgetrf;
					trs = Dense.NativeMethods.cusolverDnDgetrs;
					break;
				case DataType.ComplexSingle:
					buf = Dense.NativeMethods.cusolverDnCgetrf_bufferSize;
					trf = Dense.NativeMethods.cusolverDnCgetrf;
					trs = Dense.NativeMethods.cusolverDnCgetrs;
					break;
				case DataType.ComplexDouble:
					buf = Dense.NativeMethods.cusolverDnZgetrf_bufferSize;
					trf = Dense.NativeMethods.cusolverDnZgetrf;
					trs = Dense.NativeMethods.cusolverDnZgetrs;
					break;
				default:
					throw new NotSupportedException(Resource.DataTypeNotSupport);
			}

			// triangular LU decomposition
			int lengthWork = 0;
			buf(this._denseHandle, n, n, A, lda, ref lengthWork).Check();
			using var workBuf = Storage<T>.Create(lengthWork, onHost: false);
			using var pivotIndices = Storage<int>.Create(n, onHost: false);
			using var devInfo = Storage<int>.Create(1, onHost: false);
			// factor linear triangular systems
			var status = trf(this._denseHandle, n, n, A, lda, workBuf, pivotIndices, devInfo);
			CheckInfo(status, devInfo, MethodKind.LU, n);
			// solve linear triangular systems
			status = trs(this._denseHandle, MatrixOperation.None, n, nrhs, A, lda, pivotIndices, B, ldb, devInfo);
			CheckInfo(status, devInfo, MethodKind.LU, n);
		}

		public void QRDecomposition<T>(bool full, int m, int n, Storage<T> A, int lda, Storage<T> tri, int ldt) where T : struct, IComparable<T>
		{
			// initialize delegate
			Dense.NativeMethods.geqrfBufFunc qrfbuf;
			Dense.NativeMethods.geqrfFunc qrf; // QR delegate
			Dense.NativeMethods.orgqrBufFunc formQBuf;
			Dense.NativeMethods.orgqrFunc formQ; // form Q delegate
			switch (default(T).ToDataType())
			{
				case DataType.RealSingle:
					qrfbuf = Dense.NativeMethods.cusolverDnSgeqrf_bufferSize;
					qrf = Dense.NativeMethods.cusolverDnSgeqrf;
					formQBuf = Dense.NativeMethods.cusolverDnSorgqr_bufferSize;
					formQ = Dense.NativeMethods.cusolverDnSorgqr;
					break;
				case DataType.RealDouble:
					qrfbuf = Dense.NativeMethods.cusolverDnDgeqrf_bufferSize;
					qrf = Dense.NativeMethods.cusolverDnDgeqrf;
					formQBuf = Dense.NativeMethods.cusolverDnDorgqr_bufferSize;
					formQ = Dense.NativeMethods.cusolverDnDorgqr;
					break;
				case DataType.ComplexSingle:
					qrfbuf = Dense.NativeMethods.cusolverDnCgeqrf_bufferSize;
					qrf = Dense.NativeMethods.cusolverDnCgeqrf;
					formQBuf = Dense.NativeMethods.cusolverDnCorgqr_bufferSize;
					formQ = Dense.NativeMethods.cusolverDnCorgqr;
					break;
				case DataType.ComplexDouble:
					qrfbuf = Dense.NativeMethods.cusolverDnZgeqrf_bufferSize;
					qrf = Dense.NativeMethods.cusolverDnZgeqrf;
					formQBuf = Dense.NativeMethods.cusolverDnZorgqr_bufferSize;
					formQ = Dense.NativeMethods.cusolverDnZorgqr;
					break;
				default:
					throw new NotSupportedException(Resource.DataTypeNotSupport);
			}
			int k = Math.Min(m, n);
			int p = Math.Max(m, n);
			// QR buffer
			int lengthWork = 0;
			qrfbuf(this._denseHandle, m, n, A, lda, ref lengthWork).Check();
			using var workBuf = Storage<T>.Create(lengthWork, onHost: false);
			using var devInfo = Storage<int>.Create(1, onHost: false);
			using var τ = Storage<T>.Create(k, onHost: false);
			// QR
			var status = qrf(this._denseHandle, m, n, A, lda, τ, workBuf, lengthWork, devInfo);
			CheckInfo(status, devInfo, MethodKind.QR, n);
			// copy out trapezoidal matrix
			RT.CopyMatrixTo(source: A, dest: tri, srcLD: lda, dstLD: ldt, copyNRows: k, copyNCols: n);
			// from matrix Q
			int fromQLengthWork = 0;
			formQBuf(this._denseHandle, m, n, k, A, lda, τ, ref fromQLengthWork).Check();
			Storage<T> fromQWorkBuf = workBuf;
			if (fromQLengthWork > lengthWork)
			{
				workBuf.Dispose();
				fromQWorkBuf = Storage<T>.Create(fromQLengthWork, onHost: false);
			}
			using (fromQWorkBuf)
			{
				status = formQ(this._denseHandle, full ? p : m, full ? p : n, k, A, lda, τ, fromQWorkBuf, fromQLengthWork, devInfo);
				CheckInfo(status, devInfo, MethodKind.QR, n);
			}
		}

		public int SchurDecomposition<T>(int n, Storage<T> A, int lda, Storage<T> U, int ldu, EigMode jobu,  DoubleComplex[] orderVal = null) where T : struct, IComparable<T>
		{
			throw new NotImplementedException();
		}
		#endregion
	}
}


namespace Althea.Solver.Mkl
{
	internal sealed class MklSolver : ISolver
	{
		#region base
		public MklSolver()
		{
			// do nothing
		}

		public void Dispose()
		{
			// do nothing
			GC.SuppressFinalize(this);
		}
		#endregion

		#region checks
		private static void CheckInfo(int info, MethodKind kind, int matSize)
		{
			int i = info;
			if (i == 0)
			{
				return;
			}
			else if (i < 0)
			{
				throw new ArgumentException($"The {(-i - 1).ToOrdinal()} parameter is wrong.");
			}
			else if (kind == MethodKind.Jacobi && i != matSize + 1)
			{
				return;
			}
			else
			{
				throw new MatrixAlgorithmException(kind, i);
			}
		}
		#endregion

		#region eigen solve
		public void EigenSpecialGeneralMatrix<T, TComplex>(int n, Storage<TComplex> valOut, Storage<TComplex> leftVec, int ldvl, Storage<TComplex> rightVec, int ldvr, Storage<T> A, int lda, EigMode mode)
			where T : struct, IComparable<T>
			where TComplex : struct, IComparable<TComplex>
		{
			// constraints
			ldvr = Math.Max(1, ldvr);
			ldvl = Math.Max(1, ldvl);

			var datatype = default(T).ToDataType();
			Storage<TComplex> compA = null;
			try
			{
				// convert to complex first
				if (!(A is Storage<TComplex>))
				{
					compA = Storage<TComplex>.Create(lda * n, onHost: true);
					RT.SetValue(compA, 0, lda * n);
					var compArealPtr = compA.As<T>();
					// copy to realA to get a complex array
					Blas.API.CPU.Copy(lda * n, x: A, incx: 1, y: compArealPtr, incy: 2);
				}
				else
					compA = A as Storage<TComplex>;
				// compute
				NativeMethods.geevCompFunc geev = datatype switch
				{
					DataType.RealSingle => NativeMethods.LAPACKE_cgeev,
					DataType.RealDouble => NativeMethods.LAPACKE_zgeev,
					DataType.ComplexSingle => NativeMethods.LAPACKE_cgeev,
					DataType.ComplexDouble => NativeMethods.LAPACKE_zgeev,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
				};
				var info = geev(MklSolverLayout.ColMajor, leftVec is null ? (sbyte)'N' : (sbyte)'V', rightVec is null ? (sbyte)'N' : (sbyte)'V', n, compA, lda, valOut, leftVec, ldvl, rightVec, ldvr);
				CheckInfo(info, MethodKind.Eigenvalue, n);
			}
			finally
			{
				if (compA.Ptr != A.Ptr) compA?.Dispose();
			}
		}

		public void EigenGeneralGeneralMatrix<T, TComplex>(int n, Storage<TComplex> valOut, Storage<TComplex> leftVec, int ldvl, Storage<TComplex> rightVec, int ldvr, Storage<T> A, int lda, Storage<T> B, int ldb, EigType eigType, EigMode mode)
			where T : struct, IComparable<T>
			where TComplex : struct, IComparable<TComplex>
		{
			throw new NotImplementedException();
		}

		public void EigenGeneralHermitianMatrix<T, TReal>(int n, Storage<TReal> valOut, Storage<T> A, int lda, Storage<T> B, int ldb, EigType eigType, EigMode mode) where T : struct, IComparable<T> where TReal : struct, IComparable<TReal>
		{
			throw new NotImplementedException();
		}

		public void EigenSpecialHermitianMatrix<T, TReal>(int n, Storage<TReal> valOut, Storage<T> A, int lda, EigMode mode) where T : struct, IComparable<T> where TReal : struct, IComparable<TReal>
		{
			NativeMethods.syevdFunc evd = (default(T).ToDataType()) switch
			{
				DataType.RealSingle => NativeMethods.LAPACKE_ssyevd,
				DataType.RealDouble => NativeMethods.LAPACKE_dsyevd,
				DataType.ComplexSingle => NativeMethods.LAPACKE_cheevd,
				DataType.ComplexDouble => NativeMethods.LAPACKE_zheevd,
				_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
			};
			var info = evd(MklSolverLayout.ColMajor, mode.ToChar(), MatrixFillMode.Upper.ToChar(), n, A, lda, valOut);
			CheckInfo(info, MethodKind.Eigenvalue, n);
		}
		#endregion 

		#region solve
		public void LinearSolve<T>(int n, int nrhs, Storage<T> A, int lda, Storage<T> B, int ldb) where T : struct, IComparable<T>
		{
			// initialize delegate
			NativeMethods.getrfFunc trf; // factor delegate
			NativeMethods.getrsFunc trs; // solve delegate
			switch (default(T).ToDataType())
			{
				case DataType.RealSingle:
					trf = NativeMethods.LAPACKE_sgetrf;
					trs = NativeMethods.LAPACKE_sgetrs;
					break;
				case DataType.RealDouble:
					trf = NativeMethods.LAPACKE_dgetrf;
					trs = NativeMethods.LAPACKE_dgetrs;
					break;
				case DataType.ComplexSingle:
					trf = NativeMethods.LAPACKE_cgetrf;
					trs = NativeMethods.LAPACKE_cgetrs;
					break;
				case DataType.ComplexDouble:
					trf = NativeMethods.LAPACKE_zgetrf;
					trs = NativeMethods.LAPACKE_zgetrs;
					break;
				default:
					throw new NotSupportedException(Resource.DataTypeNotSupport);
			}

			// triangular LU decomposition
			using var pivotIndices = Storage<int>.Create(n, onHost: true);
			// factor linear triangular systems
			var info = trf(MklSolverLayout.ColMajor, n, n, A, lda, pivotIndices);
			CheckInfo(info, MethodKind.LU, n);
			// solve linear triangular systems
			info = trs(MklSolverLayout.ColMajor, MatrixOperation.None.ToCharMatrixOp(), n, nrhs, A, lda, pivotIndices, B, ldb);
			CheckInfo(info, MethodKind.LU, n);
		}
		#endregion

		#region SVD and QR
		public void QRDecomposition<T>(bool full, int m, int n, Storage<T> A, int lda, Storage<T> tri, int ldt) where T : struct, IComparable<T>
		{
			// initialize delegate
			NativeMethods.geqrfFunc qrf; // QR delegate
			NativeMethods.orgqrFunc formQ; // form Q delegate
			switch (default(T).ToDataType())
			{
				case DataType.RealSingle:
					qrf = NativeMethods.LAPACKE_sgeqrf;
					formQ = NativeMethods.LAPACKE_sorgqr;
					break;
				case DataType.RealDouble:
					qrf = NativeMethods.LAPACKE_dgeqrf;
					formQ = NativeMethods.LAPACKE_dorgqr;
					break;
				case DataType.ComplexSingle:
					qrf = NativeMethods.LAPACKE_cgeqrf;
					formQ = NativeMethods.LAPACKE_cungqr;
					break;
				case DataType.ComplexDouble:
					qrf = NativeMethods.LAPACKE_zgeqrf;
					formQ = NativeMethods.LAPACKE_zungqr;
					break;
				default:
					throw new NotSupportedException(Resource.DataTypeNotSupport);
			}
			int k = Math.Min(m, n);
			int p = Math.Max(m, n);
			// triangular LU decomposition
			using var τ = Storage<T>.Create(k, onHost: true);
			// QR
			var info = qrf(MklSolverLayout.ColMajor, m, n, A, lda, τ);
			CheckInfo(info, MethodKind.QR, n);
			// copy out trapezoidal matrix
			RT.CopyMatrixTo(source: A, dest: tri, srcLD: lda, dstLD: ldt, copyNRows: k, copyNCols: n);
			// set sub-diagonal to zero since it contains info to construct Q
			using var zeros = Storage<T>.Create(k - 1, onHost: true);
			RT.SetValue(zeros, 0);
			Blas.API.CPU.Copy(k - 1, zeros, 1, tri + 1, ldt + 1);
			// from matrix Q
			info = formQ(MklSolverLayout.ColMajor, full ? p : m, full ? p : n, k, A, lda, τ);
			CheckInfo(info, MethodKind.QR, n);
		}

		public void SingularValues<T, TReal>(SVDStore jobu, SVDStore jobvt, int m, int n, Storage<T> A, int lda, Storage<TReal> S, Storage<T> U, int ldu, Storage<T> Vct, int ldVct) where T : struct, IComparable<T> where TReal : struct, IComparable<TReal>
		{
			// constraints
			ldu = Math.Max(1, ldu);
			ldVct = Math.Max(1, ldVct);

			NativeMethods.svdFunc svd = (default(T).ToDataType()) switch
			{
				DataType.RealSingle => NativeMethods.LAPACKE_sgesvd,
				DataType.RealDouble => NativeMethods.LAPACKE_dgesvd,
				DataType.ComplexSingle => NativeMethods.LAPACKE_cgesvd,
				DataType.ComplexDouble => NativeMethods.LAPACKE_zgesvd,
				_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
			};
			using var superB = Storage<T>.Create(Math.Min(m, n) - 1, onHost: true);
			var info = svd(MklSolverLayout.ColMajor, jobu.ToChar(), jobvt.ToChar(), m, n, A, lda, S, U, ldu, Vct, ldVct, superB);
			CheckInfo(info, MethodKind.SVD, n);
		}
		#endregion

		#region Schur
		// TODO : add attributes to tell the compiler that the callback functions will only be invoked by unmanaged codes
		public int SchurDecomposition<T>(int n, Storage<T> A, int lda, Storage<T> U, int ldu, EigMode jobu, DoubleComplex[] orderVal = null) where T : struct, IComparable<T>
		{
			// constraints
			ldu = Math.Max(1, ldu);
			// check data type
			var datatype = default(T).ToDataType();
			if (!datatype.IsFloat() || !(datatype.Bytes() == DataType.RealSingle.Bytes() || datatype.Bytes() == DataType.RealDouble.Bytes()))
				throw new NotSupportedException(Resource.DataTypeNotSupport);

			int sdim = 0;
			// real case
			if (datatype.IsReal())
			{
				T[] wr = new T[n], wi = new T[n];
				// shortcut
				if (orderVal is null)
				{
					NativeMethods.realSchurFunc<T> schurFuncShortcut = datatype.Bytes() == DataType.RealSingle.Bytes() ?
									new NativeMethods.realSchurFunc<float>(NativeMethods.LAPACKE_sgees) as NativeMethods.realSchurFunc<T> :
									new NativeMethods.realSchurFunc<double>(NativeMethods.LAPACKE_dgees) as NativeMethods.realSchurFunc<T>;
					int info = schurFuncShortcut(MklSolverLayout.ColMajor, jobu.ToChar(), (sbyte)'N', null, n, A, lda, ref sdim, wr, wi, U, ldu);
					CheckInfo(info, MethodKind.Schur, n);
					return n;
				}
				// get callback function and calculate
				NativeMethods.schurCallbackReal<T> callbackFunc;
				NativeMethods.realSchurFunc<T> schurFunc;
				if (datatype.Bytes() == DataType.RealSingle.Bytes())
				{
					int callback(ref float r, ref float i)
					{
						var val = new DoubleComplex(r, i);
						var find = orderVal.ApproxIndexOfSingle(val);
						return find + 1;
					}
					callbackFunc = new NativeMethods.schurCallbackReal<float>(callback) as NativeMethods.schurCallbackReal<T>;
					schurFunc = new NativeMethods.realSchurFunc<float>(NativeMethods.LAPACKE_sgees) as NativeMethods.realSchurFunc<T>;
				}
				else
				{
					int callback(ref double r, ref double i)
					{
						var val = new DoubleComplex(r, i);
						var find = orderVal.ApproxIndexOfDouble(val);
						return find + 1;
					}
					callbackFunc = new NativeMethods.schurCallbackReal<double>(callback) as NativeMethods.schurCallbackReal<T>;
					schurFunc = new NativeMethods.realSchurFunc<double>(NativeMethods.LAPACKE_dgees) as NativeMethods.realSchurFunc<T>;
				}
				int infoFinal = schurFunc(MklSolverLayout.ColMajor, jobu.ToChar(), (sbyte)'S', callbackFunc, n, A, lda, ref sdim, wr, wi, U, ldu);
				CheckInfo(infoFinal, MethodKind.Schur, n);
				return sdim;
			}
			// complex case
			else
			{
				T[] w = new T[n];
				// shortcut
				if (orderVal is null)
				{
					NativeMethods.compSchurFunc<T> schurFuncShortcut = datatype.Bytes() == DataType.ComplexSingle.Bytes() ?
									new NativeMethods.compSchurFunc<FloatComplex>(NativeMethods.LAPACKE_cgees) as NativeMethods.compSchurFunc<T> :
									new NativeMethods.compSchurFunc<DoubleComplex>(NativeMethods.LAPACKE_zgees) as NativeMethods.compSchurFunc<T>;
					int info = schurFuncShortcut(MklSolverLayout.ColMajor, jobu.ToChar(), (sbyte)'N', null, n, A, lda, ref sdim, w, U, ldu);
					CheckInfo(info, MethodKind.Schur, n);
					return n;
				}
				// get callback function and calculate
				NativeMethods.schurCallbackComp<T> callbackFunc;
				NativeMethods.compSchurFunc<T> schurFunc;
				if (datatype.Bytes() == DataType.ComplexSingle.Bytes())
				{
					int callback(ref FloatComplex v)
					{
						DoubleComplex val = v;
						var find = orderVal.ApproxIndexOfSingle(val);
						return find + 1;
					}
					callbackFunc = new NativeMethods.schurCallbackComp<FloatComplex>(callback) as NativeMethods.schurCallbackComp<T>;
					schurFunc = new NativeMethods.compSchurFunc<FloatComplex>(NativeMethods.LAPACKE_cgees) as NativeMethods.compSchurFunc<T>;
				}
				else
				{
					int callback(ref DoubleComplex val)
					{
						var find = orderVal.ApproxIndexOfDouble(val);
						return find + 1;
					}
					callbackFunc = new NativeMethods.schurCallbackComp<DoubleComplex>(callback) as NativeMethods.schurCallbackComp<T>;
					schurFunc = new NativeMethods.compSchurFunc<DoubleComplex>(NativeMethods.LAPACKE_zgees) as NativeMethods.compSchurFunc<T>;
				}
				int infoFinal = schurFunc(MklSolverLayout.ColMajor, jobu.ToChar(), (sbyte)'S', callbackFunc, n, A, lda, ref sdim, w, U, ldu);
				CheckInfo(infoFinal, MethodKind.Schur, n);
				return sdim;
			}
		}
		#endregion
	}
}
