using System.Runtime.CompilerServices;

using Althea.Helpers;


namespace Althea.Array;

/// <summary>
/// The interface for all tensors with labeled dimensions.
/// </summary>
/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
/// <typeparam name="TSelf">The concrete type that implements <see cref="IBaseTensor{T, TSelf}"/></typeparam>
public interface IBaseTensor<T, TSelf> : ILabeledTensor<T>, IValueArray<T, TSelf>
	where T : unmanaged, IBaseNumber<T>
	where TSelf : class, IBaseTensor<T, TSelf>
{
	#region basic
	/// <summary>
	/// When implemented by a derived class, get the (both end inclusive) accumulated product of the <see cref="ILabeledTensor{T}.Size"/> of this tensor.
	/// </summary>
	/// <remarks>The first element is 1, the last element is <see cref="IValueArray{T, TSelf}.Length"/> and its size == <see cref="ILabeledTensor{T}.Rank"/> + 1</remarks>
	protected ReadOnlySpan<long> SizeProd { get; }
	#endregion

	#region element indexing
	/// <summary>
	/// The basic indexed getter and setter of this tensor.
	/// </summary>
	/// <param name="indices">The position indicated by an array of <see cref="Index"/> to be checked</param>
	/// <returns>The element at <paramref name="indices"/>.</returns>
	/// <exception cref="ArgumentException">If <paramref name="indices"/>'s length is not the same as the rank</exception>
	/// <exception cref="ArgumentOutOfRangeException">If <paramref name="indices"/> is out of range</exception>
	public T this[params Index[] indices]
	{
		get
		{
			Span<long> ind = stackalloc long[this.Rank];
			this.GetIndex(ind, indices);
			return this[ind];
		}
		set
		{
			Span<long> ind = stackalloc long[this.Rank];
			this.GetIndex(ind, indices);
			this[ind] = value;
		}
	}

	/// <summary>
	/// When implemented by a derived class, provide the basic indexed getter and setter of this tensor.
	/// </summary>
	/// <param name="indices">The position indicated by a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/> to be checked</param>
	/// <returns>The element at <paramref name="indices"/>.</returns>
	/// <exception cref="ArgumentException">If <paramref name="indices"/>'s length is not the same as the rank</exception>
	/// <exception cref="ArgumentOutOfRangeException">If <paramref name="indices"/> is out of range</exception>
	T this[ReadOnlySpan<long> indices] { get; set; }

	/// <summary>
	/// Check whether the given <paramref name="indices"/> is out of range of <paramref name="tensor"/>.
	/// </summary>
	/// <param name="tensor">The tensor to be checked</param>
	/// <param name="indices">The indices as a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/> to be checked</param>
	/// <returns>The equivalent total offset of the given position.</returns>
	/// <exception cref="ArgumentException">If <paramref name="indices"/>'s length is not the same as the rank</exception>
	/// <exception cref="ArgumentOutOfRangeException">If <paramref name="indices"/> is out of range</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static long CheckIndex(TSelf tensor, ReadOnlySpan<long> indices)
	{
		int rank = tensor.Rank;
		var size = ((ILabeledTensor<T>)tensor).Size;
		if (indices.Length != rank)
			throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(indices));
		var outerSizeProd = tensor is IPitchedArray<T> p ? p.Strides : tensor.SizeProd;
		long offset = 0;
		for (int i = 0; i < rank; i++)
		{
			if (indices[i] < 0 || indices[i] >= size[i])
				throw new ArgumentOutOfRangeException(nameof(indices), indices[i], Resources.ParameterError.InvalidValue);
			offset += outerSizeProd[i] * indices[i];
		}
		return offset;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void GetIndex(Span<long> ind, Index[] indices)
	{
		int rank = this.Rank;
		var size = ((ILabeledTensor<T>)this).Size;
		if (indices is null || indices.Length != rank)
			throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(indices));
		for (int i = 0; i < rank; i++)
		{
			ind[i] = indices[i].GetPosition(size[i]);
		}
	}
	#endregion

	#region range indexing
	/// <summary>
	/// Check whether the given ranges indicated by <paramref name="offsets"/> and <paramref name="lengths"/> are out of range of <paramref name="tensor"/>.
	/// </summary>
	/// <param name="tensor">The tensor to be checked</param>
	/// <param name="offsets">The starting offset indices as a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/> to be checked</param>
	/// <param name="lengths">The lengths as a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/> to be checked</param>
	/// <param name="sub">The sub tensor to check which can be null to ignore checking</param>
	/// <returns>The equivalent total offset of the position indicated by <paramref name="offsets"/>.</returns>
	/// <exception cref="ArgumentException">If <paramref name="offsets"/> and/or <paramref name="lengths"/>'s length is not the same as the rank</exception>
	/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offsets"/> and/or <paramref name="lengths"/> is out of range</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static long CheckRange(TSelf tensor, ReadOnlySpan<long> offsets, ReadOnlySpan<long> lengths, ILabeledTensor<T>? sub = null)
	{
		int rank = tensor.Rank;
		var size = ((ILabeledTensor<T>)tensor).Size;
		if (offsets.Length != rank)
			throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(offsets));
		if (lengths.Length != rank)
			throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(lengths));
		var outerSizeProd = tensor is IPitchedArray<T> p ? p.Strides : tensor.SizeProd;
		long offset = 0;
		for (int i = 0; i < rank; i++)
		{
			if (offsets[i] < 0 || offsets[i] >= size[i])
				throw new ArgumentOutOfRangeException(nameof(offsets), offsets[i], Resources.ParameterError.InvalidValue);
			if (lengths[i] <= 0 || offsets[i] + lengths[i] >= size[i])
				throw new ArgumentOutOfRangeException(nameof(lengths), lengths[i], Resources.ParameterError.InvalidValue);
			offset += outerSizeProd[i] * offsets[i];
		}
		if (sub is not null)
		{
			if (sub.Rank != rank)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(sub));
			var sizeSub = sub.Size;
			for (int i = 0; i < rank; i++)
			{
				if (sizeSub[i] < lengths[i])
					throw new ArgumentOutOfRangeException(nameof(lengths), lengths[i], Resources.ParameterError.InvalidValue);
			}
		}
		return offset;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void GetRange(Span<long> off, Span<long> len, Range[] ranges)
	{
		int rank = this.Rank;
		var size = ((ILabeledTensor<T>)this).Size;
		if (ranges is null || ranges.Length != rank)
			throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(ranges));
		for (int i = 0; i < rank; i++)
		{
			(off[i], len[i]) = ranges[i].GetOffsetAndCount(size[i]);
		}
	}

	/// <summary>
	/// Get or set a sub-tensor (of same rank) indicated by the given <paramref name="ranges"/>.
	/// </summary>
	/// <param name="ranges">The array of <see cref="Range"/> to indicate the target sub-tensor location and size compared to this tensor at each dimension</param>
	/// <exception cref="ArgumentNullException">If the input value is null or empty</exception>
	/// <exception cref="ArgumentException">If <paramref name="ranges"/> and/or value's size is not the same as the rank</exception>
	/// <exception cref="ArgumentOutOfRangeException">If <paramref name="ranges"/> is out of range</exception>
	public TSelf this[params Range[] ranges]
	{
		get
		{
			Span<long> off = stackalloc long[this.Rank];
			Span<long> len = stackalloc long[this.Rank];
			this.GetRange(off, len, ranges);
			return this.GetSlice(off, len);
		}
		set
		{
			if (value is null || !value.IsValid())
				throw new ArgumentNullException(nameof(value));
			Span<long> off = stackalloc long[this.Rank];
			Span<long> len = stackalloc long[this.Rank];
			this.GetRange(off, len, ranges);
			if (!len.SequenceEqual(((ILabeledTensor<T>)value).Size))
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(value));
			this.SetSlice(off, len, value);
		}
	}

	/// <summary>
	/// When implemented by a derived class, get the sub-tensor (of same rank) indicated by the given starting <paramref name="offsets"/> and <paramref name="lengths"/>.
	/// </summary>
	/// <param name="offsets">The starting offsets of the target sub-tensor compared to this tensor at each dimension, in <typeparamref name="T"/></param>
	/// <param name="lengths">The lengths of the target sub-tensor at each dimension, in <typeparamref name="T"/></param>
	/// <returns>The sub-tensor indicated by <paramref name="offsets"/> and <paramref name="lengths"/>. Shall be a referenced tensor if possible.</returns>
	/// <exception cref="ArgumentException">If <paramref name="offsets"/> and/or <paramref name="lengths"/>'s length is not the same as the rank</exception>
	/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offsets"/> and/or <paramref name="lengths"/> is out of range</exception>
	TSelf GetSlice(ReadOnlySpan<long> offsets, ReadOnlySpan<long> lengths);

	/// <summary>
	/// When implemented by a derived class, get the sub-tensor (of same rank) indicated by the given starting <paramref name="offsets"/> and <paramref name="lengths"/> and overwrite the result to <paramref name="overwrite"/>.
	/// </summary>
	/// <param name="offsets">The starting offsets of the target sub-tensor compared to this tensor at each dimension, in <typeparamref name="T"/></param>
	/// <param name="lengths">The lengths of the target sub-tensor at each dimension, in <typeparamref name="T"/></param>
	/// <param name="overwrite">The sub-tensor to be overwritten</param>
	/// <exception cref="ArgumentException">If <paramref name="offsets"/> and/or <paramref name="lengths"/>'s length is not the same as the rank</exception>
	/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offsets"/> and/or <paramref name="lengths"/> is out of range</exception>
	void GetSlice(ReadOnlySpan<long> offsets, ReadOnlySpan<long> lengths, TSelf overwrite);

	/// <summary>
	/// When implemented by a derived class, set the sub-tensor (of same rank) indicated by the given starting <paramref name="offsets"/> and the size of <paramref name="value"/> to the underlying tensor of <paramref name="value"/>.
	/// </summary>
	/// <param name="offsets">The starting offsets of the target sub-tensor compared to this tensor at each dimension, in <typeparamref name="T"/></param>
	/// <param name="lengths">The lengths of the target sub-tensor at each dimension, in <typeparamref name="T"/></param>
	/// <param name="value">The tensor to set</param>
	/// <exception cref="ArgumentNullException">If <paramref name="value"/> is null or empty</exception>
	/// <exception cref="ArgumentException">If <paramref name="offsets"/> and/or <paramref name="value"/>'s size is not the same as the rank</exception>
	/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offsets"/> and/or <paramref name="value"/>'s size is out of range</exception>
	void SetSlice(ReadOnlySpan<long> offsets, ReadOnlySpan<long> lengths, TSelf value);
	#endregion

	#region first few dimensions indexing
	/// <summary>
	/// Get or set the (full) sub-tensor of rank <paramref name="n"/> located by the given <paramref name="restIndices"/> of length <c>(<see cref="ILabeledTensor{T}.Rank">rank</see> - <paramref name="n"/>)</c>.
	/// </summary>
	/// <param name="n">The first <paramref name="n"/> dimensions to get/set</param>
	/// <param name="restIndices">The position of the target sub-tensor at the rest (<see cref="ILabeledTensor{T}.Rank">rank</see> - <paramref name="n"/>) dimensions</param>
	/// <returns>The (full) sub-tensor of the first <paramref name="n"/> dimensions</returns>
	public TSelf this[byte n, params Index[] restIndices]
	{
		get
		{
			Span<long> rest = stackalloc long[restIndices.Length];
			this.CheckFirstDims(n, restIndices, rest);
			return this.GetFirstDims(n, rest);
		}
		set
		{
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
			throw new ArgumentOutOfRangeException(nameof(n), n, Resources.ParameterError.InvalidValue);
		if (len + n != rank)
			throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(restIndices));
		var size = ((ILabeledTensor<T>)this).Size;
		for (int i = 0; i < len; i++)
		{
			rest[i] = restIndices[i].GetPosition(size[n + i]);
		}
	}

	/// <summary>
	/// Check whether the given first-few-dimension(s) taking indicated by <paramref name="n"/>, <paramref name="restIndices"/>, <paramref name="offsets"/> and <paramref name="lengths"/> are valid and put the overall offsets and lengths to <paramref name="allOffsets"/> and <paramref name="allLengths"/>.
	/// </summary>
	/// <param name="tensor">The tensor to be checked</param>
	/// <param name="n">The first <paramref name="n"/> dimensions to take</param>
	/// <param name="restIndices">The position of the target sub-tensor at the rest (<see cref="ILabeledTensor{T}.Rank">rank</see> - <paramref name="n"/>) dimensions</param>
	/// <param name="offsets">The starting offsets of the target sub-tensor compared to this tensor at the first <paramref name="n"/> dimensions. Default (an empty one) means all zeros.</param>
	/// <param name="lengths">The lengths of the target sub-tensor at the first <paramref name="n"/> dimensions. Default (an empty one) means the max possible values.</param>
	/// <param name="allOffsets">The output overall offsets of all dimensions, must be of size == rank</param>
	/// <param name="allLengths">The output overall lengths of all dimensions, must be of size == rank</param>
	/// <param name="sub">The sub tensor to check which can be null to ignore checking</param>
	/// <returns>The equivalent total offset of the position indicated by (at exit) <paramref name="allOffsets"/></returns>
	/// <exception cref="ArgumentOutOfRangeException">If <paramref name="n"/> ≤ 0 or <paramref name="n"/> ≥ <see cref="ILabeledTensor{T}.Rank">rank</see> - 1; or any of <paramref name="offsets"/> and <paramref name="lengths"/> is out of range</exception>
	/// <exception cref="ArgumentException">If the length of <paramref name="restIndices"/> is not (<see cref="ILabeledTensor{T}.Rank">rank</see> - <paramref name="n"/>)</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static long CheckFirstDims(TSelf tensor, int n, ReadOnlySpan<long> restIndices, ReadOnlySpan<long> offsets, ReadOnlySpan<long> lengths, Span<long> allOffsets, Span<long> allLengths, ILabeledTensor<T>? sub = null)
	{
		int rank = tensor.Rank;
		if (n <= 0 || n >= rank - 1)
			throw new ArgumentOutOfRangeException(nameof(n), n, Resources.ParameterError.InvalidValue);
		if (restIndices.Length + n != rank)
			throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(restIndices));
		if (sub is not null && !sub.Size.SequenceEqual(((ILabeledTensor<T>)tensor).Size[..n]))
			throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(sub));

		restIndices.CopyTo(allOffsets[n..]);
		allLengths[n..].Fill(1);
		if (!offsets.IsEmpty)
		{
			if (offsets.Length != n)
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(offsets));
			offsets.CopyTo(allOffsets[..n]);
		}
		else
		{
			allOffsets[..n].Fill(0);
		}
		if (!lengths.IsEmpty)
		{
			if (lengths.Length != n)
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(lengths));
			lengths.CopyTo(allLengths[..n]);
		}
		else
		{
			var size = ((ILabeledTensor<T>)tensor).Size;
			for (int i = 0; i < n; i++)
			{
				allLengths[i] = size[i] - allOffsets[i];
			}
		}
		// check ranges and return
		return CheckRange(tensor, allOffsets, allLengths, sub);
	}

	/// <summary>
	/// When implemented by a derived class, get the sub-tensor of rank <paramref name="n"/> with <paramref name="offsets"/> and <paramref name="lengths"/> compared to the sub-tensor located by the given <paramref name="restIndices"/> of length <c>(<see cref="ILabeledTensor{T}.Rank">rank</see> - <paramref name="n"/>)</c>.
	/// </summary>
	/// <param name="n">The first <paramref name="n"/> dimensions to get</param>
	/// <param name="restIndices">The position of the target sub-tensor at the rest (<see cref="ILabeledTensor{T}.Rank">rank</see> - <paramref name="n"/>) dimensions</param>
	/// <param name="offsets">The starting offsets of the target sub-tensor compared to this tensor at the first <paramref name="n"/> dimensions. Default (an empty one) means all zeros.</param>
	/// <param name="lengths">The lengths of the target sub-tensor at the first <paramref name="n"/> dimensions. Default (an empty one) means the max possible values.</param>
	/// <returns>The sub-tensor at the first <paramref name="n"/> dimensions</returns>
	/// <exception cref="ArgumentOutOfRangeException">If <paramref name="n"/> ≤ 0 or <paramref name="n"/> ≥ <see cref="ILabeledTensor{T}.Rank">rank</see> - 1; or any of <paramref name="offsets"/> and <paramref name="lengths"/> is out of range</exception>
	/// <exception cref="ArgumentException">If the length of <paramref name="restIndices"/> is not (<see cref="ILabeledTensor{T}.Rank">rank</see> - <paramref name="n"/>)</exception>
	TSelf GetFirstDims(int n, ReadOnlySpan<long> restIndices, ReadOnlySpan<long> offsets = default, ReadOnlySpan<long> lengths = default);

	/// <summary>
	/// When implemented by a derived class, get the sub-tensor of rank <paramref name="n"/> with <paramref name="offsets"/> and <paramref name="lengths"/> compared to the sub-tensor located by the given <paramref name="restIndices"/> of length <c>(<see cref="ILabeledTensor{T}.Rank">rank</see> - <paramref name="n"/>)</c> and write to <paramref name="overwrite"/>.
	/// </summary>
	/// <param name="n">The first <paramref name="n"/> dimensions to get</param>
	/// <param name="restIndices">The position of the target sub-tensor at the rest (<see cref="ILabeledTensor{T}.Rank">rank</see> - <paramref name="n"/>) dimensions</param>
	/// <param name="overwrite">The sub-tensor to be overwritten</param>
	/// <param name="offsets">The starting offsets of the target sub-tensor compared to this tensor at the first <paramref name="n"/> dimensions. Default (an empty one) means all zeros.</param>
	/// <param name="lengths">The lengths of the target sub-tensor at the first <paramref name="n"/> dimensions. Default (an empty one) means the max possible values.</param>
	/// <returns>The sub-tensor at the first <paramref name="n"/> dimensions</returns>
	/// <exception cref="ArgumentOutOfRangeException">If <paramref name="n"/> ≤ 0 or <paramref name="n"/> ≥ <see cref="ILabeledTensor{T}.Rank">rank</see> - 1; or any of <paramref name="offsets"/> and <paramref name="lengths"/> is out of range</exception>
	/// <exception cref="ArgumentException">If the length of <paramref name="restIndices"/> is not (<see cref="ILabeledTensor{T}.Rank">rank</see> - <paramref name="n"/>)</exception>
	void GetFirstDims(int n, ReadOnlySpan<long> restIndices, TSelf overwrite, ReadOnlySpan<long> offsets = default, ReadOnlySpan<long> lengths = default);

	/// <summary>
	/// When implemented by a derived class, set the sub-tensor of rank <paramref name="n"/> with <paramref name="offsets"/> and <paramref name="lengths"/> compared to the sub-tensor located by the given <paramref name="restIndices"/> of length <c>(<see cref="ILabeledTensor{T}.Rank">rank</see> - <paramref name="n"/>)</c> to the given <paramref name="value"/>.
	/// </summary>
	/// <param name="n">The first <paramref name="n"/> dimensions to get</param>
	/// <param name="restIndices">The position of the target sub-tensor at the rest (<see cref="ILabeledTensor{T}.Rank">rank</see> - <paramref name="n"/>) dimensions</param>
	/// <param name="value">The dense tensor to set</param>
	/// <param name="offsets">The starting offsets of the target sub-tensor compared to this tensor at the first <paramref name="n"/> dimensions. Default (an empty one) means all zeros.</param>
	/// <param name="lengths">The lengths of the target sub-tensor at the first <paramref name="n"/> dimensions. Default (an empty one) means the max possible values.</param>
	/// <exception cref="ArgumentNullException">If <paramref name="value"/> is null or invalid</exception>
	/// <exception cref="ArgumentOutOfRangeException">If <paramref name="n"/> ≤ 0 or <paramref name="n"/> ≥ <see cref="ILabeledTensor{T}.Rank">rank</see> - 1; or any of <paramref name="offsets"/> and <paramref name="lengths"/> is out of range</exception>
	/// <exception cref="ArgumentException">If the length of <paramref name="restIndices"/> is not (<see cref="ILabeledTensor{T}.Rank">rank</see> - <paramref name="n"/>); or <paramref name="value"/> cannot be used as the set parameter</exception>
	void SetFirstDims(int n, ReadOnlySpan<long> restIndices, TSelf value, ReadOnlySpan<long> offsets = default, ReadOnlySpan<long> lengths = default);
	#endregion
}
