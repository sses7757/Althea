using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

using Althea.Backend.Storage;
using Althea.NativeTypes;
using Althea.Resources;
using Althea.Storage;

using static Althea.Backend.Storage.ConcretePointersExtension;


#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释
namespace Althea.Backend.Cuda.Storage
{
	/// <summary>
	/// The CUDA back-end of the <see cref="AbstractApi"/> that supports data transfer between GPU, CPU and managed memories. May support GPUDirect® Storage that directly transfer data between files and GPU if the corresponding ABIs are found.
	/// </summary>
	public class StorageApi : AbstractApi
	{
		#region basic
		/// <summary>
		/// The <see cref="CudaFileStream"/>s allocated by <see cref="Allocate_(StorageLocation, long, out PointerSegment)"/>
		/// </summary>
		protected internal static readonly LinkedList<CudaFileStream> AllocatedCudaFiles = new();

		/// <summary>
		/// A default <see cref="StorageApi"/> that only supports storage locations of GPU and transfer with CPU memory
		/// </summary>
		protected internal static readonly StorageApi Default = new(false);

		/// <summary>
		/// Create a <see cref="StorageApi"/> with given meta data
		/// </summary>
		/// <param name="supportCuFile">Whether this class supports GPUDirect® Storage or not</param>
		/// <remarks>If the invocation of <see cref="NativeMethods.cuFileDriverOpen"/> failed, the caller must invoke that method when available later</remarks>
		protected internal StorageApi(bool supportCuFile)
		{
			this.CudaFileSupported = supportCuFile;
			if (supportCuFile)
			{
				try
				{
					NativeMethods.cuFileDriverOpen();
				}
				catch (Exception) { }
			}
		}
		
		/// <summary>
		/// The default constructor
		/// </summary>
		public StorageApi()
		{
			try
			{
				this.CudaFileSupported = NativeMethods.cuFileDriverOpen().IsSuccess;
			}
			catch (Exception)
			{
				this.CudaFileSupported = false;
				throw;
			}
		}

		protected override void Dispose(bool disposeManaged)
		{
			if (this.CudaFileSupported)
			{
				NativeMethods.cuFileDriverClose();
			}
		}

		/// <summary>
		/// Get a <see cref="bool"/> indicating whether the GPUDirect® Storage is supported by this instance when initializing it.
		/// </summary>
		public bool CudaFileSupported { get; }


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
					throw new ArgumentException(Parameter.InvalidValue, nameof(value));
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

		private Uri TempFileUri {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				string file = Path.Combine(this.TempFileFolder, Guid.NewGuid().ToString());
				return new(Uri.UriSchemeFile + Uri.SchemeDelimiter + file);
			}
		}
		#endregion

		#region driver info
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override (int major, int minor) DriverVersion(StorageLocation location) => CudaRuntime.GetDriverVersion();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override (long free, long total) FreeAndTotalMemory(StorageLocation location)
		{
			if (location.Type != LocationType.GpuRam || location.LocationDetail != CudaRuntime.CurrentDeviceID)
				return default;
			var err = NativeMethods.cudaMemGetInfo(out var free, out var total);
			return err == CudaError.Success ? (free, total) : default;
		}
		#endregion

