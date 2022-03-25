using System.Runtime.InteropServices;
using System.Text.Json;

using Althea.Helpers;
using Althea.LinearAlgebra;
using Althea.LinearAlgebra.Dense;
using Althea.NativeTypes;
using Althea.Storage;

using Blas = Althea.LinearAlgebra.Dense.BlasApiSelector;
using ExtBlas = Althea.LinearAlgebra.Dense.ExtendBlasApiSelector;
using Lapack = Althea.LinearAlgebra.Dense.LapackApiSelector;


namespace Althea.Arrays.Matrices
{
	/// <summary>
	/// The base dense matrix class whose only storage is of type <typeparamref name="TS"/>.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TS">The storage type used by the value <see cref="ISingleValueStorageArray{T, TS, TSelf}.Storage"/></typeparam>
	[StructLayout(LayoutKind.Explicit)]
	public class DenseMatrix<T, TS> : IPitchedArray<T>,
		IBaseMatrix<T, DenseMatrix<T, TS>>, ISingleValueStorageArray<T, TS, DenseMatrix<T, TS>>,
		IMatrixGetDiagonalVector<T, DenseVector<T, TS>, DenseMatrix<T, TS>>,
		IMatrixGetSetDiagonalVector<T, DenseVector<T, TS>, DenseMatrix<T, TS>>,
		IMatrixVectorMultiplyOperations<T, DenseVector<T, TS>, DenseVector<T, TS>, DenseMatrix<T, TS>>,
		IMatrixVectorMultiplyOperators<T, DenseVector<T, TS>, DenseVector<T, TS>, DenseMatrix<T, TS>>,
		IMatrixOperations<T, DenseMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>,
		IMatrixUnaryOperators<T, DenseMatrix<T, TS>, DenseMatrix<T, TS>>,
		IMatrixBinaryOperators<T, DenseMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>,
		IMatrixSolvers<T, DenseMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>, DenseVector<T, TS>>
		where T : unmanaged, INumber<T>
		where TS : class, IStorage<T, TS>
	{
		#region basic
		[FieldOffset(0)]
		private readonly long __stride1 = 1;
		[FieldOffset(sizeof(long) * 1)]
		private readonly long leadDim;
		[FieldOffset(sizeof(long) * 2)]
		private readonly long outerLength;
		[FieldOffset(sizeof(long) * 4)]
		private readonly long rows;
		[FieldOffset(sizeof(long) * 4)]
		private readonly long cols;
		[FieldOffset(sizeof(long) * 5)]
		private readonly TS values;

		/// <summary>
		/// Get the leading dimension of this dense matrix.
		/// </summary>
		public long LeadDim => this.leadDim;

		/// <inheritdoc/>
		public long NRows => this.rows;

		/// <inheritdoc/>
		public long NCols => this.cols;

		ReadOnlySpan<long> IValueArray<T, DenseMatrix<T, TS>>.Size => ReflectionHelper.CreateReadOnlySpan(in this.rows, 2);
		ReadOnlySpan<long> IPitchedArray<T>.Size => ReflectionHelper.CreateReadOnlySpan(in this.rows, 2);
		ReadOnlySpan<long> IPitchedArray<T>.Strides => ReflectionHelper.CreateReadOnlySpan(in this.__stride1, 2);
		ReadOnlySpan<long> IPitchedArray<T>.OuterSize => ReflectionHelper.CreateReadOnlySpan(in this.leadDim, 2);

		private DenseMatrix()
		{
			this.values = TS.Empty;
		}
		static DenseMatrix<T, TS> IValueArray<T, DenseMatrix<T, TS>>.Empty => new();

		long IValueArray<T, DenseMatrix<T, TS>>.Length => this.rows * this.cols;

		/// <inheritdoc/>
		public TS Storage => this.values.MakeReference();

		TS ISingleValueStorageArray<T, TS, DenseMatrix<T, TS>>.OriginalStorage => this.values;

		bool ICheckValid.IsValid() => this.values?.IsValid() ?? false;

		/// <summary>
		/// Create a new <see cref="DenseMatrix{T, TS}"/> with given <paramref name="storage"/> and <paramref name="rows"/>.
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
			((IBaseMatrix<T, DenseMatrix<T, TS>>)this).CheckRange(offsetRow, countRow, offsetCol, countCol);
			return new(this.values + (offsetRow + offsetCol * this.leadDim), countRow, countCol, this.leadDim);
		}

