using System;
using System.Runtime.InteropServices;


namespace Althea
{
	/// <summary>
	/// The format specification enum of sparse matrix
	/// </summary>
	// TODO: move to Althea.LinearAlgebra.Sparse
	[Flags]
	public enum SparseMatrixFormat
	{
		/// <summary>
		/// Coordinate Format (COO) that stores each non-zero element's <c>x</c> and <c>y</c> coordinates which are sorted in the row-first order. Value = 000...0001
		/// </summary>
		COOR = 1 << 0,

		/// <summary>
		/// Coordinate Format (COO) that stores each non-zero element's <c>x</c> and <c>y</c> coordinates which are sorted in the column-first order. Value = 000...0010
		/// </summary>
		COOC = 1 << 1,

		/// <summary>
		/// Since <see cref="COOC"/> and <see cref="COOR"/> are so similar that it can be generalized to the Coordinate Format (COO). Value = 000...0011
		/// </summary>
		/// <remarks>Since the atom formats are all orthogonal in binary, this kind of definition becomes useful.<br/>
		/// If some custom formats are defined afterwards, this trick should still be used.</remarks>
		Coordinated = COOR | COOC,

		/// <summary>
		/// Compressed Sparse Row Format (CSR). The only way the CSR differs from the COO format is that the array containing the row indices is compressed in CSR format, that is, the row index array only stores the <c>LeadDim + 1</c> the end-of-row offsets of the value array. Value = 000...0100
		/// </summary>
		CSR = 1 << 2,

		/// <summary>
		/// Compressed Sparse Column Format (CSC). The only way the CSC differs from the CSR format is that the column index array instead of row indices array stores the end-of-row offsets. Value = 000...01000
		/// </summary>
		CSC = 1 << 3,

		/// <summary>
		/// Since <see cref="CSR"/> and <see cref="CSC"/> are so similar that it can be generalized to the Compressed Format. Value = 000...01100
		/// </summary>
		/// <remarks>Since the atom formats are all orthogonal in binary, this kind of definition becomes useful.<br/>
		/// If some custom formats are defined afterwards, this trick should still be used.</remarks>
		Compressed = CSR | CSC,

		/// <summary>
		/// The row majored formats. Value = 000...0101
		/// </summary>
		RowMajor = COOR | CSR,

		/// <summary>
		/// The column majored formats. Value = 000...01010
		/// </summary>
		ColumnMajor = COOC | CSC,

		/// <summary>
		/// Any of the atomic formats. Value = 111...111
		/// </summary>
		/// <remarks>Since the atom formats are all orthogonal in binary, this kind of definition becomes useful.<br/>
		/// If some custom formats are defined afterwards, this trick should still be used.</remarks>
		Any = ~0
	}


	/// <summary>
	/// The FillMode type indicates which part (lower or upper) of the dense matrix was filled and consequently should be used by the function. <br/>
	/// Its values correspond to Fortran characters ‘L’ or ‘l’ (lower) and ‘U’ or ‘u’ (upper) that are often used as parameters to legacy BLAS implementations.
	/// </summary>
	// TODO: move to Althea.LinearAlgebra.Dense
	public enum MatrixFillMode
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
	/// The DiagType type indicates whether the main diagonal of the dense matrix is unity and consequently should not be touched or modified by the function. <br/>
	/// Its values correspond to Fortran characters ‘N’ or ‘n’ (non-unit) and ‘U’ or ‘u’ (unit) that are often used as parameters to legacy BLAS implementations.
	/// </summary>
	// TODO: move to Althea.LinearAlgebra.Sparse
	public enum DiagType
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
	/// The SideMode type indicates whether the dense matrix is on the left or right side in the matrix equation solved by a particular function. <br/>
	/// Its values correspond to Fortran characters ‘L’ or ‘l’ (left) and ‘R’ or ‘r’ (right) that are often used as parameters to legacy BLAS implementations.
	/// </summary>
	// TODO: move to Althea.LinearAlgebra.Dense
	public enum SideMode
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
	/// The operation type to indicate which operation needs to be performed with the matrix.
	/// </summary>
	/// <remarks>the entry with larger value is less preferred</remarks>
	// TODO: move to Althea.LinearAlgebra
	public enum MatrixOperation
	{
		/// <summary>
		/// the non-transpose operation
		/// </summary>
		None = 0,
		/// <summary>
		/// the transpose operation
		/// </summary>
		Transpose = 1,
		/// <summary>
		/// the conjugate transpose operation
		/// </summary>
		ConjugateTranspose = 2,
		/// <summary>
		/// the conjugate only operation
		/// </summary>
		Conjugate = 3,
	}

	/// <summary>
	/// Used in overloading operators
	/// </summary>
	// TODO: move to Althea.LinearAlgebra.Sparse
	public enum PowerOperation
	{
		/// <summary>
		/// Nothing
		/// </summary>
		None = 0,
		/// <summary>
		/// Transpose only
		/// </summary>
		Transpose = ~0, // -1
		/// <summary>
		/// Conjugate only
		/// </summary>
		Conjugate = int.MaxValue,
		/// <summary>
		/// conjugate transpose
		/// </summary>
		Dagger = ~int.MaxValue // int.MinValue
	}

