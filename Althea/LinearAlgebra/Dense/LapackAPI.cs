using Althea.SourceGenerator;
using Althea.Storage;


namespace Althea.LinearAlgebra.Dense
{
	/// <summary>
	/// The abstract interface for dense linear algebra LAPACK API routines 
	/// </summary>
	[AbstractRuntimeApi]
	public interface ILapackAbstractApi : IAbstractRuntimeApi<ILapackAbstractApi>
	{
		#region eigen-problems
		/// <summary>
		/// When implemented by a derived class, calculate the eigenvalues (and eigenvectors) of given hermitian matrix <paramref name="A"/> for the special eigen-problem.
		/// </summary>
		/// <typeparam name="T">Any unmanaged floating point number as the input data type</typeparam>
		/// <typeparam name="TS1">The first actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The second actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS3">The third actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="n">The number of rows and columns of <paramref name="A"/> of type <typeparamref name="T"/></param>
		/// <param name="upper">Whether matrix <paramref name="A"/>'s upper or lower part is stored</param>
		/// <param name="A">The input hermitian matrix to calculate the special eigen-problem</param>
		/// <param name="lda">The leading dimension of <paramref name="A"/></param>
		/// <param name="valOut">The preallocated output eigenvalues of type <typeparamref name="T"/></param>
		/// <param name="vecOut">The preallocated output eigenvectors of type <typeparamref name="T"/>, can be the same as <paramref name="A"/>, null for not computing it</param>
		/// <param name="ldvec">The leading dimension of <paramref name="vecOut"/></param>
		/// <param name="allowDestroy">Whether the input matrices can be destroyed during calculation or not, default false</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="valOut"/> or <paramref name="A"/> is null or invalid</exception>
		/// <exception cref="MatrixSolveAlgorithmException">If the internal solver failed due to some reason</exception>
		[AbstractApiMethod]
		public abstract bool EigenStandardMatrixHermitian<T, TS1, TS2, TS3>(long n, bool upper, TS1 A, long lda, TS2 valOut, TS3? vecOut, long ldvec, bool allowDestroy = false) where T : unmanaged, IBinaryFloat<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>;

		/// <summary>
		/// When implemented by a derived class, calculate the eigenvalues (and eigenvectors) of given symmetric-definite / hermitian-definite matrix pair <paramref name="A"/>, <paramref name="B"/> for the general eigen-problem.
		/// </summary>
		/// <typeparam name="T">Any unmanaged floating point number as the input data type</typeparam>
		/// <typeparam name="TS1">The first actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The second actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS3">The third actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS4">The fourth actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="type">The <see cref="GeneralEigenType"/> to indicate positions of <paramref name="A"/> and <paramref name="B"/></param>
		/// <param name="n">The number of rows and columns of <paramref name="A"/> and <paramref name="B"/></param>
		/// <param name="upper">Whether all matrices upper or lower part is stored</param>
		/// <param name="A">The input symmetric/hermitian positive-definite matrix to calculate the general eigen-problem</param>
		/// <param name="lda">The leading dimension of <paramref name="A"/></param>
		/// <param name="B">Another input symmetric/hermitian positive-definite matrix to calculate the general eigen-problem</param>
		/// <param name="ldb">The leading dimension of <paramref name="B"/></param>
		/// <param name="valOut">The preallocated output eigenvalues of type <typeparamref name="T"/></param>
		/// <param name="vecOut">The preallocated output eigenvectors of type <typeparamref name="T"/>, can be the same as <paramref name="A"/>, null for not computing it</param>
		/// <param name="LUOut">The preallocated output LU factorization of <paramref name="B"/> stored in <paramref name="upper"/>, can be the same as <paramref name="B"/>, null for not computing it</param>
		/// <param name="ldLU"></param>
		/// <param name="ldvec">The leading dimension of <paramref name="vecOut"/></param>
		/// <param name="allowDestroy">Whether the input matrices can be destroyed during calculation or not, default false</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="valOut"/> or <paramref name="A"/> or <paramref name="B"/> is null or invalid</exception>
		/// <exception cref="MatrixSolveAlgorithmException">If the internal solver failed due to some reason</exception>
		[AbstractApiMethod]
		public abstract bool EigenGeneralMatrixHermitian<T, TS1, TS2, TS3, TS4>(GeneralEigenType type, long n, bool upper, TS1 A, long lda, TS1 B, long ldb, TS2 valOut, TS3? vecOut, long ldvec, TS4? LUOut, long ldLU, bool allowDestroy = false) where T : unmanaged, IBinaryFloat<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3> where TS4 : class, IStorage<T, TS4>;

