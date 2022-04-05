using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;

using Althea.Helpers;
using Althea.LinearAlgebra.Sparse;
using Althea.Linq;
using Althea.NativeTypes;
using Althea.Storage;

using ExtBlas = Althea.LinearAlgebra.Dense.ExtendBlasApiSelector;
using SpConv = Althea.LinearAlgebra.Sparse.ConversionApiSelector;


namespace Althea.Arrays
{
	/// <summary>
	/// The concrete element-wise coordinated sparse matrix class whose value storage is of type <typeparamref name="TS"/> and sorted index storage is of type <typeparamref name="TSInd"/>.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TS">The storage type used by the value storage</typeparam>
	/// <typeparam name="TInd">Any unmanaged integer number as the index type</typeparam>
	/// <typeparam name="TSInd">The index storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
	public class CoordinateSparseMatrix<T, TInd, TS, TSInd> : SparseMatrix<T, TInd, TS, TSInd>
		where T : unmanaged, INumber<T> where TInd : unmanaged, IBinaryInteger<TInd>
		where TS : class, IStorage<T, TS> where TSInd : class, IStorage<TInd, TSInd>
	{
		#region basic
		private static readonly SparseFormat baseFormat = new(SparseFormat.Type.Coordinated, SparseFormat.Blocking.Element, (SparseFormat.Major)byte.MaxValue);

		private readonly bool rowMajor;

		/// <inheritdoc/>
		public override SparseFormat Format => this.rowMajor ? baseFormat.WithRowMajor : baseFormat.WithColumnMajor;

		/// <summary>
		/// Create a new <see cref="CoordinateSparseMatrix{T, TInd, TS, TSInd}"/> with given parameters.
		/// </summary>
		/// <param name="rowMajor">Whether the new sparse matrix is of row major or column major</param>
		/// <param name="defaultValue">The default value</param>
		/// <param name="rows">The presenting number of rows</param>
		/// <param name="cols">The presenting number of columns</param>
		/// <param name="values">The original value array</param>
		/// <param name="rowIndices">The original row index array</param>
		/// <param name="colIndices">The original column index array</param>
		/// <param name="nnz">The number of elements stored in <paramref name="values"/>, negative means all elements are stored</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="rows"/> or <paramref name="cols"/> ≤ 0</exception>
		/// <exception cref="ArgumentException">If <paramref name="values"/> is too short</exception>
		/// <remarks>The validness of the detail values (such as sorted or not) in these storages are not checked for performance issues.</remarks>
		public CoordinateSparseMatrix(bool rowMajor, long rows, long cols, TS values!!, TSInd rowIndices!!, TSInd colIndices!!, T defaultValue = default, long nnz = -1) : base(rows, cols, values, rowIndices, colIndices, defaultValue, nnz)
		{
			this.rowMajor = rowMajor;
			if (rowIndices.Length != values.Length)
			{
				this.Dispose();
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(rowIndices));
			}
			if (colIndices.Length != values.Length)
			{
				this.Dispose();
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(colIndices));
			}
		}

		internal CoordinateSparseMatrix() : base() { }

		/// <inheritdoc/>
		public override bool Equals(SparseMatrix<T, TInd, TS, TSInd>? other) => other is CoordinateSparseMatrix<T, TInd, TS, TSInd> m && base.Equals(m) && this.rowMajor == m.rowMajor;
		#endregion

		#region implementation
		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool GetOffsets(long row, long col, Span<long> offsets)
		{
			TInd x = TInd.Create(row), y = TInd.Create(col);
			TSInd xInd = this.RowIndexStorage, yInd = this.ColIndexStorage;
			if (!this.rowMajor)
			{
				(x, y) = (y, x);
				(xInd, yInd) = (yInd, xInd);
			}
			long find = SpConv.IndexBound(xInd, x, true);
			if ((xInd + find).ToManaged<TInd, TSInd>() != x)
			{
				offsets[0] = find;
				return false;
			}
			long lower = SpConv.IndexBound(xInd, x, true);
			long upper = SpConv.IndexBound(xInd, x, false);
			find = SpConv.IndexBound(yInd.MakeReference(lower, upper - lower), y, true);
			bool success = (yInd + find).ToManaged<TInd, TSInd>() == y;
			find += lower;
			offsets[0] = find;
			return success;
		}

		/// <inheritdoc/>
		protected override bool TryInsert(long row, long col, Span<long> offsets, T value)
		{
			long offset = offsets[0];
			long nnz = this.NStored;
			if (nnz + 1 > this.MaxStored)
				return false;

			using var tempVal = this.Storage.MakeReference(offset).Clone();
			using var tempRow = this.RowIndexStorage.MakeReference(offset).Clone();
			using var tempCol = this.ColIndexStorage.MakeReference(offset).Clone();
			this.NStored = nnz + 1;
			this.Storage.MakeReference(offset, 1).FromManaged(value);
			this.RowIndexStorage.MakeReference(offset, 1).FromManaged(TInd.Create(row));
			this.ColIndexStorage.MakeReference(offset, 1).FromManaged(TInd.Create(col));
			tempVal.CopyTo<T, TS, TS>(this.Storage + (++offset));
			tempRow.CopyTo<TInd, TSInd, TSInd>(this.RowIndexStorage + offset);
			tempCol.CopyTo<TInd, TSInd, TSInd>(this.ColIndexStorage + offset);
			return true;
		}

		/// <inheritdoc/>
		public override void CopyTo(SparseMatrix<T, TInd, TS, TSInd> destination)
		{
			if (destination is not CoordinateSparseMatrix<T, TInd, TS, TSInd> mat || mat.DefaultValue != this.DefaultValue || mat.rowMajor != this.rowMajor)
				throw new ArgumentException(Resources.ParameterError.UnexpectedType, nameof(destination));
			if (mat.Length != this.Length || mat.NStored != this.NStored)
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(destination));
			this.Storage.CopyTo<T, TS, TS>(destination.Storage);
			this.RowIndexStorage.CopyTo<TInd, TSInd, TSInd>(destination.RowIndexStorage);
			this.ColIndexStorage.CopyTo<TInd, TSInd, TSInd>(destination.ColIndexStorage);
		}

		/// <inheritdoc/>
		public override CoordinateSparseMatrix<T, TInd, TS, TSInd> RefTranspose() => new(!this.rowMajor, this.NCols, this.NRows, this.Storage, this.ColIndexStorage, this.RowIndexStorage, this.DefaultValue, this.NStored);

		/// <inheritdoc/>
		public override CoordinateSparseMatrix<T, TInd, TS, TSInd> CreateAlike() => new(this.rowMajor, this.NRows, this.NCols, this.Storage.CreateAlike(), this.ColIndexStorage.CreateAlike(), this.RowIndexStorage.CreateAlike(), this.DefaultValue, 0);

		/// <inheritdoc/>
		public override string Print(PrintSettings? settings = null)
		{
			var ps = settings ?? Settings.PrintSetting;
			int nnz = (int)Math.Min(this.NStored, ps.ArrayLength);
			using var tempVal = nnz.CheckStackLimit<T>();
			using var tempInd1 = nnz.CheckStackLimit<TInd>();
			using var tempInd2 = nnz.CheckStackLimit<TInd>();
			Span<T> values = tempVal.IsEmpty ? stackalloc T[nnz] : tempVal.Data;
			Span<TInd> rowInd = tempInd1.IsEmpty ? stackalloc TInd[nnz] : tempInd1.Data;
			Span<TInd> colInd = tempInd2.IsEmpty ? stackalloc TInd[nnz] : tempInd2.Data;
			this.Storage.ToManaged(values);
			this.RowIndexStorage.ToManaged(rowInd);
			this.ColIndexStorage.ToManaged(colInd);
			return values.ToSparseMatrixString<T, TInd>(rowInd, colInd, ps.Precision) + (nnz == this.NStored ? "" : string.Format(Resources.Print.MoreStored, this.NStored - nnz));
		}
		#endregion

		#region serialization
		private record struct Repr(bool RowMajor, long NRows, long NCols, T Default, TS Values, TSInd RowIndices, TSInd ColIndices);

		private CoordinateSparseMatrix(Repr repr) : this(repr.RowMajor, repr.NRows, repr.NCols, repr.Values, repr.RowIndices, repr.ColIndices, repr.Default) { }

		/// <inheritdoc/>
		public override string JsonSerialize() => JsonSerializer.Serialize(new Repr(this.rowMajor, this.NRows, this.NCols, this.DefaultValue, this.Storage, this.RowIndexStorage, this.ColIndexStorage), JsonOptions);

		private static bool TryJsonDeserialize(string json!!, [NotNullWhen(true)] out SparseMatrix<T, TInd, TS, TSInd>? matrix)
		{
			try
			{
				matrix = new CoordinateSparseMatrix<T, TInd, TS, TSInd>(JsonSerializer.Deserialize<Repr>(json, JsonOptions));
				return true;
			}
			catch (Exception)
			{
				matrix = null;
				return false;
			}
		}

		private static bool TryCreate(in SparseArrayWrapper<T, TInd, TS, TSInd> wrapper, [NotNullWhen(true)] out SparseMatrix<T, TInd, TS, TSInd>? matrix)
		{
			matrix = null;
			if (wrapper.Size.Length != 2 || wrapper.BlockSize.Length != 0 || wrapper.ValueStorages.Length != 1 || wrapper.IndexStorages.Length != 2 || (wrapper.Format & baseFormat) == SparseFormat.None || (wrapper.Format.MajorType & (SparseFormat.Major.Row | SparseFormat.Major.Column)) == 0)
				return false;
			long rows = wrapper.Size[0], cols = wrapper.Size[1];
			TS values = wrapper.ValueStorages[0];
			TSInd rowIndices = wrapper.IndexStorages[0], colIndices = wrapper.IndexStorages[1];
			if (values.Length > rows * cols)
				return false;
			if (values.Length != rowIndices.Length || values.Length != colIndices.Length)
				return false;
			matrix = new CoordinateSparseMatrix<T, TInd, TS, TSInd>(wrapper.Format.MajorType == SparseFormat.Major.Row, rows, cols, values, rowIndices, colIndices, wrapper.DefaultValue);
			return true;
		}

		static CoordinateSparseMatrix()
		{
			Creators.Add(TryCreate);
			Deserializers.Add(TryJsonDeserialize);
		}
		#endregion
	}


	/// <summary>
	/// The concrete element-wise compressed sparse matrix class whose value storage is of type <typeparamref name="TS"/> and sorted index storage is of type <typeparamref name="TSInd"/>.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TS">The storage type used by the value storage</typeparam>
	/// <typeparam name="TInd">Any unmanaged integer number as the index type</typeparam>
	/// <typeparam name="TSInd">The index storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
	public class CompressSparseMatrix<T, TInd, TS, TSInd> : SparseMatrix<T, TInd, TS, TSInd>
		where T : unmanaged, INumber<T> where TInd : unmanaged, IBinaryInteger<TInd>
		where TS : class, IStorage<T, TS> where TSInd : class, IStorage<TInd, TSInd>
	{
		#region basic
		private static readonly SparseFormat baseFormat = new(SparseFormat.Type.Compressed, SparseFormat.Blocking.Element, (SparseFormat.Major)byte.MaxValue);

		private readonly bool rowMajor;

		/// <inheritdoc/>
		public override TSInd RowIndexStorage => this.rowMajor ? this.rowIndices.MakeReference() : base.RowIndexStorage;
		/// <inheritdoc/>
		public override TSInd ColIndexStorage => this.rowMajor ? base.ColIndexStorage : this.colIndices.MakeReference();

		/// <inheritdoc/>
		public override SparseFormat Format => this.rowMajor ? baseFormat.WithRowMajor : baseFormat.WithColumnMajor;

		/// <summary>
		/// Create a new <see cref="CompressSparseMatrix{T, TInd, TS, TSInd}"/> with given parameters.
		/// </summary>
		/// <param name="rowMajor">Whether the new sparse matrix is of row major or column major</param>
		/// <param name="defaultValue">The default value</param>
		/// <param name="rows">The presenting number of rows</param>
		/// <param name="cols">The presenting number of columns</param>
		/// <param name="values">The original value array</param>
		/// <param name="rowIndices">The original row index array, shall be the row offsets whose first value is 0 when <paramref name="rowMajor"/></param>
		/// <param name="colIndices">The original column index array, shall be the column offsets whose first value is 0 when not <paramref name="rowMajor"/></param>
		/// <param name="nnz">The number of elements stored in <paramref name="values"/>, negative means all elements are stored</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="rows"/> or <paramref name="cols"/> ≤ 0</exception>
		/// <exception cref="ArgumentException">If <paramref name="values"/> is too short</exception>
		/// <remarks>The validness of the detail values (such as sorted or not) in these storages are not checked for performance issues.</remarks>
		public CompressSparseMatrix(bool rowMajor, long rows, long cols, TS values!!, TSInd rowIndices!!, TSInd colIndices!!, T defaultValue = default, long nnz = -1) : base(rows, cols, values, rowIndices, colIndices, defaultValue, nnz)
		{
			this.rowMajor = rowMajor;
			if (rowMajor && (colIndices.Length != values.Length || rowIndices.Length != rows + 1))
			{
				this.Dispose();
				throw new ArgumentException(Resources.ParameterError.WrongSize);
			}
			if (!rowMajor && (rowIndices.Length != values.Length || colIndices.Length != cols + 1))
			{
				this.Dispose();
				throw new ArgumentException(Resources.ParameterError.WrongSize);
			}
		}

		internal CompressSparseMatrix() : base() { }

		/// <inheritdoc/>
		public override bool Equals(SparseMatrix<T, TInd, TS, TSInd>? other) => other is CompressSparseMatrix<T, TInd, TS, TSInd> m && base.Equals(m) && this.rowMajor == m.rowMajor;
		#endregion

		#region implementation
		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool GetOffsets(long row, long col, Span<long> offsets)
		{
			if (this.NStored == 0)
			{
				offsets[0] = 0;
				return false;
			}
			TInd x = TInd.Create(row), y = TInd.Create(col);
			TSInd xInd = this.RowIndexStorage, yInd = this.ColIndexStorage;
			if (!this.rowMajor)
			{
				(row, _) = (col, row);
				(_, y) = (y, x);
				(xInd, yInd) = (yInd, xInd);
			}
			long rowStart = (xInd + row).ToManaged<TInd, TSInd>().As<TInd, long>(),
				rowEnd = (xInd + (row + 1)).ToManaged<TInd, TSInd>().As<TInd, long>();
			long find = SpConv.IndexBound(yInd.MakeReference(rowStart, rowEnd - rowStart), y, true);
			bool success = (yInd + find).ToManaged<TInd, TSInd>() == y;
			find += rowStart;
			offsets[0] = find;
			return success;
		}

		/// <inheritdoc/>
		protected override bool TryInsert(long row, long col, Span<long> offsets, T value)
		{
			long offset = offsets[0];
			long nnz = this.NStored;
			if (nnz + 1 > this.MaxStored)
				return false;

			using var tempVal = this.Storage.MakeReference(offset).Clone();
			using var tempRow = this.rowMajor ? null : this.RowIndexStorage.MakeReference(offset).Clone();
			using var tempCol = this.rowMajor ? this.ColIndexStorage.MakeReference(offset).Clone() : null;
			this.NStored = nnz + 1;
			this.Storage.MakeReference(offset, 1).FromManaged(value);
			if (this.rowMajor)
				this.ColIndexStorage.MakeReference(offset, 1).FromManaged(TInd.Create(row));
			else
				this.RowIndexStorage.MakeReference(offset, 1).FromManaged(TInd.Create(col));
			tempVal.CopyTo<T, TS, TS>(this.Storage + (++offset));
			if (this.rowMajor)
			{
				tempCol?.CopyTo<TInd, TSInd, TSInd>(this.ColIndexStorage + offset);
				ExtBlas.PointWiseAddScalar(this.rowIndices + (row + 1), 1, TInd.One);
			}
			else
			{
				tempRow?.CopyTo<TInd, TSInd, TSInd>(this.RowIndexStorage + offset);
				ExtBlas.PointWiseAddScalar(this.colIndices + (col + 1), 1, TInd.One);
			}
			return true;
		}

		/// <inheritdoc/>
		public override void CopyTo(SparseMatrix<T, TInd, TS, TSInd> destination)
		{
			if (destination is not CompressSparseMatrix<T, TInd, TS, TSInd> mat || mat.DefaultValue != this.DefaultValue || mat.rowMajor != this.rowMajor)
				throw new ArgumentException(Resources.ParameterError.UnexpectedType, nameof(destination));
			if (mat.Length != this.Length || mat.NStored != this.NStored)
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(destination));
			this.Storage.CopyTo<T, TS, TS>(destination.Storage);
			this.RowIndexStorage.CopyTo<TInd, TSInd, TSInd>(destination.RowIndexStorage);
			this.ColIndexStorage.CopyTo<TInd, TSInd, TSInd>(destination.ColIndexStorage);
		}

		/// <inheritdoc/>
		public override CompressSparseMatrix<T, TInd, TS, TSInd> RefTranspose() => new(!this.rowMajor, this.NCols, this.NRows, this.Storage, this.ColIndexStorage, this.RowIndexStorage, this.DefaultValue, this.NStored);

		/// <inheritdoc/>
		public override CompressSparseMatrix<T, TInd, TS, TSInd> CreateAlike() => new(this.rowMajor, this.NRows, this.NCols, this.Storage.CreateAlike(), this.ColIndexStorage.CreateAlike(), this.RowIndexStorage.CreateAlike(), this.DefaultValue, 0);

		/// <inheritdoc/>
		public override string Print(PrintSettings? settings = null)
		{
			var ps = settings ?? Settings.PrintSetting;
			int nnz = (int)Math.Min(this.NStored, ps.ArrayLength);
			using var tempVal = nnz.CheckStackLimit<T>();
			using var tempInd1 = nnz.CheckStackLimit<TInd>();
			using var tempInd2 = nnz.CheckStackLimit<TInd>();
			Span<T> values = tempVal.IsEmpty ? stackalloc T[nnz] : tempVal.Data;
			Span<TInd> rowInd = tempInd1.IsEmpty ? stackalloc TInd[nnz] : tempInd1.Data;
			Span<TInd> colInd = tempInd2.IsEmpty ? stackalloc TInd[nnz] : tempInd2.Data;
			this.Storage.ToManaged(values);
			if (this.rowMajor)
			{
				this.ColIndexStorage.ToManaged(colInd);
				int rows = (int)SpConv.IndexBound(this.rowIndices, TInd.Create(nnz), true);
				using var temp = rows.CheckStackLimit<TInd>();
				Span<TInd> tempInd = temp.IsEmpty ? stackalloc TInd[rows] : temp.Data;
				this.rowIndices.ToManaged(tempInd);
				for (int i = 0; i < rows; i++)
				{
					int start = i == 0 ? 0 : tempInd[i - 1].As<TInd, int>();
					int end = tempInd[i].As<TInd, int>();
					rowInd[start..end].Fill(TInd.Create(i));
				}
			}
			else
			{
				this.RowIndexStorage.ToManaged(rowInd);
				int cols = (int)SpConv.IndexBound(this.colIndices, TInd.Create(nnz), true);
				using var temp = cols.CheckStackLimit<TInd>();
				Span<TInd> tempInd = temp.IsEmpty ? stackalloc TInd[cols] : temp.Data;
				this.colIndices.ToManaged(tempInd);
				for (int i = 0; i < cols; i++)
				{
					int start = i == 0 ? 0 : tempInd[i - 1].As<TInd, int>();
					int end = tempInd[i].As<TInd, int>();
					colInd[start..end].Fill(TInd.Create(i));
				}
			}
			return values.ToSparseMatrixString<T, TInd>(rowInd, colInd, ps.Precision) + (nnz == this.NStored ? "" : string.Format(Resources.Print.MoreStored, this.NStored - nnz));
		}
		#endregion

		#region serialization
		private record struct Repr(bool RowMajor, long NRows, long NCols, T Default, TS Values, TSInd RowIndices, TSInd ColIndices);

		private CompressSparseMatrix(Repr repr) : this(repr.RowMajor, repr.NRows, repr.NCols, repr.Values, repr.RowIndices, repr.ColIndices, repr.Default) { }

		/// <inheritdoc/>
		public override string JsonSerialize() => JsonSerializer.Serialize(new Repr(this.rowMajor, this.NRows, this.NCols, this.DefaultValue, this.Storage, this.RowIndexStorage, this.ColIndexStorage), JsonOptions);

		private static bool TryJsonDeserialize(string json!!, [NotNullWhen(true)] out SparseMatrix<T, TInd, TS, TSInd>? matrix)
		{
			try
			{
				matrix = new CompressSparseMatrix<T, TInd, TS, TSInd>(JsonSerializer.Deserialize<Repr>(json, JsonOptions));
				return true;
			}
			catch (Exception)
			{
				matrix = null;
				return false;
			}
		}

		private static bool TryCreate(in SparseArrayWrapper<T, TInd, TS, TSInd> wrapper, [NotNullWhen(true)] out SparseMatrix<T, TInd, TS, TSInd>? matrix)
		{
			matrix = null;
			if (wrapper.Size.Length != 2 || wrapper.BlockSize.Length != 0 || wrapper.ValueStorages.Length != 1 || wrapper.IndexStorages.Length != 2 || (wrapper.Format & baseFormat) == SparseFormat.None || (wrapper.Format.MajorType & (SparseFormat.Major.Row | SparseFormat.Major.Column)) == 0)
				return false;
			long rows = wrapper.Size[0], cols = wrapper.Size[1];
			TS values = wrapper.ValueStorages[0];
			TSInd rowIndices = wrapper.IndexStorages[0], colIndices = wrapper.IndexStorages[1];
			if (values.Length > rows * cols)
				return false;
			if (values.Length != rowIndices.Length || values.Length != colIndices.Length)
				return false;
			matrix = new CompressSparseMatrix<T, TInd, TS, TSInd>(wrapper.Format.MajorType == SparseFormat.Major.Row, rows, cols, values, rowIndices, colIndices, wrapper.DefaultValue);
			return true;
		}

		static CompressSparseMatrix()
		{
			Creators.Add(TryCreate);
			Deserializers.Add(TryJsonDeserialize);
		}
		#endregion
	}
}
