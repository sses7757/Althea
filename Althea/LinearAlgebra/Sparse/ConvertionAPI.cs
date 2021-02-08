using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Althea.Linq;


namespace Althea.LinearAlgebra.Sparse
{
	/// <summary>
	/// The abstract class for runtime sparse linear algebra API routines 
	/// </summary>
	public abstract partial class AbstractApi : AbstractRuntimeApi
	{
		#region vector
		/// <summary>
		/// When implemented by a derived class, scatter (and overwrite) the sparse vector <paramref name="x"/> to the dense vector <paramref name="y"/>: <paramref name="y"/>[<paramref name="x"/>.<see cref="SparseVectorWrapper{T}.Indices">Indices</see>] = <paramref name="x"/>.<see cref="SparseVectorWrapper{T}.Values">Values</see>.
		/// </summary>
		/// <param name="x">The sparse vector x as a <see cref="SparseVectorWrapper{T}"/></param>
		/// <param name="y">The dense vector y as a <see cref="Storage{T}"/> whose elements at <paramref name="x"/>.<see cref="SparseVectorWrapper{T}.Indices">Indices</see> are overwritten</param>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		public abstract void VectorSparseToDense<T>(SparseVectorWrapper<T> x, Storage<T> y) where T : unmanaged;

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
		/// <param name="x">The input dense vector as a <see cref="Storage{T}"/> to gather from</param>
		/// <param name="pos">The input indices indicating where to gather as a <see cref="Storage{T}"/> of <see cref="int"/></param>
		/// <param name="y">The output vector to gather to as a <see cref="Storage{T}"/>, the first <paramref name="pos"/>.<see cref="Storage{T}.Length">Length</see> elements will be overwritten.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> or <paramref name="pos"/> is null or invalid</exception>
		public abstract void VectorGatherAtIndices<T>(Storage<T> x, Storage<int> pos, Storage<T> y) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, convert a dense vector <paramref name="x"/> to a sparse vector by the given truncation <paramref name="threshold"/>.
		/// </summary>
		/// <param name="x">The input dense vector as a <see cref="Storage{T}"/></param>
		/// <param name="threshold">the abs value below it will regarded as zero</param>
		/// <returns>The result sparse vector as a <see cref="SparseVectorWrapper{T}"/></returns>
		public abstract SparseVectorWrapper<T> VectorDenseToSparse<T>(Storage<T> x, float threshold = 0) where T : unmanaged;
		#endregion
	}
}