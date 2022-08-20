using System.Runtime.CompilerServices;

using Althea.Backend.Storage;
using Althea.LinearAlgebra;

using static Althea.Backend.Cuda.MemoryPointerChecker;

namespace Althea.Backend.Cuda.LinearAlgebra;

#region buffer
internal unsafe static class SignalErrorBufferExtension
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ErrorStateBuffer<T, TS> Create<T, TS>(this long size, ref bool hasError) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
	{
		if (!Check<T, TS>())
			return default;
		TS val = TS.Create(stackalloc[] { size });
		T* ptr = val.GetPointerDirect<T, TS>();
		return new(val, ptr, ref hasError);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ErrorStateBuffer<T, TS> Create<T, TS>(this TS array, ref bool hasError) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
	{
		if (!GetPointer(array, out T* ptr, out _))
			return default;
		return new(array.MakeReference(), ptr, ref hasError);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ErrorStateBuffer<T, TS> CreateFromFirst<T, TS>(this ReadOnlySpan<TS> span, long size, ref bool hasError) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
	{
		return span.Length >= 1 && span[0] is not null ? Create<T, TS>(span[0], ref hasError) : Create<T, TS>(size, ref hasError);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ErrorStateBuffer<T, TS> CreateFromSecond<T, TS>(this ReadOnlySpan<TS> span, long size, ref bool hasError) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
	{
		return span.Length >= 2 && span[1] is not null ? Create<T, TS>(span[1], ref hasError) : Create<T, TS>(size, ref hasError);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ErrorStateBuffer<U, TS> Create<T, TS, U>(this long size, ref bool hasError) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS> where U : unmanaged
	{
		if (!Check<T, TS>())
			return default;
		TS val = TS.Create(stackalloc[] { size });
		T* ptr = val.GetPointerDirect<T, TS>();
		return new(val, ptr, ref hasError);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ErrorStateBuffer<U, TS> Create<T, TS, U>(this TS array, ref bool hasError) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS> where U : unmanaged
	{
		if (!GetPointer(array, out T* ptr, out _))
			return default;
		return new(array.MakeReference(), ptr, ref hasError);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool Update<T, TS>(this in ErrorStateBuffer<T, TS> buffer, long size) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
	{
		if (!Check<T, TS>())
			return false;
		var val = TS.Create(stackalloc[] { size });
		var ptr = val.GetPointerDirect<T, TS>();
		buffer.Update(val, ptr);
		return true;
	}
}
#endregion

#region BLAS Op
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
#endregion