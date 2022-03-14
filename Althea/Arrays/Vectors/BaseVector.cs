using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Althea.Helpers;
using Althea.Storage;

using Blas = Althea.LinearAlgebra.Dense.BlasApiSelector;
using ExtBlas = Althea.LinearAlgebra.Dense.ExtendBlasApiSelector;

namespace Althea.Arrays
{
	/// <summary>
	/// The abstract vector interface whose only value storage is of type <typeparamref name="TS"/>.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TS">The storage type used by the value <see cref="Storage"/></typeparam>
	/// <typeparam name="TSelf">The concrete type that implements this <see cref="IBaseVector{T, TS, TSelf}"/></typeparam>
	public interface IBaseVector<T, TS, TSelf> : ISingleValueStorageArray<T, TS, TSelf>, IReadOnlyList<T>
		where T : unmanaged, INumber<T>
		where TS : class, IStorage<T, TS>
		where TSelf : class, ISingleValueStorageArray<T, TS, TSelf>
	{
		#region indexing
		/// <summary>
		/// Provide legacy support of C# duck type for <c>this[<see cref="Index"/>]</c> and <c>this[<see cref="Range"/>]</c>
		/// </summary>
		int IReadOnlyCollection<T>.Count => (int)this.Length;

		/// <summary>
		/// Check whether the given <paramref name="index"/> is out of range of this vector
		/// </summary>
		/// <param name="index">The index to be checked</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="index"/> is out of range</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected void CheckIndex(long index)
		{
			if (index < 0)
				throw new ArgumentOutOfRangeException(nameof(index), index, Resources.Parameter.CannotNegative);
			if (index >= this.Length)
				throw new ArgumentOutOfRangeException(nameof(index), index, Resources.Parameter.InvalidValue);
		}

		/// <summary>
		/// Check whether the given range indicated by <paramref name="offset"/> and <paramref name="length"/> is out of range of this vector
		/// </summary>
		/// <param name="offset">The starting offset index to be checked</param>
		/// <param name="length">The length to be checked</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offset"/> and/or <paramref name="length"/> is out of range</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected void CheckRange(long offset, long length)
		{
			if (offset < 0)
				throw new ArgumentOutOfRangeException(nameof(offset), offset, Resources.Parameter.CannotNegative);
			if (offset >= this.Length)
				throw new ArgumentOutOfRangeException(nameof(offset), offset, Resources.Parameter.InvalidValue);
			if (length < 0)
				throw new ArgumentOutOfRangeException(nameof(length), length, Resources.Parameter.CannotNegative);
			if (offset + length > this.Length)
				throw new ArgumentOutOfRangeException(nameof(length), length, Resources.Parameter.InvalidValue);
		}

		/// <summary>
		/// When implemented by a derived class, provide the basic indexed getter and setter of this vector
		/// </summary>
		/// <param name="index">The position of the element to get / set</param>
		/// <returns>The element at <paramref name="index"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="index"/> is out of range</exception>
		T this[long index] { get; set; }

		/// <summary>
		/// Provide legacy support of <see cref="this[long]"/> and C# duck type for <c>this[<see cref="Index"/>]</c>
		/// </summary>
		T IReadOnlyList<T>.this[int index] => this[(long)index];

		/// <summary>
		/// When implemented by a derived class, get a sub-vector indicated by the given <paramref name="start"/> offset and <paramref name="count"/>
		/// </summary>
		/// <param name="start">The starting offset of the target sub-vector compared to this vector, in <typeparamref name="T"/></param>
		/// <param name="count">The length of the target sub-vector, in <typeparamref name="T"/></param>
		/// <returns>The sub-vector indicated by <paramref name="start"/> and <paramref name="count"/>. Shall be a referenced vector if possible.</returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="start"/> and/or <paramref name="count"/> is out of range</exception>
		TSelf GetSlice(long start, long count);

		/// <summary>
		/// When implemented by a derived class, set the sub-vector indicated by the given <paramref name="start"/> offset and <paramref name="count"/> to <paramref name="value"/>
		/// </summary>
		/// <param name="start">The starting offset of the target sub-vector compared to this vector, in <typeparamref name="T"/></param>
		/// <param name="count">The length of the target sub-vector, in <typeparamref name="T"/></param>
		/// <param name="value">The sub-vector to set</param>
		/// <exception cref="ArgumentNullException">If <paramref name="value"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="start"/> and/or <paramref name="count"/> is out of range</exception>
		/// <exception cref="ArgumentException">If <paramref name="value"/> cannot be used to set</exception>
		void SetSlice(long start, long count, TSelf value);

		/// <summary>
		/// Provide legacy support of C# duck type for <c>this[<see cref="Range"/>]</c>
		/// </summary>
		public virtual TSelf Slice(int start, int length) => this.GetSlice(start, length);

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			for (long i = 0; i < this.Length; i++)
			{
				yield return this[i];
			}
		}

		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		#endregion

