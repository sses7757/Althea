using System;
using System.Collections.Generic;

using Althea.NativeTypes;
using Althea.TensorAlgebra; // TensorOrder


namespace Althea.Arrays
{
	/// <summary>
	/// The interface for mutable array whose value array can be filled with different values
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public interface IMutableArray<T> where T : unmanaged, IFormattable, IEquatable<T>
	{
		#region fills
		/// <summary>
		/// Fill this array's value array with zeros.
		/// </summary>
		void FillWithZeros();

		/// <summary>
		/// Fill this array's value array with randomly generated numbers.
		/// </summary>
		void FillWithRandoms();

		/// <summary>
		/// Fill this array's value array with ones.
		/// </summary>
		void FillWithOnes();
		#endregion
	}

	/// <summary>
	/// Simple interface for sparse array which only contains basic members, additional fillings and .conversions from / to C# arrays
	/// </summary>
	/// <typeparam name="T">the supported data types</typeparam>
	public interface ISparseArray<T> : IMutableArray<T> where T : unmanaged, IFormattable, IEquatable<T>
	{
		#region members
		/// <summary>
		/// Number of nonzero values of this sparse vector, equal to the size of the index/value array size.
		/// </summary>
		long NonZero { get; }
		#endregion

		#region fills
		/// <summary>
		/// Fill this sparse array's index array(s) with arithmetic sequence(s), from <see cref="ISparseArray{T}"/>.
		/// </summary>
		/// <param name="v">start values and steps of the sequence(s), must be of same length as <see cref="AbstractArray{T}.Size"/></param>
		/// <exception cref="ArgumentException">if the lengths/values of <paramref name="v"/> do not follow the rule</exception>
		void FillIndexWithRange(params (int start, int step)[] v);
		#endregion

		#region to C# arrays
		/// <summary>
		/// Convert the values of this array to a C# array of Fortran/MATLAB order.
		/// </summary>
		/// <param name="ranges">the ranges of each dimension, default is all</param>
		/// <returns>C# array of type <typeparamref name="T"/> containing the values of this array</returns>
		T[] ValueToFortranOrderArray(params Range[] ranges);

		/// <summary>
		/// Convert the indices of this array to an <see cref="IEnumerable{T}"/> of C# arrays
		/// </summary>
		/// <param name="ranges">the range of each index array, default all</param>
		/// <returns>an <see cref="IEnumerable{T}"/> of C# arrays of type <see cref="int"/></returns>
		IEnumerable<int[]> IndexToIntArray(params Range[] ranges);

		/// <summary>
		/// Convert the indices of this array to an <see cref="IEnumerable{T}"/> of C# arrays
		/// </summary>
		/// <param name="ranges">the range of each index array, default all</param>
		/// <returns>an <see cref="IEnumerable{T}"/> of C# arrays of type <see cref="long"/></returns>
		IEnumerable<long[]> IndexToLongArray(params Range[] ranges);
		#endregion

		#region from C# arrays
		/// <summary>
		/// Copy the <paramref name="values"/> of Fortran/MATLAB order into this array's value array.
		/// </summary>
		/// <param name="values">the value array of element type <typeparamref name="T"/></param>
		/// <param name="ranges">the ranges of each dimension, default is all</param>
		void ValueFromFortranOrderArray(T[] values, params Range[] ranges);

		/// <summary>
		/// Copy the <paramref name="indices"/> into this array's index arrays.
		/// </summary>
		/// <param name="indices">an <see cref="IEnumerable{T}"/> of C# <see cref="int"/> arrays</param>
		/// <param name="ranges">the range of each index array, default all</param>
		void IndexFromIntArray(IEnumerable<int[]> indices, params Range[] ranges);

		/// <summary>
		/// Copy the <paramref name="indices"/> into this array's index arrays.
		/// </summary>
		/// <param name="indices">an <see cref="IEnumerable{T}"/> of C# <see cref="long"/> arrays</param>
		/// <param name="ranges">the range of each index array, default all</param>
		void IndexFromLongArray(IEnumerable<long[]> indices, params Range[] ranges);
		#endregion

		#region dispose
		/// <summary>
		/// Dispose this sparse array after excluding the internal storages shared between this array and the target <paramref name="array"/>.
		/// </summary>
		/// <param name="array">the target <see cref="ISparseArray{T}"/> to exclude</param>
		void DisposeExclude(ISparseArray<T> array);
		#endregion
	}

	/// <summary>
	/// Simple interface for dense array which contains the conversions from / to C# arrays.
	/// </summary>
	/// <typeparam name="T">the supported data types</typeparam>
	public interface IDenseArray<T> : IMutableArray<T> where T : unmanaged, IFormattable, IEquatable<T>
	{
		#region to C# arrays
		/// <summary>
		/// Convert the values of this array to a C# array of Fortran/MATLAB order.
		/// </summary>
		/// <param name="ranges">the ranges of each dimension, default is all</param>
		/// <returns>C# array of type <typeparamref name="T"/> containing the values of this array</returns>
		T[] ToFortranOrderArray(params Range[] ranges);
		#endregion

		#region from C# arrays
		/// <summary>
		/// Copy the <paramref name="values"/> of Fortran/MATLAB order into this array.
		/// </summary>
		/// <param name="values">the value array of element type <typeparamref name="T"/></param>
		/// <param name="ranges">the ranges of each dimension, default is all</param>
		void FromFortranOrderArray(T[] values, params Range[] ranges);
		#endregion
	}

