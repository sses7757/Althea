using System;
using System.Collections.Generic;

using Althea.Linq;
using Althea.Arrays;
using Althea.Helpers;
using Althea.NativeTypes;
using Althea.LinearAlgebra;

using MEM = Althea.Storage.AbstractApi;
using LAD = Althea.LinearAlgebra.Dense.AbstractApi;
using LAS = Althea.LinearAlgebra.Sparse.AbstractApi;


namespace Althea.Backend.Arrays
{
	/// <summary>
	/// The concrete dense matrix class with the only <see cref="ValueArray{T}.Storage"/> that refers to the data storage.
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct that implements <see cref="IFormattable"/> and <see cref="IEquatable{T}"/> as the data type</typeparam>
	public class DenseMatrix<T> : MatrixBase<T>, IDenseMatrix, IKrylovVector<DenseMatrix<T>, T> where T : unmanaged, IFormattable, IEquatable<T>
	{
		#region basic
		/// <summary>
		/// Get the leading dimension (the length in <typeparamref name="T"/> between to consecutive column starting elements) of this dense matrix
		/// </summary>
		public long LeadDim { get; }

		/// <summary>
		/// Construct a <see cref="DenseMatrix{T}"/> with value array <paramref name="values"/> and size <paramref name="rows"/>, <paramref name="cols"/>
		/// </summary>
		/// <param name="values">The value array as a <see cref="Storage{T}"/></param>
		/// <param name="rows">The number of rows of this matrix</param>
		/// <param name="cols">The number of columns of this matrix</param>
		/// <param name="leadDim">The leading dimension of this matrix. Default 0 means <paramref name="rows"/>.</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="rows"/> or <paramref name="cols"/> or <paramref name="leadDim"/> is not positive</exception>
		/// <exception cref="ArgumentException">If <paramref name="leadDim"/> is less than <paramref name="rows"/> or the given size exceeds the boundary of <paramref name="values"/></exception>
		public DenseMatrix(Storage<T> values, long rows, long cols, long leadDim = 0) : base(values, rows, cols, actualLength: leadDim * (cols - 1) + rows)
		{
			if (leadDim == 0)
				leadDim = rows;
			if (leadDim < 0)
				throw new ArgumentOutOfRangeException(nameof(leadDim), leadDim, Resources.Parameter.MustPositive);
			if (leadDim < rows)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(leadDim));

			this.LeadDim = leadDim;
		}

		/// <summary>
		/// Create am empty <see cref="DenseMatrix{T}"/>
		/// </summary>
		public DenseMatrix() : base(Storage<T>.Empty, 0, 0)
		{
			this.LeadDim = 0;
		}
		#endregion

		#region basic indexers
		/// <summary>
		/// Get a sub-matrix by the row and column index ranges.
		/// </summary>
		/// <param name="offsetRow">The starting offset of the row to take</param>
		/// <param name="countRow">The number of the rows to take</param>
		/// <param name="offsetCol">The starting offset of the columns to take</param>
		/// <param name="countCol">The number of the columns to take</param>
		/// <returns>The sub-matrix (may be a referenced one) in the region indicated by the ranges</returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offsetRow"/> or <paramref name="countRow"/> or <paramref name="offsetCol"/> or <paramref name="countCol"/> is out of range</exception>
		public override DenseMatrix<T> GetSubmatrix(long offsetRow, long countRow, long offsetCol, long countCol)
		{
			this.CheckRange(offsetRow, countRow, offsetCol, countCol);
			return new DenseMatrix<T>(this.Storage + (offsetCol * this.LeadDim + offsetRow), countRow, countCol, this.LeadDim);
		}

		/// <summary>
		/// Get a sub-matrix by the row and column index ranges and copy it to <paramref name="overwrite"/>.
		/// </summary>
		/// <param name="offsetRow">The starting offset of the row to take</param>
		/// <param name="countRow">The number of the rows to take</param>
		/// <param name="offsetCol">The starting offset of the columns to take</param>
		/// <param name="countCol">The number of the columns to take</param>
		/// <param name="overwrite">The <see cref="MatrixBase{T}"/> to be overwritten</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offsetRow"/> or <paramref name="countRow"/> or <paramref name="offsetCol"/> or <paramref name="countCol"/> is out of range</exception>
		/// <exception cref="ArgumentException">If <paramref name="overwrite"/> cannot be overwritten</exception>
		public override void GetSubmatrix(long offsetRow, long countRow, long offsetCol, long countCol, MatrixBase<T> overwrite)
		{
			this.CheckRange(offsetRow, countRow, offsetCol, countCol);
			if (overwrite is null || !overwrite.IsValid())
				throw new ArgumentNullException(nameof(overwrite));
			if (overwrite is not DenseMatrix<T> dense)
				throw new ArgumentException(Resources.Parameter.InvalidValue, nameof(overwrite));
			if (dense.NRows < countRow || dense.NCols < countCol)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(overwrite));

