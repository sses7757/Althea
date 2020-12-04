using System;
using System.Runtime.InteropServices;

using Althea.Arrays;
using Althea.Memory;


namespace Althea.Runtime
{
	/// <summary>
	/// The static class wrapper of runtime APIs
	/// </summary>
	public static class API
	{
		#region base
		/// <summary>
		/// Static class initializer
		/// </summary>
		static API()
		{
			if (GlobalSettings.RuntimeGPU != null)
				GPUconstructor = GlobalSettings.RuntimeGPU.GetConstructor(Array.Empty<Type>());
			else
				GPUconstructor = typeof(Cuda.CudaRuntime).GetConstructor(Array.Empty<Type>());
			if (GlobalSettings.RuntimeCPU != null)
				CPUconstructor = GlobalSettings.RuntimeCPU.GetConstructor(Array.Empty<Type>());
			else
				CPUconstructor = typeof(Mkl.MklRuntime).GetConstructor(Array.Empty<Type>());
			Initialize();
		}

		/// <summary>
		/// Reset the Tensor libraries
		/// </summary>
		public static void Reset()
		{
			try
			{
				GPU.Dispose();
				CPU.Dispose();
			}
			catch (StatusException e)
			{
				Log.Write($"Error at reseting Runtime library \"{e.Message}\":" + Environment.NewLine + e.StackTrace, level: LogLevel.Error);
			}
			finally
			{
				Initialize();
			}
		}

		/// <summary>
		/// Singleton runtime API of GPU routine
		/// </summary>
		/// <remarks>once you use this property, the underlying adapter will be initialized</remarks>
		public static IRuntime GPU => _GPUInit.Value;

		/// <summary>
		/// Singleton runtime API of CPU routine
		/// </summary>
		/// <remarks>once you use this property, the underlying adapter will be initialized</remarks>
		public static IRuntime CPU => _CPUInit.Value;

		private static readonly System.Reflection.ConstructorInfo GPUconstructor, CPUconstructor;

		private static Lazy<IRuntime> _GPUInit, _CPUInit;

		private static void Initialize()
		{
			_GPUInit = new Lazy<IRuntime>(() => GPUconstructor.Invoke(Array.Empty<object>()) as IRuntime, true);
			_CPUInit = new Lazy<IRuntime>(() => CPUconstructor.Invoke(Array.Empty<object>()) as IRuntime, true);
		}
		#endregion


		#region device info
		/// <summary>
		/// Get the CUDA major version
		/// </summary>
		internal static int CUDAVersionMajor {
			get {
				int v = 0;
				Cuda.NativeMethods.cudaRuntimeGetVersion(ref v);
				return v / 1000;
			}
		}

		/// <summary>
		/// Get the CUDA minor version
		/// </summary>
		internal static int CUDAVersionMinor {
			get {
				int v = 0;
				Cuda.NativeMethods.cudaRuntimeGetVersion(ref v);
				return (v % 1000) / 10;
			}
		}

		private static (int major, int minor)? _computeCapabilityCache = null;

		/// <summary>
		/// Get current CUDA device's compute capability
		/// </summary>
		internal static (int major, int minor) CUDAComputeCapability {
			get {
				if (_computeCapabilityCache.HasValue)
					return _computeCapabilityCache.Value;
				int major = 0, minor = 0;
				Cuda.NativeMethods.getDeviceComputeCapability(DeviceNo, ref major, ref minor).Check();
				_computeCapabilityCache = (major, minor);
				return _computeCapabilityCache.Value;
			}
		}

		/// <summary>
		/// Get the GPU device driver's major and minor version
		/// </summary>
		public static (int major, int minor) DeviceVersion => GPU.DriverVersion;

		/// <summary>
		/// Set or get the GPU device to use
		/// </summary>
		public static int DeviceNo { get => GPU.CurrentDevice; set => GPU.CurrentDevice = value; }

		/// <summary>
		/// Get the count of GPU devices
		/// </summary>
		public static int DeviceCount => GPU.MaxDeviceNo;

		/// <summary>
		/// Get the available memory on current GPU device in bytes
		/// </summary>
		public static long DeviceFreeMemory => GPU.FreeAndTotalMemory.free;

		/// <summary>
		/// Get the total memory on current GPU device in bytes
		/// </summary>
		public static long DeviceTotalMemory => GPU.FreeAndTotalMemory.total;

		/// <summary>
		/// Get the available and total memory on current GPU device in bytes
		/// </summary>
		public static (long free, long total) DeviceFreeAndTotalMemory => GPU.FreeAndTotalMemory;

