using System;


namespace Althea.Solver.Cuda
{
	/// <summary>
	/// This is a CUDA Solver status type returned by the library functions and it can have the following values.
	/// </summary>
	public enum Status
	{
		/// <summary>
		/// The operation completed successfully
		/// </summary>
		Success = 0,
		/// <summary>
		/// The cuSolver library was not initialized. This is usually caused by the
		/// lack of a prior call, an error in the CUDA Runtime API called by the
		/// cuSolver routine, or an error in the hardware setup.<para/>
		/// To correct: call cusolverCreate() prior to the function call; and
		/// check that the hardware, an appropriate version of the driver, and the
		/// cuSolver library are correctly installed.
		/// </summary>
		NotInititialized = 1,
		/// <summary>
		/// Resource allocation failed inside the cuSolver library. This is usually
		/// caused by a cudaMalloc() failure.<para/>
		/// To correct: prior to the function call, deallocate previously allocated
		/// memory as much as possible.
		/// </summary>
		AllocFailed = 2,
		/// <summary>
		/// An unsupported value or parameter was passed to the function (a
		/// negative vector size, for example).<para/>
		/// To correct: ensure that all the parameters being passed have valid
		/// values.
		/// </summary>
		InvalidValue = 3,
		/// <summary>
		/// The function requires a feature absent from the device architecture;
		/// usually caused by the lack of support for atomic operations or double
		/// precision.<para/>
		/// To correct: compile and run the application on a device with compute
		/// capability 2.0 or above.
		/// </summary>
		ArchMismatch = 4,
		/// <summary>
		/// 
		/// </summary>
		MappingError = 5,
		/// <summary>
		/// The GPU program failed to execute. This is often caused by a launch
		/// failure of the kernel on the GPU, which can be caused by multiple
		/// reasons.<para/>
		/// To correct: check that the hardware, an appropriate version of the
		/// driver, and the cuSolver library are correctly installed.
		/// </summary>
		ExecutionFailed = 6,
		/// <summary>
		/// An internal cuSolver operation failed. This error is usually caused by a
		/// cudaMemcpyAsync() failure.<para/>
		/// To correct: check that the hardware, an appropriate version of the
		/// driver, and the cuSolver library are correctly installed. Also, check
		/// that the memory passed as a parameter to the routine is not being
		/// deallocated prior to the routine’s completion.
		/// </summary>
		InternalError = 7,
		/// <summary>
		/// The matrix type is not supported by this function. This is usually caused
		/// by passing an invalid matrix descriptor to the function.<para/>
		/// To correct: check that the fields in descrA were set correctly.
		/// </summary>
		MatrixTypeNotSupported = 8,
		/// <summary>
		/// 
		/// </summary>
		NotSupported = 9,
		/// <summary>
		/// 
		/// </summary>
		ZeroPivot = 10,
		/// <summary>
		/// 
		/// </summary>
		InvalidLicense = 11
	}

	/// <summary>
	/// Used for selecting part of the eigenvalues
	/// </summary>
	public enum EigRange
	{
		/// <summary>
		/// Select all the eigenvalues
		/// </summary>
		All = 1001,
		/// <summary>
		/// Select by eigenvalues' index (from small to large)
		/// </summary>
		Index = 1002,
		/// <summary>
		/// Select by eigenvalues' value
		/// </summary>
		Value = 1003,
	}

}