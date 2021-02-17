using System;
using System.Runtime.InteropServices;

using Althea.Arrays;
using Althea.Storage;


namespace Althea.SparseBlas
{
	#region converter
	internal static class Converter
	{
		internal static SparseVectorWrapper<T> ToWrapper<T>(this AbstractSparseVector<T> vector) where T : struct, IComparable<T>
		{
			return new SparseVectorWrapper<T>(vector.Storage, vector.IndexPointer);
		}

		internal static SparseMatrixWrapper<T> ToWrapper<T>(this SparseMatrix<T> matrix) where T : struct, IComparable<T>
		{
			return new SparseMatrixWrapper<T>(matrix.Storage, matrix.RowPointer, matrix.ColumnPointer);
		}
	}
	#endregion

	#region wrapper
	/// <summary>
	/// The sparse vector wrapper
	/// </summary>
	/// <typeparam name="T">see <see cref="AbstractArray{T}"/></typeparam>
	public readonly struct SparseVectorWrapper<T> : IEquatable<SparseVectorWrapper<T>> where T : struct, IComparable<T>
	{
		/// <summary>
		/// value array
		/// </summary>
		public Storage<T> Values { get; }
		/// <summary>
		/// index array
		/// </summary>
		public Storage<int> Indices { get; }

		/// <summary>
		/// Creator
		/// </summary>
		/// <param name="val"></param>
		/// <param name="ind"></param>
		public SparseVectorWrapper(Storage<T> val, Storage<int> ind)
		{
			this.Values = val ?? throw new ArgumentNullException(nameof(val));
			this.Indices = ind ?? throw new ArgumentNullException(nameof(ind));
		}

		/// <summary>
		/// Equals
		/// </summary>
		/// <param name="obj">another object</param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			if (obj is null)
				return false;
			if (obj is SparseVectorWrapper<T> sv)
				return this.Equals(sv);
			return false;
		}

		/// <summary>
		/// Get hash code
		/// </summary>
		/// <returns>the hash code</returns>
		public override int GetHashCode() => HashCode.Combine(this.Values, this.Indices);

		/// <summary>
		/// Equals
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator ==(SparseVectorWrapper<T> left, SparseVectorWrapper<T> right)
		{
			return left.Equals(right);
		}

		/// <summary>
		/// Not equals
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator !=(SparseVectorWrapper<T> left, SparseVectorWrapper<T> right)
		{
			return !(left == right);
		}

