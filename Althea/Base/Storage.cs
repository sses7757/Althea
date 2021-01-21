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
		/// Storage composed of only one memory location (pure) or a <b>set</b> of several memory locations (mixed).
		/// </summary>
		PureOrMixed = 0 << CombinationTypeExtension.ClassificationEnd | CombinationTypeExtension.ClassUnordered,
		/// <summary>
		/// Storage composed of several <b>ordered</b> memory locations and a possible URI.
		/// </summary>
		Cached = 0 << CombinationTypeExtension.ClassificationEnd | CombinationTypeExtension.ClassOrdered,
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
		/// The classification of unordered typed <see cref="CombinationType"/>
		/// </summary>
		/// <remarks>Other classifications are not supported.</remarks>
		public const int ClassUnordered = 0b0;

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
		public static bool IsOrdered(this CombinationType combinationType) => ((int)combinationType).IsBitSet(0);
	}
	#endregion


	#region storage location structures
	/// <summary>
	/// The struct of a storage location
	/// </summary>
	/// <remarks>This struct has size of a <see cref="int"/>. The <see cref="LocationType"/> occupies first few bits and its detail occupies the rest (slightly smaller than a full <see cref="int"/>).</remarks>
	[StructLayout(LayoutKind.Sequential)]
	public readonly struct StorageLocation : IEquatable<StorageLocation>
	{
		#region basic
		private readonly int _data;

		/// <summary>
		/// The location of this <see cref="StorageLocation"/>
		/// </summary>
		public LocationType Location => (LocationType)unchecked((byte)this._data);

		/// <summary>
		/// The location detail of <see cref="Location"/>.
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
			if (detail < 0 || detail >= (byte.MaxValue + 1) * (ushort.MaxValue + 1))
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
		public override bool Equals(object obj)
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
		/// Set the name of <see cref="Location"/> used for <see cref="ToString"/> of this if it represents a storage position in other types like <see cref="LocationType.OtherRam_0"/>
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
			var str = this.Location switch
			{
				LocationType.Uri => "URI",
				LocationType.CpuRam => "CPU_Memory",
				LocationType.GpuRam => "GPU_Memory",
				_ => static_OtherLocationNames.GetValueOrDefault(this.Location) ?? $"Other_Memory_{this.Location - LocationType.OtherRam_0}",
			};
			if (this.Location == LocationType.Uri)
			{
				str += $"(scheme={(Storage.UriScheme)this.LocationDetail})";
			}
			else if (this.Location < LocationType.OtherRam_0)
			{
				str += $"(device_ID={this.LocationDetail})";
			}
			else
			{
				var kv = static_OtherDetailNames[this.Location](this.LocationDetail);
				str += $"({kv.Key}={kv.Value})";
			}
			return str;
		}
		#endregion
	}

	/// <summary>
	/// The struct of a combination of storage location(s)
	/// </summary>
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	public readonly struct CombinationOfLocations : IEquatable<CombinationOfLocations>, IReadOnlyList<StorageLocation>
	{
		#region basic
		private readonly CombinationType type; // size = 2

		private readonly ushort count;

		private readonly FixedBuffer_60<StorageLocation> data;

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
			Span<StorageLocation> span = stackalloc StorageLocation[data.Length];
			data.CopyTo(span);
			if (type.IsOrdered())
			{
				span.Sort();
			}
			for (int i = 0; i < 15; i++)
			{
				this.data[i] = span[i];
			}
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
		/// <param name="memoryLocation">the given <see cref="StorageLocation"/></param>
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
		public override bool Equals(object obj)
		{
			return obj is CombinationOfLocations descr && this.Equals(descr);
		}

		/// <summary>
		/// Override <see cref="ValueType.GetHashCode"/> to get the hash code this <see cref="CombinationOfLocations"/>.
		/// </summary>
		/// <returns>The hash code</returns>
		public override int GetHashCode()
		{
			if (this.type.IsOrdered())
				return HashCode.Combine(this.type, this.data);
			else
				return HashCode.Combine(this.type, ((ReadOnlySpan<StorageLocation>)this.data.AsSpan()).HashCodeOfSet());
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
		/// <param name="index">the index</param>
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

			return new CombinationOfLocations(this.type, this.data.AsSpan().Slice(start, length));
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
			var span = this.data.AsSpan();
			for (int i = 0; i < this.Count; i++)
			{
				yield return span[i];
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
	/// The interface for an immutable pointer at any possible storage location, including any type of memory and any scheme of URI.
	/// </summary>
	public interface IPointer : IMainPropertyFormat, ICheckValid
	{
		/// <summary>
		/// The original length of this pointer's underlying storage in bytes
		/// </summary>
		ulong LengthInBytes { get; }

		/// <summary>
		/// <b>Statically</b> check whether given <paramref name="location"/> is a supported one for this pointer
		/// </summary>
		/// <param name="location">The given <see cref="StorageLocation"/> to be checked</param>
		/// <returns>Whether given <paramref name="location"/> is supported or not</returns>
		bool IsValidLocation(StorageLocation location);

		/// <summary>
		/// Get a <see cref="bool"/> indicating whether this pointer can be read or not
		/// </summary>
		bool CanRead { get; }

		/// <summary>
		/// Get a <see cref="bool"/> indicating whether this pointer can be written or not
		/// </summary>
		bool CanWrite { get; }

		/// <summary>
		/// Get a <see cref="bool"/> indicating whether this pointer can be read with offset or not
		/// </summary>
		bool CanReadOffset { get; }

		/// <summary>
		/// Get a <see cref="bool"/> indicating whether this pointer can be written with offset or not
		/// </summary>
		bool CanWriteOffset { get; }

		/// <summary>
		/// Get a <see cref="bool"/> indicating whether this pointer can be resized in-place or not
		/// </summary>
		bool CanResize { get; }
	}
	#endregion

	/// <summary>
	/// The struct of which delimits a certain section of a certain unmanaged memory block
	/// </summary>
	/// <remarks>This struct is <b>not</b> responsible for releasing unmanaged memories. It is only used for storing information of memory blocks.</remarks>
	[StructLayout(LayoutKind.Explicit, Size = 32)]
	public readonly struct PointerSegment : IEquatable<PointerSegment>, IMainPropertyFormat, ICheckValid
	{
		#region basic
		[FieldOffset(0)]
		private readonly StorageLocation location;

		[FieldOffset(8)]
		private readonly IPointer pointer;

		[FieldOffset(16)]
		private readonly ulong offset;

		[FieldOffset(24)]
		private readonly ulong length;

		/// <summary>
		/// Check whether this pointer is a valid pointer or not
		/// </summary>
		public bool IsValid() => this.pointer is not null && this.pointer.IsValid();

		/// <summary>
		/// The <see cref="StorageLocation"/> of this <see cref="PointerSegment"/>
		/// </summary>
		public StorageLocation Location => this.location;

		/// <summary>
		/// The raw pointer (without offset) as a <see cref="IPointer"/>
		/// </summary>
		public IPointer Pointer => this.pointer;

		/// <summary>
		/// The offset in bytes to the <see cref="Pointer"/> of this <see cref="PointerSegment"/>
		/// </summary>
		public ulong OffsetInBytes => this.offset;

		/// <summary>
		/// The <b>presenting</b> length in bytes of this <see cref="PointerSegment"/>
		/// </summary>
		public ulong LengthInBytes => this.length;

		/// <summary>
		/// Get the raw pointer structure (without offset) of this <see cref="PointerSegment"/> in <typeparamref name="T"/>
		/// </summary>
		/// <typeparam name="T">The raw pointer structure type</typeparam>
		/// <returns>The raw pointer structure as a <typeparamref name="T"/></returns>
		public T? GetPointer<T>() where T : struct, IPointer => this.pointer is T t ? t : null;

		/// <summary>
		/// Create with given location and pointer
		/// </summary>
		/// <param name="location">The <see cref="StorageLocation"/> of this <see cref="PointerSegment"/></param>
		/// <param name="pointer">The pointer at the given <paramref name="location"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="pointer"/> is a default value</exception>
		/// <exception cref="ArgumentException">If <paramref name="location"/> is a valid value of <paramref name="pointer"/></exception>
		public PointerSegment(StorageLocation location, IPointer pointer)
		{
			if (pointer.Equals(default))
				throw new ArgumentNullException(nameof(pointer));
			if (!pointer.IsValidLocation(location))
				throw new ArgumentException(Parameter.InvalidValue, nameof(location));

			this.location = location; this.pointer = pointer; this.offset = 0; this.length = pointer.LengthInBytes;
		}

		/// <summary>
		/// Create with given <see cref="PointerSegment"/> <paramref name="storage"/> and <paramref name="offset"/> and <paramref name="newLength"/> to the <paramref name="storage"/>
		/// </summary>
		/// <param name="storage">The <see cref="PointerSegment"/> to copy info from</param>
		/// <param name="offset">The offset to the <paramref name="storage"/> in bytes</param>
		/// <param name="newLength">The new presenting length in bytes, default 0 means automatically calculating from <paramref name="offset"/> and <see cref="IPointer.LengthInBytes"/></param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offset"/> or <paramref name="newLength"/> exceeds the boundary</exception>
		public PointerSegment(PointerSegment storage, long offset = 0, ulong newLength = 0)
		{
			offset += (long)storage.offset;
			if (offset < 0)
				throw new ArgumentOutOfRangeException(nameof(offset));
			ulong off = (ulong)offset;
			if (off > storage.pointer.LengthInBytes)
				throw new ArgumentOutOfRangeException(nameof(offset));
			if (newLength == 0)
				newLength = storage.pointer.LengthInBytes - off;
			if (off + newLength > storage.pointer.LengthInBytes)
				throw new ArgumentOutOfRangeException(nameof(newLength));

			this.location = storage.location; this.pointer = storage.pointer; this.offset = off; this.length = newLength;
		}

		/// <summary>
		/// Create a new <see cref="PointerSegment"/> with given <paramref name="offset"/>
		/// </summary>
		/// <param name="offset">The offset in bytes to move</param>
		/// <param name="newLength">The new length in bytes to set</param>
		/// <returns>The new <see cref="PointerSegment"/> moved from this pointer by <paramref name="offset"/> bytes and set the new presenting length to <paramref name="newLength"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offset"/> or <paramref name="newLength"/> exceeds the boundary</exception>
		public PointerSegment MoveBy(long offset, ulong newLength) => offset == 0 ? this : new PointerSegment(this, offset, newLength);

		/// <summary>
		/// Create a new <see cref="PointerSegment"/> with given <paramref name="newLength"/>
		/// </summary>
		/// <param name="newLength">The new length in bytes to set</param>
		/// <returns>The new <see cref="PointerSegment"/> with same pointer and offset while length is set to <paramref name="newLength"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="newLength"/> exceeds the boundary</exception>
		public PointerSegment AsLength(ulong newLength) => newLength == this.length ? this : new PointerSegment(this, 0, newLength);
		#endregion

		#region equality
		/// <summary>
		/// Whether this == <paramref name="other"/>
		/// </summary>
		/// <param name="other">another <see cref="PointerSegment"/> to compare</param>
		/// <returns>this == <paramref name="other"/></returns>
		public bool Equals(PointerSegment other)
		{
			return this.location == other.location && this.pointer == other.pointer && this.offset == other.offset;
		}

		/// <summary>
		/// Override <see cref="ValueType.Equals(object?)"/> to check whether this == <paramref name="obj"/>
		/// </summary>
		/// <param name="obj">another object to compare</param>
		/// <returns>this == <paramref name="obj"/></returns>
		public override bool Equals(object obj)
		{
			return obj is PointerSegment storage && this.Equals(storage);
		}

		/// <summary>
		/// Override <see cref="ValueType.GetHashCode"/> to get the hash code this <see cref="PointerSegment"/>.
		/// </summary>
		/// <returns>The hash code</returns>
		public override int GetHashCode()
		{
			return HashCode.Combine(this.location, this.pointer, this.offset);
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
			["location"] = this.location.ToString(),
			["length"] = this.length.ToString(),
		} : new Dictionary<string, string>
		{
			["location"] = this.location.ToString(),
			["offset"] = this.offset.ToString(),
			["length"] = this.length.ToString(),
		};

		/// <summary>
		/// Return the string representation of this <see cref="PointerSegment"/>
		/// </summary>
		/// <returns>the string representation of this <see cref="PointerSegment"/></returns>
		public override string ToString() => ((IMainPropertyFormat)this).ToString();
		#endregion

		#region operator
		/// <summary>
		/// Add offset (in bytes) to a <see cref="PointerSegment"/> to get another.
		/// </summary>
		/// <param name="storage">the <see cref="PointerSegment"/></param>
		/// <param name="offset">the offset of type <see cref="long"/></param>
		/// <returns>a <see cref="Storage{T}"/> with <paramref name="offset"/> added to the pointer</returns>
		public static PointerSegment operator +(PointerSegment storage, long offset) => offset == 0 ? storage : new PointerSegment(storage, offset);

		/// <summary>
		/// Subtract offset (in bytes) to a <see cref="PointerSegment"/> to get another.
		/// </summary>
		/// <param name="storage">the <see cref="PointerSegment"/></param>
		/// <param name="offset">the offset of type <see cref="long"/></param>
		/// <returns>a <see cref="Storage{T}"/> with <paramref name="offset"/> added to the pointer</returns>
		public static PointerSegment operator -(PointerSegment storage, long offset) => offset == 0 ? storage : new PointerSegment(storage, -offset);

		/// <summary>
		/// Get the pointer's difference (in bytes) of two <see cref="PointerSegment"/>s.
		/// </summary>
		/// <param name="left">the left <see cref="PointerSegment"/></param>
		/// <param name="right">the right <see cref="PointerSegment"/></param>
		/// <returns>If <paramref name="left"/> and <paramref name="right"/> have different references, return <see cref="long.MinValue"/>; otherwise, return a <see cref="long"/> as the difference between the <see cref="Pointer"/>s of <paramref name="left"/> and <paramref name="right"/></returns>
		public static long operator -(PointerSegment left, PointerSegment right) => left.location != right.location || left.pointer != right.pointer ? long.MinValue : (long)left.offset - (long)right.offset;
		#endregion
	}
	#endregion


	#region storage classes

	#region interfaces
	/// <summary>
	/// The interface for wrapper of unmanaged memory block(s) of different <see cref="StorageLocation"/>(s) of any data type
	/// </summary>
	public interface IStorage : IReadOnlyList<PointerSegment>, ICheckValid, IDisposable
	{
		/// <summary>
		/// The total length of the presenting array in bytes
		/// </summary>
		ulong LengthInBytes { get; }

		/// <summary>
		/// The description of the storage locations of this storage class as a <see cref="CombinationOfLocations"/>
		/// </summary>
		CombinationOfLocations LocationDescription { get; }

		/// <summary>
		/// Check whether this storage is valid or not after moving an <paramref name="offset"/> and set <see cref="LengthInBytes"/> to <paramref name="newLength"/>
		/// </summary>
		/// <param name="offset">The offset to move in bytes</param>
		/// <param name="newLength">The length to check in bytes, default 0 means auto calculation by <paramref name="offset"/></param>
		/// <returns>The validness of this storage under <paramref name="offset"/> and <paramref name="newLength"/></returns>
		bool IsOffsetValid(long offset, ulong newLength = 0);

		/// <summary>
		/// Check the given storage and throw exception if check failed.
		/// </summary>
		/// <param name="offset">The offset to move in bytes</param>
		/// <param name="length">The length to check in bytes, default 0 means auto calculation by <paramref name="offset"/></param>
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
	/// The abstract wrapper class of unmanaged memory block(s) of different <see cref="StorageLocation"/>(s).
	/// </summary>
	/// <typeparam name="T">any unmanaged data type</typeparam>
	/// <remarks>I must warn you that although C# has GC to periodically collect unused garbage to prevent memory leak, you should not rely on it too much. <b>Remember</b> to use <c>using</c> statement or call <see cref="Storage{T}.Dispose()"/>.<br/>
	/// The leaked memory which will be collected GC still causes not only performance loss but also potential bugs if you do not know how GC works, since the concrete class(es) shall be a class with finalizers thus cannot be in GC generation 0, i.e. it will not be immediately disposed when out-of-scope.<br/>
	/// See https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/ for official documentations of GC of dot NET.</remarks>
	public abstract class Storage<T> : IStorage, IEquatable<Storage<T>>, IMainPropertyFormat where T : unmanaged
	{
		#region properties
		/// <summary>
		/// Get the size of <typeparamref name="T"/> in memory in bytes
		/// </summary>
		public static readonly unsafe uint SizeOfT = (uint)sizeof(T);

		/// <summary>
		/// The total length of the presenting array in <typeparamref name="T"/> (rather than bytes)
		/// </summary>
		public abstract ulong Length { get; }

		/// <summary>
		/// The total length of the presenting array in bytes
		/// </summary>
		public virtual ulong LengthInBytes => this.Length * SizeOfT;

		/// <summary>
		/// The description of the storage locations of this storage class as a <see cref="CombinationOfLocations"/>
		/// </summary>
		public abstract CombinationOfLocations LocationDescription { get; }

		/// <summary>
		/// The number of <see cref="PointerSegment"/>(s) of this <see cref="Storage{T}"/> 
		/// </summary>
		public abstract int Count { get; }

		/// <summary>
		/// Indexer of the <see cref="PointerSegment"/>(s) of this <see cref="Storage{T}"/> (in presenting order)
		/// </summary>
		/// <param name="index">the element index</param>
		/// <returns>the <see cref="PointerSegment"/> at <paramref name="index"/></returns>
		public abstract PointerSegment this[int index] { get; }
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

		#region other methods
		/// <summary>
		/// Make a referenced <see cref="IStorage"/> with the starting pointer moving <paramref name="offset"/> and <see cref="LengthInBytes"/> changing to <paramref name="newLength"/>
		/// </summary>
		/// <param name="offset">The offset in <typeparamref name="T"/> to the starting pointer of this storage as a <see cref="long"/></param>
		/// <param name="newLength">The new length in <typeparamref name="T"/> as a <see cref="ulong"/>, default 0 means automatically calculate from <paramref name="offset"/></param>
		/// <returns>A referenced <see cref="IStorage"/> of this one</returns>
		public virtual Storage<T> MakeReference(long offset = 0, ulong newLength = 0)
		{
			return new ReferenceStorage<T>(this, offset, newLength);
		}

		/// <summary>
		/// Convert this <see cref="Storage{T}"/> to another one with different data type <typeparamref name="TOut"/>
		/// </summary>
		/// <typeparam name="TOut">the output data type</typeparam>
		/// <returns>a referenced <see cref="Storage{TOut}"/></returns>
		/// <exception cref="InvalidCastException">if <see cref="IStorage.LengthInBytes"/> cannot be divided by <see cref="Storage{TOut}.SizeOfT"/></exception>
		public abstract Storage<TOut> As<TOut>() where TOut : unmanaged;

		/// <summary>
		/// Check whether this storage is valid or not.
		/// </summary>
		/// <returns>The validness of this storage</returns>
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
		/// Check whether this storage is valid or not after moving an <paramref name="offset"/> and set <see cref="LengthInBytes"/> to <paramref name="newLength"/>.
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

		#region enumerator
		IEnumerator<PointerSegment> IEnumerable<PointerSegment>.GetEnumerator()
		{
			for (int i = 0; i < this.Count; i++)
			{
				yield return this[i];
			}
		}
		IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<PointerSegment>)this).GetEnumerator();
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
		public static bool operator ==(Storage<T> left, Storage<T> right)
		{
			if (left is null && right is null)
				return true;
			if ((left is null) != (right is null))
				return false;
			return left.Equals(right);
		}

		/// <summary>
		/// Inequality operator
		/// </summary>
		public static bool operator !=(Storage<T> left, Storage<T> right) => !(left == right);
		#endregion

		#region string
		string IMainPropertyFormat.StringMain => this.Count == 1 ? ((IMainPropertyFormat)this[0]).StringMain : string.Join(", ", ((IMainPropertyFormat)this).StringMain);

		IReadOnlyDictionary<string, string> IMainPropertyFormat.StringProperties => new Dictionary<string, string>
		{
			["type"] = typeof(T).Name,
			[this.Count == 1 ?"length" : "total_length"] = this.Length.ToString(),
		};

		/// <summary>
		/// Override <see cref="object.ToString"/> to get the string representation.
		/// </summary>
		/// <returns>string representation</returns>
		public override string ToString()
		{
			string main = this.Count == 1 ? ((IMainPropertyFormat)this).StringMain : ('{' + string.Join(", ", this) + '}');
			return IMainPropertyFormat.Combine(main, ((IMainPropertyFormat)this).StringProperties);
		}
		#endregion

		#region operator
		/// <summary>
		/// An empty <see cref="Storage{T}"/>
		/// </summary>
		public static readonly Storage<T> Empty = new ReferenceStorage<T>();

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
		/// The number of <see cref="PointerSegment"/>(s) of this <see cref="Storage{T}"/> 
		/// </summary>
		public override int Count => this.end - this.start;

		/// <summary>
		/// Override <see cref="Storage{T}.Length"/> to show the new presenting length
		/// </summary>
		public override ulong Length { get; }

		/// <summary>
		/// The description of the storage locations of this storage class as a <see cref="CombinationOfLocations"/>
		/// </summary>
		public override CombinationOfLocations LocationDescription => this.reference.LocationDescription[this.start..this.end];

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

		internal ReferenceStorage() : this(null, 0, 0, 0, 0, 0) { }
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
		/// Indexer of the <see cref="PointerSegment"/>(s) of this <see cref="Storage{T}"/> (in presenting order)
		/// </summary>
		/// <param name="index">the element index</param>
		/// <returns>the <see cref="PointerSegment"/> at <paramref name="index"/></returns>
		public override PointerSegment this[int index] {
			get {
				if (index < 0 || index >= this.Count)
					throw new ArgumentOutOfRangeException(nameof(index));
				PointerSegment pointer = this.reference[index - start];
				if (index == 0)
				{
					pointer = pointer.MoveBy((long)this.startOffsetBytes);
				}
				else if (index == this.Count - 1)
				{
					pointer = new PointerSegment(pointer, newLength: this.endLengthBytes);
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
	#endregion
}
