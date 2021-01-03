using Althea.Helpers;
using Althea.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using RT = Althea.Runtime.API;

namespace Althea.Memory
{
	#region storage location
	/// <summary>
	/// The struct of a storage location
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public readonly struct StorageLocation : IEquatable<StorageLocation>
	{
		#region basic
		/// <summary>
		/// The enum of the location, all values larger than <see cref="OtherMemory_0"/> are all considered as some platform-specific local memories.
		/// </summary>
		public enum LocationEnum : byte
		{
			/// <summary>
			/// Represents an unknown storage position
			/// </summary>
			Unknown = 0,
			/// <summary>
			/// Storage at local CPU memory
			/// </summary>
			CpuMemory = 1,
			/// <summary>
			/// Storage at local GPU memory
			/// </summary>
			GpuMemory = 2,
			/// <summary>
			/// Storage at platform-specific local memory (with custom order the 1st) other than <see cref="CpuMemory"/> and <see cref="GpuMemory"/>. For example, a RAM associated with a FPGA.
			/// </summary>
			OtherMemory_0 = 3,
			/// <summary>
			/// Storage at platform-specific local memory (with custom order the 2nd) other than <see cref="CpuMemory"/> and <see cref="GpuMemory"/>. For example, a RAM associated with a FPGA.
			/// </summary>
			OtherMemory_1 = 4,
			/// <summary>
			/// Storage at platform-specific local memory (with custom order the 3rd) other than <see cref="CpuMemory"/> and <see cref="GpuMemory"/>. For example, a RAM associated with a FPGA.
			/// </summary>
			OtherMemory_2 = 5,
		}

		private readonly LocationEnum location;

		private readonly byte deviceID;

		/// <summary>
		/// The location of this <see cref="StorageLocation"/>
		/// </summary>
		public LocationEnum Location => location;

		/// <summary>
		/// The device ID of the given <see cref="Location"/>
		/// </summary>
		public byte DeviceID => deviceID;

		/// <summary>
		/// Create with given location and device ID
		/// </summary>
		/// <param name="location">The location of this <see cref="StorageLocation"/></param>
		/// <param name="deviceID">The device ID of the given <paramref name="location"/></param>
		public StorageLocation(LocationEnum location, byte deviceID)
		{
			this.location = location; this.deviceID = deviceID;
		}

		/// <summary>
		/// Get the order for this <see cref="StorageLocation"/>'s <see cref="LocationEnum"/> if it represents a storage position in other memory types like <see cref="LocationEnum.OtherMemory_0"/>
		/// </summary>
		/// <returns>0 if this is not a memory of other types, otherwise the order for this <see cref="StorageLocation"/>'s <see cref="LocationEnum"/></returns>
		public byte OrderOfOtherMemoryType()
		{
			if (this.location < LocationEnum.OtherMemory_0)
				return 0;
			return this.location - LocationEnum.OtherMemory_0;
		}
		#endregion

		#region equality
		/// <summary>
		/// Whether this == <paramref name="other"/>
		/// </summary>
		/// <param name="other">another <see cref="StorageLocation"/> to compare</param>
		/// <returns>this == <paramref name="other"/></returns>
		public bool Equals(StorageLocation other)
		{
			return this.location == other.location && this.deviceID == other.deviceID;
		}

		/// <summary>
		/// Override <see cref="ValueType.Equals(object?)"/> to check whether this == <paramref name="obj"/>
		/// </summary>
		/// <param name="obj">another object to compare</param>
		/// <returns>this == <paramref name="obj"/></returns>
		public override bool Equals(object obj)
		{
			return obj is StorageLocation location1 && Equals(location1);
		}

		/// <summary>
		/// Override <see cref="ValueType.GetHashCode"/> to get the hash code this <see cref="StorageLocation"/>.
		/// </summary>
		/// <returns>The hash code</returns>
		public override int GetHashCode()
		{
			return HashCode.Combine(this.location, this.deviceID);
		}

		/// <summary>
		/// Equality operator
		/// </summary>
		public static bool operator ==(StorageLocation left, StorageLocation right)
		{
			return left.Equals(right);
		}

		/// <summary>
		/// Inequality operator
		/// </summary>
		public static bool operator !=(StorageLocation left, StorageLocation right)
		{
			return !(left == right);
		}
		#endregion

		#region string related
		private static readonly Dictionary<LocationEnum, string> static_OtherMemoryNames = new Dictionary<LocationEnum, string>();

		/// <summary>
		/// Set the name used for <see cref="ToString"/> of this if it represents a storage position in other memory types like <see cref="LocationEnum.OtherMemory_0"/>
		/// </summary>
		/// <param name="location">the <see cref="LocationEnum"/> of a storage position in other memory types</param>
		/// <param name="name">the name as a <see cref="string"/> to set; notice that all the spaces will be replaced by '_'</param>
		/// <returns>success or not</returns>
		public static bool SetOtherMemoryName(LocationEnum location, string name)
		{
			if (location < LocationEnum.OtherMemory_0)
				return false;
			static_OtherMemoryNames[location] = name.Replace(' ', '_');
			return true;
		}

		/// <summary>
		/// Return the string representation of this <see cref="StorageLocation"/>
		/// </summary>
		/// <returns>the string representation of this <see cref="StorageLocation"/></returns>
		public override string ToString()
		{
			return this.location switch
			{
				LocationEnum.CpuMemory => "CPU_Memory",
				LocationEnum.GpuMemory => "GPU_Memory",
				_ => static_OtherMemoryNames.GetValueOrDefault(this.location) ?? $"Other_Device_Memory_Order_{this.OrderOfOtherMemoryType()}",
			} + $"(ID={this.deviceID})";
		}
		#endregion
	}

