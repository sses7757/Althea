using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Althea.Backend.Cuda.TensorAlgebra.Dense;
using Althea.Helpers;
using Althea.LinearAlgebra;
using Althea.TensorAlgebra;
using Althea.TensorAlgebra.Dense;

using static Althea.Backend.Cuda.MemoryPointerChecker;


namespace Althea.Backend.Cuda
{
	public static partial class StatusExtension
	{
		/// <summary>
		/// Check whether the input <see cref="CudaTensorStatus"/> is success or not and throw exception if it is not
		/// </summary>
		/// <param name="err">The <see cref="CudaTensorStatus"/> to be checked</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Check(this CudaTensorStatus err)
		{
			if (err == CudaTensorStatus.NotSupported)
				return false;
			if (err != CudaTensorStatus.Success)
				throw new StatusException(err, new StackTrace(0));
			return true;
		}
	}
}

namespace Althea.Backend.Cuda.TensorAlgebra.Dense
{
	#region operations
	/// <summary>
	/// The supplement <see cref="UnaryOperation"/>s.
	/// </summary>
	public static class UnaryOperationSupplement
	{
		/// <summary>
		/// Square root
		/// </summary>
		public static readonly ManagedEnum<UnaryOperation> Sqrt = ManagedEnum<UnaryOperation>.DeclareNewEnum(@"Sqrt");
		/// <summary>
		/// Rectified linear unit (x if x > 0, otherwise 0)
		/// </summary>
		public static readonly ManagedEnum<UnaryOperation> ReLU = ManagedEnum<UnaryOperation>.DeclareNewEnum(@"ReLU");
		/// <summary>
		/// Reciprocal
		/// </summary>
		public static readonly ManagedEnum<UnaryOperation> Reciprocate = ManagedEnum<UnaryOperation>.DeclareNewEnum(@"Reciprocate");
		/// <summary>
		/// Logistic sigmoid function: <c>y = 1 / (1 + exp(-x))</c>
		/// </summary>
		public static readonly ManagedEnum<UnaryOperation> Sigmoid = ManagedEnum<UnaryOperation>.DeclareNewEnum(@"Sigmoid");
		/// <summary>
		/// <c>y = tanh(x)</c>
		/// </summary>
		public static readonly ManagedEnum<UnaryOperation> Tanh = ManagedEnum<UnaryOperation>.DeclareNewEnum(@"Tanh");
		/// <summary>
		/// <c>y = exp(x)</c>
		/// </summary>
		public static readonly ManagedEnum<UnaryOperation> Exp = ManagedEnum<UnaryOperation>.DeclareNewEnum(@"Exp");
		/// <summary>
		/// Base <c>e</c> logarithm
		/// </summary>
		public static readonly ManagedEnum<UnaryOperation> Log = ManagedEnum<UnaryOperation>.DeclareNewEnum(@"Log");
		/// <summary>
		/// Sine function
		/// </summary>
		public static readonly ManagedEnum<UnaryOperation> Sin = ManagedEnum<UnaryOperation>.DeclareNewEnum(@"Sin");
		/// <summary>
		/// Cosine function
		/// </summary>
		public static readonly ManagedEnum<UnaryOperation> Cos = ManagedEnum<UnaryOperation>.DeclareNewEnum(@"Cos");
		/// <summary>
		/// Tangent function
		/// </summary>
		public static readonly ManagedEnum<UnaryOperation> Tan = ManagedEnum<UnaryOperation>.DeclareNewEnum(@"Tan");
		/// <summary>
		/// Hyperbolic sine
		/// </summary>
		public static readonly ManagedEnum<UnaryOperation> Sinh = ManagedEnum<UnaryOperation>.DeclareNewEnum(@"Sinh");
		/// <summary>
		/// Hyperbolic cosine
		/// </summary>
		public static readonly ManagedEnum<UnaryOperation> Cosh = ManagedEnum<UnaryOperation>.DeclareNewEnum(@"Cosh");
		/// <summary>
		/// Inverse sine
		/// </summary>
		public static readonly ManagedEnum<UnaryOperation> ArcSin = ManagedEnum<UnaryOperation>.DeclareNewEnum(@"ArcSin");
		/// <summary>
		/// Inverse cosine
		/// </summary>
		public static readonly ManagedEnum<UnaryOperation> ArcCos = ManagedEnum<UnaryOperation>.DeclareNewEnum(@"ArcCos");
		/// <summary>
		/// Inverse tangent
		/// </summary>
		public static readonly ManagedEnum<UnaryOperation> ArcTan = ManagedEnum<UnaryOperation>.DeclareNewEnum(@"ArcTan");
		/// <summary>
		/// Inverse hyperbolic sine
		/// </summary>
		public static readonly ManagedEnum<UnaryOperation> ArcSinh = ManagedEnum<UnaryOperation>.DeclareNewEnum(@"ArcSinh");
		/// <summary>
		/// Inverse hyperbolic cosine
		/// </summary>
		public static readonly ManagedEnum<UnaryOperation> ArcCosh = ManagedEnum<UnaryOperation>.DeclareNewEnum(@"ArcCosh");
		/// <summary>
		/// Inverse hyperbolic tangent
		/// </summary>
		public static readonly ManagedEnum<UnaryOperation> ArcTanh = ManagedEnum<UnaryOperation>.DeclareNewEnum(@"ArcTanh");
		/// <summary>
		/// Ceiling function
		/// </summary>
		public static readonly ManagedEnum<UnaryOperation> Ceil = ManagedEnum<UnaryOperation>.DeclareNewEnum(@"Ceil");
		/// <summary>
		/// Floor function
		/// </summary>
		public static readonly ManagedEnum<UnaryOperation> Floor = ManagedEnum<UnaryOperation>.DeclareNewEnum(@"Floor");
	}

