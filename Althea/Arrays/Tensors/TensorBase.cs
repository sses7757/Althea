using System;
using System.Collections;
using System.Collections.Generic;

using Althea.Linq;
using Althea.Helpers;
using Althea.NativeTypes;
using System.Runtime.CompilerServices;

namespace Althea.Arrays
{
	/// <summary>
	/// The abstract tensor class with the only mutable <see cref="ValueArray{T}.Storage"/> that refers to the actual data storage. There may be more pointer(s) for different indices in a sparse vector that inherits <see cref="VectorBase{T}"/>, but they shall be immutable.
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
	public abstract class TensorBase<T> : ValueArray<T>, ITensor where T : unmanaged
	{
		#region basic
		private readonly FixedBuffer_128<long> m_sizeProd = default;

		private FixedBuffer_32<char> m_labels = default;

		/// <summary>
		/// Get the (inclusive) accumulated product of the <see cref="AbstractArray{T}.Size"/> of this tensor
		/// </summary>
		/// <remarks>The first element is 1 and the length is the same as <see cref="AbstractArray{T}.Size"/></remarks>
		public ReadOnlySpan<long> SizeProd => this.m_sizeProd.AsSpan();

		/// <summary>
		/// Construct a <see cref="TensorBase{T}"/> by preallocated <paramref name="values"/> and the given <paramref name="size"/>
		/// </summary>
		/// <param name="values">The preallocated <see cref="Storage{T}"/> of the value array</param>
		/// <param name="size">The presenting size of the tensor</param>
		/// <param name="label">The presenting labels of each dimension of this tensor, an empty one means auto generate as <c>{'a', 'b', ...}</c></param>
		/// <param name="actualLength">The actual length of the <paramref name="values"/>, default 0 means the length of it</param>
		protected TensorBase(Storage<T> values, ReadOnlySpan<long> size, ReadOnlySpan<char> label, long actualLength = 0) : base(values, size, actualLength)
		{
			Span<char> span = stackalloc char[size.Length];
			if (label.IsEmpty)
			{
				span.FillWithRange('a');
			}
			else
			{
				label.CopyTo(span);
			}
			if (label.Length != size.Length)
				throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(label));
			this.m_labels = default;
			this.m_labels.CopyFromSpan(span);

			this.m_sizeProd = default;
			var prod = this.m_sizeProd.AsSpan();
			this.Size.AccumulateProd(result: prod, inclusive: true);
		}
		#endregion

		#region tensor label
		/// <summary>
		/// Get or set the label array as a <see cref="ReadOnlySpan{T}"/> of <see cref="char"/> used to mark each index of this tensor
		/// </summary>
		/// <exception cref="ArgumentException">If the setting value's length is not the same as the <see cref="AbstractArray{T}.Rank"/></exception>
		public ReadOnlySpan<char> Label {
			get => this.m_labels.AsSpan();
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
		public override TensorBase<T> ToTensor(ReadOnlySpan<long> size)
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
		public abstract TensorBase<T> TensorReshape(ReadOnlySpan<long> newSize);
		#endregion

		#region indexing
		/// <summary>
		/// Check whether the given <paramref name="indices"/> is out of range of this tensor
		/// </summary>
		/// <param name="indices">The indices as a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/> to be checked</param>
		/// <exception cref="ArgumentException">If <paramref name="indices"/>'s length is not the same as the rank</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="indices"/> is out of range</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected void CheckIndex(ReadOnlySpan<long> indices)
		{
			int rank = this.Rank;
			var size = this.Size;
			if (indices.Length != rank)
				throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(indices));
			for (int i = 0; i < rank; i++)
			{
				if (indices[i] < 0 || indices[i] >= size[i])
					throw new ArgumentOutOfRangeException(nameof(indices), indices[i], Resources.Parameter.InvalidValue);
			}
		}

