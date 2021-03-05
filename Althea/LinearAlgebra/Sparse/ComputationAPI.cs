using System;
using System.Collections.Generic;

using Althea.Arrays;
using Althea.Helpers;


namespace Althea.LinearAlgebra.Sparse
{
	/// <summary>
	/// The abstract class for runtime sparse linear algebra API routines 
	/// </summary>
	public abstract partial class AbstractApi : AbstractRuntimeApi
	{
		#region support information
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
		/// Add the sparse vector <paramref name="x"/> scaled by scalar <paramref name="α"/> to a dense vector <paramref name="y"/>: <c><paramref name="y"/> += <paramref name="α"/> * <paramref name="x"/></c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="α">The scalar to multiply <paramref name="x"/></param>
		/// <param name="x">The input sparse vector as a <see cref="ISparseVector{T}"/></param>
		/// <param name="y">The input/output dense vector as a <see cref="Storage{T}"/></param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		/// <exception cref="TypeMismatchException">If the <see cref="ISparseArray{T}.IndexType"/> is not an integral type</exception>
		public static void VectorSparseAddToDense<T>(T α, ISparseVector<T> x, Storage<T> y) where T : unmanaged, IEquatable<T>
		{
			CombinationOfLocations location1 = x.Storage.LocationDescription, location2 = y.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedVectorBinary(location1, location2) && a.IsSupportedSparseVector(x), node);
				success = node.Value.VectorSparseAddToDense_(α, x, y);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// Calculate the dot (inner) product of a sparse vector <paramref name="x"/> and a dense vector <paramref name="y"/>: result = <c><paramref name="x"/>^op <paramref name="y"/></c>, op = <paramref name="conjX"/> ? H : T.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="conjX">Whether to conjugate <paramref name="x"/> or not</param>
		/// <param name="x">The input sparse vector as a <see cref="ISparseVector{T}"/></param>
		/// <param name="y">The input dense vector as a <see cref="Storage{T}"/></param>
		/// <returns>The dot product result as a <typeparamref name="T"/></returns>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		public static T VectorSparseDotDense<T>(bool conjX, ISparseVector<T> x, Storage<T> y) where T : unmanaged
		{
			CombinationOfLocations location1 = x.Storage.LocationDescription, location2 = y.LocationDescription;
			T result = default;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedVectorBinary(location1, location2) && a.IsSupportedSparseVector(x), node);
				success = node.Value.VectorSparseDotDense_(conjX, x, y, out result);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
			return result;
		}

