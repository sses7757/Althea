using System;
using System.Runtime.InteropServices;

using Althea.Array;
using Althea.Backend.Mkl;
using Althea.SourceGenerator;


namespace Althea.Backend.Cuda.LinearAlgebra.Sparse
{
	[NativeMethodClass]
	internal static unsafe class NativeMethodsTemplate
	{
		#region conversion
		[NativeMethod(8, true)]
		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseSbsr2csr(IntPtr handle, SparseMatrixOrder dirA, int mb, int nb, SparseMatrixWrapper descrA, void* bsrSortedValA, int* bsrSortedRowPtrA, int* bsrSortedColIndA, int blockDim, SparseMatrixWrapper descrC, void* csrSortedValC, int* csrSortedRowPtrC, int* csrSortedColIndC);

		[NativeMethod(8, true)]
		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseSgebsr2csr(IntPtr handle, SparseMatrixOrder dirA, int mb, int nb, SparseMatrixWrapper descrA, void* bsrSortedValA, int* bsrSortedRowPtrA, int* bsrSortedColIndA, int rowBlockDim, int colBlockDim, SparseMatrixWrapper descrC, void* csrSortedValC, int* csrSortedRowPtrC, int* csrSortedColIndC);

		[NativeMethod(8, true)]
		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseScsr2gebsr_bufferSize(IntPtr handle, SparseMatrixOrder dirA, int m, int n, SparseMatrixWrapper descrA, void* csrSortedValA, int* csrSortedRowPtrA, int* csrSortedColIndA, int rowBlockDim, int colBlockDim, out int pBufferSizeInBytes);

		[CustomNativeMethod(8, "Float32", "X")]
		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseXcsr2gebsrNnz(IntPtr handle, SparseMatrixOrder dirA, int m, int n, SparseMatrixWrapper descrA, void* csrSortedValA, int* csrSortedRowPtrA, int* csrSortedColIndA, int rowBlockDim, int colBlockDim, out int nnzTotal, void* pBuffer);

		[NativeMethod(8, true)]
		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseScsr2gebsr(IntPtr handle, SparseMatrixOrder dirA, int m, int n, SparseMatrixWrapper descrA, void* csrSortedValA, int* csrSortedRowPtrA, int* csrSortedColIndA, SparseMatrixWrapper descrC, void* bsrSortedValC, int* bsrSortedRowPtrC, int* bsrSortedColIndC, int rowBlockDim, int colBlockDim, void* pBuffer);

		[NativeMethod(8, true)]
		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseSnnz_compress(IntPtr handle, int m, SparseMatrixWrapper descr, Float32* csrSortedValA, int* csrSortedRowPtrA, int* nnzPerRow, out int nnzC, Float32 tol);

		[NativeMethod(8, true)]
		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseScsr2csr_compress(IntPtr handle, int m, int n, SparseMatrixWrapper descrA, Float32* csrSortedValA, int* csrSortedColIndA, int* csrSortedRowPtrA, int nnzA, int* nnzPerRow, Float32* csrSortedValC, int* csrSortedColIndC, int* csrSortedRowPtrC, Float32 tol);

		[NativeMethod(8, true)]
		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseSnnz(IntPtr handle, SparseMatrixOrder dirA, int m, int n, SparseMatrixWrapper descrA, void* A, int lda, int* nnzPerRowColumn, out int nnzTotal);

		[CustomNativeMethod(8, "Float16", "H")]
		[CustomNativeMethod(8, "Float32", "S")]
		[CustomNativeMethod(8, "Float64", "D")]
		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseHpruneDense2csrNnz(IntPtr handle, int m, int n, void* A, int lda, void* threshold, SparseMatrixWrapper descrC, int* csrRowPtrC, out int nnzTotal, void* pBuffer = null);

		[CustomNativeMethod(8, "Float16", "H")]
		[CustomNativeMethod(8, "Float32", "S")]
		[CustomNativeMethod(8, "Float64", "D")]
		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseSpruneDense2csr_bufferSizeExt(IntPtr handle, int m, int n, void* A, int lda, void* threshold, SparseMatrixWrapper descrC, void* csrSortedValC, int* csrSortedRowPtrC, int* csrSortedColIndC, out long pBufferSizeInBytes);

