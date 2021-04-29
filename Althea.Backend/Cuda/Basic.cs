using System;
using System.Buffers;
using System.Runtime.CompilerServices;


namespace Althea.Backend.Cuda
{
	/// <summary>
	/// The static class for static global methods and properties of 
	/// </summary>
	public static class CudaRuntime
	{
		/// <summary>
		/// Get the CUDA device's compute capability
		/// </summary>
		/// <param name="deviceID">The CDUA device ID</param>
		/// <returns>The major and minor compute capability of the <paramref name="deviceID"/>; or both 0 if an error occurred</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static (int major, int minor) GetDeviceComputeCapability(int deviceID)
		{
			CudaError err = Storage.NativeMethods.cudaGetDeviceProperties(out var prop, deviceID);
			if (err == CudaError.Success)
				return (prop.major, prop.minor);
			else
				return default;
		}

		/// <summary>
		/// Statically get the number of CUDA devices. Typically, the allowed device IDs are [0, <see cref="DeviceNumber"/> - 1].
		/// </summary>
		public static int DeviceNumber {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				Storage.NativeMethods.cudaGetDeviceCount(out int c).Check();
				return c;
			}
		}

		private static int _currentDevice = -1;

		/// <summary>
		/// Statically get or set the current CUDA device, or -1 if it cannot be obtained.
		/// </summary>
		/// <exception cref="StatusException">If an <see cref="CudaError"/> returned during setting the device</exception>
		/// <remarks>Changing the current CDUA device is a global action and shall be very careful when doing so.</remarks>
		public static int CurrentDeviceID {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				if (_currentDevice < 0)
				{
					var err = Storage.NativeMethods.cudaGetDevice(out var d);
					_currentDevice = err == CudaError.Success ? d : -1;
				}
				return _currentDevice;
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set {
				if (_currentDevice == value)
					return;
				Storage.NativeMethods.cudaSetDevice(value).Check();
				OnDeviceChange.Invoke(_currentDevice, value);
				_currentDevice = value;
			}
		}

		/// <summary>
		/// Encapsulates method(s) which will be invoked when the user changing the current device ID by setting <see cref="CurrentDeviceID"/>.
		/// </summary>
		/// <param name="previousID">The ID of the previous CUDA device</param>
		/// <param name="currentID">The ID of the current CUDA device</param>
		public delegate void DeviceChangeCallback(int previousID, int currentID);

		/// <summary>
		/// The default value of <see cref="OnDeviceChange"/> event that only <see cref="Helpers.Log.Write(string, string?, Helpers.LogLevel)"/> the changing info.
		/// </summary>
		/// <param name="previousID">The ID of the previous CUDA device</param>
		/// <param name="currentID">The ID of the current CUDA device</param>
		public static void DefaultDeviceChangeCallback(int previousID, int currentID)
		{
			Helpers.Log.Write(string.Format(Resource.ChangeDevice, previousID, currentID));
		}

		/// <summary>
		/// The event to be invoked when the user changing the current device ID by setting <see cref="CurrentDeviceID"/>.
		/// </summary>
		public static event DeviceChangeCallback OnDeviceChange = DefaultDeviceChangeCallback;

