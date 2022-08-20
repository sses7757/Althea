using System.Runtime.InteropServices;


namespace Althea.Backend.Cuda.Transformer;

/// <summary>
/// The static class for native method of CUDA FFT library
/// </summary>
public static unsafe class NativeMethods
{
	[DllImport(Cuda.NativeMethods.CUFFT_DLL_NAME)]
	internal static extern CudaFftStatus cufftCreate(out int plan);

	[DllImport(Cuda.NativeMethods.CUFFT_DLL_NAME)]
	internal static extern CudaFftStatus cufftDestroy(int plan);

	[DllImport(Cuda.NativeMethods.CUFFT_DLL_NAME)]
	internal static extern CudaFftStatus cufftSetAutoAllocation(int plan, int autoAllocate);

	[DllImport(Cuda.NativeMethods.CUFFT_DLL_NAME)]
	internal static extern CudaFftStatus cufftXtMakePlanMany(int plan, int rank, Span<long> size, Span<long> inputOuterSize, long inputStride, long inputDistance, DataType inputtype, Span<long> outputOuterSize, long outputStride, long outputDistance, DataType outputtype, long batchSize, out long workSize, DataType executiontype);

	[DllImport(Cuda.NativeMethods.CUFFT_DLL_NAME)]
	internal static extern CudaFftStatus cufftSetWorkArea(int plan, void* workArea);

	[DllImport(Cuda.NativeMethods.CUFFT_DLL_NAME)]
	internal static extern CudaFftStatus cufftXtExec(int plan, void* input, void* output, FftDirection direction);
}