		/// <summary>
		/// Equals
		/// </summary>
		/// <param name="other">other <see cref="SparseVectorWrapper{T}"/></param>
		/// <returns></returns>
		public bool Equals(SparseVectorWrapper<T> other) => this.Values == other.Values && this.Indices == other.Indices;
	}

	/// <summary>
	/// The sparse matrix wrapper
	/// </summary>
	/// <typeparam name="T">see <see cref="AbstractArray{T}"/></typeparam>
	public readonly struct SparseMatrixWrapper<T> : IEquatable<SparseMatrixWrapper<T>> where T : struct, IComparable<T>
	{
		/// <summary>
		/// value array
		/// </summary>
		public Storage<T> Values { get; }
		/// <summary>
		/// row index/pointer array
		/// </summary>
		public Storage<int> Row { get; }

		/// <summary>
		/// column index/pointer array
		/// </summary>
		public Storage<int> Column { get; }

		/// <summary>
		/// Creator
		/// </summary>
		/// <param name="val"></param>
		/// <param name="row"></param>
		/// <param name="col"></param>
		public SparseMatrixWrapper(Storage<T> val, Storage<int> row, Storage<int> col)
		{
			this.Values = val ?? throw new ArgumentNullException(nameof(val));
			this.Row = row ?? throw new ArgumentNullException(nameof(row));
			this.Column = col ?? throw new ArgumentNullException(nameof(col));
		}

		/// <summary>
		/// Equals
		/// </summary>
		/// <param name="obj">another object</param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			if (obj is null)
				return false;
			if (obj is SparseVectorWrapper<T> sv)
				return this.Equals(sv);
			return false;
		}

		/// <summary>
		/// Get hash code
		/// </summary>
		/// <returns>the hash code</returns>
		public override int GetHashCode() => HashCode.Combine(this.Values, this.Row, this.Column);

		/// <summary>
		/// Equals
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator ==(SparseMatrixWrapper<T> left, SparseMatrixWrapper<T> right)
		{
			return left.Equals(right);
		}

		/// <summary>
		/// Not equals
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator !=(SparseMatrixWrapper<T> left, SparseMatrixWrapper<T> right)
		{
			return !(left == right);
		}

		/// <summary>
		/// Equals
		/// </summary>
		/// <param name="other">other <see cref="SparseMatrixWrapper{T}"/></param>
		/// <returns></returns>
		public bool Equals(SparseMatrixWrapper<T> other) => this.Values == other.Values && this.Row == other.Row && this.Column == other.Column;
	}
	#endregion


	/// <summary>
	/// The BLAS routine interface
	/// </summary>
	public interface ISparse : IDisposable
	{
		#region vector
		/// <summary>
		/// Scatter the sparse vector x to a dense vector y. $\vec{y}[x_{\text{ind}}] = \vec{x}_{\text{val}}$
		/// </summary>
		/// <param name="x">sparse vector x</param>
		/// <param name="y">dense vector y</param>
		void VectorSparseToDense<T>(SparseVectorWrapper<T> x, Storage<T> y) where T : unmanaged;

		/// <summary>
		/// Scatter the sparse vector x to a dense vector y. $\vec{y}[x_{\text{ind}}] = \vec{x}_{\text{val}}$
		/// </summary>
		/// <param name="x">sparse vector x</param>
		/// <param name="y">dense vector y</param>
		public delegate void DelegateVectorSparseToDense<T>(SparseVectorWrapper<T> x, Storage<T> y) where T : unmanaged;

		// Ignore Spelling: pos
		/// <summary>
		/// Gather the vector <paramref name="x"/> at <paramref name="pos"/> into <paramref name="y"/>: $\vec{y}=\vec{x}[\text{pos}]$.
		/// </summary>
		/// <param name="x">vector to gather from</param>
		/// <param name="pos">indices to gather</param>
		/// <param name="y">vector to gather to</param>
		/// <param name="n">length of <paramref name="pos"/></param>
		void VectorGatherAtIndices<T>(Storage<T> x, Storage<int> pos, Storage<T> y, int n) where T : unmanaged;

		/// <summary>
		/// Gather the vector <paramref name="x"/> at <paramref name="pos"/> into <paramref name="y"/>: $\vec{y}=\vec{x}[\text{pos}]$.
		/// </summary>
		/// <param name="x">vector to gather from</param>
		/// <param name="pos">indices to gather</param>
		/// <param name="y">vector to gather to</param>
		/// <param name="n">length of <paramref name="pos"/></param>
		public delegate void DelegateVectorGatherAtIndices<T>(Storage<T> x, Storage<int> pos, Storage<T> y, int n) where T : unmanaged;

		/// <summary>
		/// Convert the dense vector y to sparse vector x with truncation <paramref name="threshold"/>.
		/// </summary>
		/// <param name="y">dense vector y</param>
		/// <param name="n">length of vector</param>
		/// <param name="threshold">The abs value below it will regarded as zero</param>
		/// <returns>a <see cref="SparseVectorWrapper{T}"/></returns>
		SparseVectorWrapper<T> VectorDenseToSparse<T>(Storage<T> y, int n, float threshold = 0) where T : unmanaged;

		/// <summary>
		/// Convert the dense vector y to sparse vector x with truncation <paramref name="threshold"/>.
		/// </summary>
		/// <param name="y">dense vector y</param>
		/// <param name="n">length of vector</param>
		/// <param name="threshold">The abs value below it will regarded as zero</param>
		/// <returns>a <see cref="SparseVectorWrapper{T}"/></returns>
		public delegate SparseVectorWrapper<T> DelegateVectorDenseToSparse<T>(Storage<T> y, int n, float threshold = 0) where T : unmanaged;

		/// <summary>
		/// Add the sparse vector x to a dense vector y. $y[x_{\text{ind}}] += \alpha x_{\text{val}}$
		/// </summary>
		/// <param name="alpha">scalar to multiply x</param>
		/// <param name="x">sparse vector x</param>
		/// <param name="y">dense vector y</param>
		void VectorSparseAddToDense<T>(T alpha, SparseVectorWrapper<T> x, Storage<T> y) where T : unmanaged;

		/// <summary>
		/// Add the sparse vector x to a dense vector y. $y[x_{\text{ind}}] += \alpha x_{\text{val}}$
		/// </summary>
		/// <param name="alpha">scalar to multiply x</param>
		/// <param name="x">sparse vector x</param>
		/// <param name="y">dense vector y</param>
		public delegate void DelegateVectorSparseAddToDense<T>(T alpha, SparseVectorWrapper<T> x, Storage<T> y) where T : unmanaged;

		/// <summary>
		/// Calculate the dot product of a sparse vector and a dense vector. $\vec{x} \cdot \vec{y}$
		/// </summary>
		/// <param name="n">length of vectors</param>
		/// <param name="x">sparse vector x</param>
		/// <param name="y">dense vector y</param>
		/// <param name="conjX">conjugate <paramref name="x"/> or not</param>
		/// <returns>output dot result</returns>
		T VectorSparseDotDense<T>(int n, SparseVectorWrapper<T> x, Storage<T> y, bool conjX) where T : unmanaged;

		/// <summary>
		/// Calculate the dot product of a sparse vector and a dense vector. $\vec{x} \cdot \vec{y}$
		/// </summary>
		/// <param name="n">length of vectors</param>
		/// <param name="x">sparse vector x</param>
		/// <param name="y">dense vector y</param>
		/// <param name="conjX">conjugate <paramref name="x"/> or not</param>
		/// <returns>output dot result</returns>
		public delegate T DelegateVectorSparseDotDense<T>(int n, SparseVectorWrapper<T> x, Storage<T> y, bool conjX) where T : unmanaged;

		/// <summary>
		/// Add the sparse vector <paramref name="x"/> to another sparse vector <paramref name="y"/>.
		/// </summary>
		/// <param name="x">sparse vector x</param>
		/// <param name="y">sparse vector y</param>
		/// <returns></returns>
		SparseVectorWrapper<T> VectorSparseAddSparse<T>(SparseVectorWrapper<T> x, SparseVectorWrapper<T> y) where T : unmanaged;

		/// <summary>
		/// Add the sparse vector <paramref name="x"/> to another sparse vector <paramref name="y"/>.
		/// </summary>
		/// <param name="x">sparse vector x</param>
		/// <param name="y">sparse vector y</param>
		/// <returns></returns>
		public delegate SparseVectorWrapper<T> DelegateVectorSparseAddSparse<T>(SparseVectorWrapper<T> x, SparseVectorWrapper<T> y) where T : unmanaged;

		/// <summary>
		/// Point-wise multiply or divide a sparse vector and a dense vector.
		/// </summary>
		/// <param name="x">sparse vector x</param>
		/// <param name="y">dense vector y</param>
		/// <param name="multiply">do multiplication or division</param>
		void VectorSparsePointWiseMultiplyDivideDense<T>(SparseVectorWrapper<T> x, Storage<T> y, bool multiply) where T : unmanaged;

		/// <summary>
		/// Point-wise multiply or divide a sparse vector and a dense vector.
		/// </summary>
		/// <param name="x">sparse vector x</param>
		/// <param name="y">dense vector y</param>
		/// <param name="multiply">do multiplication or division</param>
		public delegate void DelegateVectorSparsePointWiseMultiplyDivideDense<T>(SparseVectorWrapper<T> x, Storage<T> y, bool multiply) where T : unmanaged;
		#endregion

		#region vector and matrix
		/// <summary>
		/// Compute the sparse matrix dense vector multiplication: $\vec{y} = \alpha \cdot M^\text{op} \vec{x} + \beta \cdot \vec{y}$
		/// </summary>
		/// <param name="op">operation to <paramref name="M"/></param>
		/// <param name="m">number of rows of <paramref name="M"/> before <paramref name="op"/></param>
		/// <param name="n">number of columns of <paramref name="M"/> before <paramref name="op"/></param>
		/// <param name="M">sparse matrix M</param>
		/// <param name="format">sparse format of <paramref name="M"/></param>
		/// <param name="x">dense vector x</param>
		/// <param name="y">dense vector y</param>
		/// <param name="α">scalar to multiply <paramref name="M"/></param>
		/// <param name="β">scalar to multiply <paramref name="y"/></param>
		void MatrixVectorSparseMultiplyDense<T>(MatrixOperation op, int m, int n, SparseMatrixWrapper<T> M, SparseMatrixFormat format, Storage<T> x, Storage<T> y, T α, T β) where T : unmanaged;

		/// <summary>
		/// Compute the sparse matrix dense vector multiplication: $\vec{y} = \alpha \cdot M^\text{op} \vec{x} + \beta \cdot \vec{y}$
		/// </summary>
		/// <param name="op">operation to <paramref name="M"/></param>
		/// <param name="m">number of rows of <paramref name="M"/> before <paramref name="op"/></param>
		/// <param name="n">number of columns of <paramref name="M"/> before <paramref name="op"/></param>
		/// <param name="M">sparse matrix M</param>
		/// <param name="format">sparse format of <paramref name="M"/></param>
		/// <param name="x">dense vector x</param>
		/// <param name="y">dense vector y</param>
		/// <param name="α">scalar to multiply <paramref name="M"/></param>
		/// <param name="β">scalar to multiply <paramref name="y"/></param>
		public delegate void DelegateMatrixVectorSparseMultiplyDense<T>(MatrixOperation op, int m, int n, SparseMatrixWrapper<T> M, SparseMatrixFormat format, Storage<T> x, Storage<T> y, T α, T β) where T : unmanaged;

		/// <summary>
		/// Compute the dense matrix sparse vector multiplication: $\vec{y} = \alpha \cdot M^\text{op} \vec{x} + \beta \cdot \vec{y}$
		/// </summary>
		/// <param name="op">operation to <paramref name="M"/></param>
		/// <param name="m">number of rows of <paramref name="M"/> before <paramref name="op"/></param>
		/// <param name="n">number of columns of <paramref name="M"/> before <paramref name="op"/></param>
		/// <param name="M">dense matrix M</param>
		/// <param name="ldm">leading dimension of <paramref name="M"/></param>
		/// <param name="x">sparse vector x</param>
		/// <param name="y">dense vector y</param>
		/// <param name="α">scalar to multiply <paramref name="M"/></param>
		/// <param name="β">scalar to multiply <paramref name="y"/></param>
		void MatrixVectorDenseMultiplySparse<T>(MatrixOperation op, int m, int n, Storage<T> M, int ldm, SparseVectorWrapper<T> x, Storage<T> y, T α, T β) where T : unmanaged;

		/// <summary>
		/// Compute the dense matrix sparse vector multiplication: $\vec{y} = \alpha \cdot M^\text{op} \vec{x} + \beta \cdot \vec{y}$
		/// </summary>
		/// <param name="op">operation to <paramref name="M"/></param>
		/// <param name="m">number of rows of <paramref name="M"/> before <paramref name="op"/></param>
		/// <param name="n">number of columns of <paramref name="M"/> before <paramref name="op"/></param>
		/// <param name="M">dense matrix M</param>
		/// <param name="ldm">leading dimension of <paramref name="M"/></param>
		/// <param name="x">sparse vector x</param>
		/// <param name="y">dense vector y</param>
		/// <param name="α">scalar to multiply <paramref name="M"/></param>
		/// <param name="β">scalar to multiply <paramref name="y"/></param>
		public delegate void DelegateMatrixVectorDenseMultiplySparse<T>(MatrixOperation op, int m, int n, Storage<T> M, int ldm, SparseVectorWrapper<T> x, Storage<T> y, T α, T β) where T : unmanaged;

		/// <summary>
		/// Compute sparse vector outer product $M = \vec{x} \otimes \vec{y}$.
		/// </summary>
		/// <param name="x">sparse vector x</param>
		/// <param name="y">sparse vector y</param>
		/// <param name="M">The output sparse matrix of <see cref="SparseMatrixFormat.COOC"/> format</param>
		/// <param name="conjY">conjugate on <paramref name="y"/> or not</param>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		void VectorSparseOuterSparse<T>(SparseVectorWrapper<T> x, SparseVectorWrapper<T> y, SparseMatrixWrapper<T> M, bool conjY = true) where T : unmanaged;

		/// <summary>
		/// Compute sparse vector outer product $M = \vec{x} \otimes \vec{y}$.
		/// </summary>
		/// <param name="x">sparse vector x</param>
		/// <param name="y">sparse vector y</param>
		/// <param name="M">The output sparse matrix of <see cref="SparseMatrixFormat.COOC"/> format</param>
		/// <param name="conjY">conjugate on <paramref name="y"/> or not</param>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		public delegate void DelegateVectorSparseOuterSparse<T>(SparseVectorWrapper<T> x, SparseVectorWrapper<T> y, SparseMatrixWrapper<T> M, bool conjY = true) where T : unmanaged;

		/// <summary>
		/// Convert the indices of sparse vector to or from a sparse COO matrix's index arrays.
		/// </summary>
		/// <param name="n">length of indices</param>
		/// <param name="ind">input/output indices of sparse vector</param>
		/// <param name="row">input/output COO matrix's row index array</param>
		/// <param name="col">input/output COO matrix's column index array</param>
		/// <param name="ld">number of rows of the matrix</param>
		/// <param name="toCOO">convert to COO index arrays or backward</param>
		void MatrixVectorCOOToFromSparseIndex(long n, Storage<int> ind, Storage<int> row, Storage<int> col, int ld, bool toCOO);

		/// <summary>
		/// Convert the indices of sparse vector to or from a sparse COO matrix's index arrays.
		/// </summary>
		/// <param name="n">length of indices</param>
		/// <param name="ind">input/output indices of sparse vector</param>
		/// <param name="row">input/output COO matrix's row index array</param>
		/// <param name="col">input/output COO matrix's column index array</param>
		/// <param name="ld">number of rows of the matrix</param>
		/// <param name="toCOO">convert to COO index arrays or backward</param>
		public delegate void DelegateMatrixVectorCOOToFromSparseIndex(long n, Storage<int> ind, Storage<int> row, Storage<int> col, int ld, bool toCOO);
		#endregion

		#region matrix format conversion
		/// <summary>
		/// Convert a sparse matrix of CSR format to dense matrix.
		/// </summary>
		/// <param name="m">number of rows of <paramref name="M"/></param>
		/// <param name="n">number of columns of <paramref name="M"/></param>
		/// <param name="dest">destination dense matrix</param>
		/// <param name="ld">leading dimension of <paramref name="dest"/></param>
		/// <param name="M">source sparse matrix</param>
		void MatrixSparseCSRToDense<T>(int m, int n, Storage<T> dest, int ld, SparseMatrixWrapper<T> M) where T : unmanaged;

		/// <summary>
		/// Convert a sparse matrix of CSR format to dense matrix.
		/// </summary>
		/// <param name="m">number of rows of <paramref name="M"/></param>
		/// <param name="n">number of columns of <paramref name="M"/></param>
		/// <param name="dest">destination dense matrix</param>
		/// <param name="ld">leading dimension of <paramref name="dest"/></param>
		/// <param name="M">source sparse matrix</param>
		public delegate void DelegateMatrixSparseCSRToDense<T>(int m, int n, Storage<T> dest, int ld, SparseMatrixWrapper<T> M) where T : unmanaged;

		/// <summary>
		/// Convert a sparse matrix of CSC format to dense matrix.
		/// </summary>
		/// <param name="m">number of rows of <paramref name="M"/></param>
		/// <param name="n">number of columns of <paramref name="M"/></param>
		/// <param name="dest">destination dense matrix</param>
		/// <param name="ld">leading dimension of <paramref name="dest"/></param>
		/// <param name="M">source sparse matrix</param>
		void MatrixSparseCSCToDense<T>(int m, int n, Storage<T> dest, int ld, SparseMatrixWrapper<T> M) where T : unmanaged;

		/// <summary>
		/// Convert a sparse matrix of CSC format to dense matrix.
		/// </summary>
		/// <param name="m">number of rows of <paramref name="M"/></param>
		/// <param name="n">number of columns of <paramref name="M"/></param>
		/// <param name="dest">destination dense matrix</param>
		/// <param name="ld">leading dimension of <paramref name="dest"/></param>
		/// <param name="M">source sparse matrix</param>
		public delegate void DelegateMatrixSparseCSCToDense<T>(int m, int n, Storage<T> dest, int ld, SparseMatrixWrapper<T> M) where T : unmanaged;

		/// <summary>
		/// Convert a dense matrix to a sparse matrix with CSR format by removing the explicit zeros.
		/// </summary>
		/// <param name="m">number of rows of <paramref name="M"/></param>
		/// <param name="n">number of columns of <paramref name="M"/></param>
		/// <param name="M">source dense matrix</param>
		/// <param name="ld">leading dimension of <paramref name="M"/></param>
		/// <returns>a new sparse matrix</returns>
		SparseMatrixWrapper<T> MatrixDenseToSparseCSR<T>(int m, int n, Storage<T> M, int ld) where T : unmanaged;

		/// <summary>
		/// Convert a dense matrix to a sparse matrix with CSR format by removing the explicit zeros.
		/// </summary>
		/// <param name="m">number of rows of <paramref name="M"/></param>
		/// <param name="n">number of columns of <paramref name="M"/></param>
		/// <param name="M">source dense matrix</param>
		/// <param name="ld">leading dimension of <paramref name="M"/></param>
		/// <returns>a new sparse matrix</returns>
		public delegate SparseMatrixWrapper<T> DelegateMatrixDenseToSparseCSR<T>(int m, int n, Storage<T> M, int ld) where T : unmanaged;

		/// <summary>
		/// Convert a dense matrix to a sparse matrix with CSC format by removing the explicit zeros.
		/// </summary>
		/// <param name="m">number of rows of <paramref name="M"/></param>
		/// <param name="n">number of columns of <paramref name="M"/></param>
		/// <param name="M">source dense matrix</param>
		/// <param name="ld">leading dimension of <paramref name="M"/></param>
		/// <returns>a new sparse matrix</returns>
		SparseMatrixWrapper<T> MatrixDenseToSparseCSC<T>(int m, int n, Storage<T> M, int ld) where T : unmanaged;

		/// <summary>
		/// Convert a dense matrix to a sparse matrix with CSC format by removing the explicit zeros.
		/// </summary>
		/// <param name="m">number of rows of <paramref name="M"/></param>
		/// <param name="n">number of columns of <paramref name="M"/></param>
		/// <param name="M">source dense matrix</param>
		/// <param name="ld">leading dimension of <paramref name="M"/></param>
		/// <returns>a new sparse matrix</returns>
		public delegate SparseMatrixWrapper<T> DelegateMatrixDenseToSparseCSC<T>(int m, int n, Storage<T> M, int ld) where T : unmanaged;

		/// <summary>
		/// Convert a dense matrix to a sparse matrix with CSR format by regarding the abs values below <paramref name="threshold"/> as zeros.
		/// </summary>
		/// <param name="m">number of rows of <paramref name="M"/></param>
		/// <param name="n">number of columns of <paramref name="M"/></param>
		/// <param name="threshold">The threshold</param>
		/// <param name="M">source dense matrix</param>
		/// <param name="ld">leading dimension of <paramref name="M"/></param>
		/// <returns>a new sparse matrix</returns>
		SparseMatrixWrapper<T> MatrixDensePruneToSparseCSR<T>(int m, int n, float threshold, Storage<T> M, int ld) where T : unmanaged;

		/// <summary>
		/// Convert a dense matrix to a sparse matrix with CSR format by regarding the abs values below <paramref name="threshold"/> as zeros.
		/// </summary>
		/// <param name="m">number of rows of <paramref name="M"/></param>
		/// <param name="n">number of columns of <paramref name="M"/></param>
		/// <param name="threshold">The threshold</param>
		/// <param name="M">source dense matrix</param>
		/// <param name="ld">leading dimension of <paramref name="M"/></param>
		/// <returns>a new sparse matrix</returns>
		public delegate SparseMatrixWrapper<T> DelegateMatrixDensePruneToSparseCSR<T>(int m, int n, float threshold, Storage<T> M, int ld) where T : unmanaged;

		/// <summary>
		/// Prune a sparse compressed (CSR/CSC) matrix to a new CSR/CSC matrix by regarding the abs values below <paramref name="threshold"/> as zeros.
		/// </summary>
		/// <param name="m">number of rows of <paramref name="M"/></param>
		/// <param name="n">number of columns of <paramref name="M"/></param>
		/// <param name="threshold">The threshold</param>
		/// <param name="M">source dense matrix</param>
		/// <param name="isCSR">is <paramref name="M"/> a CSR matrix or a CSC one</param>
		/// <returns>a new sparse matrix</returns>
		SparseMatrixWrapper<T> MatrixCompressedPruneToCompressed<T>(int m, int n, float threshold, SparseMatrixWrapper<T> M, bool isCSR) where T : unmanaged;

		/// <summary>
		/// Prune a sparse compressed (CSR/CSC) matrix to a new CSR/CSC matrix by regarding the abs values below <paramref name="threshold"/> as zeros.
		/// </summary>
		/// <param name="m">number of rows of <paramref name="M"/></param>
		/// <param name="n">number of columns of <paramref name="M"/></param>
		/// <param name="threshold">The threshold</param>
		/// <param name="M">source dense matrix</param>
		/// <param name="isCSR">is <paramref name="M"/> a CSR matrix or a CSC one</param>
		/// <returns>a new sparse matrix</returns>
		public delegate SparseMatrixWrapper<T> DelegateMatrixCompressedPruneToCompressed<T>(int m, int n, float threshold, SparseMatrixWrapper<T> M, bool isCSR) where T : unmanaged;

		/// <summary>
		/// Convert a sparse matrix to a sparse matrix with different format.
		/// The out-of-place arrays of this operation are:
		/// <list type="table">
		/// <listheader><term>Format1 ↔ Format2</term><description>  Out-of-place arrays</description></listheader>
		/// <item><term>COOR ↔ COOC</term><description>  All arrays</description></item>
		/// <item><term>COOR ↔ CSR</term><description>  Row index array</description></item>
		/// <item><term>COOR ↔ CSC</term><description>  All arrays</description></item>
		/// <item><term>COOC ↔ CSR</term><description>  All arrays</description></item>
		/// <item><term>COOC ↔ CSC</term><description>  Column index array</description></item>
		/// <item><term>CSR ↔ CSC</term><description>  All arrays</description></item>
		/// </list>
		/// </summary>
		/// <param name="m">number of rows of <paramref name="M"/> before <paramref name="op"/></param>
		/// <param name="n">number of columns of <paramref name="M"/> before <paramref name="op"/></param>
		/// <param name="op">The operation to apply to <paramref name="M"/></param>
		/// <param name="M">source dense matrix</param>
		/// <param name="format">The format of <paramref name="M"/>, must be atomic</param>
		/// <param name="target">target format, can be non-atomic, it becomes the actual format at return</param>
		/// <returns>a new sparse matrix if <paramref name="target"/> does not contains <paramref name="format"/></returns>
		SparseMatrixWrapper<T> MatrixSparseFormatConvert<T>(int m, int n, MatrixOperation op, SparseMatrixWrapper<T> M, SparseMatrixFormat format, ref SparseMatrixFormat target) where T : unmanaged;

		/// <summary>
		/// Convert a sparse matrix to a sparse matrix with different format.
		/// The out-of-place arrays of this operation are:
		/// <list type="table">
		/// <listheader><term>Format1 ↔ Format2</term><description>  Out-of-place arrays</description></listheader>
		/// <item><term>COOR ↔ COOC</term><description>  All arrays</description></item>
		/// <item><term>COOR ↔ CSR</term><description>  Row index array</description></item>
		/// <item><term>COOR ↔ CSC</term><description>  All arrays</description></item>
		/// <item><term>COOC ↔ CSR</term><description>  All arrays</description></item>
		/// <item><term>COOC ↔ CSC</term><description>  Column index array</description></item>
		/// <item><term>CSR ↔ CSC</term><description>  All arrays</description></item>
		/// </list>
		/// </summary>
		/// <param name="m">number of rows of <paramref name="M"/> before <paramref name="op"/></param>
		/// <param name="n">number of columns of <paramref name="M"/> before <paramref name="op"/></param>
		/// <param name="op">The operation to apply to <paramref name="M"/></param>
		/// <param name="M">source dense matrix</param>
		/// <param name="format">The format of <paramref name="M"/>, must be atomic</param>
		/// <param name="target">target format, can be non-atomic, it becomes the actual format at return</param>
		/// <returns>a new sparse matrix if <paramref name="target"/> does not contains <paramref name="format"/></returns>
		public delegate SparseMatrixWrapper<T> DelegateMatrixSparseFormatConvert<T>(int m, int n, MatrixOperation op, SparseMatrixWrapper<T> M, SparseMatrixFormat format, ref SparseMatrixFormat target) where T : unmanaged;

		/// <summary>
		/// Get the non-empty row/column indices of a given CSR/CSC sparse matrix.
		/// </summary>
		/// <param name="m">number of rows of <paramref name="M"/></param>
		/// <param name="n">number of columns of <paramref name="M"/></param>
		/// <param name="M">sparse CSR/CSC matrix</param>
		/// <param name="isCSR">is the sparse matrix CSR format or CSC</param>
		/// <returns>The indices (in 0-based index) of non-empty rows/columns</returns>
		Storage<int> MatrixSparseCompressedGetNEI<T>(int m, int n, SparseMatrixWrapper<T> M, bool isCSR) where T : unmanaged;

		/// <summary>
		/// Get the non-empty row/column indices of a given CSR/CSC sparse matrix.
		/// </summary>
		/// <param name="m">number of rows of <paramref name="M"/></param>
		/// <param name="n">number of columns of <paramref name="M"/></param>
		/// <param name="M">sparse CSR/CSC matrix</param>
		/// <param name="isCSR">is the sparse matrix CSR format or CSC</param>
		/// <returns>The indices (in 0-based index) of non-empty rows/columns</returns>
		public delegate Storage<int> DelegateMatrixSparseCompressedGetNEI<T>(int m, int n, SparseMatrixWrapper<T> M, bool isCSR) where T : unmanaged;

		/// <summary>
		/// Fill a sparse matrix with identity.
		/// </summary>
		/// <param name="M">The sparse matrix to fill</param>
		/// <param name="format">The format of the matrix</param>
		void MatrixFillIdentity<T>(SparseMatrixWrapper<T> M, SparseMatrixFormat format) where T : unmanaged;

		/// <summary>
		/// Fill a sparse matrix with identity.
		/// </summary>
		/// <param name="M">The sparse matrix to fill</param>
		/// <param name="format">The format of the matrix</param>
		public delegate void DelegateMatrixFillIdentity<T>(SparseMatrixWrapper<T> M, SparseMatrixFormat format) where T : unmanaged;
		#endregion

		#region matrix computation
		/// <summary>
		/// Compute sparse matrices addition: $C = \alpha A^\text{opA} + \beta B^\text{opB}$.
		/// </summary>
		/// <param name="m">number of rows of matrices after operation</param>
		/// <param name="n">number of columns of matrices after operation</param>
		/// <param name="opA">operation to matrix <paramref name="A"/></param>
		/// <param name="opB">operation to matrix <paramref name="B"/></param>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		/// <param name="A">The sparse matrix A</param>
		/// <param name="formatA">The format of <paramref name="A"/></param>
		/// <param name="β">scalar of type <typeparamref name="T"/></param>
		/// <param name="B">The sparse matrix B</param>
		/// <param name="formatB">The format of <paramref name="B"/></param>
		/// <param name="target">output target format</param>
		/// <returns>A new sparse matrix C</returns>
		SparseMatrixWrapper<T> MatrixSparseAddSparse<T>(int m, int n, MatrixOperation opA, MatrixOperation opB, SparseMatrixWrapper<T> A, SparseMatrixFormat formatA, SparseMatrixWrapper<T> B, SparseMatrixFormat formatB, T α, T β, out SparseMatrixFormat target) where T : unmanaged;

		/// <summary>
		/// Compute sparse matrices addition: $C = \alpha A^\text{opA} + \beta B^\text{opB}$.
		/// </summary>
		/// <param name="m">number of rows of matrices after operation</param>
		/// <param name="n">number of columns of matrices after operation</param>
		/// <param name="opA">operation to matrix <paramref name="A"/></param>
		/// <param name="opB">operation to matrix <paramref name="B"/></param>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		/// <param name="A">The sparse matrix A</param>
		/// <param name="formatA">The format of <paramref name="A"/></param>
		/// <param name="β">scalar of type <typeparamref name="T"/></param>
		/// <param name="B">The sparse matrix B</param>
		/// <param name="formatB">The format of <paramref name="B"/></param>
		/// <param name="target">output target format</param>
		/// <returns>A new sparse matrix C</returns>
		public delegate SparseMatrixWrapper<T> DelegateMatrixSparseAddSparse<T>(int m, int n, MatrixOperation opA, MatrixOperation opB, SparseMatrixWrapper<T> A, SparseMatrixFormat formatA, SparseMatrixWrapper<T> B, SparseMatrixFormat formatB, T α, T β, out SparseMatrixFormat target) where T : unmanaged;

		/// <summary>
		/// Compute sparse matrices multiplication: $C = \alpha A \cdot B + \beta D$.
		/// </summary>
		/// <param name="m">number of rows of <c><paramref name="opA"/>(<paramref name="A"/>)</c></param>
		/// <param name="n">number of columns of <c><paramref name="opB"/>(<paramref name="B"/>)</c></param>
		/// <param name="k">number of columns of <c><paramref name="opA"/>(<paramref name="A"/>)</c> and rows of <c><paramref name="opB"/>(<paramref name="B"/>)</c></param>
		/// <param name="opA">operation to matrix <paramref name="A"/></param>
		/// <param name="opB">operation to matrix <paramref name="B"/></param>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		/// <param name="A">The sparse matrix A</param>
		/// <param name="formatA">The format of <paramref name="A"/></param>
		/// <param name="β">scalar of type <typeparamref name="T"/></param>
		/// <param name="B">The sparse matrix B</param>
		/// <param name="formatB">The format of <paramref name="B"/></param>
		/// <param name="D">The sparse matrix D</param>
		/// <param name="formatD">The format of <paramref name="D"/></param>
		/// <param name="target">output target format</param>
		/// <returns>A new sparse matrix C</returns>
		SparseMatrixWrapper<T> MatrixSparseMultiplySparse<T>(int m, int n, int k, MatrixOperation opA, MatrixOperation opB, SparseMatrixWrapper<T> A, SparseMatrixFormat formatA, SparseMatrixWrapper<T> B, SparseMatrixFormat formatB, SparseMatrixWrapper<T> D, SparseMatrixFormat formatD, T α, T β, out SparseMatrixFormat target) where T : unmanaged;

		/// <summary>
		/// Compute sparse matrices multiplication: $C = \alpha A \cdot B + \beta D$.
		/// </summary>
		/// <param name="m">number of rows of <c><paramref name="opA"/>(<paramref name="A"/>)</c></param>
		/// <param name="n">number of columns of <c><paramref name="opB"/>(<paramref name="B"/>)</c></param>
		/// <param name="k">number of columns of <c><paramref name="opA"/>(<paramref name="A"/>)</c> and rows of <c><paramref name="opB"/>(<paramref name="B"/>)</c></param>
		/// <param name="opA">operation to matrix <paramref name="A"/></param>
		/// <param name="opB">operation to matrix <paramref name="B"/></param>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		/// <param name="A">The sparse matrix A</param>
		/// <param name="formatA">The format of <paramref name="A"/></param>
		/// <param name="β">scalar of type <typeparamref name="T"/></param>
		/// <param name="B">The sparse matrix B</param>
		/// <param name="formatB">The format of <paramref name="B"/></param>
		/// <param name="D">The sparse matrix D</param>
		/// <param name="formatD">The format of <paramref name="D"/></param>
		/// <param name="target">output target format</param>
		/// <returns>A new sparse matrix C</returns>
		public delegate SparseMatrixWrapper<T> DelegateMatrixSparseMultiplySparse<T>(int m, int n, int k, MatrixOperation opA, MatrixOperation opB, SparseMatrixWrapper<T> A, SparseMatrixFormat formatA, SparseMatrixWrapper<T> B, SparseMatrixFormat formatB, SparseMatrixWrapper<T> D, SparseMatrixFormat formatD, T α, T β, out SparseMatrixFormat target) where T : unmanaged;

		/// <summary>
		/// Compute dense matrix sparse matrix multiplication: $C = \alpha A \cdot B + \beta C$.
		/// </summary>
		/// <param name="m">number of rows of <c><paramref name="opA"/>(<paramref name="A"/>)</c></param>
		/// <param name="n">number of columns of <c><paramref name="opB"/>(<paramref name="B"/>)</c></param>
		/// <param name="k">number of columns of <c><paramref name="opA"/>(<paramref name="A"/>)</c> and rows of <c><paramref name="opB"/>(<paramref name="B"/>)</c></param>
		/// <param name="opA">operation to matrix <paramref name="A"/></param>
		/// <param name="opB">operation to matrix <paramref name="B"/></param>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		/// <param name="A">The dense matrix A</param>
		/// <param name="lda">leading dimension of <paramref name="A"/></param>
		/// <param name="β">scalar of type <typeparamref name="T"/></param>
		/// <param name="B">The sparse matrix B</param>
		/// <param name="formatB">The format of <paramref name="B"/></param>
		/// <param name="C">The dense matrix C</param>
		/// <param name="ldc">leading dimension of <paramref name="C"/></param>
		void MatrixDenseMultiplySparse<T>(int m, int n, int k, MatrixOperation opA, MatrixOperation opB, Storage<T> A, int lda, SparseMatrixWrapper<T> B, SparseMatrixFormat formatB, Storage<T> C, int ldc, T α, T β) where T : unmanaged;

		/// <summary>
		/// Compute dense matrix sparse matrix multiplication: $C = \alpha A \cdot B + \beta C$.
		/// </summary>
		/// <param name="m">number of rows of <c><paramref name="opA"/>(<paramref name="A"/>)</c></param>
		/// <param name="n">number of columns of <c><paramref name="opB"/>(<paramref name="B"/>)</c></param>
		/// <param name="k">number of columns of <c><paramref name="opA"/>(<paramref name="A"/>)</c> and rows of <c><paramref name="opB"/>(<paramref name="B"/>)</c></param>
		/// <param name="opA">operation to matrix <paramref name="A"/></param>
		/// <param name="opB">operation to matrix <paramref name="B"/></param>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		/// <param name="A">The dense matrix A</param>
		/// <param name="lda">leading dimension of <paramref name="A"/></param>
		/// <param name="β">scalar of type <typeparamref name="T"/></param>
		/// <param name="B">The sparse matrix B</param>
		/// <param name="formatB">The format of <paramref name="B"/></param>
		/// <param name="C">The dense matrix C</param>
		/// <param name="ldc">leading dimension of <paramref name="C"/></param>
		public delegate void DelegateMatrixDenseMultiplySparse<T>(int m, int n, int k, MatrixOperation opA, MatrixOperation opB, Storage<T> A, int lda, SparseMatrixWrapper<T> B, SparseMatrixFormat formatB, Storage<T> C, int ldc, T α, T β) where T : unmanaged;

		/// <summary>
		/// Compute sparse matrix dense matrix multiplication: $C = \alpha A \cdot B + \beta C$.
		/// </summary>
		/// <param name="m">number of rows of <c><paramref name="opA"/>(<paramref name="A"/>)</c></param>
		/// <param name="n">number of columns of <c><paramref name="opB"/>(<paramref name="B"/>)</c></param>
		/// <param name="k">number of columns of <c><paramref name="opA"/>(<paramref name="A"/>)</c> and rows of <c><paramref name="opB"/>(<paramref name="B"/>)</c></param>
		/// <param name="opA">operation to matrix <paramref name="A"/></param>
		/// <param name="opB">operation to matrix <paramref name="B"/></param>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		/// <param name="A">The sparse matrix A</param>
		/// <param name="formatA">The format of <paramref name="A"/></param>
		/// <param name="β">scalar of type <typeparamref name="T"/></param>
		/// <param name="B">The dense matrix B</param>
		/// <param name="ldb">leading dimension of <paramref name="B"/></param>
		/// <param name="C">The dense matrix C</param>
		/// <param name="ldc">leading dimension of <paramref name="C"/></param>
		/// <returns>A new sparse matrix C</returns>
		void MatrixSparseMultiplyDense<T>(int m, int n, int k, MatrixOperation opA, MatrixOperation opB, SparseMatrixWrapper<T> A, SparseMatrixFormat formatA, Storage<T> B, int ldb, Storage<T> C, int ldc, T α, T β) where T : unmanaged;

		/// <summary>
		/// Compute sparse matrix dense matrix multiplication: $C = \alpha A \cdot B + \beta C$.
		/// </summary>
		/// <param name="m">number of rows of <c><paramref name="opA"/>(<paramref name="A"/>)</c></param>
		/// <param name="n">number of columns of <c><paramref name="opB"/>(<paramref name="B"/>)</c></param>
		/// <param name="k">number of columns of <c><paramref name="opA"/>(<paramref name="A"/>)</c> and rows of <c><paramref name="opB"/>(<paramref name="B"/>)</c></param>
		/// <param name="opA">operation to matrix <paramref name="A"/></param>
		/// <param name="opB">operation to matrix <paramref name="B"/></param>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		/// <param name="A">The sparse matrix A</param>
		/// <param name="formatA">The format of <paramref name="A"/></param>
		/// <param name="β">scalar of type <typeparamref name="T"/></param>
		/// <param name="B">The dense matrix B</param>
		/// <param name="ldb">leading dimension of <paramref name="B"/></param>
		/// <param name="C">The dense matrix C</param>
		/// <param name="ldc">leading dimension of <paramref name="C"/></param>
		/// <returns>A new sparse matrix C</returns>
		public delegate void DelegateMatrixSparseMultiplyDense<T>(int m, int n, int k, MatrixOperation opA, MatrixOperation opB, SparseMatrixWrapper<T> A, SparseMatrixFormat formatA, Storage<T> B, int ldb, Storage<T> C, int ldc, T α, T β) where T : unmanaged;

		/// <summary>
		/// Compute sparse matrices Kronecker product $M = A \otimes B$ where all three matrices are of COO format.
		/// </summary>
		/// <param name="A">input sparse matrix A</param>
		/// <param name="ma">number of rows of <paramref name="A"/></param>
		/// <param name="na">number of columns of <paramref name="A"/></param>
		/// <param name="B">input sparse matrix B</param>
		/// <param name="mb">number of rows of <paramref name="B"/></param>
		/// <param name="nb">number of columns of <paramref name="B"/></param>
		/// <param name="M">output sparse matrix M</param>
		/// <param name="targetCOOC">The result matrix sorted by column or by row</param>
		void SparseMatrixKronecker<T>(int ma, int na, int mb, int nb, SparseMatrixWrapper<T> A, SparseMatrixWrapper<T> B, SparseMatrixWrapper<T> M, bool targetCOOC = true) where T : unmanaged;

		/// <summary>
		/// Compute sparse matrices Kronecker product $M = A \otimes B$ where all three matrices are of COO format.
		/// </summary>
		/// <param name="A">input sparse matrix A</param>
		/// <param name="ma">number of rows of <paramref name="A"/></param>
		/// <param name="na">number of columns of <paramref name="A"/></param>
		/// <param name="B">input sparse matrix B</param>
		/// <param name="mb">number of rows of <paramref name="B"/></param>
		/// <param name="nb">number of columns of <paramref name="B"/></param>
		/// <param name="M">output sparse matrix M</param>
		/// <param name="targetCOOC">The result matrix sorted by column or by row</param>
		public delegate void DelegateSparseMatrixKronecker<T>(int ma, int na, int mb, int nb, SparseMatrixWrapper<T> A, SparseMatrixWrapper<T> B, SparseMatrixWrapper<T> M, bool targetCOOC = true) where T : unmanaged;
		#endregion

		#region integer operations
		/// <summary>
		/// Find the min and max values of a integer array.
		/// </summary>
		/// <param name="indexPtr">array pointer</param>
		/// <param name="N">size of the array</param>
		/// <returns>min and max values</returns>
		(int min, int max) IndexMinMax(Storage<int> indexPtr, long N);

		/// <summary>
		/// Find the min and max values of a integer array.
		/// </summary>
		/// <param name="indexPtr">array pointer</param>
		/// <param name="N">size of the array</param>
		/// <returns>min and max values</returns>
		public delegate (int min, int max) DelegateIndexMinMax(Storage<int> indexPtr, long N);

		/// <summary>
		/// Find the max value of a integer array.
		/// </summary>
		/// <param name="indexPtr">array pointer</param>
		/// <param name="N">size of the array</param>
		/// <returns>max value</returns>
		int IndexMax(Storage<int> indexPtr, long N);

		/// <summary>
		/// Find the max value of a integer array.
		/// </summary>
		/// <param name="indexPtr">array pointer</param>
		/// <param name="N">size of the array</param>
		/// <returns>max value</returns>
		public delegate int DelegateIndexMax(Storage<int> indexPtr, long N);

		/// <summary>
		/// Find the index of the target value in a integer array.
		/// </summary>
		/// <param name="indexPtr">array pointer</param>
		/// <param name="N">size of the array</param>
		/// <param name="toFind">The target value to find</param>
		/// <returns>index of target value, -1 if not found</returns>
		int IndexFind(Storage<int> indexPtr, long N, int toFind);

		/// <summary>
		/// Find the index of the target value in a integer array.
		/// </summary>
		/// <param name="indexPtr">array pointer</param>
		/// <param name="N">size of the array</param>
		/// <param name="toFind">The target value to find</param>
		/// <returns>index of target value, -1 if not found</returns>
		public delegate int DelegateIndexFind(Storage<int> indexPtr, long N, int toFind);

		/// <summary>
		/// Find the index of the target value as a (inclusive) lower / (exclusive) upper bound in a integer array.
		/// </summary>
		/// <param name="indexPtr">array pointer</param>
		/// <param name="N">size of the array</param>
		/// <param name="value">The target value to find</param>
		/// <param name="lowerBound">regard <paramref name="value"/> as lower bound or upper bound</param>
		/// <returns>index of target value, -1 if not found</returns>
		int IndexLowerUpperBound(Storage<int> indexPtr, long N, int value, bool lowerBound);

		/// <summary>
		/// Find the index of the target value as a (inclusive) lower / (exclusive) upper bound in a integer array.
		/// </summary>
		/// <param name="indexPtr">array pointer</param>
		/// <param name="N">size of the array</param>
		/// <param name="value">The target value to find</param>
		/// <param name="lowerBound">regard <paramref name="value"/> as lower bound or upper bound</param>
		/// <returns>index of target value, -1 if not found</returns>
		public delegate int DelegateIndexLowerUpperBound(Storage<int> indexPtr, long N, int value, bool lowerBound);

		/// <summary>
		/// Fill a array of type <see cref="int"/> with a range.
		/// </summary>
		/// <param name="array">array pointer</param>
		/// <param name="length">length of array</param>
		/// <param name="start">start of range</param>
		/// <param name="inc">increment step</param>
		void IndexFillWithRange(Storage<int> array, long length, int start, int inc);

		/// <summary>
		/// Fill a array of type <see cref="int"/> with a range.
		/// </summary>
		/// <param name="array">array pointer</param>
		/// <param name="length">length of array</param>
		/// <param name="start">start of range</param>
		/// <param name="inc">increment step</param>
		public delegate void DelegateIndexFillWithRange(Storage<int> array, long length, int start, int inc);

		/// <summary>
		/// Point-wise add the <paramref name="scalar"/> to the <paramref name="array"/>. 
		/// </summary>
		/// <param name="array">The <see cref="Storage{T}"/> to be added</param>
		/// <param name="scalar">The scalar <see cref="int"/> to add</param>
		/// <param name="N">length of <paramref name="array"/></param>
		void IndexAddScalar(Storage<int> array, int scalar, long N);

		/// <summary>
		/// Point-wise add the <paramref name="scalar"/> to the <paramref name="array"/>. 
		/// </summary>
		/// <param name="array">The <see cref="Storage{T}"/> to be added</param>
		/// <param name="scalar">The scalar <see cref="int"/> to add</param>
		/// <param name="N">length of <paramref name="array"/></param>
		public delegate void DelegateIndexAddScalar(Storage<int> array, int scalar, long N);
		#endregion
	}
}


