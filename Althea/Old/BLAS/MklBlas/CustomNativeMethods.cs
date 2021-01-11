using System;
using System.Runtime.InteropServices;

using Althea.Runtime;
using Althea.Array;


namespace Althea.Blas.Mkl.Customs
{
	/// <summary>
	/// The custom dense array kernels API
	/// </summary>
	public static class NativeMethods
	{
		/// <summary>
		/// The custom supplementary routines library on CPU
		/// </summary>
		public const string KERNEL_DLL_NAME = "kernels_cpu";


		/// <summary>
		/// Point-wise divide: <c><paramref name="a"/> = <paramref name="a"/> ./ <paramref name="b"/></c>.
		/// </summary>
		/// <param name="a">array to be divided</param>
		/// <param name="b">array to divide</param>
		/// <param name="N">length of <paramref name="a"/> and <paramref name="b"/></param>
		internal delegate void ewDivFunc(IntPtr a, [In] IntPtr b, long N);
		#region point-wise divide
		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void ewDivS(IntPtr a, IntPtr b, long N);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void ewDivD(IntPtr a, IntPtr b, long N);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void ewDivC(IntPtr a, IntPtr b, long N);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void ewDivZ(IntPtr a, IntPtr b, long N);
		#endregion

		/// <summary>
		/// Point-wise multiply: <c><paramref name="a"/> = <paramref name="a"/> ./ <paramref name="b"/></c>.
		/// </summary>
		/// <param name="a">array to be multiplied</param>
		/// <param name="b">array to multiply</param>
		/// <param name="N">length of <paramref name="a"/> and <paramref name="b"/></param>
		internal delegate void ewMulFunc(IntPtr a, [In] IntPtr b, long N);
		#region point-wise multiply
		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void ewMulS(IntPtr a, IntPtr b, long N);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void ewMulD(IntPtr a, IntPtr b, long N);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void ewMulC(IntPtr a, IntPtr b, long N);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void ewMulZ(IntPtr a, IntPtr b, long N);
		#endregion

		/// <summary>
		/// Point-wise power: <c><paramref name="a"/> = <paramref name="a"/> .^ <paramref name="p"/></c>.
		/// </summary>
		/// <param name="a">array to be powered</param>
		/// <param name="p">exponent</param>
		/// <param name="N">length of <paramref name="a"/></param>
		internal delegate void ewPowFunc<T>(IntPtr a, T p, long N);
		#region point-wise power
		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void ewPowS(IntPtr a, float p, long N);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void ewPowD(IntPtr a, double p, long N);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void ewPowC(IntPtr a, float p, long N);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void ewPowZ(IntPtr a, double p, long N);
		#endregion

		/// <summary>
		/// Point-wise conjugate: <c><paramref name="a"/> = conj(<paramref name="a"/>)</c>.
		/// </summary>
		/// <param name="a">array to be conjugated</param>
		/// <param name="N">length of <paramref name="a"/></param>
		internal delegate void arrConjFunc<T>(IntPtr a, long N);
		#region point-wise conjugate
		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void arrConjC(IntPtr array, long N);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void arrConjZ(IntPtr array, long N);
		#endregion

		#region point-wise up-cast
		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void arrUpS2D(IntPtr dest, [In] IntPtr src, long N);
		#endregion

		/// <summary>
		/// Fill the array with ones
		/// </summary>
		/// <param name="a">array to fill</param>
		/// <param name="N">length of array</param>
		internal delegate void fillOneFunc(IntPtr a, long N);
		#region fill operations
		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void fillOneS(IntPtr a, long N);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void fillOneD(IntPtr a, long N);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void fillOneC(IntPtr a, long N);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void fillOneZ(IntPtr a, long N);
		#endregion

		/// <summary>
		/// Set the values of <paramref name="array"/> at <paramref name="pos"/> to <paramref name="value"/>.
		/// </summary>
		/// <param name="array">array to be altered</param>
		/// <param name="value">value to set</param>
		/// <param name="pos">position <see cref="int"/> array</param>
		/// <param name="posN">length of <paramref name="pos"/></param>
		internal delegate void setArrOneFunc<T>(IntPtr array, T value, [In] IntPtr pos, long posN);
		#region set at positions
		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void setArrOneS(IntPtr array, float value, [In] IntPtr pos, long posN);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void setArrOneD(IntPtr array, double value, [In] IntPtr pos, long posN);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void setArrOneC(IntPtr array, FloatComplex value, [In] IntPtr pos, long posN);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void setArrOneZ(IntPtr array, DoubleComplex value, [In] IntPtr pos, long posN);
		#endregion

