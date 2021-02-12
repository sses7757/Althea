using System;
using System.Collections.Generic;

using Althea.Arrays;
using Althea.Helpers;


namespace Althea.LinearAlgebra.Sparse
{
	#region enum
	/// <summary>
	/// The <see cref="SparseMatrixFormat"/> enum indicates the format specification of a sparse matrix
	/// </summary>
	[Flags]
	public enum SparseMatrixFormat : long
	{
		/// <summary>
		/// Coordinate Format (COO) that stores each non-zero element's <c>x</c> and <c>y</c> coordinates which are sorted in the row-first order.
		/// </summary>
		COOR = 1 << 0,
		/// <summary>
		/// Coordinate Format (COO) that stores each non-zero element's <c>x</c> and <c>y</c> coordinates which are sorted in the column-first order.
		/// </summary>
		COOC = 1 << 1,
		/// <summary>
		/// Compressed Sparse Row Format (CSR). The only way the <see cref="CSR"/> differs from the <see cref="COOR"/> format is that the array containing the row indices is compressed, that is, the row index array only stores the end-of-row offsets of <c>size == number_of_rows + 1</c> of the value array.
		/// </summary>
		CSR = 1 << 2,
		/// <summary>
		/// Compressed Sparse Column Format (CSC). The only way the <see cref="CSC"/> differs from the <see cref="CSR"/> format is that the column index array instead of row indices array stores the end-of-column (not end-of-row) offsets.
		/// </summary>
		CSC = 1 << 3,
		/// <summary>
		/// Block Sparse Row Format (BSR). The only way the <see cref="BSR"/> differs from the <see cref="CSR"/> format is that instead of indexing values, <see cref="BSR"/> indexes the dense block sub-matrices. Therefore, this requires additional parameters: number of non-zero blocks instead of non-zero values, number of block matrix rows, number of block matrix columns, end-of-row offsets are counted in blocks and therefore is of <c>size == number_of_rows / block_rows + 1</c>.
		/// </summary>
		BSR = 1 << 4,
	}

	/// <summary>
	/// The static class for extension methods of <see cref="SparseMatrixFormat"/>
	/// </summary>
	public static class SparseMatrixFormatExtension
	{
		/// <summary>
		/// The coordinated formats.
		/// </summary>
		public const SparseMatrixFormat Coordinated = SparseMatrixFormat.COOR | SparseMatrixFormat.COOC;

		/// <summary>
		/// The compressed formats.
		/// </summary>
		public const SparseMatrixFormat Compressed = SparseMatrixFormat.CSR | SparseMatrixFormat.CSC | SparseMatrixFormat.BSR;

		/// <summary>
		/// The row majored formats.
		/// </summary>
		public const SparseMatrixFormat RowMajor = SparseMatrixFormat.COOR | SparseMatrixFormat.CSR | SparseMatrixFormat.BSR;

		/// <summary>
		/// The column majored formats.
		/// </summary>
		public const SparseMatrixFormat ColumnMajor = SparseMatrixFormat.COOC | SparseMatrixFormat.CSC;


		/// <summary>
		/// All of the possible <see cref="SparseMatrixFormat"/>s. Value = 111...111
		/// </summary>
		/// <remarks>Since the atom formats are all orthogonal in binary, this kind of definition becomes useful.<br/>
		/// If some custom formats are defined afterwards, this trick should still be used.</remarks>
		public const SparseMatrixFormat Any = (SparseMatrixFormat)~0;

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

		private static readonly Dictionary<SparseMatrixFormat, CheckLengthRegulationDelegate> cache_regulations = new Dictionary<SparseMatrixFormat, CheckLengthRegulationDelegate>();

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
				throw new ArgumentOutOfRangeException(nameof(format), Resources.Parameter.InvalidValue);
			if (rows <= 0)
				throw new ArgumentOutOfRangeException(nameof(rows), Resources.Parameter.MustPositive);
			if (columns <= 0)
				throw new ArgumentOutOfRangeException(nameof(columns), Resources.Parameter.MustPositive);
			if (valueLength <= 0)
				throw new ArgumentOutOfRangeException(nameof(valueLength), Resources.Parameter.MustPositive);
			if (rowLength <= 0)
				throw new ArgumentOutOfRangeException(nameof(rowLength), Resources.Parameter.MustPositive);
			if (columnLength <= 0)
				throw new ArgumentOutOfRangeException(nameof(columnLength), Resources.Parameter.MustPositive);

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
	}
	#endregion