		/// <summary>
		/// Set or get the number of threads used by GPU device. This may usually fail.
		/// </summary>
		public static int DeviceNumerOfThreads { get => GPU.NumberOfThreads; set => GPU.NumberOfThreads = value; }
		#endregion


		#region host info
		/// <summary>
		/// Set the MKL verbose mode and output file.
		/// <list type="table">
		/// <listheader><term>Set value</term><description>  Action</description></listheader>
		/// <item><term>null</term><description>  disable the MKL verbose mode</description></item>
		/// <item><term>empty ("")</term><description>  enable the MKL verbose mode and set the output to <see cref="Console.Out"/> (std-out)</description></item>
		/// <item><term>an existed file name</term><description>  enable the MKL verbose mode and set the output file to the value</description></item>
		/// </list>
		/// </summary>
		/// <remarks><b>DO NOT</b> set this property if your MKL library is not loaded</remarks>
		public static string MKLVerbose {
			set {
				if (value is null)
					Mkl.NativeMethods.MKL_Verbose(0);
				Mkl.NativeMethods.MKL_Verbose(1);
				int success = Mkl.NativeMethods.MKL_Verbose_Output_File(value);
				if (!string.IsNullOrEmpty(value) && success != 0)
					Log.Write($"Cannot locate file '{value}', use std-out instead.", level: LogLevel.Warning);
			}
		}

		/// <summary>
		/// Get the CPU device driver's major and minor version
		/// </summary>
		public static (int major, int minor) HostVersion => CPU.DriverVersion;

		/// <summary>
		/// Set or get the CPU device to use
		/// </summary>
		public static int HostNo { get => CPU.CurrentDevice; set => CPU.CurrentDevice = value; }

		/// <summary>
		/// Get the count of CPU devices
		/// </summary>
		public static int HostCount => CPU.MaxDeviceNo;

		/// <summary>
		/// Get the available memory on current CPU device in bytes
		/// </summary>
		public static long HostFreeMemory => CPU.FreeAndTotalMemory.free;

		/// <summary>
		/// Get the total memory on current CPU device in bytes
		/// </summary>
		public static long HostTotalMemory => CPU.FreeAndTotalMemory.total;

		/// <summary>
		/// Get the available and total memory on current CPU device in bytes
		/// </summary>
		public static (long free, long total) HostFreeAndTotalMemory => CPU.FreeAndTotalMemory;

		/// <summary>
		/// Set or get the number of threads used by CPU device.
		/// </summary>
		public static int HostNumerOfThreads { get => CPU.NumberOfThreads; set => CPU.NumberOfThreads = value; }
		#endregion


		#region raw pointer wrapper

		#region inside unmanaged
		/// <summary>
		/// Fill the array by same value, byte by byte.
		/// </summary>
		/// <typeparam name="T">any struct</typeparam>
		/// <param name="ptr">pointer to be filled</param>
		/// <param name="value">value to set</param>
		/// <param name="length">length in <typeparamref name="T"/></param>
		/// <param name="offset">offset of <paramref name="ptr"/></param>
		public static void SetValue<T>(this Storage<T> ptr, byte value, long length = 0, long offset = 0) where T : struct
		{
			if (ptr is null)
				throw new ArgumentNullException(nameof(ptr));
			if (length < 0 || length > ptr.Length)
				throw new ArgumentOutOfRangeException(nameof(length));
			if (length == 0) length = ptr.Length;

			var func = ptr.OnHost ? new IRuntime.DelegateSetMemoryValue<T>(CPU.SetMemoryValue) : GPU.SetMemoryValue;
			func(ptr + offset, length, value);
		}

		/// <summary>
		/// Simple copy wrapper function.
		/// </summary>
		/// <typeparam name="T">any struct</typeparam>
		/// <param name="source">source pointer to copy from</param>
		/// <param name="dest">destination pointer to copy into</param>
		/// <param name="length">length to copy</param>
		/// <param name="offsetSource">offset to <paramref name="source"/></param>
		/// <param name="offsetDest">offset to <paramref name="dest"/></param>
		public static void CopyTo<T>(this Storage<T> source, Storage<T> dest, long length, long offsetSource = 0, long offsetDest = 0) where T : struct
		{
			if (source is null)
				throw new ArgumentNullException(nameof(source));
			if (dest is null)
				throw new ArgumentNullException(nameof(dest));
			if (length <= 0) return;

			var func = source.OnHost && dest.OnHost ? new IRuntime.DelegateMemoryCopy<T>(CPU.MemoryCopy) : GPU.MemoryCopy;
			func(source + offsetSource, dest + offsetDest, length);
		}

