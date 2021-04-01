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

	internal sealed class SpanMatrixDebugView<T> where T : notnull
	{
		private readonly T[][] _columns;

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public T[][] Columns => _columns;

		public SpanMatrixDebugView(SpanMatrix<T> span)
		{
			_columns = span.ToArray();
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
		public readonly bool IsEmpty {
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
		public readonly Span<T> UnderlyingSpan {
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
		public readonly ref T this[int index] {
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
		public readonly Span<T> Slice(int start, int count)
		{
			if (start >= this._size)
				throw new ArgumentOutOfRangeException(nameof(start), start, Resources.Parameter.InvalidValue);
			if (start + count >= this._size)
				throw new ArgumentOutOfRangeException(nameof(count), count, Resources.Parameter.InvalidValue);
			return this._span.Slice(start, count);
		}

		/// <summary>
		/// Get the number of filled elements of this <see cref="SpanList{T}"/>
		/// </summary>
		public readonly int Count {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this._size;
		}

		/// <summary>
		/// Get the maximum number of elements allowed of this <see cref="SpanList{T}"/>
		/// </summary>
		public readonly int Capacity {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this._span.Length;
		}

		/// <summary>
		/// Get the equivalent <see cref="Span{T}"/> of the current <see cref="SpanList{T}"/> (with the same size)
		/// </summary>
		/// <returns>The equivalent <see cref="Span{T}"/> of the current <see cref="SpanList{T}"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly Span<T> AsSpan() => this._span[..this._size];

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
		public override readonly bool Equals(object? obj)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Get the hash code of this <see cref="SpanList{T}"/>
		/// </summary>
		/// <returns>The hash code</returns>
		[Obsolete("GetHashCode() on SpanList will always throw an exception.")]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public override readonly int GetHashCode()
		{
			throw new NotSupportedException();
		}
#pragma warning restore CS0809

		/// <summary>
		/// Get the enumerator (a <see cref="Span{T}.Enumerator"/>) of this <see cref="SpanList{T}"/>
		/// </summary>
		/// <returns>The enumerator of this <see cref="SpanList{T}"/></returns>
		public readonly Span<T>.Enumerator GetEnumerator()
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
		public readonly void CopyTo(Span<T> destination)
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
		public override readonly string ToString()
		{
			return $"{nameof(SpanList<T>)}<{typeof(T).Name}>[{this._size}]";
		}

		/// <summary>
		/// Convert this <see cref="SpanList{T}"/> to an array of type <typeparamref name="T"/>
		/// </summary>
		/// <returns>An array of type <typeparamref name="T"/> holding the same values as this <see cref="SpanList{T}"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly T[] ToArray()
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
		public readonly List<T> ToList()
		{
			List<T> list = new(this.Capacity);
			this.AsSpan().CopyTo(CollectionsMarshal.AsSpan(list));
			return list;
		}
		#endregion
	}


	/// <summary>
	/// The matrix-like span which is of column major whose internal implementation simply utilizes a fixed-sized <see cref="Span{T}"/>
	/// </summary>
	/// <typeparam name="T"></typeparam>
	[DebuggerTypeProxy(typeof(SpanMatrixDebugView<>))]
	[DebuggerDisplay("{ToString(),raw}")]
	public readonly ref struct SpanMatrix<T> where T : notnull
	{
		#region enumerating
		/// <summary>
		/// The enumerator for a <see cref="SpanMatrix{T}"/>
		/// </summary>
		public ref struct Enumerator
		{
			private readonly SpanMatrix<T> _matrix;

			private int _index;

			/// <summary>
			/// Get the current
			/// </summary>
			public ref T Current {
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => ref this._matrix._span[this._index];
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			internal Enumerator(SpanMatrix<T> span)
			{
				_matrix = span;
				_index = -1;
			}

			/// <summary>
			/// Move to the next element
			/// </summary>
			/// <returns>Whether there is a next element or not</returns>
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public bool MoveNext()
			{
				int num = _index + 1;
				int ld = this._matrix._leadDim;
				if (num % ld > this._matrix._rows)
					num = (num / ld + 1) * ld;
				if (num < ld * this._matrix._cols)
				{
					_index = num;
					return true;
				}
				return false;
			}
		}
		#endregion

		#region basic
		private readonly Span<T> _span;

		private readonly int _rows, _cols, _leadDim;

		/// <summary>
		/// Get the presenting number of rows of this <see cref="SpanMatrix{T}"/>
		/// </summary>
		public readonly int Rows {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this._rows;
		}
		/// <summary>
		/// Get the presenting number of columns of this <see cref="SpanMatrix{T}"/>
		/// </summary>
		public readonly int Cols {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this._cols;
		}
		/// <summary>
		/// Get the leading dimension (the actual number of rows) of this <see cref="SpanMatrix{T}"/>
		/// </summary>
		public readonly int LeadDim {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this._rows;
		}
		/// <summary>
		/// Get the presenting length of this <see cref="SpanMatrix{T}"/>
		/// </summary>
		public readonly int PresentingLength {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this._rows * this._cols;
		}

		/// <summary>
		/// Check whether this <see cref="SpanMatrix{T}"/> is empty or not
		/// </summary>
		public readonly bool IsEmpty {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this._leadDim > 0;
		}

		/// <summary>
		/// Get the underlying <see cref="Span{T}"/> of this <see cref="SpanMatrix{T}"/>
		/// </summary>
		public readonly Span<T> UnderlyingSpan {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this._span;
		}

		/// <summary>
		/// Get an empty <see cref="SpanMatrix{T}"/> without underlying <see cref="Span{T}"/>
		/// </summary>
		public static SpanMatrix<T> Empty => default;

		/// <summary>
		/// Create an empty <see cref="SpanMatrix{T}"/> with the underlying <see cref="Span{T}"/> as the input <paramref name="span"/> and the given <paramref name="leadingDim"/>
		/// </summary>
		/// <param name="span">The input underlying <see cref="Span{T}"/></param>
		/// <param name="rows">The number of desired rows. (The number of columns are calculated.)</param>
		/// <param name="leadingDim">The leading dimension of <paramref name="span"/>, default 0 means <paramref name="rows"/></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SpanMatrix(Span<T> span, int rows, int leadingDim = 0)
		{
			if (span.IsEmpty)
				throw new ArgumentNullException(nameof(span));
			if (rows <= 0)
				throw new ArgumentOutOfRangeException(nameof(rows), rows, Resources.Parameter.MustPositive);
			if (leadingDim < 0)
				throw new ArgumentOutOfRangeException(nameof(leadingDim), leadingDim, Resources.Parameter.CannotNegative);
			if (leadingDim == 0)
				leadingDim = rows;
			if (span.Length % leadingDim != 0)
				throw new ArgumentException(Resources.Other.CannotDivide, nameof(leadingDim));

			this._span = span;
			this._rows = rows;
			this._cols = span.Length / leadingDim;
			this._leadDim = leadingDim;
		}
		#endregion

		#region indexer
		/// <summary>
		/// Get the reference of the element at (<paramref name="row"/>, <paramref name="col"/>) of this <see cref="SpanMatrix{T}"/>
		/// </summary>
		/// <param name="row">The row index of the element to get reference</param>
		/// <param name="col">The column index of the element to get reference</param>
		/// <returns>The reference of the element at (<paramref name="row"/>, <paramref name="col"/>)</returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="row"/> or <paramref name="col"/> is out of range</exception>
		public readonly ref T this[int row, int col] {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				if (row < 0 || row >= this._rows)
					throw new ArgumentOutOfRangeException(nameof(row), row, Resources.Parameter.InvalidValue);
				if (col < 0 || col >= this._cols)
					throw new ArgumentOutOfRangeException(nameof(row), row, Resources.Parameter.InvalidValue);
				return ref this._span[row + col * this._leadDim];
			}
		}

		/// <summary>
		/// Get the column at <paramref name="columnIndex"/> of this <see cref="SpanMatrix{T}"/> as a <see cref="Span{T}"/>
		/// </summary>
		/// <param name="columnIndex">The index of the column to get</param>
		/// <returns>The column at <paramref name="columnIndex"/> as a <see cref="Span{T}"/></returns>
		public readonly Span<T> this[int columnIndex] {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				if (columnIndex < 0 || columnIndex >= this._cols)
					throw new ArgumentOutOfRangeException(nameof(columnIndex), columnIndex, Resources.Parameter.InvalidValue);
				return this._span[(columnIndex * this._leadDim)..(columnIndex * this._leadDim + this._rows)];
			}
		}

		/// <summary>
		/// Forms a slice out of the current <see cref="SpanMatrix{T}"/> staring at a specified <paramref name="column"/> for a specified <paramref name="count"/> of columns
		/// </summary>
		/// <param name="column">The index of the column at which to begin this slice</param>
		/// <param name="count">The desired number of columns for the slice</param>
		/// <returns>A <see cref="SpanMatrix{T}"/> that consists of <paramref name="count"/> columns from the current <see cref="SpanMatrix{T}"/> starting at the <paramref name="column"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="column"/> or <paramref name="count"/> is out of range</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly SpanMatrix<T> Slice(int column, int count)
		{
			if (column < 0 || column >= this._cols)
				throw new ArgumentOutOfRangeException(nameof(column), column, Resources.Parameter.InvalidValue);
			if (count < 0 || column + count >= this._cols)
				throw new ArgumentOutOfRangeException(nameof(count), count, Resources.Parameter.InvalidValue);
			return new(this._span[(column * this._leadDim)..((column + count) * this._leadDim)], this._rows, this._leadDim);
		}

		/// <summary>
		/// Get the number of columns of this <see cref="SpanMatrix{T}"/>
		/// </summary>
		public readonly int Count {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this._cols;
		}

		/// <summary>
		/// Forms a slice out of the current <see cref="SpanMatrix{T}"/> staring at a specified <paramref name="row"/> for a specified <paramref name="count"/> of rows
		/// </summary>
		/// <param name="row">The index of the row at which to begin this slice</param>
		/// <param name="count">The desired number of columns for the slice</param>
		/// <returns>A <see cref="SpanMatrix{T}"/> that consists of <paramref name="count"/> rows from the current <see cref="SpanMatrix{T}"/> starting at the <paramref name="row"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="row"/> or <paramref name="count"/> is out of range</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly SpanMatrix<T> SliceRow(int row, int count)
		{
			if (row < 0 || row >= this._rows)
				throw new ArgumentOutOfRangeException(nameof(row), row, Resources.Parameter.InvalidValue);
			if (count < 0 || row + count >= this._rows)
				throw new ArgumentOutOfRangeException(nameof(count), count, Resources.Parameter.InvalidValue);
			return new(this._span[row..^(this._leadDim - row - count)], count, this._leadDim);
		}

		/// <summary>
		/// Forms a slice out of the current <see cref="SpanMatrix{T}"/> with the given <paramref name="range"/> of rows
		/// </summary>
		/// <param name="range">The range of the rows to slice</param>
		/// <returns>A <see cref="SpanMatrix{T}"/> that consists of rows of <paramref name="range"/> of the current <see cref="SpanMatrix{T}"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="range"/> is out of range</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly SpanMatrix<T> SliceRow(Range range)
		{
			var (off, len) = range.GetOffsetAndLength(this._rows);
			return this.SliceRow(off, len);
		}

		/// <summary>
		/// Forms a slice out of the current <see cref="SpanMatrix{T}"/> with the given <paramref name="rowRange"/> and <paramref name="colRange"/>
		/// </summary>
		/// <param name="rowRange">The range of the rows to slice</param>
		/// <param name="colRange">The range of the columns to slice</param>
		/// <returns>A <see cref="SpanMatrix{T}"/> that consists of <paramref name="rowRange"/> and <paramref name="colRange"/> of the current <see cref="SpanMatrix{T}"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="rowRange"/> or <paramref name="colRange"/> is out of range</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly SpanMatrix<T> SubMatrix(Range rowRange, Range colRange)
		{
			var (off, len) = rowRange.GetOffsetAndLength(this._rows);
			var rowSlice = this.SliceRow(off, len);
			(off, len) = colRange.GetOffsetAndLength(this._cols);
			return rowSlice.Slice(off, len);
		}
		#endregion

		#region equality
		/// <summary>
		/// Equality operator
		/// </summary>
		public static bool operator ==(SpanMatrix<T> left, SpanMatrix<T> right)
		{
			return left._span == right._span && left._rows == right._rows && left._leadDim == right._leadDim;
		}

		/// <summary>
		/// Inequality operator
		/// </summary>
		public static bool operator !=(SpanMatrix<T> left, SpanMatrix<T> right)
		{
			return !(left == right);
		}

#pragma warning disable CS0809
		/// <summary>
		/// Checks whether the given <paramref name="obj"/> is the same as this one
		/// </summary>
		/// <param name="obj">The given object</param>
		/// <returns>Equals or not</returns>
		[Obsolete("Equals() on SpanMatrix will always throw an exception. Use == instead.")]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public override readonly bool Equals(object? obj)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Get the hash code of this <see cref="SpanMatrix{T}"/>
		/// </summary>
		/// <returns>The hash code</returns>
		[Obsolete("GetHashCode() on SpanMatrix will always throw an exception.")]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public override readonly int GetHashCode()
		{
			throw new NotSupportedException();
		}
#pragma warning restore CS0809

		/// <summary>
		/// Get the <see cref="Enumerator"/> of this <see cref="SpanMatrix{T}"/>
		/// </summary>
		/// <returns>The enumerator of this <see cref="SpanMatrix{T}"/></returns>
		public readonly Enumerator GetEnumerator()
		{
			return new Enumerator(this);
		}
		#endregion

		#region convert
		/// <summary>
		/// Copy a specific <paramref name="row"/> of the current <see cref="SpanMatrix{T}"/> to the <paramref name="destination"/> <see cref="Span{T}"/>
		/// </summary>
		/// <param name="row">The index of the row to copy</param>
		/// <param name="destination">The <see cref="Span{T}"/> to copy to</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="row"/> is out of range</exception>
		/// <exception cref="ArgumentException">If <paramref name="destination"/> is too short</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly void CopyRowTo(int row, Span<T> destination)
		{
			if (row < 0 || row >= this._rows)
				throw new ArgumentOutOfRangeException(nameof(row), row, Resources.Parameter.InvalidValue);
			if (destination.Length < this._cols)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(destination));

			for (int i = 0; i < this._cols; i++)
			{
				destination[i] = this._span[row + i * this._leadDim];
			}
		}

		/// <summary>
		/// Copy the current <see cref="SpanMatrix{T}"/> to the <paramref name="destination"/> <see cref="Span{T}"/> column-by-column
		/// </summary>
		/// <param name="destination">The <see cref="Span{T}"/> to copy to</param>
		/// <exception cref="ArgumentException">If <paramref name="destination"/> is too short</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly void CopyTo(Span<T> destination)
		{
			if (this._leadDim == this._rows)
			{
				this._span.CopyTo(destination);
				return;
			}
			// otherwise
			if (destination.Length < this._rows * this._cols)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(destination));
			for (int i = 0; i < this._cols; i++)
			{
				var dst = destination[(i * this._rows)..];
				this._span[(i * this._leadDim)..(i * this._leadDim + this._rows)].CopyTo(dst);
			}
		}

		/// <summary>
		/// Copy the current <see cref="SpanMatrix{T}"/> to the <paramref name="destination"/> <see cref="Span{T}"/> column-by-column
		/// </summary>
		/// <param name="destination">The <see cref="Span{T}"/> to copy to</param>
		/// <param name="converter">The <see cref="Converter{TInput, TOutput}"/> used to change the data type</param>
		/// <exception cref="ArgumentException">If <paramref name="destination"/> is too short</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly void CopyTo<TOut>(Span<TOut> destination, Converter<T, TOut> converter)
		{
			if (this._leadDim == this._rows)
			{
				this._span.CopyTo(destination, converter);
				return;
			}
			// otherwise
			if (destination.Length < this._rows * this._cols)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(destination));
			for (int i = 0; i < this._cols; i++)
			{
				var dst = destination[(i * this._rows)..];
				this._span[(i * this._leadDim)..(i * this._leadDim + this._rows)].CopyTo(dst, converter);
			}
		}

		/// <summary>
		/// Get the string representation of this <see cref="SpanMatrix{T}"/>
		/// </summary>
		/// <returns>The string representation of this <see cref="SpanMatrix{T}"/></returns>
		public override readonly string ToString()
		{
			return $"{nameof(SpanMatrix<T>)}<{typeof(T).Name}>[Size = {this._rows}x{this._cols}, {nameof(LeadDim)} = {this._leadDim}]";
		}

		/// <summary>
		/// Convert this <see cref="SpanMatrix{T}"/> to an array of column arrays of type <typeparamref name="T"/>
		/// </summary>
		/// <returns>An array of column arrays of type <typeparamref name="T"/> holding the same values as this <see cref="SpanList{T}"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly T[][] ToArray()
		{
			if (this._leadDim == 0)
			{
				return Array.Empty<T[]>();
			}
			T[][] array = new T[this._cols][];
			for (int i = 0; i < this._cols; i++)
			{
				T[] column = new T[this._rows];
				Unsafe.CopyBlock(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetArrayDataReference(column)),
								 ref Unsafe.As<T, byte>(ref this._span[i * this._leadDim]),
								 (uint)this._rows);
			}
			return array;
		}
		#endregion
	}
}
