using System;
using System.Text;
using Althea.Storage;


namespace Althea.Runtime
{
	/// <summary>
	/// The runtime routine interface
	/// </summary>
	public interface IRuntime : IDisposable
	{
		#region properties
		/// <summary>
		/// Get the current CPU / GPU driver's version
		/// </summary>
		(int major, int minor) DriverVersion { get; }

		/// <summary>
		/// Set or get the current CPU / GPU device to use / using. Should range from 0 to <see cref="MaxDeviceNo"/>
		/// </summary>
		int CurrentDevice { get; set; }

		/// <summary>
		/// Get the supported maximum number of CPU / GPU devices available.
		/// </summary>
		int MaxDeviceNo { get; }

		/// <summary>
		/// Get the available and total memory in bytes for current GPU / GPU device
		/// </summary>
		(long free, long total) FreeAndTotalMemory { get; }

		/// <summary>
		/// Set or get the number of threads used by current CPU / GPU driver
		/// </summary>
		int NumberOfThreads { get; set; }
		#endregion

		#region methods
		/// <summary>
		/// Fill the <paramref name="storage"/> by same <paramref name="value"/>, byte by byte.
		/// </summary>
		/// <typeparam name="T">any struct</typeparam>
		/// <param name="storage">pointer to be filled</param>
		/// <param name="value">value to set</param>
		/// <param name="length">length in <typeparamref name="T"/></param>
		void SetMemoryValue<T>(Storage<T> storage, long length, byte value) where T : struct;

		/// <summary>
		/// Fill the <paramref name="storage"/> by same <paramref name="value"/>, byte by byte.
		/// </summary>
		/// <typeparam name="T">any struct</typeparam>
		/// <param name="storage">pointer to be filled</param>
		/// <param name="value">value to set</param>
		/// <param name="length">length in <typeparamref name="T"/></param>
		public delegate void DelegateSetMemoryValue<T>(Storage<T> storage, long length, byte value) where T : struct;

		/// <summary>
		/// Copy memory from <paramref name="source"/> to <paramref name="dest"/>. If any of them is on GPU, the GPU routine will be invoked.
		/// </summary>
		/// <typeparam name="T">any struct</typeparam>
		/// <param name="source">source pointer to copy from</param>
		/// <param name="dest">destination pointer to copy into</param>
		/// <param name="length">length to copy</param>
		void MemoryCopy<T>(Storage<T> source, Storage<T> dest, long length) where T : struct;

		/// <summary>
		/// Copy memory from <paramref name="source"/> to <paramref name="dest"/>. If any of them is on GPU, the GPU routine will be invoked.
		/// </summary>
		/// <typeparam name="T">any struct</typeparam>
		/// <param name="source">source pointer to copy from</param>
		/// <param name="dest">destination pointer to copy into</param>
		/// <param name="length">length to copy</param>
		public delegate void DelegateMemoryCopy<T>(Storage<T> source, Storage<T> dest, long length) where T : struct;

		/// <summary>
		/// Copies 2D data from <paramref name="source"/> to <paramref name="dest"/>. If any of them is on GPU, the GPU routine will be invoked.
		/// </summary>
		/// <param name="source">the source pointer</param>
		/// <param name="sourceLD">source array actual height (actual leading dimension) in <typeparamref name="T"/></param>
		/// <param name="dest">the destination pointer</param>
		/// <param name="destLD">destination array actual height (actual leading dimension) in <typeparamref name="T"/></param>
		/// <param name="height">height to copy, in <typeparamref name="T"/></param>
		/// <param name="width">width to copy, in <typeparamref name="T"/> rather than bytes</param>
		void MemoryCopy2D<T>(Storage<T> source, long sourceLD, Storage<T> dest, long destLD, long height, long width) where T : struct;

		/// <summary>
		/// Copies 2D data from <paramref name="source"/> to <paramref name="dest"/>. If any of them is on GPU, the GPU routine will be invoked.
		/// </summary>
		/// <param name="source">the source pointer</param>
		/// <param name="sourceLD">source array actual height (actual leading dimension) in <typeparamref name="T"/></param>
		/// <param name="dest">the destination pointer</param>
		/// <param name="destLD">destination array actual height (actual leading dimension) in <typeparamref name="T"/></param>
		/// <param name="height">height to copy, in <typeparamref name="T"/></param>
		/// <param name="width">width to copy, in <typeparamref name="T"/> rather than bytes</param>
		public delegate void DelegateMemoryCopy2D<T>(Storage<T> source, long sourceLD, Storage<T> dest, long destLD, long height, long width) where T : struct;
		#endregion
	}
}


