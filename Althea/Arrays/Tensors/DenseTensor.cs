using System;

using Althea.Linq;
using Althea.LinearAlgebra;
using Althea.Storage;
using Althea.TensorAlgebra;

using Ten = Althea.TensorAlgebra.Dense.BaseApiSelector;
using ExtTen = Althea.TensorAlgebra.Dense.ExtendApiSelector;


namespace Althea.Arrays.Tensors
{
	/// <summary>
	/// The dense tensor interface whose only storage is of type <typeparamref name="TS"/>.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TS">The storage type used by the value <see cref="ISingleValueStorageArray{T, TS, TSelf}.Storage"/></typeparam>
	/// <typeparam name="TSelf">The concrete type that implements this <see cref="IDenseTensor{T, TS, TSelf}"/></typeparam>
	public interface IDenseTensor<T, TS, TSelf> : IBaseTensor<T, TSelf>, ISingleValueStorageArray<T, TS, TSelf>
		where T : unmanaged, INumber<T>
		where TS : class, IStorage<T, TS>
		where TSelf : class, IDenseTensor<T, TS, TSelf>
	{
		#region basic
		/// <summary>
		/// When implemented by a derived class, get the pitch (in <typeparamref name="T"/>) of this tensor (the outer size at each dimension) as a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/>. It must has length equals to <see cref="ILabeledTensor.Size"/> and consists numbers larger than or equals to <see cref="ILabeledTensor.Size"/> respectively.
		/// </summary>
		ReadOnlySpan<long> OuterSize { get; }

		/// <summary>
		/// When implemented by a derived class, get the (both end inclusive) accumulated product of the <see cref="OuterSize"/> of this tensor.
		/// </summary>
		/// <remarks>The first element is 1, the last element is the product of <see cref="OuterSize"/> and its size == <see cref="ILabeledTensor.Rank"/> + 1</remarks>
		protected ReadOnlySpan<long> OuterSizeProd { get; }

		/// <summary>
		/// When implemented by a derived class, statically create a referenced <typeparamref name="TSelf"/> with given <paramref name="storage"/>, <paramref name="size"/> and <paramref name="outerSize"/>.
		/// </summary>
		/// <param name="storage">The storage of the new tensor</param>
		/// <param name="size">The sizes of each dimension in <typeparamref name="T"/> of the new tensor</param>
		/// <param name="outerSize">The outer sizes of each dimension in <typeparamref name="T"/> of the new tensor, default means the same as <paramref name="size"/></param>
		/// <returns>The created referenced tensor of type <typeparamref name="TSelf"/>.</returns>
		protected abstract static TSelf CreateRef(TS storage, ReadOnlySpan<long> size, ReadOnlySpan<long> outerSize = default);
		#endregion

		#region element indexing
		T IBaseTensor<T, TSelf>.this[ReadOnlySpan<long> indices]
		{
			get
			{
				return (this.Storage + this.CheckIndex(indices, this.OuterSizeProd)).ToManaged<T, TS>();
			}
			set
			{
				(this.Storage + this.CheckIndex(indices, this.OuterSizeProd)).FromManaged(value);
			}
		}
		#endregion

		#region range indexing
		TSelf IBaseTensor<T, TSelf>.GetSlice(ReadOnlySpan<long> offsets, ReadOnlySpan<long> lengths)
		{
			var storage = this.Storage + this.CheckRange(offsets, lengths, this.OuterSizeProd);
			return TSelf.CreateRef(storage, lengths, this.OuterSize);
		}

		void IBaseTensor<T, TSelf>.CopyTo(TSelf destination)
		{
			if (!((ILabeledTensor)this).Size.SequenceEqual(((ILabeledTensor)destination).Size))
				throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(destination));
			Ten.Permute<T, TS, TS>(new(this, this.Storage), new(destination, destination.Storage), stackalloc int[this.Rank].FillWithRange(0));
		}

