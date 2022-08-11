using System.Runtime.CompilerServices;

using Althea.LinearAlgebra;


namespace Althea.Backend.Cuda.LinearAlgebra;

internal enum CuBlasOperation
{
	None = 0,
	Transpose = 1,
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
}