		/// <summary>
		/// When implemented by a derived class, calculate the eigenvalues (and eigenvectors) of given general matrix <paramref name="A"/> for the special eigen-problem.
		/// </summary>
		/// <typeparam name="T">Any unmanaged floating point number as the input data type</typeparam>
		/// <typeparam name="TS1">The actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The first actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS3">The second actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS4">The third actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="n">The number of rows and columns of <paramref name="A"/></param>
		/// <param name="A">The input general matrix to calculate the special eigen-problem of a </param>
		/// <param name="lda">leading dimension of <paramref name="A"/></param>
		/// <param name="valsOut">The preallocated output eigenvalues('s real parts) of type <typeparamref name="T"/></param>
		/// <param name="valsOutImag">The preallocated output eigenvalues's imaginary parts of type <typeparamref name="T"/> which shall be null if <typeparamref name="T"/> is a complex type</param>
		/// <param name="leftVec">The preallocated output left eigenvectors of type <typeparamref name="T"/>, null for not computing it</param>
		/// <param name="ldvl">The leading dimension of <paramref name="leftVec"/></param>
		/// <param name="ldvr">The leading dimension of <paramref name="rightVec"/></param>
		/// <param name="rightVec">The preallocated output right eigenvectors of type <typeparamref name="T"/>, null for not computing it</param>
		/// <param name="allowDestroy">Whether the input matrices can be destroyed during calculation or not, default false</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <remarks> When <typeparamref name="T"/> is a real type and <c><paramref name="valsOutImag"/>[i] != 0</c>, <c><paramref name="leftVec"/>[.., i], <paramref name="leftVec"/>[.., i + 1]</c> shall be the real and imaginary parts of actual <c>left_eigenvector[i], left_eigenvector[i + 1].Conjugate</c>. Same for <paramref name="rightVec"/>.</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="valsOut"/> or <paramref name="A"/> is null or invalid; or <paramref name="valsOutImag"/> is null while <typeparamref name="T"/> is a real type</exception>
		/// <exception cref="MatrixSolveAlgorithmException">If the internal solver failed due to some reason</exception>
		[AbstractApiMethod]
		public abstract bool EigenStandardMatrixGeneral<T, TS1, TS2, TS3, TS4>(long n, TS1 A, long lda, TS2 valsOut, TS2? valsOutImag, TS3? leftVec, long ldvl, TS4? rightVec, long ldvr, bool allowDestroy = false) where T : unmanaged, IBinaryFloat<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3> where TS4 : class, IStorage<T, TS4>;

