using System;
using System.Runtime.InteropServices;

using Althea.Runtime;
using Althea.Arrays;


namespace Althea.Blas.Cuda.Customs
{
	/// <summary>
	/// The custom dense array kernels API
	/// </summary>
	public static class NativeMethods
	{
		/// <summary>
		/// The custom supplementary routines library on GPU library
		/// </summary>
		public const string KERNEL_DLL_NAME = "kernels";

		#region point-wise divide
		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError ewDivS(IntPtr a, IntPtr b, long N);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError ewDivD(IntPtr a, IntPtr b, long N);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError ewDivC(IntPtr a, IntPtr b, long N);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError ewDivZ(IntPtr a, IntPtr b, long N);

		internal delegate CudaError ewMulDivFunc(IntPtr a, IntPtr b, long N);
		#endregion

		#region point-wise multiply
		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError ewMulS(IntPtr a, IntPtr b, long N);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError ewMulD(IntPtr a, IntPtr b, long N);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError ewMulC(IntPtr a, IntPtr b, long N);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError ewMulZ(IntPtr a, IntPtr b, long N);
		#endregion

		#region point-wise power
		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError ewPowS(IntPtr a, float p, long N);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError ewPowD(IntPtr a, double p, long N);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError ewPowC(IntPtr a, float p, long N);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError ewPowZ(IntPtr a, double p, long N);
		#endregion

		#region point-wise conjugate
		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError arrConjC(IntPtr array, long N);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError arrConjZ(IntPtr array, long N);
		#endregion

		#region point-wise up-cast
		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError arrUpS2D(IntPtr dest, IntPtr src, long N);
		#endregion

		#region fill operations
		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError fillOneS(IntPtr a, long N);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError fillOneD(IntPtr a, long N);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError fillOneC(IntPtr a, long N);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError fillOneZ(IntPtr a, long N);

		internal delegate CudaError fillOneFunc(IntPtr a, long N);
		#endregion

		#region set at positions
		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError setArrOneS(IntPtr array, float value, [In] IntPtr pos, long posN);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError setArrOneD(IntPtr array, double value, [In] IntPtr pos, long posN);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError setArrOneC(IntPtr array, FloatComplex value, [In] IntPtr pos, long posN);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError setArrOneZ(IntPtr array, DoubleComplex value, [In] IntPtr pos, long posN);


		internal delegate CudaError setArrOneFunc<T>(IntPtr array, T value, [In] IntPtr pos, long posN);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError vecScatterS(IntPtr array, IntPtr values, [In] IntPtr pos, long posN);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError vecScatterD(IntPtr array, IntPtr values, [In] IntPtr pos, long N);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError vecScatterC(IntPtr array, IntPtr values, [In] IntPtr pos, long N);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError vecScatterZ(IntPtr array, IntPtr values, [In] IntPtr pos, long N);

		internal delegate CudaError vecScatterFunc(IntPtr array, IntPtr values, [In] IntPtr pos, long N);
		#endregion

		#region copy array at positions
		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError vecGatherS(IntPtr dest, [In]  IntPtr source, [In] IntPtr pos, long N);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError vecGatherD(IntPtr dest, [In]  IntPtr source, [In] IntPtr pos, long N);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError vecGatherC(IntPtr dest, [In]  IntPtr source, [In] IntPtr pos, long N);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError vecGatherZ(IntPtr dest, [In]  IntPtr source, [In] IntPtr pos, long N);

		internal delegate CudaError vecGatherFunc(IntPtr dest, [In]  IntPtr source, [In] IntPtr pos, long N);
		#endregion

		#region dense array trim
		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError arrTrimS(IntPtr arr, float ratioThreshold, long N);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError arrTrimD(IntPtr arr, float ratioThreshold, long N);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError arrTrimC(IntPtr arr, float ratioThreshold, long N);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError arrTrimZ(IntPtr arr, float ratioThreshold, long N);

		internal delegate CudaError arrTrimFunc(IntPtr arr, float ratioThreshold, long N);
		#endregion

		#region direct sum
		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern float sumVecS(IntPtr arr, long N, int stride);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern double sumVecD(IntPtr arr, long N, int stride);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern FloatComplex sumVecC(IntPtr arr, long N, int stride);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern DoubleComplex sumVecZ(IntPtr arr, long N, int stride);

		internal delegate T sumVec<T>(IntPtr arr, long N, int stride);
		#endregion

		#region matrix Kronecker
		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError matKronS(IntPtr A, int lda, int ma, int na, IntPtr B, int ldb, int mb, int nb, IntPtr dest, int ldd);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError matKronD(IntPtr A, int lda, int ma, int na, IntPtr B, int ldb, int mb, int nb, IntPtr dest, int ldd);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError matKronC(IntPtr A, int lda, int ma, int na, IntPtr B, int ldb, int mb, int nb, IntPtr dest, int ldd);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError matKronZ(IntPtr A, int lda, int ma, int na, IntPtr B, int ldb, int mb, int nb, IntPtr dest, int ldd);

		internal delegate CudaError matKronFunc(IntPtr A, int lda, int ma, int na, IntPtr B, int ldb, int mb, int nb, IntPtr dest, int ldd);
		#endregion

		#region matrix upper part copy to lower
		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError matUpCpyLowS(IntPtr A, int ld, int n);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError matUpCpyLowD(IntPtr A, int ld, int n);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError matUpCpyLowC(IntPtr A, int ld, int n);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError matUpCpyLowZ(IntPtr A, int ld, int n);

		internal delegate CudaError matUpCpyLowFunc(IntPtr A, int ld, int n);
		#endregion
	}

}
