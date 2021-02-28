using System;
using System.Collections.Generic;

using Althea.Linq;
using Althea.NativeTypes;
using Althea.LinearAlgebra.Sparse;

using MEM = Althea.Storage.AbstractApi;
using LAD = Althea.LinearAlgebra.Dense.AbstractApi;


namespace Althea.Arrays
{
	/// <summary>
	/// The abstract sparse matrix class with the only mutable <see cref="ValueArray{T}.Storage"/> that refers to the value array storage.
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct that implements <see cref="IFormattable"/> and <see cref="IEquatable{T}"/> as the data type</typeparam>
	/// <typeparam name="TInd">Any integer-typed unmanaged struct as the index type</typeparam>
	public abstract class AbstractSparseMatrix<T, TInd> : MatrixBase<T>, ISparseMatrix<T>, ISparseArray<T, TInd>
		where T : unmanaged, IFormattable, IEquatable<T>
		where TInd : unmanaged
	{
		#region basic
		/// <summary>
		/// When implemented by a derived class, get the number of stored values of this sparse matrix. The default implementation returns the <see cref="ValueArray{T}.ActualLength"/>.
		/// </summary>
		public virtual long NStored => this.ActualLength;

		/// <summary>
		/// Get the sparse format of this sparse matrix as a <see cref="SparseMatrixFormat"/>
		/// </summary>
		public SparseMatrixFormat Format { get; }

		/// <summary>
		/// Get or set the default value (the value not specified) of this sparse matrix
		/// </summary>
		public T DefaultValue { get; protected internal set; }

		T ISparseArray<T>.DefaultValue { get => this.DefaultValue; set => this.DefaultValue = value; }

		/// <summary>
		/// The member of row index array as a <see cref="Storage{T}"/> of <typeparamref name="TInd"/>
		/// </summary>
		protected readonly Storage<TInd> m_rowIndexArray;
		/// <summary>
		/// The member of column index array as a <see cref="Storage{T}"/> of <typeparamref name="TInd"/>
		/// </summary>
		protected readonly Storage<TInd> m_colIndexArray;

		/// <summary>
		/// The member of all the index arrays as an array of <see cref="Storage{T}"/> of <typeparamref name="TInd"/>, is null if the sparsity is indicated by <see cref="m_rowIndexArray"/> and <see cref="m_colIndexArray"/>
		/// </summary>
		protected readonly Storage<TInd>[]? m_indexArrays = null;

		/// <summary>
		/// Create a <see cref="AbstractSparseMatrix{T, TInd}"/> with given <paramref name="rows"/>, <paramref name="cols"/>, <paramref name="valueArray"/>, <paramref name="rowIndexArray"/> and <paramref name="colIndexArray"/>
		/// </summary>
		/// <param name="rows">The presenting number of rows of this sparse matrix</param>
		/// <param name="cols">The presenting number of columns of this sparse matrix</param>
		/// <param name="valueArray">The value array as a <see cref="Storage{T}"/> of <typeparamref name="T"/></param>
		/// <param name="rowIndexArray">The row index array as a <see cref="Storage{T}"/> of <typeparamref name="TInd"/></param>
		/// <param name="colIndexArray">The column index array as a <see cref="Storage{T}"/> of <typeparamref name="TInd"/></param>
		/// <param name="format">The <see cref="SparseMatrixFormat"/> of this sparse matrix, must be atomic</param>
		/// <param name="defaultValue">The default value (the value not specified) of this sparse matrix</param>
		/// <exception cref="NotSupportedException">If the <typeparamref name="TInd"/> is not an real integral type</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="valueArray"/> or <paramref name="rowIndexArray"/> or <paramref name="colIndexArray"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="format"/> is not atomic</exception>
		protected AbstractSparseMatrix(long rows, long cols, Storage<T> valueArray, Storage<TInd> rowIndexArray, Storage<TInd> colIndexArray, SparseMatrixFormat format, T defaultValue = default) : base(valueArray, rows, cols)
		{
			var type = default(TInd).GetClassification();
			if (type.IsComplex() || (type != DataTypeClassification.SignedInteger && type != DataTypeClassification.UnsignedInteger))
				throw new NotSupportedException(Resources.Support.DataType);
			if (!format.IsAtomic())
				throw new ArgumentOutOfRangeException(nameof(format), format, Resources.Parameter.InvalidValue);
			if (rowIndexArray is null || !rowIndexArray.IsValid())
				throw new ArgumentNullException(nameof(rowIndexArray));
			if (colIndexArray is null || !colIndexArray.IsValid())
				throw new ArgumentNullException(nameof(colIndexArray));

			this.m_rowIndexArray = rowIndexArray; this.m_colIndexArray = colIndexArray ; this.m_indexArrays = null;
			this.Format = format; this.DefaultValue = defaultValue;
		}

		/// <summary>
		/// Create a <see cref="AbstractSparseMatrix{T, TInd}"/> with given <paramref name="rows"/>, <paramref name="cols"/>, <paramref name="valueArray"/> and <paramref name="indexArrays"/>
		/// </summary>
		/// <param name="rows">The presenting number of rows of this sparse matrix</param>
		/// <param name="cols">The presenting number of columns of this sparse matrix</param>
		/// <param name="valueArray">The value array as a <see cref="Storage{T}"/> of <typeparamref name="T"/></param>
		/// <param name="indexArrays">The index arrays as a list of <see cref="Storage{T}"/> of <typeparamref name="TInd"/></param>
		/// <param name="format">The <see cref="SparseMatrixFormat"/> of this sparse matrix, must be atomic</param>
		/// <param name="defaultValue">The default value (the value not specified) of this sparse matrix</param>
		/// <exception cref="ArgumentNullException">If <paramref name="indexArrays"/> is null or empty</exception>
		/// <exception cref="NotSupportedException">If the <typeparamref name="TInd"/> is not an integral type</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="format"/> is not atomic</exception>
		/// <exception cref="ArgumentException">If the length of <paramref name="indexArrays"/> is less than 2</exception>
		protected AbstractSparseMatrix(long rows, long cols, Storage<T> valueArray, Storage<TInd>[] indexArrays, SparseMatrixFormat format, T defaultValue = default) : base(valueArray, rows, cols)
		{
			if (indexArrays is null || indexArrays.Length == 0)
				throw new ArgumentNullException(nameof(indexArrays));
			var type = default(TInd).GetClassification();
			if (type != DataTypeClassification.SignedInteger && type != DataTypeClassification.UnsignedInteger)
				throw new NotSupportedException(Resources.Support.DataType);
			if (!format.IsAtomic())
				throw new ArgumentOutOfRangeException(nameof(format), format, Resources.Parameter.InvalidValue);
			if (indexArrays.Any(static a => a is null || !a.IsValid()))
				throw new ArgumentNullException(nameof(indexArrays));
			if (indexArrays.Length < 2)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(indexArrays));

			this.m_rowIndexArray = indexArrays[0]; this.m_colIndexArray = indexArrays[1]; this.m_indexArrays = (Storage<TInd>[])indexArrays.Clone();
			this.Format = format; this.DefaultValue = defaultValue;
		}
		#endregion

