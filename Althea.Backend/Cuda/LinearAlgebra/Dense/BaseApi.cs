using System.Runtime.CompilerServices;

using Althea.Helpers;
using Althea.LinearAlgebra;
using Althea.LinearAlgebra.Dense;


namespace Althea.Backend.Cuda.LinearAlgebra.Dense;

#region conjugate helper
internal readonly unsafe ref struct Conjugater1<T> where T : unmanaged, IBaseNumber<T>
{
	private readonly T* ptr;
	private readonly int n, inc;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal Conjugater1(T* ptr, int n, int inc, MatrixOperation op)
	{
		this.ptr = ptr; this.n = n; this.inc = inc;
		if (op == MatrixOperation.Conjugate)
		{
			CustomNativeMethods.vecConj(T.Type, n, ptr, inc, ptr, inc).Check();
		}
		else
		{
			this.n = 0;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose()
	{
		if (this.n != 0)
			CustomNativeMethods.vecConj(T.Type, n, ptr, inc, ptr, inc).Check();
	}
}

internal static class Conjugater
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe static Conjugater1<T> Create<T>(T* ptr, int n, int inc, MatrixOperation op) where T : unmanaged, IBaseNumber<T> => new(ptr, n, inc, op);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe static bool Conjugate<T>(T* ptr, long n, int inc) where T : unmanaged, IBaseNumber<T>
	{
		return CustomNativeMethods.vecConj(T.Type, n, ptr, inc, ptr, inc).Check();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe static bool Conjugate<T>(long n, T* ptr1, int inc1, T* ptr2, int inc2) where T : unmanaged, IBaseNumber<T>
	{
		return CustomNativeMethods.vecConj(T.Type, n, ptr1, inc1, ptr2, inc2).Check();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe static bool Conjugate<T>(T* ptr, long m, long n, long ld) where T : unmanaged, IBaseNumber<T>
	{
		return CustomNativeMethods.matConj(T.Type, m, n, ptr, ld, ptr, ld).Check();
	}
}
#endregion


/// <summary>
/// The CUDA back-end of the <see cref="IBlasAbstractApi"/>, <see cref="IExtendBlasAbstractApi"/>, <see cref="IHalfMatrixBlasAbstractApi"/> and <see cref="ILapackAbstractApi"/> that utilizes cuBLAS and cuSOLVER API with 11.2 ≤ CUDA version (and maybe future versions)
/// </summary>
/// <remarks>The legacy cuBLAS APIs are not supported.<br/>
/// The only supported location is a pure one on GPU memory. But cuFILE cached ones can be supported easily.<br/>
/// The stream operation is not supported here, but it can be easily added by utilizing "cudaStreamCreate()", "cublasSetStream()", etc.<br/>
/// The packed matrix, batched matrices and banded matrix BLAS operations are not supported, but it can be easily added as well.<br/>
/// The cuSOLVER MultiGPU library is not supported, but it can be easily added as well.</remarks>
public unsafe partial class Api : IBindedDevice, IBlasAbstractApi, IExtendBlasAbstractApi, IHalfMatrixBlasAbstractApi, ILapackAbstractApi
{
	#region basic
	/// <summary>
	/// The actual CUDA library handles used in its API calls
	/// </summary>
	protected readonly IntPtr cublasHandle, cusolverHandle;

	/// <inheritdoc/>
	public int BindedDeviceID { get; }

	/// <summary>
	/// Get or set whether the CDUA BLAS library uses the atomics mode or not
	/// </summary>
	public bool UseAtomicsMode
	{
		get
		{
			NativeMethods.cublasGetAtomicsMode(this.cublasHandle, out var mode).Check();
			return mode == CuBlasAtomicsMode.Allowed;
		}
		set
		{
			CuBlasAtomicsMode mode = value ? CuBlasAtomicsMode.Allowed : CuBlasAtomicsMode.NotAllowed;
			NativeMethods.cublasSetAtomicsMode(this.cublasHandle, mode).Check();
		}
	}

	/// <summary>
	/// Whether this implementation shall use the Gauss complexity reduction routines ("GEMM3M") or the original complex-typed general matrices multiplications ("GEMM")
	/// </summary>
	public bool ComplexGemmUseGemm3m
	{
		get => this._complexGemm3m;
		set
		{
			if (value)
			{
				var cap = Runtime.GetDeviceComputeCapability(Runtime.CurrentDeviceID);
				if (cap.major < 5)
				{
					Log.Write(string.Format(Resource.InsufficientCudaCapability, cap, (5, 0)));
					return;
				}
			}
			this._complexGemm3m = value;
		}
	}

	private bool _complexGemm3m = false;

	/// <summary>
	/// Get or set a <see cref="bool"/> to indicate whether this implementation shall use the polar decomposition to perform the singular value decomposition or the legacy QR decomposition to do so.
	/// </summary>
	/// <remarks>The polar decomposition approach is much faster but may leads to larger error(s) when the matrix to be decomposed is (near) singularity.</remarks>
	public bool SvdViaPolarDecomposition { get; set; }

	/// <summary>
	/// The default constructor of <see cref="Api"/>
	/// </summary>
	public Api()
	{
		this.BindedDeviceID = Runtime.CurrentDeviceID;
		NativeMethods.cublasCreate(out this.cublasHandle).Check();
		NativeMethods.cublasSetPointerMode(this.cublasHandle, CuBlasPointerMode.Host);
		this.UseAtomicsMode = true;
		NativeMethods.cusolverDnCreate(out this.cusolverHandle).Check();
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		NativeMethods.cublasDestroy(this.cublasHandle);
		NativeMethods.cusolverDnDestroy(this.cusolverHandle);
		this.Disposed = true;
		GC.SuppressFinalize(this);
	}

	/// <inheritdoc/>
	public bool Disposed { get; protected set; } = false;


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void StridedCopy<T>(T* from, T* to, int n, int incx, int incy) where T : unmanaged, IBaseNumber<T>
	{
		delegate*<IntPtr, int, T*, int, T*, int, CudaBlasStatus> func = default(T) switch
		{
			Float32 => &NativeMethods.cublasScopy,
			Float64 => &NativeMethods.cublasDcopy,
			Complex<Float32> => &NativeMethods.cublasCcopy,
			Complex<Float64> => &NativeMethods.cublasZcopy,
			_ => null,
		};
		func(this.cublasHandle, n, from, incx, to, incy).Check();
	}
	#endregion
}