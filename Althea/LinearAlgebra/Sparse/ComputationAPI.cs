using System;
using System.Dynamic;
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
		/// <returns>The result sparse vector of the sum of <paramref name="x"/> and <paramref name="y"/></returns>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		public static SparseArrayWrapper<T> VectorSparseAddSparse<T>(ISparseVector<T> x, ISparseVector<T> y, SparseVectorFormat format = FormatExtension.VectorAny) where T : unmanaged
		{
			CombinationOfLocations location1 = x.Storage.LocationDescription, location2 = y.Storage.LocationDescription;
			SparseArrayWrapper<T> result = default;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedVectorBinary(location1, location2) && a.IsSupportedSparseVector(x) && a.IsSupportedSparseVector(y), node);
				success = node.Value.VectorSparseAddSparse_(x, y, out result, format);
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
		/// <returns>A new sparse matrix as the outer product with format fitting <paramref name="format"/></returns>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		public static SparseArrayWrapper<T> VectorSparseOuter<T>(bool conjY, ISparseVector<T> x, ISparseVector<T> y, SparseMatrixFormat format = FormatExtension.MatrixAny) where T : unmanaged
		{
			CombinationOfLocations vector1 = x.Storage.LocationDescription, vector2 = y.Storage.LocationDescription;
			SparseArrayWrapper<T> result = default;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedVectorBinary(vector1, vector2) && a.IsSupportedSparseVector(x) && a.IsSupportedSparseVector(y), node);
				success = node.Value.VectorSparseOuter_(conjY, x, y, out result, format);
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
		/// <returns>A new sparse matrix as the summation with format fitting <paramref name="format"/></returns>
		/// <remarks>If <paramref name="A"/> is null or <paramref name="α"/> is 0, the simple matrix operation <paramref name="opB"/> will be applied to <paramref name="B"/> and the returned sparse matrix may overlap with <paramref name="B"/>. The same for <paramref name="A"/>. However, they cannot be both null or 0.</remarks>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentException">If both <paramref name="A"/> and <paramref name="B"/> are null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If both <paramref name="α"/> and <paramref name="β"/> are 0</exception>
		public static SparseArrayWrapper<T> MatrixSparseAddSparse<T>(MatrixOperation opA, MatrixOperation opB, T α, ISparseMatrix<T>? A, T β, ISparseMatrix<T>? B, SparseMatrixFormat format = FormatExtension.MatrixAny) where T : unmanaged, IEquatable<T>
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
			SparseArrayWrapper<T> result = default;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, Local_Supported, node);
				success = node.Value.MatrixSparseAddSparse_(opA, opB, α, A, β, B, out result, format);
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
		/// <returns>A new sparse matrix as the product (and sum) with format fitting <paramref name="format"/></returns>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="A"/> or <paramref name="B"/> or <paramref name="C"/> is null or invalid</exception>
		public static SparseArrayWrapper<T> MatrixSparseMultiplySparse<T>(MatrixOperation opA, MatrixOperation opB, T α, ISparseMatrix<T> A, ISparseMatrix<T> B, T β, ISparseMatrix<T>? C, SparseMatrixFormat format = FormatExtension.MatrixAny) where T : unmanaged, IEquatable<T>
		{
			CombinationOfLocations matrix1 = A.Storage.LocationDescription, matrix2 = B.Storage.LocationDescription;
			CombinationOfLocations? matrix3 = C?.Storage.LocationDescription;
			bool Local_Supported(AbstractApi api)
			{
				bool supportMatrix = api.IsSupportedSparseMatrix(A) && api.IsSupportedSparseMatrix(B) && (C is null || api.IsSupportedSparseMatrix(C));
				return supportMatrix && (matrix3.HasValue ? api.IsSupportedMatrixTrinary(matrix1, matrix2, matrix3.Value) : api.IsSupportedMatrixBinary(matrix1, matrix2));
			}
			SparseArrayWrapper<T> result = default;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, Local_Supported, node);
				success = node.Value.MatrixSparseAddSparse_(opA, opB, α, A, β, B, out result, format);
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
		/// <returns>A new sparse matrix as the Kronecker product with format fitting <paramref name="format"/></returns>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="A"/> or <paramref name="B"/> is null or invalid</exception>
		public static SparseArrayWrapper<T> MatrixSparseKronecker<T>(ISparseMatrix<T> A, ISparseMatrix<T> B, SparseMatrixFormat format = FormatExtension.MatrixAny) where T : unmanaged
		{
			CombinationOfLocations matrix1 = A.Storage.LocationDescription, matrix2 = B.Storage.LocationDescription;
			SparseArrayWrapper<T> result = default;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedMatrixBinary(matrix1, matrix2) && a.IsSupportedSparseMatrix(A) && a.IsSupportedSparseMatrix(B), node);
				success = node.Value.MatrixSparseKronecker_(A, B, out result, format);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
			return result;
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
		/// <param name="target">Output the result sparse vector of the sum of <paramref name="x"/> and <paramref name="y"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		protected abstract bool VectorSparseAddSparse_<T>(ISparseVector<T> x, ISparseVector<T> y, out SparseArrayWrapper<T> target, SparseVectorFormat format = FormatExtension.VectorAny) where T : unmanaged;

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
		/// <param name="target">Output a new sparse matrix as the outer product with format fitting <paramref name="format"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		protected abstract bool VectorSparseOuter_<T>(bool conjY, ISparseVector<T> x, ISparseVector<T> y, out SparseArrayWrapper<T> target, SparseMatrixFormat format = FormatExtension.MatrixAny) where T : unmanaged;
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
		/// <param name="target">Output a new sparse matrix as the summation with format fitting <paramref name="format"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <remarks>If <paramref name="A"/> is null or <paramref name="α"/> is 0, the simple matrix operation <paramref name="opB"/> will be applied to <paramref name="B"/> and the returned sparse matrix may overlap with <paramref name="B"/>. The same for <paramref name="A"/>. However, they cannot be both null or 0.</remarks>
		/// <exception cref="ArgumentNullException">If both <paramref name="A"/> and <paramref name="B"/> are null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If both <paramref name="α"/> and <paramref name="β"/> are 0</exception>
		protected abstract bool MatrixSparseAddSparse_<T>(MatrixOperation opA, MatrixOperation opB, T α, ISparseMatrix<T>? A, T β, ISparseMatrix<T>? B, out SparseArrayWrapper<T> target, SparseMatrixFormat format = FormatExtension.MatrixAny) where T : unmanaged, IEquatable<T>;

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
		/// <param name="target">Output a new sparse matrix as the product (and sum) with format fitting <paramref name="format"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="B"/> or <paramref name="C"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If both <paramref name="α"/> and <paramref name="β"/> are 0</exception>
		protected abstract bool MatrixSparseMultiplySparse_<T>(MatrixOperation opA, MatrixOperation opB, T α, ISparseMatrix<T> A, ISparseMatrix<T> B, T β, ISparseMatrix<T>? C, out SparseArrayWrapper<T> target, SparseMatrixFormat format = FormatExtension.MatrixAny) where T : unmanaged, IEquatable<T>;

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
		/// <param name="target">Output a new sparse matrix as the Kronecker product with format fitting <paramref name="format"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="B"/> is null or invalid</exception>
		protected abstract bool MatrixSparseKronecker_<T>(ISparseMatrix<T> A, ISparseMatrix<T> B, out SparseArrayWrapper<T> target, SparseMatrixFormat format = FormatExtension.MatrixAny) where T : unmanaged;
		#endregion
		#endregion
	}
}