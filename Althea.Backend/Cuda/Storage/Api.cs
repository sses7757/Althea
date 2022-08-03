using System.Collections.Concurrent;
using System.IO;
using System.Runtime.CompilerServices;

using Althea.Backend.Storage;
using Althea.Linq;


namespace Althea.Backend.Cuda.Storage
{
	/// <summary>
	/// The CUDA back-end of the <see cref="IAbstractApi"/> that supports data transfer between GPU, CPU and managed memories. May support GPUDirect® Storage that directly transfer data between files and GPU if the corresponding ABIs are found.
	/// </summary>
	public unsafe class Api : IAbstractApi, Althea.LinearAlgebra.Dense.ICopyAbstractApi
	{
		#region basic
		internal static readonly Api Default = new(false);

		private readonly ConcurrentDictionary<IntPtr, IntPtr>? fileBuffers;

		/// <summary>
		/// Create a <see cref="Api"/> with given meta data
		/// </summary>
		/// <param name="supportCuFile">Whether this class shall enables CUDA GPUDirect Storage or not</param>
		/// <remarks>If the invocation of <see cref="NativeMethods.cuFileDriverOpen"/> failed, the caller must invoke that method when available later</remarks>
		public Api(bool supportCuFile)
		{
			if (!supportCuFile)
				return;
			try
			{
				this.CudaFileSupported = NativeMethods.cuFileDriverOpen().IsSuccess;
				if (this.CudaFileSupported)
					this.fileBuffers = new();
			}
			catch (Exception e)
			{
				this.CudaFileSupported = false;
				Helpers.Log.Write($"Error occurred when opening CUDA file driver: {e}", level: Helpers.LogLevel.Error);
			}
		}

		/// <inheritdoc/>
		public bool Disposed { get; protected set; } = false;

		/// <inheritdoc/>
		public void Dispose()
		{
			if (this.CudaFileSupported)
			{
				var err = NativeMethods.cuFileDriverClose();
				if (!err.IsSuccess)
					Helpers.Log.Write($"Error occurred when closing CUDA file driver: {err.FileOpResult} ({err.DriverResult})", level: Helpers.LogLevel.Error);
				if (this.fileBuffers is not null)
				{
					foreach (var kv in this.fileBuffers)
					{
						var error = NativeMethods.cuFileBufDeregister(kv.Value);
						if (!error.IsSuccess)
							Helpers.Log.Write($"Error occurred when deregistering CUDA file buffer: {err.FileOpResult} ({err.DriverResult})", level: Helpers.LogLevel.Error);
					}
				}
			}
			this.Disposed = true;
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Get a <see cref="bool"/> indicating whether the GPUDirect® Storage is supported by this instance when initializing it.
		/// </summary>
		public bool CudaFileSupported { get; } = false;

		/// <summary>
		/// Get or set a <see cref="bool"/> indicating whether the GPU file's corresponding memory buffer shall be cached or not.
		/// </summary>
		public bool CudaFileCacheBuffer { get; set; } = false;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool Supported(StorageLocation location) => location.Type == LocationType.GpuRam || (this.CudaFileSupported && location == CudaFilePointer.Location);

		private string _fileFolder = Path.GetTempPath();

		/// <summary>
		/// Get or set the folder to put the temporary files, default is <see cref="Path.GetTempPath"/>
		/// </summary>
		/// <exception cref="ArgumentException">If the folder to set is not an existing folder</exception>
		/// <exception cref="UnauthorizedAccessException">If the program does not have permission to write to the folder</exception>
		/// <exception cref="IOException">Other I/O exceptions</exception>
		public string TempFileFolder {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this._fileFolder;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set {
				if (!Directory.Exists(value))
					throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(value));
				string testFile = Path.Combine(value, Path.GetRandomFileName());
				try
				{
					File.WriteAllText(testFile, " ");
				}
				finally
				{
					if (File.Exists(testFile))
						File.Delete(testFile);
				}
				this._fileFolder = value;
			}
		}

