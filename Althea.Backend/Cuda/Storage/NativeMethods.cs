using System;
using System.Runtime.InteropServices;


namespace Althea.Backend.Cuda.Storage
{
#pragma warning disable IDE1006 // 命名样式
	/// <summary>
	/// Native methods from CUDA runtime and GPUDirect® Storage API
	/// </summary>
	public static class NativeMethods
	{
		/// <summary>
		/// The CUDA Runtime library name
		/// </summary>
		public const string CUDART_API_DLL_NAME = @"cudart";

		#region device utilities
		/// <summary>
		/// Destroy all allocations and reset all state on the current device in the current process.
		/// </summary>
		[DllImport(CUDART_API_DLL_NAME)]
		internal static extern CudaError cudaDeviceReset();

		/// <summary>
		/// Set device to be used for GPU executions.
		/// </summary>
		/// <param name="device">device ID</param>
		[DllImport(CUDART_API_DLL_NAME)]
		internal static extern CudaError cudaSetDevice(int device);

		/// <summary>
		/// Get device used for GPU executions.
		/// </summary>
		/// <param name="device">device ID</param>
		[DllImport(CUDART_API_DLL_NAME)]
		internal static extern CudaError cudaGetDevice(ref int device);

		/// <summary>
		/// Wait for compute device to finish.
		/// </summary>
		[DllImport(CUDART_API_DLL_NAME)]
		internal static extern CudaError cudaDeviceSynchronize();

		/// <summary>
		/// Returns the number of compute-capable devices.
		/// </summary>
		/// <param name="count">returned device count</param>
		[DllImport(CUDART_API_DLL_NAME)]
		internal static extern CudaError cudaGetDeviceCount(ref int count);

		/// <summary>
		/// Gets free and total device memory.
		/// </summary>
		/// <param name="free">returned free memory in bytes</param>
		/// <param name="total">returned total memory in bytes</param>
		[DllImport(CUDART_API_DLL_NAME)]
		internal static extern CudaError cudaMemGetInfo(ref long free, ref long total);

		/// <summary>
		/// Returns the CUDA Runtime version.
		/// </summary>
		/// <param name="runtimeVersion">The version is returned as (1000 major + 10 minor). For example, CUDA 9.2 would be represented by 9020.</param>
		[DllImport(CUDART_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError cudaRuntimeGetVersion(ref int runtimeVersion);
		#endregion

		#region memory manipulation
		/// <summary>
		/// Frees memory on the device.
		/// </summary>
		/// <param name="devPtr">the pointer to the array to free</param>
		[DllImport(CUDART_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError cudaFree(IntPtr devPtr);

		/// <summary>
		/// Frees page-locked (pinned) memory on the host.
		/// </summary>
		/// <param name="ptr">the pointer to the array to free</param>
		[DllImport(CUDART_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError cudaFreeHost(IntPtr ptr);

		/// <summary>
		/// Allocates page-locked (pinned) memory on the host.
		/// </summary>
		/// <param name="pHost">the returned host array pointer</param>
		/// <param name="size">the array size in bytes</param>
		[DllImport(CUDART_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError cudaMallocHost(ref IntPtr pHost, long size);

		/// <summary>
		/// Allocates memory on the device.
		/// </summary>
		/// <param name="pDev">the returned device array pointer</param>
		/// <param name="size">the array size in bytes</param>
		[DllImport(CUDART_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError cudaMalloc(ref IntPtr pDev, long size);

		/// <summary>
		/// Copies data between host and device linearly.
		/// </summary>
		/// <param name="dst">destination array pointer</param>
		/// <param name="src">source array pointer</param>
		/// <param name="count">length in bytes</param>
		/// <param name="kind">copy kind <see cref="MemoryCopyKind"/></param>
		[DllImport(CUDART_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError cudaMemcpy(IntPtr dst, IntPtr src, long count, MemoryCopyKind kind);

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
		[DllImport(CUDART_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError cudaMemcpy2D(IntPtr dst, long destLD, IntPtr src, long srcLD, long height, long width, MemoryCopyKind kind);

		/// <summary>
		/// Initializes or sets device memory to a value.
		/// </summary>
		/// <param name="devPtr">Pointer to device memory</param>
		/// <param name="value">Value to set for each byte of specified memory</param>
		/// <param name="count">Size in bytes to set</param>
		[DllImport(CUDART_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError cudaMemset(IntPtr devPtr, int value, long count);
		#endregion

		#region custom
		/// <summary>
		/// The CUDA Runtime custom library name
		/// </summary>
		public const string CUDART_CUSTOM_API_DLL_NAME = "BlasSupplementCUDA";

		/// <summary>
		/// Get the CUDA device's compute capability
		/// </summary>
		/// <param name="deviceID">the id of device</param>
		/// <param name="major">output major compute capability</param>
		/// <param name="minor">output minor compute capability</param>
		/// <returns><see cref="CudaError"/></returns>
		[DllImport(CUDART_CUSTOM_API_DLL_NAME)]
		internal static extern CudaError getDeviceComputeCapability(int deviceID, ref int major, ref int minor);
		#endregion
	}
}