		/// <summary>
		/// When implemented by a derived class, calculate the eigenvalues (and eigenvectors) of given general matrix pair <paramref name="A"/>, <paramref name="B"/> for the general eigen-problem. The output eigenvalues are separated to prevent possible over- or under- flow.
		/// </summary>
		/// <typeparam name="T">Any unmanaged floating point number as the input data type</typeparam>
		/// <typeparam name="TS1">The actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The first actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS3">The second actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS4">The third actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="type">The <see cref="GeneralEigenType"/> to indicate positions of <paramref name="A"/> and <paramref name="B"/></param>
		/// <param name="n">The number of rows and columns of <paramref name="A"/> and <paramref name="B"/></param>
		/// <param name="A">The input general matrix to calculate eigensystem</param>
		/// <param name="lda">The leading dimension of <paramref name="A"/></param>
		/// <param name="B">Another input/output matrix to calculate the general eigen-problem; may be destroyed during the calculation</param>
		/// <param name="ldb">The leading dimension of <paramref name="B"/></param>
		/// <param name="valsOut">The preallocated output eigenvalues('s real parts) of type <typeparamref name="T"/></param>
		/// <param name="valsOutImag">The preallocated output eigenvalues's imaginary parts of type <typeparamref name="T"/> which shall be null if <typeparamref name="T"/> is a complex type</param>
		/// <param name="valsOutDenom">The preallocated output eigenvalues's denominators</param>
		/// <param name="leftVec">The preallocated output left eigenvectors of type <typeparamref name="T"/>, null for not computing it</param>
		/// <param name="ldvl">The leading dimension of <paramref name="leftVec"/></param>
		/// <param name="ldvr">The leading dimension of <paramref name="rightVec"/></param>
		/// <param name="rightVec">The preallocated output right eigenvectors of type <typeparamref name="T"/>, null for not computing it</param>
		/// <param name="allowDestroy">Whether the input matrices can be destroyed during calculation or not, default false</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <remarks> When <typeparamref name="T"/> is a real type and <c><paramref name="valsOutImag"/>[i] != 0</c>, <c><paramref name="leftVec"/>[.., i], <paramref name="leftVec"/>[.., i + 1]</c> shall be the real and imaginary parts of actual <c>left_eigenvector[i], left_eigenvector[i + 1].Conjugate</c>. Same for <paramref name="rightVec"/>.</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="valsOut"/> or <paramref name="A"/> is null or invalid; or <paramref name="valsOutImag"/> is null while <typeparamref name="T"/> is a real type</exception>
		/// <exception cref="MatrixSolveAlgorithmException">If the internal solver failed due to some reason</exception>
		[AbstractApiMethod]
		public abstract bool EigenGeneralMatrixGeneral<T, TS1, TS2, TS3, TS4>(GeneralEigenType type, long n, TS1 A, long lda, TS1 B, long ldb, TS2 valsOut, TS2? valsOutImag, TS2 valsOutDenom, TS3? leftVec, long ldvl, TS4? rightVec, long ldvr, bool allowDestroy = false) where T : unmanaged, IBinaryFloat<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3> where TS4 : class, IStorage<T, TS4>;
		#endregion

		#region other decompositions
		/// <summary>
		/// When implemented by a derived class, compute the singular value decomposition (SVD) of a matrix <paramref name="A"/> and corresponding the left and/or right singular vectors: <paramref name="A"/> = <paramref name="U"/> * diag(<paramref name="S"/>) * <paramref name="Vct"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged floating point number as the input data type</typeparam>
		/// <typeparam name="TS1">The first actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The second actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS3">The third actual storage type that implements <see cref="IStorage{T, TSelf}"/>></typeparam>
		/// <typeparam name="TS4">The actual storage type that implements <see cref="IStorage{T, TSelf}"/> for <paramref name="S"/></typeparam>
		/// <param name="fullU">Whether all the singular vectors in <paramref name="U"/> are required</param>
		/// <param name="fullV">Whether all the singular vectors in <paramref name="Vct"/> are required</param>
		/// <param name="m">The number of rows of matrix <paramref name="A"/></param>
		/// <param name="n">The number of columns of matrix <paramref name="A"/></param>
		/// <param name="A">The input (and possible output) matrix of size <paramref name="m"/>×<paramref name="n"/> and leading dimension <paramref name="lda"/> which will be overwritten by singular vectors if <paramref name="U"/> or <paramref name="Vct"/> == <paramref name="A"/></param>
		/// <param name="lda">The leading dimension of <paramref name="A"/></param>
		/// <param name="S">The preallocated output singular values as a vector of size at least <c>min(<paramref name="m"/>, <paramref name="n"/>)</c></param>
		/// <param name="U">The preallocated output left unitary matrix with size <paramref name="ldu"/>×<paramref name="m"/>, null for not computing it, the same as <paramref name="A"/> for overwrite <paramref name="A"/></param>
		/// <param name="ldu">The leading dimension of <paramref name="U"/></param>
		/// <param name="Vct">The preallocated output right unitary ("ct" for conjugate transpose) matrix with size <paramref name="ldvct"/>×<paramref name="n"/>, null for not computing it, the same as <paramref name="A"/> for overwrite <paramref name="A"/></param>
		/// <param name="ldvct">The leading dimension of <paramref name="Vct"/></param>
		/// <param name="allowDestroy">Whether the input matrices can be destroyed during calculation or not, default false</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="S"/> or <paramref name="A"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If both <paramref name="U"/> and <paramref name="Vct"/> == <paramref name="A"/> or the overwritten cannot be performed due to incompatible size</exception>
		/// <exception cref="MatrixSolveAlgorithmException">If the internal solver failed due to some reason</exception>
		[AbstractApiMethod]
		public abstract bool SingularValues<T, TS1, TS2, TS3, TS4>(bool fullU, bool fullV, long m, long n, TS1 A, long lda, TS2? U, long ldu, TS3? Vct, long ldvct, TS4 S, bool allowDestroy = false) where T : unmanaged, IBinaryFloat<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3> where TS4 : class, IStorage<T, TS4>;

