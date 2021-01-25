using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Althea.Linq;


namespace Althea.Storage
{
	/// <summary>
	/// The abstract class for runtime memory API routines 
	/// </summary>
	/// <remarks>The default implementations of methods about <see cref="Storage{T}"/> ensure that you can only implement the basic low-level memory operations of "pure" storages while the high-level methods about <see cref="Storage{T}"/> work fine automatically. However, if there are native supports, it is still recommended to overwrite these methods.</remarks>
	public abstract class AbstractApi : AbstractRuntimeApi
	{
		#region static methods for dispatching
		/// <summary>
		/// Get the current using <see cref="AbstractApi"/>.
		/// </summary>
		/// <remarks><b>DO NOT</b> invoke methods of this property directly unless you are sure about what you are doing; otherwise, there may be exceptions and / or unnoticeable bugs.</remarks>
		public static AbstractApi? Current => RecentAPIs.First?.Value;

		private static readonly LinkedList<AbstractApi> RecentAPIs = new LinkedList<AbstractApi>();

		internal static bool SetImplementation(Type implementation) => SetImplementation(RecentAPIs, implementation);

		/// <summary>
		/// Special version for <see cref="AbstractApi"/> of method <see cref="AbstractRuntimeApi.DisposeNotCurrent{T}(LinkedList{T})"/>
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static void DisposeNotCurrent() => DisposeNotCurrent(RecentAPIs);

		/// <summary>
		/// Special version for <see cref="AbstractApi"/> of method <see cref="AbstractRuntimeApi.SelectImplementation{T}(LinkedList{T}, StorageLocation)"/>
		/// </summary>
		public static AbstractApi SelectImplementation(StorageLocation location) => SelectImplementation(RecentAPIs, location);

		/// <summary>
		/// Special version for <see cref="AbstractApi"/> of method <see cref="AbstractRuntimeApi.SelectImplementation{T}(LinkedList{T}, IStorage)"/>
		/// </summary>
		public static AbstractApi SelectImplementation<T>(Storage<T> storage) where T : unmanaged => SelectImplementation(RecentAPIs, storage);

		/// <summary>
		/// Special version for <see cref="AbstractApi"/> of method <see cref="AbstractRuntimeApi.SelectImplementation{T}(LinkedList{T}, IStorage, IStorage)"/>
		/// </summary>
		public static AbstractApi SelectImplementation<T>(Storage<T> storage1, Storage<T> storage2) where T : unmanaged => SelectImplementation(RecentAPIs, storage1, storage2);
		#endregion


		#region support information
		/// <summary>
		/// Get list of the supported <see cref="CombinationOfLocations"/> for all ternary operations. Since <see cref="AbstractApi"/> has no definition of ternary operations, this override returns null.
		/// </summary>
		public override IReadOnlyList<ImmutableThreeElementSet<CombinationOfLocations>> SupportedTernaryLocations => Array.Empty<ImmutableThreeElementSet<CombinationOfLocations>>();

		// Ignore Spelling: N-ary
		/// <summary>
		/// Get list of the supported <see cref="CombinationOfLocations"/> for all N-ary operations. Each value in the list is a set of <paramref name="N"/> values to indicate a supported combination of certain <see cref="CombinationOfLocations"/>. Or null if there are no N-ary operations.
		/// </summary>
		/// <param name="N">The number of operands, must be <paramref name="N"/> &gt; 0</param>
		/// <returns>The list of the supported memory locations for all N-ary operations. Or null if there are no N-ary operations.</returns>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="N"/> &lt;= 0</exception>
		public override IReadOnlyList<IImmutableSet<CombinationOfLocations>> SupportedNaryLocations(int N)
		{
			return N switch
			{
				1 => this.SupportedUnaryLocations.Select(l => (IImmutableSet<CombinationOfLocations>)(ImmutableZeroOneElementSet<CombinationOfLocations>)l),
				2 => this.SupportedBinaryLocations.Select(l => (IImmutableSet<CombinationOfLocations>)l),
				3 => this.SupportedTernaryLocations.Select(l => (IImmutableSet<CombinationOfLocations>)l),
				> 3 => Array.Empty<IImmutableSet<CombinationOfLocations>>(), // there are no N-ary operations
				_ => throw new ArgumentOutOfRangeException(nameof(N)),
			};
		}

		/// <summary>
		/// When implemented by a derived class, get the list of supported transfer between <see cref="CombinationOfLocations"/> and C# managed memory
		/// </summary>
		public abstract IReadOnlyList<CombinationOfLocations> SupportedManagedTransfer { get; }

		/// <summary>
		/// When implemented by a derived class, check whether the given <see cref="CombinationOfLocations"/> can transfer data with C# managed memory using this implementation
		/// </summary>
		/// <param name="locations">The <see cref="CombinationOfLocations"/> to indicate the unmanaged storage location combination</param>
		/// <returns>Whether this implementation supports data transfer between <paramref name="locations"/> and C# managed memory</returns>
		public virtual bool IsSupportedTransfer(CombinationOfLocations locations) => this.SupportedManagedTransfer.Contains(locations);
		#endregion

		#region properties
		/// <summary>
		/// When implemented by a derived class, get the underlying driver's version of a supported <see cref="LocationType"/>.
		/// </summary>
		/// <param name="location">The given supported <see cref="LocationType"/></param>
		/// <returns>The underlying driver's version of given <paramref name="location"/></returns>
		public abstract (int major, int minor) DriverVersion(LocationType location);

