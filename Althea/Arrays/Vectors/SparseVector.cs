using System.Diagnostics.CodeAnalysis;
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
	/// The abstract sparse vector abstract class whose value storage is of type <typeparamref name="TS"/> and sorted index storage is of type <typeparamref name="TSInd"/>.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TS">The storage type used by the value storage</typeparam>
	/// <typeparam name="TInd">Any unmanaged integer number as the index type</typeparam>
	/// <typeparam name="TSInd">The index storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
	[StructLayout(LayoutKind.Sequential, Pack = sizeof(long))]
	public abstract class SparseVector<T, TInd, TS, TSInd> : ISparseArray<T, TInd, TS, TSInd>,
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

		private readonly TS values;

		private readonly T defaultValue;

		ReadOnlySpan<long> IValueArray<T, SparseVector<T, TInd, TS, TSInd>>.Size => ReflectionHelper.CreateReadOnlySpan(in this.length, 1);

		ReadOnlySpan<long> ISparseArray<T>.Size => ReflectionHelper.CreateReadOnlySpan(in this.length, 1);
		ReadOnlySpan<TS> ISparseArray<T, TInd, TS, TSInd>.ValueStorages => ReflectionHelper.CreateReadOnlySpan(in this.values, 1);
		ReadOnlySpan<TSInd> ISparseArray<T, TInd, TS, TSInd>.IndexStorages => ReflectionHelper.CreateReadOnlySpan(in this.indices, 1);
		ReadOnlySpan<TInd> ISparseArray<T, TInd, TS, TSInd>.BlockSize => default;

		bool ICheckValid.IsValid() => (this.values?.IsValid() ?? false) && (this.indices?.IsValid() ?? false);

		/// <inheritdoc/>
		public abstract SparseFormat Format { get; }

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
		/// Create a new <see cref="SparseVector{T, TInd, TS, TSInd}"/> with given parameters.
		/// </summary>
		/// <param name="defaultValue">The default value</param>
		/// <param name="length">The presenting length</param>
		/// <param name="values">The original value array</param>
		/// <param name="indices">The original index array</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="length"/> ≤ 0</exception>
		/// <exception cref="ArgumentException">If the <paramref name="values"/> is too short</exception>
		/// <remarks>The validness of the detail values (such as sorted or not) in these storages are not checked for performance issues.</remarks>
		protected SparseVector(long length, TS values!!, TSInd indices!!, T defaultValue = default)
		{
			this.defaultValue = defaultValue;
			if (length < 0)
				throw new ArgumentOutOfRangeException(nameof(length), Resources.ParameterError.CannotNegative);
			this.length = length;
			if (length < values.Length || values.Length < indices.Length)
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
		/// Deconstructor to be invoked by GC.
		/// </summary>
		~SparseVector()
		{
			this.Dispose();
		}

		/// <summary>
		/// Create an empty sparse vector
		/// </summary>
		protected SparseVector()
		{
			this.values = TS.Empty; this.indices = TSInd.Empty;
		}

		static SparseVector<T, TInd, TS, TSInd> IValueArray<T, SparseVector<T, TInd, TS, TSInd>>.Empty => new CoorinatedSparseVector<T, TInd, TS, TSInd>();

		/// <summary>
		/// When implemented by a derived class, create a new sparse vector from the given <paramref name="wrapper"/>.
		/// </summary>
		/// <param name="wrapper">The <see cref="SparseArrayWrapper{TVal, TInd, TSVal, TSInd}"/> to create from.</param>
		/// <param name="vector">A created <see cref="SparseVector{T, TInd, TS, TSInd}"/> from the given <paramref name="wrapper"/></param>
		/// <returns>Success or not.</returns>
		protected abstract bool TryCreateFromWrapper(in SparseArrayWrapper<T, TInd, TS, TSInd> wrapper, [NotNullWhen(true)] out SparseVector<T, TInd, TS, TSInd>? vector);
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
		/// <returns>The offset in <typeparamref name="T"/> compared to <see cref="Storage"/>.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected abstract long GetValueOffset(long index);

		/// <inheritdoc/>
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
				if (offset < 0)
					throw new ArgumentException(Resources.SparseError.CannotSetSparse, nameof(index));
				(this.values + offset).FromManaged(value);
			}
		}

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
			T defaultSum = this.defaultValue * T.Create(this.length - this.values.Length);
			return defaultSum + ExtBlas.AggregateSum<T, TS>(this.values, 1);
		}

		/// <inheritdoc/>
		public T AbsSum()
		{
			T defaultSum = T.Abs(this.defaultValue) * T.Create(this.length - this.values.Length);
			return defaultSum + Blas.AbsoluteValueSum<T, TS>(this.values, 1);
		}

		/// <inheritdoc/>
		public T Norm()
		{
			if (this.defaultValue == T.Zero)
				return Blas.Norm<T, TS>(this.values, 1);
			T abs = T.Abs(this.defaultValue);
			T defaultSum = abs * abs * T.Create(this.length - this.values.Length);
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
			try
			{
				if (left.TryCreateFromWrapper(in wrapper, out var vec))
					return vec;
				if (right.TryCreateFromWrapper(in wrapper, out vec))
					return vec;
				throw new NotSupportedException(Resources.SparseError.FormatNotSupport);
			}
			catch (Exception)
			{
				wrapper.DisposeAll();
				throw;
			}
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
		/// When implemented by a derived class, statically create a new sparse vector of type <see cref="SparseVector{T, TInd, TS, TSInd}"/> from <paramref name="dense"/> vector truncating by <paramref name="threshold"/>.
		/// </summary>
		/// <param name="dense">The input dense vector to convert</param>
		/// <param name="format">The target format</param>
		/// <param name="defaultValue">The target default value</param>
		/// <param name="threshold">The threshold used to truncate to sparse array</param>
		/// <returns>The created sparse vector of type <see cref="SparseVector{T, TInd, TS, TSInd}"/>.</returns>
		public abstract SparseVector<T, TInd, TS, TSInd> FromDense(DenseVector<T, TS> dense, SparseFormat format, T defaultValue, double threshold = 0);

		/// <inheritdoc/>
		public abstract SparseVector<T, TInd, TS, TSInd> CreateAlike();
		#endregion

		#region serialization
		/// <summary>
		/// The <see cref="JsonSerializerOptions"/> used for <see cref="JsonSerialize"/> and <see cref="JsonDeserialize(string)"/>.
		/// </summary>
		protected static readonly JsonSerializerOptions JsonOptions = new()
		{
			Converters = { TS.JsonConverter, TSInd.JsonConverter },
			WriteIndented = true,
		};

		/// <inheritdoc/>
		public abstract string JsonSerialize();

		/// <inheritdoc/>
		public abstract SparseVector<T, TInd, TS, TSInd> JsonDeserialize(string json);
		#endregion

		#region string
		static string IMainPropertyFormattable<SparseVector<T, TInd, TS, TSInd>>.StringMain => nameof(SparseVector<T, TInd, TS, TSInd>);

		static IEnumerable<string> IMainPropertyFormattable<SparseVector<T, TInd, TS, TSInd>>.PropertyNames => new[] { "DataType", "IndexType", "Format", "DefaultValue", "Values", "Indices" };

		IEnumerable<object?> IMainPropertyFormattable<SparseVector<T, TInd, TS, TSInd>>.PropertyValues => new object[] { Unmanaged<T>.DataType, Unmanaged<TInd>.DataType, this.Format, this.defaultValue, this.values, this.indices };

		/// <inheritdoc/>
		public override string ToString() => IMainPropertyFormattable<SparseVector<T, TInd, TS, TSInd>>.ToString(this);

		/// <inheritdoc/>
		public abstract string Print(PrintSettings? settings = null);
		#endregion
	}

	/// <summary>
	/// The coordinated non-blocked sparse vector class that inherits <see cref="SparseVector{T, TInd, TS, TSInd}"/>.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TS">The storage type used by the value storage</typeparam>
	/// <typeparam name="TInd">Any unmanaged integer number as the index type</typeparam>
	/// <typeparam name="TSInd">The index storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
	[StructLayout(LayoutKind.Sequential, Pack = sizeof(long))]
	public sealed class CoorinatedSparseVector<T, TInd, TS, TSInd> : SparseVector<T, TInd, TS, TSInd>
		where T : unmanaged, INumber<T> where TInd : unmanaged, IBinaryInteger<TInd>
		where TS : class, IStorage<T, TS> where TSInd : class, IStorage<TInd, TSInd>
	{
		#region basic
		/// <inheritdoc/>
		public override SparseFormat Format => new(SparseFormat.Type.Coordinated, SparseFormat.Blocking.Element, SparseFormat.Major.None);

		/// <summary>
		/// Create a new <see cref="CoorinatedSparseVector{T, TInd, TS, TSInd}"/> with given parameters.
		/// </summary>
		/// <param name="defaultValue">The default value</param>
		/// <param name="length">The presenting length</param>
		/// <param name="values">The original value array</param>
		/// <param name="indices">The original index array</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="length"/> ≤ 0</exception>
		/// <exception cref="ArgumentException">If <paramref name="values"/> is too short</exception>
		/// <remarks>The validness of the detail values (such as sorted or not) in these storages are not checked for performance issues.</remarks>
		public CoorinatedSparseVector(long length, TS values!!, TSInd indices!!, T defaultValue = default) : base(length, values, indices, defaultValue)
		{
			if (values.Length != indices.Length)
			{
				this.Dispose();
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(values));
			}
		}

		internal CoorinatedSparseVector() : base() { }

		/// <inheritdoc/>
		protected override bool TryCreateFromWrapper(in SparseArrayWrapper<T, TInd, TS, TSInd> wrapper, [NotNullWhen(true)] out SparseVector<T, TInd, TS, TSInd>? vector)
		{
			vector = null;
			if (wrapper.Format != this.Format)
				return false;
			if (wrapper.IndexStorages.Length != 1 || wrapper.ValueStorages.Length != 1 || wrapper.Size.Length != 1)
				return false;
			if (wrapper.IndexStorages[0].Length != wrapper.ValueStorages[0].Length || wrapper.Size[0] != wrapper.IndexStorages[0].Length)
				return false;
			vector = new CoorinatedSparseVector<T, TInd, TS, TSInd>(wrapper.Size[0], wrapper.ValueStorages[0], wrapper.IndexStorages[0], wrapper.DefaultValue);
			return true;
		}
		#endregion

		#region index
		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override long GetValueOffset(long index)
		{
			IBaseVector<T, SparseVector<T, TInd, TS, TSInd>>.CheckIndex(this, index);
			return SpConv.IndexFind(this.IndexStorage, true, TInd.Create(index));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private (long indexStart, long indexCount) GetSliceInfo(long start, long count, SparseVector<T, TInd, TS, TSInd>? sub = null)
		{
			IBaseVector<T, SparseVector<T, TInd, TS, TSInd>>.CheckRange(this, start, count, sub);
			if (sub is not null && sub.Format != this.Format)
				throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(sub));
			long indexStart = SpConv.IndexBound(this.IndexStorage, TInd.Create(start), true);
			long indexCount = SpConv.IndexBound(this.IndexStorage, TInd.Create(start + count), true);
			if (sub is not null)
			{
				if (sub.IndexStorage.Length != indexCount || sub.Storage.Length != indexCount)
					throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(sub));
			}
			return (indexStart, indexCount);
		}

		/// <inheritdoc/>
		public override CoorinatedSparseVector<T, TInd, TS, TSInd> GetSlice(long start, long count)
		{
			var (indexStart, indexCount) = this.GetSliceInfo(start, count);
			if (indexStart == 0 && indexCount == this.IndexStorage.Length)
				return new(count, this.Storage.Clone(), this.IndexStorage.Clone(), this.DefaultValue);

			var newVals = this.Storage.MakeReference(indexStart, indexCount);
			var newInds = this.IndexStorage.MakeReference(indexStart, indexCount);
			if (start != 0)
				newInds = newInds.ApplyToClone(ind => ExtBlas.PointWiseAddScalar(ind, 1, TInd.Create(-start)));
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
				ExtBlas.PointWiseAddScalar(overwrite.IndexStorage, 1, TInd.Create(-start));
		}

		/// <inheritdoc/>
		public override void CopyTo(SparseVector<T, TInd, TS, TSInd> destination)
		{
			if (destination.Format != this.Format)
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
				ExtBlas.PointWiseAddScalar(refInds, 1, TInd.Create(start));
		}
		#endregion

		#region conversion and clone
		/// <inheritdoc/>
		public override SparseVector<T, TInd, TS, TSInd> FromDense(DenseVector<T, TS> dense, SparseFormat format, T defaultValue, double threshold = 0)
		{
			var sparse = new SparseArrayWrapper<T, TInd, TS, TSInd>(defaultValue, format, default, default, default);
			SpConv.VectorDenseToSparse(ref sparse, dense.Storage, dense.Stride, threshold);
			if (this.TryCreateFromWrapper(in sparse, out var vec))
				return vec;
			sparse.DisposeAll();
			throw new NotSupportedException(Resources.SparseError.FormatNotSupport);
		}

		/// <inheritdoc/>
		public override CoorinatedSparseVector<T, TInd, TS, TSInd> CreateAlike() => new(this.Length, this.Storage.CreateAlike(), this.IndexStorage.CreateAlike(), this.DefaultValue);
		#endregion

		#region serialization
		private record struct ElementRepr(int Format, T Default, long Length, TS Values, TSInd Indices);

		private CoorinatedSparseVector(ElementRepr repr) : this(repr.Length, repr.Values, repr.Indices, repr.Default) { }

		/// <inheritdoc/>
		public override string JsonSerialize() => JsonSerializer.Serialize<ElementRepr>(new(this.Format.Data, this.DefaultValue, this.Length, this.Storage, this.IndexStorage), JsonOptions);

		/// <inheritdoc/>
		public override CoorinatedSparseVector<T, TInd, TS, TSInd> JsonDeserialize(string json!!) => new(JsonSerializer.Deserialize<ElementRepr>(json, JsonOptions));
		#endregion

		#region string
		/// <inheritdoc/>
		public override unsafe string Print(PrintSettings? settings = null)
		{
			settings ??= Settings.PrintSetting;
			int length = Math.Min((int)this.NStored, settings.Value.ArrayLength);
			Span<T> values = length.CheckStackLimit<T>() ?? stackalloc T[length];
			Span<long> indices = length.CheckStackLimit<long>() ?? stackalloc long[length];
			this.Storage.ToManaged(values);
			fixed (long* inds = indices)
			{
				var mp = new ManagedPureStorage<long>(new ManagedPointer(new(inds), length * sizeof(long)));
				ExtBlas.PointWiseCast<TInd, long, TSInd, ManagedPureStorage<long>>(this.IndexStorage, 1, mp, 1);
			}
			return values.ToSparseVectorString(indices, settings.Value.Precision) + (length == this.NStored ? "" : string.Format(Resources.Print.MoreStored, this.NStored - length));
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
	public sealed class SimpleBlockedSparseVector<T, TInd, TS, TSInd> : SparseVector<T, TInd, TS, TSInd>, ISparseArray<T, TInd, TS, TSInd>
		where T : unmanaged, INumber<T> where TInd : unmanaged, IBinaryInteger<TInd>
		where TS : class, IStorage<T, TS> where TSInd : class, IStorage<TInd, TSInd>
	{
		#region basic
		private readonly TInd blockSize;
		
		ReadOnlySpan<TInd> ISparseArray<T, TInd, TS, TSInd>.BlockSize => ReflectionHelper.CreateReadOnlySpan(in this.blockSize, 1);

		private long BS => this.blockSize.As<TInd, long>();

		/// <inheritdoc/>
		public override SparseFormat Format => new(SparseFormat.Type.Coordinated, SparseFormat.Blocking.Simple, SparseFormat.Major.None);

		/// <summary>
		/// Create a new <see cref="SimpleBlockedSparseVector{T, TInd, TS, TSInd}"/> with given parameters.
		/// </summary>
		/// <param name="defaultValue">The default value</param>
		/// <param name="length">The presenting length</param>
		/// <param name="values">The original value array</param>
		/// <param name="indices">The original index array of blocks</param>
		/// <param name="blockSize">The constant block size</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="length"/> ≤ 0</exception>
		/// <exception cref="ArgumentException">If <paramref name="values"/> is too short</exception>
		/// <remarks>The validness of the detail values (such as sorted or not) in these storages are not checked for performance issues.</remarks>
		public SimpleBlockedSparseVector(long length, TS values!!, TSInd indices!!, TInd blockSize, T defaultValue = default) : base(length, values, indices, defaultValue)
		{
			try
			{
				if (blockSize <= TInd.Zero)
					throw new ArgumentOutOfRangeException(nameof(blockSize), Resources.ParameterError.MustPositive);
				if (length % blockSize.As<TInd, long>() != 0)
					throw new ArgumentException(Resources.ArithmeticError.CannotDivide, nameof(blockSize));
				if (values.Length != indices.Length * blockSize.As<TInd, long>())
					throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(values));
			}
			catch (Exception)
			{
				this.Dispose();
				throw;
			}
			this.blockSize = blockSize;
		}

		internal SimpleBlockedSparseVector() : base() { }

		/// <inheritdoc/>
		protected override bool TryCreateFromWrapper(in SparseArrayWrapper<T, TInd, TS, TSInd> wrapper, [NotNullWhen(true)] out SparseVector<T, TInd, TS, TSInd>? vector)
		{
			vector = null;
			if (wrapper.Size.Length != 1 || wrapper.BlockSize.Length != 1 || wrapper.ValueStorages.Length != 1 || wrapper.IndexStorages.Length != 1)
				return false;
			var length = wrapper.Size[0];
			var blockSize = wrapper.BlockSize[0];
			var values = wrapper.ValueStorages[0];
			var indices = wrapper.IndexStorages[0];
			if (blockSize <= TInd.Zero || length % blockSize.As<TInd, long>() != 0)
				return false;
			if (values is null || indices is null)
				return false;
			if (values.Length != indices.Length * blockSize.As<TInd, long>())
				return false;
			vector = new SimpleBlockedSparseVector<T, TInd, TS, TSInd>(length, values, indices, blockSize, wrapper.DefaultValue);
			return true;
		}
		#endregion

		#region equality
		/// <inheritdoc/>
		public override bool Equals(SparseVector<T, TInd, TS, TSInd>? other)
		{
			if (!base.Equals(other))
				return false;
			return other is SimpleBlockedSparseVector<T, TInd, TS, TSInd> vec && this.blockSize == vec.blockSize;
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
			var (blockIndex, insideBlockOffset) = index.DivRem(this.BS);
			long offset = SpConv.IndexFind(this.IndexStorage, true, TInd.Create(blockIndex));
			if (offset >= 0)
				offset = offset * this.BS + insideBlockOffset;
			return offset;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private (long indexStart, long indexCount) GetSliceInfo(long start, long count, SparseVector<T, TInd, TS, TSInd>? sub = null)
		{
			IBaseVector<T, SparseVector<T, TInd, TS, TSInd>>.CheckRange(this, start, count, sub);
			if (sub is not null && (sub is not SimpleBlockedSparseVector<T, TInd, TS, TSInd> vec || vec.blockSize != this.blockSize))
				throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(sub));
			long indexStart, indexCount;
			if (start % this.BS != 0)
				throw new ArgumentException(Resources.SparseError.CannotCutSimpleBlocking, nameof(start));
			if (count % this.BS != 0)
				throw new ArgumentException(Resources.SparseError.CannotCutSimpleBlocking, nameof(start));
			start /= this.BS; count /= this.BS;
			indexStart = SpConv.IndexBound(this.IndexStorage, TInd.Create(start), true);
			indexCount = SpConv.IndexBound(this.IndexStorage, TInd.Create(start + count), true);
			indexCount -= indexStart;
			return (indexStart, indexCount);
		}

		/// <inheritdoc/>
		public override SimpleBlockedSparseVector<T, TInd, TS, TSInd> GetSlice(long start, long count)
		{
			var (indexStart, indexCount) = this.GetSliceInfo(start, count);
			if (indexStart == 0 && indexCount == this.IndexStorage.Length)
				return new(count, this.Storage.Clone(), this.IndexStorage.Clone(), this.blockSize, this.DefaultValue);

			var newVals = this.Storage.MakeReference(indexStart * this.BS, indexCount * this.BS);
			var newInds = this.IndexStorage.MakeReference(indexStart, indexCount);
			if (start == 0)
				newInds = newInds.ApplyToClone(ind => ExtBlas.PointWiseAddScalar(ind, 1, TInd.Create(-start / this.BS)));
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
			var refVals = this.Storage.MakeReference(indexStart * this.BS, indexCount * this.BS);
			refVals.CopyTo<T, TS, TS>(overwrite.Storage);
			var refInds = this.IndexStorage.MakeReference(indexStart, indexCount);
			refInds.CopyTo<TInd, TSInd, TSInd>(overwrite.IndexStorage);
			if (start != 0)
				ExtBlas.PointWiseAddScalar(overwrite.IndexStorage, 1, TInd.Create(-start / this.BS));
		}

		/// <inheritdoc/>
		public override void CopyTo(SparseVector<T, TInd, TS, TSInd> destination)
		{
			if (destination is not SimpleBlockedSparseVector<T, TInd, TS, TSInd> vec || vec.blockSize != this.blockSize || vec.IndexStorage.Length != this.IndexStorage.Length || vec.Storage.Length != this.NStored)
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
			var refVals = this.Storage.MakeReference(indexStart * this.BS, indexCount * this.BS);
			value.Storage.CopyTo<T, TS, TS>(refVals);
			var refInds = this.IndexStorage.MakeReference(indexStart, indexCount);
			value.IndexStorage.CopyTo<TInd, TSInd, TSInd>(refInds);
			if (start != 0)
				ExtBlas.PointWiseAddScalar(refInds, 1, TInd.Create(start / this.BS));
		}
		#endregion

		#region conversion and clone
		/// <inheritdoc/>
		public override SparseVector<T, TInd, TS, TSInd> FromDense(DenseVector<T, TS> dense, SparseFormat format, T defaultValue, double threshold = 0)
		{
			var sparse = new SparseArrayWrapper<T, TInd, TS, TSInd>(defaultValue, format, default, default, default);
			SpConv.VectorDenseToSparse(ref sparse, dense.Storage, dense.Stride, threshold);
			if (this.TryCreateFromWrapper(in sparse, out var vec))
				return vec;
			sparse.DisposeAll();
			throw new InvalidOperationException();
		}

		/// <inheritdoc/>
		public override SimpleBlockedSparseVector<T, TInd, TS, TSInd> CreateAlike() => new(this.Length, this.Storage.CreateAlike(), this.IndexStorage.CreateAlike(), this.blockSize, this.DefaultValue);
		#endregion

		#region serialization
		private record struct SimpleBlockRepr(int Format, T Default, long Length, TS Values, TSInd Indices, TInd BlockSize);

		private SimpleBlockedSparseVector(SimpleBlockRepr repr) : this(repr.Length, repr.Values, repr.Indices, repr.BlockSize, repr.Default) { }

		/// <inheritdoc/>
		public override string JsonSerialize() => JsonSerializer.Serialize<SimpleBlockRepr>(new(this.Format.Data, this.DefaultValue, this.Length, this.Storage, this.IndexStorage, this.blockSize), JsonOptions);

		/// <inheritdoc/>
		public override SimpleBlockedSparseVector<T, TInd, TS, TSInd> JsonDeserialize(string json!!) => new(JsonSerializer.Deserialize<SimpleBlockRepr>(json, JsonOptions));
		#endregion

		#region string
		/// <inheritdoc/>
		public override unsafe string Print(PrintSettings? settings = null)
		{
			settings ??= Settings.PrintSetting;
			int length = Math.Min((int)this.NStored, settings.Value.ArrayLength);
			Span<T> values = length.CheckStackLimit<T>() ?? stackalloc T[length];
			Span<long> indices = length.CheckStackLimit<long>() ?? stackalloc long[length];
			this.Storage.ToManaged(values);
			int bs = this.blockSize.As<TInd, int>();
			fixed (long* inds = indices)
			{
				var mp = new ManagedPureStorage<long>(new ManagedPointer(new(inds), length * sizeof(long)));
				ExtBlas.PointWiseCast<TInd, long, TSInd, ManagedPureStorage<long>>(this.IndexStorage, 1, mp, bs);
			}
			for (int i = 0; i < length; i++)
			{
				int diff = i % bs;
				indices[i] = indices[i - diff] * bs + diff;
			}
			return values.ToSparseVectorString(indices, settings.Value.Precision) + (length == this.NStored ? "" : string.Format(Resources.Print.MoreStored, this.NStored - length));
		}
		#endregion
	}
}
