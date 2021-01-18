using System;
using System.Runtime.InteropServices;

using Althea.Linq;


namespace Althea.Tensor.Cuda
{
	#region converter
	internal static class Converter
	{
		internal static ComputeType ToComputeType(this DataType type)
		{
			return type switch
			{
				DataType.RealInt16 => ComputeType.SignedShort,
				DataType.RealInt32 => ComputeType.SignedInteger,
				DataType.RealUInt16 => ComputeType.UnsignedShort,
				DataType.RealUInt32 => ComputeType.UnsignedInteger,
				DataType.RealSingle => ComputeType.RealSingle,
				DataType.RealDouble => ComputeType.RealDouble,
				DataType.ComplexSingle => ComputeType.ComplexSingle,
				DataType.ComplexDouble => ComputeType.ComplexDouble,
				_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
			};
		}
	}
	#endregion

	#region enum
	/// <summary>
	/// Encodes cuTENSOR’s compute type
	/// </summary>
	[Flags]
	public enum ComputeType
	{
		/// <summary>
		/// real as a half
		/// </summary>
		RealHalf = 1 << 0,
		/// <summary>
		/// complex as a half
		/// </summary>
		ComplexHalf = 1 << 1,
		/// <summary>
		/// real as a float
		/// </summary>
		RealSingle = 1 << 2,
		/// <summary>
		/// complex as a float
		/// </summary>
		ComplexSingle = 1 << 3,
		/// <summary>
		/// real as a double
		/// </summary>
		RealDouble = 1 << 4,
		/// <summary>
		/// complex as a double
		/// </summary>
		ComplexDouble = 1 << 5,
		/// <summary>
		/// real as a uint8
		/// </summary>
		UnsignedShort = 1 << 6,
		/// <summary>
		/// real as a uint32
		/// </summary>
		UnsignedInteger = 1 << 7,
		/// <summary>
		/// real as a int8
		/// </summary>
		SignedShort = 1 << 8,
		/// <summary>
		/// real as a int32
		/// </summary>
		SignedInteger = 1 << 9
	}

	/// <summary>
	/// The cuTENSOR status type returns
	/// </summary>
	public enum Status
	{
		/// <summary>
		/// The operation completed successfully.
		/// </summary>
		Success = 0,
		/// <summary>
		/// The cuTENSOR library was not initialized.
		/// </summary>
		NotInitialized = 1,
		/// <summary>
		/// Resource allocation failed inside the cuTENSOR library.
		/// </summary>
		AllocFailed = 3,
		/// <summary>
		/// An unsupported value or parameter was passed to the function (indicates an user error).
		/// </summary>
		InvalidValue = 7,
		/// <summary>
		/// Indicates that the device is either not ready, or the target architecture is not supported.
		/// </summary>
		ArchMismatch = 8,
		/// <summary>
		/// An access to GPU memory space failed, which is usually caused by a failure to bind a texture.
		/// </summary>
		MappingError = 11,
		/// <summary>
		/// The GPU program failed to execute. This is often caused by a launch failure of the kernel on the GPU, which can be caused by multiple reasons.
		/// </summary>
		ExecutionFailed = 13,
		/// <summary>
		/// An internal cuTENSOR error has occurred.
		/// </summary>
		InternalError = 14,
		/// <summary>
		/// The requested operation is not supported.
		/// </summary>
		NotSupported = 15,
		/// <summary>
		/// The functionality requested requires some license and an error was detected when trying to check the current licensing.
		/// </summary>
		LicenseError = 16,
		/// <summary>
		/// A call to CUBLAS did not succeed.
		/// </summary>
		CublasError = 17,
		/// <summary>
		/// Some unknown CUDA error has occurred.
		/// </summary>
		CudaError = 18,
		/// <summary>
		/// The provided workspace was insufficient.
		/// </summary>
		InsufficientWorkspace = 19,
		/// <summary>
		/// Indicates that the driver version is insufficient.
		/// </summary>
		InsufficientDriver = 20
	}

	/// <summary>
	/// This enum gives users finer control over which algorithm should be executed by tensor contraction. Values >= 0 correspond to certain sub-algorithms of <see cref="GETT"/>.
	/// </summary>
	public enum ContractionAlgorithm
	{
		/// <summary>
		/// Choose the GETT algorithm
		/// </summary>
		GETT = -4,
		/// <summary>
		/// Transpose (A or B) + GETT
		/// </summary>
		TGETT = -3,
		/// <summary>
		/// Transpose-Transpose-GEMM-Transpose (requires additional memory)
		/// </summary>
		TTGT = -2,
		/// <summary>
		/// Lets the internal heuristic choose
		/// </summary>
		Default = -1
	}

	/// <summary>
	/// The work space preference used by tensor contraction.
	/// </summary>
	public enum WorkSpacePreference
	{
		/// <summary>
		/// At least one algorithm will be available
		/// </summary>
		Minimum = 1,
		/// <summary>
		/// The most suitable algorithm will be available
		/// </summary>
		Recommended = 2,
		/// <summary>
		/// All algorithms will be available
		/// </summary>
		Maximum = 3,
	}
	#endregion

	#region struct
	internal static class Constants
	{
		internal const int CUTENSOR_HANDLE_SIZE = 512;

		internal const int CUTENSOR_TENSOR_SIZE = 64;

		internal const int CUTENSOR_CONTRACT_SIZE = 256;

		internal const int CUTENSOR_PLAN_SIZE = 640;

