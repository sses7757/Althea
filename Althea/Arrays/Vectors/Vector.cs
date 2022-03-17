using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Althea.LinearAlgebra;


namespace Althea.Arrays
{
	/// <summary>
	/// The base vector interface.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TSelf">The concrete type that implements this <see cref="IBaseVector{T, TSelf}"/></typeparam>
	public interface IBaseVector<T, TSelf> : IVectorMetric, IValueArray<T, TSelf>, IReadOnlyList<T>
		where T : unmanaged, INumber<T> where TSelf : class, IBaseVector<T, TSelf>
	{
		#region indexing
		/// <summary>
		/// Provide legacy support of C# duck type for <c>this[<see cref="Index"/>]</c> and <c>this[<see cref="Range"/>]</c>
		/// </summary>
		int IReadOnlyCollection<T>.Count => (int)((IVectorMetric)this).Length;

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
			if (index >= ((IVectorMetric)this).Length)
				throw new ArgumentOutOfRangeException(nameof(index), index, Resources.Parameter.InvalidValue);
		}

		/// <summary>
		/// Check whether the given range indicated by <paramref name="offset"/> and <paramref name="length"/> is out of range of this vector
		/// </summary>
		/// <param name="offset">The starting offset index to be checked</param>
		/// <param name="length">The length to be checked</param>
		/// <param name="sub">The sub vector to check which can be null to prevent checking</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offset"/> and/or <paramref name="length"/> is out of range</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected void CheckRange(long offset, long length, IVectorMetric? sub = null)
		{
			if (offset < 0)
				throw new ArgumentOutOfRangeException(nameof(offset), offset, Resources.Parameter.CannotNegative);
			if (offset >= ((IVectorMetric)this).Length)
				throw new ArgumentOutOfRangeException(nameof(offset), offset, Resources.Parameter.InvalidValue);
			if (length < 0)
				throw new ArgumentOutOfRangeException(nameof(length), length, Resources.Parameter.CannotNegative);
			if (offset + length > ((IVectorMetric)this).Length)
				throw new ArgumentOutOfRangeException(nameof(length), length, Resources.Parameter.InvalidValue);
			if (sub is not null)
			{
				if (sub.Length < length)
					throw new ArgumentOutOfRangeException(nameof(length), length, Resources.Parameter.InvalidValue);
			}
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
		/// When implemented by a derived class, get a sub-vector indicated by the given <paramref name="start"/> offset and <paramref name="count"/> and overwrite to <paramref name="overwrite"/>
		/// </summary>
		/// <param name="start">The starting offset of the target sub-vector compared to this vector, in <typeparamref name="T"/></param>
		/// <param name="count">The length of the target sub-vector, in <typeparamref name="T"/></param>
		/// <param name="overwrite">The <typeparamref name="TSelf"/> to be overwritten</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="start"/> and/or <paramref name="count"/> is out of range</exception>
		void GetSlice(long start, long count, TSelf overwrite);

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

		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		#endregion
	}
}
