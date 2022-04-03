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
using HalfBlas = Althea.LinearAlgebra.Dense.HalfMatrixBlasApiSelector;


namespace Althea.Arrays
{

	/// <summary>
	/// The base dense triangular matrix class whose only storage is of type <typeparamref name="TS"/>.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TS">The storage type used by the value storage</typeparam>
	[StructLayout(LayoutKind.Sequential)]
	public class TriangularMatrix<T, TS> : IPitchedArray<T>,
		IBaseMatrix<T, TriangularMatrix<T, TS>>,
		IMatrixVectorMultiplyOperators<T, DenseVector<T, TS>, DenseVector<T, TS>, TriangularMatrix<T, TS>>,
		IMatrixUnaryOperators<T, TriangularMatrix<T, TS>, TriangularMatrix<T, TS>>,
		IMatrixBinaryOperators<T, TriangularMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>,
		IMatrixBinaryOperators<T, TriangularMatrix<T, TS>, TriangularMatrix<T, TS>, TriangularMatrix<T, TS>>
		where T : unmanaged, INumber<T>
		where TS : class, IStorage<T, TS>
	{
		#region basic
		private readonly long __stride1 = 1, leadDim, outerLength;
		private readonly long rows, cols;
		private readonly long __ld, __cols;
		private readonly TS values;
		private readonly bool upper, unitDiag;

		/// <summary>
		/// Get the leading dimension of this dense matrix.
		/// </summary>
		public long LeadDim => this.leadDim;

		/// <inheritdoc/>
		public long NRows => this.rows;

		/// <inheritdoc/>
		public long NCols => this.cols;

		/// <summary>
		/// Get whether this triangular matrix is upper triangular or lower triangular.
		/// </summary>
		public bool Upper => this.upper;

		/// <summary>
		/// Get whether this triangular matrix's diagonal elements are all 1 and thus not used.
		/// </summary>
		public bool UnitDiagonal => this.unitDiag;

		ReadOnlySpan<long> IValueArray<T, TriangularMatrix<T, TS>>.Size => SpanHelper.CreateReadOnlySpan(in this.rows, 2);
		ReadOnlySpan<long> IPitchedArray<T>.Size => SpanHelper.CreateReadOnlySpan(in this.rows, 2);
		ReadOnlySpan<long> IPitchedArray<T>.Strides => SpanHelper.CreateReadOnlySpan(in this.__stride1, 3);
		ReadOnlySpan<long> IPitchedArray<T>.OuterSize => SpanHelper.CreateReadOnlySpan(in this.__ld, 2);

		private TriangularMatrix()
		{
			this.values = TS.Empty;
		}
		static TriangularMatrix<T, TS> IValueArray<T, TriangularMatrix<T, TS>>.Empty => new();

		long IValueArray<T, TriangularMatrix<T, TS>>.Length => this.rows * this.cols;

		/// <summary>
		/// Get the value storage of this matrix as a <typeparamref name="TS"/>.
		/// </summary>
		public TS Storage => this.values.MakeReference();

		bool ICheckValid.IsValid() => this.values?.IsValid() ?? false;

		/// <summary>
		/// Create a new <see cref="TriangularMatrix{T, TS}"/> with given <paramref name="storage"/> and size.
		/// </summary>
		/// <param name="upper">Whether the triangular matrix is upper triangular or lower triangular</param>
		/// <param name="storage">The storage of type <typeparamref name="TS"/> to create from</param>
		/// <param name="rows">The number of rows of the matrix to create</param>
		/// <param name="cols">The number of columns of the matrix to create</param>
		/// <param name="leadDim">The size of the leading dimension (the actual number of rows), default 0 means the same as <paramref name="rows"/></param>
		/// <param name="unitDiag">Whether the triangular matrix's diagonal elements are all 1 and thus not used.</param>
		/// <exception cref="ArgumentException">If the length of <paramref name="storage"/> is too short</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="rows"/> or <paramref name="cols"/> ≤ 0</exception>
		public TriangularMatrix(bool upper, TS storage!!, long rows, long cols, long leadDim = 0, bool unitDiag = false)
		{
			this.upper = upper; this.unitDiag = unitDiag;
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
		~TriangularMatrix()
		{
			this.Dispose();
		}
		#endregion

		#region equality
		/// <inheritdoc/>
		public bool Equals(TriangularMatrix<T, TS>? other) => other is not null && this.upper == other.upper && this.rows == other.rows && this.cols == other.cols && this.leadDim == other.leadDim && this.values == other.values;

		/// <summary>
		/// Equality operator
		/// </summary>
		public static bool operator ==(TriangularMatrix<T, TS> left, TriangularMatrix<T, TS> right) => left.Equals(right);

		/// <summary>
		/// Inequality operator
		/// </summary>
		public static bool operator !=(TriangularMatrix<T, TS> left, TriangularMatrix<T, TS> right) => !left.Equals(right);

		/// <inheritdoc/>
		public override bool Equals(object? obj) => this.Equals(obj as TriangularMatrix<T, TS>);

		/// <inheritdoc/>
		public override int GetHashCode() => HashCode.Combine(this.values, this.upper, this.rows, this.cols, this.leadDim);
		#endregion

		#region index
		/// <inheritdoc/>
		/// <exception cref="ArgumentException">If <paramref name="offsetRow"/> != <paramref name="offsetCol"/></exception>
		public virtual TriangularMatrix<T, TS> GetSubmatrix(long offsetRow, long countRow, long offsetCol, long countCol)
		{
			IBaseMatrix<T, TriangularMatrix<T, TS>>.CheckRange(this, offsetRow, countRow, offsetCol, countCol);
			if (offsetRow != offsetCol)
				throw new ArgumentException(Resources.ParameterError.InvalidValue);
			return new(this.upper, this.values + (offsetRow + offsetCol * this.leadDim), countRow, countCol, this.leadDim);
		}

		/// <inheritdoc/>
		/// <exception cref="NotSupportedException">Default implementation always throws <see cref="NotSupportedException"/></exception>
		public virtual void GetSubmatrix(long offsetRow, long countRow, long offsetCol, long countCol, TriangularMatrix<T, TS> overwrite)
		{
			throw new NotSupportedException();
		}

		/// <inheritdoc/>
		/// <exception cref="NotSupportedException">Default implementation always throws <see cref="NotSupportedException"/></exception>
		public virtual void SetSubmatrix(long offsetRow, long countRow, long offsetCol, long countCol, TriangularMatrix<T, TS> value)
		{
			throw new NotSupportedException();
		}

		/// <inheritdoc/>
		/// <exception cref="NotSupportedException">Default implementation always throws <see cref="NotSupportedException"/></exception>
		public virtual void CopyTo(TriangularMatrix<T, TS> destination)
		{
			throw new NotSupportedException();
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
				return (this.values + (x + y * this.leadDim)).ToManaged<T, TS>();
			}
			set
			{
				if ((this.upper && x > y) || (!this.upper && x < y) || (this.unitDiag && x == y))
					throw new ArgumentException(Resources.ParameterError.InvalidValue);
				IBaseMatrix<T, TriangularMatrix<T, TS>>.CheckIndex(this, x, y);
				(this.values + (x + y * this.leadDim)).FromManaged(value);
			}
		}
		#endregion

		#region point-wise operations
		/// <inheritdoc/>
		/// <exception cref="NotSupportedException">Default implementation always throws <see cref="NotSupportedException"/></exception>
		public virtual void FillWith(T value) => throw new NotSupportedException();

		/// <inheritdoc/>
		/// <exception cref="NotSupportedException">Default implementation always throws <see cref="NotSupportedException"/></exception>
		public virtual void AddScalar(T value) => throw new NotSupportedException();

		/// <inheritdoc/>
		/// <exception cref="NotSupportedException">Default implementation always throws <see cref="NotSupportedException"/></exception>
		public virtual void Scale(T value) => throw new NotSupportedException();

		/// <inheritdoc/>
		/// <exception cref="NotSupportedException">Default implementation always throws <see cref="NotSupportedException"/></exception>
		public virtual void Conjugate() => throw new NotSupportedException();

		/// <inheritdoc/>
		/// <exception cref="NotSupportedException">Default implementation always throws <see cref="NotSupportedException"/></exception>
		public virtual void Power(T power) => throw new NotSupportedException();

		/// <inheritdoc/>
		/// <exception cref="NotSupportedException">Default implementation always throws <see cref="NotSupportedException"/></exception>
		public virtual void Truncate(double threshold) => throw new NotSupportedException();
		#endregion

		#region simple aggregation operations
		/// <inheritdoc/>
		/// <exception cref="NotSupportedException">Default implementation always throws <see cref="NotSupportedException"/></exception>
		public virtual T Sum() => throw new NotSupportedException();

		/// <inheritdoc/>
		/// <exception cref="NotSupportedException">Default implementation always throws <see cref="NotSupportedException"/></exception>
		public virtual T AbsSum() => throw new NotSupportedException();

		/// <inheritdoc/>
		/// <exception cref="NotSupportedException">Default implementation always throws <see cref="NotSupportedException"/></exception>
		public virtual T Norm() => throw new NotSupportedException();

		/// <inheritdoc/>
		/// <exception cref="NotSupportedException">Default implementation always throws <see cref="NotSupportedException"/></exception>
		public virtual T ValueWithMaxAbs() => throw new NotSupportedException();

		/// <inheritdoc/>
		/// <exception cref="NotSupportedException">Default implementation always throws <see cref="NotSupportedException"/></exception>
		public virtual T ValueWithMinAbs() => throw new NotSupportedException();
		#endregion

		#region operators
		/// <inheritdoc/>
		public static DenseVector<T, TS> operator *(TriangularMatrix<T, TS> matrix!!, DenseVector<T, TS> vector!!) => DenseLinearAlgebraOperation<T, TS>.MatrixMultiplyVector(matrix, vector, T.One);

		/// <inheritdoc/>
		public static DenseVector<T, TS> operator *(DenseVector<T, TS> vector!!, TriangularMatrix<T, TS> matrix!!) => DenseLinearAlgebraOperation<T, TS>.MatrixMultiplyVector(matrix, vector, T.One, MatrixOperation.Transpose);

		/// <inheritdoc/>
		public static TriangularMatrix<T, TS> operator *(TriangularMatrix<T, TS> matrix!!, T scalar) => matrix.ApplyToClone(m => m.Scale(scalar));

		/// <inheritdoc/>
		public static TriangularMatrix<T, TS> operator -(TriangularMatrix<T, TS> matrix!!) => matrix * (-T.One);
		/// <inheritdoc/>
		public static TriangularMatrix<T, TS> operator *(T scalar, TriangularMatrix<T, TS> matrix!!) => matrix * scalar;

		/// <inheritdoc/>
		public static TriangularMatrix<T, TS> operator /(TriangularMatrix<T, TS> matrix!!, T scalar) => matrix * (T.One / scalar);

		/// <inheritdoc/>
		public static TriangularMatrix<T, TS> operator ^(TriangularMatrix<T, TS> matrix!!, MatrixOperation operation) => DenseLinearAlgebraOperation<T, TS>.AddMatrices(matrix, T.One, (TriangularMatrix<T, TS>?)null, default, operation);

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator +(TriangularMatrix<T, TS> left, DenseMatrix<T, TS> right) => DenseLinearAlgebraOperation<T, TS>.AddMatrices(left, T.One, right, T.One);

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator +(DenseMatrix<T, TS> left, TriangularMatrix<T, TS> right) => right + left;

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator -(TriangularMatrix<T, TS> left, DenseMatrix<T, TS> right) => DenseLinearAlgebraOperation<T, TS>.AddMatrices(left, T.One, right, -T.One);

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator -(DenseMatrix<T, TS> left, TriangularMatrix<T, TS> right) => DenseLinearAlgebraOperation<T, TS>.AddMatrices(right, -T.One, left, T.One);

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator *(TriangularMatrix<T, TS> left, DenseMatrix<T, TS> right) => DenseLinearAlgebraOperation<T, TS>.MultiplyMatries(T.One, left, right);

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator *(DenseMatrix<T, TS> left, TriangularMatrix<T, TS> right) => DenseLinearAlgebraOperation<T, TS>.MultiplyMatries(T.One, left, right);

		/// <inheritdoc/>
		public static TriangularMatrix<T, TS> operator +(TriangularMatrix<T, TS> left, TriangularMatrix<T, TS> right) => DenseLinearAlgebraOperation<T, TS>.AddMatrices(left, T.One, right, T.One);

		/// <inheritdoc/>
		public static TriangularMatrix<T, TS> operator -(TriangularMatrix<T, TS> left, TriangularMatrix<T, TS> right) => DenseLinearAlgebraOperation<T, TS>.AddMatrices(left, T.One, right, -T.One);

		/// <inheritdoc/>
		public static TriangularMatrix<T, TS> operator *(TriangularMatrix<T, TS> left, TriangularMatrix<T, TS> right) => DenseLinearAlgebraOperation<T, TS>.MultiplyMatries(T.One, left, right);
		#endregion

		#region conversion and clone
		/// <inheritdoc/>
		public TriangularMatrix<T, TS> CreateAlike() => new(this.upper, this.values.ResizeAlike(this.rows * this.cols), this.rows, this.cols, 0, this.unitDiag);

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
				ExtBlas.MatrixClearUpperLowerPart<T, TS>(this.upper, Math.Min(this.rows, this.cols), compact, this.rows);
				if (this.unitDiag)
					ExtBlas.FillWithValue(compact, T.One, this.rows + 1);
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
		private record struct Repr(TS Values, long Rows, long Cols, long LeadDim, bool Upper, bool UniDiag);
		private static readonly JsonSerializerOptions JsonOptions = new()
		{
			Converters = { TS.JsonConverter },
			WriteIndented = true,
		};

		/// <inheritdoc/>
		public string JsonSerialize()
		{
			return JsonSerializer.Serialize<Repr>(new(this.values, this.rows, this.cols, this.leadDim, this.upper, this.unitDiag), JsonOptions);
		}

		/// <inheritdoc/>
		public static TriangularMatrix<T, TS> JsonDeserialize(string json!!)
		{
			var repr = JsonSerializer.Deserialize<Repr>(json, JsonOptions);
			return new(repr.Upper, repr.Values, repr.Rows, repr.Cols, repr.LeadDim, repr.UniDiag);
		}
		#endregion

		#region string
		static string IMainPropertyFormattable<TriangularMatrix<T, TS>>.StringMain => nameof(TriangularMatrix<T, TS>);

		static IEnumerable<string> IMainPropertyFormattable<TriangularMatrix<T, TS>>.PropertyNames => new[] { "DataType", "Values", "Size", "LeadDim", "Upper", "UnitDiagonal" };

		IEnumerable<object?> IMainPropertyFormattable<TriangularMatrix<T, TS>>.PropertyValues => new object[] { Unmanaged<T>.DataType, this.values, $"{this.rows}x{this.cols}", this.leadDim, this.upper, this.unitDiag };

		/// <inheritdoc/>
		public override string ToString() => IMainPropertyFormattable<TriangularMatrix<T, TS>>.ToString(this);

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
			return values.ToMatrixString(rows, this.cols - cols, settings.Value.Precision) + (this.rows == rows ? "" : string.Format(Resources.Print.MoreRows, this.rows - rows));
		}
		#endregion
	}
}
