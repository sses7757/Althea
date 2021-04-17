using System.Diagnostics;
using System.Runtime.CompilerServices;


namespace Althea.Backend.Cuda.LinearAlgebra.Dense
{
	/// <summary>
	/// The returned status (errors) of the cuBlas (CDUA BLAS) API calls
	/// </summary>
	public enum CudaBlasStatus
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
		/// An access to GPU memory space failed, which is usually caused by a failure to bind a texture.<para/>
		/// To correct: prior to the function call, unbind any previously bound textures.
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
		/// The functionality requested is not supported
		/// </summary>
		NotSupported = 15,
		/// <summary>
		/// The functionality requested requires some license and an error was detected when trying to check the current licensing. This error can happen if the license is not present or is expired or if the environment variable NVIDIA_LICENSE_FILE is not set properly.
		/// </summary>
		LicenseError = 16
	}


	/// <summary>
	/// The static class containing extension methods for <see cref="CudaBlasStatus"/>
	/// </summary>
	public static partial class StatusExtension
	{
		/// <summary>
		/// Check whether the input <see cref="CudaBlasStatus"/> is success or not and throw exception if it is not
		/// </summary>
		/// <param name="err">The <see cref="CudaBlasStatus"/> to be checked</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Check(this CudaBlasStatus err)
		{
			if (err != CudaBlasStatus.Success)
			{
				throw new StatusException(err, new StackTrace(0));
			}
		}
	}


	/// <summary>
	/// The <see cref="PointerMode"/> enum indicates whether the scalar values are passed by reference on the host or device.<br/>
	/// It is important to point out that if several scalar values are present in the function call, all of them must conform to the same single pointer mode.<br/>
	/// The pointer mode can be set and retrieved using <see cref="NativeMethods.cublasGetPointerMode"/> and <see cref="NativeMethods.cublasSetPointerMode"/> routines, respectively.
	/// </summary>
	internal enum PointerMode
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
	/// The <see cref="AtomicsMode"/> enum indicates whether cuBLAS routines which has an alternate implementation using atomics can be used.<br/>
	/// The atomics mode can be set and queried using and routines <see cref="NativeMethods.cublasSetAtomicsMode"/> and <see cref="NativeMethods.cublasGetAtomicsMode"/>, respectively.
	/// </summary>
	internal enum AtomicsMode
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
	/// The <see cref="MatrixFillMode"/> enum type indicates which part (lower or upper) of the dense matrix was filled and consequently should be used by the function.<br/>
	/// Its values correspond to Fortran characters ‘L’ or ‘l’ (lower) and ‘U’ or ‘u’ (upper) that are often used as parameters to legacy BLAS implementations.
	/// </summary>
	internal enum MatrixFillMode
	{
		/// <summary>
		/// the lower part of the matrix is filled
		/// </summary>
		Lower = 0,
		/// <summary>
		/// the upper part of the matrix is filled
		/// </summary>
		Upper = 1
	}

	/// <summary>
	/// The <see cref="DiagType"/> enum indicates whether the main diagonal of the dense matrix is unity and consequently should not be touched or modified by the function.<br/>
	/// Its values correspond to Fortran characters ‘N’ or ‘n’ (non-unit) and ‘U’ or ‘u’ (unit) that are often used as parameters to legacy BLAS implementations.
	/// </summary>
	internal enum DiagType
	{
		/// <summary>
		/// the matrix diagonal has non-unit elements
		/// </summary>
		NonUnit = 0,
		/// <summary>
		/// the matrix diagonal has unit elements
		/// </summary>
		Unit = 1
	}

	/// <summary>
	/// The <see cref="SideMode"/> enum indicates whether the dense matrix is on the left or right side in the matrix equation solved by a particular function.<br/>
	/// Its values correspond to Fortran characters ‘L’ or ‘l’ (left) and ‘R’ or ‘r’ (right) that are often used as parameters to legacy BLAS implementations.
	/// </summary>
	internal enum SideMode
	{
		/// <summary>
		/// the matrix is on the left side in the equation
		/// </summary>
		Left = 0,
		/// <summary>
		/// the matrix is on the right side in the equation
		/// </summary>
		Right = 1
	}
	
	/// <summary>
	/// The cuBLAS computation type
	/// </summary>
	internal enum ComputeType
	{
		/// <summary>
		/// This is the default and highest-performance mode for 16-bit half precision floating point and all compute and intermediate storage precisions with at least 16-bit half precision. Tensor Cores will be used whenever possible.
		/// </summary>
		Compute16F = 64,
		/// <summary>
		/// This mode uses 16-bit half precision floating point standardized arithmetic for all phases of calculations and is primarily intended for numerical robustness studies, testing, and debugging. This mode might not be as performant as the other modes since it disables use of tensor cores.
		/// </summary>
		Compute16F_Pedantic = 65,
		/// <summary>
		/// This is the default 32-bit single precision floating point and uses compute and intermediate storage precisions of at least 32-bits.
		/// </summary>
		Compute32F = 68,
		/// <summary>
		/// Uses 32-bit single precision floating point arithmetic for all phases of calculations and also disables algorithmic optimizations such as Gaussian complexity reduction (3M).
		/// </summary>
		Compute32F_Pedantic = 69,
		/// <summary>
		/// Allows the library to use Tensor Cores with automatic down-conversion and 16-bit half-precision compute for 32-bit input and output matrices.
		/// </summary>
		Compute32F_Fast_16F = 74,
		/// <summary>
		/// Allows the library to use Tensor Cores with automatic down-conversion and <see cref="BrainHalf"/> compute for 32-bit input and output matrices. See <see ref="http://docs.nvidia.com/cuda/cuda-c-programming-guide/index.html#wmma-altfp">Alternate Floating Point</see> section for more details on <see cref="BrainHalf"/> (bfloat16).
		/// </summary>
		Compute32F_Fast_16BF = 75,
		/// <summary>
		/// Allows the library to use Tensor Cores with TF32 compute for 32-bit input and output matrices. See <see ref="http://docs.nvidia.com/cuda/cuda-c-programming-guide/index.html#wmma-altfp">Alternate Floating Point</see> section for more details on TF32 compute.
		/// </summary>
		Compute32F_Fast_TF32 = 77,
		/// <summary>
		/// This is the default 64-bit double precision floating point and uses compute and intermediate storage precisions of at least 64-bits.
		/// </summary>
		Compute64F = 70,
		/// <summary>
		/// Uses 64-bit double precision floating point arithmetic for all phases of calculations and also disables algorithmic optimizations such as Gaussian complexity reduction (3M).
		/// </summary>
		Compute64F_Pedantic = 71,
		/// <summary>
		/// This is the default 32-bit integer mode and uses compute and intermediate storage precisions of at least 32-bits.
		/// </summary>
		Compute32I = 72,
		/// <summary>
		/// Uses 32-bit integer arithmetic for all phases of calculations.
		/// </summary>
		Compute32I_Pedantic = 73,
	}

	internal enum GemmAlgorithm
	{
		Default = -1,
		// other values are all deprecated
	}
}
