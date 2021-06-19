using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Althea.Linq;


namespace Althea.Helpers
{
	/// <summary>
	/// The fixed buffer struct of type <typeparamref name="T"/> and size in bytes = __placeholder__
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct that implements <see cref="IEquatable{T}"/></typeparam>
	[StructLayout(LayoutKind.Sequential, Size = __placeholder__)]
	[UnsafeValueType]
	[DebuggerTypeProxy(typeof(FixedBufferDebugView<>))]
	[DebuggerDisplay("{ToString(),raw}")]
	public unsafe struct FixedBuffer___placeholder__<T> : IEquatable<FixedBuffer___placeholder__<T>>, IFixedBuffer<T>, IAsSpan<T> where T : unmanaged, IEquatable<T>
	{
		#region basic
		private static readonly int _count = __placeholder__ / sizeof(T);

		private T field;

		/// <summary>
		/// Create a new array of <typeparamref name="T"/> containing the elements of this fixed buffer
		/// </summary>
		/// <returns>The array containing the elements of this fixed buffer</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T[] ToArray()
		{
			T[] array = new T[_count];
			fixed (void* t = &this.field)
			fixed (T* a = array)
			{
				Unsafe.CopyBlock(a, t, __placeholder__);
			}
			return array;
		}

		/// <summary>
		/// Change data type of this fixed buffer from <typeparamref name="T"/> to <typeparamref name="TOut"/>
		/// </summary>
		/// <typeparam name="TOut">The output data type, any unmanaged struct</typeparam>
		/// <returns>The fixed buffer with same byte values as this one whose data type is <typeparamref name="TOut"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public FixedBuffer___placeholder__<TOut> As<TOut>() where TOut : unmanaged, IEquatable<TOut>
		{
			var newBuffer = new FixedBuffer___placeholder__<TOut>();
			fixed (void* t = &this.field)
			{
				Unsafe.CopyBlock(&newBuffer, t, __placeholder__);
			}
			return newBuffer;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		IFixedBuffer<TOut> IFixedBuffer<T>.As<TOut>() => this.As<TOut>();

		/// <summary>
		/// Copy the data from the given <paramref name="span"/> to this fixed buffer
		/// </summary>
		/// <param name="span">The given <see cref="ReadOnlySpan{T}"/> to copy from</param>
		/// <param name="offset">The offset to start copying in <typeparamref name="T"/></param>
		/// <exception cref="ArgumentException">If the length of <paramref name="span"/> is too large</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offset"/> is out of boundary</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void CopyFromSpan(ReadOnlySpan<T> span, int offset = 0)
		{
			if (offset < 0 || offset >= _count)
				throw new ArgumentOutOfRangeException(nameof(offset), offset, Resources.Parameter.InvalidValue);
			if (span.Length + offset > _count)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(span));

			int size = Math.Min(span.Length, _count - offset);
			fixed (void* t = &this.field)
			{
				var temp = new Span<T>((T*)t + offset, size);
				span.CopyTo(temp);
			}
		}

		/// <summary>
		/// Copy the data from this fixed buffer to the given <paramref name="span"/>
		/// </summary>
		/// <param name="span">The given <see cref="ReadOnlySpan{T}"/> to copy to</param>
		/// <param name="offset">The offset to start copying in <typeparamref name="T"/></param>
		/// <exception cref="ArgumentException">If the length of <paramref name="span"/> is too large</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offset"/> is out of boundary</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void CopyToSpan(Span<T> span, int offset = 0)
		{
			if (offset < 0 || offset >= _count)
				throw new ArgumentOutOfRangeException(nameof(offset), offset, Resources.Parameter.InvalidValue);
			if (span.Length + offset > _count)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(span));

			int size = Math.Min(span.Length, _count - offset);
			fixed (void* t = &this.field)
			{
				var temp = new ReadOnlySpan<T>((T*)t + offset, size);
				temp.CopyTo(span);
			}
		}

		/// <summary>
		/// Convert this fixed buffer to a new <typeparamref name="TStruct"/> by copying the values from <paramref name="copyStart"/> byte by byte
		/// </summary>
		/// <typeparam name="TStruct">The output struct type</typeparam>
		/// <param name="copyStart">The start position to copy in bytes</param>
		/// <returns>The created <typeparamref name="TStruct"/></returns>
		/// <exception cref="InvalidOperationException">If the size of <typeparamref name="TStruct"/> is larger than the size of this fixed buffer minus <paramref name="copyStart"/></exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public TStruct ToStruct<TStruct>(int copyStart = 0) where TStruct : struct
		{
			int size = Unsafe.SizeOf<TStruct>();
			if (size + copyStart > __placeholder__)
				throw new InvalidOperationException(Resources.Other.InvalidGeneric);
			var s = new TStruct();
			fixed (void* t = &this.field)
			{
				Unsafe.CopyBlock(Unsafe.AsPointer(ref s), (byte*)t + copyStart, (uint)size);
			}
			return s;
		}

