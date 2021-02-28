using System;
using System.Runtime.CompilerServices;

using Althea.Linq;
using Althea.Helpers;
using Althea.NativeTypes;
using Althea.LinearAlgebra;


namespace Althea.Arrays
{
	/// <summary>
	/// The abstract matrix class with the only mutable <see cref="ValueArray{T}.Storage"/> that refers to the actual data storage. There may be more pointer(s) for different indices in a sparse vector that inherits <see cref="MatrixBase{T}"/>, but they shall be immutable.
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct that implements <see cref="IFormattable"/> and <see cref="IEquatable{T}"/> as the data type</typeparam>
	public abstract class MatrixBase<T> : ValueArray<T> where T : unmanaged, IFormattable, IEquatable<T>
	{
		#region basic
		/// <summary>
		/// Number of rows of this matrix
		/// </summary>
		public long NRows => this.m_size[0];

		/// <summary>
		/// Number of columns of this matrix
		/// </summary>
		public long NCols => this.m_size[1];

		/// <summary>
		/// Construct a <see cref="MatrixBase{T}"/> with value array <paramref name="values"/> and presenting size <paramref name="rows"/>, <paramref name="cols"/>
		/// </summary>
		/// <param name="values"></param>
		/// <param name="rows">The presenting number of rows of this matrix</param>
		/// <param name="cols">The presenting number of columns of this matrix</param>
		protected MatrixBase(Storage<T> values, long rows, long cols) : base(values, stackalloc long[2].SetValue(rows, cols)) { }
		#endregion

		#region basic indexers
		/// <summary>
		/// When implemented by a derived class, get a sub-matrix by the row and column index ranges.
		/// </summary>
		/// <param name="offsetRow">The starting offset of the row to take</param>
		/// <param name="countRow">The number of the rows to take</param>
		/// <param name="offsetCol">The starting offset of the columns to take</param>
		/// <param name="countCol">The number of the columns to take</param>
		/// <returns>The sub-matrix (may be a referenced one) in the region indicated by the ranges</returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offsetRow"/> or <paramref name="countRow"/> or <paramref name="offsetCol"/> or <paramref name="countCol"/> is out of range</exception>
		public abstract MatrixBase<T> GetSubmatrix(long offsetRow, long countRow, long offsetCol, long countCol);

		/// <summary>
		/// When implemented by a derived class, get a sub-matrix by the row and column index ranges and copy it to <paramref name="overwrite"/>.
		/// </summary>
		/// <param name="offsetRow">The starting offset of the row to take</param>
		/// <param name="countRow">The number of the rows to take</param>
		/// <param name="offsetCol">The starting offset of the columns to take</param>
		/// <param name="countCol">The number of the columns to take</param>
		/// <param name="overwrite">The <see cref="MatrixBase{T}"/> to be overwritten</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offsetRow"/> or <paramref name="countRow"/> or <paramref name="offsetCol"/> or <paramref name="countCol"/> is out of range</exception>
		/// <exception cref="ArgumentException">If <paramref name="overwrite"/> cannot be overwritten</exception>
		public abstract void GetSubmatrix(long offsetRow, long countRow, long offsetCol, long countCol, MatrixBase<T> overwrite);

		/// <summary>
		/// When implemented by a derived class, set a sub-matrix by the row and column starting index (inclusive).
		/// </summary>
		/// <param name="rowStart">The <see cref="long"/> to indicate the starting row index to set</param>
		/// <param name="columnStart">The <see cref="long"/> to indicate the starting column index to set</param>
		/// <param name="value">The <see cref="MatrixBase{T}"/> whose value will overwrite this matrix from (<paramref name="rowStart"/>, <paramref name="columnStart"/>)</param>
		/// <exception cref="ArgumentNullException">If <paramref name="value"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="rowStart"/> or <paramref name="columnStart"/> and <paramref name="value"/>'s <see cref="NRows"/> or <see cref="NCols"/> are out of range</exception>
		public abstract void SetSubmatrix(long rowStart, long columnStart, MatrixBase<T> value);