namespace Althea.SparseBlas.Cuda
{
	/// <summary>
	/// The CUDA BLAS singleton class, not visible to user
	/// </summary>
	internal sealed class CudaSparse : ISparse
	{
		#region base
		private readonly IntPtr handle;

		public CudaSparse()
		{
			this.handle = new IntPtr();
			NativeMethods.cusparseCreate(ref this.handle).Check();
		}

		public void Dispose()
		{
			NativeMethods.cusparseDestroy(this.handle).Check();
			GC.SuppressFinalize(this);
		}

		~CudaSparse()
		{
			this.Dispose();
		}
		#endregion

		#region vector
		public void VectorSparseToDense<T>(SparseVectorWrapper<T> x, Storage<T> y) where T : struct, IComparable<T>
		{
			NativeMethods.sctrFunc func = (default(T).ToDataType()) switch
			{
				DataType.RealSingle => NativeMethods.cusparseSsctr,
				DataType.RealDouble => NativeMethods.cusparseDsctr,
				DataType.ComplexSingle => NativeMethods.cusparseCsctr,
				DataType.ComplexDouble => NativeMethods.cusparseZsctr,
				_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
			};
			func(this.handle, checked((int)x.Values.Length), x.Values, x.Indices, y, IndexBase.Zero).Check();
		}