	/// <summary>
	/// The <see cref="CudaDataType"/> type is an enum to specify the data precision. It is used when the data reference does not carry the type itself (e.g <see cref="IntPtr"/> alone).
	/// </summary>
	// TODO: move to Althea.Cuda, and set to internal
	public enum CudaDataType
	{
		/// <summary>
		/// 32 bit real <see cref="float"/>
		/// </summary>
		RealFloat32 = 0,
		/// <summary>
		/// 64 bit real <see cref="double"/>
		/// </summary>
		RealFloat64 = 1,
		/// <summary>
		/// 16 bit real, also known as 'half', mostly unsupported
		/// </summary>
		RealFloat16 = 2,
		/// <summary>
		/// 8 bit signed integer <see cref="sbyte"/>, mostly unsupported
		/// </summary>
		RealInt8 = 3,
		/// <summary>
		/// <see cref="FloatComplex"/> made of two <see cref="float"/>s
		/// </summary>
		ComplexFloat32 = 4,
		/// <summary>
		/// <see cref="DoubleComplex"/> made of two <see cref="double"/>s
		/// </summary>
		ComplexFloat64 = 5,
		/// <summary>
		/// complex of two 16 bit reals, mostly unsupported
		/// </summary>
		ComplexFloat16 = 6,
		/// <summary>
		/// complex made of two <see cref="sbyte"/>s
		/// </summary>
		ComplexInt8 = 7,
		/// <summary>
		/// 8 bit unsigned integer <see cref="byte"/>
		/// </summary>
		RealUInt8 = 8,
		/// <summary>
		/// complex made of two <see cref="byte"/>s
		/// </summary>
		ComplexUInt8 = 9,
		/// <summary>
		/// 32 bit signed integer <see cref="int"/>
		/// </summary>
		RealInt32 = 10,
		/// <summary>
		/// complex made of two <see cref="int"/>s
		/// </summary>
		ComplexInt32 = 11,
		/// <summary>
		/// 32 bit unsigned integer <see cref="uint"/>
		/// </summary>
		RealUInt32 = 12,
		/// <summary>
		/// complex made of two <see cref="uint"/>s
		/// </summary>
		ComplexUInt32 = 13
	}

	/// <summary>
	/// Memory copy enum
	/// </summary>
	// TODO: move to Althea.Cuda, and set to internal
	public enum MemoryCopyKind
	{
		/// <summary>
		/// host to host
		/// </summary>
		HostToHost = 0,
		/// <summary>
		/// host to device
		/// </summary>
		HostToDevice = 1,
		/// <summary>
		/// device to host
		/// </summary>
		DeviceToHost = 2,
		/// <summary>
		/// device to device
		/// </summary>
		DeviceToDevice = 3,
		/// <summary>
		/// Direction of the transfer is inferred from the pointer values. Requires unified virtual addressing
		/// </summary>
		Default = 4
	}


	/// <summary>
	/// Binary operations supported by tensor point-wise operations
	/// </summary>
	// TODO: move to Althea.TensorAlgebra, and modify
	public enum BinaryOperation
	{
		/// <summary>
		/// Addition of two elements
		/// </summary>
		Add = 3,
		/// <summary>
		/// Multiplication of two elements
		/// </summary>
		Mul = 5,
		/// <summary>
		/// Maximum of two elements
		/// </summary>
		Max = 6,
		/// <summary>
		/// Minimum of two elements
		/// </summary>
		Min = 7
	}

