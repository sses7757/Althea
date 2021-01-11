using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Althea.Blas.Mkl
{
	/// <summary>
	/// MKL BLAS library API
	/// </summary>
	public static class NativeMethods
	{
		/// <summary>
		/// The MKL BLAS library name
		/// </summary>
		public const string MKLBLAS_API_DLL_NAME = "mkl_rt";


		#region level 1
		/// <summary>
		/// This function finds the (smallest) index of the element of the maximum magnitude. ($Re|x|+Im|x|$ if the value is a complex one)
		/// </summary>
		/// <param name="n">number of elements in the vector <paramref name="x"/></param>
		/// <param name="x">vector with elements</param>
		/// <param name="incx">stride between consecutive elements of <paramref name="x"/></param>
		/// <returns>the index + 1</returns>
		internal delegate long amaxFunc(int n, IntPtr x, int incx);
		#region abs max
		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern long cblas_isamax(int n, IntPtr x, int incx);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern long cblas_idamax(int n, IntPtr x, int incx);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern long cblas_icamax(int n, IntPtr x, int incx);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern long cblas_izamax(int n, IntPtr x, int incx);
		#endregion

		/// <summary>
		/// This function finds the (smallest) index of the element of the minimum magnitude. ($Re|x|+Im|x|$ if the value is a complex one)
		/// </summary>
		/// <param name="n">number of elements in the vector <paramref name="x"/></param>
		/// <param name="x">vector with elements</param>
		/// <param name="incx">stride between consecutive elements of <paramref name="x"/></param>
		/// <returns>the index + 1</returns>
		internal delegate long aminFunc(int n, IntPtr x, int incx);
		#region abs min
		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern long cblas_isamin(int n, IntPtr x, int incx);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern long cblas_idamin(int n, IntPtr x, int incx);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern long cblas_icamin(int n, IntPtr x, int incx);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern long cblas_izamin(int n, IntPtr x, int incx);
		#endregion


		/// <summary>
		/// Computes the sum of magnitudes of the vector elements. ($Re|x|+Im|x|$ if the value is a complex one)
		/// </summary>
		/// <param name="N">Specifies the number of elements in vector <paramref name="X"/></param>
		/// <param name="X">Array, size at least <c>(1 + (<paramref name="N"/>-1)*abs(<paramref name="incX"/>))</c></param>
		/// <param name="incX">Specifies the increment for indexing vector <paramref name="X"/></param>
		/// <returns>Contains the sum of magnitudes of real and imaginary parts of all elements of the vector.</returns>
		internal delegate T asumFunc<T>(int N, [In] IntPtr X, int incX);
		#region abs sum
		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern float cblas_sasum(int N, [In] IntPtr X, int incX);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern double cblas_dasum(int N, [In] IntPtr X, int incX);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern float cblas_scasum(int N, [In] IntPtr X, int incX);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern double cblas_dzasum(int N, [In] IntPtr X, int incX);
		#endregion


		/// <summary>
		/// Computes the Euclidean norm of a vector.
		/// </summary>
		/// <param name="N">Specifies the number of elements in vector <paramref name="X"/></param>
		/// <param name="X">Array, size at least <c>(1 + (<paramref name="N"/>-1)*abs(<paramref name="incX"/>))</c></param>
		/// <param name="incX">Specifies the increment for indexing vector <paramref name="X"/></param>
		/// <returns>The Euclidean norm of the vector.</returns>
		internal delegate T nrm2Func<T>(int N, [In] IntPtr X, int incX);
		#region norm
		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern float cblas_snrm2(int N, [In] IntPtr X, int incX);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern double cblas_dnrm2(int N, [In] IntPtr X, int incX);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern float cblas_scnrm2(int N, [In] IntPtr X, int incX);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern double cblas_dznrm2(int N, [In] IntPtr X, int incX);
		#endregion


		/// <summary>
		/// Computes a vector-scalar product and adds the result to a vector.
		/// </summary>
		/// <param name="N">Specifies the number of elements in vectors <paramref name="X"/> and <paramref name="Y"/></param>
		/// <param name="alpha">Specifies the scalar α</param>
		/// <param name="X">Array, size at least <c>(1 + (<paramref name="N"/>-1)*abs(<paramref name="incX"/>))</c></param>
		/// <param name="incX">Specifies the increment for the elements of <paramref name="X"/></param>
		/// <param name="Y">Array, size at least <c>(1 + (<paramref name="N"/>-1)*abs(<paramref name="incY"/>))</c></param>
		/// <param name="incY">Specifies the increment for the elements of <paramref name="Y"/></param>
		internal delegate void axpyFuncReal<T>(int N, T alpha, [In] IntPtr X, int incX, IntPtr Y, int incY);

		internal delegate void axpyFuncComplex<T>(int N, ref T alpha, [In] IntPtr X, int incX, IntPtr Y, int incY);
		#region alpha X add to Y
		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void cblas_saxpy(int N, float alpha, [In] IntPtr X, int incX, IntPtr Y, int incY);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void cblas_daxpy(int N, double alpha, [In] IntPtr X, int incX, IntPtr Y, int incY);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void cblas_caxpy(int N, ref FloatComplex alpha, [In] IntPtr X, int incX, IntPtr Y, int incY);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void cblas_zaxpy(int N, ref DoubleComplex alpha, [In] IntPtr X, int incX, IntPtr Y, int incY);
		#endregion


		/// <summary>
		/// Computes the product of a vector by a scalar.
		/// </summary>
		/// <param name="n">Specifies the number of elements in vectors <paramref name="x"/></param>
		/// <param name="alpha">Specifies the scalar α</param>
		/// <param name="x">Array, size at least <c>(1 + (<paramref name="n"/>-1)*abs(<paramref name="incx"/>))</c></param>
		/// <param name="incx">Specifies the increment for the elements of <paramref name="x"/></param>
		internal delegate void scalFuncReal<T>(int n, T alpha, IntPtr x, int incx);

		internal delegate void scalFuncComplex<T>(int n, ref T alpha, IntPtr x, int incx);
		#region scale
		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void cblas_sscal(int n, float alpha, IntPtr x, int incx);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void cblas_dscal(int n, double alpha, IntPtr x, int incx);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void cblas_cscal(int n, ref FloatComplex alpha, IntPtr x, int incx);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void cblas_zscal(int n, ref DoubleComplex alpha, IntPtr x, int incx);
		#endregion


		/// <summary>
		/// Copies vector to another vector.
		/// </summary>
		/// <param name="N">Specifies the number of elements in vectors <paramref name="X"/> and <paramref name="Y"/></param>
		/// <param name="X">Array, size at least <c>(1 + (<paramref name="N"/>-1)*abs(<paramref name="incX"/>))</c></param>
		/// <param name="incX">Specifies the increment for the elements of <paramref name="X"/></param>
		/// <param name="Y">Array, size at least <c>(1 + (<paramref name="N"/>-1)*abs(<paramref name="incY"/>))</c></param>
		/// <param name="incY">Specifies the increment for the elements of <paramref name="Y"/></param>
		internal delegate void copyFunc(int N, [In] IntPtr X, int incX, IntPtr Y, int incY);
		#region copy
		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void cblas_scopy(int N, [In] IntPtr X, int incX, IntPtr Y, int incY);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void cblas_dcopy(int N, [In] IntPtr X, int incX, IntPtr Y, int incY);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void cblas_ccopy(int N, [In] IntPtr X, int incX, IntPtr Y, int incY);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void cblas_zcopy(int N, [In] IntPtr X, int incX, IntPtr Y, int incY);
		#endregion


		/// <summary>
		/// Computes a vector-vector dot product.
		/// </summary>
		/// <param name="N">Specifies the number of elements in vectors <paramref name="X"/> and <paramref name="Y"/></param>
		/// <param name="X">Array, size at least <c>(1 + (<paramref name="N"/>-1)*abs(<paramref name="incX"/>))</c></param>
		/// <param name="incX">Specifies the increment for the elements of <paramref name="X"/></param>
		/// <param name="Y">Array, size at least <c>(1 + (<paramref name="N"/>-1)*abs(<paramref name="incY"/>))</c></param>
		/// <param name="incY">Specifies the increment for the elements of <paramref name="Y"/></param>
		/// <returns>The result of the dot product of <paramref name="X"/> and <paramref name="Y"/>, if <paramref name="N"/> is positive. Otherwise, returns 0.</returns>
		internal delegate T dotFuncReal<T>(int N, [In] IntPtr X, int incX, [In] IntPtr Y, int incY);

		internal delegate void dotFuncComplex<T>(int N, [In] IntPtr X, int incX, [In] IntPtr Y, int incY, ref T dot);
		#region dot
		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern float cblas_sdot(int N, [In] IntPtr X, int incX, [In] IntPtr Y, int incY);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern double cblas_ddot(int N, [In] IntPtr X, int incX, [In] IntPtr Y, int incY);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void cblas_cdotu_sub(int N, [In] IntPtr X, int incX, [In] IntPtr Y, int incY, ref FloatComplex dotu);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void cblas_cdotc_sub(int N, [In] IntPtr X, int incX, [In] IntPtr Y, int incY, ref FloatComplex dotc);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void cblas_zdotu_sub(int N, [In] IntPtr X, int incX, [In] IntPtr Y, int incY, ref DoubleComplex dotu);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void cblas_zdotc_sub(int N, [In] IntPtr X, int incX, [In] IntPtr Y, int incY, ref DoubleComplex dotc);
		#endregion
		#endregion


		#region level 2
		/// <summary>
		/// Computes a matrix-vector product using a general matrix.
		/// </summary>
		/// <param name="Layout">Specifies whether two-dimensional array storage is row-major or column-major</param>
		/// <param name="trans">Specifies the form of <c>op(<paramref name="A"/>)</c> used in the matrix multiplication:
		/// <br/><list type="bullet">
		/// <item><description>if it is <see cref="MatrixOperation.NoneTranspose"/>, then <c>op(<paramref name="A"/>) = <paramref name="A"/></c>;</description></item>
		/// <item><description>if it is <see cref="MatrixOperation.Transpose"/>, then <c>op(<paramref name="A"/>) = <paramref name="A"/><sup>T</sup></c>;</description></item>
		/// <item><description>if it is <see cref="MatrixOperation.ConjugateTranspose"/>, then <c>op(<paramref name="A"/>) = <paramref name="A"/><sup>H</sup></c>;</description></item>
		/// </list></param>
		/// <param name="m">Specifies the number of rows of the matrix <paramref name="A"/>. The value must be at least zero</param>
		/// <param name="n">Specifies the number of columns of the matrix <paramref name="A"/>. The value must be at least zero</param>
		/// <param name="alpha">Scalar to multiply <c>op(<paramref name="A"/>)*<paramref name="x"/></c></param>
		/// <param name="A">Matrix with leading dimension <paramref name="lda"/></param>
		/// <param name="lda">leading dimension of <paramref name="A"/></param>
		/// <param name="x">Vector with size at least <c>(1+(<paramref name="n"/>-1)*abs(<paramref name="incx"/>))</c> when <paramref name="trans"/> == <see cref="MatrixOperation.NoneTranspose"/> and at least <c>(1+(<paramref name="m"/>-1)*abs(<paramref name="incx"/>))</c> otherwise.</param>
		/// <param name="incx">stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="beta">Scalar to multiply <paramref name="y"/></param>
		/// <param name="y">Vector with size at least <c>(1+(<paramref name="m"/>-1)*abs(<paramref name="incy"/>))</c> when <paramref name="trans"/> == <see cref="MatrixOperation.NoneTranspose"/> and at least <c>(1+(<paramref name="n"/>-1)*abs(<paramref name="incy"/>))</c> otherwise.</param>
		/// <param name="incy">stride between consecutive elements of <paramref name="y"/></param>
		internal delegate void gemvFuncReal<T>(MklBlasLayout Layout, MatrixOperation trans, int m, int n, T alpha, [In] IntPtr A, int lda, [In] IntPtr x, int incx, T beta, IntPtr y, int incy);

		internal delegate void gemvFuncComplex<T>(MklBlasLayout Layout, MatrixOperation trans, int m, int n, ref T alpha, [In] IntPtr A, int lda, [In] IntPtr x, int incx, ref T beta, IntPtr y, int incy);
		#region general matrix vector multiply
		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void cblas_sgemv(MklBlasLayout Layout, MatrixOperation trans, int m, int n, float alpha, [In] IntPtr A, int lda, [In] IntPtr x, int incx, float beta, IntPtr y, int incy);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void cblas_dgemv(MklBlasLayout Layout, MatrixOperation trans, int m, int n, double alpha, [In] IntPtr A, int lda, [In] IntPtr x, int incx, double beta, IntPtr y, int incy);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void cblas_cgemv(MklBlasLayout Layout, MatrixOperation trans, int m, int n, ref FloatComplex alpha, [In] IntPtr A, int lda, [In] IntPtr x, int incx, ref FloatComplex beta, IntPtr y, int incy);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void cblas_zgemv(MklBlasLayout Layout, MatrixOperation trans, int m, int n, ref DoubleComplex alpha, [In] IntPtr A, int lda, [In] IntPtr x, int incx, ref DoubleComplex beta, IntPtr y, int incy);
		#endregion


		/// <summary>
		/// Computes a matrix-vector product using a general matrix.
		/// </summary>
		/// <param name="Layout">Specifies whether two-dimensional array storage is row-major or column-major</param>
		/// <param name="uplo">Specifies which part of matrix <paramref name="A"/> is stored</param>
		/// <param name="n">Specifies the number of columns and rows of the matrix <paramref name="A"/>. The value must be at least zero</param>
		/// <param name="alpha">Scalar to multiply <c>op(<paramref name="A"/>)*<paramref name="x"/></c></param>
		/// <param name="A">Matrix with leading dimension <paramref name="lda"/></param>
		/// <param name="lda">leading dimension of <paramref name="A"/></param>
		/// <param name="x">Vector with size at least <c>(1+(<paramref name="n"/>-1)*abs(<paramref name="incx"/>))</c>\</param>
		/// <param name="incx">stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="beta">Scalar to multiply <paramref name="y"/></param>
		/// <param name="y">Vector with size at least <c>(1+(<paramref name="n"/>-1)*abs(<paramref name="incy"/>))</c></param>
		/// <param name="incy">stride between consecutive elements of <paramref name="y"/></param>
		internal delegate void symvFuncReal<T>(MklBlasLayout Layout, MatrixFillMode uplo, int n, T alpha, [In] IntPtr A, int lda, [In] IntPtr x, int incx, T beta, IntPtr y, int incy);

		internal delegate void symvFuncComplex<T>(MklBlasLayout Layout, MatrixFillMode uplo, int n, ref T alpha, [In] IntPtr A, int lda, [In] IntPtr x, int incx, ref T beta, IntPtr y, int incy);
		#region symmetric Hermitian matrix vector multiply
		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void cblas_ssymv(MklBlasLayout Layout, MatrixFillMode uplo, int n, float alpha, [In] IntPtr A, int lda, [In] IntPtr x, int incx, float beta, IntPtr y, int incy);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void cblas_dsymv(MklBlasLayout Layout, MatrixFillMode uplo, int n, double alpha, [In] IntPtr A, int lda, [In] IntPtr x, int incx, double beta, IntPtr y, int incy);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void cblas_chemv(MklBlasLayout Layout, MatrixFillMode uplo, int n, ref FloatComplex alpha, [In] IntPtr A, int lda, [In] IntPtr x, int incx, ref FloatComplex beta, IntPtr y, int incy);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void cblas_zhemv(MklBlasLayout Layout, MatrixFillMode uplo, int n, ref DoubleComplex alpha, [In] IntPtr A, int lda, [In] IntPtr x, int incx, ref DoubleComplex beta, IntPtr y, int incy);
		#endregion


		/// <summary>
		/// Performs a rank-1 update of a general matrix.
		/// </summary>
		/// <param name="Layout">Specifies whether two-dimensional array storage is row-major or column-major</param>
		/// <param name="m">Specifies the number of rows of the matrix <paramref name="A"/>. The value of m must be at least zero</param>
		/// <param name="n">Specifies the number of columns of the matrix <paramref name="A"/>. The value of m must be at least zero</param>
		/// <param name="alpha">Scalar to multiply <c><paramref name="x"/>*<paramref name="y"/></c></param>
		/// <param name="x">Vector with size at least <c>(1+(<paramref name="m"/>-1)*abs(<paramref name="incx"/>))</c></param>
		/// <param name="incx">stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="y">Vector with size at least <c>(1+(<paramref name="n"/>-1)*abs(<paramref name="incy"/>))</c></param>
		/// <param name="incy">stride between consecutive elements of <paramref name="y"/></param>
		/// <param name="A">Matrix with leading dimension <paramref name="lda"/></param>
		/// <param name="lda">leading dimension of <paramref name="A"/></param>
		internal delegate void gerFuncReal<T>(MklBlasLayout Layout, int m, int n, T alpha, [In] IntPtr x, int incx, [In] IntPtr y, int incy, IntPtr A, int lda);

		internal delegate void gerFuncComplex<T>(MklBlasLayout Layout, int m, int n, ref T alpha, [In] IntPtr x, int incx, [In] IntPtr y, int incy, IntPtr A, int lda);
		#region general rank one
		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void cblas_sger(MklBlasLayout Layout, int m, int n, float alpha, [In] IntPtr x, int incx, [In] IntPtr y, int incy, IntPtr A, int lda);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void cblas_dger(MklBlasLayout Layout, int m, int n, double alpha, [In] IntPtr x, int incx, [In] IntPtr y, int incy, IntPtr A, int lda);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void cblas_cgerc(MklBlasLayout Layout, int m, int n, ref FloatComplex alpha, [In] IntPtr x, int incx, [In] IntPtr y, int incy, IntPtr A, int lda);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void cblas_cgeru(MklBlasLayout Layout, int m, int n, ref FloatComplex alpha, [In] IntPtr x, int incx, [In] IntPtr y, int incy, IntPtr A, int lda);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void cblas_zgerc(MklBlasLayout Layout, int m, int n, ref DoubleComplex alpha, [In] IntPtr x, int incx, [In] IntPtr y, int incy, IntPtr A, int lda);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void cblas_zgeru(MklBlasLayout Layout, int m, int n, ref DoubleComplex alpha, [In] IntPtr x, int incx, [In] IntPtr y, int incy, IntPtr A, int lda);
		#endregion


		/// <summary>
		/// Performs a rank-1 update of a symmetric/Hermitian matrix.
		/// </summary>
		/// <param name="Layout">Specifies whether two-dimensional array storage is row-major or column-major</param>
		/// <param name="uplo">Specifies which part of matrix <paramref name="A"/> is stored</param>
		/// <param name="n">Specifies the number of rows and columns of the matrix <paramref name="A"/>. The value of m must be at least zero</param>
		/// <param name="alpha">Scalar to multiply <c><paramref name="x"/>*<paramref name="x"/><sup>T</sup></c></param>
		/// <param name="x">Vector with size at least <c>(1+(<paramref name="n"/>-1)*abs(<paramref name="incx"/>))</c></param>
		/// <param name="incx">stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="A">Matrix with leading dimension <paramref name="lda"/></param>
		/// <param name="lda">leading dimension of <paramref name="A"/></param>
		internal delegate void syrFuncReal<T>(MklBlasLayout Layout, MatrixFillMode uplo, int n, T alpha, [In] IntPtr x, int incx, IntPtr A, int lda);

		internal delegate void syrFuncComplex<T>(MklBlasLayout Layout, MatrixFillMode uplo, int n, ref T alpha, [In] IntPtr x, int incx, IntPtr A, int lda);
		#region symmetric Hermitian rank one
		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void cblas_ssyr(MklBlasLayout Layout, MatrixFillMode uplo, int n, float alpha, [In] IntPtr x, int incx, IntPtr A, int lda);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void cblas_dsyr(MklBlasLayout Layout, MatrixFillMode uplo, int n, double alpha, [In] IntPtr x, int incx, IntPtr A, int lda);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void cblas_cher(MklBlasLayout Layout, MatrixFillMode uplo, int n, ref FloatComplex alpha, [In] IntPtr x, int incx, IntPtr A, int lda);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void cblas_zher(MklBlasLayout Layout, MatrixFillMode uplo, int n, ref DoubleComplex alpha, [In] IntPtr x, int incx, IntPtr A, int lda);
		#endregion
		#endregion


		#region level 3
		/// <summary>
		/// Computes a matrix-matrix product with general matrices.
		/// </summary>
		/// <param name="Layout">Specifies whether two-dimensional array storage is row-major or column-major</param>
		/// <param name="TransA">Specifies the form of <c>op(<paramref name="A"/>)</c> used in the matrix multiplication:
		/// <br/><list type="bullet">
		/// <item><description>if it is <see cref="MatrixOperation.NoneTranspose"/>, then <c>op(<paramref name="A"/>) = <paramref name="A"/></c>;</description></item>
		/// <item><description>if it is <see cref="MatrixOperation.Transpose"/>, then <c>op(<paramref name="A"/>) = <paramref name="A"/><sup>T</sup></c>;</description></item>
		/// <item><description>if it is <see cref="MatrixOperation.ConjugateTranspose"/>, then <c>op(<paramref name="A"/>) = <paramref name="A"/><sup>H</sup></c>;</description></item>
		/// </list></param>
		/// <param name="TransB">Specifies the form of <c>op(<paramref name="B"/>)</c> used in the matrix multiplication:
		/// <br/><list type="bullet">
		/// <item><description>if it is <see cref="MatrixOperation.NoneTranspose"/>, then <c>op(<paramref name="B"/>) = <paramref name="B"/></c>;</description></item>
		/// <item><description>if it is <see cref="MatrixOperation.Transpose"/>, then <c>op(<paramref name="B"/>) = <paramref name="B"/><sup>T</sup></c>;</description></item>
		/// <item><description>if it is <see cref="MatrixOperation.ConjugateTranspose"/>, then <c>op(<paramref name="B"/>) = <paramref name="B"/><sup>H</sup></c>;</description></item>
		/// </list></param>
		/// <param name="m">Specifies the number of rows of the matrix <c>op(<paramref name="A"/>)</c> and of the matrix <paramref name="C"/>. The value must be at least zero.</param>
		/// <param name="n">Specifies the number of columns of the matrix <c>op(<paramref name="B"/>)</c> and of the matrix <paramref name="C"/>. The value must be at least zero.</param>
		/// <param name="k">Specifies the number of columns of the matrix <c>op(<paramref name="A"/>)</c> and the number of rows of matrix <paramref name="C"/>. The value must be at least zero.</param>
		/// <param name="alpha">Scalar to multiply <c>op(<paramref name="A"/>)*op(<paramref name="B"/>)</c></param>
		/// <param name="A">Matrix with leading dimension <paramref name="lda"/></param>
		/// <param name="lda">leading dimension of <paramref name="A"/></param>
		/// <param name="B">Matrix with leading dimension <paramref name="ldb"/></param>
		/// <param name="ldb">leading dimension of <paramref name="B"/></param>
		/// <param name="beta">Scalar to multiply <paramref name="C"/></param>
		/// <param name="C">Matrix with leading dimension <paramref name="ldc"/></param>
		/// <param name="ldc">leading dimension of <paramref name="C"/></param>
		internal delegate void gemmFuncReal<T>(MklBlasLayout Layout, MatrixOperation TransA, MatrixOperation TransB, int m, int n, int k, T alpha, [In] IntPtr A, int lda, [In] IntPtr B, int ldb, T beta, IntPtr C, int ldc);

		internal delegate void gemmFuncComplex<T>(MklBlasLayout Layout, MatrixOperation TransA, MatrixOperation TransB, int m, int n, int k, ref T alpha, [In] IntPtr A, int lda, [In] IntPtr B, int ldb, ref T beta, IntPtr C, int ldc);
		#region general matrix multiply
		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void cblas_sgemm(MklBlasLayout Layout, MatrixOperation TransA, MatrixOperation TransB, int m, int n, int k, float alpha, [In] IntPtr A, int lda, [In] IntPtr B, int ldb, float beta, IntPtr C, int ldc);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void cblas_dgemm(MklBlasLayout Layout, MatrixOperation TransA, MatrixOperation TransB, int m, int n, int k, double alpha, [In] IntPtr A, int lda, [In] IntPtr B, int ldb, double beta, IntPtr C, int ldc);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void cblas_cgemm(MklBlasLayout Layout, MatrixOperation TransA, MatrixOperation TransB, int m, int n, int k, ref FloatComplex alpha, [In] IntPtr A, int lda, [In] IntPtr B, int ldb, ref FloatComplex beta, IntPtr C, int ldc);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void cblas_zgemm(MklBlasLayout Layout, MatrixOperation TransA, MatrixOperation TransB, int m, int n, int k, ref DoubleComplex alpha, [In] IntPtr A, int lda, [In] IntPtr B, int ldb, ref DoubleComplex beta, IntPtr C, int ldc);
		#endregion


		/// <summary>
		/// Computes a matrix-matrix product where one input matrix is symmetric.
		/// </summary>
		/// <param name="Layout">Specifies whether two-dimensional array storage is row-major or column-major</param>
		/// <param name="side">Specifies the side of matrix <paramref name="A"/></param>
		/// <param name="uplo">Specifies which part of matrix <paramref name="A"/> is stored</param>
		/// <param name="m">Specifies the number of rows of the matrix <paramref name="B"/> and <paramref name="C"/>. The value must be at least zero.</param>
		/// <param name="n">Specifies the number of columns of the matrix <paramref name="B"/> and <paramref name="C"/>. The value must be at least zero.</param>
		/// <param name="alpha">Scalar to multiply <c><paramref name="A"/>*<paramref name="B"/></c> or <c><paramref name="B"/>*<paramref name="A"/></c></param>
		/// <param name="A">Symmetric Matrix with leading dimension <paramref name="lda"/></param>
		/// <param name="lda">leading dimension of <paramref name="A"/></param>
		/// <param name="B">Matrix with leading dimension <paramref name="ldb"/></param>
		/// <param name="ldb">leading dimension of <paramref name="B"/></param>
		/// <param name="beta">Scalar to multiply <paramref name="C"/></param>
		/// <param name="C">Matrix with leading dimension <paramref name="ldc"/></param>
		/// <param name="ldc">leading dimension of <paramref name="C"/></param>
		internal delegate void symmFuncReal<T>(MklBlasLayout Layout, SideMode side, MatrixFillMode uplo, int m, int n, T alpha, [In] IntPtr A, int lda, [In] IntPtr B, int ldb, T beta, IntPtr C, int ldc);

		internal delegate void symmFuncComplex<T>(MklBlasLayout Layout, SideMode side, MatrixFillMode uplo, int m, int n, ref T alpha, [In] IntPtr A, int lda, [In] IntPtr B, int ldb, ref T beta, IntPtr C, int ldc);
		#region symmetric Hermitian matrix multiply
		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void cblas_ssymm(MklBlasLayout Layout, SideMode side, MatrixFillMode uplo, int m, int n, float alpha, [In] IntPtr A, int lda, [In] IntPtr B, int ldb, float beta, IntPtr C, int ldc);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void cblas_dsymm(MklBlasLayout Layout, SideMode side, MatrixFillMode uplo, int m, int n, double alpha, [In] IntPtr A, int lda, [In] IntPtr B, int ldb, double beta, IntPtr C, int ldc);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void cblas_csymm(MklBlasLayout Layout, SideMode side, MatrixFillMode uplo, int m, int n, ref FloatComplex alpha, [In] IntPtr A, int lda, [In] IntPtr B, int ldb, ref FloatComplex beta, IntPtr C, int ldc);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void cblas_zsymm(MklBlasLayout Layout, SideMode side, MatrixFillMode uplo, int m, int n, ref DoubleComplex alpha, [In] IntPtr A, int lda, [In] IntPtr B, int ldb, ref DoubleComplex beta, IntPtr C, int ldc);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void cblas_chemm(MklBlasLayout Layout, SideMode side, MatrixFillMode uplo, int m, int n, ref FloatComplex alpha, [In] IntPtr A, int lda, [In] IntPtr B, int ldb, ref FloatComplex beta, IntPtr C, int ldc);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void cblas_zhemm(MklBlasLayout Layout, SideMode side, MatrixFillMode uplo, int m, int n, ref DoubleComplex alpha, [In] IntPtr A, int lda, [In] IntPtr B, int ldb, ref DoubleComplex beta, IntPtr C, int ldc);
		#endregion


		/// <summary>
		/// Performs a symmetric/Hermitian rank-k update.
		/// </summary>
		/// <param name="Layout">Specifies whether two-dimensional array storage is row-major or column-major</param>
		/// <param name="uplo">Specifies which part of matrix <paramref name="A"/> is stored</param>
		/// <param name="trans">Specifies the form of <c>op(<paramref name="A"/>)</c> used in the matrix multiplication:
		/// <br/><list type="bullet">
		/// <item><description>if it is <see cref="MatrixOperation.NoneTranspose"/>, then <c>op(<paramref name="A"/>) = <paramref name="A"/></c>;</description></item>
		/// <item><description>if it is <see cref="MatrixOperation.Transpose"/>, then <c>op(<paramref name="A"/>) = <paramref name="A"/><sup>T</sup></c>;</description></item>
		/// <item><description>if it is <see cref="MatrixOperation.ConjugateTranspose"/>, then <c>op(<paramref name="A"/>) = <paramref name="A"/><sup>H</sup></c>;</description></item>
		/// </list></param>
		/// <param name="n">Specifies the number of rows of the matrix <c>op(<paramref name="A"/>)</c> and the number of columns and rows of matrix <paramref name="C"/>. The value must be at least zero.</param>
		/// <param name="k">Specifies the number of columns of the matrix <c>op(<paramref name="A"/>)</c>. The value must be at least zero.</param>
		/// <param name="alpha">Scalar to multiply <c>op(<paramref name="A"/>)*op(<paramref name="A"/>)^H</c></param>
		/// <param name="A">Matrix with leading dimension <paramref name="lda"/></param>
		/// <param name="lda">leading dimension of <paramref name="A"/></param>
		/// <param name="beta">Scalar to multiply <paramref name="C"/></param>
		/// <param name="C">Symmetric / Hermitian matrix with leading dimension <paramref name="ldc"/></param>
		/// <param name="ldc">leading dimension of <paramref name="C"/></param>
		internal delegate void syrkFuncReal<T>(MklBlasLayout Layout, MatrixFillMode uplo, MatrixOperation trans, int n, int k, T alpha, [In] IntPtr A, int lda, T beta, IntPtr C, int ldc);

		internal delegate void syrkFuncComplex<T>(MklBlasLayout Layout, MatrixFillMode uplo, MatrixOperation trans, int n, int k, ref T alpha, [In] IntPtr A, int lda, ref T beta, IntPtr C, int ldc);
		#region rank k update
		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void cblas_ssyrk(MklBlasLayout Layout, MatrixFillMode uplo, MatrixOperation trans, int n, int k, float alpha, [In] IntPtr A, int lda, float beta, IntPtr C, int ldc);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void cblas_dsyrk(MklBlasLayout Layout, MatrixFillMode uplo, MatrixOperation trans, int n, int k, double alpha, [In] IntPtr A, int lda, double beta, IntPtr C, int ldc);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void cblas_csyrk(MklBlasLayout Layout, MatrixFillMode uplo, MatrixOperation trans, int n, int k, ref FloatComplex alpha, [In] IntPtr A, int lda, ref FloatComplex beta, IntPtr C, int ldc);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void cblas_zsyrk(MklBlasLayout Layout, MatrixFillMode uplo, MatrixOperation trans, int n, int k, ref DoubleComplex alpha, [In] IntPtr A, int lda, ref DoubleComplex beta, IntPtr C, int ldc);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void cblas_cherk(MklBlasLayout Layout, MatrixFillMode uplo, MatrixOperation trans, int n, int k, ref FloatComplex alpha, [In] IntPtr A, int lda, ref FloatComplex beta, IntPtr C, int ldc);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void cblas_zherk(MklBlasLayout Layout, MatrixFillMode uplo, MatrixOperation trans, int n, int k, ref DoubleComplex alpha, [In] IntPtr A, int lda, ref DoubleComplex beta, IntPtr C, int ldc);
		#endregion
		#endregion


		#region BLAS like
		/// <summary>
		/// Scales and sums two matrices including in addition to performing out-of-place transposition operations.
		/// </summary>
		/// <param name="ordering">Ordering of the matrix storage. If it is 'R' or 'r', the ordering is row-major. If it is 'C' or 'c', the ordering is column-major.</param>
		/// <param name="transa">Parameter that specifies the operation type on matrix <paramref name="A"/>. If it is 'N' or 'n', op(<paramref name="A"/>)=<paramref name="A"/> and the matrix <paramref name="A"/> is assumed unchanged on input. If it is 'T' or 't', it is assumed that <paramref name="A"/> should be transposed. If it is 'C' or 'c', it is assumed that <paramref name="A"/> should be conjugate transposed. If it is 'R' or 'r', it is assumed that <paramref name="A"/> should be conjugated (and not transposed). If the data is real, then it is 'R' is the same as it is 'N', and it is 'C' is the same as it is 'T'.</param>
		/// <param name="transb">Parameter that specifies the operation type on matrix <paramref name="B"/>. If it is 'N' or 'n', op(<paramref name="B"/>)=<paramref name="B"/> and the matrix <paramref name="B"/> is assumed unchanged on input. If it is 'T' or 't', it is assumed that <paramref name="B"/> should be transposed. If it is 'C' or 'c', it is assumed that <paramref name="B"/> should be conjugate transposed. If it is 'R' or 'r', it is assumed that <paramref name="B"/> should be conjugated (and not transposed). If the data is real, then it is 'R' is the same as it is 'N', and it is 'C' is the same as it is 'T'.</param>
		/// <param name="rows">The number of matrix rows in op(<paramref name="A"/>), op(<paramref name="B"/>), and <paramref name="C"/>.</param>
		/// <param name="cols">The number of matrix columns in op(<paramref name="A"/>), op(<paramref name="B"/>), and <paramref name="C"/>.</param>
		/// <param name="alpha">This parameter scales the input matrix <paramref name="A"/> by α</param>
		/// <param name="A">Matrix</param>
		/// <param name="lda">leading dimension of <paramref name="A"/></param>
		/// <param name="beta">This parameter scales the input matrix <paramref name="B"/> by β</param>
		/// <param name="B">Matrix</param>
		/// <param name="ldb">leading dimension of <paramref name="B"/></param>
		/// <param name="C">Output matrix</param>
		/// <param name="ldc">leading dimension of <paramref name="C"/></param>
		internal delegate void geamFunc<T>(byte ordering, byte transa, byte transb, long rows, long cols, T alpha, [In] IntPtr A, long lda, T beta, [In] IntPtr B, long ldb, [In] IntPtr C, long ldc);
		#region matrix add
		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void MKL_Somatadd(byte ordering, byte transa, byte transb, long rows, long cols, float alpha, [In] IntPtr A, long lda, float beta, [In] IntPtr B, long ldb, [In] IntPtr C, long ldc);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void MKL_Domatadd(byte ordering, byte transa, byte transb, long rows, long cols, double alpha, [In] IntPtr A, long lda, double beta, [In] IntPtr B, long ldb, [In] IntPtr C, long ldc);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void MKL_Comatadd(byte ordering, byte transa, byte transb, long rows, long cols, FloatComplex alpha, [In] IntPtr A, long lda, FloatComplex beta, [In] IntPtr B, long ldb, [In] IntPtr C, long ldc);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void MKL_Zomatadd(byte ordering, byte transa, byte transb, long rows, long cols, DoubleComplex alpha, [In] IntPtr A, long lda, DoubleComplex beta, [In] IntPtr B, long ldb, [In] IntPtr C, long ldc);
		#endregion

		/// <summary>
		/// Performs scaling and out-place transposition/copying of matrices.
		/// </summary>
		/// <param name="ordering">Ordering of the matrix storage. If it is 'R' or 'r', the ordering is row-major. If it is 'C' or 'c', the ordering is column-major.</param>
		/// <param name="trans">Parameter that specifies the operation type on matrix <paramref name="A"/>. If it is 'N' or 'n', op(<paramref name="A"/>)=<paramref name="A"/> and the matrix <paramref name="A"/> is assumed unchanged on input. If it is 'T' or 't', it is assumed that <paramref name="A"/> should be transposed. If it is 'C' or 'c', it is assumed that <paramref name="A"/> should be conjugate transposed. If it is 'R' or 'r', it is assumed that <paramref name="A"/> should be conjugated (and not transposed). If the data is real, then it is 'R' is the same as it is 'N', and it is 'C' is the same as it is 'T'.</param>
		/// <param name="rows">The number of rows in matrix <paramref name="A"/></param>
		/// <param name="cols">The number of columns in matrix <paramref name="A"/></param>
		/// <param name="alpha">This parameter scales the input matrix <paramref name="A"/> by alpha</param>
		/// <param name="A">The input matrix <paramref name="A"/></param>
		/// <param name="lda">leading dimension of <paramref name="A"/></param>
		/// <param name="B">The output matrix <paramref name="B"/></param>
		/// <param name="ldb">leading dimension of <paramref name="B"/></param>
		internal delegate void transFunc<T>(byte ordering, byte trans, long rows, long cols, T alpha, [In] IntPtr A, long lda, IntPtr B, long ldb);
		#region matrix transpose
		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void MKL_Somatcopy(byte ordering, byte trans, long rows, long cols, float alpha, [In] IntPtr A, long lda, IntPtr B, long ldb);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void MKL_Domatcopy(byte ordering, byte trans, long rows, long cols, double alpha, [In] IntPtr A, long lda, IntPtr B, long ldb);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void MKL_Comatcopy(byte ordering, byte trans, long rows, long cols, FloatComplex alpha, [In] IntPtr A, long lda, IntPtr B, long ldb);

		[DllImport(MKLBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void MKL_Zomatcopy(byte ordering, byte trans, long rows, long cols, DoubleComplex alpha, [In] IntPtr A, long lda, IntPtr B, long ldb);
		#endregion
		#endregion
	}
}