	/// <summary>
	/// The interface of vector that contains the members, operations and indexers of vector whose inputs and outputs are not relevant with vector.
	/// </summary>
	/// <typeparam name="T">the supported data types</typeparam>
	public interface IVector<T> where T : unmanaged, IFormattable, IEquatable<T>
	{
		/// <summary>
		/// The last index of the vector
		/// </summary>
		long LastIndex { get; }

		#region operation
		/// <summary>
		/// Scale this vector <b>in-place</b>, i.e. $\vec{v}_{\text{this}} = \alpha \vec{v}_{\text{this}}$.
		/// </summary>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		void Scale(T α);

		/// <summary>
		/// 2-norm of this vector, i.e. $\|\vec{v}\| = \sqrt{\sum_i{\vec{v}_i^2}}$.
		/// </summary>
		/// <returns>The 2-norm of this vector.</returns>
		double Norm();

		/// <summary>
		/// Normalize this vector <b>in-place</b> to make it norm-one, i.e. $\vec{v} = \vec{v} / \|\vec{v}\|$.
		/// </summary>
		void Normalize();
		#endregion

		#region indexer
		/// <summary>
		/// Basic indexer of vector.
		/// </summary>
		/// <param name="i">position in <see cref="Index"/> form</param>
		/// <returns>an instance of the data type <typeparamref name="T"/></returns>
		/// <remarks>Since a value cannot hold reference, altering the retrieved value does not change this array's value at that position.</remarks>
		T this[Index i] { get; set; }
		#endregion
	}

	/// <summary>
	/// The interface of vector that contains the operation needed for Lanczos and Krylov-Schur solver.
	/// </summary>
	/// <typeparam name="T">the supported data types</typeparam>
	/// <typeparam name="TVec">the vector type</typeparam>
	public interface IKrylovVector<TVec, T> : IVector<T>
		where TVec : IKrylovVector<TVec, T> 
		where T : unmanaged, IFormattable, IEquatable<T>
	{
		#region operation
		/// <summary>
		/// Vector inner product, compute $\vec{v}_{\text{this}} \cdot \vec{v}_{\text{other}} \equiv \vec{v}_{\text{this}}^H (\text{or }\vec{v}_{\text{this}}^H) \vec{v}_{\text{other}}$.
		/// </summary>
		/// <param name="other">the other <typeparamref name="TVec"/></param>
		/// <param name="conjugateThis">perform non- or conjugate transpose to this vector</param>
		/// <returns>The inner product result</returns>
		/// <remarks>This method is symmetric (semi-symmetric, e.g. the conjugate relation, when data type is a complex type) for this vector and the other vector.</remarks>
		T Dot(TVec other, bool? conjugateThis = null);

		/// <summary>
		/// Compute $\vec{v}_{\text{this}} = \vec{v}_{\text{this}} + \alpha \vec{x}$.
		/// </summary>
		/// <param name="x">vector</param>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		void AddBy_αx(TVec x, T α);

		/// <summary>
		/// Operate the matrix whose columns are <paramref name="notJoinedVecs"/> onto a C# array to get a result vector <typeparamref name="TVec"/>.
		/// </summary>
		/// <param name="notJoinedVecs">the columns of the matrix to operate</param>
		/// <param name="input">the input C# array to be operated</param>
		/// <returns><c>[<paramref name="notJoinedVecs"/>] * <paramref name="input"/></c> as <typeparamref name="TVec"/>.</returns>
		/// <remarks>this method is actually static</remarks>
		TVec OperateOn(IReadOnlyList<TVec> notJoinedVecs, T[] input);

		/// <summary>
		/// Replace this vector's content with <paramref name="another"/> <b>in-place</b>.
		/// </summary>
		/// <param name="another">another <typeparamref name="TVec"/> to replace from</param>
		void ReplaceBy(TVec another);
		#endregion
	}

	/// <summary>
	/// The interface of vector that contains the members, operations and indexers of vector whose inputs are relevant with vector.
	/// </summary>
	/// <typeparam name="T">the supported data types</typeparam>
	/// <typeparam name="TVec">the vector type</typeparam>
	public interface IVector<TVec, T> : IKrylovVector<TVec, T>
		where TVec : AbstractArray<T>, IVector<TVec, T>
		where T : unmanaged, IFormattable, IEquatable<T>
	{
		#region operation
		/// <summary>
		/// Compute $\vec{v}_{\text{this}}\circ\vec{v}_{\text{other}} \equiv \{\vec{v}_{\text{this}}^i \vec{v}_{\text{other}}^i\}_i$.
		/// </summary>
		/// <param name="other">the other <typeparamref name="TVec"/></param>
		/// <remarks>This method is symmetric since only the sparse vector one may be modified.</remarks>
		void PointWiseMultiply(TVec other);

		/// <summary>
		/// Compute $\vec{v}_{\text{this}} ./ \vec{v}_{\text{other}} \equiv \{\vec{v}_{\text{this}}^i \vec{v}_{\text{other}}^i\}_i$.
		/// </summary>
		/// <param name="other">the other <typeparamref name="TVec"/></param>
		void PointWiseDivide(TVec other);

		// TODO: add point wise power, etc.?
		#endregion
	}

