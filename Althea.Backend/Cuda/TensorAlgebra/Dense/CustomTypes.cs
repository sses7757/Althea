using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Althea.Linq;
using Althea.Helpers;
using Althea.NativeTypes;
using Althea.TensorAlgebra;
using Althea.TensorAlgebra.Dense;
using Althea.Backend.Cuda.TensorAlgebra.Dense;


namespace Althea.Backend.Cuda
{
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
}

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
		internal static ComputeType ToComputeType(this CudaDataType type)
		{
			return type switch
			{
				CudaDataType.RealInt8 or CudaDataType.ComplexInt8 => ComputeType.SignedByte,
				CudaDataType.RealInt32 or CudaDataType.ComplexInt32 => ComputeType.SignedInteger,
				CudaDataType.RealUInt8 or CudaDataType.ComplexUInt8 => ComputeType.UnsignedByte,
				CudaDataType.RealUInt32 or CudaDataType.ComplexUInt32 => ComputeType.UnsignedInteger,
				CudaDataType.RealFloat32 or CudaDataType.ComplexFloat32 => ComputeType.Single,
				CudaDataType.RealFloat64 or CudaDataType.ComplexFloat64 => ComputeType.Double,
				CudaDataType.RealFloat16 or CudaDataType.ComplexFloat16 => ComputeType.Half,
				CudaDataType.RealBrainFloat16 or CudaDataType.ComplexBrainFloat16 => ComputeType.BrainHalf,
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
		/// The requested operation under the given combination of data types is not supported.
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
	#endregion

	#region other enum
	/// <summary>
	/// This enum gives users finer control over which algorithm should be executed by tensor contraction. Values >= 0 correspond to certain sub-algorithms of <see cref="GETT"/>.
	/// </summary>
	public enum ContractionAlgorithm
	{
		/// <summary>
		/// Let the internal heuristic choose among all GETT algorithms
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
		/// Lets the internal heuristic choose among all algorithms
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
	/// Unary operations supported by tensor point-wise operations
	/// </summary>
	public enum UnaryOperation
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
	[StructLayout(LayoutKind.Sequential, Size = 8 * 512)]
	internal sealed class CudaTensorHandle
	{
		private readonly long data;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public CudaTensorHandle()
		{
			this.data = default;
			NativeMethods.cutensorInit(ref Unsafe.As<long, byte>(ref this.data)).Check();
		}
	}

	/// <summary>
	/// The structure for the CUDA Tensor library's tensor descriptor
	/// </summary>
	[StructLayout(LayoutKind.Explicit, Size = 8 * 64)]
	internal readonly struct TensorDescription
	{
		[FieldOffset(4 * 4)]
		private readonly int rank;

		[FieldOffset(5 * 4)]
		internal readonly CudaDataType dataType;

		[FieldOffset(6 * 4)]
		private readonly FixedBuffer_64<int> size;

		[FieldOffset(22 * 4)]
		private readonly FixedBuffer_64<int> strides;

		[FieldOffset(38 * 4)]
		private readonly UnaryOperation operation;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Create<T>(CudaTensorHandle handle, DenseTensorWrapper<T> tensor, out TensorDescription descr) where T : unmanaged, INumber<T>
		{
			var dataType = Const<T>.DataType.ToCudaDataType();
			var op = tensor.Operation.ToCudaOp();
			if (tensor.IsInvalid() || op == 0)
			{
				descr = default; return false;
			}
			CudaTensorStatus err;
			if (tensor.Strides.IsEmpty)
			{
				err = NativeMethods.cutensorInitTensorDescriptor(handle, out descr, tensor.Rank, in tensor.Size[0], in Unsafe.NullRef<long>(), dataType, op);
			}
			else
			{
				err = NativeMethods.cutensorInitTensorDescriptor(handle, out descr, tensor.Rank, in tensor.Size[0], in tensor.Strides[0], dataType, op);
			}
			if (err == CudaTensorStatus.NotSupported || err == CudaTensorStatus.InvalidValue)
				return false;
			err.Check();
			return true;
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static TensorDescription Create<T>(CudaTensorHandle handle, DenseTensorWrapper<T> tensor, CudaDataType dataType) where T : unmanaged, INumber<T>
		{
			TensorDescription descr;
			if (tensor.Strides.IsEmpty)
			{
				NativeMethods.cutensorInitTensorDescriptor(handle, out descr, tensor.Rank, in tensor.Size[0], in Unsafe.NullRef<long>(), dataType, tensor.Operation.ToCudaOp()).Check();
			}
			else
			{
				NativeMethods.cutensorInitTensorDescriptor(handle, out descr, tensor.Rank, in tensor.Size[0], in tensor.Strides[0], dataType, tensor.Operation.ToCudaOp()).Check();
			}
			return descr;
		}

		// The string representation of the <b>guessed</b> underlying structure
		public override string ToString()
		{
			return nameof(TensorDescription) + $"[DataType={this.dataType}, Rank={this.rank}, Operation={this.operation}, Size={this.size.AsSpan(this.rank).SpanJoin('x')}, Strides={{{this.strides.AsSpan(this.rank).SpanJoin(',')}}}]";
		}
	}

	/// <summary>
	/// The structure for the CUDA Tensor library's contraction descriptor
	/// </summary>
	[StructLayout(LayoutKind.Explicit, Size = 8 * 256)]
	internal readonly struct ContractDescription
	{
		[FieldOffset(4 * 4)]
		private readonly TensorDescription descrA;

		[FieldOffset(46 * 4)]
		private readonly TensorDescription descrB;

		[FieldOffset(88 * 4)]
		private readonly TensorDescription descrCD;

		[FieldOffset(178 * 4)]
		private readonly int alignA;
		[FieldOffset(179 * 4)]
		private readonly int alignB;
		[FieldOffset(180 * 4)]
		private readonly int alignC;
		[FieldOffset(181 * 4)]
		private readonly int alignD;

		// return supported or not
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Create<T>(CudaTensorHandle handle, DenseTensorWrapper<T> A, DenseTensorWrapper<T> B, DenseTensorWrapper<T> C, DenseTensorWrapper<T> D, TensorContractInfo info, out ContractDescription descr, ComputeType computeType = 0) where T : unmanaged, INumber<T>
		{
			descr = default;
			if (info.IsInvalid())
				return false;
			IntPtr	pA = DenseApi.GetPointer(A.ValueStorage), pB = DenseApi.GetPointer(B.ValueStorage),
					pC = DenseApi.GetPointer(C.ValueStorage), pD = DenseApi.GetPointer(D.ValueStorage);
			if (pA == default || pB == default || pC == default || pD == default)
				return false;
			if (!C.SizeEquals(D))
				return false;

			var dataType = Const<T>.DataType.ToCudaDataType();
			var descrA = TensorDescription.Create(handle, A, dataType);
			var descrB = TensorDescription.Create(handle, B, dataType);
			var descrC = TensorDescription.Create(handle, C, dataType);

			NativeMethods.cutensorGetAlignmentRequirement(handle, pA, in descrA, out int alignA).Check();
			NativeMethods.cutensorGetAlignmentRequirement(handle, pB, in descrB, out int alignB).Check();
			NativeMethods.cutensorGetAlignmentRequirement(handle, pC, in descrC, out int alignC).Check();
			NativeMethods.cutensorGetAlignmentRequirement(handle, pD, in descrC, out int alignD).Check();

			Span<char> labelA = stackalloc char[A.Rank], labelB = stackalloc char[B.Rank], labelC = stackalloc char[C.Rank];
			info.GetLabels(ref labelA, ref labelB, ref labelC);
			Span<int> modeA = stackalloc int[A.Rank], modeB = stackalloc int[B.Rank], modeC = stackalloc int[C.Rank];
			labelA.CopyTo(modeA, static c => c); labelB.CopyTo(modeB, static c => c); labelC.CopyTo(modeC, static c => c);

			if (computeType == 0)
				computeType = dataType.ToComputeType();

			var err = NativeMethods.cutensorInitContractionDescriptor(handle, out descr,
				in descrA, in modeA[0], alignA, in descrB, in modeB[0], alignB,
				in descrC, in modeC[0], alignC, in descrC, in modeC[0], alignD, computeType);
			if (err == CudaTensorStatus.NotSupported &&
				(computeType == ComputeType.Half || computeType == ComputeType.BrainHalf || computeType == ComputeType.TensorFloat32))
			{	// try again using float32 as the computation type
				computeType = ComputeType.Single;
				err = NativeMethods.cutensorInitContractionDescriptor(handle, out descr,
					in descrA, in modeA[0], alignA, in descrB, in modeB[0], alignB,
					in descrC, in modeC[0], alignC, in descrC, in modeC[0], alignD, computeType);
			}
			if (err == CudaTensorStatus.InvalidValue || err == CudaTensorStatus.NotSupported)
				return false;
			else
				err.Check();
			return true;
		}

		// The string representation of the <b>guessed</b> underlying structure
		public override string ToString()
		{
			return nameof(ContractDescription) + $"[AlignmentsABCD={{{this.alignA}, {this.alignB}, {this.alignC}, {this.alignD}}}, DescriptionA={this.descrA}, DescriptionB={this.descrB}, DescriptionCD={this.descrCD}]";
		}
	}

	/// <summary>
	/// The structure for the CUDA Tensor library's contraction algorithm
	/// </summary>
	[StructLayout(LayoutKind.Explicit, Size = 8 * 64)]
	internal readonly struct ContractFind : IEquatable<ContractFind>
	{
		[FieldOffset(4 * 4)]
		internal readonly ContractionAlgorithm algorithm;

		[FieldOffset(5 * 4)]
		internal readonly int GETTSpecificAlgorithm;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ContractFind(CudaTensorHandle handle, ContractionAlgorithm algorithm)
		{
			NativeMethods.cutensorInitContractionFind(handle, out this, algorithm).Check();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool TryCreate(CudaTensorHandle handle, ContractionAlgorithm algorithm, out ContractFind find)
		{
			return NativeMethods.cutensorInitContractionFind(handle, out find, algorithm) == CudaTensorStatus.Success;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe bool Equals(ContractFind other)
		{
			fixed (void* t = &this)
			{
				return new ReadOnlySpan<byte>(t, 8 * 64).SequenceEqual(new ReadOnlySpan<byte>(&other, 8 * 64));
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override bool Equals(object? obj)
		{
			return obj is ContractFind find && this.Equals(find);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe override int GetHashCode()
		{
			fixed (void* t = &this)
			{
				return new ReadOnlySpan<int>(t, 2 * 64).HashCodeOfSpan();
			}
		}

		// The string representation of the <b>guessed</b> underlying structure
		public override string ToString()
		{
			return nameof(ContractFind) + $"[Algorithm={this.algorithm}" + (this.GETTSpecificAlgorithm < 0 ? "]" : $", SpecificAlgorithm={this.GETTSpecificAlgorithm}]");
		}
	}

	/// <summary>
	/// The structure for the CUDA Tensor library's final contraction plan
	/// </summary>
	[StructLayout(LayoutKind.Explicit, Size = 8 * 640)]
	internal readonly struct ContractPlan
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Create(CudaTensorHandle handle, in ContractDescription desc, in ContractFind find, out ContractPlan plan, out long workspaceSize)
		{
			var err = NativeMethods.cutensorContractionGetWorkspace(handle, in desc, in find, WorkSpacePreference.Recommended, out workspaceSize);
			if (err == CudaTensorStatus.NotSupported || err == CudaTensorStatus.InvalidValue)
				return false;
			err.Check();
			err = NativeMethods.cutensorInitContractionPlan(handle, out plan, in desc, in find, workspaceSize);
			if (err == CudaTensorStatus.NotSupported || err == CudaTensorStatus.InvalidValue)
				return false;
			err.Check();
			return true;
		}
	}
	#endregion
}
