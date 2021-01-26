using System;


namespace Althea.Backend.CSharp
{
	/// <summary>
	/// The class used to set C# back-end implementations, inherits <see cref="ISetBackend"/>
	/// </summary>
	public sealed class CSharpImplementations : ISetBackend
	{
		bool ISetBackend.Available => true;

		Type ISetBackend.MemoryImplementation => typeof(Storage.StorageApi);

		Type ISetBackend.LinearAlgebraImplementation => typeof(LinearAlgebra.LinearAlgebraApi);

		Type ISetBackend.TensorAlgebraImplementation => typeof(TensorAlgebra.TensorAlgebraApi);

		Type ISetBackend.StatisticsImplementation => typeof(Statistics.StatisticsApi);

		Type ISetBackend.SolverImplementation => typeof(Solver.SolverApi);
	}
}
