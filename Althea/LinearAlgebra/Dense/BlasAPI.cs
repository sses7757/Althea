using Althea.SourceGenerator;
using Althea.Storage;


namespace Althea.LinearAlgebra.Dense
{
	/// <summary>
	/// The abstract interface for dense linear algebra BLAS API routines 
	/// </summary>
	[AbstractRuntimeApi]
	public interface IBlasAbstractApi : IAbstractRuntimeApi<IBlasAbstractApi>
	{
		#region BLAS level 1
		/// <summary>
		/// When implemented by a derived class, find the (smallest) index of the element with the maximum magnitude.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS">The actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="x">The vector of type <typeparamref name="T"/></param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="index">Output the resulting index</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> ≤ 0</exception>
		[AbstractApiMethod]
		public abstract bool AbsoluteValueArgMax<T, TS>(TS x, long strideX, out long index) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;
		
		/// <summary>
		/// When implemented by a derived class, find the (smallest) index of the element with the minimum magnitude.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS">The actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="x">The vector of type <typeparamref name="T"/></param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="index">Output the resulting index</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> ≤ 0</exception>
		[AbstractApiMethod]
		public abstract bool AbsoluteValueArgMin<T, TS>(TS x, long strideX, out long index) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <summary>
		/// When implemented by a derived class, compute the sum of the absolute values of the elements of vector <paramref name="x"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS">The actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="x">The vector of type <typeparamref name="T"/></param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="sum">Output the result as a <typeparamref name="T"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> ≤ 0</exception>
		[AbstractApiMethod]
		public abstract bool AbsoluteValueSum<T, TS>(TS x, long strideX, out T sum) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <summary>
		/// When implemented by a derived class, compute the Euclidean norm (2-norm) of the vector <paramref name="x"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS">The actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="x">The vector of type <typeparamref name="T"/></param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="norm">Output the result value as a <typeparamref name="T"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> ≤ 0</exception>
		[AbstractApiMethod]
		public abstract bool Norm<T, TS>(TS x, long strideX, out T norm) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <summary>
		/// When implemented by a derived class, in-place scale the vector <paramref name="x"/> by <paramref name="scalar"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS">The actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="x">The vector of type <typeparamref name="T"/></param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="scalar">The scalar used for multiplication</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> ≤ 0</exception>
		[AbstractApiMethod]
		public abstract bool Scale<T, TS>(TS x, long strideX, T scalar) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <summary>
		/// When implemented by a derived class, multiply the vector <paramref name="x"/> by the scalar <paramref name="α"/> and in-place add it to the vector <paramref name="y"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS1">The first actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The second actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="α">The scalar used for multiplication</param>
		/// <param name="x">The vector of type <typeparamref name="T"/></param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="y">The another vector of type <typeparamref name="T"/></param>
		/// <param name="strideY">The stride between consecutive elements of <paramref name="y"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> or <paramref name="strideY"/> ≤ 0</exception>
		[AbstractApiMethod]
		public abstract bool Add<T, TS1, TS2>(T α, TS1 x, long strideX, TS2 y, long strideY) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

		/// <summary>
		/// When implemented by a derived class, compute the dot (inner) product of vectors <paramref name="x"/> and <paramref name="y"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS1">The first actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The second actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="conjX">Conjugate <paramref name="x"/> or not</param>
		/// <param name="x">The vector of type <typeparamref name="T"/></param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="y">The another vector of type <typeparamref name="T"/></param>
		/// <param name="strideY">The stride between consecutive elements of <paramref name="y"/></param>
		/// <param name="dot">Output the result value as a <typeparamref name="T"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> or <paramref name="strideY"/> ≤ 0</exception>
		[AbstractApiMethod]
		public abstract bool Dot<T, TS1, TS2>(bool conjX, TS1 x, long strideX, TS2 y, long strideY, out T dot) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;
		#endregion

