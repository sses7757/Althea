using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

using Althea.Arrays;
using Althea.Helpers;
using Althea.Linq;
using Althea.NativeTypes;
using Althea.TensorAlgebra;
using Althea.Solver;

using MEM = Althea.Storage.IAbstractApi;
using TAD = Althea.TensorAlgebra.Dense.AbstractApi;


namespace Althea.Backend.Arrays
{
	/// <summary>
	/// The concrete dense tensor class with the only mutable <see cref="ValueArray{T}.Storage"/> that refers to the actual (pitched) data storage without any index storage.
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
	[StructLayout(LayoutKind.Explicit)]
	public class DenseTensor<T> : BaseTensor<T>, IPitchedArray<T>, IKrylovVector<DenseTensor<T>, T> where T : unmanaged
	{
		#region basic
		// previously defined 324 bytes
		[FieldOffset(0)]
		private readonly FixedBuffer_128<long> m_outerSize = default;
		[FieldOffset(128)]
		private long __overlap;
		[FieldOffset(128)]
		private readonly FixedBuffer_128<long> m_outerSizeProd = default;
		[FieldOffset(128 * 2)]
		private readonly long m_outerLength;
		// this defines extra 264 bytes

		/// <summary>
		/// Get the pitch (in <typeparamref name="T"/>) of this array (the outer size at each dimension) as a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/>.
		/// </summary>
		public ReadOnlySpan<long> OuterSize {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.m_outerSize.AsSpan(this.Rank);
		}

		/// <summary>
		/// Get the strides (the both-end inclusive accumulated product of <see cref="OuterSize"/>) of this tensor at all dimensions as a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/>.
		/// </summary>
		/// <remarks>The first element is 1, the last element is the product of <see cref="OuterSize"/> and the <see cref="ReadOnlySpan{T}.Length"/> == <see cref="BaseTensor{T}.Rank"/> + 1</remarks>
		public ReadOnlySpan<long> Strides {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => MemoryMarshal.CreateReadOnlySpan(ref this.__overlap, this.Rank + 1);
		}

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
				prodOuter -= prod * (outerSize[i] - size[i]);
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
			if (this.Rank < 16)
			{
				prod[this.Rank] = prod[^1] * size[^1];
			}
			else
			{
				this.m_outerLength = prod[^1] * size[^1];
			}
		}

		/// <summary>
		/// Create an empty
		/// </summary>
		public DenseTensor() : base(Storage<T>.Empty, stackalloc long[1], ReadOnlySpan<char>.Empty) { }
		#endregion

