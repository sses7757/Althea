using System;
using System.Runtime.InteropServices;


namespace Althea.Blas.Cuda
{
	/// <summary>
	/// CUDA BLAS library API
	/// </summary>
	public static class NativeMethods
	{
		// 32bit is not supported any more, only 64 bit
		/// <summary>
		/// The CUDA BLAS library name
		/// </summary>
		public const string CUBLAS_API_DLL_NAME = @"cublas";


		#region utilities
		/// <summary>
		/// This function initializes the CUDA BLAS library and creates a handle to an opaque structure holding the CUDA BLAS library context.
		/// </summary>
		/// <param name="handle">returned CUDA BLAS handle</param>
		/// <returns>operation status enum <see cref="Status"/></returns>
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasCreate_v2(ref IntPtr handle);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasCreate(ref IntPtr handle);

		/// <summary>
		/// This function releases hardware resources used by the CUBLAS library. This function is usually the last call with a particular handle to the CUBLAS library.
		/// </summary>
		/// <param name="handle">input CUDA BLAS handle</param>
		/// <returns>operation status enum <see cref="Status"/></returns>
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasDestroy_v2(IntPtr handle);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasDestroy(IntPtr handle);

		/// <summary>
		/// Some routines like <c>cublas&lt;t&gt;symv</c> and <c>cublas&lt;t&gt;hemv</c> have an alternate implementation that use atomics to cumulate results. This implementation is generally significantly faster but can generate results that are not strictly identical from one run to the others. Mathematically, those different results are not significant but when debugging
		/// those differences can be prejudicial. <para/>
		/// This function queries the atomic mode of a specific cuBLAS context.
		/// </summary>
		/// <param name="handle">input CUDA BLAS handle</param>
		/// <param name="mode">returned <see cref="AtomicsMode"/></param>
		/// <returns>operation status enum <see cref="Status"/></returns>
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasGetAtomicsMode(IntPtr handle, ref AtomicsMode mode);

		/// <summary>
		/// Some routines like <c>cublas&lt;t&gt;symv</c> have an alternate implementation that use atomics to cumulate results. This implementation is generally significantly faster but can generate results that are not strictly identical from one run to the others. Mathematically, those different results are not significant but when debugging those differences can be prejudicial.
		/// <para/>This function allows or disallows the usage of atomics in the CUDA BLAS library for all routines which have an alternate implementation.
		/// </summary>
		/// <param name="handle">input CUDA BLAS handle</param>
		/// <param name="mode">the <see cref="AtomicsMode"/> to set</param>
		/// <returns>operation status enum <see cref="Status"/></returns>
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasSetAtomicsMode(IntPtr handle, AtomicsMode mode);

		/// <summary>
		/// This function obtains the pointer mode used by the cuBLAS library.
		/// </summary>
		/// <param name="handle">input CUDA BLAS handle</param>
		/// <param name="mode">returned <see cref="PointerMode"/></param>
		/// <returns>operation status enum <see cref="Status"/></returns>
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasGetPointerMode_v2(IntPtr handle, ref PointerMode mode);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasGetPointerMode(IntPtr handle, ref PointerMode mode);

		/// <summary>
		/// This function sets the pointer mode used by the cuBLAS library.
		/// The default is for the values to be passed by reference on the host.
		/// </summary>
		/// <param name="handle">input CUDA BLAS handle</param>
		/// <param name="mode">the <see cref="PointerMode"/> to set</param>
		/// <returns>operation status enum <see cref="Status"/></returns>
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasSetPointerMode_v2(IntPtr handle, PointerMode mode);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasSetPointerMode(IntPtr handle, PointerMode mode);
		#endregion


		#region level 1
		/// <summary>
		/// This function finds the (smallest) index of the element of the maximum magnitude.
		/// </summary>
		/// <param name="handle">handle to the CUDA BLAS library context</param>
		/// <param name="n">number of elements in the vector <paramref name="x"/></param>
		/// <param name="x">vector with elements</param>
		/// <param name="incx">stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="result">the resulting index, which is 0 if <c><paramref name="n"/>,<paramref name="incx"/> &lt;= 0</c></param>
		/// <returns>operation status enum <see cref="Status"/></returns>
		internal delegate Status amaxFunc(IntPtr handle, int n, IntPtr x, int incx, ref int result);
		#region abs max
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasIsamax_v2(IntPtr handle, int n, IntPtr x, int incx, ref int result);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasIsamax(IntPtr handle, int n, IntPtr x, int incx, ref int result);


		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasIdamax_v2(IntPtr handle, int n, IntPtr x, int incx, ref int result);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasIdamax(IntPtr handle, int n, IntPtr x, int incx, ref int result);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasIcamax_v2(IntPtr handle, int n, IntPtr x, int incx, ref int result);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasIcamax(IntPtr handle, int n, IntPtr x, int incx, ref int result);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasIzamax_v2(IntPtr handle, int n, IntPtr x, int incx, ref int result);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasIzamax(IntPtr handle, int n, IntPtr x, int incx, ref int result);
		#endregion


		/// <summary>
		/// This function finds the (smallest) index of the element of the minimum magnitude.
		/// </summary>
		/// <param name="handle">handle to the CUDA BLAS library context</param>
		/// <param name="n">number of elements in the vector <paramref name="x"/></param>
		/// <param name="x">vector with elements</param>
		/// <param name="incx">stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="result">the resulting index, which is 0 if <c><paramref name="n"/>,<paramref name="incx"/> &lt;= 0</c></param>
		/// <returns>operation status enum <see cref="Status"/></returns>
		internal delegate Status aminFunc(IntPtr handle, int n, IntPtr x, int incx, ref int result);
		#region abs min
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasIsamin_v2(IntPtr handle, int n, IntPtr x, int incx, ref int result);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasIsamin(IntPtr handle, int n, IntPtr x, int incx, ref int result);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasIdamin_v2(IntPtr handle, int n, IntPtr x, int incx, ref int result);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasIdamin(IntPtr handle, int n, IntPtr x, int incx, ref int result);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasIcamin_v2(IntPtr handle, int n, IntPtr x, int incx, ref int result);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasIcamin(IntPtr handle, int n, IntPtr x, int incx, ref int result);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasIzamin_v2(IntPtr handle, int n, IntPtr x, int incx, ref int result);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasIzamin(IntPtr handle, int n, IntPtr x, int incx, ref int result);
		#endregion

		/// <summary>
		/// This function computes the sum of the absolute values of the elements of vector <paramref name="x"/>.
		/// </summary>
		/// <param name="handle">handle to the CUDA BLAS library context</param>
		/// <param name="n">number of elements in the vector <paramref name="x"/></param>
		/// <param name="x">vector with elements</param>
		/// <param name="incx">stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="result">the resulting index, which is 0 if <c><paramref name="n"/>,<paramref name="incx"/> &lt;= 0</c></param>
		/// <returns>operation status enum <see cref="Status"/></returns>
		internal delegate Status asumFunc<T>(IntPtr handle, int n, IntPtr x, int incx, ref T result);
		#region abs sum
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasSasum_v2(IntPtr handle, int n, IntPtr x, int incx, ref float result);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasSasum(IntPtr handle, int n, IntPtr x, int incx, ref float result);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasDasum_v2(IntPtr handle, int n, IntPtr x, int incx, ref double result);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasDasum(IntPtr handle, int n, IntPtr x, int incx, ref double result);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasScasum_v2(IntPtr handle, int n, IntPtr x, int incx, ref float result);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasScasum(IntPtr handle, int n, IntPtr x, int incx, ref float result);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasDzasum_v2(IntPtr handle, int n, IntPtr x, int incx, ref double result);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasDzasum(IntPtr handle, int n, IntPtr x, int incx, ref double result);
		#endregion