	/// <summary>
	/// Unitary operations supported by tensor point-wise operations
	/// </summary>
	// TODO: move to Althea.TensorAlgebra, and modify
	public enum UnitaryOperation
	{
		/// <summary>
		/// Identity operator (i.e., elements are not changed)
		/// </summary>
		Identity = 1,
		/// <summary>
		/// Square root
		/// </summary>
		Sqrt = 2,
		/// <summary>
		/// Rectified linear unit (x if x > 0, otherwise 0)
		/// </summary>
		ReLU = 8,
		/// <summary>
		/// Complex conjugate
		/// </summary>
		Conjugate = 9,
		/// <summary>
		/// Reciprocal
		/// </summary>
		Reciprocate = 10,
		/// <summary>
		/// Logistic sigmoid function: <c>y = 1 / (1 + exp(-x))</c>
		/// </summary>
		Sigmoid = 11,
		/// <summary>
		/// Exponentiation
		/// </summary>
		Exp = 22,
		/// <summary>
		/// Base <c>e</c> logarithm
		/// </summary>
		Log = 23,
		/// <summary>
		/// Absolute value
		/// </summary>
		Abs = 24,
		/// <summary>
		/// Negation
		/// </summary>
		Negate = 25,
		/// <summary>
		/// Sine function
		/// </summary>
		Sin = 26,
		/// <summary>
		/// Cosine function
		/// </summary>
		Cos = 27,
		/// <summary>
		/// Tangent function
		/// </summary>
		Tan = 28,
		/// <summary>
		/// Hyperbolic sine
		/// </summary>
		Sinh = 29,
		/// <summary>
		/// Hyperbolic cosine
		/// </summary>
		Cosh = 30,
		/// <summary>
		/// Hyperbolic tangent function
		/// </summary>
		Tanh = 12,
		/// <summary>
		/// Inverse sine
		/// </summary>
		ArcSin = 31,
		/// <summary>
		/// Inverse cosine
		/// </summary>
		ArcCos = 32,
		/// <summary>
		/// Inverse tangent
		/// </summary>
		ArcTan = 33,
		/// <summary>
		/// Inverse hyperbolic sine
		/// </summary>
		ArcSinh = 34,
		/// <summary>
		/// Inverse hyperbolic cosine
		/// </summary>
		ArcCosh = 35,
		/// <summary>
		/// Inverse hyperbolic tangent
		/// </summary>
		ArcTanh = 36,
		/// <summary>
		/// Ceiling function
		/// </summary>
		Ceil = 37,
		/// <summary>
		/// Floor function
		/// </summary>
		Floor = 38,
	}


	/// <summary>
	/// Error codes returned by CUDA driver API calls
	/// </summary>
	// TODO: move to Althea.Cuda, and set internal
	public enum CudaError
	{
		/// <summary>
		/// No errors
		/// </summary>
		Success = 0,

		/// <summary>
		/// This indicates that one or more of the parameters passed to the API call is not within an acceptable range of values
		/// </summary>
		ErrorInvalidValue = 1,

		/// <summary>
		/// The API call failed because it was unable to allocate enough memory to perform the requested operation
		/// </summary>
		ErrorOutOfMemory = 2,

		/// <summary>
		/// The API call failed because the CUDA driver and runtime could not be initialized
		/// </summary>
		ErrorNotInitialized = 3,

		/// <summary>
		/// This indicates that a CUDA Runtime API call cannot be executed because it is being called during process shut down, at a point in time after CUDA driver has been unloaded
		/// </summary>
		ErrorDeinitialized = 4,

		/// <summary>
		/// This indicates profiler is not initialized for this run.
		/// This can happen when the application is running with external profiling tools
		/// like visual profiler.
		/// </summary>
		ErrorProfilerDisabled = 5,

		/// <summary>
		/// This error return is deprecated as of CUDA 5.0. It is no longer an error
		/// to attempt to enable/disable the profiling via ::cuProfilerStart or
		/// ::cuProfilerStop without initialization.
		/// </summary>
		[Obsolete("deprecated as of CUDA 5.0")]
		ErrorProfilerNotInitialized = 6,

		/// <summary>
		/// This error return is deprecated as of CUDA 5.0. It is no longer an error
		/// to call cuProfilerStart() when profiling is already enabled.
		/// </summary>
		[Obsolete("deprecated as of CUDA 5.0")]
		ErrorProfilerAlreadyStarted = 7,

		/// <summary>
		/// This error return is deprecated as of CUDA 5.0. It is no longer an error
		/// to call cuProfilerStop() when profiling is already disabled.
		/// </summary>
		[Obsolete("deprecated as of CUDA 5.0")]
		ErrorProfilerAlreadyStopped = 8,

		/// <summary>
		/// This indicates that a kernel launch is requesting resources that can never be satisfied by the current device. Requesting more shared memory per block than the device supports will trigger this error, as will requesting too many threads or blocks. See 'cudaDeviceProp' for more device limitations.
		/// </summary>
		ErrorInvalidConfiguration = 9,

		/// <summary>
		/// This indicates that one or more of the pitch-related parameters passed to the API call is not within the acceptable range for pitch.
		/// </summary>
		ErrorInvalidPitchValue = 12,

		/// <summary>
		/// This indicates that the symbol name/identifier passed to the API call is not a valid name or identifier.
		/// </summary>
		ErrorInvalidSymbol = 13,

		/// <summary>
		/// This indicates that at least one host pointer passed to the API call is not a valid host pointer.
		/// </summary>
		[Obsolete("deprecated as of CUDA 10.1")]
		ErrorInvalidHostPointer = 16,

		/// <summary>
		/// This indicates that at least one device pointer passed to the API call is not a valid device pointer.
		/// </summary>
		[Obsolete("deprecated as of CUDA 10.1")]
		ErrorInvalidDevicePointer = 17,

		/// <summary>
		/// This indicates that the texture passed to the API call is not a valid texture.
		/// </summary>
		ErrorInvalidTexture = 18,

		/// <summary>
		/// This indicates that the texture binding is not valid. This occurs if you call 'cudaGetTextureAlignmentOffset()' with an unbound texture.
		/// </summary>
		ErrorInvalidTextureBinding = 19,

