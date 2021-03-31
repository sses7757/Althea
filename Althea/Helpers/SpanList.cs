using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Althea.Linq;


namespace Althea.Helpers
{
	#region debug
	internal sealed class SpanListDebugView<T> where T : notnull
	{
		private readonly T[] _array;

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public T[] Items => _array;

		public SpanListDebugView(SpanList<T> span)
		{
			_array = span.ToArray();
		}
	}
	#endregion

	/// <summary>
	/// The list-like span which support add, remove whose internal implementation simply utilizes a fixed-sized <see cref="Span{T}"/>
	/// </summary>
	/// <typeparam name="T">The data type</typeparam>
	[DebuggerTypeProxy(typeof(SpanListDebugView<>))]
	[DebuggerDisplay("{ToString(),raw}")]
	public ref struct SpanList<T> where T : notnull
	{
		#region basic
		private readonly Span<T> _span;

		private int _size;

		/// <summary>
		/// Check whether this <see cref="SpanList{T}"/> is empty or not
		/// </summary>
		public bool IsEmpty {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this._size > 0;
		}

		/// <summary>
		/// Get an empty <see cref="SpanList{T}"/> without underlying <see cref="Span{T}"/>
		/// </summary>
		public static SpanList<T> Empty => default;

		/// <summary>
		/// Create an empty <see cref="SpanList{T}"/> with the underlying <see cref="Span{T}"/> as the input <paramref name="span"/>
		/// </summary>
		/// <param name="span">The input underlying <see cref="Span{T}"/></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SpanList(Span<T> span)
		{
			this._span = span;
			this._size = 0;
		}

		/// <summary>
		/// Get the underlying <see cref="Span{T}"/> of this <see cref="SpanList{T}"/>
		/// </summary>
		public Span<T> UnderlyingSpan {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this._span;
		}
		#endregion

		#region indexer
		/// <summary>
		/// Get the reference of the element at <paramref name="index"/> of this <see cref="SpanList{T}"/>
		/// </summary>
		/// <param name="index">The index of the element to get reference</param>
		/// <returns>The reference of the element at <paramref name="index"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="index"/> is out of range</exception>
		public ref T this[int index] {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				if (index >= this._size)
					throw new ArgumentOutOfRangeException(nameof(index), index, Resources.Parameter.InvalidValue);
				return ref this._span[index];
			}
		}

		/// <summary>
		/// Forms a slice out of the current <see cref="SpanList{T}"/> <paramref name="start"/> at a specified index for a specified <paramref name="count"/>
		/// </summary>
		/// <param name="start">The index at which to begin this slice</param>
		/// <param name="count">The desired length for the slice</param>
		/// <returns>A <see cref="Span{T}"/> that consists of <paramref name="count"/> elements from the current <see cref="SpanList{T}"/> starting at <paramref name="start"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="start"/> or <paramref name="count"/> is out of range</exception>
		public Span<T> Slice(int start, int count)
		{
			if (start >= this._size)
				throw new ArgumentOutOfRangeException(nameof(start), start, Resources.Parameter.InvalidValue);
			if (start + count >= this._size)
				throw new ArgumentOutOfRangeException(nameof(count), start, Resources.Parameter.InvalidValue);
			return this._span.Slice(start, count);
		}

		/// <summary>
		/// Get the number of filled elements of this <see cref="SpanList{T}"/>
		/// </summary>
		public int Count {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this._size;
		}

		/// <summary>
		/// Get the maximum number of elements allowed of this <see cref="SpanList{T}"/>
		/// </summary>
		public int Capacity {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this._span.Length;
		}

		/// <summary>
		/// Get the equivalent <see cref="Span{T}"/> of the current <see cref="SpanList{T}"/> (with the same size)
		/// </summary>
		/// <returns>The equivalent <see cref="Span{T}"/> of the current <see cref="SpanList{T}"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Span<T> AsSpan() => this._span[..this._size];

		/// <summary>
		/// Append the given <paramref name="value"/> to this <see cref="SpanList{T}"/>
		/// </summary>
		/// <param name="value">The value of type <typeparamref name="T"/> to be added</param>
		/// <exception cref="InvalidOperationException">If the <paramref name="value"/> cannot be appended</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Add(T value)
		{
			if (this._size == this._span.Length)
				throw new InvalidOperationException(Resources.Other.ListCannotAdd);
			this._span[this._size++] = value;
		}

		/// <summary>
		/// Append the given <paramref name="values"/> to this <see cref="SpanList{T}"/>
		/// </summary>
		/// <param name="values">The values as a <see cref="ReadOnlySpan{T}"/> of <typeparamref name="T"/> to be appended</param>
		/// <exception cref="InvalidOperationException">If the <paramref name="values"/> cannot be appended</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void AddRange(ReadOnlySpan<T> values)
		{
			if (values.IsEmpty)
				return;
			if (this._size + values.Length > this._span.Length)
				throw new InvalidOperationException(Resources.Other.ListCannotAdd);
			values.CopyTo(this._span[this._size..]);
			this._size += values.Length;
		}

