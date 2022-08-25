using System.Linq.Expressions;

using Althea.SourceGenerator;
using Althea.Storage;


namespace Althea.LinearAlgebra.Dense
{
	/// <summary>
	/// The abstract interface for dense linear algebra extend BLAS API routines 
	/// </summary>
	[AbstractRuntimeApi]
	public interface IExtendBlasAbstractApi : IAbstractRuntimeApi<IExtendBlasAbstractApi>
	{
		#region BLAS like
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
		public abstract bool GeneralMatricesAdd<T, TS1, TS2, TS3>(MatrixOperation opA, MatrixOperation opB, long m, long n, T α, TS1? A, long lda, T β, TS2? B, long ldb, TS3 C, long ldc) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>;

		/// <summary>
		/// When implemented by a derived class, perform the matrix-matrix multiplication:
		/// <list type="table">
		/// <listheader><term>Condition</term>  <description>Equation</description></listheader>
		/// <item><term><paramref name="leftA"/></term><description>  <paramref name="C"/> = <paramref name="α"/> * <paramref name="opA"/>(<paramref name="A"/>) * diag(<paramref name="x"/>) + <paramref name="β"/> * <paramref name="C"/></description></item>
		/// <item><term>!<paramref name="leftA"/></term><description>  <paramref name="C"/> = <paramref name="α"/> * diag(<paramref name="x"/>) * <paramref name="opA"/>(<paramref name="A"/>) + <paramref name="β"/> * <paramref name="C"/></description></item>
		/// </list>
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS1">The first actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The second actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS3">The third actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="leftA">Whether to put <paramref name="A"/> in the left side or the right side</param>
		/// <param name="opA">The <see cref="MatrixOperation"/> to indicate the simple operation of <paramref name="A"/></param>
		/// <param name="conjX">Whether to conjugate <paramref name="x"/> during calculation</param>
		/// <param name="m">The number of rows of matrix <paramref name="C"/></param>
		/// <param name="n">The number of columns of matrix <paramref name="C"/></param>
		/// <param name="α">The scalar to multiply to <paramref name="A"/></param>
		/// <param name="A">The array of dimensions <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="m"/>)</c></param>
		/// <param name="lda">The leading dimension of two-dimensional array used to store the matrix <paramref name="A"/></param>
		/// <param name="x">The one-dimensional array of length at least <c>(1+(<paramref name="n"/>-1)*<paramref name="strideX"/>)</c> elements if <paramref name="leftA"/> is true or <c>(1+(<paramref name="m"/>-1)*<paramref name="strideX"/>)</c> otherwise</param>
		/// <param name="strideX">The stride of one-dimensional array <paramref name="x"/></param>
		/// <param name="β">The scalar to multiply to <paramref name="C"/></param>
		/// <param name="C">The array of dimensions <c><paramref name="ldc"/>×<paramref name="n"/></c> with <c><paramref name="ldc"/> ≥ max(1, <paramref name="m"/>)</c></param>
		/// <param name="ldc">The leading dimension of two-dimensional array used to store the matrix <paramref name="C"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> ≤ 0</exception>
		[AbstractApiMethod]
		public abstract bool DiagonalMatrixMultiplyGeneral<T, TS1, TS2, TS3>(bool leftA, MatrixOperation opA, bool conjX, long m, long n, T α, TS1 A, long lda, TS2 x, long strideX, T β, TS3 C, long ldc) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>;
		#endregion

		#region vector math
		/// <summary>
		/// When implemented by a derived class, compute <c><paramref name="y"/>[i] = <paramref name="op"/>(<paramref name="x"/>[i])</c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TS1">The input actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The output actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="x">The input vector to apply <paramref name="op"/></param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="y">The output vector to store the results</param>
		/// <param name="strideY">The stride between consecutive elements of <paramref name="y"/></param>
		/// <param name="op">The <see cref="UnaryOperation"/> to apply to each element of <paramref name="x"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> or <paramref name="strideY"/> ≤ 0</exception>
		[AbstractApiMethod]
		public abstract bool GeneralVectorUnary<T, TS1, TS2>(UnaryOperation op, TS1 x, long strideX, TS2 y, long strideY) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;
		
