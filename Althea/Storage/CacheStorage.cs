using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using System.Text.Json;

using Althea.Helpers;
using Althea.Linq;
using Althea.NativeTypes;
using Althea.Resources;

using Mem = Althea.Storage.ApiSelector;


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
	/// <typeparam name="TSelf">The actual unmanaged number that implements <see cref="ICacheStrategy{TSelf}"/></typeparam>
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
		int CacheLineSize
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
		}

		/// <summary>
		/// When implemented by a derived struct, calculate the cache offset of given actual storage offset, copy data if necessary, and update internal information of this <typeparamref name="TSelf"/>.
		/// </summary>
		/// <param name="storageOffset">The offset of actual storage in bytes which is the location of the byte of interest</param>
		/// <param name="copy">The <see cref="CopyDelegate"/> used to copy data if necessary</param>
		/// <param name="intentWrite">Whether the intent of using this piece of cache is to write (true) or read (false)</param>
		/// <returns>The offsets to the cache pointer in bytes which contains the requested byte.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		long GetCacheOf(long storageOffset, CopyDelegate copy, bool intentWrite);

		/// <summary>
		/// When implemented by a derived struct, flush all cached data into actual memory with <paramref name="copy"/> delegate.
		/// </summary>
		/// <param name="copy">The <see cref="CopyDelegate"/> used to copy data if necessary</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void Flush(CopyDelegate copy);
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

		/// <inheritdoc/>
		public int CacheLineSize
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => 1 << lineSizeLog2;
		}

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

		/// <inheritdoc/>
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

		/// <inheritdoc/>
		public void Flush(CopyDelegate copy)
		{
			int tagBitOffset = this.lineSizeLog2 + this.nLinesLog2;
			int lines = 1 << this.nLinesLog2, lineSize = 1 << this.lineSizeLog2;
			for (int i = 0; i < lines; i++)
			{
				var info = this.lineInfo[i];
				if (!info.Valid || !info.Dirty)
					continue;
				long storageOffset = info.Tag << tagBitOffset;
				copy(i << this.lineSizeLog2, storageOffset, lineSize, false);
				info.Dirty = false;
				this.lineInfo[i] = info;
			}
		}

		/// <inheritdoc/>
		public bool IsValid() => this.lineSizeLog2 > 0;
		#endregion

		#region equality
		/// <inheritdoc/>
		public bool Equals(DirectMappingStrategy other) => this.lineSizeLog2 == other.lineSizeLog2 && this.lineInfo.SequenceEqual(other.lineInfo);

		/// <summary>
		/// Equality operator
		/// </summary>
		public static bool operator ==(DirectMappingStrategy left, DirectMappingStrategy right) => left.Equals(right);

		/// <summary>
		/// Inequality operator
		/// </summary>
		public static bool operator !=(DirectMappingStrategy left, DirectMappingStrategy right) => !left.Equals(right);

		/// <inheritdoc/>
		public override bool Equals(object? obj) => obj is DirectMappingStrategy strategy && this.Equals(strategy);

		/// <inheritdoc/>
		public override int GetHashCode() => HashCode.Combine(this.lineSizeLog2, SpanLinq.HashCodeOfSpan<CacheLineInfo>(this.lineInfo));
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
		protected internal abstract TS Strategy { get; }

		/// <summary>
		/// When implemented by a derived struct, get the high speed cache <see cref="PointerSegment{T}"/> of this <see cref="CachedStorageBase{TS, TPh, TPl}"/>
		/// </summary>
		protected internal abstract PointerSegment<TPh> Cache { get; }

		/// <summary>
		/// Get the low speed memory storage <see cref="PointerSegment{T}"/> of this <see cref="CachedStorageBase{TS, TPh, TPl}"/>
		/// </summary>
		protected internal PointerSegment<TPl> Memory { get; }

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
				Mem.MemoryCopy(this.Memory + sourceOffset, this.Cache.MoveBy(destinationOffset, copyLength));
			else
				Mem.MemoryCopy(this.Cache + sourceOffset, this.Memory.MoveBy(destinationOffset, copyLength));
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
		/// When implemented by a derived class, get the offset of the first block of <see cref="CachedStorageBase{TS, TPh, TPl}.Memory"/> compared to the start of its cache line in bytes.
		/// </summary>
		protected abstract long InsideLineOffset { get; }

		/// <summary>
		/// When implemented by a derived class, get the index of the cache line of the first block of <see cref="CachedStorageBase{TS, TPh, TPl}.Memory"/>.
		/// </summary>
		protected abstract long CacheLineOffset { get; }

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

#pragma warning disable CS8619
		static MethodInfo[] IStorage.PointerGetters => new[] { typeof(CachedStorage<T, TS, TPh, TPl>).GetMethod(nameof(MemoryPieceAt)) };
