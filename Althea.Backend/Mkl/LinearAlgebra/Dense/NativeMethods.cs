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
		internal static extern void cblas_saxpy(long n, float alpha, void* x, long incx, void* y, long incy);

		[NativeMethod(6, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_sscal(long n, float alpha, void* x, long incx);

		[NativeMethod(6)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_scopy(long n, void* x, long incx, void* y, long incy);

		// Ignore Spelling: sdot ddot
		[CustomNativeMethod(6, "float", "sdot")]
		[CustomNativeMethod(6, "double", "ddot")]
		[CustomNativeMethod(6, "Complex<float>", "cdotu_sub", "", "Complex<float>", true)]
		[CustomNativeMethod(6, "Complex<float>", "cdotc_sub", "", "Complex<float>", true)]
		[CustomNativeMethod(6, "Complex<double>", "zdotu_sub", "", "Complex<double>", true)]
		[CustomNativeMethod(6, "Complex<double>", "zdotc_sub", "", "Complex<double>", true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern float cblas_sdot(long n, void* x, long incx, void* y, long incy);
		#endregion


		#region level 2
		[NativeMethod(6, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_sgemv(MklMatrixLayout Layout, MklOperation trans, long m, long n, float alpha, void* A, long lda, void* x, long incx, float beta, void* y, long incy);

		[NativeMethod(6, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_ssymv(MklMatrixLayout Layout, MklFillMode uplo, long n, float alpha, void* A, long lda, void* x, long incx, float beta, void* y, long incy);

		[NativeMethod(6)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_strmv(MklMatrixLayout Layout, MklFillMode Uplo, MklOperation TransA, MklBlasDiagType Diag, long N, void* A, long lda, void* x, long incx);

		// Ignore Spelling: sger dger cgerc cgeru zgerc zgeru
		[CustomNativeMethod(6, "float", "sger")]
		[CustomNativeMethod(6, "double", "dger")]
		[CustomNativeMethod(6, "Complex<float>", "cgerc", "in", "Complex<float>")]
		[CustomNativeMethod(6, "Complex<float>", "cdotc_sub", "in", "Complex<float>")]
		[CustomNativeMethod(6, "Complex<double>", "zdotu_sub", "in", "Complex<double>")]
		[CustomNativeMethod(6, "Complex<double>", "zdotc_sub", "in", "Complex<double>")]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_sger(MklMatrixLayout Layout, long m, long n, float alpha, void* x, long incx, void* y, long incy, void* A, long lda);

		[NativeMethod(6, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_ssyr(MklMatrixLayout Layout, MklFillMode uplo, long n, float alpha, void* x, long incx, void* A, long lda);

		[NativeMethod(6, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_ssyr2(MklMatrixLayout Layout, MklFillMode uplo, long n, float alpha, void* x, long incx, void* y, long incy, void* A, long lda);
		#endregion


		#region level 3
		[NativeMethod(6, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_strsm(MklMatrixLayout Layout, MklBlasSideMode Side, MklFillMode Uplo, MklOperation TransA, MklBlasDiagType Diag, long M, long N, float alpha, void* A, long lda, void* B, long ldb);

		[NativeMethod(6, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_strmm(MklMatrixLayout Layout, MklBlasSideMode Side, MklFillMode Uplo, MklOperation TransA, MklBlasDiagType Diag, long M, long N, float alpha, void* A, long lda, void* B, long ldb);

		[NativeMethod(6, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_sgemm(MklMatrixLayout Layout, MklOperation TransA, MklOperation TransB, long m, long n, long k, float alpha, void* A, long lda, void* B, long ldb, float beta, void* C, long ldc);

		[CustomNativeMethod(6, "Complex<float>", "c", "in", "Complex<float>")]
		[CustomNativeMethod(6, "Complex<double>", "z", "in", "Complex<double>")]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_cgemm3m(MklMatrixLayout Layout, MklOperation TransA, MklOperation TransB, long m, long n, long k, Complex<float> alpha, void* A, long lda, void* B, long ldb, Complex<float> beta, void* C, long ldc);

		[NativeMethod(6, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_ssymm(MklMatrixLayout Layout, MklBlasSideMode side, MklFillMode uplo, long m, long n, float alpha, void* A, long lda, void* B, long ldb, float beta, void* C, long ldc);

		[CustomNativeMethod(6, "Complex<float>", "c", "in", "Complex<float>")]
		[CustomNativeMethod(6, "Complex<double>", "z", "in", "Complex<double>")]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_chemm(MklMatrixLayout Layout, MklBlasSideMode side, MklFillMode uplo, long m, long n, Complex<float> alpha, void* A, long lda, void* B, long ldb, Complex<float> beta, void* C, long ldc);

		[NativeMethod(6, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_ssyrk(MklMatrixLayout Layout, MklFillMode uplo, MklOperation trans, long n, long k, float alpha, void* A, long lda, float beta, void* C, long ldc);

		[CustomNativeMethod(6, "Complex<float>", "c", "in", "Complex<float>")]
		[CustomNativeMethod(6, "Complex<double>", "z", "in", "Complex<double>")]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_cherk(MklMatrixLayout Layout, MklFillMode uplo, MklOperation trans, long n, long k, Complex<float> alpha, void* A, long lda, Complex<float> beta, void* C, long ldc);

		[NativeMethod(6, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_ssyr2k(MklMatrixLayout Layout, MklFillMode uplo, MklOperation trans, long n, long k, float alpha, void* A, long lda, void* B, long ldb, float beta, void* C, long ldc);

		[CustomNativeMethod(6, "Complex<float>", "c", "in", "Complex<float>")]
		[CustomNativeMethod(6, "Complex<double>", "z", "in", "Complex<double>")]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_cher2k(MklMatrixLayout Layout, MklFillMode uplo, MklOperation trans, long n, long k, Complex<float> alpha, void* A, long lda, void* B, long ldb, Complex<float> beta, void* C, long ldc);
		#endregion


		#region BLAS like
		[NativeMethod(6)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_sdgmm_batch(MklMatrixLayout layout, in MklBlasSideMode side, in long m, in long n, in void* a, in long lda, in void* x, in long incx, in void* c, in long ldc, long group_count, in long group_size);

		[NativeMethod(4, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void MKL_Somatadd(MklMatrixLayoutChar ordering, MklOperationChar transa, MklOperationChar transb, long rows, long cols, float alpha, void* A, long lda, float beta, void* B, long ldb, void* C, long ldc);

		[NativeMethod(4, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void MKL_Somatcopy(MklMatrixLayoutChar ordering, MklOperationChar trans, long rows, long cols, float alpha, void* A, long lda, void* B, long ldb);

		[NativeMethod(4, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void MKL_Simatcopy(MklMatrixLayoutChar ordering, MklOperationChar trans, long rows, long cols, float alpha, void* A, long lda);
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
		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern void vecsMulDiv(DataType type, void* a, void* b, long N, long strideA, long strideB, bool multiply);

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
		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern void vecsAdd(DataType type, void* scalar, void* a, void* b, long N, long strideA, long strideB);

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
		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern bool vecsEq(DataType type, void* a, void* b, long N, long strideA, long strideB);

		/// <summary>
		/// In-place exponentiate the vector <paramref name="a"/> by a scalar exponent <paramref name="p"/>
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/></param>
		/// <param name="a">The vector to be in-place modified of <paramref name="type"/></param>
		/// <param name="p">The pointer to the scalar exponent of <paramref name="type"/></param>
		/// <param name="N">The number of elements to be operated</param>
		/// <param name="stride">The spacing between consecutive elements of <paramref name="a"/></param>
		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern void vecPowSameType(DataType type, void* a, void* p, long N, long stride);

		/// <summary>
		/// In-place exponentiate the vector <paramref name="a"/> by a scalar exponent <paramref name="p"/>
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/> (must be a complex type)</param>
		/// <param name="a">The vector to be in-place modified of <paramref name="type"/></param>
		/// <param name="p">The pointer to the scalar exponent of <paramref name="type"/>'s real corresponding type</param>
		/// <param name="N">The number of elements to be operated</param>
		/// <param name="stride">The spacing between consecutive elements of <paramref name="a"/></param>
		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern void vecPowRealType(DataType type, void* a, void* p, long N, long stride);

		/// <summary>
		/// In-place conjugate the vector <paramref name="a"/>
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/></param>
		/// <param name="a">The vector to be in-place modified of <paramref name="type"/></param>
		/// <param name="N">The number of elements to be operated</param>
		/// <param name="stride">The spacing between consecutive elements of <paramref name="a"/></param>
		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern void vecConj(DataType type, void* a, long N, long stride);

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
		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern void vecDataConvert(DataType srcType, DataType dstType, void* src, void* dst, long N, long strideSrc, long strideDst, bool toRealByAbs);

		/// <summary>
		/// In-place set the values in <paramref name="a"/> whose absolute values are less than or equal to the absolute value of <paramref name="threshold"/> to 0
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/></param>
		/// <param name="a">The vector to be in-place modified of <paramref name="type"/></param>
		/// <param name="threshold">The pointer to the threshold used to clip the vector <paramref name="a"/> of <paramref name="type"/></param>
		/// <param name="N">The number of elements to be operated</param>
		/// <param name="stride">The spacing between consecutive elements of <paramref name="a"/></param>
		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern void vecClip(DataType type, void* a, void* threshold, long N, long stride);

		/// <summary>
		/// In-place add all elements in vector <paramref name="a"/> with the given <paramref name="scalar"/>
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/></param>
		/// <param name="a">The vector to be in-place modified of <paramref name="type"/></param>
		/// <param name="scalar">The pointer to the scalar to add of <paramref name="type"/></param>
		/// <param name="N">The number of elements to be operated</param>
		/// <param name="stride">The spacing between consecutive elements of <paramref name="a"/></param>
		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern void vecAddScalar(DataType type, void* a, void* scalar, long N, long stride);

		/// <summary>
		/// In-place multiplies all elements in vector <paramref name="a"/> with the given <paramref name="scalar"/>
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/></param>
		/// <param name="a">The vector to be in-place modified of <paramref name="type"/></param>
		/// <param name="scalar">The pointer to the scalar to multiply of <paramref name="type"/></param>
		/// <param name="N">The number of elements to be operated</param>
		/// <param name="stride">The spacing between consecutive elements of <paramref name="a"/></param>
		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern void vecMulScalar(DataType type, void* a, void* scalar, long N, long stride);

		/// <summary>
		/// Sums all the elements in vector <paramref name="a"/>
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/></param>
		/// <param name="a">The vector to be summed of <paramref name="type"/></param>
		/// <param name="N">The number of elements to be operated</param>
		/// <param name="stride">The spacing between consecutive elements of <paramref name="a"/></param>
		/// <param name="outSum">The output sum as a pointer of <paramref name="type"/></param>
		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern void vecSum(DataType type, void* a, long N, long stride, void* outSum);

		/// <summary>
		/// Get the index of the element with minimum absolute value in vector <paramref name="a"/>
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/></param>
		/// <param name="a">The vector to be summed of <paramref name="type"/></param>
		/// <param name="N">The number of elements to be operated</param>
		/// <param name="stride">The spacing between consecutive elements of <paramref name="a"/></param>
		/// <returns>The index of the element</returns>
		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern long vecArgAbsMin(DataType type, void* a, long N, long stride);

		/// <summary>
		/// Get the index of the element with maximum absolute value in vector <paramref name="a"/>
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/></param>
		/// <param name="a">The vector to be summed of <paramref name="type"/></param>
		/// <param name="N">The number of elements to be operated</param>
		/// <param name="stride">The spacing between consecutive elements of <paramref name="a"/></param>
		/// <returns>The index of the element</returns>
		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern long vecArgAbsMax(DataType type, void* a, long N, long stride);

		/// <summary>
		/// Sums all the elements's absolute values in vector <paramref name="a"/>
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/></param>
		/// <param name="a">The vector to be summed of <paramref name="type"/></param>
		/// <param name="N">The number of elements to be operated</param>
		/// <param name="stride">The spacing between consecutive elements of <paramref name="a"/></param>
		/// <returns>The sum as a <see cref="double"/></returns>
		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern double vecAbsSum(DataType type, void* a, long N, long stride);

		/// <summary>
		/// Compute the 2-norm of the given vector <paramref name="a"/>
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/></param>
		/// <param name="a">The vector to be summed of <paramref name="type"/></param>
		/// <param name="N">The number of elements to be operated</param>
		/// <param name="stride">The spacing between consecutive elements of <paramref name="a"/></param>
		/// <returns>The 2-norm as a <see cref="double"/></returns>
		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern double vecNorm(DataType type, void* a, long N, long stride);

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
		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern void vecDot(DataType type, void* a, void* b, long N, long strideA, long strideB, void* outProd);

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
		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern void vecDotc(DataType type, void* a, void* b, long N, long strideA, long strideB, void* outProd);

		/// <summary>
		/// Multiplies all the elements in vector <paramref name="a"/>
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="a"/></param>
		/// <param name="a">The vector to be multiplied of <paramref name="type"/></param>
		/// <param name="N">The number of elements to be operated</param>
		/// <param name="stride">The spacing between consecutive elements of <paramref name="a"/></param>
		/// <param name="outProd">The output product as a pointer of <paramref name="type"/></param>
		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern void vecProd(DataType type, void* a, long N, long stride, void* outProd);

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
		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern void vecParSum(DataType type, void* src, void* dst, long N, bool inclusive, long strideSrc, long strideDst);

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
		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern void vecParProd(DataType type, void* src, void* dst, long N, bool inclusive, long strideSrc, long strideDst);

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
		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
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
		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern void matMakeHerm(DataType type, void* A, long ld, long rows, bool upperStored, bool hermA);

		/// <summary>
		/// Clear (set to 0) the matrix <paramref name="A"/>'s upper part or its lower part
		/// </summary>
		/// <param name="type">The <see cref="DataType"/> of <paramref name="A"/></param>
		/// <param name="A">The matrix to be modified of <paramref name="type"/></param>
		/// <param name="ld">The leading dimension of <paramref name="A"/>, must be at least <paramref name="rows"/></param>
		/// <param name="rows">The number of rows of <paramref name="A"/></param>
		/// <param name="clearLower">Whether <paramref name="A"/>'s upper part or its lower part shall be preserved</param>
		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern void matTriClear(DataType type, void* A, long ld, long rows, bool clearLower);

		/// <summary>
		/// Fill the <paramref name="array"/> with given <paramref name="value"/> of <paramref name="type"/>
		/// </summary>
		/// <param name="type">The data type of the array and value</param>
		/// <param name="array">The array to be filled</param>
		/// <param name="value">The pointer to the value of <paramref name="type"/> to be filled</param>
		/// <param name="N">The number of elements of <paramref name="array"/>, in <paramref name="type"/></param>
		/// <param name="stride">The stride between two consecutive elements to be operated in <paramref name="array"/></param>
		/// <remarks>Strided filling reduce the performance greatly.</remarks>
		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static unsafe extern void vecFillVal(DataType type, void* array, void* value, long N, long stride);
		#endregion


		#region solver
		#region LU factorization
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_sgetrf(MklMatrixLayout matrix_layout, long m, long n, void* A, long lda, void* ipiv);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_dgetrf(MklMatrixLayout matrix_layout, long m, long n, void* A, long lda, void* ipiv);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_cgetrf(MklMatrixLayout matrix_layout, long m, long n, void* A, long lda, void* ipiv);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_zgetrf(MklMatrixLayout matrix_layout, long m, long n, void* A, long lda, void* ipiv);
		#endregion

		#region LU solve
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_sgetrs(MklMatrixLayout matrix_layout, MklOperationChar trans, long n, long nrhs, void* A, long lda, void* ipiv, void* B, long ldb);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_dgetrs(MklMatrixLayout matrix_layout, MklOperationChar trans, long n, long nrhs, void* A, long lda, void* ipiv, void* b, long ldb);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_cgetrs(MklMatrixLayout matrix_layout, MklOperationChar trans, long n, long nrhs, void* A, long lda, void* ipiv, void* b, long ldb);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_zgetrs(MklMatrixLayout matrix_layout, MklOperationChar trans, long n, long nrhs, void* A, long lda, void* ipiv, void* b, long ldb);
		#endregion

		#region direct matrix solve
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_sgesv(MklMatrixLayout matrix_layout, long n, long nrhs, void* a, long lda, void* ipiv, void* b, long ldb);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_dgesv(MklMatrixLayout matrix_layout, long n, long nrhs, void* a, long lda, void* ipiv, void* b, long ldb);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_cgesv(MklMatrixLayout matrix_layout, long n, long nrhs, void* a, long lda, void* ipiv, void* b, long ldb);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_zgesv(MklMatrixLayout matrix_layout, long n, long nrhs, void* a, long lda, void* ipiv, void* b, long ldb);
		#endregion

		#region QR factorization
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_sgeqrf(MklMatrixLayout matrix_layout, long m, long n, void* a, long lda, void* tau);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_dgeqrf(MklMatrixLayout matrix_layout, long m, long n, void* a, long lda, void* tau);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_cgeqrf(MklMatrixLayout matrix_layout, long m, long n, void* a, long lda, void* tau);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_zgeqrf(MklMatrixLayout matrix_layout, long m, long n, void* a, long lda, void* tau);
		#endregion

		#region QR generate Q
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_sorgqr(MklMatrixLayout matrix_layout, long m, long n, long k, void* a, long lda, void* tau);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_dorgqr(MklMatrixLayout matrix_layout, long m, long n, long k, void* a, long lda, void* tau);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_cungqr(MklMatrixLayout matrix_layout, long m, long n, long k, void* a, long lda, void* tau);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_zungqr(MklMatrixLayout matrix_layout, long m, long n, long k, void* a, long lda, void* tau);
		#endregion

		#region least square solve
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_sgels(MklMatrixLayout matrix_layout, MklOperationChar trans, long m, long n, long nrhs, void* a, long lda, void* b, long ldb);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_dgels(MklMatrixLayout matrix_layout, MklOperationChar trans, long m, long n, long nrhs, void* a, long lda, void* b, long ldb);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_cgels(MklMatrixLayout matrix_layout, MklOperationChar trans, long m, long n, long nrhs, void* a, long lda, void* b, long ldb);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_zgels(MklMatrixLayout matrix_layout, MklOperationChar trans, long m, long n, long nrhs, void* a, long lda, void* b, long ldb);
		#endregion

		#region symmetric/Hermitian eigen
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_ssyev(MklMatrixLayout matrix_layout, MklVectorModeChar jobz, MklFillModeChar uplo, long n, void* A, long lda, void* w);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_dsyev(MklMatrixLayout matrix_layout, MklVectorModeChar jobz, MklFillModeChar uplo, long n, void* A, long lda, void* w);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_cheev(MklMatrixLayout matrix_layout, MklVectorModeChar jobz, MklFillModeChar uplo, long n, void* A, long lda, void* w);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_zheev(MklMatrixLayout matrix_layout, MklVectorModeChar jobz, MklFillModeChar uplo, long n, void* A, long lda, void* w);
		#endregion

		#region general symmetric/hermitian definite eigen
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_ssygv(MklMatrixLayout matrix_layout, GeneralEigenType itype, MklVectorModeChar jobz, MklFillModeChar uplo, long n, void* a, long lda, void* b, long ldb, void* w);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_dsygv(MklMatrixLayout matrix_layout, GeneralEigenType itype, MklVectorModeChar jobz, MklFillModeChar uplo, long n, void* a, long lda, void* b, long ldb, void* w);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_chegv(MklMatrixLayout matrix_layout, GeneralEigenType itype, MklVectorModeChar jobz, MklFillModeChar uplo, long n, void* a, long lda, void* b, long ldb, void* w);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_zhegv(MklMatrixLayout matrix_layout, GeneralEigenType itype, MklVectorModeChar jobz, MklFillModeChar uplo, long n, void* a, long lda, void* b, long ldb, void* w);
		#endregion

		#region non-symmetric eigen
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_sgeev(MklMatrixLayout matrix_layout, MklVectorModeChar jobvl, MklVectorModeChar jobvr, long n, void* A, long lda, void* wr, void* wi, void* Vl, long ldvl, void* Vr, long ldvr);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_dgeev(MklMatrixLayout matrix_layout, MklVectorModeChar jobvl, MklVectorModeChar jobvr, long n, void* A, long lda, void* wr, void* wi, void* Vl, long ldvl, void* Vr, long ldvr);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_cgeev(MklMatrixLayout matrix_layout, MklVectorModeChar jobvl, MklVectorModeChar jobvr, long n, void* A, long lda, void* w, void* Vl, long ldvl, void* Vr, long ldvr);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_zgeev(MklMatrixLayout matrix_layout, MklVectorModeChar jobvl, MklVectorModeChar jobvr, long n, void* A, long lda, void* w, void* Vl, long ldvl, void* Vr, long ldvr);
		#endregion

		#region non-symmetric general eigen
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_sggev(MklMatrixLayout matrix_layout, MklVectorModeChar jobvl, MklVectorModeChar jobvr, long n, void* a, long lda, void* b, long ldb, void* alphar, void* alphai, void* beta, void* vl, long ldvl, void* vr, long ldvr);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_dggev(MklMatrixLayout matrix_layout, MklVectorModeChar jobvl, MklVectorModeChar jobvr, long n, void* a, long lda, void* b, long ldb, void* alphar, void* alphai, void* beta, void* vl, long ldvl, void* vr, long ldvr);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_cggev(MklMatrixLayout matrix_layout, MklVectorModeChar jobvl, MklVectorModeChar jobvr, long n, void* a, long lda, void* b, long ldb, void* alpha, void* beta, void* vl, long ldvl, void* vr, long ldvr);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_zggev(MklMatrixLayout matrix_layout, MklVectorModeChar jobvl, MklVectorModeChar jobvr, long n, void* a, long lda, void* b, long ldb, void* alpha, void* beta, void* vl, long ldvl, void* vr, long ldvr);
		#endregion

		#region SVD
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_sgesvd(MklMatrixLayout matrix_layout, MklSvdModeChar jobu, MklSvdModeChar jobvt, long m, long n, void* A, long lda, void* S, void* U, long ldu, void* Vt, long ldvt, byte[] superb);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_dgesvd(MklMatrixLayout matrix_layout, MklSvdModeChar jobu, MklSvdModeChar jobvt, long m, long n, void* A, long lda, void* S, void* U, long ldu, void* Vt, long ldvt, byte[] superb);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_cgesvd(MklMatrixLayout matrix_layout, MklSvdModeChar jobu, MklSvdModeChar jobvt, long m, long n, void* A, long lda, void* S, void* U, long ldu, void* Vt, long ldvt, byte[] superb);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_zgesvd(MklMatrixLayout matrix_layout, MklSvdModeChar jobu, MklSvdModeChar jobvt, long m, long n, void* A, long lda, void* S, void* U, long ldu, void* Vt, long ldvt, byte[] superb);
		#endregion

		#region Schur
		internal delegate long SchurSelect1(void* v);
		internal delegate long SchurSelect2(void* v1, void* v2);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_sgees(MklMatrixLayout matrix_layout, MklVectorModeChar jobv, MklSortModeChar sort, [MarshalAs(UnmanagedType.FunctionPtr)] SchurSelect2? selectFunc, long n, void* A, long lda, out long selected, void* wr, void* wi, void* V, long ldv);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_dgees(MklMatrixLayout matrix_layout, MklVectorModeChar jobv, MklSortModeChar sort, [MarshalAs(UnmanagedType.FunctionPtr)] SchurSelect2? selectFunc, long n, void* A, long lda, out long selected, void* wr, void* wi, void* V, long ldv);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_cgees(MklMatrixLayout matrix_layout, MklVectorModeChar jobv, MklSortModeChar sort, [MarshalAs(UnmanagedType.FunctionPtr)] SchurSelect1? selectFunc, long n, void* A, long lda, out long selected, void* w, void* V, long ldv);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklLapackInfo LAPACKE_zgees(MklMatrixLayout matrix_layout, MklVectorModeChar jobv, MklSortModeChar sort, [MarshalAs(UnmanagedType.FunctionPtr)] SchurSelect1? selectFunc, long n, void* A, long lda, out long selected, void* w, void* V, long ldv);
		#endregion
		#endregion
	}
}