		private string TempFile {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				return Path.Combine(this.TempFileFolder, Guid.NewGuid().ToString());
			}
		}
		#endregion

		#region allocate and free
		/// <inheritdoc/>
		public virtual bool Allocate<TP>(long length, out TP result) where TP : notnull, IPointer<TP>
		{
			result = TP.Default;
			if (!this.Supported(TP.Location))
				return false;
			if (TP.Location.Type == LocationType.GpuRam)
			{
				if (Runtime.CurrentDeviceID != TP.Location.Detail)
					return false;
				var err = NativeMethods.cudaMalloc(out var pointer, length);
				if (err == CudaError.ErrorOutOfMemory)
					throw new OutOfMemoryException();
				err.Check();
				var ptr = new CudaMemoryPointer(pointer, length);
				result = Unsafe.As<CudaMemoryPointer, TP>(ref ptr);
			}
			else if (result is CudaFilePointer)
			{
				if (!this.CudaFileSupported)
					return false;
				var ptr = new CudaFilePointer(this.TempFile);
				result = Unsafe.As<CudaFilePointer, TP>(ref ptr);
			}
			else
				return false;
			return true;
		}

		/// <inheritdoc/>
		public virtual bool Free<TP>(TP pointer, out bool valid) where TP : notnull, IPointer<TP>
		{
			valid = false;
			var (gpu, file) = pointer.FromGeneric();
			if (gpu.IsValid())
			{
				NativeMethods.cudaFree(gpu.Pointer).Check();
			}
			else if (file.IsValid())
			{
				if (!this.CudaFileSupported)
					return false;
				file.Dispose();
				if (this.fileBuffers!.TryGetValue(file.Handle, out var ptr))
					NativeMethods.cuFileBufDeregister(ptr).Check();
			}
			else
				return false;
			valid = true;
			return true;
		}
		#endregion

		#region fill
		/// <inheritdoc/>
		public virtual bool FillWithValue<TP>(PointerSegment<TP> pointer, byte value) where TP : notnull, IPointer<TP>
		{
			var (gpu, file) = pointer.Pointer.FromGeneric();
			if (gpu.IsValid())
			{
				NativeMethods.cudaMemset(gpu.NativePointer(pointer), value, pointer.LengthInBytes).Check();
			}
			else if (file.IsValid())
			{
				if (!file.CanWrite)
					throw new InvalidOperationException();
				using var buffer = CudaBuffer.Create(4096, 0, false);
				NativeMethods.cudaMemset(buffer.DeviceBuffer.ToPointer(), value, 4096).Check();
				using CudaFileBuffer buf = buffer;
				long end = pointer.LengthInBytes + pointer.OffsetInBytes, start = (pointer.OffsetInBytes + 4095) >> 12 << 12;
				NativeMethods.cuFileWrite(file.Handle, buf, Math.Min(start, end) - pointer.OffsetInBytes, pointer.OffsetInBytes, 0).Check();
				for (long i = start; i < end; i += 4096)
				{
					long size = Math.Min(i + 4096, end) - i;
					NativeMethods.cuFileWrite(file.Handle, buf, size, i, 0).Check();
				}
			}
			else
				return false;
			return true;
		}

		/// <inheritdoc/>
		public virtual bool FillWithValue<T, TP>(PointerSegment<TP> pointer, T value) where TP : notnull, IPointer<TP> where T : unmanaged, IBaseNumber<T>
		{
			if (value.AllBytesSame())
				return FillWithValue(pointer, *(byte*)&value);
			var (gpu, file) = pointer.Pointer.FromGeneric();
			if (gpu.IsValid())
			{
				if (NativeMethods.vecFillVal(T.Type, gpu.NativePointer(pointer), &value, pointer.LengthInBytes / sizeof(T), 1) < 0)
					return false;
			}
			else if (file.IsValid())
			{
				if (!file.CanWrite)
					throw new InvalidOperationException();
				using var buffer = CudaBuffer.Create(4096 + sizeof(T), 0, false);
				if (NativeMethods.vecFillVal(T.Type, buffer.DeviceBuffer.ToPointer(), &value, 4096 / sizeof(T) + 1, 1) < 0)
					return false;
				using CudaFileBuffer buf = buffer;
				long end = pointer.LengthInBytes + pointer.OffsetInBytes, start = (pointer.OffsetInBytes + 4095) >> 12 << 12;
				long mod = sizeof(T) - (pointer.OffsetInBytes % sizeof(T));
				NativeMethods.cuFileWrite(file.Handle, buf, Math.Min(start, end) - pointer.OffsetInBytes, pointer.OffsetInBytes, 0).Check();
				for (long i = start; i < end; i += 4096)
				{
					long size = Math.Min(i + 4096, end) - i;
					NativeMethods.cuFileWrite(file.Handle, buf, size, i, mod).Check();
				}
			}
			else
				return false;
			return true;
		}
		#endregion

