using System;
using System.Runtime.InteropServices;


namespace Althea.SparseBlas.Cuda
{
	/// <summary>
	/// The CUDA Sparse library native methods
	/// </summary>
	public static class NativeMethods
	{
		/// <summary>
		/// The CUDA Sparse library name
		/// </summary>
		public const string CUSPARSE_DLL_NAME = @"cusparse";

		#region initialize and destroy
		/// <summary>
		/// This function initializes the CUDA Sparse library and creates a handle on the CUDA Sparse context. It must be called before any other CUDA Sparse API function is invoked. It allocates hardware resources necessary for accessing the GPU.
		/// </summary>
		/// <param name="handle">The pointer to the handle to the CUDA Sparse context, returned</param>
		/// <returns>See <see cref="Status"/> for the description of the return status</returns>
		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseCreate(ref IntPtr handle);

		/// <summary>
		/// This function releases CPU-side resources used by the CUDA Sparse library. The release of GPU-side resources may be deferred until the application shuts down.
		/// </summary>
		/// <param name="handle">The pointer to the handle to the CUDA Sparse context</param>
		/// <returns>See <see cref="Status"/> for the description of the return status</returns>
		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseDestroy(IntPtr handle);
		#endregion


		#region generic vector and matrix create and destroy

		#region sparse vector
		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseCreateSpVec(ref IntPtr sparseVector, long size, long nnz, IntPtr indices, IntPtr values, IndexType idxType, IndexBase idxBase, CudaDataType valueType);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseDestroySpVec(IntPtr sparseVector);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseSpVecGetValues(IntPtr sparseVector, ref IntPtr values);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseSpVecSetValues(IntPtr sparseVector, IntPtr values);
		#endregion

		#region dense vector
		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseCreateDnVec(ref IntPtr sparseVector, long size, IntPtr values, CudaDataType valueType);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseDestroyDnVec(IntPtr sparseVector);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseDnVecGetValues(IntPtr sparseVector, ref IntPtr values);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseDnVecSetValues(IntPtr sparseVector, IntPtr values);
		#endregion

		#region sparse matrix
		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseCreateCsr(ref IntPtr sparseMatrix, long rows, long cols, long nnz, IntPtr csrRowOffsets, IntPtr csrColInd, IntPtr csrValues, IndexType csrRowOffsetsType, IndexType csrColIndType, IndexBase idxBase, CudaDataType valueType);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseCreateCoo(ref IntPtr sparseMatrix, long rows, long cols, long nnz, IntPtr cooRowInd, IntPtr cooColInd, IntPtr cooValues, IndexType indexType, IndexBase idxBase, CudaDataType valueType);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseDestroySpMat(IntPtr sparseMatrix);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseSpMatGetValues(IntPtr sparseMatrix, ref IntPtr values);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseSpMatSetValues(IntPtr sparseMatrix, IntPtr values);
		#endregion

		#region dense matrix
		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseCreateDnMat(ref IntPtr sparseMatrix, long rows, long cols, long ld, IntPtr values, CudaDataType valueType, Order major);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseDestroyDnMat(IntPtr sparseMatrix);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseDnMatGetValues(IntPtr sparseMatrix, ref IntPtr values);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseDnMatSetValues(IntPtr sparseMatrix, IntPtr values);
		#endregion

		#endregion


		#region generic API
		/// <summary>
		/// The function performs the dot product of a sparse vector <paramref name="opX"/>(<paramref name="vecX"/>) and a dense vector <paramref name="vecY"/>.
		/// </summary>
		/// <param name="handle">CUDA Sparse library handle</param>
		/// <param name="opX"><see cref="MatrixOperation"/> to indicate whether <paramref name="vecX"/> should be conjugate transpose or not</param>
		/// <param name="vecX">sparse vector x</param>
		/// <param name="vecY">dense vector y</param>
		/// <param name="result">output dot result</param>
		/// <param name="computeType">data type of vectors</param>
		/// <param name="externalBuffer">external buffer array</param>
		/// <returns><see cref="Status"/></returns>
		internal delegate Status spdotFunc<T>(IntPtr handle, MatrixOperation opX, SparseVectorWrapper vecX, DenseVectorWrapper vecY, ref T result, CudaDataType computeType, IntPtr externalBuffer);
		#region sparse vector dense vector dot
		/// <summary>
		/// The helper function of <see cref="spdotFunc{T}"/>.
		/// </summary>
		/// <param name="handle">CUDA Sparse library handle</param>
		/// <param name="opX"><see cref="MatrixOperation"/> to indicate whether <paramref name="vecX"/> should be conjugate transpose or not</param>
		/// <param name="vecX">sparse vector x</param>
		/// <param name="vecY">dense vector y</param>
		/// <param name="result">dot result, need not to be set</param>
		/// <param name="computeType">data type of vectors</param>
		/// <param name="bufferSize">output external buffer size in bytes</param>
		/// <returns><see cref="Status"/></returns>
		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseSpVV_bufferSize(IntPtr handle, MatrixOperation opX, SparseVectorWrapper vecX, DenseVectorWrapper vecY, object result, CudaDataType computeType, ref long bufferSize);

		[DllImport(CUSPARSE_DLL_NAME, EntryPoint = "cusparseSpVV")]
		//[NativeMethodBoundary]
		internal static extern Status cusparseSpVVS(IntPtr handle, MatrixOperation opX, SparseVectorWrapper vecX, DenseVectorWrapper vecY, ref float result, CudaDataType computeType, IntPtr externalBuffer);

		[DllImport(CUSPARSE_DLL_NAME, EntryPoint = "cusparseSpVV")]
		//[NativeMethodBoundary]
		internal static extern Status cusparseSpVVD(IntPtr handle, MatrixOperation opX, SparseVectorWrapper vecX, DenseVectorWrapper vecY, ref double result, CudaDataType computeType, IntPtr externalBuffer);

		[DllImport(CUSPARSE_DLL_NAME, EntryPoint = "cusparseSpVV")]
		//[NativeMethodBoundary]
		internal static extern Status cusparseSpVVC(IntPtr handle, MatrixOperation opX, SparseVectorWrapper vecX, DenseVectorWrapper vecY, ref FloatComplex result, CudaDataType computeType, IntPtr externalBuffer);

		[DllImport(CUSPARSE_DLL_NAME, EntryPoint = "cusparseSpVV")]
		//[NativeMethodBoundary]
		internal static extern Status cusparseSpVVZ(IntPtr handle, MatrixOperation opX, SparseVectorWrapper vecX, DenseVectorWrapper vecY, ref DoubleComplex result, CudaDataType computeType, IntPtr externalBuffer);
		#endregion

		#region sparse matrix dense vector multiply
		/// <summary>
		/// The helper function of <see cref="SpMV{T}"/>
		/// </summary>
		/// <param name="handle">CUDA Sparse library handle</param>
		/// <param name="opA"><see cref="MatrixOperation"/> to <paramref name="matA"/></param>
		/// <param name="alpha">scalar to multiply <paramref name="matA"/></param>
		/// <param name="matA">sparse matrix A</param>
		/// <param name="vecX">dense vector x</param>
		/// <param name="beta">scalar to multiply <paramref name="vecY"/></param>
		/// <param name="vecY">dense vector y</param>
		/// <param name="computeType">data type of matrix and vectors</param>
		/// <param name="alg"><see cref="MatrixVectorAlgorithm"/> to use</param>
		/// <param name="bufferSize">output external buffer size in bytes</param>
		/// <returns><see cref="Status"/></returns>
		internal delegate Status SpMVBuf<T>(IntPtr handle, MatrixOperation opA, ref T alpha, SparseMatrixWrapper matA, DenseVectorWrapper vecX, ref T beta, DenseVectorWrapper vecY, CudaDataType computeType, MatrixVectorAlgorithm alg, ref long bufferSize);

		[DllImport(CUSPARSE_DLL_NAME, EntryPoint = "cusparseSpMV_bufferSize")]
		//[NativeMethodBoundary]
		internal static extern Status cusparseSpMV_bufferSizeS(IntPtr handle, MatrixOperation opA, ref float alpha, SparseMatrixWrapper matA, DenseVectorWrapper vecX, ref float beta, DenseVectorWrapper vecY, CudaDataType computeType, MatrixVectorAlgorithm alg, ref long bufferSize);
		[DllImport(CUSPARSE_DLL_NAME, EntryPoint = "cusparseSpMV_bufferSize")]
		//[NativeMethodBoundary]
		internal static extern Status cusparseSpMV_bufferSizeD(IntPtr handle, MatrixOperation opA, ref double alpha, SparseMatrixWrapper matA, DenseVectorWrapper vecX, ref double beta, DenseVectorWrapper vecY, CudaDataType computeType, MatrixVectorAlgorithm alg, ref long bufferSize);
		[DllImport(CUSPARSE_DLL_NAME, EntryPoint = "cusparseSpMV_bufferSize")]
		//[NativeMethodBoundary]
		internal static extern Status cusparseSpMV_bufferSizeC(IntPtr handle, MatrixOperation opA, ref FloatComplex alpha, SparseMatrixWrapper matA, DenseVectorWrapper vecX, ref FloatComplex beta, DenseVectorWrapper vecY, CudaDataType computeType, MatrixVectorAlgorithm alg, ref long bufferSize);
		[DllImport(CUSPARSE_DLL_NAME, EntryPoint = "cusparseSpMV_bufferSize")]
		//[NativeMethodBoundary]
		internal static extern Status cusparseSpMV_bufferSizeZ(IntPtr handle, MatrixOperation opA, ref DoubleComplex alpha, SparseMatrixWrapper matA, DenseVectorWrapper vecX, ref DoubleComplex beta, DenseVectorWrapper vecY, CudaDataType computeType, MatrixVectorAlgorithm alg, ref long bufferSize);

		/// <summary>
		/// This function performs the multiplication of a sparse matrix <paramref name="opA"/>(<paramref name="matA"/>) and a dense vector <paramref name="vecX"/>, result in <paramref name="vecY"/>.
		/// </summary>
		/// <param name="handle">CUDA Sparse library handle</param>
		/// <param name="opA"><see cref="MatrixOperation"/> to <paramref name="matA"/></param>
		/// <param name="alpha">scalar to multiply <paramref name="matA"/></param>
		/// <param name="matA">sparse matrix A</param>
		/// <param name="vecX">dense vector x</param>
		/// <param name="beta">scalar to multiply <paramref name="vecY"/></param>
		/// <param name="vecY">dense vector y</param>
		/// <param name="computeType">data type of matrix and vectors</param>
		/// <param name="alg"><see cref="MatrixVectorAlgorithm"/> to use</param>
		/// <param name="externalBuffer">external buffer array</param>
		/// <returns><see cref="Status"/></returns>
		internal delegate Status SpMV<T>(IntPtr handle, MatrixOperation opA, ref T alpha, SparseMatrixWrapper matA, DenseVectorWrapper vecX, ref T beta, DenseVectorWrapper vecY, CudaDataType computeType, MatrixVectorAlgorithm alg, IntPtr externalBuffer);

		[DllImport(CUSPARSE_DLL_NAME, EntryPoint = "cusparseSpMV")]
		//[NativeMethodBoundary]
		internal static extern Status cusparseSpMVS(IntPtr handle, MatrixOperation opA, ref float alpha, SparseMatrixWrapper matA, DenseVectorWrapper vecX, ref float beta, DenseVectorWrapper vecY, CudaDataType computeType, MatrixVectorAlgorithm alg, IntPtr externalBuffer);
		[DllImport(CUSPARSE_DLL_NAME, EntryPoint = "cusparseSpMV")]
		//[NativeMethodBoundary]
		internal static extern Status cusparseSpMVD(IntPtr handle, MatrixOperation opA, ref double alpha, SparseMatrixWrapper matA, DenseVectorWrapper vecX, ref double beta, DenseVectorWrapper vecY, CudaDataType computeType, MatrixVectorAlgorithm alg, IntPtr externalBuffer);
		[DllImport(CUSPARSE_DLL_NAME, EntryPoint = "cusparseSpMV")]
		//[NativeMethodBoundary]
		internal static extern Status cusparseSpMVC(IntPtr handle, MatrixOperation opA, ref FloatComplex alpha, SparseMatrixWrapper matA, DenseVectorWrapper vecX, ref FloatComplex beta, DenseVectorWrapper vecY, CudaDataType computeType, MatrixVectorAlgorithm alg, IntPtr externalBuffer);
		[DllImport(CUSPARSE_DLL_NAME, EntryPoint = "cusparseSpMV")]
		//[NativeMethodBoundary]
		internal static extern Status cusparseSpMVZ(IntPtr handle, MatrixOperation opA, ref DoubleComplex alpha, SparseMatrixWrapper matA, DenseVectorWrapper vecX, ref DoubleComplex beta, DenseVectorWrapper vecY, CudaDataType computeType, MatrixVectorAlgorithm alg, IntPtr externalBuffer);

