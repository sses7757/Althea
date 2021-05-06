using System;
using System.Runtime.InteropServices;

using Althea.LinearAlgebra;
using Althea.NativeTypes;


#pragma warning disable IDE1006 // 命名样式
namespace Althea.Backend.Mkl.LinearAlgebra.Dense
{
	/// <summary>
	/// MKL BLAS library API
	/// </summary>
	public static unsafe class NativeMethods
	{
		/// <summary>
		/// The MKL BLAS library name
		/// </summary>
		public const string MKLBLAS_API_DLL_NAME = "mkl_rt";

		/// <summary>
		/// The custom BLAS library name
		/// </summary>
		public const string CUSTOM_API_DLL_NAME = "SupplementOMP";


		#region level 1
		#region abs max
		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern long cblas_isamax(int n, IntPtr x, int incx);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern long cblas_idamax(int n, IntPtr x, int incx);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern long cblas_icamax(int n, IntPtr x, int incx);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern long cblas_izamax(int n, IntPtr x, int incx);
		#endregion

		#region abs min
		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern long cblas_isamin(int n, IntPtr x, int incx);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern long cblas_idamin(int n, IntPtr x, int incx);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern long cblas_icamin(int n, IntPtr x, int incx);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern long cblas_izamin(int n, IntPtr x, int incx);
		#endregion

		#region abs sum
		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern float cblas_sasum(int N, IntPtr X, int incX);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern double cblas_dasum(int N, IntPtr X, int incX);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern float cblas_scasum(int N, IntPtr X, int incX);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern double cblas_dzasum(int N, IntPtr X, int incX);
		#endregion

		#region norm
		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern float cblas_snrm2(int N, IntPtr X, int incX);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern double cblas_dnrm2(int N, IntPtr X, int incX);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern float cblas_scnrm2(int N, IntPtr X, int incX);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern double cblas_dznrm2(int N, IntPtr X, int incX);
		#endregion

		#region alpha X add to Y
		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_saxpy(int N, float alpha, IntPtr X, int incX, IntPtr Y, int incY);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_daxpy(int N, double alpha, IntPtr X, int incX, IntPtr Y, int incY);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_caxpy(int N, void* alpha, IntPtr X, int incX, IntPtr Y, int incY);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_zaxpy(int N, void* alpha, IntPtr X, int incX, IntPtr Y, int incY);
		#endregion

		#region scale
		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_sscal(int n, float alpha, IntPtr x, int incx);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_dscal(int n, double alpha, IntPtr x, int incx);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_cscal(int n, void* alpha, IntPtr x, int incx);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_zscal(int n, void* alpha, IntPtr x, int incx);
		#endregion

		#region copy
		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_scopy(int N, IntPtr X, int incX, IntPtr Y, int incY);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_dcopy(int N, IntPtr X, int incX, IntPtr Y, int incY);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_ccopy(int N, IntPtr X, int incX, IntPtr Y, int incY);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_zcopy(int N, IntPtr X, int incX, IntPtr Y, int incY);
		#endregion

		#region dot
		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern float cblas_sdot(int N, IntPtr X, int incX, IntPtr Y, int incY);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern double cblas_ddot(int N, IntPtr X, int incX, IntPtr Y, int incY);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_cdotu_sub(int N, IntPtr X, int incX, IntPtr Y, int incY, void* dotu);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_cdotc_sub(int N, IntPtr X, int incX, IntPtr Y, int incY, void* dotc);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_zdotu_sub(int N, IntPtr X, int incX, IntPtr Y, int incY, void* dotu);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_zdotc_sub(int N, IntPtr X, int incX, IntPtr Y, int incY, void* dotc);
		#endregion
		#endregion


		#region level 2
		#region general matrix vector multiply
		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_sgemv(MklMatrixLayout Layout, MklOperation trans, int m, int n, float alpha, IntPtr A, int lda, IntPtr x, int incx, float beta, IntPtr y, int incy);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_dgemv(MklMatrixLayout Layout, MklOperation trans, int m, int n, double alpha, IntPtr A, int lda, IntPtr x, int incx, double beta, IntPtr y, int incy);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_cgemv(MklMatrixLayout Layout, MklOperation trans, int m, int n, void* alpha, IntPtr A, int lda, IntPtr x, int incx, void* beta, IntPtr y, int incy);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_zgemv(MklMatrixLayout Layout, MklOperation trans, int m, int n, void* alpha, IntPtr A, int lda, IntPtr x, int incx, void* beta, IntPtr y, int incy);
		#endregion

		#region symmetric Hermitian matrix vector multiply
		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_ssymv(MklMatrixLayout Layout, MklFillMode uplo, int n, float alpha, IntPtr A, int lda, IntPtr x, int incx, float beta, IntPtr y, int incy);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_dsymv(MklMatrixLayout Layout, MklFillMode uplo, int n, double alpha, IntPtr A, int lda, IntPtr x, int incx, double beta, IntPtr y, int incy);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_chemv(MklMatrixLayout Layout, MklFillMode uplo, int n, void* alpha, IntPtr A, int lda, IntPtr x, int incx, void* beta, IntPtr y, int incy);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_zhemv(MklMatrixLayout Layout, MklFillMode uplo, int n, void* alpha, IntPtr A, int lda, IntPtr x, int incx, void* beta, IntPtr y, int incy);
		#endregion