#pragma warning restore CS8619

		long IStorage.SizeOfPointer(int i)
		{
			if (!this.IsValid())
				return 0;
			if (i != 1)
				throw new ArgumentOutOfRangeException(nameof(i));
			return (this.Memory.LengthInBytes + this.CacheLineOffset - 1) / this.Strategy.CacheLineSize + 1; // ceiling divide
		}

		/// <summary>
		/// Get the cache pointer of memory piece indicated by <paramref name="index"/>.
		/// </summary>
		/// <param name="index">The piece index of the memory to be accessed</param>
		/// <param name="intentWrite">The usage intent is to write or read-only.</param>
		/// <returns>The <see cref="PointerSegment{T}"/> of requested memory cached in <see cref="CachedStorageBase{TS, TPh, TPl}.Cache"/>.</returns>
		public PointerSegment<TPh> MemoryPieceAt(long index, bool intentWrite)
		{
			long partialLength = index == 0 ? this.Strategy.CacheLineSize - this.InsideLineOffset : this.Strategy.CacheLineSize;
			long offset = index == 0 ? this.InsideLineOffset : 0;
			index += this.CacheLineOffset;
			offset = this.Strategy.GetCacheOf(offset + index * this.Strategy.CacheLineSize, this.CopyWrapper, intentWrite);
			return this.Cache.MoveBy(offset, partialLength);
		}

		/// <summary>
		/// Get the total length of the presenting array in bytes
		/// </summary>
		public long LengthInBytes => this.Memory.LengthInBytes;

		/// <summary>
		/// Get the total length of the presenting array in <typeparamref name="T"/>
		/// </summary>
		public long Length => ((IStorage<T, CachedStorage<T, TS, TPh, TPl>>)this).Length;

		/// <summary>
		/// Get a <see cref="bool"/> indicating whether this storage is disposed or not
		/// </summary>
		public bool Disposed { get; private set; } = false;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void Dispose()
		{
			if (this is ActualCachedStorage<T, TS, TPh, TPl>)
			{
				Mem.Free(this.Cache.Pointer);
				Mem.Free(this.Memory.Pointer);
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
		#endregion

		#region reference
		ReadOnlySpan<long> IStorage<T, CachedStorage<T, TS, TPh, TPl>>.GetPointerSizes(Span<long> sizes)
		{
			if (sizes.Length < 1)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(sizes));
			sizes[0] = this.Memory.LengthInBytes;
			return sizes;
		}

		/// <inheritdoc/>
		public bool OverlapWith(IStorage other)
		{
			return other is CachedStorageBase<TS, TPh, TPl> s && this.Memory.OverlapWith(s.Memory);
		}

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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static CachedStorage<T, TS, TPh, TPl> IStorage<T, CachedStorage<T, TS, TPh, TPl>>.RefFrom<TOut, TOther>(TOther storage)
		{
			return (storage as CachedStorage<TOut, TS, TPh, TPl> ?? throw new ArgumentException(ParameterError.UnexpectedType, nameof(storage))).As<T>();
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
			((IStorage<T, CachedStorage<T, TS, TPh, TPl>>)this).CheckCast<TOut>();
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
			if (lengths.Length != 1)
				throw new ArgumentException(ParameterError.WrongSize, nameof(lengths));
			if (lengths[0] <= 0)
				throw new ArgumentOutOfRangeException(nameof(lengths), ParameterError.MustPositive);
			return new ActualCachedStorage<T, TS, TPh, TPl>(lengths[0]);
		}

		static CachedStorage<T, TS, TPh, TPl> IStorage<T, CachedStorage<T, TS, TPh, TPl>>.CreateAlike<TOut, TOther>(TOther storage)
		{
			return CreateAlike(storage as CachedStorage<TOut, TS, TPh, TPl> ?? throw new ArgumentException(ParameterError.UnexpectedType, nameof(storage)));
		}

		/// <summary>
		/// Statically allocate and creates a new <see cref="CachedStorage{T, TS, TPh, TPl}"/> alike <paramref name="storage"/>.
		/// </summary>
		/// <param name="storage">The storage of data type <typeparamref name="TOut"/> to mimic.</param>
		/// <returns>A new <see cref="CachedStorage{T, TS, TPh, TPl}"/> that likes <paramref name="storage"/></returns>
		public static CachedStorage<T, TS, TPh, TPl> CreateAlike<TOut>(CachedStorage<TOut, TS, TPh, TPl> storage) where TOut : unmanaged, INumber<TOut>
		{
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
				throw new InvalidOperationException(ArithmeticError.CannotDivide);
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

		/// <inheritdoc/>
		public override string ToString() => IMainPropertyFormattable<CachedStorage<T, TS, TPh, TPl>>.ToString(this);

		static JsonConverter<CachedStorage<T, TS, TPh, TPl>> IStorage<T, CachedStorage<T, TS, TPh, TPl>>.JsonConverter => new JsonConverter();

		private sealed class JsonConverter : JsonConverter<CachedStorage<T, TS, TPh, TPl>>
		{
			private record struct Repr(string Data);

			public override CachedStorage<T, TS, TPh, TPl> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
			{
				if (reader.TokenType != JsonTokenType.StartObject || !reader.Read())
					throw new JsonException();
				if (reader.TokenType != JsonTokenType.PropertyName || reader.GetString() != nameof(Repr.Data) || !reader.Read())
					throw new JsonException();
				if (reader.TokenType != JsonTokenType.String)
					throw new JsonException();

				byte[] data = reader.GetBytesFromBase64();
				TPl pointer = Mem.Allocate<TPl>(data.LongLength);
				Mem.FromManaged<byte, TPl>(pointer, data);

				if (!reader.Read())
					throw new JsonException();
				if (reader.TokenType != JsonTokenType.EndObject)
					throw new JsonException();
				reader.Read();

				return new ActualCachedStorage<T, TS, TPh, TPl>(pointer);
			}

			public override void Write(Utf8JsonWriter writer, CachedStorage<T, TS, TPh, TPl> value!!, JsonSerializerOptions options)
			{
				if (!value.IsValid())
					throw new JsonException(ParameterError.InvalidValue);
				byte[] temp = new byte[value.LengthInBytes];
				value.Strategy.Flush(value.CopyWrapper);
				Mem.ToManaged<byte, TPl>(value.Memory, temp);
				writer.WriteStartObject();
				writer.WriteBase64String(nameof(Repr.Data), temp);
				writer.WriteEndObject();
			}
		}
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
		protected internal override TS Strategy { get; }

		/// <summary>
		/// Get the high speed cache <see cref="PointerSegment{T}"/> of this <see cref="CachedStorageBase{TS, TPh, TPl}"/>
		/// </summary>
		protected internal override PointerSegment<TPh> Cache { get; }

		/// <summary>
		/// Get the offset of the first block of <see cref="CachedStorageBase{TS, TPh, TPl}.Memory"/> compared to the start of its cache line in bytes.
		/// </summary>
		protected override long InsideLineOffset => 0;

		/// <summary>
		/// Get the index of the cache line of the first block of <see cref="CachedStorageBase{TS, TPh, TPl}.Memory"/>.
		/// </summary>
		protected override long CacheLineOffset => 0;

		internal ActualCachedStorage(TPl memory) : base(memory)
		{
			this.Strategy = TS.Create(memory.LengthInBytes, out long cacheSizeBytes);
			this.Cache = Mem.Allocate<TPh>(cacheSizeBytes);
		}

		/// <summary>
		/// Create a new <see cref="ActualCachedStorage{T, TS, TPh, TPl}"/> of given <paramref name="length"/>
		/// </summary>
		/// <param name="length">The length to create in <typeparamref name="T"/></param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="length"/> ≤ 0</exception>
		/// <exception cref="OutOfMemoryException">If <paramref name="length"/> is too large to be allocated</exception>
		public ActualCachedStorage(long length) : this(length > 0 ? Mem.Allocate<TPl>(length * Unmanaged<T>.Size) : throw new ArgumentOutOfRangeException(nameof(length), ParameterError.MustPositive))
		{
			// do nothing
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
		protected internal override TS Strategy => this.RealRef?.Strategy ?? default;

		/// <summary>
		/// Get the high speed cache <see cref="PointerSegment{T}"/> of this <see cref="CachedStorageBase{TS, TPh, TPl}"/>
		/// </summary>
		protected internal override PointerSegment<TPh> Cache => this.RealRef?.Cache ?? default;

		/// <summary>
		/// Get the reference <see cref="IStorage"/> of this <see cref="ReferenceCachedStorage{T, TS, TPh, TPl}"/>
		/// </summary>w
		public IStorage? Reference { get; }

		private CachedStorageBase<TS, TPh, TPl>? RealRef => this.Reference as CachedStorageBase<TS, TPh, TPl>;

		/// <summary>
		/// Get the total offset of this <see cref="ReferenceCachedStorage{T, TS, TPh, TPl}"/> in bytes
		/// </summary>
		public long TotalOffsetInBytes => this.Memory.OffsetInBytes;

		/// <summary>
		/// Get the offset of the first block of <see cref="CachedStorageBase{TS, TPh, TPl}.Memory"/> compared to the start of its cache line in bytes.
		/// </summary>
		protected override long InsideLineOffset { get; }

		/// <summary>
		/// Get the index of the cache line of the first block of <see cref="CachedStorageBase{TS, TPh, TPl}.Memory"/>.
		/// </summary>
		protected override long CacheLineOffset { get; }

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
			var (reference, totalOffsetBytes, _) = IReferenceStorage<T, CachedStorage<T, TS, TPh, TPl>>.Create<CachedStorageBase<TS, TPh, TPl>>(storage, offset, newLength);
			this.Reference = reference;
			this.CacheLineOffset = totalOffsetBytes / this.Strategy.CacheLineSize;
			this.InsideLineOffset = (this.CacheLineOffset + 1) * this.Strategy.CacheLineSize - totalOffsetBytes;
		}
	}
}
