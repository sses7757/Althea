using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Althea.Backend.Storage;
using Althea.Resources;


namespace Althea.Backend.Cuda.Storage
{
	/// <summary>
	/// An implementation of <see cref="Stream"/> for local files managed by CUDA file.
	/// </summary>
	public class CudaFileStream : FileStream
	{
		#region basic
		private readonly CudaFileHandle handle;

		private IntPtr gpuMem = IntPtr.Zero;

		private int gpuMemDeviceID = -1;

		/// <summary>
		/// Get the pointer to the GPU memory buffer of this <see cref="CudaFileStream"/>
		/// </summary>
		protected internal IntPtr GpuBufferPointer {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.gpuMem;
		}

		/// <summary>
		/// Get the pointer to the GPU memory buffer of this <see cref="CudaFileStream"/>
		/// </summary>
		protected internal int GpuBufferDeviceID {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.gpuMemDeviceID;
		}

		/// <summary>
		/// Get a <see cref="bool"/> indicating whether this <see cref="Stream"/> can transfer data with managed C# memory directly or not, always return false.
		/// </summary>
		public override bool CanTransferWithManaged {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => false;
		}

		/// <summary>
		/// <b>Statically</b> get the supported data transfer locations represented by <see cref="StorageLocation"/>s of this <see cref="Stream"/>. Not used.
		/// </summary>
		protected override IReadOnlyList<StorageLocation> SupportedTransfers { get; } = new[] { new StorageLocation(LocationType.GpuRam, 0) };

		/// <summary>
		/// <b>Statically</b> get a <see cref="bool"/> indicating whether data transfer with given <paramref name="location"/> is supported by this <see cref="Stream"/>. The default implementation utilizes the <see cref="SupportedTransfers"/>.
		/// </summary>
		/// <param name="location">The given <see cref="StorageLocation"/> to check transfer supporting</param>
		/// <returns>Whether data transfer with <paramref name="location"/> is supported or not</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override bool IsSupported(StorageLocation location) => location.Type == LocationType.GpuRam;

		/// <summary>
		/// Create a new <see cref="CudaFileStream"/> with given <see cref="Uri"/> of file
		/// </summary>
		/// <param name="uri">The given <see cref="Uri"/> of file scheme</param>
		/// <param name="length">The initial length in bytes</param>
		/// <param name="readOnly">Shall the file at <paramref name="uri"/> be opened as read-only or not. If this is true, then <paramref name="length"/> will be set to the value of the actual file length.</param>
		/// <exception cref="NotSupportedException">If the scheme of <paramref name="uri"/> is not file or the stream cannot be created by given <paramref name="uri"/></exception>
		/// <exception cref="System.IO.IOException">If other I/O error occurred</exception>
		/// <exception cref="UnauthorizedAccessException">If the give path in <paramref name="uri"/> cannot be created or overwritten</exception>
		public CudaFileStream(Uri uri, long length, bool readOnly = false) : base(uri, length, readOnly)
		{
			// CUDA file
			CudaFileDescription descr = new(Environment.OSVersion.Platform == PlatformID.Unix ? CudaFileHandleType.OpaqueLinux : CudaFileHandleType.OpaqueWindows, this.stream.SafeFileHandle.DangerousGetHandle());
			var err = NativeMethods.cuFileHandleRegister(out this.handle, ref descr);
			if (!err.IsSuccess)
			{
				this.stream.Dispose();
				System.IO.File.Delete(this.stream.Name);
				err.Check();
			}
		}

		/// <summary>
		/// Allocate a buffer of given <paramref name="length"/> on current CUDA device and register it as CUDA file buffer of this <see cref="CudaFileStream"/>
		/// </summary>
		/// <param name="length">The size of the buffer in bytes which will be automatically deregistered and freed when disposing this <see cref="CudaFileStream"/></param>
		/// <returns>Success or not. If this method was invoked and returned true before, returns false; otherwise, true.</returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="length"/> is not a positive number</exception>
		/// <exception cref="StatusException">If some error occurred during CUDA API call</exception>
		/// <remarks>By registering a buffer GPU memory and only write read file to/from this buffer, some overheads can be eliminated.</remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected internal bool AllocateAndRegisterBuffer(long length)
		{
			if (length <= 0)
				throw new ArgumentOutOfRangeException(nameof(length), length, Parameter.MustPositive);
			if (this.gpuMem != IntPtr.Zero)
				return false;
			NativeMethods.cudaMalloc(out this.gpuMem, length).Check();
			try
			{
				this.gpuMemDeviceID = CudaRuntime.CurrentDeviceID;
				NativeMethods.cuFileBufRegister(this.gpuMem, length, 0).Check();
				return true;
			}
			catch (Exception)
			{
				NativeMethods.cudaFree(this.gpuMem);
				throw;
			}
		}