	/// <summary>
	/// The struct of a pointer at a certain unmanaged memory block
	/// </summary>
	/// <remarks>This struct <b>does not</b> respond for releasing unmanaged memories. It is only used for storing information of memory blocks.</remarks>
	[StructLayout(LayoutKind.Sequential)]
	public readonly struct StoragePointer : IEquatable<StoragePointer>
	{
		#region basic
		private readonly StorageLocation location;

		private readonly IntPtr pointer;

		private readonly ulong length;

		/// <summary>
		/// The <see cref="StorageLocation"/> of this <see cref="StoragePointer"/>
		/// </summary>
		public StorageLocation Location => location;

		/// <summary>
		/// The raw pointer of this <see cref="StoragePointer"/>
		/// </summary>
		public IntPtr Pointer => pointer;

		/// <summary>
		/// The <b>unchecked</b> length of this <see cref="StoragePointer"/>
		/// </summary>
		public ulong Length => length;

		/// <summary>
		/// Create with given location, pointer and length
		/// </summary>
		/// <param name="location">The location of this <see cref="StorageLocation"/></param>
		/// <param name="pointer">The pointer at the given <paramref name="location"/></param>
		/// <param name="length">The length of the given <paramref name="pointer"/></param>
		public StoragePointer(StorageLocation location, IntPtr pointer, ulong length)
		{
			this.location = location; this.pointer = pointer; this.length = length;
		}

		/// <summary>
		/// Create with given location and pointer
		/// </summary>
		/// <param name="location">The location of this <see cref="StorageLocation"/></param>
		/// <param name="pointer">The pointer at the given <paramref name="location"/></param>
		/// <param name="length">The length of the given <paramref name="pointer"/></param>
		public unsafe StoragePointer(StorageLocation location, void* pointer, ulong length) : this(location, new IntPtr(pointer), length) { }

		/// <summary>
		/// Create with given <see cref="StoragePointer"/> <paramref name="storage"/> and <paramref name="offset"/> to the <paramref name="storage"/>'s <see cref="Pointer"/>
		/// </summary>
		/// <param name="storage">The <see cref="StoragePointer"/> to copy info from</param>
		/// <param name="offset">The offset to the <paramref name="storage"/>'s <see cref="Pointer"/></param>
		public StoragePointer(StoragePointer storage, long offset) : this(storage.location, new IntPtr(storage.pointer.ToInt64() + offset), offset >= 0 ? storage.length - (ulong)offset : storage.length + ((ulong)-offset)) { }

		/// <summary>
		/// Create with given <see cref="StoragePointer"/> <paramref name="storage"/> and new <see cref="Length"/>
		/// </summary>
		/// <param name="storage">The <see cref="StoragePointer"/> to copy info from</param>
		/// <param name="newLength">The new <see cref="Length"/></param>
		public StoragePointer(StoragePointer storage, ulong newLength) : this(storage.location, storage.pointer, newLength) { }
		#endregion

		#region equality
		/// <summary>
		/// Whether this == <paramref name="other"/>
		/// </summary>
		/// <param name="other">another <see cref="StoragePointer"/> to compare</param>
		/// <returns>this == <paramref name="other"/></returns>
		public bool Equals(StoragePointer other)
		{
			return this.location == other.location && this.pointer == other.pointer;
		}

		/// <summary>
		/// Override <see cref="ValueType.Equals(object?)"/> to check whether this == <paramref name="obj"/>
		/// </summary>
		/// <param name="obj">another object to compare</param>
		/// <returns>this == <paramref name="obj"/></returns>
		public override bool Equals(object obj)
		{
			return obj is StoragePointer storage1 && Equals(storage1);
		}

		/// <summary>
		/// Override <see cref="ValueType.GetHashCode"/> to get the hash code this <see cref="StoragePointer"/>.
		/// </summary>
		/// <returns>The hash code</returns>
		public override int GetHashCode()
		{
			return HashCode.Combine(this.location, this.pointer);
		}

		/// <summary>
		/// Equality operator
		/// </summary>
		public static bool operator ==(StoragePointer left, StoragePointer right)
		{
			return left.Equals(right);
		}

		/// <summary>
		/// Inequality operator
		/// </summary>
		public static bool operator !=(StoragePointer left, StoragePointer right)
		{
			return !(left == right);
		}
		#endregion

		#region to string
		/// <summary>
		/// Return the string representation of this <see cref="StoragePointer"/>
		/// </summary>
		/// <returns>the string representation of this <see cref="StoragePointer"/></returns>
		public override string ToString()
		{
			return $"0x{this.pointer:X} on {this.location}";
		}
		#endregion

		#region operator
		/// <summary>
		/// Get the managed pointer (a <c>ref <typeparamref name="T"/></c>) of this <see cref="StoragePointer"/> of type <typeparamref name="T"/>
		/// </summary>
		/// <typeparam name="T">the data type</typeparam>
		/// <returns>the managed pointer (a <c>ref <typeparamref name="T"/></c>) of this <see cref="StoragePointer"/></returns>
		public unsafe ref T AsManagedPointer<T>() where T : unmanaged => ref Unsafe.AsRef<T>(this.pointer.ToPointer());

		/// <summary>
		/// Get the <see cref="Span{T}"/> representation of this <see cref="StoragePointer"/> of type <typeparamref name="T"/>
		/// </summary>
		/// <typeparam name="T">the data type</typeparam>
		/// <returns>the <see cref="Span{T}"/> representation of this <see cref="StoragePointer"/></returns>
		public unsafe Span<T> AsSpan<T>() where T : unmanaged => new Span<T>(this.pointer.ToPointer(), checked((int)this.length));

		/// <summary>
		/// Implicit convert <see cref="StoragePointer"/> to <see cref="IntPtr"/>
		/// </summary>
		/// <param name="storage">the <see cref="StoragePointer"/> to be converted</param>
		/// <returns>The <see cref="IntPtr"/> of the start memory position of <paramref name="storage"/></returns>
		public static implicit operator IntPtr(StoragePointer storage) => storage.pointer;

		/// <summary>
		/// Add offset (in bytes) to a <see cref="StoragePointer"/> to get another.
		/// </summary>
		/// <param name="storage">the <see cref="StoragePointer"/></param>
		/// <param name="offset">the offset of type <see cref="long"/></param>
		/// <returns>a <see cref="Storage{T}"/> with <paramref name="offset"/> added to the pointer</returns>
		public static StoragePointer operator +(StoragePointer storage, long offset) => new StoragePointer(storage, offset);

		/// <summary>
		/// Subtract offset (in bytes) to a <see cref="StoragePointer"/> to get another.
		/// </summary>
		/// <param name="storage">the <see cref="StoragePointer"/></param>
		/// <param name="offset">the offset of type <see cref="long"/></param>
		/// <returns>a <see cref="Storage{T}"/> with <paramref name="offset"/> added to the pointer</returns>
		public static StoragePointer operator -(StoragePointer storage, long offset) => new StoragePointer(storage, -offset);

		/// <summary>
		/// Get the pointer's difference (in bytes) of two <see cref="StoragePointer"/>s.
		/// </summary>
		/// <param name="left">the left <see cref="StoragePointer"/></param>
		/// <param name="right">the right <see cref="StoragePointer"/></param>
		/// <returns>If <paramref name="left"/> and <paramref name="right"/> have different <see cref="Location"/>s, return <see cref="long.MinValue"/>; otherwise, return a <see cref="long"/> as the difference between the <see cref="Pointer"/>s of <paramref name="left"/> and <paramref name="right"/></returns>
		public static long operator -(StoragePointer left, StoragePointer right) => left.location != right.location ? long.MinValue : left.pointer.ToInt64() - right.pointer.ToInt64();
		#endregion
	}
	#endregion



