using System;
using System.Runtime.CompilerServices;

using Althea.Linq;
using Althea.NativeTypes;
using Althea.Storage;


namespace Althea.Arrays
{
	#region pitched (strided) array
	/// <summary>
	/// The interface of (column-major) dense array that may exist extra pitch at each dimension and thus the strides are not simply the accumulated product of its size.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	public interface IPitchedArray<T> where T : unmanaged, INumber<T>
	{
		#region properties
		/// <summary>
		/// When implemented by a derived class, get the size (in <typeparamref name="T"/>) of this array (the extent at each dimension) as a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/>, must be of positive numbers.
		/// </summary>
		ReadOnlySpan<long> Size { get; }

		/// <summary>
		/// When implemented by a derived class, get the pitch (in <typeparamref name="T"/>) of this array (the outer size at each dimension) as a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/>. It must has length equals to <see cref="Size"/> and consists numbers larger than or equals to <see cref="Size"/> respectively.
		/// </summary>
		ReadOnlySpan<long> OuterSize { get; }

		/// <summary>
		/// When implemented by a derived class, check whether this array is actually pitched. The default implementation simply checks the point-wise equality of <see cref="Size"/> and <see cref="OuterSize"/>.
		/// </summary>
		bool HasPitch => !this.OuterSize.SequenceEqual(this.Size);

		/// <summary>
		/// When implemented by a derived class, get (the both-end inclusive accumulated product of <see cref="OuterSize"/>) of this tensor at all dimensions as a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/>.
		/// </summary>
		/// <remarks>The first element shall be 1, the last element shall be the product of <see cref="OuterSize"/> and the <see cref="ReadOnlySpan{T}.Length">size</see> == rank + 1</remarks>
		ReadOnlySpan<long> Strides { get; }
		#endregion
	}
	#endregion


