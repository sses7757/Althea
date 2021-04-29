using System;
using System.Runtime.InteropServices;


#pragma warning disable IDE1006 // 命名样式
namespace Althea.Backend.Cuda.TensorAlgebra.Dense
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

		#region initialize
		[DllImport(CUTENSOR_DLL_NAME)]
		internal static extern CudaTensorStatus cutensorInit(ref byte handle);

		[DllImport(CUTENSOR_DLL_NAME)]
		internal static extern CudaTensorStatus cutensorInitTensorDescriptor(CudaTensorHandle handle, out TensorDescription desc, int numModes, in long extent, in long stride, CudaDataType dataType, UnaryOperation unaryOp);

		[DllImport(CUTENSOR_DLL_NAME)]
		internal static extern CudaTensorStatus cutensorGetAlignmentRequirement(CudaTensorHandle handle, IntPtr ptr, in TensorDescription desc, out int alignmentRequirement);
		#endregion

		#region permute
		[DllImport(CUTENSOR_DLL_NAME)]
		internal static unsafe extern CudaTensorStatus cutensorPermutation(CudaTensorHandle handle, void* alpha, IntPtr A, in TensorDescription descA, in int modeA, IntPtr B, in TensorDescription descB, in int modeB, CudaDataType type, IntPtr stream);
		#endregion

		#region point-wise
		[DllImport(CUTENSOR_DLL_NAME)]
		internal static unsafe extern CudaTensorStatus cutensorElementwiseBinary(CudaTensorHandle handle, void* alpha, IntPtr A, in TensorDescription descA, in int modeA, void* gamma, IntPtr C, in TensorDescription descC, in int modeC, IntPtr D, in TensorDescription descD, in int modeD, BinaryOperation opAC, CudaDataType typeScalar, IntPtr stream);

		[DllImport(CUTENSOR_DLL_NAME)]
		internal static unsafe extern CudaTensorStatus cutensorElementwiseTrinary(CudaTensorHandle handle, void* alpha, IntPtr A, in TensorDescription descA, in int modeA, void* beta, IntPtr B, in TensorDescription descB, in int modeB, void* gamma, IntPtr C, in TensorDescription descC, in int modeC, IntPtr D, in TensorDescription descD, in int modeD, BinaryOperation opAB, BinaryOperation opABC, CudaDataType typeScalar, IntPtr stream);
		#endregion

		#region contraction
		[DllImport(CUTENSOR_DLL_NAME)]
		internal static extern CudaTensorStatus cutensorInitContractionDescriptor(CudaTensorHandle handle, out ContractDescription desc,
			in TensorDescription descA, in int modeA, int alignmentRequirementA,
			in TensorDescription descB, in int modeB, int alignmentRequirementB,
			in TensorDescription descC, in int modeC, int alignmentRequirementC,
			in TensorDescription descD, in int modeD, int alignmentRequirementD, ComputeType computeType);

		[DllImport(CUTENSOR_DLL_NAME)]
		internal static extern CudaTensorStatus cutensorInitContractionFind(CudaTensorHandle handle, out ContractFind find, ContractionAlgorithm algo);

		[DllImport(CUTENSOR_DLL_NAME)]
		internal static extern CudaTensorStatus cutensorContractionGetWorkspace(CudaTensorHandle handle, in ContractDescription desc, in ContractFind find, WorkSpacePreference pref, out long workspaceSize);

		[DllImport(CUTENSOR_DLL_NAME)]
		internal static extern CudaTensorStatus cutensorInitContractionPlan(CudaTensorHandle handle, out ContractPlan plan, in ContractDescription desc, in ContractFind find, long workspaceSize);

		[DllImport(CUTENSOR_DLL_NAME)]
		internal static unsafe extern CudaTensorStatus cutensorContraction(CudaTensorHandle handle, in ContractPlan plan, void* alpha, IntPtr A, IntPtr B, void* beta, IntPtr C, IntPtr D, IntPtr workspace, long workspaceSize, IntPtr stream);
		#endregion

		#region reduction
		[DllImport(CUTENSOR_DLL_NAME)]
		internal static extern CudaTensorStatus cutensorReductionGetWorkspace(CudaTensorHandle handle, IntPtr A, in TensorDescription descA, in int modeA, IntPtr C, in TensorDescription descC, in int modeC, IntPtr D, in TensorDescription descD, in int modeD, BinaryOperation opReduce, ComputeType typeCompute, out long workspaceSize);

		[DllImport(CUTENSOR_DLL_NAME)]
		internal static unsafe extern CudaTensorStatus cutensorReduction(CudaTensorHandle handle, void* alpha, IntPtr A, in TensorDescription descA, in int modeA, void* beta, IntPtr C, in TensorDescription descC, in int modeC, IntPtr D, in TensorDescription descD, in int modeD, BinaryOperation opReduce, ComputeType typeCompute, IntPtr workspace, long workspaceSize, IntPtr stream);
		#endregion
	}
}
