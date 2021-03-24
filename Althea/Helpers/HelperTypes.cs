using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Althea.Linq;


namespace Althea.Helpers
{
	#region interface
	/// <summary>
	/// The interface for fixed buffer structures
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct that implements <see cref="IEquatable{T}"/></typeparam>
	public interface IFixedBuffer<T> : IReadOnlyList<T> where T : unmanaged, IEquatable<T>
	{
		/// <summary>
		/// Get the number of values whose value is not default(<typeparamref name="T"/>)
		/// </summary>
		int NonDefaults { get; }

		/// <summary>
		/// Basic indexer of this fixed buffer
		/// </summary>
		/// <param name="index">The index as a <see cref="int"/></param>
		/// <returns>The value at <paramref name="index"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="index"/> is out of range</exception>
		new T this[int index] { get; }

		/// <summary>
		/// Create a new array of <typeparamref name="T"/> containing the elements of this fixed buffer
		/// </summary>
		/// <returns>The array containing the elements of this fixed buffer</returns>
		T[] ToArray();

		internal T[] Data => this.ToArray();

		/// <summary>
		/// Change data type of this fixed buffer from <typeparamref name="T"/> to <typeparamref name="TOut"/>
		/// </summary>
		/// <typeparam name="TOut">The output data type, any unmanaged struct</typeparam>
		/// <returns>The fixed buffer with same byte values as this one whose data type is <typeparamref name="TOut"/></returns>
		IFixedBuffer<TOut> As<TOut>() where TOut : unmanaged, IEquatable<TOut>;

		/// <summary>
		/// Copy the data from this fixed buffer to the given <paramref name="span"/>
		/// </summary>
		/// <param name="span">The given <see cref="ReadOnlySpan{T}"/> to copy to</param>
		/// <param name="offset">The offset to start copying in <typeparamref name="T"/></param>
		/// <exception cref="ArgumentException">If the length of <paramref name="span"/> is too large</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offset"/> is out of boundary</exception>
		void CopyToSpan(Span<T> span, int offset = 0);

		/// <summary>
		/// Convert this fixed buffer to a new <typeparamref name="TStruct"/> by copying the values from <paramref name="copyStart"/> byte by byte
		/// </summary>
		/// <typeparam name="TStruct">The output struct type</typeparam>
		/// <param name="copyStart">The start position to copy in bytes</param>
		/// <returns>The created <typeparamref name="TStruct"/></returns>
		/// <exception cref="InvalidOperationException">If the size of <typeparamref name="TStruct"/> is larger than the size of this fixed buffer minus <paramref name="copyStart"/></exception>
		TStruct ToStruct<TStruct>(int copyStart = 0) where TStruct : struct;

		/// <summary>
		/// Check whether the this fixed buffer contains the given <paramref name="value"/> 
		/// </summary>
		/// <param name="value">The value to find</param>
		/// <returns>Whether the this fixed buffer contains the given <paramref name="value"/> </returns>
		bool Contains(T value);
	}
	#endregion


	//// 16 32 64 128
	