		/// <inheritdoc/>
		public void GetSubmatrix(long offsetRow, long countRow, long offsetCol, long countCol, DenseMatrix<T, TS> overwrite)
		{
			((IBaseMatrix<T, DenseMatrix<T, TS>>)this).CheckRange(offsetRow, countRow, offsetCol, countCol, overwrite);
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
			((IBaseMatrix<T, DenseMatrix<T, TS>>)this).CheckRange(offsetRow, countRow, offsetCol, countCol, value);
			var dst = this.values + (offsetRow + offsetCol * this.leadDim);
			var src = value.values;
			src.Copy2DTo<T, TS, TS>(value.LeadDim, dst, this.leadDim, countRow, countCol);
		}

		/// <inheritdoc/>
		public T this[long x, long y]
		{
			get
			{
				((IBaseMatrix<T, DenseMatrix<T, TS>>)this).CheckIndex(x, y);
				return (this.values + (x + y * this.leadDim)).ToManaged<T, TS>();
			}
			set
			{
				((IBaseMatrix<T, DenseMatrix<T, TS>>)this).CheckIndex(x, y);
				(this.values + (x + y * this.leadDim)).FromManaged(value);
			}
		}
		#endregion

		#region point-wise operations
		/// <inhericdoc/>
		public void FillWith(T value) => ExtBlas.GeneralMatrixFill(this.values, this.leadDim, value, this.rows, this.cols);

		/// <inhericdoc/>
		public void AddScalar(T value) => ExtBlas.GeneralMatrixAddScalar(this.values, this.leadDim, value, this.rows, this.cols);

		/// <inhericdoc/>
		public void Scale(T value) => ExtBlas.GeneralMatricesAdd(MatrixOperation.None, MatrixOperation.None, this.rows, this.cols, value, this.values, this.leadDim, T.Zero, (TS?)null, 1, this.values, this.leadDim);

		/// <inhericdoc/>
		public void Conjugate() => ExtBlas.GeneralMatricesAdd(MatrixOperation.Conjugate, MatrixOperation.None, this.rows, this.cols, T.One, this.values, this.leadDim, T.Zero, (TS?)null, 1, this.values, this.leadDim);

		/// <inhericdoc/>
		public void Power(T power) => ExtBlas.GeneralMatrixPower(this.values, this.leadDim, power, this.rows, this.cols);

		/// <inhericdoc/>
		public void Truncate(double threshold) => ExtBlas.GeneralMatrixTruncate<T, TS>(this.values, this.leadDim, threshold, this.rows, this.cols);
		#endregion

		#region simple aggregation operations
		/// <inhericdoc/>
		public T Sum() => ExtBlas.GeneralMatrixSum<T, TS>(this.values, this.leadDim, this.rows, this.cols);

		/// <inhericdoc/>
		public T AbsSum() => ExtBlas.GeneralMatrixAbsSum<T, TS>(this.values, this.leadDim, this.rows, this.cols);

		/// <inhericdoc/>
		public T Norm() => ExtBlas.GeneralMatrixNorm<T, TS>(this.values, this.leadDim, this.rows, this.cols);

		/// <inhericdoc/>
		public T ValueWithMaxAbs() => (this.values + ExtBlas.GeneralMatrixAbsArgMax<T, TS>(this.values, this.leadDim, this.rows, this.cols)).ToManaged<T, TS>();

		/// <inhericdoc/>
		public T ValueWithMinAbs() => (this.values + ExtBlas.GeneralMatrixAbsArgMin<T, TS>(this.values, this.leadDim, this.rows, this.cols)).ToManaged<T, TS>();
		#endregion

		#region operations
		/// <inhericdoc/>
		public static DenseVector<T, TS> GetDiag(DenseMatrix<T, TS> matrix, long k)
		{
			if (matrix.rows >= matrix.cols)
			{
				if (k >= 0)
					return new(matrix.Storage, matrix.cols - k, matrix.leadDim + 1);
				else
					return new(matrix.Storage, matrix.cols - k <= matrix.rows ? matrix.cols : matrix.rows + k, matrix.leadDim + 1);
			}
			else
			{
				if (k < 0)
					return new(matrix.Storage, matrix.rows + k, matrix.leadDim + 1);
				else
					return new(matrix.Storage, matrix.rows + k <= matrix.cols ? matrix.rows : matrix.cols - k, matrix.leadDim + 1);
			}
		}

		/// <inhericdoc/>
		public static void GetDiag(DenseMatrix<T, TS> matrix, long k, DenseVector<T, TS> overwrite) => GetDiag(matrix, k).CopyTo(overwrite);

		/// <inhericdoc/>
		public static void SetDiag(DenseMatrix<T, TS> matrix, long k, DenseVector<T, TS> value) => value.CopyTo(GetDiag(matrix, k));

