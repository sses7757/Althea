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
	/// <typeparam name="T">any unmanaged struct</typeparam>
	public interface IFixedBuffer<T> : IEnumerable<T>, IEnumerable, IReadOnlyCollection<T>, IReadOnlyList<T> where T : unmanaged, IEquatable<T>
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
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="index"/> is out of range</exception>
		new T this[int index] { get; set; }

		/// <summary>
		/// Get the <see cref="Span{T}"/> representation of this fixed buffer
		/// </summary>
		/// <returns>The <see cref="Span{T}"/> representation of this fixed buffer</returns>
		Span<T> AsSpan();

		/// <summary>
		/// Create a new array of <typeparamref name="T"/> containing the elements of this fixed buffer
		/// </summary>
		/// <returns>The array containing the elements of this fixed buffer</returns>
		T[] ToArray();

		/// <summary>
		/// Change data type of this fixed buffer from <typeparamref name="T"/> to <typeparamref name="TOut"/>
		/// </summary>
		/// <typeparam name="TOut">The output data type, any unmanaged struct</typeparam>
		/// <returns>The fixed buffer with same byte values as this one whose data type is <typeparamref name="TOut"/></returns>
		IFixedBuffer<TOut> As<TOut>() where TOut : unmanaged, IEquatable<TOut>;

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
	/// The fixed buffer struct of type <typeparamref name="T"/> and size in bytes = 28
	/// </summary>
	/// <typeparam name="T">any unmanaged struct</typeparam>
	[StructLayout(LayoutKind.Sequential, Size = 28)]
	[UnsafeValueType]
	public unsafe struct FixedBuffer_36<T> : IEquatable<FixedBuffer_36<T>>, IFixedBuffer<T> where T : unmanaged, IEquatable<T>
	{
		#region basic
		private T field;

		private T* Pointer => (T*)Unsafe.AsPointer(ref this.field);

		/// <summary>
		/// Get the <see cref="Span{T}"/> representation of this fixed buffer
		/// </summary>
		/// <returns>The <see cref="Span{T}"/> representation of this fixed buffer</returns>
		public Span<T> AsSpan() => new Span<T>(this.Pointer, this.Count);

		/// <summary>
		/// Create a new array of <typeparamref name="T"/> containing the elements of this fixed buffer
		/// </summary>
		/// <returns>The array containing the elements of this fixed buffer</returns>
		public T[] ToArray() => this.AsSpan().ToArray();

		/// <summary>
		/// Change data type of this fixed buffer from <typeparamref name="T"/> to <typeparamref name="TOut"/>
		/// </summary>
		/// <typeparam name="TOut">The output data type, any unmanaged struct</typeparam>
		/// <returns>The fixed buffer with same byte values as this one whose data type is <typeparamref name="TOut"/></returns>
		public FixedBuffer_36<TOut> As<TOut>() where TOut : unmanaged, IEquatable<TOut>
		{
			var newBuffer = new FixedBuffer_36<TOut>();
			Unsafe.CopyBlock(newBuffer.Pointer, this.Pointer, 36);
			return newBuffer;
		}

		IFixedBuffer<TOut> IFixedBuffer<T>.As<TOut>() => this.As<TOut>();


		/// <summary>
		/// Convert this fixed buffer to a new <typeparamref name="TStruct"/> by copying the values from <paramref name="copyStart"/> byte by byte
		/// </summary>
		/// <typeparam name="TStruct">The output struct type</typeparam>
		/// <param name="copyStart">The start position to copy in bytes</param>
		/// <returns>The created <typeparamref name="TStruct"/></returns>
		/// <exception cref="InvalidOperationException">If the size of <typeparamref name="TStruct"/> is larger than the size of this fixed buffer minus <paramref name="copyStart"/></exception>
		public TStruct ToStruct<TStruct>(int copyStart = 0) where TStruct : struct
		{
			int size = Marshal.SizeOf<TStruct>();
			if (size + copyStart > 36)
				throw new InvalidOperationException(Resources.Other.InvalidGeneric);
			var s = new TStruct();
			Unsafe.CopyBlock(Unsafe.AsPointer(ref s), this.Pointer, (uint)size);
			return s;
		}

		/// <summary>
		/// Copy the values in <paramref name="struct"/> to this fixed buffer from <paramref name="copyStart"/> byte by byte
		/// </summary>
		/// <typeparam name="TStruct">The input struct type</typeparam>
		/// <param name="struct">The structure to copy</param>
		/// <param name="copyStart">The start position to copy of this fixed buffer in bytes</param>
		/// <exception cref="InvalidOperationException">If the size of <typeparamref name="TStruct"/> is larger than the size of this fixed buffer minus <paramref name="copyStart"/></exception>
		public void FromStruct<TStruct>(TStruct @struct, int copyStart = 0) where TStruct : struct
		{
			int size = Marshal.SizeOf<TStruct>();
			if (size + copyStart > 36)
				throw new InvalidOperationException(Resources.Other.InvalidGeneric);
			Unsafe.CopyBlock(this.Pointer + copyStart, Unsafe.AsPointer(ref @struct), (uint)size);
		}
		#endregion

		#region indexer
		/// <summary>
		/// Get the number of values whose value is not default(<typeparamref name="T"/>)
		/// </summary>
		public int NonDefaults {
			get {
				var span = this.AsSpan();
				int result = 0;
				for (int i = 0; i < this.Count; i++)
				{
					if (!span[i].Equals(default))
						result++;
				}
				return result;
			}
		}

		/// <summary>
		/// The number of elements in this fixed buffer
		/// </summary>
		public int Count => 36 / sizeof(T);

		/// <summary>
		/// Basic indexer of this fixed buffer
		/// </summary>
		/// <param name="index">The index</param>
		/// <returns>The value at <paramref name="index"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="index"/> is out of range</exception>
		public T this[int index] {
			get => index >= 0 && index < this.Count ? Pointer[index] : throw new ArgumentOutOfRangeException(nameof(index));
			set => Pointer[index] = index >= 0 && index < this.Count ? value : throw new ArgumentOutOfRangeException(nameof(index));
		}

		/// <summary>
		/// Returns an enumerator that iterates through the collection.
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the collection.</returns>
		public IEnumerator<T> GetEnumerator()
		{
			var span = this.AsSpan();
			var iter = span.GetEnumerator();
			yield return iter.Current;
			while (iter.MoveNext())
			{
				yield return iter.Current;
			}
		}

		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		#endregion

		#region equality
		/// <summary>
		/// Whether this == <paramref name="other"/>
		/// </summary>
		/// <param name="other">another <see cref="FixedBuffer_36{T}"/> to compare</param>
		/// <returns>this == <paramref name="other"/></returns>
		public bool Equals(FixedBuffer_36<T> other)
		{
			T* ptrThis = this.Pointer;
			T* ptrOther = other.Pointer;
			for (int i = 0; i < this.Count; i++)
			{
				if (!ptrThis[i].Equals(ptrOther[i]))
					return false;
			}
			return true;
		}

		/// <summary>
		/// Override <see cref="ValueType.Equals(object?)"/> to check whether this == <paramref name="obj"/>
		/// </summary>
		/// <param name="obj">another object to compare</param>
		/// <returns>this == <paramref name="obj"/></returns>
		public override bool Equals(object? obj)
		{
			return obj is FixedBuffer_36<T> buffer && this.Equals(buffer);
		}

		/// <summary>
		/// Override <see cref="ValueType.GetHashCode"/> to get the hash code this <see cref="FixedBuffer_36{T}"/>.
		/// </summary>
		/// <returns>The hash code</returns>
		public override int GetHashCode() => ((ReadOnlySpan<T>)this.AsSpan()).HashCodeOfSpan();

		/// <summary>
		/// Equality operator
		/// </summary>
		public static bool operator ==(FixedBuffer_36<T> left, FixedBuffer_36<T> right)
		{
			return left.Equals(right);
		}

		/// <summary>
		/// Inequality operator
		/// </summary>
		public static bool operator !=(FixedBuffer_36<T> left, FixedBuffer_36<T> right)
		{
			return !(left == right);
		}
		#endregion

		#region string related
		/// <summary>
		/// Return the string representation of this <see cref="FixedBuffer_36{T}"/>
		/// </summary>
		/// <returns>the string representation of this <see cref="FixedBuffer_36{T}"/></returns>
		public override string ToString()
		{
			return $"Fixed Buffer [{string.Join(", ", this)}]";
		}
		#endregion
	}

	/// <summary>
	/// The fixed buffer struct of type <typeparamref name="T"/> and size in bytes = 60
	/// </summary>
	/// <typeparam name="T">any unmanaged struct</typeparam>
	[StructLayout(LayoutKind.Sequential, Size = 60)]
	[UnsafeValueType]
	public unsafe struct FixedBuffer_60<T> : IEquatable<FixedBuffer_60<T>>, IFixedBuffer<T> where T : unmanaged, IEquatable<T>
	{
		#region basic
		private T field;

		private T* Pointer => (T*)Unsafe.AsPointer(ref this.field);

		/// <summary>
		/// Get the <see cref="Span{T}"/> representation of this fixed buffer
		/// </summary>
		/// <returns>The <see cref="Span{T}"/> representation of this fixed buffer</returns>
		public Span<T> AsSpan() => new Span<T>(this.Pointer, this.Count);

		/// <summary>
		/// Create a new array of <typeparamref name="T"/> containing the elements of this fixed buffer
		/// </summary>
		/// <returns>The array containing the elements of this fixed buffer</returns>
		public T[] ToArray() => this.AsSpan().ToArray();

		/// <summary>
		/// Change data type of this fixed buffer from <typeparamref name="T"/> to <typeparamref name="TOut"/>
		/// </summary>
		/// <typeparam name="TOut">The output data type, any unmanaged struct</typeparam>
		/// <returns>The fixed buffer with same byte values as this one whose data type is <typeparamref name="TOut"/></returns>
		public FixedBuffer_60<TOut> As<TOut>() where TOut : unmanaged, IEquatable<TOut>
		{
			var newBuffer = new FixedBuffer_60<TOut>();
			Unsafe.CopyBlock(newBuffer.Pointer, this.Pointer, 60);
			return newBuffer;
		}

		IFixedBuffer<TOut> IFixedBuffer<T>.As<TOut>() => this.As<TOut>();


		/// <summary>
		/// Convert this fixed buffer to a new <typeparamref name="TStruct"/> by copying the values from <paramref name="copyStart"/> byte by byte
		/// </summary>
		/// <typeparam name="TStruct">The output struct type</typeparam>
		/// <param name="copyStart">The start position to copy in bytes</param>
		/// <returns>The created <typeparamref name="TStruct"/></returns>
		/// <exception cref="InvalidOperationException">If the size of <typeparamref name="TStruct"/> is larger than the size of this fixed buffer minus <paramref name="copyStart"/></exception>
		public TStruct ToStruct<TStruct>(int copyStart = 0) where TStruct : struct
		{
			int size = Marshal.SizeOf<TStruct>();
			if (size + copyStart > 60)
				throw new InvalidOperationException(Resources.Other.InvalidGeneric);
			var s = new TStruct();
			Unsafe.CopyBlock(Unsafe.AsPointer(ref s), this.Pointer, (uint)size);
			return s;
		}

		/// <summary>
		/// Copy the values in <paramref name="struct"/> to this fixed buffer from <paramref name="copyStart"/> byte by byte
		/// </summary>
		/// <typeparam name="TStruct">The input struct type</typeparam>
		/// <param name="struct">The structure to copy</param>
		/// <param name="copyStart">The start position to copy of this fixed buffer in bytes</param>
		/// <exception cref="InvalidOperationException">If the size of <typeparamref name="TStruct"/> is larger than the size of this fixed buffer minus <paramref name="copyStart"/></exception>
		public void FromStruct<TStruct>(TStruct @struct, int copyStart = 0) where TStruct : struct
		{
			int size = Marshal.SizeOf<TStruct>();
			if (size + copyStart > 60)
				throw new InvalidOperationException(Resources.Other.InvalidGeneric);
			Unsafe.CopyBlock(this.Pointer + copyStart, Unsafe.AsPointer(ref @struct), (uint)size);
		}
		#endregion

		#region indexer
		/// <summary>
		/// Get the number of values whose value is not default(<typeparamref name="T"/>)
		/// </summary>
		public int NonDefaults {
			get {
				var span = this.AsSpan();
				int result = 0;
				for (int i = 0; i < this.Count; i++)
				{
					if (!span[i].Equals(default))
						result++;
				}
				return result;
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
			get => index >= 0 && index < this.Count ? Pointer[index] : throw new ArgumentOutOfRangeException(nameof(index));
			set => Pointer[index] = index >= 0 && index < this.Count ? value : throw new ArgumentOutOfRangeException(nameof(index));
		}

		/// <summary>
		/// Returns an enumerator that iterates through the collection.
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the collection.</returns>
		public IEnumerator<T> GetEnumerator()
		{
			var span = this.AsSpan();
			var iter = span.GetEnumerator();
			yield return iter.Current;
			while (iter.MoveNext())
			{
				yield return iter.Current;
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
			T* ptrThis = this.Pointer;
			T* ptrOther = other.Pointer;
			for (int i = 0; i < this.Count; i++)
			{
				if (!ptrThis[i].Equals(ptrOther[i]))
					return false;
			}
			return true;
		}

		/// <summary>
		/// Override <see cref="ValueType.Equals(object?)"/> to check whether this == <paramref name="obj"/>
		/// </summary>
		/// <param name="obj">another object to compare</param>
		/// <returns>this == <paramref name="obj"/></returns>
		public override bool Equals(object? obj)
		{
			return obj is FixedBuffer_60<T> buffer && this.Equals(buffer);
		}

		/// <summary>
		/// Override <see cref="ValueType.GetHashCode"/> to get the hash code this <see cref="FixedBuffer_60{T}"/>.
		/// </summary>
		/// <returns>The hash code</returns>
		public override int GetHashCode() => ((ReadOnlySpan<T>)this.AsSpan()).HashCodeOfSpan();

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
			return $"Fixed Buffer [{string.Join(", ", this)}]";
		}
		#endregion
	}


	/// <summary>
	/// The fixed buffer struct of type <typeparamref name="T"/> and size in bytes = 64
	/// </summary>
	/// <typeparam name="T">any unmanaged struct</typeparam>
	[StructLayout(LayoutKind.Sequential, Size = 64)]
	[UnsafeValueType]
	public unsafe struct FixedBuffer_64<T> : IEquatable<FixedBuffer_64<T>>, IFixedBuffer<T> where T : unmanaged, IEquatable<T>
	{
		#region basic
		private T field;

		private T* Pointer => (T*)Unsafe.AsPointer(ref this.field);

		/// <summary>
		/// Get the <see cref="Span{T}"/> representation of this fixed buffer
		/// </summary>
		/// <returns>The <see cref="Span{T}"/> representation of this fixed buffer</returns>
		public Span<T> AsSpan() => new Span<T>(this.Pointer, this.Count);

		/// <summary>
		/// Create a new array of <typeparamref name="T"/> containing the elements of this fixed buffer
		/// </summary>
		/// <returns>The array containing the elements of this fixed buffer</returns>
		public T[] ToArray() => this.AsSpan().ToArray();

		/// <summary>
		/// Change data type of this fixed buffer from <typeparamref name="T"/> to <typeparamref name="TOut"/>
		/// </summary>
		/// <typeparam name="TOut">The output data type, any unmanaged struct</typeparam>
		/// <returns>The fixed buffer with same byte values as this one whose data type is <typeparamref name="TOut"/></returns>
		public FixedBuffer_64<TOut> As<TOut>() where TOut : unmanaged, IEquatable<TOut>
		{
			var newBuffer = new FixedBuffer_64<TOut>();
			Unsafe.CopyBlock(newBuffer.Pointer, this.Pointer, 64);
			return newBuffer;
		}

		IFixedBuffer<TOut> IFixedBuffer<T>.As<TOut>() => this.As<TOut>();


		/// <summary>
		/// Convert this fixed buffer to a new <typeparamref name="TStruct"/> by copying the values from <paramref name="copyStart"/> byte by byte
		/// </summary>
		/// <typeparam name="TStruct">The output struct type</typeparam>
		/// <param name="copyStart">The start position to copy in bytes</param>
		/// <returns>The created <typeparamref name="TStruct"/></returns>
		/// <exception cref="InvalidOperationException">If the size of <typeparamref name="TStruct"/> is larger than the size of this fixed buffer minus <paramref name="copyStart"/></exception>
		public TStruct ToStruct<TStruct>(int copyStart = 0) where TStruct : struct
		{
			int size = Marshal.SizeOf<TStruct>();
			if (size + copyStart > 64)
				throw new InvalidOperationException(Resources.Other.InvalidGeneric);
			var s = new TStruct();
			Unsafe.CopyBlock(Unsafe.AsPointer(ref s), this.Pointer, (uint)size);
			return s;
		}

		/// <summary>
		/// Copy the values in <paramref name="struct"/> to this fixed buffer from <paramref name="copyStart"/> byte by byte
		/// </summary>
		/// <typeparam name="TStruct">The input struct type</typeparam>
		/// <param name="struct">The structure to copy</param>
		/// <param name="copyStart">The start position to copy of this fixed buffer in bytes</param>
		/// <exception cref="InvalidOperationException">If the size of <typeparamref name="TStruct"/> is larger than the size of this fixed buffer minus <paramref name="copyStart"/></exception>
		public void FromStruct<TStruct>(TStruct @struct, int copyStart = 0) where TStruct : struct
		{
			int size = Marshal.SizeOf<TStruct>();
			if (size + copyStart > 64)
				throw new InvalidOperationException(Resources.Other.InvalidGeneric);
			Unsafe.CopyBlock(this.Pointer + copyStart, Unsafe.AsPointer(ref @struct), (uint)size);
		}
		#endregion

		#region indexer
		/// <summary>
		/// Get the number of values whose value is not default(<typeparamref name="T"/>)
		/// </summary>
		public int NonDefaults {
			get {
				var span = this.AsSpan();
				int result = 0;
				for (int i = 0; i < this.Count; i++)
				{
					if (!span[i].Equals(default))
						result++;
				}
				return result;
			}
		}

		/// <summary>
		/// The number of elements in this fixed buffer
		/// </summary>
		public int Count => 64 / sizeof(T);

		/// <summary>
		/// Basic indexer of this fixed buffer
		/// </summary>
		/// <param name="index">The index</param>
		/// <returns>The value at <paramref name="index"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="index"/> is out of range</exception>
		public T this[int index] {
			get => index >= 0 && index < this.Count ? Pointer[index] : throw new ArgumentOutOfRangeException(nameof(index));
			set => Pointer[index] = index >= 0 && index < this.Count ? value : throw new ArgumentOutOfRangeException(nameof(index));
		}

		/// <summary>
		/// Returns an enumerator that iterates through the collection.
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the collection.</returns>
		public IEnumerator<T> GetEnumerator()
		{
			var span = this.AsSpan();
			var iter = span.GetEnumerator();
			yield return iter.Current;
			while (iter.MoveNext())
			{
				yield return iter.Current;
			}
		}

		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		#endregion

		#region equality
		/// <summary>
		/// Whether this == <paramref name="other"/>
		/// </summary>
		/// <param name="other">another <see cref="FixedBuffer_64{T}"/> to compare</param>
		/// <returns>this == <paramref name="other"/></returns>
		public bool Equals(FixedBuffer_64<T> other)
		{
			T* ptrThis = this.Pointer;
			T* ptrOther = other.Pointer;
			for (int i = 0; i < this.Count; i++)
			{
				if (!ptrThis[i].Equals(ptrOther[i]))
					return false;
			}
			return true;
		}

		/// <summary>
		/// Override <see cref="ValueType.Equals(object?)"/> to check whether this == <paramref name="obj"/>
		/// </summary>
		/// <param name="obj">another object to compare</param>
		/// <returns>this == <paramref name="obj"/></returns>
		public override bool Equals(object? obj)
		{
			return obj is FixedBuffer_64<T> buffer && this.Equals(buffer);
		}

		/// <summary>
		/// Override <see cref="ValueType.GetHashCode"/> to get the hash code this <see cref="FixedBuffer_64{T}"/>.
		/// </summary>
		/// <returns>The hash code</returns>
		public override int GetHashCode() => ((ReadOnlySpan<T>)this.AsSpan()).HashCodeOfSpan();

		/// <summary>
		/// Equality operator
		/// </summary>
		public static bool operator ==(FixedBuffer_64<T> left, FixedBuffer_64<T> right)
		{
			return left.Equals(right);
		}

		/// <summary>
		/// Inequality operator
		/// </summary>
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
		public override string ToString()
		{
			return $"Fixed Buffer [{string.Join(", ", this)}]";
		}
		#endregion
	}


	/// <summary>
	/// The fixed buffer struct of type <typeparamref name="T"/> and size in bytes = 128
	/// </summary>
	/// <typeparam name="T">any unmanaged struct</typeparam>
	[StructLayout(LayoutKind.Sequential, Size = 128)]
	[UnsafeValueType]
	public unsafe struct FixedBuffer_128<T> : IEquatable<FixedBuffer_128<T>>, IFixedBuffer<T> where T : unmanaged, IEquatable<T>
	{
		#region basic
		private T field;

		private T* Pointer => (T*)Unsafe.AsPointer(ref this.field);

		/// <summary>
		/// Get the <see cref="Span{T}"/> representation of this fixed buffer
		/// </summary>
		/// <returns>The <see cref="Span{T}"/> representation of this fixed buffer</returns>
		public Span<T> AsSpan() => new Span<T>(this.Pointer, this.Count);

		/// <summary>
		/// Create a new array of <typeparamref name="T"/> containing the elements of this fixed buffer
		/// </summary>
		/// <returns>The array containing the elements of this fixed buffer</returns>
		public T[] ToArray() => this.AsSpan().ToArray();

		/// <summary>
		/// Change data type of this fixed buffer from <typeparamref name="T"/> to <typeparamref name="TOut"/>
		/// </summary>
		/// <typeparam name="TOut">The output data type, any unmanaged struct</typeparam>
		/// <returns>The fixed buffer with same byte values as this one whose data type is <typeparamref name="TOut"/></returns>
		public FixedBuffer_128<TOut> As<TOut>() where TOut : unmanaged, IEquatable<TOut>
		{
			var newBuffer = new FixedBuffer_128<TOut>();
			Unsafe.CopyBlock(newBuffer.Pointer, this.Pointer, 128);
			return newBuffer;
		}

		IFixedBuffer<TOut> IFixedBuffer<T>.As<TOut>() => this.As<TOut>();


		/// <summary>
		/// Convert this fixed buffer to a new <typeparamref name="TStruct"/> by copying the values from <paramref name="copyStart"/> byte by byte
		/// </summary>
		/// <typeparam name="TStruct">The output struct type</typeparam>
		/// <param name="copyStart">The start position to copy in bytes</param>
		/// <returns>The created <typeparamref name="TStruct"/></returns>
		/// <exception cref="InvalidOperationException">If the size of <typeparamref name="TStruct"/> is larger than the size of this fixed buffer minus <paramref name="copyStart"/></exception>
		public TStruct ToStruct<TStruct>(int copyStart = 0) where TStruct : struct
		{
			int size = Marshal.SizeOf<TStruct>();
			if (size + copyStart > 128)
				throw new InvalidOperationException(Resources.Other.InvalidGeneric);
			var s = new TStruct();
			Unsafe.CopyBlock(Unsafe.AsPointer(ref s), this.Pointer, (uint)size);
			return s;
		}

		/// <summary>
		/// Copy the values in <paramref name="struct"/> to this fixed buffer from <paramref name="copyStart"/> byte by byte
		/// </summary>
		/// <typeparam name="TStruct">The input struct type</typeparam>
		/// <param name="struct">The structure to copy</param>
		/// <param name="copyStart">The start position to copy of this fixed buffer in bytes</param>
		/// <exception cref="InvalidOperationException">If the size of <typeparamref name="TStruct"/> is larger than the size of this fixed buffer minus <paramref name="copyStart"/></exception>
		public void FromStruct<TStruct>(TStruct @struct, int copyStart = 0) where TStruct : struct
		{
			int size = Marshal.SizeOf<TStruct>();
			if (size + copyStart > 128)
				throw new InvalidOperationException(Resources.Other.InvalidGeneric);
			Unsafe.CopyBlock(this.Pointer + copyStart, Unsafe.AsPointer(ref @struct), (uint)size);
		}
		#endregion

		#region indexer
		/// <summary>
		/// Get the number of values whose value is not default(<typeparamref name="T"/>)
		/// </summary>
		public int NonDefaults {
			get {
				var span = this.AsSpan();
				int result = 0;
				for (int i = 0; i < this.Count; i++)
				{
					if (!span[i].Equals(default))
						result++;
				}
				return result;
			}
		}

		/// <summary>
		/// The number of elements in this fixed buffer
		/// </summary>
		public int Count => 128 / sizeof(T);

		/// <summary>
		/// Basic indexer of this fixed buffer
		/// </summary>
		/// <param name="index">The index</param>
		/// <returns>The value at <paramref name="index"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="index"/> is out of range</exception>
		public T this[int index] {
			get => index >= 0 && index < this.Count ? Pointer[index] : throw new ArgumentOutOfRangeException(nameof(index));
			set => Pointer[index] = index >= 0 && index < this.Count ? value : throw new ArgumentOutOfRangeException(nameof(index));
		}

		/// <summary>
		/// Returns an enumerator that iterates through the collection.
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the collection.</returns>
		public IEnumerator<T> GetEnumerator()
		{
			var span = this.AsSpan();
			var iter = span.GetEnumerator();
			yield return iter.Current;
			while (iter.MoveNext())
			{
				yield return iter.Current;
			}
		}

		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		#endregion

		#region equality
		/// <summary>
		/// Whether this == <paramref name="other"/>
		/// </summary>
		/// <param name="other">another <see cref="FixedBuffer_128{T}"/> to compare</param>
		/// <returns>this == <paramref name="other"/></returns>
		public bool Equals(FixedBuffer_128<T> other)
		{
			T* ptrThis = this.Pointer;
			T* ptrOther = other.Pointer;
			for (int i = 0; i < this.Count; i++)
			{
				if (!ptrThis[i].Equals(ptrOther[i]))
					return false;
			}
			return true;
		}

		/// <summary>
		/// Override <see cref="ValueType.Equals(object?)"/> to check whether this == <paramref name="obj"/>
		/// </summary>
		/// <param name="obj">another object to compare</param>
		/// <returns>this == <paramref name="obj"/></returns>
		public override bool Equals(object? obj)
		{
			return obj is FixedBuffer_128<T> buffer && this.Equals(buffer);
		}

		/// <summary>
		/// Override <see cref="ValueType.GetHashCode"/> to get the hash code this <see cref="FixedBuffer_128{T}"/>.
		/// </summary>
		/// <returns>The hash code</returns>
		public override int GetHashCode() => ((ReadOnlySpan<T>)this.AsSpan()).HashCodeOfSpan();

		/// <summary>
		/// Equality operator
		/// </summary>
		public static bool operator ==(FixedBuffer_128<T> left, FixedBuffer_128<T> right)
		{
			return left.Equals(right);
		}

		/// <summary>
		/// Inequality operator
		/// </summary>
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
		public override string ToString()
		{
			return $"Fixed Buffer [{string.Join(", ", this)}]";
		}
		#endregion
	}
}