		#region copy
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static MemoryCopyKind GetCopyKind(CpuMemoryPointer src, CpuMemoryPointer dst)
		{
			bool srcHost = src.IsValid(), dstHost = dst.IsValid();
			MemoryCopyKind copyKind;
			if (srcHost && dstHost)
				copyKind = MemoryCopyKind.HostToHost;
			else if (srcHost && !dstHost)
				copyKind = MemoryCopyKind.HostToDevice;
			else if (!srcHost && dstHost)
				copyKind = MemoryCopyKind.DeviceToHost;
			else
				copyKind = MemoryCopyKind.DeviceToDevice;
			return copyKind;
		}

		/// <inheritdoc/>
		public virtual bool MemoryCopy<T, TP1, TP2>(PointerSegment<TP1> source, PointerSegment<TP2> destination, out long actualCopied) where TP1 : notnull, IPointer<TP1> where TP2 : notnull, IPointer<TP2> where T : unmanaged, IBaseNumber<T>
		{
			actualCopied = Math.Min(source.LengthInBytes / sizeof(T), destination.LengthInBytes / sizeof(T)) * sizeof(T);
			var (gpuSrc, fileSrc) = source.Pointer.FromGeneric();
			var (gpuDst, fileDst) = destination.Pointer.FromGeneric();
			if (!this.CudaFileSupported && (fileSrc.IsValid() || fileDst.IsValid()))
				return false;
			CpuMemoryPointer cpuSrc = default, cpuDst = default;
			if (gpuSrc == default && fileSrc == default)
			{
				if (source.Pointer is not CpuMemoryPointer cpu)
					return false;
				cpuSrc = cpu;
			}
			if (gpuDst == default && fileDst == default)
			{
				if (destination.Pointer is not CpuMemoryPointer cpu)
					return false;
				cpuDst = cpu;
			}
			if (fileSrc == default && fileDst == default)
			{
				NativeMethods.cudaMemcpy(gpuDst.NativePointer(destination), gpuSrc.NativePointer(source), actualCopied, GetCopyKind(cpuSrc, cpuDst)).Check();
			}
			else if (gpuSrc.IsValid() && fileDst.IsValid())
			{
				if (!fileDst.CanWrite)
					throw new InvalidOperationException();
				bool cached = this.fileBuffers!.TryGetValue(fileDst.Handle, out var ptr) && ptr == gpuSrc.Pointer;
				using var buf = CudaFileBuffer.Create(gpuSrc.Pointer, gpuSrc.LengthInBytes, cached);
				NativeMethods.cuFileWrite(fileDst.Handle, gpuSrc.Pointer, actualCopied, destination.OffsetInBytes, source.OffsetInBytes).Check();
			}
			else if (fileSrc.IsValid() && gpuDst.IsValid())
			{
				bool cached = this.fileBuffers!.TryGetValue(fileSrc.Handle, out var ptr) && ptr == gpuDst.Pointer;
				using var buf = CudaFileBuffer.Create(gpuDst.Pointer, gpuDst.LengthInBytes, cached);
				NativeMethods.cuFileRead(fileSrc.Handle, gpuDst.Pointer, actualCopied, source.OffsetInBytes, destination.OffsetInBytes).Check();
			}
			else if (fileSrc.IsValid() && fileDst.IsValid())
			{
				// large buffer to mitigate misalignment performance loss
				using var buffer = CudaBuffer.Create(Math.Min(actualCopied, 65536), 0, false);
				using CudaFileBuffer buf = buffer;
				for (long i = 0; i < actualCopied; i += 65536)
				{
					long size = Math.Min(actualCopied, i + 65536) - i;
					NativeMethods.cuFileRead(fileSrc.Handle, buf, size, source.OffsetInBytes + i, 0).Check();
					NativeMethods.cuFileWrite(fileDst.Handle, buf, size, destination.OffsetInBytes + i, 0).Check();
				}
			}
			else
				return false;
			actualCopied /= sizeof(T);
			return true;
		}
		#endregion

