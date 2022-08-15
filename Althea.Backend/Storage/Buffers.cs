using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Althea.Backend.Storage;

#region array pool buffer
internal readonly unsafe ref struct MatBuffer<T> where T : unmanaged
{
	internal readonly long ld;
	private readonly T* ptr;
	private readonly GCHandle handle;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal MatBuffer(T* org, long ld, long n)
	{
		if (org == null)
		{
			long bytes = n * n * sizeof(T);
			if (bytes <= int.MaxValue)
			{
				var buffer = ArrayPool<byte>.Shared.Rent((int)bytes);
				this.handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
				this.ptr = (T*)this.handle.AddrOfPinnedObject().ToPointer();
			}
			else
			{
				this.ptr = (T*)Marshal.AllocHGlobal((IntPtr)bytes);
				this.handle = GCHandle.FromIntPtr((IntPtr)1);
			}
			this.ld = n;
		}
		else
		{
			this.ptr = org;
			this.ld = ld;
			this.handle = default;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose()
	{
		if (this.handle.IsAllocated)
		{
			if (GCHandle.ToIntPtr(this.handle) != (IntPtr)1)
			{
				this.handle.Free();
				if (this.handle.Target is byte[] buffer)
					ArrayPool<byte>.Shared.Return(buffer);
			}
			else
			{
				Marshal.FreeHGlobal((IntPtr)this.ptr);
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator T*(MatBuffer<T> buffer) => buffer.ptr;
}
internal readonly unsafe ref struct VecBuffer<T> where T : unmanaged
{
	private readonly GCHandle handle;
	private readonly T* ptr;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal VecBuffer(T* ptr)
	{
		this.handle = default;
		this.ptr = ptr;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal VecBuffer(long bytes)
	{
		if (bytes <= 0)
		{
			this = default;
			return;
		}
		var buffer = ArrayPool<byte>.Shared.Rent(checked((int)bytes));
		this.handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
		this.ptr = (T*)this.handle.AddrOfPinnedObject().ToPointer();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose()
	{
		if (this.handle.IsAllocated)
		{
			this.handle.Free();
			if (this.handle.Target is byte[] buffer)
				ArrayPool<byte>.Shared.Return(buffer);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator T*(VecBuffer<T> buffer) => buffer.ptr;
}

internal static unsafe class ArrayPoolBuffers
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static MatBuffer<T> Create<T>(T* ptr, long ld, long n) where T : unmanaged => new(ptr, ld, n);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static VecBuffer<T> Create<T>(long bytes) where T : unmanaged => new(bytes);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static VecBuffer<T> Create<T>(T* ptr) where T : unmanaged => new(ptr);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static VecBuffer<T> Create<T>(T* ptr, long size) where T : unmanaged => ptr == null ? new(size * sizeof(T)) : new(ptr);
}
#endregion


#region error buffer
internal unsafe readonly ref struct ErrorStateBuffer<T, TS> where T : unmanaged where TS : class, IStorage<TS>
{
	private readonly TS data;
	private readonly T* ptr;
	// cannot be modified inside, however can read from outside's error info
	private readonly ref bool hasError;

	public readonly bool Invalid
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => this.ptr == null;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ErrorStateBuffer(TS data, void* ptr, ref bool hasError)
	{
		this.data = data; this.ptr = (T*)ptr; this.hasError = ref hasError;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly void Dispose()
	{
		if (this.hasError)
			this.data?.Dispose();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator TS(ErrorStateBuffer<T, TS> buf) => buf.data;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator T*(ErrorStateBuffer<T, TS> buf) => buf.ptr;
}
#endregion
