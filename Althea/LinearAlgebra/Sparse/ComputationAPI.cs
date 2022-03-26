using Althea.Arrays;
using Althea.Storage;

using Althea.SourceGenerator;


namespace Althea.LinearAlgebra.Sparse
{
	/// <summary>
	/// The abstract interface for runtime sparse linear algebra computation API routines 
	/// </summary>
	[AbstractRuntimeApi]
	public interface IComputationAbstractApi : IAbstractRuntimeApi<IComputationAbstractApi>
	{
		#region vector
		/// <summary>
		/// When implemented by a derived class, add the sparse vector <paramref name="x"/> scaled by scalar <paramref name="α"/> to a dense vector <paramref name="y"/>: <c><paramref name="y"/> += <paramref name="α"/> * <paramref name="x"/></c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS1">The storage type used by the value array(s) of sparse vector</typeparam>
		/// <typeparam name="TS2">The storage type used by the dense vector</typeparam>
		/// <typeparam name="TInd">Any unmanaged integer number as the index data type</typeparam>
		/// <typeparam name="TSInd">The storage type used by the index array(s)</typeparam>
		/// <param name="α">The scalar to multiply <paramref name="x"/></param>
		/// <param name="x">The input sparse vector as a <see cref="ISparseArray{T, TInd, TS, TSInd}"/></param>
		/// <param name="y">The input/output dense vector as a <typeparamref name="TS2"/></param>
		/// <param name="strideY">The stride between consecutive elements in <paramref name="y"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		[AbstractApiMethod]
		public abstract bool VectorSparseAddToDense<T, TInd, TS1, TS2, TSInd>(T α, ISparseArray<T, TInd, TS1, TSInd> x, TS2 y, long strideY) where T : unmanaged, INumber<T> where TInd : unmanaged, IBinaryInteger<TInd> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TSInd : class, IStorage<TInd, TSInd>;

		/// <summary>
		/// When implemented by a derived class, calculate the dot (inner) product of a sparse vector <paramref name="x"/> and a dense vector <paramref name="y"/>: result = <c><paramref name="x"/>^op <paramref name="y"/></c>, op = <paramref name="conjX"/> ? H : T.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS1">The storage type used by the value array(s) of sparse vector</typeparam>
		/// <typeparam name="TS2">The storage type used by the dense vector</typeparam>
		/// <typeparam name="TInd">Any unmanaged integer number as the index data type</typeparam>
		/// <typeparam name="TSInd">The storage type used by the index array(s)</typeparam>
		/// <param name="conjX">Whether to conjugate <paramref name="x"/> or not</param>
		/// <param name="x">The input sparse vector as a <see cref="ISparseArray{T, TInd, TS, TSInd}"/></param>
		/// <param name="y">The input/output dense vector as a <typeparamref name="TS2"/></param>
		/// <param name="strideY">The stride between consecutive elements in <paramref name="y"/></param>
		/// <param name="dot">Output the dot product result as a <typeparamref name="T"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		[AbstractApiMethod]
		public abstract bool VectorSparseDotDense<T, TInd, TS1, TS2, TSInd>(bool conjX, ISparseArray<T, TInd, TS1, TSInd> x, TS2 y, long strideY, out T dot) where T : unmanaged, INumber<T> where TInd : unmanaged, IBinaryInteger<TInd> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TSInd : class, IStorage<TInd, TSInd>;

