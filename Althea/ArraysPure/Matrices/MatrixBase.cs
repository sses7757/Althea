using System;
using System.Runtime.CompilerServices;

using Althea.Memory;
using BLAS = Althea.Blas.API;
using SOLVE = Althea.Solver.API;


namespace Althea.Arrays
{
	#region enumerates
	/// <summary>
	/// The algorithm used to prune a dense matrix, the priorities of default algorithms are the corresponding <see cref="int"/> representations.
	/// </summary>
	public enum DenseMatrixToSparseAlgorithm
	{
		/// <summary>
		/// Default algorithm when the threshold is 0, which takes a two-step approach:
		/// <list type="number">
		/// <item><description>directly convert the <see cref="DenseMatrix{T}"/> to a <see cref="SparseMatrixFormat.Compressed"/> <see cref="SparseMatrix{T}"/>;</description></item>
		/// <item><description>prune the converted <see cref="SparseMatrix{T}"/> to get a new one with less non-zero elements (not executed if threshold is 0).</description></item>
		/// </list>
		/// This is more efficient when the second step is not executed, even when the target format is <see cref="SparseMatrixFormat.Coordinated"/> since the conversion from <see cref="SparseMatrixFormat.Compressed"/> to <see cref="SparseMatrixFormat.Coordinated"/> is both time and memory efficient.
		/// </summary>
		ZeroThresholdDefault,
		/// <summary>
		/// Default algorithm when data type is a real type, which utilizes the CUDA Sparse API only for real-typed <see cref="SparseMatrixFormat.CSR"/> matrix. Although other formats are supported by applying the transposed dense matrix first (for <see cref="SparseMatrixFormat.CSC"/>) or convert at last (for <see cref="SparseMatrixFormat.Coordinated"/>), it may slows the process.
		/// </summary>
		RealDefault,
		/// <summary>
		/// Default algorithm when the threshold is larger than 0, which takes a two-step approach:
		/// <list type="number">
		/// <item><description>prune the <see cref="DenseMatrix{T}"/> using <see cref="BLAS.Truncate{T}"/>;</description></item>
		/// <item><description>apply the <see cref="ZeroThresholdDefault"/> algorithm to get the <see cref="SparseMatrix{T}"/> in target format.</description></item>
		/// </list>
		/// This is more time efficient than <see cref="ZeroThresholdDefault"/> but less memory efficient than it when the threshold is not zero.
		/// </summary>
		NonzeroThresholdDefault,
		/// <summary>
		/// Algorithm that takes a three-step approach and utilizes the <see cref="DenseVector{T}.ToSparse(float)"/> and <see cref="SparseVector{T}.ToMatrix(long)"/> method:
		/// <list type="number">
		/// <item><description>flatten the <see cref="DenseMatrix{T}"/> to a <see cref="DenseVector{T}"/> which takes almost no time and memory space;</description></item>
		/// <item><description>prune the <see cref="DenseVector{T}"/> to get a <see cref="SparseVector{T}"/>;</description></item>
		/// <item><description>convert the index array of <see cref="SparseVector{T}"/> to the row and column index arrays by mod and quotient, which is the <see cref="SparseMatrixFormat.Coordinated"/>.</description></item>
		/// </list>
		/// The extra memory consumption of this algorithm is three times the size of the <see cref="DenseMatrix{T}"/>. The performance in time is not tested yet. This may be more efficient when the shape of <see cref="DenseMatrix{T}"/> is far from square.
		/// </summary>
		ViaVector
	}

	/// <summary>
	/// The algorithm used to convert a sparse matrix to dense one.
	/// </summary>
	public enum SparseMatrixToDenseAlgorithm
	{
		/// <summary>
		/// The default algorithm that utilizes the API in CUDA sparse for CSR / CSC formats. The conversion of COO format is done by converting to CSR format first.
		/// </summary>
		Default = 0,
		/// <summary>
		/// The algorithm that is only for COO format matrix which takes a three-step approach:
		/// <list type="number">
		/// <item><description>flatten the <see cref="SparseMatrix{T}"/> to a <see cref="SparseVector{T}"/> which takes some time and the same extra memory space as the index arrays;</description></item>
		/// <item><description>convert the <see cref="SparseVector{T}"/> to a <see cref="DenseVector{T}"/>;</description></item>
		/// <item><description>reshape the <see cref="DenseVector{T}"/> back to <see cref="DenseMatrix{T}"/> which takes almost no time and memory space.</description></item>
		/// </list>
		/// </summary>
		ViaVector = 1
	}
	#endregion


	/// <summary>
	/// The abstract matrix class that inherit the <see cref="PureArray{T}"/>.
	/// </summary>
	/// <typeparam name="T">the supported data types are <see cref="float"/>, <see cref="double"/>, <see cref="FloatComplex"/>, <see cref="DoubleComplex"/>; other types of data causes <see cref="NotSupportedException"/></typeparam>
	public abstract class MatrixBase<T> : PureArray<T>, IMatrix<MatrixBase<T>, VectorBase<T>, T> where T : struct, IComparable<T>
	{
		#region new members (mostly from IMatrix<TMat, TVec, T>)
		/// <summary>
		/// For real arrays, this is equivalent to symmetric
		/// </summary>
		public bool Hermitian { get; protected internal set; }

		/// <summary>
		/// Number of rows of this matrix
		/// </summary>
		public long NRows => Size[0];

		/// <summary>
		/// Number of columns of this matrix
		/// </summary>
		public long NCols => Size[1];

		internal int IntRows => checked((int)NRows);
		internal int IntCols => checked((int)NCols);
		#endregion


		#region initialize and destroy
		/// <summary>
		/// Null constructor
		/// </summary>
		internal MatrixBase() : base(null, new long[] { 0, 0 }) { }

		/// <summary>
		/// Abstract matrix data constructor with separate display size and actual memory size
		/// </summary>
		/// <param name="actualLength">actual length of array to allocate on memory</param>
		/// <param name="rows">display the row number</param>
		/// <param name="cols">display column number</param>
		/// <param name="herm">is the matrix hermitian or not</param>
		/// <param name="onHost">allocate on host memory or device memory</param>
		protected MatrixBase(long actualLength, long rows, long cols, bool onHost, bool herm = false) : base(actualLength, new long[] { rows, cols }, onHost)
		{
			this.Hermitian = herm;
			if (rows != cols && herm)
			{
				this.Dispose();
				throw new ArgumentException(Resource.HermMatDim, nameof(rows));
			}
		}

