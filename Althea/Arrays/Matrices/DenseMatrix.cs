using System.Runtime.InteropServices;
using System.Text.Json;

using Althea.Arrays.Matrices;
using Althea.Helpers;
using Althea.LinearAlgebra;
using Althea.LinearAlgebra.Dense;
using Althea.NativeTypes;
using Althea.Storage;

using Blas = Althea.LinearAlgebra.Dense.BlasApiSelector;
using ExtBlas = Althea.LinearAlgebra.Dense.ExtendBlasApiSelector;
using Lapack = Althea.LinearAlgebra.Dense.LapackApiSelector;


namespace Althea.Arrays
{
	/// <summary>
	/// The base dense general matrix class whose only storage is of type <typeparamref name="TS"/>.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TS">The storage type used by the value storage</typeparam>
	[StructLayout(LayoutKind.Sequential)]
	public class DenseMatrix<T, TS> : IPitchedArray<T>,
		IBaseMatrix<T, DenseMatrix<T, TS>>,
		IMatrixVectorMultiplyOperators<T, DenseVector<T, TS>, DenseVector<T, TS>, DenseMatrix<T, TS>>,
		IMatrixUnaryOperators<T, DenseMatrix<T, TS>, DenseMatrix<T, TS>>,
		IMatrixBinaryOperators<T, DenseMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>
		where T : unmanaged, INumber<T>
		where TS : class, IStorage<T, TS>
	{
		#region basic
		private readonly long __stride1 = 1, leadDim, outerLength;
		private readonly long rows, cols;
		private readonly long __ld, __cols;
		private readonly TS values;

		/// <summary>
		/// Get the leading dimension of this dense matrix.
		/// </summary>
		public long LeadDim => this.leadDim;

		/// <inheritdoc/>
		public long NRows => this.rows;

		/// <inheritdoc/>
		public long NCols => this.cols;

		ReadOnlySpan<long> IValueArray<T, DenseMatrix<T, TS>>.Size => SpanHelper.CreateReadOnlySpan(in this.rows, 2);
		ReadOnlySpan<long> IPitchedArray<T>.Size => SpanHelper.CreateReadOnlySpan(in this.rows, 2);
		ReadOnlySpan<long> IPitchedArray<T>.Strides => SpanHelper.CreateReadOnlySpan(in this.__stride1, 3);
		ReadOnlySpan<long> IPitchedArray<T>.OuterSize => SpanHelper.CreateReadOnlySpan(in this.__ld, 2);

		private DenseMatrix()
		{
			this.values = TS.Empty;
		}
		static DenseMatrix<T, TS> IValueArray<T, DenseMatrix<T, TS>>.Empty => new();

		long IValueArray<T, DenseMatrix<T, TS>>.Length => this.rows * this.cols;

		/// <summary>
		/// Get the value storage of this matrix as a <typeparamref name="TS"/>.
		/// </summary>
		public TS Storage => this.values.MakeReference();

		bool ICheckValid.IsValid() => this.values?.IsValid() ?? false;

		/// <summary>
		/// Create a new <see cref="DenseMatrix{T, TS}"/> with given <paramref name="storage"/> and size.
		/// </summary>
		/// <param name="storage">The storage of type <typeparamref name="TS"/> to create from</param>
		/// <param name="rows">The number of rows of the matrix to create</param>
		/// <param name="cols">The number of columns of the matrix to create</param>
		/// <param name="leadDim">The size of the leading dimension (the actual number of rows), default 0 means the same as <paramref name="rows"/></param>
		/// <exception cref="ArgumentException">If the length of <paramref name="storage"/> is too short</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="rows"/> or <paramref name="cols"/> ≤ 0</exception>
		public DenseMatrix(TS storage!!, long rows, long cols, long leadDim = 0)
		{
			if (!storage.IsValid())
				throw new ArgumentNullException(nameof(storage));
			if (leadDim <= 0)
				leadDim = rows;
			if (rows <= 0)
				throw new ArgumentOutOfRangeException(nameof(rows), Resources.ParameterError.MustPositive);
			if (cols <= 0)
				throw new ArgumentOutOfRangeException(nameof(cols), Resources.ParameterError.MustPositive);
			long length = storage.Length + (leadDim - rows);
			this.rows = rows; this.leadDim = leadDim; this.cols = cols; this.outerLength = leadDim * cols;
			if (length < this.outerLength)
				throw new ArgumentException(Resources.ArithmeticError.CannotDivide, nameof(leadDim));
			if (length != this.outerLength)
				storage = storage.MakeReference(0, this.outerLength - (leadDim - rows));
			this.values = storage.AddToManager();
			this.__ld = leadDim; this.__cols = cols;
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			this.values.SafeDispose();
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Deconstructor to be invoked by GC.
		/// </summary>
		~DenseMatrix()
		{
			this.Dispose();
		}
		#endregion

		#region equality
		/// <inheritdoc/>
		public bool Equals(DenseMatrix<T, TS>? other) => other is not null && this.rows == other.rows && this.cols == other.cols && this.leadDim == other.leadDim && this.values == other.values;

		/// <summary>
		/// Equality operator
		/// </summary>
		public static bool operator ==(DenseMatrix<T, TS> left, DenseMatrix<T, TS> right) => left.Equals(right);

		/// <summary>
		/// Inequality operator
		/// </summary>
		public static bool operator !=(DenseMatrix<T, TS> left, DenseMatrix<T, TS> right) => !left.Equals(right);

		/// <inheritdoc/>
		public override bool Equals(object? obj) => this.Equals(obj as DenseMatrix<T, TS>);

		/// <inheritdoc/>
		public override int GetHashCode() => HashCode.Combine(this.values, this.rows, this.cols, this.leadDim);
		#endregion

		#region index
		/// <inheritdoc/>
		public DenseMatrix<T, TS> GetSubmatrix(long offsetRow, long countRow, long offsetCol, long countCol)
		{
			IBaseMatrix<T, DenseMatrix<T, TS>>.CheckRange(this, offsetRow, countRow, offsetCol, countCol);
			return new(this.values + (offsetRow + offsetCol * this.leadDim), countRow, countCol, this.leadDim);
		}

		/// <inheritdoc/>
		public void GetSubmatrix(long offsetRow, long countRow, long offsetCol, long countCol, DenseMatrix<T, TS> overwrite)
		{
			IBaseMatrix<T, DenseMatrix<T, TS>>.CheckRange(this, offsetRow, countRow, offsetCol, countCol, overwrite);
			(this.values + (offsetRow + offsetCol * this.leadDim)).Copy2DTo<T, TS, TS>(this.leadDim, overwrite.values, overwrite.leadDim, countRow, countCol);
		}

		/// <inheritdoc/>
		public void CopyTo(DenseMatrix<T, TS> destination)
		{
			if (destination.rows != this.rows || destination.cols != this.cols)
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(destination));
			this.values.Copy2DTo<T, TS, TS>(this.leadDim, destination.values, destination.LeadDim, this.rows, this.cols);
		}

		/// <inheritdoc/>
		public void SetSubmatrix(long offsetRow, long countRow, long offsetCol, long countCol, DenseMatrix<T, TS> value)
		{
			IBaseMatrix<T, DenseMatrix<T, TS>>.CheckRange(this, offsetRow, countRow, offsetCol, countCol, value);
			var dst = this.values + (offsetRow + offsetCol * this.leadDim);
			var src = value.values;
			src.Copy2DTo<T, TS, TS>(value.LeadDim, dst, this.leadDim, countRow, countCol);
		}

		/// <inheritdoc/>
		public T this[long x, long y]
		{
			get
			{
				IBaseMatrix<T, DenseMatrix<T, TS>>.CheckIndex(this, x, y);
				return (this.values + (x + y * this.leadDim)).ToManaged<T, TS>();
			}
			set
			{
				IBaseMatrix<T, DenseMatrix<T, TS>>.CheckIndex(this, x, y);
				(this.values + (x + y * this.leadDim)).FromManaged(value);
			}
		}
		#endregion

		#region point-wise operations
		/// <inheritdoc/>
		public void FillWith(T value) => ExtBlas.GeneralMatrixFill(this.values, this.leadDim, value, this.rows, this.cols);

		/// <inheritdoc/>
		public void AddScalar(T value) => ExtBlas.GeneralMatrixAddScalar(this.values, this.leadDim, value, this.rows, this.cols);

		/// <inheritdoc/>
		public void Scale(T value) => ExtBlas.GeneralMatricesAdd(MatrixOperation.None, MatrixOperation.None, this.rows, this.cols, value, this.values, this.leadDim, T.Zero, (TS?)null, 1, this.values, this.leadDim);

		/// <inheritdoc/>
		public void Conjugate() => ExtBlas.GeneralMatricesAdd(MatrixOperation.Conjugate, MatrixOperation.None, this.rows, this.cols, T.One, this.values, this.leadDim, T.Zero, (TS?)null, 1, this.values, this.leadDim);

		/// <inheritdoc/>
		public void Power(T power) => ExtBlas.GeneralMatrixPower(this.values, this.leadDim, power, this.rows, this.cols);

		/// <inheritdoc/>
		public void Truncate(double threshold) => ExtBlas.GeneralMatrixTruncate<T, TS>(this.values, this.leadDim, threshold, this.rows, this.cols);
		#endregion

		#region simple aggregation operations
		/// <inheritdoc/>
		public T Sum() => ExtBlas.GeneralMatrixSum<T, TS>(this.values, this.leadDim, this.rows, this.cols);

		/// <inheritdoc/>
		public T AbsSum() => ExtBlas.GeneralMatrixAbsSum<T, TS>(this.values, this.leadDim, this.rows, this.cols);

		/// <inheritdoc/>
		public T Norm() => ExtBlas.GeneralMatrixNorm<T, TS>(this.values, this.leadDim, this.rows, this.cols);

		/// <inheritdoc/>
		public T ValueWithMaxAbs() => (this.values + ExtBlas.GeneralMatrixAbsArgMax<T, TS>(this.values, this.leadDim, this.rows, this.cols)).ToManaged<T, TS>();

		/// <inheritdoc/>
		public T ValueWithMinAbs() => (this.values + ExtBlas.GeneralMatrixAbsArgMin<T, TS>(this.values, this.leadDim, this.rows, this.cols)).ToManaged<T, TS>();
		#endregion

		#region operators
		/// <inheritdoc/>
		public static DenseVector<T, TS> operator *(DenseMatrix<T, TS> matrix!!, DenseVector<T, TS> vector!!) => DenseLinearAlgebraOperation<T, TS>.MatrixMultiplyVector(matrix, vector, T.One);

		/// <inheritdoc/>
		public static DenseVector<T, TS> operator *(DenseVector<T, TS> vector!!, DenseMatrix<T, TS> matrix!!) => DenseLinearAlgebraOperation<T, TS>.MatrixMultiplyVector(matrix, vector, T.One, MatrixOperation.Transpose);

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator -(DenseMatrix<T, TS> matrix!!) => matrix.ApplyToClone(static m => m.Scale(-T.One));

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator *(DenseMatrix<T, TS> matrix!!, T scalar) => matrix.ApplyToClone(m => m.Scale(scalar));

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator *(T scalar, DenseMatrix<T, TS> matrix!!) => matrix.ApplyToClone(m => m.Scale(scalar));

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator /(DenseMatrix<T, TS> matrix!!, T scalar) => matrix.ApplyToClone(m => m.Scale(T.One / scalar));

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator ^(DenseMatrix<T, TS> matrix!!, MatrixOperation operation) => DenseLinearAlgebraOperation<T, TS>.AddMatrices(matrix, T.One, null, default, operation, default);

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator +(DenseMatrix<T, TS> left!!, DenseMatrix<T, TS> right!!) => left.ApplyToAlike(m => DenseLinearAlgebraOperation<T, TS>.AddMatrices(left, T.One, right, T.One, m));

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator -(DenseMatrix<T, TS> left!!, DenseMatrix<T, TS> right!!) => left.ApplyToAlike(m => DenseLinearAlgebraOperation<T, TS>.AddMatrices(left, T.One, right, -T.One, m));

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator *(DenseMatrix<T, TS> left!!, DenseMatrix<T, TS> right!!) => DenseLinearAlgebraOperation<T, TS>.MultiplyMatries(T.One, left, right);
		#endregion

		#region conversion and clone
		/// <inheritdoc/>
		public DenseMatrix<T, TS> CreateAlike() => new(this.values.ResizeAlike(this.rows * this.cols), this.rows, this.cols);

		/// <summary>
		/// Copy the values from this dense matrix to a new <typeparamref name="TS"/> with <see cref="LeadDim"/> == <see cref="NRows"/>.
		/// </summary>
		/// <returns>The created compact vector's storage as a <typeparamref name="TS"/></returns>
		public TS ToCompact()
		{
			var compact = this.values.ResizeAlike(this.rows * this.cols);
			try
			{
				this.values.Copy2DTo<T, TS, TS>(this.leadDim, compact, this.rows, this.rows, this.cols);
				return compact;
			}
			catch (Exception)
			{
				compact.Dispose();
				throw;
			}
		}
		#endregion

		#region serialization
		private record struct Repr(TS Values, long Rows, long Cols, long LeadDim);
		private static readonly JsonSerializerOptions JsonOptions = new()
		{
			Converters = { TS.JsonConverter },
			WriteIndented = true,
		};

		/// <inheritdoc/>
		public string JsonSerialize()
		{
			return JsonSerializer.Serialize<Repr>(new(this.values, this.rows, this.cols, this.leadDim), JsonOptions);
		}

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> JsonDeserialize(string json!!)
		{
			var repr = JsonSerializer.Deserialize<Repr>(json, JsonOptions);
			return new(repr.Values, repr.Rows, repr.Cols, repr.LeadDim);
		}
		#endregion

		#region string
		static string IMainPropertyFormattable<DenseMatrix<T, TS>>.StringMain => nameof(DenseMatrix<T, TS>);

		static IEnumerable<string> IMainPropertyFormattable<DenseMatrix<T, TS>>.PropertyNames => new[] { "DataType", "Values", "Size", "LeadDim" };

		IEnumerable<object?> IMainPropertyFormattable<DenseMatrix<T, TS>>.PropertyValues => new object[] { Unmanaged<T>.DataType, this.values, $"{this.rows}x{this.cols}", this.leadDim };

		/// <inheritdoc/>
		public override string ToString() => IMainPropertyFormattable<DenseMatrix<T, TS>>.ToString(this);

		/// <inheritdoc/>
		public string Print(PrintSettings? settings = null)
		{
			settings ??= Settings.PrintSetting;
			int rows = Math.Min((int)this.rows, settings.Value.MatrixRow);
			int cols = Math.Min((int)this.cols, settings.Value.MatrixColumn);
			int length = rows * cols;
			using var temp = length.CheckStackLimit<T>();
			Span<T> values = temp.IsEmpty ? stackalloc T[length] : temp.Data;
			this.values.ToManaged2D(this.leadDim, values, rows, cols);
			return values.ToMatrixString(rows, this.cols - cols, settings.Value.Precision) + (this.rows == rows ? "" : string.Format(Resources.Print.MoreRows, this.rows - rows));
		}
		#endregion
	}
}