		/// <summary>
		/// When implemented by a derived class, get the maximum number of devices available of a supported <see cref="LocationType"/>.
		/// </summary>
		/// <param name="location">The given supported <see cref="LocationType"/></param>
		/// <returns>The maximum number of devices available of given <paramref name="location"/></returns>
		public abstract int MaxDeviceNumber(LocationType location);

		/// <summary>
		/// When implemented by a derived class, get the available and total memory in bytes for device indicated by a supported <see cref="StorageLocation"/>.
		/// </summary>
		/// <param name="location">The given supported <see cref="StorageLocation"/></param>
		/// <returns>The available and total memory in bytes of device of given <paramref name="location"/></returns>
		public abstract (ulong free, ulong total) FreeAndTotalMemory(StorageLocation location);
		#endregion

		#region low-level storage operations
		/// <summary>
		/// When implemented by a derived class, allocate a storage at given <paramref name="location"/> with given <paramref name="length"/>
		/// </summary>
		/// <param name="location">The <see cref="StorageLocation"/> to allocate on</param>
		/// <param name="length">The length to allocate in bytes</param>
		/// <returns>The allocated pointer as a <see cref="PointerSegment"/></returns>
		/// <exception cref="NotSupportedException">If <paramref name="location"/> is not supported</exception>
		/// <exception cref="InvalidOperationException">If this implementation cannot allocate <paramref name="length"/> on <paramref name="location"/> due to other issues such as <see cref="OutOfMemoryException"/></exception>
		/// <remarks>This methods shall <b>never</b> be exposed publicly to prevent unexpected memory leaks which GC cannot collect due to improper usage.</remarks>
		protected internal abstract PointerSegment Allocate(StorageLocation location, ulong length);

		/// <summary>
		/// When implemented by a derived class, allocate a storage at given <paramref name="location"/> with given <paramref name="length"/>.<br/>The default implementation utilizes <see cref="Allocate(StorageLocation, ulong)"/>.
		/// </summary>
		/// <typeparam name="T">any unmanaged struct</typeparam>
		/// <param name="location">The <see cref="StorageLocation"/> to allocate on</param>
		/// <param name="length">The length to allocate in <typeparamref name="T"/> rather than bytes</param>
		/// <returns>The allocated pointer as a <see cref="PointerSegment"/></returns>
		/// <exception cref="NotSupportedException">If <paramref name="location"/> is not supported</exception>
		/// <exception cref="InvalidOperationException">If this implementation cannot allocate <paramref name="length"/> on <paramref name="location"/> due to other issues such as <see cref="OutOfMemoryException"/></exception>
		/// <remarks>This methods shall <b>never</b> be exposed publicly to prevent unexpected memory leaks which GC cannot collect due to improper usage.</remarks>
		protected internal virtual PointerSegment Allocate<T>(StorageLocation location, ulong length) where T : unmanaged => this.Allocate(location, length * Storage<T>.SizeOfT);

		/// <summary>
		/// When implemented by a derived class, free a storage indicated by a given <paramref name="pointer"/>
		/// </summary>
		/// <param name="pointer">The <see cref="PointerSegment"/> to free</param>
		/// <param name="disposeManaged">dispose managed resources held by <paramref name="pointer"/>'s <see cref="PointerSegment.Pointer"/> or not</param>
		/// <returns>If <paramref name="pointer"/> is not supported or <paramref name="pointer"/> is not valid, return false; otherwise, return true.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="pointer"/> is invalid</exception>
		/// <remarks>This methods shall <b>never</b> be exposed publicly to prevent unexpected wild pointers due to improper usage.</remarks>
		protected internal abstract bool Free(PointerSegment pointer, bool disposeManaged = true);

		/// <summary>
		/// When implemented by a derived class, fill the <paramref name="pointer"/> by same <paramref name="value"/>, byte by byte.
		/// </summary>
		/// <param name="pointer">The pointer to be filled</param>
		/// <param name="value">The value to set as a <see cref="byte"/></param>
		/// <exception cref="NotSupportedException">If <paramref name="pointer"/> is not supported</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="pointer"/> is invalid</exception>
		public abstract void SetMemoryValue(PointerSegment pointer, byte value);

		/// <summary>
		/// When implemented by a derived class, fill the <paramref name="pointer"/>'s each value by same <paramref name="value"/>.
		/// </summary>
		/// <typeparam name="T">any unmanaged struct</typeparam>
		/// <param name="pointer">The pointer to be filled</param>
		/// <param name="value">The value to set as a <typeparamref name="T"/></param>
		/// <exception cref="NotSupportedException">if <paramref name="pointer"/> is not supported</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="pointer"/> is invalid</exception>
		public abstract void SetMemoryValue<T>(PointerSegment pointer, T value) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, copy memory from <paramref name="source"/> to <paramref name="destination"/>.
		/// </summary>
		/// <param name="source">The source pointer to copy from</param>
		/// <param name="destination">The destination pointer to copy into</param>
		/// <remarks>The one with less length in <paramref name="source"/> and <paramref name="destination"/> is used as the actual copy length in bytes</remarks>
		/// <exception cref="NotSupportedException">If copy between <paramref name="source"/> and <paramref name="destination"/> is not supported</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="destination"/> is invalid</exception>
		public abstract void MemoryCopy(PointerSegment source, PointerSegment destination);

