using System;
using System.Collections.Generic;

using Althea.NativeTypes;
using Althea.LinearAlgebra; // MatrixOperation
using Althea.TensorAlgebra; // TensorOrder


namespace Althea.Arrays
{
	/// <summary>
	/// Simple interface for sparse arrays, inherits <see cref="IReadOnlyList{T}"/> of <see cref="Storage{T}"/> of <typeparamref name="TIndex"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
	/// <typeparam name="TIndex">Any integer-typed unmanaged struct as the index data type</typeparam>
	public interface ISparseArray<T, TIndex> : IReadOnlyList<Storage<TIndex>>
		where T : unmanaged, IEquatable<T>
		where TIndex : unmanaged
	{
		#region property
		/// <summary>
		/// When implemented by a derived class, get the value array storage of this sparse array.
		/// </summary>
		Storage<T> Storage { get; }

		/// <summary>
		/// When implemented by a derived class, get the number of nonzero values of this sparse array.
		/// </summary>
		long NStored { get; }
		#endregion

		#region dispose
		/// <summary>
		/// When implemented by a derived class, dispose this sparse array after excluding the internal storages shared between this array and the target <paramref name="array"/>.
		/// </summary>
		/// <param name="array">The target <see cref="ISparseArray{T, TIndex}"/> to exclude before disposing</param>
		void DisposeExclude(ISparseArray<T, TIndex> array);
		#endregion
	}

	/// <summary>
	/// The interface of vector that contains the operation needed for Krylov-subspace methods such as Lanczos and Krylov-Schur solver.
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
	/// <typeparam name="TVec">The vector type</typeparam>
	public interface IKrylovVector<TVec, T>
		where TVec : class, IKrylovVector<TVec, T>, new()
		where T : unmanaged, IEquatable<T>
	{
		#region operation
		/// <summary>
		/// When implemented by a derived class, point-wisely in-place multiply this vector with given <paramref name="value"/>.
		/// </summary>
		/// <param name="value">The scalar as <typeparamref name="T"/> to multiply</param>
		void Scale(T value);

		/// <summary>
		/// When implemented by a derived class, compute the 2-norm (Euclidean norm) of elements in this vector.
		/// </summary>
		/// <returns>The 2-norm of this vector</returns>
		double Norm();

		/// <summary>
		/// When implemented by a derived class, in-place scale this vector such that its 2-norm (Euclidean norm) is one.
		/// </summary>
		/// <exception cref="DivideByZeroException">If the 2-norm of this array is 0</exception>
		void Normalize();

		/// <summary>
		/// When implemented by a derived class, compute dot (inner) product of this vector and <paramref name="other"/> vector. The conjugate of this vector shall be actually used.
		/// </summary>
		/// <param name="other">The other <typeparamref name="TVec"/> to perform the dot product</param>
		/// <returns>The dot (inner) product result as a <typeparamref name="T"/></returns>
		T Dot(TVec other);

		/// <summary>
		/// When implemented by a derived class, add the <paramref name="other"/> (scaling by <paramref name="scalar"/>) to this vector in-place.
		/// </summary>
		/// <param name="other">The other <typeparamref name="TVec"/> to add</param>
		/// <param name="scalar">The scalar to be multiplied to <paramref name="other"/> of type <typeparamref name="T"/></param>
		void AddByVector(TVec other, T scalar);

		/// <summary>
		/// When implemented by a derived class, replace this vector's content with the <paramref name="other"/> vector in-place.
		/// </summary>
		/// <param name="other">The other <typeparamref name="TVec"/> to replace from</param>
		/// <exception cref="InvalidOperationException">If the replacement cannot be done in-place due to reason(s) such as different sparsities between this and <paramref name="other"/></exception>
		void ReplaceBy(TVec other);

		/// <summary>
		/// When implemented by a derived class, multiply the matrix whose columns are indicated by <paramref name="unjoinedVectors"/> to a dense vector indicated by a <see cref="ReadOnlySpan{T}"/> and obtain the result vector as a <see cref="VectorBase{T}"/>.
		/// </summary>
		/// <param name="unjoinedVectors">The columns of the matrix to be multiplied</param>
		/// <param name="input">The input dense vector to be multiplied as a <see cref="ReadOnlySpan{T}"/></param>
		/// <returns>The product of <paramref name="unjoinedVectors"/> and <paramref name="input"/> as a <see cref="VectorBase{T}"/></returns>
		/// <remarks>The method shall be basically static, the information of this vector shall only be used to verify the consistency of <paramref name="unjoinedVectors"/></remarks>
		TVec OperateOn(IReadOnlyList<TVec> unjoinedVectors, ReadOnlySpan<T> input);
		#endregion
	}

