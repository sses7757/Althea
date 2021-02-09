using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Althea.Linq;


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
		/// Special version for <see cref="AbstractApi"/> of method <see cref="AbstractRuntimeApi.SelectImplementation{T}(LinkedList{T}, StorageLocation)"/>
		/// </summary>
		public static AbstractApi SelectImplementation(StorageLocation location) => SelectImplementation(RecentAPIs, location);

		/// <summary>
		/// Special version for <see cref="AbstractApi"/> of method <see cref="AbstractRuntimeApi.SelectImplementation{T}(LinkedList{T}, IStorage)"/>
		/// </summary>
		public static AbstractApi SelectImplementation(IStorage storage) => SelectImplementation(RecentAPIs, storage);

		/// <summary>
		/// Special version for <see cref="AbstractApi"/> of method <see cref="AbstractRuntimeApi.SelectImplementation{T}(LinkedList{T}, IStorage, IStorage)"/>
		/// </summary>
		public static AbstractApi SelectImplementation(IStorage storage1, IStorage storage2) => SelectImplementation(RecentAPIs, storage1, storage2);
		#endregion


		#region support information
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
		/// <param name="x">The input sparse vector as a <see cref="SparseVectorWrapper{T}"/></param>
		/// <param name="y">The input/output dense vector as a <see cref="Storage{T}"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		public abstract void VectorSparseAddToDense<T>(T α, SparseVectorWrapper<T> x, Storage<T> y) where T : unmanaged, IEquatable<T>;

		/// <summary>
		/// When implemented by a derived class, calculate the dot (inner) product of a sparse vector <paramref name="x"/> and a dense vector <paramref name="y"/>: result = <c><paramref name="x"/>^op <paramref name="y"/></c>, op = <paramref name="conjX"/> ? H : T.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="conjX">Whether to conjugate <paramref name="x"/> or not</param>
		/// <param name="x">The input sparse vector as a <see cref="SparseVectorWrapper{T}"/></param>
		/// <param name="y">The input dense vector as a <see cref="Storage{T}"/></param>
		/// <returns>The dot product result as a <typeparamref name="T"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		public abstract T VectorSparseDotDense<T>(bool conjX, SparseVectorWrapper<T> x, Storage<T> y) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, add the sparse vector <paramref name="x"/> to another sparse vector <paramref name="y"/> and put the result in a new sparse vector.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The input sparse vector x</param>
		/// <param name="y">The input sparse vector y</param>
		/// <param name="createFunc">See <see cref="DelegateCreateNew{T}"/></param>
		/// <returns>The result of sum of <paramref name="x"/> and <paramref name="y"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		public abstract SparseVectorWrapper<T> VectorSparseAddSparse<T>(SparseVectorWrapper<T> x, SparseVectorWrapper<T> y, DelegateCreateNew<T>? createFunc = null) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, point-wise multiply a sparse vector by a dense vector: <c><paramref name="x"/> *= <paramref name="y"/></c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The input/output sparse vector x</param>
		/// <param name="y">The input dense vector y</param>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		public abstract void VectorSparsePointWiseMultiplyDense<T>(SparseVectorWrapper<T> x, Storage<T> y) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, point-wise divide a sparse vector by a dense vector: <c><paramref name="x"/> /= <paramref name="y"/></c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The input/output sparse vector x</param>
		/// <param name="y">The input dense vector y</param>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		public abstract void VectorSparsePointWiseDivideDense<T>(SparseVectorWrapper<T> x, Storage<T> y) where T : unmanaged;
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
		public abstract void MatrixDenseMultiplyVectorSparse<T>(MatrixOperation op, T α, long m, long n, Storage<T> M, SparseVectorWrapper<T> x, T β, Storage<T> y) where T : unmanaged, IEquatable<T>;

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
		public abstract void VectorSparseOuter<T>(bool conjY, T α, SparseVectorWrapper<T> x, SparseVectorWrapper<T> y, T β, SparseMatrixWrapper<T> M) where T : unmanaged, IEquatable<T>;
		#endregion
	}
}