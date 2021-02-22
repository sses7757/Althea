using System;

using Althea.Arrays;


namespace Althea.LinearAlgebra.Sparse
{
	/// <summary>
	/// The abstract class for runtime sparse linear algebra API routines 
	/// </summary>
	public abstract partial class AbstractApi : AbstractRuntimeApi
	{
		#region delegate
		/// <summary>
		/// Encapsulates a method that receive the total <paramref name="length"/> in <typeparamref name="T"/> and the <paramref name="format"/> as the parameters and return an <b>allocated</b> new <see cref="ISparseVector{T}"/> of the given length (non-zeros).
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="length">The desired total length in <typeparamref name="T"/></param>
		/// <param name="format">The desired <see cref="SparseVectorFormat"/></param>
		/// <returns>An <b>allocated</b> new <see cref="ISparseVector{T}"/> of <paramref name="length"/> and <paramref name="format"/></returns>
		/// <remarks>
		/// This delegate is usually used as a nullable parameter of methods in <see cref="AbstractApi"/>.<br/>
		/// The default implementation typically shall utilize <c><see cref="Storage.StorageFactory{T}"/>.<see cref="Storage.StorageFactory{T}.CreateAlike">CreateAlike</see>(input_storage.<see cref="Storage{T}.MakeReference">MakeReference</see>(0, <paramref name="length"/>))</c>
		/// </remarks>
		public delegate ISparseVector<T> DelegateCreateVectorNew<T>(long length, SparseVectorFormat format) where T : unmanaged;

		/// <summary>
		/// Encapsulates a method that receive the presenting number of rows <paramref name="rows"/> and number of columns <paramref name="cols"/> in <typeparamref name="T"/> and the <paramref name="format"/> as the parameters and return an <b>allocated</b> new <see cref="ISparseMatrix{T}"/> of the given size.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="rows">The desired presenting number of rows in <typeparamref name="T"/></param>
		/// <param name="cols">The desired presenting number of columns in <typeparamref name="T"/></param>
		/// <param name="format">The desired <see cref="SparseMatrixFormat"/> of the target sparse matrix, must be atomic</param>
		/// <returns>An <b>allocated</b> new <see cref="ISparseMatrix{T}"/> of the given size</returns>
		/// <remarks>
		/// This delegate is usually used as a nullable parameter of methods in <see cref="AbstractApi"/>.<br/>
		/// The default implementation typically shall utilize (multiple) <c><see cref="Storage.StorageFactory{T}"/>.<see cref="Storage.StorageFactory{T}.CreateAlike">CreateAlike</see>(input_storage.<see cref="Storage{T}.MakeReference">MakeReference</see>(0, internal_length))</c>
		/// </remarks>
		public delegate ISparseMatrix<T> DelegateCreateMatrixNew<T>(long rows, long cols, SparseMatrixFormat format) where T : unmanaged;
		#endregion

		#region vector
		/// <summary>
		/// When implemented by a derived class, scatter (and overwrite) the sparse vector <paramref name="x"/> to the dense vector <paramref name="y"/>: <paramref name="y"/>[<paramref name="x"/>.Indices] = <paramref name="x"/>.<see cref="ISparseArray{T}.Storage">Values</see>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The sparse vector x as a <see cref="ISparseVector{T}"/></param>
		/// <param name="y">The dense vector y as a <see cref="Storage{T}"/> whose elements at <paramref name="x"/>.Indices are overwritten</param>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		public abstract void VectorSparseToDense<T>(ISparseVector<T> x, Storage<T> y) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, set the <paramref name="x"/>'s values at certain <paramref name="positions"/> to the give <paramref name="value"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <typeparam name="TInd">Any integral-typed unmanaged struct as the index type</typeparam>
		/// <param name="x">The input vector whose values will be set</param>
		/// <param name="positions">The given positions as a <see cref="Storage{T}"/> of <see cref="int"/></param>
		/// <param name="value">The value to set</param>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="positions"/> is null or invalid</exception>
		public abstract void VectorSetValuesAt<T, TInd>(Storage<T> x, T value, Storage<TInd> positions) where T : unmanaged where TInd : unmanaged;

		/// <summary>
		/// When implemented by a derived class, gather the dense vector <paramref name="x"/> at <paramref name="pos"/> into another vector <paramref name="y"/>: <paramref name="y"/> = <paramref name="x"/>[<paramref name="pos"/>].
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <typeparam name="TInd">Any integral-typed unmanaged struct as the index type</typeparam>
		/// <param name="x">The input dense vector as a <see cref="Storage{T}"/> to gather from</param>
		/// <param name="pos">The input indices indicating where to gather as a <see cref="Storage{T}"/> of <see cref="int"/></param>
		/// <param name="y">The output vector to gather to as a <see cref="Storage{T}"/>, the first <paramref name="pos"/>.<see cref="Storage{T}.Length">Length</see> elements will be overwritten.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> or <paramref name="pos"/> is null or invalid</exception>
		public abstract void VectorGatherValuesAt<T, TInd>(Storage<T> x, Storage<TInd> pos, Storage<T> y) where T : unmanaged where TInd : unmanaged;

		/// <summary>
		/// When implemented by a derived class, convert a dense vector <paramref name="x"/> to a sparse vector by the given truncation <paramref name="threshold"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The input dense vector as a <see cref="Storage{T}"/></param>
		/// <param name="threshold">Any element in <paramref name="x"/> whose absolute value is less than or equals to <paramref name="threshold"/> will be regarded as zero</param>
		/// <param name="format">The desired <see cref="SparseVectorFormat"/> of the target sparse vector, must be atomic</param>
		/// <param name="createFunc">See <see cref="DelegateCreateMatrixNew{T}"/></param>
		/// <returns>The result sparse vector as a <see cref="ISparseVector{T}"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="threshold"/> is less than 0 or <paramref name="format"/> is not atomic</exception>
		public abstract ISparseVector<T> VectorDenseToSparse<T>(Storage<T> x, SparseVectorFormat format, float threshold = 0, DelegateCreateMatrixNew<T>? createFunc = null) where T : unmanaged;
		#endregion

