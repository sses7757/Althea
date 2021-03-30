using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Althea.Linq;


namespace Althea.Helpers
{
	#region debug view
	internal interface IAsSpan<T>
	{
		Span<T> AsSpan(int size = 0);
	}

	internal sealed class FixedBufferDebugView<T>
	{
		private readonly T[] m_array;

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public T[] Items => this.m_array;

		public FixedBufferDebugView(IAsSpan<T> s)
		{
			this.m_array = s.AsSpan().ToArray();
		}
	}
	#endregion



	/// <summary>
	/// The fixed buffer struct of class type <typeparamref name="T"/> with size = 2.
	/// </summary>
	/// <typeparam name="T">Any class</typeparam>
	[StructLayout(LayoutKind.Sequential, Size = 122)]
	[DebuggerTypeProxy(typeof(FixedBufferDebugView<>))]
	[DebuggerDisplay("{ToString(),raw}")]
	public struct FixedClassBuffer_2<T> : IEquatable<FixedClassBuffer_2<T>>, IReadOnlyList<T>, IAsSpan<T> where T : class
	{
		#region basic
		private const int _count = 2;

		/// <summary>
		/// Create a <see cref="FixedClassBuffer_2{T}"/> with a given <paramref name="array"/>
		/// </summary>
		/// <param name="array">The given array of <typeparamref name="T"/> used to create</param>
		/// <exception cref="ArgumentException">If the size of <paramref name="array"/> is larger than 2</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="array"/> is null</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public FixedClassBuffer_2(params T[] array)
		{
			if (array is null || array.Length == 0)
				throw new ArgumentNullException(nameof(array));
			array.CopyTo(this.AsSpan());
		}

		/// <summary>
		/// Create a <see cref="FixedClassBuffer_2{T}"/> with a given <paramref name="span"/>
		/// </summary>
		/// <param name="span">The given <see cref="ReadOnlySpan{T}"/> of <typeparamref name="T"/> used to create</param>
		/// <exception cref="ArgumentException">If the size of <paramref name="span"/> is larger than 2</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="span"/> is empty</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public FixedClassBuffer_2(ReadOnlySpan<T> span)
		{
			if (span.IsEmpty)
				throw new ArgumentNullException(nameof(span));
			span.CopyTo(this.AsSpan(), static s => s);
		}

		/// <summary>
		/// Create a <see cref="Span{T}"/> from this fixed class-typed buffer
		/// </summary>
		/// <param name="size">The size of the span, default 0 means all</param>
		/// <returns>The <see cref="Span{T}"/> referring to this fixed buffer</returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="size"/> is out of range</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Span<T> AsSpan(int size = 0)
		{
			if (size == 0)
				return MemoryMarshal.CreateSpan(ref Unsafe.As<FixedClassBuffer_2<T>, T>(ref this), _count);
			// else
			if (size < 0 || size > _count)
				throw new ArgumentOutOfRangeException(nameof(size), size, Resources.Parameter.InvalidValue);
			return MemoryMarshal.CreateSpan(ref Unsafe.As<FixedClassBuffer_2<T>, T>(ref this), size);
		}
		#endregion

		#region indexer
		/// <summary>
		/// Basic indexer of this fixed buffer
		/// </summary>
		/// <param name="index">The index</param>
		/// <returns>The value at <paramref name="index"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="index"/> is out of range</exception>
		public T this[int index] {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				return this.AsSpan()[index];
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set {
				this.AsSpan()[index] = value;
			}
		}