		#region triangular matrix vector multiply
		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_strmv(MklMatrixLayout Layout, MklFillMode Uplo, MklOperation TransA, MklBlasDiagType Diag, int N, IntPtr A, int lda, IntPtr X, int incX);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_dtrmv(MklMatrixLayout Layout, MklFillMode Uplo, MklOperation TransA, MklBlasDiagType Diag, int N, IntPtr A, int lda, IntPtr X, int incX);
		
		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_ctrmv(MklMatrixLayout Layout, MklFillMode Uplo, MklOperation TransA, MklBlasDiagType Diag, int N, IntPtr A, int lda, IntPtr X, int incX);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_ztrmv(MklMatrixLayout Layout, MklFillMode Uplo, MklOperation TransA, MklBlasDiagType Diag, int N, IntPtr A, int lda, IntPtr X, int incX);
		#endregion

		#region general rank one
		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_sger(MklMatrixLayout Layout, int m, int n, float alpha, IntPtr x, int incx, IntPtr y, int incy, IntPtr A, int lda);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_dger(MklMatrixLayout Layout, int m, int n, double alpha, IntPtr x, int incx, IntPtr y, int incy, IntPtr A, int lda);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_cgerc(MklMatrixLayout Layout, int m, int n, void* alpha, IntPtr x, int incx, IntPtr y, int incy, IntPtr A, int lda);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_cgeru(MklMatrixLayout Layout, int m, int n, void* alpha, IntPtr x, int incx, IntPtr y, int incy, IntPtr A, int lda);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_zgerc(MklMatrixLayout Layout, int m, int n, void* alpha, IntPtr x, int incx, IntPtr y, int incy, IntPtr A, int lda);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_zgeru(MklMatrixLayout Layout, int m, int n, void* alpha, IntPtr x, int incx, IntPtr y, int incy, IntPtr A, int lda);
		#endregion

		#region symmetric Hermitian rank one
		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_ssyr(MklMatrixLayout Layout, MklFillMode uplo, int n, float alpha, IntPtr x, int incx, IntPtr A, int lda);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_dsyr(MklMatrixLayout Layout, MklFillMode uplo, int n, double alpha, IntPtr x, int incx, IntPtr A, int lda);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_cher(MklMatrixLayout Layout, MklFillMode uplo, int n, void* alpha, IntPtr x, int incx, IntPtr A, int lda);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_zher(MklMatrixLayout Layout, MklFillMode uplo, int n, void* alpha, IntPtr x, int incx, IntPtr A, int lda);
		#endregion

		#region symmetric Hermitian rank two
		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_ssyr2(MklMatrixLayout Layout, MklFillMode uplo, int n, float alpha, IntPtr x, int incx, IntPtr y, int incy, IntPtr A, int lda);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_dsyr2(MklMatrixLayout Layout, MklFillMode uplo, int n, double alpha, IntPtr x, int incx, IntPtr y, int incy, IntPtr A, int lda);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_cher2(MklMatrixLayout Layout, MklFillMode uplo, int n, void* alpha, IntPtr x, int incx, IntPtr y, int incy, IntPtr A, int lda);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_zher2(MklMatrixLayout Layout, MklFillMode uplo, int n, void* alpha, IntPtr x, int incx, IntPtr y, int incy, IntPtr A, int lda);
		#endregion
		#endregion


		#region BLAS-like level 2
		#region diagonal matrix multiply
		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_sdgmm_batch(MklMatrixLayout layout, in MklBlasSideMode side, in int m, in int n, in IntPtr a, in int lda, in IntPtr x, in int incx, ref IntPtr c, in int ldc, int group_count, in int group_size);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_ddgmm_batch(MklMatrixLayout layout, in MklBlasSideMode side, in int m, in int n, in IntPtr a, in int lda, in IntPtr x, in int incx, ref IntPtr c, in int ldc, int group_count, in int group_size);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_cdgmm_batch(MklMatrixLayout layout, in MklBlasSideMode side, in int m, in int n, in IntPtr a, in int lda, in IntPtr x, in int incx, ref IntPtr c, in int ldc, int group_count, in int group_size);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_zdgmm_batch(MklMatrixLayout layout, in MklBlasSideMode side, in int m, in int n, in IntPtr a, in int lda, in IntPtr x, in int incx, ref IntPtr c, in int ldc, int group_count, in int group_size);
		#endregion
		#endregion