		/// <summary>
		/// This function multiplies the vector <paramref name="x"/> by the scalar <paramref name="α"/> and adds it to the vector <paramref name="y"/> overwriting <paramref name="y"/>.
		/// </summary>
		/// <param name="handle">handle to the CUDA BLAS library context</param>
		/// <param name="n">number of elements in the vector <paramref name="x"/> and <paramref name="y"/></param>
		/// <param name="α">(host or device) scalar used for multiplication</param>
		/// <param name="x">vector with elements</param>
		/// <param name="incx">stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="y">(in and out) another vector with elements</param>
		/// <param name="incy">stride between consecutive elements of <paramref name="y"/></param>
		/// <returns>operation status enum <see cref="Status"/></returns>
		internal delegate Status axpyFunc<T>(IntPtr handle, int n, ref T α, IntPtr x, int incx, IntPtr y, int incy);
		#region vector add
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasSaxpy_v2(IntPtr handle, int n, ref float α, IntPtr x, int incx, IntPtr y, int incy);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasSaxpy(IntPtr handle, int n, ref float α, IntPtr x, int incx, IntPtr y, int incy);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasDaxpy_v2(IntPtr handle, int n, ref double α, IntPtr x, int incx, IntPtr y, int incy);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasDaxpy(IntPtr handle, int n, ref double α, IntPtr x, int incx, IntPtr y, int incy);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasCaxpy_v2(IntPtr handle, int n, ref FloatComplex α, IntPtr x, int incx, IntPtr y, int incy);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasCaxpy(IntPtr handle, int n, ref FloatComplex α, IntPtr x, int incx, IntPtr y, int incy);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasZaxpy_v2(IntPtr handle, int n, ref DoubleComplex α, IntPtr x, int incx, IntPtr y, int incy);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasZaxpy(IntPtr handle, int n, ref DoubleComplex α, IntPtr x, int incx, IntPtr y, int incy);
		#endregion

		/// <summary>
		/// This function computes the dot product of vectors <paramref name="x"/> and <paramref name="y"/>. For complex, the one with 'c' implies the conjugate dot.
		/// </summary>
		/// <param name="handle">handle to the CUDA BLAS library context</param>
		/// <param name="n">number of elements in the vector <paramref name="x"/> and <paramref name="y"/></param>
		/// <param name="x">vector with elements</param>
		/// <param name="incx">stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="y">(in and out) another vector with elements</param>
		/// <param name="incy">stride between consecutive elements of <paramref name="y"/></param>
		/// <param name="result">the resulting dot product, which is 0.0 if <c>n &lt;= 0</c></param>
		/// <returns>operation status enum <see cref="Status"/></returns>
		internal delegate Status dotFunc<T>(IntPtr handle, int n, IntPtr x, int incx, IntPtr y, int incy, ref T result);
		#region vector dot
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasSdot_v2(IntPtr handle, int n, IntPtr x, int incx, IntPtr y, int incy, ref float result);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasSdot(IntPtr handle, int n, IntPtr x, int incx, IntPtr y, int incy, ref float result);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasDdot_v2(IntPtr handle, int n, IntPtr x, int incx, IntPtr y, int incy, ref double result);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasDdot(IntPtr handle, int n, IntPtr x, int incx, IntPtr y, int incy, ref double result);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasCdotc_v2(IntPtr handle, int n, IntPtr x, int incx, IntPtr y, int incy, ref FloatComplex result);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasCdotc(IntPtr handle, int n, IntPtr x, int incx, IntPtr y, int incy, ref FloatComplex result);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasCdotu_v2(IntPtr handle, int n, IntPtr x, int incx, IntPtr y, int incy, ref FloatComplex result);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasCdotu(IntPtr handle, int n, IntPtr x, int incx, IntPtr y, int incy, ref FloatComplex result);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasZdotc_v2(IntPtr handle, int n, IntPtr x, int incx, IntPtr y, int incy, ref DoubleComplex result);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasZdotc(IntPtr handle, int n, IntPtr x, int incx, IntPtr y, int incy, ref DoubleComplex result);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasZdotu_v2(IntPtr handle, int n, IntPtr x, int incx, IntPtr y, int incy, ref DoubleComplex result);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasZdotu(IntPtr handle, int n, IntPtr x, int incx, IntPtr y, int incy, ref DoubleComplex result);
		#endregion

		/// <summary>
		/// This function computes the Euclidean norm of the vector <paramref name="x"/>.
		/// </summary>
		/// <param name="handle">handle to the CUDA BLAS library context</param>
		/// <param name="n">number of elements in the vector <paramref name="x"/></param>
		/// <param name="x">vector with elements</param>
		/// <param name="incx">stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="result">the result, which is 0 if <c><paramref name="n"/>,<paramref name="incx"/> &lt;= 0</c></param>
		/// <returns>operation status enum <see cref="Status"/></returns>
		internal delegate Status normFunc<T>(IntPtr handle, int n, IntPtr x, int incx, ref T result);
		#region vector norm
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasSnrm2_v2(IntPtr handle, int n, IntPtr x, int incx, ref float result);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasSnrm2(IntPtr handle, int n, IntPtr x, int incx, ref float result);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasDnrm2_v2(IntPtr handle, int n, IntPtr x, int incx, ref double result);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasDnrm2(IntPtr handle, int n, IntPtr x, int incx, ref double result);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasScnrm2_v2(IntPtr handle, int n, IntPtr x, int incx, ref float result);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasScnrm2(IntPtr handle, int n, IntPtr x, int incx, ref float result);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasDznrm2_v2(IntPtr handle, int n, IntPtr x, int incx, ref double result);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasDznrm2(IntPtr handle, int n, IntPtr x, int incx, ref double result);
		#endregion

		/// <summary>
		/// This function scales the vector <paramref name="x"/> by the scalar <paramref name="α"/> and overwrites it with the result.
		/// </summary>
		/// <param name="handle">handle to the CUDA BLAS library context</param>
		/// <param name="n">number of elements in the vector <paramref name="x"/></param>
		/// <param name="α">scalar used for multiplication</param>
		/// <param name="x">vector with <paramref name="n"/> elements</param>
		/// <param name="incx">stride between consecutive elements of <paramref name="x"/></param>
		/// <returns>operation status enum <see cref="Status"/></returns>
		internal delegate Status scalFunc<T>(IntPtr handle, int n, ref T α, IntPtr x, int incx);
		#region scale
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasSscal_v2(IntPtr handle, int n, ref float α, IntPtr x, int incx);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasSscal(IntPtr handle, int n, ref float α, IntPtr x, int incx);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasDscal_v2(IntPtr handle, int n, ref double α, IntPtr x, int incx);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasDscal(IntPtr handle, int n, ref double α, IntPtr x, int incx);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasCscal_v2(IntPtr handle, int n, ref FloatComplex α, IntPtr x, int incx);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasCscal(IntPtr handle, int n, ref FloatComplex α, IntPtr x, int incx);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasZscal_v2(IntPtr handle, int n, ref DoubleComplex α, IntPtr x, int incx);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasZscal(IntPtr handle, int n, ref DoubleComplex α, IntPtr x, int incx);
		#endregion

