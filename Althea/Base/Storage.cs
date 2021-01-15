using System;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Althea.Linq;
using Althea.Helpers;
using Althea.Resources;


namespace Althea
{
	#region storage location enum
	/// <summary>
	/// The enum of the storage locations, all values larger than <see cref="OtherRam_0"/> are all considered as some platform-specific local RAMs.
	/// </summary>
	public enum StorageLocation : byte
	{
		/// <summary>
		/// Represents a storage location represented by Universal Resource Identifier.
		/// </summary>
		/// <remarks>It has different logic than other memory based <see cref="StorageLocation"/>s.</remarks>
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
	public enum UriScheme : int
	{
		/// <summary>
		/// Specifies that the URI scheme is unknown
		/// </summary>
		Unknown = 0,
		/// <summary>
		/// Specifies that the URI is a pointer to a file
		/// </summary>
		File = 1,
		/// <summary>
		/// Specifies that the URI is accessed through the File Transfer Protocol (FTP).
		/// </summary>
		FTP = 1,
		/// <summary>
		/// Specifies that the URI is accessed through the TCP/IP directly.
		/// </summary>
		TCP = 1,
	}

	/// <summary>
	/// The static class for extension methods of <see cref="UriScheme"/>
	/// </summary>
	public static class UriSchemeExtension
	{
		/// <summary>
		/// Get the <see cref="UriScheme"/> from a <see cref="Uri"/>
		/// </summary>
		/// <param name="uri">the absolute <see cref="Uri"/></param>
		/// <returns>the <see cref="UriScheme"/> of <paramref name="uri"/>, or null if <paramref name="uri"/>'s scheme is not in <see cref="UriScheme"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="uri"/> is not an absolute URI</exception>
		public static UriScheme? GetScheme(this Uri uri)
		{
			if (!uri.IsAbsoluteUri)
				throw new ArgumentOutOfRangeException(nameof(uri));
			if (uri.Scheme == Uri.UriSchemeFile)
				return UriScheme.File;
			if (uri.Scheme == Uri.UriSchemeFtp)
				return UriScheme.FTP;
			if (uri.Scheme == @"tcp")
				return UriScheme.TCP;
			return null;
		}
	}

	/// <summary>
	/// The enum for the type of the description the combination of storage locations
	/// </summary>
	public enum CombinationType : int
	{
		/// <summary>
		/// Storage composed of only one memory location (pure) or a <b>set</b> of several memory locations (mixed). The first bit 0 indicates that this represents a set.
		/// </summary>
		PureOrMixed = 0 | 0,
		/// <summary>
		/// Storage composed of several <b>ordered</b> memory locations and a possible URI. The first bit 1 indicates that this represents a list.
		/// </summary>
		Cached = 0 | 1,
	}
	#endregion