	#region sparse arrays related
	/// <summary>
	/// Simple interface for sparse arrays where the index type is not indicated
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	public interface ISparseArray<T> : ICheckValid, IDisposable where T : unmanaged, INumber<T>
	{
		#region property
		/// <summary>
		/// When implemented by a derived class, get the number of non-default values (the values that are actually stored) of this sparse array.
		/// </summary>
		long NStored { get; }

		/// <summary>
		/// When implemented by a derived class, statically get the data type of the <paramref name="n"/>-th index array of this sparse array as a <see cref="DataType"/>.
		/// </summary>
		/// <param name="n">The index of the index array</param>
		/// <returns>The <see cref="DataType"/> of  the <paramref name="n"/>-th index array.</returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="n"/> is out of range</exception>
		abstract static DataType IndexType(int n);

		/// <summary>
		/// When implemented by a derived class, statically get the default value of this sparse array
		/// </summary>
		abstract static T DefaultValue { get; }
		#endregion

		#region dispose
		/// <summary>
		/// When implemented by a derived class, get the original value array's storage of this sparse array. This property is only used for disposition.
		/// </summary>
		protected IStorage ValueStorage { get; }

		/// <summary>
		/// When implemented by a derived class, get the original index array(s)' storage(s) of this sparse array. This property is only used for disposition.
		/// </summary>
		protected ReadOnlySpan<IStorage> IndexStorages { get; }

		/// <summary>
		/// When implemented by a derived class, actually dispose this sparse array's index storages. The default implementation disposes <see cref="ISparseArray{T}.IndexStorages"/>.
		/// </summary>
		void IDisposable.Dispose()
		{
			this.ValueStorage?.Dispose();
			var list = this.IndexStorages;
			for (int i = 0; i < list.Length; i++)
			{
				list[i]?.Dispose();
			}
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// When implemented by a derived class, dispose this sparse array's index storages after excluding the internal ones shared between this array and the target <paramref name="array"/>. The default implementation only compares the two <see cref="ISparseArray{T}.IndexStorages"/>.
		/// </summary>
		/// <param name="array">The target <see cref="ISparseArray{T}"/> to exclude before disposing</param>
		void DisposeExclude(ISparseArray<T> array)
		{
			var list = this.IndexStorages;
			var other = array.IndexStorages;
			for (int i = 0; i < list.Length; i++)
			{
				bool canDispose = true;
				for (int j = 0; j < other.Length; j++)
				{
					if (list[i].OverlapWith(other[j]))
					{
						canDispose = false;
						break;
					}
				}
				if (canDispose)
					list[i].Dispose();
			}
		}

		bool ICheckValid.IsValid()
		{
			if (this.NStored <= 0)
				return false;
			var list = this.IndexStorages;
			return list.All(static l => l is not null && l.IsValid());
		}
		#endregion
	}

	/// <summary>
	/// Simple interface for sparse arrays where the index type is <typeparamref name="TIndex"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TIndex">Any integer-typed unmanaged integer number as the index data type</typeparam>
	public interface ISparseArray<T, TIndex> : ISparseArray<T> where T : unmanaged, INumber<T> where TIndex : unmanaged, IBinaryInteger<TIndex>
	{
		#region helpers
		/// <summary>
		/// Check given <paramref name="indexArrays"/> and its <paramref name="indexRealLengths"/> and put the referenced ones to <paramref name="refIndexArrays"/>
		/// </summary>
		/// <typeparam name="TS">The storage type that implements <see cref="IStorage{T, TSelf}"/> of <typeparamref name="TIndex"/></typeparam>
		/// <param name="indexArrays">The index array(s)' original storage(s) as a <see cref="ReadOnlySpan{T}"/> of <typeparamref name="TS"/></param>
		/// <param name="indexRealLengths">The actual presenting length of each array in <paramref name="indexArrays"/>, any 0 elements means the same as the length of <paramref name="indexArrays"/>. An empty one means all 0.</param>
		/// <param name="refIndexArrays">The output <see cref="Span{T}"/> to put the referenced <paramref name="indexArrays"/></param>
		/// <exception cref="ArgumentException">If the lengths are not the same</exception>
		/// <exception cref="ArgumentNullException">If any of <paramref name="indexArrays"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If any of <paramref name="indexRealLengths"/> is less than 0</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static void CheckIndexArrays<TS>(ReadOnlySpan<TS> indexArrays, ReadOnlySpan<long> indexRealLengths, Span<TS> refIndexArrays) where TS : class, IStorage<TIndex, TS>
		{
			if ((!indexRealLengths.IsEmpty && indexArrays.Length != indexRealLengths.Length) || refIndexArrays.Length != indexArrays.Length)
				throw new ArgumentException(Resources.Parameter.NotSameSize);
			if (indexArrays.Any(static a => a is null || !a.IsValid()))
				throw new ArgumentNullException(nameof(indexArrays));

			for (int i = 0; i < indexArrays.Length; i++)
			{
				long len = indexRealLengths.IsEmpty ? 0 : indexRealLengths[i];
				if (len < 0)
					throw new ArgumentOutOfRangeException(nameof(indexRealLengths), len, Resources.Parameter.CannotNegative);
				if (len == 0)
					refIndexArrays[i] = indexArrays[i].MakeReference();
				else
					refIndexArrays[i] = indexArrays[i].MakeReference(newLength: len);
			}
		}

		/// <summary>
		/// When implemented by a derived class, check if this sparse array share some storage(s) with the <paramref name="other"/> one. The default implementation only compares the <see cref="ValueArray{T}.Storage"/> and the index array(s).
		/// </summary>
		/// <param name="other">The other <see cref="ValueArray{T}"/> to check</param>
		/// <returns>True if they do share some storage, false otherwise</returns>
		bool OverlapWith(ISparseArray<T, TIndex> other)
		{
			if (this.ValueStorage.OverlapWith(other.ValueStorage))
				return true;
			ReadOnlySpan<IStorage> list = this.IndexStorages, array = other.IndexStorages;
			for (int i = 0; i < list.Length; i++)
			{
				for (int j = 0; j < array.Length; j++)
				{
					if (list[i].OverlapWith(array[j]))
					{
						return true;
					}
				}
			}
			return false;
		}
		#endregion
	}

	/// <summary>
	/// The interface for sparse vectors without indicating the index data type.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	public interface ISparseVector<T> : ISparseArray<T>, IVectorMetric where T : unmanaged, INumber<T>
	{
		#region property
		/// <summary>
		/// When implemented by a derived class, get the format of this sparse vector as a <see cref="LinearAlgebra.Sparse.SparseVectorFormat"/>.
		/// </summary>
		LinearAlgebra.Sparse.SparseVectorFormat Format { get; }
		#endregion

		#region conversion
		/// <summary>
		/// Convert this sparse vector to a dense vector whose value storage is <paramref name="denseStorage"/>.
		/// </summary>
		/// <typeparam name="TS">The type of the output dense storage</typeparam>
		/// <param name="denseStorage">The <typeparamref name="TS"/> of the dense vector to overwrite</param>
		void ToDense<TS>(TS denseStorage) where TS : class, IStorage<T, TS>;
		#endregion
	}

	/// <summary>
	/// The interface for sparse matrices without indicating the index data type.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	public interface ISparseMatrix<T> : ISparseArray<T>, IMatrixMetric where T : unmanaged, INumber<T>
	{
		#region property
		/// <summary>
		/// When implemented by a derived class, get the format of this sparse matrix as a <see cref="LinearAlgebra.Sparse.SparseMatrixFormat"/>.
		/// </summary>
		LinearAlgebra.Sparse.SparseMatrixFormat Format { get; }
		#endregion

		#region conversion
		/// <summary>
		/// When implemented by a derived class, convert this sparse matrix to a dense matrix whose value storage is <paramref name="denseStorage"/>.
		/// </summary>
		/// <typeparam name="TS">The type of the output dense storage</typeparam>
		/// <param name="denseStorage">The <typeparamref name="TS"/> of the dense matrix to overwrite</param>
		/// <param name="leadDim">The leading dimension of the target dense matrix, default 0 means <see cref="IMatrixMetric.NRows"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="denseStorage"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="leadDim"/> is less than <see cref="IMatrixMetric.NRows"/></exception>
		/// <exception cref="ArgumentException">If <paramref name="leadDim"/> * <see cref="IMatrixMetric.NCols"/> &gt; <paramref name="denseStorage"/>.<see cref="IStorage{T, TSelf}.Length">Length</see></exception>
		void ToDense<TS>(TS denseStorage, long leadDim = 0) where TS : class, IStorage<T, TS>;
		#endregion
	}

	/// <summary>
	/// The interface for sparse tensor without indicating the index data type.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	public interface ISparseTensor<T> : ISparseArray<T>, ITensor where T : unmanaged, INumber<T>
	{
		#region property
		/// <summary>
		/// When implemented by a derived class, get the format of this sparse tensor as a <see cref="TensorAlgebra.Sparse.SparseTensorFormat"/>.
		/// </summary>
		TensorAlgebra.Sparse.SparseTensorFormat Format { get; }
		#endregion

		#region conversion
		/// <summary>
		/// When implemented by a derived class, convert this sparse tensor to a dense tensor whose value storage is <paramref name="denseStorage"/>
		/// </summary>
		/// <typeparam name="TS">The type of the output dense storage</typeparam>
		/// <param name="denseStorage">The <typeparamref name="TS"/> of the dense tensor to overwrite</param>
		/// <param name="outerSize">The outer size of the target dense tensor, default empty means the same as <see cref="ITensor.Size"/> of this one</param>
		/// <exception cref="ArgumentNullException">If <paramref name="denseStorage"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="outerSize"/> is less than <see cref="ITensor.Size"/></exception>
		/// <exception cref="ArgumentException">If product(<paramref name="outerSize"/>) &gt; <paramref name="denseStorage"/>.<see cref="IStorage{T, TSelf}.Length">Length</see></exception>
		void ToDense<TS>(TS denseStorage, ReadOnlySpan<long> outerSize = default) where TS : class, IStorage<T, TS>;
		#endregion
	}
	#endregion


	#region matrix and vector metric
	/// <summary>
	/// The interface for basic vector metrics
	/// </summary>
	public interface IVectorMetric
	{
		/// <summary>
		/// When implemented by a derived class, get the presenting length of this vector
		/// </summary>
		long Length { get; }
	}

	/// <summary>
	/// The interface for basic matrix metrics
	/// </summary>
	public interface IMatrixMetric
	{
		/// <summary>
		/// When implemented by a derived class, get the presenting number of rows of this matrix
		/// </summary>
		long NRows { get; }

		/// <summary>
		/// When implemented by a derived class, get the presenting number of columns of this matrix
		/// </summary>
		long NCols { get; }
	}
	#endregion


	#region tensor
	/// <summary>
	/// The interface for tensor that contains basic members (size and label).
	/// </summary>
	public interface ITensor : ICheckValid
	{
		#region properties
		/// <summary>
		/// When implemented by a derived class, get the rank of this tensor
		/// </summary>
		int Rank => this.Size.Length;

		/// <summary>
		/// When implemented by a derived class, get the size of this array (the extent at all dimensions) as a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/>, must be of positive numbers.
		/// </summary>
		ReadOnlySpan<long> Size { get; }

		/// <summary>
		/// When implemented by a derived class, get or set the label array as a <see cref="ReadOnlySpan{T}"/> of <see cref="char"/> used to mark each index of this tensor.
		/// </summary>
		/// <exception cref="ArgumentException">If the setting value's length is not the same as the <see cref="Rank"/></exception>
		ReadOnlySpan<char> Labels { get; set; }
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