		/// <summary>
		/// This function copies the vector <paramref name="x"/> into the vector <paramref name="y"/>. Hence, the performed operation is <c>y[j] = x[k] for i = 1,…,n; k = 1 + (i - 1)*incx and j = 1 + (i - 1)*incy</c>.
		/// </summary>
		/// <param name="handle">handle to the CUDA BLAS library context</param>
		/// <param name="n">number of elements in the vector <paramref name="x"/> and <paramref name="y"/></param>
		/// <param name="x">vector with <paramref name="n"/> elements</param>
		/// <param name="incx">stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="y">vector with <paramref name="n"/> elements</param>
		/// <param name="incy">stride between consecutive elements of <paramref name="y"/></param>
		/// <returns>operation status enum <see cref="Status"/></returns>
		internal delegate Status copyFunc(IntPtr handle, int n, [In] IntPtr x, int incx, IntPtr y, int incy);
		#region copy
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasScopy_v2(IntPtr handle, int n, [In] IntPtr x, int incx, IntPtr y, int incy);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasScopy(IntPtr handle, int n, [In] IntPtr x, int incx, IntPtr y, int incy);

		/// <returns></returns>
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasDcopy_v2(IntPtr handle, int n, [In] IntPtr x, int incx, IntPtr y, int incy);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasDcopy(IntPtr handle, int n, [In] IntPtr x, int incx, IntPtr y, int incy);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasCcopy_v2(IntPtr handle, int n, [In] IntPtr x, int incx, IntPtr y, int incy);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasCcopy(IntPtr handle, int n, [In] IntPtr x, int incx, IntPtr y, int incy);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasZcopy_v2(IntPtr handle, int n, [In] IntPtr x, int incx, IntPtr y, int incy);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasZcopy(IntPtr handle, int n, [In] IntPtr x, int incx, IntPtr y, int incy);
		#endregion

		#endregion


		#region level 2
		/// <summary>
		/// This function performs the matrix-vector multiplication <paramref name="y"/> = <paramref name="α"/> <paramref name="op"/>(<paramref name="A"/>) <paramref name="x"/> + <paramref name="β"/> <paramref name="y"/> where <paramref name="A"/> is a <paramref name="m"/>×<paramref name="n"/> matrix stored in column-major format, <paramref name="x"/> and <paramref name="y"/> are vectors, and <paramref name="α"/> and <paramref name="β"/> are scalars.<br/>
		/// Also, for matrix <paramref name="A"/>: <paramref name="op"/>(<paramref name="A"/>) = <paramref name="A"/> if <paramref name="op"/> == <see cref="MatrixOperation.None"/>; <paramref name="A"/>^T if <paramref name="op"/> == <see cref="MatrixOperation.Transpose"/>; <paramref name="A"/>^H if <paramref name="op"/> == <see cref="MatrixOperation.ConjugateTranspose"/>.
		/// </summary>
		/// <param name="handle">handle to the CUDA BLAS library context</param>
		/// <param name="op"><see cref="MatrixOperation"/> that is non- or (conj.) transpose</param>
		/// <param name="m">number of rows of matrix <paramref name="A"/></param>
		/// <param name="n">number of columns of matrix <paramref name="A"/></param>
		/// <param name="α">scalar used for multiplication</param>
		/// <param name="A">array of dimension <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="m"/>)</c>. Unchanged on exit.</param>
		/// <param name="lda">leading dimension of two-dimensional array used to store matrix <paramref name="A"/></param>
		/// <param name="x">vector at least <c>(1+(<paramref name="n"/>-1)*abs(<paramref name="incx"/>))</c> elements if <paramref name="op"/>==<see cref="MatrixOperation.None"/> or <c>(1+(<paramref name="m"/>-1)*abs(<paramref name="incx"/>))</c> otherwise</param>
		/// <param name="incx">stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="β">scalar used for multiplication, if <c>b<paramref name="β"/> == 0</c> then <paramref name="y"/> does not have to be a valid input</param>
		/// <param name="y">(in and out) vector at least <c>(1+(<paramref name="m"/>-1)*abs(<paramref name="incy"/>))</c> elements if <paramref name="op"/>==<see cref="MatrixOperation.None"/> or <c>(1+(<paramref name="n"/>-1)*abs(<paramref name="incy"/>))</c> otherwise</param>
		/// <param name="incy">stride between consecutive elements of <paramref name="y"/></param>
		/// <returns>operation status enum <see cref="Status"/></returns>
		internal delegate Status gemvFunc<T>(IntPtr handle, MatrixOperation op, int m, int n, ref T α, [In] IntPtr A, int lda, [In] IntPtr x, int incx, ref T β, IntPtr y, int incy);
		#region general matrix vector multiply
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasSgemv_v2(IntPtr handle, MatrixOperation op, int m, int n, ref float α, [In] IntPtr A, int lda, [In] IntPtr x, int incx, ref float β, IntPtr y, int incy);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasSgemv(IntPtr handle, MatrixOperation op, int m, int n, ref float α, [In] IntPtr A, int lda, [In] IntPtr x, int incx, ref float β, IntPtr y, int incy);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasDgemv_v2(IntPtr handle, MatrixOperation op, int m, int n, ref double α, [In] IntPtr A, int lda, [In] IntPtr x, int incx, ref double β, IntPtr y, int incy);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasDgemv(IntPtr handle, MatrixOperation op, int m, int n, ref double α, [In] IntPtr A, int lda, [In] IntPtr x, int incx, ref double β, IntPtr y, int incy);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasCgemv_v2(IntPtr handle, MatrixOperation op, int m, int n, ref FloatComplex α, [In] IntPtr A, int lda, [In] IntPtr x, int incx, ref FloatComplex β, IntPtr y, int incy);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasCgemv(IntPtr handle, MatrixOperation op, int m, int n, ref FloatComplex α, [In] IntPtr A, int lda, [In] IntPtr x, int incx, ref FloatComplex β, IntPtr y, int incy);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasZgemv_v2(IntPtr handle, MatrixOperation op, int m, int n, ref DoubleComplex α, [In] IntPtr A, int lda, [In] IntPtr x, int incx, ref DoubleComplex β, IntPtr y, int incy);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasZgemv(IntPtr handle, MatrixOperation op, int m, int n, ref DoubleComplex α, [In] IntPtr A, int lda, [In] IntPtr x, int incx, ref DoubleComplex β, IntPtr y, int incy);
		#endregion

