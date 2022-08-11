using System.Text.Json;
using System.Text.Json.Serialization;

using Althea.Helpers;
using Althea.LinearAlgebra;
using Althea.LinearAlgebra.Dense;
using Althea.Storage;

using ExtBlas = Althea.LinearAlgebra.Dense.ExtendBlasApiSelector;
using HalfBlas = Althea.LinearAlgebra.Dense.HalfMatrixBlasApiSelector;


namespace Althea.Array
{
	/// <summary>
	/// The base dense triangular matrix class whose only storage is of type <typeparamref name="TS"/>.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TS">The storage type used by the value storage</typeparam>
	public class TriangularMatrix<T, TS> : AbstractDenseMatrix<T, TS>,
		IBaseMatrix<T, TriangularMatrix<T, TS>>,
		IMatrixVectorMultiplyOperators<T, DenseVector<T, TS>, DenseVector<T, TS>, TriangularMatrix<T, TS>>,
		IMatrixUnaryOperators<T, TriangularMatrix<T, TS>, TriangularMatrix<T, TS>>,
		IMatrixBinaryOperators<T, TriangularMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>,
		IMatrixBinaryOperators<T, TriangularMatrix<T, TS>, TriangularMatrix<T, TS>, TriangularMatrix<T, TS>>
		where T : unmanaged, IBaseNumber<T>
		where TS : class, IStorage<T, TS>
	{
		#region basic
		private readonly bool upper, unitDiag;

		/// <summary>
		/// Get whether this triangular matrix is upper triangular or lower triangular.
		/// </summary>
		public bool Upper => this.upper;

		/// <summary>
		/// Get whether this triangular matrix's diagonal elements are all 1 and thus not used.
		/// </summary>
		public bool UnitDiagonal => this.unitDiag;

		ReadOnlySpan<long> IValueArray<T, TriangularMatrix<T, TS>>.Size => ((IPitchedArray<T>)this).Size;

		long IValueArray<T, TriangularMatrix<T, TS>>.Length => this.NRows * this.NCols;

		private TriangularMatrix() : base() { }
		static TriangularMatrix<T, TS> IValueArray<T, TriangularMatrix<T, TS>>.Empty => new();

		/// <summary>
		/// Create a new <see cref="TriangularMatrix{T, TS}"/> with given <paramref name="storage"/> and size.
		/// </summary>
		/// <param name="upper">Whether the triangular matrix is upper triangular or lower triangular</param>
		/// <param name="storage">The storage of type <typeparamref name="TS"/> to create from</param>
		/// <param name="rows">The number of rows of the matrix to create</param>
		/// <param name="cols">The number of columns of the matrix to create</param>
		/// <param name="leadDim">The size of the leading dimension (the actual number of rows), default 0 means the same as <paramref name="rows"/></param>
		/// <param name="unitDiagonal">Whether the triangular matrix's diagonal elements are all 1 and thus not used.</param>
		/// <exception cref="ArgumentException">If the length of <paramref name="storage"/> is too short</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="rows"/> or <paramref name="cols"/> ≤ 0</exception>
		[JsonConstructor]
		public TriangularMatrix(bool upper, TS storage, long rows, long cols, long leadDim = 0, bool unitDiagonal = false) : base(storage, rows, cols, leadDim)
		{
			this.upper = upper; this.unitDiag = unitDiagonal;
		}
		#endregion

		#region equality
		/// <inheritdoc/>
		public bool Equals(TriangularMatrix<T, TS>? other) => other is not null && this.upper == other.upper && this.unitDiag == other.unitDiag && this.NRows == other.NRows && this.NCols == other.NCols && this.LeadDim == other.LeadDim && this.Storage == other.Storage;

		/// <inheritdoc/>
		public static bool operator ==(TriangularMatrix<T, TS>? left, TriangularMatrix<T, TS>? right) => (left is null && right is null) || (left is not null && left.Equals(right));

		/// <inheritdoc/>
		public static bool operator !=(TriangularMatrix<T, TS>? left, TriangularMatrix<T, TS>? right) => !(left == right);

		/// <inheritdoc/>
		public override bool Equals(object? obj) => this.Equals(obj as TriangularMatrix<T, TS>);

		/// <inheritdoc/>
		public override int GetHashCode() => HashCode.Combine(this.upper, this.unitDiag, this.Storage, this.NRows, this.NCols, this.LeadDim);
		#endregion

		#region index
		/// <inheritdoc/>
		/// <exception cref="ArgumentException">If <paramref name="offsetRow"/> != <paramref name="offsetCol"/></exception>
		public TriangularMatrix<T, TS> GetSubmatrix(long offsetRow, long countRow, long offsetCol, long countCol)
		{
			IBaseMatrix<T, TriangularMatrix<T, TS>>.CheckRange(this, offsetRow, countRow, offsetCol, countCol);
			if (offsetRow != offsetCol)
				throw new ArgumentException(Resources.ParameterError.InvalidValue);
			return new(this.upper, this.Storage + (offsetRow + offsetCol * this.LeadDim), countRow, countCol, this.LeadDim);
		}

		/// <inheritdoc/>
		/// <exception cref="ArgumentException">If <paramref name="offsetRow"/> != <paramref name="offsetCol"/> or the <see cref="Upper"/>s or <see cref="UnitDiagonal"/>s are different</exception>
		public void GetSubmatrix(long offsetRow, long countRow, long offsetCol, long countCol, TriangularMatrix<T, TS> overwrite)
		{
			IBaseMatrix<T, TriangularMatrix<T, TS>>.CheckRange(this, offsetRow, countRow, offsetCol, countCol, overwrite);
			if (offsetRow != offsetCol)
				throw new ArgumentException(Resources.ParameterError.InvalidValue);
			if (overwrite.unitDiag != this.unitDiag || overwrite.upper != this.upper)
				throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(overwrite));
			HalfBlas.HalfMatrixCopy<T, TS, TS>(this.upper, !this.unitDiag, MatrixOperation.None, overwrite.NRows, overwrite.NCols, overwrite.Storage, overwrite.LeadDim, this.Storage + (offsetRow + offsetCol * this.LeadDim), this.LeadDim);
		}

