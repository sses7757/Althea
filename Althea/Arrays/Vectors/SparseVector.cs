using System;
using System.Runtime.CompilerServices;

using Althea.Helpers;
using Althea.Linq;
using Althea.LinearAlgebra;
using Althea.LinearAlgebra.Sparse;
using Althea.NativeTypes;
using Althea.Storage;

using Blas = Althea.LinearAlgebra.Dense.BlasApiSelector;
using ExtBlas = Althea.LinearAlgebra.Dense.ExtendBlasApiSelector;
using SpConv = Althea.LinearAlgebra.Sparse.ConversionApiSelector;
using System.Runtime.InteropServices;

namespace Althea.Arrays
{
	/// <summary>
	/// The coordinated non-blocked (or blocked) sparse vector class whose value storage is of type <typeparamref name="TS"/> and sorted index storage is of type <typeparamref name="TSInd"/>.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TS">The storage type used by the value <see cref="ISingleValueStorageArray{T, TS, TSelf}.Storage"/></typeparam>
	/// <typeparam name="TInd">Any unmanaged integer number as the index type</typeparam>
	/// <typeparam name="TSInd">The index storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
	/// <typeparam name="TStatic">The concrete type that implements <see cref="ISparseArrayStatic{T}"/></typeparam>
	[StructLayout(LayoutKind.Explicit)]
	public class SparseVector<T, TInd, TS, TSInd, TStatic> :
		IBaseVector<T, SparseVector<T, TInd, TS, TSInd, TStatic>>,
		ISingleValueStorageArray<T, TS, SparseVector<T, TInd, TS, TSInd, TStatic>>
		where T : unmanaged, INumber<T> where TInd : unmanaged, IBinaryInteger<TInd>
		where TS : class, IStorage<T, TS> where TSInd : class, IStorage<TInd, TSInd>
		where TStatic : struct, ISparseArrayStatic<T>
	{
		#region basic
		[FieldOffset(0)]
		private long length;
		[FieldOffset(sizeof(long) * 1)]
		private readonly TS values;
		[FieldOffset(sizeof(long) * 2)]
		private readonly TSInd indices;
		[FieldOffset(sizeof(long) * 3)]
		private readonly long blockSize;
		[FieldOffset(sizeof(long) * 4)]
		private readonly TSInd? blockSizes;
		[FieldOffset(sizeof(long) * 5)]
		private readonly TSInd? blockSizesScan;

		ReadOnlySpan<long> IValueArray<T, SparseVector<T, TInd, TS, TSInd, TStatic>>.Size => MemoryMarshal.CreateReadOnlySpan(ref this.length, 1);

		TS ISingleValueStorageArray<T, TS, SparseVector<T, TInd, TS, TSInd, TStatic>>.OriginalStorage => this.values;

		bool ICheckValid.IsValid() => (this.values?.IsValid() ?? false) && (this.indices?.IsValid() ?? false);

		/// <inheritdoc/>
		public TS Storage => this.OriginalStorage.MakeReference();

		/// <summary>
		/// Get the index array's storage of this sparse vector.
		/// </summary>
		public TSInd IndexStorage => this.OrginalIndexStorage.MakeReference();

		/// <summary>
		/// Get the presenting length of this sparse vector.
		/// </summary>
		public long NStored => this.OriginalStorage.Length;

		/// <inheritdoc/>
		public long Length => this.length;

		/// <summary>
		/// Get the value array's original storage of this sparse vector.
		/// </summary>
		protected TS OriginalStorage => this.values;

		/// <summary>
		/// Get the index array's original storage of this sparse vector.
		/// </summary>
		protected TSInd OrginalIndexStorage => this.indices;

		/// <summary>
		/// Get the block size array's original storage of this sparse vector which shall be null if <see cref="ISparseArrayStatic{T}.Format"/> is not of <see cref="SparseFormat.Blocking.Complicated"/>.
		/// </summary>
		protected TSInd BlockSizes => this.blockSizes ?? TSInd.Empty;

		/// <summary>
		/// Get the block size array's accumulation array's original storage of this sparse vector which shall be null if <see cref="ISparseArrayStatic{T}.Format"/> is not of <see cref="SparseFormat.Blocking.Complicated"/>.
		/// </summary>
		/// <remarks>This array's first element shall be 0 and the last element in <see cref="BlockSizes"/> shall not be accumulated (a exclusive scan).</remarks>
		protected TSInd BlockSizesScan => this.blockSizesScan ?? TSInd.Empty;

		/// <summary>
		/// Get the block size if <see cref="ISparseArrayStatic{T}.Format"/> is not of <see cref="SparseFormat.Blocking.Simple"/>.
		/// </summary>
		protected long BlockSize => this.blockSize;

		/// <summary>
		/// Create a new <see cref="SparseVector{T, TInd, TS, TSInd, TStatic}"/> with given parameters.
		/// </summary>
		/// <param name="length">The presenting length</param>
		/// <param name="values">The original value array</param>
		/// <param name="indices">The original index array</param>
		/// <param name="blockSize">The constant block size, must be 0 if the <see cref="ISparseArrayStatic{T}.Format"/> does not indicate it</param>
		/// <param name="blockSizes">The original block size array, must be null if the <see cref="ISparseArrayStatic{T}.Format"/> does not indicate it</param>
		/// <param name="blockSizesScan">The original block size scan array, must be null if <paramref name="blockSizes"/> is null, can be null if it does not to let this constructor to compute a new one</param>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="ArgumentNullException"></exception>
		protected SparseVector(long length, TS values!!, TSInd indices!!, long blockSize = 0, TSInd? blockSizes = null, TSInd? blockSizesScan = null)
		{
			if (length < 0)
				throw new ArgumentOutOfRangeException(nameof(length), Resources.ParameterError.CannotNegative);
			this.length = length;
			if (length < values.Length || values.Length < indices.Length)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(values));
			switch (TStatic.Format.BlockType)
			{
				case SparseFormat.Blocking.Element:
					if (values.Length != indices.Length)
						throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(values));
					if (blockSizes is not null || blockSizesScan is not null || blockSize != 0)
						throw new ArgumentException(Resources.SparseError.FormatNotSupport);
					break;
				case SparseFormat.Blocking.Simple:
					if (blockSizes is not null || blockSizesScan is not null)
						throw new ArgumentException(Resources.SparseError.FormatNotSupport);
					if (blockSize <= 0)
						throw new ArgumentOutOfRangeException(nameof(blockSize), Resources.ParameterError.MustPositive);
					if (values.Length != indices.Length * blockSize)
						throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(values));
					break;
				case SparseFormat.Blocking.Complicated:
					if (blockSize != 0)
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
					blockSizesScan = blockSizes.ApplyToAlike(static (org, @new) => SpConv.IndexScan<TInd, TInd, TSInd, TSInd>(org, @new, false));
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
		public virtual void Dispose()
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
		#endregion

