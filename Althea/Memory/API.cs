using System;
using System.Threading.Tasks;
using System.Collections.Generic;


namespace Althea.Memory
{
	/// <summary>
	/// The abstract class for runtime memory API routines.
	/// </summary>
	public abstract partial class AbstractMemoryApi : AbstractRuntimeApi
	{
		#region static methods for dispatching

		#endregion
	}

	/// <summary>
	/// The abstract class for runtime memory API routines 
	/// </summary>
	public abstract partial class AbstractMemoryApi : AbstractRuntimeApi
	{
		#region support information
		/// <summary>
		/// Get list of the supported memory locations for all ternary operations. Since <see cref="AbstractMemoryApi"/> has no definition of ternary operations, this returns null.
		/// </summary>
		public override IReadOnlyList<StorageLocation> SupportedTernaryLocations => null;

		// Ignore Spelling: N-ary
		/// <summary>
		/// Get list of the supported memory locations for all N-ary operations. This method will only be invoked internally with <paramref name="N"/> &gt; 3.
		/// </summary>
		/// <param name="N">the number of operands</param>
		/// <returns>The list of the supported memory locations for all N-ary operations.</returns>
		protected override IReadOnlyList<StorageLocation> Direct_SupportedNaryLocations(int N) => null;

		/// <summary>
		/// Get the supported URI direct transfer dictionary. Each value indicates that this <see cref="AbstractMemoryApi"/> supports the <b>direct</b> data transfer between the given <see cref="StorageLocation"/> (combination of flags) and the given <see cref="UriScheme"/>.
		/// </summary>
		public abstract IReadOnlyDictionary<UriScheme, StorageLocation> SupportedUriTransfers { get; }

		/// <summary>
		/// Check if the direct data transfer of given <paramref name="location"/> and given <paramref name="uriScheme"/> is supported by this <see cref="AbstractMemoryApi"/> or not.
		/// </summary>
		/// <param name="location">the given <see cref="StorageLocation"/></param>
		/// <param name="uriScheme">the given <see cref="UriScheme"/></param>
		/// <returns>Whether the direct data transfer of <paramref name="location"/> and <paramref name="uriScheme"/> is supported by this <see cref="AbstractMemoryApi"/>.</returns>
		public virtual bool IsSupportedTransfer(StorageLocation location, UriScheme uriScheme) => location != StorageLocation.Uri && (this.SupportedUriTransfers[uriScheme] & location) == location;
		#endregion

		#region properties
		/// <summary>
		/// Get the underlying driver's version of a supported <see cref="StorageLocation"/>.
		/// </summary>
		/// <param name="location">the given supported <see cref="StorageLocation"/></param>
		/// <returns>The underlying driver's version of given <paramref name="location"/></returns>
		public abstract (int major, int minor) DriverVersion(StorageLocation location);

		/// <summary>
		/// Get the maximum number of devices available of a supported <see cref="StorageLocation"/>.
		/// </summary>
		/// <param name="location">the given supported <see cref="StorageLocation"/></param>
		/// <returns>The maximum number of devices available of given <paramref name="location"/></returns>
		public abstract int MaxDeviceNumber(StorageLocation location);

		/// <summary>
		/// Get the available and total memory in bytes for device indicated by a supported <see cref="MemoryLocation"/>.
		/// </summary>
		/// <param name="location">the given supported <see cref="MemoryLocation"/></param>
		/// <returns>The available and total memory in bytes of device of given <paramref name="location"/></returns>
		public abstract (ulong free, ulong total) FreeAndTotalMemory(MemoryLocation location);
		#endregion

		#region memory related
		/// <summary>
		/// Allocate a storage at given <paramref name="location"/> with given <paramref name="length"/>
		/// </summary>
		/// <param name="location">the <see cref="MemoryLocation"/> to allocate on</param>
		/// <param name="length">length to allocate in bytes</param>
		/// <returns>The allocated pointer as a <see cref="IntPtr"/></returns>
		/// <exception cref="NotSupportedException">if <paramref name="location"/> is not supported</exception>
		public abstract IntPtr Allocate(MemoryLocation location, ulong length);

