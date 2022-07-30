using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

using Althea.Helpers;
using Althea.LinearAlgebra;
using Althea.Linq;
using Althea.Storage;
using Althea.TensorAlgebra;

using Blas = Althea.LinearAlgebra.Dense.BlasApiSelector;
using ExtBlas = Althea.LinearAlgebra.Dense.ExtendBlasApiSelector;
using SpTen = Althea.TensorAlgebra.Sparse.ApiSelector;


namespace Althea.Array
{
	/// <summary>
	/// The abstract sparse tensor class whose value storage is of type <typeparamref name="TS"/> and sorted index storage is of type <typeparamref name="TSInd"/>.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TS">The storage type used by the value storage</typeparam>
	/// <typeparam name="TInd">Any unmanaged integer number as the index type</typeparam>
	/// <typeparam name="TSInd">The index storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
	[StructLayout(LayoutKind.Explicit)]
	public abstract partial class SparseTensor<T, TInd, TS, TSInd> : ISparseArray<T, TInd, TS, TSInd>,
		ISubtypeJsonConvertible<SparseTensor<T, TInd, TS, TSInd>>,
		IBaseTensor<T, SparseTensor<T, TInd, TS, TSInd>>,
		ITensorUnaryOperators<T, SparseTensor<T, TInd, TS, TSInd>, SparseTensor<T, TInd, TS, TSInd>>,
		ITensorBinaryOperators<T, SparseTensor<T, TInd, TS, TSInd>, DenseTensor<T, TS>, DenseTensor<T, TS>>,
		ITensorBinaryOperators<T, SparseTensor<T, TInd, TS, TSInd>, SparseTensor<T, TInd, TS, TSInd>, SparseTensor<T, TInd, TS, TSInd>>
		where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd>
		where TS : class, IStorage<T, TS> where TSInd : class, IStorage<TInd, TSInd>
	{
		#region basic
		[FieldOffset(0)]
		private readonly FixedBuffer_128<long> size;
		[FieldOffset(128 - sizeof(long))]
		private readonly int rank;
		[FieldOffset(128)]
		private readonly FixedBuffer_128<long> sizeProd;
		[FieldOffset(128 * 2 - sizeof(long))]
		private readonly long length;
		[FieldOffset(128 * 2)]
		private FixedBuffer_32<char> labels;
		[FieldOffset(128 * 2 + 32)]
		private readonly TS values;

		/// <summary>
		/// The index array's storage of this sparse tensor.
		/// </summary>
		[FieldOffset(128 * 2 + 40)]
		protected readonly TSInd indices;

		[FieldOffset(128 * 2 + 48)]
		private long nnz;

		[FieldOffset(128 * 2 + 56)]
		private T defaultValue;

		private const byte MAX_RANK = 15;

		ReadOnlySpan<TS> ISparseArray<T, TInd, TS, TSInd>.ValueStorages => SpanHelper.CreateReadOnlySpan(in this.values, 1);
		ReadOnlySpan<TSInd> ISparseArray<T, TInd, TS, TSInd>.IndexStorages => SpanHelper.CreateReadOnlySpan(in this.indices, 1);
		ReadOnlySpan<long> ISparseArray<T, TInd, TS, TSInd>.BlockSize => default;

		bool ICheckValid.IsValid() => (this.values?.IsValid() ?? false) && (this.indices?.IsValid() ?? false);

		/// <inheritdoc/>
		[JsonIgnore]
		public int Rank => this.rank;

		/// <inheritdoc/>
		[JsonIgnore]
		public long Length => this.length;

		/// <inheritdoc/>
		[JsonIgnore]
		public long NStored
		{
			get => this.nnz;
			protected set => this.nnz = value < this.nnz || value > this.MaxStored ? throw new ArgumentOutOfRangeException(nameof(value)) : value;
		}

		/// <summary>
		/// Get the number of maximum possible elements that can be stored in this sparse tensor.
		/// </summary>
		[JsonIgnore]
		protected long MaxStored => this.values.Length;

		/// <inheritdoc/>
		[JsonIgnore]
		public ReadOnlySpan<long> Size => this.size.AsSpan(this.rank);

		/// <inheritdoc/>
		[JsonIgnore]
		public ReadOnlySpan<long> SizeProd => this.sizeProd.AsSpan(this.rank + 1);

		/// <inheritdoc/>
		public abstract SparseFormat Format { get; }

		/// <inheritdoc/>
		public T DefaultValue => this.defaultValue;

		/// <summary>
		/// Get the value storage of this matrix as a <typeparamref name="TS"/>.
		/// </summary>
		public TS Storage => this.values.MakeReference(0, this.nnz);

		/// <summary>
		/// Get the index array's storage of this sparse tensor.
		/// </summary>
		public virtual TSInd IndexStorage => this.indices.MakeReference(0, this.nnz);

		/// <inheritdoc/>
		[JsonIgnore]
		public ReadOnlySpan<char> Labels
		{
			get => this.labels.AsSpan(this.rank);
			set
			{
				if (value.Length != this.rank)
					throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(value));
				this.labels.CopyFromSpan(value);
			}
		}