	/// <summary>
	/// The static class that contains several extension methods for <see cref="StorageLocation"/>
	/// </summary>
	public static class StorageExtension
	{
		// TODO: edit CheckOnHost
		internal static bool CheckOnHost<T>(params Arrays.ValueArray<T>[] arrays) where T : unmanaged, IFormattable, IEquatable<T>
		{
			if (arrays is null || arrays.Length == 0)
				throw new ArgumentNullException(nameof(arrays));
			if (arrays.Any(a => a.Disposed))
				throw new ObjectDisposedException(nameof(arrays));
			if (arrays.All(a => a.Length == 0 || !a.OnHost)) // empty array can be any where
				return false;
			if (arrays.All(a => a.Length == 0 || a.OnHost))
				return true;
			// else
			throw new ArgumentException(Resource.RequireSamePos);
		}

		internal static bool CheckOnHost<T>(params Memory.Storage<T>[] arrays) where T : unmanaged, IFormattable, IEquatable<T>
		{
			if (arrays is null || arrays.Length == 0)
				throw new ArgumentNullException(nameof(arrays));
			////if (arrays.Any(a => a.AlreadyDisposed))
			////	throw new ObjectDisposedException(nameof(arrays));
			if (arrays.All(a => !a.OnHost))
				return false;
			if (arrays.All(a => a.OnHost))
				return true;
			// else
			throw new ArgumentException(Resource.RequireSamePos);
		}

	}