			MEM.MemoryCopy2D(this.Storage + (offsetCol * this.LeadDim + offsetRow), this.LeadDim, dense.Storage, dense.LeadDim, countRow, countCol);
		}

		/// <summary>
		/// Set a sub-matrix by the row and column starting index (inclusive).
		/// </summary>
		/// <param name="rowStart">The <see cref="long"/> to indicate the starting row index to set</param>
		/// <param name="columnStart">The <see cref="long"/> to indicate the starting column index to set</param>
		/// <param name="value">The <see cref="MatrixBase{T}"/> whose value will overwrite this matrix from (<paramref name="rowStart"/>, <paramref name="columnStart"/>)</param>
		/// <exception cref="ArgumentNullException">If <paramref name="value"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="rowStart"/> or <paramref name="columnStart"/> and <paramref name="value"/>'s <see cref="MatrixBase{T}.NRows"/> or <see cref="MatrixBase{T}.NCols"/> are out of range</exception>
		/// <exception cref="NotSupportedException">If <paramref name="value"/> is neither a <see cref="DenseMatrix{T}"/> nor a <see cref="ISparseMatrix{T}"/></exception>
		public override void SetSubmatrix(long rowStart, long columnStart, MatrixBase<T> value)
		{
			if (value is null || !value.IsValid())
				throw new ArgumentNullException(nameof(value));
			this.CheckRange(rowStart, columnStart, value.NRows, value.NCols);

			if (value is DenseMatrix<T> dense)
			{
				MEM.MemoryCopy2D(dense.Storage, dense.LeadDim, this.Storage + (rowStart * this.LeadDim + columnStart), this.LeadDim, dense.NRows, dense.NCols);
			}
			else if (value is ISparseMatrix<T> sparse)
			{
				using var dn = this.Storage.MakeReference(newLength: sparse.NRows * sparse.NCols).CreateAlike();
				sparse.ToDense(dn, sparse.NRows);
				MEM.MemoryCopy2D(dn, sparse.NRows, this.Storage + (rowStart * this.LeadDim + columnStart), this.LeadDim, sparse.NRows, sparse.NCols);
			}
			else
				throw new NotSupportedException();
		}

		/// <summary>
		/// Get or set the element at the given position (<paramref name="x"/>, <paramref name="y"/>)
		/// </summary>
		/// <param name="x">The row position as a <see cref="long"/></param>
		/// <param name="y">The column position as a <see cref="long"/></param>
		/// <returns>The element at position (<paramref name="x"/>, <paramref name="y"/>)</returns>
		public override T this[long x, long y] {
			get {
				this.CheckIndex(x, y);
				return MEM.ToManaged(this.Storage + (y * this.LeadDim + x));
			}
			set {
				this.CheckIndex(x, y);
				MEM.FromManaged(this.Storage + (y * this.LeadDim + x), value);
			}
		}
		#endregion

		#region diagonal indexers
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private Storage<T> GetDiagStorage(long k) => this.Storage + (k <= 0 ? k : k * this.LeadDim);

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private int GetDiagStride() => checked((int)(this.LeadDim + 1));

		/// <summary>
		/// Get the <paramref name="k"/>-th diagonal elements.
		/// </summary>
		/// <param name="k">The diagonal index: 0 for diagonal, 1 for super-diagonal at one above, -1 for sub-diagonal at one below, etc.</param>
		/// <returns>A new <see cref="DenseVector{T}"/> containing the <paramref name="k"/>-th diagonal elements.</returns>
		/// <exception cref="InvalidOperationException">If this matrix is not a square matrix</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="k"/> is out of range</exception>
		public override DenseVector<T> GetDiag(long k)
		{
			if (this.NRows != this.NCols)
				throw new InvalidOperationException();
			if (Math.Abs(k) >= this.NRows)
				throw new ArgumentOutOfRangeException(nameof(k), k, Resources.Parameter.InvalidValue);

			Storage<T>? storage = null;
			try
			{
				storage = this.Storage.MakeReference(newLength: this.NRows - k).CreateAlike();
				MEM.StridedCopy(this.GetDiagStorage(k), this.GetDiagStride(), storage, 1);
				return new DenseVector<T>(storage);
			}
			catch (Exception)
			{
				storage?.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Get the <paramref name="k"/>-th diagonal elements and write the result to <paramref name="overwrite"/>.
		/// </summary>
		/// <param name="k">The diagonal index: 0 for diagonal, 1 for super-diagonal at one above, -1 for sub-diagonal at one below, etc.</param>
		/// <param name="overwrite">The output <see cref="VectorBase{T}"/> which will contain the <paramref name="k"/>-th diagonal elements at exit</param>
		/// <exception cref="InvalidOperationException">If this matrix is not a square matrix</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="k"/> is out of range</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="overwrite"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="overwrite"/> cannot be overwritten</exception>
		public override void GetDiag(long k, VectorBase<T> overwrite)
		{
			if (this.NRows != this.NCols)
				throw new InvalidOperationException();
			if (Math.Abs(k) >= this.NRows)
				throw new ArgumentOutOfRangeException(nameof(k), k, Resources.Parameter.InvalidValue);
			if (overwrite is null || !overwrite.IsValid())
				throw new ArgumentNullException(nameof(overwrite));
			if (overwrite is not DenseVector<T> dense)
				throw new ArgumentException(Resources.Parameter.InvalidValue, nameof(overwrite));
			if (dense.Length < this.NRows - k)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(overwrite));

			MEM.StridedCopy(this.GetDiagStorage(k), this.GetDiagStride(), dense.Storage, 1);
		}

		/// <summary>
		/// Set the <paramref name="k"/>-th diagonal elements to <paramref name="value"/>.
		/// </summary>
		/// <param name="k">The diagonal index: 0 for diagonal, 1 for super-diagonal at one above, -1 for sub-diagonal at one below, etc.</param>
		/// <param name="value">The <paramref name="k"/>-th diagonal elements to set as a <see cref="VectorBase{T}"/></param>
		/// <exception cref="InvalidOperationException">If this matrix is not a square matrix</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="k"/> is out of range</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="value"/> is null or invalid</exception>
		/// <exception cref="NotSupportedException">If <paramref name="value"/> is neither a <see cref="DenseVector{T}"/> nor a <see cref="ISparseVector{T}"/></exception>
		public override void SetDiag(long k, VectorBase<T> value)
		{
			if (this.NRows != this.NCols)
				throw new InvalidOperationException();
			if (Math.Abs(k) >= this.NRows)
				throw new ArgumentOutOfRangeException(nameof(k), k, Resources.Parameter.InvalidValue);
			if (value is null || !value.IsValid())
				throw new ArgumentNullException(nameof(value));

			if (value is DenseVector<T> dense)
			{
				MEM.StridedCopy(dense.Storage, 1, this.GetDiagStorage(k), this.GetDiagStride());
			}
			else if (value is ISparseVector<T> sparse)
			{
				using var dn = this.Storage.MakeReference(newLength: this.NRows - k).CreateAlike();
				sparse.ToDense(dn);
				MEM.StridedCopy(dn, 1, this.GetDiagStorage(k), this.GetDiagStride());
			}
			else
				throw new NotSupportedException();
		}
		#endregion

		#region clone related
		/// <summary>
		/// Deep clone the matrix. This implementation utilizes <see cref="Storage{T}.Clone"/>.
		/// </summary>
		/// <returns>The cloned vector</returns>
		public override DenseMatrix<T> Clone()
		{
			var c = this.Storage.MakeReference(newLength: this.NRows * this.NCols).CreateAlike();
			try
			{
				MEM.MemoryCopy2D(this.Storage, this.LeadDim, c, this.NRows, this.NRows, this.NCols);
				return new DenseMatrix<T>(c, this.NRows, this.NCols);
			}
			catch (Exception)
			{
				c?.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Create a new matrix with same properties as this one while the underlying storages are not filled. This implementation utilizes <see cref="Althea.Storage.StorageFactory{T}"/>.
		/// </summary>
		/// <returns>The new vector alike this one</returns>
		public override DenseMatrix<T> NewArrayAlike()
		{
			var c = this.Storage.MakeReference(newLength: this.NRows * this.NCols).CreateAlike();
			return new DenseMatrix<T>(c, this.NRows, this.NCols);
		}

		/// <summary>
		/// Create a new matrix with same properties as this one while the underlying storages are not filled and the data type is changed to <typeparamref name="TOut"/>. This implementation utilizes <see cref="Althea.Storage.StorageFactory{T}"/> of <typeparamref name="TOut"/>.
		/// </summary>
		/// <typeparam name="TOut">Any unmanaged struct as the new data type</typeparam>
		/// <returns>The new matrix alike this one</returns>
		public override DenseMatrix<TOut> NewArrayAlike<TOut>()
		{
			var c = this.Storage.MakeReference(newLength: this.NRows * this.NCols).CreateAlike<TOut>();
			return new DenseMatrix<TOut>(c, this.NRows, this.NCols);
		}
		#endregion

		#region reshape
		/// <summary>
		/// When implemented by a derived class, reshape this array to a vector
		/// </summary>
		/// <returns>The referenced vector reshaped from this array</returns>
		/// <remarks>If <see cref="MatrixBase{T}.NRows"/> != <see cref="LeadDim"/>, a new vector will be created to return.</remarks>
		public override DenseVector<T> ToVector()
		{
			if (this.NRows == this.LeadDim)
				return new DenseVector<T>(this.Storage.MakeReference(newLength: this.Length));
			// else
			var storageOut = this.Storage.MakeReference(newLength: this.Length).CreateAlike();
			try
			{
				MEM.MemoryCopy2D(this.Storage, this.LeadDim, storageOut, this.NRows, this.NRows, this.NCols);
				return new DenseVector<T>(storageOut);
			}
			catch (Exception)
			{
				storageOut?.Dispose();
				throw;
			}
		}

		/// <summary>
		/// When implemented by a derived class, reshape this matrix to a matrix with leading dimension = <paramref name="rows"/>
		/// </summary>
		/// <param name="rows">The number of rows of the target matrix; if <paramref name="rows"/> ≤ 0, it is assumed that leadDim = <c>sqrt(<see cref="AbstractArray{T}.Length"/>)</c>.</param>
		/// <returns>The reshaped matrix, may be this matrix itself</returns>
		/// <remarks>If the computed new number of rows is not <see cref="MatrixBase{T}.NRows"/> and <see cref="MatrixBase{T}.NRows"/> != <see cref="LeadDim"/>, a new matrix will be created to return.</remarks>
		/// <exception cref="InvalidOperationException">If the computed new number of rows or columns is 1</exception>
		public override DenseMatrix<T> ToMatrix(long rows = 0)
		{
			Span<long> newSize = stackalloc long[2];
			newSize[0] = rows;
			CheckSize(this, newSize);
			if (newSize[0] == this.NRows)
				return this;
			else if (this.NRows == this.LeadDim)
				return new DenseMatrix<T>(this.Storage, newSize[0], newSize[1]);
			else if (newSize[0] == 1 || newSize[1] == 1)
				throw new InvalidOperationException();
			// else
			var storageOut = this.Storage.MakeReference(newLength: newSize[0] * newSize[1]).CreateAlike();
			try
			{
				MEM.MemoryCopy2D(this.Storage, this.LeadDim, storageOut, newSize[0], newSize[0], newSize[1]);
				return new DenseMatrix<T>(storageOut, newSize[0], newSize[1]);
			}
			catch (Exception)
			{
				storageOut?.Dispose();
				throw;
			}
		}

		/// <summary>
		/// When implemented by a derived class, reshape the array to a tensor with dimensionality = <paramref name="size"/>.
		/// </summary>
		/// <param name="size">The new size/dimensionality with at most one or zero uncertain dimension indicated by a non-positive number.</param>
		/// <returns>The reshaped tensor</returns>
		/// <exception cref="InvalidOperationException">If the <see cref="LeadDim"/> != <see cref="MatrixBase{T}.NRows"/></exception>
		public override DenseTensor<T> ToTensor(ReadOnlySpan<long> size)
		{
			if (this.NRows != this.LeadDim)
				throw new InvalidOperationException();

			Span<long> newSize = stackalloc long[size.Length];
			size.CopyTo(newSize);
			CheckSize(this, newSize);
			return new DenseTensor<T>(pointer: this.Storage, newSize);
		}
		#endregion

		#region linear algebra
		/// <summary>
		/// Create a new <see cref="MatrixBase{T}"/> which is the simple operation result of this matrix under <paramref name="operation"/>.
		/// </summary>
		/// <param name="operation">The input <see cref="MatrixOperation"/> used as the simple operation to be applied</param>
		/// <returns>A new <see cref="DenseMatrix{T}"/> as the result of <paramref name="operation"/>(this)</returns>
		/// <exception cref="NotSupportedException">If the given <paramref name="operation"/> is not supported</exception>
		public override DenseMatrix<T> ApplyOperation(MatrixOperation operation)
		{
			// shortcut
			if (operation == MatrixOperation.None)
				return this.Clone();
			if (operation == MatrixOperation.Conjugate)
				return this.ApplyToClone(static c => LAD.PointWiseConjugate(c.Storage, 1));
			// otherwise
			var (m, n) = (this.NCols, this.NRows);
			var storageOut = this.Storage.MakeReference(newLength: m * n).CreateAlike();
			try
			{
				LAD.GeneralMatricesAdd(operation, MatrixOperation.None, m, n, Scalars<T>.One, this.Storage, this.LeadDim, Scalars<T>.Zero, null, 0, storageOut, m);
				return new DenseMatrix<T>(storageOut, m, n);
			}
			catch (Exception)
			{
				storageOut?.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Create a new <see cref="MatrixBase{T}"/> which is the point-wise addition result of this matrix the <paramref name="other"/> matrix.
		/// </summary>
		/// <param name="scalarThis">The scalar to multiply to this matrix before addition</param>
		/// <param name="scalarOther">The scalar to multiply to the <paramref name="other"/> matrix before addition</param>
		/// <param name="other">The input right <see cref="MatrixBase{T}"/> to be added</param>
		/// <param name="opThis">The <see cref="MatrixOperation"/> to apply to this matrix before addition</param>
		/// <param name="opOther">The <see cref="MatrixOperation"/> to apply to the <paramref name="other"/> matrix before addition</param>
		/// <returns>A new <see cref="DenseMatrix{T}"/> as the result of <c><paramref name="scalarThis"/> * <paramref name="opThis"/>(this) + <paramref name="scalarOther"/> * <paramref name="opOther"/>(<paramref name="other"/>)</c></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="other"/> is null or empty</exception>
		/// <exception cref="NotSupportedException">If the given <paramref name="opThis"/> or <paramref name="opOther"/> is not supported; or <paramref name="other"/> is neither a <see cref="DenseMatrix{T}"/> nor a <see cref="ISparseMatrix{T}"/></exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="scalarThis"/> or <paramref name="scalarOther"/> is 0</exception>
		/// <exception cref="ArgumentException">If the addition cannot be performed due to incompatible sizes</exception>
		public override DenseMatrix<T> AddMatrix(T scalarThis, T scalarOther, MatrixBase<T> other, MatrixOperation opThis = MatrixOperation.None, MatrixOperation opOther = MatrixOperation.None)
		{
			if (other is null || !other.IsValid())
				throw new ArgumentNullException(nameof(other));
			if (scalarThis.IsZero())
				throw new ArgumentOutOfRangeException(nameof(scalarThis), scalarThis, Resources.Parameter.CannotZero);
			if (scalarOther.IsZero())
				throw new ArgumentOutOfRangeException(nameof(scalarOther), scalarOther, Resources.Parameter.CannotZero);
			var (m, n) = opThis.CanInPlace() ? (this.NRows, this.NCols) : (this.NCols, this.NRows);
			var (p, q) = opOther.CanInPlace() ? (other.NRows, other.NCols) : (other.NCols, other.NRows);
			if (m != p || n != q)
				throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(other));

			var storageOut = this.Storage.MakeReference(newLength: m * n).CreateAlike();
			try
			{
				if (other is DenseMatrix<T> dense)
				{
					LAD.GeneralMatricesAdd(opThis, opOther, m, n, scalarThis, this.Storage, this.LeadDim, scalarOther, dense.Storage, dense.LeadDim, storageOut, m);
				}
				else if (other is ISparseMatrix<T> sparse)
				{
					LAS.MatrixDenseAddSparse(opThis, opOther, scalarThis, this.Storage, this.LeadDim, scalarOther, sparse, storageOut, m);
				}
				else
					throw new NotSupportedException();

				return new DenseMatrix<T>(storageOut, m, n);
			}
			catch (Exception)
			{
				storageOut?.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Create a new <see cref="MatrixBase{T}"/> which is the multiplication result of this matrix and the <paramref name="other"/> matrix.
		/// </summary>
		/// <param name="scalar">The scalar to multiply to the result</param>
		/// <param name="other">The input right <see cref="MatrixBase{T}"/> to be multiplied</param>
		/// <param name="opThis">The <see cref="MatrixOperation"/> to apply to this matrix before addition</param>
		/// <param name="opOther">The <see cref="MatrixOperation"/> to apply to the <paramref name="other"/> matrix before addition</param>
		/// <returns>A new <see cref="MatrixBase{T}"/> as the result of <c><paramref name="scalar"/> * <paramref name="opThis"/>(this) * <paramref name="opOther"/>(<paramref name="other"/>)</c></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="other"/> is null or empty</exception>
		/// <exception cref="NotSupportedException">If the given <paramref name="opThis"/> or <paramref name="opOther"/> is not supported; or <paramref name="other"/> is neither a <see cref="DenseMatrix{T}"/> nor a <see cref="ISparseMatrix{T}"/></exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="scalar"/> is 0</exception>
		/// <exception cref="ArgumentException">If the multiplication cannot be performed due to incompatible sizes</exception>
		public override MatrixBase<T> MultiplyMatrix(T scalar, MatrixBase<T> other, MatrixOperation opThis = MatrixOperation.None, MatrixOperation opOther = MatrixOperation.None)
		{
			if (other is null || !other.IsValid())
				throw new ArgumentNullException(nameof(other));
			if (scalar.IsZero())
				throw new ArgumentOutOfRangeException(nameof(scalar), scalar, Resources.Parameter.CannotZero);
			var (m, n) = opThis.CanInPlace() ? (this.NRows, this.NCols) : (this.NCols, this.NRows);
			var (p, q) = opOther.CanInPlace() ? (other.NRows, other.NCols) : (other.NCols, other.NRows);
			if (n != p)
				throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(other));

			var storageOut = this.Storage.MakeReference(newLength: m * q).CreateAlike();
			try
			{
				if (other is DenseMatrix<T> dense)
				{
					LAD.GeneralMatricesMultiply(opThis, opOther, m, q, n, scalar, this.Storage, this.LeadDim, dense.Storage, dense.LeadDim, Scalars<T>.Zero, storageOut, m);
				}
				else if (other is ISparseMatrix<T> sparse)
				{
					LAS.MatrixDenseMultiplySparse(opThis, opOther, m, scalar, this.Storage, this.LeadDim, sparse, Scalars<T>.Zero, storageOut, m);
				}
				else
					throw new NotSupportedException();

				return new DenseMatrix<T>(storageOut, m, q);
			}
			catch (Exception)
			{
				storageOut?.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Overwrite this matrix with the sum of given matrices: <c>this = <paramref name="scalarA"/> * <paramref name="opA"/>(<paramref name="A"/>) + <paramref name="scalarB"/> * <paramref name="opB"/>(<paramref name="B"/>)</c>.
		/// </summary>
		/// <param name="scalarA">The scalar to multiply to <paramref name="A"/>, can be zero</param>
		/// <param name="scalarB">The scalar to multiply to <paramref name="B"/>, can be zero</param>
		/// <param name="A">The left input dense matrix, can be null or this</param>
		/// <param name="B">The right input dense matrix, can be null or this</param>
		/// <param name="opA">The <see cref="MatrixOperation"/> to apply to the <paramref name="A"/> matrix before addition</param>
		/// <param name="opB">The <see cref="MatrixOperation"/> to apply to the <paramref name="B"/> matrix before addition</param>
		/// <exception cref="ArgumentException">If both <paramref name="scalarA"/> and <paramref name="scalarB"/> are 0; or the sizes are incompatible</exception>
		/// <exception cref="ArgumentNullException">If both <paramref name="A"/> and <paramref name="B"/> are null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="A"/> is this matrix while <paramref name="opA"/> is not <see cref="MatrixOperation.None"/> or <paramref name="B"/> is this matrix while <paramref name="opB"/> is not <see cref="MatrixOperation.None"/></exception>
		/// <exception cref="NotSupportedException">If the given <paramref name="opA"/> or <paramref name="opB"/> is not supported</exception>
		public virtual void OverwriteByMatricesSum(DenseMatrix<T>? A, DenseMatrix<T>? B, T scalarA = default, T scalarB = default, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			if (scalarA.IsZero() && scalarB.IsZero())
				throw new ArgumentException(Resources.Parameter.CannotZero);
			if (A is null || !A.IsValid())
				throw new ArgumentNullException(nameof(A));
			if (B is null || !B.IsValid())
				throw new ArgumentNullException(nameof(B));
			if (ReferenceEquals(this, A) && opA != MatrixOperation.None)
				throw new ArgumentOutOfRangeException(nameof(opA), opA, Resources.Parameter.InvalidValue);
			if (ReferenceEquals(this, B) && opB != MatrixOperation.None)
				throw new ArgumentOutOfRangeException(nameof(opB), opB, Resources.Parameter.InvalidValue);
			var (m, n) = (this.NRows, this.NCols);
			if (A is not null)
			{
				var (p, q) = opA.CanInPlace() ? (A.NRows, A.NCols) : (A.NCols, A.NRows);
				if (m != p || n != q)
					throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(A));
			}
			if (B is not null)
			{
				var (p, q) = opB.CanInPlace() ? (B.NRows, B.NCols) : (B.NCols, B.NRows);
				if (m != p || n != q)
					throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(B));
			}

			LAD.GeneralMatricesAdd(opA, opB, m, n, scalarA, A?.Storage, A?.LeadDim ?? 0, scalarB, B?.Storage, B?.LeadDim ?? 0, this.Storage, this.LeadDim);
		}
		#endregion

		#region point-wise operations
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private void ApplyToColumns(Action<Storage<T>> action)
		{
			long rows = this.NRows, cols = this.NCols, ld = this.LeadDim;
			var storage = this.Storage;
			for (long i = 0; i < cols; i++)
			{
				var column = storage.MakeReference(i * ld, newLength: rows);
				action.Invoke(column);
			}
		}
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private void ApplyToColumns<TVal>(Action<Storage<T>, TVal> action, TVal value)
		{
			long rows = this.NRows, cols = this.NCols, ld = this.LeadDim;
			var storage = this.Storage;
			for (long i = 0; i < cols; i++)
			{
				var column = storage.MakeReference(i * ld, newLength: rows);
				action.Invoke(column, value);
			}
		}
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private TRet ApplyToColumns<TRet>(Func<Storage<T>, TRet> action, Func<TRet, TRet, TRet> returnAggregate, TRet init)
		{
			long rows = this.NRows, cols = this.NCols, ld = this.LeadDim;
			var storage = this.Storage;
			for (long i = 0; i < cols; i++)
			{
				var column = storage.MakeReference(i * ld, newLength: rows);
				TRet here = action.Invoke(column);
				init = returnAggregate.Invoke(init, here);
			}
			return init;
		}
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private TRet ApplyToColumns<TVal, TRet>(Func<Storage<T>, TVal, TRet> action, TVal value, Func<TRet, TRet, TRet> returnAggregate, TRet init)
		{
			long rows = this.NRows, cols = this.NCols, ld = this.LeadDim;
			var storage = this.Storage;
			for (long i = 0; i < cols; i++)
			{
				var column = storage.MakeReference(i * ld, newLength: rows);
				TRet here = action.Invoke(column, value);
				init = returnAggregate.Invoke(init, here);
			}
			return init;
		}

		/// <summary>
		/// Fill this dense matrix's <see cref="Storage"/> with given <paramref name="value"/>. The default implementation utilizes <see cref="MEM.FillWithValue{T}(Storage{T}, T)"/>.
		/// </summary>
		/// <param name="value">The value as <typeparamref name="T"/> to fill</param>
		public unsafe override void FillWith(T value)
		{
			if (this.NRows == this.LeadDim)
			{
				MEM.FillWithValue(this.Storage, value);
			}
			else
			{
				this.ApplyToColumns(MEM.FillWithValue, value);
			}
		}

		/// <summary>
		/// Point-wisely in-place add this dense matrix's <see cref="Storage"/> with given <paramref name="value"/>. The default implementation utilizes <see cref="LAD.PointWiseAddScalar{T}"/>.
		/// </summary>
		/// <param name="value">The scalar as <typeparamref name="T"/> to add</param>
		public unsafe override void AddScalar(T value)
		{
			if (this.NRows == this.LeadDim)
			{
				LAD.PointWiseAddScalar(this.Storage, 1, value);
			}
			else if (this.NRows == 1)
			{
				LAD.PointWiseAddScalar(this.Storage, checked((int)this.LeadDim), value);
			}
			else
			{
				this.ApplyToColumns(static (s, v) => LAD.PointWiseAddScalar(s, 1, v), value);
			}
		}

		/// <summary>
		/// Point-wisely in-place multiply this dense matrix's <see cref="Storage"/> with given <paramref name="value"/>. The default implementation utilizes <see cref="LAD.Scale{T}"/>.
		/// </summary>
		/// <param name="value">The scalar as <typeparamref name="T"/> to multiply</param>
		public override void Scale(T value)
		{
			if (this.NRows == this.LeadDim)
			{
				LAD.Scale(value, this.Storage, 1);
			}
			else if (this.NRows == 1)
			{
				LAD.Scale(value, this.Storage, checked((int)this.LeadDim));
			}
			else
			{
				this.ApplyToColumns(static (s, v) => LAD.Scale(v, s, 1), value);
			}
		}

		/// <summary>
		/// Point-wisely in-place conjugate this dense matrix's <see cref="Storage"/>. The default implementation utilizes <see cref="LAD.PointWiseConjugate{T}"/>.
		/// </summary>
		public override void Conjugate()
		{
			if (this.NRows == this.LeadDim)
			{
				LAD.PointWiseConjugate(this.Storage, 1);
			}
			else if (this.NRows == 1)
			{
				LAD.PointWiseConjugate(this.Storage, checked((int)this.LeadDim));
			}
			else
			{
				this.ApplyToColumns(static s => LAD.PointWiseConjugate(s, 1));
			}
		}

		/// <summary>
		/// Point-wisely in-place exponent this dense matrix's <see cref="Storage"/> with given <paramref name="power"/>. The default implementation utilizes <see cref="LAD.PointWisePower{T}(Storage{T}, int, double)"/>.
		/// </summary>
		/// <param name="power">The power as a <see cref="double"/></param>
		public override void Power(double power)
		{
			if (this.NRows == this.LeadDim)
			{
				LAD.PointWisePower(this.Storage, 1, power);
			}
			else if (this.NRows == 1)
			{
				LAD.PointWisePower(this.Storage, checked((int)this.LeadDim), power);
			}
			else
			{
				this.ApplyToColumns(static (s, v) => LAD.PointWisePower(s, 1, v), power);
			}
		}

		/// <summary>
		/// Point-wisely in-place exponent this dense matrix's <see cref="Storage"/> with given <paramref name="power"/>. The default implementation utilizes <see cref="LAD.PointWisePower{T}(Storage{T}, int, T)"/>.
		/// </summary>
		/// <param name="power">The power as a <typeparamref name="T"/></param>
		public override void Power(T power)
		{
			if (this.NRows == this.LeadDim)
			{
				LAD.PointWisePower(this.Storage, 1, power);
			}
			else if (this.NRows == 1)
			{
				LAD.PointWisePower(this.Storage, checked((int)this.LeadDim), power);
			}
			else
			{
				this.ApplyToColumns(static (s, v) => LAD.PointWisePower(s, 1, v), power);
			}
		}

		/// <summary>
		/// Point-wisely in-place truncate this dense matrix's <see cref="Storage"/> by comparing with given <paramref name="threshold"/>. The default implementation utilizes <see cref="LAD.PointWisePower{T}(Storage{T}, int, T)"/>.
		/// </summary>
		/// <param name="threshold">The threshold as a <see cref="double"/>. Any element in <see cref="Storage"/> whose absolute value ≤ <paramref name="threshold"/> will be set to 0.</param>
		public override void Truncate(double threshold)
		{
			if (this.NRows == this.LeadDim)
			{
				LAD.TruncateArray(this.Storage, threshold);
			}
			else
			{
				this.ApplyToColumns(static (s, v) => LAD.TruncateArray(s, v), threshold);
			}
		}

		/// <summary>
		/// Aggregately sum the elements in this array. The default implementation only sums <see cref="Storage"/> and utilizes <see cref="LAD.AggregateSum{T}"/>.
		/// </summary>
		/// <returns>The aggregate sum of this array</returns>
		public override T Sum()
		{
			if (this.NRows == this.LeadDim)
			{
				return LAD.AggregateSum(this.Storage, 1);
			}
			else if (this.NRows == 1)
			{
				return LAD.AggregateSum(this.Storage, checked((int)this.LeadDim));
			}
			else
			{
				return this.ApplyToColumns(static s => LAD.AggregateSum(s, 1), static (a, b) => a.GenericAdd(b), default);
			}
		}

		/// <summary>
		/// Aggregately sum the absolute values of elements in this array. The default implementation only sums <see cref="Storage"/> and utilizes <see cref="LAD.AbsoluteValueSum{T}"/>.
		/// </summary>
		/// <returns>The aggregate sum of absolute values of this array</returns>
		public override double AbsSum()
		{
			if (this.NRows == this.LeadDim)
			{
				return LAD.AbsoluteValueSum(this.Storage, 1);
			}
			else if (this.NRows == 1)
			{
				return LAD.AbsoluteValueSum(this.Storage, checked((int)this.LeadDim));
			}
			else
			{
				return this.ApplyToColumns(static s => LAD.AbsoluteValueSum(s, 1), static (a, b) => a + b, 0.0);
			}
		}

		/// <summary>
		/// Compute the 2-norm (Euclidean norm) of elements in this array. The default implementation only sums <see cref="Storage"/> and utilizes <see cref="LAD.Norm{T}"/>.
		/// </summary>
		/// <returns>The 2-norm of this array</returns>
		public override double Norm()
		{
			if (this.NRows == this.LeadDim)
			{
				return LAD.Norm(this.Storage, 1);
			}
			else if (this.NRows == 1)
			{
				return LAD.Norm(this.Storage, checked((int)this.LeadDim));
			}
			else
			{
				double normSquare = this.ApplyToColumns(static s => LAD.Norm(s, 1), static (a, b) => a + b * b, 0.0);
				return Math.Sqrt(normSquare);
			}
		}

		/// <summary>
		/// Point-wisely in-place scale this dense matrix's <see cref="Storage"/> such that its 2-norm (Euclidean norm) is 1 and utilizes the <see cref="Norm()"/> and <see cref="Scale(T)"/>.
		/// </summary>
		/// <exception cref="DivideByZeroException">If the 2-norm of this array is 0</exception>
		public override void Normalize()
		{
			double norm = this.Norm();
			if (norm == 0)
				throw new DivideByZeroException();
			this.Scale((1 / norm).FromDouble<T>());
		}

		/// <summary>
		/// Get the maximum one of all absolute values of the elements in this array. The default implementation only get the maximum absolute value of <see cref="Storage"/>. The default implementation utilizes <see cref="LAD.AbsoluteValueArgMax{T}"/>.
		/// </summary>
		/// <returns>The maximum one of all absolute values of the elements in this array</returns>
		public override double AbsMax()
		{
			[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
			static double GetMax(Storage<T> storage, int stride) => MEM.ToManaged(storage + LAD.AbsoluteValueArgMax(storage, stride)).GenericAbsolute();

			if (this.NRows == this.LeadDim)
			{
				return GetMax(this.Storage, 1);
			}
			else if (this.NRows == 1)
			{
				return GetMax(this.Storage, checked((int)this.LeadDim));
			}
			else
			{
				return this.ApplyToColumns(static s => GetMax(s, 1), static (pre, now) => Math.Max(pre, now), 0.0);
			}
		}

		/// <summary>
		/// Get the minimum one of all absolute values of the elements in this array. The default implementation only get the maximum absolute value of <see cref="Storage"/> and utilizes <see cref="LAD.AbsoluteValueArgMin{T}"/>.
		/// </summary>
		/// <returns>The minimum one of all absolute values of the elements in this array</returns>
		public override double AbsMin()
		{
			[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
			static double GetMin(Storage<T> storage, int stride) => MEM.ToManaged(storage + LAD.AbsoluteValueArgMax(storage, stride)).GenericAbsolute();

			if (this.NRows == this.LeadDim)
			{
				return GetMin(this.Storage, 1);
			}
			else if (this.NRows == 1)
			{
				return GetMin(this.Storage, checked((int)this.LeadDim));
			}
			else
			{
				return this.ApplyToColumns(static s => GetMin(s, 1), static (pre, now) => Math.Min(pre, now), double.MaxValue);
			}
		}
		#endregion

		#region IKrylovVector
		void IKrylovVector<DenseMatrix<T>, T>.Scale(T value) => this.Scale(value);

		double IKrylovVector<DenseMatrix<T>, T>.Norm() => this.Norm();

		void IKrylovVector<DenseMatrix<T>, T>.Normalize() => this.Normalize();

		T IKrylovVector<DenseMatrix<T>, T>.Dot(DenseMatrix<T> other)
		{
			if (other is null || !other.IsValid())
				throw new ArgumentNullException(nameof(other));
			if (this.NRows != other.NRows || this.NCols != other.NCols)
				throw new InvalidOperationException(Resources.Parameter.NotSameSize);

			if (this.NRows == this.LeadDim)
			{
				if (other.NRows == other.LeadDim)
				{
					return LAD.Dot(true, this.Storage, 1, other.Storage, 1);
				}
				else if (other.NRows == 1)
				{
					return LAD.Dot(true, this.Storage, 1, other.Storage, checked((int)other.LeadDim));
				}
			}
			else if (this.NRows == 1)
			{
				if (other.NRows == other.LeadDim)
				{
					return LAD.Dot(true, this.Storage, checked((int)this.LeadDim), other.Storage, 1);
				}
				else if (other.NRows == 1)
				{
					return LAD.Dot(true, this.Storage, checked((int)this.LeadDim), other.Storage, checked((int)other.LeadDim));
				}
			}
			// else
			dynamic dotSquare = default(T);
			long rows = this.NRows, cols = this.NCols, ldA = this.LeadDim, ldB = other.LeadDim;
			for (long i = 0; i < cols; i++)
			{
				var columnA = this.Storage.MakeReference(i * ldA, newLength: rows);
				var columnB = other.Storage.MakeReference(i * ldB, newLength: rows);
				T dot = LAD.Dot(true, columnA, 1, columnB, 1);
				dotSquare += (dynamic)dot * dot;
			}
			return ((T)dotSquare).GenericSqrt();
		}

		/// <summary>
		/// Add the <paramref name="other"/> (scaling by <paramref name="scalar"/>) matrix to this matrix in-place.
		/// </summary>
		/// <param name="other">The other <see cref="DenseMatrix{T}"/> to add</param>
		/// <param name="scalar">The scalar to be multiplied to <paramref name="other"/> of type <typeparamref name="T"/></param>
		public virtual void AddBy(DenseMatrix<T> other, T scalar) => this.OverwriteByMatricesSum(this, other, Scalars<T>.One, scalar);

		/// <summary>
		/// Replace this matrix's content with the <paramref name="other"/> vector in-place.
		/// </summary>
		/// <param name="other">The other <see cref="DenseMatrix{T}"/> to replace from</param>
		/// <exception cref="ArgumentNullException">If <paramref name="other"/> is null or invalid</exception>
		/// <exception cref="InvalidOperationException">If the replacement cannot be done in-place due to reason(s) such as different sparsities between this and <paramref name="other"/></exception>
		public void ReplaceBy(DenseMatrix<T> other)
		{
			if (other is null || !other.IsValid())
				throw new ArgumentNullException(nameof(other));
			if (this.NRows != other.NRows || this.NCols != other.NCols)
				throw new InvalidOperationException(Resources.Parameter.NotSameSize);

			MEM.MemoryCopy2D(other.Storage, other.LeadDim, this.Storage, this.LeadDim, this.NRows, this.NCols);
		}
		#endregion

		#region equality
		/// <summary>
		/// When implemented by a derived class, get the hash code this dense vector. The default implementation only takes <see cref="ValueArray{T}.Storage"/>'s hash code.
		/// </summary>
		/// <returns>The hash code of <see cref="ValueArray{T}.Storage"/></returns>
		public override int GetHashCode() => HashCode.Combine(this.Storage, this.LeadDim, this.NRows, this.NCols);

		/// <summary>
		/// When implemented by a derived class, check whether this object is equal to another one. The default implementation only compares <see cref="ValueArray{T}.Storage"/>.
		/// </summary>
		/// <param name="obj">The other object to compare with</param>
		/// <returns>True if this == <paramref name="obj"/></returns>
		public override bool Equals(object? obj)
		{
			return obj is DenseMatrix<T> dm && this.LeadDim == dm.LeadDim && this.NRows == dm.NRows && this.NCols == dm.NCols && this.Storage == dm.Storage;
		}
		#endregion

		#region print
		/// <summary>
		/// Print out the vector.
		/// </summary>
		/// <param name="overrideSetting">Override global settings in <see cref="Settings"/></param>
		/// <returns>The detailed string representation</returns>
		public override string Print(PrintSettings? overrideSetting = null)
		{
			string description = this.ToString();
			if (this.Disposed)
				return description;

			var settings = overrideSetting ?? Settings.PrintSetting;

			string detail = ":" + Environment.NewLine;
			// get managed array
			int rows = (int)Math.Min(settings.MatrixRow, this.NRows), cols = (int)Math.Min(settings.MatrixColumn, this.NCols);
			Span<T> managed = (rows * cols).CheckStackLimit<T>() ?? stackalloc T[rows * cols];
			MEM.ToManaged2D(this.Storage, this.LeadDim, rows, cols, managed);
			// to dense vector string
			detail += managed.ToMatrixString(rows, more: this.NCols - cols, precision: settings.Precision);
			if (this.NRows > rows)
				detail += Environment.NewLine + string.Format(Resources.Print.MoreRows, this.NRows - rows);
			return description + detail;
		}
		#endregion

		#region serialization
		/// <summary>
		/// Get all the storages of this array. Only returns the <see cref="ValueArray{T}.Storage"/>.
		/// </summary>
		/// <returns>All the storages of the array as an <see cref="IReadOnlyDictionary{TKey, TValue}"/> of <see cref="string"/> and <see cref="IStorage"/></returns>
		public override IReadOnlyDictionary<string, IStorage> GetPointers() => new Dictionary<string, IStorage>(1) { [StorageName] = this.Storage };

		/// <summary>
		/// The print name of the <see cref="LeadDim"/>
		/// </summary>
		protected const string LeadDimName = @"LeadingDimension";

		/// <summary>
		/// Get other requisite informations for re-constructing the array of that derived class type.
		/// </summary>
		/// <returns>Other requisite informations used to re-construct this array, an empty dictionary.</returns>
		public override IReadOnlyDictionary<string, object> GetOtherInfo() => new Dictionary<string, object>(1) { [LeadDimName] = this.LeadDim };
		#endregion
	}
}