		/// <summary>
		/// Full matrix constructor with pre-allocated values.
		/// </summary>
		/// <param name="values"><see cref="Storage{T}"/> of the value array</param>
		/// <param name="rows">the number of rows</param>
		/// <param name="cols">the number of columns</param>
		/// <param name="herm">is the matrix hermitian or not</param>
		protected MatrixBase(Storage<T> values, long rows, long cols, bool herm = false) : base(values, new[] { rows, cols })
		{
			this.Hermitian = herm;
		}

		/// <summary>
		/// Abstract matrix reshape constructor
		/// </summary>
		/// <param name="refArray">original array</param>
		/// <param name="actualLength">actual length of array</param>
		/// <param name="rows">new number of rows</param>
		/// <param name="cols">new number of columns</param>
		/// <param name="herm">the new matrix is Hermitian or not, if <paramref name="refArray"/> is <see cref="MatrixBase{T}"/>, its <see cref="MatrixBase{T}.Hermitian"/> will be used</param>
		/// <param name="offset">offset to the <see cref="PureArray{T}.Pointer"/> in T rather than bytes</param>
		protected MatrixBase(PureArray<T> refArray, long actualLength, long rows, long cols, bool herm = false, long offset = 0) : base(refArray, actualLength, new[] { rows, cols }, offset)
		{
			if (refArray is MatrixBase<T> m)
				this.Hermitian = rows == cols && m.Hermitian;
			else
				this.Hermitian = herm;
		}
		#endregion


		#region converter
		/// <summary>
		/// Convert this matrix to a <see cref="DenseMatrix{T}"/>. The out-of-place conversion may be performed.
		/// </summary>
		/// <param name="algorithm">the <see cref="SparseMatrixToDenseAlgorithm"/> to use</param>
		/// <returns>Converted dense matrix</returns>
		public abstract DenseMatrix<T> ToDense(SparseMatrixToDenseAlgorithm algorithm = SparseMatrixToDenseAlgorithm.Default);

		/// <summary>
		/// Convert this matrix to a <see cref="SparseMatrix{T}"/>. The out-of-place conversion may be performed.
		/// </summary>
		/// <param name="threshold">values smaller than threshold are regarded as zeros, must be larger than or equal to 0</param>
		/// <param name="targetFormat">the target <see cref="SparseMatrix{T}"/>'s format, see <see cref="SparseMatrixFormat"/></param>
		/// <param name="algorithm">the <see cref="DenseMatrixToSparseAlgorithm"/> to use, default is null which means that the default algorithms corresponding to the <paramref name="targetFormat"/> and <typeparamref name="T"/> will be used</param>
		/// <returns>Converted <see cref="SparseMatrix{T}"/></returns>
		/// <remarks>If this matrix is sparse and this method does not perform any prune.</remarks>
		public abstract SparseMatrix<T> ToSparse(float threshold = default, SparseMatrixFormat targetFormat = SparseMatrixFormat.Compressed, DenseMatrixToSparseAlgorithm? algorithm = null);
		#endregion


		#region abstract methods
		/// <summary>
		/// Fill this matrix with identity.
		/// </summary>
		public abstract void FillWithIdentity();

		/// <summary>
		/// Make this matrix actually Hermitian (if <see cref="Hermitian"/> is true now) by setting the lower half same as upper, from <see cref="IMatrix{T}.CopyUpperToLower"/>.
		/// </summary>
		/// <returns>This matrix that made general (in-place operation)</returns>
		public abstract void CopyUpperToLower();

		/// <summary>
		/// Join the array of <see cref="VectorBase{T}"/> forming into a <see cref="MatrixBase{T}"/> overwriting this matrix.
		/// </summary>
		/// <param name="vectors">the input array of <see cref="VectorBase{T}"/> </param>
		public abstract void FromColumnVectors(VectorBase<T>[] vectors);

		/// <summary>
		/// Get a new matrix by the column index range, from <see cref="IMatrix{TMat, T}.GetColumnRange(Range, TMat)"/>.
		/// </summary>
		/// <param name="columnRange"><see cref="Range"/> of columns</param>
		/// <param name="overwrite">the output <see cref="MatrixBase{T}"/> to overwrite, default null means creating a ref matrix</param>
		/// <returns>A matrix constructed by these columns. If <paramref name="overwrite"/> does not fit, it will not be returned.</returns>
		public abstract MatrixBase<T> GetColumnRange(Range columnRange, MatrixBase<T> overwrite = null);

		/// <summary>
		/// Get a new matrix by the row index range, from <see cref="IMatrix{TMat, T}.GetRowRange(Range, TMat)"/>.
		/// </summary>
		/// <param name="rowRange"><see cref="Range"/> of rows</param>
		/// <param name="overwrite">the output <see cref="MatrixBase{T}"/> to overwrite, default null means creating a ref matrix</param>
		/// <returns>A matrix constructed by these rows. If <paramref name="overwrite"/> does not fit, it will not be returned.</returns>
		public abstract MatrixBase<T> GetRowRange(Range rowRange, MatrixBase<T> overwrite = null);

		/// <summary>
		/// Get a sub-matrix by the row and column index ranges.
		/// </summary>
		/// <param name="rowRange"><see cref="Range"/> of rows</param>
		/// <param name="columnRange"><see cref="Range"/> of columns</param>
		/// <param name="overwrite">the output <see cref="MatrixBase{T}"/> to overwrite, default null means creating a ref matrix (if possible)</param>
		/// <returns>A sub-matrix in this region. If <paramref name="overwrite"/> does not fit, it will not be returned.</returns>
		public abstract MatrixBase<T> GetSubmatrix(Range rowRange, Range columnRange, MatrixBase<T> overwrite = null);