		/// <summary>
		/// Allocate a storage at given <paramref name="location"/> with given <paramref name="length"/>
		/// </summary>
		/// <param name="location">the <see cref="MemoryLocation"/> to allocate on</param>
		/// <param name="length">length to allocate in bytes</param>
		/// <returns>The allocated pointer as a <see cref="IntPtr"/></returns>
		/// <exception cref="NotSupportedException">if <paramref name="location"/> is not supported</exception>
		public delegate IntPtr DelegateAllocate(MemoryLocation location, ulong length);

		/// <summary>
		/// Allocate a storage at given <paramref name="location"/> with given <paramref name="length"/>
		/// </summary>
		/// <typeparam name="T">any unmanaged struct</typeparam>
		/// <param name="location">the <see cref="MemoryLocation"/> to allocate on</param>
		/// <param name="length">length to allocate in <typeparamref name="T"/> rather than bytes</param>
		/// <returns>The allocated pointer as a <see cref="IntPtr"/></returns>
		/// <exception cref="NotSupportedException">if <paramref name="location"/> is not supported</exception>
		public IntPtr Allocate<T>(MemoryLocation location, ulong length) where T : unmanaged, IEquatable<T>
			=> this.Allocate(location, length * (ulong)Storage<T>.SizeOfT);

		/// <summary>
		/// Free a storage indicated by a given <paramref name="ptr"/> on a given <paramref name="location"/>
		/// </summary>
		/// <param name="location">the <see cref="MemoryLocation"/> to free on</param>
		/// <param name="ptr">the pointer as a <see cref="IntPtr"/> to free</param>
		/// <returns>If <paramref name="location"/> is not supported or <paramref name="ptr"/> is not valid, return false; otherwise, return true.</returns>
		public abstract bool Free(MemoryLocation location, IntPtr ptr);

		/// <summary>
		/// Free a storage indicated by a given <paramref name="ptr"/> on a given <paramref name="location"/>
		/// </summary>
		/// <param name="location">the <see cref="MemoryLocation"/> to free on</param>
		/// <param name="ptr">the pointer as a <see cref="IntPtr"/> to free</param>
		/// <returns>If <paramref name="location"/> is not supported or <paramref name="ptr"/> is not valid, return false; otherwise, return true.</returns>
		public delegate bool DelegateFree(MemoryLocation location, IntPtr ptr);

		/// <summary>
		/// Free a storage indicated by a given <paramref name="pointer"/>
		/// </summary>
		/// <param name="pointer">the <see cref="StoragePointer"/> to free</param>
		/// <returns>If <paramref name="pointer"/> is not supported or <paramref name="pointer"/> is not valid, return false; otherwise, return true.</returns>
		public bool Free(StoragePointer pointer) => pointer.LengthInBytes != 0 && pointer.Pointer != default && this.Free(pointer.Location, pointer.Pointer);

		/// <summary>
		/// Fill the <paramref name="storage"/> by same <paramref name="value"/>, byte by byte.
		/// </summary>
		/// <param name="storage">pointer to be filled</param>
		/// <param name="value">value to set</param>
		/// <exception cref="NotSupportedException">if <paramref name="storage"/> is not supported</exception>
		public abstract void SetMemoryValue(StoragePointer storage, byte value);

		/// <summary>
		/// Fill the <paramref name="storage"/> by same <paramref name="value"/>, byte by byte.
		/// </summary>
		/// <param name="storage">pointer to be filled</param>
		/// <param name="value">value to set</param>
		/// <exception cref="NotSupportedException">if <paramref name="storage"/> is not supported</exception>
		public delegate void DelegateSetMemoryValue(StoragePointer storage, byte value);

		/// <summary>
		/// Fill the <paramref name="storage"/>'s each value by same <paramref name="value"/>.
		/// </summary>
		/// <typeparam name="T">any unmanaged struct</typeparam>
		/// <param name="storage">pointer to be filled</param>
		/// <param name="value">value to set</param>
		/// <exception cref="NotSupportedException">if <paramref name="storage"/> is not supported</exception>
		public abstract void SetMemoryValue<T>(StoragePointer storage, T value) where T : unmanaged, IEquatable<T>;

