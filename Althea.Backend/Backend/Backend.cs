using System;

[assembly: CLSCompliant(true)]


namespace Althea.Backend
{
	/// <summary>
	/// The interface used to set the back-end implementations all at once
	/// </summary>
	public interface ISetBackend
	{
		/// <summary>
		/// Check whether all the back-end implementations are available
		/// </summary>
		bool Available { get; }

		/// <summary>
		/// The implementation type of <see cref="Althea.Storage.AbstractApi"/>
		/// </summary>
		Type StorageImplementation { get; }

		/// <summary>
		/// The implementation type of <see cref="Althea.LinearAlgebra.AbstractApi"/>
		/// </summary>
		Type LinearAlgebraImplementation { get; }

		/// <summary>
		/// The implementation type of <see cref="Althea.TensorAlgebra.AbstractApi"/>
		/// </summary>
		Type TensorAlgebraImplementation { get; }

		/// <summary>
		/// The implementation type of <see cref="Althea.Statistics.AbstractApi"/>
		/// </summary>
		Type StatisticsImplementation { get; }

		/// <summary>
		/// The implementation type of <see cref="Althea.Solver.AbstractApi"/>
		/// </summary>
		Type SolverImplementation { get; }
	}
}