		#region BLAS level 2
		/// <summary>
		/// When implemented by a derived class, perform the matrix-vector multiplication: <paramref name="y"/> = <paramref name="α"/> * <paramref name="op"/>(<paramref name="A"/>) * <paramref name="x"/> + <paramref name="β"/> * <paramref name="y"/>.<br/>
		/// Where <paramref name="A"/> is a <paramref name="m"/>×<paramref name="n"/> matrix stored in column-major format, <paramref name="x"/> and <paramref name="y"/> are vectors, and <paramref name="α"/> and <paramref name="β"/> are scalars.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TSM">The matrix's actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TSV1">The first vector's actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TSV2">The second vector's actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="op">The <see cref="MatrixOperation"/> that is non- or (conj.) transpose</param>
		/// <param name="m">The number of rows of matrix <paramref name="A"/></param>
		/// <param name="n">The number of columns of matrix <paramref name="A"/></param>
		/// <param name="α">The scalar to be multiplied to <paramref name="A"/></param>
		/// <param name="A">The input array of dimension <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="m"/>)</c></param>
		/// <param name="lda">The leading dimension of the two-dimensional array used to store matrix <paramref name="A"/></param>
		/// <param name="x">The vector of length at least <c>(1+(<paramref name="n"/>-1)*<paramref name="strideX"/>)</c> elements if <paramref name="op"/> == <see cref="MatrixOperation.None"/> or <c>(1+(<paramref name="m"/>-1)*<paramref name="strideX"/>)</c> otherwise</param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="β">The scalar to be multiplied to <paramref name="y"/>. If this is 0, then the original values of <paramref name="y"/> will be ignored.</param>
		/// <param name="y">The input and output vector at least <c>(1+(<paramref name="m"/>-1)*<paramref name="strideY"/>)</c> elements if <paramref name="op"/> == <see cref="MatrixOperation.None"/> or <c>(1+(<paramref name="n"/>-1)*<paramref name="strideY"/>)</c> otherwise</param>
		/// <param name="strideY">The stride between consecutive elements of <paramref name="y"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> or <paramref name="A"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> or <paramref name="strideY"/> ≤ 0</exception>
		[AbstractApiMethod]
		public abstract bool GeneralMatrixMultiplyVector<T, TSM, TSV1, TSV2>(MatrixOperation op, long m, long n, T α, TSM A, long lda, TSV1 x, long strideX, T β, TSV2 y, long strideY) where T : unmanaged, INumber<T> where TSM : class, IStorage<T, TSM> where TSV1 : class, IStorage<T, TSV1> where TSV2 : class, IStorage<T, TSV2>;

		/// <summary>
		/// When implemented by a derived class, perform the symmetric/hermitian matrix-vector multiplication:<br/>
		/// <c><paramref name="y"/> = <paramref name="α"/> * <paramref name="A"/> * <paramref name="x"/> + <paramref name="β"/> * <paramref name="y"/></c>.<br/>
		/// Where <paramref name="A"/> is a <paramref name="n"/>×<paramref name="n"/> symmetric/hermitian matrix stored in column-major format, <paramref name="x"/> is a vector, and <paramref name="α"/> is a scalar.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TSM">The matrix's actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TSV1">The first vector's actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TSV2">The second vector's actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="fillUpper">The indicates whether <paramref name="A"/>'s upper or lower part is stored</param>
		/// <param name="hermA">Whether <paramref name="A"/> is a hermitian or a symmetric matrix</param>
		/// <param name="n">The number of rows and columns of matrix <paramref name="A"/></param>
		/// <param name="α">The scalar used for multiplication</param>
		/// <param name="A">The array of dimension <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="n"/>)</c></param>
		/// <param name="lda">The leading dimension of two-dimensional array used to store matrix <paramref name="A"/></param>
		/// <param name="x">The vector with <c>(1+(<paramref name="n"/>-1)*abs(<paramref name="strideX"/>))</c> elements</param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="β">The scalar to be multiplied to <paramref name="y"/>. If this is 0, then the original values of <paramref name="y"/> will be ignored.</param>
		/// <param name="y">The input and output vector at least <c>(1+(<paramref name="n"/>-1)*abs(<paramref name="strideY"/>))</c></param>
		/// <param name="strideY">The stride between consecutive elements of <paramref name="y"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> or <paramref name="A"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> or <paramref name="strideY"/> ≤ 0</exception>
		[AbstractApiMethod]
		public abstract bool SymmetricMatrixMultiplyVector<T, TSM, TSV1, TSV2>(bool fillUpper, bool hermA, long n, T α, TSM A, long lda, TSV1 x, long strideX, T β, TSV2 y, long strideY) where T : unmanaged, INumber<T> where TSM : class, IStorage<T, TSM> where TSV1 : class, IStorage<T, TSV1> where TSV2 : class, IStorage<T, TSV2>;

