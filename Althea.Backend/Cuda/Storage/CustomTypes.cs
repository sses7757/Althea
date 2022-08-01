using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Althea.Backend.Cuda.Storage;


namespace Althea.Backend.Cuda.Storage
{
	#region copy
	/// <summary>
	/// Memory copy enum
	/// </summary>
	internal enum MemoryCopyKind
	{
		/// <summary>
		/// host to host
		/// </summary>
		HostToHost = 0,
		/// <summary>
		/// host to device
		/// </summary>
		HostToDevice = 1,
		/// <summary>
		/// device to host
		/// </summary>
		DeviceToHost = 2,
		/// <summary>
		/// device to device
		/// </summary>
		DeviceToDevice = 3,
		/// <summary>
		/// Direction of the transfer is inferred from the pointer values. Requires unified virtual addressing
		/// </summary>
		Default = 4
	}
	#endregion

	#region CUDA file error
	// Ignore Spelling: nvidia-fs
	/// <summary>
	/// The CUDA file operation error types used by the CUDA GPUDirect® Storage (GDS) APIs
	/// </summary>
	public enum CudaFileOpError
	{
		/// <summary>
		/// The requested cuFile operation is successful.
		/// </summary>
		Success = 0,
		/// <summary>
		/// The nvidia-fs driver is not loaded.
		/// </summary>
		DriverNotInitialized = 5001,
		/// <summary>
		/// An invalid property.
		/// </summary>
		DriverInvalidProps = 5002,
		/// <summary>
		/// A property range error.
		/// </summary>
		DriverUnsupportedLimit = 5003,
		/// <summary>
		/// An nvidia-fs driver version mismatch.
		/// </summary>
		DriverVersionMismatch = 5004,
		/// <summary>
		/// An nvidia-fs driver version read error.
		/// </summary>
		DriverVersionReadError = 5005,
		/// <summary>
		/// Driver shutdown in progress.
		/// </summary>
		DriverClosing = 5006,
		/// <summary>
		/// GDS is not supported on the current platform.
		/// </summary>
		PlatformNotSupported = 5007,
		/// <summary>
		/// GDS is not supported on the current file.
		/// </summary>
		IONotSupported = 5008,
		/// <summary>
		/// GDS is not supported on the current GPU.
		/// </summary>
		DeviceNotSupported = 5009,
		/// <summary>
		/// An nvidia-fs driver ioctl error.
		/// </summary>
		NvfsDriverError = 5010,
		/// <summary>
		/// A CUDA Driver API error. This error indicates a CUDA driver-API error. If this is set, a CDUA-specific error code is set in the <see cref="CudaFileError.DriverResult"/>.
		/// </summary>
		CudaDriverError = 5011,
		/// <summary>
		/// An invalid device pointer.
		/// </summary>
		CudaPointerInvalid = 5012,
		/// <summary>
		/// An invalid pointer memory type.
		/// </summary>
		CudaMemoryTypeInvalid = 5013,
		/// <summary>
		/// The pointer range exceeds the allocated address range.
		/// </summary>
		CudaPointerRangeError = 5014,
		/// <summary>
		/// A CUDA context mismatch.
		/// </summary>
		CudaContextMismatch = 5015,
		/// <summary>
		/// Access beyond the maximum pinned memory size.
		/// </summary>
		InvalidMappingSize = 5016,
		/// <summary>
		/// Access beyond the mapped size.
		/// </summary>
		InvalidMappingRange = 5017,
		/// <summary>
		/// An unsupported file type.
		/// </summary>
		InvalidFileType = 5018,
		/// <summary>
		/// Unsupported file open flags.
		/// </summary>
		InvalidFileOpenFlag = 5019,
		/// <summary>
		/// The file direct IO is not set.
		/// </summary>
		DirectIONotSet = 5020,
		/// <summary>
		/// Invalid API arguments.
		/// </summary>
		InvalidValue = 5022,
		/// <summary>
		/// Device pointer is already registered.
		/// </summary>
		MemoryAlreadyRegistered = 5023,
		/// <summary>
		/// A device pointer lookup failure has occurred.
		/// </summary>
		MemoryNotRegistered = 5024,
		/// <summary>
		/// A driver or file access error.
		/// </summary>
		PermissionDenied = 5025,
		/// <summary>
		/// The driver is already open.
		/// </summary>
		DriverAlreadyOpen = 5026,
		/// <summary>
		/// The file descriptor is not registered.
		/// </summary>
		HandleNotRegistered = 5027,
		/// <summary>
		/// The file descriptor is already registered.
		/// </summary>
		HandleAlreadyRegistered = 5028,
		/// <summary>
		/// The GPU device cannot be not found.
		/// </summary>
		DeviceNotFound = 5029,
		/// <summary>
		/// An internal error has occurred.
		/// </summary>
		InternalError = 5030,
		/// <summary>
		/// Failed to obtain new file descriptor.
		/// </summary>
		NewFileDescriptorFailed = 5031,
		/// <summary>
		/// An NVFS driver initialization error has occurred.
		/// </summary>
		NvfsSetupError = 5033,
		/// <summary>
		/// GDS is disabled by configuration on the current file.
		/// </summary>
		IODisabled = 5034,
	}