		[JsonInclude]
		private long[] SizeArray => this.Size.ToArray();

		[JsonInclude]
		private string LabelsArray => new(this.Labels);

		/// <inheritdoc/>
		public char GetLabel(int index)
		{
			if (index < 0 || index >= this.rank)
				throw new ArgumentOutOfRangeException(nameof(index));
			return this.labels[index];
		}

		/// <inheritdoc/>
		public void SetLabel(int index, char label)
		{
			if (index < 0 || index >= this.rank)
				throw new ArgumentOutOfRangeException(nameof(index));
			this.labels[index] = label;
		}

		/// <inheritdoc/>
		public void SetLabels(params char[] labels!!)
		{
			if (labels.Length != this.rank)
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(labels));
			this.labels.CopyFromSpan(labels);
		}

		/// <summary>
		/// Create a new <see cref="SparseTensor{T, TInd, TS, TSInd}"/> with given parameters.
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
		protected SparseTensor(ReadOnlySpan<long> size, TS values!!, TSInd indices!!, T defaultValue = default, long nnz = -1, ReadOnlySpan<char> labels = default)
		{
			this.defaultValue = defaultValue;
			if (size.IsEmpty || size.Length > MAX_RANK)
				throw new NotSupportedException(Resources.ParameterError.WrongSize);
			if (size.Any(static s => s <= 0))
				throw new ArgumentOutOfRangeException(nameof(size), Resources.ParameterError.MustPositive);
			if (nnz < 0)
				nnz = values.Length;
			this.size.CopyFromSpan(size);
			size.AccumulateProd(this.sizeProd.AsSpan(size.Length + 1));
			this.rank = size.Length; this.nnz = nnz;
			long actualLength = values.Length;
			if (actualLength >= this.length)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(values));
			if (!labels.IsEmpty)
			{
				if (labels.Length != this.rank)
					throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(labels));
				if (labels.DistinctCount() != this.rank)
					throw new ArgumentException(Resources.ParameterError.DuplicateValue, nameof(labels));
				this.labels.CopyFromSpan(labels);
			}
			else
			{
				this.labels.AsSpan(this.rank).FillWithLabel();
			}
			this.values = values.AddToManager();
			this.indices = indices.AddToManager();
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// When implemented by a derived class, actually unmanaged resources held by this object.
		/// </summary>
		/// <param name="invokedByUser">Whether this method is invoked by user or by GC</param>
		protected virtual void Dispose(bool invokedByUser)
		{
			this.values.SafeDispose(invokedByUser);
			this.indices.SafeDispose(invokedByUser);
		}

		/// <summary>
		/// Deconstructor to be invoked by GC
		/// </summary>
		~SparseTensor() => this.Dispose(false);

		/// <summary>
		/// Create an empty sparse tensor.
		/// </summary>
		protected SparseTensor()
		{
			this.values = TS.Empty; this.indices = TSInd.Empty;
		}

		static SparseTensor<T, TInd, TS, TSInd> IValueArray<T, SparseTensor<T, TInd, TS, TSInd>>.Empty => new CoordinateSparseTensor<T, TInd, TS, TSInd>();
		#endregion

		#region equality
		/// <inheritdoc/>
		public virtual bool Equals(SparseTensor<T, TInd, TS, TSInd>? other)
		{
			if (other is null)
				return false;
			return this.Format == other.Format && this.defaultValue == other.defaultValue && this.nnz == other.nnz && this.values == other.values && this.indices == other.indices;
		}

		/// <summary>
		/// Equality operator
		/// </summary>
		public static bool operator ==(SparseTensor<T, TInd, TS, TSInd> left, SparseTensor<T, TInd, TS, TSInd> right) => left.Equals(right);

		/// <summary>
		/// Inequality operator
		/// </summary>
		public static bool operator !=(SparseTensor<T, TInd, TS, TSInd> left, SparseTensor<T, TInd, TS, TSInd> right) => !left.Equals(right);

		/// <inheritdoc/>
		public override bool Equals(object? obj) => this.Equals(obj as SparseTensor<T, TInd, TS, TSInd>);

		/// <inheritdoc/>
		public override int GetHashCode() => HashCode.Combine(this.Format, this.defaultValue, this.nnz, this.values, this.indices);
		#endregion

		#region index
		/// <summary>
		/// When implemented by a derived class, get the offsets to the <see cref="Storage"/> (and other index storages) of the corresponding <paramref name="indices"/>.
		/// </summary>
		/// <param name="indices">The presenting indices</param>
		/// <param name="offsets">The <see cref="Span{T}"/> used to store the result. If it is of length 1, only the offset to <see cref="Storage"/> shall be computed; otherwise, all offsets shall be computed.</param>
		/// <returns>The element in <paramref name="indices"/> is stored or not. If not, the offsets shall be insertion positions.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected abstract bool GetOffsets(ReadOnlySpan<long> indices, Span<long> offsets);

		/// <inheritdoc/>
		public T this[ReadOnlySpan<long> indices]
		{
			get
			{
				Span<long> offset = stackalloc long[1];
				if (!this.GetOffsets(indices, offset))
					return this.defaultValue;
				else
					return (this.values + offset[0]).ToManaged<T, TS>();
			}
			set
			{
				Span<long> offsets = stackalloc long[1 + ((ISparseArray<T, TInd, TS, TSInd>)this).IndexStorages.Length];
				if (this.GetOffsets(indices, offsets))
				{
					(this.values + offsets[0]).FromManaged(value);
				}
				else
				{
					if (!this.TryInsert(indices, offsets, value))
						throw new ArgumentException(Resources.SparseError.CannotSetSparse, nameof(value));
				}
			}
		}

		/// <summary>
		/// When implemented by a derived class, set the element at<paramref name="index"/> to <paramref name="value"/> when that element is not stored. 
		/// </summary>
		/// <param name="index">The presenting indices</param>
		/// <param name="value">The value to set at <paramref name="index"/></param>
		/// <param name="offsets">The <see cref="Span{T}"/> of offsets obtained from <see cref="GetOffsets(ReadOnlySpan{long}, Span{long})"/></param>
		/// <returns>Success or not.</returns>
		/// <remarks>This method is usually quite expensive to call inside a loop to add values. Please use constructor instead.</remarks>
		protected abstract bool TryInsert(ReadOnlySpan<long> index, Span<long> offsets, T value);

		/// <inheritdoc/>
		public SparseTensor<T, TInd, TS, TSInd> GetSlice(ReadOnlySpan<long> offsets, ReadOnlySpan<long> lengths)
		{
			IBaseTensor<T, SparseTensor<T, TInd, TS, TSInd>>.CheckRange(this, offsets, lengths);
			var sub = new SparseArrayWrapper<T, TInd, TS, TSInd>(this.defaultValue, SparseFormat.Any, lengths);
			SpTen.GetSlice(this, offsets, ref sub);
			return Create(in sub);
		}

		/// <inheritdoc/>
		public void GetSlice(ReadOnlySpan<long> offsets, ReadOnlySpan<long> lengths, SparseTensor<T, TInd, TS, TSInd> overwrite)
		{
			IBaseTensor<T, SparseTensor<T, TInd, TS, TSInd>>.CheckRange(this, offsets, lengths, overwrite);
			var sub = SparseArrayWrapper<T, TInd, TS, TSInd>.Create(overwrite);
			SpTen.GetSlice(this, offsets, ref sub);
		}

		/// <inheritdoc/>
		public void SetSlice(ReadOnlySpan<long> offsets, ReadOnlySpan<long> lengths, SparseTensor<T, TInd, TS, TSInd> value)
		{
			IBaseTensor<T, SparseTensor<T, TInd, TS, TSInd>>.CheckRange(this, offsets, lengths, value);
			SpTen.SetSlice(this, offsets, value);
		}

		/// <inheritdoc/>
		public abstract SparseTensor<T, TInd, TS, TSInd> GetFirstDims(int n, ReadOnlySpan<long> restIndices, ReadOnlySpan<long> offsets = default, ReadOnlySpan<long> lengths = default);

		/// <inheritdoc/>
		public abstract void GetFirstDims(int n, ReadOnlySpan<long> restIndices, SparseTensor<T, TInd, TS, TSInd> overwrite, ReadOnlySpan<long> offsets = default, ReadOnlySpan<long> lengths = default);

		/// <inheritdoc/>
		public abstract void SetFirstDims(int n, ReadOnlySpan<long> restIndices, SparseTensor<T, TInd, TS, TSInd> value, ReadOnlySpan<long> offsets = default, ReadOnlySpan<long> lengths = default);

		/// <inheritdoc/>
		public abstract void CopyTo(SparseTensor<T, TInd, TS, TSInd> destination);
		#endregion

		#region point-wise operations
		/// <inheritdoc/>
		public void FillWith(T value)
		{
			this.values.FillWith(value);
			this.defaultValue = value;
		}

		/// <inheritdoc/>
		public void AddScalar(T value)
		{
			if (value == T.Zero)
				return;
			ExtBlas.GeneralVectorBinaryScalar(BinaryScalarOperation.Add, value, this.values, 1, this.values, 1);
			this.defaultValue += value;
		}

		/// <inheritdoc/>
		public void Scale(T value)
		{
			if (value == T.One)
				return;
			Blas.Scale(this.values, 1, value);
			this.defaultValue *= value;
		}

		/// <inheritdoc/>
		public void Conjugate()
		{
			if (T.IsComplexType)
			{
				ExtBlas.GeneralVectorUnary<T, TS, TS>(UnaryOperation.Conjugate, this.values, 1, this.values, 1);
				this.defaultValue = T.Conjugate(this.defaultValue);
			}
		}
		#endregion

		#region simple aggregation operations
		/// <inheritdoc/>
		public T Sum()
		{
			T defaultSum = this.defaultValue * (this.length - this.values.Length).As<T>();
			return defaultSum + ExtBlas.GeneralVectorReduce<T, TS>(ReduceOperation.Add, this.values, 1);
		}

		/// <inheritdoc/>
		public T AbsSum()
		{
			T defaultSum = T.Abs(this.defaultValue) * (this.length - this.values.Length).As<T>();
			return defaultSum + Blas.AbsoluteValueSum<T, TS>(this.values, 1);
		}

		/// <inheritdoc/>
		public T Norm()
		{
			if (this.defaultValue == T.Zero)
				return Blas.Norm<T, TS>(this.values, 1);
			T abs = T.Abs(this.defaultValue);
			T defaultSum = abs * abs * (this.length - this.values.Length).As<T>();
			T norm = Blas.Norm<T, TS>(this.values, 1);
			double n = (norm * norm + defaultSum).AsDouble();
			return Math.Sqrt(n).As<T>();
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

		#region operators
		/// <inheritdoc/>
		public static SparseTensor<T, TInd, TS, TSInd> operator ^(SparseTensor<T, TInd, TS, TSInd> tensor, TensorOrder order) => SparseOperation<T, TInd, TS, TSInd>.Permute(tensor, order, T.One);

		/// <inheritdoc/>
		public static SparseTensor<T, TInd, TS, TSInd> operator *(SparseTensor<T, TInd, TS, TSInd> tensor, T scalar) => SparseOperation<T, TInd, TS, TSInd>.Permute(tensor, TensorOrder.Identity, scalar);

		/// <inheritdoc/>
		public static SparseTensor<T, TInd, TS, TSInd> operator *(T scalar, SparseTensor<T, TInd, TS, TSInd> tensor) => tensor * scalar;

		/// <inheritdoc/>
		public static SparseTensor<T, TInd, TS, TSInd> operator -(SparseTensor<T, TInd, TS, TSInd> tensor) => tensor * (-T.One);

		/// <inheritdoc/>
		public static SparseTensor<T, TInd, TS, TSInd> operator /(SparseTensor<T, TInd, TS, TSInd> tensor, T scalar) => tensor * (T.One / scalar);

		/// <inheritdoc/>
		public static DenseTensor<T, TS> operator *(SparseTensor<T, TInd, TS, TSInd> left, DenseTensor<T, TS> right) => SparseOperation<T, TInd, TS, TSInd>.Contract(left, UnaryOperation.Identity, right, UnaryOperation.Identity, T.One);

		/// <inheritdoc/>
		public static DenseTensor<T, TS> operator +(SparseTensor<T, TInd, TS, TSInd> left, DenseTensor<T, TS> right) => SparseOperation<T, TInd, TS, TSInd>.TensorsBinaryOperation(left, TensorOrder.Identity, UnaryOperation.Identity, T.One, right, TensorOrder.Identity, UnaryOperation.Identity, T.One, BinaryOperation.Add);

		/// <inheritdoc/>
		public static DenseTensor<T, TS> operator -(SparseTensor<T, TInd, TS, TSInd> left, DenseTensor<T, TS> right) => SparseOperation<T, TInd, TS, TSInd>.TensorsBinaryOperation(left, TensorOrder.Identity, UnaryOperation.Identity, T.One, right, TensorOrder.Identity, UnaryOperation.Negate, T.One, BinaryOperation.Add);

		/// <inheritdoc/>
		public static DenseTensor<T, TS> operator *(DenseTensor<T, TS> left, SparseTensor<T, TInd, TS, TSInd> right) => right * left;

		/// <inheritdoc/>
		public static DenseTensor<T, TS> operator +(DenseTensor<T, TS> left, SparseTensor<T, TInd, TS, TSInd> right) => right + left;

		/// <inheritdoc/>
		public static DenseTensor<T, TS> operator -(DenseTensor<T, TS> left, SparseTensor<T, TInd, TS, TSInd> right) => SparseOperation<T, TInd, TS, TSInd>.TensorsBinaryOperation(left, TensorOrder.Identity, UnaryOperation.Identity, T.One, right, TensorOrder.Identity, UnaryOperation.Negate, T.One, BinaryOperation.Add);

		/// <inheritdoc/>
		public static SparseTensor<T, TInd, TS, TSInd> operator *(SparseTensor<T, TInd, TS, TSInd> left, SparseTensor<T, TInd, TS, TSInd> right) => SparseOperation<T, TInd, TS, TSInd>.Contract(left, UnaryOperation.Identity, right, UnaryOperation.Identity, T.One);

		/// <inheritdoc/>
		public static SparseTensor<T, TInd, TS, TSInd> operator +(SparseTensor<T, TInd, TS, TSInd> left, SparseTensor<T, TInd, TS, TSInd> right) => SparseOperation<T, TInd, TS, TSInd>.TensorsBinaryOperation(left, TensorOrder.Identity, UnaryOperation.Identity, T.One, right, TensorOrder.Identity, UnaryOperation.Identity, T.One, BinaryOperation.Add);

		/// <inheritdoc/>
		public static SparseTensor<T, TInd, TS, TSInd> operator -(SparseTensor<T, TInd, TS, TSInd> left, SparseTensor<T, TInd, TS, TSInd> right) => SparseOperation<T, TInd, TS, TSInd>.TensorsBinaryOperation(left, TensorOrder.Identity, UnaryOperation.Identity, T.One, right, TensorOrder.Identity, UnaryOperation.Negate, T.One, BinaryOperation.Add);
		#endregion

		#region conversion and clone
		/// <summary>
		/// Create a new <see cref="DenseTensor{T, TS}"/> from this sparse tensor.
		/// </summary>
		/// <returns>The created <see cref="DenseTensor{T, TS}"/>.</returns>
		public DenseTensor<T, TS> ToDense()
		{
			TS dense = this.values.ResizeAlike(this.length);
			SpTen.ToDense(this, dense, this.Size);
			return new(dense, this.Size);
		}

		/// <summary>
		/// When implemented by a derived class, statically create a new <see cref="SparseTensor{T, TInd, TS, TSInd}"/> from <paramref name="dense"/> tensor truncating by <paramref name="threshold"/>.
		/// </summary>
		/// <param name="dense">The input dense tensor to convert</param>
		/// <param name="format">The target format</param>
		/// <param name="defaultValue">The target default value</param>
		/// <param name="threshold">The threshold used to truncate to sparse array</param>
		/// <returns>The created <see cref="SparseTensor{T, TInd, TS, TSInd}"/>.</returns>
		public static SparseTensor<T, TInd, TS, TSInd> FromDense(DenseTensor<T, TS> dense, SparseFormat format, T defaultValue, double threshold = 0)
		{
			var sparse = new SparseArrayWrapper<T, TInd, TS, TSInd>(defaultValue, format);
			SpTen.FromDense<T, TInd, TS, TS, TSInd>(new(dense), ref sparse, threshold);
			return Create(in sparse);
		}

		/// <inheritdoc/>
		public abstract SparseTensor<T, TInd, TS, TSInd> CreateAlike();
		#endregion

		#region string
		static string IMainPropertyFormattable<SparseTensor<T, TInd, TS, TSInd>>.StringMain => nameof(SparseTensor<T, TInd, TS, TSInd>);

		static IEnumerable<string> IMainPropertyFormattable<SparseTensor<T, TInd, TS, TSInd>>.PropertyNames => new[] { "DataType", "IndexType", "Format", "DefaultValue", "Values", "Indices" };

		IEnumerable<object?> IMainPropertyFormattable<SparseTensor<T, TInd, TS, TSInd>>.PropertyValues => new object[] { T.Type, TInd.Type, this.Format, this.defaultValue, this.values, this.indices };

		/// <inheritdoc/>
		public override string ToString() => IMainPropertyFormattable<SparseTensor<T, TInd, TS, TSInd>>.ToString(this);

		/// <inheritdoc/>
		public abstract string Print(PrintSettings? settings = null);
		#endregion

		#region protected static
		/// <inheritdoc/>
		public static JsonSerializerOptions JsonSerializeOptions { get; } = ISparseArray<T, TInd, TS, TSInd>.JsonSerializeOptions;

		static SparseTensor() => JsonSerializeOptions.Converters.Add(new ISubtypeJsonConvertible<SparseTensor<T, TInd, TS, TSInd>>.JsonConverter());

		/// <summary>
		/// Encapsulates a method that statically create a new sparse tensor from the given <paramref name="wrapper"/>.
		/// </summary>
		/// <param name="wrapper">The <see cref="SparseArrayWrapper{TVal, TInd, TSVal, TSInd}"/> to create from.</param>
		/// <param name="matrix">A created <see cref="SparseTensor{T, TInd, TS, TSInd}"/> from the given <paramref name="wrapper"/></param>
		/// <returns>Success or not.</returns>
		protected delegate bool TryCreateFromWrapper(in SparseArrayWrapper<T, TInd, TS, TSInd> wrapper, [NotNullWhen(true)] out SparseTensor<T, TInd, TS, TSInd>? matrix);

		/// <summary>
		/// The list used to store the <see cref="TryCreateFromWrapper"/>s for sub-classes.
		/// </summary>
		/// <remarks>Any sub-class that inherits <see cref="SparseTensor{T, TInd, TS, TSInd}"/> SHALL add its own <see cref="TryCreateFromWrapper"/> implementation to this list.</remarks>
		protected static readonly List<TryCreateFromWrapper> Creators = new();

		/// <summary>
		/// Statically create a new sparse tensor from the given <paramref name="wrapper"/>.
		/// </summary>
		/// <param name="wrapper">The <see cref="SparseArrayWrapper{TVal, TInd, TSVal, TSInd}"/> to create from.</param>
		/// <returns>A created <see cref="SparseTensor{T, TInd, TS, TSInd}"/> from the given <paramref name="wrapper"/>.</returns>
		/// <exception cref="NotSupportedException">If none of the creators in sub-classes can be used to create a <see cref="SparseTensor{T, TInd, TS, TSInd}"/></exception>
		public static SparseTensor<T, TInd, TS, TSInd> Create(in SparseArrayWrapper<T, TInd, TS, TSInd> wrapper)
		{
			foreach (var creator in Creators)
			{
				if (creator(in wrapper, out var ten))
					return ten;
			}
			wrapper.DisposeAll();
			throw new NotSupportedException(Resources.SparseError.FormatNotSupport);
		}
		#endregion
	}
}
