using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Althea.Linq;
using Althea.Arrays;
using Althea.NativeTypes;


namespace Althea.LinearAlgebra.Sparse
{
	/// <summary>
	/// The abstract class for runtime sparse linear algebra API routines 
	/// </summary>
	public abstract partial class AbstractApi : AbstractRuntimeApi
	{
		#region basic
		/// <summary>
		/// Get the current using <see cref="AbstractApi"/>.
		/// </summary>
		/// <remarks><b>DO NOT</b> invoke methods of this property directly unless you are sure about what you are doing; otherwise, there may be exceptions and / or unnoticeable bugs.</remarks>
		public static AbstractApi? Current => RecentAPIs.First?.Value;

		private static readonly LinkedList<AbstractApi> RecentAPIs = new LinkedList<AbstractApi>();

		internal static bool SetImplementation(Type implementation) => SetImplementation(RecentAPIs, implementation);
		#endregion


		#region support information
		/// <summary>
		/// When implemented by a derived class, check if the given <paramref name="indexType"/> is supported by vector alone operations of this implementation or not.
		/// </summary>
		/// <param name="indexType">The <see cref="DataType"/> of the vector's index array</param>
		/// <returns>Whether vector alone operations using <paramref name="indexType"/> are supported by this <see cref="AbstractApi"/>.</returns>
		/// <remarks>
		/// The operations:
		/// <list type="bullet">
		/// <item>etc.</item>
		/// </list>
		/// </remarks>
		protected abstract bool IsSupportedVectorIndexType(DataType indexType);

		/// <summary>
		/// When implemented by a derived class, check if the given <paramref name="vectorIndex"/> and <paramref name="matrixIndex"/> are supported by vector with matrix operations of this implementation or not.
		/// </summary>
		/// <param name="vectorIndex">The <see cref="DataType"/> of the vector's index array</param>
		/// <param name="matrixIndex">The <see cref="DataType"/> of the matrix's index array</param>
		/// <returns>Whether vector with matrix operations using <paramref name="vectorIndex"/> and <paramref name="matrixIndex"/> are supported by this <see cref="AbstractApi"/>.</returns>
		/// <remarks>
		/// The operations:
		/// <list type="bullet">
		/// <item>etc.</item>
		/// </list>
		/// </remarks>
		protected abstract bool IsSupportedVectorMatrixIndexType(DataType vectorIndex, DataType matrixIndex);

		/// <summary>
		/// When implemented by a derived class, check if the given <paramref name="indexType"/> is supported by matrix alone operations of this implementation or not.
		/// </summary>
		/// <param name="indexType">The <see cref="DataType"/> of the matrix's index array</param>
		/// <returns>Whether matrix alone operations using <paramref name="indexType"/> are supported by this <see cref="AbstractApi"/>.</returns>
		/// <remarks>
		/// The operations:
		/// <list type="bullet">
		/// <item>etc.</item>
		/// </list>
		/// </remarks>
		protected abstract bool IsSupportedMatrixIndexType(DataType indexType);

		#endregion


		#region vector
		/// <summary>
		/// When implemented by a derived class, add the sparse vector <paramref name="x"/> scaled by scalar <paramref name="α"/> to a dense vector <paramref name="y"/>: <c><paramref name="y"/> += <paramref name="α"/> * <paramref name="x"/></c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="α">The scalar to multiply <paramref name="x"/></param>
		/// <param name="x">The input sparse vector as a <see cref="ISparseVector{T}"/></param>
		/// <param name="y">The input/output dense vector as a <see cref="Storage{T}"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		public abstract void VectorSparseAddToDense<T>(T α, ISparseVector<T> x, Storage<T> y) where T : unmanaged, IEquatable<T>;

