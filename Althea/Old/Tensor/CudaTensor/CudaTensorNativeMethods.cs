using System;
using System.Runtime.InteropServices;


namespace Althea.Tensor.Cuda
{
	/// <summary>
	/// The CUDA Tensor library native methods
	/// </summary>
	public static class NativeMethods
	{
		/// <summary>
		/// The CUDA Tensor library name
		/// </summary>
		public const string CUTENSOR_DLL_NAME = @"cutensor";

		// Notice that all the tensor D must have same properties as C

		#region initialize
		/// <summary>
		/// Initialize the CUDA Tensor library
		/// </summary>
		/// <param name="handle"><see cref="Handle"/></param>
		/// <returns><see cref="Status"/></returns>
		[DllImport(CUTENSOR_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cutensorInit(ref Handle handle);

		/// <summary>
		/// Initializes a tensor descriptor
		/// </summary>
		/// <param name="handle">Opaque <see cref="Handle"/> holding cuTENSOR’s library context</param>
		/// <param name="desc">output <see cref="TensorDescription"/> to the allocated tensor descriptor object</param>
		/// <param name="numModes">Number of modes (rank)</param>
		/// <param name="extent">Extent (size) of each mode (must be larger than zero)</param>
		/// <param name="stride"><c>stride[i]</c> denotes the displacement (stride) between two consecutive elements in the i<sup>th</sup>-mode. If it is null, a packed generalized column-major memory layout is assumed</param>
		/// <param name="dataType">Data type of the stored entries</param>
		/// <param name="unaryOp"><see cref="UnitaryOperation"/> that will be applied to each element of the corresponding tensor in a lazy fashion</param>
		/// <returns><see cref="Status"/></returns>
		[DllImport(CUTENSOR_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cutensorInitTensorDescriptor(ref Handle handle, ref TensorDescription desc, int numModes, long[] extent, long[] stride, CudaDataType dataType, UnitaryOperation unaryOp);

		/// <summary>
		/// Computes the minimal alignment requirement for a given pointer and descriptor.
		/// </summary>
		/// <param name="handle">Opaque <see cref="Handle"/> holding cuTENSOR’s library context</param>
		/// <param name="ptr">The data pointer</param>
		/// <param name="desc"><see cref="TensorDescription"/> to the tensor</param>
		/// <param name="alignmentRequirement">output, the largest alignment requirement that <paramref name="ptr"/> can fulfill (in bytes)</param>
		/// <returns><see cref="Status"/></returns>
		[DllImport(CUTENSOR_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cutensorGetAlignmentRequirement(ref Handle handle, IntPtr ptr, ref TensorDescription desc, ref int alignmentRequirement);
		#endregion

		#region permute
		/// <summary>
		/// This function performs an element-wise tensor operation.
		/// </summary>
		/// <param name="handle">Opaque <see cref="Handle"/> holding cuTENSOR’s library context</param>
		/// <param name="alpha">scalar to multiply <paramref name="A"/></param>
		/// <param name="A">tensor A</param>
		/// <param name="descA"><see cref="TensorDescription"/> to the tensor <paramref name="A"/></param>
		/// <param name="modeA">The mode of <paramref name="A"/></param>
		/// <param name="B">tensor B</param>
		/// <param name="descB"><see cref="TensorDescription"/> to the tensor <paramref name="B"/></param>
		/// <param name="modeB">The mode of <paramref name="B"/></param>
		/// <param name="type">compute type <see cref="CudaDataType"/></param>
		/// <param name="stream">CUDA stream pointer</param>
		/// <returns><see cref="Status"/></returns>
		internal delegate Status Permutation<T>(ref Handle handle, ref T alpha, [In] IntPtr A, ref TensorDescription descA, in int modeA, IntPtr B, ref TensorDescription descB, in int modeB, CudaDataType type, IntPtr stream);

		[DllImport(CUTENSOR_DLL_NAME, EntryPoint = "cutensorPermutation")]
		//[NativeMethodBoundary]
		internal static extern Status cutensorPermutationS(ref Handle handle, ref float alpha, [In] IntPtr A, ref TensorDescription descA, in int modeA, IntPtr B, ref TensorDescription descB, in int modeB, CudaDataType type, IntPtr stream);
		[DllImport(CUTENSOR_DLL_NAME, EntryPoint = "cutensorPermutation")]
		//[NativeMethodBoundary]
		internal static extern Status cutensorPermutationD(ref Handle handle, ref double alpha, [In] IntPtr A, ref TensorDescription descA, in int modeA, IntPtr B, ref TensorDescription descB, in int modeB, CudaDataType type, IntPtr stream);
		[DllImport(CUTENSOR_DLL_NAME, EntryPoint = "cutensorPermutation")]
		//[NativeMethodBoundary]
		internal static extern Status cutensorPermutationC(ref Handle handle, ref FloatComplex alpha, [In] IntPtr A, ref TensorDescription descA, in int modeA, IntPtr B, ref TensorDescription descB, in int modeB, CudaDataType type, IntPtr stream);
		[DllImport(CUTENSOR_DLL_NAME, EntryPoint = "cutensorPermutation")]
		//[NativeMethodBoundary]
		internal static extern Status cutensorPermutationZ(ref Handle handle, ref DoubleComplex alpha, [In] IntPtr A, ref TensorDescription descA, in int modeA, IntPtr B, ref TensorDescription descB, in int modeB, CudaDataType type, IntPtr stream);
		#endregion

		#region point-wise
		/// <summary>
		/// Element-wise tensor operation for two input tensors.
		/// </summary>
		/// <param name="handle">Opaque <see cref="Handle"/> holding cuTENSOR’s library context</param>
		/// <param name="alpha">scalar to multiply <paramref name="A"/></param>
		/// <param name="A">tensor A</param>
		/// <param name="descA"><see cref="TensorDescription"/> to the tensor <paramref name="A"/></param>
		/// <param name="modeA">The mode of <paramref name="A"/></param>
		/// <param name="gamma">scalar to multiply <paramref name="C"/></param>
		/// <param name="C">tensor C</param>
		/// <param name="descC"><see cref="TensorDescription"/> to the tensor <paramref name="C"/></param>
		/// <param name="modeC">The mode of <paramref name="C"/></param>
		/// <param name="D">tensor D</param>
		/// <param name="descD"><see cref="TensorDescription"/> to the tensor <paramref name="D"/></param>
		/// <param name="modeD">The mode of <paramref name="D"/></param>
		/// <param name="opAC"><see cref="BinaryOperation"/> for <paramref name="A"/> and <paramref name="C"/></param>
		/// <param name="type">compute type <see cref="CudaDataType"/></param>
		/// <param name="stream">CUDA stream pointer</param>
		/// <returns><see cref="Status"/></returns>
		internal delegate Status ElementwiseBinary<T>(ref Handle handle, ref T alpha, [In] IntPtr A, ref TensorDescription descA, in int modeA, ref T gamma, [In] IntPtr C, ref TensorDescription descC, in int modeC, IntPtr D, ref TensorDescription descD, in int modeD, BinaryOperation opAC, CudaDataType type, IntPtr stream);

		[DllImport(CUTENSOR_DLL_NAME, EntryPoint = "cutensorElementwiseBinary")]
		//[NativeMethodBoundary]
		internal static extern Status cutensorElementwiseBinaryS(ref Handle handle, ref float alpha, [In] IntPtr A, ref TensorDescription descA, in int modeA, ref float gamma, [In] IntPtr C, ref TensorDescription descC, in int modeC, IntPtr D, ref TensorDescription descD, in int modeD, BinaryOperation opAC, CudaDataType type, IntPtr stream);
		[DllImport(CUTENSOR_DLL_NAME, EntryPoint = "cutensorElementwiseBinary")]
		//[NativeMethodBoundary]
		internal static extern Status cutensorElementwiseBinaryD(ref Handle handle, ref double alpha, [In] IntPtr A, ref TensorDescription descA, in int modeA, ref double gamma, [In] IntPtr C, ref TensorDescription descC, in int modeC, IntPtr D, ref TensorDescription descD, in int modeD, BinaryOperation opAC, CudaDataType type, IntPtr stream);
		[DllImport(CUTENSOR_DLL_NAME, EntryPoint = "cutensorElementwiseBinary")]
		//[NativeMethodBoundary]
		internal static extern Status cutensorElementwiseBinaryC(ref Handle handle, ref FloatComplex alpha, [In] IntPtr A, ref TensorDescription descA, in int modeA, ref FloatComplex gamma, [In] IntPtr C, ref TensorDescription descC, in int modeC, IntPtr D, ref TensorDescription descD, in int modeD, BinaryOperation opAC, CudaDataType type, IntPtr stream);
		[DllImport(CUTENSOR_DLL_NAME, EntryPoint = "cutensorElementwiseBinary")]
		//[NativeMethodBoundary]
		internal static extern Status cutensorElementwiseBinaryZ(ref Handle handle, ref DoubleComplex alpha, [In] IntPtr A, ref TensorDescription descA, in int modeA, ref DoubleComplex gamma, [In] IntPtr C, ref TensorDescription descC, in int modeC, IntPtr D, ref TensorDescription descD, in int modeD, BinaryOperation opAC, CudaDataType type, IntPtr stream);

		/// <summary>
		/// Element-wise tensor operation for three input tensors.
		/// </summary>
		/// <param name="handle">Opaque <see cref="Handle"/> holding cuTENSOR’s library context</param>
		/// <param name="alpha">scalar to multiply <paramref name="A"/></param>
		/// <param name="A">tensor A</param>
		/// <param name="descA"><see cref="TensorDescription"/> to the tensor <paramref name="A"/></param>
		/// <param name="modeA">The mode of <paramref name="A"/></param>
		/// <param name="beta">scalar to multiply <paramref name="B"/></param>
		/// <param name="B">tensor B</param>
		/// <param name="descB"><see cref="TensorDescription"/> to the tensor <paramref name="B"/></param>
		/// <param name="modeB">The mode of <paramref name="B"/></param>
		/// <param name="gamma">scalar to multiply <paramref name="C"/></param>
		/// <param name="C">tensor C</param>
		/// <param name="descC"><see cref="TensorDescription"/> to the tensor <paramref name="C"/></param>
		/// <param name="modeC">The mode of <paramref name="C"/></param>
		/// <param name="D">tensor D</param>
		/// <param name="descD"><see cref="TensorDescription"/> to the tensor <paramref name="D"/></param>
		/// <param name="modeD">The mode of <paramref name="D"/></param>
		/// <param name="opAB"><see cref="BinaryOperation"/> for <paramref name="A"/> and <paramref name="B"/></param>
		/// <param name="opABC"><see cref="BinaryOperation"/> for <c><paramref name="opAB"/>(<paramref name="A"/>, <paramref name="B"/>)</c> and <paramref name="C"/></param>
		/// <param name="type">compute type <see cref="CudaDataType"/></param>
		/// <param name="stream">CUDA stream pointer</param>
		/// <returns><see cref="Status"/></returns>
		internal delegate Status ElementwiseTrinary<T>(ref Handle handle, ref T alpha, [In] IntPtr A, ref TensorDescription descA, in int modeA, ref T beta, [In] IntPtr B, ref TensorDescription descB, in int modeB, ref T gamma, [In] IntPtr C, ref TensorDescription descC, in int modeC, IntPtr D, ref TensorDescription descD, in int modeD, BinaryOperation opAB, BinaryOperation opABC, CudaDataType type, IntPtr stream);

		[DllImport(CUTENSOR_DLL_NAME, EntryPoint = "cutensorElementwiseTrinary")]
		//[NativeMethodBoundary]
		internal static extern Status cutensorElementwiseTrinaryS(ref Handle handle, ref float alpha, [In] IntPtr A, ref TensorDescription descA, in int modeA, ref float beta, [In] IntPtr B, ref TensorDescription descB, in int modeB, ref float gamma, [In] IntPtr C, ref TensorDescription descC, in int modeC, IntPtr D, ref TensorDescription descD, in int modeD, BinaryOperation opAB, BinaryOperation opABC, CudaDataType type, IntPtr stream);
		[DllImport(CUTENSOR_DLL_NAME, EntryPoint = "cutensorElementwiseTrinary")]
		//[NativeMethodBoundary]
		internal static extern Status cutensorElementwiseTrinaryD(ref Handle handle, ref double alpha, [In] IntPtr A, ref TensorDescription descA, in int modeA, ref double beta, [In] IntPtr B, ref TensorDescription descB, in int modeB, ref double gamma, [In] IntPtr C, ref TensorDescription descC, in int modeC, IntPtr D, ref TensorDescription descD, in int modeD, BinaryOperation opAB, BinaryOperation opABC, CudaDataType type, IntPtr stream);
		[DllImport(CUTENSOR_DLL_NAME, EntryPoint = "cutensorElementwiseTrinary")]
		//[NativeMethodBoundary]
		internal static extern Status cutensorElementwiseTrinaryC(ref Handle handle, ref FloatComplex alpha, [In] IntPtr A, ref TensorDescription descA, in int modeA, ref FloatComplex beta, [In] IntPtr B, ref TensorDescription descB, in int modeB, ref FloatComplex gamma, [In] IntPtr C, ref TensorDescription descC, in int modeC, IntPtr D, ref TensorDescription descD, in int modeD, BinaryOperation opAB, BinaryOperation opABC, CudaDataType type, IntPtr stream);
		[DllImport(CUTENSOR_DLL_NAME, EntryPoint = "cutensorElementwiseTrinary")]
		//[NativeMethodBoundary]
		internal static extern Status cutensorElementwiseTrinaryZ(ref Handle handle, ref DoubleComplex alpha, [In] IntPtr A, ref TensorDescription descA, in int modeA, ref DoubleComplex beta, [In] IntPtr B, ref TensorDescription descB, in int modeB, ref DoubleComplex gamma, [In] IntPtr C, ref TensorDescription descC, in int modeC, IntPtr D, ref TensorDescription descD, in int modeD, BinaryOperation opAB, BinaryOperation opABC, CudaDataType type, IntPtr stream);
		#endregion

		#region contraction
		/// <summary>
		/// Describes the tensor contraction problem of the form: <c>D = α * A · B + β * C</c>.
		/// </summary>
		/// <param name="handle">Opaque <see cref="Handle"/> holding cuTENSOR’s library context</param>
		/// <param name="desc">output <see cref="ContractDescription"/> that gets filled with the information that encodes the tensor contraction problem</param>
		/// <param name="descA"><see cref="TensorDescription"/> to the tensor A</param>
		/// <param name="modeA">The mode of A</param>
		/// <param name="alignmentRequirementA">The alignment requirement given by <see cref="cutensorGetAlignmentRequirement"/> of A</param>
		/// <param name="descB"><see cref="TensorDescription"/> to the tensor B</param>
		/// <param name="modeB">The mode of B</param>
		/// <param name="alignmentRequirementB">The alignment requirement given by <see cref="cutensorGetAlignmentRequirement"/> of B</param>
		/// <param name="descC"><see cref="TensorDescription"/> to the tensor C</param>
		/// <param name="modeC">The mode of C</param>
		/// <param name="alignmentRequirementC">The alignment requirement given by <see cref="cutensorGetAlignmentRequirement"/> of C</param>
		/// <param name="descD"><see cref="TensorDescription"/> to the tensor D</param>
		/// <param name="modeD">The mode of D</param>
		/// <param name="alignmentRequirementD">The alignment requirement given by <see cref="cutensorGetAlignmentRequirement"/> of D</param>
		/// <param name="computeType">The <see cref="ComputeType"/></param>
		/// <returns><see cref="Status"/></returns>
		[DllImport(CUTENSOR_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cutensorInitContractionDescriptor(ref Handle handle, ref ContractDescription desc,
			ref TensorDescription descA, in int modeA, int alignmentRequirementA,
			ref TensorDescription descB, in int modeB, int alignmentRequirementB,
			ref TensorDescription descC, in int modeC, int alignmentRequirementC,
			ref TensorDescription descD, in int modeD, int alignmentRequirementD, ComputeType computeType);

		/// <summary>
		/// Limits the search space of viable candidates (a.k.a. algorithms).
		/// </summary>
		/// <param name="handle">Opaque <see cref="Handle"/> holding cuTENSOR’s library context</param>
		/// <param name="find">output <see cref="ContractFind"/> representing the candidate</param>
		/// <param name="algo">The <see cref="ContractionAlgorithm"/> to use</param>
		/// <returns><see cref="Status"/></returns>
		[DllImport(CUTENSOR_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cutensorInitContractionFind(ref Handle handle, ref ContractFind find, ContractionAlgorithm algo);

		/// <summary>
		/// Determines the required workspaceSize for a given tensor contraction
		/// </summary>
		/// <param name="handle">Opaque <see cref="Handle"/> holding cuTENSOR’s library context</param>
		/// <param name="desc">The <see cref="ContractDescription"/> filled with the information that encodes the tensor contraction problem</param>
		/// <param name="find">The <see cref="ContractFind"/> representing the candidate</param>
		/// <param name="pref">The <see cref="WorkSpacePreference"/></param>
		/// <param name="workspaceSize">output workspace size (in bytes) that is required for the given tensor contraction</param>
		/// <returns><see cref="Status"/></returns>
		[DllImport(CUTENSOR_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cutensorContractionGetWorkspace(ref Handle handle, ref ContractDescription desc, ref ContractFind find, WorkSpacePreference pref, ref long workspaceSize);

		/// <summary>
		/// Initializes the contraction plan for a given tensor contraction problem
		/// </summary>
		/// <param name="handle">Opaque <see cref="Handle"/> holding cuTENSOR’s library context</param>
		/// <param name="plan">output <see cref="ContractPlan"/> holding the contraction execution plan (i.e., the candidate that will be executed as well as all it’s runtime parameters for the given tensor contraction problem)</param>
		/// <param name="desc">The <see cref="ContractDescription"/> filled with the information that encodes the tensor contraction problem</param>
		/// <param name="find">The <see cref="ContractFind"/> representing the candidate</param>
		/// <param name="workspaceSize">The workspace size (in bytes) that is required for the given tensor contraction</param>
		/// <returns><see cref="Status"/></returns>
		[DllImport(CUTENSOR_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cutensorInitContractionPlan(ref Handle handle, ref ContractPlan plan, ref ContractDescription desc, ref ContractFind find, long workspaceSize);

		/// <summary>
		/// This routine computes the tensor contraction <c>D = α * A · B + β * C</c>.
		/// </summary>
		/// <param name="handle">Opaque <see cref="Handle"/> holding cuTENSOR’s library context</param>
		/// <param name="plan">The <see cref="ContractPlan"/> holding the contraction execution plan</param>
		/// <param name="alpha">scalar α</param>
		/// <param name="A">tensor A</param>
		/// <param name="B">tensor B</param>
		/// <param name="beta">scalar β</param>
		/// <param name="C">tensor C</param>
		/// <param name="D">tensor D</param>
		/// <param name="workspace">The working buffer</param>
		/// <param name="workspaceSize">size of <paramref name="workspace"/></param>
		/// <param name="stream">The CUDA stream</param>
		/// <returns><see cref="Status"/></returns>
		internal delegate Status Contraction<T>(ref Handle handle, ref ContractPlan plan, ref T alpha, [In] IntPtr A, [In] IntPtr B, ref T beta, [In] IntPtr C, IntPtr D, IntPtr workspace, long workspaceSize, IntPtr stream);

		[DllImport(CUTENSOR_DLL_NAME, EntryPoint = "cutensorContraction")]
		//[NativeMethodBoundary]
		internal static extern Status cutensorContractionS(ref Handle handle, ref ContractPlan plan, ref float alpha, [In] IntPtr A, [In] IntPtr B, ref float beta, [In] IntPtr C, IntPtr D, IntPtr workspace, long workspaceSize, IntPtr stream);
		[DllImport(CUTENSOR_DLL_NAME, EntryPoint = "cutensorContraction")]
		//[NativeMethodBoundary]
		internal static extern Status cutensorContractionD(ref Handle handle, ref ContractPlan plan, ref double alpha, [In] IntPtr A, [In] IntPtr B, ref double beta, [In] IntPtr C, IntPtr D, IntPtr workspace, long workspaceSize, IntPtr stream);
		[DllImport(CUTENSOR_DLL_NAME, EntryPoint = "cutensorContraction")]
		//[NativeMethodBoundary]
		internal static extern Status cutensorContractionC(ref Handle handle, ref ContractPlan plan, ref FloatComplex alpha, [In] IntPtr A, [In] IntPtr B, ref FloatComplex beta, [In] IntPtr C, IntPtr D, IntPtr workspace, long workspaceSize, IntPtr stream);
		[DllImport(CUTENSOR_DLL_NAME, EntryPoint = "cutensorContraction")]
		//[NativeMethodBoundary]
		internal static extern Status cutensorContractionZ(ref Handle handle, ref ContractPlan plan, ref DoubleComplex alpha, [In] IntPtr A, [In] IntPtr B, ref DoubleComplex beta, [In] IntPtr C, IntPtr D, IntPtr workspace, long workspaceSize, IntPtr stream);
		#endregion

		#region reduction
		/// <summary>
		/// Determines the required workspaceSize for a given tensor reduction
		/// </summary>
		/// <param name="handle">Opaque <see cref="Handle"/> holding cuTENSOR’s library context</param>
		/// <param name="A">tensor A</param>
		/// <param name="descA"><see cref="TensorDescription"/> to the tensor <paramref name="A"/></param>
		/// <param name="modeA">The mode of <paramref name="A"/></param>
		/// <param name="C">tensor C</param>
		/// <param name="descC"><see cref="TensorDescription"/> to the tensor <paramref name="C"/></param>
		/// <param name="modeC">The mode of <paramref name="C"/></param>
		/// <param name="D">tensor D</param>
		/// <param name="descD"><see cref="TensorDescription"/> to the tensor <paramref name="D"/></param>
		/// <param name="modeD">The mode of <paramref name="D"/></param>
		/// <param name="opReduce">The <see cref="BinaryOperation"/> as reduction operation</param>
		/// <param name="typeCompute">The <see cref="ComputeType"/></param>
		/// <param name="workspaceSize">output work buffer size in bytes</param>
		/// <returns><see cref="Status"/></returns>
		[DllImport(CUTENSOR_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cutensorReductionGetWorkspace(ref Handle handle, [In] IntPtr A, ref TensorDescription descA, in int modeA, [In] IntPtr C, ref TensorDescription descC, in int modeC, [In] IntPtr D, ref TensorDescription descD, in int modeD, BinaryOperation opReduce, ComputeType typeCompute, ref long workspaceSize);

		/// <summary>
		/// Implements a tensor reduction of the form: <c><paramref name="D"/> = α * <paramref name="opReduce"/>(<paramref name="A"/>) + β * <paramref name="C"/></c>
		/// </summary>
		/// <param name="handle">Opaque <see cref="Handle"/> holding cuTENSOR’s library context</param>
		/// <param name="alpha">scalar α</param>
		/// <param name="beta">scalar β</param>
		/// <param name="A">tensor A</param>
		/// <param name="descA"><see cref="TensorDescription"/> to the tensor <paramref name="A"/></param>
		/// <param name="modeA">The mode of <paramref name="A"/></param>
		/// <param name="C">tensor C</param>
		/// <param name="descC"><see cref="TensorDescription"/> to the tensor <paramref name="C"/></param>
		/// <param name="modeC">The mode of <paramref name="C"/></param>
		/// <param name="D">tensor D</param>
		/// <param name="descD"><see cref="TensorDescription"/> to the tensor <paramref name="D"/></param>
		/// <param name="modeD">The mode of <paramref name="D"/></param>
		/// <param name="opReduce">The <see cref="BinaryOperation"/> as reduction operation</param>
		/// <param name="typeCompute">The <see cref="ComputeType"/></param>
		/// <param name="workspace">The working buffer array</param>
		/// <param name="workspaceSize">size of <paramref name="workspace"/></param>
		/// <param name="stream">The CUDA stream pointer</param>
		/// <returns><see cref="Status"/></returns>
		internal delegate Status Reduction<T>(ref Handle handle, ref T alpha, [In] IntPtr A, ref TensorDescription descA, in int modeA, ref T beta, [In] IntPtr C, ref TensorDescription descC, in int modeC, IntPtr D, ref TensorDescription descD, in int modeD, BinaryOperation opReduce, ComputeType typeCompute, IntPtr workspace, long workspaceSize, IntPtr stream);

		[DllImport(CUTENSOR_DLL_NAME, EntryPoint = "cutensorReduction")]
		//[NativeMethodBoundary]
		internal static extern Status cutensorReductionS(ref Handle handle, ref float alpha, [In] IntPtr A, ref TensorDescription descA, in int modeA, ref float beta, [In] IntPtr C, ref TensorDescription descC, in int modeC, IntPtr D, ref TensorDescription descD, in int modeD, BinaryOperation opReduce, ComputeType typeCompute, IntPtr workspace, long workspaceSize, IntPtr stream);
		[DllImport(CUTENSOR_DLL_NAME, EntryPoint = "cutensorReduction")]
		//[NativeMethodBoundary]
		internal static extern Status cutensorReductionD(ref Handle handle, ref double alpha, [In] IntPtr A, ref TensorDescription descA, in int modeA, ref double beta, [In] IntPtr C, ref TensorDescription descC, in int modeC, IntPtr D, ref TensorDescription descD, in int modeD, BinaryOperation opReduce, ComputeType typeCompute, IntPtr workspace, long workspaceSize, IntPtr stream);
		[DllImport(CUTENSOR_DLL_NAME, EntryPoint = "cutensorReduction")]
		//[NativeMethodBoundary]
		internal static extern Status cutensorReductionC(ref Handle handle, ref FloatComplex alpha, [In] IntPtr A, ref TensorDescription descA, in int modeA, ref FloatComplex beta, [In] IntPtr C, ref TensorDescription descC, in int modeC, IntPtr D, ref TensorDescription descD, in int modeD, BinaryOperation opReduce, ComputeType typeCompute, IntPtr workspace, long workspaceSize, IntPtr stream);
		[DllImport(CUTENSOR_DLL_NAME, EntryPoint = "cutensorReduction")]
		//[NativeMethodBoundary]
		internal static extern Status cutensorReductionZ(ref Handle handle, ref DoubleComplex alpha, [In] IntPtr A, ref TensorDescription descA, in int modeA, ref DoubleComplex beta, [In] IntPtr C, ref TensorDescription descC, in int modeC, IntPtr D, ref TensorDescription descD, in int modeD, BinaryOperation opReduce, ComputeType typeCompute, IntPtr workspace, long workspaceSize, IntPtr stream);
		#endregion
	}
}
