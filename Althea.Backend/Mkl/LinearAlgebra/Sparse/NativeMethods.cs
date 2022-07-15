using System.Runtime.InteropServices;

using Althea.SourceGenerator;


namespace Althea.Backend.Mkl.LinearAlgebra.Sparse
{
	[NativeMethodClass]
	internal static unsafe class NativeMethodsTemplate
	{
		[NativeMethod(11)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklSparseBlasError mkl_sparse_s_create_coo(out IntPtr A, int indexing, MklInt rows, MklInt cols, MklInt nnz, MklInt* row_indx, MklInt* col_indx, Float32* values);

		[NativeMethod(11)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklSparseBlasError mkl_sparse_s_create_csr(out IntPtr A, int indexing, MklInt rows, MklInt cols, MklInt nnz, MklInt* row_start, MklInt* row_end, MklInt* col_indx, Float32* values);

		[NativeMethod(11)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklSparseBlasError mkl_sparse_s_create_csc(out IntPtr A, int indexing, MklInt rows, MklInt cols, MklInt nnz, MklInt* col_start, MklInt* col_end, MklInt* row_indx, Float32* values);

		[NativeMethod(11)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklSparseBlasError mkl_sparse_s_create_bsr(out IntPtr A, int indexing, MatrixMajor block_layout, MklInt rows, MklInt cols, MklInt nnz, MklInt block_size, MklInt* row_start, MklInt* row_end, MklInt* col_indx, Float32* values);

		[NativeMethod(11)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklSparseBlasError mkl_sparse_s_export_csr(IntPtr A, out int indexing, out MklInt rows, out MklInt cols, out MklInt nnz, out MklInt* row_start, out MklInt* row_end, out MklInt* col_indx, out Float32* values);

		[NativeMethod(11)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklSparseBlasError mkl_sparse_s_export_csc(IntPtr A, out int indexing, out MklInt rows, out MklInt cols, out MklInt nnz, out MklInt* col_start, out MklInt* col_end, out MklInt* row_indx, out Float32* values);

		[NativeMethod(11)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklSparseBlasError mkl_sparse_s_export_bsr(IntPtr A, out int indexing, out MatrixMajor block_layout, out MklInt rows, out MklInt cols, out MklInt nnz, out MklInt block_size, out MklInt* row_start, out MklInt* row_end, out MklInt* col_indx, out Float32* values);

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
		internal static extern MklSparseBlasError mkl_sparse_s_add(MatrixOp transA, Float32 alpha, IntPtr A, IntPtr B, ref IntPtr C);

		[NativeMethod(11)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklSparseBlasError mkl_sparse_s_spmmd(MatrixOp transA, IntPtr A, IntPtr B, MatrixMajor dense_layout, Float32* C, MklInt ldc);

		[NativeMethod(11)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklSparseBlasError mkl_sparse_s_sp2md(MatrixOp transA, MatrixDescr descrA, IntPtr A, MatrixOp transB, MatrixDescr descrB, IntPtr B, Float32 alpha, Float32 beta, Float32* C, MatrixMajor dense_layout, MklInt ldc);

		[NativeMethod(11)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklSparseBlasError mkl_sparse_s_syrkd(MatrixOp transA, IntPtr A, Float32 alpha, Float32 beta, Float32* C, MatrixMajor dense_layout, MklInt ldc);

		[NativeMethod(11)]
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklSparseBlasError mkl_sparse_s_syprd(MatrixOp transA, IntPtr A, Float32* B, MatrixMajor layoutB, MklInt ldb, Float32 alpha, Float32 beta, Float32* C, MatrixMajor dense_layout, MklInt ldc);
	}

	/// <summary>
	/// The static class for MKL sparse BLAS and LAPACK native methods
	/// </summary>
	public static unsafe partial class NativeMethods
	{
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
		internal static extern MklSparseBlasError mkl_sparse_spmm(MatrixOp transA, IntPtr A, IntPtr B, ref IntPtr C);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklSparseBlasError mkl_sparse_sp2m(MatrixOp transA, MatrixDescr descrA, IntPtr A, MatrixOp transB, MatrixDescr descrB, IntPtr B, Request request, ref IntPtr C);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklSparseBlasError mkl_sparse_syrk(MatrixOp transA, IntPtr A, ref IntPtr C);

		// C = op(A) * B * (op(A))^{T for real or H for complex}
		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklSparseBlasError mkl_sparse_sypr(MatrixOp transA, IntPtr A, IntPtr B, MatrixDescr descrB, ref IntPtr C, Request request);

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
	}
}