		#region clone related
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Storage<T> ToContiguous(Storage<T> storage, ReadOnlySpan<long> size, ReadOnlySpan<long> outerSize, ReadOnlySpan<long> strides, long newLength)
		{
			if (size.SequenceEqual(outerSize))
				return CopyToStorage(storage, size, outerSize, strides, newLength);
			else
				return storage;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ActualStorage<T> CopyToStorage(Storage<T> storage, ReadOnlySpan<long> size, ReadOnlySpan<long> outerSize, ReadOnlySpan<long> strides, long newLength)
		{
			var newStorage = Storage<T>.Create(storage[0].Location, newLength);
			try
			{
				TAD.Permute<T>(new(storage, size, outerSize, strides), new(newStorage, size), stackalloc int[size.Length].FillWithRange(0));
				return newStorage;
			}
			catch (Exception)
			{
				newStorage?.Dispose();
				throw;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private Storage<T> ToContiguous()
		{
			if (this.HasPitch)
				return this.CopyToStorage();
			else
				return this.Storage;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private ActualStorage<T> CopyToStorage()
		{
			var size = this.Size;
			var storage = Storage<T>.Create(this.Storage[0].Location, this.Length);
			try
			{
				TAD.Permute<T>(new(this), new(storage, size), stackalloc int[this.Rank].FillWithRange(0));
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
			Span<long> size = stackalloc long[] { rows, 0 };
			CheckSize(this, size);
			if (size.SequenceEqual(this.Size))
				return new(this.Storage, size[0], size[1], this.m_outerSize[0]);
			// check leading dimension
			int f = this.SizeProd.IndexOf(size[0]);
			if (f >= 0 && this.Strides[..f].SequenceEqual(this.SizeProd[..f]))
			{
				if (this.OuterSize[f..^1].SequenceEqual(this.Size[f..^1]))
					return new(this.Storage, size[0], size[1], this.m_outerSizeProd[f]);
			}
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
		/// var offsets = stackalloc long[] { 5, 50, 20, 0, 40 };<br/>
		/// var lengths = stackalloc long[] { 1, 100, 1, 200, 1 };<br/>
		/// // the size of 'tensor' is { 10, 200, 50, 200, 60 }<br/>
		/// var sub = tensor.<see cref="GetSlice(ReadOnlySpan{long}, ReadOnlySpan{long})">GetSlice</see>(offsets, lengths);<br/>
		/// var sizeWithoutOnes = stackalloc long[] { 100, 200 };<br/>
		/// var refSub = sub.<see cref="TensorReshape(ReadOnlySpan{long})">Reshape</see>(sizeWithoutOnes);<br/>
		/// // now, the 'refSub' (a <b>non</b>-referenced sub-tensor of 'tensor') contains the desired sub-tensor of lower rank
		/// </code>
		/// </example>
		/// <exception cref="ArgumentNullException">If <paramref name="overwrite"/> is null or empty</exception>
		/// <exception cref="ArgumentException">If <paramref name="offsets"/> and/or <paramref name="lengths"/>'s length is not the same as the rank; or <paramref name="overwrite"/> cannot be overwritten</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offsets"/> and/or <paramref name="lengths"/> is out of range</exception>
		public override void GetSlice(ReadOnlySpan<long> offsets, ReadOnlySpan<long> lengths, BaseTensor<T> overwrite)
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
		/// <param name="lengths">The lengths of the target sub-tensor at each dimension, in <typeparamref name="T"/></param>
		/// <param name="value">The tensor to set whose size is the lengths of the sub-tensor's size</param>
		/// <exception cref="ArgumentNullException">If <paramref name="value"/> is null or empty</exception>
		/// <exception cref="ArgumentException">If <paramref name="offsets"/> and/or <paramref name="value"/>'s size is not the same as the rank</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offsets"/> and/or <paramref name="value"/>'s size is out of range</exception>
		public override void SetSlice(ReadOnlySpan<long> offsets, ReadOnlySpan<long> lengths, BaseTensor<T> value)
		{
			if (value is null || !value.IsValid())
				throw new ArgumentNullException(nameof(value));
			if (value is not DenseTensor<T> dense)
				throw new ArgumentException(Resources.Parameter.UnexpectedType, nameof(value));

			var refSub = this.GetSlice(offsets, lengths);
			if (!value.Size.SequenceLargerEqualThan(lengths))
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(value));

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
		public override DenseTensor<T> GetFirstDims(int n, ReadOnlySpan<long> restIndices, ReadOnlySpan<long> offsets = default, ReadOnlySpan<long> lengths = default)
		{
			// get equivalent ranges
			Span<long> allOffsets = stackalloc long[this.Rank];
			Span<long> allLengths = stackalloc long[this.Rank];
			long offset = this.CheckFirstDims(n, restIndices, offsets, lengths, allOffsets, allLengths);
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
		public override void SetFirstDims(int n, ReadOnlySpan<long> restIndices, BaseTensor<T> value, ReadOnlySpan<long> offsets = default, ReadOnlySpan<long> lengths = default)
		{
			if (value is null || !value.IsValid())
				throw new ArgumentNullException(nameof(value));
			if (value is not DenseTensor<T> dense)
				throw new ArgumentException(Resources.Parameter.UnexpectedType, nameof(value));
			var refSub = this.GetFirstDims(n, restIndices, offsets, lengths);
			if (!dense.Size.SequenceLargerEqualThan(refSub.Size))
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(value));

			TAD.Permute<T>(new(value), new(dense), stackalloc int[n].FillWithRange(0));
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

			Span<int> reducePerm = stackalloc int[this.Rank];
			Span<long> size = stackalloc long[this.Rank];
			Span<char> label = stackalloc char[this.Rank];
			reducePerm = this.CheckReduce(order, reducePerm, ref size, ref label);
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
		public override DenseTensor<T> AddTensor(T scalarThis, BaseTensor<T> other, T scalarOther)
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
		public override DenseTensor<T> Contract(BaseTensor<T> other, T scalar, ReadOnlySpan<char> outputLabels = default)
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
				TAD.Contract<T>(new(this, scalar: scalar), new(dense), new(output, sizeC, scalar: default), info);
				return new(output, sizeC, labels: labelC);
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
				TAD.Contract<T>(new(this, UnaryOperation.Conjugate), new(other), new(output, sizeC, scalar: default), info);
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

		#region print
		private sealed class TensorPrintCommonInfo
		{
			private readonly FixedBuffer_128<long> outerSize = default, matrixSize = default;

			private readonly long rows, cols, ld, length;

			private readonly int rank;

			private readonly PrintSettings settings;


			internal ReadOnlySpan<long> OuterSize {
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => this.outerSize.AsSpan(this.rank);
			}

			internal ReadOnlySpan<long> MatrixSize {
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => this.matrixSize.AsSpan(this.rank);
			}

			internal long MatrixOrgRows {
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => this.rows;
			}
			internal long MatrixOrgCols {
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => this.cols;
			}
			internal long MatrixNowLD {
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => this.ld;
			}
			internal long MatrixNowLength {
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => this.length;
			}

			internal PrintSettings Settings {
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => this.settings;
			}

			internal TensorPrintCommonInfo(ReadOnlySpan<long> outerSize, ReadOnlySpan<long> matrixSize, long rows, long cols, long ld, long matrixLength, PrintSettings settings)
			{
				this.outerSize.CopyFromSpan(outerSize); this.matrixSize.CopyFromSpan(matrixSize);
				this.rank = outerSize.Length;
				this.rows = rows; this.cols = cols; this.ld = ld; this.length = matrixLength;
				this.settings = settings;
			}
		}

		private readonly ref struct TensorPrinter
		{
			private readonly Storage<T> storage;

			private readonly ReadOnlySpan<long> size;

			private readonly ReadOnlySpan<long> outerSize;

			private readonly ReadOnlySpan<long> outerSizeProd;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private TensorPrinter(ReadOnlySpan<long> size, ReadOnlySpan<long> outerSize, ReadOnlySpan<long> outerSizeProd, Storage<T> storage)
			{
				this.storage = storage;
				this.size = size;
				this.outerSize = outerSize;
				this.outerSizeProd = outerSizeProd;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private TensorPrinter(TensorPrinter parent, long rowIndex, long colIndex)
			{
				this.size = parent.size[..^2];
				this.outerSize = parent.outerSize[..^2];
				this.outerSizeProd = parent.outerSizeProd[..^2];
				this.storage = parent.storage + (parent.outerSizeProd[^2] * (rowIndex + parent.outerSize[^2] * colIndex));
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private static void AppendRow(StringBuilder sb, string currentRow, string prefix, string postfix, bool lastRow, long moreRows)
			{
				if (lastRow)
				{
					if (moreRows <= 0)
					{
						sb.Append(currentRow);
						return;
					}
					sb.AppendLine(currentRow).Append(prefix).AppendFormat(Resources.Print.MoreRows, moreRows);
				}
				else
				{
					sb.AppendLine(currentRow);
					int find = currentRow.LastIndexOf(Environment.NewLine, sb.Length - 2);
					int lineWidth = sb.Length - find - Environment.NewLine.Length * 2 - postfix.Length;
					sb.Append(prefix.PadRight(lineWidth)).AppendLine(postfix);
				}
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			internal static string Print(ReadOnlySpan<long> size, ReadOnlySpan<long> outerSize, ReadOnlySpan<long> outerSizeProd, Storage<T> storage, ReadOnlySpan<long> matrixSize, long rows, long cols, long ld, PrintSettings settings, string? prefix = null, string? postfix = null)
			{
				int rank = size.Length;
				bool topLayerIsVec = rank % 2 == 1;
				long matrixLength = matrixSize.Prod();
				prefix += "|"; postfix = "|" + postfix;
				TensorPrintCommonInfo info = new(outerSize, matrixSize, rows, cols, ld, matrixLength, settings);
				if (topLayerIsVec)
				{
					StringBuilder sb = new();
					string subPrefix = prefix + "|", subPostfix = "|" + postfix;
					long crows = size[0], lastOuterSizeProd = outerSizeProd[^1];
					int nrows = (int)Math.Min(crows, settings.MatrixRow);
					ReadOnlySpan<long> vecSize = size[..^1], vecOuterSize = outerSize[..^1], vecOuterSizeProd = outerSizeProd[..^1];
					for (int i = 0; i < nrows; i++)
					{
						TensorPrinter current = new(vecSize, vecOuterSize, vecOuterSizeProd, storage + lastOuterSizeProd * i);
						string currentRow = Print(current, info, subPrefix, subPostfix);
						AppendRow(sb, currentRow, prefix, postfix, lastRow: i == nrows - 1, moreRows: 0);
					}
					return sb.ToString();
				}
				else
				{
					var tensor = new TensorPrinter(size, outerSize, outerSizeProd, storage);
					return Print(tensor, info, prefix, postfix);
				}
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
			private static string Print(TensorPrinter tensor, TensorPrintCommonInfo info, string prefix, string postfix)
			{
				if (tensor.size.Length == 2)
				{
					using var temp = ToContiguous(tensor.storage, info.MatrixSize, info.OuterSize, stackalloc long[] { 1, info.MatrixNowLD }, info.MatrixNowLength);
					return DenseMatrix<T>.ActualPrint(temp, info.MatrixOrgRows, info.MatrixOrgCols, info.MatrixNowLD, info.Settings, prefix, postfix);
				}
				// else
				StringBuilder sb = new();
				long rows = tensor.size[0], cols = tensor.size[1];
				int nrows = (int)Math.Min(rows, info.Settings.MatrixRow), ncols = (int)Math.Min(cols, info.Settings.MatrixColumn);
				string moreElem = cols > ncols ? string.Format(Resources.Print.RowMore + postfix, cols - ncols) : postfix;
				string[] subMatsCurrentRow = new string[ncols];
				for (int i = 0; i < nrows; i++)
				{
					for (int j = 0; j < ncols; j++)
					{
						TensorPrinter current = new(tensor, i, j);
						subMatsCurrentRow[j] = Print(current, info, "|", "|");
					}
					string currentRow = subMatsCurrentRow.MultilineConcat(prefix, "|  |", moreElem);
					AppendRow(sb, currentRow, prefix, postfix, lastRow: i == nrows - 1, moreRows: rows - nrows);
				}
				return sb.ToString();
			}
		}

		internal static string ActualPrint(Storage<T> storage, ReadOnlySpan<long> size, ReadOnlySpan<long> outerSize, ReadOnlySpan<long> outerSizeProd, PrintSettings settings, string? prefix = null)
		{
			int vectorMaxLen = settings.ArrayLength, matrixMaxRows = settings.MatrixRow, matrixMaxCols = settings.MatrixColumn;
			// get actual rank
			int actualRank = 0, rank = size.Length;
			long allLength = 1;
			for (int i = 0; i < size.Length; i++)
			{
				allLength *= size[i];
				if (size[i] != 1)
					actualRank++;
			}
			Span<long> truncateSize = stackalloc long[rank];
			size.CopyTo(truncateSize);
			// actually a vector
			if (actualRank == 1)
			{
				int dd = truncateSize.IndexOf(static s => s > 1);
				truncateSize[dd] = Math.Min(vectorMaxLen, truncateSize[dd]);
				using var temp = ToContiguous(storage, truncateSize, outerSize, outerSizeProd, truncateSize[dd]);
				return DenseVector<T>.ActualPrint(temp, allLength, settings, prefix);
			}
			int d = truncateSize.IndexOf(static s => s > 1);
			long rows = truncateSize[d];
			truncateSize[d] = Math.Min(matrixMaxRows, truncateSize[d]);
			long ld = truncateSize[d];
			d = truncateSize[(d + 1)..].IndexOf(static s => s > 1) + d + 1;
			long cols = truncateSize[d];
			truncateSize[d] = Math.Min(matrixMaxCols, truncateSize[d]);
			long matrixLength = rows * cols;
			// actually a matrix
			if (actualRank == 2)
			{
				using var temp = ToContiguous(storage, truncateSize, outerSize, outerSizeProd, matrixLength);
				return DenseMatrix<T>.ActualPrint(temp, rows, cols, ld, settings, prefix);
			}
			// else
			if (settings.MatrixFormTensor)
			{
				// reduce matrix size
				matrixMaxRows = (int)Math.Ceiling(Math.Sqrt(matrixMaxRows));
				matrixMaxCols = (int)Math.Ceiling(Math.Sqrt(matrixMaxCols));
				settings = new(settings, matrixRow: matrixMaxRows, matrixColumn: matrixMaxCols);
				d = truncateSize.IndexOf(static s => s > 1);
				truncateSize[d] = Math.Min(matrixMaxRows, truncateSize[d]);
				ld = truncateSize[d];
				d = truncateSize[(d + 1)..].IndexOf(static s => s > 1) + d + 1;
				truncateSize[d] = Math.Min(matrixMaxCols, truncateSize[d]);
				matrixLength = rows * cols;
				// print
				return TensorPrinter.Print(size, outerSize, outerSizeProd, storage, truncateSize, rows, cols, ld, settings, prefix);
			}
			else
			{
				StringBuilder sb = new();
				// get lengths
				Span<long> offsets = stackalloc long[rank];
				int matrixRank = d + 1;
				truncateSize[matrixRank..].Fill(1);
				string matrixRankString = ".., ".RepeatString(matrixRank);
				// prepare loop
				int restRank = rank - matrixRank;
				int numberOfMatrices = (int)Math.Min(vectorMaxLen, allLength / matrixLength);
				Span<long> sizeProd = stackalloc long[restRank + 1].SetValue(1);
				for (int i = 1; i <= restRank; i++)
				{
					sizeProd[i] = sizeProd[i - 1] * size[i + matrixRank - 1];
				}
				// loop
				for (int i = 0; i < numberOfMatrices; i++)
				{
					for (int k = 0; k < restRank; k++)
					{
						offsets[k + matrixRank] = (i % sizeProd[k + 1]) / sizeProd[k];
					}
					using var tempMat = ToContiguous(storage + i * matrixLength, truncateSize, outerSize, outerSizeProd, matrixLength);
					sb.Append(prefix).Append("Tensor[").Append(matrixRankString).Append(offsets[matrixRank..].SpanJoin(", ")).AppendLine("] =");
					sb.AppendLine(DenseMatrix<T>.ActualPrint(tempMat, rows, cols, ld, settings, prefix));
				}
				// return
				return sb.ToString();
			}
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
			return description + ":" + Environment.NewLine + ActualPrint(this.Storage, this.Size, this.OuterSize, this.Strides, settings);
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
		/// Get other requisite informations for re-constructing the array of that derived class type. Only returns the <see cref="BaseTensor{T}.Labels"/> and <see cref="OuterSize"/>.
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