		/// <summary>
		/// When implemented by a derived class, compute <c>result = <paramref name="op"/>(<paramref name="x"/>[i], result)</c> for all <c>i</c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TS">The actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="x">The vector to be reduced</param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="op">The <see cref="ReduceOperation"/> to apply to elements of <paramref name="x"/></param>
		/// <param name="result">Output the reduction result of <paramref name="x"/> under <paramref name="op"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> ≤ 0</exception>
		[AbstractApiMethod]
		public abstract bool GeneralVectorReduce<T, TS>(ReduceOperation op, TS x, long strideX, out T result) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>;

		/// <summary>
		/// When implemented by a derived class, compute the index of the reduction result: <c>result = <paramref name="op"/>(<paramref name="x"/>[i], result)</c> for all <c>i</c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TS">The actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="x">The vector to be reduced</param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="op">The <see cref="ReduceOperation"/> to apply to elements of <paramref name="x"/>, must be ones like <see cref="ReduceOperation.Maximum"/></param>
		/// <param name="index">Output the reduction result's index of <paramref name="x"/> under <paramref name="op"/> compared to <paramref name="x"/> in <typeparamref name="T"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> ≤ 0</exception>
		[AbstractApiMethod]
		public abstract bool GeneralVectorArgReduce<T, TS>(ReduceOperation op, TS x, long strideX, out long index) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>;

		/// <summary>
		/// When implemented by a derived class, compute <c><paramref name="y"/>[i] = <paramref name="op"/>(<paramref name="x"/>[i], <paramref name="scalar"/>)</c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TS1">The input actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The output actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="x">The input vector to apply <paramref name="op"/></param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="y">The output vector to store the results</param>
		/// <param name="strideY">The stride between consecutive elements of <paramref name="y"/></param>
		/// <param name="scalar">The scalar as the second input of <paramref name="op"/></param>
		/// <param name="op">The <see cref="BinaryScalarOperation"/> to apply to each element of <paramref name="x"/> and <paramref name="scalar"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> or <paramref name="strideY"/> ≤ 0</exception>
		[AbstractApiMethod]
		public abstract bool GeneralVectorBinaryScalar<T, TS1, TS2>(BinaryScalarOperation op, T scalar, TS1 x, long strideX, TS2 y, long strideY) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

		/// <summary>
		/// When implemented by a derived class, compute <c><paramref name="z"/>[i] = <paramref name="op"/>(<paramref name="x"/>[i], <paramref name="y"/>[i])</c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TS1">The first input actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The second input actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS3">The output actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="x">The input vector as the first parameters of <paramref name="op"/></param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="y">The input vector as the second parameters of <paramref name="op"/></param>
		/// <param name="strideY">The stride between consecutive elements of <paramref name="y"/></param>
		/// <param name="z">The output vector to store the results of <paramref name="op"/></param>
		/// <param name="strideZ">The stride between consecutive elements of <paramref name="z"/></param>
		/// <param name="op">The <see cref="BinaryOperation"/> to apply to each element of <paramref name="x"/> and <paramref name="y"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/>, <paramref name="y"/> or <paramref name="z"/> is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/>, <paramref name="strideY"/> or <paramref name="strideZ"/> ≤ 0</exception>
		[AbstractApiMethod]
		public abstract bool GeneralVectorsBinary<T, TS1, TS2, TS3>(BinaryOperation op, TS1 x, long strideX, TS2 y, long strideY, TS3 z, long strideZ) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>;

		/// <summary>
		/// When implemented by a derived class, perform partial aggregate (scan) <paramref name="op"/> of the elements in vector <paramref name="x"/> and write the result to <paramref name="y"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS1">The input actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The output actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="x">The vector to be scanned</param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="y">The vector to store the scan result</param>
		/// <param name="strideY">The stride between consecutive elements of <paramref name="y"/></param>
		/// <param name="inclusive">Whether to scan <paramref name="x"/> inclusively (the first element is the first element of <paramref name="x"/>) or exclusively (the first element is the identity element of <paramref name="op"/>)</param>
		/// <param name="op">The <see cref="ReduceOperation"/> to apply to the partial scan result and each element of <paramref name="x"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> or <paramref name="strideY"/> ≤ 0</exception>
		[AbstractApiMethod]
		public abstract bool GeneralVectorsScan<T, TS1, TS2>(ReduceOperation op, TS1 x, long strideX, TS2 y, long strideY, bool inclusive) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

