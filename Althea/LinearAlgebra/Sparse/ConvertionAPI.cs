using System;
using System.Collections.Generic;

using Althea.Arrays;
using Althea.Helpers;
using Althea.NativeTypes;


namespace Althea.LinearAlgebra.Sparse
{
	/// <summary>
	/// The abstract class for runtime sparse linear algebra API routines 
	/// </summary>
	public abstract partial class AbstractApi : AbstractRuntimeApi
	{
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

		/// <summary>
		/// When implemented by a derived class, check if the given <paramref name="location"/> is supported by index vector unary operations of this implementation or not.
		/// </summary>
		/// <param name="location">The given <see cref="CombinationOfLocations"/></param>
		/// <returns>Whether index vector unary operation on <paramref name="location"/> is supported by this <see cref="AbstractApi"/>.</returns>
		protected abstract bool IsSupportedIndexVectorUnary(CombinationOfLocations location);

		/// <summary>
		/// When implemented by a derived class, check if the given <paramref name="location1"/> and <paramref name="location2"/> are supported by index vector binary operations of this implementation or not.
		/// </summary>
		/// <param name="location1">The first given <see cref="CombinationOfLocations"/></param>
		/// <param name="location2">The second given <see cref="CombinationOfLocations"/></param>
		/// <returns>Whether index vector binary operation on <paramref name="location1"/> and <paramref name="location2"/> is supported by this <see cref="AbstractApi"/>.</returns>
		protected abstract bool IsSupportedIndexVectorBinary(CombinationOfLocations location1, CombinationOfLocations location2);
		#endregion


		#region static methods as dispatchers
		#region vector
		/// <summary>
		/// Set the <paramref name="x"/>'s values at certain <paramref name="positions"/> to the give <paramref name="value"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <typeparam name="TInd">Any integral-typed unmanaged struct as the index type</typeparam>
		/// <param name="x">The input vector whose values will be set</param>
		/// <param name="positions">The given positions as a <see cref="Storage{T}"/> of <see cref="int"/></param>
		/// <param name="value">The value to set</param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="positions"/> is null or invalid</exception>
		public static void VectorSetValuesAt<T, TInd>(Storage<T> x, T value, Storage<TInd> positions) where T : unmanaged where TInd : unmanaged
		{
			CombinationOfLocations location1 = x.LocationDescription, location2 = positions.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedVectorIndexType(Const<T>.DataType) && a.IsSupportedVectorUnary(location1) && a.IsSupportedIndexVectorUnary(location2), node);
				success = node.Value.VectorSetValuesAt_(x, value, positions);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// Scatter (and overwrite) the sparse vector <paramref name="x"/> to the dense vector <paramref name="y"/>: <paramref name="y"/>[<paramref name="x"/>.Indices] = <paramref name="x"/>.<see cref="ISparseArray{T}.Storage">Values</see>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The sparse vector x as a <see cref="ISparseVector{T}"/></param>
		/// <param name="y">The dense vector y as a <see cref="Storage{T}"/> whose elements at <paramref name="x"/>.Indices are overwritten</param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		public static void VectorSparseToDense<T>(ISparseVector<T> x, Storage<T> y) where T : unmanaged
		{
			CombinationOfLocations location1 = x.Storage.LocationDescription, location2 = y.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedVectorBinary(location1, location2) && a.IsSupportedSparseVector(x), node);
				success = node.Value.VectorSparseToDense_(x, y);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// Gather the dense vector <paramref name="x"/> at the underlying position array of <paramref name="y"/> into the <see cref="ISparseArray{T}.Storage"/> of sparse vector <paramref name="y"/>: <c><paramref name="y"/>.<see cref="ISparseArray{T}.Storage">Storage</see> = <paramref name="x"/>[<paramref name="y"/>.Position]</c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The input dense vector as a <see cref="Storage{T}"/> to gather from</param>
		/// <param name="y">The input (sparse index) and output (value array) sparse vector</param>
		/// <remarks>This is equivalent to converting dense vector to sparse vector when the sparsity is known</remarks>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		public static void VectorGatherValuesAt<T>(Storage<T> x, ISparseVector<T> y) where T : unmanaged
		{
			CombinationOfLocations location1 = x.LocationDescription, location2 = y.Storage.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedVectorBinary(location1, location2) && a.IsSupportedSparseVector(y), node);
				success = node.Value.VectorGatherValuesAt_(x, y);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// Convert a dense vector <paramref name="x"/> to a sparse vector by the given truncation <paramref name="threshold"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The input dense vector as a <see cref="Storage{T}"/></param>
		/// <param name="threshold">Any element in <paramref name="x"/> whose absolute value is less than or equals to <paramref name="threshold"/> will be regarded as zero</param>
		/// <param name="format">The desired <see cref="SparseVectorFormat"/> of the target sparse vector, can be anatomic</param>
		/// <returns>The created new <see cref="SparseArrayWrapper{T}"/> with format fitting <paramref name="format"/></returns>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="threshold"/> is less than 0</exception>
		public static SparseArrayWrapper<T> VectorDenseToSparse<T>(Storage<T> x, SparseVectorFormat format, float threshold = 0) where T : unmanaged
		{
			CombinationOfLocations location1 = x.LocationDescription;
			SparseArrayWrapper<T> result = default;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedVectorUnary(location1), node);
				success = node.Value.VectorDenseToSparse_(x, format, out result, threshold);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
			return result;
		}
		#endregion