	#region base storage class
	/// <summary>
	/// The abstract wrapper class of unmanaged memory block(s) of different <see cref="StorageLocation"/>(s).
	/// </summary>
	/// <typeparam name="T">any unmanaged data type</typeparam>
	/// <remarks>I must warn you that although C# has GC to periodically collect unused garbage to prevent memory leak, you should not rely on it too much. <b>Remember</b> to use <c>using</c> statement or call <see cref="Storage{T}.Dispose()"/>.<br/>
	/// The leaked memory which will be collected GC still causes not only performance loss but also potential bugs if you do not know how GC works, since the concrete class(es) shall be a class with finalizers thus cannot be in GC generation 0, i.e. it will not be immediately disposed when out-of-scope.<br/>
	/// See https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/ for official documentations of GC of dot NET.</remarks>
	public abstract class Storage<T> : IDisposable, IEquatable<Storage<T>>, IReadOnlyList<StoragePointer> where T : unmanaged
	{
		#region properties
		/// <summary>
		/// Get the size of <typeparamref name="T"/> in memory in bytes
		/// </summary>
		public static unsafe int SizeOfT => sizeof(T);

		/// <summary>
		/// The total length of the presenting array in <typeparamref name="T"/> (rather than bytes)
		/// </summary>
		public virtual ulong Length => this.Sum(s => s.Length) / (ulong)SizeOfT;

