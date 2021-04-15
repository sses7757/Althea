using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Althea.Helpers;


namespace Althea.Backend.Cuda.Storage
{
	#region CDUA device property
	// Ignore Spelling: mipmapped Cubemap
	/// <summary>
	/// The structure that wraps the properties of a CUDA device
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public readonly struct CudaDeviceProperty
	{
		/// <summary>
		/// ASCII string identifying device
		/// </summary>
		public readonly FixedBuffer_256<byte> name;
		/// <summary>
		/// 16-byte unique identifier
		/// </summary>
		public readonly FixedBuffer_16<byte> uuid;
		/// <summary>
		/// 8-byte locally unique identifier. Value is undefined on TCC and non-Windows platforms
		/// </summary>
		public readonly FixedBuffer_8<byte> luid;
		/// <summary>
		/// LUID device node mask. Value is undefined on TCC and non-Windows platforms
		/// </summary>
		public readonly int luidDeviceNodeMask;
		/// <summary>
		/// Global memory available on device in bytes
		/// </summary>
		public readonly long totalGlobalMem;
		/// <summary>
		/// Shared memory available per block in bytes
		/// </summary>
		public readonly long sharedMemPerBlock;
		/// <summary>
		/// 32-bit registers available per block
		/// </summary>
		public readonly int regsPerBlock;
		/// <summary>
		/// Warp size in threads
		/// </summary>
		public readonly int warpSize;
		/// <summary>
		/// Maximum pitch in bytes allowed by memory copies
		/// </summary>
		public readonly long memPitch;
		/// <summary>
		/// Maximum number of threads per block
		/// </summary>
		public readonly int maxThreadsPerBlock;
		/// <summary>
		/// Maximum size of each dimension of a block
		/// </summary>
		public readonly FixedBuffer_12<int> maxThreadsDim;
		/// <summary>
		/// Maximum size of each dimension of a grid
		/// </summary>
		public readonly FixedBuffer_12<int> maxGridSize;
		/// <summary>
		/// Clock frequency in kilohertz
		/// </summary>
		public readonly int clockRate;
		/// <summary>
		/// Constant memory available on device in bytes
		/// </summary>
		public readonly long totalConstMem;
		/// <summary>
		/// Major compute capability
		/// </summary>
		public readonly int major;
		/// <summary>
		/// Minor compute capability
		/// </summary>
		public readonly int minor;
		/// <summary>
		/// Alignment requirement for textures
		/// </summary>
		public readonly long textureAlignment;
		/// <summary>
		/// Pitch alignment requirement for texture references bound to pitched memory
		/// </summary>
		public readonly long texturePitchAlignment;
		/// <summary>
		/// Device can concurrently copy memory and execute a kernel. Deprecated. Use instead asyncEngineCount.
		/// </summary>
		public readonly int deviceOverlap;
		/// <summary>
		/// Number of multiprocessors on device
		/// </summary>
		public readonly int multiProcessorCount;
		/// <summary>
		/// Specified whether there is a run time limit on kernels
		/// </summary>
		public readonly int kernelExecTimeoutEnabled;
		/// <summary>
		/// Device is integrated as opposed to discrete
		/// </summary>
		public readonly int integrated;
		/// <summary>
		/// Device can map host memory with cudaHostAlloc/cudaHostGetDevicePointer
		/// </summary>
		public readonly int canMapHostMemory;
		/// <summary>
		/// Compute mode (See ::cudaComputeMode)
		/// </summary>
		public readonly int computeMode;
		/// <summary>
		/// Maximum 1D texture size
		/// </summary>
		public readonly int maxTexture1D;
		/// <summary>
		/// Maximum 1D mipmapped texture size
		/// </summary>
		public readonly int maxTexture1DMipmap;
		/// <summary>
		/// Maximum size for 1D textures bound to linear memory
		/// </summary>
		public readonly int maxTexture1DLinear;
		/// <summary>
		/// Maximum 2D texture dimensions
		/// </summary>
		public readonly FixedBuffer_8<int> maxTexture2D;
		/// <summary>
		/// Maximum 2D mipmapped texture dimensions
		/// </summary>
		public readonly FixedBuffer_8<int> maxTexture2DMipmap;
		/// <summary>
		/// Maximum dimensions (width, height, pitch) for 2D textures bound to pitched memory
		/// </summary>
		public readonly FixedBuffer_12<int> maxTexture2DLinear;
		/// <summary>
		/// Maximum 2D texture dimensions if texture gather operations have to be performed
		/// </summary>
		public readonly FixedBuffer_8<int> maxTexture2DGather;
		/// <summary>
		/// Maximum 3D texture dimensions
		/// </summary>
		public readonly FixedBuffer_12<int> maxTexture3D;
		/// <summary>
		/// Maximum alternate 3D texture dimensions
		/// </summary>
		public readonly FixedBuffer_12<int> maxTexture3DAlt;
		/// <summary>
		/// Maximum Cubemap texture dimensions
		/// </summary>
		public readonly int maxTextureCubemap;
		/// <summary>
		/// Maximum 1D layered texture dimensions
		/// </summary>
		public readonly FixedBuffer_8<int> maxTexture1DLayered;
		/// <summary>
		/// Maximum 2D layered texture dimensions
		/// </summary>
		public readonly FixedBuffer_12<int> maxTexture2DLayered;
		/// <summary>
		/// Maximum Cubemap layered texture dimensions
		/// </summary>
		public readonly FixedBuffer_8<int> maxTextureCubemapLayered;
		/// <summary>
		/// Maximum 1D surface size
		/// </summary>
		public readonly int maxSurface1D;
		/// <summary>
		/// Maximum 2D surface dimensions
		/// </summary>
		public readonly FixedBuffer_8<int> maxSurface2D;
		/// <summary>
		/// Maximum 3D surface dimensions
		/// </summary>
		public readonly FixedBuffer_12<int> maxSurface3D;
		/// <summary>
		/// Maximum 1D layered surface dimensions
		/// </summary>
		public readonly FixedBuffer_8<int> maxSurface1DLayered;
		/// <summary>
		/// Maximum 2D layered surface dimensions
		/// </summary>
		public readonly FixedBuffer_12<int> maxSurface2DLayered;
		/// <summary>
		/// Maximum Cubemap surface dimensions
		/// </summary>
		public readonly int maxSurfaceCubemap;
		/// <summary>
		/// Maximum Cubemap layered surface dimensions
		/// </summary>
		public readonly FixedBuffer_8<int> maxSurfaceCubemapLayered;
		/// <summary>
		/// Alignment requirements for surfaces
		/// </summary>
		public readonly long surfaceAlignment;
		/// <summary>
		/// Device can possibly execute multiple kernels concurrently
		/// </summary>
		public readonly int concurrentKernels;
		/// <summary>
		/// Device has ECC support enabled
		/// </summary>
		public readonly int ECCEnabled;
		/// <summary>
		/// PCI bus ID of the device
		/// </summary>
		public readonly int pciBusID;
		/// <summary>
		/// PCI device ID of the device
		/// </summary>
		public readonly int pciDeviceID;
		/// <summary>
		/// PCI domain ID of the device
		/// </summary>
		public readonly int pciDomainID;
		/// <summary>
		/// 1 if device is a Tesla device using TCC driver, 0 otherwise
		/// </summary>
		public readonly int tccDriver;
		/// <summary>
		/// Number of asynchronous engines
		/// </summary>
		public readonly int asyncEngineCount;
		/// <summary>
		/// Device shares a unified address space with the host
		/// </summary>
		public readonly int unifiedAddressing;
		/// <summary>
		/// Peak memory clock frequency in kilohertz
		/// </summary>
		public readonly int memoryClockRate;
		/// <summary>
		/// Global memory bus width in bits
		/// </summary>
		public readonly int memoryBusWidth;
		/// <summary>
		/// Size of L2 cache in bytes
		/// </summary>
		public readonly int l2CacheSize;
		/// <summary>
		/// Device's maximum l2 persisting lines capacity setting in bytes
		/// </summary>
		public readonly int persistingL2CacheMaxSize;
		/// <summary>
		/// Maximum resident threads per multiprocessor
		/// </summary>
		public readonly int maxThreadsPerMultiProcessor;
		/// <summary>
		/// Device supports stream priorities
		/// </summary>
		public readonly int streamPrioritiesSupported;
		/// <summary>
		/// Device supports caching globals in L1
		/// </summary>
		public readonly int globalL1CacheSupported;
		/// <summary>
		/// Device supports caching locals in L1
		/// </summary>
		public readonly int localL1CacheSupported;
		/// <summary>
		/// Shared memory available per multiprocessor in bytes
		/// </summary>
		public readonly long sharedMemPerMultiprocessor;
		/// <summary>
		/// 32-bit registers available per multiprocessor
		/// </summary>
		public readonly int regsPerMultiprocessor;
		/// <summary>
		/// Device supports allocating managed memory on this system
		/// </summary>
		public readonly int managedMemory;
		/// <summary>
		/// Device is on a multi-GPU board
		/// </summary>
		public readonly int isMultiGpuBoard;
		/// <summary>
		/// Unique identifier for a group of devices on the same multi-GPU board
		/// </summary>
		public readonly int multiGpuBoardGroupID;
		/// <summary>
		/// Link between the device and the host supports native atomic operations
		/// </summary>
		public readonly int hostNativeAtomicSupported;
		/// <summary>
		/// Ratio of single precision performance (in floating-point operations per second) to double precision performance
		/// </summary>
		public readonly int singleToDoublePrecisionPerfRatio;
		/// <summary>
		/// Device supports coherently accessing page-able memory without calling cudaHostRegister on it
		/// </summary>
		public readonly int pageableMemoryAccess;
		/// <summary>
		/// Device can coherently access managed memory concurrently with the CPU
		/// </summary>
		public readonly int concurrentManagedAccess;
		/// <summary>
		/// Device supports Compute Preemption
		/// </summary>
		public readonly int computePreemptionSupported;
		/// <summary>
		/// Device can access host registered memory at the same virtual address as the CPU
		/// </summary>
		public readonly int canUseHostPointerForRegisteredMem;
		/// <summary>
		/// Device supports launching cooperative kernels via ::cudaLaunchCooperativeKernel
		/// </summary>
		public readonly int cooperativeLaunch;
		/// <summary>
		/// Device can participate in cooperative kernels launched via ::cudaLaunchCooperativeKernelMultiDevice
		/// </summary>
		public readonly int cooperativeMultiDeviceLaunch;
		/// <summary>
		/// Per device maximum shared memory per block usable by special opt in
		/// </summary>
		public readonly long sharedMemPerBlockOptin;
		/// <summary>
		/// Device accesses page-able memory via the host's page tables
		/// </summary>
		public readonly int pageableMemoryAccessUsesHostPageTables;
		/// <summary>
		/// Host can directly access managed memory on the device without migration.
		/// </summary>
		public readonly int directManagedMemAccessFromHost;
		/// <summary>
		/// Maximum number of resident blocks per multiprocessor
		/// </summary>
		public readonly int maxBlocksPerMultiProcessor;
		/// <summary>
		/// The maximum value of ::cudaAccessPolicyWindow::num_bytes.
		/// </summary>
		public readonly int accessPolicyMaxWindowSize;
		/// <summary>
		/// Shared memory reserved by CUDA driver per block in bytes
		/// </summary>
		public readonly long reservedSharedMemPerBlock;
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
		/// A CUDA Driver API error. This error indicates a CUDA driver-API error. If this is set, a CDUA-specific error code is set in the <see cref="CudaFileError.driverResult"/>.
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
	[StructLayout(LayoutKind.Sequential)]
	public readonly struct CudaFileError : IEquatable<CudaFileError>
	{
		internal readonly CudaFileOpError fileOpResult;

		internal readonly CudaError driverResult;

		/// <summary>
		/// Check whether this <see cref="CudaFileError"/> represents a success status
		/// </summary>
		public bool IsSuccess {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.fileOpResult == CudaFileOpError.Success;
		}

		/// <summary>
		/// Equality comparer
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(CudaFileError a, CudaFileError b) => a.Equals(b);

		/// <summary>
		/// Inequality comparer
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(CudaFileError a, CudaFileError b) => !a.Equals(b);

		/// <summary>
		/// Check whether this <see cref="CudaFileError"/> represents the same value as the <paramref name="other"/> one
		/// </summary>
		/// <param name="other">The other <see cref="CudaFileError"/> to compare</param>
		/// <returns><c>this == <paramref name="other"/></c></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(CudaFileError other) => this.IsSuccess && other.IsSuccess || (this.fileOpResult == other.fileOpResult && this.driverResult == other.driverResult);

		/// <summary>
		/// Check whether this <see cref="CudaFileError"/> represents the same value as the <paramref name="obj"/>
		/// </summary>
		/// <param name="obj">The other <see cref="object"/> to compare</param>
		/// <returns><c>this == <paramref name="obj"/></c></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override bool Equals(object? obj)
		{
			return obj is CudaFileError a && this.Equals(a);
		}

		/// <summary>
		/// Get the hash code of this <see cref="CudaFileError"/>
		/// </summary>
		/// <returns>The hash code of this <see cref="CudaFileError"/></returns>
		public override int GetHashCode()
		{
			return this.IsSuccess ? 0 : HashCode.Combine(this.fileOpResult, this.driverResult);
		}

		/// <summary>
		/// Get the string representation of this <see cref="CudaFileError"/>
		/// </summary>
		/// <returns>The string representation of this <see cref="CudaFileError"/></returns>
		public override string ToString()
		{
			return this.IsSuccess ? "Success" : this.fileOpResult == CudaFileOpError.CudaDriverError ? this.driverResult.ToString() : this.fileOpResult.ToString();
		}
	}

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
				if (err.fileOpResult == CudaFileOpError.CudaDriverError)
					throw new StatusException(err.fileOpResult, err.driverResult, new StackTrace(0));
				else
					throw new StatusException(err.fileOpResult, new StackTrace(0));
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
		/// Check whether the output of <see cref="NativeMethods.cuFileRead"/> and <see cref="NativeMethods.cuFileWrite"/> is success or not and throw exception if it is not
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
	#endregion

	#region CUDA file structures
	/// <summary>
	/// The supported feature flags of a certain CUDA file driver
	/// </summary>
	[Flags]
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
	[Flags]
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
	[Flags]
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
	[StructLayout(LayoutKind.Sequential)]
	public readonly struct CudaFileDriverProperty
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
		/// <summary>
		/// The <see cref="CudaFileFeatureFlag"/> of current CDUA file driver
		/// </summary>
		public readonly CudaFileFeatureFlag flags;
		/// <summary>
		/// The maximum GPU buffer space per device, in KiB and 4K-aligned, that is used internally, for example, to handle unaligned IO and optimal IO path routing. This value might be rounded down to the nearest GPU page size.
		/// </summary>
		public readonly int maxDeviceCacheSize;
		/// <summary>
		/// The GPU bounce buffer size, in KiB, used for internal pools.
		/// </summary>
		public readonly int perBufferCacheSize;
		/// <summary>
		/// The maximum buffer space, in KiB, that is pinned and mapped. See <see cref="NativeMethods.cuFileDriverSetMaxPinnedMemSize(long)"/>.
		/// </summary>
		public readonly int maxPinnedMemorySize;
		/// <summary>
		/// The timeout in milliseconds for batched IO operations
		/// </summary>
		public readonly int maxBatchIOTimeout;
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
		private readonly delegate* unmanaged<CudaFileHandle, string> getFileSystemType;

		// list of host addresses to use, NULL means no restriction
		// input = file handle, output host addresses
		private readonly delegate* unmanaged<CudaFileHandle, void**, int> getRDMADeviceList;

		// input = file handle, device memory pointer, size, offset, host address
		// return -1 means no pref
		private readonly delegate* unmanaged<CudaFileHandle, IntPtr, ulong, long, void*, int> getRDMADevicePriority;

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
		private readonly delegate* unmanaged<CudaFileHandle, IntPtr, ulong, long, ref RDMAInfo, long> read;

		// NULL means try VFS
		// input = file handle, device memory pointer, size, offset, RDMAInfo
		// return size of bytes that were successfully written
		private readonly delegate* unmanaged<CudaFileHandle, IntPtr, ulong, long, ref RDMAInfo, long> write;
	}

