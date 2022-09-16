using System.Runtime.CompilerServices;


namespace Althea.Backend.Cuda;

/// <summary>
/// The static class for static global methods and properties of 
/// </summary>
public static class Runtime
{
	static Runtime()
	{
		try
		{
			var (major, minor) = GetDeviceComputeCapability(0);
			Available = major > 0;
		}
		catch (Exception)
		{
			Available = false;
		}
	}

	/// <summary>
	/// Check whether CUDA is available or not
	/// </summary>
	public static bool Available { get; }

	/// <summary>
	/// Get the CUDA device's compute capability
	/// </summary>
	/// <param name="deviceID">The CDUA device ID</param>
	/// <returns>The major and minor compute capability of the <paramref name="deviceID"/>; or both 0 if an error occurred</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static (int major, int minor) GetDeviceComputeCapability(int deviceID)
	{
		CudaError err = NativeMethods.cudaGetDeviceProperties(out var prop, deviceID);
		if (err == CudaError.Success)
			return (prop.major, prop.minor);
		else
			return default;
	}

	/// <summary>
	/// Statically get the number of CUDA devices. Typically, the allowed device IDs are [0, <see cref="DeviceCount"/> - 1].
	/// </summary>
	public static int DeviceCount {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			NativeMethods.cudaGetDeviceCount(out int c).Check();
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
		get
		{
			if (_currentDevice < 0)
			{
				var err = NativeMethods.cudaGetDevice(out var d);
				_currentDevice = err == CudaError.Success ? d : -1;
			}
			return _currentDevice;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set
		{
			if (_currentDevice == value)
				return;
			NativeMethods.cudaSetDevice(value).Check();
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
		Helpers.Log.Write(string.Format($"CUDA device changed from {previousID} to {currentID}."));
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
		var err = NativeMethods.cudaRuntimeGetVersion(out var ver);
		return err == CudaError.Success ? (ver / 1000, (ver % 1000) / 10) : default;
	}
}