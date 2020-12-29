using System;
using System.Collections.Generic;

using Althea.Linq;
using Althea.Helpers;


namespace Althea.Arrays
{
	/// <summary>
	/// The abstract array class for device / host array. It is the top level abstract of all built-in array classes. It inherits the <see cref="IDisposable"/> and <see cref="ICloneable"/> interface.
	/// </summary>
	/// <typeparam name="T">the supported data type</typeparam>
	public abstract class AbstractArray<T> : IDisposable, ICloneable where T : unmanaged, IFormattable, IEquatable<T>
	{
		#region members
		/// <summary>
		/// The array's size, read-only
		/// </summary>
		public IReadOnlyList<long> Size { get; }

		/// <summary>
		/// The accumulated product result of <see cref="Size"/> (with the last one as the <see cref="Length"/>)
		/// </summary>
		protected IReadOnlyList<long> SizeProd { get; }
		#endregion

		#region other properties
		/// <summary>
		/// Total appearance length of the array, in <typeparamref name="T"/> rather than bytes
		/// </summary>
		public long Length => this.SizeProd[^1];
		#endregion

		#region initialize and dispose
		/// <summary>
		/// New instance of <see cref="AbstractArray{T}"/> constructor.
		/// </summary>
		/// <param name="size">size of this abstract array</param>
		protected AbstractArray(IReadOnlyList<long> size)
		{
			if (size is null || size.Count == 0)
				throw new ArgumentNullException(nameof(size));
			this.Size = size;
			this.SizeProd = size.AccumulateProd();
		}

		/// <summary>
		/// This array is disposed or not
		/// </summary>
		public bool Disposed { protected internal set; get; } = false;

		/// <summary>
		/// The dispose method invoked by the finalize method (or yourself) to release the memory.
		/// </summary>
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// The function that actually implements the dispose functionality.
		/// </summary>
		/// <param name="disposing">dispose managed resources or not</param>
		protected abstract void Dispose(bool disposing);
		#endregion

		#region overrides
		/// <summary>
		/// Override <see cref="object.ToString"/> to get the string representation of this array.
		/// </summary>
		/// <returns>String representation of this array</returns>
		public abstract override string ToString();

		/// <summary>
		/// Override <see cref="object.GetHashCode"/> to get the hash code this array.
		/// </summary>
		/// <returns>The hash code</returns>
		public abstract override int GetHashCode();

		/// <summary>
		/// Whether this object is equal to another, the shapes / sizes are also compared
		/// </summary>
		/// <param name="obj">another <see cref="AbstractArray{T}"/></param>
		public abstract override bool Equals(object obj);
		#endregion

		#region operators
		/// <summary>
		/// Whether the two <see cref="AbstractArray{T}"/> are equal, the shapes / sizes are not compared
		/// </summary>
		/// <param name="left">left operand</param>
		/// <param name="right">right operand</param>
		/// <returns><paramref name="left"/> == <paramref name="right"/></returns>
		public static bool operator ==(AbstractArray<T> left, AbstractArray<T> right)
		{
			if (left is null && right is null)
				return true;
			else if (left is null || right is null)
				return false;
			else if (left.Length == 0 && right.Length == 0)
				return true; // zero length arrays are regarded as a different type
			else if (left.Length == 0 || right.Length == 0)
				return false;
			else if (!left.Size.SequenceEqual(right.Size))
				return false;
			else
				return left.Equals(right);
		}

		/// <summary>
		/// See == of <see cref="AbstractArray{T}"/>
		/// </summary>
		/// <param name="a1"></param>
		/// <param name="a2"></param>
		/// <returns><paramref name="a1"/> != <paramref name="a2"/></returns>
		public static bool operator !=(AbstractArray<T> a1, AbstractArray<T> a2)
		{
			return !(a1 == a2);
		}
		#endregion

		#region other abstracts
		/// <summary>
		/// Check if this <see cref="AbstractArray{T}"/> share some memory / data with <paramref name="another"/> one
		/// </summary>
		/// <param name="another">another <see cref="AbstractArray{T}"/> to check</param>
		/// <returns>True if they do share some memory / data, false otherwise</returns>
		public abstract bool ShareMemoryWith(AbstractArray<T> another);

		/// <summary>
		/// Deep clone the array, the mutable status will not be copied.
		/// </summary>
		/// <returns>The cloned array</returns>
		public abstract object Clone();

		/// <summary>
		/// Create a new array with same properties (e.g. <see cref="Size"/>) as this one.
		/// </summary>
		/// <returns>The array alike this one.</returns>
		public abstract AbstractArray<T> NewArrayAlike();

		/// <summary>
		/// Print out the array.
		/// </summary>
		/// <param name="overrideSetting">override global settings <see cref="Settings.PrintConfig"/></param>
		/// <returns>detailed string representation</returns>
		public abstract string Print(IReadOnlyDictionary<PrintSetting, int> overrideSetting = null);
		#endregion

		#region abstract array operations
		/// <summary>
		/// Cast this array into another data type <typeparamref name="TOut"/>.
		/// </summary>
		/// <typeparam name="TOut">the data type to cast to</typeparam>
		/// <returns>The casted <see cref="AbstractArray{T}"/>.</returns>
		public abstract AbstractArray<TOut> DataTypeCast<TOut>() where TOut : unmanaged, IFormattable, IEquatable<TOut>;
		#endregion
	}
}
