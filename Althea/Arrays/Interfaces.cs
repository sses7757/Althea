using System;
using System.Collections.Generic;

using Althea.Linq;
using Althea.NativeTypes;


namespace Althea.Arrays
{
	#region vector
	/// <summary>
	/// The interface of vector that contains the operation needed for Krylov-subspace methods such as Lanczos and Krylov-Schur solver.
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
	/// <typeparam name="TVec">The vector type</typeparam>
	public interface IKrylovVector<TVec, T> : IDisposable
		where TVec : class, IKrylovVector<TVec, T>, IDisposable, new()
		where T : unmanaged, IEquatable<T>
	{
		#region operation
		/// <summary>
		/// When implemented by a derived class, point-wisely in-place multiply this vector with given <paramref name="value"/>.
		/// </summary>
		/// <param name="value">The scalar as <typeparamref name="T"/> to multiply</param>
		void Scale(T value);

		/// <summary>
		/// When implemented by a derived class, compute the 2-norm (Euclidean norm) of elements in this vector.
		/// </summary>
		/// <returns>The 2-norm of this vector</returns>
		double Norm();

		/// <summary>
		/// When implemented by a derived class, in-place scale this vector such that its 2-norm (Euclidean norm) is one.
		/// </summary>
		/// <exception cref="DivideByZeroException">If the 2-norm of this array is 0</exception>
		void Normalize();

		/// <summary>
		/// When implemented by a derived class, compute dot (inner) product of this vector and <paramref name="other"/> vector. The conjugate of this vector shall be actually used.
		/// </summary>
		/// <param name="other">The other <typeparamref name="TVec"/> to perform the dot product</param>
		/// <returns>The dot (inner) product result as a <typeparamref name="T"/></returns>
		T Dot(TVec other);

		/// <summary>
		/// When implemented by a derived class, add the <paramref name="other"/> (scaling by <paramref name="scalar"/>) to this vector in-place.
		/// </summary>
		/// <param name="other">The other <typeparamref name="TVec"/> to add</param>
		/// <param name="scalar">The scalar to be multiplied to <paramref name="other"/> of type <typeparamref name="T"/></param>
		void AddByVector(TVec other, T scalar);

		/// <summary>
		/// When implemented by a derived class, replace this vector's content with the <paramref name="other"/> vector in-place.
		/// </summary>
		/// <param name="other">The other <typeparamref name="TVec"/> to replace from</param>
		/// <exception cref="InvalidOperationException">If the replacement cannot be done in-place due to reason(s) such as different sparsities between this and <paramref name="other"/></exception>
		void ReplaceBy(TVec other);

		/// <summary>
		/// When implemented by a derived class, multiply the matrix whose columns are indicated by <paramref name="unjoinedVectors"/> to a dense vector indicated by a <see cref="ReadOnlySpan{T}"/> and obtain the result vector as a <see cref="VectorBase{T}"/>.
		/// </summary>
		/// <param name="unjoinedVectors">The columns of the matrix to be multiplied</param>
		/// <param name="input">The input dense vector to be multiplied as a <see cref="ReadOnlySpan{T}"/></param>
		/// <returns>The product of <paramref name="unjoinedVectors"/> and <paramref name="input"/> as a <see cref="VectorBase{T}"/></returns>
		/// <remarks>The method shall be basically static, the information of this vector shall only be used to verify the consistency of <paramref name="unjoinedVectors"/></remarks>
		TVec OperateOn(IReadOnlyList<TVec> unjoinedVectors, ReadOnlySpan<T> input);
		#endregion
	}
	#endregion


	#region sparse arrays related
	/// <summary>
	/// Simple interface for sparse arrays, inherits <see cref="IReadOnlyList{T}"/> of <see cref="IStorage"/>. The index type is not indicated
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
	public interface ISparseArray<T> : IReadOnlyList<IStorage> where T : unmanaged
	{
		#region property
		/// <summary>
		/// When implemented by a derived class, get the value array storage of this sparse array.
		/// </summary>
		Storage<T> Storage { get; }

		/// <summary>
		/// When implemented by a derived class, get the number of non-default values (the values that are actually stored) of this sparse array. The default implementation returns the <see cref="Storage{T}.Length"/> of <see cref="Storage"/>
		/// </summary>
		long NStored => this.Storage.Length;

