using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Althea.Helpers;
using Althea.Linq;
using Althea.NativeTypes;
using Althea.TensorAlgebra;


namespace Althea.Arrays
{
	/// <summary>
	/// The abstract tensor class with the only mutable <see cref="ValueArray{T}.Storage"/> that refers to the actual data storage. There may be more pointer(s) for different indices in a sparse tensor that inherits <see cref="BaseTensor{T}"/>, but they shall be immutable.
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
	/// <remarks>
	/// Since the <see cref="BaseTensor{T}"/> may be reference created quite frequently, storing the size as a C# <see cref="Array"/> is rather expensive.<br/>
	/// Thus the C++ equivalent "<c>struct { long size[16] }</c>" of <see cref="FixedBuffer_128{T}"/> (which is a essentially a <c>fixed long size[16]</c>) is used instead to reduce the GC pressure.<br/>
	/// Also, the <see cref="BaseTensor{T}"/> has no finalizer and if it is composed of <see cref="ReferenceStorage{T}"/> which still has no finalizer, the instance stays in GC generation 0 which is quite fast in deallocation.<br/>
	/// Therefore, the derived class shall follow the same strategy, such as <see cref="BaseSparseTensor{T, TInd}"/>.
	/// </remarks>
	[StructLayout(LayoutKind.Explicit)]
	public abstract class BaseTensor<T> : ValueArray<T>, ITensor where T : unmanaged
	{
		#region basic
		// previously defined 8 + (8 * 2) bytes
		[FieldOffset(0)]
		private readonly FixedBuffer_128<long> m_size = default;
		[FieldOffset(128)]
		private long __overlap;
		[FieldOffset(128)]
		private readonly FixedBuffer_128<long> m_sizeProd = default;
		[FieldOffset(128 * 2)]
		private readonly long m_length;
		[FieldOffset(128 * 2 + sizeof(long))]
		private FixedBuffer_32<char> m_labels = default;
		[FieldOffset(128 * 2 + sizeof(long) + 32)]
		private readonly int m_rank = 0;
		// this defines extra 128 + 128 + 8 + 32 + 4 == 300 bytes

		/// <summary>
		/// Get the rank of this tensor as a <see cref="int"/>
		/// </summary>
		public override int Rank {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.m_rank;
		}

		/// <summary>
		/// Get the size of this tensor as a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/>
		/// </summary>
		public override ReadOnlySpan<long> Size {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.m_size.AsSpan(this.m_rank);
		}

		/// <summary>
		/// Get the (both end inclusive) accumulated product of the <see cref="AbstractArray{T}.Size"/> of this tensor
		/// </summary>
		/// <remarks>The first element is 1, the last element is <see cref="AbstractArray{T}.Length"/> and the size == <see cref="Rank"/> + 1</remarks>
		public ReadOnlySpan<long> SizeProd {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => MemoryMarshal.CreateReadOnlySpan(ref this.__overlap, this.m_rank + 1);
		}

		/// <summary>
		/// Construct a <see cref="BaseTensor{T}"/> by preallocated <paramref name="values"/> and the given <paramref name="size"/>
		/// </summary>
		/// <param name="values">The preallocated <see cref="Storage{T}"/> of the value array</param>
		/// <param name="size">The presenting size of the tensor</param>
		/// <param name="labels">The presenting labels of each dimension of this tensor, an empty one means auto generate as <c>{'a', 'b', ...}</c></param>
		/// <param name="actualLength">The actual length of the <paramref name="values"/>, default 0 means the length of it</param>
		/// <exception cref="ArgumentException">If <paramref name="labels"/>'s length is neither 0 nor the same as the rank</exception>
		protected BaseTensor(Storage<T> values, ReadOnlySpan<long> size, ReadOnlySpan<char> labels, long actualLength = 0) : base(values, size.Prod(), actualLength)
		{
			if (size.Length == 0)
				throw new ArgumentNullException(nameof(size));
			if (size.Length > 16)
				throw new NotSupportedException(Resources.Parameter.WrongSize);
			if (size.Length == 1 && size[0] == 0)
				return;
			if (size.Any(static s => s <= 0))
				throw new ArgumentOutOfRangeException(nameof(size), size.IndexOf(static s => s <= 0), Resources.Parameter.MustPositive);
			if (!labels.IsEmpty && labels.Length != size.Length)
				throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(labels));
			// get real labels
			Span<char> span = stackalloc char[size.Length];
			if (labels.IsEmpty)
				span.FillWithLabel();
			else
				labels.CopyTo(span);
			// set members
			this.m_rank = size.Length;
			this.m_labels.CopyFromSpan(span);
			var prod = this.m_sizeProd.AsSpan(this.m_rank);
			this.Size.AccumulateProd(result: prod, inclusive: true);
			if (this.m_rank < 16)
			{
				prod[this.m_rank] = this.Length;
			}
			else
			{
				this.m_length = this.Length;
			}
		}
		#endregion

