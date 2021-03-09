using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
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
		/// <param name="format">The atomic <see cref="SparseMatrixFormat"/> of a <see cref="FormatExtension.NonBlocked"/> value</param>
		/// <param name="defaultValue">The default value (the value not specified) of this sparse vector</param>
		/// <param name="stores">The number of stored values, default 0 means the length of <paramref name="valueArray"/></param>
		/// <exception cref="TypeMismatchException">If the <typeparamref name="TInd"/> is not an integral type</exception>
		/// <exception cref="ArgumentNullException">If the size is not 0 while any of the storages is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="format"/> is not atomic or not of allowed format</exception>
		/// <exception cref="ArgumentException">If the lengths of storages does not fit the underlying regulations indicated by <paramref name="format"/></exception>
		public SparseMatrix(long rows, long cols, Storage<T> valueArray, Storage<TInd> rowIndexArray, Storage<TInd> colIndexArray, SparseMatrixFormat format, T defaultValue = default, long stores = 0) :
			base(rows, cols, valueArray, rowIndexArray, colIndexArray, format, defaultValue, stores,
				rowLength: GetRowLength(rows, valueArray, stores, format),
				colLength: GetColLength(cols, valueArray, stores, format))
		{
			if ((format & FormatExtension.NonBlocked) != format)
				throw new ArgumentOutOfRangeException(nameof(format), format, Resources.Parameter.InvalidValue);
		}

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
			var indexArrays = ((ISparseArray<T, TInd>)this).NewArraysAlike<T, TInd>(out ActualStorage<T> value, copyValues: true);
			return new SparseMatrix<T, TInd>(this.NRows, this.NCols, value, indexArrays[0], indexArrays[1], this.Format, this.DefaultValue);
		}

		/// <summary>
		/// Create a new sparse matrix with same properties as this one while the underlying storages are not filled.
		/// </summary>
		/// <returns>The new sparse matrix alike this one</returns>
		public override SparseMatrix<T, TInd> NewArrayAlike()
		{
			var indexArrays = ((ISparseArray<T, TInd>)this).NewArraysAlike<T, TInd>(out ActualStorage<T> value, copyValues: false);
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
			var indexArrays = ((ISparseArray<T, TInd>)this).NewArraysAlike<TOut, TIndOut>(out ActualStorage<TOut> value, copyValues: false);
			return new SparseMatrix<TOut, TIndOut>(this.NRows, this.NCols, value, indexArrays[0], indexArrays[1], this.Format, this.DefaultValue.GenericConvert<TOut, T>());
		}
		#endregion

		#region indexer helpers
		#region common
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static long ToLong(TInd i) => i.ReflectionConvert<TInd, long>();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static TInd ToInd(long i) => i.ReflectionConvert<long, TInd>();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static long[] ToManaged(Storage<TInd> storage)
		{
			long[] result = new long[storage.Length];
			if (storage is Storage<long> ss)
			{
				MEM.ToManaged(ss, result);
			}
			else
			{
				using var temp = storage.CreateAlike<long>();
				LAD.PointWiseCast(storage, 1, temp, 1);
				MEM.ToManaged(temp, result);
			}
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void ToManaged(Storage<TInd> storage, Span<long> managed)
		{
			if (storage is Storage<long> ss)
			{
				MEM.ToManaged(ss, managed);
			}
			else
			{
				using var temp = storage.CreateAlike<long>();
				LAD.PointWiseCast(storage, 1, temp, 1);
				MEM.ToManaged(temp, managed);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void FromManaged(Storage<TInd> storage, Span<long> managed)
		{
			if (storage is Storage<long> ss)
			{
				MEM.FromManaged(ss, managed);
			}
			else
			{
				using var temp = storage.CreateAlike<long>();
				MEM.FromManaged(temp, managed);
				LAD.PointWiseCast(temp, 1, storage, 1);
			}
		}
		#endregion

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
		private static long[] CoordinatedToRowStarts(TInd x1, TInd x2, long rows, Storage<TInd> sortedRow)
		{
			long[] rowStarts = new long[rows];
			using var temp = sortedRow.MakeReference(newLength: rows).CreateAlike<long>();
			LAS.IndexGetAllBounds(sortedRow, temp, x1, x2, lowerBound: true);
			MEM.ToManaged(temp, rowStarts);
			// check and set upper bound values
			int find = Array.BinarySearch(rowStarts, -1);
			if (find >= 0)
			{
				new Span<long>(rowStarts, find, rowStarts.Length - find).Fill(sortedRow.Length);
			}
			return rowStarts;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static long[] CompressedToRowStarts(long start, long count, Storage<TInd> sortedRow)
		{
			return ToManaged(sortedRow.MakeReference(start, count));
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void GetRangeCoordinated(TInd x1, TInd x2, TInd y1, TInd y2, bool allColumns, ref Storage<TInd> sortedRow, ref Storage<TInd> column, ref Storage<T> values)
		{
			long rows = ToLong(x2) - ToLong(x1) + 1;
			long[] rowStarts = CoordinatedToRowStarts(x1, x2, rows, sortedRow);
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
				FromManaged(compressed, rowStarts);
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
			long[] rowStarts = CompressedToRowStarts(start, count + 1, compressed);
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
					LAD.PointWiseAddScalar(outCompressed, 1, ToInd(-rowStarts[0]));
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
		private static void SetRange(long rows, TInd y1, TInd y2, long[] rowStarts, Storage<TInd> column, Storage<T> values, long[] srcRowStarts, Storage<TInd> srcColumn, Storage<T> srcValues)
		{
			// combine all consecutive copies
			int estimateLen = (int)(rows / 2);
			List<long> dstOffsets = new(estimateLen) { rowStarts[0] }, cpyLengths = new(estimateLen) { 0 }, srcOffsets = new(estimateLen) { 0 };
			for (long i = 0; i < rows; i++)
			{
				long offset = rowStarts[i], length = rowStarts[i + 1] - offset;
				Storage<T> value = values.MakeReference(offset, length);
				Storage<TInd> index = column.MakeReference(offset, length);
				SparseVector<T, TInd>.Slice(y1, y2, ref value, ref index);
				// check
				long realLength = value.Length;
				if (realLength != srcRowStarts[i])
					throw new ArgumentException(Resources.Parameter.InvalidValue);
				// combine consecutive copies
				long realOffset = value - values, relativeOffset = realOffset - offset;
				if (realLength == length)
				{	// both align
					cpyLengths[^1] += length;
				}
				else if (realOffset == offset)
				{	// left align
					cpyLengths[^1] += realLength;
					// create next
					dstOffsets.Add(rowStarts[i + 1]);
					srcOffsets.Add(rowStarts[i + 1] - rowStarts[0]);
					cpyLengths.Add(0);
				}
				else if (realLength + relativeOffset == length)
				{   // right align
					if (cpyLengths[^1] == 0)
					{
						cpyLengths[^1] = realLength;
						dstOffsets[^1] += relativeOffset;
					}
					else
					{	// create this
						dstOffsets.Add(rowStarts[i + 1] + relativeOffset);
						srcOffsets.Add(rowStarts[i + 1] - rowStarts[0]);
						cpyLengths.Add(realLength);
					}
				}
				else
				{	// no align
					if (cpyLengths[^1] == 0)
					{
						cpyLengths[^1] = realLength;
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
		private static void SetRange(TInd x1, TInd x2, TInd y1, TInd y2, SparseMatrix<T, TInd> org, SparseMatrix<T, TInd> src)
		{
			bool allRows = src.NRows == org.NRows, allCols = src.NCols == org.NCols, rowMajor = org.Format.IsRowMajor();
			// get storages
			var values = org.Storage; var srcValues = src.Storage;
			if (rowMajor)
				(x1, x2, y1, y2) = (y1, y2, x1, x2);
			var (sortedRow, column) = rowMajor ? (org.RowIndexStorage, org.ColIndexStorage) : (org.ColIndexStorage, org.RowIndexStorage);
			var (srcRow, srcColumn) = rowMajor ? (src.RowIndexStorage, src.ColIndexStorage) : (src.ColIndexStorage, src.RowIndexStorage);
			// get row starts
			long startRow = ToLong(x1), rows = ToLong(x2) - startRow + 1; TInd rowsInd = ToInd(rows);
			long[] rowStarts = org.Format.IsCompressed() ? CompressedToRowStarts(startRow, rows, sortedRow) : CoordinatedToRowStarts(x1, x2, rows, sortedRow);
			long[] srcRowStarts = src.Format.IsCompressed() ? CompressedToRowStarts(0, rows, srcRow) : CoordinatedToRowStarts(default, rowsInd, rows, srcRow);
			// set columns and values, rows are not necessary
			if (allRows && !allCols)
			{
				SetRange(rows - 1, y1, y2, rowStarts, column, values, srcRowStarts, srcColumn, srcValues);
			}
			else if (!allRows && allCols)
			{
				MEM.MemoryCopy(srcColumn, column + rowStarts[0]);
				MEM.MemoryCopy(srcValues, values + rowStarts[0]);
			}
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
			long start = ToLong(MEM.ToManaged(sorted + x));
			long count = ToLong(MEM.ToManaged(sorted + (x + 1))) - start;
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

		#region diagonal get
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static SparseVector<T, TInd> GetDiag(long k, long rows, bool compressed, Storage<T> values, Storage<TInd> sortedRow, Storage<TInd> column)
		{
			long absK = Math.Abs(k), actualRows = rows - absK;
			TInd rowsInd = ToInd(rows), absKInd = ToInd(absK), actualRowsInd = ToInd(actualRows);
			long[] rowStarts = compressed ? CompressedToRowStarts(k >= 0 ? 0: absK, actualRows, sortedRow) :
											CoordinatedToRowStarts(k >= 0 ? default : absKInd, k >= 0 ? actualRowsInd : rowsInd, rows - absK, sortedRow);
			// get indices
			List<long> indices = new((int)(actualRows / 2));
			for (long i = 0; i < actualRows; i++)
			{
				long offset = rowStarts[i], length = rowStarts[i + 1] - rowStarts[i];
				if (length > 0)
				{
					long find = LAS.IndexFind(sorted: true, column.MakeReference(offset, length), k >= 0 ? ToInd(i + k) : ToInd(i));
					if (find >= 0)
					{
						indices.Add(find + offset);
					}
				}
			}
			// to storage
			ActualStorage<TInd>? vectorIndices = null;
			ActualStorage<T>? vectorValues = null;
			try
			{
				vectorIndices = column.MakeReference(newLength: indices.Count).CreateAlike();
				Span<long> managedIndices = CollectionsMarshal.AsSpan(indices);
				FromManaged(vectorIndices, managedIndices);
				vectorValues = values.MakeReference(newLength: indices.Count).CreateAlike();
				var vector = new SparseVector<T, TInd>(actualRows, vectorValues, vectorIndices);
				LAS.VectorGatherValuesAt(values, vector);
				LAD.PointWiseAddScalar(vectorIndices, 1, ToInd(-rowStarts[0]));
				return vector;
			}
			catch (Exception)
			{
				vectorIndices?.Dispose(); vectorValues?.Dispose();
				throw;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void SetDiag(long k, long rows, bool compressed, Storage<T> values, Storage<TInd> sortedRow, Storage<TInd> column, SparseVector<T, TInd> vector)
		{
			long absK = Math.Abs(k), actualRows = rows - absK;
			TInd rowsInd = ToInd(rows), absKInd = ToInd(absK), actualRowsInd = ToInd(actualRows);
			long[] rowStarts = compressed ? CompressedToRowStarts(k >= 0 ? 0 : absK, actualRows, sortedRow) :
											CoordinatedToRowStarts(k >= 0 ? default : absKInd, k >= 0 ? actualRowsInd : rowsInd, rows - absK, sortedRow);
			// check indices
			long[] indices = ToManaged(vector.IndexStorage); long c = 0;
			for (long i = 0; i < actualRows; i++)
			{
				long offset = rowStarts[i], length = rowStarts[i + 1] - rowStarts[i];
				if (length <= 0)
					continue;
				long find = LAS.IndexFind(sorted: true, column.MakeReference(offset, length), k >= 0 ? ToInd(i + k) : ToInd(i));
				if (find < 0)
					continue;
				if (indices[c++] != find + rowStarts[0])
					throw new ArgumentException(Resources.Parameter.InvalidValue, nameof(vector));
			}
			if (c != indices.LongLength)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(vector));
			// set
			LAD.PointWiseAddScalar(vector.IndexStorage, 1, ToInd(rowStarts[0]));
			try
			{
				LAS.VectorSparseToDense(vector, values);
			}
			finally
			{
				LAD.PointWiseAddScalar(vector.IndexStorage, 1, ToInd(-rowStarts[0]));
			}
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

			TInd x1 = ToInd(offsetRow), x2 = ToInd(offsetRow + countRow);
			TInd y1 = ToInd(offsetCol), y2 = ToInd(offsetCol + countCol);
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
			this.CheckRange(rowStart, value.NRows, colStart, value.NCols);
			if (value is not SparseMatrix<T, TInd> sp || sp.Format.IsRowMajor() != this.Format.IsRowMajor())
				throw new ArgumentException(Resources.Parameter.InvalidValue, nameof(value));

			TInd x1 = ToInd(rowStart), x2 = ToInd(rowStart + value.NRows);
			TInd y1 = ToInd(colStart), y2 = ToInd(colStart + value.NCols);
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
			SetRange(x1, x2, y1, y2, this, sp);
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
				TInd xx = ToInd(x), yy = ToInd(y);
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
				TInd xx = ToInd(x), yy = ToInd(y);
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
		/// <returns>A new <see cref="SparseVector{T, TInd}"/> containing the <paramref name="k"/>-th diagonal elements.</returns>
		/// <exception cref="InvalidOperationException">If this matrix is not a square matrix</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="k"/> is out of range</exception>
		public override SparseVector<T, TInd> GetDiag(long k)
		{
			if (this.NRows != this.NCols)
				throw new InvalidOperationException();
			if (Math.Abs(k) >= this.NRows)
				throw new ArgumentOutOfRangeException(nameof(k), k, Resources.Parameter.InvalidValue);

			return this.Format switch
			{
				SparseMatrixFormat.COOR => GetDiag(k, this.NRows, false, this.Storage, this.RowIndexStorage, this.ColIndexStorage),
				SparseMatrixFormat.COOC => GetDiag(k, this.NRows, false, this.Storage, this.ColIndexStorage, this.RowIndexStorage),
				SparseMatrixFormat.CSR => GetDiag(k, this.NRows, true, this.Storage, this.RowIndexStorage, this.ColIndexStorage),
				SparseMatrixFormat.CSC => GetDiag(k, this.NRows, true, this.Storage, this.ColIndexStorage, this.RowIndexStorage),
				_ => throw new NotSupportedException(),
			};
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
				vec.ToDense(dense.Storage);
			}
			else if (overwrite is SparseVector<T, TInd> sparse)
			{
				if (sparse.NStored != vec.NStored)
					throw new ArgumentException(Resources.Parameter.WrongSize, nameof(overwrite));
				MEM.MemoryCopy(vec.Storage, sparse.Storage);
				MEM.MemoryCopy(vec.IndexStorage, sparse.IndexStorage);
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
		/// <exception cref="NotSupportedException">If <paramref name="value"/> is not a <see cref="SparseVector{T, TInd}"/></exception>
		public override void SetDiag(long k, VectorBase<T> value)
		{
			if (this.NRows != this.NCols)
				throw new InvalidOperationException();
			if (Math.Abs(k) >= this.NRows)
				throw new ArgumentOutOfRangeException(nameof(k), k, Resources.Parameter.InvalidValue);
			if (value is null || !value.IsValid())
				throw new ArgumentNullException(nameof(value));
			if (value is not SparseVector<T, TInd> sparse)
				throw new NotSupportedException();

			switch (this.Format)
			{
				case SparseMatrixFormat.COOR:
					SetDiag(k, this.NRows, false, this.Storage, this.RowIndexStorage, this.ColIndexStorage, sparse);
					break;
				case SparseMatrixFormat.COOC:
					SetDiag(k, this.NRows, false, this.Storage, this.ColIndexStorage, this.RowIndexStorage, sparse);
					break;
				case SparseMatrixFormat.CSR:
					SetDiag(k, this.NRows, true, this.Storage, this.RowIndexStorage, this.ColIndexStorage, sparse);
					break;
				case SparseMatrixFormat.CSC:
					SetDiag(k, this.NRows, true, this.Storage, this.ColIndexStorage, this.RowIndexStorage, sparse);
					break;
				default:
					break;
			}
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

			var wrapper = LAS.MatrixSparseFormatConvert(this, format);
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
		public override SparseMatrix<T, TInd> ToMatrix(long rows = 0)
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
			if (operation == MatrixOperation.Conjugate)
				return this.ApplyToClone(static c => LAD.PointWiseConjugate(c.Storage, 1));
			// otherwise
			var wrapper = LAS.MatrixSparseAddSparse(operation, MatrixOperation.None, Scalars<T>.One, this, default, null);
			try
			{
				var res = SparseVector<T, TInd>.CheckWrapper(this.NCols, this.NRows, this.DefaultValue, wrapper);
				if (res is not SparseMatrix<T, TInd> ss)
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
			if (other is null || !other.IsValid())
				throw new ArgumentNullException(nameof(other));
			if (scalarThis.IsZero())
				throw new ArgumentOutOfRangeException(nameof(scalarThis), scalarThis, Resources.Parameter.CannotZero);
			if (scalarOther.IsZero())
				throw new ArgumentOutOfRangeException(nameof(scalarOther), scalarOther, Resources.Parameter.CannotZero);
			var (m, n) = opThis.CanInPlace() ? (this.NRows, this.NCols) : (this.NCols, this.NRows);
			var (p, q) = opOther.CanInPlace() ? (other.NRows, other.NCols) : (other.NCols, other.NRows);
			if (m != p || n != q)
				throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(other));

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
			if (other is null || !other.IsValid())
				throw new ArgumentNullException(nameof(other));
			if (scalar.IsZero())
				throw new ArgumentOutOfRangeException(nameof(scalar), scalar, Resources.Parameter.CannotZero);
			var (m, n) = opThis.CanInPlace() ? (this.NRows, this.NCols) : (this.NCols, this.NRows);
			var (p, q) = opOther.CanInPlace() ? (other.NRows, other.NCols) : (other.NCols, other.NRows);
			if (n != p)
				throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(other));

			if (other is DenseMatrix<T> dense)
			{
				var output = Storage<T>.Create(dense.Storage[0].Location, m * q);
				try
				{
					if (dense is SymmetricDenseMatrix<T> symm)
						symm.ToNormal();
					LAS.MatrixSparseMultiplyDense(opThis, opOther, q, scalar, this, dense.Storage, dense.LeadDim, default, output, m);
					return new DenseMatrix<T>(output, m, q);
				}
				catch (Exception)
				{
					output?.Dispose();
					throw;
				}
			}
			else if (other is ISparseMatrix<T> sparse)
			{
				var wrapper = LAS.MatrixSparseMultiplySparse(opThis, opOther, scalar, this, sparse, default, null); ;
				try
				{
					T defThis = this.DefaultValue, defOther = sparse.DefaultValue;
					T defNew = (dynamic)defThis * defOther * n;
					return SparseVector<T, TInd>.CheckWrapper(m, q, defNew, wrapper);
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
		protected void CheckSparsity(SparseMatrix<T, TInd> other)
		{
			if (other is null || !other.IsValid())
				throw new ArgumentNullException(nameof(other));
			if (this.NRows != other.NRows || this.NCols != other.NCols || this.Format != other.Format || this.NStored != other.NStored)
				throw new InvalidOperationException(Resources.Parameter.NotSameSize);
			// check same indices
			if (this.RowIndexStorage == other.RowIndexStorage && this.ColIndexStorage == other.ColIndexStorage)
				return;
			if (!LAD.PointWiseEquals(this.RowIndexStorage, 1, other.RowIndexStorage, 1) ||
				!LAD.PointWiseEquals(this.ColIndexStorage, 1, other.ColIndexStorage, 1))
				throw new InvalidOperationException(Resources.Other.DifferentSparsity);
		}

		void IKrylovVector<SparseMatrix<T, TInd>, T>.Scale(T value) => this.Scale(value);

		double IKrylovVector<SparseMatrix<T, TInd>, T>.Norm() => this.Norm();

		void IKrylovVector<SparseMatrix<T, TInd>, T>.Normalize() => this.Normalize();

		T IKrylovVector<SparseMatrix<T, TInd>, T>.Dot(SparseMatrix<T, TInd> other)
		{
			this.CheckSparsity(other);
			return LAD.Dot(true, this.Storage, 1, other.Storage, 1);
		}

		/// <summary>
		/// Add the <paramref name="other"/> (scaling by <paramref name="scalar"/>) matrix to this matrix in-place.
		/// </summary>
		/// <param name="other">The other <see cref="SparseMatrix{T, TInd}"/> to add</param>
		/// <param name="scalar">The scalar to be multiplied to <paramref name="other"/> of type <typeparamref name="T"/></param>
		public virtual void AddBy(SparseMatrix<T, TInd> other, T scalar)
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
		public void ReplaceBy(SparseMatrix<T, TInd> other)
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
		/// The helper method used in <see cref="Althea.Arrays.SparseMatrix{T, TInd}.Print(PrintSettings?)"/> to get the first several row and column indices of this sparse matrix
		/// </summary>
		/// <param name="rowIndices">The output <see cref="Span{T}"/> of <see cref="long"/> used to store the row indices</param>
		/// <param name="colIndices">The output <see cref="Span{T}"/> of <see cref="long"/> used to store the column indices</param>
		protected override void GetIndices(Span<long> rowIndices, Span<long> colIndices)
		{
			int rows = rowIndices.Length, cols = colIndices.Length;
			long find;
			switch (this.Format)
			{
				case SparseMatrixFormat.COOR:
				case SparseMatrixFormat.COOC:
					ToManaged(this.RowIndexStorage.MakeReference(newLength: rows), rowIndices);
					ToManaged(this.ColIndexStorage.MakeReference(newLength: cols), colIndices);
					break;
				case SparseMatrixFormat.CSR:
					find = LAS.IndexBound(this.RowIndexStorage, ToInd(rows - 1), lowerBound: false);
					{
						using var temp = this.ColIndexStorage.MakeReference(newLength: rows).CreateAlike<long>();
						LAS.IndexGenerateFromBounds(this.RowIndexStorage.MakeReference(newLength: find), temp, lowerBound: true, start: default);
						MEM.ToManaged(temp, rowIndices);
					}
					ToManaged(this.ColIndexStorage.MakeReference(newLength: cols), colIndices);
					break;
				case SparseMatrixFormat.CSC:
					find = LAS.IndexBound(this.ColIndexStorage, ToInd(cols - 1), lowerBound: false);
					{
						using var temp = this.RowIndexStorage.MakeReference(newLength: cols).CreateAlike<long>();
						LAS.IndexGenerateFromBounds(this.ColIndexStorage.MakeReference(newLength: find), temp, lowerBound: true, start: default);
						MEM.ToManaged(temp, colIndices);
					}
					ToManaged(this.RowIndexStorage.MakeReference(newLength: rows), rowIndices);
					break;
				default:
					break;
			}
		}

		/// <summary>
		/// The helper method used by <see cref="Althea.Arrays.SparseMatrix{T, TInd}.GetStorages"/> to get the index storages' names. Only used when the sparse array contains more than one index storages.
		/// </summary>
		/// <param name="orderOfIndexStorage">The index of all index storages of this sparse matrix</param>
		/// <returns>The name the index storage indicated by the given <paramref name="orderOfIndexStorage"/></returns>
		protected override string IndexStorageNameOf(int orderOfIndexStorage) => string.Empty;
		#endregion
	}
}
