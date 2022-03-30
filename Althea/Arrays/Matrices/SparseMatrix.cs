using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;

using Althea.Linq;
using Althea.Helpers;
using Althea.LinearAlgebra;
using Althea.LinearAlgebra.Sparse;
using Althea.Storage;
using Althea.NativeTypes;

using Blas = Althea.LinearAlgebra.Dense.BlasApiSelector;
using ExtBlas = Althea.LinearAlgebra.Dense.ExtendBlasApiSelector;
using SpComp = Althea.LinearAlgebra.Sparse.ComputationApiSelector;
using SpConv = Althea.LinearAlgebra.Sparse.ConversionApiSelector;


namespace Althea.Arrays
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
		IMatrixVectorMultiplyOperations<T, DenseVector<T, TS>, DenseVector<T, TS>, SparseMatrix<T, TInd, TS, TSInd>>,
		IMatrixVectorMultiplyOperators<T, DenseVector<T, TS>, DenseVector<T, TS>, SparseMatrix<T, TInd, TS, TSInd>>,
		IMatrixGetDiagonalVector<T, SparseVector<T, TInd, TS, TSInd>, SparseMatrix<T, TInd, TS, TSInd>>,
		IMatrixGetDiagonalVectorVariant<T, SparseVector<T, TInd, TS, TSInd>, SparseMatrix<T, TInd, TS, TSInd>>,
		IMatrixSetDiagonalVector<T, SparseVector<T, TInd, TS, TSInd>, SparseMatrix<T, TInd, TS, TSInd>>,
		IMatrixOperations<T, DenseMatrix<T, TS>, SparseMatrix<T, TInd, TS, TSInd>, DenseMatrix<T, TS>>,
		IMatrixOperations<T, SparseMatrix<T, TInd, TS, TSInd>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>,
		IMatrixOperations<T, SparseMatrix<T, TInd, TS, TSInd>, SparseMatrix<T, TInd, TS, TSInd>, SparseMatrix<T, TInd, TS, TSInd>>,
		IMatrixUnaryOperators<T, SparseMatrix<T, TInd, TS, TSInd>, SparseMatrix<T, TInd, TS, TSInd>>,
		IMatrixBinaryOperators<T, SparseMatrix<T, TInd, TS, TSInd>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>,
		IMatrixBinaryOperators<T, SparseMatrix<T, TInd, TS, TSInd>, SparseMatrix<T, TInd, TS, TSInd>, SparseMatrix<T, TInd, TS, TSInd>>
		where T : unmanaged, INumber<T> where TInd : unmanaged, IBinaryInteger<TInd>
		where TS : class, IStorage<T, TS> where TSInd : class, IStorage<TInd, TSInd>
	{
		#region basic
		private readonly long rows, cols;

		private readonly TSInd rowIndices, colIndices;

		private readonly TS values;

		private readonly T defaultValue;

		ReadOnlySpan<long> IValueArray<T, SparseMatrix<T, TInd, TS, TSInd>>.Size => ReflectionHelper.CreateReadOnlySpan(in this.rows, 2);

		ReadOnlySpan<long> ISparseArray<T>.Size => ReflectionHelper.CreateReadOnlySpan(in this.rows, 2);
		ReadOnlySpan<TS> ISparseArray<T, TInd, TS, TSInd>.ValueStorages => ReflectionHelper.CreateReadOnlySpan(in this.values, 1);
		ReadOnlySpan<TSInd> ISparseArray<T, TInd, TS, TSInd>.IndexStorages => ReflectionHelper.CreateReadOnlySpan(in this.rowIndices, 2);
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
		public TS Storage => this.values.MakeReference();

		/// <summary>
		/// Get the row index array's storage of this sparse matrix.
		/// </summary>
		public TSInd RowIndexStorage => this.rowIndices.MakeReference();
		/// <summary>
		/// Get the column index array's storage of this sparse matrix.
		/// </summary>
		public TSInd ColIndexStorage => this.colIndices.MakeReference();

		/// <inheritdoc/>
		public long NStored => this.values.Length;

		/// <inheritdoc/>
		public long Length => this.rows * this.cols;

		/// <summary>
		/// Create a new <see cref="SparseMatrix{T, TInd, TS, TSInd}"/> with given parameters.
		/// </summary>
		/// <param name="defaultValue">The default value</param>
		/// <param name="rows">The presenting number of rows</param>
		/// <param name="cols">The presenting number of columns</param>
		/// <param name="values">The original value array</param>
		/// <param name="rowIndices">The original row index array</param>
		/// <param name="colIndices">The original column index array</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="rows"/> or <paramref name="cols"/> ≤ 0</exception>
		/// <exception cref="ArgumentException">If <paramref name="values"/> is too short</exception>
		/// <remarks>The validness of the detail values (such as sorted or not) in these storages are not checked for performance issues.</remarks>
		protected SparseMatrix(long rows, long cols, TS values!!, TSInd rowIndices!!, TSInd colIndices!!, T defaultValue = default)
		{
			this.defaultValue = defaultValue;
			if (rows <= 0)
				throw new ArgumentOutOfRangeException(nameof(rows), Resources.ParameterError.CannotNegative);
			if (cols <= 0)
				throw new ArgumentOutOfRangeException(nameof(cols), Resources.ParameterError.CannotNegative);
			this.rows = rows; this.cols = cols;
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
		/// Deconstructor to be invoked by GC.
		/// </summary>
		~SparseMatrix()
		{
			this.Dispose();
		}

		/// <summary>
		/// Create an empty sparse matrix.
		/// </summary>
		protected SparseMatrix()
		{
			this.values = TS.Empty; this.rowIndices = TSInd.Empty; this.colIndices = TSInd.Empty;
		}

		static SparseMatrix<T, TInd, TS, TSInd> IValueArray<T, SparseMatrix<T, TInd, TS, TSInd>>.Empty => new();

		/// <summary>
		/// Encapsulates a method that statically create a new sparse matrix from the given <paramref name="wrapper"/>.
		/// </summary>
		/// <param name="wrapper">The <see cref="SparseArrayWrapper{TVal, TInd, TSVal, TSInd}"/> to create from.</param>
		/// <param name="vector">A created <see cref="SparseMatrix{T, TInd, TS, TSInd}"/> from the given <paramref name="wrapper"/></param>
		/// <returns>Success or not.</returns>
		protected delegate bool TryCreateFromWrapper(in SparseArrayWrapper<T, TInd, TS, TSInd> wrapper, [NotNullWhen(true)] out SparseMatrix<T, TInd, TS, TSInd>? vector);

		/// <summary>
		/// The list used to store the <see cref="TryCreateFromWrapper"/>s for sub-classes.s
		/// </summary>
		protected static readonly List<TryCreateFromWrapper> Creators = new();

		/// <summary>
		/// Statically create a new sparse matrix from the given <paramref name="wrapper"/>.
		/// </summary>
		/// <param name="wrapper">The <see cref="SparseArrayWrapper{TVal, TInd, TSVal, TSInd}"/> to create from.</param>
		/// <param name="matrix">A created <see cref="SparseMatrix{T, TInd, TS, TSInd}"/> from the given <paramref name="wrapper"/></param>
		/// <returns>Success or not.</returns>
		public static bool TryCreate(in SparseArrayWrapper<T, TInd, TS, TSInd> wrapper, [NotNullWhen(true)] out SparseMatrix<T, TInd, TS, TSInd>? matrix)
		{
			foreach (var creator in Creators)
			{
				if (creator(in wrapper, out matrix))
					return true;
			}
			matrix = null;
			return false;
		}
		#endregion

		#region equality
		/// <inheritdoc/>
		public virtual bool Equals(SparseMatrix<T, TInd, TS, TSInd>? other)
		{
			if (other is null)
				return false;
			return this.Format == other.Format && this.defaultValue == other.defaultValue && this.values == other.values && this.rowIndices == other.rowIndices && this.colIndices == other.colIndices;
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
		public override int GetHashCode() => HashCode.Combine(this.Format, this.defaultValue, this.values, this.rowIndices, this.colIndices);
		#endregion

		#region index
		/// <summary>
		/// When implemented by a derived class, get the offset to <see cref="Storage"/> of the corresponding <paramref name="row"/> and <paramref name="col"/> index.
		/// </summary>
		/// <param name="row">The presenting row index</param>
		/// <param name="col">The presenting column index</param>
		/// <returns>The offset in <typeparamref name="T"/> compared to <see cref="Storage"/>.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected abstract long GetValueOffset(long row, long col);

		/// <inheritdoc/>
		public T this[long x, long y]
		{
			get
			{
				long offset = this.GetValueOffset(x, y);
				return offset < 0 ? this.defaultValue : (this.values + offset).ToManaged<T, TS>();
			}
			set
			{
				long offset = this.GetValueOffset(x, y);
				if (offset < 0)
					throw new ArgumentException(Resources.SparseError.CannotSetSparse);
				(this.values + offset).FromManaged(value);
			}
		}

		/// <inheritdoc/>
		public SparseMatrix<T, TInd, TS, TSInd> GetSubmatrix(long offsetRow, long countRow, long offsetCol, long countCol)
		{
			var sub = new SparseArrayWrapper<T, TInd, TS, TSInd>(this.defaultValue, SparseFormat.Any);
			SpComp.SparseMatrixGetSlice(this, MatrixSliceWrapper.Create(offsetRow, countRow, offsetCol, countCol, this), ref sub);
			if (TryCreate(in sub, out var mat))
				return mat;
			sub.DisposeAll();
			throw new NotSupportedException(Resources.SparseError.FormatNotSupport);
		}

		/// <inheritdoc/>
		public void GetSubmatrix(long offsetRow, long countRow, long offsetCol, long countCol, SparseMatrix<T, TInd, TS, TSInd> overwrite)
		{
			var sub = SparseArrayWrapper<T, TInd, TS, TSInd>.Create(overwrite);
			SpComp.SparseMatrixGetSlice(this, MatrixSliceWrapper.Create(offsetRow, countRow, offsetCol, countCol, this, overwrite), ref sub);
		}

		/// <inheritdoc/>
		public void SetSubmatrix(long offsetRow, long countRow, long offsetCol, long countCol, SparseMatrix<T, TInd, TS, TSInd> value)
		{
			SpComp.SparseMatrixSetSlice(this, MatrixSliceWrapper.Create(offsetRow, countRow, offsetCol, countCol, this, value), value);
		}

		/// <inheritdoc/>
		public abstract void CopyTo(SparseMatrix<T, TInd, TS, TSInd> destination);
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
			T defaultSum = this.defaultValue * T.Create(this.rows * this.cols - this.values.Length);
			return defaultSum + ExtBlas.AggregateSum<T, TS>(this.values, 1);
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

		#region operations
		/// <inheritdoc/>
		public static void MatrixMultiplyVector(SparseMatrix<T, TInd, TS, TSInd> matrix, DenseVector<T, TS> vector, DenseVector<T, TS> vectorOut, T α, T β = default, MatrixOperation operation = MatrixOperation.None)
		{
			IMatrixVectorMultiplyOperations<T, DenseVector<T, TS>, DenseVector<T, TS>, SparseMatrix<T, TInd, TS, TSInd>>.CheckMatMulVec(matrix, vector, vectorOut, α, operation);
			SpComp.MatrixSparseMultiplyVectorDense(operation, α, matrix, vector.Storage, vector.Stride, β, vectorOut.Storage, vectorOut.Stride);
		}

		/// <inheritdoc/>
		public static void VectorMultiplyMatrix(DenseVector<T, TS> vector, SparseMatrix<T, TInd, TS, TSInd> matrix, DenseVector<T, TS> vectorOut, T α, T β = default, MatrixOperation operation = MatrixOperation.None) => MatrixMultiplyVector(matrix, vector, vectorOut, α, β, operation.Transpose());

		/// <inheritdoc/>
		public static SparseVector<T, TInd, TS, TSInd> GetDiag(SparseMatrix<T, TInd, TS, TSInd> matrix, long k)
		{
			var vector = new SparseArrayWrapper<T, TInd, TS, TSInd>(matrix.defaultValue, SparseFormat.Any);
			SpComp.SparseMatrixGetDiag(matrix, k, ref vector);
			if (SparseVector<T, TInd, TS, TSInd>.TryCreate(in vector, out var vec))
				return vec;
			vector.DisposeAll();
			throw new NotSupportedException(Resources.SparseError.FormatNotSupport);
		}

		/// <inheritdoc/>
		public static void GetDiag(SparseMatrix<T, TInd, TS, TSInd> matrix, long k, SparseVector<T, TInd, TS, TSInd> overwrite)
		{
			var vector = SparseArrayWrapper<T, TInd, TS, TSInd>.Create(overwrite);
			SpComp.SparseMatrixGetDiag(matrix, k, ref vector);
		}

		/// <inheritdoc/>
		public static void SetDiag(SparseMatrix<T, TInd, TS, TSInd> matrix, long k, SparseVector<T, TInd, TS, TSInd> value)
		{
			SpComp.SparseMatrixSetDiag(matrix, k, value);
		}

		/// <inheritdoc/>
		public static void AddMatrices(DenseMatrix<T, TS>? A, T scalarA, SparseMatrix<T, TInd, TS, TSInd>? B, T scalarB, DenseMatrix<T, TS> C, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			IMatrixOperations<T, DenseMatrix<T, TS>, SparseMatrix<T, TInd, TS, TSInd>, DenseMatrix<T, TS>>.CheckMatAdd(A, scalarA, B, scalarB, C, opA, opB);
			if (B is null || scalarB == T.Zero)
				throw new ArgumentNullException(nameof(B));
			SpComp.MatrixDenseAddSparse(opA, opB, scalarA, A?.Storage, A?.LeadDim ?? 1, scalarB, B, C.Storage, C.LeadDim);
		}

		/// <inheritdoc/>
		public static void MultiplyMatries(T α, DenseMatrix<T, TS> A, SparseMatrix<T, TInd, TS, TSInd> B, T β, DenseMatrix<T, TS> C, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			var (m, _, _) = IMatrixOperations<T, DenseMatrix<T, TS>, SparseMatrix<T, TInd, TS, TSInd>, DenseMatrix<T, TS>>.CheckMatMul(α, A, B, C, opA, opB);
			SpComp.MatrixDenseMultiplySparse(opA, opB, m, α, A.Storage, A.LeadDim, B, β, C.Storage, C.LeadDim);
		}

		/// <inheritdoc/>
		public static void AddMatrices(SparseMatrix<T, TInd, TS, TSInd>? A, T scalarA, DenseMatrix<T, TS>? B, T scalarB, DenseMatrix<T, TS> C, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None) => AddMatrices(B, scalarB, A, scalarA, C, opB, opA);

		/// <inheritdoc/>
		public static void MultiplyMatries(T α, SparseMatrix<T, TInd, TS, TSInd> A, DenseMatrix<T, TS> B, T β, DenseMatrix<T, TS> C, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			var (_, n, _) = IMatrixOperations<T, SparseMatrix<T, TInd, TS, TSInd>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>.CheckMatMul(α, A, B, C, opA, opB);
			SpComp.MatrixSparseMultiplyDense(opA, opB, n, α, A, B.Storage, B.LeadDim, β, C.Storage, C.LeadDim);
		}

		/// <inheritdoc/>
		public static void AddMatrices(SparseMatrix<T, TInd, TS, TSInd>? A, T scalarA, SparseMatrix<T, TInd, TS, TSInd>? B, T scalarB, SparseMatrix<T, TInd, TS, TSInd> C, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			IMatrixOperations<T, SparseMatrix<T, TInd, TS, TSInd>, SparseMatrix<T, TInd, TS, TSInd>, SparseMatrix<T, TInd, TS, TSInd>>.CheckMatAdd(A, scalarA, B, scalarB, C, opA, opB);
			var target = SparseArrayWrapper<T, TInd, TS, TSInd>.Create(C);
			SpComp.MatrixSparseAddSparse(opA, opB, scalarA, A, scalarB, B, ref target);
		}

		/// <inheritdoc/>
		public static void MultiplyMatries(T α, SparseMatrix<T, TInd, TS, TSInd> A, SparseMatrix<T, TInd, TS, TSInd> B, T β, SparseMatrix<T, TInd, TS, TSInd> C, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			IMatrixOperations<T, SparseMatrix<T, TInd, TS, TSInd>, SparseMatrix<T, TInd, TS, TSInd>, SparseMatrix<T, TInd, TS, TSInd>>.CheckMatMul(α, A, B, C, opA, opB);
			var target = SparseArrayWrapper<T, TInd, TS, TSInd>.Create(C);
			SpComp.MatrixSparseMultiplySparse(opA, opB, α, A, B, β, C, ref target);
		}
		#endregion

		#region operators
		/// <inheritdoc/>
		public static DenseVector<T, TS> operator *(SparseMatrix<T, TInd, TS, TSInd> matrix, DenseVector<T, TS> vector)
		{
			if (matrix.cols != vector.Length)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(vector));
			var output = vector.Storage.ResizeAlike(matrix.rows);
			try
			{
				SpComp.MatrixSparseMultiplyVectorDense(MatrixOperation.None, T.One, matrix, vector.Storage, vector.Stride, T.Zero, output, 1);
				return new(output, matrix.rows);
			}
			catch (Exception)
			{
				output.Dispose();
				throw;
			}
		}

		/// <inheritdoc/>
		public static DenseVector<T, TS> operator *(DenseVector<T, TS> vector, SparseMatrix<T, TInd, TS, TSInd> matrix)
		{
			if (matrix.cols != vector.Length)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(vector));
			var output = vector.Storage.ResizeAlike(matrix.rows);
			try
			{
				SpComp.MatrixSparseMultiplyVectorDense(MatrixOperation.Transpose, T.One, matrix, vector.Storage, vector.Stride, T.Zero, output, 1);
				return new(output, matrix.rows);
			}
			catch (Exception)
			{
				output.Dispose();
				throw;
			}
		}

		/// <inheritdoc/>
		public static SparseMatrix<T, TInd, TS, TSInd> operator -(SparseMatrix<T, TInd, TS, TSInd> matrix) => matrix.ApplyToAlike(static m => m.Scale(-T.One));

		/// <inheritdoc/>
		public static SparseMatrix<T, TInd, TS, TSInd> operator *(SparseMatrix<T, TInd, TS, TSInd> matrix, T scalar) => matrix.ApplyToAlike(m => m.Scale(scalar));

		/// <inheritdoc/>
		public static SparseMatrix<T, TInd, TS, TSInd> operator *(T scalar, SparseMatrix<T, TInd, TS, TSInd> matrix) => matrix * scalar;

		/// <inheritdoc/>
		public static SparseMatrix<T, TInd, TS, TSInd> operator /(SparseMatrix<T, TInd, TS, TSInd> matrix, T scalar) => matrix * (T.One / scalar);

		/// <inheritdoc/>
		public static SparseMatrix<T, TInd, TS, TSInd> operator ^(SparseMatrix<T, TInd, TS, TSInd> matrix, MatrixOperation operation)
		{
			if (operation == MatrixOperation.None)
				return ((ICloneable<SparseMatrix<T, TInd, TS, TSInd>>)matrix).Clone();
			if (operation == MatrixOperation.Conjugate)
				return matrix.ApplyToClone(static m => ExtBlas.PointWiseConjugate<T, TS>(m.values, 1));
			using var trans = matrix.Transpose();
			var clone = ((ICloneable<SparseMatrix<T, TInd, TS, TSInd>>)trans).Clone();
			try
			{
				if (operation.Transpose() != MatrixOperation.Conjugate)
					return clone;
				ExtBlas.PointWiseConjugate<T, TS>(clone.values, 1);
				return clone;
			}
			catch (Exception)
			{
				clone.Dispose();
				throw;
			}
		}

		/// <summary>
		/// When implemented by a derived class, get a referenced <see cref="SparseMatrix{T, TInd, TS, TSInd}"/> which is the transpose of this matrix.
		/// </summary>
		/// <returns>A referenced <see cref="SparseMatrix{T, TInd, TS, TSInd}"/> which is the transpose of this matrix.</returns>
		public abstract SparseMatrix<T, TInd, TS, TSInd> Transpose();

		/// <inheritdoc/>
		public static SparseMatrix<T, TInd, TS, TSInd> operator +(SparseMatrix<T, TInd, TS, TSInd> left, SparseMatrix<T, TInd, TS, TSInd> right)
		{
			var target = new SparseArrayWrapper<T, TInd, TS, TSInd>(left.defaultValue + right.defaultValue, SparseFormat.Any);
			SpComp.MatrixSparseAddSparse(default, default, T.One, left, T.One, right, ref target);
			if (TryCreate(in target, out var mat))
				return mat;
			target.DisposeAll();
			throw new NotSupportedException(Resources.SparseError.FormatNotSupport);
		}

		/// <inheritdoc/>
		public static SparseMatrix<T, TInd, TS, TSInd> operator -(SparseMatrix<T, TInd, TS, TSInd> left, SparseMatrix<T, TInd, TS, TSInd> right)
		{
			var target = new SparseArrayWrapper<T, TInd, TS, TSInd>(left.defaultValue + right.defaultValue, SparseFormat.Any);
			SpComp.MatrixSparseAddSparse(default, default, T.One, left, -T.One, right, ref target);
			if (TryCreate(in target, out var mat))
				return mat;
			target.DisposeAll();
			throw new NotSupportedException(Resources.SparseError.FormatNotSupport);
		}

		/// <inheritdoc/>
		public static SparseMatrix<T, TInd, TS, TSInd> operator *(SparseMatrix<T, TInd, TS, TSInd> left, SparseMatrix<T, TInd, TS, TSInd> right)
		{
			var target = new SparseArrayWrapper<T, TInd, TS, TSInd>(left.defaultValue + right.defaultValue, SparseFormat.Any);
			SpComp.MatrixSparseMultiplySparse(default, default, T.One, left, right, T.Zero, null, ref target);
			if (TryCreate(in target, out var mat))
				return mat;
			target.DisposeAll();
			throw new NotSupportedException(Resources.SparseError.FormatNotSupport);
		}

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator +(SparseMatrix<T, TInd, TS, TSInd> left, DenseMatrix<T, TS> right) => right.ApplyToAlike(m => AddMatrices(left, T.One, right, T.One, m));

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator +(DenseMatrix<T, TS> left, SparseMatrix<T, TInd, TS, TSInd> right) => left.ApplyToAlike(m => AddMatrices(left, T.One, right, T.One, m));

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator -(SparseMatrix<T, TInd, TS, TSInd> left, DenseMatrix<T, TS> right) => right.ApplyToAlike(m => AddMatrices(left, T.One, right, -T.One, m));

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator -(DenseMatrix<T, TS> left, SparseMatrix<T, TInd, TS, TSInd> right) => left.ApplyToAlike(m => AddMatrices(left, T.One, right, -T.One, m));

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator *(SparseMatrix<T, TInd, TS, TSInd> left, DenseMatrix<T, TS> right)
		{
			if (left.NCols != right.NRows)
				throw new ArgumentException(Resources.ParameterError.NotSameSize);
			var result = left.values.ResizeAlike(left.NRows * right.NCols);
			try
			{
				SpComp.MatrixSparseMultiplyDense(default, default, right.NCols, T.One, left, right.Storage, right.LeadDim, T.Zero, result, left.NRows);
				return new(result, left.NRows, right.NCols);
			}
			catch (Exception)
			{
				result.Dispose();
				throw;
			}
		}

		/// <inheritdoc/>
		public static DenseMatrix<T, TS> operator *(DenseMatrix<T, TS> left, SparseMatrix<T, TInd, TS, TSInd> right)
		{
			if (left.NCols != right.NRows)
				throw new ArgumentException(Resources.ParameterError.NotSameSize);
			var result = right.values.ResizeAlike(left.NRows * right.NCols);
			try
			{
				SpComp.MatrixDenseMultiplySparse(default, default, left.NRows, T.One, left.Storage, left.LeadDim, right, T.Zero, result, left.NRows);
				return new(result, left.NRows, right.NCols);
			}
			catch (Exception)
			{
				result.Dispose();
				throw;
			}
		}
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
		/// When implemented by a derived class, statically create a new <see cref="SparseMatrix{T, TInd, TS, TSInd}"/> from <paramref name="dense"/> vector truncating by <paramref name="threshold"/>.
		/// </summary>
		/// <param name="dense">The input dense vector to convert</param>
		/// <param name="format">The target format</param>
		/// <param name="defaultValue">The target default value</param>
		/// <param name="threshold">The threshold used to truncate to sparse array</param>
		/// <returns>The created <see cref="SparseMatrix{T, TInd, TS, TSInd}"/>.</returns>
		public abstract SparseMatrix<T, TInd, TS, TSInd> FromDense(DenseVector<T, TS> dense, SparseFormat format, T defaultValue, double threshold = 0);

		/// <inheritdoc/>
		public abstract SparseMatrix<T, TInd, TS, TSInd> CreateAlike();
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
		public abstract SparseMatrix<T, TInd, TS, TSInd> JsonDeserialize(string json);
		#endregion

		#region string
		static string IMainPropertyFormattable<SparseMatrix<T, TInd, TS, TSInd>>.StringMain => nameof(SparseMatrix<T, TInd, TS, TSInd>);

		static IEnumerable<string> IMainPropertyFormattable<SparseMatrix<T, TInd, TS, TSInd>>.PropertyNames => new[] { "DataType", "IndexType", "Format", "DefaultValue", "Values", "RowIndices", "ColumnIndices" };

		IEnumerable<object?> IMainPropertyFormattable<SparseMatrix<T, TInd, TS, TSInd>>.PropertyValues => new object[] { Unmanaged<T>.DataType, Unmanaged<TInd>.DataType, this.Format, this.defaultValue, this.values, this.rowIndices, this.colIndices };

		/// <inheritdoc/>
		public override string ToString() => IMainPropertyFormattable<SparseMatrix<T, TInd, TS, TSInd>>.ToString(this);

		/// <inheritdoc/>
		public abstract string Print(PrintSettings? settings = null);
		#endregion
	}
}
