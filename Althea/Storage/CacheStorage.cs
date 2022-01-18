using System.Runtime.CompilerServices;

using Althea.Helpers;
using Althea.Linq;
using Althea.Resources;

using MEM = Althea.Storage.AbstractApi;


namespace Althea.Storage
{
	/// <summary>
	/// The interface for any cached storage, including actual ones and referenced one
	/// </summary>
	public interface ICachedStorage : IStorage
	{
		#region other definitions
		/// <summary>
		/// The ratio of the size of a lower caching level and the size of its upper level shall be larger than this value.
		/// </summary>
		public const int CacheSizeRatio = 10;
		#endregion

		#region new methods
		/// <summary>
		/// When implemented by a derived class, get the cache sizes in bytes of the top level as a <see cref="long"/>.
		/// </summary>
		long TopCacheSizeInBytes { get; }

		/// <summary>
		/// When implemented by a derived class, get the number of total caching levels, include the actual storage level. The default implementation is <see cref="IStorage.LocationDescription"/>.<see cref="CombinationOfLocations.Count">Count</see>.
		/// </summary>
		int CacheLevels
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.LocationDescription.Count;
		}

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
