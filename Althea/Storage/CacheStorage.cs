using System.Buffers;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Althea.Helpers;
using Althea.Linq;
using Althea.Resources;

using MEM = Althea.Storage.AbstractApi;


namespace Althea.Storage
{
	#region cache strategy
	/// <summary>
	/// Encapsulates a method that copies a block of memory from <paramref name="sourceOffset"/> to <paramref name="destinationOffset"/> of <paramref name="copyLength"/> with copy direction indicated by <paramref name="copyToCache"/>.
	/// </summary>
	/// <param name="sourceOffset">The source offset in bytes</param>
	/// <param name="destinationOffset">The destination offset in bytes</param>
	/// <param name="copyLength">The copy length in bytes</param>
	/// <param name="copyToCache">If true, copy from actual storage to cache; otherwise, copy from cache to actual storage</param>
	/// <return>Whether the encapsulated method succeed or not</return>
	public delegate bool CopyDelegate(long sourceOffset, long destinationOffset, long copyLength, bool copyToCache);

	/// <summary>
	/// The interface for caching strategy of a two-level caching system
	/// </summary>
	/// <typeparam name="TSelf">The actual unmanaged struct that implements <see cref="ICacheStrategy{TSelf}"/></typeparam>
	public interface ICacheStrategy<TSelf> : IEqualityOperators<TSelf, TSelf>, ICheckValid where TSelf : struct, ICacheStrategy<TSelf>
	{
		/// <summary>
		/// When implemented by a derived struct, statically create a <typeparamref name="TSelf"/> and get the size of the cache level in bytes with given size of the low speed level.
		/// </summary>
		/// <param name="actualStorageInBytes">The given size of the low speed level in bytes</param>
		/// <param name="cacheSizeInBytes">Output the size of the cache level in bytes with respect to <paramref name="actualStorageInBytes"/></param>
		/// <returns>The created <typeparamref name="TSelf"/>.</returns>
		abstract static TSelf Create(long actualStorageInBytes, out long cacheSizeInBytes);

		/// <summary>
		/// When implemented by a derived struct, get the size of one cache line in bytes.
		/// </summary>
		int CacheLineSize { get; }

		/// <summary>
		/// When implemented by a derived struct, calculate the cache offset of given actual storage offset, copy data if necessary, and update internal information of this <typeparamref name="TSelf"/>.
		/// </summary>
		/// <param name="storageOffset">The offset of actual storage in bytes which is the location of the byte of interest</param>
		/// <param name="copy">The <see cref="CopyDelegate"/> used to copy data if necessary</param>
		/// <param name="intentWrite">Whether the intent of using this piece of cache is to write (true) or read (false)</param>
		/// <returns>The offsets to the cache pointer in bytes which contains the requested byte.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		long GetCacheOf(long storageOffset, CopyDelegate copy, bool intentWrite);
	}

	/// <summary>
	/// The direct mapping cache strategy as the default strategy as well as a demonstration
	/// </summary>
	public struct DirectMappingStrategy : ICacheStrategy<DirectMappingStrategy>, IEqualityOperators<DirectMappingStrategy, DirectMappingStrategy>
	{
		#region basic
		[StructLayout(LayoutKind.Explicit)]
		private struct CacheLineInfo : IEqualityOperators<CacheLineInfo, CacheLineInfo>, IEquatable<CacheLineInfo>
		{
			[FieldOffset(0)] // little-endian
			private byte dirtyAndValid;
			[FieldOffset(0)]
			private ulong tag;

			private const int TAG_OFFSET = 8;
			public const int MAX_TAG_BITS = 64 - TAG_OFFSET;

			public bool Dirty
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => (this.dirtyAndValid & 0b01) == 1;
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				set
				{
					if (value)
						this.dirtyAndValid |= 1;
					else
						this.dirtyAndValid &= 0xfe;
				}
			}

			public bool Valid
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => (dirtyAndValid & 0b10) == 1;
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				set
				{
					if (value)
						this.dirtyAndValid |= 2;
					else
						this.dirtyAndValid &= 0xfd;
				}
			}

			public long Tag
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => (long)(tag >> TAG_OFFSET);
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				set
				{
					byte oldDV = dirtyAndValid;
					this.tag = (ulong)(value << TAG_OFFSET);
					this.dirtyAndValid = oldDV;
				}
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public CacheLineInfo(bool dirty, bool valid, long tag)
			{
				this.tag = (ulong)(tag << TAG_OFFSET);
				this.dirtyAndValid = (byte)((dirty ? 1 : 0) | (valid ? 2 : 0));
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public bool Equals(CacheLineInfo other) => this.tag == other.tag;

			public static bool operator ==(CacheLineInfo left, CacheLineInfo right) => left.Equals(right);

			public static bool operator !=(CacheLineInfo left, CacheLineInfo right) => !left.Equals(right);

			public override bool Equals(object? obj) => obj is CacheLineInfo info && this.Equals(info);

			public override int GetHashCode() => this.tag.GetHashCode();
		}

		private readonly byte lineSizeLog2, nLinesLog2;

		private readonly ushort withinLineMask;

		private readonly int lineAddressMask;

		private readonly CacheLineInfo[] lineInfo;

		/// <summary>
		/// Get the size of one cache line in bytes.
		/// </summary>
		public int CacheLineSize => 1 << lineSizeLog2;

		private DirectMappingStrategy(byte cacheLineSizeLog2, byte nCacheLinesLog2)
		{
			this.lineSizeLog2 = cacheLineSizeLog2;
			this.nLinesLog2 = nCacheLinesLog2;
			this.withinLineMask = (ushort)(1 << (cacheLineSizeLog2 + 1) - 1);
			this.lineAddressMask = (1 << (cacheLineSizeLog2 + nCacheLinesLog2 + 1) - 1) & ~this.withinLineMask;
			this.lineInfo = new CacheLineInfo[1 << nCacheLinesLog2];
		}

