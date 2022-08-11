using System.Runtime.InteropServices;

using Althea.SourceGenerator;


namespace Althea.Backend.Cuda.LinearAlgebra.Sparse
{
	[NativeMethodClass]
	internal static unsafe class NativeMethodsTemplate
	{
		#region conversion
		[NativeMethod(8, true)]
		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseSbsr2csr(IntPtr handle, SparseMatrixOrder dirA, int mb, int nb, IntPtr descrA, void* bsrSortedValA, int* bsrSortedRowPtrA, int* bsrSortedColIndA, int blockDim, IntPtr descrC, void* csrSortedValC, int* csrSortedRowPtrC, int* csrSortedColIndC);

		[NativeMethod(8, true)]
		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseSgebsr2csr(IntPtr handle, SparseMatrixOrder dirA, int mb, int nb, IntPtr descrA, void* bsrSortedValA, int* bsrSortedRowPtrA, int* bsrSortedColIndA, int rowBlockDim, int colBlockDim, IntPtr descrC, void* csrSortedValC, int* csrSortedRowPtrC, int* csrSortedColIndC);

		[NativeMethod(8, true)]
		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseScsr2gebsr_bufferSize(IntPtr handle, SparseMatrixOrder dirA, int m, int n, IntPtr descrA, void* csrSortedValA, int* csrSortedRowPtrA, int* csrSortedColIndA, int rowBlockDim, int colBlockDim, out int pBufferSizeInBytes);

		[CustomNativeMethod(8, "Float32", "X")]
		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseXcsr2gebsrNnz(IntPtr handle, SparseMatrixOrder dirA, int m, int n, IntPtr descrA, void* csrSortedValA, int* csrSortedRowPtrA, int* csrSortedColIndA, int rowBlockDim, int colBlockDim, out int nnzTotal, void* pBuffer);

		[NativeMethod(8, true)]
		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseScsr2gebsr(IntPtr handle, SparseMatrixOrder dirA, int m, int n, IntPtr descrA, void* csrSortedValA, int* csrSortedRowPtrA, int* csrSortedColIndA, IntPtr descrC, void* bsrSortedValC, int* bsrSortedRowPtrC, int* bsrSortedColIndC, int rowBlockDim, int colBlockDim, void* pBuffer);

		////[NativeMethod(8, true)]
		////[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		////internal static extern CudaSparseStatus cusparseSnnz_compress(IntPtr handle, int m, IntPtr descr, Float32* csrSortedValA, int* csrSortedRowPtrA, int* nnzPerRow, int* nnzC, Float32 tol);

		////[NativeMethod(8, true)]
		////[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		////internal static extern CudaSparseStatus cusparseScsr2csr_compress(IntPtr handle, int m, int n, IntPtr descrA, Float32* csrSortedValA, int* csrSortedColIndA, int* csrSortedRowPtrA, int nnzA, int* nnzPerRow, Float32* csrSortedValC, int* csrSortedColIndC, int* csrSortedRowPtrC, Float32 tol);

		[NativeMethod(8, true)]
		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseSnnz(IntPtr handle, SparseMatrixOrder dirA, int m, int n, IntPtr descrA, void* A, int lda, int* nnzPerRowColumn, out int nnzTotal);

		[CustomNativeMethod(8, "Float16", "H")]
		[CustomNativeMethod(8, "Float32", "S")]
		[CustomNativeMethod(8, "Float64", "D")]
		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseHpruneDense2csrNnz(IntPtr handle, int m, int n, void* A, int lda, void* threshold, IntPtr descrC, int* csrRowPtrC, out int nnzTotal, void* pBuffer = null);

		[CustomNativeMethod(8, "Float16", "H")]
		[CustomNativeMethod(8, "Float32", "S")]
		[CustomNativeMethod(8, "Float64", "D")]
		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseSpruneDense2csr_bufferSizeExt(IntPtr handle, int m, int n, void* A, int lda, void* threshold, IntPtr descrC, void* csrSortedValC, int* csrSortedRowPtrC, int* csrSortedColIndC, out long pBufferSizeInBytes);

