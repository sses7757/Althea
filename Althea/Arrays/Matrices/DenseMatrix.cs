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
		IMatrixGetDiagonalVector<T, DenseVector<T, TS>, DenseMatrix<T, TS>>,
		IMatrixSetDiagonalVector<T, DenseVector<T, TS>, DenseMatrix<T, TS>>,
		IMatrixGetDiagonalVectorVariant<T, DenseVector<T, TS>, DenseMatrix<T, TS>>,
		IMatrixVectorMultiplyOperations<T, DenseVector<T, TS>, DenseVector<T, TS>, DenseMatrix<T, TS>>,
		IMatrixVectorMultiplyOperators<T, DenseVector<T, TS>, DenseVector<T, TS>, DenseMatrix<T, TS>>,
		IMatrixOperations<T, DenseMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>,
		IMatrixUnaryOperators<T, DenseMatrix<T, TS>, DenseMatrix<T, TS>>,
		IMatrixBinaryOperators<T, DenseMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>,
		IMatrixSolvers<T, DenseMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>
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

		#region operations
		/// <inheritdoc/>
		public static DenseVector<T, TS> GetDiag(DenseMatrix<T, TS> matrix!!, long k)
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

		/// <inheritdoc/>
		public static void GetDiag(DenseMatrix<T, TS> matrix!!, long k, DenseVector<T, TS> overwrite!!) => GetDiag(matrix, k).CopyTo(overwrite);

		/// <inheritdoc/>
		public static void SetDiag(DenseMatrix<T, TS> matrix!!, long k, DenseVector<T, TS> value!!) => value.CopyTo(GetDiag(matrix, k));

		/// <inheritdoc/>
		public static void MatrixMultiplyVector(DenseMatrix<T, TS> matrix!!, DenseVector<T, TS> vector!!, DenseVector<T, TS> vectorOut!!, T α, T β = default, MatrixOperation operation = MatrixOperation.None)
		{
			IMatrixVectorMultiplyOperations<T, DenseVector<T, TS>, DenseVector<T, TS>, DenseMatrix<T, TS>>.CheckMatMulVec(matrix, vector, vectorOut, α, operation);
			Blas.GeneralMatrixMultiplyVector(operation, matrix.rows, matrix.cols, α, matrix.values, matrix.leadDim, vector.Storage, vector.Stride, β, vectorOut.Storage, vectorOut.Stride);
		}

		/// <inheritdoc/>
		public static void VectorMultiplyMatrix(DenseVector<T, TS> vector!!, DenseMatrix<T, TS> matrix!!, DenseVector<T, TS> vectorOut!!, T α, T β = default, MatrixOperation operation = MatrixOperation.None) => MatrixMultiplyVector(matrix, vector, vectorOut, α, β, operation.Transpose());

		/// <inheritdoc/>
		public static void AddMatrices(DenseMatrix<T, TS>? A, T scalarA, DenseMatrix<T, TS>? B, T scalarB, DenseMatrix<T, TS> C!!, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			var (m, n) = IMatrixOperations<T, DenseMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>.CheckMatAdd(A, scalarA, B, scalarB, C, opA, opB);
			  ExtBlas.GeneralMatricesAdd(opA, opB, m, n, scalarA, A?.values, A?.leadDim ?? 1, scalarB, B?.values, B?.leadDim ?? 1, C.values, C.leadDim);
		}
		
		/// <inheritdoc/>
		public static void MultiplyMatries(T α, DenseMatrix<T, TS> A!!, DenseMatrix<T, TS> B!!, T β, DenseMatrix<T, TS> C!!, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			var (m, n, k) = IMatrixOperations<T, DenseMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>.CheckMatMul(α, A, B, C, opA, opB);
			Blas.GeneralMatricesMultiply(opA, opB, m, n, k, α, A.values, A.leadDim, B.values, B.leadDim, β, C.values, C.leadDim);
		}

		/// <inheritdoc/>
		public static void LinearSolve(DenseMatrix<T, TS> coefficients!!, DenseMatrix<T, TS> rightHandSides!!, DenseMatrix<T, TS> outSolves!!, MatrixOperation opCoef = MatrixOperation.None)
		{
			IMatrixSolvers<T, DenseMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>.CheckLinear(coefficients, rightHandSides, outSolves);
			if (rightHandSides != outSolves)
				rightHandSides.values.Copy2DTo<T, TS, TS>(rightHandSides.leadDim, outSolves.values, outSolves.leadDim, outSolves.rows, outSolves.cols);
			using var coef = coefficients.ToCompact();
			Lapack.LinearSolveGeneral<T, TS, TS>(opCoef, coefficients.rows, outSolves.cols, coef, coefficients.rows, outSolves.values, outSolves.leadDim);
		}

		/// <inheritdoc/>
		public static void LeastSquareSolve(DenseMatrix<T, TS> coefficients!!, DenseMatrix<T, TS> rightHandSides!!, DenseMatrix<T, TS> outSolves!!)
		{
			IMatrixSolvers<T, DenseMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>.CheckLeast(coefficients, rightHandSides, outSolves);
			if (rightHandSides != outSolves)
				rightHandSides.values.Copy2DTo<T, TS, TS>(rightHandSides.leadDim, outSolves.values, outSolves.leadDim, outSolves.rows, outSolves.cols);
			using var coef = coefficients.ToCompact();
			Lapack.LeastSquareSolve<T, TS, TS>(coefficients.rows, coefficients.cols, outSolves.cols, coef, coefficients.rows, outSolves.values, outSolves.leadDim);
		}

		/// <inheritdoc/>
		public static void QRDecomposition(DenseMatrix<T, TS> matrix!!, DenseMatrix<T, TS> outTriangular!!, DenseMatrix<T, TS>? outUnary, bool full = false)
		{
			IMatrixSolvers<T, DenseMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>.CheckQR(matrix, outTriangular, outUnary, full);
			if (matrix.rows == matrix.cols)
			{
				matrix.CopyTo(outTriangular);
				Lapack.QRDecomposition<T, TS, TS>(true, matrix.rows, matrix.cols, outTriangular.values, outTriangular.leadDim, outUnary?.values, outUnary?.leadDim ?? 1);
			}
			else if (matrix.rows > matrix.cols)
			{
				using var temp = matrix.ToCompact();
				Lapack.QRDecomposition<T, TS, TS>(full, matrix.rows, matrix.cols, temp, matrix.rows, outUnary?.values, outUnary?.leadDim ?? 1);
				temp.Copy2DTo<T, TS, TS>(matrix.rows, outTriangular.values, outTriangular.leadDim, matrix.cols, matrix.cols);
				ExtBlas.MatrixClearUpperLowerPart<T, TS>(true, matrix.cols, outTriangular.values, outTriangular.leadDim);
			}
			else //if (matrix.rows < matrix.cols)
			{
				matrix.CopyTo(outTriangular);
				Lapack.QRDecomposition<T, TS, TS>(full, matrix.rows, matrix.cols, outTriangular.values, outTriangular.leadDim, outUnary?.values, outUnary?.leadDim ?? 1);
			}
		}
		#endregion

		#region operators
		/// <inheritdoc/>
		public static DenseVector<T, TS> operator *(DenseMatrix<T, TS> matrix!!, DenseVector<T, TS> vector!!)
		{
			if (matrix.cols != vector.Length)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(vector));
			var output = vector.Storage.ResizeAlike(matrix.rows);
			try
			{
				Blas.GeneralMatrixMultiplyVector(MatrixOperation.None, matrix.rows, matrix.cols, T.One, matrix.values, matrix.leadDim, vector.Storage, vector.Stride, T.Zero, output, 1);
				return new(output, matrix.rows);
			}
			catch (Exception)
			{
				output.Dispose();
				throw;
			}
		}

		/// <inheritdoc/>
		public static DenseVector<T, TS> operator *(DenseVector<T, TS> vector!!, DenseMatrix<T, TS> matrix!!)
		{
			if (matrix.cols != vector.Length)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(vector));
			var output = vector.Storage.ResizeAlike(matrix.rows);
			try
			{
				Blas.GeneralMatrixMultiplyVector(MatrixOperation.Transpose, matrix.rows, matrix.cols, T.One, matrix.values, matrix.leadDim, vector.Storage, vector.Stride, T.Zero, output, 1);
				return new(output, matrix.rows);
			}
			catch (Exception)
			{
				output.Dispose();
				throw;
			}
		}

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator -(DenseMatrix<T, TS> matrix!!) => matrix.ApplyToClone(static m => m.Scale(-T.One));

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator *(DenseMatrix<T, TS> matrix!!, T scalar) => matrix.ApplyToClone(m => m.Scale(scalar));

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator *(T scalar, DenseMatrix<T, TS> matrix!!) => matrix.ApplyToClone(m => m.Scale(scalar));

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator /(DenseMatrix<T, TS> matrix!!, T scalar) => matrix.ApplyToClone(m => m.Scale(T.One / scalar));

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator ^(DenseMatrix<T, TS> matrix!!, MatrixOperation operation)
		{
			if (operation == MatrixOperation.None)
				return ((ICloneable<DenseMatrix<T, TS>>)matrix).Clone();
			if (operation == MatrixOperation.Conjugate)
				return matrix.ApplyToClone(static m => ExtBlas.PointWiseConjugate<T, TS>(m.values, 1));
			var output = matrix.values.ResizeAlike(matrix.rows * matrix.cols);
			try
			{
				ExtBlas.GeneralMatricesAdd(operation, default, matrix.cols, matrix.rows, T.One, matrix.values, matrix.leadDim, T.Zero, (TS?)null, 1, output, matrix.cols);
				return new(output, matrix.rows, matrix.cols);
			}
			catch (Exception)
			{
				output.Dispose();
				throw;
			}
		}

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator +(DenseMatrix<T, TS> left!!, DenseMatrix<T, TS> right!!) => left.ApplyToAlike(m => AddMatrices(left, T.One, right, T.One, m));

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator -(DenseMatrix<T, TS> left!!, DenseMatrix<T, TS> right!!) => left.ApplyToAlike(m => AddMatrices(left, T.One, right, -T.One, m));

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator *(DenseMatrix<T, TS> left!!, DenseMatrix<T, TS> right!!)
		{
			if (left.cols != right.rows)
				throw new ArgumentException(Resources.ParameterError.NotSameSize);
			var result = left.values.ResizeAlike(left.rows * right.cols);
			try
			{
				Blas.GeneralMatricesMultiply(MatrixOperation.None, MatrixOperation.None, left.rows, right.cols, left.cols, T.One, left.values, left.leadDim, right.values, right.leadDim, T.Zero, result, left.rows);
				return new(result, left.rows, right.cols);
			}
			catch (Exception)
			{
				result.Dispose();
				throw;
			}
		}
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


	/// <summary>
	/// The base dense triangular matrix class whose only storage is of type <typeparamref name="TS"/>.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TS">The storage type used by the value storage</typeparam>
	[StructLayout(LayoutKind.Sequential)]
	public class TriangularMatrix<T, TS> : IPitchedArray<T>,
		IBaseMatrix<T, TriangularMatrix<T, TS>>,
		IMatrixGetDiagonalVector<T, DenseVector<T, TS>, TriangularMatrix<T, TS>>,
		IMatrixSetDiagonalVector<T, DenseVector<T, TS>, TriangularMatrix<T, TS>>,
		IMatrixGetDiagonalVectorVariant<T, DenseVector<T, TS>, TriangularMatrix<T, TS>>,
		IMatrixVectorMultiplyOperations<T, DenseVector<T, TS>, DenseVector<T, TS>, TriangularMatrix<T, TS>>,
		IMatrixVectorMultiplyOperators<T, DenseVector<T, TS>, DenseVector<T, TS>, TriangularMatrix<T, TS>>,
		IMatrixOperations<T, TriangularMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>,
		IMatrixOperations<T, DenseMatrix<T, TS>, TriangularMatrix<T, TS>, DenseMatrix<T, TS>>,
		IMatrixUnaryOperators<T, TriangularMatrix<T, TS>, TriangularMatrix<T, TS>>,
		IMatrixBinaryOperators<T, TriangularMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>
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
		/// <exception cref="NotSupportedException">Always</exception>
		public virtual void GetSubmatrix(long offsetRow, long countRow, long offsetCol, long countCol, TriangularMatrix<T, TS> overwrite)
		{
			throw new NotSupportedException();
		}

		/// <inheritdoc/>
		/// <exception cref="NotSupportedException">Always</exception>
		public virtual void SetSubmatrix(long offsetRow, long countRow, long offsetCol, long countCol, TriangularMatrix<T, TS> value)
		{
			throw new NotSupportedException();
		}

		/// <inheritdoc/>
		/// <exception cref="NotSupportedException">Always</exception>
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
		/// <exception cref="NotSupportedException">Always</exception>
		public virtual void FillWith(T value) => throw new NotSupportedException();

		/// <inheritdoc/>
		/// <exception cref="NotSupportedException">Always</exception>
		public virtual void AddScalar(T value) => throw new NotSupportedException();

		/// <inheritdoc/>
		/// <exception cref="NotSupportedException">Always</exception>
		public virtual void Scale(T value) => throw new NotSupportedException();

		/// <inheritdoc/>
		/// <exception cref="NotSupportedException">Always</exception>
		public virtual void Conjugate() => throw new NotSupportedException();

		/// <inheritdoc/>
		/// <exception cref="NotSupportedException">Always</exception>
		public virtual void Power(T power) => throw new NotSupportedException();

		/// <inheritdoc/>
		/// <exception cref="NotSupportedException">Always</exception>
		public virtual void Truncate(double threshold) => throw new NotSupportedException();
		#endregion

		#region simple aggregation operations
		/// <inheritdoc/>
		/// <exception cref="NotSupportedException">Always</exception>
		public virtual T Sum() => throw new NotSupportedException();

		/// <inheritdoc/>
		/// <exception cref="NotSupportedException">Always</exception>
		public virtual T AbsSum() => throw new NotSupportedException();

		/// <inheritdoc/>
		/// <exception cref="NotSupportedException">Always</exception>
		public virtual T Norm() => throw new NotSupportedException();

		/// <inheritdoc/>
		/// <exception cref="NotSupportedException">Always</exception>
		public virtual T ValueWithMaxAbs() => throw new NotSupportedException();

		/// <inheritdoc/>
		/// <exception cref="NotSupportedException">Always</exception>
		public virtual T ValueWithMinAbs() => throw new NotSupportedException();
		#endregion

		#region operations
		/// <inheritdoc/>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="k"/> refers to diagonal elements not used</exception>
		public static DenseVector<T, TS> GetDiag(TriangularMatrix<T, TS> matrix!!, long k)
		{
			if ((matrix.upper && k < 0) || (!matrix.upper && k > 0) || (matrix.unitDiag && k == 0))
				throw new ArgumentOutOfRangeException(nameof(k));
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

		/// <inheritdoc/>
		public static void GetDiag(TriangularMatrix<T, TS> matrix!!, long k, DenseVector<T, TS> overwrite!!) => GetDiag(matrix, k).CopyTo(overwrite);

		/// <inheritdoc/>
		public static void SetDiag(TriangularMatrix<T, TS> matrix!!, long k, DenseVector<T, TS> value!!) => value.CopyTo(GetDiag(matrix, k));

		/// <inheritdoc/>
		public static void MatrixMultiplyVector(TriangularMatrix<T, TS> matrix!!, DenseVector<T, TS> vector!!, DenseVector<T, TS> vectorOut!!, T α, T β = default, MatrixOperation operation = MatrixOperation.None)
		{
			if (matrix.rows != matrix.cols || β != T.Zero)
				throw new NotSupportedException();
			IMatrixVectorMultiplyOperations<T, DenseVector<T, TS>, DenseVector<T, TS>, TriangularMatrix<T, TS>>.CheckMatMulVec(matrix, vector, vectorOut, α, operation);
			vector.CopyTo(vectorOut);
			Blas.TriangularMatrixMultiplyVector<T, TS, TS>(matrix.upper, matrix.unitDiag, operation, matrix.rows, matrix.values, matrix.leadDim, vectorOut.Storage, vectorOut.Stride);
		}
		
		/// <inheritdoc/>
		public static void VectorMultiplyMatrix(DenseVector<T, TS> vector!!, TriangularMatrix<T, TS> matrix!!, DenseVector<T, TS> vectorOut!!, T α, T β = default, MatrixOperation operation = MatrixOperation.None) => MatrixMultiplyVector(matrix, vector, vectorOut, α, β, operation.Transpose());

		/// <inheritdoc/>
		public static void AddMatrices(TriangularMatrix<T, TS>? A, T scalarA, DenseMatrix<T, TS>? B, T scalarB, DenseMatrix<T, TS> C!!, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			if (A is null || scalarA == T.Zero)
			{
				DenseMatrix<T, TS>.AddMatrices(null, T.Zero, B, scalarB, C, opA, opB);
				return;
			}
			if (!opA.CanInPlace())
				throw new NotSupportedException();
			var (m, n) = IMatrixOperations<T, TriangularMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>.CheckMatAdd(A, scalarA, B, scalarB, C, opA, opB);
			A.values.Copy2DTo<T, TS, TS>(A.leadDim, C.Storage, C.LeadDim, m, n);
			ExtBlas.MatrixClearUpperLowerPart<T, TS>(A.upper, Math.Min(m, n), C.Storage, C.LeadDim);
			if (A.unitDiag)
				ExtBlas.FillWithValue(C.Storage, T.One, C.LeadDim + 1);
			DenseMatrix<T, TS>.AddMatrices(C, scalarA, B, scalarB, C, opA, opB);
		}

		/// <inheritdoc/>
		public static void MultiplyMatries(T α, TriangularMatrix<T, TS> A!!, DenseMatrix<T, TS> B!!, T β, DenseMatrix<T, TS> C!!, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			if (A.rows != A.cols || opB != MatrixOperation.None)
				throw new NotSupportedException();
			IMatrixOperations<T, TriangularMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>.CheckMatMul(α, A, B, C, opA, opB);
			Blas.TriangularMatrixMultiply(true, A.upper, A.unitDiag, opA, B.NRows, B.NCols, α, A.values, A.leadDim, B.Storage, B.LeadDim, C.Storage, C.LeadDim);
		}

		/// <inheritdoc/>
		public static void AddMatrices(DenseMatrix<T, TS>? A, T scalarA, TriangularMatrix<T, TS>? B, T scalarB, DenseMatrix<T, TS> C, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None) => AddMatrices(B, scalarB, A, scalarA, C, opA, opB);

		/// <inheritdoc/>
		public static void MultiplyMatries(T α, DenseMatrix<T, TS> A, TriangularMatrix<T, TS> B, T β, DenseMatrix<T, TS> C, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			if (B.rows != B.cols || opA != MatrixOperation.None)
				throw new NotSupportedException();
			IMatrixOperations<T, DenseMatrix<T, TS>, TriangularMatrix<T, TS>, DenseMatrix<T, TS>>.CheckMatMul(α, A, B, C, opA, opB);
			Blas.TriangularMatrixMultiply(false, B.upper, B.unitDiag, opB, A.NRows, A.NCols, α, B.values, B.leadDim, A.Storage, A.LeadDim, C.Storage, C.LeadDim);
		}

		/// <inheritdoc/>
		public static void LinearSolve(TriangularMatrix<T, TS> coefficients, DenseMatrix<T, TS> outSolves, MatrixOperation opCoef = MatrixOperation.None)
		{
			Blas.TriangularMatrixSolve(true, coefficients.upper, coefficients.unitDiag, opCoef, coefficients.rows, outSolves.NCols, T.One, coefficients.values, coefficients.leadDim, outSolves.Storage, outSolves.LeadDim);
		}
		#endregion

		#region operators
		/// <inheritdoc/>
		public static DenseVector<T, TS> operator *(TriangularMatrix<T, TS> matrix!!, DenseVector<T, TS> vector!!)
		{
			if (matrix.cols != vector.Length)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(vector));
			if (matrix.rows != matrix.cols)
				throw new NotSupportedException();
			var output = vector.Storage.ResizeAlike(matrix.rows);
			try
			{
				vector.Storage.StridedCopyTo<T, TS, TS>(vector.Stride, output, 1);
				Blas.TriangularMatrixMultiplyVector<T, TS, TS>(matrix.upper, matrix.unitDiag, MatrixOperation.None, matrix.rows, matrix.values, matrix.leadDim, output, 1);
				return new(output, matrix.rows);
			}
			catch (Exception)
			{
				output.Dispose();
				throw;
			}
		}

		/// <inheritdoc/>
		public static DenseVector<T, TS> operator *(DenseVector<T, TS> vector!!, TriangularMatrix<T, TS> matrix!!)
		{
			if (matrix.cols != vector.Length)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(vector));
			if (matrix.rows != matrix.cols)
				throw new NotSupportedException();
			var output = vector.Storage.ResizeAlike(matrix.rows);
			try
			{
				vector.Storage.StridedCopyTo<T, TS, TS>(vector.Stride, output, 1);
				Blas.TriangularMatrixMultiplyVector<T, TS, TS>(matrix.upper, matrix.unitDiag, MatrixOperation.Transpose, matrix.rows, matrix.values, matrix.leadDim, output, 1);
				return new(output, matrix.rows);
			}
			catch (Exception)
			{
				output.Dispose();
				throw;
			}
		}

		/// <inheritdoc/>
		public static TriangularMatrix<T, TS> operator -(TriangularMatrix<T, TS> matrix!!) => matrix.ApplyToClone(static m => m.Scale(-T.One));

		/// <inheritdoc/>
		public static TriangularMatrix<T, TS> operator *(TriangularMatrix<T, TS> matrix!!, T scalar) => matrix.ApplyToClone(m => m.Scale(scalar));

		/// <inheritdoc/>
		public static TriangularMatrix<T, TS> operator *(T scalar, TriangularMatrix<T, TS> matrix!!) => matrix.ApplyToClone(m => m.Scale(scalar));

		/// <inheritdoc/>
		public static TriangularMatrix<T, TS> operator /(TriangularMatrix<T, TS> matrix!!, T scalar) => matrix.ApplyToClone(m => m.Scale(T.One / scalar));

		/// <inheritdoc/>
		public static TriangularMatrix<T, TS> operator ^(TriangularMatrix<T, TS> matrix!!, MatrixOperation operation)
		{
			if (operation == MatrixOperation.None)
				return ((ICloneable<TriangularMatrix<T, TS>>)matrix).Clone();
			if (operation == MatrixOperation.Conjugate)
				return matrix.ApplyToClone(static m => ExtBlas.PointWiseConjugate<T, TS>(m.values, 1));
			var output = matrix.values.ResizeAlike(matrix.rows * matrix.cols);
			try
			{
				matrix.values.Copy2DTo<T, TS, TS>(matrix.leadDim, output, matrix.rows, matrix.rows, matrix.cols);
				ExtBlas.GeneralMatricesAdd(operation, default, matrix.cols, matrix.rows, T.One, output, matrix.rows, default, (TS?)null, 1, output, matrix.cols); // in-place transpose
				return new(!matrix.upper, output, matrix.cols, matrix.rows, 0, matrix.unitDiag);
			}
			catch (Exception)
			{
				output.Dispose();
				throw;
			}
		}

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator +(TriangularMatrix<T, TS> left, DenseMatrix<T, TS> right)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator +(DenseMatrix<T, TS> left, TriangularMatrix<T, TS> right) => right + left;

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator -(TriangularMatrix<T, TS> left, DenseMatrix<T, TS> right)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator -(DenseMatrix<T, TS> left, TriangularMatrix<T, TS> right)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator *(TriangularMatrix<T, TS> left, DenseMatrix<T, TS> right)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator *(DenseMatrix<T, TS> left, TriangularMatrix<T, TS> right)
		{
			throw new NotImplementedException();
		}

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
