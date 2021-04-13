using System;


namespace Althea.Backend.Cuda
{
	/// <summary>
	/// The class used to set CUDA back-end implementations, inherits <see cref="ISetBackend"/>
	/// </summary>
	public sealed class CudaImplementations : ISetBackend
	{
		bool ISetBackend.Available => true;

		Type ISetBackend.StorageImplementation => typeof(Storage.StorageApi);

		Type ISetBackend.DenseLinearAlgebraImplementation => typeof(LinearAlgebra.DenseApi);

		Type ISetBackend.SparseLinearAlgebraImplementation => typeof(LinearAlgebra.SparseApi);

		Type ISetBackend.DenseTensorAlgebraImplementation => typeof(TensorAlgebra.DenseApi);

		Type ISetBackend.SparseTensorAlgebraImplementation => typeof(TensorAlgebra.DenseApi);

		Type ISetBackend.RandomImplementation => typeof(Random.RandomApi);

		Type ISetBackend.SolverImplementation => typeof(Solver.SolverApi);
	}
}