		/// <summary>
		/// When implemented by a derived class, copy 2D data from <paramref name="source"/> to <paramref name="destination"/>.
		/// </summary>
		/// <param name="source">The source pointer</param>
		/// <param name="sourceLD">The source array actual height (actual leading dimension) in bytes</param>
		/// <param name="destination">The destination pointer</param>
		/// <param name="destinationLD">The destination array actual height (actual leading dimension) in bytes</param>
		/// <param name="height">The height to copy in bytes</param>
		/// <param name="width">The width to copy in the real type</param>
		/// <remarks>The lengths of <paramref name="source"/> and <paramref name="destination"/> are ignored</remarks>
		/// <exception cref="NotSupportedException">If copy between <paramref name="source"/> and <paramref name="destination"/> is not supported</exception>
		/// <exception cref="ArgumentOutOfRangeException">If any of the parameters is zero</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="destination"/> is invalid</exception>
		/// <exception cref="ArgumentException">
		/// If <paramref name="height"/> is larger than <paramref name="sourceLD"/> or <paramref name="destinationLD"/>,
		/// or <paramref name="height"/> is larger than <paramref name="sourceLD"/> or <paramref name="destinationLD"/>,
		/// or <c><paramref name="sourceLD"/> * <paramref name="width"/> &gt; <paramref name="source"/>.<see cref="IStorage.LengthInBytes">Length</see></c>, 
		/// or <c><paramref name="destinationLD"/> * <paramref name="width"/> &gt; <paramref name="destination"/>.<see cref="IStorage.LengthInBytes">Length</see></c>
		/// </exception>
		public abstract void MemoryCopy2D(PointerSegment source, ulong sourceLD, PointerSegment destination, ulong destinationLD, ulong height, ulong width);
		#endregion

		#region low-level storage and managed operations
		/// <summary>
		/// When implemented by a derived class, copy out the <b>first</b> element in unmanaged pointer <paramref name="source"/> to a managed value of type <typeparamref name="T"/>
		/// </summary>
		/// <typeparam name="T">any unmanaged struct</typeparam>
		/// <param name="source">The source <see cref="PointerSegment"/> to copy from</param>
		/// <returns>The first element in <paramref name="source"/>.</returns>
		/// <exception cref="NotSupportedException">if <paramref name="source"/> is not supported</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> is invalid</exception>
		public abstract T ToManaged<T>(PointerSegment source) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, overwrite the <b>first</b> element in unmanaged pointer <paramref name="destination"/> by a managed <paramref name="value"/> of type <typeparamref name="T"/>
		/// </summary>
		/// <typeparam name="T">any unmanaged struct</typeparam>
		/// <param name="destination">The destination <see cref="PointerSegment"/> to copy to</param>
		/// <param name="value">The value of type <typeparamref name="T"/> to copy from</param>
		/// <exception cref="NotSupportedException">if <paramref name="destination"/> is not supported</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="destination"/> is invalid</exception>
		public abstract void FromManaged<T>(PointerSegment destination, T value) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, copy out the first few elements in unmanaged pointer <paramref name="source"/> to a managed array of type <typeparamref name="T"/>
		/// </summary>
		/// <typeparam name="T">any unmanaged struct</typeparam>
		/// <param name="source">The source <see cref="PointerSegment"/> to copy from</param>
		/// <param name="destination">The managed <see cref="ArraySegment{T}"/> of type <typeparamref name="T"/> to copy to</param>
		/// <exception cref="NotSupportedException">if <paramref name="source"/> is not supported</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> is invalid</exception>
		public abstract void ToManaged<T>(PointerSegment source, ArraySegment<T> destination) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, overwrite the first few elements in unmanaged pointer <paramref name="destination"/> by the <paramref name="values"/> of a managed array of type <typeparamref name="T"/>
		/// </summary>
		/// <typeparam name="T">any unmanaged struct</typeparam>
		/// <param name="destination">The destination <see cref="PointerSegment"/> to copy to</param>
		/// <param name="values">The managed <see cref="ArraySegment{T}"/> of type <typeparamref name="T"/> to copy from</param>
		/// <exception cref="NotSupportedException">if <paramref name="destination"/> is not supported</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="destination"/> is invalid</exception>
		public abstract void FromManaged<T>(PointerSegment destination, ArraySegment<T> values) where T : unmanaged;

