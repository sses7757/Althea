using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

using Althea.Arrays;
using Althea.Helpers;
using Althea.LinearAlgebra;
using Althea.LinearAlgebra.Sparse;
using Althea.Linq;
using Althea.NativeTypes;
using Althea.Solver;

using LAD = Althea.LinearAlgebra.Dense.AbstractApi;
using LAS = Althea.LinearAlgebra.Sparse.AbstractApi;
using MEM = Althea.Storage.AbstractApi;


namespace Althea.Backend.Arrays
{
	/// <summary>
	/// The concrete (non-blocked) sparse matrix class with the only mutable <see cref="ValueArray{T}.Storage"/> that refers to the value array storage and the <see cref="SparseMatrix{T, TInd}.RowIndexStorage"/> and <see cref="SparseMatrix{T, TInd}.ColIndexStorage"/> that refer to the <b>sorted</b> row and column index arrays' storages.
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
	/// <typeparam name="TInd">Any integer-typed unmanaged struct as the index type</typeparam>
	/// <remarks>The <see cref="SparseMatrix{T, TInd}.RowIndexStorage"/> and <see cref="SparseMatrix{T, TInd}.ColIndexStorage"/> are sorted according to <see cref="Althea.Arrays.SparseMatrix{T, TInd}.Format"/>. Any external operation that disturbs such order may result in unexpected consequences.</remarks>
	[StructLayout(LayoutKind.Explicit)]
	public class SparseMatrix<T, TInd> : Althea.Arrays.SparseMatrix<T, TInd>, IKrylovVector<SparseMatrix<T, TInd>, T>, IMultipliableMatrix<SparseMatrix<T, TInd>, SparseVector<T, TInd>, T>, IMatrix<T>
		where T : unmanaged
		where TInd : unmanaged
	{
		#region basic
		[FieldOffset(0)]
		private Storage<TInd> m_originalRowIndex;
		[FieldOffset(8)]
		private readonly Storage<TInd> m_originalColIndex;

		[FieldOffset(16)]
		private Storage<TInd> m_rowIndex;
		[FieldOffset(24)]
		private readonly Storage<TInd> m_colIndex;

		/// <summary>
		/// Get the storage of the row index array of this sparse matrix as a <see cref="Storage{T}"/> of <typeparamref name="TInd"/>
		/// </summary>
		public Storage<TInd> RowIndexStorage {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.m_rowIndex;
		}

		/// <summary>
		/// Get the storage of the column index array of this sparse matrix as a <see cref="Storage{T}"/> of <typeparamref name="TInd"/>
		/// </summary>
		public Storage<TInd> ColIndexStorage {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.ColIndexStorage;
		}

		/// <summary>
		/// Get all the index arrays as a <see cref="ReadOnlySpan{T}"/> of <see cref="Storage{T}"/> of <typeparamref name="TInd"/>
		/// </summary>
		public override ReadOnlySpan<Storage<TInd>> IndexArrays {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => MemoryMarshal.CreateReadOnlySpan(ref this.m_rowIndex, 2);
		}

		/// <summary>
		/// Get the original index arrays' storages of this sparse matrix.
		/// </summary>
		protected override ReadOnlySpan<IStorage> OriginalIndexStorages {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<Storage<TInd>, IStorage>(ref this.m_originalRowIndex), 2);
		}

		/// <summary>
		/// Create an empty <see cref="SparseMatrix{T, TInd}"/>
		/// </summary>
		public SparseMatrix() : base(0, 0, Storage<T>.Empty, SparseMatrixFormat.COOR)
		{
			this.m_rowIndex = this.m_colIndex = this.m_originalRowIndex = this.m_originalColIndex = Storage<TInd>.Empty;
		}

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
		public SparseMatrix(long rows, long cols, Storage<T> valueArray, Storage<TInd> rowIndexArray, Storage<TInd> colIndexArray, SparseMatrixFormat format, T defaultValue = default, long stores = 0) : base(rows, cols, valueArray, format, defaultValue, stores)
		{
			long rowLen = GetRowLength(rows, valueArray, stores, format);
			long colLen = GetColLength(cols, valueArray, stores, format);
			this.m_rowIndex = this.m_originalRowIndex = rowIndexArray;
			this.m_colIndex = this.m_originalColIndex = colIndexArray;
			var span = this.IndexArrays;
			var outSpan = MemoryMarshal.CreateSpan(ref this.m_rowIndex, 2);
			ISparseArray<T, TInd>.CheckIndexArrays(span, stackalloc long[] { rowLen, colLen }, outSpan);
		}