		/// <summary>
		/// When implemented by a derived class, calculate the dot (inner) product of two sparse vectors <paramref name="x"/> and <paramref name="y"/>: result = <c><paramref name="x"/>^op <paramref name="y"/></c>, op = <paramref name="conjX"/> ? H : T.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS1">The first storage type used by the value array(s) of sparse vector</typeparam>
		/// <typeparam name="TS2">The second storage type used by the value array(s) of sparse vector</typeparam>
		/// <typeparam name="TInd1">Any unmanaged integer number as the first index data type</typeparam>
		/// <typeparam name="TInd2">Any unmanaged integer number as the second index data type</typeparam>
		/// <typeparam name="TSInd1">The first storage type used by the index array(s)</typeparam>
		/// <typeparam name="TSInd2">The second storage type used by the index array(s)</typeparam>
		/// <param name="conjX">Whether to conjugate <paramref name="x"/> or not</param>
		/// <param name="x">The input sparse vector as a <see cref="ISparseArray{T, TInd, TS, TSInd}"/></param>
		/// <param name="y">The other input sparse vector as a <see cref="ISparseArray{TVal, TInd, TSVal, TSInd}"/></param>
		/// <param name="dot">Output the dot product result as a <typeparamref name="T"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		[AbstractApiMethod]
		public abstract bool VectorSparseDotSparse<T, TInd1, TInd2, TS1, TS2, TSInd1, TSInd2>(bool conjX, ISparseArray<T, TInd1, TS1, TSInd1> x, ISparseArray<T, TInd2, TS2, TSInd2> y, out T dot) where T : unmanaged, INumber<T> where TInd1 : unmanaged, IBinaryInteger<TInd1> where TInd2 : unmanaged, IBinaryInteger<TInd2> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TSInd1 : class, IStorage<TInd1, TSInd1> where TSInd2 : class, IStorage<TInd2, TSInd2>;

		/// <summary>
		/// When implemented by a derived class, add the sparse vector <paramref name="x"/> to another sparse vector <paramref name="y"/> and overwrite the result to <paramref name="target"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS1">The first storage type used by the value array(s) of sparse vector</typeparam>
		/// <typeparam name="TS2">The second storage type used by the value array(s) of sparse vector</typeparam>
		/// <typeparam name="TS3">The output storage type used by the value array(s) of sparse vector</typeparam>
		/// <typeparam name="TInd1">Any unmanaged integer number as the first index data type</typeparam>
		/// <typeparam name="TInd2">Any unmanaged integer number as the second index data type</typeparam>
		/// <typeparam name="TInd3">Any unmanaged integer number as the output index data type</typeparam>
		/// <typeparam name="TSInd1">The first storage type used by the index array(s)</typeparam>
		/// <typeparam name="TSInd2">The second storage type used by the index array(s)</typeparam>
		/// <typeparam name="TSInd3">The output storage type used by the index array(s)</typeparam>
		/// <param name="α">The scalar to multiply to <paramref name="x"/> during computation</param>
		/// <param name="x">The input sparse vector x</param>
		/// <param name="y">The input sparse vector y</param>
		/// <param name="target">Output the result sparse vector of the sum of <paramref name="x"/> and <paramref name="y"/> whose <see cref="SparseArrayWrapper{TVal, TInd, TSVal, TSInd}.Format"/> is the desired output format</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		[AbstractApiMethod]
		public abstract bool VectorSparseAddSparse<T, TInd1, TInd2, TInd3, TS1, TS2, TS3, TSInd1, TSInd2, TSInd3>(T α, ISparseArray<T, TInd1, TS1, TSInd1> x, ISparseArray<T, TInd2, TS2, TSInd2> y, ref SparseArrayWrapper<T, TInd3, TS3, TSInd3> target) where T : unmanaged, INumber<T> where TInd1 : unmanaged, IBinaryInteger<TInd1> where TInd2 : unmanaged, IBinaryInteger<TInd2> where TInd3 : unmanaged, IBinaryInteger<TInd3> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3> where TSInd1 : class, IStorage<TInd1, TSInd1> where TSInd2 : class, IStorage<TInd2, TSInd2> where TSInd3 : class, IStorage<TInd3, TSInd3>;

