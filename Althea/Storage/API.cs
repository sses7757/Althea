using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Althea.Linq;
using Althea.Helpers;


namespace Althea.Storage
{
	/// <summary>
	/// The abstract class for runtime memory API routines 
	/// </summary>
	public abstract partial class AbstractApi : AbstractRuntimeApi
	{
		#region static methods for dispatching
		/// <summary>
		/// Get the current using <see cref="AbstractApi"/>.
		/// </summary>
		/// <remarks><b>DO NOT</b> invoke methods of this property directly unless you are sure about what you are doing; otherwise, there may be exceptions and / or unnoticeable bugs.</remarks>
		public static AbstractApi Current => RecentAPIs.First?.Value;

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
		/// Get list of the supported memory locations for all ternary operations. Since <see cref="AbstractApi"/> has no definition of ternary operations, this override returns null.
		/// </summary>
		public override IReadOnlyList<ImmutableThreeElementSet<CombinationOfLocations>> SupportedTernaryLocations => null;

		// Ignore Spelling: N-ary
		/// <summary>
		/// Get list of the supported memory locations for all N-ary operations. Each in the list is a set of <paramref name="N"/> values to indicate a supported combination of certain (mixed) memory locations. Or null if there are no N-ary operations.
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
				> 3 => null, // there are no N-ary operations
				_ => throw new ArgumentOutOfRangeException(nameof(N)),
			};
		}

		/// <summary>
		/// Get the supported URI direct transfer dictionary. Each value indicates that this <see cref="AbstractApi"/> supports the <b>direct</b> data transfer between the given <see cref="LocationType"/> (combination of flags) and the given <see cref="UriScheme"/>.
		/// </summary>
		public abstract IReadOnlyDictionary<UriScheme, LocationType> SupportedUriTransfers { get; }

		/// <summary>
		/// Check if the direct data transfer of given <paramref name="location"/> and given <paramref name="uriScheme"/> is supported by this <see cref="AbstractApi"/> or not.
		/// </summary>
		/// <param name="location">The given <see cref="LocationType"/>, must not be a <see cref="LocationType.Uri"/>.</param>
		/// <param name="uriScheme">The given <see cref="UriScheme"/>.</param>
		/// <returns>Whether the direct data transfer of <paramref name="location"/> and <paramref name="uriScheme"/> is supported by this <see cref="AbstractApi"/>.</returns>
		public virtual bool IsSupportedTransfer(LocationType location, UriScheme uriScheme) => location != LocationType.Uri && (this.SupportedUriTransfers[uriScheme] & location) == location;
		#endregion

		#region properties
		/// <summary>
		/// Get the underlying driver's version of a supported <see cref="LocationType"/>.
		/// </summary>
		/// <param name="location">The given supported <see cref="LocationType"/></param>
		/// <returns>The underlying driver's version of given <paramref name="location"/></returns>
		public abstract (int major, int minor) DriverVersion(LocationType location);

		/// <summary>
		/// Get the maximum number of devices available of a supported <see cref="LocationType"/>.
		/// </summary>
		/// <param name="location">The given supported <see cref="LocationType"/></param>
		/// <returns>The maximum number of devices available of given <paramref name="location"/></returns>
		public abstract int MaxDeviceNumber(LocationType location);

		/// <summary>
		/// Get the available and total memory in bytes for device indicated by a supported <see cref="StorageLocation"/>.
		/// </summary>
		/// <param name="location">The given supported <see cref="StorageLocation"/></param>
		/// <returns>The available and total memory in bytes of device of given <paramref name="location"/></returns>
		public abstract (ulong free, ulong total) FreeAndTotalMemory(StorageLocation location);
		#endregion

		#region URI related
		/// <summary>
		/// Create a <see cref="IUriWrapper"/> of given <paramref name="uri"/> which supports data transfer defined in <see cref="SupportedUriTransfers"/>
		/// </summary>
		/// <param name="uri">The given <see cref="Uri"/> used to create wrapper</param>
		/// <returns>A instance of <see cref="IUriWrapper"/> of given <paramref name="uri"/></returns>
		/// <exception cref="NotSupportedException">if <paramref name="uri"/> is not supported</exception>
		public abstract IUriWrapper CreateUriStream(Uri uri);
		#endregion

		#region low-level memory operations
		/// <summary>
		/// Allocate a storage at given <paramref name="location"/> with given <paramref name="length"/>
		/// </summary>
		/// <param name="location">The <see cref="StorageLocation"/> to allocate on</param>
		/// <param name="length">The length to allocate in bytes</param>
		/// <returns>The allocated pointer as a <see cref="StoragePointer"/></returns>
		/// <exception cref="NotSupportedException">if <paramref name="location"/> is not supported</exception>
		public abstract StoragePointer Allocate(StorageLocation location, ulong length);

		/// <summary>
		/// Allocate a storage at given <paramref name="location"/> with given <paramref name="length"/>
		/// </summary>
		/// <typeparam name="T">any unmanaged struct</typeparam>
		/// <param name="location">The <see cref="StorageLocation"/> to allocate on</param>
		/// <param name="length">The length to allocate in <typeparamref name="T"/> rather than bytes</param>
		/// <returns>The allocated pointer as a <see cref="StoragePointer"/></returns>
		/// <exception cref="NotSupportedException">if <paramref name="location"/> is not supported</exception>
		public virtual StoragePointer Allocate<T>(StorageLocation location, ulong length) where T : unmanaged => this.Allocate(location, length * Storage<T>.SizeOfT);

		/// <summary>
		/// Free a storage indicated by a given <paramref name="pointer"/>
		/// </summary>
		/// <param name="pointer">The <see cref="StoragePointer"/> to free</param>
		/// <param name="disposeManaged">dispose managed resources or not</param>
		/// <returns>If <paramref name="pointer"/> is not supported or <paramref name="pointer"/> is not valid, return false; otherwise, return true.</returns>
		public abstract bool Free(StoragePointer pointer, bool disposeManaged = true);

		/// <summary>
		/// Fill the <paramref name="storage"/> by same <paramref name="value"/>, byte by byte.
		/// </summary>
		/// <param name="storage">The pointer to be filled</param>
		/// <param name="value">The value to set as a <see cref="byte"/></param>
		/// <exception cref="NotSupportedException">if <paramref name="storage"/> is not supported</exception>
		public abstract void SetMemoryValue(StoragePointer storage, byte value);

		/// <summary>
		/// Fill the <paramref name="storage"/>'s each value by same <paramref name="value"/>.
		/// </summary>
		/// <typeparam name="T">any unmanaged struct</typeparam>
		/// <param name="storage">The pointer to be filled</param>
		/// <param name="value">The value to set as a <typeparamref name="T"/></param>
		/// <exception cref="NotSupportedException">if <paramref name="storage"/> is not supported</exception>
		public abstract void SetMemoryValue<T>(StoragePointer storage, T value) where T : unmanaged;

		/// <summary>
		/// Copy memory from <paramref name="source"/> to <paramref name="dest"/>.
		/// </summary>
		/// <param name="source">The source pointer to copy from</param>
		/// <param name="dest">The destination pointer to copy into</param>
		/// <remarks>The one with less length in <paramref name="source"/> and <paramref name="dest"/> is used as the actual copy length in bytes</remarks>
		/// <exception cref="NotSupportedException">if <paramref name="source"/> or <paramref name="dest"/> is not supported</exception>
		public abstract void MemoryCopy(StoragePointer source, StoragePointer dest);

		/// <summary>
		/// Copies 2D data from <paramref name="source"/> to <paramref name="dest"/>.
		/// </summary>
		/// <param name="source">The source pointer</param>
		/// <param name="sourceLD">The source array actual height (actual leading dimension) in bytes</param>
		/// <param name="dest">The destination pointer</param>
		/// <param name="destLD">The destination array actual height (actual leading dimension) in bytes</param>
		/// <param name="height">The height to copy in bytes</param>
		/// <param name="width">The width to copy in the real type</param>
		/// <remarks>The lengths of <paramref name="source"/> and <paramref name="dest"/> are ignored</remarks>
		/// <exception cref="NotSupportedException">if <paramref name="source"/> or <paramref name="dest"/> is not supported</exception>
		/// <exception cref="ArgumentException">If <paramref name="height"/> is larger than <paramref name="sourceLD"/> or <paramref name="destLD"/></exception>
		/// <exception cref="ArgumentOutOfRangeException">If any of the parameters is zero</exception>
		public abstract void MemoryCopy2D(StoragePointer source, ulong sourceLD, StoragePointer dest, ulong destLD, ulong height, ulong width);

		/// <summary>
		/// Copy out the first element in unmanaged pointer <paramref name="source"/> to a managed value of type <typeparamref name="T"/>
		/// </summary>
		/// <typeparam name="T">any unmanaged struct</typeparam>
		/// <param name="source">The source <see cref="StoragePointer"/> to copy from</param>
		/// <returns>The first element in <paramref name="source"/>.</returns>
		/// <exception cref="NotSupportedException">if <paramref name="source"/> is not supported</exception>
		public abstract T ToManaged<T>(StoragePointer source) where T : unmanaged;

		/// <summary>
		/// Overwrite the first element in unmanaged pointer <paramref name="dest"/> by a managed <paramref name="value"/> of type <typeparamref name="T"/>
		/// </summary>
		/// <typeparam name="T">any unmanaged struct</typeparam>
		/// <param name="dest">The destination <see cref="StoragePointer"/> to copy to</param>
		/// <param name="value">The value of type <typeparamref name="T"/> to copy from</param>
		/// <exception cref="NotSupportedException">if <paramref name="dest"/> is not supported</exception>
		public abstract void FromManaged<T>(StoragePointer dest, T value) where T : unmanaged;

		/// <summary>
		/// Copy out the first few elements in unmanaged pointer <paramref name="source"/> to a managed array of type <typeparamref name="T"/>
		/// </summary>
		/// <typeparam name="T">any unmanaged struct</typeparam>
		/// <param name="source">The source <see cref="StoragePointer"/> to copy from</param>
		/// <param name="dest">The managed array of type <typeparamref name="T"/> to copy to</param>
		/// <exception cref="NotSupportedException">if <paramref name="source"/> is not supported</exception>
		public abstract void ToManaged<T>(StoragePointer source, T[] dest) where T : unmanaged;

		/// <summary>
		/// Overwrite the first few elements in unmanaged pointer <paramref name="dest"/> by the <paramref name="values"/> of a managed array of type <typeparamref name="T"/>
		/// </summary>
		/// <typeparam name="T">any unmanaged struct</typeparam>
		/// <param name="dest">The destination <see cref="StoragePointer"/> to copy to</param>
		/// <param name="values">The managed array of type <typeparamref name="T"/> to copy from</param>
		/// <exception cref="NotSupportedException">if <paramref name="dest"/> is not supported</exception>
		public abstract void FromManaged<T>(StoragePointer dest, T[] values) where T : unmanaged;

		// Ignore Spelling: sizeof
		/// <summary>
		/// Copy out the elements in unmanaged pointer <paramref name="source"/> as a 2D matrix to a managed array of type <typeparamref name="T"/> (viewed as a 1D array).
		/// </summary>
		/// <typeparam name="T">any unmanaged struct</typeparam>
		/// <param name="source">The source <see cref="StoragePointer"/> to copy from</param>
		/// <param name="leadDim">The actual height (actual leading dimension) in bytes of <paramref name="source"/></param>
		/// <param name="height">The height to copy in bytes</param>
		/// <param name="width">The width to copy in <typeparamref name="T"/> rather than bytes</param>
		/// <param name="dest">The managed array of type <typeparamref name="T"/> to copy to, must has <c><see cref="Array.Length">Length</see> ¡Ý <paramref name="height"/> / sizeof(T) * <paramref name="width"/></c></param>
		/// <exception cref="NotSupportedException">if <paramref name="source"/> is not supported</exception>
		public abstract void ToManaged2D<T>(StoragePointer source, ulong leadDim, ulong height, ulong width, T[] dest) where T : unmanaged;

		/// <summary>
		/// Overwrite some of the elements in unmanaged pointer <paramref name="dest"/> as a 2D matrix by  a managed array of type <typeparamref name="T"/> (viewed as a 1D array).
		/// </summary>
		/// <typeparam name="T">any unmanaged struct</typeparam>
		/// <param name="dest">The destination <see cref="StoragePointer"/> to copy to</param>
		/// <param name="leadDim">The actual height (actual leading dimension) in bytes of <paramref name="dest"/></param>
		/// <param name="height">The height to copy in bytes</param>
		/// <param name="width">The width to copy in <typeparamref name="T"/> rather than bytes</param>
		/// <param name="values">The managed array of type <typeparamref name="T"/> to copy from, must has <c><see cref="Array.Length">Length</see> ¡Ý <paramref name="height"/> / sizeof(T) * <paramref name="width"/></c></param>
		/// <exception cref="NotSupportedException">if <paramref name="dest"/> is not supported</exception>
		public abstract void FromManaged2D<T>(StoragePointer dest, ulong leadDim, ulong height, ulong width, T[] values) where T : unmanaged;
		#endregion

		#region high-level storage operations
		/// <summary>
		/// Fill the <paramref name="storage"/> byte by byte to the same <paramref name="value"/>.
		/// </summary>
		/// <param name="storage">The <see cref="IStorage"/> to be filled</param>
		/// <param name="value">The value to fill</param>
		/// <exception cref="NotSupportedException">If <paramref name="storage"/> is not supported</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="storage"/> is null or empty</exception>
		public virtual void SetMemoryValue(IStorage storage, byte value)
		{
			if (storage is null || storage.Count == 0)
				throw new ArgumentNullException(nameof(storage));
			for (int i = 0; i < storage.Count; i++)
			{
				this.SetMemoryValue(storage[i], value);
			}
		}

		/// <summary>
		/// Fill the <paramref name="storage"/>'s each value by same <paramref name="value"/>.
		/// </summary>
		/// <typeparam name="T">any unmanaged struct</typeparam>
		/// <param name="storage">The <see cref="IStorage"/> to be filled</param>
		/// <param name="value">The value to fill</param>
		/// <exception cref="NotSupportedException">If <paramref name="storage"/> is not supported</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="storage"/> is null or empty</exception>
		public virtual void SetMemoryValue<T>(IStorage storage, T value) where T : unmanaged
		{
			if (storage is null || storage.Count == 0)
				throw new ArgumentNullException(nameof(storage));
			for (int i = 0; i < storage.Count; i++)
			{
				this.SetMemoryValue(storage[i], value);
			}
		}

		/// <summary>
		/// Copy memory from <paramref name="source"/> to <paramref name="dest"/>.
		/// </summary>
		/// <param name="source">The source <see cref="IServiceProvider"/> to copy from</param>
		/// <param name="dest">The <see cref="IServiceProvider"/> pointer to copy into</param>
		/// <remarks>The one with less length in <paramref name="source"/> and <paramref name="dest"/> is used as the actual copy length in bytes</remarks>
		/// <exception cref="NotSupportedException">If <paramref name="source"/> or <paramref name="dest"/> is not supported</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="dest"/> is null or empty</exception>
		public virtual void MemoryCopy(IStorage source, IStorage dest)
		{
			if (source is null || source.Count == 0)
				throw new ArgumentNullException(nameof(source));
			if (dest is null || dest.Count == 0)
				throw new ArgumentNullException(nameof(dest));

			if (source.Count == 1 && dest.Count == 1)
			{
				this.MemoryCopy(source[0], dest[0]);
				return;
			}
			StoragePointer src = source[0], dst = dest[0];
			bool incSrc = false, incDst = false;
			int i = 0, j = 0;
			while (i < source.Count && j < dest.Count)
			{
				if (incSrc)
					src = source[i];
				if (incDst)
					dst = dest[j];
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
		/// Copies 2D data from <paramref name="source"/> to <paramref name="dest"/>.
		/// </summary>
		/// <param name="source">The source <see cref="IStorage"/></param>
		/// <param name="sourceLD">The source array actual height (actual leading dimension) in bytes</param>
		/// <param name="dest">The destination <see cref="IStorage"/></param>
		/// <param name="destLD">The destination array actual height (actual leading dimension) in bytes</param>
		/// <param name="height">The height to copy in bytes</param>
		/// <param name="width">The width to copy in the real type</param>
		/// <remarks>The lengths of <paramref name="source"/> and <paramref name="dest"/> are <b>not</b> ignored</remarks>
		/// <exception cref="NotSupportedException">If <paramref name="source"/> or <paramref name="dest"/> is not supported</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="dest"/> is null or empty</exception>
		/// <exception cref="ArgumentException">If <paramref name="height"/> is larger than <paramref name="sourceLD"/> or <paramref name="destLD"/>, or <paramref name="sourceLD"/> * <paramref name="width"/> &gt; <paramref name="source"/>.<see cref="IStorage.LengthInBytes">Length</see>, or <paramref name="destLD"/> * <paramref name="width"/> &gt; <paramref name="dest"/>.<see cref="IStorage.LengthInBytes">Length</see></exception>
		/// <exception cref="ArgumentOutOfRangeException">If any of the parameters is zero</exception>
		public virtual void MemoryCopy2D(IStorage source, ulong sourceLD, IStorage dest, ulong destLD, ulong height, ulong width)
		{
			if (source is null || source.Count == 0)
				throw new ArgumentNullException(nameof(source));
			if (dest is null || dest.Count == 0)
				throw new ArgumentNullException(nameof(dest));
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
			if (destLD * width > dest.LengthInBytes)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(dest));

			if (source.Count == 1 && dest.Count == 1)
			{
				this.MemoryCopy2D(source[0], sourceLD, dest[0], destLD, height, width);
				return;
			}
			ulong copiedLength = 0;
			long srcLD = (long)sourceLD, dstLD = (long)destLD;
			while (copiedLength < sourceLD * width && copiedLength < destLD * width)
			{
				this.MemoryCopy(source, dest);
			}
		}

		#endregion
	}

	/// <summary>
	/// The interface for a readable and writable URI wrapper
	/// </summary>
	public interface IUriWrapper : IAsyncDisposable, IDisposable
	{
		#region properties
		/// <summary>
		/// Get the original <see cref="Uri"/> of this wrapper
		/// </summary>
		Uri OriginalUri { get; }

		/// <summary>
		/// Get the current length of this wrapper
		/// </summary>
		ulong Length { get; }
		#endregion

		#region synchronous methods
		/// <summary>
		/// Reads the bytes from the current wrapper and writes them to another wrapper.
		/// </summary>
		/// <param name="wrapper">another <see cref="IUriWrapper"/> of same type</param>
		/// <param name="offset">the copy start offset in bytes</param>
		/// <param name="length">the copy length in bytes</param>
		/// <exception cref="ArgumentException">if <paramref name="wrapper"/> does not have same type as this one</exception>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="offset"/> or <paramref name="length"/> exceeds the boundary of this wrapper</exception>
		void CopyTo(IUriWrapper wrapper, ulong offset, ulong length);

		/// <summary>
		/// Reads the bytes from the current wrapper and writes them to a memory pointer.
		/// </summary>
		/// <param name="pointer">the supported <see cref="StoragePointer"/> to be overwritten</param>
		/// <param name="offset">the read start offset of this wrapper in bytes</param>
		/// <exception cref="NotSupportedException">if <paramref name="pointer"/> is not supported</exception>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="offset"/> or <paramref name="pointer"/>.<see cref="StoragePointer.LengthInBytes">Length</see> exceed the boundary of this wrapper</exception>
		void Read(StoragePointer pointer, ulong offset);

		/// <summary>
		/// Reads the bytes a memory pointer and write them to this wrapper. Some bytes will be appended (and <see cref="Length"/> will grow) if necessary.
		/// </summary>
		/// <param name="pointer">the supported <see cref="StoragePointer"/> to be overwritten</param>
		/// <param name="offset">the write start offset of this wrapper in bytes</param>
		/// <exception cref="NotSupportedException">if <paramref name="pointer"/> is not supported</exception>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="offset"/> exceeds the boundary of this wrapper</exception>
		void Write(StoragePointer pointer, ulong offset);

		/// <summary>
		/// Resize this wrapper to fit in the <paramref name="newLength"/>. If <paramref name="newLength"/> is smaller than current one,a truncation of the trailer part will be performed.
		/// </summary>
		/// <param name="newLength">the new <see cref="Length"/> of this wrapper</param>
		void Resize(ulong newLength);
		#endregion

		#region asynchronous methods
		/// <summary>
		/// Reads the bytes from the current wrapper and writes them to another wrapper.
		/// </summary>
		/// <param name="wrapper">another <see cref="IUriWrapper"/> of same type</param>
		/// <param name="offset">the copy start offset in bytes</param>
		/// <param name="length">the copy length in bytes</param>
		/// <exception cref="ArgumentException">if <paramref name="wrapper"/> does not have same type as this one</exception>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="offset"/> or <paramref name="length"/> exceeds the boundary of this wrapper</exception>
		ValueTask CopyToAsync(IUriWrapper wrapper, ulong offset, ulong length);

		/// <summary>
		/// Reads the bytes from the current wrapper and writes them to a memory pointer.
		/// </summary>
		/// <param name="pointer">the supported <see cref="StoragePointer"/> to be overwritten</param>
		/// <param name="offset">the read start offset of this wrapper in bytes</param>
		/// <exception cref="NotSupportedException">if <paramref name="pointer"/> is not supported</exception>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="offset"/> or <paramref name="pointer"/>.<see cref="StoragePointer.LengthInBytes">Length</see> exceed the boundary of this wrapper</exception>
		ValueTask ReadAsync(StoragePointer pointer, ulong offset);

		/// <summary>
		/// Reads the bytes a memory pointer and write them to this wrapper. Some bytes will be appended (and <see cref="Length"/> will grow) if necessary.
		/// </summary>
		/// <param name="pointer">the supported <see cref="StoragePointer"/> to be overwritten</param>
		/// <param name="offset">the write start offset of this wrapper in bytes</param>
		/// <exception cref="NotSupportedException">if <paramref name="pointer"/> is not supported</exception>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="offset"/> exceeds the boundary of this wrapper</exception>
		ValueTask WriteAsync(StoragePointer pointer, ulong offset);

		/// <summary>
		/// Resize this wrapper to fit in the <paramref name="newLength"/>. If <paramref name="newLength"/> is smaller than current one,a truncation of the trailer part will be performed.
		/// </summary>
		/// <param name="newLength">the new <see cref="Length"/> of this wrapper</param>
		ValueTask ResizeAsync(ulong newLength);
		#endregion
	}
}