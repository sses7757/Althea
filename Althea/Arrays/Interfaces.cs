using System;
using System.Collections.Generic;

using Althea.Linq;
using Althea.Helpers;
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
		where TVec : class, IKrylovVector<TVec, T>, IDisposable, ICheckValid, new()
		where T : unmanaged
	{
		#region operation
		/// <summary>
		/// The total presenting length of this vector
		/// </summary>
		long Length { get; }

		/// <summary>
		/// Create a new vector alike this one
		/// </summary>
		/// <returns>The new vector alike this one</returns>
		TVec NewArrayAlike();

		/// <summary>
		/// Fill this vector with the given <paramref name="value"/>
		/// </summary>
		/// <param name="value">The value to fill</param>
		void FillWith(T value);

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
		/// <exception cref="ArgumentNullException">If <paramref name="other"/> is null or invalid</exception>
		T Dot(TVec other);

		/// <summary>
		/// When implemented by a derived class, add the <paramref name="other"/> (scaling by <paramref name="scalar"/>) to this vector in-place.
		/// </summary>
		/// <param name="other">The other <typeparamref name="TVec"/> to add</param>
		/// <param name="scalar">The scalar to be multiplied to <paramref name="other"/> of type <typeparamref name="T"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="other"/> is null or invalid</exception>
		void AddBy(TVec other, T scalar);

		/// <summary>
		/// When implemented by a derived class, replace this vector's content with the <paramref name="other"/> vector in-place.
		/// </summary>
		/// <param name="other">The other <typeparamref name="TVec"/> to replace from</param>
		/// <exception cref="ArgumentNullException">If <paramref name="other"/> is null or invalid</exception>
		/// <exception cref="InvalidOperationException">If the replacement cannot be done in-place due to reason(s) such as different sparsities between this and <paramref name="other"/></exception>
		void ReplaceBy(TVec other);

		/// <summary>
		/// When implemented by a derived class, multiply the matrix whose columns are indicated by <paramref name="unjoinedVectors"/> to a dense vector indicated by a <see cref="ReadOnlySpan{T}"/> and obtain the result vector as a <typeparamref name="TVec"/>.
		/// </summary>
		/// <param name="unjoinedVectors">The columns of the matrix to be multiplied</param>
		/// <param name="input">The input dense vector to be multiplied as a <see cref="ReadOnlySpan{T}"/></param>
		/// <returns>The product of <paramref name="unjoinedVectors"/> and <paramref name="input"/> as a <typeparamref name="TVec"/></returns>
		/// <remarks>The method shall be basically static, the information of this vector shall only be used to verify the consistency of <paramref name="unjoinedVectors"/></remarks>
		/// <exception cref="ArgumentNullException">If any of <paramref name="unjoinedVectors"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="input"/> and <paramref name="unjoinedVectors"/> have different size, or any element of <paramref name="unjoinedVectors"/> has different size than this vector</exception>
		TVec OperateOn(IReadOnlyList<TVec> unjoinedVectors, ReadOnlySpan<T> input)
		{
			if (unjoinedVectors is null || unjoinedVectors.Count == 0)
				throw new ArgumentNullException(nameof(unjoinedVectors));
			if (input.IsEmpty)
				throw new ArgumentNullException(nameof(input));
			if (unjoinedVectors.Count != input.Length)
				throw new ArgumentException(Resources.Parameter.NotSameSize);

			// sort first to reduce errors
			int length = input.Length;
			Span<T> values = length.CheckStackLimit<T>() ?? stackalloc T[length];
			Span<double> keys = length.CheckStackLimit<double>() ?? stackalloc double[length];
			for (int i = 0; i < length; i++)
			{
				values[i] = input[i];
				keys[i] = input[i].GenericAbsolute();
			}
			keys.Sort(values);

			var vec = this.NewArrayAlike();
			try
			{
				vec.FillWith(default);
				for (int i = 0; i < length; i++)
				{
					var dnvec = unjoinedVectors[i];
					if (dnvec is null || !dnvec.IsValid())
						throw new ArgumentNullException(nameof(unjoinedVectors));
					if (dnvec.Length != this.Length)
						throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(unjoinedVectors));
					if (!values[i].IsZero())
						vec.AddBy(dnvec, values[i]);
				}
				return vec;
			}
			catch (Exception)
			{
				vec.Dispose();
				throw;
			}
		}
		#endregion
	}
	#endregion


	#region pitched (strided) array
	/// <summary>
	/// The interface of dense array that may exist extra pitch at each dimension and thus the strides are not simply the accumulated product of its size.
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
	public interface IPitchedArray<T> where T : unmanaged
	{
		#region properties
		/// <summary>
		/// When implemented by a derived class, get the (referenced) storage of this dense pitched/strided array.
		/// </summary>
		Storage<T> Storage { get; }

		/// <summary>
		/// When implemented by a derived class, get the size (in <typeparamref name="T"/>) of this array (the extent at each dimension) as a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/>, must be of positive numbers.
		/// </summary>
		ReadOnlySpan<long> Size { get; }

		/// <summary>
		/// When implemented by a derived class, get the pitch (in <typeparamref name="T"/>) of this array (the outer size at each dimension) as a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/>. It must has length equals to <see cref="Size"/> and consists numbers larger than or equals to <see cref="Size"/> respectively.
		/// </summary>
		ReadOnlySpan<long> OuterSize { get; }
		#endregion

		#region method
		/// <summary>
		/// Get the strides of this array at each dimension and fill the result in the <paramref name="output"/>
		/// </summary>
		/// <param name="output">The <see cref="Span{T}"/> of length equals to <see cref="Size"/></param>
		/// <returns>The <paramref name="output"/></returns>
		/// <remarks>At exit, <paramref name="output"/>[i] denotes the displacement (stride) between two consecutive elements in the i<sup>th</sup>-mode.</remarks>
		Span<long> GetStrides(Span<long> output)
		{
			if (output.Length != this.Size.Length)
				throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(output));
			this.OuterSize.AccumulateProd(output, inclusive: true);
			return output;
		}
		#endregion
	}
	#endregion


	#region sparse arrays related
	/// <summary>
	/// Simple interface for sparse arrays, inherits <see cref="IReadOnlyList{T}"/> of <see cref="IStorage"/>. The index type is not indicated
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
	/// <remarks>The <see cref="ISparseArray{T}.Storage"/> and the <see cref="IReadOnlyList{T}.this[int]"/> shall all returns <b>referenced</b> storages.</remarks>
	public interface ISparseArray<T> : IReadOnlyList<IStorage>, ICheckValid where T : unmanaged
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

		#region helper
		bool ICheckValid.IsValid()
		{
			if (this.Storage is null || !this.Storage.IsValid() || this.NStored <= 0)
				return false;
			IReadOnlyList<IStorage> list = this;
			return list.All(static l => l is not null && l.IsValid());
		}
		#endregion
	}

	/// <summary>
	/// Simple interface for sparse arrays, inherits <see cref="IReadOnlyList{T}"/> of <see cref="Storage{T}"/> of <typeparamref name="TIndex"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
	/// <typeparam name="TIndex">Any integer-typed unmanaged struct as the index data type</typeparam>
	public interface ISparseArray<T, TIndex> : ISparseArray<T>, IReadOnlyList<Storage<TIndex>>
		where T : unmanaged
		where TIndex : unmanaged
	{
		#region properties
		DataType ISparseArray<T>.IndexType => Const<TIndex>.DataType;
		#endregion

		#region helpers
		/// <summary>
		/// When implemented by a derived class, get the hash code this sparse array. The default implementation only takes <see cref="Storage"/> and the index array(s) into account.
		/// </summary>
		/// <returns>The hash code of <see cref="Storage"/> and the index array(s) of this sparse array</returns>
		int GetHashCode() => HashCode.Combine(this.Storage.MakeReference(newLength: this.NStored), ((IReadOnlyList<Storage<TIndex>>)this).HashCodeOfArray());

		/// <summary>
		/// When implemented by a derived class, check whether this sparse array is equal to another one. The default implementation only compares <see cref="Storage"/> and the index array(s) of this sparse array.
		/// </summary>
		/// <param name="obj">The other object to compare with</param>
		/// <returns>True if this == <paramref name="obj"/></returns>
		bool Equals(object? obj)
		{
			if (!(obj is ISparseArray<T, TIndex> sv && this.NStored == sv.NStored && this.Storage.MakeReference(newLength: this.NStored) == sv.Storage.MakeReference(newLength: this.NStored)))
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
			if (this.Storage.OverlapWith(other.Storage))
				return true;
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
		/// <typeparam name="TIndexOut">Any integral-typed unmanaged struct as the output type</typeparam>
		/// <param name="valueArray">The cloned output value array</param>
		/// <param name="copyValues">Copy the value array from original arrays to the new arrays or not</param>
		/// <returns>The output index arrays as a <see cref="SizedFixedClassBuffer_8{T}"/> of <see cref="ActualStorage{T}"/> of <typeparamref name="TIndexOut"/></returns>
		/// <exception cref="TypeMismatchException">If <typeparamref name="TIndexOut"/> is not an integral type</exception>
		SizedFixedClassBuffer_8<ActualStorage<TIndexOut>> NewArraysAlike<TOut, TIndexOut>(out ActualStorage<TOut> valueArray, bool copyValues)
			where TOut : unmanaged where TIndexOut : unmanaged
		{
			if (!Const<TIndexOut>.IsIntegralType)
				throw new TypeMismatchException(typeof(TIndexOut), TypeMismatchException.MismatchReason.NotInteger);

			IReadOnlyList<Storage<TIndex>> list = this;
			ActualStorage<TOut>? value = null;
			SizedFixedClassBuffer_8<ActualStorage<TIndexOut>> indices = new(list.Count);
			try
			{
				value = this.Storage.CreateAlike<TOut>();
				if (copyValues)
					LinearAlgebra.Dense.AbstractApi.PointWiseCast(this.Storage, 1, value, 1);
				for (int i = 0; i < list.Count; i++)
				{
					indices[i] = list[i].CreateAlike<TIndexOut>();
					LinearAlgebra.Dense.AbstractApi.PointWiseCast(list[i], 1, indices[i], 1);
				}
				valueArray = value;
				return indices;
			}
			catch (Exception)
			{
				value?.Dispose();
				for (int i = 0; i < indices.Count; i++)
				{
					indices[i]?.Dispose();
				}
				throw;
			}
		}

		/// <summary>
		/// The helper method used to create new array alike (and copy contents) the value array and index array(s) of this sparse array
		/// </summary>
		/// <typeparam name="TOut">Any unmanaged struct as the output type</typeparam>
		/// <typeparam name="TIndexOut">Any integral-typed unmanaged struct as the output type</typeparam>
		/// <param name="target">The target <see cref="ISparseArray{T, TIndex}"/> of (<typeparamref name="TOut"/>, <typeparamref name="TIndexOut"/>) to cast to</param>
		void TypeCast<TOut, TIndexOut>(ISparseArray<TOut, TIndexOut> target)
			where TOut : unmanaged where TIndexOut : unmanaged
		{
			IReadOnlyList<Storage<TIndex>> list = this;
			IReadOnlyList<Storage<TIndexOut>> other = target;
			LinearAlgebra.Dense.AbstractApi.PointWiseCast(this.Storage, 1, target.Storage, 1);
			for (int i = 0; i < list.Count; i++)
			{
				LinearAlgebra.Dense.AbstractApi.PointWiseCast(list[i], 1, other[i], 1);
			}
		}
		#endregion

		#region clone related
		/// <summary>
		/// When implemented by a derived class, create a new sparse array with same properties as this one while the underlying storages are not filled and the data type is changed to <typeparamref name="TOut"/> while index type changed to <typeparamref name="TIndexOut"/>.
		/// </summary>
		/// <typeparam name="TOut">Any unmanaged struct as the new data type</typeparam>
		/// <typeparam name="TIndexOut">Any integral-typed unmanaged struct as the new index type</typeparam>
		/// <returns>The new sparse array of type (<typeparamref name="TOut"/>, <typeparamref name="TIndexOut"/>) alike this one</returns>
		/// <exception cref="TypeMismatchException">If the <typeparamref name="TIndexOut"/> is not an integral type</exception>
		ISparseArray<TOut, TIndexOut> NewArrayAlike<TOut, TIndexOut>()
			where TOut : unmanaged, IFormattable, IEquatable<TOut>
			where TIndexOut : unmanaged, IEquatable<TIndexOut>;

		/// <summary>
		/// When implemented by a derived class, cast this array into another data type <typeparamref name="TOut"/>. The default implementation only casts the <see cref="Storage"/> of this array.
		/// </summary>
		/// <typeparam name="TOut">Any unmanaged struct as the new data type</typeparam>
		/// <typeparam name="TIndexOut">Any integral-typed unmanaged struct as the new index type</typeparam>
		/// <returns>The new <see cref="ISparseArray{T, TIndex}"/> of (<typeparamref name="TOut"/>, <typeparamref name="TIndexOut"/>) casted from this array or this array if <typeparamref name="TOut"/> == <typeparamref name="T"/> and <typeparamref name="TIndexOut"/> == <typeparamref name="TIndex"/></returns>
		ISparseArray<TOut, TIndexOut> DataTypeCast<TOut, TIndexOut>()
			where TOut : unmanaged, IFormattable, IEquatable<TOut>
			where TIndexOut : unmanaged, IEquatable<TIndexOut>;
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
		/// <param name="leadDim">The leading dimension of the target dense matrix, default 0 means <see cref="NRows"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="denseStorage"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="leadDim"/> is less than <see cref="NRows"/></exception>
		/// <exception cref="ArgumentException">If <paramref name="leadDim"/> * <see cref="NCols"/> &gt; <paramref name="denseStorage"/>.<see cref="Storage{T}.Length">Length</see></exception>
		void ToDense(Storage<T> denseStorage, long leadDim = 0);
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
