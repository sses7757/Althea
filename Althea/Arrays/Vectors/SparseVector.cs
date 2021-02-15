using System;
using System.Collections.Generic;

using Althea.Linq;
using Althea.NativeTypes;


namespace Althea.Arrays
{
	internal interface ISparseVector<T> : IReadOnlyList<IStorage>
	{
		long NonZero { get; }

		DataType IndexType { get; }
	}

	/// <summary>
	/// The abstract sparse vector class with the only mutable <see cref="ValueArray{T}.Storage"/> that refers to the value array storage.
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct that implements <see cref="IFormattable"/> and <see cref="IEquatable{T}"/> as the data type</typeparam>
	/// <typeparam name="TIndex">Any integer-typed unmanaged struct as the index type</typeparam>
	public abstract class SparseVector<T, TIndex> : VectorBase<T>, ISparseVector<T>, ISparseArray<T, TIndex> where T : unmanaged, IFormattable, IEquatable<T> where TIndex : unmanaged
	{
		#region basic
		/// <summary>
		/// When implemented by a derived class, get the number of nonzero values of this sparse array. The default implementation returns the <see cref="ValueArray{T}.ActualLength"/>.
		/// </summary>
		public virtual long NonZero => this.ActualLength;

		DataType ISparseVector<T>.IndexType => default(TIndex).ToDataType();

		private readonly Storage<TIndex>? m_indexArray = null;

		private readonly Storage<TIndex>[]? m_indexArrays = null;

		/// <summary>
		/// Create a <see cref="SparseVector{T, TIndex}"/> with given <paramref name="length"/>, <paramref name="valueArray"/> and <paramref name="indexArray"/>
		/// </summary>
		/// <param name="length">The presenting length of this sparse vector</param>
		/// <param name="valueArray">The value array as a <see cref="Storage{T}"/> of <typeparamref name="T"/></param>
		/// <param name="indexArray">The index array as a <see cref="Storage{T}"/> of <typeparamref name="TIndex"/></param>
		/// <exception cref="NotSupportedException">If the <typeparamref name="TIndex"/> is not an integral type</exception>
		protected SparseVector(long length, Storage<T> valueArray, Storage<TIndex> indexArray) : base(valueArray, length)
		{
			var type = default(TIndex).GetClassification();
			if (type != DataTypeClassification.SignedInteger && type != DataTypeClassification.UnsignedInteger)
				throw new NotSupportedException(Resources.Support.DataType);

			this.m_indexArray = indexArray; this.m_indexArrays = null;
		}

		/// <summary>
		/// Create a <see cref="SparseVector{T, TIndex}"/> with given <paramref name="length"/>, <paramref name="valueArray"/> and <paramref name="indexArrays"/>
		/// </summary>
		/// <param name="length">The presenting length of this sparse vector</param>
		/// <param name="valueArray">The value array as a <see cref="Storage{T}"/> of <typeparamref name="T"/></param>
		/// <param name="indexArrays">The index array(s) as a list of <see cref="Storage{T}"/> of <typeparamref name="TIndex"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="indexArrays"/> is null or empty</exception>
		/// <exception cref="NotSupportedException">If the <typeparamref name="TIndex"/> is not an integral type</exception>
		protected SparseVector(long length, Storage<T> valueArray, params Storage<TIndex>[] indexArrays) : base(valueArray, length)
		{
			if (indexArrays is null || indexArrays.Length == 0)
				throw new ArgumentNullException(nameof(indexArrays));
			var type = default(TIndex).GetClassification();
			if (type != DataTypeClassification.SignedInteger && type != DataTypeClassification.UnsignedInteger)
				throw new NotSupportedException(Resources.Support.DataType);

			if (indexArrays.Length == 1)
			{
				this.m_indexArray = indexArrays[0]; this.m_indexArrays = null;
			}
			else
			{
				this.m_indexArray = null; this.m_indexArrays = (Storage<TIndex>[])indexArrays.Clone();
			}
		}
		#endregion

		#region dispose
		/// <summary>
		/// When implemented by a derived class, actually the dispose this array. The default implementation only disposes <see cref="ValueArray{T}.Storage"/> and the index array(s) passed to the constructor of <see cref="SparseVector{T, TIndex}"/>.
		/// </summary>
		/// <param name="disposing">Dispose managed resources or not</param>
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			this.m_indexArray?.Dispose();
			if (this.m_indexArrays is not null)
			{
				for (int i = 0; i < this.m_indexArrays.Length; i++)
				{
					this.m_indexArrays[i]?.Dispose();
				}
			}
		}

		/// <summary>
		/// When implemented by a derived class, dispose this sparse array after excluding the internal storages shared between this array and the target <paramref name="array"/>.  The default implementation only compares <see cref="ValueArray{T}.Storage"/> and the index array(s) passed to the constructor of <see cref="SparseVector{T, TIndex}"/>.
		/// </summary>
		/// <param name="array">The target <see cref="ISparseArray{T, TIndex}"/> to exclude before disposing this sparse vector</param>
		public virtual void DisposeExclude(ISparseArray<T, TIndex> array)
		{
			var list = (IReadOnlyList<Storage<TIndex>>)this;
			var other = (IReadOnlyList<Storage<TIndex>>)array;
			if (!this.Storage.SameOriginAs(array.Storage))
				this.Storage.Dispose();
			for (int i = 0; i < list.Count; i++)
			{
				bool canDispose = true;
				for (int j = 0; j < other.Count; j++)
				{
					if (list[i].SameOriginAs(other[j]))
					{
						canDispose = false;
						break;
					}
				}
				if (canDispose)
					list[i].Dispose();
			}
		}
		#endregion

		#region IReadOnlyList
		int IReadOnlyCollection<Storage<TIndex>>.Count => this.m_indexArrays?.Length ?? 1;

		int IReadOnlyCollection<IStorage>.Count => this.m_indexArrays?.Length ?? 1;

		IStorage IReadOnlyList<IStorage>.this[int index] => ((IReadOnlyList<Storage<TIndex>>)this)[index];

		Storage<TIndex> IReadOnlyList<Storage<TIndex>>.this[int index] {
			get {
				if (index < 0)
					throw new ArgumentOutOfRangeException(nameof(index), Resources.Parameter.CannotNegative);
				if (index >= this.Count)
					throw new ArgumentOutOfRangeException(nameof(index), Resources.Parameter.InvalidValue);
				return this.m_indexArray ?? this.m_indexArrays?[index] ?? Storage<TIndex>.Empty; // cannot be empty
			}
		}

		IEnumerator<Storage<TIndex>> IEnumerable<Storage<TIndex>>.GetEnumerator()
		{
			var list = (IReadOnlyList<Storage<TIndex>>)this;
			for (int i = 0; i < list.Count; i++)
			{
				yield return list[i];
			}
		}

		IEnumerator<IStorage> IEnumerable<IStorage>.GetEnumerator() => ((IReadOnlyList<Storage<TIndex>>)this).GetEnumerator();
		#endregion
	}
}
