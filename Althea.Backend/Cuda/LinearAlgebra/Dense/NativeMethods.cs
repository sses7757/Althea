using System.Runtime.InteropServices;

using Althea.LinearAlgebra;
using Althea.SourceGenerator;


namespace Althea.Backend.Cuda.LinearAlgebra.Dense
{
	[NativeMethodClass]
	internal static unsafe class NativeMethodsTemplate
	{
		#region level 1 BLAS
		[NativeMethod(7)]
		[DllImport(Cuda.NativeMethods.CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasIsamax(IntPtr handle, int n, void* x, int incx, out int result);

		[NativeMethod(7)]
		[DllImport(Cuda.NativeMethods.CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasIsamin(IntPtr handle, int n, void* x, int incx, out int result);

		[NativeMethod(6, true, false, true)]
		[DllImport(Cuda.NativeMethods.CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSasum(IntPtr handle, int n, void* x, int incx, void* result);

		[NativeMethod(6, true)]
		[DllImport(Cuda.NativeMethods.CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSaxpy(IntPtr handle, int n, void* α, void* x, int incx, void* y, int incy);

		[CustomNativeMethod(6, "Float32", @"Sdot")]
		[CustomNativeMethod(6, "Float64", @"Ddot")]
		[CustomNativeMethod(6, "Complex<Float32>", @"Cdotu")]
		[CustomNativeMethod(6, "Complex<Float32>", @"Cdotc")]
		[CustomNativeMethod(6, "Complex<Float64>", @"Zdotu")]
		[CustomNativeMethod(6, "Complex<Float64>", @"Zdotc")]
		[DllImport(Cuda.NativeMethods.CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSdot(IntPtr handle, int n, void* x, int incx, void* y, int incy, void* result);

		[NativeMethod(6, true)]
		[DllImport(Cuda.NativeMethods.CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSnrm2(IntPtr handle, int n, void* x, int incx, void* result);

		[NativeMethod(6, true)]
		[DllImport(Cuda.NativeMethods.CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSscal(IntPtr handle, int n, void* α, void* x, int incx);

		[NativeMethod(6, true)]
		[DllImport(Cuda.NativeMethods.CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasScopy(IntPtr handle, int n, void* x, int incx, void* y, int incy);
		#endregion

		#region level 2 BLAS
		[NativeMethod(6, true)]
		[DllImport(Cuda.NativeMethods.CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSgemv(IntPtr handle, CuBlasOperation op, int m, int n, void* α, void* A, int lda, void* x, int incx, void* β, void* y, int incy);

		[NativeMethod(6, true)]
		[DllImport(Cuda.NativeMethods.CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSsymv(IntPtr handle, CuBlasFillMode uplo, int n, void* α, void* A, int lda, void* x, int incx, void* β, void* y, int incy);

		[NativeMethod(6, true)]
		[DllImport(Cuda.NativeMethods.CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasStrmv(IntPtr handle, CuBlasFillMode uplo, CuBlasOperation op, CuBlasDiagType diag, int n, void* A, int lda, void* x, int incx);

		[CustomNativeMethod(6, "Float32", @"Sger")]
		[CustomNativeMethod(6, "Float64", @"Dger")]
		[CustomNativeMethod(6, "Complex<Float32>", @"Cgeru")]
		[CustomNativeMethod(6, "Complex<Float32>", @"Cgerc")]
		[CustomNativeMethod(6, "Complex<Float64>", @"Zgeru")]
		[CustomNativeMethod(6, "Complex<Float64>", @"Zgerc")]
		[DllImport(Cuda.NativeMethods.CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSger(IntPtr handle, int m, int n, void* α, void* x, int incx, void* y, int incy, void* A, int lda);

		[NativeMethod(6, true)]
		[DllImport(Cuda.NativeMethods.CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSsyr(IntPtr handle, CuBlasFillMode fillMode, int n, void* α, void* x, int incx, void* A, int lda);

		[NativeMethod(6, true, false, false, false)]
		[DllImport(Cuda.NativeMethods.CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCher(IntPtr handle, CuBlasFillMode fillMode, int n, void* α, void* x, int incx, void* A, int lda);

		[NativeMethod(6, true)]
		[DllImport(Cuda.NativeMethods.CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSsyr2(IntPtr handle, CuBlasFillMode fillMode, int n, void* α, void* x, int incx, void* y, int incy, void* A, int lda);

		[NativeMethod(6, true, false, false, false)]
		[DllImport(Cuda.NativeMethods.CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCher2(IntPtr handle, CuBlasFillMode fillMode, int n, void* α, void* x, int incx, void* y, int incy, void* A, int lda);
		#endregion

		#region level 3 BLAS
		[NativeMethod(6, true)]
		[DllImport(Cuda.NativeMethods.CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSgemm(IntPtr handle, CuBlasOperation opA, CuBlasOperation opB, int m, int n, int k, void* α, void* A, int lda, void* B, int ldb, void* β, void* C, int ldc);

		[NativeMethod(6, true)]
		[DllImport(Cuda.NativeMethods.CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSsymm(IntPtr handle, CuBlasSideMode side, CuBlasFillMode uplo, int m, int n, void* α, void* A, int lda, void* B, int ldb, void* β, void* C, int ldc);

		[NativeMethod(6, true, false, true, false)]
		[DllImport(Cuda.NativeMethods.CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasChemm(IntPtr handle, CuBlasSideMode side, CuBlasFillMode uplo, int m, int n, void* α, void* A, int lda, void* B, int ldb, void* β, void* C, int ldc);

		[NativeMethod(6, true)]
		[DllImport(Cuda.NativeMethods.CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSsyrk(IntPtr handle, CuBlasFillMode uplo, CuBlasOperation op, int n, int k, void* α, void* A, int lda, void* β, void* C, int ldc);

		[NativeMethod(6, true, false, true, false)]
		[DllImport(Cuda.NativeMethods.CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCherk(IntPtr handle, CuBlasFillMode uplo, CuBlasOperation op, int n, int k, void* α, void* A, int lda, void* β, void* C, int ldc);

		[NativeMethod(6, true)]
		[DllImport(Cuda.NativeMethods.CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSsyr2k(IntPtr handle, CuBlasFillMode uplo, CuBlasOperation op, int n, int k, void* α, void* A, int lda, void* B, int ldb, void* β, void* C, int ldc);

		[NativeMethod(6, true, false, true, false)]
		[DllImport(Cuda.NativeMethods.CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCher2k(IntPtr handle, CuBlasFillMode uplo, CuBlasOperation op, int n, int k, void* α, void* A, int lda, void* B, int ldb, void* β, void* C, int ldc);

		[NativeMethod(6, true)]
		[DllImport(Cuda.NativeMethods.CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSsyrkx(IntPtr handle, CuBlasFillMode uplo, CuBlasOperation op, int n, int k, void* α, void* A, int lda, void* B, int ldb, void* β, void* C, int ldc);

		[NativeMethod(6, true, false, true, false)]
		[DllImport(Cuda.NativeMethods.CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCherkx(IntPtr handle, CuBlasFillMode uplo, CuBlasOperation op, int n, int k, void* α, void* A, int lda, void* B, int ldb, void* β, void* C, int ldc);

		[NativeMethod(6, true)]
		[DllImport(Cuda.NativeMethods.CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasStrsm(IntPtr handle, CuBlasSideMode side, CuBlasFillMode uplo, CuBlasOperation op, CuBlasDiagType diag, int m, int n, void* α, void* A, int lda, void* B, int ldb);

		[NativeMethod(6, true)]
		[DllImport(Cuda.NativeMethods.CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasStrmm(IntPtr handle, CuBlasSideMode side, CuBlasFillMode uplo, CuBlasOperation op, CuBlasDiagType diag, int m, int n, void* α, void* A, int lda, void* B, int ldb, void* C, int ldc);

		[NativeMethod(6, true)]
		[DllImport(Cuda.NativeMethods.CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSgeam(IntPtr handle, CuBlasOperation opA, CuBlasOperation opB, int m, int n, void* α, void* A, int lda, void* β, void* B, int ldb, void* C, int ldc);

		[NativeMethod(6, true)]
		[DllImport(Cuda.NativeMethods.CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSdgmm(IntPtr handle, CuBlasSideMode mode, int m, int n, void* A, int lda, void* x, int incx, void* C, int ldc);
		#endregion

		#region solver
		[NativeMethod(10, true)]
		[DllImport(Cuda.NativeMethods.CUSOLVER_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnSgeqrf_bufferSize(IntPtr handle, int m, int n, void* A, int lda, out int lenWork);

		[NativeMethod(10, true)]
		[DllImport(Cuda.NativeMethods.CUSOLVER_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnSgeqrf(IntPtr handle, int m, int n, void* A, int lda, void* τ, void* Workspace, int lenWork, void* devInfo);

		[NativeMethod(10, true)]
		[DllImport(Cuda.NativeMethods.CUSOLVER_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnSorgqr_bufferSize(IntPtr handle, int m, int n, int k, void* A, int lda, void* τ, out int lenWork);

		[NativeMethod(10, true)]
		[DllImport(Cuda.NativeMethods.CUSOLVER_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnSorgqr(IntPtr handle, int m, int n, int k, void* A, int lda, void* τ, void* work, int lenWork, void* devInfo);

		[CustomNativeMethod(10, "Float32", @"Sor")]
		[CustomNativeMethod(10, "Float64", @"Dor")]
		[CustomNativeMethod(10, "Complex<Float32>", @"Cun")]
		[CustomNativeMethod(10, "Complex<Float64>", @"Zun")]
		[DllImport(Cuda.NativeMethods.CUSOLVER_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnSormqr_bufferSize(IntPtr handle, CuBlasSideMode side, CuBlasOperation trans, int m, int n, int k, void* A, int lda, void* τ, void* C, int ldc, out int lenWork);

		[CustomNativeMethod(10, "Float32", @"Sor")]
		[CustomNativeMethod(10, "Float64", @"Dor")]
		[CustomNativeMethod(10, "Complex<Float32>", @"Cun")]
		[CustomNativeMethod(10, "Complex<Float64>", @"Zun")]
		[DllImport(Cuda.NativeMethods.CUSOLVER_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnSormqr(IntPtr handle, CuBlasSideMode side, CuBlasOperation trans, int m, int n, int k, void* A, int lda, void* τ, void* C, int ldc, void* work, int lenWork, void* devInfo);

		[CustomNativeMethod(10, "Float32", @"Ssy")]
		[CustomNativeMethod(10, "Float64", @"Dsy")]
		[CustomNativeMethod(10, "Complex<Float32>", @"Che")]
		[CustomNativeMethod(10, "Complex<Float64>", @"Zhe")]
		[DllImport(Cuda.NativeMethods.CUSOLVER_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnSsygvd_bufferSize(IntPtr handle, GeneralEigenType itype, CuSolverEigMode jobz, CuBlasFillMode uplo, int n, void* A, int lda, void* B, int ldb, void* W, out int lenWork);

		[CustomNativeMethod(10, "Float32", @"Ssy")]
		[CustomNativeMethod(10, "Float64", @"Dsy")]
		[CustomNativeMethod(10, "Complex<Float32>", @"Che")]
		[CustomNativeMethod(10, "Complex<Float64>", @"Zhe")]
		[DllImport(Cuda.NativeMethods.CUSOLVER_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnSsygvd(IntPtr handle, GeneralEigenType itype, CuSolverEigMode jobz, CuBlasFillMode uplo, int n, void* A, int lda, void* B, int ldb, void* W, void* work, int lenWork, void* devInfo);
		#endregion
	}

	/// <summary>
	/// The actual class for CUDA BLAS and SOLVER library APIs
	/// </summary>
	public static unsafe partial class NativeMethods
	{
		#region utilities
		/// <summary>
		/// This function initializes the CUDA BLAS library and creates a handle to an opaque structure holding the CUDA BLAS library context.
		/// </summary>
		/// <param name="handle">returned CUDA BLAS handle</param>
		/// <returns>operation status enum <see cref="CudaBlasStatus"/></returns>
		[DllImport(Cuda.NativeMethods.CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCreate(out IntPtr handle);

		/// <summary>
		/// This function releases hardware resources used by the CUBLAS library. This function is usually the last call with a particular handle to the CUBLAS library.
		/// </summary>
		/// <param name="handle">input CUDA BLAS handle</param>
		/// <returns>operation status enum <see cref="CudaBlasStatus"/></returns>
		[DllImport(Cuda.NativeMethods.CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasDestroy(IntPtr handle);

		/// <summary>
		/// Some routines like <c>cublas&lt;t&gt;symv</c> and <c>cublas&lt;t&gt;hemv</c> have an alternate implementation that use atomics to cumulate results. This implementation is generally significantly faster but can generate results that are not strictly identical from one run to the others. Mathematically, those different results are not significant but when debugging
		/// those differences can be prejudicial. <para/>
		/// This function queries the atomic mode of a specific cuBLAS context.
		/// </summary>
		/// <param name="handle">input CUDA BLAS handle</param>
		/// <param name="mode">returned <see cref="CuBlasAtomicsMode"/></param>
		/// <returns>operation status enum <see cref="CudaBlasStatus"/></returns>
		[DllImport(Cuda.NativeMethods.CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasGetAtomicsMode(IntPtr handle, out CuBlasAtomicsMode mode);

		/// <summary>
		/// Some routines like <c>cublas&lt;t&gt;symv</c> have an alternate implementation that use atomics to cumulate results. This implementation is generally significantly faster but can generate results that are not strictly identical from one run to the others. Mathematically, those different results are not significant but when debugging those differences can be prejudicial.
		/// <para/>This function allows or disallows the usage of atomics in the CUDA BLAS library for all routines which have an alternate implementation.
		/// </summary>
		/// <param name="handle">input CUDA BLAS handle</param>
		/// <param name="mode">the <see cref="CuBlasAtomicsMode"/> to set</param>
		/// <returns>operation status enum <see cref="CudaBlasStatus"/></returns>
		[DllImport(Cuda.NativeMethods.CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSetAtomicsMode(IntPtr handle, CuBlasAtomicsMode mode);

		/// <summary>
		/// This function obtains the pointer mode used by the cuBLAS library.
		/// </summary>
		/// <param name="handle">input CUDA BLAS handle</param>
		/// <param name="mode">returned <see cref="CuBlasPointerMode"/></param>
		/// <returns>operation status enum <see cref="CudaBlasStatus"/></returns>
		[DllImport(Cuda.NativeMethods.CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasGetPointerMode(IntPtr handle, out CuBlasPointerMode mode);

		/// <summary>
		/// This function sets the pointer mode used by the cuBLAS library.
		/// The default is for the values to be passed by reference on the host.
		/// </summary>
		/// <param name="handle">input CUDA BLAS handle</param>
		/// <param name="mode">the <see cref="CuBlasPointerMode"/> to set</param>
		/// <returns>operation status enum <see cref="CudaBlasStatus"/></returns>
		[DllImport(Cuda.NativeMethods.CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSetPointerMode(IntPtr handle, CuBlasPointerMode mode);
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
		[DllImport(Cuda.NativeMethods.CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSetVector(int n, int elemSize, void* x, int incx, void* y, int incy);

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
		[DllImport(Cuda.NativeMethods.CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasGetVector(int n, int elemSize, void* x, int incx, void* y, int incy);
		#endregion

		#region extension BLAS
		// do not support BrainHalf
		[DllImport(Cuda.NativeMethods.CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasAxpyEx(IntPtr handle, int n, void* alpha, CudaDataType alphaType, void* x, CudaDataType xType, int incx, void* y, CudaDataType yType, int incy, CudaDataType executiontype);

		// do not support BrainHalf
		[DllImport(Cuda.NativeMethods.CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasDotEx(IntPtr handle, int n, void* x, CudaDataType xType, int incx, void* y, CudaDataType yType, int incy, void* reslut, CudaDataType resultType, CudaDataType executiontype);
		[DllImport(Cuda.NativeMethods.CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasDotcEx(IntPtr handle, int n, void* x, CudaDataType xType, int incx, void* y, CudaDataType yType, int incy, void* reslut, CudaDataType resultType, CudaDataType executiontype);

		// do not support BrainHalf
		[DllImport(Cuda.NativeMethods.CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasScalEx(IntPtr handle, int n, void* alpha, CudaDataType alphaType, void* x, CudaDataType xType, int incx, CudaDataType executiontype);

		[DllImport(Cuda.NativeMethods.CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasGemmEx(IntPtr handle, CuBlasOperation opA, CuBlasOperation opB, int m, int n, int k, void* alpha, void* A, CudaDataType Atype, int lda, void* B, CudaDataType Btype, int ldb, void* beta, void* C, CudaDataType Ctype, int ldc, CuBlasComputeType computeType, CuBlasGemmAlgorithm algo);
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
		[DllImport(Cuda.NativeMethods.CUSTOM_API_DLL_NAME)]
		internal static extern int vecsMulDiv(DataType type, void* a, void* b, long N, int strideA, int strideB, bool multiply);

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
		[DllImport(Cuda.NativeMethods.CUSTOM_API_DLL_NAME)]
		internal static extern int vecsAdd(DataType type, void* scalar, void* a, void* b, long N, int strideA, int strideB);

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
		[DllImport(Cuda.NativeMethods.CUSTOM_API_DLL_NAME)]
		internal static extern bool vecsEq(DataType type, void* a, void* b, long N, int strideA, int strideB);

		/// <summary>
		/// In-place exponentiate the vector <paramref name="a"/> by a scalar exponent <paramref name="p"/>
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/></param>
		/// <param name="a">The vector to be in-place modified of <paramref name="type"/></param>
		/// <param name="p">The pointer to the scalar exponent of <paramref name="type"/></param>
		/// <param name="N">The number of elements to be operated</param>
		/// <param name="stride">The spacing between consecutive elements of <paramref name="a"/></param>
		[DllImport(Cuda.NativeMethods.CUSTOM_API_DLL_NAME)]
		internal static extern int vecPowSameType(DataType type, void* a, void* p, long N, int stride);

		/// <summary>
		/// In-place exponentiate the vector <paramref name="a"/> by a scalar exponent <paramref name="p"/>
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/> (must be a complex type)</param>
		/// <param name="a">The vector to be in-place modified of <paramref name="type"/></param>
		/// <param name="p">The pointer to the scalar exponent of <paramref name="type"/>'s real corresponding type</param>
		/// <param name="N">The number of elements to be operated</param>
		/// <param name="stride">The spacing between consecutive elements of <paramref name="a"/></param>
		[DllImport(Cuda.NativeMethods.CUSTOM_API_DLL_NAME)]
		internal static extern int vecPowRealType(DataType type, void* a, void* p, long N, int stride);

		/// <summary>
		/// In-place conjugate the vector <paramref name="a"/>
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/></param>
		/// <param name="a">The vector to be in-place modified of <paramref name="type"/></param>
		/// <param name="N">The number of elements to be operated</param>
		/// <param name="stride">The spacing between consecutive elements of <paramref name="a"/></param>
		[DllImport(Cuda.NativeMethods.CUSTOM_API_DLL_NAME)]
		internal static extern int vecConj(DataType type, void* a, long N, int stride);

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
		[DllImport(Cuda.NativeMethods.CUSTOM_API_DLL_NAME)]
		internal static extern CudaError vecDataConvert(DataType srcType, DataType dstType, void* src, void* dst, long N, int strideSrc, int strideDst, bool toRealByAbs);

		/// <summary>
		/// In-place set the values in <paramref name="a"/> whose absolute values are less than or equal to the absolute value of <paramref name="threshold"/> to 0
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/></param>
		/// <param name="a">The vector to be in-place modified of <paramref name="type"/></param>
		/// <param name="threshold">The pointer to the threshold used to clip the vector <paramref name="a"/> of <paramref name="type"/></param>
		/// <param name="N">The number of elements to be operated</param>
		/// <param name="stride">The spacing between consecutive elements of <paramref name="a"/></param>
		[DllImport(Cuda.NativeMethods.CUSTOM_API_DLL_NAME)]
		internal static extern int vecClip(DataType type, void* a, void* threshold, long N, int stride);

		/// <summary>
		/// In-place add all elements in vector <paramref name="a"/> with the given <paramref name="scalar"/>
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/></param>
		/// <param name="a">The vector to be in-place modified of <paramref name="type"/></param>
		/// <param name="scalar">The pointer to the scalar to add of <paramref name="type"/></param>
		/// <param name="N">The number of elements to be operated</param>
		/// <param name="stride">The spacing between consecutive elements of <paramref name="a"/></param>
		[DllImport(Cuda.NativeMethods.CUSTOM_API_DLL_NAME)]
		internal static extern int vecAddScalar(DataType type, void* a, void* scalar, long N, int stride);

		/// <summary>
		/// In-place multiplies all elements in vector <paramref name="a"/> with the given <paramref name="scalar"/>
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/></param>
		/// <param name="a">The vector to be in-place modified of <paramref name="type"/></param>
		/// <param name="scalar">The pointer to the scalar to multiply of <paramref name="type"/></param>
		/// <param name="N">The number of elements to be operated</param>
		/// <param name="stride">The spacing between consecutive elements of <paramref name="a"/></param>
		[DllImport(Cuda.NativeMethods.CUSTOM_API_DLL_NAME)]
		internal static extern int vecMulScalar(DataType type, void* a, void* scalar, long N, int stride);

		/// <summary>
		/// Sums all the elements in vector <paramref name="a"/>
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/></param>
		/// <param name="a">The vector to be summed of <paramref name="type"/></param>
		/// <param name="N">The number of elements to be operated</param>
		/// <param name="stride">The spacing between consecutive elements of <paramref name="a"/></param>
		/// <param name="outSum">The output sum as a pointer of <paramref name="type"/></param>
		[DllImport(Cuda.NativeMethods.CUSTOM_API_DLL_NAME)]
		internal static extern int vecSum(DataType type, void* a, long N, int stride, void* outSum);

		/// <summary>
		/// Get the index of the element with minimum absolute value in vector <paramref name="a"/>
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/></param>
		/// <param name="a">The vector to be summed of <paramref name="type"/></param>
		/// <param name="N">The number of elements to be operated</param>
		/// <param name="stride">The spacing between consecutive elements of <paramref name="a"/></param>
		/// <returns>The index of the element</returns>
		[DllImport(Cuda.NativeMethods.CUSTOM_API_DLL_NAME)]
		internal static extern long vecArgAbsMin(DataType type, void* a, long N, int stride);

		/// <summary>
		/// Get the index of the element with maximum absolute value in vector <paramref name="a"/>
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/></param>
		/// <param name="a">The vector to be summed of <paramref name="type"/></param>
		/// <param name="N">The number of elements to be operated</param>
		/// <param name="stride">The spacing between consecutive elements of <paramref name="a"/></param>
		/// <returns>The index of the element</returns>
		[DllImport(Cuda.NativeMethods.CUSTOM_API_DLL_NAME)]
		internal static extern long vecArgAbsMax(DataType type, void* a, long N, int stride);

		/// <summary>
		/// Sums all the elements's absolute values in vector <paramref name="a"/>
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/></param>
		/// <param name="a">The vector to be summed of <paramref name="type"/></param>
		/// <param name="N">The number of elements to be operated</param>
		/// <param name="stride">The spacing between consecutive elements of <paramref name="a"/></param>
		/// <returns>The sum as a <see cref="double"/></returns>
		[DllImport(Cuda.NativeMethods.CUSTOM_API_DLL_NAME)]
		internal static extern double vecAbsSum(DataType type, void* a, long N, int stride);

		/// <summary>
		/// Compute the 2-norm of the given vector <paramref name="a"/>
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/></param>
		/// <param name="a">The vector to be summed of <paramref name="type"/></param>
		/// <param name="N">The number of elements to be operated</param>
		/// <param name="stride">The spacing between consecutive elements of <paramref name="a"/></param>
		/// <returns>The 2-norm as a <see cref="double"/></returns>
		[DllImport(Cuda.NativeMethods.CUSTOM_API_DLL_NAME)]
		internal static extern double vecNorm(DataType type, void* a, long N, int stride);

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
		[DllImport(Cuda.NativeMethods.CUSTOM_API_DLL_NAME)]
		internal static extern int vecDot(DataType type, void* a, void* b, long N, int strideA, int strideB, void* outProd);

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
		[DllImport(Cuda.NativeMethods.CUSTOM_API_DLL_NAME)]
		internal static extern int vecDotc(DataType type, void* a, void* b, long N, int strideA, int strideB, void* outProd);

		/// <summary>
		/// Multiplies all the elements in vector <paramref name="a"/>
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/></param>
		/// <param name="a">The vector to be multiplied of <paramref name="type"/></param>
		/// <param name="N">The number of elements to be operated</param>
		/// <param name="stride">The spacing between consecutive elements of <paramref name="a"/></param>
		/// <param name="outProd">The output product as a pointer of <paramref name="type"/></param>
		[DllImport(Cuda.NativeMethods.CUSTOM_API_DLL_NAME)]
		internal static extern int vecProd(DataType type, void* a, long N, int stride, void* outProd);

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
		[DllImport(Cuda.NativeMethods.CUSTOM_API_DLL_NAME)]
		internal static extern int vecParSum(DataType type, void* src, void* dst, long N, bool inclusive, int strideSrc, int strideDst);

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
		[DllImport(Cuda.NativeMethods.CUSTOM_API_DLL_NAME)]
		internal static extern int vecParProd(DataType type, void* src, void* dst, long N, bool inclusive, int strideSrc, int strideDst);

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
		[DllImport(Cuda.NativeMethods.CUSTOM_API_DLL_NAME)]
		internal static extern void matKron(DataType type,
											void* A, long ldA, long rowsA, long colsA,
											void* B, long ldB, long rowsB, long colsB,
											void* dest, long ldD, void* alpha, void* beta);

		/// <summary>
		/// Makes the matrix <paramref name="A"/> hermitian or symmetric by copying its upper part to/from its lower part
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="A"/></param>
		/// <param name="A">The matrix to be modified of <paramref name="type"/></param>
		/// <param name="ld">The leading dimension of <paramref name="A"/>, must be at least <paramref name="rows"/></param>
		/// <param name="rows">The number of rows of <paramref name="A"/></param>
		/// <param name="upperStored">Whether <paramref name="A"/>'s upper part or its lower part is stored</param>
		/// <param name="hermA">If <paramref name="type"/> is a complex type, make <paramref name="A"/> hermitian or symmetric</param>
		[DllImport(Cuda.NativeMethods.CUSTOM_API_DLL_NAME)]
		internal static extern void matMakeHerm(DataType type, void* A, long ld, long rows, bool upperStored, bool hermA);

		/// <summary>
		/// Clear (set to 0) the matrix <paramref name="A"/>'s upper part or its lower part
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="A"/></param>
		/// <param name="A">The matrix to be modified of <paramref name="type"/></param>
		/// <param name="ld">The leading dimension of <paramref name="A"/>, must be at least <paramref name="rows"/></param>
		/// <param name="rows">The number of rows of <paramref name="A"/></param>
		/// <param name="clearLower">Whether <paramref name="A"/>'s upper part or its lower part shall be preserved</param>
		[DllImport(Cuda.NativeMethods.CUSTOM_API_DLL_NAME)]
		internal static extern void matTriClear(DataType type, void* A, long ld, long rows, bool clearLower);
		#endregion

		#region solver
		#region create and destroy
		/// <summary>
		/// This function initializes the cuSolverDN library and creates a handle on the cuSolverDN
		/// context. It must be called before any other cuSolverDN API function is invoked. It
		/// allocates hardware resources necessary for accessing the GPU
		/// </summary>
		/// <param name="handle">the pointer to the handle to the cuSolverDN context.</param>
		[DllImport(Cuda.NativeMethods.CUSOLVER_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnCreate(out IntPtr handle);

		/// <summary>
		/// This function releases CPU-side resources used by the cuSolverDN library.
		/// </summary>
		/// <param name="handle">handle to the cuSolverDN library context.</param>
		[DllImport(Cuda.NativeMethods.CUSOLVER_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnDestroy(IntPtr handle);
		#endregion

		#region implicit QR
		[DllImport(Cuda.NativeMethods.CUSOLVER_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnXgeqrf_bufferSize(IntPtr handle, void* @params, long m, long n, CudaDataType dataTypeA, void* A, long lda, CudaDataType dataTypeTau, void* tau, CudaDataType computeType, out long workspaceInBytesOnDevice, out long workspaceInBytesOnHost);

		[DllImport(Cuda.NativeMethods.CUSOLVER_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnXgeqrf(IntPtr handle, void* @params, long m, long n, CudaDataType dataTypeA, void* A, long lda, CudaDataType dataTypeTau, void* tau, CudaDataType computeType, void* bufferOnDevice, long workspaceInBytesOnDevice, byte[] bufferOnHost, long workspaceInBytesOnHost, void* devInfo);
		#endregion

		#region LU Factorization
		[DllImport(Cuda.NativeMethods.CUSOLVER_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnXgetrf_bufferSize(IntPtr handle, void* @params, long m, long n, CudaDataType dataTypeA, void* A, long lda, CudaDataType computeType, out long workspaceInBytesOnDevice, out long workspaceInBytesOnHost);

		[DllImport(Cuda.NativeMethods.CUSOLVER_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnXgetrf(IntPtr handle, void* @params, long m, long n, CudaDataType dataTypeA, void* A, long lda, void* ipiv, CudaDataType computeType, void* bufferOnDevice, long workspaceInBytesOnDevice, byte[] bufferOnHost, long workspaceInBytesOnHost, void* devInfo);
		#endregion

		#region LU solve
		[DllImport(Cuda.NativeMethods.CUSOLVER_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnXgetrs(IntPtr handle, void* @params, CuBlasOperation trans, long n, long nrhs, CudaDataType dataTypeA, void* A, long lda, void* ipiv, CudaDataType dataTypeB, void* B, long ldb, void* devInfo);
		#endregion

		#region standard symmetric (Hermitian) eigen-solve
		[DllImport(Cuda.NativeMethods.CUSOLVER_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnXsyevd_bufferSize(IntPtr handle, void* @params, CuSolverEigMode jobz, CuBlasFillMode uplo, long n, CudaDataType dataTypeA, void* A, long lda, CudaDataType dataTypeW, void* W, CudaDataType computeType, out long workspaceInBytesOnDevice, out long workspaceInBytesOnHost);

		[DllImport(Cuda.NativeMethods.CUSOLVER_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnXsyevd(IntPtr handle, void* @params, CuSolverEigMode jobz, CuBlasFillMode uplo, long n, CudaDataType dataTypeA, void* A, long lda, CudaDataType dataTypeW, void* W, CudaDataType computeType, void* bufferOnDevice, long workspaceInBytesOnDevice, byte[] bufferOnHost, long workspaceInBytesOnHost, void* devInfo);
		#endregion
		
		#region singular value decomposition
		[DllImport(Cuda.NativeMethods.CUSOLVER_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnXgesvd_bufferSize(IntPtr handle, void* @params, sbyte jobu, sbyte jobvt, long m, long n, CudaDataType dataTypeA, void* A, long lda, CudaDataType dataTypeS, void* S, CudaDataType dataTypeU, void* U, long ldu, CudaDataType dataTypeVT, void* VT, long ldvt, CudaDataType computeType, out long workspaceInBytesOnDevice, out long workspaceInBytesOnHost);

		[DllImport(Cuda.NativeMethods.CUSOLVER_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnXgesvd(IntPtr handle, void* @params, sbyte jobu, sbyte jobvt, long m, long n, CudaDataType dataTypeA, void* A, long lda, CudaDataType dataTypeS, void* S, CudaDataType dataTypeU, void* U, long ldu, CudaDataType dataTypeVT, void* VT, long ldvt, CudaDataType computeType, void* bufferOnDevice, long workspaceInBytesOnDevice, byte[] bufferOnHost, long workspaceInBytesOnHost, void* devInfo);


		[DllImport(Cuda.NativeMethods.CUSOLVER_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnXgesvdp_bufferSize(IntPtr handle, void* @params, CuSolverEigMode jobz, int econ, long m, long n, CudaDataType dataTypeA, void* A, long lda, CudaDataType dataTypeS, void* S, CudaDataType dataTypeU, void* U, long ldu, CudaDataType dataTypeV, void* V, long ldv, CudaDataType computeType, out long workspaceInBytesOnDevice, out long workspaceInBytesOnHost);

		[DllImport(Cuda.NativeMethods.CUSOLVER_API_DLL_NAME)]
		internal static extern CudaSolverStatus cusolverDnXgesvdp(IntPtr handle, void* @params, CuSolverEigMode jobz, int econ, long m, long n, CudaDataType dataTypeA, void* A, long lda, CudaDataType dataTypeS, void* S, CudaDataType dataTypeU, void* U, long ldu, CudaDataType dataTypeV, void* V, long ldv, CudaDataType computeType, void* bufferOnDevice, long workspaceInBytesOnDevice, byte[] bufferOnHost, long workspaceInBytesOnHost, void* devInfo, out double error);
		#endregion
		#endregion
	}
}
