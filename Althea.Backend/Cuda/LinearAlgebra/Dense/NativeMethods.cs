using System;
using System.Runtime.InteropServices;

using Althea.LinearAlgebra;
using Althea.NativeTypes;


#pragma warning disable IDE1006
namespace Althea.Backend.Cuda.LinearAlgebra.Dense
{
	/// <summary>
	/// CUDA BLAS library API
	/// </summary>
	public static unsafe class NativeMethods
	{
		/// <summary>
		/// The CUDA BLAS (cuBLAS) library name
		/// </summary>
		public const string CUBLAS_API_DLL_NAME = @"cublas";

		/// <summary>
		/// The custom CUDA library name
		/// </summary>
		public const string CUSTOM_API_DLL_NAME = Storage.NativeMethods.CUSTOM_API_DLL_NAME;

		/// <summary>
		/// The CUDA Solver (cuSOLVER) dense library name
		/// </summary>
		public const string CUSOLVE_API_DLL_NAME = @"cusolver";


		#region utilities
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCreate_v2(out IntPtr handle);
		/// <summary>
		/// This function initializes the CUDA BLAS library and creates a handle to an opaque structure holding the CUDA BLAS library context.
		/// </summary>
		/// <param name="handle">returned CUDA BLAS handle</param>
		/// <returns>operation status enum <see cref="CudaBlasStatus"/></returns>
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCreate(out IntPtr handle);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasDestroy_v2(IntPtr handle);
		/// <summary>
		/// This function releases hardware resources used by the CUBLAS library. This function is usually the last call with a particular handle to the CUBLAS library.
		/// </summary>
		/// <param name="handle">input CUDA BLAS handle</param>
		/// <returns>operation status enum <see cref="CudaBlasStatus"/></returns>
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasDestroy(IntPtr handle);

		/// <summary>
		/// Some routines like <c>cublas&lt;t&gt;symv</c> and <c>cublas&lt;t&gt;hemv</c> have an alternate implementation that use atomics to cumulate results. This implementation is generally significantly faster but can generate results that are not strictly identical from one run to the others. Mathematically, those different results are not significant but when debugging
		/// those differences can be prejudicial. <para/>
		/// This function queries the atomic mode of a specific cuBLAS context.
		/// </summary>
		/// <param name="handle">input CUDA BLAS handle</param>
		/// <param name="mode">returned <see cref="AtomicsMode"/></param>
		/// <returns>operation status enum <see cref="CudaBlasStatus"/></returns>
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasGetAtomicsMode(IntPtr handle, out AtomicsMode mode);

		/// <summary>
		/// Some routines like <c>cublas&lt;t&gt;symv</c> have an alternate implementation that use atomics to cumulate results. This implementation is generally significantly faster but can generate results that are not strictly identical from one run to the others. Mathematically, those different results are not significant but when debugging those differences can be prejudicial.
		/// <para/>This function allows or disallows the usage of atomics in the CUDA BLAS library for all routines which have an alternate implementation.
		/// </summary>
		/// <param name="handle">input CUDA BLAS handle</param>
		/// <param name="mode">the <see cref="AtomicsMode"/> to set</param>
		/// <returns>operation status enum <see cref="CudaBlasStatus"/></returns>
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSetAtomicsMode(IntPtr handle, AtomicsMode mode);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasGetPointerMode_v2(IntPtr handle, out PointerMode mode);
		/// <summary>
		/// This function obtains the pointer mode used by the cuBLAS library.
		/// </summary>
		/// <param name="handle">input CUDA BLAS handle</param>
		/// <param name="mode">returned <see cref="PointerMode"/></param>
		/// <returns>operation status enum <see cref="CudaBlasStatus"/></returns>
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasGetPointerMode(IntPtr handle, out PointerMode mode);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSetPointerMode_v2(IntPtr handle, PointerMode mode);
		/// <summary>
		/// This function sets the pointer mode used by the cuBLAS library.
		/// The default is for the values to be passed by reference on the host.
		/// </summary>
		/// <param name="handle">input CUDA BLAS handle</param>
		/// <param name="mode">the <see cref="PointerMode"/> to set</param>
		/// <returns>operation status enum <see cref="CudaBlasStatus"/></returns>
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSetPointerMode(IntPtr handle, PointerMode mode);
		#endregion


		#region copy
		/// <summary>
		/// This function copies <paramref name="n"/> elements from a vector <paramref name="x"/> in CPU memory space to a vector <paramref name="y"/> in GPU memory space. Elements in both vectors are assumed to have a size of <paramref name="elemSize"/> bytes. The storage spacing between consecutive elements is given by <paramref name="incx"/> and <paramref name="incy"/> respectively.
		/// </summary>
		/// <param name="n">The number of elements to copy</param>
		/// <param name="elemSize">The size of one element in bytes</param>
		/// <param name="x">The array in host memory to copy from</param>
		/// <param name="incx">The spacing between consecutive elements of <paramref name="x"/></param>
		/// <param name="y">The array in device memory to copy to</param>
		/// <param name="incy">The spacing between consecutive elements of <paramref name="y"/></param>
		/// <returns>operation status enum <see cref="CudaBlasStatus"/></returns>
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSetVector(int n, int elemSize, IntPtr x, int incx, IntPtr y, int incy);

		/// <summary>
		/// This function copies <paramref name="n"/> elements from a vector <paramref name="x"/> in GPU memory space to a vector <paramref name="y"/> in CPU memory space. Elements in both vectors are assumed to have a size of <paramref name="elemSize"/> bytes. The storage spacing between consecutive elements is given by <paramref name="incx"/> and <paramref name="incy"/> respectively.
		/// </summary>
		/// <param name="n">The number of elements to copy</param>
		/// <param name="elemSize">The size of one element in bytes</param>
		/// <param name="x">The array in device memory to copy from</param>
		/// <param name="incx">The spacing between consecutive elements of <paramref name="x"/></param>
		/// <param name="y">The array in host memory to copy to</param>
		/// <param name="incy">The spacing between consecutive elements of <paramref name="y"/></param>
		/// <returns>operation status enum <see cref="CudaBlasStatus"/></returns>
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasGetVector(int n, int elemSize, IntPtr x, int incx, IntPtr y, int incy);
		#endregion


		#region level 1
		#region abs max
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasIsamax_v2(IntPtr handle, int n, IntPtr x, int incx, int* result);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasIsamax(IntPtr handle, int n, IntPtr x, int incx, int* result);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasIdamax_v2(IntPtr handle, int n, IntPtr x, int incx, int* result);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasIdamax(IntPtr handle, int n, IntPtr x, int incx, int* result);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasIcamax_v2(IntPtr handle, int n, IntPtr x, int incx, int* result);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasIcamax(IntPtr handle, int n, IntPtr x, int incx, int* result);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasIzamax_v2(IntPtr handle, int n, IntPtr x, int incx, int* result);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasIzamax(IntPtr handle, int n, IntPtr x, int incx, int* result);
		#endregion

		#region abs min
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasIsamin_v2(IntPtr handle, int n, IntPtr x, int incx, int* result);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasIsamin(IntPtr handle, int n, IntPtr x, int incx, int* result);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasIdamin_v2(IntPtr handle, int n, IntPtr x, int incx, int* result);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasIdamin(IntPtr handle, int n, IntPtr x, int incx, int* result);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasIcamin_v2(IntPtr handle, int n, IntPtr x, int incx, int* result);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasIcamin(IntPtr handle, int n, IntPtr x, int incx, int* result);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasIzamin_v2(IntPtr handle, int n, IntPtr x, int incx, int* result);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasIzamin(IntPtr handle, int n, IntPtr x, int incx, int* result);
		#endregion

		#region abs sum
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSasum_v2(IntPtr handle, int n, IntPtr x, int incx, void* result);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSasum(IntPtr handle, int n, IntPtr x, int incx, void* result);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasDasum_v2(IntPtr handle, int n, IntPtr x, int incx, void* result);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasDasum(IntPtr handle, int n, IntPtr x, int incx, void* result);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasScasum_v2(IntPtr handle, int n, IntPtr x, int incx, void* result);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasScasum(IntPtr handle, int n, IntPtr x, int incx, void* result);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasDzasum_v2(IntPtr handle, int n, IntPtr x, int incx, void* result);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasDzasum(IntPtr handle, int n, IntPtr x, int incx, void* result);
		#endregion

		#region vector add
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSaxpy_v2(IntPtr handle, int n, void* α, IntPtr x, int incx, IntPtr y, int incy);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSaxpy(IntPtr handle, int n, void* α, IntPtr x, int incx, IntPtr y, int incy);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasDaxpy_v2(IntPtr handle, int n, void* α, IntPtr x, int incx, IntPtr y, int incy);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasDaxpy(IntPtr handle, int n, void* α, IntPtr x, int incx, IntPtr y, int incy);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCaxpy_v2(IntPtr handle, int n, void* α, IntPtr x, int incx, IntPtr y, int incy);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCaxpy(IntPtr handle, int n, void* α, IntPtr x, int incx, IntPtr y, int incy);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZaxpy_v2(IntPtr handle, int n, void* α, IntPtr x, int incx, IntPtr y, int incy);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZaxpy(IntPtr handle, int n, void* α, IntPtr x, int incx, IntPtr y, int incy);
		#endregion