		/// <summary>
		/// This indicates that the channel descriptor passed to the API call is not valid. This occurs if the format is not one of the formats specified by 'cudaChannelFormatKind', or if one of the dimensions is invalid.
		/// </summary>
		ErrorInvalidChannelDescriptor = 20,

		/// <summary>
		/// This indicates that the direction of the <see cref="Runtime.Cuda.NativeMethods.cudaMemcpy"/> passed to the API call is not one of the types specified by <see cref="MemoryCopyKind"/>.
		/// </summary>
		ErrorInvalidMemcpyDirection = 21,

		/// <summary>
		/// This indicated that the user has taken the address of a constant variable,
		/// which was forbidden up until the CUDA 3.1 release.
		/// </summary>
		[Obsolete("This error return is deprecated as of CUDA 3.1. Variables in constant memory may now have their address taken by the runtime via cudaGetSymbolAddress().")]
		ErrorAddressOfConstant = 22,

		/// <summary>
		/// This indicated that a texture fetch was not able to be performed.
		/// This was previously used for device emulation of texture operations.
		/// </summary>
		[Obsolete("This error return is deprecated as of CUDA 3.1. Device emulation mode was removed with the CUDA 3.1 release.")]
		ErrorTextureFetchFailed = 23,

		/// <summary>
		/// This indicated that a texture was not bound for access.
		/// This was previously used for device emulation of texture operations.
		/// </summary>
		[Obsolete("This error return is deprecated as of CUDA 3.1. Device emulation mode was removed with the CUDA 3.1 release.")]
		ErrorTextureNotBound = 24,

		/// <summary>
		/// This indicated that a synchronization operation had failed.
		/// This was previously used for some device emulation functions.
		/// </summary>
		[Obsolete("This error return is deprecated as of CUDA 3.1. Device emulation mode was removed with the CUDA 3.1 release.")]
		ErrorSynchronizationError = 25,

		/// <summary>
		/// This indicates that a non-float texture was being accessed with linear
		/// filtering. This is not supported by CUDA.
		/// </summary>
		ErrorInvalidFilterSetting = 26,

		/// <summary>
		/// This indicates that an attempt was made to read a non-float texture as a
		/// normalized float. This is not supported by CUDA.
		/// </summary>
		ErrorInvalidNormSetting = 27,

		/// <summary>
		/// Mixing of device and device emulation code was not allowed.
		/// </summary>
		[Obsolete("This error return is deprecated as of CUDA 3.1. Device emulation mode was removed with the CUDA 3.1 release.")]
		ErrorMixedDeviceExecution = 28,

		/// <summary>
		/// This indicates that the API call is not yet implemented. Production
		/// releases of CUDA will never return this error.
		/// </summary>
		[Obsolete("deprecated as of CUDA 4.1")]
		ErrorNotYetImplemented = 31,

		/// <summary>
		/// This indicated that an emulated device pointer exceeded the 32-bit address range.
		/// </summary>
		[Obsolete("This error return is deprecated as of CUDA 3.1. Device emulation mode was removed with the CUDA 3.1 release.")]
		ErrorMemoryValueTooLarge = 32,

		/// <summary>
		/// This indicates that the installed NVIDIA CUDA driver is older than the
		/// CUDA runtime library. This is not a supported configuration. Users should
		/// install an updated NVIDIA display driver to allow the application to run.
		/// </summary>
		ErrorInsufficientDriver = 35,

		/// <summary>
		/// This indicates that the surface passed to the API call is not a valid
		/// surface.
		/// </summary>
		ErrorInvalidSurface = 37,

		/// <summary>
		/// This indicates that multiple global or constant variables (across separate
		/// CUDA source files in the application) share the same string name.
		/// </summary>
		ErrorDuplicateVariableName = 43,

		/// <summary>
		/// This indicates that multiple textures (across separate CUDA source
		/// files in the application) share the same string name.
		/// </summary>
		ErrorDuplicateTextureName = 44,

		/// <summary>
		/// This indicates that multiple surfaces (across separate CUDA source
		/// files in the application) share the same string name.
		/// </summary>
		ErrorDuplicateSurfaceName = 45,

		/// <summary>
		/// This indicates that all CUDA devices are busy or unavailable at the current
		/// time. Devices are often busy/unavailable due to use of
		/// 'cudaComputeModeExclusive', 'cudaComputeModeProhibited' or when long
		/// running CUDA kernels have filled up the GPU and are blocking new work
		/// from starting. They can also be unavailable due to memory constraints
		/// on a device that already has active CUDA work being performed.
		/// </summary>
		ErrorDevicesUnavailable = 46,

