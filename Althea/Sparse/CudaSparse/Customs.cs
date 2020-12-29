using System;
using System.Runtime.InteropServices;

using Althea.Array;


namespace Althea.SparseBlas.Cuda.Customs
{
	/// <summary>
	/// Custom native methods for GPU Sparse BLAS
	/// </summary>
	public static class NativeMethods
	{
		/// <summary>
		/// The custom GPU Sparse BLAS library name
		/// </summary>
		public const string KERNEL_DLL_NAME = "kernels";//@"D:\Works\git\DMRG_Heisenberg\C#\Althea\x64\Debug\kernels.dll";

		#region integer operations
		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError intMinMax([In] IntPtr v, long N, ref int min, ref int max);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError intMax([In] IntPtr v, long N, ref int max);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern int intFind([In] IntPtr v, long N, int toFind);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern int intLowerBound([In] IntPtr v, long N, int lower);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern int intUpperBound([In] IntPtr v, long N, int upper);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void intFillRange(IntPtr v, long N, int start, int step);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError intAddScalar(IntPtr arr, int scalar, long N);
		#endregion

		#region index to COO and back
		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError indexToCOO([In] IntPtr index, IntPtr rowIdx, IntPtr colIdx, long N, int ld);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError COOToIndex(IntPtr index, [In] IntPtr rowIdx, [In] IntPtr colIdx, long N, int ld);
		#endregion

		#region dense vector prune to sparse
		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError vecPruneBuffer(long N, CudaDataType type, ref long bufferSize);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError vecPruneS([In] IntPtr v, long N, float threshold, IntPtr buffer, ref long nnz, ref IntPtr indexOut, ref IntPtr valueOut);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError vecPruneD([In] IntPtr v, long N, float threshold, IntPtr buffer, ref long nnz, ref IntPtr indexOut, ref IntPtr valueOut);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError vecPruneC([In] IntPtr v, long N, float threshold, IntPtr buffer, ref long nnz, ref IntPtr indexOut, ref IntPtr valueOut);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError vecPruneZ([In] IntPtr v, long N, float threshold, IntPtr buffer, ref long nnz, ref IntPtr indexOut, ref IntPtr valueOut);

		internal delegate CudaError pruneFunc([In] IntPtr v, long N, float threshold, IntPtr buffer, ref long nnz, ref IntPtr indexOut, ref IntPtr valueOut);
		#endregion

		#region sparse vector multiply/divide dense vector
		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError vecSpDivMulDnS([In] IntPtr dense, long nnz, IntPtr sparse, [In] IntPtr index, bool mul);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError vecSpDivMulDnD([In] IntPtr dense, long nnz, IntPtr sparse, [In] IntPtr index, bool mul);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError vecSpDivMulDnC([In] IntPtr dense, long nnz, IntPtr sparse, [In] IntPtr index, bool mul);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError vecSpDivMulDnZ([In] IntPtr dense, long nnz, IntPtr sparse, [In] IntPtr index, bool mul);

		internal delegate CudaError spDivMulDnFunc([In] IntPtr dense, long nnz, IntPtr sparse, [In] IntPtr index, bool mul);
		#endregion

		#region sparse vector add sparse vector
		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError vecSpAddBuffer(long nnzA, long nnzB, CudaDataType type, ref long bufferSize);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError vecSpAddS([In] IntPtr A_index, [In] IntPtr A_value, long nnzA, [In] IntPtr B_index, [In] IntPtr B_value, long nnzB, IntPtr buffer, ref long nnzC, ref IntPtr C_index, ref IntPtr C_value);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError vecSpAddD([In] IntPtr A_index, [In] IntPtr A_value, long nnzA, [In] IntPtr B_index, [In] IntPtr B_value, long nnzB, IntPtr buffer, ref long nnzC, ref IntPtr C_index, ref IntPtr C_value);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError vecSpAddC([In] IntPtr A_index, [In] IntPtr A_value, long nnzA, [In] IntPtr B_index, [In] IntPtr B_value, long nnzB, IntPtr buffer, ref long nnzC, ref IntPtr C_index, ref IntPtr C_value);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError vecSpAddZ([In] IntPtr A_index, [In] IntPtr A_value, long nnzA, [In] IntPtr B_index, [In] IntPtr B_value, long nnzB, IntPtr buffer, ref long nnzC, ref IntPtr C_index, ref IntPtr C_value);

		internal delegate CudaError vecSpAddFunc([In] IntPtr A_index, [In] IntPtr A_value, long nnzA, [In] IntPtr B_index, [In] IntPtr B_value, long nnzB, IntPtr buffer, ref long nnzC, ref IntPtr C_index, ref IntPtr C_value);
		#endregion

