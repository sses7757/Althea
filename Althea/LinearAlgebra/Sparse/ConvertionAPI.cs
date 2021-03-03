using System;
using System.Dynamic;
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

		private static readonly LinkedList<AbstractApi> RecentAPIs = new();

		internal static bool SetImplementation(Type implementation) => SetImplementation(RecentAPIs, implementation);
		#endregion


		#region dynamic invocation
		/// <summary>
		/// Get the dynamic object used to dynamically invoke method(s) not listed explicitly here (the methods extra defined in derived classes)
		/// </summary>
		/// <remarks>
		/// Due to the limitations of dynamic invocation, <c>ref</c>, <c>in</c>, <c>out</c> and <c>ref struct</c>, etc. are not supported and non of the input arguments can be null.<br/>
		/// Since there are internal caching for <see cref="DynamicObject.TryInvokeMember(InvokeMemberBinder, object[], out object)"/>, the average repeated dynamic invocation may cost around 1 microsecond.
		/// </remarks>
		/// <example><code>
		/// long n = AbstractApi.Dynamic.SparseMatrixGetNonEmptyRows(...);
		/// </code></example>
		public static dynamic Dynamic => singletonDynamic;

		private static readonly DynamicInvocations singletonDynamic = new();

		private sealed class DynamicInvocations : DynamicInvocation
		{
			public override bool TryInvokeMember(InvokeMemberBinder binder, object?[]? args, out object? result)
			{
				result = DynamicInvokeExtraMethod(RecentAPIs, binder.Name, args);
				return true;
			}
		}
		#endregion


		#region get empty sparse arrays
		private static readonly Dictionary<(DataType type, bool vector), object> cache_emptySparseArray = new();

		private static ISparseVector<T> GetEmptySparseVector<T>() where T : unmanaged
		{
			DataType type = default(T).ToDataType();
			var key = (type, vector: true);
			if (!cache_emptySparseArray.ContainsKey(key))
			{
				var value = typeof(Backend.Arrays.SparseVector<float, int>)
									.MakeGenericType(typeof(T), typeof(int))
									.GetConstructor(Type.EmptyTypes)?
									.Invoke(null);
				if (value is null)
					throw new NotSupportedException(Resources.Support.DataType);
				cache_emptySparseArray.Add(key, value);
			}
			return (ISparseVector<T>)cache_emptySparseArray[key];
		}

		private static ISparseMatrix<T> GetEmptySparseMatrix<T>() where T : unmanaged
		{
			DataType type = default(T).ToDataType();
			var key = (type, vector: false);
			if (!cache_emptySparseArray.ContainsKey(key))
			{
				var value = typeof(Backend.Arrays.SparseMatrix<float, int>)
									.MakeGenericType(typeof(T), typeof(int))
									.GetConstructor(Type.EmptyTypes)?
									.Invoke(null);
				if (value is null)
					throw new NotSupportedException(Resources.Support.DataType);
				cache_emptySparseArray.Add(key, value);
			}
			return (ISparseMatrix<T>)cache_emptySparseArray[key];
		}
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
		/// <param name="nonDefaults">The desired total number of non-default values</param>
		/// <param name="format">The desired <see cref="SparseVectorFormat"/></param>
		/// <param name="defaultValue">The desired default value as a <typeparamref name="T"/></param>
		/// <returns>An <b>allocated</b> new <see cref="ISparseVector{T}"/> of <paramref name="length"/> and <paramref name="format"/></returns>
		/// <remarks>
		/// This delegate is usually used as a nullable parameter of methods in <see cref="AbstractApi"/>.<br/>
		/// The default implementation typically shall utilize <c><see cref="Storage.StorageFactory{T}"/>.<see cref="Storage.StorageFactory{T}.CreateAlike">CreateAlike</see>(input_storage.<see cref="Storage{T}.MakeReference">MakeReference</see>(0, <paramref name="length"/>))</c>
		/// </remarks>
		public delegate ISparseVector<T> DelegateCreateVectorNew<T>(long length, long nonDefaults, SparseVectorFormat format, T defaultValue) where T : unmanaged;

		/// <summary>
		/// Encapsulates a method that receive the presenting number of rows <paramref name="rows"/> and number of columns <paramref name="cols"/> in <typeparamref name="T"/> and the <paramref name="format"/> as the parameters and return an <b>allocated</b> new <see cref="ISparseMatrix{T}"/> of the given size.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="rows">The desired presenting number of rows in <typeparamref name="T"/></param>
		/// <param name="cols">The desired presenting number of columns in <typeparamref name="T"/></param>
		/// <param name="nonDefaults">The desired total number of non-default values</param>
		/// <param name="format">The desired <see cref="SparseMatrixFormat"/> of the target sparse matrix, must be atomic</param>
		/// <param name="defaultValue">The desired default value as a <typeparamref name="T"/></param>
		/// <returns>An <b>allocated</b> new <see cref="ISparseMatrix{T}"/> of the given size</returns>
		/// <remarks>
		/// This delegate is usually used as a nullable parameter of methods in <see cref="AbstractApi"/>.<br/>
		/// The default implementation typically shall utilize (multiple) <c><see cref="Storage.StorageFactory{T}"/>.<see cref="Storage.StorageFactory{T}.CreateAlike">CreateAlike</see>(input_storage.<see cref="Storage{T}.MakeReference">MakeReference</see>(0, internal_length))</c>
		/// </remarks>
		public delegate ISparseMatrix<T> DelegateCreateMatrixNew<T>(long rows, long cols, long nonDefaults, SparseMatrixFormat format, T defaultValue) where T : unmanaged;
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
		/// <exception cref="NullReferenceException">If <paramref name="x"/> or <paramref name="positions"/> is null or invalid</exception>
		public static void VectorSetValuesAt<T, TInd>(Storage<T> x, T value, Storage<TInd> positions) where T : unmanaged where TInd : unmanaged
		{
			CombinationOfLocations location1 = x.LocationDescription, location2 = positions.LocationDescription;
			DataType indexType = default(TInd).ToDataType();
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedVectorIndexType(indexType) && a.IsSupportedVectorUnary(location1) && a.IsSupportedIndexVectorUnary(location2), node);
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
		/// <exception cref="NullReferenceException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
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
		/// <exception cref="NullReferenceException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
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
		/// <param name="createFunc">See <see cref="DelegateCreateMatrixNew{T}"/></param>
		/// <returns>The created new <see cref="ISparseVector{T}"/> with format fitting <paramref name="format"/></returns>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="threshold"/> is less than 0</exception>
		public static ISparseVector<T> VectorDenseToSparse<T>(Storage<T> x, SparseVectorFormat format, float threshold = 0, DelegateCreateMatrixNew<T>? createFunc = null) where T : unmanaged
		{
			CombinationOfLocations location1 = x.LocationDescription;
			ISparseVector<T> result = GetEmptySparseVector<T>();
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedVectorUnary(location1), node);
				success = node.Value.VectorDenseToSparse_(x, format, out result, threshold, createFunc);
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
		/// <param name="createFunc">See <see cref="DelegateCreateMatrixNew{T}"/></param>
		/// <returns>The created new <see cref="ISparseMatrix{T}"/> with format fitting <paramref name="format"/> and size fitting <paramref name="rows"/></returns>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="vector"/> is null or invalid</exception>
		public static ISparseMatrix<T> SparseVectorToMatrix<T>(ISparseVector<T> vector, long rows, SparseMatrixFormat format, DelegateCreateMatrixNew<T>? createFunc = null) where T : unmanaged
		{
			CombinationOfLocations location1 = vector.Storage.LocationDescription;
			ISparseMatrix<T> result = GetEmptySparseMatrix<T>();
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedVectorUnary(location1) && a.IsSupportedSparseVector(vector), node);
				success = node.Value.SparseVectorToMatrix_(vector, rows, format, out result, createFunc);
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
		/// <param name="createFunc">See <see cref="DelegateCreateVectorNew{T}"/></param>
		/// <returns>The created new <see cref="ISparseVector{T}"/> with format fitting <paramref name="format"/> and desired properties (the length is the product of <see cref="ISparseMatrix{T}.NRows"/> and <see cref="ISparseMatrix{T}.NCols"/>)</returns>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="matrix"/> is null or invalid</exception>
		public static ISparseVector<T> SparseMatrixToVector<T>(ISparseMatrix<T> matrix, SparseVectorFormat format, DelegateCreateVectorNew<T>? createFunc = null) where T : unmanaged
		{
			CombinationOfLocations location1 = matrix.Storage.LocationDescription;
			ISparseVector<T> result = GetEmptySparseVector<T>();
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedMatrixUnary(location1) && a.IsSupportedSparseMatrix(matrix), node);
				success = node.Value.SparseMatrixToVector_(matrix, format, out result, createFunc);
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
		/// <exception cref="NullReferenceException">If <paramref name="source"/> or <paramref name="destination"/> is null or invalid</exception>
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
		/// Convert the given dense matrix <paramref name="source"/> of to the <paramref name="format"/> format.
		/// </summary>
		/// <param name="m">The number of rows of <paramref name="source"/></param>
		/// <param name="n">The number of columns of <paramref name="source"/></param>
		/// <param name="source">The source dense matrix to convert from</param>
		/// <param name="ld">The leading dimension of <paramref name="source"/></param>
		/// <param name="format">The destination <see cref="SparseMatrixFormat"/> of the target sparse matrix, must be atomic</param>
		/// <param name="threshold">Any element in <paramref name="source"/> less than or equals to this value will be regarded as 0</param>
		/// <param name="createFunc">See <see cref="DelegateCreateMatrixNew{T}"/></param>
		/// <returns>The created new <see cref="ISparseMatrix{T}"/> of the given properties</returns>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="source"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="threshold"/> is less than 0 or <paramref name="format"/> is not atomic</exception>
		public static ISparseMatrix<T> MatrixDenseToSparse<T>(long m, long n, Storage<T> source, long ld, SparseMatrixFormat format, float threshold = 0, DelegateCreateMatrixNew<T>? createFunc = null) where T : unmanaged
		{
			CombinationOfLocations location1 = source.LocationDescription;
			ISparseMatrix<T> result = GetEmptySparseMatrix<T>();
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedMatrixUnary(location1), node);
				success = node.Value.MatrixDenseToSparse_(m, n, source, ld, format, out result, threshold, createFunc);
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
		/// <returns>The created new <see cref="ISparseMatrix{T}"/> of same properties as <paramref name="source"/> while the values (and the index arrays accordingly) are pruned by <paramref name="threshold"/>; or <paramref name="source"/> itself if the no prune is necessary</returns>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="source"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="threshold"/> is less than 0</exception>
		public static ISparseMatrix<T> MatrixSparsePrune<T>(ISparseMatrix<T> source, float threshold) where T : unmanaged
		{
			CombinationOfLocations location1 = source.Storage.LocationDescription;
			ISparseMatrix<T> result = GetEmptySparseMatrix<T>();
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
		/// <returns>The created new <see cref="ISparseMatrix{T}"/> of desired <paramref name="format"/> while representing the same matrix as <paramref name="source"/>; or <paramref name="source"/> it self if no conversion is necessary</returns>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="source"/> is null or invalid</exception>
		public static ISparseMatrix<T> MatrixSparseFormatConvert<T>(ISparseMatrix<T> source, SparseMatrixFormat format) where T : unmanaged
		{
			CombinationOfLocations location1 = source.Storage.LocationDescription;
			ISparseMatrix<T> result = GetEmptySparseMatrix<T>();
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedMatrixUnary(location1) && a.IsSupportedSparseMatrix(source), node);
				success = node.Value.MatrixSparseFormatConvert_(source, format, out result);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
			return result;
		}

		/// <summary>
		/// Fill the given sparse matrix <paramref name="M"/> with identity matrix.
		/// </summary>
		/// <param name="M">The sparse matrix to be filled with identity</param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="M"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="M"/> is not a square matrix or its sparsity cannot be filled to be an identity matrix</exception>
		public static void MatrixSparseFillIdentity<T>(ISparseMatrix<T> M) where T : unmanaged
		{
			CombinationOfLocations location1 = M.Storage.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedMatrixUnary(location1) && a.IsSupportedSparseMatrix(M), node);
				success = node.Value.MatrixSparseFillIdentity_(M);
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
		/// <param name="createFunc">See <see cref="DelegateCreateMatrixNew{T}"/></param>
		/// <param name="target">Output the created new <see cref="ISparseVector{T}"/> with format fitting <paramref name="format"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="threshold"/> is less than 0</exception>
		protected abstract bool VectorDenseToSparse_<T>(Storage<T> x, SparseVectorFormat format, out ISparseVector<T> target, float threshold = 0, DelegateCreateMatrixNew<T>? createFunc = null) where T : unmanaged;
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
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="vector"/> is null or invalid</exception>
		protected abstract bool SparseVectorToMatrix_<T>(ISparseVector<T> vector, long rows, SparseMatrixFormat format, out ISparseMatrix<T> target, DelegateCreateMatrixNew<T>? createFunc = null) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, convert the given sparse <paramref name="format"/> to a sparse vector of given <paramref name="format"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="matrix">The input sparse matrix as a <see cref="ISparseMatrix{T}"/></param>
		/// <param name="format">The desired <see cref="SparseVectorFormat"/> of the target sparse vector, can be anatomic</param>
		/// <param name="createFunc">See <see cref="DelegateCreateVectorNew{T}"/></param>
		/// <param name="target">Output the created new <see cref="ISparseVector{T}"/> with format fitting <paramref name="format"/> and desired properties (the length is the product of <see cref="ISparseMatrix{T}.NRows"/> and <see cref="ISparseMatrix{T}.NCols"/>)</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="matrix"/> is null or invalid</exception>
		protected abstract bool SparseMatrixToVector_<T>(ISparseMatrix<T> matrix, SparseVectorFormat format, out ISparseVector<T> target, DelegateCreateVectorNew<T>? createFunc = null) where T : unmanaged;
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
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="threshold"/> is less than 0 or <paramref name="format"/> is not atomic</exception>
		protected abstract bool MatrixDenseToSparse_<T>(long m, long n, Storage<T> source, long ld, SparseMatrixFormat format, out ISparseMatrix<T> target, float threshold = 0, DelegateCreateMatrixNew<T>? createFunc = null) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, prune the given sparse matrix <paramref name="source"/> to a new one by filtering the values less than or equals to <paramref name="threshold"/>.
		/// </summary>
		/// <param name="source">The source sparse matrix to convert from</param>
		/// <param name="threshold">Any element in <paramref name="source"/> less than or equals to this value will be regarded as 0</param>
		/// <param name="target">Output a created new <see cref="ISparseMatrix{T}"/> of same properties as <paramref name="source"/> while the values (and the index arrays accordingly) are pruned by <paramref name="threshold"/>; or <paramref name="source"/> it self if no conversion is necessary</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="threshold"/> is less than 0</exception>
		protected abstract bool MatrixSparsePrune_<T>(ISparseMatrix<T> source, float threshold, out ISparseMatrix<T> target) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, convert the format of the given sparse matrix <paramref name="source"/> to a new one which fits <paramref name="format"/>.
		/// </summary>
		/// <param name="source">The source sparse matrix to convert from</param>
		/// <param name="format">The target <see cref="SparseMatrixFormat"/>, can be anatomic</param>
		/// <param name="target">Output a created new <see cref="ISparseMatrix{T}"/> of desired <paramref name="format"/> while representing the same matrix as <paramref name="source"/>; or <paramref name="source"/> it self if no conversion is necessary</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> is null or invalid</exception>
		protected abstract bool MatrixSparseFormatConvert_<T>(ISparseMatrix<T> source, SparseMatrixFormat format, out ISparseMatrix<T> target) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, fill the given sparse matrix <paramref name="M"/> with identity matrix.
		/// </summary>
		/// <param name="M">The sparse matrix to be filled with identity</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="M"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="M"/> is not a square matrix or its sparsity cannot be filled to be an identity matrix</exception>
		protected abstract bool MatrixSparseFillIdentity_<T>(ISparseMatrix<T> M) where T : unmanaged;
		#endregion
		#endregion
	}
}