		/// <summary>
		/// When implemented by a derived class, calculate the dot (inner) product of a sparse vector <paramref name="x"/> and a dense vector <paramref name="y"/>: result = <c><paramref name="x"/>^op <paramref name="y"/></c>, op = <paramref name="conjX"/> ? H : T.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="conjX">Whether to conjugate <paramref name="x"/> or not</param>
		/// <param name="x">The input sparse vector as a <see cref="ISparseVector{T}"/></param>
		/// <param name="y">The input dense vector as a <see cref="Storage{T}"/></param>
		/// <returns>The dot product result as a <typeparamref name="T"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		public abstract T VectorSparseDotDense<T>(bool conjX, ISparseVector<T> x, Storage<T> y) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, calculate the dot (inner) product of two sparse vectors <paramref name="x"/> and <paramref name="y"/>: result = <c><paramref name="x"/>^op <paramref name="y"/></c>, op = <paramref name="conjX"/> ? H : T.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="conjX">Whether to conjugate <paramref name="x"/> or not</param>
		/// <param name="x">The input sparse vector as a <see cref="ISparseVector{T}"/></param>
		/// <param name="y">The other input sparse vector as a <see cref="ISparseVector{T}"/></param>
		/// <returns>The dot product result as a <typeparamref name="T"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		public abstract T VectorSparseDotSparse<T>(bool conjX, ISparseVector<T> x, ISparseVector<T> y) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, add the sparse vector <paramref name="x"/> to another sparse vector <paramref name="y"/> and put the result in a new sparse vector.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The input sparse vector x</param>
		/// <param name="y">The input sparse vector y</param>
		/// <param name="createFunc">See <see cref="DelegateCreateNew{T}"/></param>
		/// <returns>The result of sum of <paramref name="x"/> and <paramref name="y"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		public abstract ISparseVector<T> VectorSparseAddSparse<T>(ISparseVector<T> x, ISparseVector<T> y, DelegateCreateNew<T>? createFunc = null) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, point-wise multiply a sparse vector by a dense vector: <c><paramref name="x"/> *= <paramref name="y"/></c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The input/output sparse vector x</param>
		/// <param name="y">The input dense vector y</param>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		public abstract void VectorSparsePointWiseMultiplyDense<T>(ISparseVector<T> x, Storage<T> y) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, point-wise divide a sparse vector by a dense vector: <c><paramref name="x"/> /= <paramref name="y"/></c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The input/output sparse vector x</param>
		/// <param name="y">The input dense vector y</param>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		public abstract void VectorSparsePointWiseDivideDense<T>(ISparseVector<T> x, Storage<T> y) where T : unmanaged;
		#endregion

		#region vector and matrix
		/// <summary>
		/// When implemented by a derived class, compute the sparse matrix dense vector multiplication: <c><paramref name="y"/> = <paramref name="α"/> * <paramref name="M"/>^<paramref name="op"/> * <paramref name="x"/> + <paramref name="β"/> * <paramref name="y"/></c>.
		/// </summary>
		/// <param name="op">The <see cref="MatrixOperation"/> to indicate the simple operation to <paramref name="M"/></param>
		/// <param name="M">The input sparse matrix M</param>
		/// <param name="x">The input dense vector x</param>
		/// <param name="y">The input/output dense vector y</param>
		/// <param name="α">The scalar to multiply <paramref name="M"/></param>
		/// <param name="β">The scalar to multiply <paramref name="y"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> or <paramref name="M"/> is null or invalid</exception>
		public abstract void MatrixSparseMultiplyVectorDense<T>(MatrixOperation op, T α, SparseMatrixWrapper<T> M, Storage<T> x, T β, Storage<T> y) where T : unmanaged, IEquatable<T>;

		/// <summary>
		/// When implemented by a derived class, compute the dense matrix sparse vector multiplication: <c><paramref name="y"/> = <paramref name="α"/> * <paramref name="M"/>^<paramref name="op"/> * <paramref name="x"/> + <paramref name="β"/> * <paramref name="y"/></c>.
		/// </summary>
		/// <param name="op">The <see cref="MatrixOperation"/> to indicate the simple operation to <paramref name="M"/></param>
		/// <param name="α">The scalar to multiply <paramref name="M"/></param>
		/// <param name="m">The number of rows of <paramref name="M"/> before <paramref name="op"/></param>
		/// <param name="n">The number of columns of <paramref name="M"/> before <paramref name="op"/></param>
		/// <param name="M">The input dense matrix M</param>
		/// <param name="x">The input sparse vector x</param>
		/// <param name="y">The input/output dense vector y</param>
		/// <param name="β">The scalar to multiply <paramref name="y"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> or <paramref name="M"/> is null or invalid</exception>
		public abstract void MatrixDenseMultiplyVectorSparse<T>(MatrixOperation op, T α, long m, long n, Storage<T> M, ISparseVector<T> x, T β, Storage<T> y) where T : unmanaged, IEquatable<T>;

		/// <summary>
		/// When implemented by a derived class, compute sparse vector outer product: <c><paramref name="M"/> = <paramref name="α"/> <paramref name="x"/> * <paramref name="y"/>^op + <paramref name="β"/> * <paramref name="M"/></c>, <c>op = <paramref name="conjY"/> ? H : T</c>.
		/// </summary>
		/// <param name="conjY">Whether to conjugate <paramref name="y"/> or not</param>
		/// <param name="α">The scalar to multiply <paramref name="x"/></param>
		/// <param name="x">The input sparse vector x</param>
		/// <param name="y">The input sparse vector y</param>
		/// <param name="β">The scalar to multiply <paramref name="M"/></param>
		/// <param name="M">The preallocated input/output sparse matrix of <see cref="SparseMatrixFormat.COOC"/> format with <c>non_zeros = <paramref name="x"/>.non_zeros * <paramref name="y"/>.non_zeros</c></param>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> or <paramref name="M"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="M"/> is not of <see cref="SparseMatrixFormat.COOC"/> format or has wrong number of non-zero elements</exception>
		public abstract void VectorSparseOuter<T>(bool conjY, T α, ISparseVector<T> x, ISparseVector<T> y, T β, SparseMatrixWrapper<T> M) where T : unmanaged, IEquatable<T>;
		#endregion
	}
}