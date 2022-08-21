using System.Runtime.InteropServices;


namespace Althea.Backend.Cuda.Storage;

/// <summary>
/// Native methods from CUDA runtime and GPUDirect® Storage API
/// </summary>
public static unsafe class NativeMethods
{
	#region memory manipulation
	/// <summary>
	/// Frees memory on the device.
	/// </summary>
	/// <param name="devPtr">the pointer to the array to free</param>
	[DllImport(Cuda.NativeMethods.CUDART_DLL_NAME)]
	internal static extern CudaError cudaFree(void* devPtr);

	/// <summary>
	/// Frees page-locked (pinned) memory on the host.
	/// </summary>
	/// <param name="ptr">the pointer to the array to free</param>
	[DllImport(Cuda.NativeMethods.CUDART_DLL_NAME)]
	internal static extern CudaError cudaFreeHost(void* ptr);

	/// <summary>
	/// Allocates page-locked (pinned) memory on the host.
	/// </summary>
	/// <param name="pHost">the returned host array pointer</param>
	/// <param name="size">the array size in bytes</param>
	[DllImport(Cuda.NativeMethods.CUDART_DLL_NAME)]
	internal static extern CudaError cudaMallocHost(out void* pHost, long size);

	/// <summary>
	/// Allocates memory on the device.
	/// </summary>
	/// <param name="pDev">the returned device array pointer</param>
	/// <param name="size">the array size in bytes</param>
	[DllImport(Cuda.NativeMethods.CUDART_DLL_NAME)]
	internal static extern CudaError cudaMalloc(out void* pDev, long size);

	/// <summary>
	/// Copies data between host and device linearly.
	/// </summary>
	/// <param name="dst">destination array pointer</param>
	/// <param name="src">source array pointer</param>
	/// <param name="count">length in bytes</param>
	/// <param name="kind">copy kind <see cref="MemoryCopyKind"/></param>
	[DllImport(Cuda.NativeMethods.CUDART_DLL_NAME)]
	internal static extern CudaError cudaMemcpy(void* dst, void* src, long count, MemoryCopyKind kind);

	/// <summary>
	/// Copies 2D data between host and device or within device.
	/// </summary>
	/// <param name="dst">destination pointer</param>
	/// <param name="destLD">destination array actual height (actual leading dimension) in bytes</param>
	/// <param name="src">source pointer</param>
	/// <param name="srcLD">source array actual height (actual leading dimension) in bytes</param>
	/// <param name="height">height to copy, in bytes</param>
	/// <param name="width">width to copy, in real size rather than bytes</param>
	/// <param name="kind">copy kind of <see cref="MemoryCopyKind"/></param>
	[DllImport(Cuda.NativeMethods.CUDART_DLL_NAME)]
	internal static extern CudaError cudaMemcpy2D(void* dst, long destLD, void* src, long srcLD, long height, long width, MemoryCopyKind kind);

	/// <summary>
	/// Initializes or sets device memory to a value.
	/// </summary>
	/// <param name="devPtr">Pointer to device memory</param>
	/// <param name="value">Value to set for each byte of specified memory</param>
	/// <param name="count">Size in bytes to set</param>
	[DllImport(Cuda.NativeMethods.CUDART_DLL_NAME)]
	internal static extern CudaError cudaMemset(void* devPtr, int value, long count);
	#endregion

	#region CUDA file
	/// <summary>
	/// Initialize the cuFile infrastructure
	/// </summary>
	[DllImport(Cuda.NativeMethods.CUFILE_DLL_NAME)]
	internal static extern CudaFileError cuFileDriverOpen();

	/// <summary>
	/// Finalize the cuFile system
	/// </summary>
	[DllImport(Cuda.NativeMethods.CUFILE_DLL_NAME)]
	internal static extern CudaFileError cuFileDriverClose();

	/// <summary>
	/// Query capabilities based on current versions, installed functionality
	/// </summary>
	/// <param name="prop">The output <see cref="CudaFileDriverProperty"/></param>
	[DllImport(Cuda.NativeMethods.CUFILE_DLL_NAME)]
	internal static extern CudaFileError cuFileGetDriverProperties(out CudaFileDriverProperty prop);

	/// <summary>
	/// API to set whether the Read/Write APIs use polling to do IO operations
	/// </summary>
	/// <param name="poll">Use polling mode or not</param>
	/// <param name="pollThresholdSize">The polling threshold size in KiB, must be 4K aligned</param>
	[DllImport(Cuda.NativeMethods.CUFILE_DLL_NAME)]
	internal static extern CudaFileError cuFileDriverSetPollMode(bool poll, long pollThresholdSize);

	/// <summary>
	/// API to set max IO size (KiB) used by the library to talk to NVIDIA-FS driver
	/// </summary>
	/// <param name="maxDirectIoSize">The maximum allowed direct IO size to set in KiB. The default value is 16384 KiB.This is because typically parallel-file systems perform better with bulk read/writes.</param>
	[DllImport(Cuda.NativeMethods.CUFILE_DLL_NAME)]
	internal static extern CudaFileError cuFileDriverSetMaxDirectIOSize(long maxDirectIoSize);

	/// <summary>
	/// API to set the maximum GPU buffer space, in KiB, per device and is used for internal use, for example, to handle unaligned IO and optimal IO path routing. This value might be rounded down to the nearest GPU page size.
	/// </summary>
	/// <param name="maxCacheSize">The max cache size to set in KiB, must be 4K aligned. The default value is 131072 KiB.</param>
	[DllImport(Cuda.NativeMethods.CUFILE_DLL_NAME)]
	internal static extern CudaFileError cuFileDriverSetMaxCacheSize(long maxCacheSize);