		/// <inhericdoc/>
		public static void MatrixMultiplyVector(DenseMatrix<T, TS> matrix, DenseVector<T, TS> vector, DenseVector<T, TS> vectorOut, T α, T β = default, MatrixOperation operation = MatrixOperation.None) => Blas.GeneralMatrixMultiplyVector(operation, matrix.rows, matrix.cols, α, matrix.values, matrix.leadDim, vector.Storage, vector.Stride, β, vectorOut.Storage, vectorOut.Stride);

		/// <inhericdoc/>
		public static void VectorMultiplyMatrix(DenseVector<T, TS> vector, DenseMatrix<T, TS> matrix, DenseVector<T, TS> vectorOut, T α, T β = default, MatrixOperation operation = MatrixOperation.None) => Blas.GeneralMatrixMultiplyVector(operation.Transpose(), matrix.rows, matrix.cols, α, matrix.values, matrix.leadDim, vector.Storage, vector.Stride, β, vectorOut.Storage, vectorOut.Stride);

		/// <inhericdoc/>
		public static void AddMatrices(DenseMatrix<T, TS>? A, T scalarA, DenseMatrix<T, TS>? B, T scalarB, DenseMatrix<T, TS> C, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None) => ExtBlas.GeneralMatricesAdd(opA, opB, C.rows, C.cols, scalarA, A?.values, A?.leadDim ?? 1, scalarB, B?.values, B?.leadDim ?? 1, C.values, C.leadDim);

		/// <inhericdoc/>
		public static void MultiplyMatries(T α, DenseMatrix<T, TS> A, DenseMatrix<T, TS> B, T β, DenseMatrix<T, TS> C, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None) => Blas.GeneralMatricesMultiply(opA, opB, C.rows, C.cols, opA.CanInPlace() ? A.cols : A.rows, α, A.values, A.leadDim, B.values, B.leadDim, β, C.values, C.leadDim);

		/// <inhericdoc/>
		public static void EigenSolve(DenseMatrix<T, TS> matrix, DenseVector<T, TS> outVals, DenseMatrix<T, TS>? outLeft, DenseMatrix<T, TS>? outRight, SolveVectorMode mode, DenseMatrix<T, TS>? another = null, GeneralEigenType type = GeneralEigenType.None)
		{
			throw new NotImplementedException();
		}

		/// <inhericdoc/>
		public static void SingularValueSolve(DenseMatrix<T, TS> matrix, DenseVector<T, TS> outVals, DenseMatrix<T, TS>? outU, DenseMatrix<T, TS>? outVct, SVDStore storeU, SVDStore storeV)
		{
			throw new NotImplementedException();
		}

		/// <inhericdoc/>
		public static void SchurSolve(DenseMatrix<T, TS> matrix, DenseMatrix<T, TS> outMatrix, DenseVector<T, TS> outVals, DenseMatrix<T, TS>? outLeft, DenseMatrix<T, TS>? outRight, DenseVector<T, TS>? orderVals, SolveVectorMode mode, DenseMatrix<T, TS>? another = null)
		{
			throw new NotImplementedException();
		}

