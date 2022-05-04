using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;

using Althea.Helpers;
using Althea.LinearAlgebra;
using Althea.Linq;
using Althea.NativeTypes;
using Althea.Storage;

using Blas = Althea.LinearAlgebra.Dense.BlasApiSelector;
using ExtBlas = Althea.LinearAlgebra.Dense.ExtendBlasApiSelector;
using SpConv = Althea.LinearAlgebra.Sparse.ConversionApiSelector;


namespace Althea.Array
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
		IMatrixVectorMultiplyOperators<T, DenseVector<T, TS>, DenseVector<T, TS>, SparseMatrix<T, TInd, TS, TSInd>>,
		IMatrixUnaryOperators<T, SparseMatrix<T, TInd, TS, TSInd>, SparseMatrix<T, TInd, TS, TSInd>>,
		IMatrixBinaryOperators<T, SparseMatrix<T, TInd, TS, TSInd>, DenseMatrix<T, TS>, DenseMatrix<T, TS >>,
		IMatrixBinaryOperators<T, SparseMatrix<T, TInd, TS, TSInd>, SparseMatrix<T, TInd, TS, TSInd>, SparseMatrix<T, TInd, TS, TSInd>>
		where T : unmanaged, INumber<T> where TInd : unmanaged, IBinaryInteger<TInd>
		where TS : class, IStorage<T, TS> where TSInd : class, IStorage<TInd, TSInd>
	{
		#region basic
		private readonly long rows, cols;
		private long nnz;

		/// <summary>
		/// The row and column index array's storage of this sparse matrix.
		/// </summary>
		protected readonly TSInd rowIndices, colIndices;

		private readonly TS values;

		private T defaultValue;

		ReadOnlySpan<long> IValueArray<T, SparseMatrix<T, TInd, TS, TSInd>>.Size => SpanHelper.CreateReadOnlySpan(in this.rows, 2);

		ReadOnlySpan<long> IArray<T>.Size => SpanHelper.CreateReadOnlySpan(in this.rows, 2);
		ReadOnlySpan<TS> ISparseArray<T, TInd, TS, TSInd>.ValueStorages => SpanHelper.CreateReadOnlySpan(in this.values, 1);
		ReadOnlySpan<TSInd> ISparseArray<T, TInd, TS, TSInd>.IndexStorages => SpanHelper.CreateReadOnlySpan(in this.rowIndices, 2);
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
		public TS Storage => this.values.MakeReference(0, this.nnz);

		/// <summary>
		/// Get the row index array's storage of this sparse matrix.
		/// </summary>
		public virtual TSInd RowIndexStorage => this.rowIndices.MakeReference(0, this.nnz);
		/// <summary>
		/// Get the column index array's storage of this sparse matrix.
		/// </summary>
		public virtual TSInd ColIndexStorage => this.colIndices.MakeReference(0, this.nnz);

		/// <inheritdoc/>
		public long Length => this.rows * this.cols;

		/// <inheritdoc/>
		public long NStored
		{
			get => this.nnz;
			protected set => this.nnz = value < this.nnz || value > this.MaxStored ? throw new ArgumentOutOfRangeException(nameof(value)) : value;
		}

		/// <summary>
		/// Get the number of maximum possible elements that can be stored in this sparse matrix.
		/// </summary>
		protected long MaxStored => this.values.Length;

		/// <summary>
		/// Create a new <see cref="SparseMatrix{T, TInd, TS, TSInd}"/> with given parameters.
		/// </summary>
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
		protected SparseMatrix(long rows, long cols, TS values!!, TSInd rowIndices!!, TSInd colIndices!!, T defaultValue = default, long nnz = -1)
		{
			this.defaultValue = defaultValue;
			if (rows <= 0)
				throw new ArgumentOutOfRangeException(nameof(rows), Resources.ParameterError.MustPositive);
			if (cols <= 0)
				throw new ArgumentOutOfRangeException(nameof(cols), Resources.ParameterError.MustPositive);
			if (nnz < 0)
				nnz = values.Length;
			this.rows = rows; this.cols = cols;
			this.nnz = nnz;
			long actualLength = values.Length;
			if (actualLength >= rows * cols)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(values));

			this.values = values.AddToManager();
			this.rowIndices = rowIndices.AddToManager();
			this.colIndices = colIndices.AddToManager();
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			this.values.SafeDispose();
			this.rowIndices.SafeDispose();
			this.colIndices.SafeDispose();
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Create an empty sparse matrix.
		/// </summary>
		protected SparseMatrix()
		{
			this.values = TS.Empty; this.rowIndices = TSInd.Empty; this.colIndices = TSInd.Empty;
		}

		static SparseMatrix<T, TInd, TS, TSInd> IValueArray<T, SparseMatrix<T, TInd, TS, TSInd>>.Empty => new CoordinateSparseMatrix<T, TInd, TS, TSInd>();
		#endregion

		#region equality
		/// <inheritdoc/>
		public virtual bool Equals(SparseMatrix<T, TInd, TS, TSInd>? other)
		{
			if (other is null)
				return false;
			return this.Format == other.Format && this.defaultValue == other.defaultValue && this.nnz == other.nnz && this.values == other.values && this.rowIndices == other.rowIndices && this.colIndices == other.colIndices;
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
		public override int GetHashCode() => HashCode.Combine(this.Format, this.defaultValue, this.nnz, this.values, this.rowIndices, this.colIndices);
		#endregion

		#region index
		/// <summary>
		/// When implemented by a derived class, get the offsets to the <see cref="Storage"/> (and other index storages) of the corresponding <paramref name="row"/> and <paramref name="col"/> index.
		/// </summary>
		/// <param name="row">The presenting row index</param>
		/// <param name="col">The presenting column index</param>
		/// <param name="offsets">The <see cref="Span{T}"/> used to store the result. If it is of length 1, only the offset to <see cref="Storage"/> shall be computed; otherwise, all offsets shall be computed.</param>
		/// <returns>The element in (<paramref name="row"/>, <paramref name="col"/>) is stored or not. If not, the offsets shall be insertion positions.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected abstract bool GetOffsets(long row, long col, Span<long> offsets);

		/// <inheritdoc/>
		public T this[long x, long y]
		{
			get
			{
				Span<long> offset = stackalloc long[1];
				if (!this.GetOffsets(x, y, offset))
					return this.defaultValue;
				else
					return (this.values + offset[0]).ToManaged<T, TS>();
			}
			set
			{
				Span<long> offsets = stackalloc long[1 + ((ISparseArray<T, TInd, TS, TSInd>)this).IndexStorages.Length];
				if (this.GetOffsets(x, y, offsets))
				{
					(this.values + offsets[0]).FromManaged(value);
				}
				else
				{
					if (!this.TryInsert(x, y, offsets, value))
						throw new ArgumentException(Resources.SparseError.CannotSetSparse, nameof(value));
				}
			}
		}

		/// <summary>
		/// When implemented by a derived class, set the element at (<paramref name="row"/>, <paramref name="col"/>) to <paramref name="value"/> when that element is not stored. 
		/// </summary>
		/// <param name="row">The presenting row index to set <paramref name="value"/> at</param>
		/// <param name="col">The presenting column index to set <paramref name="value"/> at</param>
		/// <param name="value">The value to set at (<paramref name="row"/>, <paramref name="col"/>)</param>
		/// <param name="offsets">The <see cref="Span{T}"/> of offsets obtained from <see cref="GetOffsets(long, long, Span{long})"/></param>
		/// <returns>Success or not.</returns>
		/// <remarks>This method is usually quite expensive to call inside a loop to add values. Please use constructor instead.</remarks>
		protected abstract bool TryInsert(long row, long col, Span<long> offsets, T value);

		/// <inheritdoc/>
		public SparseMatrix<T, TInd, TS, TSInd> GetSubmatrix(long offsetRow, long countRow, long offsetCol, long countCol)
		{
			var sub = new SparseArrayWrapper<T, TInd, TS, TSInd>(this.defaultValue, SparseFormat.Any);
			SpConv.SparseMatrixGetSlice(this, MatrixSliceWrapper.Create(offsetRow, countRow, offsetCol, countCol, this), ref sub);
			return Create(sub);
		}

		/// <inheritdoc/>
		public void GetSubmatrix(long offsetRow, long countRow, long offsetCol, long countCol, SparseMatrix<T, TInd, TS, TSInd> overwrite)
		{
			var sub = SparseArrayWrapper<T, TInd, TS, TSInd>.Create(overwrite);
			SpConv.SparseMatrixGetSlice(this, MatrixSliceWrapper.Create(offsetRow, countRow, offsetCol, countCol, this, overwrite), ref sub);
		}

		/// <inheritdoc/>
		public void SetSubmatrix(long offsetRow, long countRow, long offsetCol, long countCol, SparseMatrix<T, TInd, TS, TSInd> value)
		{
			SpConv.SparseMatrixSetSlice(this, MatrixSliceWrapper.Create(offsetRow, countRow, offsetCol, countCol, this, value), value);
		}

		/// <inheritdoc/>
		public abstract void CopyTo(SparseMatrix<T, TInd, TS, TSInd> destination);
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
			if (NumberType<T>.IsComplex)
			{
				ExtBlas.GeneralVectorUnary<T, TS, TS>(UnaryOperation.Conjugate, this.values, 1, this.values, 1);
				this.defaultValue = this.defaultValue.Conjugate();
			}
		}
		#endregion

		#region simple aggregation operations
		/// <inheritdoc/>
		public T Sum()
		{
			T defaultSum = this.defaultValue * T.Create(this.rows * this.cols - this.values.Length);
			return defaultSum + ExtBlas.GeneralVectorReduce<T, TS>(ReduceOperation.Add, this.values, 1);
		}

		/// <inheritdoc/>
		public T AbsSum()
		{
			T defaultSum = T.Abs(this.defaultValue) * T.Create(this.rows * this.cols - this.values.Length);
			return defaultSum + Blas.AbsoluteValueSum<T, TS>(this.values, 1);
		}

		/// <inheritdoc/>
		public T Norm()
		{
			if (this.defaultValue == T.Zero)
				return Blas.Norm<T, TS>(this.values, 1);
			T abs = T.Abs(this.defaultValue);
			T defaultSum = abs * abs * T.Create(this.rows * this.cols - this.values.Length);
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

		#region operators
		/// <inheritdoc/>
		public static DenseVector<T, TS> operator *(SparseMatrix<T, TInd, TS, TSInd> matrix, DenseVector<T, TS> vector) => SparseOperation<T, TInd, TS, TSInd>.MatrixMultiplyVector(matrix, vector, T.One);

		/// <inheritdoc/>
		public static DenseVector<T, TS> operator *(DenseVector<T, TS> vector, SparseMatrix<T, TInd, TS, TSInd> matrix) => SparseOperation<T, TInd, TS, TSInd>.MatrixMultiplyVector(matrix, vector, T.One, MatrixOperation.Transpose);

		/// <inheritdoc/>
		public static SparseMatrix<T, TInd, TS, TSInd> operator *(SparseMatrix<T, TInd, TS, TSInd> matrix, T scalar) => SparseOperation<T, TInd, TS, TSInd>.AddMatrices((SparseMatrix<T, TInd, TS, TSInd>?)null, default, matrix, scalar);

		/// <inheritdoc/>
		public static SparseMatrix<T, TInd, TS, TSInd> operator -(SparseMatrix<T, TInd, TS, TSInd> matrix) => matrix * (-T.One);

		/// <inheritdoc/>
		public static SparseMatrix<T, TInd, TS, TSInd> operator *(T scalar, SparseMatrix<T, TInd, TS, TSInd> matrix) => matrix * scalar;

		/// <inheritdoc/>
		public static SparseMatrix<T, TInd, TS, TSInd> operator /(SparseMatrix<T, TInd, TS, TSInd> matrix, T scalar) => matrix * (T.One / scalar);

		/// <inheritdoc/>
		public static SparseMatrix<T, TInd, TS, TSInd> operator ^(SparseMatrix<T, TInd, TS, TSInd> matrix, MatrixOperation operation) => SparseOperation<T, TInd, TS, TSInd>.AddMatrices((SparseMatrix<T, TInd, TS, TSInd>?)null, default, matrix, T.One, default, operation);

		/// <summary>
		/// When implemented by a derived class, get a referenced <see cref="SparseMatrix{T, TInd, TS, TSInd}"/> which is the transpose of this matrix.
		/// </summary>
		/// <returns>A referenced <see cref="SparseMatrix{T, TInd, TS, TSInd}"/> which is the transpose of this matrix.</returns>
		public abstract SparseMatrix<T, TInd, TS, TSInd> RefTranspose();

		/// <inheritdoc/>
		public static SparseMatrix<T, TInd, TS, TSInd> operator +(SparseMatrix<T, TInd, TS, TSInd> left, SparseMatrix<T, TInd, TS, TSInd> right) => SparseOperation<T, TInd, TS, TSInd>.AddMatrices(left, T.One, right, T.One);

		/// <inheritdoc/>
		public static SparseMatrix<T, TInd, TS, TSInd> operator -(SparseMatrix<T, TInd, TS, TSInd> left, SparseMatrix<T, TInd, TS, TSInd> right) => SparseOperation<T, TInd, TS, TSInd>.AddMatrices(left, T.One, right, -T.One);

		/// <inheritdoc/>
		public static SparseMatrix<T, TInd, TS, TSInd> operator *(SparseMatrix<T, TInd, TS, TSInd> left, SparseMatrix<T, TInd, TS, TSInd> right) => SparseOperation<T, TInd, TS, TSInd>.MultiplyMatries(left, right, T.One);

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator +(SparseMatrix<T, TInd, TS, TSInd> left, DenseMatrix<T, TS> right) => SparseOperation<T, TInd, TS, TSInd>.AddMatrices(left, T.One, right, T.One);

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator +(DenseMatrix<T, TS> left, SparseMatrix<T, TInd, TS, TSInd> right) => SparseOperation<T, TInd, TS, TSInd>.AddMatrices(left, T.One, right, T.One);

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator -(SparseMatrix<T, TInd, TS, TSInd> left, DenseMatrix<T, TS> right) => SparseOperation<T, TInd, TS, TSInd>.AddMatrices(left, T.One, right, -T.One);

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator -(DenseMatrix<T, TS> left, SparseMatrix<T, TInd, TS, TSInd> right) => SparseOperation<T, TInd, TS, TSInd>.AddMatrices(left, T.One, right, -T.One);

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator *(SparseMatrix<T, TInd, TS, TSInd> left, DenseMatrix<T, TS> right) => SparseOperation<T, TInd, TS, TSInd>.MultiplyMatries(left, right, T.One);

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator *(DenseMatrix<T, TS> left, SparseMatrix<T, TInd, TS, TSInd> right) => SparseOperation<T, TInd, TS, TSInd>.MultiplyMatries(left, right, T.One);
		#endregion

		#region conversion and clone
		/// <summary>
		/// Create a new <see cref="DenseMatrix{T, TS}"/> from this sparse matrix.
		/// </summary>
		/// <returns>The created <see cref="DenseMatrix{T, TS}"/>.</returns>
		public DenseMatrix<T, TS> ToDense()
		{
			TS dense = this.values.ResizeAlike(this.rows * this.cols);
			SpConv.MatrixSparseToDense(this, dense, this.rows);
			return new(dense, this.rows, this.cols);
		}

		/// <summary>
		/// When implemented by a derived class, statically create a new <see cref="SparseMatrix{T, TInd, TS, TSInd}"/> from <paramref name="dense"/> matrix truncating by <paramref name="threshold"/>.
		/// </summary>
		/// <param name="dense">The input dense matrix to convert</param>
		/// <param name="format">The target format</param>
		/// <param name="defaultValue">The target default value</param>
		/// <param name="threshold">The threshold used to truncate to sparse array</param>
		/// <returns>The created <see cref="SparseMatrix{T, TInd, TS, TSInd}"/>.</returns>
		public static SparseMatrix<T, TInd, TS, TSInd> FromDense(DenseMatrix<T, TS> dense, SparseFormat format, T defaultValue, double threshold = 0)
		{
			var sparse = new SparseArrayWrapper<T, TInd, TS, TSInd>(defaultValue, format);
			SpConv.MatrixDenseToSparse(dense.Storage, dense.LeadDim, ref sparse, threshold);
			return Create(sparse);
		}

		/// <inheritdoc/>
		public abstract SparseMatrix<T, TInd, TS, TSInd> CreateAlike();
		#endregion

		#region string
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

		static string IMainPropertyFormattable<SparseMatrix<T, TInd, TS, TSInd>>.StringMain => nameof(SparseMatrix<T, TInd, TS, TSInd>);

		static IEnumerable<string> IMainPropertyFormattable<SparseMatrix<T, TInd, TS, TSInd>>.PropertyNames => new[] { "DataType", "IndexType", "Format", "DefaultValue", "Size", "BlockSize", "Non-zeros", "Values", "RowIndices", "ColumnIndices" };

		IEnumerable<object?> IMainPropertyFormattable<SparseMatrix<T, TInd, TS, TSInd>>.PropertyValues => new object[] { Unmanaged<T>.DataType, Unmanaged<TInd>.DataType, this.Format, this.defaultValue, $"{this.rows}x{this.cols}", ((ISparseArray<T, TInd, TS, TSInd>)this).BlockSize.SpanJoin('x'), this.nnz, this.values, this.rowIndices, this.colIndices };

		/// <inheritdoc/>
		public override string ToString() => IMainPropertyFormattable<SparseMatrix<T, TInd, TS, TSInd>>.ToString(this);

		/// <inheritdoc/>
		public abstract string Print(PrintSettings? settings = null);
		#endregion

		#region protected static
		/// <summary>
		/// Encapsulates a method that statically create a new sparse matrix from the given <paramref name="wrapper"/>.
		/// </summary>
		/// <param name="wrapper">The <see cref="SparseArrayWrapper{TVal, TInd, TSVal, TSInd}"/> to create from.</param>
		/// <param name="matrix">A created <see cref="SparseMatrix{T, TInd, TS, TSInd}"/> from the given <paramref name="wrapper"/></param>
		/// <returns>Success or not.</returns>
		protected delegate bool TryCreateFromWrapper(in SparseArrayWrapper<T, TInd, TS, TSInd> wrapper, [NotNullWhen(true)] out SparseMatrix<T, TInd, TS, TSInd>? matrix);

		/// <summary>
		/// The list used to store the <see cref="TryCreateFromWrapper"/>s for sub-classes.
		/// </summary>
		/// <remarks>Any sub-class that inherits <see cref="SparseMatrix{T, TInd, TS, TSInd}"/> SHALL add its own <see cref="TryCreateFromWrapper"/> implementation to this list.</remarks>
		protected static readonly List<TryCreateFromWrapper> Creators = new();

		/// <summary>
		/// Statically create a new sparse matrix from the given <paramref name="wrapper"/>.
		/// </summary>
		/// <param name="wrapper">The <see cref="SparseArrayWrapper{TVal, TInd, TSVal, TSInd}"/> to create from.</param>
		/// <returns>A created <see cref="SparseMatrix{T, TInd, TS, TSInd}"/> from the given <paramref name="wrapper"/>.</returns>
		/// <exception cref="NotSupportedException">If none of the creators in sub-classes can be used to create a <see cref="SparseMatrix{T, TInd, TS, TSInd}"/></exception>
		public static SparseMatrix<T, TInd, TS, TSInd> Create(in SparseArrayWrapper<T, TInd, TS, TSInd> wrapper)
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
		/// <param name="matrix">A created <see cref="SparseMatrix{T, TInd, TS, TSInd}"/> from the given <paramref name="json"/></param>
		/// <returns>Success or not.</returns>
		protected delegate bool TryDeserialize(string json, [NotNullWhen(true)] out SparseMatrix<T, TInd, TS, TSInd>? matrix);

		/// <summary>
		/// The list used to store the JSON deserializers for sub-classes.
		/// </summary>
		/// <remarks>Any sub-class that inherits <see cref="SparseMatrix{T, TInd, TS, TSInd}"/> SHALL add its own <see cref="TryDeserialize"/> implementation to this list.</remarks>
		protected static readonly List<TryDeserialize> Deserializers = new();

		/// <inheritdoc/>
		public static SparseMatrix<T, TInd, TS, TSInd> JsonDeserialize(string json!!)
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