	/// <summary>
	/// The interface for sparse vectors without indicating the index data type whose value array is <see cref="ISparseVector{T}.Storage"/> and index array(s) is/are the inherited <see cref="IReadOnlyList{T}"/> of <see cref="IStorage"/>s.
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
	public interface ISparseVector<T> : IReadOnlyList<IStorage> where T : unmanaged
	{
		#region properties
		/// <summary>
		/// When implemented by a derived class, get the value array storage of this sparse vector
		/// </summary>
		Storage<T> Storage { get; }

		/// <summary>
		/// When implemented by a derived class, get the number of stored values of this sparse array. The default implementation returns the length of <see cref="ISparseVector{T}.Storage"/>.
		/// </summary>
		long NStored => this.Storage.Length;

		/// <summary>
		/// When implemented by a derived class, get the data type of the index array(s) of this sparse vector as a <see cref="DataType"/>
		/// </summary>
		DataType IndexType { get; }

		/// <summary>
		/// When implemented by a derived class, get the default value of this sparse vector
		/// </summary>
		T DefaultValue { get; }

		/// <summary>
		/// When implemented by a derived class, get the format of this sparse vector as a <see cref="LinearAlgebra.Sparse.SparseVectorFormat"/>
		/// </summary>
		LinearAlgebra.Sparse.SparseVectorFormat Format { get; }
		#endregion
	}


	#region matrices
	/// <summary>
	/// The interface of matrix that contains basic members, methods, operations and indexers whose inputs and outputs are not relevant with matrix.
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
	public interface IMatrix<T> where T : unmanaged, IEquatable<T>
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
	/// The interface for dense storage matrices
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
	public interface IDenseMatrix<T> : IDenseArray<T>, IMatrix<T> where T : unmanaged, IEquatable<T>
	{
		// empty
	}

	/// <summary>
	/// The interface for sparse storage matrices
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
	/// <typeparam name="TIndex">Any integer-typed unmanaged struct as the data type of index array</typeparam>
	public interface ISparseMatrix<T, TIndex> : ISparseArray<T, TIndex>, IMatrix<T> where T : unmanaged, IEquatable<T> where TIndex : unmanaged
	{
		#region property
		/// <summary>
		/// The underlying <see cref="Storage{T}"/> of type <typeparamref name="TIndex"/> for the row index array of this sparse vector
		/// </summary>
		Storage<TIndex> RowIndexStorage { get; }

		/// <summary>
		/// The underlying <see cref="Storage{T}"/> of type <typeparamref name="TIndex"/> for the column index array of this sparse vector
		/// </summary>
		Storage<TIndex> ColumnIndexStorage { get; }
		#endregion
	}