		/// <summary>
		/// Compute the standard Schur decomposition of given matrix <paramref name="A"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged floating point number as the input/output data type</typeparam>
		/// <typeparam name="TS1">The first actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The second actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS3">The actual storage type that implements <see cref="IStorage{T, TSelf}"/> for <paramref name="valsOut"/></typeparam>
		/// <param name="n">The number of rows and columns of matrix <paramref name="A"/></param>
		/// <param name="A">The input/output matrix to be decomposed of leading dimension <paramref name="lda"/> and size <paramref name="n"/>×<paramref name="n"/>, overwritten by the Schur form matrix at exit</param>
		/// <param name="valsOut">The preallocated output eigenvalues('s real parts) of type <typeparamref name="T"/></param>
		/// <param name="valsOutImag">The preallocated output eigenvalues's imaginary parts of type <typeparamref name="T"/> which shall be null if <typeparamref name="T"/> is a complex type</param>
		/// <param name="lda">The leading dimension of <paramref name="A"/></param>
		/// <param name="U">The preallocated output Schur vectors of leading dimension <paramref name="ldu"/> and size <paramref name="n"/>×<paramref name="n"/>, null for not computing it</param>
		/// <param name="ldu">The leading dimension of <paramref name="U"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="valsOut"/> or <paramref name="A"/> is null or invalid; or <paramref name="valsOutImag"/> is null while <typeparamref name="T"/> is a real type</exception>
		/// <exception cref="MatrixSolveAlgorithmException">If the internal solver failed due to some reason</exception>
		[AbstractApiMethod]
		public abstract bool SchurDecomposition<T, TS1, TS2, TS3>(long n, TS1 A, long lda, TS2? U, long ldu, TS3 valsOut, TS3? valsOutImag) where T : unmanaged, IBinaryFloat<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>;

		/// <summary>
		/// Reorder the standard Schur decomposition of given matrix <paramref name="A"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged floating point number as the input/output data type</typeparam>
		/// <typeparam name="TInd">Any unmanaged integer number as the input data type</typeparam>
		/// <typeparam name="TS1">The first actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The second actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS3">The actual storage type that implements <see cref="IStorage{T, TSelf}"/> for <paramref name="vals"/></typeparam>
		/// <typeparam name="TSInd">The actual storage type that implements <see cref="IStorage{T, TSelf}"/> for <paramref name="select"/></typeparam>
		/// <param name="n">The number of rows and columns of matrix <paramref name="A"/></param>
		/// <param name="A">The input/output Schur form matrix of leading dimension <paramref name="lda"/> and size <paramref name="n"/>×<paramref name="n"/></param>
		/// <param name="lda">The leading dimension of <paramref name="A"/></param>
		/// <param name="U">The input/output Schur vectors of leading dimension <paramref name="ldu"/> and size <paramref name="n"/>×<paramref name="n"/></param>
		/// <param name="ldu">The leading dimension of <paramref name="U"/></param>
		/// <param name="vals">The eigenvalues('s real parts) of type <typeparamref name="T"/> computed by <see cref="SchurDecomposition"/> which will be reordered as well</param>
		/// <param name="valsImag">The eigenvalues's imaginary parts of type <typeparamref name="T"/> computed by <see cref="SchurDecomposition"/> which will be reordered as well</param>
		/// <param name="select">A boolean array indicating which eigenvalues shall be selected to the top-left of the Schur form; if <typeparamref name="T"/> is a real type, then <paramref name="select"/>[i] and <paramref name="select"/>[i+1] must both be 1 for conjugate eigenvalue pair to select them</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="vals"/> (or <paramref name="valsImag"/>) is null or invalid</exception>
		/// <exception cref="MatrixSolveAlgorithmException">If the internal solver failed due to some reason</exception>
		[AbstractApiMethod]
		public abstract bool SchurReorder<T, TInd, TS1, TS2, TS3, TSInd>(long n, TS1 A, long lda, TS2? U, long ldu, TS3 vals, TS3? valsImag, TSInd select) where T : unmanaged, IBinaryFloat<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3> where TInd : unmanaged, IBaseNumber<TInd> where TSInd : class, IStorage<TInd, TSInd>;
		#endregion

