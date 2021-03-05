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

		Type ISetBackend.DenseLinearAlgebraImplementation => throw new NotImplementedException();

		Type ISetBackend.SparseLinearAlgebraImplementation => throw new NotImplementedException();

		Type ISetBackend.DenseTensorAlgebraImplementation => throw new NotImplementedException();

		Type ISetBackend.SparseTensorAlgebraImplementation => throw new NotImplementedException();

		Type ISetBackend.StatisticsImplementation => throw new NotImplementedException();

		Type ISetBackend.SolverImplementation => throw new NotImplementedException();
	}
}