		/// <summary>
		/// When implemented by a derived class, point-wise multiply a sparse vector by a dense vector: <c><paramref name="x"/> *= <paramref name="y"/></c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS1">The storage type used by the value array(s) of sparse vector</typeparam>
		/// <typeparam name="TS2">The storage type used by the dense vector</typeparam>
		/// <typeparam name="TInd">Any unmanaged integer number as the index data type</typeparam>
		/// <typeparam name="TSInd">The storage type used by the index array(s)</typeparam>
		/// <param name="x">The input/output sparse vector x</param>
		/// <param name="y">The input dense vector y</param>
		/// <param name="strideY">The stride between consecutive elements in <paramref name="y"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		[AbstractApiMethod]
		public abstract bool VectorSparsePointWiseMultiplyDense<T, TInd, TS1, TS2, TSInd>(ISparseArray<T, TInd, TS1, TSInd> x, TS2 y, long strideY) where T : unmanaged, INumber<T> where TInd : unmanaged, IBinaryInteger<TInd> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TSInd : class, IStorage<TInd, TSInd>;

		/// <summary>
		/// When implemented by a derived class, point-wise divide a sparse vector by a dense vector: <c><paramref name="x"/> *= <paramref name="y"/></c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS1">The storage type used by the value array(s) of sparse vector</typeparam>
		/// <typeparam name="TS2">The storage type used by the dense vector</typeparam>
		/// <typeparam name="TInd">Any unmanaged integer number as the index data type</typeparam>
		/// <typeparam name="TSInd">The storage type used by the index array(s)</typeparam>
		/// <param name="x">The input/output sparse vector x</param>
		/// <param name="y">The input dense vector y</param>
		/// <param name="strideY">The stride between consecutive elements in <paramref name="y"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		[AbstractApiMethod]
		public abstract bool VectorSparsePointWiseDivideDense<T, TInd, TS1, TS2, TSInd>(ISparseArray<T, TInd, TS1, TSInd> x, TS2 y, long strideY) where T : unmanaged, INumber<T> where TInd : unmanaged, IBinaryInteger<TInd> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TSInd : class, IStorage<TInd, TSInd>;
		#endregion

		#region vector and matrix
		/// <summary>
		/// When implemented by a derived class, compute the sparse matrix dense vector multiplication: <c><paramref name="y"/> = <paramref name="α"/> * <paramref name="op"/>(<paramref name="M"/>) * <paramref name="x"/> + <paramref name="β"/> * <paramref name="y"/></c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS1">The storage type used by the value array(s)</typeparam>
		/// <typeparam name="TS2">The first storage type used by the dense vector</typeparam>
		/// <typeparam name="TS3">The second storage type used by the dense vector</typeparam>
		/// <typeparam name="TInd">Any unmanaged integer number as the index data type</typeparam>
		/// <typeparam name="TSInd">The storage type used by the index array(s)</typeparam>
		/// <param name="op">The <see cref="MatrixOperation"/> to indicate the simple operation to <paramref name="M"/></param>
		/// <param name="M">The input sparse matrix M</param>
		/// <param name="x">The input dense vector x</param>
		/// <param name="y">The input/output dense vector y</param>
		/// <param name="α">The scalar to multiply <paramref name="M"/></param>
		/// <param name="β">The scalar to multiply <paramref name="y"/></param>
		/// <param name="strideX">The stride between consecutive elements in <paramref name="x"/></param>
		/// <param name="strideY">The stride between consecutive elements in <paramref name="y"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> or <paramref name="M"/> is null or invalid</exception>
		[AbstractApiMethod]
		public abstract bool MatrixSparseMultiplyVectorDense<T, TInd, TS1, TS2, TS3, TSInd>(MatrixOperation op, T α, ISparseArray<T, TInd, TS1, TSInd> M, TS2 x, long strideX, T β, TS3 y, long strideY) where T : unmanaged, INumber<T> where TInd : unmanaged, IBinaryInteger<TInd> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3> where TSInd : class, IStorage<TInd, TSInd>;