	/// <summary>
	/// The interface of vector that contains the extra operations of vector whose inputs / outputs are also relevant with matrix.
	/// </summary>
	/// <typeparam name="T">the supported data types</typeparam>
	/// <typeparam name="TVec">the vector type</typeparam>
	/// <typeparam name="TMat">the matrix type</typeparam>
	public interface IVector<TVec, TMat, T> : IVector<TVec, T>
		where TVec : AbstractArray<T>, IVector<TVec, TMat, T>
		where TMat : AbstractArray<T>, IMatrix<TMat, T>
		where T : unmanaged, IFormattable, IEquatable<T>
	{
		#region operation
		/// <summary>
		/// Compute $\vec{y}_{\text{this}} = \beta \cdot \vec{y}_{\text{this}} + \alpha \cdot A^{\text{op}} \vec{x}$.
		/// </summary>
		/// <param name="x">the input <typeparamref name="TVec"/></param>
		/// <param name="A">the input <typeparamref name="TMat"/></param>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		/// <param name="β">scalar of type <typeparamref name="T"/></param>
		/// <param name="op"><see cref="MatrixOperation"/> applied to <paramref name="A"/></param>
		void Mulβ_AddBy_αopAx(TMat A, TVec x, T α, T β = default, MatrixOperation op = MatrixOperation.None);

		/// <summary>
		/// Compute $M_{\text{result}} = \vec{v}_{\text{this}} \vec{v}_{\text{other}}^T$ or $M_{\text{result}} = \vec{v}_{\text{this}} \vec{v}_{\text{other}}^H$.
		/// </summary>
		/// <param name="other">the other input <typeparamref name="TVec"/></param>
		/// <param name="conjugateOther">perform non- or conjugate transpose to <paramref name="other"/></param>
		/// <param name="overwrite">the <typeparamref name="TMat"/> to overwrite as result, default null</param>
		/// <returns>The result <typeparamref name="TMat"/> or <paramref name="overwrite"/> if it is not null</returns>
		TMat OuterProduct(TVec other, bool? conjugateOther = null, TMat overwrite = null);
		#endregion
	}

	/// <summary>
	/// The interface of matrix that contains basic members, methods, operations and indexers whose inputs and outputs are not relevant with matrix.
	/// </summary>
	/// <typeparam name="T">the supported data types</typeparam>
	public interface IMatrix<T> where T : unmanaged, IFormattable, IEquatable<T>
	{
		#region member
		/// <summary>
		/// For real matrices, this is equivalent to symmetric.
		/// </summary>
		bool Hermitian { get; }

		/// <summary>
		/// Leading dimension. In column major, same as the row number.
		/// </summary>
		long NRows { get; }

		/// <summary>
		/// The secondary dimension. In column major, same as the column number.
		/// </summary>
		long NCols { get; }
		#endregion

		#region in-place method
		/// <summary>
		/// Make this matrix to be general.
		/// </summary>
		/// <returns>The matrix that made general</returns>
		void CopyUpperToLower();

		/// <summary>
		/// In-place conjugate the matrix.
		/// </summary>
		void ConjugateInPlace();

		/// <summary>
		/// Scale this matrix in-place, i.e. $M_{\text{this}} = \alpha M_{\text{this}}$.
		/// </summary>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		void Scale(T α);
		#endregion

		#region indexer
		/// <summary>
		/// Basic indexer of matrix.
		/// </summary>
		/// <param name="x">row position in <see cref="Index"/> form</param>
		/// <param name="y">column position in <see cref="Index"/> form</param>
		/// <returns>Element at position (<paramref name="x"/>, <paramref name="y"/>)</returns>
		/// <remarks>Since a value cannot hold reference, altering the retrieved value does not change this array's value at that position.</remarks>
		T this[Index x, Index y] { get; set; }
		#endregion
	}