		[CustomNativeMethod(8, "Float16", "H")]
		[CustomNativeMethod(8, "Float32", "S")]
		[CustomNativeMethod(8, "Float64", "D")]
		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseSpruneDense2csr(IntPtr handle, int m, int n, void* A, int lda, void* threshold, IntPtr descrC, void* csrSortedValC, int* csrSortedRowPtrC, int* csrSortedColIndC, void* pBuffer);

		#endregion
	}

	/// <summary>
	/// CUDA SPARSE library API
	/// </summary>
	public static unsafe partial class NativeMethods
	{
		#region create and destroy
		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseCreate(out IntPtr handle);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseDestroy(IntPtr handle);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseSetPointerMode(IntPtr handle, PointerMode mode = PointerMode.Host);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseCreateDnVec(out IntPtr dnVecDescr, long length, IntPtr values, CudaDataType valueType);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseDestroyDnVec(IntPtr dnVecDescr);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseCreateSpVec(out IntPtr spVecDescr, long length, long nnz, IntPtr indices, IntPtr values, IndexType idxType, IndexBase idxBase, CudaDataType valueType);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseDestroySpVec(IntPtr spVecDescr);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseCreateDnMat(out IntPtr dnMatDescr, long rows, long cols, long ld, IntPtr values, CudaDataType valueType, DenseMatrixOrder order);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseDestroyDnMat(IntPtr dnMatDescr);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaDataType cusparseCreateCoo(out IntPtr spMatDescr, long rows, long cols, long nnz, IntPtr cooRowInd, IntPtr cooColInd, IntPtr cooValues, IndexType cooIdxType, IndexBase idxBase, CudaDataType valueType);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaDataType cusparseCreateCsr(out IntPtr spMatDescr, long rows, long cols, long nnz, IntPtr csrRowOffsets, IntPtr csrColInd, IntPtr csrValues, IndexType csrRowOffsetsType, IndexType csrColIndType, IndexBase idxBase, CudaDataType valueType);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaDataType cusparseCreateCsc(out IntPtr spMatDescr, long rows, long cols, long nnz, IntPtr cscColOffsets, IntPtr cscRowInd, IntPtr cscValues, IndexType cscColOffsetsType, IndexType cscRowIndType, IndexBase idxBase, CudaDataType valueType);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseDestroySpMat(IntPtr dnVecDescr);
		#endregion

		#region conversion
		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseCreateIdentityPermutation(IntPtr handle, int n, int* p);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseXcoo2csr(IntPtr handle, int* cooRowInd, int nnz, int m, int* csrRowPtr, IndexBase idxBase);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseXcsr2coo(IntPtr handle, int* csrRowPtr, int nnz, int m, int* cooRowInd, IndexBase idxBase);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseCsr2cscEx2_bufferSize(IntPtr handle, int m, int n, int nnz, void* csrVal, int* csrRowPtr, int* csrColInd, void* cscVal, int* cscColPtr, int* cscRowInd, CudaDataType valType, SparseAction copyValues, IndexBase idxBase, Csr2CscAlgorithm alg, out long bufferSize);
		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseCsr2cscEx2(IntPtr handle, int m, int n, int nnz, void* csrVal, int* csrRowPtr, int* csrColInd, void* cscVal, int* cscColPtr, int* cscRowInd, CudaDataType valType, SparseAction copyValues, IndexBase idxBase, Csr2CscAlgorithm alg, void* buffer);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseSparseToDense_bufferSize(IntPtr handle, IntPtr matA, IntPtr matB, SparseToDenseAlgorithm alg, out long bufferSize);
		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseSparseToDense(IntPtr handle, IntPtr matA, IntPtr matB, SparseToDenseAlgorithm alg, void* externalBuffer);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseDenseToSparse_bufferSize(IntPtr handle, IntPtr matA, IntPtr matB, DenseToSparseAlgorithm alg, out long bufferSize);
		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseDenseToSparse_analysis(IntPtr handle, IntPtr matA, IntPtr matB, DenseToSparseAlgorithm alg, void* bufferSize);
		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseDenseToSparse_convert(IntPtr handle, IntPtr matA, IntPtr matB, DenseToSparseAlgorithm alg, void* bufferSize);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseXcoosort_bufferSizeExt(IntPtr handle, int m, int n, int nnz, int* cooRowsA, int* cooColsA, out long pBufferSizeInBytes);
		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseXcoosortByRow(IntPtr handle, int m, int n, int nnz, int* cooRowsA, int* cooColsA, int* P, void* pBuffer);
		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseXcoosortByColumn(IntPtr handle, int m, int n, int nnz, int* cooRowsA, int* cooColsA, int* P, void* pBuffer);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseXcsrsort_bufferSizeExt(IntPtr handle, int m, int n, int nnz, int* csrRowPtrA, int* csrColIndA, out long pBufferSizeInBytes);
		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseXcsrsort(IntPtr handle, int m, int n, int nnz, IntPtr descrA, int* csrRowPtrA, int* csrColIndA, int* P, void* pBuffer);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseXcscsort_bufferSizeExt(IntPtr handle, int m, int n, int nnz, int* cscColPtrA, int* cscRowIndA, out long pBufferSizeInBytes);
		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseXcscsort(IntPtr handle, int m, int n, int nnz, IntPtr descrA, int* cscColPtrA, int* cscRowIndA, int* P, void* pBuffer);
		#endregion

