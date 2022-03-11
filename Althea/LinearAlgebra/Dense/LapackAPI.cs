using System;

using Althea.Helpers;
using Althea.NativeTypes;
using Althea.Storage;

using Althea.SourceGenerator;


namespace Althea.LinearAlgebra.Dense
{
	public abstract partial class AbstractApi : AbstractRuntimeApi<AbstractApi>
	{
		#region eigen-problems
		/// <summary>
		/// When implemented by a derived class, calculate the eigenvalues (and eigenvectors) of given hermitian matrix <paramref name="A"/> for the special eigen-problem.
		/// </summary>
		/// <param name="mode">The <see cref="SolveVectorMode"/> to indicate which eigenvectors should be calculated, any value other than <see cref="SolveVectorMode.NoVector"/> will be regarded as <see cref="SolveVectorMode.Vector"/></param>
		/// <typeparam name="T">Any unmanaged number as the input data type</typeparam>
		/// <typeparam name="TReal">Any unmanaged number as the real corresponding type of <typeparamref name="T"/></typeparam>
		/// <typeparam name="TS">The actual storage type that implements <see cref="IStorage{T, TSelf}"/> of data type <typeparamref name="T"/></typeparam>
		/// <typeparam name="TSReal">The actual storage type that implements <see cref="IStorage{T, TSelf}"/> of data type <typeparamref name="TReal"/></typeparam>
		/// <param name="n">The number of rows and columns of <paramref name="A"/> of type <typeparamref name="T"/></param>
		/// <param name="valOut">The preallocated output eigenvalues of type <typeparamref name="TReal"/></param>
		/// <param name="A">The input/output hermitian matrix to calculate the special eigen-problem; destroyed during the calculation if <paramref name="mode"/> is <see cref="SolveVectorMode.NoVector"/> or replaced by the eigenvectors otherwise.</param>
		/// <param name="lda">The leading dimension of <paramref name="A"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="valOut"/> or <paramref name="A"/> is null or invalid</exception>
		/// <exception cref="TypeMismatchException">If <typeparamref name="TReal"/> is not a real type correspondence of <typeparamref name="T"/></exception>
		/// <exception cref="MatrixSolveAlgorithmException">If the internal solver failed due to some reason</exception>
		[AbstractApiMethod]
		public abstract bool EigenStandardMatrixHermitian<T, TReal, TS, TSReal>(SolveVectorMode mode, long n, TSReal valOut, TS A, long lda) where T : unmanaged, IFloatingPoint<T> where TReal : unmanaged, IFloatingPoint<TReal> where TS : class, IStorage<T, TS> where TSReal : class, IStorage<TReal, TSReal>;

		/// <summary>
		/// When implemented by a derived class, calculate the eigenvalues (and eigenvectors) of given symmetric-definite / hermitian-definite matrix pair <paramref name="A"/>, <paramref name="B"/> for the general eigen-problem.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the input data type</typeparam>
		/// <typeparam name="TReal">Any unmanaged number as the real corresponding type of <typeparamref name="T"/></typeparam>
		/// <typeparam name="TS1">The first actual storage type that implements <see cref="IStorage{T, TSelf}"/> of data type <typeparamref name="T"/></typeparam>
		/// <typeparam name="TS2">The second actual storage type that implements <see cref="IStorage{T, TSelf}"/> of data type <typeparamref name="T"/></typeparam>
		/// <typeparam name="TSReal">The actual storage type that implements <see cref="IStorage{T, TSelf}"/> of data type <typeparamref name="TReal"/></typeparam>
		/// <param name="mode">The <see cref="SolveVectorMode"/> to indicate which eigenvectors should be calculated, any value other than <see cref="SolveVectorMode.NoVector"/> will be regarded as <see cref="SolveVectorMode.Vector"/></param>
		/// <param name="type">The <see cref="GeneralEigenType"/> to indicate positions of <paramref name="A"/> and <paramref name="B"/></param>
		/// <param name="n">The number of rows and columns of <paramref name="A"/> and <paramref name="B"/></param>
		/// <param name="valOut">The preallocated output eigenvalues of type <typeparamref name="TReal"/></param>
		/// <param name="A">The input/output symmetric/hermitian positive-definite matrix to calculate the general eigen-problem; destroyed during the calculation if <paramref name="mode"/> is <see cref="SolveVectorMode.NoVector"/>; or replaced by the eigenvectors otherwise.</param>
		/// <param name="lda">The leading dimension of <paramref name="A"/></param>
		/// <param name="B">Another input/output symmetric/hermitian positive-definite matrix to calculate the general eigen-problem; may be destroyed during the calculation</param>
		/// <param name="ldb">The leading dimension of <paramref name="B"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="valOut"/> or <paramref name="A"/> or <paramref name="B"/> is null or invalid</exception>
		/// <exception cref="TypeMismatchException">If <typeparamref name="TReal"/> is not a real type correspondence of <typeparamref name="T"/></exception>
		/// <exception cref="MatrixSolveAlgorithmException">If the internal solver failed due to some reason</exception>
		[AbstractApiMethod]
		public abstract bool EigenGeneralMatrixHermitian<T, TReal, TS1, TS2, TSReal>(GeneralEigenType type, SolveVectorMode mode, long n, TSReal valOut, TS1 A, long lda, TS2 B, long ldb) where T : unmanaged, IFloatingPoint<T> where TReal : unmanaged, IFloatingPoint<TReal> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TSReal : class, IStorage<TReal, TSReal>;

