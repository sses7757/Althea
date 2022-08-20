using System.Runtime.InteropServices;

using Althea.SourceGenerator;


namespace Althea.Backend.Mkl.LinearAlgebra.Sparse
{
	[NativeMethodClass]
	internal static unsafe class NativeMethodsTemplate
	{
		#region level 1
		[NativeMethod(6, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_saxpyi(MklInt nnz, Float32 alpha, Float32* x, MklInt* indx, Float32* y);

		[NativeMethod(6)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_sgthr(MklInt nnz, void* y, void* x, MklInt* indx);

		[NativeMethod(6)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_sgthrz(MklInt nnz, void* y, void* x, MklInt* indx);

		[NativeMethod(6)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern void cblas_ssctr(MklInt nnz, void* x, MklInt* indx, void* y);

		// Ignore Spelling: sdoti ddoti
		[CustomNativeMethod(6, "Float32", "sdoti")]
		[CustomNativeMethod(6, "Float64", "ddoti")]
		[CustomNativeMethod(6, "Complex<Float32>", "cdotui_sub", "", "Complex<Float32>", true)]
		[CustomNativeMethod(6, "Complex<Float32>", "cdotci_sub", "", "Complex<Float32>", true)]
		[CustomNativeMethod(6, "Complex<Float64>", "zdotui_sub", "", "Complex<Float64>", true)]
		[CustomNativeMethod(6, "Complex<Float64>", "zdotci_sub", "", "Complex<Float64>", true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern Float32 cblas_sdoti(MklInt n, Float32* x, MklInt* indx, Float32* y);
		#endregion

		#region level 2 and 3
		[NativeMethod(11)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklSparseBlasError mkl_sparse_s_create_coo(out IntPtr A, int indexing, MklInt rows, MklInt cols, MklInt nnz, MklInt* row_indx, MklInt* col_indx, void* values);

		[NativeMethod(11)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklSparseBlasError mkl_sparse_s_create_csr(out IntPtr A, int indexing, MklInt rows, MklInt cols, MklInt* row_start, MklInt* row_end, MklInt* col_indx, void* values);

		[NativeMethod(11)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklSparseBlasError mkl_sparse_s_create_csc(out IntPtr A, int indexing, MklInt rows, MklInt cols, MklInt* col_start, MklInt* col_end, MklInt* row_indx, void* values);

		[NativeMethod(11)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklSparseBlasError mkl_sparse_s_create_bsr(out IntPtr A, int indexing, MatrixMajor block_layout, MklInt rows, MklInt cols, MklInt block_size, MklInt* row_start, MklInt* row_end, MklInt* col_indx, void* values);

		[NativeMethod(11)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklSparseBlasError mkl_sparse_s_export_csr(IntPtr A, out int indexing, out MklInt rows, out MklInt cols,out MklInt* row_start, out MklInt* row_end, out MklInt* col_indx, out void* values);

		[NativeMethod(11)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklSparseBlasError mkl_sparse_s_export_csc(IntPtr A, out int indexing, out MklInt rows, out MklInt cols, out MklInt* col_start, out MklInt* col_end, out MklInt* row_indx, out void* values);

		[NativeMethod(11)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklSparseBlasError mkl_sparse_s_export_bsr(IntPtr A, out int indexing, out MatrixMajor block_layout, out MklInt rows, out MklInt cols, out MklInt block_size, out MklInt* row_start, out MklInt* row_end, out MklInt* col_indx, out void* values);

		[NativeMethod(11)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklSparseBlasError mkl_sparse_s_mv(MatrixOp transA, Float32 alpha, IntPtr A, MatrixDescr descr, Float32* x, Float32 beta, Float32* y);

		[NativeMethod(11)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklSparseBlasError mkl_sparse_s_trsv(MatrixOp transA, Float32 alpha, IntPtr A, MatrixDescr descr, Float32* x, Float32* y);

		[NativeMethod(11)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklSparseBlasError mkl_sparse_s_mm(MatrixOp transA, Float32 alpha, IntPtr A, MatrixDescr descr, MatrixMajor dense_layout, Float32* B, MklInt colb, MklInt ldb, Float32 beta, Float32* C, MklInt ldc);

		[NativeMethod(11)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklSparseBlasError mkl_sparse_s_trsm(MatrixOp transA, Float32 alpha, IntPtr A, MatrixDescr descr, MatrixMajor dense_layout, Float32* B, MklInt colb, MklInt ldb, Float32* X, MklInt ldx);

		[NativeMethod(11)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklSparseBlasError mkl_sparse_s_add(MatrixOp transA, Float32 alpha, IntPtr A, IntPtr B, out IntPtr C);

		[NativeMethod(11)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklSparseBlasError mkl_sparse_s_spmmd(MatrixOp transA, IntPtr A, IntPtr B, MatrixMajor dense_layout, void* C, MklInt ldc);

		[NativeMethod(11)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklSparseBlasError mkl_sparse_s_sp2md(MatrixOp transA, MatrixDescr descrA, IntPtr A, MatrixOp transB, MatrixDescr descrB, IntPtr B, Float32 alpha, Float32 beta, Float32* C, MatrixMajor dense_layout, MklInt ldc);

		[NativeMethod(11)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklSparseBlasError mkl_sparse_s_syrkd(MatrixOp transA, IntPtr A, Float32 alpha, Float32 beta, Float32* C, MatrixMajor dense_layout, MklInt ldc);

		[NativeMethod(11)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklSparseBlasError mkl_sparse_s_syprd(MatrixOp transA, IntPtr A, Float32* B, MatrixMajor layoutB, MklInt ldb, Float32 alpha, Float32 beta, Float32* C, MatrixMajor dense_layout, MklInt ldc);
		#endregion

		#region QR solve
		[NativeMethod(11, false, false, false, true)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklSparseBlasError mkl_sparse_s_qr(MatrixOp trans, IntPtr A, MatrixDescr descr, MatrixMajor dense_layout, MklInt nrhs, void* x, MklInt ldx, void* b, MklInt ldb);
		#endregion
	}

	/// <summary>
	/// The static class for MKL sparse BLAS native methods
	/// </summary>
	public static unsafe partial class NativeMethods
	{
		#region level 1
		internal delegate T cblas_doti<T>(MklInt nnz, T* x, MklInt* indx, T* y) where T : unmanaged;

		internal delegate void cblas_doti_comp<T>(MklInt nnz, T* x, MklInt* indx, T* y, out T dot) where T : unmanaged;
		#endregion

		#region level 2 and 3
		internal delegate MklSparseBlasError mkl_sparse__export_csr<T>(IntPtr A, out int indexing, out MklInt rows, out MklInt cols, out MklInt nnz, out MklInt* row_start, out MklInt* row_end, out MklInt* col_indx, out T* values) where T : unmanaged, IBaseNumber<T>;

		internal delegate MklSparseBlasError mkl_sparse__export_csc<T>(IntPtr A, out int indexing, out MklInt rows, out MklInt cols, out MklInt nnz, out MklInt* col_start, out MklInt* col_end, out MklInt* row_indx, out T* values) where T : unmanaged, IBaseNumber<T>;

		internal delegate MklSparseBlasError mkl_sparse__export_bsr<T>(IntPtr A, out int indexing, out MatrixMajor block_layout, out MklInt rows, out MklInt cols, out MklInt nnz, out MklInt block_size, out MklInt* row_start, out MklInt* row_end, out MklInt* col_indx, out T* values) where T : unmanaged, IBaseNumber<T>;

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklSparseBlasError mkl_sparse_copy(IntPtr source, MatrixDescr descr, out IntPtr dest);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklSparseBlasError mkl_sparse_destroy(IntPtr A);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklSparseBlasError mkl_sparse_convert_csr(IntPtr source, MatrixOp op, out IntPtr dest);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklSparseBlasError mkl_sparse_convert_bsr(IntPtr source, MklInt block_size, MatrixMajor block_layout, MatrixOp op, out IntPtr dest);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklSparseBlasError mkl_sparse_order(IntPtr matrix);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklSparseBlasError mkl_sparse_spmm(MatrixOp transA, IntPtr A, IntPtr B, out IntPtr C);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklSparseBlasError mkl_sparse_sp2m(MatrixOp transA, MatrixDescr descrA, IntPtr A, MatrixOp transB, MatrixDescr descrB, IntPtr B, Request request, ref IntPtr C);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklSparseBlasError mkl_sparse_syrk(MatrixOp transA, IntPtr A, out IntPtr C);

		// C = op(A) * B * (op(A))^{T for real or H for complex}
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklSparseBlasError mkl_sparse_sypr(MatrixOp transA, IntPtr A, IntPtr B, MatrixDescr descrB, out IntPtr C, Request request);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklSparseBlasError mkl_sparse_set_mv_hint(IntPtr A, MatrixOp transA, MatrixDescr descrA, MklInt expected_calls);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklSparseBlasError mkl_sparse_set_sv_hint(IntPtr A, MatrixOp transA, MatrixDescr descrA, MklInt expected_calls);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklSparseBlasError mkl_sparse_set_mm_hint(IntPtr A, MatrixOp transA, MatrixDescr descrA, MatrixMajor dense_layout, MklInt dense_columns, MklInt expected_calls);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklSparseBlasError mkl_sparse_set_sm_hint(IntPtr A, MatrixOp transA, MatrixDescr descrA, MatrixMajor dense_layout, MklInt dense_columns, MklInt expected_calls);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklSparseBlasError mkl_sparse_set_memory_hint(IntPtr A, MemoryUsage policy);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklSparseBlasError mkl_sparse_optimize(IntPtr A);
		#endregion

		#region QR
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklSparseBlasError mkl_sparse_set_qr_hint(IntPtr A, int hint = 0);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklSparseBlasError mkl_sparse_qr_reorder(IntPtr A, MatrixDescr descr);
		#endregion
	}

	/// <summary>
	/// The static class for custom sparse BLAS native methods
	/// </summary>
	public static unsafe class CustomNativeMethods
	{
		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern int vecSetValAt(DataType type, void* a, void* value, MklInt* pos, long posN);

		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern long vecPruneDirect(DataType type, void* a, void* threshold, long n, MklInt* idxOut, void* valOut, bool safe, long nnz);

		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern long vecPruneBuffer(DataType type, long n);

		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern long vecPruneNnz(DataType type, void* a, void* threshold, long n, void* buffer);

		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern int vecPruneCal(DataType type, long n, void* buffer, long nnz, MklInt* indexOut, void* valueOut);

		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern void spVecIdxToCooIdxs(MklInt* index, MklInt* rowIdx, MklInt* colIdx, long N, long ld);

		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern void cooIdxsToSpVecIdx(MklInt* index, MklInt* rowIdx, MklInt* colIdx, long N, long ld);

		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern long vecSpAddBuffer(DataType type, MklInt nnzA, MklInt nnzB);

		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern long vecSpAddNnz(DataType type, MklInt* indA, void* valA, MklInt nnzA, MklInt* indB, void* valB, MklInt nnzB, void* alpha, void* buffer);

		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern int vecSpAddCal(DataType type, void* buffer, long nnzAB, long nnzC, MklInt* C_index, void* C_value);

		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern int spVecOuterCheck(DataType type);

		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern int spVecOuter(DataType type, void* valA, MklInt* indA, long nnzA, void* valB, MklInt* indB, long nnzB, void* valC, MklInt* rowC, MklInt* colC, bool conj);

		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern int cooMatKron(DataType type, void* valA, MklInt* rowA, MklInt* colA, long nnzA, void* valB, MklInt* rowB, MklInt* colB, long nnzB, long rowsB, long colsB, void* valC, MklInt* rowC, MklInt* colC);

		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern int vecSort(DataType type, void* array, long N, int stride);

		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern int vecSortBy(DataType keyType, DataType valType, void* keys, void* vals, long N, int strideKey, int strideVal);

		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern int vecFind(DataType type, bool sorted, void* array, long N, int stride, void* toFind, out long index);

		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern int vecFillRange(DataType type, void* array, long N, int stride, void* start, void* step);

		[DllImport(Mkl.NativeMethods.CUSTOM_DLL_NAME)]
		internal static extern int vecBound(DataType type, bool lower, void* array, long N, int stride, void* toFind, out long index);
	}
}
