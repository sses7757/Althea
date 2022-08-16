using System.Runtime.InteropServices;


namespace Althea.Backend.Cuda.TensorAlgebra.Dense
{
	/// <summary>
	/// The CUDA Tensor library native methods
	/// </summary>
	public static unsafe partial class NativeMethods
	{
		#region initialize
		[DllImport(Cuda.NativeMethods.CUTENSOR_DLL_NAME)]
		internal static extern CudaTensorStatus cutensorInit(out CudaTensorHandle handle);

		[DllImport(Cuda.NativeMethods.CUTENSOR_DLL_NAME)]
		internal static extern CudaTensorStatus cutensorInitTensorDescriptor(in CudaTensorHandle handle, out TensorDescription desc, int numModes, in long extent, in long stride, CudaDataType dataType, UnaryOperation unaryOp);

		[DllImport(Cuda.NativeMethods.CUTENSOR_DLL_NAME)]
		internal static extern CudaTensorStatus cutensorGetAlignmentRequirement(in CudaTensorHandle handle, void* ptr, in TensorDescription desc, out int alignmentRequirement);
		#endregion

		#region permute
		[LibraryImport(Cuda.NativeMethods.CUTENSOR_DLL_NAME)]
		internal static partial CudaTensorStatus cutensorPermutation(in CudaTensorHandle handle,
			void* alpha, void* A, in TensorDescription descA, Span<int> modeA,
			void* B, in TensorDescription descB, Span<int> modeB,
			CudaDataType type, void* stream);
		#endregion

		#region point-wise
		[LibraryImport(Cuda.NativeMethods.CUTENSOR_DLL_NAME)]
		internal static partial CudaTensorStatus cutensorElementwiseBinary(in CudaTensorHandle handle,
			void* alpha, void* A, in TensorDescription descA, Span<int> modeA,
			void* gamma, void* C, in TensorDescription descC, Span<int> modeC,
			void* D, in TensorDescription descD, Span<int> modeD,
			BinaryOperation opAC, CudaDataType typeScalar, void* stream);

		[LibraryImport(Cuda.NativeMethods.CUTENSOR_DLL_NAME)]
		internal static partial CudaTensorStatus cutensorElementwiseTrinary(in CudaTensorHandle handle,
			void* alpha, void* A, in TensorDescription descA, Span<int> modeA,
			void* beta, void* B, in TensorDescription descB, Span<int> modeB,
			void* gamma, void* C, in TensorDescription descC, Span<int> modeC,
			void* D, in TensorDescription descD, Span<int> modeD,
			BinaryOperation opAB, BinaryOperation opABC, CudaDataType typeScalar, void* stream);
		#endregion

		#region contraction
		[LibraryImport(Cuda.NativeMethods.CUTENSOR_DLL_NAME)]
		internal static partial CudaTensorStatus cutensorInitContractionDescriptor(in CudaTensorHandle handle, out ContractDescription desc,
			in TensorDescription descA, Span<int> modeA, int alignmentRequirementA,
			in TensorDescription descB, Span<int> modeB, int alignmentRequirementB,
			in TensorDescription descC, Span<int> modeC, int alignmentRequirementC,
			in TensorDescription descD, Span<int> modeD, int alignmentRequirementD, ComputeType computeType);

		[DllImport(Cuda.NativeMethods.CUTENSOR_DLL_NAME)]
		internal static extern CudaTensorStatus cutensorInitContractionFind(in CudaTensorHandle handle, out ContractFind find, ContractionAlgorithm algo);

		[DllImport(Cuda.NativeMethods.CUTENSOR_DLL_NAME)]
		internal static extern CudaTensorStatus cutensorContractionGetWorkspace(in CudaTensorHandle handle, in ContractDescription desc, in ContractFind find, WorkSpacePreference pref, out long workspaceSize);

		[DllImport(Cuda.NativeMethods.CUTENSOR_DLL_NAME)]
		internal static extern CudaTensorStatus cutensorInitContractionPlan(in CudaTensorHandle handle, out ContractPlan plan, in ContractDescription desc, in ContractFind find, long workspaceSize);

		[LibraryImport(Cuda.NativeMethods.CUTENSOR_DLL_NAME)]
		internal static partial CudaTensorStatus cutensorContraction(in CudaTensorHandle handle, in ContractPlan plan,
			void* alpha, void* A, void* B,
			void* beta, void* C, void* D,
			void* workspace, long workspaceSize, void* stream);
		#endregion

		#region reduction
		[LibraryImport(Cuda.NativeMethods.CUTENSOR_DLL_NAME)]
		internal static partial CudaTensorStatus cutensorReductionGetWorkspace(in CudaTensorHandle handle,
			void* A, in TensorDescription descA, Span<int> modeA,
			void* C, in TensorDescription descC, Span<int> modeC,
			void* D, in TensorDescription descD, Span<int> modeD,
			BinaryOperation opReduce, ComputeType typeCompute, out long workspaceSize);

		[LibraryImport(Cuda.NativeMethods.CUTENSOR_DLL_NAME)]
		internal static partial CudaTensorStatus cutensorReduction(in CudaTensorHandle handle,
			void* alpha, void* A, in TensorDescription descA, Span<int> modeA,
			void* beta, void* C, in TensorDescription descC, Span<int> modeC,
			void* D, in TensorDescription descD, Span<int> modeD,
			BinaryOperation opReduce, ComputeType typeCompute, void* workspace, long workspaceSize, void* stream);
		#endregion
	}
}
