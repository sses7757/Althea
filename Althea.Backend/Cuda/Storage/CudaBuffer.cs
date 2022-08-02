using System.Buffers;
using System.Runtime.CompilerServices;


namespace Althea.Backend.Cuda.Storage
{
	internal readonly ref struct CudaFileBuffer
	{
		private readonly IntPtr buffer;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator IntPtr(CudaFileBuffer buffer) => buffer.buffer;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal CudaFileBuffer(IntPtr buf, long size)
		{
			this.buffer = buf;
			NativeMethods.cuFileBufRegister(buf, size, 0).Check();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static CudaFileBuffer Create(IntPtr buf, long size, bool cached = false) => cached ? default : new(buf, size);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Dispose()
		{
			if (this.buffer != default)
				NativeMethods.cuFileBufDeregister(this.buffer).Check();
		}
	}

	internal readonly ref struct CudaBuffer
	{
		private readonly IntPtr deviceBuffer;

		private readonly long extraDeviceInfoOffset;

		private readonly byte[]? hostBuffer;

		/// <summary>
		/// Get the buffer array on host (CPU memory) as an array of <see cref="byte"/>. Returns an empty array if this <see cref="CudaBuffer"/> was created without host buffer.
		/// </summary>
		public byte[] HostBuffer
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.hostBuffer ?? System.Array.Empty<byte>();
		}

		/// <summary>
		/// Get the pointer to the buffer array on current CUDA device (GPU memory) as an <see cref="IntPtr"/> or <see cref="IntPtr.Zero"/> if no array was allocated on device.
		/// </summary>
		public IntPtr DeviceBuffer
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.deviceBuffer;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator IntPtr(CudaBuffer buffer) => buffer.deviceBuffer;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator byte[]?(CudaBuffer buffer) => buffer.hostBuffer;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator CudaFileBuffer(CudaBuffer buffer) => new(buffer.deviceBuffer, Math.Abs(buffer.extraDeviceInfoOffset));

		/// <summary>
		/// Get the pointer to the preserved extra device info on current CUDA device (GPU memory) as an <see cref="IntPtr"/> or <see cref="IntPtr.Zero"/> if there is no preserved extra device info.
		/// </summary>
		public IntPtr ExtraDeviceInfo
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.extraDeviceInfoOffset < 0 ? default : (IntPtr)((long)this.deviceBuffer + extraDeviceInfoOffset);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private CudaBuffer(long workSpaceDeviceBytes, long workSpaceHostBytes = 0, long extraDeviceInfoBytes = 0)
		{
			if (workSpaceHostBytes > int.MaxValue)
				throw new ArgumentOutOfRangeException(nameof(workSpaceHostBytes), workSpaceHostBytes, Resources.ParameterError.InvalidValue);
			this.hostBuffer = workSpaceHostBytes == 0 ? null : ArrayPool<byte>.Shared.Rent((int)workSpaceHostBytes);
			if (workSpaceDeviceBytes + extraDeviceInfoBytes > 0)
			{
				var err = NativeMethods.cudaMalloc(out this.deviceBuffer, workSpaceDeviceBytes + extraDeviceInfoBytes);
				this.extraDeviceInfoOffset = extraDeviceInfoBytes == 0 ? -workSpaceDeviceBytes : workSpaceDeviceBytes;
				if (err != CudaError.Success)
				{
					this.Dispose();
					err.Check();
				}
			}
			else
			{
				this.deviceBuffer = default;
				this.extraDeviceInfoOffset = 0;
			}
		}

		/// <summary>
		/// Create a <see cref="CudaBuffer"/> by indicating the number of bytes required on current CUDA device and host memory
		/// </summary>
		/// <param name="workSpaceDeviceBytes">The number of bytes required as the working space on current CUDA device</param>
		/// <param name="workSpaceHostBytes">The number of bytes required as the working space on host</param>
		/// <param name="extraDeviceInfo">Whether to allocate the memory space for the extra device info (as a <see cref="int"/>) or not</param>
		/// <returns>The created <see cref="CudaBuffer"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="workSpaceDeviceBytes"/> or <paramref name="workSpaceHostBytes"/> is less than 0</exception>
		/// <exception cref="OutOfMemoryException">If the requested number of bytes are too large to be allocated</exception>
		/// <exception cref="StatusException">If the CUDA API call returns other error status</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static CudaBuffer Create(long workSpaceDeviceBytes, long workSpaceHostBytes = 0, bool extraDeviceInfo = true)
		{
			return new(workSpaceDeviceBytes, workSpaceHostBytes, extraDeviceInfo ? sizeof(int) : 0);
		}

		/// <summary>
		/// Create a <see cref="CudaBuffer"/> by indicating the number of elements (in <typeparamref name="T"/>) required on current CUDA device and host memory
		/// </summary>
		/// <typeparam name="T">The element type</typeparam>
		/// <param name="workSpaceDeviceT">The number of elements required on current CUDA device</param>
		/// <param name="workSpaceHostT">The number of elements required on host</param>
		/// <param name="extraDeviceInfo">Whether to allocate the memory space for the extra device info (as a <see cref="int"/>) or not</param>
		/// <returns>The created <see cref="CudaBuffer"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="workSpaceDeviceT"/> or <paramref name="workSpaceHostT"/> is less than 0</exception>
		/// <exception cref="OutOfMemoryException">If the requested number of bytes are too large to be allocated</exception>
		/// <exception cref="StatusException">If the CUDA API call returns other error status</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe CudaBuffer Create<T>(int workSpaceDeviceT, int workSpaceHostT = 0, bool extraDeviceInfo = true) where T : unmanaged, IBaseNumber<T>
		{
			return new((long)workSpaceDeviceT * sizeof(T), (long)workSpaceHostT * sizeof(T), extraDeviceInfo ? sizeof(int) : 0);
		}

		/// <summary>
		/// Release the not pooled allocated buffer(s) of this <see cref="CudaBuffer"/>
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Dispose()
		{
			if (this.hostBuffer is not null)
				ArrayPool<byte>.Shared.Return(this.hostBuffer);
			if (this.deviceBuffer != default)
				NativeMethods.cudaFree(this.deviceBuffer);
		}
	}
}