		/// <summary>
		/// Get the CUDA driver version
		/// </summary>
		/// <returns>The CUDA driver version</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static (int major, int minor) GetDriverVersion()
		{
			var err = Storage.NativeMethods.cudaRuntimeGetVersion(out var ver);
			return err == CudaError.Success ? (ver / 1000, (ver % 1000) / 10) : default;
		}

	}

	/// <summary>
	/// The helper structure to provide safe manipulation of CUDA GPU memory (and CPU memory) buffers
	/// </summary>
	/// <remarks>Currently, the host buffers are pooled by the <see cref="ArrayPool{T}"/> while the device buffers are not pooled.</remarks>
	public readonly ref struct CudaBuffer
	{
		private readonly byte[]? hostBuffer;

		private readonly IntPtr deviceBuffer;

		private readonly long extraDeviceInfoOffset;

		/// <summary>
		/// Get the buffer array on host (CPU memory) as an array of <see cref="byte"/>. Returns an empty array if this <see cref="CudaBuffer"/> was created without host buffer.
		/// </summary>
		public byte[] HostBuffer {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.hostBuffer ?? Array.Empty<byte>();
		}

		/// <summary>
		/// Get the pointer to the buffer array on current CUDA device (GPU memory) as an <see cref="IntPtr"/> or <see cref="IntPtr.Zero"/> if no array was allocated on device.
		/// </summary>
		public IntPtr DeviceBuffer {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.deviceBuffer;
		}

		/// <summary>
		/// Get the pointer to the preserved extra device info on current CUDA device (GPU memory) as an <see cref="IntPtr"/> or <see cref="IntPtr.Zero"/> if there is no preserved extra device info.
		/// </summary>
		public IntPtr ExtraDeviceInfo {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.extraDeviceInfoOffset == 0 ? default : (IntPtr)((long)this.deviceBuffer + extraDeviceInfoOffset);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private CudaBuffer(long workSpaceDeviceBytes, long workSpaceHostBytes = 0, long extraDeviceInfoBytes = 0)
		{
			if (workSpaceDeviceBytes < 0)
				throw new ArgumentOutOfRangeException(nameof(workSpaceDeviceBytes), workSpaceDeviceBytes, Resources.Parameter.CannotNegative);
			if (workSpaceHostBytes < 0)
				throw new ArgumentOutOfRangeException(nameof(workSpaceHostBytes), workSpaceHostBytes, Resources.Parameter.CannotNegative);
			if (extraDeviceInfoBytes < 0)
				throw new ArgumentOutOfRangeException(nameof(extraDeviceInfoBytes), extraDeviceInfoBytes, Resources.Parameter.CannotNegative);
			if (workSpaceHostBytes > int.MaxValue)
				throw new ArgumentOutOfRangeException(nameof(workSpaceHostBytes), workSpaceHostBytes, Resources.Parameter.InvalidValue);
			this.hostBuffer = workSpaceHostBytes == 0 ? null : ArrayPool<byte>.Shared.Rent((int)workSpaceHostBytes);
			if (workSpaceDeviceBytes + extraDeviceInfoBytes > 0)
			{
				var err = Storage.NativeMethods.cudaMalloc(out this.deviceBuffer, workSpaceDeviceBytes + extraDeviceInfoBytes);
				this.extraDeviceInfoOffset = extraDeviceInfoBytes == 0 ? 0 : workSpaceDeviceBytes;
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
		public static unsafe CudaBuffer Create<T>(int workSpaceDeviceT, int workSpaceHostT = 0, bool extraDeviceInfo = true) where T : unmanaged
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
				Storage.NativeMethods.cudaFree(this.deviceBuffer);
		}
	}


	/// <summary>
	/// The class used to set CUDA back-end implementations, inherits <see cref="ISetBackend"/>
	/// </summary>
	public sealed class CudaImplementations : ISetBackend
	{
		/// <summary>
		/// The default constructor
		/// </summary>
		public CudaImplementations()
		{
			try
			{
				this.Available = Storage.NativeMethods.cudaDeviceSynchronize() == CudaError.Success;
			}
			catch (Exception)
			{
				this.Available = false;
				throw;
			}
		}

		/// <summary>
		/// Get a <see cref="bool"/> indicating whether CUA is available when initializing this instance
		/// </summary>
		public bool Available { get; }

		Type ISetBackend.StorageImplementation => typeof(Storage.StorageApi);

		Type ISetBackend.DenseLinearAlgebraImplementation => typeof(LinearAlgebra.Dense.DenseApi);

		Type ISetBackend.SparseLinearAlgebraImplementation => typeof(int/*LinearAlgebra.Sparse.SparseApi*/);

		Type ISetBackend.DenseTensorAlgebraImplementation => typeof(TensorAlgebra.Dense.DenseApi);

		Type ISetBackend.SparseTensorAlgebraImplementation => typeof(int);

		Type ISetBackend.RandomImplementation => typeof(int/*TODO: Random.RandomApi*/);

		Type ISetBackend.SolverImplementation => typeof(int);
	}
}