		/// <summary>
		/// Actually release the unmanaged (and possibly managed) resources held by this class
		/// </summary>
		/// <param name="disposeManaged">Dispose managed resources or not</param>
		protected override void Dispose(bool disposeManaged)
		{
			try
			{
				base.Dispose(disposeManaged);
				if (this.gpuMem != IntPtr.Zero)
				{
					NativeMethods.cuFileBufDeregister(this.gpuMem);
					NativeMethods.cudaFree(this.gpuMem);
				}
				NativeMethods.cuFileHandleDeregister(in this.handle);
			}
			catch (Exception) { }
		}

		/// <summary>
		/// Get the string representation of this <see cref="Stream"/>.
		/// </summary>
		/// <returns>The string representation of this <see cref="Stream"/></returns>
		public override string ToString()
		{
			if (this.gpuMem == IntPtr.Zero)
				return nameof(CudaFileStream) + $" [Path=\"{this.stream.Name}\", CudaFileHandle={this.handle}]";
			else
				return nameof(CudaFileStream) + $" [Path=\"{this.stream.Name}\", CudaFileHandle={this.handle}, Buffer={this.gpuMem:X}]";
		}
		#endregion

		#region implementations
		/// <summary>
		/// Clears all buffers for this stream and causes any buffered data to be written to the underlying device.
		/// </summary>
		/// <exception cref="ObjectDisposedException">If this is already disposed</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override void Flush()
		{
			// do nothing
		}

		/// <summary>
		/// Read data from this <see cref="Stream"/> started from <see cref="FileStream.Position"/> byte and write them to the given <see cref="PointerSegment"/> <paramref name="memory"/>.
		/// </summary>
		/// <param name="memory">The <see cref="PointerSegment"/> to write to</param>
		/// <remarks>When finished, the <see cref="FileStream.Position"/> shall be advanced by the number of bytes read.</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="memory"/> is not valid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="memory"/>.<see cref="PointerSegment.LengthInBytes">Length</see> exceeds the boundary of this <see cref="Stream"/></exception>
		/// <exception cref="NotSupportedException">If <paramref name="memory"/>.<see cref="PointerSegment.Location">Location</see> is not supported</exception>
		/// <exception cref="System.IO.IOException">If a general I/O error occurred</exception>
		/// <exception cref="ObjectDisposedException">If this is already disposed</exception>
		public override void ToMemory(PointerSegment memory)
		{
			var mp = this.ToMemoryCheck(memory);

			long size = memory.LengthInBytes, offset = memory.OffsetInBytes;
			if (size + this.Position > this.Length)
				throw new ArgumentException(Parameter.WrongSize, nameof(memory));
			IntPtr p = mp.Pointer;
			if (p != this.gpuMem)
				unsafe
				{
					p = (IntPtr)((byte*)p.ToPointer() + memory.OffsetInBytes);
					offset = 0;
				}
			NativeMethods.cuFileRead(this.handle, p, size, this.Position, offset).Check();
		}

		/// <summary>
		/// Read data from this <see cref="Stream"/> started from <see cref="FileStream.Position"/> and write them to the given <paramref name="managed"/> memory as a<see cref="Span{T}"/>.
		/// </summary>
		/// <param name="managed">The managed memory as a <see cref="Span{T}"/> to write into</param>
		/// <exception cref="NotSupportedException">Direct data transfer with managed memory is not supported by CuFile</exception>
		public override void ToManged<T>(Span<T> managed)
		{
			throw new NotSupportedException(Support.Location);
		}

		/// <summary>
		/// Read data from the given <see cref="PointerSegment"/> <paramref name="memory"/> and write them to this <see cref="Stream"/> started from <see cref="FileStream.Position"/> byte.
		/// </summary>
		/// <param name="memory">The <see cref="PointerSegment"/> to read from</param>
		/// <remarks>When finished, the <see cref="FileStream.Position"/> shall be advanced by the number of bytes written.</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="memory"/> is not valid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="memory"/>.<see cref="PointerSegment.LengthInBytes">Length</see> exceeds the boundary of this <see cref="Stream"/></exception>
		/// <exception cref="NotSupportedException">If <paramref name="memory"/>.<see cref="PointerSegment.Location">Location</see> is not supported</exception>
		/// <exception cref="System.IO.IOException">If a general I/O error occurred</exception>
		/// <exception cref="ObjectDisposedException">If this is already disposed</exception>
		/// <exception cref="UnauthorizedAccessException">If this <see cref="Stream"/> was created read-only</exception>
		public override void FromMemory(PointerSegment memory)
		{
			var mp = FromMemoryCheck(memory);

			long size = memory.LengthInBytes, offset = memory.OffsetInBytes;
			IntPtr p = mp.Pointer;
			if (p != this.gpuMem)
				unsafe
				{
					p = (IntPtr)((byte*)p.ToPointer() + memory.OffsetInBytes);
					offset = 0;
				}
			NativeMethods.cuFileWrite(this.handle, p, size, this.Position, offset).Check();
		}

