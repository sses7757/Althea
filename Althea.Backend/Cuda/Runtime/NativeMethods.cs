using System.Runtime.InteropServices;


namespace Althea.Backend.Cuda;

/// <summary>
/// Native methods from CUDA runtime and GPUDirect® Storage API
/// </summary>
public static class NativeMethods
{
	/// <summary>
	/// The CUDA Runtime library name
	/// </summary>
	public const string CUDART_DLL_NAME = @"cudart";
	/// <summary>
	/// The cuFile library name
	/// </summary>
	public const string CUFILE_DLL_NAME = @"cufile";
	/// <summary>
	/// The cuBlas library name
	/// </summary>
	public const string CUBLAS_DLL_NAME = @"cublas";
	/// <summary>
	/// The cuRand library name
	/// </summary>
	public const string CURAND_DLL_NAME = @"curand";
	/// <summary>
	/// The cuSolver library name
	/// </summary>
	public const string CUSOLVER_DLL_NAME = @"cusolver";
	/// <summary>
	/// The cuSparse library name
	/// </summary>
	public const string CUSPARSE_DLL_NAME = @"cusparse";
	/// <summary>
	/// The cuFft library name
	/// </summary>
	public const string CUFFT_DLL_NAME = @"cufft";
	/// <summary>
	/// The cuTensor library name
	/// </summary>
	public const string CUTENSOR_DLL_NAME = @"cutensor";
	/// <summary>
	/// The custom CUDA library name
	/// </summary>
	public const string CUSTOM_DLL_NAME = @"SupplementCUDA";

	#region device utilities
	/// <summary>
	/// Destroy all allocations and reset all state on the current device in the current process.
	/// </summary>
	[DllImport(CUDART_DLL_NAME)]
	internal static extern CudaError cudaDeviceReset();

	/// <summary>
	/// Set device to be used for GPU executions.
	/// </summary>
	/// <param name="device">device ID</param>
	[DllImport(CUDART_DLL_NAME)]
	internal static extern CudaError cudaSetDevice(int device);

	/// <summary>
	/// Get device used for GPU executions.
	/// </summary>
	/// <param name="device">device ID</param>
	[DllImport(CUDART_DLL_NAME)]
	internal static extern CudaError cudaGetDevice(out int device);

	/// <summary>
	/// Wait for compute device to finish.
	/// </summary>
	[DllImport(CUDART_DLL_NAME)]
	internal static extern CudaError cudaDeviceSynchronize();

	/// <summary>
	/// Returns the number of compute-capable devices.
	/// </summary>
	/// <param name="count">returned device count</param>
	[DllImport(CUDART_DLL_NAME)]
	internal static extern CudaError cudaGetDeviceCount(out int count);

	/// <summary>
	/// Gets free and total device memory.
	/// </summary>
	/// <param name="free">returned free memory in bytes</param>
	/// <param name="total">returned total memory in bytes</param>
	[DllImport(CUDART_DLL_NAME)]
	internal static extern CudaError cudaMemGetInfo(out long free, out long total);

	/// <summary>
	/// Returns the CUDA Runtime version.
	/// </summary>
	/// <param name="runtimeVersion">The version is returned as (1000 major + 10 minor). For example, CUDA 9.2 would be represented by 9020.</param>
	[DllImport(CUDART_DLL_NAME)]
	internal static extern CudaError cudaRuntimeGetVersion(out int runtimeVersion);

	/// <summary>
	/// Returns the <paramref name="property"/> about the compute <paramref name="device"/>
	/// </summary>
	/// <param name="property">The output <see cref="CudaDeviceProperty"/></param>
	/// <param name="device">The index of the CUDA device</param>
	[DllImport(CUDART_DLL_NAME)]
	internal static extern CudaError cudaGetDeviceProperties(out CudaDeviceProperty property, int device);
	#endregion

}
