using System.Runtime.CompilerServices;

using Althea.Backend.Storage;


namespace Althea.Backend.Mkl.LinearAlgebra;

#region buffer
internal unsafe static class SignalErrorBufferExtension
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ErrorStateBuffer<T, TS> Create<T, TS>(this long size, ref bool hasError) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
	{
		return Create<T, TS, T>(size, ref hasError);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ErrorStateBuffer<T, TS> Create<T, TS>(this TS array, ref bool hasError) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
	{
		if (!MemoryPointerChecker.GetPointer(array, out T* ptr, out _))
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
	public static ErrorStateBuffer<U, TS> CreateFromFirst<T, TS, U>(this ReadOnlySpan<TS> span, long size, ref bool hasError) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS> where U : unmanaged
	{
		return span.Length >= 1 && span[0] is not null ? Create<T, TS, U>(span[0], ref hasError) : Create<T, TS, U>(size, ref hasError);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ErrorStateBuffer<U, TS> CreateFromSecond<T, TS, U>(this ReadOnlySpan<TS> span, long size, ref bool hasError) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS> where U : unmanaged
	{
		return span.Length >= 2 && span[1] is not null ? Create<T, TS, U>(span[1], ref hasError) : Create<T, TS, U>(size, ref hasError);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ErrorStateBuffer<U, TS> Create<T, TS, U>(this long size, ref bool hasError) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS> where U : unmanaged
	{
		if (typeof(TS) != typeof(PureStorage<T, CpuMemoryPointer>))
			return default;
		var val = PureStorage<T, CpuMemoryPointer>.Create(size);
		T* ptr = val.Pointer.Pointer.UnmangedPointer<T>();
		return new((val as TS)!, ptr, ref hasError);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ErrorStateBuffer<U, TS> Create<T, TS, U>(this TS array, ref bool hasError) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS> where U : unmanaged
	{
		if (!MemoryPointerChecker.GetPointer(array, out T* ptr, out _))
			return default;
		return new(array.MakeReference(), ptr, ref hasError);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool Update<T, TS>(this in ErrorStateBuffer<T, TS> buffer, long size) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
	{
		if (typeof(TS) != typeof(PureStorage<T, CpuMemoryPointer>))
			return default;
		var val = PureStorage<T, CpuMemoryPointer>.Create(size);
		T* ptr = val.Pointer.Pointer.UnmangedPointer<T>();
		buffer.Update((val as TS)!, ptr);
		return true;
	}
}
#endregion