		/// <summary>
		/// Check the row and column index then return the offset of them.
		/// </summary>
		/// <param name="row">The row <see cref="Index"/></param>
		/// <param name="col">The column <see cref="Index"/></param>
		/// <returns>The row and column offsets</returns>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="row"/> or <paramref name="col"/> is out of range</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected (long row, long col) CheckIndex(Index row, Index col)
		{
			long rowPos = row.GetPosition(this.NRows), colPos = col.GetPosition(this.NCols);
			if (rowPos < 0)
				throw new ArgumentOutOfRangeException(nameof(row), row, Resources.Parameter.CannotNegative);
			if (rowPos >= this.NRows)
				throw new ArgumentOutOfRangeException(nameof(row), row, Resources.Parameter.InvalidValue);
			if (colPos < 0)
				throw new ArgumentOutOfRangeException(nameof(col), col, Resources.Parameter.CannotNegative);
			if (colPos >= this.NCols)
				throw new ArgumentOutOfRangeException(nameof(col), col, Resources.Parameter.InvalidValue);
			return (rowPos, colPos);
		}

		/// <summary>
		/// Check the row and column range
		/// </summary>
		/// <returns>The row and column offsets and lengths</returns>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="offsetRow"/> or <paramref name="countRow"/> or <paramref name="offsetCol"/> or <paramref name="countCol"/> is out of range</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected void CheckRange(long offsetRow, long countRow, long offsetCol, long countCol)
		{
			if (offsetRow < 0)
				throw new ArgumentOutOfRangeException(nameof(offsetRow), offsetRow, Resources.Parameter.CannotNegative);
			if (offsetRow >= this.NRows)
				throw new ArgumentOutOfRangeException(nameof(offsetRow), offsetRow, Resources.Parameter.InvalidValue);
			if (countRow <= 0)
				throw new ArgumentOutOfRangeException(nameof(countRow), countRow, Resources.Parameter.CannotNegative);
			if (countRow + offsetRow > this.NRows)
				throw new ArgumentOutOfRangeException(nameof(countRow), countRow, Resources.Parameter.InvalidValue);

			if (offsetCol < 0)
				throw new ArgumentOutOfRangeException(nameof(offsetCol), offsetCol, Resources.Parameter.CannotNegative);
			if (offsetCol >= this.NCols)
				throw new ArgumentOutOfRangeException(nameof(offsetCol), offsetCol, Resources.Parameter.InvalidValue);
			if (countCol <= 0)
				throw new ArgumentOutOfRangeException(nameof(countCol), countCol, Resources.Parameter.CannotNegative);
			if (countCol + offsetCol > this.NCols)
				throw new ArgumentOutOfRangeException(nameof(countCol), countCol, Resources.Parameter.InvalidValue);
		}


		/// <summary>
		/// Check the row and column range then return the offset/count of them.
		/// </summary>
		/// <param name="row">The row <see cref="Range"/></param>
		/// <param name="col">The column <see cref="Range"/></param>
		/// <returns>The row and column offsets and lengths</returns>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="row"/> or <paramref name="col"/> is out of range</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected (long offsetRow, long countRow, long offsetCol, long countCol) CheckRange(Range row, Range col)
		{
			var (offsetRow, countRow) = row.GetOffsetAndCount(this.NRows);
			var (offsetCol, countCol) = col.GetOffsetAndCount(this.NCols);
			this.CheckRange(offsetRow, countRow, offsetCol, countCol);
			return (offsetRow, countRow, offsetCol, countCol);
		}

		/// <summary>
		/// When implemented by a derived class, get or set the element at the given position (<paramref name="x"/>, <paramref name="y"/>)
		/// </summary>
		/// <param name="x">The row position as a <see cref="long"/></param>
		/// <param name="y">The column position as a <see cref="long"/></param>
		/// <returns>The element at position (<paramref name="x"/>, <paramref name="y"/>)</returns>
		public abstract T this[long x, long y] { get; set; }

