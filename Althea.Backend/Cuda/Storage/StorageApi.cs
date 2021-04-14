using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Althea.Backend.Storage;
using Althea.Storage;


namespace Althea.Backend.Cuda.Storage
{
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释
	/// <summary>
	/// The CUDA back-end of the <see cref="AbstractApi"/> that supports data transfer between GPU, CPU and managed memories. May support GPUDirect® Storage that directly transfer data between files and GPU if the corresponding ABI is found.
	/// </summary>
	public class StorageApi : AbstractApi
	{
		#region basic
		/// <summary>
		/// Create a <see cref="StorageApi"/> with given meta data
		/// </summary>
		/// <param name="supportCuFile">Whether this class supports GPUDirect® Storage or not</param>
		/// <param name="deviceComputeCapabilities">The devices' compute capabilities</param>
		protected internal StorageApi(bool supportCuFile, IReadOnlyDictionary<int, (int, int)> deviceComputeCapabilities)
		{
			this.CudaFileSupported = supportCuFile;
			this.DeviceComputeCapability = deviceComputeCapabilities;
			if (supportCuFile)
			{
				NativeMethods.cuFileDriverOpen();
			}
		}
		
		/// <summary>
		/// The default constructor
		/// </summary>
		public StorageApi()
		{
			int c = 0;
			NativeMethods.cudaGetDeviceCount(ref c);
			var dict = new Dictionary<int, (int, int)>(c);
			for (int i = 0; i < c; i++)
			{
				dict.Add(i, GetDeviceComputeCapability(i));
			}
			this.DeviceComputeCapability = dict;

			try
			{
				var status = NativeMethods.cuFileDriverOpen();
				if (status.IsSuccess)
					this.CudaFileSupported = true;
				else
					this.CudaFileSupported = false;
			}
			catch (Exception)
			{	// the library is not found or other errors
				this.CudaFileSupported = false;
			}
		}

		protected override void Dispose(bool disposeManaged)
		{
			if (this.CudaFileSupported)
			{
				foreach (var item in this.allocatedCudaFiles)
				{
					item?.Dispose();
				}
				NativeMethods.cuFileDriverClose();
			}
		}

		private readonly List<CudaFileStream?> allocatedCudaFiles = new();

		/// <summary>
		/// Get a <see cref="bool"/> indicating whether the GPUDirect® Storage is supported by this instance when initializing it.
		/// </summary>
		public bool CudaFileSupported { get; }

		/// <summary>
		/// Get the devices' compute capabilities of this instance when initializing it.
		/// </summary>
		public IReadOnlyDictionary<int, (int major, int minor)> DeviceComputeCapability { get; }
		#endregion

		#region driver info
		/// <summary>
		/// Get the CUDA device's compute capability
		/// </summary>
		/// <param name="deviceID">The CDUA device ID</param>
		/// <returns>The major and minor compute capability of the <paramref name="deviceID"/>; or both 0 if an error occurred</returns>
		public static (int major, int minor) GetDeviceComputeCapability(int deviceID)
		{
			CudaDeviceProperty prop = default;
			CudaError err = NativeMethods.cudaGetDeviceProperties(ref prop, deviceID);
			if (err == CudaError.Success)
				return (prop.major, prop.minor);
			else
				return default;
		}

		/// <summary>
		/// Statically get or set the current CUDA device
		/// </summary>
		public static int CurrentDeviceID {
			get {
				int d = 0;
				var err = NativeMethods.cudaGetDevice(ref d);
				return err == CudaError.Success ? d : -1;
			}
			set {
				int d = 0;
				var err = NativeMethods.cudaGetDevice(ref d);
				if (d != value)
					err = NativeMethods.cudaSetDevice(value);
					// TODO: conditional throw
			}
		}

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
		public override bool IsSupportedLocation(StorageLocation location) => throw new NotImplementedException();

		protected override bool IsSupportedBinary(CombinationOfLocations location1, CombinationOfLocations location2) => throw new NotImplementedException();

		protected override bool IsSupportedUnary(CombinationOfLocations location) => throw new NotImplementedException();

		protected override bool CanTransferWithManaged(CombinationOfLocations location) => throw new NotImplementedException();
		#endregion

		#region allocate free
		protected override bool Allocate_(StorageLocation location, long length, out PointerSegment result) => throw new NotImplementedException();

		protected override bool Free_(PointerSegment pointer, out bool valid) => throw new NotImplementedException();

		protected override PointerSegment AllocateFileAt(string path, long lengthInBytes) => throw new NotImplementedException();
		#endregion

		#region fill
		protected override bool FillWithValue_(PointerSegment pointer, byte value) => throw new NotImplementedException();

		protected override bool FillWithValue_<T>(PointerSegment pointer, T value) => throw new NotImplementedException();
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