		/// <summary>
		/// Get part of the column vectors that forms the matrix, from <see cref="IMatrix{TMat, TVec, T}.GetColumns(Range, TVec[])"/>.
		/// </summary>
		/// <param name="colRange">the <see cref="Range"/> of columns</param>
		/// <param name="overwrite">the output array of <see cref="VectorBase{T}"/> to overwrite, default null means creating ref vectors if possible</param>
		/// <returns>An array of data type <see cref="VectorBase{T}"/>. If <paramref name="overwrite"/> does not fit, it will not be returned.</returns>
		public abstract VectorBase<T>[] GetColumns(Range colRange, VectorBase<T>[] overwrite = null);

		/// <summary>
		/// Get the all the column vectors that forms the matrix, from <see cref="IMatrix{TMat, TVec, T}.GetColumns(TVec[])"/>.
		/// </summary>
		/// <param name="overwrite">the output array of <see cref="VectorBase{T}"/> to overwrite, default null means creating ref vectors if possible</param>
		/// <returns>An array of data type <see cref="VectorBase{T}"/>. If <paramref name="overwrite"/> does not fit, it will not be returned.</returns>
		public virtual VectorBase<T>[] GetColumns(VectorBase<T>[] overwrite = null) => this.GetColumns(.., overwrite);

		/// <summary>
		/// Get part of the row vectors that forms the matrix, from <see cref="IMatrix{TMat, TVec, T}.GetRows(Range, TVec[])"/>.
		/// </summary>
		/// <param name="rowRange">the <see cref="Range"/> of rows</param>
		/// <param name="overwrite">the output array of <see cref="VectorBase{T}"/> to overwrite, default null means creating ref vectors if possible</param>
		/// <returns>An array of data type <see cref="VectorBase{T}"/>. If <paramref name="overwrite"/> does not fit, it will not be returned.</returns>
		public abstract VectorBase<T>[] GetRows(Range rowRange, VectorBase<T>[] overwrite = null);

		/// <summary>
		/// Get the all the row vectors that forms the matrix, from <see cref="IMatrix{TMat, TVec, T}.GetRows(TVec[])"/>.
		/// </summary>
		/// <param name="overwrite">the output array of <see cref="VectorBase{T}"/> to overwrite, default null means creating ref vectors if possible</param>
		/// <returns>An array of data type <see cref="VectorBase{T}"/>. If <paramref name="overwrite"/> does not fit, it will not be returned.</returns>
		public virtual VectorBase<T>[] GetRows(VectorBase<T>[] overwrite = null) => this.GetRows(.., overwrite); // this.GetRows(Range.All);

		/// <summary>
		/// Get one column of the matrix, from <see cref="IMatrix{TMat, TVec, T}.GetColumnAt(Index, TVec)"/>.
		/// </summary>
		/// <param name="index">the <see cref="Index"/> of column</param>
		/// <param name="overwrite">the output <see cref="VectorBase{T}"/> to overwrite, default null means creating a ref vector if possible</param>
		/// <returns>The selected column as a <see cref="VectorBase{T}"/>. If <paramref name="overwrite"/> does not fit, it will not be returned.</returns>
		public abstract VectorBase<T> GetColumnAt(Index index, VectorBase<T> overwrite = null);

		/// <summary>
		/// Get one row of the matrix, from <see cref="IMatrix{TMat, TVec, T}.GetRowAt(Index, TVec)"/>.
		/// </summary>
		/// <param name="index">the <see cref="Index"/> of row</param>
		/// <param name="overwrite">the output <see cref="VectorBase{T}"/> to overwrite, default null means creating a ref vector if possible</param>
		/// <returns>The selected column as a <see cref="VectorBase{T}"/>. If <paramref name="overwrite"/> does not fit, it will not be returned.</returns>
		public abstract VectorBase<T> GetRowAt(Index index, VectorBase<T> overwrite = null);
		#endregion


		#region abstract operations (mostly from IMatrix<Mat, T>)

		#region basics
		/// <summary>
		/// Scale this vector in-place, i.e. $\vec{v}_{\text{this}} = \alpha \vec{v}_{\text{this}}$.
		/// </summary>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		public virtual void Scale(T α) => this.AsDenseVector().Scale(α);

		/// <summary>
		/// Symmetrize this matrix by adding its conjugate transpose out-of-place.
		/// </summary>
		/// <param name="conjugateAtLast">return the original </param>
		/// <param name="overwrite">the output <see cref="MatrixBase{T}"/> to overwrite, default null means creating a new matrix; note that it cannot always be overwritten</param>
		/// <returns>If <c><paramref name="conjugateAtLast"/> == false</c>: $B_{\text{result}}=\frac{A + A^H}{2}$; otherwise: $B_{\text{result}}=\frac{\bar{A} + A^T}{2}$</returns>
		public virtual MatrixBase<T> Symmetrize(bool conjugateAtLast = false, MatrixBase<T> overwrite = null)
		{
			if (overwrite is null || overwrite == EmptyDnMat)
				return General.ArrayOperations.Symmetrize(this, Scalars<T>.One, conjugateAtLast);

			overwrite.From_αA_Add_βB(this, this, Scalars<T>.Half, Scalars<T>.Half, MatrixOperation.ConjugateTranspose, MatrixOperation.None);
			if (conjugateAtLast && this.IsRealType)
				overwrite.ConjugateInPlace();
			return overwrite;
		}

		/// <summary>
		/// Calculate the transpose of this matrix. A new <see cref="MatrixBase{T}"/> will be created if the result is not it self.
		/// </summary>
		/// <param name="overwrite">the output <see cref="MatrixBase{T}"/> to overwrite, default null means creating a new matrix</param>
		/// <returns>The transposed <see cref="MatrixBase{T}"/>.</returns>
		public abstract MatrixBase<T> Transpose(MatrixBase<T> overwrite = null);

		/// <summary>
		/// Calculate the conjugate transpose of this matrix. A new <see cref="MatrixBase{T}"/> will be created if the result is not it self.
		/// </summary>
		/// <param name="overwrite">the output <see cref="MatrixBase{T}"/> to overwrite, default null means creating a new matrix</param>
		/// <returns>The conjugate-transposed <see cref="MatrixBase{T}"/>.</returns>
		public abstract MatrixBase<T> ConjugateTranspose(MatrixBase<T> overwrite = null);
		#endregion