		/// <summary>
		/// Get or set the element at the given position (<paramref name="x"/>, <paramref name="y"/>)
		/// </summary>
		/// <param name="x">The row position as an <see cref="Index"/></param>
		/// <param name="y">The column position as an <see cref="Index"/></param>
		/// <returns>The element at position (<paramref name="x"/>, <paramref name="y"/>)</returns>
		public T this[Index x, Index y] {
			get {
				var (row, col) = this.CheckIndex(x, y);
				return this[row, col];
			}
			set {
				var (row, col) = this.CheckIndex(x, y);
				this[row, col] = value;
			}
		}

		/// <summary>
		/// Get or set the sub-matrix indicated by <paramref name="x"/> and <paramref name="y"/>
		/// </summary>
		/// <param name="x">The range of rows to get/set as a <see cref="Range"/></param>
		/// <param name="y">The range of columns to get/set as a <see cref="Range"/></param>
		/// <returns>The sub-matrix in this range, may be a referenced one</returns>
		public MatrixBase<T> this[Range x, Range y] {
			get {
				var (offsetRow, countRow, offsetCol, countCol) = this.CheckRange(x, y);
				return this.GetSubmatrix(offsetRow, countRow, offsetCol, countCol);
			}
			set {
				var (offsetRow, countRow, offsetCol, countCol) = this.CheckRange(x, y);
				if (value.NRows != countRow)
					throw new ArgumentOutOfRangeException(nameof(value), value.NRows, Resources.Parameter.NotSameSize);
				if (value.NCols != countCol)
					throw new ArgumentOutOfRangeException(nameof(value), value.NCols, Resources.Parameter.NotSameSize);
				this.SetSubmatrix(offsetRow, offsetCol, value);
			}
		}
		#endregion

		#region diagonal indexer
		private MatrixDiagonalAccessor<T> m_diagonalAccessor;

		/// <summary>
		/// Get the diagonal element accessor <see cref="MatrixDiagonalAccessor{T}"/> that allows you to get and set diagonal values
		/// </summary>
		/// <exception cref="InvalidOperationException">If this matrix is not a square matrix</exception>
		public virtual MatrixDiagonalAccessor<T> Diagonal {
			get {
				if (this.NRows == this.NCols)
				{
					if (this.m_diagonalAccessor.Equals(default))
						this.m_diagonalAccessor = new MatrixDiagonalAccessor<T>(this);
					return this.m_diagonalAccessor;
				}
				else
				{
					throw new InvalidOperationException(Resources.Other.MatrixSquare);
				}
			}
		}

		/// <summary>
		/// The method to get diagonal elements.
		/// </summary>
		/// <param name="k">diagonal index, 0 for diag, 1 for super-diagonal at one above, -1 for sub-diagonal at one below, etc.</param>
		/// <param name="overwrite">The output <see cref="VectorBase{T}"/> to overwrite, default null means creating a new vector</param>
		/// <returns>A new <see cref="VectorBase{T}"/> containing the (super-/sub-)diagonal elements. If <paramref name="overwrite"/> does not fit, it will not be returned.</returns>
		public abstract VectorBase<T> GetDiag(long k, VectorBase<T> overwrite = null);

		/// <summary>
		/// The method to set diagonal elements.
		/// </summary>
		/// <param name="k">diagonal index, 0 for diag, 1 for super-diagonal at one above, -1 for sub-diagonal at one below, etc.</param>
		/// <param name="vec">The <see cref="VectorBase{T}"/> </param>
		public abstract void SetDiag(long k, VectorBase<T> vec);
		#endregion