	/// <summary>
	/// The interface of matrix that contains basic members, methods, operations and indexers whose inputs and outputs are relevant with matrix.
	/// </summary>
	/// <typeparam name="T">the supported data types</typeparam>
	/// <typeparam name="TMat">the matrix type</typeparam>
	public interface IMatrix<TMat, T> : IMatrix<T>, IKrylovVector<TMat, T>
		where TMat : AbstractArray<T>, IMatrix<TMat, T>
		where T : unmanaged, IFormattable, IEquatable<T>
	{
		#region method
		/// <summary>
		/// Get a new matrix by the column index range.
		/// </summary>
		/// <param name="columnRange"><see cref="Range"/> of columns</param>
		/// <param name="overwrite">the output <typeparamref name="TMat"/> to overwrite, default null means creating a ref matrix</param>
		/// <returns>A matrix constructed by these columns. If <paramref name="overwrite"/> does not fit, it will not be returned.</returns>
		TMat GetColumnRange(Range columnRange, TMat overwrite = null);

		/// <summary>
		/// Get a new matrix by the row index range.
		/// </summary>
		/// <param name="rowRange"><see cref="Range"/> of rows</param>
		/// <param name="overwrite">the output <typeparamref name="TMat"/> to overwrite, default null means creating a ref matrix</param>
		/// <returns>A matrix constructed by these rows. If <paramref name="overwrite"/> does not fit, it will not be returned.</returns>
		TMat GetRowRange(Range rowRange, TMat overwrite = null);

		/// <summary>
		/// Get a sub-matrix by the row and column index ranges.
		/// </summary>
		/// <param name="rowRange"><see cref="Range"/> of rows</param>
		/// <param name="columnRange"><see cref="Range"/> of columns</param>
		/// <param name="overwrite">the output <typeparamref name="TMat"/> to overwrite, default null means creating a ref matrix (if possible)</param>
		/// <returns>A sub-matrix in this region. If <paramref name="overwrite"/> does not fit, it will not be returned.</returns>
		TMat GetSubmatrix(Range rowRange, Range columnRange, TMat overwrite = null);
		#endregion

		#region operation
		/// <summary>
		/// Calculate the transpose of this matrix. A new <see cref="IMatrix{T}"/> will be created if the result is not it self.
		/// </summary>
		/// <param name="overwrite">the output <typeparamref name="TMat"/> to overwrite, default null means creating a new matrix</param>
		/// <returns>The transposed <typeparamref name="TMat"/>.</returns>
		TMat Transpose(TMat overwrite = null);

		/// <summary>
		/// Calculate the conjugate transpose of this matrix. A new <see cref="IMatrix{T}"/> will be created if the result is not it self.
		/// </summary>
		/// <param name="overwrite">the output <typeparamref name="TMat"/> to overwrite, default null means creating a new matrix</param>
		/// <returns>The conjugate-transposed <typeparamref name="TMat"/>.</returns>
		TMat ConjugateTranspose(TMat overwrite = null);

		/// <summary>
		/// Symmetrize this matrix by adding its conjugate transpose out-of-place.
		/// </summary>
		/// <param name="conjugateAtLast">return the original </param>
		/// <param name="overwrite">the output <typeparamref name="TMat"/> to overwrite, default null means creating a new matrix</param>
		/// <returns>If <c><paramref name="conjugateAtLast"/> == false</c>: $B_{\text{result}}=\frac{A + A^H}{2}$; otherwise: $B_{\text{result}}=\frac{\bar{A} + A^T}{2}$</returns>
		TMat Symmetrize(bool conjugateAtLast = false, TMat overwrite = null);

		/// <summary>
		/// Compute $C_{\text{this}} = \alpha A^{\text{opA}} + \beta B^{\text{opB}}$. This method will try to in-place replace this matrix.
		/// </summary>
		/// <param name="α">scalar of type <typeparamref name="T"/> with default 0. If <c><paramref name="α"/> == 0</c>, <paramref name="A"/> can be an invalid input</param>
		/// <param name="A">the <typeparamref name="TMat"/> A, can be null</param>
		/// <param name="opA">operation to matrix <paramref name="A"/></param>
		/// <param name="β">scalar of type <typeparamref name="T"/> with default 0. If <c><paramref name="β"/> == 0</c>, <paramref name="B"/> can be an invalid input</param>
		/// <param name="B">the input <typeparamref name="TMat"/> B, can be null</param>
		/// <param name="opB">operation to matrix <paramref name="B"/></param>
		void From_αA_Add_βB(TMat A, TMat B, T α = default, T β = default, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None);

		/// <summary>
		/// Compute $C_{\text{this}} = \alpha A^{\text{opA}} B^{\text{opB}} + \beta C_{\text{this}}$. This method will try to in-place replace this matrix.
		/// </summary>
		/// <param name="A">the input <typeparamref name="TMat"/> A</param>
		/// <param name="opA">operation to matrix <paramref name="A"/></param>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		/// <param name="B">the input <typeparamref name="TMat"/> B</param>
		/// <param name="opB">operation to matrix <paramref name="B"/></param>
		/// <param name="β">scalar of type <typeparamref name="T"/> with default 0</param>
		void Mulβ_AddBy_αAB(TMat A, TMat B, T α, T β = default, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None);

		/// <summary>
		/// Compute Kronecker product $A \otimes B$. If <paramref name="forceHerm"/> is true, then $(A \otimes B^H + A^H \otimes B)/2$ will be calculated.
		/// </summary>
		/// <param name="other">the other <typeparamref name="TMat"/> B at right</param>
		/// <param name="forceHerm">if the result is made Hermitian or not</param>
		/// <param name="overwrite">the <typeparamref name="TMat"/> to overwrite by result, default null</param>
		/// <returns>The result of Kronecker product, a new <typeparamref name="TMat"/> or <paramref name="overwrite"/> if it is not null.</returns>
		TMat KroneckerProd(TMat other, bool forceHerm = true, TMat overwrite = null);

		/// <summary>
		/// Compute Kronecker sum $A \oplus B \equiv A \otimes I + I \otimes B$. If <paramref name="forceHerm"/> is true, then $[(A \otimes I + I \otimes B^H) + (A^H \otimes I + I \otimes B)]/2$ will be calculated.
		/// </summary>
		/// <param name="other">the other <typeparamref name="TMat"/> B at right</param>
		/// <param name="forceHerm">if the result is made Hermitian or not</param>
		/// <param name="overwrite">the <typeparamref name="TMat"/> to overwrite by result, default null</param>
		/// <returns>The result of Kronecker sum, a new <typeparamref name="TMat"/> or <paramref name="overwrite"/> if it is not null.</returns>
		TMat KroneckerSum(TMat other, bool forceHerm = true, TMat overwrite = null);
		#endregion
	}