		#region static
		private SparseVector()
		{
			this.values = TS.Empty; this.indices = TSInd.Empty; this.blockSize = 0;
		}

		static SparseVector<T, TInd, TS, TSInd, TStatic> IValueArray<T, SparseVector<T, TInd, TS, TSInd, TStatic>>.Empty => new();

		/// <summary>
		/// Get the supported <see cref="SparseFormat"/>s of this abstract sparse vector.
		/// </summary>
		protected static readonly SparseFormat SupportFormats = new(SparseFormat.Type.Coordinated, SparseFormat.Blocking.Element | SparseFormat.Blocking.Simple | SparseFormat.Blocking.Complicated, SparseFormat.Major.None);

		static SparseVector()
		{
			if ((TStatic.Format & SupportFormats) != TStatic.Format)
				throw new NotSupportedException(Resources.SparseError.FormatNotSupport);
		}
		#endregion

		#region index
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private long GetOffset(long index)
		{
			((IBaseVector<T, SparseVector<T, TInd, TS, TSInd, TStatic>>)this).CheckIndex(index);
			long offset;
			if (TStatic.Format.BlockType == SparseFormat.Blocking.Element)
			{
				offset = SpConv.IndexFind(this.indices, true, TInd.Create(index));
			}
			else
			{
				long blockIndex = SpConv.IndexBound(this.indices, TInd.Create(index + 1), true) - 1;
				long blockSize = this.blockSizes is null ? this.blockSize : (this.blockSizes + blockIndex).ToManaged<TInd, TSInd>().As<TInd, long>();
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
				return offset < 0 ? TStatic.DefaultValue : (this.values + offset).ToManaged<T, TS>();
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
		private (long indexStart, long indexCount, long valueStart, long valueCount, long blockStartOffset, long blockEndSize) GetSliceInfo(long start, long count, SparseVector<T, TInd, TS, TSInd, TStatic>? sub = null)
		{
			((IBaseVector<T, SparseVector<T, TInd, TS, TSInd, TStatic>>)this).CheckRange(start, count, sub);
			long indexStart, indexCount;
			long valueStart, valueCount;
			long blockStartOffset = 0, blockEndSize = 0;
			if (TStatic.Format.BlockType == SparseFormat.Blocking.Element)
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
					long blockSize = this.blockSizes is null ? this.blockSize : (this.blockSizes + indexOffset).ToManaged<TInd, TSInd>().As<TInd, long>();
					long blockOffset = (this.indices + indexOffset).ToManaged<TInd, TSInd>().As<TInd, long>();
					blockOffset = allOffset - blockOffset;
					long valueOffset;
					if (blockOffset > 0 && blockOffset < blockSize)
					{
						if (TStatic.Format.BlockType == SparseFormat.Blocking.Simple)
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
		public SparseVector<T, TInd, TS, TSInd, TStatic> GetSlice(long start, long count)
		{
			var (indexStart, indexCount, valueStart, valueCount, blockStartOffset, blockEndSize) = this.GetSliceInfo(start, count);
			if (indexStart == 0 && indexCount == this.indices.Length && valueStart == 0 && valueCount == this.values.Length)
				return new(count, this.values.Clone(), this.indices.Clone(), this.blockSize, this.blockSizes?.Clone(), this.blockSizesScan?.Clone());

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
			return new(count, newVals, newInds, this.blockSize, bs, bsa);
		}

		/// <inheritdoc/>
		public void GetSlice(long start, long count, SparseVector<T, TInd, TS, TSInd, TStatic> overwrite)
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
		public void CopyTo(SparseVector<T, TInd, TS, TSInd, TStatic> destination)
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
		public void SetSlice(long start, long count, SparseVector<T, TInd, TS, TSInd, TStatic> value)
		{
			var (indexStart, indexCount, valueStart, valueCount, blockStartOffset, blockEndSize) = this.GetSliceInfo(start, count, value);
			if (indexStart == 0 && indexCount == this.indices.Length && valueStart == 0 && valueCount == this.values.Length)
			{
				value.CopyTo((SparseVector<T, TInd, TS, TSInd, TStatic>)this);
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
		public virtual void FillWith(T value)
		{
			if (value != TStatic.DefaultValue)
				throw new ArgumentException(Resources.SparseError.CannotSetSparse, nameof(value));
			this.values.FillWith(value);
		}

		/// <inheritdoc/>
		public virtual void AddScalar(T value)
		{
			if (value != T.Zero)
				throw new ArgumentException(Resources.SparseError.CannotSetSparse, nameof(value));
		}

		/// <inheritdoc/>
		public virtual void Scale(T value)
		{
			if (TStatic.DefaultValue == T.Zero)
				Blas.Scale(this.values, 1, value);
			else if (value != T.One)
				throw new ArgumentException(Resources.SparseError.CannotSetSparse, nameof(value));
		}

		/// <inheritdoc/>
		public virtual void Conjugate()
		{
			if (NumberType<T>.IsComplex)
			{
				if (NumberType<T>.IsRealValue(TStatic.DefaultValue))
					ExtBlas.PointWiseConjugate<T, TS>(this.values, 1);
				else
					throw new InvalidOperationException(Resources.SparseError.CannotSetSparse);
			}
		}

		/// <inheritdoc/>
		public virtual void Power(T power)
		{
			if (TStatic.DefaultValue == T.Zero || TStatic.DefaultValue == T.One)
				ExtBlas.PointWisePower(this.values, 1, power);
			else
				throw new ArgumentException(Resources.SparseError.CannotSetSparse, nameof(power));
		}

		/// <inheritdoc/>
		public virtual void Truncate(double threshold)
		{
			if (TStatic.DefaultValue != T.Zero && T.Abs(TStatic.DefaultValue) < T.Create(threshold))
				throw new ArgumentException(Resources.SparseError.CannotSetSparse, nameof(threshold));
			else
				ExtBlas.PointWiseTruncate<T, TS>(this.values, 1, threshold);
		}
		#endregion

		#region simple aggregation operations
		/// <inheritdoc/>
		public virtual T Sum()
		{
			T defaultSum = TStatic.DefaultValue * T.Create(((IVectorMetric)this).Length - this.values.Length);
			return defaultSum + ExtBlas.AggregateSum<T, TS>(this.values, 1);
		}

		/// <inheritdoc/>
		public virtual T AbsSum()
		{
			T defaultSum = T.Abs(TStatic.DefaultValue) * T.Create(((IVectorMetric)this).Length - this.values.Length);
			return defaultSum + Blas.AbsoluteValueSum<T, TS>(this.values, 1);
		}

		/// <inheritdoc/>
		public virtual T Norm()
		{
			if (TStatic.DefaultValue == T.Zero)
				return Blas.Norm<T, TS>(this.values, 1);
			T abs = T.Abs(TStatic.DefaultValue);
			T defaultSum = abs * abs * T.Create(((IVectorMetric)this).Length - this.values.Length);
			T norm = Blas.Norm<T, TS>(this.values, 1);
			double n = (norm * norm + defaultSum).As<T, double>();
			return Math.Sqrt(n).As<double, T>();
		}

		/// <inheritdoc/>
		public virtual T ValueWithMaxAbs()
		{
			T max = (this.values + Blas.AbsoluteValueArgMax<T, TS>(this.values, 1)).ToManaged<T, TS>();
			if (T.Abs(TStatic.DefaultValue) > T.Abs(max))
				return TStatic.DefaultValue;
			else
				return max;
		}

		/// <inheritdoc/>
		public virtual T ValueWithMinAbs()
		{
			T min = (this.values + Blas.AbsoluteValueArgMin<T, TS>(this.values, 1)).ToManaged<T, TS>();
			if (T.Abs(TStatic.DefaultValue) < T.Abs(min))
				return TStatic.DefaultValue;
			else
				return min;
		}
		#endregion

		#region conversion and clone
		/// <summary>
		/// Create a new dense vector of type <see cref="DenseVector{T, TS}"/> from this sparse vector.
		/// </summary>
		/// <returns>The created dense vector of type <see cref="DenseVector{T, TS}"/>.</returns>
		public virtual DenseVector<T, TS> ToDense()
		{
			
		}

		/// <inheritdoc/>
		public virtual SparseVector<T, TInd, TS, TSInd, TStatic> CreateAlike()
		{
			return new(this.length, this.values.CreateAlike(), this.indices.CreateAlike(), this.blockSize, this.blockSizes?.CreateAlike(), this.blockSizesScan?.CreateAlike());
		}

		/// <inheritdoc/>
		public virtual SparseVector<T, TInd, TS, TSInd, TStatic> Clone()
		{
			var clone = this.CreateAlike();
			try
			{
				this.CopyTo(clone);
				return clone;
			}
			catch (Exception)
			{
				clone?.Dispose();
				throw;
			}
		}
		#endregion

		#region serialization
		/// <inheritdoc/>
		public virtual IReadOnlyDictionary<string, IStorage> GetStorages() => this.blockSizes is null || this.blockSizesScan is null ?
			new Dictionary<string, IStorage>
			{
				[nameof(Storage)] = this.values,
				[nameof(IndexStorage)] = this.indices,
			} :
			new Dictionary<string, IStorage>
			{
				[nameof(Storage)] = this.values,
				[nameof(IndexStorage)] = this.indices,
				[nameof(BlockSizes)] = this.blockSizes,
				[nameof(BlockSizesScan)] = this.blockSizesScan,
			};

		/// <inheritdoc/>
		public virtual IReadOnlyDictionary<string, object>? GetMetaData() => this.blockSize == 0 ? null : new Dictionary<string, object> { [nameof(BlockSize)] = this.blockSize };

		/// <inheritdoc/>
		public virtual SparseVector<T, TInd, TS, TSInd, TStatic> CreateArray(ReadOnlySpan<long> size, IReadOnlyDictionary<string, IStorage> storages, IReadOnlyDictionary<string, object>? otherInfo = null)
		{
			if (size.Length != 1)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(size));
			long length = size[0];
			long blockSize = 0; TSInd? blockSizes = null, blockSizesScan = null;
			switch (TStatic.Format.BlockType)
			{
				case SparseFormat.Blocking.Element:
					if (storages.Count != 2)
						throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(storages));
					if (otherInfo is not null)
						throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(otherInfo));
					break;
				case SparseFormat.Blocking.Simple:
					if (storages.Count != 2)
						throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(storages));
					if (otherInfo is null || otherInfo.Count != 1)
						throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(otherInfo));
					if (!otherInfo.TryGetValue(nameof(BlockSize), out var objBS))
						throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(otherInfo));
					if (objBS is not long blocksize)
						throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(otherInfo));
					blockSize = blocksize;
					break;
				case SparseFormat.Blocking.Complicated:
					if (storages.Count != 4)
						throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(storages));
					if (otherInfo is not null)
						throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(otherInfo));
					if (!storages.TryGetValue(nameof(BlockSizes), out var objBSs))
						throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(otherInfo));
					if (objBSs is not TSInd blocksizes)
						throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(otherInfo));
					if (!storages.TryGetValue(nameof(BlockSizesScan), out var objBSAs))
						throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(otherInfo));
					if (objBSAs is not TSInd blocksizesscan)
						throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(otherInfo));
					blockSizes = blocksizes;
					blockSizesScan = blocksizesscan;
					break;
				default:
					break;
			}
			if (!storages.TryGetValue(nameof(Storage), out var objVals))
				throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(otherInfo));
			if (objVals is not TS values)
				throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(otherInfo));
			if (!storages.TryGetValue(nameof(IndexStorage), out var objInds))
				throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(otherInfo));
			if (objVals is not TSInd indices)
				throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(otherInfo));
			return new(length, values, indices, blockSize, blockSizes, blockSizesScan);
		}

		#endregion

		#region equality
		/// <inheritdoc/>
		public virtual bool Equals(SparseVector<T, TInd, TS, TSInd, TStatic>? other)
		{
			if (other is null)
				return false;
			return this.values == other.values && this.indices == other.indices && this.blockSize == other.blockSize &&
				(ReferenceEquals(this.blockSizes, other.blockSizes) || (this.blockSizes is not null && other.blockSizes is not null && this.blockSizes == other.blockSizes)) &&
				(ReferenceEquals(this.blockSizesScan, other.blockSizesScan) || (this.blockSizesScan is not null && other.blockSizesScan is not null && this.blockSizesScan == other.blockSizesScan));
		}

		/// <summary>
		/// Equality operator
		/// </summary>
		public static bool operator ==(SparseVector<T, TInd, TS, TSInd, TStatic> left, SparseVector<T, TInd, TS, TSInd, TStatic> right) => left.Equals(right);

		/// <summary>
		/// Inequality operator
		/// </summary>
		public static bool operator !=(SparseVector<T, TInd, TS, TSInd, TStatic> left, SparseVector<T, TInd, TS, TSInd, TStatic> right) => !left.Equals(right);

		/// <inheritdoc/>
		public override bool Equals(object? obj) => this.Equals(obj as SparseVector<T, TInd, TS, TSInd, TStatic>);

		/// <inheritdoc/>
		public override int GetHashCode() => HashCode.Combine(this.values, this.indices, this.blockSize, this.blockSizes, this.blockSizesScan);
		#endregion

		#region string
		static string IMainPropertyFormattable<SparseVector<T, TInd, TS, TSInd, TStatic>>.StringMain => nameof(SparseVector<T, TInd, TS, TSInd, TStatic>);

		static IEnumerable<string> IMainPropertyFormattable<SparseVector<T, TInd, TS, TSInd, TStatic>>.PropertyNames => new[] { "DataType", "IndexType", "Format", "DefaultValue", "Values", "Indices", "BlockSizes" };

		IEnumerable<object?> IMainPropertyFormattable<SparseVector<T, TInd, TS, TSInd, TStatic>>.PropertyValues => new object[] { Unmanaged<T>.DataType, Unmanaged<TInd>.DataType, TStatic.Format, TStatic.DefaultValue, this.values, this.indices, this.blockSizes ?? (object)(this.blockSize == 0 ? 1 : this.blockSize) };

		/// <inheritdoc/>
		public override string ToString() => IMainPropertyFormattable<SparseVector<T, TInd, TS, TSInd, TStatic>>.ToString(this);

		/// <inheritdoc/>
		public unsafe string Print(PrintSettings? settings = null)
		{
			settings ??= Settings.PrintSetting;
			int length = Math.Min((int)this.values.Length, settings.Value.ArrayLength);
			Span<T> values = length.CheckStackLimit<T>() ?? stackalloc T[length];
			Span<int> indices = length.CheckStackLimit<int>() ?? stackalloc int[length];
			this.Storage.ToManaged(values);
			fixed (int* inds = indices)
			{
				var mp = new ManagedPureStorage<int>(new ManagedPointer(new(inds), length * sizeof(int)));
				switch (TStatic.Format.BlockType)
				{
					case SparseFormat.Blocking.Element:
						ExtBlas.PointWiseCast<TInd, int, TSInd, ManagedPureStorage<int>>(this.indices, 1, mp, 1);
						break;
					case SparseFormat.Blocking.Simple:
						ExtBlas.PointWiseCast<TInd, int, TSInd, ManagedPureStorage<int>>(this.indices, 1, mp, (int)this.blockSize);
						for (long i = 0; i < length; i++)
						{
							var diff = i % this.blockSize;
							inds[i] = (int)(inds[i - diff] + diff);
						}
						break;
					case SparseFormat.Blocking.Complicated:
						int blocks = 1 + (int)SpConv.IndexBound(this.BlockSizesScan, TInd.Create(length), true);
						for (int i = 0; i < length; i++)
						{
							inds[i] = inds[i - i % this.blockSize];
						}
						break;
				}
			}
		}
		#endregion
	}
}
