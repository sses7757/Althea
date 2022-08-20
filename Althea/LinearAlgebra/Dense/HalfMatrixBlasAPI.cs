using Althea.SourceGenerator;
using Althea.Storage;


namespace Althea.LinearAlgebra.Dense
{
	/// <summary>
	/// The abstract interface for dense linear algebra extend half-stored matrices BLAS API routines 
	/// </summary>
	[AbstractRuntimeApi]
	public interface IHalfMatrixBlasAbstractApi : IAbstractRuntimeApi<IHalfMatrixBlasAbstractApi>
	{
		#region basic
		/// <summary>
		/// When implemented by a derived class, perform the matrix-matrix addition and/or transposition:<br/>
		/// <c><paramref name="C"/> = <paramref name="α"/> * <paramref name="opA"/>(<paramref name="A"/>) + <paramref name="β"/> * <paramref name="opB"/>(<paramref name="B"/>)</c>. <br/>
		/// Where <paramref name="α"/> and <paramref name="β"/> are scalars; and <paramref name="A"/>, <paramref name="B"/>, <paramref name="C"/> are matrices stored in column-major format with dimensions <paramref name="opA"/>(<paramref name="A"/>) → <paramref name="m"/>×<paramref name="n"/>, <paramref name="opB"/>(<paramref name="B"/>) → <paramref name="m"/>×<paramref name="n"/> and <paramref name="C"/> → <paramref name="m"/>×<paramref name="n"/>, respectively.
		/// </summary>
		/// <remarks>See <see cref="IExtendBlasAbstractApi.GeneralMatricesAdd"/>.</remarks>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS1">The first actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The second actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS3">The third actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="unitDiag">Whether to prevent adding the matrices' diagonal elements or not</param>
		/// <param name="upper">Whether all matrices' (after <see cref="MatrixOperation"/>s) upper half or lower half is stored</param>
		/// <param name="opA">The <see cref="MatrixOperation"/> to indicate the simple operation of <paramref name="A"/></param>
		/// <param name="opB">The <see cref="MatrixOperation"/> to indicate the simple operation of <paramref name="B"/></param>
		/// <param name="m">The number of rows of matrix <paramref name="opA"/>(<paramref name="A"/>) and <paramref name="C"/></param>
		/// <param name="n">The number of columns of matrix <paramref name="opB"/>(<paramref name="B"/>) and <paramref name="C"/></param>
		/// <param name="α">The scalar used for multiplication. If <c><paramref name="α"/> == 0</c>, <paramref name="A"/> does not have to be a valid input</param>
		/// <param name="A">The array of dimensions <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="m"/>)</c> if <paramref name="opA"/> == <see cref="MatrixOperation.None"/> and <c><paramref name="lda"/>×<paramref name="m"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="n"/>)</c> otherwise</param>
		/// <param name="lda">The leading dimension of two-dimensional array used to store the matrix <paramref name="A"/></param>
		/// <param name="β">The scalar used for multiplication. If <c><paramref name="β"/> == 0</c>, <paramref name="B"/> does not have to be a valid input</param>
		/// <param name="B">The array of dimensions <c><paramref name="ldb"/>×<paramref name="n"/></c> with <c><paramref name="ldb"/> ≥ max(1, <paramref name="m"/>)</c> if <paramref name="opA"/> == <see cref="MatrixOperation.None"/> and <c><paramref name="ldb"/>×<paramref name="m"/></c> with <c><paramref name="ldb"/> ≥ max(1, <paramref name="n"/>)</c> otherwise</param>
		/// <param name="ldb">The leading dimension of two-dimensional array used to store the matrix <paramref name="B"/></param>
		/// <param name="C">The array of dimensions <c><paramref name="ldc"/>×<paramref name="n"/></c> with <c><paramref name="ldc"/> ≥ max(1, <paramref name="m"/>)</c></param>
		/// <param name="ldc">The leading dimension of two-dimensional array used to store the matrix <paramref name="C"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentException">If the parameters do not fit any mode</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> and <paramref name="B"/> or <paramref name="C"/> is null or invalid</exception>
		[AbstractApiMethod]
		public abstract bool TriangularMatricesAdd<T, TS1, TS2, TS3>(bool unitDiag, bool upper, MatrixOperation opA, MatrixOperation opB, long m, long n, T α, TS1? A, long lda, T β, TS2? B, long ldb, TS3 C, long ldc) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>;