		#region level 3
		#region triangular solve
		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_strsm(MklMatrixLayout Layout, MklBlasSideMode Side, MklFillMode Uplo, MklOperation TransA, MklBlasDiagType Diag, int M, int N, float alpha, IntPtr A, int lda, IntPtr B, int ldb);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_dtrsm(MklMatrixLayout Layout, MklBlasSideMode Side, MklFillMode Uplo, MklOperation TransA, MklBlasDiagType Diag, int M, int N, double alpha, IntPtr A, int lda, IntPtr B, int ldb);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_ctrsm(MklMatrixLayout Layout, MklBlasSideMode Side, MklFillMode Uplo, MklOperation TransA, MklBlasDiagType Diag, int M, int N, void* alpha, IntPtr A, int lda, IntPtr B, int ldb);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_ztrsm(MklMatrixLayout Layout, MklBlasSideMode Side, MklFillMode Uplo, MklOperation TransA, MklBlasDiagType Diag, int M, int N, void* alpha, IntPtr A, int lda, IntPtr B, int ldb);
		#endregion

		#region triangular multiply
		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_strmm(MklMatrixLayout Layout, MklBlasSideMode Side, MklFillMode Uplo, MklOperation TransA, MklBlasDiagType Diag, int M, int N, float alpha, IntPtr A, int lda, IntPtr B, int ldb);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_dtrmm(MklMatrixLayout Layout, MklBlasSideMode Side, MklFillMode Uplo, MklOperation TransA, MklBlasDiagType Diag, int M, int N, double alpha, IntPtr A, int lda, IntPtr B, int ldb);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_ctrmm(MklMatrixLayout Layout, MklBlasSideMode Side, MklFillMode Uplo, MklOperation TransA, MklBlasDiagType Diag, int M, int N, void* alpha, IntPtr A, int lda, IntPtr B, int ldb);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_ztrmm(MklMatrixLayout Layout, MklBlasSideMode Side, MklFillMode Uplo, MklOperation TransA, MklBlasDiagType Diag, int M, int N, void* alpha, IntPtr A, int lda, IntPtr B, int ldb);
		#endregion

		#region general matrix multiply
		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_sgemm(MklMatrixLayout Layout, MklOperation TransA, MklOperation TransB, int m, int n, int k, float alpha, IntPtr A, int lda, IntPtr B, int ldb, float beta, IntPtr C, int ldc);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_dgemm(MklMatrixLayout Layout, MklOperation TransA, MklOperation TransB, int m, int n, int k, double alpha, IntPtr A, int lda, IntPtr B, int ldb, double beta, IntPtr C, int ldc);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_cgemm(MklMatrixLayout Layout, MklOperation TransA, MklOperation TransB, int m, int n, int k, void* alpha, IntPtr A, int lda, IntPtr B, int ldb, void* beta, IntPtr C, int ldc);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_zgemm(MklMatrixLayout Layout, MklOperation TransA, MklOperation TransB, int m, int n, int k, void* alpha, IntPtr A, int lda, IntPtr B, int ldb, void* beta, IntPtr C, int ldc);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_cgemm3m(MklMatrixLayout Layout, MklOperation TransA, MklOperation TransB, int m, int n, int k, void* alpha, IntPtr A, int lda, IntPtr B, int ldb, void* beta, IntPtr C, int ldc);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_zgemm3m(MklMatrixLayout Layout, MklOperation TransA, MklOperation TransB, int m, int n, int k, void* alpha, IntPtr A, int lda, IntPtr B, int ldb, void* beta, IntPtr C, int ldc);
		#endregion

		#region symmetric Hermitian matrix multiply
		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_ssymm(MklMatrixLayout Layout, MklBlasSideMode side, MklFillMode uplo, int m, int n, float alpha, IntPtr A, int lda, IntPtr B, int ldb, float beta, IntPtr C, int ldc);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_dsymm(MklMatrixLayout Layout, MklBlasSideMode side, MklFillMode uplo, int m, int n, double alpha, IntPtr A, int lda, IntPtr B, int ldb, double beta, IntPtr C, int ldc);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_csymm(MklMatrixLayout Layout, MklBlasSideMode side, MklFillMode uplo, int m, int n, void* alpha, IntPtr A, int lda, IntPtr B, int ldb, void* beta, IntPtr C, int ldc);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_zsymm(MklMatrixLayout Layout, MklBlasSideMode side, MklFillMode uplo, int m, int n, void* alpha, IntPtr A, int lda, IntPtr B, int ldb, void* beta, IntPtr C, int ldc);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_chemm(MklMatrixLayout Layout, MklBlasSideMode side, MklFillMode uplo, int m, int n, void* alpha, IntPtr A, int lda, IntPtr B, int ldb, void* beta, IntPtr C, int ldc);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_zhemm(MklMatrixLayout Layout, MklBlasSideMode side, MklFillMode uplo, int m, int n, void* alpha, IntPtr A, int lda, IntPtr B, int ldb, void* beta, IntPtr C, int ldc);
		#endregion