		#region storage related
		/// <summary>
		/// When implemented by a derived class, check whether this sparse matrix is a valid one or not. The default implementation only checks <see cref="AbstractArray{T}.Length"/>, <see cref="NStored"/>, <see cref="ValueArray{T}.Storage"/> and the underlying index array(s) of this sparse matrix.
		/// </summary>
		/// <returns>The validness of this array</returns>
		public override bool IsValid()
		{
			if (!base.IsValid() || this.NStored <= 0)
				return false;
			if (this.m_indexArrays is null)
			{
				return	this.m_rowIndexArray is not null && this.m_rowIndexArray.IsValid() &&
						this.m_colIndexArray is not null && this.m_colIndexArray.IsValid();
			}
			else
			{
				return this.m_indexArrays.All(static a => a is not null && a.IsValid());
			}
		}

		/// <summary>
		/// When implemented by a derived class, check if this sparse matrix share some storage(s) with the <paramref name="other"/> one. The default implementation only utilizes the default implementation of <see cref="ISparseArray{T, TIndex}.OverlapWith(ISparseArray{T, TIndex})"/>
		/// </summary>
		/// <param name="other">The other <see cref="ValueArray{T}"/> to check</param>
		/// <returns>True if they do share some storage, false otherwise</returns>
		public override bool OverlapWith(ValueArray<T> other)
		{
			if (base.OverlapWith(other))
				return true;
			if (other is not ISparseArray<T, TInd> sparse)
				return false;
			// else
			return ((ISparseArray<T, TInd>)this).OverlapWith(sparse);
		}

		/// <summary>
		/// When implemented by a derived class, dispose this sparse matrix after excluding the internal storages shared between this array and the target <paramref name="array"/>. The default implementation only utilizes the default implementation of <see cref="ISparseArray{T}.DisposeExclude(ISparseArray{T})"/>.
		/// </summary>
		/// <param name="array">The target <see cref="ISparseArray{T}"/> to exclude before disposing this sparse matrix</param>
		public virtual void DisposeExclude(ISparseArray<T> array) => ((ISparseArray<T>)this).DisposeExclude(array);

