namespace Althea.Blas.Cuda
{
	/// <summary>
	/// CUBLAS status type returns
	/// </summary>
	public enum Status
	{
		/// <summary>
		/// The operation completed successfully
		/// </summary>
		Success = 0,
		/// <summary>
		/// The cuBlas library was not initialized. This is usually caused by the
		/// lack of a prior call, an error in the CUDA Runtime API called by the
		/// cuSolver routine, or an error in the hardware setup.<para/>
		/// To correct: call cuBlasCreate() prior to the function call; and
		/// check that the hardware, an appropriate version of the driver, and the
		/// cuSolver library are correctly installed.
		/// </summary>
		NotInitialized = 1,
		/// <summary>
		/// Resource allocation failed inside the cuBlas library. This is usually
		/// caused by a cudaMalloc() failure.<para/>
		/// To correct: prior to the function call, deallocate previously allocated
		/// memory as much as possible.
		/// </summary>
		AllocFailed = 3,
		/// <summary>
		/// An unsupported value or parameter was passed to the function (a
		/// negative vector size, for example).<para/>
		/// To correct: ensure that all the parameters being passed have valid
		/// values.
		/// </summary>
		InvalidValue = 7,
		/// <summary>
		/// The function requires a feature absent from the device architecture;
		/// usually caused by the lack of support for atomic operations or double
		/// precision.<para/>
		/// To correct: compile and run the application on a device with compute
		/// capability 2.0 or above.
		/// </summary>
		ArchMismatch = 8,
		/// <summary>
		/// 
		/// </summary>
		MappingError = 11,
		/// <summary>
		/// The GPU program failed to execute. This is often caused by a launch
		/// failure of the kernel on the GPU, which can be caused by multiple
		/// reasons.<para/>
		/// To correct: check that the hardware, an appropriate version of the
		/// driver, and the cuSolver library are correctly installed.
		/// </summary>
		ExecutionFailed = 13,
		/// <summary>
		/// An internal cuSolver operation failed. This error is usually caused by a
		/// cudaMemcpyAsync() failure.<para/>
		/// To correct: check that the hardware, an appropriate version of the
		/// driver, and the cuSolver library are correctly installed. Also, check
		/// that the memory passed as a parameter to the routine is not being
		/// deallocated prior to the routine’s completion.
		/// </summary>
		InternalError = 14,
		/// <summary>
		/// 
		/// </summary>
		NotSupported = 15,
		/// <summary>
		/// 
		/// </summary>
		LicenseError = 16
	}

	/// <summary>
	/// The PointerMode type indicates whether the scalar values are passed by reference on the host or device. <br/>
	/// It is important to point out that if several scalar values are present in the function call, all of them must conform to the same single pointer mode. <br/>
	/// The pointer mode can be set and retrieved using <see cref="NativeMethods.cublasGetPointerMode_v2"/> and <see cref="NativeMethods.cublasSetPointerMode_v2"/> routines, respectively.
	/// </summary>
	public enum PointerMode
	{
		/// <summary>
		/// the scalars are passed by reference on the host
		/// </summary>
		Host = 0,
		/// <summary>
		/// the scalars are passed by reference on the device
		/// </summary>
		Device = 1
	}

	/// <summary>
	/// The type indicates whether cuBLAS routines which has an alternate implementation
	/// using atomics can be used.<br/> The atomics mode can be set and queried using and routines <br/>
	/// <see cref="NativeMethods.cublasSetAtomicsMode"/> and <see cref="NativeMethods.cublasGetAtomicsMode"/>
	/// <br/> respectively.
	/// </summary>
	public enum AtomicsMode
	{
		/// <summary>
		/// the usage of atomics is not allowed
		/// </summary>
		NotAllowed = 0,
		/// <summary>
		/// the usage of atomics is allowed
		/// </summary>
		Allowed = 1
	}

	/// <summary>
	/// Enum for default math mode/tensor operation
	/// </summary>
	public enum MathOp
	{
		/// <summary>
		/// </summary>
		DefaultMath = 0,
		/// <summary>
		/// </summary>
		TensorOpMath = 1
	}

	/// <summary>
	/// Used by routines <see cref="Xt.NativeMethods.cublasXtSetPinningMemMode"/> and <see cref="Xt.NativeMethods.cublasXtGetPinningMemMode"/>.
	/// </summary>
	public enum PinnedMemoryMode
	{
		/// <summary>
		/// 
		/// </summary>
		Disabled = 0,
		/// <summary>
		/// 
		/// </summary>
		Enabled = 1
	}

	/// <summary>
	/// The BLAS3 or BLAS-like routine supported by cublasXt API. <br/>
	/// This enum is used as parameters of the routines:
	/// <list type="bullet">
	/// <item><description><see cref="Xt.NativeMethods.cublasXtSetCpuRoutine"/></description></item>
	/// <item><description><see cref="Xt.NativeMethods.cublasXtSetCpuRatio"/></description></item>
	/// </list>
	/// to setup the hybrid configuration.
	/// </summary>
	public enum XtOperationList
	{
		/// <summary>
		/// general matrix multiply
		/// </summary>
		GEMM = 0,
		/// <summary>
		/// symmetric matrix rank-k update
		/// </summary>
		SYRK = 1,
		/// <summary>
		/// hermitian matrix rank-k update
		/// </summary>
		HERK = 2,
		/// <summary>
		/// symmetric matrix multiply
		/// </summary>
		SYMM = 3,
		/// <summary>
		/// hermitian matrix multiply
		/// </summary>
		HEMM = 4,
		/// <summary>
		/// triangular linear system solve
		/// </summary>
		TRSM = 5,
		/// <summary>
		/// symmetric matrix rank-2 update
		/// </summary>
		SYR2K = 6,
		/// <summary>
		/// hermitian matrix rank-2 update
		/// </summary>
		HER2K = 7,
		/// <summary>
		/// sparse matrix multiply
		/// </summary>
		SPMM = 8,
		/// <summary>
		/// symmetric matrix rank-x update
		/// </summary>
		SYRKX = 9,
		/// <summary>
		/// hermitian matrix rank-x update
		/// </summary>
		HERKX = 10,
		/// <summary>
		/// triangular matrix-matrix multiplication
		/// </summary>
		TRMM = 11,
		/// <summary>
		/// max number of routines
		/// </summary>
		ROUTINE_MAX = 12,
	}
}