		/// <summary>
		/// Get the number of elements in this fixed buffer
		/// </summary>
		public int Count {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _count;
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
		#endregion

		#region equality
		/// <summary>
		/// Whether this == <paramref name="other"/>
		/// </summary>
		/// <param name="other">another <see cref="FixedClassBuffer_2{T}"/> to compare</param>
		/// <returns>this == <paramref name="other"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(FixedClassBuffer_2<T> other)
		{
			var comparer = EqualityComparer<T>.Default;
			return this.AsSpan().SequenceEqual<T, T>(other.AsSpan(), (a, b) => comparer.Equals(a, b));
		}

		/// <summary>
		/// Override <see cref="ValueType.Equals(object?)"/> to check whether this == <paramref name="obj"/>
		/// </summary>
		/// <param name="obj">another object to compare</param>
		/// <returns>this == <paramref name="obj"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override bool Equals(object? obj)
		{
			return obj is FixedClassBuffer_2<T> buffer && this.Equals(buffer);
		}

		/// <summary>
		/// Override <see cref="ValueType.GetHashCode"/> to get the hash code this <see cref="FixedClassBuffer_2{T}"/>.
		/// </summary>
		/// <returns>The hash code</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode()
		{
			Span<int> hash = stackalloc int[_count];
			this.AsSpan().CopyTo(hash, static s => s?.GetHashCode() ?? 0);
			return hash.HashCodeOfSpan();
		}

		/// <summary>
		/// Equality operator
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(FixedClassBuffer_2<T> left, FixedClassBuffer_2<T> right)
		{
			return left.Equals(right);
		}

		/// <summary>
		/// Inequality operator
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(FixedClassBuffer_2<T> left, FixedClassBuffer_2<T> right)
		{
			return !(left == right);
		}
		#endregion

		#region string related
		/// <summary>
		/// Return the string representation of this <see cref="FixedClassBuffer_2{T}"/>
		/// </summary>
		/// <returns>the string representation of this <see cref="FixedClassBuffer_2{T}"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override string ToString()
		{
			return $"Fixed Class-type Buffer [Size=2, Type={typeof(T).GetGenericString()}]";
		}
		#endregion
	}


	/// <summary>
	/// The fixed buffer struct of class type <typeparamref name="T"/> with size = 8.
	/// </summary>
	/// <typeparam name="T">Any class</typeparam>
	[StructLayout(LayoutKind.Sequential, Size = 128)]
	[DebuggerTypeProxy(typeof(FixedBufferDebugView<>))]
	[DebuggerDisplay("{ToString(),raw}")]
	public struct FixedClassBuffer_8<T> : IEquatable<FixedClassBuffer_8<T>>, IReadOnlyList<T>, IAsSpan<T> where T : class
	{
		#region basic
		private const int _count = 8;

		/// <summary>
		/// Create a <see cref="FixedClassBuffer_8{T}"/> with a given <paramref name="array"/>
		/// </summary>
		/// <param name="array">The given array of <typeparamref name="T"/> used to create</param>
		/// <exception cref="ArgumentException">If the size of <paramref name="array"/> is larger than 8</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="array"/> is null</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public FixedClassBuffer_8(params T[] array)
		{
			if (array is null || array.Length == 0)
				throw new ArgumentNullException(nameof(array));
			array.CopyTo(this.AsSpan());
		}

		/// <summary>
		/// Create a <see cref="FixedClassBuffer_8{T}"/> with a given <paramref name="span"/>
		/// </summary>
		/// <param name="span">The given <see cref="ReadOnlySpan{T}"/> of <typeparamref name="T"/> used to create</param>
		/// <exception cref="ArgumentException">If the size of <paramref name="span"/> is larger than 8</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="span"/> is empty</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public FixedClassBuffer_8(ReadOnlySpan<T> span)
		{
			if (span.IsEmpty)
				throw new ArgumentNullException(nameof(span));
			span.CopyTo(this.AsSpan(), static s => s);
		}

		/// <summary>
		/// Create a <see cref="Span{T}"/> from this fixed class-typed buffer
		/// </summary>
		/// <param name="size">The size of the span, default 0 means all</param>
		/// <returns>The <see cref="Span{T}"/> referring to this fixed buffer</returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="size"/> is out of range</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Span<T> AsSpan(int size = 0)
		{
			if (size == 0)
				return MemoryMarshal.CreateSpan(ref Unsafe.As<FixedClassBuffer_8<T>, T>(ref this), _count);
			// else
			if (size < 0 || size > _count)
				throw new ArgumentOutOfRangeException(nameof(size), size, Resources.Parameter.InvalidValue);
			return MemoryMarshal.CreateSpan(ref Unsafe.As<FixedClassBuffer_8<T>, T>(ref this), size);
		}
		#endregion

		#region indexer
		/// <summary>
		/// Basic indexer of this fixed buffer
		/// </summary>
		/// <param name="index">The index</param>
		/// <returns>The value at <paramref name="index"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="index"/> is out of range</exception>
		public T this[int index] {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				return this.AsSpan()[index];
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set {
				this.AsSpan()[index] = value;
			}
		}