		public void VectorGatherAtIndices<T>(Storage<T> x, Storage<int> pos, Storage<T> y, int n) where T : struct, IComparable<T>
		{
			NativeMethods.gthrFunc func = (default(T).ToDataType()) switch
			{
				DataType.RealSingle => NativeMethods.cusparseSgthr,
				DataType.RealDouble => NativeMethods.cusparseDgthr,
				DataType.ComplexSingle => NativeMethods.cusparseCgthr,
				DataType.ComplexDouble => NativeMethods.cusparseZgthr,
				_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
			};
			func(this.handle, n, x, y, pos, IndexBase.Zero).Check();
		}

		public void VectorSparseAddToDense<T>(T alpha, SparseVectorWrapper<T> x, Storage<T> y) where T : struct, IComparable<T>
		{
			NativeMethods.spaxpyFunc<T> func = (default(T).ToDataType()) switch
			{
				DataType.RealSingle => new NativeMethods.spaxpyFunc<float>(NativeMethods.cusparseSaxpyi) as NativeMethods.spaxpyFunc<T>,
				DataType.RealDouble => new NativeMethods.spaxpyFunc<double>(NativeMethods.cusparseDaxpyi) as NativeMethods.spaxpyFunc<T>,
				DataType.ComplexSingle => new NativeMethods.spaxpyFunc<FloatComplex>(NativeMethods.cusparseCaxpyi) as NativeMethods.spaxpyFunc<T>,
				DataType.ComplexDouble => new NativeMethods.spaxpyFunc<DoubleComplex>(NativeMethods.cusparseZaxpyi) as NativeMethods.spaxpyFunc<T>,
				_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
			};
			func(this.handle, checked((int)x.Values.Length), ref alpha, x.Values, x.Indices, y, IndexBase.Zero).Check();
		}

		public T VectorSparseDotDense<T>(int n, SparseVectorWrapper<T> x, Storage<T> y, bool conjX) where T : struct, IComparable<T>
		{
			if (CudaCSharpHelpers.IsWindows && Runtime.API.CUDAVersionMajor <= 10)
			{
				T result = default;
				NativeMethods.dotFunc<T> func = (default(T).ToDataType()) switch
				{
					DataType.RealSingle => new NativeMethods.dotFunc<float>(NativeMethods.cusparseSdoti) as NativeMethods.dotFunc<T>,
					DataType.RealDouble => new NativeMethods.dotFunc<double>(NativeMethods.cusparseDdoti) as NativeMethods.dotFunc<T>,
					DataType.ComplexSingle => (conjX ? new NativeMethods.dotFunc<FloatComplex>(NativeMethods.cusparseCdotci) : new NativeMethods.dotFunc<FloatComplex>(NativeMethods.cusparseCdoti)) as NativeMethods.dotFunc<T>,
					DataType.ComplexDouble => (conjX ? new NativeMethods.dotFunc<DoubleComplex>(NativeMethods.cusparseZdotci) : new NativeMethods.dotFunc<DoubleComplex>(NativeMethods.cusparseZdoti)) as NativeMethods.dotFunc<T>,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
				};
				func(this.handle, checked((int)x.Values.Length), x.Values, x.Indices, y, ref result, IndexBase.Zero).Check();
				return result;
			}
			else
			{
				MatrixOperation opX = conjX ? MatrixOperation.ConjugateTranspose : MatrixOperation.None;
				var computeType = default(T).ToDataType().ToCudaDataType();
				using var vecX = SparseVectorWrapper.Create(x, n);
				using var vecY = DenseVectorWrapper.Create(y, n);

				// buffer
				var temp = (object)default(T);
				long bufferSize = 0;
				var status = NativeMethods.cusparseSpVV_bufferSize(this.handle, opX, vecX, vecY, temp, computeType, ref bufferSize);
				if (status != Status.Success)
					throw new StatusException(status);
				using var buffer = Storage<byte>.Create(bufferSize, onHost: false);

				// calculate
				NativeMethods.spdotFunc<T> func = (default(T).ToDataType()) switch
				{
					DataType.RealSingle => new NativeMethods.spdotFunc<float>(NativeMethods.cusparseSpVVS) as NativeMethods.spdotFunc<T>,
					DataType.RealDouble => new NativeMethods.spdotFunc<double>(NativeMethods.cusparseSpVVD) as NativeMethods.spdotFunc<T>,
					DataType.ComplexSingle => new NativeMethods.spdotFunc<FloatComplex>(NativeMethods.cusparseSpVVC) as NativeMethods.spdotFunc<T>,
					DataType.ComplexDouble => new NativeMethods.spdotFunc<DoubleComplex>(NativeMethods.cusparseSpVVZ) as NativeMethods.spdotFunc<T>,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
				};
				T result = default;
				func(this.handle, opX, vecX, vecY, ref result, computeType, buffer);
				return result;
			}
		}
		#endregion

		#region vector and matrix
		public void MatrixVectorSparseMultiplyDense<T>(MatrixOperation op, int m, int n, SparseMatrixWrapper<T> M, SparseMatrixFormat format, Storage<T> x, Storage<T> y, T α, T β) where T : struct, IComparable<T>
		{
			if (CudaCSharpHelpers.IsWindows && Runtime.API.CUDAVersionMajor <= 10)
			{
				// the CSRMV support CSR only
				var csrFormat = SparseMatrixFormat.CSR;
				var A = MatrixSparseFormatConvert(m, n, op, M, format, ref csrFormat);
				try
				{
					op = MatrixOperation.None;
					var descr = SparseMatrixDescription.Create(A.Values);
					NativeMethods.csrmvFunc<T> func = (default(T).ToDataType()) switch
					{
						DataType.RealSingle => new NativeMethods.csrmvFunc<float>(NativeMethods.cusparseScsrmv) as NativeMethods.csrmvFunc<T>,
						DataType.RealDouble => new NativeMethods.csrmvFunc<double>(NativeMethods.cusparseDcsrmv) as NativeMethods.csrmvFunc<T>,
						DataType.ComplexSingle => new NativeMethods.csrmvFunc<FloatComplex>(NativeMethods.cusparseCcsrmv) as NativeMethods.csrmvFunc<T>,
						DataType.ComplexDouble => new NativeMethods.csrmvFunc<DoubleComplex>(NativeMethods.cusparseZcsrmv) as NativeMethods.csrmvFunc<T>,
						_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
					};
					func(this.handle, op, m, n, checked((int)A.Values.Length), ref α, descr, A.Values, A.Row, A.Column, x, ref β, y).Check();
				}
				finally
				{
					// dispose possible transpositions
					if (A.Values != M.Values) A.Values?.Dispose();
					if (A.Row != M.Row) A.Row?.Dispose();
					if (A.Column != M.Column) A.Column?.Dispose();
				}
			}
			else
			{
				// the SpMV support CSR (CSC via CSR) and COO
				
				// create wrappers
				using var mat = SparseMatrixWrapper.Create(M, m, n, format, op.ToPowerOp());
				using var vecx = DenseVectorWrapper.Create(x, m);
				using var vecy = DenseVectorWrapper.Create(y, n);
				var type = default(T).ToDataType().ToCudaDataType();
				// deal with conjugate op
				if (mat.Operation == PowerOperation.Conjugate)
				{
					var newM = Storage<T>.Create(length: M.Values.Length, onHost: false);
					try
					{
						Runtime.API.CopyTo(source: M.Values, dest: newM, length: M.Values.Length);
						Blas.API.GPU.PointWiseConjugate(newM, M.Values.Length);
						M = new SparseMatrixWrapper<T>(newM, M.Row, M.Column);
					}
					catch (Exception)
					{
						newM.Dispose();
						throw;
					}
				}
				op = mat.Operation.ToBlasOp();

				try
				{
					NativeMethods.SpMVBuf<T> bufFunc = (default(T).ToDataType()) switch
					{
						DataType.RealSingle => new NativeMethods.SpMVBuf<float>(NativeMethods.cusparseSpMV_bufferSizeS) as NativeMethods.SpMVBuf<T>,
						DataType.RealDouble => new NativeMethods.SpMVBuf<double>(NativeMethods.cusparseSpMV_bufferSizeD) as NativeMethods.SpMVBuf<T>,
						DataType.ComplexSingle => new NativeMethods.SpMVBuf<FloatComplex>(NativeMethods.cusparseSpMV_bufferSizeC) as NativeMethods.SpMVBuf<T>,
						DataType.ComplexDouble => new NativeMethods.SpMVBuf<DoubleComplex>(NativeMethods.cusparseSpMV_bufferSizeZ) as NativeMethods.SpMVBuf<T>,
						_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
					};
					NativeMethods.SpMV<T> func = (default(T).ToDataType()) switch
					{
						DataType.RealSingle => new NativeMethods.SpMV<float>(NativeMethods.cusparseSpMVS) as NativeMethods.SpMV<T>,
						DataType.RealDouble => new NativeMethods.SpMV<double>(NativeMethods.cusparseSpMVD) as NativeMethods.SpMV<T>,
						DataType.ComplexSingle => new NativeMethods.SpMV<FloatComplex>(NativeMethods.cusparseSpMVC) as NativeMethods.SpMV<T>,
						DataType.ComplexDouble => new NativeMethods.SpMV<DoubleComplex>(NativeMethods.cusparseSpMVZ) as NativeMethods.SpMV<T>,
						_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
					};
					// buffer
					long bufferSize = 0;
					bufFunc(this.handle, op, ref α, mat, vecx, ref β, vecy, type, MatrixVectorAlgorithm.Default, ref bufferSize).Check();
					using var buffer = Storage<byte>.Create(bufferSize, onHost: false);
					// calculate
					func(this.handle, op, ref α, mat, vecx, ref β, vecy, type, MatrixVectorAlgorithm.Default, buffer).Check();
				}
				finally
				{
					if (mat.Operation == PowerOperation.Conjugate)
						M.Values.Dispose(); // since it's a new one
				}
			}
		}

		public void MatrixVectorDenseMultiplySparse<T>(MatrixOperation op, int m, int n, Storage<T> M, int ldm, SparseVectorWrapper<T> x, Storage<T> y, T α, T β) where T : struct, IComparable<T>
		{
			//// CUDA routine `gemvi` may not support conjugate transpose
			var realOp = op;
			var realM = M;
			var realLD = ldm;
			try
			{
				if (op == MatrixOperation.ConjugateTranspose)
				{
					realM = Storage<T>.Create(m * n, onHost: false);
					Runtime.API.CopyMatrixTo(source: M, dest: realM, srcLD: ldm, dstLD: m, copyNRows: m, copyNCols: n);
					Blas.API.GPU.PointWiseConjugate(realM, m * n);
					realOp = MatrixOperation.Transpose;
					realLD = m;
				}
				// buffer
				NativeMethods.gemviBufFunc bufFunc = (default(T).ToDataType()) switch
				{
					DataType.RealSingle => NativeMethods.cusparseSgemvi_bufferSize,
					DataType.RealDouble => NativeMethods.cusparseDgemvi_bufferSize,
					DataType.ComplexSingle => NativeMethods.cusparseCgemvi_bufferSize,
					DataType.ComplexDouble => NativeMethods.cusparseZgemvi_bufferSize,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
				};
				int bufferSize = 0;
				bufFunc(this.handle, realOp, m, n, checked((int)x.Values.Length), ref bufferSize).Check();
				using var buffer = Storage<byte>.Create(bufferSize, onHost: false);

				// calculate
				NativeMethods.gemviFunc<T> func = (default(T).ToDataType()) switch
				{
					DataType.RealSingle => new NativeMethods.gemviFunc<float>(NativeMethods.cusparseSgemvi) as NativeMethods.gemviFunc<T>,
					DataType.RealDouble => new NativeMethods.gemviFunc<double>(NativeMethods.cusparseDgemvi) as NativeMethods.gemviFunc<T>,
					DataType.ComplexSingle => new NativeMethods.gemviFunc<FloatComplex>(NativeMethods.cusparseCgemvi) as NativeMethods.gemviFunc<T>,
					DataType.ComplexDouble => new NativeMethods.gemviFunc<DoubleComplex>(NativeMethods.cusparseZgemvi) as NativeMethods.gemviFunc<T>,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
				};
				func(this.handle, realOp, m, n, ref α, realM, realLD, checked((int)x.Values.Length), x.Values, x.Indices, ref β, y, IndexBase.Zero, buffer).Check();
			}
			finally
			{
				if (realM != M) realM.Dispose();
			}
		}
		#endregion