	#region wrapper
	/// <summary>
	/// The sparse vector wrapper
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
	public readonly struct SparseVectorWrapper<T> : IEquatable<SparseVectorWrapper<T>>, ICheckValid where T : unmanaged
	{
		/// <summary>
		/// value array
		/// </summary>
		public Storage<T> Values { get; }

		/// <summary>
		/// index array
		/// </summary>
		public Storage<int> Indices { get; }

		/// <summary>
		/// Check whether this object is a valid one or not
		/// </summary>
		/// <returns>The validness of this object</returns>
		public bool IsValid() => this.Values.IsValid() && this.Indices.IsValid();

		/// <summary>
		/// Simple constructor from given value array <paramref name="val"/> and index array <paramref name="ind"/>
		/// </summary>
		/// <param name="val">The value array storage as a <see cref="Storage{T}"/> of <typeparamref name="T"/></param>
		/// <param name="ind">The index array storage as a <see cref="Storage{T}"/> of <see cref="int"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="ind"/> or <paramref name="val"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="ind"/> and <paramref name="val"/> have different length</exception>
		public SparseVectorWrapper(Storage<T> val, Storage<int> ind)
		{
			if (val is null || !val.IsValid())
				throw new ArgumentNullException(nameof(val));
			if (ind is null || !ind.IsValid())
				throw new ArgumentNullException(nameof(ind));
			if (val.Length != ind.Length)
				throw new ArgumentException(Resources.Parameter.NotSameSize);
			this.Values = val;
			this.Indices = ind;
		}

		/// <summary>
		/// Indicates whether this instance and another <see cref="SparseVectorWrapper{T}"/> are equal.
		/// </summary>
		/// <param name="other">The other <see cref="SparseVectorWrapper{T}"/></param>
		/// <returns>True if obj and this instance are the same type and represent the same value</returns>
		public bool Equals(SparseVectorWrapper<T> other) => this.Values == other.Values && this.Indices == other.Indices;

		/// <summary>
		/// Indicates whether this instance and a specified object are equal.
		/// </summary>
		/// <param name="obj">The object to compare with the current instance.</param>
		/// <returns>True if obj and this instance are the same type and represent the same value</returns>
		public override bool Equals(object? obj)
		{
			if (obj is SparseVectorWrapper<T> sv)
				return this.Equals(sv);
			else
				return false;
		}

		/// <summary>
		/// Get hash code
		/// </summary>
		/// <returns>the hash code</returns>
		public override int GetHashCode() => HashCode.Combine(this.Values, this.Indices);

		/// <summary>
		/// Equality operator
		/// </summary>
		public static bool operator ==(SparseVectorWrapper<T> left, SparseVectorWrapper<T> right)
		{
			return left.Equals(right);
		}

		/// <summary>
		/// Inequality operator
		/// </summary>
		public static bool operator !=(SparseVectorWrapper<T> left, SparseVectorWrapper<T> right)
		{
			return !(left == right);
		}
	}