		#region rank k update
		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_ssyrk(MklMatrixLayout Layout, MklFillMode uplo, MklOperation trans, int n, int k, float alpha, IntPtr A, int lda, float beta, IntPtr C, int ldc);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_dsyrk(MklMatrixLayout Layout, MklFillMode uplo, MklOperation trans, int n, int k, double alpha, IntPtr A, int lda, double beta, IntPtr C, int ldc);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_csyrk(MklMatrixLayout Layout, MklFillMode uplo, MklOperation trans, int n, int k, void* alpha, IntPtr A, int lda, void* beta, IntPtr C, int ldc);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_zsyrk(MklMatrixLayout Layout, MklFillMode uplo, MklOperation trans, int n, int k, void* alpha, IntPtr A, int lda, void* beta, IntPtr C, int ldc);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_cherk(MklMatrixLayout Layout, MklFillMode uplo, MklOperation trans, int n, int k, void* alpha, IntPtr A, int lda, void* beta, IntPtr C, int ldc);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_zherk(MklMatrixLayout Layout, MklFillMode uplo, MklOperation trans, int n, int k, void* alpha, IntPtr A, int lda, void* beta, IntPtr C, int ldc);
		#endregion

		#region rank 2k update
		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_ssyr2k(MklMatrixLayout Layout, MklFillMode uplo, MklOperation trans, int n, int k, float alpha, IntPtr A, int lda, IntPtr B, int ldb, float beta, IntPtr C, int ldc);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_dsyr2k(MklMatrixLayout Layout, MklFillMode uplo, MklOperation trans, int n, int k, double alpha, IntPtr A, int lda, IntPtr B, int ldb, double beta, IntPtr C, int ldc);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_csyr2k(MklMatrixLayout Layout, MklFillMode uplo, MklOperation trans, int n, int k, void* alpha, IntPtr A, int lda, IntPtr B, int ldb, void* beta, IntPtr C, int ldc);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_zsyr2k(MklMatrixLayout Layout, MklFillMode uplo, MklOperation trans, int n, int k, void* alpha, IntPtr A, int lda, IntPtr B, int ldb, void* beta, IntPtr C, int ldc);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_cher2k(MklMatrixLayout Layout, MklFillMode uplo, MklOperation trans, int n, int k, void* alpha, IntPtr A, int lda, IntPtr B, int ldb, void* beta, IntPtr C, int ldc);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_zher2k(MklMatrixLayout Layout, MklFillMode uplo, MklOperation trans, int n, int k, void* alpha, IntPtr A, int lda, IntPtr B, int ldb, void* beta, IntPtr C, int ldc);
		#endregion
		#endregion


		#region BLAS like
		#region matrix add
		internal delegate void omatadd<T>(MklMatrixLayoutChar ordering, MklOperationChar transa, MklOperationChar transb, long rows, long cols, T alpha, IntPtr A, long lda, T beta, IntPtr B, long ldb, IntPtr C, long ldc);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void MKL_Somatadd(MklMatrixLayoutChar ordering, MklOperationChar transa, MklOperationChar transb, long rows, long cols, float alpha, IntPtr A, long lda, float beta, IntPtr B, long ldb, IntPtr C, long ldc);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void MKL_Domatadd(MklMatrixLayoutChar ordering, MklOperationChar transa, MklOperationChar transb, long rows, long cols, double alpha, IntPtr A, long lda, double beta, IntPtr B, long ldb, IntPtr C, long ldc);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void MKL_Comatadd(MklMatrixLayoutChar ordering, MklOperationChar transa, MklOperationChar transb, long rows, long cols, ComplexSingle alpha, IntPtr A, long lda, ComplexSingle beta, IntPtr B, long ldb, IntPtr C, long ldc);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void MKL_Zomatadd(MklMatrixLayoutChar ordering, MklOperationChar transa, MklOperationChar transb, long rows, long cols, ComplexDouble alpha, IntPtr A, long lda, ComplexDouble beta, IntPtr B, long ldb, IntPtr C, long ldc);
		#endregion

		#region matrix transpose
		internal delegate void omatcopy<T>(MklMatrixLayoutChar ordering, MklOperationChar trans, long rows, long cols, T alpha, IntPtr A, long lda, IntPtr B, long ldb);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void MKL_Somatcopy(MklMatrixLayoutChar ordering, MklOperationChar trans, long rows, long cols, float alpha, IntPtr A, long lda, IntPtr B, long ldb);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void MKL_Domatcopy(MklMatrixLayoutChar ordering, MklOperationChar trans, long rows, long cols, double alpha, IntPtr A, long lda, IntPtr B, long ldb);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void MKL_Comatcopy(MklMatrixLayoutChar ordering, MklOperationChar trans, long rows, long cols, ComplexSingle alpha, IntPtr A, long lda, IntPtr B, long ldb);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void MKL_Zomatcopy(MklMatrixLayoutChar ordering, MklOperationChar trans, long rows, long cols, ComplexDouble alpha, IntPtr A, long lda, IntPtr B, long ldb);
		#endregion