		/// <summary>
		/// Get the number of elements in this fixed buffer
		/// </summary>
		public int Count {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _count;
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
		#endregion

		#region equality
		/// <summary>
		/// Whether this == <paramref name="other"/>
		/// </summary>
		/// <param name="other">another <see cref="FixedClassBuffer_8{T}"/> to compare</param>
		/// <returns>this == <paramref name="other"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(FixedClassBuffer_8<T> other)
		{
			var comparer = EqualityComparer<T>.Default;
			return this.AsSpan().SequenceEqual<T, T>(other.AsSpan(), (a, b) => comparer.Equals(a, b));
		}

		/// <summary>
		/// Override <see cref="ValueType.Equals(object?)"/> to check whether this == <paramref name="obj"/>
		/// </summary>
		/// <param name="obj">another object to compare</param>
		/// <returns>this == <paramref name="obj"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override bool Equals(object? obj)
		{
			return obj is FixedClassBuffer_8<T> buffer && this.Equals(buffer);
		}

		/// <summary>
		/// Override <see cref="ValueType.GetHashCode"/> to get the hash code this <see cref="FixedClassBuffer_8{T}"/>.
		/// </summary>
		/// <returns>The hash code</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode()
		{
			Span<int> hash = stackalloc int[_count];
			this.AsSpan().CopyTo(hash, static s => s?.GetHashCode() ?? 0);
			return hash.HashCodeOfSpan();
		}

		/// <summary>
		/// Equality operator
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(FixedClassBuffer_8<T> left, FixedClassBuffer_8<T> right)
		{
			return left.Equals(right);
		}

		/// <summary>
		/// Inequality operator
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(FixedClassBuffer_8<T> left, FixedClassBuffer_8<T> right)
		{
			return !(left == right);
		}
		#endregion

		#region string related
		/// <summary>
		/// Return the string representation of this <see cref="FixedClassBuffer_8{T}"/>
		/// </summary>
		/// <returns>the string representation of this <see cref="FixedClassBuffer_8{T}"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override string ToString()
		{
			return $"Fixed Class-type Buffer [Size=8, Type={typeof(T).GetGenericString()}]";
		}
		#endregion
	}


	/// <summary>
	/// The fixed buffer struct of class type <typeparamref name="T"/> with size = 16.
	/// </summary>
	/// <typeparam name="T">Any class</typeparam>
	[StructLayout(LayoutKind.Sequential, Size = 128)]
	[DebuggerTypeProxy(typeof(FixedBufferDebugView<>))]
	[DebuggerDisplay("{ToString(),raw}")]
	public struct FixedClassBuffer_16<T> : IEquatable<FixedClassBuffer_16<T>>, IReadOnlyList<T>, IAsSpan<T> where T : class
	{
		#region basic
		private const int _count = 16;

		/// <summary>
		/// Create a <see cref="FixedClassBuffer_16{T}"/> with a given <paramref name="array"/>
		/// </summary>
		/// <param name="array">The given array of <typeparamref name="T"/> used to create</param>
		/// <exception cref="ArgumentException">If the size of <paramref name="array"/> is larger than 16</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="array"/> is null</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public FixedClassBuffer_16(params T[] array)
		{
			if (array is null || array.Length == 0)
				throw new ArgumentNullException(nameof(array));
			array.CopyTo(this.AsSpan());
		}

		/// <summary>
		/// Create a <see cref="FixedClassBuffer_16{T}"/> with a given <paramref name="span"/>
		/// </summary>
		/// <param name="span">The given <see cref="ReadOnlySpan{T}"/> of <typeparamref name="T"/> used to create</param>
		/// <exception cref="ArgumentException">If the size of <paramref name="span"/> is larger than 16</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="span"/> is empty</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public FixedClassBuffer_16(ReadOnlySpan<T> span)
		{
			if (span.IsEmpty)
				throw new ArgumentNullException(nameof(span));
			span.CopyTo(this.AsSpan(), static s => s);
		}

