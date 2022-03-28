using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;

using Althea.Helpers;
using Althea.LinearAlgebra;
using Althea.LinearAlgebra.Sparse;
using Althea.Linq;
using Althea.NativeTypes;
using Althea.Storage;

using Blas = Althea.LinearAlgebra.Dense.BlasApiSelector;
using ExtBlas = Althea.LinearAlgebra.Dense.ExtendBlasApiSelector;
using SpComp = Althea.LinearAlgebra.Sparse.ComputationApiSelector;
using SpConv = Althea.LinearAlgebra.Sparse.ConversionApiSelector;


namespace Althea.Arrays
{
	/// <summary>
	/// The coordinated non-blocked (or blocked) sparse vector class whose value storage is of type <typeparamref name="TS"/> and sorted index storage is of type <typeparamref name="TSInd"/>.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TS">The storage type used by the value storage</typeparam>
	/// <typeparam name="TInd">Any unmanaged integer number as the index type</typeparam>
	/// <typeparam name="TSInd">The index storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
	[StructLayout(LayoutKind.Sequential)]
	public class SparseVector<T, TInd, TS, TSInd> : ISparseArray<T, TInd, TS, TSInd>,
		IBaseVector<T, SparseVector<T, TInd, TS, TSInd>>,
		IVectorOperations<T, DenseVector<T, TS>, SparseVector<T, TInd, TS, TSInd>>,
		IVectorUnaryOperators<T, SparseVector<T, TInd, TS, TSInd>, SparseVector<T, TInd, TS, TSInd>>,
		IVectorBinaryOperators<T, SparseVector<T, TInd, TS, TSInd>, DenseVector<T, TS>, DenseVector<T, TS>>,
		IVectorBinaryOperators<T, SparseVector<T, TInd, TS, TSInd>, SparseVector<T, TInd, TS, TSInd>, SparseVector<T, TInd, TS, TSInd>>,
		IMatrixSetDiagonalVector<T, SparseVector<T, TInd, TS, TSInd>, DenseMatrix<T, TS>>,
		IMatrixVectorMultiplyOperations<T, SparseVector<T, TInd, TS, TSInd>, DenseVector<T, TS>, DenseMatrix<T, TS>>,
		IVectorMatrixMultiplyOperators<T, SparseVector<T, TInd, TS, TSInd>, DenseVector<T, TS>, DenseMatrix<T, TS>>
		where T : unmanaged, INumber<T> where TInd : unmanaged, IBinaryInteger<TInd>
		where TS : class, IStorage<T, TS> where TSInd : class, IStorage<TInd, TSInd>
	{
		#region basic
		private readonly long length;

		private readonly TSInd indices;
		private readonly TSInd? blockSizes, blockSizesScan;

		private readonly TS values;

		private readonly TInd blockSize;
		private readonly SparseFormat format;
		private readonly T defaultValue;

		ReadOnlySpan<long> IValueArray<T, SparseVector<T, TInd, TS, TSInd>>.Size => ReflectionHelper.CreateReadOnlySpan(in this.length, 1);

		ReadOnlySpan<long> ISparseArray<T>.Size => ReflectionHelper.CreateReadOnlySpan(in this.length, 1);
		ReadOnlySpan<TS> ISparseArray<T, TInd, TS, TSInd>.ValueStorages => ReflectionHelper.CreateReadOnlySpan(in this.values, 1);
		ReadOnlySpan<TSInd> ISparseArray<T, TInd, TS, TSInd>.IndexStorages => ReflectionHelper.CreateReadOnlySpan(in this.indices, this.format.BlockType == SparseFormat.Blocking.Complicated ? 3 : 1);
		ReadOnlySpan<TInd> ISparseArray<T, TInd, TS, TSInd>.BlockSize => this.format.BlockType == SparseFormat.Blocking.Simple ? ReflectionHelper.CreateReadOnlySpan(in this.blockSize, 1) : default;

		bool ICheckValid.IsValid() => (this.values?.IsValid() ?? false) && (this.indices?.IsValid() ?? false);

		/// <inheritdoc/>
		public SparseFormat Format => this.format;

		/// <inheritdoc/>
		public T DefaultValue => this.defaultValue;

		/// <summary>
		/// Get the value storage of this vector as a <typeparamref name="TS"/>.
		/// </summary>
		public TS Storage => this.values.MakeReference();

		/// <summary>
		/// Get the index array's storage of this sparse vector.
		/// </summary>
		public TSInd IndexStorage => this.indices.MakeReference();

		/// <inheritdoc/>
		public long NStored => this.values.Length;

		/// <inheritdoc/>
		public long Length => this.length;

		/// <summary>
		/// Get the index array's original storage of this sparse vector.
		/// </summary>
		protected TSInd OrginalIndexStorage => this.indices;

		/// <summary>
		/// Get the block size array's original storage of this sparse vector which shall be null if <see cref="ISparseArray{T}.Format"/> is not of <see cref="SparseFormat.Blocking.Complicated"/>.
		/// </summary>
		protected TSInd BlockSizes => this.blockSizes ?? TSInd.Empty;

		/// <summary>
		/// Get the block size array's accumulation array's original storage of this sparse vector which shall be null if <see cref="ISparseArray{T}.Format"/> is not of <see cref="SparseFormat.Blocking.Complicated"/>.
		/// </summary>
		protected TSInd BlockSizesScan => this.blockSizesScan ?? TSInd.Empty;

		/// <summary>
		/// Get the block size if <see cref="ISparseArray{T}.Format"/> is not of <see cref="SparseFormat.Blocking.Simple"/>.
		/// </summary>
		protected TInd BlockSize => this.blockSize;

		/// <summary>
		/// Create a new <see cref="SparseVector{T, TInd, TS, TSInd}"/> with given parameters.
		/// </summary>
		/// <param name="format">The <see cref="SparseFormat"/></param>
		/// <param name="defaultValue">The default value</param>
		/// <param name="length">The presenting length</param>
		/// <param name="values">The original value array</param>
		/// <param name="indices">The original index array</param>
		/// <param name="blockSize">The constant block size, must be 0 if the <see cref="ISparseArray{T}.Format"/> does not indicate it</param>
		/// <param name="blockSizes">The original block size array, must be null if the <see cref="ISparseArray{T}.Format"/> does not indicate it</param>
		/// <param name="blockSizesScan">The original block size scan array, must be null if <paramref name="blockSizes"/> is null, can be null if it does not to let this constructor to compute a new one</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="length"/> ≤ 0</exception>
		/// <exception cref="ArgumentException">If <paramref name="format"/> is not supported or <paramref name="values"/> is too short</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="blockSizes"/> is null when it shall not be according to <paramref name="format"/></exception>
		/// <remarks>The validness of the detail values (such as sorted or not) in these storages are not checked for performance issues.</remarks>
		public SparseVector(SparseFormat format, long length, TS values!!, TSInd indices!!, TInd blockSize = default, TSInd? blockSizes = null, TSInd? blockSizesScan = null, T defaultValue = default)
		{
			if (!format.IsAtomic || format.Class != SparseFormat.Type.Coordinated || format.MajorType != SparseFormat.Major.None)
				throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(format));
			if (format.BlockType != SparseFormat.Blocking.Element && format.BlockType != SparseFormat.Blocking.Simple && format.BlockType != SparseFormat.Blocking.Complicated)
				throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(format));
			this.format = format;
			this.defaultValue = defaultValue;
			if (length < 0)
				throw new ArgumentOutOfRangeException(nameof(length), Resources.ParameterError.CannotNegative);
			this.length = length;
			if (length < values.Length || values.Length < indices.Length)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(values));
			switch (this.format.BlockType)
			{
				case SparseFormat.Blocking.Element:
					if (values.Length != indices.Length)
						throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(values));
					if (blockSizes is not null || blockSizesScan is not null || blockSize != TInd.Zero)
						throw new ArgumentException(Resources.SparseError.FormatNotSupport);
					break;
				case SparseFormat.Blocking.Simple: // TODO : Simple, Complicated wrong here
					if (blockSizes is not null || blockSizesScan is not null)
						throw new ArgumentException(Resources.SparseError.FormatNotSupport);
					if (blockSize <= TInd.Zero)
						throw new ArgumentOutOfRangeException(nameof(blockSize), Resources.ParameterError.MustPositive);
					if (length % blockSize.As<TInd, long>() != 0)
						throw new ArgumentException(Resources.ArithmeticError.CannotDivide, nameof(blockSize));
					if (values.Length != indices.Length * blockSize.As<TInd, long>())
						throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(values));
					break;
				case SparseFormat.Blocking.Complicated:
					if (blockSize != TInd.Zero)
						throw new ArgumentException(Resources.SparseError.FormatNotSupport);
					if (blockSizes is null)
						throw new ArgumentNullException(nameof(blockSizes));
					if (blockSizes.Length != indices.Length)
						throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(blockSizes));
					if (blockSizesScan is not null)
					{
						if (blockSizesScan.Length != indices.Length)
							throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(blockSizesScan));
						break;
					}
					blockSizesScan = blockSizes.ApplyToAlike(static (org, @new) => ExtBlas.PartialSum<TInd, TSInd, TSInd>(org, 1, @new, 1, false));
					break;
				default:
					break;
			}
			this.values = values.AddToManager();
			this.indices = indices.AddToManager();
			this.blockSize = blockSize;
			this.blockSizes = blockSizes?.AddToManager();
			this.blockSizesScan = blockSizesScan?.AddToManager();
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			this.values.SafeDispose();
			this.indices.SafeDispose();
			this.blockSizes.SafeDispose();
			this.blockSizesScan.SafeDispose();
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Deconstructor to be invoked by GC.
		/// </summary>
		~SparseVector()
		{
			this.Dispose();
		}

		private SparseVector()
		{
			this.values = TS.Empty; this.indices = TSInd.Empty;
		}

		static SparseVector<T, TInd, TS, TSInd> IValueArray<T, SparseVector<T, TInd, TS, TSInd>>.Empty => new();
		#endregion

		#region equality
		/// <inheritdoc/>
		public bool Equals(SparseVector<T, TInd, TS, TSInd>? other)
		{
			if (other is null)
				return false;
			return this.format == other.format && this.defaultValue == other.defaultValue&&
				this.values == other.values && this.indices == other.indices && this.blockSize == other.blockSize &&
				(ReferenceEquals(this.blockSizes, other.blockSizes) || (this.blockSizes is not null && other.blockSizes is not null && this.blockSizes == other.blockSizes)) &&
				(ReferenceEquals(this.blockSizesScan, other.blockSizesScan) || (this.blockSizesScan is not null && other.blockSizesScan is not null && this.blockSizesScan == other.blockSizesScan));
		}

		/// <summary>
		/// Equality operator
		/// </summary>
		public static bool operator ==(SparseVector<T, TInd, TS, TSInd> left, SparseVector<T, TInd, TS, TSInd> right) => left.Equals(right);

		/// <summary>
		/// Inequality operator
		/// </summary>
		public static bool operator !=(SparseVector<T, TInd, TS, TSInd> left, SparseVector<T, TInd, TS, TSInd> right) => !left.Equals(right);

		/// <inheritdoc/>
		public override bool Equals(object? obj) => this.Equals(obj as SparseVector<T, TInd, TS, TSInd>);

		/// <inheritdoc/>
		public override int GetHashCode() => HashCode.Combine(this.format, this.defaultValue, this.values, this.indices, this.blockSize, this.blockSizes, this.blockSizesScan);
		#endregion

		#region index
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private long GetOffset(long index)
		{
			IBaseVector<T, SparseVector<T, TInd, TS, TSInd>>.CheckIndex(this, index);
			long offset;
			if (this.format.BlockType == SparseFormat.Blocking.Element)
			{
				offset = SpConv.IndexFind(this.indices, true, TInd.Create(index));
			}
			else
			{
				long blockIndex = SpConv.IndexBound(this.indices, TInd.Create(index + 1), true) - 1;
				long blockSize = this.blockSizes is null ? this.blockSize.As<TInd, long>() : (this.blockSizes + blockIndex).ToManaged<TInd, TSInd>().As<TInd, long>();
				long blockOffset = (this.indices + blockIndex).ToManaged<TInd, TSInd>().As<TInd, long>();
				offset = index - blockOffset;
				if (offset >= blockSize)
					offset = -1;
				else
					offset += this.blockSizesScan is null ? blockSize * blockIndex : (this.blockSizesScan + blockIndex).ToManaged<TInd, TSInd>().As<TInd, long>();
			}
			return offset;
		}

		/// <inheritdoc/>
		public T this[long index]
		{
			get
			{
				long offset = this.GetOffset(index);
				return offset < 0 ? this.defaultValue : (this.values + offset).ToManaged<T, TS>();
			}
			set
			{
				long offset = this.GetOffset(index);
				if (offset < 0)
					throw new ArgumentException(Resources.SparseError.CannotSetSparse, nameof(index));
				(this.values + offset).FromManaged(value);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private (long indexStart, long indexCount, long valueStart, long valueCount, long blockStartOffset, long blockEndSize) GetSliceInfo(long start, long count, SparseVector<T, TInd, TS, TSInd>? sub = null)
		{
			IBaseVector<T, SparseVector<T, TInd, TS, TSInd>>.CheckRange(this, start, count, sub);
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
		public SparseVector<T, TInd, TS, TSInd> GetSlice(long start, long count)
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
		public void GetSlice(long start, long count, SparseVector<T, TInd, TS, TSInd> overwrite)
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
		public void CopyTo(SparseVector<T, TInd, TS, TSInd> destination)
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
		public void SetSlice(long start, long count, SparseVector<T, TInd, TS, TSInd> value)
		{
			var (indexStart, indexCount, valueStart, valueCount, blockStartOffset, blockEndSize) = this.GetSliceInfo(start, count, value);
			if (indexStart == 0 && indexCount == this.indices.Length && valueStart == 0 && valueCount == this.values.Length)
			{
				value.CopyTo((SparseVector<T, TInd, TS, TSInd>)this);
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
		public static T Dot(DenseVector<T, TS> left, SparseVector<T, TInd, TS, TSInd> right, bool conjugateLeft = true)
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
		public static T Dot(SparseVector<T, TInd, TS, TSInd> left, SparseVector<T, TInd, TS, TSInd> right, bool conjugateLeft = true)
		{
			if (left.Length != right.Length)
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(right));
			return SpComp.VectorSparseDotSparse(conjugateLeft, right, left);
		}

		/// <inheritdoc/>
		public static void AddBy(DenseVector<T, TS> left, SparseVector<T, TInd, TS, TSInd> right, T scalar)
		{
			if (left.Length != right.Length)
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(right));
			SpComp.VectorSparseAddToDense(scalar, right, left.Storage, left.Stride);
		}

		/// <inheritdoc/>
		public static void SetDiag(DenseMatrix<T, TS> matrix, long k, SparseVector<T, TInd, TS, TSInd> value)
		{
			var diag = DenseMatrix<T, TS>.GetDiag(matrix, k);
			diag.FillWith(T.Zero);
			AddBy(diag, value, T.One);
		}

		/// <inheritdoc/>
		public static void MatrixMultiplyVector(DenseMatrix<T, TS> matrix, SparseVector<T, TInd, TS, TSInd> vector, DenseVector<T, TS> vectorOut, T α, T β = default, MatrixOperation operation = MatrixOperation.None)
		{
			IMatrixVectorMultiplyOperations<T, SparseVector<T, TInd, TS, TSInd>, DenseVector<T, TS>, DenseMatrix<T, TS>>.CheckMatMulVec(matrix, vector, vectorOut, α, operation);
			SpComp.MatrixDenseMultiplyVectorSparse(operation, α, operation.CanInPlace() ? matrix.NRows : matrix.NCols, matrix.Storage, matrix.LeadDim, vector, β, vectorOut.Storage, vectorOut.Stride);
		}

		/// <inheritdoc/>
		public static void VectorMultiplyMatrix(SparseVector<T, TInd, TS, TSInd> vector, DenseMatrix<T, TS> matrix, DenseVector<T, TS> vectorOut, T α, T β = default, MatrixOperation operation = MatrixOperation.None) => MatrixMultiplyVector(matrix, vector, vectorOut, α, β, operation.Transpose());

		/// <summary>
		/// Statically compute the out-of-place addition for two <see cref="SparseVector{T, TInd, TS, TSInd}"/>s.
		/// </summary>
		/// <param name="scalarLeft">The scalar to multiply to <paramref name="left"/> during computation</param>
		/// <param name="left">The input left sparse vector</param>
		/// <param name="right">The input right sparse vector</param>
		/// <returns>The created new sparse vector as the addition result.</returns>
		/// <exception cref="ArgumentException">If <paramref name="scalarLeft"/> == 0 or <paramref name="left"/> and <paramref name="right"/> have different lengths</exception>
		public static SparseVector<T, TInd, TS, TSInd> VectorsAdd(T scalarLeft, SparseVector<T, TInd, TS, TSInd> left, SparseVector<T, TInd, TS, TSInd> right)
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
		public static SparseVector<T, TInd, TS, TSInd> operator -(SparseVector<T, TInd, TS, TSInd> vector!!) => vector.ApplyToClone(static v => v.Scale(-T.One));

		/// <inheritdoc/>
		public static SparseVector<T, TInd, TS, TSInd> operator *(SparseVector<T, TInd, TS, TSInd> vector!!, T scalar) => vector.ApplyToClone(v => v.Scale(scalar));

		/// <inheritdoc/>
		public static SparseVector<T, TInd, TS, TSInd> operator *(T scalar, SparseVector<T, TInd, TS, TSInd> vector!!) => vector * scalar;

		/// <inheritdoc/>
		public static SparseVector<T, TInd, TS, TSInd> operator /(SparseVector<T, TInd, TS, TSInd> vector!!, T scalar) => vector * (T.One / scalar);

		/// <inheritdoc/>
		public static DenseVector<T, TS> operator +(SparseVector<T, TInd, TS, TSInd> left!!, DenseVector<T, TS> right!!) => right.ApplyToClone(v => AddBy(v, left, T.One));

		/// <inheritdoc/>
		public static DenseVector<T, TS> operator -(DenseVector<T, TS> left!!,  SparseVector<T, TInd, TS, TSInd> right!!) => left.ApplyToClone(v => AddBy(v, right, -T.One));

		/// <inheritdoc/>
		public static DenseVector<T, TS> operator -(SparseVector<T, TInd, TS, TSInd> left!!, DenseVector<T, TS> right!!) => right.ApplyToClone(v =>
		{
			AddBy(v, left, -T.One); v.Scale(-T.One);
		});

		/// <inheritdoc/>
		public static SparseVector<T, TInd, TS, TSInd> operator +(SparseVector<T, TInd, TS, TSInd> left!!, SparseVector<T, TInd, TS, TSInd> right!!) => VectorsAdd(T.One, left, right);

		/// <inheritdoc/>
		public static SparseVector<T, TInd, TS, TSInd> operator -(SparseVector<T, TInd, TS, TSInd> left!!, SparseVector<T, TInd, TS, TSInd> right!!) => VectorsAdd(-T.One, right, left);

		/// <inheritdoc/>
		public static DenseVector<T, TS> operator *(DenseMatrix<T, TS> matrix!!, SparseVector<T, TInd, TS, TSInd> vector!!)
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
		public static DenseVector<T, TS> operator *(SparseVector<T, TInd, TS, TSInd> vector!!, DenseMatrix<T, TS> matrix!!)
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
		/// Create a new sparse vector of type <see cref="SparseVector{T, TInd, TS, TSInd}"/> from <paramref name="dense"/> vector truncating by <paramref name="threshold"/>.
		/// </summary>
		/// <param name="dense">The input dense vector to convert</param>
		/// <param name="format">The target format</param>
		/// <param name="defaultValue">The target default value</param>
		/// <param name="threshold">The threshold used to truncate to sparse array</param>
		/// <returns>The created sparse vector of type <see cref="SparseVector{T, TInd, TS, TSInd}"/>.</returns>
		public static SparseVector<T, TInd, TS, TSInd> FromDense(DenseVector<T, TS> dense, SparseFormat format, T defaultValue, double threshold = 0)
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
		public SparseVector<T, TInd, TS, TSInd> CreateAlike()
		{
			return new(this.format, this.length, this.values.CreateAlike(), this.indices.CreateAlike(), this.blockSize, this.blockSizes?.CreateAlike(), this.blockSizesScan?.CreateAlike(), this.defaultValue);
		}
		#endregion

		#region serialization
		private record struct ElementRepr(int Format, T Default, long Length, TS Values, TSInd Indices);
		private record struct SimpleBlockRepr(int Format, T Default, long Length, TS Values, TSInd Indices, TInd BlockSize);
		private record struct ComplexBlockRepr(int Format, T Default, long Length, TS Values, TSInd Indices, TSInd BlockSizes);

		private SparseVector(ElementRepr repr) : this(new(repr.Format), repr.Length, repr.Values, repr.Indices, defaultValue: repr.Default) { }
		private SparseVector(SimpleBlockRepr repr) : this(new(repr.Format), repr.Length, repr.Values, repr.Indices, repr.BlockSize, defaultValue: repr.Default) { }
		private SparseVector(ComplexBlockRepr repr) : this(new(repr.Format), repr.Length, repr.Values, repr.Indices, TInd.Zero, repr.BlockSizes, defaultValue: repr.Default) { }

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
		public SparseVector<T, TInd, TS, TSInd> JsonDeserialize(string json!!)
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
		static string IMainPropertyFormattable<SparseVector<T, TInd, TS, TSInd>>.StringMain => nameof(SparseVector<T, TInd, TS, TSInd>);

		static IEnumerable<string> IMainPropertyFormattable<SparseVector<T, TInd, TS, TSInd>>.PropertyNames => new[] { "DataType", "IndexType", "Format", "DefaultValue", "Values", "Indices", "BlockSizes" };

		IEnumerable<object?> IMainPropertyFormattable<SparseVector<T, TInd, TS, TSInd>>.PropertyValues => new object[] { Unmanaged<T>.DataType, Unmanaged<TInd>.DataType, this.format, this.defaultValue, this.values, this.indices, this.blockSizes ?? (object)(this.blockSize == TInd.Zero ? 1 : this.blockSize) };

		/// <inheritdoc/>
		public override string ToString() => IMainPropertyFormattable<SparseVector<T, TInd, TS, TSInd>>.ToString(this);

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
