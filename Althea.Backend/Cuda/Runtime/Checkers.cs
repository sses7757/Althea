using System.Runtime.CompilerServices;


namespace Althea.Backend.Cuda;

internal static unsafe partial class MemoryPointerChecker
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool GetPointerInner<T, TS>(TS s, out T* pointer, out long length, string? sName = null) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
	{
		var ps = Unsafe.As<TS, PureStorage<T, CudaMemoryPointer>>(ref Unsafe.AsRef(in s)).Pointer;
		pointer = ps.Pointer.UnmangedPointer<T>(ps.OffsetInBytes);
		if (pointer == default)
			throw new ArgumentException(Resources.ParameterError.InvalidValue, sName);
		length = ps.LengthInBytes / sizeof(T);
		if (length < 0)
			return false;
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool GetPointerInner<T, TS>(TS s, long stride, out T* pointer, out int length, out int inc, string? sName = null, string? strideName = null) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
	{
		if (stride <= 0)
			throw new ArgumentOutOfRangeException(strideName, strideName, Resources.ParameterError.MustPositive);
		var ps = Unsafe.As<TS, PureStorage<T, CudaMemoryPointer>>(ref Unsafe.AsRef(in s)).Pointer;
		pointer = ps.Pointer.UnmangedPointer<T>(ps.OffsetInBytes);
		if (pointer == default)
			throw new ArgumentException(Resources.ParameterError.InvalidValue, sName);
		long n = ps.LengthInBytes / sizeof(T);
		n = (n - 1) / stride + 1;
		length = (int)n; inc = (int)stride;
		if (n > int.MaxValue || n < 0 || stride > int.MaxValue)
			return false;
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static unsafe bool GetPointerInner<T, TS>(TS? s, long m, long n, long ld, out T* pointer, string? sName = null, string? mName = null, string? nName = null, string? ldName = null) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
	{
		if (m <= 0)
			throw new ArgumentOutOfRangeException(mName, m, Resources.ParameterError.MustPositive);
		if (n <= 0)
			throw new ArgumentOutOfRangeException(nName, n, Resources.ParameterError.MustPositive);
		if (ld < m)
			throw new ArgumentOutOfRangeException(ldName, ld, Resources.ParameterError.InvalidValue);
		pointer = default;
		if (m > int.MaxValue || n > int.MaxValue || ld > int.MaxValue)
			return false;
		if (s is not PureStorage<T, CudaMemoryPointer> ps)
			return false; // not support
		pointer = ps.Pointer.Pointer.UnmangedPointer<T>(ps.Pointer.OffsetInBytes);
		if (pointer == default)
			throw new ArgumentException(Resources.ParameterError.InvalidValue, sName);
		if ((ps.Length + (ld - m)) / ld < n)
			throw new ArgumentException(Resources.ParameterError.WrongSize);
		return true;
	}
}