		#region vector dot
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSdot_v2(IntPtr handle, int n, IntPtr x, int incx, IntPtr y, int incy, void* result);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSdot(IntPtr handle, int n, IntPtr x, int incx, IntPtr y, int incy, void* result);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasDdot_v2(IntPtr handle, int n, IntPtr x, int incx, IntPtr y, int incy, void* result);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasDdot(IntPtr handle, int n, IntPtr x, int incx, IntPtr y, int incy, void* result);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCdotc_v2(IntPtr handle, int n, IntPtr x, int incx, IntPtr y, int incy, void* result);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCdotc(IntPtr handle, int n, IntPtr x, int incx, IntPtr y, int incy, void* result);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCdotu_v2(IntPtr handle, int n, IntPtr x, int incx, IntPtr y, int incy, void* result);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCdotu(IntPtr handle, int n, IntPtr x, int incx, IntPtr y, int incy, void* result);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZdotc_v2(IntPtr handle, int n, IntPtr x, int incx, IntPtr y, int incy, void* result);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZdotc(IntPtr handle, int n, IntPtr x, int incx, IntPtr y, int incy, void* result);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZdotu_v2(IntPtr handle, int n, IntPtr x, int incx, IntPtr y, int incy, void* result);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZdotu(IntPtr handle, int n, IntPtr x, int incx, IntPtr y, int incy, void* result);
		#endregion

		#region vector norm
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSnrm2_v2(IntPtr handle, int n, IntPtr x, int incx, void* result);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSnrm2(IntPtr handle, int n, IntPtr x, int incx, void* result);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasDnrm2_v2(IntPtr handle, int n, IntPtr x, int incx, void* result);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasDnrm2(IntPtr handle, int n, IntPtr x, int incx, void* result);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasScnrm2_v2(IntPtr handle, int n, IntPtr x, int incx, void* result);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasScnrm2(IntPtr handle, int n, IntPtr x, int incx, void* result);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasDznrm2_v2(IntPtr handle, int n, IntPtr x, int incx, void* result);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasDznrm2(IntPtr handle, int n, IntPtr x, int incx, void* result);
		#endregion

		#region scale
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSscal_v2(IntPtr handle, int n, void* α, IntPtr x, int incx);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSscal(IntPtr handle, int n, void* α, IntPtr x, int incx);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasDscal_v2(IntPtr handle, int n, void* α, IntPtr x, int incx);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasDscal(IntPtr handle, int n, void* α, IntPtr x, int incx);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCscal_v2(IntPtr handle, int n, void* α, IntPtr x, int incx);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCscal(IntPtr handle, int n, void* α, IntPtr x, int incx);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZscal_v2(IntPtr handle, int n, void* α, IntPtr x, int incx);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZscal(IntPtr handle, int n, void* α, IntPtr x, int incx);
		#endregion

		#region copy
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasScopy_v2(IntPtr handle, int n, IntPtr x, int incx, IntPtr y, int incy);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasScopy(IntPtr handle, int n, IntPtr x, int incx, IntPtr y, int incy);

		/// <returns></returns>
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasDcopy_v2(IntPtr handle, int n, IntPtr x, int incx, IntPtr y, int incy);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasDcopy(IntPtr handle, int n, IntPtr x, int incx, IntPtr y, int incy);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCcopy_v2(IntPtr handle, int n, IntPtr x, int incx, IntPtr y, int incy);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCcopy(IntPtr handle, int n, IntPtr x, int incx, IntPtr y, int incy);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZcopy_v2(IntPtr handle, int n, IntPtr x, int incx, IntPtr y, int incy);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZcopy(IntPtr handle, int n, IntPtr x, int incx, IntPtr y, int incy);
		#endregion
		#endregion


		#region level 2
		#region general matrix vector multiply
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSgemv_v2(IntPtr handle, CuBlasOperation op, int m, int n, void* α, IntPtr A, int lda, IntPtr x, int incx, void* β, IntPtr y, int incy);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSgemv(IntPtr handle, CuBlasOperation op, int m, int n, void* α, IntPtr A, int lda, IntPtr x, int incx, void* β, IntPtr y, int incy);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasDgemv_v2(IntPtr handle, CuBlasOperation op, int m, int n, void* α, IntPtr A, int lda, IntPtr x, int incx, void* β, IntPtr y, int incy);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasDgemv(IntPtr handle, CuBlasOperation op, int m, int n, void* α, IntPtr A, int lda, IntPtr x, int incx, void* β, IntPtr y, int incy);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCgemv_v2(IntPtr handle, CuBlasOperation op, int m, int n, void* α, IntPtr A, int lda, IntPtr x, int incx, void* β, IntPtr y, int incy);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCgemv(IntPtr handle, CuBlasOperation op, int m, int n, void* α, IntPtr A, int lda, IntPtr x, int incx, void* β, IntPtr y, int incy);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZgemv_v2(IntPtr handle, CuBlasOperation op, int m, int n, void* α, IntPtr A, int lda, IntPtr x, int incx, void* β, IntPtr y, int incy);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZgemv(IntPtr handle, CuBlasOperation op, int m, int n, void* α, IntPtr A, int lda, IntPtr x, int incx, void* β, IntPtr y, int incy);
		#endregion

		#region symmetric matrix vector multiply
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSsymv_v2(IntPtr handle, MatrixFillMode uplo, int n, void* α, IntPtr A, int lda, IntPtr x, int incx, void* β, IntPtr y, int incy);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSsymv(IntPtr handle, MatrixFillMode uplo, int n, void* α, IntPtr A, int lda, IntPtr x, int incx, void* β, IntPtr y, int incy);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasDsymv_v2(IntPtr handle, MatrixFillMode uplo, int n, void* α, IntPtr A, int lda, IntPtr x, int incx, void* β, IntPtr y, int incy);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasDsymv(IntPtr handle, MatrixFillMode uplo, int n, void* α, IntPtr A, int lda, IntPtr x, int incx, void* β, IntPtr y, int incy);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCsymv_v2(IntPtr handle, MatrixFillMode uplo, int n, void* α, IntPtr A, int lda, IntPtr x, int incx, void* β, IntPtr y, int incy);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCsymv(IntPtr handle, MatrixFillMode uplo, int n, void* α, IntPtr A, int lda, IntPtr x, int incx, void* β, IntPtr y, int incy);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZsymv_v2(IntPtr handle, MatrixFillMode uplo, int n, void* α, IntPtr A, int lda, IntPtr x, int incx, void* β, IntPtr y, int incy);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZsymv(IntPtr handle, MatrixFillMode uplo, int n, void* α, IntPtr A, int lda, IntPtr x, int incx, void* β, IntPtr y, int incy);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasChemv_v2(IntPtr handle, MatrixFillMode uplo, int n, void* α, IntPtr A, int lda, IntPtr x, int incx, void* β, IntPtr y, int incy);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasChemv(IntPtr handle, MatrixFillMode uplo, int n, void* α, IntPtr A, int lda, IntPtr x, int incx, void* β, IntPtr y, int incy);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZhemv_v2(IntPtr handle, MatrixFillMode uplo, int n, void* α, IntPtr A, int lda, IntPtr x, int incx, void* β, IntPtr y, int incy);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZhemv(IntPtr handle, MatrixFillMode uplo, int n, void* α, IntPtr A, int lda, IntPtr x, int incx, void* β, IntPtr y, int incy);
		#endregion

		#region symmetric matrix vector multiply
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasStrmv_v2(IntPtr handle, MatrixFillMode uplo, CuBlasOperation op, DiagType diag, int n, IntPtr A, int lda, IntPtr x, int incx);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasStrmv(IntPtr handle, MatrixFillMode uplo, CuBlasOperation op, DiagType diag, int n, IntPtr A, int lda, IntPtr x, int incx);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasDtrmv_v2(IntPtr handle, MatrixFillMode uplo, CuBlasOperation op, DiagType diag, int n, IntPtr A, int lda, IntPtr x, int incx);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasDtrmv(IntPtr handle, MatrixFillMode uplo, CuBlasOperation op, DiagType diag, int n, IntPtr A, int lda, IntPtr x, int incx);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCtrmv_v2(IntPtr handle, MatrixFillMode uplo, CuBlasOperation op, DiagType diag, int n, IntPtr A, int lda, IntPtr x, int incx);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCtrmv(IntPtr handle, MatrixFillMode uplo, CuBlasOperation op, DiagType diag, int n, IntPtr A, int lda, IntPtr x, int incx);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZtrmv_v2(IntPtr handle, MatrixFillMode uplo, CuBlasOperation op, DiagType diag, int n, IntPtr A, int lda, IntPtr x, int incx);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZtrmv(IntPtr handle, MatrixFillMode uplo, CuBlasOperation op, DiagType diag, int n, IntPtr A, int lda, IntPtr x, int incx);
		#endregion