		/// <summary>
		/// Copy a rectangle range of the source matrix to the destination matrix
		/// </summary>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <param name="source">source matrix</param>
		/// <param name="srcLD">leading dimension of <paramref name="source"/></param>
		/// <param name="copyNRows">copy area height, in <typeparamref name="T"/> rather than bytes</param>
		/// <param name="copyNCols">copy area width, in <typeparamref name="T"/> rather than bytes</param>
		/// <param name="dest">destination matrix</param>
		/// <param name="dstLD">leading dimension of <paramref name="dest"/></param>
		/// <param name="offsetDestRow">destination matrix height offset, in <typeparamref name="T"/> rather than bytes</param>
		/// <param name="offsetDestCol">destination matrix width offset, in <typeparamref name="T"/> rather than bytes</param>
		/// <param name="offsetSouceRow">source matrix height offset, in <typeparamref name="T"/> rather than bytes</param>
		/// <param name="offsetSouceCol">source matrix width offset, in <typeparamref name="T"/> rather than bytes</param>
		/// <returns>the destination matrix; if <paramref name="dest"/>==null, a new matrix is created and returned</returns>
		public static void CopyMatrixTo<T>(this PureArray<T> source, PureArray<T> dest, long srcLD, long dstLD, long copyNRows, long copyNCols, long offsetSouceRow = 0, long offsetSouceCol = 0, long offsetDestRow = 0, long offsetDestCol = 0) where T : struct, IComparable<T>
		{
			CopyMatrixTo(source.Pointer, dest.Pointer, srcLD, dstLD, copyNRows, copyNCols, offsetSouceRow, offsetSouceCol, offsetDestRow, offsetDestCol);
		}

		/// <summary>
		/// Copy a rectangle range of the source matrix to the destination matrix
		/// </summary>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <param name="source">source matrix</param>
		/// <param name="srcLD">leading dimension of <paramref name="source"/></param>
		/// <param name="copyNRows">copy area height, in <typeparamref name="T"/> rather than bytes</param>
		/// <param name="copyNCols">copy area width, in <typeparamref name="T"/> rather than bytes</param>
		/// <param name="dest">destination matrix</param>
		/// <param name="dstLD">leading dimension of <paramref name="dest"/></param>
		/// <param name="offsetDestRow">destination matrix height offset, in <typeparamref name="T"/> rather than bytes</param>
		/// <param name="offsetDestCol">destination matrix width offset, in <typeparamref name="T"/> rather than bytes</param>
		/// <param name="offsetSouceRow">source matrix height offset, in <typeparamref name="T"/> rather than bytes</param>
		/// <param name="offsetSouceCol">source matrix width offset, in <typeparamref name="T"/> rather than bytes</param>
		public static void CopyMatrixTo<T>(this Storage<T> source, Storage<T> dest, long srcLD, long dstLD, long copyNRows, long copyNCols, long offsetSouceRow = 0, long offsetSouceCol = 0, long offsetDestRow = 0, long offsetDestCol = 0) where T : struct, IComparable<T>
		{
			if (source is null)
				throw new ArgumentNullException(nameof(source));
			if (dest is null)
				throw new ArgumentNullException(nameof(dest));
			//// shall be checked in Storage<T>
			////if (srcLD * copyNCols + srcLD * offsetSouceCol + offsetSouceRow >= source.Length)
			////	throw new ArgumentOutOfRangeException(nameof(source));

			var func = source.OnHost && dest.OnHost ? new IRuntime.DelegateMemoryCopy2D<T>(CPU.MemoryCopy2D) : GPU.MemoryCopy2D;
			func(source + (offsetSouceCol * srcLD + offsetSouceRow), srcLD, dest + (offsetDestCol * dstLD + offsetDestRow), dstLD, copyNRows, copyNCols);
		}
		#endregion

		#region unmanaged to managed
		/// <summary>
		/// Copy value at a certain position of an array pointer into a instance value
		/// </summary>
		/// <typeparam name="T">any struct</typeparam>
		/// <param name="source">source array pointer</param>
		/// <param name="offset">offset to source pointer, in the count of <typeparamref name="T"/> rather than bytes</param>
		public static T CopyOut<T>(this Storage<T> source, long offset = 0) where T : struct
		{
			if (source is null)
				throw new ArgumentNullException(nameof(source), Resource.ArrayCannotNull);
			T value = new T();
			// T is a struct and therefor a value type. GCHandle will pin a copy of destination, not destination itself
			GCHandle handle = GCHandle.Alloc(value, GCHandleType.Pinned);
			try
			{
				var ptr = Storage<T>.Create(handle.AddrOfPinnedObject(), 0, onHost: true);
				var func = source.OnHost ? new IRuntime.DelegateMemoryCopy<T>(CPU.MemoryCopy) : GPU.MemoryCopy;
				func(source + offset, ptr, length: 1);
				// Copy data from pinned clone to original destination
				value = (T)handle.Target;
			}
			finally
			{
				handle.Free();
			}
			
			return value;
		}

