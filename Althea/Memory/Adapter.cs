using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Diagnostics;

using Althea.Linq;
using Althea.Helpers;


namespace Althea.Memory
{
	/// <summary>
	/// The runtime memory routine interface
	/// </summary>
	public interface IMemory : IDisposable
	{
		#region support information
		/// <summary>
		/// Get the supported memory locations for all unitary operations such as <see cref="SetMemoryValue"/>. Each flag in this value indicates a support of a certain location.
		/// </summary>
		StorageLocation SupportedUnitaryLocations { get; }

		/// <summary>
		/// Check if the given <paramref name="location"/> is supported by unitary operations of this <see cref="IMemory"/> or not.
		/// </summary>
		/// <param name="location">the given <see cref="StorageLocation"/> (can be combination of flags)</param>
		/// <returns>Whether <paramref name="location"/> is supported by this <see cref="IMemory"/>.</returns>
		bool IsSupportedUnitary(StorageLocation location) => location != StorageLocation.Uri && (location & this.SupportedUnitaryLocations) == location;

		/// <summary>
		/// Get list of the supported memory locations for all binary operations such as <see cref="MemoryCopy"/>. Each value must has exactly one or two flags to indicate a supported pair of certain locations.
		/// </summary>
		IReadOnlyList<StorageLocation> SupportedBinaryLocations { get; }

		/// <summary>
		/// Check if the given <paramref name="location"/> is supported by binary operations of this <see cref="IMemory"/> or not.
		/// </summary>
		/// <param name="location">the given <see cref="StorageLocation"/> (must has exactly one or two flags)</param>
		/// <returns>Whether binary operations between <paramref name="location"/> are supported by this <see cref="IMemory"/>.</returns>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="location"/> does not exactly one or two flags</exception>
		bool IsSupportedBinary(StorageLocation location) => location.NumberOfFlags() <= 2 && this.SupportedBinaryLocations.Contains(location);

		/// <summary>
		/// Check if the given <see cref="StorageLocation"/>s are supported by binary operations of this <see cref="IMemory"/> or not.
		/// </summary>
		/// <param name="location1">the first given <see cref="StorageLocation"/> (must be a flag)</param>
		/// <param name="location2">the second given <see cref="StorageLocation"/> (must be a flag)</param>
		/// <returns>Whether binary operations between <paramref name="location1"/> and <paramref name="location2"/> are supported by this <see cref="IMemory"/>.</returns>
		bool IsSupportedBinary(StorageLocation location1, StorageLocation location2)
		{
			if (!location1.IsFlag() || !location2.IsFlag())
			{
				return false;
			}	
			if (location1 == location2)
			{
				return this.SupportedBinaryLocations.Contains(location1);
			}
			else
			{
				return this.SupportedBinaryLocations.Contains(location1 | location2);
			}
		}

		/// <summary>
		/// Get the supported URI direct transfer dictionary. Each value indicates that this <see cref="IMemory"/> supports the <b>direct</b> data transfer between the given <see cref="StorageLocation"/> (combination of flags) and the given <see cref="UriScheme"/>.
		/// </summary>
		IReadOnlyDictionary<UriScheme, StorageLocation> SupportedUriTransfers { get; }

		/// <summary>
		/// Check if the direct data transfer of given <paramref name="location"/> and given <paramref name="uriScheme"/> is supported by this <see cref="IMemory"/> or not.
		/// </summary>
		/// <param name="location">the given <see cref="StorageLocation"/></param>
		/// <param name="uriScheme">the given <see cref="UriScheme"/></param>
		/// <returns>Whether the direct data transfer of <paramref name="location"/> and <paramref name="uriScheme"/> is supported by this <see cref="IMemory"/>.</returns>
		bool IsSupportedTransfer(StorageLocation location, UriScheme uriScheme) => location != StorageLocation.Uri && (this.SupportedUriTransfers[uriScheme] & location) == location;
		#endregion

		#region properties
		/// <summary>
		/// Get the underlying driver's version of a supported <see cref="StorageLocation"/>.
		/// </summary>
		/// <param name="location">the given supported <see cref="StorageLocation"/></param>
		/// <returns>The underlying driver's version of given <paramref name="location"/></returns>
		(int major, int minor) DriverVersion(StorageLocation location);

		/// <summary>
		/// Get the maximum number of devices available of a supported <see cref="StorageLocation"/>.
		/// </summary>
		/// <param name="location">the given supported <see cref="StorageLocation"/></param>
		/// <returns>The maximum number of devices available of given <paramref name="location"/></returns>
		int MaxDeviceNumber(StorageLocation location);