		/// <summary>
		/// When implemented by a derived class, perform the matrix-matrix addition and/or transposition:<br/>
		/// <c><paramref name="C"/> = <paramref name="α"/> * <paramref name="opA"/>(<paramref name="A"/>) + <paramref name="β"/> * <paramref name="opB"/>(<paramref name="B"/>)</c>. <br/>
		/// Where <paramref name="α"/> and <paramref name="β"/> are scalars; and <paramref name="A"/>, <paramref name="B"/>, <paramref name="C"/> are symmetric matrices stored in column-major format.
		/// </summary>
		/// <remarks>See <see cref="IExtendBlasAbstractApi.GeneralMatricesAdd"/>.</remarks>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS1">The first actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The second actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS3">The third actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="upperA">Whether <paramref name="A"/>'s upper half or lower half is stored</param>
		/// <param name="upperB">Whether <paramref name="B"/>'s upper half or lower half is stored</param>
		/// <param name="upperC">Whether <paramref name="C"/>'s upper half or lower half is stored</param>
		/// <param name="opA">The <see cref="MatrixOperation"/> to indicate the simple operation of <paramref name="A"/></param>
		/// <param name="opB">The <see cref="MatrixOperation"/> to indicate the simple operation of <paramref name="B"/></param>
		/// <param name="n">The number of rows and columns of <paramref name="opA"/>(<paramref name="A"/>), <paramref name="opB"/>(<paramref name="B"/>) and <paramref name="C"/></param>
		/// <param name="α">The scalar used for multiplication. If <c><paramref name="α"/> == 0</c>, <paramref name="A"/> does not have to be a valid input</param>
		/// <param name="A">The array of dimensions <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="n"/>)</c></param>
		/// <param name="lda">The leading dimension of two-dimensional array used to store the matrix <paramref name="A"/></param>
		/// <param name="β">The scalar used for multiplication. If <c><paramref name="β"/> == 0</c>, <paramref name="B"/> does not have to be a valid input</param>
		/// <param name="B">The array of dimensions <c><paramref name="ldb"/>×<paramref name="n"/></c> with <c><paramref name="ldb"/> ≥ max(1, <paramref name="n"/>)</c></param>
		/// <param name="ldb">The leading dimension of two-dimensional array used to store the matrix <paramref name="B"/></param>
		/// <param name="C">The array of dimensions <c><paramref name="ldc"/>×<paramref name="n"/></c> with <c><paramref name="ldc"/> ≥ max(1, <paramref name="n"/>)</c></param>
		/// <param name="ldc">The leading dimension of two-dimensional array used to store the matrix <paramref name="C"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentException">If the parameters do not fit any mode</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> and <paramref name="B"/> or <paramref name="C"/> is null or invalid</exception>
		[AbstractApiMethod]
		public abstract bool SymmetricMatricesAdd<T, TS1, TS2, TS3>(bool upperA, bool upperB, bool upperC, MatrixOperation opA, MatrixOperation opB, long n, T α, TS1? A, long lda, T β, TS2? B, long ldb, TS3 C, long ldc) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>;