		/// <summary>
		/// This function performs the symmetric/Hermitian matrix-vector multiplication <paramref name="y"/> = <paramref name="α"/> <paramref name="A"/> <paramref name="x"/> + <paramref name="β"/> <paramref name="y"/> where <paramref name="A"/> is a <paramref name="n"/>×<paramref name="n"/> symmetric/Hermitian matrix stored in column-major format, <paramref name="x"/> is a vector, and <paramref name="α"/> is a scalar.
		/// </summary>
		/// <param name="handle">handle to the CUDA BLAS library context</param>
		/// <param name="uplo">indicates if matrix lower or upper part is stored</param>
		/// <param name="n">number of rows and columns of matrix <paramref name="A"/></param>
		/// <param name="α">scalar used for multiplication</param>
		/// <param name="A">array of dimension <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="n"/>)</c></param>
		/// <param name="lda">leading dimension of two-dimensional array used to store matrix <paramref name="A"/></param>
		/// <param name="x">vector with <c>(1+(<paramref name="n"/>-1)*abs(<paramref name="incx"/>))</c> elements</param>
		/// <param name="incx">stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="β">scalar used for multiplication, if <c>b<paramref name="β"/> == 0</c> then <paramref name="y"/> does not have to be a valid input</param>
		/// <param name="y">(in and out) vector at least <c>(1+(<paramref name="n"/>-1)*abs(<paramref name="incy"/>))</c></param>
		/// <param name="incy">stride between consecutive elements of <paramref name="y"/></param>
		/// <returns>operation status enum <see cref="Status"/></returns>
		internal delegate Status symvFunc<T>(IntPtr handle, MatrixFillMode uplo, int n, ref T α, [In] IntPtr A, int lda, [In] IntPtr x, int incx, ref T β, IntPtr y, int incy);
		#region symmetric matrix vector multiply
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasSsymv_v2(IntPtr handle, MatrixFillMode uplo, int n, ref float α, [In] IntPtr A, int lda, [In] IntPtr x, int incx, ref float β, IntPtr y, int incy);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasSsymv(IntPtr handle, MatrixFillMode uplo, int n, ref float α, [In] IntPtr A, int lda, [In] IntPtr x, int incx, ref float β, IntPtr y, int incy);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasDsymv_v2(IntPtr handle, MatrixFillMode uplo, int n, ref double α, [In] IntPtr A, int lda, [In] IntPtr x, int incx, ref double β, IntPtr y, int incy);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasDsymv(IntPtr handle, MatrixFillMode uplo, int n, ref double α, [In] IntPtr A, int lda, [In] IntPtr x, int incx, ref double β, IntPtr y, int incy);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasCsymv_v2(IntPtr handle, MatrixFillMode uplo, int n, ref FloatComplex α, [In] IntPtr A, int lda, [In] IntPtr x, int incx, ref FloatComplex β, IntPtr y, int incy);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasCsymv(IntPtr handle, MatrixFillMode uplo, int n, ref FloatComplex α, [In] IntPtr A, int lda, [In] IntPtr x, int incx, ref FloatComplex β, IntPtr y, int incy);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasZsymv_v2(IntPtr handle, MatrixFillMode uplo, int n, ref DoubleComplex α, [In] IntPtr A, int lda, [In] IntPtr x, int incx, ref DoubleComplex β, IntPtr y, int incy);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasZsymv(IntPtr handle, MatrixFillMode uplo, int n, ref DoubleComplex α, [In] IntPtr A, int lda, [In] IntPtr x, int incx, ref DoubleComplex β, IntPtr y, int incy);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasChemv_v2(IntPtr handle, MatrixFillMode uplo, int n, ref FloatComplex α, [In] IntPtr A, int lda, [In] IntPtr x, int incx, ref FloatComplex β, IntPtr y, int incy);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasChemv(IntPtr handle, MatrixFillMode uplo, int n, ref FloatComplex α, [In] IntPtr A, int lda, [In] IntPtr x, int incx, ref FloatComplex β, IntPtr y, int incy);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasZhemv_v2(IntPtr handle, MatrixFillMode uplo, int n, ref DoubleComplex α, [In] IntPtr A, int lda, [In] IntPtr x, int incx, ref DoubleComplex β, IntPtr y, int incy);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasZhemv(IntPtr handle, MatrixFillMode uplo, int n, ref DoubleComplex α, [In] IntPtr A, int lda, [In] IntPtr x, int incx, ref DoubleComplex β, IntPtr y, int incy);
		#endregion

		/// <summary>
		/// This function performs the rank-1 update <paramref name="A"/> = <paramref name="α"/> <paramref name="x"/> (<paramref name="y"/>^T) + <paramref name="A"/> if <c>ger()</c>/<c>geru()</c> is called; <paramref name="A"/> = <paramref name="α"/> <paramref name="x"/> (<paramref name="y"/>^H) + <paramref name="A"/> if <c>gerc()</c> is called; where <paramref name="A"/> is a <paramref name="m"/>×<paramref name="n"/> matrix stored in column-major format, <paramref name="x"/> and <paramref name="y"/> are vectors, and <paramref name="α"/> is a scalar.
		/// </summary>
		/// <param name="handle">handle to the CUDA BLAS library context</param>
		/// <param name="m">number of rows of matrix <paramref name="A"/></param>
		/// <param name="n">number of columns of matrix <paramref name="A"/></param>
		/// <param name="α">scalar used for multiplication</param>
		/// <param name="x">vector with <c>(1+(<paramref name="m"/>-1)*abs(<paramref name="incx"/>))</c> elements</param>
		/// <param name="incx">stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="y">vector with <c>(1+(<paramref name="n"/>-1)*abs(<paramref name="incy"/>))</c> elements</param>
		/// <param name="incy">stride between consecutive elements of <paramref name="y"/></param>
		/// <param name="A">array of dimension <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="m"/>)</c></param>
		/// <param name="lda">leading dimension of two-dimensional array used to store matrix <paramref name="A"/></param>
		/// <returns>operation status enum <see cref="Status"/></returns>
		internal delegate Status gerFunc<T>(IntPtr handle, int m, int n, ref T α, [In] IntPtr x, int incx, [In] IntPtr y, int incy, IntPtr A, int lda);
		#region general rank 1 update
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasSger_v2(IntPtr handle, int m, int n, ref float α, [In] IntPtr x, int incx, [In] IntPtr y, int incy, IntPtr A, int lda);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasSger(IntPtr handle, int m, int n, ref float α, [In] IntPtr x, int incx, [In] IntPtr y, int incy, IntPtr A, int lda);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasDger_v2(IntPtr handle, int m, int n, ref double α, [In] IntPtr x, int incx, [In] IntPtr y, int incy, IntPtr A, int lda);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasDger(IntPtr handle, int m, int n, ref double α, [In] IntPtr x, int incx, [In] IntPtr y, int incy, IntPtr A, int lda);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasCgeru_v2(IntPtr handle, int m, int n, ref FloatComplex α, [In] IntPtr x, int incx, [In] IntPtr y, int incy, IntPtr A, int lda);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasCgeru(IntPtr handle, int m, int n, ref FloatComplex α, [In] IntPtr x, int incx, [In] IntPtr y, int incy, IntPtr A, int lda);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasCgerc_v2(IntPtr handle, int m, int n, ref FloatComplex α, [In] IntPtr x, int incx, [In] IntPtr y, int incy, IntPtr A, int lda);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasCgerc(IntPtr handle, int m, int n, ref FloatComplex α, [In] IntPtr x, int incx, [In] IntPtr y, int incy, IntPtr A, int lda);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasZgeru_v2(IntPtr handle, int m, int n, ref DoubleComplex α, [In] IntPtr x, int incx, [In] IntPtr y, int incy, IntPtr A, int lda);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasZgeru(IntPtr handle, int m, int n, ref DoubleComplex α, [In] IntPtr x, int incx, [In] IntPtr y, int incy, IntPtr A, int lda);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasZgerc_v2(IntPtr handle, int m, int n, ref DoubleComplex α, [In] IntPtr x, int incx, [In] IntPtr y, int incy, IntPtr A, int lda);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasZgerc(IntPtr handle, int m, int n, ref DoubleComplex α, [In] IntPtr x, int incx, [In] IntPtr y, int incy, IntPtr A, int lda);
		#endregion

