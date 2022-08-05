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
			_ = NativeMethods.vecConj(T.Type, ptr, n, inc);
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
			_ = NativeMethods.vecConj(T.Type, ptr, n, inc);
	}
}

internal static class Conjugater
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe static Conjugater1<T> Create<T>(T* ptr, int n, int inc, MatrixOperation op) where T : unmanaged, IBaseNumber<T> => new(ptr, n, inc, op);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe static bool Conjugate<T>(T* ptr, int n, int inc) where T : unmanaged, IBaseNumber<T>
	{
		return NativeMethods.vecConj(T.Type, ptr, n, inc) == 0;
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


	#region BLAS like level 2
	protected override unsafe bool DiagonalMatrixMultiplyGeneral_<T>(bool leftA, long m, long n, T α, Storage<T> A, long lda, Storage<T> x, int strideX, T β, Storage<T> C, long ldc)
	{
		if (!T.Type.CheckBaseSupport())
			return false;
		if (!GetPointer(x, out var px, out var nx, strideX))
			return false;
		if (!GetPointer(A, m, n, lda, out var pA, out int mm, out int nn, out int llda))
			return false;
		if (!GetPointer(C, m, n, ldc, out var pC, out _, out _, out int lldc))
			return false;
		int lenX = leftA ? nn : mm;
		////if (nx < lenX)
		////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(x));

		delegate*<IntPtr, CuBlasSideMode, int, int, IntPtr, int, IntPtr, int, IntPtr, int, CudaBlasStatus> func;
		func = default(T) switch
		{
			Float32 => &NativeMethods.cublasSdgmm,
			Float64 => &NativeMethods.cublasDdgmm,
			Complex<Float32> => &NativeMethods.cublasCdgmm,
			Complex<Float64> => &NativeMethods.cublasZdgmm,
			_ => null,
		};
		IntPtr cacheC = default;
		if (!β.IsZero())
			Storage.NativeMethods.cudaMalloc(out cacheC, sizeof(T) * m * n).Check();
		var oldC = new TempGpuStorage<T>(cacheC, m * n);
		try
		{
			// cache C
			if (!β.IsZero())
			{
				if (!this.GeneralMatricesAdd_(MatrixOperation.None, MatrixOperation.None, m, n, Const<T>.One, C, ldc, default, default, default, oldC, m))
					return false;
			}
			// overwrite C by diagonal multiply result
			func(this.cublasHandle, leftA ? CuBlasSideMode.Right : CuBlasSideMode.Left, mm, nn, pA, llda, px, strideX, pC, lldc).Check();
			// C = α * C + β * oldC
			if (!β.IsZero())
				return this.GeneralMatricesAdd_(MatrixOperation.None, MatrixOperation.None, m, n, α, C, ldc, β, oldC, m, C, ldc);
			else if (!α.IsOne())
				return this.GeneralMatricesAdd_(MatrixOperation.None, MatrixOperation.None, m, n, α, C, ldc, default, default, default, C, ldc);
			else
				return true;
		}
		finally
		{
			if (cacheC != default)
				Storage.NativeMethods.cudaFree(cacheC);
		}
	}
	#endregion


	#region BLAS level 3
	protected override unsafe bool TriangularMatrixSolve_<T>(bool leftA, bool fillUpper, bool unitDiag, MatrixOperation op, long m, long n, T α, Storage<T> A, long lda, Storage<T> B, long ldb)
	{
		if (!T.Type.CheckBaseSupport())
			return false;
		if (!GetPointer(A, m, m, lda, out var pA, out int mm, out _, out int llda))
			return false;
		if (!GetPointer(B, m, n, ldb, out var pB, out _, out int nn, out int lldb))
			return false;
		if (α.IsZero()) // result is 0
			return this.GeneralMatricesAdd_(MatrixOperation.None, MatrixOperation.None, m, n, α, B, ldb, default, default, default, B, ldb);

		delegate*<IntPtr, CuBlasSideMode, CuBlasFillMode, CuBlasOperation, CuBlasDiagType, int, int, T*, IntPtr, int, IntPtr, int, CudaBlasStatus> func;
		if (this.Cuda110OrAbove)
		{
			func = default(T) switch
			{
				Float32 => &NativeMethods.cublasStrsm,
				Float64 => &NativeMethods.cublasDtrsm,
				Complex<Float32> => &NativeMethods.cublasCtrsm,
				Complex<Float64> => &NativeMethods.cublasZtrsm,
				_ => null,
			};
		}
		else
		{
			func = default(T) switch
			{
				Float32 => &NativeMethods.cublasStrsm_v2,
				Float64 => &NativeMethods.cublasDtrsm_v2,
				Complex<Float32> => &NativeMethods.cublasCtrsm_v2,
				Complex<Float64> => &NativeMethods.cublasZtrsm_v2,
				_ => null,
			};
		}
		var opCuda = op.ToCuda();
		if (opCuda == CuBlasOperation.ConjugateAlone)
			return false;
		func(this.cublasHandle, leftA ? CuBlasSideMode.Right : CuBlasSideMode.Left, fillUpper ? CuBlasFillMode.Upper : CuBlasFillMode.Lower, opCuda, unitDiag ? CuBlasDiagType.Unit : CuBlasDiagType.NonUnit, mm, nn, &α, pA, llda, pB, lldb).Check();
		return true;
	}

	protected override unsafe bool TriangularMatrixMultiply_<T>(bool leftA, bool fillUpper, bool unitDiag, MatrixOperation op, long m, long n, T α, Storage<T> A, long lda, Storage<T> B, long ldb, Storage<T> C, long ldc)
	{
		if (!T.Type.CheckBaseSupport())
			return false;
		var opCuda = op.ToCuda();
		if (opCuda == CuBlasOperation.ConjugateAlone)
			return false;
		if (!GetPointer(B, m, n, ldb, out var pB, out int mm, out int nn, out int lldb))
			return false;
		if (!GetPointer(C, m, n, ldc, out var pC, out _, out _, out int lldc))
			return false;
		if (!GetPointer(A, leftA ? m : n, leftA ? m : n, lda, out var pA, out _, out _, out int llda))
			return false;
		if (α.IsZero()) // result is 0
			return this.GeneralMatricesAdd_(MatrixOperation.None, MatrixOperation.None, m, n, α, C, ldc, default, default, default, C, ldc);

		delegate*<IntPtr, CuBlasSideMode, CuBlasFillMode, CuBlasOperation, CuBlasDiagType, int, int, T*, IntPtr, int, IntPtr, int, IntPtr, int, CudaBlasStatus> func;
		if (this.Cuda110OrAbove)
		{
			func = default(T) switch
			{
				Float32 => &NativeMethods.cublasStrmm,
				Float64 => &NativeMethods.cublasDtrmm,
				Complex<Float32> => &NativeMethods.cublasCtrmm,
				Complex<Float64> => &NativeMethods.cublasZtrmm,
				_ => null,
			};
		}
		else
		{
			func = default(T) switch
			{
				Float32 => &NativeMethods.cublasStrmm_v2,
				Float64 => &NativeMethods.cublasDtrmm_v2,
				Complex<Float32> => &NativeMethods.cublasCtrmm_v2,
				Complex<Float64> => &NativeMethods.cublasZtrmm_v2,
				_ => null,
			};
		}
		func(this.cublasHandle, leftA ? CuBlasSideMode.Right : CuBlasSideMode.Left, fillUpper ? CuBlasFillMode.Upper : CuBlasFillMode.Lower, opCuda, unitDiag ? CuBlasDiagType.Unit : CuBlasDiagType.NonUnit, mm, nn, &α, pA, llda, pB, lldb, pC, lldc).Check();
		return true;
	}

	protected override unsafe bool GeneralMatricesAdd_<T>(MatrixOperation opA, MatrixOperation opB, long m, long n, T α, Storage<T>? A, long lda, T β, Storage<T>? B, long ldb, Storage<T> C, long ldc)
	{
		if (!T.Type.CheckBaseSupport())
			return false;
		if (!GetPointer(A, opA, m, n, lda, out var opcA, out var pA, out _, out _, out int llda))
			return false;
		if (!GetPointer(B, opB, m, n, ldb, out var opcB, out var pB, out _, out _, out int lldb))
			return false;
		if (!GetPointer(C, m, n, ldc, out var pC, out int mm, out int nn, out int lldc))
			return false;
		// shortcut
		if ((A is null || α.IsZero()) != (B is null || β.IsZero()))
		{
			if ((A is null || α.IsZero()) && opB == MatrixOperation.None && β.IsOne())
			{   // copy B to C
				Storage.NativeMethods.cudaMemcpy2D(pC, ldc * sizeof(T), pB, ldb * sizeof(T), m * sizeof(T), n, Storage.MemoryCopyKind.DeviceToDevice).Check();
			}
			else if ((B is null || β.IsZero()) && opA == MatrixOperation.None && α.IsOne())
			{   // copy A to C
				Storage.NativeMethods.cudaMemcpy2D(pC, ldc * sizeof(T), pA, lda * sizeof(T), m * sizeof(T), n, Storage.MemoryCopyKind.DeviceToDevice).Check();
			}
			return true;
		}

		delegate*<IntPtr, CuBlasOperation, CuBlasOperation, int, int, T*, IntPtr, int, T*, IntPtr, int, IntPtr, int, CudaBlasStatus> func;
		func = default(T) switch
		{
			Float32 => &NativeMethods.cublasSgeam,
			Float64 => &NativeMethods.cublasDgeam,
			Complex<Float32> => &NativeMethods.cublasCgeam,
			Complex<Float64> => &NativeMethods.cublasZgeam,
			_ => null,
		};
		func(this.cublasHandle, opcA, opcB, mm, nn, &α, pA, llda, &β, pB, lldb, pC, lldc).Check();
		return true;
	}

	protected override unsafe bool GeneralMatricesMultiply_<T>(MatrixOperation opA, MatrixOperation opB, long m, long n, long k, T α, Storage<T> A, long lda, Storage<T> B, long ldb, T β, Storage<T> C, long ldc)
	{
		if (!T.Type.CheckEx2Support())
			return false;
		if (!GetPointer(A, opA, m, k, lda, out var opcA, out var pA, out _, out int kk, out int llda))
			return false;
		if (!GetPointer(B, opB, k, n, ldb, out var opcB, out var pB, out _, out _, out int lldb))
			return false;
		if (!GetPointer(C, m, n, ldc, out var pC, out int mm, out int nn, out int lldc))
			return false;

		delegate*<IntPtr, CuBlasOperation, CuBlasOperation, int, int, int, T*, IntPtr, int, IntPtr, int, T*, IntPtr, int, CudaBlasStatus> func;
		if (this.Cuda110OrAbove)
		{
			func = default(T) switch
			{
				Float32 => &NativeMethods.cublasSgemm,
				Float64 => &NativeMethods.cublasDgemm,
				Complex<Float32> => this.ComplexGemmUseGemm3m ? &NativeMethods.cublasCgemm3m : &NativeMethods.cublasCgemm,
				Complex<Float64> => this.ComplexGemmUseGemm3m ? &NativeMethods.cublasZgemm3m : &NativeMethods.cublasZgemm,
				_ => null,
			};
		}
		else
		{
			func = default(T) switch
			{
				Float32 => &NativeMethods.cublasSgemm_v2,
				Float64 => &NativeMethods.cublasDgemm_v2,
				Complex<Float32> => this.ComplexGemmUseGemm3m ? &NativeMethods.cublasCgemm3m : &NativeMethods.cublasCgemm_v2,
				Complex<Float64> => this.ComplexGemmUseGemm3m ? &NativeMethods.cublasZgemm3m : &NativeMethods.cublasZgemm_v2,
				_ => null,
			};
		}
		if (func is not null)
		{
			func(this.cublasHandle, opcA, opcB, mm, nn, kk, &α, pA, llda, pB, lldb, &β, pC, lldc).Check();
		}
		else
		{
			if (T.Type == DataType.ComplexFloat16 || T.Type == BrainFloatConst.ComplexBrainFloat16)
				return false;
			var type = T.Type.ToCudaDataType();
			CuBlasComputeType cType = type switch
			{
				CudaDataType.RealFloat32 or CudaDataType.ComplexFloat32 => CuBlasComputeType.Compute32F,
				CudaDataType.RealFloat64 or CudaDataType.ComplexFloat64 => CuBlasComputeType.Compute64F,
				CudaDataType.RealFloat16 => CuBlasComputeType.Compute16F,
				CudaDataType.RealBrainFloat16 => CuBlasComputeType.Compute32F,
				_ => default,
			};
			NativeMethods.cublasGemmEx(this.cublasHandle, opcA, opcB, mm, nn, kk, &α, pA, type, llda, pB, type, lldb, &β, pC, type, lldc, cType, CuBlasGemmAlgorithm.Default).Check();
		}
		return true;
	}

	protected override unsafe bool SymmHermMatrixMultiplyGeneral_<T>(bool fillUpper, bool leftA, bool hermA, long m, long n, T α, Storage<T> A, long lda, Storage<T> B, long ldb, T β, Storage<T> C, long ldc)
	{
		if (!T.Type.CheckBaseSupport())
			return false;
		if (!GetPointer(A, leftA ? m : n, leftA ? m : n, lda, out var pA, out _, out _, out int llda))
			return false;
		if (!GetPointer(B, m, n, ldb, out var pB, out _, out _, out int lldb))
			return false;
		if (!GetPointer(C, m, n, ldc, out var pC, out int mm, out int nn, out int lldc))
			return false;

		delegate*<IntPtr, CuBlasSideMode, CuBlasFillMode, int, int, T*, IntPtr, int, IntPtr, int, T*, IntPtr, int, CudaBlasStatus> func;
		if (this.Cuda110OrAbove)
		{
			func = default(T) switch
			{
				Float32 => &NativeMethods.cublasSsymm,
				Float64 => &NativeMethods.cublasDsymm,
				Complex<Float32> => hermA ? &NativeMethods.cublasChemm : &NativeMethods.cublasCsymm,
				Complex<Float64> => hermA ? &NativeMethods.cublasZhemm : &NativeMethods.cublasZsymm,
				_ => null,
			};
		}
		else
		{
			func = default(T) switch
			{
				Float32 => &NativeMethods.cublasSsymm_v2,
				Float64 => &NativeMethods.cublasDsymm_v2,
				Complex<Float32> => hermA ? &NativeMethods.cublasChemm_v2 : &NativeMethods.cublasCsymm_v2,
				Complex<Float64> => hermA ? &NativeMethods.cublasZhemm_v2 : &NativeMethods.cublasZsymm_v2,
				_ => null,
			};
		}
		func(this.cublasHandle, leftA ? CuBlasSideMode.Left : CuBlasSideMode.Right, fillUpper ? CuBlasFillMode.Upper : CuBlasFillMode.Lower, mm, nn, &α, pA, llda, pB, lldb, &β, pC, lldc).Check();
		return true;
	}

	protected override unsafe bool RankKUpdate_<T>(bool fillUpper, MatrixOperation op, bool conjA, long n, long k, T α, Storage<T> A, long lda, T β, Storage<T> C, long ldc)
	{
		if (!T.Type.CheckBaseSupport())
			return false;
		if (!GetPointer(A, op, n, k, lda, out var opcA, out var pA, out int nn, out int kk, out int llda))
			return false;
		if (!GetPointer(C, n, n, ldc, out var pC, out _, out _, out int lldc))
			return false;

		delegate*<IntPtr, CuBlasFillMode, CuBlasOperation, int, int, T*, IntPtr, int, T*, IntPtr, int, CudaBlasStatus> func;
		if (this.Cuda110OrAbove)
		{
			func = default(T) switch
			{
				Float32 => &NativeMethods.cublasSsyrk,
				Float64 => &NativeMethods.cublasDsyrk,
				Complex<Float32> => conjA ? &NativeMethods.cublasCherk : &NativeMethods.cublasCsyrk,
				Complex<Float64> => conjA ? &NativeMethods.cublasZherk : &NativeMethods.cublasZsyrk,
				_ => null,
			};
		}
		else
		{
			func = default(T) switch
			{
				Float32 => &NativeMethods.cublasSsyrk_v2,
				Float64 => &NativeMethods.cublasDsyrk_v2,
				Complex<Float32> => conjA ? &NativeMethods.cublasCherk_v2 : &NativeMethods.cublasCsyrk_v2,
				Complex<Float64> => conjA ? &NativeMethods.cublasZherk_v2 : &NativeMethods.cublasZsyrk_v2,
				_ => null,
			};
		}
		func(this.cublasHandle, fillUpper ? CuBlasFillMode.Upper : CuBlasFillMode.Lower, opcA, nn, kk, &α, pA, llda, &β, pC, lldc).Check();
		return true;
	}

	protected override unsafe bool RankTwoKUpdate_<T>(bool fillUpper, MatrixOperation op, bool conjugate, long n, long k, T α, Storage<T> A, long lda, Storage<T> B, long ldb, T β, Storage<T> C, long ldc)
	{
		if (!T.Type.CheckBaseSupport())
			return false;
		if (!GetPointer(A, op, n, k, lda, out var opCuda, out var pA, out int nn, out int kk, out int llda))
			return false;
		if (!GetPointer(B, op, n, k, lda, out _, out var pB, out _, out _, out int lldb))
			return false;
		if (!GetPointer(C, n, n, ldc, out var pC, out _, out _, out int lldc))
			return false;

		delegate*<IntPtr, CuBlasFillMode, CuBlasOperation, int, int, T*, IntPtr, int, IntPtr, int, T*, IntPtr, int, CudaBlasStatus> func;
		if (this.Cuda110OrAbove)
		{
			func = default(T) switch
			{
				Float32 => &NativeMethods.cublasSsyr2k,
				Float64 => &NativeMethods.cublasDsyr2k,
				Complex<Float32> => conjugate ? &NativeMethods.cublasCher2k : &NativeMethods.cublasCsyr2k,
				Complex<Float64> => conjugate ? &NativeMethods.cublasZher2k : &NativeMethods.cublasZsyr2k,
				_ => null,
			};
		}
		else
		{
			func = default(T) switch
			{
				Float32 => &NativeMethods.cublasSsyr2k_v2,
				Float64 => &NativeMethods.cublasDsyr2k_v2,
				Complex<Float32> => conjugate ? &NativeMethods.cublasCher2k_v2 : &NativeMethods.cublasCsyr2k_v2,
				Complex<Float64> => conjugate ? &NativeMethods.cublasZher2k_v2 : &NativeMethods.cublasZsyr2k_v2,
				_ => null,
			};
		}
		func(this.cublasHandle, fillUpper ? CuBlasFillMode.Upper : CuBlasFillMode.Lower, opCuda, nn, kk, &α, pA, llda, pB, lldb, &β, pC, lldc).Check();
		return true;
	}

	protected override unsafe bool RankKUpdateVariant_<T>(bool fillUpper, MatrixOperation op, bool conjB, long n, long k, T α, Storage<T> A, long lda, Storage<T> B, long ldb, T β, Storage<T> C, long ldc)
	{
		if (!T.Type.CheckBaseSupport())
			return false;
		if (!GetPointer(A, op, n, k, lda, out var opCuda, out var pA, out int nn, out int kk, out int llda))
			return false;
		if (!GetPointer(B, op, n, k, lda, out _, out var pB, out _, out _, out int lldb))
			return false;
		if (!GetPointer(C, n, n, ldc, out var pC, out _, out _, out int lldc))
			return false;

		delegate*<IntPtr, CuBlasFillMode, CuBlasOperation, int, int, T*, IntPtr, int, IntPtr, int, T*, IntPtr, int, CudaBlasStatus> func;
		if (this.Cuda110OrAbove)
		{
			func = default(T) switch
			{
				Float32 => &NativeMethods.cublasSsyrkx,
				Float64 => &NativeMethods.cublasDsyrkx,
				Complex<Float32> => conjB ? &NativeMethods.cublasCherkx : &NativeMethods.cublasCsyrkx,
				Complex<Float64> => conjB ? &NativeMethods.cublasZherkx : &NativeMethods.cublasZsyrkx,
				_ => null,
			};
		}
		else
		{
			func = default(T) switch
			{
				Float32 => &NativeMethods.cublasSsyrkx_v2,
				Float64 => &NativeMethods.cublasDsyrkx_v2,
				Complex<Float32> => conjB ? &NativeMethods.cublasCherkx_v2 : &NativeMethods.cublasCsyrkx_v2,
				Complex<Float64> => conjB ? &NativeMethods.cublasZherkx_v2 : &NativeMethods.cublasZsyrkx_v2,
				_ => null,
			};
		}
		func(this.cublasHandle, fillUpper ? CuBlasFillMode.Upper : CuBlasFillMode.Lower, opCuda, nn, kk, &α, pA, llda, pB, lldb, &β, pC, lldc).Check();
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


	#region solve
	#region linear solve
	protected override unsafe bool LinearSolve_<T, TInd>(MatrixOperation op, long n, long nrhs, Storage<T> A, long lda, Storage<T> B, long ldb, Storage<TInd>? pivot = null)
	{
		if (!T.Type.CheckBaseSupport())
			return false;
		if ((typeof(TInd) == typeof(long) || typeof(TInd) == typeof(ulong)) && !this.Cuda111OrAbove)
			return false;
		if (typeof(TInd) != typeof(long) && typeof(TInd) == typeof(ulong) && typeof(TInd) != typeof(int) && typeof(TInd) != typeof(uint))
			return false;
		var opCuda = op.ToCuda();
		if (opCuda == CuBlasOperation.ConjugateAlone)
			return false;

		if ((typeof(TInd) == typeof(long) || typeof(TInd) == typeof(ulong)) && this.Cuda111OrAbove)
		{
			if (!CheckPointerLong(A, n, lda, out var pA))
				return false;
			if (!CheckPointerLong(B, nrhs, ldb, out var pB))
				return false;
			IntPtr pP = default;
			if (pivot is not null && !CheckPointerLong(pivot, out pP, out var np))
				return false;
			////if (np < n)
			////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(pivot));

			var type = T.Type.ToCudaDataType();
			// factorize
			NativeMethods.cusolverDnXgetrf_bufferSize(this.cusolverHandle, IntPtr.Zero, n, n, type, pA, lda, type, out var workDevice, out var workHost).Check();
			using var buffer = pP == default ? CudaBuffer.Create(workDevice + n * sizeof(TInd), workHost) : CudaBuffer.Create(workDevice, workHost);
			if (pP == default)
				pP = (IntPtr)(buffer.DeviceBuffer.ToInt64() + workDevice);
			NativeMethods.cusolverDnXgetrf(this.cusolverHandle, IntPtr.Zero, n, n, type, pA, lda, pP, type, buffer.DeviceBuffer, workDevice, buffer.HostBuffer, workHost, buffer.ExtraDeviceInfo).Check();
			SolveMethodKind.LU.CheckDeviceInfo(buffer.ExtraDeviceInfo);
			// solve
			NativeMethods.cusolverDnXgetrs(this.cusolverHandle, IntPtr.Zero, opCuda, n, nrhs, type, pA, lda, pP, type, pB, ldb, buffer.ExtraDeviceInfo).Check();
			SolveMethodKind.LU.CheckDeviceInfo(buffer.ExtraDeviceInfo);
		}
		else
		{   // use legacy
			if (!GetPointer(A, n, n, lda, out var pA, out int nn, out _, out int llda))
				return false;
			if (!GetPointer(B, n, nrhs, ldb, out var pB, out _, out int nnrhs, out int lldb))
				return false;
			IntPtr pP = default;
			if (pivot is not null && !GetPointer(pivot, out pP, out int np))
				return false;
			////if (np < nn)
			////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(pivot));

			delegate*<IntPtr, int, int, IntPtr, int, out int, CudaSolverStatus> bufFunc;
			delegate*<IntPtr, int, int, IntPtr, int, IntPtr, IntPtr, IntPtr, CudaSolverStatus> calFunc;
			delegate*<IntPtr, CuBlasOperation, int, int, IntPtr, int, IntPtr, IntPtr, int, IntPtr, CudaSolverStatus> solveFunc;
			switch (T.Type)
			{
				case DataType.RealFloat32:
					bufFunc = &NativeMethods.cusolverDnSgetrf_bufferSize;
					calFunc = &NativeMethods.cusolverDnSgetrf;
					solveFunc = &NativeMethods.cusolverDnSgetrs;
					break;
				case DataType.RealFloat64:
					bufFunc = &NativeMethods.cusolverDnDgetrf_bufferSize;
					calFunc = &NativeMethods.cusolverDnDgetrf;
					solveFunc = &NativeMethods.cusolverDnDgetrs;
					break;
				case DataType.ComplexFloat32:
					bufFunc = &NativeMethods.cusolverDnCgetrf_bufferSize;
					calFunc = &NativeMethods.cusolverDnCgetrf;
					solveFunc = &NativeMethods.cusolverDnCgetrs;
					break;
				case DataType.ComplexFloat64:
					bufFunc = &NativeMethods.cusolverDnZgetrf_bufferSize;
					calFunc = &NativeMethods.cusolverDnZgetrf;
					solveFunc = &NativeMethods.cusolverDnZgetrs;
					break;
				default:
					bufFunc = null;
					calFunc = null;
					solveFunc = null;
					break;
			}
			// factorize
			bufFunc(this.cusolverHandle, nn, nn, pA, llda, out var work).Check();
			using var buffer = pP == default ? CudaBuffer.Create(work * sizeof(T) + n * sizeof(TInd)) : CudaBuffer.Create<T>(work);
			if (pP == default)
				pP = (IntPtr)(buffer.DeviceBuffer.ToInt64() + work * sizeof(T));
			calFunc(this.cusolverHandle, nn, nn, pA, llda, buffer.DeviceBuffer, pP, buffer.ExtraDeviceInfo).Check();
			// solve
			solveFunc(this.cusolverHandle, opCuda, nn, nnrhs, pA, llda, pP, pB, lldb, buffer.ExtraDeviceInfo).Check();
			SolveMethodKind.LU.CheckDeviceInfo(buffer.ExtraDeviceInfo);
		}
		return true;
	}
	#endregion

	#region QR
	protected override unsafe bool LeastSquareSolve_<T>(long m, long n, long nrhs, Storage<T> A, long lda, Storage<T> B, long ldb, Storage<T>? work = null)
	{
		if (!T.Type.CheckBaseSupport())
			return false;
		if (!GetPointer(A, m, n, lda, out var pA, out int mm, out int nn, out int llda))
			return false;
		if (!GetPointer(B, m, nrhs, ldb, out var pB, out _, out int nnrhs, out int lldb))
			return false;
		if (!GetPointer(work, out var pW, out int nw))
			return false;
		////if (nw > 0 && nw < nn)
		////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(work));

		IntPtr tau;
		if (pW == default)
			Storage.NativeMethods.cudaMalloc(out tau, n * sizeof(T)).Check();
		else
			tau = pW;
		try
		{
			delegate*<IntPtr, int, int, IntPtr, int, out int, CudaSolverStatus> bufQRFunc = null;
			delegate*<IntPtr, int, int, IntPtr, int, IntPtr, IntPtr, int, IntPtr, CudaSolverStatus> calQRFunc = null;
			delegate*<IntPtr, CuBlasSideMode, CuBlasOperation, int, int, int, IntPtr, int, IntPtr, IntPtr, int, out int, CudaSolverStatus> bufQmulFunc = null;
			delegate*<IntPtr, CuBlasSideMode, CuBlasOperation, int, int, int, IntPtr, int, IntPtr, IntPtr, int, IntPtr, int, IntPtr, CudaSolverStatus> calQmulFunc = null;
			delegate*<IntPtr, CuBlasSideMode, CuBlasFillMode, CuBlasOperation, CuBlasDiagType, int, int, T*, IntPtr, int, IntPtr, int, CudaBlasStatus> triSolveFunc = null;
			CuBlasOperation op = CuBlasOperation.Transpose;
			switch (T.Type)
			{
				case DataType.RealFloat32:
					bufQRFunc = &NativeMethods.cusolverDnSgeqrf_bufferSize;
					bufQmulFunc = &NativeMethods.cusolverDnSormqr_bufferSize;
					calQRFunc = &NativeMethods.cusolverDnSgeqrf;
					calQmulFunc = &NativeMethods.cusolverDnSormqr;
					triSolveFunc = this.Cuda110OrAbove ? &NativeMethods.cublasStrsm : &NativeMethods.cublasStrsm_v2;
					break;
				case DataType.RealFloat64:
					bufQRFunc = &NativeMethods.cusolverDnDgeqrf_bufferSize;
					bufQmulFunc = &NativeMethods.cusolverDnDormqr_bufferSize;
					calQRFunc = &NativeMethods.cusolverDnDgeqrf;
					calQmulFunc = &NativeMethods.cusolverDnDormqr;
					triSolveFunc = this.Cuda110OrAbove ? &NativeMethods.cublasDtrsm : &NativeMethods.cublasDtrsm_v2;
					break;
				case DataType.ComplexFloat32:
					bufQRFunc = &NativeMethods.cusolverDnCgeqrf_bufferSize;
					bufQmulFunc = &NativeMethods.cusolverDnCunmqr_bufferSize;
					calQRFunc = &NativeMethods.cusolverDnCgeqrf;
					calQmulFunc = &NativeMethods.cusolverDnCunmqr;
					triSolveFunc = this.Cuda110OrAbove ? &NativeMethods.cublasCtrsm : &NativeMethods.cublasCtrsm_v2;
					op = CuBlasOperation.ConjugateTranspose;
					break;
				case DataType.ComplexFloat64:
					bufQRFunc = &NativeMethods.cusolverDnZgeqrf_bufferSize;
					bufQmulFunc = &NativeMethods.cusolverDnZunmqr_bufferSize;
					calQRFunc = &NativeMethods.cusolverDnZgeqrf;
					calQmulFunc = &NativeMethods.cusolverDnZunmqr;
					triSolveFunc = this.Cuda110OrAbove ? &NativeMethods.cublasZtrsm : &NativeMethods.cublasZtrsm_v2;
					op = CuBlasOperation.ConjugateTranspose;
					break;
				default:
					break;
			}
			// get buffer
			bufQRFunc(this.cusolverHandle, nn, nn, pA, llda, out var workSizeT1).Check();
			bufQmulFunc(this.cusolverHandle, CuBlasSideMode.Left, CuBlasOperation.None, nn, nnrhs, nn, pA, llda, tau, pB, lldb, out var workSizeT2).Check();
			using var buffer = CudaBuffer.Create<T>(Math.Max(workSizeT1, workSizeT2));
			// implicit QR
			calQRFunc(this.cusolverHandle, mm, nn, pA, llda, tau, buffer.DeviceBuffer, workSizeT1, buffer.ExtraDeviceInfo).Check();
			SolveMethodKind.QR.CheckDeviceInfo(buffer.ExtraDeviceInfo);
			// implicit Q^H * B
			calQmulFunc(this.cusolverHandle, CuBlasSideMode.Left, op, mm, nnrhs, nn, pA, llda, tau, pB, lldb, buffer.DeviceBuffer, workSizeT2, buffer.ExtraDeviceInfo).Check();
			SolveMethodKind.QR.CheckDeviceInfo(buffer.ExtraDeviceInfo);
			// triangular solve R * X = Q^H * B
			T one = Const<T>.One;
			triSolveFunc(this.cublasHandle, CuBlasSideMode.Left, CuBlasFillMode.Upper, CuBlasOperation.None, CuBlasDiagType.NonUnit, nn, nnrhs, &one, pA, llda, pB, lldb).Check();
			return true;
		}
		finally
		{
			if (tau != pW)
				Storage.NativeMethods.cudaFree(tau);
		}
	}

	protected override unsafe bool QRDecomposition_<T>(bool full, long m, long n, Storage<T> A, long lda, Storage<T> Q, long ldq, Storage<T>? work = null)
	{
		if (!T.Type.CheckBaseSupport())
			return false;
		if (!GetPointer(A, m, n, lda, out var pA, out int mm, out int nn, out int llda))
			return false;
		int kk = Math.Min(mm, nn); long colsQ = full ? m : kk;
		if (!GetPointer(Q, m, colsQ, ldq, out var pQ, out _, out int nnQ, out int lldq))
			return false;
		if (!GetPointer(work, out var pW, out int nw))
			return false;

		IntPtr tau;
		if (pW == default)
			Storage.NativeMethods.cudaMalloc(out tau, n * sizeof(T)).Check();
		else
			tau = pW;
		try
		{
			delegate*<IntPtr, int, int, IntPtr, int, out int, CudaSolverStatus> bufQRFunc = null;
			delegate*<IntPtr, int, int, IntPtr, int, IntPtr, IntPtr, int, IntPtr, CudaSolverStatus> calQRFunc = null;
			delegate*<IntPtr, int, int, int, IntPtr, int, IntPtr, out int, CudaSolverStatus> bufGetQFunc = null;
			delegate*<IntPtr, int, int, int, IntPtr, int, IntPtr, IntPtr, int, IntPtr, CudaSolverStatus> calGetQFunc = null;
			switch (T.Type)
			{
				case DataType.RealFloat32:
					bufQRFunc = &NativeMethods.cusolverDnSgeqrf_bufferSize;
					bufGetQFunc = &NativeMethods.cusolverDnSorgqr_bufferSize;
					calQRFunc = &NativeMethods.cusolverDnSgeqrf;
					calGetQFunc = &NativeMethods.cusolverDnSorgqr;
					break;
				case DataType.RealFloat64:
					bufQRFunc = &NativeMethods.cusolverDnDgeqrf_bufferSize;
					bufGetQFunc = &NativeMethods.cusolverDnDorgqr_bufferSize;
					calQRFunc = &NativeMethods.cusolverDnDgeqrf;
					calGetQFunc = &NativeMethods.cusolverDnDorgqr;
					break;
				case DataType.ComplexFloat32:
					bufQRFunc = &NativeMethods.cusolverDnCgeqrf_bufferSize;
					bufGetQFunc = &NativeMethods.cusolverDnCungqr_bufferSize;
					calQRFunc = &NativeMethods.cusolverDnCgeqrf;
					calGetQFunc = &NativeMethods.cusolverDnCungqr;
					break;
				case DataType.ComplexFloat64:
					bufQRFunc = &NativeMethods.cusolverDnZgeqrf_bufferSize;
					bufGetQFunc = &NativeMethods.cusolverDnZungqr_bufferSize;
					calQRFunc = &NativeMethods.cusolverDnZgeqrf;
					calGetQFunc = &NativeMethods.cusolverDnZungqr;
					break;
				default:
					break;
			}
			// get buffer
			bufQRFunc(this.cusolverHandle, nn, nn, pA, llda, out var workSizeT1).Check();
			bufGetQFunc(this.cusolverHandle, mm, nnQ, kk, pQ, lldq, tau, out var workSizeT2).Check();
			using var buffer = CudaBuffer.Create<T>(Math.Max(workSizeT1, workSizeT2));
			// implicit QR
			calQRFunc(this.cusolverHandle, mm, nn, pA, llda, tau, buffer.DeviceBuffer, workSizeT1, buffer.ExtraDeviceInfo).Check();
			SolveMethodKind.QR.CheckDeviceInfo(buffer.ExtraDeviceInfo);
			// copy A to Q
			Storage.NativeMethods.cudaMemcpy2D(pQ, ldq, pA, lda, m, Math.Min(colsQ, n), Storage.MemoryCopyKind.DeviceToDevice);
			// form Q
			calGetQFunc(this.cusolverHandle, mm, nnQ, kk, pQ, lldq, tau, buffer.DeviceBuffer, workSizeT2, buffer.ExtraDeviceInfo).Check();
			SolveMethodKind.QR.CheckDeviceInfo(buffer.ExtraDeviceInfo);
			return true;
		}
		finally
		{
			if (tau != pW)
				Storage.NativeMethods.cudaFree(tau);
		}
	}
	#endregion

	#region eigen
	protected override unsafe bool EigenSpecialMatrixHermitian_<T, TReal>(SolveVectorMode mode, long n, Storage<TReal> valOut, Storage<T> A, long lda)
	{
		if (!T.Type.CheckBaseSupport())
			return false;
		if (mode != SolveVectorMode.NoVector)
			mode = SolveVectorMode.Vector;

		if (this.Cuda111OrAbove)
		{
			if (!CheckPointerLong(A, n, lda, out var pA))
				return false;
			if (!CheckPointerLong(valOut, out var pV, out long nv))
				return false;
			////if (nv < n)
			////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(valOut));

			var type = T.Type.ToCudaDataType();
			NativeMethods.cusolverDnXsyevd_bufferSize(this.cusolverHandle, IntPtr.Zero, mode, CuBlasFillMode.Upper, n, type, pA, lda, type, pV, type, out var workDevice, out var workHost).Check();
			using var buffer = CudaBuffer.Create(workDevice, workHost);
			NativeMethods.cusolverDnXsyevd(this.cusolverHandle, IntPtr.Zero, mode, CuBlasFillMode.Upper, n, type, pA, lda, type, pV, type, buffer.DeviceBuffer, workDevice, buffer.HostBuffer, workHost, buffer.ExtraDeviceInfo).Check();
			SolveMethodKind.Eigenvalue.CheckDeviceInfo(buffer.ExtraDeviceInfo);
		}
		else
		{   // CUDA version <= 11.0
			if (!GetPointer(A, n, n, lda, out var pA, out int nn, out _, out int llda))
				return false;
			if (!GetPointer(valOut, out var pV, out int nv))
				return false;
			////if (nv < nn)
			////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(valOut));

			delegate*<IntPtr, SolveVectorMode, CuBlasFillMode, int, IntPtr, int, IntPtr, out int, CudaSolverStatus> bufFunc;
			delegate*<IntPtr, SolveVectorMode, CuBlasFillMode, int, IntPtr, int, IntPtr, IntPtr, int, IntPtr, CudaSolverStatus> calFunc;
			bufFunc = default(T) switch
			{
				Float32 => &NativeMethods.cusolverDnSsyevd_bufferSize,
				Float64 => &NativeMethods.cusolverDnDsyevd_bufferSize,
				Complex<Float32> => &NativeMethods.cusolverDnCheevd_bufferSize,
				Complex<Float64> => &NativeMethods.cusolverDnZheevd_bufferSize,
				_ => null,
			};
			calFunc = default(T) switch
			{
				Float32 => &NativeMethods.cusolverDnSsyevd,
				Float64 => &NativeMethods.cusolverDnDsyevd,
				Complex<Float32> => &NativeMethods.cusolverDnCheevd,
				Complex<Float64> => &NativeMethods.cusolverDnZheevd,
				_ => null,
			};
			bufFunc(this.cusolverHandle, mode, CuBlasFillMode.Upper, nn, pA, llda, pV, out var work).Check();
			using var buffer = CudaBuffer.Create<T>(work);
			calFunc(this.cusolverHandle, mode, CuBlasFillMode.Upper, nn, pA, llda, pV, buffer.DeviceBuffer, work, buffer.ExtraDeviceInfo).Check();
			SolveMethodKind.Eigenvalue.CheckDeviceInfo(buffer.ExtraDeviceInfo);
		}
		return true;
	}

	protected override unsafe bool EigenGeneralMatrixHermitian_<T, TReal>(GeneralEigenType eigType, SolveVectorMode mode, long n, Storage<TReal> valOut, Storage<T> A, long lda, Storage<T> B, long ldb)
	{
		if (!T.Type.CheckBaseSupport())
			return false;
		if (mode != SolveVectorMode.NoVector)
			mode = SolveVectorMode.Vector;
		if (!GetPointer(A, n, n, lda, out var pA, out int nn, out _, out int llda))
			return false;
		if (!GetPointer(B, n, n, ldb, out var pB, out _, out _, out int lldb))
			return false;
		if (!GetPointer(valOut, out var pV, out int nv))
			return false;
		////if (nv < nn)
		////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(valOut));

		delegate*<IntPtr, GeneralEigenType, SolveVectorMode, CuBlasFillMode, int, IntPtr, int, IntPtr, int, IntPtr, out int, CudaSolverStatus> bufFunc;
		delegate*<IntPtr, GeneralEigenType, SolveVectorMode, CuBlasFillMode, int, IntPtr, int, IntPtr, int, IntPtr, IntPtr, int, IntPtr, CudaSolverStatus> calFunc;
		bufFunc = default(T) switch
		{
			Float32 => &NativeMethods.cusolverDnSsygvd_bufferSize,
			Float64 => &NativeMethods.cusolverDnDsygvd_bufferSize,
			Complex<Float32> => &NativeMethods.cusolverDnChegvd_bufferSize,
			Complex<Float64> => &NativeMethods.cusolverDnZhegvd_bufferSize,
			_ => null,
		};
		calFunc = default(T) switch
		{
			Float32 => &NativeMethods.cusolverDnSsygvd,
			Float64 => &NativeMethods.cusolverDnDsygvd,
			Complex<Float32> => &NativeMethods.cusolverDnChegvd,
			Complex<Float64> => &NativeMethods.cusolverDnZhegvd,
			_ => null,
		};
		bufFunc(this.cusolverHandle, eigType, mode, CuBlasFillMode.Upper, nn, pA, llda, pB, lldb, pV, out var work).Check();
		using var buffer = CudaBuffer.Create<T>(work);
		calFunc(this.cusolverHandle, eigType, mode, CuBlasFillMode.Upper, nn, pA, llda, pB, lldb, pV, buffer.DeviceBuffer, work, buffer.ExtraDeviceInfo).Check();
		SolveMethodKind.GeneralEigen.CheckDeviceInfo(buffer.ExtraDeviceInfo);
		return true;
	}

	protected override unsafe bool SingularValues_<T, TReal>(SVDStore storeU, SVDStore storeV, long m, long n, Storage<T> A, long lda, Storage<TReal> S, Storage<T>? U, long ldu, Storage<T>? Vct, long ldvct)
	{
		if (storeU == SVDStore.Overwrite && storeV == SVDStore.Overwrite)
			throw new ArgumentException(Resources.Parameter.DuplicateValue, nameof(storeU));
		if (!T.Type.CheckBaseSupport())
			return false;
		sbyte jobU = storeU.ToChar(), jobV = storeV.ToChar();
		if (jobU == 0 || jobV == 0)
			return false;
		if (!GetPointer(A, m, n, lda, out var pA, out int mm, out int nn, out int llda))
			return false;
		int kk = Math.Min(mm, nn);
		if (!GetPointer(U, storeU == SVDStore.All ? m : kk, m, ldu, out var pU, out int mmU, out _, out int lldu))
			return false;
		if (!GetPointer(Vct, n, storeV == SVDStore.All ? n : kk, ldvct, out var pV, out _, out int nnV, out int lldv))
			return false;
		if (!GetPointer(S, out var pS, out int ns))
			return false;
		////if (ns < kk)
		////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(S));

		if (this.Cuda111OrAbove)
		{
			var type = T.Type.ToCudaDataType();
			if (this.SvdViaPolarDecomposition)
			{
				if (storeU != storeV)
					return false;
				if (storeU == SVDStore.Overwrite)
					return false;
				SolveVectorMode mode = storeU == SVDStore.None ? SolveVectorMode.NoVector : SolveVectorMode.Vector;
				int econ = storeU == SVDStore.Economic ? 1 : 0;
				NativeMethods.cusolverDnXgesvdp_bufferSize(this.cusolverHandle, IntPtr.Zero, mode, econ, m, n, type, pA, lda, type, pS, type, pU, ldu, type, pV, ldvct, type, out var workDevice, out var workHost).Check();
				using var buffer = CudaBuffer.Create(workDevice, workHost);
				NativeMethods.cusolverDnXgesvdp(this.cusolverHandle, IntPtr.Zero, mode, econ, m, n, type, pA, lda, type, pS, type, pU, ldu, type, pV, ldvct, type, buffer.DeviceBuffer, workDevice, buffer.HostBuffer, workHost, buffer.ExtraDeviceInfo, out var error).Check();
				SolveMethodKind.SVD.CheckDeviceInfo(buffer.ExtraDeviceInfo);
			}
			else
			{
				if (m < n)
					return false;
				NativeMethods.cusolverDnXgesvd_bufferSize(this.cusolverHandle, IntPtr.Zero, jobU, jobV, m, n, type, pA, lda, type, pS, type, pU, ldu, type, pV, ldvct, type, out var workDevice, out var workHost).Check();
				using var buffer = CudaBuffer.Create(workDevice, workHost);
				NativeMethods.cusolverDnXgesvd(this.cusolverHandle, IntPtr.Zero, jobU, jobV, m, n, type, pA, lda, type, pS, type, pU, ldu, type, pV, ldvct, type, buffer.DeviceBuffer, workDevice, buffer.HostBuffer, workHost, buffer.ExtraDeviceInfo).Check();
				SolveMethodKind.SVD.CheckDeviceInfo(buffer.ExtraDeviceInfo);
			}
		}
		else
		{   // CUDA version <= 11.0
			if (m < n)
				return false;
			delegate*<IntPtr, int, int, out int, CudaSolverStatus> bufFunc;
			delegate*<IntPtr, sbyte, sbyte, int, int, IntPtr, int, IntPtr, IntPtr, int, IntPtr, int, IntPtr, int, IntPtr, IntPtr, CudaSolverStatus> calFunc;
			bufFunc = default(T) switch
			{
				Float32 => &NativeMethods.cusolverDnSgesvd_bufferSize,
				Float64 => &NativeMethods.cusolverDnDgesvd_bufferSize,
				Complex<Float32> => &NativeMethods.cusolverDnCgesvd_bufferSize,
				Complex<Float64> => &NativeMethods.cusolverDnZgesvd_bufferSize,
				_ => null,
			};
			calFunc = default(T) switch
			{
				Float32 => &NativeMethods.cusolverDnSgesvd,
				Float64 => &NativeMethods.cusolverDnDgesvd,
				Complex<Float32> => &NativeMethods.cusolverDnCgesvd,
				Complex<Float64> => &NativeMethods.cusolverDnZgesvd,
				_ => null,
			};
			bufFunc(this.cusolverHandle, mm, nn, out var work).Check();
			using var buffer = CudaBuffer.Create<T>(work);
			calFunc(this.cusolverHandle, jobU, jobV, mm, nn, pA, llda, pS, pU, lldu, pV, lldv, buffer.DeviceBuffer, work, IntPtr.Zero, buffer.ExtraDeviceInfo).Check();
			SolveMethodKind.GeneralEigen.CheckDeviceInfo(buffer.ExtraDeviceInfo);
		}
		return true;
	}
	#endregion

	#region not supported routines
	protected override bool EigenSpecialMatrixGeneral_<T, TComplex>(SolveVectorMode mode, long n, Storage<TComplex> valOut, Storage<TComplex>? leftVec, long ldvl, Storage<TComplex>? rightVec, long ldvr, Storage<T> A, long lda) => false;

	protected override bool EigenGeneralMatrixGeneral_<T, TComplex>(GeneralEigenType type, SolveVectorMode mode, long n, Storage<TComplex> α, Storage<T> β, Storage<TComplex>? leftVec, long ldvl, Storage<TComplex>? rightVec, long ldvr, Storage<T> A, long lda, Storage<T> B, long ldb) => false;

	protected override bool SchurDecomposition_<T>(SolveVectorMode jobu, long n, Storage<T> A, long lda, Storage<T>? U, long ldu, out long actualNumber, Storage<ComplexDouble>? orderVal = null)
	{
		actualNumber = 0; return false;
	}
	#endregion
	#endregion
}