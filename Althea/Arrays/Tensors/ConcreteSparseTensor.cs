using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;

using Althea.Helpers;
using Althea.LinearAlgebra.Sparse;
using Althea.Linq;
using Althea.NativeTypes;
using Althea.Storage;
using Althea.TensorAlgebra;

using Blas = Althea.LinearAlgebra.Dense.BlasApiSelector;
using ExtBlas = Althea.LinearAlgebra.Dense.ExtendBlasApiSelector;
using SpConv = Althea.LinearAlgebra.Sparse.ConversionApiSelector;
using SpTen = Althea.TensorAlgebra.Sparse.ApiSelector;


namespace Althea.Arrays
{
	/// <summary>
	/// The coordinated element-wise sparse tensor class whose value storage is of type <typeparamref name="TS"/> and the only sorted index (as if in column major) storage is of type <typeparamref name="TSInd"/>.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TS">The storage type used by the value storage</typeparam>
	/// <typeparam name="TInd">Any unmanaged integer number as the index type</typeparam>
	/// <typeparam name="TSInd">The index storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
	public class CoordinateSparseTensor<T, TInd, TS, TSInd> : SparseTensor<T, TInd, TS, TSInd>
		where T : unmanaged, INumber<T> where TInd : unmanaged, IBinaryInteger<TInd>
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
		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool GetOffsets(ReadOnlySpan<long> indices, Span<long> offsets)
		{
			long presentIndex = IBaseTensor<T, SparseTensor<T, TInd, TS, TSInd>>.CheckIndex(this, indices);
			var ind = TInd.Create(presentIndex);
			long find = SpConv.IndexBound(this.IndexStorage, ind, true);
			offsets[0] = find;
			if (offsets.Length > 1)
				offsets[1] = presentIndex;
			return (this.IndexStorage + find).ToManaged<TInd, TSInd>() == ind;
		}

		/// <inheritdoc/>
		protected override bool TryInsert(ReadOnlySpan<long> indices, Span<long> offsets, T value)
		{
			long offset = offsets[0], presentIndex = offsets[1];
			long nnz = this.NStored;
			if (nnz + 1 > this.MaxStored)
				return false;

			using var tempVal = this.Storage.MakeReference(offset).Clone();
			using var tempInd = this.IndexStorage.MakeReference(offset).Clone();
			this.NStored = nnz + 1;
			this.Storage.MakeReference(offset, 1).FromManaged(value);
			this.IndexStorage.MakeReference(offset, 1).FromManaged(TInd.Create(presentIndex));
			tempVal.CopyTo<T, TS, TS>(this.Storage + (++offset));
			tempInd.CopyTo<TInd, TSInd, TSInd>(this.IndexStorage + offset);
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

		/// <inheritdoc/>
		public override CoordinateSparseTensor<T, TInd, TS, TSInd> GetFirstDims(int n, ReadOnlySpan<long> restIndices, ReadOnlySpan<long> offsets, ReadOnlySpan<long> lengths)
		{

		}

		/// <inheritdoc/>
		public override void SetFirstDims(int n, ReadOnlySpan<long> restIndices, SparseTensor<T, TInd, TS, TSInd> value, ReadOnlySpan<long> offsets, ReadOnlySpan<long> lengths)
		{

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
			long length = size.Prod();
			if (wrapper.IndexStorages[0].Length != wrapper.ValueStorages[0].Length || length != wrapper.IndexStorages[0].Length)
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
}