		#region in-place matrix transpose
		internal delegate void imatcopy<T>(MklMatrixLayoutChar ordering, MklOperationChar trans, long rows, long cols, T alpha, IntPtr A, long lda);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void MKL_Simatcopy(MklMatrixLayoutChar ordering, MklOperationChar trans, long rows, long cols, float alpha, IntPtr A, long lda);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void MKL_Dimatcopy(MklMatrixLayoutChar ordering, MklOperationChar trans, long rows, long cols, double alpha, IntPtr A, long lda);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void MKL_Cimatcopy(MklMatrixLayoutChar ordering, MklOperationChar trans, long rows, long cols, ComplexSingle alpha, IntPtr A, long lda);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void MKL_Zimatcopy(MklMatrixLayoutChar ordering, MklOperationChar trans, long rows, long cols, ComplexDouble alpha, IntPtr A, long lda);
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
		[DllImport(CUSTOM_API_DLL_NAME)]
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
		[DllImport(CUSTOM_API_DLL_NAME)]
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
		[DllImport(CUSTOM_API_DLL_NAME)]
		internal static extern bool vecsEq(DataType type, IntPtr a, IntPtr b, long N, int strideA, int strideB);

		/// <summary>
		/// In-place exponentiate the vector <paramref name="a"/> by a scalar exponent <paramref name="p"/>
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/></param>
		/// <param name="a">The vector to be in-place modified of <paramref name="type"/></param>
		/// <param name="p">The pointer to the scalar exponent of <paramref name="type"/></param>
		/// <param name="N">The number of elements to be operated</param>
		/// <param name="stride">The spacing between consecutive elements of <paramref name="a"/></param>
		[DllImport(CUSTOM_API_DLL_NAME)]
		internal static extern void vecPowSameType(DataType type, IntPtr a, void* p, long N, int stride);

		/// <summary>
		/// In-place exponentiate the vector <paramref name="a"/> by a scalar exponent <paramref name="p"/>
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/> (must be a complex type)</param>
		/// <param name="a">The vector to be in-place modified of <paramref name="type"/></param>
		/// <param name="p">The pointer to the scalar exponent of <paramref name="type"/>'s real corresponding type</param>
		/// <param name="N">The number of elements to be operated</param>
		/// <param name="stride">The spacing between consecutive elements of <paramref name="a"/></param>
		[DllImport(CUSTOM_API_DLL_NAME)]
		internal static extern void vecPowRealType(DataType type, IntPtr a, void* p, long N, int stride);

		/// <summary>
		/// In-place conjugate the vector <paramref name="a"/>
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/></param>
		/// <param name="a">The vector to be in-place modified of <paramref name="type"/></param>
		/// <param name="N">The number of elements to be operated</param>
		/// <param name="stride">The spacing between consecutive elements of <paramref name="a"/></param>
		[DllImport(CUSTOM_API_DLL_NAME)]
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
		[DllImport(CUSTOM_API_DLL_NAME)]
		internal static extern void vecDataConvert(DataType srcType, DataType dstType, IntPtr src, IntPtr dst, long N, int strideSrc, int strideDst, bool toRealByAbs);

		/// <summary>
		/// In-place set the values in <paramref name="a"/> whose absolute values are less than or equal to the absolute value of <paramref name="threshold"/> to 0
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/></param>
		/// <param name="a">The vector to be in-place modified of <paramref name="type"/></param>
		/// <param name="threshold">The pointer to the threshold used to clip the vector <paramref name="a"/> of <paramref name="type"/></param>
		/// <param name="N">The number of elements to be operated</param>
		/// <param name="stride">The spacing between consecutive elements of <paramref name="a"/></param>
		[DllImport(CUSTOM_API_DLL_NAME)]
		internal static extern void vecClip(DataType type, IntPtr a, void* threshold, long N, int stride);

		/// <summary>
		/// In-place add all elements in vector <paramref name="a"/> with the given <paramref name="scalar"/>
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/></param>
		/// <param name="a">The vector to be in-place modified of <paramref name="type"/></param>
		/// <param name="scalar">The pointer to the scalar to add of <paramref name="type"/></param>
		/// <param name="N">The number of elements to be operated</param>
		/// <param name="stride">The spacing between consecutive elements of <paramref name="a"/></param>
		[DllImport(CUSTOM_API_DLL_NAME)]
		internal static extern void vecAddScalar(DataType type, IntPtr a, void* scalar, long N, int stride);

		/// <summary>
		/// In-place multiplies all elements in vector <paramref name="a"/> with the given <paramref name="scalar"/>
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/></param>
		/// <param name="a">The vector to be in-place modified of <paramref name="type"/></param>
		/// <param name="scalar">The pointer to the scalar to multiply of <paramref name="type"/></param>
		/// <param name="N">The number of elements to be operated</param>
		/// <param name="stride">The spacing between consecutive elements of <paramref name="a"/></param>
		[DllImport(CUSTOM_API_DLL_NAME)]
		internal static extern void vecMulScalar(DataType type, IntPtr a, void* scalar, long N, int stride);

		/// <summary>
		/// Sums all the elements in vector <paramref name="a"/>
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/></param>
		/// <param name="a">The vector to be summed of <paramref name="type"/></param>
		/// <param name="N">The number of elements to be operated</param>
		/// <param name="stride">The spacing between consecutive elements of <paramref name="a"/></param>
		/// <param name="outSum">The output sum as a pointer of <paramref name="type"/></param>
		[DllImport(CUSTOM_API_DLL_NAME)]
		internal static extern void vecSum(DataType type, IntPtr a, long N, int stride, void* outSum);