		/// <summary>
		/// Create a <see cref="Span{T}"/> from this fixed class-typed buffer
		/// </summary>
		/// <param name="size">The size of the span, default 0 means all</param>
		/// <returns>The <see cref="Span{T}"/> referring to this fixed buffer</returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="size"/> is out of range</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Span<T> AsSpan(int size = 0)
		{
			if (size == 0)
				return MemoryMarshal.CreateSpan(ref Unsafe.As<FixedClassBuffer_16<T>, T>(ref this), _count);
			// else
			if (size < 0 || size > _count)
				throw new ArgumentOutOfRangeException(nameof(size), size, Resources.Parameter.InvalidValue);
			return MemoryMarshal.CreateSpan(ref Unsafe.As<FixedClassBuffer_16<T>, T>(ref this), size);
		}

		/// <summary>
		/// Create a <see cref="Span{T}"/> of type <typeparamref name="TClass"/> from this fixed class-typed buffer
		/// </summary>
		/// <typeparam name="TClass">Another class type as the output type</typeparam>
		/// <param name="size">The size of the span, default 0 means all</param>
		/// <returns>The <see cref="Span{T}"/> of <typeparamref name="TClass"/> referring to this fixed buffer</returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="size"/> is out of range</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Span<TClass> AsSpan<TClass>(int size = 0) where TClass : class
		{
			if (size == 0)
				return MemoryMarshal.CreateSpan(ref Unsafe.As<FixedClassBuffer_16<T>, TClass>(ref this), _count);
			// else
			if (size < 0 || size > _count)
				throw new ArgumentOutOfRangeException(nameof(size), size, Resources.Parameter.InvalidValue);
			return MemoryMarshal.CreateSpan(ref Unsafe.As<FixedClassBuffer_16<T>, TClass>(ref this), size);
		}
		#endregion

		#region indexer
		/// <summary>
		/// Basic indexer of this fixed buffer
		/// </summary>
		/// <param name="index">The index</param>
		/// <returns>The value at <paramref name="index"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="index"/> is out of range</exception>
		public T this[int index] {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				return this.AsSpan()[index];
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set {
				this.AsSpan()[index] = value;
			}
		}

		/// <summary>
		/// Get the number of elements in this fixed buffer
		/// </summary>
		public int Count {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _count;
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
		#endregion

		#region equality
		/// <summary>
		/// Whether this == <paramref name="other"/>
		/// </summary>
		/// <param name="other">another <see cref="FixedClassBuffer_16{T}"/> to compare</param>
		/// <returns>this == <paramref name="other"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(FixedClassBuffer_16<T> other)
		{
			var comparer = EqualityComparer<T>.Default;
			return this.AsSpan().SequenceEqual<T, T>(other.AsSpan(), (a, b) => comparer.Equals(a, b));
		}

		/// <summary>
		/// Override <see cref="ValueType.Equals(object?)"/> to check whether this == <paramref name="obj"/>
		/// </summary>
		/// <param name="obj">another object to compare</param>
		/// <returns>this == <paramref name="obj"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override bool Equals(object? obj)
		{
			return obj is FixedClassBuffer_16<T> buffer && this.Equals(buffer);
		}

		/// <summary>
		/// Override <see cref="ValueType.GetHashCode"/> to get the hash code this <see cref="FixedClassBuffer_16{T}"/>.
		/// </summary>
		/// <returns>The hash code</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode()
		{
			Span<int> hash = stackalloc int[_count];
			this.AsSpan().CopyTo(hash, static s => s?.GetHashCode() ?? 0);
			return hash.HashCodeOfSpan();
		}

		/// <summary>
		/// Equality operator
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(FixedClassBuffer_16<T> left, FixedClassBuffer_16<T> right)
		{
			return left.Equals(right);
		}

		/// <summary>
		/// Inequality operator
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(FixedClassBuffer_16<T> left, FixedClassBuffer_16<T> right)
		{
			return !(left == right);
		}
		#endregion

		#region string related
		/// <summary>
		/// Return the string representation of this <see cref="FixedClassBuffer_16{T}"/>
		/// </summary>
		/// <returns>the string representation of this <see cref="FixedClassBuffer_16{T}"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override string ToString()
		{
			return $"Fixed Class-type Buffer [Size=16, Type={typeof(T).GetGenericString()}]";
		}
		#endregion
	}
}
