using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

using Althea.Backend.Storage;
using Althea.Resources;
using Althea.Storage;

using static Althea.Backend.Storage.ConcretePointersExtension;


namespace Althea.Backend.Cuda.Storage
{
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释
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


		private string fileFolder = Path.GetTempPath();

		/// <summary>
		/// Get or set the folder to put the temporary files, default is <see cref="Path.GetTempPath"/>
		/// </summary>
		/// <exception cref="ArgumentException">If the folder to set is not an existing folder</exception>
		/// <exception cref="UnauthorizedAccessException">If the program does not have permission to write to the folder</exception>
		/// <exception cref="IOException">Other I/O exceptions</exception>
		public string TempFileFolder {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.fileFolder;
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
				this.fileFolder = value;
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
		/// <summary>
		/// Get the CUDA device's compute capability
		/// </summary>
		/// <param name="deviceID">The CDUA device ID</param>
		/// <returns>The major and minor compute capability of the <paramref name="deviceID"/>; or both 0 if an error occurred</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static (int major, int minor) GetDeviceComputeCapability(int deviceID)
		{
			CudaDeviceProperty prop = default;
			CudaError err = NativeMethods.cudaGetDeviceProperties(ref prop, deviceID);
			if (err == CudaError.Success)
				return (prop.major, prop.minor);
			else
				return default;
		}

		private static int _currentDevice = -1;

		/// <summary>
		/// Statically get or set the current CUDA device, or -1 if it cannot be obtained.
		/// </summary>
		/// <exception cref="StatusException">If an <see cref="CudaError"/> returned during setting the device</exception>
		public static int CurrentDeviceID {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				if (_currentDevice < 0)
				{
					int d = 0;
					var err = NativeMethods.cudaGetDevice(ref d);
					_currentDevice = err == CudaError.Success ? d : -1;
				}
				return _currentDevice;
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set {
				NativeMethods.cudaSetDevice(value).Check();
				_currentDevice = value;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override (int major, int minor) DriverVersion(StorageLocation location)
		{
			if (location.Type != LocationType.GpuRam)
				return default;
			int oldDev = CurrentDeviceID;
			if (oldDev != location.LocationDetail)
				CurrentDeviceID = location.LocationDetail;
			int ver = 0;
			var err = NativeMethods.cudaRuntimeGetVersion(ref ver);
			CurrentDeviceID = oldDev;
			return err == CudaError.Success ? (ver / 1000, (ver % 1000) / 10) : default;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override (long free, long total) FreeAndTotalMemory(StorageLocation location)
		{
			if (location.Type != LocationType.GpuRam)
				return default;
			int oldDev = CurrentDeviceID;
			if (oldDev != location.LocationDetail)
				CurrentDeviceID = location.LocationDetail;
			long free = 0, total = 0;
			var err = NativeMethods.cudaMemGetInfo(ref free, ref total);
			CurrentDeviceID = oldDev;
			return err == CudaError.Success ? (free, total) : default;
		}
		#endregion

		#region support
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override bool IsSupportedLocation(StorageLocation location) => (location.Type == LocationType.GpuRam && location.LocationDetail == CurrentDeviceID) || (this.CudaFileSupported && location == FileAlone);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool IsSupportedLocation(CombinationOfLocations location)
		{
			if (location.Count != 1)
				return false;
			var t = location[0];
			return this.IsSupportedLocation(t);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool IsSupportedLocationCopy(CombinationOfLocations location)
		{
			if (location.Count != 1)
				return false;
			var t = location[0];
			return t.Type == LocationType.CpuRam || this.IsSupportedLocation(t);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedBinary(CombinationOfLocations location1, CombinationOfLocations location2)
			=> this.IsSupportedLocationCopy(location1) && this.IsSupportedLocationCopy(location2);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedUnary(CombinationOfLocations location) => this.IsSupportedLocation(location);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool CanTransferWithManaged(CombinationOfLocations location) => false;
		#endregion

		#region allocate and free
		protected override bool Allocate_(StorageLocation location, long length, out PointerSegment result)
		{
			result = default;
			if (!this.IsSupportedLocation(location))
				return false; // not supported
			if (location.Type == LocationType.GpuRam)
			{
				IntPtr ptr = default;
				var err = NativeMethods.cudaMalloc(ref ptr, length);
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
			return new(new StreamPointer(new CudaFileStream(new(path), lengthInBytes), new(LocationType.GpuRam, CurrentDeviceID));
		}
		#endregion

		#region fill
		protected override bool FillWithValue_(PointerSegment pointer, byte value) => throw new NotImplementedException();

		protected override bool FillWithValue_<T>(PointerSegment pointer, T value) => throw new NotImplementedException();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static new bool FillWithValue<T>(PointerSegment pointer, T value) where T : unmanaged => Default.FillWithValue_(pointer, value);
		#endregion

		#region copy
		protected override bool MemoryCopy_(PointerSegment source, PointerSegment destination, out long actualCopied) => throw new NotImplementedException();

		protected override bool MemoryCopy2D_(PointerSegment source, long sourceLD, PointerSegment destination, long destinationLD, long height, long width) => throw new NotImplementedException();

		protected override bool FromManaged2D_<T>(PointerSegment destination, long leadDim, long height, long width, ReadOnlySpan<T> values, long valuesLeadDim = 0) => throw new NotImplementedException();
		
		protected override bool FromManaged_<T>(PointerSegment destination, T value) => throw new NotImplementedException();
		
		protected override bool FromManaged_<T>(PointerSegment destination, ReadOnlySpan<T> values, out long actualCopied) => throw new NotImplementedException();
		
		protected override bool ToManaged2D_<T>(PointerSegment source, long leadDim, long height, long width, Span<T> destination, long destinationLeadDim = 0) => throw new NotImplementedException();
		
		protected override bool ToManaged_<T>(PointerSegment source, out T value) => throw new NotImplementedException();
		
		protected override bool ToManaged_<T>(PointerSegment source, Span<T> destination, out long actualCopied) => throw new NotImplementedException();
		#endregion

		#region strided copy
		protected override bool StridedCopy_<T>(PointerSegment source, int incrementSource, PointerSegment destination, int incrementDestination, out long actualCopied) => throw new NotImplementedException();
		#endregion
	}
#pragma warning restore CS1591 // 缺少对公共可见类型或成员的 XML 注释
}