	/// <summary>
	/// The fixed buffer struct of type <typeparamref name="T"/> and size in bytes = 16
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct that implements <see cref="IEquatable{T}"/></typeparam>
	[StructLayout(LayoutKind.Sequential, Size = 16)]
	[UnsafeValueType]
	public unsafe struct FixedBuffer_16<T> : IEquatable<FixedBuffer_16<T>>, IFixedBuffer<T> where T : unmanaged, IEquatable<T>
	{
		#region basic
		private static readonly int _count = 16 / sizeof(T);

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
				Unsafe.CopyBlock(a, t, 16);
			}
			return array;
		}

		/// <summary>
		/// Change data type of this fixed buffer from <typeparamref name="T"/> to <typeparamref name="TOut"/>
		/// </summary>
		/// <typeparam name="TOut">The output data type, any unmanaged struct</typeparam>
		/// <returns>The fixed buffer with same byte values as this one whose data type is <typeparamref name="TOut"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public FixedBuffer_16<TOut> As<TOut>() where TOut : unmanaged, IEquatable<TOut>
		{
			var newBuffer = new FixedBuffer_16<TOut>();
			fixed (void* t = &this.field)
			{
				Unsafe.CopyBlock(&newBuffer, t, 16);
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
			if (size + copyStart > 16)
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
			if (size + copyStart > 16)
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
		/// <param name="other">another <see cref="FixedBuffer_16{T}"/> to compare</param>
		/// <returns>this == <paramref name="other"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(FixedBuffer_16<T> other)
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
			return obj is FixedBuffer_16<T> buffer && this.Equals(buffer);
		}

		/// <summary>
		/// Override <see cref="ValueType.GetHashCode"/> to get the hash code this <see cref="FixedBuffer_16{T}"/>.
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
		public static bool operator ==(FixedBuffer_16<T> left, FixedBuffer_16<T> right)
		{
			return left.Equals(right);
		}

		/// <summary>
		/// Inequality operator
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(FixedBuffer_16<T> left, FixedBuffer_16<T> right)
		{
			return !(left == right);
		}
		#endregion

		#region string related
		/// <summary>
		/// Return the string representation of this <see cref="FixedBuffer_16{T}"/>
		/// </summary>
		/// <returns>the string representation of this <see cref="FixedBuffer_16{T}"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override string ToString()
		{
			return $"Fixed Buffer [Size={_count}, Type={typeof(T).GetGenericString()}]";
		}
		#endregion
	}


	/// <summary>
	/// The fixed buffer struct of type <typeparamref name="T"/> and size in bytes = 32
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct that implements <see cref="IEquatable{T}"/></typeparam>
	[StructLayout(LayoutKind.Sequential, Size = 32)]
	[UnsafeValueType]
	public unsafe struct FixedBuffer_32<T> : IEquatable<FixedBuffer_32<T>>, IFixedBuffer<T> where T : unmanaged, IEquatable<T>
	{
		#region basic
		private static readonly int _count = 32 / sizeof(T);

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
				Unsafe.CopyBlock(a, t, 32);
			}
			return array;
		}

		/// <summary>
		/// Change data type of this fixed buffer from <typeparamref name="T"/> to <typeparamref name="TOut"/>
		/// </summary>
		/// <typeparam name="TOut">The output data type, any unmanaged struct</typeparam>
		/// <returns>The fixed buffer with same byte values as this one whose data type is <typeparamref name="TOut"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public FixedBuffer_32<TOut> As<TOut>() where TOut : unmanaged, IEquatable<TOut>
		{
			var newBuffer = new FixedBuffer_32<TOut>();
			fixed (void* t = &this.field)
			{
				Unsafe.CopyBlock(&newBuffer, t, 32);
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
			if (size + copyStart > 32)
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
			if (size + copyStart > 32)
				throw new InvalidOperationException(Resources.Other.InvalidGeneric);
			fixed (void* t = &this.field)
			{
				Unsafe.CopyBlock((byte*)t + copyStart, Unsafe.AsPointer(ref @struct), (uint)size);
			}
		}

		/// <summary>
		/// Create a <see cref="Span{T}"/> from this fixed buffer
		/// </summary>
		/// <param name="size">The size of the span, default 0 means all</param>
		/// <returns>The <see cref="Span{T}"/> referring to this fixed buffer</returns>
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
		/// <param name="other">another <see cref="FixedBuffer_32{T}"/> to compare</param>
		/// <returns>this == <paramref name="other"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(FixedBuffer_32<T> other)
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
			return obj is FixedBuffer_32<T> buffer && this.Equals(buffer);
		}

		/// <summary>
		/// Override <see cref="ValueType.GetHashCode"/> to get the hash code this <see cref="FixedBuffer_32{T}"/>.
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
		public static bool operator ==(FixedBuffer_32<T> left, FixedBuffer_32<T> right)
		{
			return left.Equals(right);
		}

		/// <summary>
		/// Inequality operator
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(FixedBuffer_32<T> left, FixedBuffer_32<T> right)
		{
			return !(left == right);
		}
		#endregion

		#region string related
		/// <summary>
		/// Return the string representation of this <see cref="FixedBuffer_32{T}"/>
		/// </summary>
		/// <returns>the string representation of this <see cref="FixedBuffer_32{T}"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override string ToString()
		{
			return $"Fixed Buffer [Size={_count}, Type={typeof(T).GetGenericString()}]";
		}
		#endregion
	}


	/// <summary>
	/// The fixed buffer struct of type <typeparamref name="T"/> and size in bytes = 64
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct that implements <see cref="IEquatable{T}"/></typeparam>
	[StructLayout(LayoutKind.Sequential, Size = 64)]
	[UnsafeValueType]
	public unsafe struct FixedBuffer_64<T> : IEquatable<FixedBuffer_64<T>>, IFixedBuffer<T> where T : unmanaged, IEquatable<T>
	{
		#region basic
		private static readonly int _count = 64 / sizeof(T);

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
				Unsafe.CopyBlock(a, t, 64);
			}
			return array;
		}

		/// <summary>
		/// Change data type of this fixed buffer from <typeparamref name="T"/> to <typeparamref name="TOut"/>
		/// </summary>
		/// <typeparam name="TOut">The output data type, any unmanaged struct</typeparam>
		/// <returns>The fixed buffer with same byte values as this one whose data type is <typeparamref name="TOut"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public FixedBuffer_64<TOut> As<TOut>() where TOut : unmanaged, IEquatable<TOut>
		{
			var newBuffer = new FixedBuffer_64<TOut>();
			fixed (void* t = &this.field)
			{
				Unsafe.CopyBlock(&newBuffer, t, 64);
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
			if (size + copyStart > 64)
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
			if (size + copyStart > 64)
				throw new InvalidOperationException(Resources.Other.InvalidGeneric);
			fixed (void* t = &this.field)
			{
				Unsafe.CopyBlock((byte*)t + copyStart, Unsafe.AsPointer(ref @struct), (uint)size);
			}
		}

		/// <summary>
		/// Create a <see cref="Span{T}"/> from this fixed buffer
		/// </summary>
		/// <param name="size">The size of the span, default 0 means all</param>
		/// <returns>The <see cref="Span{T}"/> referring to this fixed buffer</returns>
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
		/// <param name="other">another <see cref="FixedBuffer_64{T}"/> to compare</param>
		/// <returns>this == <paramref name="other"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(FixedBuffer_64<T> other)
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
			return obj is FixedBuffer_64<T> buffer && this.Equals(buffer);
		}

		/// <summary>
		/// Override <see cref="ValueType.GetHashCode"/> to get the hash code this <see cref="FixedBuffer_64{T}"/>.
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
		public static bool operator ==(FixedBuffer_64<T> left, FixedBuffer_64<T> right)
		{
			return left.Equals(right);
		}

		/// <summary>
		/// Inequality operator
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(FixedBuffer_64<T> left, FixedBuffer_64<T> right)
		{
			return !(left == right);
		}
		#endregion

		#region string related
		/// <summary>
		/// Return the string representation of this <see cref="FixedBuffer_64{T}"/>
		/// </summary>
		/// <returns>the string representation of this <see cref="FixedBuffer_64{T}"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override string ToString()
		{
			return $"Fixed Buffer [Size={_count}, Type={typeof(T).GetGenericString()}]";
		}
		#endregion
	}


	/// <summary>
	/// The fixed buffer struct of type <typeparamref name="T"/> and size in bytes = 128
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct that implements <see cref="IEquatable{T}"/></typeparam>
	[StructLayout(LayoutKind.Sequential, Size = 128)]
	[UnsafeValueType]
	public unsafe struct FixedBuffer_128<T> : IEquatable<FixedBuffer_128<T>>, IFixedBuffer<T> where T : unmanaged, IEquatable<T>
	{
		#region basic
		private static readonly int _count = 128 / sizeof(T);

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
				Unsafe.CopyBlock(a, t, 128);
			}
			return array;
		}

		/// <summary>
		/// Change data type of this fixed buffer from <typeparamref name="T"/> to <typeparamref name="TOut"/>
		/// </summary>
		/// <typeparam name="TOut">The output data type, any unmanaged struct</typeparam>
		/// <returns>The fixed buffer with same byte values as this one whose data type is <typeparamref name="TOut"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public FixedBuffer_128<TOut> As<TOut>() where TOut : unmanaged, IEquatable<TOut>
		{
			var newBuffer = new FixedBuffer_128<TOut>();
			fixed (void* t = &this.field)
			{
				Unsafe.CopyBlock(&newBuffer, t, 128);
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
			if (size + copyStart > 128)
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
			if (size + copyStart > 128)
				throw new InvalidOperationException(Resources.Other.InvalidGeneric);
			fixed (void* t = &this.field)
			{
				Unsafe.CopyBlock((byte*)t + copyStart, Unsafe.AsPointer(ref @struct), (uint)size);
			}
		}

		/// <summary>
		/// Create a <see cref="Span{T}"/> from this fixed buffer
		/// </summary>
		/// <param name="size">The size of the span, default 0 means all</param>
		/// <returns>The <see cref="Span{T}"/> referring to this fixed buffer</returns>
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
		/// <param name="other">another <see cref="FixedBuffer_128{T}"/> to compare</param>
		/// <returns>this == <paramref name="other"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(FixedBuffer_128<T> other)
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
			return obj is FixedBuffer_128<T> buffer && this.Equals(buffer);
		}

		/// <summary>
		/// Override <see cref="ValueType.GetHashCode"/> to get the hash code this <see cref="FixedBuffer_128{T}"/>.
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
		public static bool operator ==(FixedBuffer_128<T> left, FixedBuffer_128<T> right)
		{
			return left.Equals(right);
		}

		/// <summary>
		/// Inequality operator
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(FixedBuffer_128<T> left, FixedBuffer_128<T> right)
		{
			return !(left == right);
		}
		#endregion

		#region string related
		/// <summary>
		/// Return the string representation of this <see cref="FixedBuffer_128{T}"/>
		/// </summary>
		/// <returns>the string representation of this <see cref="FixedBuffer_128{T}"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override string ToString()
		{
			return $"Fixed Buffer [Size={_count}, Type={typeof(T).GetGenericString()}]";
		}
		#endregion
	}



	/// <summary>
	/// The fixed buffer struct of class type <typeparamref name="T"/> with size = 8.
	/// </summary>
	/// <typeparam name="T">Any class</typeparam>
	[StructLayout(LayoutKind.Sequential)]
	public struct FixedClassBuffer_8<T> : IEquatable<FixedClassBuffer_8<T>>, IReadOnlyList<T> where T : class
	{
		#region basic
		private T? a0, a1, a2, a3, a4, a5, a6, a7;

		/// <summary>
		/// Create a <see cref="SizedFixedClassBuffer_8{T}"/> with a given <paramref name="array"/>
		/// </summary>
		/// <param name="array">The given array of <typeparamref name="T"/> used to create</param>
		/// <exception cref="ArgumentException">If the size of <paramref name="array"/> is larger than 8</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="array"/> is null</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public FixedClassBuffer_8(params T[] array) : this((IReadOnlyList<T>)array) { }

		/// <summary>
		/// Create a <see cref="SizedFixedClassBuffer_8{T}"/> with a given <paramref name="list"/>
		/// </summary>
		/// <param name="list">The given <see cref="IReadOnlyList{T}"/> of <typeparamref name="T"/> used to create</param>
		/// <exception cref="ArgumentException">If the size of <paramref name="list"/> is larger than 8</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="list"/> is null</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public FixedClassBuffer_8(IReadOnlyList<T> list)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
			a0 = a1 = a2 = a3 = a4 = a5 = a6 = a7 = default;
			switch (list.Count)
			{
				case 8:
					a7 = list[7];
					goto case 7;
				case 7:
					a6 = list[6];
					goto case 6;
				case 6:
					a6 = list[6];
					goto case 6;
				case 5:
					a6 = list[6];
					goto case 4;
				case 4:
					a6 = list[6];
					goto case 3;
				case 3:
					a6 = list[6];
					goto case 2;
				case 2:
					a6 = list[6];
					goto case 1;
				case 1:
					a0 = list[0];
					break;
				default:
					break;
			}
		}
		#endregion

		#region indexer
		/// <summary>
		/// Basic indexer of this fixed buffer
		/// </summary>
		/// <param name="index">The index</param>
		/// <returns>The value at <paramref name="index"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="index"/> is out of range</exception>
		public T this[int index] {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				T? res = index switch
				{
					0 => a0,
					1 => a1,
					2 => a2,
					3 => a3,
					4 => a4,
					5 => a5,
					6 => a6,
					7 => a7,
					_ => null,
				};
				return res ?? throw new ArgumentOutOfRangeException(nameof(index), index, Resources.Parameter.InvalidValue);
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set {
				switch (index)
				{
					case 7:
						this.a7 = value;
						return;
					case 6:
						this.a6 = value;
						return;
					case 5:
						this.a5 = value;
						return;
					case 4:
						this.a4 = value;
						return;
					case 3:
						this.a3 = value;
						return;
					case 2:
						this.a2 = value;
						return;
					case 1:
						this.a1 = value;
						return;
					case 0:
						this.a0 = value;
						return;
					default:
						throw new ArgumentOutOfRangeException(nameof(index), index, Resources.Parameter.InvalidValue);
				}
			}
		}

		/// <summary>
		/// Get the number of elements in this fixed buffer
		/// </summary>
		public int Count {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => 8;
		}

		/// <summary>
		/// Returns an enumerator that iterates through the collection.
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the collection.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public IEnumerator<T> GetEnumerator() => throw new NotImplementedException();

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
			return ReferenceEquals(this.a0, other.a0) && ReferenceEquals(this.a1, other.a1) &&
				   ReferenceEquals(this.a2, other.a2) && ReferenceEquals(this.a3, other.a3) &&
				   ReferenceEquals(this.a4, other.a4) && ReferenceEquals(this.a5, other.a5) &&
				   ReferenceEquals(this.a6, other.a6) && ReferenceEquals(this.a7, other.a7);
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
			return HashCode.Combine(this.a0, this.a1, this.a2, this.a3, this.a4, this.a5, this.a6, this.a7);
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
			return $"Fixed Class-type Buffer [Size = 8, Type={typeof(T).GetGenericString()}]";
		}
		#endregion

		#region converter
		/// <summary>
		/// Implicitly convert a <see cref="SizedFixedClassBuffer_8{T}"/> to a <see cref="FixedClassBuffer_8{T}"/>
		/// </summary>
		/// <param name="sized">The input <see cref="SizedFixedClassBuffer_8{T}"/></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator FixedClassBuffer_8<T>(SizedFixedClassBuffer_8<T> sized) => new(sized);
		#endregion
	}


	/// <summary>
	/// The sized fixed buffer struct of class type <typeparamref name="T"/> with maximum size = 8.
	/// </summary>
	/// <typeparam name="T">Any class</typeparam>
	[StructLayout(LayoutKind.Sequential)]
	public struct SizedFixedClassBuffer_8<T> : IEquatable<SizedFixedClassBuffer_8<T>>, IReadOnlyList<T> where T : class
	{
		#region basic
		private T? a0, a1, a2, a3, a4, a5, a6, a7;

		private readonly int size;

		/// <summary>
		/// Create a <see cref="SizedFixedClassBuffer_8{T}"/> of the given <paramref name="size"/>
		/// </summary>
		/// <param name="size">The size</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="size"/> is out of range</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SizedFixedClassBuffer_8(int size)
		{
			if (size <= 0 || size > 8)
				throw new ArgumentOutOfRangeException(nameof(size), size, Resources.Parameter.InvalidValue);
			a0 = a1 = a2 = a3 = a4 = a5 = a6 = a7 = default;
			this.size = size;
		}

		/// <summary>
		/// Create a <see cref="SizedFixedClassBuffer_8{T}"/> with a given <paramref name="array"/>
		/// </summary>
		/// <param name="array">The given array of <typeparamref name="T"/> used to create</param>
		/// <exception cref="ArgumentException">If the size of <paramref name="array"/> is larger than 8</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="array"/> is null</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SizedFixedClassBuffer_8(params T[] array) : this((IReadOnlyList<T>)array) { }

		/// <summary>
		/// Create a <see cref="SizedFixedClassBuffer_8{T}"/> with a given <paramref name="list"/>
		/// </summary>
		/// <param name="list">The given <see cref="IReadOnlyList{T}"/> of <typeparamref name="T"/> used to create</param>
		/// <exception cref="ArgumentException">If the size of <paramref name="list"/> is larger than 8</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="list"/> is null</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SizedFixedClassBuffer_8(IReadOnlyList<T> list)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
			a0 = a1 = a2 = a3 = a4 = a5 = a6 = a7 = default;
			switch (list.Count)
			{
				case 8:
					a7 = list[7];
					goto case 7;
				case 7:
					a6 = list[6];
					goto case 6;
				case 6:
					a6 = list[6];
					goto case 6;
				case 5:
					a6 = list[6];
					goto case 4;
				case 4:
					a6 = list[6];
					goto case 3;
				case 3:
					a6 = list[6];
					goto case 2;
				case 2:
					a6 = list[6];
					goto case 1;
				case 1:
					a0 = list[0];
					break;
				default:
					break;
			}
			this.size = list.Count;
		}
		#endregion

		#region indexer
		/// <summary>
		/// Basic indexer of this fixed buffer
		/// </summary>
		/// <param name="index">The index</param>
		/// <returns>The value at <paramref name="index"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="index"/> is out of range</exception>
		public T this[int index] {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				if (index < 0 || index >= this.Count)
					throw new ArgumentOutOfRangeException(nameof(index), index, Resources.Parameter.InvalidValue);
#pragma warning disable CS8603
				return index switch
				{
					0 => a0,
					1 => a1,
					2 => a2,
					3 => a3,
					4 => a4,
					5 => a5,
					6 => a6,
					7 => a7,
					_ => null,
				};
#pragma warning restore CS8603
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set {
				if (index < 0 || index >= this.Count)
					throw new ArgumentOutOfRangeException(nameof(index), index, Resources.Parameter.InvalidValue);
				switch (index)
				{
					case 7:
						this.a7 = value;
						return;
					case 6:
						this.a6 = value;
						return;
					case 5:
						this.a5 = value;
						return;
					case 4:
						this.a4 = value;
						return;
					case 3:
						this.a3 = value;
						return;
					case 2:
						this.a2 = value;
						return;
					case 1:
						this.a1 = value;
						return;
					case 0:
						this.a0 = value;
						return;
					default:
						return;
				}
			}
		}

		/// <summary>
		/// Get the number of elements in this fixed buffer
		/// </summary>
		public int Count {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.size;
		}

		/// <summary>
		/// Returns an enumerator that iterates through the collection.
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the collection.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public IEnumerator<T> GetEnumerator() => throw new NotImplementedException();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		#endregion

		#region equality
		/// <summary>
		/// Whether this == <paramref name="other"/>
		/// </summary>
		/// <param name="other">another <see cref="SizedFixedClassBuffer_8{T}"/> to compare</param>
		/// <returns>this == <paramref name="other"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(SizedFixedClassBuffer_8<T> other)
		{
			if (this.size != other.size)
				return false;
			bool equals = true;
			switch (this.size)
			{
				case 8:
					equals = equals && this.a7 == other.a7;
					goto case 7;
				case 7:
					equals = equals && this.a6 == other.a6;
					goto case 6;
				case 6:
					equals = equals && this.a5 == other.a5;
					goto case 6;
				case 5:
					equals = equals && this.a4 == other.a4;
					goto case 4;
				case 4:
					equals = equals && this.a3 == other.a3;
					goto case 3;
				case 3:
					equals = equals && this.a2 == other.a2;
					goto case 2;
				case 2:
					equals = equals && this.a1 == other.a1;
					goto case 1;
				case 1:
					equals = equals && this.a0 == other.a0;
					break;
				default:
					break;
			}
			return equals;
		}

		/// <summary>
		/// Override <see cref="ValueType.Equals(object?)"/> to check whether this == <paramref name="obj"/>
		/// </summary>
		/// <param name="obj">another object to compare</param>
		/// <returns>this == <paramref name="obj"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override bool Equals(object? obj)
		{
			return obj is SizedFixedClassBuffer_8<T> buffer && this.Equals(buffer);
		}

		/// <summary>
		/// Override <see cref="ValueType.GetHashCode"/> to get the hash code this <see cref="SizedFixedClassBuffer_8{T}"/>.
		/// </summary>
		/// <returns>The hash code</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode()
		{
			int hash = 0;
			switch (this.size)
			{
				case 8:
					hash = HashCode.Combine(hash, this.a7);
					goto case 7;
				case 7:
					hash = HashCode.Combine(hash, this.a6);
					goto case 6;
				case 6:
					hash = HashCode.Combine(hash, this.a5);
					goto case 6;
				case 5:
					hash = HashCode.Combine(hash, this.a4);
					goto case 4;
				case 4:
					hash = HashCode.Combine(hash, this.a3);
					goto case 3;
				case 3:
					hash = HashCode.Combine(hash, this.a2);
					goto case 2;
				case 2:
					hash = HashCode.Combine(hash, this.a1);
					goto case 1;
				case 1:
					hash = HashCode.Combine(hash, this.a0);
					break;
				default:
					break;
			}
			return hash;
		}

		/// <summary>
		/// Equality operator
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(SizedFixedClassBuffer_8<T> left, SizedFixedClassBuffer_8<T> right)
		{
			return left.Equals(right);
		}

		/// <summary>
		/// Inequality operator
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(SizedFixedClassBuffer_8<T> left, SizedFixedClassBuffer_8<T> right)
		{
			return !(left == right);
		}
		#endregion

		#region string related
		/// <summary>
		/// Return the string representation of this <see cref="SizedFixedClassBuffer_8{T}"/>
		/// </summary>
		/// <returns>the string representation of this <see cref="SizedFixedClassBuffer_8{T}"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override string ToString()
		{
			return $"Read-only Fixed Class-type Buffer [Size={this.size}, Type={typeof(T).GetGenericString()}]";
		}
		#endregion

		#region converter
		/// <summary>
		/// Implicitly convert a <typeparamref name="T"/> to a <see cref="SizedFixedClassBuffer_8{T}"/>
		/// </summary>
		/// <param name="value">The value of type <typeparamref name="T"/> to be converted</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator SizedFixedClassBuffer_8<T>(T value) => new(1) { a0 = value };

		/// <summary>
		/// Implicitly convert a tuple of <typeparamref name="T"/> to a <see cref="SizedFixedClassBuffer_8{T}"/>
		/// </summary>
		/// <param name="value">The value tuple of type <typeparamref name="T"/> to be converted</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator SizedFixedClassBuffer_8<T>(ValueTuple<T, T> value) => new(1) { a0 = value.Item1, a1 = value.Item2 };
		#endregion
	}



	/// <summary>
	/// The fixed buffer struct of struct type <typeparamref name="T"/> and size in bytes = 360
	/// </summary>
	/// <typeparam name="T">Any struct that implements <see cref="IEquatable{T}"/></typeparam>
	[StructLayout(LayoutKind.Sequential, Size = 360)]
	[UnsafeValueType]
	public unsafe struct FixedStructBuffer_360<T> : IEquatable<FixedStructBuffer_360<T>>, IReadOnlyList<T> where T : struct, IEquatable<T>
	{
		#region basic
		private static readonly int _count = 360 / Marshal.SizeOf<T>();

		private T field;

		/// <summary>
		/// Create a <see cref="Span{T}"/> from this fixed buffer
		/// </summary>
		/// <param name="size">The size of the span, default 0 means all</param>
		/// <returns>The <see cref="Span{T}"/> referring to this fixed buffer</returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="size"/> is out of range</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Span<T> AsSpan(int size = 0)
		{
			if (size == 0)
				return MemoryMarshal.CreateSpan(ref this.field, _count);
			// else
			if (size < 0 || size > _count)
				throw new ArgumentOutOfRangeException(nameof(size), size, Resources.Parameter.InvalidValue);
			return MemoryMarshal.CreateSpan(ref this.field, size);
		}

		/// <summary>
		/// Create a new array of <typeparamref name="T"/> containing the elements of this fixed buffer
		/// </summary>
		/// <returns>The array containing the elements of this fixed buffer</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T[] ToArray()
		{
			T[] array = new T[_count];
			this.AsSpan().CopyTo(array);
			return array;
		}

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

			var t = this.AsSpan();
			span.CopyTo(t[offset..]);
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

			var t = this.AsSpan();
			t[offset..].CopyTo(span);
		}
		#endregion

		#region indexer
		/// <summary>
		/// Get the number of values whose value is not default(<typeparamref name="T"/>)
		/// </summary>
		public int NonDefaults {
			get {
				var t = this.AsSpan();
				int result = 0;
				for (int i = 0; i < _count; i++)
				{
					if (!t[i].Equals(default))
						result++;
				}
				return result;
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
				return Unsafe.Add(ref this.field, index);
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set {
				if (index < 0 || index >= _count)
					throw new ArgumentOutOfRangeException(nameof(index), index, Resources.Parameter.InvalidValue);
				Unsafe.Add(ref this.field, index) = value;
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
			return this.AsSpan().Contains(value);
		}
		#endregion

		#region equality
		/// <summary>
		/// Whether this == <paramref name="other"/>
		/// </summary>
		/// <param name="other">another <see cref="FixedStructBuffer_360{T}"/> to compare</param>
		/// <returns>this == <paramref name="other"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(FixedStructBuffer_360<T> other)
		{
			return this.AsSpan().SequenceEqual(other.AsSpan());
		}

		/// <summary>
		/// Override <see cref="ValueType.Equals(object?)"/> to check whether this == <paramref name="obj"/>
		/// </summary>
		/// <param name="obj">another object to compare</param>
		/// <returns>this == <paramref name="obj"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override bool Equals(object? obj)
		{
			return obj is FixedStructBuffer_360<T> buffer && this.Equals(buffer);
		}

		/// <summary>
		/// Override <see cref="ValueType.GetHashCode"/> to get the hash code this <see cref="FixedStructBuffer_360{T}"/>.
		/// </summary>
		/// <returns>The hash code</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode()
		{
			return this.AsSpan().HashCodeOfSpan();
		}

		/// <summary>
		/// Equality operator
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(FixedStructBuffer_360<T> left, FixedStructBuffer_360<T> right)
		{
			return left.Equals(right);
		}

		/// <summary>
		/// Inequality operator
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(FixedStructBuffer_360<T> left, FixedStructBuffer_360<T> right)
		{
			return !(left == right);
		}
		#endregion

		#region string related
		/// <summary>
		/// Return the string representation of this <see cref="FixedStructBuffer_360{T}"/>
		/// </summary>
		/// <returns>the string representation of this <see cref="FixedStructBuffer_360{T}"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override string ToString()
		{
			return $"Fixed General-Struct-type Buffer [Size={_count}, Type={typeof(T).GetGenericString()}]";
		}
		#endregion
	}
}