		#region tensor label
		/// <summary>
		/// Get or set the label array as a <see cref="ReadOnlySpan{T}"/> of <see cref="char"/> used to mark each index of this tensor
		/// </summary>
		/// <exception cref="ArgumentException">If the setting value's length is not the same as the <see cref="AbstractArray{T}.Rank"/></exception>
		public ReadOnlySpan<char> Labels {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.m_labels.AsSpan(this.Rank);
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set {
				if (value.Length != this.Rank)
					throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(value));
				this.m_labels.CopyFromSpan(value);
			}
		}

		/// <summary>
		/// Get the label at rank <paramref name="index"/>
		/// </summary>
		/// <param name="index">The index of the rank whose label will be obtained</param>
		/// <returns>The <see cref="char"/> label at <paramref name="index"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="index"/> is out of range of <see cref="AbstractArray{T}.Rank"/></exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public char GetLabel(int index)
		{
			if (index < 0 || index >= this.Rank)
				throw new ArgumentOutOfRangeException(nameof(index), index, Resources.Parameter.InvalidValue);
			return this.m_labels[index];
		}

		/// <summary>
		/// Set the label at rank <paramref name="index"/> to <paramref name="value"/>
		/// </summary>
		/// <param name="index">The index of the rank whose label will be set</param>
		/// <param name="value">The <see cref="char"/> label at <paramref name="index"/> to set</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="index"/> is out of range of <see cref="AbstractArray{T}.Rank"/></exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void SetLabel(int index, char value)
		{
			if (index < 0 || index >= this.Rank)
				throw new ArgumentOutOfRangeException(nameof(index), index, Resources.Parameter.InvalidValue);
			this.m_labels[index] = value;
		}

		/// <summary>
		/// Set the label(s) used to mark each index of this tensor
		/// </summary>
		/// <param name="labels">The label(s) to set as an array of <see cref="char"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="labels"/> is null or empty</exception>
		/// <exception cref="ArgumentException">If the length of <paramref name="labels"/> is not the same as the <see cref="AbstractArray{T}.Rank"/></exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void SetLabels(params char[] labels)
		{
			if (labels.Length != this.Rank)
				throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(labels));
			this.m_labels.CopyFromSpan(labels);
		}
		#endregion

		#region reshape
		/// <summary>
		/// Reshape the array to a tensor with dimensionality = <paramref name="size"/>.
		/// </summary>
		/// <param name="size">The new size/dimensionality with at most one or zero uncertain dimension indicated by a non-positive number.</param>
		/// <returns>The reshaped tensor</returns>
		public override BaseTensor<T> ToTensor(ReadOnlySpan<long> size)
		{
			Span<long> newSize = stackalloc long[size.Length];
			size.CopyTo(newSize);
			CheckSize(this, newSize);
			if (newSize.SequenceEqual(this.Size))
				return this;
			else
				return TensorReshape(newSize);
		}

		/// <summary>
		/// When implemented by a derived class, reshape this tensor to another tensor with the given <paramref name="newSize"/> 
		/// </summary>
		/// <param name="newSize">The new size of the tensor as a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/></param>
		/// <returns>The reshaped tensor, may be a referenced one of this tensor</returns>
		public abstract BaseTensor<T> TensorReshape(ReadOnlySpan<long> newSize);
		#endregion

		#region indexing
		#region element indexing
		/// <summary>
		/// The basic indexed getter and setter of this tensor
		/// </summary>
		/// <param name="indices">The position indicated by an array of <see cref="Index"/> to be checked</param>
		/// <returns>The element at <paramref name="indices"/></returns>
		/// <exception cref="ArgumentException">If <paramref name="indices"/>'s length is not the same as the rank</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="indices"/> is out of range</exception>
		public T this[params Index[] indices] {
			get {
				Span<long> ind = stackalloc long[this.Rank];
				this.GetIndex(ind, indices);
				return this[ind];
			}
			set {
				Span<long> ind = stackalloc long[this.Rank];
				this.GetIndex(ind, indices);
				this[ind] = value;
			}
		}

		/// <summary>
		/// When implemented by a derived class, provide the basic indexed getter and setter of this tensor
		/// </summary>
		/// <param name="indices">The position indicated by a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/> to be checked</param>
		/// <returns>The element at <paramref name="indices"/></returns>
		/// <exception cref="ArgumentException">If <paramref name="indices"/>'s length is not the same as the rank</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="indices"/> is out of range</exception>
		public abstract T this[ReadOnlySpan<long> indices] { get; set; }

		/// <summary>
		/// Check whether the given <paramref name="indices"/> is out of range of this tensor
		/// </summary>
		/// <param name="indices">The indices as a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/> to be checked</param>
		/// <returns>The equivalent total offset of the given position compared to the <see cref="ValueArray{T}.Storage"/></returns>
		/// <exception cref="ArgumentException">If <paramref name="indices"/>'s length is not the same as the rank</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="indices"/> is out of range</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected long CheckIndex(ReadOnlySpan<long> indices) => this.CheckIndex(indices, this.SizeProd);

		/// <summary>
		/// Check whether the given <paramref name="indices"/> is out of range of this tensor
		/// </summary>
		/// <param name="indices">The indices as a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/> to be checked</param>
		/// <param name="outerSizeProd">The (inclusive) accumulated product of the outer size (i.e. the strides of all dimensions)</param>
		/// <returns>The equivalent total offset of the given position compared to the <see cref="ValueArray{T}.Storage"/></returns>
		/// <exception cref="ArgumentException">If <paramref name="indices"/>'s length is not the same as the rank</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="indices"/> is out of range</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected long CheckIndex(ReadOnlySpan<long> indices, ReadOnlySpan<long> outerSizeProd)
		{
			int rank = this.Rank;
			var size = this.Size;
			if (indices.Length != rank)
				throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(indices));
			long offset = 0;
			for (int i = 0; i < rank; i++)
			{
				if (indices[i] < 0 || indices[i] >= size[i])
					throw new ArgumentOutOfRangeException(nameof(indices), indices[i], Resources.Parameter.InvalidValue);
				offset += outerSizeProd[i] * indices[i];
			}
			return offset;
		}
		#endregion

		#region range indexing
		/// <summary>
		/// Check whether the given ranges indicated by <paramref name="offsets"/> and <paramref name="lengths"/> are out of range of this tensor
		/// </summary>
		/// <param name="offsets">The starting offset indices as a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/> to be checked</param>
		/// <param name="lengths">The lengths as a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/> to be checked</param>
		/// <returns>The equivalent total offset of the position indicated by <paramref name="offsets"/> compared to the <see cref="ValueArray{T}.Storage"/></returns>
		/// <exception cref="ArgumentException">If <paramref name="offsets"/> and/or <paramref name="lengths"/>'s length is not the same as the rank</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offsets"/> and/or <paramref name="lengths"/> is out of range</exception>
		protected long CheckRange(ReadOnlySpan<long> offsets, ReadOnlySpan<long> lengths) => this.CheckRange(offsets, lengths, this.SizeProd);

		/// <summary>
		/// Check whether the given ranges indicated by <paramref name="offsets"/> and <paramref name="lengths"/> are out of range of this tensor
		/// </summary>
		/// <param name="offsets">The starting offset indices as a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/> to be checked</param>
		/// <param name="lengths">The lengths as a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/> to be checked</param>
		/// <param name="outerSizeProd">The (inclusive) accumulated product of the outer size (i.e. the strides of all dimensions)</param>
		/// <returns>The equivalent total offset of the position indicated by <paramref name="offsets"/> compared to the <see cref="ValueArray{T}.Storage"/></returns>
		/// <exception cref="ArgumentException">If <paramref name="offsets"/> and/or <paramref name="lengths"/>'s length is not the same as the rank</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offsets"/> and/or <paramref name="lengths"/> is out of range</exception>
		protected long CheckRange(ReadOnlySpan<long> offsets, ReadOnlySpan<long> lengths, ReadOnlySpan<long> outerSizeProd)
		{
			int rank = this.Rank;
			var size = this.Size;
			if (offsets.Length != rank)
				throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(offsets));
			if (lengths.Length != rank)
				throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(lengths));
			long offset = 0;
			for (int i = 0; i < rank; i++)
			{
				if (offsets[i] < 0 || offsets[i] >= size[i])
					throw new ArgumentOutOfRangeException(nameof(offsets), offsets[i], Resources.Parameter.InvalidValue);
				if (lengths[i] <= 0 || offsets[i] + lengths[i] >= size[i])
					throw new ArgumentOutOfRangeException(nameof(lengths), lengths[i], Resources.Parameter.InvalidValue);
				offset += outerSizeProd[i] * offsets[i];
			}
			return offset;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void GetIndex(Span<long> ind, Index[] indices)
		{
			int rank = this.Rank;
			var size = this.Size;
			if (indices is null || indices.Length != rank)
				throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(indices));
			for (int i = 0; i < rank; i++)
			{
				ind[i] = indices[i].GetPosition(size[i]);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void GetRange(Span<long> off, Span<long> len, Range[] ranges)
		{
			int rank = this.Rank;
			var size = this.Size;
			if (ranges is null || ranges.Length != rank)
				throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(ranges));
			for (int i = 0; i < rank; i++)
			{
				(off[i], len[i]) = ranges[i].GetOffsetAndCount(size[i]);
			}
		}

		/// <summary>
		/// Get or set a sub-tensor (of same rank) indicated by the given <paramref name="ranges"/>
		/// </summary>
		/// <param name="ranges">The array of <see cref="Range"/> to indicate the target sub-tensor location and size compared to this tensor at each dimension</param>
		/// <exception cref="ArgumentNullException">If the input value is null or empty</exception>
		/// <exception cref="ArgumentException">If <paramref name="ranges"/> and/or value's size is not the same as the rank</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="ranges"/> is out of range</exception>
		public BaseTensor<T> this[params Range[] ranges] {
			get {
				Span<long> off = stackalloc long[this.Rank];
				Span<long> len = stackalloc long[this.Rank];
				this.GetRange(off, len, ranges);
				return this.GetSlice(off, len);
			}
			set {
				if (value is null || !value.IsValid())
					throw new ArgumentNullException(nameof(value));
				Span<long> off = stackalloc long[this.Rank];
				Span<long> len = stackalloc long[this.Rank];
				this.GetRange(off, len, ranges);
				if (!len.SequenceEqual(value.Size, static (a, b) => a <= b))
					throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(value));
				this.SetSlice(off, len, value);
			}
		}

		/// <summary>
		/// When implemented by a derived class, get the sub-tensor (of same rank) indicated by the given starting <paramref name="offsets"/> and <paramref name="lengths"/>
		/// </summary>
		/// <param name="offsets">The starting offsets of the target sub-tensor compared to this tensor at each dimension, in <typeparamref name="T"/></param>
		/// <param name="lengths">The lengths of the target sub-tensor at each dimension, in <typeparamref name="T"/></param>
		/// <returns>The sub-tensor indicated by <paramref name="offsets"/> and <paramref name="lengths"/>. Shall be a referenced tensor if possible.</returns>
		/// <exception cref="ArgumentException">If <paramref name="offsets"/> and/or <paramref name="lengths"/>'s length is not the same as the rank</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offsets"/> and/or <paramref name="lengths"/> is out of range</exception>
		public abstract BaseTensor<T> GetSlice(ReadOnlySpan<long> offsets, ReadOnlySpan<long> lengths);

		/// <summary>
		/// When implemented by a derived class, get the sub-tensor (of same rank) indicated by the given starting <paramref name="offsets"/> and <paramref name="lengths"/> and copy it to <paramref name="overwrite"/>
		/// </summary>
		/// <param name="offsets">The starting offsets of the target sub-tensor compared to this tensor at each dimension, in <typeparamref name="T"/></param>
		/// <param name="lengths">The lengths of the target sub-tensor at each dimension, in <typeparamref name="T"/></param>
		/// <param name="overwrite">The tensor to be overwritten by the sub-tensor</param>
		/// <exception cref="ArgumentNullException">If <paramref name="overwrite"/> is null or empty</exception>
		/// <exception cref="ArgumentException">If <paramref name="offsets"/> and/or <paramref name="lengths"/>'s length is not the same as the rank; or <paramref name="overwrite"/> cannot be overwritten</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offsets"/> and/or <paramref name="lengths"/> is out of range</exception>
		public abstract void GetSlice(ReadOnlySpan<long> offsets, ReadOnlySpan<long> lengths, BaseTensor<T> overwrite);

		/// <summary>
		/// When implemented by a derived class, set the sub-tensor (of same rank) indicated by the given starting <paramref name="offsets"/> and the size of <paramref name="value"/> to the underlying tensor of <paramref name="value"/>
		/// </summary>
		/// <param name="offsets">The starting offsets of the target sub-tensor compared to this tensor at each dimension, in <typeparamref name="T"/></param>
		/// <param name="lengths">The lengths of the target sub-tensor at each dimension, in <typeparamref name="T"/></param>
		/// <param name="value">The tensor to set</param>
		/// <exception cref="ArgumentNullException">If <paramref name="value"/> is null or empty</exception>
		/// <exception cref="ArgumentException">If <paramref name="offsets"/> and/or <paramref name="value"/>'s size is not the same as the rank</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offsets"/> and/or <paramref name="value"/>'s size is out of range</exception>
		public abstract void SetSlice(ReadOnlySpan<long> offsets, ReadOnlySpan<long> lengths, BaseTensor<T> value);
		#endregion

		#region first few dimensions indexing
		/// <summary>
		/// Get or set the (full) sub-tensor of rank <paramref name="n"/> located by the given <paramref name="restIndices"/> of length <c>(<see cref="AbstractArray{T}.Rank">rank</see> - <paramref name="n"/>)</c>.
		/// </summary>
		/// <param name="n">The first <paramref name="n"/> dimensions to get/set</param>
		/// <param name="restIndices">The position of the target sub-tensor at the rest (<see cref="AbstractArray{T}.Rank">rank</see> - <paramref name="n"/>) dimensions</param>
		/// <returns>The (full) sub-tensor of the first <paramref name="n"/> dimensions</returns>
		public BaseTensor<T> this[byte n, params Index[] restIndices] {
			get {
				Span<long> rest = stackalloc long[restIndices.Length];
				this.CheckFirstDims(n, restIndices, rest);
				return this.GetFirstDims(n, rest);
			}
			set {
				Span<long> rest = stackalloc long[restIndices.Length];
				this.CheckFirstDims(n, restIndices, rest);
				this.SetFirstDims(n, rest, value);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void CheckFirstDims(byte n, Index[] restIndices, Span<long> rest)
		{
			int rank = this.Rank, len = restIndices.Length;
			if (n >= rank - 1)
				throw new ArgumentOutOfRangeException(nameof(n), n, Resources.Parameter.InvalidValue);
			if (len + n != rank)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(restIndices));
			var size = this.Size;
			for (int i = 0; i < len; i++)
			{
				rest[i] = restIndices[i].GetPosition(size[n + i]);
			}
		}

		/// <summary>
		/// Check whether the given first-few-dimension(s) taking indicated by <paramref name="n"/>, <paramref name="restIndices"/>, <paramref name="offsets"/> and <paramref name="lengths"/> are valid and put the overall offsets and lengths to <paramref name="allOffsets"/> and <paramref name="allLengths"/>.
		/// </summary>
		/// <param name="n">The first <paramref name="n"/> dimensions to take</param>
		/// <param name="restIndices">The position of the target sub-tensor at the rest (<see cref="AbstractArray{T}.Rank">rank</see> - <paramref name="n"/>) dimensions</param>
		/// <param name="offsets">The starting offsets of the target sub-tensor compared to this tensor at the first <paramref name="n"/> dimensions. Default (an empty one) means all zeros.</param>
		/// <param name="lengths">The lengths of the target sub-tensor at the first <paramref name="n"/> dimensions. Default (an empty one) means the max possible values.</param>
		/// <param name="allOffsets">The output overall offsets of all dimensions, must be of size == rank</param>
		/// <param name="allLengths">The output overall lengths of all dimensions, must be of size == rank</param>
		/// <returns>The equivalent total offset of the position indicated by (at exit) <paramref name="allOffsets"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="n"/> ≤ 0 or <paramref name="n"/> ≥ <see cref="AbstractArray{T}.Rank">rank</see> - 1; or any of <paramref name="offsets"/> and <paramref name="lengths"/> is out of range</exception>
		/// <exception cref="ArgumentException">If the length of <paramref name="restIndices"/> is not (<see cref="AbstractArray{T}.Rank">rank</see> - <paramref name="n"/>)</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected long CheckFirstDims(int n, ReadOnlySpan<long> restIndices, ReadOnlySpan<long> offsets, ReadOnlySpan<long> lengths, Span<long> allOffsets, Span<long> allLengths)
		{
			int rank = this.Rank;
			if (n <= 0 || n >= rank - 1)
				throw new ArgumentOutOfRangeException(nameof(n), n, Resources.Parameter.InvalidValue);
			if (restIndices.Length + n != rank)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(restIndices));

			restIndices.CopyTo(allOffsets[n..]);
			allLengths[n..].Fill(1);
			if (!offsets.IsEmpty)
			{
				if (offsets.Length != n)
					throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(offsets));
				offsets.CopyTo(allOffsets[..n]);
			}
			if (!lengths.IsEmpty)
			{
				if (lengths.Length != n)
					throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(lengths));
				lengths.CopyTo(allLengths[..n]);
			}
			else
			{
				var size = this.Size;
				for (int i = 0; i < n; i++)
				{
					allLengths[i] = size[i] - allOffsets[i];
				}
			}
			// check ranges and return
			return CheckRange(allOffsets, allLengths);
		}

		/// <summary>
		/// When implemented by a derived class, get the sub-tensor of rank <paramref name="n"/> with <paramref name="offsets"/> and <paramref name="lengths"/> compared to the sub-tensor located by the given <paramref name="restIndices"/> of length <c>(<see cref="AbstractArray{T}.Rank">rank</see> - <paramref name="n"/>)</c>.
		/// </summary>
		/// <param name="n">The first <paramref name="n"/> dimensions to get</param>
		/// <param name="restIndices">The position of the target sub-tensor at the rest (<see cref="AbstractArray{T}.Rank">rank</see> - <paramref name="n"/>) dimensions</param>
		/// <param name="offsets">The starting offsets of the target sub-tensor compared to this tensor at the first <paramref name="n"/> dimensions. Default (an empty one) means all zeros.</param>
		/// <param name="lengths">The lengths of the target sub-tensor at the first <paramref name="n"/> dimensions. Default (an empty one) means the max possible values.</param>
		/// <returns>The sub-tensor at the first <paramref name="n"/> dimensions</returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="n"/> ≤ 0 or <paramref name="n"/> ≥ <see cref="AbstractArray{T}.Rank">rank</see> - 1; or any of <paramref name="offsets"/> and <paramref name="lengths"/> is out of range</exception>
		/// <exception cref="ArgumentException">If the length of <paramref name="restIndices"/> is not (<see cref="AbstractArray{T}.Rank">rank</see> - <paramref name="n"/>)</exception>
		public abstract BaseTensor<T> GetFirstDims(int n, ReadOnlySpan<long> restIndices, ReadOnlySpan<long> offsets = default, ReadOnlySpan<long> lengths = default);

		/// <summary>
		/// When implemented by a derived class, set the sub-tensor of rank <paramref name="n"/> with <paramref name="offsets"/> and <paramref name="lengths"/> compared to the sub-tensor located by the given <paramref name="restIndices"/> of length <c>(<see cref="AbstractArray{T}.Rank">rank</see> - <paramref name="n"/>)</c> to the given <paramref name="value"/>.
		/// </summary>
		/// <param name="n">The first <paramref name="n"/> dimensions to get</param>
		/// <param name="restIndices">The position of the target sub-tensor at the rest (<see cref="AbstractArray{T}.Rank">rank</see> - <paramref name="n"/>) dimensions</param>
		/// <param name="value">The dense tensor to set</param>
		/// <param name="offsets">The starting offsets of the target sub-tensor compared to this tensor at the first <paramref name="n"/> dimensions. Default (an empty one) means all zeros.</param>
		/// <param name="lengths">The lengths of the target sub-tensor at the first <paramref name="n"/> dimensions. Default (an empty one) means the max possible values.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="value"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="n"/> ≤ 0 or <paramref name="n"/> ≥ <see cref="AbstractArray{T}.Rank">rank</see> - 1; or any of <paramref name="offsets"/> and <paramref name="lengths"/> is out of range</exception>
		/// <exception cref="ArgumentException">If the length of <paramref name="restIndices"/> is not (<see cref="AbstractArray{T}.Rank">rank</see> - <paramref name="n"/>); or <paramref name="value"/> cannot be used as the set parameter</exception>
		public abstract void SetFirstDims(int n, ReadOnlySpan<long> restIndices, BaseTensor<T> value, ReadOnlySpan<long> offsets = default, ReadOnlySpan<long> lengths = default);
		#endregion
		#endregion

		#region tensor algebra abstract methods
		/// <summary>
		/// When implemented by a derived class, compute the tensor reduction (self partial summation) of this tensor under the given <paramref name="order"/>.
		/// </summary>
		/// <param name="order">The given <see cref="TensorOrder"/> to indicate which part(s) of dimension(s) to sum, its order will be ignored</param>
		/// <param name="scalar">The scalar to multiply to the result</param>
		/// <returns>The reduction result as a new <see cref="BaseTensor{T}"/></returns>
		/// <exception cref="ArgumentException">If <paramref name="order"/> does not indicate a partial permutation order</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="scalar"/> is 0</exception>
		public abstract BaseTensor<T> Reduce(TensorOrder order, T scalar);

		/// <summary>
		/// When implemented by a derived class, compute the tensor permutation of this tensor under the given <paramref name="order"/>.
		/// </summary>
		/// <param name="order">The given <see cref="TensorOrder"/> to indicate the permutation order</param>
		/// <param name="scalar">The scalar to multiply to the result</param>
		/// <returns>The permutation result as a new <see cref="BaseTensor{T}"/></returns>
		/// <exception cref="ArgumentException">If <paramref name="order"/> does not indicate a full permutation order</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="scalar"/> is 0</exception>
		public abstract BaseTensor<T> Permute(TensorOrder order, T scalar);

		/// <summary>
		/// When implemented by a derived class, compute the tensor contraction of this tensor and the <paramref name="other"/> tensor using their .
		/// </summary>
		/// <param name="other">The other tensor to perform the contraction with</param>
		/// <param name="scalar">The scalar to multiply to the result</param>
		/// <param name="outputLabels">The desired output tensor's labels as a <see cref="ReadOnlySpan{T}"/> of <see cref="char"/>. Default (empty) means simple union of the labels of this tensor and the <paramref name="other"/> tensor.</param>
		/// <returns>The contraction result as a new <see cref="BaseTensor{T}"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="other"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="other"/>'s labels indicate that it cannot contract with this tensor</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="scalar"/> is 0</exception>
		public abstract BaseTensor<T> Contract(BaseTensor<T> other, T scalar, ReadOnlySpan<char> outputLabels = default);

		/// <summary>
		/// When implemented by a derived class, compute the tensor point-wise addition of this tensor and the <paramref name="other"/> tensor.
		/// </summary>
		/// <param name="scalarThis">The scalar to multiply to this tensor</param>
		/// <param name="other">The other tensor to perform the contraction with</param>
		/// <param name="scalarOther">The scalar to multiply to the <paramref name="other"/> tensor</param>
		/// <returns>The addition result as a new <see cref="BaseTensor{T}"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="other"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="other"/> has different size than this one</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="scalarThis"/> or <paramref name="scalarOther"/> is 0</exception>
		public abstract BaseTensor<T> AddTensor(T scalarThis, BaseTensor<T> other, T scalarOther);
		#endregion

		#region operators
		/// <summary>
		/// Create a new <see cref="BaseTensor{T}"/> which is the which is the permutation of the given <paramref name="tensor"/> under <paramref name="order"/>.
		/// </summary>
		/// <param name="tensor">One original tensor as the left operand</param>
		/// <param name="order">The <see cref="TensorOrder"/> indicating the permutation order</param>
		/// <returns>A new <see cref="BaseTensor{T}"/> which is the permutation result of the given <paramref name="tensor"/> under <paramref name="order"/></returns>
		public static BaseTensor<T> operator ^(BaseTensor<T> tensor, TensorOrder order)
		{
			if (tensor is null || !tensor.IsValid())
				throw new ArgumentNullException(nameof(tensor));

			return tensor.Permute(order, Const<T>.One);
		}

		/// <summary>
		/// Create a new <see cref="BaseTensor{T}"/> which is the which is the tensor contraction of the given <paramref name="left"/> and <paramref name="right"/> tensors.
		/// </summary>
		/// <param name="left">One original tensor as the left operand</param>
		/// <param name="right">One original tensor as the right operand</param>
		/// <returns>A new <see cref="BaseTensor{T}"/> which is the contraction result of the given <paramref name="left"/> and <paramref name="right"/> tensors</returns>
		public static BaseTensor<T> operator *(BaseTensor<T> left, BaseTensor<T> right)
		{
			if (left is null || !left.IsValid())
				throw new ArgumentNullException(nameof(left));
			if (right is null || !right.IsValid())
				throw new ArgumentNullException(nameof(right));

			return left.Contract(right, Const<T>.One);
		}

		/// <summary>
		/// Create a new <see cref="BaseTensor{T}"/> which is the (point-wise) addition result of the given <paramref name="left"/> and <paramref name="right"/> tensors.
		/// </summary>
		/// <param name="left">One original tensor as the left operand</param>
		/// <param name="right">One original tensor as the right operand</param>
		/// <returns>A new <see cref="BaseTensor{T}"/> which is the addition result of the given <paramref name="left"/> and <paramref name="right"/> tensor</returns>
		public static BaseTensor<T> operator +(BaseTensor<T> left, BaseTensor<T> right)
		{
			if (left is null || !left.IsValid())
				throw new ArgumentNullException(nameof(left));
			if (right is null || !right.IsValid())
				throw new ArgumentNullException(nameof(right));

			return left.AddTensor(Const<T>.One, right, Const<T>.One);
		}

		/// <summary>
		/// Create a new <see cref="BaseTensor{T}"/> which is the (point-wise) subtraction result of the given <paramref name="left"/> and <paramref name="right"/> tensors.
		/// </summary>
		/// <param name="left">One original tensor as the left operand</param>
		/// <param name="right">One original tensor as the right operand</param>
		/// <returns>A new <see cref="BaseTensor{T}"/> which is the addition result of the given <paramref name="left"/> and <paramref name="right"/> tensor</returns>
		public static BaseTensor<T> operator -(BaseTensor<T> left, BaseTensor<T> right)
		{
			if (left is null || !left.IsValid())
				throw new ArgumentNullException(nameof(left));
			if (right is null || !right.IsValid())
				throw new ArgumentNullException(nameof(right));

			return left.AddTensor(Const<T>.One, right, Const<T>.MinusOne);
		}

		/// <summary>
		/// Create a new <see cref="BaseTensor{T}"/> which is the (point-wise) multiplication result of the given <paramref name="tensor"/> and <paramref name="scalar"/>
		/// </summary>
		/// <param name="tensor">The original tensor to multiply</param>
		/// <param name="scalar">The scalar of type <typeparamref name="T"/> to multiply</param>
		/// <returns>A new <see cref="BaseTensor{T}"/> which is the multiplication result of the given <paramref name="tensor"/> and <paramref name="scalar"/></returns>
		public static BaseTensor<T> operator *(BaseTensor<T> tensor, T scalar)
		{
			if (tensor is null || !tensor.IsValid())
				throw new ArgumentNullException(nameof(tensor));

			return tensor.ApplyToClone(v => v.Scale(scalar));
		}

		/// <summary>
		/// Create a new <see cref="BaseTensor{T}"/> which is the negation result of the given <paramref name="tensor"/>
		/// </summary>
		/// <param name="tensor">The original tensor to negate</param>
		/// <returns>A new <see cref="BaseTensor{T}"/> which is the negation result of the given <paramref name="tensor"/></returns>
		public static BaseTensor<T> operator -(BaseTensor<T> tensor) => tensor * Const<T>.MinusOne;

		/// <summary>
		/// Create a new <see cref="BaseTensor{T}"/> which is the multiplication result of the given <paramref name="tensor"/> and <paramref name="scalar"/>
		/// </summary>
		/// <param name="tensor">The original tensor to multiply</param>
		/// <param name="scalar">The scalar of type <typeparamref name="T"/> to multiply</param>
		/// <returns>A new <see cref="BaseTensor{T}"/> which is the multiplication result of the given <paramref name="tensor"/> and <paramref name="scalar"/></returns>
		public static BaseTensor<T> operator *(T scalar, BaseTensor<T> tensor) => tensor * scalar;

		/// <summary>
		/// Create a new <see cref="BaseTensor{T}"/> which is the division result of the given <paramref name="tensor"/> and <paramref name="scalar"/>
		/// </summary>
		/// <param name="tensor">The original tensor to be divided</param>
		/// <param name="scalar">The scalar of type <typeparamref name="T"/> to divide</param>
		/// <returns>A new <see cref="BaseTensor{T}"/> which is the multiplication result of the given <paramref name="tensor"/> and <paramref name="scalar"/></returns>
		public static BaseTensor<T> operator /(BaseTensor<T> tensor, T scalar) => tensor * scalar.NativeReciprocal();
		#endregion

		#region serialization
		/// <summary>
		/// The presenting name of the <see cref="Labels"/>
		/// </summary>
		public const string LabelsName = nameof(Labels);

		/// <summary>
		/// When implemented by a derived class, get other requisite informations for re-constructing the array of that derived class type. The default implementation simply returns the <see cref="Labels"/>.
		/// </summary>
		/// <returns>Other requisite informations used to re-construct this array</returns>
		public override IReadOnlyDictionary<string, object> GetMetaData() => new Dictionary<string, object>(1)
		{
			[LabelsName] = this.Labels.ToArray()
		};
		#endregion
	}
}
