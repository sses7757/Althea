using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

using Althea.Arrays;
using Althea.Helpers;
using Althea.Linq;
using Althea.NativeTypes;
using Althea.TensorAlgebra;

using MEM = Althea.Storage.AbstractApi;
using TAD = Althea.TensorAlgebra.Dense.AbstractApi;


namespace Althea.Backend.Arrays
{
	/// <summary>
	/// The concrete dense tensor class with the only mutable <see cref="ValueArray{T}.Storage"/> that refers to the actual (pitched) data storage without any index storage.
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
	public class DenseTensor<T> : TensorBase<T>, IPitchedArray<T>, IKrylovVector<DenseTensor<T>, T> where T : unmanaged
	{
		#region basic
		private readonly FixedBuffer_128<long> m_outerSize = default;

		private readonly FixedBuffer_128<long> m_outerSizeProd = default;

		/// <summary>
		/// Get the pitch (in <typeparamref name="T"/>) of this array (the outer size at each dimension) as a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/>.
		/// </summary>
		public ReadOnlySpan<long> OuterSize => this.m_outerSize.AsSpan(this.Rank);

		/// <summary>
		/// Get the strides (the inclusive accumulated product of <see cref="OuterSize"/>) of this tensor at all dimensions as a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/>.
		/// </summary>
		public ReadOnlySpan<long> Strides => this.m_outerSizeProd.AsSpan(this.Rank);

