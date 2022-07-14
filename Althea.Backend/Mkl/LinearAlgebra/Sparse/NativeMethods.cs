using System.Runtime.InteropServices;

using Althea.SourceGenerator;


namespace Althea.Backend.Mkl.LinearAlgebra.Sparse
{
	[NativeMethodClass]
	internal static unsafe class NativeMethodsTemplate
	{
		[NativeMethod(11)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklSparseBlasError mkl_sparse_s_create_coo(out IntPtr A, int indexing, long rows, long cols, long nnz, void* row_indx, void* col_indx, void* values);
    }
}