		/// <summary>
		/// When implemented by a derived class, perform the triangular matrix multiply:<br/>
		/// <c><paramref name="y"/> = <paramref name="α"/> * <paramref name="op"/>(<paramref name="A"/>) * <paramref name="x"/> + <paramref name="β"/> * <paramref name="y"/></c><br/>
		/// Where <paramref name="A"/> is a <paramref name="n"/>×<paramref name="n"/> upper/lower triangular matrix stored in column-major format and <paramref name="x"/> is a vector.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TSM">The matrix's actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TSV1">The input vector's actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TSV2">The output vector's actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="α">The scalar to multiply to <paramref name="A"/> during computation</param>
		/// <param name="β">The scalar to multiply to <paramref name="y"/> during computation</param>
		/// <param name="fillUpper">Whether <paramref name="A"/> is upper or lower triangular</param>
		/// <param name="unitDiag">Whether the diagonal elements of <paramref name="A"/> are all unit (1) or not</param>
		/// <param name="op">The <see cref="MatrixOperation"/> to indicate the simple operation of <paramref name="A"/></param>
		/// <param name="m">The number of rows of matrix <paramref name="A"/></param>
		/// <param name="n">The number of columns of matrix <paramref name="A"/></param>
		/// <param name="x">The input dense vector</param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="y">The output dense vector</param>
		/// <param name="strideY">The stride between consecutive elements of <paramref name="y"/></param>
		/// <param name="A">The array of dimension <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="n"/>)</c></param>
		/// <param name="lda">The leading dimension of two-dimensional array used to store matrix <paramref name="A"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="A"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> ≤ 0</exception>
		[AbstractApiMethod]
		public abstract bool TriangularMatrixMultiplyVector<T, TSM, TSV1, TSV2>(bool fillUpper, bool unitDiag, MatrixOperation op, long m, long n, TSM A, long lda, T α, TSV1 x, long strideX, T β, TSV2 y, long strideY) where T : unmanaged, INumber<T> where TSM : class, IStorage<T, TSM> where TSV1 : class, IStorage<T, TSV1> where TSV2 : class, IStorage<T, TSV2>;

		/// <summary>
		/// When implemented by a derived class, perform the rank-1 update:<br/>
		/// <c><paramref name="A"/> = <paramref name="α"/> * <paramref name="x"/> * <paramref name="y"/>^op + <paramref name="β"/> * <paramref name="A"/></c>, <c>op = <paramref name="conjY"/> ? H : T</c>.<br/>
		/// Where <paramref name="A"/> is a <paramref name="m"/>×<paramref name="n"/> matrix stored in column-major format, <paramref name="x"/> and <paramref name="y"/> are vectors, and <paramref name="α"/> is a scalar.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TSM">The matrix's actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TSV1">The first vector's actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TSV2">The second vector's actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="conjY">Conjugate <paramref name="y"/> or not</param>
		/// <param name="m">The number of rows of matrix <paramref name="A"/></param>
		/// <param name="n">The number of columns of matrix <paramref name="A"/></param>
		/// <param name="α">The scalar used for multiplication</param>
		/// <param name="x">The vector with <c>(1+(<paramref name="m"/>-1)*<paramref name="strideX"/>)</c> elements</param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="y">The vector with <c>(1+(<paramref name="n"/>-1)*<paramref name="strideY"/>)</c> elements</param>
		/// <param name="strideY">The stride between consecutive elements of <paramref name="y"/></param>
		/// <param name="β">The scalar to be multiplied to <paramref name="A"/>. If this is 0, then the original values of <paramref name="A"/> will be ignored.</param>
		/// <param name="A">The input and output array of dimension <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="m"/>)</c></param>
		/// <param name="lda">The leading dimension of two-dimensional array used to store matrix <paramref name="A"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> or <paramref name="A"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> or <paramref name="strideY"/> ≤ 0</exception>
		[AbstractApiMethod]
		public abstract bool GeneralRankOneUpdate<T, TSM, TSV1, TSV2>(bool conjY, long m, long n, T α, TSV1 x, long strideX, TSV2 y, long strideY, T β, TSM A, long lda) where T : unmanaged, INumber<T> where TSM : class, IStorage<T, TSM> where TSV1 : class, IStorage<T, TSV1> where TSV2 : class, IStorage<T, TSV2>;