		/// <summary>
		/// Remove the element at <paramref name="index"/> of this <see cref="SpanList{T}"/>
		/// </summary>
		/// <param name="index">The index of the element to be removed</param>
		/// <returns>The removed element</returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="index"/> is out of range</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe T Remove(int index)
		{
			if (index < 0 || index >= this._size)
				throw new ArgumentOutOfRangeException(nameof(index));
			this._size--;
			// shortcut
			if (index == this._size)
			{
				return this._span[this._size];
			}
			// otherwise
			T value = this._span[index];
			// allocate temp
			var heapArray = this._size.CheckStackLimit<T>(out int sizeT);
			var tmpPtr = stackalloc byte[heapArray is null ? 0 : sizeT * this._size];
			Span<T> temp = heapArray ?? new Span<T>(tmpPtr, this._size);
			// copy
			if (index > 0)
				this._span[..index].CopyTo(temp);
			this._span[(index + 1)..(this._size + 1)].CopyTo(temp[index..]);
			temp.CopyTo(this._span);
			// return
			return value;
		}

		/// <summary>
		/// Clear this <see cref="SpanList{T}"/> and set the size to 0
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Clear()
		{
			this._span.Clear();
			this._size = 0;
		}

		/// <summary>
		/// Clear this <see cref="SpanList{T}"/> with clear <paramref name="action"/> and set the size to 0
		/// </summary>
		/// <param name="action">The <see cref="Action{T}"/> used to clear the elements</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Clear(Action<T> action)
		{
			foreach (var item in this)
			{
				action.Invoke(item);
			}
			this._span.Clear();
			this._size = 0;
		}
		#endregion

		#region equality
		/// <summary>
		/// Equality operator
		/// </summary>
		public static bool operator ==(SpanList<T> left, SpanList<T> right)
		{
			return left._span == right._span && left._size == right._size;
		}

		/// <summary>
		/// Inequality operator
		/// </summary>
		public static bool operator !=(SpanList<T> left, SpanList<T> right)
		{
			return !(left == right);
		}

#pragma warning disable CS0809
		/// <summary>
		/// Checks whether the given <paramref name="obj"/> is the same as this one
		/// </summary>
		/// <param name="obj">The given object</param>
		/// <returns>Equals or not</returns>
		[Obsolete("Equals() on SpanList will always throw an exception. Use == instead.")]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public override bool Equals(object? obj)
		{
			throw new NotSupportedException();
		}
#pragma warning restore CS0809

		/// <summary>
		/// Get the hash code of this <see cref="SpanList{T}"/>
		/// </summary>
		/// <returns>The hash code</returns>
		public override int GetHashCode() => this._span[..this._size].HashCodeOfSpan();

		/// <summary>
		/// Get the enumerator (a <see cref="Span{T}.Enumerator"/>) of this <see cref="SpanList{T}"/>
		/// </summary>
		/// <returns>The enumerator of this <see cref="SpanList{T}"/></returns>
		public Span<T>.Enumerator GetEnumerator()
		{
			return this.AsSpan().GetEnumerator();
		}
		#endregion

		#region convert
		/// <summary>
		/// Copy the current <see cref="SpanList{T}"/> to the <paramref name="destination"/> <see cref="Span{T}"/>
		/// </summary>
		/// <param name="destination">The <see cref="Span{T}"/> to copy to</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void CopyTo(Span<T> destination)
		{
			this.AsSpan().CopyTo(destination);
		}

		/// <summary>
		/// Implicitly convert the given <paramref name="list"/> to a <see cref="Span{T}"/>
		/// </summary>
		/// <param name="list">The <see cref="SpanList{T}"/> to be converted</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator Span<T>(SpanList<T> list)
		{
			return list.AsSpan();
		}

		/// <summary>
		/// Implicitly convert the given <paramref name="list"/> to a <see cref="ReadOnlySpan{T}"/>
		/// </summary>
		/// <param name="list">The <see cref="SpanList{T}"/> to be converted</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator ReadOnlySpan<T>(SpanList<T> list)
		{
			return list.AsSpan();
		}

		/// <summary>
		/// Get the string representation of this <see cref="SpanList{T}"/>
		/// </summary>
		/// <returns>The string representation of this <see cref="SpanList{T}"/></returns>
		public override string ToString()
		{
			return $"{nameof(SpanList<T>)}<{typeof(T).Name}>[{this._size}]";
		}

		/// <summary>
		/// Convert this <see cref="SpanList{T}"/> to an array of type <typeparamref name="T"/>
		/// </summary>
		/// <returns>An array of type <typeparamref name="T"/> holding the same values as this <see cref="SpanList{T}"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T[] ToArray()
		{
			if (this._size == 0)
			{
				return Array.Empty<T>();
			}
			T[] array = new T[this._size];
			Unsafe.CopyBlock(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetArrayDataReference(array)), ref Unsafe.As<T, byte>(ref this._span[0]), (uint)this._size);
			return array;
		}

		/// <summary>
		/// Convert this <see cref="SpanList{T}"/> to a <see cref="List{T}"/> of <typeparamref name="T"/>
		/// </summary>
		/// <returns>A <see cref="List{T}"/> of <typeparamref name="T"/> <typeparamref name="T"/> holding the same values as this <see cref="SpanList{T}"/></returns>
		public List<T> ToList()
		{
			List<T> list = new(this.Capacity);
			this.AsSpan().CopyTo(CollectionsMarshal.AsSpan(list));
			return list;
		}
		#endregion
	}
}
