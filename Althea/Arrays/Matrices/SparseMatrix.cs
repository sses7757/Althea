using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Althea.Linq;
using Althea.Memory;
using RT = Althea.Runtime.API;
using BLAS = Althea.Blas.API;
using Sparse = Althea.SparseBlas.API;


namespace Althea.Arrays
{
	/// <summary>
	/// The sparse matrix class that inherit the <see cref="MatrixBase{T}"/> and implements <see cref="ISparseArray{T}"/>.
	/// </summary>
	/// <typeparam name="T">the supported data types are <see cref="float"/>, <see cref="double"/>, <see cref="FloatComplex"/>, <see cref="DoubleComplex"/>; other types of data causes <see cref="NotSupportedException"/></typeparam>
	/// <remarks>For compatibility issues, only general sparse matrix is supported</remarks>
	public sealed class SparseMatrix<T> : MatrixBase<T>, ISparseArray<T>, IMatrix<SparseMatrix<T>, SparseVector<T>, T>, IMatrix<DenseMatrix<T>, DenseVector<T>, T> where T : struct, IComparable<T>
	{
		#region sparse matrix special
		/// <summary>
		/// The pointer to the row index array (array of <see cref="int"/>) of the sparse matrix, read-only
		/// </summary>
		internal Storage<int> RowPointer { get; set; }

		/// <summary>
		/// The length of row index array
		/// </summary>
		public long RowIndexLength => this.RowPointer.Length;////this.Format == SparseMatrixFormat.CSR ? this.NRows + 1 : this.NonZero;

		private int IntRowIdxLength => checked((int)this.RowIndexLength);

		/// <summary>
		/// The pointer to the column index array (array of <see cref="int"/>) of the sparse matrix, read-only
		/// </summary>
		internal Storage<int> ColumnPointer { get; set; }

		/// <summary>
		/// The length of column index array
		/// </summary>
		public long ColumnIndexLength => this.ColumnPointer.Length;////this.Format == SparseMatrixFormat.CSC ? this.NCols + 1 : this.NonZero;

		private int IntColIdxLength => checked((int)this.ColumnIndexLength);

		/// <summary>
		/// Number of nonzero values of this sparse vector, equal to the array size <see cref="ValueArray{T}.Pointer"/>, from <see cref="ISparseArray{T}.NonZero"/>.
		/// </summary>
		public long NonZero => this.ActualLength;

		internal int IntNNZ => checked((int)this.NonZero);

		/// <summary>
		/// The <see cref="SparseMatrixFormat"/> of this sparse matrix
		/// </summary>
		public SparseMatrixFormat Format { get; internal set; }
		#endregion


		#region initialize and destroy
		private static (Storage<int> row, Storage<int> col) AllocateIndex(long leadDim, long secondDim, long nnz, SparseMatrixFormat format, SparseMatrix<T> mat, bool onHost, bool allocRow = true, bool allocCol = true)
		{
			try
			{
				switch (format)
				{
					case SparseMatrixFormat.COOR:
					case SparseMatrixFormat.COOC:
						return (allocRow ? Storage<int>.Create(nnz, onHost) : null, allocCol ? Storage<int>.Create(nnz, onHost) : null);
					case SparseMatrixFormat.CSR:
						return (allocRow ? Storage<int>.Create(leadDim + 1, onHost) : null, allocCol ? Storage<int>.Create(nnz, onHost) : null);
					case SparseMatrixFormat.CSC:
						return (allocRow ? Storage<int>.Create(nnz, onHost) : null, allocCol ? Storage<int>.Create(secondDim + 1, onHost) : null);
					default:
						throw new ArgumentOutOfRangeException(nameof(format), format, Resource.FormatNotAtomic);
				}
			}
			catch
			{
				mat.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Empty constructor
		/// </summary>
		public SparseMatrix() : this(0, 0, 0, default, onHost: false) { }

		/// <summary>
		/// Matrix constructor with all pointers allocated inside.
		/// </summary>
		/// <param name="rows">leading dimension, must be equal to the number of rows</param>
		/// <param name="cols">secondary dimension, must be equal to the number of columns</param>
		/// <param name="nonZeros">the actual length of the stored data, i.e. the number of non-zero values</param>
		/// <param name="format">the <see cref="SparseMatrixFormat"/> of this new sparse matrix, must be an atomic one</param>
		/// <param name="herm">the new matrix is Hermitian or not</param>
		/// <param name="onHost">allocate on host memory or device memory</param>
		/// <exception cref="ArgumentException">if <c>3 * <paramref name="nonZeros"/> ≥ <paramref name="rows"/> * <paramref name="cols"/></c> and <paramref name="rows"/> * <paramref name="cols"/> &gt; <see cref="GlobalSettings.SparseMatrixUncheck"/></exception>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="format"/> is not atomic</exception>
		public SparseMatrix(long rows, long cols, long nonZeros, SparseMatrixFormat format, bool onHost = false, bool herm = false) : base(nonZeros, rows, cols, onHost, herm)
		{
			if (rows == 0 || cols == 0 || nonZeros == 0)
				return;
			if (!format.IsAtomic())
			{
				this.Pointer.Dispose();
				throw new ArgumentOutOfRangeException(nameof(format), format, Resource.FormatNotAtomic);
			}
			this.Format = format;
			(this.RowPointer, this.ColumnPointer) = AllocateIndex(rows, cols, nonZeros, format, this, onHost);
			if (this.RowIndexLength + this.ColumnIndexLength + this.NonZero >= rows * cols && rows * cols > GlobalSettings.SparseMatrixUncheck)
			{
				Log.Write(Resource.SpMatTooDense, category: "SparseMatrix Creator", level: LogLevel.Warning);
			}
		}

		/// <summary>
		/// Matrix full constructor with all pointers pre-allocated.
		/// </summary>
		/// <param name="rows">leading dimension, must be equal to the number of rows</param>
		/// <param name="cols">secondary dimension, must be equal to the number of columns</param>
		/// <param name="value">the value array pointer</param>
		/// <param name="rowPtr">a <see cref="Storage{Int32}"/> to indicate the row index array</param>
		/// <param name="colPtr">a <see cref="Storage{Int32}"/> to indicate the column index array</param>
		/// <param name="format">the <see cref="SparseMatrixFormat"/> of this new sparse matrix</param>
		/// <param name="herm">the new matrix is Hermitian or not</param>
		/// <param name="refVal"><paramref name="value"/> is a reference or not</param>
		/// <param name="refCol"><paramref name="colPtr"/> a reference or not</param>
		/// <param name="refRow"><paramref name="rowPtr"/> a reference or not</param>
		/// <exception cref="ArgumentException">if <c>3 * <see cref="NonZero"/> ≥ <paramref name="rows"/> * <paramref name="cols"/></c> and <paramref name="rows"/> * <paramref name="cols"/> &gt; <see cref="GlobalSettings.SparseMatrixUncheck"/></exception>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="format"/> is not atomic</exception>
		public SparseMatrix(long rows, long cols, Storage<T> value, Storage<int> rowPtr, Storage<int> colPtr, SparseMatrixFormat format, bool herm = false, bool refVal = false, bool refRow = false, bool refCol = false) : base(refVal ? value + 0 : value, rows, cols, herm: herm)
		{
			if (!format.IsAtomic())
			{
				this.Pointer.Dispose();
				throw new ArgumentOutOfRangeException(nameof(format), format, Resource.FormatNotAtomic);
			}
			if (colPtr is null)
			{
				this.Pointer.Dispose();
				throw new ArgumentNullException(nameof(colPtr));
			}
			if (rowPtr is null)
			{
				this.Pointer.Dispose();
				throw new ArgumentNullException(nameof(rowPtr));
			}
			if (value is null)
			{
				this.Pointer.Dispose();
				throw new ArgumentNullException(nameof(value));
			}
			if (colPtr.OnHost != rowPtr.OnHost || colPtr.OnHost != value.OnHost)
			{
				this.Pointer.Dispose();
				throw new ArgumentException(Resource.RequireSamePos);
			}
			this.Format = format;
			this.RowPointer = refRow ? rowPtr + 0 : rowPtr; this.ColumnPointer = refCol ? colPtr + 0 : colPtr;
			if (this.RowIndexLength + this.ColumnIndexLength + this.NonZero >= rows * cols && rows * cols > GlobalSettings.SparseMatrixUncheck)
			{
				Log.Write(Resource.SpMatTooDense, category: "SparseMatrix Creator", level: LogLevel.Warning);
			}
		}

		/// <summary>
		/// Matrix full reference constructor with all pointers pre-allocated.
		/// </summary>
		/// <param name="rows">leading dimension, must be equal to the number of rows</param>
		/// <param name="cols">secondary dimension, must be equal to the number of columns</param>
		/// <param name="refArray">the reference array</param>
		/// <param name="rowPtr">a <see cref="Storage{Int32}"/> to indicate the row index array</param>
		/// <param name="colPtr">a <see cref="Storage{Int32}"/> to indicate the column index array</param>
		/// <param name="format">the <see cref="SparseMatrixFormat"/> of this new sparse matrix</param>
		/// <param name="herm">the new matrix is Hermitian or not</param>
		/// <param name="offsetRef">offset to the <see cref="ValueArray{T}.Pointer"/> of <paramref name="refArray"/></param>
		/// <param name="refCol"><paramref name="colPtr"/> a reference or not</param>
		/// <param name="refRow"><paramref name="rowPtr"/> a reference or not</param>
		/// <exception cref="ArgumentException">if <c>3 * <see cref="NonZero"/> ≥ <paramref name="rows"/> * <paramref name="cols"/></c> and <paramref name="rows"/> * <paramref name="cols"/> &gt; <see cref="GlobalSettings.SparseMatrixUncheck"/></exception>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="format"/> is not atomic</exception>
		public SparseMatrix(ValueArray<T> refArray, long rows, long cols, Storage<int> rowPtr, Storage<int> colPtr, SparseMatrixFormat format, bool herm = false, long offsetRef = 0, bool refRow = false, bool refCol = false) : base(refArray, refArray.ActualLength, rows, cols, herm, offsetRef)
		{
			if (!format.IsAtomic())
			{
				this.Pointer.Dispose();
				throw new ArgumentOutOfRangeException(nameof(format), format, Resource.FormatNotAtomic);
			}
			if (colPtr is null)
			{
				this.Pointer.Dispose();
				throw new ArgumentNullException(nameof(colPtr));
			}
			if (rowPtr is null)
			{
				this.Pointer.Dispose();
				throw new ArgumentNullException(nameof(rowPtr));
			}
			if (colPtr.OnHost != rowPtr.OnHost || colPtr.OnHost != refArray.OnHost)
			{
				this.Pointer.Dispose();
				throw new ArgumentException(Resource.RequireSamePos);
			}
			this.Format = format;
			this.RowPointer = refRow ? rowPtr + 0 : rowPtr; this.ColumnPointer = refCol ? colPtr + 0 : colPtr;
			if (this.RowIndexLength + this.ColumnIndexLength + this.NonZero >= rows * cols && rows * cols > GlobalSettings.SparseMatrixUncheck)
			{
				Log.Write(Resource.SpMatTooDense, category: "SparseMatrix Creator", level: LogLevel.Warning);
			}
		}

		/// <summary>
		/// Sparse matrix copy constructor.
		/// </summary>
		/// <param name="M">the <see cref="SparseMatrix{T}"/> to copy</param>
		/// <param name="copyIndex">copy sparse pattern indices or not</param>
		public SparseMatrix(SparseMatrix<T> M, bool copyIndex) : base(M != null ? M.NonZero : throw new ArgumentNullException(nameof(M), Resource.ArrayCannotNull), M.NRows, M.NCols, onHost: M.OnHost, herm: M.Hermitian)
		{
			this.Format = M.Format;
			try
			{
				RT.CopyTo(source: M, dest: this, length: this.NonZero);
			}
			catch (Exception)
			{
				this.Dispose();
				throw;
			}
			if (copyIndex)
			{
				(this.RowPointer, this.ColumnPointer) = AllocateIndex(M.NRows, M.NCols, M.NonZero, M.Format, this, M.OnHost);
				try
				{
					RT.CopyTo(source: M.RowPointer, dest: this.RowPointer, length: this.RowIndexLength);
					RT.CopyTo(source: M.ColumnPointer, dest: this.ColumnPointer, length: this.ColumnIndexLength);
				}
				catch (Exception)
				{
					this.Dispose();
					throw;
				}
			}
			else
			{
				this.RowPointer = M.RowPointer + 0;
				this.ColumnPointer = M.ColumnPointer + 0;
			}
		}

		/// <summary>
		/// Reshape constructor
		/// </summary>
		/// <param name="refArray">the reference array, <see cref="NonZero"/> is obtained from its pointer's length</param>
		/// <param name="rows">number of rows</param>
		/// <param name="cols">number of columns</param>
		/// <param name="format"><see cref="SparseMatrixFormat"/> of this sparse matrix</param>
		/// <param name="herm">Hermitian or not</param>
		/// <param name="offset">offset to the pointer of <paramref name="refArray"/></param>
		public SparseMatrix(ValueArray<T> refArray, long rows, long cols, SparseMatrixFormat format, bool herm = false, long offset = 0) : base(refArray, refArray.ActualLength, rows, cols, herm, offset)
		{
			this.Format = format;
			(this.RowPointer, this.ColumnPointer) = AllocateIndex(rows, cols, this.NonZero, format, this, this.OnHost);
			if (this.RowIndexLength + this.ColumnIndexLength + this.NonZero >= rows * cols && rows * cols > GlobalSettings.SparseMatrixUncheck)
			{
				Log.Write(Resource.SpMatTooDense, category: "SparseMatrix Creator", level: LogLevel.Warning);
			}
		}

		/// <summary>
		/// The function that actually implements the dispose functionality, override <see cref="ValueArray{T}.Dispose(bool)"/>.
		/// </summary>
		/// <exception cref="AccessViolationException">if the memory free failed</exception>
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing); // dispose value array
			this.vecIndex?.Dispose();
			if (this.Disposed || this.Length == 0 || this.Pointer is null || !(this._root is null))
				return;
			this.RowPointer.Dispose();
			this.ColumnPointer.Dispose();
		}
		#endregion


		#region reshape
		/// <summary>
		/// Flatten the matrix to a <see cref="VectorBase{T}"/>. Override <see cref="ValueArray{T}.ToVector"/>.
		/// </summary>
		/// <returns>The flattened vector</returns>
		public override ValueArray<T> ToVector()
		{
			if (this.Format == SparseMatrixFormat.COOC)
			{
				var vec = new SparseVector<T>(this, this.Length, this.NonZero);
				try
				{
					Sparse.VectorToFromCOOMatrix(vec, this, toCOO: false);
					return vec;
				}
				catch (Exception)
				{
					vec.Dispose();
					throw;
				}
			}
			else
			{
				using var matCOO = this.ToFormat(SparseMatrixFormat.COOC);
				return matCOO.ToVector();
			}
		}

		/// <summary>
		/// Reshape the matrix to a (new) <see cref="MatrixBase{T}"/> with leading dimension = leadDim. Override <see cref="ValueArray{T}.ToMatrix(long)"/> and make it abstract.
		/// </summary>
		/// <param name="leadDim">leading dimension of matrix; if leadDim ≤ 0, it is assumed that leadDim = <c>sqrt(<see cref="AbstractArray{T}.Length"/>)</c>.</param>
		/// <returns>The reshaped matrix</returns>
		/// <remarks>This process is done via conversion to sparse vector first.</remarks>
		public override ValueArray<T> ToMatrix(long leadDim = 0)
		{
			var size = CheckSize(new[] { NRows, 0 });
			leadDim = size[0];
			if (leadDim == this.NRows)
				return this;
			using var spVec = this.ToVector() as SparseVector<T>;
			return spVec.ToMatrix(leadDim);
		}

		/// <summary>
		/// Reshape the array to a general <see cref="DenseTensor{T}"/> with dimensionality = size. Override <see cref="ValueArray{T}.ToTensor(long[])"/>.
		/// </summary>
		/// <param name="size">The new dimensions. You can have one or zero uncertain dimension, indicated by a non-positive number.</param>
		/// <returns>The reshaped tensor</returns>
		/// <remarks>Not supported by sparse matrix</remarks>
		public override ValueArray<T> ToTensor(params long[] size) => throw new NotSupportedException();
		#endregion


		#region sparse array interface
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private long GetValuePointerOffset()
		{
			long offset = 0;
			if (this.Format == SparseMatrixFormat.CSR)
				offset = RT.CopyOut(this.RowPointer);
			else if (this.Format == SparseMatrixFormat.CSC)
				offset = RT.CopyOut(this.ColumnPointer);
			return offset;
		}