		/// <summary>
		/// Get a <see cref="bool"/> indicating whether this tensor is actually pitched.
		/// </summary>
		public bool HasPitch {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => !this.Size.SequenceEqual(this.OuterSize);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static long GetActualLength(ReadOnlySpan<long> size, ReadOnlySpan<long> outerSize)
		{
			if (outerSize.IsEmpty)
				return 0;
			if (outerSize.Length != size.Length)
				throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(outerSize));
			int r = size.Length - 1;
			if (outerSize.Length != r + 1 || outerSize[r] < size[r])
				throw new ArgumentException(Resources.Parameter.InvalidValue, nameof(outerSize));

			long prodOuter = 1; bool allOnes = true;
			for (int i = r; i >= 0; i--)
			{
				if (allOnes && size[i] != 1)
				{
					prodOuter *= outerSize[i];
					allOnes = false;
				}
				else if (!allOnes)
				{
					prodOuter *= outerSize[i];
				}
			}
			long prod = 1;
			for (int i = 0; i < r; i++)
			{
				prodOuter -= prod * (outerSize[i] - outerSize[i]);
				prod *= outerSize[i];
			}
			return prodOuter;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void ModifySize(ReadOnlySpan<long> size, Span<long> outerSize)
		{
			int r = size.Length - 1;
			outerSize[r] = size[r];
			bool allOnes = true;
			for (int i = r; i >= 0; i--)
			{
				if (allOnes && size[i] != 1)
				{
					allOnes = false;
				}
				else if (allOnes)
				{
					outerSize[i] = size[i];
				}
			}
		}

		/// <summary>
		/// Create a <see cref="DenseTensor{T}"/> with given <paramref name="values"/>, presenting <paramref name="size"/>, actual <paramref name="outerSize"/> and <paramref name="labels"/>
		/// </summary>
		/// <param name="values">The preallocated <see cref="Storage{T}"/> of the value array</param>
		/// <param name="size">The presenting size of the tensor</param>
		/// <param name="outerSize">The outer size (actual lengths at all dimensions) of this tensor, default (an empty one) mean the same as <paramref name="size"/>. The last element will be replaced by the last element of <paramref name="size"/>.</param>
		/// <param name="labels">The presenting labels of each dimension of this tensor, default (an empty one) means auto generate as <c>{'a', 'b', ...}</c></param>
		/// <exception cref="ArgumentException">If <paramref name="labels"/> or <paramref name="outerSize"/>'s length is neither 0 nor the same as the rank; or the last of <paramref name="outerSize"/> is smaller than the last of <paramref name="size"/></exception>
		public DenseTensor(Storage<T> values, ReadOnlySpan<long> size, ReadOnlySpan<long> outerSize = default, ReadOnlySpan<char> labels = default) :
			base(values, size, labels, GetActualLength(size, outerSize))
		{
			if (outerSize.IsEmpty)
			{
				this.m_outerSize.CopyFromSpan(size);
			}
			else
			{
				Span<long> newOuterSize = stackalloc long[this.Rank];
				outerSize.CopyTo(newOuterSize);
				ModifySize(size, newOuterSize);
				this.m_outerSize.CopyFromSpan(newOuterSize);
			}
			var prod = this.m_outerSizeProd.AsSpan(this.Rank);
			this.OuterSize.AccumulateProd(result: prod, inclusive: true);
		}

		/// <summary>
		/// Create an empty
		/// </summary>
		public DenseTensor() : base(Storage<T>.Empty, stackalloc long[1], ReadOnlySpan<char>.Empty) { }
		#endregion

		#region clone related
		private Storage<T> ToContiguous()
		{
			if (this.HasPitch)
				return this.CopyToStorage();
			else
				return this.Storage;
		}

		private ActualStorage<T> CopyToStorage()
		{
			var size = this.Size;
			var storage = Storage<T>.Create(this.Storage[0].Location, this.Length);
			try
			{
				TAD.Permute<T>(new(this), new(storage, size, size), stackalloc int[this.Rank].FillWithRange(0));
				return storage;
			}
			catch (Exception)
			{
				storage?.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Deep clone the array, the mutable status will not be copied.
		/// </summary>
		/// <returns>The cloned array</returns>
		public override DenseTensor<T> Clone()
		{
			var size = this.Size;
			var storage = this.CopyToStorage();
			return new(storage, size, size, this.Labels);
		}

		/// <summary>
		/// Create a new array with same properties as this one while the underlying storages are not filled.
		/// </summary>
		/// <returns>The new array alike this one</returns>
		public override DenseTensor<T> NewArrayAlike()
		{
			var size = this.Size;
			var storage = Storage<T>.Create(this.Storage[0].Location, this.Length);
			return new(storage, size, size, this.Labels);
		}

		/// <summary>
		/// Create a new array with same properties as this one while the underlying storages are not filled and the data type is changed to <typeparamref name="TOut"/>.
		/// </summary>
		/// <typeparam name="TOut">Any unmanaged struct as the new data type</typeparam>
		/// <returns>The new array alike this one</returns>
		public override DenseTensor<TOut> NewArrayAlike<TOut>()
		{
			var size = this.Size;
			var storage = Storage<TOut>.Create(this.Storage[0].Location, this.Length);
			return new(storage, size, size, this.Labels);
		}
		#endregion

		#region reshape
		/// <summary>
		/// Reshape this array to a vector
		/// </summary>
		/// <returns>The referenced vector reshaped from this array</returns>
		public override DenseVector<T> ToVector()
		{
			if (!this.HasPitch)
				return new(this.Storage);
			// else
			var storage = this.CopyToStorage();
			return new(storage);
		}

		/// <summary>
		/// Reshape this array to a matrix with leading dimension = <paramref name="rows"/>
		/// </summary>
		/// <param name="rows">The number of rows of the target matrix; if <paramref name="rows"/> ≤ 0, it is assumed that leadDim = <c>sqrt(<see cref="AbstractArray{T}.Length"/>)</c>.</param>
		/// <returns>The reshaped matrix</returns>
		public override DenseMatrix<T> ToMatrix(long rows = 0)
		{
			Span<long> size = stackalloc long[2];
			size[0] = rows;
			CheckSize(this, size);
			if (size.SequenceEqual(this.Size))
				return new(this.Storage, size[0], size[1], this.m_outerSize[0]);
			// else
			var storage = this.CopyToStorage();
			return new(storage, size[0], size[1]);
		}

		/// <summary>
		/// Reshape this tensor to another tensor with the given <paramref name="newSize"/> 
		/// </summary>
		/// <param name="newSize">The new size of the tensor as a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/></param>
		/// <returns>The reshaped tensor, may be a referenced one of this tensor</returns>
		public override DenseTensor<T> TensorReshape(ReadOnlySpan<long> newSize)
		{
			Span<long> size = stackalloc long[newSize.Length];
			newSize.CopyTo(size);
			CheckSize(this, size);
			if (size.SequenceEqual(this.Size))
				return this;
			// else
			var storage = this.CopyToStorage();
			return new(storage, size, size);
		}
		#endregion

		#region indexing
		/// <summary>
		/// The basic indexed getter and setter of this tensor
		/// </summary>
		/// <param name="indices">The position indicated by a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/> to be checked</param>
		/// <returns>The element at <paramref name="indices"/></returns>
		/// <exception cref="ArgumentException">If <paramref name="indices"/>'s length is not the same as the rank</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="indices"/> is out of range</exception>
		public override T this[ReadOnlySpan<long> indices] {
			get {
				long offset = this.CheckIndex(indices, this.OuterSize);
				return MEM.ToManaged(this.Storage + offset);
			}
			set {
				long offset = this.CheckIndex(indices, this.OuterSize);
				MEM.FromManaged(this.Storage + offset, value);
			}
		}

		/// <summary>
		/// Get a sub-tensor (of same rank) indicated by the given starting <paramref name="offsets"/> and <paramref name="lengths"/>
		/// </summary>
		/// <param name="offsets">The starting offsets of the target sub-tensor compared to this tensor at each dimension, in <typeparamref name="T"/></param>
		/// <param name="lengths">The lengths of the target sub-tensor at each dimension, in <typeparamref name="T"/></param>
		/// <returns>The <b>referenced</b> sub-tensor indicated by <paramref name="offsets"/> and <paramref name="lengths"/>. Shall be a referenced tensor if possible.</returns>
		/// <exception cref="ArgumentException">If <paramref name="offsets"/> and/or <paramref name="lengths"/>'s length is not the same as the rank</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offsets"/> and/or <paramref name="lengths"/> is out of range</exception>
		public override DenseTensor<T> GetSlice(ReadOnlySpan<long> offsets, ReadOnlySpan<long> lengths)
		{
			long offset = this.CheckRange(offsets, lengths, this.OuterSize);
			return new(this.Storage + offset, lengths, this.OuterSize);
		}

		// Ignore Spelling: stackalloc
		/// <summary>
		/// Get the sub-tensor (of same rank) indicated by the given starting <paramref name="offsets"/> and <paramref name="lengths"/> and copy it to <paramref name="overwrite"/>
		/// </summary>
		/// <param name="offsets">The starting offsets of the target sub-tensor compared to this tensor at each dimension, in <typeparamref name="T"/></param>
		/// <param name="lengths">The lengths of the target sub-tensor at each dimension, in <typeparamref name="T"/></param>
		/// <param name="overwrite">The tensor to be overwritten by the sub-tensor</param>
		/// <example>If you want to get a sub-tensor of lower rank, there is a way to do so:<br/>
		/// <code>
		/// var offsets = stackalloc long[] { 5, 50, 20, 0, 40 };
		/// var lengths = stackalloc long[] { 1, 100, 1, 200, 1 };
		/// // the size of 'tensor' is { 10, 200, 50, 200, 60 }
		/// var sub = tensor.<see cref="GetSlice(ReadOnlySpan{long}, ReadOnlySpan{long})">GetSlice</see>(offsets, lengths);
		/// var sizeWithoutOnes = stackalloc long[] { 100, 200 };
		/// var refSub = sub.<see cref="TensorReshape(ReadOnlySpan{long})">Reshape</see>(sizeWithoutOnes);
		/// // now, the 'refSub' (a <b>non</b>-referenced sub-tensor of 'tensor') contains the desired sub-tensor of lower rank
		/// </code>
		/// </example>
		/// <exception cref="ArgumentNullException">If <paramref name="overwrite"/> is null or empty</exception>
		/// <exception cref="ArgumentException">If <paramref name="offsets"/> and/or <paramref name="lengths"/>'s length is not the same as the rank; or <paramref name="overwrite"/> cannot be overwritten</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offsets"/> and/or <paramref name="lengths"/> is out of range</exception>
		public override void GetSlice(ReadOnlySpan<long> offsets, ReadOnlySpan<long> lengths, TensorBase<T> overwrite)
		{
			if (overwrite is null || !overwrite.IsValid())
				throw new ArgumentNullException(nameof(overwrite));
			if (overwrite is not DenseTensor<T> dense)
				throw new ArgumentException(Resources.Parameter.UnexpectedType, nameof(overwrite));
			var refSub = this.GetSlice(offsets, lengths);
			if (!dense.Size.SequenceEqual(refSub.Size))
				throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(overwrite));

			TAD.Permute<T>(new(refSub), new(dense), stackalloc int[this.Rank].FillWithRange(0));
		}

		/// <summary>
		/// Set the sub-tensor (of same rank) indicated by the given starting <paramref name="offsets"/> and the size of <paramref name="value"/> to the underlying tensor of <paramref name="value"/>
		/// </summary>
		/// <param name="offsets">The starting offsets of the target sub-tensor compared to this tensor at each dimension, in <typeparamref name="T"/></param>
		/// <param name="value">The tensor to set whose size is the lengths of the sub-tensor's size</param>
		/// <exception cref="ArgumentNullException">If <paramref name="value"/> is null or empty</exception>
		/// <exception cref="ArgumentException">If <paramref name="offsets"/> and/or <paramref name="value"/>'s size is not the same as the rank</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offsets"/> and/or <paramref name="value"/>'s size is out of range</exception>
		public override void SetSlice(ReadOnlySpan<long> offsets, TensorBase<T> value)
		{
			if (value is null || !value.IsValid())
				throw new ArgumentNullException(nameof(value));
			if (value is not DenseTensor<T> dense)
				throw new ArgumentException(Resources.Parameter.UnexpectedType, nameof(value));

			var refSub = this.GetSlice(offsets, dense.Size);
			TAD.Permute<T>(new(dense), new(refSub), stackalloc int[this.Rank].FillWithRange(0));
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
		public DenseTensor<T> GetFirstDims(int n, ReadOnlySpan<long> restIndices, ReadOnlySpan<long> offsets = default, ReadOnlySpan<long> lengths = default)
		{
			int rank = this.Rank;
			if (n <= 0 || n >= rank - 1)
				throw new ArgumentOutOfRangeException(nameof(n), n, Resources.Parameter.InvalidValue);
			if (restIndices.Length + n != rank)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(restIndices));
			// get equivalent ranges
			Span<long> allOffsets = stackalloc long[rank];
			Span<long> allLengths = stackalloc long[rank];
			restIndices.CopyTo(allOffsets[n..]);
			allLengths[n..].Fill(1);
			if (!offsets.IsEmpty)
			{
				if (offsets.Length != n)
					throw new ArgumentException(Resources.Parameter.WrongSize, nameof(offsets));
				offsets.CopyTo(allOffsets[..n]);
			}
			if (!lengths.IsEmpty)
			{
				if (lengths.Length != n)
					throw new ArgumentException(Resources.Parameter.WrongSize, nameof(lengths));
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
			long offset = CheckRange(allOffsets, allLengths);
			return new(this.Storage + offset, allLengths[..n], this.OuterSize[..n], this.Labels[..n]);
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
		public void SetFirstDims(int n, ReadOnlySpan<long> restIndices, DenseTensor<T> value, ReadOnlySpan<long> offsets = default, ReadOnlySpan<long> lengths = default)
		{
			if (value is null || !value.IsValid())
				throw new ArgumentNullException(nameof(value));
			var refSub = this.GetFirstDims(n, restIndices, offsets, lengths);
			if (!value.Size.SequenceEqual(refSub.Size))
				throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(value));

			TAD.Permute<T>(new(value), new(refSub), stackalloc int[n].FillWithRange(0));
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
		/// Get or set the (full) sub-tensor of rank <paramref name="n"/> located by the given <paramref name="restIndices"/> of length <c>(<see cref="AbstractArray{T}.Rank">rank</see> - <paramref name="n"/>)</c>.
		/// </summary>
		/// <param name="n">The first <paramref name="n"/> dimensions to get</param>
		/// <param name="restIndices">The position of the target sub-tensor at the rest (<see cref="AbstractArray{T}.Rank">rank</see> - <paramref name="n"/>) dimensions</param>
		/// <returns>The (full) sub-tensor of the first <paramref name="n"/> dimensions</returns>
		public DenseTensor<T> this[byte n, params Index[] restIndices] {
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
		#endregion

		#region tensor algebra methods
		/// <summary>
		/// Compute the tensor reduction (self partial summation) of this tensor under the given <paramref name="order"/>.
		/// </summary>
		/// <param name="order">The given <see cref="TensorOrder"/> to indicate which part(s) of dimension(s) to sum, its order will be ignored</param>
		/// <param name="scalar">The scalar to multiply to the result</param>
		/// <returns>The reduction result as a new <see cref="DenseTensor{T}"/></returns>
		/// <exception cref="ArgumentException">If <paramref name="order"/> does not indicate a partial permutation order</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="scalar"/> is 0</exception>
		public override DenseTensor<T> Reduce(TensorOrder order, T scalar)
		{
			if (scalar.IsZero())
				throw new ArgumentOutOfRangeException(nameof(scalar), scalar, Resources.Parameter.CannotZero);
			// get reduction permutation
			Span<int> reducePerm = stackalloc int[this.Rank];
			reducePerm = order.GetIntSpanOrder(this, reducePerm, allowPartial: true);
			// get output permutation
			int outRank = this.Rank - reducePerm.Length;
			Span<int> outPerm = stackalloc int[outRank];
			Span<int> identityPerm = stackalloc int[this.Rank].FillWithRange(0);
			identityPerm.SetExept(outPerm, outPerm);
			// get output members
			if (outRank == 0)
			{
				Span<long> size = stackalloc long[] { 1 };
				var storage = Storage<T>.Create(this.Storage[0].Location, 1);
				try
				{
					var tensor = new DenseTensor<T>(storage, size, size);
					TAD.Reduce<T>(BinaryOperation.Addition, new(this, scalar: scalar), new(tensor), reducePerm);
					return tensor;
				}
				catch (Exception)
				{
					storage?.Dispose();
					throw;
				}
			}
			else
			{
				Span<long> size = stackalloc long[outRank];
				Span<char> label = stackalloc char[outRank];
				this.Size.ReOrderTo(size, outPerm);
				this.Labels.ReOrderTo(label, outPerm);
				var storage = Storage<T>.Create(this.Storage[0].Location, size.Prod());
				try
				{
					var tensor = new DenseTensor<T>(storage, size, size, label);
					TAD.Reduce<T>(BinaryOperation.Addition, new(this, scalar: scalar), new(tensor), reducePerm);
					return tensor;
				}
				catch (Exception)
				{
					storage?.Dispose();
					throw;
				}
			}
		}

		/// <summary>
		/// Compute the tensor permutation of this tensor under the given <paramref name="order"/>.
		/// </summary>
		/// <param name="order">The given <see cref="TensorOrder"/> to indicate the permutation order</param>
		/// <param name="scalar">The scalar to multiply to the result</param>
		/// <returns>The permutation result as a new <see cref="DenseTensor{T}"/></returns>
		/// <exception cref="ArgumentException">If <paramref name="order"/> does not indicate a full permutation order</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="scalar"/> is 0</exception>
		public override DenseTensor<T> Permute(TensorOrder order, T scalar)
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
			var storage = Storage<T>.Create(this.Storage[0].Location, this.Length);
			try
			{
				var tensor = new DenseTensor<T>(storage, size, size, label);
				TAD.Permute<T>(new(this, scalar: scalar), new(tensor), perm);
				return tensor;
			}
			catch (Exception)
			{
				storage?.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Compute the tensor point-wise addition of this tensor and the <paramref name="other"/> tensor.
		/// </summary>
		/// <param name="scalarThis">The scalar to multiply to this tensor</param>
		/// <param name="other">The other tensor to perform the addition with</param>
		/// <param name="scalarOther">The scalar to multiply to the <paramref name="other"/> tensor</param>
		/// <returns>The addition result as a new <see cref="DenseTensor{T}"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="other"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="other"/> has different size than this one</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="scalarThis"/> or <paramref name="scalarOther"/> is 0</exception>
		public override DenseTensor<T> AddTensor(T scalarThis, TensorBase<T> other, T scalarOther)
		{
			if (scalarThis.IsZero())
				throw new ArgumentOutOfRangeException(nameof(scalarThis), scalarThis, Resources.Parameter.CannotZero);
			if (scalarOther.IsZero())
				throw new ArgumentOutOfRangeException(nameof(scalarOther), scalarOther, Resources.Parameter.CannotZero);
			if (other is not DenseTensor<T> dense)
				throw new NotSupportedException(Resources.Parameter.UnexpectedType);
			if (!this.Size.SequenceEqual(other.Size))
				throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(other));

			Span<int> identityPerm = stackalloc int[this.Rank].FillWithRange(0);
			var alike = this.NewArrayAlike();
			try
			{
				TAD.OperationBinary<T>(BinaryOperation.Addition, new(this, scalar: scalarThis), identityPerm, new(other, scalar: scalarOther), identityPerm, new(alike));
				return alike;
			}
			catch (Exception)
			{
				alike?.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Compute the tensor contraction of this tensor and the <paramref name="other"/> tensor using their .
		/// </summary>
		/// <param name="other">The other tensor to perform the contraction with</param>
		/// <param name="scalar">The scalar to multiply to the result</param>
		/// <param name="outputLabels">The desired output tensor's labels as a <see cref="ReadOnlySpan{T}"/> of <see cref="char"/>. Default (empty) means simple union of the labels of this tensor and the <paramref name="other"/> tensor.</param>
		/// <returns>The contraction result as a new <see cref="DenseTensor{T}"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="other"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="other"/>'s labels indicate that it cannot contract with this tensor</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="scalar"/> is 0</exception>
		public override DenseTensor<T> Contract(TensorBase<T> other, T scalar, ReadOnlySpan<char> outputLabels = default)
		{
			if (scalar.IsZero())
				throw new ArgumentOutOfRangeException(nameof(scalar), scalar, Resources.Parameter.CannotZero);
			if (other is not DenseTensor<T> dense)
				throw new NotSupportedException(Resources.Parameter.UnexpectedType);
			// stack allocate
			int commonRank = TensorContractInfo.GetContractRank(this, other);
			Span<int> concA = stackalloc int[commonRank], concB = stackalloc int[commonRank];
			Span<int> freeCA = stackalloc int[this.Rank - commonRank], freeCB = stackalloc int[dense.Rank - commonRank];
			Span<long> sizeC = stackalloc long[this.Rank + dense.Rank - commonRank];
			Span<char> labelC = stackalloc char[sizeC.Length];
			// get contraction info
			var info = TensorContractInfo.GetBinaryContractInfo(this.Size, this.Labels,
																dense.Size, dense.Labels,
																concA, concB, freeCA, freeCB,
																sizeC, labelC, outputLabels);
			// contract tensor
			Span<long> sizeOne = stackalloc long[] { 1 };
			if (sizeC.IsEmpty)
			{   // contract to a scalar
				sizeC = sizeOne; labelC = default;
			}
			var output = Storage<T>.Create(this.Storage[0].Location, sizeC.Prod());
			try
			{
				TAD.Contract<T>(new(this, scalar: scalar), new(dense), new(output, sizeC, sizeC, scalar: default), info);
				return new(output, sizeC, sizeC, labelC);
			}
			catch (Exception)
			{
				output?.Dispose();
				throw;
			}
		}
		#endregion

		#region in-place tensor operations
		/// <summary>
		/// Permute <paramref name="tensor"/> by <paramref name="order"/> and overwrite the result to this tensor.
		/// </summary>
		/// <param name="tensor">The <see cref="DenseTensor{T}"/> to be permuted</param>
		/// <param name="order">The full permutation order as a <see cref="TensorOrder"/></param>
		/// <param name="scalar">The scalar to multiply to the result</param>
		/// <param name="operation">The <see cref="UnaryOperation"/> to be applied to each element of the result tensor</param>
		/// <exception cref="ArgumentNullException">If <paramref name="tensor"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="order"/> does not indicate a full permutation order; or the permutation cannot be performed due to incompatible size</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="scalar"/> is 0</exception>
		public void PermuteFrom(DenseTensor<T> tensor, TensorOrder order, T scalar, UnaryOperation operation = UnaryOperation.Identity)
		{
			Span<int> perm = stackalloc int[this.Rank];
			order.GetIntSpanOrder(tensor, perm);
			TAD.Permute<T>(new(tensor, operation, scalar), new(this), perm);
		}

		/// <summary>
		/// Compute the tensor reduction of the given <paramref name="tensor"/> of the given <paramref name="order"/> and overwrite the result to this tensor.
		/// </summary>
		/// <param name="tensor">The <see cref="DenseTensor{T}"/> to be reduced</param>
		/// <param name="order">The given <see cref="TensorOrder"/> to indicate which part(s) of dimension(s) of <paramref name="tensor"/> to sum, its order will be ignored</param>
		/// <param name="scalar">The scalar to multiply to the result</param>
		/// <param name="unary">The <see cref="UnaryOperation"/> to be applied to each element of the <paramref name="tensor"/> before reduction</param>
		/// <param name="reduction">The <see cref="BinaryOperation"/> used to indicate which reduction operation to use</param>
		/// <exception cref="ArgumentNullException">If <paramref name="tensor"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="order"/> does not indicate a partial permutation order; or the permutation cannot be performed due to incompatible size</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="scalar"/> is 0</exception>
		public void ReduceFrom(DenseTensor<T> tensor, TensorOrder order, T scalar, UnaryOperation unary = UnaryOperation.Identity, BinaryOperation reduction = BinaryOperation.Addition)
		{
			Span<int> perm = stackalloc int[this.Rank];
			perm = order.GetIntSpanOrder(tensor, perm, allowPartial: true);
			TAD.Reduce<T>(reduction, new(tensor, unary, scalar), new(this), perm);
		}

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
		/// <exception cref="ArgumentException">If <paramref name="A"/>'s labels indicate that it cannot contract with <paramref name="B"/>'s</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="scalar"/> is 0</exception>
		public void ContractFrom(T scalar, DenseTensor<T> A, DenseTensor<T> B, T scalarThis = default, UnaryOperation unaryA = UnaryOperation.Identity, UnaryOperation unaryB = UnaryOperation.Identity, UnaryOperation unaryThis = UnaryOperation.Identity)
		{
			int concRank = TensorContractInfo.GetContractRank(A, B);
			Span<int> concA = stackalloc int[concRank], concB = stackalloc int[concRank];
			Span<int> freeCA = stackalloc int[A.Rank - concRank], freeCB = stackalloc int[B.Rank - concRank];
			TensorContractInfo info = new(A, B, this, concA, concB, freeCA, freeCB);
			TAD.Contract<T>(new(A, unaryA, scalar), new(B, unaryB), new(this, unaryThis, scalarThis), info);
		}

		/// <summary>
		/// Compute the tensor point-wise <paramref name="binary"/> operation of the given tensor <paramref name="orderA"/>(<paramref name="A"/>) and <paramref name="orderB"/>(<paramref name="B"/>) and overwrite the result to this tensor.
		/// </summary>
		/// <param name="binary">The <see cref="BinaryOperation"/> used to indicate which point-wise binary operation to use</param>
		/// <param name="A">The left input tensor to perform the binary operation with, can be null</param>
		/// <param name="B">The right input tensor to perform the binary operation with, can be null</param>
		/// <param name="scalarA">The scalar to multiply to <paramref name="A"/> before the binary operation, can be 0</param>
		/// <param name="scalarB">The scalar to multiply to <paramref name="B"/> before the binary operation, can be 0</param>
		/// <param name="orderA">The full permutation order of <paramref name="A"/></param>
		/// <param name="orderB">The full permutation order of <paramref name="B"/></param>
		/// <param name="unaryA">The <see cref="UnaryOperation"/> to be applied to each element of <paramref name="A"/> before binary operation</param>
		/// <param name="unaryB">The <see cref="UnaryOperation"/> to be applied to each element of <paramref name="B"/> before binary operation</param>
		/// <exception cref="ArgumentException">If both <paramref name="A"/> and <paramref name="B"/> are null or invalid; or both <paramref name="scalarA"/> and <paramref name="scalarB"/> are 0; or one of them has different size than this tensor</exception>
		public void TensorBinaryOperation(BinaryOperation binary, DenseTensor<T>? A, DenseTensor<T>? B, T scalarA = default, T scalarB = default, TensorOrder orderA = default, TensorOrder orderB = default, UnaryOperation unaryA = UnaryOperation.Identity, UnaryOperation unaryB = UnaryOperation.Identity)
		{
			if (scalarA.IsZero())
				A = null;
			if (scalarB.IsZero())
				B = null;
			if (A is null && B is null)
				throw new ArgumentException(Resources.Parameter.CannotAllNull);

			Span<int> permA = stackalloc int[A?.Rank ?? 0], permB = stackalloc int[B?.Rank ?? 0];
			if (A is not null)
				orderA.GetIntSpanOrder(A, permA);
			if (B is not null)
				orderB.GetIntSpanOrder(B, permB);
			TAD.OperationBinary<T>(binary, new(A, unaryA, scalarA), permA, new(B, unaryB, scalarB), permB, new(this));
		}
		#endregion

		#region IKrylovVector
		T IKrylovVector<DenseTensor<T>, T>.Dot(DenseTensor<T> other)
		{
			if (other is null || !other.IsValid())
				throw new ArgumentNullException(nameof(other));
			if (!this.Size.SequenceEqual(other.Size))
				throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(other));

			// shortcut
			if (!this.HasPitch && !other.HasPitch)
				return this.ToVector().Dot(other.ToVector());
			// else
			Span<long> sizeC = stackalloc long[] { 1 };
			Span<int> identityPerm = stackalloc int[this.Rank].FillWithRange(0);
			TensorContractInfo info = new(identityPerm, identityPerm, ReadOnlySpan<int>.Empty, ReadOnlySpan<int>.Empty);
			var output = Storage<T>.Create(this.Storage[0].Location, 1);
			try
			{
				TAD.Contract<T>(new(this, UnaryOperation.Conjugate), new(other), new(output, sizeC, sizeC, scalar: default), info);
				return MEM.ToManaged(output);
			}
			catch (Exception)
			{
				output?.Dispose();
				throw;
			}
		}

		void IKrylovVector<DenseTensor<T>, T>.AddBy(DenseTensor<T> other, T scalar)
			=> this.TensorBinaryOperation(BinaryOperation.Addition, other, this, scalar, Const<T>.One);

		/// <summary>
		/// Replace this tensor's content with the <paramref name="other"/> tensor in-place.
		/// </summary>
		/// <param name="other">The other <see cref="DenseTensor{T}"/> to replace from</param>
		/// <exception cref="ArgumentNullException">If <paramref name="other"/> is null or invalid</exception>
		/// <exception cref="InvalidOperationException">If this and <paramref name="other"/> have different sizes</exception>
		public void ReplaceBy(DenseTensor<T> other)
		{
			if (other is null || !other.IsValid())
				throw new ArgumentNullException(nameof(other));
			if (!this.Size.SequenceEqual(other.Size))
				throw new InvalidOperationException(Resources.Parameter.NotSameSize);

			TAD.Permute<T>(new(other), new(this), stackalloc int[this.Rank].FillWithRange(0));
		}
		#endregion

		#region matrix operation and decompositions
		/*
		/// <summary>
		/// Multiply this tensor as a matrix with the <paramref name="right"/> tensor as another matrix.
		/// </summary>
		/// <param name="right">The other <see cref="DenseTensor{T}"/> as a matrix</param>
		/// <param name="partitionLeft">a <see cref="Index"/> to indicate the first <paramref name="partitionLeft"/> (exclude) indices of this tensor will be regarded as the row and others column</param>
		/// <param name="partitionRight">a <see cref="Index"/> to indicate the first <paramref name="partitionRight"/> (exclude) indices of tensor <paramref name="right"/> will be regarded as the row and others column</param>
		/// <param name="leftOp">The <see cref="MatrixOperation"/> to apply on this one</param>
		/// <param name="rightOp">The <see cref="MatrixOperation"/> to apply on <paramref name="right"/></param>
		/// <returns>the multiplication result, out-of-place</returns>
		public DenseTensor<T> OperatorMatrixMultiply(DenseTensor<T> right, Index partitionLeft, Index partitionRight, MatrixOperation leftOp = MatrixOperation.None, MatrixOperation rightOp = MatrixOperation.None)
		{
			if (this.OnHost != right.OnHost)
				throw new ArgumentException(Resource.RequireSamePos, nameof(right));
			int pl = (int)partitionLeft.GetPosition(this.Rank);
			int pr = (int)partitionRight.GetPosition(right.Rank);

			var (m, n) = (this.SizeProd[pl], this.Length / this.SizeProd[pl]);
			if (leftOp != MatrixOperation.None)
				(m, n) = (n, m);
			var (p, q) = (right.SizeProd[pr], right.Length / right.SizeProd[pr]);
			if (rightOp != MatrixOperation.None)
				(p, q) = (q, p);
			if (n != p)
				throw new ArgumentException(Resource.TensorWrongSize, nameof(right));
			var outSizeL = leftOp == MatrixOperation.None ? this.Size.Take(pl) : this.Size.TakeLast(this.Rank - pl);
			var outSizeR = rightOp != MatrixOperation.None ? right.Size.Take(pr) : right.Size.TakeLast(right.Rank - pr);

			var output = new DenseMatrix<T>(m, q, this.OnHost);
			try
			{
				using var l = this.ToMatrix(this.SizeProd[pl]) as DenseMatrix<T>;
				using var r = right.ToMatrix(right.SizeProd[pr]) as DenseMatrix<T>;
				output.Mulβ_AddBy_αAB(l, r, Scalars<T>.One, opA: leftOp, opB: rightOp);
				return FromDense(output, outSizeL.Concat(outSizeR).ToArray());
			}
			catch (Exception)
			{
				output.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Compute the singular value decomposition (SVD) of this tensor and corresponding the left and/or right singular vectors: $A = U S V^*$ <b>out-of-place</b>, where $A$ is this matrix.
		/// </summary>
		/// <param name="partition">a <see cref="Index"/> to indicate the first <paramref name="partition"/> (exclude) indices of this tensor will be regarded as the row and others column</param>
		/// <param name="calcU">calculate the left singular vectors or not, if false, the return <c>U</c> will be null</param>
		/// <param name="calcV">calculate the right singular vectors or not, if false, the return <c>Vct</c> will be null</param>
		/// <returns>the singular values as a <see cref="double"/> array and left, right singular vectors</returns>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		public (double[] S, DenseTensor<T> U, DenseTensor<T> Vct) SingularValues(Index partition, bool calcU = true, bool calcV = true)
		{
			int p = (int)partition.GetPosition(this.Rank);

			var leftSize = this.Size.Take(p); var rightSize = this.Size.TakeLast(this.Rank - p);
			long leftLength = this.SizeProd[p], rightLength = this.Length / leftLength;
			var middleSize = new[] { Math.Min(leftLength, rightLength) };
			var Usize = leftSize.Concat(middleSize).ToArray();
			var VctSize = middleSize.Concat(rightSize).ToArray();

			using var mat = this.ToMatrix(leftLength) as DenseMatrix<T>;
			var (S, U, Vct) = mat.SingularValues(null, null, null, calcU, calcV);
			using (S)
				return (Array.ConvertAll(S.ToFortranOrderArray(), s => s.ToDouble()), FromDense(U, Usize), FromDense(Vct, VctSize));
		}

		/// <summary>
		/// Compute the singular value decomposition (SVD) of this tensor and corresponding the left and/or right singular vectors: $A = U S V^*$ <b>out-of-place</b>, where $A$ is this matrix. Not necessarily sorted descending by singular values. Then truncate the singular values $S$ and vectors $U$, $V^*$ to preserve at most <paramref name="maxPreserve"/> entries.
		/// </summary>
		/// <param name="partition">a <see cref="Index"/> to indicate the first <paramref name="partition"/> (exclude) indices of this tensor will be regarded as the row and others column</param>
		/// <param name="maxPreserve">The maximum number of singular values and vectors to preserve, must be positive</param>
		/// <returns>The singular values and left, right singular vectors with at most <paramref name="maxPreserve"/> entries.</returns>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="partition"/> is out of range</exception>
		public (DenseTensor<T> S, DenseTensor<T> U, DenseTensor<T> Vct) SingularValuesTruncate(Index partition, int maxPreserve)
		{
			int p = (int)partition.GetPosition(this.Rank);

			var leftSize = this.Size.Take(p); var rightSize = this.Size.TakeLast(this.Rank - p);
			long leftLength = this.SizeProd[p], rightLength = this.Length / leftLength;
			var middleSize = new[] { Math.Min(Math.Min(leftLength, rightLength), maxPreserve) };
			var Usize = leftSize.Concat(middleSize).ToArray();
			var VctSize = middleSize.Concat(rightSize).ToArray();

			using var mat = this.ToMatrix(leftLength) as DenseMatrix<T>;
			var (S, U, Vct) = mat.SingularValues(null, null, null, calcU: true, calcV: true);
			if (maxPreserve >= leftLength || maxPreserve >= rightLength)
			{
				var returnS = new DenseTensor<T>(new[] { middleSize[0], middleSize[0] }, onHost: this.OnHost);
				try
				{
					(returnS.ToMatrix() as DenseMatrix<T>).SetDiag(0, S);
					return (returnS, FromDense(U, Usize), FromDense(Vct, VctSize));
				}
				catch (Exception)
				{
					returnS?.Dispose();
					throw;
				}
				finally
				{
					S.Dispose();
				}
			}
			// else
			using (S) using (U) using (Vct)
			{
				var arrayS = S.ToFortranOrderArray();
				var arrayU = U.GetColumns();
				var arrayV = Vct.GetRows();
				try
				{
					var combine = arrayU.Zip(arrayV).ToArray();
					Array.Sort(keys: arrayS, items: combine);
					arrayS = arrayS.Reverse().ToArray();
					combine = combine.Reverse().ToArray();
					arrayS = arrayS[..maxPreserve];
					combine = combine[..maxPreserve];
					// copy to return U
					var returnU = new DenseMatrix<T>(U.NRows, U.NCols, U.OnHost);
					returnU.FromColumnVectors(Array.ConvertAll(combine, c => c.First));
					// copy to return V
					using var returnV = new DenseMatrix<T>(Vct.NCols, Vct.NRows, Vct.OnHost);
					returnV.FromColumnVectors(Array.ConvertAll(combine, c => c.Second));
					var returnVct = returnV.Transpose();
					// copy to return S
					var returnS = new DenseTensor<T>(new[] { middleSize[0], middleSize[0] }, onHost: this.OnHost);
					using var vecS = new DenseVector<T>(arrayS.Length, S.OnHost);
					vecS.FromFortranOrderArray(arrayS);
					(returnS.ToMatrix() as DenseMatrix<T>).SetDiag(0, S);
					// return
					return (returnS, FromDense(returnU, Usize), FromDense(returnVct, VctSize));
				}
				finally
				{
					arrayU.ClearList();
					arrayV.ClearList();
				}
			}
		}

		/// <summary>
		/// QR factorize this tensor <b>out-of-place</b>.
		/// </summary>
		/// <param name="partition">a <see cref="Index"/> to indicate the first <paramref name="partition"/> (exclude) indices of this tensor will be regarded as the row and others column</param>
		/// <param name="full">perform full factorization or not</param>
		/// <returns>the Q matrix and R matrix</returns>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		public (DenseTensor<T> Q, DenseTensor<T> R) QR(Index partition, bool full = false)
		{
			int p = (int)partition.GetPosition(this.Rank);

			var leftSize = this.Size.Take(p); var rightSize = this.Size.TakeLast(this.Rank - p);
			long leftLength = this.SizeProd[p], rightLength = this.Length / leftLength;
			full = full && leftLength > rightLength; // for 'fat' matrices, full == economic
			var middleSize = new[] { full ? leftLength : Math.Min(leftLength, rightLength) };
			var Qsize = leftSize.Concat(middleSize).ToArray();
			var Rsize = middleSize.Concat(rightSize).ToArray();

			using var mat = this.ToMatrix(leftLength) as DenseMatrix<T>;
			var (Q, R) = mat.QR(full, null, null);
			return (FromDense(Q, Qsize), FromDense(R, Rsize));
		}

		/// <summary>
		/// (Conjugate) transpose this tensor <b>out-of-place</b>.
		/// </summary>
		/// <param name="partition">a <see cref="Index"/> to indicate the first <paramref name="partition"/> (exclude) indices of this tensor will be regarded as the row and others column</param>
		/// <param name="conjugate">conjugate or not, default null means true for complex type (<see cref="IComplex{T}"/>)</param>
		/// <returns>the (conjugate) transpose of this tensor with <c>Size = this.Size[<paramref name="partition"/>..] concatenate this.Size[..<paramref name="partition"/>]</c></returns>
		public DenseTensor<T> Transpose(Index partition, bool? conjugate = null)
		{
			int p = (int)partition.GetPosition(this.Rank);
			using var mat = this.ToMatrix(this.SizeProd[p]) as DenseMatrix<T>;
			var matTranspose = (conjugate ?? !default(T).ToDataType().IsReal()) ? mat.ConjugateTranspose() : mat.Transpose();
			return FromDense(matTranspose, this.Size.TakeLast(this.Rank - p).Concat(this.Size.Take(p)).ToArray());
		}

		/// <summary>
		/// Calculate the trace of this tensor as a matrix.
		/// </summary>
		/// <returns>the trace of this tensor as a matrix</returns>
		/// <exception cref="InvalidOperationException">if this tensor's shape is not a square matrix</exception>
		public T Trace()
		{
			if (this.Rank != 2 || this.Size[0] != this.Size[1])
				throw new InvalidOperationException();
			using var diag = ((DenseMatrix<T>)this.ToMatrix(this.Size[0])).GetDiag(0);
			return diag.Sum();
		}

		/// <summary>
		/// Shift all the eigenvalues of this tensor by adding <paramref name="shift"/> to each diagonal elements of this tensor as a matrix.
		/// </summary>
		/// <param name="shift">The shift value, if it is zero, no operation shall be performed</param>
		/// <exception cref="InvalidOperationException">if this tensor's shape is not a square matrix</exception>
		public void EigenvalueShift(T shift)
		{
			if (this.Rank != 2 || this.Size[0] != this.Size[1])
				throw new InvalidOperationException();
			// shortcut
			if (shift.CompareTo(Scalars<T>.Zero) == 0)
				return;
			using var ones = new DenseVector<T>(this.Size[0], this.OnHost);
			ones.FillWithOnes();
			BLAS.VectorAddBy(y: (DenseMatrix<T>)this.ToMatrix(this.Size[0]), x: ones, α: shift, strideY: (int)ones.Length + 1);
		}
		*/
		#endregion

		#region print
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
			int vectorMaxLen = settings.ArrayLength, matrixMaxRows = settings.MatrixRow, matrixMaxCols = settings.MatrixColumn;

			// get actual rank
			int actualRank = 0;
			var size = this.Size;
			for (int i = 0; i < size.Length; i++)
			{
				if (size[i] != 1)
					actualRank++;
			}
			Span<long> truncateSize = stackalloc long[this.Rank];
			size.CopyTo(truncateSize);
			Span<long> offsets = stackalloc long[this.Rank];
			// actually a vector
			if (actualRank == 1)
			{
				int dd = truncateSize.IndexOf(static s => s > 1);
				truncateSize[dd] = Math.Min(vectorMaxLen, truncateSize[dd]);
				using var temp = this.GetSlice(offsets, truncateSize).ToContiguous();
				return description + ":" + Environment.NewLine + DenseVector<T>.ActualPrint(temp, this.Length, settings);
			}
			int d = truncateSize.IndexOf(static s => s > 1);
			long rows = truncateSize[d];
			truncateSize[d] = Math.Min(matrixMaxRows, truncateSize[d]);
			long ld = truncateSize[d];
			d = truncateSize[(d + 1)..].IndexOf(static s => s > 1);
			long cols = truncateSize[d];
			truncateSize[d] = Math.Min(matrixMaxCols, truncateSize[d]);
			// actually a matrix
			if (actualRank == 2)
			{
				using var temp = this.GetSlice(offsets, truncateSize).ToContiguous();
				return description + ":" + Environment.NewLine + DenseMatrix<T>.ActualPrint(temp, rows, cols, ld, settings);
			}
			// else
			StringBuilder sb = new(description);
			sb.AppendLine(":");
			// get lengths
			Span<long> matSize = stackalloc long[this.Rank];
			truncateSize.CopyTo(matSize);
			int matrixRank = d + 1;
			matSize[matrixRank..].Fill(1);
			// prepare loop
			int restRank = this.Rank - matrixRank;
			long matrixLength = rows * cols;
			int maxShow = (int)Math.Min(vectorMaxLen, this.Length / matrixLength);
			Span<long> sizeProd = stackalloc long[restRank + 1];
			this.SizeProd[matrixRank..].CopyTo(sizeProd); sizeProd[^1] = this.Length;
			sizeProd.CopyTo(sizeProd, s => s / matrixLength);
			// loop
			for (int i = 0; i < maxShow; i++)
			{
				for (int k = 0; k < restRank; k++)
				{
					offsets[k + matrixRank] = (i % sizeProd[k + 1]) / sizeProd[k];
				}
				using var tempMat = this.GetSlice(offsets, matSize).ToContiguous();
				sb.Append($"Tensor[.., .., {offsets[matrixRank..].SpanJoin(", ")}]:").AppendLine();
				sb.AppendLine(DenseMatrix<T>.ActualPrint(tempMat, rows, cols, ld, settings)).AppendLine();
			}
			// return
			return sb.ToString();
		}
		#endregion

		#region serialization
		/// <summary>
		/// Get all the storages of this array. Only returns <see cref="ValueArray{T}.Storage"/>.
		/// </summary>
		/// <returns>All the storages of the array as an <see cref="IReadOnlyDictionary{TKey, TValue}"/> of <see cref="string"/> and <see cref="IStorage"/></returns>
		public override IReadOnlyDictionary<string, IStorage> GetStorages() => new Dictionary<string, IStorage>(1)
		{
			[StorageName] = this.Storage
		};

		/// <summary>
		/// The presenting name of <see cref="OuterSize"/>
		/// </summary>
		protected internal const string OuterSizeName = nameof(OuterSize);

		/// <summary>
		/// Get other requisite informations for re-constructing the array of that derived class type. Only returns the <see cref="TensorBase{T}.Labels"/> and <see cref="OuterSize"/>.
		/// </summary>
		/// <returns>Other requisite informations used to re-construct this array</returns>
		public override IReadOnlyDictionary<string, object> GetMetaData() => new Dictionary<string, object>(2)
		{
			[LabelsName] = this.Labels.ToArray(),
			[OuterSizeName] = this.OuterSize.ToArray()
		};
		#endregion
	}
}