		#region matrix format conversion
		public void MatrixSparseCSRToDense<T>(int m, int n, Storage<T> dest, int ld, SparseMatrixWrapper<T> M) where T : struct, IComparable<T>
		{
			NativeMethods.csr2denseFunc func = (default(T).ToDataType()) switch
			{
				DataType.RealSingle => NativeMethods.cusparseScsr2dense,
				DataType.RealDouble => NativeMethods.cusparseDcsr2dense,
				DataType.ComplexSingle => NativeMethods.cusparseCcsr2dense,
				DataType.ComplexDouble => NativeMethods.cusparseZcsr2dense,
				_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
			};
			var descr = SparseMatrixDescription.Create(M.Values);
			func(this.handle, m, n, descr, M.Values, M.Row, M.Column, dest, ld).Check();
		}

		public void MatrixSparseCSCToDense<T>(int m, int n, Storage<T> dest, int ld, SparseMatrixWrapper<T> M) where T : struct, IComparable<T>
		{
			NativeMethods.csc2denseFunc func = (default(T).ToDataType()) switch
			{
				DataType.RealSingle => NativeMethods.cusparseScsc2dense,
				DataType.RealDouble => NativeMethods.cusparseDcsc2dense,
				DataType.ComplexSingle => NativeMethods.cusparseCcsc2dense,
				DataType.ComplexDouble => NativeMethods.cusparseZcsc2dense,
				_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
			};
			var descr = SparseMatrixDescription.Create(M.Values);
			func(this.handle, m, n, descr, M.Values, M.Row, M.Column, dest, ld).Check();
		}

		public SparseMatrixWrapper<T> MatrixDenseToSparseCSR<T>(int m, int n, Storage<T> M, int ld) where T : struct, IComparable<T>
		{
			// count number of non-zeros
			NativeMethods.nnzFunc nnzfunc = (default(T).ToDataType()) switch
			{
				DataType.RealSingle => NativeMethods.cusparseSnnz,
				DataType.RealDouble => NativeMethods.cusparseDnnz,
				DataType.ComplexSingle => NativeMethods.cusparseCnnz,
				DataType.ComplexDouble => NativeMethods.cusparseZnnz,
				_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
			};
			var descr = SparseMatrixDescription.Create(M);
			using var nnzPerRow = Storage<int>.Create(m, onHost: false);
			int nnzTotal = 0;
			nnzfunc(this.handle, Direction.Row, m, n, descr, M, ld, nnzPerRow, ref nnzTotal).Check();

			// to CSR
			NativeMethods.dense2csrFunc toCSRFunc = (default(T).ToDataType()) switch
			{
				DataType.RealSingle => NativeMethods.cusparseSdense2csr,
				DataType.RealDouble => NativeMethods.cusparseDdense2csr,
				DataType.ComplexSingle => NativeMethods.cusparseCdense2csr,
				DataType.ComplexDouble => NativeMethods.cusparseZdense2csr,
				_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
			};
			Storage<T> valPtr = null; Storage<int> rowPtr = null, colPtr = null;
			try
			{
				rowPtr = Storage<int>.Create(m + 1, onHost: false);
				colPtr = Storage<int>.Create(nnzTotal, onHost: false);
				valPtr = Storage<T>.Create(nnzTotal, onHost: false);
				toCSRFunc(this.handle, m, n, descr, M, ld, nnzPerRow, valPtr, rowPtr, colPtr).Check();
				return new SparseMatrixWrapper<T>(valPtr, rowPtr, colPtr);
			}
			catch (Exception)
			{
				valPtr?.Dispose(); rowPtr?.Dispose(); colPtr?.Dispose();
				throw;
			}
		}

		public SparseMatrixWrapper<T> MatrixDenseToSparseCSC<T>(int m, int n, Storage<T> M, int ld) where T : struct, IComparable<T>
		{
			// count number of non-zeros
			NativeMethods.nnzFunc nnzfunc = (default(T).ToDataType()) switch
			{
				DataType.RealSingle => NativeMethods.cusparseSnnz,
				DataType.RealDouble => NativeMethods.cusparseDnnz,
				DataType.ComplexSingle => NativeMethods.cusparseCnnz,
				DataType.ComplexDouble => NativeMethods.cusparseZnnz,
				_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
			};
			var descr = SparseMatrixDescription.Create(M);
			using var nnzPerCol = Storage<int>.Create(n, onHost: false);
			int nnzTotal = 0;
			nnzfunc(this.handle, Direction.Column, m, n, descr, M, ld, nnzPerCol, ref nnzTotal).Check();

			// to CSC
			NativeMethods.dense2cscFunc toCSCFunc = (default(T).ToDataType()) switch
			{
				DataType.RealSingle => NativeMethods.cusparseSdense2csc,
				DataType.RealDouble => NativeMethods.cusparseDdense2csc,
				DataType.ComplexSingle => NativeMethods.cusparseCdense2csc,
				DataType.ComplexDouble => NativeMethods.cusparseZdense2csc,
				_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
			};
			Storage<T> valPtr = null; Storage<int> rowPtr = null, colPtr = null;
			try
			{
				rowPtr = Storage<int>.Create(nnzTotal, onHost: false);
				colPtr = Storage<int>.Create(n + 1, onHost: false);
				valPtr = Storage<T>.Create(nnzTotal, onHost: false);
				toCSCFunc(this.handle, m, n, descr, M, ld, nnzPerCol, valPtr, rowPtr, colPtr).Check();
				return new SparseMatrixWrapper<T>(valPtr, rowPtr, colPtr);
			}
			catch (Exception)
			{
				valPtr?.Dispose(); rowPtr?.Dispose(); colPtr?.Dispose();
				throw;
			}
		}

		public SparseMatrixWrapper<T> MatrixDensePruneToSparseCSR<T>(int m, int n, float threshold, Storage<T> M, int ld) where T : struct, IComparable<T>
		{
			var datatype = default(T).ToDataType();
			if (datatype != DataType.RealSingle || datatype != DataType.RealDouble)
				throw new ArgumentException("Direct prune with complex/integer data type" + Resource.BaseNotSupport, nameof(M));
			bool isSingle = datatype == DataType.RealSingle;

			// buffer
			T thre = threshold.GenericConvert<T, float>();
			var descr = SparseMatrixDescription.Create(M);
			long bufferSize = 0;
			NativeMethods.pruneDense2csrBufFunc<T> bufFunc = isSingle ?
				new NativeMethods.pruneDense2csrBufFunc<float>(NativeMethods.cusparseSpruneDense2csr_bufferSizeExt)  as NativeMethods.pruneDense2csrBufFunc<T> :
				new NativeMethods.pruneDense2csrBufFunc<double>(NativeMethods.cusparseDpruneDense2csr_bufferSizeExt) as NativeMethods.pruneDense2csrBufFunc<T>;
			bufFunc(this.handle, m, n, M, ld, ref thre, descr, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, ref bufferSize).Check();
			// create buffer
			using var buffer = Storage<byte>.Create(bufferSize, onHost: false);

			Storage<T> valPtr = null; Storage<int> rowPtr = null, colPtr = null;
			try
			{
				rowPtr = Storage<int>.Create(m + 1, onHost: false);
				// calculate number of non-zeros
				int nnzTotal = 0;
				NativeMethods.pruneDense2csrNnzFunc<T> nnzFunc = isSingle ?
					new NativeMethods.pruneDense2csrNnzFunc<float>(NativeMethods.cusparseSpruneDense2csrNnz) as NativeMethods.pruneDense2csrNnzFunc<T> :
					new NativeMethods.pruneDense2csrNnzFunc<double>(NativeMethods.cusparseDpruneDense2csrNnz) as NativeMethods.pruneDense2csrNnzFunc<T>;
				nnzFunc(this.handle, m, n, M, ld, ref thre, descr, rowPtr, ref nnzTotal, buffer).Check();
				// create valPtr, colPtr
				colPtr = Storage<int>.Create(nnzTotal, onHost: false);
				valPtr = Storage<T>.Create(nnzTotal, onHost: false);

				// prune
				NativeMethods.pruneDense2csrFunc<T> func = isSingle ?
					new NativeMethods.pruneDense2csrFunc<float>(NativeMethods.cusparseSpruneDense2csr) as NativeMethods.pruneDense2csrFunc<T> :
					new NativeMethods.pruneDense2csrFunc<double>(NativeMethods.cusparseDpruneDense2csr) as NativeMethods.pruneDense2csrFunc<T>;
				func(this.handle, m, n, M, ld, ref thre, descr, valPtr, rowPtr, colPtr, buffer).Check();

				// return
				return new SparseMatrixWrapper<T>(valPtr, rowPtr, colPtr);
			}
			catch (Exception)
			{
				valPtr?.Dispose(); rowPtr?.Dispose(); colPtr?.Dispose();
				throw;
			}
			
		}