		/*
		[DllImport(CUSPARSE_DLL_NAME, EntryPoint = "cusparseSpMV")]
		//[NativeMethodBoundary]
		internal static extern Status cusparseSpMVS(IntPtr handle, MatrixOperation opA, ref float alpha, SparseMatrixWrapper matA, DenseVectorWrapper vecX, ref float beta, DenseVectorWrapper vecY, CudaDataType computeType, MatrixVectorAlgorithm alg, IntPtr externalBuffer);

		[DllImport(CUSPARSE_DLL_NAME, EntryPoint = "cusparseSpMV")]
		//[NativeMethodBoundary]
		internal static extern Status cusparseSpMVD(IntPtr handle, MatrixOperation opA, ref double alpha, SparseMatrixWrapper matA, DenseVectorWrapper vecX, ref double beta, DenseVectorWrapper vecY, CudaDataType computeType, MatrixVectorAlgorithm alg, IntPtr externalBuffer);

		[DllImport(CUSPARSE_DLL_NAME, EntryPoint = "cusparseSpMV")]
		//[NativeMethodBoundary]
		internal static extern Status cusparseSpMVC(IntPtr handle, MatrixOperation opA, ref FloatComplex alpha, SparseMatrixWrapper matA, DenseVectorWrapper vecX, ref FloatComplex beta, DenseVectorWrapper vecY, CudaDataType computeType, MatrixVectorAlgorithm alg, IntPtr externalBuffer);

		[DllImport(CUSPARSE_DLL_NAME, EntryPoint = "cusparseSpMV")]
		//[NativeMethodBoundary]
		internal static extern Status cusparseSpMVZ(IntPtr handle, MatrixOperation opA, ref DoubleComplex alpha, SparseMatrixWrapper matA, DenseVectorWrapper vecX, ref DoubleComplex beta, DenseVectorWrapper vecY, CudaDataType computeType, MatrixVectorAlgorithm alg, IntPtr externalBuffer);
		*/
		#endregion

		#region sparse matrix dense matrix multiply
		/// <summary>
		/// The helper function of <see cref="SpMM{T}"/>.
		/// </summary>
		/// <param name="handle">CUDA Sparse library handle</param>
		/// <param name="opA"><see cref="MatrixOperation"/> to <paramref name="matA"/></param>
		/// <param name="opB"><see cref="MatrixOperation"/> to <paramref name="matB"/></param>
		/// <param name="alpha">scalar to multiply <paramref name="matA"/></param>
		/// <param name="matA">sparse matrix A</param>
		/// <param name="matB">dense matrix B</param>
		/// <param name="beta">scalar to multiply <paramref name="matC"/></param>
		/// <param name="matC">dense matrix C</param>
		/// <param name="computeType">data type of matrices</param>
		/// <param name="alg"><see cref="MatrixMatrixAlgorithm"/> to use</param>
		/// <param name="bufferSize">output buffer size in bytes</param>
		/// <returns><see cref="Status"/></returns>
		internal delegate Status SpMMBuf<T>(IntPtr handle, MatrixOperation opA, MatrixOperation opB, ref T alpha, SparseMatrixWrapper matA, DenseMatrixWrapper matB, ref T beta, DenseMatrixWrapper matC, CudaDataType computeType, MatrixMatrixAlgorithm alg, ref long bufferSize);

		[DllImport(CUSPARSE_DLL_NAME, EntryPoint = "cusparseSpMM_bufferSize")]
		//[NativeMethodBoundary]
		internal static extern Status cusparseSpMM_bufferSizeS(IntPtr handle, MatrixOperation opA, MatrixOperation opB, ref float alpha, SparseMatrixWrapper matA, DenseMatrixWrapper matB, ref float beta, DenseMatrixWrapper matC, CudaDataType computeType, MatrixMatrixAlgorithm alg, ref long bufferSize);
		[DllImport(CUSPARSE_DLL_NAME, EntryPoint = "cusparseSpMM_bufferSize")]
		//[NativeMethodBoundary]
		internal static extern Status cusparseSpMM_bufferSizeD(IntPtr handle, MatrixOperation opA, MatrixOperation opB, ref double alpha, SparseMatrixWrapper matA, DenseMatrixWrapper matB, ref double beta, DenseMatrixWrapper matC, CudaDataType computeType, MatrixMatrixAlgorithm alg, ref long bufferSize);
		[DllImport(CUSPARSE_DLL_NAME, EntryPoint = "cusparseSpMM_bufferSize")]
		//[NativeMethodBoundary]
		internal static extern Status cusparseSpMM_bufferSizeC(IntPtr handle, MatrixOperation opA, MatrixOperation opB, ref FloatComplex alpha, SparseMatrixWrapper matA, DenseMatrixWrapper matB, ref FloatComplex beta, DenseMatrixWrapper matC, CudaDataType computeType, MatrixMatrixAlgorithm alg, ref long bufferSize);
		[DllImport(CUSPARSE_DLL_NAME, EntryPoint = "cusparseSpMM_bufferSize")]
		//[NativeMethodBoundary]
		internal static extern Status cusparseSpMM_bufferSizeZ(IntPtr handle, MatrixOperation opA, MatrixOperation opB, ref DoubleComplex alpha, SparseMatrixWrapper matA, DenseMatrixWrapper matB, ref DoubleComplex beta, DenseMatrixWrapper matC, CudaDataType computeType, MatrixMatrixAlgorithm alg, ref long bufferSize);

		/// <summary>
		/// This function performs the multiplication of a sparse matrix <paramref name="opA"/>(<paramref name="matA"/>) and a dense matrix <paramref name="opB"/>(<paramref name="matB"/>), result in <paramref name="matC"/>.
		/// </summary>
		/// <param name="handle">CUDA Sparse library handle</param>
		/// <param name="opA"><see cref="MatrixOperation"/> to <paramref name="matA"/></param>
		/// <param name="opB"><see cref="MatrixOperation"/> to <paramref name="matB"/></param>
		/// <param name="alpha">scalar to multiply <paramref name="matA"/></param>
		/// <param name="matA">sparse matrix A</param>
		/// <param name="matB">dense matrix B</param>
		/// <param name="beta">scalar to multiply <paramref name="matC"/></param>
		/// <param name="matC">dense matrix C</param>
		/// <param name="computeType">data type of matrices</param>
		/// <param name="alg"><see cref="MatrixMatrixAlgorithm"/> to use</param>
		/// <param name="externalBuffer">external buffer array</param>
		/// <returns><see cref="Status"/></returns>
		internal delegate Status SpMM<T>(IntPtr handle, MatrixOperation opA, MatrixOperation opB, ref T alpha, SparseMatrixWrapper matA, DenseMatrixWrapper matB, ref T beta, DenseMatrixWrapper matC, CudaDataType computeType, MatrixMatrixAlgorithm alg, IntPtr externalBuffer);

		[DllImport(CUSPARSE_DLL_NAME, EntryPoint = "cusparseSpMM")]
		//[NativeMethodBoundary]
		internal static extern Status cusparseSpMMS(IntPtr handle, MatrixOperation opA, MatrixOperation opB, ref float alpha, SparseMatrixWrapper matA, DenseMatrixWrapper matB, ref float beta, DenseMatrixWrapper matC, CudaDataType computeType, MatrixMatrixAlgorithm alg, IntPtr externalBuffer);
		[DllImport(CUSPARSE_DLL_NAME, EntryPoint = "cusparseSpMM")]
		//[NativeMethodBoundary]
		internal static extern Status cusparseSpMMD(IntPtr handle, MatrixOperation opA, MatrixOperation opB, ref double alpha, SparseMatrixWrapper matA, DenseMatrixWrapper matB, ref double beta, DenseMatrixWrapper matC, CudaDataType computeType, MatrixMatrixAlgorithm alg, IntPtr externalBuffer);
		[DllImport(CUSPARSE_DLL_NAME, EntryPoint = "cusparseSpMM")]
		//[NativeMethodBoundary]
		internal static extern Status cusparseSpMMC(IntPtr handle, MatrixOperation opA, MatrixOperation opB, ref FloatComplex alpha, SparseMatrixWrapper matA, DenseMatrixWrapper matB, ref FloatComplex beta, DenseMatrixWrapper matC, CudaDataType computeType, MatrixMatrixAlgorithm alg, IntPtr externalBuffer);
		[DllImport(CUSPARSE_DLL_NAME, EntryPoint = "cusparseSpMM")]
		//[NativeMethodBoundary]
		internal static extern Status cusparseSpMMZ(IntPtr handle, MatrixOperation opA, MatrixOperation opB, ref DoubleComplex alpha, SparseMatrixWrapper matA, DenseMatrixWrapper matB, ref DoubleComplex beta, DenseMatrixWrapper matC, CudaDataType computeType, MatrixMatrixAlgorithm alg, IntPtr externalBuffer);
		/*
		[DllImport(CUSPARSE_DLL_NAME, EntryPoint = "cusparseSpMM")]
		//[NativeMethodBoundary]
		internal static extern Status cusparseSpMMS(IntPtr handle, MatrixOperation opA, MatrixOperation opB, ref float alpha, SparseMatrixWrapper matA, DenseMatrixWrapper matB, ref float beta, DenseMatrixWrapper matC, CudaDataType computeType, MatrixMatrixAlgorithm alg, IntPtr externalBuffer);

		[DllImport(CUSPARSE_DLL_NAME, EntryPoint = "cusparseSpMM")]
		//[NativeMethodBoundary]
		internal static extern Status cusparseSpMMD(IntPtr handle, MatrixOperation opA, MatrixOperation opB, ref double alpha, SparseMatrixWrapper matA, DenseMatrixWrapper matB, ref double beta, DenseMatrixWrapper matC, CudaDataType computeType, MatrixMatrixAlgorithm alg, IntPtr externalBuffer);

		[DllImport(CUSPARSE_DLL_NAME, EntryPoint = "cusparseSpMM")]
		//[NativeMethodBoundary]
		internal static extern Status cusparseSpMMC(IntPtr handle, MatrixOperation opA, MatrixOperation opB, ref FloatComplex alpha, SparseMatrixWrapper matA, DenseMatrixWrapper matB, ref FloatComplex beta, DenseMatrixWrapper matC, CudaDataType computeType, MatrixMatrixAlgorithm alg, IntPtr externalBuffer);

		[DllImport(CUSPARSE_DLL_NAME, EntryPoint = "cusparseSpMM")]
		//[NativeMethodBoundary]
		internal static extern Status cusparseSpMMZ(IntPtr handle, MatrixOperation opA, MatrixOperation opB, ref DoubleComplex alpha, SparseMatrixWrapper matA, DenseMatrixWrapper matB, ref DoubleComplex beta, DenseMatrixWrapper matC, CudaDataType computeType, MatrixMatrixAlgorithm alg, IntPtr externalBuffer);
		*/
		#endregion

		#endregion


		#region normal API