		#region vector and matrix
		/// <summary>
		/// When implemented by a derived class, convert the given sparse <paramref name="vector"/> to a sparse matrix of <paramref name="format"/> and presenting number of <paramref name="rows"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="vector">The input sparse vector as a <see cref="ISparseVector{T}"/></param>
		/// <param name="rows">The desired number of rows of the target sparse matrix (the number of columns is calculated from this)</param>
		/// <param name="format">The desired <see cref="SparseMatrixFormat"/> of the target sparse matrix, must be atomic</param>
		/// <param name="createFunc">See <see cref="DelegateCreateMatrixNew{T}"/></param>
		/// <returns>The created new <see cref="ISparseMatrix{T}"/> of desired properties</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="vector"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="format"/> is not atomic</exception>
		public abstract ISparseMatrix<T> SparseVectorToMatrix<T>(ISparseVector<T> vector, long rows, SparseMatrixFormat format, DelegateCreateMatrixNew<T>? createFunc = null) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, convert the given sparse <paramref name="format"/> to a sparse vector of given <paramref name="format"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="matrix">The input sparse matrix as a <see cref="ISparseMatrix{T}"/></param>
		/// <param name="format">The desired <see cref="SparseVectorFormat"/> of the target sparse vector, must be atomic</param>
		/// <param name="createFunc">See <see cref="DelegateCreateVectorNew{T}"/></param>
		/// <returns>The created new <see cref="ISparseVector{T}"/> of desired properties (the length is the product of <see cref="ISparseMatrix{T}.NRows"/> and <see cref="ISparseMatrix{T}.NCols"/>)</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="matrix"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="format"/> is not atomic</exception>
		public abstract ISparseVector<T> SparseMatrixToVector<T>(ISparseMatrix<T> matrix, SparseVectorFormat format, DelegateCreateVectorNew<T>? createFunc = null) where T : unmanaged;
		#endregion

		#region matrix
		/// <summary>
		/// When implemented by a derived class, convert the given sparse matrix <paramref name="source"/> to a dense matrix <paramref name="destination"/>.
		/// </summary>
		/// <param name="source">The source sparse matrix to convert from</param>
		/// <param name="destination">The storage of the destination dense matrix of the same size as <paramref name="source"/></param>
		/// <param name="ld">The leading dimension of <paramref name="destination"/></param>
		public abstract void MatrixSparseToDense<T>(ISparseMatrix<T> source, Storage<T> destination, long ld) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, convert the given dense matrix <paramref name="source"/> of to the <paramref name="format"/> format.
		/// </summary>
		/// <param name="m">The number of rows of <paramref name="source"/></param>
		/// <param name="n">The number of columns of <paramref name="source"/></param>
		/// <param name="source">The source dense matrix to convert from</param>
		/// <param name="ld">The leading dimension of <paramref name="source"/></param>
		/// <param name="format">The destination <see cref="SparseMatrixFormat"/> of the target sparse matrix, must be atomic</param>
		/// <param name="threshold">Any element in <paramref name="source"/> less than or equals to this value will be regarded as 0</param>
		/// <param name="createFunc">See <see cref="DelegateCreateMatrixNew{T}"/></param>
		/// <returns>A new <see cref="ISparseMatrix{T}"/> of the given properties</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="threshold"/> is less than 0 or <paramref name="format"/> is not atomic</exception>
		public abstract ISparseMatrix<T> MatrixDenseToSparse<T>(long m, long n, Storage<T> source, long ld, SparseMatrixFormat format, float threshold = 0, DelegateCreateMatrixNew<T>? createFunc = null) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, prune the given sparse matrix <paramref name="source"/> to a new one by filtering the values less than or equals to <paramref name="threshold"/>.
		/// </summary>
		/// <param name="source">The source sparse matrix to convert from</param>
		/// <param name="threshold">Any element in <paramref name="source"/> less than or equals to this value will be regarded as 0</param>
		/// <returns>A new <see cref="ISparseMatrix{T}"/> of same properties as <paramref name="source"/> while the values (and the index arrays accordingly) are pruned by <paramref name="threshold"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="threshold"/> is less than 0</exception>
		public abstract ISparseMatrix<T> MatrixSparsePrune<T>(ISparseMatrix<T> source, float threshold) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, convert the format of the given sparse matrix <paramref name="source"/> to a new one which fits <paramref name="format"/>.
		/// </summary>
		/// <param name="source">The source sparse matrix to convert from</param>
		/// <param name="format">The target <see cref="SparseMatrixFormat"/>, can be anatomic</param>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> is null or invalid</exception>
		public abstract ISparseMatrix<T> MatrixSparseFormatConvert<T>(ISparseMatrix<T> source, SparseMatrixFormat format) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, fill the given sparse matrix <paramref name="M"/> with identity matrix.
		/// </summary>
		/// <param name="M">The sparse matrix to be filled with identity</param>
		/// <exception cref="ArgumentNullException">If <paramref name="M"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="M"/> is not a square matrix or its sparsity cannot be filled to be an identity matrix</exception>
		public abstract void MatrixSparseFillIdentity<T>(ISparseMatrix<T> M) where T : unmanaged;

		#endregion
	}
}