		/// <summary>
		/// When implemented by a derived class, perform the symmetric/hermitian rank-1 update:<br/>
		/// <c><paramref name="A"/> = <paramref name="α"/> * <paramref name="x"/> * <paramref name="x"/>^op + <paramref name="β"/> * <paramref name="A"/></c>, <c>op = <paramref name="conjX"/> ? H : T</c>.<br/>
		/// Where <paramref name="A"/> is a <paramref name="n"/>×<paramref name="n"/> symmetric/hermitian matrix stored in column-major format, <paramref name="x"/> is a vector, and <paramref name="α"/> is a scalar.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TSM">The matrix's actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TSV">The vector's actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="fillUpper">Whether the result symmetric matrix <paramref name="A"/> shall be stored in its upper or the lower part</param>
		/// <param name="conjX">Conjugate the second <paramref name="x"/> or not</param>
		/// <param name="n">The number of rows and columns of matrix <paramref name="A"/></param>
		/// <param name="α">The scalar used for multiplication</param>
		/// <param name="x">The vector with <c>(1+(<paramref name="n"/>-1)*<paramref name="strideX"/>)</c> elements</param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="β">The scalar to be multiplied to <paramref name="A"/>. If this is 0, then the original values of <paramref name="A"/> will be ignored.</param>
		/// <param name="A">The array of dimension <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="n"/>)</c></param>
		/// <param name="lda">The leading dimension of two-dimensional array used to store matrix <paramref name="A"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="A"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> ≤ 0</exception>
		[AbstractApiMethod]
		public abstract bool SymmetricRankOneUpdate<T, TSM, TSV>(bool fillUpper, bool conjX, long n, T α, TSV x, long strideX, T β, TSM A, long lda) where T : unmanaged, INumber<T> where TSM : class, IStorage<T, TSM> where TSV : class, IStorage<T, TSV>;

		/// <summary>
		/// When implemented by a derived class, perform the symmetric/hermitian rank-2 update:<br/>
		/// <c><paramref name="A"/> = <paramref name="α"/> * (<paramref name="x"/> * <paramref name="y"/>^op + <paramref name="x"/>^op * <paramref name="y"/>) + <paramref name="β"/> * <paramref name="A"/></c>, <c>op = <paramref name="conjugate"/> ? H : T</c>.<br/>
		/// Where <paramref name="A"/> is a <paramref name="n"/>×<paramref name="n"/> symmetric/hermitian matrix stored in column-major format, <paramref name="x"/> is a vector, and <paramref name="α"/> is a scalar.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TSM">The matrix's actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TSV1">The first vector's actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TSV2">The second vector's actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="fillUpper">Whether the result symmetric matrix <paramref name="A"/> shall be stored in its upper or the lower part</param>
		/// <param name="conjugate">Conjugate the vectors <paramref name="x"/> and <paramref name="y"/> or not</param>
		/// <param name="n">The number of rows and columns of matrix <paramref name="A"/></param>
		/// <param name="α">The scalar used for multiplication</param>
		/// <param name="x">The left vector with <c>(1+(<paramref name="n"/>-1)*<paramref name="strideX"/>)</c> elements</param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="y">The right vector with <c>(1+(<paramref name="n"/>-1)*<paramref name="strideY"/>)</c> elements</param>
		/// <param name="strideY">The stride between consecutive elements of <paramref name="y"/></param>
		/// <param name="β">The scalar to be multiplied to <paramref name="A"/>. If this is 0, then the original values of <paramref name="A"/> will be ignored.</param>
		/// <param name="A">The array of dimension <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="n"/>)</c></param>
		/// <param name="lda">The leading dimension of two-dimensional array used to store matrix <paramref name="A"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> or <paramref name="A"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> or <paramref name="strideY"/> ≤ 0</exception>
		[AbstractApiMethod]
		public abstract bool SymmetricRankTwoUpdate<T, TSM, TSV1, TSV2>(bool fillUpper, bool conjugate, long n, T α, TSV1 x, long strideX, TSV2 y, long strideY, T β, TSM A, long lda) where T : unmanaged, INumber<T> where TSM : class, IStorage<T, TSM> where TSV1 : class, IStorage<T, TSV1> where TSV2 : class, IStorage<T, TSV2>;
		#endregion

		#region BLAS level 3
		/// <summary>
		/// When implemented by a derived class, perform the matrix-matrix multiplication:<br/>
		/// <paramref name="C"/> = <paramref name="α"/> * <paramref name="opA"/>(<paramref name="A"/>) * <paramref name="opB"/>(<paramref name="B"/>) + <paramref name="β"/> * <paramref name="C"/>.<br/>
		/// Where <paramref name="α"/> and <paramref name="β"/> are scalars; <paramref name="A"/>, <paramref name="B"/> and <paramref name="C"/> are matrices stored in column-major format with dimensions <paramref name="opA"/>(<paramref name="A"/>) → <paramref name="m"/>×<paramref name="k"/>, <paramref name="opB"/>(<paramref name="B"/>) → <paramref name="k"/>×<paramref name="n"/> and <paramref name="C"/> → <paramref name="m"/>×<paramref name="n"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS1">The first actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The second actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS3">The third actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
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
		public abstract bool GeneralMatricesMultiply<T, TS1, TS2, TS3>(MatrixOperation opA, MatrixOperation opB, long m, long n, long k, T α, TS1 A, long lda, TS2 B, long ldb, T β, TS3 C, long ldc) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>;