		/// <summary>
		/// When implemented by a derived class, check if all elements in <paramref name="x"/> and <paramref name="y"/> are equal: <c><paramref name="x"/>[i] == <paramref name="y"/>[i]</c> for all <c>i</c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS1">The first actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The second actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="x">The vector to be checked</param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="y">The other vector to be checked</param>
		/// <param name="strideY">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="equals">Output <see cref="bool"/> indicating whether all elements in <paramref name="x"/> and <paramref name="y"/> are equal</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> or <paramref name="strideY"/> ≤ 0</exception>
		[AbstractApiMethod]
		public abstract bool GeneralVectorsEqual<T, TS1, TS2>(TS1 x, long strideX, TS2 y, long strideY, out bool equals) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

		/// <summary>
		/// When implemented by a derived class, cast the given vector from type <typeparamref name="TIn"/> to type <typeparamref name="TOut"/>.
		/// </summary>
		/// <typeparam name="TIn">Any unmanaged number as the input data type</typeparam>
		/// <typeparam name="TOut">Any unmanaged number as the output data type</typeparam>
		/// <typeparam name="TSIn">The input actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TSOut">The output actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="source">The source vector</param>
		/// <param name="strideSource">The stride between consecutive elements of <paramref name="source"/></param>
		/// <param name="destination">The destination vector</param>
		/// <param name="strideDestination">The stride between consecutive elements of <paramref name="destination"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="destination"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideSource"/> or <paramref name="strideDestination"/> ≤ 0</exception>
		[AbstractApiMethod]
		public abstract bool GeneralVectorsCast<TIn, TOut, TSIn, TSOut>(TSIn source, long strideSource, TSOut destination, long strideDestination) where TIn : unmanaged, IBaseNumber<TIn> where TOut : unmanaged, IBaseNumber<TOut> where TSIn : class, IStorage<TIn, TSIn> where TSOut : class, IStorage<TOut, TSOut>;
		#endregion

		#region matrix math
		/// <summary>
		/// When implemented by a derived class, compute <c><paramref name="B"/>[i, j] = <paramref name="op"/>(<paramref name="A"/>[i, j])</c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TS1">The input actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The output actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
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
		public abstract bool GeneralMatrixUnary<T, TS1, TS2>(UnaryOperation op, long rows, long cols, TS1 A, long lda, TS2 B, long ldb) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

		/// <summary>
		/// When implemented by a derived class, compute <c>result = <paramref name="op"/>(<paramref name="A"/>[i, j], result)</c> for all <c>i, j</c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TS">The actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
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
		public abstract bool GeneralMatrixReduce<T, TS>(ReduceOperation op, long rows, long cols, TS A, long lda, out T result) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>;

		/// <summary>
		/// When implemented by a derived class, compute the index of the reduction result: <c>result = <paramref name="op"/>(<paramref name="A"/>[i, j], result)</c> for all <c>i, j</c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TS">The actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
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
		public abstract bool GeneralMatrixArgReduce<T, TS>(ReduceOperation op, long rows, long cols, TS A, long lda, out long index) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>;

		/// <summary>
		/// When implemented by a derived class, compute reduction <c><paramref name="x"/>[i] = <paramref name="op"/>(<paramref name="A"/>[j, i], <paramref name="x"/>[i])</c> for all <c>i, j</c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TS1">The actual matrix storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The actual vector storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
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
		public abstract bool GeneralMatrixColumnReduce<T, TS1, TS2>(ReduceOperation op, long rows, long cols, TS1 A, long lda, TS2 x, long strideX) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

		/// <summary>
		/// When implemented by a derived class, compute <c><paramref name="B"/>[i, j] = <paramref name="op"/>(<paramref name="A"/>[i, j], <paramref name="scalar"/>)</c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TS1">The input actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The output actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
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
		public abstract bool GeneralMatrixBinaryScalar<T, TS1, TS2>(BinaryScalarOperation op, long rows, long cols, T scalar, TS1 A, long lda, TS2 B, long ldb) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

		/// <summary>
		/// When implemented by a derived class, compute <c><paramref name="C"/>[i, j] = <paramref name="op"/>(<paramref name="A"/>[i, j], <paramref name="B"/>[i, j])</c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TS1">The first input actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The second input actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS3">The output actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
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
		public abstract bool GeneralMatricesBinary<T, TS1, TS2, TS3>(BinaryOperation op, long rows, long cols, TS1 A, long lda, TS2 B, long ldb, TS3 C, long ldc) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>;