		/// <summary>
		/// When implemented by a derived class, calculate the eigenvalues (and eigenvectors) of given general matrix <paramref name="A"/> for the special eigen-problem.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the input data type</typeparam>
		/// <typeparam name="TComp">Any unmanaged number as the complex corresponding type of <typeparamref name="T"/></typeparam>
		/// <typeparam name="TS">The actual storage type that implements <see cref="IStorage{T, TSelf}"/> of data type <typeparamref name="T"/></typeparam>
		/// <typeparam name="TSComp1">The first actual storage type that implements <see cref="IStorage{T, TSelf}"/> of data type <typeparamref name="TComp"/></typeparam>
		/// <typeparam name="TSComp2">The second actual storage type that implements <see cref="IStorage{T, TSelf}"/> of data type <typeparamref name="TComp"/></typeparam>
		/// <typeparam name="TSComp3">The third actual storage type that implements <see cref="IStorage{T, TSelf}"/> of data type <typeparamref name="TComp"/></typeparam>
		/// <param name="mode">The <see cref="SolveVectorMode"/> to indicate which eigenvectors should be calculated</param>
		/// <param name="n">The number of rows and columns of <paramref name="A"/></param>
		/// <param name="valOut">The preallocated output eigenvalues of type <typeparamref name="TComp"/></param>
		/// <param name="leftVec">The preallocated output left eigenvectors of type <typeparamref name="TComp"/>, can be null if <paramref name="mode"/> does not indicate the output of left eigenvectors.</param>
		/// <param name="ldvl">The leading dimension of <paramref name="leftVec"/></param>
		/// <param name="ldvr">The leading dimension of <paramref name="rightVec"/></param>
		/// <param name="rightVec">The preallocated output right eigenvectors of type <typeparamref name="TComp"/>, can be null if <paramref name="mode"/> does not indicate the output of left eigenvectors.</param>
		/// <param name="A">The input general matrix to calculate the special eigen-problem of a </param>
		/// <param name="lda">leading dimension of <paramref name="A"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="valOut"/> or <paramref name="A"/> is null or invalid</exception>
		/// <exception cref="TypeMismatchException">If <typeparamref name="TComp"/> is not a complex type correspondence of <typeparamref name="T"/></exception>
		/// <exception cref="MatrixSolveAlgorithmException">If the internal solver failed due to some reason</exception>
		[AbstractApiMethod]
		public abstract bool EigenStandardMatrixGeneral<T, TComp, TS, TSComp1, TSComp2, TSComp3>(SolveVectorMode mode, long n, TSComp1 valOut, TSComp2? leftVec, long ldvl, TSComp3? rightVec, long ldvr, TS A, long lda) where T : unmanaged, IFloatingPoint<T> where TComp : unmanaged, IComplexFloatNumber<TComp> where TS : class, IStorage<T, TS> where TSComp1 : class, IStorage<TComp, TSComp1> where TSComp2 : class, IStorage<TComp, TSComp2> where TSComp3 : class, IStorage<TComp, TSComp3>;