		public SparseMatrixWrapper<T> MatrixCompressedPruneToCompressed<T>(int m, int n, float threshold, SparseMatrixWrapper<T> M, bool isCSR) where T : struct, IComparable<T>
		{
			// switch if CSC
			var (rowPtr, colPtr) = isCSR ? (M.Row, M.Column) : (M.Column, M.Row);
			(m, n) = isCSR ? (m, n) : (n, m);
			var descr = SparseMatrixDescription.Create(M.Values);

			// number of non-zero compress
			using var nnzPerRowCol = Storage<int>.Create(m + 1, onHost: false);
			int nnzTotal = 0;
			NativeMethods.nnz_compress<T> nnzFunc = (default(T).ToDataType()) switch
			{
				DataType.RealSingle => new NativeMethods.nnz_compress<float>(NativeMethods.cusparseSnnz_compress) as NativeMethods.nnz_compress<T>,
				DataType.RealDouble => new NativeMethods.nnz_compress<double>(NativeMethods.cusparseDnnz_compress) as NativeMethods.nnz_compress<T>,
				DataType.ComplexSingle => new NativeMethods.nnz_compress<FloatComplex>(NativeMethods.cusparseCnnz_compress) as NativeMethods.nnz_compress<T>,
				DataType.ComplexDouble => new NativeMethods.nnz_compress<DoubleComplex>(NativeMethods.cusparseZnnz_compress) as NativeMethods.nnz_compress<T>,
				_ => throw new NotSupportedException(Resource.DataTypeNotSupport)
			};
			nnzFunc(this.handle, m, descr, M.Values, rowPtr, nnzPerRowCol, ref nnzTotal, threshold.GenericConvert<T, float>()).Check();

			Storage<T> newVal = null; Storage<int> newRow = null, newCol = null;
			try
			{
				// create new rowPtr colPtr valPtr
				newRow = Storage<int>.Create(m + 1, onHost: false);
				newCol = Storage<int>.Create(nnzTotal, onHost: false);
				newVal = Storage<T>.Create(nnzTotal, onHost: false);

				// prune
				NativeMethods.csr2csr_compress<T> pruneFunc = (default(T).ToDataType()) switch
				{
					DataType.RealSingle => new NativeMethods.csr2csr_compress<float>(NativeMethods.cusparseScsr2csr_compress) as NativeMethods.csr2csr_compress<T>,
					DataType.RealDouble => new NativeMethods.csr2csr_compress<double>(NativeMethods.cusparseDcsr2csr_compress) as NativeMethods.csr2csr_compress<T>,
					DataType.ComplexSingle => new NativeMethods.csr2csr_compress<FloatComplex>(NativeMethods.cusparseCcsr2csr_compress) as NativeMethods.csr2csr_compress<T>,
					DataType.ComplexDouble => new NativeMethods.csr2csr_compress<DoubleComplex>(NativeMethods.cusparseZcsr2csr_compress) as NativeMethods.csr2csr_compress<T>,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport)
				};
				pruneFunc(this.handle, m, n, descr, M.Values, colPtr, rowPtr, checked((int)M.Values.Length), nnzPerRowCol, newVal, newCol, newRow, threshold.GenericConvert<T, float>()).Check();

				// return
				return new SparseMatrixWrapper<T>(newVal, isCSR ? newRow : newCol, isCSR ? newCol : newRow);
			}
			catch (Exception)
			{
				newVal?.Dispose(); newRow?.Dispose(); newCol?.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Convert a sparse compressed (CSR/CSC) matrix to a new COOR/COOC matrix.
		/// </summary>
		/// <param name="m">number of rows of <paramref name="M"/></param>
		/// <param name="n">number of columns of <paramref name="M"/></param>
		/// <param name="M">source dense matrix</param>
		/// <param name="isCSR">is <paramref name="M"/> a CSR matrix or a CSC one</param>
		/// <returns>a new sparse matrix</returns>
		public SparseMatrixWrapper<T> MatrixCompressedToCOO<T>(int m, int n, SparseMatrixWrapper<T> M, bool isCSR) where T : struct, IComparable<T>
		{
			// switch if CSC
			var rowPtr = isCSR ? M.Row : M.Column;
			m = isCSR ? m : n;

			// to COO
			Storage<int> cooRow = Storage<int>.Create(checked((int)M.Values.Length), onHost: false);
			try
			{
				NativeMethods.cusparseXcsr2coo(this.handle, rowPtr, checked((int)M.Values.Length), m, cooRow, IndexBase.Zero).Check();
				// return
				return new SparseMatrixWrapper<T>(M.Values, isCSR ? cooRow : M.Row, isCSR ? M.Column : cooRow);
			}
			catch (Exception)
			{
				cooRow?.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Convert a sparse COOR/COOC matrix to a new CSR/CSC matrix.
		/// </summary>
		/// <param name="m">number of rows of <paramref name="M"/></param>
		/// <param name="n">number of columns of <paramref name="M"/></param>
		/// <param name="M">source dense matrix</param>
		/// <param name="isCOOR">is <paramref name="M"/> a COOR matrix or a COOC one</param>
		/// <returns>a new sparse matrix</returns>
		public SparseMatrixWrapper<T> MatrixCOOToCompressed<T>(int m, int n, SparseMatrixWrapper<T> M, bool isCOOR) where T : struct, IComparable<T>
		{
			// switch if COOC
			var rowPtr = isCOOR ? M.Row : M.Column;
			m = isCOOR ? m : n;

			// to CSR / CSC
			Storage<int> csrRow = Storage<int>.Create(m + 1, onHost: false);
			try
			{
				NativeMethods.cusparseXcoo2csr(this.handle, rowPtr, checked((int)M.Values.Length), m, csrRow, IndexBase.Zero).Check();
				// return
				return new SparseMatrixWrapper<T>(M.Values, isCOOR ? csrRow : M.Row, isCOOR ? M.Column : csrRow);
			}
			catch (Exception)
			{
				csrRow?.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Convert a sparse compressed (CSR/CSC) matrix to its opposite (CSC/CSR) matrix.
		/// </summary>
		/// <param name="m">number of rows of <paramref name="M"/></param>
		/// <param name="n">number of columns of <paramref name="M"/></param>
		/// <param name="M">source dense matrix</param>
		/// <param name="isCSR">is <paramref name="M"/> a CSR matrix or a CSC one</param>
		/// <returns>a new sparse matrix</returns>
		public SparseMatrixWrapper<T> MatrixCompressedExchange<T>(int m, int n, SparseMatrixWrapper<T> M, bool isCSR) where T : struct, IComparable<T>
		{
			// switch if CSC
			CudaDataType dataType = default(T).ToDataType().ToCudaDataType();
			var (rowPtr, colPtr) = isCSR ? (M.Row, M.Column) : (M.Column, M.Row);
			(m, n) = isCSR ? (m, n) : (n, m);

			// buffer
			Storage<T> cscVal = null; Storage<int> cscRow = null, cscCol = null;
			try
			{
				cscRow = Storage<int>.Create(M.Values.Length, onHost: false);
				cscCol = Storage<int>.Create(n + 1, onHost: false);
				cscVal = Storage<T>.Create(M.Values.Length, onHost: false);

				long bufferSize = 0;
				NativeMethods.cusparseCsr2cscEx2_bufferSize(this.handle, m, n, checked((int)M.Values.Length), M.Values, rowPtr, colPtr, cscVal, cscCol, cscRow, dataType, Action.Numeric, IndexBase.Zero, CSR2CSCAlgorithm.Algorithm_1, ref bufferSize).Check();
				using var buffer = Storage<byte>.Create(bufferSize, onHost: false);

				// convert
				NativeMethods.cusparseCsr2cscEx2(this.handle, m, n, checked((int)M.Values.Length), M.Values, rowPtr, colPtr, cscVal, cscCol, cscRow, dataType, Action.Numeric, IndexBase.Zero, CSR2CSCAlgorithm.Algorithm_1, buffer).Check();

				// return
				return new SparseMatrixWrapper<T>(cscVal, isCSR ? cscRow : cscCol, isCSR ? cscCol : cscRow);
			}
			catch (Exception)
			{
				cscVal?.Dispose(); cscRow?.Dispose(); cscCol?.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Sort a sparse COO matrix in-place (the value array is out-of-place) to COOC or COOR format.
		/// </summary>
		/// <param name="nnz">number of non-zeros</param>
		/// <param name="values">original matrix value array</param>
		/// <param name="row">original matrix row index array</param>
		/// <param name="col">original matrix columns index array</param>
		/// <param name="newValues">output new value array, must be pre-allocated</param>
		/// <param name="m">number of rows of matrix</param>
		/// <param name="n">number of columns of matrix</param>
		/// <param name="byRow">sort by row to get a COOR format matrix</param>
		public void MatrixCOOInPlaceSort<T>(int m, int n, bool byRow, int nnz, [In] Storage<T> values, Storage<int> row, Storage<int> col, Storage<T> newValues) where T : struct, IComparable<T>
		{
			// buffer
			long bufferSize = 0;
			NativeMethods.cusparseXcoosort_bufferSizeExt(this.handle, m, n, nnz, row, col, ref bufferSize).Check();
			using var buffer = Storage<byte>.Create(bufferSize, onHost: false);
			using var P = Storage<int>.Create(nnz, onHost: false);
			NativeMethods.cusparseCreateIdentityPermutation(this.handle, nnz, P);

			// calculate
			if (byRow)
				NativeMethods.cusparseXcoosortByRow(this.handle, m, n, nnz, row, col, P, buffer);
			else
				NativeMethods.cusparseXcoosortByColumn(this.handle, m, n, nnz, row, col, P, buffer);

			// copy to new values
			VectorGatherAtIndices(values, P, newValues, nnz);
		}

		private SparseMatrixWrapper<T> COOToFormat<T>(int m, int n, SparseMatrixWrapper<T> M, bool thisCOOR, ref SparseMatrixFormat target) where T : struct, IComparable<T>
		{
			var easyFormat = thisCOOR ? SparseMatrixFormat.RowMajor : SparseMatrixFormat.ColumnMajor;
			if ((target & easyFormat) == 0)
			{
				SparseMatrixWrapper<T> result;
				Storage<T> val = null; Storage<int> row = null, col = null;
				try
				{
					val = Storage<T>.Create(length: M.Values.Length, onHost: false);
					row = Storage<int>.Create(length: M.Values.Length, onHost: false);
					Runtime.API.CopyTo(source: M.Row, dest: row, length: M.Values.Length);
					col = Storage<int>.Create(length: M.Values.Length, onHost: false);
					Runtime.API.CopyTo(source: M.Column, dest: col, length: M.Values.Length);
					MatrixCOOInPlaceSort(m, n, byRow: !thisCOOR, checked((int)M.Values.Length), M.Values, row, col, val);
					result = new SparseMatrixWrapper<T>(val, row, col);

					if ((target & (thisCOOR ? SparseMatrixFormat.CSC : SparseMatrixFormat.CSR)) != 0)
					{
						target = thisCOOR ? SparseMatrixFormat.CSC : SparseMatrixFormat.CSR;
						var newRes = MatrixCOOToCompressed(m, n, result, isCOOR: !thisCOOR);
						(thisCOOR ? result.Column : result.Row).Dispose();
						return newRes;
					}
					else
					{
						target = thisCOOR ? SparseMatrixFormat.COOC : SparseMatrixFormat.COOR;
						return result;
					}
				}
				catch (Exception)
				{
					val?.Dispose(); row?.Dispose(); col?.Dispose();
					throw;
				}
			}
			else
			{
				target = thisCOOR ? SparseMatrixFormat.CSR : SparseMatrixFormat.CSC;
				return MatrixCOOToCompressed(m, n, M, isCOOR: thisCOOR);
			}
		}

		private SparseMatrixWrapper<T> CompressedToFormat<T>(int m, int n, SparseMatrixWrapper<T> M, bool thisCSR, ref SparseMatrixFormat target) where T : struct, IComparable<T>
		{
			var easyFormat = thisCSR ? SparseMatrixFormat.RowMajor : SparseMatrixFormat.ColumnMajor;
			if ((target & easyFormat) == 0)
			{
				var result = MatrixCompressedExchange(m, n, M, thisCSR);

				if ((target & (thisCSR ? SparseMatrixFormat.COOC : SparseMatrixFormat.COOR)) != 0)
				{
					target = thisCSR ? SparseMatrixFormat.COOC : SparseMatrixFormat.COOR;
					try
					{
						var newRes = MatrixCompressedToCOO(m, n, result, !thisCSR);
						(thisCSR ? result.Column : result.Row).Dispose();
						return newRes;
					}
					catch (Exception)
					{
						result.Values?.Dispose();
						result.Row?.Dispose();
						result.Column?.Dispose();
						throw;
					}
				}
				else
				{
					target = thisCSR ? SparseMatrixFormat.CSC : SparseMatrixFormat.CSR;
					return result;
				}
			}
			else
			{
				target = thisCSR ? SparseMatrixFormat.COOR : SparseMatrixFormat.COOC;
				return MatrixCompressedToCOO(m, n, M, thisCSR);
			}
		}

		public SparseMatrixWrapper<T> MatrixSparseFormatConvert<T>(int m, int n, MatrixOperation op, SparseMatrixWrapper<T> M, SparseMatrixFormat format, ref SparseMatrixFormat target) where T : struct, IComparable<T>
		{
			if (op == MatrixOperation.ConjugateTranspose)
			{
				var newM = Storage<T>.Create(length: M.Values.Length, onHost: false);
				try
				{
					Runtime.API.CopyTo(source: M.Values, dest: newM, length: M.Values.Length);
					Blas.API.GPU.PointWiseConjugate(newM, M.Values.Length);
					M = new SparseMatrixWrapper<T>(newM, M.Row, M.Column);
				}
				catch (Exception)
				{
					newM.Dispose();
					throw;
				}
				M = new SparseMatrixWrapper<T>(newM, M.Row, M.Column);
				op = MatrixOperation.Transpose;
			}
			try
			{
				if (op == MatrixOperation.None)
				{
					if ((format & target) != 0)
						return M;
					var res = format switch
					{
						SparseMatrixFormat.COOR => this.COOToFormat(m, n, M, thisCOOR: true, ref target),
						SparseMatrixFormat.COOC => this.COOToFormat(m, n, M, thisCOOR: false, ref target),
						SparseMatrixFormat.CSR => this.CompressedToFormat(m, n, M, thisCSR: true, ref target),
						SparseMatrixFormat.CSC => this.CompressedToFormat(m, n, M, thisCSR: false, ref target),
						_ => throw new NotSupportedException(Resource.NotSupportedFormat),
					};
					return res;
				}
				else
				{
					var transFormat = format.GetTransposedFormat();
					var transM = new SparseMatrixWrapper<T>(M.Values, M.Column, M.Row);
					if ((transFormat & target) != 0)
						return transM;
					else
						return MatrixSparseFormatConvert(m, n, MatrixOperation.None, transM, transFormat, ref target);
				}
			}
			catch (Exception)
			{
				if (op == MatrixOperation.ConjugateTranspose)
					M.Values.Dispose();
				throw;
			}
		}
		#endregion

		#region matrix computation
		public SparseMatrixWrapper<T> MatrixSparseAddSparse<T>(int orgm, int orgn, MatrixOperation opA, MatrixOperation opB, SparseMatrixWrapper<T> orgA, SparseMatrixFormat formatA, SparseMatrixWrapper<T> orgB, SparseMatrixFormat formatB, T α, T β, out SparseMatrixFormat target) where T : struct, IComparable<T>
		{
			SparseMatrixWrapper<T> A = default, B = default;
			try
			{
				// do op
				var csrFormat = SparseMatrixFormat.CSR;
				var (m, n) = opA == MatrixOperation.None ? (orgm, orgn) : (orgn, orgm);
				A = MatrixSparseFormatConvert(m, n, opA, orgA, formatA, ref csrFormat);
				(m, n) = opB == MatrixOperation.None ? (orgm, orgn) : (orgn, orgm);
				B = MatrixSparseFormatConvert(m, n, opB, orgB, formatB, ref csrFormat);
				(m, n) = (orgm, orgn); // reset back

				// descriptors
				SparseMatrixDescription descrA = SparseMatrixDescription.Create(A.Values), descrB = SparseMatrixDescription.Create(B.Values);
				var descrC = new SparseMatrixDescription(matrixType: MatrixType.General);
				// pointers, allocate rowC
				using var rowC = Storage<int>.Create(m + 1, onHost: false);

				// buffer
				long bufferSize = 0;
				NativeMethods.geamBufFunc<T> bufFunc = (default(T).ToDataType()) switch
				{
					DataType.RealSingle => new NativeMethods.geamBufFunc<float>(NativeMethods.cusparseScsrgeam2_bufferSizeExt) as NativeMethods.geamBufFunc<T>,
					DataType.RealDouble => new NativeMethods.geamBufFunc<double>(NativeMethods.cusparseDcsrgeam2_bufferSizeExt) as NativeMethods.geamBufFunc<T>,
					DataType.ComplexSingle => new NativeMethods.geamBufFunc<FloatComplex>(NativeMethods.cusparseCcsrgeam2_bufferSizeExt) as NativeMethods.geamBufFunc<T>,
					DataType.ComplexDouble => new NativeMethods.geamBufFunc<DoubleComplex>(NativeMethods.cusparseZcsrgeam2_bufferSizeExt) as NativeMethods.geamBufFunc<T>,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
				};
				int nnzA = checked((int)A.Values.Length), nnzB = checked((int)B.Values.Length);
				bufFunc(this.handle, m, n, ref α, descrA, nnzA, A.Values, A.Row, A.Column, ref β, descrB, nnzB, B.Values, B.Row, B.Column, descrC, IntPtr.Zero, rowC, IntPtr.Zero, ref bufferSize).Check();
				using var buffer = Storage<byte>.Create(bufferSize, onHost: false);

				// non zeros
				int nnzC = 0;
				NativeMethods.cusparseXcsrgeam2Nnz(this.handle, m, n, descrA, nnzA, A.Row, A.Column, descrB, nnzB, B.Row, B.Column, descrC, rowC, ref nnzC, buffer).Check();
				// allocate valC and colC
				using var valC = Storage<T>.Create(nnzC, onHost: false);
				using var colC = Storage<int>.Create(nnzC, onHost: false);

				// calculate
				NativeMethods.geamFunc<T> calcFunc = (default(T).ToDataType()) switch
				{
					DataType.RealSingle => new NativeMethods.geamFunc<float>(NativeMethods.cusparseScsrgeam2) as NativeMethods.geamFunc<T>,
					DataType.RealDouble => new NativeMethods.geamFunc<double>(NativeMethods.cusparseDcsrgeam2) as NativeMethods.geamFunc<T>,
					DataType.ComplexSingle => new NativeMethods.geamFunc<FloatComplex>(NativeMethods.cusparseCcsrgeam2) as NativeMethods.geamFunc<T>,
					DataType.ComplexDouble => new NativeMethods.geamFunc<DoubleComplex>(NativeMethods.cusparseZcsrgeam2) as NativeMethods.geamFunc<T>,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
				};
				calcFunc(this.handle, m, n, ref α, descrA, nnzA, A.Values, A.Row, A.Column, ref β, descrB, nnzB, B.Values, B.Row, B.Column, descrC, valC, rowC, colC, buffer).Check();

				// return
				target = SparseMatrixFormat.CSR;
				return new SparseMatrixWrapper<T>(valC, rowC, colC);
			}
			finally
			{
				// dispose possible transpositions
				if (A.Values != orgA.Values) A.Values?.Dispose();
				if (A.Row != orgA.Row) A.Row?.Dispose();
				if (A.Column != orgA.Column) A.Column?.Dispose();
				if (B.Values != orgB.Values) B.Values?.Dispose();
				if (B.Row != orgB.Row) B.Row?.Dispose();
				if (B.Column != orgB.Column) B.Column?.Dispose();
			}
		}

		public SparseMatrixWrapper<T> MatrixSparseMultiplySparse<T>(int orgm, int orgn, int orgk, MatrixOperation opA, MatrixOperation opB, SparseMatrixWrapper<T> orgA, SparseMatrixFormat formatA, SparseMatrixWrapper<T> orgB, SparseMatrixFormat formatB, SparseMatrixWrapper<T> orgD, SparseMatrixFormat formatD, T α, T β, out SparseMatrixFormat target) where T : struct, IComparable<T>
		{
			var info = new IntPtr();
			SparseMatrixWrapper<T> A = default, B = default, D = default;
			try
			{
				// info
				NativeMethods.cusparseCreateCsrgemm2Info(ref info);
				// do op
				var csrFormat = SparseMatrixFormat.CSR;
				var (m, k) = opA == MatrixOperation.None ? (orgm, orgk) : (orgk, orgm);
				A = MatrixSparseFormatConvert(m, k, opA, orgA, formatA, ref csrFormat);
				int n;
				(k, n) = opB == MatrixOperation.None ? (orgk, orgn) : (orgk, orgn);
				B = MatrixSparseFormatConvert(k, n, opB, orgB, formatB, ref csrFormat);
				(m, n, k) = (orgm, orgn, orgk); // reset back
				D = MatrixSparseFormatConvert(m, n, MatrixOperation.None, orgD, formatD, ref csrFormat);

				// create rowC and descriptions
				using var rowC = Storage<int>.Create(m + 1, onHost: false);
				var descrA = SparseMatrixDescription.Create(A.Values);
				var descrB = SparseMatrixDescription.Create(B.Values);
				var descrD = SparseMatrixDescription.Create(D.Values);

				var descrC = new SparseMatrixDescription(matrixType: MatrixType.General);
				// buffer
				NativeMethods.gemmBufFunc<T> bufferFunc = (default(T).ToDataType()) switch
				{
					DataType.RealSingle => new NativeMethods.gemmBufFunc<float>(NativeMethods.cusparseScsrgemm2_bufferSizeExt) as NativeMethods.gemmBufFunc<T>,
					DataType.RealDouble => new NativeMethods.gemmBufFunc<double>(NativeMethods.cusparseDcsrgemm2_bufferSizeExt) as NativeMethods.gemmBufFunc<T>,
					DataType.ComplexSingle => new NativeMethods.gemmBufFunc<FloatComplex>(NativeMethods.cusparseCcsrgemm2_bufferSizeExt) as NativeMethods.gemmBufFunc<T>,
					DataType.ComplexDouble => new NativeMethods.gemmBufFunc<DoubleComplex>(NativeMethods.cusparseZcsrgemm2_bufferSizeExt) as NativeMethods.gemmBufFunc<T>,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
				};
				long bufferSize = 0;
				int nnzA = checked((int)A.Values.Length), nnzB = checked((int)B.Values.Length), nnzD = checked((int)D.Values.Length);
				bufferFunc(this.handle, m, n, k, ref α, descrA, nnzA, A.Row, A.Column, descrB, nnzB, B.Row, B.Column, ref β, descrD, nnzD, D.Row, D.Column, info, ref bufferSize).Check();
				using var buffer = Storage<byte>.Create(bufferSize, onHost: false);

				// number of non-zeros
				int nnzC = 0;
				NativeMethods.cusparseXcsrgemm2Nnz(this.handle, m, n, k, descrA, nnzA, A.Row, A.Column, descrB, nnzB, B.Row, B.Column, descrD, nnzD, D.Row, D.Column, descrC, rowC, ref nnzC, info, buffer).Check();
				// create colC valC
				using var colC = Storage<int>.Create(nnzC, onHost: false);
				using var valC = Storage<T>.Create(nnzC, onHost: false);

				// calculate
				NativeMethods.gemmFunc<T> calcFunc = (default(T).ToDataType()) switch
				{
					DataType.RealSingle => new NativeMethods.gemmFunc<float>(NativeMethods.cusparseScsrgemm2) as NativeMethods.gemmFunc<T>,
					DataType.RealDouble => new NativeMethods.gemmFunc<double>(NativeMethods.cusparseDcsrgemm2) as NativeMethods.gemmFunc<T>,
					DataType.ComplexSingle => new NativeMethods.gemmFunc<FloatComplex>(NativeMethods.cusparseCcsrgemm2) as NativeMethods.gemmFunc<T>,
					DataType.ComplexDouble => new NativeMethods.gemmFunc<DoubleComplex>(NativeMethods.cusparseZcsrgemm2) as NativeMethods.gemmFunc<T>,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
				};
				calcFunc(this.handle, m, n, k, ref α, descrA, nnzA, A.Values, A.Row, A.Column, descrB, nnzB, B.Values, B.Row, B.Column, ref β, descrD, nnzD, D.Values, D.Row, D.Column, descrC, valC, rowC, colC, info, buffer).Check();

				// return
				target = SparseMatrixFormat.CSR;
				return new SparseMatrixWrapper<T>(valC, rowC, colC);
			}
			finally
			{
				NativeMethods.cusparseDestroyCsrgemm2Info(info);
				// dispose possible transpositions
				if (A.Values != orgA.Values) A.Values?.Dispose();
				if (A.Row != orgA.Row) A.Row?.Dispose();
				if (A.Column != orgA.Column) A.Column?.Dispose();
				if (B.Values != orgB.Values) B.Values?.Dispose();
				if (B.Row != orgB.Row) B.Row?.Dispose();
				if (B.Column != orgB.Column) B.Column?.Dispose();
				if (D.Values != orgD.Values) D.Values?.Dispose();
				if (D.Row != orgD.Row) D.Row?.Dispose();
				if (D.Column != orgD.Column) D.Column?.Dispose();
			}
		}

		public void MatrixDenseMultiplySparse<T>(int orgm, int orgn, int orgk, MatrixOperation opA, MatrixOperation opB, Storage<T> orgA, int lda, SparseMatrixWrapper<T> orgB, SparseMatrixFormat formatB, Storage<T> C, int ldc, T α, T β) where T : struct, IComparable<T>
		{
			SparseMatrixWrapper<T> B = default;
			Storage<T> A = null;
			try
			{
				// do op
				if (opA != MatrixOperation.None)
				{
					A = Storage<T>.Create(length: orgm * orgk, onHost: false);
					Blas.API.GPU.GeneralMatricesAdd(opA, MatrixOperation.None, orgm, orgk, Scalars<T>.One, orgA, lda, Scalars<T>.Zero, orgA, lda, A, orgm);
					lda = orgm;
				}
				else
				{
					A = orgA;
				}
				var csrFormat = SparseMatrixFormat.CSR;
				var (k, n) = opB == MatrixOperation.None ? (orgk, orgn) : (orgk, orgn);
				B = MatrixSparseFormatConvert(k, n, opB, orgB, formatB, ref csrFormat);
				int m;
				(m, n, k) = (orgm, orgn, orgk); // reset back

				// calculate
				NativeMethods.gemmiFunc<T> calcFunc = (default(T).ToDataType()) switch
				{
					DataType.RealSingle => new NativeMethods.gemmiFunc<float>(NativeMethods.cusparseSgemmi) as NativeMethods.gemmiFunc<T>,
					DataType.RealDouble => new NativeMethods.gemmiFunc<double>(NativeMethods.cusparseDgemmi) as NativeMethods.gemmiFunc<T>,
					DataType.ComplexSingle => new NativeMethods.gemmiFunc<FloatComplex>(NativeMethods.cusparseCgemmi) as NativeMethods.gemmiFunc<T>,
					DataType.ComplexDouble => new NativeMethods.gemmiFunc<DoubleComplex>(NativeMethods.cusparseZgemmi) as NativeMethods.gemmiFunc<T>,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
				};
				calcFunc(this.handle, m, n, k, checked((int)B.Values.Length), ref α, A, lda, B.Values, B.Column, B.Row, ref β, C, lda).Check();
			}
			finally
			{
				// dispose possible transpositions
				if (A != orgA) A?.Dispose();
				if (B.Values != orgB.Values) B.Values?.Dispose();
				if (B.Row != orgB.Row) B.Row?.Dispose();
				if (B.Column != orgB.Column) B.Column?.Dispose();
			}
		}

		public void MatrixSparseMultiplyDense<T>(int orgm, int orgn, int orgk, MatrixOperation opA, MatrixOperation opB, SparseMatrixWrapper<T> orgA, SparseMatrixFormat formatA, Storage<T> orgB, int ldb, Storage<T> C, int ldc, T α, T β) where T : struct, IComparable<T>
		{
			if (CudaCSharpHelpers.IsWindows && Runtime.API.CUDAVersionMajor <= 10)
			{
				// the CSRMM only support CSR matrix
				SparseMatrixWrapper<T> A = default;
				Storage<T> B = null;
				try
				{
					var csrFormat = SparseMatrixFormat.CSR;
					var (m, k) = opA == MatrixOperation.None ? (orgm, orgk) : (orgk, orgm);
					A = formatA == SparseMatrixFormat.CSR ? orgA : MatrixSparseFormatConvert(m, k, opA, orgA, formatA, ref csrFormat);
					var opAreal = formatA == SparseMatrixFormat.CSR ? opA : MatrixOperation.None;
					var descrA = SparseMatrixDescription.Create(A.Values);
					int n;
					(k, n) = opB == MatrixOperation.None ? (orgk, orgn) : (orgk, orgn);
					var opBreal = opB;
					if (opB != MatrixOperation.None)
					{
						opBreal = MatrixOperation.None;
						ldb = orgk;
						B = Storage<T>.Create(length: orgk * orgn, onHost: false);
						Blas.API.GPU.GeneralMatricesAdd(opB, MatrixOperation.None, orgk, orgn, Scalars<T>.One, orgB, ldb, Scalars<T>.Zero, orgB, ldb, C: B, ldc: orgk);
					}
					(m, n, k) = (orgm, orgn, orgk);

					// calculate
					NativeMethods.csrmmFunc<T> calcFunc = (default(T).ToDataType()) switch
					{
						DataType.RealSingle => new NativeMethods.csrmmFunc<float>(NativeMethods.cusparseScsrmm2) as NativeMethods.csrmmFunc<T>,
						DataType.RealDouble => new NativeMethods.csrmmFunc<double>(NativeMethods.cusparseDcsrmm2) as NativeMethods.csrmmFunc<T>,
						DataType.ComplexSingle => new NativeMethods.csrmmFunc<FloatComplex>(NativeMethods.cusparseCcsrmm2) as NativeMethods.csrmmFunc<T>,
						DataType.ComplexDouble => new NativeMethods.csrmmFunc<DoubleComplex>(NativeMethods.cusparseZcsrmm2) as NativeMethods.csrmmFunc<T>,
						_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
					};
					calcFunc(this.handle, opAreal, opBreal, m, n, k, checked((int)A.Values.Length), ref α, descrA, A.Values, A.Row, A.Column, B, ldb, ref β, C, ldc).Check();

				}
				finally
				{
					if (opB != MatrixOperation.None) B?.Dispose();
					if (A.Values != orgA.Values) A.Values?.Dispose();
					if (A.Row != orgA.Row) A.Row?.Dispose();
					if (A.Column != orgA.Column) A.Column?.Dispose();
				}
			}
			else
			{
				var (m, k) = opA == MatrixOperation.None ? (orgm, orgk) : (orgk, orgm);
				using var matA = SparseMatrixWrapper.Create(orgA, m, k, formatA, opA.ToPowerOp());
				int n;
				(k, n) = opB == MatrixOperation.None ? (orgk, orgn) : (orgk, orgn);
				using var matB = DenseMatrixWrapper.Create(orgB, k, n, ldb);
				(m, n, k) = (orgm, orgn, orgk);
				using var matC = DenseMatrixWrapper.Create(C, m, n, ldc);
				var opTemp = matA.Operation;
				try
				{
					// conjugate A if necessary
					if (opTemp == PowerOperation.Conjugate)
					{
						var newA = Storage<T>.Create(orgA.Values.Length, onHost: false);
						try
						{
							Runtime.API.CopyTo(source: orgA.Values, dest: newA, length: orgA.Values.Length);
							Blas.API.GPU.PointWiseConjugate(newA, orgA.Values.Length);
							orgA = new SparseMatrixWrapper<T>(newA, orgA.Row, orgA.Column);
						}
						catch (Exception)
						{
							newA.Dispose();
							throw;
						}
						opA = MatrixOperation.None;
					}

					NativeMethods.SpMMBuf<T> bufFunc = (default(T).ToDataType()) switch
					{
						DataType.RealSingle => new NativeMethods.SpMMBuf<float>(NativeMethods.cusparseSpMM_bufferSizeS) as NativeMethods.SpMMBuf<T>,
						DataType.RealDouble => new NativeMethods.SpMMBuf<double>(NativeMethods.cusparseSpMM_bufferSizeD) as NativeMethods.SpMMBuf<T>,
						DataType.ComplexSingle => new NativeMethods.SpMMBuf<FloatComplex>(NativeMethods.cusparseSpMM_bufferSizeC) as NativeMethods.SpMMBuf<T>,
						DataType.ComplexDouble => new NativeMethods.SpMMBuf<DoubleComplex>(NativeMethods.cusparseSpMM_bufferSizeZ) as NativeMethods.SpMMBuf<T>,
						_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
					};
					NativeMethods.SpMM<T> func = (default(T).ToDataType()) switch
					{
						DataType.RealSingle => new NativeMethods.SpMM<float>(NativeMethods.cusparseSpMMS) as NativeMethods.SpMM<T>,
						DataType.RealDouble => new NativeMethods.SpMM<double>(NativeMethods.cusparseSpMMD) as NativeMethods.SpMM<T>,
						DataType.ComplexSingle => new NativeMethods.SpMM<FloatComplex>(NativeMethods.cusparseSpMMC) as NativeMethods.SpMM<T>,
						DataType.ComplexDouble => new NativeMethods.SpMM<DoubleComplex>(NativeMethods.cusparseSpMMZ) as NativeMethods.SpMM<T>,
						_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
					};

					// buffer
					long bufferSize = 0;
					bufFunc(this.handle, opA, opB, ref α, matA, matB, ref β, matC, default(T).ToDataType().ToCudaDataType(), MatrixMatrixAlgorithm.Default, ref bufferSize).Check();
					using var buffer = Storage<byte>.Create(bufferSize, onHost: false);
					// calculate
					func(this.handle, opA, opB, ref α, matA, matB, ref β, matC, default(T).ToDataType().ToCudaDataType(), MatrixMatrixAlgorithm.Default, buffer).Check();
				}
				finally
				{
					if (opTemp == PowerOperation.Conjugate) orgA.Values?.Dispose();
				}
			}
		}
		#endregion

		#region customs
		public SparseVectorWrapper<T> VectorDenseToSparse<T>(Storage<T> y, int n, float threshold = 0) where T : struct, IComparable<T>
		{
			// buffer
			long bufferSize = 0;
			var cudatype = default(T).ToDataType().ToCudaDataType();
			Customs.NativeMethods.vecPruneBuffer(n, cudatype, ref bufferSize).Check();
			using var buffer = Storage<byte>.Create(bufferSize, onHost: false);

			// calculate		
			Customs.NativeMethods.pruneFunc func = (default(T).ToDataType()) switch
			{
				DataType.RealSingle => Customs.NativeMethods.vecPruneS,
				DataType.RealDouble => Customs.NativeMethods.vecPruneD,
				DataType.ComplexSingle => Customs.NativeMethods.vecPruneC,
				DataType.ComplexDouble => Customs.NativeMethods.vecPruneZ,
				_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
			};
			IntPtr indexOut = new IntPtr(), valueOut = new IntPtr();
			long nnz = 0;
			var status = func(y, n, threshold, buffer, ref nnz, ref indexOut, ref valueOut);
			if (status != CudaError.Success)
			{
				if (indexOut != IntPtr.Zero) Runtime.Cuda.NativeMethods.cudaFree(indexOut);
				if (valueOut != IntPtr.Zero) Runtime.Cuda.NativeMethods.cudaFree(valueOut);
				throw new StatusException(status);
			}
			return new SparseVectorWrapper<T>(Storage<T>.Create(valueOut, nnz, onHost: false), Storage<int>.Create(indexOut, nnz, onHost: false));
		}

		public SparseVectorWrapper<T> VectorSparseAddSparse<T>(SparseVectorWrapper<T> x, SparseVectorWrapper<T> y) where T : struct, IComparable<T>
		{
			// buffer
			long bufferSize = 0;
			Customs.NativeMethods.vecSpAddBuffer(x.Values.Length, y.Values.Length, default(T).ToDataType().ToCudaDataType(), ref bufferSize).Check();
			using var buffer = Storage<byte>.Create(bufferSize, onHost: false);

			// calculate
			Customs.NativeMethods.vecSpAddFunc func = (default(T).ToDataType()) switch
			{
				DataType.RealSingle => Customs.NativeMethods.vecSpAddS,
				DataType.RealDouble => Customs.NativeMethods.vecSpAddD,
				DataType.ComplexSingle => Customs.NativeMethods.vecSpAddC,
				DataType.ComplexDouble => Customs.NativeMethods.vecSpAddZ,
				_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
			};
			IntPtr indexOut = new IntPtr(), valueOut = new IntPtr();
			long nnz = 0;
			var status = func(x.Indices, x.Values, x.Values.Length, y.Indices, y.Values, y.Values.Length, buffer, ref nnz, ref indexOut, ref valueOut);
			if (status != CudaError.Success)
			{
				if (indexOut != IntPtr.Zero) Runtime.Cuda.NativeMethods.cudaFree(indexOut);
				if (valueOut != IntPtr.Zero) Runtime.Cuda.NativeMethods.cudaFree(valueOut);
				throw new StatusException(status);
			}
			return new SparseVectorWrapper<T>(Storage<T>.Create(valueOut, nnz, onHost: false), Storage<int>.Create(indexOut, nnz, onHost: false));
		}

		public void VectorSparseOuterSparse<T>(SparseVectorWrapper<T> x, SparseVectorWrapper<T> y, SparseMatrixWrapper<T> M, bool conjY = true) where T : struct, IComparable<T>
		{
			Customs.NativeMethods.spVecOuterFunc func = (default(T).ToDataType()) switch
			{
				DataType.RealSingle => Customs.NativeMethods.spVecOuterS,
				DataType.RealDouble => Customs.NativeMethods.spVecOuterD,
				DataType.ComplexSingle => conjY ? (Customs.NativeMethods.spVecOuterFunc)Customs.NativeMethods.spVecOuterC : Customs.NativeMethods.spVecOuterNonconjC,
				DataType.ComplexDouble => conjY ? (Customs.NativeMethods.spVecOuterFunc)Customs.NativeMethods.spVecOuterZ : Customs.NativeMethods.spVecOuterNonconjZ,
				_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
			};
			func(x.Indices, x.Values, x.Values.Length, y.Indices, y.Values, y.Values.Length, M.Values, M.Row, M.Column).Check();
		}

		public void VectorSparsePointWiseMultiplyDivideDense<T>(SparseVectorWrapper<T> x, Storage<T> y, bool multiply) where T : struct, IComparable<T>
		{
			Customs.NativeMethods.spDivMulDnFunc func = (default(T).ToDataType()) switch
			{
				DataType.RealSingle => Customs.NativeMethods.vecSpDivMulDnS,
				DataType.RealDouble => Customs.NativeMethods.vecSpDivMulDnD,
				DataType.ComplexSingle => Customs.NativeMethods.vecSpDivMulDnC,
				DataType.ComplexDouble => Customs.NativeMethods.vecSpDivMulDnZ,
				_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
			};
			func(y, x.Values.Length, x.Values, x.Indices, multiply).Check();
		}

		public void MatrixVectorCOOToFromSparseIndex(long n, Storage<int> ind, Storage<int> row, Storage<int> col, int ld, bool toCOO)
		{
			if (toCOO)
				Customs.NativeMethods.indexToCOO(ind, row, col, n, ld).Check();
			else
				Customs.NativeMethods.COOToIndex(ind, row, col, n, ld).Check();
		}

		public Storage<int> MatrixSparseCompressedGetNEI<T>(int m, int n, SparseMatrixWrapper<T> M, bool isCSR) where T : struct, IComparable<T>
		{
			var pointer = isCSR ? M.Row : M.Column;
			var rowcols = isCSR ? m : n;
			// buffer
			long bufferSize = Customs.NativeMethods.CSRGetNerBuffer(rowcols);
			using var buffer = Storage<byte>.Create(bufferSize, onHost: false);
			// calculate
			int neiTotal = -1;
			IntPtr result = new IntPtr();
			Customs.NativeMethods.CSRGetNer(pointer, rowcols, ref neiTotal, buffer, ref result).Check();
			return Storage<int>.Create(result, neiTotal, onHost: false);
		}

		public void MatrixFillIdentity<T>(SparseMatrixWrapper<T> M, SparseMatrixFormat format) where T : struct, IComparable<T>
		{
			Blas.API.GPU.FillWithOnes(M.Values, M.Values.Length);
			long rowN, colN;
			switch (format)
			{
				case SparseMatrixFormat.COOC:
				case SparseMatrixFormat.COOR:
					rowN = colN = M.Values.Length;
					break;
				case SparseMatrixFormat.CSR:
					rowN = M.Values.Length + 1; colN = M.Values.Length;
					break;
				case SparseMatrixFormat.CSC:
					rowN = M.Values.Length; colN = M.Values.Length + 1;
					break;
				default:
					throw new NotSupportedException(Resource.NotSupportedFormat);
			}
			NativeMethods.cusparseCreateIdentityPermutation(this.handle, checked((int)rowN), M.Row).Check();
			NativeMethods.cusparseCreateIdentityPermutation(this.handle, checked((int)colN), M.Column).Check();
		}

		public void SparseMatrixKronecker<T>(int ma, int na, int mb, int nb, SparseMatrixWrapper<T> A, SparseMatrixWrapper<T> B, SparseMatrixWrapper<T> M, bool targetCOOC = true) where T : struct, IComparable<T>
		{
			Customs.NativeMethods.cooMatKronFunc func = (default(T).ToDataType()) switch
			{
				DataType.RealSingle => Customs.NativeMethods.cooMatKronS,
				DataType.RealDouble => Customs.NativeMethods.cooMatKronD,
				DataType.ComplexSingle => Customs.NativeMethods.cooMatKronC,
				DataType.ComplexDouble => Customs.NativeMethods.cooMatKronZ,
				_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
			};
			// calculate
			using var valC = Storage<T>.Create(M.Values.Length, onHost: false);
			func(A.Values, A.Row, A.Column, A.Values.Length, B.Values, B.Row, B.Column, B.Values.Length, mb, nb, valC, M.Row, M.Column).Check();
			// sort
			MatrixCOOInPlaceSort(ma * mb, na * nb, !targetCOOC, checked((int)M.Values.Length), valC, M.Row, M.Column, M.Values);
		}

		public (int min, int max) IndexMinMax(Storage<int> indexPtr, long N)
		{
			if (N <= 0)
				return (int.MinValue, int.MaxValue);
			int min = 0, max = 0;
			var status = Customs.NativeMethods.intMinMax(indexPtr, N, ref min, ref max);
			if (status != CudaError.Success)
				throw new StatusException(status);
			return (min, max);
		}

		public int IndexMax(Storage<int> indexPtr, long N)
		{
			if (N <= 0)
				return int.MaxValue;
			int max = 0;
			var status = Customs.NativeMethods.intMax(indexPtr, N, ref max);
			if (status != CudaError.Success)
				throw new StatusException(status);
			return max;
		}

		public int IndexFind(Storage<int> indexPtr, long N, int toFind)
		{
			if (N <= 0)
				return 0;
			int res = Customs.NativeMethods.intFind(indexPtr, N, toFind);
			if (res >= N)
				return -1;
			else
				return res;
		}

		public int IndexLowerUpperBound(Storage<int> indexPtr, long N, int value, bool lowerBound)
		{
			if (N <= 0)
				return -1;
			if (lowerBound)
				return Customs.NativeMethods.intLowerBound(indexPtr, N, value);
			else
				return Customs.NativeMethods.intUpperBound(indexPtr, N, value);
		}

		public void IndexFillWithRange(Storage<int> array, long length, int start, int inc)
		{
			Customs.NativeMethods.intFillRange(array, length, start, inc);
		}

		public void IndexAddScalar(Storage<int> array, int scalar, long N)
		{
			Customs.NativeMethods.intAddScalar(array, scalar, N).Check();
		}
		#endregion
	}
}


namespace Althea.SparseBlas.Mkl
{
	/// <summary>
	/// The MKL BLAS singleton class, not visible to user
	/// </summary>
	internal sealed class MklSparse : ISparse
	{
		// TODO: MKL sparse

		#region base
		public MklSparse()
		{
			// do nothing
		}

		public void Dispose()
		{
			GC.SuppressFinalize(this);
		}

		~MklSparse()
		{
			this.Dispose();
		}
		#endregion

		#region vector
		public void VectorSparseToDense<T>(SparseVectorWrapper<T> x, Storage<T> y) where T : struct, IComparable<T>
		{
			throw new NotImplementedException();
		}

		public void VectorGatherAtIndices<T>(Storage<T> x, Storage<int> pos, Storage<T> y, int n) where T : struct, IComparable<T>
		{

		}

		public void VectorSparseAddToDense<T>(T alpha, SparseVectorWrapper<T> x, Storage<T> y) where T : struct, IComparable<T>
		{
			throw new NotImplementedException();
		}

		public T VectorSparseDotDense<T>(int n, SparseVectorWrapper<T> x, Storage<T> y, bool conjX) where T : struct, IComparable<T>
		{
			throw new NotImplementedException();
		}
		#endregion

		#region vector and matrix
		public void MatrixVectorSparseMultiplyDense<T>(MatrixOperation op, int m, int n, SparseMatrixWrapper<T> M, SparseMatrixFormat format, Storage<T> x, Storage<T> y, T α, T β) where T : struct, IComparable<T>
		{
			throw new NotImplementedException();
		}

		public void MatrixVectorDenseMultiplySparse<T>(MatrixOperation op, int m, int n, Storage<T> M, int ldm, SparseVectorWrapper<T> x, Storage<T> y, T α, T β) where T : struct, IComparable<T>
		{
			throw new NotImplementedException();
		}
		#endregion

		#region matrix format conversion
		public void MatrixSparseCSRToDense<T>(int m, int n, Storage<T> dest, int ld, SparseMatrixWrapper<T> M) where T : struct, IComparable<T>
		{
			throw new NotImplementedException();
		}

		public void MatrixSparseCSCToDense<T>(int m, int n, Storage<T> dest, int ld, SparseMatrixWrapper<T> M) where T : struct, IComparable<T>
		{
			throw new NotImplementedException();
		}

		public SparseMatrixWrapper<T> MatrixDenseToSparseCSR<T>(int m, int n, Storage<T> M, int ld) where T : struct, IComparable<T>
		{
			throw new NotImplementedException();
		}

		public SparseMatrixWrapper<T> MatrixDenseToSparseCSC<T>(int m, int n, Storage<T> M, int ld) where T : struct, IComparable<T>
		{
			throw new NotImplementedException();
		}

		public SparseMatrixWrapper<T> MatrixDensePruneToSparseCSR<T>(int m, int n, float threshold, Storage<T> M, int ld) where T : struct, IComparable<T>
		{
			throw new NotImplementedException();
		}

		public SparseMatrixWrapper<T> MatrixCompressedPruneToCompressed<T>(int m, int n, float threshold, SparseMatrixWrapper<T> M, bool isCSR) where T : struct, IComparable<T>
		{
			throw new NotImplementedException();
		}

		public SparseMatrixWrapper<T> MatrixSparseFormatConvert<T>(int m, int n, MatrixOperation op, SparseMatrixWrapper<T> M, SparseMatrixFormat format, ref SparseMatrixFormat target) where T : struct, IComparable<T>
		{
			throw new NotImplementedException();
		}
		#endregion

		#region matrix computation
		public SparseMatrixWrapper<T> MatrixSparseAddSparse<T>(int m, int n, MatrixOperation opA, MatrixOperation opB, SparseMatrixWrapper<T> A, SparseMatrixFormat formatA, SparseMatrixWrapper<T> B, SparseMatrixFormat formatB, T α, T β, out SparseMatrixFormat target) where T : struct, IComparable<T>
		{
			throw new NotImplementedException();
		}

		public SparseMatrixWrapper<T> MatrixSparseMultiplySparse<T>(int m, int n, int k, MatrixOperation opA, MatrixOperation opB, SparseMatrixWrapper<T> A, SparseMatrixFormat formatA, SparseMatrixWrapper<T> B, SparseMatrixFormat formatB, SparseMatrixWrapper<T> D, SparseMatrixFormat formatD, T α, T β, out SparseMatrixFormat target) where T : struct, IComparable<T>
		{
			throw new NotImplementedException();
		}

		public void MatrixDenseMultiplySparse<T>(int m, int n, int k, MatrixOperation opA, MatrixOperation opB, Storage<T> A, int lda, SparseMatrixWrapper<T> B, SparseMatrixFormat formatB, Storage<T> C, int ldc, T α, T β) where T : struct, IComparable<T>
		{
			throw new NotImplementedException();
		}

		public void MatrixSparseMultiplyDense<T>(int m, int n, int k, MatrixOperation opA, MatrixOperation opB, SparseMatrixWrapper<T> A, SparseMatrixFormat formatA, Storage<T> B, int ldb, Storage<T> C, int ldc, T α, T β) where T : struct, IComparable<T>
		{
			throw new NotImplementedException();
		}
		#endregion

		#region customs
		public SparseVectorWrapper<T> VectorDenseToSparse<T>(Storage<T> y, int n, float threshold = 0) where T : struct, IComparable<T>
		{
			throw new NotImplementedException();
		}

		public SparseVectorWrapper<T> VectorSparseAddSparse<T>(SparseVectorWrapper<T> x, SparseVectorWrapper<T> y) where T : struct, IComparable<T>
		{
			throw new NotImplementedException();
		}

		public void VectorSparseOuterSparse<T>(SparseVectorWrapper<T> x, SparseVectorWrapper<T> y, SparseMatrixWrapper<T> M, bool conjY = true) where T : struct, IComparable<T>
		{
			throw new NotImplementedException();
		}

		public void VectorSparsePointWiseMultiplyDivideDense<T>(SparseVectorWrapper<T> x, Storage<T> y, bool multiply) where T : struct, IComparable<T>
		{
			throw new NotImplementedException();
		}

		public void MatrixVectorCOOToFromSparseIndex(long n, Storage<int> ind, Storage<int> row, Storage<int> col, int ld, bool toCOO)
		{
			throw new NotImplementedException();
		}

		public Storage<int> MatrixSparseCompressedGetNEI<T>(int m, int n, SparseMatrixWrapper<T> M, bool isCSR) where T : struct, IComparable<T>
		{
			throw new NotImplementedException();
		}

		public void MatrixFillIdentity<T>(SparseMatrixWrapper<T> M, SparseMatrixFormat format) where T : struct, IComparable<T>
		{
			throw new NotImplementedException();
		}

		public void SparseMatrixKronecker<T>(int ma, int na, int mb, int nb, SparseMatrixWrapper<T> A, SparseMatrixWrapper<T> B, SparseMatrixWrapper<T> M, bool targetCOOC = true) where T : struct, IComparable<T>
		{
			throw new NotImplementedException();
		}

		public (int min, int max) IndexMinMax(Storage<int> indexPtr, long N)
		{
			throw new NotImplementedException();
		}

		public int IndexMax(Storage<int> indexPtr, long N)
		{
			throw new NotImplementedException();
		}

		public int IndexFind(Storage<int> indexPtr, long N, int toFind)
		{
			throw new NotImplementedException();
		}

		public int IndexLowerUpperBound(Storage<int> indexPtr, long N, int value, bool lowerBound)
		{
			throw new NotImplementedException();
		}

		public void IndexFillWithRange(Storage<int> array, long length, int start, int inc)
		{
			throw new NotImplementedException();
		}

		public void IndexAddScalar(Storage<int> array, int scalar, long N)
		{
			throw new NotImplementedException();
		}
		#endregion
	}
}