		/// <summary>
		/// When implemented by a derived class, perform the symmetric/hermitian matrix-matrix multiplication:<br/>
		/// If <paramref name="leftA"/> is true, <c><paramref name="C"/> = <paramref name="α"/> * <paramref name="opA"/>(<paramref name="A"/>) * <paramref name="opB"/>(<paramref name="B"/>) + <paramref name="β"/> * <paramref name="C"/></c>; otherwise, <c><paramref name="C"/> = <paramref name="α"/> * <paramref name="opB"/>(<paramref name="B"/>) * <paramref name="opA"/>(<paramref name="A"/>) + <paramref name="β"/> * <paramref name="C"/></c>.<br/>
		/// Where <paramref name="A"/> is a symmetric/hermitian matrix stored in lower or upper mode, <paramref name="B"/> and <paramref name="C"/> are dense matrices, and <paramref name="α"/> and <paramref name="β"/> are scalars.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS1">The first actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The second actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS3">The third actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="fillUpper">The <see cref="bool"/> indicates whether matrix <paramref name="A"/> upper or lower part is stored</param>
		/// <param name="leftA">The <see cref="bool"/> indicates whether matrix <paramref name="A"/> is on the left or right of <paramref name="B"/></param>
		/// <param name="hermA">Whether <paramref name="A"/> is a hermitian or symmetric matrix</param>
		/// <param name="opA">The <see cref="MatrixOperation"/> to indicate the simple operation of <paramref name="A"/></param>
		/// <param name="opB">The <see cref="MatrixOperation"/> to indicate the simple operation of <paramref name="B"/></param>
		/// <param name="m">The number of rows of matrix <paramref name="C"/></param>
		/// <param name="n">The number of columns of matrix <paramref name="C"/></param>
		/// <param name="α">The scalar to be multiplied to <paramref name="A"/></param>
		/// <param name="A">The symmetric/Hermitian matrix of dimension <c><paramref name="lda"/>×<paramref name="m"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="m"/>)</c> if <paramref name="leftA"/> is true, and <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="n"/>)</c> otherwise</param>
		/// <param name="lda">The leading dimension of two-dimensional array used to store matrix <paramref name="A"/></param>
		/// <param name="B">The array of leading dimension <paramref name="ldb"/> with <c><paramref name="ldb"/> ≥ max(1, <paramref name="m"/>)</c></param>
		/// <param name="ldb">The leading dimension of two-dimensional array used to store matrix <paramref name="B"/></param>
		/// <param name="β">The scalar to be multiplied by <paramref name="C"/>. If it is 0, the original values of <paramref name="C"/> will be ignored.</param>
		/// <param name="C">The array of dimension <c><paramref name="ldc"/>×<paramref name="n"/></c> with <c><paramref name="ldc"/> ≥ max(1, <paramref name="m"/>)</c></param>
		/// <param name="ldc">The leading dimension of two-dimensional array used to store matrix <paramref name="B"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="B"/> or <paramref name="C"/> is null or invalid</exception>
		[AbstractApiMethod]
		public abstract bool SymmetricMatrixMultiplyGeneral<T, TS1, TS2, TS3>(bool fillUpper, bool leftA, bool hermA, MatrixOperation opA, MatrixOperation opB, long m, long n, T α, TS1 A, long lda, TS2 B, long ldb, T β, TS3 C, long ldc) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>;

