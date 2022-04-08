using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;


namespace Althea.Backend.Mkl
{
	/// <summary>
	/// The helper structure to provide safe manipulation of CPU memory buffers
	/// </summary>
	/// <remarks>Currently, the host buffers are pooled by the <see cref="ArrayPool{T}"/> of <see cref="byte"/>.</remarks>
	public readonly ref struct CpuBuffer
	{
		private readonly byte[]? hostBuffer;

		/// <summary>
		/// Get the buffer array on CPU memory as an array of <see cref="byte"/>.
		/// </summary>
		public byte[] Buffer {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.hostBuffer ?? Array.Empty<byte>();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private CpuBuffer(long workSpaceHostBytes)
		{
			if (workSpaceHostBytes < 0)
				throw new ArgumentOutOfRangeException(nameof(workSpaceHostBytes), workSpaceHostBytes, Resources.Parameter.CannotNegative);
			if (workSpaceHostBytes > int.MaxValue)
				throw new ArgumentOutOfRangeException(nameof(workSpaceHostBytes), workSpaceHostBytes, Resources.Parameter.InvalidValue);
			this.hostBuffer = workSpaceHostBytes == 0 ? null : ArrayPool<byte>.Shared.Rent((int)workSpaceHostBytes);
		}

		/// <summary>
		/// Create a <see cref="CpuBuffer"/> by indicating the number of bytes required on CPU memory
		/// </summary>
		/// <param name="workSpaceHostBytes">The number of bytes required as the working space on host</param>
		/// <param name="extraDeviceInfo">Whether to allocate the memory space for the extra device info (as a <see cref="int"/>) or not</param>
		/// <returns>The created <see cref="CpuBuffer"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="workSpaceHostBytes"/> is less than 0</exception>
		/// <exception cref="OutOfMemoryException">If the requested number of bytes are too large to be allocated</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static CpuBuffer Create(long workSpaceHostBytes = 0, bool extraDeviceInfo = false)
		{
			return new(workSpaceHostBytes + (extraDeviceInfo ? sizeof(int) : 0));
		}

		/// <summary>
		/// Create a <see cref="CpuBuffer"/> by indicating the number of elements (in <typeparamref name="T"/>) required on current CUDA device and host memory
		/// </summary>
		/// <typeparam name="T">The element type</typeparam>
		/// <param name="workSpaceHostT">The number of elements required on host</param>
		/// <param name="extraDeviceInfo">Whether to allocate the memory space for the extra device info (as a <see cref="int"/>) or not</param>
		/// <returns>The created <see cref="CpuBuffer"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="workSpaceHostT"/> is less than 0</exception>
		/// <exception cref="OutOfMemoryException">If the requested number of bytes are too large to be allocated</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe CpuBuffer Create<T>(int workSpaceHostT = 0, bool extraDeviceInfo = false) where T : unmanaged, INumber<T>
		{
			return new((long)workSpaceHostT * sizeof(T) + (extraDeviceInfo ? sizeof(int) : 0));
		}

		/// <summary>
		/// Release the not pooled allocated buffer(s) of this <see cref="CpuBuffer"/>
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Dispose()
		{
			if (this.hostBuffer is not null)
				ArrayPool<byte>.Shared.Return(this.hostBuffer);
		}
	}


	// Ignore Spelling: Xeon
	/// <summary>
	/// The enum for instruction sets of MKL
	/// </summary>
	public enum MklInstruction
	{
		/// <summary>
		/// Intel® Streaming SIMD Extensions 4.2 (Intel® SSE4.2).
		/// </summary>
		SSE_42 = 0,
		/// <summary>
		/// Intel® Advanced Vector Extensions (Intel® AVX).
		/// </summary>
		AVX = 1,
		/// <summary>
		/// Intel® Advanced Vector Extensions 2 (Intel® AVX2).
		/// </summary>
		AVX2 = 2,
		/// <summary>
		/// Intel AVX-512 on Intel® Xeon Phi™ processors.
		/// </summary>
		AVX512_MIC = 3,
		/// <summary>
		/// Intel® Advanced Vector Extensions 512 (Intel® AVX-512) on Intel® Xeon processors.
		/// </summary>
		AVX512 = 4,
		/// <summary>
		/// Intel® Advanced Vector Extensions 512 (Intel® AVX-512) with support for Vector Neural Network Instructions on Intel® Xeon Phi™ processors.
		/// </summary>
		AVX512_MIC_E1 = 5,
		/// <summary>
		/// Intel® Advanced Vector Extensions 512 (Intel® AVX-512) with support for Vector Neural Network Instructions.
		/// </summary>
		AVX512_E1 = 6,
	}

	/// <summary>
	/// The static class for static global methods and properties of 
	/// </summary>
	public static class MklRuntime
	{
		/// <summary>
		/// Get the MKL version
		/// </summary>
		/// <returns>The MKL's major and minor version</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static (int major, int minor) GetDriverVersion()
		{
			const int StringLength = 198;
			StringBuilder sb = new(StringLength);
			Storage.NativeMethods.MKL_Get_Version_String(sb, StringLength);
			string s = sb.ToString();
			int versionStart = s.IndexOf("Version") + "Version".Length + 1;
			s = s[versionStart..s.IndexOf("Product")];
			var ss = s.Split('.');
			return (Convert.ToInt32(ss[0]), Convert.ToInt32(ss[1]));
		}

		/// <summary>
		/// Get or set the maximum number of threads used by the MKL
		/// </summary>
		public static int NumberOfThreads {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => Storage.NativeMethods.MKL_Get_Max_Threads();
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set {
				if (value > 0 && value <= Environment.ProcessorCount)
					Storage.NativeMethods.MKL_Set_Num_Threads(value);
				else
					throw new ArgumentOutOfRangeException(nameof(value), value, Resources.Parameter.InvalidValue);
			}
		}


		private static bool? _verbose = null;

		/// <summary>
		/// Get or set the verbose mode of MKL
		/// </summary>
		public static bool Verbose {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				if (_verbose.HasValue)
					return _verbose.Value;
				_ = Storage.NativeMethods.MKL_Verbose(0);
				_verbose = false;
				return false;
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set {
				_ = Storage.NativeMethods.MKL_Verbose(value ? 1 : 0);
				_verbose = value;
			}
		}


		private static MklInstruction? _instrction = null;
		
		/// <summary>
		/// Get or set the instruction set(s) used by the MKL
		/// </summary>
		public static MklInstruction Instruction {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				if (_instrction.HasValue)
					return _instrction.Value;
				int err = Storage.NativeMethods.MKL_Enable_Instructions(MklInstruction.AVX512);
				if (err != 0)
				{
					_instrction = MklInstruction.AVX512;
					return _instrction.Value;
				}
				err = Storage.NativeMethods.MKL_Enable_Instructions(MklInstruction.AVX2);
				if (err != 0)
				{
					_instrction = MklInstruction.AVX2;
					return _instrction.Value;
				}
				err = Storage.NativeMethods.MKL_Enable_Instructions(MklInstruction.AVX);
				if (err != 0)
				{
					_instrction = MklInstruction.AVX;
					return _instrction.Value;
				}
				err = Storage.NativeMethods.MKL_Enable_Instructions(MklInstruction.SSE_42);
				if (err != 0)
				{
					_instrction = MklInstruction.SSE_42;
					return _instrction.Value;
				}
				throw new InvalidOperationException();
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set {
				int err = Storage.NativeMethods.MKL_Enable_Instructions(value);
				if (err == 0)
					throw new NotSupportedException();
				_instrction = value;
			}
		}
	}

	/// <summary>
	/// The class used to set MKL back-end implementations, inherits <see cref="ISetBackend"/>
	/// </summary>
	public sealed class MklImplementations : ISetBackend
	{
		/// <summary>
		/// The default constructor
		/// </summary>
		public MklImplementations()
		{
			try
			{
				this.Available = Storage.NativeMethods.MKL_Get_Max_Threads() > 0;
			}
			catch (Exception)
			{
				this.Available = false;
			}
		}

		/// <summary>
		/// Get a <see cref="bool"/> indicating whether MKL is available when initializing this instance
		/// </summary>
		public bool Available { get; }

		Type ISetBackend.StorageImplementation => typeof(Storage.StorageApi);

		Type ISetBackend.DenseLinearAlgebraImplementation => typeof(LinearAlgebra.Dense.DenseApi);

		Type ISetBackend.SparseLinearAlgebraImplementation => typeof(int);

		Type ISetBackend.DenseTensorAlgebraImplementation => typeof(int);

		Type ISetBackend.SparseTensorAlgebraImplementation => typeof(int);

		Type ISetBackend.RandomImplementation => typeof(Random.RandomApi);

		Type ISetBackend.SolverImplementation => typeof(int);
	}
}
