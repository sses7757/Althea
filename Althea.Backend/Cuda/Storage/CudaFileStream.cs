using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Althea.Backend.Storage;
using Althea.NativeTypes;
using Althea.Resources;
using Althea.Storage;


namespace Althea.Backend.Cuda.Storage
{
	/// <summary>
	/// An implementation of <see cref="Stream"/> for local files managed by CUDA file.
	/// </summary>
	public class CudaFileStream : Stream
	{
		#region basic
		private readonly System.IO.FileStream stream;

		private IntPtr gpuMem = IntPtr.Zero;

		private readonly CudaFileHandle handle;

		private long position = 0, gpuMemSize = 0;

		/// <summary>
		/// Get or set the position (offset) in bytes of this <see cref="FileStream"/>
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">If the value to set is out of range</exception>
		public override long Position {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.position;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set {
				if (value < 0)
					throw new ArgumentOutOfRangeException(nameof(value));
				this.position = value;
			}
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
		/// <exception cref="NotSupportedException">If the scheme of <paramref name="uri"/> is not file or the stream cannot be created by given <paramref name="uri"/></exception>
		/// <exception cref="System.IO.IOException">If other I/O error occurred</exception>
		/// <exception cref="UnauthorizedAccessException">If the give path in <paramref name="uri"/> cannot be created or overwritten</exception>
		public CudaFileStream(Uri uri, long length) : base(length)
		{
			if (uri.GetScheme() != UriScheme.File)
				throw new NotSupportedException(Support.Location);
			// check
			string path = uri.LocalPath;
			if (System.IO.File.Exists(path))
			{
				var flags = System.IO.File.GetAttributes(path);
				if ((flags & (System.IO.FileAttributes.ReadOnly | System.IO.FileAttributes.System | System.IO.FileAttributes.Directory)) != 0)
					throw new NotSupportedException(Support.Location);
			}
			else
				throw new NotSupportedException(Support.Location);
			string folder = System.IO.Path.GetDirectoryName(path) ?? "";
			if (!string.IsNullOrEmpty(folder) && !System.IO.Directory.Exists(folder))
			{
				System.IO.Directory.CreateDirectory(folder);
			}
			var platform = Environment.OSVersion.Platform;
			if (platform == PlatformID.Other)
				throw new NotSupportedException(Support.OperationSystem);
			// create
			this.stream = new System.IO.FileStream(path, System.IO.FileMode.Create, System.IO.FileAccess.ReadWrite);
			this.stream.SetLength(length);
			this.stream.Flush();
			// CUDA file
			CudaFileDescription descr = new(Environment.OSVersion.Platform == PlatformID.Unix ? CudaFileHandleType.OpaqueLinux : CudaFileHandleType.OpaqueWindows, this.stream.SafeFileHandle.DangerousGetHandle());
			var err = NativeMethods.cuFileHandleRegister(ref this.handle, ref descr);
			if (!err.IsSuccess)
			{
				this.stream.Dispose();
				System.IO.File.Delete(this.stream.Name);
				// TODO: throw
			}
		}

		/// <summary>
		/// Allocate a buffer of given <paramref name="length"/> on current CUDA device and register it as CUDA file buffer of this <see cref="CudaFileStream"/>
		/// </summary>
		/// <param name="length">The size of the buffer in bytes</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="length"/> is not a positive number</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected void AllocateAndRegisterBuffer(long length)
		{
			if (length <= 0)
				throw new ArgumentOutOfRangeException(nameof(length), length, Parameter.MustPositive);
			var err1 = NativeMethods.cudaMalloc(ref this.gpuMem, length);
			// TODO: conditionally throw
			var err2 = NativeMethods.cuFileBufRegister(this.gpuMem, length, 0);
			// TODO: conditionally throw
			this.gpuMemSize = length;
		}

		/// <summary>
		/// Actually release the unmanaged (and possibly managed) resources held by this class
		/// </summary>
		/// <param name="disposeManaged">Dispose managed resources or not</param>
		protected override void Dispose(bool disposeManaged)
		{
			try
			{
				if (this.gpuMemSize != 0)
				{
					NativeMethods.cuFileBufDeregister(this.gpuMem);
					NativeMethods.cudaFree(this.gpuMem);
				}
				NativeMethods.cuFileHandleDeregister(in this.handle);
				this.stream.Dispose();
				System.IO.File.Delete(this.stream.Name);
			}
			catch (Exception) { }
		}

		/// <summary>
		/// Get the string representation of this <see cref="Stream"/>.
		/// </summary>
		/// <returns>The string representation of this <see cref="Stream"/></returns>
		public override string ToString()
		{
			if (this.gpuMemSize == 0)
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
			if (this.Disposed)
				throw new ObjectDisposedException(nameof(FileStream));
			long err = NativeMethods.cuFileWrite(this.handle, this.gpuMem, this.gpuMemSize, this.position, 0);
			// TODO: conditionally throw
		}

		/// <summary>
		/// Read data from this <see cref="Stream"/> started from <see cref="Position"/> byte and write them to the given <see cref="PointerSegment"/> <paramref name="memory"/>.
		/// </summary>
		/// <param name="memory">The <see cref="PointerSegment"/> to write to</param>
		/// <remarks>When finished, the <see cref="Position"/> shall be advanced by the number of bytes read.</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="memory"/> is not valid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="memory"/>.<see cref="PointerSegment.LengthInBytes">Length</see> exceeds the boundary of this <see cref="Stream"/></exception>
		/// <exception cref="NotSupportedException">If <paramref name="memory"/>.<see cref="PointerSegment.Location">Location</see> is not supported</exception>
		/// <exception cref="System.IO.IOException">If a general I/O error occurred</exception>
		/// <exception cref="ObjectDisposedException">If this is already disposed</exception>
		public override void ToMemory(PointerSegment memory)
		{
			if (!memory.IsValid())
				throw new ArgumentNullException(nameof(memory));
			if (this.IsSupported(memory.Location))
				throw new NotSupportedException(Support.Location);
			if (memory.Pointer is not IMemoryPointer mp)
				throw new NotSupportedException(Support.Location);
			// other checks in ToManged
			
		}

		/// <summary>
		/// Read data from this <see cref="Stream"/> started from <see cref="Position"/> and write them to the given <paramref name="managed"/> memory as a<see cref="Span{T}"/>.
		/// </summary>
		/// <param name="managed">The managed memory as a <see cref="Span{T}"/> to write into</param>
		/// <exception cref="NotSupportedException">Direct data transfer with managed memory is not supported by CuFile</exception>
		public override void ToManged<T>(Span<T> managed)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Read data from the given <see cref="PointerSegment"/> <paramref name="memory"/> and write them to this <see cref="Stream"/> started from <see cref="Position"/> byte.
		/// </summary>
		/// <param name="memory">The <see cref="PointerSegment"/> to read from</param>
		/// <remarks>When finished, the <see cref="Position"/> shall be advanced by the number of bytes written.</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="memory"/> is not valid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="memory"/>.<see cref="PointerSegment.LengthInBytes">Length</see> exceeds the boundary of this <see cref="Stream"/></exception>
		/// <exception cref="NotSupportedException">If <paramref name="memory"/>.<see cref="PointerSegment.Location">Location</see> is not supported</exception>
		/// <exception cref="System.IO.IOException">If a general I/O error occurred</exception>
		/// <exception cref="ObjectDisposedException">If this is already disposed</exception>
		public override void FromMemory(PointerSegment memory)
		{
			if (!memory.IsValid())
				throw new ArgumentNullException(nameof(memory));
			if (this.IsSupported(memory.Location))
				throw new NotSupportedException(Support.Location);
			if (memory.Pointer is not IMemoryPointer mp)
				throw new NotSupportedException(Support.Location);
			// other checks in FromManged
			
		}

		/// <summary>
		/// Read data from the given <paramref name="managed"/> memory as a<see cref="Span{T}"/> and write them to this <see cref="Stream"/> started from <see cref="Position"/>.
		/// </summary>
		/// <param name="managed">The managed memory as a <see cref="ReadOnlySpan{T}"/> to read from</param>
		/// <exception cref="NotSupportedException">Direct data transfer with managed memory is not supported by CuFile</exception>
		public override void FromManged<T>(ReadOnlySpan<T> managed)
		{
			throw new NotSupportedException();
		}
		#endregion
	}
}