	/// <summary>
	/// API to set the maximum buffer space, in KiB, that is pinned and mapped. This value might be rounded down to the nearest GPU page size.
	/// </summary>
	/// <param name="maxPinnedMemorySize">The maximum buffer size to set in KiB, must be 4K aligned, that is pinned and mapped to the GPU BAR space.</param>
	[DllImport(Cuda.NativeMethods.CUFILE_DLL_NAME)]
	internal static extern CudaFileError cuFileDriverSetMaxPinnedMemSize(long maxPinnedMemorySize);

	/// <summary>
	/// This API registers the specified GPU <paramref name="address"/> and <paramref name="size"/> (in bytes) for use with the cuFileRead and cuFileWrite operations. The user must call <see cref="cuFileBufDeregister(void* )"/> to release the pinned memory mappings.
	/// </summary>
	/// <param name="address">Address of device pointer. cuFileRead and cuFileWrite <b>must</b> use this devPtr_base as the base address.</param>
	/// <param name="size">Size in bytes from the start of memory to map.</param>
	/// <param name="flags">Reserved for future use, must be 0.</param>
	[DllImport(Cuda.NativeMethods.CUFILE_DLL_NAME)]
	internal static extern CudaFileError cuFileBufRegister(void* address, long size, int flags);

	/// <summary>
	/// This API deregister memory mappings that were registered by <see cref="cuFileBufRegister(void* , long, int)"/>.
	/// </summary>
	/// <param name="address">Address of device pointer to release the mappings that were provided to <see cref="cuFileBufRegister(void* , long, int)"/></param>
	/// <returns></returns>
	[DllImport(Cuda.NativeMethods.CUFILE_DLL_NAME)]
	internal static extern CudaFileError cuFileBufDeregister(void* address);

	/// <summary>
	/// This API makes a file descriptor or handle that is known to the cuFile subsystem by using an OS-agnostic interface.
	/// </summary>
	/// <param name="fileHandle">Output a valid pointer to the OS-neutral cuFile handle structure that is supplied by the user but populated and maintained by the cuFile runtime.</param>
	/// <param name="descr">Input <see cref="CudaFileDescription"/> that is supplied by the user carrying details regarding the file to be opened.</param>
	[DllImport(Cuda.NativeMethods.CUFILE_DLL_NAME)]
	internal static extern CudaFileError cuFileHandleRegister(out IntPtr fileHandle, ref CudaFileDescription descr);

	/// <summary>
	/// The API is used to release resources that are claimed by cuFileHandleRegister.<br/>
	/// This API should be invoked only after the application ensures there are no outstanding IO operations with the handle.
	/// </summary>
	/// <param name="fileHandle">The <see cref="IntPtr"/> obtained from <see cref="cuFileHandleRegister(out IntPtr, ref CudaFileDescription)"/></param>
	[DllImport(Cuda.NativeMethods.CUFILE_DLL_NAME)]
	internal static extern CudaFileError cuFileHandleDeregister(in IntPtr fileHandle);

	/// <summary>
	/// This API reads the data from a specified <paramref name="fileHandle"/> at a specified <paramref name="fileOffset"/> for <paramref name="size"/> bytes into the GPU memory indicated by <paramref name="devPtr"/> and <paramref name="devPtrOffset"/> by using GDS functionality.<br/>
	/// The API works correctly for unaligned offsets and any data size, although the performance might not match the performance of aligned reads.<br/>
	/// This is a synchronous call and blocks until the IO is complete.
	/// </summary>
	/// <param name="fileHandle">The handle of the file</param>
	/// <param name="devPtr">The base address of buffer in device memory. For registered buffers, it be the same as the one used in <see cref="cuFileBufRegister(void* , long, int)"/>.</param>
	/// <param name="size">The size in bytes to read.</param>
	/// <param name="fileOffset">Offset in the file to read from.</param>
	/// <param name="devPtrOffset">Offset relative to the <paramref name="devPtr"/> to read into. This parameter should be used only with registered buffers.</param>
	/// <returns>Size of bytes that were successfully read; or -1 on an error, so error number is set to indicate file system errors. All other errors return a negative integer value of the <see cref="CudaFileOpError"/> value.</returns>
	[DllImport(Cuda.NativeMethods.CUFILE_DLL_NAME)]
	internal static extern long cuFileRead(IntPtr fileHandle, void* devPtr, long size, long fileOffset, long devPtrOffset);

	/// <summary>
	/// This API reads the data from a specified <paramref name="fileHandle"/> at a specified <paramref name="fileOffset"/> for <paramref name="size"/> bytes into the GPU memory indicated by <paramref name="devPtr"/> and <paramref name="devPtrOffset"/> by using GDS functionality.<br/>
	/// The API works correctly for unaligned offsets and any data size, although the performance might not match the performance of aligned reads.<br/>
	/// This is a synchronous call and blocks until the IO is complete.
	/// </summary>
	/// <param name="fileHandle">The handle of the file</param>
	/// <param name="devPtr">The base address of buffer in device memory. For registered buffers, it be the same as the one used in <see cref="cuFileBufRegister(void* , long, int)"/>.</param>
	/// <param name="size">The size in bytes to read.</param>
	/// <param name="fileOffset">Offset in the file to read from.</param>
	/// <param name="devPtrOffset">Offset relative to the <paramref name="devPtr"/> to read into. This parameter should be used only with registered buffers.</param>
	/// <returns>Size of bytes that were successfully read; or -1 on an error, so error number is set to indicate file system errors. All other errors return a negative integer value of the <see cref="CudaFileOpError"/> value.</returns>
	[DllImport(Cuda.NativeMethods.CUFILE_DLL_NAME)]
	internal static extern long cuFileWrite(IntPtr fileHandle, void* devPtr, long size, long fileOffset, long devPtrOffset);
	#endregion
}