		/// <summary>
		/// This function performs the symmetric/Hermitian rank-1 update  <paramref name="A"/> = <paramref name="α"/> <paramref name="x"/> (<paramref name="x"/>^T) + <paramref name="A"/> ; where <paramref name="A"/> is a <paramref name="n"/>×<paramref name="n"/> matrix stored in column-major format, <paramref name="x"/> is a vector, and <paramref name="α"/> is a scalar.
		/// </summary>
		/// <param name="handle">handle to the CUDA BLAS library context</param>
		/// <param name="fillMode"><see cref="MatrixFillMode"/> of result matrix</param>
		/// <param name="n">number of rows and columns of matrix <paramref name="A"/></param>
		/// <param name="α">scalar used for multiplication</param>
		/// <param name="x">vector with <c>(1+(<paramref name="n"/>-1)*abs(<paramref name="incx"/>))</c> elements</param>
		/// <param name="incx">stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="A">array of dimension <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="n"/>)</c></param>
		/// <param name="lda">leading dimension of two-dimensional array used to store matrix <paramref name="A"/></param>
		/// <returns>operation status enum <see cref="Status"/></returns>
		internal delegate Status syrFunc<T>(IntPtr handle, MatrixFillMode fillMode, int n, ref T α, [In] IntPtr x, int incx, IntPtr A, int lda);
		#region symmetric rank 1 update
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasSsyr_v2(IntPtr handle, MatrixFillMode fillMode, int n, ref float α, [In] IntPtr x, int incx, IntPtr A, int lda);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasSsyr(IntPtr handle, MatrixFillMode fillMode, int n, ref float α, [In] IntPtr x, int incx, IntPtr A, int lda);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasDsyr_v2(IntPtr handle, MatrixFillMode fillMode, int n, ref double α, [In] IntPtr x, int incx, IntPtr A, int lda);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasDsyr(IntPtr handle, MatrixFillMode fillMode, int n, ref double α, [In] IntPtr x, int incx, IntPtr A, int lda);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasCsyr_v2(IntPtr handle, MatrixFillMode fillMode, int n, ref FloatComplex α, [In] IntPtr x, int incx, IntPtr A, int lda);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasCsyr(IntPtr handle, MatrixFillMode fillMode, int n, ref FloatComplex α, [In] IntPtr x, int incx, IntPtr A, int lda);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasZsyr_v2(IntPtr handle, MatrixFillMode fillMode, int n, ref DoubleComplex α, [In] IntPtr x, int incx, IntPtr A, int lda);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasZsyr(IntPtr handle, MatrixFillMode fillMode, int n, ref DoubleComplex α, [In] IntPtr x, int incx, IntPtr A, int lda);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasCher_v2(IntPtr handle, MatrixFillMode fillMode, int n, ref FloatComplex α, [In] IntPtr x, int incx, IntPtr A, int lda);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasCher(IntPtr handle, MatrixFillMode fillMode, int n, ref FloatComplex α, [In] IntPtr x, int incx, IntPtr A, int lda);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasZher_v2(IntPtr handle, MatrixFillMode fillMode, int n, ref DoubleComplex α, [In] IntPtr x, int incx, IntPtr A, int lda);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasZher(IntPtr handle, MatrixFillMode fillMode, int n, ref DoubleComplex α, [In] IntPtr x, int incx, IntPtr A, int lda);
		#endregion

		#endregion


