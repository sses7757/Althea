using System;
using System.Runtime.InteropServices;

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
		internal static extern void cblas_sgemv(MklBlasLayout Layout, MklBlasOperation trans, int m, int n, float alpha, IntPtr A, int lda, IntPtr x, int incx, float beta, IntPtr y, int incy);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_dgemv(MklBlasLayout Layout, MklBlasOperation trans, int m, int n, double alpha, IntPtr A, int lda, IntPtr x, int incx, double beta, IntPtr y, int incy);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_cgemv(MklBlasLayout Layout, MklBlasOperation trans, int m, int n, void* alpha, IntPtr A, int lda, IntPtr x, int incx, void* beta, IntPtr y, int incy);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_zgemv(MklBlasLayout Layout, MklBlasOperation trans, int m, int n, void* alpha, IntPtr A, int lda, IntPtr x, int incx, void* beta, IntPtr y, int incy);
		#endregion

		#region symmetric Hermitian matrix vector multiply
		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_ssymv(MklBlasLayout Layout, MklBlasFillMode uplo, int n, float alpha, IntPtr A, int lda, IntPtr x, int incx, float beta, IntPtr y, int incy);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_dsymv(MklBlasLayout Layout, MklBlasFillMode uplo, int n, double alpha, IntPtr A, int lda, IntPtr x, int incx, double beta, IntPtr y, int incy);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_chemv(MklBlasLayout Layout, MklBlasFillMode uplo, int n, void* alpha, IntPtr A, int lda, IntPtr x, int incx, void* beta, IntPtr y, int incy);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_zhemv(MklBlasLayout Layout, MklBlasFillMode uplo, int n, void* alpha, IntPtr A, int lda, IntPtr x, int incx, void* beta, IntPtr y, int incy);
		#endregion

		#region triangular matrix vector multiply
		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_strmv(MklBlasLayout Layout, MklBlasFillMode Uplo, MklBlasOperation TransA, MklBlasDiagType Diag, int N, IntPtr A, int lda, IntPtr X, int incX);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_dtrmv(MklBlasLayout Layout, MklBlasFillMode Uplo, MklBlasOperation TransA, MklBlasDiagType Diag, int N, IntPtr A, int lda, IntPtr X, int incX);
		
		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_ctrmv(MklBlasLayout Layout, MklBlasFillMode Uplo, MklBlasOperation TransA, MklBlasDiagType Diag, int N, IntPtr A, int lda, IntPtr X, int incX);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_ztrmv(MklBlasLayout Layout, MklBlasFillMode Uplo, MklBlasOperation TransA, MklBlasDiagType Diag, int N, IntPtr A, int lda, IntPtr X, int incX);
		#endregion

		#region general rank one
		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_sger(MklBlasLayout Layout, int m, int n, float alpha, IntPtr x, int incx, IntPtr y, int incy, IntPtr A, int lda);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_dger(MklBlasLayout Layout, int m, int n, double alpha, IntPtr x, int incx, IntPtr y, int incy, IntPtr A, int lda);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_cgerc(MklBlasLayout Layout, int m, int n, void* alpha, IntPtr x, int incx, IntPtr y, int incy, IntPtr A, int lda);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_cgeru(MklBlasLayout Layout, int m, int n, void* alpha, IntPtr x, int incx, IntPtr y, int incy, IntPtr A, int lda);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_zgerc(MklBlasLayout Layout, int m, int n, void* alpha, IntPtr x, int incx, IntPtr y, int incy, IntPtr A, int lda);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_zgeru(MklBlasLayout Layout, int m, int n, void* alpha, IntPtr x, int incx, IntPtr y, int incy, IntPtr A, int lda);
		#endregion

		#region symmetric Hermitian rank one
		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_ssyr(MklBlasLayout Layout, MklBlasFillMode uplo, int n, float alpha, IntPtr x, int incx, IntPtr A, int lda);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_dsyr(MklBlasLayout Layout, MklBlasFillMode uplo, int n, double alpha, IntPtr x, int incx, IntPtr A, int lda);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_cher(MklBlasLayout Layout, MklBlasFillMode uplo, int n, void* alpha, IntPtr x, int incx, IntPtr A, int lda);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_zher(MklBlasLayout Layout, MklBlasFillMode uplo, int n, void* alpha, IntPtr x, int incx, IntPtr A, int lda);
		#endregion

		#region symmetric Hermitian rank two
		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_ssyr2(MklBlasLayout Layout, MklBlasFillMode uplo, int n, float alpha, IntPtr x, int incx, IntPtr y, int incy, IntPtr A, int lda);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_dsyr2(MklBlasLayout Layout, MklBlasFillMode uplo, int n, double alpha, IntPtr x, int incx, IntPtr y, int incy, IntPtr A, int lda);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_cher2(MklBlasLayout Layout, MklBlasFillMode uplo, int n, void* alpha, IntPtr x, int incx, IntPtr y, int incy, IntPtr A, int lda);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_zher2(MklBlasLayout Layout, MklBlasFillMode uplo, int n, void* alpha, IntPtr x, int incx, IntPtr y, int incy, IntPtr A, int lda);
		#endregion
		#endregion


		#region BLAS-like level 2
		#region diagonal matrix multiply
		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_sdgmm_batch(MklBlasLayout layout, in MklBlasSideMode side, in int m, in int n, in IntPtr a, in int lda, in IntPtr x, in int incx, ref IntPtr c, in int ldc, int group_count, in int group_size);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_ddgmm_batch(MklBlasLayout layout, in MklBlasSideMode side, in int m, in int n, in IntPtr a, in int lda, in IntPtr x, in int incx, ref IntPtr c, in int ldc, int group_count, in int group_size);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_cdgmm_batch(MklBlasLayout layout, in MklBlasSideMode side, in int m, in int n, in IntPtr a, in int lda, in IntPtr x, in int incx, ref IntPtr c, in int ldc, int group_count, in int group_size);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_zdgmm_batch(MklBlasLayout layout, in MklBlasSideMode side, in int m, in int n, in IntPtr a, in int lda, in IntPtr x, in int incx, ref IntPtr c, in int ldc, int group_count, in int group_size);
		#endregion
		#endregion


		#region level 3
		#region general matrix multiply
		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_sgemm(MklBlasLayout Layout, MklBlasOperation TransA, MklBlasOperation TransB, int m, int n, int k, float alpha, IntPtr A, int lda, IntPtr B, int ldb, float beta, IntPtr C, int ldc);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_dgemm(MklBlasLayout Layout, MklBlasOperation TransA, MklBlasOperation TransB, int m, int n, int k, double alpha, IntPtr A, int lda, IntPtr B, int ldb, double beta, IntPtr C, int ldc);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_cgemm(MklBlasLayout Layout, MklBlasOperation TransA, MklBlasOperation TransB, int m, int n, int k, void* alpha, IntPtr A, int lda, IntPtr B, int ldb, void* beta, IntPtr C, int ldc);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_zgemm(MklBlasLayout Layout, MklBlasOperation TransA, MklBlasOperation TransB, int m, int n, int k, void* alpha, IntPtr A, int lda, IntPtr B, int ldb, void* beta, IntPtr C, int ldc);
		#endregion

		#region symmetric Hermitian matrix multiply
		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_ssymm(MklBlasLayout Layout, MklBlasSideMode side, MklBlasFillMode uplo, int m, int n, float alpha, IntPtr A, int lda, IntPtr B, int ldb, float beta, IntPtr C, int ldc);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_dsymm(MklBlasLayout Layout, MklBlasSideMode side, MklBlasFillMode uplo, int m, int n, double alpha, IntPtr A, int lda, IntPtr B, int ldb, double beta, IntPtr C, int ldc);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_csymm(MklBlasLayout Layout, MklBlasSideMode side, MklBlasFillMode uplo, int m, int n, void* alpha, IntPtr A, int lda, IntPtr B, int ldb, void* beta, IntPtr C, int ldc);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_zsymm(MklBlasLayout Layout, MklBlasSideMode side, MklBlasFillMode uplo, int m, int n, void* alpha, IntPtr A, int lda, IntPtr B, int ldb, void* beta, IntPtr C, int ldc);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_chemm(MklBlasLayout Layout, MklBlasSideMode side, MklBlasFillMode uplo, int m, int n, void* alpha, IntPtr A, int lda, IntPtr B, int ldb, void* beta, IntPtr C, int ldc);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_zhemm(MklBlasLayout Layout, MklBlasSideMode side, MklBlasFillMode uplo, int m, int n, void* alpha, IntPtr A, int lda, IntPtr B, int ldb, void* beta, IntPtr C, int ldc);
		#endregion

		#region rank k update
		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_ssyrk(MklBlasLayout Layout, MklBlasFillMode uplo, MklBlasOperation trans, int n, int k, float alpha, IntPtr A, int lda, float beta, IntPtr C, int ldc);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_dsyrk(MklBlasLayout Layout, MklBlasFillMode uplo, MklBlasOperation trans, int n, int k, double alpha, IntPtr A, int lda, double beta, IntPtr C, int ldc);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_csyrk(MklBlasLayout Layout, MklBlasFillMode uplo, MklBlasOperation trans, int n, int k, void* alpha, IntPtr A, int lda, void* beta, IntPtr C, int ldc);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_zsyrk(MklBlasLayout Layout, MklBlasFillMode uplo, MklBlasOperation trans, int n, int k, void* alpha, IntPtr A, int lda, void* beta, IntPtr C, int ldc);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_cherk(MklBlasLayout Layout, MklBlasFillMode uplo, MklBlasOperation trans, int n, int k, void* alpha, IntPtr A, int lda, void* beta, IntPtr C, int ldc);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void cblas_zherk(MklBlasLayout Layout, MklBlasFillMode uplo, MklBlasOperation trans, int n, int k, void* alpha, IntPtr A, int lda, void* beta, IntPtr C, int ldc);
		#endregion
		#endregion


		#region BLAS like
		#region matrix add
		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void MKL_Somatadd(byte ordering, byte transa, byte transb, long rows, long cols, float alpha, IntPtr A, long lda, float beta, IntPtr B, long ldb, IntPtr C, long ldc);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void MKL_Domatadd(byte ordering, byte transa, byte transb, long rows, long cols, double alpha, IntPtr A, long lda, double beta, IntPtr B, long ldb, IntPtr C, long ldc);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void MKL_Comatadd(byte ordering, byte transa, byte transb, long rows, long cols, ComplexSingle alpha, IntPtr A, long lda, ComplexSingle beta, IntPtr B, long ldb, IntPtr C, long ldc);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void MKL_Zomatadd(byte ordering, byte transa, byte transb, long rows, long cols, ComplexDouble alpha, IntPtr A, long lda, ComplexDouble beta, IntPtr B, long ldb, IntPtr C, long ldc);
		#endregion

		#region matrix transpose
		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void MKL_Somatcopy(byte ordering, byte trans, long rows, long cols, float alpha, IntPtr A, long lda, IntPtr B, long ldb);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void MKL_Domatcopy(byte ordering, byte trans, long rows, long cols, double alpha, IntPtr A, long lda, IntPtr B, long ldb);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void MKL_Comatcopy(byte ordering, byte trans, long rows, long cols, ComplexSingle alpha, IntPtr A, long lda, IntPtr B, long ldb);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		internal static extern void MKL_Zomatcopy(byte ordering, byte trans, long rows, long cols, ComplexDouble alpha, IntPtr A, long lda, IntPtr B, long ldb);
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
	}
}