		/// <summary>
		/// When implemented by a derived class, actually the dispose this array. The default implementation only disposes <see cref="ValueArray{T}.Storage"/> and the index array(s) passed to the constructor of <see cref="AbstractSparseVector{T, TInd}"/>.
		/// </summary>
		/// <param name="disposing">Dispose managed resources or not</param>
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			this.m_rowIndexArray?.Dispose();
			this.m_colIndexArray?.Dispose();
			if (this.m_indexArrays is not null)
			{
				for (int i = 0; i < this.m_indexArrays.Length; i++)
				{
					this.m_indexArrays[i]?.Dispose();
				}
			}
		}
		#endregion

		#region IReadOnlyList of ISparseArray
		int IReadOnlyCollection<Storage<TInd>>.Count => this.m_indexArrays?.Length ?? 2;

		int IReadOnlyCollection<IStorage>.Count => this.m_indexArrays?.Length ?? 2;

		IStorage IReadOnlyList<IStorage>.this[int index] => ((IReadOnlyList<Storage<TInd>>)this)[index];

		Storage<TInd> IReadOnlyList<Storage<TInd>>.this[int index] {
			get {
				if (index < 0)
					throw new ArgumentOutOfRangeException(nameof(index), index, Resources.Parameter.CannotNegative);
				if (index >= ((IReadOnlyCollection<IStorage>)this).Count)
					throw new ArgumentOutOfRangeException(nameof(index), index, Resources.Parameter.InvalidValue);

				if (this.m_indexArrays is null)
					return index == 0 ? this.m_rowIndexArray : this.m_colIndexArray;
				else
					return this.m_indexArrays[index];
			}
		}

		IEnumerator<Storage<TInd>> IEnumerable<Storage<TInd>>.GetEnumerator()
		{
			var list = (IReadOnlyList<Storage<TInd>>)this;
			for (int i = 0; i < list.Count; i++)
			{
				yield return list[i];
			}
		}

		IEnumerator<IStorage> IEnumerable<IStorage>.GetEnumerator() => ((IReadOnlyList<Storage<TInd>>)this).GetEnumerator();
		#endregion

		#region clone related
		/// <summary>
		/// When implemented by a derived class, convert this sparse matrix to a dense matrix whose <see cref="Storage{T}"/> is <paramref name="matrix"/>
		/// </summary>
		/// <param name="matrix">The <see cref="MatrixBase{T}"/> as the dense matrix to overwrite</param>
		/// <exception cref="ArgumentNullException">If <paramref name="matrix"/> is null or has length less than <see cref="AbstractArray{T}.Length"/> of this</exception>
		/// <exception cref="ArgumentException">If <paramref name="matrix"/> is a sparse matrix</exception>
		public abstract void ToDense(MatrixBase<T> matrix);

		/// <summary>
		/// When implemented by a derived class, deep clone the sparse matrix, the mutable status will not be copied.
		/// </summary>
		/// <returns>The cloned sparse matrix</returns>
		public override abstract AbstractSparseMatrix<T, TInd> Clone();

		/// <summary>
		/// When implemented by a derived class, create a new sparse matrix with same properties as this one while the underlying storages are not filled.
		/// </summary>
		/// <returns>The new sparse matrix alike this one</returns>
		public override abstract AbstractSparseMatrix<T, TInd> NewArrayAlike();

		/// <summary>
		/// When implemented by a derived class, create a new sparse matrix with same properties as this one while the underlying storages are not filled and the data type is changed to <typeparamref name="TOut"/>.
		/// </summary>
		/// <typeparam name="TOut">Any unmanaged struct as the new data type</typeparam>
		/// <returns>The new sparse matrix alike this one</returns>
		public override abstract AbstractSparseMatrix<TOut, TInd> NewArrayAlike<TOut>();

		/// <summary>
		/// When implemented by a derived class, cast this array into another data type <typeparamref name="TOut"/>. The default implementation only casts the <see cref="Storage"/> of this array.
		/// </summary>
		/// <typeparam name="TOut">The data type to cast to</typeparam>
		/// <returns>The new <see cref="ValueArray{TOut}"/> casted from this array or this array if <typeparamref name="TOut"/> == <typeparamref name="T"/></returns>
		public override abstract AbstractSparseMatrix<TOut, TInd> DataTypeCast<TOut>();
		#endregion

	}
}