		/// <summary>
		/// This indicates that the current context is not compatible with this
		/// the CUDA Runtime. This can only occur if you are using CUDA
		/// Runtime/Driver interoperability and have created an existing Driver
		/// context using the driver API. The Driver context may be incompatible
		/// either because the Driver context was created using an older version 
		/// of the API, because the Runtime API call expects a primary driver 
		/// context and the Driver context is not primary, or because the Driver 
		/// context has been destroyed. Please see 'CUDART_DRIVER' "Interactions 
		/// with the CUDA Driver API" for more information.
		/// </summary>
		ErrorIncompatibleDriverContext = 49,

		/// <summary>
		/// The device function being invoked (usually via 'cudaLaunchKernel()') was not
		/// previously configured via the 'cudaConfigureCall()' function.
		/// </summary>
		ErrorMissingConfiguration = 52,

		/// <summary>
		/// This indicated that a previous kernel launch failed. This was previously
		/// used for device emulation of kernel launches.
		/// </summary>
		[Obsolete("This error return is deprecated as of CUDA 3.1. Device emulation mode was removed with the CUDA 3.1 release.")]
		ErrorPriorLaunchFailure = 53,

		/// <summary>
		/// This error indicates that a device runtime grid launch did not occur 
		/// because the depth of the child grid would exceed the maximum supported
		/// number of nested grid launches. 
		/// </summary>
		ErrorLaunchMaxDepthExceeded = 65,

		/// <summary>
		/// This error indicates that a grid launch did not occur because the kernel 
		/// uses file-scoped textures which are unsupported by the device runtime. 
		/// Kernels launched via the device runtime only support textures created with 
		/// the Texture Object API's.
		/// </summary>
		ErrorLaunchFileScopedTex = 66,

		/// <summary>
		/// This error indicates that a grid launch did not occur because the kernel 
		/// uses file-scoped surfaces which are unsupported by the device runtime.
		/// Kernels launched via the device runtime only support surfaces created with
		/// the Surface Object API's.
		/// </summary>
		ErrorLaunchFileScopedSurf = 67,

		/// <summary>
		/// This error indicates that a call to <see cref="Runtime.Cuda.NativeMethods.cudaDeviceSynchronize"/> made from
		/// the device runtime failed because the call was made at grid depth greater
		/// than either the default (2 levels of grids) or user specified device 
		/// limit 'cudaLimitDevRuntimeSyncDepth'. To be able to synchronize on 
		/// launched grids at a greater depth successfully, the maximum nested 
		/// depth at which :<see cref="Runtime.Cuda.NativeMethods.cudaDeviceSynchronize"/> will be called must be specified 
		/// with the 'cudaLimitDevRuntimeSyncDepth' limit to the 'cudaDeviceSetLimit'
		/// API before the host-side launch of a kernel using the device runtime. 
		/// Keep in mind that additional levels of sync depth require the runtime 
		/// to reserve large amounts of device memory that cannot be used for 
		/// user allocations.
		/// </summary>
		ErrorSyncDepthExceeded = 68,

		/// <summary>
		/// This error indicates that a device runtime grid launch failed because
		/// the launch would exceed the limit ::cudaLimitDevRuntimePendingLaunchCount.
		/// For this launch to proceed successfully, 'cudaDeviceSetLimit' must be
		/// called to set the 'cudaLimitDevRuntimePendingLaunchCount' to be higher 
		/// than the upper bound of outstanding launches that can be issued to the
		/// device runtime. Keep in mind that raising the limit of pending device
		/// runtime launches will require the runtime to reserve device memory that
		/// cannot be used for user allocations.
		/// </summary>
		ErrorLaunchPendingCountExceeded = 69,

		/// <summary>
		/// The requested device function does not exist or is not compiled for the
		/// proper device architecture.
		/// </summary>
		ErrorInvalidDeviceFunction = 98,

		/// <summary>
		/// This indicates that no CUDA-capable devices were detected by the installed
		/// CUDA driver.
		/// </summary>
		ErrorNoDevice = 100,

		/// <summary>
		/// This indicates that the device ordinal supplied by the user does not
		/// correspond to a valid CUDA device.
		/// </summary>
		ErrorInvalidDevice = 101,

		/// <summary>
		/// This indicates an internal startup failure in the CUDA runtime.
		/// </summary>
		ErrorStartupFailure = 127,

		/// <summary>
		/// This indicates that the device kernel image is invalid.
		/// </summary>
		ErrorInvalidKernelImage = 200,

		/// <summary>
		/// This most frequently indicates that there is no context bound to the
		/// current thread. This can also be returned if the context passed to an
		/// API call is not a valid handle (such as a context that has had
		/// ::cuCtxDestroy() invoked on it). This can also be returned if a user
		/// mixes different API versions (i.e. 3010 context with 3020 API calls).
		/// See ::cuCtxGetApiVersion() for more details.
		/// </summary>
		ErrorDeviceUninitialized = 201,

		/// <summary>
		/// This indicates that the buffer object could not be mapped.
		/// </summary>
		ErrorMapBufferObjectFailed = 205,

		/// <summary>
		/// This indicates that the buffer object could not be unmapped.
		/// </summary>
		ErrorUnmapBufferObjectFailed = 206,