		#region linear algebra abstract methods
		/// <summary>
		/// When implemented by a derived class, compute the dot (inner) product of this vector and the <paramref name="other"/> vector. The default implementation simply dots two <see cref="ISingleValueStorageArray{T, TS, TSelf}.Storage"/>s.
		/// </summary>
		/// <param name="other">The other vector to perform the dot product</param>
		/// <param name="conjugateThis">Whether the dot product is performed on the conjugation of this vector or directly.</param>
		/// <returns>The dot (inner) product result as a <typeparamref name="T"/></returns>
		public virtual T Dot(TSelf other, bool conjugateThis = true) => Blas.Dot<T, TS, TS>(conjugateThis, this.Storage, 1, other.Storage, 1);

		/// <summary>
		/// When implemented by a derived class, compute the out-of-place addition of the <paramref name="other"/> vector (scaling by <paramref name="scalar"/>) and this vector. The default implementation simply adds two <see cref="ISingleValueStorageArray{T, TS, TSelf}.Storage"/>s
		/// </summary>
		/// <param name="other">The other vector to add</param>
		/// <param name="scalar">The scalar to be multiplied to <paramref name="other"/> of type <typeparamref name="T"/></param>
		/// <returns>The addition result of this + <paramref name="scalar"/> * <paramref name="other"/></returns>
		public virtual TSelf AddVector(TSelf other, T scalar) => ((TSelf)this).ApplyToClone(c => Blas.Add(scalar, other.Storage, 1, c.Storage, 1));

		/// <summary>
		/// When implemented by a derived class, compute the addition of the multiplication result of the given <paramref name="matrix"/> and <paramref name="vector"/> (scaled by <paramref name="α"/>) with this vector (scaled by <paramref name="β"/>).
		/// </summary>
		/// <param name="matrix">The input matrix to be multiplied</param>
		/// <param name="vector">The input vector to be multiplied</param>
		/// <param name="α">The scalar to be multiplied to the <paramref name="matrix"/> of type <typeparamref name="T"/></param>
		/// <param name="β">The scalar to be multiplied to this vector of type <typeparamref name="T"/></param>
		/// <param name="operation">The simple operation to be applied to <paramref name="matrix"/> before computation as a <see cref="LinearAlgebra.MatrixOperation"/></param>
		/// <returns>The addition result of <paramref name="β"/> * this + <paramref name="α"/> * <paramref name="operation"/>(<paramref name="matrix"/>) * <paramref name="vector"/></returns>
		public abstract BaseVector<T> AddMatrixMultiplyVector(BaseMatrix<T> matrix, BaseVector<T> vector, T α, T β = default, LinearAlgebra.MatrixOperation operation = LinearAlgebra.MatrixOperation.None);
		#endregion

		#region operators
		/// <summary>
		/// Create a new <see cref="BaseVector{T}"/> which is the point-wise exponentiation result of the given <paramref name="vector"/> and <paramref name="power"/>.
		/// </summary>
		/// <param name="vector">The original vector whose elements are the bases</param>
		/// <param name="power">The power acting as the exponent of type <see cref="double"/></param>
		/// <returns>A new <see cref="BaseVector{T}"/> which is the point-wise exponentiate result of the given <paramref name="vector"/> and <paramref name="power"/></returns>
		public static BaseVector<T> operator ^(BaseVector<T> vector, double power)
		{
			if (vector is null || !vector.IsValid())
				throw new ArgumentNullException(nameof(vector));

			return vector.ApplyToClone(v => v.Power(power));
		}

		/// <summary>
		/// Compute the dot (inner) product result of the given <paramref name="left"/> and <paramref name="right"/> vectors.
		/// </summary>
		/// <param name="left">One original vector as the left operand</param>
		/// <param name="right">One original vector as the right operand</param>
		/// <returns>A <typeparamref name="T"/> which is the dot (inner) product result of the given <paramref name="left"/> and <paramref name="right"/> vectors</returns>
		public static T operator *(BaseVector<T> left, BaseVector<T> right)
		{
			if (left is null || !left.IsValid())
				throw new ArgumentNullException(nameof(left));
			if (right is null || !right.IsValid())
				throw new ArgumentNullException(nameof(right));

			return left.Dot(right);
		}

		/// <summary>
		/// Create a new <see cref="BaseVector{T}"/> which is the (point-wise) addition result of the given <paramref name="left"/> and <paramref name="right"/> vectors.
		/// </summary>
		/// <param name="left">One original vector as the left operand</param>
		/// <param name="right">One original vector as the right operand</param>
		/// <returns>A new <see cref="BaseVector{T}"/> which is the addition result of the given <paramref name="left"/> and <paramref name="right"/> vectors</returns>
		public static BaseVector<T> operator +(BaseVector<T> left, BaseVector<T> right)
		{
			if (left is null || !left.IsValid())
				throw new ArgumentNullException(nameof(left));
			if (right is null || !right.IsValid())
				throw new ArgumentNullException(nameof(right));

			return left.AddVector(right, Const<T>.One);
		}