	internal enum CuTensorBinary
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

	internal enum CuTensorUnary
	{
		Identity = 1,
		Sqrt = 2,
		ReLU = 8,
		Conjugate = 9,
		Reciprocate = 10,
		Sigmoid = 11,
		Tanh = 12,
		Exp = 22,
		Log = 23,
		Abs = 24,
		Negate = 25,
		Sin = 26,
		Cos = 27,
		Tan = 28,
		Sinh = 29,
		Cosh = 30,
		ArcSin = 31,
		ArcCos = 32,
		ArcTan = 33,
		ArcSinh = 34,
		ArcCosh = 35,
		ArcTanh = 36,
		Ceil = 37,
		Floor = 38,
	}

	internal static partial class Converter
	{
		internal static CuTensorBinary ToCudaOp(this ManagedEnum<BinaryOperation> op)
		{
			return op.Value switch
			{
				BinaryOperation.Add => CuTensorBinary.Add,
				BinaryOperation.Multiply => CuTensorBinary.Mul,
				BinaryOperation.Maximum => CuTensorBinary.Max,
				BinaryOperation.Mininum => CuTensorBinary.Min,
				_ => 0,
			};
		}

		internal static CuTensorBinary ToCudaOp(this ManagedEnum<ReduceOperation> op)
		{
			return op.Value switch
			{
				ReduceOperation.Add => CuTensorBinary.Add,
				ReduceOperation.Multiply => CuTensorBinary.Mul,
				ReduceOperation.Maximum => CuTensorBinary.Max,
				ReduceOperation.Mininum => CuTensorBinary.Min,
				_ => 0,
			};
		}
		internal static CuTensorUnary ToCudaOp(this ManagedEnum<UnaryOperation> op)
		{
			return op.Value switch
			{
				UnaryOperation.Identity => CuTensorUnary.Identity,
				UnaryOperation.Conjugate => CuTensorUnary.Conjugate,
				UnaryOperation.Negate => CuTensorUnary.Negate,
				UnaryOperation.AbsoluteValue => CuTensorUnary.Abs,
				_ when op == UnaryOperationSupplement.Sqrt => CuTensorUnary.Sqrt,
				_ when op == UnaryOperationSupplement.Sqrt => CuTensorUnary.Sqrt,
				_ when op == UnaryOperationSupplement.ReLU => CuTensorUnary.ReLU,
				_ when op == UnaryOperationSupplement.Reciprocate => CuTensorUnary.Reciprocate,
				_ when op == UnaryOperationSupplement.Sigmoid => CuTensorUnary.Sigmoid,
				_ when op == UnaryOperationSupplement.Tanh => CuTensorUnary.Tanh,
				_ when op == UnaryOperationSupplement.Exp => CuTensorUnary.Exp,
				_ when op == UnaryOperationSupplement.Log => CuTensorUnary.Log,
				_ when op == UnaryOperationSupplement.Sin => CuTensorUnary.Sin,
				_ when op == UnaryOperationSupplement.Cos => CuTensorUnary.Cos,
				_ when op == UnaryOperationSupplement.Tan => CuTensorUnary.Tan,
				_ when op == UnaryOperationSupplement.Sinh => CuTensorUnary.Sinh,
				_ when op == UnaryOperationSupplement.Cosh => CuTensorUnary.Cosh,
				_ when op == UnaryOperationSupplement.ArcSin => CuTensorUnary.ArcSin,
				_ when op == UnaryOperationSupplement.ArcCos => CuTensorUnary.ArcCos,
				_ when op == UnaryOperationSupplement.ArcTan => CuTensorUnary.ArcTan,
				_ when op == UnaryOperationSupplement.ArcSinh => CuTensorUnary.ArcSinh,
				_ when op == UnaryOperationSupplement.ArcCosh => CuTensorUnary.ArcCosh,
				_ when op == UnaryOperationSupplement.ArcTanh => CuTensorUnary.ArcTanh,
				_ when op == UnaryOperationSupplement.Ceil => CuTensorUnary.Ceil,
				_ when op == UnaryOperationSupplement.Floor => CuTensorUnary.Floor,
				_ => 0
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

	#region compute type
	[Flags]
	internal enum ComputeType
	{
		/// <summary>
		/// <see cref="DataTypeClassification.BinaryFloat_IEEE754"/> binary-16 floating point number (a.k.a. <see cref="System.Half"/>)
		/// </summary>
		Float16 = 1 << 0,
		/// <summary>
		/// <see cref="BrainHalf.RealBrainHalfType"/> binary-16 floating point number (a.k.a. <see cref="Cuda.BrainHalf"/>)
		/// </summary>
		BrainFloat16 = 1 << 10,
		/// <summary>
		/// The floating point number with 8-bit exponent and 10-bit mantissa, not supported in current implementation
		/// </summary>
		TensorFloat32 = 1 << 3,
		/// <summary>
		/// <see cref="DataTypeClassification.BinaryFloat_IEEE754"/> binary-32 floating point number (a.k.a. <see cref="float"/>) 
		/// </summary>
		Float32 = 1 << 2,
		/// <summary>
		/// <see cref="DataTypeClassification.BinaryFloat_IEEE754"/> binary-64 floating point number (a.k.a. <see cref="double"/>) 
		/// </summary>
		Float64 = 1 << 4,
		/// <summary>
		/// The 8-bit unsigned integer (a.k.a. <see cref="byte"/>)
		/// </summary>
		UInt8 = 1 << 6,
		/// <summary>
		/// The 8-bit signed integer (a.k.a. <see cref="sbyte"/>)
		/// </summary>
		SInt8 = 1 << 8,
		/// <summary>
		/// The 32-bit unsigned integer (a.k.a. <see cref="uint"/>)
		/// </summary>
		UInt32 = 1 << 7,
		/// <summary>
		/// The 32-bit signed integer (a.k.a. <see cref="int"/>)
		/// </summary>
		SInt32 = 1 << 9
	}

	internal static partial class Converter
	{
		internal static ComputeType ToComputeType(this CudaDataType type)
		{
			return type switch
			{
				CudaDataType.RealInt8 or CudaDataType.ComplexInt8 => ComputeType.SInt8,
				CudaDataType.RealInt32 or CudaDataType.ComplexInt32 => ComputeType.SInt32,
				CudaDataType.RealUInt8 or CudaDataType.ComplexUInt8 => ComputeType.UInt8,
				CudaDataType.RealUInt32 or CudaDataType.ComplexUInt32 => ComputeType.UInt32,
				CudaDataType.RealFloat32 or CudaDataType.ComplexFloat32 => ComputeType.Float32,
				CudaDataType.RealFloat64 or CudaDataType.ComplexFloat64 => ComputeType.Float64,
				CudaDataType.RealFloat16 or CudaDataType.ComplexFloat16 => ComputeType.Float32,
				CudaDataType.RealBrainFloat16 or CudaDataType.ComplexBrainFloat16 => ComputeType.Float32,
				// TensorFloat32 not supported
				_ => 0,
			};
		}
	}
	#endregion

	#region other enum
	/// <summary>
	/// This enum gives users finer control over which algorithm should be executed by tensor contraction.
	/// </summary>
	/// <remarks>Values >= 0 correspond to certain sub-algorithms of <see cref="GETT"/>.</remarks>
	public enum ContractionAlgorithm
	{
		/// <summary>
		/// The more accurate but also more time-consuming performance model
		/// </summary>
		DefaultPatient = -6,
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

	#region wrapper
	[StructLayout(LayoutKind.Explicit, Size = 8 * 512)]
	internal readonly struct CudaTensorHandle { }

	[StructLayout(LayoutKind.Explicit, Size = 8 * 72)]
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
		private readonly CuTensorUnary operation;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool TryCreate<T, TS>(in CudaTensorHandle handle, DenseTensorWrapper<T, TS> tensor, out TensorDescription descr) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
		{
			return TryCreate(handle, tensor, T.Type.ToCudaDataType(), out descr);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool TryCreate<T, TS>(in CudaTensorHandle handle, DenseTensorWrapper<T, TS> tensor, CudaDataType type, out TensorDescription descr) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
		{
			var op = tensor.Operation.ToCudaOp();
			if (tensor.IsInvalid() || op == 0)
			{
				descr = default; return false;
			}
			CudaTensorStatus err;
			if (tensor.Strides.IsEmpty)
			{
				err = NativeMethods.cutensorInitTensorDescriptor(handle, out descr, tensor.Rank, in tensor.Size[0], in Unsafe.NullRef<long>(), type, op);
			}
			else
			{
				err = NativeMethods.cutensorInitTensorDescriptor(handle, out descr, tensor.Rank, in tensor.Size[0], in tensor.Strides[0], type, op);
			}
			return err.Check();
		}
	}

	[StructLayout(LayoutKind.Explicit, Size = 8 * 288)]
	internal readonly unsafe record struct ContractDescription
	{
		// TODO: test fields
		[FieldOffset(16)]
		private readonly TensorDescription descrA;
		[FieldOffset(8 * 72 + 16)]
		private readonly TensorDescription descrB;
		[FieldOffset(8 * 72 * 2 + 16)]
		private readonly TensorDescription descrCD;

		[FieldOffset(1168)]
		private readonly int alignA;
		[FieldOffset(1168 + 4)]
		private readonly int alignB;
		[FieldOffset(1168 + 4 * 2)]
		private readonly int alignC;
		[FieldOffset(1168 + 4 * 3)]
		private readonly int alignD;

		public readonly TensorDescription DescriptionA => this.descrA;
		public readonly TensorDescription DescriptionB => this.descrB;
		public readonly TensorDescription DescriptionCD => this.descrCD;
		public readonly int AlignmentA => this.alignA;
		public readonly int AlignmentB => this.alignB;
		public readonly int AlignmentC => this.alignC;
		public readonly int AlignmentD => this.alignD;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool TryCreate<T, TS1, TS2, TS3>(Api api, DenseTensorWrapper<T, TS1> A, DenseTensorWrapper<T, TS2> B, DenseTensorWrapper<T, TS3> C, DenseTensorWrapper<T, TS3> D, TensorContractInfo info, out ContractDescription descr, ComputeType computeType = 0) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>
		{
			descr = default;
			if (info.IsInvalid())
				return false;
			if (!C.SizeEquals(D))
				return false;
			if (!GetPointer(api, A.ValueStorage, A.Size, A.OuterSize, out T* pA))
				return false;
			if (!GetPointer(api, B.ValueStorage, B.Size, B.OuterSize, out T* pB))
				return false;
			if (!GetPointer(api, C.ValueStorage, C.Size, C.OuterSize, out T* pC))
				return false;
			if (!GetPointer(api, D.ValueStorage, D.Size, D.OuterSize, out T* pD))
				return false;

			var dataType = T.Type.ToCudaDataType();
			if (!TensorDescription.TryCreate(api.handle, A, dataType, out var descrA))
				return false;
			if (!TensorDescription.TryCreate(api.handle, B, dataType, out var descrB))
				return false;
			if (!TensorDescription.TryCreate(api.handle, C, dataType, out var descrC))
				return false;

			NativeMethods.cutensorGetAlignmentRequirement(api.handle, pA, &descrA, out int alignA).Check();
			NativeMethods.cutensorGetAlignmentRequirement(api.handle, pB, &descrB, out int alignB).Check();
			NativeMethods.cutensorGetAlignmentRequirement(api.handle, pC, &descrC, out int alignC).Check();
			NativeMethods.cutensorGetAlignmentRequirement(api.handle, pD, &descrC, out int alignD).Check();

			Span<char> labelA = stackalloc char[A.Rank], labelB = stackalloc char[B.Rank], labelC = stackalloc char[C.Rank];
			info.GetLabels(ref labelA, ref labelB, ref labelC);
			Span<int> modeA = stackalloc int[A.Rank], modeB = stackalloc int[B.Rank], modeC = stackalloc int[C.Rank];
			labelA.CopyTo(modeA, static c => c); labelB.CopyTo(modeB, static c => c); labelC.CopyTo(modeC, static c => c);

			if (computeType == 0)
				computeType = dataType.ToComputeType();

			ContractDescription descr1 = default;
			var err = NativeMethods.cutensorInitContractionDescriptor(api.handle, &descr1,
				&descrA, modeA, alignA, &descrB, modeB, alignB,
				&descrC, modeC, alignC, &descrC, modeC, alignD, computeType);
			descr = descr1;
			return err.Check();
		}
	}

	[StructLayout(LayoutKind.Explicit, Size = 8 * 64)]
	internal readonly record struct ContractFind
	{
		[FieldOffset(4 * 4)]
		private readonly ContractionAlgorithm algorithm;

		[FieldOffset(5 * 4)]
		private readonly int GETTSpecificAlgorithm;

		public readonly ContractionAlgorithm Algorithm => this.algorithm;
		public readonly int GETTAlgorithm => this.GETTSpecificAlgorithm;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ContractFind(in CudaTensorHandle handle, ContractionAlgorithm algorithm)
		{
			NativeMethods.cutensorInitContractionFind(handle, out this, algorithm).Check();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool TryCreate(in CudaTensorHandle handle, ContractionAlgorithm algorithm, out ContractFind find)
		{
			return NativeMethods.cutensorInitContractionFind(handle, out find, algorithm) == CudaTensorStatus.Success;
		}

		public readonly bool Invalid
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.algorithm == 0;
		}
	}

	[StructLayout(LayoutKind.Explicit, Size = 8 * 1408)]
	internal readonly struct ContractPlan
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe bool TryCreate(in CudaTensorHandle handle, ContractDescription* desc, in ContractFind find, out ContractPlan plan, out long workspaceSize)
		{
			var err = NativeMethods.cutensorContractionGetWorkspace(handle, desc, in find, WorkSpacePreference.Recommended, out workspaceSize);
			if (!err.Check())
				return false;
			err = NativeMethods.cutensorInitContractionPlan(handle, out plan, desc, in find, workspaceSize);
			return err.Check();
		}
	}
	#endregion
}