	/// <summary>
	/// The interface of matrix that contains basic members, methods, operations and indexers whose inputs and outputs are relevant with matrix.
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
	/// <typeparam name="TMat">The matrix type</typeparam>
	public interface IMatrix<TMat, T> : IMatrix<T>, IKrylovVector<TMat, T>
		where TMat : class, IMatrix<TMat, T>, new()
		where T : unmanaged, IEquatable<T>
	{
		#region method
		/// <summary>
		/// Get a new matrix by the column index range.
		/// </summary>
		/// <param name="columnRange"><see cref="Range"/> of columns</param>
		/// <param name="overwrite">The output <typeparamref name="TMat"/> to overwrite, default null means creating a ref matrix</param>
		/// <returns>A matrix constructed by these columns. If <paramref name="overwrite"/> does not fit, it will not be returned.</returns>
		TMat GetColumnRange(Range columnRange, TMat? overwrite = null);

		/// <summary>
		/// Get a new matrix by the row index range.
		/// </summary>
		/// <param name="rowRange"><see cref="Range"/> of rows</param>
		/// <param name="overwrite">The output <typeparamref name="TMat"/> to overwrite, default null means creating a ref matrix</param>
		/// <returns>A matrix constructed by these rows. If <paramref name="overwrite"/> does not fit, it will not be returned.</returns>
		TMat GetRowRange(Range rowRange, TMat? overwrite = null);

		/// <summary>
		/// Get a sub-matrix by the row and column index ranges.
		/// </summary>
		/// <param name="rowRange"><see cref="Range"/> of rows</param>
		/// <param name="columnRange"><see cref="Range"/> of columns</param>
		/// <param name="overwrite">The output <typeparamref name="TMat"/> to overwrite, default null means creating a ref matrix (if possible)</param>
		/// <returns>A sub-matrix in this region. If <paramref name="overwrite"/> does not fit, it will not be returned.</returns>
		TMat GetSubmatrix(Range rowRange, Range columnRange, TMat? overwrite = null);
		#endregion

		#region operation
		/// <summary>
		/// Calculate the transpose of this matrix. A new <see cref="IMatrix{T}"/> will be created if the result is not it self.
		/// </summary>
		/// <param name="overwrite">The output <typeparamref name="TMat"/> to overwrite, default null means creating a new matrix</param>
		/// <returns>The transposed <typeparamref name="TMat"/>.</returns>
		TMat Transpose(TMat? overwrite = null);

		/// <summary>
		/// Calculate the conjugate transpose of this matrix. A new <see cref="IMatrix{T}"/> will be created if the result is not it self.
		/// </summary>
		/// <param name="overwrite">The output <typeparamref name="TMat"/> to overwrite, default null means creating a new matrix</param>
		/// <returns>The conjugate-transposed <typeparamref name="TMat"/>.</returns>
		TMat ConjugateTranspose(TMat? overwrite = null);

		/// <summary>
		/// Symmetrize this matrix by adding its conjugate transpose out-of-place.
		/// </summary>
		/// <param name="conjugateAtLast">return the original </param>
		/// <param name="overwrite">The output <typeparamref name="TMat"/> to overwrite, default null means creating a new matrix</param>
		/// <returns>If <c><paramref name="conjugateAtLast"/> == false</c>: $B_{\text{result}}=\frac{A + A^H}{2}$; otherwise: $B_{\text{result}}=\frac{\bar{A} + A^T}{2}$</returns>
		TMat Symmetrize(bool conjugateAtLast = false, TMat? overwrite = null);

		/// <summary>
		/// Compute $C_{\text{this}} = \alpha A^{\text{opA}} + \beta B^{\text{opB}}$. This method will try to in-place replace this matrix.
		/// </summary>
		/// <param name="α">scalar of type <typeparamref name="T"/> with default 0. If <c><paramref name="α"/> == 0</c>, <paramref name="A"/> can be an invalid input</param>
		/// <param name="A">The <typeparamref name="TMat"/> A, can be null</param>
		/// <param name="opA">operation to matrix <paramref name="A"/></param>
		/// <param name="β">scalar of type <typeparamref name="T"/> with default 0. If <c><paramref name="β"/> == 0</c>, <paramref name="B"/> can be an invalid input</param>
		/// <param name="B">The input <typeparamref name="TMat"/> B, can be null</param>
		/// <param name="opB">operation to matrix <paramref name="B"/></param>
		void From_αA_Add_βB(TMat A, TMat B, T α = default, T β = default, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None);

		/// <summary>
		/// Compute $C_{\text{this}} = \alpha A^{\text{opA}} B^{\text{opB}} + \beta C_{\text{this}}$. This method will try to in-place replace this matrix.
		/// </summary>
		/// <param name="A">The input <typeparamref name="TMat"/> A</param>
		/// <param name="opA">operation to matrix <paramref name="A"/></param>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		/// <param name="B">The input <typeparamref name="TMat"/> B</param>
		/// <param name="opB">operation to matrix <paramref name="B"/></param>
		/// <param name="β">scalar of type <typeparamref name="T"/> with default 0</param>
		void Mulβ_AddBy_αAB(TMat A, TMat B, T α, T β = default, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None);

		/// <summary>
		/// Compute Kronecker product $A \otimes B$. If <paramref name="forceHerm"/> is true, then $(A \otimes B^H + A^H \otimes B)/2$ will be calculated.
		/// </summary>
		/// <param name="other">The other <typeparamref name="TMat"/> B at right</param>
		/// <param name="forceHerm">if the result is made Hermitian or not</param>
		/// <param name="overwrite">The <typeparamref name="TMat"/> to overwrite by result, default null</param>
		/// <returns>The result of Kronecker product, a new <typeparamref name="TMat"/> or <paramref name="overwrite"/> if it is not null.</returns>
		TMat KroneckerProd(TMat other, bool forceHerm = true, TMat? overwrite = null);

		/// <summary>
		/// Compute Kronecker sum $A \oplus B \equiv A \otimes I + I \otimes B$. If <paramref name="forceHerm"/> is true, then $[(A \otimes I + I \otimes B^H) + (A^H \otimes I + I \otimes B)]/2$ will be calculated.
		/// </summary>
		/// <param name="other">The other <typeparamref name="TMat"/> B at right</param>
		/// <param name="forceHerm">if the result is made Hermitian or not</param>
		/// <param name="overwrite">The <typeparamref name="TMat"/> to overwrite by result, default null</param>
		/// <returns>The result of Kronecker sum, a new <typeparamref name="TMat"/> or <paramref name="overwrite"/> if it is not null.</returns>
		TMat KroneckerSum(TMat other, bool forceHerm = true, TMat? overwrite = null);
		#endregion
	}

