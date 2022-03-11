using System;
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
				MatrixOperation.Transpose => MklOperation.Transpose,
				MatrixOperation.ConjugateTranspose => MklOperation.ConjugateTranspose,
				MatrixOperation.Conjugate => MklOperation.ConjugateAlone,
				_ => default,
			};
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static MklOperationChar ToMklChar(this MatrixOperation op)
		{
			return op switch
			{
				MatrixOperation.None => MklOperationChar.NoneTranspose,
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
				throw new ArgumentException(Resources.Parameter.InvalidValue, (-info.status).ToOrdinal());
		}
	}

#pragma warning disable CS0649
	internal readonly struct MklLapackInfo
	{
		internal readonly int status;
	}
#pragma warning restore CS0649

	/// <summary>
	/// The matrix layout enum in MKL BLAS
	/// </summary>
	internal enum MklMatrixLayout
	{
		/// <summary>
		/// Row major storage layout
		/// </summary>
		RowMajor = 101,
		/// <summary>
		/// Column major storage layout
		/// </summary>
		ColMajor = 102
	}

	/// <summary>
	/// The matrix transposition operation enum in MKL BLAS
	/// </summary>
	internal enum MklOperation
	{
		/// <summary>
		/// Do not perform any transpositions 
		/// </summary>
		NoneTranspose = 111,
		/// <summary>
		/// Perform transposition
		/// </summary>
		Transpose = 112,
		/// <summary>
		/// Perform conjugate transposition
		/// </summary>
		ConjugateTranspose = 113,
		/// <summary>
		/// Perform conjugate alone, not supported by MKL BLAS
		/// </summary>
		ConjugateAlone = 114,
	}

	/// <summary>
	/// The symmetric/Hermitian matrix's storage mode in MKL BLAS
	/// </summary>
	internal enum MklFillMode
	{
		/// <summary>
		/// The upper part is filled
		/// </summary>
		Upper = 121,
		/// <summary>
		/// The lower part is filled
		/// </summary>
		Lower = 122
	}

	/// <summary>
	/// The triangular matrix's diagonal element type in MKL BLAS
	/// </summary>
	internal enum MklBlasDiagType
	{
		/// <summary>
		/// The diagonal elements are not unit and stored explicitly
		/// </summary>
		NonUnit = 131,
		/// <summary>
		/// The diagonal elements are unit and may not be stored
		/// </summary>
		Unit = 132
	}

	/// <summary>
	/// The side mode in MKL BLAS
	/// </summary>
	internal enum MklBlasSideMode
	{
		/// <summary>
		/// Left
		/// </summary>
		Left = 141,
		/// <summary>
		/// Right
		/// </summary>
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
	}

	internal enum MklFillModeChar : byte
	{
		Upper = (byte)'U',
		Lower = (byte)'L'
	}

	internal enum MklVectorModeChar : byte
	{
		NoVector = (byte)'N',
		Vector = (byte)'V'
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
}
