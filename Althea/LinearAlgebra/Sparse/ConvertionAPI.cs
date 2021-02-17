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
		/// Encapsulates a method that receive the total <paramref name="length"/> in <typeparamref name="T"/> as the parameter and return an <b>allocated</b> new <see cref="ISparseVector{T}"/> of the given length (non-zeros).
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="length">The desired total length in <typeparamref name="T"/></param>
		/// <returns>An <b>allocated</b> new <see cref="ISparseVector{T}"/> of <paramref name="length"/></returns>
		/// <remarks>
		/// This delegate is usually used as a nullable parameter of methods in <see cref="AbstractApi"/>.<br/>
		/// The default implementation typically shall utilize <c><see cref="Storage.StorageFactory{T}"/>.<see cref="Storage.StorageFactory{T}.CreateAlike">CreateAlike</see>(input_storage.<see cref="Storage{T}.MakeReference">MakeReference</see>(0, <paramref name="length"/>))</c>
		/// </remarks>
		public delegate ISparseVector<T> DelegateCreateNew<T>(long length) where T : unmanaged;
		#endregion

		#region vector
		/// <summary>
		/// When implemented by a derived class, scatter (and overwrite) the sparse vector <paramref name="x"/> to the dense vector <paramref name="y"/>: <paramref name="y"/>[<paramref name="x"/>.Indices] = <paramref name="x"/>.<see cref="ISparseVector{T}.Storage">Values</see>.
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
		/// <param name="x">The input vector whose values will be set</param>
		/// <param name="positions">The given positions as a <see cref="Storage{T}"/> of <see cref="int"/></param>
		/// <param name="value">The value to set</param>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="positions"/> is null or invalid</exception>
		public abstract void SetVectorValuesAt<T>(Storage<T> x, T value, Storage<int> positions) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, gather the dense vector <paramref name="x"/> at <paramref name="pos"/> into another vector <paramref name="y"/>: <paramref name="y"/> = <paramref name="x"/>[<paramref name="pos"/>].
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The input dense vector as a <see cref="Storage{T}"/> to gather from</param>
		/// <param name="pos">The input indices indicating where to gather as a <see cref="Storage{T}"/> of <see cref="int"/></param>
		/// <param name="y">The output vector to gather to as a <see cref="Storage{T}"/>, the first <paramref name="pos"/>.<see cref="Storage{T}.Length">Length</see> elements will be overwritten.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> or <paramref name="pos"/> is null or invalid</exception>
		public abstract void VectorGatherAtIndices<T>(Storage<T> x, Storage<int> pos, Storage<T> y) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, convert a dense vector <paramref name="x"/> to a sparse vector by the given truncation <paramref name="threshold"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The input dense vector as a <see cref="Storage{T}"/></param>
		/// <param name="threshold">Any element in <paramref name="x"/> whose absolute value is less than or equals to <paramref name="threshold"/> will be regarded as zero</param>
		/// <param name="createFunc">See <see cref="DelegateCreateNew{T}"/></param>
		/// <returns>The result sparse vector as a <see cref="ISparseVector{T}"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		public abstract ISparseVector<T> VectorDenseToSparse<T>(Storage<T> x, float threshold = 0, DelegateCreateNew<T>? createFunc = null) where T : unmanaged;
		#endregion

		#region vector and matrix
		/// <summary>
		/// When implemented by a derived class, convert the indices of a sparse vector to or from a sparse COO matrix's index arrays.
		/// </summary>
		/// <param name="toCOO">Whether to convert to COO index arrays or backward -- if true, <paramref name="ind"/> is input; otherwise, <paramref name="ind"/> is output</param>
		/// <param name="ind">The input/output indices of sparse vector</param>
		/// <param name="row">The output/input COO matrix's row index array</param>
		/// <param name="col">The output/input COO matrix's column index array</param>
		/// <param name="ld">The number of rows of the matrix</param>
		public abstract void MatrixVectorCOOToFromSparseIndex(bool toCOO, Storage<int> ind, Storage<int> row, Storage<int> col, long ld);
		#endregion
	}
}