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

		Type ISetBackend.DenseLinearAlgebraImplementation => typeof(LinearAlgebra.Dense.DenseApi);

		Type ISetBackend.SparseLinearAlgebraImplementation => typeof(int/*LinearAlgebra.Sparse.SparseApi*/);

		Type ISetBackend.DenseTensorAlgebraImplementation => typeof(int/*TensorAlgebra.DenseApi*/);

		Type ISetBackend.SparseTensorAlgebraImplementation => typeof(int/*TensorAlgebra.DenseApi*/);

		Type ISetBackend.RandomImplementation => typeof(int/*Random.RandomApi*/);

		Type ISetBackend.SolverImplementation => typeof(int/*Solver.SolverApi*/);
	}
}