		/// <summary>
		/// Calculate the dot (inner) product of two sparse vectors <paramref name="x"/> and <paramref name="y"/>: result = <c><paramref name="x"/>^op <paramref name="y"/></c>, op = <paramref name="conjX"/> ? H : T.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="conjX">Whether to conjugate <paramref name="x"/> or not</param>
		/// <param name="x">The input sparse vector as a <see cref="ISparseVector{T}"/></param>
		/// <param name="y">The other input sparse vector as a <see cref="ISparseVector{T}"/></param>
		/// <returns>The dot product result as a <typeparamref name="T"/></returns>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		public static T VectorSparseDotSparse<T>(bool conjX, ISparseVector<T> x, ISparseVector<T> y) where T : unmanaged
		{
			CombinationOfLocations location1 = x.Storage.LocationDescription, location2 = y.Storage.LocationDescription;
			T result = default;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedVectorBinary(location1, location2) && a.IsSupportedSparseVector(x) && a.IsSupportedSparseVector(y), node);
				success = node.Value.VectorSparseDotSparse_(conjX, x, y, out result);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
			return result;
		}

		/// <summary>
		/// Add the sparse vector <paramref name="x"/> to another sparse vector <paramref name="y"/> and put the result in a new sparse vector.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The input sparse vector x</param>
		/// <param name="y">The input sparse vector y</param>
		/// <param name="format">The desired output sparse vector's <see cref="SparseVectorFormat"/>, can be anatomic</param>
		/// <param name="createFunc">See <see cref="DelegateCreateVectorNew{T}"/></param>
		/// <returns>The result sparse vector of the sum of <paramref name="x"/> and <paramref name="y"/></returns>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		public static ISparseVector<T> VectorSparseAddSparse<T>(ISparseVector<T> x, ISparseVector<T> y, SparseVectorFormat format = FormatExtension.VectorAny, DelegateCreateVectorNew<T>? createFunc = null) where T : unmanaged
		{
			CombinationOfLocations location1 = x.Storage.LocationDescription, location2 = y.Storage.LocationDescription;
			ISparseVector<T> result = GetEmptySparseVector<T>();
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedVectorBinary(location1, location2) && a.IsSupportedSparseVector(x) && a.IsSupportedSparseVector(y), node);
				success = node.Value.VectorSparseAddSparse_(x, y, out result, format, createFunc);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
			return result;
		}

		/// <summary>
		/// Point-wise multiply a sparse vector by a dense vector: <c><paramref name="x"/> *= <paramref name="y"/></c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The input/output sparse vector x</param>
		/// <param name="y">The input dense vector y</param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		public static void VectorSparsePointWiseMultiplyDense<T>(ISparseVector<T> x, Storage<T> y) where T : unmanaged
		{
			CombinationOfLocations location1 = x.Storage.LocationDescription, location2 = y.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedVectorBinary(location1, location2) && a.IsSupportedSparseVector(x), node);
				success = node.Value.VectorSparsePointWiseMultiplyDense_(x, y);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// Point-wise divide a sparse vector by a dense vector: <c><paramref name="x"/> /= <paramref name="y"/></c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The input/output sparse vector x</param>
		/// <param name="y">The input dense vector y</param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		public static void VectorSparsePointWiseDivideDense<T>(ISparseVector<T> x, Storage<T> y) where T : unmanaged
		{
			CombinationOfLocations location1 = x.Storage.LocationDescription, location2 = y.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedVectorBinary(location1, location2) && a.IsSupportedSparseVector(x), node);
				success = node.Value.VectorSparsePointWiseDivideDense_(x, y);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}
		#endregion

		#region vector and matrix
		/// <summary>
		/// Compute the sparse matrix dense vector multiplication: <c><paramref name="y"/> = <paramref name="α"/> * <paramref name="op"/>(<paramref name="M"/>) * <paramref name="x"/> + <paramref name="β"/> * <paramref name="y"/></c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="op">The <see cref="MatrixOperation"/> to indicate the simple operation to <paramref name="M"/></param>
		/// <param name="M">The input sparse matrix M</param>
		/// <param name="x">The input dense vector x</param>
		/// <param name="y">The input/output dense vector y</param>
		/// <param name="α">The scalar to multiply <paramref name="M"/></param>
		/// <param name="β">The scalar to multiply <paramref name="y"/></param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="x"/> or <paramref name="y"/> or <paramref name="M"/> is null or invalid</exception>
		public static void MatrixSparseMultiplyVectorDense<T>(MatrixOperation op, T α, ISparseMatrix<T> M, Storage<T> x, T β, Storage<T> y) where T : unmanaged, IEquatable<T>
		{
			CombinationOfLocations vector1 = x.LocationDescription, vector2 = y.LocationDescription, matrix = M.Storage.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedVectorBinaryMatrixUnary(vector1, vector2, matrix) && a.IsSupportedSparseMatrix(M), node);
				success = node.Value.MatrixSparseMultiplyVectorDense_(op, α, M, x, β, y);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// Compute the dense matrix sparse vector multiplication: <c><paramref name="y"/> = <paramref name="α"/> * <paramref name="op"/>(<paramref name="M"/>) * <paramref name="x"/> + <paramref name="β"/> * <paramref name="y"/></c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="op">The <see cref="MatrixOperation"/> to indicate the simple operation to <paramref name="M"/></param>
		/// <param name="α">The scalar to multiply <paramref name="M"/></param>
		/// <param name="m">The number of rows of <paramref name="op"/>(<paramref name="M"/>) (the number of columns is implied in <paramref name="x"/>)</param>
		/// <param name="M">The input dense matrix M</param>
		/// <param name="ldm">The leading dimension of <paramref name="M"/></param>
		/// <param name="x">The input sparse vector x</param>
		/// <param name="y">The input/output dense vector y</param>
		/// <param name="β">The scalar to multiply <paramref name="y"/></param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="x"/> or <paramref name="y"/> or <paramref name="M"/> is null or invalid</exception>
		public static void MatrixDenseMultiplyVectorSparse<T>(MatrixOperation op, T α, long m, Storage<T> M, long ldm, ISparseVector<T> x, T β, Storage<T> y) where T : unmanaged, IEquatable<T>
		{
			CombinationOfLocations vector1 = x.Storage.LocationDescription, vector2 = y.LocationDescription, matrix = M.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedVectorBinaryMatrixUnary(vector1, vector2, matrix) && a.IsSupportedSparseVector(x), node);
				success = node.Value.MatrixDenseMultiplyVectorSparse_(op, α, m, M, ldm, x, β, y);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// Compute sparse vector outer product: <c><paramref name="x"/> * <paramref name="y"/>^op</c>, <c>op = <paramref name="conjY"/> ? H : T</c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="conjY">Whether to conjugate <paramref name="y"/> or not</param>
		/// <param name="x">The input sparse vector x</param>
		/// <param name="y">The input sparse vector y</param>
		/// <param name="format">The desired output sparse matrix's format, can be anatomic</param>
		/// <param name="createFunc">See <see cref="DelegateCreateMatrixNew{T}"/></param>
		/// <returns>A new sparse matrix as the outer product with format fitting <paramref name="format"/></returns>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		public static ISparseMatrix<T> VectorSparseOuter<T>(bool conjY, ISparseVector<T> x, ISparseVector<T> y, SparseMatrixFormat format = FormatExtension.MatrixAny, DelegateCreateMatrixNew<T>? createFunc = null) where T : unmanaged
		{
			CombinationOfLocations vector1 = x.Storage.LocationDescription, vector2 = y.Storage.LocationDescription;
			ISparseMatrix<T> result = GetEmptySparseMatrix<T>();
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedVectorBinary(vector1, vector2) && a.IsSupportedSparseVector(x) && a.IsSupportedSparseVector(y), node);
				success = node.Value.VectorSparseOuter_(conjY, x, y, out result, format, createFunc);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
			return result;
		}
		#endregion

		#region matrix
		/// <summary>
		/// When implemented by a derived class, perform the dense matrix and sparse matrix addition: <c><paramref name="C"/> = <paramref name="α"/> * <paramref name="opA"/>(<paramref name="A"/>) + <paramref name="β"/> * <paramref name="opB"/>(<paramref name="B"/>)</c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="opA">The simple operation to matrix <paramref name="A"/> as a <see cref="MatrixOperation"/></param>
		/// <param name="opB">The simple operation to matrix <paramref name="B"/> as a <see cref="MatrixOperation"/></param>
		/// <param name="α">The scalar to multiply to <paramref name="A"/></param>
		/// <param name="A">The input dense matrix</param>
		/// <param name="lda">The leading dimension of <paramref name="A"/></param>
		/// <param name="β">The scalar to multiply to <paramref name="B"/></param>
		/// <param name="B">The input sparse matrix</param>
		/// <param name="C">The input/output dense matrix</param>
		/// <param name="ldc">The leading dimension of <paramref name="C"/></param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="B"/> or <paramref name="C"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If both <paramref name="α"/> and <paramref name="β"/> are 0</exception>
		public static void MatrixDenseAddSparse<T>(MatrixOperation opA, MatrixOperation opB, T α, Storage<T> A, long lda, T β, ISparseMatrix<T> B, Storage<T> C, long ldc) where T : unmanaged, IEquatable<T>
		{
			CombinationOfLocations matrix1 = A.LocationDescription, matrix2 = B.Storage.LocationDescription, matrix3 = C.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedMatrixTrinary(matrix1, matrix2, matrix3) && a.IsSupportedSparseMatrix(B), node);
				success = node.Value.MatrixDenseAddSparse_(opA, opB, α, A, lda, β, B, C, ldc);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// Perform the sparse matrices addition: <c><paramref name="α"/> * <paramref name="opA"/>(<paramref name="A"/>) + <paramref name="β"/> * <paramref name="opB"/>(<paramref name="B"/>)</c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="opA">The simple operation to matrix <paramref name="A"/> as a <see cref="MatrixOperation"/></param>
		/// <param name="opB">The simple operation to matrix <paramref name="B"/> as a <see cref="MatrixOperation"/></param>
		/// <param name="α">The scalar to multiply to <paramref name="A"/></param>
		/// <param name="A">The first input sparse matrix</param>
		/// <param name="β">The scalar to multiply to <paramref name="B"/></param>
		/// <param name="B">The second input sparse matrix</param>
		/// <param name="format">The desired output sparse matrix's format, can be anatomic</param>
		/// <param name="createFunc">See <see cref="DelegateCreateMatrixNew{T}"/></param>
		/// <returns>A new sparse matrix as the summation with format fitting <paramref name="format"/></returns>
		/// <remarks>If <paramref name="A"/> is null or <paramref name="α"/> is 0, the simple matrix operation <paramref name="opB"/> will be applied to <paramref name="B"/> and the returned sparse matrix may overlap with <paramref name="B"/>. The same for <paramref name="A"/>. However, they cannot be both null or 0.</remarks>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentException">If both <paramref name="A"/> and <paramref name="B"/> are null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If both <paramref name="α"/> and <paramref name="β"/> are 0</exception>
		public static ISparseMatrix<T> MatrixSparseAddSparse<T>(MatrixOperation opA, MatrixOperation opB, T α, ISparseMatrix<T>? A, T β, ISparseMatrix<T>? B, SparseMatrixFormat format = FormatExtension.MatrixAny, DelegateCreateMatrixNew<T>? createFunc = null) where T : unmanaged, IEquatable<T>
		{
			if ((A is null || !A.IsValid()) && (B is null || !B.IsValid()))
				throw new ArgumentException(Resources.Parameter.CannotAllNull);

			CombinationOfLocations? matrix1 = A?.Storage.LocationDescription, matrix2 = B?.Storage.LocationDescription;
			bool Local_Supported(AbstractApi api)
			{
				bool supportMatrix = (A is null || api.IsSupportedSparseMatrix(A)) && (B is null || api.IsSupportedSparseMatrix(B));
				if (!supportMatrix)
					return false;
				if (matrix1 is null || matrix2 is null)
					return api.IsSupportedMatrixUnary(matrix1 ?? matrix2 ?? default);
				else
					return api.IsSupportedMatrixBinary(matrix1.Value, matrix2.Value);
			}
			ISparseMatrix<T> result = GetEmptySparseMatrix<T>();
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, Local_Supported, node);
				success = node.Value.MatrixSparseAddSparse_(opA, opB, α, A, β, B, out result, format, createFunc);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
			return result;
		}

		/// <summary>
		/// Perform the sparse matrices multiplication: <c><paramref name="α"/> * <paramref name="opA"/>(<paramref name="A"/>) * <paramref name="opB"/>(<paramref name="B"/>) + <paramref name="β"/> * <paramref name="C"/></c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="opA">The simple operation to matrix <paramref name="A"/> as a <see cref="MatrixOperation"/></param>
		/// <param name="opB">The simple operation to matrix <paramref name="B"/> as a <see cref="MatrixOperation"/></param>
		/// <param name="α">The scalar to multiply to <paramref name="A"/></param>
		/// <param name="A">The first input sparse matrix</param>
		/// <param name="B">The second input sparse matrix</param>
		/// <param name="β">The scalar to multiply to <paramref name="C"/></param>
		/// <param name="C">The third input sparse matrix, can be null. If this is null or <paramref name="β"/> is 0, no addition will be performed</param>
		/// <param name="format">The desired output sparse matrix's format, can be anatomic</param>
		/// <param name="createFunc">See <see cref="DelegateCreateMatrixNew{T}"/></param>
		/// <returns>A new sparse matrix as the product (and sum) with format fitting <paramref name="format"/></returns>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="A"/> or <paramref name="B"/> or <paramref name="C"/> is null or invalid</exception>
		public static ISparseMatrix<T> MatrixSparseMultiplySparse<T>(MatrixOperation opA, MatrixOperation opB, T α, ISparseMatrix<T> A, ISparseMatrix<T> B, T β, ISparseMatrix<T>? C, SparseMatrixFormat format = FormatExtension.MatrixAny, DelegateCreateMatrixNew<T>? createFunc = null) where T : unmanaged, IEquatable<T>
		{
			CombinationOfLocations matrix1 = A.Storage.LocationDescription, matrix2 = B.Storage.LocationDescription;
			CombinationOfLocations? matrix3 = C?.Storage.LocationDescription;
			bool Local_Supported(AbstractApi api)
			{
				bool supportMatrix = api.IsSupportedSparseMatrix(A) && api.IsSupportedSparseMatrix(B) && (C is null || api.IsSupportedSparseMatrix(C));
				return supportMatrix && (matrix3.HasValue ? api.IsSupportedMatrixTrinary(matrix1, matrix2, matrix3.Value) : api.IsSupportedMatrixBinary(matrix1, matrix2));
			}
			ISparseMatrix<T> result = GetEmptySparseMatrix<T>();
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, Local_Supported, node);
				success = node.Value.MatrixSparseAddSparse_(opA, opB, α, A, β, B, out result, format, createFunc);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
			return result;
		}

		/// <summary>
		/// Perform the dense matrix and sparse matrix multiplication: <c><paramref name="C"/> = <paramref name="α"/> * <paramref name="opA"/>(<paramref name="A"/>) * <paramref name="opB"/>(<paramref name="B"/>) + <paramref name="β"/> * <paramref name="C"/></c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="m">The number of rows of <paramref name="opA"/>(<paramref name="A"/>). (The number of columns is implied from <paramref name="opB"/> and <paramref name="B"/>)</param>
		/// <param name="opA">The simple operation to matrix <paramref name="A"/> as a <see cref="MatrixOperation"/></param>
		/// <param name="opB">The simple operation to matrix <paramref name="B"/> as a <see cref="MatrixOperation"/></param>
		/// <param name="α">The scalar to multiply to <paramref name="A"/></param>
		/// <param name="A">The input dense matrix</param>
		/// <param name="B">The input sparse matrix</param>
		/// <param name="lda">The leading dimension of <paramref name="A"/></param>
		/// <param name="β">The scalar to multiply to <paramref name="C"/></param>
		/// <param name="C">The input/output dense matrix</param>
		/// <param name="ldc">The leading dimension of <paramref name="C"/></param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="A"/> or <paramref name="B"/> or <paramref name="C"/> is null or invalid</exception>
		public static void MatrixDenseMultiplySparse<T>(MatrixOperation opA, MatrixOperation opB, long m, T α, Storage<T> A, long lda, ISparseMatrix<T> B, T β, Storage<T> C, long ldc) where T : unmanaged, IEquatable<T>
		{
			CombinationOfLocations matrix1 = A.LocationDescription, matrix2 = B.Storage.LocationDescription, matrix3 = C.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedMatrixTrinary(matrix1, matrix2, matrix3) && a.IsSupportedSparseMatrix(B), node);
				success = node.Value.MatrixDenseMultiplySparse_(opA, opB, m, α, A, lda, B, β, C, ldc);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// Perform the dense matrix and sparse matrix multiplication: <c><paramref name="C"/> = <paramref name="α"/> * <paramref name="opA"/>(<paramref name="A"/>) * <paramref name="opB"/>(<paramref name="B"/>) + <paramref name="β"/> * <paramref name="C"/></c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="n">The number of columns of <paramref name="opB"/>(<paramref name="B"/>). (The number of rows is implied from <paramref name="opA"/> and <paramref name="A"/>)</param>
		/// <param name="opA">The simple operation to matrix <paramref name="A"/> as a <see cref="MatrixOperation"/></param>
		/// <param name="opB">The simple operation to matrix <paramref name="B"/> as a <see cref="MatrixOperation"/></param>
		/// <param name="α">The scalar to multiply to <paramref name="A"/></param>
		/// <param name="A">The input sparse matrix</param>
		/// <param name="B">The input dense matrix</param>
		/// <param name="ldb">The leading dimension of <paramref name="B"/></param>
		/// <param name="β">The scalar to multiply to <paramref name="C"/></param>
		/// <param name="C">The input/output dense matrix</param>
		/// <param name="ldc">The leading dimension of <paramref name="C"/></param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="A"/> or <paramref name="B"/> or <paramref name="C"/> is null or invalid</exception>
		public static void MatrixSparseMultiplyDense<T>(MatrixOperation opA, MatrixOperation opB, long n, T α, ISparseMatrix<T> A, Storage<T> B, long ldb, T β, Storage<T> C, long ldc) where T : unmanaged, IEquatable<T>
		{
			CombinationOfLocations matrix1 = A.Storage.LocationDescription, matrix2 = B.LocationDescription, matrix3 = C.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedMatrixTrinary(matrix1, matrix2, matrix3) && a.IsSupportedSparseMatrix(A), node);
				success = node.Value.MatrixSparseMultiplyDense_(opA, opB, n, α, A, B, ldb, β, C, ldc);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// Perform the sparse matrices Kronecker product: <c><paramref name="A"/> ⨂ <paramref name="B"/></c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="A">The first input sparse matrix</param>
		/// <param name="B">The second input sparse matrix</param>
		/// <param name="format">The desired output sparse matrix's format, can be anatomic</param>
		/// <param name="createFunc">See <see cref="DelegateCreateMatrixNew{T}"/></param>
		/// <returns>A new sparse matrix as the Kronecker product with format fitting <paramref name="format"/></returns>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="A"/> or <paramref name="B"/> is null or invalid</exception>
		public static ISparseMatrix<T> MatrixSparseKronecker<T>(ISparseMatrix<T> A, ISparseMatrix<T> B, SparseMatrixFormat format = FormatExtension.MatrixAny, DelegateCreateMatrixNew<T>? createFunc = null) where T : unmanaged
		{
			CombinationOfLocations matrix1 = A.Storage.LocationDescription, matrix2 = B.Storage.LocationDescription;
			ISparseMatrix<T> result = GetEmptySparseMatrix<T>();
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedMatrixBinary(matrix1, matrix2) && a.IsSupportedSparseMatrix(A) && a.IsSupportedSparseMatrix(B), node);
				success = node.Value.MatrixSparseKronecker_(A, B, out result, format, createFunc);
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
		/// <exception cref="NullReferenceException">If <paramref name="array"/> is null or invalid</exception>
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
		/// <exception cref="NullReferenceException">If <paramref name="array"/> is null or invalid</exception>
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
		/// <exception cref="NullReferenceException">If <paramref name="array"/> is null or invalid</exception>
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
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="array"/> is null or invalid</exception>
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
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="array"/> or <paramref name="target"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="target"/>'s length is too short or <paramref name="end"/> is less than <paramref name="start"/></exception>
		/// <exception cref="TypeMismatchException">If <typeparamref name="TInd"/> or <typeparamref name="TIndOut"/> is not an integral type</exception>
		public static void IndexGetAllBounds<TInd, TIndOut>(Storage<TInd> array, Storage<TIndOut> target, TInd start, TInd end, bool lowerBound)
			where TInd : unmanaged, IEquatable<TInd>
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
		#endregion
		#endregion


		#region abstract methods that actually do computations
		#region vector
		/// <summary>
		/// When implemented by a derived class, add the sparse vector <paramref name="x"/> scaled by scalar <paramref name="α"/> to a dense vector <paramref name="y"/>: <c><paramref name="y"/> += <paramref name="α"/> * <paramref name="x"/></c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="α">The scalar to multiply <paramref name="x"/></param>
		/// <param name="x">The input sparse vector as a <see cref="ISparseVector{T}"/></param>
		/// <param name="y">The input/output dense vector as a <see cref="Storage{T}"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		/// <exception cref="TypeMismatchException">If the <see cref="ISparseArray{T}.IndexType"/> is not an integral type</exception>
		protected abstract bool VectorSparseAddToDense_<T>(T α, ISparseVector<T> x, Storage<T> y) where T : unmanaged, IEquatable<T>;

		/// <summary>
		/// When implemented by a derived class, calculate the dot (inner) product of a sparse vector <paramref name="x"/> and a dense vector <paramref name="y"/>: result = <c><paramref name="x"/>^op <paramref name="y"/></c>, op = <paramref name="conjX"/> ? H : T.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="conjX">Whether to conjugate <paramref name="x"/> or not</param>
		/// <param name="x">The input sparse vector as a <see cref="ISparseVector{T}"/></param>
		/// <param name="y">The input dense vector as a <see cref="Storage{T}"/></param>
		/// <param name="dot">Output the dot product result as a <typeparamref name="T"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		protected abstract bool VectorSparseDotDense_<T>(bool conjX, ISparseVector<T> x, Storage<T> y, out T dot) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, calculate the dot (inner) product of two sparse vectors <paramref name="x"/> and <paramref name="y"/>: result = <c><paramref name="x"/>^op <paramref name="y"/></c>, op = <paramref name="conjX"/> ? H : T.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="conjX">Whether to conjugate <paramref name="x"/> or not</param>
		/// <param name="x">The input sparse vector as a <see cref="ISparseVector{T}"/></param>
		/// <param name="y">The other input sparse vector as a <see cref="ISparseVector{T}"/></param>
		/// <param name="dot">Output the dot product result as a <typeparamref name="T"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		protected abstract bool VectorSparseDotSparse_<T>(bool conjX, ISparseVector<T> x, ISparseVector<T> y, out T dot) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, add the sparse vector <paramref name="x"/> to another sparse vector <paramref name="y"/> and put the result in a new sparse vector.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The input sparse vector x</param>
		/// <param name="y">The input sparse vector y</param>
		/// <param name="format">The desired output sparse vector's <see cref="SparseVectorFormat"/>, can be anatomic</param>
		/// <param name="createFunc">See <see cref="DelegateCreateVectorNew{T}"/></param>
		/// <param name="target">Output the result sparse vector of the sum of <paramref name="x"/> and <paramref name="y"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		protected abstract bool VectorSparseAddSparse_<T>(ISparseVector<T> x, ISparseVector<T> y, out ISparseVector<T> target, SparseVectorFormat format = FormatExtension.VectorAny, DelegateCreateVectorNew<T>? createFunc = null) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, point-wise multiply a sparse vector by a dense vector: <c><paramref name="x"/> *= <paramref name="y"/></c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The input/output sparse vector x</param>
		/// <param name="y">The input dense vector y</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		protected abstract bool VectorSparsePointWiseMultiplyDense_<T>(ISparseVector<T> x, Storage<T> y) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, point-wise divide a sparse vector by a dense vector: <c><paramref name="x"/> /= <paramref name="y"/></c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The input/output sparse vector x</param>
		/// <param name="y">The input dense vector y</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		protected abstract bool VectorSparsePointWiseDivideDense_<T>(ISparseVector<T> x, Storage<T> y) where T : unmanaged;
		#endregion

		#region vector and matrix
		/// <summary>
		/// When implemented by a derived class, compute the sparse matrix dense vector multiplication: <c><paramref name="y"/> = <paramref name="α"/> * <paramref name="op"/>(<paramref name="M"/>) * <paramref name="x"/> + <paramref name="β"/> * <paramref name="y"/></c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="op">The <see cref="MatrixOperation"/> to indicate the simple operation to <paramref name="M"/></param>
		/// <param name="M">The input sparse matrix M</param>
		/// <param name="x">The input dense vector x</param>
		/// <param name="y">The input/output dense vector y</param>
		/// <param name="α">The scalar to multiply <paramref name="M"/></param>
		/// <param name="β">The scalar to multiply <paramref name="y"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> or <paramref name="M"/> is null or invalid</exception>
		protected abstract bool MatrixSparseMultiplyVectorDense_<T>(MatrixOperation op, T α, ISparseMatrix<T> M, Storage<T> x, T β, Storage<T> y) where T : unmanaged, IEquatable<T>;

		/// <summary>
		/// When implemented by a derived class, compute the dense matrix sparse vector multiplication: <c><paramref name="y"/> = <paramref name="α"/> * <paramref name="op"/>(<paramref name="M"/>) * <paramref name="x"/> + <paramref name="β"/> * <paramref name="y"/></c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="op">The <see cref="MatrixOperation"/> to indicate the simple operation to <paramref name="M"/></param>
		/// <param name="α">The scalar to multiply <paramref name="M"/></param>
		/// <param name="m">The number of rows of <paramref name="op"/>(<paramref name="M"/>) (the number of columns is implied in <paramref name="x"/>)</param>
		/// <param name="M">The input dense matrix M</param>
		/// <param name="ldm">The leading dimension of <paramref name="M"/></param>
		/// <param name="x">The input sparse vector x</param>
		/// <param name="y">The input/output dense vector y</param>
		/// <param name="β">The scalar to multiply <paramref name="y"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> or <paramref name="M"/> is null or invalid</exception>
		protected abstract bool MatrixDenseMultiplyVectorSparse_<T>(MatrixOperation op, T α, long m, Storage<T> M, long ldm, ISparseVector<T> x, T β, Storage<T> y) where T : unmanaged, IEquatable<T>;

		/// <summary>
		/// When implemented by a derived class, compute sparse vector outer product: <c><paramref name="x"/> * <paramref name="y"/>^op</c>, <c>op = <paramref name="conjY"/> ? H : T</c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="conjY">Whether to conjugate <paramref name="y"/> or not</param>
		/// <param name="x">The input sparse vector x</param>
		/// <param name="y">The input sparse vector y</param>
		/// <param name="format">The desired output sparse matrix's format, can be anatomic</param>
		/// <param name="createFunc">See <see cref="DelegateCreateMatrixNew{T}"/></param>
		/// <param name="target">Output a new sparse matrix as the outer product with format fitting <paramref name="format"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		protected abstract bool VectorSparseOuter_<T>(bool conjY, ISparseVector<T> x, ISparseVector<T> y, out ISparseMatrix<T> target, SparseMatrixFormat format = FormatExtension.MatrixAny, DelegateCreateMatrixNew<T>? createFunc = null) where T : unmanaged;
		#endregion

		#region matrix
		/// <summary>
		/// When implemented by a derived class, perform the dense matrix and sparse matrix addition: <c><paramref name="C"/> = <paramref name="α"/> * <paramref name="opA"/>(<paramref name="A"/>) + <paramref name="β"/> * <paramref name="opB"/>(<paramref name="B"/>)</c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="opA">The simple operation to matrix <paramref name="A"/> as a <see cref="MatrixOperation"/></param>
		/// <param name="opB">The simple operation to matrix <paramref name="B"/> as a <see cref="MatrixOperation"/></param>
		/// <param name="α">The scalar to multiply to <paramref name="A"/></param>
		/// <param name="A">The input dense matrix</param>
		/// <param name="lda">The leading dimension of <paramref name="A"/></param>
		/// <param name="β">The scalar to multiply to <paramref name="B"/></param>
		/// <param name="B">The input sparse matrix</param>
		/// <param name="C">The input/output dense matrix</param>
		/// <param name="ldc">The leading dimension of <paramref name="C"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="B"/> or <paramref name="C"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If both <paramref name="α"/> and <paramref name="β"/> are 0</exception>
		protected abstract bool MatrixDenseAddSparse_<T>(MatrixOperation opA, MatrixOperation opB, T α, Storage<T> A, long lda, T β, ISparseMatrix<T> B, Storage<T> C, long ldc) where T : unmanaged, IEquatable<T>;

		/// <summary>
		/// When implemented by a derived class, perform the sparse matrices addition: <c><paramref name="α"/> * <paramref name="opA"/>(<paramref name="A"/>) + <paramref name="β"/> * <paramref name="opB"/>(<paramref name="B"/>)</c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="opA">The simple operation to matrix <paramref name="A"/> as a <see cref="MatrixOperation"/></param>
		/// <param name="opB">The simple operation to matrix <paramref name="B"/> as a <see cref="MatrixOperation"/></param>
		/// <param name="α">The scalar to multiply to <paramref name="A"/></param>
		/// <param name="A">The first input sparse matrix</param>
		/// <param name="β">The scalar to multiply to <paramref name="B"/></param>
		/// <param name="B">The second input sparse matrix</param>
		/// <param name="format">The desired output sparse matrix's format, can be anatomic</param>
		/// <param name="createFunc">See <see cref="DelegateCreateMatrixNew{T}"/></param>
		/// <param name="target">Output a new sparse matrix as the summation with format fitting <paramref name="format"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <remarks>If <paramref name="A"/> is null or <paramref name="α"/> is 0, the simple matrix operation <paramref name="opB"/> will be applied to <paramref name="B"/> and the returned sparse matrix may overlap with <paramref name="B"/>. The same for <paramref name="A"/>. However, they cannot be both null or 0.</remarks>
		/// <exception cref="ArgumentNullException">If both <paramref name="A"/> and <paramref name="B"/> are null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If both <paramref name="α"/> and <paramref name="β"/> are 0</exception>
		protected abstract bool MatrixSparseAddSparse_<T>(MatrixOperation opA, MatrixOperation opB, T α, ISparseMatrix<T>? A, T β, ISparseMatrix<T>? B, out ISparseMatrix<T> target, SparseMatrixFormat format = FormatExtension.MatrixAny, DelegateCreateMatrixNew<T>? createFunc = null) where T : unmanaged, IEquatable<T>;

		/// <summary>
		/// When implemented by a derived class, perform the sparse matrices multiplication: <c><paramref name="α"/> * <paramref name="opA"/>(<paramref name="A"/>) * <paramref name="opB"/>(<paramref name="B"/>) + <paramref name="β"/> * <paramref name="C"/></c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="opA">The simple operation to matrix <paramref name="A"/> as a <see cref="MatrixOperation"/></param>
		/// <param name="opB">The simple operation to matrix <paramref name="B"/> as a <see cref="MatrixOperation"/></param>
		/// <param name="α">The scalar to multiply to <paramref name="A"/></param>
		/// <param name="A">The first input sparse matrix</param>
		/// <param name="B">The second input sparse matrix</param>
		/// <param name="β">The scalar to multiply to <paramref name="C"/></param>
		/// <param name="C">The third input sparse matrix, can be null. If this is null or <paramref name="β"/> is 0, no addition will be performed</param>
		/// <param name="format">The desired output sparse matrix's format, can be anatomic</param>
		/// <param name="createFunc">See <see cref="DelegateCreateMatrixNew{T}"/></param>
		/// <param name="target">Output a new sparse matrix as the product (and sum) with format fitting <paramref name="format"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="B"/> or <paramref name="C"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If both <paramref name="α"/> and <paramref name="β"/> are 0</exception>
		protected abstract bool MatrixSparseMultiplySparse_<T>(MatrixOperation opA, MatrixOperation opB, T α, ISparseMatrix<T> A, ISparseMatrix<T> B, T β, ISparseMatrix<T>? C, out ISparseMatrix<T> target, SparseMatrixFormat format = FormatExtension.MatrixAny, DelegateCreateMatrixNew<T>? createFunc = null) where T : unmanaged, IEquatable<T>;

		/// <summary>
		/// When implemented by a derived class, perform the dense matrix and sparse matrix multiplication: <c><paramref name="C"/> = <paramref name="α"/> * <paramref name="opA"/>(<paramref name="A"/>) * <paramref name="opB"/>(<paramref name="B"/>) + <paramref name="β"/> * <paramref name="C"/></c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="m">The number of rows of <paramref name="opA"/>(<paramref name="A"/>). (The number of columns is implied from <paramref name="opB"/> and <paramref name="B"/>)</param>
		/// <param name="opA">The simple operation to matrix <paramref name="A"/> as a <see cref="MatrixOperation"/></param>
		/// <param name="opB">The simple operation to matrix <paramref name="B"/> as a <see cref="MatrixOperation"/></param>
		/// <param name="α">The scalar to multiply to <paramref name="A"/></param>
		/// <param name="A">The input dense matrix</param>
		/// <param name="B">The input sparse matrix</param>
		/// <param name="lda">The leading dimension of <paramref name="A"/></param>
		/// <param name="β">The scalar to multiply to <paramref name="C"/></param>
		/// <param name="C">The input/output dense matrix</param>
		/// <param name="ldc">The leading dimension of <paramref name="C"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="B"/> or <paramref name="C"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If both <paramref name="α"/> and <paramref name="β"/> are 0</exception>
		protected abstract bool MatrixDenseMultiplySparse_<T>(MatrixOperation opA, MatrixOperation opB, long m, T α, Storage<T> A, long lda, ISparseMatrix<T> B, T β, Storage<T> C, long ldc) where T : unmanaged, IEquatable<T>;

		/// <summary>
		/// When implemented by a derived class, perform the dense matrix and sparse matrix multiplication: <c><paramref name="C"/> = <paramref name="α"/> * <paramref name="opA"/>(<paramref name="A"/>) * <paramref name="opB"/>(<paramref name="B"/>) + <paramref name="β"/> * <paramref name="C"/></c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="n">The number of columns of <paramref name="opB"/>(<paramref name="B"/>). (The number of rows is implied from <paramref name="opA"/> and <paramref name="A"/>)</param>
		/// <param name="opA">The simple operation to matrix <paramref name="A"/> as a <see cref="MatrixOperation"/></param>
		/// <param name="opB">The simple operation to matrix <paramref name="B"/> as a <see cref="MatrixOperation"/></param>
		/// <param name="α">The scalar to multiply to <paramref name="A"/></param>
		/// <param name="A">The input sparse matrix</param>
		/// <param name="B">The input dense matrix</param>
		/// <param name="ldb">The leading dimension of <paramref name="B"/></param>
		/// <param name="β">The scalar to multiply to <paramref name="C"/></param>
		/// <param name="C">The input/output dense matrix</param>
		/// <param name="ldc">The leading dimension of <paramref name="C"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="B"/> or <paramref name="C"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If both <paramref name="α"/> and <paramref name="β"/> are 0</exception>
		protected abstract bool MatrixSparseMultiplyDense_<T>(MatrixOperation opA, MatrixOperation opB, long n, T α, ISparseMatrix<T> A, Storage<T> B, long ldb, T β, Storage<T> C, long ldc) where T : unmanaged, IEquatable<T>;

		/// <summary>
		/// When implemented by a derived class, perform the sparse matrices Kronecker product: <c><paramref name="A"/> ⨂ <paramref name="B"/></c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="A">The first input sparse matrix</param>
		/// <param name="B">The second input sparse matrix</param>
		/// <param name="format">The desired output sparse matrix's format, can be anatomic</param>
		/// <param name="createFunc">See <see cref="DelegateCreateMatrixNew{T}"/></param>
		/// <param name="target">Output a new sparse matrix as the Kronecker product with format fitting <paramref name="format"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="B"/> is null or invalid</exception>
		protected abstract bool MatrixSparseKronecker_<T>(ISparseMatrix<T> A, ISparseMatrix<T> B, out ISparseMatrix<T> target, SparseMatrixFormat format = FormatExtension.MatrixAny, DelegateCreateMatrixNew<T>? createFunc = null) where T : unmanaged;
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
		/// <exception cref="ArgumentNullException">If <paramref name="array"/> or <paramref name="target"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="target"/>'s length is too short or <paramref name="end"/> is less than <paramref name="start"/></exception>
		/// <exception cref="TypeMismatchException">If <typeparamref name="TInd"/> or <typeparamref name="TIndOut"/> is not an integral type</exception>
		protected abstract bool IndexGetAllBounds_<TInd, TIndOut>(Storage<TInd> array, Storage<TIndOut> target, TInd start, TInd end, bool lowerBound)
			where TInd : unmanaged, IEquatable<TInd>
			where TIndOut : unmanaged;
		#endregion
		#endregion
	}
}