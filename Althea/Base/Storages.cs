using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Althea.Linq;
using Althea.Helpers;
using Althea.Resources;

using MEM = Althea.Storage.AbstractApi;


namespace Althea
{
	#region storage location enum
	/// <summary>
	/// The enum of the storage location types
	/// </summary>
	public enum LocationType : byte
	{
		/// <summary>
		/// Represents a storage location represented by Universal Resource Identifier.
		/// </summary>
		Uri = 0 << LocationTypeExtension.ClassificationEnd | LocationTypeExtension.ClassStream,
		/// <summary>
		/// Storage at local CPU RAM
		/// </summary>
		CpuRam = 0 << LocationTypeExtension.ClassificationEnd | LocationTypeExtension.ClassMemory,
		/// <summary>
		/// Storage at local GPU RAM
		/// </summary>
		GpuRam = 1 << LocationTypeExtension.ClassificationEnd | LocationTypeExtension.ClassMemory,
		/// <summary>
		/// Storage at platform-specific local RAM (with custom order the 1st) other than <see cref="CpuRam"/> and <see cref="GpuRam"/>. For example, a RAM associated with a FPGA.
		/// </summary>
		OtherRam_0 = 2 << LocationTypeExtension.ClassificationEnd | LocationTypeExtension.ClassMemory,
	}

	/// <summary>
	/// Extension class for <see cref="LocationType"/>
	/// </summary>
	public static class LocationTypeExtension
	{
		/// <summary>
		/// The classification mask end bit of <see cref="LocationType"/>
		/// </summary>
		public const int ClassificationEnd = 3;
		
		/// <summary>
		/// The classification mask of <see cref="LocationType"/>
		/// </summary>
		public const int ClassificationMask = 0b111;

		/// <summary>
		/// The classification of stream typed <see cref="LocationType"/>
		/// </summary>
		/// <remarks>Other classifications are not presented here but they are also supported.</remarks>
		public const int ClassStream = 0b000;

		/// <summary>
		/// The classification of memory typed <see cref="LocationType"/>
		/// </summary>
		/// <remarks>Other classifications are not presented here but they are also supported.</remarks>
		public const int ClassMemory = 0b001;

		/// <summary>
		/// Get the classification of given <see cref="LocationType"/>
		/// </summary>
		/// <param name="locationType">The given <see cref="LocationType"/></param>
		/// <returns>The classification as a <see cref="byte"/></returns>
		public static byte GetClassification(this LocationType locationType) => (byte)(((byte)locationType) & ClassificationMask);
	}

	/// <summary>
	/// The enum for the type of the description the combination of storage locations
	/// </summary>
	public enum CombinationType : short
	{
		/// <summary>
		/// Storage composed of only one storage location (pure) or a <b>set</b> of storage memory locations (mixed)
		/// </summary>
		/// <remarks>The storages with this type only </remarks>
		PureOrMixed = (0 << CombinationTypeExtension.ClassificationEnd) | CombinationTypeExtension.ClassCombined,
		/// <summary>
		/// Storage composed of several <b>ordered</b> storage locations
		/// </summary>
		Cached = (0 << CombinationTypeExtension.ClassificationEnd) | CombinationTypeExtension.ClassOrdered,
	}

	/// <summary>
	/// Extension class for <see cref="CombinationType"/>
	/// </summary>
	public static class CombinationTypeExtension
	{
		/// <summary>
		/// The classification mask end bit of <see cref="CombinationType"/>
		/// </summary>
		public const int ClassificationEnd = 1;

		/// <summary>
		/// The classification mask of <see cref="CombinationType"/>
		/// </summary>
		public const int ClassificationMask = 0b1;

		/// <summary>
		/// The classification of combined (unordered) typed <see cref="CombinationType"/>
		/// </summary>
		/// <remarks>Other classifications are not supported.</remarks>
		public const int ClassCombined = 0b0;

		/// <summary>
		/// The classification of ordered typed <see cref="CombinationType"/>
		/// </summary>
		/// <remarks>Other classifications are not supported.</remarks>
		public const int ClassOrdered = 0b1;

		/// <summary>
		/// Get a <see cref="bool"/> indicating whether the given <see cref="CombinationType"/> is an ordered one or a unordered one.
		/// </summary>
		/// <param name="combinationType">The given <see cref="CombinationType"/></param>
		/// <returns>Whether the given <see cref="CombinationType"/> is an ordered one or a unordered one</returns>
		public static bool IsOrdered(this CombinationType combinationType) => ((short)combinationType).IsBitSet(0);
	}
	#endregion


