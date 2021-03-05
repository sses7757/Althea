using System;
using System.Collections.Generic;

using Althea.Helpers;


namespace Althea.LinearAlgebra.Sparse
{
	#region enum
	/// <summary>
	/// The <see cref="SparseVectorFormat"/> enum indicates the format specification of a sparse vector. Each bit flag indicates an atomic format.
	/// </summary>
	[Flags]
	public enum SparseVectorFormat : int
	{
		/// <summary>
		/// The Coordinate Format that stores each non-zero element and its <b>zero-based</b> index in separate storages.
		/// </summary>
		/// <remarks>For default implementations, this is the only supported sparse vector format</remarks>
		Coordinated = 1 << 0,
	}

	/// <summary>
	/// The <see cref="SparseMatrixFormat"/> enum indicates the format specification of a sparse matrix. Each bit flag indicates an atomic format.
	/// </summary>
	/// <remarks>All the internally defined formats are 3-array variations rather than 4-array ones.</remarks>
	[Flags]
	public enum SparseMatrixFormat : int
	{
		/// <summary>
		/// Coordinate Format (COO) that stores each non-zero element's <c>x</c> and <c>y</c> coordinates which are sorted in the row-first order.
		/// </summary>
		COOR = 1 << 0,
		/// <summary>
		/// Coordinate Format (COO) that stores each non-zero element's <c>x</c> and <c>y</c> coordinates which are sorted in the column-first order. The transpose of a <see cref="COOR"/> format is the <see cref="COOC"/> format.
		/// </summary>
		COOC = 1 << 1,
		/// <summary>
		/// Compressed Sparse Row Format (CSR). The only way the <see cref="CSR"/> differs from the <see cref="COOR"/> format is that the array containing the row indices is compressed, that is, the row index array only stores the end-of-row offsets with <c>size == number_of_rows + 1</c> whose first element is 0.
		/// </summary>
		CSR = 1 << 2,
		/// <summary>
		/// Compressed Sparse Column Format (CSC). The only way the <see cref="CSC"/> differs from the <see cref="CSR"/> format is that the column index array instead of row index array stores the end-of-column (not end-of-row) offsets. The transpose of a <see cref="CSR"/> format is the <see cref="CSC"/> format.
		/// </summary>
		CSC = 1 << 3,
		/// <summary>
		/// Block Sparse Row Format (BSR). The only way the <see cref="BSR"/> differs from the <see cref="CSR"/> format is that instead of indexing values, <see cref="BSR"/> indexes the dense block sub-matrices. Therefore, this requires additional parameters: number of non-zero blocks instead of non-zero values, number of block matrix rows, number of block matrix columns, end-of-row offsets are counted in blocks and therefore is of <c>size == number_of_rows / block_rows + 1</c>.
		/// </summary>
		BSR = 1 << 4,
		/// <summary>
		/// Block Sparse Column Format (BSC). The only way the <see cref="BSC"/> differs from the <see cref="BSR"/> format is that the column index array instead of row index array stores the end-of-column (not end-of-row) offsets of blocks. The transpose of a <see cref="BSR"/> format is the <see cref="BSC"/> format.
		/// </summary>
		BSC = 1 << 5,
	}
	#endregion


	#region extension methods
	/// <summary>
	/// The static class for extension methods of <see cref="SparseVectorFormat"/> and <see cref="SparseMatrixFormat"/>
	/// </summary>
	public static class FormatExtension
	{
		#region constants
		/// <summary>
		/// The coordinated formats for sparse matrices.
		/// </summary>
		public const SparseMatrixFormat Coordinated = SparseMatrixFormat.COOR | SparseMatrixFormat.COOC;

		/// <summary>
		/// The compressed formats for sparse matrices.
		/// </summary>
		public const SparseMatrixFormat Compressed = SparseMatrixFormat.CSR | SparseMatrixFormat.CSC | SparseMatrixFormat.BSR | SparseMatrixFormat.BSC;

		/// <summary>
		/// The row majored formats for sparse matrices.
		/// </summary>
		public const SparseMatrixFormat RowMajor = SparseMatrixFormat.COOR | SparseMatrixFormat.CSR | SparseMatrixFormat.BSR;

		/// <summary>
		/// The column majored formats for sparse matrices.
		/// </summary>
		public const SparseMatrixFormat ColumnMajor = SparseMatrixFormat.COOC | SparseMatrixFormat.CSC | SparseMatrixFormat.BSC;

		/// <summary>
		/// The internally defined formats for sparse matrices.
		/// </summary>
		public const SparseMatrixFormat PreDefined = RowMajor | ColumnMajor;


		/// <summary>
		/// All of the possible <see cref="SparseVectorFormat"/>s. Value = 111...111
		/// </summary>
		/// <remarks>Since the atom formats are all orthogonal in binary, this kind of definition becomes useful.<br/>
		/// If some custom formats are defined afterwards, this trick should still be used.</remarks>
		public const SparseVectorFormat VectorAny = (SparseVectorFormat)~0;

		/// <summary>
		/// All of the possible <see cref="SparseMatrixFormat"/>s. Value = 111...111
		/// </summary>
		/// <remarks>Since the atom formats are all orthogonal in binary, this kind of definition becomes useful.<br/>
		/// If some custom formats are defined afterwards, this trick should still be used.</remarks>
		public const SparseMatrixFormat MatrixAny = (SparseMatrixFormat)~0;
		#endregion

		#region methods
		/// <summary>
		/// Check whether the given <see cref="SparseVectorFormat"/> is an atomic format or not
		/// </summary>
		/// <param name="format">The given <see cref="SparseVectorFormat"/> to check</param>
		/// <returns>True if <paramref name="format"/> is an atomic format, i.e. a power of two; false otherwise.</returns>
		public static bool IsAtomic(this SparseVectorFormat format) => ((int)format).IsPowerOfTwo();

		/// <summary>
		/// Check whether the given <see cref="SparseMatrixFormat"/> is an atomic format or not
		/// </summary>
		/// <param name="format">The given <see cref="SparseMatrixFormat"/> to check</param>
		/// <returns>True if <paramref name="format"/> is an atomic format, i.e. a power of two; false otherwise.</returns>
		public static bool IsAtomic(this SparseMatrixFormat format) => ((int)format).IsPowerOfTwo();

		/// <summary>
		/// Check whether the given <see cref="SparseMatrixFormat"/> is a row major format or not
		/// </summary>
		/// <param name="format">The given <see cref="SparseMatrixFormat"/> to check</param>
		/// <returns>True if <paramref name="format"/> is a row major format, i.e. <c>log2(<paramref name="format"/>) % 2 == 0</c> ; false otherwise.</returns>
		public static bool IsRowMajor(this SparseMatrixFormat format) => ((int)format).Log2() % 2 == 0;
		#endregion
	}
	#endregion
}