		#region sparse vector dense vector dot old (remove in next version)
		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseSdoti(IntPtr handle, int nnz, [In] IntPtr xVal, [In] IntPtr xInd, [In] IntPtr y, ref float result, IndexBase idxBase);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseDdoti(IntPtr handle, int nnz, [In] IntPtr xVal, [In] IntPtr xInd, [In] IntPtr y, ref double result, IndexBase idxBase);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseCdoti(IntPtr handle, int nnz, [In] IntPtr xVal, [In] IntPtr xInd, [In] IntPtr y, ref FloatComplex result, IndexBase idxBase);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseZdoti(IntPtr handle, int nnz, [In] IntPtr xVal, [In] IntPtr xInd, [In] IntPtr y, ref DoubleComplex result, IndexBase idxBase);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseCdotci(IntPtr handle, int nnz, [In] IntPtr xVal, [In] IntPtr xInd, [In] IntPtr y, ref FloatComplex result, IndexBase idxBase);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseZdotci(IntPtr handle, int nnz, [In] IntPtr xVal, [In] IntPtr xInd, [In] IntPtr y, ref DoubleComplex result, IndexBase idxBase);

		internal delegate Status dotFunc<T>(IntPtr handle, int nnz, [In] IntPtr xVal, [In] IntPtr xInd, [In] IntPtr y, ref T result, IndexBase idxBase);
		#endregion

