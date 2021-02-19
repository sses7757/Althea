using System;

using Althea.Linq;
using Althea.Helpers;


namespace Althea.Arrays
{
	/// <summary>
	/// The abstract array class for any kind of array. It is the top level abstract of all built-in array classes. It implements the <see cref="IDisposable"/> and <see cref="ICloneable{T}"/> interface.
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct that implements <see cref="IFormattable"/> and <see cref="IEquatable{T}"/> as the data type</typeparam>
	public abstract class AbstractArray<T> : IDisposable, ICloneable<AbstractArray<T>> where T : unmanaged, IFormattable, IEquatable<T>
	{
		#region members
		/// <summary>
		/// The member that actually stores the size
		/// </summary>
		protected readonly SizedFixedBuffer_128<long> m_size = default;

		private readonly long m_length = 0;
		#endregion

		#region properties
		/// <summary>
		/// Get the rank of this array as a <see cref="int"/>
		/// </summary>
		public int Rank => this.m_size.Count;

		/// <summary>
		/// Get the size of this mutable array as a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/>
		/// </summary>
		public ReadOnlySpan<long> Size => this.m_size.AsSpan();

		/// <summary>
		/// Total appearance length of the array, in <typeparamref name="T"/> rather than bytes
		/// </summary>
		public long Length => this.m_length;
		#endregion

		#region initialize and dispose
		/// <summary>
		/// Create a new instance of <see cref="AbstractArray{T}"/> by indicating the size of each rank.
		/// </summary>
		/// <param name="size">The size of this abstract array as a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="size"/> is of length 0</exception>
		/// <exception cref="NotSupportedException">If <paramref name="size"/> has length larger than 16</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="size"/> contains any non-positive value</exception>
		protected AbstractArray(ReadOnlySpan<long> size)
		{
			if (size.Length == 0)
				throw new ArgumentNullException(nameof(size));
			if (size.Length > 16)
				throw new NotSupportedException(Resources.Parameter.WrongSize);
			if (size.Length == 1 && size[0] == 0)
				return;
			if (size.Any(static s => s <= 0))
				throw new ArgumentOutOfRangeException(nameof(size), Resources.Parameter.MustPositive);

			this.m_size = new SizedFixedBuffer_128<long>(size);
			this.m_length = size.Prod();
		}

		/// <summary>
		/// Get a <see cref="bool"/> to indicate whether this array is disposed or not
		/// </summary>
		protected bool Disposed { private set; get; } = false;

		/// <summary>
		/// The dispose method invoked by the user to release the (possible) underlying unmanaged memory
		/// </summary>
		public void Dispose()
		{
			this.Dispose(true);
			this.Disposed = true;
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// When implemented by a derived class, actually the dispose this array.
		/// </summary>
		/// <param name="disposing">Dispose managed resources or not</param>
		protected abstract void Dispose(bool disposing);
		#endregion

		#region overrides
		/// <summary>
		/// When implemented by a derived class, override <see cref="object.ToString"/> to get the string representation of this array.
		/// </summary>
		/// <returns>The string representation of this array</returns>
		public abstract override string ToString();

		/// <summary>
		/// When implemented by a derived class, override <see cref="object.GetHashCode"/> to get the hash code this array.
		/// </summary>
		/// <returns>The hash code of this array</returns>
		public abstract override int GetHashCode();

		/// <summary>
		/// When implemented by a derived class, check whether this object is equal to another one. The default implementation <b>only</b> compares the size.
		/// </summary>
		/// <param name="obj">The other <see cref="AbstractArray{T}"/> to compare with</param>
		/// <returns>True if this == <paramref name="obj"/></returns>
		public override bool Equals(object? obj) => obj is AbstractArray<T> arr && this.m_size == arr.m_size;
		#endregion

		#region operators
		/// <summary>
		/// Whether the two <see cref="AbstractArray{T}"/> are equal
		/// </summary>
		/// <param name="left">The left operand</param>
		/// <param name="right">The right operand</param>
		/// <returns><paramref name="left"/> == <paramref name="right"/></returns>
		public static bool operator ==(AbstractArray<T> left, AbstractArray<T> right)
		{
			if (left is null && right is null)
				return true;
			else if (left is null || right is null)
				return false;
			else if (left.Length == 0 && right.Length == 0)
				return true; // zero length arrays are regarded as the same
			else if (left.Length == 0 || right.Length == 0)
				return false;
			else
				return left.Equals(right);
		}

		/// <summary>
		/// Whether the two <see cref="AbstractArray{T}"/> are not equal
		/// </summary>
		/// <param name="left">The left operand</param>
		/// <param name="right">The right operand</param>
		/// <returns><paramref name="left"/> != <paramref name="right"/></returns>
		public static bool operator !=(AbstractArray<T> left, AbstractArray<T> right) => !(left == right);
		#endregion

		#region abstracts
		/// <summary>
		/// When implemented by a derived class, deep clone the array, the mutable status will not be copied.
		/// </summary>
		/// <returns>The cloned array</returns>
		public abstract AbstractArray<T> Clone();

		/// <summary>
		/// When implemented by a derived class, cast this array into another data type <typeparamref name="TOut"/> by creating a new array of <typeparamref name="TOut"/>.
		/// </summary>
		/// <typeparam name="TOut">The data type to cast to</typeparam>
		/// <returns>The casted <see cref="AbstractArray{T}"/> of type <typeparamref name="TOut"/>.</returns>
		public abstract AbstractArray<TOut> DataTypeCast<TOut>() where TOut : unmanaged, IFormattable, IEquatable<TOut>;

		/// <summary>
		/// When implemented by a derived class, print out the array.
		/// </summary>
		/// <param name="overrideSetting">Override global settings in <see cref="Settings"/></param>
		/// <returns>The detailed string representation</returns>
		public abstract string Print(PrintSettings? overrideSetting = null);
		#endregion
	}
}