		/// <summary>
		/// When implemented by a derived class, solves the triangular linear systems with multiple right-hand-sides for <c>x</c> and overwrite it to <paramref name="B"/>:<br/>
		/// <c><paramref name="op"/>(<paramref name="A"/>) * x == <paramref name="α"/> * <paramref name="B"/></c> if <paramref name="leftA"/> is true, or <c>x * <paramref name="op"/>(<paramref name="A"/>) == <paramref name="α"/> * <paramref name="B"/></c> otherwise.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS1">The first actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The second actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="leftA">Whether the matrix <paramref name="A"/> is at left side or right side</param>
		/// <param name="fillUpper">Whether the matrix <paramref name="A"/>'s upper or lower triangle is filled</param>
		/// <param name="unitDiag">Whether the matrix <paramref name="A"/>'s diagonal elements are all 1 or not</param>
		/// <param name="op">The <see cref="MatrixOperation"/> indicates the simple operation to <paramref name="A"/></param>
		/// <param name="m">The number of rows and columns of <paramref name="A"/> and number of rows of <paramref name="B"/></param>
		/// <param name="n">The number of columns of <paramref name="B"/>, i.e., the number of linear systems to be solved</param>
		/// <param name="α">The scalar to multiply to <paramref name="B"/></param>
		/// <param name="A">The input triangular matrix <paramref name="A"/> of dimension <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="m"/>)</c></param>
		/// <param name="lda">The leading dimension of two-dimensional array used to store matrix <paramref name="A"/></param>
		/// <param name="B">The input/output right-hand-side matrix. Overwritten by the solutions at exit.</param>
		/// <param name="ldb">The leading dimension of two-dimensional array used to store matrix <paramref name="B"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="B"/> is null or invalid</exception>
		[AbstractApiMethod]
		public abstract bool TriangularMatrixSolve<T, TS1, TS2>(bool leftA, bool fillUpper, bool unitDiag, MatrixOperation op, long m, long n, T α, TS1 A, long lda, TS2 B, long ldb) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

		/// <summary>
		/// When implemented by a derived class, multiply the triangular matrix <paramref name="A"/> with the given matrix <paramref name="B"/> and overwrite the result to <paramref name="C"/>:<br/>
		/// <c><paramref name="C"/> = <paramref name="α"/> * <paramref name="opA"/>(<paramref name="A"/>) * <paramref name="opB"/>(<paramref name="B"/>) + <paramref name="β"/> * <paramref name="C"/></c> if <paramref name="leftA"/> is true, or <c><paramref name="C"/> = <paramref name="α"/> * <paramref name="opB"/>(<paramref name="B"/>) * <paramref name="opA"/>(<paramref name="A"/>) + <paramref name="β"/> * <paramref name="C"/></c> otherwise.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS1">The first actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The second actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS3">The third actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="leftA">Whether the matrix <paramref name="A"/> is at left side or right side</param>
		/// <param name="fillUpper">Whether the matrix <paramref name="A"/>'s upper or lower triangle is filled</param>
		/// <param name="unitDiag">Whether the matrix <paramref name="A"/>'s diagonal elements are all 1 or not</param>
		/// <param name="opA">The <see cref="MatrixOperation"/> indicates the simple operation to <paramref name="A"/></param>
		/// <param name="opB">The <see cref="MatrixOperation"/> indicates the simple operation to <paramref name="B"/></param>
		/// <param name="m">The number of rows of matrix <c><paramref name="leftA"/> ? <paramref name="opA"/>(<paramref name="A"/>) : <paramref name="opB"/>(<paramref name="B"/>)</c> and <paramref name="C"/></param>
		/// <param name="n">The number of columns of matrix <c><paramref name="leftA"/> ? <paramref name="opB"/>(<paramref name="B"/>) : <paramref name="opA"/>(<paramref name="A"/>)</c> and <paramref name="C"/></param>
		/// <param name="k">If <paramref name="leftA"/>, the number of columns of <paramref name="opA"/>(<paramref name="A"/>) and rows of <paramref name="opB"/>(<paramref name="B"/>); otherwise, the number of rows of <paramref name="opA"/>(<paramref name="A"/>) and columns of <paramref name="opB"/>(<paramref name="B"/>)</param>
		/// <param name="α">The scalar to multiply to <paramref name="A"/> or <paramref name="B"/></param>
		/// <param name="A">The input triangular matrix</param>
		/// <param name="lda">The leading dimension of two-dimensional array used to store matrix <paramref name="A"/></param>
		/// <param name="B">The input general matrix</param>
		/// <param name="ldb">The leading dimension of two-dimensional array used to store matrix <paramref name="B"/></param>
		/// <param name="β">The scalar to multiply to <paramref name="C"/></param>
		/// <param name="C">The output matrix of size <c><paramref name="ldc"/>x<paramref name="n"/></c> to be overwritten by the result at exit, can be <paramref name="B"/> when <paramref name="ldc"/> == <paramref name="ldb"/>.</param>
		/// <param name="ldc">The leading dimension of two-dimensional array used to store matrix <paramref name="C"/>, must be <paramref name="ldb"/> when <paramref name="B"/> == <paramref name="C"/>.</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="B"/> or <paramref name="C"/> is null or invalid</exception>
		[AbstractApiMethod]
		public abstract bool TriangularMatrixMultiplyGeneral<T, TS1, TS2, TS3>(bool leftA, bool fillUpper, bool unitDiag, MatrixOperation opA, MatrixOperation opB, long m, long n, long k, T α, TS1 A, long lda, TS2 B, long ldb, T β, TS3 C, long ldc) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>;

