using System;
using System.Runtime.CompilerServices;

using Althea.Helpers;
using Althea.LinearAlgebra;


namespace Althea.Arrays
{
	/// <summary>
	/// The base matrix interface.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TSelf">The concrete type that implements this <see cref="IBaseMatrix{T, TSelf}"/></typeparam>
	public interface IBaseMatrix<T, TSelf> : IMatrixMetric where T : unmanaged, INumber<T> where TSelf : class, IBaseMatrix<T, TSelf>
	{
		#region basic
		/// <summary>
		/// When implemented by a derived class, get a sub-matrix by the row and column index ranges.
		/// </summary>
		/// <param name="offsetRow">The starting offset of the row to take</param>
		/// <param name="countRow">The number of the rows to take</param>
		/// <param name="offsetCol">The starting offset of the columns to take</param>
		/// <param name="countCol">The number of the columns to take</param>
		/// <returns>The sub-matrix (may be a referenced one) in the region indicated by the ranges</returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offsetRow"/> or <paramref name="countRow"/> or <paramref name="offsetCol"/> or <paramref name="countCol"/> is out of range</exception>
		TSelf GetSubmatrix(long offsetRow, long countRow, long offsetCol, long countCol);

		/// <summary>
		/// When implemented by a derived class, copy this matrix's elements to <paramref name="destination"/>'s ones.
		/// </summary>
		/// <param name="destination">The destination matrix to copy to</param>
		/// <exception cref="ArgumentException">If <paramref name="destination"/> is not of same size as this one</exception>
		void CopyTo(TSelf destination);

		/// <summary>
		/// When implemented by a derived class, set a sub-matrix by the row and column index ranges with the given <paramref name="value"/>.
		/// </summary>
		/// <param name="offsetRow">The starting offset of the row to take</param>
		/// <param name="countRow">The number of the rows to take</param>
		/// <param name="offsetCol">The starting offset of the columns to take</param>
		/// <param name="countCol">The number of the columns to take</param>
		/// <param name="value">The <typeparamref name="TSelf"/> whose value will overwrite this matrix from (<paramref name="offsetRow"/>, <paramref name="countRow"/>) with size (<paramref name="countRow"/>, <paramref name="countCol"/>)</param>
		/// <exception cref="ArgumentNullException">If <paramref name="value"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offsetRow"/> or <paramref name="countRow"/> or <paramref name="offsetCol"/> or <paramref name="countCol"/> is out of range</exception>
		void SetSubmatrix(long offsetRow, long countRow, long offsetCol, long countCol, TSelf value);

		/// <summary>
		/// Check the row and column indices
		/// </summary>
		/// <param name="row">The row index as a <see cref="long"/></param>
		/// <param name="col">The column index as a <see cref="long"/></param>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="row"/> or <paramref name="col"/> is out of range</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected void CheckIndex(long row, long col)
		{
			if (row < 0)
				throw new ArgumentOutOfRangeException(nameof(row), row, Resources.ParameterError.CannotNegative);
			if (row >= this.NRows)
				throw new ArgumentOutOfRangeException(nameof(row), row, Resources.ParameterError.InvalidValue);
			if (col < 0)
				throw new ArgumentOutOfRangeException(nameof(col), col, Resources.ParameterError.CannotNegative);
			if (col >= this.NCols)
				throw new ArgumentOutOfRangeException(nameof(col), col, Resources.ParameterError.InvalidValue);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private (long row, long col) CheckIndex(Index row, Index col)
		{
			long rowPos = row.GetPosition(this.NRows), colPos = col.GetPosition(this.NCols);
			return (rowPos, colPos);
		}

		/// <summary>
		/// Check the row and column ranges
		/// </summary>
		/// <param name="offsetRow">The starting row index as a <see cref="long"/></param>
		/// <param name="offsetCol">The starting column index as a <see cref="long"/></param>
		/// <param name="countRow">The row count as a <see cref="long"/></param>
		/// <param name="countCol">The column count as a <see cref="long"/></param>
		/// <param name="sub">The sub matrix to check which can be null to prevent checking</param>
		/// <exception cref="ArgumentOutOfRangeException">If the any of the parameters is out of range</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected void CheckRange(long offsetRow, long countRow, long offsetCol, long countCol, IMatrixMetric? sub = null)
		{
			MatrixSliceWrapper.Create(offsetRow, countRow, offsetCol, countCol, this, sub);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private (long offsetRow, long countRow, long offsetCol, long countCol) CheckRange(Range row, Range col)
		{
			var (offsetRow, countRow) = row.GetOffsetAndCount(this.NRows);
			var (offsetCol, countCol) = col.GetOffsetAndCount(this.NCols);
			return (offsetRow, countRow, offsetCol, countCol);
		}

		/// <summary>
		/// When implemented by a derived class, get or set the element at the given position (<paramref name="x"/>, <paramref name="y"/>).
		/// </summary>
		/// <param name="x">The row position as a <see cref="long"/></param>
		/// <param name="y">The column position as a <see cref="long"/></param>
		/// <returns>The element at position (<paramref name="x"/>, <paramref name="y"/>).</returns>
		T this[long x, long y] { get; set; }

		/// <summary>
		/// Get or set the element at the given position (<paramref name="x"/>, <paramref name="y"/>).
		/// </summary>
		/// <param name="x">The row position as an <see cref="Index"/></param>
		/// <param name="y">The column position as an <see cref="Index"/></param>
		/// <returns>The element at position (<paramref name="x"/>, <paramref name="y"/>).</returns>
		T this[Index x, Index y]
		{
			get
			{
				var (row, col) = this.CheckIndex(x, y);
				return this[row, col];
			}
			set
			{
				var (row, col) = this.CheckIndex(x, y);
				this[row, col] = value;
			}
		}

		/// <summary>
		/// Get or set the sub-matrix indicated by <paramref name="x"/> and <paramref name="y"/>.
		/// </summary>
		/// <param name="x">The range of rows to get/set as a <see cref="Range"/></param>
		/// <param name="y">The range of columns to get/set as a <see cref="Range"/></param>
		/// <returns>The sub-matrix in this range, may be a referenced one.</returns>
		TSelf this[Range x, Range y]
		{
			get
			{
				var (offsetRow, countRow, offsetCol, countCol) = this.CheckRange(x, y);
				return this.GetSubmatrix(offsetRow, countRow, offsetCol, countCol);
			}
			set
			{
				var (offsetRow, countRow, offsetCol, countCol) = this.CheckRange(x, y);
				this.SetSubmatrix(offsetRow, countRow, offsetCol, countCol, value);
			}
		}
		#endregion
	}
}