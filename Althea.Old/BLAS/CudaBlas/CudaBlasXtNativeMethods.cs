using System;
using System.Runtime.InteropServices;


namespace Althea.Blas.Cuda.Xt
{
	/// <summary>
	/// The cublasXt API supports only the compute-intensive BLAS3 routines (e.g matrix-matrix operations) where the PCI transfers back and forth from the GPU can be amortized. <br/>
	/// It takes care of allocating the memory across the designated GPUs and dispatched the workload between them and finally retrieves the results back.
	/// </summary>
	public static class NativeMethods
	{
		// 32bit is not supported any more, only 64 bit
		/// <summary>
		/// The CUDA BLASXt library name
		/// </summary>
		public const string CUBLASXT_API_DLL_NAME = Cuda.NativeMethods.CUBLAS_API_DLL_NAME;

		#region utilities
		/// <summary>
		/// This function initializes the CUDA BLASXt library and creates a handle to an opaque structure holding the CUBLAS library context.
		/// </summary>
		/// <param name="handle">returned CUDA BLASXt handle</param>
		/// <returns>operation status enum <see cref="Status"/></returns>
		[DllImport(CUBLASXT_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasXtCreate(ref IntPtr handle);

		/// <summary>
		/// This function releases hardware resources used by the CUBLAS library. This function is usually the last call with a particular handle to the CUBLASXt library.
		/// </summary>
		/// <param name="handle">input CUDA BLASXt handle</param>
		/// <returns>operation status enum <see cref="Status"/></returns>
		[DllImport(CUBLASXT_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasXtDestroy(IntPtr handle);

		/// <summary>
		/// This function allows the user to provide the number of GPU devices and their respective Ids  that will participate to the subsequent cublasXt API Math function calls.
		/// <br/>This function will create a CUDA BLASXt context for every GPU provided in that list.
		/// <br/>Currently the device configuration is static and cannot be changed between Math function calls.
		/// </summary>
		/// <param name="handle">CUDA BLASXt context retrieved by <see cref="cublasXtCreate"/></param>
		/// <param name="nbDevices">number of devices</param>
		/// <param name="deviceId">The IDs' of devices</param>
		/// <returns>operation status enum <see cref="Status"/></returns>
		[DllImport(CUBLASXT_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasXtDeviceSelect(IntPtr handle, int nbDevices, int[] deviceId);

		/// <summary>
		/// This function allows the user to set the block dimension used for the tiling of the matrices  for the subsequent Math function calls.
		/// <br/> Matrices are split in square tiles of blockDim×blockDim dimension.
		/// <br/> This function can be called anytime and will take effect for the following Math function calls.
		/// <br/> The block dimension should be chosen in a way to optimize the math operation and to make sure that the PCI transfers are well overlapped with the computation.
		/// </summary>
		/// <param name="handle">CUDA BLASXt context retrieved by <see cref="cublasXtCreate"/></param>
		/// <param name="blockDim">block dimension to override</param>
		/// <returns>operation status enum <see cref="Status"/></returns>
		[DllImport(CUBLASXT_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasXtSetBlockDim(IntPtr handle, int blockDim);

		/// <summary>
		/// This function allows the user to query the block dimension used for the tiling of the matrices.
		/// </summary>
		/// <param name="handle">CUDA BLASXt context retrieved by <see cref="cublasXtCreate"/></param>
		/// <param name="blockDim">The returned block dimension</param>
		/// <returns>operation status enum <see cref="Status"/></returns>
		[DllImport(CUBLASXT_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasXtGetBlockDim(IntPtr handle, ref int blockDim);


		/// <summary>
		/// This function allows the user to query the Pinning Memory mode. By default, the Pinning Memory mode is disabled.
		/// </summary>
		/// <param name="handle">CUDA BLASXt context retrieved by <see cref="cublasXtCreate"/></param>
		/// <param name="mode">returned <see cref="PinnedMemoryMode"/></param>
		/// <returns>operation status enum <see cref="Status"/></returns>
		[DllImport(CUBLASXT_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasXtGetPinningMemMode(IntPtr handle, ref PinnedMemoryMode mode);

		/// <summary>
		/// This function allows the user to enable or disable the Pinning Memory mode. See https://docs.nvidia.com/cuda/cublas/index.html#cublasxt_setPinningMemMode for more detail.
		/// </summary>
		/// <param name="handle">CUDA BLASXt context retrieved by <see cref="cublasXtCreate"/></param>
		/// <param name="mode">The <see cref="PinnedMemoryMode"/> to set</param>
		/// <returns>operation status enum <see cref="Status"/></returns>
		[DllImport(CUBLASXT_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasXtSetPinningMemMode(IntPtr handle, PinnedMemoryMode mode);

		/// <summary>
		/// This function allows the user to provide a CPU implementation of the corresponding BLAS routine. This function can be used with the function <see cref="cublasXtSetCpuRatio(IntPtr, XtOperationList, DataType, float)"/> to define an hybrid computation between the CPU and the GPUs.
		/// <br/>See https://docs.nvidia.com/cuda/cublas/index.html#cublasxt_setCpuRoutine for more detail.
		/// </summary>
		/// <param name="handle">CUDA BLASXt context retrieved by <see cref="cublasXtCreate"/></param>
		/// <param name="blasOp">one of the enum <see cref="XtOperationList"/> to indicate the routine to specify</param>
		/// <param name="type">data type to of the routine to specify</param>
		/// <param name="blasFunctor">CPU BLAS routine function pointer</param>
		/// <returns>operation status enum <see cref="Status"/></returns>
		/// <remarks>Currently the hybrid feature is only supported for the <see cref="XtOperationList.GEMM"/> routines.</remarks>
		[DllImport(CUBLASXT_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasXtSetCpuRoutine(IntPtr handle, XtOperationList blasOp, DataType type, IntPtr blasFunctor);

		/// <summary>
		/// This function allows the user to define the percentage of workload that should be done on a CPU in the context of an hybrid computation. This function can be used with the function <see cref="cublasXtSetCpuRoutine(IntPtr, XtOperationList, DataType, IntPtr)"/> to define an hybrid computation between the CPU and the GPUs. By default, no CPU BLAS routine is used.
		/// <br/>See https://docs.nvidia.com/cuda/cublas/index.html#cublasxt_setCpuRatio for more detail.
		/// </summary>
		/// <param name="handle">CUDA BLASXt context retrieved by <see cref="cublasXtCreate"/></param>
		/// <param name="blasOp">one of the enum <see cref="XtOperationList"/> to indicate the routine to specify</param>
		/// <param name="type">data type to of the routine to specify</param>
		/// <param name="ratio">ratio of the CPU BLAS routine</param>
		/// <returns>operation status enum <see cref="Status"/></returns>
		/// <remarks>Currently the hybrid feature is only supported for the <see cref="XtOperationList.GEMM"/> routines.</remarks>
		[DllImport(CUBLASXT_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasXtSetCpuRatio(IntPtr handle, XtOperationList blasOp, DataType type, float ratio);
		#endregion

		#region Math API
		[DllImport(CUBLASXT_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasXtSgemm(IntPtr handle, MatrixOperation transa, MatrixOperation transb, int m, int n, int k, ref float alpha, [In] IntPtr A, int lda, [In] IntPtr B, int ldb, ref float beta, IntPtr C, int ldc);

		[DllImport(CUBLASXT_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasXtDgemm(IntPtr handle, MatrixOperation transa, MatrixOperation transb, int m, int n, int k, ref double alpha, [In] IntPtr A, int lda, [In] IntPtr B, int ldb, ref double beta, IntPtr C, int ldc);

		[DllImport(CUBLASXT_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasXtCgemm(IntPtr handle, MatrixOperation transa, MatrixOperation transb, int m, int n, int k, ref FloatComplex alpha, [In] IntPtr A, int lda, [In] IntPtr B, int ldb, ref FloatComplex beta, IntPtr C, int ldc);

		[DllImport(CUBLASXT_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasXtZgemm(IntPtr handle, MatrixOperation transa, MatrixOperation transb, int m, int n, int k, ref DoubleComplex alpha, [In] IntPtr A, int lda, [In] IntPtr B, int ldb, ref DoubleComplex beta, IntPtr C, int ldc);

		internal delegate Status gemmFunc<T>(IntPtr handle, MatrixOperation transa, MatrixOperation transb, int m, int n, int k, ref T alpha, [In] IntPtr A, int lda, [In] IntPtr B, int ldb, ref T beta, IntPtr C, int ldc);


		[DllImport(CUBLASXT_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasXtChemm(IntPtr handle, SideMode side, MatrixFillMode uplo, int m, int n, ref FloatComplex alpha, [In] IntPtr A, int lda, [In] IntPtr B, int ldb, ref FloatComplex beta, IntPtr C, int ldc);

		[DllImport(CUBLASXT_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasXtZhemm(IntPtr handle, SideMode side, MatrixFillMode uplo, int m, int n, ref DoubleComplex alpha, [In] IntPtr A, int lda, [In] IntPtr B, int ldb, ref DoubleComplex beta, IntPtr C, int ldc);

		[DllImport(CUBLASXT_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasXtSsymm(IntPtr handle, SideMode side, MatrixFillMode uplo, int m, int n, ref float alpha, [In] IntPtr A, int lda, [In] IntPtr B, int ldb, ref float beta, IntPtr C, int ldc);

		[DllImport(CUBLASXT_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasXtDsymm(IntPtr handle, SideMode side, MatrixFillMode uplo, int m, int n, ref double alpha, [In] IntPtr A, int lda, [In] IntPtr B, int ldb, ref double beta, IntPtr C, int ldc);

		[DllImport(CUBLASXT_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasXtCsymm(IntPtr handle, SideMode side, MatrixFillMode uplo, int m, int n, ref FloatComplex alpha, [In] IntPtr A, int lda, [In] IntPtr B, int ldb, ref FloatComplex beta, IntPtr C, int ldc);

		[DllImport(CUBLASXT_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasXtZsymm(IntPtr handle, SideMode side, MatrixFillMode uplo, int m, int n, ref DoubleComplex alpha, [In] IntPtr A, int lda, [In] IntPtr B, int ldb, ref DoubleComplex beta, IntPtr C, int ldc);

		internal delegate Status symmFunc<T>(IntPtr handle, SideMode side, MatrixFillMode uplo, int m, int n, ref T alpha, [In] IntPtr A, int lda, [In] IntPtr B, int ldb, ref T beta, IntPtr C, int ldc);


		[DllImport(CUBLASXT_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasXtSsyrk(IntPtr handle, MatrixFillMode uplo, MatrixOperation op, int n, int k, ref float α, [In] IntPtr A, int lda, ref float β, IntPtr C, int ldc);

		[DllImport(CUBLASXT_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasXtDsyrk(IntPtr handle, MatrixFillMode uplo, MatrixOperation op, int n, int k, ref double α, [In] IntPtr A, int lda, ref double β, IntPtr C, int ldc);

		[DllImport(CUBLASXT_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasXtCsyrk(IntPtr handle, MatrixFillMode uplo, MatrixOperation op, int n, int k, ref FloatComplex α, [In] IntPtr A, int lda, ref FloatComplex β, IntPtr C, int ldc);

		[DllImport(CUBLASXT_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasXtZsyrk(IntPtr handle, MatrixFillMode uplo, MatrixOperation op, int n, int k, ref DoubleComplex α, [In] IntPtr A, int lda, ref DoubleComplex β, IntPtr C, int ldc);


		[DllImport(CUBLASXT_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasXtCherk(IntPtr handle, MatrixFillMode uplo, MatrixOperation op, int n, int k, ref FloatComplex α, [In] IntPtr A, int lda, ref FloatComplex β, IntPtr C, int ldc);

		[DllImport(CUBLASXT_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasXtZherk(IntPtr handle, MatrixFillMode uplo, MatrixOperation op, int n, int k, ref DoubleComplex α, [In] IntPtr A, int lda, ref DoubleComplex β, IntPtr C, int ldc);

		internal delegate Status syrkFunc<T>(IntPtr handle, MatrixFillMode uplo, MatrixOperation op, int n, int k, ref T α, [In] IntPtr A, int lda, ref T β, IntPtr C, int ldc);
		#endregion
	}
}