		/// <summary>
		/// Get the index of the element with minimum absolute value in vector <paramref name="a"/>
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/></param>
		/// <param name="a">The vector to be summed of <paramref name="type"/></param>
		/// <param name="N">The number of elements to be operated</param>
		/// <param name="stride">The spacing between consecutive elements of <paramref name="a"/></param>
		/// <returns>The index of the element</returns>
		[DllImport(CUSTOM_API_DLL_NAME)]
		internal static extern long vecArgAbsMin(DataType type, IntPtr a, long N, int stride);

		/// <summary>
		/// Get the index of the element with maximum absolute value in vector <paramref name="a"/>
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/></param>
		/// <param name="a">The vector to be summed of <paramref name="type"/></param>
		/// <param name="N">The number of elements to be operated</param>
		/// <param name="stride">The spacing between consecutive elements of <paramref name="a"/></param>
		/// <returns>The index of the element</returns>
		[DllImport(CUSTOM_API_DLL_NAME)]
		internal static extern long vecArgAbsMax(DataType type, IntPtr a, long N, int stride);

		/// <summary>
		/// Sums all the elements's absolute values in vector <paramref name="a"/>
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/></param>
		/// <param name="a">The vector to be summed of <paramref name="type"/></param>
		/// <param name="N">The number of elements to be operated</param>
		/// <param name="stride">The spacing between consecutive elements of <paramref name="a"/></param>
		/// <returns>The sum as a <see cref="double"/></returns>
		[DllImport(CUSTOM_API_DLL_NAME)]
		internal static extern double vecAbsSum(DataType type, IntPtr a, long N, int stride);

		/// <summary>
		/// Compute the 2-norm of the given vector <paramref name="a"/>
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/></param>
		/// <param name="a">The vector to be summed of <paramref name="type"/></param>
		/// <param name="N">The number of elements to be operated</param>
		/// <param name="stride">The spacing between consecutive elements of <paramref name="a"/></param>
		/// <returns>The 2-norm as a <see cref="double"/></returns>
		[DllImport(CUSTOM_API_DLL_NAME)]
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
		[DllImport(CUSTOM_API_DLL_NAME)]
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
		[DllImport(CUSTOM_API_DLL_NAME)]
		internal static extern void vecDotc(DataType type, IntPtr a, IntPtr b, long N, int strideA, int strideB, void* outProd);

		/// <summary>
		/// Multiplies all the elements in vector <paramref name="a"/>
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/></param>
		/// <param name="a">The vector to be multiplied of <paramref name="type"/></param>
		/// <param name="N">The number of elements to be operated</param>
		/// <param name="stride">The spacing between consecutive elements of <paramref name="a"/></param>
		/// <param name="outProd">The output product as a pointer of <paramref name="type"/></param>
		[DllImport(CUSTOM_API_DLL_NAME)]
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
		[DllImport(CUSTOM_API_DLL_NAME)]
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
		[DllImport(CUSTOM_API_DLL_NAME)]
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
		[DllImport(CUSTOM_API_DLL_NAME)]
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
		[DllImport(CUSTOM_API_DLL_NAME)]
		internal static extern void matMakeHerm(DataType type, IntPtr A, long ld, long rows, bool upperStored, bool hermA);

		/// <summary>
		/// Clear (set to 0) the matrix <paramref name="A"/>'s upper part or its lower part
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="A"/></param>
		/// <param name="A">The matrix to be modified of <paramref name="type"/></param>
		/// <param name="ld">The leading dimension of <paramref name="A"/>, must be at least <paramref name="rows"/></param>
		/// <param name="rows">The number of rows of <paramref name="A"/></param>
		/// <param name="clearLower">Whether <paramref name="A"/>'s upper part or its lower part shall be preserved</param>
		[DllImport(CUSTOM_API_DLL_NAME)]
		internal static extern void matTriClear(DataType type, IntPtr A, long ld, long rows, bool clearLower);

		/// <summary>
		/// Fill the <paramref name="array"/> with given <paramref name="value"/> of <paramref name="type"/>
		/// </summary>
		/// <param name="type">The data type of the array and value</param>
		/// <param name="array">The array to be filled</param>
		/// <param name="value">The pointer to the value of <paramref name="type"/> to be filled</param>
		/// <param name="N">The number of elements of <paramref name="array"/>, in <paramref name="type"/></param>
		/// <param name="stride">The stride between two consecutive elements to be operated in <paramref name="array"/></param>
		/// <remarks>Strided filling reduce the performance greatly.</remarks>
		[DllImport(CUSTOM_API_DLL_NAME)]
		internal static unsafe extern void vecFillVal(DataType type, IntPtr array, void* value, long N, int stride);
		#endregion


		#region solver
		#region LU factorization
		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_sgetrf(MklMatrixLayout matrix_layout, int m, int n, IntPtr A, int lda, IntPtr ipiv);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_dgetrf(MklMatrixLayout matrix_layout, int m, int n, IntPtr A, int lda, IntPtr ipiv);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_cgetrf(MklMatrixLayout matrix_layout, int m, int n, IntPtr A, int lda, IntPtr ipiv);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_zgetrf(MklMatrixLayout matrix_layout, int m, int n, IntPtr A, int lda, IntPtr ipiv);
		#endregion