		/// <summary>
		/// Read data from the given <paramref name="managed"/> memory as a<see cref="Span{T}"/> and write them to this <see cref="Stream"/> started from <see cref="FileStream.Position"/>.
		/// </summary>
		/// <param name="managed">The managed memory as a <see cref="ReadOnlySpan{T}"/> to read from</param>
		/// <exception cref="NotSupportedException">Direct data transfer with managed memory is not supported by CuFile</exception>
		public override void FromManged<T>(ReadOnlySpan<T> managed)
		{
			throw new NotSupportedException(Support.Location);
		}

		/// <summary>
		/// Copy some data from this <see cref="CudaFileStream"/> to <paramref name="other"/> <see cref="CudaFileStream"/> of given <paramref name="length"/>.
		/// </summary>
		/// <param name="other">The other <see cref="CudaFileStream"/> to copy to</param>
		/// <param name="length">The length in bytes to copy</param>
		/// <remarks>This method allocates an internal buffer, to avoid that, use <see cref="CopyTo(CudaFileStream, long, IntPtr, long, bool)"/> instead</remarks>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="length"/> exceeds any of the boundaries</exception>
		/// <exception cref="NotSupportedException">If <paramref name="other"/> is not a <see cref="CudaFileStream"/></exception>
		/// <exception cref="System.IO.IOException">If an I/O error occurs</exception>
		/// <exception cref="ObjectDisposedException">If this or <paramref name="other"/> is already disposed</exception>
		/// <exception cref="UnauthorizedAccessException">If the <paramref name="other"/> <see cref="Stream"/> was created read-only</exception>
		public override void CopyTo(Stream other, long length)
		{
			this.CopyToCheck(other, length);
			if (other is not CudaFileStream cf)
				throw new NotSupportedException(Support.DataType);

			int bufferSize = BufferSizeInBytes<byte>();
			NativeMethods.cudaMalloc(out IntPtr buf, Math.Min(bufferSize, length)).Check();
			try
			{
				if (length <= bufferSize)
				{   // do not register
					NativeMethods.cuFileRead(this.handle, buf, length, this.Position, 0).Check();
					NativeMethods.cuFileWrite(cf.handle, buf, length, cf.Position, 0).Check();
				}
				else
				{
					NativeMethods.cuFileBufRegister(buf, bufferSize, 0).Check();
					try
					{
						long offset = 0;
						while (offset < length)
						{
							long copyLen = Math.Min(length - offset, bufferSize);
							NativeMethods.cuFileRead(this.handle, buf, copyLen, this.Position, offset).Check();
							NativeMethods.cuFileWrite(cf.handle, buf, copyLen, cf.Position, offset).Check();
							offset += bufferSize;
						}
					}
					finally
					{
						NativeMethods.cuFileBufDeregister(buf);
					}
				}
			}
			finally
			{
				NativeMethods.cudaFree(buf);
			}
		}

		/// <summary>
		/// Copy some data from this <see cref="CudaFileStream"/> to <paramref name="other"/> <see cref="CudaFileStream"/> of given <paramref name="length"/>.
		/// </summary>
		/// <param name="other">The other <see cref="CudaFileStream"/> to copy to</param>
		/// <param name="length">The length in bytes to copy</param>
		/// <param name="buffer">The pre-allocated GPU memory as the copying buffer</param>
		/// <param name="bufferSize">The size of <paramref name="buffer"/> in bytes</param>
		/// <param name="doRegister">Whether to register and deregister the <paramref name="buffer"/> internally or not if <paramref name="bufferSize"/> is smaller than <paramref name="length"/></param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="length"/> or <paramref name="bufferSize"/> exceeds any of the boundaries</exception>
		/// <exception cref="System.IO.IOException">If an I/O error occurs</exception>
		/// <exception cref="ObjectDisposedException">If this or <paramref name="other"/> is already disposed</exception>
		/// <exception cref="UnauthorizedAccessException">If the <paramref name="other"/> <see cref="Stream"/> was created read-only</exception>
		public virtual void CopyTo(CudaFileStream other, long length, IntPtr buffer, long bufferSize, bool doRegister)
		{
			this.CopyToCheck(other, length);

			if (length <= bufferSize)
			{   // do not register
				NativeMethods.cuFileRead(this.handle, buffer, length, this.Position, 0).Check();
				NativeMethods.cuFileWrite(other.handle, buffer, length, other.Position, 0).Check();
			}
			else
			{
				if (doRegister)
					NativeMethods.cuFileBufRegister(buffer, bufferSize, 0).Check();
				try
				{
					long offset = 0;
					while (offset < length)
					{
						long copyLen = Math.Min(length - offset, bufferSize);
						NativeMethods.cuFileRead(this.handle, buffer, copyLen, this.Position, offset).Check();
						NativeMethods.cuFileWrite(other.handle, buffer, copyLen, other.Position, offset).Check();
						offset += bufferSize;
					}
				}
				finally
				{
					if (doRegister)
						NativeMethods.cuFileBufDeregister(buffer);
				}
			}
		}

