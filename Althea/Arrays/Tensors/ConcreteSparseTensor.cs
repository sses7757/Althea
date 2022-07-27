using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

using Althea.Helpers;
using Althea.Linq;
using Althea.Storage;

using ExtBlas = Althea.LinearAlgebra.Dense.ExtendBlasApiSelector;
using SpIdx = Althea.LinearAlgebra.Sparse.IndexOperationApiSelector;


namespace Althea.Array
{
	/// <summary>
	/// The coordinated element-wise sparse tensor class whose value storage is of type <typeparamref name="TS"/> and the only sorted index (as if in column major) storage is of type <typeparamref name="TSInd"/>.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TS">The storage type used by the value storage</typeparam>
	/// <typeparam name="TInd">Any unmanaged integer number as the index type</typeparam>
	/// <typeparam name="TSInd">The index storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
	public class CoordinateSparseTensor<T, TInd, TS, TSInd> : SparseTensor<T, TInd, TS, TSInd>
		where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd>
		where TS : class, IStorage<T, TS> where TSInd : class, IStorage<TInd, TSInd>
	{
		#region basic
		private static readonly SparseFormat format = new(SparseFormat.Type.Coordinated, SparseFormat.Blocking.Element, SparseFormat.Major.Column);

		/// <inheritdoc/>
		public override SparseFormat Format => format;

		/// <summary>
		/// Create a new <see cref="CoordinateSparseTensor{T, TInd, TS, TSInd}"/> with given parameters.
		/// </summary>
		/// <param name="defaultValue">The default value</param>
		/// <param name="size">The presenting size of all dimensions</param>
		/// <param name="values">The original value array</param>
		/// <param name="indices">The original index array</param>
		/// <param name="nnz">The number of elements stored in <paramref name="values"/>, negative means all elements are stored</param>
		/// <param name="labels">The labels of all dimensions of the new tensor, default means <c>{'a', 'b', ...}</c></param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="size"/> ≤ 0</exception>
		/// <exception cref="ArgumentException">If <paramref name="values"/> is too short</exception>
		/// <remarks>The validness of the detail values (such as sorted or not) in these storages are not checked for performance issues.</remarks>
		public CoordinateSparseTensor(ReadOnlySpan<long> size, TS values!!, TSInd indices!!, T defaultValue = default, long nnz = -1, ReadOnlySpan<char> labels = default) : base(size, values, indices, defaultValue, nnz, labels)
		{
			if (values.Length != indices.Length)
			{
				this.Dispose();
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(values));
			}
		}

		internal CoordinateSparseTensor() : base() { }
		#endregion

		#region implementation
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool GetOffsets(long presentIndex, Span<long> offsets)
		{
			var ind = (presentIndex).As<TInd>();
			long find = SpIdx.BoundOf(this.IndexStorage, 1, ind, true);
			offsets[0] = find;
			if (offsets.Length > 1)
				offsets[1] = presentIndex;
			return (this.IndexStorage + find).ToManaged<TInd, TSInd>() == ind;
		}

		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool GetOffsets(ReadOnlySpan<long> indices, Span<long> offsets)
		{
			long presentIndex = IBaseTensor<T, SparseTensor<T, TInd, TS, TSInd>>.CheckIndex(this, indices);
			return this.GetOffsets(presentIndex, offsets);
		}

		/// <inheritdoc/>
		protected override bool TryInsert(ReadOnlySpan<long> indices, Span<long> offsets, T value)
		{
			long offset = offsets[0], presentIndex = offsets[1];
			long nnz = this.NStored;
			if (nnz + 1 > this.MaxStored)
				return false;
			this.Storage.TryInsert(offset, stackalloc T[] { value });
			this.IndexStorage.TryInsert(offset, stackalloc TInd[] { (presentIndex).As<TInd>() });
			this.NStored = nnz + 1;
			return true;
		}

