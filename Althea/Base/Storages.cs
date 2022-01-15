using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Althea.Helpers;
using Althea.Linq;
using Althea.NativeTypes;
using Althea.Resources;

using MEM = Althea.Storage.AbstractApi;


namespace Althea
{
	#region storage location enum
	/// <summary>
	/// The enum of the storage location types
	/// </summary>
	public enum LocationType : short
	{
		/// <summary>
		/// Represents a storage location represented by Universal Resource Identifier.
		/// </summary>
		Uri = LocationTypeExtension.ClassStream + 0,
		/// <summary>
		/// Storage at local CPU RAM
		/// </summary>
		CpuRam = LocationTypeExtension.ClassMemory + 0,
		/// <summary>
		/// Storage at local GPU RAM
		/// </summary>
		GpuRam = LocationTypeExtension.ClassMemory + 1 << LocationTypeExtension.ClassMaskEnd,
	}

	/// <summary>
	/// Extension class for <see cref="LocationType"/>
	/// </summary>
	public static class LocationTypeExtension
	{
		/// <summary>
		/// The classification mask end bit of <see cref="LocationType"/>
		/// </summary>
		public const int ClassMaskEnd = 3;

		/// <summary>
		/// The classification mask of <see cref="LocationType"/>
		/// </summary>
		public const int ClassMask = 0b111;

		/// <summary>
		/// The classification of stream typed <see cref="LocationType"/>
		/// </summary>
		/// <remarks>Other classifications are not presented here but they are also supported.</remarks>
		public const int ClassStream = 0b001;

		/// <summary>
		/// The classification of memory typed <see cref="LocationType"/>
		/// </summary>
		/// <remarks>Other classifications are not presented here but they are also supported.</remarks>
		public const int ClassMemory = 0b000;

		/// <summary>
		/// Get the classification of given <see cref="LocationType"/>
		/// </summary>
		/// <param name="locationType">The given <see cref="LocationType"/></param>
		/// <returns>The classification as a <see cref="byte"/></returns>
		public static byte GetClassification(this LocationType locationType) => (byte)(((byte)locationType) & ClassMask);

		/// <summary>
		/// The delegate for obtaining the description of the given <paramref name="detail"/> which is associated with different values of <see cref="LocationType"/>
		/// </summary>
		/// <param name="detail">The input detail as a <see cref="short"/></param>
		/// <returns>The description of the given <paramref name="detail"/> associated with current value of <see cref="LocationType"/></returns>
		public delegate string GetDetailDescription(short detail);

		static LocationTypeExtension()
		{
			EnumHelper.SetMethod<LocationType, GetDetailDescription>(LocationType.Uri, static d => $"(scheme={((Storage.UriScheme)d).GetName()})");
			EnumHelper.SetMethod<LocationType, GetDetailDescription>(LocationType.CpuRam, static d => $"(device_ID={d})");
			EnumHelper.SetMethod<LocationType, GetDetailDescription>(LocationType.GpuRam, static d => $"(device_ID={d})");
		}
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
	/// This struct has size of a <see cref="int"/>. The <see cref="LocationType"/> occupies first 2 bytes and its detail occupies the rest.
	/// </remarks>
	public readonly struct StorageLocation : IEqualityOperators<StorageLocation, StorageLocation>
	{
		#region basic
		/// <summary>
		/// The location type of this <see cref="StorageLocation"/> as a <see cref="LocationType"/>
		/// </summary>
		public LocationType Type { get; }

		/// <summary>
		/// The location detail of the <see cref="Type"/>.
		/// </summary>
		public short Detail { get; }

		/// <summary>
		/// Create with given location and device ID
		/// </summary>
		/// <param name="type">The location of this <see cref="StorageLocation"/>, must be a flag</param>
		/// <param name="detail">The detail of <paramref name="type"/>: a <see cref="Althea.Storage.UriScheme"/> for a <see cref="LocationType.Uri"/> or a device ID otherwise.</param>
		public StorageLocation(LocationType type, short detail)
		{
			this.Type = type; this.Detail = detail;
		}

		[System.Text.Json.Serialization.JsonConstructor]
		internal StorageLocation(string type, short detail)
		{
			this.Type = EnumHelper.Parse<LocationType>(type); this.Detail = detail;
		}
		#endregion

		#region equality
		/// <summary>
		/// Whether this == <paramref name="other"/>
		/// </summary>
		/// <param name="other">another <see cref="StorageLocation"/> to compare</param>
		/// <returns>this == <paramref name="other"/></returns>
		public bool Equals(StorageLocation other) => this.Type == other.Type && this.Detail == other.Detail;

		/// <summary>
		/// Override <see cref="ValueType.Equals(object?)"/> to check whether this == <paramref name="obj"/>
		/// </summary>
		/// <param name="obj">another object to compare</param>
		/// <returns>this == <paramref name="obj"/></returns>
		public override bool Equals(object? obj) => obj is StorageLocation storageDetail && this.Equals(storageDetail);

		/// <summary>
		/// Override <see cref="ValueType.GetHashCode"/> to get the hash code this <see cref="StorageLocation"/>.
		/// </summary>
		/// <returns>The hash code</returns>
		public override int GetHashCode() => ((int)this.Type << sizeof(short)) + this.Detail;

		/// <summary>
		/// Equality operator
		/// </summary>
		public static bool operator ==(StorageLocation left, StorageLocation right) => left.Equals(right);

		/// <summary>
		/// Inequality operator
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(StorageLocation left, StorageLocation right) => !(left == right);
		#endregion

		#region string related
		/// <summary>
		/// Return the string representation of this <see cref="StorageLocation"/>
		/// </summary>
		/// <returns>the string representation of this <see cref="StorageLocation"/></returns>
		public override string ToString()
		{
			return this.Type.GetName() + this.Type.GetMethod<LocationType, LocationTypeExtension.GetDetailDescription>();
		}
		#endregion
	}