		#region algebra
		/// <summary>
		/// Compute $C_{\text{this}} = \alpha A^{\text{opA}} + \beta B^{\text{opB}}$ <b>in-place</b>.
		/// </summary>
		/// <param name="α">scalar of type <typeparamref name="T"/> with default 0. If <c><paramref name="α"/> == 0</c>, <paramref name="A"/> can be an invalid input</param>
		/// <param name="A">the <see cref="MatrixBase{T}"/> A, can be null</param>
		/// <param name="opA">operation to matrix <paramref name="A"/></param>
		/// <param name="β">scalar of type <typeparamref name="T"/> with default 0. If <c><paramref name="β"/> == 0</c>, <paramref name="B"/> can be an invalid input</param>
		/// <param name="B">the input <see cref="MatrixBase{T}"/> B, can be null</param>
		/// <param name="opB">operation to matrix <paramref name="B"/></param>
		/// <returns>whether this operation can be done in-place</returns>
		/// <exception cref="ArgumentNullException">if all of the array are null</exception>
		/// <exception cref="ArgumentException">if the arrays do not match in size</exception>
		/// <exception cref="StatusException">if the internal returns error status</exception>
		/// <remarks>If the returned matrix is same as <paramref name="A"/> or <paramref name="B"/>, no new matrix will be created.</remarks>
		public abstract void From_αA_Add_βB(MatrixBase<T> A, MatrixBase<T> B, T α = default, T β = default, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None);

		/// <summary>
		/// Compute $C = \alpha opA(A_{\text{this}}) + \beta B^{\text{opB}}$ (this matrix is <c>A</c>).
		/// </summary>
		/// <param name="α">scalar of type <typeparamref name="T"/> with default 0. If <c><paramref name="α"/> == 0</c>, <c>A</c> can be an invalid input</param>
		/// <param name="C">the output <see cref="MatrixBase{T}"/> C, cannot be null</param>
		/// <param name="opA">operation to matrix <c>A</c></param>
		/// <param name="β">scalar of type <typeparamref name="T"/> with default 0. If <c><paramref name="β"/> == 0</c>, <paramref name="B"/> can be an invalid input</param>
		/// <param name="B">the input <see cref="MatrixBase{T}"/> B, can be null</param>
		/// <param name="opB">operation to matrix <paramref name="B"/></param>
		/// <exception cref="ArgumentNullException">if all of the array are null</exception>
		/// <exception cref="ArgumentException">if the arrays do not match in size</exception>
		/// <exception cref="StatusException">if the internal returns error status</exception>
		/// <remarks>
		/// If the returned matrix is same as <c>A</c> or <paramref name="B"/>, no new matrix will be created and <paramref name="C"/> will not be altered.<br/>
		/// This is the opposite operation of <see cref="From_αA_Add_βB"/>, and is not implemented by built-in matrix classes while is used by them to support other possible matrix classes inherit from <see cref="MatrixBase{T}"/> in operation <see cref="From_αA_Add_βB"/>. Since <c>A</c> and <paramref name="B"/> are symmetric parameters, this method is enough.
		/// </remarks>
		internal protected abstract void From_αA_Add_βB_Opposite(MatrixBase<T> C, MatrixBase<T> B, T α = default, T β = default, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None);

		/// <summary>
		/// Compute $C_{\text{this}} = \alpha A^{\text{opA}} B^{\text{opB}} + \beta C_{\text{this}}$ <b>in-place</b>.
		/// </summary>
		/// <param name="A">the input <see cref="MatrixBase{T}"/> A</param>
		/// <param name="opA">operation to matrix <paramref name="A"/></param>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		/// <param name="B">the input <see cref="MatrixBase{T}"/> B</param>
		/// <param name="opB">operation to matrix <paramref name="B"/></param>
		/// <param name="β">scalar of type <typeparamref name="T"/> with default 0</param>
		/// <returns>whether this operation can be done in-place</returns>
		/// <exception cref="ArgumentNullException">if any of the array is null</exception>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="α"/> is zero</exception>
		/// <exception cref="ArgumentException">if the arrays do not match in size</exception>
		/// <exception cref="StatusException">if the internal calculation returns error status</exception>
		public abstract void Mulβ_AddBy_αAB(MatrixBase<T> A, MatrixBase<T> B, T α, T β = default, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None);

		/// <summary>
		/// Compute $C = \alpha opA(A_{\text{this}}) B^{\text{opB}} + \beta C$.
		/// </summary>
		/// <param name="side">the side of $opA(A_{\text{this}})$</param>
		/// <param name="C">the output <see cref="MatrixBase{T}"/> C</param>
		/// <param name="opA">operation to matrix A</param>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		/// <param name="B">the input <see cref="MatrixBase{T}"/> B</param>
		/// <param name="opB">operation to matrix <paramref name="B"/></param>
		/// <param name="β">scalar of type <typeparamref name="T"/> with default 0</param>
		/// <exception cref="ArgumentNullException">if any of the array is null</exception>
		/// <exception cref="ArgumentException">if the arrays do not match in size</exception>
		/// <exception cref="StatusException">if the internal calculation returns error status</exception>
		/// <remarks>
		/// This is the opposite operation of <see cref="Mulβ_AddBy_αAB"/>, and is not implemented by built-in matrix classes while is used by them to support other possible matrix classes inherit from <see cref="MatrixBase{T}"/> in operation <see cref="Mulβ_AddBy_αAB"/>. Since <c>A</c> and <paramref name="B"/> are (semi-)symmetric parameters, this method is enough.
		/// </remarks>
		protected internal abstract void Mulβ_AddBy_αAB_Opposite(MatrixBase<T> C, MatrixBase<T> B, SideMode side, T α, T β = default, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None);
		#endregion