		private SparseMatrix(SparseMatrix<T, TInd> r) : this(r.NRows, r.NCols, r.Storage, r.RowIndexStorage, r.ColIndexStorage, r.Format, r.DefaultValue, r.NStored) { }

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
				_ => throw new ArgumentOutOfRangeException(nameof(format), format, Resources.Parameter.InvalidValue),
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
			var outIndex = new FixedClassBuffer_2<ActualStorage<TInd>>();
			var value = ((ISparseArray<T, TInd>)this).CreateArraysAlike<T, TInd>(outIndex.AsSpan(), copyValues: false);
			return new SparseMatrix<T, TInd>(this.NRows, this.NCols, value, outIndex[0], outIndex[1], this.Format, this.DefaultValue);
		}

		/// <summary>
		/// Create a new sparse matrix with same properties as this one while the underlying storages are not filled.
		/// </summary>
		/// <returns>The new sparse matrix alike this one</returns>
		public override SparseMatrix<T, TInd> NewArrayAlike() => (SparseMatrix<T, TInd>)base.NewArrayAlike();

		/// <summary>
		/// Create a new sparse matrix with same properties as this one while the underlying storages are not filled and the data type is changed to <typeparamref name="TOut"/> while index type changed to <typeparamref name="TIndOut"/>.
		/// </summary>
		/// <typeparam name="TOut">Any unmanaged struct as the new data type</typeparam>
		/// <typeparam name="TIndOut">Any integral-typed unmanaged struct as the new index type</typeparam>
		/// <returns>The new sparse matrix alike this one</returns>
		/// <exception cref="TypeMismatchException">If the <typeparamref name="TIndOut"/> is not an integral type</exception>
		public override SparseMatrix<TOut, TIndOut> NewArrayAlike<TOut, TIndOut>()
		{
			var outIndex = new FixedClassBuffer_2<ActualStorage<TIndOut>>();
			var value = ((ISparseArray<T, TInd>)this).CreateArraysAlike<TOut, TIndOut>(outIndex.AsSpan(), copyValues: false);
			return new SparseMatrix<TOut, TIndOut>(this.NRows, this.NCols, value, outIndex[0], outIndex[1], this.Format, this.DefaultValue.GenericConvert<T, TOut>());
		}
		#endregion

		#region indexer helpers
		#region common
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static long ToLong(TInd i) => i.GenericConvert<TInd, long>();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static TInd ToInd(long i) => i.GenericConvert<long, TInd>();

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
		internal static void ToManaged(Storage<TInd> storage, Span<long> managed)
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
		private static void RefColumValue(long[] rowStarts, ref Storage<TInd> column, ref Storage<T> values, TInd y1, long pack)
		{
			long offset = rowStarts[0], length = rowStarts[^1] - rowStarts[0];
			values = values.MakeReference(offset * pack, length * pack);
			column = column.MakeReference(offset, length);
			if (!y1.IsZero())
			{
				column = column.ApplyToClone(c => LAD.PointWiseAddScalar(c, 1, y1.GenericNegate()));
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void GetRange(long rows, TInd y1, TInd y2, long[] rowStarts, ref Storage<TInd> column, ref Storage<T> values, long pack)
		{
			long length = 0;
			bool allColumns = true;
			Storage<T>[] valueArray = new Storage<T>[rows];
			Storage<TInd>[] indexArray = new Storage<TInd>[rows];
			for (long i = 0; i < rows; i++)
			{
				long off = rowStarts[i], len = rowStarts[i + 1] - off;
				Storage<T> value = values.MakeReference(off * pack, len * pack);
				Storage<TInd> index = column.MakeReference(off, len);
				SparseVector<T, TInd>.GetSlice(y1, y2, ref value, ref index, pack);
				if (allColumns && index.Length != len)
					allColumns = false;
				valueArray[i] = value; indexArray[i] = index;
				length += index.Length;
				rowStarts[i + 1] = off + index.Length;
			}
			if (allColumns)
			{
				RefColumValue(rowStarts, ref column, ref values, y1, pack);
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
		internal static long[] CoordinatedToRowStarts(TInd x1, TInd x2, long rows, Storage<TInd> sortedRow)
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
		internal static long[] CompressedToRowStarts(long start, long count, Storage<TInd> sortedRow)
		{
			return ToManaged(sortedRow.MakeReference(start, count));
		}
		#endregion

		#region element indexing
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static long IndexCoordinated(long x, long y, Storage<TInd> sorted, Storage<TInd> other, long max)
		{
			TInd xx = ToInd(x), yy = ToInd(y);
			long find = 0;
			while (find >= 0)
			{
				long newFind = LAS.IndexFind(sorted: true, sorted + find, xx);
				if (newFind < 0)
					break;
				find += newFind;
				if (MEM.ToManaged(other + find).IsEqual(yy))
				{
					return find;
				}
				if (find >= max)
					break;
			}
			// not stored
			return -1;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static long IndexCompressed(long x, long y, Storage<TInd> sorted, Storage<TInd> other)
		{
			TInd yy = ToInd(y);
			long start = ToLong(MEM.ToManaged(sorted + x));
			long count = ToLong(MEM.ToManaged(sorted + (x + 1))) - start;
			if (count > 0)
			{
				long find = LAS.IndexFind(sorted: true, other.MakeReference(start, count), yy);
				if (find >= 0)
				{
					return find + start;
				}
			}
			// not stored
			return -1;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private T ElementIndex(long x, long y, Storage<TInd> sorted, Storage<TInd> other, T value, bool compressed, bool get)
		{
			long find = compressed ? IndexCompressed(x, y, sorted, other) : IndexCoordinated(x, y, sorted, other, this.ActualLength);
			if (find >= 0)
			{
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
			var slice = MatrixSliceWrapper.Create<T>(offsetRow, countRow, offsetCol, countCol, this);
			var wrapper = LAS.SparseMatrixSlice(this, slice);
			try
			{
				return (SparseMatrix<T, TInd>)wrapper.CheckWrapper<T, TInd>(countRow, countCol);
			}
			catch (Exception)
			{
				wrapper.Dispose();
				throw;
			}
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
			if (overwrite is null || !overwrite.IsValid())
				throw new ArgumentNullException(nameof(overwrite));
			var slice = MatrixSliceWrapper.Create<T>(offsetRow, countRow, offsetCol, countCol, this, overwrite);

			if (overwrite is DenseMatrix<T> dense)
			{
				LAS.SparseMatrixSlice(this, slice, dense.Storage, dense.LeadDim);
			}
			else if (overwrite is SparseMatrix<T, TInd> sparse)
			{
				LAS.SparseMatrixSlice(this, slice, sparse);
			}
			else
				throw new ArgumentException(Resources.Parameter.UnexpectedType, nameof(overwrite));
		}

		/// <summary>
		/// Set a sub-matrix by the row and column index ranges with the given <paramref name="value"/>.
		/// </summary>
		/// <param name="offsetRow">The starting offset of the row to take</param>
		/// <param name="countRow">The number of the rows to take</param>
		/// <param name="offsetCol">The starting offset of the columns to take</param>
		/// <param name="countCol">The number of the columns to take</param>
		/// <param name="value">The <see cref="MatrixBase{T}"/> whose value will overwrite this matrix from (<paramref name="offsetRow"/>, <paramref name="countRow"/>) with size (<paramref name="countRow"/>, <paramref name="countCol"/>)</param>
		/// <exception cref="ArgumentNullException">If <paramref name="value"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offsetRow"/> or <paramref name="countRow"/> or <paramref name="offsetCol"/> or <paramref name="countCol"/> is out of range</exception>
		/// <exception cref="NotSupportedException">If <paramref name="value"/> is neither a <see cref="SparseMatrix{T, TInd}"/> of compatible format</exception>
		public override void SetSubmatrix(long offsetRow, long countRow, long offsetCol, long countCol, MatrixBase<T> value)
		{
			if (value is null || !value.IsValid())
				throw new ArgumentNullException(nameof(value));
			if (value is not SparseMatrix<T, TInd> sparse)
				throw new NotSupportedException(Resources.Parameter.UnexpectedType);
			var slice = MatrixSliceWrapper.Create<T>(offsetRow, countRow, offsetCol, countCol, this, value);

			try
			{
				LAS.SparseMatrixSetSlice(this, slice, sparse);
			}
			catch (Exception e)
			{
				throw new NotSupportedException(Resources.Parameter.UnexpectedType, e);
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
		/// <returns>A new <see cref="SparseVector{T, TInd}"/> containing the <paramref name="k"/>-th diagonal elements.</returns>
		/// <exception cref="InvalidOperationException">If this matrix is not a square matrix</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="k"/> is out of range</exception>
		public override SparseVector<T, TInd> GetDiag(long k)
		{
			if (this.NRows != this.NCols)
				throw new InvalidOperationException();
			if (Math.Abs(k) >= this.NRows)
				throw new ArgumentOutOfRangeException(nameof(k), k, Resources.Parameter.InvalidValue);

			// get row starts
			bool compressed = this.Format.IsCompressed();
			var (sortedRow, column) = (this.RowIndexStorage, this.ColIndexStorage);
			if (this.Format.IsRowMajor())
			{
				k = -k;
				(sortedRow, column) = (column, sortedRow);
			}
			long absK = Math.Abs(k), actualRows = this.NRows - absK;
			TInd rowsInd = ToInd(this.NRows), absKInd = ToInd(absK), actualRowsInd = ToInd(actualRows);
			long[] rowStarts = compressed ? CompressedToRowStarts(k >= 0 ? 0 : absK, actualRows, sortedRow) :
											CoordinatedToRowStarts(k >= 0 ? default : absKInd, k >= 0 ? actualRowsInd : rowsInd, this.NRows - absK, sortedRow);
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
				vectorValues = this.Storage.MakeReference(newLength: indices.Count).CreateAlike();
				var vector = new SparseVector<T, TInd>(actualRows, vectorValues, vectorIndices, this.DefaultValue);
				LAS.VectorGatherValuesAt(this.Storage, vector);
				LAD.PointWiseAddScalar(vectorIndices, 1, ToInd(-rowStarts[0]));
				return vector;
			}
			catch (Exception)
			{
				vectorIndices?.Dispose(); vectorValues?.Dispose();
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
				sparse.DefaultValue = this.DefaultValue;
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

			// get row starts
			bool compressed = this.Format.IsCompressed();
			var (sortedRow, column) = (this.RowIndexStorage, this.ColIndexStorage);
			if (this.Format.IsRowMajor())
			{
				k = -k;
				(sortedRow, column) = (column, sortedRow);
			}
			long absK = Math.Abs(k), actualRows = this.NRows - absK;
			TInd rowsInd = ToInd(this.NRows), absKInd = ToInd(absK), actualRowsInd = ToInd(actualRows);
			long[] rowStarts = compressed ? CompressedToRowStarts(k >= 0 ? 0 : absK, actualRows, sortedRow) :
											CoordinatedToRowStarts(k >= 0 ? default : absKInd, k >= 0 ? actualRowsInd : rowsInd, this.NRows - absK, sortedRow);
			// check indices
			long[] indices = ToManaged(sparse.IndexStorage); long c = 0;
			for (long i = 0; i < actualRows; i++)
			{
				long offset = rowStarts[i], length = rowStarts[i + 1] - rowStarts[i];
				if (length <= 0)
					continue;
				long find = LAS.IndexFind(sorted: true, column.MakeReference(offset, length), k >= 0 ? ToInd(i + k) : ToInd(i));
				if (find < 0)
					continue;
				if (indices[c++] != find + rowStarts[0])
					throw new ArgumentException(Resources.Parameter.InvalidValue, nameof(value));
			}
			if (c != indices.LongLength)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(value));
			// set
			LAD.PointWiseAddScalar(sparse.IndexStorage, 1, ToInd(rowStarts[0]));
			try
			{
				LAS.VectorSparseToDense(sparse, this.Storage);
			}
			finally
			{
				LAD.PointWiseAddScalar(sparse.IndexStorage, 1, ToInd(-rowStarts[0]));
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
		/// <param name="otherInfo">The target sparse matrix's <see cref="IOtherInfo"/>, default null means letting the internal implementation determine</param>
		/// <returns>The converted <see cref="Althea.Arrays.SparseMatrix{T, TInd}"/> whose <see cref="Althea.Arrays.SparseMatrix{T, TInd}.Format"/> fits the given <paramref name="format"/>, or this one if no conversion is necessary</returns>
		/// <exception cref="NotSupportedException">If <paramref name="format"/> is not composed of internally defined values or <paramref name="otherInfo"/> is neither null nor <see cref="BlockedSparseMatrixOtherInfo"/></exception>
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
				return wrapper.CheckWrapper<T, TInd>(this.NRows, this.NCols);
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
				return wrapper.CheckWrapper<T, TInd>(this.NRows * this.NCols);
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
			Span<long> size = stackalloc long[] { rows, 0 };
			CheckSize(this, size);
			if (size[0] == this.NRows)
				return this;
			var wrapper = LAS.MatrixSparseReshape(this, size[0]);
			try
			{
				var matrix = wrapper.CheckWrapper<T, TInd>(size[0], size[1]);
				if (matrix is not SparseMatrix<T, TInd> s)
					throw new InvalidOperationException();
				return s;
			}
			catch (Exception)
			{
				wrapper.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Reshape the array to a tensor with dimensionality = <paramref name="size"/>.
		/// </summary>
		/// <param name="size">The new size/dimensionality with at most one or zero uncertain dimension indicated by a non-positive number.</param>
		/// <returns>The reshaped tensor</returns>
		public override SparseTensor<T, TInd> ToTensor(ReadOnlySpan<long> size)
		{
			Span<long> newSize = stackalloc long[size.Length];
			size.CopyTo(newSize);
			CheckSize(this, newSize);
			// to vector
			var wrapper = LAS.SparseMatrixToVector(this, SparseVectorFormat.Coordinated);
			try
			{
				wrapper.CheckWrapper<T, TInd>(this.NRows * this.NCols);
				return new(newSize, wrapper.ValueStorage, (Storage<TInd>)wrapper.IndexStorages[0], defaultValue: this.DefaultValue);
			}
			catch (Exception)
			{
				wrapper.Dispose();
				throw;
			}
		}
		#endregion

		#region linear algebra
		/// <summary>
		/// Create a new <see cref="SparseMatrix{T, TInd}"/> which is the simple operation result of this matrix under <paramref name="operation"/>.
		/// </summary>
		/// <param name="operation">The input <see cref="MatrixOperation"/> used as the simple operation to be applied</param>
		/// <returns>A new <see cref="SparseMatrix{T, TInd}"/> as the result of <paramref name="operation"/>(this)</returns>
		/// <exception cref="NotSupportedException">If the given <paramref name="operation"/> is not supported</exception>
		public override SparseMatrix<T, TInd> ApplyOperation(MatrixOperation operation)
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
				var res = wrapper.CheckWrapper<T, TInd>(this.NCols, this.NRows);
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
			var (m, n) = ((IMatrix<T>)this).CheckAdd(scalarThis, scalarOther, other, ref opThis, ref opOther);
			if (other is IDenseMatrix<T> dense)
			{
				if (other is SymmetricDenseMatrix<T> symm)
					symm.ToNormal();
				var clone = other.Storage.CreateAlike();
				try
				{
					LAS.MatrixDenseAddSparse(opOther, opThis, scalarOther, other.Storage, dense.LeadDim, scalarThis, this, clone, m);
					return new DenseMatrix<T>(clone, m, n);
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
					return wrapper.CheckWrapper<T, TInd>(m, n);
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
			var (m, n, _) = ((IMatrix<T>)this).CheckMultiply(scalar, other, ref opThis, ref opOther);
			if (other is IDenseMatrix<T> dense)
			{
				var output = Storage<T>.Create(other.Storage[0].Location, m * n);
				try
				{
					if (dense is SymmetricDenseMatrix<T> symm)
						symm.ToNormal();
					LAS.MatrixSparseMultiplyDense(opThis, opOther, n, scalar, this, other.Storage, dense.LeadDim, default, output, m);
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
					return wrapper.CheckWrapper<T, TInd>(m, n);
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
		SparseMatrix<T, TInd> IKrylovVector<SparseMatrix<T, TInd>, T>.NewArrayAlike()
		{
			var values = this.Storage.Clone();
			try
			{
				return new(this.NRows, this.NCols, values, this.RowIndexStorage, this.ColIndexStorage, this.Format, this.DefaultValue);
			}
			catch (Exception)
			{
				values?.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Check the sparsity of this sparse matrix and the <paramref name="other"/> one
		/// </summary>
		/// <param name="other">The other sparse matrix to check sparsity</param>
		/// <exception cref="ArgumentNullException">If <paramref name="other"/> is null or invalid</exception>
		/// <exception cref="InvalidOperationException">If the <paramref name="other"/> matrix has different sparsity from this one</exception>
		public void CheckSparsity(SparseMatrix<T, TInd> other)
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
		public void AddBy(SparseMatrix<T, TInd> other, T scalar)
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

		#region IMultipliableMatrix
		bool IMultipliableMatrix<SparseMatrix<T, TInd>, SparseVector<T, TInd>, T>.CanOperateInPlace => false;

		void IMultipliableMatrix<SparseMatrix<T, TInd>, SparseVector<T, TInd>, T>.InPlaceFusedMultiplyAdd(SparseMatrix<T, TInd> left, SparseMatrix<T, TInd> right, T scalar, T scalarThis, MatrixOperation opLeft, MatrixOperation opRight) => throw new NotImplementedException();

		SparseMatrix<T, TInd> IMultipliableMatrix<SparseMatrix<T, TInd>, SparseVector<T, TInd>, T>.OutOfPlaceFusedMultiplyAdd(SparseMatrix<T, TInd> left, SparseMatrix<T, TInd> right, T scalar, T scalarThis, MatrixOperation opLeft, MatrixOperation opRight)
		{
			var (m, n, _) = ((IMatrix<T>)left).CheckMultiply(scalar, right, ref opLeft, ref opRight);
			if (!scalarThis.IsZero() && (this.NRows != m || this.NCols != n))
				throw new ArgumentException(Resources.Parameter.WrongSize);
			var wrapper = LAS.MatrixSparseMultiplySparse(opLeft, opRight, scalar, left, right, scalarThis, this, FormatExtension.NonBlocked);
			try
			{
				return (SparseMatrix<T, TInd>)wrapper.CheckWrapper<T, TInd>(m, n);
			}
			catch (Exception)
			{
				wrapper.Dispose();
				throw;
			}
		}
		#endregion

		#region print
		private void GetIndices(Span<long> rowIndices, Span<long> colIndices)
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

			string detail = ":" + Environment.NewLine;
			// get managed arrays
			int length = (int)Math.Min(settings.ArrayLength, this.NStored);
			Span<T> values = length.CheckStackLimit<T>() ?? stackalloc T[length];
			MEM.ToManaged(this.Storage, values);
			Span<long> row = length.CheckStackLimit<long>() ?? stackalloc long[length];
			Span<long> col = length.CheckStackLimit<long>() ?? stackalloc long[length];
			this.GetIndices(row, col);
			// to matrix string
			detail += values.ToSparseMatrixString(row, col, precision: settings.Precision);
			if (this.NStored > values.Length)
				detail += Environment.NewLine + string.Format(Resources.Print.MoreStored, this.NStored - values.Length);
			return description + detail;
		}
		#endregion

		#region serialization
		/// <summary>
		/// The presenting name of <see cref="RowIndexStorage"/>
		/// </summary>
		protected internal const string RowIndexStorageName = nameof(RowIndexStorage);

		/// <summary>
		/// The presenting name of <see cref="ColIndexStorage"/>
		/// </summary>
		protected internal const string ColIndexStorageName =nameof(ColIndexStorage);

		/// <summary>
		/// Get all the storages of this array. Only returns <see cref="ValueArray{T}.Storage"/>, <see cref="RowIndexStorage"/> and <see cref="ColIndexStorage"/>.
		/// </summary>
		/// <returns>All the storages of the array as an <see cref="IReadOnlyDictionary{TKey, TValue}"/> of <see cref="string"/> and <see cref="IStorage"/></returns>
		public override IReadOnlyDictionary<string, IStorage> GetStorages() => new Dictionary<string, IStorage>(2)
		{
			[StorageName] = this.Storage,
			[RowIndexStorageName] = this.m_rowIndex,
			[ColIndexStorageName] = this.m_colIndex,
		};
		#endregion
	}
}
