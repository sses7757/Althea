using System;
using System.Runtime.InteropServices;

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


		#region utilities
		/// <summary>
		/// This function initializes the CUDA BLAS library and creates a handle to an opaque structure holding the CUDA BLAS library context.
		/// </summary>
		/// <param name="handle">returned CUDA BLAS handle</param>
		/// <returns>operation status enum <see cref="CudaBlasStatus"/></returns>
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCreate_v2(ref IntPtr handle);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCreate(ref IntPtr handle);

		/// <summary>
		/// This function releases hardware resources used by the CUBLAS library. This function is usually the last call with a particular handle to the CUBLAS library.
		/// </summary>
		/// <param name="handle">input CUDA BLAS handle</param>
		/// <returns>operation status enum <see cref="CudaBlasStatus"/></returns>
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasDestroy_v2(IntPtr handle);
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
		internal static extern CudaBlasStatus cublasGetAtomicsMode(IntPtr handle, ref AtomicsMode mode);

		/// <summary>
		/// Some routines like <c>cublas&lt;t&gt;symv</c> have an alternate implementation that use atomics to cumulate results. This implementation is generally significantly faster but can generate results that are not strictly identical from one run to the others. Mathematically, those different results are not significant but when debugging those differences can be prejudicial.
		/// <para/>This function allows or disallows the usage of atomics in the CUDA BLAS library for all routines which have an alternate implementation.
		/// </summary>
		/// <param name="handle">input CUDA BLAS handle</param>
		/// <param name="mode">the <see cref="AtomicsMode"/> to set</param>
		/// <returns>operation status enum <see cref="CudaBlasStatus"/></returns>
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSetAtomicsMode(IntPtr handle, AtomicsMode mode);

		/// <summary>
		/// This function obtains the pointer mode used by the cuBLAS library.
		/// </summary>
		/// <param name="handle">input CUDA BLAS handle</param>
		/// <param name="mode">returned <see cref="PointerMode"/></param>
		/// <returns>operation status enum <see cref="CudaBlasStatus"/></returns>
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasGetPointerMode_v2(IntPtr handle, ref PointerMode mode);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasGetPointerMode(IntPtr handle, ref PointerMode mode);

		/// <summary>
		/// This function sets the pointer mode used by the cuBLAS library.
		/// The default is for the values to be passed by reference on the host.
		/// </summary>
		/// <param name="handle">input CUDA BLAS handle</param>
		/// <param name="mode">the <see cref="PointerMode"/> to set</param>
		/// <returns>operation status enum <see cref="CudaBlasStatus"/></returns>
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSetPointerMode_v2(IntPtr handle, PointerMode mode);
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
		/// <summary>
		/// This function finds the (smallest) index of the element of the maximum magnitude.
		/// </summary>
		/// <param name="handle">handle to the CUDA BLAS library context</param>
		/// <param name="n">number of elements in the vector <paramref name="x"/></param>
		/// <param name="x">vector with elements</param>
		/// <param name="incx">stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="result">the resulting index, which is 0 if <c><paramref name="n"/>,<paramref name="incx"/> &lt;= 0</c></param>
		/// <returns>operation status enum <see cref="CudaBlasStatus"/></returns>
		internal delegate CudaBlasStatus amaxFunc(IntPtr handle, int n, IntPtr x, int incx, int* result);
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


		/// <summary>
		/// This function finds the (smallest) index of the element of the minimum magnitude.
		/// </summary>
		/// <param name="handle">handle to the CUDA BLAS library context</param>
		/// <param name="n">number of elements in the vector <paramref name="x"/></param>
		/// <param name="x">vector with elements</param>
		/// <param name="incx">stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="result">the resulting index, which is 0 if <c><paramref name="n"/>,<paramref name="incx"/> &lt;= 0</c></param>
		/// <returns>operation status enum <see cref="CudaBlasStatus"/></returns>
		internal delegate CudaBlasStatus aminFunc(IntPtr handle, int n, IntPtr x, int incx, int* result);
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

		/// <summary>
		/// This function computes the sum of the absolute values of the elements of vector <paramref name="x"/>.
		/// </summary>
		/// <param name="handle">handle to the CUDA BLAS library context</param>
		/// <param name="n">number of elements in the vector <paramref name="x"/></param>
		/// <param name="x">vector with elements</param>
		/// <param name="incx">stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="result">the resulting index, which is 0 if <c><paramref name="n"/>,<paramref name="incx"/> &lt;= 0</c></param>
		/// <returns>operation status enum <see cref="CudaBlasStatus"/></returns>
		internal delegate CudaBlasStatus asumFunc<T>(IntPtr handle, int n, IntPtr x, int incx, ref T result);
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
		/// <returns>operation status enum <see cref="CudaBlasStatus"/></returns>
		internal delegate CudaBlasStatus axpyFunc<T>(IntPtr handle, int n, ref T α, IntPtr x, int incx, IntPtr y, int incy);
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
		/// <returns>operation status enum <see cref="CudaBlasStatus"/></returns>
		internal delegate CudaBlasStatus dotFunc<T>(IntPtr handle, int n, IntPtr x, int incx, IntPtr y, int incy, ref T result);
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

		/// <summary>
		/// This function computes the Euclidean norm of the vector <paramref name="x"/>.
		/// </summary>
		/// <param name="handle">handle to the CUDA BLAS library context</param>
		/// <param name="n">number of elements in the vector <paramref name="x"/></param>
		/// <param name="x">vector with elements</param>
		/// <param name="incx">stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="result">the result, which is 0 if <c><paramref name="n"/>,<paramref name="incx"/> &lt;= 0</c></param>
		/// <returns>operation status enum <see cref="CudaBlasStatus"/></returns>
		internal delegate CudaBlasStatus normFunc<T>(IntPtr handle, int n, IntPtr x, int incx, ref T result);
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

		/// <summary>
		/// This function scales the vector <paramref name="x"/> by the scalar <paramref name="α"/> and overwrites it with the result.
		/// </summary>
		/// <param name="handle">handle to the CUDA BLAS library context</param>
		/// <param name="n">number of elements in the vector <paramref name="x"/></param>
		/// <param name="α">scalar used for multiplication</param>
		/// <param name="x">vector with <paramref name="n"/> elements</param>
		/// <param name="incx">stride between consecutive elements of <paramref name="x"/></param>
		/// <returns>operation status enum <see cref="CudaBlasStatus"/></returns>
		internal delegate CudaBlasStatus scalFunc<T>(IntPtr handle, int n, ref T α, IntPtr x, int incx);
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

		/// <summary>
		/// This function copies the vector <paramref name="x"/> into the vector <paramref name="y"/>. Hence, the performed operation is <c>y[j] = x[k] for i = 1,…,n; k = 1 + (i - 1)*incx and j = 1 + (i - 1)*incy</c>.
		/// </summary>
		/// <param name="handle">handle to the CUDA BLAS library context</param>
		/// <param name="n">number of elements in the vector <paramref name="x"/> and <paramref name="y"/></param>
		/// <param name="x">vector with <paramref name="n"/> elements</param>
		/// <param name="incx">stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="y">vector with <paramref name="n"/> elements</param>
		/// <param name="incy">stride between consecutive elements of <paramref name="y"/></param>
		/// <returns>operation status enum <see cref="CudaBlasStatus"/></returns>
		internal delegate CudaBlasStatus copyFunc(IntPtr handle, int n, IntPtr x, int incx, IntPtr y, int incy);
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
		/// <summary>
		/// This function performs the matrix-vector multiplication <paramref name="y"/> = <paramref name="α"/> <paramref name="op"/>(<paramref name="A"/>) <paramref name="x"/> + <paramref name="β"/> <paramref name="y"/> where <paramref name="A"/> is a <paramref name="m"/>×<paramref name="n"/> matrix stored in column-major format, <paramref name="x"/> and <paramref name="y"/> are vectors, and <paramref name="α"/> and <paramref name="β"/> are scalars.<br/>
		/// Also, for matrix <paramref name="A"/>: <paramref name="op"/>(<paramref name="A"/>) = <paramref name="A"/> if <paramref name="op"/> == <see cref="CuBlasMatrixOperation.None"/>; <paramref name="A"/>^T if <paramref name="op"/> == <see cref="CuBlasMatrixOperation.Transpose"/>; <paramref name="A"/>^H if <paramref name="op"/> == <see cref="CuBlasMatrixOperation.ConjugateTranspose"/>.
		/// </summary>
		/// <param name="handle">handle to the CUDA BLAS library context</param>
		/// <param name="op"><see cref="CuBlasMatrixOperation"/> that is non- or (conj.) transpose</param>
		/// <param name="m">number of rows of matrix <paramref name="A"/></param>
		/// <param name="n">number of columns of matrix <paramref name="A"/></param>
		/// <param name="α">scalar used for multiplication</param>
		/// <param name="A">array of dimension <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="m"/>)</c>. Unchanged on exit.</param>
		/// <param name="lda">leading dimension of two-dimensional array used to store matrix <paramref name="A"/></param>
		/// <param name="x">vector at least <c>(1+(<paramref name="n"/>-1)*abs(<paramref name="incx"/>))</c> elements if <paramref name="op"/>==<see cref="CuBlasMatrixOperation.None"/> or <c>(1+(<paramref name="m"/>-1)*abs(<paramref name="incx"/>))</c> otherwise</param>
		/// <param name="incx">stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="β">scalar used for multiplication, if <c>b<paramref name="β"/> == 0</c> then <paramref name="y"/> does not have to be a valid input</param>
		/// <param name="y">(in and out) vector at least <c>(1+(<paramref name="m"/>-1)*abs(<paramref name="incy"/>))</c> elements if <paramref name="op"/>==<see cref="CuBlasMatrixOperation.None"/> or <c>(1+(<paramref name="n"/>-1)*abs(<paramref name="incy"/>))</c> otherwise</param>
		/// <param name="incy">stride between consecutive elements of <paramref name="y"/></param>
		/// <returns>operation status enum <see cref="CudaBlasStatus"/></returns>
		internal delegate CudaBlasStatus gemvFunc<T>(IntPtr handle, CuBlasMatrixOperation op, int m, int n, ref T α, IntPtr A, int lda, IntPtr x, int incx, ref T β, IntPtr y, int incy);
		#region general matrix vector multiply
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSgemv_v2(IntPtr handle, CuBlasMatrixOperation op, int m, int n, void* α, IntPtr A, int lda, IntPtr x, int incx, void* β, IntPtr y, int incy);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSgemv(IntPtr handle, CuBlasMatrixOperation op, int m, int n, void* α, IntPtr A, int lda, IntPtr x, int incx, void* β, IntPtr y, int incy);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasDgemv_v2(IntPtr handle, CuBlasMatrixOperation op, int m, int n, void* α, IntPtr A, int lda, IntPtr x, int incx, void* β, IntPtr y, int incy);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasDgemv(IntPtr handle, CuBlasMatrixOperation op, int m, int n, void* α, IntPtr A, int lda, IntPtr x, int incx, void* β, IntPtr y, int incy);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCgemv_v2(IntPtr handle, CuBlasMatrixOperation op, int m, int n, void* α, IntPtr A, int lda, IntPtr x, int incx, void* β, IntPtr y, int incy);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCgemv(IntPtr handle, CuBlasMatrixOperation op, int m, int n, void* α, IntPtr A, int lda, IntPtr x, int incx, void* β, IntPtr y, int incy);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZgemv_v2(IntPtr handle, CuBlasMatrixOperation op, int m, int n, void* α, IntPtr A, int lda, IntPtr x, int incx, void* β, IntPtr y, int incy);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZgemv(IntPtr handle, CuBlasMatrixOperation op, int m, int n, void* α, IntPtr A, int lda, IntPtr x, int incx, void* β, IntPtr y, int incy);
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
		/// <returns>operation status enum <see cref="CudaBlasStatus"/></returns>
		internal delegate CudaBlasStatus symvFunc<T>(IntPtr handle, MatrixFillMode uplo, int n, ref T α, IntPtr A, int lda, IntPtr x, int incx, ref T β, IntPtr y, int incy);
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
		/// <returns>operation status enum <see cref="CudaBlasStatus"/></returns>
		internal delegate CudaBlasStatus gerFunc<T>(IntPtr handle, int m, int n, ref T α, IntPtr x, int incx, IntPtr y, int incy, IntPtr A, int lda);
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
		/// <returns>operation status enum <see cref="CudaBlasStatus"/></returns>
		internal delegate CudaBlasStatus syrFunc<T>(IntPtr handle, MatrixFillMode fillMode, int n, ref T α, IntPtr x, int incx, IntPtr A, int lda);
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

		#endregion


		#region level 3
		/// <summary>
		/// This function performs the matrix-matrix multiplication <paramref name="C"/> = <paramref name="α"/> <paramref name="opA"/>(<paramref name="A"/>) <paramref name="opB"/>(<paramref name="B"/>) + <paramref name="β"/> <paramref name="C"/> where <paramref name="α"/> and <paramref name="β"/> are scalars; <paramref name="A"/> , <paramref name="B"/> and <paramref name="C"/> are matrices stored in column-major format with dimensions <paramref name="opA"/>(<paramref name="A"/>) -- <paramref name="m"/>×<paramref name="k"/>, <paramref name="opB"/>(<paramref name="B"/>) -- <paramref name="k"/>×<paramref name="n"/> and <paramref name="C"/> -- <paramref name="m"/>×<paramref name="n"/>, respectively. <br/>
		/// Also, for matrix <paramref name="A"/>,
		/// <paramref name="opA"/>(<paramref name="A"/>) = <paramref name="A"/> if <paramref name="opA"/> == <see cref="CuBlasMatrixOperation.None"/>;
		/// <paramref name="opA"/>(<paramref name="A"/>) = <paramref name="A"/>^T if <paramref name="opA"/> == <see cref="CuBlasMatrixOperation.Transpose"/>;
		/// <paramref name="opA"/>(<paramref name="A"/>) = <paramref name="A"/>^H if <paramref name="opA"/> == <see cref="CuBlasMatrixOperation.ConjugateTranspose"/>.
		/// The same for <paramref name="B"/>.
		/// </summary>
		/// <param name="handle">handle to the CUDA BLAS library context</param>
		/// <param name="opA"><see cref="CuBlasMatrixOperation"/> that is non- or (conj.) transpose</param>
		/// <param name="opB"><see cref="CuBlasMatrixOperation"/> that is non- or (conj.) transpose</param>
		/// <param name="m">number of rows of matrix <paramref name="opA"/>(<paramref name="A"/>) and <paramref name="C"/></param>
		/// <param name="n">number of columns of matrix <paramref name="opB"/>(<paramref name="B"/>) and <paramref name="C"/></param>
		/// <param name="k">number of columns of <paramref name="opA"/>(<paramref name="A"/>) and rows of <paramref name="opB"/>(<paramref name="B"/>)</param>
		/// <param name="α">scalar used for multiplication</param>
		/// <param name="A">array of dimensions <c><paramref name="lda"/>×<paramref name="k"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="m"/>)</c> if <paramref name="opA"/> == <see cref="CuBlasMatrixOperation.None"/>, and <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="k"/>)</c> otherwise</param>
		/// <param name="lda">leading dimension of two-dimensional array used to store the matrix <paramref name="A"/></param>
		/// <param name="B">array of dimension <c><paramref name="ldb"/>×<paramref name="n"/></c> with <c><paramref name="ldb"/> ≥ max(1, <paramref name="k"/>)</c> if <paramref name="opB"/> == <see cref="CuBlasMatrixOperation.None"/>, and <c><paramref name="ldb"/>×<paramref name="k"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="n"/>)</c> otherwise</param>
		/// <param name="ldb">leading dimension of two-dimensional array used to store matrix <paramref name="B"/></param>
		/// <param name="β">scalar used for multiplication. If <c><paramref name="β"/> == 0</c>, <paramref name="C"/> does not have to be a valid input</param>
		/// <param name="C">array of dimensions <c><paramref name="ldc"/>×<paramref name="n"/></c> with <c><paramref name="ldc"/> ≥ max(1, <paramref name="m"/>)</c></param>
		/// <param name="ldc">leading dimension of a two-dimensional array used to store the matrix <paramref name="C"/></param>
		/// <returns>operation status enum <see cref="CudaBlasStatus"/></returns>
		internal delegate CudaBlasStatus gemmFunc<T>(IntPtr handle, CuBlasMatrixOperation opA, CuBlasMatrixOperation opB, int m, int n, int k, ref T α, IntPtr A, int lda, IntPtr B, int ldb, ref T β, IntPtr C, int ldc);
		#region general matrix multiply
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSgemm_v2(IntPtr handle, CuBlasMatrixOperation opA, CuBlasMatrixOperation opB, int m, int n, int k, void* α, IntPtr A, int lda, IntPtr B, int ldb, void* β, IntPtr C, int ldc);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSgemm(IntPtr handle, CuBlasMatrixOperation opA, CuBlasMatrixOperation opB, int m, int n, int k, void* α, IntPtr A, int lda, IntPtr B, int ldb, void* β, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasDgemm_v2(IntPtr handle, CuBlasMatrixOperation opA, CuBlasMatrixOperation opB, int m, int n, int k, void* α, IntPtr A, int lda, IntPtr B, int ldb, void* β, IntPtr C, int ldc);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasDgemm(IntPtr handle, CuBlasMatrixOperation opA, CuBlasMatrixOperation opB, int m, int n, int k, void* α, IntPtr A, int lda, IntPtr B, int ldb, void* β, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCgemm_v2(IntPtr handle, CuBlasMatrixOperation opA, CuBlasMatrixOperation opB, int m, int n, int k, void* α, IntPtr A, int lda, IntPtr B, int ldb, void* β, IntPtr C, int ldc);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCgemm(IntPtr handle, CuBlasMatrixOperation opA, CuBlasMatrixOperation opB, int m, int n, int k, void* α, IntPtr A, int lda, IntPtr B, int ldb, void* β, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZgemm_v2(IntPtr handle, CuBlasMatrixOperation opA, CuBlasMatrixOperation opB, int m, int n, int k, void* α, IntPtr A, int lda, IntPtr B, int ldb, void* β, IntPtr C, int ldc);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZgemm(IntPtr handle, CuBlasMatrixOperation opA, CuBlasMatrixOperation opB, int m, int n, int k, void* α, IntPtr A, int lda, IntPtr B, int ldb, void* β, IntPtr C, int ldc);
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
		/// <returns>operation status enum <see cref="CudaBlasStatus"/></returns>
		internal delegate CudaBlasStatus symmFunc<T>(IntPtr handle, SideMode side, MatrixFillMode uplo, int m, int n, ref T α, IntPtr A, int lda, IntPtr B, int ldb, ref T β, IntPtr C, int ldc);
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

		/// <summary>
		/// This function performs the symmetric/Hermitian rank-k update  <paramref name="C"/> = <paramref name="α"/> <paramref name="op"/>(<paramref name="A"/>) <paramref name="op"/>(<paramref name="A"/>)^T + <paramref name="β"/> <paramref name="C"/> ; where <paramref name="α"/> and <paramref name="β"/> are scalars, <paramref name="C"/> is a symmetric matrix stored in lower or upper mode, and <paramref name="A"/> is a matrix with dimensions <paramref name="op"/>(<paramref name="A"/>) <paramref name="n"/>×<paramref name="k"/>.
		/// </summary>
		/// <param name="handle">handle to the CUDA BLAS library context</param>
		/// <param name="uplo">indicates if matrix <paramref name="C"/> lower or upper part is stored</param>
		/// <param name="op"><see cref="CuBlasMatrixOperation"/> that is non- or transpose</param>
		/// <param name="n">number of rows of matrix <paramref name="op"/>(<paramref name="A"/>) and <paramref name="C"/></param>
		/// <param name="k">number of columns of matrix <paramref name="op"/>(<paramref name="A"/>)</param>
		/// <param name="α">scalar used for multiplication</param>
		/// <param name="A">array of dimension <c><paramref name="lda"/>×<paramref name="k"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="n"/>)</c> if trans == <see cref="CuBlasMatrixOperation.None"/> and <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="k"/>)</c> otherwise</param>
		/// <param name="lda">leading dimension of two-dimensional array used to store matrix <paramref name="A"/></param>
		/// <param name="β">if <c><paramref name="β"/> == 0</c> then <paramref name="C"/> does not have to be a valid input</param>
		/// <param name="C">array of dimension <c><paramref name="ldc"/>×<paramref name="n"/></c> with <c><paramref name="ldc"/> ≥ max(1, <paramref name="n"/>)</c></param>
		/// <param name="ldc">leading dimension of two-dimensional array used to store matrix <paramref name="C"/></param>
		/// <returns>operation status enum <see cref="CudaBlasStatus"/></returns>
		internal delegate CudaBlasStatus syrkFunc<T>(IntPtr handle, MatrixFillMode uplo, CuBlasMatrixOperation op, int n, int k, ref T α, IntPtr A, int lda, ref T β, IntPtr C, int ldc);
		#region symmetric rank k update
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSsyrk_v2(IntPtr handle, MatrixFillMode uplo, CuBlasMatrixOperation op, int n, int k, void* α, IntPtr A, int lda, void* β, IntPtr C, int ldc);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSsyrk(IntPtr handle, MatrixFillMode uplo, CuBlasMatrixOperation op, int n, int k, void* α, IntPtr A, int lda, void* β, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasDsyrk_v2(IntPtr handle, MatrixFillMode uplo, CuBlasMatrixOperation op, int n, int k, void* α, IntPtr A, int lda, void* β, IntPtr C, int ldc);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasDsyrk(IntPtr handle, MatrixFillMode uplo, CuBlasMatrixOperation op, int n, int k, void* α, IntPtr A, int lda, void* β, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCsyrk_v2(IntPtr handle, MatrixFillMode uplo, CuBlasMatrixOperation op, int n, int k, void* α, IntPtr A, int lda, void* β, IntPtr C, int ldc);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCsyrk(IntPtr handle, MatrixFillMode uplo, CuBlasMatrixOperation op, int n, int k, void* α, IntPtr A, int lda, void* β, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZsyrk_v2(IntPtr handle, MatrixFillMode uplo, CuBlasMatrixOperation op, int n, int k, void* α, IntPtr A, int lda, void* β, IntPtr C, int ldc);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZsyrk(IntPtr handle, MatrixFillMode uplo, CuBlasMatrixOperation op, int n, int k, void* α, IntPtr A, int lda, void* β, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCherk_v2(IntPtr handle, MatrixFillMode uplo, CuBlasMatrixOperation op, int n, int k, void* α, IntPtr A, int lda, void* β, IntPtr C, int ldc);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCherk(IntPtr handle, MatrixFillMode uplo, CuBlasMatrixOperation op, int n, int k, void* α, IntPtr A, int lda, void* β, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZherk_v2(IntPtr handle, MatrixFillMode uplo, CuBlasMatrixOperation op, int n, int k, void* α, IntPtr A, int lda, void* β, IntPtr C, int ldc);
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZherk(IntPtr handle, MatrixFillMode uplo, CuBlasMatrixOperation op, int n, int k, void* α, IntPtr A, int lda, void* β, IntPtr C, int ldc);
		#endregion

		#endregion


		#region BLAS like
		/// <summary>
		/// This function performs the matrix-matrix addition/transposition <paramref name="C"/> = <paramref name="α"/> <paramref name="opA"/>(<paramref name="A"/>) + <paramref name="β"/> <paramref name="opB"/>(<paramref name="B"/>)
		/// where <paramref name="α"/> and <paramref name="β"/> are scalars; <paramref name="A"/>, <paramref name="B"/>, <paramref name="C"/> are matrices stored in column-major format with dimensions <paramref name="opA"/>(<paramref name="A"/>) -- <paramref name="m"/>×<paramref name="n"/>, <paramref name="opB"/>(<paramref name="B"/>) -- <paramref name="m"/>×<paramref name="n"/> and <paramref name="C"/> -- <paramref name="m"/>×<paramref name="n"/>, respectively. <br/>
		/// Also, for matrix <paramref name="A"/>,
		/// <paramref name="opA"/>(<paramref name="A"/>) = <paramref name="A"/> if <paramref name="opA"/> == <see cref="CuBlasMatrixOperation.None"/>;
		/// <paramref name="opA"/>(<paramref name="A"/>) = <paramref name="A"/>^T if <paramref name="opA"/> == <see cref="CuBlasMatrixOperation.Transpose"/>;
		/// <paramref name="opA"/>(<paramref name="A"/>) = <paramref name="A"/>^H if <paramref name="opA"/> == <see cref="CuBlasMatrixOperation.ConjugateTranspose"/>.
		/// The same for <paramref name="B"/>.
		/// </summary>
		/// <remarks>
		/// The operation is out-of-place if <paramref name="C"/> does not overlap <paramref name="A"/> or <paramref name="B"/>.<para/>
		/// The in-place mode supports the following two operations, <paramref name="C"/> = <paramref name="α"/> <paramref name="C"/> + <paramref name="β"/> <paramref name="opB"/>(<paramref name="B"/>) and <paramref name="C"/> = <paramref name="α"/> <paramref name="opA"/>(<paramref name="A"/>) + <paramref name="β"/> C.<br/>
		/// If <c><paramref name="C"/> == <paramref name="A"/></c>, <paramref name="ldc"/> = <paramref name="lda"/> and <paramref name="opA"/> = <see cref="CuBlasMatrixOperation.None"/>, or If <c><paramref name="C"/> == <paramref name="B"/> &amp;&amp; <paramref name="ldc"/> == <paramref name="ldb"/> &amp;&amp; <paramref name="opA"/> == <see cref="CuBlasMatrixOperation.None"/></c>, in-place mode will be used.<br/>
		/// If the user does not meet above requirements, <see cref="CudaBlasStatus.InvalidValue"/> is returned.
		/// </remarks>
		/// <param name="handle">handle to the CUDA BLAS library context</param>
		/// <param name="opA"><see cref="CuBlasMatrixOperation"/> that is non- or (conj.) transpose</param>
		/// <param name="opB"><see cref="CuBlasMatrixOperation"/> that is non- or (conj.) transpose</param>
		/// <param name="m">number of rows of matrix <paramref name="opA"/>(<paramref name="A"/>) and <paramref name="C"/></param>
		/// <param name="n">number of columns of matrix <paramref name="opB"/>(<paramref name="B"/>) and <paramref name="C"/></param>
		/// <param name="α">scalar used for multiplication. If <c><paramref name="α"/> == 0</c>, <paramref name="A"/> does not have to be a valid input</param>
		/// <param name="A">array of dimensions <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="m"/>)</c> if <paramref name="opA"/> == <see cref="CuBlasMatrixOperation.None"/> and <c><paramref name="lda"/>×<paramref name="m"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="n"/>)</c> otherwise</param>
		/// <param name="lda">leading dimension of two-dimensional array used to store the matrix <paramref name="A"/></param>
		/// <param name="β">scalar used for multiplication. If <c><paramref name="β"/> == 0</c>, <paramref name="B"/> does not have to be a valid input</param>
		/// <param name="B">array of dimensions <c><paramref name="ldb"/>×<paramref name="n"/></c> with <c><paramref name="ldb"/> ≥ max(1, <paramref name="m"/>)</c> if <paramref name="opA"/> == <see cref="CuBlasMatrixOperation.None"/> and <c><paramref name="ldb"/>×<paramref name="m"/></c> with <c><paramref name="ldb"/> ≥ max(1, <paramref name="n"/>)</c> otherwise</param>
		/// <param name="ldb">leading dimension of two-dimensional array used to store the matrix <paramref name="B"/></param>
		/// <param name="C">array of dimensions <c><paramref name="ldc"/>×<paramref name="n"/></c> with <c><paramref name="ldc"/> ≥ max(1, <paramref name="m"/>)</c></param>
		/// <param name="ldc">leading dimension of two-dimensional array used to store the matrix <paramref name="C"/></param>
		/// <returns>operation status enum <see cref="CudaBlasStatus"/></returns>
		internal delegate CudaBlasStatus geamFunc<T>(IntPtr handle, CuBlasMatrixOperation opA, CuBlasMatrixOperation opB, int m, int n, ref T α, IntPtr A, int lda, ref T β, IntPtr B, int ldb, IntPtr C, int ldc);
		#region matrix add
		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasSgeam(IntPtr handle, CuBlasMatrixOperation opA, CuBlasMatrixOperation opB, int m, int n, void* α, IntPtr A, int lda, void* β, IntPtr B, int ldb, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasDgeam(IntPtr handle, CuBlasMatrixOperation opA, CuBlasMatrixOperation opB, int m, int n, void* α, IntPtr A, int lda, void* β, IntPtr B, int ldb, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasCgeam(IntPtr handle, CuBlasMatrixOperation opA, CuBlasMatrixOperation opB, int m, int n, void* α, IntPtr A, int lda, void* β, IntPtr B, int ldb, IntPtr C, int ldc);

		[DllImport(CUBLAS_API_DLL_NAME)]
		internal static extern CudaBlasStatus cublasZgeam(IntPtr handle, CuBlasMatrixOperation opA, CuBlasMatrixOperation opB, int m, int n, void* α, IntPtr A, int lda, void* β, IntPtr B, int ldb, IntPtr C, int ldc);
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
		/// <returns>operation status enum <see cref="CudaBlasStatus"/></returns>
		internal delegate CudaBlasStatus dgmmFunc(IntPtr handle, SideMode mode, int m, int n, IntPtr A, int lda, IntPtr x, int incx, IntPtr C, int ldc);
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
		internal static extern CudaBlasStatus cublasGemmEx(IntPtr handle, CuBlasMatrixOperation transa, int m, int n, int k, void* alpha, IntPtr A, CudaDataType Atype, int lda, IntPtr B, CudaDataType Btype, int ldb, IntPtr beta, IntPtr C, CudaDataType Ctype, int ldc, ComputeType computeType, GemmAlgorithm algo);
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
	}
}
