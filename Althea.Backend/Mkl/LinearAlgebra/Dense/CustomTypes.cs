using System.Runtime.CompilerServices;

using Althea.Helpers;
using Althea.LinearAlgebra;
using Althea.NativeTypes;


namespace Althea.Backend.Mkl.LinearAlgebra.Dense
{
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
		internal static MklVectorModeChar ToChar(this SolveVectorMode mode)
		{
			return mode switch
			{
				SolveVectorMode.NoVector => MklVectorModeChar.NoVector,
				_ => MklVectorModeChar.Vector,
			};
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static (MklVectorModeChar l, MklVectorModeChar r) ToLRChar(this SolveVectorMode mode)
		{
			return mode switch
			{
				SolveVectorMode.NoVector => (MklVectorModeChar.NoVector, MklVectorModeChar.NoVector),
				SolveVectorMode.Vector => (MklVectorModeChar.Vector, MklVectorModeChar.Vector),
				SolveVectorMode.Left => (MklVectorModeChar.Vector, MklVectorModeChar.NoVector),
				SolveVectorMode.Right => (MklVectorModeChar.NoVector, MklVectorModeChar.Vector),
				_ => default,
			};
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static MklFillModeChar ToChar(this bool fillUpper)
		{
			return fillUpper ? MklFillModeChar.Upper : MklFillModeChar.Lower;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static MklSvdModeChar ToChar(this SVDStore store)
		{
			return store switch
			{
				SVDStore.All => MklSvdModeChar.All,
				SVDStore.Economic => MklSvdModeChar.Store,
				SVDStore.Overwrite => MklSvdModeChar.Overwrite,
				SVDStore.None => MklSvdModeChar.None,
				_ => default,
			};
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void CheckLapackInfo(this SolveMethodKind kind, MklLapackInfo info)
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
}