		/// <summary>
		/// Fill the <paramref name="storage"/>'s each value by same <paramref name="value"/>.
		/// </summary>
		/// <typeparam name="T">any unmanaged struct</typeparam>
		/// <param name="storage">pointer to be filled</param>
		/// <param name="value">value to set</param>
		/// <exception cref="NotSupportedException">if <paramref name="storage"/> is not supported</exception>
		public delegate void DelegateSetMemoryValue<T>(StoragePointer storage, T value) where T : unmanaged, IEquatable<T>;

		/// <summary>
		/// Copy memory from <paramref name="source"/> to <paramref name="dest"/>.
		/// </summary>
		/// <param name="source">source pointer to copy from</param>
		/// <param name="dest">destination pointer to copy into</param>
		/// <remarks>The one with less length in <paramref name="source"/> and <paramref name="dest"/> is used as the actual copy length in bytes</remarks>
		/// <exception cref="NotSupportedException">if <paramref name="source"/> or <paramref name="dest"/> is not supported</exception>
		public abstract void MemoryCopy(StoragePointer source, StoragePointer dest);

		/// <summary>
		/// Copy memory from <paramref name="source"/> to <paramref name="dest"/>.
		/// </summary>
		/// <param name="source">source pointer to copy from</param>
		/// <param name="dest">destination pointer to copy into</param>
		/// <remarks>The one with less length in <paramref name="source"/> and <paramref name="dest"/> is used as the actual copy length in bytes</remarks>
		/// <exception cref="NotSupportedException">if <paramref name="source"/> or <paramref name="dest"/> is not supported</exception>
		public delegate void DelegateMemoryCopy(StoragePointer source, StoragePointer dest);

		/// <summary>
		/// Copies 2D data from <paramref name="source"/> to <paramref name="dest"/>.
		/// </summary>
		/// <param name="source">the source pointer</param>
		/// <param name="sourceLD">source array actual height (actual leading dimension) in bytes</param>
		/// <param name="dest">the destination pointer</param>
		/// <param name="destLD">destination array actual height (actual leading dimension) in bytes</param>
		/// <param name="height">height to copy in bytes</param>
		/// <param name="width">width to copy in bytes</param>
		/// <remarks>The lengths of <paramref name="source"/> and <paramref name="dest"/> are ignored</remarks>
		/// <exception cref="NotSupportedException">if <paramref name="source"/> or <paramref name="dest"/> is not supported</exception>
		public abstract void MemoryCopy2D(StoragePointer source, ulong sourceLD, StoragePointer dest, ulong destLD, ulong height, ulong width);

		/// <summary>
		/// Copies 2D data from <paramref name="source"/> to <paramref name="dest"/>.
		/// </summary>
		/// <param name="source">the source pointer</param>
		/// <param name="sourceLD">source array actual height (actual leading dimension) in bytes</param>
		/// <param name="dest">the destination pointer</param>
		/// <param name="destLD">destination array actual height (actual leading dimension) in bytes</param>
		/// <param name="height">height to copy in bytes</param>
		/// <param name="width">width to copy in bytes</param>
		/// <remarks>The lengths of <paramref name="source"/> and <paramref name="dest"/> are ignored</remarks>
		/// <exception cref="NotSupportedException">if <paramref name="source"/> or <paramref name="dest"/> is not supported</exception>
		public delegate void DelegateMemoryCopy2D(StoragePointer source, ulong sourceLD, StoragePointer dest, ulong destLD, ulong height, ulong width);
		#endregion

		#region URI related
		/// <summary>
		/// Create a <see cref="IUriWrapper"/> of given <paramref name="uri"/> which supports data transfer defined in <see cref="SupportedUriTransfers"/>
		/// </summary>
		/// <param name="uri">the given <see cref="Uri"/></param>
		/// <returns>A instance of <see cref="IUriWrapper"/> of given <paramref name="uri"/></returns>
		/// <exception cref="NotSupportedException">if <paramref name="uri"/> is not supported</exception>
		public abstract IUriWrapper CreateUriStream(Uri uri);
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