		#region 2D copy
		/// <inheritdoc/>
		public virtual bool MemoryCopy2D<T, TP1, TP2>(PointerSegment<TP1> source, long sourceLD, PointerSegment<TP2> destination, long destinationLD, long height, long width, out long copyWidth) where TP1 : notnull, IPointer<TP1> where TP2 : notnull, IPointer<TP2> where T : unmanaged, IBaseNumber<T>
		{
			if (width == 0)
				width = Math.Min((source.LengthInBytes / sizeof(T) + sourceLD - height) / sourceLD, (destination.LengthInBytes / sizeof(T) + destinationLD - height) / destinationLD);
			copyWidth = width;
			sourceLD *= sizeof(T); destinationLD *= sizeof(T); height *= sizeof(T);
			if (sourceLD == destinationLD && sourceLD == height)
				return MemoryCopy<T, TP1, TP2>(source.AsLength(width * height), destination, out _);

			var (gpuSrc, fileSrc) = source.Pointer.FromGeneric();
			var (gpuDst, fileDst) = destination.Pointer.FromGeneric();
			if (!this.CudaFileSupported && (fileSrc.IsValid() || fileDst.IsValid()))
				return false;
			CpuMemoryPointer cpuSrc = default, cpuDst = default;
			if (gpuSrc == default && fileSrc == default)
			{
				if (source.Pointer is not CpuMemoryPointer cpu)
					return false;
				cpuSrc = cpu;
			}
			if (gpuDst == default && fileDst == default)
			{
				if (destination.Pointer is not CpuMemoryPointer cpu)
					return false;
				cpuDst = cpu;
			}

			if (fileSrc == default && fileDst == default)
			{
				NativeMethods.cudaMemcpy2D(gpuDst.NativePointer(destination), destinationLD, gpuSrc.NativePointer(source), sourceLD, height, width, GetCopyKind(cpuSrc, cpuDst)).Check();
			}
			else if (gpuSrc.IsValid() && fileDst.IsValid())
			{
				if (!fileDst.CanWrite)
					throw new InvalidOperationException();
				bool cached = this.fileBuffers!.TryGetValue(fileDst.Handle, out var ptr) && ptr == gpuSrc.Pointer;
				using var buf = CudaFileBuffer.Create(gpuSrc.Pointer, gpuSrc.LengthInBytes, cached);
				for (long i = 0; i < width; i++)
				{
					NativeMethods.cuFileWrite(fileDst.Handle, gpuSrc.Pointer, height, destination.OffsetInBytes + i * destinationLD, source.OffsetInBytes + i * sourceLD).Check();
				}
			}
			else if (fileSrc.IsValid() && gpuDst.IsValid())
			{
				bool cached = this.fileBuffers!.TryGetValue(fileSrc.Handle, out var ptr) && ptr == gpuDst.Pointer;
				using var buf = CudaFileBuffer.Create(gpuDst.Pointer, gpuDst.LengthInBytes, cached);
				for (long i = 0; i < width; i++)
				{
					NativeMethods.cuFileRead(fileSrc.Handle, gpuDst.Pointer, height, source.OffsetInBytes + i * sourceLD, destination.OffsetInBytes + i * destinationLD).Check();
				}
			}
			else if (fileSrc.IsValid() && fileDst.IsValid())
			{
				using var buffer = CudaBuffer.Create(height, 0, false);
				using CudaFileBuffer buf = buffer;
				for (long i = 0; i < width; i++)
				{
					NativeMethods.cuFileRead(fileSrc.Handle, buf, height, source.OffsetInBytes + i * sourceLD, 0).Check();
					NativeMethods.cuFileWrite(fileDst.Handle, buf, height, destination.OffsetInBytes + i * destinationLD, 0).Check();
				}
			}
			else
				return false;
			return true;
		}
		#endregion