	/// <summary>
	/// The struct of a combination of storage location(s)
	/// </summary>
	/// <remarks>
	/// The size of this structure is 64, which is exactly inside the cache line boundaries for Intel CPUs.<br/>
	/// The data field of this struct is a <see cref="FixedBuffer_64{T}"/> rather than an array of <see cref="StorageLocation"/> to reduce GC pressure since this structure is frequently created.<br/>
	/// Furthermore, still nearly no GC is necessary even if some reference type has field of this <see cref="CombinationOfLocations"/>.
	/// </remarks>
	[StructLayout(LayoutKind.Explicit)]
	public readonly struct CombinationOfLocations : IEqualityOperators<CombinationOfLocations, CombinationOfLocations>, IReadOnlyList<StorageLocation>, IMainPropertyFormattable<CombinationOfLocations>
	{
		#region basic
		private const int MaxSize = (64 - 4) / 4;//default(FixedBuffer_64<StorageLocation>).Count - 1;

		[FieldOffset(0)]
		private readonly FixedBuffer_64<StorageLocation> data;
		[FieldOffset(64 - 4)]
		private readonly CombinationType type; // size = 2
		[FieldOffset(64 - 4 + 2)]
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
			this.data = default;
			if (data.Length >= MaxSize || data.IsEmpty)
				throw new ArgumentOutOfRangeException(nameof(data), data.Length, Parameter.WrongSize);
			// initialize
			this.type = type;
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
			this.data = default;
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
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(CombinationOfLocations other)
		{
			if (this.type != other.type)
				return false;
			if (this.count != other.count)
				return false;
			if (this.count == 0)
				return true;
			if (this.type.IsOrdered())
				return this.data.Equals(other.data);
			else
				return this.data.AsSpan(this.count).SetEquals(other.data.AsSpan(this.count));
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
		public static bool operator ==(CombinationOfLocations left, CombinationOfLocations right) => left.Equals(right);

		/// <summary>
		/// Inequality operator
		/// </summary>
		public static bool operator !=(CombinationOfLocations left, CombinationOfLocations right) => !(left == right);
		#endregion

		#region index
		/// <summary>
		/// Basic indexer of this <see cref="CombinationOfLocations"/>
		/// </summary>
		/// <param name="index">The index</param>
		/// <returns>The element at <paramref name="index"/> as a <see cref="StorageLocation"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="index"/> is out of range</exception>
		public StorageLocation this[int index] => index >= 0 && index < this.count ? this.data[index] : throw new ArgumentOutOfRangeException(nameof(index), index, Parameter.InvalidValue);

		/// <summary>
		/// Forms a slice out of the current <see cref="CombinationOfLocations"/> starting at a specified <paramref name="start"/> for a specified <paramref name="length"/>.
		/// </summary>
		/// <param name="start">The index at which to begin this slice.</param>
		/// <param name="length">The desired length for the slice.</param>
		/// <returns>A <see cref="CombinationOfLocations"/> that consists of <see cref="StorageLocation"/>s composed of <paramref name="length"/> elements from the <paramref name="start"/> and the same <see cref="Type"/>.</returns>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="start"/> and/or <paramref name="length"/> exceeds the boundary of this <see cref="CombinationOfLocations"/></exception>
		public CombinationOfLocations Slice(int start, int length)
		{
			if (start < 0 || start >= this.count)
				throw new ArgumentOutOfRangeException(nameof(start), start, Parameter.InvalidValue);
			if (length <= 0 || length + start > this.count)
				throw new ArgumentOutOfRangeException(nameof(length), length, Parameter.InvalidValue);

			var locations = this.CopyLocationsToSpan(stackalloc StorageLocation[this.count]);
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
			for (int i = 0; i < this.count; i++)
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
		public static implicit operator CombinationOfLocations(StorageLocation storageDetail) => new(storageDetail);

		/// <summary>
		/// Copy the <see cref="StorageLocation"/>s of this combination to a given <paramref name="span"/>
		/// </summary>
		/// <param name="span">The given <see cref="Span{T}"/> of <see cref="StorageLocation"/> to copy to</param>
		/// <returns><paramref name="span"/>.<see cref="Span{T}.Slice(int, int)">Slice</see>(0, <see cref="Count">Count</see>)</returns>
		/// <exception cref="ArgumentException">If <paramref name="span"/>'s length is less than <see cref="Count"/></exception>
		public Span<StorageLocation> CopyLocationsToSpan(Span<StorageLocation> span)
		{
			if (span.Length < this.count)
				throw new ArgumentException(Parameter.WrongSize, nameof(span));
			this.data.CopyToSpan(span);
			return span[..this.count];
		}
		#endregion

		#region string related
		static string IMainPropertyFormattable<CombinationOfLocations>.StringMain => nameof(CombinationOfLocations);

		static IEnumerable<string> IMainPropertyFormattable<CombinationOfLocations>.PropertyNames => new[] { "Type", "Data" };

		IEnumerable<object?> IMainPropertyFormattable<CombinationOfLocations>.PropertyValues => new object[] { this.type.GetName(), this.data.AsSpan(this.count).SpanJoin(", ") };

		/// <summary>
		/// Return the string representation of this <see cref="CombinationOfLocations"/>
		/// </summary>
		/// <returns>the string representation of this <see cref="CombinationOfLocations"/></returns>
		public override string ToString() => IMainPropertyFormattable<CombinationOfLocations>.ToString(in this);
		#endregion
	}
	#endregion


	#region pointer

	#region interface
	/// <summary>
	/// The interface for an immutable pointer which can be read, overwritten and positioned at any possible storage location, including any type of memory and any scheme of URI.
	/// </summary>
	/// <typeparam name="TSelf">The actual implementation type</typeparam>
	public interface IPointer<TSelf> : ICheckValid, IEqualityOperators<TSelf, TSelf>, IMainPropertyFormattable<TSelf> where TSelf : IPointer<TSelf>
	{
		/// <summary>
		/// When implemented by derived classes, statically get the <see cref="StorageLocation"/> of this pointer's underlying type
		/// </summary>
		abstract static StorageLocation Location { get; }

		/// <summary>
		/// When implemented by derived classes, get the original (native) length of this pointer's underlying storage in bytes
		/// </summary>
		long LengthInBytes { get; }

		/// <summary>
		/// When implemented by derived classes, get the hash code this <see cref="IPointer{T}"/>.
		/// </summary>
		/// <returns>The hash code</returns>
		int GetHashCode();
	}
	#endregion

	/// <summary>
	/// The struct of which delimits a certain section of a certain memory block
	/// </summary>
	/// <typeparam name="T">The type of <see cref="IPointer{T}"/></typeparam>
	/// <remarks>This struct is <b>not</b> responsible for releasing unmanaged memories. It is only used to store information of memory blocks.</remarks>
	public readonly struct PointerSegment<T> :
		ICheckValid,
		IMainPropertyFormattable<PointerSegment<T>>,
		IEqualityOperators<PointerSegment<T>, PointerSegment<T>>,
		IAdditiveIdentity<PointerSegment<T>, long>,
		IAdditionOperators<PointerSegment<T>, long, PointerSegment<T>>,
		ISubtractionOperators<PointerSegment<T>, long, PointerSegment<T>>,
		ISubtractionOperators<PointerSegment<T>, PointerSegment<T>, long>
		where T : IPointer<T>
	{
		#region basic
		/// <summary>
		/// Check whether this pointer is a valid pointer or not
		/// </summary>
		public bool IsValid() => this.LengthInBytes > 0 && this.Pointer is not null && this.Pointer.IsValid();

		/// <summary>
		/// The <see cref="StorageLocation"/> of this <see cref="PointerSegment{T}"/>
		/// </summary>
		public static StorageLocation Location => T.Location;

		/// <summary>
		/// The native pointer (without offset and presenting length) as a <see cref="IPointer{T}"/>
		/// </summary>
		public T Pointer { get; }

		/// <summary>
		/// The offset in bytes to the <see cref="Pointer"/> of this <see cref="PointerSegment{T}"/>
		/// </summary>
		public long OffsetInBytes { get; }

		/// <summary>
		/// The <b>presenting</b> length in bytes of this <see cref="PointerSegment{T}"/>
		/// </summary>
		public long LengthInBytes { get; }

		/// <summary>
		/// Create with given pointer
		/// </summary>
		/// <param name="pointer">The underlying pointer</param>
		/// <exception cref="ArgumentNullException">If <paramref name="pointer"/> is not a valid value</exception>
		public PointerSegment(T pointer)
		{
			if (pointer is null || !pointer.IsValid())
				throw new ArgumentNullException(nameof(pointer));

			this.Pointer = pointer; this.OffsetInBytes = 0; this.LengthInBytes = pointer.LengthInBytes;
		}

		/// <summary>
		/// Create with given <see cref="PointerSegment{T}"/> <paramref name="storage"/> and <paramref name="offset"/> and <paramref name="newLength"/> to the <paramref name="storage"/>
		/// </summary>
		/// <param name="storage">The <see cref="PointerSegment{T}"/> to copy info from</param>
		/// <param name="offset">The offset to the <paramref name="storage"/> in bytes</param>
		/// <param name="newLength">The new presenting length in bytes, default 0 means automatically calculating from <paramref name="offset"/> and <see cref="IPointer{T}.LengthInBytes"/>. A value less than or equals to 0 means automatically calculate.</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offset"/> or <paramref name="newLength"/> exceeds the boundary</exception>
		public PointerSegment(PointerSegment<T> storage, long offset = 0, long newLength = 0)
		{
			offset += storage.OffsetInBytes;
			if (offset < 0)
				throw new ArgumentOutOfRangeException(nameof(offset), offset, Parameter.CannotNegative);
			long off = offset;
			if (off > storage.Pointer.LengthInBytes)
				throw new ArgumentOutOfRangeException(nameof(offset), offset, Parameter.InvalidValue);
			if (newLength <= 0)
				newLength = storage.Pointer.LengthInBytes - off;
			if (off + newLength > storage.Pointer.LengthInBytes)
				throw new ArgumentOutOfRangeException(nameof(newLength), newLength, Parameter.InvalidValue);

			this.Pointer = storage.Pointer; this.OffsetInBytes = off; this.LengthInBytes = newLength;
		}

		/// <summary>
		/// Create a new <see cref="PointerSegment{T}"/> with given <paramref name="offset"/>
		/// </summary>
		/// <param name="offset">The offset in bytes to move</param>
		/// <param name="newLength">The new length in bytes to set, default 0 means auto calculation from <paramref name="offset"/>. A value less than or equals to 0 means automatically calculate.</param>
		/// <returns>The new <see cref="PointerSegment{T}"/> moved from this pointer by <paramref name="offset"/> bytes and set the new presenting length to <paramref name="newLength"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offset"/> or <paramref name="newLength"/> exceeds the boundary</exception>
		public PointerSegment<T> MoveBy(long offset, long newLength = 0) => offset == 0 && newLength <= 0 ? this : new(this, offset, newLength);

		/// <summary>
		/// Create a new <see cref="PointerSegment{T}"/> with given <paramref name="newLength"/>
		/// </summary>
		/// <param name="newLength">The new length in bytes to set</param>
		/// <returns>The new <see cref="PointerSegment{T}"/> with same pointer and offset while length is set to <paramref name="newLength"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="newLength"/> exceeds the boundary</exception>
		public PointerSegment<T> AsLength(long newLength) => newLength == this.LengthInBytes ? this : new(this, 0, newLength);
		#endregion

		#region equality
		/// <summary>
		/// Check whether this <see cref="PointerSegment{T}"/> overlaps with the <paramref name="other"/> <see cref="PointerSegment{T}"/>
		/// </summary>
		/// <param name="other">The other <see cref="PointerSegment{T}"/> to check overlap</param>
		/// <returns>True if this overlaps with the <paramref name="other"/>, false otherwise</returns>
		public bool OverlapWith(PointerSegment<T> other) => this.Pointer == other.Pointer && (this.OffsetInBytes > other.OffsetInBytes + other.LengthInBytes || other.OffsetInBytes > this.OffsetInBytes + this.LengthInBytes);

		/// <summary>
		/// Whether this == <paramref name="other"/>
		/// </summary>
		/// <param name="other">another <see cref="PointerSegment{T}"/> to compare</param>
		/// <returns>this == <paramref name="other"/></returns>
		public bool Equals(PointerSegment<T> other) => this.Pointer == other.Pointer && this.OffsetInBytes == other.OffsetInBytes;

		/// <summary>
		/// Override <see cref="ValueType.Equals(object?)"/> to check whether this == <paramref name="obj"/>
		/// </summary>
		/// <param name="obj">another object to compare</param>
		/// <returns>this == <paramref name="obj"/></returns>
		public override bool Equals(object? obj) => obj is PointerSegment<T> s && this.Equals(s);

		/// <summary>
		/// Override <see cref="ValueType.GetHashCode"/> to get the hash code this <see cref="PointerSegment{T}"/>.
		/// </summary>
		/// <returns>The hash code</returns>
		public override int GetHashCode() => HashCode.Combine(this.Pointer.GetHashCode(), this.OffsetInBytes);

		/// <summary>
		/// Equality operator
		/// </summary>
		public static bool operator ==(PointerSegment<T> left, PointerSegment<T> right) => left.Equals(right);

		/// <summary>
		/// Inequality operator
		/// </summary>
		public static bool operator !=(PointerSegment<T> left, PointerSegment<T> right) => !(left == right);
		#endregion

		#region to string
		private static readonly string[] names = new[] { nameof(Location), nameof(LengthInBytes), nameof(OffsetInBytes) };

		static string IMainPropertyFormattable<PointerSegment<T>>.StringMain => T.StringMain;
		static IEnumerable<string> IMainPropertyFormattable<PointerSegment<T>>.PropertyNames => names;
		IEnumerable<object?> IMainPropertyFormattable<PointerSegment<T>>.PropertyValues => new object[] { Location, this.LengthInBytes, this.OffsetInBytes };

		/// <summary>
		/// Return the string representation of this <see cref="PointerSegment{T}"/>
		/// </summary>
		/// <returns>the string representation of this <see cref="PointerSegment{T}"/></returns>
		public override string ToString() => IMainPropertyFormattable<PointerSegment<T>>.ToString(in this);
		#endregion

		#region operators
		static long IAdditiveIdentity<PointerSegment<T>, long>.AdditiveIdentity => 0;

		/// <summary>
		/// Add offset (in bytes) to a <see cref="PointerSegment{T}"/> to get another.
		/// </summary>
		/// <param name="storage">The <see cref="PointerSegment{T}"/></param>
		/// <param name="offset">The offset of type <see cref="long"/></param>
		/// <returns>A <see cref="PointerSegment{T}"/> with <paramref name="offset"/> added to the pointer</returns>
		public static PointerSegment<T> operator +(PointerSegment<T> storage, long offset) => offset == 0 ? storage : new(storage, offset);

		/// <summary>
		/// Subtract offset (in bytes) to a <see cref="PointerSegment{T}"/> to get another.
		/// </summary>
		/// <param name="storage">The <see cref="PointerSegment{T}"/></param>
		/// <param name="offset">The offset of type <see cref="long"/></param>
		/// <returns>A <see cref="PointerSegment{T}"/> with <paramref name="offset"/> subtracted from the pointer</returns>
		public static PointerSegment<T> operator -(PointerSegment<T> storage, long offset) => offset == 0 ? storage : new(storage, -offset);

		/// <summary>
		/// Get the pointer's difference (in bytes) of two <see cref="PointerSegment{T}"/>s.
		/// </summary>
		/// <param name="left">The left <see cref="PointerSegment{T}"/></param>
		/// <param name="right">The right <see cref="PointerSegment{T}"/></param>
		/// <returns>A <see cref="long"/> as the difference between the <see cref="Pointer"/>s of <paramref name="left"/> and <paramref name="right"/></returns>
		/// <exception cref="InvalidOperationException">If <paramref name="left"/> and <paramref name="right"/> have different pointers</exception>
		public static long operator -(PointerSegment<T> left, PointerSegment<T> right) => left.Pointer == right.Pointer ? left.OffsetInBytes - right.OffsetInBytes : throw new InvalidOperationException();
		#endregion
	}
	#endregion


	#region storage interfaces
	/// <summary>
	/// The interface for wrapper of unmanaged memory block(s) of different <see cref="StorageLocation"/>(s) of any data type
	/// </summary>
	/// <typeparam name="TSelf">The actual class that implement <see cref="IStorage{TSelf}"/></typeparam>
	public interface IStorage<TSelf> :
		ICheckValid, IDisposable, IEqualityOperators<TSelf, TSelf>, ICloneable<TSelf>, IMainPropertyFormattable<TSelf>
		where TSelf : class, IStorage<TSelf>
	{
		/// <summary>
		/// When implemented by a derived class, get the data type of this storage as a <see cref="NativeTypes.DataType"/>
		/// </summary>
		abstract static DataType DataType { get; }

		/// <summary>
		/// When implemented by a derived class, statically get an empty <typeparamref name="TSelf"/>
		/// </summary>
		abstract static TSelf Empty { get; }

		/// <summary>
		/// When implemented by a derived class, get the total length of the presenting array in bytes
		/// </summary>
		long LengthInBytes { get; }

		/// <summary>
		/// When implemented by a derived class, statically get the description of the storage locations of this <typeparamref name="TSelf"/> as a <see cref="CombinationOfLocations"/>
		/// </summary>
		abstract static CombinationOfLocations LocationDescription { get; }

		/// <summary>
		/// When implemented by a derived class, check whether this <typeparamref name="TSelf"/> is valid or not after moving an <paramref name="offset"/> and set <see cref="LengthInBytes"/> to <paramref name="newLength"/>
		/// </summary>
		/// <param name="offset">The offset to move in bytes</param>
		/// <param name="newLength">The length to check in bytes, default 0 means auto calculation by <paramref name="offset"/></param>
		/// <returns>The validness of this <typeparamref name="TSelf"/> under <paramref name="offset"/> and <paramref name="newLength"/></returns>
		/// <remarks>Default implementation utilizes <see cref="LengthInBytes"/> and <see cref="IReferenceStorage{TStorage}.TotalOffsetInBytes"/></remarks>
		bool IsByteOffsetValid(long offset, long newLength = 0)
		{
			if (newLength < 0 || !this.IsValid())
				return false;
			if (this is IReferenceStorage<TSelf> reference)
			{
				if (reference.Reference is null)
					return false;
				offset += reference.TotalOffsetInBytes;
				if (offset < 0 || offset >= reference.Reference.LengthInBytes)
					return false;
				if (newLength > 0 && newLength + offset > reference.Reference.LengthInBytes)
					return false;
				return true;
			}
			else
			{
				if (offset < 0 || offset >= this.LengthInBytes)
					return false;
				if (newLength > 0 && newLength + offset > this.LengthInBytes)
					return false;
				return true;
			}
		}

		/// <summary>
		/// When implemented by a derived class, check whether this <typeparamref name="TSelf"/> has same origin as the <paramref name="other"/> <typeparamref name="TSelf"/>.
		/// </summary>
		/// <param name="other">The other <typeparamref name="TSelf"/> to check overlap</param>
		/// <returns>True if this storage has same origin with the <paramref name="other"/>, false otherwise</returns>
		/// <remarks>Default implementation utilizes the <see cref="IEqualityOperators{TSelf, TOther}"/>.</remarks>
		/// <exception cref="NotImplementedException">If this <typeparamref name="TSelf"/> or the <paramref name="other"/> <typeparamref name="TSelf"/> is neither an <see cref="IActualStorage{TStorage}"/> nor an <see cref="IReferenceStorage{TStorage}"/>.</exception>
		bool SameOriginAs(TSelf other)
		{
			if (!this.IsValid() || !other.IsValid())
				return false;

			var originThis = (TSelf?)(this as IActualStorage<TSelf>) ?? (this as IReferenceStorage<TSelf>)?.Reference;
			var originOther = (TSelf?)(other as IActualStorage<TSelf>) ?? (other as IReferenceStorage<TSelf>)?.Reference;
			if (originThis is null || originOther is null)
				throw new NotImplementedException();
			return originThis == originOther;
		}

		/// <summary>
		/// When implemented by a derived class, check whether this <typeparamref name="TSelf"/> overlaps with the <paramref name="other"/> <typeparamref name="TSelf"/>.
		/// </summary>
		/// <param name="other">The other <typeparamref name="TSelf"/> to check overlap</param>
		/// <returns>True if this overlaps with the <paramref name="other"/>, false otherwise</returns>
		bool OverlapWith(TSelf other);

		/// <summary>
		/// When implemented by a derived class, create a referenced storage of type <typeparamref name="TOther"/> over this storage
		/// </summary>
		/// <typeparam name="TOther">Any storage type that implements <see cref="IStorage{TOther}"/></typeparam>
		/// <returns>A referenced storage of type <typeparamref name="TOther"/> over this storage</returns>
		/// <exception cref="InvalidCastException">If a referenced <typeparamref name="TOther"/> cannot be created from <typeparamref name="TSelf"/></exception>
		TOther As<TOther>() where TOther : class, IStorage<TOther>;

		/// <summary>
		/// When implemented by a derived class, statically allocate and creates a new <typeparamref name="TSelf"/> alike <paramref name="storage"/>.
		/// </summary>
		/// <param name="storage">The storage of type <typeparamref name="TOther"/> to mimic.</param>
		/// <returns>A new <typeparamref name="TSelf"/> that likes <paramref name="storage"/></returns>
		/// <exception cref="InvalidCastException">If an actual storage <typeparamref name="TOther"/> cannot be created alike <typeparamref name="TSelf"/></exception>
		abstract static TSelf CreateAlike<TOther>(TOther storage) where TOther : class, IStorage<TOther>;

		TSelf ICloneable<TSelf>.Clone()
		{
			var storage = TSelf.CreateAlike((TSelf)this);
			try
			{
				//// TODO: MEM.MemoryCopy(this, storage);
				return storage;
			}
			catch (System.Exception)
			{
				storage?.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Statically get the distance in bytes between two <typeparamref name="TSelf"/>s
		/// </summary>
		/// <param name="left">The left operand of type <typeparamref name="TSelf"/></param>
		/// <param name="right">The right operand of type <typeparamref name="TSelf"/></param>
		/// <returns>The distance between two <typeparamref name="TSelf"/>s in bytes as a <see cref="long"/>.</returns>
		/// <exception cref="InvalidOperationException">If <paramref name="left"/> and <paramref name="right"/> have different origin.</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static long StorageDiff(TSelf left, TSelf right)
		{
			if (!left.IsValid())
				throw new ArgumentNullException(nameof(left));
			if (!right.IsValid())
				throw new ArgumentNullException(nameof(right));
			// check same origin
			if (!left.SameOriginAs(right))
				throw new InvalidOperationException();
			IActualStorage<TSelf>? actualLeft = left as IActualStorage<TSelf>, actualRight = right as IActualStorage<TSelf>;
			IReferenceStorage<TSelf>? refLeft = left as IReferenceStorage<TSelf>, refRight = right as IReferenceStorage<TSelf>;
			// check offset divisible
			if (actualLeft is not null && refRight is not null)
				return -refRight.TotalOffsetInBytes;
			else if (refLeft is not null && actualRight is not null)
				return refLeft.TotalOffsetInBytes;
			else if (refLeft is not null && refRight is not null)
				return refRight.TotalOffsetInBytes - refLeft.TotalOffsetInBytes;
			else
				return 0;
		}
	}

	/// <summary>
	/// The interface for wrapper of unmanaged memory block(s) of different <see cref="StorageLocation"/>(s) of type <typeparamref name="T"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TSelf">The actual class that implement <see cref="IStorage{T, TSelf}"/></typeparam>
	public interface IStorage<T, TSelf> : IStorage<TSelf>,
		IAdditiveIdentity<TSelf, long>, IAdditionOperators<TSelf, long, TSelf>, ISubtractionOperators<TSelf, long, TSelf>
		where T : unmanaged, INumber<T>
		where TSelf : class, IStorage<T, TSelf>
	{
		/// <summary>
		/// Get the total length of the presenting array in type <typeparamref name="T"/>. The default implementation uses <see cref="NativeType{T}.Size"/>.
		/// </summary>
		long Length => LengthInBytes / NativeType<T>.Size;

		/// <summary>
		/// When implemented by a derived class, check whether this <typeparamref name="TSelf"/> is valid or not after moving an <paramref name="offset"/> and set <see cref="Length"/> to <paramref name="newLength"/>.
		/// </summary>
		/// <param name="offset">The offset to move in <typeparamref name="T"/></param>
		/// <param name="newLength">The length to check in <typeparamref name="T"/>, default 0 means auto calculation by <paramref name="offset"/></param>
		/// <returns>The validness of this <typeparamref name="TSelf"/> under <paramref name="offset"/> and <paramref name="newLength"/></returns>
		/// <remarks>Default implementation utilizes <see cref="IStorage{TSelf}.IsByteOffsetValid(long, long)"/>.</remarks>
		bool IsOffsetValid(long offset, long newLength = 0) => IsByteOffsetValid(offset * NativeType<T>.Size, newLength * NativeType<T>.Size);

		/// <summary>
		/// When implemented by a derived class, make a referenced <typeparamref name="TSelf"/> with the starting pointer moving <paramref name="offset"/> and <see cref="Length"/> changing to <paramref name="newLength"/>.
		/// </summary>
		/// <param name="offset">The offset in <typeparamref name="T"/> to the starting pointer of this <typeparamref name="TSelf"/> as a <see cref="long"/></param>
		/// <param name="newLength">The new length in <typeparamref name="T"/> as a <see cref="long"/>, default 0 means automatically calculate from <paramref name="offset"/></param>
		/// <returns>A referenced <typeparamref name="TSelf"/> of this one</returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offset"/> and <paramref name="newLength"/> is out of boundary</exception>
		TSelf MakeReference(long offset = 0, long newLength = 0);

		/// <summary>
		/// When implemented by a derived class, statically get the distance in <typeparamref name="T"/> between two <typeparamref name="TSelf"/>s
		/// </summary>
		/// <param name="left">The left operand of type <typeparamref name="TSelf"/></param>
		/// <param name="right">The right operand of type <typeparamref name="TSelf"/></param>
		/// <returns>The distance between two <typeparamref name="TSelf"/>s in <typeparamref name="T"/> as a <see cref="long"/>.</returns>
		/// <exception cref="InvalidOperationException">If <paramref name="left"/> and <paramref name="right"/> have different origin.</exception>
		abstract static long operator -(TSelf left, TSelf right);

		/// <summary>
		/// Statically <b>allocate</b> and create a new <typeparamref name="TSelf"/> on given <paramref name="location"/> with corresponding <paramref name="length"/>.
		/// </summary>
		/// <param name="location">The given <see cref="StorageLocation"/> to create on</param>
		/// <param name="length">The corresponding length in <typeparamref name="T"/></param>
		/// <returns>The created new <typeparamref name="TSelf"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="length"/> ≤ 0</exception>
		/// <exception cref="OutOfMemoryException">If the underlying allocation failed due to insufficient memory</exception>
		/// <exception cref="InvalidOperationException">If underlying creation fails due to other reasons</exception>
		public static TSelf Create(StorageLocation location, long length)
		{
			Span<StorageLocation> locations = stackalloc StorageLocation[1];
			locations.SetValue(location);
			Span<long> lengths = stackalloc long[] { length };
			return TSelf.Create(CombinationType.PureOrMixed, locations, lengths);
		}

		/// <summary>
		/// When implemented by a derived class, statically <b>allocate</b> and create a new <typeparamref name="TSelf"/> of given <paramref name="combinationType"/> and given locations and lengths.
		/// </summary>
		/// <param name="combinationType">The given <see cref="CombinationType"/> to create</param>
		/// <param name="locations">The given <see cref="StorageLocation"/>s</param>
		/// <param name="lengths">The given lengths in <typeparamref name="T"/></param>
		/// <returns>The created new <typeparamref name="TSelf"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="locations"/> or <paramref name="lengths"/> is null or empty</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="lengths"/> has length(s) ≤ 0</exception>
		/// <exception cref="OutOfMemoryException">If the underlying allocation failed due to insufficient memory</exception>
		/// <exception cref="InvalidOperationException">If underlying creation fails due to other reasons</exception>
		abstract static TSelf Create(CombinationType combinationType, ReadOnlySpan<StorageLocation> locations, ReadOnlySpan<long> lengths);

		/// <summary>
		/// Check whether the given <paramref name="size"/> in <typeparamref name="T"/> can be casted without loss to <typeparamref name="TOut"/>
		/// </summary>
		/// <typeparam name="TOut">The output data type</typeparam>
		/// <param name="size">The size in <typeparamref name="T"/> to check</param>
		/// <param name="sizeInBytes">Whether <paramref name="size"/> is in bytes or in <typeparamref name="T"/></param>
		/// <returns>The <paramref name="size"/> (multiplies the size of <typeparamref name="T"/> then) divides the size of <typeparamref name="TOut"/></returns>
		/// <exception cref="InvalidCastException">if <paramref name="size"/>( multiplies the size of <typeparamref name="T"/>) cannot be divided by the size of <typeparamref name="TOut"/></exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static long CheckCast<TOut>(long size, bool sizeInBytes = false) where TOut : unmanaged, INumber<TOut>
		{
			long newSize = sizeInBytes ? size : (size * NativeType<T>.Size);
			if (size * NativeType<T>.Size % NativeType<TOut>.Size != 0)
				throw new InvalidCastException(Other.CannotDivide);
			newSize /= NativeType<TOut>.Size;
			return newSize;
		}
	}

	/// <summary>
	/// The interface for an actual storage of any data type
	/// </summary>
	/// <typeparam name="TStorage">The actual class that implement <see cref="IStorage{TSelf}"/></typeparam>
	public interface IActualStorage<TStorage> : IStorage<TStorage> where TStorage : class, IStorage<TStorage>
	{
		void IDisposable.Dispose()
		{
			if (this.IsValid())
				this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// When implemented by a derived class, actually unmanaged resources held by this <typeparamref name="TStorage"/>
		/// </summary>
		/// <param name="invokedByUser">Whether this method is invoked by user or by GC</param>
		protected abstract void Dispose(bool invokedByUser);
	}

	/// <summary>
	/// The interface for an actual storage of data type <typeparamref name="T"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TStorage">The actual class that implement <see cref="IStorage{T, TSelf}"/></typeparam>
	public interface IActualStorage<T, TStorage> : IStorage<T, TStorage>, IActualStorage<TStorage>
		where T : unmanaged, INumber<T> where
		TStorage : class, IStorage<T, TStorage>
	{
	}

	/// <summary>
	/// The interface for a referenced storage of any data type and storage type <typeparamref name="TStorage"/>
	/// </summary>
	/// <typeparam name="TStorage">The actual class that implement <see cref="IStorage{TStorage}"/></typeparam>
	public interface IReferenceStorage<TStorage> : IStorage<TStorage> where TStorage : class, IStorage<TStorage>
	{
		void IDisposable.Dispose()
		{
			// do nothing
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// When implemented by a derived class, get the referenced storage as a nullable <typeparamref name="TStorage"/>
		/// </summary>
		TStorage? Reference { get; }

		/// <summary>
		/// When implemented by a derived class, get the total offset compared to the start of <see cref="Reference"/> in bytes
		/// </summary>
		long TotalOffsetInBytes { get; }

		/// <summary>
		/// Create a referenced <typeparamref name="TStorage"/> with given reference <paramref name="storage"/> and <paramref name="offsetInBytes"/> to it
		/// </summary>
		/// <param name="storage">The <typeparamref name="TStorage"/> to be referenced</param>
		/// <param name="offsetInBytes">The total offset in bytes compared to <paramref name="storage"/> as a <see cref="long"/></param>
		/// <param name="newLengthInBytes">The new presenting length in bytes. A value less than or equals to 0 means the maximum possible value calculate from <paramref name="storage"/> and <paramref name="offsetInBytes"/></param>
		/// <returns>The reference as <typeparamref name="TStorage?"/> and the real total offset and length in bytes</returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offsetInBytes"/> and <paramref name="newLengthInBytes"/> is out of boundary</exception>
		protected static (TStorage? reference, long totalOffsetBytes, long lengthBytes) Create(TStorage? storage, long offsetInBytes = 0, long newLengthInBytes = 0)
		{
			if (storage is null)
				return default;
			// get offset and new length in bytes
			if (newLengthInBytes <= 0)
				newLengthInBytes = storage.LengthInBytes - offsetInBytes;
			// dereference first
			while (storage is IReferenceStorage<TStorage> @ref)
			{
				if (@ref.Reference is null)
					return default;
				storage = @ref.Reference;
				offsetInBytes += @ref.TotalOffsetInBytes;
			}
			// check
			if (offsetInBytes < 0)
				throw new ArgumentOutOfRangeException(nameof(offsetInBytes), offsetInBytes, Parameter.CannotNegative);
			if (storage.LengthInBytes != offsetInBytes + newLengthInBytes)
				throw new ArgumentOutOfRangeException(nameof(newLengthInBytes), newLengthInBytes, Parameter.InvalidValue);
			// return
			return (storage, offsetInBytes, newLengthInBytes);
		}
	}

	/// <summary>
	/// The interface for a referenced storage of data type <typeparamref name="T"/> and storage type <typeparamref name="TStorage"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TStorage">The actual class that implement <see cref="IStorage{T, TStorage}"/></typeparam>
	public interface IReferenceStorage<T, TStorage> : IStorage<T, TStorage>, IReferenceStorage<TStorage>
		where T : unmanaged, INumber<T>
		where TStorage : class, IStorage<T, TStorage>
	{
		/// <summary>
		/// Get the total offset compared to the start of the underlying reference in <typeparamref name="T"/>.
		/// </summary>
		/// <remarks>The default implementation does not check whether <see cref="IReferenceStorage{TStorage}.TotalOffsetInBytes"/> can be divided by <see cref="NativeType{T}.Size"/> or not.</remarks>
		long TotalOffset => TotalOffsetInBytes / NativeType<T>.Size;
	}
	#endregion
}
