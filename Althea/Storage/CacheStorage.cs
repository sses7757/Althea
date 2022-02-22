using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Althea.Helpers;
using Althea.Linq;
using Althea.NativeTypes;
using Althea.Resources;

using MEM = Althea.Storage.ApiSelector;


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
	public delegate void CopyDelegate(long sourceOffset, long destinationOffset, long copyLength, bool copyToCache);

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
		/// When implemented by a derived struct, get the high speed cache <see cref="PointerSegment{T}"/> of this <see cref="CachedStorageBase{TS, TPh, TPl}"/>
		/// </summary>
		public abstract PointerSegment<TPh> Cache { get; }

		/// <summary>
		/// Get the low speed memory storage <see cref="PointerSegment{T}"/> of this <see cref="CachedStorageBase{TS, TPh, TPl}"/>
		/// </summary>
		public PointerSegment<TPl> Memory { get; }

		/// <summary>
		/// Create a new <see cref="CachedStorageBase{TS, TPh, TPl}"/> with given memory <see cref="PointerSegment{T}"/>
		/// </summary>
		/// <param name="memory">The <see cref="PointerSegment{T}"/> of type <typeparamref name="TPl"/> as memory storage pointer</param>
		protected CachedStorageBase(PointerSegment<TPl> memory)
		{
			this.Memory = memory;
		}

		/// <summary>
		/// Copies a block of memory from <paramref name="sourceOffset"/> to <paramref name="destinationOffset"/> of <paramref name="copyLength"/> with copy direction indicated by <paramref name="copyToCache"/>.
		/// </summary>
		/// <param name="sourceOffset">The source offset in bytes</param>
		/// <param name="destinationOffset">The destination offset in bytes</param>
		/// <param name="copyLength">The copy length in bytes</param>
		/// <param name="copyToCache">If true, copy from actual storage to cache; otherwise, copy from cache to actual storage</param>
		protected void CopyWrapper(long sourceOffset, long destinationOffset, long copyLength, bool copyToCache)
		{
			if (copyToCache)
				MEM.MemoryCopy(this.Memory + sourceOffset, this.Cache.MoveBy(destinationOffset, copyLength));
			else
				MEM.MemoryCopy(this.Cache + sourceOffset, this.Memory.MoveBy(destinationOffset, copyLength));
		}
	}

	/// <summary>
	/// The abstract cached storage class that inherits <see cref="CachedStorageBase{TS, TPh, TPl}"/> and constrains data type to <typeparamref name="T"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TS">Any cache strategy struct which implements <see cref="ICacheStrategy{TSelf}"/></typeparam>
	/// <typeparam name="TPh">Any high speed pointer type which implements <see cref="IPointer{TSelf}"/></typeparam>
	/// <typeparam name="TPl">Any low speed pointer type which implements <see cref="IPointer{TSelf}"/></typeparam>
	public abstract class CachedStorage<T, TS, TPh, TPl> : CachedStorageBase<TS, TPh, TPl>, IStorage<T, CachedStorage<T, TS, TPh, TPl>>
		where T : unmanaged, INumber<T>
		where TS : struct, ICacheStrategy<TS>
		where TPh : notnull, IPointer<TPh>
		where TPl : notnull, IPointer<TPl>
	{
		#region basic
		/// <summary>
		/// Create a new <see cref="CachedStorage{T, TS, TPh, TPl}"/> with given memory <see cref="PointerSegment{T}"/>
		/// </summary>
		/// <param name="memory">The <see cref="PointerSegment{T}"/> of type <typeparamref name="TPl"/> as memory storage pointer</param>
		protected CachedStorage(PointerSegment<TPl> memory) : base(memory)
		{ }

		/// <summary>
		/// Statically get an empty <see cref="CachedStorage{T, TS, TPh, TPl}"/>
		/// </summary>
		public static CachedStorage<T, TS, TPh, TPl> Empty => new ReferenceCachedStorage<T, TS, TPh, TPl>(null);

		/// <summary>
		/// Statically get the data type of this storage as a <see cref="NativeTypes.DataType"/>
		/// </summary>
		public static DataType DataType => Unmanaged<T>.DataType;

		/// <summary>
		/// Statically get the description of the storage locations of this <see cref="CachedStorage{T, TS, TPh, TPl}"/> as a <see cref="CombinationOfLocations"/>
		/// </summary>
		public static CombinationOfLocations LocationDescription => new(stackalloc bool[] { true, false }.CreateCombinationType(), stackalloc StorageLocation[] { TPh.Location, TPl.Location });

		/// <summary>
		/// Get the total length of the presenting array in bytes
		/// </summary>
		public long LengthInBytes => this.Memory.LengthInBytes;

		/// <summary>
		/// Get the total length of the presenting array in <typeparamref name="T"/>
		/// </summary>
		public long Length => this.Memory.LengthInBytes / Unmanaged<T>.Size;

		/// <summary>
		/// Get a <see cref="bool"/> indicating whether this storage is disposed or not
		/// </summary>
		public bool Disposed { get; private set; } = false;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void Dispose()
		{
			if (this is ActualCachedStorage<T, TS, TPh, TPl>)
			{
				MEM.Free(this.Cache.Pointer);
				MEM.Free(this.Memory.Pointer);
			}
			this.Disposed = true;
		}

		void IStorage.Dispose(bool invokedByUser) => this.Dispose();

		/// <summary>
		/// The deconstructor invoked by GC
		/// </summary>
		~CachedStorage() => this.Dispose();

		/// <summary>
		/// Check whether this <see cref="CachedStorage{T, TS, TPh, TPl}"/> is a valid one or not
		/// </summary>
		/// <returns>The validness of this <see cref="CachedStorage{T, TS, TPh, TPl}"/></returns>
		public bool IsValid() => !this.Disposed && this.Memory.IsValid();

		/// <summary>
		/// Request usage of a piece of storage started from <paramref name="offset"/> with <paramref name="length"/> and will be used as <paramref name="intentWrite"/>.
		/// </summary>
		/// <param name="offset">The starting requesting element offset compared to this storage</param>
		/// <param name="length">The number of element(s) requested</param>
		/// <param name="intentWrite">The usage intent is to write (true) or to read (false)</param>
		/// <returns>The maximum length from <paramref name="offset"/> allowed for request, or 0 if <paramref name="length"/> is allowed.</returns>
		public long Request(long offset, long length, bool intentWrite)
		{
			offset *= Unmanaged<T>.Size; length *= Unmanaged<T>.Size;
			offset += this.Memory.OffsetInBytes;
			long cacheIndex = offset / this.Strategy.CacheLineSize;
			if (cacheIndex != (offset + length) / this.Strategy.CacheLineSize) // too large
				return this.Strategy.CacheLineSize - offset + cacheIndex * this.Strategy.CacheLineSize;
			this.Strategy.GetCacheOf(offset, this.CopyWrapper, intentWrite);
			return 0;
		}
		#endregion

		#region reference
		/// <summary>
		/// Make a referenced <see cref="CachedStorage{T, TS, TPh, TPl}"/> with the starting pointer moving <paramref name="offset"/> and <see cref="IStorage{T, TSelf}.Length"/> changing to <paramref name="newLength"/>.
		/// </summary>
		/// <param name="offset">The offset in <typeparamref name="T"/> to the starting pointer of this <see cref="CachedStorage{T, TS, TPh, TPl}"/> as a <see cref="long"/></param>
		/// <param name="newLength">The new length in <typeparamref name="T"/> as a <see cref="long"/>, default 0 means automatically calculate from <paramref name="offset"/></param>
		/// <returns>A referenced <see cref="CachedStorage{T, TS, TPh, TPl}"/> of this one</returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offset"/> and <paramref name="newLength"/> is out of boundary</exception>
		public CachedStorage<T, TS, TPh, TPl> MakeReference(long offset = 0, long newLength = 0)
		{
			if (offset == 0 && newLength == 0 && this is ReferenceCachedStorage<T, TS, TPh, TPl> @ref)
				return @ref;
			else
				return new ReferenceCachedStorage<T, TS, TPh, TPl>(this, offset, newLength);
		}

		/// <summary>
		/// Check whether this <see cref="CachedStorage{T, TS, TPh, TPl}"/> overlaps with the <paramref name="other"/> <see cref="CachedStorage{T, TS, TPh, TPl}"/>.
		/// </summary>
		/// <param name="other">The other <see cref="CachedStorage{T, TS, TPh, TPl}"/> to check overlap</param>
		/// <returns>True if this overlaps with the <paramref name="other"/>, false otherwise</returns>
		public bool OverlapWith(CachedStorage<T, TS, TPh, TPl> other) => this.Memory.OverlapWith(other.Memory);

		static CachedStorage<T, TS, TPh, TPl> IStorage<T, CachedStorage<T, TS, TPh, TPl>>.RefFrom<TOut, TOther>(TOther storage)
		{
			return (storage as CachedStorage<TOut, TS, TPh, TPl> ?? throw new InvalidOperationException(Parameter.UnexpectedType)).As<T>();
		}

		/// <summary>
		/// Create a referenced storage of data type <typeparamref name="TOut"/> over this storage
		/// </summary>
		/// <typeparam name="TOut">Any unmanaged number as the new data type</typeparam>
		/// <returns>The referenced <see cref="CachedStorage{T, TS, TPh, TPl}"/> of data type <typeparamref name="TOut"/></returns>
		/// <exception cref="InvalidCastException">If the <see cref="LengthInBytes"/> cannot be divided by the size of <typeparamref name="TOut"/></exception>
		public CachedStorage<TOut, TS, TPh, TPl> As<TOut>() where TOut : unmanaged, INumber<TOut>
		{
			if (typeof(TOut) == typeof(T))
				return this.MakeReference() as CachedStorage<TOut, TS, TPh, TPl> ?? CachedStorage<TOut, TS, TPh, TPl>.Empty;
			IStorage<T, CachedStorage<T, TS, TPh, TPl>>.CheckCast<TOut>(this.Length);
			return new ReferenceCachedStorage<TOut, TS, TPh, TPl>(this);
		}
		#endregion

		#region create
		/// <summary>
		/// Statically <b>allocate</b> and create a new <see cref="CachedStorage{T, TS, TPh, TPl}"/> of given lengths.
		/// </summary>
		/// <param name="lengths">The given lengths in <typeparamref name="T"/></param>
		/// <returns>The created new <see cref="CachedStorage{T, TS, TPh, TPl}"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="lengths"/> is null or empty</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="lengths"/> has length(s) ≤ 0</exception>
		/// <exception cref="OutOfMemoryException">If the underlying allocation failed due to insufficient memory</exception>
		/// <exception cref="InvalidOperationException">If underlying creation fails due to other reasons</exception>
		public static CachedStorage<T, TS, TPh, TPl> Create(ReadOnlySpan<long> lengths)
		{
			if (lengths.Length != 2)
				throw new InvalidOperationException(Support.Location);
			if (lengths[1] <= 0)
				throw new ArgumentOutOfRangeException(nameof(lengths), Parameter.MustPositive);
			return new ActualCachedStorage<T, TS, TPh, TPl>(lengths[1]);
		}

		static CachedStorage<T, TS, TPh, TPl> IStorage<T, CachedStorage<T, TS, TPh, TPl>>.CreateAlike<TOut, TOther>(TOther storage)
		{
			return CreateAlike(storage as CachedStorage<TOut, TS, TPh, TPl> ?? throw new InvalidOperationException(Parameter.UnexpectedType));
		}

		/// <summary>
		/// Statically allocate and creates a new <see cref="CachedStorage{T, TS, TPh, TPl}"/> alike <paramref name="storage"/>.
		/// </summary>
		/// <param name="storage">The storage of data type <typeparamref name="TOut"/> to mimic.</param>
		/// <returns>A new <see cref="CachedStorage{T, TS, TPh, TPl}"/> that likes <paramref name="storage"/></returns>
		public static CachedStorage<T, TS, TPh, TPl> CreateAlike<TOut>(CachedStorage<TOut, TS, TPh, TPl> storage) where TOut : unmanaged, INumber<TOut>
		{
			var descr = CachedStorage<TOut, TS, TPh, TPl>.LocationDescription;
			return Create(stackalloc long[] { storage.Cache.LengthInBytes / Unmanaged<TOut>.Size, storage.Length });
		}
		#endregion

		#region operators
		static long IAdditiveIdentity<CachedStorage<T, TS, TPh, TPl>, long>.AdditiveIdentity => 0;

		/// <summary>
		/// Indicates whether the current <see cref="CachedStorage{T, TS, TPh, TPl}"/> is equal to the <paramref name="other"/> <see cref="CachedStorage{T, TS, TPh, TPl}"/> of the same type.
		/// </summary>
		/// <param name="other">The other <see cref="CachedStorage{T, TS, TPh, TPl}"/> to compare to</param>
		/// <returns>true if the current <see cref="CachedStorage{T, TS, TPh, TPl}"/> is equal to the <paramref name="other"/>; otherwise, false.</returns>
		public bool Equals(CachedStorage<T, TS, TPh, TPl>? other) => other is not null && this.Memory == other.Memory;

		/// <summary>
		/// Get the hash code of this <see cref="CachedStorage{T, TS, TPh, TPl}"/>
		/// </summary>
		/// <returns>The hash code of this <see cref="CachedStorage{T, TS, TPh, TPl}"/></returns>
		public override int GetHashCode() => this.Memory.GetHashCode();

		/// <summary>
		/// Check whether this <see cref="CachedStorage{T, TS, TPh, TPl}"/> equals the other <paramref name="obj"/> or not
		/// </summary>
		/// <param name="obj">The other object to compare to</param>
		/// <returns><c>this == <paramref name="obj"/></c></returns>
		public override bool Equals(object? obj) => this.Equals(obj as CachedStorage<T, TS, TPh, TPl>);

		/// <summary>
		/// Statically get the distance in <typeparamref name="T"/> between two <see cref="CachedStorage{T, TS, TPh, TPl}"/>s
		/// </summary>
		/// <param name="left">The left operand of type <see cref="CachedStorage{T, TS, TPh, TPl}"/></param>
		/// <param name="right">The right operand of type <see cref="CachedStorage{T, TS, TPh, TPl}"/></param>
		/// <returns>The distance between two <see cref="CachedStorage{T, TS, TPh, TPl}"/>s in <typeparamref name="T"/> as a <see cref="long"/>.</returns>
		/// <exception cref="InvalidOperationException">If <paramref name="left"/> and <paramref name="right"/> have different origin.</exception>
		public static long operator -(CachedStorage<T, TS, TPh, TPl> left, CachedStorage<T, TS, TPh, TPl> right)
		{
			long diffBytes = IStorage<T, CachedStorage<T, TS, TPh, TPl>>.StorageDiffBytes(left, right);
			if (diffBytes % Unmanaged<T>.Size != 0)
				throw new InvalidOperationException(Other.CannotDivide);
			return diffBytes / Unmanaged<T>.Size;
		}

		/// <summary>
		/// <see cref="CachedStorage{T, TS, TPh, TPl}"/> addition operator
		/// </summary>
		public static CachedStorage<T, TS, TPh, TPl> operator +(CachedStorage<T, TS, TPh, TPl> left, long right) => left.MakeReference(right);

		/// <summary>
		/// <see cref="CachedStorage{T, TS, TPh, TPl}"/> subtraction operator
		/// </summary>
		public static CachedStorage<T, TS, TPh, TPl> operator -(CachedStorage<T, TS, TPh, TPl> left, long right) => left.MakeReference(-right);

		/// <summary>
		/// <see cref="CachedStorage{T, TS, TPh, TPl}"/> equality operator
		/// </summary>
		public static bool operator ==(CachedStorage<T, TS, TPh, TPl> left, CachedStorage<T, TS, TPh, TPl> right) => left.Equals(right);

		/// <summary>
		/// <see cref="CachedStorage{T, TS, TPh, TPl}"/> inequality operator
		/// </summary>
		public static bool operator !=(CachedStorage<T, TS, TPh, TPl> left, CachedStorage<T, TS, TPh, TPl> right) => !left.Equals(right);
		#endregion

		#region string
		static string IMainPropertyFormattable<CachedStorage<T, TS, TPh, TPl>>.StringMain => nameof(CachedStorage<T, TS, TPh, TPl>);

		static IEnumerable<string> IMainPropertyFormattable<CachedStorage<T, TS, TPh, TPl>>.PropertyNames => new[] { nameof(DataType), nameof(IStorage<T, CachedStorage<T, TS, TPh, TPl>>.Length), nameof(Cache), nameof(Memory) };

		IEnumerable<object?> IMainPropertyFormattable<CachedStorage<T, TS, TPh, TPl>>.PropertyValues => new object?[] { DataType, this.Length, this.Cache.ToString(), this.Memory.ToString() };

		/// <summary>
		/// Return the string representation of this <see cref="CachedStorage{T, TS, TPh, TPl}"/>
		/// </summary>
		/// <returns>the string representation of this <see cref="CachedStorage{T, TS, TPh, TPl}"/></returns>
		public override string ToString() => IMainPropertyFormattable<CachedStorage<T, TS, TPh, TPl>>.ToString(this);
		#endregion
	}

	/// <summary>
	/// The actual single level cached storage class that inherits <see cref="CachedStorageBase{TS, TPh, TPl}"/> and constrains data type to <typeparamref name="T"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TS">Any cache strategy struct which implements <see cref="ICacheStrategy{TSelf}"/></typeparam>
	/// <typeparam name="TPh">Any high speed pointer type which implements <see cref="IPointer{TSelf}"/></typeparam>
	/// <typeparam name="TPl">Any low speed pointer type which implements <see cref="IPointer{TSelf}"/></typeparam>
	public sealed class ActualCachedStorage<T, TS, TPh, TPl> : CachedStorage<T, TS, TPh, TPl>, IActualStorage<T, CachedStorage<T, TS, TPh, TPl>>
		where T : unmanaged, INumber<T>
		where TS : struct, ICacheStrategy<TS>
		where TPh : notnull, IPointer<TPh>
		where TPl : notnull, IPointer<TPl>
	{
		/// <summary>
		/// Get the cache strategy of this <see cref="CachedStorageBase{TS, TPh, TPl}"/>
		/// </summary>
		public override TS Strategy { get; }

		/// <summary>
		/// Get the high speed cache <see cref="PointerSegment{T}"/> of this <see cref="CachedStorageBase{TS, TPh, TPl}"/>
		/// </summary>
		public override PointerSegment<TPh> Cache { get; }

		/// <summary>
		/// Create a new <see cref="ActualCachedStorage{T, TS, TPh, TPl}"/> of given <paramref name="length"/>
		/// </summary>
		/// <param name="length">The length to create in <typeparamref name="T"/></param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="length"/> ≤ 0</exception>
		/// <exception cref="OutOfMemoryException">If <paramref name="length"/> is too large to be allocated</exception>
		public ActualCachedStorage(long length) : base(length > 0 ? MEM.Allocate<T, TPl>(length) : throw new ArgumentOutOfRangeException(nameof(length), Parameter.MustPositive))
		{
			this.Strategy = TS.Create(length * Unmanaged<T>.Size, out long cacheSizeBytes);
			this.Cache = MEM.Allocate<TPh>(cacheSizeBytes);
		}
	}

	/// <summary>
	/// The referenced single level cached storage class that inherits <see cref="CachedStorageBase{TS, TPh, TPl}"/> and constrains data type to <typeparamref name="T"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TS">Any cache strategy struct which implements <see cref="ICacheStrategy{TSelf}"/></typeparam>
	/// <typeparam name="TPh">Any high speed pointer type which implements <see cref="IPointer{TSelf}"/></typeparam>
	/// <typeparam name="TPl">Any low speed pointer type which implements <see cref="IPointer{TSelf}"/></typeparam>
	public sealed class ReferenceCachedStorage<T, TS, TPh, TPl> : CachedStorage<T, TS, TPh, TPl>, IReferenceStorage<T, CachedStorage<T, TS, TPh, TPl>>
		where T : unmanaged, INumber<T>
		where TS : struct, ICacheStrategy<TS>
		where TPh : notnull, IPointer<TPh>
		where TPl : notnull, IPointer<TPl>
	{
		/// <summary>
		/// Get the cache strategy of this <see cref="CachedStorageBase{TS, TPh, TPl}"/>
		/// </summary>
		public override TS Strategy => this.Reference_?.Strategy ?? default;

		/// <summary>
		/// Get the high speed cache <see cref="PointerSegment{T}"/> of this <see cref="CachedStorageBase{TS, TPh, TPl}"/>
		/// </summary>
		public override PointerSegment<TPh> Cache => this.Reference_?.Cache ?? default;

		/// <summary>
		/// Get the reference <see cref="IStorage"/> of this <see cref="ReferenceCachedStorage{T, TS, TPh, TPl}"/>
		/// </summary>
		public IStorage? Reference { get; }

		private CachedStorageBase<TS, TPh, TPl>? Reference_ => (CachedStorageBase<TS, TPh, TPl>?)this.Reference;

		/// <summary>
		/// Get the total offset of this <see cref="ReferenceCachedStorage{T, TS, TPh, TPl}"/> in bytes
		/// </summary>
		public long TotalOffsetInBytes => this.Memory.OffsetInBytes;

		/// <summary>
		/// Create a new <see cref="ReferenceCachedStorage{T, TS, TPh, TPl}"/> from given base <paramref name="storage"/> and <paramref name="offset"/> and <paramref name="newLength"/>.
		/// </summary>
		/// <param name="storage">The base <see cref="IStorage"/> to refer to</param>
		/// <param name="offset">The offset in <typeparamref name="T"/> compared to <paramref name="storage"/></param>
		/// <param name="newLength">The new presenting length in <typeparamref name="T"/></param>
		/// <exception cref="ArgumentException">If <paramref name="storage"/> is not a <see cref="CachedStorageBase{TS, TPh, TPl}"/></exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offset"/> and <paramref name="newLength"/> are out of boundary</exception>
		public ReferenceCachedStorage(IStorage? storage, long offset = 0, long newLength = 0) :
			base(storage is CachedStorageBase<TS, TPh, TPl> p ? p.Memory.MoveBy(offset * Unmanaged<T>.Size, newLength * Unmanaged<T>.Size) : default)
		{
			var (reference, _, _) = IReferenceStorage<T, CachedStorage<T, TS, TPh, TPl>>.Create<CachedStorageBase<TS, TPh, TPl>>(storage, offset, newLength);
			this.Reference = reference;
		}
	}
}