		/// <summary>
		/// Copy values starts at a certain position and ends else where of an array into an instance value array
		/// </summary>
		/// <typeparam name="T">any struct</typeparam>
		/// <param name="source">source array pointer</param>
		/// <param name="length">length of array <typeparamref name="T"/>[]</param>
		/// <param name="offset">offset to source pointer, in the count of <typeparamref name="T"/> rather than bytes</param>
		public static T[] CopyOutArray<T>(this Storage<T> source, long length = 0, long offset = 0) where T : struct
		{
			if (source is null)
				throw new ArgumentNullException(nameof(source));
			if (length < 0 || length + offset > source.Length)
				throw new ArgumentOutOfRangeException(nameof(length));
			if (length == 0) length = source.Length - offset;
			T[] value = new T[length];
			GCHandle handle = GCHandle.Alloc(value, GCHandleType.Pinned);
			try
			{
				var ptr = Storage<T>.Create(handle.AddrOfPinnedObject(), 0, onHost: true);
				var func = source.OnHost ? new IRuntime.DelegateMemoryCopy<T>(CPU.MemoryCopy) : GPU.MemoryCopy;
				func(source + offset, ptr, length);
				// Copy data from pinned clone to original destination
				value = (T[])handle.Target;
			}
			finally
			{
				handle.Free();
			}
			return value;
		}

		/// <summary>
		/// Copy values of at a certain position range of a matrix into an instance value array in column major
		/// </summary>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <param name="source">source array</param>
		/// <param name="leadDim">lead dimension of copy out matrix</param>
		/// <param name="copyCols">number of columns to copy</param>
		/// <param name="copyRows">number of rows to copy, default = 0 means equal to <paramref name="leadDim"/></param>
		/// <param name="offset">offset to source pointer, in the count of <typeparamref name="T"/> rather than bytes</param>
		public static T[,] CopyOutMatrix<T>(this Storage<T> source, long leadDim, long copyCols, long copyRows = 0, long offset = 0) where T : struct
		{
			if (copyRows <= 0)
				copyRows = leadDim;
			return CopyOutColumnMajorMatrix(source, leadDim, copyCols, copyRows, offset).Make2DArray(copyRows, copyCols);
		}

		/// <summary>
		/// Copy values of a certain range of a matrix pointer into an new instance of C# array in column major.
		/// </summary>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <param name="source">source pointer <see cref="Storage{T}"/></param>
		/// <param name="leadDim">lead dimension of copy out matrix</param>
		/// <param name="copyCols">number of columns to copy</param>
		/// <param name="copyRows">number of rows to copy, default = 0 means equal to <paramref name="leadDim"/></param>
		/// <param name="offset">offset to source pointer, in the count of <typeparamref name="T"/> rather than bytes</param>
		public static T[] CopyOutColumnMajorMatrix<T>(this Storage<T> source, long leadDim, long copyCols, long copyRows = 0, long offset = 0) where T : struct
		{
			if (source is null)
				throw new ArgumentNullException(nameof(source));
			if (copyRows <= 0)
				copyRows = leadDim;
			if (leadDim <= 0)
				throw new ArgumentOutOfRangeException(nameof(leadDim));
			if (copyCols <= 0)
				throw new ArgumentOutOfRangeException(nameof(copyCols));
			if (offset < 0)
				throw new ArgumentOutOfRangeException(nameof(offset));
			if (leadDim * (copyCols - 1) + offset > source.Length)
				throw new ArgumentOutOfRangeException();
			T[] value = new T[copyRows * copyCols];
			// T is a struct and therefor a value type. GCHandle will pin a copy of destination, not destination itself
			GCHandle handle = GCHandle.Alloc(value, GCHandleType.Pinned);
			try
			{
				var ptr = Storage<T>.Create(handle.AddrOfPinnedObject(), 0, onHost: true);
				var func = source.OnHost ? new IRuntime.DelegateMemoryCopy2D<T>(CPU.MemoryCopy2D) : GPU.MemoryCopy2D;
				func(source + offset, leadDim, ptr, copyRows, copyRows, copyCols);
				// Copy data from pinned clone to original destination
				value = (T[])handle.Target;
			}
			finally
			{
				handle.Free();
			}
			return value;
		}
		#endregion

