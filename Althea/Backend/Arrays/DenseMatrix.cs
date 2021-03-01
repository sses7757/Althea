using System;
using System.Collections.Generic;

using Althea.Linq;
using Althea.Arrays;

using MEM = Althea.Storage.AbstractApi;
using LAD = Althea.LinearAlgebra.Dense.AbstractApi;


namespace Althea.Backend.Arrays
{
	/// <summary>
	/// The concrete dense matrix class with the only <see cref="ValueArray{T}.Storage"/> that refers to the data storage.
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct that implements <see cref="IFormattable"/> and <see cref="IEquatable{T}"/> as the data type</typeparam>
	public class DenseMatrix<T> : MatrixBase<T> where T : unmanaged, IFormattable, IEquatable<T>
	{
		#region basic
		/// <summary>
		/// Get the leading dimension (the length in <typeparamref name="T"/> between to consecutive column starting elements) of this dense matrix
		/// </summary>
		public long LeadDim { get; }

		/// <summary>
		/// Get the total number of the visible values in memory, in <typeparamref name="T"/> rather than bytes. This override returns the actual valid length of <see cref="ValueArray{T}.Storage"/>.
		/// </summary>
		public override long ActualLength => this.LeadDim * (this.NCols - 1) + this.NRows;

		/// <summary>
		/// Construct a <see cref="DenseMatrix{T}"/> with value array <paramref name="values"/> and size <paramref name="rows"/>, <paramref name="cols"/>
		/// </summary>
		/// <param name="values"></param>
		/// <param name="rows">The number of rows of this matrix</param>
		/// <param name="cols">The number of columns of this matrix</param>
		/// <param name="leadDim">The leading dimension of this matrix. Default 0 means <paramref name="rows"/>.</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="rows"/> or <paramref name="cols"/> or <paramref name="leadDim"/> is not positive</exception>
		/// <exception cref="ArgumentException">If <paramref name="leadDim"/> is less than <paramref name="rows"/> or the given size exceeds the boundary of <paramref name="values"/></exception>
		public DenseMatrix(Storage<T> values, long rows, long cols, long leadDim = 0) : base(values, rows, cols)
		{
			if (leadDim == 0)
				leadDim = rows;
			if (leadDim < 0)
				throw new ArgumentOutOfRangeException(nameof(leadDim), Resources.Parameter.MustPositive);
			if (leadDim < rows)
				throw new ArgumentException(Resources.Parameter.NotSameSize);
			if (leadDim * (cols - 1) + rows > values.Length)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(values));

			this.LeadDim = leadDim;
		}

		/// <summary>
		/// Create am empty <see cref="DenseMatrix{T}"/>
		/// </summary>
		public DenseMatrix() : base(Storage<T>.Empty, 0, 0)
		{
			this.LeadDim = 0;
		}
		#endregion

		#region basic indexers
		/// <summary>
		/// Get a sub-matrix by the row and column index ranges.
		/// </summary>
		/// <param name="offsetRow">The starting offset of the row to take</param>
		/// <param name="countRow">The number of the rows to take</param>
		/// <param name="offsetCol">The starting offset of the columns to take</param>
		/// <param name="countCol">The number of the columns to take</param>
		/// <returns>The sub-matrix (may be a referenced one) in the region indicated by the ranges</returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offsetRow"/> or <paramref name="countRow"/> or <paramref name="offsetCol"/> or <paramref name="countCol"/> is out of range</exception>
		public override DenseMatrix<T> GetSubmatrix(long offsetRow, long countRow, long offsetCol, long countCol)
		{
			this.CheckRange(offsetRow, countRow, offsetCol, countCol);
			return new DenseMatrix<T>(this.Storage.MakeReference(offsetCol * this.LeadDim + offsetRow), countRow, countCol, this.LeadDim);
		}

