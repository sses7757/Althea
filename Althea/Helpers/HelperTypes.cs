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
		new T this[int index] { get; set; }

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
		/// Copy the data from the given <paramref name="span"/> to this fixed buffer
		/// </summary>
		/// <param name="span">The given <see cref="ReadOnlySpan{T}"/> to copy from</param>
		/// <param name="offset">The offset to start copying in <typeparamref name="T"/></param>
		/// <exception cref="ArgumentException">If the length of <paramref name="span"/> is too large</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offset"/> is out of boundary</exception>
		void CopyFromSpan(ReadOnlySpan<T> span, int offset = 0);

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
		/// Copy the values in <paramref name="struct"/> to this fixed buffer from <paramref name="copyStart"/> byte by byte
		/// </summary>
		/// <typeparam name="TStruct">The input struct type</typeparam>
		/// <param name="struct">The structure to copy</param>
		/// <param name="copyStart">The start position to copy of this fixed buffer in bytes</param>
		/// <exception cref="InvalidOperationException">If the size of <typeparamref name="TStruct"/> is larger than the size of this fixed buffer minus <paramref name="copyStart"/></exception>
		void FromStruct<TStruct>(TStruct @struct, int copyStart = 0) where TStruct : struct;
	}
	#endregion



	/// <summary>
	/// The fixed buffer struct of type <typeparamref name="T"/> and size in bytes = 56
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct that implements <see cref="IEquatable{T}"/></typeparam>
	[StructLayout(LayoutKind.Sequential, Size = 56)]
	[UnsafeValueType]
	public unsafe struct FixedBuffer_56<T> : IEquatable<FixedBuffer_56<T>>, IFixedBuffer<T> where T : unmanaged, IEquatable<T>
	{
		#region basic
		private readonly T field;

		/// <summary>
		/// Create a new array of <typeparamref name="T"/> containing the elements of this fixed buffer
		/// </summary>
		/// <returns>The array containing the elements of this fixed buffer</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T[] ToArray()
		{
			T[] array = new T[this.Count];
			fixed (void* t = &this)
			fixed (T* a = array)
			{
				Unsafe.CopyBlock(a, t, 56);
			}
			return array;
		}

		/// <summary>
		/// Change data type of this fixed buffer from <typeparamref name="T"/> to <typeparamref name="TOut"/>
		/// </summary>
		/// <typeparam name="TOut">The output data type, any unmanaged struct</typeparam>
		/// <returns>The fixed buffer with same byte values as this one whose data type is <typeparamref name="TOut"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public FixedBuffer_56<TOut> As<TOut>() where TOut : unmanaged, IEquatable<TOut>
		{
			var newBuffer = new FixedBuffer_56<TOut>();
			fixed (void* t = &this)
			{
				Unsafe.CopyBlock(&newBuffer, t, 56);
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
			if (offset < 0 || offset >= this.Count)
				throw new ArgumentOutOfRangeException(nameof(offset), Resources.Parameter.InvalidValue);
			if (span.Length > this.Count)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(span));
			int size = Math.Min(span.Length, this.Count);
			fixed (void* t = &this)
			{
				var temp = new Span<T>(t, size);
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
			if (offset < 0 || offset >= this.Count)
				throw new ArgumentOutOfRangeException(nameof(offset), Resources.Parameter.InvalidValue);
			if (span.Length > this.Count)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(span));
			int size = Math.Min(span.Length, this.Count);
			fixed (void* t = &this)
			{
				var temp = new ReadOnlySpan<T>(t, size);
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
			int size = Marshal.SizeOf<TStruct>();
			if (size + copyStart > 56)
				throw new InvalidOperationException(Resources.Other.InvalidGeneric);
			var s = new TStruct();
			fixed (void* t = &this)
			{
				Unsafe.CopyBlock(Unsafe.AsPointer(ref s), t, (uint)size);
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
			int size = Marshal.SizeOf<TStruct>();
			if (size + copyStart > 56)
				throw new InvalidOperationException(Resources.Other.InvalidGeneric);
			fixed (void* t = &this)
			{
				Unsafe.CopyBlock((byte*)t + copyStart, Unsafe.AsPointer(ref @struct), (uint)size);
			}
		}
		#endregion

		#region indexer
		/// <summary>
		/// Get the number of values whose value is not default(<typeparamref name="T"/>)
		/// </summary>
		public int NonDefaults {
			get {
				fixed (void* t = &this)
				{
					T* ptr = (T*)t;
					int result = 0;
					for (int i = 0; i < this.Count; i++)
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
		public int Count => 56 / sizeof(T);

		/// <summary>
		/// Basic indexer of this fixed buffer
		/// </summary>
		/// <param name="index">The index</param>
		/// <returns>The value at <paramref name="index"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="index"/> is out of range</exception>
		public T this[int index] {
			get {
				if (index < 0 || index >= this.Count)
					throw new ArgumentOutOfRangeException(nameof(index));
				fixed (void* t = &this)
				{
					return ((T*)t)[index];
				}
			}
			set {
				if (index < 0 || index >= this.Count)
					throw new ArgumentOutOfRangeException(nameof(index));
				fixed (void* t = &this)
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
			for (int i = 0; i < this.Count; i++)
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
		/// <param name="other">another <see cref="FixedBuffer_56{T}"/> to compare</param>
		/// <returns>this == <paramref name="other"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(FixedBuffer_56<T> other)
		{
			fixed (void* t = &this)
			{
				T* ptrThis = (T*)t;
				T* ptrOther = &other.field;
				for (int i = 0; i < this.Count; i++)
				{
					if (!ptrThis[i].Equals(ptrOther[i]))
						return false;
				}
				return true;
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
			return obj is FixedBuffer_56<T> buffer && this.Equals(buffer);
		}

		/// <summary>
		/// Override <see cref="ValueType.GetHashCode"/> to get the hash code this <see cref="FixedBuffer_56{T}"/>.
		/// </summary>
		/// <returns>The hash code</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode()
		{
			fixed (void* t = &this)
			{
				var temp = new ReadOnlySpan<T>(t, this.Count);
				return temp.HashCodeOfSpan();
			}
		}

		/// <summary>
		/// Equality operator
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(FixedBuffer_56<T> left, FixedBuffer_56<T> right)
		{
			return left.Equals(right);
		}

		/// <summary>
		/// Inequality operator
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(FixedBuffer_56<T> left, FixedBuffer_56<T> right)
		{
			return !(left == right);
		}
		#endregion

		#region string related
		/// <summary>
		/// Return the string representation of this <see cref="FixedBuffer_56{T}"/>
		/// </summary>
		/// <returns>the string representation of this <see cref="FixedBuffer_56{T}"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override string ToString()
		{
			return $"Fixed Buffer";
		}
		#endregion
	}


	/// <summary>
	/// The fixed buffer struct of type <typeparamref name="T"/> and size in bytes = 60
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct that implements <see cref="IEquatable{T}"/></typeparam>
	[StructLayout(LayoutKind.Sequential, Size = 60)]
	[UnsafeValueType]
	public unsafe struct FixedBuffer_60<T> : IEquatable<FixedBuffer_60<T>>, IFixedBuffer<T> where T : unmanaged, IEquatable<T>
	{
		#region basic
		private readonly T field;

		/// <summary>
		/// Create a new array of <typeparamref name="T"/> containing the elements of this fixed buffer
		/// </summary>
		/// <returns>The array containing the elements of this fixed buffer</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T[] ToArray()
		{
			T[] array = new T[this.Count];
			fixed (void* t = &this)
			fixed (T* a = array)
			{
				Unsafe.CopyBlock(a, t, 60);
			}
			return array;
		}

		/// <summary>
		/// Change data type of this fixed buffer from <typeparamref name="T"/> to <typeparamref name="TOut"/>
		/// </summary>
		/// <typeparam name="TOut">The output data type, any unmanaged struct</typeparam>
		/// <returns>The fixed buffer with same byte values as this one whose data type is <typeparamref name="TOut"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public FixedBuffer_60<TOut> As<TOut>() where TOut : unmanaged, IEquatable<TOut>
		{
			var newBuffer = new FixedBuffer_60<TOut>();
			fixed (void* t = &this)
			{
				Unsafe.CopyBlock(&newBuffer, t, 60);
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
			if (offset < 0 || offset >= this.Count)
				throw new ArgumentOutOfRangeException(nameof(offset), Resources.Parameter.InvalidValue);
			if (span.Length > this.Count)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(span));
			int size = Math.Min(span.Length, this.Count);
			fixed (void* t = &this)
			{
				var temp = new Span<T>(t, size);
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
			if (offset < 0 || offset >= this.Count)
				throw new ArgumentOutOfRangeException(nameof(offset), Resources.Parameter.InvalidValue);
			if (span.Length > this.Count)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(span));
			int size = Math.Min(span.Length, this.Count);
			fixed (void* t = &this)
			{
				var temp = new ReadOnlySpan<T>(t, size);
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
			int size = Marshal.SizeOf<TStruct>();
			if (size + copyStart > 60)
				throw new InvalidOperationException(Resources.Other.InvalidGeneric);
			var s = new TStruct();
			fixed (void* t = &this)
			{
				Unsafe.CopyBlock(Unsafe.AsPointer(ref s), t, (uint)size);
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
			int size = Marshal.SizeOf<TStruct>();
			if (size + copyStart > 60)
				throw new InvalidOperationException(Resources.Other.InvalidGeneric);
			fixed (void* t = &this)
			{
				Unsafe.CopyBlock((byte*)t + copyStart, Unsafe.AsPointer(ref @struct), (uint)size);
			}
		}
		#endregion

		#region indexer
		/// <summary>
		/// Get the number of values whose value is not default(<typeparamref name="T"/>)
		/// </summary>
		public int NonDefaults {
			get {
				fixed (void* t = &this)
				{
					T* ptr = (T*)t;
					int result = 0;
					for (int i = 0; i < this.Count; i++)
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
		public int Count => 60 / sizeof(T);

		/// <summary>
		/// Basic indexer of this fixed buffer
		/// </summary>
		/// <param name="index">The index</param>
		/// <returns>The value at <paramref name="index"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="index"/> is out of range</exception>
		public T this[int index] {
			get {
				if (index < 0 || index >= this.Count)
					throw new ArgumentOutOfRangeException(nameof(index));
				fixed (void* t = &this)
				{
					return ((T*)t)[index];
				}
			}
			set {
				if (index < 0 || index >= this.Count)
					throw new ArgumentOutOfRangeException(nameof(index));
				fixed (void* t = &this)
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
			for (int i = 0; i < this.Count; i++)
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
		/// <param name="other">another <see cref="FixedBuffer_60{T}"/> to compare</param>
		/// <returns>this == <paramref name="other"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(FixedBuffer_60<T> other)
		{
			fixed (void* t = &this)
			{
				T* ptrThis = (T*)t;
				T* ptrOther = &other.field;
				for (int i = 0; i < this.Count; i++)
				{
					if (!ptrThis[i].Equals(ptrOther[i]))
						return false;
				}
				return true;
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
			return obj is FixedBuffer_60<T> buffer && this.Equals(buffer);
		}

		/// <summary>
		/// Override <see cref="ValueType.GetHashCode"/> to get the hash code this <see cref="FixedBuffer_60{T}"/>.
		/// </summary>
		/// <returns>The hash code</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode()
		{
			fixed (void* t = &this)
			{
				var temp = new ReadOnlySpan<T>(t, this.Count);
				return temp.HashCodeOfSpan();
			}
		}

		/// <summary>
		/// Equality operator
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(FixedBuffer_60<T> left, FixedBuffer_60<T> right)
		{
			return left.Equals(right);
		}

		/// <summary>
		/// Inequality operator
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
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
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override string ToString()
		{
			return $"Fixed Buffer";
		}
		#endregion
	}


	/// <summary>
	/// The read-only fixed buffer struct of type <typeparamref name="T"/> with maximum data size in bytes = 128. There are extra 4 bytes used to store the data size in <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct that implements <see cref="IEquatable{T}"/></typeparam>
	[StructLayout(LayoutKind.Sequential, Size = 128 + sizeof(int))]
	[UnsafeValueType]
	public unsafe struct SizedFixedBuffer_128<T> : IEquatable<SizedFixedBuffer_128<T>>, IFixedBuffer<T> where T : unmanaged, IEquatable<T>
	{
		#region basic
		private readonly int size;

		private T field;

		/// <summary>
		/// The number of elements in this fixed buffer
		/// </summary>
		public int Count => this.size;

		/// <summary>
		/// Create a new <see cref="SizedFixedBuffer_128{T}"/> with given <paramref name="data"/>
		/// </summary>
		/// <param name="data">The data as a <see cref="ReadOnlySpan{T}"/></param>
		public SizedFixedBuffer_128(ReadOnlySpan<T> data)
		{
			this.size = data.Length;
			this.field = default;
			this.CopyFromSpan(data);
		}

		/// <summary>
		/// Create a new array of <typeparamref name="T"/> containing the elements of this fixed buffer
		/// </summary>
		/// <returns>The array containing the elements of this fixed buffer</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T[] ToArray()
		{
			T[] array = new T[this.size];
			fixed (void* t = &this)
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
		public SizedFixedBuffer_128<TOut> As<TOut>() where TOut : unmanaged, IEquatable<TOut>
		{
			var newBuffer = new SizedFixedBuffer_128<TOut>();
			fixed (void* t = &this)
			{
				Unsafe.CopyBlock(&newBuffer, t, unchecked((uint)(this.size * sizeof(T))));
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
			if (offset < 0 || offset >= this.size)
				throw new ArgumentOutOfRangeException(nameof(offset), Resources.Parameter.InvalidValue);
			if (span.Length > this.size)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(span));
			int size = Math.Min(span.Length, this.size);
			fixed (void* t = &this)
			{
				var temp = new Span<T>(t, size);
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
			if (offset < 0 || offset >= this.size)
				throw new ArgumentOutOfRangeException(nameof(offset), Resources.Parameter.InvalidValue);
			if (span.Length > this.size)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(span));
			int size = Math.Min(span.Length, this.size);
			fixed (void* t = &this)
			{
				var temp = new ReadOnlySpan<T>(t, size);
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
			int size = Marshal.SizeOf<TStruct>();
			if (size + copyStart > this.size * sizeof(T))
				throw new InvalidOperationException(Resources.Other.InvalidGeneric);
			var s = new TStruct();
			fixed (void* t = &this)
			{
				Unsafe.CopyBlock(Unsafe.AsPointer(ref s), t, (uint)size);
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
			int size = Marshal.SizeOf<TStruct>();
			if (size + copyStart > this.size * sizeof(T))
				throw new InvalidOperationException(Resources.Other.InvalidGeneric);
			fixed (void* t = &this)
			{
				Unsafe.CopyBlock((byte*)t + copyStart, Unsafe.AsPointer(ref @struct), (uint)size);
			}
		}

		/// <summary>
		/// Create a <see cref="ReadOnlySpan{T}"/> from this sized fixed buffer
		/// </summary>
		/// <returns>The <see cref="ReadOnlySpan{T}"/> referring to this sized fixed buffer</returns>
		public ReadOnlySpan<T> AsSpan()
		{
			return MemoryMarshal.CreateReadOnlySpan(ref this.field, this.size);
		}
		#endregion

		#region indexer
		/// <summary>
		/// Get the number of values whose value is not default(<typeparamref name="T"/>)
		/// </summary>
		public int NonDefaults {
			get {
				fixed (void* t = &this)
				{
					T* ptr = (T*)t;
					int result = 0;
					for (int i = 0; i < this.size; i++)
					{
						if (!ptr[i].Equals(default))
							result++;
					}
					return result;
				}
			}
		}

		/// <summary>
		/// Basic indexer of this fixed buffer
		/// </summary>
		/// <param name="index">The index</param>
		/// <returns>The value at <paramref name="index"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="index"/> is out of range</exception>
		public T this[int index] {
			get {
				if (index < 0 || index >= this.size)
					throw new ArgumentOutOfRangeException(nameof(index));
				fixed (void* t = &this)
				{
					return ((T*)t)[index];
				}
			}
			set {
				if (index < 0 || index >= this.size)
					throw new ArgumentOutOfRangeException(nameof(index));
				fixed (void* t = &this)
				{
					((T*)t)[index] = value;
				}
			}
		}

		/// <summary>
		/// Returns an enumerator that iterates through the collection.
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the collection.</returns>
		public IEnumerator<T> GetEnumerator()
		{
			for (int i = 0; i < this.size; i++)
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
		/// <param name="other">another <see cref="SizedFixedBuffer_128{T}"/> to compare</param>
		/// <returns>this == <paramref name="other"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(SizedFixedBuffer_128<T> other)
		{
			fixed (void* t = &this)
			{
				T* ptrThis = (T*)t;
				T* ptrOther = &other.field;
				for (int i = 0; i < this.size; i++)
				{
					if (!ptrThis[i].Equals(ptrOther[i]))
						return false;
				}
				return true;
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
			return obj is SizedFixedBuffer_128<T> buffer && this.Equals(buffer);
		}

		/// <summary>
		/// Override <see cref="ValueType.GetHashCode"/> to get the hash code this <see cref="SizedFixedBuffer_128{T}"/>.
		/// </summary>
		/// <returns>The hash code</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode()
		{
			fixed (void* t = &this)
			{
				var temp = new ReadOnlySpan<T>(t, this.size);
				return temp.HashCodeOfSpan();
			}
		}

		/// <summary>
		/// Equality operator
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(SizedFixedBuffer_128<T> left, SizedFixedBuffer_128<T> right)
		{
			return left.Equals(right);
		}

		/// <summary>
		/// Inequality operator
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(SizedFixedBuffer_128<T> left, SizedFixedBuffer_128<T> right)
		{
			return !(left == right);
		}
		#endregion

		#region string related
		/// <summary>
		/// Return the string representation of this <see cref="SizedFixedBuffer_128{T}"/>
		/// </summary>
		/// <returns>the string representation of this <see cref="SizedFixedBuffer_128{T}"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override string ToString()
		{
			return $"Read-only Fixed Buffer";
		}
		#endregion

		#region converter
		/// <summary>
		/// Implicitly convert the given <paramref name="span"/> to a <see cref="SizedFixedBuffer_128{T}"/>
		/// </summary>
		/// <param name="span">The given <see cref="ReadOnlySpan{T}"/> to be converted</param>
		public static implicit operator SizedFixedBuffer_128<T>(ReadOnlySpan<T> span) => new SizedFixedBuffer_128<T>(span);
		#endregion
	}
}
