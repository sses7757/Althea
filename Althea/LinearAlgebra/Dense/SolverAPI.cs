using System;

using Althea.NativeTypes;


namespace Althea.LinearAlgebra.Dense
{
	/// <summary>
	/// The abstract class for runtime dense linear algebra API routines 
	/// </summary>
	public abstract partial class AbstractApi : AbstractRuntimeApi
	{
		#region eigen-problems
		/// <summary>
		/// When implemented by a derived class, calculate the eigenvalues (and eigenvectors) of given hermitian matrix <paramref name="A"/> for the special eigen-problem.
		/// </summary>
		/// <param name="mode">The <see cref="SolveVectorMode"/> to indicate which eigenvectors should be calculated, any value other than <see cref="SolveVectorMode.NoVector"/> will be regarded as <see cref="SolveVectorMode.Vector"/></param>
		/// <typeparam name="T">Any unmanaged struct as the input data type</typeparam>
		/// <typeparam name="TReal">Any unmanaged struct as the real corresponding type of <typeparamref name="T"/></typeparam>
		/// <param name="n">The number of rows and columns of <paramref name="A"/> of type <typeparamref name="T"/></param>
		/// <param name="valOut">The preallocated output eigenvalues of type <typeparamref name="TReal"/></param>
		/// <param name="A">The input/output hermitian matrix to calculate the special eigen-problem; destroyed during the calculation if <paramref name="mode"/> is <see cref="SolveVectorMode.NoVector"/> or replaced by the eigenvectors otherwise.</param>
		/// <param name="lda">The leading dimension of <paramref name="A"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="valOut"/> or <paramref name="A"/> is null or invalid</exception>
		/// <exception cref="TypeMismatchException">If <typeparamref name="TReal"/> is not a real type correspondence of <typeparamref name="T"/></exception>
		public abstract void EigenSpecialMatrixHermitian<T, TReal>(SolveVectorMode mode, long n, Storage<TReal> valOut, Storage<T> A, long lda) where T : unmanaged where TReal : unmanaged;

		/// <summary>
		/// When implemented by a derived class, calculate the eigenvalues (and eigenvectors) of given hermitian matrix pair <paramref name="A"/>, <paramref name="B"/> for the general eigen-problem.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the input data type</typeparam>
		/// <typeparam name="TReal">Any unmanaged struct as the real corresponding type of <typeparamref name="T"/></typeparam>
		/// <param name="mode">The <see cref="SolveVectorMode"/> to indicate which eigenvectors should be calculated, any value other than <see cref="SolveVectorMode.NoVector"/> will be regarded as <see cref="SolveVectorMode.Vector"/></param>
		/// <param name="type">The <see cref="GeneralEigenType"/> to indicate positions of <paramref name="A"/> and <paramref name="B"/></param>
		/// <param name="n">The number of rows and columns of <paramref name="A"/> and <paramref name="B"/></param>
		/// <param name="valOut">The preallocated output eigenvalues of type <typeparamref name="TReal"/></param>
		/// <param name="A">The input/output hermitian matrix to calculate the general eigen-problem; destroyed during the calculation if <paramref name="mode"/> is <see cref="SolveVectorMode.NoVector"/>; or replaced by the eigenvectors otherwise.</param>
		/// <param name="lda">The leading dimension of <paramref name="A"/></param>
		/// <param name="B">Another input hermitian matrix to calculate the general eigen-problem</param>
		/// <param name="ldb">The leading dimension of <paramref name="B"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="valOut"/> or <paramref name="A"/> or <paramref name="B"/> is null or invalid</exception>
		/// <exception cref="TypeMismatchException">If <typeparamref name="TReal"/> is not a real type correspondence of <typeparamref name="T"/></exception>
		public abstract void EigenGeneralMatrixHermitian<T, TReal>(GeneralEigenType type, SolveVectorMode mode, long n, Storage<TReal> valOut, Storage<T> A, long lda, Storage<T> B, long ldb) where T : unmanaged where TReal : unmanaged;