		/// <summary>
		/// This indicates that the specified array is currently mapped and thus
		/// cannot be destroyed.
		/// </summary>
		ErrorArrayIsMapped = 207,

		/// <summary>
		/// This indicates that the resource is already mapped.
		/// </summary>
		ErrorAlreadyMapped = 208,

		/// <summary>
		/// This indicates that there is no kernel image available that is suitable
		/// for the device. This can occur when a user specifies code generation
		/// options for a particular CUDA source file that do not include the
		/// corresponding device configuration.
		/// </summary>
		ErrorNoKernelImageForDevice = 209,

		/// <summary>
		/// This indicates that a resource has already been acquired.
		/// </summary>
		ErrorAlreadyAcquired = 210,

		/// <summary>
		/// This indicates that a resource is not mapped.
		/// </summary>
		ErrorNotMapped = 211,

		/// <summary>
		/// This indicates that a mapped resource is not available for access as an
		/// array.
		/// </summary>
		ErrorNotMappedAsArray = 212,

		/// <summary>
		/// This indicates that a mapped resource is not available for access as a
		/// pointer.
		/// </summary>
		ErrorNotMappedAsPointer = 213,

		/// <summary>
		/// This indicates that an uncorrectable ECC error was detected during
		/// execution.
		/// </summary>
		ErrorECCUncorrectable = 214,

		/// <summary>
		/// This indicates that the ::cudaLimit passed to the API call is not
		/// supported by the active device.
		/// </summary>
		ErrorUnsupportedLimit = 215,

		/// <summary>
		/// This indicates that a call tried to access an exclusive-thread device that 
		/// is already in use by a different thread.
		/// </summary>
		ErrorDeviceAlreadyInUse = 216,

		/// <summary>
		/// This error indicates that P2P access is not supported across the given
		/// devices.
		/// </summary>
		ErrorPeerAccessUnsupported = 217,

		/// <summary>
		/// A PTX compilation failed. The runtime may fall back to compiling PTX if
		/// an application does not contain a suitable binary for the current device.
		/// </summary>
		ErrorInvalidPtx = 218,

		/// <summary>
		/// This indicates an error with the OpenGL or DirectX context.
		/// </summary>
		ErrorInvalidGraphicsContext = 219,

		/// <summary>
		/// This indicates that an uncorrectable NVLink error was detected during the
		/// execution.
		/// </summary>
		ErrorNvlinkUncorrectable = 220,

		/// <summary>
		/// This indicates that the PTX JIT compiler library was not found. The JIT Compiler
		/// library is used for PTX compilation. The runtime may fall back to compiling PTX
		/// if an application does not contain a suitable binary for the current device.
		/// </summary>
		ErrorJitCompilerNotFound = 221,

		/// <summary>
		/// This indicates that the device kernel source is invalid.
		/// </summary>
		ErrorInvalidSource = 300,

		/// <summary>
		/// This indicates that the file specified was not found.
		/// </summary>
		ErrorFileNotFound = 301,

		/// <summary>
		/// This indicates that a link to a shared object failed to resolve.
		/// </summary>
		ErrorSharedObjectSymbolNotFound = 302,

		/// <summary>
		/// This indicates that initialization of a shared object failed.
		/// </summary>
		ErrorSharedObjectInitFailed = 303,

		/// <summary>
		/// This error indicates that an OS call failed.
		/// </summary>
		ErrorOperatingSystem = 304,

		/// <summary>
		/// This indicates that a resource handle passed to the API call was not
		/// valid. Resource handles are opaque types like ::cudaStream_t and
		/// ::Event_t.
		/// </summary>
		ErrorInvalidResourceHandle = 400,

		/// <summary>
		/// This indicates that a resource required by the API call is not in a
		/// valid state to perform the requested operation.
		/// </summary>
		ErrorIllegalState = 401,

		/// <summary>
		/// This indicates that a named symbol was not found. Examples of symbols
		/// are global/constant variable names, texture names, and surface names.
		/// </summary>
		ErrorSymbolNotFound = 500,

		/// <summary>
		/// This indicates that asynchronous operations issued previously have not
		/// completed yet. This result is not actually an error, but must be indicated
		/// differently than <see cref="Success"/> (which indicates completion). Calls that
		/// may return this value include ::EventQuery() and ::cudaStreamQuery().
		/// </summary>
		ErrorNotReady = 600,

		/// <summary>
		/// The device encountered a load or store instruction on an invalid memory address.
		/// This leaves the process in an inconsistent state and any further CUDA work
		/// will return the same error. To continue using CUDA, the process must be terminated
		/// and relaunched.
		/// </summary>
		ErrorIllegalAddress = 700,

		/// <summary>
		/// This indicates that a launch did not occur because it did not have
		/// appropriate resources. Although this error is similar to
		/// <see cref="ErrorInvalidConfiguration"/>, this error usually indicates that the
		/// user has attempted to pass too many arguments to the device kernel, or the
		/// kernel launch specifies too many threads for the kernel's register count.
		/// </summary>
		ErrorLaunchOutOfResources = 701,