	#region storage location structures
	/// <summary>
	/// The struct of a storage location with some extra details
	/// </summary>
	/// <remarks>This struct has size of a <see cref="int"/>. The <see cref="StorageLocation"/> occupies first few bits and its detail occupies the rest (slightly smaller than a full <see cref="int"/>).</remarks>
	[StructLayout(LayoutKind.Sequential, Size = sizeof(int))]
	public readonly struct StorageDetail : IEquatable<StorageDetail>
	{
		#region basic
		private readonly int _data;

		/// <summary>
		/// The location of this <see cref="StorageDetail"/>
		/// </summary>
		public StorageLocation Location => (StorageLocation)unchecked((byte)this._data);

		/// <summary>
		/// The location detail of <see cref="Location"/>.
		/// </summary>
		public int LocationDetail => this._data >> (sizeof(StorageLocation) * 8);

		/// <summary>
		/// Create with given location and device ID
		/// </summary>
		/// <param name="location">The location of this <see cref="StorageDetail"/>, must be a flag</param>
		/// <param name="detail">The detail of <paramref name="location"/>: a URI scheme for a <see cref="StorageLocation.Uri"/> or a device ID otherwise.</param>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="detail"/> is too large to fit with <see cref="StorageLocation"/></exception>
		public StorageDetail(StorageLocation location, int detail)
		{
			if (detail >> ((sizeof(int) - sizeof(StorageLocation)) * 8) != 0)
				throw new ArgumentOutOfRangeException(nameof(detail), Parameter.InvalidValue);
			this._data = (byte)location + (detail << (sizeof(StorageLocation) * 8));
		}
		#endregion

		#region equality
		/// <summary>
		/// Whether this == <paramref name="other"/>
		/// </summary>
		/// <param name="other">another <see cref="StorageDetail"/> to compare</param>
		/// <returns>this == <paramref name="other"/></returns>
		public bool Equals(StorageDetail other)
		{
			return this._data == other._data;
		}

		/// <summary>
		/// Override <see cref="ValueType.Equals(object?)"/> to check whether this == <paramref name="obj"/>
		/// </summary>
		/// <param name="obj">another object to compare</param>
		/// <returns>this == <paramref name="obj"/></returns>
		public override bool Equals(object obj)
		{
			return obj is StorageDetail storageDetail && this.Equals(storageDetail);
		}

		/// <summary>
		/// Override <see cref="ValueType.GetHashCode"/> to get the hash code this <see cref="StorageDetail"/>.
		/// </summary>
		/// <returns>The hash code</returns>
		public override int GetHashCode()
		{
			return this._data;
		}

		/// <summary>
		/// Equality operator
		/// </summary>
		public static bool operator ==(StorageDetail left, StorageDetail right)
		{
			return left.Equals(right);
		}

		/// <summary>
		/// Inequality operator
		/// </summary>
		public static bool operator !=(StorageDetail left, StorageDetail right)
		{
			return !(left == right);
		}
		#endregion

		#region string related
		private static readonly Dictionary<StorageLocation, string> static_OtherMemoryNames = new Dictionary<StorageLocation, string>();

		/// <summary>
		/// Set the name used for <see cref="ToString"/> of this if it represents a storage position in other memory types like <see cref="StorageLocation.OtherRam_0"/>
		/// </summary>
		/// <param name="location">the <see cref="StorageLocation"/> of a storage position in other memory types</param>
		/// <param name="name">the name as a <see cref="string"/> to set; notice that all the spaces will be replaced by '_'</param>
		/// <returns>success or not</returns>
		public static bool SetOtherMemoryName(StorageLocation location, string name)
		{
			if (location < StorageLocation.OtherRam_0)
				return false;
			static_OtherMemoryNames[location] = name.Replace(' ', '_');
			return true;
		}

		/// <summary>
		/// Return the string representation of this <see cref="StorageDetail"/>
		/// </summary>
		/// <returns>the string representation of this <see cref="StorageDetail"/></returns>
		public override string ToString()
		{
			var str = this.Location switch
			{
				StorageLocation.Uri => "URI_" + (UriScheme)this.LocationDetail,
				StorageLocation.CpuRam => "CPU_Memory",
				StorageLocation.GpuRam => "GPU_Memory",
				_ => static_OtherMemoryNames.GetValueOrDefault(this.Location) ?? $"Other_Device_Memory_Order_{this.Location - StorageLocation.OtherRam_0}",
			};
			if (this.Location != StorageLocation.Uri)
			{
				str += $"(device_ID={this.LocationDetail})";
			}
			return str;
		}
		#endregion
	}