	/// <summary>
	/// The interface of matrix that contains extra methods, operations and indexers whose inputs and outputs are also relevant with vector.
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
	/// <typeparam name="TMat">The matrix type</typeparam>
	/// <typeparam name="TVec">The vector type</typeparam>
	public interface IMatrix<TMat, TVec, T> : IMatrix<TMat, T>
		where TMat : class, IMatrix<TMat, TVec, T>, new()
		where TVec : class, IVector<TVec, T>, new()
		where T : unmanaged, IEquatable<T>
	{
		#region method
		/// <summary>
		/// Join the array of <typeparamref name="TVec"/> forming into a <typeparamref name="TMat"/> overwriting this matrix.
		/// </summary>
		/// <param name="vectors">The input array of <typeparamref name="TVec"/></param>
		void FromColumnVectors(TVec[] vectors);

		/// <summary>
		/// Get part of the column vectors that forms the matrix.
		/// </summary>
		/// <param name="colRange">The <see cref="Range"/> of columns</param>
		/// <param name="overwrite">The output array of <typeparamref name="TVec"/> to overwrite, default null means creating ref vectors if possible</param>
		/// <returns>An array of data type <typeparamref name="TVec"/>. If <paramref name="overwrite"/> does not fit, it will not be returned.</returns>
		TVec[] GetColumns(Range colRange, TVec[]? overwrite = null);

		/// <summary>
		/// Get part of the row vectors that forms the matrix.
		/// </summary>
		/// <param name="rowRange">The <see cref="Range"/> of rows</param>
		/// <param name="overwrite">The output array of <typeparamref name="TVec"/> to overwrite, default null means creating ref vectors if possible</param>
		/// <returns>An array of data type <typeparamref name="TVec"/>. If <paramref name="overwrite"/> does not fit, it will not be returned.</returns>
		TVec[] GetRows(Range rowRange, TVec[]? overwrite = null);

		/// <summary>
		/// Get all of the column vectors that forms the matrix.
		/// </summary>
		/// <param name="overwrite">The output array of <typeparamref name="TVec"/> to overwrite, default null means creating ref vectors if possible</param>
		/// <returns>An array of data type <typeparamref name="TVec"/>. If <paramref name="overwrite"/> does not fit, it will not be returned.</returns>
		TVec[] GetColumns(TVec[]? overwrite = null);

		/// <summary>
		/// Get all of the row vectors that forms the matrix.
		/// </summary>
		/// <param name="overwrite">The output array of <typeparamref name="TVec"/> to overwrite, default null means creating ref vectors if possible</param>
		/// <returns>An array of data type <typeparamref name="TVec"/>. If <paramref name="overwrite"/> does not fit, it will not be returned.</returns>
		TVec[] GetRows(TVec[]? overwrite = null);

		/// <summary>
		/// Get one column of the matrix.
		/// </summary>
		/// <param name="index">The <see cref="Index"/> of column</param>
		/// <param name="overwrite">The output <typeparamref name="TVec"/> to overwrite, default null means creating a ref vector if possible</param>
		/// <returns>The selected column as a <typeparamref name="TVec"/>. If <paramref name="overwrite"/> does not fit, it will not be returned.</returns>
		TVec GetColumnAt(Index index, TVec? overwrite = null);

		/// <summary>
		/// Get one row of the matrix.
		/// </summary>
		/// <param name="index">The <see cref="Index"/> of row</param>
		/// <param name="overwrite">The output <typeparamref name="TVec"/> to overwrite, default null means creating a ref vector if possible</param>
		/// <returns>The selected row as a <typeparamref name="TVec"/>. If <paramref name="overwrite"/> does not fit, it will not be returned.</returns>
		TVec GetRowAt(Index index, TVec? overwrite = null);
		#endregion

		#region diagonal indexer
		/// <summary>
		/// The method to get diagonal elements.
		/// </summary>
		/// <param name="k">diagonal index, 0 for diag, 1 for super-diagonal at one above, -1 for sub-diagonal at one below, etc.</param>
		/// <param name="overwrite">The output <typeparamref name="TVec"/> to overwrite, default null means creating a new vector</param>
		/// <returns>A new <typeparamref name="TVec"/> containing the (super-/sub-)diagonal elements. If <paramref name="overwrite"/> does not fit, it will not be returned.</returns>
		TVec GetDiag(long k, TVec? overwrite = null);

		/// <summary>
		/// The method to set diagonal elements.
		/// </summary>
		/// <param name="k">diagonal index, 0 for diag, 1 for super-diagonal at one above, -1 for sub-diagonal at one below, etc.</param>
		/// <param name="vec">The <typeparamref name="TVec"/></param>
		void SetDiag(long k, TVec vec);
		#endregion

		#region decompositions
		/// <summary>
		/// Calculate the eigenvalues (and eigenvectors) of this Hermitian matrix for the special eigen-problem -- $A V = V \Lambda$, or matrices pair A, <paramref name="B"/> for the general one -- $A V = \Lambda B V$ or $A B V = \Lambda V$ or $B A V = \Lambda V$ <b>out-of-place</b>. Here, matrix A is this matrix.
		/// </summary>
		/// <param name="B">The input <typeparamref name="TMat"/> to calculate general eigen-problem; if <c><paramref name="B"/> is null</c>, the normal eigen is performed and <paramref name="type"/> is not used; otherwise, the general one is performed</param>
		/// <param name="type">The <see cref="Solver.EigType"/> to indicate positions of this matrix and <paramref name="B"/></param>
		/// <returns>The eigenvalues</returns>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		TVec EigenvalueHerm(TMat B = null, Solver.EigType type = Solver.EigType.Type1);

		/// <summary>
		/// Calculate the eigenvalues (and eigenvectors) of this Hermitian matrix for the special eigen-problem -- $A V = V \Lambda$, or matrices pair A, <paramref name="B"/> for the general one -- $A V = \Lambda B V$ or $A B V = \Lambda V$ or $B A V = \Lambda V$ <b>out-of-place</b>. Here, matrix A is this matrix.
		/// </summary>
		/// <param name="overwriteValues">The <typeparamref name="TVec"/> to store eigenvalues, default null means that this method will create a new one and return</param>
		/// <param name="overwriteVectors">The <typeparamref name="TMat"/> to store eigenvectors, default null means that this method will create a new one and return</param>
		/// <param name="B">The input <typeparamref name="TMat"/> to calculate general eigen-problem; if <c><paramref name="B"/> is null</c>, the normal eigen is performed and <paramref name="type"/> is not used; otherwise, the general one is performed</param>
		/// <param name="type">The <see cref="Solver.EigType"/> to indicate positions of this matrix and <paramref name="B"/></param>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		(TVec values, TMat vectors) EigensystemHerm(TVec overwriteValues = null, TMat overwriteVectors = null, TMat B = null, Solver.EigType type = Solver.EigType.Type1);

		/// <summary>
		/// Compute the singular value decomposition (SVD) of this matrix and corresponding the left and/or right singular vectors: $A = U S V^*$ <b>out-of-place</b>, where A is this matrix.
		/// </summary>
		/// <param name="overwriteS">The <typeparamref name="TVec"/> to store singular values, default null means that this method will create a new one and return</param>
		/// <param name="overwriteU">The <typeparamref name="TMat"/> to store left singular vectors, default null means that this method will create a new one and return</param>
		/// <param name="overwriteVct">The <typeparamref name="TMat"/> to store right singular vectors, default null means that this method will create a new one and return</param>
		/// <param name="calcU">calculate the left singular vectors or not, if false, the return <c>U</c> will be null</param>
		/// <param name="calcV">calculate the right singular vectors or not, if false, the return <c>Vct</c> will be null</param>
		/// <returns>the singular values and left, right singular vectors</returns>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		(TVec S, TMat U, TMat Vct) SingularValues(TVec overwriteS = null, TMat overwriteU = null, TMat overwriteVct = null, bool calcU = true, bool calcV = true);

		/// <summary>
		/// QR factorize this matrix <b>out-of-place</b>.
		/// </summary>
		/// <param name="full">perform full factorization or not</param>
		/// <param name="overwriteQ">The <typeparamref name="TMat"/> to store triangular matrix Q, default null means that this method will create a new one and return</param>
		/// <param name="overwriteR">The <typeparamref name="TMat"/> to store triangular matrix R, default null means that this method will create a new one and return</param>
		/// <returns>the Q matrix and R matrix</returns>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		(TMat Q, TMat R) QR(bool full = false, TMat overwriteQ = null, TMat overwriteR = null);
		#endregion
	}
	#endregion