		/// <summary>
		/// This indicates that the device kernel took too long to execute. This can
		/// only occur if timeouts are enabled - see the device property
		/// \ref ::cudaDeviceProp::kernelExecTimeoutEnabled "kernelExecTimeoutEnabled"
		/// for more information.
		/// This leaves the process in an inconsistent state and any further CUDA work
		/// will return the same error. To continue using CUDA, the process must be terminated
		/// and relaunched.
		/// </summary>
		ErrorLaunchTimeout = 702,

		/// <summary>
		/// This error indicates a kernel launch that uses an incompatible texturing
		/// mode.
		/// </summary>
		ErrorLaunchIncompatibleTexturing = 703,

		/// <summary>
		/// This error indicates that a call to ::cudaDeviceEnablePeerAccess() is
		/// trying to re-enable peer addressing on from a context which has already
		/// had peer addressing enabled.
		/// </summary>
		ErrorPeerAccessAlreadyEnabled = 704,

		/// <summary>
		/// This error indicates that ::cudaDeviceDisablePeerAccess() is trying to 
		/// disable peer addressing which has not been enabled yet via 
		/// ::cudaDeviceEnablePeerAccess().
		/// </summary>
		ErrorPeerAccessNotEnabled = 705,

		/// <summary>
		/// This indicates that the user has called ::cudaSetValidDevices(),
		/// ::cudaSetDeviceFlags(), ::cudaD3D9SetDirect3DDevice(),
		/// ::cudaD3D10SetDirect3DDevice, ::cudaD3D11SetDirect3DDevice(), or
		/// ::cudaVDPAUSetVDPAUDevice() after initializing the CUDA runtime by
		/// calling non-device management operations (allocating memory and
		/// launching kernels are examples of non-device management operations).
		/// This error can also be returned if using runtime/driver
		/// interoperability and there is an existing ::CUcontext active on the
		/// host thread.
		/// </summary>
		ErrorSetOnActiveProcess = 708,

		/// <summary>
		/// This error indicates that the context current to the calling thread
		/// has been destroyed using ::cuCtxDestroy, or is a primary context which
		/// has not yet been initialized.
		/// </summary>
		ErrorContextIsDestroyed = 709,

		/// <summary>
		/// An assert triggered in device code during kernel execution. The device
		/// cannot be used again. All existing allocations are invalid. To continue
		/// using CUDA, the process must be terminated and relaunched.
		/// </summary>
		ErrorAssert = 710,

		/// <summary>
		/// This error indicates that the hardware resources required to enable
		/// peer access have been exhausted for one or more of the devices 
		/// passed to ::EnablePeerAccess().
		/// </summary>
		ErrorTooManyPeers = 711,

		/// <summary>
		/// This error indicates that the memory range passed to ::cudaHostRegister()
		/// has already been registered.
		/// </summary>
		ErrorHostMemoryAlreadyRegistered = 712,

		/// <summary>
		/// This error indicates that the pointer passed to ::cudaHostUnregister()
		/// does not correspond to any currently registered memory region.
		/// </summary>
		ErrorHostMemoryNotRegistered = 713,

		/// <summary>
		/// Device encountered an error in the call stack during kernel execution,
		/// possibly due to stack corruption or exceeding the stack size limit.
		/// This leaves the process in an inconsistent state and any further CUDA work
		/// will return the same error. To continue using CUDA, the process must be terminated
		/// and relaunched.
		/// </summary>
		ErrorHardwareStackError = 714,

		/// <summary>
		/// The device encountered an illegal instruction during kernel execution
		/// This leaves the process in an inconsistent state and any further CUDA work
		/// will return the same error. To continue using CUDA, the process must be terminated
		/// and relaunched.
		/// </summary>
		ErrorIllegalInstruction = 715,

		/// <summary>
		/// The device encountered a load or store instruction
		/// on a memory address which is not aligned.
		/// This leaves the process in an inconsistent state and any further CUDA work
		/// will return the same error. To continue using CUDA, the process must be terminated
		/// and relaunched.
		/// </summary>
		ErrorMisalignedAddress = 716,

		/// <summary>
		/// While executing a kernel, the device encountered an instruction
		/// which can only operate on memory locations in certain address spaces
		/// (global, shared, or local), but was supplied a memory address not
		/// belonging to an allowed address space.
		/// This leaves the process in an inconsistent state and any further CUDA work
		/// will return the same error. To continue using CUDA, the process must be terminated
		/// and relaunched.
		/// </summary>
		ErrorInvalidAddressSpace = 717,

		/// <summary>
		/// The device encountered an invalid program counter.
		/// This leaves the process in an inconsistent state and any further CUDA work
		/// will return the same error. To continue using CUDA, the process must be terminated
		/// and relaunched.
		/// </summary>
		ErrorInvalidPc = 718,

