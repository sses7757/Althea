using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

using Althea.Helpers;
using Althea.LinearAlgebra;
using Althea.NativeTypes;


namespace Althea.Backend.Mkl.LinearAlgebra.Dense
{
	/// <summary>
	/// The supplement <see cref="UnaryOperation"/>s.
	/// </summary>
	public enum UnaryOperationSupplement
	{
		/// <summary>
		/// Operation that returns the base <c>e</c> exponential of the input parameter
		/// </summary>
		Exp = UnaryOperation.AbsoluteValue + 1,
		/// <summary>
		/// Operation that returns the base 2 exponential of the input parameter
		/// </summary>
		Exp2,
		/// <summary>
		/// Operation that returns the base 10 exponential of the input parameter
		/// </summary>
		Exp10,
		/// <summary>
		/// Operation that returns the base <c>e</c> exponential of the input parameter minus 1
		/// </summary>
		ExpM1,
		/// <summary>
		/// Operation that returns the base <c>e</c> logarithm of the input parameter
		/// </summary>
		Ln,
		/// <summary>
		/// Operation that returns the base 2 logarithm of the input parameter
		/// </summary>
		Log2,
		/// <summary>
		/// Operation that returns the base 10 logarithm of the input parameter
		/// </summary>
		Log10,
		/// <summary>
		/// Operation that returns the base <c>e</c> logarithm of the input parameter plus 1
		/// </summary>
		Log1p,
		/// <summary>
		/// Operation that returns the exponent part of the input parameter
		/// </summary>
		LogBinary,
		/// <summary>
		/// Operation that returns the cosine of the input parameter
		/// </summary>
		Cos,
		/// <summary>
		/// Operation that returns the sine of the input parameter
		/// </summary>
		Sin,
		/// <summary>
		/// Operation that returns the tangent of the input parameter
		/// </summary>
		Tan,
		/// <summary>
		/// Operation that returns the inverse cosine of the input parameter
		/// </summary>
		ArcCos,
		/// <summary>
		/// Operation that returns the inverse sine of the input parameter
		/// </summary>
		ArcSin,
		/// <summary>
		/// Operation that returns the inverse tangent of the input parameter
		/// </summary>
		ArcTan,
		/// <summary>
		/// Operation that returns the hyperbolic cosine of the input parameter
		/// </summary>
		Cosh,
		/// <summary>
		/// Operation that returns the hyperbolic sine of the input parameter
		/// </summary>
		Sinh,
		/// <summary>
		/// Operation that returns the hyperbolic tangent of the input parameter
		/// </summary>
		Tanh,
		/// <summary>
		/// Operation that returns the inverse hyperbolic cosine of the input parameter
		/// </summary>
		ArcCosh,
		/// <summary>
		/// Operation that returns the inverse hyperbolic sine of the input parameter
		/// </summary>
		ArcSinh,
		/// <summary>
		/// Operation that returns the inverse hyperbolic tangent of the input parameter
		/// </summary>
		ArcTanh,
	}

	/// <summary>
	/// Encapsulates methods as MKL VML error callback methods.
	/// </summary>
	/// <param name="context">The error context as a <see cref="VmlErrorContext"/></param>
	/// <returns>Whether the callback handled the error (0) or not.</returns>
	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	public delegate int VmlErrorCallbackDelegate(ref VmlErrorContext context);

	/// <summary>
	/// The error structure used by MKL Vector VML error callbacks.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct VmlErrorContext
	{
		/// <summary>
		/// Error status value
		/// </summary>
		public int Code;
		/// <summary>
		/// Index for bad array	element, or bad array dimension, or bad array pointer
		/// </summary>
		public int Index;
		/// <summary>
		/// Error argument 1
		/// </summary>
		public double Argument1;
		/// <summary>
		/// Error argument 2
		/// </summary>
		public double Argument2;
		/// <summary>
		/// Error result 1
		/// </summary>
		public double Result1;
		/// <summary>
		/// Error result 2
		/// </summary>
		public double Result2;
		/// <summary>
		/// Error function name as a byte array
		/// </summary>
		public FixedBuffer_64<byte> FuncName;
		/// <summary>
		/// Length of <see cref="FuncName"/>
		/// </summary>
		public int FuncNameLength;
		/// <summary>
		/// Error argument 1's imaginary part
		/// </summary>
		public double Argument1Imag;
		/// <summary>
		/// Error argument 2's imaginary part
		/// </summary>
		public double Argument2Imag;
		/// <summary>
		/// Error result 1's imaginary part
		/// </summary>
		public double Result1Imag;
		/// <summary>
		/// Error result 2's imaginary part
		/// </summary>
		public double Result2Imag;

		/// <summary>
		/// Get the error function name as a <see cref="string"/>.
		/// </summary>
		public string FunctionName => Encoding.ASCII.GetString(FuncName.AsSpan(this.FuncNameLength));
	}