		/// <summary>
		/// The number of <see cref="StoragePointer"/>(s) of this <see cref="Storage{T}"/> 
		/// </summary>
		public abstract int Count { get; }

		/// <summary>
		/// Indexer of the <see cref="StoragePointer"/>(s) of this <see cref="Storage{T}"/> (in presenting order)
		/// </summary>
		/// <param name="index">the element index</param>
		/// <returns>the <see cref="StoragePointer"/> at <paramref name="index"/></returns>
		public abstract StoragePointer this[int index] { get; }
		#endregion

		#region dispose
		/// <summary>
		/// Is this <see cref="Storage{T}"/> disposed or not
		/// </summary>
		public bool Disposed { get; private set; } = false;

		/// <summary>
		/// Dispose this storage
		/// </summary>
		public void Dispose()
		{
			this.Dispose(true);
			this.Disposed = true;
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// The function that actually dispose this storage
		/// </summary>
		/// <param name="disposeManaged">dispose managed resources or not</param>
		protected abstract void Dispose(bool disposeManaged);
		#endregion

		#region enumerator
		IEnumerator<StoragePointer> IEnumerable<StoragePointer>.GetEnumerator()
		{
			for (int i = 0; i < this.Count; i++)
			{
				yield return this[i];
			}
		}
		IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<StoragePointer>)this).GetEnumerator();
		#endregion

		#region other methods
		/// <summary>
		/// Make a referenced <see cref="Storage{T}"/> with the same pointer as this one while <see cref="Length"/> is changed to <paramref name="newLength"/>
		/// </summary>
		/// <param name="newLength">the new length of referenced <see cref="Storage{T}"/></param>
		/// <returns>a referenced <see cref="Storage{T}"/> with different <see cref="Length"/></returns>
		public Storage<T> MakeReferenceOfSize(ulong newLength)
		{
			if (newLength == this.Length)
				return this;
			return new ReferenceStorage<T>(this, newLength: newLength);
		}

		/// <summary>
		/// Convert this <see cref="Storage{T}"/> to another one with different data type <typeparamref name="TOut"/>
		/// </summary>
		/// <typeparam name="TOut">the output data type</typeparam>
		/// <returns>a referenced <see cref="Storage{TOut}"/></returns>
		public abstract Storage<TOut> As<TOut>() where TOut : unmanaged;
		#endregion

		#region equality
		/// <summary>
		/// Determines whether the specified object is equal to the current object.
		/// </summary>
		/// <param name="obj">another object</param>
		/// <returns>this equals to <paramref name="obj"/> or not</returns>
		public override bool Equals(object obj)
		{
			return this.Equals(obj as Storage<T>);
		}

		/// <summary>
		/// Determines whether the specified object is equal to the current object.
		/// </summary>
		/// <param name="obj">another object</param>
		/// <returns>this equals to <paramref name="obj"/> or not</returns>
		public abstract bool Equals(Storage<T> obj);