		#region strided copy
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool PointerStridedCopy<T>(void* source, int incrementSource, void* destination, int incrementDestination, MemoryCopyKind copyKind, long copies) where T : unmanaged, IBaseNumber<T>
		{
			if (incrementSource == 1 && incrementDestination == 1)
			{
				NativeMethods.cudaMemcpy(destination, source, sizeof(T) * copies, copyKind).Check();
				return true;
			}
			switch (copyKind)
			{
				case MemoryCopyKind.HostToDevice:
					if (copies > int.MaxValue)
						return false;
					LinearAlgebra.Dense.NativeMethods.cublasSetVector((int)copies, sizeof(T), source, incrementSource, destination, incrementDestination).Check();
					break;
				case MemoryCopyKind.DeviceToHost:
					if (copies > int.MaxValue)
						return false;
					LinearAlgebra.Dense.NativeMethods.cublasGetVector((int)copies, sizeof(T), source, incrementSource, destination, incrementDestination).Check();
					break;
				case MemoryCopyKind.DeviceToDevice:
					return NativeMethods.vecStridedCopy(T.Type, source, destination, copies, incrementSource, incrementDestination) == 0;
				case MemoryCopyKind.HostToHost:
					if (copies > int.MaxValue)
						return false;
					CSharp.Storage.Api.StridedCopy((T*)source, (T*)destination, incrementSource, incrementDestination, (int)copies);
					return true;
				default:
					return false;
			}
			return true;
		}

		/// <inheritdoc/>
		public virtual bool StridedCopy<T, TP1, TP2>(PointerSegment<TP1> source, long incrementSource, PointerSegment<TP2> destination, long incrementDestination, out long actualCopied) where TP1 : notnull, IPointer<TP1> where TP2 : notnull, IPointer<TP2> where T : unmanaged, IBaseNumber<T>
		{
			// shortcut
			if (incrementSource == 1 && incrementDestination == 1)
			{
				return this.MemoryCopy<T, TP1, TP2>(source, destination, out actualCopied);
			}
			actualCopied = (source.LengthInBytes / sizeof(T) + incrementSource - 1) / incrementSource;
			actualCopied = Math.Min(actualCopied, (destination.LengthInBytes / sizeof(T) + incrementDestination - 1) / incrementDestination);
			if (incrementSource > int.MaxValue || incrementDestination > int.MaxValue)
				return false;

			var (gpuSrc, fileSrc) = source.Pointer.FromGeneric();
			var (gpuDst, fileDst) = destination.Pointer.FromGeneric();
			if (fileSrc.IsValid() || fileDst.IsValid())
				return false;
			CpuMemoryPointer cpuSrc = default, cpuDst = default;
			if (gpuSrc == default && fileSrc == default)
			{
				if (source.Pointer is not CpuMemoryPointer cpu)
					return false;
				cpuSrc = cpu;
			}
			if (gpuDst == default && fileDst == default)
			{
				if (destination.Pointer is not CpuMemoryPointer cpu)
					return false;
				cpuDst = cpu;
			}
			void* src = gpuSrc.IsValid() ? gpuSrc.NativePointer(source) : cpuSrc.NativePointer(source);
			void* dst = gpuDst.IsValid() ? gpuDst.NativePointer(destination) : cpuDst.NativePointer(destination);

			return PointerStridedCopy<T>(src, (int)incrementSource, dst, (int)incrementDestination, GetCopyKind(cpuSrc, cpuDst), actualCopied);
		}
		#endregion
	}
}