	/// <summary>
	/// The sparse matrix wrapper
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
	public readonly struct SparseMatrixWrapper<T> : IEquatable<SparseMatrixWrapper<T>>, ICheckValid where T : unmanaged
	{
		/// <summary>
		/// Get the value array
		/// </summary>
		public Storage<T> Values { get; }

		/// <summary>
		/// Get the row index/pointer array
		/// </summary>
		public Storage<int> Row { get; }

		/// <summary>
		/// Get the column index/pointer array
		/// </summary>
		public Storage<int> Column { get; }

		/// <summary>
		/// Get the sparse format
		/// </summary>
		public SparseMatrixFormat Format { get; }

		/// <summary>
		/// Check whether this object is a valid one or not
		/// </summary>
		/// <returns>The validness of this object</returns>
		public bool IsValid() => this.Values.IsValid() && this.Row.IsValid() && this.Column.IsValid();

		/// <summary>
		/// Simple constructor from given value array <paramref name="val"/> and index arrays <paramref name="row"/> and <paramref name="column"/>
		/// </summary>
		/// <param name="val">The value array storage as a <see cref="Storage{T}"/> of <typeparamref name="T"/></param>
		/// <param name="row">The row index array storage as a <see cref="Storage{T}"/> of <see cref="int"/></param>
		/// <param name="column">The column index array storage as a <see cref="Storage{T}"/> of <see cref="int"/></param>
		/// <param name="format">The sparse matrix format as a <see cref="SparseMatrixFormat"/></param>
		/// <exception cref="ArgumentOutOfRangeException">If </exception>
		/// <exception cref="ArgumentNullException">If <paramref name="row"/> or <paramref name="column"/> or <paramref name="val"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="row"/> and <paramref name="column"/> and <paramref name="val"/> have lengths not the same as the regulation implied in <paramref name="format"/></exception>
		public SparseMatrixWrapper(Storage<T> val, Storage<int> row, Storage<int> column, SparseMatrixFormat format)
		{
			if (!format.IsAtomic())
				throw new ArgumentOutOfRangeException(nameof(format), Resources.Parameter.InvalidValue);
			if (val is null || !val.IsValid())
				throw new ArgumentNullException(nameof(val));
			if (row is null || !row.IsValid())
				throw new ArgumentNullException(nameof(row));
			if (column is null || !column.IsValid())
				throw new ArgumentNullException(nameof(column));

			this.Values = val;
			this.Row = row;
			this.Column = column;
			this.Format = format;
		}

		/// <summary>
		/// Indicates whether this instance and another <see cref="SparseMatrixWrapper{T}"/> are equal.
		/// </summary>
		/// <param name="other">The other <see cref="SparseMatrixWrapper{T}"/></param>
		/// <returns>True if obj and this instance are the same type and represent the same value</returns>
		public bool Equals(SparseMatrixWrapper<T> other) => this.Values == other.Values && this.Row == other.Row && this.Column == other.Column && this.Format == other.Format;

		/// <summary>
		/// Indicates whether this instance and a specified object are equal.
		/// </summary>
		/// <param name="obj">The object to compare with the current instance.</param>
		/// <returns>True if obj and this instance are the same type and represent the same value</returns>
		public override bool Equals(object? obj)
		{
			if (obj is SparseMatrixWrapper<T> sm)
				return this.Equals(sm);
			else
				return false;
		}

		/// <summary>
		/// Get hash code
		/// </summary>
		/// <returns>the hash code</returns>
		public override int GetHashCode() => HashCode.Combine(this.Values, this.Row, this.Column, this.Format);

		/// <summary>
		/// Equality operator
		/// </summary>
		public static bool operator ==(SparseMatrixWrapper<T> left, SparseMatrixWrapper<T> right)
		{
			return left.Equals(right);
		}

		/// <summary>
		/// Inequality operator
		/// </summary>
		public static bool operator !=(SparseMatrixWrapper<T> left, SparseMatrixWrapper<T> right)
		{
			return !(left == right);
		}
	}
	#endregion


	#region converter
	/// <summary>
	/// The static class which contains converter methods
	/// </summary>
	public static class Converter
	{
		/// <summary>
		/// Convert the given <paramref name="vector"/> of type <see cref="ISparseVector{T, Int32}"/> to a <see cref="SparseVectorWrapper{T}"/>
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="vector">The input sparse vector as a <see cref="ISparseVector{T, Int32}"/></param>
		/// <returns>The created <see cref="SparseVectorWrapper{T}"/></returns>
		public static SparseVectorWrapper<T> ToWrapper<T>(this ISparseVector<T, int> vector) where T : unmanaged, IEquatable<T>
		{
			return new SparseVectorWrapper<T>(vector.Storage, vector.IndexStorage);
		}

		/// <summary>
		/// Convert the given <paramref name="matrix"/> of type <see cref="ISparseMatrix{T, Int32}"/> to a <see cref="SparseMatrixWrapper{T}"/>
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="matrix">The input sparse vector as a <see cref="ISparseMatrix{T, Int32}"/></param>
		/// <returns>The created <see cref="SparseMatrixWrapper{T}"/></returns>
		public static SparseMatrixWrapper<T> ToWrapper<T>(this ISparseMatrix<T, int> matrix) where T : unmanaged, IEquatable<T>
		{
			return new SparseMatrixWrapper<T>(matrix.Storage, matrix.RowIndexStorage, matrix.ColumnIndexStorage);
		}
	}
	#endregion
}