		#region managed to unmanaged
		/// <summary>
		/// Copy a instance of value to a certain position of an <see cref="PureArray{T}"/>
		/// </summary>
		/// <typeparam name="T">any struct</typeparam>
		/// <param name="dest">destination array</param>
		/// <param name="value">value to copy</param>
		/// <param name="offset">offset of destination pointer, in the count of <typeparamref name="T"/> rather than bytes</param>
		public static void CopyInto<T>(this Storage<T> dest, T value, long offset = 0) where T : struct
		{
			if (dest is null)
				throw new ArgumentNullException(nameof(dest));

			GCHandle handle = GCHandle.Alloc(value, GCHandleType.Pinned);
			try
			{
				var ptr = Storage<T>.Create(handle.AddrOfPinnedObject(), 0, onHost: true);
				var func = dest.OnHost ? new IRuntime.DelegateMemoryCopy<T>(CPU.MemoryCopy) : GPU.MemoryCopy;
				func(ptr, dest + offset, length: 1);
			}
			finally
			{
				handle.Free();
			}
		}

		/// <summary>
		/// Copy an array of value to a certain position of an <see cref="PureArray{T}"/>
		/// </summary>
		/// <typeparam name="T">any struct</typeparam>
		/// <param name="dest">destination array</param>
		/// <param name="value">value array to copy</param>
		/// <param name="length">length to take from the <paramref name="value"/> array</param>
		/// <param name="offset">offset of destination pointer, in the count of <typeparamref name="T"/> rather than bytes</param>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="length"/> is longer than <paramref name="value"/>'s content</exception>
		public static void CopyIntoArray<T>(this Storage<T> dest, T[] value, long length = 0, long offset = 0) where T : struct
		{
			if (dest is null)
				throw new ArgumentNullException(nameof(dest));
			if (value is null)
				throw new ArgumentNullException(nameof(value));
			length = length <= 0 ? value.LongLength : length;
			if (length > value.LongLength || length + offset > dest.Length)
				throw new ArgumentOutOfRangeException(nameof(length));

			GCHandle handle = GCHandle.Alloc(value, GCHandleType.Pinned);
			try
			{
				var ptr = Storage<T>.Create(handle.AddrOfPinnedObject(), 0, onHost: true);
				var func = dest.OnHost ? new IRuntime.DelegateMemoryCopy<T>(CPU.MemoryCopy) : GPU.MemoryCopy;
				func(ptr, dest + offset, length);
			}
			finally
			{
				handle.Free();
			}
		}

		/// <summary>
		/// Copy values of a certain range of a column-major matrix in C# array form into an destination matrix pointer.
		/// </summary>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <param name="dest">destination <see cref="Storage{T}"/></param>
		/// <param name="source">source column-major C# array</param>
		/// <param name="destLeadDim">lead dimension of <paramref name="dest"/></param>
		/// <param name="sourceLeadDim">lead dimension of <paramref name="source"/></param>
		/// <param name="copyCols">number of columns to copy</param>
		/// <param name="copyRows">number of rows to copy</param>
		/// <param name="offsetDest">offset to <paramref name="dest"/>, in the count of <typeparamref name="T"/> rather than bytes</param>
		public static void CopyIntoColumnMajorMatrix<T>(this Storage<T> dest, T[] source, long destLeadDim, long sourceLeadDim, long copyCols, long copyRows, long offsetDest = 0) where T : struct
		{
			if (source is null)
				throw new ArgumentNullException(nameof(source));
			if (dest is null)
				throw new ArgumentNullException(nameof(dest));
			if (copyRows <= 0)
				throw new ArgumentOutOfRangeException(nameof(copyRows));
			if (destLeadDim <= 0)
				throw new ArgumentOutOfRangeException(nameof(destLeadDim));
			if (sourceLeadDim <= 0)
				throw new ArgumentOutOfRangeException(nameof(sourceLeadDim));
			if (copyCols <= 0)
				throw new ArgumentOutOfRangeException(nameof(copyCols));
			if (offsetDest < 0 || destLeadDim * (copyCols - 1) + offsetDest > dest.Length)
				throw new ArgumentOutOfRangeException(nameof(offsetDest));
			if (sourceLeadDim * copyCols > source.Length)
				throw new ArgumentOutOfRangeException(nameof(sourceLeadDim));

			GCHandle handle = GCHandle.Alloc(source, GCHandleType.Pinned);
			try
			{
				var ptr = Storage<T>.Create(handle.AddrOfPinnedObject(), 0, onHost: true);
				var func = dest.OnHost ? new IRuntime.DelegateMemoryCopy2D<T>(CPU.MemoryCopy2D) : GPU.MemoryCopy2D;
				func(source: ptr, sourceLD: sourceLeadDim, dest: dest + offsetDest, destLD: destLeadDim, copyRows, copyCols);
			}
			finally
			{
				handle.Free();
			}
		}
		#endregion

