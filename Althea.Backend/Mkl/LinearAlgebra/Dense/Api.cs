using System.Runtime.CompilerServices;
using System.Threading;

using Althea.Backend.Storage;
using Althea.LinearAlgebra;
using Althea.LinearAlgebra.Dense;

using NM = Althea.Backend.Mkl.LinearAlgebra.Dense.NativeMethods;


namespace Althea.Backend.Mkl.LinearAlgebra.Dense
{
	#region conjugate helper
	internal readonly unsafe ref struct Conjugater1<T> where T : unmanaged, IBaseNumber<T>
	{
		private readonly T* ptr;
		private readonly MklInt n, inc;
		private readonly delegate*<MklInt, T*, MklInt, T*, MklInt, void> conj;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal Conjugater1(T* ptr, MklInt n, MklInt inc, ref MatrixOperation op)
		{
			this.ptr = ptr; this.n = n; this.inc = inc;
			this.conj = null;
			if (op == MatrixOperation.Conjugate)
			{
				op = MatrixOperation.None;
				this.conj = typeof(T) == typeof(Complex<Float32>) ? &NM.vcConjI : typeof(T) == typeof(Complex<Float64>) ? &NM.vzConjI : null;
				if (this.conj is not null)
					this.conj(n, ptr, inc, ptr, inc);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Dispose()
		{
			if (this.conj is not null)
				this.conj(this.n, this.ptr, this.inc, this.ptr, this.inc);
		}
	}

	internal readonly unsafe ref struct Conjugater2<T> where T : unmanaged, IBaseNumber<T>
	{
		private readonly T* ptr1, ptr2;
		private readonly MklInt len1, len2, inc1, inc2;
		private readonly delegate*<MklInt, T*, MklInt, T*, MklInt, void> conj;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal Conjugater2(T* ptr1, MklInt len1, MklInt inc1, T* ptr2, MklInt len2, MklInt inc2, ref MatrixOperation op)
		{
			this.ptr1 = ptr1; this.ptr2 = ptr2; this.len1 = len1; this.len2 = len2; this.inc1 = inc1; this.inc2 = inc2;
			this.conj = null;
			if (op == MatrixOperation.Conjugate)
			{
				op = MatrixOperation.None;
				this.conj = typeof(T) == typeof(Complex<Float32>) ? &NM.vcConjI : typeof(T) == typeof(Complex<Float64>) ? &NM.vzConjI : null;
				if (this.conj is not null)
					this.conj(len1, ptr1, inc1, ptr1, inc1);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Dispose()
		{
			if (this.conj is not null)
			{
				this.conj(this.len1, this.ptr1, this.inc1, this.ptr1, this.inc1);
				this.conj(this.len2, this.ptr2, this.inc2, this.ptr2, this.inc2);
			}
		}
	}

	internal readonly unsafe ref struct ConjugaterMat1<T> where T : unmanaged, IBaseNumber<T>
	{
		private readonly T* ptr;
		private readonly MklInt m, n, ld;
		private readonly NM.MKL_imatcopy<T>? func;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal ConjugaterMat1(T* ptr, MklInt m, MklInt n, MklInt ld, ref MatrixOperation op)
		{
			this.ptr = ptr; this.m = m; this.n = n; this.ld = ld;
			if (op == MatrixOperation.Conjugate)
			{
				op = MatrixOperation.None;
				this.func = default(T) switch
				{
					Float32 => new NM.MKL_imatcopy<Float32>(NM.MKL_Simatcopy) as NM.MKL_imatcopy<T>,
					Float64 => new NM.MKL_imatcopy<Float64>(NM.MKL_Dimatcopy) as NM.MKL_imatcopy<T>,
					Complex<Float32> => new NM.MKL_imatcopy<Complex<Float32>>(NM.MKL_Cimatcopy) as NM.MKL_imatcopy<T>,
					Complex<Float64> => new NM.MKL_imatcopy<Complex<Float64>>(NM.MKL_Zimatcopy) as NM.MKL_imatcopy<T>,
					_ => null
				};
				this.func?.Invoke(MklMatrixLayoutChar.ColMajor, MklOperationChar.Conjugate, this.m, this.n, T.One, this.ptr, this.ld);
			}
			else
			{
				this.ptr = default; this.func = null;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Dispose()
		{
			this.func?.Invoke(MklMatrixLayoutChar.ColMajor, MklOperationChar.Conjugate, this.m, this.n, T.One, this.ptr, this.ld);
		}
	}

	internal static class Conjugater
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal unsafe static Conjugater1<T> Create<T>(T* ptr, MklInt n, MklInt inc, ref MatrixOperation op) where T : unmanaged, IBaseNumber<T> => new(ptr, n, inc, ref op);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal unsafe static Conjugater2<T> Create<T>(T* ptr1, MklInt len1, MklInt inc1, T* ptr2, MklInt len2, MklInt inc2, ref MatrixOperation op) where T : unmanaged, IBaseNumber<T> => new(ptr1, len1, inc1, ptr2, len2, inc2, ref op);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal unsafe static ConjugaterMat1<T> Create<T>(T* ptr, MklInt m, MklInt n, MklInt ld, ref MatrixOperation op) where T : unmanaged, IBaseNumber<T> => new(ptr, m, n, ld, ref op);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal unsafe static void Conjugate<T>(T* ptr, MklInt n, MklInt inc) where T : unmanaged, IBaseNumber<T>
		{
			delegate*<MklInt, T*, MklInt, T*, MklInt, void> func = typeof(T) == typeof(Complex<Float32>) ? &NM.vcConjI : typeof(T) == typeof(Complex<Float64>) ? &NM.vzConjI : null;
			if (func == null)
				return;
			func(n, ptr, inc, ptr, inc);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal unsafe static bool Scale<T>(T* ptr, MklInt m, MklInt n, MklInt ld, T scalar) where T : unmanaged, IBaseNumber<T>
		{
			var func = default(T) switch
			{
				Float32 => new NM.MKL_imatcopy<Float32>(NM.MKL_Simatcopy) as NM.MKL_imatcopy<T>,
				Float64 => new NM.MKL_imatcopy<Float64>(NM.MKL_Dimatcopy) as NM.MKL_imatcopy<T>,
				Complex<Float32> => new NM.MKL_imatcopy<Complex<Float32>>(NM.MKL_Cimatcopy) as NM.MKL_imatcopy<T>,
				Complex<Float64> => new NM.MKL_imatcopy<Complex<Float64>>(NM.MKL_Zimatcopy) as NM.MKL_imatcopy<T>,
				_ => null
			};
			func?.Invoke(MklMatrixLayoutChar.ColMajor, MklOperationChar.NoneTranspose, m, n, scalar, ptr, ld);
			return func != null;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal unsafe static bool Conjugate<T>(T* ptr, MklInt m, MklInt n, MklInt ld) where T : unmanaged, IBaseNumber<T>
		{
			var func = default(T) switch
			{
				Float32 => new NM.MKL_imatcopy<Float32>(NM.MKL_Simatcopy) as NM.MKL_imatcopy<T>,
				Float64 => new NM.MKL_imatcopy<Float64>(NM.MKL_Dimatcopy) as NM.MKL_imatcopy<T>,
				Complex<Float32> => new NM.MKL_imatcopy<Complex<Float32>>(NM.MKL_Cimatcopy) as NM.MKL_imatcopy<T>,
				Complex<Float64> => new NM.MKL_imatcopy<Complex<Float64>>(NM.MKL_Zimatcopy) as NM.MKL_imatcopy<T>,
				_ => null
			};
			func?.Invoke(MklMatrixLayoutChar.ColMajor, MklOperationChar.Conjugate, m, n, T.One, ptr, ld);
			return func != null;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal unsafe static bool Transpose<T>(T* ptr, MklInt m, MklInt n, MklInt ld, MatrixOperation op) where T : unmanaged, IBaseNumber<T>
		{
			if (m != ld)
				return false;
			var func = default(T) switch
			{
				Complex<Float32> => new NM.MKL_imatcopy<Complex<Float32>>(NM.MKL_Cimatcopy) as NM.MKL_imatcopy<T>,
				Complex<Float64> => new NM.MKL_imatcopy<Complex<Float64>>(NM.MKL_Zimatcopy) as NM.MKL_imatcopy<T>,
				_ => null
			};
			func?.Invoke(MklMatrixLayoutChar.ColMajor, op.ToMklChar(), m, n, T.One, ptr, ld);
			return func != null;
		}
	}
	#endregion


	/// <summary>
	/// The MKL back-end of <see cref="IBlasAbstractApi"/>, <see cref="IExtendBlasAbstractApi"/>, <see cref="IHalfMatrixBlasAbstractApi"/>, <see cref="ILapackAbstractApi"/> that supports storage locations of CPU memory.
	/// </summary>
	public unsafe partial class Api : IBlasAbstractApi, IExtendBlasAbstractApi, IHalfMatrixBlasAbstractApi, ILapackAbstractApi
	{
		#region basic
		static Api()
		{
			try
			{
				NativeMethodsTemplate.vmlClearErrorCallBack();
				Valid = true;
			}
			catch (Exception)
			{
				Valid = false;
			}
		}

		/// <summary>
		/// Statically get or set the MKL VML error callback function.
		/// </summary>
		public static VmlErrorCallbackDelegate? VmlErrorCallback
		{
			get => NativeMethodsTemplate.vmlGetErrorCallBack();
			set
			{
				if (value is null)
					NativeMethodsTemplate.vmlClearErrorCallBack();
				else
					NativeMethodsTemplate.vmlSetErrorCallBack(value);
			}
		}

		/// <summary>
		/// Statically get whether the MKL back-end is valid or not.
		/// </summary>
		public static bool Valid { get; }

		/// <inheritdoc/>
		public void Dispose()
		{
			foreach (var kv in this.compiled)
			{
				kv.Value.locker.EnterWriteLock();
				try
				{
					NM.mkl_jit_destroy(kv.Value.jitter);
				}
				finally
				{
					kv.Value.locker.ExitWriteLock();
					kv.Value.locker.Dispose();
				}
			}
			this.Disposed = true;
			GC.SuppressFinalize(this);
		}

		/// <inheritdoc/>
		public bool Disposed { get; set; } = false;

		/// <summary>
		/// Whether this implementation shall use the Gauss complexity reduction routines ("GEMM3M") or the original complex-typed general matrices multiplications ("GEMM").
		/// </summary>
		public bool ComplexGemmUseGemm3M { get; set; } = true;

		/// <summary>
		/// Whether this implementation shall MKL GEMM JIT compiler to cache the frequently used GEMMs or not. If it is enabled, <see cref="ComplexGemmUseGemm3M"/> will be ignored. Default false.
		/// </summary>
		/// <remarks>It will only be better than the traditional ones when the GEMMs with same parameters are invoked more than several hundred of times.</remarks>
		public bool GemmJitCache { get; set; } = false;

		/// <summary>
		/// Get or set the maximum queue size of the MKL GEMM JIT candidates of the GEMMs with different parameter setup.
		/// </summary>
		public int GemmJitCandidateSize
		{
			get => this.candidates.EnsureCapacity(1);
			set => this.candidates.EnsureCapacity(value);
		}

		/// <summary>
		/// Get or set the maximum number of the MKL GEMM JIT compiled parameter setups.
		/// </summary>
		public int GemmJitSize
		{
			get => this.compiled.EnsureCapacity(1);
			set => this.compiled.EnsureCapacity(value);
		}

		/// <summary>
		/// Get or set the threshold of number of invocations before start MKL GEMM JIT compile the GEMM with certain parameters.
		/// </summary>
		public int GemmJitThreshold { get; set; } = 100;

		private readonly Dictionary<(MatrixOperation opA, MatrixOperation opB, long m, long n, long k, Complex<Float64> α, Complex<Float64> β, long lda, long ldb, long ldc), int> candidates = new(128);
		private readonly Queue<(MatrixOperation opA, MatrixOperation opB, long m, long n, long k, Complex<Float64> α, Complex<Float64> β, long lda, long ldb, long ldc)> candidatesQueue = new(128);

		private readonly Dictionary<(MatrixOperation opA, MatrixOperation opB, long m, long n, long k, Complex<Float64> α, Complex<Float64> β, long lda, long ldb, long ldc), (IntPtr jitter, ReaderWriterLockSlim locker)> compiled = new(16);
		private readonly Queue<(MatrixOperation opA, MatrixOperation opB, long m, long n, long k, Complex<Float64> α, Complex<Float64> β, long lda, long ldb, long ldc)> compiledQueue = new(16);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool GetPointer<T, TS>(TS s, long stride, out T* pointer, out long length, [CallerArgumentExpression("s")] string? sName = null, [CallerArgumentExpression("stride")] string? strideName = null) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
		{
			pointer = default; length = 0;
			if (s is null || !s.IsValid())
				throw new ArgumentNullException(sName);
			if (stride <= 0)
				throw new ArgumentOutOfRangeException(strideName, stride, Resources.ParameterError.MustPositive);
			if (stride > MklInt.MaxValue)
				return false;
			if (s is not PureStorage<T, CpuMemoryPointer> ps)
				return false; // not support
			ps.Pointer.Pointer.UnmangedPointer<T>(ps.Pointer.OffsetInBytes);
			if (pointer == default)
				throw new ArgumentException(Resources.ParameterError.InvalidValue, sName);
			length = ps.Length;
			length = (length - 1) / stride + 1;
			if (length > MklInt.MaxValue || length < 0)
				return false;
			return true;
		}

		// Ignore Spelling: ld
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool GetPointer<T, TS>(TS? s, long m, long n, long ld, out T* pointer, [CallerArgumentExpression("s")] string? sName = null, [CallerArgumentExpression("m")] string? mName = null, [CallerArgumentExpression("n")] string? nName = null, [CallerArgumentExpression("ld")] string? ldName = null) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
		{
			pointer = default;
			if (s is null || !s.IsValid())
				return true;
			if (m <= 0)
				throw new ArgumentOutOfRangeException(mName, m, Resources.ParameterError.MustPositive);
			if (n <= 0)
				throw new ArgumentOutOfRangeException(nName, n, Resources.ParameterError.MustPositive);
			if (ld < m)
				throw new ArgumentOutOfRangeException(ldName, ld, Resources.ParameterError.InvalidValue);
			if (m > MklInt.MaxValue || n > MklInt.MaxValue || ld > MklInt.MaxValue)
				return false;
			if (s is not PureStorage<T, CpuMemoryPointer> ps)
				return false; // not support
			pointer = ps.Pointer.Pointer.UnmangedPointer<T>(ps.Pointer.OffsetInBytes);
			if (pointer == default)
				throw new ArgumentException(Resources.ParameterError.InvalidValue, sName);
			if ((ps.Length + (ld - m)) / ld < n)
				throw new ArgumentException(Resources.ParameterError.WrongSize);
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void RealToComp<TComp>(TComp* real, TComp* comp, long n) where TComp : unmanaged, IBaseNumber<TComp>
		{
			if (!TComp.IsComplexType)
				return;
			Unsafe.InitBlockUnaligned(comp, 0, (uint)(n * sizeof(TComp)));
			if (typeof(TComp) == typeof(Complex<Float32>))
				Storage.Api.PointerStridedCopy((Float32*)real, 1, (Float32*)comp, 2, n);
			else
				Storage.Api.PointerStridedCopy((Float64*)real, 1, (Float64*)comp, 2, n);
		}
		#endregion
	}
}
