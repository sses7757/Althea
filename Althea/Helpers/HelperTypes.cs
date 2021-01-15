using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Althea.Linq;


namespace Althea.Helpers
{
	/// <summary>
	/// The fixed buffer struct of type <typeparamref name="T"/> and size in bytes = 60
	/// </summary>
	/// <typeparam name="T">any unmanaged struct</typeparam>
	[StructLayout(LayoutKind.Sequential, Size = 60)]
	[UnsafeValueType]
	public unsafe struct FixedBuffer_60<T> : IEquatable<FixedBuffer_60<T>>, IReadOnlyList<T> where T : unmanaged
	{
		#region basic
		private T field;

		private T* Pointer => (T*)Unsafe.AsPointer(ref this.field);

		/// <summary>
		/// The number of elements in this fixed buffer
		/// </summary>
		public int Count => 60 / sizeof(T);

		/// <summary>
		/// Basic indexer of this fixed buffer
		/// </summary>
		/// <param name="index">the index</param>
		/// <returns>the value at <paramref name="index"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="index"/> is out of range</exception>
		public T this[int index] {
			get => index >= 0 && index < this.Count ? Pointer[index] : throw new ArgumentOutOfRangeException(nameof(index));
			set => Pointer[index] = index >= 0 && index < this.Count ? value : throw new ArgumentOutOfRangeException(nameof(index));
		}

		/// <summary>
		/// Get the <see cref="Span{T}"/> of this fixed buffer
		/// </summary>
		/// <returns>The <see cref="Span{T}"/> of this fixed buffer</returns>
		public Span<T> AsSpan() => new Span<T>(this.Pointer, this.Count);

		/// <summary>
		/// Returns an enumerator that iterates through the collection.
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the collection.</returns>
		public IEnumerator<T> GetEnumerator()
		{
			for (int i = 0; i < this.Count; i++)
			{
				yield return this[i];
			}
		}

		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		#endregion

		#region equality
		/// <summary>
		/// Whether this == <paramref name="other"/>
		/// </summary>
		/// <param name="other">another <see cref="FixedBuffer_60{T}"/> to compare</param>
		/// <returns>this == <paramref name="other"/></returns>
		public bool Equals(FixedBuffer_60<T> other)
		{
			return this.SequenceEqual(other);
		}

		/// <summary>
		/// Override <see cref="ValueType.Equals(object?)"/> to check whether this == <paramref name="obj"/>
		/// </summary>
		/// <param name="obj">another object to compare</param>
		/// <returns>this == <paramref name="obj"/></returns>
		public override bool Equals(object obj)
		{
			return obj is FixedBuffer_60<T> buffer && this.Equals(buffer);
		}

		/// <summary>
		/// Override <see cref="ValueType.GetHashCode"/> to get the hash code this <see cref="FixedBuffer_60{T}"/>.
		/// </summary>
		/// <returns>The hash code</returns>
		public override int GetHashCode()
		{
			return this.HashCodeOfArray();
		}

		/// <summary>
		/// Equality operator
		/// </summary>
		public static bool operator ==(FixedBuffer_60<T> left, FixedBuffer_60<T> right)
		{
			return left.Equals(right);
		}

		/// <summary>
		/// Inequality operator
		/// </summary>
		public static bool operator !=(FixedBuffer_60<T> left, FixedBuffer_60<T> right)
		{
			return !(left == right);
		}
		#endregion

		#region string related
		/// <summary>
		/// Return the string representation of this <see cref="FixedBuffer_60{T}"/>
		/// </summary>
		/// <returns>the string representation of this <see cref="FixedBuffer_60{T}"/></returns>
		public override string ToString()
		{
			return string.Join(',', this);
		}
		#endregion
	}
}