		/// <summary>
		/// When implemented by a derived class, perform partial aggregate (scan) <paramref name="op"/> of the elements in columns of <paramref name="A"/> and write the result to columns <paramref name="B"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS1">The input actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The output actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="rows">The number of rows in <typeparamref name="T"/></param>
		/// <param name="cols">The number of columns in <typeparamref name="T"/></param>
		/// <param name="A">The matrix whose columns will be scanned</param>
		/// <param name="lda">The leading dimension of <paramref name="A"/> in <typeparamref name="T"/></param>
		/// <param name="B">The matrix whose columns will be overwritten by the scan results of <paramref name="A"/>'s column s</param>
		/// <param name="ldb">The leading dimension of <paramref name="B"/> in <typeparamref name="T"/></param>
		/// <param name="inclusive">Whether to scan <paramref name="A"/> inclusively (the first elements are the first elements of columns <paramref name="A"/>) or exclusively (the first elements are the identity element of <paramref name="op"/>)</param>
		/// <param name="op">The <see cref="ReduceOperation"/> to apply to the partial scan result and each element of <paramref name="A"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="B"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="lda"/> or <paramref name="ldb"/> &lt; <paramref name="rows"/></exception>
		[AbstractApiMethod]
		public abstract bool GeneralMatrixColumnScan<T, TS1, TS2>(ReduceOperation op, bool inclusive, long rows, long cols, TS1 A, long lda, TS2 B, long ldb) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

		/// <summary>
		/// When implemented by a derived class, check if all elements in <paramref name="A"/> and <paramref name="B"/> are equal: <c><paramref name="A"/>[i, j] == <paramref name="B"/>[i, j]</c> for all <c>i, j</c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS1">The first actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The second actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
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
		public abstract bool GeneralMatricesEqual<T, TS1, TS2>(long rows, long cols, TS1 A, long lda, TS2 B, long ldb, out bool equals) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

		/// <summary>
		/// When implemented by a derived class, cast the given matrix from type <typeparamref name="TIn"/> to type <typeparamref name="TOut"/>.
		/// </summary>
		/// <typeparam name="TIn">Any unmanaged number as the input data type</typeparam>
		/// <typeparam name="TOut">Any unmanaged number as the output data type</typeparam>
		/// <typeparam name="TSIn">The input actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TSOut">The output actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
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
		public abstract bool GeneralMatrixCast<TIn, TOut, TSIn, TSOut>(long rows, long cols, TSIn source, long lds, TSOut destination, long ldd) where TIn : unmanaged, IBaseNumber<TIn> where TOut : unmanaged, IBaseNumber<TOut> where TSIn : class, IStorage<TIn, TSIn> where TSOut : class, IStorage<TOut, TSOut>;
		#endregion

		#region matrix extended
		/// <summary>
		/// When implemented by a derived class, calculate matrix Kronecker product: <c><paramref name="C"/> = <paramref name="α"/> * <paramref name="A"/> ⊗ <paramref name="B"/> + <paramref name="β"/> * <paramref name="C"/></c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS1">The first actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The second actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS3">The third actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="ma">The number of rows of <paramref name="A"/></param>
		/// <param name="na">The number of columns of <paramref name="A"/></param>
		/// <param name="mb">The number of rows of <paramref name="B"/></param>
		/// <param name="nb">The number of columns of <paramref name="B"/></param>
		/// <param name="α">The scalar to be multiplied to <paramref name="A"/></param>
		/// <param name="A">dense matrix with size <c><paramref name="lda"/>×<paramref name="na"/></c></param>
		/// <param name="lda">leading dimension of <paramref name="A"/>, must be at least <paramref name="ma"/></param>
		/// <param name="B">dense matrix with size <c><paramref name="ldb"/>×<paramref name="nb"/></c></param>
		/// <param name="ldb">leading dimension of <paramref name="B"/>, must be at least <paramref name="mb"/></param>
		/// <param name="β">The scalar to be multiplied to <paramref name="C"/></param>
		/// <param name="C">The pre-allocated destination matrix with size <c><paramref name="ldc"/> × <paramref name="na"/>*<paramref name="nb"/></c></param>
		/// <param name="ldc">The leading dimension of <paramref name="C"/>, must be at least <paramref name="na"/>*<paramref name="nb"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="B"/> or <paramref name="C"/> is null or invalid</exception>
		[AbstractApiMethod]
		public abstract bool MatrixKronecker<T, TS1, TS2, TS3>(long ma, long na, long mb, long nb, T α, TS1 A, long lda, TS2 B, long ldb, T β, TS3 C, long ldc) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>;
		#endregion
	}