	/// <summary>
	/// The interface of matrix that contains extra methods, operations and indexers whose inputs and outputs are also relevant with vector.
	/// </summary>
	/// <typeparam name="T">the supported data types</typeparam>
	/// <typeparam name="TMat">the matrix type</typeparam>
	/// <typeparam name="TVec">the vector type</typeparam>
	public interface IMatrix<TMat, TVec, T> : IMatrix<TMat, T>
		where TMat : AbstractArray<T>, IMatrix<TMat, TVec, T>
		where TVec : AbstractArray<T>, IVector<TVec, T>
		where T : unmanaged, IFormattable, IEquatable<T>
	{
		#region method
		/// <summary>
		/// Join the array of <typeparamref name="TVec"/> forming into a <typeparamref name="TMat"/> overwriting this matrix.
		/// </summary>
		/// <param name="vectors">the input array of <typeparamref name="TVec"/></param>
		void FromColumnVectors(TVec[] vectors);

		/// <summary>
		/// Get part of the column vectors that forms the matrix.
		/// </summary>
		/// <param name="colRange">the <see cref="Range"/> of columns</param>
		/// <param name="overwrite">the output array of <typeparamref name="TVec"/> to overwrite, default null means creating ref vectors if possible</param>
		/// <returns>An array of data type <typeparamref name="TVec"/>. If <paramref name="overwrite"/> does not fit, it will not be returned.</returns>
		TVec[] GetColumns(Range colRange, TVec[] overwrite = null);

		/// <summary>
		/// Get part of the row vectors that forms the matrix.
		/// </summary>
		/// <param name="rowRange">the <see cref="Range"/> of rows</param>
		/// <param name="overwrite">the output array of <typeparamref name="TVec"/> to overwrite, default null means creating ref vectors if possible</param>
		/// <returns>An array of data type <typeparamref name="TVec"/>. If <paramref name="overwrite"/> does not fit, it will not be returned.</returns>
		TVec[] GetRows(Range rowRange, TVec[] overwrite = null);

		/// <summary>
		/// Get all of the column vectors that forms the matrix.
		/// </summary>
		/// <param name="overwrite">the output array of <typeparamref name="TVec"/> to overwrite, default null means creating ref vectors if possible</param>
		/// <returns>An array of data type <typeparamref name="TVec"/>. If <paramref name="overwrite"/> does not fit, it will not be returned.</returns>
		TVec[] GetColumns(TVec[] overwrite = null);

		/// <summary>
		/// Get all of the row vectors that forms the matrix.
		/// </summary>
		/// <param name="overwrite">the output array of <typeparamref name="TVec"/> to overwrite, default null means creating ref vectors if possible</param>
		/// <returns>An array of data type <typeparamref name="TVec"/>. If <paramref name="overwrite"/> does not fit, it will not be returned.</returns>
		TVec[] GetRows(TVec[] overwrite = null);

		/// <summary>
		/// Get one column of the matrix.
		/// </summary>
		/// <param name="index">the <see cref="Index"/> of column</param>
		/// <param name="overwrite">the output <typeparamref name="TVec"/> to overwrite, default null means creating a ref vector if possible</param>
		/// <returns>The selected column as a <typeparamref name="TVec"/>. If <paramref name="overwrite"/> does not fit, it will not be returned.</returns>
		TVec GetColumnAt(Index index, TVec overwrite = null);

		/// <summary>
		/// Get one row of the matrix.
		/// </summary>
		/// <param name="index">the <see cref="Index"/> of row</param>
		/// <param name="overwrite">the output <typeparamref name="TVec"/> to overwrite, default null means creating a ref vector if possible</param>
		/// <returns>The selected row as a <typeparamref name="TVec"/>. If <paramref name="overwrite"/> does not fit, it will not be returned.</returns>
		TVec GetRowAt(Index index, TVec overwrite = null);
		#endregion

		#region diagonal indexer
		/// <summary>
		/// The method to get diagonal elements.
		/// </summary>
		/// <param name="k">diagonal index, 0 for diag, 1 for super-diagonal at one above, -1 for sub-diagonal at one below, etc.</param>
		/// <param name="overwrite">the output <typeparamref name="TVec"/> to overwrite, default null means creating a new vector</param>
		/// <returns>A new <typeparamref name="TVec"/> containing the (super-/sub-)diagonal elements. If <paramref name="overwrite"/> does not fit, it will not be returned.</returns>
		TVec GetDiag(long k, TVec overwrite = null);

		/// <summary>
		/// The method to set diagonal elements.
		/// </summary>
		/// <param name="k">diagonal index, 0 for diag, 1 for super-diagonal at one above, -1 for sub-diagonal at one below, etc.</param>
		/// <param name="vec">the <typeparamref name="TVec"/></param>
		void SetDiag(long k, TVec vec);
		#endregion
	}