	/// <summary>
	/// The CDUA file operation or driver error wrapper returned by the CUDA GPUDirect® Storage (GDS) APIs
	/// </summary>
	public readonly record struct CudaFileError(CudaFileOpError FileOpResult, CudaError DriverResult)
	{
		/// <summary>
		/// Check whether this <see cref="CudaFileError"/> represents a success status
		/// </summary>
		public bool IsSuccess {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.FileOpResult == CudaFileOpError.Success;
		}
	}
	#endregion

	#region CUDA file structures
	/// <summary>
	/// The supported feature flags of a certain CUDA file driver
	/// </summary>
	public enum CudaFileFeatureFlag
	{
		/// <summary>
		/// The dynamic routing feature of CUDA file driver is supported
		/// </summary>
		DynamicRouting = 0,
		/// <summary>
		/// The batched I/O operation of CUDA file driver is supported
		/// </summary>
		BatchIO = 1,
		/// <summary>
		/// The CUDA streams can be used in the CUDA file driver
		/// </summary>
		Streams = 2
	}

	// Ignore Spelling: Lustre
	/// <summary>
	/// The solution supporting status of a CUDA file driver
	/// </summary>
	public enum CudaFileDriverStatusFlag
	{
		/// <summary>
		/// The DDN EXAScaler® parallel file system solutions (based on the Lustre file system) client supports GDS.
		/// </summary>
		EXAScaler = 0,
		/// <summary>
		/// The WekaFS supports GDS.
		/// </summary>
		WekaFS = 1
	}

	/// <summary>
	/// The supported control mode of a CUDA file driver
	/// </summary>
	public enum CudaFileDriverControlFlag
	{
		/// <summary>
		/// CUDA file driver operates IO in the polling mode
		/// </summary>
		UsePollMode = 0,
		/// <summary>
		/// CUDA file driver operates IO in the compatible mode
		/// </summary>
		AllowCompatibleMode = 1,
	}

	/// <summary>
	/// The structure that wraps the properties of a CDUA file driver
	/// </summary>
	/// <param name="Nfsp">The instance of <see cref="NvidiaFileSystemProperty"/> that wrappers the properties of a NVIDIA file system</param>
	/// <param name="Flags">The <see cref="CudaFileFeatureFlag"/> of current CDUA file driver</param>
	/// <param name="MaxDeviceCacheSize">The maximum GPU buffer space per device, in KiB and 4K-aligned, that is used internally, for example, to handle unaligned IO and optimal IO path routing. This value might be rounded down to the nearest GPU page size.</param>
	/// <param name="PerBufferCacheSize">The GPU bounce buffer size, in KiB, used for internal pools.</param>
	/// <param name="MaxPinnedMemorySize">The maximum buffer space, in KiB, that is pinned and mapped. See <see cref="NativeMethods.cuFileDriverSetMaxPinnedMemSize(long)"/>.</param>
	/// <param name="MaxBatchIOTimeout">The timeout in milliseconds for batched IO operations</param>
	[StructLayout(LayoutKind.Sequential)]
	public readonly record struct CudaFileDriverProperty(CudaFileDriverProperty.NvidiaFileSystemProperty Nfsp, CudaFileFeatureFlag Flags, int MaxDeviceCacheSize, int PerBufferCacheSize, int MaxPinnedMemorySize, int MaxBatchIOTimeout)
	{
		/// <summary>
		/// The structure that wrappers the properties of a NVIDIA file system
		/// </summary>
		[StructLayout(LayoutKind.Sequential)]
		public readonly struct NvidiaFileSystemProperty
		{
			/// <summary>
			/// The major version of the NVIDIA file system
			/// </summary>
			public readonly int majorVersion;
			/// <summary>
			/// The minor version of the NVIDIA file system
			/// </summary>
			public readonly int minorVersion;
			/// <summary>
			/// The maximum IO size, in KiB and 4K-aligned, that is used for the <see cref="CudaFileDriverControlFlag.UsePollMode"/>
			/// </summary>
			public readonly long pollThreshSize;
			/// <summary>
			/// The maximum GDS IO size, in KiB and 4K-aligned, that is requested by the nvidia-fs driver to the underlying file system
			/// </summary>
			public readonly long maxDirectIOSize;
			/// <summary>
			/// The <see cref="CudaFileDriverStatusFlag"/> of current nvidia file system
			/// </summary>
			public readonly CudaFileDriverStatusFlag status;
			/// <summary>
			/// The <see cref="CudaFileDriverControlFlag"/> of current nvidia file system
			/// </summary>
			public readonly CudaFileDriverControlFlag control;
		}
	}

