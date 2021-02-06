using System;

using Althea.NativeTypes;


namespace Althea.LinearAlgebra.Dense
{
	/// <summary>
	/// The abstract class for runtime linear algebra API routines 
	/// </summary>
	public abstract partial class AbstractApi : AbstractRuntimeApi
	{
		#region eigen-problem
		/// <summary>
		/// When implemented by a derived class, Calculate the eigenvalues (and eigenvectors) of given Hermitian matrix <paramref name="A"/> for the special eigen-problem.
		/// </summary>
		/// <param name="mode">The <see cref="EigenSolveMode"/> to indicate whether the eigenvectors should be calculated</param>
		/// <typeparam name="T">Any unmanaged struct as the input data type</typeparam>
		/// <typeparam name="TReal">Any unmanaged struct as the real corresponding type of <typeparamref name="T"/></typeparam>
		/// <param name="n">The number of rows and columns of <paramref name="A"/> of type <typeparamref name="T"/></param>
		/// <param name="valOut">The preallocated output eigenvalues of type <typeparamref name="TReal"/></param>
		/// <param name="A">The input/output matrix to calculate eigensystem; destroyed during the calculation if <paramref name="mode"/> is <see cref="EigenSolveMode.NoVector"/> or replaced by the eigenvectors otherwise</param>
		/// <param name="lda">The leading dimension of <paramref name="A"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="valOut"/> or <paramref name="A"/> is null or invalid</exception>
		public abstract void EigenSpecialHermitianMatrix<T, TReal>(EigenSolveMode mode, ulong n, Storage<TReal> valOut, Storage<T> A, ulong lda) where T : unmanaged, IEquatable<T> where TReal : unmanaged, IEquatable<TReal>;

		/// <summary>
		/// Calculate the eigenvalues (and eigenvectors) of given Hermitian matrix pair <paramref name="A"/>, <paramref name="B"/> for the general one.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the input data type</typeparam>
		/// <typeparam name="TReal">Any unmanaged struct as the real corresponding type of <typeparamref name="T"/></typeparam>
		/// <param name="mode">The <see cref="EigenSolveMode"/> to indicate whether the eigenvectors should be calculated</param>
		/// <param name="eigType">The <see cref="GeneralEigenType"/> to indicate positions of <paramref name="A"/> and <paramref name="B"/></param>
		/// <param name="n">The number of rows and columns of <paramref name="A"/> and <paramref name="B"/></param>
		/// <param name="valOut">The preallocated output eigenvalues of type <typeparamref name="TReal"/></param>
		/// <param name="A">The input/output matrix to calculate eigensystem; destroyed during the calculation if <paramref name="mode"/> is <see cref="EigenSolveMode.NoVector"/> or replaced by the eigenvectors otherwise</param>
		/// <param name="lda">The leading dimension of <paramref name="A"/></param>
		/// <param name="B">The input matrix to calculate general eigen-problem</param>
		/// <param name="ldb">The leading dimension of <paramref name="B"/>d</param>
		public abstract void EigenGeneralHermitianMatrix<T, TReal>(GeneralEigenType eigType, EigenSolveMode mode, ulong n, Storage<TReal> valOut, Storage<T> A, ulong lda, Storage<T> B, ulong ldb) where T : unmanaged where TReal : unmanaged;

		/// <summary>
		/// Calculate the eigenvalues (and eigenvectors) of given general matrix <paramref name="A"/> for the special eigen-problem.
		/// </summary>
		/// <param name="n">number of rows and columns of <paramref name="A"/></param>
		/// <param name="valOut">the output eigenvalues, must be preallocated, of corresponding complex type</param>
		/// <param name="leftVec">the output left eigenvectors, must be preallocated, of corresponding complex type</param>
		/// <param name="ldvl">the leading dimension of <paramref name="leftVec"/></param>
		/// <param name="ldvr">the leading dimension of <paramref name="rightVec"/></param>
		/// <param name="rightVec">the output right eigenvectors, must be preallocated, of corresponding complex type</param>
		/// <param name="A">the input/output matrix to calculate eigensystem; destroyed during the calculation if <paramref name="mode"/> is <see cref="EigenSolveMode.NoVector"/> or replaced by the eigenvectors otherwise</param>
		/// <param name="lda">leading dimension of <paramref name="A"/></param>
		/// <param name="mode">the <see cref="EigenSolveMode"/> to indicate whether the eigenvectors should be calculated</param>
		public abstract void EigenSpecialGeneralMatrix<T, TComplex>(int n, Storage<TComplex> valOut, Storage<TComplex> leftVec, int ldvl, Storage<TComplex> rightVec, int ldvr, Storage<T> A, int lda, EigenSolveMode mode) where T : unmanaged where TComplex : unmanaged;