	/// <summary>
	/// The interface of decomposable matrix that contains EVD, SVD and QR.
	/// </summary>
	/// <typeparam name="T">the supported data types</typeparam>
	/// <typeparam name="TVec">the dense vector type</typeparam>
	/// <typeparam name="TMat">the matrix type</typeparam>
	public interface IDecomposable<TMat, TVec, T>
		where TMat : AbstractArray<T>, new()
		where TVec : AbstractArray<T>, new()
		where T : unmanaged, IFormattable, IEquatable<T>
	{
		#region methods
		/// <summary>
		/// Calculate the inverse of this matrix out-of-place
		/// </summary>
		/// <param name="overwrite">the <typeparamref name="TMat"/> to store the inverse matrix, default null means that this method will create a new one and return</param>
		/// <returns>the inverse matrix</returns>
		TMat Inverse(TMat overwrite = null);

		/// <summary>
		/// Calculate the eigenvalues (and eigenvectors) of this Hermitian matrix for the special eigen-problem -- $A V = V \Lambda$, or matrices pair A, <paramref name="B"/> for the general one -- $A V = \Lambda B V$ or $A B V = \Lambda V$ or $B A V = \Lambda V$ <b>out-of-place</b>. Here, matrix A is this matrix.
		/// </summary>
		/// <param name="B">the input <typeparamref name="TMat"/> to calculate general eigen-problem; if <c><paramref name="B"/> is null</c>, the normal eigen is performed and <paramref name="type"/> is not used; otherwise, the general one is performed</param>
		/// <param name="type">the <see cref="Solver.EigType"/> to indicate positions of this matrix and <paramref name="B"/></param>
		/// <returns>The eigenvalues</returns>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		TVec EigenvalueHerm(TMat B = null, Solver.EigType type = Solver.EigType.Type1);

		/// <summary>
		/// Calculate the eigenvalues (and eigenvectors) of this Hermitian matrix for the special eigen-problem -- $A V = V \Lambda$, or matrices pair A, <paramref name="B"/> for the general one -- $A V = \Lambda B V$ or $A B V = \Lambda V$ or $B A V = \Lambda V$ <b>out-of-place</b>. Here, matrix A is this matrix.
		/// </summary>
		/// <param name="overwriteValues">the <typeparamref name="TVec"/> to store eigenvalues, default null means that this method will create a new one and return</param>
		/// <param name="overwriteVectors">the <typeparamref name="TMat"/> to store eigenvectors, default null means that this method will create a new one and return</param>
		/// <param name="B">the input <typeparamref name="TMat"/> to calculate general eigen-problem; if <c><paramref name="B"/> is null</c>, the normal eigen is performed and <paramref name="type"/> is not used; otherwise, the general one is performed</param>
		/// <param name="type">the <see cref="Solver.EigType"/> to indicate positions of this matrix and <paramref name="B"/></param>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		(TVec values, TMat vectors) EigensystemHerm(TVec overwriteValues = null, TMat overwriteVectors = null, TMat B = null, Solver.EigType type = Solver.EigType.Type1);

		/// <summary>
		/// Compute the singular value decomposition (SVD) of this matrix and corresponding the left and/or right singular vectors: $A = U S V^*$ <b>out-of-place</b>, where A is this matrix.
		/// </summary>
		/// <param name="overwriteS">the <typeparamref name="TVec"/> to store singular values, default null means that this method will create a new one and return</param>
		/// <param name="overwriteU">the <typeparamref name="TMat"/> to store left singular vectors, default null means that this method will create a new one and return</param>
		/// <param name="overwriteVct">the <typeparamref name="TMat"/> to store right singular vectors, default null means that this method will create a new one and return</param>
		/// <param name="calcU">calculate the left singular vectors or not, if false, the return <c>U</c> will be null</param>
		/// <param name="calcV">calculate the right singular vectors or not, if false, the return <c>Vct</c> will be null</param>
		/// <returns>the singular values and left, right singular vectors</returns>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		(TVec S, TMat U, TMat Vct) SingularValues(TVec overwriteS = null, TMat overwriteU = null, TMat overwriteVct = null, bool calcU = true, bool calcV = true);

		/// <summary>
		/// QR factorize this matrix <b>out-of-place</b>.
		/// </summary>
		/// <param name="full">perform full factorization or not</param>
		/// <param name="overwriteQ">the <typeparamref name="TMat"/> to store triangular matrix Q, default null means that this method will create a new one and return</param>
		/// <param name="overwriteR">the <typeparamref name="TMat"/> to store triangular matrix R, default null means that this method will create a new one and return</param>
		/// <returns>the Q matrix and R matrix</returns>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		(TMat Q, TMat R) QR(bool full = false, TMat overwriteQ = null, TMat overwriteR = null);
		#endregion
	}

	/// <summary>
	/// The interface for tensor that contains basic members (size and label).
	/// </summary>
	public interface ITensor
	{
		#region members
		/// <summary>
		/// Get the rank of this tensor
		/// </summary>
		int Rank { get; }

		/// <summary>
		/// The label to mark each index of this tensor
		/// </summary>
		IReadOnlyList<char> Label { get; set; }
		#endregion

		#region method
		/// <summary>
		/// Set the label to mark each index of this tensor
		/// </summary>
		/// <param name="label">label to set</param>
		void SetLabel(params char[] label);
		#endregion
	}

	/// <summary>
	/// The interface for tensor that contains basic indexers whose inputs and outputs are not relevant with tensors.
	/// </summary>
	/// <typeparam name="T">the data type</typeparam>
	public interface ITensor<T> : ITensor where T : unmanaged, IFormattable, IEquatable<T>
	{
		#region indexer
		/// <summary>
		/// Get or set an element in of this tensor.
		/// </summary>
		/// <param name="pos">the indices of each rank</param>
		/// <returns>the value at <paramref name="pos"/></returns>
		T this[params Index[] pos] { get; set; }
		#endregion

		#region operation
		/// <summary>
		/// Calculate the absolute values' sum of this tensor
		/// </summary>
		/// <returns>the absolute sum</returns>
		double AbsSum();

		/// <summary>
		/// Calculate the maximum absolute value of this tensor
		/// </summary>
		/// <returns>the maximum absolute value</returns>
		double AbsMax();

		/// <summary>
		/// Calculate the sum of all values of this tensor
		/// </summary>
		/// <returns>the sum</returns>
		T Sum();

		/// <summary>
		/// Conjugate this tensor <b>in-place</b>
		/// </summary>
		void ConjugateInPlace();

		/// <summary>
		/// <b>In-place</b> dual this tensor without conjugate
		/// </summary>
		void DualInPlace();
		#endregion
	}

