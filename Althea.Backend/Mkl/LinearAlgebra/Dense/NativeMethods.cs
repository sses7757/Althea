using System.Runtime.InteropServices;

using Althea.LinearAlgebra;
using Althea.NativeTypes;
using Althea.SourceGenerator;


namespace Althea.Backend.Mkl.LinearAlgebra.Dense
{
	[NativeMethodClass]
	internal static unsafe class NativeMethodsTemplate
	{
		#region level 1
		[NativeMethod(7)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern long cblas_isamax(long n, void* x, long incx);

		[NativeMethod(7)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern long cblas_isamin(long n, void* x, long incx);

		[NativeMethod(6, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern float cblas_sasum(long n, void* x, long incx);

		[NativeMethod(6, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern float cblas_snrm2(long n, void* x, long incx);

		[NativeMethod(6, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_saxpy(long n, float alpha, float* x, long incx, float* y, long incy);

		[NativeMethod(6, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_sscal(long n, float alpha, float* x, long incx);

		[NativeMethod(6)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_scopy(long n, void* x, long incx, void* y, long incy);

		[NativeMethod(6)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_sswap(long n, void* x, long incx, void* y, long incy);

		// Ignore Spelling: sdot ddot
		[CustomNativeMethod(6, "float", "sdot")]
		[CustomNativeMethod(6, "double", "ddot")]
		[CustomNativeMethod(6, "Complex<float>", "cdotu_sub", "", "Complex<float>", true)]
		[CustomNativeMethod(6, "Complex<float>", "cdotc_sub", "", "Complex<float>", true)]
		[CustomNativeMethod(6, "Complex<double>", "zdotu_sub", "", "Complex<double>", true)]
		[CustomNativeMethod(6, "Complex<double>", "zdotc_sub", "", "Complex<double>", true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern float cblas_sdot(long n, float* x, long incx, float* y, long incy);

		internal delegate T cblas_dot<T>(long n, T* x, long incx, T* y, long incy) where T : unmanaged;
		internal delegate void cblas_dot_comp<T>(long n, T* x, long incx, T* y, long incy, out T dot) where T : unmanaged;
		#endregion

		#region level 2
		[NativeMethod(6, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_sgemv(MklMatrixLayout Layout, MklOperation trans, long m, long n, float alpha, float* A, long lda, float* x, long incx, float beta, float* y, long incy);

		[NativeMethod(6, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_ssymv(MklMatrixLayout Layout, MklFillMode uplo, long n, float alpha, float* A, long lda, float* x, long incx, float beta, float* y, long incy);

		[NativeMethod(6, false, true, false, false)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_chemv(MklMatrixLayout Layout, MklFillMode uplo, long n, Complex<float> alpha, Complex<float>* A, long lda, Complex<float>* x, long incx, Complex<float> beta, Complex<float>* y, long incy);

		[NativeMethod(6)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_strmv(MklMatrixLayout Layout, MklFillMode Uplo, MklOperation TransA, MklBlasDiagType Diag, long n, void* A, long lda, void* x, long incx);

		// Ignore Spelling: sger dger cgerc cgeru zgerc zgeru
		[CustomNativeMethod(6, "float", "sger")]
		[CustomNativeMethod(6, "double", "dger")]
		[CustomNativeMethod(6, "Complex<float>", "cgerc", "in", "Complex<float>")]
		[CustomNativeMethod(6, "Complex<float>", "cgeru", "in", "Complex<float>")]
		[CustomNativeMethod(6, "Complex<double>", "zgeru", "in", "Complex<double>")]
		[CustomNativeMethod(6, "Complex<double>", "zgerc", "in", "Complex<double>")]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_sger(MklMatrixLayout Layout, long m, long n, float alpha, float* x, long incx, float* y, long incy, float* A, long lda);

		internal delegate void cblas_ger<T>(MklMatrixLayout Layout, long m, long n, T alpha, T* x, long incx, T* y, long incy, T* A, long lda) where T : unmanaged;
		internal delegate void cblas_ger_comp<T>(MklMatrixLayout Layout, long m, long n, in T alpha, T* x, long incx, T* y, long incy, T* A, long lda) where T : unmanaged;

		[NativeMethod(6, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_ssyr(MklMatrixLayout Layout, MklFillMode uplo, long n, float alpha, float* x, long incx, float* A, long lda);

		[NativeMethod(6, false, true, false, false)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_cher(MklMatrixLayout Layout, MklFillMode uplo, long n, Complex<float> alpha, Complex<float>* x, long incx, Complex<float>* A, long lda);

		[NativeMethod(6, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_ssyr2(MklMatrixLayout Layout, MklFillMode uplo, long n, float alpha, float* x, long incx, float* y, long incy, float* A, long lda);

		[NativeMethod(6, false, true, false, false)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_cher2(MklMatrixLayout Layout, MklFillMode uplo, long n, Complex<float> alpha, Complex<float>* x, long incx, Complex<float>* y, long incy, Complex<float>* A, long lda);

		#endregion

		#region level 3
		[NativeMethod(6, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_strsm(MklMatrixLayout Layout, MklBlasSideMode Side, MklFillMode Uplo, MklOperation TransA, MklBlasDiagType Diag, long m, long n, float alpha, float* A, long lda, float* B, long ldb);

		[NativeMethod(6, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_strmm(MklMatrixLayout Layout, MklBlasSideMode Side, MklFillMode Uplo, MklOperation TransA, MklBlasDiagType Diag, long m, long n, float alpha, float* A, long lda, float* B, long ldb);

		[NativeMethod(6, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_sgemm(MklMatrixLayout Layout, MklOperation TransA, MklOperation TransB, long m, long n, long k, float alpha, float* A, long lda, float* B, long ldb, float beta, float* C, long ldc);

		[NativeMethod(6, false, true, false, false)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_cgemm3m(MklMatrixLayout Layout, MklOperation TransA, MklOperation TransB, long m, long n, long k, Complex<float> alpha, Complex<float>* A, long lda, Complex<float>* B, long ldb, Complex<float> beta, Complex<float>* C, long ldc);

		[NativeMethod(15, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void mkl_jit_create_sgemm(out IntPtr jitter, MklMatrixLayout Layout, MklOperation TransA, MklOperation TransB, long m, long n, long k, float alpha, long lda, long ldb, float beta, long ldc);

		[NativeMethod(12)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern delegate* unmanaged<IntPtr, void*, void*, void*, void> mkl_jit_get_sgemm_ptr(IntPtr jitter);

		[CustomNativeMethod(0, "float", "m")]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklJitStatus mkl_jit_destroy(IntPtr jitter);

		[NativeMethod(6, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_ssymm(MklMatrixLayout Layout, MklBlasSideMode side, MklFillMode uplo, long m, long n, float alpha, float* A, long lda, float* B, long ldb, float beta, float* C, long ldc);

		[NativeMethod(6, false, true, false, false)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_chemm(MklMatrixLayout Layout, MklBlasSideMode side, MklFillMode uplo, long m, long n, Complex<float> alpha, Complex<float>* A, long lda, Complex<float>* B, long ldb, Complex<float> beta, Complex<float>* C, long ldc);

		[NativeMethod(6, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_ssyrk(MklMatrixLayout Layout, MklFillMode uplo, MklOperation trans, long n, long k, float alpha, float* A, long lda, float beta, float* C, long ldc);

		[NativeMethod(6, false, true, false, false)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_cherk(MklMatrixLayout Layout, MklFillMode uplo, MklOperation trans, long n, long k, Complex<float> alpha, Complex<float>* A, long lda, Complex<float> beta, Complex<float>* C, long ldc);

		[NativeMethod(6, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_ssyr2k(MklMatrixLayout Layout, MklFillMode uplo, MklOperation trans, long n, long k, float alpha, float* A, long lda, float* B, long ldb, float beta, float* C, long ldc);

		[NativeMethod(6, false, true, false, false)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_cher2k(MklMatrixLayout Layout, MklFillMode uplo, MklOperation trans, long n, long k, Complex<float> alpha, Complex<float>* A, long lda, Complex<float>* B, long ldb, Complex<float> beta, Complex<float>* C, long ldc);
		#endregion

		#region BLAS like
		[NativeMethod(6)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_sdgmm_batch_strided(MklMatrixLayout layout, MklBlasSideMode side, long m, long n, void* A, long lda, long strideA, void* x, long incx, long strideX, void* C, long ldc, long strideC, long batch_size);

		[NativeMethod(4, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void MKL_Somatadd(MklMatrixLayoutChar ordering, MklOperationChar transa, MklOperationChar transb, long rows, long cols, float alpha, float* A, long lda, float beta, float* B, long ldb, float* C, long ldc);

		[NativeMethod(4, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void MKL_Somatcopy(MklMatrixLayoutChar ordering, MklOperationChar trans, long rows, long cols, float alpha, float* A, long lda, float* B, long ldb);

		[NativeMethod(4, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void MKL_Simatcopy(MklMatrixLayoutChar ordering, MklOperationChar trans, long rows, long cols, float alpha, float* A, long lda);
		#endregion

		#region vector math
		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsAdd(long n, void* a, void* b, void* y);

		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsAddI(long n, void* a, long inca, void* b, long incb, void* y, long incy);

		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsSub(long n, void* a, void* b, void* y);

		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsSubI(long n, void* a, long inca, void* b, long incb, void* y, long incy);

		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsMul(long n, void* a, void* b, void* y);

		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsMulI(long n, void* a, long inca, void* b, long incb, void* y, long incy);

		[NativeMethod(1, false, false, false, false)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vcConj(long n, void* a, void* y);

		[NativeMethod(1, false, false, false, false)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vcConjI(long n, void* a, long inca, void* y, long incy);

		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsAbs(long n, void* a, void* y);

		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsAbsI(long n, void* a, long inca, void* y, long incy);

		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsDiv(long n, void* a, void* b, void* y);

		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsDivI(long n, void* a, long inca, void* b, long incb, void* y, long incy);

		[NativeMethod(1, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsSqrt(long n, void* a, void* y);

		[NativeMethod(1, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsSqrtI(long n, void* a, long inca, void* y, long incy);

		[NativeMethod(1, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsInvSqrt(long n, void* a, void* y);

		[NativeMethod(1, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsInvSqrtI(long n, void* a, long inca, void* y, long incy);

		[NativeMethod(1, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsCbrt(long n, void* a, void* y);

		[NativeMethod(1, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsCbrtI(long n, void* a, long inca, void* y, long incy);

		[NativeMethod(1, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsInvCbrt(long n, void* a, void* y);

		[NativeMethod(1, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsInvCbrtI(long n, void* a, long inca, void* y, long incy);

		[NativeMethod(1, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsPow2o3(long n, void* a, void* y);

		[NativeMethod(1, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsPow2o3I(long n, void* a, long inca, void* y, long incy);

		[NativeMethod(1, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsPow3o2(long n, void* a, void* y);

		[NativeMethod(1, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsPow3o2I(long n, void* a, long inca, void* y, long incy);

		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsPow(long n, void* a, void* b, void* y);

		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsPowI(long n, void* a, long inca, void* b, long incb, void* y, long incy);

		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsPowx(long n, float* a, float b, float* y);

		[NativeMethod(1)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void vsPowxI(long n, float* a, long inca, float b, float* y, long incy);
		#endregion;

		#region solver
		// LU factorize A
		[NativeMethod(8)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_sgetrf(MklMatrixLayout matrix_layout, long m, long n, void* A, long lda, long* ipiv);

		// solve A*X=B with LU factorized A
		[NativeMethod(8)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_sgetrs(MklMatrixLayout matrix_layout, MklOperationChar trans, long n, long nrhs, void* A, long lda, long* ipiv, void* B, long ldb);

		// direct solve A*X=B and LU factorize A
		[NativeMethod(8)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_sgesv(MklMatrixLayout matrix_layout, long n, long nrhs, void* a, long lda, long* ipiv, void* b, long ldb);

		// QR factorize
		[NativeMethod(8)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_sgeqrf(MklMatrixLayout matrix_layout, long m, long n, void* a, long lda, void* tau);

		// QR generate Q
		[NativeMethod(8, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_sorgqr(MklMatrixLayout matrix_layout, long m, long n, long k, void* a, long lda, void* tau);

		[NativeMethod(8, false, false, false, false)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_cungqr(MklMatrixLayout matrix_layout, long m, long n, long k, void* a, long lda, void* tau);

		// least square solve
		[NativeMethod(8)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_sgels(MklMatrixLayout matrix_layout, MklOperationChar trans, long m, long n, long nrhs, void* a, long lda, void* b, long ldb);

		// symmetric/Hermitian eigen
		[NativeMethod(8, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_ssyev(MklMatrixLayout matrix_layout, MklVectorModeChar jobz, MklFillModeChar uplo, long n, void* A, long lda, void* w);

		[NativeMethod(8, false, false, false, false)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_cheev(MklMatrixLayout matrix_layout, MklVectorModeChar jobz, MklFillModeChar uplo, long n, void* A, long lda, void* w);

		// general symmetric/hermitian positive-definite eigen
		[NativeMethod(8, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_ssygv(MklMatrixLayout matrix_layout, GeneralEigenType itype, MklVectorModeChar jobz, MklFillModeChar uplo, long n, void* a, long lda, void* b, long ldb, void* w);

		[NativeMethod(8, false, false, false, false)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_chegv(MklMatrixLayout matrix_layout, GeneralEigenType itype, MklVectorModeChar jobz, MklFillModeChar uplo, long n, void* a, long lda, void* b, long ldb, void* w);

		// non-symmetric eigen
		[NativeMethod(8, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_sgeev(MklMatrixLayout matrix_layout, MklVectorModeChar jobvl, MklVectorModeChar jobvr, long n, void* A, long lda, void* wr, void* wi, void* Vl, long ldvl, void* Vr, long ldvr);

		[NativeMethod(8, false, false, false, false)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_cgeev(MklMatrixLayout matrix_layout, MklVectorModeChar jobvl, MklVectorModeChar jobvr, long n, void* A, long lda, void* w, void* Vl, long ldvl, void* Vr, long ldvr);

		// general non-symmetric eigen
		[NativeMethod(8, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_sggev(MklMatrixLayout matrix_layout, MklVectorModeChar jobvl, MklVectorModeChar jobvr, long n, void* a, long lda, void* b, long ldb, void* alphar, void* alphai, void* beta, void* vl, long ldvl, void* vr, long ldvr);

		[NativeMethod(8, false, false, false, false)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_cggev(MklMatrixLayout matrix_layout, MklVectorModeChar jobvl, MklVectorModeChar jobvr, long n, void* a, long lda, void* b, long ldb, void* alpha, void* beta, void* vl, long ldvl, void* vr, long ldvr);

		// SVD
		[NativeMethod(8)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_sgesvd(MklMatrixLayout matrix_layout, MklSvdModeChar jobu, MklSvdModeChar jobvt, long m, long n, void* A, long lda, void* S, void* U, long ldu, void* Vt, long ldvt, void* superb);

		// direct Schur
		[NativeMethod(8, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_sgees(MklMatrixLayout matrix_layout, MklVectorModeChar jobv, MklSortModeChar sort, delegate* unmanaged<void*, void*, long> selectFunc, long n, void* A, long lda, out long selected, void* wr, void* wi, void* V, long ldv);

		[NativeMethod(8, false, false, false, false)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_cgees(MklMatrixLayout matrix_layout, MklVectorModeChar jobv, MklSortModeChar sort, delegate* unmanaged<void*, long> selectFunc, long n, void* A, long lda, out long selected, void* w, void* V, long ldv);

		// direct general Schur
		[NativeMethod(8, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_sgges(MklMatrixLayout matrix_layout, MklVectorModeChar jobvl, MklVectorModeChar jobvr, MklSortModeChar sort, delegate* unmanaged<void*, void*, void*, long> selectFunc, long n, void* A, long lda, void* B, long ldb, out long selected, void* alphar, void* alphai, void* beta, void* Vl, long ldvl, void* vr, long ldvr);

		[NativeMethod(8, false, false, false, false)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_cgges(MklMatrixLayout matrix_layout, MklVectorModeChar jobvl, MklVectorModeChar jobvr, MklSortModeChar sort, delegate* unmanaged<void*, void*, long> selectFunc, long n, void* A, long lda, void* B, long ldb, out long selected, void* alphar, void* alphai, void* beta, void* Vl, long ldvl, void* vr, long ldvr);

		// Hessenberg form Schur, iLow == 1 && iHigh == n
		[NativeMethod(8, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_shseqr(MklMatrixLayout matrix_layout, MklSchurModeChar job, MklVectorModeChar compz, long n, long ilo, long ihi, void* H, long ldh, void* wr, void* wi, void* Z, long ldz);

		[NativeMethod(8, false, false, false, false)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_chseqr(MklMatrixLayout matrix_layout, MklSchurModeChar job, MklVectorModeChar compz, long n, long ilo, long ihi, void* H, long ldh, void* w, void* Z, long ldz);

		// eigenvectors from Schur, mm == n
		[NativeMethod(8)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_strevc(MklMatrixLayout matrix_layout, MklSchurEigenvectorModeChar side, MklSchurEigenSelectModeChar howmny, long* select, long n, void* T, long ldt, void* Vl, long ldvl, void* Vr, long ldvr, long mm, out long selected);

		// reorder Schur
		[NativeMethod(8)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_strexc(MklMatrixLayout matrix_layout, MklVectorModeChar jobq, long n, void* T, long ldt, void* Q, long ldq, long* rowIndexFrom, long* rowIndexTo);

		// reorder general Schur
		[NativeMethod(8)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_stgexc(MklMatrixLayout matrix_layout, long wantq, long wantz, long n, void* A, long lda, void* B, long ldb, void* Q, long ldq, void* Z, long ldz, long* rowIndexFrom, long* rowIndexTo);
		#endregion
	}
}
