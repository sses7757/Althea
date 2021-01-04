using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Althea.Linq;


namespace Althea.Memory
{
	#region storage location
	/// <summary>
	/// The enum of the memory location, all values larger than <see cref="OtherRam_0"/> are all considered as some platform-specific local RAMs.
	/// </summary>
	public enum MemoryLocation : byte
	{
		/// <summary>
		/// Represents a storage location represented by Universal Resource Identifier.
		/// </summary>
		/// <remarks>It has different logic than other memory based <see cref="MemoryLocation"/>s.</remarks>
		Uri = 0,
		/// <summary>
		/// Storage at local CPU RAM
		/// </summary>
		CpuRam = 1,
		/// <summary>
		/// Storage at local GPU RAM
		/// </summary>
		GpuRam = 2,
		/// <summary>
		/// Storage at platform-specific local RAM (with custom order the 1st) other than <see cref="CpuRam"/> and <see cref="GpuRam"/>. For example, a RAM associated with a FPGA.
		/// </summary>
		OtherRam_0 = 3,
		/// <summary>
		/// Storage at platform-specific local RAM (with custom order the 2nd) other than <see cref="CpuRam"/> and <see cref="GpuRam"/>. For example, a RAM associated with a FPGA.
		/// </summary>
		OtherRam_1 = 4,
		/// <summary>
		/// Storage at platform-specific local RAM (with custom order the 3rd) other than <see cref="CpuRam"/> and <see cref="GpuRam"/>. For example, a RAM associated with a FPGA.
		/// </summary>
		OtherRam_2 = 5,
	}

	/// <summary>
	/// The enum representing the URI schemes which can be used as memories.
	/// </summary>
	/// <remarks>See <see cref="Uri.UriSchemeFile"/>, etc.</remarks>
	public enum UriScheme : byte
	{
		/// <summary>
		/// Specifies that the URI is a pointer to a file
		/// </summary>
		File,
		/// <summary>
		/// Specifies that the URI is accessed through the File Transfer Protocol (FTP).
		/// </summary>
		FTP,
		/// <summary>
		/// Specifies that the URI is accessed through the Gopher protocol.
		/// </summary>
		Gopher,
		/// <summary>
		/// Specifies that the URI is accessed through the Hypertext Transfer Protocol (HTTP).
		/// </summary>
		HTTP,
		/// <summary>
		/// Specifies that the URI is accessed through the Secure Hypertext Transfer Protocol (HTTPS).
		/// </summary>
		HTTPS,
		/// <summary>
		/// Specifies that the URI is accessed through the NetPipe scheme used by Windows Communication Foundation (WCF). 
		/// </summary>
		NetPipe,
		/// <summary>
		/// Specifies that the URI is accessed through the NetTcp scheme used by Windows Communication Foundation (WCF).
		/// </summary>
		NetTcp,
	}

	/// <summary>
	/// The struct of a storage location
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public readonly struct StorageLocation : IEquatable<StorageLocation>
	{
		#region basic
		private readonly MemoryLocation location;

		private readonly byte locationDetail;

		/// <summary>
		/// The location of this <see cref="StorageLocation"/>
		/// </summary>
		public MemoryLocation Location => location;

		/// <summary>
		/// The detail information of the <see cref="Location"/>. <see cref="LocationDetail"/> shall be the device ID if the <see cref="Location"/> is a RAM, or the <see cref="UriScheme"/> if it is a <see cref="MemoryLocation.Uri"/>.
		/// </summary>
		public byte LocationDetail => locationDetail;

		/// <summary>
		/// Create with given location and device ID
		/// </summary>
		/// <param name="location">The location of this <see cref="StorageLocation"/></param>
		/// <param name="locationDetail">The detail information of the given <paramref name="location"/>. <paramref name="locationDetail"/> shall be the device ID if the given <paramref name="location"/> is a RAM, or the <see cref="UriScheme"/> if it is a <see cref="MemoryLocation.Uri"/>.</param>
		public StorageLocation(MemoryLocation location, byte locationDetail)
		{
			this.location = location; this.locationDetail = locationDetail;
		}

		/// <summary>
		/// Get the order for this <see cref="StorageLocation"/>'s <see cref="MemoryLocation"/> if it represents a storage position in other memory types like <see cref="MemoryLocation.OtherRam_0"/>
		/// </summary>
		/// <returns>0 if this is not a memory of other types, otherwise the order for this <see cref="StorageLocation"/>'s <see cref="MemoryLocation"/></returns>
		public byte OrderOfOtherMemoryType()
		{
			if (this.location < MemoryLocation.OtherRam_0)
				return 0;
			return this.location - MemoryLocation.OtherRam_0;
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
			return this.location == other.location && this.locationDetail == other.locationDetail;
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
			return HashCode.Combine(this.location, this.locationDetail);
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
		private static readonly Dictionary<MemoryLocation, string> static_OtherMemoryNames = new Dictionary<MemoryLocation, string>();

		/// <summary>
		/// Set the name used for <see cref="ToString"/> of this if it represents a storage position in other memory types like <see cref="MemoryLocation.OtherRam_0"/>
		/// </summary>
		/// <param name="location">the <see cref="MemoryLocation"/> of a storage position in other memory types</param>
		/// <param name="name">the name as a <see cref="string"/> to set; notice that all the spaces will be replaced by '_'</param>
		/// <returns>success or not</returns>
		public static bool SetOtherMemoryName(MemoryLocation location, string name)
		{
			if (location < MemoryLocation.OtherRam_0)
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
				MemoryLocation.Uri => "URI_" + (UriScheme)this.locationDetail,
				MemoryLocation.CpuRam => "CPU_Memory",
				MemoryLocation.GpuRam => "GPU_Memory",
				_ => static_OtherMemoryNames.GetValueOrDefault(this.location) ?? $"Other_Device_Memory_Order_{this.OrderOfOtherMemoryType()}",
			} + $"(ID={this.locationDetail})";
		}
		#endregion
	}

