using System;
using System.Buffers;
using System.Runtime.CompilerServices;

using Althea.Helpers;
using Althea.LinearAlgebra;
using Althea.LinearAlgebra.Sparse;
using Althea.NativeTypes;
using Althea.Storage;

using Mem = Althea.Storage.ApiSelector;
using Blas = Althea.LinearAlgebra.Dense.BlasApiSelector;
using ExtBlas = Althea.LinearAlgebra.Dense.ExtendBlasApiSelector;
using SpConv = Althea.LinearAlgebra.Sparse.ConversionApiSelector;
using SpComp = Althea.LinearAlgebra.Sparse.ComputationApiSelector;


namespace Althea.Arrays
{
	/// <summary>
	/// The coordinated non-blocked (or blocked) sparse vector interface whose value storage is of type <typeparamref name="TS"/> and sorted index storage is of type <typeparamref name="TSInd"/>.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TS">The storage type used by the value <see cref="ISingleValueStorageArray{T, TS, TSelf}.Storage"/></typeparam>
	/// <typeparam name="TInd">Any unmanaged integer number as the index type</typeparam>
	/// <typeparam name="TSInd">The index storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
	/// <typeparam name="TSelf">The concrete type that implements this <see cref="ISparseVector{T, TInd, TS, TSInd, TSelf}"/></typeparam>
	public interface ISparseVector<T, TInd, TS, TSInd, TSelf> : IBaseVector<T, TSelf>, ISingleValueStorageArray<T, TS, TSelf>, ISparseArray<T>
		where T : unmanaged, INumber<T> where TInd : unmanaged, IBinaryInteger<TInd>
		where TS : class, IStorage<T, TS> where TSInd : class, IStorage<TInd, TSInd>
		where TSelf : class, ISparseVector<T, TInd, TS, TSInd, TSelf>
	{
		#region basic
		/// <summary>
		/// When implemented by a derived class, get the index array's original storage of this sparse vector.
		/// </summary>
		protected TSInd OrginalIndexStorage { get; }

		/// <summary>
		/// When implemented by a derived class, get the block size array's original storage of this sparse vector which shall be null if <see cref="ISparseArray{T}.Format"/> is not of <see cref="SparseFormat.Blocking.Complicated"/>.
		/// </summary>
		protected TSInd? BlockSizes { get; }

		/// <summary>
		/// When implemented by a derived class, get the block size array's accumulation array's original storage of this sparse vector which shall be null if <see cref="ISparseArray{T}.Format"/> is not of <see cref="SparseFormat.Blocking.Complicated"/>.
		/// </summary>
		protected TSInd? BlockSizeAccu { get; }

		/// <summary>
		/// When implemented by a derived class, get the block size if <see cref="ISparseArray{T}.Format"/> is not of <see cref="SparseFormat.Blocking.Simple"/>.
		/// </summary>
		protected long BlockSize { get; }

		/// <summary>
		/// The supported <see cref="SparseFormat"/>s of this interface.
		/// </summary>
		protected static readonly SparseFormat SupportFormats = new(SparseFormat.Type.Coordinated, SparseFormat.Blocking.Element | SparseFormat.Blocking.Simple | SparseFormat.Blocking.Complicated, SparseFormat.Major.None);

		/// <summary>
		/// When implemented by a derived class, get the index array's storage of this sparse vector.
		/// </summary>
		TSInd IndexStorage => this.OrginalIndexStorage.MakeReference();

		IStorage ISparseArray<T>.ValueStorage => this.OriginalStorage;

		ReadOnlySpan<IStorage> ISparseArray<T>.IndexStorages => this.BlockSizes is null ? new[] { this.OrginalIndexStorage } : new[] { this.OrginalIndexStorage, this.BlockSizes };

		static ISparseVector()
		{
			if ((TSelf.Format & SupportFormats) != TSelf.Format)
				throw new NotSupportedException(Resources.SparseError.FormatNotSupport);
		}
		#endregion

		#region index
		private long GetOffset(long index)
		{
			this.CheckIndex(index);
			long offset;
			if (TSelf.Format.BlockType == SparseFormat.Blocking.Element)
			{
				long find = SpConv.IndexFind(true, this.IndexStorage, TInd.Create(index));
				if (find < 0)
					return -1;
				offset = (this.IndexStorage + find).ToManaged<TInd, TSInd>().As<TInd, long>();
			}
			else
			{
				long blockIndex = SpConv.IndexBound(this.IndexStorage, TInd.Create(index + 1), true) - 1;
				long blockSize = this.BlockSizes is null ? this.BlockSize : (this.BlockSizes + blockIndex).ToManaged<TInd, TSInd>().As<TInd, long>();
				long blockOffset = (this.IndexStorage + blockIndex).ToManaged<TInd, TSInd>().As<TInd, long>();
				offset = index - blockOffset;
				if (offset >= blockSize)
					return -1;
				offset += this.BlockSizeAccu is null ? blockSize * blockIndex : (this.BlockSizeAccu + blockIndex).ToManaged<TInd, TSInd>().As<TInd, long>();
			}
			return offset;
		}

		T IBaseVector<T, TSelf>.this[long index]
		{
			get
			{
				long offset = this.GetOffset(index);
				return offset < 0 ? TSelf.DefaultValue : (this.Storage + offset).ToManaged<T, TS>();
			}
			set
			{
				long offset = this.GetOffset(index);
				if (offset < 0)
					throw new ArgumentException(Resources.SparseError.CannotSetSparse, nameof(index));
				(this.Storage + offset).FromManaged(value);
			}
		}

		TSelf IBaseVector<T, TSelf>.GetSlice(long start, long count)
		{

		}

		void IBaseVector<T, TSelf>.GetSlice(long start, long count, TSelf overwrite)
		{

		}

		void IBaseVector<T, TSelf>.CopyTo(TSelf destination);

		void IBaseVector<T, TSelf>.SetSlice(long start, long count, TSelf value);

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			
		}
		#endregion

