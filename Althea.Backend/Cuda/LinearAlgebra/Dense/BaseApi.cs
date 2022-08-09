using System.Runtime.CompilerServices;

using Althea.Backend.Cuda.Storage;
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
			CustomNativeMethods.vecConj(T.Type, n, ptr, inc).Check();
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
			CustomNativeMethods.vecConj(T.Type, n, ptr, inc).Check();
	}
}

internal static class Conjugater
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe static Conjugater1<T> Create<T>(T* ptr, int n, int inc, MatrixOperation op) where T : unmanaged, IBaseNumber<T> => new(ptr, n, inc, op);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe static bool Conjugate<T>(T* ptr, int n, int inc) where T : unmanaged, IBaseNumber<T>
	{
		return CustomNativeMethods.vecConj(T.Type, n, ptr, inc).Check();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe static bool Conjugate<T>(T* ptr, int m, int n, int ld) where T : unmanaged, IBaseNumber<T>
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


	#region custom level 1
	protected override unsafe bool AggregateProduct_<T>(Storage<T> x, int stride, out T product)
	{
		product = default;
		if (!Const<T>.IsPreDefinedNoHalf)
			return false;
		if (!CheckPointerLong(x, out var px, out var n, stride))
			return false;
		T result;
		NativeMethods.vecProd(T.Type, px, n, stride, &result);
		product = result;
		return true;
	}

	protected override unsafe bool AggregateSum_<T>(Storage<T> x, int stride, out T sum)
	{
		sum = default;
		if (!Const<T>.IsPreDefinedNoHalf)
			return false;
		if (!CheckPointerLong(x, out var px, out var n, stride))
			return false;
		T result;
		NativeMethods.vecSum(T.Type, px, n, stride, &result);
		sum = result;
		return true;
	}

	protected override unsafe bool PartialProduct_<T>(Storage<T> x, int strideX, Storage<T> y, int strideY, bool inclusive)
	{
		if (!Const<T>.IsPreDefinedNoHalf)
			return false;
		if (!CheckPointerLong(x, out var px, out var nx, strideX))
			return false;
		if (!CheckPointerLong(y, out var py, out var ny, strideY))
			return false;
		long n = Math.Min(nx, ny);
		NativeMethods.vecParProd(T.Type, px, py, n, inclusive, strideX, strideY);
		return true;
	}

	protected override bool PartialSum_<T>(Storage<T> x, int strideX, Storage<T> y, int strideY, bool inclusive)
	{
		if (!Const<T>.IsPreDefinedNoHalf)
			return false;
		if (!CheckPointerLong(x, out var px, out var nx, strideX))
			return false;
		if (!CheckPointerLong(y, out var py, out var ny, strideY))
			return false;
		long n = Math.Min(nx, ny);
		NativeMethods.vecParSum(T.Type, px, py, n, inclusive, strideX, strideY);
		return true;
	}

	protected override unsafe bool PointWiseAddScalar_<T>(Storage<T> x, int stride, T scalr)
	{
		if (scalr.IsZero())
			return true;
		if (!Const<T>.IsPreDefinedNoHalf)
			return false;
		if (!CheckPointerLong(x, out var px, out var n, stride))
			return false;
		NativeMethods.vecAddScalar(T.Type, px, &scalr, n, stride);
		return true;
	}

	protected override bool PointWiseCast_<T, TOut>(Storage<T> source, int incSrc, Storage<TOut> destination, int incDst)
	{
		if (!Const<T>.IsPreDefinedNoHalf)
			return false;
		if (!CheckPointerLong(source, out var px, out var nx, incSrc))
			return false;
		if (!CheckPointerLong(destination, out var py, out var ny, incDst))
			return false;
		long n = Math.Min(nx, ny);
		NativeMethods.vecDataConvert(T.Type, Const<TOut>.DataType, px, py, n, incSrc, incDst, true).Check();
		return true;
	}

	protected override bool PointWiseConjugate_<T>(Storage<T> x, int stride)
	{
		if (!Const<T>.IsPreDefinedNoHalf)
			return false;
		if (!CheckPointerLong(x, out var px, out var n, stride))
			return false;
		NativeMethods.vecConj(T.Type, px, n, stride);
		return true;
	}

	protected override bool PointWiseDivide_<T>(Storage<T> x, int strideX, Storage<T> y, int strideY)
	{
		if (!Const<T>.IsPreDefinedNoHalf)
			return false;
		if (!CheckPointerLong(x, out var px, out var nx, strideX))
			return false;
		if (!CheckPointerLong(y, out var py, out var ny, strideY))
			return false;
		long n = Math.Min(nx, ny);
		NativeMethods.vecsMulDiv(T.Type, px, py, n, strideX, strideY, false);
		return true;
	}

	protected override bool PointWiseEquals_<T>(Storage<T> x, int strideX, Storage<T> y, int strideY, out bool equals)
	{
		equals = false;
		if (!Const<T>.IsPreDefinedNoHalf)
			return false;
		if (!CheckPointerLong(x, out var px, out var nx, strideX))
			return false;
		if (!CheckPointerLong(y, out var py, out var ny, strideY))
			return false;
		long n = Math.Min(nx, ny);
		equals = NativeMethods.vecsEq(T.Type, px, py, n, strideX, strideY);
		return true;
	}

	protected override bool PointWiseMultiply_<T>(Storage<T> x, int strideX, Storage<T> y, int strideY)
	{
		if (!Const<T>.IsPreDefinedNoHalf)
			return false;
		if (!CheckPointerLong(x, out var px, out var nx, strideX))
			return false;
		if (!CheckPointerLong(y, out var py, out var ny, strideY))
			return false;
		long n = Math.Min(nx, ny);
		NativeMethods.vecsMulDiv(T.Type, px, py, n, strideX, strideY, true);
		return true;
	}

	protected override unsafe bool PointWisePower_<T>(Storage<T> x, int stride, double p)
	{
		if (p == 1)
			return true;
		if (!Const<T>.IsPreDefinedNoHalf)
			return false;
		if (!CheckPointerLong(x, out var px, out var n, stride))
			return false;
		if (p == 0)
		{
			T one = Const<T>.One;
			Storage.NativeMethods.vecFillVal(T.Type, px, &one, n, stride);
			return true;
		}
		if (p == 2)
		{
			NativeMethods.vecsMulDiv(T.Type, px, px, n, stride, stride, true);
			return true;
		}
		T pp = p.FromDouble<T>(); // for complex type, (&pp)[0..sizeof(T)/2] == (T::value_type)p
		NativeMethods.vecPowSameType(T.Type, px, &pp, n, stride);
		return true;
	}

	protected override unsafe bool PointWisePower_<T>(Storage<T> x, int stride, T p)
	{
		if (p.IsEqual(Const<T>.One))
			return true;
		if (!Const<T>.IsPreDefinedNoHalf)
			return false;
		if (!CheckPointerLong(x, out var px, out var n, stride))
			return false;
		if (p.IsZero())
		{
			T one = Const<T>.One;
			Storage.NativeMethods.vecFillVal(T.Type, px, &one, n, stride);
			return true;
		}
		if (p.IsEqual(Const<T>.Two))
		{
			NativeMethods.vecsMulDiv(T.Type, px, px, n, stride, stride, true);
			return true;
		}
		NativeMethods.vecPowSameType(T.Type, px, &p, n, stride);
		return true;
	}

	protected override unsafe bool TruncateArray_<T>(Storage<T> x, int stride, double threshold)
	{
		if (threshold <= 0)
			throw new ArgumentOutOfRangeException(nameof(threshold), threshold, Resources.Parameter.MustPositive);
		if (!Const<T>.IsPreDefinedNoHalf)
			return false;
		if (!CheckPointerLong(x, out var px, out var n, stride))
			return false;
		T pp = threshold.FromDouble<T>();
		NativeMethods.vecClip(T.Type, px, &pp, n, stride);
		return true;
	}
	#endregion


	#region custom level 3
	protected override bool MatrixCopyUpperLowerParts_<T>(bool storedUpper, bool hermitian, long n, Storage<T> A, long lda)
	{
		if (!Const<T>.IsPreDefinedNoHalf)
			return false;
		if (!CheckPointerLong(A, n, lda, out var pA))
			return false;
		NativeMethods.matMakeHerm(T.Type, pA, lda, n, storedUpper, hermitian);
		return true;
	}

	protected override bool MatrixClearUpperLowerPart_<T>(bool clearLower, long n, Storage<T> A, long lda)
	{
		if (!Const<T>.IsPreDefinedNoHalf)
			return false;
		if (!CheckPointerLong(A, n, lda, out var pA))
			return false;
		NativeMethods.matTriClear(T.Type, pA, lda, n, clearLower);
		return true;
	}

	// Ignore Spelling: lda ma na ldb mb nb ldc
	protected override unsafe bool MatrixKronecker_<T>(long ma, long na, long mb, long nb, T α, Storage<T> A, long lda, Storage<T> B, long ldb, T β, Storage<T> C, long ldc)
	{
		if (!Const<T>.IsPreDefinedNoHalf)
			return false;
		if (!CheckPointerLong(A, na, lda, out var pA))
			return false;
		if (!CheckPointerLong(B, nb, ldb, out var pB))
			return false;
		////if (ldc < ma * mb)
		////	throw new ArgumentException(Resources.Parameter.InvalidValue, nameof(ldc));

		if (!CheckPointerLong(C, na * nb, ldc, out var pC))
			return false;
		NativeMethods.matKron(T.Type, pA, lda, ma, na, pB, ldb, mb, nb, pC, ldc, &α, &β);
		return true;
	}

	#endregion
}