		/// <inheritdoc/>
		public override void CopyTo(SparseTensor<T, TInd, TS, TSInd> destination)
		{
			if (destination is not CoordinateSparseTensor<T, TInd, TS, TSInd> ten)
				throw new ArgumentException(Resources.ParameterError.UnexpectedType, nameof(destination));
			if (ten.NStored != this.NStored || !ten.Size.SequenceEqual(this.Size))
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(destination));
			this.Storage.CopyTo<T, TS, TS>(ten.Storage);
			this.IndexStorage.CopyTo<TInd, TSInd, TSInd>(ten.IndexStorage);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private (long startIndex, long start, long count) GetFirstDimOffsets(int n, ReadOnlySpan<long> restIndices, ReadOnlySpan<long> offsets, ReadOnlySpan<long> lengths, Span<long> allLengths, SparseTensor<T, TInd, TS, TSInd>? sub)
		{
			CoordinateSparseTensor<T, TInd, TS, TSInd>? coo = null;
			if (sub is not null)
			{
				if (sub is not CoordinateSparseTensor<T, TInd, TS, TSInd>)
					throw new ArgumentException(Resources.ParameterError.UnexpectedType, nameof(sub));
				coo = sub as CoordinateSparseTensor<T, TInd, TS, TSInd>;
			}
			// get start
			Span<long> allOffsets = stackalloc long[this.Rank];
			long startIndex = IBaseTensor<T, SparseTensor<T, TInd, TS, TSInd>>.CheckFirstDims(this, n, restIndices, offsets, lengths, allOffsets, allLengths, coo);
			Span<long> tempOffsets = stackalloc long[1];
			this.GetOffsets(startIndex, tempOffsets);
			long start = tempOffsets[0];
			// get end
			Span<long> newRestInds = stackalloc long[this.Rank - n];
			restIndices.CopyTo(newRestInds);
			newRestInds[0]++;
			for (int i = n; i < this.Rank; i++)
			{
				if (newRestInds[i - n] < this.Size[i])
					break;
				newRestInds[i - n + 1]++;
			}
			long endIndex = IBaseTensor<T, SparseTensor<T, TInd, TS, TSInd>>.CheckFirstDims(this, n, newRestInds, offsets, lengths, allOffsets, allLengths);
			this.GetOffsets(endIndex, tempOffsets);
			long count = tempOffsets[0] - start;
			// check
			if (coo is not null)
			{
				if (coo.NStored != count)
					throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(sub));
			}
			return (startIndex, start, count);
		}

		/// <inheritdoc/>
		public override CoordinateSparseTensor<T, TInd, TS, TSInd> GetFirstDims(int n, ReadOnlySpan<long> restIndices, ReadOnlySpan<long> offsets, ReadOnlySpan<long> lengths)
		{
			Span<long> allLengths = stackalloc long[this.Rank];
			var (startIndex, start, count) = this.GetFirstDimOffsets(n, restIndices, offsets, lengths, allLengths, null);
			if (count == 0)
				return new();
			var outInds = this.indices.MakeReference(start, count).ApplyToAlike((org, alike) => ExtBlas.GeneralVectorBinaryScalar(LinearAlgebra.BinaryScalarOperation.Add, (-startIndex).As<TInd>(), org, 1, alike, 1));
			return new(allLengths[..n], this.Storage.MakeReference(start, count), outInds, this.DefaultValue, -1, this.Labels[..n]);
		}

		/// <inheritdoc/>
		public override void GetFirstDims(int n, ReadOnlySpan<long> restIndices, SparseTensor<T, TInd, TS, TSInd> overwrite, ReadOnlySpan<long> offsets = default, ReadOnlySpan<long> lengths = default)
		{
			Span<long> allLengths = stackalloc long[this.Rank];
			var (startIndex, start, count) = this.GetFirstDimOffsets(n, restIndices, offsets, lengths, allLengths, overwrite);
			if (count == 0)
				return;
			this.Storage.MakeReference(start, count).CopyTo<T, TS, TS>(overwrite.Storage);
			this.indices.MakeReference(start, count).CopyTo<TInd, TSInd, TSInd>(overwrite.IndexStorage);
			ExtBlas.GeneralVectorBinaryScalar(LinearAlgebra.BinaryScalarOperation.Add, (-startIndex).As<TInd>(), overwrite.IndexStorage, 1, overwrite.IndexStorage, 1);
		}

		/// <inheritdoc/>
		public override void SetFirstDims(int n, ReadOnlySpan<long> restIndices, SparseTensor<T, TInd, TS, TSInd> value, ReadOnlySpan<long> offsets, ReadOnlySpan<long> lengths)
		{
			Span<long> allLengths = stackalloc long[this.Rank];
			var (startIndex, start, count) = this.GetFirstDimOffsets(n, restIndices, offsets, lengths, allLengths, value);
			if (count == 0)
				return;
			value.Storage.CopyTo<T, TS, TS>(this.Storage.MakeReference(start, count));
			var inds = this.indices.MakeReference(start, count);
			value.IndexStorage.CopyTo<TInd, TSInd, TSInd>(inds);
			ExtBlas.GeneralVectorBinaryScalar(LinearAlgebra.BinaryScalarOperation.Add, (startIndex).As<TInd>(), inds, 1, inds, 1);
		}

		/// <inheritdoc/>
		public override CoordinateSparseTensor<T, TInd, TS, TSInd> CreateAlike() => new(this.Size, this.Storage.CreateAlike(), this.IndexStorage.CreateAlike(), this.DefaultValue, 0);

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

		#region serialization
		private record struct Repr(int Format, T Default, long[] Length, long NonZeros, TS Values, TSInd Indices);

		private CoordinateSparseTensor(Repr repr) : this(repr.Length, repr.Values, repr.Indices, repr.Default, repr.NonZeros) { }

		/// <inheritdoc/>
		public override string JsonSerialize() => JsonSerializer.Serialize<Repr>(new(this.Format.Data, this.DefaultValue, this.Size.ToArray(), this.NStored, this.Storage, this.IndexStorage), JsonOptions);

		private static bool TryJsonDeserialize(string json!!, [NotNullWhen(true)] out SparseTensor<T, TInd, TS, TSInd>? vector)
		{
			try
			{
				vector = new CoordinateSparseTensor<T, TInd, TS, TSInd>(JsonSerializer.Deserialize<Repr>(json, JsonOptions));
				return true;
			}
			catch (Exception)
			{
				vector = null;
				return false;
			}
		}

		private static bool TryCreate(in SparseArrayWrapper<T, TInd, TS, TSInd> wrapper, [NotNullWhen(true)] out SparseTensor<T, TInd, TS, TSInd>? vector)
		{
			vector = null;
			if (wrapper.Format != format)
				return false;
			if (wrapper.IndexStorages.Length != 1 || wrapper.ValueStorages.Length != 1 || wrapper.Size.IsEmpty)
				return false;
			var size = wrapper.Size;
			if (wrapper.IndexStorages[0].Length != wrapper.ValueStorages[0].Length)
				return false;
			vector = new CoordinateSparseTensor<T, TInd, TS, TSInd>(size, wrapper.ValueStorages[0], wrapper.IndexStorages[0], wrapper.DefaultValue);
			return true;
		}

		static CoordinateSparseTensor()
		{
			Creators.Add(TryCreate);
			Deserializers.Add(TryJsonDeserialize);
		}
		#endregion
	}


	/// <summary>
	/// The coordinated simple block sparse tensor class whose value storage is of type <typeparamref name="TS"/> and the only sorted index (as if in column major) storage is of type <typeparamref name="TSInd"/>.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TS">The storage type used by the value storage</typeparam>
	/// <typeparam name="TInd">Any unmanaged integer number as the index type</typeparam>
	/// <typeparam name="TSInd">The index storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
	public class CoordinateBlockSparseTensor<T, TInd, TS, TSInd> : SparseTensor<T, TInd, TS, TSInd>, ISparseArray<T, TInd, TS, TSInd>
		where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd>
		where TS : class, IStorage<T, TS> where TSInd : class, IStorage<TInd, TSInd>
	{
		#region basic
		private readonly FixedBuffer_128<long> blockSize;

		private readonly long blockLength;

		/// <inheritdoc/>
		public ReadOnlySpan<long> BlockSize => this.blockSize.AsSpan(this.Rank);

		private static readonly SparseFormat format = new(SparseFormat.Type.Coordinated, SparseFormat.Blocking.Simple, SparseFormat.Major.Column);

		/// <inheritdoc/>
		public override SparseFormat Format => format;

		/// <inheritdoc/>
		public override TSInd IndexStorage => this.indices.MakeReference(0, this.NStored / this.blockLength);

		/// <summary>
		/// Create a new <see cref="CoordinateBlockSparseTensor{T, TInd, TS, TSInd}"/> with given parameters.
		/// </summary>
		/// <param name="defaultValue">The default value</param>
		/// <param name="size">The presenting size of all dimensions</param>
		/// <param name="blockSize">The constant size of all dimensions of a block</param>
		/// <param name="values">The original value array</param>
		/// <param name="indices">The original index array</param>
		/// <param name="nnz">The number of elements stored in <paramref name="values"/>, negative means all elements are stored</param>
		/// <param name="labels">The labels of all dimensions of the new tensor, default means <c>{'a', 'b', ...}</c></param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="size"/> ≤ 0</exception>
		/// <exception cref="ArgumentException">If <paramref name="values"/> is too short</exception>
		/// <remarks>The validness of the detail values (such as sorted or not) in these storages are not checked for performance issues.</remarks>
		public CoordinateBlockSparseTensor(ReadOnlySpan<long> size, ReadOnlySpan<long> blockSize, TS values!!, TSInd indices!!, T defaultValue = default, long nnz = -1, ReadOnlySpan<char> labels = default) : base(size, values, indices, defaultValue, nnz, labels)
		{
			try
			{
				if (blockSize.Length != this.Rank)
					throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(blockSize));
				this.blockSize.CopyFromSpan(blockSize);
				this.blockLength = blockSize.Prod();
				if (values.Length != indices.Length * this.blockLength)
					throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(values));
				if (this.NStored % this.blockLength != 0)
					throw new ArgumentException(Resources.ArithmeticError.CannotDivide, nameof(nnz));
			}
			catch (Exception)
			{
				this.Dispose();
				throw;
			}
		}

		internal CoordinateBlockSparseTensor() : base() { }
		#endregion

		#region implementation
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool GetOffsets(long presentIndex, Span<long> offsets)
		{
			long insideBlockOffset = presentIndex % this.blockLength;
			var ind = (presentIndex / this.blockLength).As<TInd>();
			long find = SpIdx.BoundOf(this.IndexStorage, 1, ind, true);
			offsets[0] = find * this.blockLength + insideBlockOffset;
			if (offsets.Length > 1)
				offsets[1] = find;
			return (this.IndexStorage + find).ToManaged<TInd, TSInd>() == ind;
		}

		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool GetOffsets(ReadOnlySpan<long> indices, Span<long> offsets)
		{
			long presentIndex = IBaseTensor<T, SparseTensor<T, TInd, TS, TSInd>>.CheckIndex(this, indices);
			return this.GetOffsets(presentIndex, offsets);
		}

		/// <inheritdoc/>
		protected override bool TryInsert(ReadOnlySpan<long> indices, Span<long> offsets, T value)
		{
			long offsetVal = offsets[0], offsetInd = offsets[1];
			long nnz = this.NStored;
			if (nnz + this.blockLength > this.MaxStored)
				return false;
			int bl = (int)this.blockLength;
			using var temp = bl.CheckStackLimit<T>();
			Span<T> values = temp.IsEmpty ? stackalloc T[bl] : temp.Data;
			values.Fill(this.DefaultValue); values[(int)(offsetVal % this.blockLength)] = value;
			this.Storage.TryInsert(offsetVal, values);
			this.IndexStorage.TryInsert(offsetInd, stackalloc TInd[] { (offsetInd * this.blockLength).As<TInd>() });
			this.NStored = nnz + this.blockLength;
			return true;
		}

		/// <inheritdoc/>
		public override void CopyTo(SparseTensor<T, TInd, TS, TSInd> destination)
		{
			if (destination is not CoordinateBlockSparseTensor<T, TInd, TS, TSInd> ten)
				throw new ArgumentException(Resources.ParameterError.UnexpectedType, nameof(destination));
			if (ten.NStored != this.NStored || !ten.Size.SequenceEqual(this.Size) || !ten.BlockSize.SequenceEqual(this.BlockSize))
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(destination));
			this.Storage.CopyTo<T, TS, TS>(ten.Storage);
			this.IndexStorage.CopyTo<TInd, TSInd, TSInd>(ten.IndexStorage);
		}

		// TODO: block sparse tensor first few dimensions
		/* Ignore Spelling: nameof inds
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private (long startOffsetInd, long countInd) GetFirstDimOffsets(int n, ReadOnlySpan<long> restIndices, ReadOnlySpan<long> offsets, ReadOnlySpan<long> lengths, Span<long> allLengths, SparseTensor<T, TInd, TS, TSInd>? sub)
		{
			CoordinateBlockSparseTensor<T, TInd, TS, TSInd>? coo = null;
			if (sub is not null)
			{
				if (sub is not CoordinateBlockSparseTensor<T, TInd, TS, TSInd>)
					throw new ArgumentException(Resources.ParameterError.UnexpectedType, nameof(sub));
				coo = (CoordinateBlockSparseTensor<T, TInd, TS, TSInd>)sub;
				if (!coo.BlockSize.SequenceEqual(this.BlockSize))
					throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(sub));
			}
			// get start
			Span<long> allOffsets = stackalloc long[this.Rank];
			long startIndex = IBaseTensor<T, SparseTensor<T, TInd, TS, TSInd>>.CheckFirstDims(this, n, restIndices, offsets, lengths, allOffsets, allLengths, coo);
			Span<long> tempOffsets = stackalloc long[2];
			this.GetOffsets(startIndex, tempOffsets);
			long startOffsetVal = tempOffsets[0], startOffsetInd = tempOffsets[1];
			if (startOffsetVal % this.blockLength != 0)
				throw new ArgumentException(Resources.SparseError.CannotCutSimpleBlocking, nameof(offsets));
			// get end
			Span<long> newRestInds = stackalloc long[this.Rank - n];
			restIndices.CopyTo(newRestInds);
			newRestInds[0]++;
			for (int i = n; i < this.Rank; i++)
			{
				if (newRestInds[i - n] < this.Size[i])
					break;
				newRestInds[i - n + 1]++;
			}
			long endIndex = IBaseTensor<T, SparseTensor<T, TInd, TS, TSInd>>.CheckFirstDims(this, n, newRestInds, offsets, lengths, allOffsets, allLengths);
			this.GetOffsets(endIndex, tempOffsets);
			long endOffsetVal = tempOffsets[0], countInd = tempOffsets[1] - startOffsetInd;
			if (endOffsetVal % this.blockLength != 0)
				throw new ArgumentException(Resources.SparseError.CannotCutSimpleBlocking, nameof(lengths));
			// check
			if (coo is not null)
			{
				if (coo.NStored != countInd * this.blockLength)
					throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(sub));
			}
			return (startOffsetInd, countInd);
		}

		/// <inheritdoc/>
		public override CoordinateBlockSparseTensor<T, TInd, TS, TSInd> GetFirstDims(int n, ReadOnlySpan<long> restIndices, ReadOnlySpan<long> offsets, ReadOnlySpan<long> lengths)
		{
			Span<long> allLengths = stackalloc long[this.Rank];
			var (startOffsetInd, countInd) = this.GetFirstDimOffsets(n, restIndices, offsets, lengths, allLengths, null);
			if (countInd == 0)
				return new();
			var outInds = this.indices.MakeReference(startOffsetInd, countInd).ApplyToClone(i => ExtBlas.PointwiseAddScalar(i, 1, (-startOffsetInd).As<TInd>()));
			return new(allLengths[..n], this.BlockSize, this.Storage.MakeReference(startOffsetInd * this.blockLength, countInd * this.blockLength), outInds, this.DefaultValue, -1, this.Labels[..n]);
		}

		/// <inheritdoc/>
		public override void GetFirstDims(int n, ReadOnlySpan<long> restIndices, SparseTensor<T, TInd, TS, TSInd> overwrite, ReadOnlySpan<long> offsets = default, ReadOnlySpan<long> lengths = default)
		{
			Span<long> allLengths = stackalloc long[this.Rank];
			var (startOffsetInd, countInd) = this.GetFirstDimOffsets(n, restIndices, offsets, lengths, allLengths, overwrite);
			if (countInd == 0)
				return;
			this.Storage.MakeReference(startOffsetInd * this.blockLength, countInd * this.blockLength).CopyTo<T, TS, TS>(overwrite.Storage);
			this.indices.MakeReference(startOffsetInd, countInd).CopyTo<TInd, TSInd, TSInd>(overwrite.IndexStorage);
			ExtBlas.PointwiseAddScalar(overwrite.IndexStorage, 1, (-startOffsetInd).As<TInd>());
		}

		/// <inheritdoc/>
		public override void SetFirstDims(int n, ReadOnlySpan<long> restIndices, SparseTensor<T, TInd, TS, TSInd> value, ReadOnlySpan<long> offsets, ReadOnlySpan<long> lengths)
		{
			Span<long> allLengths = stackalloc long[this.Rank];
			var (startOffsetInd, countInd) = this.GetFirstDimOffsets(n, restIndices, offsets, lengths, allLengths, value);
			if (countInd == 0)
				return;
			value.Storage.CopyTo<T, TS, TS>(this.Storage.MakeReference(startOffsetInd * this.blockLength, countInd * this.blockLength));
			var inds = this.indices.MakeReference(startOffsetInd, countInd);
			value.IndexStorage.CopyTo<TInd, TSInd, TSInd>(inds);
			ExtBlas.PointwiseAddScalar(inds, 1, (startOffsetInd).As<TInd>());
		}
		*/

		/// <inheritdoc/>
		public override CoordinateBlockSparseTensor<T, TInd, TS, TSInd> GetFirstDims(int n, ReadOnlySpan<long> restIndices, ReadOnlySpan<long> offsets, ReadOnlySpan<long> lengths) => throw new NotSupportedException();

		/// <inheritdoc/>
		public override void GetFirstDims(int n, ReadOnlySpan<long> restIndices, SparseTensor<T, TInd, TS, TSInd> overwrite, ReadOnlySpan<long> offsets = default, ReadOnlySpan<long> lengths = default) => throw new NotSupportedException();

		/// <inheritdoc/>
		public override void SetFirstDims(int n, ReadOnlySpan<long> restIndices, SparseTensor<T, TInd, TS, TSInd> value, ReadOnlySpan<long> offsets, ReadOnlySpan<long> lengths) => throw new NotSupportedException();

		/// <inheritdoc/>
		public override CoordinateBlockSparseTensor<T, TInd, TS, TSInd> CreateAlike() => new(this.Size, this.BlockSize, this.Storage.CreateAlike(), this.IndexStorage.CreateAlike(), this.DefaultValue, 0);

		/// <inheritdoc/>
		public override string Print(PrintSettings? settings = null)
		{
			var ps = settings ?? Settings.PrintSetting;
			int nnz = Math.Min((int)(this.NStored / this.blockLength), ps.ArrayLength);
			using var tempInd = nnz.CheckStackLimit<TInd>();
			Span<TInd> indices = tempInd.IsEmpty ? stackalloc TInd[nnz] : tempInd.Data;
			this.IndexStorage.ToManaged(indices);

			Span<long> bsp = stackalloc long[this.Rank + 1];
			this.BlockSize.AccumulateProd(bsp);
			Span<long> position = stackalloc long[this.Rank];
			StringBuilder sb = new();
			for (int i = 0; i < nnz; i++)
			{
				for (int k = 1; k <= this.Rank; k++)
					position[k] = (i % bsp[k]) / bsp[k - 1];
				sb.Append("Tensor[");
				for (int k = 0; k < this.Rank; k++)
					sb.Append($"{position[k] * this.blockSize[k]}..{(position[k] + 1) * this.blockSize[k]}, ");
				sb.Remove(sb.Length - 2, 2);
				sb.AppendLine("] = ");
				using var block = new DenseTensor<T, TS>(this.Storage + i * this.blockLength, this.BlockSize);
				sb.AppendLine(block.Print(ps));
			}
			if (nnz < this.NStored)
				sb.AppendFormat(Resources.Print.MoreStored, this.NStored - nnz);
			return sb.ToString();
		}
		#endregion

		#region serialization
		private record struct Repr(int Format, T Default, long[] Length, long[] BlockSize, long NonZeros, TS Values, TSInd Indices);

		private CoordinateBlockSparseTensor(Repr repr) : this(repr.Length, repr.BlockSize, repr.Values, repr.Indices, repr.Default, repr.NonZeros) { }

		/// <inheritdoc/>
		public override string JsonSerialize() => JsonSerializer.Serialize<Repr>(new(this.Format.Data, this.DefaultValue, this.Size.ToArray(), this.BlockSize.ToArray(), this.NStored, this.Storage, this.IndexStorage), JsonOptions);

		private static bool TryJsonDeserialize(string json!!, [NotNullWhen(true)] out SparseTensor<T, TInd, TS, TSInd>? vector)
		{
			try
			{
				vector = new CoordinateBlockSparseTensor<T, TInd, TS, TSInd>(JsonSerializer.Deserialize<Repr>(json, JsonOptions));
				return true;
			}
			catch (Exception)
			{
				vector = null;
				return false;
			}
		}

		private static bool TryCreate(in SparseArrayWrapper<T, TInd, TS, TSInd> wrapper, [NotNullWhen(true)] out SparseTensor<T, TInd, TS, TSInd>? vector)
		{
			vector = null;
			if (wrapper.Format != format)
				return false;
			if (wrapper.IndexStorages.Length != 1 || wrapper.ValueStorages.Length != 1 || wrapper.Size.IsEmpty || wrapper.BlockSize.Length != wrapper.Size.Length)
				return false;
			var size = wrapper.Size;
			var blockSize = wrapper.BlockSize;
			long blockLength = blockSize.Prod();
			var indices = wrapper.IndexStorages[0];
			var values = wrapper.ValueStorages[0];
			if (values.Length != indices.Length * blockLength)
				return false;
			vector = new CoordinateBlockSparseTensor<T, TInd, TS, TSInd>(size, blockSize, values, indices, wrapper.DefaultValue);
			return true;
		}

		static CoordinateBlockSparseTensor()
		{
			if (TInd.Size > sizeof(long))
				throw new NotSupportedException(Resources.ArithmeticError.DataTypeNotAllow);
			Creators.Add(TryCreate);
			Deserializers.Add(TryJsonDeserialize);
		}
		#endregion
	}
}