		[CustomNativeMethod(8, "Float16", "H")]
		[CustomNativeMethod(8, "Float32", "S")]
		[CustomNativeMethod(8, "Float64", "D")]
		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseSpruneDense2csr(IntPtr handle, int m, int n, void* A, int lda, void* threshold, SparseMatrixWrapper descrC, void* csrSortedValC, int* csrSortedRowPtrC, int* csrSortedColIndC, void* pBuffer);

		[CustomNativeMethod(8, "Float16", "H")]
		[CustomNativeMethod(8, "Float32", "S")]
		[CustomNativeMethod(8, "Float64", "D")]
		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseHpruneCsr2csr_bufferSizeExt(IntPtr handle, int m, int n, int nnzA, SparseMatrixWrapper descrA, void* csrValA, int* csrRowPtrA, int* csrColIndA, void* threshold, SparseMatrixWrapper descrC, void* csrValC, int* csrRowPtrC, int* csrColIndC, out long pBufferSizeInBytes);

		[CustomNativeMethod(8, "Float16", "H")]
		[CustomNativeMethod(8, "Float32", "S")]
		[CustomNativeMethod(8, "Float64", "D")]
		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseHpruneCsr2csrNnz(IntPtr handle, int m, int n, int nnzA, SparseMatrixWrapper descrA, void* csrValA, int* csrRowPtrA, int* csrColIndA, void* threshold, SparseMatrixWrapper descrC, int* csrRowPtrC, out int nnzTotal, void* pBuffer);

		[CustomNativeMethod(8, "Float16", "H")]
		[CustomNativeMethod(8, "Float32", "S")]
		[CustomNativeMethod(8, "Float64", "D")]
		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseHpruneCsr2csr(IntPtr handle, int m, int n, int nnzA, SparseMatrixWrapper descrA, void* csrValA, int* csrRowPtrA, int* csrColIndA, void* threshold, SparseMatrixWrapper descrC, void* csrValC, int* csrRowPtrC, int* csrColIndC, void* pBuffer);
		#endregion
	}

	/// <summary>
	/// CUDA SPARSE library API
	/// </summary>
	public static unsafe partial class NativeMethods
	{
		#region format
		////internal const SparseFormat.Type EllType = (SparseFormat.Type)((int)SparseFormat.Type.Compressed << 1);

		////internal static readonly SparseFormat BlockEllFormat = new(EllType, SparseFormat.Blocking.Simple, SparseFormat.Major.Row);/

		internal static readonly SparseFormat SupportFormat = SparseFormat.MatrixCocFormat | SparseFormat.MatrixCorFormat | SparseFormat.MatrixCsrFormat | SparseFormat.MatrixCscFormat;
		#endregion