		/// <summary>
		/// When implemented by a derived class, perform the triangular matrix-matrix multiplication:<br/>
		/// <paramref name="C"/> = <paramref name="α"/> * <paramref name="opA"/>(<paramref name="A"/>) * <paramref name="opB"/>(<paramref name="B"/>) + <paramref name="β"/> * <paramref name="C"/>.<br/>
		/// Where <paramref name="α"/> and <paramref name="β"/> are scalars; <paramref name="A"/>, <paramref name="B"/> and <paramref name="C"/> are matrices stored in column-major format with dimensions <paramref name="opA"/>(<paramref name="A"/>) → <paramref name="m"/>×<paramref name="k"/>, <paramref name="opB"/>(<paramref name="B"/>) → <paramref name="k"/>×<paramref name="n"/> and <paramref name="C"/> → <paramref name="m"/>×<paramref name="n"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS1">The first actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The second actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS3">The third actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="unitDiag">Whether all matrices are of unit diagonal or not</param>
		/// <param name="upper">Whether all matrices' (after <see cref="MatrixOperation"/>s) upper or lower parts are stored</param>
		/// <param name="opA">The <see cref="MatrixOperation"/> to indicate the simple operation of <paramref name="A"/></param>
		/// <param name="opB">The <see cref="MatrixOperation"/> to indicate the simple operation of <paramref name="B"/></param>
		/// <param name="m">The number of rows of matrix <paramref name="opA"/>(<paramref name="A"/>) and <paramref name="C"/></param>
		/// <param name="n">The number of columns of matrix <paramref name="opB"/>(<paramref name="B"/>) and <paramref name="C"/></param>
		/// <param name="k">The number of columns of <paramref name="opA"/>(<paramref name="A"/>) and rows of <paramref name="opB"/>(<paramref name="B"/>)</param>
		/// <param name="α">The scalar to be multiplied to <paramref name="opA"/>(<paramref name="A"/>) * <paramref name="opB"/>(<paramref name="B"/>)</param>
		/// <param name="A">The array of dimensions <c><paramref name="lda"/>×<paramref name="k"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="m"/>)</c> if <paramref name="opA"/> == <see cref="MatrixOperation.None"/>, and <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="k"/>)</c> otherwise</param>
		/// <param name="lda">The leading dimension of two-dimensional array used to store the matrix <paramref name="A"/></param>
		/// <param name="B">The array of dimension <c><paramref name="ldb"/>×<paramref name="n"/></c> with <c><paramref name="ldb"/> ≥ max(1, <paramref name="k"/>)</c> if <paramref name="opB"/> == <see cref="MatrixOperation.None"/>, and <c><paramref name="ldb"/>×<paramref name="k"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="n"/>)</c> otherwise</param>
		/// <param name="ldb">The leading dimension of two-dimensional array used to store matrix <paramref name="B"/></param>
		/// <param name="β">The scalar to be multiplied to <paramref name="C"/>. If this is 0, the original values of <paramref name="C"/> will be ignored.</param>
		/// <param name="C">The array of dimensions <c><paramref name="ldc"/>×<paramref name="n"/></c> with <c><paramref name="ldc"/> ≥ max(1, <paramref name="m"/>)</c></param>
		/// <param name="ldc">The leading dimension of a two-dimensional array used to store the matrix <paramref name="C"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="B"/> or <paramref name="C"/> is null or invalid</exception>
		[AbstractApiMethod]
		public abstract bool TriangularMatricesMultiply<T, TS1, TS2, TS3>(bool unitDiag, bool upper, MatrixOperation opA, MatrixOperation opB, long m, long n, long k, T α, TS1 A, long lda, TS2 B, long ldb, T β, TS3 C, long ldc) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>;

