using System;
using System.Collections.Generic;

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
	/// The concrete symmetric or hermitian dense matrix class with the only <see cref="ValueArray{T}.Storage"/> that refers to the data storage.
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct that implements <see cref="IFormattable"/> and <see cref="IEquatable{T}"/> as the data type</typeparam>
	public sealed class SymmetricDenseMatrix<T> : MatrixBase<T>, IKrylovVector<SymmetricDenseMatrix<T>, T>, IMatrix<T> where T : unmanaged, IFormattable, IEquatable<T>
	{
		#region basic
		/// <summary>
		/// Get the leading dimension (the length in <typeparamref name="T"/> between to consecutive column starting elements) of this dense matrix
		/// </summary>
		public long LeadDim { get; } = 0;

		/// <summary>
		/// Get a <see cref="bool"/> indicating whether this symmetric matrix is hermitian or simply symmetric. For real-typed <typeparamref name="T"/>, this is always false.
		/// </summary>
		public bool Hermitian { get; } = false;

		/// <summary>
		/// Get a <see cref="bool"/> indicating whether this symmetric matrix stores the data at upper triangle or lower triangle
		/// </summary>
		public bool StoredUpper { get; } = true;

		private readonly DenseMatrix<T> m_dense;

		/// <summary>
		/// Create an empty <see cref="SymmetricDenseMatrix{T}"/>
		/// </summary>
		public SymmetricDenseMatrix() : base(Storage<T>.Empty, 0, 0, 0) => this.m_dense = new DenseMatrix<T>();

		/// <summary>
		/// Construct a <see cref="SymmetricDenseMatrix{T}"/> with value array <paramref name="values"/> and size <paramref name="n"/>
		/// </summary>
		/// <param name="values">The value array as a <see cref="Storage{T}"/></param>
		/// <param name="n">The number of rows and columns of this matrix</param>
		/// <param name="leadDim">The leading dimension of this matrix. Default 0 means <paramref name="n"/></param>
		/// <param name="hermitian">Whether this symmetric matrix is hermitian or simply symmetric</param>
		/// <param name="storedUpper">Whether this symmetric matrix stores the data at upper triangle or lower triangle</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="n"/> or <paramref name="leadDim"/> is not positive</exception>
		/// <exception cref="ArgumentException">If <paramref name="leadDim"/> is less than <paramref name="n"/> or the given size exceeds the boundary of <paramref name="values"/></exception>
		public SymmetricDenseMatrix(Storage<T> values, long n, long leadDim = 0, bool hermitian = false, bool storedUpper = true) :
			base(values, n, n, leadDim * (n - 1) + n)
		{
			if (leadDim == 0)
				leadDim = n;
			if (leadDim < 0)
				throw new ArgumentOutOfRangeException(nameof(leadDim), leadDim, Resources.Parameter.MustPositive);
			if (leadDim < n)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(leadDim));

			this.LeadDim = leadDim;
			this.Hermitian = hermitian && default(T).IsComplex(); this.StoredUpper = storedUpper;
			this.m_dense = new DenseMatrix<T>(this.Storage, this.NRows, this.NCols, this.LeadDim);
		}
		#endregion

		#region normal dense matrix conversions
		/// <summary>
		/// Copy the stored upper or the lower part to the other part according to <see cref="StoredUpper"/> in-place to make this matrix a normal one, just like <see cref="DenseMatrix{T}"/>
		/// </summary>
		/// <returns>The referenced <see cref="DenseMatrix{T}"/> of this matrix after the copy</returns>
		public DenseMatrix<T> ToNormal()
		{
			LAD.MatrixCopyUpperLowerParts(this.StoredUpper, this.Hermitian, this.NRows, this.Storage, this.LeadDim);
			return this.m_dense;
		}

		/// <summary>
		/// Overwrite this symmetric dense matrix using a given <paramref name="normal"/> dense matrix
		/// </summary>
		/// <param name="normal">The normal <see cref="DenseMatrix{T}"/> used to get </param>
		/// <param name="positiveDefinite">Whether this matrix shall be a positive definite one after exit or simply symmetric / hermitian</param>
		/// <param name="op">The simple operation to apply to <paramref name="normal"/> before the calculation as a <see cref="MatrixOperation"/></param>
		/// <remarks><list type="table">
		///  <listheader><term>(<paramref name="positiveDefinite"/>, <see cref="Hermitian"/>)</term>  <description>Actual Operation</description></listheader>
		/// <item><term>(false, false)</term>  <description>0.5 * (<paramref name="normal"/> + <paramref name="normal"/>^T)</description></item>
		/// <item><term>(false, true)</term>  <description>0.5 * (<paramref name="normal"/> + <paramref name="normal"/>^H)</description></item>
		/// <item><term>(true, false)</term>  <description>(<paramref name="normal"/> * <paramref name="normal"/>^T)</description></item>
		/// <item><term>(true, true)</term>  <description>(<paramref name="normal"/> * <paramref name="normal"/>^H)</description></item>
		/// </list></remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="normal"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="normal"/> is not a square matrix when <paramref name="positiveDefinite"/> is false; or <paramref name="normal"/> has incompatible size</exception>
		public void FromNormal(DenseMatrix<T> normal, bool positiveDefinite = false, MatrixOperation op = MatrixOperation.None)
		{
			if (normal is null || !normal.IsValid())
				throw new ArgumentNullException(nameof(normal));
			if (!positiveDefinite && normal.NRows != normal.NCols)
				throw new ArgumentException(Resources.Other.MatrixSquare, nameof(normal));
			if ((op.CanInPlace() ? normal.NRows : normal.NCols) != this.NRows)
				throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(normal));

			if (normal is SymmetricDenseMatrix<T> symm)
			{
				if (this.Hermitian == symm.Hermitian && this.StoredUpper == symm.StoredUpper && !positiveDefinite)
				{
					MEM.MemoryCopy2D(symm.Storage, symm.LeadDim, this.Storage, this.LeadDim, this.NRows, this.NRows);
					return;
				}
				// else
				symm.ToNormal();
			}
			if (positiveDefinite)
			{
				LAD.RankKUpdate(this.StoredUpper, op, this.Hermitian, this.NRows, op.CanInPlace() ? normal.NCols : normal.NRows,
								Scalars<T>.One, normal.Storage, normal.LeadDim,
								Scalars<T>.Zero, this.Storage, this.LeadDim);
			}
			else
			{
				LAD.GeneralMatricesAdd(op, op.Transpose(), this.NRows, this.NRows,
									   Scalars<T>.Half, normal.Storage, normal.LeadDim,
									   Scalars<T>.Half, normal.Storage, normal.LeadDim,
									   this.Storage, this.LeadDim);
			}
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
		public override MatrixBase<T> GetSubmatrix(long offsetRow, long countRow, long offsetCol, long countCol)
		{
			this.CheckRange(offsetRow, countRow, offsetCol, countCol);
			if (offsetRow == offsetCol && countRow == countCol)
			{
				return new SymmetricDenseMatrix<T>(this.Storage + ((offsetRow + 1) * this.LeadDim), countRow, this.LeadDim, this.Hermitian, this.StoredUpper);
			}
			else
			{
				return this.ToNormal().GetSubmatrix(offsetRow, countRow, offsetCol, countCol);
			}
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

			if (offsetRow == offsetCol && countRow == countCol &&
				overwrite is SymmetricDenseMatrix<T> symm && this.Hermitian == symm.Hermitian && this.StoredUpper == symm.StoredUpper)
			{
				MEM.MemoryCopy2D(this.Storage + ((offsetRow + 1) * this.LeadDim), this.LeadDim, symm.Storage, symm.LeadDim, countRow, countRow);
			}
			else
			{
				this.ToNormal().GetSubmatrix(offsetRow, countRow, offsetCol, countCol, overwrite);
			}
		}

		/// <summary>
		/// Set a sub-matrix by the row and column starting index (inclusive).
		/// </summary>
		/// <param name="rowStart">The <see cref="long"/> to indicate the starting row index to set</param>
		/// <param name="columnStart">The <see cref="long"/> to indicate the starting column index to set</param>
		/// <param name="value">The <see cref="MatrixBase{T}"/> whose value will overwrite this matrix from (<paramref name="rowStart"/>, <paramref name="columnStart"/>)</param>
		/// <exception cref="ArgumentNullException">If <paramref name="value"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="rowStart"/> or <paramref name="columnStart"/> and <paramref name="value"/>'s <see cref="MatrixBase{T}.NRows"/> or <see cref="MatrixBase{T}.NCols"/> are out of range</exception>
		/// <exception cref="InvalidOperationException">If <paramref name="rowStart"/> != <paramref name="columnStart"/> or <paramref name="value"/> is not a <see cref="SymmetricDenseMatrix{T}"/></exception>
		public override void SetSubmatrix(long rowStart, long columnStart, MatrixBase<T> value)
		{
			if (value is null || !value.IsValid())
				throw new ArgumentNullException(nameof(value));
			this.CheckRange(rowStart, columnStart, value.NRows, value.NCols);

			if (rowStart == columnStart && value is SymmetricDenseMatrix<T> symm && this.Hermitian == symm.Hermitian && this.StoredUpper == symm.StoredUpper)
			{
				MEM.MemoryCopy2D(symm.Storage, symm.LeadDim, this.Storage + ((rowStart + 1) * this.LeadDim), this.LeadDim, symm.NRows, symm.NRows);
			}
			else
				throw new InvalidOperationException();
		}

		/// <summary>
		/// Get or set the element at the given position (<paramref name="x"/>, <paramref name="y"/>)
		/// </summary>
		/// <param name="x">The row position as a <see cref="long"/></param>
		/// <param name="y">The column position as a <see cref="long"/></param>
		/// <returns>The element at position (<paramref name="x"/>, <paramref name="y"/>)</returns>
		public override T this[long x, long y] {
			get {
				bool swapped = false;
				if ((this.StoredUpper && x > y) || (!this.StoredUpper && x < y))
				{
					(x, y) = (y, x); swapped = true;
				}
				T value = MEM.ToManaged(this.Storage + (y * this.LeadDim + x));
				if (this.Hermitian && swapped)
					return value.GenericConjugate();
				else
					return value;
			}
			set {
				if ((this.StoredUpper && x > y) || (!this.StoredUpper && x < y))
				{
					(x, y) = (y, x);
					if (this.Hermitian)
						value = value.GenericConjugate();
				}
				MEM.FromManaged(this.Storage + (y * this.LeadDim + x), value);
			}
		}
		#endregion

		#region diagonal indexer
		/// <summary>
		/// Get the <paramref name="k"/>-th diagonal elements.
		/// </summary>
		/// <param name="k">The diagonal index: 0 for diagonal, 1 for super-diagonal at one above, -1 for sub-diagonal at one below, etc.</param>
		/// <returns>A new <see cref="DenseVector{T}"/> containing the <paramref name="k"/>-th diagonal elements.</returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="k"/> is out of range</exception>
		public override DenseVector<T> GetDiag(long k)
		{
			bool swapped = false;
			if ((this.StoredUpper && k < 0) || (!this.StoredUpper && k > 0))
			{
				k = -k; swapped = true;
			}
			var vector = this.m_dense.GetDiag(k);
			if (this.Hermitian && swapped)
				vector.Conjugate();
			return vector;
		}

		/// <summary>
		/// Get the <paramref name="k"/>-th diagonal elements and write the result to <paramref name="overwrite"/>.
		/// </summary>
		/// <param name="k">The diagonal index: 0 for diagonal, 1 for super-diagonal at one above, -1 for sub-diagonal at one below, etc.</param>
		/// <param name="overwrite">The output <see cref="VectorBase{T}"/> which will contain the <paramref name="k"/>-th diagonal elements at exit</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="k"/> is out of range</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="overwrite"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="overwrite"/> cannot be overwritten</exception>
		public override void GetDiag(long k, VectorBase<T> overwrite)
		{
			bool swapped = false;
			if ((this.StoredUpper && k < 0) || (!this.StoredUpper && k > 0))
			{
				k = -k; swapped = true;
			}
			this.m_dense.GetDiag(k, overwrite);
			if (this.Hermitian && swapped)
				overwrite.Conjugate();
		}

		/// <summary>
		/// Set the <paramref name="k"/>-th diagonal elements to <paramref name="value"/>.
		/// </summary>
		/// <param name="k">The diagonal index: 0 for diagonal, 1 for super-diagonal at one above, -1 for sub-diagonal at one below, etc.</param>
		/// <param name="value">The <paramref name="k"/>-th diagonal elements to set as a <see cref="VectorBase{T}"/></param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="k"/> is out of range</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="value"/> is null or invalid</exception>
		/// <exception cref="NotSupportedException">If <paramref name="value"/> is neither a <see cref="DenseVector{T}"/> nor a <see cref="ISparseVector{T}"/></exception>
		public override void SetDiag(long k, VectorBase<T> value)
		{
			bool conj = false;
			if ((this.StoredUpper && k < 0) || (!this.StoredUpper && k > 0))
			{
				k = -k; conj = this.Hermitian;
			}
			try
			{
				if (conj)
					value.Conjugate();
				this.m_dense.SetDiag(k, value);
			}
			finally
			{
				if (conj)
					value.Conjugate();
			}
		}
		#endregion

		#region clone related
		/// <summary>
		/// Deep clone the matrix. This implementation utilizes <see cref="Storage{T}.Clone"/>.
		/// </summary>
		/// <returns>The cloned vector</returns>
		public override SymmetricDenseMatrix<T> Clone()
		{
			var c = this.Storage.MakeReference(newLength: this.NRows * this.NCols).CreateAlike();
			try
			{
				MEM.MemoryCopy2D(this.Storage, this.LeadDim, c, this.NRows, this.NRows, this.NCols);
				return new SymmetricDenseMatrix<T>(c, this.NRows, this.NRows, this.Hermitian, this.StoredUpper);
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
		/// <returns>The new matrix alike this one</returns>
		public override SymmetricDenseMatrix<T> NewArrayAlike()
		{
			var c = this.Storage.MakeReference(newLength: this.NRows * this.NCols).CreateAlike();
			return new SymmetricDenseMatrix<T>(c, this.NRows, this.NRows, this.Hermitian, this.StoredUpper);
		}

		/// <summary>
		/// Create a new matrix with same properties as this one while the underlying storages are not filled and the data type is changed to <typeparamref name="TOut"/>. This implementation utilizes <see cref="Althea.Storage.StorageFactory{T}"/> of <typeparamref name="TOut"/>.
		/// </summary>
		/// <typeparam name="TOut">Any unmanaged struct as the new data type</typeparam>
		/// <returns>The new matrix alike this one</returns>
		public override SymmetricDenseMatrix<TOut> NewArrayAlike<TOut>()
		{
			var c = this.Storage.MakeReference(newLength: this.NRows * this.NCols).CreateAlike<TOut>();
			return new SymmetricDenseMatrix<TOut>(c, this.NRows, this.NRows, this.Hermitian, this.StoredUpper);
		}
		#endregion

		#region reshape
		/// <summary>
		/// Reshape this array to a vector
		/// </summary>
		/// <returns>The vector reshaped from this array</returns>
		public override DenseVector<T> ToVector() => this.ToNormal().ToVector();

		/// <summary>
		/// Reshape this matrix to a matrix with leading dimension = <paramref name="rows"/>
		/// </summary>
		/// <param name="rows">The number of rows of the target matrix; if <paramref name="rows"/> ≤ 0, it is assumed that leadDim = <c>sqrt(<see cref="AbstractArray{T}.Length"/>)</c>.</param>
		/// <returns>The reshaped matrix, may be this matrix itself</returns>
		/// <exception cref="InvalidOperationException">If the computed new number of rows or columns is 1</exception>
		public override MatrixBase<T> ToMatrix(long rows = 0)
		{
			Span<long> newSize = stackalloc long[2];
			newSize[0] = rows;
			CheckSize(this, newSize);
			if (newSize[0] == this.NRows)
				return this;
			// else
			return this.ToNormal().ToMatrix(newSize[0]);
		}

		/// <summary>
		/// Reshape the array to a tensor with dimensionality = <paramref name="size"/>.
		/// </summary>
		/// <param name="size">The new size/dimensionality with at most one or zero uncertain dimension indicated by a non-positive number.</param>
		/// <returns>The reshaped tensor</returns>
		public override DenseTensor<T> ToTensor(ReadOnlySpan<long> size) => this.ToNormal().ToTensor(size);
		#endregion

		#region linear algebra
		/// <summary>
		/// Create a new <see cref="MatrixBase{T}"/> which is the simple operation result of this matrix under <paramref name="operation"/>.
		/// </summary>
		/// <param name="operation">The input <see cref="MatrixOperation"/> used as the simple operation to be applied</param>
		/// <returns>A new <see cref="MatrixBase{T}"/> as the result of <paramref name="operation"/>(this)</returns>
		public override MatrixBase<T> ApplyOperation(MatrixOperation operation)
		{
			operation = operation.Simplify<T>(this.Hermitian);
			// shortcut
			if (operation == MatrixOperation.None)
				return this.Clone();
			else // MatrixOperation.Conjugate
				return this.ApplyToClone(static c => { c.ToNormal(); LAD.PointWiseConjugate(c.Storage, 1); });
		}

		/// <summary>
		/// Create a new <see cref="MatrixBase{T}"/> which is the point-wise addition result of this matrix the <paramref name="other"/> matrix.
		/// </summary>
		/// <param name="scalarThis">The scalar to multiply to this matrix before addition</param>
		/// <param name="scalarOther">The scalar to multiply to the <paramref name="other"/> matrix before addition</param>
		/// <param name="other">The input right <see cref="MatrixBase{T}"/> to be added</param>
		/// <param name="opThis">The <see cref="MatrixOperation"/> to apply to this matrix before addition</param>
		/// <param name="opOther">The <see cref="MatrixOperation"/> to apply to the <paramref name="other"/> matrix before addition</param>
		/// <returns>A new <see cref="MatrixBase{T}"/> as the result of <c><paramref name="scalarThis"/> * <paramref name="opThis"/>(this) + <paramref name="scalarOther"/> * <paramref name="opOther"/>(<paramref name="other"/>)</c></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="other"/> is null or empty</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="scalarThis"/> or <paramref name="scalarOther"/> is 0</exception>
		/// <exception cref="ArgumentException">If the addition cannot be performed due to incompatible sizes</exception>
		/// <exception cref="NotSupportedException">If <paramref name="other"/> is neither a <see cref="DenseMatrix{T}"/> nor a <see cref="SymmetricDenseMatrix{T}"/> nor a <see cref="ISparseMatrix{T}"/></exception>
		public override MatrixBase<T> AddMatrix(T scalarThis, T scalarOther, MatrixBase<T> other, MatrixOperation opThis = MatrixOperation.None, MatrixOperation opOther = MatrixOperation.None)
		{
			var (m, n) = ((IMatrix<T>)this).CheckAdd(scalarThis, scalarOther, other, ref opThis, ref opOther);
			opThis = opThis.Simplify<T>(this.Hermitian);
			if (other is SymmetricDenseMatrix<T> symm)
			{
				opOther = opOther.Simplify<T>(symm.Hermitian);
				bool upperThis = this.StoredUpper ^ !opThis.CanInPlace(), upperOther = symm.StoredUpper ^ !opOther.CanInPlace();
				if (upperThis == upperOther && this.Hermitian == symm.Hermitian && (!this.Hermitian ||
					(this.Hermitian && opThis == MatrixOperation.None && opOther == MatrixOperation.None)))
				{
					var storageOut = this.Storage.MakeReference(newLength: m * n).CreateAlike();
					try
					{
						LAD.GeneralMatricesAdd(opThis, opOther, m, n,
											   scalarThis, this.Storage, this.LeadDim,
											   scalarOther, symm.Storage, symm.LeadDim,
											   storageOut, m);
						return new SymmetricDenseMatrix<T>(storageOut, m, m, this.Hermitian, upperThis);
					}
					catch (Exception)
					{
						storageOut?.Dispose();
						throw;
					}
				}
				symm.ToNormal();
			}	
			// otherwise
			return this.ToNormal().AddMatrix(scalarThis, scalarOther, other, opThis, opOther);
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
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="scalar"/> is 0</exception>
		/// <exception cref="ArgumentException">If the multiplication cannot be performed due to incompatible sizes</exception>
		/// <exception cref="NotSupportedException">If <paramref name="other"/> is neither a <see cref="DenseMatrix{T}"/> nor a <see cref="SymmetricDenseMatrix{T}"/> nor a <see cref="ISparseMatrix{T}"/></exception>
		public override MatrixBase<T> MultiplyMatrix(T scalar, MatrixBase<T> other, MatrixOperation opThis = MatrixOperation.None, MatrixOperation opOther = MatrixOperation.None)
		{
			var (m, n, k) = ((IMatrix<T>)this).CheckMultiply(scalar, other, ref opThis, ref opOther);
			var storageOut = Storage<T>.Create(this.Storage[0].Location, m * n);
			try
			{
				if (other is SymmetricDenseMatrix<T> symm)
				{
					symm.ToNormal();
					// TODO: op
					LAD.SymmHermMatrixMultiplyGeneral(this.StoredUpper, leftA: true, this.Hermitian, m, n, scalar, this.Storage, this.LeadDim, symm.Storage, symm.LeadDim, Scalars<T>.Zero, storageOut, m);
				}
				else if (other is DenseMatrix<T> dense)
				{
					// TODO: op
					LAD.SymmHermMatrixMultiplyGeneral(this.StoredUpper, leftA: false, this.Hermitian, m, n, scalar, this.Storage, this.LeadDim, dense.Storage, dense.LeadDim, Scalars<T>.Zero, storageOut, m);
				}
				else if (other is ISparseMatrix<T> sparse)
				{
					this.ToNormal();
					LAS.MatrixDenseMultiplySparse(opThis, opOther, m, scalar, this.Storage, this.LeadDim, sparse, Scalars<T>.Zero, storageOut, m);
				}
				else
					throw new NotSupportedException();
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
		/// <exception cref="ArgumentException">If both <paramref name="scalarA"/> and <paramref name="scalarB"/> are 0; or the sizes are incompatible; or both <paramref name="A"/> and <paramref name="B"/> are null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="A"/> is this matrix while <paramref name="opA"/> is not <see cref="MatrixOperation.None"/> or <paramref name="B"/> is this matrix while <paramref name="opB"/> is not <see cref="MatrixOperation.None"/></exception>
		/// <exception cref="NotSupportedException">If <paramref name="A"/> or <paramref name="B"/> is neither a <see cref="DenseMatrix{T}"/> nor a <see cref="SymmetricDenseMatrix{T}"/> nor a <see cref="ISparseMatrix{T}"/></exception>
		public override void OverwriteByMatricesSum(MatrixBase<T>? A, MatrixBase<T>? B, T scalarA = default, T scalarB = default, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			var (m, n, nullA, nullB) = ((IMatrix<T>)this).CheckOverwriteBySum(ref A, ref B, scalarA, scalarB, ref opA, ref opB);

		}


		/// <summary>
		/// Overwrite this matrix with the multiplication of given matrices: <c>this = <paramref name="α"/> * <paramref name="opA"/>(<paramref name="A"/>) * <paramref name="opB"/>(<paramref name="B"/>) + <paramref name="β"/> * this</c>.
		/// </summary>
		/// <param name="α">The scalar to multiply to <paramref name="A"/>, cannot be zero</param>
		/// <param name="β">The scalar to multiply to this matrix, can be zero</param>
		/// <param name="A">The left input dense matrix</param>
		/// <param name="B">The right input dense matrix</param>
		/// <param name="opA">The <see cref="MatrixOperation"/> to apply to the <paramref name="A"/> matrix before multiplication</param>
		/// <param name="opB">The <see cref="MatrixOperation"/> to apply to the <paramref name="B"/> matrix before multiplication</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="α"/> is 0</exception>
		/// <exception cref="ArgumentException">If the sizes are incompatible</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="B"/> is null or invalid</exception>
		/// <exception cref="NotSupportedException">If <paramref name="A"/> or <paramref name="B"/> is neither a <see cref="DenseMatrix{T}"/> nor a <see cref="SymmetricDenseMatrix{T}"/> nor a <see cref="ISparseMatrix{T}"/></exception>
		public virtual void OverwriteByMatricesProduct(T α, MatrixBase<T> A, MatrixBase<T> B, T β = default, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None)
		{
			var (m, n, k) = ((IMatrix<T>)this).CheckOverwriteByProduct(α, A, B, ref opA, ref opB);

		}
		#endregion

		#region point-wise operations
		/// <summary>
		/// Fill this dense matrix's <see cref="Storage"/> with given <paramref name="value"/>. The default implementation utilizes <see cref="MEM.FillWithValue{T}(Storage{T}, T)"/>.
		/// </summary>
		/// <param name="value">The value as <typeparamref name="T"/> to fill</param>
		public override void FillWith(T value) => this.m_dense.FillWith(value);

		/// <summary>
		/// Point-wisely in-place add this dense matrix's <see cref="Storage"/> with given <paramref name="value"/>. The default implementation utilizes <see cref="LAD.PointWiseAddScalar{T}"/>.
		/// </summary>
		/// <param name="value">The scalar as <typeparamref name="T"/> to add</param>
		public override void AddScalar(T value) => this.m_dense.AddScalar(value);

		/// <summary>
		/// Point-wisely in-place multiply this dense matrix's <see cref="Storage"/> with given <paramref name="value"/>. The default implementation utilizes <see cref="LAD.Scale{T}"/>.
		/// </summary>
		/// <param name="value">The scalar as <typeparamref name="T"/> to multiply</param>
		public override void Scale(T value) => this.m_dense.Scale(value);

		/// <summary>
		/// Point-wisely in-place conjugate this dense matrix's <see cref="Storage"/>. The default implementation utilizes <see cref="LAD.PointWiseConjugate{T}"/>.
		/// </summary>
		public override void Conjugate() => this.m_dense.Conjugate();

		/// <summary>
		/// Point-wisely in-place exponent this dense matrix's <see cref="Storage"/> with given <paramref name="power"/>. The default implementation utilizes <see cref="LAD.PointWisePower{T}(Storage{T}, int, double)"/>.
		/// </summary>
		/// <param name="power">The power as a <see cref="double"/></param>
		public override void Power(double power) => this.m_dense.Power(power);

		/// <summary>
		/// Point-wisely in-place exponent this dense matrix's <see cref="Storage"/> with given <paramref name="power"/>. The default implementation utilizes <see cref="LAD.PointWisePower{T}(Storage{T}, int, T)"/>.
		/// </summary>
		/// <param name="power">The power as a <typeparamref name="T"/></param>
		public override void Power(T power) => this.m_dense.Power(power);

		/// <summary>
		/// Point-wisely in-place truncate this dense matrix's <see cref="Storage"/> by comparing with given <paramref name="threshold"/>. The default implementation utilizes <see cref="LAD.PointWisePower{T}(Storage{T}, int, T)"/>.
		/// </summary>
		/// <param name="threshold">The threshold as a <see cref="double"/>. Any element in <see cref="Storage"/> whose absolute value ≤ <paramref name="threshold"/> will be set to 0.</param>
		public override void Truncate(double threshold) => this.m_dense.Truncate(threshold);
		#endregion

		#region aggregate operations
		/// <summary>
		/// Aggregately sum the elements in this array. The default implementation only sums <see cref="Storage"/> and utilizes <see cref="LAD.AggregateSum{T}"/>.
		/// </summary>
		/// <returns>The aggregate sum of this array</returns>
		public override T Sum() => this.ToNormal().Sum();

		/// <summary>
		/// Aggregately sum the absolute values of elements in this array. The default implementation only sums <see cref="Storage"/> and utilizes <see cref="LAD.AbsoluteValueSum{T}"/>.
		/// </summary>
		/// <returns>The aggregate sum of absolute values of this array</returns>
		public override double AbsSum() => this.ToNormal().AbsSum();

		/// <summary>
		/// Compute the 2-norm (Euclidean norm) of elements in this array. The default implementation only sums <see cref="Storage"/> and utilizes <see cref="LAD.Norm{T}"/>.
		/// </summary>
		/// <returns>The 2-norm of this array</returns>
		public override double Norm() => this.ToNormal().Norm();

		/// <summary>
		/// Get the maximum one of all absolute values of the elements in this array. The default implementation only get the maximum absolute value of <see cref="Storage"/>. The default implementation utilizes <see cref="LAD.AbsoluteValueArgMax{T}"/>.
		/// </summary>
		/// <returns>The maximum one of all absolute values of the elements in this array</returns>
		public override double AbsMax() => this.ToNormal().AbsMax();

		/// <summary>
		/// Get the minimum one of all absolute values of the elements in this array. The default implementation only get the maximum absolute value of <see cref="Storage"/> and utilizes <see cref="LAD.AbsoluteValueArgMin{T}"/>.
		/// </summary>
		/// <returns>The minimum one of all absolute values of the elements in this array</returns>
		public override double AbsMin() => this.ToNormal().AbsMin();
		#endregion

		#region IKrylovVector
		T IKrylovVector<SymmetricDenseMatrix<T>, T>.Dot(SymmetricDenseMatrix<T> other) => this.ToNormal().Dot(other.ToNormal());

		void IKrylovVector<SymmetricDenseMatrix<T>, T>.AddBy(SymmetricDenseMatrix<T> other, T scalar) => this.OverwriteByMatricesSum(this, other, Scalars<T>.One, scalar);

		/// <summary>
		/// Replace this matrix's content with the <paramref name="other"/> vector in-place.
		/// </summary>
		/// <param name="other">The other <see cref="SymmetricDenseMatrix{T}"/> to replace from</param>
		/// <exception cref="ArgumentNullException">If <paramref name="other"/> is null or invalid</exception>
		/// <exception cref="InvalidOperationException">If the replacement cannot be done in-place due to reason(s) such as different sparsities between this and <paramref name="other"/></exception>
		public void ReplaceBy(SymmetricDenseMatrix<T> other)
		{
			if (other is null || !other.IsValid())
				throw new ArgumentNullException(nameof(other));
			if (this.NRows != other.NRows || this.NCols != other.NCols || this.StoredUpper != other.StoredUpper || this.Hermitian != other.Hermitian)
				throw new InvalidOperationException(Resources.Parameter.NotSameSize);

			MEM.MemoryCopy2D(other.Storage, other.LeadDim, this.Storage, this.LeadDim, this.NRows, this.NCols);
		}
		#endregion

		#region equality
		/// <summary>
		/// Get the hash code this symmetric dense matrix. The default implementation only takes the hash codes of <see cref="DenseMatrix{T}.GetHashCode"/>, <see cref="Hermitian"/> and <see cref="StoredUpper"/>.
		/// </summary>
		/// <returns>The hash code of <see cref="DenseMatrix{T}.GetHashCode"/>, <see cref="Hermitian"/> and <see cref="StoredUpper"/></returns>
		public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), this.Hermitian, this.StoredUpper);

		/// <summary>
		/// Check whether this object is equal to another one. The default implementation only compares <see cref="ValueArray{T}.Storage"/>.
		/// </summary>
		/// <param name="obj">The other object to compare with</param>
		/// <returns>True if this == <paramref name="obj"/></returns>
		public override bool Equals(object? obj)
		{
			return obj is SymmetricDenseMatrix<T> dm && base.Equals(dm) && this.Hermitian == dm.Hermitian && this.StoredUpper == dm.StoredUpper;
		}
		#endregion

		#region print
		/// <summary>
		/// Print out this symmetric dense matrix.
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
			int n = (int)Math.Min(Math.Min(settings.MatrixRow, settings.MatrixColumn), this.NRows);
			Span<T> managed = (n * n).CheckStackLimit<T>() ?? stackalloc T[n * n];
			MEM.ToManaged2D(this.Storage, this.LeadDim, n, n, managed);
			// copy
			if (this.StoredUpper && this.Hermitian)
			{
				for (int i = 0; i < n; i++)
				{
					for (int j = i + 1; j < n; j++)
					{
						managed[i * n + j] = managed[i + j * n].GenericConjugate();
					}
				}
			}
			else if (this.StoredUpper && !this.Hermitian)
			{
				for (int i = 0; i < n; i++)
				{
					for (int j = i + 1; j < n; j++)
					{
						managed[i * n + j] = managed[i + j * n];
					}
				}
			}
			else if (!this.StoredUpper && this.Hermitian)
			{
				for (int i = 0; i < n; i++)
				{
					for (int j = i + 1; j < n; j++)
					{
						managed[i + j * n] = managed[i * n + j].GenericConjugate();
					}
				}
			}
			else
			{
				for (int i = 0; i < n; i++)
				{
					for (int j = i + 1; j < n; j++)
					{
						managed[i + j * n] = managed[i * n + j];
					}
				}
			}
			// to dense matrix string
			detail += managed.ToMatrixString(n, more: this.NCols - n, precision: settings.Precision);
			if (this.NRows > n)
				detail += Environment.NewLine + string.Format(Resources.Print.MoreRows, this.NRows - n);
			return description + detail;
		}
		#endregion

		#region serialization
		internal const string LeadDimName = @"LeadingDimension";

		internal const string HermitianName = nameof(Hermitian);

		internal const string StoredUpperName = nameof(StoredUpper);

		/// <summary>
		/// Get all the storages of this array. Only returns the <see cref="ValueArray{T}.Storage"/>.
		/// </summary>
		/// <returns>All the storages of the array as an <see cref="IReadOnlyDictionary{TKey, TValue}"/> of <see cref="string"/> and <see cref="IStorage"/></returns>
		public override IReadOnlyDictionary<string, IStorage> GetStorages() => new Dictionary<string, IStorage>(1) { [StorageName] = this.Storage };

		/// <summary>
		/// Get other requisite informations for re-constructing the array of that derived class type.
		/// </summary>
		/// <returns>Other requisite informations used to re-construct this array, an empty dictionary.</returns>
		public override IReadOnlyDictionary<string, object> GetMetaData() => new Dictionary<string, object>(3)
		{
			[LeadDimName] = this.LeadDim,
			[HermitianName] = this.Hermitian,
			[StoredUpperName] = this.StoredUpper
		};
		#endregion
	}
}