		/// <summary>
		/// Check whether the given ranges indicated by <paramref name="offsets"/> and <paramref name="lengths"/> are out of range of this tensor
		/// </summary>
		/// <param name="offsets">The starting offset indices as a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/> to be checked</param>
		/// <param name="lengths">The lengths as a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/> to be checked</param>
		/// <exception cref="ArgumentException">If <paramref name="offsets"/> and/or <paramref name="lengths"/>'s length is not the same as the rank</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offsets"/> and/or <paramref name="lengths"/> is out of range</exception>
		protected void CheckRange(ReadOnlySpan<long> offsets, ReadOnlySpan<long> lengths)
		{
			int rank = this.Rank;
			var size = this.Size;
			if (offsets.Length != rank)
				throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(offsets));
			if (lengths.Length != rank)
				throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(lengths));
			for (int i = 0; i < rank; i++)
			{
				if (offsets[i] < 0 || offsets[i] >= size[i])
					throw new ArgumentOutOfRangeException(nameof(offsets), offsets[i], Resources.Parameter.InvalidValue);
				if (lengths[i] < 0 || offsets[i] + lengths[i] >= size[i])
					throw new ArgumentOutOfRangeException(nameof(offsets), offsets[i], Resources.Parameter.InvalidValue);
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
		/// When implemented by a derived class, get a sub-tensor indicated by the given starting <paramref name="offsets"/> and <paramref name="lengths"/>
		/// </summary>
		/// <param name="offsets">The starting offsets of the target sub-tensor compared to this tensor at each dimension, in <typeparamref name="T"/></param>
		/// <param name="lengths">The lengths of the target sub-tensor at each dimension, in <typeparamref name="T"/></param>
		/// <returns>The sub-tensor indicated by <paramref name="offsets"/> and <paramref name="lengths"/>. Shall be a referenced tensor if possible.</returns>
		/// <exception cref="ArgumentException">If <paramref name="offsets"/> and/or <paramref name="lengths"/>'s length is not the same as the rank</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offsets"/> and/or <paramref name="lengths"/> is out of range</exception>
		public abstract TensorBase<T> Slice(ReadOnlySpan<long> offsets, ReadOnlySpan<long> lengths);

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
		/// Get a sub-tensor indicated by the given <paramref name="ranges"/>
		/// </summary>
		/// <param name="ranges">The array of <see cref="Range"/> to indicate the target sub-tensor location and size compared to this tensor at each dimension</param>
		/// <returns>The sub-tensor indicated by <paramref name="ranges"/>. May be a referenced tensor.</returns>
		/// <exception cref="ArgumentException">If <paramref name="ranges"/>'s length is not the same as the rank</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="ranges"/> is out of range</exception>
		public TensorBase<T> Slice(params Range[] ranges)
		{
			Span<long> off = stackalloc long[this.Rank];
			Span<long> len = stackalloc long[this.Rank];
			this.GetRange(off, len, ranges);
			return this.Slice(off, len);
		}
		#endregion

		#region tensor algebra abstract methods
		/// <summary>
		/// When implemented by a derived class, compute the dot (inner) product of this vector and the <paramref name="other"/> vector.
		/// </summary>
		/// <param name="other">The other vector to perform the dot product</param>
		/// <param name="conjugateThis">Whether the dot product is performed on the conjugation of this vector or directly.</param>
		/// <returns>The dot (inner) product result as a <typeparamref name="T"/></returns>
		public abstract T Dot(VectorBase<T> other, bool conjugateThis = true);
		#endregion

		#region operators
		/// <summary>
		/// Compute the dot (inner) product result of the given <paramref name="left"/> and <paramref name="right"/> vectors.
		/// </summary>
		/// <param name="left">One original vector as the left operand</param>
		/// <param name="right">One original vector as the right operand</param>
		/// <returns>A <typeparamref name="T"/> which is the dot (inner) product result of the given <paramref name="left"/> and <paramref name="right"/> vectors</returns>
		public static T operator *(VectorBase<T> left, VectorBase<T> right)
		{
			if (left is null || !left.IsValid())
				throw new ArgumentNullException(nameof(left));
			if (right is null || !right.IsValid())
				throw new ArgumentNullException(nameof(right));

			return left.Dot(right);
		}

		/// <summary>
		/// Create a new <see cref="VectorBase{T}"/> which is the (point-wise) addition result of the given <paramref name="left"/> and <paramref name="right"/> vectors.
		/// </summary>
		/// <param name="left">One original vector as the left operand</param>
		/// <param name="right">One original vector as the right operand</param>
		/// <returns>A new <see cref="VectorBase{T}"/> which is the addition result of the given <paramref name="left"/> and <paramref name="right"/> vectors</returns>
		public static VectorBase<T> operator +(VectorBase<T> left, VectorBase<T> right)
		{
			if (left is null || !left.IsValid())
				throw new ArgumentNullException(nameof(left));
			if (right is null || !right.IsValid())
				throw new ArgumentNullException(nameof(right));

			return left.AddVector(right, Const<T>.One);
		}

		/// <summary>
		/// Create a new <see cref="VectorBase{T}"/> which is the (point-wise) subtraction result of the given <paramref name="left"/> and <paramref name="right"/> vectors.
		/// </summary>
		/// <param name="left">One original vector as the left operand</param>
		/// <param name="right">One original vector as the right operand</param>
		/// <returns>A new <see cref="VectorBase{T}"/> which is the subtraction result of the given <paramref name="left"/> and <paramref name="right"/> vectors</returns>
		public static VectorBase<T> operator -(VectorBase<T> left, VectorBase<T> right)
		{
			if (left is null || !left.IsValid())
				throw new ArgumentNullException(nameof(left));
			if (right is null || !right.IsValid())
				throw new ArgumentNullException(nameof(right));

			return left.AddVector(right, Const<T>.MinusOne);
		}

		/// <summary>
		/// Create a new <see cref="VectorBase{T}"/> which is the multiplication result of the given <paramref name="vector"/> and <paramref name="scalar"/>
		/// </summary>
		/// <param name="vector">The original vector to multiply</param>
		/// <param name="scalar">The scalar of type <typeparamref name="T"/> to multiply</param>
		/// <returns>A new <see cref="VectorBase{T}"/> which is the multiplication result of the given <paramref name="vector"/> and <paramref name="scalar"/></returns>
		public static VectorBase<T> operator *(VectorBase<T> vector, T scalar)
		{
			if (vector is null || !vector.IsValid())
				throw new ArgumentNullException(nameof(vector));

			return vector.ApplyToClone(v => v.AddScalar(scalar));
		}

		/// <summary>
		/// Create a new <see cref="VectorBase{T}"/> which is the negation result of the given <paramref name="vector"/>
		/// </summary>
		/// <param name="vector">The original vector to negate</param>
		/// <returns>A new <see cref="VectorBase{T}"/> which is the negation result of the given <paramref name="vector"/></returns>
		public static VectorBase<T> operator -(VectorBase<T> vector) => vector * Const<T>.MinusOne;

		/// <summary>
		/// Create a new <see cref="VectorBase{T}"/> which is the multiplication result of the given <paramref name="vector"/> and <paramref name="scalar"/>
		/// </summary>
		/// <param name="vector">The original vector to multiply</param>
		/// <param name="scalar">The scalar of type <typeparamref name="T"/> to multiply</param>
		/// <returns>A new <see cref="VectorBase{T}"/> which is the multiplication result of the given <paramref name="vector"/> and <paramref name="scalar"/></returns>
		public static VectorBase<T> operator *(T scalar, VectorBase<T> vector) => vector * scalar;

		/// <summary>
		/// Create a new <see cref="VectorBase{T}"/> which is the division result of the given <paramref name="vector"/> and <paramref name="scalar"/>
		/// </summary>
		/// <param name="vector">The original vector to be divided</param>
		/// <param name="scalar">The scalar of type <typeparamref name="T"/> to divide</param>
		/// <returns>A new <see cref="VectorBase{T}"/> which is the multiplication result of the given <paramref name="vector"/> and <paramref name="scalar"/></returns>
		public static VectorBase<T> operator /(VectorBase<T> vector, T scalar) => vector * scalar.GenericReciprocal();

		/// <summary>
		/// Create a new <see cref="VectorBase{T}"/> which is the multiplication result of the given left <paramref name="matrix"/> and right <paramref name="vector"/>.
		/// </summary>
		/// <param name="vector">The input vector to be multiplied at the right side</param>
		/// <param name="matrix">The input matrix to be multiplied at the left side</param>
		/// <returns>A new <see cref="VectorBase{T}"/> which is the multiplication result of the given left <paramref name="matrix"/> and right <paramref name="vector"/></returns>
		public static VectorBase<T> operator *(MatrixBase<T> matrix, VectorBase<T> vector)
		{
			if (matrix is null || !matrix.IsValid())
				throw new ArgumentNullException(nameof(matrix));
			if (vector is null || !vector.IsValid())
				throw new ArgumentNullException(nameof(vector));

			return vector.AddMatrixMultiplyVector(matrix, vector, Const<T>.One);
		}

		/// <summary>
		/// Create a new <see cref="VectorBase{T}"/> which is the multiplication result of the given left <paramref name="vector"/> and right <paramref name="matrix"/>.
		/// </summary>
		/// <param name="vector">The input vector to be multiplied at the left side</param>
		/// <param name="matrix">The input matrix to be multiplied at the right side</param>
		/// <returns>A new <see cref="VectorBase{T}"/> which is the multiplication result of the given left <paramref name="vector"/> and right <paramref name="matrix"/></returns>
		public static VectorBase<T> operator *(VectorBase<T> vector, MatrixBase<T> matrix)
		{
			if (matrix is null || !matrix.IsValid())
				throw new ArgumentNullException(nameof(matrix));
			if (vector is null || !vector.IsValid())
				throw new ArgumentNullException(nameof(vector));

			return vector.AddMatrixMultiplyVector(matrix, vector, Const<T>.One, operation: LinearAlgebra.MatrixOperation.Transpose);
		}
		#endregion
	}
}