		#region dense vector add sparse
		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError vecAxpyiS(IntPtr dense, long nnz, float alpha, [In] IntPtr sparse, [In] IntPtr index);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError vecAxpyiD(IntPtr dense, long nnz, double alpha, [In] IntPtr sparse, [In] IntPtr index);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError vecAxpyiC(IntPtr dense, long nnz, FloatComplex alpha, [In] IntPtr sparse, [In] IntPtr index);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError vecAxpyiZ(IntPtr dense, long nnz, DoubleComplex alpha, [In] IntPtr sparse, [In] IntPtr index);

		internal delegate CudaError vecSpAxpyFunc<T>(IntPtr dense, long nnz, T alpha, [In] IntPtr sparse, [In] IntPtr index);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError vecDnAddSpS(IntPtr dense, long nnz, [In] IntPtr sparse, [In] IntPtr index);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError vecDnAddSpD(IntPtr dense, long nnz, [In] IntPtr sparse, [In] IntPtr index);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError vecDnAddSpC(IntPtr dense, long nnz, [In] IntPtr sparse, [In] IntPtr index);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError vecDnAddSpZ(IntPtr dense, long nnz, [In] IntPtr sparse, [In] IntPtr index);

		internal delegate CudaError vecDnAddSpFunc(IntPtr dense, long nnz, [In] IntPtr sparse, [In] IntPtr index);
		#endregion

		#region get CSR / CSC matrix non-empty row / column indices
		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern long CSRGetNerBuffer(int rowsCols);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError CSRGetNer([In] IntPtr rowPtr, int rowsCols, ref int ner, IntPtr buffer, ref IntPtr result);
		#endregion

		#region sparse vector outer product to COOC matrix
		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError spVecOuterS([In] IntPtr valA, [In] IntPtr indA, long nnzA, [In] IntPtr valB, [In] IntPtr indB, long nnzB, IntPtr C, IntPtr rowC, IntPtr colC);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError spVecOuterD([In] IntPtr valA, [In] IntPtr indA, long nnzA, [In] IntPtr valB, [In] IntPtr indB, long nnzB, IntPtr C, IntPtr rowC, IntPtr colC);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError spVecOuterC([In] IntPtr valA, [In] IntPtr indA, long nnzA, [In] IntPtr valB, [In] IntPtr indB, long nnzB, IntPtr C, IntPtr rowC, IntPtr colC);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError spVecOuterZ([In] IntPtr valA, [In] IntPtr indA, long nnzA, [In] IntPtr valB, [In] IntPtr indB, long nnzB, IntPtr C, IntPtr rowC, IntPtr colC);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError spVecOuterNonconjC([In] IntPtr valA, [In] IntPtr indA, long nnzA, [In] IntPtr valB, [In] IntPtr indB, long nnzB, IntPtr C, IntPtr rowC, IntPtr colC);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError spVecOuterNonconjZ([In] IntPtr valA, [In] IntPtr indA, long nnzA, [In] IntPtr valB, [In] IntPtr indB, long nnzB, IntPtr C, IntPtr rowC, IntPtr colC);

		internal delegate CudaError spVecOuterFunc([In] IntPtr valA, [In] IntPtr indA, long nnzA, [In] IntPtr valB, [In] IntPtr indB, long nnzB, IntPtr C, IntPtr rowC, IntPtr colC);
		#endregion

		#region sparse COO matrix Kronecker
		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError cooMatKronS([In] IntPtr valA, [In] IntPtr rowA, [In] IntPtr colA, long nnzA, [In] IntPtr valB, [In] IntPtr rowB, [In] IntPtr colB, long nnzB, long ldB, long sdB, IntPtr valC, IntPtr rowC, IntPtr colC);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError cooMatKronD([In] IntPtr valA, [In] IntPtr rowA, [In] IntPtr colA, long nnzA, [In] IntPtr valB, [In] IntPtr rowB, [In] IntPtr colB, long nnzB, long ldB, long sdB, IntPtr valC, IntPtr rowC, IntPtr colC);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError cooMatKronC([In] IntPtr valA, [In] IntPtr rowA, [In] IntPtr colA, long nnzA, [In] IntPtr valB, [In] IntPtr rowB, [In] IntPtr colB, long nnzB, long ldB, long sdB, IntPtr valC, IntPtr rowC, IntPtr colC);

		[DllImport(KERNEL_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CudaError cooMatKronZ([In] IntPtr valA, [In] IntPtr rowA, [In] IntPtr colA, long nnzA, [In] IntPtr valB, [In] IntPtr rowB, [In] IntPtr colB, long nnzB, long ldB, long sdB, IntPtr valC, IntPtr rowC, IntPtr colC);

		internal delegate CudaError cooMatKronFunc([In] IntPtr valA, [In] IntPtr rowA, [In] IntPtr colA, long nnzA, [In] IntPtr valB, [In] IntPtr rowB, [In] IntPtr colB, long nnzB, long ldB, long sdB, IntPtr valC, IntPtr rowC, IntPtr colC);
		#endregion
	}
}