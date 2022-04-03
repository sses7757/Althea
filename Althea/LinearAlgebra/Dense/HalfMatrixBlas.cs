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
		/// <remarks>
		/// The out-of-place addition mode shall be enabled if <paramref name="C"/> is not <paramref name="A"/> nor <paramref name="B"/>. Both <paramref name="opA"/> and <paramref name="opB"/> can have any predefined value.<br/>
		/// The in-place mode shall be enabled if one of the following two operations is identified: <c><paramref name="C"/> = <paramref name="α"/> <paramref name="C"/> + <paramref name="β"/> <paramref name="opB"/>(<paramref name="B"/>)</c> or <c><paramref name="C"/> = <paramref name="α"/> <paramref name="opA"/>(<paramref name="A"/>) + <paramref name="β"/> <paramref name="C"/></c>.<br/>
		/// The out-of-place transposition mode shall be enabled if one of <paramref name="A"/> and <paramref name="B"/> is null or invalid or one of <paramref name="α"/> and <paramref name="β"/> is 0.
		/// </remarks>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS1">The first actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The second actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS3">The third actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="addDiag">Whether to add the matrices' diagonal elements or not</param>
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
		public abstract bool HalfMatricesAdd<T, TS1, TS2, TS3>(bool addDiag, bool upper, MatrixOperation opA, MatrixOperation opB, long m, long n, T α, TS1? A, long lda, T β, TS2? B, long ldb, TS3 C, long ldc) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>;

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
		/// <param name="A">The array of dimensions <c><paramref name="lda"/>×<paramref name="k"/></c> with <c><paramref name="lda"/> ≥ max(0, <paramref name="m"/>)</c> if <paramref name="opA"/> == <see cref="MatrixOperation.None"/>, and <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(0, <paramref name="k"/>)</c> otherwise</param>
		/// <param name="lda">The leading dimension of two-dimensional array used to store the matrix <paramref name="A"/></param>
		/// <param name="B">The array of dimension <c><paramref name="ldb"/>×<paramref name="n"/></c> with <c><paramref name="ldb"/> ≥ max(0, <paramref name="k"/>)</c> if <paramref name="opB"/> == <see cref="MatrixOperation.None"/>, and <c><paramref name="ldb"/>×<paramref name="k"/></c> with <c><paramref name="lda"/> ≥ max(0, <paramref name="n"/>)</c> otherwise</param>
		/// <param name="ldb">The leading dimension of two-dimensional array used to store matrix <paramref name="B"/></param>
		/// <param name="β">The scalar to be multiplied to <paramref name="C"/>. If this is 0, the original values of <paramref name="C"/> will be ignored.</param>
		/// <param name="C">The array of dimensions <c><paramref name="ldc"/>×<paramref name="n"/></c> with <c><paramref name="ldc"/> ≥ max(0, <paramref name="m"/>)</c></param>
		/// <param name="ldc">The leading dimension of a two-dimensional array used to store the matrix <paramref name="C"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="B"/> or <paramref name="C"/> is null or invalid</exception>
		[AbstractApiMethod]
		public abstract bool TriangularMatricesMultiply<T, TS1, TS2, TS3>(bool unitDiag, bool upper, MatrixOperation opA, MatrixOperation opB, long m, long n, long k, T α, TS1 A, long lda, TS2 B, long ldb, T β, TS3 C, long ldc) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>;

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
		public abstract bool SymmetricMatrixToNormal<T, TS>(bool upper, bool hermitian, long n, TS A, long lda) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <summary>
		/// When implemented by a derived class, clear the matrix <paramref name="A"/>'s upper or lower part (not including the diagonal elements) to 0.
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
		public abstract bool HalfMatrixClearPart<T, TS>(bool clearDiag, bool clearLower, long m, long n, TS A, long lda) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <summary>
		/// When implemented by a derived class, copy the matrix <paramref name="opA"/>(<paramref name="A"/>)'s upper or lower part to <paramref name="B"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS1">The actual input storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The actual output storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="copyDiag">Whether to copy the diagonal of <paramref name="A"/> or not</param>
		/// <param name="upper">Whether the upper triangular part of <paramref name="A"/> is stored or its lower part</param>
		/// <param name="opA">The <see cref="MatrixOperation"/> to indicate the simple operation of <paramref name="A"/></param>
		/// <param name="m">The number of rows of <paramref name="A"/> and <paramref name="B"/></param>
		/// <param name="n">The number of columns of <paramref name="A"/> and <paramref name="B"/></param>
		/// <param name="A">The input matrix with size <paramref name="m"/>×<paramref name="n"/></param>
		/// <param name="lda">The leading dimension of <paramref name="A"/>, must be at least <paramref name="m"/></param>
		/// <param name="B">The output matrix with size <paramref name="n"/>×<paramref name="n"/></param>
		/// <param name="ldb">The leading dimension of <paramref name="B"/>, must be at least <paramref name="m"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> is null or invalid</exception>
		[AbstractApiMethod]
		public abstract bool HalfMatrixCopy<T, TS1, TS2>(bool upper, bool copyDiag, MatrixOperation opA, long m, long n, TS1 A, long lda, TS2 B, long ldb) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;
		#endregion

		#region matrix math
		/// <summary>
		/// When implemented by a derived class, fill the matrix <paramref name="A"/>'s values by same <paramref name="value"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TS">The actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="unitDiag">Whether all matrices are of unit diagonal or not</param>
		/// <param name="upperA">Whether <paramref name="A"/>'s upper half or lower half is stored</param>
		/// <param name="A">The matrix to be filled</param>
		/// <param name="value">The value to set as a <typeparamref name="T"/></param>
		/// <param name="rows">The number of rows of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="cols">The number of columns of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="ld">The leading dimension of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="rows"/>, <paramref name="cols"/> or <paramref name="ld"/> is out of range</exception>
		[AbstractApiMethod]
		public abstract bool HalfMatrixFill<T, TS>(bool unitDiag, TS A, bool upperA, long ld, T value, long rows, long cols) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <summary>
		/// When implemented by a derived class, check if all elements in matrices <paramref name="A"/> and <paramref name="B"/> are equal.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS1">The first actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The second actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="unitDiag">Whether all matrices are of unit diagonal or not</param>
		/// <param name="upperA">Whether <paramref name="A"/>'s upper half or lower half is stored</param>
		/// <param name="upperB">Whether <paramref name="B"/>'s upper half or lower half is stored</param>
		/// <param name="A">The matrix to be checked</param>
		/// <param name="rows">The number of rows of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="cols">The number of columns of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="lda">The leading dimension of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="B">The other matrix to be checked</param>
		/// <param name="ldb">The leading dimension of <paramref name="B"/> in <typeparamref name="T"/></param>
		/// <param name="equals">Output <see cref="bool"/> indicating whether all elements in <paramref name="A"/> and <paramref name="B"/> are equal</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="B"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="rows"/>, <paramref name="cols"/>, <paramref name="lda"/> or <paramref name="ldb"/> is out of range</exception>
		[AbstractApiMethod]
		public abstract bool HalfMatricesEquals<T, TS1, TS2>(bool unitDiag, TS1 A, bool upperA, long lda, TS2 B, bool upperB, long ldb, long rows, long cols, out bool equals) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

		/// <summary>
		/// When implemented by a derived class, compute <c><paramref name="A"/> = <paramref name="A"/>.*<paramref name="B"/></c> (point-wise multiplication).
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS1">The first actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The second actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="unitDiag">Whether all matrices are of unit diagonal or not</param>
		/// <param name="upperA">Whether <paramref name="A"/>'s upper half or lower half is stored</param>
		/// <param name="upperB">Whether <paramref name="B"/>'s upper half or lower half is stored</param>
		/// <param name="A">The matrix to be multiplied in-place</param>
		/// <param name="rows">The number of rows of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="cols">The number of columns of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="lda">The leading dimension of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="B">The other matrix to multiply</param>
		/// <param name="ldb">The leading dimension of <paramref name="B"/> in <typeparamref name="T"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="B"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="rows"/>, <paramref name="cols"/>, <paramref name="lda"/> or <paramref name="ldb"/> is out of range</exception>
		[AbstractApiMethod]
		public abstract bool HalfMatricesMultiply<T, TS1, TS2>(bool unitDiag, TS1 A, bool upperA, long lda, TS2 B, bool upperB, long ldb, long rows, long cols) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

		/// <summary>
		/// When implemented by a derived class, compute <c><paramref name="A"/> = <paramref name="A"/>./<paramref name="B"/></c> (point-wise division).
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS1">The first actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The second actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="unitDiag">Whether all matrices are of unit diagonal or not</param>
		/// <param name="upperA">Whether <paramref name="A"/>'s upper half or lower half is stored</param>
		/// <param name="upperB">Whether <paramref name="B"/>'s upper half or lower half is stored</param>
		/// <param name="A">The matrix to be divided in-place</param>
		/// <param name="rows">The number of rows of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="cols">The number of columns of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="lda">The leading dimension of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="B">The other matrix to divide</param>
		/// <param name="ldb">The leading dimension of <paramref name="B"/> in <typeparamref name="T"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="B"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="rows"/>, <paramref name="cols"/>, <paramref name="lda"/> or <paramref name="ldb"/> is out of range</exception>
		[AbstractApiMethod]
		public abstract bool HalfMatricesDivide<T, TS1, TS2>(bool unitDiag, TS1 A, bool upperA, long lda, TS2 B, bool upperB, long ldb, long rows, long cols) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

		/// <summary>
		/// When implemented by a derived class, <c><paramref name="A"/> = <paramref name="A"/>.^<paramref name="p"/></c> (point-wise power).
		/// </summary>
		/// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TS">The actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="unitDiag">Whether all matrices are of unit diagonal or not</param>
		/// <param name="upperA">Whether <paramref name="A"/>'s upper half or lower half is stored</param>
		/// <param name="A">The matrix to be powered in-place</param>
		/// <param name="p">The exponent as a <typeparamref name="T"/></param>
		/// <param name="rows">The number of rows of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="cols">The number of columns of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="ld">The leading dimension of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="rows"/>, <paramref name="cols"/> or <paramref name="ld"/> is out of range</exception>
		[AbstractApiMethod]
		public abstract bool HalfMatrixPower<T, TS>(bool unitDiag, TS A, bool upperA, long ld, T p, long rows, long cols) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <summary>
		/// When implemented by a derived class, <c><paramref name="A"/> = <paramref name="A"/> + <paramref name="scalar"/></c> (point-wise addition).
		/// </summary>
		/// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TS">The actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="unitDiag">Whether all matrices are of unit diagonal or not</param>
		/// <param name="upperA">Whether <paramref name="A"/>'s upper half or lower half is stored</param>
		/// <param name="A">The matrix to be added in-place</param>
		/// <param name="scalar">The scalar to add</param>
		/// <param name="rows">The number of rows of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="cols">The number of columns of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="ld">The leading dimension of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="rows"/>, <paramref name="cols"/> or <paramref name="ld"/> is out of range</exception>
		[AbstractApiMethod]
		public abstract bool HalfMatrixAddScalar<T, TS>(bool unitDiag, TS A, bool upperA, long ld, T scalar, long rows, long cols) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <summary>
		/// When implemented by a derived class, cast the given matrix from type <typeparamref name="TIn"/> to type <typeparamref name="TOut"/>.
		/// </summary>
		/// <typeparam name="TIn">Any unmanaged number as the input data type</typeparam>
		/// <typeparam name="TOut">Any unmanaged number as the output data type</typeparam>
		/// <typeparam name="TSIn">The input actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TSOut">The output actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="unitDiag">Whether all matrices are of unit diagonal or not</param>
		/// <param name="upperSrc">Whether <paramref name="source"/>'s upper half or lower half is stored</param>
		/// <param name="upperDst">Whether <paramref name="destination"/>'s upper half or lower half is stored</param>
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
		public abstract bool HalfMatrixCast<TIn, TOut, TSIn, TSOut>(bool unitDiag, TSIn source, bool upperSrc, long lds, TSOut destination, bool upperDst, long ldd, long rows, long cols) where TIn : unmanaged, INumber<TIn> where TOut : unmanaged, INumber<TOut> where TSIn : class, IStorage<TIn, TSIn> where TSOut : class, IStorage<TOut, TSOut>;

		/// <summary>
		/// When implemented by a derived class, truncate the matrix by comparing each element's absolute value in <paramref name="A"/> to the given <paramref name="threshold"/>, if it is smaller than <paramref name="threshold"/>, it will be set to 0.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TS">The actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="unitDiag">Whether all matrices are of unit diagonal or not</param>
		/// <param name="upperA">Whether <paramref name="A"/>'s upper half or lower half is stored</param>
		/// <param name="A">The matrix to be truncated in-place</param>
		/// <param name="threshold">If any element's absolute value is smaller than <paramref name="threshold"/>, it will be set to 0</param>
		/// <param name="rows">The number of rows of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="cols">The number of columns of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="ld">The leading dimension of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="rows"/>, <paramref name="cols"/> or <paramref name="ld"/> is out of range</exception>
		[AbstractApiMethod]
		public abstract bool HalfMatrixTruncate<T, TS>(bool unitDiag, TS A, bool upperA, long ld, double threshold, long rows, long cols) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <summary>
		/// When implemented by a derived class, aggregately sum the elements in matrix <paramref name="A"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TS">The actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="unitDiag">Whether all matrices are of unit diagonal or not</param>
		/// <param name="upperA">Whether <paramref name="A"/>'s upper half or lower half is stored</param>
		/// <param name="A">The matrix to sum</param>
		/// <param name="rows">The number of rows of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="cols">The number of columns of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="ld">The leading dimension of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="sum">Output the sum as a <typeparamref name="T"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="rows"/>, <paramref name="cols"/> or <paramref name="ld"/> is out of range</exception>
		[AbstractApiMethod]
		public abstract bool TriangularMatrixSum<T, TS>(bool unitDiag, TS A, bool upperA, long ld, long rows, long cols, out T sum) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <summary>
		/// When implemented by a derived class, aggregately sum the absolute values of the elements in matrix <paramref name="A"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TS">The actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="unitDiag">Whether all matrices are of unit diagonal or not</param>
		/// <param name="upperA">Whether <paramref name="A"/>'s upper half or lower half is stored</param>
		/// <param name="A">The matrix to sum</param>
		/// <param name="rows">The number of rows of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="cols">The number of columns of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="ld">The leading dimension of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="sum">Output the sum as a <typeparamref name="T"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="rows"/>, <paramref name="cols"/> or <paramref name="ld"/> is out of range</exception>
		[AbstractApiMethod]
		public abstract bool TriangularMatrixAbsSum<T, TS>(bool unitDiag, TS A, bool upperA, long ld, long rows, long cols, out T sum) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <summary>
		/// When implemented by a derived class, aggregately sum the elements in matrix <paramref name="A"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TS">The actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="symmHerm">Whether all matrices are symmetric or Hermitian</param>
		/// <param name="upperA">Whether <paramref name="A"/>'s upper half or lower half is stored</param>
		/// <param name="A">The matrix to sum</param>
		/// <param name="rows">The number of rows of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="cols">The number of columns of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="ld">The leading dimension of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="sum">Output the sum as a <typeparamref name="T"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="rows"/>, <paramref name="cols"/> or <paramref name="ld"/> is out of range</exception>
		[AbstractApiMethod]
		public abstract bool SymmetricMatrixSum<T, TS>(bool symmHerm, TS A, bool upperA, long ld, long rows, long cols, out T sum) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <summary>
		/// When implemented by a derived class, aggregately sum the absolute values of the elements in matrix <paramref name="A"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TS">The actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="symmHerm">Whether all matrices are symmetric or Hermitian</param>
		/// <param name="upperA">Whether <paramref name="A"/>'s upper half or lower half is stored</param>
		/// <param name="A">The matrix to sum</param>
		/// <param name="rows">The number of rows of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="cols">The number of columns of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="ld">The leading dimension of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="sum">Output the sum as a <typeparamref name="T"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="rows"/>, <paramref name="cols"/> or <paramref name="ld"/> is out of range</exception>
		[AbstractApiMethod]
		public abstract bool SymmetricMatrixAbsSum<T, TS>(bool symmHerm, TS A, bool upperA, long ld, long rows, long cols, out T sum) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <summary>
		/// When implemented by a derived class, compute the norm of the elements in matrix <paramref name="A"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TS">The actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="unitDiag">Whether all matrices are of unit diagonal or not</param>
		/// <param name="upperA">Whether <paramref name="A"/>'s upper half or lower half is stored</param>
		/// <param name="A">The matrix to sum</param>
		/// <param name="rows">The number of rows of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="cols">The number of columns of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="ld">The leading dimension of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="norm">Output the norm as a <typeparamref name="T"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="rows"/>, <paramref name="cols"/> or <paramref name="ld"/> is out of range</exception>
		[AbstractApiMethod]
		public abstract bool TriangularMatrixNorm<T, TS>(bool unitDiag, TS A, bool upperA, long ld, long rows, long cols, out T norm) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <summary>
		/// When implemented by a derived class, compute the norm of the elements in matrix <paramref name="A"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TS">The actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="symmHerm">Whether all matrices are symmetric or Hermitian</param>
		/// <param name="upperA">Whether <paramref name="A"/>'s upper half or lower half is stored</param>
		/// <param name="A">The matrix to sum</param>
		/// <param name="rows">The number of rows of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="cols">The number of columns of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="ld">The leading dimension of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="norm">Output the norm as a <typeparamref name="T"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="rows"/>, <paramref name="cols"/> or <paramref name="ld"/> is out of range</exception>
		[AbstractApiMethod]
		public abstract bool SymmetricMatrixNorm<T, TS>(bool symmHerm, TS A, bool upperA, long ld, long rows, long cols, out T norm) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <summary>
		/// When implemented by a derived class, aggregately product the elements in matrix <paramref name="A"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TS">The actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="symmHerm">Whether all matrices are symmetric or Hermitian</param>
		/// <param name="upperA">Whether <paramref name="A"/>'s upper half or lower half is stored</param>
		/// <param name="A">The matrix to product</param>
		/// <param name="rows">The number of rows of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="cols">The number of columns of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="ld">The leading dimension of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="product">Output the product as a <typeparamref name="T"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="rows"/>, <paramref name="cols"/> or <paramref name="ld"/> is out of range</exception>
		[AbstractApiMethod]
		public abstract bool SymmetricMatrixProduct<T, TS>(bool symmHerm, TS A, bool upperA, long ld, long rows, long cols, out T product) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <summary>
		/// When implemented by a derived class, get the index of the index of the element with largest absolute value in matrix <paramref name="A"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TS">The actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="unitDiag">Whether all matrices are of unit diagonal or not</param>
		/// <param name="upperA">Whether <paramref name="A"/>'s upper half or lower half is stored</param>
		/// <param name="A">The matrix to sum</param>
		/// <param name="rows">The number of rows of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="cols">The number of columns of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="ld">The leading dimension of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="index">Output the index compared to <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="rows"/>, <paramref name="cols"/> or <paramref name="ld"/> is out of range</exception>
		[AbstractApiMethod]
		public abstract bool TriangularMatrixAbsArgMax<T, TS>(bool unitDiag, TS A, bool upperA, long ld, long rows, long cols, out long index) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <summary>
		/// When implemented by a derived class, get the index of the element with largest absolute value in matrix <paramref name="A"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TS">The actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="symmHerm">Whether all matrices are symmetric or Hermitian</param>
		/// <param name="upperA">Whether <paramref name="A"/>'s upper half or lower half is stored</param>
		/// <param name="A">The matrix to sum</param>
		/// <param name="rows">The number of rows of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="cols">The number of columns of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="ld">The leading dimension of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="index">Output the index compared to <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="rows"/>, <paramref name="cols"/> or <paramref name="ld"/> is out of range</exception>
		[AbstractApiMethod]
		public abstract bool SymmetricMatrixAbsArgMax<T, TS>(bool symmHerm, TS A, bool upperA, long ld, long rows, long cols, out long index) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <summary>
		/// When implemented by a derived class, get the index of the element with smallest absolute value in matrix <paramref name="A"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TS">The actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="symmHerm">Whether all matrices are symmetric or Hermitian</param>
		/// <param name="upperA">Whether <paramref name="A"/>'s upper half or lower half is stored</param>
		/// <param name="A">The matrix to sum</param>
		/// <param name="rows">The number of rows of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="cols">The number of columns of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="ld">The leading dimension of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="index">Output the index compared to <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="rows"/>, <paramref name="cols"/> or <paramref name="ld"/> is out of range</exception>
		[AbstractApiMethod]
		public abstract bool SymmetricMatrixAbsArgMin<T, TS>(bool symmHerm, TS A, bool upperA, long ld, long rows, long cols, out long index) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <summary>
		/// When implemented by a derived class, aggregately sum the elements in each columns of matrix <paramref name="A"/> to vector <paramref name="x"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TS1">The first actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The second actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="symmHerm">Whether all matrices are symmetric or Hermitian (true or false) or triangular (null)</param>
		/// <param name="unitDiag">Whether all matrices are of unit diagonal or not</param>
		/// <param name="upperA">Whether <paramref name="A"/>'s upper half or lower half is stored</param>
		/// <param name="A">The matrix to sum</param>
		/// <param name="rows">The number of rows of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="cols">The number of columns of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="ld">The leading dimension of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="x">The output vector to store the sums</param>
		/// <param name="stride">The stride between consecutive elements of <paramref name="x"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="rows"/>, <paramref name="cols"/>, <paramref name="ld"/> or <paramref name="stride"/> is out of range</exception>
		[AbstractApiMethod]
		public abstract bool HalfMatrixSumColumns<T, TS1, TS2>(bool? symmHerm, bool unitDiag, TS1 A, bool upperA, long ld, long rows, long cols, TS2 x, long stride) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1>;

		/// <summary>
		/// When implemented by a derived class, aggregately product the elements in each columns of matrix <paramref name="A"/> to vector <paramref name="x"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TS1">The first actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The second actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="symmHerm">Whether all matrices are symmetric or Hermitian</param>
		/// <param name="upperA">Whether <paramref name="A"/>'s upper half or lower half is stored</param>
		/// <param name="A">The matrix to product</param>
		/// <param name="rows">The number of rows of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="cols">The number of columns of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="ld">The leading dimension of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="x">The output vector to store the products</param>
		/// <param name="stride">The stride between consecutive elements of <paramref name="x"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="rows"/>, <paramref name="cols"/>, <paramref name="ld"/> or <paramref name="stride"/> is out of range</exception>
		[AbstractApiMethod]
		public abstract bool SymmetricMatrixProductColumns<T, TS1, TS2>(bool symmHerm, TS1 A, bool upperA, long ld, long rows, long cols, TS2 x, long stride) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1>;
		#endregion
	}
}
