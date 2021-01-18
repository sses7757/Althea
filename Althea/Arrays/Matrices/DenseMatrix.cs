using System;
using System.Collections.Generic;

using Althea.Linq;
using Althea.Storage;
using RT = Althea.Runtime.API;
using BLAS = Althea.Blas.API;
using SOLVER = Althea.Solver.API;
using Sparse = Althea.SparseBlas.API;


namespace Althea.Arrays
{

	/// <summary>
	/// The dense matrix class that inherit the <see cref="MatrixBase{T}"/> and implements <see cref="IDenseArray{T}"/>.
	/// </summary>
	/// <remarks>Matrices are stored in the column-major format in the memory. This is often known as the <c>Fortran</c> or the <c>MATLAB</c> way.</remarks>
	/// <typeparam name="T">the supported data types are <see cref="float"/>, <see cref="double"/>, <see cref="FloatComplex"/>, <see cref="DoubleComplex"/>; other types of data causes <see cref="NotSupportedException"/></typeparam>
	public sealed class DenseMatrix<T> : MatrixBase<T>, IDenseArray<T>, IMatrix<DenseMatrix<T>, DenseVector<T>, T>, IDecomposable<DenseMatrix<T>, DenseVector<T>, T> where T : struct, IComparable<T>
	{
		#region initialize and destroy
		/// <summary>
		/// The leading dimension of this matrix, i.e. the actual number of row in memory
		/// </summary>
		public long LeadDim { get; }

		internal int IntLeadDim => checked((int)this.LeadDim);

		/// <summary>
		/// Empty constructor
		/// </summary>
		public DenseMatrix() : this(0, 0, onHost: false) { }

		/// <summary>
		/// Matrix constructor
		/// </summary>
		/// <param name="rows">the number of rows</param>
		/// <param name="cols">the number of columns</param>
		/// <param name="herm">is the matrix hermitian or not</param>
		/// <param name="onHost">allocate on host memory or device memory</param>
		public DenseMatrix(long rows, long cols, bool onHost = false, bool herm = false) : base(rows * cols, rows, cols, onHost, herm)
		{
			this.LeadDim = rows;
		}

		/// <summary>
		/// Matrix deep clone constructor
		/// </summary>
		/// <param name="matrix">original matrix</param>
		public DenseMatrix(DenseMatrix<T> matrix) : this(matrix != null ? matrix.NRows : throw new ArgumentNullException(nameof(matrix), Resource.ArrayCannotNull), matrix.NCols, onHost: matrix.OnHost, herm: matrix.Hermitian)
		{
			this.LeadDim = matrix.NRows;
			RT.CopyMatrixTo(source: matrix, dest: this, copyNRows: matrix.NRows, copyNCols: matrix.NCols);
		}

		/// <summary>
		/// Matrix full constructor with pre-allocated values.
		/// </summary>
		/// <param name="values">the pre-allocated values <see cref="Storage{T}"/></param>
		/// <param name="rows">number of rows</param>
		/// <param name="cols">number of columns</param>
		/// <param name="ld">leading dimension; if <paramref name="ld"/> ≤ 0, it will be set to <paramref name="rows"/></param>
		/// <param name="herm">the new matrix is Hermitian or not</param>
		/// <param name="offset">offset to the <paramref name="values"/>, in <typeparamref name="T"/> rather than bytes</param>
		public DenseMatrix(Storage<T> values, long rows, long cols, long ld = 0, bool herm = false, long offset = 0) : base(values + offset, rows, cols, herm: herm)
		{
			if (ld <= 0)
				ld = rows;
			this.LeadDim = ld;
		}

		/// <summary>
		/// Matrix reshape constructor
		/// </summary>
		/// <param name="refArray">original array</param>
		/// <param name="newRows">new number of rows</param>
		/// <param name="newCols">new number of columns</param>
		/// <param name="newLD">new leading dimension, less than or equal to 0 means that it is equal to <paramref name="newRows"/></param>
		/// <param name="herm">the new matrix is Hermitian or not, if <paramref name="refArray"/> is <see cref="MatrixBase{T}"/>, its <see cref="MatrixBase{T}.Hermitian"/> will be used</param>
		/// <param name="offset">offset to the <see cref="ValueArray{T}.Pointer"/> in <typeparamref name="T"/> rather than bytes</param>
		public DenseMatrix(ValueArray<T> refArray, long newRows, long newCols, long newLD = 0, bool herm = false, long offset = 0) : base(refArray, (newLD > 0 ? newLD : refArray is DenseMatrix<T> m ? m.LeadDim : newRows) * newCols, newRows, newCols, herm, offset)
		{
			if (newLD <= 0)
			{
				if (refArray is DenseMatrix<T> mm)
					newLD = mm.LeadDim;
				else
					newLD = newRows;
			}
			this.LeadDim = newLD;
		}
		#endregion


		#region reshape
		/// <summary>
		/// Reshape the array to a <see cref="MatrixBase{T}"/> with leading dimension = leadDim. Override <see cref="ValueArray{T}.ToMatrix(long)"/>.
		/// </summary>
		/// <param name="leadDim">leading dimension of target matrix</param>
		/// <returns>If <paramref name="leadDim"/> &lt;= 0 or <paramref name="leadDim"/> == <see cref="MatrixBase{T}.NRows"/>, the matrix itself is returned</returns>
		public override ValueArray<T> ToMatrix(long leadDim = 0)
		{
			if (this.LeadDim == leadDim || leadDim <= 0)
				return this;
			if (this.LeadDim == this.NRows)
				return base.ToMatrix(leadDim);
			var size = this.CheckSize(new[] { leadDim });
			return new DenseMatrix<T>(this, size[0], size[1]);
		}
		#endregion


		#region dense array interface
		/// <summary>
		/// Convert the values of this matrix to a C# array column-major array.
		/// </summary>
		/// <param name="ranges">the indicating row and column ranges, default is all</param>
		/// <returns>C# array of type <typeparamref name="T"/> containing the values of this matrix</returns>
		public T[] ToFortranOrderArray(params Range[] ranges)
		{
			if (ranges is null || ranges.Length == 0)
				ranges = new[] { Range.All, Range.All };
			if (ranges.Length != 2)
				throw new ArgumentException(Resource.VectorWrongSize, nameof(ranges));
			var (offsetRow, countRow, offsetCol, countCol) = this.CheckRange(ranges[0], ranges[1]);
			return RT.CopyOutColumnMajorMatrix(this, rows: countRow, cols: countCol, offset: this.LeadDim * offsetCol + offsetRow);
		}

		/// <summary>
		/// Copy the <paramref name="values"/> of Fortran/MATLAB order into this dense matrix.
		/// </summary>
		/// <param name="values">the value array of element type <typeparamref name="T"/></param>
		/// <param name="ranges">the ranges of each dimension, default is all</param>
		public void FromFortranOrderArray(T[] values, params Range[] ranges)
		{
			if (ranges is null || ranges.Length == 0)
				ranges = new[] { Range.All, Range.All };
			if (ranges.Length != 2)
				throw new ArgumentException(Resource.VectorWrongSize, nameof(ranges));
			var (offsetRow, countRow, offsetCol, countCol) = this.CheckRange(ranges[0], ranges[1]);
			RT.CopyIntoColumnMajorMatrix(this, values, copyCols: countCol, copyRows: countRow, offsetDest: offsetRow + offsetCol * this.LeadDim);
		}
		#endregion