		/// <summary>
		/// Get a sub-matrix by the row and column index ranges and copy it to <paramref name="overwrite"/>.
		/// </summary>
		/// <param name="offsetRow">The starting offset of the row to take</param>
		/// <param name="countRow">The number of the rows to take</param>
		/// <param name="offsetCol">The starting offset of the columns to take</param>
		/// <param name="countCol">The number of the columns to take</param>
		/// <param name="overwrite">The <see cref="MatrixBase{T}"/> to be overwritten</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offsetRow"/> or <paramref name="countRow"/> or <paramref name="offsetCol"/> or <paramref name="countCol"/> is out of range</exception>
		/// <exception cref="ArgumentException">If <paramref name="overwrite"/> cannot be overwritten</exception>
		public override void GetSubmatrix(long offsetRow, long countRow, long offsetCol, long countCol, MatrixBase<T> overwrite)
		{
			this.CheckRange(offsetRow, countRow, offsetCol, countCol);
			if (overwrite is null || !overwrite.IsValid())
				throw new ArgumentNullException(nameof(overwrite));
			if (overwrite is not DenseMatrix<T> dense)
				throw new ArgumentException(Resources.Parameter.InvalidValue, nameof(overwrite));
			if (dense.NRows < countRow || dense.NCols < countCol)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(overwrite));