		/// <summary>
		/// When implemented by a derived class, calculate the eigenvalues (and eigenvectors) of given general matrix pair <paramref name="A"/>, <paramref name="B"/> for the general eigen-problem. The output eigenvalues are separated to prevent possible over- or under- flow.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the input data type</typeparam>
		/// <typeparam name="TComp">Any unmanaged number as the complex corresponding type of <typeparamref name="T"/></typeparam>
		/// <typeparam name="TS">The actual storage type that implements <see cref="IStorage{T, TSelf}"/> of data type <typeparamref name="T"/></typeparam>
		/// <typeparam name="TSComp1">The first actual storage type that implements <see cref="IStorage{T, TSelf}"/> of data type <typeparamref name="TComp"/></typeparam>
		/// <typeparam name="TSComp2">The second actual storage type that implements <see cref="IStorage{T, TSelf}"/> of data type <typeparamref name="TComp"/></typeparam>
		/// <typeparam name="TSComp3">The third actual storage type that implements <see cref="IStorage{T, TSelf}"/> of data type <typeparamref name="TComp"/></typeparam>
		/// <param name="type">The <see cref="GeneralEigenType"/> to indicate positions of <paramref name="A"/> and <paramref name="B"/></param>
		/// <param name="mode">The <see cref="SolveVectorMode"/> to indicate which eigenvectors should be calculated</param>
		/// <param name="n">The number of rows and columns of <paramref name="A"/> and <paramref name="B"/></param>
		/// <param name="valOut">The preallocated output eigenvalues of type <typeparamref name="TComp"/></param>
		/// <param name="leftVec">The output left eigenvectors, must be preallocated, of corresponding complex type</param>
		/// <param name="ldvl">The leading dimension of <paramref name="leftVec"/></param>
		/// <param name="ldvr">The leading dimension of <paramref name="rightVec"/></param>
		/// <param name="rightVec">The output right eigenvectors, must be preallocated, of corresponding complex type</param>
		/// <param name="A">The input general matrix to calculate eigensystem</param>
		/// <param name="lda">The leading dimension of <paramref name="A"/></param>
		/// <param name="B">Another input/output matrix to calculate the general eigen-problem; may be destroyed during the calculation</param>
		/// <param name="ldb">The leading dimension of <paramref name="B"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="valOut"/> or <paramref name="A"/> or <paramref name="B"/> is null or invalid</exception>
		/// <exception cref="TypeMismatchException">If <typeparamref name="TComp"/> is not a complex type correspondence of <typeparamref name="T"/></exception>
		/// <exception cref="MatrixSolveAlgorithmException">If the internal solver failed due to some reason</exception>
		[AbstractApiMethod]
		public abstract bool EigenGeneralMatrixGeneral<T, TComp, TS, TSComp1, TSComp2, TSComp3>(GeneralEigenType type, SolveVectorMode mode, long n, TSComp1 valOut, TSComp2? leftVec, long ldvl, TSComp3? rightVec, long ldvr, TS A, long lda, TS B, long ldb) where T : unmanaged, IFloatingPoint<T> where TComp : unmanaged, IComplexFloatNumber<TComp> where TS : class, IStorage<T, TS> where TSComp1 : class, IStorage<TComp, TSComp1> where TSComp2 : class, IStorage<TComp, TSComp2> where TSComp3 : class, IStorage<TComp, TSComp3>;
		#endregion

		#region linear solve
		/// <summary>
		/// When implemented by a derived class, solve a series of linear systems: <c><paramref name="op"/>(<paramref name="A"/>) * X == <paramref name="B"/></c>. Where each column pair of X and <paramref name="B"/> together with <paramref name="op"/>(<paramref name="A"/>) is a linear system.<br/>
		/// Both <paramref name="A"/> and <paramref name="B"/> are in-place: <paramref name="A"/> may be replaced by its LU decomposition, and <paramref name="B"/> shall be replaced by the solution X.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TInd">Any integral type unmanaged number as the data type</typeparam>
		/// <typeparam name="TS1">The first actual storage type that implements <see cref="IStorage{T, TSelf}"/> of data type <typeparamref name="T"/></typeparam>
		/// <typeparam name="TS2">The second actual storage type that implements <see cref="IStorage{T, TSelf}"/> of data type <typeparamref name="T"/></typeparam>
		/// <param name="op">The <see cref="MatrixOperation"/> indicates the simple operation to the <paramref name="A"/></param>
		/// <param name="n">The number of rows and columns of matrix <paramref name="A"/></param>
		/// <param name="nrhs">The number of right-hand sides, a.k.a. the number of linear systems.</param>
		/// <param name="A">The input/output coefficient matrix; may be overwritten by its LU decomposition after exit.</param>
		/// <param name="lda">The leading dimension of <paramref name="A"/></param>
		/// <param name="B">The input/output matrix whose each column is a vector at right-hand side; will be overwritten by solution X after exit.</param>
		/// <param name="ldb">The leading dimension of <paramref name="B"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="B"/> is null or invalid</exception>
		/// <exception cref="MatrixSolveAlgorithmException">If the internal solver failed due to some reason</exception>
		[AbstractApiMethod]
		public abstract bool LinearSolveGeneral<T, TInd, TS1, TS2>(MatrixOperation op, long n, long nrhs, TS1 A, long lda, TS2 B, long ldb) where T : unmanaged, IFloatingPoint<T> where TInd : unmanaged, IBinaryInteger<TInd> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;
		#endregion

