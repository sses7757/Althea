using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Althea.Linq;
using Althea.Arrays;
using Althea.Helpers;
using Althea.NativeTypes;
using Althea.LinearAlgebra;
using Althea.LinearAlgebra.Sparse;

using MEM = Althea.Storage.AbstractApi;
using LAD = Althea.LinearAlgebra.Dense.AbstractApi;
using LAS = Althea.LinearAlgebra.Sparse.AbstractApi;


namespace Althea.Backend.Arrays
{
	/// <summary>
	/// The concrete (non-blocked) sparse matrix class with the only mutable <see cref="ValueArray{T}.Storage"/> that refers to the value array storage and the <see cref="SparseMatrix{T, TInd}.RowIndexStorage"/> and <see cref="SparseMatrix{T, TInd}.ColIndexStorage"/> that refer to the <b>sorted</b> row and column index arrays' storages.
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct that implements <see cref="IFormattable"/> and <see cref="IEquatable{T}"/> as the data type</typeparam>
	/// <typeparam name="TInd">Any integer-typed unmanaged struct as the index type</typeparam>
	/// <remarks>The <see cref="SparseMatrix{T, TInd}.RowIndexStorage"/> and <see cref="SparseMatrix{T, TInd}.ColIndexStorage"/> are sorted according to <see cref="Althea.Arrays.SparseMatrix{T, TInd}.Format"/>. Any external operation that disturbs such order may result in unexpected consequences.</remarks>
	public class SparseMatrix<T, TInd> : Althea.Arrays.SparseMatrix<T, TInd>, IKrylovVector<SparseMatrix<T, TInd>, T>
		where T : unmanaged, IFormattable, IEquatable<T>
		where TInd : unmanaged, IEquatable<TInd>
	{
		#region basic
		/// <summary>
		/// Get the storage of the row index array of this sparse matrix as a <see cref="Storage{T}"/> of <typeparamref name="TInd"/>
		/// </summary>
		public Storage<TInd> RowIndexStorage => this.m_indexArrays[0];

		/// <summary>
		/// Get the storage of the column index array of this sparse matrix as a <see cref="Storage{T}"/> of <typeparamref name="TInd"/>
		/// </summary>
		public Storage<TInd> ColIndexStorage => this.m_indexArrays[1];

		/// <summary>
		/// Create an empty <see cref="SparseMatrix{T, TInd}"/>
		/// </summary>
		public SparseMatrix() : base(0, 0, Storage<T>.Empty, Storage<TInd>.Empty, Storage<TInd>.Empty, SparseMatrixFormat.COOR) { }

		/// <summary>
		/// Create a <see cref="SparseMatrix{T, TInd}"/> (of <see cref="SparseMatrixFormat.COOR"/>, <see cref="SparseMatrixFormat.COOC"/>, <see cref="SparseMatrixFormat.CSR"/> or <see cref="SparseMatrixFormat.CSC"/> format) with given size, <paramref name="valueArray"/> and index arrays.
		/// </summary>
		/// <param name="rows">The presenting number of rows of this matrix</param>
		/// <param name="cols">The presenting number of columns of this matrix</param>
		/// <param name="valueArray">The value array as a <see cref="Storage{T}"/> of <typeparamref name="T"/></param>
		/// <param name="rowIndexArray">The row index array as a <see cref="Storage{T}"/> of <typeparamref name="TInd"/></param>
		/// <param name="colIndexArray">The column index array as a <see cref="Storage{T}"/> of <typeparamref name="TInd"/></param>
		/// <param name="format">The atomic <see cref="SparseMatrixFormat"/> of a pre-defined value</param>
		/// <param name="defaultValue">The default value (the value not specified) of this sparse vector</param>
		/// <param name="stores">The number of stored values, default 0 means the length of <paramref name="valueArray"/></param>
		/// <exception cref="NotSupportedException">If the <typeparamref name="TInd"/> is not an integral type</exception>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="format"/> is not atomic or not of allowed format</exception>
		/// <exception cref="ArgumentException">If the lengths of storages does not fit the underlying regulations indicated by <paramref name="format"/></exception>
		public SparseMatrix(long rows, long cols, Storage<T> valueArray, Storage<TInd> rowIndexArray, Storage<TInd> colIndexArray, SparseMatrixFormat format, T defaultValue = default, long stores = 0) :
			base(rows, cols, valueArray, rowIndexArray, colIndexArray, format, defaultValue, stores,
				rowLength: GetRowLength(rows, valueArray, stores, format),
				colLength: GetColLength(cols, valueArray, stores, format))
		{ }

		private SparseMatrix(SparseMatrix<T, TInd> reference) : base(reference.NRows, reference.NCols, reference.Storage.MakeReference(), reference.RowIndexStorage.MakeReference(), reference.ColIndexStorage.MakeReference(), reference.Format, reference.DefaultValue) { }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static long GetRowLength(long rows, Storage<T> valueArray, long stores, SparseMatrixFormat format)
		{
			if (stores <= 0)
				stores = valueArray.Length;
			return format switch
			{
				SparseMatrixFormat.COOR or SparseMatrixFormat.COOC or SparseMatrixFormat.CSC => stores,
				SparseMatrixFormat.CSR => rows + 1,
				_ => throw new ArgumentOutOfRangeException(nameof(format), format, Resources.Parameter.InvalidValue),
			};
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static long GetColLength(long cols, Storage<T> valueArray, long stores, SparseMatrixFormat format)
		{
			if (stores <= 0)
				stores = valueArray.Length;
			return format switch
			{
				SparseMatrixFormat.COOR or SparseMatrixFormat.COOC or SparseMatrixFormat.CSR => stores,
				SparseMatrixFormat.CSC => cols + 1,
				_ => -1,
			};
		}
		#endregion

		#region clone related
		/// <summary>
		/// Deep clone the sparse matrix, the mutable status will not be copied.
		/// </summary>
		/// <returns>The cloned sparse matrix</returns>
		public override SparseMatrix<T, TInd> Clone()
		{
			var indexArrays = ((ISparseArray<T, TInd>)this).NewArraysAlike<T, TInd>(out ActualStorage<T> value, copyContent: true);
			return new SparseMatrix<T, TInd>(this.NRows, this.NCols, value, indexArrays[0], indexArrays[1], this.Format, this.DefaultValue);
		}

		/// <summary>
		/// Create a new sparse matrix with same properties as this one while the underlying storages are not filled.
		/// </summary>
		/// <returns>The new sparse matrix alike this one</returns>
		public override SparseMatrix<T, TInd> NewArrayAlike()
		{
			var indexArrays = ((ISparseArray<T, TInd>)this).NewArraysAlike<T, TInd>(out ActualStorage<T> value, copyContent: false);
			return new SparseMatrix<T, TInd>(this.NRows, this.NCols, value, indexArrays[0], indexArrays[1], this.Format, this.DefaultValue);
		}

		/// <summary>
		/// Create a new sparse matrix with same properties as this one while the underlying storages are not filled and the data type is changed to <typeparamref name="TOut"/> while index type changed to <typeparamref name="TIndOut"/>.
		/// </summary>
		/// <typeparam name="TOut">Any unmanaged struct as the new data type</typeparam>
		/// <typeparam name="TIndOut">Any integral-typed unmanaged struct as the new index type</typeparam>
		/// <returns>The new sparse matrix alike this one</returns>
		/// <exception cref="TypeMismatchException">If the <typeparamref name="TIndOut"/> is not an integral type</exception>
		public override SparseMatrix<TOut, TIndOut> NewArrayAlike<TOut, TIndOut>()
		{
			var indexArrays = ((ISparseArray<T, TInd>)this).NewArraysAlike<TOut, TIndOut>(out ActualStorage<TOut> value, copyContent: false);
			return new SparseMatrix<TOut, TIndOut>(this.NRows, this.NCols, value, indexArrays[0], indexArrays[1], this.Format, this.DefaultValue.GenericConvert<TOut, T>());
		}
		#endregion

		#region indexer helpers
		#region sub matrix get
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void RefColumValue(long[] rowStarts, ref Storage<TInd> column, ref Storage<T> values, TInd y1 = default)
		{
			long offset = rowStarts[0], length = rowStarts[^1] - rowStarts[0];
			values = values.MakeReference(offset, length);
			column = column.MakeReference(offset, length);
			if (!y1.IsZero())
			{
				column = column.ApplyToClone(c => LAD.PointWiseAddScalar(c, 1, y1.GenericNegate()));
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void GetRange(long rows, TInd y1, TInd y2, long[] rowStarts, ref Storage<TInd> column, ref Storage<T> values)
		{
			long length = 0;
			bool allColumns = true;
			Storage<T>[] valueArray = new Storage<T>[rows];
			Storage<TInd>[] indexArray = new Storage<TInd>[rows];
			for (long i = 0; i < rows; i++)
			{
				long off = rowStarts[i], len = rowStarts[i + 1] - off;
				Storage<T> value = values.MakeReference(off, len);
				Storage<TInd> index = column.MakeReference(off, len);
				SparseVector<T, TInd>.Slice(y1, y2, ref value, ref index);
				if (allColumns && value.Length != len)
					allColumns = false;
				valueArray[i] = value; indexArray[i] = index;
				length += value.Length;
				rowStarts[i + 1] = off + value.Length;
			}
			if (allColumns)
			{
				RefColumValue(rowStarts, ref column, ref values, y1);
			}
			else
			{
				ActualStorage<T>? outValues = null;
				ActualStorage<TInd>? outColumn = null;
				try
				{
					outValues = values.MakeReference(newLength: length).CreateAlike();
					outColumn = column.MakeReference(newLength: length).CreateAlike();
					long offset = 0;
					for (long i = 0; i < rows; i++)
					{
						MEM.MemoryCopy(valueArray[i], outValues.MakeReference(offset));
						offset += MEM.MemoryCopy(indexArray[i], outColumn.MakeReference(offset));
					}
					LAD.PointWiseAddScalar(outColumn, 1, y1.GenericNegate());
					values = outValues; column = outColumn;
				}
				catch (Exception)
				{
					outValues?.Dispose(); outColumn?.Dispose();
					throw;
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void GetRangeCoordinated(TInd x1, TInd x2, TInd y1, TInd y2, bool allColumns, ref Storage<TInd> sortedRow, ref Storage<TInd> column, ref Storage<T> values)
		{
			long rows = x2.ToLong() - x1.ToLong() + 1;
			long[] rowStarts = new long[rows];
			using (var temp = sortedRow.MakeReference(newLength: rows).CreateAlike<long>())
			{
				LAS.IndexGetAllBounds(sortedRow, temp, x1, x2, lowerBound: true);
				MEM.ToManaged(temp, rowStarts);
				if (rowStarts[^1] < 0)
					rowStarts[^1] = sortedRow.Length;
			}
			rows--;
			// get column and value
			if (allColumns)
			{
				RefColumValue(rowStarts, ref column, ref values);
			}
			else
			{
				GetRange(rows, y1, y2, rowStarts, ref column, ref values);
			}
			// get compressed row
			var compressed = Storage<TInd>.Create(sortedRow[0].Location, rowStarts.LongLength);
			try
			{
				if (compressed is Storage<long> ss)
				{
					MEM.FromManaged(ss, rowStarts);
				}
				else
				{
					using var temp = Storage<long>.Create(sortedRow[0].Location, rowStarts.LongLength);
					MEM.FromManaged(temp, rowStarts);
					LAD.PointWiseCast(temp, 1, compressed, 1);
				}
			}
			catch (Exception)
			{
				compressed?.Dispose();
				throw;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void GetRangeCompressed(long start, long count, TInd y1, TInd y2, bool allColumns, ref Storage<TInd> compressed, ref Storage<TInd> column, ref Storage<T> values)
		{
			long[] rowStarts = new long[count + 1];
			if (compressed is Storage<long> ss)
			{
				MEM.ToManaged(ss.MakeReference(start, count + 1), rowStarts);
			}
			else
			{
				using var temp = compressed.MakeReference(newLength: count + 1).CreateAlike<long>();
				LAD.PointWiseCast(compressed.MakeReference(start, count + 1), 1, temp, 1);
				MEM.ToManaged(temp, rowStarts);
			}
			// return
			if (allColumns)
			{
				RefColumValue(rowStarts, ref column, ref values);
			}
			else
			{
				GetRange(count, y1, y2, rowStarts, ref column, ref values);
			}
			bool allRows = count == compressed.Length - 1;
			if (allRows)
			{
				compressed = compressed.MakeReference();
			}
			else
			{
				ActualStorage<TInd> outCompressed = compressed.MakeReference(newLength: count + 1).Clone();
				try
				{
					LAD.PointWiseAddScalar(outCompressed, 1, (-rowStarts[0]).FromLong<TInd>());
					compressed = outCompressed;
				}
				catch (Exception)
				{
					outCompressed?.Dispose();
					throw;
				}
			}
		}
		#endregion

		#region sub matrix set
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void SetRange(long rows, TInd y1, TInd y2, long[] rowStarts, Storage<TInd> column, Storage<T> values, Storage<TInd> srcColumn, Storage<T> srcValues)
		{
			// combine all consecutive copies
			int estimateLen = (int)(rows / 2);
			List<long> dstOffsets = new(estimateLen), cpyLengths = new(estimateLen), srcOffsets = new(estimateLen);
			for (long i = 0; i < rows; i++)
			{
				long offset = rowStarts[i], length = rowStarts[i + 1] - offset;
				Storage<T> value = values.MakeReference(offset, length);
				Storage<TInd> index = column.MakeReference(offset, length);
				SparseVector<T, TInd>.Slice(y1, y2, ref value, ref index);
				long realOffset = value - values, relativeOffset = realOffset - offset;
				if (value.Length == length)
				{	// both align
					cpyLengths[^1] += length;
				}
				else if (realOffset == offset)
				{	// left align
					cpyLengths[^1] += value.Length;
					// create next
					dstOffsets.Add(rowStarts[i + 1]);
					srcOffsets.Add(rowStarts[i + 1] - rowStarts[0]);
					cpyLengths.Add(0);
				}
				else if (value.Length + relativeOffset == length)
				{   // right align
					if (cpyLengths[^1] == 0)
					{
						cpyLengths[^1] = value.Length;
						dstOffsets[^1] += relativeOffset;
					}
					else
					{	// create this
						dstOffsets.Add(rowStarts[i + 1] + relativeOffset);
						srcOffsets.Add(rowStarts[i + 1] - rowStarts[0]);
						cpyLengths.Add(value.Length);
					}
				}
				else
				{	// no align
					if (cpyLengths[^1] == 0)
					{
						cpyLengths[^1] = value.Length;
						dstOffsets[^1] += realOffset;
					}
					// create next
					dstOffsets.Add(rowStarts[i + 1]);
					srcOffsets.Add(rowStarts[i + 1] - rowStarts[0]);
					cpyLengths.Add(0);
				}
			}
			// actual copy
			for (int i = 0; i < cpyLengths.Count; i++)
			{
				if (cpyLengths[i] <= 0)
					continue;
				var tempVal = values.MakeReference(dstOffsets[i], cpyLengths[i]);
				var tempCol = column.MakeReference(dstOffsets[i], cpyLengths[i]);
				MEM.MemoryCopy(srcValues + srcOffsets[i], tempVal);
				MEM.MemoryCopy(srcColumn + srcOffsets[i], tempCol);
				LAD.PointWiseAddScalar(tempCol, 1, y1);
			}
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void SetRangeCoordinated(TInd x1, TInd x2, TInd y1, TInd y2, SparseMatrix<T, TInd> org, SparseMatrix<T, TInd> src)
		{
			bool allRows = src.NRows == org.NRows, allCols = src.NCols == org.NCols;
			// get storages
			bool setCompressed = src.Format != org.Format;
			var values = org.Storage; var srcValues = src.Storage;
			if (!org.Format.IsRowMajor())
				(x1, x2, y1, y2) = (y1, y2, x1, x2);
			var (sortedRow, column) = org.Format.IsRowMajor() ? (org.RowIndexStorage, org.ColIndexStorage) : (org.ColIndexStorage, org.RowIndexStorage);
			var (srcRow, srcColumn) = org.Format.IsRowMajor() ? (src.RowIndexStorage, src.ColIndexStorage) : (src.ColIndexStorage, src.RowIndexStorage);
			// check row
			long offset = LAS.IndexBound(sortedRow, x1, lowerBound: true);
			long length = LAS.IndexBound(sortedRow, x2, lowerBound: false);
			if (length < 0)
				length = sortedRow.Length;
			length -= offset;
			if (!setCompressed)
			{
				if (srcRow.Length != length)
					throw new ArgumentException(Resources.Parameter.InvalidValue);
				using var temp = srcRow.Clone();
				LAD.PointWiseAddScalar(temp, 1, x1);
				// TODO: this is not correct
				if (!LAD.PointWiseEquals(temp, 1, sortedRow.MakeReference(offset, length), 1))
					throw new ArgumentException(Resources.Parameter.InvalidValue);
			}
			// get row starts
			long rows = x2.ToLong() - x1.ToLong() + 1;
			long[] rowStarts = new long[rows];
			using (var temp = sortedRow.MakeReference(newLength: rows).CreateAlike<long>())
			{
				LAS.IndexGetAllBounds(sortedRow, temp, x1, x2, lowerBound: true);
				var tempLast = temp + (rows - 1);
				if (MEM.ToManaged(tempLast) < 0)
					MEM.FromManaged(tempLast, sortedRow.Length);
				if (setCompressed)
				{ // check row
					bool check;
					if (temp is Storage<TInd> tt)
					{
						check = LAD.PointWiseEquals(tt, 1, srcRow, 1);
					}
					else
					{
						using var temp2 = temp.CreateAlike<TInd>();
						LAD.PointWiseCast(temp, 1, temp2, 1);
						check = LAD.PointWiseEquals(temp2, 1, srcRow, 1);
					}
					if (!check)
						throw new ArgumentException(Resources.Parameter.InvalidValue);
				}
				MEM.ToManaged(temp, rowStarts);
			}
			// set
			rows--;
			if (allRows && allCols)
			{
				if (srcColumn.Length == length && srcValues.Length == length)
				{
					MEM.MemoryCopy(srcValues, values + rowStarts[0]);
					MEM.MemoryCopy(srcColumn, column + rowStarts[0]);
				}
				else
					throw new ArgumentException(Resources.Parameter.InvalidValue);
			}
			else if (allRows && !allCols)
			{
				SetRange(rows, y1, y2, rowStarts, column, values, srcColumn, srcValues);
			}
			MEM.MemoryCopy(srcRow, sortedRow + rowStarts[0]);
			if (!x1.IsZero())
				LAD.PointWiseAddScalar(sortedRow.MakeReference(rowStarts[0], length), 1, x1);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void SetRangeCompressed(TInd x1, TInd x2, TInd y1, TInd y2, SparseMatrix<T, TInd> org, SparseMatrix<T, TInd> src)
		{
			long start = x1.ToLong(), count = x2.ToLong() - start;
			bool allRows = src.NRows == org.NRows, allCols = src.NCols == org.NCols;
			// get storages
			bool setCoordinated = src.Format != org.Format;
			var values = org.Storage; var srcValues = src.Storage;
			if (!org.Format.IsRowMajor())
				(x1, x2, y1, y2) = (y1, y2, x1, x2);
			var (sortedRow, column) = org.Format.IsRowMajor() ? (org.RowIndexStorage, org.ColIndexStorage) : (org.ColIndexStorage, org.RowIndexStorage);
			var (srcRow, srcColumn) = org.Format.IsRowMajor() ? (src.RowIndexStorage, src.ColIndexStorage) : (src.ColIndexStorage, src.RowIndexStorage);
			// check row
			if (!setCoordinated)
			{
				using var temp = srcRow.Clone();
				LAD.PointWiseAddScalar(temp, 1, x1.GenericNegate());
				if (!LAD.PointWiseEquals(temp, 1, sortedRow.MakeReference(start, count), 1))

			}
			// get row starts
			long[] rowStarts = new long[count + 1];
			using (var temp = compressed.MakeReference(newLength: count + 1).CreateAlike<long>())
			{
				LAD.PointWiseCast(compressed.MakeReference(start, count + 1), 1, temp, 1);
				MEM.ToManaged(temp, rowStarts);
			}
			// set
		}
		#endregion

		#region element indexing
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private T IndexCoordinated(TInd x, TInd y, Storage<TInd> sorted, Storage<TInd> other, T value, bool get)
		{
			long find = 0;
			while (find >= 0)
			{
				long newFind = LAS.IndexFind(sorted: true, sorted + find, x);
				if (newFind < 0)
					break;
				find += newFind;
				if (MEM.ToManaged(other + find).Equals(y))
				{
					if (get)
						return MEM.ToManaged(this.Storage + find);
					MEM.FromManaged(this.Storage + find, value);
					return default;
				}
				if (find >= this.ActualLength)
					break;
			}
			// not stored
			if (get)
				return value;
			else if (value.Equals(this.DefaultValue))
				return default;
			else // cannot set
				throw new InvalidOperationException();
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private T IndexCompressed(long x, TInd y, Storage<TInd> sorted, Storage<TInd> other, T value, bool get)
		{
			long start = MEM.ToManaged(sorted + x).ToLong();
			long count = MEM.ToManaged(sorted + (x + 1)).ToLong() - start;
			if (count > 0)
			{
				long find = LAS.IndexFind(sorted: true, other.MakeReference(start, count), y);
				if (find >= 0)
				{
					if (get)
						return MEM.ToManaged(this.Storage + (find + start));
					MEM.FromManaged(this.Storage + (find + start), value);
					return default;
				}
			}
			// not stored
			if (get)
				return value;
			else if (value.Equals(this.DefaultValue))
				return default;
			else // cannot set
				throw new InvalidOperationException();
		}
		#endregion
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
		public override SparseMatrix<T, TInd> GetSubmatrix(long offsetRow, long countRow, long offsetCol, long countCol)
		{
			this.CheckRange(offsetRow, countRow, offsetCol, countCol);

			TInd x1 = offsetRow.FromLong<TInd>(), x2 = (offsetRow + countRow).FromLong<TInd>();
			TInd y1 = offsetCol.FromLong<TInd>(), y2 = (offsetCol + countCol).FromLong<TInd>();
			bool allRows = countRow == this.NRows, allCols = countCol == this.NCols;
			// shortcut
			if (allRows && allCols)
				return new SparseMatrix<T, TInd>(this);
			// else
			SparseMatrixFormat format = this.Format;
			var values = this.Storage; var rowInd = this.RowIndexStorage; var colInd = this.ColIndexStorage;
			switch (this.Format)
			{
				case SparseMatrixFormat.COOR:
					GetRangeCoordinated(x1, x2, y1, y2, allCols, ref rowInd, ref colInd, ref values);
					format = SparseMatrixFormat.CSR;
					break;
				case SparseMatrixFormat.COOC:
					GetRangeCoordinated(y1, y2, x1, x2, allRows, ref colInd, ref rowInd, ref values);
					format = SparseMatrixFormat.CSC;
					break;
				case SparseMatrixFormat.CSR:
					GetRangeCompressed(offsetRow, countRow, y1, y2, allCols, ref rowInd, ref colInd, ref values);
					break;
				case SparseMatrixFormat.CSC:
					GetRangeCompressed(offsetCol, countCol, x1, x2, allRows, ref colInd, ref rowInd, ref values);
					break;
				default: // never here
					throw new NotSupportedException();
			}
			return new SparseMatrix<T, TInd>(countRow, countCol, values, rowInd, colInd, format, this.DefaultValue);
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
			if (overwrite is not (IDenseMatrix or SparseMatrix<T, TInd>))
				throw new ArgumentException(Resources.Parameter.InvalidValue, nameof(overwrite));
			if (overwrite is IDenseMatrix dn && (dn.NRows < countRow || dn.NCols < countCol))
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(overwrite));
			if (overwrite is SparseMatrix<T, TInd> sp && (sp.NRows != countRow || sp.NCols != countCol))
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(overwrite));

			if (overwrite is IDenseMatrix)
			{
				using var sub = this.GetSubmatrix(offsetRow, countRow, offsetCol, countCol);
				sub.ToDense(overwrite);
			}
			else if (overwrite is SparseMatrix<T, TInd> sparse)
			{
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
			this.CheckRange(rowStart, value.NRows, colStart, value.NCols);
			if (value is not SparseMatrix<T, TInd> sp || sp.Format.IsRowMajor() != this.Format.IsRowMajor())
				throw new ArgumentException(Resources.Parameter.InvalidValue, nameof(value));

			TInd x1 = rowStart.FromLong<TInd>(), x2 = (rowStart + value.NRows).FromLong<TInd>();
			TInd y1 = colStart.FromLong<TInd>(), y2 = (colStart + value.NCols).FromLong<TInd>();
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
			switch (this.Format)
			{
				case SparseMatrixFormat.COOR:
				case SparseMatrixFormat.COOC:
					SetRangeCoordinated(x1, x2, y1, y2, this, sp);
					break;
				case SparseMatrixFormat.CSR:
				case SparseMatrixFormat.CSC:
					SetRangeCompressed(x1, x2, y1, y2, this, sp);
					break;
				default: // never here
					throw new NotSupportedException();
			}
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
				TInd xx = x.FromLong<TInd>(), yy = y.FromLong<TInd>();
				return this.Format switch
				{
					SparseMatrixFormat.COOR => this.IndexCoordinated(xx, yy, this.RowIndexStorage, this.ColIndexStorage, this.DefaultValue, get: true),
					SparseMatrixFormat.COOC => this.IndexCoordinated(yy, xx, this.ColIndexStorage, this.RowIndexStorage, this.DefaultValue, get: true),
					SparseMatrixFormat.CSR => this.IndexCompressed(x, yy, this.RowIndexStorage, this.ColIndexStorage, this.DefaultValue, get: true),
					SparseMatrixFormat.CSC => this.IndexCompressed(y, xx, this.ColIndexStorage, this.RowIndexStorage, this.DefaultValue, get: true),
					_ => default,
				};
			}
			set {
				this.CheckIndex(x, y);
				TInd xx = x.FromLong<TInd>(), yy = y.FromLong<TInd>();
				switch (this.Format)
				{
					case SparseMatrixFormat.COOR:
						this.IndexCoordinated(xx, yy, this.RowIndexStorage, this.ColIndexStorage, value, get: false);
						break;
					case SparseMatrixFormat.COOC:
						this.IndexCoordinated(yy, xx, this.ColIndexStorage, this.RowIndexStorage, value, get: false);
						break;
					case SparseMatrixFormat.CSR:
						this.IndexCompressed(x, yy, this.RowIndexStorage, this.ColIndexStorage, value, get: false);
						break;
					case SparseMatrixFormat.CSC:
						this.IndexCompressed(y, xx, this.ColIndexStorage, this.RowIndexStorage, value, get: false);
						break;
					default:
						break;
				}
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
		/// <returns>The converted <see cref="SparseMatrix{T, TInd}"/> whose <see cref="Althea.Arrays.SparseMatrix{T, TInd}.Format"/> fits the given <paramref name="format"/>, or this one if no conversion is necessary</returns>
		/// <exception cref="NotSupportedException">If <paramref name="format"/> is not composed of internally defined values</exception>
		public override Althea.Arrays.SparseMatrix<T, TInd> ToFormat(SparseMatrixFormat format)
		{
			if ((format & this.Format) != 0)
				return this;
			if ((format & FormatExtension.PreDefined) == 0)
				throw new NotSupportedException(Resources.Support.Format);

			return LAS.MatrixSparseFormatConvert(this, format) as Althea.Arrays.SparseMatrix<T, TInd>;
		}
		#endregion

		#region reshape
		/// <summary>
		/// When implemented by a derived class, convert this matrix to a vector
		/// </summary>
		/// <returns>The new vector reshaped from this matrix</returns>
		public override SparseVector<T, TInd> ToVector()
		{
			return LAS.SparseMatrixToVector(this, SparseVectorFormat.Coordinated) as SparseVector<T, TInd>;
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

		}

		/// <summary>
		/// Reshape the array to a tensor with dimensionality = <paramref name="size"/>.
		/// </summary>
		/// <param name="size">The new size/dimensionality with at most one or zero uncertain dimension indicated by a non-positive number.</param>
		/// <returns>The reshaped tensor</returns>
		public override ValueArray<T> ToTensor(ReadOnlySpan<long> size)
		{

		}
		#endregion

		#region linear algebra
		/// <summary>
		/// Create a new <see cref="MatrixBase{T}"/> which is the simple operation result of this matrix under <paramref name="operation"/>.
		/// </summary>
		/// <param name="operation">The input <see cref="MatrixOperation"/> used as the simple operation to be applied</param>
		/// <returns>A new <see cref="DenseMatrix{T}"/> as the result of <paramref name="operation"/>(this)</returns>
		/// <exception cref="NotSupportedException">If the given <paramref name="operation"/> is not supported</exception>
		public override SparseMatrix<T, TInd> ApplyOperation(MatrixOperation operation)
		{
			// shortcut
			if (operation == MatrixOperation.None)
				return this.Clone();
			// otherwise
			var (m, n) = (this.NCols, this.NRows);
			var storageOut = this.Storage.MakeReference(newLength: m * n).CreateAlike();
			try
			{
				LAD.GeneralMatricesAdd(operation, MatrixOperation.None, m, n, Scalars<T>.One, this.Storage, this.LeadDim, Scalars<T>.Zero, null, 0, storageOut, m);
				return new SparseMatrix<T, TInd>(storageOut, m, n);
			}
			catch (Exception)
			{
				storageOut?.Dispose();
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
		public override SparseMatrix<T, TInd> AddMatrix(T scalarThis, T scalarOther, MatrixBase<T> other, MatrixOperation opThis = MatrixOperation.None, MatrixOperation opOther = MatrixOperation.None)
		{
			if (other is null || !other.IsValid())
				throw new ArgumentNullException(nameof(other));
			if (scalarThis.IsZero())
				throw new ArgumentOutOfRangeException(nameof(scalarThis), Resources.Parameter.CannotZero);
			if (scalarOther.IsZero())
				throw new ArgumentOutOfRangeException(nameof(scalarOther), Resources.Parameter.CannotZero);
			var (m, n) = opThis == MatrixOperation.None ? (this.NRows, this.NCols) : (this.NCols, this.NRows);
			var (p, q) = opOther == MatrixOperation.None ? (other.NRows, other.NCols) : (other.NCols, other.NRows);
			if (m != p || n != q)
				throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(other));

			var storageOut = this.Storage.MakeReference(newLength: m * n).CreateAlike();
			try
			{
				if (other is DenseMatrix<T> dense)
				{
					LAD.GeneralMatricesAdd(opThis, opOther, m, n, scalarThis, this.Storage, this.LeadDim, scalarOther, dense.Storage, dense.LeadDim, storageOut, m);
				}
				else if (other is ISparseMatrix<T> sparse)
				{
					LAS.MatrixDenseAddSparse(opThis, opOther, scalarThis, this.Storage, this.LeadDim, scalarOther, sparse, storageOut, m);
				}
				else
					throw new NotSupportedException();

				return new DenseMatrix<T>(storageOut, m, n);
			}
			catch (Exception)
			{
				storageOut?.Dispose();
				throw;
			}
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
		public override SparseMatrix<T, TInd> MultiplyMatrix(T scalar, MatrixBase<T> other, MatrixOperation opThis = MatrixOperation.None, MatrixOperation opOther = MatrixOperation.None)
		{
			if (other is null || !other.IsValid())
				throw new ArgumentNullException(nameof(other));
			if (scalar.IsZero())
				throw new ArgumentOutOfRangeException(nameof(scalar), Resources.Parameter.CannotZero);
			var (m, n) = opThis == MatrixOperation.None ? (this.NRows, this.NCols) : (this.NCols, this.NRows);
			var (p, q) = opOther == MatrixOperation.None ? (other.NRows, other.NCols) : (other.NCols, other.NRows);
			if (n != p)
				throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(other));

			var storageOut = this.Storage.MakeReference(newLength: m * q).CreateAlike();
			try
			{
				if (other is DenseMatrix<T> dense)
				{
					LAD.GeneralMatricesMultiply(opThis, opOther, m, q, n, scalar, this.Storage, this.LeadDim, dense.Storage, dense.LeadDim, Scalars<T>.Zero, storageOut, m);
				}
				else if (other is ISparseMatrix<T> sparse)
				{
					LAS.MatrixDenseMultiplySparse(opThis, opOther, m, scalar, this.Storage, this.LeadDim, sparse, Scalars<T>.Zero, storageOut, m);
				}
				else
					throw new NotSupportedException();

				return new DenseMatrix<T>(storageOut, m, q);
			}
			catch (Exception)
			{
				storageOut?.Dispose();
				throw;
			}
		}
		#endregion

		#region IKrylovVector
		void IKrylovVector<SparseMatrix<T, TInd>, T>.Scale(T value) => this.Scale(value);

		double IKrylovVector<SparseMatrix<T, TInd>, T>.Norm() => this.Norm();

		void IKrylovVector<SparseMatrix<T, TInd>, T>.Normalize() => this.Normalize();

		T IKrylovVector<SparseMatrix<T, TInd>, T>.Dot(SparseMatrix<T, TInd> other)
		{
			if (other is null || !other.IsValid())
				throw new ArgumentNullException(nameof(other));
			if (this.NRows != other.NRows || this.NCols != other.NCols)
				throw new InvalidOperationException(Resources.Parameter.NotSameSize);

			if (this.NRows == this.LeadDim)
			{
				if (other.NRows == other.LeadDim)
				{
					return LAD.Dot(true, this.Storage, 1, other.Storage, 1);
				}
				else if (other.NRows == 1)
				{
					return LAD.Dot(true, this.Storage, 1, other.Storage, checked((int)other.LeadDim));
				}
			}
			else if (this.NRows == 1)
			{
				if (other.NRows == other.LeadDim)
				{
					return LAD.Dot(true, this.Storage, checked((int)this.LeadDim), other.Storage, 1);
				}
				else if (other.NRows == 1)
				{
					return LAD.Dot(true, this.Storage, checked((int)this.LeadDim), other.Storage, checked((int)other.LeadDim));
				}
			}
			// else
			dynamic dotSquare = default(T);
			long rows = this.NRows, cols = this.NCols, ldA = this.LeadDim, ldB = other.LeadDim;
			for (long i = 0; i < cols; i++)
			{
				var columnA = this.Storage.MakeReference(i * ldA, newLength: rows);
				var columnB = other.Storage.MakeReference(i * ldB, newLength: rows);
				T dot = LAD.Dot(true, columnA, 1, columnB, 1);
				dotSquare += (dynamic)dot * dot;
			}
			return ((T)dotSquare).GenericSqrt();
		}

		/// <summary>
		/// Add the <paramref name="other"/> (scaling by <paramref name="scalar"/>) matrix to this matrix in-place.
		/// </summary>
		/// <param name="other">The other <see cref="DenseMatrix{T}"/> to add</param>
		/// <param name="scalar">The scalar to be multiplied to <paramref name="other"/> of type <typeparamref name="T"/></param>
		public virtual void AddBy(SparseMatrix<T, TInd> other, T scalar) => this.OverwriteByMatricesSum(this, other, Scalars<T>.One, scalar);

		/// <summary>
		/// When implemented by a derived class, replace this matrix's content with the <paramref name="other"/> vector in-place.
		/// </summary>
		/// <param name="other">The other dense vector to replace from</param>
		/// <exception cref="ArgumentNullException">If <paramref name="other"/> is null or invalid</exception>
		/// <exception cref="InvalidOperationException">If the replacement cannot be done in-place due to reason(s) such as different sparsities between this and <paramref name="other"/></exception>
		public void ReplaceBy(SparseMatrix<T, TInd> other)
		{
			if (other is null || !other.IsValid())
				throw new ArgumentNullException(nameof(other));
			if (this.NRows != other.NRows || this.NCols != other.NCols)
				throw new InvalidOperationException(Resources.Parameter.NotSameSize);

			MEM.MemoryCopy2D(other.Storage, other.LeadDim, this.Storage, this.LeadDim, this.NRows, this.NCols);
		}

		/// <summary>
		/// When implemented by a derived class, multiply the matrix whose columns are indicated by <paramref name="unjoinedVectors"/> to a dense vector indicated by a <see cref="ReadOnlySpan{T}"/> and obtain the result vector as a <see cref="SparseMatrix{T, TInd}"/>.
		/// </summary>
		/// <param name="unjoinedVectors">The columns of the matrix to be multiplied</param>
		/// <param name="input">The input dense vector to be multiplied as a <see cref="ReadOnlySpan{T}"/></param>
		/// <returns>The product of <paramref name="unjoinedVectors"/> and <paramref name="input"/> as a <see cref="SparseMatrix{T, TInd}"/></returns>
		/// <remarks>The method shall be basically static, the information of this matrix shall only be used to verify the consistency of <paramref name="unjoinedVectors"/></remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="unjoinedVectors"/> or any of its element is null or invalid, or <paramref name="input"/> is empty</exception>
		/// <exception cref="ArgumentException">If <paramref name="input"/> and <paramref name="unjoinedVectors"/> have different size, or any element of <paramref name="unjoinedVectors"/> has different size than this matrix</exception>
		public SparseMatrix<T, TInd> OperateOn(IReadOnlyList<SparseMatrix<T, TInd>> unjoinedVectors, ReadOnlySpan<T> input)
		{
			if (unjoinedVectors is null || unjoinedVectors.Count == 0)
				throw new ArgumentNullException(nameof(unjoinedVectors));
			if (input.IsEmpty)
				throw new ArgumentNullException(nameof(input));
			if (unjoinedVectors.Count != input.Length)
				throw new ArgumentException(Resources.Parameter.NotSameSize);

			// sort first to reduce errors
			int length = input.Length;
			Span<T> values = length.CheckStackLimit<T>() ?? stackalloc T[length];
			Span<double> keys = length.CheckStackLimit<double>() ?? stackalloc double[length];
			for (int i = 0; i < length; i++)
			{
				values[i] = input[i];
				keys[i] = input[i].GenericAbsolute();
			}
			keys.Sort(values);

			var vec = this.NewArrayAlike();
			try
			{
				vec.FillWith(default);
				for (int i = 0; i < length; i++)
				{
					var dnvec = unjoinedVectors[i];
					if (dnvec is null || !dnvec.IsValid())
						throw new ArgumentNullException(nameof(unjoinedVectors));
					if (dnvec.Length != this.Length)
						throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(unjoinedVectors));
					if (!values[i].IsZero())
						vec.AddBy(dnvec, values[i]);
				}
				return vec;
			}
			catch (Exception)
			{
				vec.Dispose();
				throw;
			}
		}
		#endregion

		#region helper methods
		/// <summary>
		/// The helper method used in <see cref="Althea.Arrays.SparseMatrix{T, TInd}.Print(PrintSettings?)"/> to get the first several row and column indices of this sparse matrix
		/// </summary>
		/// <param name="rowIndices">The output <see cref="Span{T}"/> of <see cref="long"/> used to store the row indices</param>
		/// <param name="colIndices">The output <see cref="Span{T}"/> of <see cref="long"/> used to store the column indices</param>
		protected override void GetIndices(Span<long> rowIndices, Span<long> colIndices)
		{
			int length = rowIndices.Length;
			// TODO
		}

		/// <summary>
		/// The helper method used by <see cref="Althea.Arrays.SparseMatrix{T, TInd}.GetPointers"/> to get the index storages' names. Only used when the sparse array contains more than one index storages.
		/// </summary>
		/// <param name="orderOfIndexStorage">The index of all index storages of this sparse matrix</param>
		/// <returns>The name the index storage indicated by the given <paramref name="orderOfIndexStorage"/></returns>
		protected override string IndexStorageNameOf(int orderOfIndexStorage) => string.Empty;
		#endregion
	}
}