		/// <summary>
		/// When implemented by a derived class, perform the symmetric/hermitian rank-k update:<br/>
		/// <c><paramref name="C"/> = <paramref name="α"/> * <paramref name="op"/>(<paramref name="A"/>) * <paramref name="op"/>(<paramref name="A"/>)^pow + <paramref name="β"/> * <paramref name="C"/></c>, <c>pow = <paramref name="conjA"/> ? H : T</c>.<br/>
		/// Where <paramref name="α"/> and <paramref name="β"/> are scalars, <paramref name="C"/> is a symmetric/hermitian matrix stored in lower or upper mode, and <paramref name="A"/> is a matrix with dimensions<c><paramref name="op"/>(<paramref name="A"/>) == <paramref name="n"/>×<paramref name="k"/></c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS1">The first actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The second actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="fillUpper">The <see cref="bool"/> indicates whether matrix <paramref name="C"/>'s upper or lower part will be overwritten</param>
		/// <param name="op">The <see cref="MatrixOperation"/> indicates the simple operation to <paramref name="A"/></param>
		/// <param name="conjA">Conjugate transpose <paramref name="A"/> or just transpose <paramref name="A"/></param>
		/// <param name="n">The number of rows of matrix <paramref name="op"/>(<paramref name="A"/>) and <paramref name="C"/></param>
		/// <param name="k">The number of columns of matrix <paramref name="op"/>(<paramref name="A"/>)</param>
		/// <param name="α">The scalar to be multiplied to <paramref name="op"/>(<paramref name="A"/>) * <paramref name="op"/>(<paramref name="A"/>)^pow</param>
		/// <param name="A">The array of column major with leading dimension = <paramref name="lda"/></param>
		/// <param name="lda">The leading dimension of two-dimensional array used to store matrix <paramref name="A"/>, must be of at least its number of rows</param>
		/// <param name="β">The scalar to be multiplied by <paramref name="C"/>. If it is 0, the original values of <paramref name="C"/> will be ignored.</param>
		/// <param name="C">The symmetric/hermitian matrix of dimension <c><paramref name="ldc"/>×<paramref name="n"/></c> with <c><paramref name="ldc"/> ≥ max(1, <paramref name="n"/>)</c></param>
		/// <param name="ldc">The leading dimension of two-dimensional array used to store matrix <paramref name="C"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="C"/> is null or invalid</exception>
		[AbstractApiMethod]
		public abstract bool SymmetricRankKUpdate<T, TS1, TS2>(bool fillUpper, MatrixOperation op, bool conjA, long n, long k, T α, TS1 A, long lda, T β, TS2 C, long ldc) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

		/// <summary>
		/// When implemented by a derived class, perform the symmetric/hermitian rank-2k update:<br/>
		/// <c><paramref name="C"/> = <paramref name="α"/> * (<paramref name="op"/>(<paramref name="A"/>) * <paramref name="op"/>(<paramref name="B"/>)^pow + <paramref name="op"/>(<paramref name="A"/>)^pow * <paramref name="op"/>(<paramref name="B"/>)) + <paramref name="β"/> * <paramref name="C"/></c>, <c>pow = <paramref name="conjugate"/> ? H : T</c>.<br/>
		/// Where <paramref name="α"/> and <paramref name="β"/> are scalars, <paramref name="C"/> is a symmetric/hermitian matrix stored in lower or upper mode, and <paramref name="A"/> and <paramref name="B"/> are matrices with dimensions <c><paramref name="op"/>(<paramref name="A"/>) == <paramref name="op"/>(<paramref name="B"/>) == <paramref name="n"/>×<paramref name="k"/></c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS1">The first actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The second actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS3">The third actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="fillUpper">The <see cref="bool"/> indicates whether matrix <paramref name="C"/>'s upper or lower part will be overwritten</param>
		/// <param name="op">The <see cref="MatrixOperation"/> indicates the simple operation to <paramref name="A"/> and <paramref name="B"/></param>
		/// <param name="conjugate">Conjugate transpose <paramref name="A"/> and <paramref name="B"/> or just transpose</param>
		/// <param name="n">The number of rows of matrix <paramref name="op"/>(<paramref name="A"/>), <paramref name="op"/>(<paramref name="B"/>) and <paramref name="C"/></param>
		/// <param name="k">The number of columns of matrix <paramref name="op"/>(<paramref name="A"/>) and <paramref name="op"/>(<paramref name="B"/>)</param>
		/// <param name="α">The scalar to be multiplied to <paramref name="op"/>(<paramref name="A"/>) * <paramref name="op"/>(<paramref name="A"/>)^pow</param>
		/// <param name="A">The array of column major with leading dimension = <paramref name="lda"/></param>
		/// <param name="lda">The leading dimension of two-dimensional array used to store matrix <paramref name="A"/>, must be of at least its number of rows</param>
		/// <param name="B">The array of column major with leading dimension = <paramref name="ldb"/></param>
		/// <param name="ldb">The leading dimension of two-dimensional array used to store matrix <paramref name="B"/>, must be of at least its number of rows</param>
		/// <param name="β">The scalar to be multiplied by <paramref name="C"/>. If it is 0, the original values of <paramref name="C"/> will be ignored.</param>
		/// <param name="C">The symmetric/hermitian matrix of dimension <c><paramref name="ldc"/>×<paramref name="n"/></c> with <c><paramref name="ldc"/> ≥ max(1, <paramref name="n"/>)</c></param>
		/// <param name="ldc">The leading dimension of two-dimensional array used to store matrix <paramref name="C"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="C"/> is null or invalid</exception>
		[AbstractApiMethod]
		public abstract bool SymmetricRankTwoKUpdate<T, TS1, TS2, TS3>(bool fillUpper, MatrixOperation op, bool conjugate, long n, long k, T α, TS1 A, long lda, TS2 B, long ldb, T β, TS3 C, long ldc) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>;