		#region vector and matrix
		/// <summary>
		/// Convert the given sparse <paramref name="vector"/> to a sparse matrix of <paramref name="format"/> and presenting number of <paramref name="rows"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="vector">The input sparse vector as a <see cref="ISparseVector{T}"/></param>
		/// <param name="rows">The desired number of rows of the target sparse matrix (the number of columns is calculated from this)</param>
		/// <param name="format">The desired <see cref="SparseMatrixFormat"/> of the target sparse matrix, can be anatomic</param>
		/// <returns>The created new <see cref="SparseArrayWrapper{T}"/> with format fitting <paramref name="format"/> and size fitting <paramref name="rows"/></returns>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="vector"/> is null or invalid</exception>
		public static SparseArrayWrapper<T> SparseVectorToMatrix<T>(ISparseVector<T> vector, long rows, SparseMatrixFormat format) where T : unmanaged
		{
			CombinationOfLocations location1 = vector.Storage.LocationDescription;
			SparseArrayWrapper<T> result = default;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedVectorUnary(location1) && a.IsSupportedSparseVector(vector), node);
				success = node.Value.SparseVectorToMatrix_(vector, rows, format, out result);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
			return result;
		}

		/// <summary>
		/// Convert the given sparse <paramref name="format"/> to a sparse vector of given <paramref name="format"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="matrix">The input sparse matrix as a <see cref="ISparseMatrix{T}"/></param>
		/// <param name="format">The desired <see cref="SparseVectorFormat"/> of the target sparse vector, can be anatomic</param>
		/// <returns>The created new <see cref="SparseArrayWrapper{T}"/> with format fitting <paramref name="format"/> and desired properties (the length is the product of <see cref="IMatrix{T}.NRows"/> and <see cref="IMatrix{T}.NCols"/>)</returns>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="matrix"/> is null or invalid</exception>
		public static SparseArrayWrapper<T> SparseMatrixToVector<T>(ISparseMatrix<T> matrix, SparseVectorFormat format) where T : unmanaged
		{
			CombinationOfLocations location1 = matrix.Storage.LocationDescription;
			SparseArrayWrapper<T> result = default;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedMatrixUnary(location1) && a.IsSupportedSparseMatrix(matrix), node);
				success = node.Value.SparseMatrixToVector_(matrix, format, out result);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
			return result;
		}
		#endregion

		#region matrix
		/// <summary>
		/// Convert the given sparse matrix <paramref name="source"/> to a dense matrix <paramref name="destination"/>.
		/// </summary>
		/// <param name="source">The source sparse matrix to convert from</param>
		/// <param name="destination">The storage of the destination dense matrix of the same size as <paramref name="source"/></param>
		/// <param name="ld">The leading dimension of <paramref name="destination"/></param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="destination"/> is null or invalid</exception>
		public static void MatrixSparseToDense<T>(ISparseMatrix<T> source, Storage<T> destination, long ld) where T : unmanaged
		{
			CombinationOfLocations location1 = source.Storage.LocationDescription, location2 = destination.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedMatrixBinary(location1, location2) && a.IsSupportedSparseMatrix(source), node);
				success = node.Value.MatrixSparseToDense_(source, destination, ld);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// Convert the the given dense matrix <paramref name="source"/> to a sparse matrix of the given <paramref name="format"/>.
		/// </summary>
		/// <param name="m">The number of rows of <paramref name="source"/></param>
		/// <param name="n">The number of columns of <paramref name="source"/></param>
		/// <param name="source">The source dense matrix to convert from</param>
		/// <param name="ld">The leading dimension of <paramref name="source"/></param>
		/// <param name="format">The destination <see cref="SparseMatrixFormat"/> of the target sparse matrix, must be atomic</param>
		/// <param name="threshold">Any element in <paramref name="source"/> less than or equals to this value will be regarded as 0</param>
		/// <returns>The created new <see cref="SparseArrayWrapper{T}"/> of the given properties</returns>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="threshold"/> is less than 0 or <paramref name="format"/> is not atomic</exception>
		public static SparseArrayWrapper<T> MatrixDenseToSparse<T>(long m, long n, Storage<T> source, long ld, SparseMatrixFormat format, float threshold = 0) where T : unmanaged
		{
			CombinationOfLocations location1 = source.LocationDescription;
			SparseArrayWrapper<T> result = default;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedMatrixUnary(location1), node);
				success = node.Value.MatrixDenseToSparse_(m, n, source, ld, format, out result, threshold);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
			return result;
		}

		/// <summary>
		/// Prune the given sparse matrix <paramref name="source"/> to a new one by filtering the values less than or equals to <paramref name="threshold"/>.
		/// </summary>
		/// <param name="source">The source sparse matrix to convert from</param>
		/// <param name="threshold">Any element in <paramref name="source"/> less than or equals to this value will be regarded as 0</param>
		/// <returns>The created new <see cref="SparseArrayWrapper{T}"/> of same properties as <paramref name="source"/> while the values (and the index arrays accordingly) are pruned by <paramref name="threshold"/>; or <paramref name="source"/> itself if the no prune is necessary</returns>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="threshold"/> is less than 0</exception>
		public static SparseArrayWrapper<T> MatrixSparsePrune<T>(ISparseMatrix<T> source, float threshold) where T : unmanaged
		{
			CombinationOfLocations location1 = source.Storage.LocationDescription;
			SparseArrayWrapper<T> result = default;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedMatrixUnary(location1) && a.IsSupportedSparseMatrix(source), node);
				success = node.Value.MatrixSparsePrune_(source, threshold, out result);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
			return result;
		}

		/// <summary>
		/// Convert the format of the given sparse matrix <paramref name="source"/> to a new one which fits <paramref name="format"/>.
		/// </summary>
		/// <param name="source">The source sparse matrix to convert from</param>
		/// <param name="format">The target <see cref="SparseMatrixFormat"/>, can be anatomic</param>
		/// <param name="otherInfo">The target sparse matrix's <see cref="IOtherInfo"/>, default null means letting the internal implementation determine</param>
		/// <returns>The created new <see cref="SparseArrayWrapper{T}"/> of desired <paramref name="format"/> while representing the same matrix as <paramref name="source"/>; or <paramref name="source"/> it self if no conversion is necessary</returns>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> is null or invalid</exception>
		public static SparseArrayWrapper<T> MatrixSparseFormatConvert<T>(ISparseMatrix<T> source, SparseMatrixFormat format, IOtherInfo? otherInfo = null) where T : unmanaged
		{
			CombinationOfLocations location1 = source.Storage.LocationDescription;
			SparseArrayWrapper<T> result = default;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedMatrixUnary(location1) && a.IsSupportedSparseMatrix(source), node);
				success = node.Value.MatrixSparseFormatConvert_(source, format, out result, otherInfo);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
			return result;
		}

		/// <summary>
		/// Reshape the given sparse matrix <paramref name="source"/> to a new one with <paramref name="newNRows"/>.
		/// </summary>
		/// <param name="source">The source sparse matrix to convert from</param>
		/// <param name="newNRows">The target sparse matrix's number of rows</param>
		/// <returns>A created new <see cref="ISparseMatrix{T}"/> of desired <paramref name="newNRows"/>.</returns>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="newNRows"/> is out of range</exception>
		public static SparseArrayWrapper<T> MatrixSparseReshape<T>(ISparseMatrix<T> source, long newNRows) where T : unmanaged
		{
			CombinationOfLocations location1 = source.Storage.LocationDescription;
			SparseArrayWrapper<T> result = default;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedMatrixUnary(location1) && a.IsSupportedSparseMatrix(source), node);
				success = node.Value.MatrixSparseReshape_(source, newNRows, out result);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
			return result;
		}
		#endregion

		#region index only
		/// <summary>
		/// Find the maximum value of the given <b>sorted</b> integer-typed <paramref name="array"/>.
		/// </summary>
		/// <typeparam name="TInd">Any integral-typed unmanaged struct as the index type</typeparam>
		/// <param name="array">The storage of the integer-typed array</param>
		/// <returns>The maximum value of <paramref name="array"/></returns>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="array"/> is null or invalid</exception>
		/// <exception cref="TypeMismatchException">If <typeparamref name="TInd"/> is not an integral type</exception>
		public static TInd IndexMax<TInd>(Storage<TInd> array) where TInd : unmanaged
		{
			CombinationOfLocations location = array.LocationDescription;
			TInd result = default;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedIndexVectorUnary(location), node);
				success = node.Value.IndexMax_(array, out result);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
			return result;
		}

		/// <summary>
		/// Find the minimum value of the given <b>sorted</b> integer-typed <paramref name="array"/>.
		/// </summary>
		/// <typeparam name="TInd">Any integral-typed unmanaged struct as the index type</typeparam>
		/// <param name="array">The storage of the integer-typed array</param>
		/// <returns>The minimum value of <paramref name="array"/></returns>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="array"/> is null or invalid</exception>
		/// <exception cref="TypeMismatchException">If <typeparamref name="TInd"/> is not an integral type</exception>
		public static TInd IndexMin<TInd>(Storage<TInd> array) where TInd : unmanaged
		{
			CombinationOfLocations location = array.LocationDescription;
			TInd result = default;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedIndexVectorUnary(location), node);
				success = node.Value.IndexMin_(array, out result);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
			return result;
		}

		/// <summary>
		/// Find the zero-based index of the target <paramref name="value"/> in the given <b>sorted</b> integer-typed <paramref name="array"/>.
		/// </summary>
		/// <typeparam name="TInd">Any integral-typed unmanaged struct as the index type</typeparam>
		/// <param name="sorted">Whether <paramref name="array"/> is sorted or not</param>
		/// <param name="array">The storage of the integer-typed array</param>
		/// <param name="value">The target value to find</param>
		/// <returns>The zero-based index of the target <paramref name="value"/> in <paramref name="array"/></returns>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="array"/> is null or invalid</exception>
		/// <exception cref="TypeMismatchException">If <typeparamref name="TInd"/> is not an integral type</exception>
		public static long IndexFind<TInd>(bool sorted, Storage<TInd> array, TInd value) where TInd : unmanaged
		{
			CombinationOfLocations location = array.LocationDescription;
			long result = default;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedIndexVectorUnary(location), node);
				success = node.Value.IndexFind_(sorted, array, value, out result);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
			return result;
		}

		/// <summary>
		/// Find the zero-based index of the target <paramref name="value"/> as a (inclusive) lower / (exclusive) upper bound in the given <b>sorted</b> integer-typed <paramref name="array"/>.
		/// </summary>
		/// <typeparam name="TInd">Any integral-typed unmanaged struct as the index type</typeparam>
		/// <param name="array">The storage of the <b>sorted</b> integer-typed array</param>
		/// <param name="value">The target value to find</param>
		/// <param name="lowerBound">Whether to find the first element in <paramref name="array"/> whose value is not less than <paramref name="value"/> or the first element in <paramref name="array"/> whose value is larger than <paramref name="value"/></param>
		/// <returns>The zero-based index of the target bound in <paramref name="array"/></returns>
		/// <remarks>If not found, returns -1 if <paramref name="lowerBound"/> is true or <paramref name="array"/>.<see cref="Storage{T}.Length">Length</see> otherwise.</remarks>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="array"/> is null or invalid</exception>
		/// <exception cref="TypeMismatchException">If <typeparamref name="TInd"/> is not an integral type</exception>
		public static long IndexBound<TInd>(Storage<TInd> array, TInd value, bool lowerBound) where TInd : unmanaged
		{
			CombinationOfLocations location = array.LocationDescription;
			long result = default;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedIndexVectorUnary(location), node);
				success = node.Value.IndexBound_(array, value, lowerBound, out result);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
			return result;
		}

		/// <summary>
		/// Find the zero-based indices from <paramref name="start"/> to <paramref name="end"/> as (inclusive) lower / (exclusive) upper bounds in the given <b>sorted</b> integer-typed <paramref name="array"/> and store the result to <paramref name="target"/>.
		/// </summary>
		/// <typeparam name="TInd">Any integral-typed unmanaged struct as the index type</typeparam>
		/// <typeparam name="TIndOut">Any integral-typed unmanaged struct as the output index type</typeparam>
		/// <param name="array">The storage of the <b>sorted</b> integer-typed array</param>
		/// <param name="target">The storage of the result indices, must has length larger than <paramref name="end"/> - <paramref name="start"/></param>
		/// <param name="start">The inclusive start value to find</param>
		/// <param name="end">The inclusive end value to find</param>
		/// <param name="lowerBound">Whether to find the index of the first element in <paramref name="array"/> who is not less than the given value or the first who is larger than the given value</param>
		/// <remarks>If some value is not found, the corresponding index in <paramref name="target"/> is -1 if <paramref name="lowerBound"/> is true or <paramref name="array"/>.<see cref="Storage{T}.Length">Length</see> otherwise.</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="array"/> or <paramref name="target"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="target"/>'s length is too short or <paramref name="end"/> is less than <paramref name="start"/></exception>
		/// <exception cref="TypeMismatchException">If <typeparamref name="TInd"/> or <typeparamref name="TIndOut"/> is not an integral type</exception>
		public static void IndexGetAllBounds<TInd, TIndOut>(Storage<TInd> array, Storage<TIndOut> target, TInd start, TInd end, bool lowerBound)
			where TInd : unmanaged
			where TIndOut : unmanaged
		{
			CombinationOfLocations location1 = array.LocationDescription, location2 = target.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedIndexVectorBinary(location1, location2), node);
				success = node.Value.IndexGetAllBounds_(array, target, start, end, lowerBound);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// Reverse the operation of <see cref="IndexGetAllBounds"/> to get the sorted <paramref name="target"/> array from the given <paramref name="bounds"/>.
		/// </summary>
		/// <typeparam name="TInd">Any integral-typed unmanaged struct as the bound index type</typeparam>
		/// <typeparam name="TIndOut">Any integral-typed unmanaged struct as the output index type</typeparam>
		/// <param name="bounds">The storage of the bound index array, usually generated from <see cref="IndexGetAllBounds"/></param>
		/// <param name="target">The storage of the result indices, must has length ≥ the last element in <paramref name="bounds"/></param>
		/// <param name="start">The start value to fill in <paramref name="target"/></param>
		/// <param name="lowerBound">Whether to fill the <paramref name="target"/> with <paramref name="bounds"/> regarded as lower bounds or upper bounds</param>
		/// <exception cref="ArgumentNullException">If <paramref name="bounds"/> or <paramref name="target"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="target"/>'s length is too short</exception>
		/// <exception cref="TypeMismatchException">If <typeparamref name="TInd"/> or <typeparamref name="TIndOut"/> is not an integral type</exception>
		public static void IndexGenerateFromBounds<TInd, TIndOut>(Storage<TInd> bounds, Storage<TIndOut> target, bool lowerBound, TIndOut start = default)
			where TInd : unmanaged
			where TIndOut : unmanaged
		{
			CombinationOfLocations location1 = bounds.LocationDescription, location2 = target.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedIndexVectorBinary(location1, location2), node);
				success = node.Value.IndexGenerateFromBounds_(bounds, target, lowerBound, start);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}
		#endregion
		#endregion


		#region abstract methods that actually do computations
		#region vector
		/// <summary>
		/// When implemented by a derived class, set the <paramref name="x"/>'s values at certain <paramref name="positions"/> to the give <paramref name="value"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <typeparam name="TInd">Any integral-typed unmanaged struct as the index type</typeparam>
		/// <param name="x">The input vector whose values will be set</param>
		/// <param name="positions">The given positions as a <see cref="Storage{T}"/> of <see cref="int"/></param>
		/// <param name="value">The value to set</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="positions"/> is null or invalid</exception>
		protected abstract bool VectorSetValuesAt_<T, TInd>(Storage<T> x, T value, Storage<TInd> positions) where T : unmanaged where TInd : unmanaged;

		/// <summary>
		/// When implemented by a derived class, scatter (and overwrite) the sparse vector <paramref name="x"/> to the dense vector <paramref name="y"/>: <paramref name="y"/>[<paramref name="x"/>.Indices] = <paramref name="x"/>.<see cref="ISparseArray{T}.Storage">Values</see>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The sparse vector x as a <see cref="ISparseVector{T}"/></param>
		/// <param name="y">The dense vector y as a <see cref="Storage{T}"/> whose elements at <paramref name="x"/>.Indices are overwritten</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		protected abstract bool VectorSparseToDense_<T>(ISparseVector<T> x, Storage<T> y) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, gather the dense vector <paramref name="x"/> at the underlying position array of <paramref name="y"/> into the <see cref="ISparseArray{T}.Storage"/> of sparse vector <paramref name="y"/>: <c><paramref name="y"/>.<see cref="ISparseArray{T}.Storage">Storage</see> = <paramref name="x"/>[<paramref name="y"/>.Position]</c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The input dense vector as a <see cref="Storage{T}"/> to gather from</param>
		/// <param name="y">The input (sparse index) and output (value array) sparse vector</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <remarks>This is equivalent to converting dense vector to sparse vector when the sparsity is known</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		protected abstract bool VectorGatherValuesAt_<T>(Storage<T> x, ISparseVector<T> y) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, convert a dense vector <paramref name="x"/> to a sparse vector by the given truncation <paramref name="threshold"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The input dense vector as a <see cref="Storage{T}"/></param>
		/// <param name="threshold">Any element in <paramref name="x"/> whose absolute value is less than or equals to <paramref name="threshold"/> will be regarded as zero</param>
		/// <param name="format">The desired <see cref="SparseVectorFormat"/> of the target sparse vector, can be anatomic</param>
		/// <param name="target">Output the created new <see cref="ISparseVector{T}"/> with format fitting <paramref name="format"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="threshold"/> is less than 0</exception>
		protected abstract bool VectorDenseToSparse_<T>(Storage<T> x, SparseVectorFormat format, out SparseArrayWrapper<T> target, float threshold = 0) where T : unmanaged;
		#endregion

		#region vector and matrix
		/// <summary>
		/// When implemented by a derived class, convert the given sparse <paramref name="vector"/> to a sparse matrix of <paramref name="format"/> and presenting number of <paramref name="rows"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="vector">The input sparse vector as a <see cref="ISparseVector{T}"/></param>
		/// <param name="rows">The desired number of rows of the target sparse matrix (the number of columns is calculated from this)</param>
		/// <param name="format">The desired <see cref="SparseMatrixFormat"/> of the target sparse matrix, can be anatomic</param>
		/// <param name="target">Output the created new <see cref="ISparseMatrix{T}"/> with format fitting <paramref name="format"/> and size fitting <paramref name="rows"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="vector"/> is null or invalid</exception>
		protected abstract bool SparseVectorToMatrix_<T>(ISparseVector<T> vector, long rows, SparseMatrixFormat format, out SparseArrayWrapper<T> target) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, convert the given sparse <paramref name="format"/> to a sparse vector of given <paramref name="format"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="matrix">The input sparse matrix as a <see cref="ISparseMatrix{T}"/></param>
		/// <param name="format">The desired <see cref="SparseVectorFormat"/> of the target sparse vector, can be anatomic</param>
		/// <param name="target">Output the created new <see cref="ISparseVector{T}"/> with format fitting <paramref name="format"/> and desired properties (the length is the product of <see cref="IMatrix{T}.NRows"/> and <see cref="IMatrix{T}.NCols"/>)</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="matrix"/> is null or invalid</exception>
		protected abstract bool SparseMatrixToVector_<T>(ISparseMatrix<T> matrix, SparseVectorFormat format, out SparseArrayWrapper<T> target) where T : unmanaged;
		#endregion

		#region matrix
		/// <summary>
		/// When implemented by a derived class, convert the given sparse matrix <paramref name="source"/> to a dense matrix <paramref name="destination"/>.
		/// </summary>
		/// <param name="source">The source sparse matrix to convert from</param>
		/// <param name="destination">The storage of the destination dense matrix of the same size as <paramref name="source"/></param>
		/// <param name="ld">The leading dimension of <paramref name="destination"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="destination"/> is null or invalid</exception>
		protected abstract bool MatrixSparseToDense_<T>(ISparseMatrix<T> source, Storage<T> destination, long ld) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, convert the given dense matrix <paramref name="source"/> to a sparse matrix of the given <paramref name="format"/>.
		/// </summary>
		/// <param name="m">The number of rows of <paramref name="source"/></param>
		/// <param name="n">The number of columns of <paramref name="source"/></param>
		/// <param name="source">The source dense matrix to convert from</param>
		/// <param name="ld">The leading dimension of <paramref name="source"/></param>
		/// <param name="format">The destination <see cref="SparseMatrixFormat"/> of the target sparse matrix, must be atomic</param>
		/// <param name="threshold">Any element in <paramref name="source"/> less than or equals to this value will be regarded as 0</param>
		/// <param name="target">Output a created new <see cref="ISparseMatrix{T}"/> of the given properties</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="threshold"/> is less than 0 or <paramref name="format"/> is not atomic</exception>
		protected abstract bool MatrixDenseToSparse_<T>(long m, long n, Storage<T> source, long ld, SparseMatrixFormat format, out SparseArrayWrapper<T> target, float threshold = 0) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, prune the given sparse matrix <paramref name="source"/> to a new one by filtering the values less than or equals to <paramref name="threshold"/>.
		/// </summary>
		/// <param name="source">The source sparse matrix to convert from</param>
		/// <param name="threshold">Any element in <paramref name="source"/> less than or equals to this value will be regarded as 0</param>
		/// <param name="target">Output a created new <see cref="ISparseMatrix{T}"/> of same properties as <paramref name="source"/> while the values (and the index arrays accordingly) are pruned by <paramref name="threshold"/>; or <paramref name="source"/> it self if no conversion is necessary</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="threshold"/> is less than 0</exception>
		protected abstract bool MatrixSparsePrune_<T>(ISparseMatrix<T> source, float threshold, out SparseArrayWrapper<T> target) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, convert the format of the given sparse matrix <paramref name="source"/> to a new one which fits <paramref name="format"/>.
		/// </summary>
		/// <param name="source">The source sparse matrix to convert from</param>
		/// <param name="format">The target <see cref="SparseMatrixFormat"/>, can be anatomic</param>
		/// <param name="target">Output a created new <see cref="ISparseMatrix{T}"/> of desired <paramref name="format"/> while representing the same matrix as <paramref name="source"/>; or <paramref name="source"/> it self if no conversion is necessary</param>
		/// <param name="otherInfo">The target sparse matrix's <see cref="IOtherInfo"/>, default null means letting the internal implementation determine</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> is null or invalid</exception>
		protected abstract bool MatrixSparseFormatConvert_<T>(ISparseMatrix<T> source, SparseMatrixFormat format, out SparseArrayWrapper<T> target, IOtherInfo? otherInfo = null) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, reshape the given sparse matrix <paramref name="source"/> to a new one with <paramref name="newNRows"/>.
		/// </summary>
		/// <param name="source">The source sparse matrix to convert from</param>
		/// <param name="newNRows">The target sparse matrix's number of rows</param>
		/// <param name="target">Output a created new <see cref="ISparseMatrix{T}"/> of desired <paramref name="newNRows"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="newNRows"/> is out of range</exception>
		protected abstract bool MatrixSparseReshape_<T>(ISparseMatrix<T> source, long newNRows, out SparseArrayWrapper<T> target) where T : unmanaged;
		#endregion

		#region index only
		/// <summary>
		/// When implemented by a derived class, find the maximum value of the given <b>sorted</b> integer-typed <paramref name="array"/>.
		/// </summary>
		/// <typeparam name="TInd">Any integral-typed unmanaged struct as the index type</typeparam>
		/// <param name="array">The storage of the integer-typed array</param>
		/// <param name="max">Output the maximum value</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="array"/> is null or invalid</exception>
		/// <exception cref="TypeMismatchException">If <typeparamref name="TInd"/> is not an integral type</exception>
		protected abstract bool IndexMax_<TInd>(Storage<TInd> array, out TInd max) where TInd : unmanaged;

		/// <summary>
		/// When implemented by a derived class, find the minimum value of the given <b>sorted</b> integer-typed <paramref name="array"/>.
		/// </summary>
		/// <typeparam name="TInd">Any integral-typed unmanaged struct as the index type</typeparam>
		/// <param name="array">The storage of the integer-typed array</param>
		/// <param name="min">Output the minimum value</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="array"/> is null or invalid</exception>
		/// <exception cref="TypeMismatchException">If <typeparamref name="TInd"/> is not an integral type</exception>
		protected abstract bool IndexMin_<TInd>(Storage<TInd> array, out TInd min) where TInd : unmanaged;

		/// <summary>
		/// When implemented by a derived class, find the zero-based index of the target <paramref name="value"/> in the given <b>sorted</b> integer-typed <paramref name="array"/>.
		/// </summary>
		/// <typeparam name="TInd">Any integral-typed unmanaged struct as the index type</typeparam>
		/// <param name="sorted">Whether <paramref name="array"/> is sorted or not</param>
		/// <param name="array">The storage of the integer-typed array</param>
		/// <param name="value">The target value to find</param>
		/// <param name="find">Output the zero-based index of the target <paramref name="value"/> in <paramref name="array"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="array"/> is null or invalid</exception>
		/// <exception cref="TypeMismatchException">If <typeparamref name="TInd"/> is not an integral type</exception>
		protected abstract bool IndexFind_<TInd>(bool sorted, Storage<TInd> array, TInd value, out long find) where TInd : unmanaged;

		/// <summary>
		/// When implemented by a derived class, find the zero-based index of the target <paramref name="value"/> as a (inclusive) lower / (exclusive) upper bound in the given <b>sorted</b> integer-typed <paramref name="array"/>.
		/// </summary>
		/// <typeparam name="TInd">Any integral-typed unmanaged struct as the index type</typeparam>
		/// <param name="array">The storage of the <b>sorted</b> integer-typed array</param>
		/// <param name="value">The target value to find</param>
		/// <param name="lowerBound">Whether to find the first element in <paramref name="array"/> whose value is not less than <paramref name="value"/> or the first element in <paramref name="array"/> whose value is larger than <paramref name="value"/></param>
		/// <param name="index">Output the zero-based index of the target bound in <paramref name="array"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <remarks>If not found, <paramref name="index"/> shall be -1 if <paramref name="lowerBound"/> is true or <paramref name="array"/>.<see cref="Storage{T}.Length">Length</see> otherwise.</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="array"/> is null or invalid</exception>
		/// <exception cref="TypeMismatchException">If <typeparamref name="TInd"/> is not an integral type</exception>
		protected abstract bool IndexBound_<TInd>(Storage<TInd> array, TInd value, bool lowerBound, out long index) where TInd : unmanaged;

		/// <summary>
		/// When implemented by a derived class, find the zero-based indices from <paramref name="start"/> to <paramref name="end"/> as (inclusive) lower / (exclusive) upper bounds in the given <b>sorted</b> integer-typed <paramref name="array"/> and store the result to <paramref name="target"/>.
		/// </summary>
		/// <typeparam name="TInd">Any integral-typed unmanaged struct as the index type</typeparam>
		/// <typeparam name="TIndOut">Any integral-typed unmanaged struct as the output index type</typeparam>
		/// <param name="array">The storage of the <b>sorted</b> integer-typed array</param>
		/// <param name="target">The storage of the result indices, must has length larger than <paramref name="end"/> - <paramref name="start"/></param>
		/// <param name="start">The inclusive start value to find</param>
		/// <param name="end">The inclusive end value to find</param>
		/// <param name="lowerBound">Whether to find the index of the first element in <paramref name="array"/> who is not less than the given value or the first who is larger than the given value</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <remarks>If not found, the corresponding index in <paramref name="target"/> shall be -1 if <paramref name="lowerBound"/> is true or <paramref name="array"/>.<see cref="Storage{T}.Length">Length</see> otherwise.</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="array"/> or <paramref name="target"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="target"/>'s length is too short or <paramref name="end"/> is less than <paramref name="start"/></exception>
		/// <exception cref="TypeMismatchException">If <typeparamref name="TInd"/> or <typeparamref name="TIndOut"/> is not an integral type</exception>
		protected abstract bool IndexGetAllBounds_<TInd, TIndOut>(Storage<TInd> array, Storage<TIndOut> target, TInd start, TInd end, bool lowerBound)
			where TInd : unmanaged
			where TIndOut : unmanaged;

		/// <summary>
		/// When implemented by a derived class, reverse the operation of <see cref="IndexGetAllBounds_"/> to get the sorted <paramref name="target"/> array from the given <paramref name="bounds"/>.
		/// </summary>
		/// <typeparam name="TInd">Any integral-typed unmanaged struct as the bound index type</typeparam>
		/// <typeparam name="TIndOut">Any integral-typed unmanaged struct as the output index type</typeparam>
		/// <param name="bounds">The storage of the bound index array, usually generated from <see cref="IndexGetAllBounds_"/></param>
		/// <param name="target">The storage of the result indices, must has length ≥ the last element in <paramref name="bounds"/></param>
		/// <param name="start">The start value to fill in <paramref name="target"/></param>
		/// <param name="lowerBound">Whether to fill the <paramref name="target"/> with <paramref name="bounds"/> regarded as lower bounds or upper bounds</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="bounds"/> or <paramref name="target"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="target"/>'s length is too short</exception>
		/// <exception cref="TypeMismatchException">If <typeparamref name="TInd"/> or <typeparamref name="TIndOut"/> is not an integral type</exception>
		protected abstract bool IndexGenerateFromBounds_<TInd, TIndOut>(Storage<TInd> bounds, Storage<TIndOut> target, bool lowerBound, TIndOut start = default)
			where TInd : unmanaged
			where TIndOut : unmanaged;
		#endregion
		#endregion
	}
}