		#region sparse matrix dense vector multiply old (remove in next version)
		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseScsrmv(IntPtr handle, MatrixOperation opA, int m, int n, int nnz, ref float alpha, SparseMatrixDescription descrA, [In] IntPtr csrValA, [In] IntPtr csrRowPtrA, [In] IntPtr csrColIndA, [In] IntPtr x, ref float beta, IntPtr y);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseDcsrmv(IntPtr handle, MatrixOperation opA, int m, int n, int nnz, ref double alpha, SparseMatrixDescription descrA, [In] IntPtr csrValA, [In] IntPtr csrRowPtrA, [In] IntPtr csrColIndA, [In] IntPtr x, ref double beta, IntPtr y);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]]
		internal static extern Status cusparseCcsrmv(IntPtr handle, MatrixOperation opA, int m, int n, int nnz, ref FloatComplex alpha, SparseMatrixDescription descrA, [In] IntPtr csrValA, [In] IntPtr csrRowPtrA, [In] IntPtr csrColIndA, [In] IntPtr x, ref FloatComplex beta, IntPtr y);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseZcsrmv(IntPtr handle, MatrixOperation opA, int m, int n, int nnz, ref DoubleComplex alpha, SparseMatrixDescription descrA, [In] IntPtr csrValA, [In] IntPtr csrRowPtrA, [In] IntPtr csrColIndA, [In] IntPtr x, ref DoubleComplex beta, IntPtr y);

		internal delegate Status csrmvFunc<T>(IntPtr handle, MatrixOperation opA, int m, int n, int nnz, ref T alpha, SparseMatrixDescription descrA, [In] IntPtr csrValA, [In] IntPtr csrRowPtrA, [In] IntPtr csrColIndA, [In] IntPtr x, ref T beta, IntPtr y);
		#endregion

		#region sparse matrix dense matrix multiply old (remove in next version)
		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseScsrmm2(IntPtr handle, MatrixOperation opA, MatrixOperation transB, int m, int n, int k, int nnz, ref float alpha, SparseMatrixDescription descrA, [In] IntPtr csrValA, [In] IntPtr csrRowPtrA, [In] IntPtr csrColIndA, [In] IntPtr B, int ldb, ref float beta, IntPtr C, int ldc);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseDcsrmm2(IntPtr handle, MatrixOperation opA, MatrixOperation transB, int m, int n, int k, int nnz, ref double alpha, SparseMatrixDescription descrA, [In] IntPtr csrValA, [In] IntPtr csrRowPtrA, [In] IntPtr csrColIndA, [In] IntPtr B, int ldb, ref double beta, IntPtr C, int ldc);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseCcsrmm2(IntPtr handle, MatrixOperation opA, MatrixOperation transB, int m, int n, int k, int nnz, ref FloatComplex alpha, SparseMatrixDescription descrA, [In] IntPtr csrValA, [In] IntPtr csrRowPtrA, [In] IntPtr csrColIndA, [In] IntPtr B, int ldb, ref FloatComplex beta, IntPtr C, int ldc);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseZcsrmm2(IntPtr handle, MatrixOperation opA, MatrixOperation transB, int m, int n, int k, int nnz, ref DoubleComplex alpha, SparseMatrixDescription descrA, [In] IntPtr csrValA, [In] IntPtr csrRowPtrA, [In] IntPtr csrColIndA, [In] IntPtr B, int ldb, ref DoubleComplex beta, IntPtr C, int ldc);

		internal delegate Status csrmmFunc<T>(IntPtr handle, MatrixOperation opA, MatrixOperation transB, int m, int n, int k, int nnz, ref T alpha, SparseMatrixDescription descrA, [In] IntPtr csrValA, [In] IntPtr csrRowPtrA, [In] IntPtr csrColIndA, [In] IntPtr B, int ldb, ref T beta, IntPtr C, int ldc);
		#endregion

		/// <summary>
		/// This function multiplies the vector x in sparse format by the constant and adds the result to the vector <paramref name="y"/> in dense format.
		/// </summary>
		/// <param name="handle">CUDA Sparse library handle</param>
		/// <param name="nnz">number of non-zeros of vector x</param>
		/// <param name="alpha">scalar to multiply</param>
		/// <param name="xVal">value array of vector x</param>
		/// <param name="xInd">index array of vector x</param>
		/// <param name="y">dense vector y</param>
		/// <param name="idxBase"><see cref="IndexBase"/></param>
		/// <returns><see cref="Status"/></returns>
		internal delegate Status spaxpyFunc<T>(IntPtr handle, int nnz, ref T alpha, IntPtr xVal, IntPtr xInd, IntPtr y, IndexBase idxBase);
		#region dense vector add sparse vector
		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseSaxpyi(IntPtr handle, int nnz, ref float alpha, IntPtr xVal, IntPtr xInd, IntPtr y, IndexBase idxBase);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseDaxpyi(IntPtr handle, int nnz, ref double alpha, IntPtr xVal, IntPtr xInd, IntPtr y, IndexBase idxBase);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseCaxpyi(IntPtr handle, int nnz, ref FloatComplex alpha, IntPtr xVal, IntPtr xInd, IntPtr y, IndexBase idxBase);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseZaxpyi(IntPtr handle, int nnz, ref DoubleComplex alpha, IntPtr xVal, IntPtr xInd, IntPtr y, IndexBase idxBase);
		#endregion

		/// <summary>
		/// The helper function of <see cref="gemviFunc{T}"/>
		/// </summary>
		/// <param name="handle">CUDA Sparse library handle</param>
		/// <param name="opA">the operation to matrix</param>
		/// <param name="m">number of rows of matrix</param>
		/// <param name="n">number of columns of matrix</param>
		/// <param name="nnz">number of non-zeros of vector</param>
		/// <param name="pBufferSize">output buffer size in bytes</param>
		/// <returns><see cref="Status"/></returns>
		internal delegate Status gemviBufFunc(IntPtr handle, MatrixOperation opA, int m, int n, int nnz, ref int pBufferSize);

		/// <summary>
		/// This function performs the dense matrix <paramref name="opA"/>(<paramref name="A"/>) and sparse vector <paramref name="x"/> operation, result saved in <paramref name="y"/>.
		/// </summary>
		/// <param name="handle">CUDA Sparse library handle</param>
		/// <param name="opA">the operation to matrix</param>
		/// <param name="m">number of rows of matrix</param>
		/// <param name="n">number of columns of matrix</param>
		/// <param name="alpha">scalar to multiply matrix <paramref name="A"/></param>
		/// <param name="A">dense matrix A</param>
		/// <param name="lda">leading dimension of <paramref name="A"/></param>
		/// <param name="nnz">number of non-zeros of vector</param>
		/// <param name="x">value array of x</param>
		/// <param name="xInd">index array of x</param>
		/// <param name="beta">scalar to multiply <paramref name="y"/></param>
		/// <param name="y">dense vector y</param>
		/// <param name="idxBase"><see cref="IndexBase"/></param>
		/// <param name="pBuffer">external buffer array</param>
		/// <returns><see cref="Status"/></returns>
		internal delegate Status gemviFunc<T>(IntPtr handle, MatrixOperation opA, int m, int n, ref T alpha, [In] IntPtr A, int lda, int nnz, [In] IntPtr x, [In] IntPtr xInd, ref T beta, IntPtr y, IndexBase idxBase, IntPtr pBuffer);
		#region dense matrix sparse vector multiply (only for general matrix)
		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseSgemvi_bufferSize(IntPtr handle, MatrixOperation opA, int m, int n, int nnz, ref int pBufferSize);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseDgemvi_bufferSize(IntPtr handle, MatrixOperation opA, int m, int n, int nnz, ref int pBufferSize);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseCgemvi_bufferSize(IntPtr handle, MatrixOperation opA, int m, int n, int nnz, ref int pBufferSize);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseZgemvi_bufferSize(IntPtr handle, MatrixOperation opA, int m, int n, int nnz, ref int pBufferSize);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseSgemvi(IntPtr handle, MatrixOperation opA, int m, int n, ref float alpha, [In] IntPtr A, int lda, int nnz, [In] IntPtr x, [In] IntPtr xInd, ref float beta, IntPtr y, IndexBase idxBase, IntPtr pBuffer);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseDgemvi(IntPtr handle, MatrixOperation opA, int m, int n, ref double alpha, [In] IntPtr A, int lda, int nnz, [In] IntPtr x, [In] IntPtr xInd, ref double beta, IntPtr y, IndexBase idxBase, IntPtr pBuffer);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseCgemvi(IntPtr handle, MatrixOperation opA, int m, int n, ref FloatComplex alpha, [In] IntPtr A, int lda, int nnz, IntPtr x, IntPtr xInd, ref FloatComplex beta, IntPtr y, IndexBase idxBase, IntPtr pBuffer);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseZgemvi(IntPtr handle, MatrixOperation opA, int m, int n, ref DoubleComplex alpha, [In] IntPtr A, int lda, int nnz, [In] IntPtr x, [In] IntPtr xInd, ref DoubleComplex beta, IntPtr y, IndexBase idxBase, IntPtr pBuffer);
		#endregion

		/// <summary>
		/// This function performs the multiplication of dense matrix <paramref name="A"/> and sparse CSC matrix <c>B</c>, result saved in <paramref name="C"/>.
		/// </summary>
		/// <param name="handle">CUDA Sparse library handle</param>
		/// <param name="m">number of rows of matrix <paramref name="A"/></param>
		/// <param name="n">number of columns of matrix <c>B</c> and <paramref name="C"/></param>
		/// <param name="k">number of columns of matrix <paramref name="A"/></param>
		/// <param name="nnz">number of non-zeros of matrix <c>B</c></param>
		/// <param name="alpha">scalar to multiply matrix <paramref name="A"/></param>
		/// <param name="A">dense matrix A</param>
		/// <param name="lda">leading dimension of <paramref name="A"/></param>
		/// <param name="cscValB">value array of matrix B</param>
		/// <param name="cscColPtrB">column pointer array of matrix B</param>
		/// <param name="cscRowIndB">row index array of matrix B</param>
		/// <param name="beta">scalar to multiply <paramref name="C"/></param>
		/// <param name="C">dense matrix C</param>
		/// <param name="ldc">leading dimension of <paramref name="C"/></param>
		/// <returns><see cref="Status"/></returns>
		internal delegate Status gemmiFunc<T>(IntPtr handle, int m, int n, int k, int nnz, ref T alpha, [In] IntPtr A, int lda, [In] IntPtr cscValB, [In] IntPtr cscColPtrB, [In] IntPtr cscRowIndB, ref T beta, IntPtr C, int ldc);
		#region dense matrix sparse matrix multiply (only for general matrix)
		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseSgemmi(IntPtr handle, int m, int n, int k, int nnz, ref float alpha, [In] IntPtr A, int lda, [In] IntPtr cscValB, [In] IntPtr cscColPtrB, [In] IntPtr cscRowIndB, ref float beta, IntPtr C, int ldc);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseDgemmi(IntPtr handle, int m, int n, int k, int nnz, ref double alpha, [In] IntPtr A, int lda, [In] IntPtr cscValB, [In] IntPtr cscColPtrB, [In] IntPtr cscRowIndB, ref double beta, IntPtr C, int ldc);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseCgemmi(IntPtr handle, int m, int n, int k, int nnz, ref FloatComplex alpha, [In] IntPtr A, int lda, [In] IntPtr cscValB, [In] IntPtr cscColPtrB, [In] IntPtr cscRowIndB, ref FloatComplex beta, IntPtr C, int ldc);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseZgemmi(IntPtr handle, int m, int n, int k, int nnz, ref DoubleComplex alpha, [In] IntPtr A, int lda, [In] IntPtr cscValB, [In] IntPtr cscColPtrB, [In] IntPtr cscRowIndB, ref DoubleComplex beta, IntPtr C, int ldc);
		#endregion

		/// <summary>
		/// The helper function of <see cref="geamFunc{T}"/>.
		/// </summary>
		/// <param name="handle">CUDA Sparse library handle</param>
		/// <param name="m">number of rows of matrix <c>A</c></param>
		/// <param name="n">number of columns of matrix <c>B</c></param>
		/// <param name="alpha">scalar to multiply <c>A</c></param>
		/// <param name="descrA"><see cref="SparseMatrixDescription"/> of matrix A</param>
		/// <param name="nnzA">number of non-zeros of matrix A</param>
		/// <param name="csrSortedValA">value array of matrix A</param>
		/// <param name="csrSortedRowPtrA">row pointer array of matrix A</param>
		/// <param name="csrSortedColIndA">column index array of matrix A</param>
		/// <param name="beta">scalar to multiply <c>B</c></param>
		/// <param name="descrB"><see cref="SparseMatrixDescription"/> of matrix B</param>
		/// <param name="nnzB">number of non-zeros of matrix B</param>
		/// <param name="csrSortedValB">value array of matrix B</param>
		/// <param name="csrSortedRowPtrB">row pointer array of matrix B</param>
		/// <param name="csrSortedColIndB">column index array of matrix B</param>
		/// <param name="descrC"><see cref="SparseMatrixDescription"/> of matrix C</param>
		/// <param name="csrSortedValC">value array of matrix C</param>
		/// <param name="csrSortedRowPtrC">row pointer array of matrix C</param>
		/// <param name="csrSortedColIndC">column index array of matrix C</param>
		/// <param name="pBufferSizeInBytes">output buffer size in bytes</param>
		/// <returns><see cref="Status"/></returns>
		internal delegate Status geamBufFunc<T>(IntPtr handle, int m, int n, ref T alpha, SparseMatrixDescription descrA, int nnzA, [In] IntPtr csrSortedValA, [In] IntPtr csrSortedRowPtrA, [In] IntPtr csrSortedColIndA, ref T beta, SparseMatrixDescription descrB, int nnzB, [In] IntPtr csrSortedValB, [In] IntPtr csrSortedRowPtrB, [In] IntPtr csrSortedColIndB, SparseMatrixDescription descrC, [In] IntPtr csrSortedValC, [In] IntPtr csrSortedRowPtrC, [In] IntPtr csrSortedColIndC, ref long pBufferSizeInBytes);

		/// <summary>
		/// This function performs spare matrix <c>A</c> plus sparse matrix <c>B</c>.
		/// </summary>
		/// <param name="handle">CUDA Sparse library handle</param>
		/// <param name="m">number of rows of matrix <c>A</c></param>
		/// <param name="n">number of columns of matrix <c>B</c></param>
		/// <param name="alpha">scalar to multiply <c>A</c></param>
		/// <param name="descrA"><see cref="SparseMatrixDescription"/> of matrix A</param>
		/// <param name="nnzA">number of non-zeros of matrix A</param>
		/// <param name="csrSortedValA">value array of matrix A</param>
		/// <param name="csrSortedRowPtrA">row pointer array of matrix A</param>
		/// <param name="csrSortedColIndA">column index array of matrix A</param>
		/// <param name="beta">scalar to multiply <c>B</c></param>
		/// <param name="descrB"><see cref="SparseMatrixDescription"/> of matrix B</param>
		/// <param name="nnzB">number of non-zeros of matrix B</param>
		/// <param name="csrSortedValB">value array of matrix B</param>
		/// <param name="csrSortedRowPtrB">row pointer array of matrix B</param>
		/// <param name="csrSortedColIndB">column index array of matrix B</param>
		/// <param name="descrC"><see cref="SparseMatrixDescription"/> of matrix C</param>
		/// <param name="csrSortedValC">value array of matrix C</param>
		/// <param name="csrSortedRowPtrC">row pointer array of matrix C</param>
		/// <param name="csrSortedColIndC">column index array of matrix C</param>
		/// <param name="buffer">external buffer array</param>
		/// <returns><see cref="Status"/></returns>
		internal delegate Status geamFunc<T>(IntPtr handle, int m, int n, ref T alpha,
			SparseMatrixDescription descrA, int nnzA, [In] IntPtr csrSortedValA, [In] IntPtr csrSortedRowPtrA, [In] IntPtr csrSortedColIndA, ref T beta,
			SparseMatrixDescription descrB, int nnzB, [In] IntPtr csrSortedValB, [In] IntPtr csrSortedRowPtrB, [In] IntPtr csrSortedColIndB,
			SparseMatrixDescription descrC, IntPtr csrSortedValC, IntPtr csrSortedRowPtrC, IntPtr csrSortedColIndC, IntPtr buffer);
		#region sparse matrices add (only for general matrices)
		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseScsrgeam2_bufferSizeExt(IntPtr handle, int m, int n, ref float alpha,
			SparseMatrixDescription descrA, int nnzA, [In] IntPtr csrSortedValA, [In] IntPtr csrSortedRowPtrA, [In] IntPtr csrSortedColIndA,
			ref float beta,
			SparseMatrixDescription descrB, int nnzB, [In] IntPtr csrSortedValB, [In] IntPtr csrSortedRowPtrB, [In] IntPtr csrSortedColIndB,
			SparseMatrixDescription descrC, [In] IntPtr csrSortedValC, [In] IntPtr csrSortedRowPtrC, [In] IntPtr csrSortedColIndC,
			ref long pBufferSizeInBytes);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseDcsrgeam2_bufferSizeExt(IntPtr handle, int m, int n, ref double alpha,
			SparseMatrixDescription descrA, int nnzA, [In] IntPtr csrSortedValA, [In] IntPtr csrSortedRowPtrA, [In] IntPtr csrSortedColIndA,
			ref double beta,
			SparseMatrixDescription descrB, int nnzB, [In] IntPtr csrSortedValB, [In] IntPtr csrSortedRowPtrB, [In] IntPtr csrSortedColIndB,
			SparseMatrixDescription descrC, [In] IntPtr csrSortedValC, [In] IntPtr csrSortedRowPtrC, [In] IntPtr csrSortedColIndC,
			ref long pBufferSizeInBytes);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseCcsrgeam2_bufferSizeExt(IntPtr handle, int m, int n, ref FloatComplex alpha,
			SparseMatrixDescription descrA, int nnzA, [In] IntPtr csrSortedValA, [In] IntPtr csrSortedRowPtrA, [In] IntPtr csrSortedColIndA,
			ref FloatComplex beta,
			SparseMatrixDescription descrB, int nnzB, [In] IntPtr csrSortedValB, [In] IntPtr csrSortedRowPtrB, [In] IntPtr csrSortedColIndB,
			SparseMatrixDescription descrC, [In] IntPtr csrSortedValC, [In] IntPtr csrSortedRowPtrC, [In] IntPtr csrSortedColIndC,
			ref long pBufferSizeInBytes);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseZcsrgeam2_bufferSizeExt(IntPtr handle, int m, int n, ref DoubleComplex alpha,
			SparseMatrixDescription descrA, int nnzA, [In] IntPtr csrSortedValA, [In] IntPtr csrSortedRowPtrA, [In] IntPtr csrSortedColIndA, ref DoubleComplex beta,
			SparseMatrixDescription descrB, int nnzB, [In] IntPtr csrSortedValB, [In] IntPtr csrSortedRowPtrB, [In] IntPtr csrSortedColIndB,
			SparseMatrixDescription descrC, [In] IntPtr csrSortedValC, [In] IntPtr csrSortedRowPtrC, [In] IntPtr csrSortedColIndC, ref long pBufferSizeInBytes);

		/// <summary>
		/// The number of non-zero calculation function of <see cref="geamFunc{T}"/>.
		/// </summary>
		/// <param name="handle">CUDA Sparse library handle</param>
		/// <param name="m">number of rows of matrix <c>A</c></param>
		/// <param name="n">number of columns of matrix <c>B</c></param>
		/// <param name="descrA"><see cref="SparseMatrixDescription"/> of matrix A</param>
		/// <param name="nnzA">number of non-zeros of matrix A</param>
		/// <param name="csrSortedRowPtrA">row pointer array of matrix A</param>
		/// <param name="csrSortedColIndA">column index array of matrix A</param>
		/// <param name="descrB"><see cref="SparseMatrixDescription"/> of matrix B</param>
		/// <param name="nnzB">number of non-zeros of matrix B</param>
		/// <param name="csrSortedRowPtrB">row pointer array of matrix B</param>
		/// <param name="csrSortedColIndB">column index array of matrix B</param>
		/// <param name="descrC"><see cref="SparseMatrixDescription"/> of matrix C</param>
		/// <param name="csrSortedRowPtrC">output row pointer array of matrix C</param>
		/// <param name="nnzC">output total number of non-zeros of matrix C</param>
		/// <param name="buffer">external buffer array</param>
		/// <returns><see cref="Status"/></returns>
		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseXcsrgeam2Nnz(IntPtr handle, int m, int n,
			SparseMatrixDescription descrA, int nnzA, [In] IntPtr csrSortedRowPtrA, [In] IntPtr csrSortedColIndA,
			SparseMatrixDescription descrB, int nnzB, [In] IntPtr csrSortedRowPtrB, [In] IntPtr csrSortedColIndB,
			SparseMatrixDescription descrC, IntPtr csrSortedRowPtrC, ref int nnzC, IntPtr buffer);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseScsrgeam2(IntPtr handle, int m, int n, ref float alpha,
			SparseMatrixDescription descrA, int nnzA, [In] IntPtr csrSortedValA, [In] IntPtr csrSortedRowPtrA, [In] IntPtr csrSortedColIndA, ref float beta,
			SparseMatrixDescription descrB, int nnzB, [In] IntPtr csrSortedValB, [In] IntPtr csrSortedRowPtrB, [In] IntPtr csrSortedColIndB,
			SparseMatrixDescription descrC, IntPtr csrSortedValC, IntPtr csrSortedRowPtrC, IntPtr csrSortedColIndC, IntPtr buffer);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseDcsrgeam2(IntPtr handle, int m, int n, ref double alpha,
			SparseMatrixDescription descrA, int nnzA, [In] IntPtr csrSortedValA, [In] IntPtr csrSortedRowPtrA, [In] IntPtr csrSortedColIndA, ref double beta,
			SparseMatrixDescription descrB, int nnzB, [In] IntPtr csrSortedValB, [In] IntPtr csrSortedRowPtrB, [In] IntPtr csrSortedColIndB,
			SparseMatrixDescription descrC, IntPtr csrSortedValC, IntPtr csrSortedRowPtrC, IntPtr csrSortedColIndC, IntPtr buffer);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseCcsrgeam2(IntPtr handle, int m, int n, ref FloatComplex alpha,
			SparseMatrixDescription descrA, int nnzA, [In] IntPtr csrSortedValA, [In] IntPtr csrSortedRowPtrA, [In] IntPtr csrSortedColIndA, ref FloatComplex beta,
			SparseMatrixDescription descrB, int nnzB, [In] IntPtr csrSortedValB, [In] IntPtr csrSortedRowPtrB, [In] IntPtr csrSortedColIndB,
			SparseMatrixDescription descrC, IntPtr csrSortedValC, IntPtr csrSortedRowPtrC, IntPtr csrSortedColIndC, IntPtr buffer);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseZcsrgeam2(IntPtr handle, int m, int n, ref DoubleComplex alpha,
			SparseMatrixDescription descrA, int nnzA, [In] IntPtr csrSortedValA, [In] IntPtr csrSortedRowPtrA, [In] IntPtr csrSortedColIndA, ref DoubleComplex beta,
			SparseMatrixDescription descrB, int nnzB, [In] IntPtr csrSortedValB, [In] IntPtr csrSortedRowPtrB, [In] IntPtr csrSortedColIndB,
			SparseMatrixDescription descrC, IntPtr csrSortedValC, IntPtr csrSortedRowPtrC, IntPtr csrSortedColIndC, IntPtr buffer);
		#endregion

		/// <summary>
		/// The helper function of <see cref="gemmFunc{T}"/>
		/// </summary>
		/// <param name="handle">CUDA Sparse library handle</param>
		/// <param name="m">number of rows of matrix <c>A</c></param>
		/// <param name="n">number of columns of matrix <c>B</c> and <c>D</c></param>
		/// <param name="k">number of columns of <c>A</c> and rows of <c>B</c></param>
		/// <param name="alpha">scalar to multiply <c>A</c></param>
		/// <param name="descrA"><see cref="SparseMatrixDescription"/> of matrix A</param>
		/// <param name="nnzA">number of non-zeros of matrix A</param>
		/// <param name="csrRowPtrA">row pointer array of matrix A</param>
		/// <param name="csrColIndA">column index array of matrix A</param>
		/// <param name="beta">scalar to multiply <c>D</c></param>
		/// <param name="descrB"><see cref="SparseMatrixDescription"/> of matrix B</param>
		/// <param name="nnzB">number of non-zeros of matrix B</param>
		/// <param name="csrRowPtrB">row pointer array of matrix B</param>
		/// <param name="csrColIndB">column index array of matrix B</param>
		/// <param name="descrD"><see cref="SparseMatrixDescription"/> of matrix D</param>
		/// <param name="nnzD">number of non-zeros of matrix D</param>
		/// <param name="csrRowPtrD">row pointer array of matrix D</param>
		/// <param name="csrColIndD">column index array of matrix D</param>
		/// <param name="info">the info</param>
		/// <param name="pBufferSizeInBytes">output buffer size in bytes</param>
		/// <returns><see cref="Status"/></returns>
		internal delegate Status gemmBufFunc<T>(IntPtr handle, int m, int n, int k, ref T alpha,
			SparseMatrixDescription descrA, int nnzA, [In] IntPtr csrRowPtrA, [In] IntPtr csrColIndA,
			SparseMatrixDescription descrB, int nnzB, [In] IntPtr csrRowPtrB, [In] IntPtr csrColIndB,
			ref T beta,
			SparseMatrixDescription descrD, int nnzD, [In] IntPtr csrRowPtrD, [In] IntPtr csrColIndD,
			IntPtr info, ref long pBufferSizeInBytes);

		/// <summary>
		/// This function performs spare matrix <c>A</c> multiply sparse matrix <c>B</c> adding sparse matrix <c>D</c>, result saved in new sparse matrix <c>C</c>.
		/// </summary>
		/// <param name="handle">CUDA Sparse library handle</param>
		/// <param name="m">number of rows of matrix <c>A</c></param>
		/// <param name="n">number of columns of matrix <c>B</c> and <c>D</c></param>
		/// <param name="k">number of columns of <c>A</c> and rows of <c>B</c></param>
		/// <param name="alpha">scalar to multiply <c>A</c></param>
		/// <param name="descrA"><see cref="SparseMatrixDescription"/> of matrix A</param>
		/// <param name="nnzA">number of non-zeros of matrix A</param>
		/// <param name="csrValA">value array of matrix A</param>
		/// <param name="csrRowPtrA">row pointer array of matrix A</param>
		/// <param name="csrColIndA">column index array of matrix A</param>
		/// <param name="beta">scalar to multiply <c>D</c></param>
		/// <param name="descrB"><see cref="SparseMatrixDescription"/> of matrix B</param>
		/// <param name="nnzB">number of non-zeros of matrix B</param>
		/// <param name="csrValB">value array of matrix B</param>
		/// <param name="csrRowPtrB">row pointer array of matrix B</param>
		/// <param name="csrColIndB">column index array of matrix B</param>
		/// <param name="descrD"><see cref="SparseMatrixDescription"/> of matrix D</param>
		/// <param name="nnzD">number of non-zeros of matrix D</param>
		/// <param name="csrValD">value array of matrix D</param>
		/// <param name="csrRowPtrD">row pointer array of matrix D</param>
		/// <param name="csrColIndD">column index array of matrix D</param>
		/// <param name="descrC"><see cref="SparseMatrixDescription"/> of matrix C</param>
		/// <param name="csrValC">value array of matrix C</param>
		/// <param name="csrRowPtrC">row pointer array of matrix C</param>
		/// <param name="csrColIndC">column index array of matrix C</param>
		/// <param name="info">the info</param>
		/// <param name="buffer">external buffer array</param>
		/// <returns><see cref="Status"/></returns>
		internal delegate Status gemmFunc<T>(IntPtr handle, int m, int n, int k, ref T alpha,
			SparseMatrixDescription descrA, int nnzA, [In] IntPtr csrValA, [In] IntPtr csrRowPtrA, [In] IntPtr csrColIndA,
			SparseMatrixDescription descrB, int nnzB, [In] IntPtr csrValB, [In] IntPtr csrRowPtrB, [In] IntPtr csrColIndB,
			ref T beta,
			SparseMatrixDescription descrD, int nnzD, [In] IntPtr csrValD, [In] IntPtr csrRowPtrD, [In] IntPtr csrColIndD,
			SparseMatrixDescription descrC, IntPtr csrValC, [In] IntPtr csrRowPtrC, IntPtr csrColIndC,
			[In] IntPtr info, IntPtr buffer);
		#region sparse matrices multiply (only for general matrices)
		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseScsrgemm2_bufferSizeExt(IntPtr handle, int m, int n, int k, ref float alpha,
			SparseMatrixDescription descrA, int nnzA, [In] IntPtr csrRowPtrA, [In] IntPtr csrColIndA,
			SparseMatrixDescription descrB, int nnzB, [In] IntPtr csrRowPtrB, [In] IntPtr csrColIndB,
			ref float beta,
			SparseMatrixDescription descrD, int nnzD, [In] IntPtr csrRowPtrD, [In] IntPtr csrColIndD,
			IntPtr info, ref long pBufferSizeInBytes);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseDcsrgemm2_bufferSizeExt(IntPtr handle, int m, int n, int k, ref double alpha,
			SparseMatrixDescription descrA, int nnzA, [In] IntPtr csrRowPtrA, [In] IntPtr csrColIndA,
			SparseMatrixDescription descrB, int nnzB, [In] IntPtr csrRowPtrB, [In] IntPtr csrColIndB,
			ref double beta,
			SparseMatrixDescription descrD, int nnzD, [In] IntPtr csrRowPtrD, [In] IntPtr csrColIndD,
			IntPtr info, ref long pBufferSizeInBytes);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseCcsrgemm2_bufferSizeExt(IntPtr handle, int m, int n, int k, ref FloatComplex alpha,
			SparseMatrixDescription descrA, int nnzA, [In] IntPtr csrRowPtrA, [In] IntPtr csrColIndA,
			SparseMatrixDescription descrB, int nnzB, [In] IntPtr csrRowPtrB, [In] IntPtr csrColIndB,
			ref FloatComplex beta,
			SparseMatrixDescription descrD, int nnzD, [In] IntPtr csrRowPtrD, [In] IntPtr csrColIndD,
			IntPtr info, ref long pBufferSizeInBytes);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseZcsrgemm2_bufferSizeExt(IntPtr handle, int m, int n, int k, ref DoubleComplex alpha,
			SparseMatrixDescription descrA, int nnzA, [In] IntPtr csrRowPtrA, [In] IntPtr csrColIndA,
			SparseMatrixDescription descrB, int nnzB, [In] IntPtr csrRowPtrB, [In] IntPtr csrColIndB,
			ref DoubleComplex beta,
			SparseMatrixDescription descrD, int nnzD, [In] IntPtr csrRowPtrD, [In] IntPtr csrColIndD,
			IntPtr info, ref long pBufferSizeInBytes);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseCreateCsrgemm2Info(ref IntPtr info);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseDestroyCsrgemm2Info(IntPtr info);

		/// <summary>
		/// The helper function that calculate the number of non-zeros of matrix <c>C</c>.
		/// </summary>
		/// <param name="handle">CUDA Sparse library handle</param>
		/// <param name="m">number of rows of matrix <c>A</c></param>
		/// <param name="n">number of columns of matrix <c>B</c> and <c>D</c></param>
		/// <param name="k">number of columns of <c>A</c> and rows of <c>B</c></param>
		/// <param name="descrA"><see cref="SparseMatrixDescription"/> of matrix A</param>
		/// <param name="nnzA">number of non-zeros of matrix A</param>
		/// <param name="csrRowPtrA">row pointer array of matrix A</param>
		/// <param name="csrColIndA">column index array of matrix A</param>
		/// <param name="descrB"><see cref="SparseMatrixDescription"/> of matrix B</param>
		/// <param name="nnzB">number of non-zeros of matrix B</param>
		/// <param name="csrRowPtrB">row pointer array of matrix B</param>
		/// <param name="csrColIndB">column index array of matrix B</param>
		/// <param name="descrD"><see cref="SparseMatrixDescription"/> of matrix D</param>
		/// <param name="nnzD">number of non-zeros of matrix D</param>
		/// <param name="csrRowPtrD">row pointer array of matrix D</param>
		/// <param name="csrColIndD">column index array of matrix D</param>
		/// <param name="descrC"><see cref="SparseMatrixDescription"/> of matrix C</param>
		/// <param name="csrRowPtrC">output row pointer array of matrix C</param>
		/// <param name="nnzC">output number of non-zeros</param>
		/// <param name="info">the info</param>
		/// <param name="buffer">external buffer array</param>
		/// <returns><see cref="Status"/></returns>
		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseXcsrgemm2Nnz(IntPtr handle, int m, int n, int k,
			SparseMatrixDescription descrA, int nnzA, [In] IntPtr csrRowPtrA, [In] IntPtr csrColIndA,
			SparseMatrixDescription descrB, int nnzB, [In] IntPtr csrRowPtrB, [In] IntPtr csrColIndB,
			SparseMatrixDescription descrD, int nnzD, [In] IntPtr csrRowPtrD, [In] IntPtr csrColIndD,
			SparseMatrixDescription descrC, IntPtr csrRowPtrC, ref int nnzC,
			[In] IntPtr info, IntPtr buffer);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseScsrgemm2(IntPtr handle, int m, int n, int k, ref float alpha,
			SparseMatrixDescription descrA, int nnzA, [In] IntPtr csrValA, [In] IntPtr csrRowPtrA, [In] IntPtr csrColIndA,
			SparseMatrixDescription descrB, int nnzB, [In] IntPtr csrValB, [In] IntPtr csrRowPtrB, [In] IntPtr csrColIndB,
			ref float beta,
			SparseMatrixDescription descrD, int nnzD, [In] IntPtr csrValD, [In] IntPtr csrRowPtrD, [In] IntPtr csrColIndD,
			SparseMatrixDescription descrC, IntPtr csrValC, [In] IntPtr csrRowPtrC, IntPtr csrColIndC,
			[In] IntPtr info, IntPtr buffer);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseDcsrgemm2(IntPtr handle, int m, int n, int k, ref double alpha,
			SparseMatrixDescription descrA, int nnzA, [In] IntPtr csrValA, [In] IntPtr csrRowPtrA, [In] IntPtr csrColIndA,
			SparseMatrixDescription descrB, int nnzB, [In] IntPtr csrValB, [In] IntPtr csrRowPtrB, [In] IntPtr csrColIndB,
			ref double beta,
			SparseMatrixDescription descrD, int nnzD, [In] IntPtr csrValD, [In] IntPtr csrRowPtrD, [In] IntPtr csrColIndD,
			SparseMatrixDescription descrC, IntPtr csrValC, [In] IntPtr csrRowPtrC, IntPtr csrColIndC,
			[In] IntPtr info, IntPtr buffer);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseCcsrgemm2(IntPtr handle, int m, int n, int k, ref FloatComplex alpha,
			SparseMatrixDescription descrA, int nnzA, [In] IntPtr csrValA, [In] IntPtr csrRowPtrA, [In] IntPtr csrColIndA,
			SparseMatrixDescription descrB, int nnzB, [In] IntPtr csrValB, [In] IntPtr csrRowPtrB, [In] IntPtr csrColIndB,
			ref FloatComplex beta,
			SparseMatrixDescription descrD, int nnzD, [In] IntPtr csrValD, [In] IntPtr csrRowPtrD, [In] IntPtr csrColIndD,
			SparseMatrixDescription descrC, IntPtr csrValC, [In] IntPtr csrRowPtrC, IntPtr csrColIndC,
			[In] IntPtr info, IntPtr buffer);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseZcsrgemm2(IntPtr handle, int m, int n, int k, ref DoubleComplex alpha,
			SparseMatrixDescription descrA, int nnzA, [In] IntPtr csrValA, [In] IntPtr csrRowPtrA, [In] IntPtr csrColIndA,
			SparseMatrixDescription descrB, int nnzB, [In] IntPtr csrValB, [In] IntPtr csrRowPtrB, [In] IntPtr csrColIndB,
			ref DoubleComplex beta,
			SparseMatrixDescription descrD, int nnzD, [In] IntPtr csrValD, [In] IntPtr csrRowPtrD, [In] IntPtr csrColIndD,
			SparseMatrixDescription descrC, IntPtr csrValC, [In] IntPtr csrRowPtrC, IntPtr csrColIndC,
			[In] IntPtr info, IntPtr buffer);
		#endregion

		#endregion


		#region format convert
		/// <summary>
		/// Scatter the sparse vector of 32bit index to a dense vector
		/// </summary>
		/// <param name="handle">CUDA Sparse library handle</param>
		/// <param name="nnz">number of non-zeros of x (length of <paramref name="xVal"/>)</param>
		/// <param name="xVal">value array of x</param>
		/// <param name="xInd">32bit index array of x</param>
		/// <param name="y">dense vector</param>
		/// <param name="idxBase"><see cref="IndexBase"/></param>
		/// <returns><see cref="Status"/></returns>
		internal delegate Status sctrFunc(IntPtr handle, int nnz, [In] IntPtr xVal, [In] IntPtr xInd, IntPtr y, IndexBase idxBase);
		#region sparse vector to dense vector
		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseSsctr(IntPtr handle, int nnz, [In] IntPtr xVal, [In] IntPtr xInd, IntPtr y, IndexBase idxBase);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseDsctr(IntPtr handle, int nnz, [In] IntPtr xVal, [In] IntPtr xInd, IntPtr y, IndexBase idxBase);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseCsctr(IntPtr handle, int nnz, [In] IntPtr xVal, [In] IntPtr xInd, IntPtr y, IndexBase idxBase);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseZsctr(IntPtr handle, int nnz, [In] IntPtr xVal, [In] IntPtr xInd, IntPtr y, IndexBase idxBase);
		#endregion

		/// <summary>
		/// Scatter the sparse vector of 32bit index to a dense vector
		/// </summary>
		/// <param name="handle">CUDA Sparse library handle</param>
		/// <param name="nnz">number of non-zeros of <paramref name="source"/> (length of <paramref name="indexOfSource"/>)</param>
		/// <param name="source">source vector to gather from</param>
		/// <param name="dest">destination vector to gather to</param>
		/// <param name="indexOfSource">2bit index array</param>
		/// <param name="idxBase"><see cref="IndexBase"/></param>
		/// <returns><see cref="Status"/></returns>
		internal delegate Status gthrFunc(IntPtr handle, int nnz, [In] IntPtr source, IntPtr dest, [In] IntPtr indexOfSource, IndexBase idxBase);
		#region dense vector gather
		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseSgthr(IntPtr handle, int nnz, [In] IntPtr y, IntPtr xVal, [In] IntPtr xInd, IndexBase idxBase);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseDgthr(IntPtr handle, int nnz, [In] IntPtr y, IntPtr xVal, [In] IntPtr xInd, IndexBase idxBase);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseCgthr(IntPtr handle, int nnz, [In] IntPtr y, IntPtr xVal, [In] IntPtr xInd, IndexBase idxBase);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseZgthr(IntPtr handle, int nnz, [In] IntPtr y, IntPtr xVal, [In] IntPtr xInd, IndexBase idxBase);
		#endregion

		/// <summary>
		/// Convert a sparse matrix with CSR format to dense one.
		/// </summary>
		/// <param name="handle">CUDA Sparse library handle</param>
		/// <param name="m">number of rows of <paramref name="A"/></param>
		/// <param name="n">number of columns of <paramref name="A"/></param>
		/// <param name="descrA"><see cref="SparseMatrixDescription"/> of sparse matrix</param>
		/// <param name="csrValA">value array of sparse matrix</param>
		/// <param name="csrRowPtrA">row pointer array of sparse matrix</param>
		/// <param name="csrColIndA">column index array of sparse matrix</param>
		/// <param name="A">pre-allocated dense matrix</param>
		/// <param name="lda">leading dimension of <paramref name="A"/></param>
		/// <returns><see cref="Status"/></returns>
		internal delegate Status csr2denseFunc(IntPtr handle, int m, int n, SparseMatrixDescription descrA, [In] IntPtr csrValA, [In] IntPtr csrRowPtrA, [In] IntPtr csrColIndA, IntPtr A, int lda);
		#region sparse CSR matrix to dense matrix
		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseScsr2dense(IntPtr handle, int m, int n, SparseMatrixDescription descrA, [In] IntPtr csrValA, [In] IntPtr csrRowPtrA, [In] IntPtr csrColIndA, IntPtr A, int lda);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseDcsr2dense(IntPtr handle, int m, int n, SparseMatrixDescription descrA, [In] IntPtr csrValA, [In] IntPtr csrRowPtrA, [In] IntPtr csrColIndA, IntPtr A, int lda);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseCcsr2dense(IntPtr handle, int m, int n, SparseMatrixDescription descrA, [In] IntPtr csrValA, [In] IntPtr csrRowPtrA, [In] IntPtr csrColIndA, IntPtr A, int lda);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseZcsr2dense(IntPtr handle, int m, int n, SparseMatrixDescription descrA, [In] IntPtr csrValA, [In] IntPtr csrRowPtrA, [In] IntPtr csrColIndA, IntPtr A, int lda);
		#endregion

		/// <summary>
		/// Convert a sparse matrix with CSR format to dense one.
		/// </summary>
		/// <param name="handle">CUDA Sparse library handle</param>
		/// <param name="m">number of rows of <paramref name="A"/></param>
		/// <param name="n">number of columns of <paramref name="A"/></param>
		/// <param name="descrA"><see cref="SparseMatrixDescription"/> of sparse matrix, only general matrix is supported</param>
		/// <param name="cscValA">value array of sparse matrix</param>
		/// <param name="cscRowIndA">row index array of sparse matrix</param>
		/// <param name="cscColPtrA">column pointer array of sparse matrix</param>
		/// <param name="A">pre-allocated dense matrix</param>
		/// <param name="lda">leading dimension of <paramref name="A"/></param>
		/// <returns><see cref="Status"/></returns>
		internal delegate Status csc2denseFunc(IntPtr handle, int m, int n, SparseMatrixDescription descrA, [In] IntPtr cscValA, [In] IntPtr cscRowIndA, [In] IntPtr cscColPtrA, IntPtr A, int lda);
		#region sparse CSC matrix to dense matrix
		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseScsc2dense(IntPtr handle, int m, int n, SparseMatrixDescription descrA, [In] IntPtr cscValA, [In] IntPtr cscRowIndA, [In] IntPtr cscColPtrA, IntPtr A, int lda);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseDcsc2dense(IntPtr handle, int m, int n, SparseMatrixDescription descrA, [In] IntPtr cscValA, [In] IntPtr cscRowIndA, [In] IntPtr cscColPtrA, IntPtr A, int lda);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseCcsc2dense(IntPtr handle, int m, int n, SparseMatrixDescription descrA, [In] IntPtr cscValA, [In] IntPtr cscRowIndA, [In] IntPtr cscColPtrA, IntPtr A, int lda);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseZcsc2dense(IntPtr handle, int m, int n, SparseMatrixDescription descrA, [In] IntPtr cscValA, [In] IntPtr cscRowIndA, [In] IntPtr cscColPtrA, IntPtr A, int lda);
		#endregion


		#region sparse CSR to CSC (can be used backward)
		// regard the CSC row indices as CSR column indices and the CSC column pointers as the CSR row pointers, you will get the transpose of the original CSR matrix in CSR format
		// similarly, if you do nothing while regard the CSR column indices as CSC row indices and the CSR row pointers as the CSC column pointers, you will get the transpose of the original CSR matrix in CSC format

		/// <summary>
		/// Convert sparse matrix A from CSR to CSC format or backward
		/// </summary>
		/// <param name="handle">CUDA Sparse library handle</param>
		/// <param name="m">number of rows of A</param>
		/// <param name="n">number of columns of A</param>
		/// <param name="nnz">number of non-zeros elements of A</param>
		/// <param name="csrVal">value array of CSR matrix</param>
		/// <param name="csrRowPtr">row pointer array of CSR matrix</param>
		/// <param name="csrColInd">column index array of CSR matrix</param>
		/// <param name="cscVal">output value array of CSC matrix</param>
		/// <param name="cscColPtr">output column pointer array of CSC matrix</param>
		/// <param name="cscRowInd">output row index array of CSC matrix</param>
		/// <param name="valType">data type <see cref="CudaDataType"/></param>
		/// <param name="copyValues">copy value array or not</param>
		/// <param name="idxBase"><see cref="IndexBase"/></param>
		/// <param name="alg"><see cref="CSR2CSCAlgorithm"/></param>
		/// <param name="bufferSize">output buffer size in bytes</param>
		/// <returns><see cref="Status"/></returns>
		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseCsr2cscEx2_bufferSize(IntPtr handle, int m, int n, int nnz, [In] IntPtr csrVal, [In] IntPtr csrRowPtr, [In] IntPtr csrColInd, IntPtr cscVal, IntPtr cscColPtr, IntPtr cscRowInd, CudaDataType valType, Action copyValues, IndexBase idxBase, CSR2CSCAlgorithm alg, ref long bufferSize);

		/// <summary>
		/// Convert sparse matrix A from CSR to CSC format or backward
		/// </summary>
		/// <param name="handle">CUDA Sparse library handle</param>
		/// <param name="m">number of rows of A</param>
		/// <param name="n">number of columns of A</param>
		/// <param name="nnz">number of non-zeros elements of A</param>
		/// <param name="csrVal">value array of CSR matrix</param>
		/// <param name="csrRowPtr">row pointer array of CSR matrix</param>
		/// <param name="csrColInd">column index array of CSR matrix</param>
		/// <param name="cscVal">output value array of CSC matrix</param>
		/// <param name="cscColPtr">output column pointer array of CSC matrix</param>
		/// <param name="cscRowInd">output row index array of CSC matrix</param>
		/// <param name="valType">data type <see cref="CudaDataType"/></param>
		/// <param name="copyValues">copy value array or not</param>
		/// <param name="idxBase"><see cref="IndexBase"/></param>
		/// <param name="alg"><see cref="CSR2CSCAlgorithm"/></param>
		/// <param name="buffer">buffer array</param>
		/// <returns><see cref="Status"/></returns>
		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseCsr2cscEx2(IntPtr handle, int m, int n, int nnz, [In] IntPtr csrVal, [In] IntPtr csrRowPtr, [In] IntPtr csrColInd, IntPtr cscVal, IntPtr cscColPtr, IntPtr cscRowInd, CudaDataType valType, Action copyValues, IndexBase idxBase, CSR2CSCAlgorithm alg, IntPtr buffer);
		#endregion

		/// <summary>
		/// This function computes the number of nonzero elements per row or column and the total number of nonzero elements in a dense matrix.
		/// </summary>
		/// <param name="handle">CUDA Sparse library handle</param>
		/// <param name="dirA">direction that specifies whether to count nonzero elements by rows or columns</param>
		/// <param name="m">number of rows of matrix <paramref name="A"/></param>
		/// <param name="n">number of columns of matrix <paramref name="A"/></param>
		/// <param name="descrA"><see cref="SparseMatrixDescription"/>  of matrix <paramref name="A"/>, only general matrix is supported</param>
		/// <param name="A">matrix to count number of nonzero elements</param>
		/// <param name="lda">leading dimension of dense array <paramref name="A"/></param>
		/// <param name="nnzPerRowColumn">output <see cref="int"/> array containing the number of nonzero elements per row or column, respectively</param>
		/// <param name="nnzTotal">output total number of nonzero elements</param>
		/// <returns><see cref="Status"/></returns>
		internal delegate Status nnzFunc(IntPtr handle, Direction dirA, int m, int n, SparseMatrixDescription descrA, [In] IntPtr A, int lda, IntPtr nnzPerRowColumn, ref int nnzTotal);
		#region count non-zero elements (used before dense to CSR/CSC)
		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseSnnz(IntPtr handle, Direction dirA, int m, int n, SparseMatrixDescription descrA, [In] IntPtr A, int lda, IntPtr nnzPerRowColumn, ref int nnzTotal);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseDnnz(IntPtr handle, Direction dirA, int m, int n, SparseMatrixDescription descrA, [In] IntPtr A, int lda, IntPtr nnzPerRowColumn, ref int nnzTotal);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseCnnz(IntPtr handle, Direction dirA, int m, int n, SparseMatrixDescription descrA, [In] IntPtr A, int lda, IntPtr nnzPerRowColumn, ref int nnzTotal);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseZnnz(IntPtr handle, Direction dirA, int m, int n, SparseMatrixDescription descrA, [In] IntPtr A, int lda, IntPtr nnzPerRowColumn, ref int nnzTotal);
		#endregion

		/// <summary>
		/// This function converts the matrix <paramref name="A"/> in dense format into a sparse matrix in CSR format.
		/// </summary>
		/// <param name="handle">CUDA Sparse library handle</param>
		/// <param name="m">number of rows of matrix <paramref name="A"/></param>
		/// <param name="n">number of columns of matrix <paramref name="A"/></param>
		/// <param name="descrA"><see cref="SparseMatrixDescription"/>  of matrix <paramref name="A"/>, only general matrix is supported</param>
		/// <param name="A">dense matrix</param>
		/// <param name="lda">leading dimension of dense array <paramref name="A"/></param>
		/// <param name="nnzPerRow">the <see cref="int"/> array containing the number of nonzero elements per row generated by <see cref="nnzFunc"/></param>
		/// <param name="csrValA">output nonzero elements array</param>
		/// <param name="csrRowPtrA">output row pointer array</param>
		/// <param name="csrColIndA">output column index array</param>
		/// <returns><see cref="Status"/></returns>
		internal delegate Status dense2csrFunc(IntPtr handle, int m, int n, SparseMatrixDescription descrA, [In] IntPtr A, int lda, [In] IntPtr nnzPerRow, IntPtr csrValA, IntPtr csrRowPtrA, IntPtr csrColIndA);
		#region dense matrix to CSR direct
		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseSdense2csr(IntPtr handle, int m, int n, SparseMatrixDescription descrA, [In] IntPtr A, int lda, [In] IntPtr nnzPerRow, IntPtr csrValA, IntPtr csrRowPtrA, IntPtr csrColIndA);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseDdense2csr(IntPtr handle, int m, int n, SparseMatrixDescription descrA, [In] IntPtr A, int lda, [In] IntPtr nnzPerRow, IntPtr csrValA, IntPtr csrRowPtrA, IntPtr csrColIndA);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseCdense2csr(IntPtr handle, int m, int n, SparseMatrixDescription descrA, [In] IntPtr A, int lda, [In] IntPtr nnzPerRow, IntPtr csrValA, IntPtr csrRowPtrA, IntPtr csrColIndA);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseZdense2csr(IntPtr handle, int m, int n, SparseMatrixDescription descrA, [In] IntPtr A, int lda, [In] IntPtr nnzPerRow, IntPtr csrValA, IntPtr csrRowPtrA, IntPtr csrColIndA);
		#endregion

		/// <summary>
		/// This function converts the matrix <paramref name="A"/> in dense format into a sparse matrix in CSC format.
		/// </summary>
		/// <param name="handle">CUDA Sparse library handle</param>
		/// <param name="m">number of rows of matrix <paramref name="A"/></param>
		/// <param name="n">number of columns of matrix <paramref name="A"/></param>
		/// <param name="descrA"><see cref="SparseMatrixDescription"/>  of matrix <paramref name="A"/>, only general matrix is supported</param>
		/// <param name="A">dense matrix</param>
		/// <param name="lda">leading dimension of dense array <paramref name="A"/></param>
		/// <param name="nnzPerCol">the <see cref="int"/> array containing the number of nonzero elements per row generated by <see cref="nnzFunc"/></param>
		/// <param name="cscValA">output nonzero elements array</param>
		/// <param name="cscRowIndA">output row index array</param>
		/// <param name="cscColPtrA">output column pointer array</param>
		/// <returns><see cref="Status"/></returns>
		internal delegate Status dense2cscFunc(IntPtr handle, int m, int n, SparseMatrixDescription descrA, [In] IntPtr A, int lda, [In] IntPtr nnzPerCol, IntPtr cscValA, IntPtr cscRowIndA, IntPtr cscColPtrA);
		#region dense matrix to CSC direct
		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseSdense2csc(IntPtr handle, int m, int n, SparseMatrixDescription descrA, [In] IntPtr A, int lda, [In] IntPtr nnzPerCol, IntPtr cscValA, IntPtr cscRowIndA, IntPtr cscColPtrA);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseDdense2csc(IntPtr handle, int m, int n, SparseMatrixDescription descrA, [In] IntPtr A, int lda, [In] IntPtr nnzPerCol, IntPtr cscValA, IntPtr cscRowIndA, IntPtr cscColPtrA);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseCdense2csc(IntPtr handle, int m, int n, SparseMatrixDescription descrA, [In] IntPtr A, int lda, [In] IntPtr nnzPerCol, IntPtr cscValA, IntPtr cscRowIndA, IntPtr cscColPtrA);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseZdense2csc(IntPtr handle, int m, int n, SparseMatrixDescription descrA, [In] IntPtr A, int lda, [In] IntPtr nnzPerCol, IntPtr cscValA, IntPtr cscRowIndA, IntPtr cscColPtrA);
		#endregion

		/// <summary>
		/// The helper function of <see cref="pruneDense2csrFunc{T}"/>
		/// </summary>
		/// <param name="handle">CUDA Sparse library handle</param>
		/// <param name="m">number of rows of matrix <paramref name="A"/></param>
		/// <param name="n">number of columns of matrix <paramref name="A"/></param>
		/// <param name="A">dense matrix</param>
		/// <param name="lda">leading dimension of dense array <paramref name="A"/></param>
		/// <param name="threshold">the value with abs below <paramref name="threshold"/> will be pruned</param>
		/// <param name="descrC"><see cref="SparseMatrixDescription"/> of sparse matrix C, only general matrix is supported</param>
		/// <param name="csrValC">output nonzero elements array</param>
		/// <param name="csrRowPtrC">output row pointer array</param>
		/// <param name="csrColIndC">output column index array</param>
		/// <param name="pBufferSizeInBytes">output buffer size in bytes</param>
		/// <returns><see cref="Status"/></returns>
		internal delegate Status pruneDense2csrBufFunc<T>(IntPtr handle, int m, int n, [In] IntPtr A, int lda, ref T threshold, SparseMatrixDescription descrC, [In] IntPtr csrValC, [In] IntPtr csrRowPtrC, [In] IntPtr csrColIndC, ref long pBufferSizeInBytes);

		/// <summary>
		/// The helper function of <see cref="pruneDense2csrFunc{T}"/>, calculate the number of non-zero elements.
		/// </summary>
		/// <param name="handle">CUDA Sparse library handle</param>
		/// <param name="m">number of rows of matrix <paramref name="A"/></param>
		/// <param name="n">number of columns of matrix <paramref name="A"/></param>
		/// <param name="A">dense matrix</param>
		/// <param name="lda">leading dimension of dense array <paramref name="A"/></param>
		/// <param name="threshold">the value with abs below <paramref name="threshold"/> will be pruned</param>
		/// <param name="descrC"><see cref="SparseMatrixDescription"/> of sparse matrix C, only general matrix is supported</param>
		/// <param name="csrRowPtrC">output row pointer array</param>
		/// <param name="nnzTotal">output int to indicate how many total non-zero elements there are</param>
		/// <param name="buffer">buffer array</param>
		/// <returns><see cref="Status"/></returns>
		internal delegate Status pruneDense2csrNnzFunc<T>(IntPtr handle, int m, int n, [In] IntPtr A, int lda, ref T threshold, SparseMatrixDescription descrC, IntPtr csrRowPtrC, ref int nnzTotal, IntPtr buffer);

		/// <summary>
		/// Prunes a dense array directly into a sparse one. The abs values that are lower than <paramref name="threshold"/> are regarded as zero.
		/// </summary>
		/// <param name="handle">CUDA Sparse library handle</param>
		/// <param name="m">number of rows of matrix <paramref name="A"/></param>
		/// <param name="n">number of columns of matrix <paramref name="A"/></param>
		/// <param name="A">dense matrix</param>
		/// <param name="lda">leading dimension of dense array <paramref name="A"/></param>
		/// <param name="threshold">the value with abs below <paramref name="threshold"/> will be pruned</param>
		/// <param name="descrC"><see cref="SparseMatrixDescription"/> of sparse matrix C, only general matrix is supported</param>
		/// <param name="csrValC">output nonzero elements array</param>
		/// <param name="csrRowPtrC">input row pointer array calculated by <see cref="pruneDense2csrNnzFunc{T}"/></param>
		/// <param name="csrColIndC">output column index array</param>
		/// <param name="buffer">buffer array</param>
		/// <returns><see cref="Status"/></returns>
		internal delegate Status pruneDense2csrFunc<T>(IntPtr handle, int m, int n, [In] IntPtr A, int lda, ref T threshold, SparseMatrixDescription descrC, IntPtr csrValC, [In] IntPtr csrRowPtrC, IntPtr csrColIndC, IntPtr buffer);
		#region real dense matrix direct prune to CSR
		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseSpruneDense2csr_bufferSizeExt(IntPtr handle, int m, int n, [In] IntPtr A, int lda, ref float threshold, SparseMatrixDescription descrC, [In] IntPtr csrValC, [In] IntPtr csrRowPtrC, [In] IntPtr csrColIndC, ref long pBufferSizeInBytes);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseDpruneDense2csr_bufferSizeExt(IntPtr handle, int m, int n, [In] IntPtr A, int lda, ref double threshold, SparseMatrixDescription descrC, [In] IntPtr csrValC, [In] IntPtr csrRowPtrC, [In] IntPtr csrColIndC, ref long pBufferSizeInBytes);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseSpruneDense2csrNnz(IntPtr handle, int m, int n, [In] IntPtr A, int lda, ref float threshold, SparseMatrixDescription descrC, IntPtr csrRowPtrC, ref int nnzTotal, IntPtr buffer);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseDpruneDense2csrNnz(IntPtr handle, int m, int n, [In] IntPtr A, int lda, ref double threshold, SparseMatrixDescription descrC, IntPtr csrRowPtrC, ref int nnzTotal, IntPtr buffer);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseSpruneDense2csr(IntPtr handle, int m, int n, [In] IntPtr A, int lda, ref float threshold, SparseMatrixDescription descrC, IntPtr csrValC, [In] IntPtr csrRowPtrC, IntPtr csrColIndC, IntPtr buffer);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseDpruneDense2csr(IntPtr handle, int m, int n, [In] IntPtr A, int lda, ref double threshold, SparseMatrixDescription descrC, IntPtr csrValC, [In] IntPtr csrRowPtrC, IntPtr csrColIndC, IntPtr buffer);
		#endregion

		#region device range array (this + sort + gather = actual sort)
		/// <summary>
		/// Fill the <see cref="int"/> array with range [0, <paramref name="n"/>)
		/// </summary>
		/// <param name="handle">CUDA Sparse library handle</param>
		/// <param name="n">length of <paramref name="p"/></param>
		/// <param name="p">array to fill</param>
		/// <returns><see cref="Status"/></returns>
		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseCreateIdentityPermutation(IntPtr handle, int n, IntPtr p);
		#endregion

		#region CSR sort
		/// <summary>
		/// The helper function of <see cref="cusparseXcsrsort"/>.
		/// </summary>
		/// <param name="handle">CUDA Sparse library handle</param>
		/// <param name="m">number of rows of matrix</param>
		/// <param name="n">number of columns of matrix</param>
		/// <param name="nnz">number of non-zeros</param>
		/// <param name="csrRowPtr">input row pointer array</param>
		/// <param name="csrColInd">input column index array</param>
		/// <param name="pBufferSizeInBytes">output buffer size in bytes</param>
		/// <returns><see cref="Status"/></returns>
		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseXcsrsort_bufferSizeExt(IntPtr handle, int m, int n, int nnz, [In] IntPtr csrRowPtr, [In] IntPtr csrColInd, ref long pBufferSizeInBytes);

		/// <summary>
		/// Sorts the CSR format sparse matrix.
		/// </summary>
		/// <param name="handle">CUDA Sparse library handle</param>
		/// <param name="m">number of rows of matrix</param>
		/// <param name="n">number of columns of matrix</param>
		/// <param name="nnz">number of non-zeros</param>
		/// <param name="descrA"><see cref="SparseMatrixDescription"/>  of matrix, only general matrix is supported</param>
		/// <param name="csrRowPtr">input/output row pointer array</param>
		/// <param name="csrColInd">input/output column index array</param>
		/// <param name="P">input array generated by <see cref="cusparseCreateIdentityPermutation"/>, becomes the value array's new permutation at output</param>
		/// <param name="buffer">buffer array</param>
		/// <returns><see cref="Status"/></returns>
		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseXcsrsort(IntPtr handle, int m, int n, int nnz, SparseMatrixDescription descrA, [In] IntPtr csrRowPtr, IntPtr csrColInd, IntPtr P, IntPtr buffer);
		#endregion

		#region CSC sort
		/// <summary>
		/// The helper function of <see cref="cusparseXcscsort"/>.
		/// </summary>
		/// <param name="handle">CUDA Sparse library handle</param>
		/// <param name="m">number of rows of matrix</param>
		/// <param name="n">number of columns of matrix</param>
		/// <param name="nnz">number of non-zeros</param>
		/// <param name="cscColPtr">input column pointer array</param>
		/// <param name="cscRowInd">input row index array</param>
		/// <param name="pBufferSizeInBytes">output buffer size in bytes</param>
		/// <returns><see cref="Status"/></returns>
		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseXcscsort_bufferSizeExt(IntPtr handle, int m, int n, int nnz, [In] IntPtr cscColPtr, [In] IntPtr cscRowInd, ref long pBufferSizeInBytes);

		/// <summary>
		/// Sorts the CSC format sparse matrix.
		/// </summary>
		/// <param name="handle">CUDA Sparse library handle</param>
		/// <param name="m">number of rows of matrix</param>
		/// <param name="n">number of columns of matrix</param>
		/// <param name="nnz">number of non-zeros</param>
		/// <param name="descrA"><see cref="SparseMatrixDescription"/>  of matrix, only general matrix is supported</param>
		/// <param name="cscColPtr">input/output column pointer array</param>
		/// <param name="cscRowInd">input/output row index array</param>
		/// <param name="P">input array generated by <see cref="cusparseCreateIdentityPermutation"/>, becomes the value array's new permutation at output</param>
		/// <param name="buffer">buffer array</param>
		/// <returns><see cref="Status"/></returns>
		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseXcscsort(IntPtr handle, int m, int n, int nnz, SparseMatrixDescription descrA, [In] IntPtr cscColPtr, IntPtr cscRowInd, IntPtr P, IntPtr buffer);
		#endregion


		/// <summary>
		/// The helper function of <see cref="csr2csr_compress{T}"/>.
		/// </summary>
		/// <param name="handle">CUDA Sparse library handle</param>
		/// <param name="m">number of rows of matrix before compression</param>
		/// <param name="descr"><see cref="SparseMatrixDescription"/> of matrix before compression</param>
		/// <param name="csrValA">input nonzero elements array</param>
		/// <param name="csrRowPtrA">input row pointer array</param>
		/// <param name="nnzPerRow">output <see cref="int"/> array to indicate how many non-zero elements pre row after compression</param>
		/// <param name="nnzC">output total number of non-zero elements after compression</param>
		/// <param name="tol">tolerance used for compression</param>
		/// <returns><see cref="Status"/></returns>
		internal delegate Status nnz_compress<T>(IntPtr handle, int m, SparseMatrixDescription descr, [In] IntPtr csrValA, [In] IntPtr csrRowPtrA, IntPtr nnzPerRow, ref int nnzC, T tol);

		/// <summary>
		/// Prune the original CSR spare matrix to a new one where the abs value below <paramref name="tol"/> are regarded as zero.
		/// </summary>
		/// <param name="handle">CUDA Sparse library handle</param>
		/// <param name="m">number of rows of matrix before compression</param>
		/// <param name="n">number of columns of matrix before compression</param>
		/// <param name="descr"><see cref="SparseMatrixDescription"/> of before compression</param>
		/// <param name="csrValA">input nonzero elements array</param>
		/// <param name="csrRowPtrA">input row pointer array</param>
		/// <param name="csrColIndA">input column index array</param> 
		/// <param name="nnzA">input total number of non-zero elements of before compression</param>
		/// <param name="nnzPerRow">input <see cref="int"/> array to indicate how many non-zero elements pre row after compression</param>
		/// <param name="csrValC">output nonzero elements array after compression</param>
		/// <param name="csrRowPtrC">output row pointer array after compression</param>
		/// <param name="csrColIndC">output column index array after compression</param> 
		/// <param name="tol">tolerance used for compression</param>
		/// <returns><see cref="Status"/></returns>
		internal delegate Status csr2csr_compress<T>(IntPtr handle, int m, int n, SparseMatrixDescription descr, [In] IntPtr csrValA, [In] IntPtr csrColIndA, [In] IntPtr csrRowPtrA, int nnzA, [In] IntPtr nnzPerRow, IntPtr csrValC, IntPtr csrColIndC, IntPtr csrRowPtrC, T tol);
		#region CSR prune
		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseSnnz_compress(IntPtr handle, int m, SparseMatrixDescription descr, [In] IntPtr csrValA, [In] IntPtr csrRowPtrA, IntPtr nnzPerRow, ref int nnzC, float tol);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseDnnz_compress(IntPtr handle, int m, SparseMatrixDescription descr, [In] IntPtr csrValA, [In] IntPtr csrRowPtrA, IntPtr nnzPerRow, ref int nnzC, double tol);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseCnnz_compress(IntPtr handle, int m, SparseMatrixDescription descr, [In] IntPtr csrValA, [In] IntPtr csrRowPtrA, IntPtr nnzPerRow, ref int nnzC, FloatComplex tol);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseZnnz_compress(IntPtr handle, int m, SparseMatrixDescription descr, [In] IntPtr csrValA, [In] IntPtr csrRowPtrA, IntPtr nnzPerRow, ref int nnzC, DoubleComplex tol);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseScsr2csr_compress(IntPtr handle, int m, int n, SparseMatrixDescription descr, [In] IntPtr csrValA, [In] IntPtr csrColIndA, [In] IntPtr csrRowPtrA, int nnzA, [In] IntPtr nnzPerRow, IntPtr csrValC, IntPtr csrColIndC, IntPtr csrRowPtrC, float tol);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseDcsr2csr_compress(IntPtr handle, int m, int n, SparseMatrixDescription descr, [In] IntPtr csrValA, [In] IntPtr csrColIndA, [In] IntPtr csrRowPtrA, int nnzA, [In] IntPtr nnzPerRow, IntPtr csrValC, IntPtr csrColIndC, IntPtr csrRowPtrC, double tol);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseCcsr2csr_compress(IntPtr handle, int m, int n, SparseMatrixDescription descr, [In] IntPtr csrValA, [In] IntPtr csrColIndA, [In] IntPtr csrRowPtrA, int nnzA, [In] IntPtr nnzPerRow, IntPtr csrValC, IntPtr csrColIndC, IntPtr csrRowPtrC, FloatComplex tol);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseZcsr2csr_compress(IntPtr handle, int m, int n, SparseMatrixDescription descr, [In] IntPtr csrValA, [In] IntPtr csrColIndA, [In] IntPtr csrRowPtrA, int nnzA, [In] IntPtr nnzPerRow, IntPtr csrValC, IntPtr csrColIndC, IntPtr csrRowPtrC, DoubleComplex tol);
		#endregion

		#region COO sort (this + sort + gather = actual sort)
		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseXcoosort_bufferSizeExt(IntPtr handle, int m, int n, int nnz, [In] IntPtr cooRows, [In] IntPtr cooCols, ref long pBufferSizeInBytes);

		/// <summary>
		/// Sort the COO format sparse matrix by column or row first order
		/// </summary>
		/// <param name="handle">CUDA Sparse library handle</param>
		/// <param name="m">number of rows of matrix</param>
		/// <param name="n">number of columns of matrix</param>
		/// <param name="nnz">total number of non-zero elements</param>
		/// <param name="cooRows">input/output row index array</param>
		/// <param name="cooCols">input/output column index array</param>
		/// <param name="P">input array generated by <see cref="cusparseCreateIdentityPermutation"/>, becomes the value array's new permutation at output</param>
		/// <param name="buffer">buffer array</param>
		/// <returns><see cref="Status"/></returns>
		internal delegate Status cooSortFunc(IntPtr handle, int m, int n, int nnz, IntPtr cooRows, IntPtr cooCols, IntPtr P, IntPtr buffer);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseXcoosortByRow(IntPtr handle, int m, int n, int nnz, IntPtr cooRows, IntPtr cooCols, IntPtr P, IntPtr buffer);

		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseXcoosortByColumn(IntPtr handle, int m, int n, int nnz, IntPtr cooRows, IntPtr cooCols, IntPtr P, IntPtr buffer);
		#endregion

		#region CSR/CSC <-> COO
		/// <summary>
		/// Convert the CSR / CSC format sparse matrix to a COO format matrix
		/// </summary>
		/// <param name="handle">CUDA Sparse library handle</param>
		/// <param name="csrRowPtr">input row pointer array of CSR matrix or column pointer array of CSC matrix</param>
		/// <param name="nnz">total number of non-zero elements</param>
		/// <param name="m">number of rows of matrix</param>
		/// <param name="cooRowInd">output COO row index array or COO column index array</param>
		/// <param name="idxBase"><see cref="IndexBase"/></param>
		/// <returns><see cref="Status"/></returns>
		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseXcsr2coo(IntPtr handle, [In] IntPtr csrRowPtr, int nnz, int m, IntPtr cooRowInd, IndexBase idxBase);

		/// <summary>
		/// Convert the COO format sparse matrix to a CSR / CSC format matrix
		/// </summary>
		/// <param name="handle">CUDA Sparse library handle</param>
		/// <param name="csrRowPtr">output row pointer array of CSR matrix or column pointer array of CSC matrix</param>
		/// <param name="nnz">total number of non-zero elements</param>
		/// <param name="m">number of rows of matrix</param>
		/// <param name="cooRowInd">input COO row index array or COO column index array</param>
		/// <param name="idxBase"><see cref="IndexBase"/></param>
		/// <returns><see cref="Status"/></returns>
		[DllImport(CUSPARSE_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusparseXcoo2csr(IntPtr handle, [In] IntPtr cooRowInd, int nnz, int m, IntPtr csrRowPtr, IndexBase idxBase);
		#endregion

		#endregion
	}
}