		/// <summary>
		/// Calculate the eigenvalues (and eigenvectors) of given general matrix pair <paramref name="A"/>, <paramref name="B"/> for the general one.
		/// </summary>
		/// <param name="n">number of rows and columns of <paramref name="A"/> and <paramref name="B"/></param>
		/// <param name="valOut">the output eigenvalues, must be preallocated, of corresponding complex type</param>
		/// <param name="leftVec">the output left eigenvectors, must be preallocated, of corresponding complex type</param>
		/// <param name="ldvl">the leading dimension of <paramref name="leftVec"/></param>
		/// <param name="ldvr">the leading dimension of <paramref name="rightVec"/></param>
		/// <param name="rightVec">the output right eigenvectors, must be preallocated, of corresponding complex type</param>
		/// <param name="A">the input/output matrix to calculate eigensystem; destroyed during the calculation if <paramref name="mode"/> is <see cref="EigenSolveMode.NoVector"/> or replaced by the eigenvectors otherwise</param>
		/// <param name="lda">leading dimension of <paramref name="A"/></param>
		/// <param name="B">the input matrix to calculate general eigen-problem; if <c><paramref name="B"/> is null</c>, the normal eigen is performed and <paramref name="eigType"/> is not used; otherwise, the general one is performed</param>
		/// <param name="ldb">leading dimension of <paramref name="B"/>d</param>
		/// <param name="mode">the <see cref="EigenSolveMode"/> to indicate whether the eigenvectors should be calculated</param>
		/// <param name="eigType">the <see cref="GeneralEigenType"/> to indicate positions of <paramref name="A"/> and <paramref name="B"/></param>
		public abstract void EigenGeneralGeneralMatrix<T, TComplex>(int n, Storage<TComplex> valOut, Storage<TComplex> leftVec, int ldvl, Storage<TComplex> rightVec, int ldvr, Storage<T> A, int lda, Storage<T> B, int ldb, GeneralEigenType eigType, EigenSolveMode mode) where T : unmanaged where TComplex : unmanaged;
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
		public abstract void SingularValues<T, TReal>(SVDStore jobu, SVDStore jobvt, int m, int n, Storage<T> A, int lda, Storage<TReal> S, Storage<T> U, int ldu, Storage<T> Vct, int ldVct) where T : unmanaged where TReal : unmanaged;
		#endregion

		#region solve
		/// <summary>
		/// Solve a series of linear systems: $A X = B$, where each column pair of X and <paramref name="B"/> is a linear system. <br/>
		/// Both <paramref name="A"/> and <paramref name="B"/> are in-place where <paramref name="A"/> is replaced by its LU decomposition and <paramref name="B"/> the solution X.
		/// </summary>
		/// <param name="n">number of rows and columns of matrix <paramref name="A"/></param>
		/// <param name="nrhs">number of right-hand sides.</param>
		/// <param name="A">the coefficient matrix</param>
		/// <param name="lda">leading dimension of <paramref name="A"/></param>
		/// <param name="B">each column of this matrix is the vector at right; overwritten by solution X in the end</param>
		/// <param name="ldb">leading dimension of <paramref name="B"/></param>
		public abstract void LinearSolve<T>(int n, int nrhs, Storage<T> A, int lda, Storage<T> B, int ldb) where T : unmanaged;
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
		/// <param name="tri">the output triangular matrix</param>
		/// <param name="ldt">leading dimension of <paramref name="tri"/></param>
		public abstract void QRDecomposition<T>(bool full, int m, int n, Storage<T> A, int lda, Storage<T> tri, int ldt) where T : unmanaged;

		/// <summary>
		/// Compute the Schur decomposition of given matrix <paramref name="A"/>.
		/// </summary>
		/// <param name="n">number of rows and columns of matrix <paramref name="A"/></param>
		/// <param name="A">matrix to be decomposed, overwritten by the triangular matrix at exit</param>
		/// <param name="lda">leading dimension of <paramref name="A"/></param>
		/// <param name="U">the output unitary matrix</param>
		/// <param name="ldu">leading dimension of <paramref name="U"/></param>
		/// <param name="jobu">calculate Schur vectors or not</param>
		/// <param name="orderVal">the value order of the factorization so that selected eigenvalues are at the top left of Schur form. Default null means use default order</param>
		/// <returns>the actual number of eigenvalues returned</returns>
		public abstract int SchurDecomposition<T, TComplex>(int n, Storage<T> A, int lda, Storage<T> U, int ldu, EigenSolveMode jobu, TComplex[] orderVal = null) where T : unmanaged where TComplex : unmanaged;
		#endregion
	}
}