	#region tensors
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
	/// <typeparam name="T">The data type</typeparam>
	public interface ITensor<T> : ITensor where T : unmanaged, IEquatable<T>
	{
		#region indexer
		/// <summary>
		/// Get or set an element in of this tensor.
		/// </summary>
		/// <param name="pos">The indices of each rank</param>
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
	/// <typeparam name="T">The data type</typeparam>
	/// <typeparam name="TTen">The tensor type</typeparam>
	public interface ITensor<TTen, T> : ITensor<T>, IKrylovVector<TTen, T>
		where TTen : class, ITensor<TTen, T>, new()
		where T : unmanaged, IEquatable<T>
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
		/// <param name="tensor">The tensor to be permuted</param>
		/// <param name="order">The new permutation <see cref="TensorOrder"/>, zero-based</param>
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
		/// <param name="order">The new permutation <see cref="TensorOrder"/>, zero-based</param>
		/// <returns>the result tensor, a new <typeparamref name="TTen"/></returns>
		TTen OperatorPermute(TensorOrder order);

		/// <summary>
		/// Contraction operator for two tensors: this as left and <paramref name="right"/>.
		/// </summary>
		/// <param name="right">right operand</param>
		/// <param name="order">The order of the result tensor; if this parameter is null or empty, the order will be determined within</param>
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
		/// <param name="value">The sub <typeparamref name="TTen"/> to set</param>
		/// <param name="firstNRank">first N ranks to set or get</param>
		/// <param name="restPos">rest of the tensor's rank's position <see cref="Index"/></param>
		void SetSpan(TTen value, int firstNRank, params Index[] restPos);
		#endregion
	}