		/// <inheritdoc/>
		/// <exception cref="ArgumentException">If <paramref name="offsetRow"/> != <paramref name="offsetCol"/> or the <see cref="Upper"/>s or <see cref="UnitDiagonal"/>s are different</exception>
		public void SetSubmatrix(long offsetRow, long countRow, long offsetCol, long countCol, TriangularMatrix<T, TS> value)
		{
			IBaseMatrix<T, TriangularMatrix<T, TS>>.CheckRange(this, offsetRow, countRow, offsetCol, countCol, value);
			if (offsetRow != offsetCol)
				throw new ArgumentException(Resources.ParameterError.InvalidValue);
			if (value.unitDiag != this.unitDiag || value.upper != this.upper)
				throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(value));
			HalfBlas.HalfMatrixCopy<T, TS, TS>(this.upper, !this.unitDiag, MatrixOperation.None, value.NRows, value.NCols, this.Storage + (offsetRow + offsetCol * this.LeadDim), this.LeadDim, value.Storage, value.LeadDim);
		}

		/// <inheritdoc/>
		/// <exception cref="ArgumentException">If the <see cref="Upper"/>s or <see cref="UnitDiagonal"/>s are different</exception>
		public void CopyTo(TriangularMatrix<T, TS> destination)
		{
			if (destination.NRows != this.NRows || destination.NCols != this.NCols)
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(destination));
			if (destination.unitDiag != this.unitDiag || destination.upper != this.upper)
				throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(destination));
			HalfBlas.HalfMatrixCopy<T, TS, TS>(this.upper, !this.unitDiag, MatrixOperation.None, this.NRows, this.NCols, this.Storage, this.LeadDim, destination.Storage, destination.LeadDim);
		}

		/// <inheritdoc/>
		/// <exception cref="ArgumentException">If <paramref name="x"/> and <paramref name="y"/> refer to element not used</exception>
		public T this[long x, long y]
		{
			get
			{
				if ((this.upper && x > y) || (!this.upper && x < y) || (this.unitDiag && x == y))
					throw new ArgumentException(Resources.ParameterError.InvalidValue);
				IBaseMatrix<T, TriangularMatrix<T, TS>>.CheckIndex(this, x, y);
				return (this.Storage + (x + y * this.LeadDim)).ToManaged<T, TS>();
			}
			set
			{
				if ((this.upper && x > y) || (!this.upper && x < y) || (this.unitDiag && x == y))
					throw new ArgumentException(Resources.ParameterError.InvalidValue);
				IBaseMatrix<T, TriangularMatrix<T, TS>>.CheckIndex(this, x, y);
				(this.Storage + (x + y * this.LeadDim)).FromManaged(value);
			}
		}
		#endregion

		#region point-wise operations
		/// <inheritdoc/>
		/// <exception cref="InvalidOperationException">If <paramref name="value"/> != 0</exception>
		public void FillWith(T value)
		{
			if (value == T.Zero)
				ExtBlas.GeneralMatrixBinaryScalar(BinaryScalarOperation.Fill, this.NRows, this.NCols, T.Zero, this.Storage, this.LeadDim, this.Storage, this.LeadDim);
			else
				throw new InvalidOperationException();
		}

		/// <inheritdoc/>
		/// <exception cref="InvalidOperationException">If <paramref name="value"/> != 0</exception>
		public void AddScalar(T value)
		{
			if (value != T.Zero)
				throw new InvalidOperationException();
		}

		/// <inheritdoc/>
		public void Scale(T value) => HalfBlas.HalfMatrixBinaryScalar(BinaryScalarOperation.Multiply, this.upper, this.unitDiag, this.NRows, this.NCols, value, this.Storage, this.LeadDim, this.Storage, this.LeadDim);

		/// <inheritdoc/>
		public void Conjugate() => HalfBlas.HalfMatrixUnary<T, TS, TS>(UnaryOperation.Conjugate, this.upper, this.unitDiag, this.NRows, this.NCols, this.Storage, this.LeadDim, this.Storage, this.LeadDim);
		#endregion

		#region simple aggregation operations
		/// <inheritdoc/>
		public T Sum() => HalfBlas.HalfMatrixReduce<T, TS>(ReduceOperation.Add, this.upper, true, this.unitDiag, this.NRows, this.NCols, this.Storage, this.LeadDim);

		/// <inheritdoc/>
		public T AbsSum() => HalfBlas.HalfMatrixReduce<T, TS>(ReduceOperation.AddAbsolute, this.upper, true, this.unitDiag, this.NRows, this.NCols, this.Storage, this.LeadDim);

		/// <inheritdoc/>
		public T Norm() => HalfBlas.HalfMatrixReduce<T, TS>(ReduceOperation.Norm, this.upper, true, this.unitDiag, this.NRows, this.NCols, this.Storage, this.LeadDim);

		/// <inheritdoc/>
		public T ValueWithMaxAbs() => HalfBlas.HalfMatrixReduce<T, TS>(ReduceOperation.AbsoluteMaximum, this.upper, true, this.unitDiag, this.NRows, this.NCols, this.Storage, this.LeadDim);

		/// <inheritdoc/>
		public T ValueWithMinAbs() => T.Zero;
		#endregion

		#region operators
		/// <inheritdoc/>
		public static DenseVector<T, TS> operator *(TriangularMatrix<T, TS> matrix, DenseVector<T, TS> vector) => DenseOperation<T, TS>.MatrixMultiplyVector(matrix, vector, T.One);

		/// <inheritdoc/>
		public static DenseVector<T, TS> operator *(DenseVector<T, TS> vector, TriangularMatrix<T, TS> matrix) => DenseOperation<T, TS>.MatrixMultiplyVector(matrix, vector, T.One, MatrixOperation.Transpose);

		/// <inheritdoc/>
		public static TriangularMatrix<T, TS> operator *(TriangularMatrix<T, TS> matrix, T scalar) => matrix.ApplyToClone(m => m.Scale(scalar));

		/// <inheritdoc/>
		public static TriangularMatrix<T, TS> operator -(TriangularMatrix<T, TS> matrix) => matrix * (-T.One);
		/// <inheritdoc/>
		public static TriangularMatrix<T, TS> operator *(T scalar, TriangularMatrix<T, TS> matrix) => matrix * scalar;

		/// <inheritdoc/>
		public static TriangularMatrix<T, TS> operator /(TriangularMatrix<T, TS> matrix, T scalar) => matrix * (T.One / scalar);

		/// <inheritdoc/>
		public static TriangularMatrix<T, TS> operator ^(TriangularMatrix<T, TS> matrix, MatrixOperation operation) => DenseOperation<T, TS>.AddMatrices(matrix, T.One, (TriangularMatrix<T, TS>?)null, default, operation);

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator +(TriangularMatrix<T, TS> left, DenseMatrix<T, TS> right) => DenseOperation<T, TS>.AddMatrices(left, T.One, right, T.One);

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator +(DenseMatrix<T, TS> left, TriangularMatrix<T, TS> right) => right + left;

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator -(TriangularMatrix<T, TS> left, DenseMatrix<T, TS> right) => DenseOperation<T, TS>.AddMatrices(left, T.One, right, -T.One);

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator -(DenseMatrix<T, TS> left, TriangularMatrix<T, TS> right) => DenseOperation<T, TS>.AddMatrices(right, -T.One, left, T.One);

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator *(TriangularMatrix<T, TS> left, DenseMatrix<T, TS> right) => DenseOperation<T, TS>.MultiplyMatries(left, right, T.One);

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator *(DenseMatrix<T, TS> left, TriangularMatrix<T, TS> right) => DenseOperation<T, TS>.MultiplyMatries(left, right, T.One);

		/// <inheritdoc/>
		public static TriangularMatrix<T, TS> operator +(TriangularMatrix<T, TS> left, TriangularMatrix<T, TS> right) => DenseOperation<T, TS>.AddMatrices(left, T.One, right, T.One);

		/// <inheritdoc/>
		public static TriangularMatrix<T, TS> operator -(TriangularMatrix<T, TS> left, TriangularMatrix<T, TS> right) => DenseOperation<T, TS>.AddMatrices(left, T.One, right, -T.One);

		/// <inheritdoc/>
		public static TriangularMatrix<T, TS> operator *(TriangularMatrix<T, TS> left, TriangularMatrix<T, TS> right) => DenseOperation<T, TS>.MultiplyMatries(left, right, T.One);
		#endregion

		#region conversion and clone
		/// <inheritdoc/>
		public TriangularMatrix<T, TS> CreateAlike() => new(this.upper, this.Storage.ResizeAlike(this.NRows * this.NCols), this.NRows, this.NCols, 0, this.unitDiag);

		/// <inheritdoc/>
		public override TS ToCompact()
		{
			var compact = this.Storage.ResizeAlike(this.NRows * this.NCols);
			try
			{
				this.Storage.Copy2DTo<T, TS, TS>(this.LeadDim, compact, this.NRows, this.NRows, this.NCols);
				HalfBlas.HalfMatrixClearPart<T, TS>(false, this.upper, this.NRows, this.NCols, compact, this.NRows);
				if (this.unitDiag)
					ExtBlas.GeneralVectorBinaryScalar(BinaryScalarOperation.Fill, T.One, compact, this.NRows + 1, compact, this.NRows + 1);
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
		static JsonSerializerOptions IValueArray<T, TriangularMatrix<T, TS>>.JsonSerializeOptions => IDenseArray<T, TS>.JsonSerializeOptions;
		#endregion

		#region string
		static string IMainPropertyFormattable<TriangularMatrix<T, TS>>.StringMain => nameof(TriangularMatrix<T, TS>);

		static IEnumerable<string> IMainPropertyFormattable<TriangularMatrix<T, TS>>.PropertyNames => new[] { "DataType", "Values", "Size", "LeadDim", "Upper", "UnitDiagonal" };

		IEnumerable<object?> IMainPropertyFormattable<TriangularMatrix<T, TS>>.PropertyValues => new object[] { T.Type, this.Storage, $"{this.NRows}x{this.NCols}", this.LeadDim, this.upper, this.unitDiag };

		/// <inheritdoc/>
		public override string ToString() => IMainPropertyFormattable<TriangularMatrix<T, TS>>.ToString(this);

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
			for (int y = 0; y < cols; y++)
			{
				for (int x = 0; x < rows; x++)
				{
					if ((this.upper && x > y) || (!this.upper && x < y))
						values[x + y * rows] = T.Zero;
					if (this.unitDiag && x == y)
						values[x + y * rows] = T.One;
				}
			}
			return values.ToMatrixString(rows, false, this.NCols - cols, settings.Value.Precision) + (this.NRows == rows ? "" : string.Format(Resources.Print.MoreRows, this.NRows - rows));
		}
		#endregion
	}


	/// <summary>
	/// The base dense symmetric matrix class whose only storage is of type <typeparamref name="TS"/>.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TS">The storage type used by the value storage</typeparam>
	public class SymmetricMatrix<T, TS> : AbstractDenseMatrix<T, TS>,
		IBaseMatrix<T, SymmetricMatrix<T, TS>>,
		IMatrixVectorMultiplyOperators<T, DenseVector<T, TS>, DenseVector<T, TS>, SymmetricMatrix<T, TS>>,
		IMatrixUnaryOperators<T, SymmetricMatrix<T, TS>, SymmetricMatrix<T, TS>>,
		IMatrixBinaryOperators<T, SymmetricMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>,
		IMatrixAddOperators<T, SymmetricMatrix<T, TS>, SymmetricMatrix<T, TS>, SymmetricMatrix<T, TS>>,
		IMatrixMultiplyOperator<T, SymmetricMatrix<T, TS>, SymmetricMatrix<T, TS>, DenseMatrix<T, TS>>
		where T : unmanaged, IBaseNumber<T>
		where TS : class, IStorage<T, TS>
	{
		#region basic
		private readonly bool upper, herm;

		/// <summary>
		/// Get whether this symmetric matrix is upper symmetric or lower symmetric.
		/// </summary>
		public bool Upper => this.upper;

		/// <summary>
		/// Get whether this symmetric matrix is Hermitian or simply symmetric.
		/// </summary>
		public bool Hermitian => this.herm;

		ReadOnlySpan<long> IValueArray<T, SymmetricMatrix<T, TS>>.Size => ((IPitchedArray<T>)this).Size;

		long IValueArray<T, SymmetricMatrix<T, TS>>.Length => this.NRows * this.NCols;

		private SymmetricMatrix() : base() { }
		static SymmetricMatrix<T, TS> IValueArray<T, SymmetricMatrix<T, TS>>.Empty => new();

		/// <summary>
		/// Create a new <see cref="SymmetricMatrix{T, TS}"/> with given <paramref name="storage"/> and size.
		/// </summary>
		/// <param name="upper">Whether the symmetric matrix is upper symmetric or lower symmetric</param>
		/// <param name="storage">The storage of type <typeparamref name="TS"/> to create from</param>
		/// <param name="n">The number of rows and columns of the matrix to create</param>
		/// <param name="leadDim">The size of the leading dimension (the actual number of rows), default 0 means the same as <paramref name="n"/></param>
		/// <param name="hermitian">Whether the symmetric matrix is Hermitian or simply symmetric or according to <typeparamref name="T"/> (null)</param>
		/// <exception cref="ArgumentException">If the length of <paramref name="storage"/> is too short</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="n"/> ≤ 0</exception>
		[JsonConstructor]
		public SymmetricMatrix(bool upper, TS storage, long n, long leadDim = 0, bool? hermitian = null) : base(storage, n, n, leadDim)
		{
			this.upper = upper;
			this.herm = hermitian ?? true;
			if (!T.IsComplexType)
				this.herm = false;
		}
		#endregion

		#region equality
		/// <inheritdoc/>
		public bool Equals(SymmetricMatrix<T, TS>? other) => other is not null && this.upper == other.upper && this.herm == other.herm && this.NRows == other.NRows && this.NCols == other.NCols && this.LeadDim == other.LeadDim && this.Storage == other.Storage;

		/// <inheritdoc/>
		public static bool operator ==(SymmetricMatrix<T, TS>? left, SymmetricMatrix<T, TS>? right) => (left is null && right is null) || (left is not null && left.Equals(right));

		/// <inheritdoc/>
		public static bool operator !=(SymmetricMatrix<T, TS>? left, SymmetricMatrix<T, TS>? right) => !(left == right);

		/// <inheritdoc/>
		public override bool Equals(object? obj) => this.Equals(obj as SymmetricMatrix<T, TS>);

		/// <inheritdoc/>
		public override int GetHashCode() => HashCode.Combine(this.upper, this.herm, this.Storage, this.NRows, this.NCols, this.LeadDim);
		#endregion

		#region index
		/// <inheritdoc/>
		/// <exception cref="ArgumentException">If <paramref name="offsetRow"/> != <paramref name="offsetCol"/> or <paramref name="countRow"/> != <paramref name="countCol"/></exception>
		public SymmetricMatrix<T, TS> GetSubmatrix(long offsetRow, long countRow, long offsetCol, long countCol)
		{
			IBaseMatrix<T, SymmetricMatrix<T, TS>>.CheckRange(this, offsetRow, countRow, offsetCol, countCol);
			if (offsetRow != offsetCol || countRow != countCol)
				throw new ArgumentException(Resources.ParameterError.InvalidValue);
			return new(this.upper, this.Storage + (offsetRow + offsetCol * this.LeadDim), countRow, this.LeadDim);
		}

		/// <inheritdoc/>
		/// <exception cref="ArgumentException">If <paramref name="offsetRow"/> != <paramref name="offsetCol"/> or <paramref name="countRow"/> != <paramref name="countCol"/> or the <see cref="Upper"/>s or <see cref="Hermitian"/>s are different</exception>
		public void GetSubmatrix(long offsetRow, long countRow, long offsetCol, long countCol, SymmetricMatrix<T, TS> overwrite)
		{
			IBaseMatrix<T, SymmetricMatrix<T, TS>>.CheckRange(this, offsetRow, countRow, offsetCol, countCol, overwrite);
			if (offsetRow != offsetCol || countRow != countCol)
				throw new ArgumentException(Resources.ParameterError.InvalidValue);
			if (overwrite.herm != this.herm || overwrite.upper != this.upper)
				throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(overwrite));
			HalfBlas.HalfMatrixCopy<T, TS, TS>(this.upper, true, MatrixOperation.None, overwrite.NRows, overwrite.NCols, overwrite.Storage, overwrite.LeadDim, this.Storage + (offsetRow + offsetCol * this.LeadDim), this.LeadDim);
		}

		/// <inheritdoc/>
		/// <exception cref="ArgumentException">If <paramref name="offsetRow"/> != <paramref name="offsetCol"/> or the <see cref="Upper"/>s or <see cref="Hermitian"/>s are different</exception>
		public void SetSubmatrix(long offsetRow, long countRow, long offsetCol, long countCol, SymmetricMatrix<T, TS> value)
		{
			IBaseMatrix<T, SymmetricMatrix<T, TS>>.CheckRange(this, offsetRow, countRow, offsetCol, countCol, value);
			if (offsetRow != offsetCol)
				throw new ArgumentException(Resources.ParameterError.InvalidValue);
			if (value.herm != this.herm || value.upper != this.upper)
				throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(value));
			HalfBlas.HalfMatrixCopy<T, TS, TS>(this.upper, true, MatrixOperation.None, value.NRows, value.NCols, this.Storage + (offsetRow + offsetCol * this.LeadDim), this.LeadDim, value.Storage, value.LeadDim);
		}

		/// <inheritdoc/>
		/// <exception cref="ArgumentException">If the <see cref="Upper"/>s or <see cref="Hermitian"/>s are different</exception>
		public void CopyTo(SymmetricMatrix<T, TS> destination)
		{
			if (destination.NRows != this.NRows || destination.NCols != this.NCols)
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(destination));
			if (destination.herm != this.herm || destination.upper != this.upper)
				throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(destination));
			HalfBlas.HalfMatrixCopy<T, TS, TS>(this.upper, true, MatrixOperation.None, this.NRows, this.NCols, this.Storage, this.LeadDim, destination.Storage, destination.LeadDim);
		}

		/// <inheritdoc/>
		/// <exception cref="ArgumentException">If <paramref name="x"/> and <paramref name="y"/> refer to element not used</exception>
		public T this[long x, long y]
		{
			get
			{
				if ((this.upper && x > y) || (!this.upper && x < y))
					return this.herm ? this[y, x] : T.Conjugate(this[y, x]);
				IBaseMatrix<T, SymmetricMatrix<T, TS>>.CheckIndex(this, x, y);
				return (this.Storage + (x + y * this.LeadDim)).ToManaged<T, TS>();
			}
			set
			{
#pragma warning disable CA2011
				if ((this.upper && x > y) || (!this.upper && x < y))
					this[y, x] = this.herm ? value : T.Conjugate(value);
#pragma warning restore CA2011
				IBaseMatrix<T, SymmetricMatrix<T, TS>>.CheckIndex(this, x, y);
				(this.Storage + (x + y * this.LeadDim)).FromManaged(value);
			}
		}
		#endregion

		#region point-wise operations
		/// <inheritdoc/>
		/// <exception cref="InvalidOperationException">If this is a Hermitian matrix and <paramref name="value"/> is not a real number</exception>
		public void FillWith(T value)
		{
			if (!this.herm || !T.IsComplex(value))
				HalfBlas.HalfMatrixBinaryScalar(BinaryScalarOperation.Fill, this.upper, false, this.NRows, this.NCols, value, this.Storage, this.LeadDim, this.Storage, this.LeadDim);
			else
				throw new InvalidOperationException();
		}

		/// <inheritdoc/>
		public void AddScalar(T value)
		{
			if (!this.herm || !T.IsComplex(value))
				HalfBlas.HalfMatrixBinaryScalar(BinaryScalarOperation.Fill, this.upper, false, this.NRows, this.NCols, value, this.Storage, this.LeadDim, this.Storage, this.LeadDim);
			throw new InvalidOperationException();
		}

		/// <inheritdoc/>
		public void Scale(T value)
		{
			if (!this.herm || !T.IsComplex(value))
				HalfBlas.HalfMatrixBinaryScalar(BinaryScalarOperation.Fill, this.upper, false, this.NRows, this.NCols, value, this.Storage, this.LeadDim, this.Storage, this.LeadDim);
			else
				throw new InvalidOperationException();
		}

		/// <inheritdoc/>
		public void Conjugate() => HalfBlas.HalfMatrixUnary<T, TS, TS>(UnaryOperation.Conjugate, this.upper, false, this.NRows, this.NCols, this.Storage, this.LeadDim, this.Storage, this.LeadDim);
		#endregion

		#region simple aggregation operations
		/// <inheritdoc/>
		public T Sum() => HalfBlas.HalfMatrixReduce<T, TS>(ReduceOperation.Add, this.upper, false, this.herm, this.NRows, this.NCols, this.Storage, this.LeadDim);

		/// <inheritdoc/>
		public T AbsSum() => HalfBlas.HalfMatrixReduce<T, TS>(ReduceOperation.AddAbsolute, this.upper, false, this.herm, this.NRows, this.NCols, this.Storage, this.LeadDim);

		/// <inheritdoc/>
		public T Norm() => HalfBlas.HalfMatrixReduce<T, TS>(ReduceOperation.Norm, this.upper, false, this.herm, this.NRows, this.NCols, this.Storage, this.LeadDim);

		/// <inheritdoc/>
		public T ValueWithMaxAbs() => HalfBlas.HalfMatrixReduce<T, TS>(ReduceOperation.AbsoluteMaximum, this.upper, false, this.herm, this.NRows, this.NCols, this.Storage, this.LeadDim);

		/// <inheritdoc/>
		public T ValueWithMinAbs() => HalfBlas.HalfMatrixReduce<T, TS>(ReduceOperation.AbsoluteMininum, this.upper, false, this.herm, this.NRows, this.NCols, this.Storage, this.LeadDim);
		#endregion

		#region operators
		/// <inheritdoc/>
		public static DenseVector<T, TS> operator *(SymmetricMatrix<T, TS> matrix, DenseVector<T, TS> vector) => DenseOperation<T, TS>.MatrixMultiplyVector(matrix, vector, T.One);

		/// <inheritdoc/>
		public static DenseVector<T, TS> operator *(DenseVector<T, TS> vector, SymmetricMatrix<T, TS> matrix) => DenseOperation<T, TS>.MatrixMultiplyVector(matrix, vector, T.One, MatrixOperation.Transpose);

		/// <inheritdoc/>
		public static SymmetricMatrix<T, TS> operator *(SymmetricMatrix<T, TS> matrix, T scalar) => matrix.ApplyToClone(m => m.Scale(scalar));

		/// <inheritdoc/>
		public static SymmetricMatrix<T, TS> operator -(SymmetricMatrix<T, TS> matrix) => matrix * (-T.One);
		/// <inheritdoc/>
		public static SymmetricMatrix<T, TS> operator *(T scalar, SymmetricMatrix<T, TS> matrix) => matrix * scalar;

		/// <inheritdoc/>
		public static SymmetricMatrix<T, TS> operator /(SymmetricMatrix<T, TS> matrix, T scalar) => matrix * (T.One / scalar);

		/// <inheritdoc/>
		public static SymmetricMatrix<T, TS> operator ^(SymmetricMatrix<T, TS> matrix, MatrixOperation operation) => DenseOperation<T, TS>.AddMatrices(matrix, T.One, (SymmetricMatrix<T, TS>?)null, default, operation);

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator +(SymmetricMatrix<T, TS> left, DenseMatrix<T, TS> right) => DenseOperation<T, TS>.AddMatrices(left, T.One, right, T.One);

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator +(DenseMatrix<T, TS> left, SymmetricMatrix<T, TS> right) => right + left;

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator -(SymmetricMatrix<T, TS> left, DenseMatrix<T, TS> right) => DenseOperation<T, TS>.AddMatrices(left, T.One, right, -T.One);

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator -(DenseMatrix<T, TS> left, SymmetricMatrix<T, TS> right) => DenseOperation<T, TS>.AddMatrices(right, -T.One, left, T.One);

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator *(SymmetricMatrix<T, TS> left, DenseMatrix<T, TS> right) => DenseOperation<T, TS>.MultiplyMatries(left, right, T.One);

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator *(DenseMatrix<T, TS> left, SymmetricMatrix<T, TS> right) => DenseOperation<T, TS>.MultiplyMatries(left, right, T.One);

		/// <inheritdoc/>
		public static SymmetricMatrix<T, TS> operator +(SymmetricMatrix<T, TS> left, SymmetricMatrix<T, TS> right) => DenseOperation<T, TS>.AddMatrices(left, T.One, right, T.One);

		/// <inheritdoc/>
		public static SymmetricMatrix<T, TS> operator -(SymmetricMatrix<T, TS> left, SymmetricMatrix<T, TS> right) => DenseOperation<T, TS>.AddMatrices(left, T.One, right, -T.One);

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator *(SymmetricMatrix<T, TS> left, SymmetricMatrix<T, TS> right) => DenseOperation<T, TS>.MultiplyMatries(left, right, T.One);
		#endregion

		#region conversion and clone
		/// <inheritdoc/>
		public SymmetricMatrix<T, TS> CreateAlike() => new(this.upper, this.Storage.ResizeAlike(this.NRows * this.NCols), this.NRows, 0, this.herm);

		/// <inheritdoc/>
		public override TS ToCompact()
		{
			var compact = this.Storage.ResizeAlike(this.NRows * this.NCols);
			try
			{
				this.Storage.Copy2DTo<T, TS, TS>(this.LeadDim, compact, this.NRows, this.NRows, this.NCols);
				HalfBlas.SymmetricMatrixToNormal<T, TS>(this.upper, this.herm, this.NRows, compact, this.NRows);
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
		static JsonSerializerOptions IValueArray<T, SymmetricMatrix<T, TS>>.JsonSerializeOptions => IDenseArray<T, TS>.JsonSerializeOptions;
		#endregion

		#region string
		static string IMainPropertyFormattable<SymmetricMatrix<T, TS>>.StringMain => nameof(SymmetricMatrix<T, TS>);

		static IEnumerable<string> IMainPropertyFormattable<SymmetricMatrix<T, TS>>.PropertyNames => new[] { "DataType", "Values", "Size", "LeadDim", "Upper", "Hermitian" };

		IEnumerable<object?> IMainPropertyFormattable<SymmetricMatrix<T, TS>>.PropertyValues => new object[] { T.Type, this.Storage, $"{this.NRows}x{this.NCols}", this.LeadDim, this.upper, this.herm };

		/// <inheritdoc/>
		public override string ToString() => IMainPropertyFormattable<SymmetricMatrix<T, TS>>.ToString(this);

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
			for (int y = 0; y < cols; y++)
			{
				for (int x = 0; x < rows; x++)
				{
					if ((this.upper && x > y) || (!this.upper && x < y))
						values[x + y * rows] = this.herm ? values[y + x * rows] : T.Conjugate(values[y + x * rows]);
				}
			}
			return values.ToMatrixString(rows, false, this.NCols - cols, settings.Value.Precision) + (this.NRows == rows ? "" : string.Format(Resources.Print.MoreRows, this.NRows - rows));
		}
		#endregion
	}
}