		/// <summary>
		/// When implemented by a derived class, compute the dense matrix sparse vector multiplication: <c><paramref name="y"/> = <paramref name="α"/> * <paramref name="op"/>(<paramref name="M"/>) * <paramref name="x"/> + <paramref name="β"/> * <paramref name="y"/></c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS1">The storage type used by the value array(s)</typeparam>
		/// <typeparam name="TS2">The first storage type used by the dense vector</typeparam>
		/// <typeparam name="TS3">The second storage type used by the dense vector</typeparam>
		/// <typeparam name="TInd">Any unmanaged integer number as the index data type</typeparam>
		/// <typeparam name="TSInd">The storage type used by the index array(s)</typeparam>
		/// <param name="op">The <see cref="MatrixOperation"/> to indicate the simple operation to <paramref name="M"/></param>
		/// <param name="α">The scalar to multiply <paramref name="M"/></param>
		/// <param name="m">The number of rows of <paramref name="op"/>(<paramref name="M"/>) (the number of columns is implied in <paramref name="x"/>)</param>
		/// <param name="M">The input dense matrix</param>
		/// <param name="ldm">The leading dimension of <paramref name="M"/></param>
		/// <param name="x">The input sparse vector x</param>
		/// <param name="y">The input/output dense vector y</param>
		/// <param name="β">The scalar to multiply <paramref name="y"/></param>
		/// <param name="strideY">The stride between consecutive elements in <paramref name="y"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> or <paramref name="M"/> is null or invalid</exception>
		[AbstractApiMethod]
		public abstract bool MatrixDenseMultiplyVectorSparse<T, TInd, TS1, TS2, TS3, TSInd>(MatrixOperation op, T α, long m, TS2 M, long ldm, ISparseArray<T, TInd, TS1, TSInd> x, T β, TS3 y, long strideY) where T : unmanaged, INumber<T> where TInd : unmanaged, IBinaryInteger<TInd> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3> where TSInd : class, IStorage<TInd, TSInd>;

		/// <summary>
		/// When implemented by a derived class, compute sparse vector outer product: <c><paramref name="x"/> * <paramref name="y"/>^op</c>, <c>op = <paramref name="conjY"/> ? H : T</c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS1">The first storage type used by the value array(s) of sparse vector</typeparam>
		/// <typeparam name="TS2">The second storage type used by the value array(s) of sparse vector</typeparam>
		/// <typeparam name="TS3">The output storage type used by the value array(s) of sparse matrix</typeparam>
		/// <typeparam name="TInd1">Any unmanaged integer number as the first index data type</typeparam>
		/// <typeparam name="TInd2">Any unmanaged integer number as the second index data type</typeparam>
		/// <typeparam name="TInd3">Any unmanaged integer number as the output index data type</typeparam>
		/// <typeparam name="TSInd1">The first storage type used by the index array(s)</typeparam>
		/// <typeparam name="TSInd2">The second storage type used by the index array(s)</typeparam>
		/// <typeparam name="TSInd3">The output storage type used by the index array(s)</typeparam>
		/// <param name="conjY">Whether to conjugate <paramref name="y"/> or not</param>
		/// <param name="x">The input sparse vector x</param>
		/// <param name="y">The input sparse vector y</param>
		/// <param name="target">Output a new sparse matrix as the outer product with whose <see cref="SparseArrayWrapper{TVal, TInd, TSVal, TSInd}.Format"/> is the desired format</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		[AbstractApiMethod]
		public abstract bool VectorSparseOuter<T, TInd1, TInd2, TInd3, TS1, TS2, TS3, TSInd1, TSInd2, TSInd3>(bool conjY, ISparseArray<T, TInd1, TS1, TSInd1> x, ISparseArray<T, TInd2, TS2, TSInd2> y, ref SparseArrayWrapper<T, TInd3, TS3, TSInd3> target) where T : unmanaged, INumber<T> where TInd1 : unmanaged, IBinaryInteger<TInd1> where TInd2 : unmanaged, IBinaryInteger<TInd2> where TInd3 : unmanaged, IBinaryInteger<TInd3> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3> where TSInd1 : class, IStorage<TInd1, TSInd1> where TSInd2 : class, IStorage<TInd2, TSInd2> where TSInd3 : class, IStorage<TInd3, TSInd3>;
		#endregion

