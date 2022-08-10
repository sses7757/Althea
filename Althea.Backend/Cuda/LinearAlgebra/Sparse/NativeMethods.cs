using System.Runtime.InteropServices;


namespace Althea.Backend.Cuda.LinearAlgebra.Sparse;

/// <summary>
/// CUDA SPARSE library API
/// </summary>
public static unsafe class NativeMethods
{
	#region create and destroy
	[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
	internal static unsafe extern CudaSparseStatus cusparseCreate(out IntPtr handle);

	[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
	internal static unsafe extern CudaSparseStatus cusparseDestroy(IntPtr handle);

	[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
	internal static unsafe extern CudaSparseStatus cusparseCreateDnVec(ref IntPtr dnVecDescr, long length, IntPtr values, CudaDataType valueType);

	[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
	internal static unsafe extern CudaSparseStatus cusparseDestroyDnVec(IntPtr dnVecDescr);

	[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
	internal static unsafe extern CudaSparseStatus cusparseCreateSpVec(ref IntPtr spVecDescr, long length, long nnz, IntPtr indices, IntPtr values, IndexType idxType, IndexBase idxBase, CudaDataType valueType);

	[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
	internal static unsafe extern CudaSparseStatus cusparseDestroySpVec(IntPtr spVecDescr);

	[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
	internal static unsafe extern CudaSparseStatus cusparseCreateDnMat(ref IntPtr dnMatDescr, long rows, long cols, long ld, IntPtr values, CudaDataType valueType, DenseMatrixOrder order);

	[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
	internal static unsafe extern CudaSparseStatus cusparseDestroyDnMat(IntPtr dnMatDescr);

	[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
	internal static unsafe extern CudaDataType cusparseCreateCoo(ref IntPtr spMatDescr, long rows, long cols, long nnz, IntPtr cooRowInd, IntPtr cooColInd, IntPtr cooValues, IndexType cooIdxType, IndexBase idxBase, CudaDataType valueType);

	[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
	internal static unsafe extern CudaDataType cusparseCreateCsr(ref IntPtr spMatDescr, long rows, long cols, long nnz, IntPtr csrRowOffsets, IntPtr csrColInd, IntPtr csrValues, IndexType csrRowOffsetsType, IndexType csrColIndType, IndexBase idxBase, CudaDataType valueType);

	[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
	internal static unsafe extern CudaDataType cusparseCreateCsc(ref IntPtr spMatDescr, long rows, long cols, long nnz, IntPtr cscColOffsets, IntPtr cscRowInd, IntPtr cscValues, IndexType cscColOffsetsType, IndexType cscRowIndType, IndexBase idxBase, CudaDataType valueType);

	[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
	internal static unsafe extern CudaSparseStatus cusparseDestroySpMat(IntPtr dnVecDescr);

	#endregion
}
