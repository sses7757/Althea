using System;
using System.Runtime.InteropServices;


namespace Althea.Backend.Cuda.Storage
{
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
		/// A CUDA Driver API error. This error indicates a CUDA driver-API error. If this is set, a CDUA-specific error code is set in the following <see cref="CudaError"/>.
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
		/// The file direct Io is not set.
		/// </summary>
		DioNotSet = 5020,
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
		NewfdFailed = 5031,
		/// <summary>
		/// An NVFS driver initialization error has occurred.
		/// </summary>
		NvfsSetupError = 5033,
		/// <summary>
		/// GDS is disabled by configuration on the current file.
		/// </summary>
		IoDisabled = 5034,
	}

	/// <summary>
	/// The CDUA file operation or driver error wrapper returned by the CUDA GPUDirect® Storage (GDS) APIs
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public readonly struct CudaFileError
	{
		/// <summary>
		/// The <see cref="CudaFileOpError"/> that represents the CUDA file operation error type or success
		/// </summary>
		public readonly CudaFileOpError FileOpResult;

		/// <summary>
		/// The <see cref="CudaError"/> that represents the CUDA driver error type or success
		/// </summary>
		public readonly CudaError DriverResult;
	}
}