		// Ignore Spelling: sizeof
		/// <summary>
		/// When implemented by a derived class, copy out the elements in unmanaged pointer <paramref name="source"/> as a 2D matrix to a managed array of type <typeparamref name="T"/> (viewed as a 1D array).
		/// </summary>
		/// <typeparam name="T">any unmanaged struct</typeparam>
		/// <param name="source">The source <see cref="PointerSegment"/> to copy from</param>
		/// <param name="leadDim">The actual height (actual leading dimension) in <typeparamref name="T"/> of <paramref name="source"/></param>
		/// <param name="height">The height to copy in <typeparamref name="T"/></param>
		/// <param name="width">The width to copy in <typeparamref name="T"/> rather than bytes</param>
		/// <param name="destination">The managed <see cref="ArraySegment{T}"/> of type <typeparamref name="T"/> to copy to, must has <c><see cref="Array.Length">Length</see> ¡Ý <paramref name="height"/> <paramref name="width"/></c></param>
		/// <param name="destinationLeadDim">The actual height (actual leading dimension) in <typeparamref name="T"/> of <paramref name="destination"/>, default 0 means <paramref name="height"/></param>
		/// <exception cref="NotSupportedException">if <paramref name="source"/> is not supported</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> is invalid</exception>
		/// <exception cref="ArgumentException">
		/// If <paramref name="height"/> is larger than <paramref name="leadDim"/> or <paramref name="destinationLeadDim"/>,
		/// or <c><paramref name="leadDim"/> * <paramref name="width"/> * sizeof(<typeparamref name="T"/>) &gt; <paramref name="source"/>.<see cref="PointerSegment.LengthInBytes">Length</see></c>,
		/// or <c><paramref name="destinationLeadDim"/> * <paramref name="width"/> &gt; <paramref name="destination"/></c>.<see cref="Array.Length">Length</see>
		/// </exception>
		public abstract void ToManaged2D<T>(PointerSegment source, ulong leadDim, ulong height, ulong width, ArraySegment<T> destination, ulong destinationLeadDim = 0) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, overwrite some of the elements in unmanaged pointer <paramref name="destination"/> as a 2D matrix by  a managed array of type <typeparamref name="T"/> (viewed as a 1D array).
		/// </summary>
		/// <typeparam name="T">any unmanaged struct</typeparam>
		/// <param name="destination">The destination <see cref="PointerSegment"/> to copy to</param>
		/// <param name="leadDim">The actual height (actual leading dimension) in <typeparamref name="T"/> of <paramref name="destination"/></param>
		/// <param name="height">The height to copy in <typeparamref name="T"/></param>
		/// <param name="width">The width to copy in <typeparamref name="T"/> rather than bytes</param>
		/// <param name="values">The managed <see cref="ArraySegment{T}"/> of type <typeparamref name="T"/> to copy from, must has <c><see cref="Array.Length">Length</see> ¡Ý <paramref name="height"/> * <paramref name="width"/></c></param>
		/// <param name="valuesLeadDim">The actual height (actual leading dimension) in <typeparamref name="T"/> of <paramref name="values"/>, default 0 means <paramref name="height"/></param>
		/// <exception cref="NotSupportedException">if <paramref name="destination"/> is not supported</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="destination"/> is invalid</exception>
		/// <exception cref="ArgumentException">
		/// If <paramref name="height"/> is larger than <paramref name="leadDim"/> or <paramref name="valuesLeadDim"/>,
		/// or <c><paramref name="leadDim"/> * <paramref name="width"/> * sizeof(<typeparamref name="T"/>) &gt; <paramref name="destination"/>.<see cref="PointerSegment.LengthInBytes">Length</see></c>,
		/// or <c><paramref name="valuesLeadDim"/> * <paramref name="width"/> &gt; <paramref name="values"/></c>.<see cref="Array.Length">Length</see>
		/// </exception>
		public abstract void FromManaged2D<T>(PointerSegment destination, ulong leadDim, ulong height, ulong width, ArraySegment<T> values, ulong valuesLeadDim = 0) where T : unmanaged;
		#endregion

		#region high-level storage operations
		/// <summary>
		/// Get the actual storage of the given <paramref name="storage"/> if it is a <see cref="PureOrMixedStorage{T}"/> or a <see cref="CachedStorage{T}"/>.
		/// </summary>
		/// <typeparam name="T">any unmanaged data type</typeparam>
		/// <param name="storage">The given <see cref="Storage{T}"/> to dereference</param>
		/// <param name="mixed">The output nullable <see cref="IPureOrMixedStorage"/></param>
		/// <param name="cached">The output nullable <see cref="ICachedStorage"/></param>
		/// <returns>The offset in bytes and the length in bytes</returns>
		/// <exception cref="NotImplementedException">If <paramref name="storage"/> is neither <see cref="ActualStorage{T}"/> nor <see cref="ReferenceStorage{T}"/> or it is <see cref="ReferenceStorage{T}"/> while its dereference is neither <see cref="PureOrMixedStorage{T}"/> nor <see cref="CachedStorage{T}"/></exception>
		protected static void Cast<T>(Storage<T> storage, out IPureOrMixedStorage? mixed, out ICachedStorage? cached) where T : unmanaged
		{
			mixed = null; cached = null;
			if (storage is IPureOrMixedStorage mix)
			{
				mixed = mix;
			}
			else if (storage is ICachedStorage cache)
			{
				cached = cache;
			}
			else
				throw new NotImplementedException();
		}

		/// <summary>
		/// When implemented by a derived class, fill the <paramref name="storage"/> byte by byte to the same <paramref name="value"/>.<br/>The default implementation only works for <see cref="Storage{T}.LocationDescription"/>.<see cref="CombinationOfLocations.Type">Type</see> == <see cref="CombinationType.PureOrMixed"/> or <see cref="CombinationType.Cached"/>.
		/// </summary>
		/// <typeparam name="T">any unmanaged struct</typeparam>
		/// <param name="storage">The <see cref="Storage{T}"/> to be filled</param>
		/// <param name="value">The value to fill</param>
		/// <exception cref="NotSupportedException">If <paramref name="storage"/> is not supported</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="storage"/> is null or invalid</exception>
		public virtual void SetMemoryValue<T>(Storage<T> storage, byte value) where T : unmanaged
		{
			if (!storage.IsValid())
				throw new ArgumentNullException(nameof(storage));

			Cast(storage, out IPureOrMixedStorage? mixed, out ICachedStorage? cached);
			if (mixed is not null)
			{
				for (int i = 0; i < storage.Count; i++)
				{
					this.SetMemoryValue(storage[i], value);
				}
			}
			else if (cached is not null)
			{
				cached.Flush(); this.SetMemoryValue(cached[0], value);
			}
		}