		/// <summary>
		/// When implemented by a derived class, get the (major) data type of the index array(s) of this sparse array as a <see cref="DataType"/>
		/// </summary>
		DataType IndexType { get; }

		/// <summary>
		/// When implemented by a derived class, get the default value of this sparse array
		/// </summary>
		T DefaultValue { get; protected internal set; }
		#endregion

		#region dispose
		/// <summary>
		/// When implemented by a derived class, dispose this sparse array after excluding the internal storages shared between this array and the target <paramref name="array"/>. The default implementation only compares <see cref="ISparseArray{T}.Storage"/> and the index array(s) implied in <see cref="ISparseArray{T}"/>.
		/// </summary>
		/// <param name="array">The target <see cref="ISparseArray{T}"/> to exclude before disposing</param>
		void DisposeExclude(ISparseArray<T> array)
		{
			var list = (IReadOnlyList<IStorage>)this;
			var other = (IReadOnlyList<IStorage>)array;
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

		#region data type cast helper
		/// <summary>
		/// The helper method used to create an storage casted from the value array of this sparse vector
		/// </summary>
		/// <typeparam name="TOut">The data type to cast to</typeparam>
		/// <param name="valueArray">A created storage casted from the value array of this sparse vector, or <see cref="ValueArray{T}.Storage"/> if <typeparamref name="T"/> == <typeparamref name="TOut"/></param>
		protected void DataTypeCast<TOut>(out Storage<TOut> valueArray) where TOut : unmanaged, IFormattable, IEquatable<TOut>
		{
			if (typeof(T) == typeof(TOut))
			{
				valueArray = this.Storage as Storage<TOut> ?? Storage<TOut>.Empty;
				return;
			}
			valueArray = this.Storage.CreateAlike<TOut>();
			try
			{
				LinearAlgebra.Dense.AbstractApi.PointWiseCast(this.Storage, 1, valueArray, 1);
			}
			catch (Exception)
			{
				valueArray?.Dispose();
				throw;
			}
		}
		#endregion
	}