		/// <summary>
		/// When implemented by a derived class, perform the variant symmetric/hermitian rank-k update:<br/>
		/// <c><paramref name="C"/> = <paramref name="α"/> * <paramref name="op"/>(<paramref name="A"/>) * <paramref name="op"/>(<paramref name="B"/>)^pow + <paramref name="β"/> * <paramref name="C"/></c>, <c>pow = <paramref name="conjB"/> ? H : T</c>.<br/>
		/// Where <paramref name="α"/> and <paramref name="β"/> are scalars, <paramref name="C"/> is a symmetric/hermitian matrix stored in lower or upper mode, and <paramref name="A"/> and <paramref name="B"/> are matrices with dimensions <c><paramref name="op"/>(<paramref name="A"/>) == <paramref name="op"/>(<paramref name="B"/>) == <paramref name="n"/>×<paramref name="k"/></c>.<br/>
		/// This routine can be used when the matrix <paramref name="B"/> is in such way that the result is guaranteed to be hermitian. For example, <paramref name="B"/> is a column-wise scaling of <paramref name="A"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS1">The first actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The second actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS3">The third actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="op">The <see cref="MatrixOperation"/> indicates the simple operation to <paramref name="A"/> and <paramref name="B"/></param>
		/// <param name="conjB">Conjugate transpose <paramref name="B"/> or just transpose <paramref name="B"/></param>
		/// <param name="n">The number of rows of matrix <paramref name="op"/>(<paramref name="A"/>), <paramref name="op"/>(<paramref name="B"/>) and <paramref name="C"/></param>
		/// <param name="k">The number of columns of matrix <paramref name="op"/>(<paramref name="A"/>) and <paramref name="op"/>(<paramref name="B"/>)</param>
		/// <param name="α">The scalar to be multiplied to <paramref name="op"/>(<paramref name="A"/>) * <paramref name="op"/>(<paramref name="A"/>)^pow</param>
		/// <param name="A">The array of column major with leading dimension = <paramref name="lda"/></param>
		/// <param name="lda">The leading dimension of two-dimensional array used to store matrix <paramref name="A"/>, must be of at least its number of rows</param>
		/// <param name="B">The array of column major with leading dimension = <paramref name="ldb"/></param>
		/// <param name="ldb">The leading dimension of two-dimensional array used to store matrix <paramref name="B"/>, must be of at least its number of rows</param>
		/// <param name="β">The scalar to be multiplied by <paramref name="C"/>. If it is 0, the original values of <paramref name="C"/> will be ignored.</param>
		/// <param name="C">The general matrix of dimension <c><paramref name="ldc"/>×<paramref name="n"/></c> with <c><paramref name="ldc"/> ≥ max(1, <paramref name="n"/>)</c></param>
		/// <param name="ldc">The leading dimension of two-dimensional array used to store matrix <paramref name="C"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="C"/> is null or invalid</exception>
		[AbstractApiMethod]
		public abstract bool GeneralRankKUpdate<T, TS1, TS2, TS3>(MatrixOperation op, bool conjB, long n, long k, T α, TS1 A, long lda, TS2 B, long ldb, T β, TS3 C, long ldc) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>;
		#endregion
	}
}