		/// <summary>
		/// Fill this sparse matrix's index array(s) with arithmetic sequence(s), from <see cref="ISparseArray{T}"/>.
		/// </summary>
		/// <param name="v">start values and steps of the sequence(s), must be of same length as <see cref="AbstractArray{T}.Size"/></param>
		/// <exception cref="ArgumentException">if the lengths/values of <paramref name="v"/> do not follow the rule</exception>
		public void FillIndexWithRange(params (int start, int step)[] v)
		{
			if (v is null)
				throw new ArgumentNullException(nameof(v));
			if (v.Length != 2)
				throw new ArgumentException(Resource.VectorWrongSize, nameof(v));
			if (v.Any(a => a.start < 0 || a.step <= 0))
				throw new ArgumentException(Resource.VectorWrongValue, nameof(v));
			Sparse.IndexFillWithRange(this.RowPointer, this.RowIndexLength, v[0].start, v[0].step);
			Sparse.IndexFillWithRange(this.ColumnPointer, this.ColumnIndexLength, v[0].start, v[0].step);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private (long offset, long count) GetNNZRange(Range range)
		{
			var (offset, count) = range.GetOffsetAndCount(this.NonZero);
			if (offset + count > this.NonZero || offset < 0)
				throw new ArgumentOutOfRangeException(nameof(range));
			return (offset, count);
		}

		/// <summary>
		/// Convert the values of this matrix to a C# array.
		/// </summary>
		/// <param name="ranges">the range with max value = <c>nnz</c>, default is all</param>
		/// <returns>C# array of type <typeparamref name="T"/> containing the values of this matrix</returns>
		public T[] ValueToFortranOrderArray(params Range[] ranges)
		{
			if (ranges is null || ranges.Length == 0)
				ranges = new[] { Range.All };
			if (ranges.Length != 1)
				throw new ArgumentException(Resource.VectorWrongSize, nameof(ranges));
			var (offset, count) = this.GetNNZRange(ranges[0]);
			return RT.CopyOutArray(this, length: count, offset: offset + this.GetValuePointerOffset());
		}

		/// <summary>
		/// Check the row and column range of index arrays then return the offset/count of them.
		/// </summary>
		/// <param name="row">row <see cref="Range"/></param>
		/// <param name="col">column <see cref="Range"/></param>
		/// <returns>row and column offset/count</returns>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="row"/> or <paramref name="col"/> is out of range</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private (long offsetRow, long countRow, long offsetCol, long countCol) CheckIndexRange(Range row, Range col)
		{
			var (offsetRow, countRow) = row.GetOffsetAndCount(this.RowIndexLength);
			if (offsetRow < 0 || offsetRow >= this.RowIndexLength)
				throw new ArgumentOutOfRangeException(nameof(row), row, Resource.RangeStartWrong);
			if (countRow <= 0)
				throw new ArgumentOutOfRangeException(nameof(row), row, Resource.RangeCountWrong);
			var (offsetCol, countCol) = col.GetOffsetAndCount(this.ColumnIndexLength);
			if (offsetCol < 0 || offsetCol >= this.IntColIdxLength)
				throw new ArgumentOutOfRangeException(nameof(col), col, Resource.RangeStartWrong);
			if (countCol <= 0)
				throw new ArgumentOutOfRangeException(nameof(col), col, Resource.RangeCountWrong);
			return (offsetRow, countRow, offsetCol, countCol);
		}

		/// <summary>
		/// Convert the index arrays of this matrix to an <see cref="IEnumerable{T}"/> of C# array
		/// </summary>
		/// <param name="ranges">the range of each index array, default all</param>
		/// <returns>an <see cref="IEnumerable{T}"/> of C# array of type <see cref="int"/></returns>
		public IEnumerable<int[]> IndexToIntArray(params Range[] ranges)
		{
			if (ranges is null || ranges.Length == 0)
				ranges = new[] { Range.All, Range.All };
			if (ranges.Length != 2)
				throw new ArgumentException(Resource.VectorWrongSize, nameof(ranges));
			var (offsetRow, countRow, offsetCol, countCol) = this.CheckIndexRange(ranges[0], ranges[1]);
			yield return RT.CopyOutArray(this.RowPointer, length: countRow, offset: offsetRow);
			yield return RT.CopyOutArray(this.ColumnPointer, length: countCol, offset: offsetCol);
		}

		/// <summary>
		/// Convert the index arrays of this matrix to an <see cref="IEnumerable{T}"/> of C# array
		/// </summary>
		/// <param name="ranges">the range of each index array, default all</param>
		/// <returns>an <see cref="IEnumerable{T}"/> of C# array of type <see cref="long"/></returns>
		public IEnumerable<long[]> IndexToLongArray(params Range[] ranges)
		{
			foreach (var item in this.IndexToIntArray(ranges))
			{
				yield return Array.ConvertAll(item, a => (long)a);
			}
		}

		/// <summary>
		/// Copy the <paramref name="values"/> into this sparse vector's value array.
		/// </summary>
		/// <param name="values">the value array of element type <typeparamref name="T"/></param>
		/// <param name="ranges">the ranges of each dimension, default is all</param>
		public void ValueFromFortranOrderArray(T[] values, params Range[] ranges)
		{
			if (values is null)
				throw new ArgumentNullException(nameof(values));
			if (ranges is null || ranges.Length == 0)
				ranges = new[] { Range.All };
			if (ranges.Length != 1)
				throw new ArgumentException(Resource.VectorWrongSize, nameof(ranges));
			var (offset, count) = this.GetNNZRange(ranges[0]);
			if (values.LongLength < count)
				throw new ArgumentException(Resource.VectorTooShort, nameof(values));
			RT.CopyIntoArray(this, values, length: count, offset: offset + this.GetValuePointerOffset());
		}

		/// <summary>
		/// Copy the <paramref name="indices"/> into this sparse vector's index array.
		/// </summary>
		/// <param name="indices">an <see cref="IEnumerable{T}"/> of C# <see cref="int"/> array</param>
		/// <param name="ranges">the range of each index array, default all</param>
		public void IndexFromIntArray(IEnumerable<int[]> indices, params Range[] ranges)
		{
			if (System.Linq.Enumerable.Count(indices) != 2)
				throw new ArgumentNullException(nameof(indices));
			int[] first = System.Linq.Enumerable.First(indices), last = System.Linq.Enumerable.Last(indices);
			if (first is null || last is null)
				throw new ArgumentNullException(nameof(indices));
			if (ranges is null || ranges.Length == 0)
				ranges = new[] { Range.All, Range.All };
			if (ranges.Length != 2)
				throw new ArgumentException(Resource.VectorWrongSize, nameof(ranges));
			var (offsetRow, countRow, offsetCol, countCol) = this.CheckIndexRange(ranges[0], ranges[1]);
			if (first.LongLength < countRow || last.LongLength < countCol)
				throw new ArgumentException(Resource.VectorTooShort, nameof(indices));
			RT.CopyIntoArray(this.RowPointer, first, length: countRow, offset: offsetRow);
			RT.CopyIntoArray(this.ColumnPointer, last, length: countCol, offset: offsetCol);
		}

		/// <summary>
		/// Copy the <paramref name="indices"/> into this sparse vector's index array.
		/// </summary>
		/// <param name="indices">an <see cref="IEnumerable{T}"/> of C# <see cref="long"/> array</param>
		/// <param name="ranges">the range of each index array, default all</param>
		public void IndexFromLongArray(IEnumerable<long[]> indices, params Range[] ranges)
		{
			if (System.Linq.Enumerable.Count(indices) != 2)
				throw new ArgumentNullException(nameof(indices));
			long[] first = System.Linq.Enumerable.First(indices), last = System.Linq.Enumerable.Last(indices);
			if (first is null || last is null)
				throw new ArgumentNullException(nameof(indices));
			var row = Array.ConvertAll(first, a => (int)a);
			var col = Array.ConvertAll(last, a => (int)a);
			this.IndexFromIntArray(new[] { row, col }, ranges);
		}

		/// <summary>
		/// Dispose this sparse matrix after comparing the pointers between this matrix and the target <paramref name="array"/>.
		/// </summary>
		/// <param name="array">the target <see cref="ISparseArray{T}"/> to compare</param>
		public void DisposeExclude(ISparseArray<T> array)
		{
			if (this == (array as SparseMatrix<T>))
				return;
			if (array is SparseVector<T> sv)
			{
				if (this.Pointer != sv.Pointer)
					this.Pointer.Dispose();
				this.RowPointer.Dispose();
				this.ColumnPointer.Dispose();
			}
			else if (array is SparseMatrix<T> sm)
			{
				if (this.Pointer != sm.Pointer)
					this.Pointer.Dispose();
				if (this.ColumnPointer != sm.ColumnPointer && this.ColumnPointer != sm.RowPointer)
					this.ColumnPointer.Dispose();
				if (this.RowPointer != sm.RowPointer && this.RowPointer != sm.ColumnPointer)
					this.RowPointer.Dispose();
			}
			else
			{
				// other cases cannot share same pointers
				this.Pointer.Dispose();
				this.RowPointer.Dispose();
				this.ColumnPointer.Dispose();
			}

			this.Disposed = true;
		}
		#endregion


		#region format conversion

		#region prune
		/// <summary>
		/// Prune this sparse matrix of <see cref="SparseMatrixFormat.Compressed"/> to a new one.
		/// </summary>
		/// <param name="threshold">values smaller than threshold are regarded as zeros, must be larger than or equal to 0</param>
		/// <returns>The pruned <see cref="SparseMatrix{T}"/>.</returns>
		/// <exception cref="NotSupportedException">if this matrix is not of <see cref="SparseMatrixFormat.Compressed"/></exception>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="threshold"/> ≤ 0</exception>
		public SparseMatrix<T> Prune(float threshold = default)
		{
			if ((this.Format & SparseMatrixFormat.Compressed) != 0)
				return Sparse.MatrixCompressedPrune(this, threshold);
			// else
			using var matCompress = this.ToFormat(SparseMatrixFormat.Compressed);
			return matCompress.Prune(threshold);
		}
		#endregion

		#region format convert
		/// <summary>
		/// Convert this matrix to target <see cref="SparseMatrixFormat"/>.<para/>
		/// The out-of-place arrays of this operation are:
		/// <list type="table">
		/// <listheader><term>Format1 ↔ Format2</term><description>  Out-of-place arrays</description></listheader>
		/// <item><term>COOR ↔ COOC</term><description>  All arrays</description></item>
		/// <item><term>COOR ↔ CSR</term><description>  Row index array <see cref="RowPointer"/></description></item>
		/// <item><term>COOR ↔ CSC</term><description>  All arrays</description></item>
		/// <item><term>COOC ↔ CSR</term><description>  All arrays</description></item>
		/// <item><term>COOC ↔ CSC</term><description>  Column index array <see cref="ColumnPointer"/></description></item>
		/// <item><term>CSR ↔ CSC</term><description>  All arrays</description></item>
		/// </list>
		/// </summary>
		/// <param name="target">target <see cref="SparseMatrixFormat"/></param>
		/// <param name="disposeThis">dispose this matrix during the progress or not.</param>
		/// <returns>Converted sparse matrix with format <paramref name="target"/>.</returns>
		/// <remarks>If the <paramref name="target"/> is compatible with <see cref="Format"/>, this matrix will be returned directly leaving <paramref name="disposeThis"/> ignored.</remarks>
		public SparseMatrix<T> ToFormat(SparseMatrixFormat target, bool disposeThis = false)
		{
			if ((target & this.Format) != 0) // early return if target
				return this;
			var newM = Sparse.MatrixSparseFormatConvert(this, target, MatrixOperation.None);
			if (disposeThis)
				this.DisposeComparedTo(newM);
			return newM;
		}
		#endregion

		#region transpose convert
		/// <summary>
		/// Transpose this matrix to target format.
		/// </summary>
		/// <param name="target">target <see cref="SparseMatrixFormat"/>, default any</param>
		/// <returns>Transposed sparse matrix with format <paramref name="target"/>.</returns>
		/// <remarks>
		/// The in-place transpose operations are:
		/// <list type="bullet">
		/// <item>CSR ↔ CSR</item>
		/// <item>COOR ↔ COOC</item>
		/// </list>
		/// Other transpositions with different <paramref name="target"/> formats are done by using <see cref="ToFormat"/> first.
		/// </remarks>
		public SparseMatrix<T> Transpose(SparseMatrixFormat target = SparseMatrixFormat.Any)
		{
			return Sparse.MatrixSparseFormatConvert(this, target, MatrixOperation.Transpose);
		}
		#endregion

		#region apply convert
		/// <summary>
		/// Apply this matrix with a <see cref="PowerOperation"/> and get a new sparse matrix with target format.
		/// </summary>
		/// <param name="op"><see cref="PowerOperation"/> to apply</param>
		/// <param name="target">target <see cref="SparseMatrixFormat"/></param>
		/// <returns>A new <see cref="SparseMatrix{T}"/> with <paramref name="op"/> applied and format <paramref name="target"/> and all three arrays are correctly referenced to this one.</returns>
		public SparseMatrix<T> ApplyOpWithFormat(PowerOperation op, SparseMatrixFormat target)
		{
			switch (op)
			{
				case PowerOperation.None:
				case PowerOperation.Conjugate:
				case PowerOperation.Transpose:
					return Sparse.MatrixSparseFormatConvert(this, target, MatrixOperation.Transpose);
				case PowerOperation.Dagger:
					return base.ConjugateOutOfPlace() as SparseMatrix<T>;
				default:
					throw new ArgumentOutOfRangeException(nameof(op));
			}
		}
		#endregion

		#endregion


		#region checkers
		private bool CanOverwrite(SparseMatrix<T> overwrite, SparseMatrixFormat format = (SparseMatrixFormat)(-1), long rows = 0, long cols = 0, long nnz = 0)
		{
			if (overwrite is null || overwrite == EmptySpMat)
				return false;
			if (rows == 0) rows = overwrite.NRows;
			if (cols == 0) cols = overwrite.NCols;
			if (nnz == 0) nnz = overwrite.NonZero;
			if ((int)format == -1) format = overwrite.Format;
			return overwrite.OnHost == this.OnHost && overwrite.Format == format && overwrite.NRows == rows && overwrite.NCols == cols && overwrite.NonZero == nnz;
		}

		private bool CanOverwrite(SparseVector<T> overwrite,long len = 0, long nnz = 0)
		{
			if (overwrite is null || overwrite == EmptySpVec)
				return false;
			if (len == 0) len = overwrite.Length;
			if (nnz == 0) nnz = overwrite.NonZero;
			return overwrite.OnHost == this.OnHost && overwrite.Length == len && overwrite.NonZero == nnz;
		}

		private bool CanOverwrite(SparseVector<T>[] overwrite, long count = 0, long len = 0, long[] nnzs = null)
		{
			if (overwrite is null || overwrite.LongLength == 0)
				return false;
			if (count == 0) count = overwrite.LongLength;
			if (len == 0) len = overwrite[0].Length;
			if (nnzs is null)
				return overwrite.LongLength == count && overwrite.All(o => o != null && o != EmptySpVec && o.OnHost == this.OnHost && o.Length == len);
			else
				return overwrite.LongLength == count && overwrite.Zip(nnzs).All(o => o.First != null && o.First != EmptySpVec && o.First.OnHost == this.OnHost && o.First.Length == len && o.First.NonZero == o.Second);
		}

		private bool CanOverwrite(DenseMatrix<T> overwrite, long rows = 0, long cols = 0, long ld = 0)
		{
			if (overwrite is null || overwrite == EmptyDnMat)
				return false;
			if (rows == 0) rows = overwrite.NRows;
			if (cols == 0) cols = overwrite.NCols;
			if (ld == 0) ld = overwrite.LeadDim;
			return overwrite.OnHost == this.OnHost && overwrite.NRows == rows && overwrite.NCols == cols && overwrite.LeadDim == ld;
		}

		private bool CanOverwrite(DenseVector<T> overwrite, long len = 0)
		{
			if (overwrite is null || overwrite == EmptyDnVec)
				return false;
			if (len == 0) len = overwrite.Length;
			return overwrite.OnHost == this.OnHost && overwrite.Length == len;
		}

		private bool CanOverwrite(DenseVector<T>[] overwrite, long count = 0, long len = 0)
		{
			if (overwrite is null || overwrite.LongLength == 0)
				return false;
			if (count == 0) count = overwrite.LongLength;
			if (len == 0) len = overwrite[0].Length;
			return overwrite.LongLength == count && overwrite.All(o => o != null && o != EmptyDnVec && o.OnHost == this.OnHost && o.Length == len);
		}
		#endregion


		#region sparse matrix sparse vector restricted
		#region other methods
		/// <summary>
		/// Join the array of <see cref="SparseVector{T}"/> forming into a <see cref="SparseMatrix{T}"/>. From <see cref="IMatrix{TMat, TVec, T}.FromColumnVectors"/>.
		/// </summary>
		/// <param name="vecs">the input array of <see cref="SparseVector{T}"/></param>
		public void FromColumnVectors(SparseVector<T>[] vecs)
		{
			if (vecs is null)
				throw new ArgumentNullException(nameof(vecs));
			if (vecs.LongLength <= 1)
				throw new ArgumentException(Resource.VectorTooShort, nameof(vecs));
			if (!vecs.All(e => e != null && e != EmptyDnVec && e.Length == vecs[0].Length))
				throw new ArgumentException(Resource.CannotOperate, nameof(vecs));

			// get col host
			var colHost = new int[vecs.LongLength + 1];
			for (long i = 1; i <= vecs.LongLength; i++)
			{
				colHost[i] = (int)vecs[i - 1].NonZero + colHost[i - 1];
			}
			if (!CanOverwrite(this, format: SparseMatrixFormat.CSC, rows: vecs[0].Length, cols: vecs.LongLength, nnz: colHost[^1]))
				throw new ArgumentException(Resource.VectorWrongSize, nameof(vecs));

			RT.CopyIntoArray(this.ColumnPointer, colHost);
			for (long i = 0; i < vecs.LongLength; i++)
			{
				RT.CopyTo(source: vecs[i].Pointer, dest: this.Pointer, length: vecs[i].NonZero, offsetDest: colHost[i]);
				RT.CopyTo(source: vecs[i].IndexPointer, dest: this.RowPointer, length: vecs[i].NonZero, offsetDest: colHost[i]);
			}
		}
		#endregion

		#region get range methods
		/// <summary>
		/// Get a new matrix by the column index range, from <see cref="IMatrix{TMat, T}.GetColumnRange(Range, TMat)"/>.
		/// </summary>
		/// <param name="columnRange"><see cref="Range"/> of columns</param>
		/// <param name="overwrite">the output <see cref="SparseMatrix{T}"/> to overwrite, default null means creating a ref matrix</param>
		/// <returns>A matrix constructed by these columns. If <paramref name="overwrite"/> does not fit, it will not be returned.</returns>
		/// <exception cref="InvalidOperationException">if this matrix is not of <see cref="SparseMatrixFormat.ColumnMajor"/></exception>
		public SparseMatrix<T> GetColumnRange(Range columnRange, SparseMatrix<T> overwrite = null)
		{
			var (_, _, from, count) = CheckRange(Range.All, columnRange);
			
			if (this.Format == SparseMatrixFormat.CSC)
			{
				// get the Pointer offset corresponding to the columnRange by retrieving ColumnPointer values at certain positions
				var fromOffset = RT.CopyOut(this.ColumnPointer, offset: from);
				var endOffset = RT.CopyOut(this.ColumnPointer, offset: from + count);
				var newNNZ = endOffset - fromOffset;
				// a new column pointer
				using var newColPtr = Storage<int>.Create(length: count + 1, this.OnHost);
				RT.CopyTo(source: this.ColumnPointer, dest: newColPtr, length: count + 1, offsetSource: from);
				Sparse.IndexAddScalar(newColPtr, -RT.CopyOut(newColPtr), count + 1);
				if (!CanOverwrite(overwrite, format: this.Format, rows: this.NRows, cols: count, nnz: newNNZ))
				{
					return new SparseMatrix<T>(this, this.NRows, count, offsetRef: fromOffset, rowPtr: this.RowPointer + fromOffset, colPtr: newColPtr, format: this.Format, herm: this.Hermitian);
				}
				else
				{
					RT.CopyTo(source: this, dest: overwrite, length: newNNZ, offsetSource: fromOffset);
					RT.CopyTo(source: this.RowPointer, dest: overwrite.RowPointer, length: newNNZ, offsetSource: fromOffset);
					RT.CopyTo(source: newColPtr, dest: overwrite.ColumnPointer, length: count + 1, offsetSource: 0);
					return overwrite;
				}
			}
			else if (this.Format == SparseMatrixFormat.COOC)
			{
				int fromPos = checked((int)from), endPos = checked((int)(from + count - 1));
				int lb = Sparse.IndexLowerUpperBound(this.ColumnPointer, this.IntNNZ, fromPos, lowerBound: true);
				int ub = Sparse.IndexLowerUpperBound(this.ColumnPointer, this.IntNNZ, endPos, lowerBound: false);
				var newNNZ = ub - lb;
				if (!CanOverwrite(overwrite, format: this.Format, rows: this.NRows, cols: count, nnz: newNNZ))
				{
					return new SparseMatrix<T>(this, this.NRows, count, offsetRef: lb, rowPtr: this.RowPointer + lb, colPtr: this.ColumnPointer + lb, format: this.Format);
				}
				else
				{
					RT.CopyTo(source: this, dest: overwrite, length: newNNZ, offsetSource: lb);
					RT.CopyTo(source: this.RowPointer, dest: overwrite.RowPointer, length: newNNZ, offsetSource: lb);
					RT.CopyTo(source: this.ColumnPointer, dest: overwrite.ColumnPointer, length: newNNZ, offsetSource: lb);
					return overwrite;
				}
			}
			else
				throw new InvalidOperationException(string.Format(Resource.Culture, Resource.SpMatMustFormat, SparseMatrixFormat.ColumnMajor));
		}

		/// <summary>
		/// Get a new matrix by the row index range, from <see cref="IMatrix{TMat, T}.GetRowRange(Range, TMat)"/>.
		/// </summary>
		/// <param name="rowRange"><see cref="Range"/> of rows</param>
		/// <param name="overwrite">the output <see cref="SparseMatrix{T}"/> to overwrite, default null means creating a ref matrix</param>
		/// <returns>A matrix constructed by these rows. If <paramref name="overwrite"/> does not fit, it will not be returned.</returns>
		/// <exception cref="InvalidOperationException">if this matrix is not of <see cref="SparseMatrixFormat.RowMajor"/></exception>
		public SparseMatrix<T> GetRowRange(Range rowRange, SparseMatrix<T> overwrite = null)
		{
			var (from, count, _, _) = CheckRange(rowRange, Range.All);
			if (this.Format == SparseMatrixFormat.CSR)
			{
				// get the Pointer offset corresponding to the columnRange by retrieving ColumnPointer values at certain positions
				int fromOffset = RT.CopyOut(this.RowPointer, offset: from);
				int endOffset = RT.CopyOut(this.RowPointer, offset: from + count);
				var newNNZ = endOffset - fromOffset;
				// a new row pointer
				using var newRowPtr = Storage<int>.Create(length: count + 1, this.OnHost);
				RT.CopyTo(source: this.RowPointer, dest: newRowPtr, length: count + 1, offsetSource: from);
				Sparse.IndexAddScalar(newRowPtr, -RT.CopyOut(newRowPtr), count + 1);
				if (!CanOverwrite(overwrite, format: this.Format, rows: count, cols: this.NCols, nnz: newNNZ))
				{
					return new SparseMatrix<T>(this, count, this.NRows, offsetRef: fromOffset, rowPtr: newRowPtr, colPtr: this.ColumnPointer + fromOffset, format: this.Format, herm: this.Hermitian);
				}
				else
				{
					RT.CopyTo(source: this, dest: overwrite, length: newNNZ, offsetSource: fromOffset);
					RT.CopyTo(source: this.ColumnPointer, dest: overwrite.ColumnPointer, length: newNNZ, offsetSource: fromOffset);
					RT.CopyTo(source: newRowPtr, dest: overwrite.RowPointer, length: count + 1, offsetSource: 0);
					return overwrite;
				}
			}
			else if (this.Format == SparseMatrixFormat.COOR)
			{
				int fromPos = checked((int)from), endPos = checked((int)(from + count - 1));
				int lb = Sparse.IndexLowerUpperBound(this.RowPointer, this.IntNNZ, fromPos, lowerBound: true);
				int ub = Sparse.IndexLowerUpperBound(this.RowPointer, this.IntNNZ, endPos, lowerBound: false);
				var newNNZ = ub - lb;
				if (!CanOverwrite(overwrite, format: this.Format, rows: count, cols: this.NCols, nnz: newNNZ))
				{
					return new SparseMatrix<T>(this, count, this.NCols, offsetRef: lb, rowPtr: this.RowPointer + lb, colPtr: this.ColumnPointer + lb, format: this.Format);
				}
				else
				{
					RT.CopyTo(source: this, dest: overwrite, length: newNNZ, offsetSource: lb);
					RT.CopyTo(source: this.ColumnPointer, dest: overwrite.ColumnPointer, length: newNNZ, offsetSource: lb);
					RT.CopyTo(source: this.RowPointer, dest: overwrite.RowPointer, length: newNNZ, offsetSource: lb);
					return overwrite;
				}
			}
			else
				throw new InvalidOperationException(string.Format(Resource.Culture, Resource.SpMatMustFormat, SparseMatrixFormat.RowMajor));
		}

		/// <summary>
		/// Get a sub-matrix by the row and column index ranges, from <see cref="IMatrix{TMat, T}.GetSubmatrix(Range, Range, TMat)"/>.
		/// </summary>
		/// <param name="rowRange"><see cref="Range"/> of rows</param>
		/// <param name="columnRange"><see cref="Range"/> of columns</param>
		/// <param name="overwrite">the output <see cref="SparseMatrix{T}"/> to overwrite, default null means creating a ref matrix (if possible)</param>
		/// <returns>A sub-matrix in this region. If <paramref name="overwrite"/> does not fit, it will not be returned.</returns>
		public SparseMatrix<T> GetSubmatrix(Range rowRange, Range columnRange, SparseMatrix<T> overwrite = null)
		{
			CheckRange(rowRange, columnRange);
			switch (this.Format)
			{
				case SparseMatrixFormat.COOR:
				case SparseMatrixFormat.CSR:
					var refedRows = this.GetRowRange(rowRange, EmptySpMat);
					using (var allNewRows = refedRows.ToFormat(this.Format == SparseMatrixFormat.COOR ? SparseMatrixFormat.COOC : SparseMatrixFormat.CSC))
					{
						allNewRows.Disposed = true;
						return allNewRows.GetColumnRange(columnRange, overwrite);
					}
				case SparseMatrixFormat.COOC:
				case SparseMatrixFormat.CSC:
					var refedCols = this.GetColumnRange(columnRange, EmptySpMat);
					using (var allNewCols = refedCols.ToFormat(this.Format == SparseMatrixFormat.COOC ? SparseMatrixFormat.COOR : SparseMatrixFormat.CSR))
					{
						allNewCols.Disposed = true;
						return allNewCols.GetRowRange(rowRange, overwrite);
					}
				default:
					throw new NotSupportedException(Resource.FormatNotSupport);
			}
		}

		/// <summary>
		/// Get part of the column vectors that forms the matrix, from <see cref="IMatrix{TMat, TVec, T}.GetColumns(Range, TVec[])"/>.
		/// </summary>
		/// <param name="colRange">the <see cref="Range"/> of columns</param>
		/// <param name="overwrite">the output array of <see cref="SparseVector{T}"/> to overwrite, default null means creating ref vectors if possible</param>
		/// <returns>An array of data type <see cref="SparseVector{T}"/>. If <paramref name="overwrite"/> does not fit, it will not be returned.</returns>
		/// <exception cref="InvalidOperationException">if this matrix is not of <see cref="SparseMatrixFormat.CSC"/></exception>
		/// <remarks>The <see cref="SparseMatrixFormat.COOC"/> is no longer supported (compared to <see cref="GetColumnRange(Range, SparseMatrix{T})"/>) since the finding of index will be executed multiple times.</remarks>
		public SparseVector<T>[] GetColumns(Range colRange, SparseVector<T>[] overwrite = null)
		{
			if (this.Format != SparseMatrixFormat.CSC)
				throw new InvalidOperationException(string.Format(Resource.Culture, Resource.SpMatMustFormat, SparseMatrixFormat.CSC));

			var (_, _, from, count) = CheckRange(Range.All, colRange);
			var colPtr = RT.CopyOutArray(this.ColumnPointer, length: count + 1, offset: from);
			var newNNZs = new long[count];
			for (long i = 0; i < count; i++)
			{
				newNNZs[i] = colPtr[i + 1] - colPtr[i];
			}
			bool canOverwrite = CanOverwrite(overwrite, count: count, len: this.NRows, nnzs: newNNZs);
			if (canOverwrite && overwrite != null)
			{
				for (long i = 0; i < count; i++)
				{
					RT.CopyTo(source: this, dest: overwrite[i], length: newNNZs[i], offsetSource: colPtr[i]);
					RT.CopyTo(source: this.RowPointer, dest: overwrite[i].IndexPointer, length: newNNZs[i], offsetSource: colPtr[i]);
				}
				return overwrite;
			}
			else
			{
				var rows = new SparseVector<T>[count];
				for (long i = 0; i < count; i++)
					rows[i] = new SparseVector<T>(this, this.NCols, newNNZs[i], indices: this.RowPointer, offset: colPtr[i]);
				return rows;
			}
		}

		/// <summary>
		/// Get all of the column vectors that forms the matrix, from <see cref="IMatrix{TMat, TVec, T}.GetColumns(TVec[])"/>.
		/// </summary>
		/// <param name="overwrite">the output array of <see cref="SparseVector{T}"/> to overwrite, default null means creating ref vectors if possible</param>
		/// <returns>An array of data type <see cref="SparseVector{T}"/>. If <paramref name="overwrite"/> does not fit, it will not be returned.</returns>
		/// <exception cref="InvalidOperationException">if this matrix is not of <see cref="SparseMatrixFormat.CSC"/></exception>
		/// <remarks>The <see cref="SparseMatrixFormat.COOC"/> is no longer supported (compared to <see cref="GetColumns(Range, SparseVector{T}[])"/>) since the finding of index will be executed multiple times.</remarks>
		public SparseVector<T>[] GetColumns(SparseVector<T>[] overwrite = null) => this.GetColumns(Range.All, overwrite);

		/// <summary>
		/// Get part of the row vectors that forms the matrix, from <see cref="IMatrix{TMat, TVec, T}.GetRows(Range, TVec[])"/>.
		/// </summary>
		/// <param name="rowRange">the <see cref="Range"/> of rows</param>
		/// <param name="overwrite">the output array of <see cref="SparseVector{T}"/> to overwrite, default null means creating ref vectors if possible</param>
		/// <returns>An array of data type <see cref="SparseVector{T}"/>. If <paramref name="overwrite"/> does not fit, it will not be returned.</returns>
		/// <exception cref="InvalidOperationException">if this matrix is not of <see cref="SparseMatrixFormat.CSR"/></exception>
		///  <remarks>The <see cref="SparseMatrixFormat.COOR"/> is no longer supported (compared to <see cref="GetRowRange(Range, SparseMatrix{T})"/>) since the finding of index will be executed multiple times.</remarks>
		public SparseVector<T>[] GetRows(Range rowRange, SparseVector<T>[] overwrite = null)
		{
			if (this.Format != SparseMatrixFormat.CSR)
				throw new InvalidOperationException(string.Format(Resource.Culture, Resource.SpMatMustFormat, SparseMatrixFormat.CSR));

			var (from, count, _, _) = CheckRange(rowRange, Range.All);
			var rowPtr = RT.CopyOutArray(this.RowPointer, length: count + 1, offset: from);
			var newNNZs = new long[count];
			for (long i = 0; i < count; i++)
			{
				newNNZs[i] = rowPtr[i + 1] - rowPtr[i];
			}
			bool canOverwrite = CanOverwrite(overwrite, count: count, len: this.NCols, nnzs: newNNZs);
			if (canOverwrite && overwrite != null)
			{
				for (long i = 0; i < count; i++)
				{
					RT.CopyTo(source: this, dest: overwrite[i], length: newNNZs[i], offsetSource: rowPtr[i]);
					RT.CopyTo(source: this.ColumnPointer, dest: overwrite[i].IndexPointer, length: newNNZs[i], offsetSource: rowPtr[i]);
				}
				return overwrite;
			}
			else
			{
				var rows = new SparseVector<T>[count];
				for (long i = 0; i < count; i++)
					rows[i] = new SparseVector<T>(this, this.NCols, newNNZs[i], indices: this.ColumnPointer, offset: rowPtr[i]);
				return rows;
			}
		}

		/// <summary>
		/// Get all of the row vectors that forms the matrix, from <see cref="IMatrix{TMat, TVec, T}.GetRows(TVec[])"/>.
		/// </summary>
		/// <param name="overwrite">the output array of <see cref="SparseVector{T}"/> to overwrite, default null means creating ref vectors if possible</param>
		/// <returns>An array of data type <see cref="SparseVector{T}"/>. If <paramref name="overwrite"/> does not fit, it will not be returned.</returns>
		/// <exception cref="InvalidOperationException">if this matrix is not of <see cref="SparseMatrixFormat.CSR"/></exception>
		///  <remarks>The <see cref="SparseMatrixFormat.COOR"/> is not supported since the finding of index will be executed multiple times.</remarks>
		public SparseVector<T>[] GetRows(SparseVector<T>[] overwrite = null) => this.GetRows(Range.All, overwrite);

		/// <summary>
		/// Get one column of the matrix, from <see cref="IMatrix{TMat, TVec, T}.GetColumnAt(Index, TVec)"/>.
		/// </summary>
		/// <param name="index">the <see cref="Index"/> of column</param>
		/// <param name="overwrite">the output <see cref="SparseVector{T}"/> to overwrite, default null means creating ref vectors if possible</param>
		/// <returns>The selected column as <see cref="SparseVector{T}"/>. If <paramref name="overwrite"/> does not fit, it will not be returned.</returns>
		/// <exception cref="InvalidOperationException">if this matrix is not of <see cref="SparseMatrixFormat.ColumnMajor"/></exception>
		public SparseVector<T> GetColumnAt(Index index, SparseVector<T> overwrite = null)
		{
			var (_, colIdx) = CheckRange(0, index);
			if (this.Format == SparseMatrixFormat.CSC)
			{
				var fromOffset = RT.CopyOut(this.ColumnPointer, offset: colIdx);
				var toOffset = RT.CopyOut(this.ColumnPointer, offset: colIdx + 1);
				var newNNZ = toOffset - fromOffset;
				if (!CanOverwrite(overwrite, len: this.NRows, nnz: newNNZ))
					return new SparseVector<T>(this, this.NRows, newNNZ: newNNZ, indices: this.RowPointer, offset: fromOffset);
				else
				{
					RT.CopyTo(source: this, dest: overwrite, length: newNNZ, offsetSource: fromOffset);
					RT.CopyTo(source: this.RowPointer, dest: overwrite.IndexPointer, length: newNNZ, offsetSource: fromOffset);
					return overwrite;
				}
			}
			else if (this.Format == SparseMatrixFormat.COOC)
			{
				int pos = checked((int)colIdx);
				int lb = Sparse.IndexLowerUpperBound(this.ColumnPointer, this.IntNNZ, pos, lowerBound: true);
				int ub = Sparse.IndexLowerUpperBound(this.ColumnPointer, this.IntNNZ, pos, lowerBound: false);
				var newNNZ = ub - lb;
				if (overwrite is null || overwrite == EmptySpVec || overwrite.Length != this.NRows || overwrite.NonZero != newNNZ)
					return new SparseVector<T>(this, this.NRows, newNNZ: newNNZ, indices: this.RowPointer, offset: lb);
				else
				{
					RT.CopyTo(source: this, dest: overwrite, length: newNNZ, offsetSource: lb);
					RT.CopyTo(source: this.RowPointer, dest: overwrite.IndexPointer, length: newNNZ, offsetSource: lb);
					return overwrite;
				}
			}
			else
				throw new InvalidOperationException(string.Format(Resource.Culture, Resource.SpMatMustFormat, SparseMatrixFormat.ColumnMajor));
		}

		/// <summary>
		/// Get one row of the matrix, from <see cref="IMatrix{TMat, TVec, T}.GetRowAt(Index, TVec)"/>.
		/// </summary>
		/// <param name="index">the <see cref="Index"/> of column</param>
		/// <param name="overwrite">the output <see cref="SparseVector{T}"/> to overwrite, default null means creating ref vectors if possible</param>
		/// <returns>The selected row as <see cref="SparseVector{T}"/>. If <paramref name="overwrite"/> does not fit, it will not be returned.</returns>
		/// <exception cref="InvalidOperationException">if this matrix is not of <see cref="SparseMatrixFormat.ColumnMajor"/></exception>
		public SparseVector<T> GetRowAt(Index index, SparseVector<T> overwrite = null)
		{
			var (rowIdx, _) = CheckRange(index, 0);
			if (this.Format == SparseMatrixFormat.CSR)
			{
				var fromOffset = RT.CopyOut(this.RowPointer, offset: rowIdx);
				var toOffset = RT.CopyOut(this.RowPointer, offset: rowIdx + 1);
				var newNNZ = toOffset - fromOffset;
				if (!CanOverwrite(overwrite, len: this.NCols, nnz: newNNZ))
					return new SparseVector<T>(this, this.NCols, newNNZ: newNNZ, indices: this.ColumnPointer, offset: fromOffset);
				else
				{
					RT.CopyTo(source: this, dest: overwrite, length: newNNZ, offsetSource: fromOffset);
					RT.CopyTo(source: this.ColumnPointer, dest: overwrite.IndexPointer, length: newNNZ, offsetSource: fromOffset);
					return overwrite;
				}
			}
			else if (this.Format == SparseMatrixFormat.COOR)
			{
				int pos = checked((int)rowIdx);
				int lb = Sparse.IndexLowerUpperBound(this.RowPointer, this.IntNNZ, pos, lowerBound: true);
				int ub = Sparse.IndexLowerUpperBound(this.RowPointer, this.IntNNZ, pos, lowerBound: false);
				var newNNZ = ub - lb;
				if (overwrite is null || overwrite == EmptySpVec || overwrite.Length != this.NCols || overwrite.NonZero != newNNZ)
					return new SparseVector<T>(this, this.NCols, newNNZ: newNNZ, indices: this.ColumnPointer, offset: lb);
				else
				{
					RT.CopyTo(source: this, dest: overwrite, length: newNNZ, offsetSource: lb);
					RT.CopyTo(source: this.ColumnPointer, dest: overwrite.IndexPointer, length: newNNZ, offsetSource: lb);
					return overwrite;
				}
			}
			else
				throw new InvalidOperationException(string.Format(Resource.Culture, Resource.SpMatMustFormat, SparseMatrixFormat.RowMajor));
		}
		#endregion

		#region diagonal methods
		/// <summary>
		/// The method to get diagonal elements, from <see cref="IMatrix{TMat, TVec, T}.GetDiag(long, TVec)"/>.
		/// </summary>
		/// <param name="k">diagonal index, 0 for diagonal, 1 for super-diagonal at one above, -1 for sub-diagonal at one below, etc.</param>
		/// <param name="overwrite">the output <see cref="SparseVector{T}"/> to overwrite, default null means creating a new vector</param>
		/// <returns>A new <see cref="SparseVector{T}"/> representing the (super-/sub-)diagonal elements.</returns>
		/// <exception cref="InvalidOperationException">if this matrix is not square</exception>
		public SparseVector<T> GetDiag(long k, SparseVector<T> overwrite = null)
		{
			return this.GetDiag(k, overwrite as VectorBase<T>) as SparseVector<T>;
		}

		/// <summary>
		/// The method to set diagonal elements, from <see cref="IMatrix{TMat, TVec, T}.SetDiag(long, TVec)"/>.
		/// </summary>
		/// <param name="k">diagonal index, 0 for diagonal, 1 for super-diagonal at one above, -1 for sub-diagonal at one below, etc.</param>
		/// <param name="vec">the <see cref="SparseVector{T}"/></param>
		/// <exception cref="InvalidOperationException">if this matrix is not square</exception>
		public void SetDiag(long k, SparseVector<T> vec)
		{
			if (this.NRows != this.NCols)
				throw new InvalidOperationException(Resource.MatMustSquare);
			if (vec is null || vec == EmptySpVec)
				throw new ArgumentNullException(nameof(vec), Resource.ArrayCannotNull);
			using var dv = vec.ToDense();
			this.SetDiag(k, dv as VectorBase<T>);
		}
		#endregion

		#region transpositions
		private SparseMatrixFormat GetTransposedFormat()
		{
			return this.Format switch
			{
				SparseMatrixFormat.COOR => SparseMatrixFormat.COOC,
				SparseMatrixFormat.COOC => SparseMatrixFormat.COOR,
				SparseMatrixFormat.CSR => SparseMatrixFormat.CSC,
				SparseMatrixFormat.CSC => SparseMatrixFormat.CSR,
				_ => throw new NotSupportedException(Resource.FormatNotSupport),
			};
		}

		/// <summary>
		/// Calculate the transpose of this matrix, from <see cref="IMatrix{TMat, T}.Transpose(TMat)"/>;
		/// </summary>
		/// <param name="overwrite">the output <see cref="SparseMatrix{T}"/> to overwrite, default null means creating a new matrix</param>
		/// <returns>The transposed matrix out-of-place. If <paramref name="overwrite"/> does not fit, it will not be used.</returns>
		public SparseMatrix<T> Transpose(SparseMatrix<T> overwrite = null)
		{
			if (this.Hermitian && this.IsRealType)
				return this;
			if (!CanOverwrite(overwrite, format: this.GetTransposedFormat(), rows: this.NCols, cols: this.NRows, nnz: this.NonZero))
			{
				return this.Transpose(target: SparseMatrixFormat.Any);
			}
			else
			{
				RT.CopyTo(source: this.Pointer, dest: overwrite.Pointer, length: this.NonZero);
				RT.CopyTo(source: this.RowPointer, dest: overwrite.ColumnPointer, length: this.RowIndexLength);
				RT.CopyTo(source: this.ColumnPointer, dest: overwrite.RowPointer, length: this.ColumnIndexLength);
				return overwrite;
			}
		}

		/// <summary>
		/// Calculate the conjugate transpose of this matrix, from <see cref="IMatrix{TMat, T}.ConjugateTranspose(TMat)"/>.
		/// </summary>
		/// <param name="overwrite">the output <see cref="SparseMatrix{T}"/> to overwrite, default null means creating a new matrix</param>
		/// <returns>The conjugate transposed matrix out-of-place. If <paramref name="overwrite"/> does not fit, it will not be used.</returns>
		public SparseMatrix<T> ConjugateTranspose(SparseMatrix<T> overwrite = null)
		{
			if (this.Hermitian)
				return this;
			if (this.IsRealType)
				return this.Transpose(overwrite);
			if (!CanOverwrite(overwrite, format: this.GetTransposedFormat(), rows: this.NCols, cols: this.NRows, nnz: this.NonZero))
			{
				return this.ApplyOpWithFormat(PowerOperation.Dagger, SparseMatrixFormat.Any);
			}
			else
			{
				RT.CopyTo(source: this.Pointer, dest: overwrite.Pointer, length: this.NonZero);
				RT.CopyTo(source: this.RowPointer, dest: overwrite.ColumnPointer, length: this.RowIndexLength);
				RT.CopyTo(source: this.ColumnPointer, dest: overwrite.RowPointer, length: this.ColumnIndexLength);
				BLAS.PointWiseConjugate(overwrite);
				return overwrite;
			}
		}

		/// <summary>
		/// Symmetrize this matrix by adding its conjugate transpose out-of-place, from <see cref="IMatrix{TMat, T}.Symmetrize(bool, TMat)"/>.
		/// </summary>
		/// <param name="conjugateAtLast">return the original </param>
		/// <param name="overwrite">the output <see cref="SparseMatrix{T}"/> to overwrite, default null means creating a new matrix; note that it cannot always be overwritten</param>
		/// <returns>If <c><paramref name="conjugateAtLast"/> == false</c>: $B_{\text{result}}=\frac{A + A^H}{2}$; otherwise: $B_{\text{result}}=\frac{\bar{A} + A^T}{2}$</returns>
		public SparseMatrix<T> Symmetrize(bool conjugateAtLast = false, SparseMatrix<T> overwrite = null)
		{
			return base.Symmetrize(conjugateAtLast, overwrite) as SparseMatrix<T>;
		}
		#endregion

		#region operations
		/// <summary>
		/// Compute $C_{\text{this}} = \alpha A^{\text{opA}} + \beta B^{\text{opB}}$, from <see cref="IMatrix{TMat, T}.From_αA_Add_βB"/>.
		/// </summary>
		/// <param name="α">scalar of type <typeparamref name="T"/> with default 0. If <c><paramref name="α"/> == 0</c>, <paramref name="A"/> can be an invalid input</param>
		/// <param name="A">the input <see cref="SparseMatrix{T}"/> A, can be null</param>
		/// <param name="opA">operation to matrix <paramref name="A"/></param>
		/// <param name="β">scalar of type <typeparamref name="T"/> with default 0. If <c><paramref name="β"/> == 0</c>, <paramref name="B"/> can be an invalid input</param>
		/// <param name="B">the input <see cref="SparseMatrix{T}"/> B, can be null</param>
		/// <param name="opB">operation to matrix <paramref name="B"/></param>
		/// <exception cref="ArgumentNullException">if all of the array are null</exception>
		/// <exception cref="ArgumentException">if the arrays do not match in size</exception>
		/// <exception cref="StatusException">if the internal returns error status</exception>
		/// <remarks>This operation cannot overwrite this matrix when both <paramref name="A"/> and <paramref name="B"/> are built-in matrix classes.</remarks>
		public void From_αA_Add_βB(SparseMatrix<T> A, SparseMatrix<T> B, T α = default, T β = default, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			bool zeroA = α.Equals(Scalars<T>.Zero) || A is null || A == EmptyDnMat;
			bool zeroB = β.Equals(Scalars<T>.Zero) || B is null || B == EmptyDnMat;
			if (zeroA && zeroB)
				throw new ArgumentException(Resource.ParaCannotZero);
			if (zeroA) // cannot overwrite this matrix
			{
				if (B is null || B == EmptyDnMat)
					throw new ArgumentNullException(nameof(B), Resource.ArrayCannotNull);

				Sparse.MatrixSparseFormatConvert(B, this, SparseMatrixFormat.Any, opB);
			}
			else if (zeroB) // symmetric to zeroA: switch A & B
			{
				this.From_αA_Add_βB(EmptySpMat, A, β: α, opB: opA);
			}
			else // all non zero
			{
				if (A is null || A == EmptyDnMat)
					throw new ArgumentNullException(nameof(A), Resource.ArrayCannotNull);
				if (B is null || B == EmptyDnMat)
					throw new ArgumentNullException(nameof(B), Resource.ArrayCannotNull);

				Sparse.MatrixSparseAddSparse(A, B, this, α, β, opA, opB);
			}
		}

		/// <summary>
		/// Compute $C_{\text{this}} = \alpha A^{\text{opA}} B^{\text{opB}} + \beta C_{\text{this}}$, from <see cref="IMatrix{TMat, T}.Mulβ_AddBy_αAB"/>.
		/// </summary>
		/// <param name="A">the input <see cref="SparseMatrix{T}"/> A</param>
		/// <param name="opA">operation to matrix <paramref name="A"/></param>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		/// <param name="B">the input <see cref="SparseMatrix{T}"/> B</param>
		/// <param name="opB">operation to matrix <paramref name="B"/></param>
		/// <param name="β">scalar of type <typeparamref name="T"/> with default 0</param>
		/// <returns>A new <see cref="SparseMatrix{T}"/> as the result.</returns>
		/// <exception cref="ArgumentNullException">if any of the array is null</exception>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="α"/> is zero</exception>
		/// <exception cref="ArgumentException">if the arrays do not match in size</exception>
		/// <exception cref="StatusException">if the internal calculation returns error status</exception>
		/// <remarks>This operation cannot overwrite this matrix when both <paramref name="A"/> and <paramref name="B"/> are built-in matrix classes.</remarks>
		public void Mulβ_AddBy_αAB(SparseMatrix<T> A, SparseMatrix<T> B, T α, T β = default, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			if (A is null || A == EmptyDnMat)
				throw new ArgumentNullException(nameof(A), Resource.ArrayCannotNull);
			if (B is null || B == EmptyDnMat)
				throw new ArgumentNullException(nameof(B), Resource.ArrayCannotNull);
			if (α.Equals(Scalars<T>.Zero))
				throw new ArgumentOutOfRangeException(nameof(α), α, Resource.ParaCannotZero);
			if (this.NRows != (opA == MatrixOperation.None ? A.NRows : A.NCols) || this.NCols != (opB == MatrixOperation.None ? B.NCols : B.NRows))
				throw new ArgumentException(Resource.MatrixWrongSize);

			Sparse.MatrixSparseMultiplySparse(A, B, this, α, β, opA, opB);
		}

		/// <summary>
		/// Compute Kronecker product $A \otimes B$. If <paramref name="forceHerm"/> is true, then $(A \otimes B^H + A^H \otimes B)/2$ will be calculated. From <see cref="IMatrix{TMat, T}.KroneckerProd"/>.
		/// </summary>
		/// <param name="B">right <see cref="SparseMatrix{T}"/></param>
		/// <param name="forceHerm">if the result is made Hermitian or not</param>
		/// <param name="overwrite">the <see cref="SparseMatrix{T}"/> to overwrite by result, default null</param>
		/// <returns>The result of Kronecker product, a new <see cref="SparseMatrix{T}"/> or <paramref name="overwrite"/> if it is not null.</returns>
		public SparseMatrix<T> KroneckerProd(SparseMatrix<T> B, bool forceHerm = true, SparseMatrix<T> overwrite = null)
		{
			if (B is null || B == EmptyDnMat)
				throw new ArgumentNullException(nameof(B), Resource.ArrayCannotNull);
			if (B.OnHost != this.OnHost)
				throw new ArgumentException(Resource.RequireSamePos, nameof(B));
			if (B.OnHost != this.OnHost)
				throw new ArgumentException(Resource.RequireSamePos, nameof(B));

			SparseMatrix<T> sA = null, sB = null, output = null;
			try
			{
				sB = B.ToFormat(SparseMatrixFormat.Coordinated);
				sA = this.ToFormat(SparseMatrixFormat.Coordinated);
				output = CanOverwrite(overwrite, format: SparseMatrixFormat.Coordinated, rows: this.NRows * B.NRows, cols: this.NCols * B.NCols, nnz: this.NonZero * B.NonZero) ? overwrite : new SparseMatrix<T>(rows: this.NRows * B.NRows, cols: this.NCols * B.NCols, nonZeros: this.NonZero * B.NonZero, format: SparseMatrixFormat.COOR, onHost: this.OnHost, herm: this.Hermitian && B.Hermitian);

				if ((this.Hermitian && B.Hermitian) || !forceHerm)
				{
					Sparse.SparseMatrixKronecker(sA, sB, output, targetCOOC: false);
				}
				else
				{
					if (output != overwrite)
						output.Dispose();
					SparseMatrix<T> A_T = null, B_T = null;
					try
					{
						A_T = this.ApplyOpWithFormat(PowerOperation.Dagger, SparseMatrixFormat.Coordinated);
						B_T = B.ApplyOpWithFormat(PowerOperation.Dagger, SparseMatrixFormat.Coordinated);
						using var A_Bt = output.NewArrayAlike() as SparseMatrix<T>;
						Sparse.SparseMatrixKronecker(sA, B_T, A_Bt, targetCOOC: false);
						using var At_B = output.NewArrayAlike() as SparseMatrix<T>;
						Sparse.SparseMatrixKronecker(A_T, sB, At_B, targetCOOC: false);
						Sparse.MatrixSparseAddSparse(A_Bt, At_B, output, α: Scalars<T>.Half, β: Scalars<T>.Half);
					}
					finally
					{
						A_T?.DisposeExclude(this);
						B_T?.DisposeExclude(B);
					}
				}
				return output;
			}
			catch (Exception)
			{
				if (output != overwrite) output?.Dispose();
				throw;
			}
			finally
			{
				sA?.DisposeExclude(this);
				sB?.DisposeExclude(B);
			}
		}

		/// <summary>
		/// Compute Kronecker sum $A \oplus B \equiv A \otimes I + I \otimes B$ where $A$ is this matrix. If <paramref name="forceHerm"/> is true, then $[(A \otimes I + I \otimes B^H) + (A^H \otimes I + I \otimes B)]/2$ will be calculated. From <see cref="IMatrix{TMat, T}.KroneckerSum"/>.
		/// </summary>
		/// <param name="B">right <see cref="DenseMatrix{T}"/></param>
		/// <param name="forceHerm">if the result is made Hermitian or not</param>
		/// <param name="overwrite">the <see cref="SparseMatrix{T}"/> to overwrite by result, default null</param>
		/// <returns>The result of Kronecker sum, a new <see cref="SparseMatrix{T}"/> (<paramref name="overwrite"/> will not be used)</returns>
		public SparseMatrix<T> KroneckerSum(SparseMatrix<T> B, bool forceHerm = true, SparseMatrix<T> overwrite = null)
		{
			if (B is null || B == EmptyDnMat)
				throw new ArgumentNullException(nameof(B), Resource.ArrayCannotNull);
			if (this.NRows != this.NCols || B.NRows != B.NCols)
				throw new ArgumentException(Resource.MatMustSquare);
			if (B.OnHost != this.OnHost)
				throw new ArgumentException(Resource.RequireSamePos, nameof(B));

			SparseMatrix<T> output = null;
			try
			{
				output = CanOverwrite(overwrite, format: SparseMatrixFormat.Coordinated, rows: this.NRows * B.NRows, cols: this.NCols * B.NCols, nnz: this.NonZero * B.NonZero) ? overwrite : new SparseMatrix<T>(rows: this.NRows * B.NRows, cols: this.NCols * B.NCols, nonZeros: this.NonZero * B.NonZero, format: SparseMatrixFormat.COOR, onHost: this.OnHost, herm: this.Hermitian && B.Hermitian);
				using var eyeA = new SparseMatrix<T>(this.NRows, this.NRows, this.NRows, SparseMatrixFormat.COOC, this.OnHost);
				using var eyeB = new SparseMatrix<T>(B.NRows, B.NRows, B.NRows, SparseMatrixFormat.COOC, this.OnHost);
				eyeA.FillWithIdentity();
				eyeB.FillWithIdentity();

				if ((this.Hermitian && B.Hermitian) || !forceHerm)
				{
					SparseMatrix<T> sA = null, sB = null;
					try
					{
						sB = B.ToFormat(SparseMatrixFormat.Coordinated);
						sA = this.ToFormat(SparseMatrixFormat.Coordinated);
						using var A_eyeB = output.NewArrayAlike() as SparseMatrix<T>;
						Sparse.SparseMatrixKronecker(sA, eyeB, A_eyeB, targetCOOC: false);
						using var eyeA_B = output.NewArrayAlike() as SparseMatrix<T>;
						Sparse.SparseMatrixKronecker(eyeA, sB, eyeA_B, targetCOOC: false);
						Sparse.MatrixSparseAddSparse(A_eyeB, eyeA_B, output, α: Scalars<T>.One, β: Scalars<T>.One);
					}
					finally
					{
						sA?.DisposeExclude(this);
						sB?.DisposeExclude(B);
					}
				}
				else
				{
					SparseMatrix<T> symmA = null, symmB = null;
					try
					{
						symmA = this.Symmetrize(overwrite: EmptySpMat);
						symmA = symmA.ToFormat(SparseMatrixFormat.Coordinated, disposeThis: symmA != this);
						symmB = B.Symmetrize(overwrite: EmptySpMat);
						symmB = symmB.ToFormat(SparseMatrixFormat.Coordinated, disposeThis: symmB != B);
						using var A_eyeB = output.NewArrayAlike() as SparseMatrix<T>;
						Sparse.SparseMatrixKronecker(symmA, eyeB, A_eyeB, targetCOOC: false);
						using var eyeA_B = output.NewArrayAlike() as SparseMatrix<T>;
						Sparse.SparseMatrixKronecker(eyeA, symmB, eyeA_B, targetCOOC: false);
						Sparse.MatrixSparseAddSparse(A_eyeB, eyeA_B, output, α: Scalars<T>.One, β: Scalars<T>.One);
					}
					finally
					{
						symmA?.DisposeExclude(this);
						symmB?.DisposeExclude(B);
					}
				}
				return output;
			}
			catch (Exception)
			{
				if (output != overwrite) output?.Dispose();
				throw;
			}
		}
		#endregion
		#endregion


		#region dense matrix dense vector restricted
		#region other methods
		/// <summary>
		/// Join the array of <see cref="DenseVector{T}"/> forming into a <see cref="DenseMatrix{T}"/>. From <see cref="IMatrix{TMat, TVec, T}.FromColumnVectors"/>.
		/// </summary>
		/// <param name="vecs">the input array of <see cref="DenseVector{T}"/> </param>
		public void FromColumnVectors(DenseVector<T>[] vecs) => throw new NotImplementedException();
		#endregion

		#region get range methods
		/// <summary>
		/// Get a new matrix by the column index range, from <see cref="IMatrix{TMat, T}.GetColumnRange(Range, TMat)"/>.
		/// </summary>
		/// <param name="columnRange"><see cref="Range"/> of columns</param>
		/// <param name="overwrite">the output <see cref="DenseMatrix{T}"/> to overwrite, default null means creating a ref matrix</param>
		/// <returns>A matrix constructed by these columns. If <paramref name="overwrite"/> does not fit, it will not be returned.</returns>
		/// <exception cref="InvalidOperationException">if this matrix is not of <see cref="SparseMatrixFormat.CSC"/></exception>
		public DenseMatrix<T> GetColumnRange(Range columnRange, DenseMatrix<T> overwrite = null)
		{
			var (_, _, from, count) = CheckRange(Range.All, columnRange);

			if (this.Format == SparseMatrixFormat.CSC)
			{
				// get the Pointer offset corresponding to the columnRange by retrieving ColumnPointer values at certain positions
				var fromOffset = RT.CopyOut(this.ColumnPointer, offset: from);
				// a new column pointer
				using var newColPtr = Storage<int>.Create(length: count + 1, this.OnHost);
				RT.CopyTo(source: this.ColumnPointer, dest: newColPtr, length: count + 1, offsetSource: from);
				Sparse.IndexAddScalar(newColPtr, -RT.CopyOut(newColPtr), count + 1);
				using var spMat = new SparseMatrix<T>(this, this.NRows, count, offsetRef: fromOffset, rowPtr: this.RowPointer + fromOffset, colPtr: newColPtr, format: this.Format, herm: this.Hermitian);
				if (!CanOverwrite(overwrite, rows: this.NRows, cols: count))
				{
					return spMat.ToDense();
				}
				else
				{
					Sparse.MatrixSparseCSCToDense(overwrite, spMat);
					return overwrite;
				}
			}
			else
				throw new InvalidOperationException(string.Format(Resource.Culture, Resource.SpMatMustFormat, SparseMatrixFormat.CSC));
		}

		/// <summary>
		/// Get a new matrix by the row index range, from <see cref="IMatrix{TMat, T}.GetRowRange(Range, TMat)"/>.
		/// </summary>
		/// <param name="rowRange"><see cref="Range"/> of rows</param>
		/// <param name="overwrite">the output <see cref="DenseMatrix{T}"/> to overwrite, default null means creating a ref matrix</param>
		/// <returns>A matrix constructed by these rows. If <paramref name="overwrite"/> does not fit, it will not be returned.</returns>
		/// <exception cref="InvalidOperationException">if this matrix is not of <see cref="SparseMatrixFormat.CSR"/></exception>
		public DenseMatrix<T> GetRowRange(Range rowRange, DenseMatrix<T> overwrite = null)
		{
			var (from, count, _, _) = CheckRange(rowRange, Range.All);
			if (this.Format == SparseMatrixFormat.CSR)
			{
				// get the Pointer offset corresponding to the columnRange by retrieving ColumnPointer values at certain positions
				int fromOffset = RT.CopyOut(this.RowPointer, offset: from);
				// a new row pointer
				using var newRowPtr = Storage<int>.Create(length: count + 1, this.OnHost);
				RT.CopyTo(source: this.RowPointer, dest: newRowPtr, length: count + 1, offsetSource: from);
				Sparse.IndexAddScalar(newRowPtr, -RT.CopyOut(newRowPtr), count + 1);
				using var spMat = new SparseMatrix<T>(this, count, this.NRows, offsetRef: fromOffset, rowPtr: newRowPtr, colPtr: this.ColumnPointer + fromOffset, format: this.Format, herm: this.Hermitian);
				if (!CanOverwrite(overwrite, rows: count, cols: this.NCols))
				{
					return spMat.ToDense();
				}
				else
				{
					Sparse.MatrixSparseCSRToDense(overwrite, spMat);
					return overwrite;
				}
			}
			else
				throw new InvalidOperationException(string.Format(Resource.Culture, Resource.SpMatMustFormat, SparseMatrixFormat.CSR));
		}

		/// <summary>
		/// Get a sub-matrix by the row and column index ranges, from <see cref="IMatrix{TMat, T}.GetSubmatrix(Range, Range, TMat)"/>.
		/// </summary>
		/// <param name="rowRange"><see cref="Range"/> of rows</param>
		/// <param name="columnRange"><see cref="Range"/> of columns</param>
		/// <param name="overwrite">the output <see cref="SparseMatrix{T}"/> to overwrite, default null means creating a ref matrix (if possible)</param>
		/// <returns>A sub-matrix in this region. If <paramref name="overwrite"/> does not fit, it will not be returned.</returns>
		public DenseMatrix<T> GetSubmatrix(Range rowRange, Range columnRange, DenseMatrix<T> overwrite = null)
		{
			CheckRange(rowRange, columnRange);
			switch (this.Format)
			{
				case SparseMatrixFormat.COOR:
				case SparseMatrixFormat.CSR:
					var refedRows = this.GetRowRange(rowRange, EmptySpMat);
					using (var allNewRows = refedRows.ToFormat(this.Format == SparseMatrixFormat.COOR ? SparseMatrixFormat.COOC : SparseMatrixFormat.CSC))
					{
						allNewRows.Disposed = true;
						return allNewRows.GetColumnRange(columnRange, overwrite);
					}
				case SparseMatrixFormat.COOC:
				case SparseMatrixFormat.CSC:
					var refedCols = this.GetColumnRange(columnRange, EmptySpMat);
					using (var allNewCols = refedCols.ToFormat(this.Format == SparseMatrixFormat.COOC ? SparseMatrixFormat.COOR : SparseMatrixFormat.CSR))
					{
						allNewCols.Disposed = true;
						return allNewCols.GetRowRange(rowRange, overwrite);
					}
				default:
					throw new NotSupportedException(Resource.FormatNotSupport);
			}
		}

		/// <summary>
		/// Get part of the column vectors that forms the matrix, from <see cref="IMatrix{TMat, TVec, T}.GetColumns(Range, TVec[])"/>.
		/// </summary>
		/// <param name="colRange">the <see cref="Range"/> of columns</param>
		/// <param name="overwrite">the output array of <see cref="DenseVector{T}"/> to overwrite, default null means creating ref vectors if possible</param>
		/// <returns>An array of data type <see cref="DenseVector{T}"/>. If <paramref name="overwrite"/> does not fit, it will not be returned.</returns>
		/// <exception cref="InvalidOperationException">if this matrix is not of <see cref="SparseMatrixFormat.CSC"/></exception>
		public DenseVector<T>[] GetColumns(Range colRange, DenseVector<T>[] overwrite = null)
		{
			if (this.Format != SparseMatrixFormat.CSC)
				throw new InvalidOperationException(string.Format(Resource.Culture, Resource.SpMatMustFormat, SparseMatrixFormat.CSC));

			var (_, _, from, count) = CheckRange(Range.All, colRange);
			var colPtr = RT.CopyOutArray(this.ColumnPointer, length: count + 1, offset: from);
			var newNNZs = new long[count];
			for (long i = 0; i < count; i++)
			{
				newNNZs[i] = colPtr[i + 1] - colPtr[i];
			}
			bool canOverwrite = CanOverwrite(overwrite, count: count, len: this.NRows);
			if (canOverwrite && overwrite != null)
			{
				for (long i = 0; i < count; i++)
				{
					overwrite[i].FillWithZeros();
					Sparse.VectorSetAtIndices(src: this.Pointer + colPtr[i], pos: this.RowPointer + colPtr[i], dst: overwrite[i].Pointer, N: newNNZs[i]);
				}
				return overwrite;
			}
			else
			{
				var sp = this.GetColumns(colRange, null as SparseVector<T>[]);
				try
				{
					var dn = new DenseVector<T>[count];
					for (long i = 0; i < count; i++)
					{
						dn[i] = sp[i].ToDense();
					}
					return dn;
				}
				finally
				{
					sp.ForEach((a, i) => a.Dispose());
				}
			}
		}

		/// <summary>
		/// Get all of the column vectors that forms the matrix, from <see cref="IMatrix{TMat, TVec, T}.GetColumns(TVec[])"/>.
		/// </summary>
		/// <param name="overwrite">the output array of <see cref="DenseVector{T}"/> to overwrite, default null means creating ref vectors if possible</param>
		/// <returns>An array of data type <see cref="DenseVector{T}"/>. If <paramref name="overwrite"/> does not fit, it will not be returned.</returns>
		/// <exception cref="InvalidOperationException">if this matrix is not of <see cref="SparseMatrixFormat.CSC"/></exception>
		public DenseVector<T>[] GetColumns(DenseVector<T>[] overwrite = null) => this.GetColumns(Range.All, overwrite);

		/// <summary>
		/// Get part of the row vectors that forms the matrix, from <see cref="IMatrix{TMat, TVec, T}.GetRows(Range, TVec[])"/>.
		/// </summary>
		/// <param name="rowRange">the <see cref="Range"/> of rows</param>
		/// <param name="overwrite">the output array of <see cref="DenseVector{T}"/> to overwrite, default null means creating ref vectors if possible</param>
		/// <returns>An array of data type <see cref="DenseVector{T}"/>. If <paramref name="overwrite"/> does not fit, it will not be returned.</returns>
		/// <exception cref="InvalidOperationException">if this matrix is not of <see cref="SparseMatrixFormat.CSR"/></exception>
		public DenseVector<T>[] GetRows(Range rowRange, DenseVector<T>[] overwrite = null)
		{
			if (this.Format != SparseMatrixFormat.CSR)
				throw new InvalidOperationException(string.Format(Resource.Culture, Resource.SpMatMustFormat, SparseMatrixFormat.CSR));

			var (from, count, _, _) = CheckRange(rowRange, Range.All);
			var rowPtr = RT.CopyOutArray(this.RowPointer, length: count + 1, offset: from);
			var newNNZs = new int[count];
			for (long i = 0; i < count; i++)
			{
				newNNZs[i] = rowPtr[i + 1] - rowPtr[i];
			}
			bool canOverwrite = CanOverwrite(overwrite, count: count, len: this.NCols);
			if (canOverwrite && overwrite != null)
			{
				for (long i = 0; i < count; i++)
				{
					overwrite[i].FillWithZeros();
					Sparse.VectorSetAtIndices(src: this.Pointer + rowPtr[i], pos: this.ColumnPointer + rowPtr[i], dst: overwrite[i].Pointer, N: newNNZs[i]);
				}
				return overwrite;
			}
			else
			{
				var sp = this.GetRows(rowRange, null as SparseVector<T>[]);
				try
				{
					var dn = new DenseVector<T>[count];
					for (long i = 0; i < count; i++)
					{
						dn[i] = sp[i].ToDense();
					}
					return dn;
				}
				finally
				{
					sp.ForEach((a, i) => a.Dispose());
				}
			}
		}

		/// <summary>
		/// Get all of the row vectors that forms the matrix, from <see cref="IMatrix{TMat, TVec, T}.GetRows(TVec[])"/>.
		/// </summary>
		/// <param name="overwrite">the output array of <see cref="DenseVector{T}"/> to overwrite, default null means creating ref vectors if possible</param>
		/// <returns>An array of data type <see cref="DenseVector{T}"/>. If <paramref name="overwrite"/> does not fit, it will not be returned.</returns>
		/// <exception cref="InvalidOperationException">if this matrix is not of <see cref="SparseMatrixFormat.CSR"/></exception>
		public DenseVector<T>[] GetRows(DenseVector<T>[] overwrite = null) => this.GetRows(Range.All, overwrite);

		/// <summary>
		/// Get one column of the matrix, from <see cref="IMatrix{TMat, TVec, T}.GetColumnAt(Index, TVec)"/>.
		/// </summary>
		/// <param name="index">the <see cref="Index"/> of column</param>
		/// <param name="overwrite">the output <see cref="DenseVector{T}"/> to overwrite, default null means creating ref vectors if possible</param>
		/// <returns>The selected column as <see cref="DenseVector{T}"/>. If <paramref name="overwrite"/> does not fit, it will not be returned.</returns>
		/// <exception cref="InvalidOperationException">if this matrix is not of <see cref="SparseMatrixFormat.ColumnMajor"/></exception>
		public DenseVector<T> GetColumnAt(Index index, DenseVector<T> overwrite = null)
		{
			var (_, colIdx) = CheckRange(0, index);
			int fromOffset, newNNZ;
			if (this.Format == SparseMatrixFormat.CSC)
			{
				fromOffset = RT.CopyOut(this.ColumnPointer, offset: colIdx);
				var toOffset = RT.CopyOut(this.ColumnPointer, offset: colIdx + 1);
				newNNZ = toOffset - fromOffset;
			}
			else if (this.Format == SparseMatrixFormat.COOC)
			{
				int fromPos = checked((int)colIdx);
				fromOffset = Sparse.IndexLowerUpperBound(this.ColumnPointer, this.IntNNZ, fromPos, lowerBound: true);
				int ub = Sparse.IndexLowerUpperBound(this.ColumnPointer, this.IntNNZ, fromPos, lowerBound: false);
				newNNZ = ub - fromOffset;
			}
			else
				throw new InvalidOperationException(string.Format(Resource.Culture, Resource.SpMatMustFormat, SparseMatrixFormat.ColumnMajor));

			if (!CanOverwrite(overwrite, len: this.NRows))
				return new SparseVector<T>(this, this.NRows, newNNZ: newNNZ, indices: this.RowPointer, offset: fromOffset).ToDense();
			else
			{
				if (overwrite is null) return null; // never here
				overwrite.FillWithZeros();
				Sparse.VectorSetAtIndices(src: this.Pointer + fromOffset, pos: this.RowPointer + fromOffset, dst: overwrite.Pointer, N: newNNZ);
				return overwrite;
			}
		}

		/// <summary>
		/// Get one row of the matrix, from <see cref="IMatrix{TMat, TVec, T}.GetRowAt(Index, TVec)"/>.
		/// </summary>
		/// <param name="index">the <see cref="Index"/> of column</param>
		/// <param name="overwrite">the output <see cref="DenseVector{T}"/> to overwrite, default null means creating ref vectors if possible</param>
		/// <returns>The selected row as <see cref="DenseVector{T}"/>. If <paramref name="overwrite"/> does not fit, it will not be returned.</returns>
		/// <exception cref="InvalidOperationException">if this matrix is not of <see cref="SparseMatrixFormat.ColumnMajor"/></exception>
		public DenseVector<T> GetRowAt(Index index, DenseVector<T> overwrite = null)
		{
			var (rowIdx, _) = CheckRange(index, 0);
			int fromOffset, newNNZ;
			if (this.Format == SparseMatrixFormat.CSR)
			{
				fromOffset = RT.CopyOut(this.RowPointer, offset: rowIdx);
				var toOffset = RT.CopyOut(this.RowPointer, offset: rowIdx + 1);
				newNNZ = toOffset - fromOffset;
			}
			else if (this.Format == SparseMatrixFormat.COOR)
			{
				int fromPos = checked((int)rowIdx);
				fromOffset = Sparse.IndexLowerUpperBound(this.RowPointer, this.IntNNZ, fromPos, lowerBound: true);
				int ub = Sparse.IndexLowerUpperBound(this.RowPointer, this.IntNNZ, fromPos, lowerBound: false);
				newNNZ = ub - fromOffset;
			}
			else
				throw new InvalidOperationException(string.Format(Resource.Culture, Resource.SpMatMustFormat, SparseMatrixFormat.RowMajor));

			if (!CanOverwrite(overwrite, len: this.NCols))
				return new SparseVector<T>(this, this.NCols, newNNZ: newNNZ, indices: this.ColumnPointer, offset: fromOffset).ToDense();
			else
			{
				if (overwrite is null) return null; // never here
				overwrite.FillWithZeros();
				Sparse.VectorSetAtIndices(src: this.Pointer + fromOffset, pos: this.ColumnPointer + fromOffset, dst: overwrite.Pointer, N: newNNZ);
				return overwrite;
			}
		}
		#endregion

		#region diagonal methods
		/// <summary>
		/// The method to get diagonal elements, from <see cref="IMatrix{TMat, TVec, T}.GetDiag(long, TVec)"/>.
		/// </summary>
		/// <param name="k">diagonal index, 0 for diagonal, 1 for super-diagonal at one above, -1 for sub-diagonal at one below, etc.</param>
		/// <param name="overwrite">the output <see cref="VectorBase{T}"/> to overwrite, default null means creating a new vector</param>
		/// <returns>A new <see cref="VectorBase{T}"/> representing the (super-/sub-)diagonal elements.</returns>
		/// <exception cref="InvalidOperationException">if this matrix is not square</exception>
		public DenseVector<T> GetDiag(long k, DenseVector<T> overwrite = null)
		{
			using var spDiag = this.GetDiag(k, overwrite as VectorBase<T>);
			return spDiag.ToDense();
		}

		/// <summary>
		/// The method to set diagonal elements, from <see cref="IMatrix{TMat, TVec, T}.SetDiag(long, TVec)"/>.
		/// </summary>
		/// <param name="k">diagonal index, 0 for diagonal, 1 for super-diagonal at one above, -1 for sub-diagonal at one below, etc.</param>
		/// <param name="vec">the <see cref="DenseVector{T}"/></param>
		/// <exception cref="InvalidOperationException">if this matrix is not square</exception>
		public void SetDiag(long k, DenseVector<T> vec) => this.SetDiag(k, vec as VectorBase<T>);
		#endregion

		#region operations
		/// <summary>
		/// Calculate the transpose of this matrix. A new <see cref="DenseMatrix{T}"/> will be created. From <see cref="IMatrix{TMat, T}.Transpose(TMat)"/>;
		/// </summary>
		/// <param name="overwrite">the output <see cref="DenseMatrix{T}"/> to overwrite, default null means creating a new matrix</param>
		/// <returns>The transposed matrix out-of-place.</returns>
		public DenseMatrix<T> Transpose(DenseMatrix<T> overwrite = null)
		{
			var dM = this.ToDense();
			DenseMatrix<T> res = null;
			try
			{
				res = dM.Transpose(overwrite);
				return res;
			}
			finally
			{
				if (res != dM) dM.Dispose();
			}
		}

		/// <summary>
		/// Calculate the conjugate transpose of this matrix. A new <see cref="DenseMatrix{T}"/> will be created. From <see cref="MatrixBase{T}.ConjugateTranspose"/>.
		/// </summary>
		/// <param name="overwrite">the output <see cref="DenseMatrix{T}"/> to overwrite, default null means creating a new matrix</param>
		/// <returns>The conjugate transposed matrix out-of-place.</returns>
		public DenseMatrix<T> ConjugateTranspose(DenseMatrix<T> overwrite = null)
		{
			var dM = this.ToDense();
			DenseMatrix<T> res = null;
			try
			{
				res = dM.ConjugateTranspose(overwrite);
				return res;
			}
			finally
			{
				if (res != dM) dM.Dispose();
			}
		}

		/// <summary>
		/// Symmetrize this matrix by adding its conjugate transpose out-of-place, from <see cref="IMatrix{TMat, T}.Symmetrize(bool, TMat)"/>.
		/// </summary>
		/// <param name="conjugateAtLast">return the original </param>
		/// <param name="overwrite">the output <see cref="DenseMatrix{T}"/> to overwrite, default null means creating a new matrix; note that it cannot always be overwritten</param>
		/// <returns>If <c><paramref name="conjugateAtLast"/> == false</c>: $B_{\text{result}}=\frac{A + A^H}{2}$; otherwise: $B_{\text{result}}=\frac{\bar{A} + A^T}{2}$</returns>
		public DenseMatrix<T> Symmetrize(bool conjugateAtLast = false, DenseMatrix<T> overwrite = null)
		{
			var dM = this.ToDense();
			DenseMatrix<T> res = null;
			try
			{
				res = dM.Symmetrize(conjugateAtLast, overwrite);
				return res;
			}
			finally
			{
				if (res != dM) dM.Dispose();
			}
		}

		/// <summary>
		/// Compute $C_{\text{this}} = \alpha A^{\text{opA}} + \beta B^{\text{opB}}$, from <see cref="IMatrix{TMat, T}.From_αA_Add_βB"/>.
		/// </summary>
		/// <param name="α">scalar of type <typeparamref name="T"/> with default 0. If <c><paramref name="α"/> == 0</c>, <paramref name="A"/> can be an invalid input</param>
		/// <param name="A">the input <see cref="DenseMatrix{T}"/> A, can be null</param>
		/// <param name="opA">operation to matrix <paramref name="A"/></param>
		/// <param name="β">scalar of type <typeparamref name="T"/> with default 0. If <c><paramref name="β"/> == 0</c>, <paramref name="B"/> can be an invalid input</param>
		/// <param name="B">the input <see cref="DenseMatrix{T}"/> B, can be null</param>
		/// <param name="opB">operation to matrix <paramref name="B"/></param>
		/// <returns>A new <see cref="DenseMatrix{T}"/> as a result.</returns>
		/// <exception cref="ArgumentNullException">if all of the array are null</exception>
		/// <exception cref="ArgumentException">if the arrays do not match in size</exception>
		/// <exception cref="StatusException">if the internal returns error status</exception>
		/// <remarks>This operation cannot overwrite this matrix when both <paramref name="A"/> and <paramref name="B"/> are built-in matrix classes.</remarks>
		public void From_αA_Add_βB(DenseMatrix<T> A, DenseMatrix<T> B, T α = default, T β = default, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			using var newMat = new DenseMatrix<T>(this.NRows, this.NCols, this.OnHost, herm: A != null && A.Hermitian && (opA != MatrixOperation.Transpose || this.IsRealType) && B != null && B.Hermitian && (opB != MatrixOperation.Transpose || this.IsRealType));
			BLAS.MatrixGeneralAdd(A, B, newMat, α, β, opA, opB);
			Sparse.MatrixDenseToSparseCSR(newMat, this);
		}

		/// <summary>
		/// Compute $C_{\text{this}} = \alpha A^{\text{opA}} B^{\text{opB}} + \beta C_{\text{this}}$, from <see cref="IMatrix{TMat, T}.Mulβ_AddBy_αAB"/>.
		/// </summary>
		/// <param name="A">the input <see cref="DenseMatrix{T}"/> A</param>
		/// <param name="opA">operation to matrix <paramref name="A"/></param>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		/// <param name="B">the input <see cref="DenseMatrix{T}"/> B</param>
		/// <param name="opB">operation to matrix <paramref name="B"/></param>
		/// <param name="β">scalar of type <typeparamref name="T"/> with default 0</param>
		/// <returns>A new <see cref="DenseMatrix{T}"/> as the result.</returns>
		/// <exception cref="ArgumentNullException">if any of the array is null</exception>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="α"/> is zero</exception>
		/// <exception cref="ArgumentException">if the arrays do not match in size</exception>
		/// <exception cref="StatusException">if the internal calculation returns error status</exception>
		/// <remarks>This operation cannot overwrite this matrix when both <paramref name="A"/> and <paramref name="B"/> are built-in matrix classes.</remarks>
		public void Mulβ_AddBy_αAB(DenseMatrix<T> A, DenseMatrix<T> B, T α, T β = default, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			if (A is null || A == EmptyDnMat)
				throw new ArgumentNullException(nameof(A), Resource.ArrayCannotNull);
			if (B is null || B == EmptyDnMat)
				throw new ArgumentNullException(nameof(B), Resource.ArrayCannotNull);
			if (α.Equals(Scalars<T>.Zero))
				throw new ArgumentOutOfRangeException(nameof(α), α, Resource.ParaCannotZero);
			if (this.NRows != (opA == MatrixOperation.None ? A.NRows : A.NCols) || this.NCols != (opB == MatrixOperation.None ? B.NCols : B.NRows))
				throw new ArgumentException(Resource.MatrixWrongSize);

			var newMat = β.IsZero() ? new DenseMatrix<T>(this.NRows, this.NCols, this.OnHost) : this.ToDense();
			try
			{
				BLAS.MatrixMultiply(A, B, newMat, α, β, opA, opB);
				Sparse.MatrixDenseToSparseCSR(newMat, this);
			}
			catch (Exception)
			{
				newMat.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Compute Kronecker product $A \otimes B$. If <paramref name="forceHerm"/> is true, then $(A \otimes B^H + A^H \otimes B)/2$ will be calculated, from <see cref="IMatrix{TMat, T}.KroneckerProd"/> 
		/// </summary>
		/// <param name="B">right <see cref="DenseMatrix{T}"/></param>
		/// <param name="forceHerm">if the result is made Hermitian or not</param>
		/// <param name="overwrite">the <see cref="DenseMatrix{T}"/> to overwrite by result, default null</param>
		/// <returns>The result of Kronecker product, a new <see cref="DenseMatrix{T}"/> or <paramref name="overwrite"/> if it is not null.</returns>
		public DenseMatrix<T> KroneckerProd(DenseMatrix<T> B, bool forceHerm = true, DenseMatrix<T> overwrite = null)
		{
			if (B is null || B == EmptyDnMat)
				throw new ArgumentNullException(nameof(B), Resource.ArrayCannotNull);
			if (B.OnHost != this.OnHost)
				throw new ArgumentException(Resource.RequireSamePos, nameof(B));

			using var dm = this.ToDense();
			return dm.KroneckerProd(B, forceHerm, overwrite);
		}

		/// <summary>
		/// Compute Kronecker sum $A \oplus B \equiv A \otimes I + I \otimes B$ where $A$ is this matrix. If <paramref name="forceHerm"/> is true, then $[(A \otimes I + I \otimes B^H) + (A^H \otimes I + I \otimes B)]/2$ will be calculated, from <see cref="IMatrix{TMat, T}.KroneckerSum"/>.
		/// </summary>
		/// <param name="B">right <see cref="DenseMatrix{T}"/></param>
		/// <param name="forceHerm">if the result is made Hermitian or not</param>
		/// <param name="overwrite">the <see cref="DenseMatrix{T}"/> to overwrite by result, default null</param>
		/// <returns>The result of Kronecker product, a new <see cref="DenseMatrix{T}"/> or <paramref name="overwrite"/> if it is not null.</returns>z
		public DenseMatrix<T> KroneckerSum(DenseMatrix<T> B, bool forceHerm = true, DenseMatrix<T> overwrite = null)
		{
			if (B is null || B == EmptyDnMat)
				throw new ArgumentNullException(nameof(B), Resource.ArrayCannotNull);
			if (this.NRows != this.NCols || B.NRows != B.NCols)
				throw new ArgumentException(Resource.MatMustSquare);
			if (B.OnHost != this.OnHost)
				throw new ArgumentException(Resource.RequireSamePos, nameof(B));

			using var dm = this.ToDense();
			return dm.KroneckerSum(B, forceHerm, overwrite);
		}
		#endregion
		#endregion

		#region dense and sparse matrix restricted
		/// <summary>
		/// Compute $C_{\text{this}} = \alpha A^{\text{opA}} + \beta B^{\text{opB}}$. From_αA_Add_βB(TMat, TMat, T, T, MatrixOperation, MatrixOperation)"/>.
		/// </summary>
		/// <param name="α">scalar of type <typeparamref name="T"/> with default 0. If <c><paramref name="α"/> == 0</c>, <paramref name="A"/> can be an invalid input</param>
		/// <param name="A">the input <see cref="DenseMatrix{T}"/> A, can be null</param>
		/// <param name="opA">operation to matrix <paramref name="A"/></param>
		/// <param name="β">scalar of type <typeparamref name="T"/> with default 0. If <c><paramref name="β"/> == 0</c>, <paramref name="B"/> can be an invalid input</param>
		/// <param name="B">the input <see cref="SparseMatrix{T}"/> B, can be null</param>
		/// <param name="opB">operation to matrix <paramref name="B"/></param>
		/// <returns>A new <see cref="DenseMatrix{T}"/> as a result.</returns>
		/// <exception cref="ArgumentNullException">if all of the array are null</exception>
		/// <exception cref="ArgumentException">if the arrays do not match in size</exception>
		/// <exception cref="StatusException">if the internal returns error status</exception>
		/// <remarks>This operation cannot overwrite this matrix when both <paramref name="A"/> and <paramref name="B"/> are built-in matrix classes.</remarks>
		public void From_αA_Add_βB(DenseMatrix<T> A, SparseMatrix<T> B, T α = default, T β = default, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			bool zeroA = α.Equals(Scalars<T>.Zero) || A is null || A == EmptyDnMat;
			bool zeroB = β.Equals(Scalars<T>.Zero) || B is null || B == EmptyDnMat;
			if (zeroA && zeroB)
				throw new ArgumentException(Resource.ParaCannotZero);
			if (zeroA) // cannot overwrite this matrix
			{
				this.From_αA_Add_βB(EmptySpMat, B, Scalars<T>.Zero, β, opB: opB);
			}
			else if (zeroB) // similar to zeroA
			{
				this.From_αA_Add_βB(EmptyDnMat, A, β: α, opB: opA);
			}
			else // all non zero
			{
				if (A is null || A == EmptyDnMat)
					throw new ArgumentNullException(nameof(A), Resource.ArrayCannotNull);
				if (B is null || B == EmptyDnMat)
					throw new ArgumentNullException(nameof(B), Resource.ArrayCannotNull);

				using var dB = B.ToDense();
				this.From_αA_Add_βB(A, dB, α, β, opA, opB);
			}
		}

		/// <summary>
		/// Compute $C_{\text{this}} = \alpha A^{\text{opA}} B^{\text{opB}} + \beta C_{\text{this}}$. This method will return a new matrix irrelevant to this one, from <see cref="IMatrix{TMat, T}.Mulβ_AddBy_αAB(TMat, TMat, T, T, MatrixOperation, MatrixOperation)"/>.
		/// </summary>
		/// <param name="A">the input <see cref="DenseMatrix{T}"/> A</param>
		/// <param name="opA">operation to matrix <paramref name="A"/></param>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		/// <param name="B">the input <see cref="SparseMatrix{T}"/> B</param>
		/// <param name="opB">operation to matrix <paramref name="B"/></param>
		/// <param name="β">scalar of type <typeparamref name="T"/> with default 0</param>
		/// <returns>A new <see cref="DenseMatrix{T}"/> as the result.</returns>
		/// <exception cref="ArgumentNullException">if any of the array is null</exception>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="α"/> is zero</exception>
		/// <exception cref="ArgumentException">if the arrays do not match in size</exception>
		/// <exception cref="StatusException">if the internal calculation returns error status</exception>
		/// <remarks>This operation cannot overwrite this matrix when both <paramref name="A"/> and <paramref name="B"/> are built-in matrix classes.</remarks>
		public void Mulβ_AddBy_αAB(DenseMatrix<T> A, SparseMatrix<T> B, T α, T β = default, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			if (A is null || A == EmptyDnMat)
				throw new ArgumentNullException(nameof(A), Resource.ArrayCannotNull);
			if (B is null || B == EmptyDnMat)
				throw new ArgumentNullException(nameof(B), Resource.ArrayCannotNull);
			if (α.Equals(Scalars<T>.Zero))
				throw new ArgumentOutOfRangeException(nameof(α), α, Resource.ParaCannotZero);
			if (this.NRows != (opA == MatrixOperation.None ? A.NRows : A.NCols) || this.NCols != (opB == MatrixOperation.None ? B.NCols : B.NRows))
				throw new ArgumentException(Resource.MatrixWrongSize);

			var newMat = β.Equals(Scalars<T>.Zero) ? new DenseMatrix<T>(this.NRows, this.NCols, this.OnHost) : this.ToDense();
			try
			{
				newMat.Mulβ_AddBy_αAB(A, B, α, β, opA, opB);
				Sparse.MatrixDenseToSparseCSR(newMat, this);
			}
			catch (Exception)
			{
				newMat.Dispose();
				throw;
			}
		}
		#endregion


		#region implement converter
		/// <summary>
		/// Convert this array to another memory.
		/// </summary>
		/// <returns>a new <see cref="ValueArray{T}"/> with same value as this one if this array is on host memory</returns>
		public override ValueArray<T> ToTheOtherMemory()
		{
			var newVec = new SparseMatrix<T>(this.NRows, this.NCols, this.NonZero, this.Format, !this.OnHost, this.Hermitian);
			try
			{
				RT.CopyTo(source: this.Pointer, dest: newVec.Pointer, length: this.NonZero);
				RT.CopyTo(source: this.RowPointer, dest: newVec.RowPointer, length: this.RowIndexLength);
				RT.CopyTo(source: this.ColumnPointer, dest: newVec.ColumnPointer, length: this.ColumnIndexLength);
				return newVec;
			}
			catch (Exception)
			{
				newVec.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Convert this matrix to a <see cref="DenseMatrix{T}"/>. Override <see cref="MatrixBase{T}.ToDense"/>.
		/// </summary>
		/// <param name="algorithm">the <see cref="SparseMatrixToDenseAlgorithm"/> to use</param>
		/// <returns>The converted <see cref="DenseMatrix{T}"/>.</returns>
		/// <remarks>If the <paramref name="algorithm"/> is <see cref="SparseMatrixToDenseAlgorithm.ViaVector"/>, it will be used only when <see cref="Format"/> is <see cref="SparseMatrixFormat.COOC"/>.</remarks>
		public override DenseMatrix<T> ToDense(SparseMatrixToDenseAlgorithm algorithm = default)
		{
			DenseMatrix<T> mat;
			switch (this.Format)
			{
				case SparseMatrixFormat.CSR:
					mat = new DenseMatrix<T>(this.NRows, this.NCols, this.OnHost);
					Sparse.MatrixSparseCSRToDense(mat, this);
					break;
				case SparseMatrixFormat.CSC:
					mat = new DenseMatrix<T>(this.NRows, this.NCols, this.OnHost);
					Sparse.MatrixSparseCSCToDense(mat, this);
					break;
				case SparseMatrixFormat.COOR:
					using (var tempR = this.ToFormat(SparseMatrixFormat.CSR))
					{
						mat = new DenseMatrix<T>(this.NRows, this.NCols, this.OnHost);
						Sparse.MatrixSparseCSRToDense(mat, tempR);
					}
					break;
				case SparseMatrixFormat.COOC:
					if (algorithm == SparseMatrixToDenseAlgorithm.ViaVector)
					{
						using var temp = this.ToVector() as SparseVector<T>;
						mat = temp.ToDense().ToMatrix() as DenseMatrix<T>;
					}
					else
					{
						using var temp = this.ToFormat(SparseMatrixFormat.CSC);
						mat = new DenseMatrix<T>(this.NRows, this.NCols, this.OnHost);
						Sparse.MatrixSparseCSCToDense(mat, temp);
					}
					break;
				default:
					throw new NotSupportedException(Resource.FormatNotSupport);
			}
			return mat;
		}

		/// <summary>
		/// Convert this matrix to a <see cref="SparseMatrix{T}"/>. The out-of-place conversion may be performed.
		/// </summary>
		/// <param name="threshold">values smaller than thresholds are regarded as zeros, must be larger than or equal to 0</param>
		/// <param name="targetFormat">the target <see cref="SparseMatrix{T}"/>'s format, see <see cref="SparseMatrixFormat"/></param>
		/// <param name="algorithm">the <see cref="DenseMatrixToSparseAlgorithm"/> to use, default is null which means that the default algorithms corresponding to the <paramref name="targetFormat"/> and <typeparamref name="T"/> will be used</param>
		/// <returns>This matrix.</returns>
		public override SparseMatrix<T> ToSparse(float threshold = default, SparseMatrixFormat targetFormat = SparseMatrixFormat.Any, DenseMatrixToSparseAlgorithm? algorithm = null) => this.ToFormat(targetFormat);

		/// <summary>
		/// Create a new array with same immutable properties as this one, the mutable status will not be copied.
		/// </summary>
		/// <returns>The array alike this one.</returns>
		public override AbstractArray<T> NewArrayAlike() => new SparseMatrix<T>(this.NRows, this.NCols, this.NonZero, this.Format, this.Hermitian);

		/// <summary>
		/// Take out the data array as a new <see cref="DenseVector{T}"/>, override <see cref="ValueArray{T}.AsDenseVector"/>.
		/// </summary>
		/// <returns>A new <see cref="DenseVector{T}"/> containing the referenced data array of this one.</returns>
		public override DenseVector<T> AsDenseVector() => new DenseVector<T>(this.Pointer, this.NonZero);

		/// <summary>
		/// Create a new array like this one (with same type and other info) while the data type is <typeparamref name="TOut"/>
		/// </summary>
		/// <typeparam name="TOut">the new data type</typeparam>
		/// <returns>the new array</returns>
		public override ValueArray<TOut> NewArrayAlike<TOut>() => new SparseMatrix<TOut>(this.NRows, this.NCols, this.NonZero, this.Format, this.Hermitian);

		/// <summary>
		/// Cast this array into another data type <typeparamref name="TOut"/>.
		/// </summary>
		/// <typeparam name="TOut">the data type to cast to</typeparam>
		/// <returns>The casted <see cref="AbstractArray{T}"/>.</returns>
		public override AbstractArray<TOut> DataTypeCast<TOut>()
		{
			if (typeof(TOut) == typeof(T))
				return this as SparseVector<TOut>;
			var mat = base.DataTypeCast<TOut>() as SparseMatrix<TOut>;
			try
			{
				RT.CopyTo(this.RowPointer, mat.RowPointer, this.RowIndexLength);
				RT.CopyTo(this.ColumnPointer, mat.ColumnPointer, this.ColumnIndexLength);
				return mat;
			}
			catch (Exception)
			{
				mat.Dispose();
				throw;
			}
		}
		#endregion


		#region implement abstract methods
		/// <summary>
		/// Fill this matrix with identity.
		/// </summary>
		public override void FillWithIdentity()
		{
			Sparse.FillIdentity(this);
		}

		/// <summary>
		/// Override the <see cref="AbstractArray{T}.Clone"/>.
		/// </summary>
		/// <returns>Copied matrix</returns>
		public override object Clone() => new SparseMatrix<T>(this, copyIndex: true);

		/// <summary>
		/// Make this matrix actually Hermitian (if <see cref="MatrixBase{T}.Hermitian"/> is true now) by setting the lower half same as upper, override <see cref="MatrixBase{T}.CopyUpperToLower"/>.
		/// </summary>
		public override void CopyUpperToLower() { }

		/// <summary>
		/// Join the array of <see cref="VectorBase{T}"/> forming into a <see cref="MatrixBase{T}"/> to overwrite this matrix.
		/// </summary>
		/// <param name="vecs">the input array of <see cref="VectorBase{T}"/></param>
		public override void FromColumnVectors(VectorBase<T>[] vecs)
		{
			this.FromColumnVectors(vecs as SparseVector<T>[]);
		}

		/// <summary>
		/// Get a new matrix by the column index range, from <see cref="IMatrix{TMat, T}.GetColumnRange(Range, TMat)"/>.
		/// </summary>
		/// <param name="columnRange"><see cref="Range"/> of columns</param>
		/// <param name="overwrite">the output <see cref="MatrixBase{T}"/> to overwrite, default null means creating a ref matrix</param>
		/// <returns>A matrix constructed by these columns. If <paramref name="overwrite"/> does not fit, it will not be returned.</returns>
		public override MatrixBase<T> GetColumnRange(Range columnRange, MatrixBase<T> overwrite = null)
		{
			if (overwrite != null && overwrite is DenseMatrix<T> dm)
				return this.GetColumnRange(columnRange, dm);
			else if (overwrite != null && overwrite is SparseMatrix<T> sm)
				return this.GetColumnRange(columnRange, sm);
			else
				return this.GetColumnRange(columnRange, EmptySpMat);
		}

		/// <summary>
		/// Get a new matrix by the row index range, from <see cref="IMatrix{TMat, T}.GetRowRange(Range, TMat)"/>.
		/// </summary>
		/// <param name="rowRange"><see cref="Range"/> of rows</param>
		/// <param name="overwrite">the output <see cref="MatrixBase{T}"/> to overwrite, default null means creating a ref matrix</param>
		/// <returns>A matrix constructed by these rows. If <paramref name="overwrite"/> does not fit, it will not be returned.</returns>
		public override MatrixBase<T> GetRowRange(Range rowRange, MatrixBase<T> overwrite = null)
		{
			if (overwrite != null && overwrite is DenseMatrix<T> dm)
				return this.GetRowRange(rowRange, dm);
			else if (overwrite != null && overwrite is SparseMatrix<T> sm)
				return this.GetRowRange(rowRange, sm);
			else
				return this.GetRowRange(rowRange, EmptySpMat);
		}

		/// <summary>
		/// Get a sub-matrix by the row and column index ranges.
		/// </summary>
		/// <param name="rowRange"><see cref="Range"/> of rows</param>
		/// <param name="columnRange"><see cref="Range"/> of columns</param>
		/// <param name="overwrite">the output <see cref="MatrixBase{T}"/> to overwrite, default null means creating a ref matrix (if possible)</param>
		/// <returns>A sub-matrix in this region. If <paramref name="overwrite"/> does not fit, it will not be returned.</returns>
		public override MatrixBase<T> GetSubmatrix(Range rowRange, Range columnRange, MatrixBase<T> overwrite = null)
		{
			if (overwrite != null && overwrite is DenseMatrix<T> dm)
				return this.GetSubmatrix(rowRange, columnRange, dm);
			else if (overwrite != null && overwrite is SparseMatrix<T> sm)
				return this.GetSubmatrix(rowRange, columnRange, sm);
			else
				return this.GetSubmatrix(rowRange, columnRange, EmptySpMat);
		}

		/// <summary>
		/// Get part of the column vectors that forms the matrix, from <see cref="IMatrix{TMat, TVec, T}.GetColumns(Range, TVec[])"/>.
		/// </summary>
		/// <param name="colRange">the <see cref="Range"/> of columns</param>
		/// <param name="overwrite">the output array of <see cref="VectorBase{T}"/> to overwrite, default null means creating ref vectors if possible</param>
		/// <returns>An array of data type <see cref="VectorBase{T}"/>. If <paramref name="overwrite"/> does not fit, it will not be returned.</returns>
		public override VectorBase<T>[] GetColumns(Range colRange, VectorBase<T>[] overwrite = null)
		{
			if (overwrite != null && overwrite.All(v => v is SparseVector<T>))
				return this.GetColumns(colRange, overwrite.Select(v => v as SparseVector<T>).ToArray());
			else if (overwrite != null && overwrite.All(v => v is DenseVector<T>))
				return this.GetColumns(colRange, overwrite.Select(v => v as DenseVector<T>).ToArray());
			else
				return this.GetColumns(colRange, Array.Empty<SparseVector<T>>());
		}

		/// <summary>
		/// Get part of the row vectors that forms the matrix, from <see cref="IMatrix{TMat, TVec, T}.GetRows(Range, TVec[])"/>.
		/// </summary>
		/// <param name="rowRange">the <see cref="Range"/> of rows</param>
		/// <param name="overwrite">the output array of <see cref="VectorBase{T}"/> to overwrite, default null means creating ref vectors if possible</param>
		/// <returns>An array of data type <see cref="VectorBase{T}"/>. If <paramref name="overwrite"/> does not fit, it will not be returned.</returns>
		public override VectorBase<T>[] GetRows(Range rowRange, VectorBase<T>[] overwrite = null)
		{
			if (overwrite != null && overwrite.All(v => v is SparseVector<T>))
				return this.GetRows(rowRange, overwrite.Select(v => v as SparseVector<T>).ToArray());
			else if (overwrite != null && overwrite.All(v => v is DenseVector<T>))
				return this.GetRows(rowRange, overwrite.Select(v => v as DenseVector<T>).ToArray());
			else
				return this.GetRows(rowRange, Array.Empty<SparseVector<T>>());
		}

		/// <summary>
		/// Get one column of the matrix, from <see cref="IMatrix{TMat, TVec, T}.GetColumnAt(Index, TVec)"/>.
		/// </summary>
		/// <param name="index">the <see cref="Index"/> of column</param>
		/// <param name="overwrite">the output <see cref="VectorBase{T}"/> to overwrite, default null means creating a ref vector if possible</param>
		/// <returns>The selected column as a <see cref="VectorBase{T}"/>. If <paramref name="overwrite"/> does not fit, it will not be returned.</returns>
		public override VectorBase<T> GetColumnAt(Index index, VectorBase<T> overwrite = null)
		{
			if (overwrite != null && overwrite is SparseVector<T>)
				return this.GetColumnAt(index, overwrite as SparseVector<T>);
			else if (overwrite != null && overwrite is DenseVector<T>)
				return this.GetColumnAt(index, overwrite as DenseVector<T>);
			else
				return this.GetColumnAt(index, EmptySpVec);
		}

		/// <summary>
		/// Get one row of the matrix, from <see cref="IMatrix{TMat, TVec, T}.GetRowAt(Index, TVec)"/>.
		/// </summary>
		/// <param name="index">the <see cref="Index"/> of row</param>
		/// <param name="overwrite">the output <see cref="VectorBase{T}"/> to overwrite, default null means creating a ref vector if possible</param>
		/// <returns>The selected column as a <see cref="VectorBase{T}"/>. If <paramref name="overwrite"/> does not fit, it will not be returned.</returns>
		public override VectorBase<T> GetRowAt(Index index, VectorBase<T> overwrite = null)
		{
			if (overwrite != null && overwrite is SparseVector<T>)
				return this.GetRowAt(index, overwrite as SparseVector<T>);
			else if (overwrite != null && overwrite is DenseVector<T>)
				return this.GetRowAt(index, overwrite as DenseVector<T>);
			else
				return this.GetRowAt(index, EmptySpVec);
		}
		#endregion


		#region implement abstract operations
		/// <summary>
		/// Calculate the transpose of this matrix, override <see cref="MatrixBase{T}.Transpose(MatrixBase{T})"/>;
		/// </summary>
		/// <param name="overwrite">the output <see cref="MatrixBase{T}"/> to overwrite, default null means creating a new matrix</param>
		/// <returns>The transposed matrix out-of-place. If <paramref name="overwrite"/> does not fit, it will not be used.</returns>
		/// <remarks>Overwriting to <see cref="DenseMatrix{T}"/> is not supported yet.</remarks>
		public override MatrixBase<T> Transpose(MatrixBase<T> overwrite = null)
		{
			if (this.Hermitian && this.IsRealType)
				return this;
			if (overwrite is null || overwrite == EmptyDnMat)
				return this.Transpose(EmptySpMat);
			if (overwrite is SparseMatrix<T> sm)
				return this.Transpose(sm);
			// else
			return this.Transpose(EmptySpMat);
		}

		/// <summary>
		/// Calculate the conjugate transpose of this matrix, override <see cref="MatrixBase{T}.ConjugateTranspose(MatrixBase{T})"/>.
		/// </summary>
		/// <param name="overwrite">the output <see cref="MatrixBase{T}"/> to overwrite, default null means creating a new matrix</param>
		/// <returns>The conjugate transposed matrix out-of-place. If <paramref name="overwrite"/> does not fit, it will not be used.</returns>
		/// <remarks>Overwriting to <see cref="DenseMatrix{T}"/> is not supported yet.</remarks>
		public override MatrixBase<T> ConjugateTranspose(MatrixBase<T> overwrite = null)
		{
			if (this.Hermitian)
				return this;
			if (this.IsRealType)
				return this.Transpose(overwrite);

			if (overwrite is null || overwrite == EmptyDnMat)
				return this.ConjugateTranspose(EmptySpMat);
			if (overwrite is SparseMatrix<T> sm)
				return this.ConjugateTranspose(sm);
			// else
			return this.ConjugateTranspose(EmptySpMat);
		}

		/// <summary>
		/// Compute $\alpha A^{\text{opA}} + \beta B^{\text{opB}}$. Override <see cref="MatrixBase{T}.From_αA_Add_βB"/>.
		/// </summary>
		/// <param name="α">scalar of type <typeparamref name="T"/> with default 0. If <c><paramref name="α"/> == 0</c>, <paramref name="A"/> can be an invalid input</param>
		/// <param name="A">the <see cref="MatrixBase{T}"/> A, can be null</param>
		/// <param name="opA">operation to matrix <paramref name="A"/></param>
		/// <param name="β">scalar of type <typeparamref name="T"/> with default 0. If <c><paramref name="β"/> == 0</c>, <paramref name="B"/> can be an invalid input</param>
		/// <param name="B">the input <see cref="MatrixBase{T}"/> B, can be null</param>
		/// <param name="opB">operation to matrix <paramref name="B"/></param>
		/// <returns>The result <see cref="MatrixBase{T}"/> according to the in-place mode.</returns>
		/// <exception cref="ArgumentNullException">if all of the array are null</exception>
		/// <exception cref="ArgumentException">if the arrays do not match in size</exception>
		/// <exception cref="StatusException">if the internal returns error status</exception>
		/// <remarks>This operation cannot overwrite this matrix when both <paramref name="A"/> and <paramref name="B"/> are built-in matrix classes.</remarks>
		public override void From_αA_Add_βB(MatrixBase<T> A, MatrixBase<T> B, T α = default, T β = default, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			// Ignore Spelling: Dn
			bool zeroA = α.Equals(Scalars<T>.Zero) || A is null || A == EmptyDnMat;
			bool zeroB = β.Equals(Scalars<T>.Zero) || B is null || B == EmptyDnMat;
			if (zeroA && zeroB)
				throw new ArgumentException(Resource.ParaCannotZero);
			if (zeroA)
				A = EmptyDnMat;
			if (zeroB)
				B = EmptyDnMat;

			DenseMatrix<T> dA = A as DenseMatrix<T>, dB = B as DenseMatrix<T>;
			SparseMatrix<T> sA = A as SparseMatrix<T>, sB = B as SparseMatrix<T>;
			if (dA is null && sA is null)
			{
				From_αA_Add_βB_Opposite(this, B, α, β, opA, opB);
			}
			else if (dB is null && sB is null)
			{
				From_αA_Add_βB_Opposite(this, A, β, α, opB, opA);
			}

			if (zeroA) // cannot overwrite this matrix
			{
				if (dB != null)
					this.From_αA_Add_βB(EmptyDnMat, dB, β: β, opB: opB);
				else
					this.From_αA_Add_βB(EmptySpMat, sB, β: β, opB: opB);
			}
			else if (zeroB) // symmetric to zeroA: switch A & B
			{
				if (dA != null)
					this.From_αA_Add_βB(EmptyDnMat, dA, β: α, opB: opA);
				else
					this.From_αA_Add_βB(EmptySpMat, sA, β: α, opB: opA);
			}
			else // all non zero
			{
				if (dA != null && dB != null) // cannot overwrite this matrix
				{
					this.From_αA_Add_βB(dA, dB, α, β, opA, opB);
				}
				if (sA != null && sB != null) // still, cannot overwrite this matrix
				{
					this.From_αA_Add_βB(sA, sB, α, β, opA, opB);
				}
				else if (dA != null && sB != null) // Dn + Sp
				{
					this.From_αA_Add_βB(dA, sB, α, β, opA, opB);
				}
				else // Sp + Dn : swap A & B
				{
					this.From_αA_Add_βB(dB, sA, β, α, opB, opA);
				}
			}
		}

		/// <summary>
		/// DO NOT Call this method since <see cref="SparseMatrix{T}"/> have no need to implement it.
		/// </summary>
		protected internal override void From_αA_Add_βB_Opposite(MatrixBase<T> C, MatrixBase<T> B, T α = default, T β = default, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			throw new NotImplementedException(string.Format(Resource.Culture, Resource.BuiltInClassImp, nameof(SparseMatrix<T>)));
		}

		/// <summary>
		/// Compute $\alpha A^{\text{opA}} B^{\text{opB}} + \beta C_{\text{this}}$. Override <see cref="MatrixBase{T}.Mulβ_AddBy_αAB"/>.
		/// </summary>
		/// <param name="A">the input <see cref="MatrixBase{T}"/> A</param>
		/// <param name="opA">operation to matrix <paramref name="A"/></param>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		/// <param name="B">the input <see cref="MatrixBase{T}"/> B</param>
		/// <param name="opB">operation to matrix <paramref name="B"/></param>
		/// <param name="β">scalar of type <typeparamref name="T"/> with default 0</param>
		/// <returns>The result <see cref="MatrixBase{T}"/> according to the in-place mode. If <c><paramref name="β"/> == 0</c>, this matrix will be completely overridden.</returns>
		/// <exception cref="ArgumentNullException">if any of the array is null</exception>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="α"/> is zero</exception>
		/// <exception cref="ArgumentException">if the arrays do not match in size</exception>
		/// <exception cref="StatusException">if the internal calculation returns error status</exception>
		/// <remarks>This operation cannot overwrite this matrix when both <paramref name="A"/> and <paramref name="B"/> are built-in matrix classes.</remarks>
		public override void Mulβ_AddBy_αAB(MatrixBase<T> A, MatrixBase<T> B, T α, T β = default, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			if (α.Equals(Scalars<T>.Zero))
				throw new ArgumentOutOfRangeException(nameof(α), α, Resource.ParaCannotZero);
			DenseMatrix<T> dA = A as DenseMatrix<T>, dB = B as DenseMatrix<T>;
			SparseMatrix<T> sA = A as SparseMatrix<T>, sB = B as SparseMatrix<T>;
			if (dA is null && sA is null)
			{
				Mulβ_AddBy_αAB_Opposite(this, B, SideMode.Left, α, β, opA, opB);
			}
			if (dB is null && sB is null)
			{
				Mulβ_AddBy_αAB_Opposite(this, A, SideMode.Right, α, β, opA, opB);
			}
			if (dA != null && dB != null) // both dense
			{
				this.Mulβ_AddBy_αAB(dA, dB, α, β, opA, opB);
			}
			if (sA != null && sB != null) // both sparse
			{
				this.Mulβ_AddBy_αAB(sA, sB, α, β, opA, opB);
			}
			if (dA != null && sB != null) // Dn * Sp
			{
				this.Mulβ_AddBy_αAB(dA, sB, α, β, opA, opB);
			}
			else // Sp * Dn
			{
				this.Mulβ_AddBy_αAB(sA, dB, α, β, opA, opB);
			}
		}

		/// <summary>
		/// DO NOT Call this method since <see cref="SparseMatrix{T}"/> have no need to implement it.
		/// </summary>
		protected internal override void Mulβ_AddBy_αAB_Opposite(MatrixBase<T> C, MatrixBase<T> B, SideMode side, T α, T β = default, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			throw new NotImplementedException(string.Format(Resource.Culture, Resource.BuiltInClassImp, nameof(SparseMatrix<T>)));
		}

		/// <summary>
		/// DO NOT call this method since <see cref="DenseMatrix{T}"/> has no need to implement it.
		/// </summary>
		internal protected override VectorBase<T> Mulx_AddTo_y(VectorBase<T> x, VectorBase<T> y, T α, T β = default, MatrixOperation op = MatrixOperation.None)
		{
			throw new NotImplementedException(string.Format(Resource.Culture, Resource.BuiltInClassImp, nameof(SparseMatrix<T>)));
		}

		/// <summary>
		/// Compute Kronecker product $A \otimes B$ where $A$ is this matrix. If <paramref name="forceHerm"/> is true, then $(A \otimes B^H + A^H \otimes B)/2$ will be calculated. From <see cref="IMatrix{TMat, T}.KroneckerProd"/>.
		/// </summary>
		/// <param name="B">right <see cref="MatrixBase{T}"/></param>
		/// <param name="forceHerm">if the result is made Hermitian or not</param>
		/// <param name="overwrite">the <see cref="MatrixBase{T}"/> to overwrite by result, default null</param>
		/// <returns>The result of Kronecker product, a new <see cref="MatrixBase{T}"/> or <paramref name="overwrite"/> if it is not null.</returns>
		public override MatrixBase<T> KroneckerProd(MatrixBase<T> B, bool forceHerm = true, MatrixBase<T> overwrite = null)
		{
			if (B is null || B == EmptyDnMat)
				throw new ArgumentNullException(nameof(B), Resource.ArrayCannotNull);
			if (B.OnHost != this.OnHost)
				throw new ArgumentException(Resource.RequireSamePos, nameof(B));

			if (B is DenseMatrix<T> dB)
				return this.KroneckerProd(dB, forceHerm, overwrite);
			else if (B is SparseMatrix<T> sB)
				return this.KroneckerProd(sB, forceHerm, overwrite);
			else
			{
				var ddB = B.ToDense();
				try
				{
					return this.KroneckerProd(ddB, forceHerm, overwrite);
				}
				finally
				{
					if (ddB != B) ddB.Dispose();
				}
			}
		}

		/// <summary>
		/// Compute Kronecker sum $A \oplus B \equiv A \otimes I + I \otimes B$ where $A$ is this matrix. If <paramref name="forceHerm"/> is true, then $[(A \otimes I + I \otimes B^H) + (A^H \otimes I + I \otimes B)]/2$ will be calculated. From <see cref="IMatrix{TMat, T}.KroneckerSum"/>.
		/// </summary>
		/// <param name="B">right <see cref="DenseMatrix{T}"/></param>
		/// <param name="forceHerm">if the result is made Hermitian or not</param>
		/// <param name="overwrite">the <see cref="MatrixBase{T}"/> to overwrite by result, default null</param>
		/// <returns>The result of Kronecker product, a new <see cref="MatrixBase{T}"/> or <paramref name="overwrite"/> if it is not null.</returns>
		public override MatrixBase<T> KroneckerSum(MatrixBase<T> B, bool forceHerm = true, MatrixBase<T> overwrite = null)
		{
			if (B is null || B == EmptyDnMat)
				throw new ArgumentNullException(nameof(B), Resource.ArrayCannotNull);
			if (this.NRows != this.NCols || B.NRows != B.NCols)
				throw new ArgumentException(Resource.MatMustSquare);
			if (B.OnHost != this.OnHost)
				throw new ArgumentException(Resource.RequireSamePos, nameof(B));

			if (B is DenseMatrix<T> dB)
				return this.KroneckerSum(dB, forceHerm, overwrite);
			else if (B is SparseMatrix<T> sB)
				return this.KroneckerSum(sB, forceHerm, overwrite);
			else
			{
				var ddB = B.ToDense();
				try
				{
					return this.KroneckerSum(ddB, forceHerm, overwrite);
				}
				finally
				{
					if (ddB != B) ddB.Dispose();
				}
			}
		}
		#endregion


		#region implement indexers
#pragma warning disable IDE0069 // will be released by Dispose() by the method that calls it
		private Storage<int> vecIndex = null;
#pragma warning restore IDE0069

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private int FindIndex(long px, long py)
		{
			int x = checked((int)px), y = checked((int)py);
			int findx, findy, lowerBound;
			switch (this.Format)
			{
				case SparseMatrixFormat.COOC:
				case SparseMatrixFormat.COOR:
					if (vecIndex is null)
					{
						this.vecIndex = Storage<int>.Create(this.NonZero, this.OnHost);
						var temp = new SparseVector<T>(this.Length, this.Pointer + 0, this.vecIndex + 0);
						Sparse.VectorToFromCOOMatrix(temp, this, toCOO: false);
					}
					int find = Sparse.IndexFind(this.vecIndex, this.NonZero, this.Format == SparseMatrixFormat.COOC ? checked((int)(py * this.NRows + px)) : checked((int)(px * this.NCols + py)));
					return find;
				case SparseMatrixFormat.CSR:
					findy = Sparse.IndexFind(this.ColumnPointer, this.IntColIdxLength, y);
					if (findy < 0)
						return -1;
					lowerBound = Sparse.IndexLowerUpperBound(this.RowPointer, this.IntRowIdxLength, findy, lowerBound: true);
					if (lowerBound != findy)
						return -1;
					return findy;
				case SparseMatrixFormat.CSC:
					findx = Sparse.IndexFind(this.RowPointer, this.IntRowIdxLength, x);
					if (findx < 0)
						return -1;
					lowerBound = Sparse.IndexLowerUpperBound(this.ColumnPointer, this.IntColIdxLength, findx, lowerBound: true);
					if (lowerBound != findx)
						return -1;
					return findx;
				default:
					throw new NotSupportedException(Resource.FormatNotSupport);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private int FindIndex(Index row, Index column)
		{
			var (px, py) = CheckRange(row, column);
			return this.FindIndex(px, py);
		}

		// setter maintain the hermitian
		private T this[long row, long column] {
			set {
				var find = this.FindIndex(row, column);
				if (find >= 0)
				{
					RT.CopyInto(this, value, offset: find);
					if (this.Hermitian)
					{
						if (!row.Equals(column))
						{
							find = this.FindIndex(column, row);
							if (find >= 0)
								RT.CopyInto(this, value, offset: find);
							else
								throw new AccessViolationException(Resource.InsertSparse);
						}
					}
				}
				else
					throw new AccessViolationException(Resource.InsertSparse);
			}
		}

		/// <summary>
		/// Basic indexer of matrix.
		/// </summary>
		/// <param name="row">row position in <see cref="Index"/> form</param>
		/// <param name="column">column position in <see cref="Index"/> form</param>
		/// <returns>Element at position (<paramref name="row"/>, <paramref name="column"/>)</returns>
		/// <exception cref="AccessViolationException">if you are trying to insert into this sparse array</exception>
		/// <remarks>Since a value cannot hold reference, altering the retrieved value does not change this array's value at that position.</remarks>
		public override T this[Index row, Index column] {
			get {
				var find = this.FindIndex(row, column);
				if (find >= 0)
					return RT.CopyOut(this, offset: find);
				else
					return default;
			}
			set {
				var (r, c) = CheckRange(row, column);
				this[r, c] = value;
			}
		}

		/// <summary>
		/// Range indexer of matrix.
		/// </summary>
		/// <param name="row">range of rows in <see cref="Range"/> form, end is exclusive</param>
		/// <param name="column">range of columns in <see cref="Range"/> form, end is exclusive</param>
		/// <returns>A copied sub-matrix in this range</returns>
		/// <remarks>See <see cref="Index"/> and <see cref="Range"/> for more information.</remarks>
		public override MatrixBase<T> this[Range row, Range column] {
			get {
				return this.GetSubmatrix(row, column, EmptySpMat);
			}
			set {
				throw new NotSupportedException("Setting matrix by a sub-matrix" + Resource.BaseNotSupport);
			}
		}

		private VectorBase<T> this[long[] rowPos, long[] colPos] {
			get {
				var vals = new List<T>();
				var inds = new List<int>();
				try
				{
					for (int i = 0; i < rowPos.Length; i++)
					{
						int find = this.FindIndex(rowPos[i], colPos[i]);
						if (find >= 0)
						{
							vals.Add(RT.CopyOut(this, offset: find));
							inds.Add(find);
						}
					}
					return (SparseVector<T>)(vals.ToArray(), inds.ToArray(), rowPos.LongLength, this.OnHost);
				}
				finally
				{
					this.vecIndex?.Dispose();
					this.vecIndex = null;
				}
			}
			set {
				var vv = value;
				if (vv.Length == 1)
				{
					T v = RT.CopyOut(vv);
					for (int i = 0; i < rowPos.Length; i++)
					{
						this[rowPos[i], colPos[i]] = v;
					}
				}
				else if (vv.Length >= rowPos.Length)
				{
					T[] v = RT.CopyOutArray(vv);
					for (int i = 0; i < rowPos.Length; i++)
					{
						this[rowPos[i], colPos[i]] = v[i];
					}
				}
				else
				{
					throw new ArgumentException(Resource.VectorTooShort, nameof(value));
				}
			}
		}

		/// <summary>
		/// Multiple element indexer of matrix.
		/// </summary>
		/// <param name="indices">row and column positions in <see cref="Index"/> array form</param>
		/// <returns>Elements at these positions copied into a new <see cref="DenseVector{T}"/></returns>
		/// <remarks>
		/// The input value array of setter is only used at certain positions corresponding the <paramref name="indices"/>.<br/>
		/// This indexer is implemented by utilizing <see cref="this[Index, Index]"/> which may be quite slow.<br/>
		/// The input value array of setter will be first regarded as a <see cref="DenseVector{T}"/> by <see cref="ValueArray{T}.AsDenseVector"/>
		/// </remarks>
		public override VectorBase<T> this[params (Index x, Index y)[] indices] {
			get {
				var (rowPos, colPos) = CheckRange(indices);
				return this[rowPos, colPos];
			}
			set {
				var (rowPos, colPos) = CheckRange(indices);
				this[rowPos, colPos] = value;
			}
		}
		#endregion


		#region implement diag indexer
		private (long[] rowPos, long[] colPos) GenerateDiagRange(long k)
		{
			IReadOnlyList<long> rowPos, colPos;
			if (k <= 0)
			{
				rowPos = ArrayLinq.Range(Math.Abs(k), this.NRows + k, step: 1);
				colPos = ArrayLinq.Range(0, this.NRows + k, step: 1);
			}
			else
			{
				rowPos = ArrayLinq.Range(0, this.NRows - k, step: 1);
				colPos = ArrayLinq.Range(k, this.NRows - k, step: 1);
			}
			return (rowPos.ToArray(), colPos.ToArray());
		}

		/// <summary>
		/// The method to get diagonal elements, override <see cref="MatrixBase{T}.GetDiag(long, VectorBase{T})"/>.
		/// </summary>
		/// <param name="k">diagonal index, 0 for diag, 1 for super-diagonal at one above, -1 for sub-diagonal at one below, etc.</param>
		/// <param name="overwrite">the output <see cref="VectorBase{T}"/> to overwrite, default null means creating a new vector</param>
		/// <returns>A new <see cref="VectorBase{T}"/> containing the (super-/sub-)diagonal elements. The <paramref name="overwrite"/> is never used.</returns>
		/// <exception cref="InvalidOperationException">if this matrix is not square</exception>
		public override VectorBase<T> GetDiag(long k, VectorBase<T> overwrite = null)
		{
			if (this.NRows != this.NCols)
				throw new InvalidOperationException(Resource.MatMustSquare);
			Log.Write("Getting or setting diagonal elements of a sparse matrix is not a good choice.", level: LogLevel.Warning);
			var (rowPos, colPos) = this.GenerateDiagRange(k);
			return this[rowPos, colPos];
		}

		/// <summary>
		/// The method to set diagonal elements, override <see cref="MatrixBase{T}.SetDiag(long, VectorBase{T})"/>.
		/// </summary>
		/// <param name="k">diagonal index, 0 for diag, 1 for super-diagonal at one above, -1 for sub-diagonal at one below, etc.</param>
		/// <param name="vec">the <see cref="VectorBase{T}"/> </param>
		/// <exception cref="InvalidOperationException">if this matrix is not square</exception>
		public override void SetDiag(long k, VectorBase<T> vec)
		{
			if (this.NRows != this.NCols)
				throw new InvalidOperationException(Resource.MatMustSquare);
			Log.Write("Getting or setting diagonal elements of a sparse matrix is not a good choice.", level: LogLevel.Warning);
			var (rowPos, colPos) = this.GenerateDiagRange(k);
			this[rowPos, colPos] = vec;
		}
		#endregion


		#region managed converter
		/// <summary>
		/// Initialize from managed C# two-dimensional array to a sparse matrix of <see cref="SparseMatrixFormat.COOR"/>.
		/// </summary>
		/// <param name="input">the C# multidimensional array of type <typeparamref name="T"/> and on host indicator</param>
		public static explicit operator SparseMatrix<T>((T[,] value, bool onHost) input) => (SparseMatrix<T>)(input.value, default(T), input.onHost);

		/// <summary>
		/// Initialize from managed C# two-dimensional array to a sparse matrix of <see cref="SparseMatrixFormat.COOR"/> with thresholds.
		/// </summary>
		/// <param name="input">the C# multidimensional array of type <typeparamref name="T"/>, the threshold and the on host indicator</param>
		public static explicit operator SparseMatrix<T>((T[,] value, T threshold, bool onHost) input)
		{
			var (value, threshold, onHost) = input;
			List<int> indRow = new List<int>(), indCol = new List<int>();
			List<T> val = new List<T>();
			void AddList(T v, int i, int j)
			{
				if (v.CompareTo(threshold) > 0)
				{
					val.Add(v);
					indRow.Add(i);
					indCol.Add(j);
				}
			}
			value.ForEach(AddList);
			var (rows, cols) = value.GetRowColumns();
			var mat = new SparseMatrix<T>(rows, cols, val.Count, SparseMatrixFormat.COOR, onHost, herm: value.IsHermitian());
			try
			{
				RT.CopyIntoArray(mat, val.ToArray());
				RT.CopyIntoArray(mat.RowPointer, indRow.ToArray());
				RT.CopyIntoArray(mat.ColumnPointer, indCol.ToArray());
				return mat;
			}
			catch (Exception)
			{
				mat.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Initialize from C# one-dimensional value and index arrays.
		/// </summary>
		/// <param name="input">the 1D value array of type <typeparamref name="T"/>, the 1D row index array, the 1D column index array, the <see cref="SparseMatrixFormat"/> and the on host indicator</param>
		public static explicit operator SparseMatrix<T>((T[] value, int[] row, int[] col, int rows, int cols, SparseMatrixFormat format, bool onHost) input)
		{
			var (value, row, col, rows, cols, format, onHost) = input;
			int nnz = value.Length;
			switch (format)
			{
				case SparseMatrixFormat.COOR:
				case SparseMatrixFormat.COOC:
					if (row.Length != col.Length || row.Length != nnz)
						throw new ArgumentException(Resource.VectorWrongSize);;
					break;
				case SparseMatrixFormat.CSR:
					if (row.Length != rows + 1 || col.Length != nnz)
						throw new ArgumentException(Resource.VectorWrongSize);
					break;
				case SparseMatrixFormat.CSC:
					if (col.Length != cols + 1 || row.Length != nnz)
						throw new ArgumentException(Resource.VectorWrongSize);
					break;
				default:
					throw new NotSupportedException(Resource.FormatNotSupport);
			}
			var mat = new SparseMatrix<T>(rows, cols, nnz, format, onHost, herm: false);
			try
			{
				RT.CopyIntoArray(mat, value);
				RT.CopyIntoArray(mat.RowPointer, row);
				RT.CopyIntoArray(mat.ColumnPointer, col);
				return mat;
			}
			catch (Exception)
			{
				mat.Dispose();
				throw;
			}
		}
		#endregion


		#region equality
		/// <summary>
		/// Check if this <see cref="ValueArray{T}"/> share some memory / data with <paramref name="another"/> one
		/// </summary>
		/// <param name="another">another <see cref="AbstractArray{T}"/> to check</param>
		/// <returns>True if they do share some memory / data, false otherwise</returns>
		public override bool ShareMemoryWith(AbstractArray<T> another)
		{
			if (base.ShareMemoryWith(another))
				return true;
			else if (another is SparseVector<T> sv)
			{
				return this.RowPointer.ShareMemoryWith(sv.IndexPointer) || this.ColumnPointer.ShareMemoryWith(sv.IndexPointer);
			}
			else if (another is SparseMatrix<T> sm)
			{
				return	this.RowPointer.ShareMemoryWith(sm.RowPointer) || this.ColumnPointer.ShareMemoryWith(sm.ColumnPointer) ||
						this.ColumnPointer.ShareMemoryWith(sm.RowPointer) || this.RowPointer.ShareMemoryWith(sm.ColumnPointer);
			}
			else
				return false;
		}

		/// <summary>
		/// Override <see cref="ValueArray{T}.GetHashCode"/> to get the hash code this array.
		/// </summary>
		/// <returns>The hash code</returns>
		public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), this.RowPointer, this.ColumnPointer);

		/// <summary>
		/// Whether this array is equal to another one, override <see cref="AbstractArray{T}.Equals(object)"/>
		/// </summary>
		/// <param name="obj">another <see cref="SparseVector{T}"/></param>
		public override bool Equals(object obj)
		{
			if (obj is null || !(obj is ValueArray<T> a))
				return false;
			else if (this.Pointer != a.Pointer)
				return false;
			if (obj is SparseMatrix<T> s)
				return this.Format == s.Format && this.NonZero == s.NonZero &&
						this.RowPointer == s.RowPointer && this.ColumnPointer == s.ColumnPointer;
			return false;
		}
		#endregion


		#region print
		/// <summary>
		/// Override <see cref="ValueArray{T}.ToString()"/> to get the string representation of this array.
		/// </summary>
		/// <returns>String representation of this array</returns>
		public override string ToString()
		{
			return base.ToString(new Dictionary<string, object>
			{ 
				["value_address"] = $"0x{this.Pointer.ToHexString()}",
				["row_address"] = $"0x{this.RowPointer.ToHexString()}",
				["column_address"] = $"0x{this.ColumnPointer.ToHexString()}",
				["non_zeros"] = this.NonZero,
				["format"] = this.Format,
			}, new[] { StringTerms.DataType, StringTerms.Size});
		}

		private (int[] indrow, int[] indcol, T[] values) Raw(IReadOnlyDictionary<PrintSetting, int> config = null)
		{
			config ??= GlobalSettings.PrintConfig;
			long length = Math.Min(config[PrintSetting.ArrayLength], this.NonZero);
			T[] res = RT.CopyOutArray(this.Pointer, length);
			int[] rowInd, colInd;
			switch (this.Format)
			{
				case SparseMatrixFormat.COOR:
				case SparseMatrixFormat.COOC:
					rowInd = RT.CopyOutArray(this.RowPointer, length);
					colInd = RT.CopyOutArray(this.ColumnPointer, length);
					break;
				case SparseMatrixFormat.CSR:
				case SparseMatrixFormat.CSC:
					using (var coo = this.ToFormat(SparseMatrixFormat.Coordinated))
					{
						rowInd = RT.CopyOutArray(coo.RowPointer, length);
						colInd = RT.CopyOutArray(coo.ColumnPointer, length);
					}
					break;
				default:
					throw new NotSupportedException(Resource.FormatNotSupport);
			}
			return (rowInd, colInd, res);
		}

		/// <summary>
		/// Override <see cref="AbstractArray{T}.Print"/> to show detail.
		/// </summary>
		/// <param name="overrideSetting">See <see cref="AbstractArray{T}.Print"/></param>
		/// <returns>The string representation</returns>
		public override string Print(IReadOnlyDictionary<PrintSetting, int> overrideSetting = null)
		{
			string description = ToString();
			if (this.Disposed)
				return description;

			var printConfig = new Dictionary<PrintSetting, int>(GlobalSettings.PrintConfig);
			if (overrideSetting != null)
			{
				if (overrideSetting.ContainsKey(PrintSetting.ArrayLength))
					printConfig[PrintSetting.ArrayLength] = overrideSetting[PrintSetting.ArrayLength];
				if (overrideSetting.ContainsKey(PrintSetting.Precision))
					printConfig[PrintSetting.Precision] = overrideSetting[PrintSetting.Precision];
			}

			string detail = ":" + Environment.NewLine;
			var (indx, indy, res) = this.Raw(printConfig);
			detail += res.ToSparseMatrixString(indx, indy, precision: printConfig[PrintSetting.Precision]);
			if (this.NonZero > res.Length)
				detail += Environment.NewLine + $"...{this.NonZero - res.Length} more non-zero elements";

			return description + detail;
		}
		#endregion


		#region serialize
		/// <summary>
		/// Get the pointers of this instance.
		/// </summary>
		/// <returns>the pointers</returns>
		public override IReadOnlyDictionary<string, IStorage> GetPointers() => SparseMatrixFactory.GetPointers(this);

		/// <summary>
		/// Get other requisite informations for re-constructing this array.
		/// </summary>
		/// <returns>other requisite informations</returns>
		public override IReadOnlyDictionary<string, object> GetOtherInfo() => SparseMatrixFactory.GetOtherInfo(this);
		#endregion
	}
}