		#region defined operators
		/// <summary>
		/// Create a new <see cref="MatrixBase{T}"/> which is the point-wise negation result of the given <paramref name="matrix"/>.
		/// </summary>
		/// <param name="matrix">The input <see cref="MatrixBase{T}"/> whose elements will be used</param>
		/// <returns>A new <see cref="MatrixBase{T}"/> as the negation of <paramref name="matrix"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="matrix"/> is null or empty</exception>
		public static MatrixBase<T> operator -(MatrixBase<T> matrix) => matrix * Scalars<T>.MinusOne;

		/// <summary>
		/// Create a new <see cref="MatrixBase{T}"/> which is the point-wise multiplication result of the given <paramref name="matrix"/> and <paramref name="scalar"/>.
		/// </summary>
		/// <param name="matrix">The input <see cref="MatrixBase{T}"/> whose elements will be used</param>
		/// <param name="scalar">The input scalar used as the multiplier</param>
		/// <returns>A new <see cref="MatrixBase{T}"/> as the result of <paramref name="matrix"/> * <paramref name="scalar"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="matrix"/> is null or empty</exception>
		public static MatrixBase<T> operator *(MatrixBase<T> matrix, T scalar)
		{
			if (matrix is null || !matrix.IsValid())
				throw new ArgumentNullException(nameof(matrix));
			return matrix.ApplyToClone(newA => newA.Scale(scalar));
		}

		/// <summary>
		/// Create a new <see cref="MatrixBase{T}"/> which is the point-wise multiplication result of the given <paramref name="matrix"/> and <paramref name="scalar"/>.
		/// </summary>
		/// <param name="matrix">The input <see cref="MatrixBase{T}"/> whose elements will be used</param>
		/// <param name="scalar">The input scalar used as the multiplier</param>
		/// <returns>A new <see cref="MatrixBase{T}"/> as the result of <paramref name="matrix"/> * <paramref name="scalar"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="matrix"/> is null or empty</exception>
		public static MatrixBase<T> operator *(T scalar, MatrixBase<T> matrix) => matrix * scalar;

		/// <summary>
		/// Create a new <see cref="MatrixBase{T}"/> which is the point-wise division result of the given <paramref name="matrix"/> and <paramref name="scalar"/>.
		/// </summary>
		/// <param name="matrix">The input <see cref="MatrixBase{T}"/> whose elements will be used</param>
		/// <param name="scalar">The input scalar used as the divider</param>
		/// <returns>A new <see cref="MatrixBase{T}"/> as the result of <paramref name="matrix"/> / <paramref name="scalar"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="matrix"/> is null or empty</exception>
		public static MatrixBase<T> operator /(MatrixBase<T> matrix, T scalar) => matrix * scalar.GenericReciprocal();

		/// <summary>
		/// Create a new <see cref="MatrixBase{T}"/> which is the simple operation result of the given <paramref name="matrix"/> under <paramref name="operation"/>.
		/// </summary>
		/// <param name="matrix">The input <see cref="MatrixBase{T}"/> whose elements will be used</param>
		/// <param name="operation">The input <see cref="MatrixOperation"/> used as the operation</param>
		/// <returns>A new <see cref="MatrixBase{T}"/> as the result of <paramref name="operation"/>(<paramref name="matrix"/>)</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="matrix"/> is null or empty</exception>
		public static MatrixBase<T> operator ^(MatrixBase<T> matrix, MatrixOperation operation)
		{
			if (matrix is null || !matrix.IsValid())
				throw new ArgumentNullException(nameof(matrix));

			return matrix.ApplyOperation(operation);
		}

		/// <summary>
		/// Create a new <see cref="MatrixBase{T}"/> which is the point-wise addition result of the given <paramref name="left"/> and <paramref name="right"/> matrices.
		/// </summary>
		/// <param name="left">The input left <see cref="MatrixBase{T}"/> to be added</param>
		/// <param name="right">The input right <see cref="MatrixBase{T}"/> to be added</param>
		/// <returns>A new <see cref="MatrixBase{T}"/> as the result of <paramref name="left"/> + <paramref name="right"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="left"/> or <paramref name="right"/> is null or empty</exception>
		/// <exception cref="ArgumentException">If the addition cannot be performed due to incompatible sizes</exception>
		public static MatrixBase<T> operator +(MatrixBase<T> left, MatrixBase<T> right)
		{
			if (left is null || !left.IsValid())
				throw new ArgumentNullException(nameof(left));
			if (right is null || !right.IsValid())
				throw new ArgumentNullException(nameof(right));

			return left.AddMatrix(Scalars<T>.One, Scalars<T>.One, right);
		}