		#region LU solve
		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_sgetrs(MklMatrixLayout matrix_layout, MklOperationChar trans, int n, int nrhs, IntPtr A, int lda, IntPtr ipiv, IntPtr B, int ldb);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_dgetrs(MklMatrixLayout matrix_layout, MklOperationChar trans, int n, int nrhs, IntPtr A, int lda, IntPtr ipiv, IntPtr b, int ldb);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_cgetrs(MklMatrixLayout matrix_layout, MklOperationChar trans, int n, int nrhs, IntPtr A, int lda, IntPtr ipiv, IntPtr b, int ldb);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_zgetrs(MklMatrixLayout matrix_layout, MklOperationChar trans, int n, int nrhs, IntPtr A, int lda, IntPtr ipiv, IntPtr b, int ldb);
		#endregion

		#region direct matrix solve
		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_sgesv(MklMatrixLayout matrix_layout, int n, int nrhs, IntPtr a, int lda, IntPtr ipiv, IntPtr b, int ldb);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_dgesv(MklMatrixLayout matrix_layout, int n, int nrhs, IntPtr a, int lda, IntPtr ipiv, IntPtr b, int ldb);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_cgesv(MklMatrixLayout matrix_layout, int n, int nrhs, IntPtr a, int lda, IntPtr ipiv, IntPtr b, int ldb);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_zgesv(MklMatrixLayout matrix_layout, int n, int nrhs, IntPtr a, int lda, IntPtr ipiv, IntPtr b, int ldb);
		#endregion

		#region QR factorization
		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_sgeqrf(MklMatrixLayout matrix_layout, int m, int n, IntPtr a, int lda, IntPtr tau);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_dgeqrf(MklMatrixLayout matrix_layout, int m, int n, IntPtr a, int lda, IntPtr tau);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_cgeqrf(MklMatrixLayout matrix_layout, int m, int n, IntPtr a, int lda, IntPtr tau);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_zgeqrf(MklMatrixLayout matrix_layout, int m, int n, IntPtr a, int lda, IntPtr tau);
		#endregion

		#region QR generate Q
		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_sorgqr(MklMatrixLayout matrix_layout, int m, int n, int k, IntPtr a, int lda, IntPtr tau);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_dorgqr(MklMatrixLayout matrix_layout, int m, int n, int k, IntPtr a, int lda, IntPtr tau);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_cungqr(MklMatrixLayout matrix_layout, int m, int n, int k, IntPtr a, int lda, IntPtr tau);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_zungqr(MklMatrixLayout matrix_layout, int m, int n, int k, IntPtr a, int lda, IntPtr tau);
		#endregion

		#region least square solve
		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_sgels(MklMatrixLayout matrix_layout, MklOperationChar trans, int m, int n, int nrhs, IntPtr a, int lda, IntPtr b, int ldb);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_dgels(MklMatrixLayout matrix_layout, MklOperationChar trans, int m, int n, int nrhs, IntPtr a, int lda, IntPtr b, int ldb);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_cgels(MklMatrixLayout matrix_layout, MklOperationChar trans, int m, int n, int nrhs, IntPtr a, int lda, IntPtr b, int ldb);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_zgels(MklMatrixLayout matrix_layout, MklOperationChar trans, int m, int n, int nrhs, IntPtr a, int lda, IntPtr b, int ldb);
		#endregion

		#region symmetric/Hermitian eigen
		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_ssyev(MklMatrixLayout matrix_layout, MklVectorModeChar jobz, MklFillModeChar uplo, int n, IntPtr A, int lda, IntPtr w);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_dsyev(MklMatrixLayout matrix_layout, MklVectorModeChar jobz, MklFillModeChar uplo, int n, IntPtr A, int lda, IntPtr w);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_cheev(MklMatrixLayout matrix_layout, MklVectorModeChar jobz, MklFillModeChar uplo, int n, IntPtr A, int lda, IntPtr w);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_zheev(MklMatrixLayout matrix_layout, MklVectorModeChar jobz, MklFillModeChar uplo, int n, IntPtr A, int lda, IntPtr w);
		#endregion

		#region general symmetric/hermitian definite eigen
		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_ssygv(MklMatrixLayout matrix_layout, GeneralEigenType itype, MklVectorModeChar jobz, MklFillModeChar uplo, int n, IntPtr a, int lda, IntPtr b, int ldb, IntPtr w);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_dsygv(MklMatrixLayout matrix_layout, GeneralEigenType itype, MklVectorModeChar jobz, MklFillModeChar uplo, int n, IntPtr a, int lda, IntPtr b, int ldb, IntPtr w);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_chegv(MklMatrixLayout matrix_layout, GeneralEigenType itype, MklVectorModeChar jobz, MklFillModeChar uplo, int n, IntPtr a, int lda, IntPtr b, int ldb, IntPtr w);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_zhegv(MklMatrixLayout matrix_layout, GeneralEigenType itype, MklVectorModeChar jobz, MklFillModeChar uplo, int n, IntPtr a, int lda, IntPtr b, int ldb, IntPtr w);
		#endregion