	/// <summary>
	/// The interface for tensor that contains more methods, operations and indexers.
	/// </summary>
	/// <typeparam name="T">the data type</typeparam>
	/// <typeparam name="TTen">the tensor type</typeparam>
	public interface ITensor<TTen, T> : ITensor<T>, IKrylovVector<TTen, T>
		where TTen : AbstractArray<T>, ITensor<TTen, T>
		where T : unmanaged, IFormattable, IEquatable<T>
	{
		#region operations
		/// <summary>
		/// Reshape this tensor to a new presenting <paramref name="size"/>.
		/// </summary>
		/// <param name="size">size/extent of new tensor</param>
		/// <returns>The reshaped new tensor. If the reshaped tensor is not contiguous in memory, it shall be re-ordered to be contiguous and thus requires manually disposition.</returns>
		TTen Reshape(params long[] size);

		/// <summary>
		/// Return a reference <typeparamref name="TTen"/> of this one with same properties
		/// </summary>
		/// <returns>A reference <typeparamref name="TTen"/> of this one</returns>
		TTen MakeReference();

		/// <summary>
		/// Contract two tensors <paramref name="A"/> and <paramref name="B"/>: $\text{this}_{i_0,i_1,...,i_n} = \alpha \sum_{j_a = k_b}{A_{j_0,j_1,...,j_p} \cdot B_{k_0,k_1,...,k_q}} + \beta C_{i_0,i_1,...,i_n}$;
		/// </summary>
		/// <param name="α">scalar α</param>
		/// <param name="A"><typeparamref name="TTen"/> A</param>
		/// <param name="B"><typeparamref name="TTen"/> B</param>
		/// <param name="β">scalar β, default 0</param>
		/// <param name="C"><typeparamref name="TTen"/> C, default null means this</param>
		/// <remarks>If <paramref name="C"/> is null, or <paramref name="β"/> is zero, this tensor itself will be used instead of <paramref name="C"/>.</remarks>
		void Contract(T α, TTen A, TTen B, T β = default, TTen C = null);

		/// <summary>
		/// Permute <paramref name="tensor"/> by <paramref name="order"/> and replace to this tensor
		/// </summary>
		/// <param name="tensor">the tensor to be permuted</param>
		/// <param name="order">the new permutation <see cref="TensorOrder"/>, zero-based</param>
		void Permute(TTen tensor, TensorOrder order);
		#endregion

		#region operators
		/// <summary>
		/// The <b>out-of-place</b> conjugate operator for this tensor.
		/// </summary>
		/// <returns>the conjugate tensor, if <typeparamref name="T"/> is a real type, this tensor itself will be returned</returns>
		TTen ConjugateOutOfPlace();

		/// <summary>
		/// The permute operator of this tensor.
		/// </summary>
		/// <param name="order">the new permutation <see cref="TensorOrder"/>, zero-based</param>
		/// <returns>the result tensor, a new <typeparamref name="TTen"/></returns>
		TTen OperatorPermute(TensorOrder order);

		/// <summary>
		/// Contraction operator for two tensors: this as left and <paramref name="right"/>.
		/// </summary>
		/// <param name="right">right operand</param>
		/// <param name="order">the order of the result tensor; if this parameter is null or empty, the order will be determined within</param>
		/// <returns>the contraction result, out-of-place</returns>
		/// <remarks>the <see cref="ITensor.Label"/> of operands will be utilized</remarks>
		TTen OperatorContract(TTen right, params char[] order);
		#endregion

		#region indexer
		/// <summary>
		/// Get the sub tensor formed by the first N rank of this tensor.
		/// </summary>
		/// <param name="firstNRank">first N ranks to set or get</param>
		/// <param name="restPos">rest of the tensor's rank's position <see cref="Index"/></param>
		/// <returns>the sub <typeparamref name="TTen"/> of the <paramref name="firstNRank"/> at <paramref name="restPos"/></returns>
		TTen GetSpan(int firstNRank, params Index[] restPos);

		/// <summary>
		/// Set the sub tensor formed by the first N rank of this tensor.
		/// </summary>
		/// <param name="value">the sub <typeparamref name="TTen"/> to set</param>
		/// <param name="firstNRank">first N ranks to set or get</param>
		/// <param name="restPos">rest of the tensor's rank's position <see cref="Index"/></param>
		void SetSpan(TTen value, int firstNRank, params Index[] restPos);
		#endregion
	}