	/// <summary>
	/// The enum that indicates the type of the file handle
	/// </summary>
	public enum CudaFileHandleType
	{
		/// <summary>
		/// The opaque file handle of Linux POSIX
		/// </summary>
		OpaqueLinux = 1,
		/// <summary>
		/// The opaque file handle of Windows file system
		/// </summary>
		OpaqueWindows = 2,
		/// <summary>
		/// A user-space based file system
		/// </summary>
		Userspace = 3,
	}

	/// <summary>
	/// The class containing the function pointers of CUDA file system operations
	/// </summary>
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
	public unsafe class FileSystemOperations
	{
		// NULL means discover using FSTAT
		// input = file handle
		private readonly delegate* unmanaged<IntPtr, string> getFileSystemType;

		// list of host addresses to use, NULL means no restriction
		// input = file handle, output host addresses
		private readonly delegate* unmanaged<IntPtr, void**, int> getRDMADeviceList;

		// input = file handle, device memory pointer, size, offset, host address
		// return -1 means no pref
		private readonly delegate* unmanaged<IntPtr, IntPtr, ulong, long, void*, int> getRDMADevicePriority;

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
		private readonly struct RDMAInfo
		{
			private readonly int version;
			private readonly int desc_len;
			private readonly string desc_str;
		}

		// NULL means try VFS
		// input = file handle, device memory pointer, size, offset, RDMAInfo
		// return size of bytes that were successfully read
		private readonly delegate* unmanaged<IntPtr, IntPtr, ulong, long, ref RDMAInfo, long> read;

		// NULL means try VFS
		// input = file handle, device memory pointer, size, offset, RDMAInfo
		// return size of bytes that were successfully written
		private readonly delegate* unmanaged<IntPtr, IntPtr, ulong, long, ref RDMAInfo, long> write;
	}

	/// <summary>
	/// The structure that wraps the information about a registered CUDA file
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public readonly record struct CudaFileDescription(CudaFileHandleType Type, IntPtr OsHandle, FileSystemOperations Operations)
	{
		/// <summary>
		/// Get the file handle of this registered file as if the operating system is Windows
		/// </summary>
		public IntPtr FileHandleAsWindows {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.OsHandle;
		}

		/// <summary>
		/// Get the file handle of this registered file as if the operating system is Linux
		/// </summary>
		public int FileHandleAsLinux {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.OsHandle.ToInt32();
		}

		/// <summary>
		/// Create a <see cref="CudaFileDescription"/> with given <see cref="CudaFileHandleType"/> and the actual file <paramref name="handle"/>
		/// </summary>
		/// <param name="type">The <see cref="CudaFileHandleType"/> indicating the type of the <paramref name="handle"/></param>
		/// <param name="handle">The actual file handle as a <see cref="IntPtr"/></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public CudaFileDescription(CudaFileHandleType type, IntPtr handle) : this(type, handle, new()) { }
	}
	#endregion