		void IBaseTensor<T, TSelf>.SetSlice(ReadOnlySpan<long> offsets, ReadOnlySpan<long> lengths, TSelf value)
		{
			var storage = this.Storage + this.CheckRange(offsets, lengths, this.OuterSizeProd, value);
			Ten.Permute<T, TS, TS>(new(value, value.Storage), new(storage, lengths, this.OuterSize, this.OuterSizeProd), stackalloc int[this.Rank].FillWithRange(0));
		}
		#endregion

		#region first few dimensions indexing
		TSelf IBaseTensor<T, TSelf>.GetFirstDims(int n, ReadOnlySpan<long> restIndices, ReadOnlySpan<long> offsets, ReadOnlySpan<long> lengths)
		{
			Span<long> allOffsets = stackalloc long[this.Rank], allLengths = stackalloc long[this.Rank];
			var storage = this.Storage + this.CheckFirstDims(n, restIndices, offsets, lengths, allOffsets, allLengths, this.OuterSizeProd);
			return TSelf.CreateRef(storage, lengths, this.OuterSize[..n]);
		}

		void IBaseTensor<T, TSelf>.SetFirstDims(int n, ReadOnlySpan<long> restIndices, TSelf value, ReadOnlySpan<long> offsets, ReadOnlySpan<long> lengths)
		{
			Span<long> allOffsets = stackalloc long[this.Rank], allLengths = stackalloc long[this.Rank];
			var storage = this.Storage + this.CheckFirstDims(n, restIndices, offsets, lengths, allOffsets, allLengths, this.OuterSizeProd, value);
			Ten.Permute<T, TS, TS>(new(value, value.Storage), new(storage, lengths, this.OuterSize[..n], this.OuterSizeProd[..(n + 1)]), stackalloc int[n].FillWithRange(0));
		}
		#endregion

		#region point-wise operations
		void IValueArray<T, TSelf>.FillWith(T value) => ExtTen.PointWiseBinary<T, TS>(new(this, this.Storage), value, BinaryOperation.GetSecond);

		void IValueArray<T, TSelf>.AddScalar(T value) => ExtTen.PointWiseBinary<T, TS>(new(this, this.Storage), value, BinaryOperation.Addition);

		void IValueArray<T, TSelf>.Scale(T value) => Ten.Permute<T, TS, TS>(new(this, this.Storage, scalar: value), new(this, this.Storage), stackalloc int[this.Rank].FillWithRange(0));

		void IValueArray<T, TSelf>.Conjugate() => Ten.Permute<T, TS, TS>(new(this, this.Storage, UnaryOperation.Conjugate), new(this, this.Storage), stackalloc int[this.Rank].FillWithRange(0));

		void IValueArray<T, TSelf>.Power(T power) => ExtTen.PointWiseBinary<T, TS>(new(this, this.Storage), power, BinaryOperation.Power);

		void IValueArray<T, TSelf>.Truncate(double threshold) => ExtTen.PointWiseBinary<T, TS>(new(this, this.Storage), T.Create(threshold), BinaryOperation.ClipFirstBySecond);
		#endregion

		#region simple aggregation operations
		T IValueArray<T, TSelf>.Sum() => ExtTen.PointWiseAggregation<T, TS>(new(this, this.Storage), UnaryOperation.Identity, BinaryOperation.Addition);

		T IValueArray<T, TSelf>.AbsSum() => ExtTen.PointWiseAggregation<T, TS>(new(this, this.Storage), UnaryOperation.AbsoluteValue, BinaryOperation.Addition);

		T IValueArray<T, TSelf>.Norm() => ExtTen.Norm<T, TS>(new(this, this.Storage));

		T IValueArray<T, TSelf>.ValueWithMaxAbs() => ExtTen.PointWiseAggregation<T, TS>(new(this, this.Storage), UnaryOperation.AbsoluteValue, BinaryOperation.Maximum);

		T IValueArray<T, TSelf>.ValueWithMinAbs() => ExtTen.PointWiseAggregation<T, TS>(new(this, this.Storage), UnaryOperation.AbsoluteValue, BinaryOperation.Mininum);
		#endregion
	}
}
