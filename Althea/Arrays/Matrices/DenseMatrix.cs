using System.Runtime.InteropServices;
using System.Text.Json;

using Althea.GeneralSolver;
using Althea.Helpers;
using Althea.LinearAlgebra;
using Althea.LinearAlgebra.Dense;
using Althea.NativeTypes;
using Althea.Storage;

using ExtBlas = Althea.LinearAlgebra.Dense.ExtendBlasApiSelector;


namespace Althea.Array
{
	/// <summary>
	/// The abstract dense matrix class whose only storage is of type <typeparamref name="TS"/>.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TS">The storage type used by the value storage</typeparam>
	[StructLayout(LayoutKind.Sequential)]
	public abstract class AbstractDenseMatrix<T, TS> : ICheckValid, IDisposable, IMatrixMetric, IDenseArray<T, TS>
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
		public long LeadDim => this.LeadDim;

		/// <inheritdoc/>
		public long NRows => this.rows;

		/// <inheritdoc/>
		public long NCols => this.cols;

		ReadOnlySpan<long> IArray<T>.Size => SpanHelper.CreateReadOnlySpan(in this.rows, 2);
		ReadOnlySpan<long> IPitchedArray<T>.Strides => SpanHelper.CreateReadOnlySpan(in this.__stride1, 3);
		ReadOnlySpan<long> IPitchedArray<T>.OuterSize => SpanHelper.CreateReadOnlySpan(in this.__ld, 2);

		/// <summary>
		/// Create an empty <see cref="AbstractDenseMatrix{T, TS}"/>.
		/// </summary>
		protected AbstractDenseMatrix()
		{
			this.values = TS.Empty;
		}

		/// <summary>
		/// Get the value storage of this matrix as a <typeparamref name="TS"/>.
		/// </summary>
		public TS Storage => this.Storage.MakeReference();

		bool ICheckValid.IsValid() => this.Storage?.IsValid() ?? false;

		/// <summary>
		/// Create a new <see cref="AbstractDenseMatrix{T, TS}"/> with given <paramref name="storage"/> and size.
		/// </summary>
		/// <param name="storage">The storage of type <typeparamref name="TS"/> to create from</param>
		/// <param name="rows">The number of rows of the matrix to create</param>
		/// <param name="cols">The number of columns of the matrix to create</param>
		/// <param name="leadDim">The size of the leading dimension (the actual number of rows), default 0 means the same as <paramref name="rows"/></param>
		/// <exception cref="ArgumentException">If the length of <paramref name="storage"/> is too short</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="rows"/> or <paramref name="cols"/> ≤ 0</exception>
		protected AbstractDenseMatrix(TS storage!!, long rows, long cols, long leadDim = 0)
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
		~AbstractDenseMatrix()
		{
			this.Dispose();
		}

