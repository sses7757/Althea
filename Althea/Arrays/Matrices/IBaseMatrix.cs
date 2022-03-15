using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Althea.Helpers;
using Althea.LinearAlgebra;
using Althea.NativeTypes;


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
		/// When implemented by a derived class, get a sub-matrix by the row and column index ranges and copy it to <paramref name="overwrite"/>.
		/// </summary>
		/// <param name="offsetRow">The starting offset of the row to take</param>
		/// <param name="countRow">The number of the rows to take</param>
		/// <param name="offsetCol">The starting offset of the columns to take</param>
		/// <param name="countCol">The number of the columns to take</param>
		/// <param name="overwrite">The <typeparamref name="TSelf"/> to be overwritten</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offsetRow"/> or <paramref name="countRow"/> or <paramref name="offsetCol"/> or <paramref name="countCol"/> is out of range</exception>
		/// <exception cref="ArgumentException">If <paramref name="overwrite"/> cannot be overwritten</exception>
		void GetSubmatrix(long offsetRow, long countRow, long offsetCol, long countCol, TSelf overwrite);

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
				throw new ArgumentOutOfRangeException(nameof(row), row, Resources.Parameter.CannotNegative);
			if (row >= this.NRows)
				throw new ArgumentOutOfRangeException(nameof(row), row, Resources.Parameter.InvalidValue);
			if (col < 0)
				throw new ArgumentOutOfRangeException(nameof(col), col, Resources.Parameter.CannotNegative);
			if (col >= this.NCols)
				throw new ArgumentOutOfRangeException(nameof(col), col, Resources.Parameter.InvalidValue);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private (long row, long col) CheckIndex(Index row, Index col)
		{
			long rowPos = row.GetPosition(this.NRows), colPos = col.GetPosition(this.NCols);
			this.CheckIndex(rowPos, colPos);
			return (rowPos, colPos);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private (long offsetRow, long countRow, long offsetCol, long countCol) CheckRange(Range row, Range col, TSelf? sub = null)
		{
			var (offsetRow, countRow) = row.GetOffsetAndCount(this.NRows);
			var (offsetCol, countCol) = col.GetOffsetAndCount(this.NCols);
			MatrixSliceWrapper.Create(offsetRow, countRow, offsetCol, countCol, this, sub);
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
				var (offsetRow, countRow, offsetCol, countCol) = this.CheckRange(x, y, value);
				this.SetSubmatrix(offsetRow, countRow, offsetCol, countCol, value);
			}
		}
		#endregion

		#region defined operators
		/// <summary>
		/// When implemented by a derived class, create a new <typeparamref name="TSelf"/> which is the point-wise negation result of the given <paramref name="matrix"/>.
		/// </summary>
		/// <param name="matrix">The input <typeparamref name="TSelf"/> whose elements will be used</param>
		/// <returns>A new <typeparamref name="TSelf"/> as the negation of <paramref name="matrix"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="matrix"/> is null or empty</exception>
		public abstract static TSelf operator -(TSelf matrix);

		/// <summary>
		/// When implemented by a derived class, create a new <typeparamref name="TSelf"/> which is the point-wise multiplication result of the given <paramref name="matrix"/> and <paramref name="scalar"/>.
		/// </summary>
		/// <param name="matrix">The input <typeparamref name="TSelf"/> whose elements will be used</param>
		/// <param name="scalar">The input scalar used as the multiplier</param>
		/// <returns>A new <typeparamref name="TSelf"/> as the result of <paramref name="matrix"/> * <paramref name="scalar"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="matrix"/> is null or empty</exception>
		public abstract static TSelf operator *(TSelf matrix, T scalar);

		/// <summary>
		/// When implemented by a derived class, create a new <typeparamref name="TSelf"/> which is the point-wise multiplication result of the given <paramref name="matrix"/> and <paramref name="scalar"/>.
		/// </summary>
		/// <param name="matrix">The input <typeparamref name="TSelf"/> whose elements will be used</param>
		/// <param name="scalar">The input scalar used as the multiplier</param>
		/// <returns>A new <typeparamref name="TSelf"/> as the result of <paramref name="matrix"/> * <paramref name="scalar"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="matrix"/> is null or empty</exception>
		public abstract static TSelf operator *(T scalar, TSelf matrix);

		/// <summary>
		/// When implemented by a derived class, create a new <typeparamref name="TSelf"/> which is the point-wise division result of the given <paramref name="matrix"/> and <paramref name="scalar"/>.
		/// </summary>
		/// <param name="matrix">The input <typeparamref name="TSelf"/> whose elements will be used</param>
		/// <param name="scalar">The input scalar used as the divider</param>
		/// <returns>A new <typeparamref name="TSelf"/> as the result of <paramref name="matrix"/> / <paramref name="scalar"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="matrix"/> is null or empty</exception>
		public abstract static TSelf operator /(TSelf matrix, T scalar);

		/// <summary>
		/// When implemented by a derived class, create a new <typeparamref name="TSelf"/> which is the simple operation result of the given <paramref name="matrix"/> under <paramref name="operation"/>.
		/// </summary>
		/// <param name="matrix">The input <typeparamref name="TSelf"/> whose elements will be used</param>
		/// <param name="operation">The input <see cref="MatrixOperation"/> used as the operation</param>
		/// <returns>A new <typeparamref name="TSelf"/> as the result of <paramref name="operation"/>(<paramref name="matrix"/>)</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="matrix"/> is null or empty</exception>
		public abstract static TSelf operator ^(TSelf matrix, MatrixOperation operation);

		/// <summary>
		/// When implemented by a derived class, create a new <typeparamref name="TSelf"/> which is the point-wise addition result of the given <paramref name="left"/> and <paramref name="right"/> matrices.
		/// </summary>
		/// <param name="left">The input left <typeparamref name="TSelf"/> to be added</param>
		/// <param name="right">The input right <typeparamref name="TSelf"/> to be added</param>
		/// <returns>A new <typeparamref name="TSelf"/> as the result of <paramref name="left"/> + <paramref name="right"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="left"/> or <paramref name="right"/> is null or empty</exception>
		/// <exception cref="ArgumentException">If the addition cannot be performed due to incompatible sizes</exception>
		public abstract static TSelf operator +(TSelf left, TSelf right);

		/// <summary>
		/// When implemented by a derived class, create a new <typeparamref name="TSelf"/> which is the point-wise subtraction result of the given <paramref name="left"/> and <paramref name="right"/> matrices.
		/// </summary>
		/// <param name="left">The input left <typeparamref name="TSelf"/> to be subtracted from</param>
		/// <param name="right">The input right <typeparamref name="TSelf"/> to subtract</param>
		/// <returns>A new <typeparamref name="TSelf"/> as the result of <paramref name="left"/> - <paramref name="right"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="left"/> or <paramref name="right"/> is null or empty</exception>
		/// <exception cref="ArgumentException">If the subtraction cannot be performed due to incompatible sizes</exception>
		public abstract static TSelf operator -(TSelf left, TSelf right);

		/// <summary>
		/// When implemented by a derived class, create a new <typeparamref name="TSelf"/> which is the matrix multiplication result of the given <paramref name="left"/> and <paramref name="right"/> matrices.
		/// </summary>
		/// <param name="left">The input <typeparamref name="TSelf"/> to be multiplied at left</param>
		/// <param name="right">The input <typeparamref name="TSelf"/> to be multiplied at right</param>
		/// <returns>A new <typeparamref name="TSelf"/> as the result of <paramref name="left"/> * <paramref name="right"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="left"/> or <paramref name="right"/> is null or empty</exception>
		/// <exception cref="ArgumentException">If the multiplication cannot be performed due to incompatible sizes</exception>
		public abstract static TSelf operator *(TSelf left, TSelf right);
		#endregion

		#region linear algebra
		/// <summary>
		/// When implemented by a derived class, overwrite which is the point-wise addition result of this matrix the <paramref name="other"/> matrix.
		/// </summary>
		/// <param name="scalarThis">The scalar to multiply to this matrix before addition</param>
		/// <param name="scalarOther">The scalar to multiply to the <paramref name="other"/> matrix before addition</param>
		/// <param name="other">The input right <typeparamref name="TSelf"/> to be added</param>
		/// <param name="opThis">The <see cref="MatrixOperation"/> to apply to this matrix before addition</param>
		/// <param name="opOther">The <see cref="MatrixOperation"/> to apply to the <paramref name="other"/> matrix before addition</param>
		/// <returns>A new <typeparamref name="TSelf"/> as the result of <c><paramref name="scalarThis"/> * <paramref name="opThis"/>(this) + <paramref name="scalarOther"/> * <paramref name="opOther"/>(<paramref name="other"/>)</c></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="other"/> is null or empty</exception>
		/// <exception cref="NotSupportedException">If the given <paramref name="opThis"/> or <paramref name="opOther"/> is not supported</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="scalarThis"/> or <paramref name="scalarOther"/> is 0</exception>
		/// <exception cref="ArgumentException">If the addition cannot be performed due to incompatible sizes</exception>
		TSelf ReplaceByMatrixAddition(TSelf A, T scalarA, T scalarB, TSelf B, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None);

		/// <summary>
		/// When implemented by a derived class, create a new <typeparamref name="TSelf"/> which is the multiplication result of this matrix and the <paramref name="other"/> matrix.
		/// </summary>
		/// <param name="scalar">The scalar to multiply to the result</param>
		/// <param name="other">The input right <typeparamref name="TSelf"/> to be multiplied</param>
		/// <param name="opThis">The <see cref="MatrixOperation"/> to apply to this matrix before addition</param>
		/// <param name="opOther">The <see cref="MatrixOperation"/> to apply to the <paramref name="other"/> matrix before addition</param>
		/// <returns>A new <typeparamref name="TSelf"/> as the result of <c><paramref name="scalar"/> * <paramref name="opThis"/>(this) * <paramref name="opOther"/>(<paramref name="other"/>)</c></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="other"/> is null or empty</exception>
		/// <exception cref="NotSupportedException">If the given <paramref name="opThis"/> or <paramref name="opOther"/> is not supported</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="scalar"/> is 0</exception>
		/// <exception cref="ArgumentException">If the multiplication cannot be performed due to incompatible sizes</exception>
		public abstract TSelf MultiplyMatrix(T scalar, TSelf other, MatrixOperation opThis = MatrixOperation.None, MatrixOperation opOther = MatrixOperation.None);
		#endregion
	}
}