		#region matrix
		/// <summary>
		/// When implemented by a derived class, slice the given sparse <paramref name="matrix"/> with the given <paramref name="slice"/> ParameterError.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="matrix">The input sparse matrix to be sliced</param>
		/// <param name="slice">The slicing parameters as a <see cref="MatrixSliceWrapper"/></param>
		/// <param name="sub">Output the sliced sub sparse matrix of <paramref name="matrix"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="matrix"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="slice"/> is default</exception>
		[AbstractApiMethod]
		public abstract bool SparseMatrixSlice<T>(ISparseMatrix<T> matrix, MatrixSliceWrapper slice, out SparseArrayWrapper<T> sub) where T : unmanaged, INumber<T>;

		/// <summary>
		/// When implemented by a derived class, slice the given sparse <paramref name="matrix"/> with the given <paramref name="slice"/> parameter and overwrite the <paramref name="sub"/> matrix.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="matrix">The input sparse matrix to be sliced</param>
		/// <param name="slice">The slicing parameters as a <see cref="MatrixSliceWrapper"/></param>
		/// <param name="sub">The sub sparse matrix to be overwritten</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="matrix"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="slice"/> is default</exception>
		/// <exception cref="ArgumentException">If <paramref name="sub"/> cannot be overwritten by the sliced <paramref name="matrix"/></exception>
		[AbstractApiMethod]
		public abstract bool SparseMatrixSlice<T>(ISparseMatrix<T> matrix, MatrixSliceWrapper slice, ISparseMatrix<T> sub) where T : unmanaged, INumber<T>;

		/// <summary>
		/// When implemented by a derived class, slice the given sparse <paramref name="matrix"/> with the given <paramref name="slice"/> parameter and overwrite the <paramref name="sub"/> dense matrix.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="matrix">The input sparse matrix to be sliced</param>
		/// <param name="slice">The slicing parameters as a <see cref="MatrixSliceWrapper"/></param>
		/// <param name="sub">The sub dense matrix to be overwritten</param>
		/// <param name="subLD">The leading dimension of <paramref name="sub"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="matrix"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="slice"/> is default or <paramref name="subLD"/> is out of range</exception>
		[AbstractApiMethod]
		public abstract bool SparseMatrixSlice<T>(ISparseMatrix<T> matrix, MatrixSliceWrapper slice, Storage<T> sub, long subLD) where T : unmanaged, INumber<T>;

		/// <summary>
		/// When implemented by a derived class, set the given sparse <paramref name="matrix"/>'s <paramref name="slice"/> parameter and overwrite the <paramref name="sub"/> matrix.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="matrix">The input sparse matrix whose slice will be overwritten</param>
		/// <param name="slice">The slicing parameters as a <see cref="MatrixSliceWrapper"/></param>
		/// <param name="sub">The sub sparse matrix to overwrite the sliced <paramref name="matrix"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="matrix"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="slice"/> is default</exception>
		/// <exception cref="ArgumentException">If <paramref name="sub"/> cannot overwrite the sliced <paramref name="matrix"/></exception>
		[AbstractApiMethod]
		public abstract bool SparseMatrixSetSlice<T>(ISparseMatrix<T> matrix, MatrixSliceWrapper slice, ISparseMatrix<T> sub) where T : unmanaged, INumber<T>;

		/// <summary>
		/// When implemented by a derived class, perform the dense matrix and sparse matrix addition: <c><paramref name="C"/> = <paramref name="α"/> * <paramref name="opA"/>(<paramref name="A"/>) + <paramref name="β"/> * <paramref name="opB"/>(<paramref name="B"/>)</c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="opA">The simple operation to matrix <paramref name="A"/> as a <see cref="MatrixOperation"/></param>
		/// <param name="opB">The simple operation to matrix <paramref name="B"/> as a <see cref="MatrixOperation"/></param>
		/// <param name="α">The scalar to multiply to <paramref name="A"/></param>
		/// <param name="A">The input dense matrix</param>
		/// <param name="lda">The leading dimension of <paramref name="A"/></param>
		/// <param name="β">The scalar to multiply to <paramref name="B"/></param>
		/// <param name="B">The input sparse matrix</param>
		/// <param name="C">The input/output dense matrix</param>
		/// <param name="ldc">The leading dimension of <paramref name="C"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="B"/> or <paramref name="C"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If both <paramref name="α"/> and <paramref name="β"/> are 0</exception>
		[AbstractApiMethod]
		public abstract bool MatrixDenseAddSparse<T>(MatrixOperation opA, MatrixOperation opB, T α, Storage<T> A, long lda, T β, ISparseMatrix<T> B, Storage<T> C, long ldc) where T : unmanaged, INumber<T>;