		#region outer product
		/// <summary>
		/// Compute Kronecker product $A \otimes B$ where $A$ is this matrix. If <paramref name="forceHerm"/> is true, then $(A \otimes B^H + A^H \otimes B)/2$ will be calculated. From <see cref="IMatrix{TMat, T}.KroneckerProd"/>.
		/// </summary>
		/// <param name="B">right <see cref="MatrixBase{T}"/></param>
		/// <param name="forceHerm">if the result is made Hermitian or not</param>
		/// <param name="overwrite">the <see cref="MatrixBase{T}"/> to overwrite by result, default null</param>
		/// <returns>The result of Kronecker product, a new <see cref="MatrixBase{T}"/> or <paramref name="overwrite"/> if it is not null.</returns>
		public abstract MatrixBase<T> KroneckerProd(MatrixBase<T> B, bool forceHerm = true, MatrixBase<T> overwrite = null);

		/// <summary>
		/// Compute Kronecker sum $A \oplus B \equiv A \otimes I + I \otimes B$ where $A$ is this matrix. If <paramref name="forceHerm"/> is true, then $[(A \otimes I + I \otimes B^H) + (A^H \otimes I + I \otimes B)]/2$ will be calculated. From <see cref="IMatrix{TMat, T}.KroneckerSum"/>.
		/// </summary>
		/// <param name="B">right <see cref="DenseMatrix{T}"/></param>
		/// <param name="forceHerm">if the result is made Hermitian or not</param>
		/// <param name="overwrite">the <see cref="MatrixBase{T}"/> to overwrite by result, default null</param>
		/// <returns>The result of Kronecker product, a new <see cref="MatrixBase{T}"/> or <paramref name="overwrite"/> if it is not null.</returns>
		public abstract MatrixBase<T> KroneckerSum(MatrixBase<T> B, bool forceHerm = true, MatrixBase<T> overwrite = null);
		#endregion

		#region solver
		/// <summary>
		/// Solve a series of linear systems: $A X = B$, where each column pair of X and <paramref name="B"/> is a linear system <b>out-of-place</b>.
		/// </summary>
		/// <param name="B">each column of this <see cref="MatrixBase{T}"/> is the vector at right</param>
		/// <param name="overwriteB">if true, <paramref name="B"/> will be overwritten by solution X in the end</param>
		/// <exception cref="ArgumentNullException">if any of the array is null</exception>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		/// <exception cref="StatusException">if the calculation of CUDA Solver routines returns error status</exception>
		/// <exception cref="Solver.MatrixAlgorithmException">if the internal calculation fails, mainly caused when this matrix has no inverse</exception>
		/// <remarks>The default implementation converts all matrices into <see cref="DenseMatrix{T}"/> before calling <see cref="SOLVE.LinearSolve{T}(DenseMatrix{T}, DenseMatrix{T})"/> to solve it, since solving sparse matrices is never a good idea. The built-in matrix classes <see cref="DenseMatrix{T}"/> and <see cref="SparseMatrix{T}"/> do not override this implementation.</remarks>
		public virtual MatrixBase<T> LinearSolve(MatrixBase<T> B, bool overwriteB = false)
		{
			if (B is null || B == EmptyDnMat)
				throw new ArgumentNullException(nameof(B), Resource.ArrayCannotNull);
			DenseMatrix<T> dA = this.ToDense(), dB = B.ToDense();
			if (dA == this)
				dA = this.Clone() as DenseMatrix<T>;
			if (dB == B && !overwriteB)
				dB = B.Clone() as DenseMatrix<T>;
			try
			{
				SOLVE.LinearSolve(dA, dB);
				return dB;
			}
			catch (Exception)
			{
				if (dB != B) dB.Dispose();
				throw;
			}
			finally
			{
				if (dA != this) dA.Dispose();
			}
		}
		#endregion

		#region will be implemented by non built-in matrix class(es)
		/// <summary>
		/// Compute $\vec{y} = \beta \cdot \vec{y} + \alpha \cdot op(A_{\text{this}}) \vec{x}$.
		/// </summary>
		/// <param name="x">the input <see cref="VectorBase{T}"/></param>
		/// <param name="y">the output <see cref="VectorBase{T}"/></param>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		/// <param name="β">scalar of type <typeparamref name="T"/></param>
		/// <param name="op"><see cref="MatrixOperation"/> applied to this matrix</param>
		/// <returns>The result vector <paramref name="y"/> or a new one if <paramref name="y"/> cannot be in-place altered</returns>
		/// <remarks>The opposite of <see cref="VectorBase{T}.Mulβ_AddBy_αopAx"/>, only the classes directly inherits <see cref="MatrixBase{T}"/> need to implement this method. This method is used by built-in <see cref="DenseVector{T}"/> and <see cref="SparseVector{T}"/> to implement <see cref="VectorBase{T}.Mulβ_AddBy_αopAx"/>.</remarks>
		internal protected abstract VectorBase<T> Mulx_AddTo_y(VectorBase<T> x, VectorBase<T> y, T α, T β = default, MatrixOperation op = MatrixOperation.None);
		#endregion

		#endregion


		#region defined operators
		/// <summary>
		/// Matrix Transpose, conjugate and conjugate transpose, <b>out-of-place</b>.
		/// </summary>
		/// <param name="M">input <see cref="MatrixBase{T}"/></param>
		/// <param name="op">the <see cref="PowerOperation"/></param>
		/// <returns>a <see cref="MatrixBase{T}"/> after the <paramref name="op"/></returns>
		/// <remarks>If the result matrix is itself, this matrix will directly be returned where no new matrix will be created.</remarks>
		public static MatrixBase<T> operator ^(MatrixBase<T> M, PowerOperation op)
		{
			if (M is null || M == EmptyDnMat)
				throw new ArgumentNullException(nameof(M), Resource.ArrayCannotNull);
			switch (op)
			{
				case PowerOperation.Transpose:
					if (M.Hermitian && M.IsRealType)
						return M;
					return M.Transpose();
				case PowerOperation.Conjugate:
					if (M.IsRealType)
						return M;
					return M.ApplyToClone(newMat => BLAS.PointWiseConjugate(newMat));
				case PowerOperation.Dagger:
					if (M.Hermitian)
						return M;
					return M.ConjugateTranspose();
				case PowerOperation.None:
					return M;
				default:
					throw new ArgumentOutOfRangeException(nameof(op));
			}
		}

