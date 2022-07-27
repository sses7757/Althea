using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;

using Althea.Helpers;
using Althea.LinearAlgebra.Dense;
using Althea.Storage;

using ExtBlas = Althea.LinearAlgebra.Dense.ExtendBlasApiSelector;
using SpIdx = Althea.LinearAlgebra.Sparse.IndexOperationApiSelector;


namespace Althea.Array
{
	/// <summary>
	/// The coordinated non-blocked sparse vector class that inherits <see cref="SparseVector{T, TInd, TS, TSInd}"/>.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TS">The storage type used by the value storage</typeparam>
	/// <typeparam name="TInd">Any unmanaged integer number as the index type</typeparam>
	/// <typeparam name="TSInd">The index storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
	[StructLayout(LayoutKind.Sequential, Pack = sizeof(long))]
	public class CoordinateSparseVector<T, TInd, TS, TSInd> : SparseVector<T, TInd, TS, TSInd>
		where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd>
		where TS : class, IStorage<T, TS> where TSInd : class, IStorage<TInd, TSInd>
	{
		#region basic
		private static readonly SparseFormat format = new(SparseFormat.Type.Coordinated, SparseFormat.Blocking.Element, SparseFormat.Major.None);

		/// <inheritdoc/>
		public override SparseFormat Format => format;

		/// <summary>
		/// Create a new <see cref="CoordinateSparseVector{T, TInd, TS, TSInd}"/> with given parameters.
		/// </summary>
		/// <param name="defaultValue">The default value</param>
		/// <param name="length">The presenting length</param>
		/// <param name="values">The original value array</param>
		/// <param name="indices">The original index array</param>
		/// <param name="nnz">The number of elements stored in <paramref name="values"/>, negative means all elements are stored</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="length"/> ≤ 0</exception>
		/// <exception cref="ArgumentException">If <paramref name="values"/> is too short</exception>
		/// <remarks>The validness of the detail values (such as sorted or not) in these storages are not checked for performance issues.</remarks>
		public CoordinateSparseVector(long length, TS values!!, TSInd indices!!, T defaultValue = default, long nnz = -1) : base(length, values, indices, defaultValue, nnz)
		{
			if (values.Length != indices.Length)
			{
				this.Dispose();
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(values));
			}
		}

		internal CoordinateSparseVector() : base() { }
		#endregion