	/// <summary>
	/// The struct of a pointer at a certain unmanaged memory block
	/// </summary>
	/// <remarks>This struct <b>is not</b> responsible for releasing unmanaged memories. It is only used for storing information of memory blocks.</remarks>
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
		/// The <b>unchecked</b> length of this <see cref="StoragePointer"/> in bytes
		/// </summary>
		public ulong LengthInBytes => length;

		/// <summary>
		/// Create with given location, pointer and length
		/// </summary>
		/// <param name="location">The location of this <see cref="StorageLocation"/></param>
		/// <param name="pointer">The pointer at the given <paramref name="location"/></param>
		/// <param name="length">The length in bytes of the given <paramref name="pointer"/></param>
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
		/// Create with given <see cref="StoragePointer"/> <paramref name="storage"/> and new <see cref="LengthInBytes"/>
		/// </summary>
		/// <param name="storage">The <see cref="StoragePointer"/> to copy info from</param>
		/// <param name="newLength">The new <see cref="LengthInBytes"/></param>
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


	#region storage classes
	/// <summary>
	/// The interface for wrapper of unmanaged memory block(s) of different <see cref="StorageLocation"/>(s) of any data type
	/// </summary>
	public interface IStorage : IDisposable, IReadOnlyList<StoragePointer>
	{
		/// <summary>
		/// The total length of the presenting array in  bytes
		/// </summary>
		public virtual ulong LengthInBytes => this.Sum(s => s.LengthInBytes);
	}

	/// <summary>
	/// The abstract wrapper class of unmanaged memory block(s) of different <see cref="StorageLocation"/>(s).
	/// </summary>
	/// <typeparam name="T">any unmanaged data type</typeparam>
	/// <remarks>I must warn you that although C# has GC to periodically collect unused garbage to prevent memory leak, you should not rely on it too much. <b>Remember</b> to use <c>using</c> statement or call <see cref="Storage{T}.Dispose()"/>.<br/>
	/// The leaked memory which will be collected GC still causes not only performance loss but also potential bugs if you do not know how GC works, since the concrete class(es) shall be a class with finalizers thus cannot be in GC generation 0, i.e. it will not be immediately disposed when out-of-scope.<br/>
	/// See https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/ for official documentations of GC of dot NET.</remarks>
	public abstract class Storage<T> : IStorage, IEquatable<Storage<T>> where T : unmanaged
	{
		#region properties
		/// <summary>
		/// Get the size of <typeparamref name="T"/> in memory in bytes
		/// </summary>
		public static unsafe int SizeOfT => sizeof(T);