		/// <summary>
		/// When implemented by a derived class, perform the symmetric/Hermitian matrix-matrix multiplication to a dense matrix:<br/>
		/// <paramref name="C"/> = <paramref name="α"/> * <paramref name="opA"/>(<paramref name="A"/>) * <paramref name="opB"/>(<paramref name="B"/>) + <paramref name="β"/> * <paramref name="C"/>.<br/>
		/// Where <paramref name="α"/> and <paramref name="β"/> are scalars; <paramref name="A"/> and <paramref name="B"/> are symmetric/Hermitian matrices; <paramref name="C"/> is a dense matrix.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS1">The first actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The second actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS3">The third actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="upperA">Whether <paramref name="A"/>'s upper or lower parts are stored</param>
		/// <param name="upperB">Whether <paramref name="B"/>'s upper or lower parts are stored</param>
		/// <param name="hermA">Whether <paramref name="A"/> is Hermitian or simply symmetric</param>
		/// <param name="hermB">Whether <paramref name="B"/> is Hermitian or simply symmetric</param>
		/// <param name="opA">The <see cref="MatrixOperation"/> to indicate the simple operation of <paramref name="A"/></param>
		/// <param name="opB">The <see cref="MatrixOperation"/> to indicate the simple operation of <paramref name="B"/></param>
		/// <param name="n">The number of rows and columns of all matrices</param>
		/// <param name="α">The scalar to be multiplied to <paramref name="opA"/>(<paramref name="A"/>) * <paramref name="opB"/>(<paramref name="B"/>)</param>
		/// <param name="A">The array of dimensions <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="n"/>)</c></param>
		/// <param name="lda">The leading dimension of two-dimensional array used to store the matrix <paramref name="A"/></param>
		/// <param name="B">The array of dimension <c><paramref name="ldb"/>×<paramref name="n"/></c> with <c><paramref name="ldb"/> ≥ max(1, <paramref name="n"/>)</c></param>
		/// <param name="ldb">The leading dimension of two-dimensional array used to store matrix <paramref name="B"/></param>
		/// <param name="β">The scalar to be multiplied to <paramref name="C"/>. If this is 0, the original values of <paramref name="C"/> will be ignored.</param>
		/// <param name="C">The array of dimensions <c><paramref name="ldc"/>×<paramref name="n"/></c> with <c><paramref name="ldc"/> ≥ max(1, <paramref name="n"/>)</c></param>
		/// <param name="ldc">The leading dimension of a two-dimensional array used to store the matrix <paramref name="C"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="B"/> or <paramref name="C"/> is null or invalid</exception>
		[AbstractApiMethod]
		public abstract bool SymmetricMatricesMultiply<T, TS1, TS2, TS3>(bool upperA, bool upperB, bool hermA, bool hermB, MatrixOperation opA, MatrixOperation opB, long n, T α, TS1 A, long lda, TS2 B, long ldb, T β, TS3 C, long ldc) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>;

		/// <summary>
		/// When implemented by a derived class, make the matrix <paramref name="A"/> a normal one by copying its upper or lower part to the other part and set the diagonal elements to its absolute value is <typeparamref name="T"/> is a complex type.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS">The actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="upper">Whether the upper triangular part of <paramref name="A"/> is stored or its lower part</param>
		/// <param name="hermitian">Whether to use hermitian conjugate copies or simple copies</param>
		/// <param name="n">The number of rows and columns of <paramref name="A"/></param>
		/// <param name="A">The matrix with size <paramref name="n"/>×<paramref name="n"/></param>
		/// <param name="lda">The leading dimension of <paramref name="A"/>, must be at least <paramref name="n"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> is null or invalid</exception>
		[AbstractApiMethod]
		public abstract bool SymmetricMatrixToNormal<T, TS>(bool upper, bool hermitian, long n, TS A, long lda) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>;

		/// <summary>
		/// When implemented by a derived class, clear the matrix <paramref name="A"/>'s upper or lower part (along with or without the diagonal elements) to 0.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS">The actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="clearDiag">Whether the diagonal of <paramref name="A"/> shall be cleared or not</param>
		/// <param name="clearLower">Whether the lower triangular part of <paramref name="A"/> shall be cleared or its upper part</param>
		/// <param name="m">The number of rows of <paramref name="A"/></param>
		/// <param name="n">The number of columns of <paramref name="A"/></param>
		/// <param name="A">The matrix with size <paramref name="n"/>×<paramref name="n"/></param>
		/// <param name="lda">The leading dimension of <paramref name="A"/>, must be at least <paramref name="n"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> is null or invalid</exception>
		[AbstractApiMethod]
		public abstract bool HalfMatrixClearPart<T, TS>(bool clearDiag, bool clearLower, long m, long n, TS A, long lda) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>;