		/// <summary>
		/// Create a new <see cref="MatrixBase{T}"/> which is the point-wise subtraction result of the given <paramref name="left"/> and <paramref name="right"/> matrices.
		/// </summary>
		/// <param name="left">The input left <see cref="MatrixBase{T}"/> to be subtracted from</param>
		/// <param name="right">The input right <see cref="MatrixBase{T}"/> to subtract</param>
		/// <returns>A new <see cref="MatrixBase{T}"/> as the result of <paramref name="left"/> - <paramref name="right"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="left"/> or <paramref name="right"/> is null or empty</exception>
		/// <exception cref="ArgumentException">If the subtraction cannot be performed due to incompatible sizes</exception>
		public static MatrixBase<T> operator -(MatrixBase<T> left, MatrixBase<T> right)
		{
			if (left is null || !left.IsValid())
				throw new ArgumentNullException(nameof(left));
			if (right is null || !right.IsValid())
				throw new ArgumentNullException(nameof(right));

			return left.AddMatrix(Scalars<T>.One, Scalars<T>.MinusOne, right);
		}

		/// <summary>
		/// Create a new <see cref="MatrixBase{T}"/> which is the matrix multiplication result of the given <paramref name="left"/> and <paramref name="right"/> matrices.
		/// </summary>
		/// <param name="left">The input <see cref="MatrixBase{T}"/> to be multiplied at left</param>
		/// <param name="right">The input <see cref="MatrixBase{T}"/> to be multiplied at right</param>
		/// <returns>A new <see cref="MatrixBase{T}"/> as the result of <paramref name="left"/> * <paramref name="right"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="left"/> or <paramref name="right"/> is null or empty</exception>
		/// <exception cref="ArgumentException">If the multiplication cannot be performed due to incompatible sizes</exception>
		public static MatrixBase<T> operator *(MatrixBase<T> left, MatrixBase<T> right)
		{
			if (left is null || !left.IsValid())
				throw new ArgumentNullException(nameof(left));
			if (right is null || !right.IsValid())
				throw new ArgumentNullException(nameof(right));

			return new DenseMatrix<T>().AddMatricesMultiplication(Scalars<T>.One, left, right);
		}
		#endregion

		#region linear algebra
		/// <summary>
		/// Create a new <see cref="MatrixBase{T}"/> which is the simple operation result of this matrix under <paramref name="operation"/>.
		/// </summary>
		/// <param name="operation">The input <see cref="MatrixOperation"/> used as the simple operation to be applied</param>
		/// <returns>A new <see cref="MatrixBase{T}"/> as the result of <paramref name="operation"/>(this)</returns>
		/// <exception cref="NotSupportedException">If the given <paramref name="operation"/> is not supported</exception>
		public abstract MatrixBase<T> ApplyOperation(MatrixOperation operation);

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
		/// <exception cref="NotSupportedException">If the given <paramref name="opThis"/> or <paramref name="opOther"/> is not supported</exception>
		/// <exception cref="ArgumentOutOfRangeException">If both <paramref name="scalarThis"/> and <paramref name="opOther"/> are 0</exception>
		/// <exception cref="ArgumentException">If the addition cannot be performed due to incompatible sizes</exception>
		public abstract MatrixBase<T> AddMatrix(T scalarThis, T scalarOther, MatrixBase<T> other, MatrixOperation opThis = MatrixOperation.None, MatrixOperation opOther = MatrixOperation.None);

