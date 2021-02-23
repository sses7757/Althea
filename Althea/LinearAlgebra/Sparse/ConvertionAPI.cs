using System;
using System.Collections.Generic;

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
		protected abstract bool IsSupportedVectorIndexType(DataType indexType);

		/// <summary>
		/// When implemented by a derived class, check if the given <paramref name="vectorIndex"/> and <paramref name="matrixIndex"/> are supported by vector with matrix operations of this implementation or not.
		/// </summary>
		/// <param name="vectorIndex">The <see cref="DataType"/> of the vector's index array</param>
		/// <param name="matrixIndex">The <see cref="DataType"/> of the matrix's index array</param>
		/// <returns>Whether vector with matrix operations using <paramref name="vectorIndex"/> and <paramref name="matrixIndex"/> are supported by this <see cref="AbstractApi"/>.</returns>
		protected abstract bool IsSupportedVectorMatrixIndexType(DataType vectorIndex, DataType matrixIndex);

		/// <summary>
		/// When implemented by a derived class, check if the given <paramref name="indexType"/> is supported by matrix alone operations of this implementation or not.
		/// </summary>
		/// <param name="indexType">The <see cref="DataType"/> of the matrix's index array</param>
		/// <returns>Whether matrix alone operations using <paramref name="indexType"/> are supported by this <see cref="AbstractApi"/>.</returns>
		protected abstract bool IsSupportedMatrixIndexType(DataType indexType);

		/// <summary>
		/// When implemented by a derived class, check if the given <paramref name="vector"/>'s meta-data (such as length, index type, <see cref="CombinationOfLocations"/> of index arrays) is supported by this implementation or not.
		/// </summary>
		/// <param name="vector">The <see cref="ISparseVector{T}"/> to check</param>
		/// <returns>Whether the given <paramref name="vector"/>'s meta-data (such as length, index type, <see cref="CombinationOfLocations"/> of index arrays) is supported by this <see cref="AbstractApi"/> or not.</returns>
		protected abstract bool IsSupportedSparseVector<T>(ISparseVector<T> vector) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, check if the given <paramref name="matrix"/>'s meta-data (such as length, index type, <see cref="CombinationOfLocations"/> of index arrays) is supported by this implementation or not.
		/// </summary>
		/// <param name="matrix">The <see cref="ISparseMatrix{T}"/> to check</param>
		/// <returns>Whether the given <paramref name="matrix"/>'s meta-data (such as length, index type, <see cref="CombinationOfLocations"/> of index arrays) is supported by this <see cref="AbstractApi"/> or not.</returns>
		protected abstract bool IsSupportedSparseMatrix<T>(ISparseMatrix<T> matrix) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, check if the given <paramref name="location"/> is supported by vector unary operations of this implementation or not.
		/// </summary>
		/// <param name="location">The given <see cref="CombinationOfLocations"/></param>
		/// <returns>Whether vector unary operation on <paramref name="location"/> is supported by this <see cref="AbstractApi"/>.</returns>
		protected abstract bool IsSupportedVectorUnary(CombinationOfLocations location);

		/// <summary>
		/// When implemented by a derived class, check if the given <see cref="CombinationOfLocations"/>s are supported by vector binary operations of this implementation or not.
		/// </summary>
		/// <param name="location1">The first given <see cref="CombinationOfLocations"/></param>
		/// <param name="location2">The second given <see cref="CombinationOfLocations"/></param>
		/// <returns>Whether binary operations on <paramref name="location1"/> and <paramref name="location2"/> are supported by this <see cref="AbstractApi"/>.</returns>
		protected abstract bool IsSupportedVectorBinary(CombinationOfLocations location1, CombinationOfLocations location2);

		/// <summary>
		/// When implemented by a derived class, check if the given <see cref="CombinationOfLocations"/>s are supported by vector and matrix binary operations of this implementation or not.
		/// </summary>
		/// <param name="vector">The given <see cref="CombinationOfLocations"/> of the vector</param>
		/// <param name="matrix">The given <see cref="CombinationOfLocations"/> of the matrix</param>
		/// <returns>Whether binary operations on <paramref name="vector"/> and <paramref name="matrix"/> are supported by this <see cref="AbstractApi"/>.</returns>
		protected abstract bool IsSupportedVectorUnaryMatrixUnary(CombinationOfLocations vector, CombinationOfLocations matrix);

		/// <summary>
		/// When implemented by a derived class, check if the given <see cref="CombinationOfLocations"/>s are supported by binary vector and unary matrix operations of this implementation or not.
		/// </summary>
		/// <param name="vector1">The given <see cref="CombinationOfLocations"/> of the first vector</param>
		/// <param name="vector2">The given <see cref="CombinationOfLocations"/> of the second vector</param>
		/// <param name="matrix">The given <see cref="CombinationOfLocations"/> of matrix</param>
		/// <returns>Whether binary vector and unary matrix operations on <paramref name="vector1"/> and <paramref name="vector2"/> and <paramref name="matrix"/> are supported by this <see cref="AbstractApi"/>.</returns>
		protected abstract bool IsSupportedVectorBinaryMatrixUnary(CombinationOfLocations vector1, CombinationOfLocations vector2, CombinationOfLocations matrix);

		/// <summary>
		/// When implemented by a derived class, check if the given <see cref="CombinationOfLocations"/>s are supported by unary vector and binary matrix operations of this implementation or not.
		/// </summary>
		/// <param name="vector">The given <see cref="CombinationOfLocations"/> of the vector</param>
		/// <param name="matrix1">The given <see cref="CombinationOfLocations"/> of the first matrix</param>
		/// <param name="matrix2">The given <see cref="CombinationOfLocations"/> of the second matrix</param>
		/// <returns>Whether unary vector and binary matrix operations on <paramref name="vector"/> and <paramref name="matrix1"/> and <paramref name="matrix2"/> are supported by this <see cref="AbstractApi"/>.</returns>
		protected abstract bool IsSupportedVectorUnaryMatrixBinary(CombinationOfLocations vector, CombinationOfLocations matrix1, CombinationOfLocations matrix2);

		/// <summary>
		/// When implemented by a derived class, check if the given <paramref name="location"/> is supported by matrix unary operations of this implementation or not.
		/// </summary>
		/// <param name="location">The given <see cref="CombinationOfLocations"/></param>
		/// <returns>Whether matrix unary operation on <paramref name="location"/> is supported by this <see cref="AbstractApi"/>.</returns>
		protected abstract bool IsSupportedMatrixUnary(CombinationOfLocations location);

		/// <summary>
		/// When implemented by a derived class, check if the given <see cref="CombinationOfLocations"/>s are supported by matrix binary operations of this implementation or not.
		/// </summary>
		/// <param name="location1">The first given <see cref="CombinationOfLocations"/></param>
		/// <param name="location2">The second given <see cref="CombinationOfLocations"/></param>
		/// <returns>Whether binary operations on <paramref name="location1"/> and <paramref name="location2"/> are supported by this <see cref="AbstractApi"/>.</returns>
		protected abstract bool IsSupportedMatrixBinary(CombinationOfLocations location1, CombinationOfLocations location2);

		/// <summary>
		/// When implemented by a derived class, check if the given <see cref="CombinationOfLocations"/>s are supported by matrix trinary operations of this implementation or not.
		/// </summary>
		/// <param name="location1">The first given <see cref="CombinationOfLocations"/></param>
		/// <param name="location2">The second given <see cref="CombinationOfLocations"/></param>
		/// <param name="location3">The third given <see cref="CombinationOfLocations"/></param>
		/// <returns>Whether trinary operations on <paramref name="location1"/> and <paramref name="location2"/> are supported by this <see cref="AbstractApi"/>.</returns>
		protected abstract bool IsSupportedMatrixTrinary(CombinationOfLocations location1, CombinationOfLocations location2, CombinationOfLocations location3);
		#endregion


		#region delegate
		/// <summary>
		/// Encapsulates a method that receive the total <paramref name="length"/> in <typeparamref name="T"/> and the <paramref name="format"/> as the parameters and return an <b>allocated</b> new <see cref="ISparseVector{T}"/> of the given length (non-zeros).
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="length">The desired total length in <typeparamref name="T"/></param>
		/// <param name="format">The desired <see cref="SparseVectorFormat"/></param>
		/// <returns>An <b>allocated</b> new <see cref="ISparseVector{T}"/> of <paramref name="length"/> and <paramref name="format"/></returns>
		/// <remarks>
		/// This delegate is usually used as a nullable parameter of methods in <see cref="AbstractApi"/>.<br/>
		/// The default implementation typically shall utilize <c><see cref="Storage.StorageFactory{T}"/>.<see cref="Storage.StorageFactory{T}.CreateAlike">CreateAlike</see>(input_storage.<see cref="Storage{T}.MakeReference">MakeReference</see>(0, <paramref name="length"/>))</c>
		/// </remarks>
		public delegate ISparseVector<T> DelegateCreateVectorNew<T>(long length, SparseVectorFormat format) where T : unmanaged;

		/// <summary>
		/// Encapsulates a method that receive the presenting number of rows <paramref name="rows"/> and number of columns <paramref name="cols"/> in <typeparamref name="T"/> and the <paramref name="format"/> as the parameters and return an <b>allocated</b> new <see cref="ISparseMatrix{T}"/> of the given size.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="rows">The desired presenting number of rows in <typeparamref name="T"/></param>
		/// <param name="cols">The desired presenting number of columns in <typeparamref name="T"/></param>
		/// <param name="format">The desired <see cref="SparseMatrixFormat"/> of the target sparse matrix, must be atomic</param>
		/// <returns>An <b>allocated</b> new <see cref="ISparseMatrix{T}"/> of the given size</returns>
		/// <remarks>
		/// This delegate is usually used as a nullable parameter of methods in <see cref="AbstractApi"/>.<br/>
		/// The default implementation typically shall utilize (multiple) <c><see cref="Storage.StorageFactory{T}"/>.<see cref="Storage.StorageFactory{T}.CreateAlike">CreateAlike</see>(input_storage.<see cref="Storage{T}.MakeReference">MakeReference</see>(0, internal_length))</c>
		/// </remarks>
		public delegate ISparseMatrix<T> DelegateCreateMatrixNew<T>(long rows, long cols, SparseMatrixFormat format) where T : unmanaged;
		#endregion


		#region vector
		/// <summary>
		/// When implemented by a derived class, scatter (and overwrite) the sparse vector <paramref name="x"/> to the dense vector <paramref name="y"/>: <paramref name="y"/>[<paramref name="x"/>.Indices] = <paramref name="x"/>.<see cref="ISparseArray{T}.Storage">Values</see>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The sparse vector x as a <see cref="ISparseVector{T}"/></param>
		/// <param name="y">The dense vector y as a <see cref="Storage{T}"/> whose elements at <paramref name="x"/>.Indices are overwritten</param>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		public abstract void VectorSparseToDense<T>(ISparseVector<T> x, Storage<T> y) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, set the <paramref name="x"/>'s values at certain <paramref name="positions"/> to the give <paramref name="value"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <typeparam name="TInd">Any integral-typed unmanaged struct as the index type</typeparam>
		/// <param name="x">The input vector whose values will be set</param>
		/// <param name="positions">The given positions as a <see cref="Storage{T}"/> of <see cref="int"/></param>
		/// <param name="value">The value to set</param>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="positions"/> is null or invalid</exception>
		public abstract void VectorSetValuesAt<T, TInd>(Storage<T> x, T value, Storage<TInd> positions) where T : unmanaged where TInd : unmanaged;

		/// <summary>
		/// When implemented by a derived class, gather the dense vector <paramref name="x"/> at <paramref name="pos"/> into another vector <paramref name="y"/>: <paramref name="y"/> = <paramref name="x"/>[<paramref name="pos"/>].
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <typeparam name="TInd">Any integral-typed unmanaged struct as the index type</typeparam>
		/// <param name="x">The input dense vector as a <see cref="Storage{T}"/> to gather from</param>
		/// <param name="pos">The input indices indicating where to gather as a <see cref="Storage{T}"/> of <see cref="int"/></param>
		/// <param name="y">The output vector to gather to as a <see cref="Storage{T}"/>, the first <paramref name="pos"/>.<see cref="Storage{T}.Length">Length</see> elements will be overwritten.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> or <paramref name="pos"/> is null or invalid</exception>
		public abstract void VectorGatherValuesAt<T, TInd>(Storage<T> x, Storage<TInd> pos, Storage<T> y) where T : unmanaged where TInd : unmanaged;

		/// <summary>
		/// When implemented by a derived class, convert a dense vector <paramref name="x"/> to a sparse vector by the given truncation <paramref name="threshold"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The input dense vector as a <see cref="Storage{T}"/></param>
		/// <param name="threshold">Any element in <paramref name="x"/> whose absolute value is less than or equals to <paramref name="threshold"/> will be regarded as zero</param>
		/// <param name="format">The desired <see cref="SparseVectorFormat"/> of the target sparse vector, can be anatomic</param>
		/// <param name="createFunc">See <see cref="DelegateCreateMatrixNew{T}"/></param>
		/// <param name="target">Output the created new <see cref="ISparseVector{T}"/> with format fitting <paramref name="format"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="threshold"/> is less than 0</exception>
		public abstract void VectorDenseToSparse<T>(Storage<T> x, SparseVectorFormat format, out ISparseVector<T> target, float threshold = 0, DelegateCreateMatrixNew<T>? createFunc = null) where T : unmanaged;
		#endregion

		#region vector and matrix
		/// <summary>
		/// When implemented by a derived class, convert the given sparse <paramref name="vector"/> to a sparse matrix of <paramref name="format"/> and presenting number of <paramref name="rows"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="vector">The input sparse vector as a <see cref="ISparseVector{T}"/></param>
		/// <param name="rows">The desired number of rows of the target sparse matrix (the number of columns is calculated from this)</param>
		/// <param name="format">The desired <see cref="SparseMatrixFormat"/> of the target sparse matrix, can be anatomic</param>
		/// <param name="createFunc">See <see cref="DelegateCreateMatrixNew{T}"/></param>
		/// <param name="target">Output the created new <see cref="ISparseMatrix{T}"/> with format fitting <paramref name="format"/> and size fitting <paramref name="rows"/></param>
		/// <returns>The created new <see cref="ISparseMatrix{T}"/> of desired properties</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="vector"/> is null or invalid</exception>
		public abstract void SparseVectorToMatrix<T>(ISparseVector<T> vector, long rows, SparseMatrixFormat format, out ISparseMatrix<T> target, DelegateCreateMatrixNew<T>? createFunc = null) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, convert the given sparse <paramref name="format"/> to a sparse vector of given <paramref name="format"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="matrix">The input sparse matrix as a <see cref="ISparseMatrix{T}"/></param>
		/// <param name="format">The desired <see cref="SparseVectorFormat"/> of the target sparse vector, can be anatomic</param>
		/// <param name="createFunc">See <see cref="DelegateCreateVectorNew{T}"/></param>
		/// <param name="target">Output the created new <see cref="ISparseVector{T}"/> with format fitting <paramref name="format"/> and desired properties (the length is the product of <see cref="ISparseMatrix{T}.NRows"/> and <see cref="ISparseMatrix{T}.NCols"/>)</param>
		/// <exception cref="ArgumentNullException">If <paramref name="matrix"/> is null or invalid</exception>
		public abstract void SparseMatrixToVector<T>(ISparseMatrix<T> matrix, SparseVectorFormat format, out ISparseVector<T> target, DelegateCreateVectorNew<T>? createFunc = null) where T : unmanaged;
		#endregion

		#region matrix
		/// <summary>
		/// When implemented by a derived class, convert the given sparse matrix <paramref name="source"/> to a dense matrix <paramref name="destination"/>.
		/// </summary>
		/// <param name="source">The source sparse matrix to convert from</param>
		/// <param name="destination">The storage of the destination dense matrix of the same size as <paramref name="source"/></param>
		/// <param name="ld">The leading dimension of <paramref name="destination"/></param>
		public abstract void MatrixSparseToDense<T>(ISparseMatrix<T> source, Storage<T> destination, long ld) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, convert the given dense matrix <paramref name="source"/> of to the <paramref name="format"/> format.
		/// </summary>
		/// <param name="m">The number of rows of <paramref name="source"/></param>
		/// <param name="n">The number of columns of <paramref name="source"/></param>
		/// <param name="source">The source dense matrix to convert from</param>
		/// <param name="ld">The leading dimension of <paramref name="source"/></param>
		/// <param name="format">The destination <see cref="SparseMatrixFormat"/> of the target sparse matrix, must be atomic</param>
		/// <param name="threshold">Any element in <paramref name="source"/> less than or equals to this value will be regarded as 0</param>
		/// <param name="createFunc">See <see cref="DelegateCreateMatrixNew{T}"/></param>
		/// <param name="target">Output a created new <see cref="ISparseMatrix{T}"/> of the given properties</param>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="threshold"/> is less than 0 or <paramref name="format"/> is not atomic</exception>
		public abstract void MatrixDenseToSparse<T>(long m, long n, Storage<T> source, long ld, SparseMatrixFormat format, out ISparseMatrix<T> target, float threshold = 0, DelegateCreateMatrixNew<T>? createFunc = null) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, prune the given sparse matrix <paramref name="source"/> to a new one by filtering the values less than or equals to <paramref name="threshold"/>.
		/// </summary>
		/// <param name="source">The source sparse matrix to convert from</param>
		/// <param name="threshold">Any element in <paramref name="source"/> less than or equals to this value will be regarded as 0</param>
		/// <param name="target">Output a created new <see cref="ISparseMatrix{T}"/> of same properties as <paramref name="source"/> while the values (and the index arrays accordingly) are pruned by <paramref name="threshold"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="threshold"/> is less than 0</exception>
		public abstract void MatrixSparsePrune<T>(ISparseMatrix<T> source, float threshold, out ISparseMatrix<T> target) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, convert the format of the given sparse matrix <paramref name="source"/> to a new one which fits <paramref name="format"/>.
		/// </summary>
		/// <param name="source">The source sparse matrix to convert from</param>
		/// <param name="format">The target <see cref="SparseMatrixFormat"/>, can be anatomic</param>
		/// <param name="target">Output a created new <see cref="ISparseMatrix{T}"/> of desired <paramref name="format"/> while representing the same matrix as <paramref name="source"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> is null or invalid</exception>
		public abstract void MatrixSparseFormatConvert<T>(ISparseMatrix<T> source, SparseMatrixFormat format, out ISparseMatrix<T> target) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, fill the given sparse matrix <paramref name="M"/> with identity matrix.
		/// </summary>
		/// <param name="M">The sparse matrix to be filled with identity</param>
		/// <exception cref="ArgumentNullException">If <paramref name="M"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="M"/> is not a square matrix or its sparsity cannot be filled to be an identity matrix</exception>
		public abstract void MatrixSparseFillIdentity<T>(ISparseMatrix<T> M) where T : unmanaged;
		#endregion
	}
}