		#region general rank 1 update
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSger_v2(IntPtr handle, int m, int n, void* α, IntPtr x, int incx, IntPtr y, int incy, IntPtr A, int lda);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSger(IntPtr handle, int m, int n, void* α, IntPtr x, int incx, IntPtr y, int incy, IntPtr A, int lda);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasDger_v2(IntPtr handle, int m, int n, void* α, IntPtr x, int incx, IntPtr y, int incy, IntPtr A, int lda);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasDger(IntPtr handle, int m, int n, void* α, IntPtr x, int incx, IntPtr y, int incy, IntPtr A, int lda);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCgeru_v2(IntPtr handle, int m, int n, void* α, IntPtr x, int incx, IntPtr y, int incy, IntPtr A, int lda);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCgeru(IntPtr handle, int m, int n, void* α, IntPtr x, int incx, IntPtr y, int incy, IntPtr A, int lda);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCgerc_v2(IntPtr handle, int m, int n, void* α, IntPtr x, int incx, IntPtr y, int incy, IntPtr A, int lda);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCgerc(IntPtr handle, int m, int n, void* α, IntPtr x, int incx, IntPtr y, int incy, IntPtr A, int lda);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZgeru_v2(IntPtr handle, int m, int n, void* α, IntPtr x, int incx, IntPtr y, int incy, IntPtr A, int lda);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZgeru(IntPtr handle, int m, int n, void* α, IntPtr x, int incx, IntPtr y, int incy, IntPtr A, int lda);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZgerc_v2(IntPtr handle, int m, int n, void* α, IntPtr x, int incx, IntPtr y, int incy, IntPtr A, int lda);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZgerc(IntPtr handle, int m, int n, void* α, IntPtr x, int incx, IntPtr y, int incy, IntPtr A, int lda);
		#endregion

		#region symmetric rank 1 update
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSsyr_v2(IntPtr handle, MatrixFillMode fillMode, int n, void* α, IntPtr x, int incx, IntPtr A, int lda);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSsyr(IntPtr handle, MatrixFillMode fillMode, int n, void* α, IntPtr x, int incx, IntPtr A, int lda);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasDsyr_v2(IntPtr handle, MatrixFillMode fillMode, int n, void* α, IntPtr x, int incx, IntPtr A, int lda);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasDsyr(IntPtr handle, MatrixFillMode fillMode, int n, void* α, IntPtr x, int incx, IntPtr A, int lda);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCsyr_v2(IntPtr handle, MatrixFillMode fillMode, int n, void* α, IntPtr x, int incx, IntPtr A, int lda);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCsyr(IntPtr handle, MatrixFillMode fillMode, int n, void* α, IntPtr x, int incx, IntPtr A, int lda);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZsyr_v2(IntPtr handle, MatrixFillMode fillMode, int n, void* α, IntPtr x, int incx, IntPtr A, int lda);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZsyr(IntPtr handle, MatrixFillMode fillMode, int n, void* α, IntPtr x, int incx, IntPtr A, int lda);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCher_v2(IntPtr handle, MatrixFillMode fillMode, int n, void* α, IntPtr x, int incx, IntPtr A, int lda);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCher(IntPtr handle, MatrixFillMode fillMode, int n, void* α, IntPtr x, int incx, IntPtr A, int lda);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZher_v2(IntPtr handle, MatrixFillMode fillMode, int n, void* α, IntPtr x, int incx, IntPtr A, int lda);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZher(IntPtr handle, MatrixFillMode fillMode, int n, void* α, IntPtr x, int incx, IntPtr A, int lda);
		#endregion

		#region symmetric rank 2 update
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSsyr2_v2(IntPtr handle, MatrixFillMode fillMode, int n, void* α, IntPtr x, int incx, IntPtr y, int incy, IntPtr A, int lda);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSsyr2(IntPtr handle, MatrixFillMode fillMode, int n, void* α, IntPtr x, int incx, IntPtr y, int incy, IntPtr A, int lda);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasDsyr2_v2(IntPtr handle, MatrixFillMode fillMode, int n, void* α, IntPtr x, int incx, IntPtr y, int incy, IntPtr A, int lda);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasDsyr2(IntPtr handle, MatrixFillMode fillMode, int n, void* α, IntPtr x, int incx, IntPtr y, int incy, IntPtr A, int lda);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCsyr2_v2(IntPtr handle, MatrixFillMode fillMode, int n, void* α, IntPtr x, int incx, IntPtr y, int incy, IntPtr A, int lda);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCsyr2(IntPtr handle, MatrixFillMode fillMode, int n, void* α, IntPtr x, int incx, IntPtr y, int incy, IntPtr A, int lda);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZsyr2_v2(IntPtr handle, MatrixFillMode fillMode, int n, void* α, IntPtr x, int incx, IntPtr y, int incy, IntPtr A, int lda);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZsyr2(IntPtr handle, MatrixFillMode fillMode, int n, void* α, IntPtr x, int incx, IntPtr y, int incy, IntPtr A, int lda);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCher2_v2(IntPtr handle, MatrixFillMode fillMode, int n, void* α, IntPtr x, int incx, IntPtr y, int incy, IntPtr A, int lda);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCher2(IntPtr handle, MatrixFillMode fillMode, int n, void* α, IntPtr x, int incx, IntPtr y, int incy, IntPtr A, int lda);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZher2_v2(IntPtr handle, MatrixFillMode fillMode, int n, void* α, IntPtr x, int incx, IntPtr y, int incy, IntPtr A, int lda);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZher2(IntPtr handle, MatrixFillMode fillMode, int n, void* α, IntPtr x, int incx, IntPtr y, int incy, IntPtr A, int lda);
		#endregion

		#endregion