		/// <summary>
		/// Create a new <see cref="MatrixBase{T}"/> which is the addition result of this matrix and the multiplication the given <paramref name="otherLeft"/> and <paramref name="opRight"/> matrices.
		/// </summary>
		/// <param name="scalarOther">The scalar to multiply to the matrix multiplication result before addition</param>
		/// <param name="otherLeft">The input <see cref="MatrixBase{T}"/> to be multiplied at left</param>
		/// <param name="otherRight">The input <see cref="MatrixBase{T}"/> to be multiplied at right</param>
		/// <param name="scalarThis">The scalar to multiply to this matrix before addition. If <paramref name="scalarThis"/> is 0, this matrix can be invalid.</param>
		/// <param name="opLeft">The <see cref="MatrixOperation"/> to apply to <paramref name="otherLeft"/> before multiplication</param>
		/// <param name="opRight">The <see cref="MatrixOperation"/> to apply to <paramref name="otherRight"/> before multiplication</param>
		/// <returns>A new <see cref="MatrixBase{T}"/> as the result of <c><paramref name="scalarThis"/> * this + <paramref name="scalarOther"/> * <paramref name="opLeft"/>(<paramref name="otherLeft"/>) * <paramref name="opRight"/>(<paramref name="otherRight"/>)</c></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="otherLeft"/> or <paramref name="otherRight"/> is null or empty</exception>
		/// <exception cref="NotSupportedException">If the given <paramref name="opLeft"/> or <paramref name="opRight"/> is not supported</exception>
		/// <exception cref="ArgumentOutOfRangeException">If both <paramref name="scalarThis"/> and <paramref name="opRight"/> are 0</exception>
		/// <exception cref="ArgumentException">If the addition cannot be performed due to incompatible sizes</exception>
		public abstract MatrixBase<T> AddMatricesMultiplication(T scalarOther, MatrixBase<T> otherLeft, MatrixBase<T> otherRight, T scalarThis = default, MatrixOperation opLeft = MatrixOperation.None, MatrixOperation opRight = MatrixOperation.None);
		#endregion
	}


	/// <summary>
	/// The wrapper structure used to access diagonal elements of a <see cref="MatrixBase{T}"/>
	/// </summary>
	public readonly struct MatrixDiagonalAccessor<T> : IEquatable<MatrixDiagonalAccessor<T>> where T : unmanaged, IFormattable, IEquatable<T>
	{
		#region basic
		private readonly MatrixBase<T> _owner;

		internal MatrixDiagonalAccessor(MatrixBase<T> o) => _owner = o;

		/// <summary>
		/// The indexer of getting and setting diagonal or sub-diagonal or super-diagonal elements as a whole
		/// </summary>
		/// <param name="k">The diagonal index, 0 for diag, 1 for super-diagonal, -1 for sub-diagonal, etc.</param>
		public VectorBase<T> this[long k] {
			get => _owner.GetDiag(k);
			set => _owner.SetDiag(k, value);
		}
		#endregion

		#region equality
		/// <summary>
		/// Equality operator
		/// </summary>
		public static bool operator ==(MatrixDiagonalAccessor<T> a, MatrixDiagonalAccessor<T> b) => a.Equals(b);

		/// <summary>
		/// Inequality operator
		/// </summary>
		public static bool operator !=(MatrixDiagonalAccessor<T> a, MatrixDiagonalAccessor<T> b) => !a.Equals(b);

		/// <summary>
		/// Override <see cref="object.Equals(object)"/>
		/// </summary>
		public override bool Equals(object? obj) => (obj is MatrixDiagonalAccessor<T> d) && (d._owner == this._owner);

		/// <summary>
		/// Override <see cref="IEquatable{T}.Equals(T)"/>
		/// </summary>
		public bool Equals(MatrixDiagonalAccessor<T> other) => other._owner == this._owner;

		/// <summary>
		/// Get the hash code of this structure
		/// </summary>
		/// <returns>The hash code of this structure</returns>
		public override int GetHashCode() => this._owner.GetHashCode();
		#endregion
	}
}