		/// <summary>
		/// When implemented by a derived class, fill the <paramref name="storage"/>'s each value by same <paramref name="value"/>.<br/>The default implementation only works for <see cref="Storage{T}.LocationDescription"/>.<see cref="CombinationOfLocations.Type">Type</see> == <see cref="CombinationType.PureOrMixed"/> or <see cref="CombinationType.Cached"/>.
		/// </summary>
		/// <typeparam name="T">any unmanaged struct</typeparam>
		/// <param name="storage">The <see cref="Storage{T}"/> to be filled</param>
		/// <param name="value">The value to fill</param>
		/// <exception cref="NotSupportedException">If <paramref name="storage"/> is not supported</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="storage"/> is null or invalid</exception>
		public virtual void SetMemoryValue<T>(Storage<T> storage, T value) where T : unmanaged
		{
			if (!storage.IsValid())
				throw new ArgumentNullException(nameof(storage));

			Cast(storage, out IPureOrMixedStorage? mixed, out ICachedStorage? cached);
			if (mixed is not null)
			{
				for (int i = 0; i < storage.Count; i++)
				{
					this.SetMemoryValue(storage[i], value);
				}
			}
			else if (cached is not null)
			{
				cached.Flush(); this.SetMemoryValue(cached[0], value);
			}
		}

		/// <summary>
		/// When implemented by a derived class, copy memory from <paramref name="source"/> to <paramref name="destination"/> where both are <see cref="IPureOrMixedStorage"/>. The default implementation utilizes the <see cref="MemoryCopy(PointerSegment, PointerSegment)"/>.
		/// </summary>
		/// <param name="source">The source <see cref="IPureOrMixedStorage"/> to copy from</param>
		/// <param name="destination">The destination <see cref="IPureOrMixedStorage"/> to copy into</param>
		/// <remarks>The one with less length among <paramref name="source"/> and <paramref name="destination"/> is used as the actual copy length</remarks>
		protected virtual void MixedStorageMemoryCopy(IPureOrMixedStorage source, IPureOrMixedStorage destination)
		{
			PointerSegment src = source[0], dst = destination[0];
			bool incSrc = false, incDst = false;
			int i = 0, j = 0;
			while (i < source.Count && j < destination.Count)
			{
				if (incSrc)
					src = source[i];
				if (incDst)
					dst = destination[j];
				this.MemoryCopy(src, dst);
				if (src.LengthInBytes > dst.LengthInBytes)
				{
					src += (long)dst.LengthInBytes;
					incDst = true; incSrc = false;
					j++;
				}
				else if (src.LengthInBytes < dst.LengthInBytes)
				{
					dst += (long)src.LengthInBytes;
					incDst = false; incSrc = true;
					i++;
				}
				else
				{
					incDst = true; incSrc = true;
					i++; j++;
				}
			}
		}

		/// <summary>
		/// When implemented by a derived class, copy memory from a <see cref="IPureOrMixedStorage"/> <paramref name="source"/> to a <see cref="ICachedStorage"/> <paramref name="destination"/>. The default implementation utilizes the <see cref="MemoryCopy(PointerSegment, PointerSegment)"/>.
		/// </summary>
		/// <param name="source">The source <see cref="PureOrMixedStorage{T}"/> to copy from</param>
		/// <param name="destination">The destination <see cref="CachedStorage{T}"/> to copy into</param>
		/// <remarks>The one with less length in <paramref name="source"/> and <paramref name="destination"/> is used as the actual copy length</remarks>
		protected virtual void MixedToCachedStorageMemoryCopy(IPureOrMixedStorage source, ICachedStorage destination)
		{
			// TODO
		}

		/// <summary>
		/// When implemented by a derived class, copy memory from a <see cref="ICachedStorage"/> <paramref name="source"/> to a <see cref="IPureOrMixedStorage"/> <paramref name="destination"/>. The default implementation utilizes the <see cref="MemoryCopy(PointerSegment, PointerSegment)"/>.
		/// </summary>
		/// <param name="source">The source <see cref="PureOrMixedStorage{T}"/> to copy from</param>
		/// <param name="destination">The destination <see cref="CachedStorage{T}"/> to copy into</param>
		/// <remarks>The one with less length among <paramref name="source"/> and <paramref name="destination"/> is used as the actual copy length</remarks>
		protected virtual void CachedToMixedStorageMemoryCopy(ICachedStorage source, IPureOrMixedStorage destination)
		{
			// TODO
		}

		/// <summary>
		/// When implemented by a derived class, copy memory from <paramref name="source"/> to <paramref name="destination"/> where both are <see cref="ICachedStorage"/>. The default implementation utilizes the <see cref="MemoryCopy(PointerSegment, PointerSegment)"/>.
		/// </summary>
		/// <param name="source">The source <see cref="ICachedStorage"/> to copy from</param>
		/// <param name="destination">The destination <see cref="ICachedStorage"/> to copy into</param>
		/// <remarks>The one with less length among <paramref name="source"/> and <paramref name="destination"/> is used as the actual copy length</remarks>
		protected virtual void CachedStorageMemoryCopy(ICachedStorage source, ICachedStorage destination)
		{
			// TODO
		}