			MEM.MemoryCopy2D(this.Storage.MakeReference(offsetCol * this.LeadDim + offsetRow), this.LeadDim, dense.Storage, dense.LeadDim, countRow, countCol);
		}

		/// <summary>
		/// Set a sub-matrix by the row and column starting index (inclusive).
		/// </summary>
		/// <param name="rowStart">The <see cref="long"/> to indicate the starting row index to set</param>
		/// <param name="columnStart">The <see cref="long"/> to indicate the starting column index to set</param>
		/// <param name="value">The <see cref="MatrixBase{T}"/> whose value will overwrite this matrix from (<paramref name="rowStart"/>, <paramref name="columnStart"/>)</param>
		/// <exception cref="ArgumentNullException">If <paramref name="value"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="rowStart"/> or <paramref name="columnStart"/> and <paramref name="value"/>'s <see cref="MatrixBase{T}.NRows"/> or <see cref="MatrixBase{T}.NCols"/> are out of range</exception>
		/// <exception cref="NotSupportedException">If <paramref name="value"/> is neither a <see cref="DenseMatrix{T}"/> nor a <see cref="ISparseMatrix{T}"/></exception>
		public override void SetSubmatrix(long rowStart, long columnStart, MatrixBase<T> value)
		{
			if (value is null || !value.IsValid())
				throw new ArgumentNullException(nameof(value));
			this.CheckRange(rowStart, columnStart, value.NRows, value.NCols);

			if (value is DenseMatrix<T> dense)
			{
				MEM.MemoryCopy2D(dense.Storage, dense.LeadDim, this.Storage.MakeReference(rowStart * this.LeadDim + columnStart), this.LeadDim, dense.NRows, dense.NCols);
			}
			else if (value is ISparseMatrix<T> sparse)
			{
				using var dn = this.Storage.MakeReference(newLength: sparse.NRows * sparse.NCols).CreateAlike();
				sparse.ToDense(dn, sparse.NRows, sparse.NRows, sparse.NCols);
				MEM.MemoryCopy2D(dn, sparse.NRows, this.Storage.MakeReference(rowStart * this.LeadDim + columnStart), this.LeadDim, sparse.NRows, sparse.NCols);
			}
			else
				throw new NotSupportedException();
		}

		/// <summary>
		/// Get or set the element at the given position (<paramref name="x"/>, <paramref name="y"/>)
		/// </summary>
		/// <param name="x">The row position as a <see cref="long"/></param>
		/// <param name="y">The column position as a <see cref="long"/></param>
		/// <returns>The element at position (<paramref name="x"/>, <paramref name="y"/>)</returns>
		public override T this[long x, long y] {
			get {
				this.CheckIndex(x, y);
				return MEM.ToManaged(this.Storage.MakeReference(y * this.LeadDim + x));
			}
			set {
				this.CheckIndex(x, y);
				MEM.FromManaged(this.Storage.MakeReference(y * this.LeadDim + x), value);
			}
		}
		#endregion

		#region diagonal indexers
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private Storage<T> GetDiagStorage(long k) => this.Storage.MakeReference(k <= 0 ? k : k * this.LeadDim);

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private int GetDiagStride() => checked((int)(this.LeadDim + 1));

		/// <summary>
		/// Get the <paramref name="k"/>-th diagonal elements.
		/// </summary>
		/// <param name="k">The diagonal index: 0 for diagonal, 1 for super-diagonal at one above, -1 for sub-diagonal at one below, etc.</param>
		/// <returns>A new <see cref="DenseVector{T}"/> containing the <paramref name="k"/>-th diagonal elements.</returns>
		/// <exception cref="InvalidOperationException">If this matrix is not a square matrix</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="k"/> is out of range</exception>
		public override DenseVector<T> GetDiag(long k)
		{
			if (this.NRows != this.NCols)
				throw new InvalidOperationException();
			if (Math.Abs(k) >= this.NRows)
				throw new ArgumentOutOfRangeException(nameof(k), Resources.Parameter.InvalidValue);

			Storage<T>? storage = null;
			try
			{
				storage = this.Storage.MakeReference(newLength: this.NRows - k).CreateAlike();
				MEM.StridedCopy(this.GetDiagStorage(k), this.GetDiagStride(), storage, 1);
				return new DenseVector<T>(storage);
			}
			catch (Exception)
			{
				storage?.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Get the <paramref name="k"/>-th diagonal elements and write the result to <paramref name="overwrite"/>.
		/// </summary>
		/// <param name="k">The diagonal index: 0 for diagonal, 1 for super-diagonal at one above, -1 for sub-diagonal at one below, etc.</param>
		/// <param name="overwrite">The output <see cref="VectorBase{T}"/> which will contain the <paramref name="k"/>-th diagonal elements at exit</param>
		/// <exception cref="InvalidOperationException">If this matrix is not a square matrix</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="k"/> is out of range</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="overwrite"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="overwrite"/> cannot be overwritten</exception>
		public override void GetDiag(long k, VectorBase<T> overwrite)
		{
			if (this.NRows != this.NCols)
				throw new InvalidOperationException();
			if (Math.Abs(k) >= this.NRows)
				throw new ArgumentOutOfRangeException(nameof(k), Resources.Parameter.InvalidValue);
			if (overwrite is null || !overwrite.IsValid())
				throw new ArgumentNullException(nameof(overwrite));
			if (overwrite is not DenseVector<T> dense)
				throw new ArgumentException(Resources.Parameter.InvalidValue, nameof(overwrite));
			if (dense.Length < this.NRows - k)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(overwrite));

			MEM.StridedCopy(this.GetDiagStorage(k), this.GetDiagStride(), dense.Storage, 1);
		}

		/// <summary>
		/// Set the <paramref name="k"/>-th diagonal elements to <paramref name="value"/>.
		/// </summary>
		/// <param name="k">The diagonal index: 0 for diagonal, 1 for super-diagonal at one above, -1 for sub-diagonal at one below, etc.</param>
		/// <param name="value">The <paramref name="k"/>-th diagonal elements to set as a <see cref="VectorBase{T}"/></param>
		/// <exception cref="InvalidOperationException">If this matrix is not a square matrix</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="k"/> is out of range</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="value"/> is null or invalid</exception>
		/// <exception cref="NotSupportedException">If <paramref name="value"/> is neither a <see cref="DenseVector{T}"/> nor a <see cref="ISparseVector{T}"/></exception>
		public override void SetDiag(long k, VectorBase<T> value)
		{
			if (this.NRows != this.NCols)
				throw new InvalidOperationException();
			if (Math.Abs(k) >= this.NRows)
				throw new ArgumentOutOfRangeException(nameof(k), Resources.Parameter.InvalidValue);
			if (value is null || !value.IsValid())
				throw new ArgumentNullException(nameof(value));

			if (value is DenseVector<T> dense)
			{
				MEM.StridedCopy(dense.Storage, 1, this.GetDiagStorage(k), this.GetDiagStride());
			}
			else if (value is ISparseVector<T> sparse)
			{
				using var dn = this.Storage.MakeReference(newLength: this.NRows - k).CreateAlike();
				sparse.ToDense(dn);
				MEM.StridedCopy(dn, 1, this.GetDiagStorage(k), this.GetDiagStride());
			}
			else
				throw new NotSupportedException();
		}
		#endregion


	}
}
