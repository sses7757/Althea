using System.IO;
using System.Runtime.CompilerServices;

using Althea.Backend.Cuda.LinearAlgebra.Dense;
using Althea.Backend.Storage;


namespace Althea.Backend.Cuda.Storage
{
	/// <summary>
	/// The CUDA back-end of the <see cref="IAbstractApi"/> that supports data transfer between GPU, CPU and managed memories. May support GPUDirect® Storage that directly transfer data between files and GPU if the corresponding ABIs are found.
	/// </summary>
	public unsafe class StorageApi : IAbstractApi
	{
		#region basic
		internal static readonly StorageApi Default = new(false);

		/// <summary>
		/// Create a <see cref="StorageApi"/> with given meta data
		/// </summary>
		/// <param name="supportCuFile">Whether this class shall enables CUDA GPUDirect Storage or not</param>
		/// <remarks>If the invocation of <see cref="NativeMethods.cuFileDriverOpen"/> failed, the caller must invoke that method when available later</remarks>
		public StorageApi(bool supportCuFile)
		{
			if (!supportCuFile)
				return;
			try
			{
				this.CudaFileSupported = NativeMethods.cuFileDriverOpen().IsSuccess;
			}
			catch (Exception e)
			{
				this.CudaFileSupported = false;
				Helpers.Log.Write($"Error occurred when opening CUDA file driver: {e}", level: Helpers.LogLevel.Error);
			}
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			if (this.CudaFileSupported)
			{
				var err = NativeMethods.cuFileDriverClose();
				if (!err.IsSuccess)
					Helpers.Log.Write($"Error occurred when closing CUDA file driver: {err.FileOpResult} ({err.DriverResult})", level: Helpers.LogLevel.Error);
			}
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Get a <see cref="bool"/> indicating whether the GPUDirect® Storage is supported by this instance when initializing it.
		/// </summary>
		public bool CudaFileSupported { get; } = false;

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
			if (TP.Location.Type == LocationType.GpuRam)
			{
				if (Runtime.CurrentDeviceID != TP.Location.Detail)
					return false;
				var ptr = pointer.FromGenericGpu();
				var err = NativeMethods.cudaFree(ptr.Pointer);
				if (err != CudaError.Success)
					return true;
			}
			else if (pointer is CudaFilePointer ptr)
			{
				if (!this.CudaFileSupported)
					return false;
				ptr.Dispose();
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
			long offset = pointer.GetPointerOffsetCuda(out var mp, out var sp);
			if (offset == NOT_SUPPORT)
				return false;
			if (mp is not null)
				NativeMethods.cudaMemset(mp.OffsetPointer(offset), value, pointer.LengthInBytes).Check();
			if (sp is not null)
				sp.NativeStream.SetValues(value, pointer.LengthInBytes);
			return true;
		}

		protected override unsafe bool FillWithValue_<T>(PointerSegment pointer, T value)
		{
			if (value.IsZero() || T.Size == sizeof(byte))
				return FillWithValue_(pointer, *(byte*)&value);
			long offset = pointer.GetPointerOffsetCuda(out var mp, out var sp);
			if (offset == NOT_SUPPORT)
				return false;
			if (mp is not null)
				NativeMethods.vecFillVal(Const<T>.DataType, mp.OffsetPointer(offset), &value, pointer.LengthInBytes / T.Size, 1);
			if (sp is not null)
				sp.NativeStream.SetValues(value, pointer.LengthInBytes);
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static new bool FillWithValue<T>(PointerSegment pointer, T value) where T : unmanaged, IBaseNumber<T> => Default.FillWithValue_(pointer, value);
		#endregion

		#region copy
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static MemoryCopyKind GetCopyKind(IMemoryPointer srcMP, IMemoryPointer dstMP)
		{
			bool srcHost = srcMP.Location.Type == LocationType.CpuRam,
					 dstHost = dstMP.Location.Type == LocationType.CpuRam;
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

		protected override bool MemoryCopy_(PointerSegment source, PointerSegment destination, out long actualCopied)
		{
			actualCopied = 0;
			long srcOff = source.GetPointerOffsetCuda(out var srcMP, out var srcSP);
			long dstOff = source.GetPointerOffsetCuda(out var dstMP, out var dstSP);
			if (srcOff == NOT_SUPPORT || dstOff == NOT_SUPPORT)
				return false;
			actualCopied = Math.Min(source.LengthInBytes, destination.LengthInBytes);
			if (srcMP is not null && dstMP is not null)
			{
				NativeMethods.cudaMemcpy(dstMP.OffsetPointer(dstOff), srcMP.OffsetPointer(srcOff), actualCopied, GetCopyKind(srcMP, dstMP)).Check();
			}
			else
			{
				StreamAndMemoryCopy(srcOff, dstOff, actualCopied, source, destination, srcMP, srcSP, dstMP, dstSP);
			}
			return true;
		}

		protected override bool MemoryCopy2D_(PointerSegment source, long sourceLD, PointerSegment destination, long destinationLD, long height, long width)
		{
			Copy2DCheck(source, sourceLD, destination, destinationLD, height, width);
			// shortcut
			if (sourceLD == destinationLD && sourceLD == height)
			{
				return this.MemoryCopy_(source.AsLength(height * width), destination.AsLength(height * width), out _);
			}
			// normal cases
			long srcOff = source.GetPointerOffsetManaged(out IMemoryPointer? srcMP, out IStreamPointer? srcSP);
			long dstOff = destination.GetPointerOffsetManaged(out IMemoryPointer? dstMP, out IStreamPointer? dstSP);
			if (srcOff == NOT_SUPPORT || dstOff == NOT_SUPPORT)
				return false;
			if (srcMP is not null && dstMP is not null)
			{
				NativeMethods.cudaMemcpy2D(dstMP.OffsetPointer(dstOff), destinationLD, srcMP.OffsetPointer(srcOff), sourceLD, height, width, GetCopyKind(srcMP, dstMP)).Check();
			}
			else
			{
				StreamAndMemoryCopy2D(sourceLD, destinationLD, height, width, srcOff, dstOff, source, destination, srcMP, srcSP, dstMP, dstSP);
			}
			return true;
		}

		protected override unsafe bool FromManaged2D_<T>(PointerSegment destination, long leadDim, long height, long width, ReadOnlySpan<T> values, long valuesLeadDim = 0)
		{
			fixed (T* ptr = values)
			{
				return MemoryCopy2D_(values.AsPointerSegment(ptr), valuesLeadDim == 0 ? height : valuesLeadDim, destination, leadDim, height, width);
			}
		}
		
		protected override unsafe bool FromManaged_<T>(PointerSegment destination, T value)
		{
			return MemoryCopy_(new(CpuMemoryPointer.Create<T>((IntPtr)(&value), 1)), destination, out _);
		}
		
		protected override unsafe bool FromManaged_<T>(PointerSegment destination, ReadOnlySpan<T> values, out long actualCopied)
		{
			fixed (T* ptr = values)
			{
				return MemoryCopy_(values.AsPointerSegment(ptr), destination, out actualCopied);
			}
		}
		
		protected override unsafe bool ToManaged2D_<T>(PointerSegment source, long leadDim, long height, long width, Span<T> destination, long destinationLeadDim = 0)
		{
			fixed (T* ptr = destination)
			{
				return MemoryCopy2D_(source, leadDim, destination.AsPointerSegment(ptr), destinationLeadDim == 0 ? height : destinationLeadDim, height, width);
			}
		}
		
		protected override unsafe bool ToManaged_<T>(PointerSegment source, out T value)
		{
			value = default;
			return MemoryCopy_(source, new(CpuMemoryPointer.Create<T>((IntPtr)Unsafe.AsPointer(ref value), 1)), out _);
		}
		
		protected override unsafe bool ToManaged_<T>(PointerSegment source, Span<T> destination, out long actualCopied)
		{
			fixed (T* ptr = destination)
			{
				return MemoryCopy_(source, destination.AsPointerSegment(ptr), out actualCopied);
			}
		}
		#endregion

		#region strided copy
		internal static unsafe bool PointerStridedCopy<T>(IntPtr source, int incrementSource, IntPtr destination, int incrementDestination, MemoryCopyKind copyKind, int copies) where T : unmanaged, IBaseNumber<T>
		{
			if (incrementSource == 1 && incrementDestination == 1)
			{
				return NativeMethods.cudaMemcpy(destination, source, sizeof(T) * (long)copies, copyKind) == CudaError.Success;
			}
			switch (copyKind)
			{
				case MemoryCopyKind.HostToDevice:
					LinearAlgebra.Dense.NativeMethods.cublasSetVector(copies, sizeof(T), source, incrementSource, destination, incrementDestination);
					break;
				case MemoryCopyKind.DeviceToHost:
					LinearAlgebra.Dense.NativeMethods.cublasGetVector(copies, sizeof(T), source, incrementSource, destination, incrementDestination);
					break;
				case MemoryCopyKind.DeviceToDevice:
					var (major, minor) = CudaRuntime.GetDriverVersion();
					bool cuda111Above = (major == 11 && minor >= 1) || major > 11;
					var handles = DenseApi.deviceHandles[CudaRuntime.CurrentDeviceID];
					if (handles is not null && handles.First is { } h)
					{
						delegate*<IntPtr, int, IntPtr, int, IntPtr, int, CudaBlasStatus> func = null;
						switch (Const<T>.DataType)
						{
							case DataType.RealFloat32:
								func = cuda111Above ? &LinearAlgebra.Dense.NativeMethods.cublasScopy : &LinearAlgebra.Dense.NativeMethods.cublasScopy_v2;
								break;
							case DataType.RealFloat64:
								func = cuda111Above ? &LinearAlgebra.Dense.NativeMethods.cublasDcopy : &LinearAlgebra.Dense.NativeMethods.cublasDcopy_v2;
								break;
							case DataType.ComplexSingle:
								func = cuda111Above ? &LinearAlgebra.Dense.NativeMethods.cublasCcopy : &LinearAlgebra.Dense.NativeMethods.cublasCcopy_v2;
								break;
							case DataType.ComplexDouble:
								func = cuda111Above ? &LinearAlgebra.Dense.NativeMethods.cublasZcopy : &LinearAlgebra.Dense.NativeMethods.cublasZcopy_v2;
								break;
							default:
								break;
						}
						if (func is not null)
						{
							func(h.Value, copies, source, incrementSource, destination, incrementDestination).Check();
							return true;
						}
					}
					NativeMethods.vecStridedCopy(Const<T>.DataType, source, destination, copies, incrementSource, incrementDestination);
					break;
				default:
					return false;
			}
			return true;
		}

		protected override unsafe bool StridedCopy_<T>(PointerSegment source, int incrementSource, PointerSegment destination, int incrementDestination, out long actualCopied)
		{
			// shortcut
			if (incrementSource == 1 && incrementDestination == 1)
			{
				return this.MemoryCopy_(source, destination, out actualCopied);
			}
			// other cases
			var (srcLen, dstLen) = StridedCopyCheck<T>(source, incrementSource, destination, incrementDestination);
			actualCopied = Math.Min((srcLen - 1) / incrementSource + 1, (dstLen - 1) / incrementDestination + 1);
			if (actualCopied > int.MaxValue)
			{
				actualCopied = 0; return false;
			}
			int copies = (int)actualCopied;
			long srcOff = source.GetPointerOffsetManaged(out IMemoryPointer? srcMP, out _);
			long dstOff = destination.GetPointerOffsetManaged(out IMemoryPointer? dstMP, out _);
			if (srcOff == NOT_SUPPORT || dstOff == NOT_SUPPORT || srcMP is null || dstMP is null)
			{
				actualCopied = 0; return false;
			}
			MemoryCopyKind copyKind = GetCopyKind(srcMP, dstMP);
			if (!PointerStridedCopy<T>(srcMP.OffsetPointer(srcOff), incrementSource, dstMP.OffsetPointer(dstOff), incrementDestination, copyKind, copies))
			{
				actualCopied = 0; return false;
			}
			else
			{
				return true;
			}
		}
		#endregion
	}
}
#pragma warning restore CS1591 // 缺少对公共可见类型或成员的 XML 注释