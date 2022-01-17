using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

using Althea.Helpers;
using Althea.Linq;
using Althea.NativeTypes;
using Althea.Resources;

using MEM = Althea.Storage.AbstractApi;


namespace Althea.Storage
{
	#region URI related
	/// <summary>
	/// The enum representing the URI schemes which can be used as a storage location detail <see cref="StorageLocation.Detail"/>.
	/// </summary>
	/// <remarks>See <see cref="Uri.UriSchemeFile"/>, etc.</remarks>
	public enum UriScheme : short
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
		/// Specifies that the URI is accessed through the TCP/IP directly.
		/// </summary>
		TCP = 2,
		/// <summary>
		/// Specifies that the URI is accessed through the File Transfer Protocol (FTP).
		/// </summary>
		FTP = 3,
		/// <summary>
		/// Specifies that the URI is accessed through the Hypertext Transfer Protocol (HTTP).
		/// </summary>
		HTTP = 4,
		/// <summary>
		/// Specifies that the URI is accessed through the Secure Hypertext Transfer Protocol (HTTPS).
		/// </summary>
		HTTPS = 5,
	}

	/// <summary>
	/// The static class for extension methods of <see cref="UriScheme"/>
	/// </summary>
	public static class UriSchemeExtension
	{
		/// <summary>
		/// Get the <see cref="UriScheme"/> from a <see cref="Uri"/>
		/// </summary>
		/// <param name="uri">The absolute <see cref="Uri"/></param>
		/// <returns>the <see cref="UriScheme"/> of <paramref name="uri"/>, or <see cref="UriScheme.Unknown"/> if <paramref name="uri"/>'s scheme is not in <see cref="UriScheme"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="uri"/> is not an absolute URI</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static UriScheme GetScheme(this Uri uri)
		{
			if (!uri.IsAbsoluteUri)
				throw new ArgumentOutOfRangeException(nameof(uri), uri, Parameter.InvalidValue);
			if (uri.Scheme == Uri.UriSchemeFile)
				return UriScheme.File;
			if (uri.Scheme == @"tcp" || uri.Scheme == Uri.UriSchemeNetTcp)
				return UriScheme.TCP;
			if (uri.Scheme == Uri.UriSchemeFtp)
				return UriScheme.FTP;
			if (uri.Scheme == Uri.UriSchemeHttp)
				return UriScheme.HTTP;
			if (uri.Scheme == Uri.UriSchemeHttps)
				return UriScheme.HTTPS;
			if (EnumHelper.TryParse(uri.Scheme, out UriScheme s))
				return s;
			return UriScheme.Unknown;
		}
	}
	#endregion


	#region pure storage
	/// <summary>
	/// The abstract storage class as a base class for all storage classes whose <see cref="IStorage.LocationDescription"/>.<see cref="CombinationOfLocations.Count">Count</see> == 1 and its <see cref="CombinationOfLocations.Type"/> == <see cref="CombinationType.AllStored"/>.
	/// </summary>
	/// <typeparam name="TP">Any pointer type which implements <see cref="IPointer{TSelf}"/></typeparam>
	/// <remarks>This class only servers as a type identifier which can not be used directly</remarks>
	public abstract class PureStorageBase<TP> where TP : notnull, IPointer<TP>
	{
		/// <summary>
		/// Get the <see cref="PointerSegment{T}"/> of this <see cref="PureStorage{T, TP}"/>
		/// </summary>
		public PointerSegment<TP> Pointer { get; }

		/// <summary>
		/// Create a new <see cref="PureStorageBase{TP}"/> with given <see cref="PointerSegment{T}"/> of type <typeparamref name="TP"/>
		/// </summary>
		/// <param name="pointer">The <see cref="PointerSegment{T}"/> of type <typeparamref name="TP"/> to create from</param>
		protected PureStorageBase(PointerSegment<TP> pointer)
		{
			this.Pointer = pointer;
		}
	}

	/// <summary>
	/// The abstract pure storage class that inherits <see cref="PureStorageBase{TP}"/> and constrains data type to <typeparamref name="T"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct which implements <see cref="INumber{TSelf}"/> as the data type</typeparam>
	/// <typeparam name="TP">Any pointer type which implements <see cref="IPointer{TSelf}"/></typeparam>
	public abstract class PureStorage<T, TP> : PureStorageBase<TP>, IStorage<T, PureStorage<T, TP>> where T : unmanaged, INumber<T> where TP : notnull, IPointer<TP>
	{
		#region basic
		/// <summary>
		/// Create a new <see cref="PureStorage{T, TP}"/> with given <see cref="PointerSegment{T}"/> of type <typeparamref name="TP"/>
		/// </summary>
		/// <param name="pointer">The <see cref="PointerSegment{T}"/> of type <typeparamref name="TP"/> to create from</param>
		protected PureStorage(PointerSegment<TP> pointer) : base(pointer)
		{ }

		/// <summary>
		/// Statically get an empty <see cref="PureStorage{T, TP}"/>
		/// </summary>
		public static PureStorage<T, TP> Empty => new ReferencePureStorage<T, TP>(null);

		/// <summary>
		/// Statically get the data type of this storage as a <see cref="NativeTypes.DataType"/>
		/// </summary>
		public static DataType DataType => NativeType<T>.DataType;

		/// <summary>
		/// Statically get the description of the storage locations of this <see cref="PureStorage{T, TP}"/> as a <see cref="CombinationOfLocations"/>
		/// </summary>
		public static CombinationOfLocations LocationDescription => new(TP.Location);

		/// <summary>
		/// Get the total length of the presenting array in bytes
		/// </summary>
		public long LengthInBytes => Pointer.LengthInBytes;

		/// <summary>
		/// Get the total length of the presenting array in <typeparamref name="T"/>
		/// </summary>
		public long Length => Pointer.LengthInBytes / NativeType<T>.Size;

		void IStorage.Dispose(bool invokedByUser)
		{
			if (this is ActualPureStorage<T, TP>)
			{
				MEM.Free(this.Pointer.Pointer, invokedByUser);
			}
		}

		/// <summary>
		/// Check whether this <see cref="PureStorage{T, TP}"/> is a valid one or not
		/// </summary>
		/// <returns>The validness of this <see cref="PureStorage{T, TP}"/></returns>
		public bool IsValid() => this.Pointer.IsValid();
		#endregion

		#region reference
		/// <summary>
		/// Make a referenced <see cref="PureStorage{T, TP}"/> with the starting pointer moving <paramref name="offset"/> and <see cref="IStorage{T, TSelf}.Length"/> changing to <paramref name="newLength"/>.
		/// </summary>
		/// <param name="offset">The offset in <typeparamref name="T"/> to the starting pointer of this <see cref="PureStorage{T, TP}"/> as a <see cref="long"/></param>
		/// <param name="newLength">The new length in <typeparamref name="T"/> as a <see cref="long"/>, default 0 means automatically calculate from <paramref name="offset"/></param>
		/// <returns>A referenced <see cref="PureStorage{T, TP}"/> of this one</returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offset"/> and <paramref name="newLength"/> is out of boundary</exception>
		public PureStorage<T, TP> MakeReference(long offset = 0, long newLength = 0)
		{
			if (offset == 0 && newLength == 0 && this is ReferencePureStorage<T, TP> @ref)
				return @ref;
			else
				return new ReferencePureStorage<T, TP>(this, offset, newLength);
		}

		/// <summary>
		/// Check whether this <see cref="PureStorage{T, TP}"/> overlaps with the <paramref name="other"/> <see cref="PureStorage{T, TP}"/>.
		/// </summary>
		/// <param name="other">The other <see cref="PureStorage{T, TP}"/> to check overlap</param>
		/// <returns>True if this overlaps with the <paramref name="other"/>, false otherwise</returns>
		public bool OverlapWith(PureStorage<T, TP> other) => this.Pointer.OverlapWith(other.Pointer);

		static PureStorage<T, TP> IStorage<T, PureStorage<T, TP>>.RefFrom<TOut, TOther>(TOther storage)
		{
			return (storage as PureStorage<TOut, TP> ?? throw new InvalidOperationException(Parameter.UnexpectedType)).As<T>();
		}

		/// <summary>
		/// Create a referenced storage of data type <typeparamref name="TOut"/> over this storage
		/// </summary>
		/// <typeparam name="TOut">Any unmanaged number as the new data type</typeparam>
		/// <returns>The referenced <see cref="PureStorage{T, TP}"/> of data type <typeparamref name="TOut"/></returns>
		/// <exception cref="InvalidCastException">If the <see cref="LengthInBytes"/> cannot be divided by the size of <typeparamref name="TOut"/></exception>
		public PureStorage<TOut, TP> As<TOut>() where TOut : unmanaged, INumber<TOut>
		{
			if (typeof(TOut) == typeof(T))
				return this.MakeReference() as PureStorage<TOut, TP> ?? PureStorage<TOut, TP>.Empty;
			IStorage<T, PureStorage<T, TP>>.CheckCast<TOut>(this.Length);
			return new ReferencePureStorage<TOut, TP>(this);
		}
		#endregion

		#region create
		/// <summary>
		/// Statically <b>allocate</b> and create a new <see cref="PureStorage{T, TP}"/> of given <paramref name="combinationType"/> and given locations and lengths.
		/// </summary>
		/// <param name="combinationType">The given <see cref="CombinationType"/> to create</param>
		/// <param name="locations">The given <see cref="StorageLocation"/>s</param>
		/// <param name="lengths">The given lengths in <typeparamref name="T"/></param>
		/// <returns>The created new <see cref="PureStorage{T, TP}"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="locations"/> or <paramref name="lengths"/> is null or empty</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="lengths"/> has length(s) ≤ 0</exception>
		/// <exception cref="OutOfMemoryException">If the underlying allocation failed due to insufficient memory</exception>
		/// <exception cref="InvalidOperationException">If underlying creation fails due to other reasons</exception>
		public static PureStorage<T, TP> Create(CombinationType combinationType, ReadOnlySpan<StorageLocation> locations, ReadOnlySpan<long> lengths)
		{
			if (combinationType != CombinationType.AllStored || locations.Length != 1 || lengths.Length != 1)
				throw new InvalidOperationException(Support.Location);
			if (lengths[0] <= 0)
				throw new ArgumentOutOfRangeException(nameof(lengths), Parameter.MustPositive);
			TP pointer = MEM.Allocate<T>(locations[0], lengths[0]);
			return new ActualPureStorage<T, TP>(pointer);
		}


		static PureStorage<T, TP> IStorage<T, PureStorage<T, TP>>.CreateAlike<TOut, TOther>(TOther storage)
		{
			return CreateAlike(storage as PureStorage<TOut, TP> ?? throw new InvalidOperationException(Parameter.UnexpectedType));
		}

		/// <summary>
		/// Statically allocate and creates a new <see cref="PureStorage{T, TP}"/> alike <paramref name="storage"/>.
		/// </summary>
		/// <param name="storage">The storage of data type <typeparamref name="TOut"/> to mimic.</param>
		/// <returns>A new <see cref="PureStorage{T, TP}"/> that likes <paramref name="storage"/></returns>
		public static PureStorage<T, TP> CreateAlike<TOut>(PureStorage<TOut, TP> storage) where TOut : unmanaged, INumber<TOut>
		{
			var descr = PureStorage<TOut, TP>.LocationDescription;
			return Create(descr.Type, descr.CopyLocationsToSpan(stackalloc StorageLocation[1]), stackalloc long[1] { storage.Length });
		}
		#endregion

		#region operators
		static long IAdditiveIdentity<PureStorage<T, TP>, long>.AdditiveIdentity => 0;

		/// <summary>
		/// Indicates whether the current <see cref="PureStorage{T, TP}"/> is equal to the <paramref name="other"/> <see cref="PureStorage{T, TP}"/> of the same type.
		/// </summary>
		/// <param name="other">The other <see cref="PureStorage{T, TP}"/> to compare to</param>
		/// <returns>true if the current <see cref="PureStorage{T, TP}"/> is equal to the <paramref name="other"/>; otherwise, false.</returns>
		public bool Equals(PureStorage<T, TP>? other) => other is not null && this.Pointer == other.Pointer;

		/// <summary>
		/// Get the hash code of this <see cref="PureStorage{T, TP}"/>
		/// </summary>
		/// <returns>The hash code of this <see cref="PureStorage{T, TP}"/></returns>
		public override int GetHashCode() => this.Pointer.GetHashCode();

		/// <summary>
		/// Check whether this <see cref="PureStorage{T, TP}"/> equals the other <paramref name="obj"/> or not
		/// </summary>
		/// <param name="obj">The other object to compare to</param>
		/// <returns><c>this == <paramref name="obj"/></c></returns>
		public override bool Equals(object? obj) => this.Equals(obj as PureStorage<T, TP>);

		/// <summary>
		/// Statically get the distance in <typeparamref name="T"/> between two <see cref="PureStorage{T, TP}"/>s
		/// </summary>
		/// <param name="left">The left operand of type <see cref="PureStorage{T, TP}"/></param>
		/// <param name="right">The right operand of type <see cref="PureStorage{T, TP}"/></param>
		/// <returns>The distance between two <see cref="PureStorage{T, TP}"/>s in <typeparamref name="T"/> as a <see cref="long"/>.</returns>
		/// <exception cref="InvalidOperationException">If <paramref name="left"/> and <paramref name="right"/> have different origin.</exception>
		public static long operator -(PureStorage<T, TP> left, PureStorage<T, TP> right)
		{
			long diffBytes = IStorage<T, PureStorage<T, TP>>.StorageDiffBytes(left, right);
			if (diffBytes % NativeType<T>.Size != 0)
				throw new InvalidOperationException(Other.CannotDivide);
			return diffBytes / NativeType<T>.Size;
		}

		/// <summary>
		/// <see cref="PureStorage{T, TP}"/> addition operator
		/// </summary>
		public static PureStorage<T, TP> operator +(PureStorage<T, TP> left, long right) => left.MakeReference(right);

		/// <summary>
		/// <see cref="PureStorage{T, TP}"/> subtraction operator
		/// </summary>
		public static PureStorage<T, TP> operator -(PureStorage<T, TP> left, long right) => left.MakeReference(-right);

		/// <summary>
		/// <see cref="PureStorage{T, TP}"/> equality operator
		/// </summary>
		public static bool operator ==(PureStorage<T, TP> left, PureStorage<T, TP> right) => left.Equals(right);

		/// <summary>
		/// <see cref="PureStorage{T, TP}"/> inequality operator
		/// </summary>
		public static bool operator !=(PureStorage<T, TP> left, PureStorage<T, TP> right) => !left.Equals(right);
		#endregion

		#region string
		static string IMainPropertyFormattable<PureStorage<T, TP>>.StringMain => nameof(PureStorage<T, TP>);

		static IEnumerable<string> IMainPropertyFormattable<PureStorage<T, TP>>.PropertyNames => new[] { nameof(DataType), nameof(IStorage<T, PureStorage<T, TP>>.Length), nameof(Pointer) };

		IEnumerable<object?> IMainPropertyFormattable<PureStorage<T, TP>>.PropertyValues => new object?[] { DataType, this.Length, this.Pointer.Pointer.ToString() };

		/// <summary>
		/// Return the string representation of this <see cref="PureStorage{T, TP}"/>
		/// </summary>
		/// <returns>the string representation of this <see cref="PureStorage{T, TP}"/></returns>
		public override string ToString() => IMainPropertyFormattable<PureStorage<T, TP>>.ToString(this);
		#endregion
	}

	/// <summary>
	/// The actual storage class for a pure storage on a single location.
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct which implements <see cref="INumber{TSelf}"/> as the data type</typeparam>
	/// <typeparam name="TP">Any pointer type which implements <see cref="IPointer{TSelf}"/></typeparam>
	public sealed class ActualPureStorage<T, TP> : PureStorage<T, TP>, IActualStorage<T, PureStorage<T, TP>>
		where T : unmanaged, INumber<T>
		where TP : notnull, IPointer<TP>
	{
		/// <summary>
		/// Create a new <see cref="ActualPureStorage{T, TP}"/> from the given <paramref name="pointer"/> of type <typeparamref name="TP"/>
		/// </summary>
		/// <param name="pointer">The given pointer of type <typeparamref name="TP"/></param>
		public ActualPureStorage(TP pointer) : base(new(pointer))
		{
			// do nothing
		}
	}

	/// <summary>
	/// The reference storage class for a pure storage on a single location.
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct which implements <see cref="INumber{TSelf}"/> as the data type</typeparam>
	/// <typeparam name="TP">Any pointer type which implements <see cref="IPointer{TSelf}"/></typeparam>
	public sealed class ReferencePureStorage<T, TP> : PureStorage<T, TP>, IReferenceStorage<T, PureStorage<T, TP>>
		where T : unmanaged, INumber<T>
		where TP : notnull, IPointer<TP>
	{
		/// <summary>
		/// Get the reference <see cref="IStorage"/> of this <see cref="ReferencePureStorage{T, TP}"/>
		/// </summary>
		public IStorage? Reference { get; }

		/// <summary>
		/// Get the total offset of this <see cref="ReferencePureStorage{T, TP}"/> in bytes
		/// </summary>
		public long TotalOffsetInBytes => this.Pointer.OffsetInBytes;

		/// <summary>
		/// Create a new <see cref="ReferencePureStorage{T, TP}"/> from given base <paramref name="storage"/> and <paramref name="offset"/> and <paramref name="newLength"/>.
		/// </summary>
		/// <param name="storage">The base <see cref="IStorage"/> to refer to</param>
		/// <param name="offset">The offset in <typeparamref name="T"/> compared to <paramref name="storage"/></param>
		/// <param name="newLength">The new presenting length in <typeparamref name="T"/></param>
		/// <exception cref="ArgumentException">If <paramref name="storage"/> is not a <see cref="PureStorageBase{TP}"/></exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offset"/> and <paramref name="newLength"/> are out of boundary</exception>
		public ReferencePureStorage(IStorage? storage, long offset = 0, long newLength = 0) :
			base(storage is PureStorageBase<TP> p ? p.Pointer.MoveBy(offset * NativeType<T>.Size, newLength * NativeType<T>.Size) : default)
		{
			var (reference, _, _) = IReferenceStorage<T, PureStorage<T, TP>>.Create<PureStorageBase<TP>>(storage, offset, newLength);
			this.Reference = reference;
		}
	}
	#endregion


	#region cached storage classes
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
	#endregion


	#region factory
	internal delegate IStorage CreateDelegate(CombinationType type, ReadOnlySpan<StorageLocation> locations, ReadOnlySpan<long> lengths);

	/// <summary>
	/// The storage factory for creating concrete storage classes. This is a simple factory pattern.
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
	public static class StorageFactory<T> where T : unmanaged
	{
		#region basic
		/// <summary>
		/// Encapsulates a method that allocates and creates a new <see cref="Storage{T}"/> with given <paramref name="locations"/> and <paramref name="lengths"/>
		/// </summary>
		/// <param name="locations">The given <see cref="ReadOnlySpan{T}"/> of <see cref="StorageLocation"/>s indicating the locations</param>
		/// <param name="lengths">The given <see cref="ReadOnlySpan{T}"/> of <see cref="long"/> indicating the length in <typeparamref name="T"/> of each location</param>
		/// <returns>The created new <see cref="Storage{T}"/></returns>
		/// <remarks>Independent checks for parameters are not necessary</remarks>
		public delegate ActualStorage<T> CreateDelegate(ReadOnlySpan<StorageLocation> locations, ReadOnlySpan<long> lengths);


		private static readonly Dictionary<CombinationType, CreateDelegate> cache_create = new()
		{
			[CombinationType.PureOrMixed] = DefaultCreatePureOrMixed,
			[CombinationType.Cached] = DefaultCreateCached,
		};

		private static ActualStorage<T> DefaultCreatePureOrMixed(ReadOnlySpan<StorageLocation> locations, ReadOnlySpan<long> lengths)
		{
			if (locations.Length == 1)
				return new PureStorage<T>(locations[0], lengths[0]);
			else
				return new MixedStorage<T>(locations, lengths);
		}

		private static ActualStorage<T> DefaultCreateCached(ReadOnlySpan<StorageLocation> locations, ReadOnlySpan<long> lengths)
		{
			if (locations.Length == 2 &&
				locations[0].Type.GetClassification() == LocationTypeExtension.ClassMemory &&
				locations[1].Type.GetClassification() == LocationTypeExtension.ClassStream)
				return new StreamToMemoryCachedStorage<T>(locations[0], locations[1], lengths[0], lengths[1]);
			else
				throw new InvalidOperationException();
		}

		/// <summary>
		/// Set the creation method for a given <see cref="CombinationType"/>
		/// </summary>
		/// <param name="combinationType">The given <see cref="CombinationType"/> to set the creation method</param>
		/// <param name="createDelegate">The creation method as a <see cref="CreateDelegate"/></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void SetCreateMethod(CombinationType combinationType, CreateDelegate createDelegate)
		{
			cache_create[combinationType] = createDelegate;
		}


		private delegate bool _SupportDelegate(CombinationType combinationType, ReadOnlySpan<StorageLocation> locations);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool TryFindAndSetCreateMethod(CombinationType combinationType, ReadOnlySpan<StorageLocation> locations)
		{
			var thisAssembly = Assembly.GetExecutingAssembly();
			var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => a != thisAssembly);
			static IReadOnlyList<Type> GetTypes(Assembly a)
			{
				try
				{
					return a.GetExportedTypes();
				}
				catch (System.Exception)
				{
					return Array.Empty<Type>();
				}
			}
			var types = assemblies.SelectMany(GetTypes);
			Type actualStorageType = typeof(ActualStorage<T>), referenceStorageType = typeof(IReferenceStorage);
			Type[] supportMethodArgTypes = new[] { typeof(CombinationType), typeof(ReadOnlySpan<StorageLocation>) };
			Type[] constructorArgTypes = new[] { typeof(ReadOnlySpan<StorageLocation>), typeof(ReadOnlySpan<long>) };
			for (int i = 0; i < types.Count; i++)
			{
				var type = types[i];
				if (!type.IsGenericType || type.GenericTypeArguments.Length != 1 || type.IsAbstract || type.IsInterface || !type.IsClass)
					continue;
				try
				{
					type = type.MakeGenericType(typeof(T));
				}
				catch (System.Exception)
				{
					continue;
				}
				if (!type.IsAssignableTo(actualStorageType) || type.IsAssignableTo(referenceStorageType))
					continue;
				try
				{
					var method = type.GetMethod(nameof(PureStorageBase<T>.IsSupported), BindingFlags.Public | BindingFlags.Static, null, supportMethodArgTypes, null);
					if (method is null)
						continue;
					if (!method.CreateDelegate<_SupportDelegate>().Invoke(combinationType, locations))
						continue;
				}
				catch (System.Exception)
				{
					continue;
				}
				try
				{
					var ctor = type.GetConstructor(constructorArgTypes);
					if (ctor is null)
						continue;
					DynamicMethod dynamic = new(string.Empty, type, constructorArgTypes, type);
					ILGenerator il = dynamic.GetILGenerator();
					il.DeclareLocal(type);
					il.Emit(OpCodes.Newobj, ctor);
					il.Emit(OpCodes.Stloc_0);
					il.Emit(OpCodes.Ldloc_0);
					il.Emit(OpCodes.Ret);
					var func = (CreateDelegate)dynamic.CreateDelegate(typeof(CreateDelegate));
					if (func is null)
						continue;
					// success
					cache_create.Add(combinationType, func);
					return true;
				}
				catch (System.Exception)
				{
					continue;
				}
			}
			// cannot find any
			return false;
		}
		#endregion

		#region create
		/// <summary>
		/// Allocate and create a new <see cref="Storage{T}"/> with given <paramref name="locations"/> and <paramref name="lengths"/>
		/// </summary>
		/// <param name="type">The <see cref="CombinationType"/> used to identify which creation method to use</param>
		/// <param name="locations">The given <see cref="ReadOnlySpan{T}"/> of <see cref="StorageLocation"/> indicating the locations</param>
		/// <param name="lengths">The given <see cref="ReadOnlySpan{T}"/> of <see cref="long"/> indicating the length in <typeparamref name="T"/> of each location</param>
		/// <returns>The created new <see cref="Storage{T}"/></returns>
		/// <remarks>If the creation method of <paramref name="type"/> is neither default indicated nor manually indicated by <see cref="SetCreateMethod"/>,<br/>
		/// this method will try to find the first suitable one by iterating all exported types of all loaded assemblies,<br/>
		/// (the type with static method like <see cref="PureStorageBase{T}.IsSupported"/> and constructor using <paramref name="locations"/> and <paramref name="lengths"/>)<br/>
		/// which can be <b>really</b> slow. Therefore, try to use <see cref="SetCreateMethod"/> before calling this method if possible.</remarks>
		/// <exception cref="InvalidOperationException">If the creation method of <paramref name="type"/> is neither default indicated nor manually indicated by <see cref="SetCreateMethod(CombinationType, CreateDelegate)"/>, and it cannot be obtained from the public constructors of other assemblies</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ActualStorage<T> Create(CombinationType type, ReadOnlySpan<StorageLocation> locations, ReadOnlySpan<long> lengths)
		{
			if (!cache_create.ContainsKey(type))
				throw new InvalidOperationException();

			if (!cache_create.ContainsKey(type))
			{
				if (!TryFindAndSetCreateMethod(type, locations))
					throw new InvalidOperationException();
			}
			return cache_create[type].Invoke(locations, lengths);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void GetLocationsAndLengths(Storage<T> storage, Span<StorageLocation> locations, Span<long> lengths)
		{
			int sizeT = Const<T>.SizeT;
			var descr = storage.LocationDescription;
			descr.CopyLocationsToSpan(locations);
			// get lengths
			// the most possible case
			if (storage.LengthInBytes == storage.Sum(static p => p.LengthInBytes))
			{
				long bytesLeft = 0;
				for (int i = 0; i < descr.Count; i++)
				{
					long lengthInBytes = bytesLeft + storage[i].LengthInBytes;
					lengths[i] = lengthInBytes / sizeT;
					bytesLeft = lengthInBytes - lengths[i] * sizeT;
				}
				return;
			}
			// else, less possible, need GetActualPointerAt()
			long actualOccupiedBytes = 0;
			for (int i = 0; i < descr.Count; i++)
				actualOccupiedBytes += storage.GetActualPointerAt(i).LengthInBytes;
			if (storage.LengthInBytes == actualOccupiedBytes)
			{
				long bytesLeft = 0;
				for (int i = 0; i < descr.Count; i++)
				{
					long lengthInBytes = bytesLeft + storage.GetActualPointerAt(i).LengthInBytes;
					lengths[i] = lengthInBytes / sizeT;
					bytesLeft = lengthInBytes - lengths[i] * sizeT;
				}
				return;
			}
			// else, cannot auto align
			for (int i = 0; i < descr.Count; i++)
			{
				long length = storage.GetActualPointerAt(i).LengthInBytes;
				if (length % sizeT != 0)
					throw new ArgumentException(Other.CannotDivide, nameof(storage));
				lengths[i] = length / sizeT;
			}
		}

		/// <summary>
		/// Allocate and create a new <see cref="Storage{T}"/> alike the given <paramref name="storage"/>
		/// </summary>
		/// <param name="storage">The given <see cref="Storage{T}"/> as the template of <see cref="Storage{T}.LocationDescription"/> and lengths to create the new one</param>
		/// <returns>A new <see cref="Storage{T}"/> alike <paramref name="storage"/></returns>
		/// <exception cref="ArgumentException">If <paramref name="storage"/>'s <see cref="PointerSegment"/> are not aligned to the size of <typeparamref name="T"/>, meanwhile auto alignment cannot be done with neither <see cref="Storage{T}.this[int]"/> nor <see cref="Storage{T}.GetActualPointerAt(int)"/></exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ActualStorage<T> CreateAlike(Storage<T> storage)
		{
			int sizeT = Const<T>.SizeT;
			var descr = storage.LocationDescription;
			Span<StorageLocation> locations = stackalloc StorageLocation[descr.Count];
			Span<long> lengths = stackalloc long[descr.Count];
			GetLocationsAndLengths(storage, locations, lengths);
			return Create(descr.Type, locations, lengths);
		}

		/// <summary>
		/// Allocate and create a new <see cref="Storage{T}"/> alike the given <paramref name="storage"/>
		/// </summary>
		/// <param name="storage">The given <see cref="Storage{T}"/> as the template of <see cref="Storage{T}.LocationDescription"/> and lengths to create the new one</param>
		/// <returns>A new <see cref="Storage{T}"/> of type <typeparamref name="TOut"/> alike <paramref name="storage"/> with the new lengths in <typeparamref name="TOut"/> equals to the original lengths in <typeparamref name="T"/></returns>
		/// <exception cref="ArgumentException">If <paramref name="storage"/>'s <see cref="PointerSegment"/> are not aligned to the size of <typeparamref name="T"/>, meanwhile auto alignment cannot be done with neither <see cref="Storage{T}.this[int]"/> nor <see cref="Storage{T}.GetActualPointerAt(int)"/></exception>
		public static ActualStorage<TOut> CreateAlike<TOut>(Storage<T> storage) where TOut : unmanaged
		{
			// shortcut
			if (storage is Storage<TOut> s)
				return s.CreateAlike();
			// otherwise
			int sizeT = Const<T>.SizeT;
			var descr = storage.LocationDescription;
			Span<StorageLocation> locations = stackalloc StorageLocation[descr.Count];
			Span<long> lengths = stackalloc long[descr.Count];
			GetLocationsAndLengths(storage, locations, lengths);
			return StorageFactory<TOut>.Create(descr.Type, locations, lengths);
		}
		#endregion
	}
	#endregion
}