		/// <summary>
		/// When implemented by a derived class, perform the sparse matrices addition: <c><paramref name="α"/> * <paramref name="opA"/>(<paramref name="A"/>) + <paramref name="β"/> * <paramref name="opB"/>(<paramref name="B"/>)</c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="opA">The simple operation to matrix <paramref name="A"/> as a <see cref="MatrixOperation"/></param>
		/// <param name="opB">The simple operation to matrix <paramref name="B"/> as a <see cref="MatrixOperation"/></param>
		/// <param name="α">The scalar to multiply to <paramref name="A"/></param>
		/// <param name="A">The first input sparse matrix</param>
		/// <param name="β">The scalar to multiply to <paramref name="B"/></param>
		/// <param name="B">The second input sparse matrix</param>
		/// <param name="format">The desired output sparse matrix's format, can be anatomic</param>
		/// <param name="target">Output a new sparse matrix as the summation with format fitting <paramref name="format"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <remarks>If <paramref name="A"/> is null or <paramref name="α"/> is 0, the simple matrix operation <paramref name="opB"/> will be applied to <paramref name="B"/> and the returned sparse matrix may overlap with <paramref name="B"/>. The same for <paramref name="A"/>. However, they cannot be both null or 0.</remarks>
		/// <exception cref="ArgumentNullException">If both <paramref name="A"/> and <paramref name="B"/> are null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If both <paramref name="α"/> and <paramref name="β"/> are 0</exception>
		[AbstractApiMethod]
		public abstract bool MatrixSparseAddSparse<T>(MatrixOperation opA, MatrixOperation opB, T α, ISparseMatrix<T>? A, T β, ISparseMatrix<T>? B, out SparseArrayWrapper<T> target, SparseMatrixFormat format = FormatExtension.MatrixAny) where T : unmanaged, INumber<T>;

		/// <summary>
		/// When implemented by a derived class, perform the sparse matrices multiplication: <c><paramref name="α"/> * <paramref name="opA"/>(<paramref name="A"/>) * <paramref name="opB"/>(<paramref name="B"/>) + <paramref name="β"/> * <paramref name="C"/></c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="opA">The simple operation to matrix <paramref name="A"/> as a <see cref="MatrixOperation"/></param>
		/// <param name="opB">The simple operation to matrix <paramref name="B"/> as a <see cref="MatrixOperation"/></param>
		/// <param name="α">The scalar to multiply to <paramref name="A"/></param>
		/// <param name="A">The first input sparse matrix</param>
		/// <param name="B">The second input sparse matrix</param>
		/// <param name="β">The scalar to multiply to <paramref name="C"/></param>
		/// <param name="C">The third input sparse matrix, can be null. If this is null or <paramref name="β"/> is 0, no addition will be performed</param>
		/// <param name="format">The desired output sparse matrix's format, can be anatomic</param>
		/// <param name="target">Output a new sparse matrix as the product (and sum) with format fitting <paramref name="format"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="B"/> or <paramref name="C"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If both <paramref name="α"/> and <paramref name="β"/> are 0</exception>
		[AbstractApiMethod]
		public abstract bool MatrixSparseMultiplySparse<T>(MatrixOperation opA, MatrixOperation opB, T α, ISparseMatrix<T> A, ISparseMatrix<T> B, T β, ISparseMatrix<T>? C, out SparseArrayWrapper<T> target, SparseMatrixFormat format = FormatExtension.MatrixAny) where T : unmanaged, INumber<T>;