		/// <summary>
		/// When implemented by a derived class, calculate the eigenvalues (and eigenvectors) of given general matrix <paramref name="A"/> for the special eigen-problem.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the input data type</typeparam>
		/// <typeparam name="TComplex">Any unmanaged struct as the complex corresponding type of <typeparamref name="T"/></typeparam>
		/// <param name="mode">The <see cref="SolveVectorMode"/> to indicate which eigenvectors should be calculated</param>
		/// <param name="n">The number of rows and columns of <paramref name="A"/></param>
		/// <param name="valOut">The preallocated output eigenvalues of type <typeparamref name="TComplex"/></param>
		/// <param name="leftVec">The preallocated output left eigenvectors of type <typeparamref name="TComplex"/>, can be null if <paramref name="mode"/> does not indicate the output of left eigenvectors.</param>
		/// <param name="ldvl">The leading dimension of <paramref name="leftVec"/></param>
		/// <param name="ldvr">The leading dimension of <paramref name="rightVec"/></param>
		/// <param name="rightVec">The preallocated output right eigenvectors of type <typeparamref name="TComplex"/>, can be null if <paramref name="mode"/> does not indicate the output of left eigenvectors.</param>
		/// <param name="A">The input general matrix to calculate the special eigen-problem of a </param>
		/// <param name="lda">leading dimension of <paramref name="A"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="valOut"/> or <paramref name="A"/> is null or invalid</exception>
		/// <exception cref="TypeMismatchException">If <typeparamref name="TComplex"/> is not a complex type correspondence of <typeparamref name="T"/></exception>
		public abstract void EigenSpecialMatrixGeneral<T, TComplex>(SolveVectorMode mode, long n, Storage<TComplex> valOut, Storage<TComplex>? leftVec, long ldvl, Storage<TComplex>? rightVec, long ldvr, Storage<T> A, long lda) where T : unmanaged where TComplex : unmanaged, ICustomNativeType<TComplex>;

		/// <summary>
		/// When implemented by a derived class, calculate the eigenvalues (and eigenvectors) of given general matrix pair <paramref name="A"/>, <paramref name="B"/> for the general eigen-problem.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the input data type</typeparam>
		/// <typeparam name="TComplex">Any unmanaged struct as the complex corresponding type of <typeparamref name="T"/></typeparam>
		/// <param name="type">The <see cref="GeneralEigenType"/> to indicate positions of <paramref name="A"/> and <paramref name="B"/></param>
		/// <param name="mode">The <see cref="SolveVectorMode"/> to indicate which eigenvectors should be calculated</param>
		/// <param name="n">The number of rows and columns of <paramref name="A"/> and <paramref name="B"/></param>
		/// <param name="valOut">The output eigenvalues, must be preallocated, of corresponding complex type</param>
		/// <param name="leftVec">The output left eigenvectors, must be preallocated, of corresponding complex type</param>
		/// <param name="ldvl">The leading dimension of <paramref name="leftVec"/></param>
		/// <param name="ldvr">The leading dimension of <paramref name="rightVec"/></param>
		/// <param name="rightVec">The output right eigenvectors, must be preallocated, of corresponding complex type</param>
		/// <param name="A">The input general matrix to calculate eigensystem</param>
		/// <param name="lda">The leading dimension of <paramref name="A"/></param>
		/// <param name="B">The input general matrix to calculate general eigen-problem; if <c><paramref name="B"/> is null</c>, the normal eigen is performed and <paramref name="type"/> is not used; otherwise, the general one is performed</param>
		/// <param name="ldb">The leading dimension of <paramref name="B"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="valOut"/> or <paramref name="A"/> or <paramref name="B"/> is null or invalid</exception>
		/// <exception cref="TypeMismatchException">If <typeparamref name="TComplex"/> is not a complex type correspondence of <typeparamref name="T"/></exception>
		public abstract void EigenGeneralMatrixGeneral<T, TComplex>(GeneralEigenType type, SolveVectorMode mode, long n, Storage<TComplex> valOut, Storage<TComplex>? leftVec, long ldvl, Storage<TComplex>? rightVec, long ldvr, Storage<T> A, long lda, Storage<T> B, long ldb) where T : unmanaged where TComplex : unmanaged, ICustomNativeType<TComplex>;
		#endregion