		#region level 3
		#region general matrix multiply
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSgemm_v2(IntPtr handle, CuBlasOperation opA, CuBlasOperation opB, int m, int n, int k, void* α, IntPtr A, int lda, IntPtr B, int ldb, void* β, IntPtr C, int ldc);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSgemm(IntPtr handle, CuBlasOperation opA, CuBlasOperation opB, int m, int n, int k, void* α, IntPtr A, int lda, IntPtr B, int ldb, void* β, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasDgemm_v2(IntPtr handle, CuBlasOperation opA, CuBlasOperation opB, int m, int n, int k, void* α, IntPtr A, int lda, IntPtr B, int ldb, void* β, IntPtr C, int ldc);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasDgemm(IntPtr handle, CuBlasOperation opA, CuBlasOperation opB, int m, int n, int k, void* α, IntPtr A, int lda, IntPtr B, int ldb, void* β, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCgemm_v2(IntPtr handle, CuBlasOperation opA, CuBlasOperation opB, int m, int n, int k, void* α, IntPtr A, int lda, IntPtr B, int ldb, void* β, IntPtr C, int ldc);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCgemm(IntPtr handle, CuBlasOperation opA, CuBlasOperation opB, int m, int n, int k, void* α, IntPtr A, int lda, IntPtr B, int ldb, void* β, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZgemm_v2(IntPtr handle, CuBlasOperation opA, CuBlasOperation opB, int m, int n, int k, void* α, IntPtr A, int lda, IntPtr B, int ldb, void* β, IntPtr C, int ldc);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZgemm(IntPtr handle, CuBlasOperation opA, CuBlasOperation opB, int m, int n, int k, void* α, IntPtr A, int lda, IntPtr B, int ldb, void* β, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCgemm3m(IntPtr handle, CuBlasOperation opA, CuBlasOperation opB, int m, int n, int k, void* α, IntPtr A, int lda, IntPtr B, int ldb, void* β, IntPtr C, int ldc);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZgemm3m(IntPtr handle, CuBlasOperation opA, CuBlasOperation opB, int m, int n, int k, void* α, IntPtr A, int lda, IntPtr B, int ldb, void* β, IntPtr C, int ldc);
		#endregion

		#region symmetric matrix multiply
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSsymm_v2(IntPtr handle, SideMode side, MatrixFillMode uplo, int m, int n, void* α, IntPtr A, int lda, IntPtr B, int ldb, void* β, IntPtr C, int ldc);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSsymm(IntPtr handle, SideMode side, MatrixFillMode uplo, int m, int n, void* α, IntPtr A, int lda, IntPtr B, int ldb, void* β, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasDsymm_v2(IntPtr handle, SideMode side, MatrixFillMode uplo, int m, int n, void* α, IntPtr A, int lda, IntPtr B, int ldb, void* β, IntPtr C, int ldc);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasDsymm(IntPtr handle, SideMode side, MatrixFillMode uplo, int m, int n, void* α, IntPtr A, int lda, IntPtr B, int ldb, void* β, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCsymm_v2(IntPtr handle, SideMode side, MatrixFillMode uplo, int m, int n, void* α, IntPtr A, int lda, IntPtr B, int ldb, void* β, IntPtr C, int ldc);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCsymm(IntPtr handle, SideMode side, MatrixFillMode uplo, int m, int n, void* α, IntPtr A, int lda, IntPtr B, int ldb, void* β, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZsymm_v2(IntPtr handle, SideMode side, MatrixFillMode uplo, int m, int n, void* α, IntPtr A, int lda, IntPtr B, int ldb, void* β, IntPtr C, int ldc);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZsymm(IntPtr handle, SideMode side, MatrixFillMode uplo, int m, int n, void* α, IntPtr A, int lda, IntPtr B, int ldb, void* β, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasChemm_v2(IntPtr handle, SideMode side, MatrixFillMode uplo, int m, int n, void* α, IntPtr A, int lda, IntPtr B, int ldb, void* β, IntPtr C, int ldc);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasChemm(IntPtr handle, SideMode side, MatrixFillMode uplo, int m, int n, void* α, IntPtr A, int lda, IntPtr B, int ldb, void* β, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZhemm_v2(IntPtr handle, SideMode side, MatrixFillMode uplo, int m, int n, void* α, IntPtr A, int lda, IntPtr B, int ldb, void* β, IntPtr C, int ldc);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZhemm(IntPtr handle, SideMode side, MatrixFillMode uplo, int m, int n, void* α, IntPtr A, int lda, IntPtr B, int ldb, void* β, IntPtr C, int ldc);
		#endregion

		#region symmetric rank k update
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSsyrk_v2(IntPtr handle, MatrixFillMode uplo, CuBlasOperation op, int n, int k, void* α, IntPtr A, int lda, void* β, IntPtr C, int ldc);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSsyrk(IntPtr handle, MatrixFillMode uplo, CuBlasOperation op, int n, int k, void* α, IntPtr A, int lda, void* β, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasDsyrk_v2(IntPtr handle, MatrixFillMode uplo, CuBlasOperation op, int n, int k, void* α, IntPtr A, int lda, void* β, IntPtr C, int ldc);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasDsyrk(IntPtr handle, MatrixFillMode uplo, CuBlasOperation op, int n, int k, void* α, IntPtr A, int lda, void* β, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCsyrk_v2(IntPtr handle, MatrixFillMode uplo, CuBlasOperation op, int n, int k, void* α, IntPtr A, int lda, void* β, IntPtr C, int ldc);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCsyrk(IntPtr handle, MatrixFillMode uplo, CuBlasOperation op, int n, int k, void* α, IntPtr A, int lda, void* β, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZsyrk_v2(IntPtr handle, MatrixFillMode uplo, CuBlasOperation op, int n, int k, void* α, IntPtr A, int lda, void* β, IntPtr C, int ldc);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZsyrk(IntPtr handle, MatrixFillMode uplo, CuBlasOperation op, int n, int k, void* α, IntPtr A, int lda, void* β, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCherk_v2(IntPtr handle, MatrixFillMode uplo, CuBlasOperation op, int n, int k, void* α, IntPtr A, int lda, void* β, IntPtr C, int ldc);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCherk(IntPtr handle, MatrixFillMode uplo, CuBlasOperation op, int n, int k, void* α, IntPtr A, int lda, void* β, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZherk_v2(IntPtr handle, MatrixFillMode uplo, CuBlasOperation op, int n, int k, void* α, IntPtr A, int lda, void* β, IntPtr C, int ldc);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZherk(IntPtr handle, MatrixFillMode uplo, CuBlasOperation op, int n, int k, void* α, IntPtr A, int lda, void* β, IntPtr C, int ldc);
		#endregion

		#region triangular matrix solve
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasStrsm(IntPtr handle, SideMode side, MatrixFillMode uplo, CuBlasOperation op, DiagType diag, int m, int n, void* α, IntPtr A, int lda, IntPtr B, int ldb);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasStrsm_v2(IntPtr handle, SideMode side, MatrixFillMode uplo, CuBlasOperation op, DiagType diag, int m, int n, void* α, IntPtr A, int lda, IntPtr B, int ldb);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasDtrsm(IntPtr handle, SideMode side, MatrixFillMode uplo, CuBlasOperation op, DiagType diag, int m, int n, void* α, IntPtr A, int lda, IntPtr B, int ldb);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasDtrsm_v2(IntPtr handle, SideMode side, MatrixFillMode uplo, CuBlasOperation op, DiagType diag, int m, int n, void* α, IntPtr A, int lda, IntPtr B, int ldb);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCtrsm(IntPtr handle, SideMode side, MatrixFillMode uplo, CuBlasOperation op, DiagType diag, int m, int n, void* α, IntPtr A, int lda, IntPtr B, int ldb);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCtrsm_v2(IntPtr handle, SideMode side, MatrixFillMode uplo, CuBlasOperation op, DiagType diag, int m, int n, void* α, IntPtr A, int lda, IntPtr B, int ldb);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZtrsm(IntPtr handle, SideMode side, MatrixFillMode uplo, CuBlasOperation op, DiagType diag, int m, int n, void* α, IntPtr A, int lda, IntPtr B, int ldb);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZtrsm_v2(IntPtr handle, SideMode side, MatrixFillMode uplo, CuBlasOperation op, DiagType diag, int m, int n, void* α, IntPtr A, int lda, IntPtr B, int ldb);
		#endregion

		#region triangular matrix solve
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasStrmm(IntPtr handle, SideMode side, MatrixFillMode uplo, CuBlasOperation op, DiagType diag, int m, int n, void* α, IntPtr A, int lda, IntPtr B, int ldb, IntPtr C, int ldc);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasStrmm_v2(IntPtr handle, SideMode side, MatrixFillMode uplo, CuBlasOperation op, DiagType diag, int m, int n, void* α, IntPtr A, int lda, IntPtr B, int ldb, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasDtrmm(IntPtr handle, SideMode side, MatrixFillMode uplo, CuBlasOperation op, DiagType diag, int m, int n, void* α, IntPtr A, int lda, IntPtr B, int ldb, IntPtr C, int ldc);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasDtrmm_v2(IntPtr handle, SideMode side, MatrixFillMode uplo, CuBlasOperation op, DiagType diag, int m, int n, void* α, IntPtr A, int lda, IntPtr B, int ldb, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCtrmm(IntPtr handle, SideMode side, MatrixFillMode uplo, CuBlasOperation op, DiagType diag, int m, int n, void* α, IntPtr A, int lda, IntPtr B, int ldb, IntPtr C, int ldc);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCtrmm_v2(IntPtr handle, SideMode side, MatrixFillMode uplo, CuBlasOperation op, DiagType diag, int m, int n, void* α, IntPtr A, int lda, IntPtr B, int ldb, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZtrmm(IntPtr handle, SideMode side, MatrixFillMode uplo, CuBlasOperation op, DiagType diag, int m, int n, void* α, IntPtr A, int lda, IntPtr B, int ldb, IntPtr C, int ldc);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZtrmm_v2(IntPtr handle, SideMode side, MatrixFillMode uplo, CuBlasOperation op, DiagType diag, int m, int n, void* α, IntPtr A, int lda, IntPtr B, int ldb, IntPtr C, int ldc);
		#endregion

		#region symmetric rank-2k update
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSsyr2k_v2(IntPtr handle, MatrixFillMode uplo, CuBlasOperation op, int n, int k, void* α, IntPtr A, int lda, IntPtr B, int ldb, void* β, IntPtr C, int ldc);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSsyr2k(IntPtr handle, MatrixFillMode uplo, CuBlasOperation op, int n, int k, void* α, IntPtr A, int lda, IntPtr B, int ldb, void* β, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasDsyr2k_v2(IntPtr handle, MatrixFillMode uplo, CuBlasOperation op, int n, int k, void* α, IntPtr A, int lda, IntPtr B, int ldb, void* β, IntPtr C, int ldc);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasDsyr2k(IntPtr handle, MatrixFillMode uplo, CuBlasOperation op, int n, int k, void* α, IntPtr A, int lda, IntPtr B, int ldb, void* β, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCsyr2k_v2(IntPtr handle, MatrixFillMode uplo, CuBlasOperation op, int n, int k, void* α, IntPtr A, int lda, IntPtr B, int ldb, void* β, IntPtr C, int ldc);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCsyr2k(IntPtr handle, MatrixFillMode uplo, CuBlasOperation op, int n, int k, void* α, IntPtr A, int lda, IntPtr B, int ldb, void* β, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZsyr2k_v2(IntPtr handle, MatrixFillMode uplo, CuBlasOperation op, int n, int k, void* α, IntPtr A, int lda, IntPtr B, int ldb, void* β, IntPtr C, int ldc);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZsyr2k(IntPtr handle, MatrixFillMode uplo, CuBlasOperation op, int n, int k, void* α, IntPtr A, int lda, IntPtr B, int ldb, void* β, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCher2k_v2(IntPtr handle, MatrixFillMode uplo, CuBlasOperation op, int n, int k, void* α, IntPtr A, int lda, IntPtr B, int ldb, void* β, IntPtr C, int ldc);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCher2k(IntPtr handle, MatrixFillMode uplo, CuBlasOperation op, int n, int k, void* α, IntPtr A, int lda, IntPtr B, int ldb, void* β, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZher2k_v2(IntPtr handle, MatrixFillMode uplo, CuBlasOperation op, int n, int k, void* α, IntPtr A, int lda, IntPtr B, int ldb, void* β, IntPtr C, int ldc);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZher2k(IntPtr handle, MatrixFillMode uplo, CuBlasOperation op, int n, int k, void* α, IntPtr A, int lda, IntPtr B, int ldb, void* β, IntPtr C, int ldc);
		#endregion

		#region symmetric rank-k update variant
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSsyrkx_v2(IntPtr handle, MatrixFillMode uplo, CuBlasOperation op, int n, int k, void* α, IntPtr A, int lda, IntPtr B, int ldb, void* β, IntPtr C, int ldc);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSsyrkx(IntPtr handle, MatrixFillMode uplo, CuBlasOperation op, int n, int k, void* α, IntPtr A, int lda, IntPtr B, int ldb, void* β, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasDsyrkx_v2(IntPtr handle, MatrixFillMode uplo, CuBlasOperation op, int n, int k, void* α, IntPtr A, int lda, IntPtr B, int ldb, void* β, IntPtr C, int ldc);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasDsyrkx(IntPtr handle, MatrixFillMode uplo, CuBlasOperation op, int n, int k, void* α, IntPtr A, int lda, IntPtr B, int ldb, void* β, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCsyrkx_v2(IntPtr handle, MatrixFillMode uplo, CuBlasOperation op, int n, int k, void* α, IntPtr A, int lda, IntPtr B, int ldb, void* β, IntPtr C, int ldc);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCsyrkx(IntPtr handle, MatrixFillMode uplo, CuBlasOperation op, int n, int k, void* α, IntPtr A, int lda, IntPtr B, int ldb, void* β, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZsyrkx_v2(IntPtr handle, MatrixFillMode uplo, CuBlasOperation op, int n, int k, void* α, IntPtr A, int lda, IntPtr B, int ldb, void* β, IntPtr C, int ldc);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZsyrkx(IntPtr handle, MatrixFillMode uplo, CuBlasOperation op, int n, int k, void* α, IntPtr A, int lda, IntPtr B, int ldb, void* β, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCherkx_v2(IntPtr handle, MatrixFillMode uplo, CuBlasOperation op, int n, int k, void* α, IntPtr A, int lda, IntPtr B, int ldb, void* β, IntPtr C, int ldc);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCherkx(IntPtr handle, MatrixFillMode uplo, CuBlasOperation op, int n, int k, void* α, IntPtr A, int lda, IntPtr B, int ldb, void* β, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZherkx_v2(IntPtr handle, MatrixFillMode uplo, CuBlasOperation op, int n, int k, void* α, IntPtr A, int lda, IntPtr B, int ldb, void* β, IntPtr C, int ldc);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZherkx(IntPtr handle, MatrixFillMode uplo, CuBlasOperation op, int n, int k, void* α, IntPtr A, int lda, IntPtr B, int ldb, void* β, IntPtr C, int ldc);
		#endregion
		#endregion


		#region BLAS like
		#region matrix add
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSgeam(IntPtr handle, CuBlasOperation opA, CuBlasOperation opB, int m, int n, void* α, IntPtr A, int lda, void* β, IntPtr B, int ldb, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasDgeam(IntPtr handle, CuBlasOperation opA, CuBlasOperation opB, int m, int n, void* α, IntPtr A, int lda, void* β, IntPtr B, int ldb, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCgeam(IntPtr handle, CuBlasOperation opA, CuBlasOperation opB, int m, int n, void* α, IntPtr A, int lda, void* β, IntPtr B, int ldb, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZgeam(IntPtr handle, CuBlasOperation opA, CuBlasOperation opB, int m, int n, void* α, IntPtr A, int lda, void* β, IntPtr B, int ldb, IntPtr C, int ldc);
		#endregion

		#region diagonal matrix multiply
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSdgmm(IntPtr handle, SideMode mode, int m, int n, IntPtr A, int lda, IntPtr x, int incx, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasDdgmm(IntPtr handle, SideMode mode, int m, int n, IntPtr A, int lda, IntPtr x, int incx, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCdgmm(IntPtr handle, SideMode mode, int m, int n, IntPtr A, int lda, IntPtr x, int incx, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZdgmm(IntPtr handle, SideMode mode, int m, int n, IntPtr A, int lda, IntPtr x, int incx, IntPtr C, int ldc);
		#endregion


		#region extension
		// do not support BrainHalf
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasAxpyEx(IntPtr handle, int n, void* alpha, CudaDataType alphaType, IntPtr x, CudaDataType xType, int incx, IntPtr y, CudaDataType yType, int incy, CudaDataType executiontype);

		// do not support BrainHalf
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasDotEx(IntPtr handle, int n, IntPtr x, CudaDataType xType, int incx, IntPtr y, CudaDataType yType, int incy, void* reslut, CudaDataType resultType, CudaDataType executiontype);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasDotcEx(IntPtr handle, int n, IntPtr x, CudaDataType xType, int incx, IntPtr y, CudaDataType yType, int incy, void* reslut, CudaDataType resultType, CudaDataType executiontype);

		// do not support BrainHalf
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasScalEx(IntPtr handle, int n, void* alpha, CudaDataType alphaType, IntPtr x, CudaDataType xType, int incx, CudaDataType executiontype);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasGemmEx(IntPtr handle, CuBlasOperation opA, CuBlasOperation opB, int m, int n, int k, void* alpha, IntPtr A, CudaDataType Atype, int lda, IntPtr B, CudaDataType Btype, int ldb, void* beta, IntPtr C, CudaDataType Ctype, int ldc, ComputeType computeType, GemmAlgorithm algo);
		#endregion
		#endregion


		#region custom
		/// <summary>
		/// Multiply or divide the vector <paramref name="a"/> by <paramref name="b"/> in-place
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/> and <paramref name="b"/></param>
		/// <param name="a">The vector to be in-place multiplied or divided of <paramref name="type"/></param>
		/// <param name="b">The vector to multiply or divide <paramref name="a"/> of <paramref name="type"/></param>
		/// <param name="N">The number of elements to be operated</param>
		/// <param name="strideA">The spacing between consecutive elements of <paramref name="a"/></param>
		/// <param name="strideB">The spacing between consecutive elements of <paramref name="b"/></param>
		/// <param name="multiply">Perform multiply or divide</param>
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern void vecsMulDiv(DataType type, IntPtr a, IntPtr b, long N, int strideA, int strideB, bool multiply);

		/// <summary>
		/// Add the vector <paramref name="a"/> scaled by <paramref name="scalar"/> to <paramref name="b"/> in-place
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/> and <paramref name="b"/></param>
		/// <param name="scalar">The scalar to multiply of <paramref name="type"/></param>
		/// <param name="a">The vector to add of <paramref name="type"/></param>
		/// <param name="b">The vector to be in-place modified of <paramref name="type"/></param>
		/// <param name="N">The number of elements to be operated</param>
		/// <param name="strideA">The spacing between consecutive elements of <paramref name="a"/></param>
		/// <param name="strideB">The spacing between consecutive elements of <paramref name="b"/></param>
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern void vecsAdd(DataType type, void* scalar, IntPtr a, IntPtr b, long N, int strideA, int strideB);

		/// <summary>
		/// Check whether the two vectors <paramref name="a"/> and <paramref name="b"/> are element-wise equal
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/> and <paramref name="b"/></param>
		/// <param name="a">The first vector to compare of <paramref name="type"/></param>
		/// <param name="b">The second vector to compare of <paramref name="type"/></param>
		/// <param name="N">The number of elements to be operated</param>
		/// <param name="strideA">The spacing between consecutive elements of <paramref name="a"/></param>
		/// <param name="strideB">The spacing between consecutive elements of <paramref name="b"/></param>
		/// <returns>The two vectors are element-wise equal</returns>
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern bool vecsEq(DataType type, IntPtr a, IntPtr b, long N, int strideA, int strideB);

		/// <summary>
		/// In-place exponentiate the vector <paramref name="a"/> by a scalar exponent <paramref name="p"/>
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/></param>
		/// <param name="a">The vector to be in-place modified of <paramref name="type"/></param>
		/// <param name="p">The pointer to the scalar exponent of <paramref name="type"/></param>
		/// <param name="N">The number of elements to be operated</param>
		/// <param name="stride">The spacing between consecutive elements of <paramref name="a"/></param>
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern void vecPowSameType(DataType type, IntPtr a, void* p, long N, int stride);

		/// <summary>
		/// In-place exponentiate the vector <paramref name="a"/> by a scalar exponent <paramref name="p"/>
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/> (must be a complex type)</param>
		/// <param name="a">The vector to be in-place modified of <paramref name="type"/></param>
		/// <param name="p">The pointer to the scalar exponent of <paramref name="type"/>'s real corresponding type</param>
		/// <param name="N">The number of elements to be operated</param>
		/// <param name="stride">The spacing between consecutive elements of <paramref name="a"/></param>
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern void vecPowRealType(DataType type, IntPtr a, void* p, long N, int stride);

		/// <summary>
		/// In-place conjugate the vector <paramref name="a"/>
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/></param>
		/// <param name="a">The vector to be in-place modified of <paramref name="type"/></param>
		/// <param name="N">The number of elements to be operated</param>
		/// <param name="stride">The spacing between consecutive elements of <paramref name="a"/></param>
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern void vecConj(DataType type, IntPtr a, long N, int stride);

		/// <summary>
		/// Convert the <paramref name="src"/> vector of <paramref name="srcType"/> to the <paramref name="dst"/> vector of <paramref name="dstType"/>
		/// </summary>
		/// <param name="srcType">The <see cref="DataType"/> of <paramref name="src"/></param>
		/// <param name="dstType">The <see cref="DataType"/> of <paramref name="dst"/></param>
		/// <param name="src">The source vector of <paramref name="srcType"/></param>
		/// <param name="dst">The destination vector of <paramref name="dstType"/></param>
		/// <param name="N">The number of elements to be operated</param>
		/// <param name="strideSrc">The spacing between consecutive elements of <paramref name="src"/></param>
		/// <param name="strideDst">The spacing between consecutive elements of <paramref name="dst"/></param>
		/// <param name="toRealByAbs">If the conversion converts a complex type to a real type, whether the down grade elements be of the complexes's absolute values or their real parts.</param>
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaError vecDataConvert(DataType srcType, DataType dstType, IntPtr src, IntPtr dst, long N, int strideSrc, int strideDst, bool toRealByAbs);

		/// <summary>
		/// In-place set the values in <paramref name="a"/> whose absolute values are less than or equal to the absolute value of <paramref name="threshold"/> to 0
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/></param>
		/// <param name="a">The vector to be in-place modified of <paramref name="type"/></param>
		/// <param name="threshold">The pointer to the threshold used to clip the vector <paramref name="a"/> of <paramref name="type"/></param>
		/// <param name="N">The number of elements to be operated</param>
		/// <param name="stride">The spacing between consecutive elements of <paramref name="a"/></param>
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern void vecClip(DataType type, IntPtr a, void* threshold, long N, int stride);

		/// <summary>
		/// In-place add all elements in vector <paramref name="a"/> with the given <paramref name="scalar"/>
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/></param>
		/// <param name="a">The vector to be in-place modified of <paramref name="type"/></param>
		/// <param name="scalar">The pointer to the scalar to add of <paramref name="type"/></param>
		/// <param name="N">The number of elements to be operated</param>
		/// <param name="stride">The spacing between consecutive elements of <paramref name="a"/></param>
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern void vecAddScalar(DataType type, IntPtr a, void* scalar, long N, int stride);

		/// <summary>
		/// In-place multiplies all elements in vector <paramref name="a"/> with the given <paramref name="scalar"/>
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/></param>
		/// <param name="a">The vector to be in-place modified of <paramref name="type"/></param>
		/// <param name="scalar">The pointer to the scalar to multiply of <paramref name="type"/></param>
		/// <param name="N">The number of elements to be operated</param>
		/// <param name="stride">The spacing between consecutive elements of <paramref name="a"/></param>
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern void vecMulScalar(DataType type, IntPtr a, void* scalar, long N, int stride);

		/// <summary>
		/// Sums all the elements in vector <paramref name="a"/>
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/></param>
		/// <param name="a">The vector to be summed of <paramref name="type"/></param>
		/// <param name="N">The number of elements to be operated</param>
		/// <param name="stride">The spacing between consecutive elements of <paramref name="a"/></param>
		/// <param name="outSum">The output sum as a pointer of <paramref name="type"/></param>
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern void vecSum(DataType type, IntPtr a, long N, int stride, void* outSum);

		/// <summary>
		/// Get the index of the element with minimum absolute value in vector <paramref name="a"/>
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/></param>
		/// <param name="a">The vector to be summed of <paramref name="type"/></param>
		/// <param name="N">The number of elements to be operated</param>
		/// <param name="stride">The spacing between consecutive elements of <paramref name="a"/></param>
		/// <returns>The index of the element</returns>
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern long vecArgAbsMin(DataType type, IntPtr a, long N, int stride);

		/// <summary>
		/// Get the index of the element with maximum absolute value in vector <paramref name="a"/>
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/></param>
		/// <param name="a">The vector to be summed of <paramref name="type"/></param>
		/// <param name="N">The number of elements to be operated</param>
		/// <param name="stride">The spacing between consecutive elements of <paramref name="a"/></param>
		/// <returns>The index of the element</returns>
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern long vecArgAbsMax(DataType type, IntPtr a, long N, int stride);

		/// <summary>
		/// Sums all the elements's absolute values in vector <paramref name="a"/>
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/></param>
		/// <param name="a">The vector to be summed of <paramref name="type"/></param>
		/// <param name="N">The number of elements to be operated</param>
		/// <param name="stride">The spacing between consecutive elements of <paramref name="a"/></param>
		/// <returns>The sum as a <see cref="double"/></returns>
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern double vecAbsSum(DataType type, IntPtr a, long N, int stride);

		/// <summary>
		/// Compute the 2-norm of the given vector <paramref name="a"/>
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/></param>
		/// <param name="a">The vector to be summed of <paramref name="type"/></param>
		/// <param name="N">The number of elements to be operated</param>
		/// <param name="stride">The spacing between consecutive elements of <paramref name="a"/></param>
		/// <returns>The 2-norm as a <see cref="double"/></returns>
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern double vecNorm(DataType type, IntPtr a, long N, int stride);

		/// <summary>
		/// Calculate the inner product of vector <paramref name="a"/> and <paramref name="b"/>
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/></param>
		/// <param name="a">The left vector to be inner product of <paramref name="type"/></param>
		/// <param name="b">The right vector to be inner product of <paramref name="type"/></param>
		/// <param name="N">The number of elements to be operated</param>
		/// <param name="strideA">The spacing between consecutive elements of <paramref name="a"/></param>
		/// <param name="strideB">The spacing between consecutive elements of <paramref name="b"/></param>
		/// <param name="outProd">The output inner product as a pointer of <paramref name="type"/></param>
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern void vecDot(DataType type, IntPtr a, IntPtr b, long N, int strideA, int strideB, void* outProd);

		/// <summary>
		/// Calculate the inner product of the conjugate of vector <paramref name="a"/> and <paramref name="b"/>
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/>, must be a complex type</param>
		/// <param name="a">The left vector to be inner product of <paramref name="type"/></param>
		/// <param name="b">The right vector to be inner product of <paramref name="type"/></param>
		/// <param name="N">The number of elements to be operated</param>
		/// <param name="strideA">The spacing between consecutive elements of <paramref name="a"/></param>
		/// <param name="strideB">The spacing between consecutive elements of <paramref name="b"/></param>
		/// <param name="outProd">The output inner product as a pointer of <paramref name="type"/></param>
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern void vecDotc(DataType type, IntPtr a, IntPtr b, long N, int strideA, int strideB, void* outProd);

		/// <summary>
		/// Multiplies all the elements in vector <paramref name="a"/>
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/></param>
		/// <param name="a">The vector to be multiplied of <paramref name="type"/></param>
		/// <param name="N">The number of elements to be operated</param>
		/// <param name="stride">The spacing between consecutive elements of <paramref name="a"/></param>
		/// <param name="outProd">The output product as a pointer of <paramref name="type"/></param>
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern void vecProd(DataType type, IntPtr a, long N, int stride, void* outProd);

		/// <summary>
		/// Performs the partial sum from vector <paramref name="src"/> to vector <paramref name="dst"/>
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="src"/> and <paramref name="dst"/></param>
		/// <param name="src">The source vector of <paramref name="type"/></param>
		/// <param name="dst">The destination vector of <paramref name="type"/></param>
		/// <param name="N">The number of elements to be operated</param>
		/// <param name="inclusive">Perform inclusive (the first element is <paramref name="src"/>[0]) or exclusive (the first element is 0)</param>
		/// <param name="strideSrc">The spacing between consecutive elements of <paramref name="src"/></param>
		/// <param name="strideDst">The spacing between consecutive elements of <paramref name="dst"/></param>
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern void vecParSum(DataType type, IntPtr src, IntPtr dst, long N, bool inclusive, int strideSrc, int strideDst);

		/// <summary>
		/// Performs the partial sum from vector <paramref name="src"/> to vector <paramref name="dst"/>
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="src"/> and <paramref name="dst"/></param>
		/// <param name="src">The source vector of <paramref name="type"/></param>
		/// <param name="dst">The destination vector of <paramref name="type"/></param>
		/// <param name="N">The number of elements to be operated</param>
		/// <param name="inclusive">Perform inclusive (the first element is <paramref name="src"/>[0]) or exclusive (the first element is 1)</param>
		/// <param name="strideSrc">The spacing between consecutive elements of <paramref name="src"/></param>
		/// <param name="strideDst">The spacing between consecutive elements of <paramref name="dst"/></param>
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern void vecParProd(DataType type, IntPtr src, IntPtr dst, long N, bool inclusive, int strideSrc, int strideDst);

		/// <summary>
		/// Performs the Kronecker product of matrix <paramref name="A"/> and <paramref name="B"/> and add the result to <paramref name="dest"/> in-place
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="A"/>, <paramref name="B"/> and <paramref name="dest"/></param>
		/// <param name="A">The input left matrix of <paramref name="type"/></param>
		/// <param name="ldA">The leading dimension of <paramref name="A"/>, must be at least <paramref name="rowsA"/></param>
		/// <param name="rowsA">The number of rows of <paramref name="A"/></param>
		/// <param name="colsA">The number of columns of <paramref name="A"/></param>
		/// <param name="B">The input right matrix of <paramref name="type"/></param>
		/// <param name="ldB">The leading dimension of <paramref name="B"/>, must be at least <paramref name="rowsB"/></param>
		/// <param name="rowsB">The number of rows of <paramref name="B"/></param>
		/// <param name="colsB">The number of columns of <paramref name="B"/></param>
		/// <param name="dest">The destination matrix of <paramref name="type"/></param>
		/// <param name="ldD">The leading dimension of <paramref name="dest"/></param>
		/// <param name="alpha">The pointer to the scalar of <paramref name="type"/> to multiply to <paramref name="A"/>'s elements during the computation</param>
		/// <param name="beta">The pointer to the scalar of <paramref name="type"/> to multiply to <paramref name="dest"/>'s elements during the computation</param>
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern void matKron(DataType type,
											IntPtr A, long ldA, long rowsA, long colsA,
											IntPtr B, long ldB, long rowsB, long colsB,
											IntPtr dest, long ldD, void* alpha, void* beta);

		/// <summary>
		/// Makes the matrix <paramref name="A"/> hermitian or symmetric by copying its upper part to/from its lower part
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="A"/></param>
		/// <param name="A">The matrix to be modified of <paramref name="type"/></param>
		/// <param name="ld">The leading dimension of <paramref name="A"/>, must be at least <paramref name="rows"/></param>
		/// <param name="rows">The number of rows of <paramref name="A"/></param>
		/// <param name="upperStored">Whether <paramref name="A"/>'s upper part or its lower part is stored</param>
		/// <param name="hermA">If <paramref name="type"/> is a complex type, make <paramref name="A"/> hermitian or symmetric</param>
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern void matMakeHerm(DataType type, IntPtr A, long ld, long rows, bool upperStored, bool hermA);

		/// <summary>
		/// Clear (set to 0) the matrix <paramref name="A"/>'s upper part or its lower part
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="A"/></param>
		/// <param name="A">The matrix to be modified of <paramref name="type"/></param>
		/// <param name="ld">The leading dimension of <paramref name="A"/>, must be at least <paramref name="rows"/></param>
		/// <param name="rows">The number of rows of <paramref name="A"/></param>
		/// <param name="clearLower">Whether <paramref name="A"/>'s upper part or its lower part shall be preserved</param>
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern void matTriClear(DataType type, IntPtr A, long ld, long rows, bool clearLower);
		#endregion



		#region solver
		#region create and destroy
		/// <summary>
		/// This function initializes the cuSolverDN library and creates a handle on the cuSolverDN
		/// context. It must be called before any other cuSolverDN API function is invoked. It
		/// allocates hardware resources necessary for accessing the GPU
		/// </summary>
		/// <param name="handle">the pointer to the handle to the cuSolverDN context.</param>
		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnCreate(out IntPtr handle);

		/// <summary>
		/// This function releases CPU-side resources used by the cuSolverDN library.
		/// </summary>
		/// <param name="handle">handle to the cuSolverDN library context.</param>
		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnDestroy(IntPtr handle);
		#endregion

		#region implicit QR
		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnSgeqrf_bufferSize(IntPtr handle, int m, int n, IntPtr A, int lda, out int lenWork);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnDgeqrf_bufferSize(IntPtr handle, int m, int n, IntPtr A, int lda, out int lenWork);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnCgeqrf_bufferSize(IntPtr handle, int m, int n, IntPtr A, int lda, out int lenWork);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnZgeqrf_bufferSize(IntPtr handle, int m, int n, IntPtr A, int lda, out int lenWork);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnSgeqrf(IntPtr handle, int m, int n, IntPtr A, int lda, IntPtr τ, IntPtr Workspace, int lenWork, IntPtr devInfo);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnDgeqrf(IntPtr handle, int m, int n, IntPtr A, int lda, IntPtr τ, IntPtr Workspace, int lenWork, IntPtr devInfo);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnCgeqrf(IntPtr handle, int m, int n, IntPtr A, int lda, IntPtr τ, IntPtr Workspace, int lenWork, IntPtr devInfo);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnZgeqrf(IntPtr handle, int m, int n, IntPtr A, int lda, IntPtr τ, IntPtr Workspace, int lenWork, IntPtr devInfo);


		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnXgeqrf_bufferSize(IntPtr handle, IntPtr @params, long m, long n, CudaDataType dataTypeA, IntPtr A, long lda, CudaDataType dataTypeTau, IntPtr tau, CudaDataType computeType, out long workspaceInBytesOnDevice, out long workspaceInBytesOnHost);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnXgeqrf(IntPtr handle, IntPtr @params, long m, long n, CudaDataType dataTypeA, IntPtr A, long lda, CudaDataType dataTypeTau, IntPtr tau, CudaDataType computeType, IntPtr bufferOnDevice, long workspaceInBytesOnDevice, byte[] bufferOnHost, long workspaceInBytesOnHost, IntPtr devInfo);
		#endregion

		#region generate Q of implicit QR
		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnSorgqr_bufferSize(IntPtr handle, int m, int n, int k, IntPtr A, int lda, IntPtr τ, out int lenWork);
		
		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnDorgqr_bufferSize(IntPtr handle, int m, int n, int k, IntPtr A, int lda, IntPtr τ, out int lenWork);
		
		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnCungqr_bufferSize(IntPtr handle, int m, int n, int k, IntPtr A, int lda, IntPtr τ, out int lenWork);
		
		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnZungqr_bufferSize(IntPtr handle, int m, int n, int k, IntPtr A, int lda, IntPtr τ, out int lenWork);
		
		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnSorgqr(IntPtr handle, int m, int n, int k, IntPtr A, int lda, IntPtr τ, IntPtr work, int lenWork, IntPtr devInfo);
		
		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnDorgqr(IntPtr handle, int m, int n, int k, IntPtr A, int lda, IntPtr τ, IntPtr work, int lenWork, IntPtr devInfo);
		
		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnCungqr(IntPtr handle, int m, int n, int k, IntPtr A, int lda, IntPtr τ, IntPtr work, int lenWork, IntPtr devInfo);
		
		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnZungqr(IntPtr handle, int m, int n, int k, IntPtr A, int lda, IntPtr τ, IntPtr work, int lenWork, IntPtr devInfo);
		#endregion

		#region multiply Q of implicit QR
		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnSormqr_bufferSize(IntPtr handle, SideMode side, CuBlasOperation trans, int m, int n, int k, IntPtr A, int lda, IntPtr τ, IntPtr C, int ldc, out int lenWork);
		
		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnDormqr_bufferSize(IntPtr handle, SideMode side, CuBlasOperation trans, int m, int n, int k, IntPtr A, int lda, IntPtr τ, IntPtr C, int ldc, out int lenWork);
		
		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnCunmqr_bufferSize(IntPtr handle, SideMode side, CuBlasOperation trans, int m, int n, int k, IntPtr A, int lda, IntPtr τ, IntPtr C, int ldc,  out int lenWork);
		
		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnZunmqr_bufferSize(IntPtr handle, SideMode side, CuBlasOperation trans, int m, int n, int k, IntPtr A, int lda, IntPtr τ, IntPtr C, int ldc, out int lenWork);
		
		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnSormqr(IntPtr handle, SideMode side, CuBlasOperation trans, int m, int n, int k, IntPtr A, int lda, IntPtr τ, IntPtr C, int ldc, IntPtr work, int lenWork, IntPtr devInfo);
		
		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnDormqr(IntPtr handle, SideMode side, CuBlasOperation trans, int m, int n, int k, IntPtr A, int lda, IntPtr τ, IntPtr C, int ldc, IntPtr work, int lenWork, IntPtr devInfo);
		
		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnCunmqr(IntPtr handle, SideMode side, CuBlasOperation trans, int m, int n, int k, IntPtr A, int lda, IntPtr τ, IntPtr C, int ldc, IntPtr work, int lenWork, IntPtr devInfo);
		
		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnZunmqr(IntPtr handle, SideMode side, CuBlasOperation trans, int m, int n, int k, IntPtr A, int lda, IntPtr τ, IntPtr C, int ldc, IntPtr work, int lenWork, IntPtr devInfo);
		#endregion

		#region LU Factorization
		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnSgetrf_bufferSize(IntPtr handle, int m, int n, IntPtr A, int lda, out int lenWork);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnDgetrf_bufferSize(IntPtr handle, int m, int n, IntPtr A, int lda, out int lenWork);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnCgetrf_bufferSize(IntPtr handle, int m, int n, IntPtr A, int lda, out int lenWork);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnZgetrf_bufferSize(IntPtr handle, int m, int n, IntPtr A, int lda, out int lenWork);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnSgetrf(IntPtr handle, int m, int n, IntPtr A, int lda, IntPtr Workspace, IntPtr devIpiv, IntPtr devInfo);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnDgetrf(IntPtr handle, int m, int n, IntPtr A, int lda, IntPtr Workspace, IntPtr devIpiv, IntPtr devInfo);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnCgetrf(IntPtr handle, int m, int n, IntPtr A, int lda, IntPtr Workspace, IntPtr devIpiv, IntPtr devInfo);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnZgetrf(IntPtr handle, int m, int n, IntPtr A, int lda, IntPtr Workspace, IntPtr devIpiv, IntPtr devInfo);


		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnXgetrf_bufferSize(IntPtr handle, IntPtr @params, long m, long n, CudaDataType dataTypeA, IntPtr A, long lda, CudaDataType computeType, out long workspaceInBytesOnDevice, out long workspaceInBytesOnHost);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnXgetrf(IntPtr handle, IntPtr @params, long m, long n, CudaDataType dataTypeA, IntPtr A, long lda, IntPtr ipiv, CudaDataType computeType, IntPtr bufferOnDevice, long workspaceInBytesOnDevice, byte[] bufferOnHost, long workspaceInBytesOnHost, IntPtr devInfo);
		#endregion

		#region LU solve
		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnSgetrs(IntPtr handle, CuBlasOperation trans, int n, int nrhs, IntPtr A, int lda, IntPtr devIpiv, IntPtr B, int ldb, IntPtr devInfo);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnDgetrs(IntPtr handle, CuBlasOperation trans, int n, int nrhs, IntPtr A, int lda, IntPtr devIpiv, IntPtr B, int ldb, IntPtr devInfo);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnCgetrs(IntPtr handle, CuBlasOperation trans, int n, int nrhs, IntPtr A, int lda, IntPtr devIpiv, IntPtr B, int ldb, IntPtr devInfo);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnZgetrs(IntPtr handle, CuBlasOperation trans, int n, int nrhs, IntPtr A, int lda, IntPtr devIpiv, IntPtr B, int ldb, IntPtr devInfo);


		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnXgetrs(IntPtr handle, IntPtr @params, CuBlasOperation trans, long n, long nrhs, CudaDataType dataTypeA, IntPtr A, long lda, IntPtr ipiv, CudaDataType dataTypeB, IntPtr B, long ldb, IntPtr devInfo);
		#endregion

		#region standard symmetric (Hermitian) eigen-solve
		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnSsyevd_bufferSize(IntPtr handle, SolveVectorMode jobz, MatrixFillMode uplo, int n, IntPtr A, int lda, IntPtr eigVecOut, out int lenWork);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnDsyevd_bufferSize(IntPtr handle, SolveVectorMode jobz, MatrixFillMode uplo, int n, IntPtr A, int lda, IntPtr eigVecOut, out int lenWork);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnCheevd_bufferSize(IntPtr handle, SolveVectorMode jobz, MatrixFillMode uplo, int n, IntPtr A, int lda, IntPtr eigVecOut, out int lenWork);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnZheevd_bufferSize(IntPtr handle, SolveVectorMode jobz, MatrixFillMode uplo, int n, IntPtr A, int lda, IntPtr eigVecOut, out int lenWork);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnSsyevd(IntPtr handle, SolveVectorMode jobz, MatrixFillMode uplo, int n, IntPtr A, int lda, IntPtr eigVecOut, IntPtr work, int lenWork, IntPtr devInfo);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnDsyevd(IntPtr handle, SolveVectorMode jobz, MatrixFillMode uplo, int n, IntPtr A, int lda, IntPtr eigVecOut, IntPtr work, int lenWork, IntPtr devInfo);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnCheevd(IntPtr handle, SolveVectorMode jobz, MatrixFillMode uplo, int n, IntPtr A, int lda, IntPtr eigVecOut, IntPtr work, int lenWork, IntPtr devInfo);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnZheevd(IntPtr handle, SolveVectorMode jobz, MatrixFillMode uplo, int n, IntPtr A, int lda, IntPtr eigVecOut, IntPtr work, int lenWork, IntPtr devInfo);


		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnXsyevd_bufferSize(IntPtr handle, IntPtr @params, SolveVectorMode jobz, MatrixFillMode uplo, long n, CudaDataType dataTypeA, IntPtr A, long lda, CudaDataType dataTypeW, IntPtr W, CudaDataType computeType, out long workspaceInBytesOnDevice, out long workspaceInBytesOnHost);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnXsyevd(IntPtr handle, IntPtr @params, SolveVectorMode jobz, MatrixFillMode uplo, long n, CudaDataType dataTypeA, IntPtr A, long lda, CudaDataType dataTypeW, IntPtr W, CudaDataType computeType, IntPtr bufferOnDevice, long workspaceInBytesOnDevice, byte[] bufferOnHost, long workspaceInBytesOnHost, IntPtr devInfo);
		#endregion

		#region generalized symmetric (Hermitian) eigen-solve
		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnSsygvd_bufferSize(IntPtr handle, GeneralEigenType itype, SolveVectorMode jobz, MatrixFillMode uplo, int n, IntPtr A, int lda, IntPtr B, int ldb, IntPtr W, out int lenWork);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnDsygvd_bufferSize(IntPtr handle, GeneralEigenType itype, SolveVectorMode jobz, MatrixFillMode uplo, int n, IntPtr A, int lda, IntPtr B, int ldb, IntPtr W, out int lenWork);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnChegvd_bufferSize(IntPtr handle, GeneralEigenType itype, SolveVectorMode jobz, MatrixFillMode uplo, int n, IntPtr A, int lda, IntPtr B, int ldb, IntPtr W, out int lenWork);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnZhegvd_bufferSize(IntPtr handle, GeneralEigenType itype, SolveVectorMode jobz, MatrixFillMode uplo, int n, IntPtr A, int lda, IntPtr B, int ldb, IntPtr W, out int lenWork);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnSsygvd(IntPtr handle, GeneralEigenType itype, SolveVectorMode jobz, MatrixFillMode uplo, int n, IntPtr A, int lda, IntPtr B, int ldb, IntPtr W, IntPtr work, int lenWork, IntPtr devInfo);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnDsygvd(IntPtr handle, GeneralEigenType itype, SolveVectorMode jobz, MatrixFillMode uplo, int n, IntPtr A, int lda, IntPtr B, int ldb, IntPtr W, IntPtr work, int lenWork, IntPtr devInfo);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnChegvd(IntPtr handle, GeneralEigenType itype, SolveVectorMode jobz, MatrixFillMode uplo, int n, IntPtr A, int lda, IntPtr B, int ldb, IntPtr W, IntPtr work, int lenWork, IntPtr devInfo);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnZhegvd(IntPtr handle, GeneralEigenType itype, SolveVectorMode jobz, MatrixFillMode uplo, int n, IntPtr A, int lda, IntPtr B, int ldb, IntPtr W, IntPtr work, int lenWork, IntPtr devInfo);
		#endregion

		#region singular value decomposition
		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnSgesvd_bufferSize(IntPtr handle, int m, int n, out int lenWork);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnDgesvd_bufferSize(IntPtr handle, int m, int n, out int lenWork);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnCgesvd_bufferSize(IntPtr handle, int m, int n, out int lenWork);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnZgesvd_bufferSize(IntPtr handle, int m, int n, out int lenWork);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnSgesvd(IntPtr handle, sbyte jobu, sbyte jobvt, int m, int n, IntPtr A, int lda, IntPtr S, IntPtr U, int ldu, IntPtr VT, int ldvt, IntPtr work, int lenWork, IntPtr rwork, IntPtr devInfo);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnDgesvd(IntPtr handle, sbyte jobu, sbyte jobvt, int m, int n, IntPtr A, int lda, IntPtr S, IntPtr U, int ldu, IntPtr VT, int ldvt, IntPtr work, int lenWork, IntPtr rwork, IntPtr devInfo);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnCgesvd(IntPtr handle, sbyte jobu, sbyte jobvt, int m, int n, IntPtr A, int lda, IntPtr S, IntPtr U, int ldu, IntPtr VT, int ldvt, IntPtr work, int lenWork, IntPtr rwork, IntPtr devInfo);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnZgesvd(IntPtr handle, sbyte jobu, sbyte jobvt, int m, int n, IntPtr A, int lda, IntPtr S, IntPtr U, int ldu, IntPtr VT, int ldvt, IntPtr work, int lenWork, IntPtr rwork, IntPtr devInfo);


		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnXgesvd_bufferSize(IntPtr handle, IntPtr @params, sbyte jobu, sbyte jobvt, long m, long n, CudaDataType dataTypeA, IntPtr A, long lda, CudaDataType dataTypeS, IntPtr S, CudaDataType dataTypeU, IntPtr U, long ldu, CudaDataType dataTypeVT, IntPtr VT, long ldvt, CudaDataType computeType, out long workspaceInBytesOnDevice, out long workspaceInBytesOnHost);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnXgesvd(IntPtr handle, IntPtr @params, sbyte jobu, sbyte jobvt, long m, long n, CudaDataType dataTypeA, IntPtr A, long lda, CudaDataType dataTypeS, IntPtr S, CudaDataType dataTypeU, IntPtr U, long ldu, CudaDataType dataTypeVT, IntPtr VT, long ldvt, CudaDataType computeType, IntPtr bufferOnDevice, long workspaceInBytesOnDevice, byte[] bufferOnHost, long workspaceInBytesOnHost, IntPtr devInfo);


		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnXgesvdp_bufferSize(IntPtr handle, IntPtr @params, SolveVectorMode jobz, int econ, long m, long n, CudaDataType dataTypeA, IntPtr A, long lda, CudaDataType dataTypeS, IntPtr S, CudaDataType dataTypeU, IntPtr U, long ldu, CudaDataType dataTypeV, IntPtr V, long ldv, CudaDataType computeType, out long workspaceInBytesOnDevice, out long workspaceInBytesOnHost);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnXgesvdp(IntPtr handle, IntPtr @params, SolveVectorMode jobz, int econ, long m, long n, CudaDataType dataTypeA, IntPtr A, long lda, CudaDataType dataTypeS, IntPtr S, CudaDataType dataTypeU, IntPtr U, long ldu, CudaDataType dataTypeV, IntPtr V, long ldv, CudaDataType computeType, IntPtr bufferOnDevice, long workspaceInBytesOnDevice, byte[] bufferOnHost, long workspaceInBytesOnHost, IntPtr devInfo, out double error);
		#endregion
		#endregion
	}
}