		/// <summary>
		/// When implemented by a derived class, perform the dense matrix and sparse matrix multiplication: <c><paramref name="C"/> = <paramref name="α"/> * <paramref name="opA"/>(<paramref name="A"/>) * <paramref name="opB"/>(<paramref name="B"/>) + <paramref name="β"/> * <paramref name="C"/></c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="m">The number of rows of <paramref name="opA"/>(<paramref name="A"/>). (The number of columns is implied from <paramref name="opB"/> and <paramref name="B"/>)</param>
		/// <param name="opA">The simple operation to matrix <paramref name="A"/> as a <see cref="MatrixOperation"/></param>
		/// <param name="opB">The simple operation to matrix <paramref name="B"/> as a <see cref="MatrixOperation"/></param>
		/// <param name="α">The scalar to multiply to <paramref name="A"/></param>
		/// <param name="A">The input dense matrix</param>
		/// <param name="B">The input sparse matrix</param>
		/// <param name="lda">The leading dimension of <paramref name="A"/></param>
		/// <param name="β">The scalar to multiply to <paramref name="C"/></param>
		/// <param name="C">The input/output dense matrix</param>
		/// <param name="ldc">The leading dimension of <paramref name="C"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="B"/> or <paramref name="C"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If both <paramref name="α"/> and <paramref name="β"/> are 0</exception>
		[AbstractApiMethod]
		public abstract bool MatrixDenseMultiplySparse<T>(MatrixOperation opA, MatrixOperation opB, long m, T α, Storage<T> A, long lda, ISparseMatrix<T> B, T β, Storage<T> C, long ldc) where T : unmanaged, INumber<T>;

		/// <summary>
		/// When implemented by a derived class, perform the dense matrix and sparse matrix multiplication: <c><paramref name="C"/> = <paramref name="α"/> * <paramref name="opA"/>(<paramref name="A"/>) * <paramref name="opB"/>(<paramref name="B"/>) + <paramref name="β"/> * <paramref name="C"/></c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="n">The number of columns of <paramref name="opB"/>(<paramref name="B"/>). (The number of rows is implied from <paramref name="opA"/> and <paramref name="A"/>)</param>
		/// <param name="opA">The simple operation to matrix <paramref name="A"/> as a <see cref="MatrixOperation"/></param>
		/// <param name="opB">The simple operation to matrix <paramref name="B"/> as a <see cref="MatrixOperation"/></param>
		/// <param name="α">The scalar to multiply to <paramref name="A"/></param>
		/// <param name="A">The input sparse matrix</param>
		/// <param name="B">The input dense matrix</param>
		/// <param name="ldb">The leading dimension of <paramref name="B"/></param>
		/// <param name="β">The scalar to multiply to <paramref name="C"/></param>
		/// <param name="C">The input/output dense matrix</param>
		/// <param name="ldc">The leading dimension of <paramref name="C"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="B"/> or <paramref name="C"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If both <paramref name="α"/> and <paramref name="β"/> are 0</exception>
		[AbstractApiMethod]
		public abstract bool MatrixSparseMultiplyDense<T>(MatrixOperation opA, MatrixOperation opB, long n, T α, ISparseMatrix<T> A, Storage<T> B, long ldb, T β, Storage<T> C, long ldc) where T : unmanaged, INumber<T>;

		/// <summary>
		/// When implemented by a derived class, perform the sparse matrices Kronecker product: <c><paramref name="A"/> ⨂ <paramref name="B"/></c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="A">The first input sparse matrix</param>
		/// <param name="B">The second input sparse matrix</param>
		/// <param name="format">The desired output sparse matrix's format, can be anatomic</param>
		/// <param name="target">Output a new sparse matrix as the Kronecker product with format fitting <paramref name="format"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="B"/> is null or invalid</exception>
		[AbstractApiMethod]
		public abstract bool MatrixSparseKronecker<T>(ISparseMatrix<T> A, ISparseMatrix<T> B, out SparseArrayWrapper<T> target, SparseMatrixFormat format = FormatExtension.MatrixAny) where T : unmanaged, INumber<T>;
		#endregion
	}
}