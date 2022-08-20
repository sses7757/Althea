using System.Runtime.CompilerServices;

using Althea.LinearAlgebra;
using Althea.LinearAlgebra.Dense;

using NM = Althea.Backend.Mkl.LinearAlgebra.Dense.NativeMethods;


namespace Althea.Backend.Mkl.LinearAlgebra.Dense;

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
	/// <summary>
	/// The default constructor of <see cref="Api"/> that sets the <see cref="GemmJitThreshold"/> to 100
	/// </summary>
	public Api() : this(100) { }

	/// <summary>
	/// The constructor of <see cref="Api"/> that indicates the <see cref="GemmJitThreshold"/>
	/// </summary>
	/// <param name="gemmJitThreshold">If this value ≤ 0, JIT will be disabled</param>
	public Api(int gemmJitThreshold)
	{
		if (gemmJitThreshold <= 0)
		{
			this.cacher = default;
		}
		else
		{
			this.cacher = new(16, 128, gemmJitThreshold, JitCompileGemm);
		}
	}

#pragma warning disable IDE1006
	private readonly record struct GemmInfo(MatrixOperation opA, MatrixOperation opB, long m, long n, long k, Complex<Float64> α, Complex<Float64> β, long lda, long ldb, long ldc, DataType type);
#pragma warning restore IDE1006

	private readonly record struct GemmJitter(IntPtr Handle) : IDisposable
	{
		public readonly void Dispose() => NM.mkl_jit_destroy(this.Handle);

		public readonly delegate* unmanaged<IntPtr, T*, T*, T*, void> GetFunc<T>() where T : unmanaged
		{
			delegate*<IntPtr, delegate* unmanaged<IntPtr, T*, T*, T*, void>> getGemmFunc = default(T) switch
			{
				Float32 => &NM.mkl_jit_get_sgemm_ptr,
				Float64 => &NM.mkl_jit_get_dgemm_ptr,
				Complex<Float32> => &NM.mkl_jit_get_cgemm_ptr,
				Complex<Float64> => &NM.mkl_jit_get_zgemm_ptr,
				_ => null,
			};
			if (getGemmFunc is null)
				return null;
			return getGemmFunc(this.Handle);
		}
	}

	private static bool JitCompileGemm(in GemmInfo info, out GemmJitter jitter)
	{
		IntPtr handle;
		switch (info.type)
		{
			case DataType.RealFloat32:
				NM.mkl_jit_create_sgemm(out handle, MklMatrixLayout.ColMajor, info.opA.ToMkl(), info.opB.ToMkl(), info.m, info.n, info.k, info.α.Real.As<Float64, Float32>(), info.lda, info.ldb, info.β.Real.As<Float64, Float32>(), info.ldc);
				break;
			case DataType.RealFloat64:
				NM.mkl_jit_create_dgemm(out handle, MklMatrixLayout.ColMajor, info.opA.ToMkl(), info.opB.ToMkl(), info.m, info.n, info.k, info.α.Real, info.lda, info.ldb, info.β.Real, info.ldc);
				break;
			case DataType.ComplexFloat32:
				NM.mkl_jit_create_cgemm(out handle, MklMatrixLayout.ColMajor, info.opA.ToMkl(), info.opB.ToMkl(), info.m, info.n, info.k, info.α.As<Complex<Float64>, Complex<Float32>>(), info.lda, info.ldb, info.β.As<Complex<Float64>, Complex<Float32>>(), info.ldc);
				break;
			case DataType.ComplexFloat64:
				NM.mkl_jit_create_zgemm(out handle, MklMatrixLayout.ColMajor, info.opA.ToMkl(), info.opB.ToMkl(), info.m, info.n, info.k, info.α, info.lda, info.ldb, info.β, info.ldc);
				break;
			default:
				handle = default;
				break;
		}
		jitter = new(handle);
		return handle != default;
	}

	private Helpers.CandidateCacher<GemmInfo, GemmJitter> cacher;

	/// <inheritdoc/>
	public void Dispose()
	{
		this.cacher.Dispose();
		this.Disposed = true;
		GC.SuppressFinalize(this);
	}

	/// <inheritdoc/>
	public bool Disposed { get; protected set; } = false;

	/// <summary>
	/// Whether this implementation shall MKL GEMM JIT compiler to cache the frequently used GEMMs or not. If it is enabled, <see cref="ComplexGemmUseGemm3M"/> will be ignored. Default false.
	/// </summary>
	/// <remarks>It will only be better than the traditional ones when the GEMMs with same parameters are invoked more than several hundred of times.</remarks>
	public bool GemmJitCache => this.cacher.IsValid();

	/// <summary>
	/// Get the threshold of number of invocations before start MKL GEMM JIT compile the GEMM with certain parameters.
	/// </summary>
	public int GemmJitThreshold => this.cacher.HitCountThreshold;

	/// <summary>
	/// Get or set the maximum queue size of the MKL GEMM JIT candidates of the GEMMs with different parameter setup.
	/// </summary>
	public int GemmJitCandidateSize
	{
		get => this.cacher.CandidateCapacity;
		set => this.cacher.CandidateCapacity = value;
	}

	/// <summary>
	/// Get or set the maximum number of the MKL GEMM JIT compiled parameter setups.
	/// </summary>
	public int GemmJitSize
	{
		get => this.cacher.CacheCapacity;
		set => this.cacher.CacheCapacity = value;
	}

	/// <summary>
	/// Whether this implementation shall use the Gauss complexity reduction routines ("GEMM3M") or the original complex-typed general matrices multiplications ("GEMM").
	/// </summary>
	public bool ComplexGemmUseGemm3M { get; set; } = true;

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