		/// <summary>
		/// When implemented by a derived class, copy the matrix <paramref name="opA"/>(<paramref name="A"/>)'s upper or lower part to <paramref name="B"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS1">The actual input storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The actual output storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="copyDiag">Whether to copy the diagonal of <paramref name="A"/> or not</param>
		/// <param name="upper">Whether the upper triangular part of <paramref name="A"/> is stored or its lower part</param>
		/// <param name="opA">The <see cref="MatrixOperation"/> to indicate the simple operation of <paramref name="A"/></param>
		/// <param name="m">The number of rows of <paramref name="opA"/>(<paramref name="A"/>) and <paramref name="B"/></param>
		/// <param name="n">The number of columns of <paramref name="opA"/>(<paramref name="A"/>) and <paramref name="B"/></param>
		/// <param name="A">The input matrix with size <paramref name="m"/>×<paramref name="n"/></param>
		/// <param name="lda">The leading dimension of <paramref name="A"/>, must be at least <paramref name="m"/></param>
		/// <param name="B">The output matrix with size <paramref name="n"/>×<paramref name="n"/></param>
		/// <param name="ldb">The leading dimension of <paramref name="B"/>, must be at least <paramref name="m"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> is null or invalid</exception>
		[AbstractApiMethod]
		public abstract bool HalfMatrixCopy<T, TS1, TS2>(bool upper, bool copyDiag, MatrixOperation opA, long m, long n, TS1 A, long lda, TS2 B, long ldb) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;
		#endregion

		#region matrix math
		/// <summary>
		/// When implemented by a derived class, compute <c><paramref name="B"/>[i, j] = <paramref name="op"/>(<paramref name="A"/>[i, j])</c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TS1">The input actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The output actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="upper">Whether all matrices' upper or lower parts are stored</param>
		/// <param name="unitDiag">Whether the diagonal elements of <paramref name="A"/> are all ones (thus not stored) or not</param>
		/// <param name="rows">The number of rows in <typeparamref name="T"/></param>
		/// <param name="cols">The number of columns in <typeparamref name="T"/></param>
		/// <param name="A">The matrix to be operated</param>
		/// <param name="lda">The leading dimension of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="B">The matrix to be overwritten</param>
		/// <param name="ldb">The leading dimension of <paramref name="B"/> in <typeparamref name="T"/></param>
		/// <param name="op">The <see cref="UnaryOperation"/> to apply to each element of <paramref name="A"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="lda"/> &lt; <paramref name="rows"/></exception>
		[AbstractApiMethod]
		public abstract bool HalfMatrixUnary<T, TS1, TS2>(UnaryOperation op, bool upper, bool unitDiag, long rows, long cols, TS1 A, long lda, TS2 B, long ldb) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

		/// <summary>
		/// When implemented by a derived class, compute <c>result = <paramref name="op"/>(<paramref name="A"/>[i, j], result)</c> for all <c>i, j</c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TS">The actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="upper">Whether all matrices' upper or lower parts are stored</param>
		/// <param name="triangular">Whether <paramref name="A"/> is triangular matrix or a symmetric/Hermitian matrix</param>
		/// <param name="unitDiagOrHerm">If <paramref name="triangular"/>, indicates whether the diagonal elements of <paramref name="A"/> are all ones (thus not stored) or not; or whether <paramref name="A"/> is Hermitian or simply symmetric otherwise</param>
		/// <param name="rows">The number of rows in <typeparamref name="T"/></param>
		/// <param name="cols">The number of columns in <typeparamref name="T"/></param>
		/// <param name="A">The matrix to be reduced</param>
		/// <param name="lda">The leading dimension of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="op">The <see cref="ReduceOperation"/> to apply to elements of <paramref name="A"/></param>
		/// <param name="result">Output the reduction result of <paramref name="A"/> under <paramref name="op"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="lda"/> &lt; <paramref name="rows"/></exception>
		[AbstractApiMethod]
		public abstract bool HalfMatrixReduce<T, TS>(ReduceOperation op, bool upper, bool triangular, bool unitDiagOrHerm, long rows, long cols, TS A, long lda, out T result) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>;