		/// <summary>
		/// Copy the values in <paramref name="struct"/> to this fixed buffer from <paramref name="copyStart"/> byte by byte
		/// </summary>
		/// <typeparam name="TStruct">The input struct type</typeparam>
		/// <param name="struct">The structure to copy</param>
		/// <param name="copyStart">The start position to copy of this fixed buffer in bytes</param>
		/// <exception cref="InvalidOperationException">If the size of <typeparamref name="TStruct"/> is larger than the size of this fixed buffer minus <paramref name="copyStart"/></exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void FromStruct<TStruct>(TStruct @struct, int copyStart = 0) where TStruct : struct
		{
			int size = Unsafe.SizeOf<TStruct>();
			if (size + copyStart > __placeholder__)
				throw new InvalidOperationException(Resources.Other.InvalidGeneric);
			fixed (void* t = &this.field)
			{
				Unsafe.CopyBlock((byte*)t + copyStart, Unsafe.AsPointer(ref @struct), (uint)size);
			}
		}

		/// <summary>
		/// Create a <see cref="Span{T}"/> from this fixed buffer
		/// </summary>
		/// <returns>The <see cref="Span{T}"/> referring to this fixed buffer</returns>
		/// <param name="size">The size of the span, default 0 means all</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="size"/> is out of range</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Span<T> AsSpan(int size = 0)
		{
			if (size < 0 || size > _count)
				throw new ArgumentOutOfRangeException(nameof(size), size, Resources.Parameter.InvalidValue);
			return MemoryMarshal.CreateSpan(ref this.field, size == 0 ? _count : size);
		}
		#endregion

		#region indexer
		/// <summary>
		/// Get the number of values whose value is not default(<typeparamref name="T"/>)
		/// </summary>
		public int NonDefaults {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				fixed (void* t = &this.field)
				{
					T* ptr = (T*)t;
					int result = 0;
					for (int i = 0; i < _count; i++)
					{
						if (!ptr[i].Equals(default))
							result++;
					}
					return result;
				}
			}
		}

		/// <summary>
		/// The number of elements in this fixed buffer
		/// </summary>
		public int Count {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _count;
		}

		/// <summary>
		/// Basic indexer of this fixed buffer
		/// </summary>
		/// <param name="index">The index</param>
		/// <returns>The value at <paramref name="index"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="index"/> is out of range</exception>
		public T this[int index] {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				if (index < 0 || index >= _count)
					throw new ArgumentOutOfRangeException(nameof(index), index, Resources.Parameter.InvalidValue);
				fixed (void* t = &this.field)
				{
					return ((T*)t)[index];
				}
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set {
				if (index < 0 || index >= _count)
					throw new ArgumentOutOfRangeException(nameof(index), index, Resources.Parameter.InvalidValue);
				fixed (void* t = &this.field)
				{
					((T*)t)[index] = value;
				}
			}
		}

		/// <summary>
		/// Returns an enumerator that iterates through the collection.
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the collection.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public IEnumerator<T> GetEnumerator()
		{
			for (int i = 0; i < _count; i++)
			{
				yield return this[i];
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

		/// <summary>
		/// Check whether the this fixed buffer contains the given <paramref name="value"/> 
		/// </summary>
		/// <param name="value">The value to find</param>
		/// <returns>Whether the this fixed buffer contains the given <paramref name="value"/> </returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Contains(T value)
		{
			fixed (void* t = &this.field)
			{
				return new ReadOnlySpan<T>(t, _count).Contains(value);
			}
		}
		#endregion

		#region equality
		/// <summary>
		/// Whether this == <paramref name="other"/>
		/// </summary>
		/// <param name="other">another <see cref="FixedBuffer___placeholder__{T}"/> to compare</param>
		/// <returns>this == <paramref name="other"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(FixedBuffer___placeholder__<T> other)
		{
			fixed (void* t = &this.field)
			{
				return new ReadOnlySpan<T>(t, _count).SequenceEqual(new ReadOnlySpan<T>(&other.field, _count));
			}
		}

		/// <summary>
		/// Override <see cref="ValueType.Equals(object?)"/> to check whether this == <paramref name="obj"/>
		/// </summary>
		/// <param name="obj">another object to compare</param>
		/// <returns>this == <paramref name="obj"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override bool Equals(object? obj)
		{
			return obj is FixedBuffer___placeholder__<T> buffer && this.Equals(buffer);
		}

		/// <summary>
		/// Override <see cref="ValueType.GetHashCode"/> to get the hash code this <see cref="FixedBuffer___placeholder__{T}"/>.
		/// </summary>
		/// <returns>The hash code</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode()
		{
			fixed (void* t = &this.field)
			{
				var temp = new ReadOnlySpan<T>(t, _count);
				return temp.HashCodeOfSpan();
			}
		}

		/// <summary>
		/// Equality operator
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(FixedBuffer___placeholder__<T> left, FixedBuffer___placeholder__<T> right)
		{
			return left.Equals(right);
		}

		/// <summary>
		/// Inequality operator
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(FixedBuffer___placeholder__<T> left, FixedBuffer___placeholder__<T> right)
		{
			return !(left == right);
		}
		#endregion

		#region string related
		/// <summary>
		/// Return the string representation of this <see cref="FixedBuffer___placeholder__{T}"/>
		/// </summary>
		/// <returns>the string representation of this <see cref="FixedBuffer___placeholder__{T}"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override string ToString()
		{
			return $"Fixed Buffer [Size={_count}, Type={typeof(T).GetGenericString()}]";
		}
		#endregion
	}
}