	/// <summary>
	/// The description of a combination of storage location(s)
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public readonly struct StorageDetailsCombination : IEquatable<StorageDetailsCombination>, IReadOnlyList<StorageDetail>
	{
		#region basic
		private readonly CombinationType type;

		private readonly FixedBuffer_60<StorageDetail> data;

		/// <summary>
		/// Get a <see cref="bool"/> to indicate whether this <see cref="StorageDetailsCombination"/>'s underlying data is a set or a list.
		/// </summary>
		/// <returns>Whether the underlying data is a set or a list.</returns>
		public bool IsASet() => !((short)this.type).IsBitSet(0);

		/// <summary>
		/// The <see cref="CombinationType"/> of this <see cref="StorageDetailsCombination"/>
		/// </summary>
		public CombinationType Type => this.type;

		/// <summary>
		/// Create a <see cref="StorageDetailsCombination"/> with given <see cref="CombinationType"/> (whether <paramref name="type"/> represents a set or a list is defined inside), and a <see cref="ReadOnlySpan{T}"/> containing the actual data
		/// </summary>
		/// <param name="type">The <see cref="CombinationType"/></param>
		/// <param name="data">A <see cref="ReadOnlySpan{T}"/> of <see cref="StorageDetail"/> containing the actual storage details, must has length between 1 and 15</param>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="data"/> has incompatible size</exception>
		public StorageDetailsCombination(CombinationType type, ReadOnlySpan<StorageDetail> data)
		{
			if (data.Length > 15 || data.Length <= 0)
				throw new ArgumentOutOfRangeException(nameof(data), Parameter.WrongSize);
			// initialize
			this.type = type;
			this.data = new FixedBuffer_60<StorageDetail>();
			// set the values of data
			Span<StorageDetail> span = stackalloc StorageDetail[data.Length];
			data.CopyTo(span);
			if (!this.IsASet())
			{
				span.Sort();
			}
			for (int i = 0; i < 15; i++)
			{
				this.data[i] = span[i];
			}
		}

		/// <summary>
		/// Create a <see cref="StorageDetailsCombination"/> from a single <see cref="StorageDetail"/>
		/// </summary>
		/// <param name="memoryLocation">the given <see cref="StorageDetail"/></param>
		public StorageDetailsCombination(StorageDetail memoryLocation)
		{
			this.type = CombinationType.PureOrMixed;
			this.data = new FixedBuffer_60<StorageDetail>();
			this.data[0] = memoryLocation;
		}
		#endregion

		#region equality
		/// <summary>
		/// Whether this == <paramref name="other"/>
		/// </summary>
		/// <param name="other">another <see cref="StorageDetailsCombination"/> to compare</param>
		/// <returns>this == <paramref name="other"/></returns>
		public bool Equals(StorageDetailsCombination other)
		{
			return this.type == other.type && this.data == other.data;
		}

		/// <summary>
		/// Override <see cref="ValueType.Equals(object?)"/> to check whether this == <paramref name="obj"/>
		/// </summary>
		/// <param name="obj">another object to compare</param>
		/// <returns>this == <paramref name="obj"/></returns>
		public override bool Equals(object obj)
		{
			return obj is StorageDetailsCombination descr && this.Equals(descr);
		}

		/// <summary>
		/// Override <see cref="ValueType.GetHashCode"/> to get the hash code this <see cref="StorageDetailsCombination"/>.
		/// </summary>
		/// <returns>The hash code</returns>
		public override int GetHashCode()
		{
			if (this.IsASet())
				return HashCode.Combine(this.type, this.data.HashCodeOfSet());
			else
				return HashCode.Combine(this.type, this.data);
		}

		/// <summary>
		/// Equality operator
		/// </summary>
		public static bool operator ==(StorageDetailsCombination left, StorageDetailsCombination right)
		{
			return left.Equals(right);
		}

		/// <summary>
		/// Inequality operator
		/// </summary>
		public static bool operator !=(StorageDetailsCombination left, StorageDetailsCombination right)
		{
			return !(left == right);
		}
		#endregion

		#region index
		/// <summary>
		/// The number of <see cref="StorageDetail"/>s in this description
		/// </summary>
		public int Count {
			get {
				for (int i = 0; i < this.data.Count; i++)
				{
					if (this.data[i] == default)
						return i - 1;
				}
				return this.data.Count;
			}
		}

		/// <summary>
		/// Basic indexer of this <see cref="StorageDetailsCombination"/>
		/// </summary>
		/// <param name="index">the index</param>
		/// <returns>The element at <paramref name="index"/> as a <see cref="StorageDetail"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="index"/> is out of range</exception>
		public StorageDetail this[int index] => index >= 0 && index < this.Count ? this.data[index] : throw new ArgumentOutOfRangeException(nameof(index));

		/// <summary>
		/// Forms a slice out of the current <see cref="StorageDetailsCombination"/> starting at a specified <paramref name="start"/> for a specified <paramref name="length"/>.
		/// </summary>
		/// <param name="start">The index at which to begin this slice.</param>
		/// <param name="length">The desired length for the slice.</param>
		/// <returns>A <see cref="StorageDetailsCombination"/> that consists of <see cref="StorageDetail"/>s composed of <paramref name="length"/> elements from the <paramref name="start"/> and the same <see cref="Type"/>.</returns>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="start"/> and/or <paramref name="length"/> exceeds the boundary of this <see cref="StorageDetailsCombination"/></exception>
		public StorageDetailsCombination Slice(int start, int length)
		{
			int count = this.Count;
			if (start < 0 || start >= count)
				throw new ArgumentOutOfRangeException(nameof(start));
			if (length <= 0 || length + start > count)
				throw new ArgumentOutOfRangeException(nameof(length));

			return new StorageDetailsCombination(this.type, this.data.AsSpan().Slice(start, length));
		}

		/// <summary>
		/// Forms a slice out of the current <see cref="StorageDetailsCombination"/> starting at a specified <paramref name="start"/> to the end.
		/// </summary>
		/// <param name="start">The index at which to begin this slice.</param>
		/// <returns>A <see cref="StorageDetailsCombination"/> that consists of <see cref="StorageDetail"/>s composed of (<see cref="Count"/> - <paramref name="start"/>) elements from the <paramref name="start"/> and the same <see cref="Type"/>.</returns>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="start"/> exceeds the boundary of this <see cref="StorageDetailsCombination"/></exception>
		public StorageDetailsCombination Slice(int start) => this[start..];

		IEnumerator<StorageDetail> IEnumerable<StorageDetail>.GetEnumerator()
		{
			int count = this.Count;
			var span = this.data.AsSpan();
			for (int i = 0; i < count; i++)
			{
				yield return span[i];
			}
		}

		IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<StorageDetail>)this).GetEnumerator();
		#endregion

		#region conversion
		/// <summary>
		/// Implicitly convert a <see cref="StorageDetail"/> to a <see cref="StorageDetailsCombination"/>
		/// </summary>
		/// <param name="storageDetail">The <see cref="StorageDetail"/> to be converted</param>
		public static implicit operator StorageDetailsCombination(StorageDetail storageDetail) => new StorageDetailsCombination(storageDetail);
		#endregion

