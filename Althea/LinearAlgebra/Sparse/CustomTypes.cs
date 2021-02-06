using System;


namespace Althea.LinearAlgebra.Sparse
{
	/// <summary>
	/// The <see cref="SparseMatrixFormat"/> enum indicates the format specification of a sparse matrix
	/// </summary>
	[Flags]
	public enum SparseMatrixFormat
	{
		/// <summary>
		/// Coordinate Format (COO) that stores each non-zero element's <c>x</c> and <c>y</c> coordinates which are sorted in the row-first order. Value = 000...0001
		/// </summary>
		COOR = 1 << 0,

		/// <summary>
		/// Coordinate Format (COO) that stores each non-zero element's <c>x</c> and <c>y</c> coordinates which are sorted in the column-first order. Value = 000...0010
		/// </summary>
		COOC = 1 << 1,

		/// <summary>
		/// Since <see cref="COOC"/> and <see cref="COOR"/> are so similar that it can be generalized to the Coordinate Format (COO). Value = 000...0011
		/// </summary>
		/// <remarks>Since the atom formats are all orthogonal in binary, this kind of definition becomes useful.<br/>
		/// If some custom formats are defined afterwards, this trick should still be used.</remarks>
		Coordinated = COOR | COOC,

		/// <summary>
		/// Compressed Sparse Row Format (CSR). The only way the CSR differs from the COO format is that the array containing the row indices is compressed in CSR format, that is, the row index array only stores the <c>LeadDim + 1</c> the end-of-row offsets of the value array. Value = 000...0100
		/// </summary>
		CSR = 1 << 2,

		/// <summary>
		/// Compressed Sparse Column Format (CSC). The only way the CSC differs from the CSR format is that the column index array instead of row indices array stores the end-of-row offsets. Value = 000...01000
		/// </summary>
		CSC = 1 << 3,

		/// <summary>
		/// Since <see cref="CSR"/> and <see cref="CSC"/> are so similar that it can be generalized to the Compressed Format. Value = 000...01100
		/// </summary>
		/// <remarks>Since the atom formats are all orthogonal in binary, this kind of definition becomes useful.<br/>
		/// If some custom formats are defined afterwards, this trick should still be used.</remarks>
		Compressed = CSR | CSC,

		/// <summary>
		/// The row majored formats. Value = 000...0101
		/// </summary>
		RowMajor = COOR | CSR,

		/// <summary>
		/// The column majored formats. Value = 000...01010
		/// </summary>
		ColumnMajor = COOC | CSC,

		/// <summary>
		/// Any of the atomic formats. Value = 111...111
		/// </summary>
		/// <remarks>Since the atom formats are all orthogonal in binary, this kind of definition becomes useful.<br/>
		/// If some custom formats are defined afterwards, this trick should still be used.</remarks>
		Any = ~0
	}
}
