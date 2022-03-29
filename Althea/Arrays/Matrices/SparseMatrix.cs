using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;

using Althea.Helpers;
using Althea.LinearAlgebra.Sparse;
using Althea.Storage;
using Althea.NativeTypes;

using Blas = Althea.LinearAlgebra.Dense.BlasApiSelector;
using ExtBlas = Althea.LinearAlgebra.Dense.ExtendBlasApiSelector;
using SpComp = Althea.LinearAlgebra.Sparse.ComputationApiSelector;
using SpConv = Althea.LinearAlgebra.Sparse.ConversionApiSelector;


namespace Althea.Arrays
{
	/// <summary>
	/// The abstract sparse matrix class whose value storage is of type <typeparamref name="TS"/> and sorted index storage is of type <typeparamref name="TSInd"/>.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TS">The storage type used by the value storage</typeparam>
	/// <typeparam name="TInd">Any unmanaged integer number as the index type</typeparam>
	/// <typeparam name="TSInd">The index storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
	[StructLayout(LayoutKind.Sequential)]
	public abstract class SparseMatrix<T, TInd, TS, TSInd> : ISparseArray<T, TInd, TS, TSInd>,
		IBaseMatrix<T, SparseMatrix<T, TInd, TS, TSInd>>,
		IMatrixVectorMultiplyOperations<T, DenseVector<T, TS>, DenseVector<T, TS>, SparseMatrix<T, TInd, TS, TSInd>>,
		IMatrixVectorMultiplyOperations<T, SparseVector<T, TInd, TS, TSInd>, DenseVector<T, TS>, SparseMatrix<T, TInd, TS, TSInd>>,
		IMatrixVectorMultiplyOperators<T, DenseVector<T, TS>, DenseVector<T, TS>, SparseMatrix<T, TInd, TS, TSInd>>,
		IMatrixVectorMultiplyOperators<T, SparseVector<T, TInd, TS, TSInd>, DenseVector<T, TS>, SparseMatrix<T, TInd, TS, TSInd>>,
		IMatrixGetDiagonalVector<T, SparseVector<T, TInd, TS, TSInd>, SparseMatrix<T, TInd, TS, TSInd>>,
		IMatrixGetDiagonalVectorVariant<T, SparseVector<T, TInd, TS, TSInd>, SparseMatrix<T, TInd, TS, TSInd>>,
		IMatrixSetDiagonalVector<T, SparseVector<T, TInd, TS, TSInd>, SparseMatrix<T, TInd, TS, TSInd>>,
		IMatrixOperations<T, DenseMatrix<T, TS>, SparseMatrix<T, TInd, TS, TSInd>, DenseMatrix<T, TS>>,
		IMatrixOperations<T, SparseMatrix<T, TInd, TS, TSInd>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>,
		IMatrixUnaryOperators<T, SparseMatrix<T, TInd, TS, TSInd>, SparseMatrix<T, TInd, TS, TSInd>>,
		IMatrixBinaryOperators<T, SparseMatrix<T, TInd, TS, TSInd>, SparseMatrix<T, TInd, TS, TSInd>, SparseMatrix<T, TInd, TS, TSInd>>,
		IMatrixBinaryOperators<T, SparseMatrix<T, TInd, TS, TSInd>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>
		where T : unmanaged, INumber<T> where TInd : unmanaged, IBinaryInteger<TInd>
		where TS : class, IStorage<T, TS> where TSInd : class, IStorage<TInd, TSInd>
	{
		#region basic
		private readonly long rows, cols;

		private readonly TSInd rowIndices, colIndices;

		private readonly TS values;

		private readonly T defaultValue;

		ReadOnlySpan<long> IValueArray<T, SparseMatrix<T, TInd, TS, TSInd>>.Size => ReflectionHelper.CreateReadOnlySpan(in this.rows, 2);

		ReadOnlySpan<long> ISparseArray<T>.Size => ReflectionHelper.CreateReadOnlySpan(in this.rows, 2);
		ReadOnlySpan<TS> ISparseArray<T, TInd, TS, TSInd>.ValueStorages => ReflectionHelper.CreateReadOnlySpan(in this.values, 1);
		ReadOnlySpan<TSInd> ISparseArray<T, TInd, TS, TSInd>.IndexStorages => ReflectionHelper.CreateReadOnlySpan(in this.rowIndices, 2);
		ReadOnlySpan<TInd> ISparseArray<T, TInd, TS, TSInd>.BlockSize => default;

		bool ICheckValid.IsValid() => (this.values?.IsValid() ?? false) && (this.rowIndices?.IsValid() ?? false) && (this.colIndices?.IsValid() ?? false);

		/// <inheritdoc/>
		public long NRows => this.rows;
		/// <inheritdoc/>
		public long NCols => this.cols;

		/// <inheritdoc/>
		public abstract SparseFormat Format { get; }

		/// <inheritdoc/>
		public T DefaultValue => this.defaultValue;

		/// <summary>
		/// Get the value storage of this matrix as a <typeparamref name="TS"/>.
		/// </summary>
		public TS Storage => this.values.MakeReference();

		/// <summary>
		/// Get the row index array's storage of this sparse matrix.
		/// </summary>
		public TSInd RowIndexStorage => this.rowIndices.MakeReference();
		/// <summary>
		/// Get the column index array's storage of this sparse matrix.
		/// </summary>
		public TSInd ColIndexStorage => this.colIndices.MakeReference();

		/// <inheritdoc/>
		public long NStored => this.values.Length;

		/// <inheritdoc/>
		public long Length => this.rows * this.cols;

		/// <summary>
		/// Create a new <see cref="SparseMatrix{T, TInd, TS, TSInd}"/> with given parameters.
		/// </summary>
		/// <param name="format">The <see cref="SparseFormat"/>. Only the combinations of <see cref="SparseFormat.Type.Coordinated"/> or <see cref="SparseFormat.Type.Compressed"/>, <see cref="SparseFormat.Major.Row"/> or <see cref="SparseFormat.Major.Column"/> and <see cref="SparseFormat.Blocking.Element"/> or <see cref="SparseFormat.Blocking.Simple"/> or <see cref="SparseFormat.Blocking.Complicated"/> are supported.</param>
		/// <param name="defaultValue">The default value</param>
		/// <param name="rows">The presenting number of rows</param>
		/// <param name="cols">The presenting number of columns</param>
		/// <param name="values">The original value array</param>
		/// <param name="rowIndices">The original row index array</param>
		/// <param name="colIndices">The original column index array</param>
		/// <param name="rowBlockSize">The constant row block size, must be 0 if the <see cref="ISparseArray{T}.Format"/> does not indicate it</param>
		/// <param name="colBlockSize">The constant column block size, must be 0 if the <see cref="ISparseArray{T}.Format"/> does not indicate it</param>
		/// <param name="rowBlockSizes">The original row block size array, must be null if the <see cref="ISparseArray{T}.Format"/> does not indicate it</param>
		/// <param name="rowBlockSizesScan">The original row block size scan array, must be null if <paramref name="rowBlockSizes"/> is null, can be null if it does not to let this constructor to compute a new one</param>
		/// <param name="colBlockSizes">The original column block size array, must be null if the <see cref="ISparseArray{T}.Format"/> does not indicate it</param>
		/// <param name="colBlockSizesScan">The original column block size scan array, must be null if <paramref name="colBlockSizes"/> is null, can be null if it does not to let this constructor to compute a new one</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="rows"/> or <paramref name="cols"/> ≤ 0</exception>
		/// <exception cref="ArgumentException">If <paramref name="format"/> is not supported or <paramref name="values"/> is too short</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="rowBlockSizes"/> or <paramref name="colBlockSizes"/> is null when it shall not be according to <paramref name="format"/></exception>
		/// <remarks>The validness of the detail values (such as sorted or not) in these storages are not checked for performance issues.</remarks>
		protected SparseMatrix(SparseFormat format, long rows, long cols, TS values!!, TSInd rowIndices!!, TSInd colIndices!!, TInd rowBlockSize = default, TInd colBlockSize = default, TSInd? rowBlockSizes = null, TSInd? rowBlockSizesScan = null, TSInd? colBlockSizes = null, TSInd? colBlockSizesScan = null, T defaultValue = default)
		{
			if (!format.IsAtomic ||
				(format.Class & (SparseFormat.Type.Coordinated | SparseFormat.Type.Compressed)) == 0 ||
				(format.MajorType & (SparseFormat.Major.Row | SparseFormat.Major.Column)) == 0 ||
				(format.BlockType & (SparseFormat.Blocking.Element | SparseFormat.Blocking.Simple | SparseFormat.Blocking.Complicated)) == 0)
				throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(format));
			this.format = format;
			this.defaultValue = defaultValue;
			if (rows <= 0)
				throw new ArgumentOutOfRangeException(nameof(rows), Resources.ParameterError.CannotNegative);
			if (cols <= 0)
				throw new ArgumentOutOfRangeException(nameof(cols), Resources.ParameterError.CannotNegative);
			this.rows = rows; this.cols = cols;
			long actualLength = values.Length;
			if (actualLength >= rows * cols)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(values));

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			void SimpleCheck()
			{
				if (format.Class == SparseFormat.Type.Coordinated)
				{
					if (rowIndices.Length != actualLength)
						throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(rowIndices));
					if (colIndices.Length != actualLength)
						throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(colIndices));
				}
				else
				{
					if (format.MajorType == SparseFormat.Major.Row)
					{
						if (rowIndices.Length != rows)
							throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(rowIndices));
						if (colIndices.Length != actualLength)
							throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(colIndices));
					}
					else
					{
						if (rowIndices.Length != actualLength)
							throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(rowIndices));
						if (colIndices.Length != cols)
							throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(colIndices));
					}
				}
			}

			if (format.BlockType == SparseFormat.Blocking.Element)
			{
				if (rowBlockSize != TInd.Zero)
					throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(rowBlockSize));
				if (colBlockSize != TInd.Zero)
					throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(colBlockSize));
				if ((rowBlockSizes ?? rowBlockSizesScan) is not null)
					throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(rowBlockSizes));
				if ((colBlockSizes ?? colBlockSizesScan) is not null)
					throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(colBlockSizes));
				SimpleCheck();
			}
			else if (format.BlockType == SparseFormat.Blocking.Simple)
			{
				if (rowBlockSize <= TInd.Zero)
					throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(rowBlockSize));
				if (colBlockSize <= TInd.Zero)
					throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(colBlockSize));
				if ((rowBlockSizes ?? rowBlockSizesScan) is not null)
					throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(rowBlockSizes));
				if ((colBlockSizes ?? colBlockSizesScan) is not null)
					throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(colBlockSizes));
				long rbs = rowBlockSize.As<TInd, long>(), cbs = colBlockSize.As<TInd, long>();
				if (rows % rbs != 0)
					throw new ArgumentException(Resources.ArithmeticError.CannotDivide, nameof(rowBlockSize));
				if (cols % cbs != 0)
					throw new ArgumentException(Resources.ArithmeticError.CannotDivide, nameof(colBlockSize));
				if (actualLength % (rbs * cbs) != 0)
					throw new ArgumentException(Resources.ArithmeticError.CannotDivide);
				rows /= rowBlockSize.As<TInd, long>(); cols /= colBlockSize.As<TInd, long>();
				actualLength /= rbs * cbs;
				SimpleCheck();
			}
			else if (format.BlockType == SparseFormat.Blocking.Complicated)
			{
				if (rowBlockSize != TInd.Zero)
					throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(rowBlockSize));
				if (colBlockSize != TInd.Zero)
					throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(colBlockSize));
				if (rowBlockSizes is null)
					throw new ArgumentNullException(nameof(rowBlockSizes));
				if (colBlockSizes is null)
					throw new ArgumentNullException(nameof(colBlockSizes));
				if (rowBlockSizesScan is not null && rowBlockSizesScan.Length != rowBlockSizes.Length)
					throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(rowBlockSizesScan));
				if (colBlockSizesScan is not null && colBlockSizesScan.Length != colBlockSizes.Length)
					throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(colBlockSizesScan));
				if (format.Class == SparseFormat.Type.Coordinated)
				{
					if (rowBlockSizes.Length != rowIndices.Length)
						throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(rowBlockSizes));
					if (rowBlockSizes.Length != colIndices.Length)
						throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(rowBlockSizes));
					if (colBlockSizes.Length != colIndices.Length)
						throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(colBlockSizes));
				}
				else
				{
					long rowBlocks = rowBlockSizes.Length, colBlocks = colBlockSizes.Length;
					if (format.MajorType == SparseFormat.Major.Row)
					{
						if (rowIndices.Length != rows)
							throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(rowIndices));
					}
					else
					{
						if (colIndices.Length != cols)
							throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(colIndices));
					}
				}
				if (rowBlockSizesScan is null)
					rowBlockSizesScan = rowBlockSizes.ApplyToAlike(static (org, @new) => ExtBlas.PartialSum<TInd, TSInd, TSInd>(org, 1, @new, 1, false));
				if (colBlockSizesScan is null)
					colBlockSizesScan = colBlockSizes.ApplyToAlike(static (org, @new) => ExtBlas.PartialSum<TInd, TSInd, TSInd>(org, 1, @new, 1, false));
			}

			this.values = values.AddToManager();
			this.rowIndices = rowIndices.AddToManager();
			this.colIndices = colIndices.AddToManager();
			this.rowBlockSize = rowBlockSize;
			this.colBlockSize = colBlockSize;
			this.rowBlockSizes = rowBlockSizes?.AddToManager();
			this.rowBlockSizesScan = rowBlockSizesScan?.AddToManager();
			this.colBlockSizes = colBlockSizes?.AddToManager();
			this.colBlockSizesScan = colBlockSizesScan?.AddToManager();
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			this.values.SafeDispose();
			this.rowIndices.SafeDispose();
			this.colIndices.SafeDispose();
			this.rowBlockSizes.SafeDispose();
			this.rowBlockSizesScan.SafeDispose();
			this.colBlockSizes.SafeDispose();
			this.colBlockSizesScan.SafeDispose();
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Deconstructor to be invoked by GC.
		/// </summary>
		~SparseMatrix()
		{
			this.Dispose();
		}

		private SparseMatrix()
		{
			this.values = TS.Empty; this.rowIndices = TSInd.Empty; this.colIndices = TSInd.Empty;
		}

		static SparseMatrix<T, TInd, TS, TSInd> IValueArray<T, SparseMatrix<T, TInd, TS, TSInd>>.Empty => new();
		#endregion

		#region equality
		/// <inheritdoc/>
		public bool Equals(SparseMatrix<T, TInd, TS, TSInd>? other)
		{
			if (other is null)
				return false;
			return this.format == other.format && this.defaultValue == other.defaultValue &&
				this.values == other.values && this.rowIndices == other.rowIndices && this.colIndices == other.colIndices;
		}

		/// <summary>
		/// Equality operator
		/// </summary>
		public static bool operator ==(SparseMatrix<T, TInd, TS, TSInd> left, SparseMatrix<T, TInd, TS, TSInd> right) => left.Equals(right);

		/// <summary>
		/// Inequality operator
		/// </summary>
		public static bool operator !=(SparseMatrix<T, TInd, TS, TSInd> left, SparseMatrix<T, TInd, TS, TSInd> right) => !left.Equals(right);

		/// <inheritdoc/>
		public override bool Equals(object? obj) => this.Equals(obj as SparseMatrix<T, TInd, TS, TSInd>);

		/// <inheritdoc/>
		public override int GetHashCode() => HashCode.Combine(this.format, this.defaultValue, this.values, this.rowIndices, this.colIndices);
		#endregion

		#region index
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private long GetOffset(long row, long col)
		{
			IBaseMatrix<T, SparseMatrix<T, TInd, TS, TSInd>>.CheckIndex(this, row, col);
			long offset;
			if (this.format.BlockType == SparseFormat.Blocking.Element)
			{
				offset = SpConv.IndexFind(this.indices, true, TInd.Create(index));
			}
			else if(this.format.BlockType == SparseFormat.Blocking.Simple)
			{
				var (blockIndex, insideBlockOffset) = index.DivRem(this.BS);
				offset = SpConv.IndexFind(this.indices, true, TInd.Create(blockIndex));
				if (offset >= 0)
					offset = offset * this.BS + insideBlockOffset;
			}
			else
			{

			}
			return offset;
		}

		/// <inheritdoc/>
		public T this[long x, long y]
		{
			get
			{
				long offset = this.GetOffset(x, y);
				return offset < 0 ? this.defaultValue : (this.values + offset).ToManaged<T, TS>();
			}
			set
			{
				long offset = this.GetOffset(x, y);
				if (offset < 0)
					throw new ArgumentException(Resources.SparseError.CannotSetSparse);
				(this.values + offset).FromManaged(value);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private (long indexStart, long indexCount, long valueStart, long valueCount, long blockStartOffset, long blockEndSize) GetSliceInfo(long start, long count, SparseMatrix<T, TInd, TS, TSInd>? sub = null)
		{
			IBaseMatrix<T, SparseMatrix<T, TInd, TS, TSInd>>.CheckRange(this, start, count, sub);
			long indexStart, indexCount;
			long valueStart, valueCount;
			long blockStartOffset = 0, blockEndSize = 0;
			if (this.format.BlockType == SparseFormat.Blocking.Element)
			{
				indexStart = SpConv.IndexBound(this.indices, TInd.Create(start), true);
				indexCount = SpConv.IndexBound(this.indices, TInd.Create(start + count), true);
				indexCount -= indexStart;
				valueStart = indexStart; valueCount = indexCount;
			}
			else
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				(long index, long value, long block) GetOffsets(long allOffset)
				{
					long indexOffset = SpConv.IndexBound(this.indices, TInd.Create(allOffset + 1), true) - 1;
					if (indexOffset >= this.indices.Length)
						return (indexOffset, this.values.Length, 0);
					long blockSize = this.blockSizes is null ? this.blockSize.As<TInd, long>() : (this.blockSizes + indexOffset).ToManaged<TInd, TSInd>().As<TInd, long>();
					long blockOffset = (this.indices + indexOffset).ToManaged<TInd, TSInd>().As<TInd, long>();
					blockOffset = allOffset - blockOffset;
					long valueOffset;
					if (blockOffset > 0 && blockOffset < blockSize)
					{
						if (this.format.BlockType == SparseFormat.Blocking.Simple)
							throw new ArgumentException(Resources.SparseError.CannotCutSimpleBlocking, nameof(allOffset));
						valueOffset = blockOffset + (this.blockSizesScan is null ? blockSize * indexOffset : (this.blockSizesScan + indexOffset).ToManaged<TInd, TSInd>().As<TInd, long>());
					}
					else
					{
						valueOffset = blockOffset + (this.blockSizesScan is null ? blockSize * indexOffset : (this.blockSizesScan + indexOffset).ToManaged<TInd, TSInd>().As<TInd, long>());
						blockOffset = 0;
					}
					return (indexOffset, valueOffset, blockOffset);
				}

				(indexStart, valueStart, blockStartOffset) = GetOffsets(start);
				(indexCount, valueCount, blockEndSize) = GetOffsets(start + count);
				indexCount -= indexStart; valueCount -= valueStart;
			}
			if (sub is not null)
			{
				if (sub.IndexStorage.Length != indexCount || sub.Storage.Length != valueCount)
					throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(sub));
			}
			return (indexStart, indexCount, valueStart, valueCount, blockStartOffset, blockEndSize);
		}

		/// <inheritdoc/>
		public SparseMatrix<T, TInd, TS, TSInd> GetSlice(long start, long count)
		{
			var (indexStart, indexCount, valueStart, valueCount, blockStartOffset, blockEndSize) = this.GetSliceInfo(start, count);
			if (indexStart == 0 && indexCount == this.indices.Length && valueStart == 0 && valueCount == this.values.Length)
				return new(this.format, count, this.values.Clone(), this.indices.Clone(), this.blockSize, this.blockSizes?.Clone(), this.blockSizesScan?.Clone(), this.defaultValue);

			var newVals = this.values.MakeReference(valueStart, valueCount);
			var newInds = this.indices.MakeReference(indexStart, indexCount);
			if (start == 0)
				newInds = newInds.Clone();
			else
				newInds = newInds.ApplyToClone(ind => ExtBlas.PointWiseAddScalar(ind, 1, TInd.Create(-start)));
			TSInd? bs = null, bsa = null;
			if (this.blockSizes is not null && this.blockSizesScan is not null)
			{
				bs = this.blockSizes.MakeReference(indexStart, indexCount);
				if (blockStartOffset == 0 && blockEndSize == 0)
				{
					bs = bs.Clone();
				}
				else
				{
					bs = bs.ApplyToClone(@new =>
					{
						@new.FromManaged(@new.ToManaged<TInd, TSInd>() - TInd.Create(blockStartOffset));
						if (blockEndSize == 0)
							return;
						var newEnd = @new + (@new.Length - 1);
						newEnd.FromManaged(TInd.Create(blockEndSize));
					});
				}
				bsa = this.blockSizesScan.MakeReference(indexStart, indexCount);
				bsa = bsa.ApplyToClone(@new =>
				{
					ExtBlas.PointWiseAddScalar(@new, 1, -@new.ToManaged<TInd, TSInd>());
					if (blockStartOffset != 0)
						ExtBlas.PointWiseAddScalar(@new + 1, 1, TInd.Create(-blockStartOffset));
				});
			}
			return new(this.format, count, newVals, newInds, this.blockSize, bs, bsa, this.defaultValue);
		}

		/// <inheritdoc/>
		public void GetSlice(long start, long count, SparseMatrix<T, TInd, TS, TSInd> overwrite)
		{
			var (indexStart, indexCount, valueStart, valueCount, blockStartOffset, blockEndSize) = this.GetSliceInfo(start, count, overwrite);
			if (indexStart == 0 && indexCount == this.indices.Length && valueStart == 0 && valueCount == this.values.Length)
			{
				this.CopyTo(overwrite);
				return;
			}

			var refVals = this.values.MakeReference(valueStart, valueCount);
			refVals.CopyTo<T, TS, TS>(overwrite.Storage);
			var refInds = this.indices.MakeReference(indexStart, indexCount);
			refInds.CopyTo<TInd, TSInd, TSInd>(overwrite.IndexStorage);
			if (start != 0)
				ExtBlas.PointWiseAddScalar(overwrite.IndexStorage, 1, TInd.Create(-start));

			if (this.blockSizes is not null && this.blockSizesScan is not null && overwrite.blockSizes is not null && overwrite.blockSizesScan is not null)
			{
				TSInd bs = this.blockSizes.MakeReference(indexStart, indexCount);
				bs.CopyTo<TInd, TSInd, TSInd>(overwrite.blockSizes);
				if (blockStartOffset != 0)
				{
					overwrite.blockSizes.FromManaged(overwrite.blockSizes.ToManaged<TInd, TSInd>() - TInd.Create(blockStartOffset));
				}
				if (blockEndSize != 0)
				{
					var newEnd = overwrite.blockSizes + (indexCount - 1);
					newEnd.FromManaged(TInd.Create(blockEndSize));
				}
				TSInd bsa = this.blockSizesScan.MakeReference(indexStart, indexCount);
				bsa.CopyTo<TInd, TSInd, TSInd>(overwrite.blockSizesScan);
				ExtBlas.PointWiseAddScalar(overwrite.blockSizesScan, 1, -overwrite.blockSizesScan.ToManaged<TInd, TSInd>());
				if (blockStartOffset != 0)
					ExtBlas.PointWiseAddScalar(overwrite.blockSizesScan + 1, 1, TInd.Create(-blockStartOffset));
			}
		}

		/// <inheritdoc/>
		public void CopyTo(SparseMatrix<T, TInd, TS, TSInd> destination)
		{
			if (destination.indices.Length != this.indices.Length || destination.values.Length != this.values.Length)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(destination));
			this.values.CopyTo<T, TS, TS>(destination.values);
			this.indices.CopyTo<TInd, TSInd, TSInd>(destination.indices);
			if (this.blockSizes is not null && this.blockSizesScan is not null && destination.blockSizes is not null && destination.blockSizesScan is not null)
			{
				this.blockSizes.CopyTo<TInd, TSInd, TSInd>(destination.blockSizes);
				this.blockSizesScan.CopyTo<TInd, TSInd, TSInd>(destination.blockSizesScan);
			}
		}

		/// <inheritdoc/>
		public void SetSlice(long start, long count, SparseMatrix<T, TInd, TS, TSInd> value)
		{
			var (indexStart, indexCount, valueStart, valueCount, blockStartOffset, blockEndSize) = this.GetSliceInfo(start, count, value);
			if (indexStart == 0 && indexCount == this.indices.Length && valueStart == 0 && valueCount == this.values.Length)
			{
				value.CopyTo((SparseMatrix<T, TInd, TS, TSInd>)this);
				return;
			}

			var refVals = this.values.MakeReference(valueStart, valueCount);
			value.Storage.CopyTo<T, TS, TS>(refVals);
			var refInds = this.indices.MakeReference(indexStart, indexCount);
			value.IndexStorage.CopyTo<TInd, TSInd, TSInd>(refInds);
			if (start != 0)
				ExtBlas.PointWiseAddScalar(refInds, 1, TInd.Create(start));

			if (this.blockSizes is not null && this.blockSizesScan is not null && value.blockSizes is not null && value.blockSizesScan is not null)
			{
				TSInd bs = this.blockSizes.MakeReference(indexStart, indexCount);
				value.blockSizes.CopyTo<TInd, TSInd, TSInd>(bs);
				if (blockStartOffset != 0)
				{
					bs.FromManaged(bs.ToManaged<TInd, TSInd>() + TInd.Create(blockStartOffset));
				}
				if (blockEndSize != 0)
				{
					var newEnd = bs + (indexCount - 1);
					newEnd.FromManaged(TInd.Create(blockEndSize));
				}
				TSInd bsa = this.blockSizesScan.MakeReference(indexStart, indexCount);
				TInd scanStart = bsa.ToManaged<TInd, TSInd>();
				value.blockSizesScan.CopyTo<TInd, TSInd, TSInd>(bsa);
				ExtBlas.PointWiseAddScalar(bsa, 1, scanStart);
				if (blockStartOffset != 0)
					ExtBlas.PointWiseAddScalar(bsa + 1, 1, TInd.Create(blockStartOffset));
			}
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			using var dense = this.ToDense();
			foreach (var item in dense)
			{
				yield return item;
			}
		}
		#endregion

		#region point-wise operations
		/// <inheritdoc/>
		public void FillWith(T value)
		{
			if (value != this.defaultValue)
				throw new ArgumentException(Resources.SparseError.CannotSetSparse, nameof(value));
			this.values.FillWith(value);
		}

		/// <inheritdoc/>
		public void AddScalar(T value)
		{
			if (value != T.Zero)
				throw new ArgumentException(Resources.SparseError.CannotSetSparse, nameof(value));
		}

		/// <inheritdoc/>
		public void Scale(T value)
		{
			if (this.defaultValue == T.Zero)
				Blas.Scale(this.values, 1, value);
			else if (value != T.One)
				throw new ArgumentException(Resources.SparseError.CannotSetSparse, nameof(value));
		}

		/// <inheritdoc/>
		public void Conjugate()
		{
			if (NumberType<T>.IsComplex)
			{
				if (NumberType<T>.IsRealValue(this.defaultValue))
					ExtBlas.PointWiseConjugate<T, TS>(this.values, 1);
				else
					throw new InvalidOperationException(Resources.SparseError.CannotSetSparse);
			}
		}

		/// <inheritdoc/>
		public void Power(T power)
		{
			if (this.defaultValue == T.Zero || this.defaultValue == T.One)
				ExtBlas.PointWisePower(this.values, 1, power);
			else
				throw new ArgumentException(Resources.SparseError.CannotSetSparse, nameof(power));
		}

		/// <inheritdoc/>
		public void Truncate(double threshold)
		{
			if (this.defaultValue != T.Zero && T.Abs(this.defaultValue) < T.Create(threshold))
				throw new ArgumentException(Resources.SparseError.CannotSetSparse, nameof(threshold));
			else
				ExtBlas.PointWiseTruncate<T, TS>(this.values, 1, threshold);
		}
		#endregion

		#region simple aggregation operations
		/// <inheritdoc/>
		public T Sum()
		{
			T defaultSum = this.defaultValue * T.Create(((IVectorMetric)this).Length - this.values.Length);
			return defaultSum + ExtBlas.AggregateSum<T, TS>(this.values, 1);
		}

		/// <inheritdoc/>
		public T AbsSum()
		{
			T defaultSum = T.Abs(this.defaultValue) * T.Create(((IVectorMetric)this).Length - this.values.Length);
			return defaultSum + Blas.AbsoluteValueSum<T, TS>(this.values, 1);
		}

		/// <inheritdoc/>
		public T Norm()
		{
			if (this.defaultValue == T.Zero)
				return Blas.Norm<T, TS>(this.values, 1);
			T abs = T.Abs(this.defaultValue);
			T defaultSum = abs * abs * T.Create(((IVectorMetric)this).Length - this.values.Length);
			T norm = Blas.Norm<T, TS>(this.values, 1);
			double n = (norm * norm + defaultSum).As<T, double>();
			return Math.Sqrt(n).As<double, T>();
		}

		/// <inheritdoc/>
		public T ValueWithMaxAbs()
		{
			T max = (this.values + Blas.AbsoluteValueArgMax<T, TS>(this.values, 1)).ToManaged<T, TS>();
			if (T.Abs(this.defaultValue) > T.Abs(max))
				return this.defaultValue;
			else
				return max;
		}

		/// <inheritdoc/>
		public T ValueWithMinAbs()
		{
			T min = (this.values + Blas.AbsoluteValueArgMin<T, TS>(this.values, 1)).ToManaged<T, TS>();
			if (T.Abs(this.defaultValue) < T.Abs(min))
				return this.defaultValue;
			else
				return min;
		}
		#endregion

		#region operations
		/// <inheritdoc/>
		public static T Dot(DenseVector<T, TS> left, SparseMatrix<T, TInd, TS, TSInd> right, bool conjugateLeft = true)
		{
			if (left.Length != right.Length)
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(right));
			return SpComp.VectorSparseDotDense(conjugateLeft, right, left.Storage, left.Stride);
		}

		/// <summary>
		/// Statically compute the dot (inner) product of <paramref name="left"/> and <paramref name="right"/>.
		/// </summary>
		/// <param name="left">The left vector to perform the dot product</param>
		/// <param name="right">The right vector to perform the dot product</param>
		/// <param name="conjugateLeft">Whether the dot product is performed on the conjugation of <paramref name="left"/> or directly.</param>
		/// <returns>The dot (inner) product result as a <typeparamref name="T"/></returns>
		public static T Dot(SparseMatrix<T, TInd, TS, TSInd> left, SparseMatrix<T, TInd, TS, TSInd> right, bool conjugateLeft = true)
		{
			if (left.Length != right.Length)
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(right));
			return SpComp.VectorSparseDotSparse(conjugateLeft, right, left);
		}

		/// <inheritdoc/>
		public static void AddBy(DenseVector<T, TS> left, SparseMatrix<T, TInd, TS, TSInd> right, T scalar)
		{
			if (left.Length != right.Length)
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(right));
			SpComp.VectorSparseAddToDense(scalar, right, left.Storage, left.Stride);
		}

		/// <inheritdoc/>
		public static void SetDiag(DenseMatrix<T, TS> matrix, long k, SparseMatrix<T, TInd, TS, TSInd> value)
		{
			var diag = DenseMatrix<T, TS>.GetDiag(matrix, k);
			diag.FillWith(T.Zero);
			AddBy(diag, value, T.One);
		}

		/// <inheritdoc/>
		public static void MatrixMultiplyVector(DenseMatrix<T, TS> matrix, SparseMatrix<T, TInd, TS, TSInd> vector, DenseVector<T, TS> vectorOut, T α, T β = default, MatrixOperation operation = MatrixOperation.None)
		{
			IMatrixVectorMultiplyOperations<T, SparseMatrix<T, TInd, TS, TSInd>, DenseVector<T, TS>, DenseMatrix<T, TS>>.CheckMatMulVec(matrix, vector, vectorOut, α, operation);
			SpComp.MatrixDenseMultiplyVectorSparse(operation, α, operation.CanInPlace() ? matrix.NRows : matrix.NCols, matrix.Storage, matrix.LeadDim, vector, β, vectorOut.Storage, vectorOut.Stride);
		}

		/// <inheritdoc/>
		public static void VectorMultiplyMatrix(SparseMatrix<T, TInd, TS, TSInd> vector, DenseMatrix<T, TS> matrix, DenseVector<T, TS> vectorOut, T α, T β = default, MatrixOperation operation = MatrixOperation.None) => MatrixMultiplyVector(matrix, vector, vectorOut, α, β, operation.Transpose());

		/// <summary>
		/// Statically compute the out-of-place addition for two <see cref="SparseMatrix{T, TInd, TS, TSInd}"/>s.
		/// </summary>
		/// <param name="scalarLeft">The scalar to multiply to <paramref name="left"/> during computation</param>
		/// <param name="left">The input left sparse vector</param>
		/// <param name="right">The input right sparse vector</param>
		/// <returns>The created new sparse vector as the addition result.</returns>
		/// <exception cref="ArgumentException">If <paramref name="scalarLeft"/> == 0 or <paramref name="left"/> and <paramref name="right"/> have different lengths</exception>
		public static SparseMatrix<T, TInd, TS, TSInd> VectorsAdd(T scalarLeft, SparseMatrix<T, TInd, TS, TSInd> left, SparseMatrix<T, TInd, TS, TSInd> right)
		{
			if (scalarLeft == T.Zero)
				throw new ArgumentException(Resources.ParameterError.CannotZero, nameof(scalarLeft));
			if (left.length != right.length)
				throw new ArgumentException(Resources.ParameterError.NotSameSize);
			var wrapper = new SparseArrayWrapper<T, TInd, TS, TSInd>(left.defaultValue + right.defaultValue, SparseFormat.Any);
			SpComp.VectorSparseAddSparse(scalarLeft, left, right, ref wrapper);
			return new(wrapper.Format, wrapper.Size[0], wrapper.ValueStorages[0], wrapper.IndexStorages[0], wrapper.BlockSize.IsEmpty ? TInd.Zero : wrapper.BlockSize[0], wrapper.IndexStorages.Length < 2 ? null : wrapper.IndexStorages[1], wrapper.IndexStorages.Length < 3 ? null : wrapper.IndexStorages[2]);
		}
		#endregion

		#region operators
		/// <inheritdoc/>
		public static SparseMatrix<T, TInd, TS, TSInd> operator -(SparseMatrix<T, TInd, TS, TSInd> vector!!) => vector.ApplyToClone(static v => v.Scale(-T.One));

		/// <inheritdoc/>
		public static SparseMatrix<T, TInd, TS, TSInd> operator *(SparseMatrix<T, TInd, TS, TSInd> vector!!, T scalar) => vector.ApplyToClone(v => v.Scale(scalar));

		/// <inheritdoc/>
		public static SparseMatrix<T, TInd, TS, TSInd> operator *(T scalar, SparseMatrix<T, TInd, TS, TSInd> vector!!) => vector * scalar;

		/// <inheritdoc/>
		public static SparseMatrix<T, TInd, TS, TSInd> operator /(SparseMatrix<T, TInd, TS, TSInd> vector!!, T scalar) => vector * (T.One / scalar);

		/// <inheritdoc/>
		public static DenseVector<T, TS> operator +(SparseMatrix<T, TInd, TS, TSInd> left!!, DenseVector<T, TS> right!!) => right.ApplyToClone(v => AddBy(v, left, T.One));

		/// <inheritdoc/>
		public static DenseVector<T, TS> operator -(DenseVector<T, TS> left!!, SparseMatrix<T, TInd, TS, TSInd> right!!) => left.ApplyToClone(v => AddBy(v, right, -T.One));

		/// <inheritdoc/>
		public static DenseVector<T, TS> operator -(SparseMatrix<T, TInd, TS, TSInd> left!!, DenseVector<T, TS> right!!) => right.ApplyToClone(v =>
		{
			AddBy(v, left, -T.One); v.Scale(-T.One);
		});

		/// <inheritdoc/>
		public static SparseMatrix<T, TInd, TS, TSInd> operator +(SparseMatrix<T, TInd, TS, TSInd> left!!, SparseMatrix<T, TInd, TS, TSInd> right!!) => VectorsAdd(T.One, left, right);

		/// <inheritdoc/>
		public static SparseMatrix<T, TInd, TS, TSInd> operator -(SparseMatrix<T, TInd, TS, TSInd> left!!, SparseMatrix<T, TInd, TS, TSInd> right!!) => VectorsAdd(-T.One, right, left);

		/// <inheritdoc/>
		public static DenseVector<T, TS> operator *(DenseMatrix<T, TS> matrix!!, SparseMatrix<T, TInd, TS, TSInd> vector!!)
		{
			if (matrix.NCols != vector.length)
				throw new ArgumentException(Resources.ParameterError.NotSameSize);
			var output = vector.values.ResizeAlike(matrix.NRows);
			try
			{
				SpComp.MatrixDenseMultiplyVectorSparse(MatrixOperation.None, T.One, matrix.NRows, matrix.Storage, matrix.LeadDim, vector, T.Zero, output, 1);
				return new(output, matrix.NRows);
			}
			catch (Exception)
			{
				output.Dispose();
				throw;
			}
		}

		/// <inheritdoc/>
		public static DenseVector<T, TS> operator *(SparseMatrix<T, TInd, TS, TSInd> vector!!, DenseMatrix<T, TS> matrix!!)
		{
			if (matrix.NRows != vector.length)
				throw new ArgumentException(Resources.ParameterError.NotSameSize);
			var output = vector.values.ResizeAlike(matrix.NCols);
			try
			{
				SpComp.MatrixDenseMultiplyVectorSparse(MatrixOperation.Transpose, T.One, matrix.NCols, matrix.Storage, matrix.LeadDim, vector, T.Zero, output, 1);
				return new(output, matrix.NCols);
			}
			catch (Exception)
			{
				output.Dispose();
				throw;
			}
		}
		#endregion

		#region conversion and clone
		/// <summary>
		/// Create a new dense vector of type <see cref="DenseVector{T, TS}"/> from this sparse vector.
		/// </summary>
		/// <returns>The created <see cref="DenseVector{T, TS}"/>.</returns>
		public DenseVector<T, TS> ToDense()
		{
			TS dense = this.values.ResizeAlike(this.length);
			SpConv.VectorSparseToDense(this, dense, 1);
			return new(dense, dense.Length);
		}

		/// <summary>
		/// Create a new sparse vector of type <see cref="SparseMatrix{T, TInd, TS, TSInd}"/> from <paramref name="dense"/> vector truncating by <paramref name="threshold"/>.
		/// </summary>
		/// <param name="dense">The input dense vector to convert</param>
		/// <param name="format">The target format</param>
		/// <param name="defaultValue">The target default value</param>
		/// <param name="threshold">The threshold used to truncate to sparse array</param>
		/// <returns>The created sparse vector of type <see cref="SparseMatrix{T, TInd, TS, TSInd}"/>.</returns>
		public static SparseMatrix<T, TInd, TS, TSInd> FromDense(DenseVector<T, TS> dense, SparseFormat format, T defaultValue, double threshold = 0)
		{
			var sparse = new SparseArrayWrapper<T, TInd, TS, TSInd>(defaultValue, format, default, default, default);
			SpConv.VectorDenseToSparse(ref sparse, dense.Storage, dense.Stride, threshold);
			try
			{
				if (sparse.ValueStorages.Length != 1)
					throw new InvalidOperationException();
				if (format.BlockType == SparseFormat.Blocking.Complicated)
				{
					return new(format, sparse.Size[0], sparse.ValueStorages[0], sparse.IndexStorages[0], sparse.BlockSize[0], sparse.IndexStorages[1], sparse.IndexStorages[2], defaultValue);
				}
				else
				{
					return new(format, sparse.Size[0], sparse.ValueStorages[0], sparse.IndexStorages[0], sparse.BlockSize[0], defaultValue: defaultValue);
				}
			}
			catch (Exception)
			{
				sparse.ValueStorages.ClearList();
				sparse.IndexStorages.ClearList();
				throw;
			}
		}


		/// <inheritdoc/>
		public SparseMatrix<T, TInd, TS, TSInd> CreateAlike()
		{
			return new(this.format, this.length, this.values.CreateAlike(), this.indices.CreateAlike(), this.blockSize, this.blockSizes?.CreateAlike(), this.blockSizesScan?.CreateAlike(), this.defaultValue);
		}
		#endregion

		#region serialization
		private record struct ElementRepr(int Format, T Default, long Length, TS Values, TSInd Indices);
		private record struct SimpleBlockRepr(int Format, T Default, long Length, TS Values, TSInd Indices, TInd BlockSize);
		private record struct ComplexBlockRepr(int Format, T Default, long Length, TS Values, TSInd Indices, TSInd BlockSizes);

		private SparseMatrix(ElementRepr repr) : this(new(repr.Format), repr.Length, repr.Values, repr.Indices, defaultValue: repr.Default) { }
		private SparseMatrix(SimpleBlockRepr repr) : this(new(repr.Format), repr.Length, repr.Values, repr.Indices, repr.BlockSize, defaultValue: repr.Default) { }
		private SparseMatrix(ComplexBlockRepr repr) : this(new(repr.Format), repr.Length, repr.Values, repr.Indices, TInd.Zero, repr.BlockSizes, defaultValue: repr.Default) { }

		private static readonly JsonSerializerOptions JsonOptions = new()
		{
			Converters = { TS.JsonConverter, TSInd.JsonConverter },
			WriteIndented = true,
		};

		/// <inheritdoc/>
		public string JsonSerialize()
		{
			return this.format.BlockType switch
			{
				SparseFormat.Blocking.Element => JsonSerializer.Serialize<ElementRepr>(new(this.format.Data, this.defaultValue, this.length, this.values, this.indices), JsonOptions),
				SparseFormat.Blocking.Simple => JsonSerializer.Serialize<SimpleBlockRepr>(new(this.format.Data, this.defaultValue, this.length, this.values, this.indices, this.blockSize), JsonOptions),
				SparseFormat.Blocking.Complicated => JsonSerializer.Serialize<ComplexBlockRepr>(new(this.format.Data, this.defaultValue, this.length, this.values, this.indices, this.BlockSizes), JsonOptions),
				_ => string.Empty,
			};
		}

		/// <inheritdoc/>
		public SparseMatrix<T, TInd, TS, TSInd> JsonDeserialize(string json!!)
		{
			return this.format.BlockType switch
			{
				SparseFormat.Blocking.Element => new(JsonSerializer.Deserialize<ElementRepr>(json, JsonOptions)),
				SparseFormat.Blocking.Simple => new(JsonSerializer.Deserialize<SimpleBlockRepr>(json, JsonOptions)),
				SparseFormat.Blocking.Complicated => new(JsonSerializer.Deserialize<ComplexBlockRepr>(json, JsonOptions)),
				_ => throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(json)),
			};
		}
		#endregion

		#region string
		static string IMainPropertyFormattable<SparseMatrix<T, TInd, TS, TSInd>>.StringMain => nameof(SparseMatrix<T, TInd, TS, TSInd>);

		static IEnumerable<string> IMainPropertyFormattable<SparseMatrix<T, TInd, TS, TSInd>>.PropertyNames => new[] { "DataType", "IndexType", "Format", "DefaultValue", "Values", "Indices", "BlockSizes" };

		IEnumerable<object?> IMainPropertyFormattable<SparseMatrix<T, TInd, TS, TSInd>>.PropertyValues => new object[] { Unmanaged<T>.DataType, Unmanaged<TInd>.DataType, this.format, this.defaultValue, this.values, this.indices, this.blockSizes ?? (object)(this.blockSize == TInd.Zero ? 1 : this.blockSize) };

		/// <inheritdoc/>
		public override string ToString() => IMainPropertyFormattable<SparseMatrix<T, TInd, TS, TSInd>>.ToString(this);

		/// <inheritdoc/>
		public unsafe string Print(PrintSettings? settings = null)
		{
			settings ??= Settings.PrintSetting;
			int length = Math.Min((int)this.values.Length, settings.Value.ArrayLength);
			Span<T> values = length.CheckStackLimit<T>() ?? stackalloc T[length];
			Span<long> indices = length.CheckStackLimit<long>() ?? stackalloc long[length];
			this.Storage.ToManaged(values);
			switch (this.format.BlockType)
			{
				case SparseFormat.Blocking.Element:
					fixed (long* inds = indices)
					{
						var mp = new ManagedPureStorage<long>(new ManagedPointer(new(inds), length * sizeof(long)));
						ExtBlas.PointWiseCast<TInd, long, TSInd, ManagedPureStorage<long>>(this.indices, 1, mp, 1);
					}
					break;
				case SparseFormat.Blocking.Simple:
					int bs = this.blockSize.As<TInd, int>();
					fixed (long* inds = indices)
					{
						var mp = new ManagedPureStorage<long>(new ManagedPointer(new(inds), length * sizeof(long)));
						ExtBlas.PointWiseCast<TInd, long, TSInd, ManagedPureStorage<long>>(this.indices, 1, mp, bs);
					}
					for (int i = 0; i < length; i++)
					{
						int diff = i % bs;
						indices[i] = indices[i - diff] + diff;
					}
					break;
				case SparseFormat.Blocking.Complicated:
					int blocks = 1 + (int)SpConv.IndexBound(this.BlockSizesScan, TInd.Create(length), true);
					Span<long> tempInds = blocks.CheckStackLimit<long>() ?? stackalloc long[blocks];
					Span<long> tempSize = blocks.CheckStackLimit<long>() ?? stackalloc long[blocks];
					fixed (long* tInd = tempInds)
					fixed (long* tSiz = tempSize)
					{
						ExtBlas.PointWiseCast<TInd, long, TSInd, ManagedPureStorage<long>>(this.indices, 1, new(new(new(tInd), tempInds.Length * sizeof(long))), 1);
						ExtBlas.PointWiseCast<TInd, long, TSInd, ManagedPureStorage<long>>(this.BlockSizes, 1, new(new(new(tSiz), tempInds.Length * sizeof(long))), 1);
					}
					for (int i = 0; i < blocks; i++)
					{
						for (int j = 0; j < tempSize[i]; j++)
						{
							indices[i + j] = tempInds[i] + j;
						}
					}
					break;
			}
			return values.ToSparseVectorString(indices, settings.Value.Precision) + (length == this.values.Length ? "" : string.Format(Resources.Print.MoreStored, this.values.Length - length));
		}
		#endregion
	}
}