		/// <summary>
		/// Fill some values of this <see cref="Stream"/> of given <paramref name="length"/>.
		/// </summary>
		/// <typeparam name="T">any unmanaged data type</typeparam>
		/// <param name="value">The value of type <typeparamref name="T"/> to be set</param>
		/// <param name="length">The length in <typeparamref name="T"/></param>
		/// <remarks>This method allocates an internal buffer, to avoid that, use <see cref="SetValues{T}(T, long, IntPtr, long, bool)"/> instead</remarks>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="length"/> exceeds any of the boundaries</exception>
		/// <exception cref="System.IO.IOException">If an I/O error occurs</exception>
		/// <exception cref="ObjectDisposedException">If this is already disposed</exception>
		/// <exception cref="UnauthorizedAccessException">If this <see cref="Stream"/> was created read-only</exception>
		public override void SetValues<T>(T value, long length)
		{
			length = this.SetValuesCheck<T>(length);

			int bufferSize = BufferSizeInBytes<byte>();
			NativeMethods.cudaMalloc(out IntPtr buf, Math.Min(bufferSize, length)).Check();
			try
			{
				PointerSegment ptr = new(new MemoryPointer(buf, bufferSize, new(LocationType.GpuRam, 0)));
				StorageApi.FillWithValue(ptr, value);
				if (length <= bufferSize)
				{
					NativeMethods.cuFileWrite(this.handle, buf, length, this.Position, 0).Check();
				}
				else
				{
					NativeMethods.cuFileBufRegister(buf, bufferSize, 0).Check();
					try
					{
						long offset = 0;
						while (offset < length)
						{
							long copyLen = Math.Min(length - offset, bufferSize);
							NativeMethods.cuFileWrite(this.handle, buf, copyLen, this.Position, offset).Check();
							offset += bufferSize;
						}
					}
					finally
					{
						NativeMethods.cuFileBufDeregister(buf);
					}
				}
			}
			finally
			{
				NativeMethods.cudaFree(buf);
			}
		}

		/// <summary>
		/// Fill some values of this <see cref="Stream"/> of given <paramref name="length"/>.
		/// </summary>
		/// <typeparam name="T">any unmanaged data type</typeparam>
		/// <param name="value">The value of type <typeparamref name="T"/> to be set</param>
		/// <param name="length">The length in <typeparamref name="T"/></param>
		/// <param name="buffer">The pre-allocated GPU memory as the copying buffer</param>
		/// <param name="bufferSize">The size of <paramref name="buffer"/> in bytes</param>
		/// <param name="doRegister">Whether to register and deregister the <paramref name="buffer"/> internally or not if <paramref name="bufferSize"/> is smaller than <paramref name="length"/></param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="length"/> exceeds any of the boundaries</exception>
		/// <exception cref="System.IO.IOException">If an I/O error occurs</exception>
		/// <exception cref="ObjectDisposedException">If this is already disposed</exception>
		/// <exception cref="UnauthorizedAccessException">If this <see cref="Stream"/> was created read-only</exception>
		public void SetValues<T>(T value, long length, IntPtr buffer, long bufferSize, bool doRegister) where T : unmanaged
		{
			length = this.SetValuesCheck<T>(length);

			PointerSegment ptr = new(new MemoryPointer(buffer, bufferSize, new(LocationType.GpuRam, 0)));
			StorageApi.FillWithValue(ptr, value);
			if (length <= bufferSize)
			{   // do not register
				NativeMethods.cuFileWrite(this.handle, buffer, length, this.Position, 0).Check();
			}
			else
			{
				if (doRegister)
					NativeMethods.cuFileBufRegister(buffer, bufferSize, 0).Check();
				try
				{
					long offset = 0;
					while (offset < length)
					{
						long copyLen = Math.Min(length - offset, bufferSize);
						NativeMethods.cuFileWrite(this.handle, buffer, copyLen, this.Position, offset).Check();
						offset += bufferSize;
					}
				}
				finally
				{
					if (doRegister)
						NativeMethods.cuFileBufDeregister(buffer);
				}
			}
		}
		#endregion
	}
}