		#region SVD
		/// <summary>
		/// When implemented by a derived class, compute the singular value decomposition (SVD) of a matrix <paramref name="A"/> and corresponding the left and/or right singular vectors: <paramref name="A"/> = <paramref name="U"/> <paramref name="S"/> <paramref name="Vct"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the input data type</typeparam>
		/// <typeparam name="TReal">Any unmanaged struct as the real corresponding type of <typeparamref name="T"/></typeparam>
		/// <param name="storeU">The <see cref="SVDStore"/> to specify options for computing all or part of the matrix <paramref name="U"/></param>
		/// <param name="storeV">The <see cref="SVDStore"/> to specify options for computing all or part of the matrix <paramref name="Vct"/></param>
		/// <param name="m">The number of rows of matrix</param>
		/// <param name="n">The number of columns of matrix</param>
		/// <param name="A">The input (and possible output) matrix of size <paramref name="m"/>×<paramref name="n"/> and leading dimension <paramref name="lda"/>. Will be overwritten by singular vectors if <paramref name="storeU"/> or <paramref name="storeV"/> is <see cref="SVDStore.Overwrite"/>.</param>
		/// <param name="lda">The leading dimension of <paramref name="A"/></param>
		/// <param name="S">The preallocated output singular values as a vector of size at least <c>min(<paramref name="m"/>, <paramref name="n"/>)</c></param>
		/// <param name="U">The preallocated output left unitary matrix with size <paramref name="ldu"/>×<paramref name="m"/>, can be null if <paramref name="storeU"/> is <see cref="SVDStore.Overwrite"/> or <see cref="SVDStore.Overwrite"/>.</param>
		/// <param name="ldu">The leading dimension of <paramref name="U"/></param>
		/// <param name="Vct">The preallocated output right unitary ("ct" for conjugate transpose) matrix with size <paramref name="ldvct"/>×<paramref name="n"/>, can be null if <paramref name="storeV"/> is <see cref="SVDStore.Overwrite"/> or <see cref="SVDStore.Overwrite"/>.</param>
		/// <param name="ldvct">The leading dimension of <paramref name="Vct"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="S"/> or <paramref name="A"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="storeU"/> and <paramref name="storeV"/> are both <see cref="SVDStore.Overwrite"/></exception>
		/// <exception cref="TypeMismatchException">If <typeparamref name="TReal"/> is not a real type correspondence of <typeparamref name="T"/></exception>
		public abstract void SingularValues<T, TReal>(SVDStore storeU, SVDStore storeV, long m, long n, Storage<T> A, long lda, Storage<TReal> S, Storage<T>? U, long ldu, Storage<T>? Vct, long ldvct) where T : unmanaged where TReal : unmanaged;
		#endregion

		#region linear solve
		/// <summary>
		/// When implemented by a derived class, compute the LU decomposition of <paramref name="A"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="n">The number of rows and columns of matrix <paramref name="A"/></param>
		/// <param name="A">The input/output matrix; will be overwritten by its LU decomposition after exit.</param>
		/// <param name="lda">The leading dimension of <paramref name="A"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> is null or invalid</exception>
		public abstract void LuDecomposition<T>(long n, Storage<T> A, long lda) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, solve a series of linear systems: <paramref name="A"/> X = <paramref name="B"/>. Where each column pair of X and <paramref name="B"/> is a linear system. <br/>
		/// Both <paramref name="A"/> and <paramref name="B"/> are in-place: <paramref name="A"/> will be replaced by its LU decomposition and <paramref name="B"/> the solution X.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="n">The number of rows and columns of matrix <paramref name="A"/></param>
		/// <param name="nrhs">The number of right-hand sides, a.k.a. the number of linear systems.</param>
		/// <param name="A">The input/output coefficient matrix; will be overwritten by its LU decomposition after exit.</param>
		/// <param name="lda">The leading dimension of <paramref name="A"/></param>
		/// <param name="B">The input/output matrix whose each column is a vector at right-hand side; will be overwritten by solution X after exit.</param>
		/// <param name="ldb">The leading dimension of <paramref name="B"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="B"/> is null or invalid</exception>
		public abstract void LinearSolve<T>(long n, long nrhs, Storage<T> A, long lda, Storage<T> B, long ldb) where T : unmanaged;
		#endregion