		/// <summary>
		/// Addition of two matrices, <b>out-of-place</b>.
		/// </summary>
		/// <param name="A">input <see cref="MatrixBase{T}"/> A</param>
		/// <param name="B">input <see cref="MatrixBase{T}"/> B</param>
		/// <returns>output <see cref="MatrixBase{T}"/> C</returns>
		public static MatrixBase<T> operator +(MatrixBase<T> A, MatrixBase<T> B)
		{
			if (A is null || A == EmptyDnMat)
				throw new ArgumentNullException(nameof(A), Resource.ArrayCannotNull);
			if (B is null || B == EmptyDnMat)
				throw new ArgumentNullException(nameof(B), Resource.ArrayCannotNull);
			if (A.OnHost != B.OnHost)
				throw new ArgumentException(Resource.RequireSamePos);
			if (A.NRows != B.NRows || A.NCols != B.NCols)
				throw new ArgumentException(Resource.MatrixWrongSize);

			var result = new DenseMatrix<T>(A.NRows, A.NCols, A.OnHost, herm: A.Hermitian && B.Hermitian);
			try
			{
				result.From_αA_Add_βB(A, B, Scalars<T>.One, Scalars<T>.One);
				return result;
			}
			catch (Exception)
			{
				result.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Subtraction of two matrices, <b>out-of-place</b>.
		/// </summary>
		/// <param name="A">input <see cref="MatrixBase{T}"/> A</param>
		/// <param name="B">input <see cref="MatrixBase{T}"/> B</param>
		/// <returns>output <see cref="MatrixBase{T}"/> C</returns>
		public static MatrixBase<T> operator -(MatrixBase<T> A, MatrixBase<T> B)
		{
			if (A is null || A == EmptyDnMat)
				throw new ArgumentNullException(nameof(A), Resource.ArrayCannotNull);
			if (B is null || B == EmptyDnMat)
				throw new ArgumentNullException(nameof(B), Resource.ArrayCannotNull);
			if (A.OnHost != B.OnHost)
				throw new ArgumentException(Resource.RequireSamePos);
			if (A.NRows != B.NRows || A.NCols != B.NCols)
				throw new ArgumentException(Resource.MatrixWrongSize);

			var result = new DenseMatrix<T>(A.NRows, A.NCols, A.OnHost, herm: A.Hermitian && B.Hermitian);
			try
			{
				result.From_αA_Add_βB(A, B, Scalars<T>.One, Scalars<T>.MinusOne);
				return result;
			}
			catch (Exception)
			{
				result.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Matrix negation.
		/// </summary>
		/// <param name="A">input <see cref="MatrixBase{T}"/>, will be overwritten if it is in-place</param>
		/// <returns>The negation <see cref="MatrixBase{T}"/></returns>
		public static MatrixBase<T> operator -(MatrixBase<T> A)
		{
			if (A is null || A == EmptyDnMat)
				throw new ArgumentNullException(nameof(A), Resource.ArrayCannotNull);
			return A * Scalars<T>.MinusOne;
		}

		/// <summary>
		/// Matrix scaling.
		/// </summary>
		/// <param name="A">input <see cref="MatrixBase{T}"/>, will be overwritten if it is in-place</param>
		/// <param name="α">input scalar of type <typeparamref name="T"/></param>
		/// <returns>output <see cref="MatrixBase{T}"/> C</returns>
		/// <remarks>This operator is implemented by the <see cref="VectorBase{T}.Scale(T)"/> rather than a dedicate abstract operation in <see cref="MatrixBase{T}"/>.</remarks>
		public static MatrixBase<T> operator *(MatrixBase<T> A, T α)
		{
			if (A is null || A == EmptyDnMat)
				throw new ArgumentNullException(nameof(A), Resource.ArrayCannotNull);
			return A.ApplyToClone(newA => newA.AsDenseVector().Scale(α));
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
		/// Multiply two matrices, <b>out-of-place</b>.
		/// </summary>
		/// <param name="A">input <see cref="MatrixBase{T}"/> A</param>
		/// <param name="B">input <see cref="MatrixBase{T}"/> B</param>
		/// <returns>output <see cref="MatrixBase{T}"/> C</returns>
		public static MatrixBase<T> operator *(MatrixBase<T> A, MatrixBase<T> B)
		{
			if (A is null || A == EmptyDnMat)
				throw new ArgumentNullException(nameof(A), Resource.ArrayCannotNull);
			if (B is null || B == EmptyDnMat)
				throw new ArgumentNullException(nameof(B), Resource.ArrayCannotNull);
			if (A.OnHost != B.OnHost)
				throw new ArgumentException(Resource.RequireSamePos);
			if (A.NCols != B.NRows)
				throw new ArgumentException(Resource.MatrixWrongSize);

			var result = new DenseMatrix<T>(A.NRows, B.NCols, A.OnHost);
			try
			{
				result.Mulβ_AddBy_αAB(A, B, Scalars<T>.One);
				return result;
			}
			catch (Exception)
			{
				result.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Calculate the inverse of the matrix by solving linear systems, <b>out-of-place</b>.
		/// </summary>
		/// <param name="overwrite">the <see cref="MatrixBase{T}"/> to store the inverse matrix, default null means that this method will create a new one and return</param>
		/// <returns>Inverse of this <see cref="MatrixBase{T}"/>.</returns>
		/// <exception cref="NotSupportedException">if this matrix is not square</exception>
		/// <remarks>This operator creates a dense identity matrix inside if this is a built-in matrix class, or a sparse one of <see cref="SparseMatrixFormat.Coordinated"/> if this is a custom sparse matrix class.</remarks>
		public MatrixBase<T> Inverse(MatrixBase<T> overwrite = null)
		{
			if (this.NRows != this.NCols)
				throw new NotSupportedException(Resource.MatMustSquare);
			bool canOverwrite = overwrite != null && overwrite != EmptyDnMat && overwrite is DenseMatrix<T> && overwrite.NRows == this.NRows && overwrite.NCols == this.NCols && overwrite.OnHost == this.OnHost;
			var I = canOverwrite ? overwrite as DenseMatrix<T> : new DenseMatrix<T>(this.NRows, this.NRows, this.OnHost);
			try
			{
				I.FillWithIdentity();
				this.LinearSolve(I, overwriteB: true); // I is now the solution;
				return I;
			}
			catch (Exception)
			{
				if (I != overwrite) I.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Matrix integer power, calculated by matrix multiplication or eigenvalue decomposition according to the value of <paramref name="power"/>.
		/// </summary>
		/// <param name="M">input <see cref="MatrixBase{T}"/> to power</param>
		/// <param name="power">the power of <see cref="int"/> type</param>
		public static MatrixBase<T> operator ^(MatrixBase<T> M, int power)
		{
			if (M is null || M == EmptyDnMat)
				throw new ArgumentNullException(nameof(M), Resource.ArrayCannotNull);
			if (M.NRows != M.NCols)
				throw new NotSupportedException(Resource.MatMustSquare);
			if (power == 0)
				throw new ArgumentOutOfRangeException(nameof(power));
			if (power == 1)
			{
				return M;
			}
			else if (power < 0)
			{
				if (power == -1)
					return M.Inverse();
				else
				{
					using var mi = M.Inverse();
					return mi ^ (-power);
				}
			}
			else if (power > 16)
			{
				return PowerByEigenDecompose(M, power);
			}
			else
			{
				return PowerByRecurrence(M, power);
			}
		}

		private static MatrixBase<T> PowerByEigenDecompose(MatrixBase<T> M, int pow)
		{
			DenseMatrix<T> dn = M.ToDense();
			try
			{
				if (M.IsRealType)
				{
					if (M.IsSingleType)
					{
						var (values, _, vectors) = dn.Eigensystem<FloatComplex>();
						using (values) using (vectors)
						{
							BLAS.PointWisePower(values, pow);
							using var temp = vectors.NewArrayAlike() as DenseMatrix<FloatComplex>;
							BLAS.DiagonalMatrixMultiply(vectors, values, temp);
							using var final = temp * vectors;
							return final.DataTypeCast<T>() as MatrixBase<T>;
						}
					}
					else
					{
						var (values, _, vectors) = dn.Eigensystem<DoubleComplex>();
						using (values) using (vectors)
						{
							BLAS.PointWisePower(values, pow);
							using var temp = vectors.NewArrayAlike() as DenseMatrix<DoubleComplex>;
							BLAS.DiagonalMatrixMultiply(vectors, values, temp);
							using var final = temp * vectors;
							return final.DataTypeCast<T>() as MatrixBase<T>;
						}
					}
				}
				else
				{
					var (values, _, vectors) = dn.Eigensystem<T>();
					using (values) using (vectors)
					{
						BLAS.PointWisePower(values, pow);
						using var temp = vectors.NewArrayAlike() as DenseMatrix<T>;
						BLAS.DiagonalMatrixMultiply(vectors, values, temp);
						return temp * vectors;
					}
				}
			}
			finally
			{
				if (dn != M) dn.Dispose();
			}
		}

		private static MatrixBase<T> PowerByRecurrence(MatrixBase<T> M, int pow)
		{
			if (pow == 1)
				return M;
			else
			{
				using var p = PowerByRecurrence(M, pow / 2);
				if (pow % 2 == 0)
					return p * p;
				else
				{
					using var psquare = p * p;
					return psquare * M;
				}
			}
		}
		#endregion


		#region abstract indexers
		/// <summary>
		/// Check the row and column index then return the offset of them.
		/// </summary>
		/// <param name="row">row <see cref="Index"/></param>
		/// <param name="col">column <see cref="Index"/></param>
		/// <returns>row and column offset</returns>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="row"/> or <paramref name="col"/> is out of range</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected (long row, long col) CheckRange(Index row, Index col)
		{
			long rowPos = row.GetPosition(this.NRows), colPos = col.GetPosition(this.NCols);
			if (rowPos < 0 || rowPos >= this.NRows)
				throw new ArgumentOutOfRangeException(nameof(row));
			if (colPos < 0 || colPos >= this.NRows)
				throw new ArgumentOutOfRangeException(nameof(col));
			return (rowPos, colPos);
		}

		/// <summary>
		/// Check the row and column range then return the offset/count of them.
		/// </summary>
		/// <param name="row">row <see cref="Range"/></param>
		/// <param name="col">column <see cref="Range"/></param>
		/// <returns>row and column offset/count</returns>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="row"/> or <paramref name="col"/> is out of range</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected (long offsetRow, long countRow, long offsetCol, long countCol) CheckRange(Range row, Range col)
		{
			var (offsetRow, countRow) = row.GetOffsetAndCount(this.NRows);
			if (offsetRow < 0 || offsetRow >= this.NRows)
				throw new ArgumentOutOfRangeException(nameof(row), row, Resource.RangeStartWrong);
			if (countRow <= 0)
				throw new ArgumentOutOfRangeException(nameof(row), row, Resource.RangeCountWrong);
			var (offsetCol, countCol) = col.GetOffsetAndCount(this.NCols);
			if (offsetCol < 0 || offsetCol >= this.NCols)
				throw new ArgumentOutOfRangeException(nameof(col), col, Resource.RangeStartWrong);
			if (countCol <= 0)
				throw new ArgumentOutOfRangeException(nameof(col), col, Resource.RangeCountWrong);
			return (offsetRow, countRow, offsetCol, countCol);
		}

		/// <summary>
		/// Check the row and column indices then return the offsets of them.
		/// </summary>
		/// <param name="indices">array of form <c>(<see cref="Index"/> x, <see cref="Index"/> y)</c></param>
		/// <returns>row and column offsets</returns>
		/// <exception cref="ArgumentOutOfRangeException">if any <paramref name="indices"/> is out of range</exception>
		/// <exception cref="ArgumentException">if the indices are not unique</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected (long[] rows, long[] cols) CheckRange((Index row, Index col)[] indices)
		{
			if (indices is null)
				throw new ArgumentNullException(nameof(indices));
			long[] rows = new long[indices.Length], cols = new long[indices.Length];
			for (int i = 0; i < indices.Length; i++)
			{
				(rows[i], cols[i]) = CheckRange(indices[i].row, indices[i].col);
				for (int j = 0; j < i; j++)
				{
					if (rows[j] == rows[i] && cols[j] == cols[i])
						throw new ArgumentException(Resource.DuplicateIndices, nameof(indices));
				}
			}
			return (rows, cols);
			
		}

		/// <summary>
		/// Basic indexer of matrix, declared in <see cref="IMatrix{T}"/>.
		/// </summary>
		/// <param name="x">row position in <see cref="Index"/> form</param>
		/// <param name="y">column position in <see cref="Index"/> form</param>
		/// <returns>Element at position (<paramref name="x"/>, <paramref name="y"/>)</returns>
		/// <remarks>Since a value cannot hold reference, altering the retrieved value does not change this array's value at that position.</remarks>
		public abstract T this[Index x, Index y] { get; set; }

		/// <summary>
		/// Range indexer of matrix, new in <see cref="MatrixBase{T}"/>.
		/// </summary>
		/// <param name="x">range of rows in <see cref="Range"/> form, end is exclusive</param>
		/// <param name="y">range of columns in <see cref="Range"/> form, end is exclusive</param>
		/// <returns>A sub-matrix in this range, try to be a reference</returns>
		/// <remarks>See <see cref="Index"/> and <see cref="Range"/> for more information.</remarks>
		public abstract MatrixBase<T> this[Range x, Range y] { get; set; }

		/// <summary>
		/// Multiple element indexer of matrix, new in <see cref="MatrixBase{T}"/>.
		/// </summary>
		/// <param name="indices">row and column positions in <see cref="Index"/> array form</param>
		/// <returns>Elements at these positions copied into a new <see cref="VectorBase{T}"/></returns>
		public abstract VectorBase<T> this[params (Index x, Index y)[] indices] { get; set; }
		#endregion


		#region abstract diagonal indexer
		/// <summary>
		/// The diagonal accessor
		/// </summary>
		protected DeviceMatrixDiagAccessor<T> DiagAccessor { get; set; }

		/// <summary>
		/// Get the diagonal element accessor that allows you to read and set diagonal values
		/// </summary>
		/// <exception cref="InvalidOperationException">if this matrix cannot </exception>
		public virtual DeviceMatrixDiagAccessor<T> Diagonal {
			get {
				if (this.NRows == this.NCols)
				{
					if (this.DiagAccessor.Equals(default))
						this.DiagAccessor = new DeviceMatrixDiagAccessor<T>(this);
					return this.DiagAccessor;
				}
				else
				{
					return default;
					//throw new InvalidOperationException(Resource.MatMustSquare);
				}
			}
			set { throw new InvalidOperationException(); }
		}

		/// <summary>
		/// The method to get diagonal elements.
		/// </summary>
		/// <param name="k">diagonal index, 0 for diag, 1 for super-diagonal at one above, -1 for sub-diagonal at one below, etc.</param>
		/// <param name="overwrite">the output <see cref="VectorBase{T}"/> to overwrite, default null means creating a new vector</param>
		/// <returns>A new <see cref="VectorBase{T}"/> containing the (super-/sub-)diagonal elements. If <paramref name="overwrite"/> does not fit, it will not be returned.</returns>
		public abstract VectorBase<T> GetDiag(long k, VectorBase<T> overwrite = null);

		/// <summary>
		/// The method to set diagonal elements.
		/// </summary>
		/// <param name="k">diagonal index, 0 for diag, 1 for super-diagonal at one above, -1 for sub-diagonal at one below, etc.</param>
		/// <param name="vec">the <see cref="VectorBase{T}"/> </param>
		public abstract void SetDiag(long k, VectorBase<T> vec);
		#endregion
	}


	/// <summary>
	/// The diagonal element access class
	/// </summary>
	public readonly struct DeviceMatrixDiagAccessor<T> : IEquatable<DeviceMatrixDiagAccessor<T>> where T : struct, IComparable<T>
	{
		#region basic
		private readonly MatrixBase<T> _owner;

		internal DeviceMatrixDiagAccessor(MatrixBase<T> o) => _owner = o;

		/// <summary>
		/// Indexer of getting and setting diagonal (or sub- / super- diagonal) elements
		/// </summary>
		/// <param name="k">diagonal index, 0 for diag, 1 for super-diagonal, -1 for sub-diagonal, etc.</param>
		/// <remarks>If <see cref="MatrixBase{T}.Hermitian"/> is true, all the sub-diagonals are set to / get as super-diagonals.</remarks>
		public VectorBase<T> this[long k] {
			get => _owner.GetDiag(k);
			set => _owner.SetDiag(k, value);
		}
		#endregion

		#region equality
		/// <summary>
		/// Equal operator
		/// </summary>
		/// <param name="a">left <see cref="DeviceMatrixDiagAccessor{T}"/></param>
		/// <param name="b">right <see cref="DeviceMatrixDiagAccessor{T}"/></param>
		/// <returns>equal or not</returns>
		public static bool operator ==(DeviceMatrixDiagAccessor<T> a, DeviceMatrixDiagAccessor<T> b) => a.Equals(b);

		/// <summary>
		/// Not equal operator
		/// </summary>
		/// <param name="a">left <see cref="DeviceMatrixDiagAccessor{T}"/></param>
		/// <param name="b">right <see cref="DeviceMatrixDiagAccessor{T}"/></param>
		/// <returns>non-equal or not</returns>
		public static bool operator !=(DeviceMatrixDiagAccessor<T> a, DeviceMatrixDiagAccessor<T> b) => !a.Equals(b);

		/// <summary>
		/// Override <see cref="object.Equals(object)"/>
		/// </summary>
		/// <param name="obj">another object</param>
		/// <returns>equal or not</returns>
		public override bool Equals(object obj) => (obj is DeviceMatrixDiagAccessor<T> d) && (d._owner == this._owner);

		/// <summary>
		/// Override <see cref="IEquatable{T}.Equals(T)"/>
		/// </summary>
		/// <param name="other">another <see cref="DeviceMatrixDiagAccessor{T}"/></param>
		/// <returns>equal or not</returns>
		public bool Equals(DeviceMatrixDiagAccessor<T> other) => other._owner == this._owner;

		/// <summary>
		/// Override <see cref="object.GetHashCode"/>
		/// </summary>
		/// <returns>hash code</returns>
		public override int GetHashCode() => this._owner.GetHashCode();
		#endregion
	}
}