		/// <inhericdoc/>
		public static void LinearSolve(DenseMatrix<T, TS> coefficients, DenseMatrix<T, TS> rightHandSides, DenseMatrix<T, TS> outSolves, MatrixOperation opCoef = MatrixOperation.None)
		{
			if (coefficients.rows != coefficients.cols || coefficients.rows != rightHandSides.rows)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(coefficients));
			if (rightHandSides.rows != outSolves.rows || rightHandSides.cols != outSolves.cols)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(rightHandSides));
			rightHandSides.values.Copy2DTo<T, TS, TS>(rightHandSides.leadDim, outSolves.values, outSolves.leadDim, outSolves.rows, outSolves.cols);
			using var coef = coefficients.values.ResizeAlike(coefficients.rows * coefficients.cols);
			coefficients.ToCompact(coef);
			Lapack.LinearSolveGeneral<T, TS, TS>(opCoef, coefficients.rows, outSolves.cols, coef, coefficients.rows, outSolves.values, outSolves.leadDim);
		}

		/// <inhericdoc/>
		public static void LeastSquareSolve(DenseMatrix<T, TS> coefficients, DenseMatrix<T, TS> rightHandSides, DenseMatrix<T, TS> outSolves, MatrixOperation opCoef = MatrixOperation.None)
		{
			throw new NotImplementedException();
		}

		/// <inhericdoc/>
		public static void QRDecomposition(DenseMatrix<T, TS> matrix, DenseMatrix<T, TS> outTriangular, DenseMatrix<T, TS> outUnary, bool full = false)
		{
			throw new NotImplementedException();
		}
		#endregion

		#region operators
		/// <inhericdoc/>
		public static DenseVector<T, TS> operator *(DenseMatrix<T, TS> matrix, DenseVector<T, TS> vector)
		{
			throw new NotImplementedException();
		}

		/// <inhericdoc/>
		public static DenseVector<T, TS> operator *(DenseVector<T, TS> vector, DenseMatrix<T, TS> matrix)
		{
			throw new NotImplementedException();
		}

		/// <inhericdoc/>
		public static DenseMatrix<T, TS> operator -(DenseMatrix<T, TS> matrix)
		{
			throw new NotImplementedException();
		}

		/// <inhericdoc/>
		public static DenseMatrix<T, TS> operator *(DenseMatrix<T, TS> matrix, T scalar)
		{
			throw new NotImplementedException();
		}

		/// <inhericdoc/>
		public static DenseMatrix<T, TS> operator *(T scalar, DenseMatrix<T, TS> matrix)
		{
			throw new NotImplementedException();
		}

		/// <inhericdoc/>
		public static DenseMatrix<T, TS> operator /(DenseMatrix<T, TS> matrix, T scalar)
		{
			throw new NotImplementedException();
		}

		/// <inhericdoc/>
		public static DenseMatrix<T, TS> operator ^(DenseMatrix<T, TS> matrix, MatrixOperation operation)
		{
			throw new NotImplementedException();
		}

		/// <inhericdoc/>
		public static DenseMatrix<T, TS> operator +(DenseMatrix<T, TS> left, DenseMatrix<T, TS> right)
		{
			throw new NotImplementedException();
		}

		/// <inhericdoc/>
		public static DenseMatrix<T, TS> operator -(DenseMatrix<T, TS> left, DenseMatrix<T, TS> right)
		{
			throw new NotImplementedException();
		}

		/// <inhericdoc/>
		public static DenseMatrix<T, TS> operator *(DenseMatrix<T, TS> left, DenseMatrix<T, TS> right)
		{
			throw new NotImplementedException();
		}
		#endregion

		#region conversion and clone
		/// <inheritdoc/>
		public DenseMatrix<T, TS> CreateAlike() => new(this.values.CreateAlike(), this.rows, this.cols, this.leadDim);

		/// <summary>
		/// Copy the values from this dense vector to the <paramref name="other"/> one without stride.
		/// </summary>
		/// <typeparam name="TS2">The concrete storage type of <paramref name="other"/></typeparam>
		/// <param name="other">The destination dense storage to copy to</param>
		/// <exception cref="ArgumentException">If <paramref name="other"/>'s length is less than this</exception>
		public void ToCompact<TS2>(TS2 other!!) where TS2 : class, IStorage<T, TS2>
		{
			if (other.Length < this.rows * this.cols)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(other));
			this.values.Copy2DTo<T, TS, TS2>(this.leadDim, other, this.rows, this.rows, this.cols);
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
		public DenseMatrix<T, TS> JsonDeserialize(string json!!)
		{
			var repr = JsonSerializer.Deserialize<Repr>(json, JsonOptions);
			return new(repr.Values, repr.Rows, repr.Cols, repr.LeadDim);
		}
		#endregion

		#region string
		static string IMainPropertyFormattable<DenseMatrix<T, TS>>.StringMain => nameof(DenseMatrix<T, TS>);

		static IEnumerable<string> IMainPropertyFormattable<DenseMatrix<T, TS>>.PropertyNames => new[] { "DataType", "Values", "Rows", "Columns", "LeadDim" };

		IEnumerable<object?> IMainPropertyFormattable<DenseMatrix<T, TS>>.PropertyValues => new object[] { Unmanaged<T>.DataType, this.values, this.rows, this.cols, this.leadDim };

		/// <inheritdoc/>
		public override string ToString() => IMainPropertyFormattable<DenseMatrix<T, TS>>.ToString(this);

		/// <inheritdoc/>
		public string Print(PrintSettings? settings = null)
		{
			settings ??= Settings.PrintSetting;
			int rows = Math.Min((int)this.rows, settings.Value.MatrixRow);
			int cols = Math.Min((int)this.cols, settings.Value.MatrixColumn);
			int length = rows * cols;
			Span<T> values = length.CheckStackLimit<T>() ?? stackalloc T[length];
			this.values.ToManaged2D(this.leadDim, values, rows, cols);
			return values.ToMatrixString(rows, this.cols - cols, settings.Value.Precision) + (this.rows == rows ? "" : String.Format(Resources.Print.MoreRows, this.rows - rows));
		}
		#endregion
	}
}