		#region level 3
		/// <summary>
		/// This function performs the matrix-matrix multiplication <paramref name="C"/> = <paramref name="α"/> <paramref name="opA"/>(<paramref name="A"/>) <paramref name="opB"/>(<paramref name="B"/>) + <paramref name="β"/> <paramref name="C"/> where <paramref name="α"/> and <paramref name="β"/> are scalars; <paramref name="A"/> , <paramref name="B"/> and <paramref name="C"/> are matrices stored in column-major format with dimensions <paramref name="opA"/>(<paramref name="A"/>) -- <paramref name="m"/>×<paramref name="k"/>, <paramref name="opB"/>(<paramref name="B"/>) -- <paramref name="k"/>×<paramref name="n"/> and <paramref name="C"/> -- <paramref name="m"/>×<paramref name="n"/>, respectively. <br/>
		/// Also, for matrix <paramref name="A"/>,
		/// <paramref name="opA"/>(<paramref name="A"/>) = <paramref name="A"/> if <paramref name="opA"/> == <see cref="MatrixOperation.None"/>;
		/// <paramref name="opA"/>(<paramref name="A"/>) = <paramref name="A"/>^T if <paramref name="opA"/> == <see cref="MatrixOperation.Transpose"/>;
		/// <paramref name="opA"/>(<paramref name="A"/>) = <paramref name="A"/>^H if <paramref name="opA"/> == <see cref="MatrixOperation.ConjugateTranspose"/>.
		/// The same for <paramref name="B"/>.
		/// </summary>
		/// <param name="handle">handle to the CUDA BLAS library context</param>
		/// <param name="opA"><see cref="MatrixOperation"/> that is non- or (conj.) transpose</param>
		/// <param name="opB"><see cref="MatrixOperation"/> that is non- or (conj.) transpose</param>
		/// <param name="m">number of rows of matrix <paramref name="opA"/>(<paramref name="A"/>) and <paramref name="C"/></param>
		/// <param name="n">number of columns of matrix <paramref name="opB"/>(<paramref name="B"/>) and <paramref name="C"/></param>
		/// <param name="k">number of columns of <paramref name="opA"/>(<paramref name="A"/>) and rows of <paramref name="opB"/>(<paramref name="B"/>)</param>
		/// <param name="α">scalar used for multiplication</param>
		/// <param name="A">array of dimensions <c><paramref name="lda"/>×<paramref name="k"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="m"/>)</c> if <paramref name="opA"/> == <see cref="MatrixOperation.None"/>, and <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="k"/>)</c> otherwise</param>
		/// <param name="lda">leading dimension of two-dimensional array used to store the matrix <paramref name="A"/></param>
		/// <param name="B">array of dimension <c><paramref name="ldb"/>×<paramref name="n"/></c> with <c><paramref name="ldb"/> ≥ max(1, <paramref name="k"/>)</c> if <paramref name="opB"/> == <see cref="MatrixOperation.None"/>, and <c><paramref name="ldb"/>×<paramref name="k"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="n"/>)</c> otherwise</param>
		/// <param name="ldb">leading dimension of two-dimensional array used to store matrix <paramref name="B"/></param>
		/// <param name="β">scalar used for multiplication. If <c><paramref name="β"/> == 0</c>, <paramref name="C"/> does not have to be a valid input</param>
		/// <param name="C">array of dimensions <c><paramref name="ldc"/>×<paramref name="n"/></c> with <c><paramref name="ldc"/> ≥ max(1, <paramref name="m"/>)</c></param>
		/// <param name="ldc">leading dimension of a two-dimensional array used to store the matrix <paramref name="C"/></param>
		/// <returns>operation status enum <see cref="Status"/></returns>
		internal delegate Status gemmFunc<T>(IntPtr handle, MatrixOperation opA, MatrixOperation opB, int m, int n, int k, ref T α, [In] IntPtr A, int lda, [In] IntPtr B, int ldb, ref T β, IntPtr C, int ldc);
		#region general matrix multiply
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasSgemm_v2(IntPtr handle, MatrixOperation opA, MatrixOperation opB, int m, int n, int k, ref float α, [In] IntPtr A, int lda, [In] IntPtr B, int ldb, ref float β, IntPtr C, int ldc);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasSgemm(IntPtr handle, MatrixOperation opA, MatrixOperation opB, int m, int n, int k, ref float α, [In] IntPtr A, int lda, [In] IntPtr B, int ldb, ref float β, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasDgemm_v2(IntPtr handle, MatrixOperation opA, MatrixOperation opB, int m, int n, int k, ref double α, [In] IntPtr A, int lda, [In] IntPtr B, int ldb, ref double β, IntPtr C, int ldc);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasDgemm(IntPtr handle, MatrixOperation opA, MatrixOperation opB, int m, int n, int k, ref double α, [In] IntPtr A, int lda, [In] IntPtr B, int ldb, ref double β, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasCgemm_v2(IntPtr handle, MatrixOperation opA, MatrixOperation opB, int m, int n, int k, ref FloatComplex α, [In] IntPtr A, int lda, [In] IntPtr B, int ldb, ref FloatComplex β, IntPtr C, int ldc);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasCgemm(IntPtr handle, MatrixOperation opA, MatrixOperation opB, int m, int n, int k, ref FloatComplex α, [In] IntPtr A, int lda, [In] IntPtr B, int ldb, ref FloatComplex β, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasZgemm_v2(IntPtr handle, MatrixOperation opA, MatrixOperation opB, int m, int n, int k, ref DoubleComplex α, [In] IntPtr A, int lda, [In] IntPtr B, int ldb, ref DoubleComplex β, IntPtr C, int ldc);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasZgemm(IntPtr handle, MatrixOperation opA, MatrixOperation opB, int m, int n, int k, ref DoubleComplex α, [In] IntPtr A, int lda, [In] IntPtr B, int ldb, ref DoubleComplex β, IntPtr C, int ldc);
		#endregion

		/// <summary>
		/// This function performs the symmetric/Hermitian matrix-matrix multiplication: if <paramref name="side"/> == <see cref="SideMode.Left"/>  <paramref name="C"/> = <paramref name="α"/> <paramref name="A"/> <paramref name="B"/> + <paramref name="β"/> <paramref name="C"/> , otherwise  <paramref name="C"/> = <paramref name="α"/> <paramref name="B"/> A + <paramref name="β"/> <paramref name="C"/>.<br/>
		/// Where <paramref name="A"/> is a symmetric matrix stored in lower or upper mode, <paramref name="B"/> and <paramref name="C"/> are <paramref name="m"/>×<paramref name="n"/> matrices, and <paramref name="α"/> and <paramref name="β"/> are scalars.
		/// </summary>
		/// <param name="handle">handle to the CUDA BLAS library context</param>
		/// <param name="side">indicates if matrix <paramref name="A"/> is on the left or right of <paramref name="B"/></param>
		/// <param name="uplo">indicates if matrix <paramref name="A"/> lower or upper part is stored</param>
		/// <param name="m">number of rows of matrix <paramref name="C"/> and <paramref name="B"/>, with matrix <paramref name="A"/> sized accordingly</param>
		/// <param name="n">number of columns of matrix <paramref name="C"/> and <paramref name="B"/>, with matrix <paramref name="A"/> sized accordingly</param>
		/// <param name="α">scalar used for multiplication</param>
		/// <param name="A">array of dimension <c><paramref name="lda"/>×<paramref name="m"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="m"/>)</c> if <paramref name="side"/> == <see cref="SideMode.Left"/>, and <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="n"/>)</c>otherwise</param>
		/// <param name="lda">leading dimension of two-dimensional array used to store matrix <paramref name="A"/></param>
		/// <param name="B">array of dimension <c><paramref name="ldb"/>×<paramref name="n"/></c> with <c><paramref name="ldb"/> ≥ max(1, <paramref name="m"/>)</c></param>
		/// <param name="ldb">leading dimension of two-dimensional array used to store matrix <paramref name="B"/></param>
		/// <param name="β">scalar used for multiplication, if <c><paramref name="β"/> == 0</c> then <paramref name="C"/> does not have to be a valid input</param>
		/// <param name="C">array of dimension <c><paramref name="ldc"/>×<paramref name="n"/></c> with <c><paramref name="ldc"/> ≥ max(1, <paramref name="m"/>)</c></param>
		/// <param name="ldc">leading dimension of two-dimensional array used to store matrix <paramref name="B"/></param>
		/// <returns>operation status enum <see cref="Status"/></returns>
		internal delegate Status symmFunc<T>(IntPtr handle, SideMode side, MatrixFillMode uplo, int m, int n, ref T α, [In] IntPtr A, int lda, [In] IntPtr B, int ldb, ref T β, IntPtr C, int ldc);
		#region symmetric matrix multiply
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasSsymm_v2(IntPtr handle, SideMode side, MatrixFillMode uplo, int m, int n, ref float α, [In] IntPtr A, int lda, [In] IntPtr B, int ldb, ref float β, IntPtr C, int ldc);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasSsymm(IntPtr handle, SideMode side, MatrixFillMode uplo, int m, int n, ref float α, [In] IntPtr A, int lda, [In] IntPtr B, int ldb, ref float β, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasDsymm_v2(IntPtr handle, SideMode side, MatrixFillMode uplo, int m, int n, ref double α, [In] IntPtr A, int lda, [In] IntPtr B, int ldb, ref double β, IntPtr C, int ldc);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasDsymm(IntPtr handle, SideMode side, MatrixFillMode uplo, int m, int n, ref double α, [In] IntPtr A, int lda, [In] IntPtr B, int ldb, ref double β, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasCsymm_v2(IntPtr handle, SideMode side, MatrixFillMode uplo, int m, int n, ref FloatComplex α, [In] IntPtr A, int lda, [In] IntPtr B, int ldb, ref FloatComplex β, IntPtr C, int ldc);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasCsymm(IntPtr handle, SideMode side, MatrixFillMode uplo, int m, int n, ref FloatComplex α, [In] IntPtr A, int lda, [In] IntPtr B, int ldb, ref FloatComplex β, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasZsymm_v2(IntPtr handle, SideMode side, MatrixFillMode uplo, int m, int n, ref DoubleComplex α, [In] IntPtr A, int lda, [In] IntPtr B, int ldb, ref DoubleComplex β, IntPtr C, int ldc);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasZsymm(IntPtr handle, SideMode side, MatrixFillMode uplo, int m, int n, ref DoubleComplex α, [In] IntPtr A, int lda, [In] IntPtr B, int ldb, ref DoubleComplex β, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasChemm_v2(IntPtr handle, SideMode side, MatrixFillMode uplo, int m, int n, ref FloatComplex α, [In] IntPtr A, int lda, [In] IntPtr B, int ldb, ref FloatComplex β, IntPtr C, int ldc);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasChemm(IntPtr handle, SideMode side, MatrixFillMode uplo, int m, int n, ref FloatComplex α, [In] IntPtr A, int lda, [In] IntPtr B, int ldb, ref FloatComplex β, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasZhemm_v2(IntPtr handle, SideMode side, MatrixFillMode uplo, int m, int n, ref DoubleComplex α, [In] IntPtr A, int lda, [In] IntPtr B, int ldb, ref DoubleComplex β, IntPtr C, int ldc);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasZhemm(IntPtr handle, SideMode side, MatrixFillMode uplo, int m, int n, ref DoubleComplex α, [In] IntPtr A, int lda, [In] IntPtr B, int ldb, ref DoubleComplex β, IntPtr C, int ldc);
		#endregion

		/// <summary>
		/// This function performs the symmetric/Hermitian rank-k update  <paramref name="C"/> = <paramref name="α"/> <paramref name="op"/>(<paramref name="A"/>) <paramref name="op"/>(<paramref name="A"/>)^T + <paramref name="β"/> <paramref name="C"/> ; where <paramref name="α"/> and <paramref name="β"/> are scalars, <paramref name="C"/> is a symmetric matrix stored in lower or upper mode, and <paramref name="A"/> is a matrix with dimensions <paramref name="op"/>(<paramref name="A"/>) <paramref name="n"/>×<paramref name="k"/>.
		/// </summary>
		/// <param name="handle">handle to the CUDA BLAS library context</param>
		/// <param name="uplo">indicates if matrix <paramref name="C"/> lower or upper part is stored</param>
		/// <param name="op"><see cref="MatrixOperation"/> that is non- or transpose</param>
		/// <param name="n">number of rows of matrix <paramref name="op"/>(<paramref name="A"/>) and <paramref name="C"/></param>
		/// <param name="k">number of columns of matrix <paramref name="op"/>(<paramref name="A"/>)</param>
		/// <param name="α">scalar used for multiplication</param>
		/// <param name="A">array of dimension <c><paramref name="lda"/>×<paramref name="k"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="n"/>)</c> if trans == <see cref="MatrixOperation.None"/> and <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="k"/>)</c> otherwise</param>
		/// <param name="lda">leading dimension of two-dimensional array used to store matrix <paramref name="A"/></param>
		/// <param name="β">if <c><paramref name="β"/> == 0</c> then <paramref name="C"/> does not have to be a valid input</param>
		/// <param name="C">array of dimension <c><paramref name="ldc"/>×<paramref name="n"/></c> with <c><paramref name="ldc"/> ≥ max(1, <paramref name="n"/>)</c></param>
		/// <param name="ldc">leading dimension of two-dimensional array used to store matrix <paramref name="C"/></param>
		/// <returns>operation status enum <see cref="Status"/></returns>
		internal delegate Status syrkFunc<T>(IntPtr handle, MatrixFillMode uplo, MatrixOperation op, int n, int k, ref T α, [In] IntPtr A, int lda, ref T β, IntPtr C, int ldc);
		#region symmetric rank k update
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasSsyrk_v2(IntPtr handle, MatrixFillMode uplo, MatrixOperation op, int n, int k, ref float α, [In] IntPtr A, int lda, ref float β, IntPtr C, int ldc);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasSsyrk(IntPtr handle, MatrixFillMode uplo, MatrixOperation op, int n, int k, ref float α, [In] IntPtr A, int lda, ref float β, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasDsyrk_v2(IntPtr handle, MatrixFillMode uplo, MatrixOperation op, int n, int k, ref double α, [In] IntPtr A, int lda, ref double β, IntPtr C, int ldc);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasDsyrk(IntPtr handle, MatrixFillMode uplo, MatrixOperation op, int n, int k, ref double α, [In] IntPtr A, int lda, ref double β, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasCsyrk_v2(IntPtr handle, MatrixFillMode uplo, MatrixOperation op, int n, int k, ref FloatComplex α, [In] IntPtr A, int lda, ref FloatComplex β, IntPtr C, int ldc);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasCsyrk(IntPtr handle, MatrixFillMode uplo, MatrixOperation op, int n, int k, ref FloatComplex α, [In] IntPtr A, int lda, ref FloatComplex β, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasZsyrk_v2(IntPtr handle, MatrixFillMode uplo, MatrixOperation op, int n, int k, ref DoubleComplex α, [In] IntPtr A, int lda, ref DoubleComplex β, IntPtr C, int ldc);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasZsyrk(IntPtr handle, MatrixFillMode uplo, MatrixOperation op, int n, int k, ref DoubleComplex α, [In] IntPtr A, int lda, ref DoubleComplex β, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasCherk_v2(IntPtr handle, MatrixFillMode uplo, MatrixOperation op, int n, int k, ref FloatComplex α, [In] IntPtr A, int lda, ref FloatComplex β, IntPtr C, int ldc);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasCherk(IntPtr handle, MatrixFillMode uplo, MatrixOperation op, int n, int k, ref FloatComplex α, [In] IntPtr A, int lda, ref FloatComplex β, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasZherk_v2(IntPtr handle, MatrixFillMode uplo, MatrixOperation op, int n, int k, ref DoubleComplex α, [In] IntPtr A, int lda, ref DoubleComplex β, IntPtr C, int ldc);
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasZherk(IntPtr handle, MatrixFillMode uplo, MatrixOperation op, int n, int k, ref DoubleComplex α, [In] IntPtr A, int lda, ref DoubleComplex β, IntPtr C, int ldc);
		#endregion

		#endregion


		#region BLAS like
		/// <summary>
		/// This function performs the matrix-matrix addition/transposition <paramref name="C"/> = <paramref name="α"/> <paramref name="opA"/>(<paramref name="A"/>) + <paramref name="β"/> <paramref name="opB"/>(<paramref name="B"/>)
		/// where <paramref name="α"/> and <paramref name="β"/> are scalars; <paramref name="A"/>, <paramref name="B"/>, <paramref name="C"/> are matrices stored in column-major format with dimensions <paramref name="opA"/>(<paramref name="A"/>) -- <paramref name="m"/>×<paramref name="n"/>, <paramref name="opB"/>(<paramref name="B"/>) -- <paramref name="m"/>×<paramref name="n"/> and <paramref name="C"/> -- <paramref name="m"/>×<paramref name="n"/>, respectively. <br/>
		/// Also, for matrix <paramref name="A"/>,
		/// <paramref name="opA"/>(<paramref name="A"/>) = <paramref name="A"/> if <paramref name="opA"/> == <see cref="MatrixOperation.None"/>;
		/// <paramref name="opA"/>(<paramref name="A"/>) = <paramref name="A"/>^T if <paramref name="opA"/> == <see cref="MatrixOperation.Transpose"/>;
		/// <paramref name="opA"/>(<paramref name="A"/>) = <paramref name="A"/>^H if <paramref name="opA"/> == <see cref="MatrixOperation.ConjugateTranspose"/>.
		/// The same for <paramref name="B"/>.
		/// </summary>
		/// <remarks>
		/// The operation is out-of-place if <paramref name="C"/> does not overlap <paramref name="A"/> or <paramref name="B"/>.<para/>
		/// The in-place mode supports the following two operations, <paramref name="C"/> = <paramref name="α"/> <paramref name="C"/> + <paramref name="β"/> <paramref name="opB"/>(<paramref name="B"/>) and <paramref name="C"/> = <paramref name="α"/> <paramref name="opA"/>(<paramref name="A"/>) + <paramref name="β"/> C.<br/>
		/// If <c><paramref name="C"/> == <paramref name="A"/></c>, <paramref name="ldc"/> = <paramref name="lda"/> and <paramref name="opA"/> = <see cref="MatrixOperation.None"/>, or If <c><paramref name="C"/> == <paramref name="B"/> &amp;&amp; <paramref name="ldc"/> == <paramref name="ldb"/> &amp;&amp; <paramref name="opA"/> == <see cref="MatrixOperation.None"/></c>, in-place mode will be used.<br/>
		/// If the user does not meet above requirements, <see cref="Status.InvalidValue"/> is returned.
		/// </remarks>
		/// <param name="handle">handle to the CUDA BLAS library context</param>
		/// <param name="opA"><see cref="MatrixOperation"/> that is non- or (conj.) transpose</param>
		/// <param name="opB"><see cref="MatrixOperation"/> that is non- or (conj.) transpose</param>
		/// <param name="m">number of rows of matrix <paramref name="opA"/>(<paramref name="A"/>) and <paramref name="C"/></param>
		/// <param name="n">number of columns of matrix <paramref name="opB"/>(<paramref name="B"/>) and <paramref name="C"/></param>
		/// <param name="α">scalar used for multiplication. If <c><paramref name="α"/> == 0</c>, <paramref name="A"/> does not have to be a valid input</param>
		/// <param name="A">array of dimensions <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="m"/>)</c> if <paramref name="opA"/> == <see cref="MatrixOperation.None"/> and <c><paramref name="lda"/>×<paramref name="m"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="n"/>)</c> otherwise</param>
		/// <param name="lda">leading dimension of two-dimensional array used to store the matrix <paramref name="A"/></param>
		/// <param name="β">scalar used for multiplication. If <c><paramref name="β"/> == 0</c>, <paramref name="B"/> does not have to be a valid input</param>
		/// <param name="B">array of dimensions <c><paramref name="ldb"/>×<paramref name="n"/></c> with <c><paramref name="ldb"/> ≥ max(1, <paramref name="m"/>)</c> if <paramref name="opA"/> == <see cref="MatrixOperation.None"/> and <c><paramref name="ldb"/>×<paramref name="m"/></c> with <c><paramref name="ldb"/> ≥ max(1, <paramref name="n"/>)</c> otherwise</param>
		/// <param name="ldb">leading dimension of two-dimensional array used to store the matrix <paramref name="B"/></param>
		/// <param name="C">array of dimensions <c><paramref name="ldc"/>×<paramref name="n"/></c> with <c><paramref name="ldc"/> ≥ max(1, <paramref name="m"/>)</c></param>
		/// <param name="ldc">leading dimension of two-dimensional array used to store the matrix <paramref name="C"/></param>
		/// <returns>operation status enum <see cref="Status"/></returns>
		internal delegate Status geamFunc<T>(IntPtr handle, MatrixOperation opA, MatrixOperation opB, int m, int n, ref T α, [In] IntPtr A, int lda, ref T β, [In] IntPtr B, int ldb, IntPtr C, int ldc);
		#region matrix add
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasSgeam(IntPtr handle, MatrixOperation opA, MatrixOperation opB, int m, int n, ref float α, [In] IntPtr A, int lda, ref float β, [In] IntPtr B, int ldb, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasDgeam(IntPtr handle, MatrixOperation opA, MatrixOperation opB, int m, int n, ref double α, [In] IntPtr A, int lda, ref double β, [In] IntPtr B, int ldb, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasCgeam(IntPtr handle, MatrixOperation opA, MatrixOperation opB, int m, int n, ref FloatComplex α, [In] IntPtr A, int lda, ref FloatComplex β, [In] IntPtr B, int ldb, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasZgeam(IntPtr handle, MatrixOperation opA, MatrixOperation opB, int m, int n, ref DoubleComplex α, [In] IntPtr A, int lda, ref DoubleComplex β, [In] IntPtr B, int ldb, IntPtr C, int ldc);
		#endregion

		/// <summary>
		/// This function performs the matrix-matrix multiplication <paramref name="C"/> = <paramref name="A"/> diag(x) if <paramref name="mode"/> == <see cref="SideMode.Right"/> or <paramref name="C"/> = diag(x) * <paramref name="A"/> otherwise.
		/// </summary>
		/// <param name="handle">handle to the CUDA BLAS library context</param>
		/// <param name="mode">left or right multiply</param>
		/// <param name="m">number of rows of matrix <paramref name="A"/> and <paramref name="C"/></param>
		/// <param name="n">number of columns of matrix <paramref name="A"/> and <paramref name="C"/></param>
		/// <param name="A">array of dimensions <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="m"/>)</c></param>
		/// <param name="lda">leading dimension of two-dimensional array used to store the matrix <paramref name="A"/></param>
		/// <param name="x">one-dimensional array</param>
		/// <param name="incx">stride of one-dimensional array x</param>
		/// <param name="C">array of dimensions <c><paramref name="ldc"/>×<paramref name="n"/></c> with <c><paramref name="ldc"/> ≥ max(1, <paramref name="m"/>)</c></param>
		/// <param name="ldc">leading dimension of two-dimensional array used to store the matrix <paramref name="C"/></param>
		/// <returns>operation status enum <see cref="Status"/></returns>
		internal delegate Status dgmmFunc(IntPtr handle, SideMode mode, int m, int n, [In] IntPtr A, int lda, [In] IntPtr x, int incx, IntPtr C, int ldc);
		#region diagonal matrix multiply
		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasSdgmm(IntPtr handle, SideMode mode, int m, int n, [In] IntPtr A, int lda, [In] IntPtr x, int incx, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasDdgmm(IntPtr handle, SideMode mode, int m, int n, [In] IntPtr A, int lda, [In] IntPtr x, int incx, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasCdgmm(IntPtr handle, SideMode mode, int m, int n, [In] IntPtr A, int lda, [In] IntPtr x, int incx, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cublasZdgmm(IntPtr handle, SideMode mode, int m, int n, [In] IntPtr A, int lda, [In] IntPtr x, int incx, IntPtr C, int ldc);
		#endregion

		#endregion
	}
}