	/// <summary>
	/// The abstract interface for dense linear algebra extend BLAS API routines which involves runtime parsing <see cref="Expression"/>s.
	/// </summary>
	/// <remarks>Since <see cref="Expression"/> is a class (therefore introduces GC) and must be parsed before calculation, it is not recommended to use this API for non-critical situations.</remarks>
	public partial interface IDynamicExtendBlasAbstractApi : IAbstractRuntimeApi<IDynamicExtendBlasAbstractApi>
	{
		#region expression extended for general types
		/// <summary>
		/// When implemented by a derived class, compute <c><paramref name="outputs"/>[i] = <paramref name="op"/>(<paramref name="inputs"/>[i], <paramref name="scalars"/>)</c>.
		/// </summary>
		/// <typeparam name="TIn">Any unmanaged number struct as the input data type</typeparam>
		/// <typeparam name="TOut">Any unmanaged number struct as the output data type</typeparam>
		/// <typeparam name="TScalar">Any unmanaged number struct as the scalar data type</typeparam>
		/// <typeparam name="TSIn">The input actual storage type that implements <see cref="IStorage{TIn, TSelf}"/></typeparam>
		/// <typeparam name="TSOut">The output actual storage type that implements <see cref="IStorage{TIn, TSelf}"/></typeparam>
		/// <param name="op">The <see cref="Expression{TDelegate}"/> of <see cref="Func{TIn, TScalar, TOut}"/> to apply to each elements of <paramref name="inputs"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <param name="inputs">The input vectors and strides to apply <paramref name="op"/></param>
		/// <param name="scalars">The scalar inputs</param>
		/// <param name="outputs">The output vectors and strides to store the results</param>
		/// <exception cref="ArgumentNullException">If any of the vectors is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If any of the strides ≤ 0</exception>
		/// <exception cref="ArgumentException">If <paramref name="op"/> is not a point-wise operation whose input matches (<paramref name="inputs"/>, <paramref name="scalars"/>) and output matches <paramref name="outputs"/></exception>
		[AbstractApiMethod]
		public abstract bool GeneralVectorsPointwise<TIn, TOut, TScalar, TSIn, TSOut>(Expression<Func<TIn[], TScalar[], TOut[]>> op, ReadOnlySpan<(TSIn Vector, long Stride)> inputs, ReadOnlySpan<TScalar> scalars, ReadOnlySpan<(TSOut Vector, long Stride)> outputs) where TIn : unmanaged, IBaseNumber<TIn> where TOut : unmanaged, IBaseNumber<TOut> where TSIn : class, IStorage<TIn, TSIn> where TScalar : unmanaged, IBaseNumber<TScalar> where TSOut : class, IStorage<TOut, TSOut>;

		/// <summary>
		/// When implemented by a derived class, perform partial aggregate (scan) <paramref name="op"/> of the elements in <paramref name="inputs"/> and write the result to <paramref name="outputs"/>.
		/// </summary>
		/// <typeparam name="TIn">Any unmanaged number struct as the input data type</typeparam>
		/// <typeparam name="TOut">Any unmanaged number struct as the output data type</typeparam>
		/// <typeparam name="TSIn">The input actual storage type that implements <see cref="IStorage{TIn, TSelf}"/></typeparam>
		/// <typeparam name="TSOut">The output actual storage type that implements <see cref="IStorage{TIn, TSelf}"/></typeparam>
		/// <param name="op">The <see cref="Expression{TDelegate}"/> of <see cref="Func{TIn, TOut, TOut}"/> to apply to each elements of <paramref name="inputs"/> and the partial scan results</param>
		/// <param name="inputs">The input vectors and strides to apply <paramref name="op"/></param>
		/// <param name="outputs">The output vectors and strides to store the results</param>
		/// <param name="inclusive">Whether to scan <paramref name="inputs"/> inclusively (the first elements are the first elements of <paramref name="inputs"/>) or exclusively (the first element is the identity element of <paramref name="op"/>)</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If any of the vectors is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If any of the strides ≤ 0</exception>
		/// <exception cref="ArgumentException">If <paramref name="op"/> is not an aggregation operation whose input matches (<paramref name="inputs"/>, <paramref name="outputs"/>) and output matches <paramref name="outputs"/></exception>
		[AbstractApiMethod]
		public abstract bool GeneralVectorsScan<TIn, TOut, TSIn, TSOut>(Expression<Func<TIn[], TOut[], TOut[]>> op, bool inclusive, ReadOnlySpan<(TSIn Vector, long Stride)> inputs, ReadOnlySpan<(TSOut Vector, long Stride)> outputs) where TIn : unmanaged, IBaseNumber<TIn> where TOut : unmanaged, IBaseNumber<TOut> where TSIn : class, IStorage<TIn, TSIn> where TSOut : class, IStorage<TOut, TSOut>;

