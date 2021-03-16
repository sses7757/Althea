using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

using Althea.Arrays;
using Althea.Helpers;
using Althea.LinearAlgebra;
using Althea.LinearAlgebra.Sparse;
using Althea.Linq;
using Althea.NativeTypes;

using LAD = Althea.LinearAlgebra.Dense.AbstractApi;
using LAS = Althea.LinearAlgebra.Sparse.AbstractApi;
using MEM = Althea.Storage.AbstractApi;


namespace Althea.Backend.Arrays
{
	#region other info
	/// <summary>
	/// The <see cref="IOtherInfo"/> corresponding to <see cref="BlockedSparseMatrix{T, TInd}"/>
	/// </summary>
	public sealed record BlockedSparseMatrixOtherInfo(long BlockRows, long BlockCols) : IOtherInfo
	{
		object IReadOnlyList<object>.this[int index] => index == 0 ? this.BlockRows : index == 1 ? this.BlockCols : throw new ArgumentOutOfRangeException(nameof(index)); 

		int IReadOnlyCollection<object>.Count => 2;

		IEnumerator<object> IEnumerable<object>.GetEnumerator()
		{
			yield return this.BlockRows;
			yield return this.BlockCols;
		}

		IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<object>)this).GetEnumerator();
	}
	#endregion


	/// <summary>
	/// The concrete (blocked) sparse matrix class with the only mutable <see cref="ValueArray{T}.Storage"/> that refers to the value array storage and the <see cref="BlockedSparseMatrix{T, TInd}.RowIndexStorage"/> and <see cref="BlockedSparseMatrix{T, TInd}.ColIndexStorage"/> that refer to the <b>sorted</b> row and column index arrays' (of block sub-matrices) storages.
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
	/// <typeparam name="TInd">Any integer-typed unmanaged struct as the index type</typeparam>
	/// <remarks>The <see cref="BlockedSparseMatrix{T, TInd}.RowIndexStorage"/> and <see cref="BlockedSparseMatrix{T, TInd}.ColIndexStorage"/> are sorted according to <see cref="Althea.Arrays.SparseMatrix{T, TInd}.Format"/>. Any external operation that disturbs such order may result in unexpected consequences.</remarks>
	public class BlockedSparseMatrix<T, TInd> : Althea.Arrays.SparseMatrix<T, TInd>, IKrylovVector<BlockedSparseMatrix<T, TInd>, T>, IMatrix<T>
		where T : unmanaged
		where TInd : unmanaged
	{
		#region basic
		/// <summary>
		/// Get the storage of the row index (of block sub-matrices) array of this sparse matrix as a <see cref="Storage{T}"/> of <typeparamref name="TInd"/>
		/// </summary>
		public Storage<TInd> RowIndexStorage {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.m_indexArrays[0];
		}

		/// <summary>
		/// Get the storage of the column index (of block sub-matrices) array of this sparse matrix as a <see cref="Storage{T}"/> of <typeparamref name="TInd"/>
		/// </summary>
		public Storage<TInd> ColIndexStorage {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.m_indexArrays[1];
		}

		/// <summary>
		/// Get the number of rows of (any) block sub-matrix
		/// </summary>
		public long BlockNRows { get; }

		/// <summary>
		/// Get the number of column of (any) block sub-matrix
		/// </summary>
		public long BlockNCols { get; }

		private long Pack {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.BlockNRows * this.BlockNCols;
		}

		/// <summary>
		/// Create an empty <see cref="BlockedSparseMatrix{T, TInd}"/>
		/// </summary>
		public BlockedSparseMatrix() : base(0, 0, Storage<T>.Empty, Storage<TInd>.Empty, Storage<TInd>.Empty, SparseMatrixFormat.COOR) { }

		/// <summary>
		/// Create a <see cref="SparseMatrix{T, TInd}"/> (of <see cref="SparseMatrixFormat.COOR"/>, <see cref="SparseMatrixFormat.COOC"/>, <see cref="SparseMatrixFormat.CSR"/> or <see cref="SparseMatrixFormat.CSC"/> format) with given size, <paramref name="valueArray"/> and index arrays.
		/// </summary>
		/// <param name="blockRows">The number of rows of (any) block sub-matrix of this matrix</param>
		/// <param name="blockCols">The number of column of (any) block sub-matrix of this matrix</param>
		/// <param name="rows">The presenting number of rows of this matrix</param>
		/// <param name="cols">The presenting number of columns of this matrix</param>
		/// <param name="valueArray">The value array as a <see cref="Storage{T}"/> of <typeparamref name="T"/></param>
		/// <param name="rowIndexArray">The row index (of block matrices) array as a <see cref="Storage{T}"/> of <typeparamref name="TInd"/></param>
		/// <param name="colIndexArray">The column index (of block matrices) array as a <see cref="Storage{T}"/> of <typeparamref name="TInd"/></param>
		/// <param name="format">The atomic <see cref="SparseMatrixFormat"/> of a <see cref="FormatExtension.Blocked"/> value</param>
		/// <param name="defaultValue">The default value (the value not specified) of this sparse vector</param>
		/// <param name="stores">The number of stored values, default 0 means the length of <paramref name="valueArray"/></param>
		/// <exception cref="TypeMismatchException">If the <typeparamref name="TInd"/> is not an integral type</exception>
		/// <exception cref="ArgumentNullException">If the size is not 0 while any of the storages is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="format"/> is not atomic or not of allowed format; or <paramref name="blockRows"/> or <paramref name="blockCols"/> is non-positive</exception>
		/// <exception cref="ArgumentException">If the lengths of storages does not fit the underlying regulations indicated by <paramref name="format"/></exception>
		public BlockedSparseMatrix(long rows, long cols, long blockRows, long blockCols, Storage<T> valueArray, Storage<TInd> rowIndexArray, Storage<TInd> colIndexArray, SparseMatrixFormat format, T defaultValue = default, long stores = 0) :
			base(rows, cols, valueArray, rowIndexArray, colIndexArray, format, defaultValue, stores,
				rowLength: GetRowLength(rows, blockRows, blockCols, valueArray, stores, format),
				colLength: GetColLength(cols, blockRows, blockCols, valueArray, stores, format))
		{
			this.BlockNRows = blockRows; this.BlockNCols = blockCols;
		}

		private BlockedSparseMatrix(BlockedSparseMatrix<T, TInd> reference) : base(reference.NRows, reference.NCols, reference.Storage.MakeReference(), reference.RowIndexStorage.MakeReference(), reference.ColIndexStorage.MakeReference(), reference.Format, reference.DefaultValue) { }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static long GetRowLength(long rows, long blockRows, long blockCols, Storage<T> valueArray, long stores, SparseMatrixFormat format)
		{
			if (blockRows <= 0)
				throw new ArgumentOutOfRangeException(nameof(blockRows), blockRows, Resources.Parameter.MustPositive);
			if (blockCols <= 0)
				throw new ArgumentOutOfRangeException(nameof(blockCols), blockCols, Resources.Parameter.MustPositive);
			if (rows % blockRows != 0)
				throw new ArgumentException(Resources.Other.CannotDivide, nameof(rows));
			if (stores <= 0)
				stores = valueArray.Length;
			if (stores % (blockRows * blockCols) != 0)
				throw new ArgumentException(Resources.Other.CannotDivide, nameof(stores));
			if (stores <= (blockRows * blockCols))
				throw new ArgumentException(Resources.Parameter.InvalidValue, nameof(blockRows));
			return format switch
			{
				SparseMatrixFormat.BCOR or SparseMatrixFormat.BCOC or SparseMatrixFormat.BSC => stores / (blockRows * blockCols),
				SparseMatrixFormat.BSR => rows / blockRows + 1,
				_ => throw new ArgumentOutOfRangeException(nameof(format), format, Resources.Parameter.InvalidValue),
			};
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static long GetColLength(long cols, long blockRows, long blockCols, Storage<T> valueArray, long stores, SparseMatrixFormat format)
		{
			if (cols % blockCols != 0)
				throw new ArgumentException(Resources.Other.CannotDivide, nameof(cols));
			if (stores <= 0)
				stores = valueArray.Length;
			return format switch
			{
				SparseMatrixFormat.BCOR or SparseMatrixFormat.BCOC or SparseMatrixFormat.BSR => stores / (blockRows * blockCols),
				SparseMatrixFormat.BSC => cols / blockCols + 1,
				_ => throw new ArgumentOutOfRangeException(nameof(format), format, Resources.Parameter.InvalidValue),
			};
		}
		#endregion

		#region clone related
		/// <summary>
		/// Deep clone the sparse matrix, the mutable status will not be copied.
		/// </summary>
		/// <returns>The cloned sparse matrix</returns>
		public override BlockedSparseMatrix<T, TInd> Clone()
		{
			var indexArrays = ((ISparseArray<T, TInd>)this).NewArraysAlike<T, TInd>(out ActualStorage<T> value, copyValues: true);
			return new BlockedSparseMatrix<T, TInd>(this.NRows, this.NCols, this.BlockNRows, this.BlockNCols, value, indexArrays[0], indexArrays[1], this.Format, this.DefaultValue);
		}

		/// <summary>
		/// Create a new sparse matrix with same properties as this one while the underlying storages are not filled.
		/// </summary>
		/// <returns>The new sparse matrix alike this one</returns>
		public override BlockedSparseMatrix<T, TInd> NewArrayAlike()
		{
			var indexArrays = ((ISparseArray<T, TInd>)this).NewArraysAlike<T, TInd>(out ActualStorage<T> value, copyValues: false);
			return new BlockedSparseMatrix<T, TInd>(this.NRows, this.NCols, this.BlockNRows, this.BlockNCols, value, indexArrays[0], indexArrays[1], this.Format, this.DefaultValue);
		}

		/// <summary>
		/// Create a new sparse matrix with same properties as this one while the underlying storages are not filled and the data type is changed to <typeparamref name="TOut"/> while index type changed to <typeparamref name="TIndOut"/>.
		/// </summary>
		/// <typeparam name="TOut">Any unmanaged struct as the new data type</typeparam>
		/// <typeparam name="TIndOut">Any integral-typed unmanaged struct as the new index type</typeparam>
		/// <returns>The new sparse matrix alike this one</returns>
		/// <exception cref="TypeMismatchException">If the <typeparamref name="TIndOut"/> is not an integral type</exception>
		public override BlockedSparseMatrix<TOut, TIndOut> NewArrayAlike<TOut, TIndOut>()
		{
			var indexArrays = ((ISparseArray<T, TInd>)this).NewArraysAlike<TOut, TIndOut>(out ActualStorage<TOut> value, copyValues: false);
			return new BlockedSparseMatrix<TOut, TIndOut>(this.NRows, this.NCols, this.BlockNRows, this.BlockNCols, value, indexArrays[0], indexArrays[1], this.Format, this.DefaultValue.GenericConvert<T, TOut>());
		}
		#endregion

		#region indexer helpers
		/// <summary>
		/// Check the row and column ranges
		/// </summary>
		/// <returns>The row and column offsets and lengths, divided by <see cref="BlockNRows"/> and <see cref="BlockNCols"/> respectively</returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offsetRow"/> or <paramref name="countRow"/> or <paramref name="offsetCol"/> or <paramref name="countCol"/> is out of range</exception>
		/// <exception cref="InvalidOperationException">If <paramref name="offsetRow"/> or <paramref name="countRow"/> cannot divide <see cref="BlockNRows"/>; or <paramref name="offsetCol"/> or <paramref name="countCol"/> cannot divide <see cref="BlockNCols"/></exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected (long offsetRow, long countRow, long offsetCol, long countCol) CheckRangeAndBlock(long offsetRow, long countRow, long offsetCol, long countCol)
		{
			this.CheckRange(offsetRow, countRow, offsetCol, countCol);
			if (offsetRow % this.BlockNRows != 0)
				throw new InvalidOperationException(Resources.Other.CannotDivide);
			if (countRow % this.BlockNRows != 0)
				throw new InvalidOperationException(Resources.Other.CannotDivide);
			if (offsetCol % this.BlockNCols != 0)
				throw new InvalidOperationException(Resources.Other.CannotDivide);
			if (countCol % this.BlockNCols != 0)
				throw new InvalidOperationException(Resources.Other.CannotDivide);
			return (offsetRow / this.BlockNRows, countRow / this.BlockNRows, offsetCol / this.BlockNCols, countCol / this.BlockNCols);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static long ToLong(TInd i) => i.GenericConvert<TInd, long>();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static TInd ToInd(long i) => i.GenericConvert<long, TInd>();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private T ElementIndex(long x, long y, Storage<TInd> sorted, Storage<TInd> other, T value, bool compressed, bool get)
		{
			long xx = Math.DivRem(x, this.BlockNRows, out long remX);
			long yy = Math.DivRem(y, this.BlockNCols, out long remY);
			long find = compressed ? SparseMatrix<T, TInd>.IndexCompressed(xx, yy, sorted, other) :
									SparseMatrix<T, TInd>.IndexCoordinated(xx, yy, sorted, other, this.NStored / this.Pack);
			if (find >= 0)
			{
				find = find * this.Pack + remY * this.BlockNRows + remX;
				if (get)
					return MEM.ToManaged(this.Storage + find);
				// else
				MEM.FromManaged(this.Storage + find, value);
				return default;
			}
			// else not stored
			if (get)
				return value;
			else if (value.IsEqual(this.DefaultValue))
				return default;
			else // cannot set
				throw new InvalidOperationException();
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
		/// <exception cref="InvalidOperationException">If the ranges cuts through the block sub-matrices of this matrix</exception>
		public override BlockedSparseMatrix<T, TInd> GetSubmatrix(long offsetRow, long countRow, long offsetCol, long countCol)
		{
			(offsetRow, countRow, offsetCol, countCol) = this.CheckRangeAndBlock(offsetRow, countRow, offsetCol, countCol);
			TInd x1 = ToInd(offsetRow), x2 = ToInd(offsetRow + countRow);
			TInd y1 = ToInd(offsetCol), y2 = ToInd(offsetCol + countCol);
			bool allRows = countRow == this.NRows, allCols = countCol == this.NCols;
			// shortcut
			if (allRows && allCols)
				return new BlockedSparseMatrix<T, TInd>(this);
			// else
			SparseMatrixFormat format = this.Format;
			var values = this.Storage; var rowInd = this.RowIndexStorage; var colInd = this.ColIndexStorage;
			switch (this.Format)
			{
				case SparseMatrixFormat.BCOR:
					SparseMatrix<T, TInd>.GetRangeCoordinated(x1, x2, y1, y2, allCols, ref rowInd, ref colInd, ref values, this.Pack);
					format = SparseMatrixFormat.BSR;
					break;
				case SparseMatrixFormat.BCOC:
					SparseMatrix<T, TInd>.GetRangeCoordinated(y1, y2, x1, x2, allRows, ref colInd, ref rowInd, ref values, this.Pack);
					format = SparseMatrixFormat.BSC;
					break;
				case SparseMatrixFormat.BSR:
					SparseMatrix<T, TInd>.GetRangeCompressed(offsetRow, countRow, y1, y2, allCols, ref rowInd, ref colInd, ref values, this.Pack);
					break;
				case SparseMatrixFormat.BSC:
					SparseMatrix<T, TInd>.GetRangeCompressed(offsetCol, countCol, x1, x2, allRows, ref colInd, ref rowInd, ref values, this.Pack);
					break;
				default: // never here
					throw new NotSupportedException();
			}
			return new BlockedSparseMatrix<T, TInd>(countRow, countCol, this.BlockNRows, this.BlockNCols, values, rowInd, colInd, format, this.DefaultValue);
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
		/// <exception cref="InvalidOperationException">If the ranges cuts through the block sub-matrices of this matrix</exception>
		public override void GetSubmatrix(long offsetRow, long countRow, long offsetCol, long countCol, MatrixBase<T> overwrite)
		{
			this.CheckRange(offsetRow, countRow, offsetCol, countCol);
			if (overwrite is null || !overwrite.IsValid())
				throw new ArgumentNullException(nameof(overwrite));

			if (overwrite is DenseMatrix<T> dense)
			{
				if (dense.NRows < countRow || dense.NCols < countCol)
					throw new ArgumentException(Resources.Parameter.WrongSize, nameof(overwrite));
				using var sub = this.GetSubmatrix(offsetRow, countRow, offsetCol, countCol);
				sub.ToDense(dense.Storage, dense.LeadDim);
			}
			else if (overwrite is BlockedSparseMatrix<T, TInd> sparse)
{
				if (sparse.NRows != countRow || sparse.NCols != countCol || sparse.BlockNRows != this.BlockNRows || sparse.BlockNCols != this.BlockNCols)
					throw new ArgumentException(Resources.Parameter.WrongSize, nameof(overwrite));
				if (this.Format != sparse.Format)
					throw new ArgumentException(Resources.Parameter.InvalidValue, nameof(overwrite));
				using var sub = this.GetSubmatrix(offsetRow, countRow, offsetCol, countCol);
				if (sub.NStored != sparse.NStored)
					throw new ArgumentException(Resources.Parameter.InvalidValue, nameof(overwrite));
				// copy
				MEM.MemoryCopy(sub.Storage, sparse.Storage);
				MEM.MemoryCopy(sub.RowIndexStorage, sparse.RowIndexStorage);
				MEM.MemoryCopy(sub.ColIndexStorage, sparse.ColIndexStorage);
			}
			else
				throw new ArgumentException(Resources.Parameter.UnexpectedType, nameof(overwrite));
		}

		/// <summary>
		/// Set a sub-matrix by the row and column starting index (inclusive).
		/// </summary>
		/// <param name="rowStart">The <see cref="long"/> to indicate the starting row index to set</param>
		/// <param name="colStart">The <see cref="long"/> to indicate the starting column index to set</param>
		/// <param name="value">The <see cref="MatrixBase{T}"/> whose value will overwrite this matrix from (<paramref name="rowStart"/>, <paramref name="colStart"/>)</param>
		/// <exception cref="ArgumentNullException">If <paramref name="value"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="rowStart"/> or <paramref name="colStart"/> and <paramref name="value"/>'s <see cref="MatrixBase{T}.NRows"/> or <see cref="MatrixBase{T}.NCols"/> are out of range</exception>
		/// <exception cref="ArgumentException">If <paramref name="value"/> is not a <see cref="SparseMatrix{T, TInd}"/> or of incompatible format</exception>
		public override void SetSubmatrix(long rowStart, long colStart, MatrixBase<T> value)
		{
			if (value is null || !value.IsValid())
				throw new ArgumentNullException(nameof(value));
			if (value is not BlockedSparseMatrix<T, TInd> sp || sp.Format.IsRowMajor() != this.Format.IsRowMajor())
				throw new ArgumentException(Resources.Parameter.InvalidValue, nameof(value));
			var (offsetRow, countRow, offsetCol, countCol) = this.CheckRangeAndBlock(rowStart, sp.NRows, colStart, sp.NCols);

			TInd x1 = ToInd(offsetRow), x2 = ToInd(offsetRow + countRow);
			TInd y1 = ToInd(offsetCol), y2 = ToInd(offsetCol + countCol);
			bool allRows = value.NRows == this.NRows, allCols = value.NCols == this.NCols;
			// shortcut
			if (allRows && allCols)
			{
				if (sp.NStored != this.NStored)
					throw new ArgumentException(Resources.Parameter.InvalidValue, nameof(value));
				MEM.MemoryCopy(sp.Storage, this.Storage);
				MEM.MemoryCopy(sp.RowIndexStorage, this.RowIndexStorage);
				MEM.MemoryCopy(sp.ColIndexStorage, this.ColIndexStorage);
				return;
			}
			// else
			SparseMatrix<T, TInd>.SetRange(x1, x2, y1, y2, this, sp, this.Pack);
		}

		/// <summary>
		/// Get or set the element at the given position (<paramref name="x"/>, <paramref name="y"/>)
		/// </summary>
		/// <param name="x">The row position as a <see cref="long"/></param>
		/// <param name="y">The column position as a <see cref="long"/></param>
		/// <returns>The element at position (<paramref name="x"/>, <paramref name="y"/>)</returns>
		/// <exception cref="InvalidOperationException">If the element at the given position is not stored while the set value is not <see cref="Althea.Arrays.SparseMatrix{T, TInd}.DefaultValue"/></exception>
		public override T this[long x, long y] {
			get {
				this.CheckIndex(x, y);
				var (sorted, other) = this.Format.IsRowMajor() ? (this.RowIndexStorage, this.ColIndexStorage) : (this.ColIndexStorage, this.RowIndexStorage);
				return this.ElementIndex(x, y, sorted, other, this.DefaultValue, this.Format.IsCompressed(), get: true);
			}
			set {
				this.CheckIndex(x, y);
				var (sorted, other) = this.Format.IsRowMajor() ? (this.RowIndexStorage, this.ColIndexStorage) : (this.ColIndexStorage, this.RowIndexStorage);
				this.ElementIndex(x, y, sorted, other, value, this.Format.IsCompressed(), get: false);
			}
		}
		#endregion

		#region diagonal indexers
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
				throw new ArgumentOutOfRangeException(nameof(k), k, Resources.Parameter.InvalidValue);

			using var dense = Storage<T>.Create(this.Storage[0].Location, this.NRows * this.NCols);
			this.ToDense(dense, this.NRows);
			return new DenseMatrix<T>(dense, this.NRows, this.NCols).GetDiag(k);
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
			if (overwrite is null || !overwrite.IsValid())
				throw new ArgumentNullException(nameof(overwrite));
			if (overwrite is DenseVector<T> dn && dn.Length < this.NRows - k)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(overwrite));

			using var vec = this.GetDiag(k);
			if (overwrite is DenseVector<T> dense)
			{
				dense.ReplaceBy(vec);
			}
			else
				throw new ArgumentException(Resources.Parameter.UnexpectedType, nameof(overwrite));
		}

		/// <summary>
		/// Set the <paramref name="k"/>-th diagonal elements to <paramref name="value"/>.
		/// </summary>
		/// <param name="k">The diagonal index: 0 for diagonal, 1 for super-diagonal at one above, -1 for sub-diagonal at one below, etc.</param>
		/// <param name="value">The <paramref name="k"/>-th diagonal elements to set as a <see cref="VectorBase{T}"/></param>
		/// <exception cref="InvalidOperationException">If this matrix is not a square matrix</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="k"/> is out of range</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="value"/> is null or invalid</exception>
		/// <exception cref="NotSupportedException">If <paramref name="value"/> is not a <see cref="SparseVector{T, TInd}"/> or has different defaults</exception>
		public override void SetDiag(long k, VectorBase<T> value)
		{
			if (this.NRows != this.NCols)
				throw new InvalidOperationException();
			if (Math.Abs(k) >= this.NRows)
				throw new ArgumentOutOfRangeException(nameof(k), k, Resources.Parameter.InvalidValue);
			if (value is null || !value.IsValid())
				throw new ArgumentNullException(nameof(value));
			if (value is not SparseVector<T, TInd> sparse || !sparse.DefaultValue.IsEqual(this.DefaultValue))
				throw new NotSupportedException();

			throw new NotImplementedException();
		}
		#endregion

		#region conversion
		/// <summary>
		/// When implemented by a derived class, convert this sparse matrix to a dense matrix whose <see cref="Storage{T}"/> is <paramref name="denseStorage"/>
		/// </summary>
		/// <param name="denseStorage">The <see cref="Storage{T}"/> of the dense matrix to overwrite</param>
		/// <param name="leadDim">The leading dimension of the target dense matrix, default 0 means <see cref="MatrixBase{T}.NRows"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="denseStorage"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="leadDim"/> is less than <see cref="MatrixBase{T}.NRows"/></exception>
		/// <exception cref="ArgumentException">If <paramref name="leadDim"/> * <see cref="MatrixBase{T}.NCols"/> &gt; <paramref name="denseStorage"/>.<see cref="Storage{T}.Length">Length</see></exception>
		public override void ToDense(Storage<T> denseStorage, long leadDim)
		{
			if (denseStorage is null || !denseStorage.IsValid())
				throw new ArgumentNullException(nameof(denseStorage));
			if (leadDim == 0)
				leadDim = this.NRows;
			if (leadDim < this.NRows)
				throw new ArgumentOutOfRangeException(nameof(leadDim), leadDim, Resources.Parameter.InvalidValue);
			if (leadDim * this.NCols > denseStorage.Length)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(denseStorage));

			LAS.MatrixSparseToDense(this, denseStorage, leadDim);
		}

		/// <summary>
		/// When implemented by a derived class, convert this sparse matrix to another sparse matrix with <see cref="Althea.Arrays.SparseMatrix{T, TInd}.Format"/> fitting <paramref name="format"/>
		/// </summary>
		/// <param name="format">The target format, can be anatomic</param>
		/// <param name="otherInfo">The target sparse matrix's <see cref="IOtherInfo"/>, default null means letting the internal implementation determine</param>
		/// <returns>The converted <see cref="Althea.Arrays.SparseMatrix{T, TInd}"/> whose <see cref="Althea.Arrays.SparseMatrix{T, TInd}.Format"/> fits the given <paramref name="format"/>, or this one if no conversion is necessary</returns>
		/// <exception cref="NotSupportedException">If <paramref name="format"/> is not composed of internally defined values</exception>
		public override Althea.Arrays.SparseMatrix<T, TInd> ToFormat(SparseMatrixFormat format, IOtherInfo? otherInfo = null)
		{
			if ((format & this.Format) != 0)
				return this;
			if ((format & FormatExtension.PreDefined) == 0)
				throw new NotSupportedException(Resources.Support.Format);
			if (otherInfo is not null && otherInfo is not BlockedSparseMatrixOtherInfo)
				throw new NotSupportedException(Resources.Support.Format);

			var wrapper = LAS.MatrixSparseFormatConvert(this, format, otherInfo);
			try
			{
				return SparseVector<T, TInd>.CheckWrapper(this.NRows, this.NCols, this.DefaultValue, wrapper);
			}
			catch (Exception)
			{
				wrapper.Dispose();
				throw;
			}
		}
		#endregion

		#region reshape
		/// <summary>
		/// When implemented by a derived class, convert this matrix to a vector
		/// </summary>
		/// <returns>The new vector reshaped from this matrix</returns>
		public override SparseVector<T, TInd> ToVector()
		{
			var wrapper = LAS.SparseMatrixToVector(this, SparseVectorFormat.Coordinated);
			try
			{
				return SparseVector<T, TInd>.CheckWrapper(this.NRows * this.NCols, this.DefaultValue, wrapper);
			}
			catch (Exception)
			{
				wrapper.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Reshape this matrix to a matrix with leading dimension = <paramref name="rows"/>
		/// </summary>
		/// <param name="rows">The number of rows of the target matrix; if <paramref name="rows"/> ≤ 0, it is assumed that leadDim = <c>sqrt(<see cref="AbstractArray{T}.Length"/>)</c>.</param>
		/// <returns>The reshaped matrix, may be this matrix itself</returns>
		public override Althea.Arrays.SparseMatrix<T, TInd> ToMatrix(long rows = 0)
		{
			Span<long> size = stackalloc long[2].SetValue(rows);
			CheckSize(this, size);
			if (size[0] == this.NRows)
				return this;
			using var vector = this.ToVector();
			return vector.ToMatrix(rows);
		}

		/// <summary>
		/// Reshape the array to a tensor with dimensionality = <paramref name="size"/>.
		/// </summary>
		/// <param name="size">The new size/dimensionality with at most one or zero uncertain dimension indicated by a non-positive number.</param>
		/// <returns>The reshaped tensor</returns>
		public override ValueArray<T> ToTensor(ReadOnlySpan<long> size) { }
		#endregion

		#region linear algebra
		/// <summary>
		/// Create a new <see cref="BlockedSparseMatrix{T, TInd}"/> which is the simple operation result of this matrix under <paramref name="operation"/>.
		/// </summary>
		/// <param name="operation">The input <see cref="MatrixOperation"/> used as the simple operation to be applied</param>
		/// <returns>A new <see cref="BlockedSparseMatrix{T, TInd}"/> as the result of <paramref name="operation"/>(this)</returns>
		/// <exception cref="NotSupportedException">If the given <paramref name="operation"/> is not supported</exception>
		public override BlockedSparseMatrix<T, TInd> ApplyOperation(MatrixOperation operation)
		{
			// shortcut
			if (operation == MatrixOperation.None)
				return this.Clone();
			if (operation == MatrixOperation.Conjugate)
				return this.ApplyToClone(static c => LAD.PointWiseConjugate(c.Storage, 1));
			// otherwise
			var wrapper = LAS.MatrixSparseAddSparse(operation, MatrixOperation.None, Const<T>.One, this, default, null);
			try
			{
				var res = SparseVector<T, TInd>.CheckWrapper(this.NCols, this.NRows, this.DefaultValue, wrapper);
				if (res is not BlockedSparseMatrix<T, TInd> ss)
					throw new InvalidOperationException(Resources.Support.Format);
				return ss;
			}
			catch (Exception)
			{
				wrapper.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Create a new <see cref="MatrixBase{T}"/> which is the point-wise addition result of this matrix the <paramref name="other"/> matrix.
		/// </summary>
		/// <param name="scalarThis">The scalar to multiply to this matrix before addition</param>
		/// <param name="scalarOther">The scalar to multiply to the <paramref name="other"/> matrix before addition</param>
		/// <param name="other">The input right <see cref="MatrixBase{T}"/> to be added</param>
		/// <param name="opThis">The <see cref="MatrixOperation"/> to apply to this matrix before addition</param>
		/// <param name="opOther">The <see cref="MatrixOperation"/> to apply to the <paramref name="other"/> matrix before addition</param>
		/// <returns>A new <see cref="DenseMatrix{T}"/> as the result of <c><paramref name="scalarThis"/> * <paramref name="opThis"/>(this) + <paramref name="scalarOther"/> * <paramref name="opOther"/>(<paramref name="other"/>)</c></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="other"/> is null or empty</exception>
		/// <exception cref="NotSupportedException">If the given <paramref name="opThis"/> or <paramref name="opOther"/> is not supported; or <paramref name="other"/> is neither a <see cref="DenseMatrix{T}"/> nor a <see cref="ISparseMatrix{T}"/></exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="scalarThis"/> or <paramref name="scalarOther"/> is 0</exception>
		/// <exception cref="ArgumentException">If the addition cannot be performed due to incompatible sizes</exception>
		public override MatrixBase<T> AddMatrix(T scalarThis, T scalarOther, MatrixBase<T> other, MatrixOperation opThis = MatrixOperation.None, MatrixOperation opOther = MatrixOperation.None)
		{
			var (m, n) = ((IMatrix<T>)this).CheckAdd(scalarThis, scalarOther, other, ref opThis, ref opOther);
			if (other is DenseMatrix<T> dense)
			{
				var clone = dense.NewArrayAlike();
				try
				{
					LAS.MatrixDenseAddSparse(opOther, opThis, scalarOther, dense.Storage, dense.LeadDim, scalarThis, this, clone.Storage, clone.LeadDim);
					return clone;
				}
				catch (Exception)
				{
					clone?.Dispose();
					throw;
				}
			}
			else if (other is ISparseMatrix<T> sparse)
			{
				var wrapper = LAS.MatrixSparseAddSparse(opThis, opOther, scalarThis, this, scalarOther, sparse);
				try
				{
					return SparseVector<T, TInd>.CheckWrapper(m, n, this.DefaultValue.GenericAdd(sparse.DefaultValue), wrapper);
				}
				catch (Exception)
				{
					wrapper.Dispose();
					throw;
				}
			}
			else
				throw new NotSupportedException();
		}

		/// <summary>
		/// Create a new <see cref="MatrixBase{T}"/> which is the multiplication result of this matrix and the <paramref name="other"/> matrix.
		/// </summary>
		/// <param name="scalar">The scalar to multiply to the result</param>
		/// <param name="other">The input right <see cref="MatrixBase{T}"/> to be multiplied</param>
		/// <param name="opThis">The <see cref="MatrixOperation"/> to apply to this matrix before addition</param>
		/// <param name="opOther">The <see cref="MatrixOperation"/> to apply to the <paramref name="other"/> matrix before addition</param>
		/// <returns>A new <see cref="MatrixBase{T}"/> as the result of <c><paramref name="scalar"/> * <paramref name="opThis"/>(this) * <paramref name="opOther"/>(<paramref name="other"/>)</c></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="other"/> is null or empty</exception>
		/// <exception cref="NotSupportedException">If the given <paramref name="opThis"/> or <paramref name="opOther"/> is not supported; or <paramref name="other"/> is neither a <see cref="DenseMatrix{T}"/> nor a <see cref="ISparseMatrix{T}"/></exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="scalar"/> is 0</exception>
		/// <exception cref="ArgumentException">If the multiplication cannot be performed due to incompatible sizes</exception>
		public override MatrixBase<T> MultiplyMatrix(T scalar, MatrixBase<T> other, MatrixOperation opThis = MatrixOperation.None, MatrixOperation opOther = MatrixOperation.None)
		{
			var (m, n, k) = ((IMatrix<T>)this).CheckMultiply(scalar, other, ref opThis, ref opOther);
			if (other is DenseMatrix<T> dense)
			{
				var output = Storage<T>.Create(dense.Storage[0].Location, m * n);
				try
				{
					LAS.MatrixSparseMultiplyDense(opThis, opOther, n, scalar, this, dense.Storage, dense.LeadDim, default, output, m);
					return new DenseMatrix<T>(output, m, n);
				}
				catch (Exception)
				{
					output?.Dispose();
					throw;
				}
			}
			else if (other is ISparseMatrix<T> sparse)
			{
				var wrapper = LAS.MatrixSparseMultiplySparse(opThis, opOther, scalar, this, sparse, default, null);
				try
				{
					T defThis = this.DefaultValue, defOther = sparse.DefaultValue;
					T defNew = (dynamic)defThis * defOther * k;
					return SparseVector<T, TInd>.CheckWrapper(m, n, defNew, wrapper);
				}
				catch (Exception)
				{
					wrapper.Dispose();
					throw;
				}
			}
			else
				throw new NotSupportedException();
		}
		#endregion

		#region IKrylovVector
		/// <summary>
		/// Check the sparsity of this sparse matrix and the <paramref name="other"/> one
		/// </summary>
		/// <param name="other">The other sparse matrix to check sparsity</param>
		/// <exception cref="ArgumentNullException">If <paramref name="other"/> is null or invalid</exception>
		/// <exception cref="InvalidOperationException">If the <paramref name="other"/> matrix has different sparsity from this one</exception>
		protected void CheckSparsity(BlockedSparseMatrix<T, TInd> other)
		{
			if (other is null || !other.IsValid())
				throw new ArgumentNullException(nameof(other));
			if (this.NRows != other.NRows || this.NCols != other.NCols ||
				this.BlockNRows != other.BlockNRows || this.BlockNCols != other.BlockNCols ||
				this.Format != other.Format || this.NStored != other.NStored)
				throw new InvalidOperationException(Resources.Parameter.NotSameSize);
			// check same indices
			if (this.RowIndexStorage == other.RowIndexStorage && this.ColIndexStorage == other.ColIndexStorage)
				return;
			if (!LAD.PointWiseEquals(this.RowIndexStorage, 1, other.RowIndexStorage, 1) ||
				!LAD.PointWiseEquals(this.ColIndexStorage, 1, other.ColIndexStorage, 1))
				throw new InvalidOperationException(Resources.Other.DifferentSparsity);
		}

		void IKrylovVector<BlockedSparseMatrix<T, TInd>, T>.Scale(T value) => this.Scale(value);

		double IKrylovVector<BlockedSparseMatrix<T, TInd>, T>.Norm() => this.Norm();

		void IKrylovVector<BlockedSparseMatrix<T, TInd>, T>.Normalize() => this.Normalize();

		T IKrylovVector<BlockedSparseMatrix<T, TInd>, T>.Dot(BlockedSparseMatrix<T, TInd> other)
		{
			this.CheckSparsity(other);
			return LAD.Dot(true, this.Storage, 1, other.Storage, 1);
		}

		/// <summary>
		/// Add the <paramref name="other"/> (scaling by <paramref name="scalar"/>) matrix to this matrix in-place.
		/// </summary>
		/// <param name="other">The other <see cref="BlockedSparseMatrix{T, TInd}"/> to add</param>
		/// <param name="scalar">The scalar to be multiplied to <paramref name="other"/> of type <typeparamref name="T"/></param>
		public virtual void AddBy(BlockedSparseMatrix<T, TInd> other, T scalar)
		{
			this.CheckSparsity(other);
			LAD.VectorGeneralAdd(scalar, other.Storage, 1, this.Storage, 1);
		}

		/// <summary>
		/// Replace this matrix's content with the <paramref name="other"/> vector in-place.
		/// </summary>
		/// <param name="other">The other <see cref="SparseMatrix{T, TInd}"/> to replace from</param>
		/// <exception cref="ArgumentNullException">If <paramref name="other"/> is null or invalid</exception>
		/// <exception cref="InvalidOperationException">If the replacement cannot be done in-place due to reason(s) such as different sparsities between this and <paramref name="other"/></exception>
		public void ReplaceBy(BlockedSparseMatrix<T, TInd> other)
		{
			if (other is null || !other.IsValid())
				throw new ArgumentNullException(nameof(other));
			if (this.NRows != other.NRows || this.NCols != other.NCols || this.Format != other.Format || this.NStored != other.NStored)
				throw new InvalidOperationException(Resources.Parameter.NotSameSize);

			MEM.MemoryCopy(other.Storage, this.Storage);
			MEM.MemoryCopy(other.RowIndexStorage, this.RowIndexStorage);
			MEM.MemoryCopy(other.ColIndexStorage, this.ColIndexStorage);
		}
		#endregion

		#region helper methods
		/// <summary>
		/// The helper method used in <see cref="Althea.Arrays.SparseMatrix{T, TInd}.Print(PrintSettings?)"/> to get the first several row and column indices of this sparse matrix's block sub-matrices
		/// </summary>
		/// <param name="rowIndices">The output <see cref="Span{T}"/> of <see cref="long"/> used to store the row indices</param>
		/// <param name="colIndices">The output <see cref="Span{T}"/> of <see cref="long"/> used to store the column indices</param>
		protected override void GetIndices(Span<long> rowIndices, Span<long> colIndices)
		{
			int rows = rowIndices.Length, cols = colIndices.Length;
			long find;
			switch (this.Format)
			{
				case SparseMatrixFormat.BCOR:
				case SparseMatrixFormat.BCOC:
					SparseMatrix<T, TInd>.ToManaged(this.RowIndexStorage.MakeReference(newLength: rows), rowIndices);
					SparseMatrix<T, TInd>.ToManaged(this.ColIndexStorage.MakeReference(newLength: cols), colIndices);
					break;
				case SparseMatrixFormat.BSR:
					find = LAS.IndexBound(this.RowIndexStorage, ToInd(rows - 1), lowerBound: false);
					{
						using var temp = this.ColIndexStorage.MakeReference(newLength: rows).CreateAlike<long>();
						LAS.IndexGenerateFromBounds(this.RowIndexStorage.MakeReference(newLength: find), temp, lowerBound: true, start: default);
						MEM.ToManaged(temp, rowIndices);
					}
					SparseMatrix<T, TInd>.ToManaged(this.ColIndexStorage.MakeReference(newLength: cols), colIndices);
					break;
				case SparseMatrixFormat.BSC:
					find = LAS.IndexBound(this.ColIndexStorage, ToInd(cols - 1), lowerBound: false);
					{
						using var temp = this.RowIndexStorage.MakeReference(newLength: cols).CreateAlike<long>();
						LAS.IndexGenerateFromBounds(this.ColIndexStorage.MakeReference(newLength: find), temp, lowerBound: true, start: default);
						MEM.ToManaged(temp, colIndices);
					}
					SparseMatrix<T, TInd>.ToManaged(this.RowIndexStorage.MakeReference(newLength: rows), rowIndices);
					break;
				default:
					break;
			}
		}

		/// <summary>
		/// Print out this sparse matrix.
		/// </summary>
		/// <param name="overrideSetting">Override global settings in <see cref="Settings"/></param>
		/// <returns>The detailed string representation of this sparse matrix</returns>
		public override string Print(PrintSettings? overrideSetting = null)
		{
			string description = this.ToString();
			if (this.Disposed)
				return description;

			var settings = overrideSetting ?? Settings.PrintSetting;

			StringBuilder detail = new(description);
			detail.AppendLine(":");
			// get managed arrays
			int length = (int)Math.Min(settings.ArrayLength, this.NStored / this.Pack);
			Span<long> row = length.CheckStackLimit<long>() ?? stackalloc long[length];
			Span<long> col = length.CheckStackLimit<long>() ?? stackalloc long[length];
			this.GetIndices(row, col);
			// to matrix string
			for (int i = 0; i < length; i++)
			{
				string indexPair = $"({row[i]}, {col[i]}) -> ";
				detail.Append(indexPair);
				string pad = new(' ', indexPair.Length);
				string matrixRepr = new DenseMatrix<T>(this.Storage + this.Pack * i, this.BlockNRows, this.BlockNCols).Print(overrideSetting);
				string[] reprs = matrixRepr.Split(Environment.NewLine);
				for (int j = 0; j < reprs.Length - 1; j++)
				{
					detail.AppendLine(reprs[j]).Append(pad);
				}
				detail.Append(reprs[^1]);
			}
			if (this.NStored / this.Pack > length)
				detail.AppendLine().Append(string.Format(Resources.Print.MoreStored, this.NStored / this.Pack - length));
			return detail.ToString();
		}

		/// <summary>
		/// The helper method used by <see cref="Althea.Arrays.SparseMatrix{T, TInd}.GetStorages"/> to get the index storages' names. Only used when the sparse array contains more than one index storages.
		/// </summary>
		/// <param name="orderOfIndexStorage">The index of all index storages of this sparse matrix</param>
		/// <returns>The name the index storage indicated by the given <paramref name="orderOfIndexStorage"/></returns>
		protected override string IndexStorageNameOf(int orderOfIndexStorage) => string.Empty;

		/// <summary>
		/// The presenting name of <see cref="BlockNRows"/>
		/// </summary>
		protected internal const string BlockNRowsName = nameof(BlockNRows);

		/// <summary>
		/// The presenting name of <see cref="BlockNCols"/>
		/// </summary>
		protected internal const string BlockNColsName = nameof(BlockNCols);

		/// <summary>
		/// Get other requisite informations for re-constructing the sparse matrix of that derived class type. The default implementation returns the <see cref="Althea.Arrays.SparseMatrix{T, TInd}.DefaultValue"/>, <see cref="Althea.Arrays.SparseMatrix{T, TInd}.Format"/>, <see cref="BlockNRows"/> and <see cref="BlockNCols"/>.
		/// </summary>
		/// <returns>Other requisite informations used to re-construct this sparse matrix</returns>
		public override IReadOnlyDictionary<string, object> GetMetaData() => new Dictionary<string, object>(4)
		{
			[DefaultValueName] = this.DefaultValue,
			[FormatName] = this.Format,
			[BlockNRowsName] = this.BlockNRows,
			[BlockNColsName] = this.BlockNCols
		};
		#endregion
	}
}