		#region non-symmetric eigen
		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_sgeev(MklMatrixLayout matrix_layout, MklVectorModeChar jobvl, MklVectorModeChar jobvr, int n, IntPtr A, int lda, void* wr, void* wi, void* Vl, int ldvl, void* Vr, int ldvr);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_dgeev(MklMatrixLayout matrix_layout, MklVectorModeChar jobvl, MklVectorModeChar jobvr, int n, IntPtr A, int lda, void* wr, void* wi, void* Vl, int ldvl, void* Vr, int ldvr);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_cgeev(MklMatrixLayout matrix_layout, MklVectorModeChar jobvl, MklVectorModeChar jobvr, int n, IntPtr A, int lda, IntPtr w, IntPtr Vl, int ldvl, IntPtr Vr, int ldvr);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_zgeev(MklMatrixLayout matrix_layout, MklVectorModeChar jobvl, MklVectorModeChar jobvr, int n, IntPtr A, int lda, IntPtr w, IntPtr Vl, int ldvl, IntPtr Vr, int ldvr);
		#endregion

		#region non-symmetric general eigen
		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_sggev(MklMatrixLayout matrix_layout, MklVectorModeChar jobvl, MklVectorModeChar jobvr, int n, IntPtr a, int lda, IntPtr b, int ldb, void* alphar, void* alphai, IntPtr beta, void* vl, int ldvl, void* vr, int ldvr);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_dggev(MklMatrixLayout matrix_layout, MklVectorModeChar jobvl, MklVectorModeChar jobvr, int n, IntPtr a, int lda, IntPtr b, int ldb, void* alphar, void* alphai, IntPtr beta, void* vl, int ldvl, void* vr, int ldvr);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_cggev(MklMatrixLayout matrix_layout, MklVectorModeChar jobvl, MklVectorModeChar jobvr, int n, IntPtr a, int lda, IntPtr b, int ldb, IntPtr alpha, IntPtr beta, IntPtr vl, int ldvl, IntPtr vr, int ldvr);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_zggev(MklMatrixLayout matrix_layout, MklVectorModeChar jobvl, MklVectorModeChar jobvr, int n, IntPtr a, int lda, IntPtr b, int ldb, IntPtr alpha, IntPtr beta, IntPtr vl, int ldvl, IntPtr vr, int ldvr);
		#endregion

		#region SVD
		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_sgesvd(MklMatrixLayout matrix_layout, MklSvdModeChar jobu, MklSvdModeChar jobvt, int m, int n, IntPtr A, int lda, IntPtr S, IntPtr U, int ldu, IntPtr Vt, int ldvt, byte[] superb);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_dgesvd(MklMatrixLayout matrix_layout, MklSvdModeChar jobu, MklSvdModeChar jobvt, int m, int n, IntPtr A, int lda, IntPtr S, IntPtr U, int ldu, IntPtr Vt, int ldvt, byte[] superb);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_cgesvd(MklMatrixLayout matrix_layout, MklSvdModeChar jobu, MklSvdModeChar jobvt, int m, int n, IntPtr A, int lda, IntPtr S, IntPtr U, int ldu, IntPtr Vt, int ldvt, byte[] superb);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_zgesvd(MklMatrixLayout matrix_layout, MklSvdModeChar jobu, MklSvdModeChar jobvt, int m, int n, IntPtr A, int lda, IntPtr S, IntPtr U, int ldu, IntPtr Vt, int ldvt, byte[] superb);
		#endregion

		#region Schur
		internal delegate int SchurSelect1(void* v);
		internal delegate int SchurSelect2(void* v1, void* v2);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_sgees(MklMatrixLayout matrix_layout, MklVectorModeChar jobv, MklSortModeChar sort, [MarshalAs(UnmanagedType.FunctionPtr)] SchurSelect2? selectFunc, int n, IntPtr A, int lda, out int selected, void* wr, void* wi, IntPtr V, int ldv);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_dgees(MklMatrixLayout matrix_layout, MklVectorModeChar jobv, MklSortModeChar sort, [MarshalAs(UnmanagedType.FunctionPtr)] SchurSelect2? selectFunc, int n, IntPtr A, int lda, out int selected, void* wr, void* wi, IntPtr V, int ldv);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_cgees(MklMatrixLayout matrix_layout, MklVectorModeChar jobv, MklSortModeChar sort, [MarshalAs(UnmanagedType.FunctionPtr)] SchurSelect1? selectFunc, int n, IntPtr A, int lda, out int selected, void* w, IntPtr V, int ldv);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_zgees(MklMatrixLayout matrix_layout, MklVectorModeChar jobv, MklSortModeChar sort, [MarshalAs(UnmanagedType.FunctionPtr)] SchurSelect1? selectFunc, int n, IntPtr A, int lda, out int selected, void* w, IntPtr V, int ldv);
		#endregion
		#endregion
	}
}