		#region other decompositions
		/// <summary>
		/// When implemented by a derived class, compute the QR factorization the given matrix <paramref name="A"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="full">Whether to perform full factorization or not</param>
		/// <param name="m">The number of rows of matrix <paramref name="A"/></param>
		/// <param name="n">The number of columns of matrix <paramref name="A"/></param>
		/// <param name="A">The input/output matrix to be factorized of leading dimension <paramref name="lda"/> and size <paramref name="m"/>×<paramref name="n"/>. <br/>
		/// If <paramref name="m"/> ≥ <paramref name="n"/> and <paramref name="full"/> is false, all of it will be overwritten by the unitary matrix at exit;<br/>
		/// If <paramref name="m"/> &gt; <paramref name="n"/> and <paramref name="full"/> is true, <paramref name="A"/>'s underlying array must actually contains at least <paramref name="m"/> columns for the overwriting to succeed;<br/>
		/// Otherwise, only the top-left <paramref name="m"/>×<paramref name="m"/> matrix will be overwritten.</param>
		/// <param name="lda">The leading dimension of <paramref name="A"/></param>
		/// <param name="tri">The preallocated output triangular matrix of leading dimension <paramref name="ldt"/> and size min(<paramref name="m"/>, <paramref name="n"/>) × <paramref name="n"/></param>
		/// <param name="ldt">The leading dimension of <paramref name="tri"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="tri"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="m"/> &gt; <paramref name="n"/> and <paramref name="full"/> is true while <paramref name="A"/> do not contain enough space to be overwritten</exception>
		public abstract void QRDecomposition<T>(bool full, long m, long n, Storage<T> A, long lda, Storage<T> tri, long ldt) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, compute the Schur decomposition of given matrix <paramref name="A"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the input/output data type</typeparam>
		/// <typeparam name="TComplex">Any unmanaged struct as the complex corresponding type of <typeparamref name="T"/></typeparam>
		/// <param name="jobu">The <see cref="SolveVectorMode"/> to indicate whether to calculate Schur vectors or not. Any value other than <see cref="SolveVectorMode.NoVector"/> will be regarded as <see cref="SolveVectorMode.Vector"/>.</param>
		/// <param name="n">The number of rows and columns of matrix <paramref name="A"/></param>
		/// <param name="A">The input/output matrix to be decomposed of leading dimension <paramref name="lda"/> and size <paramref name="n"/>×<paramref name="n"/>, overwritten by the triangular matrix at exit</param>
		/// <param name="lda">The leading dimension of <paramref name="A"/></param>
		/// <param name="U">The preallocated output Schur vectors of leading dimension <paramref name="ldu"/> and size <paramref name="n"/>×<paramref name="n"/>, can be null if <paramref name="jobu"/> is <see cref="SolveVectorMode.NoVector"/>.</param>
		/// <param name="ldu">The leading dimension of <paramref name="U"/></param>
		/// <param name="orderVal">The eigenvalues in this array will be selected to at the top left of Schur form. Default null means no particular order is preferred.</param>
		/// <returns>The actual number of eigenvalues returned</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="orderVal"/> has duplicate values or its length is larger than <paramref name="n"/></exception>
		/// <exception cref="TypeMismatchException">If <typeparamref name="TComplex"/> is not a complex type correspondence of <typeparamref name="T"/></exception>
		public abstract long SchurDecomposition<T, TComplex>(SolveVectorMode jobu, long n, Storage<T> A, long lda, Storage<T>? U, long ldu, Storage<TComplex> orderVal = null) where T : unmanaged where TComplex : unmanaged, ICustomNativeType<TComplex>;
		#endregion
	}
}