		#region implement converter
		private DenseMatrix<T> HostDeviceConvert()
		{
			var newMat = new DenseMatrix<T>(this.NRows, this.NCols, !this.OnHost, this.Hermitian);
			try
			{
				RT.CopyMatrixTo(source: this, dest: newMat, copyNRows: this.NRows, copyNCols: this.NCols);
				return newMat;
			}
			catch (Exception)
			{
				newMat.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Convert this array to the other memory.
		/// </summary>
		/// <returns>a new <see cref="ValueArray{T}"/> with same value as this one</returns>
		public override ValueArray<T> ToTheOtherMemory()
		{
			var newMat = new DenseMatrix<T>(this.NRows, this.NCols, !this.OnHost, this.Hermitian);
			try
			{
				RT.CopyMatrixTo(source: this, dest: newMat, copyNRows: this.NRows, copyNCols: this.NCols);
				return newMat;
			}
			catch (Exception)
			{
				newMat.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Convert this matrix to a <see cref="DenseMatrix{T}"/>. Override <see cref="MatrixBase{T}.ToDense"/>.
		/// </summary>
		/// <returns>This matrix</returns>
		public override DenseMatrix<T> ToDense(SparseMatrixToDenseAlgorithm algorithm = default) => this;

		/// <summary>
		/// Convert this matrix to a <see cref="SparseMatrix{T}"/>. The out-of-place conversion may be performed.
		/// </summary>
		/// <param name="threshold">values smaller than threshold are regarded as zeros, must be larger than or equal to 0</param>
		/// <param name="targetFormat">the target <see cref="SparseMatrix{T}"/>'s format, see <see cref="SparseMatrixFormat"/></param>
		/// <param name="algorithm">the <see cref="DenseMatrixToSparseAlgorithm"/> to use, default is null which means that the default algorithms corresponding to the <paramref name="targetFormat"/> and <typeparamref name="T"/> will be used</param>
		/// <returns>Converted <see cref="SparseMatrix{T}"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="threshold"/> &lt; 0 or the <paramref name="algorithm"/> is incompatible with <typeparamref name="T"/></exception>
		public override SparseMatrix<T> ToSparse(float threshold = default, SparseMatrixFormat targetFormat = SparseMatrixFormat.Any, DenseMatrixToSparseAlgorithm? algorithm = null)
		{
			if (threshold < 0)
				throw new ArgumentOutOfRangeException(nameof(threshold), threshold, Resource.ParaCannotNegative);
			// set defaults
			if (threshold == 0)
				algorithm ??= DenseMatrixToSparseAlgorithm.NonzeroThresholdDefault;
			else if (this.IsRealType)
				algorithm ??= DenseMatrixToSparseAlgorithm.RealDefault;
			else
				algorithm ??= DenseMatrixToSparseAlgorithm.NonzeroThresholdDefault;
			// check algorithm
			if (!this.IsRealType && algorithm.Value == DenseMatrixToSparseAlgorithm.RealDefault)
				throw new ArgumentOutOfRangeException(nameof(algorithm));

			// perform conversion
			SparseMatrix<T> mat = null;
			try
			{
				switch (algorithm.Value)
				{
					case DenseMatrixToSparseAlgorithm.ZeroThresholdDefault:
						// the COO format is converted to at the end, if target is any or compressed, CSR is the default
						mat = (targetFormat & SparseMatrixFormat.ColumnMajor) != 0 ? Sparse.MatrixDenseToSparseCSC(this) : Sparse.MatrixDenseToSparseCSR(this);
						if (threshold == 0) // fast return
							break;
						// otherwise
						var pruneMat = Sparse.MatrixCompressedPrune(mat, threshold);
						if (pruneMat != mat) mat.Dispose();
						mat = pruneMat;
						break;
					case DenseMatrixToSparseAlgorithm.RealDefault:
						if (targetFormat == SparseMatrixFormat.CSC)
						{
							using var temp = this.Transpose();
							mat = Sparse.MatrixDensePruneToCSR(temp, threshold); // direct prune to CSR
							mat.Format = SparseMatrixFormat.CSC; // set format as CSC
						}
						else
						{
							mat = Sparse.MatrixDensePruneToCSR(this, threshold); // direct prune to CSR
						}
						break;
					case DenseMatrixToSparseAlgorithm.NonzeroThresholdDefault:
						if (threshold == 0) // do zero threshold
						{
							// the COO format is converted to at the end, if target is any or compressed, CSR is the default
							mat = (targetFormat & SparseMatrixFormat.ColumnMajor) != 0 ? Sparse.MatrixDenseToSparseCSC(this) : Sparse.MatrixDenseToSparseCSR(this);
						}
						else
						{
							// truncate first
							using var temp = this.Clone() as DenseMatrix<T>;
							BLAS.Truncate(temp, threshold);
							// the COO format is converted to at the end, if target is any or compressed, CSR is the default
							mat = (targetFormat & SparseMatrixFormat.ColumnMajor) != 0 ? Sparse.MatrixDenseToSparseCSC(temp) : Sparse.MatrixDenseToSparseCSR(temp);
						}
						break;
					case DenseMatrixToSparseAlgorithm.ViaVector:
						var vec = this.ToVector() as DenseVector<T>;            // to dense vector
						var spVec = vec.ToSparse(threshold);                    // to sparse vector
						mat = spVec.ToMatrix(this.NRows) as SparseMatrix<T>;  // to COO matrix
						break;
					default:
						throw new NotSupportedException($"The algorithm {algorithm.Value}" + Resource.BaseNotSupport);
				}

				// to target format at last
				return mat.ToFormat(targetFormat, disposeThis: true);
			}
			catch (Exception)
			{
				mat?.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Create a new array with same immutable properties as this one, the mutable status will not be copied.
		/// </summary>
		/// <returns>The array alike this one.</returns>
		public override AbstractArray<T> NewArrayAlike() => new DenseMatrix<T>(this.NRows, this.NCols, this.OnHost, this.Hermitian);

		/// <summary>
		/// Take out the data array as a new <see cref="DenseVector{T}"/>, override <see cref="ValueArray{T}.AsDenseVector"/>.
		/// </summary>
		/// <returns>A new <see cref="DenseVector{T}"/> containing the referenced data array of this one.</returns>
		public override DenseVector<T> AsDenseVector() => this.ToVector() as DenseVector<T>;

		/// <summary>
		/// Create a new array like this one (with same type and other info) while the data type is <typeparamref name="TOut"/>
		/// </summary>
		/// <typeparam name="TOut">the new data type</typeparam>
		/// <returns>the new array</returns>
		public override ValueArray<TOut> NewArrayAlike<TOut>() => new DenseMatrix<TOut>(this.NRows, this.NCols, this.OnHost, this.Hermitian);
		#endregion


		#region checkers
		private bool CanOverwrite(MatrixBase<T> overwrite, long rows = 0, long cols = 0, long ld = 0)
		{
			if (overwrite is null || overwrite == EmptyDnMat)
				return false;
			if (rows == 0) rows = overwrite.NRows;
			if (cols == 0) cols = overwrite.NCols;
			if (ld == 0 && overwrite is DenseMatrix<T> dn) ld = dn.LeadDim;
			return overwrite is DenseMatrix<T> dd && dd.OnHost == this.OnHost && dd.NRows == rows && dd.NCols == cols && dd.LeadDim == ld;
		}

		private bool CanOverwrite(VectorBase<T> overwrite, long len = 0)
		{
			if (overwrite is null || overwrite == EmptyDnVec)
				return false;
			if (len == 0) len = overwrite.Length;
			return overwrite is DenseVector<T> dd && dd.OnHost == this.OnHost && dd.Length == len;
		}

		private bool CanOverwrite<TOther>(VectorBase<TOther> overwrite, long len = 0) where TOther : struct, IComparable<TOther>
		{
			if (overwrite is null || overwrite == ValueArray<TOther>.EmptyDnVec)
				return false;
			if (len == 0) len = overwrite.Length;
			return overwrite is DenseVector<TOther> dd && dd.OnHost == this.OnHost && dd.Length == len;
		}

		private bool CanOverwrite(VectorBase<T>[] overwrite, long count = 0, long len = 0, long nnz = 0)
		{
			if (overwrite is null || overwrite.LongLength == 0)
				return false;
			if (count == 0) count = overwrite.LongLength;
			if (len == 0) len = overwrite[0].Length;
			if (nnz == 0 && overwrite[0] is SparseVector<T> sp) nnz = sp.NonZero;
			return overwrite.LongLength == count && overwrite.All(o => o is DenseVector<T> dd && dd.OnHost == this.OnHost && dd.Length == len);
		}
		#endregion


		#region dense matrix dense vector restricted

		#region other methods
		/// <summary>
		/// Join the array of <see cref="DenseVector{T}"/> as columns of this matrix. From <see cref="IMatrix{TMat, TVec, T}.FromColumnVectors"/>.
		/// </summary>
		/// <param name="vecs">the input array of <see cref="DenseVector{T}"/></param>
		public void FromColumnVectors(DenseVector<T>[] vecs)
		{
			if (vecs is null)
				throw new ArgumentNullException(nameof(vecs));
			if (vecs.LongLength <= 1)
				throw new ArgumentException(Resource.VectorTooShort, nameof(vecs));
			if (!vecs.All(e => e != null && e != EmptyDnVec && e.Length == vecs[0].Length))
				throw new ArgumentException(Resource.CannotOperate, nameof(vecs));
			if (this.NRows != vecs[0].Length || this.NCols != vecs.LongLength)
				throw new ArgumentException(Resource.VectorWrongSize, nameof(vecs));

			for (long i = 0; i < vecs.LongLength; i++)
			{
				RT.CopyTo(source: vecs[i], dest: this, length: vecs[i].Length, offsetDest: this.LeadDim * i);
			}
		}

		/// <summary>
		/// Join the array of <see cref="DenseVector{T}"/> as rows of this matrix.
		/// </summary>
		/// <param name="vecs">the input array of <see cref="DenseVector{T}"/></param>
		public void FromRowVectors(DenseVector<T>[] vecs)
		{
			if (vecs is null)
				throw new ArgumentNullException(nameof(vecs));
			if (vecs.LongLength <= 1)
				throw new ArgumentException(Resource.VectorTooShort, nameof(vecs));
			if (!vecs.All(e => e != null && e != EmptyDnVec && e.Length == vecs[0].Length))
				throw new ArgumentException(Resource.CannotOperate, nameof(vecs));
			if (this.NCols != vecs[0].Length || this.NRows != vecs.LongLength)
				throw new ArgumentException(Resource.VectorWrongSize, nameof(vecs));

			for (long i = 0; i < vecs.LongLength; i++)
			{
				BLAS.VectorGenralCopy(dst: this, src: vecs[i], count: vecs[i].Length, strideDst: this.LeadDim + 1, offsetDst: this.LeadDim * i);
			}
		}
		#endregion

		#region get range methods
		/// <summary>
		/// Get part of the vectors that forms the matrix of length, from <see cref="IMatrix{TMat, TVec, T}.GetColumns(Range, TVec[])"/>.
		/// </summary>
		/// <param name="colRange">the <see cref="Range"/> of columns</param>
		/// <param name="overwrite">the output array of <see cref="DenseVector{T}"/> to overwrite, default null means creating ref vectors if possible</param>
		/// <returns>An array of data type <see cref="DenseVector{T}"/>. If <paramref name="overwrite"/> does not fit, it will not be used.</returns>
		public DenseVector<T>[] GetColumns(Range colRange, DenseVector<T>[] overwrite = null)
		{
			var (_, _, from, count) = CheckRange(Range.All, colRange);
			if (overwrite is null || !CanOverwrite(overwrite, count: count, len: this.NRows))
			{
				var vecs = new DenseVector<T>[count];
				try
				{
					for (var i = from; i < from + count; ++i)
					{
						vecs[i - from] = new DenseVector<T>(this, this.NRows, i * this.LeadDim);
					}
					return vecs;
				}
				catch (Exception)
				{
					Array.ForEach(vecs, v => v?.Dispose());
					throw;
				}
			}
			// else
			for (var i = from; i < from + count; ++i)
			{
				RT.CopyTo(source: this, dest: overwrite[i - from], length: this.NRows, offsetSource: i * this.LeadDim);
			}
			return overwrite;
		}

		/// <summary>
		/// Get part of the row vectors that forms the matrix, from <see cref="IMatrix{TMat, TVec, T}.GetRows(Range, TVec[])"/>.
		/// </summary>
		/// <param name="rowRange">the <see cref="Range"/> of rows</param>
		/// <param name="overwrite">the output array of <see cref="DenseVector{T}"/> to overwrite, default null means creating ref vectors if possible</param>
		/// <returns>An array of data type <see cref="DenseVector{T}"/>. If <paramref name="overwrite"/> does not fit, it will not be used.</returns>
		public DenseVector<T>[] GetRows(Range rowRange, DenseVector<T>[] overwrite = null)
		{
			var (from, count, _, _) = this.CheckRange(rowRange, Range.All);
			if (overwrite is null || !CanOverwrite(overwrite, count: count, len: this.NCols))
			{
				var vecs = new DenseVector<T>[count];
				try
				{
					for (var i = from; i < from + count; ++i)
					{
						vecs[i - from] = new DenseVector<T>(this.NCols, this.OnHost);
						BLAS.VectorGenralCopy(vecs[i - from], this, this.NCols, strideSrc: this.LeadDim, offsetSrc: i);
					}
					return vecs;
				}
				catch (Exception)
				{
					Array.ForEach(vecs, v => v?.Dispose());
					throw;
				}
			}
			// else
			for (var i = from; i < from + count; ++i)
			{
				BLAS.VectorGenralCopy(overwrite[i - from], this, this.NCols, strideSrc: this.LeadDim, offsetSrc: i);
			}
			return overwrite;
		}

		/// <summary>
		/// Get all of the vectors that forms the matrix of length, from <see cref="IMatrix{TMat, TVec, T}.GetColumns(Range, TVec[])"/>.
		/// </summary>
		/// <param name="overwrite">the output array of <see cref="DenseVector{T}"/> to overwrite, default null means creating ref vectors if possible</param>
		/// <returns>An array of data type <see cref="DenseVector{T}"/>. If <paramref name="overwrite"/> does not fit, it will not be used.</returns>
		public DenseVector<T>[] GetColumns(DenseVector<T>[] overwrite = null) => this.GetColumns(Range.All, overwrite);

		/// <summary>
		/// Get all of the row vectors that forms the matrix, from <see cref="IMatrix{TMat, TVec, T}.GetRows(Range, TVec[])"/>.
		/// </summary>
		/// <param name="overwrite">the output array of <see cref="DenseVector{T}"/> to overwrite, default null means creating ref vectors if possible</param>
		/// <returns>An array of data type <see cref="DenseVector{T}"/>. If <paramref name="overwrite"/> does not fit, it will not be used.</returns>
		public DenseVector<T>[] GetRows(DenseVector<T>[] overwrite = null) => this.GetRows(Range.All, overwrite);

		/// <summary>
		/// Get one column of the matrix, from <see cref="IMatrix{TMat, TVec, T}"/>.
		/// </summary>
		/// <param name="index">the <see cref="Index"/> of column</param>
		/// <param name="overwrite">the output <see cref="DenseVector{T}"/> to overwrite, default null means creating a ref vector if possible</param>
		/// <returns>The selected column as a <see cref="DenseVector{T}"/>. If <paramref name="overwrite"/> does not fit, it will not be used.</returns>
		public DenseVector<T> GetColumnAt(Index index, DenseVector<T> overwrite = null)
		{
			var (_, from) = CheckRange(Index.Start, index);
			if (overwrite is null || !CanOverwrite(overwrite, len: this.NRows))
			{
				var vector = new DenseVector<T>(this, this.NRows, from * this.LeadDim);
				return vector;
			}
			// else
			RT.CopyTo(source: this, dest: overwrite, length: this.NRows, offsetSource: from * this.LeadDim);
			return overwrite;
		}

		/// <summary>
		/// Get one row of the matrix, from <see cref="IMatrix{TMat, TVec, T}"/>.
		/// </summary>
		/// <param name="index">the <see cref="Index"/> of column</param>
		/// <param name="overwrite">the output <see cref="DenseVector{T}"/> to overwrite, default null means creating a new vector</param>
		/// <returns>The selected column as a <see cref="DenseVector{T}"/>. If <paramref name="overwrite"/> does not fit, it will not be used.</returns>
		public DenseVector<T> GetRowAt(Index index, DenseVector<T> overwrite = null)
		{
			var (from, _) = CheckRange(index, Index.Start);
			DenseVector<T> vec;
			if (overwrite is null || !CanOverwrite(overwrite, len: this.NCols))
				vec = new DenseVector<T>(this.NCols, this.OnHost);
			else
				vec = overwrite;
			try
			{
				BLAS.VectorGenralCopy(vec, this, this.NCols, strideSrc: this.LeadDim, offsetSrc: from);
				return vec;
			}
			catch (Exception)
			{
				if (vec != overwrite) vec.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Get a new matrix by the column index range, from <see cref="IMatrix{TMat, T}.GetColumnRange(Range, TMat)"/>.
		/// </summary>
		/// <param name="columnRange"><see cref="Range"/> of columns</param>
		/// <param name="overwrite">the output <see cref="MatrixBase{T}"/> to overwrite, default null means creating a ref matrix</param>
		/// <returns>The new matrix (data is not copied) constructed by these columns. If <paramref name="overwrite"/> does not fit, it will not be used.</returns>
		/// <remarks>The new matrix's Hermitian is same as this one (unless it is not square any more).</remarks>
		public DenseMatrix<T> GetColumnRange(Range columnRange, DenseMatrix<T> overwrite = null)
		{
			var (_, _, from, count) = CheckRange(Range.All, columnRange);
			if (overwrite is null || !CanOverwrite(overwrite, rows: this.NRows, cols: count))
			{
				if (columnRange.Equals(Range.All))
					return this;
				else
					return new DenseMatrix<T>(this, this.NRows, count, newLD: this.LeadDim, offset: from * this.LeadDim);
			}
			// else
			RT.CopyMatrixTo(source: this, dest: overwrite, copyNRows: NRows, copyNCols: count, offsetSouceRow: 0, offsetSouceCol: from);
			return overwrite;
		}

		/// <summary>
		/// Get a new matrix by the row index range, from <see cref="IMatrix{TMat, T}.GetRowRange(Range, TMat)"/>.
		/// </summary>
		/// <param name="rowRange"><see cref="Range"/> of rows</param>
		/// <param name="overwrite">the output <see cref="DenseMatrix{T}"/> to overwrite, default null means creating a ref matrix</param>
		/// <returns>A matrix constructed by these rows. If <paramref name="overwrite"/> does not fit, it will not be used.</returns>
		public DenseMatrix<T> GetRowRange(Range rowRange, DenseMatrix<T> overwrite = null)
		{
			var (from, count, _, _) = CheckRange(rowRange, Range.All);
			if (overwrite is null || !CanOverwrite(overwrite, rows: count, cols: this.NCols))
			{
				if (rowRange.Equals(Range.All))
					return this;
				else
					return new DenseMatrix<T>(this, count, this.NCols, newLD: this.LeadDim, offset: from);
			}
			// else
			RT.CopyMatrixTo(source: this, dest: overwrite, copyNRows: count, copyNCols: this.NCols, offsetSouceRow: from, offsetSouceCol: 0);
			return overwrite;
		}

		/// <summary>
		/// Get a sub-matrix by the row and column index ranges, from <see cref="IMatrix{TMat, T}.GetSubmatrix(Range, Range, TMat)"/>.
		/// </summary>
		/// <param name="rowRange"><see cref="Range"/> of rows</param>
		/// <param name="columnRange"><see cref="Range"/> of columns</param>
		/// <param name="overwrite">the output <see cref="MatrixBase{T}"/> to overwrite, default null means creating a ref matrix (if possible)</param>
		/// <returns>A sub-matrix in this region. If <paramref name="overwrite"/> does not fit, it will not be used.</returns>
		public DenseMatrix<T> GetSubmatrix(Range rowRange, Range columnRange, DenseMatrix<T> overwrite = null)
		{
			var (rowfrom, rowcount, colfrom, colcount) = CheckRange(rowRange, columnRange);
			if (overwrite is null || !CanOverwrite(overwrite, rows: rowcount, cols: colcount))
				return new DenseMatrix<T>(this, rowcount, colcount, newLD: this.LeadDim, offset: rowfrom + colfrom * this.LeadDim);
			// else
			RT.CopyMatrixTo(source: this, dest: overwrite, copyNRows: rowcount, copyNCols: colcount, offsetSouceRow: rowfrom, offsetSouceCol: colfrom);
			return overwrite;
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
			if (this.NRows != this.NCols)
				throw new InvalidOperationException(Resource.MatMustSquare);
			if (this.Hermitian && k < 0) k = -k;
			long offset, stride, count;
			if (k <= 0) // diagonal and sub-diagonal
			{
				offset = -k; // start offset
				stride = this.LeadDim + 1; // increment of n + 1 leads to the diagonal elements
				count = this.NRows + k;
			}
			else
			{
				offset = k * this.LeadDim; // start offset
				stride = this.LeadDim + 1; // increment of n + 1 leads to the diagonal elements
				count = this.NRows - k;
			}
			DenseVector<T> output;
			if (overwrite is null || !CanOverwrite(overwrite, len: count))
				output = new DenseVector<T>(count, this.OnHost);
			else
				output = overwrite;
			try
			{
				BLAS.VectorGenralCopy(output, this, count, strideSrc: stride, offsetSrc: offset);
				return output;
			}
			catch (Exception)
			{
				if (output != overwrite) output.Dispose();
				throw;
			}
		}

		/// <summary>
		/// The method to set diagonal elements, from <see cref="IMatrix{TMat, TVec, T}.SetDiag(long, TVec)"/>.
		/// </summary>
		/// <param name="k">diagonal index, 0 for diagonal, 1 for super-diagonal at one above, -1 for sub-diagonal at one below, etc.</param>
		/// <param name="vec">the <see cref="DenseVector{T}"/></param>
		/// <exception cref="InvalidOperationException">if this matrix is not square</exception>
		public void SetDiag(long k, DenseVector<T> vec)
		{
			if (this.NRows != this.NCols)
				throw new InvalidOperationException(Resource.MatMustSquare);
			if (this.Hermitian && k < 0) k = -k;
			long offset, stride, count;
			if (k <= 0) // diagonal and sub-diagonal
			{
				offset = -k; // start offset
				stride = this.LeadDim + 1; // increment of n + 1 leads to the diagonal elements
				count = this.NRows + k;
			}
			else
			{
				offset = k * this.LeadDim; // start offset
				stride = this.LeadDim + 1; // increment of n + 1 leads to the diagonal elements
				count = this.NRows - k;
			}
			if (vec is DenseVector<T> dv)
				BLAS.VectorGenralCopy(this, dv, count, strideDst: stride, offsetDst: offset);
		}
		#endregion

		#region operations
		/// <summary>
		/// Calculate the transpose of this matrix. A new <see cref="DenseMatrix{T}"/> will be created if the result is not it self, from <see cref="IMatrix{TMat, T}.Transpose(TMat)"/>;
		/// </summary>
		/// <param name="overwrite">the output <see cref="DenseMatrix{T}"/> to overwrite, default null means creating a new matrix</param>
		/// <returns>The transposed matrix out-of-place.</returns>
		public DenseMatrix<T> Transpose(DenseMatrix<T> overwrite = null)
		{
			if (this.Hermitian && this.IsRealType)
				return this;
			bool cannotOverwrite = !CanOverwrite(overwrite, rows: this.NCols, cols: this.NRows);
			if (this.Hermitian)
			{
				if (cannotOverwrite)
					return base.ConjugateOutOfPlace() as DenseMatrix<T>;
				RT.CopyMatrixTo(source: this, dest: overwrite, copyNRows: this.NRows, copyNCols: this.NCols);
				overwrite.ConjugateInPlace();
				return overwrite;
			}

			var newMat = cannotOverwrite ? new DenseMatrix<T>(this.NCols, this.NRows, onHost: this.OnHost, herm: false) : overwrite;
			try
			{
				BLAS.MatrixGeneralAdd(this, EmptyDnMat, newMat, α: Scalars<T>.One, opA: MatrixOperation.Transpose);
				return newMat;
			}
			catch (Exception)
			{
				if (newMat != overwrite) newMat.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Calculate the conjugate transpose of this matrix. A new <see cref="DenseMatrix{T}"/> will be created if the result is not it self. Override <see cref="MatrixBase{T}.ConjugateTranspose"/>.
		/// </summary>
		/// <param name="overwrite">the output <see cref="DenseMatrix{T}"/> to overwrite, default null means creating a new matrix</param>
		/// <returns>The conjugate transposed matrix out-of-place.</returns>
		public DenseMatrix<T> ConjugateTranspose(DenseMatrix<T> overwrite = null)
		{
			if (this.Hermitian)
				return this;
			var newMat = CanOverwrite(overwrite, this.NCols, this.NRows) ? overwrite : new DenseMatrix<T>(this.NCols, this.NRows, this.OnHost, herm: false);
			try
			{
				BLAS.MatrixGeneralAdd(this, EmptyDnMat, newMat, Scalars<T>.One, opA: MatrixOperation.ConjugateTranspose);
				return newMat;
			}
			catch (Exception)
			{
				if (newMat != overwrite) newMat.Dispose();
				throw;
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
			return base.Symmetrize(conjugateAtLast, overwrite) as DenseMatrix<T>;
		}
		#endregion

		#region algebra
		/// <summary>
		/// Compute $C_{\text{this}} = \alpha A^{\text{opA}} + \beta B^{\text{opB}}$, from <see cref="IMatrix{TMat, T}.From_αA_Add_βB"/>.
		/// </summary>
		/// <param name="α">scalar of type <typeparamref name="T"/> with default 0. If <c><paramref name="α"/> == 0</c>, <paramref name="A"/> can be an invalid input</param>
		/// <param name="A">the <see cref="DenseMatrix{T}"/> A, can be null</param>
		/// <param name="opA">operation to matrix <paramref name="A"/></param>
		/// <param name="β">scalar of type <typeparamref name="T"/> with default 0. If <c><paramref name="β"/> == 0</c>, <paramref name="B"/> can be an invalid input</param>
		/// <param name="B">the input <see cref="DenseMatrix{T}"/> B, can be null</param>
		/// <param name="opB">operation to matrix <paramref name="B"/></param>
		/// <exception cref="ArgumentNullException">if all of the array are null</exception>
		/// <exception cref="ArgumentException">if the arrays do not match in size</exception>
		/// <exception cref="StatusException">if the internal returns error status</exception>
		public void From_αA_Add_βB(DenseMatrix<T> A, DenseMatrix<T> B, T α = default, T β = default, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			bool zeroA = α.Equals(Scalars<T>.Zero) || A is null || A == EmptyDnMat;
			bool zeroB = β.Equals(Scalars<T>.Zero) || B is null || B == EmptyDnMat;
			if (zeroA && zeroB)
				throw new ArgumentException(Resource.ParaCannotZero);
			if (zeroA)
			{
				BLAS.MatrixGeneralAdd(EmptyDnMat, B, this, β: β, opB: opB);
			}
			else if (zeroB) // symmetric to zeroA: switch A & B
			{
				BLAS.MatrixGeneralAdd(EmptyDnMat, A, this, β: α, opB: opA);
			}
			else // all non zero
			{
				BLAS.MatrixGeneralAdd(A, B, this, α, β, opA, opB);
			}
		}

		/// <summary>
		/// Compute $C_{\text{this}} = \alpha A^{\text{opA}} B^{\text{opB}} + \beta C_{\text{this}}$, from <see cref="IMatrix{TMat, T}.Mulβ_AddBy_αAB(TMat, TMat, T, T, MatrixOperation, MatrixOperation)"/>.
		/// </summary>
		/// <param name="A">the input <see cref="DenseMatrix{T}"/> A</param>
		/// <param name="opA">operation to matrix <paramref name="A"/></param>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		/// <param name="B">the input <see cref="DenseMatrix{T}"/> B</param>
		/// <param name="opB">operation to matrix <paramref name="B"/></param>
		/// <param name="β">scalar of type <typeparamref name="T"/> with default 0</param>
		/// <exception cref="ArgumentNullException">if any of the array is null</exception>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="α"/> is zero</exception>
		/// <exception cref="ArgumentException">if the arrays do not match in size</exception>
		/// <exception cref="StatusException">if the internal calculation returns error status</exception>
		public void Mulβ_AddBy_αAB(DenseMatrix<T> A, DenseMatrix<T> B, T α, T β = default, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			if (α.Equals(Scalars<T>.Zero))
				throw new ArgumentOutOfRangeException(nameof(α), α, Resource.ParaCannotZero);
			BLAS.MatrixMultiply(A, B, this, α, β, opA, opB);
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
			bool cannotOverwrite = !CanOverwrite(overwrite, this.NRows * B.NRows, this.NCols * B.NCols);
			if (cannotOverwrite)
				overwrite = new DenseMatrix<T>(this.NRows * B.NRows, this.NCols * B.NCols, this.OnHost, herm: this.Hermitian && B.Hermitian);

			try
			{
				if ((this.Hermitian && B.Hermitian) || !forceHerm)
				{
					BLAS.MatrixKronecker(this, B, overwrite);
				}
				else
				{
					DenseMatrix<T> A_T = null, B_T = null;
					try
					{
						A_T = this.ConjugateTranspose();
						B_T = B.ConjugateTranspose();
						BLAS.MatrixKronecker(this, B_T, overwrite);
						using var temp = overwrite.NewArrayAlike() as DenseMatrix<T>;
						BLAS.MatrixKronecker(A_T, B, temp);
						// overwrite = temp / 2 + overwrite / 2
						BLAS.MatrixGeneralAdd(temp, overwrite, overwrite, Scalars<T>.Half, Scalars<T>.Half);
					}
					finally
					{
						if (!this.Hermitian) A_T?.Dispose();
						if (!B.Hermitian) B_T?.Dispose();
					}
				}
				return overwrite;
			}
			catch (Exception)
			{
				if (cannotOverwrite) overwrite.Dispose();
				throw;
			}
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
			if (B.OnHost != this.OnHost)
				throw new ArgumentException(Resource.RequireSamePos, nameof(B));
			if (this.NRows != this.NCols || B.NRows != B.NCols)
				throw new ArgumentException(Resource.MatMustSquare);
			bool cannotOverwrite = !CanOverwrite(overwrite, this.NRows * B.NRows, this.NCols * B.NCols);
			if (cannotOverwrite)
				overwrite = new DenseMatrix<T>(this.NRows * B.NRows, this.NCols * B.NCols, this.OnHost, herm: this.Hermitian && B.Hermitian);

			try
			{
				using var eyeB = new DenseMatrix<T>(B.NRows, B.NRows, B.OnHost, herm: true);
				using var eyeA = new DenseMatrix<T>(this.NRows, this.NRows, this.OnHost, herm: true);
				using var temp = overwrite.NewArrayAlike() as DenseMatrix<T>;
				BLAS.FillIdentity(eyeB);
				BLAS.FillIdentity(eyeA);
				if ((this.Hermitian && B.Hermitian) || !forceHerm)
				{
					BLAS.MatrixKronecker(eyeA, B, temp);
					BLAS.MatrixKronecker(this, eyeB, overwrite);
				}
				else
				{
					DenseMatrix<T> symmA = null, symmB = null;
					try
					{
						symmA = this.Symmetrize();
						symmB = B.Symmetrize();
						BLAS.MatrixKronecker(eyeA, symmB, temp);
						BLAS.MatrixKronecker(symmA, eyeB, overwrite);
					}
					finally
					{
						if (!this.Hermitian) symmA?.Dispose();
						if (!B.Hermitian) symmB?.Dispose();
					}
				}
				eyeA.Dispose(); eyeB.Dispose();
				BLAS.MatrixGeneralAdd(overwrite, temp, overwrite, Scalars<T>.One, Scalars<T>.One);
				return overwrite;
			}
			catch (Exception)
			{
				if (cannotOverwrite) overwrite.Dispose();
				throw;
			}
		}
		#endregion

		#endregion

		#region sparse vector restricted
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
			using var ddv = vec.ToDense();
			this.SetDiag(k, ddv);
		}
		#endregion

		#region both sparse matrices restricted operations
		/// <summary>
		/// Compute $C_{\text{this}} = \alpha A^{\text{opA}} + \beta B^{\text{opB}}$, from <see cref="IMatrix{TMat, T}.From_αA_Add_βB"/>.
		/// </summary>
		/// <param name="α">scalar of type <typeparamref name="T"/> with default 0. If <c><paramref name="α"/> == 0</c>, <paramref name="A"/> can be an invalid input</param>
		/// <param name="A">the <see cref="SparseMatrix{T}"/> A, can be null</param>
		/// <param name="opA">operation to matrix <paramref name="A"/></param>
		/// <param name="β">scalar of type <typeparamref name="T"/> with default 0. If <c><paramref name="β"/> == 0</c>, <paramref name="B"/> can be an invalid input</param>
		/// <param name="B">the input <see cref="SparseMatrix{T}"/> B, can be null</param>
		/// <param name="opB">operation to matrix <paramref name="B"/></param>
		/// <exception cref="ArgumentNullException">if all of the array are null</exception>
		/// <exception cref="ArgumentException">if the arrays do not match in size</exception>
		/// <exception cref="StatusException">if the internal returns error status</exception>
		public void From_αA_Add_βB(SparseMatrix<T> A, SparseMatrix<T> B, T α = default, T β = default, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			bool zeroA = α.Equals(Scalars<T>.Zero) || A is null || A == EmptySpMat;
			bool zeroB = β.Equals(Scalars<T>.Zero) || B is null || B == EmptySpMat;
			if (zeroA && zeroB)
				throw new ArgumentException(Resource.ParaCannotZero);
			if (zeroA)
			{
				if (B is null || B == EmptySpMat)
					throw new ArgumentNullException(nameof(B), Resource.ArrayCannotNull);
				if (B.NRows != this.NRows || B.NCols != this.NCols)
					throw new ArgumentException(Resource.MatrixWrongSize, nameof(B));
				switch (opB)
				{
					case MatrixOperation.None:
						using (var dB = B.ToDense())
						{
							RT.CopyMatrixTo(source: dB, dest: this, this.NRows, this.NCols);
						}
						break;
					case MatrixOperation.Transpose:
						B.Transpose(this);
						break;
					case MatrixOperation.ConjugateTranspose:
						B.ConjugateTranspose(this);
						break;
					default:
						break;
				}
				if (β.Equals(Scalars<T>.One))
					return;
				// else
				this.Scale(β);
			}
			else if (zeroB) // symmetric to zeroA: switch A & B
			{
				this.From_αA_Add_βB(EmptySpMat, A, β: α, opB: opA);
			}
			else // all non zero
			{
				if (A is null || A == EmptySpMat)
					throw new ArgumentNullException(nameof(A), Resource.ArrayCannotNull);
				if (B is null || B == EmptySpMat)
					throw new ArgumentNullException(nameof(B), Resource.ArrayCannotNull);
				using var dA = A.ToDense();
				using var dB = B.ToDense();
				this.From_αA_Add_βB(dA, dB, α, β, opA, opB);
			}
		}

		/// <summary>
		/// Compute $C_{\text{this}} = \alpha A^{\text{opA}} B^{\text{opB}} + \beta C_{\text{this}}$, from <see cref="IMatrix{TMat, T}.Mulβ_AddBy_αAB"/>.
		/// </summary>
		/// <param name="A">the input <see cref="MatrixBase{T}"/> A</param>
		/// <param name="opA">operation to matrix <paramref name="A"/></param>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		/// <param name="B">the input <see cref="MatrixBase{T}"/> B</param>
		/// <param name="opB">operation to matrix <paramref name="B"/></param>
		/// <param name="β">scalar of type <typeparamref name="T"/> with default 0</param>
		/// <exception cref="ArgumentNullException">if any of the array is null</exception>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="α"/> is zero</exception>
		/// <exception cref="ArgumentException">if the arrays do not match in size</exception>
		/// <exception cref="StatusException">if the internal calculation returns error status</exception>
		public void Mulβ_AddBy_αAB(SparseMatrix<T> A, SparseMatrix<T> B, T α, T β = default, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			if (α.Equals(Scalars<T>.Zero))
				throw new ArgumentOutOfRangeException(nameof(α), α, Resource.ParaCannotZero);
			if (A is null || A == EmptySpMat)
				throw new ArgumentNullException(nameof(A), Resource.ArrayCannotNull);
			if (B is null || B == EmptySpMat)
				throw new ArgumentNullException(nameof(B), Resource.ArrayCannotNull);

			if ((A.Format & SparseMatrixFormat.ColumnMajor) != 0)
			{
				using var dA = A.ToDense();
				Sparse.MatrixDenseMultiplySparse(dA, B, this, α, β, opA, opB);
			}
			else
			{
				using var dB = B.ToDense();
				Sparse.MatrixSparseMultiplyDense(A, dB, this, α, β, opA, opB);
			}
		}
		#endregion

		#region sparse and dense matrix restricted operations
		/// <summary>
		/// Compute $C_{\text{this}} = \alpha A^{\text{opA}} + \beta B^{\text{opB}}$, from <see cref="IMatrix{TMat, T}.From_αA_Add_βB"/>.
		/// </summary>
		/// <param name="α">scalar of type <typeparamref name="T"/> with default 0. If <c><paramref name="α"/> == 0</c>, <paramref name="A"/> can be an invalid input</param>
		/// <param name="A">the <see cref="SparseMatrix{T}"/> A, can be null</param>
		/// <param name="opA">operation to matrix <paramref name="A"/></param>
		/// <param name="β">scalar of type <typeparamref name="T"/> with default 0. If <c><paramref name="β"/> == 0</c>, <paramref name="B"/> can be an invalid input</param>
		/// <param name="B">the input <see cref="DenseMatrix{T}"/> B, can be null</param>
		/// <param name="opB">operation to matrix <paramref name="B"/></param>
		/// <exception cref="ArgumentNullException">if all of the array are null</exception>
		/// <exception cref="ArgumentException">if the arrays do not match in size</exception>
		/// <exception cref="StatusException">if the internal returns error status</exception>
		public void From_αA_Add_βB(SparseMatrix<T> A, DenseMatrix<T> B, T α = default, T β = default, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			bool zeroA = α.Equals(Scalars<T>.Zero) || A is null || A == EmptyDnMat;
			bool zeroB = β.Equals(Scalars<T>.Zero) || B is null || B == EmptyDnMat;
			if (zeroA && zeroB)
				throw new ArgumentException(Resource.ParaCannotZero);
			
			if (zeroA)
			{
				this.From_αA_Add_βB(EmptyDnMat, B, α, β, opA, opB);
			}
			else if (zeroB)
			{
				this.From_αA_Add_βB(A, EmptySpMat, α, β, opA, opB);
			}
			// else
			if (A is null || A == EmptyDnMat)
				throw new ArgumentNullException(nameof(A), Resource.ArrayCannotNull);
			if (B is null || B == EmptyDnMat)
				throw new ArgumentNullException(nameof(B), Resource.ArrayCannotNull);
			using var dA = A.ToDense();
			BLAS.MatrixGeneralAdd(dA, B, this, α, β, opA, opB);
		}

		/// <summary>
		/// Compute $C_{\text{this}} = \alpha A^{\text{opA}} B^{\text{opB}} + \beta C_{\text{this}}$, from <see cref="IMatrix{TMat, T}.Mulβ_AddBy_αAB"/>.
		/// </summary>
		/// <param name="A">the input <see cref="DenseMatrix{T}"/> A</param>
		/// <param name="opA">operation to matrix <paramref name="A"/></param>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		/// <param name="B">the input <see cref="SparseMatrix{T}"/> B</param>
		/// <param name="opB">operation to matrix <paramref name="B"/></param>
		/// <param name="β">scalar of type <typeparamref name="T"/> with default 0</param>
		/// <exception cref="ArgumentNullException">if any of the array is null</exception>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="α"/> is zero</exception>
		/// <exception cref="ArgumentException">if the arrays do not match in size</exception>
		/// <exception cref="StatusException">if the internal calculation returns error status</exception>
		public void Mulβ_AddBy_αAB(DenseMatrix<T> A, SparseMatrix<T> B, T α, T β = default, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			if (α.Equals(Scalars<T>.Zero))
				throw new ArgumentOutOfRangeException(nameof(α), α, Resource.ParaCannotZero);

			Sparse.MatrixDenseMultiplySparse(A, B, this, α, β, opA, opB);
		}

		/// <summary>
		/// Compute $C_{\text{this}} = \alpha A^{\text{opA}} B^{\text{opB}} + \beta C_{\text{this}}$, from <see cref="IMatrix{TMat, T}.Mulβ_AddBy_αAB(TMat, TMat, T, T, MatrixOperation, MatrixOperation)"/>.
		/// </summary>
		/// <param name="A">the input <see cref="SparseMatrix{T}"/> A</param>
		/// <param name="opA">operation to matrix <paramref name="A"/></param>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		/// <param name="B">the input <see cref="DenseMatrix{T}"/> B</param>
		/// <param name="opB">operation to matrix <paramref name="B"/></param>
		/// <param name="β">scalar of type <typeparamref name="T"/> with default 0</param>
		/// <exception cref="ArgumentNullException">if any of the array is null</exception>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="α"/> is zero</exception>
		/// <exception cref="ArgumentException">if the arrays do not match in size</exception>
		/// <exception cref="StatusException">if the internal calculation returns error status</exception>
		public void Mulβ_AddBy_αAB(SparseMatrix<T> A, DenseMatrix<T> B, T α, T β = default, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			if (α.Equals(Scalars<T>.Zero))
				throw new ArgumentOutOfRangeException(nameof(α), α, Resource.ParaCannotZero);

			Sparse.MatrixSparseMultiplyDense(A, B, this, α, β, opA, opB);
		}
		#endregion


		#region implement abstract methods
		/// <summary>
		/// Fill this matrix with identity.
		/// </summary>
		public override void FillWithIdentity()
		{
			BLAS.FillIdentity(this);
		}

		/// <summary>
		/// Make this matrix actually Hermitian (if <see cref="MatrixBase{T}.Hermitian"/> is true now) by setting the lower half same as upper, override <see cref="MatrixBase{T}.CopyUpperToLower"/>.
		/// </summary>
		/// <returns>This matrix that made general</returns>
		public override void CopyUpperToLower()
		{
			if (this.Hermitian)
				BLAS.MatrixCopyUpperPartToLower(this);
		}

		/// <summary>
		/// Join the array of <see cref="VectorBase{T}"/> forming into a <see cref="MatrixBase{T}"/>
		/// </summary>
		/// <param name="vecs">the input array of <see cref="VectorBase{T}"/>, <see cref="SparseVector{T}"/> is not supported here</param>
		/// <remarks>For <see cref="ISparseArray{T}"/>, please use <see cref="SparseMatrix{T}.FromColumnVectors(VectorBase{T}[])"/> instead.</remarks>
		public override void FromColumnVectors(VectorBase<T>[] vecs)
		{
			if (vecs is null)
				throw new ArgumentNullException(nameof(vecs));
			if (vecs.LongLength <= 1)
				throw new ArgumentException(Resource.VectorTooShort, nameof(vecs));
			if (!vecs.All(e => e != null && e != EmptyDnVec && e is DenseVector<T> && e.Length == vecs[0].Length))
				throw new ArgumentException(Resource.CannotOperate, nameof(vecs));

			this.FromColumnVectors(vecs as DenseVector<T>[]);
		}

		/// <summary>
		/// Get a new matrix by the column index range, override <see cref="MatrixBase{T}.GetColumnRange(Range, MatrixBase{T})"/>.
		/// </summary>
		/// <param name="columnRange"><see cref="Range"/> of columns</param>
		/// <param name="overwrite">the output <see cref="MatrixBase{T}"/> to overwrite, default null means creating a ref matrix</param>
		/// <returns>The new matrix (data is not copied) constructed by these columns. If <paramref name="overwrite"/> does not fit, it will not be used.</returns>
		/// <remarks>The new matrix's Hermitian is same as this one (unless it is not square any more).</remarks>
		public override MatrixBase<T> GetColumnRange(Range columnRange, MatrixBase<T> overwrite = null)
			=> this.GetColumnRange(columnRange, overwrite as DenseMatrix<T>);

		/// <summary>
		/// Get a new matrix by the row index range, override <see cref="MatrixBase{T}.GetRowRange(Range, MatrixBase{T})"/>.
		/// </summary>
		/// <param name="rowRange"><see cref="Range"/> of rows</param>
		/// <param name="overwrite">the output <see cref="MatrixBase{T}"/> to overwrite, default null means creating a ref matrix</param>
		/// <returns>A matrix constructed by these rows. If <paramref name="overwrite"/> does not fit, it will not be used.</returns>
		public override MatrixBase<T> GetRowRange(Range rowRange, MatrixBase<T> overwrite = null)
			=> this.GetRowRange(rowRange, overwrite as DenseMatrix<T>);

		/// <summary>
		/// Get a sub-matrix by the row and column index ranges, override <see cref="MatrixBase{T}.GetSubmatrix(Range, Range, MatrixBase{T})"/>.
		/// </summary>
		/// <param name="rowRange"><see cref="Range"/> of rows</param>
		/// <param name="columnRange"><see cref="Range"/> of columns</param>
		/// <param name="overwrite">the output <see cref="MatrixBase{T}"/> to overwrite, default null means creating a ref matrix (if possible)</param>
		/// <returns>A sub-matrix in this region. If <paramref name="overwrite"/> does not fit, it will not be used.</returns>
		public override MatrixBase<T> GetSubmatrix(Range rowRange, Range columnRange, MatrixBase<T> overwrite = null)
			=> this.GetSubmatrix(rowRange, columnRange, overwrite as DenseMatrix<T>);

		/// <summary>
		/// Get part of the vectors that forms the matrix of length, override <see cref="MatrixBase{T}.GetColumns(Range, VectorBase{T}[])"/>.
		/// </summary>
		/// <param name="colRange">the <see cref="Range"/> of columns</param>
		/// <param name="overwrite">the output array of <see cref="VectorBase{T}"/> to overwrite, default null means creating ref vectors if possible</param>
		/// <returns>An array of data type <see cref="DenseVector{T}"/>. If <paramref name="overwrite"/> does not fit, it will not be used.</returns>
		public override VectorBase<T>[] GetColumns(Range colRange, VectorBase<T>[] overwrite = null)
			=> this.GetColumns(colRange, overwrite as DenseVector<T>[]);

		/// <summary>
		/// Get part of the row vectors that forms the matrix, override <see cref="MatrixBase{T}.GetRows(Range, VectorBase{T}[])"/>.
		/// </summary>
		/// <param name="rowRange">the <see cref="Range"/> of rows</param>
		/// <param name="overwrite">the output array of <see cref="VectorBase{T}"/> to overwrite, default null means creating ref vectors if possible</param>
		/// <returns>An array of data type <see cref="DenseVector{T}"/>. If <paramref name="overwrite"/> does not fit, it will not be used.</returns>
		public override VectorBase<T>[] GetRows(Range rowRange, VectorBase<T>[] overwrite = null)
			=> this.GetRows(rowRange, overwrite as DenseVector<T>[]);

		/// <summary>
		/// Get one column of the matrix, override <see cref="MatrixBase{T}.GetColumnAt(Index, VectorBase{T})"/>.
		/// </summary>
		/// <param name="index">the <see cref="Index"/> of column</param>
		/// <param name="overwrite">the output <see cref="VectorBase{T}"/> to overwrite, default null means creating a ref vector if possible</param>
		/// <returns>The selected column as a <see cref="VectorBase{T}"/>. If <paramref name="overwrite"/> does not fit, it will not be used.</returns>
		public override VectorBase<T> GetColumnAt(Index index, VectorBase<T> overwrite = null)
			=> this.GetColumnAt(index, overwrite as DenseVector<T>);

		/// <summary>
		/// Get one row of the matrix, override <see cref="MatrixBase{T}.GetRowAt(Index, VectorBase{T})"/>.
		/// </summary>
		/// <param name="index">the <see cref="Index"/> of row</param>
		/// <param name="overwrite">the output <see cref="VectorBase{T}"/> to overwrite, default null means creating a ref vector if possible</param>
		/// <returns>The selected row as a <see cref="VectorBase{T}"/>. If <paramref name="overwrite"/> does not fit, it will not be used.</returns>
		public override VectorBase<T> GetRowAt(Index index, VectorBase<T> overwrite = null)
			=> this.GetRowAt(index, overwrite as DenseVector<T>);

		/// <summary>
		/// Override the <see cref="AbstractArray{T}.Clone"/>.
		/// </summary>
		/// <returns>copied matrix</returns>
		public override object Clone() => new DenseMatrix<T>(this);
		#endregion


		#region implement abstract operations
		/// <summary>
		/// Calculate the transpose of this matrix. A new <see cref="MatrixBase{T}"/> will be created if the result is not it self. Override <see cref="MatrixBase{T}.Transpose"/>.
		/// </summary>
		/// <param name="overwrite">the output <see cref="MatrixBase{T}"/> to overwrite, default null means creating a new matrix</param>
		/// <returns>The transposed matrix out-of-place.</returns>
		public override MatrixBase<T> Transpose(MatrixBase<T> overwrite = null)
			=> this.Transpose(overwrite as DenseMatrix<T>);

		/// <summary>
		/// Calculate the conjugate transpose of this matrix. A new <see cref="MatrixBase{T}"/> will be created if the result is not it self. Override <see cref="MatrixBase{T}.ConjugateTranspose"/>.
		/// </summary>
		/// <param name="overwrite">the output <see cref="MatrixBase{T}"/> to overwrite, default null means creating a new matrix</param>
		/// <returns>The conjugate transposed matrix out-of-place.</returns>
		public override MatrixBase<T> ConjugateTranspose(MatrixBase<T> overwrite = null)
			=> this.ConjugateTranspose(overwrite as DenseMatrix<T>);

		/// <summary>
		/// Compute $C_{\text{this}} = \alpha A^{\text{opA}} + \beta B^{\text{opB}}$. Override <see cref="MatrixBase{T}.From_αA_Add_βB"/>.
		/// </summary>
		/// <param name="α">scalar of type <typeparamref name="T"/> with default 0. If <c><paramref name="α"/> == 0</c>, <paramref name="A"/> can be an invalid input</param>
		/// <param name="A">the <see cref="MatrixBase{T}"/> A, can be null</param>
		/// <param name="opA">operation to matrix <paramref name="A"/></param>
		/// <param name="β">scalar of type <typeparamref name="T"/> with default 0. If <c><paramref name="β"/> == 0</c>, <paramref name="B"/> can be an invalid input</param>
		/// <param name="B">the input <see cref="MatrixBase{T}"/> B, can be null</param>
		/// <param name="opB">operation to matrix <paramref name="B"/></param>
		/// <returns>whether this operation can be done in-place</returns>
		/// <exception cref="ArgumentNullException">if all of the array are null</exception>
		/// <exception cref="ArgumentException">if the arrays do not match in size</exception>
		/// <exception cref="StatusException">if the internal returns error status</exception>
		public override void From_αA_Add_βB(MatrixBase<T> A, MatrixBase<T> B, T α = default, T β = default, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			// Ignore Spelling: Dn
			bool zeroA = α.Equals(Scalars<T>.Zero) || A is null || A == EmptyDnMat;
			bool zeroB = β.Equals(Scalars<T>.Zero) || B is null || B == EmptyDnMat;
			if (A is null)
				A = EmptyDnMat;
			if (B is null)
				B = EmptyDnMat;
			if (zeroA && zeroB)
				throw new ArgumentException(Resource.ParaCannotZero);
			DenseMatrix<T> dA = A as DenseMatrix<T>, dB = B as DenseMatrix<T>;
			SparseMatrix<T> sA = A as SparseMatrix<T>, sB = B as SparseMatrix<T>;
			if (dA is null && sA is null)
			{
				A.From_αA_Add_βB_Opposite(this, B, α, β, opA, opB);
			}
			else if (dB is null && sB is null)
			{
				B.From_αA_Add_βB_Opposite(this, A, β, α, opB, opA);
			}
			else if (dA != null && dB != null) // both dense
			{
				this.From_αA_Add_βB(dA, dB, α, β, opA, opB);
			}
			else if (sA != null && sB != null)
			{
				this.From_αA_Add_βB(sA, sB, α, β, opA, opB);
			}
			else if (sA != null && dB != null) // Sp + Dn
			{
				this.From_αA_Add_βB(sA, dB, α, β, opA, opB);
			}
			else if (dA != null && sB != null) // Dn + Sp, swap A & B
			{
				this.From_αA_Add_βB(sB, dA, β, α, opB, opA);
			}
			else
				throw new NotSupportedException();
		}

		/// <summary>
		/// DO NOT Call this method since <see cref="DenseMatrix{T}"/> have no need to implement it.
		/// </summary>
		protected internal override void From_αA_Add_βB_Opposite(MatrixBase<T> C, MatrixBase<T> B, T α = default, T β = default, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			throw new NotImplementedException(string.Format(Resource.Culture, Resource.BuiltInClassImp, nameof(DenseMatrix<T>)));
		}

		/// <summary>
		/// Compute $C_{\text{this}} = \alpha A^{\text{opA}} B^{\text{opB}} + \beta C_{\text{this}}$. Override <see cref="MatrixBase{T}.Mulβ_AddBy_αAB"/>.
		/// </summary>
		/// <param name="A">the input <see cref="MatrixBase{T}"/> A</param>
		/// <param name="opA">operation to matrix <paramref name="A"/></param>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		/// <param name="B">the input <see cref="MatrixBase{T}"/> B</param>
		/// <param name="opB">operation to matrix <paramref name="B"/></param>
		/// <param name="β">scalar of type <typeparamref name="T"/> with default 0</param>
		/// <returns>whether this operation can be done in-place</returns>
		/// <exception cref="ArgumentNullException">if any of the array is null</exception>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="α"/> is zero</exception>
		/// <exception cref="ArgumentException">if the arrays do not match in size</exception>
		/// <exception cref="StatusException">if the internal calculation returns error status</exception>
		public override void Mulβ_AddBy_αAB(MatrixBase<T> A, MatrixBase<T> B, T α, T β = default, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			if (α.Equals(Scalars<T>.Zero))
				throw new ArgumentOutOfRangeException(nameof(α), α, Resource.ParaCannotZero);
			DenseMatrix<T> dA = A as DenseMatrix<T>, dB = B as DenseMatrix<T>;
			SparseMatrix<T> sA = A as SparseMatrix<T>, sB = B as SparseMatrix<T>;
			if (dA is null && sA is null)
			{
				A.Mulβ_AddBy_αAB_Opposite(this, B, SideMode.Left, α, β, opA, opB);
			}
			else if (dB is null && sB is null)
			{
				B.Mulβ_AddBy_αAB_Opposite(this, A, SideMode.Right, α, β, opA, opB);
			}
			else if (dA != null && dB != null) // both dense
			{
				this.Mulβ_AddBy_αAB(dA, dB, α, β, opA, opB);
			}
			else if (sA != null && sB != null) // both sparse
			{
				this.Mulβ_AddBy_αAB(sA, sB, α, β, opA, opB);
			}
			else if (dA != null && sB != null) // Dn * Sp
			{
				this.Mulβ_AddBy_αAB(dA, sB, α, β, opA, opB);
			}
			else if (sA != null && dB != null)  // Sp * Dn
			{
				this.Mulβ_AddBy_αAB(sA, dB, α, β, opA, opB);
			}
			else
				throw new NotSupportedException();
		}

		/// <summary>
		/// DO NOT Call this method since <see cref="DenseMatrix{T}"/> have no need to implement it.
		/// </summary>
		protected internal override void Mulβ_AddBy_αAB_Opposite(MatrixBase<T> C, MatrixBase<T> B, SideMode side, T α, T β = default, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			throw new NotImplementedException(string.Format(Resource.Culture, Resource.BuiltInClassImp, nameof(DenseMatrix<T>)));
		}

		/// <summary>
		/// DO NOT call this method since <see cref="DenseMatrix{T}"/> has no need to implement it.
		/// </summary>
		internal protected override VectorBase<T> Mulx_AddTo_y(VectorBase<T> x, VectorBase<T> y, T α, T β = default, MatrixOperation op = MatrixOperation.None)
		{
			throw new NotImplementedException(string.Format(Resource.Culture, Resource.BuiltInClassImp, nameof(DenseMatrix<T>)));
		}

		/// <summary>
		/// Compute Kronecker product $A \otimes B$. If <paramref name="forceHerm"/> is true, then $(A \otimes B^H + A^H \otimes B)/2$ will be calculated. Override <see cref="MatrixBase{T}.KroneckerProd"/>.
		/// </summary>
		/// <param name="B">right <see cref="MatrixBase{T}"/></param>
		/// <param name="forceHerm">if the result is made Hermitian or not</param>
		/// <param name="overwrite">the <see cref="MatrixBase{T}"/> to overwrite by result, default null</param>
		/// <returns>The result of Kronecker product, a new <see cref="MatrixBase{T}"/> or <paramref name="overwrite"/> if it is not null.</returns>
		public override MatrixBase<T> KroneckerProd(MatrixBase<T> B, bool forceHerm = true, MatrixBase<T> overwrite = null)
		{
			if (B is null || B == EmptyDnMat)
				throw new ArgumentNullException(nameof(B), Resource.ArrayCannotNull);
			var dB = B.ToDense();
			try
			{
				return this.KroneckerProd(dB, forceHerm, overwrite as DenseMatrix<T>);
			}
			finally
			{
				if (dB != B) dB.Dispose();
			}
		}

		/// <summary>
		/// Compute Kronecker sum $A \oplus B \equiv A \otimes I + I \otimes B$ where $A$ is this matrix. If <paramref name="forceHerm"/> is true, then $[(A \otimes I + I \otimes B^H) + (A^H \otimes I + I \otimes B)]/2$ will be calculated. Override <see cref="MatrixBase{T}.KroneckerSum"/>.
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

			var dB = B.ToDense();
			try
			{
				return this.KroneckerSum(dB, forceHerm, overwrite as DenseMatrix<T>);
			}
			finally
			{
				if (dB != B) dB.Dispose();
			}
		}
		#endregion


		#region implement supplement solver

		#region new solver
		/// <summary>
		/// Calculate the eigenvalues (and eigenvectors) of given matrix A for the special eigen-problem -- $A V = V \Lambda$, or matrices pair A, <paramref name="B"/> for the general one -- $A V = \Lambda B V$ or $A B V = \Lambda V$ or $B A V = \Lambda V$. Here, matrix A is this matrix.
		/// </summary>
		/// <typeparam name="TOut">The data type corresponding to <typeparamref name="T"/>:
		/// <list type="table">
		/// <listheader><term>Matrix Type</term><description>  <typeparamref name="T"/> → <typeparamref name="TOut"/></description></listheader>
		/// <item><term>Hermitian</term><description>  <see cref="FloatComplex"/> → <see cref="float"/></description></item>
		/// <item><term>Hermitian</term><description>  <see cref="DoubleComplex"/> → <see cref="double"/></description></item>
		/// <item><term>Non-Hermitian</term><description>  <see cref="float"/> → <see cref="FloatComplex"/></description></item>
		/// <item><term>Non-Hermitian</term><description>  <see cref="double"/> → <see cref="DoubleComplex"/></description></item>
		/// <item><term>Other</term><description>  Other data types remains unchanged</description></item>
		/// </list></typeparam>
		/// <param name="B">the input <see cref="DenseMatrix{T}"/> to calculate general eigen-problem; if <c><paramref name="B"/> is null</c>, the normal eigen is performed and <paramref name="type"/> is not used; otherwise, the general one is performed</param>
		/// <param name="type">the <see cref="Solver.EigType"/> to indicate positions of this matrix and <paramref name="B"/></param>
		/// <returns>The eigenvalues.</returns>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type or <typeparamref name="T"/> and <typeparamref name="TOut"/> do not match</exception>
		/// <exception cref="StatusException">if the internal calculation returns error status</exception>
		/// <exception cref="Solver.MatrixAlgorithmException">if the internal calculation fails</exception>
		/// <remarks>Only Hermitian matrices this and <paramref name="B"/> are supported not.</remarks>
		public DenseVector<TOut> Eigenvalue<TOut>(DenseMatrix<T> B = null, Solver.EigType type = Solver.EigType.Type1) where TOut : struct, IComparable<TOut>
		{
			if (!SOLVER.CheckEigenType<T, TOut>(this, B))
				throw new NotSupportedException(Resource.DataTypeNotSupport);

			DenseVector<TOut> eigenvalues = new DenseVector<TOut>(this.NRows, this.OnHost);
			try
			{
				using var copy = this.Clone() as DenseMatrix<T>;
				// solve
				SOLVER.EigenSolve(eigenvalues, copy, null, null, B, type, Solver.EigMode.NoVector);
				return eigenvalues;
			}
			catch (Exception)
			{
				eigenvalues.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Calculate the eigenvalues (and eigenvectors) of given matrix A for the special eigen-problem -- $A V = V \Lambda$, or matrices pair A, <paramref name="B"/> for the general one -- $A V = \Lambda B V$ or $A B V = \Lambda V$ or $B A V = \Lambda V$. Here, matrix A is this matrix.
		/// </summary>
		/// <typeparam name="TOut">The output eigenvalues' data type corresponding to <typeparamref name="T"/>:
		/// <list type="table">
		/// <listheader><term>Matrix Type</term><description>  <typeparamref name="T"/> → <typeparamref name="TOut"/></description></listheader>
		/// <item><term>Hermitian</term><description>  <see cref="FloatComplex"/> → <see cref="float"/></description></item>
		/// <item><term>Hermitian</term><description>  <see cref="DoubleComplex"/> → <see cref="double"/></description></item>
		/// <item><term>Non-Hermitian</term><description>  <see cref="float"/> → <see cref="FloatComplex"/></description></item>
		/// <item><term>Non-Hermitian</term><description>  <see cref="double"/> → <see cref="DoubleComplex"/></description></item>
		/// <item><term>Other</term><description>  Other data types remains unchanged</description></item>
		/// </list>
		/// </typeparam>
		/// <param name="calcLeft">calculate both left and right eigenvectors or only right eigenvectors</param>
		/// <param name="overwriteValues">the <see cref="DenseVector{TOut}"/> to store eigenvalues, default null means that this method will create a new one and return</param>
		/// <param name="B">the input <see cref="MatrixBase{T}"/> to calculate general eigen-problem; if <c><paramref name="B"/> is null</c>, the normal eigen is performed and <paramref name="type"/> is not used; otherwise, the general one is performed</param>
		/// <param name="type">the <see cref="Solver.EigType"/> to indicate positions of this matrix and <paramref name="B"/></param>
		/// <returns>The eigenvalues and the eigenvectors. If both this matrix and <paramref name="B"/> are Hermitian, this matrix will be overwritten by eigenvectors and the output <c>left</c> and <c>right</c> are both null. Otherwise, the output <c>left</c> and <c>right</c> store the output left and right eigenvectors</returns>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type or <typeparamref name="T"/> and <typeparamref name="TOut"/> do not match</exception>
		/// <exception cref="StatusException">if the internal calculation returns error status</exception>
		/// <exception cref="Solver.MatrixAlgorithmException">if the internal calculation fails</exception>
		public (DenseVector<TOut> values, DenseMatrix<TOut> left, DenseMatrix<TOut> right) Eigensystem<TOut>(bool calcLeft = false, DenseVector<TOut> overwriteValues = null, DenseMatrix<T> B = null, Solver.EigType type = Solver.EigType.Type1) where TOut : struct, IComparable<TOut>
		{
			if (this.NRows != this.NCols)
				throw new InvalidOperationException(Resource.MatMustSquare);
			if (!SOLVER.CheckEigenType<T, TOut>(this, B))
				throw new NotSupportedException(Resource.DataTypeNotSupport);

			var eigenvalues = CanOverwrite(overwriteValues, this.NRows) ? overwriteValues : new DenseVector<TOut>(this.NRows, this.OnHost);
			DenseMatrix<TOut> left = null, right = null;
			try
			{
				if (!this.Hermitian || (B != null && !B.Hermitian))
				{
					if (calcLeft)
						left = new DenseMatrix<TOut>(this.NRows, this.NCols, this.OnHost);
					right = new DenseMatrix<TOut>(this.NRows, this.NCols, this.OnHost);
				}
				// solve
				SOLVER.EigenSolve(eigenvalues, this, left, right, B, type, calcLeft ? Solver.EigMode.Vector : Solver.EigMode.RightOnly);
				// return
				if (!this.Hermitian || (B != null && !B.Hermitian))
					return (eigenvalues, left, right);
				else
					return (eigenvalues, null, null);
			}
			catch (Exception)
			{
				if (eigenvalues != overwriteValues) eigenvalues.Dispose();
				left?.Dispose();
				right?.Dispose();
				throw;
			}
		}
		#endregion

		#region IDecomposable interface
		/// <summary>
		/// Calculate the eigenvalues (and eigenvectors) of this Hermitian matrix for the special eigen-problem -- $A V = V \Lambda$, or matrices pair A, <paramref name="B"/> for the general one -- $A V = \Lambda B V$ or $A B V = \Lambda V$ or $B A V = \Lambda V$. Here, matrix A is this matrix.
		/// </summary>
		/// <param name="B">the input <see cref="DenseMatrix{T}"/> to calculate general eigen-problem; if <c><paramref name="B"/> is null</c>, the normal eigen is performed and <paramref name="type"/> is not used; otherwise, the general one is performed</param>
		/// <param name="type">the <see cref="Solver.EigType"/> to indicate positions of this matrix and <paramref name="B"/></param>
		/// <returns>The eigenvalues</returns>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		/// <exception cref="StatusException">if the internal calculation returns error status</exception>
		/// <exception cref="Solver.MatrixAlgorithmException">if the internal calculation fails</exception>
		public DenseVector<T> EigenvalueHerm(DenseMatrix<T> B = null, Solver.EigType type = Solver.EigType.Type1)
		{
			if (!this.Hermitian)
				throw new NotSupportedException(Resource.MatMustHerm);
			if (B != null && B != EmptyDnMat && !B.Hermitian)
				throw new ArgumentException(Resource.MatMustHerm, nameof(B));

			// local function
			DenseVector<T> Calc<TOut>() where TOut : struct, IComparable<TOut>
			{
				var val = this.Eigenvalue<TOut>(B, type);
				if (typeof(TOut) == typeof(T))
					return val as DenseVector<T>;
				else
				{
					DenseVector<T> outVal = null;
					try
					{
						BLAS.PointWiseToComplex(val, outVal);
						return outVal;
					}
					catch (Exception)
					{
						outVal?.Dispose();
						throw;
					}
					finally
					{
						val.Dispose();
					}
				}
			}

			// invoke local function
			if (this.IsRealType)
				return Calc<T>();
			else if (this.IsSingleType)
				return Calc<float>();
			else
				return Calc<double>();
		}

		/// <summary>
		/// Calculate the eigenvalues (and eigenvectors) of this Hermitian matrix for the special eigen-problem -- $A V = V \Lambda$, or matrices pair A, <paramref name="B"/> for the general one -- $A V = \Lambda B V$ or $A B V = \Lambda V$ or $B A V = \Lambda V$ <b>out-of-place</b>. Here, matrix A is this matrix.
		/// </summary>
		/// <param name="overwriteValues">the <see cref="DenseVector{T}"/> to store eigenvalues, default null means that this method will create a new one and return</param>
		/// <param name="overwriteVectors">the <see cref="DenseMatrix{T}"/> to store eigenvectors, default null means that this method will create a new one and return</param>
		/// <param name="B">the input <see cref="DenseMatrix{T}"/> to calculate general eigen-problem; if <c><paramref name="B"/> is null</c>, the normal eigen is performed and <paramref name="type"/> is not used; otherwise, the general one is performed</param>
		/// <param name="type">the <see cref="Solver.EigType"/> to indicate positions of this matrix and <paramref name="B"/></param>
		/// <returns>The eigenvalues and the eigenvectors.</returns>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		/// <exception cref="StatusException">if the internal calculation returns error status</exception>
		/// <exception cref="Solver.MatrixAlgorithmException">if the internal calculation fails</exception>
		public (DenseVector<T> values, DenseMatrix<T> vectors) EigensystemHerm(DenseVector<T> overwriteValues = null, DenseMatrix<T> overwriteVectors = null, DenseMatrix<T> B = null, Solver.EigType type = Solver.EigType.Type1)
		{
			if (!this.Hermitian)
				throw new NotSupportedException(Resource.MatMustHerm);
			if (B != null && B != EmptyDnMat && !B.Hermitian)
				throw new ArgumentException(Resource.MatMustHerm, nameof(B));

			// local function
			(DenseVector<T> val, DenseMatrix<T> vec) Calc<TOut>() where TOut : struct, IComparable<TOut>
			{
				bool sameType = typeof(TOut) == typeof(T);
				var A = this.Clone() as DenseMatrix<T>;
				try
				{
					if (sameType)
					{
						var (values, _, _) = A.Eigensystem(false, overwriteValues, B, type);
						return (values, A);
					}
					else
					{
						var (values, _, _) = A.Eigensystem(false, overwriteValues, B, type);
						DenseVector<T> valOut = null;
						try
						{
							valOut = CanOverwrite(overwriteValues, len: this.NRows) ? overwriteValues : new DenseVector<T>(this.NRows, this.OnHost);
							BLAS.PointWiseToComplex(values, valOut);
							return (valOut, A);
						}
						catch (Exception)
						{
							if (valOut != overwriteValues) valOut?.Dispose();
							throw;
						}
						finally
						{
							values.Dispose();
						}
					}
				}
				catch (Exception)
				{
					A.Dispose();
					throw;
				}
			}

			// invoke local function
			if (this.IsRealType)
				return Calc<T>();
			else if (this.IsSingleType)
				return Calc<float>();
			else
				return Calc<double>();
		}

		/// <summary>
		/// Compute the singular value decomposition (SVD) of this matrix and corresponding the left and/or right singular vectors: $\text{this} = U S V^*$.
		/// </summary>
		/// <param name="overwriteS">the <see cref="DenseVector{T}"/> to store singular values, default null means that this method will create a new one and return</param>
		/// <param name="overwriteU">the <see cref="DenseMatrix{T}"/> to store left singular vectors, default null means that this method will create a new one and return</param>
		/// <param name="overwriteVct">the <see cref="DenseMatrix{T}"/> to store right singular vectors, default null means that this method will create a new one and return</param>
		/// <param name="calcU">calculate the left singular vectors or not, if false, the return <c>U</c> will be null</param>
		/// <param name="calcV">calculate the right singular vectors or not, if false, the return <c>Vct</c> will be null</param>
		/// <returns>the singular values and left, right singular vectors</returns>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		/// <exception cref="StatusException">if the internal calculation returns error status</exception>
		/// <exception cref="Solver.MatrixAlgorithmException">if the internal calculation fails</exception>
		public (DenseVector<T> S, DenseMatrix<T> U, DenseMatrix<T> Vct) SingularValues(DenseVector<T> overwriteS = null, DenseMatrix<T> overwriteU = null, DenseMatrix<T> overwriteVct = null, bool calcU = true, bool calcV = true)
		{
			long m = Math.Min(this.NRows, this.NCols);

			// local function
			(DenseVector<T> S, DenseMatrix<T> U, DenseMatrix<T> Vct) Calc<TOut>() where TOut : struct, IComparable<TOut>
			{
				bool sameType = typeof(TOut) == typeof(T);
				DenseVector<TOut> tempS = null;
				DenseVector<T> S = null;
				DenseMatrix<T> U = null, Vct = null;
				try
				{
					S = CanOverwrite(overwriteS, len: m) ? overwriteS : new DenseVector<T>(m, this.OnHost);
					if (calcU)
						U = CanOverwrite(overwriteU, rows: this.NRows, cols: m) ? overwriteU : new DenseMatrix<T>(this.NRows, m, this.OnHost);
					if (calcV)
						Vct = CanOverwrite(overwriteVct, rows: m, cols: this.NCols) ? overwriteVct : new DenseMatrix<T>(m, this.NCols, this.OnHost);
					tempS = sameType ? S as DenseVector<TOut> : new DenseVector<TOut>(m, this.OnHost);

					using var A = this.Clone() as DenseMatrix<T>; // prevent this matrix from being destroyed
					SOLVER.SingularValues(A, tempS, U, Vct, calcU ? Solver.SVDStore.Economic : Solver.SVDStore.None, calcV ? Solver.SVDStore.Economic : Solver.SVDStore.None);
					if (!sameType)
						BLAS.PointWiseToComplex(tempS, S);
					return (S, U, Vct);
				}
				catch (Exception)
				{
					if (S != overwriteS) S?.Dispose();
					if (U != overwriteU) U?.Dispose();
					if (Vct != overwriteVct) Vct?.Dispose();
					throw;
				}
				finally
				{
					if (!sameType) tempS?.Dispose();
				}
			}

			// invoke local function
			if (this.IsRealType)
				return Calc<T>();
			else if (this.IsSingleType)
				return Calc<float>();
			else
				return Calc<double>();
		}

		/// <summary>
		/// QR factorize this matrix.
		/// </summary>
		/// <param name="full">perform full factorization or economic factorization</param>
		/// <param name="overwriteQ">the <see cref="DenseMatrix{T}"/> to store triangular matrix Q, default null means that this method will create a new one and return</param>
		/// <param name="overwriteR">the <see cref="DenseMatrix{T}"/> to store triangular matrix R, default null means that this method will create a new one and return</param>
		/// <returns>the Q and R matrices</returns>
		public (DenseMatrix<T> Q, DenseMatrix<T> R) QR(bool full = false, DenseMatrix<T> overwriteQ = null, DenseMatrix<T> overwriteR = null)
		{
			full = full && this.NRows > this.NCols; // for 'fat' matrices, full == economic
			long m = Math.Min(this.NRows, this.NCols);
			long n = Math.Max(this.NRows, this.NCols);
			DenseMatrix<T> R = null, Q = null;
			try
			{
				Q = CanOverwrite(overwriteQ, rows: this.NRows, cols: full ? n : m) ? overwriteQ : new DenseMatrix<T>(this.NRows, full ? n : m, this.OnHost);
				R = CanOverwrite(overwriteR, rows: m, cols: this.NCols) ? overwriteR : new DenseMatrix<T>(m, this.NCols, this.OnHost);
				SOLVER.QR(this, Q, R);
				return (Q, R);
			}
			catch (Exception)
			{
				if (R != overwriteR) R?.Dispose();
				if (Q != overwriteQ) Q?.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Calculate the inverse of this matrix out-of-place
		/// </summary>
		/// <param name="overwrite">the <see cref="DenseMatrix{T}"/> to store the inverse matrix, default null means that this method will create a new one and return</param>
		/// <returns>the inverse matrix</returns>
		public DenseMatrix<T> Inverse(DenseMatrix<T> overwrite = null)
		{
			return base.Inverse(overwrite) as DenseMatrix<T>;
		}
		#endregion

		#endregion


		#region implement indexers
		/// <summary>
		/// Basic indexer of matrix.
		/// </summary>
		/// <param name="row">row position in <see cref="Index"/> form</param>
		/// <param name="column">column position in <see cref="Index"/> form</param>
		/// <returns>Element at position (<paramref name="row"/>, <paramref name="column"/>)</returns>
		/// <remarks>Since a value cannot hold reference, altering the retrieved value does not change this array's value at that position.</remarks>
		public override T this[Index row, Index column] {
			get {
				var (px, py) = CheckRange(row, column);
				return RT.CopyOut(this, offset: py * LeadDim + px);
			}
			set {
				var (px, py) = CheckRange(row, column);
				RT.CopyInto(this, value, offset: py * LeadDim + px);
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
				return this.GetSubmatrix(row, column);
			}
			set {
				if (value is null || value == EmptyDnMat)
					return;
				var (offsetRow, countRow, offsetCol, countCol) = CheckRange(row, column);
				if (value.Length != countRow * countCol)
				{
					var indices = new long[countRow * countCol];
					for (long i = 0; i < countRow; i++)
						for (long j = 0; j < countCol; j++)
							indices[i + j * countCol] = (i + offsetRow) + (j + offsetCol) * this.LeadDim;
					var intInds = Array.ConvertAll(indices, a => (int)a);
					BLAS.SetArrayValues(this, intInds, RT.CopyOut(value));
				}
				else
				{
					var dv = value.ToDense();
					try
					{
						RT.CopyMatrixTo(source: dv, dest: this, copyNRows: countRow, copyNCols: countCol, offsetDestRow: offsetRow, offsetDestCol: offsetCol);
					}
					finally
					{
						if (dv != value) dv.Dispose();
					}
				}
			}
		}

		/// <summary>
		/// Multiple element indexer of matrix.
		/// </summary>
		/// <param name="indices">row and column positions in <see cref="Index"/> array form</param>
		/// <returns>Elements at these positions copied into a new <see cref="DenseVector{T}"/></returns>
		public override VectorBase<T> this[params (Index x, Index y)[] indices] {
			get {
				var (rowPos, colPos) = CheckRange(indices);
				var offsets = checked(rowPos.Zip(colPos, (r, c) => (int)r + (int)c * (int)this.LeadDim).ToArray());
				var vec = new DenseVector<T>(offsets.LongLength, this.OnHost);
				try
				{
					Sparse.VectorGatherAtIndices(this, offsets, vec);
					return vec;
				}
				catch (Exception)
				{
					vec.Dispose();
					throw;
				}
			}
			set {
				if (value is null || value == EmptyDnVec)
					return;
				var v = value.ToDense();
				try
				{
					var (rowPos, colPos) = CheckRange(indices);
					var offsets = checked(rowPos.Zip(colPos, (r, c) => (int)r + (int)c * (int)this.LeadDim).ToArray());
					if (value.Length == 1)
						BLAS.SetArrayValues(this, offsets, RT.CopyOut(v));
					else
						Sparse.VectorSetAtIndices(v, offsets, this);
				}
				finally
				{
					if (v != value) v.Dispose();
				}
			}
		}
		#endregion


		#region implement diagonal indexer
		/// <summary>
		/// The method to get diagonal elements, override <see cref="MatrixBase{T}.GetDiag(long, VectorBase{T})"/>.
		/// </summary>
		/// <param name="k">diagonal index, 0 for diagonal, 1 for super-diagonal at one above, -1 for sub-diagonal at one below, etc.</param>
		/// <param name="overwrite">the output <see cref="VectorBase{T}"/> to overwrite, default null means creating a new vector</param>
		/// <returns>A new <see cref="VectorBase{T}"/> representing the (super-/sub-)diagonal elements.</returns>
		/// <exception cref="InvalidOperationException">if this matrix is not square</exception>
		public override VectorBase<T> GetDiag(long k, VectorBase<T> overwrite = null)
			=> this.GetDiag(k, overwrite as DenseVector<T>);

		/// <summary>
		/// The method to set diagonal elements, override <see cref="MatrixBase{T}.SetDiag(long, VectorBase{T})"/>.
		/// </summary>
		/// <param name="k">diagonal index, 0 for diagonal, 1 for super-diagonal at one above, -1 for sub-diagonal at one below, etc.</param>
		/// <param name="vec">the <see cref="VectorBase{T}"/> </param>
		/// <exception cref="InvalidOperationException">if this matrix is not square</exception>
		public override void SetDiag(long k, VectorBase<T> vec)
		{
			if (this.NRows != this.NCols)
				throw new InvalidOperationException(Resource.MatMustSquare);
			if (vec is null || vec == EmptyDnVec)
				throw new ArgumentNullException(nameof(vec), Resource.ArrayCannotNull);
			if (vec is DenseVector<T> dv)
				this.SetDiag(k, dv);
			else if (vec is SparseVector<T> sv)
				this.SetDiag(k, sv);
			else
			{
				var ddv = vec.ToDense();
				try
				{
					this.SetDiag(k, ddv);
				}
				finally
				{
					if (ddv != vec) ddv.Dispose();
				}
			}
		}
		#endregion


		#region new operations in dense matrix
		/// <summary>
		/// Compute Hermitian rank-k update $C_{\text{this}} = \alpha A^{\text{op}} A^{\text{op}}^H + \beta C_{\text{this}}$.
		/// </summary>
		/// <param name="A">the input non-Hermitian <see cref="DenseMatrix{T}"/> A</param>
		/// <param name="op">operation to matrix <paramref name="A"/></param>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		/// <param name="β">scalar of type <typeparamref name="T"/></param>
		/// <exception cref="ArgumentNullException">if any of the array is null</exception>
		/// <exception cref="ArgumentException">if the arrays do not match in size or β ≠ 0 while C is not Hermitian</exception>
		/// <exception cref="StatusException">if the internal calculation returns error status</exception>
		/// <remarks>This matrix will be a Hermitian matrix stored in upper mode after this operation if <c><paramref name="β"/> == 0</c> or this matrix is Hermitian at first.</remarks>
		public void RankKUpdate(DenseMatrix<T> A, T α, T β = default, MatrixOperation op = MatrixOperation.None)
		{
			BLAS.RankKUpdate(A, this, α, β, opA: op);
		}
		#endregion


		#region managed converter
		/// <summary>
		/// Initialize from managed C# two-dimensional array.
		/// </summary>
		/// <param name="input">the C# multidimensional array of type <typeparamref name="T"/> and on host indicator</param>
		public static explicit operator DenseMatrix<T>((T[,] value, bool onHost) input)
		{
			var (value, onHost) = input;
			var (rows, cols) = value.GetRowColumns();
			bool herm = value.IsHermitian();
			T[] flat = value.ColumnTake();
			var mat = new DenseMatrix<T>(rows: rows, cols: cols, onHost: onHost, herm: herm);
			try
			{
				RT.CopyIntoArray(mat, flat);
				return mat;
			}
			catch (Exception)
			{
				mat.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Initialize from host C# one-dimensional array which stores the matrix in column-order.
		/// </summary>
		/// <param name="input">the C# one-dimensional array of type <typeparamref name="T"/> and the leading dimension</param>
		public static explicit operator DenseMatrix<T>((T[] value, long leadDim, bool onHost) input)
		{
			var (value, leadDim, onHost) = input;
			if (value.LongLength % leadDim != 0)
				return null;
			long rows = leadDim, cols = value.LongLength / leadDim;
			var mat = new DenseMatrix<T>(rows: rows, cols: cols, onHost: onHost);
			try
			{
				RT.CopyIntoArray(mat, value);
				return mat;
			}
			catch (Exception)
			{
				mat.Dispose();
				throw;
			}
		}
		#endregion


		#region print
		/// <summary>
		/// Override <see cref="ValueArray{T}.ToString()"/> to get the string representation of this array.
		/// </summary>
		/// <returns>String representation of this array</returns>
		public override string ToString()
		{
			return base.ToString(new Dictionary<string, object> { ["lead_dim"] = this.LeadDim });
		}

		internal T[,] Raw(IReadOnlyDictionary<PrintSetting, int> config = null)
		{
			config ??= GlobalSettings.PrintConfig;
			var rowCount = Math.Min(config[PrintSetting.MatrixRow], this.NRows);
			var colCount = Math.Min(config[PrintSetting.MatrixColumn], this.NCols);
			var max = Math.Max(rowCount, colCount);
			T[,] result;
			if (this.Hermitian)
			{
				result = RT.CopyOutMatrix(this, max, max);
				var (rows, cols) = result.GetRowColumns();
				for (long i = 0; i < rows; i++)
					for (long j = i + 1; j < cols; j++)
						result[j, i] = result[i, j].GenericConjugate();
			}
			else
			{
				result = RT.CopyOutMatrix(this, rowCount, colCount);
			}
			return result;
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
				if (overrideSetting.ContainsKey(PrintSetting.MatrixRow))
					printConfig[PrintSetting.MatrixRow] = overrideSetting[PrintSetting.MatrixRow];
				if (overrideSetting.ContainsKey(PrintSetting.MatrixColumn))
					printConfig[PrintSetting.MatrixColumn] = overrideSetting[PrintSetting.MatrixColumn];
				if (overrideSetting.ContainsKey(PrintSetting.Precision))
					printConfig[PrintSetting.Precision] = overrideSetting[PrintSetting.Precision];
			}

			string detail = ":" + Environment.NewLine;
			T[,] res = this.Raw(printConfig);
			detail += res.ToMatrixString(hasMore: this.NCols > res.GetLength(1), precision: printConfig[PrintSetting.Precision]);
			if (this.NRows > res.GetLength(0))
				detail += $"...{this.NRows - res.GetLength(0)} more rows";

			return description + detail;
		}
		#endregion


		#region serialize
		/// <summary>
		/// Get the pointers of this instance.
		/// </summary>
		/// <returns>the pointers</returns>
		public override IReadOnlyDictionary<string, IStorage> GetPointers() => DenseMatrixFactory.GetPointers(this);

		/// <summary>
		/// Get other requisite informations for re-constructing this array.
		/// </summary>
		/// <returns>other requisite informations</returns>
		public override IReadOnlyDictionary<string, object> GetOtherInfo() => DenseMatrixFactory.GetOtherInfo(this);
		#endregion
	}
}
