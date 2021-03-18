using System;

using Althea.Linq;
using Althea.Helpers;
using System.Runtime.CompilerServices;

namespace Althea.Arrays
{
	/// <summary>
	/// The abstract array class for any kind of array. It is the top level abstract of all built-in array classes. It implements the <see cref="IDisposable"/> and <see cref="ICloneable{T}"/> interface.
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
	/// <remarks>
	/// Since the <see cref="AbstractArray{T}"/> may be reference created quite frequently, storing the size as a C# <see cref="Array"/> is rather expensive.<br/>
	/// Thus the C++ equivalent "<c>struct { int rank, long size[16] }</c>" of <see cref="SizedFixedBuffer_128{T}"/> is used instead to reduce the GC pressure.<br/>
	/// Also, the <see cref="AbstractArray{T}"/> has no finalizer and if it is composed of <see cref="ReferenceStorage{T}"/> which still has no finalizer, the instance stays in GC generation 0 which is quite fast in deallocation.<br/>
	/// Therefore, the derived class shall follow the same strategy, such as <see cref="SparseVector{T, TInd}"/> and <see cref="SparseMatrix{T, TInd}"/>.
	/// </remarks>
	public abstract class AbstractArray<T> : IDisposable, ICloneable<AbstractArray<T>> where T : unmanaged
	{
		#region properties
		private readonly long m_length;

		/// <summary>
		/// When implemented by a derived class, get the rank of this array as a <see cref="int"/>
		/// </summary>
		public abstract int Rank { get; }

		/// <summary>
		/// When implemented by a derived class, get the size of this array as a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/>
		/// </summary>
		public abstract ReadOnlySpan<long> Size { get; }

		/// <summary>
		/// Get the total appearance length of the array, in <typeparamref name="T"/> rather than bytes
		/// </summary>
		public long Length {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.m_length;
		}
		#endregion

		#region initialize and dispose
		/// <summary>
		/// Create a new instance of <see cref="AbstractArray{T}"/> by indicating the total appearance length.
		/// </summary>
		/// <param name="length">The total appearance length of this array as a <see cref="long"/>, 0 means an empty array</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="length"/> is negative</exception>
		protected AbstractArray(long length)
		{
			if (length < 0)
				throw new ArgumentOutOfRangeException(nameof(length), length, Resources.Parameter.CannotNegative);

			this.m_length = length;
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
		/// When implemented by a derived class, check whether this array equals to another object.
		/// </summary>
		/// <param name="obj">The other object to compare with</param>
		/// <returns>True if this == <paramref name="obj"/></returns>
		public abstract override bool Equals(object? obj);
		#endregion

		#region operators
		/// <summary>
		/// Whether the two <see cref="AbstractArray{T}"/> are equal
		/// </summary>
		/// <param name="left">The left operand</param>
		/// <param name="right">The right operand</param>
		/// <returns><paramref name="left"/> == <paramref name="right"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(AbstractArray<T>? left, AbstractArray<T>? right)
		{
			if (ReferenceEquals(left, right))
				return true;
			if (left is null && right is null)
				return true;
			if (left is null || right is null)
				return false;
			if (left.Length == 0 && right.Length == 0)
				return true; // zero length arrays are regarded as the same
			if (left.Length == 0 || right.Length == 0)
				return false;
			// else
			return left.Equals(right);
		}

		/// <summary>
		/// Whether the two <see cref="AbstractArray{T}"/> are not equal
		/// </summary>
		/// <param name="left">The left operand</param>
		/// <param name="right">The right operand</param>
		/// <returns><paramref name="left"/> != <paramref name="right"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(AbstractArray<T>? left, AbstractArray<T>? right) => !(left == right);
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