	/// <summary>
	/// The interface of decomposable and matrix-multipliable tensor that contains matrix multiplication, SVD and QR.
	/// </summary>
	/// <typeparam name="T">the data type</typeparam>
	/// <typeparam name="TTen">the tensor type</typeparam>
	public interface ITensorAsMatrix<TTen, T>
		where TTen : AbstractArray<T>, ITensor<TTen, T>, ITensorAsMatrix<TTen, T>
		where T : unmanaged, IFormattable, IEquatable<T>
	{
		#region basics
		/// <summary>
		/// (Conjugate) transpose this tensor <b>out-of-place</b>.
		/// </summary>
		/// <param name="partition">a <see cref="Index"/> to indicate the first <paramref name="partition"/> (exclude) indices of this tensor will be regarded as the row and others column</param>
		/// <param name="conjugate">conjugate or not, default null means true for complex type (<see cref="Complex{T}"/>)</param>
		/// <returns>the (conjugate) transpose of this tensor with <c>Size = this.Size[<paramref name="partition"/>..] concatenate this.Size[..<paramref name="partition"/>]</c></returns>
		TTen Transpose(Index partition, bool? conjugate = null);

		/// <summary>
		/// Multiply this tensor as a matrix with the <paramref name="right"/> tensor as another matrix.
		/// </summary>
		/// <param name="right">the other <typeparamref name="TTen"/> as a matrix</param>
		/// <param name="partitionLeft">a <see cref="Index"/> to indicate the first <paramref name="partitionLeft"/> (exclude) indices of this tensor will be regarded as the row and others column</param>
		/// <param name="partitionRight">a <see cref="Index"/> to indicate the first <paramref name="partitionRight"/> (exclude) indices of tensor <paramref name="right"/> will be regarded as the row and others column</param>
		/// <param name="leftOp">the <see cref="MatrixOperation"/> to apply on this one</param>
		/// <param name="rightOp">the <see cref="MatrixOperation"/> to apply on <paramref name="right"/></param>
		/// <returns>The <b>out-of-place</b> multiplication result as a tensor whose <see cref="AbstractArray{T}.Size">size</see> is the same as corresponding <see cref="ITensor{TTen, T}.OperatorContract(TTen, char[])"/>.</returns>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="partitionLeft"/> or <paramref name="partitionRight"/> is out of range</exception>
		TTen OperatorMatrixMultiply(TTen right, Index partitionLeft, Index partitionRight, MatrixOperation leftOp = MatrixOperation.None, MatrixOperation rightOp = MatrixOperation.None);
		#endregion

		#region diagonal related
		/// <summary>
		/// Calculate the trace of this tensor as a matrix.
		/// </summary>
		/// <returns>the trace of this tensor as a matrix</returns>
		/// <exception cref="InvalidOperationException">if this tensor's shape is not a square matrix</exception>
		T Trace();

		/// <summary>
		/// Shift all the eigenvalues of this tensor by adding <paramref name="shift"/> to each diagonal elements of this tensor as a matrix.
		/// </summary>
		/// <param name="shift">the shift value, if it is zero, no operation shall be performed</param>
		/// <exception cref="InvalidOperationException">if this tensor's shape is not a square matrix</exception>
		void EigenvalueShift(T shift);
		#endregion

		#region decompose
		/// <summary>
		/// Compute the singular value decomposition (SVD) of this tensor and (optional) corresponding the left and/or right singular vectors: $A = U S V^*$ <b>out-of-place</b>, where $A$ is this matrix. Not necessarily sorted descending by singular values.
		/// </summary>
		/// <param name="partition">a <see cref="Index"/> to indicate the first <paramref name="partition"/> (exclude) indices of this tensor will be regarded as the row and others column</param>
		/// <param name="calcU">calculate the left singular vectors or not, if false, the return <c>U</c> will be null</param>
		/// <param name="calcV">calculate the right singular vectors or not, if false, the return <c>Vct</c> will be null</param>
		/// <returns>The singular values as a <see cref="double"/> array and (optional) left, right singular vectors.
		/// <list type="table">
		/// <listheader><term>Singular vectors</term><description>  Size</description></listheader>
		/// <item><term>Left</term><description>  <c><see cref="AbstractArray{T}.Size">size</see>[..<paramref name="partition"/>]</c> append <c>min(prod(<see cref="AbstractArray{T}.Size">size</see>[<paramref name="partition"/>..]), prod(<see cref="AbstractArray{T}.Size">size</see>[..<paramref name="partition"/>]))</c></description></item>
		/// <item><term>Right</term><description>  <c><see cref="AbstractArray{T}.Size">size</see>[<paramref name="partition"/>..]</c> prepend <c>min(prod(<see cref="AbstractArray{T}.Size">size</see>[<paramref name="partition"/>..]), prod(<see cref="AbstractArray{T}.Size">size</see>[..<paramref name="partition"/>]))</c></description></item>
		/// </list>
		/// </returns>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="partition"/> is out of range</exception>
		(double[] S, TTen U, TTen Vct) SingularValues(Index partition, bool calcU = true, bool calcV = true);

		/// <summary>
		/// Compute the singular value decomposition (SVD) of this tensor and corresponding the left and/or right singular vectors: $A = U S V^*$ <b>out-of-place</b>, where $A$ is this matrix. Not necessarily sorted descending by singular values. Then truncate the singular values $S$ and vectors $U$, $V^*$ to preserve at most <paramref name="maxPreserve"/> entries.
		/// </summary>
		/// <param name="partition">a <see cref="Index"/> to indicate the first <paramref name="partition"/> (exclude) indices of this tensor will be regarded as the row and others column</param>
		/// <param name="maxPreserve">the maximum number of singular values and vectors to preserve, must be positive</param>
		/// <returns>The singular values and left, right singular vectors with at most <paramref name="maxPreserve"/> entries.</returns>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="partition"/> is out of range</exception>
		(TTen S, TTen U, TTen Vct) SingularValuesTruncate(Index partition, int maxPreserve);

		/// <summary>
		/// QR factorize this tensor <b>out-of-place</b>.
		/// </summary>
		/// <param name="partition">a <see cref="Index"/> to indicate the first <paramref name="partition"/> (exclude) indices of this tensor will be regarded as the row and others column</param>
		/// <param name="full">perform full factorization or not</param>
		/// <returns>The (column) orthogonal Q matrix and upper-triangular R matrix.</returns>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="partition"/> is out of range</exception>
		(TTen Q, TTen R) QR(Index partition, bool full = false);

		/// <summary>
		/// LQ factorize this tensor <b>out-of-place</b>.
		/// </summary>
		/// <param name="partition">a <see cref="Index"/> to indicate the first <paramref name="partition"/> (exclude) indices of this tensor will be regarded as the row and others column</param>
		/// <param name="full">perform full factorization or not</param>
		/// <returns>The lower-triangular L matrix and (row) orthogonal Q matrix</returns>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="partition"/> is out of range</exception>
		public (TTen L, TTen Q) LQ(Index partition, bool full = false)
		{
			using var transposed = this.Transpose(partition);
			var (Q, R) = transposed.QR(partition, full);
			using (Q) using (R)
			{
				return (R.Transpose(partition), Q.Transpose(partition));
			}
		}
		#endregion
	}
}