		#region linear solve
		/// <summary>
		/// When implemented by a derived class, solve a series of linear systems: <c><paramref name="A"/> * X == <paramref name="B"/></c>. Where each column pair of X and <paramref name="B"/> together with <paramref name="A"/> is a linear system.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS1">The first actual storage type that implements <see cref="IStorage{T, TSelf}"/> of data type <typeparamref name="T"/></typeparam>
		/// <typeparam name="TS2">The second actual storage type that implements <see cref="IStorage{T, TSelf}"/> of data type <typeparamref name="T"/></typeparam>
		/// <param name="n">The number of rows and columns of matrix <paramref name="A"/></param>
		/// <param name="nrhs">The number of right-hand sides, a.k.a. the number of linear systems.</param>
		/// <param name="A">The input/output coefficient matrix which may be overwritten by its LU decomposition at exit if <paramref name="allowDestroy"/></param>
		/// <param name="lda">The leading dimension of <paramref name="A"/></param>
		/// <param name="B">The input/output matrix whose each column is a vector at right-hand side which will be overwritten by solution X at exit</param>
		/// <param name="ldb">The leading dimension of <paramref name="B"/></param>
		/// <param name="allowDestroy">Whether the input matrices can be destroyed during calculation or not, default false</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="B"/> is null or invalid</exception>
		/// <exception cref="MatrixSolveAlgorithmException">If the internal solver failed due to some reason</exception>
		[AbstractApiMethod]
		public abstract bool LinearSolveGeneral<T, TS1, TS2>(long n, long nrhs, TS1 A, long lda, TS2 B, long ldb, bool allowDestroy = false) where T : unmanaged, IBinaryFloat<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;
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
		/// <param name="Q">The preallocated output unitary matrix of leading dimension <paramref name="ldq"/>, null means do not calculate Q matrix</param>
		/// <param name="ldq">The leading dimension of <paramref name="Q"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="Q"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="Q"/> do not contain enough space to be overwritten</exception>
		/// <exception cref="MatrixSolveAlgorithmException">If the internal solver failed due to some reason</exception>
		[AbstractApiMethod]
		public abstract bool QRDecomposition<T, TS1, TS2>(bool full, long m, long n, TS1 A, long lda, TS2? Q, long ldq) where T : unmanaged, IBinaryFloat<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

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
		/// <param name="A">The input coefficient matrix</param>
		/// <param name="lda">The leading dimension of <paramref name="A"/></param>
		/// <param name="B">The input/output matrix whose each column is a vector at right-hand side; will be overwritten by solution X after exit.</param>
		/// <param name="ldb">The leading dimension of <paramref name="B"/></param>
		/// <param name="allowDestroy">Whether the input matrices can be destroyed during calculation or not, default false</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="B"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="m"/> ≤ <paramref name="n"/></exception>
		/// <exception cref="MatrixSolveAlgorithmException">If the internal solver failed due to some reason</exception>
		[AbstractApiMethod]
		public abstract bool LeastSquareSolve<T, TS1, TS2>(long m, long n, long nrhs, TS1 A, long lda, TS2 B, long ldb, bool allowDestroy = false) where T : unmanaged, IBinaryFloat<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;
		#endregion
	}
}
