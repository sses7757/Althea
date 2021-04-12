using System;


namespace Althea.Backend.CSharp
{
	/// <summary>
	/// The class used to set C# back-end implementations, inherits <see cref="ISetBackend"/>
	/// </summary>
	public sealed class CSharpImplementations : ISetBackend
	{
		bool ISetBackend.Available => true;

		Type ISetBackend.StorageImplementation => typeof(Storage.StorageApi);

		Type ISetBackend.DenseLinearAlgebraImplementation => typeof(LinearAlgebra.DenseApi);

		Type ISetBackend.SparseLinearAlgebraImplementation => typeof(LinearAlgebra.SparseApi);

#pragma warning disable CS8603
		Type ISetBackend.DenseTensorAlgebraImplementation => null;

		Type ISetBackend.SparseTensorAlgebraImplementation => null;
#pragma warning restore CS8603

		Type ISetBackend.RandomImplementation => typeof(Random.RandomApi);
		
		Type ISetBackend.SolverImplementation => typeof(Solver.SolverApi);
	}
}
