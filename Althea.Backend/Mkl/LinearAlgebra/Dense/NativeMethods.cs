using System.Runtime.InteropServices;

using Althea.LinearAlgebra;
using Althea.SourceGenerator;


namespace Althea.Backend.Mkl.LinearAlgebra.Dense
{
	[NativeMethodClass]
	internal static unsafe class NativeMethodsTemplate
	{
		#region level 1
		[NativeMethod(7)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern ulong cblas_isamax(MklInt n, void* x, MklInt incx);

		[NativeMethod(7)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern ulong cblas_isamin(MklInt n, void* x, MklInt incx);

		[NativeMethod(6, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern Float32 cblas_sasum(MklInt n, void* x, MklInt incx);

		[NativeMethod(6, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern Float32 cblas_snrm2(MklInt n, void* x, MklInt incx);

		[NativeMethod(6, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_saxpy(MklInt n, Float32 alpha, Float32* x, MklInt incx, Float32* y, MklInt incy);

		[NativeMethod(6, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_sscal(MklInt n, Float32 alpha, Float32* x, MklInt incx);

		[NativeMethod(6)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_scopy(MklInt n, void* x, MklInt incx, void* y, MklInt incy);

		[NativeMethod(6)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_sswap(MklInt n, void* x, MklInt incx, void* y, MklInt incy);

		// Ignore Spelling: sdot ddot
		[CustomNativeMethod(6, "Float32", "sdot")]
		[CustomNativeMethod(6, "Float64", "ddot")]
		[CustomNativeMethod(6, "Complex<Float32>", "cdotu_sub", "", "Complex<Float32>", true)]
		[CustomNativeMethod(6, "Complex<Float32>", "cdotc_sub", "", "Complex<Float32>", true)]
		[CustomNativeMethod(6, "Complex<Float64>", "zdotu_sub", "", "Complex<Float64>", true)]
		[CustomNativeMethod(6, "Complex<Float64>", "zdotc_sub", "", "Complex<Float64>", true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern Float32 cblas_sdot(MklInt n, Float32* x, MklInt incx, Float32* y, MklInt incy);

		internal delegate T cblas_dot<T>(MklInt n, T* x, MklInt incx, T* y, MklInt incy) where T : unmanaged;
		internal delegate void cblas_dot_comp<T>(MklInt n, T* x, MklInt incx, T* y, MklInt incy, out T dot) where T : unmanaged;
		#endregion

		#region level 2
		[NativeMethod(6, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_sgemv(MklMatrixLayout Layout, MklOperation trans, MklInt m, MklInt n, Float32 alpha, Float32* A, MklInt lda, Float32* x, MklInt incx, Float32 beta, Float32* y, MklInt incy);

		[NativeMethod(6, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_ssymv(MklMatrixLayout Layout, MklFillMode uplo, MklInt n, Float32 alpha, Float32* A, MklInt lda, Float32* x, MklInt incx, Float32 beta, Float32* y, MklInt incy);

		[NativeMethod(6, false, true, false, false)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_chemv(MklMatrixLayout Layout, MklFillMode uplo, MklInt n, Complex<Float32> alpha, Complex<Float32>* A, MklInt lda, Complex<Float32>* x, MklInt incx, Complex<Float32> beta, Complex<Float32>* y, MklInt incy);

		[NativeMethod(6)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_strmv(MklMatrixLayout Layout, MklFillMode Uplo, MklOperation TransA, MklBlasDiagType Diag, MklInt n, void* A, MklInt lda, void* x, MklInt incx);

		// Ignore Spelling: sger dger cgerc cgeru zgerc zgeru
		[CustomNativeMethod(6, "Float32", "sger")]
		[CustomNativeMethod(6, "Float64", "dger")]
		[CustomNativeMethod(6, "Complex<Float32>", "cgerc", "in", "Complex<Float32>")]
		[CustomNativeMethod(6, "Complex<Float32>", "cgeru", "in", "Complex<Float32>")]
		[CustomNativeMethod(6, "Complex<Float64>", "zgeru", "in", "Complex<Float64>")]
		[CustomNativeMethod(6, "Complex<Float64>", "zgerc", "in", "Complex<Float64>")]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_sger(MklMatrixLayout Layout, MklInt m, MklInt n, Float32 alpha, Float32* x, MklInt incx, Float32* y, MklInt incy, Float32* A, MklInt lda);

		internal delegate void cblas_ger<T>(MklMatrixLayout Layout, MklInt m, MklInt n, T alpha, T* x, MklInt incx, T* y, MklInt incy, T* A, MklInt lda) where T : unmanaged;
		internal delegate void cblas_ger_comp<T>(MklMatrixLayout Layout, MklInt m, MklInt n, in T alpha, T* x, MklInt incx, T* y, MklInt incy, T* A, MklInt lda) where T : unmanaged;

		[NativeMethod(6, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_ssyr(MklMatrixLayout Layout, MklFillMode uplo, MklInt n, Float32 alpha, Float32* x, MklInt incx, Float32* A, MklInt lda);

		[NativeMethod(6, false, true, false, false)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_cher(MklMatrixLayout Layout, MklFillMode uplo, MklInt n, Complex<Float32> alpha, Complex<Float32>* x, MklInt incx, Complex<Float32>* A, MklInt lda);

		[NativeMethod(6, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_ssyr2(MklMatrixLayout Layout, MklFillMode uplo, MklInt n, Float32 alpha, Float32* x, MklInt incx, Float32* y, MklInt incy, Float32* A, MklInt lda);

		[NativeMethod(6, false, true, false, false)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_cher2(MklMatrixLayout Layout, MklFillMode uplo, MklInt n, Complex<Float32> alpha, Complex<Float32>* x, MklInt incx, Complex<Float32>* y, MklInt incy, Complex<Float32>* A, MklInt lda);

		#endregion

		#region level 3
		[NativeMethod(6, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_strsm(MklMatrixLayout Layout, MklBlasSideMode Side, MklFillMode Uplo, MklOperation TransA, MklBlasDiagType Diag, MklInt m, MklInt n, Float32 alpha, Float32* A, MklInt lda, Float32* B, MklInt ldb);

		[NativeMethod(6, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_strmm(MklMatrixLayout Layout, MklBlasSideMode Side, MklFillMode Uplo, MklOperation TransA, MklBlasDiagType Diag, MklInt m, MklInt n, Float32 alpha, Float32* A, MklInt lda, Float32* B, MklInt ldb);

		[NativeMethod(6, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_sgemm(MklMatrixLayout Layout, MklOperation TransA, MklOperation TransB, MklInt m, MklInt n, MklInt k, Float32 alpha, Float32* A, MklInt lda, Float32* B, MklInt ldb, Float32 beta, Float32* C, MklInt ldc);

		[NativeMethod(6, false, true, false, false)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_cgemm3m(MklMatrixLayout Layout, MklOperation TransA, MklOperation TransB, MklInt m, MklInt n, MklInt k, Complex<Float32> alpha, Complex<Float32>* A, MklInt lda, Complex<Float32>* B, MklInt ldb, Complex<Float32> beta, Complex<Float32>* C, MklInt ldc);

		[NativeMethod(15, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void mkl_jit_create_sgemm(out IntPtr jitter, MklMatrixLayout Layout, MklOperation TransA, MklOperation TransB, MklInt m, MklInt n, MklInt k, Float32 alpha, MklInt lda, MklInt ldb, Float32 beta, MklInt ldc);

		[NativeMethod(12)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern delegate* unmanaged<IntPtr, void*, void*, void*, void> mkl_jit_get_sgemm_ptr(IntPtr jitter);

		[CustomNativeMethod(0, "Float32", "m")]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklJitStatus mkl_jit_destroy(IntPtr jitter);

		[NativeMethod(6, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_ssymm(MklMatrixLayout Layout, MklBlasSideMode side, MklFillMode uplo, MklInt m, MklInt n, Float32 alpha, Float32* A, MklInt lda, Float32* B, MklInt ldb, Float32 beta, Float32* C, MklInt ldc);

		[NativeMethod(6, false, true, false, false)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_chemm(MklMatrixLayout Layout, MklBlasSideMode side, MklFillMode uplo, MklInt m, MklInt n, Complex<Float32> alpha, Complex<Float32>* A, MklInt lda, Complex<Float32>* B, MklInt ldb, Complex<Float32> beta, Complex<Float32>* C, MklInt ldc);

		[NativeMethod(6, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_ssyrk(MklMatrixLayout Layout, MklFillMode uplo, MklOperation trans, MklInt n, MklInt k, Float32 alpha, Float32* A, MklInt lda, Float32 beta, Float32* C, MklInt ldc);

		[NativeMethod(6, false, true, false, false)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_cherk(MklMatrixLayout Layout, MklFillMode uplo, MklOperation trans, MklInt n, MklInt k, Complex<Float32> alpha, Complex<Float32>* A, MklInt lda, Complex<Float32> beta, Complex<Float32>* C, MklInt ldc);

		[NativeMethod(6, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_ssyr2k(MklMatrixLayout Layout, MklFillMode uplo, MklOperation trans, MklInt n, MklInt k, Float32 alpha, Float32* A, MklInt lda, Float32* B, MklInt ldb, Float32 beta, Float32* C, MklInt ldc);

		[NativeMethod(6, false, true, false, false)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_cher2k(MklMatrixLayout Layout, MklFillMode uplo, MklOperation trans, MklInt n, MklInt k, Complex<Float32> alpha, Complex<Float32>* A, MklInt lda, Complex<Float32>* B, MklInt ldb, Complex<Float32> beta, Complex<Float32>* C, MklInt ldc);
		#endregion

		#region BLAS like
		[NativeMethod(6)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_sdgmm_batch_strided(MklMatrixLayout layout, MklBlasSideMode side, MklInt m, MklInt n, void* A, MklInt lda, MklInt strideA, void* x, MklInt incx, MklInt strideX, void* C, MklInt ldc, MklInt strideC, MklInt batch_size);

		[NativeMethod(4, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void MKL_Somatadd(MklMatrixLayoutChar ordering, MklOperationChar transa, MklOperationChar transb, MklInt rows, MklInt cols, Float32 alpha, Float32* A, MklInt lda, Float32 beta, Float32* B, MklInt ldb, Float32* C, MklInt ldc);

		[NativeMethod(4, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void MKL_Somatcopy(MklMatrixLayoutChar ordering, MklOperationChar trans, MklInt rows, MklInt cols, Float32 alpha, Float32* A, MklInt lda, Float32* B, MklInt ldb);

		[NativeMethod(4, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void MKL_Simatcopy(MklMatrixLayoutChar ordering, MklOperationChar trans, MklInt rows, MklInt cols, Float32 alpha, Float32* A, MklInt lda);
		#endregion

		#region vector math
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		[return: MarshalAs(UnmanagedType.FunctionPtr)]
		internal static extern VmlErrorCallbackDelegate? vmlGetErrorCallBack();

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		[return: MarshalAs(UnmanagedType.FunctionPtr)]
		internal static extern VmlErrorCallbackDelegate? vmlClearErrorCallBack();

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		[return: MarshalAs(UnmanagedType.FunctionPtr)]
		internal static extern VmlErrorCallbackDelegate? vmlSetErrorCallBack([MarshalAs(UnmanagedType.FunctionPtr)] VmlErrorCallbackDelegate newCallback);

		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsAdd(MklInt n, void* a, void* b, void* y);

		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsAddI(MklInt n, void* a, MklInt inca, void* b, MklInt incb, void* y, MklInt incy);

		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsSub(MklInt n, void* a, void* b, void* y);

		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsSubI(MklInt n, void* a, MklInt inca, void* b, MklInt incb, void* y, MklInt incy);

		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsMul(MklInt n, void* a, void* b, void* y);

		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsMulI(MklInt n, void* a, MklInt inca, void* b, MklInt incb, void* y, MklInt incy);

		[NativeMethod(1, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsFmax(MklInt n, void* a, void* b, void* y);

		[NativeMethod(1, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsFmaxI(MklInt n, void* a, MklInt inca, void* b, MklInt incb, void* y, MklInt incy);

		[NativeMethod(1, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsFmin(MklInt n, void* a, void* b, void* y);

		[NativeMethod(1, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsFminI(MklInt n, void* a, MklInt inca, void* b, MklInt incb, void* y, MklInt incy);

		[NativeMethod(1, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsMaxMag(MklInt n, void* a, void* b, void* y);

		[NativeMethod(1, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsMaxMagI(MklInt n, void* a, MklInt inca, void* b, MklInt incb, void* y, MklInt incy);

		[NativeMethod(1, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsMinMag(MklInt n, void* a, void* b, void* y);

		[NativeMethod(1, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsMinMagI(MklInt n, void* a, MklInt inca, void* b, MklInt incb, void* y, MklInt incy);

		[NativeMethod(1, false, false, false, false)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vcConj(MklInt n, void* a, void* y);

		[NativeMethod(1, false, false, false, false)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vcConjI(MklInt n, void* a, MklInt inca, void* y, MklInt incy);

		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsAbs(MklInt n, void* a, void* y);

		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsAbsI(MklInt n, void* a, MklInt inca, void* y, MklInt incy);

		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsDiv(MklInt n, void* a, void* b, void* y);

		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsDivI(MklInt n, void* a, MklInt inca, void* b, MklInt incb, void* y, MklInt incy);

		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsSqrt(MklInt n, void* a, void* y);

		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsSqrtI(MklInt n, void* a, MklInt inca, void* y, MklInt incy);

		[NativeMethod(1, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsInv(MklInt n, void* a, void* y);

		[NativeMethod(1, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsInvI(MklInt n, void* a, MklInt inca, void* y, MklInt incy);

		[NativeMethod(1, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsInvSqrt(MklInt n, void* a, void* y);

		[NativeMethod(1, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsInvSqrtI(MklInt n, void* a, MklInt inca, void* y, MklInt incy);

		[NativeMethod(1, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsCbrt(MklInt n, void* a, void* y);

		[NativeMethod(1, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsCbrtI(MklInt n, void* a, MklInt inca, void* y, MklInt incy);

		[NativeMethod(1, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsInvCbrt(MklInt n, void* a, void* y);

		[NativeMethod(1, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsInvCbrtI(MklInt n, void* a, MklInt inca, void* y, MklInt incy);

		[NativeMethod(1, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsPow2o3(MklInt n, void* a, void* y);

		[NativeMethod(1, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsPow2o3I(MklInt n, void* a, MklInt inca, void* y, MklInt incy);

		[NativeMethod(1, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsPow3o2(MklInt n, void* a, void* y);

		[NativeMethod(1, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsPow3o2I(MklInt n, void* a, MklInt inca, void* y, MklInt incy);

		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsPow(MklInt n, void* a, void* b, void* y);

		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsPowI(MklInt n, void* a, MklInt inca, void* b, MklInt incb, void* y, MklInt incy);

		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsPowx(MklInt n, Float32* a, Float32 b, Float32* y);

		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsPowxI(MklInt n, Float32* a, MklInt inca, Float32 b, Float32* y, MklInt incy);

		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsExp(MklInt n, void* a, void* y);

		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsExpI(MklInt n, void* a, MklInt inca, void* y, MklInt incy);

		[NativeMethod(1, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsExp2(MklInt n, void* a, void* y);

		[NativeMethod(1, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsExp2I(MklInt n, void* a, MklInt inca, void* y, MklInt incy);

		[NativeMethod(1, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsExp10(MklInt n, void* a, void* y);

		[NativeMethod(1, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsExp10I(MklInt n, void* a, MklInt inca, void* y, MklInt incy);

		[NativeMethod(1, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsExpm1(MklInt n, void* a, void* y);

		[NativeMethod(1, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsExpm1I(MklInt n, void* a, MklInt inca, void* y, MklInt incy);

		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsLn(MklInt n, void* a, void* y);

		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsLnI(MklInt n, void* a, MklInt inca, void* y, MklInt incy);

		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsLog10(MklInt n, void* a, void* y);

		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsLog10I(MklInt n, void* a, MklInt inca, void* y, MklInt incy);

		[NativeMethod(1, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsLog2(MklInt n, void* a, void* y);

		[NativeMethod(1, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsLog2I(MklInt n, void* a, MklInt inca, void* y, MklInt incy);

		[NativeMethod(1, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsLog1p(MklInt n, void* a, void* y);

		[NativeMethod(1, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsLog1pI(MklInt n, void* a, MklInt inca, void* y, MklInt incy);

		[NativeMethod(1, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsLogb(MklInt n, void* a, void* y);

		[NativeMethod(1, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsLogbI(MklInt n, void* a, MklInt inca, void* y, MklInt incy);

		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsCos(MklInt n, void* a, void* y);

		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsCosI(MklInt n, void* a, MklInt inca, void* y, MklInt incy);

		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsSin(MklInt n, void* a, void* y);

		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsSinI(MklInt n, void* a, MklInt inca, void* y, MklInt incy);

		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsTan(MklInt n, void* a, void* y);

		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsTanI(MklInt n, void* a, MklInt inca, void* y, MklInt incy);

		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsAcos(MklInt n, void* a, void* y);

		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsAcosI(MklInt n, void* a, MklInt inca, void* y, MklInt incy);

		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsAsin(MklInt n, void* a, void* y);

		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsAsinI(MklInt n, void* a, MklInt inca, void* y, MklInt incy);

		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsAtan(MklInt n, void* a, void* y);

		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsAtanI(MklInt n, void* a, MklInt inca, void* y, MklInt incy);

		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsCosh(MklInt n, void* a, void* y);

		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsCoshI(MklInt n, void* a, MklInt inca, void* y, MklInt incy);

		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsSinh(MklInt n, void* a, void* y);

		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsSinhI(MklInt n, void* a, MklInt inca, void* y, MklInt incy);

		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsTanh(MklInt n, void* a, void* y);

		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsTanhI(MklInt n, void* a, MklInt inca, void* y, MklInt incy);

		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsAcosh(MklInt n, void* a, void* y);

		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsAcoshI(MklInt n, void* a, MklInt inca, void* y, MklInt incy);

		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsAsinh(MklInt n, void* a, void* y);

		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsAsinhI(MklInt n, void* a, MklInt inca, void* y, MklInt incy);

		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsAtanh(MklInt n, void* a, void* y);

		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsAtanhI(MklInt n, void* a, MklInt inca, void* y, MklInt incy);
		#endregion;

		#region solver
		// LU factorize A
		[NativeMethod(8)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_sgetrf(MklMatrixLayout matrix_layout, MklInt m, MklInt n, void* A, MklInt lda, MklInt* ipiv);

		// solve A*X=B with LU factorized A
		[NativeMethod(8)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_sgetrs(MklMatrixLayout matrix_layout, MklOperationChar trans, MklInt n, MklInt nrhs, void* A, MklInt lda, MklInt* ipiv, void* B, MklInt ldb);

		// direct solve A*X=B and LU factorize A
		[NativeMethod(8)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_sgesv(MklMatrixLayout matrix_layout, MklInt n, MklInt nrhs, void* a, MklInt lda, MklInt* ipiv, void* b, MklInt ldb);

		// QR factorize
		[NativeMethod(8)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_sgeqrf(MklMatrixLayout matrix_layout, MklInt m, MklInt n, void* a, MklInt lda, void* tau);

		// QR generate Q
		[NativeMethod(8, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_sorgqr(MklMatrixLayout matrix_layout, MklInt m, MklInt n, MklInt k, void* a, MklInt lda, void* tau);

		[NativeMethod(8, false, false, false, false)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_cungqr(MklMatrixLayout matrix_layout, MklInt m, MklInt n, MklInt k, void* a, MklInt lda, void* tau);

		// least square solve
		[NativeMethod(8)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_sgels(MklMatrixLayout matrix_layout, MklOperationChar trans, MklInt m, MklInt n, MklInt nrhs, void* a, MklInt lda, void* b, MklInt ldb);

		// symmetric/Hermitian eigen
		[NativeMethod(8, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_ssyev(MklMatrixLayout matrix_layout, MklVectorModeChar jobz, MklFillModeChar uplo, MklInt n, void* A, MklInt lda, void* w);

		[NativeMethod(8, false, false, false, false)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_cheev(MklMatrixLayout matrix_layout, MklVectorModeChar jobz, MklFillModeChar uplo, MklInt n, void* A, MklInt lda, void* w);

		// general symmetric/hermitian positive-definite eigen
		[NativeMethod(8, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_ssygv(MklMatrixLayout matrix_layout, GeneralEigenType itype, MklVectorModeChar jobz, MklFillModeChar uplo, MklInt n, void* a, MklInt lda, void* b, MklInt ldb, void* w);

		[NativeMethod(8, false, false, false, false)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_chegv(MklMatrixLayout matrix_layout, GeneralEigenType itype, MklVectorModeChar jobz, MklFillModeChar uplo, MklInt n, void* a, MklInt lda, void* b, MklInt ldb, void* w);

		// non-symmetric eigen
		[NativeMethod(8, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_sgeev(MklMatrixLayout matrix_layout, MklVectorModeChar jobvl, MklVectorModeChar jobvr, MklInt n, void* A, MklInt lda, void* wr, void* wi, void* Vl, MklInt ldvl, void* Vr, MklInt ldvr);

		[NativeMethod(8, false, false, false, false)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_cgeev(MklMatrixLayout matrix_layout, MklVectorModeChar jobvl, MklVectorModeChar jobvr, MklInt n, void* A, MklInt lda, void* w, void* Vl, MklInt ldvl, void* Vr, MklInt ldvr);

		// general non-symmetric eigen
		[NativeMethod(8, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_sggev(MklMatrixLayout matrix_layout, MklVectorModeChar jobvl, MklVectorModeChar jobvr, MklInt n, void* a, MklInt lda, void* b, MklInt ldb, void* alphar, void* alphai, void* beta, void* vl, MklInt ldvl, void* vr, MklInt ldvr);

		[NativeMethod(8, false, false, false, false)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_cggev(MklMatrixLayout matrix_layout, MklVectorModeChar jobvl, MklVectorModeChar jobvr, MklInt n, void* a, MklInt lda, void* b, MklInt ldb, void* alpha, void* beta, void* vl, MklInt ldvl, void* vr, MklInt ldvr);

		// SVD
		[NativeMethod(8)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_sgesvd(MklMatrixLayout matrix_layout, MklSvdModeChar jobu, MklSvdModeChar jobvt, MklInt m, MklInt n, void* A, MklInt lda, void* S, void* U, MklInt ldu, void* Vt, MklInt ldvt, void* superb);

		// direct Schur
		[NativeMethod(8, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_sgees(MklMatrixLayout matrix_layout, MklVectorModeChar jobv, MklSortModeChar sort, delegate* unmanaged<void*, void*, MklInt> selectFunc, MklInt n, void* A, MklInt lda, out MklInt selected, void* wr, void* wi, void* V, MklInt ldv);

		[NativeMethod(8, false, false, false, false)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_cgees(MklMatrixLayout matrix_layout, MklVectorModeChar jobv, MklSortModeChar sort, delegate* unmanaged<void*, MklInt> selectFunc, MklInt n, void* A, MklInt lda, out MklInt selected, void* w, void* V, MklInt ldv);

		// Hessenberg form Schur, iLow == 1 && iHigh == n
		[NativeMethod(8, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_shseqr(MklMatrixLayout matrix_layout, MklSchurModeChar job, MklVectorModeChar compz, MklInt n, MklInt ilo, MklInt ihi, void* H, MklInt ldh, void* wr, void* wi, void* Z, MklInt ldz);

		[NativeMethod(8, false, false, false, false)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_chseqr(MklMatrixLayout matrix_layout, MklSchurModeChar job, MklVectorModeChar compz, MklInt n, MklInt ilo, MklInt ihi, void* H, MklInt ldh, void* w, void* Z, MklInt ldz);

		// eigenvectors from Schur, mm == n
		[NativeMethod(8)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_strevc(MklMatrixLayout matrix_layout, MklSchurEigenvectorModeChar side, MklSchurEigenSelectModeChar howmny, MklInt* select, MklInt n, void* T, MklInt ldt, void* Vl, MklInt ldvl, void* Vr, MklInt ldvr, MklInt mm, out MklInt selected);

		// reorder Schur
		[NativeMethod(8, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_strsen(MklMatrixLayout matrix_layout, MklSchurReorderConditionNumberModeChar job, MklVectorModeChar compq, MklInt* select, MklInt n, void* T, MklInt ldt, void* Q, MklInt ldq, void* wr, void* wi, out MklInt selected, void* s = null, void* sep = null);

		[NativeMethod(8, false, false, false, false)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_ctrsen(MklMatrixLayout matrix_layout, MklSchurReorderConditionNumberModeChar job, MklVectorModeChar compq, MklInt* select, MklInt n, void* T, MklInt ldt, void* Q, MklInt ldq, void* w, out MklInt selected, void* s = null, void* sep = null);
		#endregion
	}
}