		internal const int CUTENSOR_FIND_SIZE = 64;
	}

#pragma warning disable CA1066, CA1815 // no need to override equals

	/// <summary>
	/// The cuTENSOR's handle struct
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public readonly struct Handle
	{
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.CUTENSOR_HANDLE_SIZE * 2)]
		private readonly int[] unmanagedArray;
	}

	/// <summary>
	/// The cuTENSOR's tensor descriptor struct
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public readonly struct TensorDescription
	{
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.CUTENSOR_TENSOR_SIZE * 2)]
		private readonly int[] unmanagedArray;
		
		/// <summary>
		/// Create a <see cref="TensorDescription"/>
		/// </summary>
		/// <typeparam name="T">see <see cref="Storage.Storage{T}"/> for supported data types</typeparam>
		/// <param name="handle">the CUDA Tensor library handle</param>
		/// <param name="op">the <see cref="UnitaryOperation"/> to the tensor in the following computation, default identity</param>
		/// <param name="size">size/extent of each dimension of the tensor</param>
		/// <param name="stride">stride of each dimension of the tensor, default null means that all strides are one</param>
		/// <returns>the created <see cref="TensorDescription"/></returns>
		public static TensorDescription Create<T>(Handle handle, long[] size, UnitaryOperation op = UnitaryOperation.Identity, long[] stride = null) where T : struct, IComparable<T>
		{
			if (size is null)
				throw new ArgumentNullException(nameof(size));
			if (!(stride is null) && stride.Length != size.Length)
				throw new ArgumentNullException(nameof(stride));

			var type = default(T).ToDataType().ToCudaDataType();
			var descr = new TensorDescription();
			NativeMethods.cutensorInitTensorDescriptor(ref handle, ref descr, (uint)size.Length, size, stride, type, op).Check();
			return descr;
		}

		/// <summary>
		/// Return the string representation
		/// </summary>
		/// <returns>the string representation of guessed underlying structure which is hidden and not defined in header filed of CuTensor</returns>
		public override string ToString()
		{
			var p = this.unmanagedArray;
			var datatype = (CudaDataType)p[1];
			var rank = p[0];
			var op = (UnitaryOperation)p[34];
			int[] size = p[2..(2 + rank)], stride = p[18..(18 + rank)];
			return $"TensorDescription[data_type={datatype}, rank={rank}, unitary_operation={op}, size={string.Join("x", size)}, stride={string.Join(",", stride)}]";
		}
	}

	/// <summary>
	/// The cuTENSOR's tensor contraction descriptor struct
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public readonly struct ContractDescription : ICloneable, IEquatable<ContractDescription>
	{
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.CUTENSOR_TENSOR_SIZE * 2)]
		private readonly int[] unmanagedArray;

		/// <summary>
		/// Determines whether this == <paramref name="other"/>
		/// </summary>
		/// <param name="other">the other <see cref="ContractDescription"/></param>
		/// <returns>equals or not</returns>
		public bool Equals(ContractDescription other)
		{
			return this.unmanagedArray.SequenceEqual(other.unmanagedArray);
		}

		/// <summary>
		/// Determines whether this == <paramref name="obj"/>
		/// </summary>
		/// <param name="obj">the other <see cref="object"/></param>
		/// <returns>equals or not</returns>
		public override bool Equals(object obj)
		{
			if (obj is ContractDescription c)
				return this.Equals(c);
			return false;
		}

		/// <summary>
		/// Returns the hash code for this instance.
		/// </summary>
		/// <returns>the hash code</returns>
		public override int GetHashCode()
		{
			return this.unmanagedArray.HashCodeOfArray();
		}

		/// <summary>
		/// Clone this object
		/// </summary>
		/// <returns></returns>
		public object Clone()
		{
			var newOne = new ContractDescription();
			Array.Copy(this.unmanagedArray, newOne.unmanagedArray, this.unmanagedArray.Length);
			return newOne;
		}

		/// <summary>
		/// Equals operator
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator ==(ContractDescription left, ContractDescription right)
		{
			return left.Equals(right);
		}

		/// <summary>
		/// Not-equals operator
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator !=(ContractDescription left, ContractDescription right)
		{
			return !(left == right);
		}
	}

	/// <summary>
	/// The cuTENSOR's tensor contraction plan struct
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public readonly struct ContractPlan : ICloneable, IDisposable
	{
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.CUTENSOR_TENSOR_SIZE * 2)]
		private readonly int[] unmanagedArray;

		internal readonly long workSize;

		internal ContractPlan(long workSize)
		{
			this.workSize = workSize;
			this.unmanagedArray = new int[Constants.CUTENSOR_TENSOR_SIZE * 2];
		}

		/// <summary>
		/// Clone this object
		/// </summary>
		/// <returns></returns>
		public object Clone()
		{
			var newOne = new ContractPlan(this.workSize);
			Array.Copy(this.unmanagedArray, newOne.unmanagedArray, this.unmanagedArray.Length);
			return newOne;
		}

		/// <summary>
		/// Dispose unmanaged resources
		/// </summary>
		public void Dispose()
		{
			// do nothing
		}
	}

	/// <summary>
	/// The cuTENSOR's tensor contraction plan struct
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public readonly struct ContractFind
	{
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.CUTENSOR_TENSOR_SIZE * 2)]
		private readonly int[] unmanagedArray;
	}

#pragma warning restore CA1815, CA1066
	#endregion
}