		/// <summary>
		/// When implemented by a derived class, compute the index of the reduction result: <c>result = <paramref name="op"/>(<paramref name="A"/>[i, j], result)</c> for all <c>i, j</c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TS">The actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="upper">Whether all matrices' upper or lower parts are stored</param>
		/// <param name="triangular">Whether <paramref name="A"/> is triangular matrix or a symmetric/Hermitian matrix</param>
		/// <param name="unitDiagOrHerm">If <paramref name="triangular"/>, indicates whether the diagonal elements of <paramref name="A"/> are all ones (thus not stored) or not; or whether <paramref name="A"/> is Hermitian or simply symmetric otherwise</param>
		/// <param name="rows">The number of rows in <typeparamref name="T"/></param>
		/// <param name="cols">The number of columns in <typeparamref name="T"/></param>
		/// <param name="A">The matrix to be reduced</param>
		/// <param name="lda">The leading dimension of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="op">The <see cref="ReduceOperation"/> to apply to elements of <paramref name="A"/>, must be ones like <see cref="ReduceOperation.Maximum"/></param>
		/// <param name="index">Output the reduction result's index of <paramref name="A"/> under <paramref name="op"/> compared to <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="lda"/> &lt; <paramref name="rows"/></exception>
		[AbstractApiMethod]
		public abstract bool HalfMatrixArgReduce<T, TS>(ReduceOperation op, bool upper, bool triangular, bool unitDiagOrHerm, long rows, long cols, TS A, long lda, out long index) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>;

		/// <summary>
		/// When implemented by a derived class, compute <c><paramref name="x"/>[i] = <paramref name="op"/>(<paramref name="A"/>[j, i], <paramref name="x"/>[i])</c> for all <c>i, j</c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TS1">The actual matrix storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The actual vector storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="upper">Whether all matrices' upper or lower parts are stored</param>
		/// <param name="triangular">Whether <paramref name="A"/> is triangular matrix or a symmetric/Hermitian matrix</param>
		/// <param name="unitDiagOrHerm">If <paramref name="triangular"/>, indicates whether the diagonal elements of <paramref name="A"/> are all ones (thus not stored) or not; or whether <paramref name="A"/> is Hermitian or simply symmetric otherwise</param>
		/// <param name="rows">The number of rows in <typeparamref name="T"/></param>
		/// <param name="cols">The number of columns in <typeparamref name="T"/></param>
		/// <param name="A">The matrix whose columns will be reduced</param>
		/// <param name="lda">The leading dimension of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="op">The <see cref="ReduceOperation"/> to apply to elements of <paramref name="A"/></param>
		/// <param name="x">The vector to store the reduction results of <paramref name="A"/>'s columns under <paramref name="op"/></param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="x"/> is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="lda"/> &lt; <paramref name="rows"/> or <paramref name="strideX"/> ≤ 0</exception>
		[AbstractApiMethod]
		public abstract bool HalfMatrixColumnReduce<T, TS1, TS2>(ReduceOperation op, bool upper, bool triangular, bool unitDiagOrHerm, long rows, long cols, TS1 A, long lda, TS2 x, long strideX) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

