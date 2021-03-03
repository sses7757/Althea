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
		/// Encapsulates a method that receives the input of lengths of value, row index and column index arrays and check whether they obey the underlying regulation of underlying format.
		/// </summary>
		/// <param name="rows">The number of rows of given sparse matrix</param>
		/// <param name="columns">The number of columns of given sparse matrix</param>
		/// <param name="valueLength">The length of the value array in <see cref="long"/></param>
		/// <param name="rowLength">The length of the row index array in <see cref="long"/></param>
		/// <param name="columnLength">The length of the column index array in <see cref="long"/></param>
		/// <returns>True if <paramref name="valueLength"/>, <paramref name="rowLength"/> and <paramref name="columnLength"/> obey the underlying regulation of underlying format, false otherwise.</returns>
		public delegate bool CheckLengthRegulationDelegate(long rows, long columns, long valueLength, long rowLength, long columnLength);

		private static readonly Dictionary<SparseMatrixFormat, CheckLengthRegulationDelegate> cache_regulations = new();

		/// <summary>
		/// Set the length regulation of given <paramref name="format"/> by indicating length-regulation-check function.
		/// </summary>
		/// <param name="format">The <b>atomic</b> <see cref="SparseMatrixFormat"/> to indicate the length regulation</param>
		/// <param name="checkFunc">The <see cref="CheckLengthRegulationDelegate"/> used to check the lengths. It is highly recommended that static method is used to create this parameter.</param>
		/// <returns>True if the length regulation is set successfully, false otherwise.</returns>
		public static bool SetLengthRegulation(this SparseMatrixFormat format, CheckLengthRegulationDelegate checkFunc)
		{
			if (!format.IsAtomic())
				return false;
			if (format <= SparseMatrixFormat.CSC)
				return false;
			if (checkFunc is null)
				return false;
			cache_regulations[format] = checkFunc;
			return true;
		}

		/// <summary>
		/// Check whether the lengths of value, row index and column index arrays obey the underlying regulation of the given <paramref name="format"/>.
		/// </summary>
		/// <param name="format">The <b>atomic</b> <see cref="SparseMatrixFormat"/> to indicate which length regulation to use</param>
		/// <param name="rows">The number of rows of given sparse matrix</param>
		/// <param name="columns">The number of columns of given sparse matrix</param>
		/// <param name="valueLength">The length of the value array in <see cref="long"/></param>
		/// <param name="rowLength">The length of the row index array in <see cref="long"/></param>
		/// <param name="columnLength">The length of the column index array in <see cref="long"/></param>
		/// <returns>True if <paramref name="valueLength"/>, <paramref name="rowLength"/> and <paramref name="columnLength"/> obey the underlying regulation of underlying format, false otherwise.</returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="format"/> is atomic; or <paramref name="rows"/> or <paramref name="columns"/> or <paramref name="valueLength"/> or <paramref name="rowLength"/> or <paramref name="columnLength"/> is not positive</exception>
		/// <exception cref="InvalidOperationException">If <paramref name="format"/>'s regulation is neither internally known nor indicated by <see cref="SetLengthRegulation"/></exception>
		public static bool CheckLengthRegulation(this SparseMatrixFormat format, long rows, long columns, long valueLength, long rowLength, long columnLength)
		{
			if (!format.IsAtomic())
				throw new ArgumentOutOfRangeException(nameof(format), format, Resources.Parameter.InvalidValue);
			if (rows <= 0)
				throw new ArgumentOutOfRangeException(nameof(rows), rows, Resources.Parameter.MustPositive);
			if (columns <= 0)
				throw new ArgumentOutOfRangeException(nameof(columns), columns, Resources.Parameter.MustPositive);
			if (valueLength <= 0)
				throw new ArgumentOutOfRangeException(nameof(valueLength), valueLength, Resources.Parameter.MustPositive);
			if (rowLength <= 0)
				throw new ArgumentOutOfRangeException(nameof(rowLength), rowLength, Resources.Parameter.MustPositive);
			if (columnLength <= 0)
				throw new ArgumentOutOfRangeException(nameof(columnLength), columnLength, Resources.Parameter.MustPositive);

			switch (format)
			{
				case SparseMatrixFormat.COOR:
				case SparseMatrixFormat.COOC:
					return valueLength <= rows * columns && valueLength == rowLength && valueLength == columnLength;
				case SparseMatrixFormat.CSR:
					return valueLength <= rows * columns && rowLength == rows + 1 && valueLength == columnLength;
				case SparseMatrixFormat.CSC:
					return valueLength <= rows * columns && columnLength == columns + 1 && valueLength == rowLength;
				default:
					if (cache_regulations.ContainsKey(format))
						return cache_regulations[format].Invoke(rows, columns, valueLength, rowLength, columnLength);
					else
						throw new InvalidOperationException();
			}
		}
		#endregion
	}
	#endregion
}