		/// <summary>
		/// Get the hash code of this <see cref="Storage{T}"/>
		/// </summary>
		/// <returns>the hash code</returns>
		public abstract override int GetHashCode();

		/// <summary>
		/// Equality operator
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns>equals or not</returns>
		public static bool operator ==(Storage<T> left, Storage<T> right)
		{
			if (left is null && right is null)
				return true;
			if ((left is null) != (right is null))
				return false;
			return left.Equals(right);
		}

		/// <summary>
		/// Non-equality operator
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns>not-equals or equals</returns>
		public static bool operator !=(Storage<T> left, Storage<T> right) => !(left == right);
		#endregion

		#region string
		// Ignore Spelling: typeof
		/// <summary>
		/// Override <see cref="object.ToString"/> to get the string representation.
		/// </summary>
		/// <returns>string representation</returns>
		public override string ToString()
		{
			return $"{{{string.Join(", ", this)}}} [type={typeof(T).Name}, total_length={this.Length}]";
		}
		#endregion

		#region operator
		/// <summary>
		/// Add offset (in bytes) to a <see cref="Storage{T}"/> to get another.
		/// </summary>
		/// <param name="storage">the <see cref="Storage{T}"/></param>
		/// <param name="offset">the offset of type <see cref="long"/></param>
		/// <returns>a <see cref="Storage{T}"/> with <paramref name="offset"/> added to the pointer</returns>
		public static Storage<T> operator +(Storage<T> storage, long offset) => new ReferenceStorage<T>(storage, offset);