	#region CUDA file pointer
	/// <summary>
	/// The wrapper for CUDA file handle and its size that implements <see cref="IPointer{TSelf}"/>.
	/// </summary>
	public readonly struct CudaFilePointer : IPointer<CudaFilePointer>, IDisposable
	{
		#region basic
		/// <inheritdoc/>
		public static StorageLocation Location => new(LocationType.Uri, (short)UriScheme.File);

		static CudaFilePointer IPointer<CudaFilePointer>.Default => throw new NotImplementedException();

		private readonly FileStream stream;
		private readonly IntPtr handle;
		/// <summary>
		/// The underlying CUDA file handle
		/// </summary>
		public readonly IntPtr Handle => this.handle;
		/// <summary>
		/// The size of the underlying file in bytes
		/// </summary>
		public readonly long LengthInBytes => this.stream.Length;

		/// <inheritdoc/>
		public bool IsValid() => this.Handle != default;

		/// <summary>
		/// Create a new <see cref="CudaFilePointer"/> with given <paramref name="filePath"/>.
		/// </summary>
		/// <param name="filePath">The given path to the file to be created or overwritten</param>
		/// <param name="readOnly">Whether the file shall be opened as read-only or read-and-write</param>
		public CudaFilePointer(string filePath, bool readOnly = false)
		{
			// stream
			this.stream = new(filePath, FileMode.OpenOrCreate, readOnly ? FileAccess.Read : FileAccess.ReadWrite, FileShare.Read, 0);
			// CUDA file
			CudaFileDescription descr = new(Environment.OSVersion.Platform == PlatformID.Unix ? CudaFileHandleType.OpaqueLinux : CudaFileHandleType.OpaqueWindows, this.stream.SafeFileHandle.DangerousGetHandle());
			var err = NativeMethods.cuFileHandleRegister(out this.handle, ref descr);
			if (!err.IsSuccess)
			{
				this.Dispose();
				err.Check();
			}
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			if (this.stream is null)
				return;
			NativeMethods.cuFileHandleDeregister(this.handle).Check();
			this.stream.Dispose();
			File.Delete(this.stream.Name);
		}
		#endregion

		#region equality
		/// <inheritdoc/>
		public bool Equals(CudaFilePointer cudaFile) => this.handle == cudaFile.handle;

		/// <inheritdoc/>
		public override bool Equals(object? obj) => obj is CudaFilePointer cudaFile && this.Equals(cudaFile);

		/// <inheritdoc/>
		public override int GetHashCode() => this.handle.GetHashCode();

		/// <inheritdoc/>
		public static bool operator ==(CudaFilePointer left, CudaFilePointer right) => left.Equals(right);
		/// <inheritdoc/>
		public static bool operator !=(CudaFilePointer left, CudaFilePointer right) => !left.Equals(right);

		/// <inheritdoc/>
		public override string ToString() => $"[CudaHandle = {this.handle:X}, File = {this.stream.Name}]";
		#endregion
	}

	#endregion
}

#region error checks
namespace Althea.Backend.Cuda
{
	/// <summary>
	/// The static class containing extension methods for <see cref="CudaFileError"/> and <see cref="CudaFileOpError"/>
	/// </summary>
	public static partial class StatusExtension
	{
		/// <summary>
		/// Check whether the input <see cref="CudaFileError"/> is success or not and throw exception if it is not
		/// </summary>
		/// <param name="err">The <see cref="CudaFileError"/> to be checked</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Check(this CudaFileError err)
		{
			if (err.IsSuccess)
			{
				if (err.FileOpResult == CudaFileOpError.CudaDriverError)
					throw new StatusException(err.FileOpResult, err.DriverResult, new StackTrace(0));
				else
					throw new StatusException(err.FileOpResult, new StackTrace(0));
			}
		}

		/// <summary>
		/// Check whether the input <see cref="CudaFileOpError"/> is success or not and throw exception if it is not
		/// </summary>
		/// <param name="err">The <see cref="CudaFileOpError"/> to be checked</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Check(this CudaFileOpError err)
		{
			if (err != CudaFileOpError.Success)
			{
				throw new StatusException(err, new StackTrace(0));
			}
		}

		/// <summary>
		/// Check whether the output of <see cref="Storage.NativeMethods.cuFileRead"/> and <see cref="Storage.NativeMethods.cuFileWrite"/> is success or not and throw exception if it is not
		/// </summary>
		/// <param name="err">The <see cref="CudaFileOpError"/> to be checked</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void Check(this long err)
		{
			if (err < 0)
			{
				if (err <= -(int)CudaFileOpError.DriverNotInitialized)
					throw new StatusException((CudaFileOpError)(int)err, new StackTrace(0));
				else
					throw new System.IO.IOException(Resource.CuFileFS, (int)err);
			}
		}
	}
}
#endregion