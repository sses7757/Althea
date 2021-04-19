using System;
using System.Runtime.CompilerServices;

using Althea.LinearAlgebra;
using Althea.NativeTypes;


namespace Althea.Backend.Cuda.LinearAlgebra
{
	/// <summary>
	/// The <see cref="CuBlasMatrixOperation"/> enum indicates which operation needs to be performed with the dense matrix.<br/>
	/// Its values correspond to Fortran characters ‘N’ or ‘n’ (non-transpose), ‘T’ or ‘t’ (transpose) and ‘C’ or ‘c’ (conjugate transpose) that are often used as parameters to legacy BLAS implementations.
	/// </summary>
	internal enum CuBlasMatrixOperation
	{
		/// <summary>
		/// the non-transpose operation is selected
		/// </summary>
		None = 0,
		/// <summary>
		/// the transpose operation is selected
		/// </summary>
		Transpose = 1,
		/// <summary>
		/// the conjugate transpose operation is selected
		/// </summary>
		ConjugateTranspose = 2,
		/// <summary>
		/// the conjugate alone operation, not used in cuBLAS, shall be further dealt with
		/// </summary>
		ConjugateAlone = 3,
	}

	internal static class Conversions
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static char ToChar(this SVDStore store)
		{
			return store switch
			{
				SVDStore.All => 'A',
				SVDStore.Economic => 'S',
				SVDStore.Overwrite => 'O',
				SVDStore.None => 'N',
				_ => throw new NotSupportedException(),
			};
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static CuBlasMatrixOperation ToCuda(this Althea.LinearAlgebra.MatrixOperation op, bool? hermitian = null)
		{
			return op switch
			{
				Althea.LinearAlgebra.MatrixOperation.Transpose => CuBlasMatrixOperation.Transpose,
				Althea.LinearAlgebra.MatrixOperation.Conjugate => hermitian.HasValue && hermitian.Value ? CuBlasMatrixOperation.Transpose : CuBlasMatrixOperation.ConjugateAlone,
				Althea.LinearAlgebra.MatrixOperation.ConjugateTranspose => CuBlasMatrixOperation.ConjugateTranspose,
				_ => CuBlasMatrixOperation.None,
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
		internal static bool CheckExSupport(this DataType type)
		{
			return type switch
			{
				DataType.RealSingle or DataType.RealDouble or DataType.RealHalf or
				DataType.ComplexSingle or DataType.ComplexDouble or DataType.ComplexHalf => true,
				_ => false,
			};
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool CheckEx2Support(this DataType type)
		{
			return type switch
			{
				DataType.RealSingle or DataType.RealDouble or DataType.RealHalf or BrainFloatConst.RealBrainFloat16 or
				DataType.ComplexSingle or DataType.ComplexDouble or DataType.ComplexHalf or BrainFloatConst.ComplexBrainFloat16 => true,
				_ => false,
			};
		}
	}
}