		/// <summary>
		/// The total length of the presenting array in <typeparamref name="T"/> (rather than bytes)
		/// </summary>
		public virtual ulong Length => ((IStorage)this).LengthInBytes / (ulong)SizeOfT;

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
		protected bool Disposed { get; private set; } = false;

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
		/// <returns>if <paramref name="newLength"/> == this.<see cref="Length"/>, return this; otherwise, return a <see cref="ReferenceStorage{T}"/> with <paramref name="newLength"/></returns>
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
		/// <exception cref="InvalidCastException">if <see cref="IStorage.LengthInBytes"/> cannot be divided by <see cref="Storage{TOut}.SizeOfT"/></exception>
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
			if (this.Count == 1)
			{
				return $"{this[0]} [type={typeof(T).Name}, length={this.Length}]";
			}
			else
			{
				return $"{{{string.Join(", ", this)}}} [type={typeof(T).Name}, total_length={this.Length}]";
			}
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
		private readonly IStorage reference;

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
		/// <param name="offset">the offset in <typeparamref name="T"/> as a <see cref="long"/></param>
		/// <param name="newLength">the new presenting length in <typeparamref name="T"/>, default 0 means automatically calculate by <paramref name="storage"/> and <paramref name="offset"/></param>
		public ReferenceStorage(IStorage storage, long offset = 0, ulong newLength = 0)
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
				newLength = storage.LengthInBytes - (ulong)(SizeOfT * offset);
			else if (storage.LengthInBytes != (ulong)offset + newLength)
				throw new ArgumentOutOfRangeException(nameof(offset));
			// set offsets
			ulong totalOffset = (ulong)offset;
			bool setStart = true;
			for (int i = 0; i < storage.Count; i++)
			{
				ulong lengthOfI = storage[i].LengthInBytes / (ulong)SizeOfT;
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

		private ReferenceStorage(IStorage storage, long totalOffset, int start, ulong startOffset, ulong endLength)
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
		/// <exception cref="InvalidCastException">if <see cref="IStorage.LengthInBytes"/> cannot be divided by <see cref="Storage{TOut}.SizeOfT"/></exception>
		public override ReferenceStorage<TOut> As<TOut>()
		{
			long offset = this.totalOffset * SizeOfT;
			if (offset % Storage<TOut>.SizeOfT != 0)
				throw new InvalidCastException();
			offset /= Storage<TOut>.SizeOfT;
			return new ReferenceStorage<TOut>(this.reference, offset, this.start, this.startOffsetBytes, this.endLengthBytes);
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

	/// <summary>
	/// The abstract storage class as a base class for all non-referenced <see cref="Storage{T}"/> classes
	/// </summary>
	/// <typeparam name="T">any unmanaged data type</typeparam>
	public abstract class ActualStorage<T> : Storage<T> where T : unmanaged
	{
		#region memory
		/// <summary>
		/// The finalizer of <see cref="ActualStorage{T}"/>
		/// </summary>
		~ActualStorage()
		{
			this.Dispose(false);
		}

		/// <summary>
		/// The function that actually dispose this storage, override <see cref="Storage{T}.Dispose(bool)"/>
		/// </summary>
		/// <param name="disposeManaged">dispose managed resources or not</param>
		protected override void Dispose(bool disposeManaged)
		{
			for (int i = 0; i < this.Count; i++)
			{
				var ptr = this[i];
				// TODO: adapter dispose
			}
		}

		/// <summary>
		/// Allocate a <see cref="StoragePointer"/> of given <see cref="Storage{T}.Length"/> on given <see cref="StorageLocation"/> 
		/// </summary>
		/// <param name="location">a <see cref="StorageLocation"/> to represent the memory location</param>
		/// <param name="length">the length of contiguous memory block in <typeparamref name="T"/></param>
		protected static StoragePointer Allocate(StorageLocation location, ulong length)
		{
			IntPtr ptr = default;
			// TODO: adapter create
			return new StoragePointer(location, ptr, length);
		}
		#endregion

		#region override
		/// <summary>
		/// Convert this <see cref="ActualStorage{T}"/> to another one with different data type <typeparamref name="TOut"/>
		/// </summary>
		/// <typeparam name="TOut">the output data type</typeparam>
		/// <returns>a <see cref="ReferenceStorage{TOut}"/></returns>
		/// <exception cref="InvalidCastException">if <see cref="IStorage.LengthInBytes"/> cannot be divided by <see cref="Storage{TOut}.SizeOfT"/></exception>
		public override ReferenceStorage<TOut> As<TOut>()
		{
			if (((IStorage)this).LengthInBytes % (ulong)Storage<TOut>.SizeOfT != 0)
				throw new InvalidCastException();
			ulong newLength = ((IStorage)this).LengthInBytes / (ulong)Storage<TOut>.SizeOfT;
			return new ReferenceStorage<TOut>(this, newLength: newLength);
		}

		/// <summary>
		/// Determines whether the specified object is equal to the current object.
		/// </summary>
		/// <param name="obj">another object</param>
		/// <returns>this equals to <paramref name="obj"/> or not</returns>
		public override bool Equals(Storage<T> obj)
		{
			if (obj is not null && obj is ActualStorage<T> another)
			{
				if (this.Count != another.Count)
					return false;
				for (int i = 0; i < this.Count; i++)
				{
					if (this[i] != another[i])
						return false;
				}
				return true;
			}
			return false;
		}

		/// <summary>
		/// Get the hash code of this <see cref="PureStorage{T}"/>
		/// </summary>
		/// <returns>the hash code</returns>
		public override int GetHashCode() => this.HashCodeOfArray();
		#endregion
	}

	/// <summary>
	/// Represents a storage of a contiguous memory block on a certain memory location, inherits <see cref="Storage{T}"/>
	/// </summary>
	/// <typeparam name="T">any unmanaged data type</typeparam>
	public class PureStorage<T> : ActualStorage<T> where T : unmanaged
	{
		#region basic
		private readonly StoragePointer pointer;

		/// <summary>
		/// <b>Allocate</b> and create a <see cref="PureStorage{T}"/> of given <see cref="Storage{T}.Length"/> on given <see cref="StorageLocation"/> 
		/// </summary>
		/// <param name="location">a <see cref="StorageLocation"/> to represent the memory location</param>
		/// <param name="length">the length of contiguous memory block in <typeparamref name="T"/></param>
		public PureStorage(StorageLocation location, ulong length)
		{
			this.pointer = Allocate(location, length);
		}
		#endregion

		#region override
		/// <summary>
		/// The number of <see cref="StoragePointer"/>(s) of this <see cref="Storage{T}"/> 
		/// </summary>
		public override int Count => 1;

		/// <summary>
		/// Indexer of the <see cref="StoragePointer"/>(s) of this <see cref="Storage{T}"/> (in presenting order)
		/// </summary>
		/// <param name="index">the element index</param>
		/// <returns>the <see cref="StoragePointer"/> at <paramref name="index"/></returns>
		public override StoragePointer this[int index] {
			get {
				if (index < 0 || index >= 1)
					throw new ArgumentOutOfRangeException(nameof(index));
				return pointer;
			}
		}
		#endregion
	}

	/// <summary>
	/// Represents a storage of several contiguous memory blocks on different memory locations with fixed sizes, inherits <see cref="Storage{T}"/>
	/// </summary>
	/// <typeparam name="T">any unmanaged data type</typeparam>
	public class MixedStorage<T> : ActualStorage<T> where T : unmanaged
	{
		#region basic
		private readonly StoragePointer[] pointers;

		/// <summary>
		/// <b>Allocate</b> and create a <see cref="MixedStorage{T}"/> of given lengths on given <see cref="StorageLocation"/>s
		/// </summary>
		/// <param name="param">the <see cref="IEnumerable{T}"/> of given lengths and <see cref="StorageLocation"/>s</param>
		/// <param name="allowSameLocation">allow same <see cref="StorageLocation"/>s in <paramref name="param"/> or not</param>
		public MixedStorage(IEnumerable<(StorageLocation location, ulong length)> param, bool allowSameLocation = true)
		{
			var temp = new List<StoragePointer>();
			foreach (var (location, length) in param)
			{
				if (!allowSameLocation && temp.Contains(location, selector: p => p.Location))
					throw new ArgumentOutOfRangeException(nameof(param), Resource.DuplicateValue);
				temp.Add(Allocate(location, length));
			}
			if (temp.Count <= 1)
				throw new ArgumentOutOfRangeException(nameof(param), Resource.ParamWrongSize);
			this.pointers = temp.ToArray();
		}

		/// <summary>
		/// <b>Allocate</b> and create a <see cref="MixedStorage{T}"/> of given lengths on given <see cref="StorageLocation"/>s
		/// </summary>
		/// <param name="param">the <see cref="Array"/> of given <see cref="Storage{T}.Length"/>s on given <see cref="StorageLocation"/>s</param>
		public MixedStorage(params (StorageLocation location, ulong length)[] param) : this(param as IEnumerable<(StorageLocation location, ulong length)>) { }

		/// <summary>
		/// <b>Allocate</b> and create a <see cref="MixedStorage{T}"/> of given lengths on given <see cref="StorageLocation"/>s
		/// </summary>
		/// <param name="locations">the <see cref="IEnumerable{T}"/> of given <see cref="StorageLocation"/>s</param>
		/// <param name="lengths">the <see cref="IEnumerable{T}"/> of given lengths</param>
		/// <param name="allowSameLocation">allow same <see cref="StorageLocation"/>s in <paramref name="locations"/> or not</param>
		public MixedStorage(IEnumerable<StorageLocation> locations, IEnumerable<ulong> lengths, bool allowSameLocation = true) : this(System.Linq.Enumerable.Zip(locations, lengths), allowSameLocation) { }
		#endregion

		#region override
		/// <summary>
		/// The number of <see cref="StoragePointer"/>(s) of this <see cref="Storage{T}"/> 
		/// </summary>
		public override int Count => this.pointers.Length;

		/// <summary>
		/// Indexer of the <see cref="StoragePointer"/>(s) of this <see cref="Storage{T}"/> (in presenting order)
		/// </summary>
		/// <param name="index">the element index</param>
		/// <returns>the <see cref="StoragePointer"/> at <paramref name="index"/></returns>
		public override StoragePointer this[int index] {
			get {
				if (index < 0 || index >= this.Count)
					throw new ArgumentOutOfRangeException(nameof(index));
				return this.pointers[index];
			}
		}
		#endregion
	}

	/// <summary>
	/// Represents a storage of several contiguous memory blocks on different memory locations with variable sizes purposed to cache memories of higher performance, inherits <see cref="Storage{T}"/>
	/// </summary>
	/// <typeparam name="T">any unmanaged data type</typeparam>
	public class CachedStorage<T> : ActualStorage<T> where T : unmanaged
	{
		#region basic
		private readonly StoragePointer[] pointers;

		private readonly Uri uri;

		/// <summary>
		/// <b>Allocate</b> and create a <see cref="CachedStorage{T}"/> of given <see cref="StorageLocation"/>s as priorities and total length (<see cref="Storage{T}.Length"/>) in <typeparamref name="T"/>
		/// </summary>
		/// <param name="priorities">the <see cref="IEnumerable{T}"/> of <see cref="StorageLocation"/>s to represent the priorities from higher-performance memories to lower ones</param>
		/// <param name="totalLength">the desired total length (in <typeparamref name="T"/>) of presenting array</param>
		public CachedStorage(IEnumerable<StorageLocation> priorities, ulong totalLength)
		{
			var temp = new List<StoragePointer>();
			foreach (var location in priorities)
			{
				if (temp.Contains(location, selector: p => p.Location))
					throw new ArgumentOutOfRangeException(nameof(priorities), Resource.DuplicateValue);
				// do not allocate here
				temp.Add(new StoragePointer(location, default(IntPtr), 0));
			}
			if (temp.Count <= 1)
				throw new ArgumentOutOfRangeException(nameof(priorities), Resource.ParamWrongSize);
			this.pointers = temp.ToArray();
			if (this.pointers[..^1].Any(p => p.Location.Location == MemoryLocation.Uri))
				throw new ArgumentException(Resource.UnexpectedValue, nameof(priorities));
			// allocate here
			// TODO
			IntPtr ptr = Marshal.StringToHGlobalUni(this.uri.AbsoluteUri);
		}
		#endregion

		#region override
		/// <summary>
		/// The function that actually dispose this storage, override <see cref="Storage{T}.Dispose(bool)"/>
		/// </summary>
		/// <param name="disposeManaged">dispose managed resources or not</param>
		protected override void Dispose(bool disposeManaged)
		{
			base.Dispose(disposeManaged);
			if (this.uri is not null)
				Marshal.FreeHGlobal(this.pointers[^1].Pointer);
		}

		/// <summary>
		/// The number of <see cref="StoragePointer"/>(s) of this <see cref="Storage{T}"/> 
		/// </summary>
		public override int Count => this.pointers.Length;

		/// <summary>
		/// Indexer of the <see cref="StoragePointer"/>(s) of this <see cref="Storage{T}"/> (in presenting order)
		/// </summary>
		/// <param name="index">the element index</param>
		/// <returns>the <see cref="StoragePointer"/> at <paramref name="index"/></returns>
		public override StoragePointer this[int index] {
			get {
				if (index < 0 || index >= this.Count)
					throw new ArgumentOutOfRangeException(nameof(index));
				return this.pointers[index];
			}
		}
		#endregion
	}
	#endregion


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
