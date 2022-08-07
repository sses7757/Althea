using System.Runtime.CompilerServices;

using Althea.LinearAlgebra;


namespace Althea.Backend.Cuda.LinearAlgebra;

/// <summary>
/// The <see cref="CuBlasOperation"/> enum indicates which operation needs to be performed with the dense matrix.<br/>
/// Its values correspond to Fortran characters ‘N’ or ‘n’ (non-transpose), ‘T’ or ‘t’ (transpose) and ‘C’ or ‘c’ (conjugate transpose) that are often used as parameters to legacy BLAS implementations.
/// </summary>
internal enum CuBlasOperation
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
	Conjugate = 3,
}

internal static class Conversions
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static unsafe sbyte ToSvdChar<T>(T* matrix, T* svd, bool full) where T : unmanaged, IBaseNumber<T>
	{
		if (svd is null)
			return (sbyte)'N';
		if (svd == matrix)
			return (sbyte)'O';
		return full ? (sbyte)'A' : (sbyte)'S';
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static CuBlasOperation ToCuda(this MatrixOperation op, bool? hermitian = null)
	{
		return op switch
		{
			MatrixOperation.Transpose => CuBlasOperation.Transpose,
			MatrixOperation.Conjugate => hermitian.HasValue && hermitian.Value ? CuBlasOperation.Transpose : CuBlasOperation.Conjugate,
			MatrixOperation.ConjugateTranspose => CuBlasOperation.ConjugateTranspose,
			_ => CuBlasOperation.None,
		};
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool CheckBaseSupport<T>(this T value) where T : unmanaged, IBaseNumber<T>
	{
		return value switch
		{
			Float32 or Float64 or
			Complex<Float32> or Complex<Float64> => true,
			_ => false,
		};
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool CheckExSupport<T>(this T value) where T : unmanaged, IBaseNumber<T>
	{
		return value switch
		{
			Float32 or Float64 or Float16 or
			Complex<Float32> or Complex<Float64> or Complex<Float16> => true,
			_ => false,
		};
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool CheckEx2Support<T>(this T value) where T : unmanaged, IBaseNumber<T>
	{
		return value switch
		{
			Float32 or Float64 or Float16 or BrainHalf or
			Complex<Float32> or Complex<Float64> or Complex<Float16> or Complex<BrainHalf> => true,
			_ => false,
		};
	}
}
