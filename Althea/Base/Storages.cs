using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Althea.Helpers;
using Althea.Linq;
using Althea.Resources;


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
	/// The enum for the type of the description the combination of storage locations, each bit represents the type of its corresponding location in <see cref="CombinationOfLocations"/>
	/// </summary>
	[Flags]
	public enum CombinationType : short
	{
		/// <summary>
		/// Indicating that all locations are used as storage
		/// </summary>
		AllStored = 0,
	}

	/// <summary>
	/// Extension class for <see cref="CombinationType"/>
	/// </summary>
	public static class CombinationTypeExtension
	{
		/// <summary>
		/// Create a <see cref="CombinationType"/> from given information about whether the levels are caches or actual storages.
		/// </summary>
		/// <param name="levelAsCache">The input <see cref="Span{T}"/> of <see cref="bool"/> to indicate whether the levels are caches or actual storages</param>
		/// <returns>The created <see cref="CombinationType"/> from <paramref name="levelAsCache"/>.</returns>
		/// <exception cref="ArgumentException">If <paramref name="levelAsCache"/>'s length is too long to fit in a <see cref="CombinationType"/>.</exception>
		public static CombinationType CreateCombinationType(this Span<bool> levelAsCache) => CreateCombinationType((ReadOnlySpan<bool>)levelAsCache);

		/// <summary>
		/// Create a <see cref="CombinationType"/> from given information about whether the levels are caches or actual storages.
		/// </summary>
		/// <param name="levelAsCache">The input <see cref="ReadOnlySpan{T}"/> of <see cref="bool"/> to indicate whether the levels are caches or actual storages</param>
		/// <returns>The created <see cref="CombinationType"/> from <paramref name="levelAsCache"/>.</returns>
		/// <exception cref="ArgumentException">If <paramref name="levelAsCache"/>'s length is too long to fit in a <see cref="CombinationType"/>.</exception>
		public static CombinationType CreateCombinationType(this ReadOnlySpan<bool> levelAsCache)
		{
			if (levelAsCache.IsEmpty)
				return 0;
			if (levelAsCache.Length > sizeof(CombinationType) * 8)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(levelAsCache));
			CombinationType c = 0;
			for (int i = 0; i < levelAsCache.Length; i++)
			{
				if (levelAsCache[i])
					c |= (CombinationType)(1 << i);
			}
			return c;
		}

		/// <summary>
		/// Get the string representation of the given <see cref="CombinationType"/> under given <paramref name="length"/>.
		/// </summary>
		/// <param name="c">The given <see cref="CombinationType"/> to get string representation</param>
		/// <param name="length">The size of <paramref name="c"/></param>
		/// <returns>The name  string representation of <paramref name="c"/>.</returns>
		public static string GetName(this CombinationType c, int length)
		{
			int l = "Store".Length + 1;
			Span<char> chars = stackalloc char[length * l + 2];
			chars[0] = '{';
			for (int i = 0; i < length; i++)
			{
				if ((c & (CombinationType)(1 << i)) == 0)
					"Store".CopyTo(chars[(1 + l * i)..]);
				else
					"Cache".CopyTo(chars[(1 + l * i)..]);
			}
			chars[^1] = '}';
			return new(chars);
		}
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
			this.type = 0;
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
			return HashCode.Combine(this.type, span.HashCodeOfSet());
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

		IEnumerable<object?> IMainPropertyFormattable<CombinationOfLocations>.PropertyValues => new object[] { this.type.GetName(this.count), this.data.AsSpan(this.count).SpanJoin(',') };

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
		/// When implemented by derived classes, statically get the default value of <typeparamref name="TSelf"/>
		/// </summary>
		abstract static TSelf Default { get; }

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
		where T : notnull, IPointer<T>
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
		/// Create with given <see cref="PointerSegment{T}"/> <paramref name="pointer"/> and <paramref name="offset"/> and <paramref name="newLength"/> to the <paramref name="pointer"/>
		/// </summary>
		/// <param name="pointer">The <see cref="PointerSegment{T}"/> to copy info from</param>
		/// <param name="offset">The offset to the <paramref name="pointer"/> in bytes</param>
		/// <param name="newLength">The new presenting length in bytes, default 0 means automatically calculating from <paramref name="offset"/> and <see cref="IPointer{T}.LengthInBytes"/>. A value less than or equals to 0 means automatically calculate.</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offset"/> or <paramref name="newLength"/> exceeds the boundary</exception>
		public PointerSegment(PointerSegment<T> pointer, long offset = 0, long newLength = 0)
		{
			offset += pointer.OffsetInBytes;
			if (offset < 0)
				throw new ArgumentOutOfRangeException(nameof(offset), offset, Parameter.CannotNegative);
			long off = offset;
			if (off > pointer.Pointer.LengthInBytes)
				throw new ArgumentOutOfRangeException(nameof(offset), offset, Parameter.InvalidValue);
			if (newLength <= 0)
				newLength = pointer.Pointer.LengthInBytes - off;
			if (off + newLength > pointer.Pointer.LengthInBytes)
				throw new ArgumentOutOfRangeException(nameof(newLength), newLength, Parameter.InvalidValue);

			this.Pointer = pointer.Pointer; this.OffsetInBytes = off; this.LengthInBytes = newLength;
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

		/// <summary>
		/// Implicitly convert a pointer of type <typeparamref name="T"/> to a <see cref="PointerSegment{T}"/>
		/// </summary>
		public static implicit operator PointerSegment<T>(T pointer) => new(pointer);
		#endregion

		#region to string
		static string IMainPropertyFormattable<PointerSegment<T>>.StringMain => T.StringMain;
		static IEnumerable<string> IMainPropertyFormattable<PointerSegment<T>>.PropertyNames => new[] { nameof(Location), nameof(LengthInBytes), nameof(OffsetInBytes), nameof(Pointer) };
		IEnumerable<object?> IMainPropertyFormattable<PointerSegment<T>>.PropertyValues => new object[] { Location, this.LengthInBytes, this.OffsetInBytes, this.Pointer };

		/// <summary>
		/// Return the string representation of this <see cref="PointerSegment{T}"/>
		/// </summary>
		/// <returns>the string representation of this <see cref="PointerSegment{T}"/></returns>
		public override string ToString() => IMainPropertyFormattable<PointerSegment<T>>.ToString(in this);
		#endregion
	}

	#endregion
}