namespace Althea.Runtime.Cuda
{
	internal sealed class CudaRuntime : IRuntime
	{
		public CudaRuntime()
		{
			NativeMethods.cudaDeviceReset().Check();
			NativeMethods.cudaDeviceSynchronize().Check();
		}

		public void Dispose()
		{
			NativeMethods.cudaDeviceSynchronize().Check();
			NativeMethods.cudaDeviceReset().Check();
		}

		public (int major, int minor) DriverVersion {
			get {
				int v = 0;
				NativeMethods.cudaRuntimeGetVersion(ref v);
				return (v / 1000, (v % 1000) / 10);
			}
		}

		public int CurrentDevice {
			get {
				int c = 0;
				NativeMethods.cudaGetDevice(ref c).Check();
				return c;
			}
			set {
				if (value >= 0 && value < this.MaxDeviceNo)
					NativeMethods.cudaSetDevice(value);
				else
					throw new ArgumentOutOfRangeException(nameof(value));
			}
		}

		public int MaxDeviceNo {
			get {
				int c = 0;
				NativeMethods.cudaGetDeviceCount(ref c).Check();
				return c;
			}
		}

		public (long free, long total) FreeAndTotalMemory {
			get {
				long free = 0, total = 0;
				Cuda.NativeMethods.cudaMemGetInfo(ref free, ref total).Check();
				return (free, total);
			}
		}

		public int NumberOfThreads { get => 1; set => throw new InvalidOperationException(); }


		public void MemoryCopy<T>(Storage<T> source, Storage<T> dest, long length) where T : struct
		{
			NativeMethods.cudaMemcpy(dst: dest, src: source, length * Storage<T>.SizeOfT, source.CopyToKind(dest)).Check();
		}

		public void SetMemoryValue<T>(Storage<T> storage, long length, byte value) where T : struct
		{
			NativeMethods.cudaMemset(storage, value, length * Storage<T>.SizeOfT).Check();
		}

		public void MemoryCopy2D<T>(Storage<T> source, long sourceLD, Storage<T> dest, long destLD, long height, long width) where T : struct
		{
			var size = Storage<T>.SizeOfT;
			NativeMethods.cudaMemcpy2D( dst: dest, destLD: destLD * size,
										src: source, srcLD: sourceLD * size,
										height: height * size, width: width,
										kind: source.CopyToKind(dest)).Check();
		}
	}
}


namespace Althea.Runtime.Mkl
{
	internal sealed class MklRuntime : IRuntime
	{
		public MklRuntime()
		{
			NativeMethods.MKL_Set_Num_Threads(Environment.ProcessorCount);
		}

		public void Dispose()
		{
			NativeMethods.MKL_Free_Buffers();
			////NativeMethods.MKL_Finalize();
		}

		public (int major, int minor) DriverVersion {
			get {
				int len = 198;
				StringBuilder str = new StringBuilder(len);
				NativeMethods.MKL_Get_Version_String(str, len);
				var s = str.ToString();
				int versionStart = s.IndexOf("Version") + "Version".Length + 1;
				s = s[versionStart..s.IndexOf("Product")];
				var ss = s.Split('.');
				return (Convert.ToInt32(ss[0]), Convert.ToInt32(ss[1]));
			}
		}

		public int CurrentDevice { get => 0; set => throw new InvalidOperationException(); }

		public int MaxDeviceNo => 1;

		public (long free, long total) FreeAndTotalMemory {
			get {
				long free = 0, total = 0;
				NativeMethods.getTotalSystemMemory(ref total, ref free);
				return (checked(free), checked(total));
			}
		}

		public int NumberOfThreads {
			get => NativeMethods.MKL_Get_Max_Threads();
			set {
				if (value > 0)
					NativeMethods.MKL_Set_Num_Threads(value);
				else
					throw new ArgumentOutOfRangeException(nameof(value), value, Resource.ParaMustPositive);
			}
		}

		public void MemoryCopy<T>(Storage<T> source, Storage<T> dest, long length) where T : struct
		{
			NativeMethods.hostmemcopy(src: source, dst: dest, length * Storage<T>.SizeOfT);
		}

		public void MemoryCopy2D<T>(Storage<T> source, long sourceLD, Storage<T> dest, long destLD, long height, long width) where T : struct
		{
			var size = Storage<T>.SizeOfT;
			NativeMethods.hostmemcopy2D(src: source, srcPitch: sourceLD * size,
										dst: dest, dstPitch: destLD * size,
										height: height * size, width: width);
		}

		public void SetMemoryValue<T>(Storage<T> storage, long length, byte value) where T : struct
		{
			NativeMethods.hostmemset(storage, value, length * Storage<T>.SizeOfT);
		}
	}
}
