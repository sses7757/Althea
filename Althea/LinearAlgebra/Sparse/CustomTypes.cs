using System;

using Althea.Arrays;


namespace Althea.LinearAlgebra.Sparse
{
	#region enum
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
	#endregion


	#region wrapper
	/// <summary>
	/// The sparse vector wrapper
	/// </summary>
	/// <typeparam name="T">see <see cref="AbstractArray{T}"/></typeparam>
	public readonly struct SparseVectorWrapper<T> : IEquatable<SparseVectorWrapper<T>> where T : unmanaged
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
		/// Simple constructor from given value array <paramref name="val"/> and index array <paramref name="ind"/>
		/// </summary>
		public SparseVectorWrapper(Storage<T> val, Storage<int> ind)
		{
			this.Values = val ?? throw new ArgumentNullException(nameof(val));
			this.Indices = ind ?? throw new ArgumentNullException(nameof(ind));
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
	/// <typeparam name="T">see <see cref="AbstractArray{T}"/></typeparam>
	public readonly struct SparseMatrixWrapper<T> : IEquatable<SparseMatrixWrapper<T>> where T : unmanaged
	{
		/// <summary>
		/// value array
		/// </summary>
		public Storage<T> Values { get; }

		/// <summary>
		/// row index/pointer array
		/// </summary>
		public Storage<int> Row { get; }

		/// <summary>
		/// column index/pointer array
		/// </summary>
		public Storage<int> Column { get; }

		/// <summary>
		/// Simple constructor from given value array <paramref name="val"/> and index arrays <paramref name="row"/> and <paramref name="column"/>
		/// </summary>
		public SparseMatrixWrapper(Storage<T> val, Storage<int> row, Storage<int> column)
		{
			this.Values = val ?? throw new ArgumentNullException(nameof(val));
			this.Row = row ?? throw new ArgumentNullException(nameof(row));
			this.Column = column ?? throw new ArgumentNullException(nameof(column));
		}

		/// <summary>
		/// Indicates whether this instance and another <see cref="SparseMatrixWrapper{T}"/> are equal.
		/// </summary>
		/// <param name="other">The other <see cref="SparseMatrixWrapper{T}"/></param>
		/// <returns>True if obj and this instance are the same type and represent the same value</returns>
		public bool Equals(SparseMatrixWrapper<T> other) => this.Values == other.Values && this.Row == other.Row && this.Column == other.Column;

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
		public override int GetHashCode() => HashCode.Combine(this.Values, this.Row, this.Column);

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
