using System;
using System.Runtime.CompilerServices;

using Althea.LinearAlgebra;
using Althea.NativeTypes;

namespace Althea.Backend.Mkl.LinearAlgebra.Dense
{
	internal static class MklBlasExtension
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static MklBlasOperation ToMkl(this MatrixOperation op)
		{
			return op switch
			{
				MatrixOperation.None => MklBlasOperation.NoneTranspose,
				MatrixOperation.Transpose => MklBlasOperation.Transpose,
				MatrixOperation.ConjugateTranspose => MklBlasOperation.ConjugateTranspose,
				MatrixOperation.Conjugate => MklBlasOperation.ConjugateAlone,
				_ => default,
			};
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static byte ToMklChar(this MatrixOperation op)
		{
			return op switch
			{
				MatrixOperation.None => (byte)MklBlasOperation.NoneTranspose,
				MatrixOperation.Transpose => (byte)MklBlasOperation.Transpose,
				MatrixOperation.ConjugateTranspose => (byte)MklBlasOperation.ConjugateTranspose,
				MatrixOperation.Conjugate => (byte)MklBlasOperation.ConjugateAlone,
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
	}

	/// <summary>
	/// The matrix layout enum in MKL BLAS
	/// </summary>
	internal enum MklBlasLayout
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
	internal enum MklBlasOperation
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
	internal enum MklBlasFillMode
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
}