		#region QR solve
		/// <summary>
		/// When implemented by a derived class, compute the complete QR factorization the given matrix <paramref name="A"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS1">The first actual storage type that implements <see cref="IStorage{T, TSelf}"/> of data type <typeparamref name="T"/></typeparam>
		/// <typeparam name="TS2">The second actual storage type that implements <see cref="IStorage{T, TSelf}"/> of data type <typeparamref name="T"/></typeparam>
		/// <param name="full">Whether to perform full factorization or not</param>
		/// <param name="m">The number of rows of matrix <paramref name="A"/></param>
		/// <param name="n">The number of columns of matrix <paramref name="A"/></param>
		/// <param name="A">The input/output matrix to be factorized of leading dimension <paramref name="lda"/> and size <paramref name="m"/>×<paramref name="n"/> whose upper triangular part will be overwritten by the triangular matrix at exit (rest part may be filled with other values).</param>
		/// <param name="lda">The leading dimension of <paramref name="A"/></param>
		/// <param name="Q">The preallocated output unitary matrix of leading dimension <paramref name="ldq"/></param>
		/// <param name="ldq">The leading dimension of <paramref name="Q"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="Q"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="Q"/> do not contain enough space to be overwritten</exception>
		/// <exception cref="MatrixSolveAlgorithmException">If the internal solver failed due to some reason</exception>
		[AbstractApiMethod]
		public abstract bool QRDecomposition<T, TS1, TS2>(bool full, long m, long n, TS1 A, long lda, TS2 Q, long ldq) where T : unmanaged, IFloatingPoint<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

		/// <summary>
		/// When implemented by a derived class, least square solve a series of linear systems: <c><paramref name="A"/> * X == <paramref name="B"/></c>. Where each column pair of X and <paramref name="B"/> together with <paramref name="A"/> is a overdetermined linear system.<br/>
		/// Both <paramref name="A"/> and <paramref name="B"/> are in-place: <paramref name="A"/> may be replaced by its implicit QR decomposition, and <paramref name="B"/> shall be replaced by the solution X.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS1">The first actual storage type that implements <see cref="IStorage{T, TSelf}"/> of data type <typeparamref name="T"/></typeparam>
		/// <typeparam name="TS2">The second actual storage type that implements <see cref="IStorage{T, TSelf}"/> of data type <typeparamref name="T"/></typeparam>
		/// <param name="m">The number of rows of matrix <paramref name="A"/>, must be larger than <paramref name="n"/></param>
		/// <param name="n">The number of columns of matrix <paramref name="A"/></param>
		/// <param name="nrhs">The number of right-hand sides, a.k.a. the number of overdetermined linear systems.</param>
		/// <param name="A">The input/output coefficient matrix; may be overwritten by its implicit QR decomposition after exit.</param>
		/// <param name="lda">The leading dimension of <paramref name="A"/></param>
		/// <param name="B">The input/output matrix whose each column is a vector at right-hand side; will be overwritten by solution X after exit.</param>
		/// <param name="ldb">The leading dimension of <paramref name="B"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="B"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="m"/> ≤ <paramref name="n"/></exception>
		/// <exception cref="MatrixSolveAlgorithmException">If the internal solver failed due to some reason</exception>
		[AbstractApiMethod]
		public abstract bool LeastSquareSolve<T, TS1, TS2>(long m, long n, long nrhs, TS1 A, long lda, TS2 B, long ldb) where T : unmanaged, IFloatingPoint<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;
		#endregion