		/// <summary>
		/// Get the available and total memory in bytes for device indicated by a supported <see cref="MemoryLocation"/>.
		/// </summary>
		/// <param name="location">the given supported <see cref="MemoryLocation"/></param>
		/// <returns>The available and total memory in bytes of device of given <paramref name="location"/></returns>
		(ulong free, ulong total) FreeAndTotalMemory(MemoryLocation location);
		#endregion

		#region memory related
		/// <summary>
		/// Allocate a storage at given <paramref name="location"/> with given <paramref name="length"/>
		/// </summary>
		/// <param name="location">the <see cref="MemoryLocation"/> to allocate on</param>
		/// <param name="length">length to allocate in bytes</param>
		/// <returns>The allocated pointer as a <see cref="IntPtr"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="location"/> is not supported</exception>
		IntPtr Allocate(MemoryLocation location, ulong length);

		/// <summary>
		/// Allocate a storage at given <paramref name="location"/> with given <paramref name="length"/>
		/// </summary>
		/// <param name="location">the <see cref="MemoryLocation"/> to allocate on</param>
		/// <param name="length">length to allocate in bytes</param>
		/// <returns>The allocated pointer as a <see cref="IntPtr"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="location"/> is not supported</exception>
		public delegate IntPtr DelegateAllocate(MemoryLocation location, ulong length);

		/// <summary>
		/// Allocate a storage at given <paramref name="location"/> with given <paramref name="length"/>
		/// </summary>
		/// <typeparam name="T">any unmanaged struct</typeparam>
		/// <param name="location">the <see cref="MemoryLocation"/> to allocate on</param>
		/// <param name="length">length to allocate in <typeparamref name="T"/> rather than bytes</param>
		/// <returns>The allocated pointer as a <see cref="IntPtr"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="location"/> is not supported</exception>
		public IntPtr Allocate<T>(MemoryLocation location, ulong length) where T : unmanaged => this.Allocate(location, length * (ulong)Storage<T>.SizeOfT);

		/// <summary>
		/// Free a storage indicated by a given <paramref name="ptr"/> on a given <paramref name="location"/>
		/// </summary>
		/// <param name="location">the <see cref="MemoryLocation"/> to free on</param>
		/// <param name="ptr">the pointer as a <see cref="IntPtr"/> to free</param>
		/// <returns>If <paramref name="location"/> is not supported or <paramref name="ptr"/> is not valid, return false; otherwise, return true.</returns>
		bool Free(MemoryLocation location, IntPtr ptr);

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
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="storage"/> is not supported</exception>
		void SetMemoryValue(StoragePointer storage, byte value);

		/// <summary>
		/// Fill the <paramref name="storage"/> by same <paramref name="value"/>, byte by byte.
		/// </summary>
		/// <param name="storage">pointer to be filled</param>
		/// <param name="value">value to set</param>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="storage"/> is not supported</exception>
		public delegate void DelegateSetMemoryValue(StoragePointer storage, byte value);

		/// <summary>
		/// Fill the <paramref name="storage"/>'s each value by same <paramref name="value"/>.
		/// </summary>
		/// <typeparam name="T">any unmanaged struct</typeparam>
		/// <param name="storage">pointer to be filled</param>
		/// <param name="value">value to set</param>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="storage"/> is not supported</exception>
		void SetMemoryValue<T>(StoragePointer storage, T value) where T : unmanaged;

		/// <summary>
		/// Fill the <paramref name="storage"/>'s each value by same <paramref name="value"/>.
		/// </summary>
		/// <typeparam name="T">any unmanaged struct</typeparam>
		/// <param name="storage">pointer to be filled</param>
		/// <param name="value">value to set</param>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="storage"/> is not supported</exception>
		public delegate void DelegateSetMemoryValue<T>(StoragePointer storage, T value) where T : unmanaged;

		/// <summary>
		/// Copy memory from <paramref name="source"/> to <paramref name="dest"/>.
		/// </summary>
		/// <param name="source">source pointer to copy from</param>
		/// <param name="dest">destination pointer to copy into</param>
		/// <remarks>The one with less length in <paramref name="source"/> and <paramref name="dest"/> is used as the actual copy length in bytes</remarks>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="source"/> or <paramref name="dest"/> is not supported</exception>
		void MemoryCopy(StoragePointer source, StoragePointer dest);

