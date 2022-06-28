using System;
using System.Runtime.InteropServices;

using Althea.LinearAlgebra;
using Althea.LinearAlgebra.Sparse;
using Althea.Numerics;


#pragma warning disable IDE1006
namespace Althea.Backend.Cuda.LinearAlgebra.Sparse
{
	/// <summary>
	/// CUDA SPARSE library API
	/// </summary>
	public static unsafe class NativeMethods
	{
		/// <summary>
		/// The CUDA SPARSE (cuSPARSE) library name
		/// </summary>
		public const string CUSPARSE_API_DLL_NAME = @"cusparse";
		
		/// <summary>
		/// The custom CUDA library name
		/// </summary>
		public const string CUSTOM_API_DLL_NAME = Storage.NativeMethods.CUSTOM_API_DLL_NAME;


		#region create and destroy
		[DllImport(CUSPARSE_API_DLL_NAME)]
		internal static unsafe extern CudaSparseStatus cusparseCreate(out IntPtr handle);

		[DllImport(CUSPARSE_API_DLL_NAME)]
		internal static unsafe extern CudaSparseStatus cusparseDestroy(IntPtr handle);

		[DllImport(CUSPARSE_API_DLL_NAME)]
		internal static unsafe extern CudaSparseStatus cusparseCreateDnVec(ref IntPtr dnVecDescr, long length, IntPtr values, CudaDataType valueType);

		[DllImport(CUSPARSE_API_DLL_NAME)]
		internal static unsafe extern CudaSparseStatus cusparseDestroyDnVec(IntPtr dnVecDescr);

		[DllImport(CUSPARSE_API_DLL_NAME)]
		internal static unsafe extern CudaSparseStatus cusparseCreateSpVec(ref IntPtr spVecDescr, long length, long nnz, IntPtr indices, IntPtr values, IndexType idxType, IndexBase idxBase, CudaDataType valueType);

		[DllImport(CUSPARSE_API_DLL_NAME)]
		internal static unsafe extern CudaSparseStatus cusparseDestroySpVec(IntPtr spVecDescr);

		[DllImport(CUSPARSE_API_DLL_NAME)]
		internal static unsafe extern CudaSparseStatus cusparseCreateDnMat(ref IntPtr dnMatDescr, long rows, long cols, long ld, IntPtr values, CudaDataType valueType, DenseMatrixOrder order);

		[DllImport(CUSPARSE_API_DLL_NAME)]
		internal static unsafe extern CudaSparseStatus cusparseDestroyDnMat(IntPtr dnMatDescr);

		[DllImport(CUSPARSE_API_DLL_NAME)]
		internal static unsafe extern CudaDataType cusparseCreateCoo(ref IntPtr spMatDescr, long rows, long cols, long nnz, IntPtr cooRowInd, IntPtr cooColInd, IntPtr cooValues, IndexType cooIdxType, IndexBase idxBase, CudaDataType valueType);

		[DllImport(CUSPARSE_API_DLL_NAME)]
		internal static unsafe extern CudaDataType cusparseCreateCsr(ref IntPtr spMatDescr, long rows, long cols, long nnz, IntPtr csrRowOffsets, IntPtr csrColInd, IntPtr csrValues, IndexType csrRowOffsetsType, IndexType csrColIndType, IndexBase idxBase, CudaDataType valueType);

		[DllImport(CUSPARSE_API_DLL_NAME)]
		internal static unsafe extern CudaDataType cusparseCreateCsc(ref IntPtr spMatDescr, long rows, long cols, long nnz, IntPtr cscColOffsets, IntPtr cscRowInd, IntPtr cscValues, IndexType cscColOffsetsType, IndexType cscRowIndType, IndexBase idxBase, CudaDataType valueType);

		[DllImport(CUSPARSE_API_DLL_NAME)]
		internal static unsafe extern CudaSparseStatus cusparseDestroySpMat(IntPtr dnVecDescr);

		#endregion
	}
}