	/// <summary>
	/// The interface of decomposable and matrix-multipliable tensor that contains matrix multiplication, SVD and QR.
	/// </summary>
	/// <typeparam name="T">The data type</typeparam>
	/// <typeparam name="TTen">The tensor type</typeparam>
	public interface ITensorAsMatrix<TTen, T>
		where TTen : class, IDisposable, ITensor<TTen, T>, ITensorAsMatrix<TTen, T>, new()
		where T : unmanaged, IEquatable<T>
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
		/// <param name="right">The other <typeparamref name="TTen"/> as a matrix</param>
		/// <param name="partitionLeft">a <see cref="Index"/> to indicate the first <paramref name="partitionLeft"/> (exclude) indices of this tensor will be regarded as the row and others column</param>
		/// <param name="partitionRight">a <see cref="Index"/> to indicate the first <paramref name="partitionRight"/> (exclude) indices of tensor <paramref name="right"/> will be regarded as the row and others column</param>
		/// <param name="leftOp">The <see cref="MatrixOperation"/> to apply on this one</param>
		/// <param name="rightOp">The <see cref="MatrixOperation"/> to apply on <paramref name="right"/></param>
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
		/// <param name="shift">The shift value, if it is zero, no operation shall be performed</param>
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
		/// <param name="maxPreserve">The maximum number of singular values and vectors to preserve, must be positive</param>
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
	#endregion
}