		/// <summary>
		/// Truncate the array by comparing between each element and the given one <c><paramref name="arr"/><sub>i</sub> ← 0  i.f.f. <paramref name="arr"/><sub>i</sub> &lt; abs(<paramref name="threshold"/>)</c>.
		/// </summary>
		/// <param name="arr">array to be truncated</param>
		/// <param name="threshold">threshold used for truncation</param>
		/// <param name="N">length of <paramref name="arr"/></param>
		internal delegate void arrTrimFunc(IntPtr arr, float threshold, long N);
		#region dense array trim
		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void arrTrimS(IntPtr arr, float ratioThreshold, long N);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void arrTrimD(IntPtr arr, float ratioThreshold, long N);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void arrTrimC(IntPtr arr, float ratioThreshold, long N);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void arrTrimZ(IntPtr arr, float ratioThreshold, long N);
		#endregion

		/// <summary>
		/// Sum the vector.
		/// </summary>
		/// <param name="arr">array to be summed</param>
		/// <param name="N">length of <paramref name="arr"/></param>
		/// <param name="stride">the stride of <paramref name="arr"/></param>
		/// <return>the sum</return>
		internal delegate T sumVec<T>(IntPtr arr, long N, int stride);
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
		#endregion

		/// <summary>
		/// Calculate the Kronecker product of matrices.
		/// </summary>
		/// <param name="A">matrix at left with size <paramref name="lda"/>×<paramref name="na"/></param>
		/// <param name="lda">leading dimension of <paramref name="A"/></param>
		/// <param name="ma">number of rows of <paramref name="A"/></param>
		/// <param name="na">number of columns of <paramref name="A"/></param>
		/// <param name="B">matrix at right with size <paramref name="ldb"/>×<paramref name="nb"/></param>
		/// <param name="ldb">leading dimension of <paramref name="B"/></param>
		/// <param name="mb">number of rows of <paramref name="B"/></param>
		/// <param name="nb">number of columns of <paramref name="B"/></param>
		/// <param name="dest">destination matrix with size <c><paramref name="ldd"/> × <paramref name="na"/>*<paramref name="nb"/></c></param>
		/// <param name="ldd">leading dimension of <paramref name="dest"/></param>
		/// <param name="threads">number of threads to use</param>
		internal delegate void matKronFunc(IntPtr A, int lda, int ma, int na, IntPtr B, int ldb, int mb, int nb, IntPtr dest, int ldd, int threads);
		#region matrix Kronecker
		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void matKronS(IntPtr A, int lda, int ma, int na, IntPtr B, int ldb, int mb, int nb, IntPtr dest, int ldd, int threads);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void matKronD(IntPtr A, int lda, int ma, int na, IntPtr B, int ldb, int mb, int nb, IntPtr dest, int ldd, int threads);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void matKronC(IntPtr A, int lda, int ma, int na, IntPtr B, int ldb, int mb, int nb, IntPtr dest, int ldd, int threads);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void matKronZ(IntPtr A, int lda, int ma, int na, IntPtr B, int ldb, int mb, int nb, IntPtr dest, int ldd, int threads);
		#endregion

		/// <summary>
		/// Copy the upper part of matrix to its lower part.
		/// </summary>
		/// <param name="A">matrix at left with size <paramref name="ld"/>×<paramref name="n"/></param>
		/// <param name="ld">leading dimension of <paramref name="A"/></param>
		/// <param name="n">number of rows and columns of <paramref name="A"/></param>
		internal delegate void matUpCpyLowFunc(IntPtr A, int ld, int n);
		#region matrix upper part copy to lower
		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void matUpCpyLowS(IntPtr A, int ld, int n);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void matUpCpyLowD(IntPtr A, int ld, int n);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void matUpCpyLowC(IntPtr A, int ld, int n);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void matUpCpyLowZ(IntPtr A, int ld, int n);
		#endregion
	}

}