		/// <summary>
		/// Statically create a <see cref="DirectMappingStrategy"/> and get the size of the cache level in bytes with given size of the low speed level.
		/// </summary>
		/// <param name="actualStorageInBytes">The given size of the low speed level in bytes</param>
		/// <param name="cacheSizeInBytes">Output the size of the cache level in bytes with respect to <paramref name="actualStorageInBytes"/></param>
		/// <returns>The created <see cref="DirectMappingStrategy"/>.</returns>
		/// <remarks>The cache strategy created has at most 8KiB cache line size, the same as internal file buffer size of CLR.</remarks>
		public static DirectMappingStrategy Create(long actualStorageInBytes, out long cacheSizeInBytes)
		{
			const int MAX_CACHE_LINE_BITS = 13; // 8KiB
			int addressBits = (int)actualStorageInBytes.CeilLog2();
			if (addressBits <= MAX_CACHE_LINE_BITS)
			{
				byte bits = (byte)actualStorageInBytes.Log2();
				cacheSizeInBytes = 1 << bits;
				return new(bits, (byte)(actualStorageInBytes > cacheSizeInBytes ? 2 : 1));
			}
			cacheSizeInBytes = 1 << MAX_CACHE_LINE_BITS;
			return new(MAX_CACHE_LINE_BITS, (byte)((actualStorageInBytes + 1) >> MAX_CACHE_LINE_BITS).CeilLog2());
		}


		/// <summary>
		/// Calculate the cache offset of given actual storage offset, copy data if necessary, and update internal information of this <see cref="DirectMappingStrategy"/>.
		/// </summary>
		/// <param name="storageOffset">The offset of actual storage in bytes which is the location of the byte of interest</param>
		/// <param name="copy">The <see cref="CopyDelegate"/> used to copy data if necessary</param>
		/// <param name="intentWrite">Whether the intent of using this piece of cache is to write (true) or read (false)</param>
		/// <returns>The offsets to the cache pointer in bytes which contains the requested byte.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public long GetCacheOf(long storageOffset, CopyDelegate copy, bool intentWrite)
		{
			long tag = storageOffset >> (this.lineSizeLog2 + this.nLinesLog2);
			int cacheLine = (int)storageOffset & this.lineAddressMask;
			int withinLineOffset = (int)storageOffset & this.withinLineMask;
			long cacheOffset = cacheLine << this.lineSizeLog2;
			var info = this.lineInfo[cacheLine];
			if (!info.Valid)
			{   // cache empty
				goto SWAP_IN;
			}
			if (info.Tag == tag)
			{   // cache hit
				info.Dirty &= intentWrite;
				this.lineInfo[cacheLine] = info;
				return cacheOffset + withinLineOffset;
			}
			// cache miss
			if (info.Dirty)
			{   // swap out
				copy(cacheOffset, storageOffset, 1 << this.lineSizeLog2, false);
			}
		SWAP_IN:
			this.lineInfo[cacheLine] = new(false, true, tag);
			copy(storageOffset, cacheOffset, 1 << this.lineSizeLog2, true);
			return cacheOffset + withinLineOffset;
		}

		/// <summary>
		/// Check whether this <see cref="DirectMappingStrategy"/> is a valid one or not
		/// </summary>
		/// <returns>The validness of this <see cref="DirectMappingStrategy"/></returns>
		public bool IsValid() => this.lineSizeLog2 > 0;
		#endregion

		#region equality
		/// <summary>
		/// Check whether this <see cref="DirectMappingStrategy"/> is the same as the <paramref name="other"/> one
		/// </summary>
		/// <param name="other">The other <see cref="DirectMappingStrategy"/> to compare</param>
		/// <returns>this == <paramref name="other"/> or not</returns>
		public bool Equals(DirectMappingStrategy other) => this.lineSizeLog2 == other.lineSizeLog2 && this.lineInfo.SequenceEqual(other.lineInfo);

		/// <summary>
		/// Equality operator
		/// </summary>
		public static bool operator ==(DirectMappingStrategy left, DirectMappingStrategy right) => left.Equals(right);

		/// <summary>
		/// Inequality operator
		/// </summary>
		public static bool operator !=(DirectMappingStrategy left, DirectMappingStrategy right) => !left.Equals(right);

		/// <summary>
		/// Check whether this <see cref="DirectMappingStrategy"/> is the same as the other object
		/// </summary>
		/// <param name="obj">The other <see cref="object"/> to compare</param>
		/// <returns>this == <paramref name="obj"/> or not</returns>
		public override bool Equals(object? obj) => obj is DirectMappingStrategy strategy && this.Equals(strategy);