		/// <summary>
		/// Create a new <see cref="BaseVector{T}"/> which is the (point-wise) subtraction result of the given <paramref name="left"/> and <paramref name="right"/> vectors.
		/// </summary>
		/// <param name="left">One original vector as the left operand</param>
		/// <param name="right">One original vector as the right operand</param>
		/// <returns>A new <see cref="BaseVector{T}"/> which is the subtraction result of the given <paramref name="left"/> and <paramref name="right"/> vectors</returns>
		public static BaseVector<T> operator -(BaseVector<T> left, BaseVector<T> right)
		{
			if (left is null || !left.IsValid())
				throw new ArgumentNullException(nameof(left));
			if (right is null || !right.IsValid())
				throw new ArgumentNullException(nameof(right));

			return left.AddVector(right, Const<T>.MinusOne);
		}

		/// <summary>
		/// Create a new <see cref="BaseVector{T}"/> which is the multiplication result of the given <paramref name="vector"/> and <paramref name="scalar"/>
		/// </summary>
		/// <param name="vector">The original vector to multiply</param>
		/// <param name="scalar">The scalar of type <typeparamref name="T"/> to multiply</param>
		/// <returns>A new <see cref="BaseVector{T}"/> which is the multiplication result of the given <paramref name="vector"/> and <paramref name="scalar"/></returns>
		public static BaseVector<T> operator *(BaseVector<T> vector, T scalar)
		{
			if (vector is null || !vector.IsValid())
				throw new ArgumentNullException(nameof(vector));

			return vector.ApplyToClone(v => v.Scale(scalar));
		}

		/// <summary>
		/// Create a new <see cref="BaseVector{T}"/> which is the negation result of the given <paramref name="vector"/>
		/// </summary>
		/// <param name="vector">The original vector to negate</param>
		/// <returns>A new <see cref="BaseVector{T}"/> which is the negation result of the given <paramref name="vector"/></returns>
		public static BaseVector<T> operator -(BaseVector<T> vector) => vector * Const<T>.MinusOne;

		/// <summary>
		/// Create a new <see cref="BaseVector{T}"/> which is the multiplication result of the given <paramref name="vector"/> and <paramref name="scalar"/>
		/// </summary>
		/// <param name="vector">The original vector to multiply</param>
		/// <param name="scalar">The scalar of type <typeparamref name="T"/> to multiply</param>
		/// <returns>A new <see cref="BaseVector{T}"/> which is the multiplication result of the given <paramref name="vector"/> and <paramref name="scalar"/></returns>
		public static BaseVector<T> operator *(T scalar, BaseVector<T> vector) => vector * scalar;

		/// <summary>
		/// Create a new <see cref="BaseVector{T}"/> which is the division result of the given <paramref name="vector"/> and <paramref name="scalar"/>
		/// </summary>
		/// <param name="vector">The original vector to be divided</param>
		/// <param name="scalar">The scalar of type <typeparamref name="T"/> to divide</param>
		/// <returns>A new <see cref="BaseVector{T}"/> which is the multiplication result of the given <paramref name="vector"/> and <paramref name="scalar"/></returns>
		public static BaseVector<T> operator /(BaseVector<T> vector, T scalar) => vector * scalar.NativeReciprocal();

		/// <summary>
		/// Create a new <see cref="BaseVector{T}"/> which is the multiplication result of the given left <paramref name="matrix"/> and right <paramref name="vector"/>.
		/// </summary>
		/// <param name="vector">The input vector to be multiplied at the right side</param>
		/// <param name="matrix">The input matrix to be multiplied at the left side</param>
		/// <returns>A new <see cref="BaseVector{T}"/> which is the multiplication result of the given left <paramref name="matrix"/> and right <paramref name="vector"/></returns>
		public static BaseVector<T> operator *(BaseMatrix<T> matrix, BaseVector<T> vector)
		{
			if (matrix is null || !matrix.IsValid())
				throw new ArgumentNullException(nameof(matrix));
			if (vector is null || !vector.IsValid())
				throw new ArgumentNullException(nameof(vector));

			return vector.AddMatrixMultiplyVector(matrix, vector, Const<T>.One);
		}

		/// <summary>
		/// Create a new <see cref="BaseVector{T}"/> which is the multiplication result of the given left <paramref name="vector"/> and right <paramref name="matrix"/>.
		/// </summary>
		/// <param name="vector">The input vector to be multiplied at the left side</param>
		/// <param name="matrix">The input matrix to be multiplied at the right side</param>
		/// <returns>A new <see cref="BaseVector{T}"/> which is the multiplication result of the given left <paramref name="vector"/> and right <paramref name="matrix"/></returns>
		public static BaseVector<T> operator *(BaseVector<T> vector, BaseMatrix<T> matrix)
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
