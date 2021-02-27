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
		/// The matrix negation operator that .
		/// </summary>
		/// <param name="A">input <see cref="MatrixBase{T}"/>, will be overwritten if it is in-place</param>
		/// <returns>The negation <see cref="MatrixBase{T}"/></returns>
		public static MatrixBase<T> operator -(MatrixBase<T> A) => A * Scalars<T>.MinusOne;

		/// <summary>
		/// Matrix scaling.
		/// </summary>
		/// <param name="A">input <see cref="MatrixBase{T}"/>, will be overwritten if it is in-place</param>
		/// <param name="α">input scalar of type <typeparamref name="T"/></param>
		/// <returns>output <see cref="MatrixBase{T}"/> C</returns>
		/// <remarks>This operator is implemented by the <see cref="VectorBase{T}.Scale(T)"/> rather than a dedicate abstract operation in <see cref="MatrixBase{T}"/>.</remarks>
		public static MatrixBase<T> operator *(MatrixBase<T> A, T α)
		{
			if (A is null || !A.IsValid())
				throw new ArgumentNullException(nameof(A));
			return A.ApplyToClone(newA => newA.Scale(α));
		}

		/// <summary>
		/// Matrix scaling.
		/// </summary>
		/// <param name="A">input <see cref="MatrixBase{T}"/>, will be overwritten if it is in-place</param>
		/// <param name="α">input scalar of type <typeparamref name="T"/></param>
		/// <returns>output <see cref="MatrixBase{T}"/> C</returns>
		/// <remarks>This operator is implemented by the <see cref="VectorBase{T}.Scale(T)"/> rather than a dedicate abstract operation in <see cref="MatrixBase{T}"/>.</remarks>
		public static MatrixBase<T> operator *(T α, MatrixBase<T> A) => A * α;

		/// <summary>
		/// Matrix number multiply out-of-place, i.e. $C = \frac{1}{\alpha} A$.
		/// </summary>
		/// <param name="A">input <see cref="MatrixBase{T}"/></param>
		/// <param name="α">input scalar of type <typeparamref name="T"/></param>
		/// <returns>output <see cref="MatrixBase{T}"/> C</returns>
		/// <remarks>This operator is implemented by the <see cref="VectorBase{T}.Scale(T)"/> rather than a dedicate abstract operation in <see cref="MatrixBase{T}"/>.</remarks>
		public static MatrixBase<T> operator /(MatrixBase<T> A, T α) => A * α.GenericReciprocal();

		/// <summary>
		/// Matrix Transpose, conjugate and conjugate transpose, <b>out-of-place</b>.
		/// </summary>
		/// <param name="M">input <see cref="MatrixBase{T}"/></param>
		/// <param name="op">The <see cref="PowerOperation"/></param>
		/// <returns>a <see cref="MatrixBase{T}"/> after the <paramref name="op"/></returns>
		/// <remarks>If the result matrix is itself, this matrix will directly be returned where no new matrix will be created.</remarks>
		public static MatrixBase<T> operator ^(MatrixBase<T> M, PowerOperation op)
		{
			if (M is null || !M.IsValid())
				throw new ArgumentNullException(nameof(M));

		}

		/// <summary>
		/// Addition of two matrices, <b>out-of-place</b>.
		/// </summary>
		/// <param name="A">input <see cref="MatrixBase{T}"/> A</param>
		/// <param name="B">input <see cref="MatrixBase{T}"/> B</param>
		/// <returns>output <see cref="MatrixBase{T}"/> C</returns>
		public static MatrixBase<T> operator +(MatrixBase<T> A, MatrixBase<T> B)
		{
			if (A is null || !A.IsValid())
				throw new ArgumentNullException(nameof(A));
			if (B is null || !B.IsValid())
				throw new ArgumentNullException(nameof(B));

		}

		/// <summary>
		/// Subtraction of two matrices, <b>out-of-place</b>.
		/// </summary>
		/// <param name="A">input <see cref="MatrixBase{T}"/> A</param>
		/// <param name="B">input <see cref="MatrixBase{T}"/> B</param>
		/// <returns>output <see cref="MatrixBase{T}"/> C</returns>
		public static MatrixBase<T> operator -(MatrixBase<T> A, MatrixBase<T> B)
		{
			if (A is null || !A.IsValid())
				throw new ArgumentNullException(nameof(A));
			if (B is null || !B.IsValid())
				throw new ArgumentNullException(nameof(B));

		}

		/// <summary>
		/// Multiply two matrices, <b>out-of-place</b>.
		/// </summary>
		/// <param name="A">input <see cref="MatrixBase{T}"/> A</param>
		/// <param name="B">input <see cref="MatrixBase{T}"/> B</param>
		/// <returns>output <see cref="MatrixBase{T}"/> C</returns>
		public static MatrixBase<T> operator *(MatrixBase<T> A, MatrixBase<T> B)
		{
			if (A is null || !A.IsValid())
				throw new ArgumentNullException(nameof(A));
			if (B is null || !B.IsValid())
				throw new ArgumentNullException(nameof(B));

		}
		#endregion

	}


	/// <summary>
	/// The diagonal element access class
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