		#region support
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool IsSupportedGpuRam(StorageLocation location) => location.Type == LocationType.GpuRam && location.LocationDetail == CudaRuntime.CurrentDeviceID;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool IsSupportedCache(CombinationOfLocations location)
		{
			if (!this.CudaFileSupported)
				return false;
			return location.Type == CombinationType.Cached && location.Count == 2 && IsSupportedGpuRam(location[0]) && location[1] == FileAlone;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override bool IsSupportedLocation(StorageLocation location) => (location.Type == LocationType.GpuRam && location.LocationDetail == CudaRuntime.CurrentDeviceID) || (this.CudaFileSupported && location == FileAlone);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool IsSupportedNonCache(CombinationOfLocations location)
		{
			if (location.Count != 1)
				return false;
			var t = location[0];
			return this.IsSupportedLocation(t);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool IsSupportedLocationNonCopy(CombinationOfLocations location)
		{
			return this.IsSupportedNonCache(location) || this.IsSupportedCache(location);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool IsSupportedLocationCopy(CombinationOfLocations location)
		{
			return (location.Count == 1 && location[0].Type == LocationType.CpuRam) || this.IsSupportedNonCache(location) || this.IsSupportedCache(location);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedBinary(CombinationOfLocations location1, CombinationOfLocations location2)
			=> this.IsSupportedLocationCopy(location1) && this.IsSupportedLocationCopy(location2);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedUnary(CombinationOfLocations location) => this.IsSupportedLocationNonCopy(location);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool CanTransferWithManaged(CombinationOfLocations location) => this.IsSupportedLocationNonCopy(location);
		#endregion

		#region allocate and free
		protected override bool Allocate_(StorageLocation location, long length, out PointerSegment result)
		{
			result = default;
			if (!this.IsSupportedLocation(location))
				return false; // not supported
			if (location.Type == LocationType.GpuRam)
			{
				var err = NativeMethods.cudaMalloc(out var ptr, length);
				if (err == CudaError.ErrorOutOfMemory)
					throw new OutOfMemoryException();
				err.Check();
				result = new(new MemoryPointer(ptr, length, location));
			}
			else
			{
				result = new(new StreamPointer(new CudaFileStream(this.TempFileUri, length), location));
			}
			return true;
		}

		protected override bool Free_(PointerSegment pointer, out bool valid)
		{
			valid = false;
			if (!this.IsSupportedLocation(pointer.Location))
				return false;
			long offset = pointer.GetPointerOffsetCuda(out var mp, out var sp, @throw: false);
			if (offset != 0)
				return false;
			valid = true;
			if (mp is not null)
				return NativeMethods.cudaFree(mp.Pointer) == CudaError.Success;
			if (sp is not null)
				sp.Dispose();
			return true;
		}

		protected override PointerSegment AllocateFileAt(string path, long lengthInBytes)
		{
			return new(new StreamPointer(new CudaFileStream(new(path), lengthInBytes), new(LocationType.GpuRam, CudaRuntime.CurrentDeviceID)));
		}
		#endregion

		#region fill
		protected override bool FillWithValue_(PointerSegment pointer, byte value)
		{
			long offset = pointer.GetPointerOffsetCuda(out var mp, out var sp);
			if (offset == NOT_SUPPORT)
				return false;
			if (mp is not null)
				NativeMethods.cudaMemset(mp.Pointer, value, pointer.LengthInBytes).Check();
			if (sp is not null)
				sp.NativeStream.SetValues(value, pointer.LengthInBytes);
			return true;
		}

		protected override unsafe bool FillWithValue_<T>(PointerSegment pointer, T value)
		{
			if (value.IsZero() || Const<T>.SizeT == sizeof(byte))
				return FillWithValue_(pointer, *(byte*)&value);
			long offset = pointer.GetPointerOffsetCuda(out var mp, out var sp);
			if (offset == NOT_SUPPORT)
				return false;
			if (mp is not null)
				NativeMethods.vecFillVal(Const<T>.DataType, mp.Pointer, &value, pointer.LengthInBytes / Const<T>.SizeT, 1);
			if (sp is not null)
				sp.NativeStream.SetValues(value, pointer.LengthInBytes);
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static new bool FillWithValue<T>(PointerSegment pointer, T value) where T : unmanaged => Default.FillWithValue_(pointer, value);
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
				NativeMethods.cudaMemcpy(dstMP.Pointer, srcMP.Pointer, actualCopied, GetCopyKind(srcMP, dstMP)).Check();
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
				NativeMethods.cudaMemcpy2D(dstMP.Pointer, destinationLD, srcMP.Pointer, sourceLD, height, width, GetCopyKind(srcMP, dstMP)).Check();
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
			return MemoryCopy_(new(MemoryPointer.Create<T>((IntPtr)(&value), 1)), destination, out _);
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
			return MemoryCopy_(source, new(MemoryPointer.Create<T>((IntPtr)Unsafe.AsPointer(ref value), 1)), out _);
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
		protected override bool StridedCopy_<T>(PointerSegment source, int incrementSource, PointerSegment destination, int incrementDestination, out long actualCopied)
		{
			// shortcut
			if (incrementSource == 1 && incrementDestination == 1)
			{
				return this.MemoryCopy_(source, destination, out actualCopied);
			}
			// other cases
			var (srcLen, dstLen) = StridedCopyCheck<T>(source, incrementSource, destination, incrementDestination);
			actualCopied = Math.Min((srcLen - 1) / incrementSource + 1, (dstLen - 1) / incrementDestination + 1);
			long srcOff = source.GetPointerOffsetManaged(out IMemoryPointer? srcMP, out _);
			long dstOff = destination.GetPointerOffsetManaged(out IMemoryPointer? dstMP, out _);
			if (srcOff == NOT_SUPPORT || dstOff == NOT_SUPPORT || srcMP is null || dstMP is null)
			{
				actualCopied = 0; return false;
			}
			MemoryCopyKind copyKind = GetCopyKind(srcMP, dstMP);
			switch (copyKind)
			{
				case MemoryCopyKind.HostToDevice:
					LinearAlgebra.Dense.NativeMethods.cublasSetVector((int)actualCopied, Const<T>.SizeT, srcMP.Pointer, incrementSource, dstMP.Pointer, incrementDestination);
					break;
				case MemoryCopyKind.DeviceToHost:
					LinearAlgebra.Dense.NativeMethods.cublasGetVector((int)actualCopied, Const<T>.SizeT, srcMP.Pointer, incrementSource, dstMP.Pointer, incrementDestination);
					break;
				case MemoryCopyKind.DeviceToDevice:
					NativeMethods.vecStridedCopy(Const<T>.DataType, srcMP.Pointer, dstMP.Pointer, actualCopied, incrementSource, incrementDestination);
					break;
				default:
					actualCopied = 0;
					return false;
			}
			return true;
		}
		#endregion
	}
}
#pragma warning restore CS1591 // 缺少对公共可见类型或成员的 XML 注释