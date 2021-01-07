using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;

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
		bool IsSupportedBinary(StorageLocation location) => location.NumberOfFlags() <= 2 ? this.SupportedBinaryLocations.Contains(location) : throw new ArgumentOutOfRangeException(nameof(location));

		/// <summary>
		/// Check if the given <see cref="StorageLocation"/>s are supported by binary operations of this <see cref="IMemory"/> or not.
		/// </summary>
		/// <param name="location1">the first given <see cref="StorageLocation"/> (must be a flag)</param>
		/// <param name="location2">the second given <see cref="StorageLocation"/> (must be a flag)</param>
		/// <returns>Whether binary operations between <paramref name="location1"/> and <paramref name="location2"/> are supported by this <see cref="IMemory"/>.</returns>
		bool IsSupportedBinary(StorageLocation location1, StorageLocation location2)
		{
			if (!location1.IsFlag())
				throw new ArgumentOutOfRangeException(nameof(location1));
			if (!location2.IsFlag())
				throw new ArgumentOutOfRangeException(nameof(location2));
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
		(long free, long total) FreeAndTotalMemory(MemoryLocation location);
		#endregion

		#region methods
		/// <summary>
		/// Fill the <paramref name="storage"/> by same <paramref name="value"/>, byte by byte.
		/// </summary>
		/// <param name="storage">pointer to be filled</param>
		/// <param name="value">value to set</param>
		/// <param name="length">length in bytes</param>
		void SetMemoryValue(StoragePointer storage, ulong length, byte value);

		/// <summary>
		/// Fill the <paramref name="storage"/> by same <paramref name="value"/>, byte by byte.
		/// </summary>
		/// <param name="storage">pointer to be filled</param>
		/// <param name="value">value to set</param>
		/// <param name="length">length in bytes</param>
		public delegate void DelegateSetMemoryValue(StoragePointer storage, ulong length, byte value);

		/// <summary>
		/// Fill the <paramref name="storage"/>'s each value by same <paramref name="value"/>.
		/// </summary>
		/// <typeparam name="T">any unmanaged struct</typeparam>
		/// <param name="storage">pointer to be filled</param>
		/// <param name="value">value to set</param>
		/// <param name="length">length in <typeparamref name="T"/></param>
		void SetMemoryValue<T>(StoragePointer storage, ulong length, T value) where T : unmanaged;

		/// <summary>
		/// Fill the <paramref name="storage"/>'s each value by same <paramref name="value"/>.
		/// </summary>
		/// <typeparam name="T">any unmanaged struct</typeparam>
		/// <param name="storage">pointer to be filled</param>
		/// <param name="value">value to set</param>
		/// <param name="length">length in <typeparamref name="T"/></param>
		public delegate void DelegateSetMemoryValue<T>(StoragePointer storage, ulong length, T value) where T : unmanaged;

		/// <summary>
		/// Copy memory from <paramref name="source"/> to <paramref name="dest"/>.
		/// </summary>
		/// <param name="source">source pointer to copy from</param>
		/// <param name="dest">destination pointer to copy into</param>
		/// <param name="length">length to copy in bytes</param>
		void MemoryCopy(StoragePointer source, StoragePointer dest, ulong length);

		/// <summary>
		/// Copy memory from <paramref name="source"/> to <paramref name="dest"/>.
		/// </summary>
		/// <param name="source">source pointer to copy from</param>
		/// <param name="dest">destination pointer to copy into</param>
		/// <param name="length">length to copy</param>
		public delegate void DelegateMemoryCopy(StoragePointer source, StoragePointer dest, ulong length);

		/// <summary>
		/// Copies 2D data from <paramref name="source"/> to <paramref name="dest"/>.
		/// </summary>
		/// <param name="source">the source pointer</param>
		/// <param name="sourceLD">source array actual height (actual leading dimension) in bytes</param>
		/// <param name="dest">the destination pointer</param>
		/// <param name="destLD">destination array actual height (actual leading dimension) in bytes</param>
		/// <param name="height">height to copy in bytes</param>
		/// <param name="width">width to copy in bytes</param>
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
		public delegate void DelegateMemoryCopy2D(StoragePointer source, ulong sourceLD, StoragePointer dest, ulong destLD, ulong height, ulong width);
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
		public (int major, int minor) DriverVersion(StorageLocation location) => throw new NotImplementedException();
		public (long free, long total) FreeAndTotalMemory(MemoryLocation location) => throw new NotImplementedException();
		public int MaxDeviceNumber(StorageLocation location) => throw new NotImplementedException();
		#endregion

		#region other methods
		public void MemoryCopy(StoragePointer source, StoragePointer dest, ulong length) => throw new NotImplementedException();
		public void MemoryCopy2D(StoragePointer source, ulong sourceLD, StoragePointer dest, ulong destLD, ulong height, ulong width) => throw new NotImplementedException();
		public void SetMemoryValue(StoragePointer storage, ulong length, byte value) => throw new NotImplementedException();
		public void SetMemoryValue<T>(StoragePointer storage, ulong length, T value) where T : unmanaged => throw new NotImplementedException();
		#endregion
	}
}
