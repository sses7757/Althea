using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Althea.NativeTypes;
using Althea.TensorAlgebra;
using Althea.TensorAlgebra.Dense;

using Microsoft.VisualBasic;

namespace Althea.Backend.Cuda.TensorAlgebra.Dense
{
	#region compute type
	/// <summary>
	/// This enum encodes the cuTENSOR’s compute type
	/// </summary>
	[Flags]
	public enum ComputeType
	{
		/// <summary>
		/// <see cref="DataTypeClassification.FloatPoint_IEEE754"/> binary-16 floating point number (a.k.a. <see cref="System.Half"/>)
		/// </summary>
		Half = 1 << 0,
		/// <summary>
		/// <see cref="BrainFloatConst.BrainFloat"/> binary-16 floating point number (a.k.a. <see cref="Cuda.BrainHalf"/>)
		/// </summary>
		BrainHalf = 1 << 10,
		/// <summary>
		/// The floating point number with 8-bit exponent and 10-bit mantissa, not supported in current implementation
		/// </summary>
		TensorFloat32 = 1 << 3,
		/// <summary>
		/// <see cref="DataTypeClassification.FloatPoint_IEEE754"/> binary-32 floating point number (a.k.a. <see cref="float"/>) 
		/// </summary>
		Single = 1 << 2,
		/// <summary>
		/// <see cref="DataTypeClassification.FloatPoint_IEEE754"/> binary-64 floating point number (a.k.a. <see cref="Double"/>) 
		/// </summary>
		Double = 1 << 4,
		/// <summary>
		/// The 8-bit unsigned integer (a.k.a. <see cref="byte"/>)
		/// </summary>
		UnsignedByte = 1 << 6,
		/// <summary>
		/// The 8-bit signed integer (a.k.a. <see cref="sbyte"/>)
		/// </summary>
		SignedByte = 1 << 8,
		/// <summary>
		/// The 32-bit unsigned integer (a.k.a. <see cref="uint"/>)
		/// </summary>
		UnsignedInteger = 1 << 7,
		/// <summary>
		/// The 32-bit signed integer (a.k.a. <see cref="int"/>)
		/// </summary>
		SignedInteger = 1 << 9
	}

	internal static partial class Converter
	{
		internal static ComputeType ToComputeType(this DataType type)
		{
			return type switch
			{
				DataType.RealInt8 or DataType.ComplexInt8 => ComputeType.SignedByte,
				DataType.RealInt32 or DataType.ComplexInt32 => ComputeType.SignedInteger,
				DataType.RealUInt8 or DataType.ComplexUInt8 => ComputeType.UnsignedByte,
				DataType.RealUInt32 or DataType.ComplexUInt32 => ComputeType.UnsignedInteger,
				DataType.RealSingle or DataType.ComplexSingle => ComputeType.Single,
				DataType.RealDouble or DataType.ComplexDouble => ComputeType.Double,
				DataType.RealHalf or DataType.ComplexHalf => ComputeType.Half,
				BrainFloatConst.RealBrainFloat16 or BrainFloatConst.ComplexBrainFloat16 => ComputeType.BrainHalf,
				// TensorFloat32 not supported
				_ => 0,
			};
		}
	}
	#endregion

	#region error
	/// <summary>
	/// The returns status of cuTENSOR API calls
	/// </summary>
	public enum CudaTensorStatus
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
		InsufficientDriver = 20,
		/// <summary>
		/// Indicates an error related to file I/O.
		/// </summary>
		IOError = 21,
	}

	/// <summary>
	/// The static class containing extension methods for <see cref="CudaTensorStatus"/> and <see cref="CudaTensorStatus"/>
	/// </summary>
	public static partial class StatusExtension
	{
		/// <summary>
		/// Check whether the input <see cref="CudaTensorStatus"/> is success or not and throw exception if it is not
		/// </summary>
		/// <param name="err">The <see cref="CudaTensorStatus"/> to be checked</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Check(this CudaTensorStatus err)
		{
			if (err != CudaTensorStatus.Success)
			{
				throw new StatusException(err, new StackTrace(0));
			}
		}
	}
	#endregion

	#region other enum
	/// <summary>
	/// This enum gives users finer control over which algorithm should be executed by tensor contraction. Values >= 0 correspond to certain sub-algorithms of <see cref="GETT"/>.
	/// </summary>
	internal enum ContractionAlgorithm
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
	internal enum WorkSpacePreference
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

	#region operations
	/// <summary>
	/// Binary operations supported by tensor point-wise operations
	/// </summary>
	internal enum BinaryOperation
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
	/// Unary operations supported by tensor point-wise operations
	/// </summary>
	internal enum UnaryOperation
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

	internal static partial class Converter
	{
		internal static BinaryOperation ToCudaOp(this Althea.TensorAlgebra.BinaryOperation op)
		{
			return op switch
			{
				Althea.TensorAlgebra.BinaryOperation.Addition => BinaryOperation.Add,
				Althea.TensorAlgebra.BinaryOperation.Multiply => BinaryOperation.Mul,
				Althea.TensorAlgebra.BinaryOperation.Maximum => BinaryOperation.Max,
				Althea.TensorAlgebra.BinaryOperation.Mininum => BinaryOperation.Min,
				_ => 0,
			};
		}

		internal static UnaryOperation ToCudaOp(this Althea.TensorAlgebra.UnaryOperation op)
		{
			return op switch
			{
				Althea.TensorAlgebra.UnaryOperation.Identity => UnaryOperation.Identity,
				Althea.TensorAlgebra.UnaryOperation.Conjugate => UnaryOperation.Conjugate,
				Althea.TensorAlgebra.UnaryOperation.Negate => UnaryOperation.Negate,
				_ => 0,
			};
		}
	}
	#endregion

	#region wrapper
	/// <summary>
	/// The cuTENSOR's handle wrapper
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	internal class CudaTensorHandle
	{
		[StructLayout(LayoutKind.Sequential)]
		private unsafe struct Long512
		{
			internal fixed long data[512];
		}


	}

	#endregion
}