		/// <summary>
		/// When implemented by a derived class, compute <c><paramref name="results"/> = <paramref name="op"/>(<paramref name="inputs"/>[i], result)</c> for all <c>i</c>.
		/// </summary>
		/// <typeparam name="TIn">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TOut">Any unmanaged number struct as the output data type</typeparam>
		/// <typeparam name="TSIn">The input actual storage type that implements <see cref="IStorage{TIn, TSelf}"/></typeparam>
		/// <param name="op">The <see cref="Expression{TDelegate}"/> of <see cref="Func{TIn, TOut, TOut}"/> to apply to each elements of <paramref name="inputs"/> and partial reduction results</param>
		/// <param name="inputs">The input vectors and strides to apply <paramref name="op"/></param>
		/// <param name="results">Output the reduction results of <paramref name="inputs"/> under <paramref name="op"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If any of the vectors is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If any of the strides ≤ 0</exception>
		/// <exception cref="ArgumentException">If <paramref name="op"/> is not a reduction operation whose input matches (<paramref name="inputs"/>, <paramref name="results"/>) and output matches <paramref name="results"/></exception>
		[AbstractApiMethod]
		public abstract bool GeneralVectorsReduce<TIn, TOut, TSIn>(Expression op, ReadOnlySpan<(TSIn Vector, long Stride)> inputs, Span<TOut> results) where TIn : unmanaged, IBaseNumber<TIn> where TOut : unmanaged, IBaseNumber<TOut> where TSIn : class, IStorage<TIn, TSIn>;

		/// <summary>
		/// When implemented by a derived class, compute <c><paramref name="outputs"/>[i] = <paramref name="op"/>(<paramref name="inputs"/>[i], <paramref name="scalars"/>)</c>.
		/// </summary>
		/// <typeparam name="TIn">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TOut">Any unmanaged number struct as the output data type</typeparam>
		/// <typeparam name="TScalar">Any unmanaged number struct as the scalar data type</typeparam>
		/// <typeparam name="TSIn">The input actual storage type that implements <see cref="IStorage{TIn, TSelf}"/></typeparam>
		/// <typeparam name="TSOut">The output actual storage type that implements <see cref="IStorage{TIn, TSelf}"/></typeparam>
		/// <param name="op">The <see cref="Expression{TDelegate}"/> of <see cref="Func{TIn, TScalar, TOut}"/> to apply to each elements of <paramref name="inputs"/></param>
		/// <param name="rows">The number of rows of all matrices</param>
		/// <param name="cols">The number of columns of all matrices</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <param name="inputs">The input matrices and leading dimensions to apply <paramref name="op"/></param>
		/// <param name="scalars">The scalar inputs</param>
		/// <param name="outputs">The output matrices and leading dimensions to store the results</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If any of the matrices is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If any of the leading dimensions or <paramref name="rows"/> or <paramref name="cols"/> ≤ 0</exception>
		/// <exception cref="ArgumentException">If <paramref name="op"/> is not a point-wise operation whose input matches (<paramref name="inputs"/>, <paramref name="scalars"/>) and output matches <paramref name="outputs"/></exception>
		[AbstractApiMethod]
		public abstract bool GeneralMatricesPointwise<TIn, TOut, TScalar, TSIn, TSOut>(Expression<Func<TIn[], TScalar[], TOut[]>> op, long rows, long cols, ReadOnlySpan<(TSIn Matrix, long LeadDim)> inputs, ReadOnlySpan<TIn> scalars, ReadOnlySpan<(TSOut Matrix, long LeadDim)> outputs) where TIn : unmanaged, IBaseNumber<TIn> where TOut : unmanaged, IBaseNumber<TOut> where TScalar : unmanaged, IBaseNumber<TScalar> where TSIn : class, IStorage<TIn, TSIn> where TSOut : class, IStorage<TIn, TSOut>;