		#endregion


		#region copy wrapper

		#region inside unmanaged
		/// <summary>
		/// Simple copy function.
		/// </summary>
		/// <typeparam name="T">any struct</typeparam>
		/// <param name="source">source array to copy from</param>
		/// <param name="dest">destination array to copy into</param>
		/// <param name="length">length to copy</param>
		/// <param name="offsetSource">offset to <paramref name="source"/></param>
		/// <param name="offsetDest">offset to <paramref name="dest"/></param>
		public static void CopyTo<T>(this PureArray<T> source, PureArray<T> dest, long length = 0, long offsetSource = 0, long offsetDest = 0) where T : struct, IComparable<T>
		{
			if (source is null)
				throw new ArgumentNullException(nameof(source), Resource.ArrayCannotNull);
			if (dest is null)
				throw new ArgumentNullException(nameof(dest), Resource.ArrayCannotNull);
			if (offsetDest < 0) offsetDest = 0;
			if (offsetSource < 0) offsetSource = 0;
			if (length == 0)
				length = Math.Min(source.ActualLength - offsetSource, dest.ActualLength - offsetDest);
			if (source.ActualLength < offsetSource + length || dest.ActualLength < offsetDest + length)
				throw new ArgumentOutOfRangeException(nameof(length));
			CopyTo(source.Pointer, dest.Pointer, length, offsetSource, offsetDest);
		}

		/// <summary>
		/// Copy a rectangle range of the source matrix to the destination matrix
		/// </summary>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <param name="source">source matrix</param>
		/// <param name="copyNRows">copy area height, in <typeparamref name="T"/> rather than bytes</param>
		/// <param name="copyNCols">copy area width, in <typeparamref name="T"/> rather than bytes</param>
		/// <param name="dest">destination matrix</param>
		/// <param name="offsetDestRow">destination matrix height offset, in <typeparamref name="T"/> rather than bytes</param>
		/// <param name="offsetDestCol">destination matrix width offset, in <typeparamref name="T"/> rather than bytes</param>
		/// <param name="offsetSouceRow">source matrix height offset, in <typeparamref name="T"/> rather than bytes</param>
		/// <param name="offsetSouceCol">source matrix width offset, in <typeparamref name="T"/> rather than bytes</param>
		/// <exception cref="ArgumentOutOfRangeException">if the <paramref name="dest"/> or <paramref name="source"/> copy height/width are out of range</exception>
		public static void CopyMatrixTo<T>(DenseMatrix<T> source, DenseMatrix<T> dest, long copyNRows, long copyNCols, long offsetSouceRow = 0, long offsetSouceCol = 0, long offsetDestRow = 0, long offsetDestCol = 0) where T : struct, IComparable<T>
		{
			if (source is null || source == PureArray<T>.EmptyDnMat)
				throw new ArgumentNullException(nameof(source), Resource.ArrayCannotNull);
			if (dest is null || dest == PureArray<T>.EmptyDnMat)
				throw new ArgumentNullException(nameof(dest), Resource.ArrayCannotNull);
			if (copyNCols + offsetSouceCol > source.NCols || copyNCols + offsetDestCol > dest.NCols)
				throw new ArgumentOutOfRangeException(nameof(copyNCols));
			CopyMatrixTo(source.Pointer, dest.Pointer, source.LeadDim, dest.LeadDim, copyNRows, copyNCols, offsetSouceRow, offsetSouceCol, offsetDestRow, offsetDestCol);
		}
		#endregion

		#region unmanaged to managed
		/// <summary>
		/// Copy value at a certain position of an array into a instance value
		/// </summary>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <param name="source">source array</param>
		/// <param name="offset">offset to source pointer, in the count of <typeparamref name="T"/> rather than bytes</param>
		public static T CopyOut<T>(this PureArray<T> source, long offset = 0) where T : struct, IComparable<T>
		{
			if (source is null)
				throw new ArgumentNullException(nameof(source), Resource.ArrayCannotNull);
			if (offset < 0) offset = 0;
			if (offset >= source.ActualLength)
				throw new ArgumentOutOfRangeException(nameof(offset));
			return CopyOut(source.Pointer, offset);
		}