		/// <summary>
		/// Subtract offset (in bytes) to a <see cref="Storage{T}"/> to get another.
		/// </summary>
		/// <param name="storage">the <see cref="Storage{T}"/></param>
		/// <param name="offset">the offset of type <see cref="long"/></param>
		/// <returns>a <see cref="Storage{T}"/> with <paramref name="offset"/> added to the pointer</returns>
		public static Storage<T> operator -(Storage<T> storage, long offset) => new ReferenceStorage<T>(storage, -offset);
		#endregion
	}

	/// <summary>
	/// The storage class that reference to a <see cref="Storage{T}"/>
	/// </summary>
	/// <typeparam name="T">any unmanaged data type</typeparam>
	public sealed class ReferenceStorage<T> : Storage<T> where T : unmanaged
	{
		#region basic
		private readonly Storage<T> reference;

		private readonly long totalOffset;

		private readonly int start;

		private readonly ulong startOffsetBytes;
		private readonly ulong endLengthBytes;

		/// <summary>
		/// Override <see cref="Storage{T}.Length"/> to show the new presenting length
		/// </summary>
		public override ulong Length { get; }

		/// <summary>
		/// Create a <see cref="ReferenceStorage{T}"/> with given reference <paramref name="storage"/> and <paramref name="offset"/> to it
		/// </summary>
		/// <param name="storage">the <see cref="Storage{T}"/> to be referenced</param>
		/// <param name="offset">the offset as a <see cref="long"/></param>
		/// <param name="newLength">the new presenting length, default 0 means automatically calculate by <paramref name="storage"/> and <paramref name="offset"/></param>
		public ReferenceStorage(Storage<T> storage, long offset = 0, ulong newLength = 0)
		{
			// dereference first
			while (storage is ReferenceStorage<T> @ref)
			{
				storage = @ref.reference;
				offset += @ref.totalOffset;
			}
			// check
			if (offset < 0)
				throw new ArgumentOutOfRangeException(nameof(offset));
			if (newLength == 0)
				newLength = storage.Length - (ulong)offset;
			else if (storage.Length != (ulong)offset + newLength)
				throw new ArgumentOutOfRangeException(nameof(offset));
			// set offsets
			ulong totalOffset = (ulong)offset;
			bool setStart = true;
			for (int i = 0; i < storage.Count; i++)
			{
				ulong lengthOfI = storage[i].Length / (ulong)SizeOfT;
				if (totalOffset < lengthOfI)
				{
					if (setStart)
					{
						this.start = i; this.startOffsetBytes = totalOffset * (ulong)SizeOfT;
						totalOffset += newLength;
					}
					else
					{
						this.endLengthBytes = totalOffset * (ulong)SizeOfT;
						break;
					}
				}
				else
				{
					totalOffset -= lengthOfI;
				}
			}
		}

		private ReferenceStorage(Storage<T> storage, long totalOffset, int start, ulong startOffset, ulong endLength)
		{
			this.reference = storage; this.totalOffset = totalOffset; this.start = start; this.startOffsetBytes = startOffset; this.endLengthBytes = endLength;
		}
		#endregion

		#region override
		/// <summary>
		/// The function that actually dispose this storage, override <see cref="Storage{T}.Dispose(bool)"/>
		/// </summary>
		/// <param name="disposeManaged">dispose managed resources or not</param>
		protected override void Dispose(bool disposeManaged)
		{
			// since this is a reference, we shall do nothing
		}

		/// <summary>
		/// Indexer of the <see cref="StoragePointer"/>(s) of this <see cref="Storage{T}"/> (in presenting order)
		/// </summary>
		/// <param name="index">the element index</param>
		/// <returns>the <see cref="StoragePointer"/> at <paramref name="index"/></returns>
		public override StoragePointer this[int index] {
			get {
				if (index < 0 || index >= this.Count)
					throw new ArgumentOutOfRangeException(nameof(index));
				StoragePointer pointer = this.reference[index - start];
				if (index == 0)
				{
					pointer = new StoragePointer(pointer, offset: (long)this.startOffsetBytes);
				}
				else if (index == Count - 1)
				{
					pointer = new StoragePointer(pointer, newLength: this.endLengthBytes);
				}
				return pointer;
			}
		}

		/// <summary>
		/// The number of <see cref="StoragePointer"/>(s) of this <see cref="Storage{T}"/> 
		/// </summary>
		public override int Count => this.reference.Count - this.start;

		/// <summary>
		/// Convert this <see cref="ReferenceStorage{T}"/> to another one with different data type <typeparamref name="TOut"/>
		/// </summary>
		/// <typeparam name="TOut">the output data type</typeparam>
		/// <returns>a referenced <see cref="ReferenceStorage{TOut}"/></returns>
		public override ReferenceStorage<TOut> As<TOut>()
		{
			long offset = this.totalOffset * SizeOfT;
			if (offset % Storage<TOut>.SizeOfT != 0)
				throw new InvalidCastException();
			offset /= Storage<TOut>.SizeOfT;
			return new ReferenceStorage<TOut>(this.reference.As<TOut>(), offset, this.start, this.startOffsetBytes, this.endLengthBytes);
		}

		/// <summary>
		/// Determines whether the specified object is equal to the current object.
		/// </summary>
		/// <param name="obj">another object</param>
		/// <returns>this equals to <paramref name="obj"/> or not</returns>
		public override bool Equals(Storage<T> obj)
		{
			if (obj is not null && obj is ReferenceStorage<T> @ref)
			{
				return this.reference == @ref.reference && this.start == @ref.start && this.startOffsetBytes == @ref.startOffsetBytes && this.endLengthBytes == @ref.endLengthBytes;
			}
			return false;
		}

		/// <summary>
		/// Get the hash code of this <see cref="ReferenceStorage{T}"/>
		/// </summary>
		/// <returns>the hash code</returns>
		public override int GetHashCode() => HashCode.Combine(this.reference, this.start, this.startOffsetBytes, this.endLengthBytes);
		#endregion
	}
	#endregion


	#region actual storage class

	#endregion


	/// <summary>
	/// Some extension methods of list of arrays
	/// </summary>
	public static class ListOfArraysExtension
	{
		/// <summary>
		/// Clear a general array
		/// </summary>
		/// <typeparam name="TArr">the array type</typeparam>
		/// <param name="array">the array to clear</param>
		public static void ClearList<TArr>(this TArr[] array) where TArr : IDisposable
		{
			if (array is null)
				return;
			array.ForEach(l => l?.Dispose());
			Array.Clear(array, 0, array.Length);
		}

		/// <summary>
		/// Clear a general list
		/// </summary>
		/// <typeparam name="TArr">the array type</typeparam>
		/// <param name="list">the list to clear</param>
		public static void ClearList<TArr>(this List<TArr> list) where TArr : IDisposable
		{
			if (list is null)
				return;
			list.ForEach(l => l?.Dispose());
			list.Clear();
		}

		/// <summary>
		/// Dispose a general read-only list
		/// </summary>
		/// <typeparam name="TArr">the array type</typeparam>
		/// <param name="list">the read-only list to dispose</param>
		public static void ClearList<TArr>(this IReadOnlyList<TArr> list) where TArr : IDisposable
		{
			if (list is null)
				return;
			for (int i = 0; i < list.Count; i++)
			{
				list[i]?.Dispose();
			}
		}

		/// <summary>
		/// Dispose a general dictionary
		/// </summary>
		/// <typeparam name="T">the dictionary key type</typeparam>
		/// <typeparam name="TArr">the array type</typeparam>
		/// <param name="dict">the dictionary to dispose</param>
		public static void ClearDict<T, TArr>(this IReadOnlyDictionary<T, TArr> dict) where TArr : IDisposable
		{
			if (dict is null)
				return;
			foreach (var item in dict)
			{
				item.Value?.Dispose();
			}
		}
	}

	internal static class AutoSwapMemory
	{
		internal static readonly Stopwatch timer = Stopwatch.StartNew();

		private static readonly List<ISwappablePointer> devicePointers = new List<ISwappablePointer>(), swappedPointers = new List<ISwappablePointer>();

		internal static void NotifyNewStorage(ISwappablePointer pointer)
		{
			if (!pointer.DirectOnHost)
				devicePointers.Add(pointer);
		}

		internal static void NotifyDisposeStorage(ISwappablePointer pointer)
		{
			if (!pointer.DirectOnHost)
				devicePointers.Remove(pointer);
			else
				swappedPointers.Remove(pointer);
		}

		internal static void NotifyUsage(ISwappablePointer pointer)
		{
			if (pointer is null)
				return;
			pointer.LastUsedTime = timer.ElapsedTicks;
			if (pointer.DirectOnHost && swappedPointers.Contains(pointer))
			{
				bool success = NotifyNewDeviceMemory(pointer.LengthInBytes);
				if (!success)
				{
					GC.Collect(); GC.WaitForPendingFinalizers();
					success = NotifyNewDeviceMemory(pointer.LengthInBytes);
				}
				if (!success) // still fails to make enough room
					throw new InsufficientMemoryException($"Cannot swap {(pointer.LengthInBytes / 1024.0 / 1024.0):N1}MiB memory back to device.");
				pointer.ToOtherMemory();
			}
		}

		internal static bool NotifyNewDeviceMemory(long lenghtInBytes)
		{
			var (free, total) = RT.DeviceFreeAndTotalMemory;
			if (free + lenghtInBytes < total)
				return true;
			long lengthNeed = lenghtInBytes - free;
			var ordered = devicePointers.OrderBy(p => p.LastUsedTime);
			long len = 0; int i;
			for (i = 0; i < ordered.Count; i++)
			{
				len += ordered[i].LengthInBytes;
				if (len >= lengthNeed)
					break;
			}
			if (i + 1 == ordered.Count)
				return false;
			for (int j = 0; j <= i; j++)
			{
				ordered[j].ToOtherMemory();
				swappedPointers.Add(ordered[j]);
				devicePointers.Remove(ordered[j]);
			}
			return true;
		}
	}
}