		#region other decompositions
		/// <summary>
		/// When implemented by a derived class, compute the singular value decomposition (SVD) of a matrix <paramref name="A"/> and corresponding the left and/or right singular vectors: <paramref name="A"/> = <paramref name="U"/> <paramref name="S"/> <paramref name="Vct"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the input data type</typeparam>
		/// <typeparam name="TReal">Any unmanaged number as the real corresponding type of <typeparamref name="T"/></typeparam>
		/// <typeparam name="TS1">The first actual storage type that implements <see cref="IStorage{T, TSelf}"/> of data type <typeparamref name="T"/></typeparam>
		/// <typeparam name="TS2">The second actual storage type that implements <see cref="IStorage{T, TSelf}"/> of data type <typeparamref name="T"/></typeparam>
		/// <typeparam name="TS3">The third actual storage type that implements <see cref="IStorage{T, TSelf}"/> of data type <typeparamref name="T"/></typeparam>
		/// <typeparam name="TSReal">The actual storage type that implements <see cref="IStorage{T, TSelf}"/> of data type <typeparamref name="TReal"/></typeparam>
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
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="S"/> or <paramref name="A"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="storeU"/> and <paramref name="storeV"/> are both <see cref="SVDStore.Overwrite"/></exception>
		/// <exception cref="TypeMismatchException">If <typeparamref name="TReal"/> is not a real type correspondence of <typeparamref name="T"/></exception>
		/// <exception cref="MatrixSolveAlgorithmException">If the internal solver failed due to some reason</exception>
		[AbstractApiMethod]
		public abstract bool SingularValues<T, TReal, TS1, TS2, TS3, TSReal>(SVDStore storeU, SVDStore storeV, long m, long n, TS1 A, long lda, TSReal S, TS2? U, long ldu, TS3? Vct, long ldvct) where T : unmanaged, IFloatingPoint<T> where TReal : unmanaged, IFloatingPoint<TReal> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3> where TSReal : class, IStorage<TReal, TSReal>;

		/// <summary>
		/// Compute the standard Schur decomposition of given matrix <paramref name="A"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the input/output data type</typeparam>
		/// <typeparam name="TComp">Any unmanaged number as the complex corresponding type of <typeparamref name="T"/></typeparam>
		/// <typeparam name="TS1">The first actual storage type that implements <see cref="IStorage{T, TSelf}"/> of data type <typeparamref name="T"/></typeparam>
		/// <typeparam name="TS2">The second actual storage type that implements <see cref="IStorage{T, TSelf}"/> of data type <typeparamref name="T"/></typeparam>
		/// <typeparam name="TSComp1">The first actual storage type that implements <see cref="IStorage{T, TSelf}"/> of data type <typeparamref name="TComp"/></typeparam>
		/// <typeparam name="TSComp2">The second actual storage type that implements <see cref="IStorage{T, TSelf}"/> of data type <typeparamref name="TComp"/></typeparam>
		/// <param name="mode">The <see cref="SolveVectorMode"/> to indicate whether to calculate Schur vectors or not</param>
		/// <param name="n">The number of rows and columns of matrix <paramref name="A"/></param>
		/// <param name="A">The input/output matrix to be decomposed of leading dimension <paramref name="lda"/> and size <paramref name="n"/>×<paramref name="n"/>, overwritten by the triangular Schur matrix at exit</param>
		/// <param name="valOut">The preallocated output eigenvalues of type <typeparamref name="TComp"/></param>
		/// <param name="lda">The leading dimension of <paramref name="A"/></param>
		/// <param name="U">The preallocated output Schur vectors of leading dimension <paramref name="ldu"/> and size <paramref name="n"/>×<paramref name="n"/>, can be null if <paramref name="mode"/> is <see cref="SolveVectorMode.NoVector"/>.</param>
		/// <param name="ldu">The leading dimension of <paramref name="U"/></param>
		/// <param name="orderVal">The values in this array will be selected to the top left of Schur form <paramref name="A"/>. Default null means no particular order is preferred.</param>
		/// <param name="actualNumber">Output the actual number of eigenvalues returned</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="orderVal"/> has duplicate values or its length is larger than <paramref name="n"/></exception>
		/// <exception cref="MatrixSolveAlgorithmException">If the internal solver failed due to some reason</exception>
		[AbstractApiMethod]
		public abstract bool StandardSchurDecomposition<T, TComp, TS1, TS2, TSComp1, TSComp2>(SolveVectorMode mode, long n, TS1 A, long lda, TSComp1 valOut, TS2? U, long ldu, out long actualNumber, TSComp2? orderVal = null) where T : unmanaged, IFloatingPoint<T> where TComp : unmanaged, IComplexFloatNumber<TComp> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TSComp1 : class, IStorage<TComp, TSComp1> where TSComp2 : class, IStorage<TComp, TSComp2>;