		/// <summary>
		/// When implemented by a derived class, copy the values from this dense matrix to a new <typeparamref name="TS"/> with all values stored in compact mode that <see cref="AbstractDenseMatrix{T, TS}.LeadDim"/> == <see cref="AbstractDenseMatrix{T, TS}.NRows"/>.
		/// </summary>
		/// <returns>The created compact matrix's storage as a <typeparamref name="TS"/></returns>
		public abstract TS ToCompact();
		#endregion
	}

	/// <summary>
	/// The base dense general matrix class whose only storage is of type <typeparamref name="TS"/>.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TS">The storage type used by the value storage</typeparam>
	public class DenseMatrix<T, TS> : AbstractDenseMatrix<T, TS>,
		IBaseMatrix<T, DenseMatrix<T, TS>>,
		IMatrixVectorMultiplyOperators<T, DenseVector<T, TS>, DenseVector<T, TS>, DenseMatrix<T, TS>>,
		IMatrixUnaryOperators<T, DenseMatrix<T, TS>, DenseMatrix<T, TS>>,
		IMatrixBinaryOperators<T, DenseMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>,
		IConvertibleMatrix<T, DenseMatrix<T, TS>, DenseVector<T, TS>>
		where T : unmanaged, INumber<T>
		where TS : class, IStorage<T, TS>
	{
		#region basic
		ReadOnlySpan<long> IValueArray<T, DenseMatrix<T, TS>>.Size => ((IPitchedArray<T>)this).Size;
		long IValueArray<T, DenseMatrix<T, TS>>.Length => this.NRows * this.NCols;

		private DenseMatrix() : base() { }
		static DenseMatrix<T, TS> IValueArray<T, DenseMatrix<T, TS>>.Empty => new();

		/// <summary>
		/// Create a new <see cref="DenseMatrix{T, TS}"/> with given <paramref name="storage"/> and size.
		/// </summary>
		/// <param name="storage">The storage of type <typeparamref name="TS"/> to create from</param>
		/// <param name="rows">The number of rows of the matrix to create</param>
		/// <param name="cols">The number of columns of the matrix to create</param>
		/// <param name="leadDim">The size of the leading dimension (the actual number of rows), default 0 means the same as <paramref name="rows"/></param>
		/// <exception cref="ArgumentException">If the length of <paramref name="storage"/> is too short</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="rows"/> or <paramref name="cols"/> ≤ 0</exception>
		public DenseMatrix(TS storage!!, long rows, long cols, long leadDim = 0) : base(storage, rows, cols, leadDim)
		{
			// do nothing
		}
		#endregion

		#region equality
		/// <inheritdoc/>
		public bool Equals(DenseMatrix<T, TS>? other) => other is not null && this.NRows == other.NRows && this.NCols == other.NCols && this.LeadDim == other.LeadDim && this.Storage == other.Storage;

		/// <inheritdoc/>
		public static bool operator ==(DenseMatrix<T, TS>? left, DenseMatrix<T, TS>? right) => (left is null && right is null) || (left is not null && left.Equals(right));

		/// <inheritdoc/>
		public static bool operator !=(DenseMatrix<T, TS>? left, DenseMatrix<T, TS>? right) => !(left == right);

		/// <inheritdoc/>
		public override bool Equals(object? obj) => this.Equals(obj as AbstractDenseMatrix<T, TS>);

		/// <inheritdoc/>
		public override int GetHashCode() => HashCode.Combine(this.Storage, this.NRows, this.NCols, this.LeadDim);
		#endregion

		#region index
		/// <inheritdoc/>
		public DenseMatrix<T, TS> GetSubmatrix(long offsetRow, long countRow, long offsetCol, long countCol)
		{
			IBaseMatrix<T, DenseMatrix<T, TS>>.CheckRange(this, offsetRow, countRow, offsetCol, countCol);
			return new(this.Storage + (offsetRow + offsetCol * this.LeadDim), countRow, countCol, this.LeadDim);
		}

		/// <inheritdoc/>
		public void GetSubmatrix(long offsetRow, long countRow, long offsetCol, long countCol, DenseMatrix<T, TS> overwrite)
		{
			IBaseMatrix<T, DenseMatrix<T, TS>>.CheckRange(this, offsetRow, countRow, offsetCol, countCol, overwrite);
			(this.Storage + (offsetRow + offsetCol * this.LeadDim)).Copy2DTo<T, TS, TS>(this.LeadDim, overwrite.Storage, overwrite.LeadDim, countRow, countCol);
		}

		/// <inheritdoc/>
		public void CopyTo(DenseMatrix<T, TS> destination)
		{
			if (destination.NRows != this.NRows || destination.NCols != this.NCols)
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(destination));
			this.Storage.Copy2DTo<T, TS, TS>(this.LeadDim, destination.Storage, destination.LeadDim, this.NRows, this.NCols);
		}

		/// <inheritdoc/>
		public void SetSubmatrix(long offsetRow, long countRow, long offsetCol, long countCol, DenseMatrix<T, TS> value)
		{
			IBaseMatrix<T, DenseMatrix<T, TS>>.CheckRange(this, offsetRow, countRow, offsetCol, countCol, value);
			var dst = this.Storage + (offsetRow + offsetCol * this.LeadDim);
			var src = value.Storage;
			src.Copy2DTo<T, TS, TS>(value.LeadDim, dst, this.LeadDim, countRow, countCol);
		}

		/// <inheritdoc/>
		public T this[long x, long y]
		{
			get
			{
				IBaseMatrix<T, DenseMatrix<T, TS>>.CheckIndex(this, x, y);
				return (this.Storage + (x + y * this.LeadDim)).ToManaged<T, TS>();
			}
			set
			{
				IBaseMatrix<T, DenseMatrix<T, TS>>.CheckIndex(this, x, y);
				(this.Storage + (x + y * this.LeadDim)).FromManaged(value);
			}
		}
		#endregion

		#region point-wise operations
		/// <inheritdoc/>
		public void FillWith(T value) => ExtBlas.GeneralMatrixFill(this.Storage, this.LeadDim, value, this.NRows, this.NCols);

		/// <inheritdoc/>
		public void AddScalar(T value) => ExtBlas.GeneralMatrixAddScalar(this.Storage, this.LeadDim, value, this.NRows, this.NCols);

		/// <inheritdoc/>
		public void Scale(T value) => ExtBlas.GeneralMatricesAdd(MatrixOperation.None, MatrixOperation.None, this.NRows, this.NCols, value, this.Storage, this.LeadDim, T.Zero, (TS?)null, 1, this.Storage, this.LeadDim);

		/// <inheritdoc/>
		public void Conjugate() => ExtBlas.GeneralMatricesAdd(MatrixOperation.Conjugate, MatrixOperation.None, this.NRows, this.NCols, T.One, this.Storage, this.LeadDim, T.Zero, (TS?)null, 1, this.Storage, this.LeadDim);

		/// <inheritdoc/>
		public void Power(T power) => ExtBlas.GeneralMatrixPower(this.Storage, this.LeadDim, power, this.NRows, this.NCols);

		/// <inheritdoc/>
		public void Truncate(double threshold) => ExtBlas.GeneralMatrixTruncate<T, TS>(this.Storage, this.LeadDim, threshold, this.NRows, this.NCols);
		#endregion

		#region simple aggregation operations
		/// <inheritdoc/>
		public T Sum() => ExtBlas.GeneralMatrixSum<T, TS>(this.Storage, this.LeadDim, this.NRows, this.NCols);

		/// <inheritdoc/>
		public T AbsSum() => ExtBlas.GeneralMatrixAbsSum<T, TS>(this.Storage, this.LeadDim, this.NRows, this.NCols);

		/// <inheritdoc/>
		public T Norm() => ExtBlas.GeneralMatrixNorm<T, TS>(this.Storage, this.LeadDim, this.NRows, this.NCols);

		/// <inheritdoc/>
		public T ValueWithMaxAbs() => (this.Storage + ExtBlas.GeneralMatrixAbsArgMax<T, TS>(this.Storage, this.LeadDim, this.NRows, this.NCols)).ToManaged<T, TS>();

		/// <inheritdoc/>
		public T ValueWithMinAbs() => (this.Storage + ExtBlas.GeneralMatrixAbsArgMin<T, TS>(this.Storage, this.LeadDim, this.NRows, this.NCols)).ToManaged<T, TS>();
		#endregion

		#region operators
		/// <inheritdoc/>
		public static DenseVector<T, TS> operator *(DenseMatrix<T, TS> matrix, DenseVector<T, TS> vector) => DenseOperation<T, TS>.MatrixMultiplyVector(matrix, vector, T.One);

		/// <inheritdoc/>
		public static DenseVector<T, TS> operator *(DenseVector<T, TS> vector, DenseMatrix<T, TS> matrix) => DenseOperation<T, TS>.MatrixMultiplyVector(matrix, vector, T.One, MatrixOperation.Transpose);

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator *(DenseMatrix<T, TS> matrix, T scalar) => DenseOperation<T, TS>.AddMatrices(matrix, scalar, (DenseMatrix<T, TS>?)null, default);

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator -(DenseMatrix<T, TS> matrix) => matrix * (-T.One);

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator *(T scalar, DenseMatrix<T, TS> matrix) => matrix * scalar;

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator /(DenseMatrix<T, TS> matrix, T scalar) => matrix * (T.One / scalar);

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator ^(DenseMatrix<T, TS> matrix, MatrixOperation operation) => DenseOperation<T, TS>.AddMatrices(matrix, T.One, (DenseMatrix<T, TS>?)null, default, operation);

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator +(DenseMatrix<T, TS> left, DenseMatrix<T, TS> right) => DenseOperation<T, TS>.AddMatrices(left, T.One, right, T.One);

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator -(DenseMatrix<T, TS> left, DenseMatrix<T, TS> right) => DenseOperation<T, TS>.AddMatrices(left, T.One, right, -T.One);

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator *(DenseMatrix<T, TS> left, DenseMatrix<T, TS> right) => DenseOperation<T, TS>.MultiplyMatries(left, right, T.One);
		#endregion

		#region conversion and clone
		/// <inheritdoc/>
		public DenseMatrix<T, TS> CreateAlike() => new(this.Storage.ResizeAlike(this.NRows * this.NCols), this.NRows, this.NCols);

		/// <inheritdoc/>
		public override TS ToCompact()
		{
			var compact = this.Storage.ResizeAlike(this.NRows * this.NCols);
			try
			{
				this.Storage.Copy2DTo<T, TS, TS>(this.LeadDim, compact, this.NRows, this.NRows, this.NCols);
				return compact;
			}
			catch (Exception)
			{
				compact.Dispose();
				throw;
			}
		}
		#endregion

		#region Krylov
		DenseVector<T, TS> IConvertibleMatrix<T, DenseMatrix<T, TS>, DenseVector<T, TS>>.ToVector()
		{
			var output = this.ToCompact();
			return new(output, this.NRows * this.NCols);
		}

		static void IMatrixAddOperations<T, DenseMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>.AddMatrices(DenseMatrix<T, TS>? A, T scalarA, DenseMatrix<T, TS>? B, T scalarB, DenseMatrix<T, TS> C, MatrixOperation opA, MatrixOperation opB) => DenseOperation<T, TS>.AddMatrices(A, scalarA, B, scalarB, C, opA, opB);

		static DenseMatrix<T, TS> IMatrixAddOperations<T, DenseMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>.AddMatrices(DenseMatrix<T, TS>? A, T scalarA, DenseMatrix<T, TS>? B, T scalarB, MatrixOperation opA, MatrixOperation opB) => DenseOperation<T, TS>.AddMatrices(A, scalarA, B, scalarB, opA, opB);

		static void IMatrixMultiplyOperations<T, DenseMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>.MultiplyMatries(DenseMatrix<T, TS> A, DenseMatrix<T, TS> B, T α, T β, DenseMatrix<T, TS> C, MatrixOperation opA, MatrixOperation opB) => DenseOperation<T, TS>.MultiplyMatries(A, B, α, β, C, opA, opB);

		static DenseMatrix<T, TS> IMatrixMultiplyOperations<T, DenseMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>.MultiplyMatries(DenseMatrix<T, TS> A, DenseMatrix<T, TS> B, T α, MatrixOperation opA, MatrixOperation opB) => DenseOperation<T, TS>.MultiplyMatries(A, B, α, opA, opB);
		#endregion

		#region serialization
		private record struct Repr(TS Values, long Rows, long Cols, long LeadDim);
		private static JsonSerializerOptions JsonOptions => new()
		{
			Converters = { TS.JsonConverter },
			WriteIndented = true,
		};

		/// <inheritdoc/>
		public string JsonSerialize()
		{
			return JsonSerializer.Serialize<Repr>(new(this.Storage, this.NRows, this.NCols, this.LeadDim), JsonOptions);
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

		IEnumerable<object?> IMainPropertyFormattable<DenseMatrix<T, TS>>.PropertyValues => new object[] { Unmanaged<T>.DataType, this.Storage, $"{this.NRows}x{this.NCols}", this.LeadDim };

		/// <inheritdoc/>
		public override string ToString() => IMainPropertyFormattable<DenseMatrix<T, TS>>.ToString(this);

		/// <inheritdoc/>
		public string Print(PrintSettings? settings = null)
		{
			settings ??= Settings.PrintSetting;
			int rows = Math.Min((int)this.NRows, settings.Value.MatrixRow);
			int cols = Math.Min((int)this.NCols, settings.Value.MatrixColumn);
			int length = rows * cols;
			using var temp = length.CheckStackLimit<T>();
			Span<T> values = temp.IsEmpty ? stackalloc T[length] : temp.Data;
			this.Storage.ToManaged2D(this.LeadDim, values, rows, cols);
			return values.ToMatrixString(rows, false, this.NCols - cols, settings.Value.Precision) + (this.NRows == rows ? "" : string.Format(Resources.Print.MoreRows, this.NRows - rows));
		}
		#endregion
	}
}
