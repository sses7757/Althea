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
		/// When implemented by a derived class, add the sparse vector x to a dense vector y. $y[x_{\text{ind}}] += \alpha x_{\text{val}}$
		/// </summary>
		/// <param name="alpha">scalar to multiply x</param>
		/// <param name="x">sparse vector x</param>
		/// <param name="y">dense vector y</param>
		public abstract void VectorSparseAddToDense<T>(T alpha, SparseVectorWrapper<T> x, Storage<T> y) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, calculate the dot product of a sparse vector and a dense vector. $\vec{x} \cdot \vec{y}$
		/// </summary>
		/// <param name="x">sparse vector x</param>
		/// <param name="y">dense vector y</param>
		/// <param name="conjX">conjugate <paramref name="x"/> or not</param>
		/// <returns>output dot result</returns>
		public abstract T VectorSparseDotDense<T>(SparseVectorWrapper<T> x, Storage<T> y, bool conjX) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, add the sparse vector <paramref name="x"/> to another sparse vector <paramref name="y"/>.
		/// </summary>
		/// <param name="x">sparse vector x</param>
		/// <param name="y">sparse vector y</param>
		/// <returns></returns>
		public abstract SparseVectorWrapper<T> VectorSparseAddSparse<T>(SparseVectorWrapper<T> x, SparseVectorWrapper<T> y) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, point-wise multiply a sparse vector and a dense vector.
		/// </summary>
		/// <param name="x">sparse vector x</param>
		/// <param name="y">dense vector y</param>
		public abstract void VectorSparsePointWiseMultiplyDense<T>(SparseVectorWrapper<T> x, Storage<T> y) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, point-wise divide a sparse vector and a dense vector.
		/// </summary>
		/// <param name="x">sparse vector x</param>
		/// <param name="y">dense vector y</param>
		public abstract void VectorSparsePointWiseDivideDense<T>(SparseVectorWrapper<T> x, Storage<T> y) where T : unmanaged;
		#endregion
	}
}