	/// <summary>
	/// Simple interface for sparse arrays, inherits <see cref="IReadOnlyList{T}"/> of <see cref="Storage{T}"/> of <typeparamref name="TIndex"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
	/// <typeparam name="TIndex">Any integer-typed unmanaged struct as the index data type</typeparam>
	public interface ISparseArray<T, TIndex> : ISparseArray<T>, IReadOnlyList<Storage<TIndex>>
		where T : unmanaged, IEquatable<T>
		where TIndex : unmanaged
	{
		#region properties
		/// <summary>
		/// The <see cref="DataType"/> of the type parameter <typeparamref name="TIndex"/>
		/// </summary>
		protected static readonly DataType indexDataType = default(TIndex).ToDataType();

		DataType ISparseArray<T>.IndexType => indexDataType;
		#endregion

		#region dispose
		/// <summary>
		/// When implemented by a derived class, dispose this sparse array after excluding the internal storages shared between this array and the target <paramref name="array"/>. The default implementation only utilizes the default implementation of <see cref="ISparseArray{T}.DisposeExclude(ISparseArray{T})"/>.
		/// </summary>
		/// <param name="array">The target <see cref="ISparseArray{T, TIndex}"/> to exclude before disposing</param>
		void DisposeExclude(ISparseArray<T, TIndex> array) => ((ISparseArray<T>)this).DisposeExclude(array);
		#endregion

		#region helpers
		/// <summary>
		/// When implemented by a derived class, get the hash code this sparse array. The default implementation only takes <see cref="Storage"/> and the index array(s) into account.
		/// </summary>
		/// <returns>The hash code of <see cref="Storage"/> and the index array(s) of this sparse array</returns>
		int GetHashCode() => HashCode.Combine(this.Storage, ((IReadOnlyList<Storage<TIndex>>)this).HashCodeOfArray());

		/// <summary>
		/// When implemented by a derived class, check whether this sparse array is equal to another one. The default implementation only compares <see cref="Storage"/> and the index array(s) of this sparse array.
		/// </summary>
		/// <param name="obj">The other object to compare with</param>
		/// <returns>True if this == <paramref name="obj"/></returns>
		bool Equals(object? obj)
		{
			if (!(obj is ISparseArray<T, TIndex> sv && this.Storage == sv.Storage))
				return false;
			IReadOnlyList<Storage<TIndex>> list1 = this, list2 = sv;
			return list1.SequenceEqual(list2);
		}

		/// <summary>
		/// When implemented by a derived class, check if this sparse array share some storage(s) with the <paramref name="other"/> one. The default implementation only compares the <see cref="ValueArray{T}.Storage"/> and the index array(s).
		/// </summary>
		/// <param name="other">The other <see cref="ValueArray{T}"/> to check</param>
		/// <returns>True if they do share some storage, false otherwise</returns>
		bool OverlapWith(ISparseArray<T, TIndex> other)
		{
			IReadOnlyList<Storage<TIndex>> list = this, array = other;
			for (int i = 0; i < list.Count; i++)
			{
				for (int j = 0; j < array.Count; j++)
				{
					if (list[i].OverlapWith(array[j]))
					{
						return true;
					}
				}
			}
			return false;
		}

		/// <summary>
		/// The helper method used to create new array alike (and copy contents) the value array and index array(s) of this sparse array
		/// </summary>
		/// <typeparam name="TOut">Any unmanaged struct as the output type</typeparam>
		/// <param name="valueArray">The cloned output value array</param>
		/// <param name="indexArrays">The cloned output all index arrays, must has length equaling this.<see cref="IReadOnlyCollection{T}.Count">Count</see></param>
		/// <param name="copyContent">Copy the contents from original arrays to the new arrays or not</param>
		/// <exception cref="ArgumentNullException">If <paramref name="copyContent"/> is empty</exception>
		/// <exception cref="ArgumentException">If <paramref name="copyContent"/> has incompatible length</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <typeparamref name="T"/> is not <typeparamref name="TOut"/> while <paramref name="copyContent"/> is true</exception>
		protected void NewArraysAlike<TOut>(out ActualStorage<TOut> valueArray, Span<IntPtr> indexArrays, bool copyContent) where TOut : unmanaged
		{
			if (indexArrays.IsEmpty)
				throw new ArgumentNullException(nameof(indexArrays));
			if (typeof(T) != typeof(TOut) && copyContent)
				throw new ArgumentOutOfRangeException(nameof(copyContent), Resources.Parameter.InvalidValue);
			IReadOnlyList<Storage<TIndex>> list = this;
			if (indexArrays.Length != list.Count)
				throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(indexArrays));

			ActualStorage<TOut>? value = null;
			Span<ActualStorage<TIndex>> indices = indexArrays.AsReferenceType<ActualStorage<TIndex>>();
			try
			{
				value = copyContent ? (this.Storage.Clone() as ActualStorage<TOut> ?? ActualStorage<TOut>.Empty) : this.Storage.CreateAlike<TOut>();
				for (int i = 0; i < list.Count; i++)
				{
					indices[i] = copyContent ? list[i].Clone() : list[i].CreateAlike();
				}
				valueArray = value;
			}
			catch (Exception)
			{
				value?.Dispose();
				for (int i = 0; i < indices.Length; i++)
				{
					indices[i]?.Dispose();
				}
				throw;
			}
		}
		#endregion
	}

	/// <summary>
	/// The interface for sparse vectors without indicating the index data type. The value array is <see cref="ISparseArray{T}.Storage"/> and index array(s) is/are the inherited <see cref="IReadOnlyList{T}"/> of <see cref="IStorage"/>s.
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
	public interface ISparseVector<T> : ISparseArray<T> where T : unmanaged
	{
		#region property
		/// <summary>
		/// When implemented by a derived class, get the presenting length of this sparse vector
		/// </summary>
		long Length { get; }

		/// <summary>
		/// When implemented by a derived class, get the format of this sparse vector as a <see cref="LinearAlgebra.Sparse.SparseVectorFormat"/>
		/// </summary>
		LinearAlgebra.Sparse.SparseVectorFormat Format { get; }
		#endregion

