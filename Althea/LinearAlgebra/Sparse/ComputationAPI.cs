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
		#region static methods for dispatching
		/// <summary>
		/// Get the current using <see cref="AbstractApi"/>.
		/// </summary>
		/// <remarks><b>DO NOT</b> invoke methods of this property directly unless you are sure about what you are doing; otherwise, there may be exceptions and / or unnoticeable bugs.</remarks>
		public static AbstractApi? Current => RecentAPIs.First?.Value;

		private static readonly LinkedList<AbstractApi> RecentAPIs = new LinkedList<AbstractApi>();

		internal static bool SetImplementation(Type implementation) => SetImplementation(RecentAPIs, implementation);

		/// <summary>
		/// Special version for <see cref="AbstractApi"/> of method <see cref="AbstractRuntimeApi.DisposeNotCurrent{T}(LinkedList{T})"/>
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static void DisposeNotCurrent() => DisposeNotCurrent(RecentAPIs);

		/// <summary>
		/// Dense and sparse vector version for <see cref="AbstractApi"/> of method <see cref="AbstractRuntimeApi.SelectImplementation{T}(LinkedList{T}, IStorage, Predicate{T})"/>
		/// </summary>
		public static AbstractApi SelectImplementation<T>(Storage<T> dense, ISparseVector<T> sparse) where T : unmanaged
		{
			var selection = SelectImplementation(RecentAPIs, dense, sparse.Storage);
			if (selection.IsSupportedSparseFormat(sparse.Format) &&
				selection.IsSupportedSparseIndexType(sparse.IndexType) &&
				selection.IsSupportedSparseDefaultValue(sparse.DefaultValue))
				return selection;
			// otherwise, use predicate search
			return SelectImplementation(RecentAPIs, dense, sparse.Storage, validApi: a =>
					a.IsSupportedSparseFormat(sparse.Format) &&
					a.IsSupportedSparseIndexType(sparse.IndexType) &&
					a.IsSupportedSparseDefaultValue(sparse.DefaultValue));
		}

		/// <summary>
		/// Sparse and sparse vector version for <see cref="AbstractApi"/> of method <see cref="AbstractRuntimeApi.SelectImplementation{T}(LinkedList{T}, IStorage, Predicate{T})"/>
		/// </summary>
		public static AbstractApi SelectImplementation<T>(ISparseVector<T> sparse1, ISparseVector<T> sparse2) where T : unmanaged
		{
			var selection = SelectImplementation(RecentAPIs, sparse1.Storage, sparse2.Storage);
			if (selection.IsSupportedSparseFormat(sparse1.Format, sparse2.Format) &&
				selection.IsSupportedSparseIndexType(sparse1.IndexType, sparse2.IndexType) &&
				selection.IsSupportedSparseDefaultValue(sparse1.DefaultValue, sparse2.DefaultValue))
				return selection;
			// otherwise, use predicate search
			return SelectImplementation(RecentAPIs, sparse1.Storage, sparse2.Storage, validApi: a =>
					a.IsSupportedSparseFormat(sparse1.Format, sparse2.Format) &&
					a.IsSupportedSparseIndexType(sparse1.IndexType, sparse2.IndexType) &&
					a.IsSupportedSparseDefaultValue(sparse1.DefaultValue, sparse2.DefaultValue));
		}

		/// <summary>
		/// Special version for <see cref="AbstractApi"/> of method <see cref="AbstractRuntimeApi.SelectImplementation{T}(LinkedList{T}, IStorage, IStorage, Predicate{T})"/>
		/// </summary>
		public static AbstractApi SelectImplementation(IStorage storage1, IStorage storage2) => SelectImplementation(RecentAPIs, storage1, storage2);
		#endregion


		#region support information
		public abstract bool IsSupportedSparseFormat(SparseVectorFormat format);

		public abstract bool IsSupportedSparseFormat(SparseMatrixFormat format);

		public abstract bool IsSupportedSparseFormat(SparseVectorFormat format1, SparseVectorFormat format2);

		public abstract bool IsSupportedSparseFormat(SparseVectorFormat vector, SparseMatrixFormat matrix);

		public abstract bool IsSupportedSparseFormat(SparseMatrixFormat format1, SparseMatrixFormat format2);

		public abstract bool IsSupportedSparseIndexType(DataType type);

		public abstract bool IsSupportedSparseDefaultValue<T>(T value) where T : unmanaged;

		public abstract bool IsSupportedSparseIndexType(DataType type1, DataType type2);

		public abstract bool IsSupportedSparseDefaultValue<T>(T value1, T value2) where T : unmanaged;

		/// <summary>
		/// Get list of the supported <see cref="CombinationOfLocations"/> for all ternary operations. Since <see cref="AbstractApi"/> has no definition of ternary operations, this override returns null.
		/// </summary>
		public override IReadOnlyList<ImmutableThreeElementSet<CombinationOfLocations>> SupportedTernaryLocations => Array.Empty<ImmutableThreeElementSet<CombinationOfLocations>>();

		/// <summary>
		/// When implemented by a derived class, get the list of supported transfer between <see cref="CombinationOfLocations"/> and C# managed memory
		/// </summary>
		public abstract IReadOnlyList<CombinationOfLocations> SupportedManagedTransfer { get; }

		/// <summary>
		/// When implemented by a derived class, check whether the given <see cref="CombinationOfLocations"/> can transfer data with C# managed memory using this implementation
		/// </summary>
		/// <param name="locations">The <see cref="CombinationOfLocations"/> to indicate the unmanaged storage location combination</param>
		/// <returns>Whether this implementation supports data transfer between <paramref name="locations"/> and C# managed memory</returns>
		public virtual bool IsSupportedTransfer(CombinationOfLocations locations) => this.SupportedManagedTransfer.Contains(locations);
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