		/// <summary>
		/// Compute the general Schur decomposition of given matrices <paramref name="A"/> and <paramref name="B"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the input/output data type</typeparam>
		/// <typeparam name="TComp">Any unmanaged number as the complex corresponding type of <typeparamref name="T"/></typeparam>
		/// <typeparam name="TS1">The first actual storage type that implements <see cref="IStorage{T, TSelf}"/> of data type <typeparamref name="T"/></typeparam>
		/// <typeparam name="TS2">The second actual storage type that implements <see cref="IStorage{T, TSelf}"/> of data type <typeparamref name="T"/></typeparam>
		/// <typeparam name="TS3">The third actual storage type that implements <see cref="IStorage{T, TSelf}"/> of data type <typeparamref name="T"/></typeparam>
		/// <typeparam name="TSComp1">The first actual storage type that implements <see cref="IStorage{T, TSelf}"/> of data type <typeparamref name="TComp"/></typeparam>
		/// <typeparam name="TSComp2">The second actual storage type that implements <see cref="IStorage{T, TSelf}"/> of data type <typeparamref name="TComp"/></typeparam>
		/// <param name="mode">The <see cref="SolveVectorMode"/> to indicate whether to calculate Schur vectors or not</param>
		/// <param name="n">The number of rows and columns of matrix <paramref name="A"/></param>
		/// <param name="A">The first input/output matrix to be decomposed of leading dimension <paramref name="lda"/> and size <paramref name="n"/>×<paramref name="n"/>, overwritten by the first triangular Schur matrix at exit</param>
		/// <param name="B">The second input/output matrix to be decomposed of leading dimension <paramref name="ldb"/> and size <paramref name="n"/>×<paramref name="n"/>, overwritten by the second triangular Schur matrix at exit</param>
		/// <param name="valOut">The preallocated output eigenvalues of type <typeparamref name="TComp"/></param>
		/// <param name="lda">The leading dimension of <paramref name="A"/></param>
		/// <param name="ldb">The leading dimension of <paramref name="B"/></param>
		/// <param name="Ul">The preallocated output left Schur vectors of leading dimension <paramref name="ldul"/> and size <paramref name="n"/>×<paramref name="n"/>, can be null if <paramref name="mode"/> does not contains <see cref="SolveVectorMode.Left"/>.</param>
		/// <param name="ldul">The leading dimension of <paramref name="Ul"/></param>
		/// <param name="Ur">The preallocated output right Schur vectors of leading dimension <paramref name="ldur"/> and size <paramref name="n"/>×<paramref name="n"/>, can be null if <paramref name="mode"/> does not contains <see cref="SolveVectorMode.Right"/>.</param>
		/// <param name="ldur">The leading dimension of <paramref name="Ur"/></param>
		/// <param name="orderVal">The values in this array will be selected to the top left of Schur form <paramref name="A"/>. Default null means no particular order is preferred.</param>
		/// <param name="actualNumber">Output the actual number of eigenvalues returned</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="orderVal"/> has duplicate values or its length is larger than <paramref name="n"/></exception>
		/// <exception cref="MatrixSolveAlgorithmException">If the internal solver failed due to some reason</exception>
		[AbstractApiMethod]
		public abstract bool GeneralSchurDecomposition<T, TComp, TS1, TS2, TS3, TSComp1, TSComp2>(SolveVectorMode mode, long n, TS1 A, long lda, TS2 B, long ldb, TSComp1 valOut, TS2? Ul, long ldul, TS3? Ur, long ldur, out long actualNumber, TSComp2? orderVal = null) where T : unmanaged, IFloatingPoint<T> where TComp : unmanaged, IComplexFloatNumber<TComp> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3> where TSComp1 : class, IStorage<TComp, TSComp1> where TSComp2 : class, IStorage<TComp, TSComp2>;
		#endregion
	}
}