		/// <summary>
		/// When implemented by a derived class, copy memory from <paramref name="source"/> to <paramref name="destination"/>.<br/>The default implementation only works for <see cref="Storage{T}.LocationDescription"/>.<see cref="CombinationOfLocations.Type">Type</see> == <see cref="CombinationType.PureOrMixed"/> or <see cref="CombinationType.Cached"/>.
		/// </summary>
		/// <typeparam name="T">any unmanaged struct</typeparam>
		/// <param name="source">The source <see cref="Storage{T}"/> to copy from</param>
		/// <param name="destination">The destination <see cref="Storage{T}"/> pointer to copy into</param>
		/// <remarks>The one with less length among <paramref name="source"/> and <paramref name="destination"/> is used as the actual copy length</remarks>
		/// <exception cref="NotSupportedException">If <paramref name="source"/> or <paramref name="destination"/> is not supported</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="destination"/> is null or invalid</exception>
		public virtual void MemoryCopy<T>(Storage<T> source, Storage<T> destination) where T : unmanaged
		{
			if (!source.IsValid())
				throw new ArgumentNullException(nameof(source));
			if (!destination.IsValid())
				throw new ArgumentNullException(nameof(destination));

			Cast(source, out IPureOrMixedStorage? srcMixed, out ICachedStorage? srcCached);
			Cast(destination, out IPureOrMixedStorage? dstMixed, out ICachedStorage? dstCached);
			// normal cases
			if (srcMixed is not null && dstMixed is not null)
			{
				// shortcut
				if (source.Count == 1 && destination.Count == 1)
				{
					this.MemoryCopy(source[0], destination[0]);
					return;
				}
				this.MixedStorageMemoryCopy(srcMixed, dstMixed);
			}
			else if (srcMixed is not null && dstCached is not null)
			{
				this.MixedToCachedStorageMemoryCopy(srcMixed, dstCached);
			}
			else if (srcCached is not null && dstMixed is not null)
			{
				this.CachedToMixedStorageMemoryCopy(srcCached, dstMixed);
			}
			else if (srcCached is not null && dstCached is not null)
			{
				this.CachedStorageMemoryCopy(srcCached, dstCached);
			}
		}

		/// <summary>
		/// When implemented by a derived class, copy 2D data from <paramref name="source"/> to <paramref name="destination"/>.<br/>The default implementation only works for <see cref="Storage{T}.LocationDescription"/>.<see cref="CombinationOfLocations.Type">Type</see> == <see cref="CombinationType.PureOrMixed"/> or <see cref="CombinationType.Cached"/>.
		/// </summary>
		/// <typeparam name="T">any unmanaged struct</typeparam>
		/// <param name="source">The source <see cref="Storage{T}"/></param>
		/// <param name="sourceLD">The source array actual height (actual leading dimension) in <typeparamref name="T"/></param>
		/// <param name="destination">The destination <see cref="Storage{T}"/></param>
		/// <param name="destLD">The destination array actual height (actual leading dimension) in <typeparamref name="T"/></param>
		/// <param name="height">The height to copy in <typeparamref name="T"/></param>
		/// <param name="width">The width to copy in the real type</param>
		/// <remarks>The lengths of <paramref name="source"/> and <paramref name="destination"/> are <b>not</b> ignored</remarks>
		/// <exception cref="NotSupportedException">If <paramref name="source"/> or <paramref name="destination"/> is not supported</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="destination"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If any of the parameters is zero</exception>
		/// <exception cref="ArgumentException">
		/// If <paramref name="height"/> is larger than <paramref name="sourceLD"/> or <paramref name="destLD"/>,
		/// or <c><paramref name="sourceLD"/> * <paramref name="width"/> &gt; <paramref name="source"/>.<see cref="Storage{T}.Length">Length</see></c>, 
		/// or <c><paramref name="destLD"/> * <paramref name="width"/> &gt; <paramref name="destination"/>.<see cref="Storage{T}.Length">Length</see></c>
		/// </exception>
		public virtual void MemoryCopy2D<T>(Storage<T> source, ulong sourceLD, Storage<T> destination, ulong destLD, ulong height, ulong width) where T : unmanaged
		{
			if (!source.IsValid())
				throw new ArgumentNullException(nameof(source));
			if (!destination.IsValid())
				throw new ArgumentNullException(nameof(destination));
			if (sourceLD == 0)
				throw new ArgumentOutOfRangeException(nameof(sourceLD), Resources.Parameter.MustPositive);
			if (destLD == 0)
				throw new ArgumentOutOfRangeException(nameof(destLD), Resources.Parameter.MustPositive);
			if (width == 0)
				throw new ArgumentOutOfRangeException(nameof(width), Resources.Parameter.MustPositive);
			if (height == 0)
				throw new ArgumentOutOfRangeException(nameof(height), Resources.Parameter.MustPositive);
			if (height > sourceLD || height > destLD)
				throw new ArgumentException(Resources.Parameter.InvalidValue, nameof(height));
			if (sourceLD * width > source.LengthInBytes)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(source));
			if (destLD * width > destination.LengthInBytes)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(destination));