		#region conversion
		/// <summary>
		/// Convert this sparse vector to a dense vector whose <see cref="Storage{T}"/> is <paramref name="denseStorage"/>
		/// </summary>
		/// <param name="denseStorage">The <see cref="Storage{T}"/> of the dense vector to overwrite</param>
		void ToDense(Storage<T> denseStorage);
		#endregion
	}

	/// <summary>
	/// The interface for sparse matrices without indicating the index data type. The value array is <see cref="ISparseArray{T}.Storage"/> and index array(s) is/are the inherited <see cref="IReadOnlyList{T}"/> of <see cref="IStorage"/>s.
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
	public interface ISparseMatrix<T> : ISparseArray<T> where T : unmanaged
	{
		#region property
		/// <summary>
		/// When implemented by a derived class, get the presenting number of rows of this sparse matrix
		/// </summary>
		long NRows { get; }

		/// <summary>
		/// When implemented by a derived class, get the presenting number of columns of this sparse matrix
		/// </summary>
		long NCols { get; }

		/// <summary>
		/// When implemented by a derived class, get the format of this sparse matrix as a <see cref="LinearAlgebra.Sparse.SparseMatrixFormat"/>
		/// </summary>
		LinearAlgebra.Sparse.SparseMatrixFormat Format { get; }
		#endregion

		#region conversion
		/// <summary>
		/// When implemented by a derived class, convert this sparse matrix to a dense matrix whose <see cref="Storage{T}"/> is <paramref name="denseStorage"/>
		/// </summary>
		/// <param name="denseStorage">The <see cref="Storage{T}"/> of the dense matrix to overwrite</param>
		/// <param name="leadDim">The leading dimension of the target dense matrix</param>
		/// <param name="rows">The number of rows of the dense matrix</param>
		/// <param name="cols">The number of columns of the dense matrix</param>
		/// <exception cref="ArgumentNullException">If <paramref name="denseStorage"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="rows"/> or <paramref name="cols"/> or <paramref name="leadDim"/> is less than 1</exception>
		/// <exception cref="ArgumentException">If <paramref name="rows"/> &gt; <paramref name="leadDim"/> or <paramref name="leadDim"/> * <paramref name="cols"/> &gt; <paramref name="denseStorage"/>.<see cref="Storage{T}.Length">Length</see></exception>
		void ToDense(Storage<T> denseStorage, long leadDim, long rows, long cols);
		#endregion
	}
	#endregion


	#region tensor
	/// <summary>
	/// The interface for tensor that contains basic members (size and label).
	/// </summary>
	public interface ITensor
	{
		#region properties
		/// <summary>
		/// When implemented by a derived class, get the rank of this tensor
		/// </summary>
		int Rank { get; }

		/// <summary>
		/// When implemented by a derived class, get or set the label array as a <see cref="ReadOnlySpan{T}"/> of <see cref="char"/> used to mark each index of this tensor
		/// </summary>
		/// <exception cref="ArgumentException">If the setting value's length is not the same as the <see cref="Rank"/></exception>
		ReadOnlySpan<char> Label { get; set; }
		#endregion

		#region method
		/// <summary>
		/// When implemented by a derived class, get the label at rank <paramref name="index"/>
		/// </summary>
		/// <param name="index">The index of the rank whose label will be obtained</param>
		/// <returns>The <see cref="char"/> label at <paramref name="index"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="index"/> is out of range of <see cref="Rank"/></exception>
		char GetLabel(int index);

		/// <summary>
		/// When implemented by a derived class, set the label at rank <paramref name="index"/> to <paramref name="value"/>
		/// </summary>
		/// <param name="index">The index of the rank whose label will be set</param>
		/// <param name="value">The <see cref="char"/> label at <paramref name="index"/> to set</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="index"/> is out of range of <see cref="Rank"/></exception>
		void SetLabel(int index, char value);

		/// <summary>
		/// When implemented by a derived class, set the label(s) used to mark each index of this tensor
		/// </summary>
		/// <param name="labels">The label(s) to set as an array of <see cref="char"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="labels"/> is null or empty</exception>
		/// <exception cref="ArgumentException">If the length of <paramref name="labels"/> is not the same as the <see cref="Rank"/></exception>
		void SetLabels(params char[] labels);
		#endregion
	}
	#endregion
}