		#region index
		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override long GetValueOffset(long index)
		{
			var ind = (index).As<TInd>();
			IBaseVector<T, SparseVector<T, TInd, TS, TSInd>>.CheckIndex(this, index);
			long find = SpIdx.BoundOf(this.IndexStorage, 1, ind, true);
			if ((this.IndexStorage + find).ToManaged<TInd, TSInd>() != ind)
				return ~find;
			else
				return find;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private (long indexStart, long indexCount) GetSliceInfo(long start, long count, SparseVector<T, TInd, TS, TSInd>? sub = null)
		{
			IBaseVector<T, SparseVector<T, TInd, TS, TSInd>>.CheckRange(this, start, count, sub);
			if (sub is not null && sub.Format != this.Format)
				throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(sub));
			long indexStart = SpIdx.BoundOf(this.IndexStorage, 1, (start).As<TInd>(), true);
			long indexCount = SpIdx.BoundOf(this.IndexStorage, 1, (start + count).As<TInd>(), true);
			if (sub is not null)
			{
				if (sub.IndexStorage.Length != indexCount || sub.NStored != indexCount)
					throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(sub));
			}
			return (indexStart, indexCount);
		}

		/// <inheritdoc/>
		protected override bool TryInsert(long index, long offset, T value)
		{
			offset = ~offset;
			long nnz = this.NStored;
			if (nnz + 1 > this.MaxStored)
				return false;
			this.Storage.TryInsert(offset, stackalloc T[] { value });
			this.IndexStorage.TryInsert(offset, stackalloc TInd[] { (index).As<TInd>() });
			this.NStored = nnz + 1;
			return true;
		}

		/// <inheritdoc/>
		public override CoordinateSparseVector<T, TInd, TS, TSInd> GetSlice(long start, long count)
		{
			var (indexStart, indexCount) = this.GetSliceInfo(start, count);
			if (indexStart == 0 && indexCount == this.IndexStorage.Length)
				return new(count, this.Storage.Clone(), this.IndexStorage.Clone(), this.DefaultValue);
			var newVals = this.Storage.MakeReference(indexStart, indexCount);
			var newInds = this.IndexStorage.MakeReference(indexStart, indexCount);
			if (start != 0)
				newInds = newInds.ApplyToClone((org, ind) => ExtBlas.GeneralVectorBinaryScalar(LinearAlgebra.BinaryScalarOperation.Add, (-start).As<TInd>(), org, 1, ind, 1));
			return new(count, newVals, newInds, this.DefaultValue);
		}

		/// <inheritdoc/>
		public override void GetSlice(long start, long count, SparseVector<T, TInd, TS, TSInd> overwrite)
		{
			var (indexStart, indexCount) = this.GetSliceInfo(start, count, overwrite);
			if (indexStart == 0 && indexCount == this.IndexStorage.Length)
			{
				this.CopyTo(overwrite);
				return;
			}
			var refVals = this.Storage.MakeReference(indexStart, indexCount);
			refVals.CopyTo<T, TS, TS>(overwrite.Storage);
			var refInds = this.IndexStorage.MakeReference(indexStart, indexCount);
			refInds.CopyTo<TInd, TSInd, TSInd>(overwrite.IndexStorage);
			if (start != 0)
				ExtBlas.GeneralVectorBinaryScalar(LinearAlgebra.BinaryScalarOperation.Add, (-start).As<TInd>(), overwrite.IndexStorage, 1, overwrite.IndexStorage, 1);
		}

		/// <inheritdoc/>
		public override void CopyTo(SparseVector<T, TInd, TS, TSInd> destination)
		{
			if (destination.Format != this.Format || destination.DefaultValue != this.DefaultValue)
				throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(destination));
			if (destination.IndexStorage.Length != this.IndexStorage.Length)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(destination));
			this.Storage.CopyTo<T, TS, TS>(destination.Storage);
			this.IndexStorage.CopyTo<TInd, TSInd, TSInd>(destination.IndexStorage);
		}

		/// <inheritdoc/>
		public override void SetSlice(long start, long count, SparseVector<T, TInd, TS, TSInd> value)
		{
			var (indexStart, indexCount) = this.GetSliceInfo(start, count, value);
			if (indexStart == 0 && indexCount == this.IndexStorage.Length)
			{
				value.CopyTo(this);
				return;
			}
			var refVals = this.Storage.MakeReference(indexStart, indexCount);
			value.Storage.CopyTo<T, TS, TS>(refVals);
			var refInds = this.IndexStorage.MakeReference(indexStart, indexCount);
			value.IndexStorage.CopyTo<TInd, TSInd, TSInd>(refInds);
			if (start != 0)
				ExtBlas.GeneralVectorBinaryScalar(LinearAlgebra.BinaryScalarOperation.Add, (start).As<TInd>(), refInds, 1, refInds, 1);
		}
		#endregion

		#region conversion and clone
		/// <inheritdoc/>
		public override CoordinateSparseVector<T, TInd, TS, TSInd> CreateAlike() => new(this.Length, this.Storage.CreateAlike(), this.IndexStorage.CreateAlike(), this.DefaultValue, 0);
		#endregion

		#region serialization
		private record struct Repr(int Format, T Default, long Length, long NonZeros, TS Values, TSInd Indices);

		private CoordinateSparseVector(Repr repr) : this(repr.Length, repr.Values, repr.Indices, repr.Default, repr.NonZeros) { }

		/// <inheritdoc/>
		public override string JsonSerialize() => JsonSerializer.Serialize<Repr>(new(this.Format.Data, this.DefaultValue, this.Length, this.NStored, this.Storage, this.IndexStorage), JsonOptions);

		private static bool TryJsonDeserialize(string json!!, [NotNullWhen(true)] out SparseVector<T, TInd, TS, TSInd>? vector)
		{
			try
			{
				vector = new CoordinateSparseVector<T, TInd, TS, TSInd>(JsonSerializer.Deserialize<Repr>(json, JsonOptions));
				return true;
			}
			catch (Exception)
			{
				vector = null;
				return false;
			}
		}

		private static bool TryCreate(in SparseArrayWrapper<T, TInd, TS, TSInd> wrapper, [NotNullWhen(true)] out SparseVector<T, TInd, TS, TSInd>? vector)
		{
			vector = null;
			if (wrapper.Format != format)
				return false;
			if (wrapper.IndexStorages.Length != 1 || wrapper.ValueStorages.Length != 1 || wrapper.Size.Length != 1)
				return false;
			if (wrapper.IndexStorages[0].Length != wrapper.ValueStorages[0].Length || wrapper.Size[0] != wrapper.IndexStorages[0].Length)
				return false;
			vector = new CoordinateSparseVector<T, TInd, TS, TSInd>(wrapper.Size[0], wrapper.ValueStorages[0], wrapper.IndexStorages[0], wrapper.DefaultValue);
			return true;
		}

		static CoordinateSparseVector()
		{
			Creators.Add(TryCreate);
			Deserializers.Add(TryJsonDeserialize);
		}
		#endregion

		#region string
		/// <inheritdoc/>
		public override string Print(PrintSettings? settings = null)
		{
			settings ??= Settings.PrintSetting;
			int length = Math.Min((int)this.NStored, settings.Value.ArrayLength);
			using var tempVal = length.CheckStackLimit<T>();
			Span<T> values = tempVal.IsEmpty ? stackalloc T[length] : tempVal.Data;
			using var tempInd = length.CheckStackLimit<TInd>();
			Span<TInd> indices = tempInd.IsEmpty ? stackalloc TInd[length] : tempInd.Data;
			this.Storage.ToManaged(values);
			this.IndexStorage.ToManaged(indices);
			return values.ToSparseVectorString<T, TInd>(indices, settings.Value.Precision) + (length == this.NStored ? "" : string.Format(Resources.Print.MoreStored, this.NStored - length));
		}
		#endregion
	}


	/// <summary>
	/// The coordinated simple blocked sparse vector class that inherits <see cref="SparseVector{T, TInd, TS, TSInd}"/>.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TS">The storage type used by the value storage</typeparam>
	/// <typeparam name="TInd">Any unmanaged integer number as the index type</typeparam>
	/// <typeparam name="TSInd">The index storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
	[StructLayout(LayoutKind.Sequential, Pack = sizeof(long))]
	public class BlockSparseVector<T, TInd, TS, TSInd> : SparseVector<T, TInd, TS, TSInd>, ISparseArray<T, TInd, TS, TSInd>
		where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd>
		where TS : class, IStorage<T, TS> where TSInd : class, IStorage<TInd, TSInd>
	{
		#region basic
		private readonly long blockSize;

		ReadOnlySpan<long> ISparseArray<T, TInd, TS, TSInd>.BlockSize => SpanHelper.CreateReadOnlySpan(in this.blockSize, 1);

		private readonly static SparseFormat format = new(SparseFormat.Type.Coordinated, SparseFormat.Blocking.Simple, SparseFormat.Major.None);

		/// <inheritdoc/>
		public override SparseFormat Format => format;

		/// <inheritdoc/>
		public override TSInd IndexStorage => this.indices.MakeReference(0, this.NStored / this.blockSize); 

		/// <summary>
		/// Create a new <see cref="BlockSparseVector{T, TInd, TS, TSInd}"/> with given parameters.
		/// </summary>
		/// <param name="defaultValue">The default value</param>
		/// <param name="length">The presenting length</param>
		/// <param name="values">The original value array</param>
		/// <param name="indices">The original index array of blocks</param>
		/// <param name="blockSize">The constant block size</param>
		/// <param name="nnz">The number of elements stored in <paramref name="values"/>, negative means all elements are stored</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="length"/> ≤ 0</exception>
		/// <exception cref="ArgumentException">If <paramref name="values"/> is too short</exception>
		/// <remarks>The validness of the detail values (such as sorted or not) in these storages are not checked for performance issues.</remarks>
		public BlockSparseVector(long length, TS values!!, TSInd indices!!, long blockSize, T defaultValue = default, long nnz = -1) : base(length, values, indices, defaultValue, nnz)
		{
			this.blockSize = blockSize;
			try
			{
				if (blockSize <= 0)
					throw new ArgumentOutOfRangeException(nameof(blockSize), Resources.ParameterError.MustPositive);
				if (length % blockSize != 0)
					throw new ArgumentException(Resources.ArithmeticError.CannotDivide, nameof(blockSize));
				if (values.Length != indices.Length * blockSize)
					throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(values));
				if (this.NStored % blockSize != 0)
					throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(nnz));
			}
			catch (Exception)
			{
				this.Dispose();
				throw;
			}
		}

		internal BlockSparseVector() : base() { }
		#endregion

		#region equality
		/// <inheritdoc/>
		public override bool Equals(SparseVector<T, TInd, TS, TSInd>? other)
		{
			if (!base.Equals(other))
				return false;
			return other is BlockSparseVector<T, TInd, TS, TSInd> vec && this.blockSize == vec.blockSize;
		}

		/// <inheritdoc/>
		public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), this.blockSize);
		#endregion

		#region index
		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override long GetValueOffset(long index)
		{
			IBaseVector<T, SparseVector<T, TInd, TS, TSInd>>.CheckIndex(this, index);
			var (blockIndex, insideBlockOffset) = long.DivRem(index, this.blockSize);
			var ind = (blockIndex).As<TInd>();
			long find = SpIdx.BoundOf(this.IndexStorage, 1, ind, true);
			if ((this.IndexStorage + find).ToManaged<TInd, TSInd>() != ind)
				return ~(find * this.blockSize);
			else
				return find * this.blockSize + insideBlockOffset;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private (long indexStart, long indexCount) GetSliceInfo(long start, long count, SparseVector<T, TInd, TS, TSInd>? sub = null)
		{
			IBaseVector<T, SparseVector<T, TInd, TS, TSInd>>.CheckRange(this, start, count, sub);
			if (sub is not null && (sub is not BlockSparseVector<T, TInd, TS, TSInd> vec || vec.blockSize != this.blockSize))
				throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(sub));
			long indexStart, indexCount;
			if (start % this.blockSize != 0)
				throw new ArgumentException(Resources.SparseError.CannotCutSimpleBlocking, nameof(start));
			if (count % this.blockSize != 0)
				throw new ArgumentException(Resources.SparseError.CannotCutSimpleBlocking, nameof(start));
			start /= this.blockSize; count /= this.blockSize;
			indexStart = SpIdx.BoundOf(this.IndexStorage, 1, (start).As<TInd>(), true);
			indexCount = SpIdx.BoundOf(this.IndexStorage, 1, (start + count).As<TInd>(), true);
			indexCount -= indexStart;
			return (indexStart, indexCount);
		}

		/// <inheritdoc/>
		protected override bool TryInsert(long index, long offsetVal, T value)
		{
			offsetVal = ~offsetVal;
			long nnz = this.NStored + this.blockSize;
			if (nnz + this.blockSize > this.MaxStored)
				return false;
			long offsetInd = offsetVal / this.blockSize;
			int bs = (int)this.blockSize;
			using var temp = bs.CheckStackLimit<T>();
			Span<T> values = temp.IsEmpty ? stackalloc T[bs] : temp.Data;
			values.Fill(this.DefaultValue); values[(int)(index % this.blockSize)] = value;
			this.Storage.TryInsert(offsetVal, values);
			this.IndexStorage.TryInsert(offsetInd, stackalloc TInd[] { (index / this.blockSize).As<TInd>() });
			this.NStored = nnz + 1;
			return true;
		}

		/// <inheritdoc/>
		public override BlockSparseVector<T, TInd, TS, TSInd> GetSlice(long start, long count)
		{
			var (indexStart, indexCount) = this.GetSliceInfo(start, count);
			if (indexStart == 0 && indexCount == this.IndexStorage.Length)
				return new(count, this.Storage.Clone(), this.IndexStorage.Clone(), this.blockSize, this.DefaultValue);
			var newVals = this.Storage.MakeReference(indexStart * this.blockSize, indexCount * this.blockSize);
			var newInds = this.IndexStorage.MakeReference(indexStart, indexCount);
			if (start == 0)
				newInds = newInds.ApplyToClone((org, ind) => ExtBlas.GeneralVectorBinaryScalar(LinearAlgebra.BinaryScalarOperation.Add, (-start / this.blockSize).As<TInd>(), org, 1, ind, 1));
			return new(count, newVals, newInds, this.blockSize, this.DefaultValue);
		}

		/// <inheritdoc/>
		public override void GetSlice(long start, long count, SparseVector<T, TInd, TS, TSInd> overwrite)
		{
			var (indexStart, indexCount) = this.GetSliceInfo(start, count, overwrite);
			if (indexStart == 0 && indexCount == this.IndexStorage.Length)
			{
				this.CopyTo(overwrite);
				return;
			}
			var refVals = this.Storage.MakeReference(indexStart * this.blockSize, indexCount * this.blockSize);
			refVals.CopyTo<T, TS, TS>(overwrite.Storage);
			var refInds = this.IndexStorage.MakeReference(indexStart, indexCount);
			refInds.CopyTo<TInd, TSInd, TSInd>(overwrite.IndexStorage);
			if (start != 0)
				ExtBlas.GeneralVectorBinaryScalar(LinearAlgebra.BinaryScalarOperation.Add, (-start / this.blockSize).As<TInd>(), overwrite.IndexStorage, 1, overwrite.IndexStorage, 1);
		}

		/// <inheritdoc/>
		public override void CopyTo(SparseVector<T, TInd, TS, TSInd> destination)
		{
			if (destination is not BlockSparseVector<T, TInd, TS, TSInd> vec || vec.DefaultValue != this.DefaultValue || vec.blockSize != this.blockSize || vec.IndexStorage.Length != this.IndexStorage.Length || vec.NStored != this.NStored)
				throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(destination));
			this.Storage.CopyTo<T, TS, TS>(destination.Storage);
			this.IndexStorage.CopyTo<TInd, TSInd, TSInd>(destination.IndexStorage);
		}

		/// <inheritdoc/>
		public override void SetSlice(long start, long count, SparseVector<T, TInd, TS, TSInd> value)
		{
			var (indexStart, indexCount) = this.GetSliceInfo(start, count, value);
			if (indexStart == 0 && indexCount == this.IndexStorage.Length)
			{
				value.CopyTo(this);
				return;
			}
			var refVals = this.Storage.MakeReference(indexStart * this.blockSize, indexCount * this.blockSize);
			value.Storage.CopyTo<T, TS, TS>(refVals);
			var refInds = this.IndexStorage.MakeReference(indexStart, indexCount);
			value.IndexStorage.CopyTo<TInd, TSInd, TSInd>(refInds);
			if (start != 0)
				ExtBlas.GeneralVectorBinaryScalar(LinearAlgebra.BinaryScalarOperation.Add, (start / this.blockSize).As<TInd>(), refInds, 1, refInds, 1);
		}
		#endregion

		#region conversion and clone
		/// <inheritdoc/>
		public override BlockSparseVector<T, TInd, TS, TSInd> CreateAlike() => new(this.Length, this.Storage.CreateAlike(), this.IndexStorage.CreateAlike(), this.blockSize, this.DefaultValue, 0);
		#endregion

		#region serialization
		private record struct Repr(int Format, T Default, long Length, long NonZeros, TS Values, TSInd Indices, long BlockSize);

		private BlockSparseVector(Repr repr) : this(repr.Length, repr.Values, repr.Indices, repr.BlockSize, repr.Default, repr.NonZeros) { }

		/// <inheritdoc/>
		public override string JsonSerialize() => JsonSerializer.Serialize<Repr>(new(this.Format.Data, this.DefaultValue, this.Length, this.NStored, this.Storage, this.IndexStorage, this.blockSize), JsonOptions);


		private static bool TryJsonDeserialize(string json!!, [NotNullWhen(true)] out SparseVector<T, TInd, TS, TSInd>? vector)
		{
			try
			{
				vector = new BlockSparseVector<T, TInd, TS, TSInd>(JsonSerializer.Deserialize<Repr>(json, JsonOptions));
				return true;
			}
			catch (Exception)
			{
				vector = null;
				return false;
			}
		}

		private static bool TryCreate(in SparseArrayWrapper<T, TInd, TS, TSInd> wrapper, [NotNullWhen(true)] out SparseVector<T, TInd, TS, TSInd>? vector)
		{
			vector = null;
			if (wrapper.Format != format || wrapper.Size.Length != 1 || wrapper.BlockSize.Length != 1 || wrapper.ValueStorages.Length != 1 || wrapper.IndexStorages.Length != 1)
				return false;
			var length = wrapper.Size[0];
			var blockSize = wrapper.BlockSize[0];
			var values = wrapper.ValueStorages[0];
			var indices = wrapper.IndexStorages[0];
			if (blockSize <= 0 || length % blockSize != 0)
				return false;
			if (values is null || indices is null)
				return false;
			if (values.Length != indices.Length * blockSize)
				return false;
			vector = new BlockSparseVector<T, TInd, TS, TSInd>(length, values, indices, blockSize, wrapper.DefaultValue);
			return true;
		}

		static BlockSparseVector()
		{
			Creators.Add(TryCreate);
			Deserializers.Add(TryJsonDeserialize);
		}
		#endregion

		#region string
		/// <inheritdoc/>
		public override string Print(PrintSettings? settings = null)
		{
			settings ??= Settings.PrintSetting;
			int length = Math.Min((int)this.NStored, settings.Value.ArrayLength);
			using var tempVal = length.CheckStackLimit<T>();
			Span<T> values = tempVal.IsEmpty ? stackalloc T[length] : tempVal.Data;
			using var tempInd = length.CheckStackLimit<TInd>();
			Span<TInd> indices = tempInd.IsEmpty ? stackalloc TInd[length] : tempInd.Data;
			this.Storage.ToManaged(values);
			int bs = (int)this.blockSize;
			this.IndexStorage.ToManagedStride(1, indices, bs);
			for (int i = 0; i < length; i++)
			{
				int diff = i % bs;
				indices[i] = (indices[i - diff].AsInt32() * bs + diff).As<TInd>();
			}
			return values.ToSparseVectorString<T, TInd>(indices, settings.Value.Precision) + (length == this.NStored ? "" : string.Format(Resources.Print.MoreStored, this.NStored - length));
		}
		#endregion
	}
}