	#region storage location structures
	/// <summary>
	/// The struct of a storage location
	/// </summary>
	/// <remarks>
	/// This struct has size of a <see cref="int"/>. The <see cref="LocationType"/> occupies first few bits and its detail occupies the rest (slightly smaller than a full <see cref="int"/>).
	/// </remarks>
	public readonly struct StorageLocation : IEquatable<StorageLocation>
	{
		#region basic
		private readonly int _data;

		/// <summary>
		/// The location type of this <see cref="StorageLocation"/> as a <see cref="LocationType"/>
		/// </summary>
		public LocationType Type => (LocationType)unchecked((byte)(this._data & byte.MaxValue));

		/// <summary>
		/// The location detail of the <see cref="Type"/>.
		/// </summary>
		public int LocationDetail => this._data >> 8;

		/// <summary>
		/// Create with given location and device ID
		/// </summary>
		/// <param name="location">The location of this <see cref="StorageLocation"/>, must be a flag</param>
		/// <param name="detail">The detail of <paramref name="location"/>: a <see cref="Althea.Storage.UriScheme"/> for a <see cref="LocationType.Uri"/> or a device ID otherwise. The largest 8 bits must be empty.</param>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="detail"/> is too large to fit with <see cref="LocationType"/></exception>
		public StorageLocation(LocationType location, int detail)
		{
			if (detail < 0 || detail >= 0xffffff)
				throw new ArgumentOutOfRangeException(nameof(detail), Parameter.InvalidValue);
			this._data = (byte)location + (detail << 8);
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
			return this._data == other._data;
		}

		/// <summary>
		/// Override <see cref="ValueType.Equals(object?)"/> to check whether this == <paramref name="obj"/>
		/// </summary>
		/// <param name="obj">another object to compare</param>
		/// <returns>this == <paramref name="obj"/></returns>
		public override bool Equals(object? obj)
		{
			return obj is StorageLocation storageDetail && this.Equals(storageDetail);
		}

		/// <summary>
		/// Override <see cref="ValueType.GetHashCode"/> to get the hash code this <see cref="StorageLocation"/>.
		/// </summary>
		/// <returns>The hash code</returns>
		public override int GetHashCode()
		{
			return this._data;
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
		private static readonly Dictionary<LocationType, string> static_OtherLocationNames = new Dictionary<LocationType, string>();
		private static readonly Dictionary<LocationType, Func<int, KeyValuePair<string, string>>> static_OtherDetailNames = new Dictionary<LocationType, Func<int, KeyValuePair<string, string>>>();

		/// <summary>
		/// Set the name of <see cref="Type"/> used for <see cref="ToString"/> of this if it represents a storage position in other types like <see cref="LocationType.OtherRam_0"/>
		/// </summary>
		/// <param name="location">The <see cref="LocationType"/> of a storage position in other types</param>
		/// <param name="name">The name as a <see cref="string"/> to set; notice that all the spaces will be replaced by '_'</param>
		/// <returns>success or not</returns>
		public static bool SetOtherLocationName(LocationType location, string name)
		{
			if (location < LocationType.OtherRam_0)
				return false;
			static_OtherLocationNames[location] = name.Replace(' ', '_');
			return true;
		}

		/// <summary>
		/// Set the name of <see cref="LocationDetail"/> used for <see cref="ToString"/> of this if it represents a storage position in other types like <see cref="LocationType.OtherRam_0"/>
		/// </summary>
		/// <param name="location">The <see cref="LocationType"/> of a storage position in other types</param>
		/// <param name="nameFunc">The name function as a <see cref="Func{T, TResult}"/> to set. The input value is <see cref="LocationDetail"/> and the output is a <see cref="KeyValuePair{TKey, TValue}"/> of <see cref="string"/>s to represent the name and value</param>
		/// <returns>success or not</returns>
		public static bool SetOtherDetailName(LocationType location, Func<int, KeyValuePair<string, string>> nameFunc)
		{
			if (location < LocationType.OtherRam_0)
				return false;
			static_OtherDetailNames[location] = nameFunc;
			return true;
		}

		/// <summary>
		/// Return the string representation of this <see cref="StorageLocation"/>
		/// </summary>
		/// <returns>the string representation of this <see cref="StorageLocation"/></returns>
		public override string ToString()
		{
			var str = this.Type switch
			{
				LocationType.Uri => "URI",
				LocationType.CpuRam => "CPU_Memory",
				LocationType.GpuRam => "GPU_Memory",
				_ => static_OtherLocationNames.GetValueOrDefault(this.Type) ?? $"Other_Memory_{this.Type - LocationType.OtherRam_0}",
			};
			if (this.Type == LocationType.Uri)
			{
				str += $"(scheme={Storage.UriSchemeExtension.GetName((Storage.UriScheme)this.LocationDetail)})";
			}
			else if (this.Type < LocationType.OtherRam_0)
			{
				str += $"(device_ID={this.LocationDetail})";
			}
			else
			{
				var kv = static_OtherDetailNames[this.Type](this.LocationDetail);
				str += $"({kv.Key}={kv.Value})";
			}
			return str;
		}
		#endregion
	}

	/// <summary>
	/// The struct of a combination of storage location(s)
	/// </summary>
	/// <remarks>
	/// The size of this structure is 64, which is exactly inside the cache line boundaries for Intel CPUs.<br/>
	/// The data field of this struct is a <see cref="FixedBuffer_60{T}"/> rather than an array of <see cref="StorageLocation"/> to reduce GC pressure since this structure is frequently created.<br/>
	/// In fact, nearly no GC is necessary even if some reference type has field of this <see cref="CombinationOfLocations"/>.
	/// </remarks>
	public readonly struct CombinationOfLocations : IEquatable<CombinationOfLocations>, IReadOnlyList<StorageLocation>
	{
		#region basic
		private readonly FixedBuffer_60<StorageLocation> data;

		private readonly CombinationType type; // size = 2

		private readonly ushort count;

		/// <summary>
		/// The number of <see cref="StorageLocation"/>s in this description
		/// </summary>
		public int Count => this.count;

		/// <summary>
		/// The <see cref="CombinationType"/> of this <see cref="CombinationOfLocations"/>
		/// </summary>
		public CombinationType Type => this.type;

		/// <summary>
		/// Create a <see cref="CombinationOfLocations"/> with given <see cref="CombinationType"/> (whether <paramref name="type"/> represents a set or a list is defined inside), and a <see cref="ReadOnlySpan{T}"/> containing the actual data
		/// </summary>
		/// <param name="type">The <see cref="CombinationType"/></param>
		/// <param name="data">A <see cref="ReadOnlySpan{T}"/> of <see cref="StorageLocation"/> containing the actual storage details, must has length between 1 and 15</param>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="data"/> has incompatible size</exception>
		public CombinationOfLocations(CombinationType type, ReadOnlySpan<StorageLocation> data)
		{
			if (data.Length > 15 || data.Length <= 0)
				throw new ArgumentOutOfRangeException(nameof(data), Parameter.WrongSize);
			// initialize
			this.type = type;
			this.data = new FixedBuffer_60<StorageLocation>();
			this.count = (ushort)data.Length;
			// set the values of data
			this.data.CopyFromSpan(data);
		}

		/// <summary>
		/// Create a <see cref="CombinationOfLocations"/> with given <see cref="CombinationType"/> (whether <paramref name="type"/> represents a set or a list is defined inside), and an array containing the actual data
		/// </summary>
		/// <param name="type">The <see cref="CombinationType"/></param>
		/// <param name="data">An array of <see cref="StorageLocation"/> containing the actual storage details, must has length between 1 and 15</param>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="data"/> has incompatible size</exception>
		public CombinationOfLocations(CombinationType type, params StorageLocation[] data) : this(type, (ReadOnlySpan<StorageLocation>)data) { }

		/// <summary>
		/// Create a <see cref="CombinationOfLocations"/> from a single <see cref="StorageLocation"/>
		/// </summary>
		/// <param name="memoryLocation">The given <see cref="StorageLocation"/></param>
		public CombinationOfLocations(StorageLocation memoryLocation)
		{
			this.type = CombinationType.PureOrMixed;
			this.data = new FixedBuffer_60<StorageLocation>();
			this.data[0] = memoryLocation;
			this.count = 1;
		}
		#endregion

		#region equality
		/// <summary>
		/// Whether this == <paramref name="other"/>
		/// </summary>
		/// <param name="other">another <see cref="CombinationOfLocations"/> to compare</param>
		/// <returns>this == <paramref name="other"/></returns>
		public bool Equals(CombinationOfLocations other)
		{
			return this.type == other.type && this.data == other.data;
		}

		/// <summary>
		/// Override <see cref="ValueType.Equals(object?)"/> to check whether this == <paramref name="obj"/>
		/// </summary>
		/// <param name="obj">another object to compare</param>
		/// <returns>this == <paramref name="obj"/></returns>
		public override bool Equals(object? obj)
		{
			return obj is CombinationOfLocations descr && this.Equals(descr);
		}

		/// <summary>
		/// Override <see cref="ValueType.GetHashCode"/> to get the hash code this <see cref="CombinationOfLocations"/>.
		/// </summary>
		/// <returns>The hash code</returns>
		public override int GetHashCode()
		{
			Span<StorageLocation> span = stackalloc StorageLocation[this.count];
			this.data.CopyToSpan(span);
			ReadOnlySpan<StorageLocation> s = span;
			if (this.type.IsOrdered())
				return HashCode.Combine(this.type, s.HashCodeOfSpan());
			else
				return HashCode.Combine(this.type, s.HashCodeOfSet());
		}

		/// <summary>
		/// Equality operator
		/// </summary>
		public static bool operator ==(CombinationOfLocations left, CombinationOfLocations right)
		{
			return left.Equals(right);
		}

		/// <summary>
		/// Inequality operator
		/// </summary>
		public static bool operator !=(CombinationOfLocations left, CombinationOfLocations right)
		{
			return !(left == right);
		}
		#endregion

		#region index
		/// <summary>
		/// Basic indexer of this <see cref="CombinationOfLocations"/>
		/// </summary>
		/// <param name="index">The index</param>
		/// <returns>The element at <paramref name="index"/> as a <see cref="StorageLocation"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="index"/> is out of range</exception>
		public StorageLocation this[int index] => index >= 0 && index < this.Count ? this.data[index] : throw new ArgumentOutOfRangeException(nameof(index));

		/// <summary>
		/// Forms a slice out of the current <see cref="CombinationOfLocations"/> starting at a specified <paramref name="start"/> for a specified <paramref name="length"/>.
		/// </summary>
		/// <param name="start">The index at which to begin this slice.</param>
		/// <param name="length">The desired length for the slice.</param>
		/// <returns>A <see cref="CombinationOfLocations"/> that consists of <see cref="StorageLocation"/>s composed of <paramref name="length"/> elements from the <paramref name="start"/> and the same <see cref="Type"/>.</returns>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="start"/> and/or <paramref name="length"/> exceeds the boundary of this <see cref="CombinationOfLocations"/></exception>
		public CombinationOfLocations Slice(int start, int length)
		{
			if (start < 0 || start >= this.Count)
				throw new ArgumentOutOfRangeException(nameof(start));
			if (length <= 0 || length + start > this.Count)
				throw new ArgumentOutOfRangeException(nameof(length));

			Span<StorageLocation> locations = stackalloc StorageLocation[this.count];
			this.CopyLocationsToSpan(locations);
			return new CombinationOfLocations(this.type, locations.Slice(start, length));
		}

		/// <summary>
		/// Forms a slice out of the current <see cref="CombinationOfLocations"/> starting at a specified <paramref name="start"/> to the end.
		/// </summary>
		/// <param name="start">The index at which to begin this slice.</param>
		/// <returns>A <see cref="CombinationOfLocations"/> that consists of <see cref="StorageLocation"/>s composed of (<see cref="Count"/> - <paramref name="start"/>) elements from the <paramref name="start"/> and the same <see cref="Type"/>.</returns>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="start"/> exceeds the boundary of this <see cref="CombinationOfLocations"/></exception>
		public CombinationOfLocations Slice(int start) => this[start..];

		IEnumerator<StorageLocation> IEnumerable<StorageLocation>.GetEnumerator()
		{
			for (int i = 0; i < this.Count; i++)
			{
				yield return this.data[i];
			}
		}

		IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<StorageLocation>)this).GetEnumerator();
		#endregion

