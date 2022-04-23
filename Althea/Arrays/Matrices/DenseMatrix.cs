using System.Runtime.InteropServices;
using System.Text.Json;

using Althea.GeneralSolver;
using Althea.Helpers;
using Althea.LinearAlgebra;
using Althea.LinearAlgebra.Dense;
using Althea.NativeTypes;
using Althea.Storage;

using Blas = Althea.LinearAlgebra.Dense.BlasApiSelector;
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

		/// <inheritdoc/>
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
		public void FillWith(T value) => ExtBlas.GeneralMatrixBinaryScalar(BinaryScalarOperation.Fill, this.NRows, this.NCols, value, this.Storage, this.LeadDim);

		/// <inheritdoc/>
		public void AddScalar(T value) => ExtBlas.GeneralMatrixBinaryScalar(BinaryScalarOperation.Add, this.NRows, this.NCols, value, this.Storage, this.LeadDim);

		/// <inheritdoc/>
		public void Scale(T value) => ExtBlas.GeneralMatrixBinaryScalar(BinaryScalarOperation.Multiply, this.NRows, this.NCols, value, this.Storage, this.LeadDim);

		/// <inheritdoc/>
		public void Conjugate() => ExtBlas.GeneralMatrixUnary<T, TS>(UnaryOperation.Conjugate, this.NRows, this.NCols, this.Storage, this.LeadDim);
		#endregion

		#region simple aggregation operations
		/// <inheritdoc/>
		public T Sum() => ExtBlas.GeneralMatrixReduce<T, TS>(ReduceOperation.Add, this.NRows, this.NCols, this.Storage, this.LeadDim);

		/// <inheritdoc/>
		public T AbsSum() => ExtBlas.GeneralMatrixReduce<T, TS>(ReduceOperation.AddAbsolute, this.NRows, this.NCols, this.Storage, this.LeadDim);

		/// <inheritdoc/>
		public T Norm() => ExtBlas.GeneralMatrixReduce<T, TS>(ReduceOperation.Norm, this.NRows, this.NCols, this.Storage, this.LeadDim);

		/// <inheritdoc/>
		public T ValueWithMaxAbs() => ExtBlas.GeneralMatrixReduce<T, TS>(ReduceOperation.AbsoluteMaximum, this.NRows, this.NCols, this.Storage, this.LeadDim);

		/// <inheritdoc/>
		public T ValueWithMinAbs() => ExtBlas.GeneralMatrixReduce<T, TS>(ReduceOperation.AbsoluteMininum, this.NRows, this.NCols, this.Storage, this.LeadDim);
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


	/// <summary>
	/// The base dense diagonal matrix class that only stores the diagonal elements whose type is <typeparamref name="TS"/>.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TS">The storage type used by the value storage</typeparam>
	[StructLayout(LayoutKind.Sequential)]
	public class DiagonalMatrix<T, TS> : IArray<T>, IBaseMatrix<T, DiagonalMatrix<T, TS>>,
		IMatrixVectorMultiplyOperators<T, DenseVector<T, TS>, DenseVector<T, TS>, DiagonalMatrix<T, TS>>,
		IMatrixUnaryOperators<T, DiagonalMatrix<T, TS>, DiagonalMatrix<T, TS>>,
		IMatrixBinaryOperators<T, DiagonalMatrix<T, TS>, DiagonalMatrix<T, TS>, DiagonalMatrix<T, TS>>,
		IMatrixBinaryOperators<T, DiagonalMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>
		where T : unmanaged, INumber<T>
		where TS : class, IStorage<T, TS>
	{
		#region basic
		private readonly long n, nn, stride;
		private readonly TS values;

		/// <summary>
		/// Get the stride between consecutive stored diagonal elements.
		/// </summary>
		public long Stride => this.stride;

		/// <inheritdoc/>
		public long NRows => this.n;

		/// <inheritdoc/>
		public long NCols => this.n;

		ReadOnlySpan<long> IArray<T>.Size => SpanHelper.CreateReadOnlySpan(in this.n, 2);

		ReadOnlySpan<long> IValueArray<T, DiagonalMatrix<T, TS>>.Size => ((IPitchedArray<T>)this).Size;

		long IValueArray<T, DiagonalMatrix<T, TS>>.Length => this.n * this.n;

		static DiagonalMatrix<T, TS> IValueArray<T, DiagonalMatrix<T, TS>>.Empty => new();

		private DiagonalMatrix()
		{
			this.values = TS.Empty;
		}

		/// <summary>
		/// Get the value storage of this matrix as a <typeparamref name="TS"/>.
		/// </summary>
		public TS Storage => this.values.MakeReference();

		bool ICheckValid.IsValid() => this.values?.IsValid() ?? false;

		/// <summary>
		/// Create a new <see cref="DiagonalMatrix{T, TS}"/> with given <paramref name="storage"/> and size.
		/// </summary>
		/// <param name="storage">The storage of type <typeparamref name="TS"/> to create from</param>
		/// <param name="n">The number of rows and columns of the matrix to create</param>
		/// <param name="stride">The size of the leading dimension (the actual number of rows), default 0 means the same as <paramref name="n"/></param>
		/// <exception cref="ArgumentException">If the length of <paramref name="storage"/> is too short</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="n"/> or <paramref name="stride"/> ≤ 0</exception>
		public DiagonalMatrix(TS storage!!, long n, long stride = 1)
		{
			if (!storage.IsValid())
				throw new ArgumentNullException(nameof(storage));
			if (n <= 0)
				throw new ArgumentOutOfRangeException(nameof(n), Resources.ParameterError.MustPositive);
			if (stride <= 0)
				throw new ArgumentOutOfRangeException(nameof(stride), Resources.ParameterError.MustPositive);
			long length = (storage.Length - 1) / stride + 1;
			this.nn = this.n = n; this.stride = stride;
			if (length < n)
				throw new ArgumentException(Resources.ArithmeticError.CannotDivide, nameof(stride));
			if (length != n)
				storage = storage.MakeReference(0, (n - 1) * stride + 1);
			this.values = storage.AddToManager();
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
		~DiagonalMatrix()
		{
			this.Dispose();
		}

		/// <summary>
		/// Copy the values from this <see cref="DiagonalMatrix{T, TS}"/> to a new <typeparamref name="TS"/> with all values stored in compact mode that <see cref="Stride"/> == 1.
		/// </summary>
		/// <returns>The created compact <see cref="DiagonalMatrix{T, TS}"/>'s storage as a <typeparamref name="TS"/></returns>
		public TS ToCompact()
		{
			TS result = this.values.ResizeAlike(this.n);
			try
			{
				this.values.StridedCopyTo<T, TS, TS>(this.stride, result, 1);
				return result;
			}
			catch (Exception)
			{
				result.Dispose();
				throw;
			}
		}
		#endregion

		#region equality
		/// <inheritdoc/>
		public bool Equals(DiagonalMatrix<T, TS>? other) => other is not null && this.n == other.n && this.stride == other.stride && this.values == other.values;

		/// <inheritdoc/>
		public static bool operator ==(DiagonalMatrix<T, TS>? left, DiagonalMatrix<T, TS>? right) => (left is null && right is null) || (left is not null && left.Equals(right));

		/// <inheritdoc/>
		public static bool operator !=(DiagonalMatrix<T, TS>? left, DiagonalMatrix<T, TS>? right) => !(left == right);

		/// <inheritdoc/>
		public override bool Equals(object? obj) => this.Equals(obj as AbstractDenseMatrix<T, TS>);

		/// <inheritdoc/>
		public override int GetHashCode() => HashCode.Combine(this.values, this.n, this.stride);
		#endregion

		#region index
		/// <inheritdoc/>
		public DiagonalMatrix<T, TS> GetSubmatrix(long offsetRow, long countRow, long offsetCol, long countCol)
		{
			if (offsetRow != offsetCol || countRow != countCol)
				throw new ArgumentException(Resources.ParameterError.NotSameSize);
			IBaseMatrix<T, DiagonalMatrix<T, TS>>.CheckRange(this, offsetRow, countRow, offsetCol, countCol);
			return new(this.values + (offsetRow * this.Stride), countRow, this.stride);
		}

		/// <inheritdoc/>
		public void GetSubmatrix(long offsetRow, long countRow, long offsetCol, long countCol, DiagonalMatrix<T, TS> overwrite)
		{
			if (offsetRow != offsetCol || countRow != countCol)
				throw new ArgumentException(Resources.ParameterError.NotSameSize);
			IBaseMatrix<T, DiagonalMatrix<T, TS>>.CheckRange(this, offsetRow, countRow, offsetCol, countCol, overwrite);
			(this.values + (offsetRow * this.stride)).StridedCopyTo<T, TS, TS>(this.stride, overwrite.values, overwrite.stride);
		}

		/// <inheritdoc/>
		public void CopyTo(DiagonalMatrix<T, TS> destination)
		{
			if (destination.n != this.n)
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(destination));
			this.values.StridedCopyTo<T, TS, TS>(this.stride, destination.values, destination.stride);
		}

		/// <inheritdoc/>
		public void SetSubmatrix(long offsetRow, long countRow, long offsetCol, long countCol, DiagonalMatrix<T, TS> value)
		{
			if (offsetRow != offsetCol || countRow != countCol)
				throw new ArgumentException(Resources.ParameterError.NotSameSize);
			IBaseMatrix<T, DiagonalMatrix<T, TS>>.CheckRange(this, offsetRow, countRow, offsetCol, countCol, value);
			var dst = this.values + (offsetRow * this.stride);
			var src = value.values;
			src.StridedCopyTo<T, TS, TS>(value.stride, dst, this.stride);
		}

		/// <inheritdoc/>
		public T this[long x, long y]
		{
			get
			{
				IBaseMatrix<T, DiagonalMatrix<T, TS>>.CheckIndex(this, x, y);
				if (x != y)
					return T.Zero;
				return (this.values + (x * this.stride)).ToManaged<T, TS>();
			}
			set
			{
				IBaseMatrix<T, DiagonalMatrix<T, TS>>.CheckIndex(this, x, y);
				if (x != y && value != T.Zero)
					throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(value));
				(this.values + (x * this.stride)).FromManaged(value);
			}
		}
		#endregion

		#region point-wise operations
		/// <inheritdoc/>
		public void FillWith(T value) => ExtBlas.GeneralVectorBinaryScalar(BinaryScalarOperation.Fill, value, this.values, this.stride);

		/// <inheritdoc/>
		public void AddScalar(T value) => ExtBlas.GeneralVectorBinaryScalar(BinaryScalarOperation.Add, value, this.values, this.stride);

		/// <inheritdoc/>
		public void Scale(T value) => Blas.Scale(this.values, this.stride, value);

		/// <inheritdoc/>
		public void Conjugate() => ExtBlas.GeneralVectorUnary<T, TS>(UnaryOperation.Conjugate, this.values, this.stride);
		#endregion

		#region simple aggregation operations
		/// <inheritdoc/>
		public T Sum() => ExtBlas.GeneralVectorReduce<T, TS>(ReduceOperation.Add, this.values, this.stride);

		/// <inheritdoc/>
		public T AbsSum() => Blas.AbsoluteValueSum<T, TS>(this.values, this.stride);

		/// <inheritdoc/>
		public T Norm() => Blas.Norm<T, TS>(this.values, this.stride);

		/// <inheritdoc/>
		public T ValueWithMaxAbs() => (this.values + Blas.AbsoluteValueArgMax<T, TS>(this.values, this.stride)).ToManaged<T, TS>();

		/// <inheritdoc/>
		public T ValueWithMinAbs() => (this.values + Blas.AbsoluteValueArgMin<T, TS>(this.values, this.stride)).ToManaged<T, TS>();
		#endregion

		#region operators
		/// <inheritdoc/>
		public static DenseVector<T, TS> operator *(DiagonalMatrix<T, TS> matrix!!, DenseVector<T, TS> vector!!)
		{
			if (vector.Length != matrix.n)
				throw new ArgumentException(Resources.ParameterError.NotSameSize);
			return vector.ApplyToClone(v => ExtBlas.GeneralVectorsBinary<T, TS, TS>(BinaryOperation.Multiply, v.Storage, v.Stride, matrix.values, matrix.stride));
		}

		/// <inheritdoc/>
		public static DenseVector<T, TS> operator *(DenseVector<T, TS> vector, DiagonalMatrix<T, TS> matrix) => matrix * vector;

		/// <inheritdoc/>
		public static DiagonalMatrix<T, TS> operator *(DiagonalMatrix<T, TS> matrix!!, T scalar) => matrix.ApplyToAlike(m => Blas.Scale(m.values, m.stride, scalar));

		/// <inheritdoc/>
		public static DiagonalMatrix<T, TS> operator -(DiagonalMatrix<T, TS> matrix) => matrix * (-T.One);

		/// <inheritdoc/>
		public static DiagonalMatrix<T, TS> operator *(T scalar, DiagonalMatrix<T, TS> matrix) => matrix * scalar;

		/// <inheritdoc/>
		public static DiagonalMatrix<T, TS> operator /(DiagonalMatrix<T, TS> matrix, T scalar) => matrix * (T.One / scalar);

		/// <inheritdoc/>
		public static DiagonalMatrix<T, TS> operator ^(DiagonalMatrix<T, TS> matrix!!, MatrixOperation operation)
		{
			if (!NumberType<T>.IsComplex || (operation & MatrixOperation.Conjugate) != 0)
				return ((ICloneable<DiagonalMatrix<T, TS>>)matrix).Clone();
			return matrix.ApplyToClone(static m => ExtBlas.GeneralVectorUnary<T, TS>(UnaryOperation.Conjugate, m.values, m.stride));
		}

		/// <inheritdoc/>
		public static DiagonalMatrix<T, TS> operator *(DiagonalMatrix<T, TS> left!!, DiagonalMatrix<T, TS> right!!)
		{
			if (left.n != right.n)
				throw new ArgumentException(Resources.ParameterError.NotSameSize);
			return left.ApplyToClone(m => ExtBlas.GeneralVectorsBinary<T, TS, TS>(BinaryOperation.Multiply, m.values, m.stride, right.values, right.stride));
		}

		/// <inheritdoc/>
		public static DiagonalMatrix<T, TS> operator +(DiagonalMatrix<T, TS> left!!, DiagonalMatrix<T, TS> right!!)
		{
			if (left.n != right.n)
				throw new ArgumentException(Resources.ParameterError.NotSameSize);
			return left.ApplyToClone(m => Blas.Add(T.One, right.values, right.stride, m.values, m.stride));
		}

		/// <inheritdoc/>
		public static DiagonalMatrix<T, TS> operator -(DiagonalMatrix<T, TS> left!!, DiagonalMatrix<T, TS> right!!)
		{
			if (left.n != right.n)
				throw new ArgumentException(Resources.ParameterError.NotSameSize);
			return left.ApplyToClone(m => Blas.Add(-T.One, right.values, right.stride, m.values, m.stride));
		}

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator +(DiagonalMatrix<T, TS> left!!, DenseMatrix<T, TS> right!!)
		{
			if (left.n != right.NRows || left.n != right.NCols)
				throw new ArgumentException(Resources.ParameterError.NotSameSize);
			return right.ApplyToClone(m =>
			{
				using var v = DenseOperation<T, TS>.GetDiag(m, 0);
				Blas.Add(T.One, left.values, left.stride, v.Storage, v.Stride);
			});
		}

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator -(DiagonalMatrix<T, TS> left!!, DenseMatrix<T, TS> right!!)
		{
			if (left.n != right.NRows || left.n != right.NCols)
				throw new ArgumentException(Resources.ParameterError.NotSameSize);
			return right.ApplyToClone(m =>
			{
				m.Scale(-T.One);
				using var v = DenseOperation<T, TS>.GetDiag(m, 0);
				Blas.Add(T.One, left.values, left.stride, v.Storage, v.Stride);
			});
		}

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator +(DenseMatrix<T, TS> left, DiagonalMatrix<T, TS> right) => right + left;

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator -(DenseMatrix<T, TS> left, DiagonalMatrix<T, TS> right)
		{
			if (right.n != left.NRows || right.n != left.NCols)
				throw new ArgumentException(Resources.ParameterError.NotSameSize);
			return left.ApplyToClone(m =>
			{
				using var v = DenseOperation<T, TS>.GetDiag(m, 0);
				Blas.Add(-T.One, right.values, right.stride, v.Storage, v.Stride);
			});
		}

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator *(DenseMatrix<T, TS> left, DiagonalMatrix<T, TS> right) => DenseOperation<T, TS>.MultiplyMatries(left, right, T.One);

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator *(DiagonalMatrix<T, TS> left, DenseMatrix<T, TS> right) => DenseOperation<T, TS>.MultiplyMatries(left, right, T.One);

		/// <inheritdoc/>
		public DiagonalMatrix<T, TS> CreateAlike() => new(this.values.ResizeAlike(this.n), this.n);

		/// <summary>
		/// Get a referenced <see cref="DenseVector{T, TS}"/> from the underlying storage of this <see cref="DiagonalMatrix{T, TS}"/>.
		/// </summary>
		public DenseVector<T, TS> AsVector() => new(this.values, this.n, this.stride);

		/// <summary>
		/// Get a referenced <see cref="DiagonalMatrix{T, TS}"/> from the underlying storage of this <see cref="DenseVector{T, TS}"/>.
		/// </summary>
		public static DiagonalMatrix<T, TS> FromVector(DenseVector<T, TS> vector!!) => new(vector.Storage, vector.Length, vector.Stride);
		#endregion

		#region serialization
		private record struct Repr(TS Values, long N, long Stride);
		private static JsonSerializerOptions JsonOptions => new()
		{
			Converters = { TS.JsonConverter },
			WriteIndented = true,
		};

		/// <inheritdoc/>
		public string JsonSerialize()
		{
			return JsonSerializer.Serialize<Repr>(new(this.values, this.n, this.stride), JsonOptions);
		}

		/// <inheritdoc/>
		public static DiagonalMatrix<T, TS> JsonDeserialize(string json!!)
		{
			var repr = JsonSerializer.Deserialize<Repr>(json, JsonOptions);
			return new(repr.Values, repr.N, repr.Stride);
		}
		#endregion

		#region string
		static string IMainPropertyFormattable<DiagonalMatrix<T, TS>>.StringMain => nameof(DiagonalMatrix<T, TS>);

		static IEnumerable<string> IMainPropertyFormattable<DiagonalMatrix<T, TS>>.PropertyNames => new[] { "DataType", "Values", "Size", "Stride" };

		IEnumerable<object?> IMainPropertyFormattable<DiagonalMatrix<T, TS>>.PropertyValues => new object[] { Unmanaged<T>.DataType, this.values, $"{this.n}x{this.n}", this.stride };

		/// <inheritdoc/>
		public override string ToString() => IMainPropertyFormattable<DiagonalMatrix<T, TS>>.ToString(this);

		/// <inheritdoc/>
		public string Print(PrintSettings? settings = null)
		{
			settings ??= Settings.PrintSetting;
			int length = Math.Min((int)this.n, settings.Value.MatrixRow);
			using var temp = length.CheckStackLimit<T>();
			Span<T> values = temp.IsEmpty ? stackalloc T[length] : temp.Data;
			this.values.ToManagedStride(this.stride, values);
			return values.ToVectorString(settings.Value.Precision) + (this.n == length ? "" : string.Format(Resources.Print.MoreRows, this.n == length));
		}
		#endregion
	}
}