		/// <summary>
		/// Copy memory from <paramref name="source"/> to <paramref name="dest"/>.
		/// </summary>
		/// <param name="source">source pointer to copy from</param>
		/// <param name="dest">destination pointer to copy into</param>
		/// <remarks>The one with less length in <paramref name="source"/> and <paramref name="dest"/> is used as the actual copy length in bytes</remarks>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="source"/> or <paramref name="dest"/> is not supported</exception>
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
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="source"/> or <paramref name="dest"/> is not supported</exception>
		void MemoryCopy2D(StoragePointer source, ulong sourceLD, StoragePointer dest, ulong destLD, ulong height, ulong width);

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
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="source"/> or <paramref name="dest"/> is not supported</exception>
		public delegate void DelegateMemoryCopy2D(StoragePointer source, ulong sourceLD, StoragePointer dest, ulong destLD, ulong height, ulong width);
		#endregion

		#region URI related

		#endregion
	}
}


namespace Althea.Memory.CSharp
{
	internal sealed class DefaultMemory : IMemory
	{
		#region basic
		public DefaultMemory()
		{
			// do nothing
		}

		public void Dispose()
		{
			// do nothing
		}
		#endregion

		#region support
		public StorageLocation SupportedUnitaryLocations => StorageLocation.CpuRam;

		public IReadOnlyList<StorageLocation> SupportedBinaryLocations { get; } = new[] { StorageLocation.CpuRam };

		public IReadOnlyDictionary<UriScheme, StorageLocation> SupportedUriTransfers { get; } = new Dictionary<UriScheme, StorageLocation>
		{
			[UriScheme.File] = StorageLocation.CpuRam,
			[UriScheme.FTP] = StorageLocation.CpuRam,
		};
		#endregion

		#region properties
		public (int major, int minor) DriverVersion(StorageLocation location) => default;

		// since this is not implemented yet (see https://github.com/dotnet/runtime/issues/22948), this is a manual implementation
		public (ulong free, ulong total) FreeAndTotalMemory(MemoryLocation location)
		{
			var memoryInfo = GC.GetGCMemoryInfo();
			ulong total = unchecked((ulong)memoryInfo.TotalAvailableMemoryBytes);
			ulong free = total - unchecked((ulong)Environment.WorkingSet);
			return (free, total);
		}

		public int MaxDeviceNumber(StorageLocation location) => 1;
		#endregion

		#region memory
		public IntPtr Allocate(MemoryLocation location, ulong length) => location.Location == StorageLocation.CpuRam ? Marshal.AllocHGlobal(checked((int)length)) : throw new ArgumentOutOfRangeException(nameof(location), Resource.NotSupportedLocation);

		public bool Free(MemoryLocation location, IntPtr ptr)
		{
			if (location.Location == StorageLocation.CpuRam)
			{
				Marshal.FreeHGlobal(ptr);
				return true;
			}
			return false;
		}

		public void SetMemoryValue(StoragePointer storage, byte value)
		{
			if (storage.Location.Location != StorageLocation.CpuRam)
				throw new ArgumentOutOfRangeException(nameof(storage));
			unsafe
			{
				Unsafe.InitBlock(storage.UnmangedPointer, value, checked((uint)storage.LengthInBytes));
			}
		}

		public void SetMemoryValue<T>(StoragePointer storage, T value) where T : unmanaged
		{
			if (storage.Location.Location != StorageLocation.CpuRam)
				throw new ArgumentOutOfRangeException(nameof(storage));
			storage.AsSpan<T>().Fill(value);
		}

		public void MemoryCopy(StoragePointer source, StoragePointer dest)
		{
			if (source.Location.Location != StorageLocation.CpuRam)
				throw new ArgumentOutOfRangeException(nameof(source));
			if (dest.Location.Location != StorageLocation.CpuRam)
				throw new ArgumentOutOfRangeException(nameof(dest));
			unsafe
			{
				Unsafe.CopyBlock(source.UnmangedPointer, dest.UnmangedPointer, checked((uint)source.LengthInBytes));
			}
		}

		public void MemoryCopy2D(StoragePointer source, ulong sourceLD, StoragePointer dest, ulong destLD, ulong height, ulong width)
		{
			if (source.Location.Location != StorageLocation.CpuRam)
				throw new ArgumentOutOfRangeException(nameof(source));
			if (dest.Location.Location != StorageLocation.CpuRam)
				throw new ArgumentOutOfRangeException(nameof(dest));

			if (sourceLD == destLD && sourceLD == height)
			{
				MemoryCopy(source.SetLength(height * width), dest.SetLength(height * width));
				return;
			}
			uint h = checked((uint)height);
			unsafe
			{
				byte* s = (byte*)source.UnmangedPointer;
				byte* end = s + sourceLD * width;
				byte* d = (byte*)dest.UnmangedPointer;
				for (; s < end; s += sourceLD, d += destLD)
				{
					Unsafe.CopyBlock(d, s, h);
				}
			}
		}
		#endregion
	}
}