		/// <summary>
		/// Copy values starts at a certain position and ends else where of an array into an instance value array
		/// </summary>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <param name="source">source array</param>
		/// <param name="length">length of array of <typeparamref name="T"/>, default 0 means length of <paramref name="source"/> from <paramref name="offset"/></param>
		/// <param name="offset">offset to source pointer, in the count of <typeparamref name="T"/> rather than bytes</param>
		public static T[] CopyOutArray<T>(this PureArray<T> source, long length = 0, long offset = 0) where T : struct, IComparable<T>
		{
			if (source is null)
				throw new ArgumentNullException(nameof(source), Resource.ArrayCannotNull);
			if (offset < 0) offset = 0;
			if (length == 0)
				length = source.ActualLength - offset;
			if (offset + length > source.ActualLength)
				throw new ArgumentOutOfRangeException(nameof(offset));
			return CopyOutArray(source.Pointer, length, offset);
		}

		/// <summary>
		/// Copy values of a certain range of a matrix pointer into an new instance of C# array in column major.
		/// </summary>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <param name="source">source array <see cref="PureArray{T}"/></param>
		/// <param name="leadDim">lead dimension of copy out matrix</param>
		/// <param name="copyCols">number of columns to copy</param>
		/// <param name="copyRows">number of rows to copy, default = 0 means equal to <paramref name="leadDim"/></param>
		/// <param name="offset">offset to source pointer, in the count of <typeparamref name="T"/> rather than bytes</param>
		public static T[] CopyOutColumnMajorMatrix<T>(this PureArray<T> source, long leadDim, long copyCols, long copyRows = 0, long offset = 0) where T : struct, IComparable<T>
		{
			return CopyOutColumnMajorMatrix(source.Pointer, leadDim, copyCols, copyRows, offset);
		}

		/// <summary>
		/// Copy values of at a certain position range of a matrix into an instance value array in column major
		/// </summary>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <param name="source">source array</param>
		/// <param name="rows">number of rows to copy out</param>
		/// <param name="cols">number of columns to copy out</param>
		/// <param name="offset">offset to source pointer, in the count of <typeparamref name="T"/> rather than bytes</param>
		public static T[,] CopyOutMatrix<T>(this DenseMatrix<T> source, long rows = 0, long cols = 0, long offset = 0) where T : struct, IComparable<T>
		{
			if (source is null || source == PureArray<T>.EmptyDnMat)
				throw new ArgumentNullException(nameof(source), Resource.ArrayCannotNull);
			if (rows <= 0)
				rows = source.NRows;
			if (cols <= 0)
				cols = source.NCols;
			if (offset < 0) offset = 0;
			if (offset >= source.ActualLength)
				throw new ArgumentOutOfRangeException(nameof(offset));
			return CopyOutMatrix(source.Pointer, source.LeadDim, cols, copyRows: rows, offset: offset);
		}

		/// <summary>
		/// Copy values of at a certain position range of a matrix into an instance value array in column major
		/// </summary>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <param name="source">source array</param>
		/// <param name="rows">number of rows to copy out</param>
		/// <param name="cols">number of columns to copy out</param>
		/// <param name="offset">offset to source pointer, in the count of <typeparamref name="T"/> rather than bytes</param>
		public static T[] CopyOutColumnMajorMatrix<T>(this DenseMatrix<T> source, long rows = 0, long cols = 0, long offset = 0) where T : struct, IComparable<T>
		{
			if (source is null || source == PureArray<T>.EmptyDnMat)
				throw new ArgumentNullException(nameof(source), Resource.ArrayCannotNull);
			if (rows <= 0)
				rows = source.NRows;
			if (cols <= 0)
				cols = source.NCols;
			if (offset < 0) offset = 0;
			if (offset >= source.ActualLength)
				throw new ArgumentOutOfRangeException(nameof(offset));
			return CopyOutColumnMajorMatrix(source.Pointer, source.LeadDim, cols, copyRows: rows, offset: offset);
		}
		#endregion

