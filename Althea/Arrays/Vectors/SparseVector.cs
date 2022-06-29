using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;

using Althea.Helpers;
using Althea.LinearAlgebra;
using Althea.Linq;
using Althea.Storage;

using Blas = Althea.LinearAlgebra.Dense.BlasApiSelector;
using ExtBlas = Althea.LinearAlgebra.Dense.ExtendBlasApiSelector;
using SpConv = Althea.LinearAlgebra.Sparse.ConversionApiSelector;


namespace Althea.Array
{
	/// <summary>
	/// The abstract sparse vector abstract class whose value storage is of type <typeparamref name="TS"/> and sorted index storage is of type <typeparamref name="TSInd"/>.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TS">The storage type used by the value storage</typeparam>
	/// <typeparam name="TInd">Any unmanaged integer number as the index type</typeparam>
	/// <typeparam name="TSInd">The index storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
	[StructLayout(LayoutKind.Sequential, Pack = sizeof(long))]
	public abstract class SparseVector<T, TInd, TS, TSInd> : ISparseArray<T, TInd, TS, TSInd>,
		IBaseVector<T, SparseVector<T, TInd, TS, TSInd>>,
		IVectorUnaryOperators<T, SparseVector<T, TInd, TS, TSInd>, SparseVector<T, TInd, TS, TSInd>>,
		IVectorBinaryOperators<T, SparseVector<T, TInd, TS, TSInd>, DenseVector<T, TS>, DenseVector<T, TS>>,
		IVectorBinaryOperators<T, SparseVector<T, TInd, TS, TSInd>, SparseVector<T, TInd, TS, TSInd>, SparseVector<T, TInd, TS, TSInd>>,
		IVectorMatrixMultiplyOperators<T, SparseVector<T, TInd, TS, TSInd>, DenseVector<T, TS>, DenseMatrix<T, TS>>
		where T : unmanaged, INumber<T> where TInd : unmanaged, IBinaryInteger<TInd>
		where TS : class, IStorage<T, TS> where TSInd : class, IStorage<TInd, TSInd>
	{
		#region basic
		private readonly long length;
		private long nnz;

		/// <summary>
		/// The index array's storage of this sparse vector.
		/// </summary>
		protected readonly TSInd indices;

		private readonly TS values;

		private T defaultValue;

		ReadOnlySpan<long> IValueArray<T, SparseVector<T, TInd, TS, TSInd>>.Size => SpanHelper.CreateReadOnlySpan(in this.length, 1);

		ReadOnlySpan<long> IArray<T>.Size => SpanHelper.CreateReadOnlySpan(in this.length, 1);
		ReadOnlySpan<TS> ISparseArray<T, TInd, TS, TSInd>.ValueStorages => SpanHelper.CreateReadOnlySpan(in this.values, 1);
		ReadOnlySpan<TSInd> ISparseArray<T, TInd, TS, TSInd>.IndexStorages => SpanHelper.CreateReadOnlySpan(in this.indices, 1);
		ReadOnlySpan<TInd> ISparseArray<T, TInd, TS, TSInd>.BlockSize => default;

		bool ICheckValid.IsValid() => (this.values?.IsValid() ?? false) && (this.indices?.IsValid() ?? false);

		/// <inheritdoc/>
		public abstract SparseFormat Format { get; }

		/// <inheritdoc/>
		public T DefaultValue => this.defaultValue;

		/// <summary>
		/// Get the value storage of this vector as a <typeparamref name="TS"/>.
		/// </summary>
		public TS Storage => this.values.MakeReference(0, this.nnz);

		/// <summary>
		/// Get the index array's storage of this sparse vector.
		/// </summary>
		public virtual TSInd IndexStorage => this.indices.MakeReference(0, this.nnz);

		/// <inheritdoc/>
		public long Length => this.length;

		/// <inheritdoc/>
		public long NStored 
		{
			get => this.nnz; 
			protected set => this.nnz = value < this.nnz || value > this.MaxStored ? throw new ArgumentOutOfRangeException(nameof(value)) : value;
		}

		/// <summary>
		/// Get the number of maximum possible elements that can be stored in this sparse vector.
		/// </summary>
		protected long MaxStored => this.values.Length;

		/// <summary>
		/// Create a new <see cref="SparseVector{T, TInd, TS, TSInd}"/> with given parameters.
		/// </summary>
		/// <param name="defaultValue">The default value</param>
		/// <param name="length">The presenting length</param>
		/// <param name="values">The original value array</param>
		/// <param name="indices">The original index array</param>
		/// <param name="nnz">The number of elements stored in <paramref name="values"/>, negative means all elements are stored</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="length"/> ≤ 0</exception>
		/// <exception cref="ArgumentException">If the <paramref name="values"/> is too short</exception>
		/// <remarks>The validness of the detail values (such as sorted or not) in these storages are not checked for performance issues.</remarks>
		protected SparseVector(long length, TS values!!, TSInd indices!!, T defaultValue = default, long nnz = -1)
		{
			this.defaultValue = defaultValue;
			if (length < 0)
				throw new ArgumentOutOfRangeException(nameof(length), Resources.ParameterError.CannotNegative);
			if (nnz < 0)
				nnz = values.Length;
			this.length = length; this.nnz = nnz;
			if (length < values.Length || values.Length < indices.Length || nnz > values.Length)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(values));
			this.values = values.AddToManager();
			this.indices = indices.AddToManager();
		}