		#region create and destroy
		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseCreate(out IntPtr handle);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseDestroy(IntPtr handle);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseSetPointerMode(IntPtr handle, PointerMode mode = PointerMode.Host);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseCreateDnVec(out IntPtr dnVecDescr, long length, void* values, CudaDataType valueType);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseDestroyDnVec(IntPtr dnVecDescr);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseCreateSpVec(out IntPtr spVecDescr, long length, long nnz, void* indices, void* values, IndexType idxType, IndexBase idxBase, CudaDataType valueType);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseDestroySpVec(IntPtr spVecDescr);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseCreateDnMat(out IntPtr dnMatDescr, long rows, long cols, long ld, void* values, CudaDataType valueType, DenseMatrixOrder order);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseDestroyDnMat(IntPtr dnMatDescr);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseCreateCoo(out IntPtr spMatDescr, long rows, long cols, long nnz, void* cooRowInd, void* cooColInd, void* cooValues, IndexType cooIdxType, IndexBase idxBase, CudaDataType valueType);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseCreateCsr(out IntPtr spMatDescr, long rows, long cols, long nnz, void* csrRowOffsets, void* csrColInd, void* csrValues, IndexType csrRowOffsetsType, IndexType csrColIndType, IndexBase idxBase, CudaDataType valueType);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseCreateCsc(out IntPtr spMatDescr, long rows, long cols, long nnz, void* cscColOffsets, void* cscRowInd, void* cscValues, IndexType cscColOffsetsType, IndexType cscRowIndType, IndexBase idxBase, CudaDataType valueType);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseDestroySpMat(IntPtr dnVecDescr);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseSpMatGetSize(IntPtr spMatDescr, out long rows, out long cols, out long nnz);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseSpMatGetFormat(IntPtr spMatDescr, out MatrixFormat format);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseSpMatGetValues(IntPtr spMatDescr, out void* values);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseCsrSetPointers(IntPtr spMatDescr, void* csrRowOffsets, void* csrColInd, void* csrValues);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseCscSetPointers(IntPtr spMatDescr, void* csrColOffsets, void* csrRowInd, void* cscValues);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseCooSetPointers(IntPtr spMatDescr, void* cooRows, void* cooCols, void* cooValues);
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
		internal static extern CudaSparseStatus cusparseSparseToDense_bufferSize(IntPtr handle, SparseMatrixWrapper matA, DenseMatrixWrapper matB, SparseToDenseAlgorithm alg, out long bufferSize);
		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseSparseToDense(IntPtr handle, SparseMatrixWrapper matA, DenseMatrixWrapper matB, SparseToDenseAlgorithm alg, void* externalBuffer);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseDenseToSparse_bufferSize(IntPtr handle, DenseMatrixWrapper matA, SparseMatrixWrapper matB, DenseToSparseAlgorithm alg, out long bufferSize);
		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseDenseToSparse_analysis(IntPtr handle, DenseMatrixWrapper matA, SparseMatrixWrapper matB, DenseToSparseAlgorithm alg, void* bufferSize);
		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseDenseToSparse_convert(IntPtr handle, DenseMatrixWrapper matA, SparseMatrixWrapper matB, DenseToSparseAlgorithm alg, void* bufferSize);

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
		internal static extern CudaSparseStatus cusparseAxpby(IntPtr handle, void* alpha, SparseVectorWrapper vecX, void* beta, DenseVectorWrapper vecY);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseGather(IntPtr handle, DenseVectorWrapper vecY, SparseVectorWrapper vecX);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseScatter(IntPtr handle, SparseVectorWrapper vecX, DenseVectorWrapper vecY);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseSpVV_bufferSize(IntPtr handle, CuBlasOperation opX, SparseVectorWrapper vecX, DenseVectorWrapper vecY, void* result, CudaDataType computeType, out long bufferSize);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseSpVV(IntPtr handle, CuBlasOperation opX, SparseVectorWrapper vecX, DenseVectorWrapper vecY, void* result, CudaDataType computeType, void* buffer);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseSpMV_bufferSize(IntPtr handle, CuBlasOperation opA, void* alpha, SparseMatrixWrapper matA, DenseVectorWrapper vecX, void* beta, DenseVectorWrapper vecY, CudaDataType computeType, SparseMVAlgorithm alg, out long bufferSize);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseSpMV(IntPtr handle, CuBlasOperation opA, void* alpha, SparseMatrixWrapper matA, DenseVectorWrapper vecX, void* beta, DenseVectorWrapper vecY, CudaDataType computeType, SparseMVAlgorithm alg, void* buffer);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseSpMM_bufferSize(IntPtr handle, CuBlasOperation opA, CuBlasOperation opB, void* alpha, SparseMatrixWrapper matA, DenseMatrixWrapper matB, void* beta, DenseMatrixWrapper matC, CudaDataType computeType, SparseMMAlgorithm alg, out long bufferSize);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseSpMM_preprocess(IntPtr handle, CuBlasOperation opA, CuBlasOperation opB, void* alpha, SparseMatrixWrapper matA, DenseMatrixWrapper matB, void* beta, DenseMatrixWrapper matC, CudaDataType computeType, SparseMMAlgorithm alg, void* externalBuffer);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseSpMM(IntPtr handle, CuBlasOperation opA, CuBlasOperation opB, void* alpha, SparseMatrixWrapper matA, DenseMatrixWrapper matB, void* beta, DenseMatrixWrapper matC, CudaDataType computeType, SparseMMAlgorithm alg, void* externalBuffer);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseSpGEMM_createDescr(out IntPtr descr);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseSpGEMM_destroyDescr(IntPtr descr);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseSpGEMM_workEstimation(IntPtr handle, CuBlasOperation opA, CuBlasOperation opB, void* alpha, DenseMatrixWrapper matA, DenseMatrixWrapper matB, void* beta, DenseMatrixWrapper matC, CudaDataType computeType, SparseGemmAlgorithm alg, IntPtr spgemmDescr, out long bufferSize1, void* externalBuffer1 = null);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseSpGEMM_compute(IntPtr handle, CuBlasOperation opA, CuBlasOperation opB, void* alpha, DenseMatrixWrapper matA, DenseMatrixWrapper matB, void* beta, DenseMatrixWrapper matC, CudaDataType computeType, SparseGemmAlgorithm alg, IntPtr spgemmDescr, out long bufferSize2, void* externalBuffer2 = null);