		#region managed to unmanaged
		/// <summary>
		/// Copy a instance of value to a certain position of an <see cref="PureArray{T}"/>
		/// </summary>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <param name="dest">destination array</param>
		/// <param name="value">value to copy</param>
		/// <param name="offset">offset of destination pointer, in the count of <typeparamref name="T"/> rather than bytes</param>
		public static void CopyInto<T>(this PureArray<T> dest, T value, long offset = 0) where T : struct, IComparable<T>
		{
			if (dest is null)
				throw new ArgumentNullException(nameof(dest), Resource.ArrayCannotNull);
			if (offset < 0) offset = 0;
			if (offset >= dest.ActualLength)
				throw new ArgumentOutOfRangeException(nameof(offset));
			CopyInto(dest.Pointer, value, offset);
		}

		/// <summary>
		/// Copy an array of value to a certain position of an <see cref="PureArray{T}"/>
		/// </summary>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <param name="dest">destination array</param>
		/// <param name="value">value array to copy</param>
		/// <param name="length">length to take from the <paramref name="value"/> array</param>
		/// <param name="offset">offset of destination pointer, in the count of <typeparamref name="T"/> rather than bytes</param>
		public static void CopyIntoArray<T>(this PureArray<T> dest, T[] value, long length = 0, long offset = 0) where T : struct, IComparable<T>
		{
			if (dest is null)
				throw new ArgumentNullException(nameof(dest), Resource.ArrayCannotNull);
			if (offset < 0) offset = 0;
			if (length == 0)
				length = dest.ActualLength - offset;
			if (offset + length > dest.ActualLength)
				throw new ArgumentOutOfRangeException(nameof(offset));
			CopyIntoArray(dest.Pointer, value, length, offset);
		}

		/// <summary>
		/// Copy values of a certain range of a column-major matrix in C# array form into an destination dense matrix.
		/// </summary>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <param name="dest">destination <see cref="DenseMatrix{T}"/></param>
		/// <param name="source">source column-major C# array</param>
		/// <param name="destLeadDim">lead dimension of <paramref name="dest"/></param>
		/// <param name="sourceLeadDim">lead dimension of <paramref name="source"/></param>
		/// <param name="copyCols">number of columns to copy</param>
		/// <param name="copyRows">number of rows to copy</param>
		/// <param name="offsetDest">offset to <paramref name="dest"/>, in the count of <typeparamref name="T"/> rather than bytes</param>
		public static void CopyIntoColumnMajorMatrix<T>(this DenseMatrix<T> dest, T[] source, long destLeadDim = 0, long sourceLeadDim = 0, long copyCols = 0, long copyRows = 0, long offsetDest = 0) where T : struct, IComparable<T>
		{
			if (dest is null || dest == PureArray<T>.EmptyDnMat)
				throw new ArgumentNullException(nameof(source), Resource.ArrayCannotNull);
			if (source is null || source.LongLength == 0)
				throw new ArgumentNullException(nameof(source), Resource.ArrayCannotNull);
			if (destLeadDim <= 0)
				destLeadDim = dest.LeadDim;
			if (copyCols <= 0)
				copyCols = dest.NCols;
			if (copyRows <= 0)
				copyRows = dest.NRows;
			if (sourceLeadDim <= 0)
				sourceLeadDim = copyRows;
			if (offsetDest < 0)
				offsetDest = 0;
			if (offsetDest > dest.ActualLength - destLeadDim * copyCols)
				throw new ArgumentOutOfRangeException(nameof(offsetDest));
			dest.Pointer.CopyIntoColumnMajorMatrix(source, destLeadDim, sourceLeadDim, copyCols, copyRows, offsetDest);
		}

		/// <summary>
		/// Copy values of a certain range of a column-major matrix in C# array form into an destination matrix pointer.
		/// </summary>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <param name="dest">destination <see cref="PureArray{T}"/></param>
		/// <param name="source">source column-major C# array</param>
		/// <param name="destLeadDim">lead dimension of <paramref name="dest"/></param>
		/// <param name="sourceLeadDim">lead dimension of <paramref name="source"/></param>
		/// <param name="copyCols">number of columns to copy</param>
		/// <param name="copyRows">number of rows to copy</param>
		/// <param name="offsetDest">offset to <paramref name="dest"/>, in the count of <typeparamref name="T"/> rather than bytes</param>
		public static void CopyIntoColumnMajorMatrix<T>(this PureArray<T> dest, T[] source, long destLeadDim, long sourceLeadDim, long copyCols, long copyRows, long offsetDest = 0) where T : struct, IComparable<T>
		{
			CopyIntoColumnMajorMatrix(dest.Pointer, source, destLeadDim, sourceLeadDim, copyCols, copyRows, offsetDest);
		}
		#endregion

			#endregion
	}
}