		/// <inheritdoc/>
		public virtual void Dispose()
		{
			this.values.SafeDispose();
			this.indices.SafeDispose();
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Create an empty sparse vector
		/// </summary>
		protected SparseVector()
		{
			this.values = TS.Empty; this.indices = TSInd.Empty;
		}

		static SparseVector<T, TInd, TS, TSInd> IValueArray<T, SparseVector<T, TInd, TS, TSInd>>.Empty => new CoordinateSparseVector<T, TInd, TS, TSInd>();
		#endregion

		#region equality
		/// <inheritdoc/>
		public virtual bool Equals(SparseVector<T, TInd, TS, TSInd>? other)
		{
			if (other is null)
				return false;
			return this.Format == other.Format && this.defaultValue == other.defaultValue && this.values == other.values && this.indices == other.indices;
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
		public override int GetHashCode() => HashCode.Combine(this.Format, this.defaultValue, this.values, this.indices);
		#endregion

		#region index
		/// <summary>
		/// When implemented by a derived class, get the offset to <see cref="Storage"/> of the corresponding <paramref name="index"/>.
		/// </summary>
		/// <param name="index">The presenting index</param>
		/// <returns>The offset in <typeparamref name="T"/> compared to <see cref="Storage"/> if it is stored, or the bitwise NOT of the position of insertion.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected abstract long GetValueOffset(long index);

		/// <inheritdoc/>
		/// <remarks>This method is usually quite expensive to call inside a loop to add values. Please use constructor instead.</remarks>
		public T this[long index]
		{
			get
			{
				long offset = this.GetValueOffset(index);
				return offset < 0 ? this.defaultValue : (this.values + offset).ToManaged<T, TS>();
			}
			set
			{
				long offset = this.GetValueOffset(index);
				if (offset >= 0)
				{
					(this.values + offset).FromManaged(value);
				}
				else
				{
					if (!this.TryInsert(index, offset, value))
						throw new ArgumentException(Resources.SparseError.CannotSetSparse, nameof(index));
				}
			}
		}

		/// <summary>
		/// When implemented by a derived class, set the element at <paramref name="index"/> to <paramref name="value"/> when the element at <paramref name="index"/> is a <see cref="DefaultValue"/>. 
		/// </summary>
		/// <param name="index">The presenting index to set <paramref name="value"/> at</param>
		/// <param name="offset">The offset compared to <see cref="Storage"/> obtained from <see cref="GetValueOffset(long)"/></param>
		/// <param name="value">The value to set at <paramref name="index"/></param>
		/// <returns>Success or not.</returns>
		/// <remarks>This method is usually quite expensive to call inside a loop to add values. Please use constructor instead.</remarks>
		protected abstract bool TryInsert(long index, long offset, T value);

		/// <inheritdoc/>
		public abstract SparseVector<T, TInd, TS, TSInd> GetSlice(long start, long count);

		/// <inheritdoc/>
		public abstract void GetSlice(long start, long count, SparseVector<T, TInd, TS, TSInd> overwrite);

		/// <inheritdoc/>
		public abstract void CopyTo(SparseVector<T, TInd, TS, TSInd> destination);

		/// <inheritdoc/>
		public abstract void SetSlice(long start, long count, SparseVector<T, TInd, TS, TSInd> value);

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
			T defaultSum = this.defaultValue * (this.length - this.NStored).As<T>();
			return defaultSum + ExtBlas.GeneralVectorReduce<T, TS>(ReduceOperation.Add, this.values, 1);
		}

		/// <inheritdoc/>
		public T AbsSum()
		{
			T defaultSum = T.Abs(this.defaultValue) * (this.length - this.NStored).As<T>();
			return defaultSum + Blas.AbsoluteValueSum<T, TS>(this.values, 1);
		}

		/// <inheritdoc/>
		public T Norm()
		{
			if (this.defaultValue == T.Zero)
				return Blas.Norm<T, TS>(this.values, 1);
			T abs = T.Abs(this.defaultValue);
			T defaultSum = abs * abs * (this.length - this.NStored).As<T>();
			T norm = Blas.Norm<T, TS>(this.values, 1);
			Numerics.Double n = (norm * norm + defaultSum).As<T, Numerics.Double>();
			return Numerics.Double.Sqrt(n).As<Numerics.Double, T>();
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
		public static SparseVector<T, TInd, TS, TSInd> operator *(SparseVector<T, TInd, TS, TSInd> vector!!, T scalar) => vector.ApplyToClone(v => v.Scale(scalar));

		/// <inheritdoc/>
		public static SparseVector<T, TInd, TS, TSInd> operator -(SparseVector<T, TInd, TS, TSInd> vector) => vector * (-T.One);

		/// <inheritdoc/>
		public static SparseVector<T, TInd, TS, TSInd> operator *(T scalar, SparseVector<T, TInd, TS, TSInd> vector) => vector * scalar;

		/// <inheritdoc/>
		public static SparseVector<T, TInd, TS, TSInd> operator /(SparseVector<T, TInd, TS, TSInd> vector, T scalar) => vector * (T.One / scalar);

		/// <inheritdoc/>
		public static DenseVector<T, TS> operator +(SparseVector<T, TInd, TS, TSInd> left, DenseVector<T, TS> right) => right.ApplyToClone(v => SparseOperation<T, TInd, TS, TSInd>.AddBy(v, left, T.One));

		/// <inheritdoc/>
		public static DenseVector<T, TS> operator -(DenseVector<T, TS> left,  SparseVector<T, TInd, TS, TSInd> right) => left.ApplyToClone(v => SparseOperation<T, TInd, TS, TSInd>.AddBy(v, right, -T.One));

		/// <inheritdoc/>
		public static DenseVector<T, TS> operator -(SparseVector<T, TInd, TS, TSInd> left, DenseVector<T, TS> right) => right.ApplyToClone(v =>
		{
			SparseOperation<T, TInd, TS, TSInd>.AddBy(v, left, -T.One);
			v.Scale(-T.One);
		});

		/// <inheritdoc/>
		public static SparseVector<T, TInd, TS, TSInd> operator +(SparseVector<T, TInd, TS, TSInd> left, SparseVector<T, TInd, TS, TSInd> right) => SparseOperation<T, TInd, TS, TSInd>.VectorsAdd(T.One, left, right);

		/// <inheritdoc/>
		public static SparseVector<T, TInd, TS, TSInd> operator -(SparseVector<T, TInd, TS, TSInd> left!!, SparseVector<T, TInd, TS, TSInd> right!!) => SparseOperation<T, TInd, TS, TSInd>.VectorsAdd(-T.One, right, left);

		/// <inheritdoc/>
		public static DenseVector<T, TS> operator *(DenseMatrix<T, TS> matrix!!, SparseVector<T, TInd, TS, TSInd> vector!!) => SparseOperation<T, TInd, TS, TSInd>.MatrixMultiplyVector(matrix, vector, T.One);

		/// <inheritdoc/>
		public static DenseVector<T, TS> operator *(SparseVector<T, TInd, TS, TSInd> vector!!, DenseMatrix<T, TS> matrix!!) => SparseOperation<T, TInd, TS, TSInd>.MatrixMultiplyVector(matrix, vector, T.One, MatrixOperation.Transpose);
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
		///Statically create a new sparse vector of type <see cref="SparseVector{T, TInd, TS, TSInd}"/> from <paramref name="dense"/> vector truncating by <paramref name="threshold"/>.
		/// </summary>
		/// <param name="dense">The input dense vector to convert</param>
		/// <param name="format">The target format</param>
		/// <param name="defaultValue">The target default value</param>
		/// <param name="threshold">The threshold used to truncate to sparse array</param>
		/// <returns>The created sparse vector of type <see cref="SparseVector{T, TInd, TS, TSInd}"/>.</returns>
		public static SparseVector<T, TInd, TS, TSInd> FromDense(DenseVector<T, TS> dense, SparseFormat format, T defaultValue, double threshold = 0)
		{
			var sparse = new SparseArrayWrapper<T, TInd, TS, TSInd>(defaultValue, format);
			SpConv.VectorDenseToSparse(dense.Storage, dense.Stride, ref sparse, threshold);
			return Create(sparse);
		}

		/// <inheritdoc/>
		public abstract SparseVector<T, TInd, TS, TSInd> CreateAlike();
		#endregion

		#region serialization
		/// <summary>
		/// The <see cref="JsonSerializerOptions"/> used for <see cref="JsonSerialize"/> and <see cref="JsonDeserialize(string)"/>.
		/// </summary>
		protected static JsonSerializerOptions JsonOptions => new()
		{
			Converters = { TS.JsonConverter, TSInd.JsonConverter },
			WriteIndented = true,
		};

		/// <inheritdoc/>
		public abstract string JsonSerialize();
		#endregion

		#region string
		static string IMainPropertyFormattable<SparseVector<T, TInd, TS, TSInd>>.StringMain => nameof(SparseVector<T, TInd, TS, TSInd>);

		static IEnumerable<string> IMainPropertyFormattable<SparseVector<T, TInd, TS, TSInd>>.PropertyNames => new[] { "DataType", "IndexType", "Format", "DefaultValue", "Values", "Indices" };

		IEnumerable<object?> IMainPropertyFormattable<SparseVector<T, TInd, TS, TSInd>>.PropertyValues => new object[] { T.Type, TInd.Type, this.Format, this.defaultValue, this.values, this.indices };

		/// <inheritdoc/>
		public override string ToString() => IMainPropertyFormattable<SparseVector<T, TInd, TS, TSInd>>.ToString(this);

		/// <inheritdoc/>
		public abstract string Print(PrintSettings? settings = null);
		#endregion

		#region protected static
		/// <summary>
		/// Encapsulates a method that statically create a new sparse vector from the given <paramref name="wrapper"/>.
		/// </summary>
		/// <param name="wrapper">The <see cref="SparseArrayWrapper{TVal, TInd, TSVal, TSInd}"/> to create from.</param>
		/// <param name="vector">A created <see cref="SparseVector{T, TInd, TS, TSInd}"/> from the given <paramref name="wrapper"/></param>
		/// <returns>Success or not.</returns>
		protected delegate bool TryCreateFromWrapper(in SparseArrayWrapper<T, TInd, TS, TSInd> wrapper, [NotNullWhen(true)] out SparseVector<T, TInd, TS, TSInd>? vector);

		/// <summary>
		/// The list used to store the <see cref="TryCreateFromWrapper"/>s for sub-classes.
		/// </summary>
		/// <remarks>Any sub-class that inherits <see cref="SparseVector{T, TInd, TS, TSInd}"/> SHALL add its own <see cref="TryCreateFromWrapper"/> implementation to this list.</remarks>
		protected static readonly List<TryCreateFromWrapper> Creators = new();

		/// <summary>
		/// Statically create a new sparse vector from the given <paramref name="wrapper"/>.
		/// </summary>
		/// <param name="wrapper">The <see cref="SparseArrayWrapper{TVal, TInd, TSVal, TSInd}"/> to create from.</param>
		/// <returns>A created <see cref="SparseVector{T, TInd, TS, TSInd}"/> from the given <paramref name="wrapper"/>.</returns>
		/// <exception cref="NotSupportedException">If none of the creators in sub-classes can be used to create a <see cref="SparseVector{T, TInd, TS, TSInd}"/></exception>
		public static SparseVector<T, TInd, TS, TSInd> Create(in SparseArrayWrapper<T, TInd, TS, TSInd> wrapper)
		{
			foreach (var creator in Creators)
			{
				if (creator(in wrapper, out var mat))
					return mat;
			}
			wrapper.DisposeAll();
			throw new NotSupportedException(Resources.SparseError.FormatNotSupport);
		}

		/// <summary>
		/// Encapsulates a method that statically deserialize the given <paramref name="json"/> to a new sparse matrix.
		/// </summary>
		/// <param name="json">The JSON string to create from.</param>
		/// <param name="matrix">A created <see cref="SparseVector{T, TInd, TS, TSInd}"/> from the given <paramref name="json"/></param>
		/// <returns>Success or not.</returns>
		protected delegate bool TryDeserialize(string json, [NotNullWhen(true)] out SparseVector<T, TInd, TS, TSInd>? matrix);

		/// <summary>
		/// The list used to store the JSON deserializers for sub-classes.
		/// </summary>
		/// <remarks>Any sub-class that inherits <see cref="SparseVector{T, TInd, TS, TSInd}"/> SHALL add its own <see cref="TryDeserialize"/> implementation to this list.</remarks>
		protected static readonly List<TryDeserialize> Deserializers = new();

		/// <inheritdoc/>
		public static SparseVector<T, TInd, TS, TSInd> JsonDeserialize(string json!!)
		{
			foreach (var deserializer in Deserializers)
			{
				if (deserializer(json, out var mat))
					return mat;
			}
			throw new NotSupportedException(Resources.SparseError.FormatNotSupport);
		}
		#endregion
	}
}