		#region conversion
		/// <summary>
		/// Implicitly convert a <see cref="StorageLocation"/> to a <see cref="CombinationOfLocations"/>
		/// </summary>
		/// <param name="storageDetail">The <see cref="StorageLocation"/> to be converted</param>
		public static implicit operator CombinationOfLocations(StorageLocation storageDetail) => new CombinationOfLocations(storageDetail);

		/// <summary>
		/// Copy the <see cref="StorageLocation"/>s of this combination to a given <paramref name="span"/>
		/// </summary>
		/// <param name="span">The given <see cref="Span{T}"/> of <see cref="StorageLocation"/> to copy to</param>
		/// <exception cref="ArgumentException">If <paramref name="span"/>'s length is not the same as <see cref="Count"/></exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void CopyLocationsToSpan(Span<StorageLocation> span)
		{
			if (span.Length != this.count)
				throw new ArgumentException(Parameter.NotSameSize, nameof(span));
			this.data.CopyToSpan(span);
		}
		#endregion

		#region string related
		/// <summary>
		/// Return the string representation of this <see cref="CombinationOfLocations"/>
		/// </summary>
		/// <returns>the string representation of this <see cref="CombinationOfLocations"/></returns>
		public override string ToString()
		{
			return $"{nameof(CombinationOfLocations)}[type={this.type}, data={string.Join(", ", this)}]";
		}
		#endregion
	}
	#endregion


	#region pointer

	#region interface
	/// <summary>
	/// The interface for an immutable pointer which can be read, overwritten and positioned at any possible storage location, including any type of memory and any scheme of URI.
	/// </summary>
	public interface IPointer : IMainPropertyFormat, ICheckValid, IEquatable<IPointer>
	{
		/// <summary>
		/// When implemented by derived classes, get the <see cref="StorageLocation"/> of this pointer's underlying storage
		/// </summary>
		StorageLocation Location { get; }

		/// <summary>
		/// When implemented by derived classes, get the original (native) length of this pointer's underlying storage in bytes
		/// </summary>
		long LengthInBytes { get; }

		/// <summary>
		/// When implemented by derived classes, get the hash code this <see cref="IPointer"/>.
		/// </summary>
		/// <returns>The hash code</returns>
		int GetHashCode();
	}
	#endregion

	/// <summary>
	/// The struct of which delimits a certain section of a certain unmanaged memory block
	/// </summary>
	/// <remarks>This struct is <b>not</b> responsible for releasing unmanaged memories. It is only used for storing information of memory blocks.</remarks>
	public readonly struct PointerSegment : IEquatable<PointerSegment>, IMainPropertyFormat, ICheckValid
	{
		#region basic
		private readonly IPointer pointer;
		
		private readonly long offset;

		private readonly long length;

		/// <summary>
		/// Check whether this pointer is a valid pointer or not
		/// </summary>
		public bool IsValid() => this.length > 0 && this.pointer is not null && this.pointer.IsValid();

		/// <summary>
		/// The <see cref="StorageLocation"/> of this <see cref="PointerSegment"/>
		/// </summary>
		public StorageLocation Location => this.pointer?.Location ?? default;

		/// <summary>
		/// The native pointer (without offset and presenting length) as a <see cref="IPointer"/>
		/// </summary>
		public IPointer Pointer => this.pointer;

		/// <summary>
		/// The offset in bytes to the <see cref="Pointer"/> of this <see cref="PointerSegment"/>
		/// </summary>
		public long OffsetInBytes => this.offset;

		/// <summary>
		/// The <b>presenting</b> length in bytes of this <see cref="PointerSegment"/>
		/// </summary>
		public long LengthInBytes => this.length;

		/// <summary>
		/// Create with given pointer
		/// </summary>
		/// <param name="pointer">The pointer</param>
		/// <exception cref="ArgumentNullException">If <paramref name="pointer"/> is not a valid value</exception>
		public PointerSegment(IPointer pointer)
		{
			if (pointer is null || !pointer.IsValid())
				throw new ArgumentNullException(nameof(pointer));

			this.pointer = pointer; this.offset = 0; this.length = pointer.LengthInBytes;
		}

		/// <summary>
		/// Create with given <see cref="PointerSegment"/> <paramref name="storage"/> and <paramref name="offset"/> and <paramref name="newLength"/> to the <paramref name="storage"/>
		/// </summary>
		/// <param name="storage">The <see cref="PointerSegment"/> to copy info from</param>
		/// <param name="offset">The offset to the <paramref name="storage"/> in bytes</param>
		/// <param name="newLength">The new presenting length in bytes, default 0 means automatically calculating from <paramref name="offset"/> and <see cref="IPointer.LengthInBytes"/>. A value less than or equals to 0 means automatically calculate.</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offset"/> or <paramref name="newLength"/> exceeds the boundary</exception>
		public PointerSegment(PointerSegment storage, long offset = 0, long newLength = 0)
		{
			offset += storage.offset;
			if (offset < 0)
				throw new ArgumentOutOfRangeException(nameof(offset));
			long off = offset;
			if (off > storage.pointer.LengthInBytes)
				throw new ArgumentOutOfRangeException(nameof(offset));
			if (newLength <= 0)
				newLength = storage.pointer.LengthInBytes - off;
			if (off + newLength > storage.pointer.LengthInBytes)
				throw new ArgumentOutOfRangeException(nameof(newLength));

			this.pointer = storage.pointer; this.offset = off; this.length = newLength;
		}

		/// <summary>
		/// Create a new <see cref="PointerSegment"/> with given <paramref name="offset"/>
		/// </summary>
		/// <param name="offset">The offset in bytes to move</param>
		/// <param name="newLength">The new length in bytes to set, default 0 means auto calculation from <paramref name="offset"/>. A value less than or equals to 0 means automatically calculate.</param>
		/// <returns>The new <see cref="PointerSegment"/> moved from this pointer by <paramref name="offset"/> bytes and set the new presenting length to <paramref name="newLength"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offset"/> or <paramref name="newLength"/> exceeds the boundary</exception>
		public PointerSegment MoveBy(long offset, long newLength = 0) => offset == 0 && newLength <= 0 ? this : new PointerSegment(this, offset, newLength);

		/// <summary>
		/// Create a new <see cref="PointerSegment"/> with given <paramref name="newLength"/>
		/// </summary>
		/// <param name="newLength">The new length in bytes to set</param>
		/// <returns>The new <see cref="PointerSegment"/> with same pointer and offset while length is set to <paramref name="newLength"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="newLength"/> exceeds the boundary</exception>
		public PointerSegment AsLength(long newLength) => newLength == this.length ? this : new PointerSegment(this, 0, newLength);
		#endregion

		#region equality
		/// <summary>
		/// Check whether this <see cref="PointerSegment"/> overlaps with the <paramref name="other"/> <see cref="PointerSegment"/>
		/// </summary>
		/// <param name="other">The other <see cref="PointerSegment"/> to check overlap</param>
		/// <returns>True if this overlaps with the <paramref name="other"/>, false otherwise</returns>
		public bool OverlapWith(PointerSegment other) => this.pointer.Equals(other.pointer) && (this.offset > other.offset + other.length || other.offset > this.offset + this.length);

		/// <summary>
		/// Whether this == <paramref name="other"/>
		/// </summary>
		/// <param name="other">another <see cref="PointerSegment"/> to compare</param>
		/// <returns>this == <paramref name="other"/></returns>
		public bool Equals(PointerSegment other)
		{
			return this.pointer.Equals(other.pointer) && this.offset == other.offset;
		}

		/// <summary>
		/// Override <see cref="ValueType.Equals(object?)"/> to check whether this == <paramref name="obj"/>
		/// </summary>
		/// <param name="obj">another object to compare</param>
		/// <returns>this == <paramref name="obj"/></returns>
		public override bool Equals(object? obj)
		{
			return obj is PointerSegment storage && this.Equals(storage);
		}

		/// <summary>
		/// Override <see cref="ValueType.GetHashCode"/> to get the hash code this <see cref="PointerSegment"/>.
		/// </summary>
		/// <returns>The hash code</returns>
		public override int GetHashCode()
		{
			return HashCode.Combine(this.pointer.GetHashCode(), this.offset);
		}

		/// <summary>
		/// Equality operator
		/// </summary>
		public static bool operator ==(PointerSegment left, PointerSegment right)
		{
			return left.Equals(right);
		}

		/// <summary>
		/// Inequality operator
		/// </summary>
		public static bool operator !=(PointerSegment left, PointerSegment right)
		{
			return !(left == right);
		}
		#endregion

		#region to string
		string IMainPropertyFormat.StringMain => this.pointer.StringMain;

		IReadOnlyDictionary<string, string> IMainPropertyFormat.StringProperties => this.offset == 0 ? new Dictionary<string, string>
		{
			["location"] = this.pointer.Location.ToString(),
			["length_bytes"] = this.length.ToString(),
		} : new Dictionary<string, string>
		{
			["location"] = this.pointer.Location.ToString(),
			["offset_bytes"] = this.offset.ToString(),
			["length_bytes"] = this.length.ToString(),
		};

		/// <summary>
		/// Return the string representation of this <see cref="PointerSegment"/>
		/// </summary>
		/// <returns>the string representation of this <see cref="PointerSegment"/></returns>
		public override string? ToString() => ((IMainPropertyFormat)this).ToString();
		#endregion

		#region operator
		/// <summary>
		/// Add offset (in bytes) to a <see cref="PointerSegment"/> to get another.
		/// </summary>
		/// <param name="storage">The <see cref="PointerSegment"/></param>
		/// <param name="offset">The offset of type <see cref="long"/></param>
		/// <returns>a <see cref="Storage{T}"/> with <paramref name="offset"/> added to the pointer</returns>
		public static PointerSegment operator +(PointerSegment storage, long offset) => offset == 0 ? storage : new PointerSegment(storage, offset);

		/// <summary>
		/// Subtract offset (in bytes) to a <see cref="PointerSegment"/> to get another.
		/// </summary>
		/// <param name="storage">The <see cref="PointerSegment"/></param>
		/// <param name="offset">The offset of type <see cref="long"/></param>
		/// <returns>a <see cref="Storage{T}"/> with <paramref name="offset"/> added to the pointer</returns>
		public static PointerSegment operator -(PointerSegment storage, long offset) => offset == 0 ? storage : new PointerSegment(storage, -offset);

		/// <summary>
		/// Get the pointer's difference (in bytes) of two <see cref="PointerSegment"/>s.
		/// </summary>
		/// <param name="left">The left <see cref="PointerSegment"/></param>
		/// <param name="right">The right <see cref="PointerSegment"/></param>
		/// <returns>If <paramref name="left"/> and <paramref name="right"/> have different references, return <see cref="long.MinValue"/>; otherwise, return a <see cref="long"/> as the difference between the <see cref="Pointer"/>s of <paramref name="left"/> and <paramref name="right"/></returns>
		public static long operator -(PointerSegment left, PointerSegment right) => left.Location != right.Location || !left.pointer.Equals(right.pointer) ? long.MinValue : left.offset - right.offset;
		#endregion
	}
	#endregion


	#region storage classes

	#region interfaces
	/// <summary>
	/// The interface for wrapper of unmanaged memory block(s) of different <see cref="StorageLocation"/>(s) of any data type
	/// </summary>
	/// <remarks>This interface exists only because it is necessary for a data type cast operation to be conducted without copying. You shall <b>NOT</b> implement this interface; implement <see cref="Storage{T}"/> instead.</remarks>
	public interface IStorage : IReadOnlyList<PointerSegment>, IEquatable<IStorage>, ICheckValid, IDisposable, IMainPropertyFormat
	{
		/// <summary>
		/// The total length of the presenting array in bytes
		/// </summary>
		long LengthInBytes { get; }

		/// <summary>
		/// When implemented by a derived class, get the total length of the presenting array in its presenting type (rather than bytes)
		/// </summary>
		long Length { get; }

		/// <summary>
		/// The description of the storage locations of this <see cref="Storage{T}"/> class as a <see cref="CombinationOfLocations"/>
		/// </summary>
		CombinationOfLocations LocationDescription { get; }

		/// <summary>
		/// Check whether this <see cref="Storage{T}"/> is valid or not after moving an <paramref name="offset"/> and set <see cref="LengthInBytes"/> to <paramref name="newLength"/>
		/// </summary>
		/// <param name="offset">The offset to move in bytes</param>
		/// <param name="newLength">The length to check in bytes, default 0 means auto calculation by <paramref name="offset"/></param>
		/// <returns>The validness of this <see cref="Storage{T}"/> under <paramref name="offset"/> and <paramref name="newLength"/></returns>
		bool IsOffsetValid(long offset, long newLength = 0);

		/// <summary>
		/// Check the given storage and throw exception if check failed.
		/// </summary>
		/// <param name="offset">The offset to move in bytes</param>
		/// <param name="length">The length to check in bytes, default 0 means auto calculation by <paramref name="offset"/></param>
		/// <exception cref="ArgumentException">if this <see cref="Storage{T}"/> has invalid value</exception>
		/// <exception cref="ArgumentOutOfRangeException">if offset and length breach the boundary of this <see cref="Storage{T}"/></exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Check(long offset = 0, long length = 0)
		{
			if (!this.IsValid())
				throw new ArgumentException(Parameter.InvalidValue);
			if ((offset != 0 || length != 0) && !this.IsOffsetValid(offset, length))
				throw new ArgumentOutOfRangeException($"{nameof(offset)}, {nameof(length)}");
		}
	}

	/// <summary>
	/// The interface for a referenced storage of <see cref="IStorage"/>
	/// </summary>
	/// <remarks>This interface exists only because it is necessary for a data type cast operation to be conducted without copying. You shall <b>NOT</b> implement this interface; implement <see cref="ReferenceStorage{T}"/> instead.</remarks>
	public interface IReferenceStorage : IStorage
	{
		/// <summary>
		/// The referenced storage as a nullable <see cref="IStorage"/>
		/// </summary>
		IStorage? Reference { get; }

		/// <summary>
		/// The total offset compared to the start of <see cref="Reference"/> in bytes
		/// </summary>
		long TotalOffsetInBytes { get; }
	}
	#endregion

	/// <summary>
	/// The abstract wrapper class of unmanaged memory block(s) of different <see cref="StorageLocation"/>(s).
	/// </summary>
	/// <typeparam name="T">any unmanaged data type</typeparam>
	/// <remarks>
	/// I must warn you that although .NET has GC to periodically collect unused garbage to prevent memory leak, you should not rely on it too much. <b>Remember</b> to use <c>using</c> statement or call <see cref="Storage{T}.Dispose()"/>.<br/>
	/// The leaked memory which will be collected GC still causes not only performance loss but also potential bugs if you do not know how GC works.<br/>
	/// See https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/ for official documentations of GC of .NET.</remarks>
	public abstract class Storage<T> : IStorage, IEquatable<Storage<T>>, ICloneable<Storage<T>> where T : unmanaged
	{
		#region properties
		/// <summary>
		/// Get the size of <typeparamref name="T"/> in memory in bytes
		/// </summary>
		public static readonly unsafe int SizeOfT = sizeof(T);

		/// <summary>
		/// Get an empty <see cref="Storage{T}"/>
		/// </summary>
		public static readonly Storage<T> Empty = new Storage.PureOrMixedReferenceStorage<T>();

		/// <summary>
		/// When implemented by a derived class, get the total length of the presenting array in <typeparamref name="T"/> (rather than bytes)
		/// </summary>
		public abstract long Length { get; }

		/// <summary>
		/// When implemented by a derived class, get the total length of the presenting array in bytes. The default implementation returns the multiplication of <see cref="Length"/> and <see cref="SizeOfT"/>.
		/// </summary>
		public virtual long LengthInBytes => this.Length * SizeOfT;

		/// <summary>
		/// When implemented by a derived class, get the description of the storage locations of this <see cref="Storage{T}"/> class as a <see cref="CombinationOfLocations"/>
		/// </summary>
		public abstract CombinationOfLocations LocationDescription { get; }

		/// <summary>
		/// When implemented by a derived class, get the number of <see cref="PointerSegment"/>(s) of this <see cref="Storage{T}"/> 
		/// </summary>
		public abstract int Count { get; }

		/// <summary>
		/// When implemented by a derived class, get one of the <see cref="PointerSegment"/> of this <see cref="Storage{T}"/> (in presenting order)
		/// </summary>
		/// <param name="index">The element index</param>
		/// <returns>the <see cref="PointerSegment"/> at <paramref name="index"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="index"/> is out of the range</exception>
		public abstract PointerSegment this[int index] { get; }
		#endregion

		#region dispose
		/// <summary>
		/// Is this <see cref="Storage{T}"/> disposed or not
		/// </summary>
		protected bool Disposed { get; private set; } = false;

		/// <summary>
		/// Dispose the unmanaged and managed resources held by this <see cref="Storage{T}"/>
		/// </summary>
		public void Dispose()
		{
			if (this.IsValid())
				this.Dispose(true);
			this.Disposed = true;
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// When implemented by a derived class, actually unmanaged (and possibly managed) resources held by this <see cref="Storage{T}"/>
		/// </summary>
		/// <param name="disposeManaged">dispose managed resources or not</param>
		protected abstract void Dispose(bool disposeManaged);
		#endregion

		#region other methods
		/// <summary>
		/// When implemented by a derived class, check whether this <see cref="Storage{T}"/> has same origin with the <paramref name="other"/> <see cref="Storage{T}"/>. The default implementation only works when both this and <paramref name="other"/> are <see cref="ActualStorage{T}"/> or <see cref="ReferenceStorage{T}"/>.
		/// </summary>
		/// <param name="other">The other <see cref="Storage{T}"/> to check overlap</param>
		/// <returns>True if this storage has same origin with the <paramref name="other"/>, false otherwise</returns>
		/// <exception cref="NotImplementedException">If either of this and <paramref name="other"/> is neither <see cref="ActualStorage{T}"/> nor <see cref="ReferenceStorage{T}"/></exception>
		public virtual bool SameOriginAs(Storage<T> other)
		{
			if (!this.IsValid() || !other.IsValid())
				return false;

			var originThis = this as ActualStorage<T> ?? (this as ReferenceStorage<T>)?.Reference;
			var originOther = other as ActualStorage<T> ?? (other as ReferenceStorage<T>)?.Reference;
			if (originThis is null || originOther is null)
				throw new NotImplementedException();
			return originThis.Equals(originOther);
		}

		/// <summary>
		/// When implemented by a derived class, check whether this <see cref="Storage{T}"/> overlaps with the <paramref name="other"/> <see cref="Storage{T}"/>. The default implementation is direct if both this and <paramref name="other"/> are <see cref="ActualStorage{T}"/> or <see cref="ReferenceStorage{T}"/>; otherwise, it assumes that only <see cref="PointerSegment"/>s visible from <see cref="this[int]"/> can be referenced.
		/// </summary>
		/// <param name="other">The other <see cref="Storage{T}"/> to check overlap</param>
		/// <returns>True if this overlaps with the <paramref name="other"/>, false otherwise</returns>
		public virtual bool OverlapWith(Storage<T> other)
		{
			if (!this.IsValid() || !other.IsValid())
				return false;

			var actualThis = this as ActualStorage<T>;
			var actualOther = other as ActualStorage<T>;
			var referenceThis = this as ReferenceStorage<T>;
			var referenceOther = other as ReferenceStorage<T>;

			if (actualThis is not null && actualOther is not null)
				return actualThis.Equals(actualOther);
			else if (actualThis is not null && referenceOther is not null)
				return actualThis.Equals(referenceOther.Reference);
			else if (referenceThis is not null && actualOther is not null)
				return actualOther.Equals(referenceThis.Reference);
			else if (referenceThis is not null && referenceOther is not null)
				return referenceThis.Reference is not null && referenceOther is not null && referenceThis.Reference.Equals(referenceOther.Reference);

			// else
			for (int i = 0; i < this.Count; i++)
			{
				for (int j = 0; j < other.Count; j++)
				{
					if (this[i].OverlapWith(other[j]))
						return true;
				}
			}
			return false;
		}

		Storage<T> ICloneable<Storage<T>>.Clone() => this.Clone();

		/// <summary>
		/// When implemented by a derived class, allocate and creates a new <see cref="Storage{T}"/> that is a copy of the current one. The default implementation utilizes <see cref="Althea.Storage.StorageFactory{T}.CreateAlike(Storage{T})"/> and <see cref="MEM.MemoryCopy{T}(Storage{T}, Storage{T})"/>.
		/// </summary>
		/// <returns>A new <see cref="Storage{T}"/> that is a copy of the current instance</returns>
		public virtual ActualStorage<T> Clone()
		{
			var storage = Storage.StorageFactory<T>.CreateAlike(this);
			try
			{
				MEM.MemoryCopy(this, storage);
				return storage;
			}
			catch (System.Exception)
			{
				storage?.Dispose();
				throw;
			}
		}

		/// <summary>
		/// <b>Allocate</b> and create a new <see cref="Storage{T}"/> of given <paramref name="combinationType"/> and given locations and lengths. This implementation utilizes the <see cref="Storage.StorageFactory{T}"/>.
		/// </summary>
		/// <param name="combinationType">The given <see cref="CombinationType"/> to create</param>
		/// <param name="locationsAndLengths">The given <see cref="StorageLocation"/>s and corresponding lengths in <typeparamref name="T"/></param>
		/// <returns>The created new <see cref="Storage{T}"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="locationsAndLengths"/> is null or empty</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="locationsAndLengths"/> has length(s) equals to 0</exception>
		/// <exception cref="OutOfMemoryException">If the underlying allocation failed due to insufficient memory</exception>
		/// <exception cref="InvalidOperationException">If underlying creation fails</exception>
		public static ActualStorage<T> Create(CombinationType combinationType, params (StorageLocation location, long length)[] locationsAndLengths)
		{
			if (locationsAndLengths is null || locationsAndLengths.Length <= 0)
				throw new ArgumentNullException(nameof(locationsAndLengths));

			if (locationsAndLengths.Length == 1)
			{
				if (locationsAndLengths[0].length <= 0)
					throw new ArgumentOutOfRangeException(nameof(locationsAndLengths), Parameter.MustPositive);
				return Create(locationsAndLengths[0].location, locationsAndLengths[0].length);
			}
			else
			{
				if (locationsAndLengths.Any(static p => p.length <= 0))
					throw new ArgumentOutOfRangeException(nameof(locationsAndLengths), Parameter.MustPositive);
				Span<StorageLocation> locations = stackalloc StorageLocation[locationsAndLengths.Length];
				Span<long> lengths = stackalloc long[locationsAndLengths.Length];
				for (int i = 0; i < locationsAndLengths.Length; i++)
				{
					locations[i] = locationsAndLengths[i].location;
					lengths[i] = locationsAndLengths[i].length;
				}
				return Storage.StorageFactory<T>.Create(combinationType, locations, lengths);
			}
		}

		/// <summary>
		/// <b>Allocate</b> and create a new <see cref="Storage{T}"/> on given <paramref name="location"/> with corresponding <paramref name="length"/>. This implementation utilizes the <see cref="Storage.StorageFactory{T}"/>.
		/// </summary>
		/// <param name="location">The given <see cref="StorageLocation"/> to create on</param>
		/// <param name="length">The corresponding length in <typeparamref name="T"/></param>
		/// <returns>The created new <see cref="Storage{T}"/></returns>
		/// <exception cref="InvalidOperationException">If underlying creation fails</exception>
		public static ActualStorage<T> Create(StorageLocation location, long length)
		{
			Span<StorageLocation> locations = stackalloc StorageLocation[1];
			locations.SetValue(location);
			Span<long> lengths = stackalloc long[1];
			lengths.SetValue(length);
			return Storage.StorageFactory<T>.Create(CombinationType.PureOrMixed, locations, lengths);
		}

		/// <summary>
		/// When implemented by a derived class, make a <see cref="ReferenceStorage{T}"/> with the starting pointer moving <paramref name="offset"/> and <see cref="Length"/> changing to <paramref name="newLength"/>.
		/// </summary>
		/// <param name="offset">The offset in <typeparamref name="T"/> to the starting pointer of this <see cref="Storage{T}"/> as a <see cref="long"/></param>
		/// <param name="newLength">The new length in <typeparamref name="T"/> as a <see cref="long"/>, default 0 means automatically calculate from <paramref name="offset"/></param>
		/// <returns>A <see cref="ReferenceStorage{T}"/> of this one</returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offset"/> and <paramref name="newLength"/> is out of boundary</exception>
		public abstract ReferenceStorage<T> MakeReference(long offset = 0, long newLength = 0);

		/// <summary>
		/// Check whether the given <paramref name="size"/> in <typeparamref name="T"/> can be casted without loss to <typeparamref name="TOut"/>
		/// </summary>
		/// <typeparam name="TOut">The output data type</typeparam>
		/// <param name="size">The size in <typeparamref name="T"/> to check</param>
		/// <param name="sizeInBytes">Whether <paramref name="size"/> is in bytes or in <typeparamref name="T"/></param>
		/// <returns>The <paramref name="size"/> (multiplies the size of <typeparamref name="T"/> then) divides the size of <typeparamref name="TOut"/></returns>
		/// <exception cref="InvalidCastException">if <paramref name="size"/>( multiplies the size of <typeparamref name="T"/>) cannot be divided by the size of <typeparamref name="TOut"/></exception>
		protected static long CheckCast<TOut>(long size, bool sizeInBytes = false) where TOut : unmanaged
		{
			long newSize = sizeInBytes ? size : (size * SizeOfT);
			if (size * SizeOfT % Storage<TOut>.SizeOfT != 0)
				throw new InvalidCastException(Other.CannotDivide);
			newSize /= Storage<TOut>.SizeOfT;
			return newSize;
		}

		/// <summary>
		/// When implemented by a derived class, convert this <see cref="Storage{T}"/> to another one with different data type <typeparamref name="TOut"/> without copying data.
		/// </summary>
		/// <typeparam name="TOut">The output data type</typeparam>
		/// <returns>A <see cref="ReferenceStorage{TOut}"/> of type <typeparamref name="TOut"/></returns>
		/// <exception cref="InvalidCastException">if <see cref="LengthInBytes"/> cannot be divided by the size of <typeparamref name="TOut"/></exception>
		public abstract ReferenceStorage<TOut> As<TOut>() where TOut : unmanaged;

		/// <summary>
		/// When implemented by a derived class, check whether this <see cref="Storage{T}"/> is valid or not. The default implementation checks the <see cref="Disposed"/>, <see cref="Count"/> and the <see cref="ICheckValid.IsValid"/> of each pointer.
		/// </summary>
		/// <returns>The validness of this <see cref="Storage{T}"/></returns>
		public virtual bool IsValid()
		{
			if (this.Disposed || this.Count == 0)
			{
				return false;
			}
			for (int i = 0; i < this.Count; i++)
			{
				var pointer = this[i];
				if (!pointer.IsValid())
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// When implemented by a derived class, check whether this <see cref="Storage{T}"/> is valid or not after moving an <paramref name="offset"/> and set <see cref="LengthInBytes"/> to <paramref name="newLength"/>. The default implementation works for both <see cref="ReferenceStorage{T}"/> and any non-referenced storage.
		/// </summary>
		/// <param name="offset">The offset to move</param>
		/// <param name="newLength">The length to check in bytes</param>
		/// <returns>The validness of this <see cref="Storage{T}"/> under <paramref name="offset"/> and <paramref name="newLength"/></returns>
		public virtual bool IsOffsetValid(long offset, long newLength = 0)
		{
			if (this is IReferenceStorage reference)
			{
				if (reference.Reference is null)
					return false;
				offset += reference.TotalOffsetInBytes;
				if (offset < 0 || offset >= reference.Reference.LengthInBytes)
					return false;
				if (newLength > 0 && newLength + offset >= reference.Reference.LengthInBytes)
					return false;
				return true;
			}
			else
			{
				if (offset < 0 || offset >= this.LengthInBytes)
					return false;
				if (newLength > 0 && newLength + offset >= this.LengthInBytes)
					return false;
				return true;
			}
		}
		#endregion

		#region enumerator
		IEnumerator<PointerSegment> IEnumerable<PointerSegment>.GetEnumerator()
		{
			for (int i = 0; i < this.Count; i++)
			{
				yield return this[i];
			}
		}
		IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<PointerSegment>)this).GetEnumerator();

		/// <summary>
		/// When implemented by a derived class, get the actual <see cref="PointerSegment"/> at the actual index (the index in <see cref="LocationDescription"/>) <paramref name="i"/>. The default implementation simply returns <see cref="this[int]"/>.
		/// </summary>
		/// <param name="i">The actual index</param>
		/// <returns>The actual <see cref="PointerSegment"/> at <paramref name="i"/></returns>
		internal protected virtual PointerSegment GetActualPointerAt(int i) => this[i];
		#endregion

		#region equality
		bool IEquatable<IStorage>.Equals(IStorage? other) => this.Equals(other as Storage<T>);

		/// <summary>
		/// Determines whether the specified object is equal to the current object.
		/// </summary>
		/// <param name="obj">another object</param>
		/// <returns>this equals to <paramref name="obj"/> or not</returns>
		public override bool Equals(object? obj)
		{
			return this.Equals(obj as Storage<T>);
		}

		/// <summary>
		/// When implemented by a derived class, determines whether the specified object is equal to the current object.
		/// </summary>
		/// <param name="obj">another object</param>
		/// <returns>this equals to <paramref name="obj"/> or not</returns>
		public abstract bool Equals(Storage<T>? obj);

		/// <summary>
		/// When implemented by a derived class, get the hash code of this <see cref="Storage{T}"/>
		/// </summary>
		/// <returns>the hash code</returns>
		public abstract override int GetHashCode();

		/// <summary>
		/// Equality operator
		/// </summary>
		public static bool operator ==(Storage<T>? left, Storage<T>? right)
		{
			if (left is not null)
				return left.Equals(right);
			else if (right is null)
				return true;
			else if (ReferenceEquals(left, right))
				return true;
			else
				return right.Equals(left);
		}

		/// <summary>
		/// Inequality operator
		/// </summary>
		public static bool operator !=(Storage<T>? left, Storage<T>? right) => !(left == right);
		#endregion

		#region string
		string IMainPropertyFormat.StringMain => this.Count == 1 ? ((IMainPropertyFormat)this[0]).StringMain : ('{' + string.Join(", ", this) + '}');

		IReadOnlyDictionary<string, string> IMainPropertyFormat.StringProperties => new Dictionary<string, string>
		{
			["type"] = typeof(T).Name,
			[this.Count == 1 ? "length" : "total_length"] = this.Length.ToString(),
		};

		/// <summary>
		/// When implemented by a derived class, get the string representation of this <see cref="Storage{T}"/>. The default implementation utilizes <see cref="IMainPropertyFormat.ToString()"/>
		/// </summary>
		/// <returns>The string representation</returns>
		public override string? ToString() => ((IMainPropertyFormat)this).ToString();
		#endregion

		#region operator
		/// <summary>
		/// Add offset (in bytes) to a <see cref="Storage{T}"/> to get another.
		/// </summary>
		/// <param name="storage">The <see cref="Storage{T}"/></param>
		/// <param name="offset">The offset of type <see cref="long"/></param>
		/// <returns>a <see cref="Storage{T}"/> with <paramref name="offset"/> added to the pointer</returns>
		public static Storage<T> operator +(Storage<T> storage, long offset) => storage.MakeReference(offset);

		/// <summary>
		/// Subtract offset (in bytes) to a <see cref="Storage{T}"/> to get another.
		/// </summary>
		/// <param name="storage">The <see cref="Storage{T}"/></param>
		/// <param name="offset">The offset of type <see cref="long"/></param>
		/// <returns>a <see cref="Storage{T}"/> with <paramref name="offset"/> added to the pointer</returns>
		public static Storage<T> operator -(Storage<T> storage, long offset) => storage.MakeReference(-offset);
		#endregion
	}


	/// <summary>
	/// The abstract storage class as a bas class for all referenced <see cref="Storage{T}"/> classes
	/// </summary>
	/// <typeparam name="T">any unmanaged data type</typeparam>
	public abstract class ReferenceStorage<T> : Storage<T>, IReferenceStorage where T : unmanaged
	{
		#region basic
		private readonly IStorage? reference;

		/// <summary>
		/// The referenced storage as a nullable <see cref="IStorage"/>
		/// </summary>
		public IStorage? Reference => this.reference;

		/// <summary>
		/// When implemented by a derived class, get the total offset compared to the start of the referenced <see cref="IStorage"/> in bytes. It is not counted in <typeparamref name="T"/> since there may be data type difference between the <see cref="IStorage"/> and this.
		/// </summary>
		public virtual long TotalOffsetInBytes { get; }

		/// <summary>
		/// When implemented by a derived class, get the total length of the presenting array in <typeparamref name="T"/> (rather than bytes)
		/// </summary>
		public override long Length { get; }

		/// <summary>
		/// Create a <see cref="ReferenceStorage{T}"/> with given reference <paramref name="storage"/> and <paramref name="offset"/> to it
		/// </summary>
		/// <param name="storage">The <see cref="Storage{T}"/> to be referenced</param>
		/// <param name="offset">The total offset in <typeparamref name="T"/> as a <see cref="long"/></param>
		/// <param name="newLength">The new presenting length in <typeparamref name="T"/>. A value less than or equals to 0 means the maximum possible value calculate from <paramref name="storage"/> and <paramref name="offset"/></param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offset"/> and <paramref name="newLength"/> is out of boundary</exception>
		protected ReferenceStorage(IStorage? storage, long offset = 0, long newLength = 0)
		{
			if (storage is null)
				return;
			// get offset and new length in bytes
			long offsetInBytes = offset * SizeOfT;
			long newLengthInBytes;
			if (newLength <= 0)
				newLengthInBytes = storage.LengthInBytes - SizeOfT * offset;
			else
				newLengthInBytes = newLength * SizeOfT;
			// dereference first
			while (storage is IReferenceStorage @ref)
			{
				if (@ref.Reference is null)
					return;
				storage = @ref.Reference;
				offsetInBytes += @ref.TotalOffsetInBytes;
			}
			// set reference
			this.reference = storage;
			// check
			if (offsetInBytes < 0)
				throw new ArgumentOutOfRangeException(nameof(offset));
			if (storage.LengthInBytes != offsetInBytes + newLengthInBytes)
				throw new ArgumentOutOfRangeException(nameof(newLength));
			// set offset and length
			this.TotalOffsetInBytes = offsetInBytes;
			this.Length = newLengthInBytes / SizeOfT;
		}
		#endregion

		#region override
		/// <summary>
		/// The function that actually dispose this <see cref="ReferenceStorage{T}"/>, override <see cref="Storage{T}.Dispose(bool)"/>
		/// </summary>
		/// <param name="disposeManaged">dispose managed resources or not</param>
		/// <remarks>Since this is a reference, this method does nothing</remarks>
		protected override void Dispose(bool disposeManaged) { }
		#endregion
	}


	/// <summary>
	/// The abstract storage class as a base class for all non-referenced <see cref="Storage{T}"/> classes
	/// </summary>
	/// <typeparam name="T">any unmanaged data type</typeparam>
	/// <remarks>Since this class has a finalizer, it cannot be in GC generation 0, i.e., it will not be disposed immediately when out of scope.</remarks>
	public abstract class ActualStorage<T> : Storage<T> where T : unmanaged
	{
		#region static
		/// <summary>
		/// Get an empty <see cref="ActualStorage{T}"/>
		/// </summary>
		public static new readonly ActualStorage<T> Empty = new Storage.PureStorage<T>(default, 0);
		#endregion

		#region memory
		/// <summary>
		/// The total length of the presenting array in <typeparamref name="T"/> (rather than bytes), override <see cref="Storage{T}.Length"/>
		/// </summary>
		public override long Length { get; }

		/// <summary>
		/// Create an <see cref="ActualStorage{T}"/> with given length of presenting array
		/// </summary>
		/// <param name="length">The length of presenting array <typeparamref name="T"/></param>
		protected ActualStorage(long length)
		{
			if (length <= 0)
				throw new ArgumentOutOfRangeException(nameof(length), Parameter.MustPositive);
			this.Length = length;
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
				if (ptr.IsValid())
					MEM.Free(ptr, disposeManaged);
			}
		}

		/// <summary>
		/// The finalizer of <see cref="ActualStorage{T}"/>
		/// </summary>
		~ActualStorage()
		{
			this.Dispose(false);
		}

		/// <summary>
		/// Allocate a <see cref="PointerSegment"/> of given <see cref="Storage{T}.Length"/> on given <see cref="StorageLocation"/> 
		/// </summary>
		/// <param name="location">a <see cref="StorageLocation"/> to represent the memory location</param>
		/// <param name="length">The length of contiguous memory block in <typeparamref name="T"/></param>
		/// <exception cref="OutOfMemoryException">If system cannot allocate <paramref name="length"/> on <paramref name="location"/></exception>
		protected static PointerSegment Allocate(StorageLocation location, long length)
		{
			return MEM.Allocate<T>(location, length);
		}
		#endregion
	}
	#endregion
}
