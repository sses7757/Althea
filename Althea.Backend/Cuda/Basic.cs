using System;


namespace Althea.Backend.Cuda
{
	/// <summary>
	/// The class used to set CUDA back-end implementations, inherits <see cref="ISetBackend"/>
	/// </summary>
	public sealed class CudaImplementations : ISetBackend
	{
		/// <summary>
		/// The default constructor
		/// </summary>
		public CudaImplementations()
		{
			try
			{
				this.Available = Storage.NativeMethods.cudaDeviceSynchronize() == CudaError.Success;
			}
			catch (Exception)
			{
				this.Available = false;
				throw;
			}
		}

		/// <summary>
		/// Get a <see cref="bool"/> indicating whether CUA is available when initializing this instance
		/// </summary>
		public bool Available { get; }

		Type ISetBackend.StorageImplementation => typeof(Storage.StorageApi);

		Type ISetBackend.DenseLinearAlgebraImplementation => typeof(LinearAlgebra.DenseApi);

		Type ISetBackend.SparseLinearAlgebraImplementation => typeof(LinearAlgebra.SparseApi);

		Type ISetBackend.DenseTensorAlgebraImplementation => typeof(TensorAlgebra.DenseApi);

		Type ISetBackend.SparseTensorAlgebraImplementation => typeof(TensorAlgebra.DenseApi);

		Type ISetBackend.RandomImplementation => typeof(Random.RandomApi);

		Type ISetBackend.SolverImplementation => typeof(Solver.SolverApi);
	}
}