		#region point-wise operations
		void IValueArray<T, TSelf>.FillWith(T value)
		{
			if (value != TSelf.DefaultValue)
				throw new ArgumentException(Resources.SparseError.CannotSetSparse, nameof(value));
			this.Storage.FillWith(value);
		}

		void IValueArray<T, TSelf>.AddScalar(T value)
		{
			if (value != T.Zero)
				throw new ArgumentException(Resources.SparseError.CannotSetSparse, nameof(value));
		}

		void IValueArray<T, TSelf>.Scale(T value)
		{
			if (TSelf.DefaultValue == T.Zero)
				Blas.Scale(this.Storage, 1, value);
			else if (value != T.One)
				throw new ArgumentException(Resources.SparseError.CannotSetSparse, nameof(value));
		}

		void IValueArray<T, TSelf>.Conjugate()
		{
			if (NumberType<T>.IsComplex)
			{
				if (NumberType<T>.IsRealValue(TSelf.DefaultValue))
					ExtBlas.PointWiseConjugate<T, TS>(this.Storage, 1);
				else
					throw new InvalidOperationException(Resources.SparseError.CannotSetSparse);
			}
		}

		void IValueArray<T, TSelf>.Power(T power)
		{
			if (TSelf.DefaultValue == T.Zero || TSelf.DefaultValue == T.One)
				ExtBlas.PointWisePower(this.Storage, 1, power);
			else
				throw new ArgumentException(Resources.SparseError.CannotSetSparse, nameof(power));
		}

		void IValueArray<T, TSelf>.Truncate(double threshold)
		{
			if (TSelf.DefaultValue != T.Zero && T.Abs(TSelf.DefaultValue) < T.Create(threshold))
				throw new ArgumentException(Resources.SparseError.CannotSetSparse, nameof(threshold));
			else
				ExtBlas.PointWiseTruncate<T, TS>(this.Storage, 1, threshold);
		}
		#endregion

		#region simple aggregation operations
		T IValueArray<T, TSelf>.Sum()
		{
			T defaultSum = TSelf.DefaultValue * T.Create(((IVectorMetric)this).Length - this.Storage.Length);
			return defaultSum + ExtBlas.AggregateSum<T, TS>(this.Storage, 1);
		}

		T IValueArray<T, TSelf>.AbsSum()
		{
			T defaultSum = T.Abs(TSelf.DefaultValue) * T.Create(((IVectorMetric)this).Length - this.Storage.Length);
			return defaultSum + Blas.AbsoluteValueSum<T, TS>(this.Storage, 1);
		}

		T IValueArray<T, TSelf>.Norm()
		{
			if (TSelf.DefaultValue == T.Zero)
				return Blas.Norm<T, TS>(this.Storage, 1);
			T abs = T.Abs(TSelf.DefaultValue);
			T defaultSum = abs * abs * T.Create(((IVectorMetric)this).Length - this.Storage.Length);
			T norm = Blas.Norm<T, TS>(this.Storage, 1);
			double n = (norm * norm + defaultSum).As<T, double>();
			return Math.Sqrt(n).As<double, T>();
		}

		T IValueArray<T, TSelf>.ValueWithMaxAbs()
		{
			T max = (this.Storage + Blas.AbsoluteValueArgMax<T, TS>(this.Storage, 1)).ToManaged<T, TS>();
			if (T.Abs(TSelf.DefaultValue) > T.Abs(max))
				return TSelf.DefaultValue;
			else
				return max;
		}

		T IValueArray<T, TSelf>.ValueWithMinAbs()
		{
			T min = (this.Storage + Blas.AbsoluteValueArgMin<T, TS>(this.Storage, 1)).ToManaged<T, TS>();
			if (T.Abs(TSelf.DefaultValue) < T.Abs(min))
				return TSelf.DefaultValue;
			else
				return min;
		}
		#endregion
	}
}