			Cast(source, out IPureOrMixedStorage? srcMixed, out ICachedStorage? _);
			Cast(destination, out IPureOrMixedStorage? dstMixed, out ICachedStorage? _);
			// shortcut
			if (srcMixed is not null && dstMixed is not null && source.Count == 1 && destination.Count == 1)
			{
				this.MemoryCopy2D(source[0], sourceLD, destination[0], destLD, height, width);
				return;
			}
			long srcLD = (long)sourceLD, dstLD = (long)destLD;
			source = source.MakeReference(newLength: height);
			destination = destination.MakeReference(newLength: height);
			for (ulong column = 0; column < width - 1; column++)
			{
				this.MemoryCopy(source, destination);
				source = source.MakeReference(offset: srcLD, newLength: height);
				destination = destination.MakeReference(offset: dstLD, newLength: height);
			}
			// copy last column
			this.MemoryCopy(source, destination);
		}

		#endregion

		#region high-level storage and managed operations
		/// <summary>
		/// When implemented by a derived class, copy out the <b>first</b> element in <see cref="Storage{T}"/> <paramref name="source"/> to a managed value of type <typeparamref name="T"/>.<br/>The default implementation only works for <see cref="Storage{T}.LocationDescription"/>.<see cref="CombinationOfLocations.Type">Type</see> == <see cref="CombinationType.PureOrMixed"/>.
		/// </summary>
		/// <typeparam name="T">any unmanaged struct</typeparam>
		/// <param name="source">The source <see cref="Storage{T}"/> to copy from</param>
		/// <returns>The first element in <paramref name="source"/>.</returns>
		/// <exception cref="NotSupportedException">if <paramref name="source"/> is not supported</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> is null or invalid</exception>
		public virtual T ToManaged<T>(Storage<T> source) where T : unmanaged
		{
			if (!source.IsValid())
				throw new ArgumentNullException(nameof(source));

			return this.ToManaged<T>(source[0]);
		}

		/// <summary>
		/// When implemented by a derived class, overwrite the <b>first</b> element in unmanaged pointer <paramref name="destination"/> by a managed <paramref name="value"/> of type <typeparamref name="T"/>.<br/>The default implementation only works for <see cref="Storage{T}.LocationDescription"/>.<see cref="CombinationOfLocations.Type">Type</see> == <see cref="CombinationType.PureOrMixed"/>.
		/// </summary>
		/// <typeparam name="T">any unmanaged struct</typeparam>
		/// <param name="destination">The destination <see cref="Storage{T}"/> to copy to</param>
		/// <param name="value">The value of type <typeparamref name="T"/> to copy from</param>
		/// <exception cref="NotSupportedException">if <paramref name="destination"/> is not supported</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="destination"/> is null or invalid</exception>
		public virtual void FromManaged<T>(Storage<T> destination, T value) where T : unmanaged
		{
			if (!destination.IsValid())
				throw new ArgumentNullException(nameof(destination));

			this.FromManaged(destination[0], value);
		}

		/// <summary>
		/// When implemented by a derived class, copy out the first few elements in unmanaged pointer <paramref name="source"/> to a managed array of type <typeparamref name="T"/>.<br/>The default implementation only works for <see cref="Storage{T}.LocationDescription"/>.<see cref="CombinationOfLocations.Type">Type</see> == <see cref="CombinationType.PureOrMixed"/>.
		/// </summary>
		/// <typeparam name="T">any unmanaged struct</typeparam>
		/// <param name="source">The source <see cref="Storage{T}"/> to copy from</param>
		/// <param name="destination">The managed <see cref="ArraySegment{T}"/> of type <typeparamref name="T"/> to copy to</param>
		/// <exception cref="NotSupportedException">if <paramref name="source"/> is not supported</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> is null or invalid</exception>
		public virtual void ToManaged<T>(Storage<T> source, ArraySegment<T> destination) where T : unmanaged
		{
			if (!source.IsValid())
				throw new ArgumentNullException(nameof(source));

			int offset = 0;
			for (int i = 0; i < source.Count; i++)
			{
				this.ToManaged(source[i], destination[offset..]);
				if (offset >= destination.Count)
					return;
				offset += (int)(source[i].LengthInBytes / Storage<T>.SizeOfT);
			}
		}

		/// <summary>
		/// When implemented by a derived class, overwrite the first few elements in unmanaged pointer <paramref name="destination"/> by the <paramref name="values"/> of a managed array of type <typeparamref name="T"/>.<br/>The default implementation only works for <see cref="Storage{T}.LocationDescription"/>.<see cref="CombinationOfLocations.Type">Type</see> == <see cref="CombinationType.PureOrMixed"/>.
		/// </summary>
		/// <typeparam name="T">any unmanaged struct</typeparam>
		/// <param name="destination">The destination <see cref="Storage{T}"/> to copy to</param>
		/// <param name="values">The managed <see cref="ArraySegment{T}"/> of type <typeparamref name="T"/> to copy from</param>
		/// <exception cref="NotSupportedException">if <paramref name="destination"/> is not supported</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="destination"/> is invalid</exception>
		public virtual void FromManaged<T>(Storage<T> destination, ArraySegment<T> values) where T : unmanaged
		{
			if (!destination.IsValid())
				throw new ArgumentNullException(nameof(destination));

			int offset = 0;
			for (int i = 0; i < destination.Count; i++)
			{
				this.FromManaged(destination[i], values[offset..]);
				if (offset >= values.Count)
					return;
				offset += (int)(destination[i].LengthInBytes / Storage<T>.SizeOfT);
			}
		}

		/// <summary>
		/// When implemented by a derived class, copy out the elements in unmanaged pointer <paramref name="source"/> as a 2D matrix to a managed array of type <typeparamref name="T"/> (viewed as a 1D array).<br/>The default implementation only works for <see cref="Storage{T}.LocationDescription"/>.<see cref="CombinationOfLocations.Type">Type</see> == <see cref="CombinationType.PureOrMixed"/>.
		/// </summary>
		/// <typeparam name="T">any unmanaged struct</typeparam>
		/// <param name="source">The source <see cref="Storage{T}"/> to copy from</param>
		/// <param name="leadDim">The actual height (actual leading dimension) in <typeparamref name="T"/> of <paramref name="source"/></param>
		/// <param name="height">The height to copy in <typeparamref name="T"/></param>
		/// <param name="width">The width to copy in <typeparamref name="T"/> rather than bytes</param>
		/// <param name="destination">The managed <see cref="ArraySegment{T}"/> of type <typeparamref name="T"/> to copy to, must has <c><see cref="Array.Length">Length</see> ¡Ý <paramref name="height"/> * <paramref name="width"/></c></param>
		/// <param name="destinationLeadDim">The actual height (actual leading dimension) in <typeparamref name="T"/> of <paramref name="destination"/>, default 0 means <paramref name="height"/></param>
		/// <exception cref="NotSupportedException">if <paramref name="source"/> is not supported</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> is invalid</exception>
		/// <exception cref="ArgumentException">
		/// If <paramref name="height"/> is larger than <paramref name="leadDim"/> or <paramref name="destinationLeadDim"/>,
		/// or <c><paramref name="leadDim"/> * <paramref name="width"/> &gt; <paramref name="source"/>.<see cref="Storage{T}.Length">Length</see></c>,
		/// or <c><paramref name="destinationLeadDim"/> * <paramref name="width"/> &gt; <paramref name="destination"/></c>.<see cref="Array.Length">Length</see>
		/// </exception>
		public virtual void ToManaged2D<T>(Storage<T> source, ulong leadDim, ulong height, ulong width, ArraySegment<T> destination, ulong destinationLeadDim = 0) where T : unmanaged
		{
			if (!source.IsValid())
				throw new ArgumentNullException(nameof(source));
			if (leadDim == 0)
				throw new ArgumentOutOfRangeException(nameof(leadDim), Resources.Parameter.MustPositive);
			if (width == 0)
				throw new ArgumentOutOfRangeException(nameof(width), Resources.Parameter.MustPositive);
			if (height == 0)
				throw new ArgumentOutOfRangeException(nameof(height), Resources.Parameter.MustPositive);
			if (height > leadDim || height > destinationLeadDim)
				throw new ArgumentException(Resources.Parameter.InvalidValue, nameof(height));
			if (leadDim * width > source.Length)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(source));
			if (leadDim * width > (ulong)destination.Count)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(destination));
			// shortcut
			if (source.Count == 1)
			{
				this.ToManaged2D(source[0], leadDim, height, width, destination, destinationLeadDim);
				return;
			}
			// normal case
			int h = checked((int)height), dstLD = checked((int)(destinationLeadDim == 0 ? height : destinationLeadDim));
			long srcLD = (long)leadDim, max = (long)(leadDim * width);
			int dstOffset = 0;
			for (long srcOffset = 0; srcOffset < max; srcOffset += srcLD, dstOffset += dstLD)
			{
				var src = source.MakeReference(offset: srcOffset, newLength: height);
				var dst = destination.Slice(dstOffset, h);
				this.ToManaged(src, dst);
			}
		}

		/// <summary>
		/// When implemented by a derived class, overwrite some of the elements in unmanaged pointer <paramref name="destination"/> as a 2D matrix by  a managed array of type <typeparamref name="T"/> (viewed as a 1D array).<br/>The default implementation only works for <see cref="Storage{T}.LocationDescription"/>.<see cref="CombinationOfLocations.Type">Type</see> == <see cref="CombinationType.PureOrMixed"/>.
		/// </summary>
		/// <typeparam name="T">any unmanaged struct</typeparam>
		/// <param name="destination">The destination <see cref="Storage{T}"/> to copy to</param>
		/// <param name="leadDim">The actual height (actual leading dimension) in <typeparamref name="T"/> of <paramref name="destination"/></param>
		/// <param name="height">The height to copy in <typeparamref name="T"/></param>
		/// <param name="width">The width to copy in <typeparamref name="T"/> rather than bytes</param>
		/// <param name="values">The managed <see cref="ArraySegment{T}"/> of type <typeparamref name="T"/> to copy from, must has <c><see cref="Array.Length">Length</see> ¡Ý <paramref name="height"/> * <paramref name="width"/></c></param>
		/// <param name="valuesLeadDim">The actual height (actual leading dimension) in <typeparamref name="T"/> of <paramref name="values"/>, default 0 means <paramref name="height"/></param>
		/// <exception cref="NotSupportedException">if <paramref name="destination"/> is not supported</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="destination"/> is invalid</exception>
		/// <exception cref="ArgumentException">
		/// If <paramref name="height"/> is larger than <paramref name="leadDim"/> or <paramref name="valuesLeadDim"/>,
		/// or <c><paramref name="leadDim"/> * <paramref name="width"/> &gt; <paramref name="destination"/>.<see cref="Storage{T}.Length">Length</see></c>,
		/// or <c><paramref name="valuesLeadDim"/> * <paramref name="width"/> &gt; <paramref name="values"/></c>.<see cref="Array.Length">Length</see>
		/// </exception>
		public virtual void FromManaged2D<T>(Storage<T> destination, ulong leadDim, ulong height, ulong width, ArraySegment<T> values, ulong valuesLeadDim = 0) where T : unmanaged
		{
			if (!destination.IsValid())
				throw new ArgumentNullException(nameof(destination));
			if (leadDim == 0)
				throw new ArgumentOutOfRangeException(nameof(leadDim), Resources.Parameter.MustPositive);
			if (width == 0)
				throw new ArgumentOutOfRangeException(nameof(width), Resources.Parameter.MustPositive);
			if (height == 0)
				throw new ArgumentOutOfRangeException(nameof(height), Resources.Parameter.MustPositive);
			if (height > leadDim || height > valuesLeadDim)
				throw new ArgumentException(Resources.Parameter.InvalidValue, nameof(height));
			if (leadDim * width > destination.Length)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(destination));
			if (leadDim * width > (ulong)values.Count)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(values));
			// shortcut
			if (destination.Count == 1)
			{
				this.FromManaged2D(destination[0], leadDim, height, width, values);
				return;
			}
			// normal case
			int h = checked((int)height), srcLD = checked((int)(valuesLeadDim == 0 ? height : valuesLeadDim));
			long dstLD = (long)leadDim, max = (long)(leadDim * width);
			int srcOffset = 0;
			for (long dstOffset = 0; dstOffset < max; dstOffset += dstLD, srcOffset += srcLD)
			{
				var dst = destination.MakeReference(offset: dstOffset, newLength: height);
				var src = values.Slice(srcOffset, h);
				this.FromManaged(dst, src);
			}
		}
		#endregion
	}
}