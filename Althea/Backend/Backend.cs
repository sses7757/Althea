using System;


namespace Althea.Backend
{
	/// <summary>
	/// The interface used to set the back-end implementations all at once
	/// </summary>
	public interface ISetBackend
	{
		/// <summary>
		/// The implementation type of <see cref="Memory.AbstractApi"/>
		/// </summary>
		Type MemoryImplementation { get; }

		/// <summary>
		/// The implementation type of <see cref="LinearAlgebra.AbstractApi"/>
		/// </summary>
		Type LinearAlgebraImplementation { get; }

		/// <summary>
		/// The implementation type of <see cref="TensorAlgebra.AbstractApi"/>
		/// </summary>
		Type TensorAlgebraImplementation { get; }

		/// <summary>
		/// The implementation type of <see cref="Statistics.AbstractApi"/>
		/// </summary>
		Type StatisticsImplementation { get; }

		/// <summary>
		/// The implementation type of <see cref="Solver.AbstractApi"/>
		/// </summary>
		Type SolverImplementation { get; }
	}
}