	internal static class MklBlasExtension
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static MklOperation ToMkl(this MatrixOperation op)
		{
			return op switch
			{
				MatrixOperation.None => MklOperation.NoneTranspose,
				MatrixOperation.Conjugate => MklOperation.NoneTranspose,
				MatrixOperation.Transpose => MklOperation.Transpose,
				MatrixOperation.ConjugateTranspose => MklOperation.ConjugateTranspose,
				_ => default,
			};
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static MklOperationChar ToMklChar(this MatrixOperation op)
		{
			return op switch
			{
				MatrixOperation.None => MklOperationChar.NoneTranspose,
				MatrixOperation.Conjugate => MklOperationChar.Conjugate,
				MatrixOperation.Transpose => MklOperationChar.Transpose,
				MatrixOperation.ConjugateTranspose => MklOperationChar.ConjugateTranspose,
				_ => default,
			};
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static MklOperationChar ToChar(this MklOperation op)
		{
			return op switch
			{
				MklOperation.NoneTranspose => MklOperationChar.NoneTranspose,
				MklOperation.Transpose => MklOperationChar.Transpose,
				MklOperation.ConjugateTranspose => MklOperationChar.ConjugateTranspose,
				_ => default,
			};
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool CheckBaseSupport(this DataType type)
		{
			return type switch
			{
				DataType.RealSingle or DataType.RealDouble or
				DataType.ComplexSingle or DataType.ComplexDouble => true,
				_ => false,
			};
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static MklFillModeChar ToChar(this bool fillUpper)
		{
			return fillUpper ? MklFillModeChar.Upper : MklFillModeChar.Lower;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void Check(this MklLapackInfo info, SolveMethodKind kind)
		{
			if (info.status > 0)
				throw new MatrixSolveAlgorithmException(kind, info.status);
			if (info.status < 0)
				throw new ArgumentException(Resources.ParameterError.InvalidValue, (-info.status).ToOrdinal());
		}
	}

	internal readonly struct MklLapackInfo
	{
		internal readonly long status;
	}

	internal enum MklMatrixLayout
	{
		RowMajor = 101,
		ColMajor = 102
	}

	/// <summary>
	/// The matrix transposition operation enum in MKL BLAS
	/// </summary>
	internal enum MklOperation
	{
		NoneTranspose = 111,
		Transpose = 112,
		ConjugateTranspose = 113,
	}

	internal enum MklFillMode
	{
		Upper = 121,
		Lower = 122
	}

	internal enum MklBlasDiagType
	{
		NonUnit = 131,
		Unit = 132
	}

	internal enum MklBlasSideMode
	{
		Left = 141,
		Right = 142
	}


	internal enum MklMatrixLayoutChar : byte
	{
		RowMajor = (byte)'R',
		ColMajor = (byte)'C'
	}

	internal enum MklOperationChar : byte
	{
		NoneTranspose = (byte)'N',
		Transpose = (byte)'T',
		ConjugateTranspose = (byte)'C',
		Conjugate = (byte)'R',
	}

	internal enum MklFillModeChar : byte
	{
		Upper = (byte)'U',
		Lower = (byte)'L'
	}

	internal enum MklJitStatus : int
	{
		Success = 0,
		NoJit = 1,
		JitError = 2
	}

	internal enum MklVectorModeChar : byte
	{
		NoVector = (byte)'N',
		Vector = (byte)'V',
		Immediate = (byte)'I'
	}

	internal enum MklSortModeChar : byte
	{
		NoSort = (byte)'N',
		Sort = (byte)'S'
	}

	internal enum MklSvdModeChar : byte
	{
		None = (byte)'N',
		All = (byte)'A',
		Store = (byte)'S',
		Overwrite = (byte)'O',
	}

	internal enum MklSchurModeChar : byte
	{
		OnlyEigenvalues = (byte)'E',
		SchurForm = (byte)'S'
	}

	internal enum MklSchurEigenvectorModeChar : byte
	{
		Right = (byte)'R',
		Left = (byte)'L',
		Both = (byte)'B'
	}

	internal enum MklSchurEigenSelectModeChar : byte
	{
		All = (byte)'A',
		BackTransform = (byte)'B',
		Selected = (byte)'S'
	}

	internal enum MklSchurReorderConditionNumberModeChar : byte
	{
		None = (byte)'N',
		Eigenvalues = (byte)'E',
		InvariantSubspace = (byte)'V',
		Both = (byte)'B'
	}
}
