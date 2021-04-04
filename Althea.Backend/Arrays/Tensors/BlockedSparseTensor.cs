using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

using Althea.Arrays;
using Althea.Helpers;
using Althea.LinearAlgebra.Sparse;
using Althea.Linq;
using Althea.NativeTypes;
using Althea.Solver;
using Althea.TensorAlgebra;
using Althea.TensorAlgebra.Sparse;

using LAD = Althea.LinearAlgebra.Dense.AbstractApi;
using LAS = Althea.LinearAlgebra.Sparse.AbstractApi;
using MEM = Althea.Storage.AbstractApi;
using TAS = Althea.TensorAlgebra.Sparse.AbstractApi;


namespace Althea.Backend.Arrays
{
	#region other info
	/// <summary>
	/// The <see cref="IOtherInfo"/> corresponding to <see cref="BlockedSparseTensor{T, TInd}"/>
	/// </summary>
	public sealed class BlockedSparseTensorOtherInfo : IOtherInfo
	{
		private readonly int rank;

		private readonly FixedBuffer_64<int> blockSize = default;

		/// <summary>
		/// Get the block size as a <see cref="ReadOnlySpan{T}"/> of <see cref="int"/>.
		/// </summary>
		public ReadOnlySpan<int> BlockSize {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.blockSize.AsSpan(this.rank);
		}

		/// <summary>
		/// Create a <see cref="BlockedSparseTensorOtherInfo"/> from the given <paramref name="blockSize"/>
		/// </summary>
		/// <param name="blockSize">The block size</param>
		public BlockedSparseTensorOtherInfo(ReadOnlySpan<int> blockSize)
		{
			this.rank = blockSize.Length;
			this.blockSize.CopyFromSpan(blockSize);
		}

		object IReadOnlyList<object>.this[int index] {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => index < 0 || index >= this.rank ? throw new ArgumentOutOfRangeException(nameof(index), index, Resources.Parameter.InvalidValue) : this.blockSize[index];
		}

		int IReadOnlyCollection<object>.Count {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.rank;
		}

		IEnumerator<object> IEnumerable<object>.GetEnumerator()
		{
			for (int i = 0; i < this.rank; i++)
			{
				yield return this.blockSize[i];
			}
		}

		IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<object>)this).GetEnumerator();
	}
	#endregion

	/// <summary>
	/// The concrete blocked sparse tensor class of format <see cref="SparseTensorFormat.BlockCoordinated"/> with the only mutable <see cref="ValueArray{T}.Storage"/> that refers to the value array storage and the <see cref="BlockedSparseTensor{T, TInd}.PositionStorages"/> refer to the overall position (of blocked tensors) storage.
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
	/// <typeparam name="TInd">Any integer-typed unmanaged struct as the index type</typeparam>
	public class BlockedSparseTensor<T, TInd> : BaseSparseTensor<T, TInd>, IKrylovVector<BlockedSparseTensor<T, TInd>, T> where T : unmanaged where TInd : unmanaged
	{
		#region basic
		private readonly FixedClassBuffer_16<Storage<TInd>> m_originalPosition;

		private readonly FixedClassBuffer_16<Storage<TInd>> m_position;

		private readonly FixedBuffer_64<int> m_blockSize = default;

		private readonly int m_blockLength;

		/// <summary>
		/// Get the size of the block tensors of this sparse tensor as a <see cref="ReadOnlySpan{T}"/> of <see cref="int"/>.
		/// </summary>
		public ReadOnlySpan<int> BlockSize {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.m_blockSize.AsSpan(this.Rank);
		}

		/// <summary>
		/// Get the total length (the product of <see cref="BlockSize"/>) of the block tensors of this sparse tensor as an <see cref="int"/>.
		/// </summary>
		public int BlockLength {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.m_blockLength;
		}

		/// <summary>
		/// Get the storage of the total presenting position of stored block tensors of this sparse tensor as a <see cref="ReadOnlySpan{T}"/> of <see cref="Storage{T}"/> of <typeparamref name="TInd"/>
		/// </summary>
		public ReadOnlySpan<Storage<TInd>> PositionStorages {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.m_position.AsSpan(this.Rank);
		}

		/// <summary>
		/// Get all the index arrays as a <see cref="ReadOnlySpan{T}"/> of <see cref="Storage{T}"/> of <typeparamref name="TInd"/>
		/// </summary>
		public override ReadOnlySpan<Storage<TInd>> IndexArrays {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.PositionStorages;
		}

		/// <summary>
		/// Get all the index arrays as a <see cref="ReadOnlySpan{T}"/> of <see cref="IStorage"/>
		/// </summary>
		public ReadOnlySpan<IStorage> IndexArraysInterfaceType {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.m_position.AsSpan<IStorage>(this.Rank);
		}

		/// <summary>
		/// Get the original index array's storage of this sparse tensor.
		/// </summary>
		protected override ReadOnlySpan<IStorage> OriginalIndexStorages {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.m_originalPosition.AsSpan<IStorage>(this.Rank);
		}

		/// <summary>
		/// Create an empty <see cref="BlockedSparseTensor{T, TInd}"/>
		/// </summary>
		public BlockedSparseTensor() : base(stackalloc long[1], Storage<T>.Empty, SparseTensorFormat.BlockCoordinated)
		{
			this.m_originalPosition = this.m_position = default;
			this.m_blockLength = 0;
		}

		/// <summary>
		/// Create a <see cref="BlockedSparseTensor{T, TInd}"/> of format <see cref="SparseTensorFormat.BlockCoordinated"/> with given <paramref name="size"/>, <paramref name="blockSize"/>, <paramref name="valueArray"/> and total presenting <paramref name="position"/> of block tensors.
		/// </summary>
		/// <param name="size">The presenting size of this sparse tensor</param>
		/// <param name="blockSize">The size of the block tensors</param>
		/// <param name="valueArray">The value array as a <see cref="Storage{T}"/> of <typeparamref name="T"/></param>
		/// <param name="position">The total presenting position of stored block tensors as a <see cref="ReadOnlySpan{T}"/> of <see cref="Storage{T}"/> of <typeparamref name="TInd"/> (with size == rank)</param>
		/// <param name="labels">The presenting labels of each dimension of this tensor, an empty one means auto generate as <c>{'a', 'b', ...}</c></param>
		/// <param name="stores">The number of stored values, default 0 means the length of <paramref name="valueArray"/></param>
		/// <param name="defaultValue">The default value (the value not specified) of this sparse tensor, default 0</param>
		/// <exception cref="TypeMismatchException">If the <typeparamref name="TInd"/> is not an integral type</exception>
		/// <exception cref="ArgumentException">If <paramref name="labels"/>'s length is neither 0 nor the same as the rank; or <paramref name="size"/> cannot be divided by <paramref name="blockSize"/>; or <paramref name="position"/> has incompatible size</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="valueArray"/> or any of <paramref name="position"/> is null or empty</exception>
		public BlockedSparseTensor(ReadOnlySpan<long> size, ReadOnlySpan<int> blockSize, Storage<T> valueArray, ReadOnlySpan<Storage<TInd>> position, ReadOnlySpan<char> labels = default, T defaultValue = default, long stores = 0) : base(size, valueArray, SparseTensorFormat.BlockCoordinated, labels, defaultValue, stores)
		{
			if (position.Length != size.Length)
				throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(position));
			if (blockSize.Length != size.Length)
				throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(blockSize));
			if (!size.SequenceEqual(blockSize, static (s, b) => s % b == 0))
				throw new ArgumentException(Resources.Other.CannotDivide, nameof(blockSize));

			this.m_blockSize.CopyFromSpan(blockSize);
			this.m_blockLength = blockSize.Prod();
			if (this.NStored % this.m_blockLength != 0)
				throw new ArgumentException(Resources.Other.CannotDivide, nameof(blockSize));

			this.m_originalPosition = new(position);
			this.m_position = default;
			ISparseArray<T, TInd>.CheckIndexArrays(position, stackalloc long[] { stores / this.m_blockLength }, m_position.AsSpan(this.Rank));
		}
		#endregion

		#region clone related
		/// <summary>
		/// Deep clone the array, the mutable status will not be copied.
		/// </summary>
		/// <returns>The cloned array</returns>
		public override BlockedSparseTensor<T, TInd> Clone()
		{
			Span<IntPtr> temp = stackalloc IntPtr[this.Rank];
			var outIndex = temp.AsClassType<ActualStorage<TInd>>();
			var value = ((ISparseArray<T, TInd>)this).CreateArraysAlike<T, TInd>(outIndex, copyValues: true);
			return new BlockedSparseTensor<T, TInd>(this.Size, this.BlockSize, value, temp.AsClassType<Storage<TInd>>(), this.Labels, this.DefaultValue);
		}

		/// <summary>
		/// Create a new sparse tensor with same properties as this one while the underlying storages are not filled.
		/// </summary>
		/// <returns>The new sparse tensor alike this one</returns>
		public override BlockedSparseTensor<T, TInd> NewArrayAlike() => (BlockedSparseTensor<T, TInd>)base.NewArrayAlike();

		/// <summary>
		/// Create a new sparse tensor with same properties as this one while the underlying storages are not filled and the data type is changed to <typeparamref name="TOut"/> while index type changed to <typeparamref name="TIndOut"/>.
		/// </summary>
		/// <typeparam name="TOut">Any unmanaged struct as the new data type</typeparam>
		/// <typeparam name="TIndOut">Any integral-typed unmanaged struct as the new index type</typeparam>
		/// <returns>The new sparse tensor alike this one</returns>
		/// <exception cref="TypeMismatchException">If the <typeparamref name="TIndOut"/> is not an integral type</exception>
		public override BlockedSparseTensor<TOut, TIndOut> NewArrayAlike<TOut, TIndOut>()
		{
			Span<IntPtr> temp = stackalloc IntPtr[this.Rank];
			var outIndex = temp.AsClassType<ActualStorage<TIndOut>>();
			var value = ((ISparseArray<T, TInd>)this).CreateArraysAlike<TOut, TIndOut>(outIndex, copyValues: false);
			return new BlockedSparseTensor<TOut, TIndOut>(this.Size, this.BlockSize, value, temp.AsClassType<Storage<TIndOut>>(), this.Labels, this.DefaultValue.GenericConvert<T, TOut>());
		}
		#endregion

		#region conversion
		/// <summary>
		/// Convert this sparse tensor to a dense tensor whose <see cref="Storage{T}"/> is <paramref name="denseStorage"/>
		/// </summary>
		/// <param name="denseStorage">The <see cref="Storage{T}"/> of the dense tensor to overwrite</param>
		/// <param name="outerSize">The outer size of the target dense tensor, default empty means the same as <see cref="BaseTensor{T}.Size"/> of this one</param>
		/// <exception cref="ArgumentNullException">If <paramref name="denseStorage"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="outerSize"/> is less than <see cref="BaseTensor{T}.Size"/></exception>
		/// <exception cref="ArgumentException">If product(<paramref name="outerSize"/>) &gt; <paramref name="denseStorage"/>.<see cref="Storage{T}.Length">Length</see></exception>
		public override void ToDense(Storage<T> denseStorage, ReadOnlySpan<long> outerSize = default)
		{
			if (denseStorage is null || !denseStorage.IsValid())
				throw new ArgumentNullException(nameof(denseStorage));
			if (outerSize.IsEmpty)
			{
				outerSize = this.Size;
			}
			else
			{
				if (outerSize.Length != this.Rank)
					throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(outerSize));
				if (outerSize.Prod() > denseStorage.Length)
					throw new ArgumentException(Resources.Parameter.InvalidValue, nameof(outerSize));
				if (!outerSize.SequenceLargerEqualThan(this.Size))
					throw new ArgumentOutOfRangeException(nameof(outerSize), outerSize.ToArray(), Resources.Parameter.InvalidValue);
			}
			TAS.ToDense(new SparseTensorWrapper<T>(this), denseStorage, outerSize);
		}
		#endregion

		#region reshape
		/// <summary>
		/// Reshape this array to a vector
		/// </summary>
		/// <returns>The referenced vector reshaped from this array</returns>
		public override SparseVector<T, TInd> ToVector()
		{
			var wrapper = TAS.Reshape<T>(new(this), stackalloc long[] { this.Length });
			try
			{
				return wrapper.CheckWrapper<T, TInd>(this.Length);
			}
			catch (Exception)
			{
				wrapper.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Reshape this array to a matrix with leading dimension = <paramref name="rows"/>
		/// </summary>
		/// <param name="rows">The number of rows of the target matrix; if <paramref name="rows"/> ≤ 0, it is assumed that leadDim = <c>sqrt(<see cref="AbstractArray{T}.Length"/>)</c>.</param>
		/// <returns>The reshaped matrix</returns>
		public unsafe override Althea.Arrays.BaseSparseMatrix<T, TInd> ToMatrix(long rows = 0)
		{
			var sizePtr = stackalloc long[] { rows, 0 };
			Span<long> size = new(sizePtr, 2);
			CheckSize(this, size);
			// get matrix shaped tensor
			SparseArrayWrapper<T> tensor;
			if (size.SequenceEqual(this.Size))
			{
				tensor = new(this.Storage, this.IndexArraysInterfaceType, (int)this.Format, this.DefaultValue, new BlockedSparseTensorOtherInfo(this.BlockSize));
			}
			else
			{
				tensor = TAS.Reshape<T>(new(this), size);
			}
			// index to matrix indices
			try
			{
				if (tensor.OtherInfo is not BlockedSparseTensorOtherInfo info)
					throw new NotSupportedException();
				return new BlockedSparseMatrix<T, TInd>(size[0], size[1], info.BlockSize[0], info.BlockSize[1], this.Storage, (Storage<TInd>)tensor.IndexStorages[0], (Storage<TInd>)tensor.IndexStorages[1], SparseMatrixFormat.BCOC, this.DefaultValue);
			}
			catch (Exception)
			{
				tensor.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Reshape this tensor to another tensor with the given <paramref name="newSize"/> 
		/// </summary>
		/// <param name="newSize">The new size of the tensor as a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/></param>
		/// <returns>The referenced reshaped tensor</returns>
		public override BlockedSparseTensor<T, TInd> TensorReshape(ReadOnlySpan<long> newSize)
		{
			Span<long> size = stackalloc long[newSize.Length];
			newSize.CopyTo(size);
			CheckSize(this, size);
			if (size.SequenceEqual(this.Size))
				return this;
			// else
			var wrapper = TAS.Reshape<T>(new(this), size);
			try
			{
				return (BlockedSparseTensor<T, TInd>)wrapper.CheckWrapper<T, TInd>(size, default);
			}
			catch (Exception)
			{
				wrapper.Dispose();
				throw;
			}
		}
		#endregion

		#region indexing
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private T ElementIndexing(ReadOnlySpan<long> indices, T? value)
		{
			int rank = this.Rank;
			var positions = this.PositionStorages;
			long offset = this.CheckIndex(indices);
			long blockOffset = offset % this.m_blockLength;
			long find = -1;
			for (int i = 0; i < rank; i++)
			{
				long f = LAS.IndexFind(sorted: true, positions[i], indices[i].FromLong<TInd>());
				if (find == -1)
				{
					find = f;
					continue;
				}
				if (f < 0 || f != find)
				{
					find = -1;
					break;
				}
			}
			if (find < 0)
			{
				if (value.HasValue)
					throw new InvalidOperationException();
				else
					return this.DefaultValue;
			}
			else
			{
				if (value.HasValue)
				{
					MEM.FromManaged(this.Storage + (find * this.m_blockLength + blockOffset), value.Value);
					return default;
				}
				else
				{
					return MEM.ToManaged(this.Storage + (find * this.m_blockLength + blockOffset));
				}
			}
		}

		/// <summary>
		/// The basic indexed getter and setter of this tensor
		/// </summary>
		/// <param name="indices">The position indicated by a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/> to be checked</param>
		/// <returns>The element at <paramref name="indices"/></returns>
		/// <exception cref="ArgumentException">If <paramref name="indices"/>'s length is not the same as the rank</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="indices"/> is out of range</exception>
		/// <exception cref="InvalidOperationException">If the element at <paramref name="indices"/> is not stored and the setting value is not <see cref="Althea.Arrays.BaseSparseTensor{T, TInd}.DefaultValue"/></exception>
		public override T this[ReadOnlySpan<long> indices] {
			get {
				return this.ElementIndexing(indices, null);
			}
			set {
				this.ElementIndexing(indices, value);
			}
		}

		/// <summary>
		/// Get a sub-tensor (of same rank) indicated by the given starting <paramref name="offsets"/> and <paramref name="lengths"/>
		/// </summary>
		/// <param name="offsets">The starting offsets of the target sub-tensor compared to this tensor at each dimension, in <typeparamref name="T"/></param>
		/// <param name="lengths">The lengths of the target sub-tensor at each dimension, in <typeparamref name="T"/></param>
		/// <returns>The sub-tensor indicated by <paramref name="offsets"/> and <paramref name="lengths"/>. Shall be a referenced tensor if possible.</returns>
		/// <exception cref="ArgumentException">If <paramref name="offsets"/> and/or <paramref name="lengths"/>'s length is not the same as the rank</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offsets"/> and/or <paramref name="lengths"/> is out of range</exception>
		public override BlockedSparseTensor<T, TInd> GetSlice(ReadOnlySpan<long> offsets, ReadOnlySpan<long> lengths)
		{
			this.CheckRange(offsets, lengths);
			if (!lengths.SequenceEqual(this.BlockSize, static (l, b) => l % b == 0))
				throw new ArgumentException(Resources.Other.CannotDivide, nameof(lengths));
			var wrapper = TAS.GetSlice<T>(new(this), offsets, lengths);
			try
			{
				return (BlockedSparseTensor<T, TInd>)wrapper.CheckWrapper<T, TInd>(lengths, this.Labels);
			}
			catch (Exception)
			{
				wrapper.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Get the sub-tensor (of same rank) indicated by the given starting <paramref name="offsets"/> and <paramref name="lengths"/> and copy it to <paramref name="overwrite"/>
		/// </summary>
		/// <param name="offsets">The starting offsets of the target sub-tensor compared to this tensor at each dimension, in <typeparamref name="T"/></param>
		/// <param name="lengths">The lengths of the target sub-tensor at each dimension, in <typeparamref name="T"/></param>
		/// <param name="overwrite">The tensor to be overwritten by the sub-tensor</param>
		/// <exception cref="ArgumentNullException">If <paramref name="overwrite"/> is null or empty</exception>
		/// <exception cref="ArgumentException">If <paramref name="offsets"/> and/or <paramref name="lengths"/>'s length is not the same as the rank; or <paramref name="overwrite"/> cannot be overwritten</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offsets"/> and/or <paramref name="lengths"/> is out of range</exception>
		public override void GetSlice(ReadOnlySpan<long> offsets, ReadOnlySpan<long> lengths, BaseTensor<T> overwrite)
		{
			if (overwrite is null || !overwrite.IsValid())
				throw new ArgumentNullException(nameof(overwrite));
			if (!overwrite.Size.SequenceLargerEqualThan(lengths))
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(overwrite));
			this.CheckRange(offsets, lengths);
			if (!lengths.SequenceEqual(this.BlockSize, static (l, b) => l % b == 0))
				throw new ArgumentException(Resources.Other.CannotDivide, nameof(lengths));

			if (overwrite is DenseTensor<T> dense)
			{
				TAS.GetSlice(new(this), offsets, lengths, dense.Storage, dense.OuterSize);
			}
			else if (overwrite is SparseTensor<T, TInd> sparse1)
			{
				TAS.GetSlice<T>(new(this), offsets, lengths, new(sparse1));
			}
			else if (overwrite is BlockedSparseTensor<T, TInd> sparse2)
			{
				if (!sparse2.BlockSize.SequenceEqual(this.BlockSize))
					throw new ArgumentException(Resources.Parameter.WrongSize, nameof(overwrite));
				TAS.GetSlice<T>(new(this), offsets, lengths, new(sparse2));
			}
			else
				throw new ArgumentException(Resources.Parameter.UnexpectedType, nameof(overwrite));
		}

		/// <summary>
		/// Set the sub-tensor (of same rank) indicated by the given starting <paramref name="offsets"/> and the size of <paramref name="value"/> to the underlying tensor of <paramref name="value"/>
		/// </summary>
		/// <param name="offsets">The starting offsets of the target sub-tensor compared to this tensor at each dimension, in <typeparamref name="T"/></param>
		/// <param name="lengths">The lengths of the target sub-tensor at each dimension, in <typeparamref name="T"/></param>
		/// <param name="value">The tensor to set whose size is the lengths of the sub-tensor's size</param>
		/// <exception cref="ArgumentNullException">If <paramref name="value"/> is null or empty</exception>
		/// <exception cref="ArgumentException">If <paramref name="offsets"/> and/or <paramref name="value"/>'s size is not the same as the rank</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offsets"/> and/or <paramref name="value"/>'s size is out of range</exception>
		public override void SetSlice(ReadOnlySpan<long> offsets, ReadOnlySpan<long> lengths, BaseTensor<T> value)
		{
			if (value is null || !value.IsValid())
				throw new ArgumentNullException(nameof(value));
			if (value is not BlockedSparseTensor<T, TInd> sparse)
				throw new ArgumentException(Resources.Parameter.UnexpectedType, nameof(value));
			if (!sparse.Size.SequenceLargerEqualThan(lengths) || !sparse.BlockSize.SequenceEqual(this.BlockSize))
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(value));
			if (!lengths.SequenceEqual(this.BlockSize, static (l, b) => l % b == 0))
				throw new ArgumentException(Resources.Other.CannotDivide, nameof(lengths));

			this.CheckRange(offsets, lengths);
			TAS.SetSlice<T>(new(this), offsets, lengths, new(sparse));
		}

		/// <summary>
		/// Get the sub-tensor of rank <paramref name="n"/> with <paramref name="offsets"/> and <paramref name="lengths"/> compared to the sub-tensor located by the given <paramref name="restIndices"/> of length <c>(<see cref="AbstractArray{T}.Rank">rank</see> - <paramref name="n"/>)</c>.
		/// </summary>
		/// <param name="n">The first <paramref name="n"/> dimensions to get</param>
		/// <param name="restIndices">The position of the target sub-tensor at the rest (<see cref="AbstractArray{T}.Rank">rank</see> - <paramref name="n"/>) dimensions</param>
		/// <param name="offsets">The starting offsets of the target sub-tensor compared to this tensor at the first <paramref name="n"/> dimensions. Default (an empty one) means all zeros.</param>
		/// <param name="lengths">The lengths of the target sub-tensor at the first <paramref name="n"/> dimensions. Default (an empty one) means the max possible values.</param>
		/// <returns>The sub-tensor at the first <paramref name="n"/> dimensions</returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="n"/> ≤ 0 or <paramref name="n"/> ≥ <see cref="AbstractArray{T}.Rank">rank</see> - 1; or any of <paramref name="offsets"/> and <paramref name="lengths"/> is out of range</exception>
		/// <exception cref="ArgumentException">If the length of <paramref name="restIndices"/> is not (<see cref="AbstractArray{T}.Rank">rank</see> - <paramref name="n"/>)</exception>
		public override BlockedSparseTensor<T, TInd> GetFirstDims(int n, ReadOnlySpan<long> restIndices, ReadOnlySpan<long> offsets = default, ReadOnlySpan<long> lengths = default)
		{
			// get equivalent ranges
			Span<long> allOffsets = stackalloc long[this.Rank];
			Span<long> allLengths = stackalloc long[this.Rank];
			long offset = this.CheckFirstDims(n, restIndices, offsets, lengths, allOffsets, allLengths);
			// check
			if (!allOffsets[..n].All(static a => a == 0) || !allLengths[..n].SequenceEqual(this.Size[..n]))
				throw new NotSupportedException();
			// return
			return this.GetSlice(allOffsets, allLengths);
		}

		/// <summary>
		/// Set the sub-tensor of rank <paramref name="n"/> with <paramref name="offsets"/> and <paramref name="lengths"/> compared to the sub-tensor located by the given <paramref name="restIndices"/> of length <c>(<see cref="AbstractArray{T}.Rank">rank</see> - <paramref name="n"/>)</c> to the given <paramref name="value"/>.
		/// </summary>
		/// <param name="n">The first <paramref name="n"/> dimensions to get</param>
		/// <param name="restIndices">The position of the target sub-tensor at the rest (<see cref="AbstractArray{T}.Rank">rank</see> - <paramref name="n"/>) dimensions</param>
		/// <param name="value">The dense tensor to set</param>
		/// <param name="offsets">The starting offsets of the target sub-tensor compared to this tensor at the first <paramref name="n"/> dimensions. Default (an empty one) means all zeros.</param>
		/// <param name="lengths">The lengths of the target sub-tensor at the first <paramref name="n"/> dimensions. Default (an empty one) means the max possible values.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="value"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="n"/> ≤ 0 or <paramref name="n"/> ≥ <see cref="AbstractArray{T}.Rank">rank</see> - 1; or any of <paramref name="offsets"/> and <paramref name="lengths"/> is out of range</exception>
		/// <exception cref="ArgumentException">If the length of <paramref name="restIndices"/> is not (<see cref="AbstractArray{T}.Rank">rank</see> - <paramref name="n"/>)</exception>
		public override void SetFirstDims(int n, ReadOnlySpan<long> restIndices, BaseTensor<T> value, ReadOnlySpan<long> offsets = default, ReadOnlySpan<long> lengths = default)
		{
			if (value is null || !value.IsValid())
				throw new ArgumentNullException(nameof(value));
			if (value is not BlockedSparseTensor<T, TInd> sparse)
				throw new ArgumentException(Resources.Parameter.UnexpectedType, nameof(value));
			if (!sparse.BlockSize.SequenceEqual(this.BlockSize))
				throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(value));
			// get equivalent ranges
			Span<long> allOffsets = stackalloc long[this.Rank];
			Span<long> allLengths = stackalloc long[this.Rank];
			long offset = this.CheckFirstDims(n, restIndices, offsets, lengths, allOffsets, allLengths);
			// check
			if (!allOffsets[..n].All(static a => a == 0) || !allLengths[..n].SequenceEqual(this.Size[..n]))
				throw new NotSupportedException();
			// set
			this.SetSlice(allOffsets, allLengths, sparse);
		}
		#endregion

		#region tensor algebra methods
		/// <summary>
		/// Compute the tensor reduction (self partial summation) of this tensor under the given <paramref name="order"/>.
		/// </summary>
		/// <param name="order">The given <see cref="TensorOrder"/> to indicate which part(s) of dimension(s) to sum, its order will be ignored</param>
		/// <param name="scalar">The scalar to multiply to the result</param>
		/// <returns>The reduction result as a new <see cref="BlockedSparseTensor{T, TInd}"/></returns>
		/// <exception cref="ArgumentException">If <paramref name="order"/> does not indicate a partial permutation order</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="scalar"/> is 0</exception>
		public override BlockedSparseTensor<T, TInd> Reduce(TensorOrder order, T scalar)
		{
			if (scalar.IsZero())
				throw new ArgumentOutOfRangeException(nameof(scalar), scalar, Resources.Parameter.CannotZero);

			Span<int> reducePerm = stackalloc int[this.Rank];
			Span<long> size = stackalloc long[this.Rank];
			Span<char> label = stackalloc char[this.Rank];
			reducePerm = this.CheckReduce(order, reducePerm, ref size, ref label);
			// reduce
			var wrapper = TAS.Reduce<T>(BinaryOperation.Addition, new(this, scalar: scalar), reducePerm);
			try
			{
				return (BlockedSparseTensor<T, TInd>)wrapper.CheckWrapper<T, TInd>(size, label);
			}
			catch (Exception)
			{
				wrapper.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Compute the tensor permutation of this tensor under the given <paramref name="order"/>.
		/// </summary>
		/// <param name="order">The given <see cref="TensorOrder"/> to indicate the permutation order</param>
		/// <param name="scalar">The scalar to multiply to the result</param>
		/// <returns>The permutation result as a new <see cref="BlockedSparseTensor{T, TInd}"/></returns>
		/// <exception cref="ArgumentException">If <paramref name="order"/> does not indicate a full permutation order</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="scalar"/> is 0</exception>
		public override BlockedSparseTensor<T, TInd> Permute(TensorOrder order, T scalar)
		{
			if (scalar.IsZero())
				throw new ArgumentOutOfRangeException(nameof(scalar), scalar, Resources.Parameter.CannotZero);
			// get permutation
			int rank = this.Rank;
			Span<int> perm = stackalloc int[rank];
			perm = order.GetIntSpanOrder(this, perm, allowPartial: false);
			// get output members
			Span<long> size = stackalloc long[rank];
			Span<char> label = stackalloc char[rank];
			this.Size.ReOrderTo(size, perm);
			this.Labels.ReOrderTo(label, perm);
			// permute
			var wrapper = TAS.Permute<T>(new(this, scalar: scalar), perm);
			try
			{
				return (BlockedSparseTensor<T, TInd>)wrapper.CheckWrapper<T, TInd>(size, label);
			}
			catch (Exception)
			{
				wrapper.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Compute the tensor point-wise addition of this tensor and the <paramref name="other"/> tensor.
		/// </summary>
		/// <param name="scalarThis">The scalar to multiply to this tensor</param>
		/// <param name="other">The other tensor to perform the addition with</param>
		/// <param name="scalarOther">The scalar to multiply to the <paramref name="other"/> tensor</param>
		/// <returns>The addition result as a new <see cref="BlockedSparseTensor{T, TInd}"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="other"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="other"/> has different size than this one</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="scalarThis"/> or <paramref name="scalarOther"/> is 0</exception>
		public override BlockedSparseTensor<T, TInd> AddTensor(T scalarThis, BaseTensor<T> other, T scalarOther)
		{
			if (scalarThis.IsZero())
				throw new ArgumentOutOfRangeException(nameof(scalarThis), scalarThis, Resources.Parameter.CannotZero);
			if (scalarOther.IsZero())
				throw new ArgumentOutOfRangeException(nameof(scalarOther), scalarOther, Resources.Parameter.CannotZero);
			if (other is not BlockedSparseTensor<T, TInd> sparse || !sparse.BlockSize.SequenceEqual(this.BlockSize))
				throw new NotSupportedException(Resources.Parameter.UnexpectedType);
			if (!this.Size.SequenceEqual(other.Size))
				throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(other));

			Span<int> identityPerm = stackalloc int[this.Rank].FillWithRange(0);
			var wrapper = TAS.OperationBinary<T>(BinaryOperation.Addition, new(this, scalar: scalarThis), identityPerm, new(sparse, scalar: scalarOther), identityPerm);
			try
			{
				return (BlockedSparseTensor<T, TInd>)wrapper.CheckWrapper<T, TInd>(this.Size, this.Labels);
			}
			catch (Exception)
			{
				wrapper.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Compute the tensor contraction of this tensor and the <paramref name="other"/> tensor using their .
		/// </summary>
		/// <param name="other">The other tensor to perform the contraction with</param>
		/// <param name="scalar">The scalar to multiply to the result</param>
		/// <param name="outputLabels">The desired output tensor's labels as a <see cref="ReadOnlySpan{T}"/> of <see cref="char"/>. Default (empty) means simple union of the labels of this tensor and the <paramref name="other"/> tensor.</param>
		/// <returns>The contraction result as a new <see cref="BlockedSparseTensor{T, TInd}"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="other"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="other"/>'s labels indicate that it cannot contract with this tensor</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="scalar"/> is 0</exception>
		public override BlockedSparseTensor<T, TInd> Contract(BaseTensor<T> other, T scalar, ReadOnlySpan<char> outputLabels = default)
		{
			if (scalar.IsZero())
				throw new ArgumentOutOfRangeException(nameof(scalar), scalar, Resources.Parameter.CannotZero);
			if (other is not BlockedSparseTensor<T, TInd> sparse || !sparse.BlockSize.SequenceEqual(this.BlockSize))
				throw new NotSupportedException(Resources.Parameter.UnexpectedType);
			// stack allocate
			int commonRank = TensorContractInfo.GetContractRank(this, other);
			Span<int> concA = stackalloc int[commonRank], concB = stackalloc int[commonRank];
			Span<int> freeCA = stackalloc int[this.Rank - commonRank], freeCB = stackalloc int[sparse.Rank - commonRank];
			Span<long> sizeC = stackalloc long[this.Rank + sparse.Rank - commonRank];
			Span<char> labelC = stackalloc char[sizeC.Length];
			// get contraction info
			var info = TensorContractInfo.GetBinaryContractInfo(this.Size, this.Labels,
																sparse.Size, sparse.Labels,
																concA, concB, freeCA, freeCB,
																sizeC, labelC, outputLabels);
			// contract tensor
			var wrapper = TAS.Contract<T>(new(this, scalar: scalar), new(sparse), info);
			try
			{
				return (BlockedSparseTensor<T, TInd>)wrapper.CheckWrapper<T, TInd>(sizeC, labelC);
			}
			catch (Exception)
			{
				wrapper.Dispose();
				throw;
			}
		}
		#endregion

		#region in-place tensor contraction
		/// <summary>
		/// Compute the tensor contraction of the given tensors <paramref name="A"/> and <paramref name="B"/> and add the result to this tensor (scaled by <paramref name="scalarThis"/>) in-place.
		/// </summary>
		/// <param name="A">The first input tensor to perform the contraction</param>
		/// <param name="B">The second input tensor to perform the contraction</param>
		/// <param name="scalar">The scalar to multiply to the contraction result</param>
		/// <param name="scalarThis">The scalar to multiply this tensor before addition</param>
		/// <param name="unaryA">The <see cref="UnaryOperation"/> to be applied to each element of <paramref name="A"/> before contraction</param>
		/// <param name="unaryB">The <see cref="UnaryOperation"/> to be applied to each element of <paramref name="B"/> before contraction</param>
		/// <param name="unaryThis">The <see cref="UnaryOperation"/> to be applied to each element of this tensor before addition</param>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="B"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="A"/>'s labels indicate that it cannot contract with <paramref name="B"/>'s; or the contraction cannot be performed in-place</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="scalar"/> is 0</exception>
		public void ContractFrom(T scalar, BlockedSparseTensor<T, TInd> A, BlockedSparseTensor<T, TInd> B, T scalarThis = default, UnaryOperation unaryA = UnaryOperation.Identity, UnaryOperation unaryB = UnaryOperation.Identity, UnaryOperation unaryThis = UnaryOperation.Identity)
		{
			if (scalar.IsZero())
				throw new ArgumentOutOfRangeException(nameof(scalar), scalar, Resources.Parameter.CannotZero);
			if (A is null || !A.IsValid())
				throw new ArgumentNullException(nameof(A));
			if (!A.BlockSize.SequenceEqual(this.BlockSize))
				throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(A));
			if (B is null || !B.IsValid())
				throw new ArgumentNullException(nameof(B));
			if (!B.BlockSize.SequenceEqual(this.BlockSize))
				throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(B));

			int concRank = TensorContractInfo.GetContractRank(A, B);
			Span<int> concA = stackalloc int[concRank], concB = stackalloc int[concRank];
			Span<int> freeCA = stackalloc int[A.Rank - concRank], freeCB = stackalloc int[B.Rank - concRank];
			TensorContractInfo info = new(A, B, this, concA, concB, freeCA, freeCB);
			TAS.ContractInPlace<T>(new(A, scalar, unaryA), new(B, unaryB), info, new(this, scalarThis, unaryThis));
		}
		#endregion

		#region IKrylovVector
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void CheckSparsity(BlockedSparseTensor<T, TInd> other)
		{
			if (other is null || !other.IsValid())
				throw new ArgumentNullException(nameof(other));
			if (!this.Size.SequenceEqual(other.Size))
				throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(other));
			if (this.NStored != other.NStored || !this.BlockSize.SequenceEqual(other.BlockSize))
				throw new InvalidOperationException(Resources.Parameter.NotSameSize);
			if (!this.PositionStorages.SequenceEqual(other.PositionStorages) &&
				!this.PositionStorages.SequenceEqual(other.PositionStorages, static (a, b) => LAD.PointWiseEquals(a, 1, b, 1)))
				throw new InvalidOperationException(Resources.Other.DifferentSparsity);
		}

		BlockedSparseTensor<T, TInd> IKrylovVector<BlockedSparseTensor<T, TInd>, T>.NewArrayAlike()
		{
			var values = this.Storage.Clone();
			try
			{
				return new(this.Size, this.BlockSize, values, this.PositionStorages, this.Labels, this.DefaultValue);
			}
			catch (Exception)
			{
				values?.Dispose();
				throw;
			}
		}

		T IKrylovVector<BlockedSparseTensor<T, TInd>, T>.Dot(BlockedSparseTensor<T, TInd> other)
		{
			this.CheckSparsity(other);
			return LAD.Dot(conjX: true, this.Storage, 1, other.Storage, 1);
		}

		void IKrylovVector<BlockedSparseTensor<T, TInd>, T>.AddBy(BlockedSparseTensor<T, TInd> other, T scalar)
		{
			this.CheckSparsity(other);
			LAD.VectorGeneralAdd(scalar, other.Storage, 1, this.Storage, 1);
		}

		/// <summary>
		/// Replace this tensor's content with the <paramref name="other"/> tensor in-place.
		/// </summary>
		/// <param name="other">The other <see cref="BlockedSparseTensor{T, TInd}"/> to replace from</param>
		/// <exception cref="ArgumentNullException">If <paramref name="other"/> is null or invalid</exception>
		/// <exception cref="InvalidOperationException">If this and <paramref name="other"/> have different sizes</exception>
		public void ReplaceBy(BlockedSparseTensor<T, TInd> other)
		{
			if (other is null || !other.IsValid())
				throw new ArgumentNullException(nameof(other));
			if (!this.Size.SequenceEqual(other.Size))
				throw new InvalidOperationException(Resources.Parameter.NotSameSize);
			if (this.NStored != other.NStored || !this.BlockSize.SequenceEqual(other.BlockSize))
				throw new InvalidOperationException(Resources.Parameter.NotSameSize);

			MEM.MemoryCopy(other.Storage, this.Storage);
			var posOther = other.PositionStorages; var posThis = this.PositionStorages;
			for (int i = 0; i < this.Rank; i++)
			{
				MEM.MemoryCopy(posOther[i], posThis[i]);
			}
		}
		#endregion

		#region print
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private long[][] GetPosition(int length)
		{
			long[][] result = new long[this.Rank][];
			for (int i = 0; i < this.Rank; i++)
			{
				long[] resI = new long[length];
				SparseMatrix<T, TInd>.ToManaged(this.PositionStorages[i], resI);
				result[i] = resI;
			}
			return result;
		}

		/// <summary>
		/// Print out this tensor.
		/// </summary>
		/// <param name="overrideSetting">Override global settings in <see cref="Settings"/></param>
		/// <returns>The detailed string representation</returns>
		public override string Print(PrintSettings? overrideSetting = null)
		{
			string description = this.ToString();
			if (this.Disposed)
				return description;

			var settings = overrideSetting ?? Settings.PrintSetting;

			StringBuilder detail = new(description);
			detail.AppendLine(":");
			// get sizes
			int length = (int)Math.Min(settings.ArrayLength, this.NStored / this.m_blockLength);
			if (!settings.MatrixFormTensor)
			{
				length = (int)Math.Ceiling(Math.Sqrt(length));
				settings = new(settings, arrayLength: length);
			}
			int rank = this.Rank;
			var sizeProd = this.SizeProd;
			Span<long> blockSize = stackalloc long[rank];
			Span<long> blockSizeProd = stackalloc long[rank + 1];
			this.BlockSize.CopyTo(blockSize, static s => s);
			blockSize.AccumulateProd(blockSizeProd);
			var position = this.GetPosition(length);
			// to string
			for (int i = 0; i < length; i++)
			{
				// append range part
				int detailLengthPrev = detail.Length;
				detail.Append('[');
				for (int k = 0; k < rank; k++)
				{
					long offsetK = position[k][i];
					detail.Append(offsetK).Append("..").Append(offsetK + blockSize[k]).Append(", ");
				}
				detail.Remove(detail.Length - 2, 2).Append("] -> ");
				// append dense block tensor
				string pad = new(' ', detail.Length - detailLengthPrev);
				string tensorRepr = DenseTensor<T>.ActualPrint(this.Storage + i * this.m_blockLength, blockSize, blockSize, blockSizeProd, settings, prefix: pad);
				detail.Append(((ReadOnlySpan<char>)tensorRepr)[pad.Length..]);
			}
			if (this.NStored / this.m_blockLength > length)
				detail.AppendLine().Append(string.Format(Resources.Print.MoreStored, this.NStored / this.m_blockLength - length));
			return detail.ToString();
		}
		#endregion

		#region serialization
		/// <summary>
		/// The presenting name of <see cref="PositionStorages"/>, shall be used in <see cref="string.Format(string, object?)"/>
		/// </summary>
		protected internal const string PositionStoragesName = nameof(PositionStorages) + "_{0}";

		/// <summary>
		/// Get all the storages of this array. Only returns <see cref="ValueArray{T}.Storage"/> and <see cref="PositionStorages"/>.
		/// </summary>
		/// <returns>All the storages of the array as an <see cref="IReadOnlyDictionary{TKey, TValue}"/> of <see cref="string"/> and <see cref="IStorage"/></returns>
		public override IReadOnlyDictionary<string, IStorage> GetStorages()
		{
			var dict = new Dictionary<string, IStorage>(1 + this.Rank)
			{
				[StorageName] = this.Storage,
			};
			var positions = this.PositionStorages;
			for (int i = 0; i < positions.Length; i++)
			{
				dict.Add(string.Format(PositionStoragesName, i), positions[i]);
			}
			return dict;
		}

		/// <summary>
		/// The presenting name of <see cref="BlockSize"/>
		/// </summary>
		protected internal const string BlockSizeName = nameof(BlockSize);

		/// <summary>
		/// Get other requisite informations for re-constructing the array of that derived class type. Only returns the <see cref="BaseTensor{T}.Labels"/> and <see cref="BlockSize"/> and <see cref="Althea.Arrays.BaseSparseTensor{T, TInd}.DefaultValue"/>.
		/// </summary>
		/// <returns>Other requisite informations used to re-construct this array</returns>
		public override IReadOnlyDictionary<string, object> GetMetaData() => new Dictionary<string, object>(3)
		{
			[LabelsName] = this.Labels.ToArray(),
			[BlockSizeName] = this.BlockSize.ToArray(),
			[DefaultValueName] = this.DefaultValue
		};
		#endregion
	}
}