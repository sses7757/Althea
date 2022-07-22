using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;


namespace Althea.Backend.Storage
{
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
}
