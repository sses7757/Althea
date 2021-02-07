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

		// Ignore Spelling: N-ary
		/// <summary>
		/// Get list of the supported <see cref="CombinationOfLocations"/> for all N-ary operations. Each value in the list is a set of <paramref name="N"/> values to indicate a supported combination of certain <see cref="CombinationOfLocations"/>. Or null if there are no N-ary operations.
		/// </summary>
		/// <param name="N">The number of operands, must be <paramref name="N"/> &gt; 0</param>
		/// <returns>The list of the supported memory locations for all N-ary operations. Or null if there are no N-ary operations.</returns>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="N"/> &lt;= 0</exception>
		public override IReadOnlyList<IImmutableSet<CombinationOfLocations>> SupportedNaryLocations(int N)
		{
			return N switch
			{
				1 => this.SupportedUnaryLocations.Select(l => (IImmutableSet<CombinationOfLocations>)(ImmutableZeroOneElementSet<CombinationOfLocations>)l),
				2 => this.SupportedBinaryLocations.Select(l => (IImmutableSet<CombinationOfLocations>)l),
				3 => this.SupportedTernaryLocations.Select(l => (IImmutableSet<CombinationOfLocations>)l),
				> 3 => Array.Empty<IImmutableSet<CombinationOfLocations>>(), // there are no N-ary operations
				_ => throw new ArgumentOutOfRangeException(nameof(N)),
			};
		}

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
		/// <param name="threshold">the abs value below it will regarded as zero</param>
		/// <returns>a <see cref="SparseVectorWrapper{T}"/></returns>
		SparseVectorWrapper<T> VectorDenseToSparse<T>(Storage<T> y, int n, float threshold = 0) where T : unmanaged;

		/// <summary>
		/// Convert the dense vector y to sparse vector x with truncation <paramref name="threshold"/>.
		/// </summary>
		/// <param name="y">dense vector y</param>
		/// <param name="n">length of vector</param>
		/// <param name="threshold">the abs value below it will regarded as zero</param>
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
		#endregion
	}
}