		#region computation
		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseAxpby(IntPtr handle, void* alpha, IntPtr vecX, void* beta, IntPtr vecY);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseGather(IntPtr handle, IntPtr vecY, IntPtr vecX);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseScatter(IntPtr handle, IntPtr vecX, IntPtr vecY);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseSpVV_bufferSize(IntPtr handle, CuBlasOperation opX, IntPtr vecX, IntPtr vecY, void* result, CudaDataType computeType, out long bufferSize);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseSpVV(IntPtr handle, CuBlasOperation opX, IntPtr vecX, IntPtr vecY, void* result, CudaDataType computeType, void* buffer);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseSpMV_bufferSize(IntPtr handle, CuBlasOperation opA, void* alpha, IntPtr matA, IntPtr vecX, void* beta, IntPtr vecY, CudaDataType computeType, SparseMVAlgorithm alg, out long bufferSize);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseSpMV(IntPtr handle, CuBlasOperation opA, void* alpha, IntPtr matA, IntPtr vecX, void* beta, IntPtr vecY, CudaDataType computeType, SparseMVAlgorithm alg, void* buffer);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseSpMM_bufferSize(IntPtr handle, CuBlasOperation opA, CuBlasOperation opB, void* alpha, IntPtr matA, IntPtr matB, void* beta, IntPtr matC, CudaDataType computeType, SparseMMAlgorithm alg, out long bufferSize);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseSpMM_preprocess(IntPtr handle, CuBlasOperation opA, CuBlasOperation opB, void* alpha, IntPtr matA, IntPtr matB, void* beta, IntPtr matC, CudaDataType computeType, SparseMMAlgorithm alg, void* externalBuffer);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseSpMM(IntPtr handle, CuBlasOperation opA, CuBlasOperation opB, void* alpha, IntPtr matA, IntPtr matB, void* beta, IntPtr matC, CudaDataType computeType, SparseMMAlgorithm alg, void* externalBuffer);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseSpGEMM_createDescr(out IntPtr descr);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseSpGEMM_destroyDescr(IntPtr descr);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseSpGEMM_workEstimation(IntPtr handle, CuBlasOperation opA, CuBlasOperation opB, void* alpha, IntPtr matA, IntPtr matB, void* beta, IntPtr matC, CudaDataType computeType, SparseGemmAlgorithm alg, IntPtr spgemmDescr, out long bufferSize1, void* externalBuffer1 = null);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseSpGEMM_compute(IntPtr handle, CuBlasOperation opA, CuBlasOperation opB, void* alpha, IntPtr matA, IntPtr matB, void* beta, IntPtr matC, CudaDataType computeType, SparseGemmAlgorithm alg, IntPtr spgemmDescr, out long bufferSize2, void* externalBuffer2 = null);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseSpGEMM_copy(IntPtr handle, CuBlasOperation opA, CuBlasOperation opB, void* alpha, IntPtr matA, IntPtr matB, void* beta, IntPtr matC, CudaDataType computeType, SparseGemmAlgorithm alg, IntPtr spgemmDescr);
		#endregion
	}
}