		/// <summary>
		/// When implemented by a derived class, perform inclusive partial aggregate (scan) <paramref name="op"/> of the elements in <paramref name="inputs"/> and write the result to <paramref name="outputs"/>.
		/// </summary>
		/// <typeparam name="TIn">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TOut">Any unmanaged number struct as the output data type</typeparam>
		/// <typeparam name="TSIn">The input actual storage type that implements <see cref="IStorage{TIn, TSelf}"/></typeparam>
		/// <typeparam name="TSOut">The output actual storage type that implements <see cref="IStorage{TIn, TSelf}"/></typeparam>
		/// <param name="op">The <see cref="Expression{TDelegate}"/> of <see cref="Func{TIn, TOut, TOut}"/> to apply to each elements of <paramref name="inputs"/> and the partial scan results</param>
		/// <param name="rows">The number of rows of all matrices</param>
		/// <param name="cols">The number of columns of all matrices</param>
		/// <param name="inputs">The input matrices to apply <paramref name="op"/></param>
		/// <param name="outputs">The output matrices to store the results</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If any of the matrices is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If any of the leading dimensions or <paramref name="rows"/> or <paramref name="cols"/> ≤ 0</exception>
		/// <exception cref="ArgumentException">If <paramref name="op"/> is not an aggregation operation whose input matches (<paramref name="inputs"/>, <paramref name="outputs"/>) and output matches <paramref name="outputs"/></exception>
		[AbstractApiMethod]
		public abstract bool GeneralMatricesScan<TIn, TOut, TSIn, TSOut>(Expression<Func<TIn[], TOut[], TOut[]>> op, long rows, long cols, ReadOnlySpan<(TSIn Matrix, long LeadDim)> inputs, ReadOnlySpan<(TSOut Matrix, long LeadDim)> outputs) where TIn : unmanaged, IBaseNumber<TIn> where TOut : unmanaged, IBaseNumber<TOut> where TSIn : class, IStorage<TIn, TSIn> where TSOut : class, IStorage<TIn, TSOut>;

		/// <summary>
		/// When implemented by a derived class, compute <c><paramref name="results"/> = <paramref name="op"/>(<paramref name="inputs"/>[i], <paramref name="results"/>)</c> for all <c>i</c>.
		/// </summary>
		/// <typeparam name="TIn">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TOut">Any unmanaged number struct as the output data type</typeparam>
		/// <typeparam name="TSIn">The input actual storage type that implements <see cref="IStorage{TIn, TSelf}"/></typeparam>
		/// <param name="op">The <see cref="Expression{TDelegate}"/> of <see cref="Func{TIn, TOut, TOut}"/> to apply to each elements of <paramref name="inputs"/> and the partial scan results</param>
		/// <param name="rows">The number of rows of all matrices</param>
		/// <param name="cols">The number of columns of all matrices</param>
		/// <param name="inputs">The input matrices to apply <paramref name="op"/></param>
		/// <param name="results">Output the reduction results of <paramref name="inputs"/> under <paramref name="op"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If any of the matrices is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If any of the leading dimensions or <paramref name="rows"/> or <paramref name="cols"/> ≤ 0</exception>
		/// <exception cref="ArgumentException">If <paramref name="op"/> is not a reduction operation whose input matches (<paramref name="inputs"/>, <paramref name="results"/>) and output matches <paramref name="results"/></exception>
		[AbstractApiMethod]
		public abstract bool GeneralMatricesReduce<TIn, TOut, TSIn>(Expression<Func<TIn[], TOut[], TOut[]>> op, long rows, long cols, ReadOnlySpan<(TSIn Matrix, long LeadDim)> inputs, Span<TOut> results) where TIn : unmanaged, IBaseNumber<TIn> where TOut : unmanaged, IBaseNumber<TOut> where TSIn : class, IStorage<TIn, TSIn>;