	/// <summary>
	/// The structure that wraps the information about a registered CUDA file
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public readonly struct CudaFileDescription : ICloneable<CudaFileDescription>
	{
		private readonly CudaFileHandleType type;

		private readonly IntPtr handle;

		private readonly FileSystemOperations operations;

		/// <summary>
		/// Get the <see cref="CudaFileHandleType"/> of this registered file
		/// </summary>
		public CudaFileHandleType HandleType {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.type;
		}

		/// <summary>
		/// Get the file handle of this registered file as if the operating system is Windows
		/// </summary>
		public IntPtr FileHandleAsWindows {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.handle;
		}

		/// <summary>
		/// Get the file handle of this registered file as if the operating system is Linux
		/// </summary>
		public int FileHandleAsLinux {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.handle.ToInt32();
		}

		/// <summary>
		/// Create a <see cref="CudaFileDescription"/> with given <see cref="CudaFileHandleType"/> and the actual file <paramref name="handle"/>
		/// </summary>
		/// <param name="type">The <see cref="CudaFileHandleType"/> indicating the type of the <paramref name="handle"/></param>
		/// <param name="handle">The actual file handle as a <see cref="IntPtr"/></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public CudaFileDescription(CudaFileHandleType type, IntPtr handle) : this(type, handle, new()) { }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private CudaFileDescription(CudaFileHandleType type, IntPtr handle, FileSystemOperations op)
		{
			this.type = type; this.handle = handle;
			this.operations = op;
		}

		/// <summary>
		/// Clone this <see cref="CudaFileDescription"/>
		/// </summary>
		/// <returns>The cloned <see cref="CudaFileDescription"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public CudaFileDescription Clone()
		{
			return new(this.type, this.handle, this.operations);
		}
	}

	/// <summary>
	/// The structure that wraps file handle managed by CUDA file runtime
	/// </summary>
	public readonly struct CudaFileHandle
	{
		private readonly IntPtr pointer;

		/// <summary>
		/// Get the string representation of this <see cref="CudaFileHandle"/>
		/// </summary>
		/// <returns>The string representation of this <see cref="CudaFileHandle"/></returns>
		public override string ToString() => this.pointer.ToString("X");
	}
	#endregion
}