		#region string related
		/// <summary>
		/// Return the string representation of this <see cref="StorageDetailsCombination"/>
		/// </summary>
		/// <returns>the string representation of this <see cref="StorageDetailsCombination"/></returns>
		public override string ToString()
		{
			return $"Description of Storage Locations [type={this.type}, data={this.data}]";
		}
		#endregion
	}
	#endregion


	#region storage pointer
	/// <summary>
	/// The struct of a pointer at a certain unmanaged memory block
	/// </summary>
	/// <remarks>This struct is <b>not</b> responsible for releasing unmanaged memories. It is only used for storing information of memory blocks.</remarks>
	[StructLayout(LayoutKind.Explicit)]
	public readonly struct StoragePointer : IEquatable<StoragePointer>
	{
		#region basic
		[FieldOffset(0)]
		private readonly StorageDetail location;

		[FieldOffset(sizeof(long))]
		private readonly IntPtr pointer;

		[FieldOffset(sizeof(long) * 2)]
		private readonly ulong length;

		/// <summary>
		/// The <see cref="StorageDetail"/> of this <see cref="StoragePointer"/>
		/// </summary>
		public StorageDetail Location => location;

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
		/// <param name="location">The <see cref="StorageDetail"/> of this <see cref="StoragePointer"/></param>
		/// <param name="pointer">The pointer at the given <paramref name="location"/></param>
		/// <param name="length">The length in bytes of the given <paramref name="pointer"/></param>
		public StoragePointer(StorageDetail location, IntPtr pointer, ulong length)
		{
			this.location = location; this.pointer = pointer; this.length = length;
		}

		/// <summary>
		/// Create with given location and pointer
		/// </summary>
		/// <param name="location">The <see cref="StorageDetail"/> of this <see cref="StoragePointer"/></param>
		/// <param name="pointer">The pointer at the given <paramref name="location"/></param>
		/// <param name="length">The length of the given <paramref name="pointer"/></param>
		public unsafe StoragePointer(StorageDetail location, void* pointer, ulong length) : this(location, new IntPtr(pointer), length) { }

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

		/// <summary>
		/// Create a new <see cref="StoragePointer"/> with given new <see cref="LengthInBytes"/>
		/// </summary>
		/// <param name="newLength">The new <see cref="LengthInBytes"/></param>
		/// <returns>The new <see cref="StoragePointer"/></returns>
		public StoragePointer AsLength(ulong newLength) => new StoragePointer(this, newLength);

		/// <summary>
		/// Create a new <see cref="StoragePointer"/> with given new <paramref name="offset"/>
		/// </summary>
		/// <param name="offset">The offset</param>
		/// <returns>The new <see cref="StoragePointer"/></returns>
		public StoragePointer AsOffset(long offset) => new StoragePointer(this, offset);
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
		/// Get the unmanaged pointer (a <c>void*</c>) of this <see cref="StoragePointer"/>
		/// </summary>
		/// <returns>the unmanaged pointer as a <c>void*</c></returns>
		public unsafe void* UnmangedPointer => this.pointer.ToPointer();

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


	#region storage classes

	#region interfaces
	/// <summary>
	/// The interface for wrapper of unmanaged memory block(s) of different <see cref="StorageDetail"/>(s) of any data type
	/// </summary>
	public interface IStorage : IDisposable, IReadOnlyList<StoragePointer>
	{
		/// <summary>
		/// The total length of the presenting array in bytes
		/// </summary>
		ulong LengthInBytes { get; }

		/// <summary>
		/// The description of the storage locations of this storage class as a <see cref="StorageDetailsCombination"/>
		/// </summary>
		StorageDetailsCombination LocationsCombination { get; }

		/// <summary>
		/// Check whether this storage is valid or not
		/// </summary>
		/// <returns>The validness of this storage</returns>
		bool IsValid();

		/// <summary>
		/// Check whether this storage is valid or not after moving an <paramref name="offset"/> and set <see cref="LengthInBytes"/> to <paramref name="newLength"/>
		/// </summary>
		/// <param name="offset">the offset to move</param>
		/// <param name="newLength">the length to check in bytes, default 0 means auto calculation by <paramref name="offset"/></param>
		/// <returns>The validness of this storage under <paramref name="offset"/> and <paramref name="newLength"/></returns>
		bool IsOffsetValid(long offset, ulong newLength = 0);

		/// <summary>
		/// Check the given storage and throw exception if check failed.
		/// </summary>
		/// <exception cref="ArgumentException">if this storage has invalid value</exception>
		/// <exception cref="ArgumentOutOfRangeException">if offset and length breach the boundary of this storage</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Check(long offset = 0, ulong length = 0)
		{
			if (!this.IsValid())
				throw new ArgumentException(Parameter.InvalidValue);
			if ((offset != 0 || length != 0) && !this.IsOffsetValid(offset, length))
				throw new ArgumentOutOfRangeException($"{nameof(offset)}, {nameof(length)}");
		}
	}

	internal interface IReferenceStorage
	{
		IStorage Reference { get; }

		long TotalOffset { get; }
	}
	#endregion

	/// <summary>
	/// The abstract wrapper class of unmanaged memory block(s) of different <see cref="StorageDetail"/>(s).
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
		public abstract ulong Length { get; }

		/// <summary>
		/// The total length of the presenting array in bytes
		/// </summary>
		public virtual ulong LengthInBytes => this.Length * (ulong)SizeOfT;

		/// <summary>
		/// The description of the storage locations of this storage class as a <see cref="StorageDetailsCombination"/>
		/// </summary>
		public abstract StorageDetailsCombination LocationsCombination { get; }

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

		/// <summary>
		/// Check whether this storage is valid or not. The default implementation does not suit the URI storage (like <see cref="UriStorage{T}"/>) case.
		/// </summary>
		/// <returns>The validness of this storage</returns>
		public virtual bool IsValid()
		{
			if (this.Disposed)
			{
				return false;
			}
			for (int i = 0; i < this.Count; i++)
			{
				var pointer = this[i];
				if (pointer.Pointer == default || pointer.LengthInBytes == 0 || pointer.Location == default)
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Check whether this storage is valid or not after moving an <paramref name="offset"/> and set <see cref="LengthInBytes"/> to <paramref name="newLength"/>. The default implementation works for <see cref="ReferenceStorage{T}"/>, <see cref="ActualStorage{T}"/> and <see cref="UriStorage{T}"/>.
		/// </summary>
		/// <param name="offset">the offset to move</param>
		/// <param name="newLength">the length to check in bytes</param>
		/// <returns>The validness of this storage under <paramref name="offset"/> and <paramref name="newLength"/></returns>
		public virtual bool IsOffsetValid(long offset, ulong newLength = 0)
		{
			if (this is IReferenceStorage reference)
			{
				offset += reference.TotalOffset;
				if (offset < 0 || (ulong)offset >= reference.Reference.LengthInBytes)
					return false;
				if (newLength > 0 && newLength + (ulong)offset >= reference.Reference.LengthInBytes)
					return false;
				return true;
			}
			else
			{
				if (offset < 0 || (ulong)offset >= this.LengthInBytes)
					return false;
				if (newLength > 0 && newLength + (ulong)offset >= this.LengthInBytes)
					return false;
				return true;
			}
		}
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
	public sealed class ReferenceStorage<T> : Storage<T>, IReferenceStorage where T : unmanaged
	{
		#region basic
		private readonly IStorage reference;

		private readonly long totalOffset;

		IStorage IReferenceStorage.Reference => this.reference;

		long IReferenceStorage.TotalOffset => this.totalOffset;

		private readonly int start, end;

		private readonly ulong startOffsetBytes, endLengthBytes;

		/// <summary>
		/// The number of <see cref="StoragePointer"/>(s) of this <see cref="Storage{T}"/> 
		/// </summary>
		public override int Count => this.end - this.start;

		/// <summary>
		/// Override <see cref="Storage{T}.Length"/> to show the new presenting length
		/// </summary>
		public override ulong Length { get; }

		/// <summary>
		/// The description of the storage locations of this storage class as a <see cref="StorageDetailsCombination"/>
		/// </summary>
		public override StorageDetailsCombination LocationsCombination => this.reference.LocationsCombination[this.start..this.end];

		/// <summary>
		/// Create a <see cref="ReferenceStorage{T}"/> with given reference <paramref name="storage"/> and <paramref name="offset"/> to it
		/// </summary>
		/// <param name="storage">the <see cref="Storage{T}"/> to be referenced</param>
		/// <param name="offset">the total offset in <typeparamref name="T"/> as a <see cref="long"/></param>
		/// <param name="newLength">the new presenting length in <typeparamref name="T"/>, default 0 means automatically calculate by <paramref name="storage"/> and <paramref name="offset"/></param>
		public ReferenceStorage(IStorage storage, long offset = 0, ulong newLength = 0)
		{
			// dereference first
			while (storage is IReferenceStorage @ref)
			{
				storage = @ref.Reference;
				offset += @ref.TotalOffset;
			}
			// check
			if (offset < 0)
				throw new ArgumentOutOfRangeException(nameof(offset));
			if (newLength == 0)
				newLength = storage.LengthInBytes - (ulong)(SizeOfT * offset);
			else if (storage.LengthInBytes != (ulong)offset + newLength)
				throw new ArgumentOutOfRangeException(nameof(offset));
			// set length
			this.Length = newLength;
			// set offsets
			ulong offsetInBytes = (ulong)(offset * SizeOfT);
			for (int i = 0; i < storage.Count; i++)
			{
				ulong lengthOfI = storage[i].LengthInBytes;
				if (offsetInBytes < lengthOfI)
				{
					this.start = i; this.startOffsetBytes = offsetInBytes;
					break;
				}
				else
				{
					offsetInBytes -= lengthOfI;
				}
			}
			offsetInBytes += newLength;
			for (int i = this.start; i < storage.Count; i++)
			{
				ulong lengthOfI = storage[i].LengthInBytes;
				if (offsetInBytes <= lengthOfI)
				{
					this.end = i + 1; this.endLengthBytes = offsetInBytes;
					break;
				}
				else
				{
					offsetInBytes -= lengthOfI;
				}
			}
		}

		private ReferenceStorage(IStorage storage, long totalOffset, int start, ulong startOffset, ulong endLength, ulong newLength)
		{
			this.reference = storage; this.totalOffset = totalOffset;
			this.start = start; this.startOffsetBytes = startOffset; this.endLengthBytes = endLength;
			this.Length = newLength;
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
				else if (index == this.Count - 1)
				{
					pointer = new StoragePointer(pointer, newLength: this.endLengthBytes);
				}
				return pointer;
			}
		}

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
				throw new InvalidCastException(Other.CannotDivide);
			offset /= Storage<TOut>.SizeOfT;
			return new ReferenceStorage<TOut>(this.reference, offset, this.start, this.startOffsetBytes, this.endLengthBytes, this.Length);
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
		/// The total length of the presenting array in <typeparamref name="T"/> (rather than bytes), override <see cref="Storage{T}.Length"/>
		/// </summary>
		public override ulong Length { get; }

		/// <summary>
		/// Create an <see cref="ActualStorage{T}"/> with given length of presenting array
		/// </summary>
		/// <param name="length">the length of presenting array <typeparamref name="T"/></param>
		protected ActualStorage(ulong length)
		{
			if (length == 0)
				throw new ArgumentOutOfRangeException(nameof(length), Parameter.MustPositive);
			this.Length = length;
		}

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
		/// Allocate a <see cref="StoragePointer"/> of given <see cref="Storage{T}.Length"/> on given <see cref="StorageDetail"/> 
		/// </summary>
		/// <param name="location">a <see cref="StorageDetail"/> to represent the memory location</param>
		/// <param name="length">the length of contiguous memory block in <typeparamref name="T"/></param>
		protected static StoragePointer Allocate(StorageDetail location, ulong length)
		{
			IntPtr ptr = default;
			// TODO: adapter allocate
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
			if (this.LengthInBytes % (ulong)Storage<TOut>.SizeOfT != 0)
				throw new InvalidCastException(Other.CannotDivide);
			ulong newLength = this.LengthInBytes / (ulong)Storage<TOut>.SizeOfT;
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
		/// The description of the storage locations of this storage class as a <see cref="StorageDetailsCombination"/>
		/// </summary>
		public override StorageDetailsCombination LocationsCombination => this.pointer.Location;

		/// <summary>
		/// <b>Allocate</b> and create a <see cref="PureStorage{T}"/> of given <see cref="Storage{T}.Length"/> on given <see cref="StorageDetail"/> 
		/// </summary>
		/// <param name="location">a <see cref="StorageDetail"/> to represent the memory location</param>
		/// <param name="length">the length of contiguous memory block in <typeparamref name="T"/></param>
		public PureStorage(StorageDetail location, ulong length) : base(length)
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
		/// The description of the storage locations of this storage class as a <see cref="StorageDetailsCombination"/>
		/// </summary>
		public override StorageDetailsCombination LocationsCombination {
			get {
				Span<StorageDetail> span = stackalloc StorageDetail[this.Count];
				this.CopyTo(span, p => p.Location);
				return new StorageDetailsCombination(CombinationType.PureOrMixed, span);
			}
		}

		/// <summary>
		/// <b>Allocate</b> and create a <see cref="MixedStorage{T}"/> of given lengths on given <see cref="StorageDetail"/>s
		/// </summary>
		/// <param name="param">the <see cref="IReadOnlyList{T}"/> of given lengths and <see cref="StorageDetail"/>s</param>
		/// <param name="allowSameLocation">allow same <see cref="StorageDetail"/>s in <paramref name="param"/> or not</param>
		public MixedStorage(IReadOnlyList<(StorageDetail location, ulong length)> param, bool allowSameLocation = true) : base(param.Sum(p => p.length))
		{
			if (param.Count <= 1)
				throw new ArgumentOutOfRangeException(nameof(param), Parameter.WrongSize);
			this.pointers = new StoragePointer[param.Count];
			for (int i = 0; i < param.Count; i++)
			{
				var (location, length) = param[i];
				if (!allowSameLocation && pointers.Contains(location, selector: p => p.Location))
					throw new ArgumentOutOfRangeException(nameof(param), Parameter.DuplicateValue);
				this.pointers[i] = Allocate(location, length);
			}
		}

		/// <summary>
		/// <b>Allocate</b> and create a <see cref="MixedStorage{T}"/> of given lengths on given <see cref="StorageDetail"/>s
		/// </summary>
		/// <param name="param">the <see cref="Array"/> of given <see cref="Storage{T}.Length"/>s on given <see cref="StorageDetail"/>s</param>
		public MixedStorage(params (StorageDetail location, ulong length)[] param) : this(param as IReadOnlyList<(StorageDetail location, ulong length)>) { }

		/// <summary>
		/// <b>Allocate</b> and create a <see cref="MixedStorage{T}"/> of given lengths on given <see cref="StorageDetail"/>s
		/// </summary>
		/// <param name="locations">the <see cref="IEnumerable{T}"/> of given <see cref="StorageDetail"/>s</param>
		/// <param name="lengths">the <see cref="IEnumerable{T}"/> of given lengths</param>
		/// <param name="allowSameLocation">allow same <see cref="StorageDetail"/>s in <paramref name="locations"/> or not</param>
		public MixedStorage(IReadOnlyList<StorageDetail> locations, IReadOnlyList<ulong> lengths, bool allowSameLocation = true) : this(locations.Zip(lengths), allowSameLocation) { }
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

		private readonly UriStorage<T> uriStorage;

		/// <summary>
		/// The description of the storage locations of this storage class as a <see cref="StorageDetailsCombination"/>
		/// </summary>
		public override StorageDetailsCombination LocationsCombination {
			get {
				Span<StorageDetail> span = stackalloc StorageDetail[this.Count];
				this.CopyTo(span, p => p.Location);
				return new StorageDetailsCombination(CombinationType.Cached, span);
			}
		}

		/// <summary>
		/// <b>Allocate</b> and create a <see cref="CachedStorage{T}"/> of given <see cref="StorageDetail"/>s and <see cref="ulong"/>s as priorities and total length (<see cref="Storage{T}.Length"/>) in <typeparamref name="T"/>
		/// </summary>
		/// <param name="priorities">the <see cref="IEnumerable{T}"/> of <see cref="StorageDetail"/>s and <see cref="ulong"/>s to represent the priorities from higher-performance memories to lower ones (cannot contain <see cref="StorageLocation.Uri"/> or any duplicate locations)</param>
		/// <param name="totalLength">the desired total length (in <typeparamref name="T"/>) of presenting array</param>
		/// <param name="cacheUri">the final caching indicated by a <see cref="Uri"/>, default null means do not cache to URI</param>
		/// <exception cref="ArgumentException">if <paramref name="priorities"/> has unexpected value(s) or is of wrong size</exception>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="totalLength"/> or <paramref name="cacheUri"/> has unexpected value(s)</exception>
		public CachedStorage(IEnumerable<(StorageDetail location, ulong maxLengthInBytes)> priorities, ulong totalLength, Uri cacheUri = null) : base(totalLength)
		{
			var temp = new List<StoragePointer>();
			foreach (var (location, maxLengthInBytes) in priorities)
			{
				if (location.Location == StorageLocation.Uri)
					throw new ArgumentException(Parameter.UnexpectedValue, nameof(priorities));
				if (temp.Contains(location, selector: p => p.Location))
					throw new ArgumentException(Parameter.DuplicateValue, nameof(priorities));
				ulong length = 0; // TODO: adapter get available length on 'location'
				if (maxLengthInBytes != 0 && maxLengthInBytes < length)
				{
					length = maxLengthInBytes;
				}
				// do not allocate here
				temp.Add(new StoragePointer(location, default(IntPtr), length));
			}
			if (temp.Count <= 1)
				throw new ArgumentException(Parameter.WrongSize, nameof(priorities));
			ulong allowedTotalLength = temp.Sum(p => p.LengthInBytes);
			if (allowedTotalLength <= totalLength && cacheUri is null)
				throw new ArgumentOutOfRangeException(nameof(totalLength));
			// deal with URI
			if (cacheUri is not null)
			{
				UriScheme? scheme = cacheUri.GetScheme();
				if (!scheme.HasValue)
					throw new ArgumentOutOfRangeException(nameof(cacheUri), Parameter.InvalidValue);
				// do not allocate here
				temp.Add(new StoragePointer(new StorageDetail(StorageLocation.Uri, (byte)scheme.Value), default(IntPtr), totalLength <= allowedTotalLength ? 0 : totalLength - allowedTotalLength));
			}
			this.pointers = temp.ToArray();
			// allocate here
			// TODO: adapter allocate
		}

		/// <summary>
		/// The function that actually dispose this storage, override <see cref="ActualStorage{T}.Dispose(bool)"/>
		/// </summary>
		/// <param name="disposeManaged">dispose managed resources or not</param>
		protected override void Dispose(bool disposeManaged)
		{
			base.Dispose(disposeManaged);
			this.uriStorage?.Dispose();
		}
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
	/// Represents a storage of a "memory" block represented by a <see cref="Uri"/>, inherits <see cref="Storage{T}"/>
	/// </summary>
	/// <typeparam name="T">any unmanaged data type</typeparam>
	public class UriStorage<T> : Storage<T>, IAsyncDisposable where T : unmanaged
	{
		#region basic
		/// <summary>
		/// The total length of the presenting array in <typeparamref name="T"/> (rather than bytes), override <see cref="Storage{T}.Length"/>
		/// </summary>
		public override ulong Length => this.LengthInBytes / (ulong)SizeOfT;

		/// <summary>
		/// The description of the storage locations of this storage class as a <see cref="StorageDetailsCombination"/>
		/// </summary>
		public override StorageDetailsCombination LocationsCombination => this.Location;

		/// <summary>
		/// The total length of the presenting array in bytes, override <see cref="Storage{T}.LengthInBytes"/>
		/// </summary>
		public override ulong LengthInBytes => this.wrapper.Length;

		private readonly Memory.IUriWrapper wrapper;

		private StorageDetail Location => new StorageDetail(StorageLocation.Uri, (byte)this.wrapper.OriginalUri.GetScheme().Value);

		/// <summary>
		/// <b>Allocate</b> and create a <see cref="UriStorage{T}"/> of given <see cref="Storage{T}.Length"/> on given <see cref="Uri"/> 
		/// </summary>
		/// <param name="uri">a <see cref="Uri"/> to represent the resource name to create</param>
		/// <param name="length">the length of contiguous memory block in <typeparamref name="T"/></param>
		/// <exception cref="NotSupportedException">if <paramref name="uri"/> has unsupported scheme</exception>
		public UriStorage(Uri uri, ulong length)
		{
			if (!uri.GetScheme().HasValue)
				throw new NotSupportedException(Support.Location);
			// TODO: adapter create
			////this.wrapper = uri;
		}

		private UriStorage(Memory.IUriWrapper wrapper)
		{
			this.wrapper = wrapper;
		}
		#endregion

		#region other methods
		/// <summary>
		/// The function that actually dispose this storage, override <see cref="Storage{T}.Dispose(bool)"/>
		/// </summary>
		/// <param name="disposeManaged">dispose managed resources or not</param>
		protected override void Dispose(bool disposeManaged)
		{
			this.wrapper.Dispose();
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
		/// </summary>
		/// <returns>A task that represents the asynchronous dispose operation.</returns>
		public async ValueTask DisposeAsync()
		{
			await this.wrapper.DisposeAsync();
		}

		/// <summary>
		/// Resize this <see cref="UriStorage{T}"/>
		/// </summary>
		/// <param name="newLength">the new length in <typeparamref name="T"/></param>
		public void Resize(ulong newLength)
		{
			this.wrapper.Resize(newLength);
		}

		/// <summary>
		/// Resize this <see cref="UriStorage{T}"/> asynchronously
		/// </summary>
		/// <param name="newLength">the new length in <typeparamref name="T"/></param>
		public async ValueTask ResizeAsync(ulong newLength)
		{
			await this.wrapper.ResizeAsync(newLength);
		}
		#endregion

		#region override
		/// <summary>
		/// Convert this <see cref="ActualStorage{T}"/> to another one with different data type <typeparamref name="TOut"/>
		/// </summary>
		/// <typeparam name="TOut">the output data type</typeparam>
		/// <returns>a <see cref="UriStorage{TOut}"/></returns>
		/// <exception cref="InvalidCastException">if <see cref="IStorage.LengthInBytes"/> cannot be divided by <see cref="Storage{TOut}.SizeOfT"/></exception>
		public override UriStorage<TOut> As<TOut>()
		{
			if (this.LengthInBytes % (ulong)Storage<TOut>.SizeOfT != 0)
				throw new InvalidCastException(Other.CannotDivide);
			return new UriStorage<TOut>(this.wrapper);
		}

		/// <summary>
		/// Determines whether the specified object is equal to the current object.
		/// </summary>
		/// <param name="obj">another object</param>
		/// <returns>this equals to <paramref name="obj"/> or not</returns>
		public override bool Equals(Storage<T> obj)
		{
			if (obj is not null && obj is UriStorage<T> another)
			{
				return this.wrapper.OriginalUri == another.wrapper.OriginalUri;
			}
			return false;
		}

		/// <summary>
		/// Get the hash code of this <see cref="PureStorage{T}"/>
		/// </summary>
		/// <returns>the hash code</returns>
		public override int GetHashCode() => this.wrapper.OriginalUri.GetHashCode();

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
				return new StoragePointer(this.Location, default(IntPtr), this.LengthInBytes);
			}
		}

		/// <summary>
		/// Check whether this storage is valid or not. The default implementation does not suit the URI storage (like <see cref="UriStorage{T}"/>) case.
		/// </summary>
		/// <returns>The validness of this storage</returns>
		public override bool IsValid() => !this.Disposed && this.wrapper is not null;
		#endregion
	}
	#endregion
}
