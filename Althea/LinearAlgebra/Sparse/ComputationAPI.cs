using System;

using Althea.Arrays;
using Althea.Helpers;

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
		/// <param name="α">The scalar to multiply <paramref name="x"/></param>
		/// <param name="x">The input sparse vector as a <see cref="ISparseVector{T}"/></param>
		/// <param name="y">The input/output dense vector as a <see cref="Storage{T}"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		/// <exception cref="TypeMismatchException">If the <see cref="ISparseArray{T}.IndexType"/> is not an integral type</exception>
		protected abstract bool VectorSparseAddToDense<T>(T α, ISparseVector<T> x, Storage<T> y) where T : unmanaged, INumber<T>;

		/// <summary>
		/// When implemented by a derived class, calculate the dot (inner) product of a sparse vector <paramref name="x"/> and a dense vector <paramref name="y"/>: result = <c><paramref name="x"/>^op <paramref name="y"/></c>, op = <paramref name="conjX"/> ? H : T.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="conjX">Whether to conjugate <paramref name="x"/> or not</param>
		/// <param name="x">The input sparse vector as a <see cref="ISparseVector{T}"/></param>
		/// <param name="y">The input dense vector as a <see cref="Storage{T}"/></param>
		/// <param name="dot">Output the dot product result as a <typeparamref name="T"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		protected abstract bool VectorSparseDotDense<T>(bool conjX, ISparseVector<T> x, Storage<T> y, out T dot) where T : unmanaged, INumber<T>;

		/// <summary>
		/// When implemented by a derived class, calculate the dot (inner) product of two sparse vectors <paramref name="x"/> and <paramref name="y"/>: result = <c><paramref name="x"/>^op <paramref name="y"/></c>, op = <paramref name="conjX"/> ? H : T.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="conjX">Whether to conjugate <paramref name="x"/> or not</param>
		/// <param name="x">The input sparse vector as a <see cref="ISparseVector{T}"/></param>
		/// <param name="y">The other input sparse vector as a <see cref="ISparseVector{T}"/></param>
		/// <param name="dot">Output the dot product result as a <typeparamref name="T"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		protected abstract bool VectorSparseDotSparse<T>(bool conjX, ISparseVector<T> x, ISparseVector<T> y, out T dot) where T : unmanaged, INumber<T>;

		/// <summary>
		/// When implemented by a derived class, add the sparse vector <paramref name="x"/> to another sparse vector <paramref name="y"/> and put the result in a new sparse vector.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="x">The input sparse vector x</param>
		/// <param name="y">The input sparse vector y</param>
		/// <param name="format">The desired output sparse vector's <see cref="SparseVectorFormat"/>, can be anatomic</param>
		/// <param name="target">Output the result sparse vector of the sum of <paramref name="x"/> and <paramref name="y"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		protected abstract bool VectorSparseAddSparse<T>(ISparseVector<T> x, ISparseVector<T> y, out SparseArrayWrapper<T> target, SparseVectorFormat format = FormatExtension.VectorAny) where T : unmanaged, INumber<T>;

		/// <summary>
		/// When implemented by a derived class, point-wise multiply a sparse vector by a dense vector: <c><paramref name="x"/> *= <paramref name="y"/></c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="x">The input/output sparse vector x</param>
		/// <param name="y">The input dense vector y</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		protected abstract bool VectorSparsePointWiseMultiplyDense<T>(ISparseVector<T> x, Storage<T> y) where T : unmanaged, INumber<T>;

		/// <summary>
		/// When implemented by a derived class, point-wise divide a sparse vector by a dense vector: <c><paramref name="x"/> /= <paramref name="y"/></c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="x">The input/output sparse vector x</param>
		/// <param name="y">The input dense vector y</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		protected abstract bool VectorSparsePointWiseDivideDense<T>(ISparseVector<T> x, Storage<T> y) where T : unmanaged, INumber<T>;
		#endregion

		#region vector and matrix
		/// <summary>
		/// When implemented by a derived class, compute the sparse matrix dense vector multiplication: <c><paramref name="y"/> = <paramref name="α"/> * <paramref name="op"/>(<paramref name="M"/>) * <paramref name="x"/> + <paramref name="β"/> * <paramref name="y"/></c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="op">The <see cref="MatrixOperation"/> to indicate the simple operation to <paramref name="M"/></param>
		/// <param name="M">The input sparse matrix M</param>
		/// <param name="x">The input dense vector x</param>
		/// <param name="y">The input/output dense vector y</param>
		/// <param name="α">The scalar to multiply <paramref name="M"/></param>
		/// <param name="β">The scalar to multiply <paramref name="y"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> or <paramref name="M"/> is null or invalid</exception>
		protected abstract bool MatrixSparseMultiplyVectorDense<T>(MatrixOperation op, T α, ISparseMatrix<T> M, Storage<T> x, T β, Storage<T> y) where T : unmanaged, INumber<T>;

		/// <summary>
		/// When implemented by a derived class, compute the dense matrix sparse vector multiplication: <c><paramref name="y"/> = <paramref name="α"/> * <paramref name="op"/>(<paramref name="M"/>) * <paramref name="x"/> + <paramref name="β"/> * <paramref name="y"/></c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="op">The <see cref="MatrixOperation"/> to indicate the simple operation to <paramref name="M"/></param>
		/// <param name="α">The scalar to multiply <paramref name="M"/></param>
		/// <param name="m">The number of rows of <paramref name="op"/>(<paramref name="M"/>) (the number of columns is implied in <paramref name="x"/>)</param>
		/// <param name="M">The input dense matrix M</param>
		/// <param name="ldm">The leading dimension of <paramref name="M"/></param>
		/// <param name="x">The input sparse vector x</param>
		/// <param name="y">The input/output dense vector y</param>
		/// <param name="β">The scalar to multiply <paramref name="y"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> or <paramref name="M"/> is null or invalid</exception>
		protected abstract bool MatrixDenseMultiplyVectorSparse<T>(MatrixOperation op, T α, long m, Storage<T> M, long ldm, ISparseVector<T> x, T β, Storage<T> y) where T : unmanaged, INumber<T>;

		/// <summary>
		/// When implemented by a derived class, compute sparse vector outer product: <c><paramref name="x"/> * <paramref name="y"/>^op</c>, <c>op = <paramref name="conjY"/> ? H : T</c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="conjY">Whether to conjugate <paramref name="y"/> or not</param>
		/// <param name="x">The input sparse vector x</param>
		/// <param name="y">The input sparse vector y</param>
		/// <param name="format">The desired output sparse matrix's format, can be anatomic</param>
		/// <param name="target">Output a new sparse matrix as the outer product with format fitting <paramref name="format"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		protected abstract bool VectorSparseOuter<T>(bool conjY, ISparseVector<T> x, ISparseVector<T> y, out SparseArrayWrapper<T> target, SparseMatrixFormat format = FormatExtension.MatrixAny) where T : unmanaged, INumber<T>;
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
		protected abstract bool SparseMatrixSlice<T>(ISparseMatrix<T> matrix, MatrixSliceWrapper slice, out SparseArrayWrapper<T> sub) where T : unmanaged, INumber<T>;

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
		protected abstract bool SparseMatrixSlice<T>(ISparseMatrix<T> matrix, MatrixSliceWrapper slice, ISparseMatrix<T> sub) where T : unmanaged, INumber<T>;

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
		protected abstract bool SparseMatrixSlice<T>(ISparseMatrix<T> matrix, MatrixSliceWrapper slice, Storage<T> sub, long subLD) where T : unmanaged, INumber<T>;

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
		protected abstract bool SparseMatrixSetSlice<T>(ISparseMatrix<T> matrix, MatrixSliceWrapper slice, ISparseMatrix<T> sub) where T : unmanaged, INumber<T>;

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
		protected abstract bool MatrixDenseAddSparse<T>(MatrixOperation opA, MatrixOperation opB, T α, Storage<T> A, long lda, T β, ISparseMatrix<T> B, Storage<T> C, long ldc) where T : unmanaged, INumber<T>;

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
		protected abstract bool MatrixSparseAddSparse<T>(MatrixOperation opA, MatrixOperation opB, T α, ISparseMatrix<T>? A, T β, ISparseMatrix<T>? B, out SparseArrayWrapper<T> target, SparseMatrixFormat format = FormatExtension.MatrixAny) where T : unmanaged, INumber<T>;

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
		protected abstract bool MatrixSparseMultiplySparse<T>(MatrixOperation opA, MatrixOperation opB, T α, ISparseMatrix<T> A, ISparseMatrix<T> B, T β, ISparseMatrix<T>? C, out SparseArrayWrapper<T> target, SparseMatrixFormat format = FormatExtension.MatrixAny) where T : unmanaged, INumber<T>;

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
		protected abstract bool MatrixDenseMultiplySparse<T>(MatrixOperation opA, MatrixOperation opB, long m, T α, Storage<T> A, long lda, ISparseMatrix<T> B, T β, Storage<T> C, long ldc) where T : unmanaged, INumber<T>;

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
		protected abstract bool MatrixSparseMultiplyDense<T>(MatrixOperation opA, MatrixOperation opB, long n, T α, ISparseMatrix<T> A, Storage<T> B, long ldb, T β, Storage<T> C, long ldc) where T : unmanaged, INumber<T>;

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
		protected abstract bool MatrixSparseKronecker<T>(ISparseMatrix<T> A, ISparseMatrix<T> B, out SparseArrayWrapper<T> target, SparseMatrixFormat format = FormatExtension.MatrixAny) where T : unmanaged, INumber<T>;
		#endregion
	}
}