		/// <summary>
		/// An exception occurred on the device while executing a kernel. Common
		/// causes include dereferencing an invalid device pointer and accessing
		/// out of bounds shared memory. Less common cases can be system specific - more
		/// information about these cases can be found in the system specific user guide.
		/// This leaves the process in an inconsistent state and any further CUDA work
		/// will return the same error. To continue using CUDA, the process must be terminated
		/// and relaunched.
		/// </summary>
		ErrorLaunchFailure = 719,

		/// <summary>
		/// This error indicates that the number of blocks launched per grid for a kernel that was
		/// launched via either ::cudaLaunchCooperativeKernel or ::cudaLaunchCooperativeKernelMultiDevice
		/// exceeds the maximum number of blocks as allowed by ::cudaOccupancyMaxActiveBlocksPerMultiprocessor
		/// or ::cudaOccupancyMaxActiveBlocksPerMultiprocessorWithFlags times the number of multiprocessors
		/// as specified by the device attribute ::cudaDevAttrMultiProcessorCount.
		/// </summary>
		ErrorCooperativeLaunchTooLarge = 720,

		/// <summary>
		/// This error indicates the attempted operation is not permitted.
		/// </summary>
		ErrorNotPermitted = 800,

		/// <summary>
		/// This error indicates the attempted operation is not supported
		/// on the current system or device.
		/// </summary>
		ErrorNotSupported = 801,

		/// <summary>
		/// This error indicates that the system is not yet ready to start any CUDA
		/// work.  To continue using CUDA, verify the system configuration is in a
		/// valid state and all required driver daemons are actively running.
		/// More information about this error can be found in the system specific
		/// user guide.
		/// </summary>
		ErrorSystemNotReady = 802,

		/// <summary>
		/// This error indicates that there is a mismatch between the versions of
		/// the display driver and the CUDA driver. Refer to the compatibility documentation
		/// for supported versions.
		/// </summary>
		ErrorSystemDriverMismatch = 803,

		/// <summary>
		/// This error indicates that the system was upgraded to run with forward compatibility
		/// but the visible hardware detected by CUDA does not support this configuration.
		/// Refer to the compatibility documentation for the supported hardware matrix or ensure
		/// that only supported hardware is visible during initialization via the CUDA_VISIBLE_DEVICES
		/// environment variable.
		/// </summary>
		ErrorCompatNotSupportedOnDevice = 804,

		/// <summary>
		/// The operation is not permitted when the stream is capturing.
		/// </summary>
		ErrorStreamCaptureUnsupported = 900,

		/// <summary>
		/// The current capture sequence on the stream has been invalidated due to
		/// a previous error.
		/// </summary>
		ErrorStreamCaptureInvalidated = 901,

		/// <summary>
		/// The operation would have resulted in a merge of two independent capture
		/// sequences.
		/// </summary>
		ErrorStreamCaptureMerge = 902,

		/// <summary>
		/// The capture was not initiated in this stream.
		/// </summary>
		ErrorStreamCaptureUnmatched = 903,

		/// <summary>
		/// The capture sequence contains a fork that was not joined to the primary
		/// stream.
		/// </summary>
		ErrorStreamCaptureUnjoined = 904,

		/// <summary>
		/// A dependency would have been created which crosses the capture sequence
		/// boundary. Only implicit in-stream ordering dependencies are allowed to
		/// cross the boundary.
		/// </summary>
		ErrorStreamCaptureIsolation = 905,

		/// <summary>
		/// The operation would have resulted in a disallowed implicit dependency on
		/// a current capture sequence from cudaStreamLegacy.
		/// </summary>
		ErrorStreamCaptureImplicit = 906,

		/// <summary>
		/// The operation is not permitted on an event which was last recorded in a
		/// capturing stream.
		/// </summary>
		ErrorCapturedEvent = 907,

		/// <summary>
		/// A stream capture sequence not initiated with the ::cudaStreamCaptureModeRelaxed
		/// argument to ::cudaStreamBeginCapture was passed to ::cudaStreamEndCapture in a
		/// different thread.
		/// </summary>
		ErrorStreamCaptureWrongThread = 908,

		/// <summary>
		/// This indicates that the wait operation has timed out.
		/// </summary>
		ErrorTimeout = 909,

		/// <summary>
		/// This error indicates that the graph update was not performed because it included 
		/// changes which violated constraints specific to instantiated graph update.
		/// </summary>
		ErrorGraphExecUpdateFailure = 910,

		/// <summary>
		/// This indicates that an unknown internal error has occurred.
		/// </summary>
		ErrorUnknown = 999,

		/// <summary>
		/// Any not handled CUDA driver error is added to this value and returned via
		/// the runtime. Production releases of CUDA should not return such errors.
		/// </summary>
		[Obsolete("deprecated as of CUDA 4.1")]
		ErrorApiFailureBase = 10000
	}
}