		/// <summary>
		/// When implemented by a derived class, compute <c><paramref name="A"/>[i, j] = <paramref name="op"/>(<paramref name="A"/>[i, j], <paramref name="scalar"/>)</c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TS1">The input actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The output actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="upper">Whether all matrices' upper or lower parts are stored</param>
		/// <param name="unitDiag">Whether the diagonal elements of <paramref name="A"/> are all ones (thus not stored) or not</param>
		/// <param name="rows">The number of rows in <typeparamref name="T"/></param>
		/// <param name="cols">The number of columns in <typeparamref name="T"/></param>
		/// <param name="A">The matrix as the first inputs of <paramref name="op"/></param>
		/// <param name="lda">The leading dimension of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="B">The matrix to be overwritten</param>
		/// <param name="ldb">The leading dimension of <paramref name="B"/> in <typeparamref name="T"/></param>
		/// <param name="scalar">The scalar as the second input of <paramref name="op"/></param>
		/// <param name="op">The <see cref="BinaryScalarOperation"/> to apply to each element of <paramref name="A"/> and <paramref name="scalar"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="lda"/> &lt; <paramref name="rows"/></exception>
		[AbstractApiMethod]
		public abstract bool HalfMatrixBinaryScalar<T, TS1, TS2>(BinaryScalarOperation op, bool upper, bool unitDiag, long rows, long cols, T scalar, TS1 A, long lda, TS2 B, long ldb) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

		/// <summary>
		/// When implemented by a derived class, compute <c><paramref name="C"/>[i, j] = <paramref name="op"/>(<paramref name="A"/>[i, j], <paramref name="B"/>[i, j])</c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TS1">The first input actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The second input actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS3">The output actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="upper">Whether all matrices' upper or lower parts are stored</param>
		/// <param name="unitDiag">Whether the diagonal elements of <paramref name="A"/> and <paramref name="B"/> are all ones (thus not stored) or not</param>
		/// <param name="rows">The number of rows in <typeparamref name="T"/></param>
		/// <param name="cols">The number of columns in <typeparamref name="T"/></param>
		/// <param name="A">The matrix act as the first inputs</param>
		/// <param name="lda">The leading dimension of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="B">The matrix act as the second inputs</param>
		/// <param name="ldb">The leading dimension of <paramref name="B"/> in <typeparamref name="T"/></param>
		/// <param name="C">The matrix to be overwritten</param>
		/// <param name="ldc">The leading dimension of <paramref name="C"/> in <typeparamref name="T"/></param>
		/// <param name="op">The <see cref="BinaryOperation"/> to apply to each element of <paramref name="A"/> and <paramref name="B"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="B"/> is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="lda"/> &lt; <paramref name="rows"/> or <paramref name="ldb"/> &lt; <paramref name="rows"/></exception>
		[AbstractApiMethod]
		public abstract bool HalfMatricesBinary<T, TS1, TS2, TS3>(BinaryOperation op, bool upper, bool unitDiag, long rows, long cols, TS1 A, long lda, TS2 B, long ldb, TS3 C, long ldc) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>;

		/// <summary>
		/// When implemented by a derived class, perform partial aggregate (scan) <paramref name="op"/> of the elements in columns of <paramref name="A"/> and write the result to columns <paramref name="B"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS1">The input actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The output actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="upper">Whether all matrices' upper or lower parts are stored</param>
		/// <param name="triangular">Whether <paramref name="A"/> is triangular matrix or a symmetric/Hermitian matrix</param>
		/// <param name="unitDiagOrHerm">If <paramref name="triangular"/>, indicates whether the diagonal elements of <paramref name="A"/> are all ones (thus not stored) or not; or whether <paramref name="A"/> is Hermitian or simply symmetric otherwise</param>
		/// <param name="rows">The number of rows in <typeparamref name="T"/></param>
		/// <param name="cols">The number of columns in <typeparamref name="T"/></param>
		/// <param name="A">The matrix whose columns will be scanned</param>
		/// <param name="lda">The leading dimension of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="B">The matrix whose columns will be overwritten by the scan results of <paramref name="A"/>'s column s</param>
		/// <param name="ldb">The leading dimension of <paramref name="B"/> in <typeparamref name="T"/></param>
		/// <param name="inclusive">Whether to scan <paramref name="A"/> inclusively (the first elements are the first elements of columns <paramref name="A"/>) or exclusively (the first elements are the identity element of <paramref name="op"/>)</param>
		/// <param name="op">The <see cref="BinaryOperation"/> to apply to the partial scan result and each element of <paramref name="A"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="B"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="lda"/> or <paramref name="ldb"/> &lt; <paramref name="rows"/></exception>
		[AbstractApiMethod]
		public abstract bool HalfMatrixColumnScan<T, TS1, TS2>(BinaryOperation op, bool inclusive, bool upper, bool triangular, bool unitDiagOrHerm, long rows, long cols, TS1 A, long lda, TS2 B, long ldb) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