		[DllImport(Cuda.NativeMethods.CUSPARSE_DLL_NAME)]
		internal static extern CudaSparseStatus cusparseSpGEMM_copy(IntPtr handle, CuBlasOperation opA, CuBlasOperation opB, void* alpha, DenseMatrixWrapper matA, DenseMatrixWrapper matB, void* beta, DenseMatrixWrapper matC, CudaDataType computeType, SparseGemmAlgorithm alg, IntPtr spgemmDescr);
		#endregion
	}


	/// <summary>
	/// The static class for custom sparse BLAS native methods
	/// </summary>
	public static unsafe class CustomNativeMethods
	{
		[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern int vecSetValAt(DataType type, void* a, void* value, MklInt* pos, long posN);

		[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern long vecPruneDirect(DataType type, void* a, void* threshold, long n, MklInt* idxOut, void* valOut, bool safe, long nnz);

		[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern long vecPruneBuffer(DataType type, long n);

		[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern long vecPruneNnz(DataType type, void* a, void* threshold, long n, void* buffer);

		[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern int vecPruneCal(DataType type, long n, void* buffer, long nnz, MklInt* indexOut, void* valueOut);

		[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern void spVecIdxToCooIdxs(MklInt* index, MklInt* rowIdx, MklInt* colIdx, long N, long ld);

		[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern void cooIdxsToSpVecIdx(MklInt* index, MklInt* rowIdx, MklInt* colIdx, long N, long ld);

		[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern long vecSpAddBuffer(DataType type, MklInt nnzA, MklInt nnzB);

		[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern long vecSpAddNnz(DataType type, MklInt* indA, void* valA, MklInt nnzA, MklInt* indB, void* valB, MklInt nnzB, void* alpha, void* buffer);

		[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern int vecSpAddCal(DataType type, void* buffer, long nnzAB, long nnzC, MklInt* C_index, void* C_value);

		[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern int spVecOuterCheck(DataType type);

		[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern int spVecOuter(DataType type, void* valA, MklInt* indA, long nnzA, void* valB, MklInt* indB, long nnzB, void* valC, MklInt* rowC, MklInt* colC, bool conj);

		[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern int CooMatKron(DataType type, void* valA, MklInt* rowA, MklInt* colA, long nnzA, void* valB, MklInt* rowB, MklInt* colB, long nnzB, long rowsB, long colsB, void* valC, MklInt* rowC, MklInt* colC);

		[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern int vecSort(DataType type, void* array, long N, int stride);

		[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern int vecSortBy(DataType keyType, DataType valType, void* keys, void* vals, long N, int strideKey, int strideVal);

		[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern int vecFind(DataType type, bool sorted, void* array, long N, int stride, void* toFind, out long index);

		[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern int vecFillRange(DataType type, void* array, long N, int stride, void* start, void* step);

		[DllImport(Cuda.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern int vecBound(DataType type, bool lower, void* array, long N, int stride, void* toFind, out long index);
	}
}