		/// <summary>
		/// When implemented by a derived class, compute reduction <c><paramref name="outputs"/>[i] = <paramref name="op"/>(<paramref name="inputs"/>[j, i], <paramref name="outputs"/>[i])</c> for all <c>i, j</c>.
		/// </summary>
		/// <typeparam name="TIn">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TOut">Any unmanaged number struct as the output data type</typeparam>
		/// <typeparam name="TSIn">The actual input storage type that implements <see cref="IStorage{TIn, TSelf}"/></typeparam>
		/// <typeparam name="TSOut">The actual output storage type that implements <see cref="IStorage{TIn, TSelf}"/></typeparam>
		/// <param name="op">The <see cref="Expression{TDelegate}"/> of <see cref="Func{TIn, TOut, TOut}"/> to apply to each elements of <paramref name="inputs"/> and the partial reduction results</param>
		/// <param name="rows">The number of rows of all matrices</param>
		/// <param name="cols">The number of columns of all matrices</param>
		/// <param name="inputs">The input matrices to apply <paramref name="op"/></param>
		/// <param name="outputs">The output reduction result vectors of <paramref name="op"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If any of the matrices is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If any of the leading dimensions or <paramref name="rows"/> or <paramref name="cols"/> ≤ 0</exception>
		/// <exception cref="ArgumentException">If <paramref name="op"/> is not a reduction operation whose input matches (<paramref name="inputs"/>, <paramref name="outputs"/>) and output matches <paramref name="outputs"/></exception>
		[AbstractApiMethod]
		public abstract bool GeneralMatricesColumnReduce<TIn, TOut, TSIn, TSOut>(Expression<Func<TIn[], TOut[], TOut[]>> op, long rows, long cols, ReadOnlySpan<(TSIn Matrix, long LeadDim)> inputs, ReadOnlySpan<(TSOut Vector, long Stride)> outputs) where TIn : unmanaged, IBaseNumber<TIn> where TSIn : class, IStorage<TIn, TSIn> where TOut : unmanaged, IBaseNumber<TOut> where TSOut : class, IStorage<TOut, TSOut>;

		/// <summary>
		/// When implemented by a derived class, perform partial aggregate (scan) <paramref name="op"/> of the elements in columns of <paramref name="inputs"/> and write the result to columns <paramref name="outputs"/>.
		/// </summary>
		/// <typeparam name="TIn">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TOut">Any unmanaged number struct as the output data type</typeparam>
		/// <typeparam name="TSIn">The input actual storage type that implements <see cref="IStorage{TIn, TSelf}"/></typeparam>
		/// <typeparam name="TSOut">The output actual storage type that implements <see cref="IStorage{TIn, TSelf}"/></typeparam>
		/// <param name="op">The <see cref="Expression{TDelegate}"/> of <see cref="Func{TIn, TOut, TOut}"/> to apply to each elements of <paramref name="inputs"/> and the partial scan results</param>
		/// <param name="rows">The number of rows of all matrices</param>
		/// <param name="cols">The number of columns of all matrices</param>
		/// <param name="inputs">The input matrices whose columns will be applied by <paramref name="op"/></param>
		/// <param name="outputs">The output matrices to store the results</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If any of the matrices is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If any of the leading dimensions or <paramref name="rows"/> or <paramref name="cols"/> ≤ 0</exception>
		/// <exception cref="ArgumentException">If <paramref name="op"/> is not a reduction operation whose input matches (<paramref name="inputs"/>, <paramref name="outputs"/>) and output matches <paramref name="outputs"/></exception>
		[AbstractApiMethod]
		public abstract bool GeneralMatricesColumnScan<TIn, TOut, TSIn, TSOut>(Expression<Func<TIn[], TOut[], TOut[]>> op, long rows, long cols, ReadOnlySpan<(TSIn Matrix, long LeadDim)> inputs, ReadOnlySpan<(TSOut Vector, long Stride)> outputs) where TIn : unmanaged, IBaseNumber<TIn> where TSIn : class, IStorage<TIn, TSIn> where TOut : unmanaged, IBaseNumber<TOut> where TSOut : class, IStorage<TOut, TSOut>;
		#endregion
	}
}