		/// <summary>
		/// When implemented by a derived class, check if all elements in <paramref name="A"/> and <paramref name="B"/> are equal: <c><paramref name="A"/>[i, j] == <paramref name="B"/>[i, j]</c> for all <c>i, j</c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS1">The first actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The second actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="upper">Whether all matrices' upper or lower parts are stored</param>
		/// <param name="ignoreDiag">Whether to ignore the diagonal elements of <paramref name="A"/> and <paramref name="B"/> or not</param>
		/// <param name="rows">The number of rows in <typeparamref name="T"/></param>
		/// <param name="cols">The number of columns in <typeparamref name="T"/></param>
		/// <param name="A">The matrix to be checked</param>
		/// <param name="lda">The leading dimension of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="B">The matrix to be checked</param>
		/// <param name="ldb">The leading dimension of <paramref name="B"/> in <typeparamref name="T"/></param>
		/// <param name="equals">Output <see cref="bool"/> indicating whether all elements in <paramref name="A"/> and <paramref name="B"/> are equal</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="B"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="lda"/> or <paramref name="ldb"/> &lt; <paramref name="rows"/></exception>
		[AbstractApiMethod]
		public abstract bool HalfMatricesEqual<T, TS1, TS2>(bool upper, bool ignoreDiag, long rows, long cols, TS1 A, long lda, TS2 B, long ldb, out bool equals) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

		/// <summary>
		/// When implemented by a derived class, cast the given matrix from type <typeparamref name="TIn"/> to type <typeparamref name="TOut"/>.
		/// </summary>
		/// <typeparam name="TIn">Any unmanaged number as the input data type</typeparam>
		/// <typeparam name="TOut">Any unmanaged number as the output data type</typeparam>
		/// <typeparam name="TSIn">The input actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TSOut">The output actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="upper">Whether all matrices' upper or lower parts are stored</param>
		/// <param name="ignoreDiag">Whether to ignore the diagonal elements of <paramref name="source"/> and <paramref name="destination"/> or not</param>
		/// <param name="source">The source matrix</param>
		/// <param name="rows">The number of rows</param>
		/// <param name="cols">The number of columns</param>
		/// <param name="lds">The leading dimension of <paramref name="source"/></param>
		/// <param name="destination">The destination matrix</param>
		/// <param name="ldd">The leading dimension of <paramref name="destination"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="destination"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="rows"/>, <paramref name="cols"/>, <paramref name="lds"/> or <paramref name="ldd"/> is out of range</exception>
		[AbstractApiMethod]
		public abstract bool HalfMatrixCast<TIn, TOut, TSIn, TSOut>(bool upper, bool ignoreDiag, long rows, long cols, TSIn source, long lds, TSOut destination, long ldd) where TIn : unmanaged, IBaseNumber<TIn> where TOut : unmanaged, IBaseNumber<TOut> where TSIn : class, IStorage<TIn, TSIn> where TSOut : class, IStorage<TOut, TSOut>;
		#endregion
	}
}