		/// <summary>
		/// Get the hash code of this <see cref="DirectMappingStrategy"/>
		/// </summary>
		/// <returns>The hash code</returns>
		public override int GetHashCode() => HashCode.Combine(this.lineSizeLog2, this.lineInfo.HashCodeOfArray());
		#endregion
	}
	#endregion

	/// <summary>
	/// The abstract storage class as a base class for all single level caching storage classes whose <see cref="IStorage.LocationDescription"/>.<see cref="CombinationOfLocations.Count">Count</see> == 2.
	/// </summary>
	/// <typeparam name="TS">Any cache strategy struct which implements <see cref="ICacheStrategy{TSelf}"/></typeparam>
	/// <typeparam name="TPh">Any high speed pointer type which implements <see cref="IPointer{TSelf}"/></typeparam>
	/// <typeparam name="TPl">Any low speed pointer type which implements <see cref="IPointer{TSelf}"/></typeparam>
	/// <remarks>This class only servers as a type identifier which can not be used directly</remarks>
	public abstract class CachedStorageBase<TS, TPh, TPl>
		where TS : struct, ICacheStrategy<TS>
		where TPh : notnull, IPointer<TPh>
		where TPl : notnull, IPointer<TPl>
	{
		/// <summary>
		/// When implemented by a derived struct, get the cache strategy of this <see cref="CachedStorageBase{TS, TPh, TPl}"/>
		/// </summary>
		public abstract TS Strategy { get; }

		/// <summary>
		/// Get the high speed cache <see cref="PointerSegment{T}"/> of this <see cref="CachedStorageBase{TS, TPh, TPl}"/>
		/// </summary>
		public PointerSegment<TPh> Cache { get; }

		/// <summary>
		/// Get the low speed actual storage <see cref="PointerSegment{T}"/> of this <see cref="CachedStorageBase{TS, TPh, TPl}"/>
		/// </summary>
		public PointerSegment<TPl> Actual { get; }

		/// <summary>
		/// Create a new <see cref="CachedStorageBase{TS, TPh, TPl}"/> with given cache and actual <see cref="PointerSegment{T}"/>s
		/// </summary>
		/// <param name="cache">The <see cref="PointerSegment{T}"/> of type <typeparamref name="TPh"/> as cache pointer</param>
		/// <param name="actual">The <see cref="PointerSegment{T}"/> of type <typeparamref name="TPl"/> as actual storage pointer</param>
		protected CachedStorageBase(PointerSegment<TPh> cache, PointerSegment<TPl> actual)
		{
			this.Cache = cache;
			this.Actual = actual;
		}
	}

	/// <summary>
	/// The interface for any cached storage, including actual ones and referenced one
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TSelf">The actual class that implement <see cref="IStorage{T, TSelf}"/></typeparam>
	public interface ICachedStorage<T, TSelf> : IStorage<T, TSelf> where T : unmanaged, INumber<T> where TSelf : class, ICachedStorage<T, TSelf>
	{
		#region new methods
		/// <summary>
		/// When implemented by a derived class, get the cache sizes in bytes of the top level as a <see cref="long"/>.
		/// </summary>
		long TopCacheSizeInBytes { get; }

		/// <summary>
		/// Statically get the number of total caching levels, include the actual storage level. The default implementation is <see cref="IStorage.LocationDescription"/>.<see cref="CombinationOfLocations.Count">Count</see>.
		/// </summary>
		public static int CacheLevels => TSelf.LocationDescription.Count;

		/// <summary>
		/// When implemented by a derived class, get the whole caching level at <paramref name="index"/> as a <see cref="PointerSegment"/>
		/// </summary>
		/// <param name="index">The index to indicate the cache level</param>
		/// <returns>The whole caching level at <paramref name="index"/> as a <see cref="PointerSegment"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="index"/> is out of range</exception>
		PointerSegment GetCacheLevel(int index);

		/// <summary>
		/// Encapsulates a method that copies a <see cref="PointerSegment"/> from <paramref name="source"/> to another <see cref="PointerSegment"/> <paramref name="destination"/>
		/// </summary>
		/// <param name="source">The source <see cref="PointerSegment"/> to copy from</param>
		/// <param name="destination">The destination <see cref="PointerSegment"/> to copy to</param>
		/// <param name="copied">Output the actual number of bytes copied</param>
		/// <return>Whether the encapsulated method support such copy or not</return>
		public delegate bool CopyDelegate(PointerSegment source, PointerSegment destination, out long copied);

		/// <summary>
		/// When implemented by a derived class, retrieve some part of the data delimited by <paramref name="lengthInBytes"/> and <paramref name="totalOffsetInBytes"/> via promoting them to the highest caching level.
		/// </summary>
		/// <param name="totalOffsetInBytes">The total offset (in bytes) in the lowest caching level (the cache level where all the data preserves)</param>
		/// <param name="lengthInBytes">The length to retrieve (in bytes), default 0 means retrieve as much as possible</param>
		/// <param name="copy">The <see cref="CopyDelegate"/> used to copy data between caching levels. The default null value will be replaced by the <see cref="MEM.MemoryCopy(PointerSegment, PointerSegment)"/> of <see cref="MEM.Current"/>.</param>
		/// <returns>The <see cref="PointerSegment"/> of the highest caching level containing the required data</returns>
		/// <exception cref="NotSupportedException">If <paramref name="copy"/> returns false</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="totalOffsetInBytes"/> + <paramref name="lengthInBytes"/> is out of the boundary, or <paramref name="lengthInBytes"/> is larger than <see cref="TopCacheSizeInBytes"/></exception>
		/// <remarks>
		/// Typically, the methods in an instance of <see cref="MEM"/> will invoke this method with <paramref name="copy"/> set to the  correct internal method <see cref="MEM.MemoryCopy_(PointerSegment, PointerSegment, out long)"/>.<br/>
		/// Some caching strategies and algorithms (such as the ones utilized by modern computers) shall be used to improve performance.<br/>
		/// It is not necessary to write the data in the higher caching level back to the lower one immediately while it is necessary if some new data are retrieved.
		/// </remarks>
		PointerSegment Retrieve(long totalOffsetInBytes, long lengthInBytes = 0, CopyDelegate? copy = null);

		/// <summary>
		/// When implemented by a derived class, update all the data in the lowest caching level by causing all caching levels to fall back to the lower ones.
		/// </summary>
		/// <param name="copy">See <see cref="Retrieve(long, long, CopyDelegate?)"/></param>
		void Flush(CopyDelegate? copy = null);
		#endregion

		#region override
		string IMainPropertyFormattable.StringMain
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => ((IMainPropertyFormattable)this[0]).StringMain;
		}

		IEnumerable<KeyValuePair<string, object?>> IMainPropertyFormattable.StringProperties
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				int count = this.CacheLevels - 1;
				var dict = new Dictionary<string, object?>(2 + count)
				{
					[nameof(DataType)] = string.Join(", ", this.GetType().GenericTypeArguments.Select(static t => t.GetGenericString()).ToArray()),
					[nameof(this.Length)] = this.Length,
				};
				for (int i = 0; i < count; i++)
				{
					dict.Add($"CacheLevel_{i}", this.GetCacheLevel(i));
				}
				return dict;
			}
		}
		#endregion
	}

	/// <summary>
	/// An abstract class which represents a storage of several contiguous memory blocks on different memory locations with variable sizes purposed to cache memories of higher performance. Inherits <see cref="ActualStorage{T}"/>.
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
	public abstract class CachedStorage<T> : ActualStorage<T>, ICachedStorage where T : unmanaged
	{
		#region basic
		/// <summary>
		/// The description of the storage locations of this storage class as a <see cref="CombinationOfLocations"/>
		/// </summary>
		public override CombinationOfLocations LocationDescription { get; }

		/// <summary>
		/// Get the number of total caching levels, include the actual storage level.
		/// </summary>
		public int CacheLevels
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.LocationDescription.Count;
		}

		/// <summary>
		/// The cache sizes in bytes of the top level as a <see cref="long"/>.
		/// </summary>
		public long TopCacheSizeInBytes { get; }

		/// <summary>
		/// Create (without allocation) a <see cref="CachedStorage{T}"/> of given <see cref="StorageLocation"/>s and <see cref="long"/>s as priorities and total length (<see cref="Storage{T}.Length"/>) in <typeparamref name="T"/>
		/// </summary>
		/// <param name="locations">The <see cref="ReadOnlySpan{T}"/> of <see cref="StorageLocation"/> to represent the caching levels from higher-performance ones to lower ones. It cannot contain any duplicate values or has size less than 2.</param>
		/// <param name="maxLengths">The <see cref="ReadOnlySpan{T}"/> of <see cref="long"/> to represent the maximum size of each caching levels. The last value is the actual length in <typeparamref name="T"/> It must be of same size as <paramref name="locations"/>.</param>
		/// <exception cref="ArgumentNullException">If the sizes of <paramref name="locations"/> or <paramref name="maxLengths"/> is 0</exception>
		/// <exception cref="ArgumentException">If <paramref name="locations"/> is of wrong size or has duplicate value(s) or is of wrong size; or if <paramref name="maxLengths"/> is of wrong size or has non-increase cache size</exception>
		/// <exception cref="ArgumentOutOfRangeException">If any length in <paramref name="maxLengths"/> is 0</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected CachedStorage(ReadOnlySpan<StorageLocation> locations, ReadOnlySpan<long> maxLengths) : base(maxLengths[^1])
		{
			if (locations.Length <= 1)
				throw new ArgumentException(Parameter.WrongSize, nameof(locations));
			if (maxLengths.Length <= 1)
				throw new ArgumentException(Parameter.WrongSize, nameof(maxLengths));
			if (maxLengths.Length != locations.Length)
				throw new ArgumentException(Parameter.NotSameSize);
			if (!locations.ElementsUnique())
				throw new ArgumentException(Parameter.DuplicateValue, nameof(locations));
			if (maxLengths.Any(static l => l <= 0))
				throw new ArgumentOutOfRangeException(nameof(maxLengths), maxLengths.ToArray(), Parameter.MustPositive);
			// check ratios
			for (int i = 1; i < locations.Length; i++)
			{
				if (maxLengths[i] <= maxLengths[i - 1])
					throw new ArgumentException(Parameter.InvalidValue, nameof(maxLengths));
				else if (maxLengths[i] / maxLengths[i - 1] < ICachedStorage.CacheSizeRatio)
					Helpers.Log.Write(Other.CacheSizeRatioSmall, level: Helpers.LogLevel.Warning);
			}
			this.LocationDescription = new CombinationOfLocations(CombinationType.Cached, locations);
			this.TopCacheSizeInBytes = maxLengths[0] * Const<T>.SizeT;
		}
		#endregion

		#region override
		/// <summary>
		/// The function that actually dispose this storage, override <see cref="Storage{T}.Dispose(bool)"/>
		/// </summary>
		/// <param name="invokedByUser">Whether this method is invoked by user or by GC</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override void Dispose(bool invokedByUser)
		{
			for (int i = 0; i < this.CacheLevels; i++)
			{
				var ptr = this.GetCacheLevel(i);
				if (ptr.IsValid())
					MEM.Free(ptr, invokedByUser);
			}
		}

		/// <summary>
		/// The number of <see cref="PointerSegment"/>(s) of this <see cref="Storage{T}"/>, always return 1
		/// </summary>
		public override int Count
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => 1;
		}

		/// <summary>
		/// Indexer of the <see cref="PointerSegment"/>(s) of this <see cref="Storage{T}"/> (in presenting order)
		/// </summary>
		/// <param name="index">The element index, must be 0</param>
		/// <returns>the <see cref="PointerSegment"/> at <paramref name="index"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="index"/> is out of range</exception>
		/// <remarks>You can <b>only</b> modify the data of the result of this indexer <b>right after</b> calling <see cref="Flush"/>. Otherwise, it may cause unexpected results.</remarks>
		public override PointerSegment this[int index]
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				if (index != 0)
					throw new ArgumentOutOfRangeException(nameof(index), index, Parameter.InvalidValue);
				return this.GetCacheLevel(this.CacheLevels - 1);
			}
		}

		/// <summary>
		/// Get the actual <see cref="PointerSegment"/> at the actual index (the index in <see cref="LocationDescription"/>) <paramref name="i"/>
		/// </summary>
		/// <param name="i">The actual index</param>
		/// <returns>The actual <see cref="PointerSegment"/> at <paramref name="i"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected override PointerSegment GetActualPointerAt(int i) => this.GetCacheLevel(i);

		/// <summary>
		/// Get the hash code of this <see cref="CachedStorage{T}"/>.
		/// </summary>
		/// <returns>the hash code</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode()
		{
			int count = this.CacheLevels;
			int hc = count;
			for (int i = 0; i < count; ++i)
			{   // CRC
				var a = this.GetCacheLevel(i);
				hc = unchecked(hc * ArrayLinq.CRC_CONST + a.GetHashCode());
			}
			return hc;
		}

		/// <summary>
		/// Make a <see cref="CachedReferenceStorage{T}"/> with the starting pointer moving <paramref name="offset"/> and <see cref="Storage{T}.Length"/> changing to <paramref name="newLength"/>.
		/// </summary>
		/// <param name="offset">The offset in <typeparamref name="T"/> to the starting pointer of this <see cref="Storage{T}"/> as a <see cref="long"/></param>
		/// <param name="newLength">The new length in <typeparamref name="T"/> as a <see cref="long"/>, default 0 means automatically calculate from <paramref name="offset"/></param>
		/// <returns>A <see cref="CachedReferenceStorage{T}"/> of this one</returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offset"/> and <paramref name="newLength"/> is out of boundary</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override CachedReferenceStorage<T> MakeReference(long offset = 0, long newLength = 0)
		{
			return new CachedReferenceStorage<T>(this, offset, newLength);
		}

		/// <summary>
		/// Convert this <see cref="CachedReferenceStorage{T}"/> to another one with different data type <typeparamref name="TOut"/>
		/// </summary>
		/// <typeparam name="TOut">The output data type</typeparam>
		/// <returns>A <see cref="CachedReferenceStorage{TOut}"/></returns>
		/// <exception cref="InvalidCastException">if <see cref="IStorage.LengthInBytes"/> cannot be divided by <see cref="Const{TOut}.SizeT"/></exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override CachedReferenceStorage<TOut> As<TOut>()
		{
			long newLength = CheckCast<TOut>(this.Length);
			return new CachedReferenceStorage<TOut>(this, newLength: newLength);
		}
		#endregion

		#region new methods
		/// <summary>
		/// When implemented by a derived class, get the whole caching level at <paramref name="index"/> as a <see cref="PointerSegment"/>
		/// </summary>
		/// <param name="index">The index to indicate the cache level</param>
		/// <returns>The whole caching level at <paramref name="index"/> as a <see cref="PointerSegment"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="index"/> is out of range</exception>
		public abstract PointerSegment GetCacheLevel(int index);

		/// <summary>
		/// When implemented by a derived class, retrieve some part of the data delimited by <paramref name="lengthInBytes"/> and <paramref name="totalOffsetInBytes"/> via promoting them to the highest caching level.
		/// </summary>
		/// <param name="totalOffsetInBytes">The total offset (in bytes) in the lowest caching level (the cache level where all the data preserves)</param>
		/// <param name="lengthInBytes">The length to retrieve (in bytes), default 0 means retrieve as much as possible</param>
		/// <param name="copy">The <see cref="ICachedStorage.CopyDelegate"/> used to copy data between caching levels. The default null value will be replaced by the <see cref="MEM.MemoryCopy(PointerSegment, PointerSegment)"/>.</param>
		/// <exception cref="NotSupportedException">If <paramref name="copy"/> returns false</exception>
		/// <returns>The <see cref="PointerSegment"/> of the highest caching level containing the required data</returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="totalOffsetInBytes"/> + <paramref name="lengthInBytes"/> is out of the boundary, or <paramref name="lengthInBytes"/> is larger than <see cref="TopCacheSizeInBytes"/></exception>
		/// <remarks>
		/// Typically, the methods in an instance of <see cref="MEM"/> will invoke this method with <paramref name="copy"/> set to correct internal method <see cref="MEM.MemoryCopy_(PointerSegment, PointerSegment, out long)"/>.<br/>
		/// Some caching strategies and algorithms (such as the ones utilized by modern computers) shall be used to improve performance.<br/>
		/// It is not necessary to write the data in the higher caching level back to the lower one immediately while it is necessary if some new data are retrieved.
		/// </remarks>
		public abstract PointerSegment Retrieve(long totalOffsetInBytes, long lengthInBytes = 0, ICachedStorage.CopyDelegate? copy = null);

		/// <summary>
		/// When implemented by a derived class, update all the data in the lowest caching level by causing all caching levels to fall back to the lower ones.
		/// </summary>
		/// <param name="copy">See <see cref="Retrieve(long, long, ICachedStorage.CopyDelegate?)"/></param>
		public abstract void Flush(ICachedStorage.CopyDelegate? copy = null);
		#endregion
	}

	/// <summary>
	/// The storage class that references to a <see cref="CachedStorage{T}"/>, implements <see cref="ReferenceStorage{T}"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
	public class CachedReferenceStorage<T> : ReferenceStorage<T>, IReferenceStorage, ICachedStorage where T : unmanaged
	{
		#region basic
		/// <summary>
		/// The cache sizes in bytes of the top level as a <see cref="long"/>.
		/// </summary>
		public long TopCacheSizeInBytes
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.Reference is ICachedStorage c ? c.TopCacheSizeInBytes : 0;
		}

		/// <summary>
		/// Get the number of <see cref="PointerSegment"/>(s) of this <see cref="Storage{T}"/> 
		/// </summary>
		public override int Count
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.Reference is null ? 0 : 1;
		}

		/// <summary>
		/// Get the description of the storage locations of this <see cref="Storage{T}"/> class as a <see cref="CombinationOfLocations"/>
		/// </summary>
		public override CombinationOfLocations LocationDescription
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.Reference?.LocationDescription ?? default;
		}

		/// <summary>
		/// Get the number of total caching levels, include the actual storage level.
		/// </summary>
		public int CacheLevels
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.Reference is ICachedStorage c ? c.CacheLevels : 0;
		}

		/// <summary>
		/// Create a <see cref="CachedReferenceStorage{T}"/> with given reference <paramref name="storage"/> and <paramref name="offset"/> to it
		/// </summary>
		/// <param name="storage">The <see cref="CachedStorage{T}"/> to be referenced</param>
		/// <param name="offset">The total offset in <typeparamref name="T"/> as a <see cref="long"/></param>
		/// <param name="newLength">The new presenting length in <typeparamref name="T"/>, default 0 means automatically calculate by <paramref name="storage"/> and <paramref name="offset"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="storage"/> or its reference is null</exception>
		/// <exception cref="ArgumentException">If <paramref name="storage"/> is not a <see cref="CachedStorage{T}"/></exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offset"/> and <paramref name="newLength"/> is out of boundary</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public CachedReferenceStorage(IStorage? storage, long offset = 0, long newLength = 0) : base(storage, offset, newLength)
		{
			if (this.Reference is null)
				return;
			// check 
			if (this.Reference is not CachedStorage<T> && !this.Reference.GetType().MakeGenericType(typeof(T)).IsAssignableTo(typeof(CachedStorage<T>)))
				throw new ArgumentException(Parameter.UnexpectedType, nameof(storage));
		}

		/// <summary>
		/// When implemented by a derived class, get the whole caching level at <paramref name="index"/> as a <see cref="PointerSegment"/>
		/// </summary>
		/// <param name="index">The index to indicate the cache level</param>
		/// <returns>The whole caching level at <paramref name="index"/> as a <see cref="PointerSegment"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="index"/> is out of range</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public PointerSegment GetCacheLevel(int index)
		{
			if (this.Reference is ICachedStorage c)
				return c.GetCacheLevel(index);
			else
				return default;
		}
		#endregion

		#region override
		/// <summary>
		/// Indexer of the <see cref="PointerSegment"/>(s) of this <see cref="CachedReferenceStorage{T}"/> (in presenting order)
		/// </summary>
		/// <param name="index">The element index</param>
		/// <returns>the <see cref="PointerSegment"/> at <paramref name="index"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="index"/> is out of the range</exception>
		/// <exception cref="InvalidOperationException">If the referenced storage of this <see cref="CachedReferenceStorage{T}"/> is null</exception>
		/// <remarks>You can <b>only</b> modify the data of the result of this indexer <b>right after</b> calling <see cref="Flush"/>. Otherwise, it may cause unexpected results.</remarks>
		public override PointerSegment this[int index]
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				if (index != 0)
					throw new ArgumentOutOfRangeException(nameof(index), index, Parameter.InvalidValue);
				if (this.Reference is null)
					throw new InvalidOperationException();
				return this.Reference[0].MoveBy(this.TotalOffsetInBytes, this.LengthInBytes);
			}
		}

		/// <summary>
		/// Make a <see cref="ReferenceStorage{T}"/> with the starting pointer moving <paramref name="offset"/> and <see cref="Storage{T}.Length"/> changing to <paramref name="newLength"/>.
		/// </summary>
		/// <param name="offset">The offset in <typeparamref name="T"/> to the starting pointer of this <see cref="Storage{T}"/> as a <see cref="long"/></param>
		/// <param name="newLength">The new length in <typeparamref name="T"/> as a <see cref="long"/>, default 0 means automatically calculate from <paramref name="offset"/></param>
		/// <returns>A <see cref="CachedReferenceStorage{T}"/> of this one</returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offset"/> and <paramref name="newLength"/> is out of boundary</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override CachedReferenceStorage<T> MakeReference(long offset = 0, long newLength = 0)
		{
			return new CachedReferenceStorage<T>(this, offset, newLength);
		}

		/// <summary>
		/// Convert this <see cref="CachedReferenceStorage{T}"/> to another one with different data type <typeparamref name="TOut"/>
		/// </summary>
		/// <typeparam name="TOut">The output data type</typeparam>
		/// <returns>a referenced <see cref="CachedReferenceStorage{TOut}"/></returns>
		/// <exception cref="InvalidCastException">if <see cref="IStorage.LengthInBytes"/> cannot be divided by <see cref="Const{TOut}.SizeT"/></exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override CachedReferenceStorage<TOut> As<TOut>()
		{
			if (this.Reference is null)
				return new CachedReferenceStorage<TOut>(null, 0, 0);
			long offset = CheckCast<TOut>(this.TotalOffsetInBytes, sizeInBytes: true);
			long length = CheckCast<TOut>(this.Reference.LengthInBytes - this.TotalOffsetInBytes, sizeInBytes: true);
			return new CachedReferenceStorage<TOut>(this.Reference, offset, length);
		}

		/// <summary>
		/// Retrieve some part of the data delimited by <paramref name="lengthInBytes"/> and <paramref name="totalOffsetInBytes"/> via promoting them to the highest caching level.
		/// </summary>
		/// <param name="totalOffsetInBytes">The total offset (in bytes) in the lowest caching level (the cache level where all the data preserves)</param>
		/// <param name="lengthInBytes">The length to retrieve (in bytes), default 0 means retrieve as much as possible</param>
		/// <param name="copy">The <see cref="ICachedStorage.CopyDelegate"/> used to copy data between caching levels. The default null value will be replaced by the <see cref="MEM.MemoryCopy(PointerSegment, PointerSegment)"/>.</param>
		/// <returns>The <see cref="PointerSegment"/> of the highest caching level containing the required data</returns>
		/// <exception cref="NotSupportedException">If <paramref name="copy"/> returns false</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="totalOffsetInBytes"/> + <paramref name="lengthInBytes"/> is out of the boundary, or <paramref name="lengthInBytes"/> is larger than <see cref="TopCacheSizeInBytes"/></exception>
		/// <remarks>This method utilizes the <see cref="ICachedStorage.Retrieve(long, long, ICachedStorage.CopyDelegate?)"/> of <see cref="ReferenceStorage{T}.Reference"/></remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public PointerSegment Retrieve(long totalOffsetInBytes, long lengthInBytes = 0, ICachedStorage.CopyDelegate? copy = null)
		{
			if (this.Reference is not ICachedStorage c)
				return default;
			if (totalOffsetInBytes + lengthInBytes > this.LengthInBytes)
				throw new ArgumentOutOfRangeException(nameof(totalOffsetInBytes), totalOffsetInBytes, Parameter.InvalidValue);
			if (lengthInBytes > this.TopCacheSizeInBytes)
				throw new ArgumentOutOfRangeException(nameof(lengthInBytes), lengthInBytes, Parameter.InvalidValue);
			return c.Retrieve(this.TotalOffsetInBytes + totalOffsetInBytes, lengthInBytes, copy);
		}

		/// <summary>
		/// Update all the data in the lowest caching level by causing all caching levels to fall back to the lower ones.
		/// </summary>
		/// <param name="copy">See <see cref="Retrieve(long, long, ICachedStorage.CopyDelegate?)"/>.</param>
		/// <remarks>This method utilizes the <see cref="ICachedStorage.Flush(ICachedStorage.CopyDelegate?)"/> of <see cref="ReferenceStorage{T}.Reference"/></remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Flush(ICachedStorage.CopyDelegate? copy = null)
		{
			(this.Reference as ICachedStorage)?.Flush(copy);
		}

		/// <summary>
		/// Get the hash code of this <see cref="ReferenceStorage{T}"/>
		/// </summary>
		/// <returns>the hash code</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode() => HashCode.Combine(this.Reference, this.TotalOffsetInBytes, this.Length);
		#endregion
	}

	/// <summary>
	/// The storage class that caches a stream storage to a memory storage, implements <see cref="CachedStorage{T}"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
	public class StreamToMemoryCachedStorage<T> : CachedStorage<T> where T : unmanaged
	{
		#region basic
		/// <summary>
		/// Get whether the given <see cref="CombinationType"/> and <see cref="StorageLocation"/>s is supported by this class
		/// </summary>
		/// <param name="type">The given <see cref="CombinationType"/></param>
		/// <param name="locations">The given <see cref="StorageLocation"/>s</param>
		/// <returns>Whether <paramref name="type"/> and <paramref name="locations"/> is supported or not</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsSupported(CombinationType type, ReadOnlySpan<StorageLocation> locations)
		{
			return type == CombinationType.Cached && locations.Length == 2 && locations[0].Type == LocationType.CpuRam && locations[1].Type == LocationType.Uri;
		}

		private readonly PointerSegment stream, memory;

		/// <summary>
		/// <b>Allocate</b> and create a new <see cref="StreamToMemoryCachedStorage{T}"/> with given <see cref="StorageLocation"/>s and sizes
		/// </summary>
		/// <param name="memoryLocation">The <see cref="StorageLocation"/> of the (top) cache level as a memory-typed location</param>
		/// <param name="streamLocation">The <see cref="StorageLocation"/> of actual storage level as a stream-typed location</param>
		/// <param name="maxMemoryCacheSize">The maximum size in <typeparamref name="T"/> allowed in <paramref name="memoryLocation"/></param>
		/// <param name="length">The actual length of this storage in <typeparamref name="T"/></param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="memoryLocation"/> is not a memory-typed location or <paramref name="streamLocation"/> is not a stream-typed location</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public StreamToMemoryCachedStorage(StorageLocation memoryLocation, StorageLocation streamLocation, long maxMemoryCacheSize, long length) :
			base(stackalloc[] { memoryLocation, streamLocation }, stackalloc[] { maxMemoryCacheSize, length })
		{
			if (memoryLocation.Type.GetClassification() != LocationTypeExtension.ClassMemory)
				throw new ArgumentOutOfRangeException(nameof(memoryLocation), memoryLocation, Parameter.InvalidValue);
			if (streamLocation.Type.GetClassification() != LocationTypeExtension.ClassStream)
				throw new ArgumentOutOfRangeException(nameof(streamLocation), streamLocation, Parameter.InvalidValue);

			try
			{
				this.stream = Allocate(streamLocation, length);
				this.memory = Allocate(memoryLocation, maxMemoryCacheSize);
			}
			catch (System.Exception)
			{
				this.Dispose(true);
				throw;
			}
		}
		#endregion

		#region override
		/// <summary>
		/// Get the whole caching level at <paramref name="index"/> as a <see cref="PointerSegment"/>
		/// </summary>
		/// <param name="index">The index to indicate the cache level</param>
		/// <returns>The whole caching level at <paramref name="index"/> as a <see cref="PointerSegment"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="index"/> is out of range</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override PointerSegment GetCacheLevel(int index)
		{
			if (index < 0 || index >= 2)
				throw new ArgumentOutOfRangeException(nameof(index), index, Parameter.InvalidValue);

			if (index == 0)
				return this.memory;
			else
				return this.stream;
		}

		private long streamOffset = 0;
		private bool cached = false;

		/// <summary>
		/// Update all the data in the lowest caching level by causing all caching levels to fall back to the lower ones.
		/// </summary>
		/// <param name="copy">See <see cref="Retrieve(long, long, ICachedStorage.CopyDelegate?)"/>.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override void Flush(ICachedStorage.CopyDelegate? copy = null)
		{
			if (!this.cached)
				return;
			// copy
			if (copy is null)
			{
				MEM.MemoryCopy(this.memory, this.stream.MoveBy(streamOffset, this.memory.LengthInBytes));
			}
			else
			{
				bool support = copy.Invoke(this.memory, this.stream.MoveBy(streamOffset, this.memory.LengthInBytes), out _);
				if (!support)
					throw new NotSupportedException(Support.Location);
			}
			// reset
			this.cached = false; this.streamOffset = 0;
		}

		/// <summary>
		/// Retrieve some part of the data delimited by <paramref name="lengthInBytes"/> and <paramref name="totalOffsetInBytes"/> via promoting them to the highest caching level.
		/// </summary>
		/// <param name="totalOffsetInBytes">The total offset (in bytes) in the lowest caching level (the cache level where all the data preserves)</param>
		/// <param name="lengthInBytes">The length to retrieve (in bytes), default 0 means retrieve as much as possible</param>
		/// <param name="copy">The <see cref="ICachedStorage.CopyDelegate"/> used to copy data between caching levels. The default null value will be replaced by the<see cref="MEM.MemoryCopy(PointerSegment, PointerSegment)"/>.</param>
		/// <returns>The <see cref="PointerSegment"/> of the highest caching level containing the required data</returns>
		/// <exception cref="NotSupportedException">If <paramref name="copy"/> returns false</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="totalOffsetInBytes"/> + <paramref name="lengthInBytes"/> is out of the boundary, or <paramref name="lengthInBytes"/> is larger than <see cref="CachedStorage{T}.TopCacheSizeInBytes"/></exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override PointerSegment Retrieve(long totalOffsetInBytes, long lengthInBytes = 0, ICachedStorage.CopyDelegate? copy = null)
		{
			if (totalOffsetInBytes >= this.stream.LengthInBytes)
				throw new ArgumentOutOfRangeException(nameof(totalOffsetInBytes), totalOffsetInBytes, Parameter.InvalidValue);
			long memLen = this.memory.LengthInBytes;
			if (lengthInBytes <= 0)
				lengthInBytes = memLen;
			if (totalOffsetInBytes + lengthInBytes > this.stream.LengthInBytes)
				throw new ArgumentOutOfRangeException(nameof(lengthInBytes), lengthInBytes, Parameter.InvalidValue);

			long offset = totalOffsetInBytes;
			if (!this.cached)
			{   // not cached yet
				bool support = true;
				// copy from stream storage to memory cache as much as possible
				if (copy is null)
					MEM.MemoryCopy(this.stream.MoveBy(offset, memLen), this.memory);
				else
					support = copy.Invoke(this.stream.MoveBy(offset, memLen), this.memory, out _);
				if (!support)
					throw new NotSupportedException(Support.Location);
				return this.memory.AsLength(LengthInBytes);
			}
			// else
			long offsetDiff = offset - this.streamOffset;
			long offsetDiffU = offsetDiff >= 0 ? offsetDiff : -offsetDiff;
			if (offsetDiff >= 0 && offsetDiffU + lengthInBytes <= memLen)
			{   // already cached
				return this.memory.MoveBy(offsetDiff, lengthInBytes);
			}
			else if (offsetDiffU >= memLen || offsetDiff + lengthInBytes <= 0)
			{   // no overlap
				// copy from stream storage to memory cache as much as possible
				this.Flush(copy);
				return this.Retrieve(totalOffsetInBytes, lengthInBytes, copy);
			}
			else if (offsetDiff > 0)
			{   // partial overlap
				long overlapLength = memLen - offsetDiffU;
				var stream = this.stream.MoveBy(this.streamOffset);
				if (copy is null)
				{
					// flush (copy from memory cache to stream storage)
					MEM.MemoryCopy(this.memory.AsLength(offsetDiffU), stream.AsLength(offsetDiffU));
					// copy inside memory cache
					MEM.MemoryCopy(this.memory.MoveBy(offsetDiff, overlapLength), this.memory.AsLength(overlapLength));
					// copy from stream storage to memory cache as much as possible
					MEM.MemoryCopy(stream.MoveBy(memLen, offsetDiffU), this.memory.MoveBy(offsetDiff, offsetDiffU));
				}
				else
				{
					// flush (copy from memory cache to stream storage)
					if (!copy.Invoke(this.memory.AsLength(offsetDiffU), stream.AsLength(offsetDiffU), out _))
						throw new NotSupportedException(Support.Location);
					// copy inside memory cache
					if (!copy.Invoke(this.memory.MoveBy(offsetDiff, overlapLength), this.memory.AsLength(overlapLength), out _))
						throw new NotSupportedException(Support.Location);
					// copy from stream storage to memory cache as much as possible
					if (!copy.Invoke(stream.MoveBy(memLen, offsetDiffU), this.memory.MoveBy(offsetDiff, offsetDiffU), out _))
						throw new NotSupportedException(Support.Location);
				}
				return this.stream.AsLength(lengthInBytes);
			}
			else
			{   // partial overlap, offsetDiff < 0
				long overlapLength = memLen - offsetDiffU;
				long overlapLengthI = overlapLength;
				var stream = this.stream.MoveBy(this.streamOffset);
				if (copy is null)
				{
					// flush (copy from memory cache to stream storage)
					MEM.MemoryCopy(this.memory.MoveBy(overlapLengthI, offsetDiffU), stream.MoveBy(overlapLengthI, offsetDiffU));
					// copy inside memory cache
					MEM.MemoryCopy(this.memory.AsLength(overlapLength), this.memory.MoveBy(-offsetDiff, overlapLength));
					// copy from stream storage to memory cache as much as possible
					MEM.MemoryCopy(stream.MoveBy(offsetDiff, offsetDiffU), this.memory.AsLength(offsetDiffU));
				}
				else
				{
					// flush (copy from memory cache to stream storage)
					if (!copy.Invoke(this.memory.MoveBy(overlapLengthI, offsetDiffU), stream.MoveBy(overlapLengthI, offsetDiffU), out _))
						throw new NotSupportedException(Support.Location);
					// copy inside memory cache
					if (!copy.Invoke(this.memory.AsLength(overlapLength), this.memory.MoveBy(-offsetDiff, overlapLength), out _))
						throw new NotSupportedException(Support.Location);
					// copy from stream storage to memory cache as much as possible
					if (!copy.Invoke(stream.MoveBy(offsetDiff, offsetDiffU), this.memory.AsLength(offsetDiffU), out _))
						throw new NotSupportedException(Support.Location);
				}
				return this.stream.AsLength(lengthInBytes);
			}
		}
		#endregion
	}
}
