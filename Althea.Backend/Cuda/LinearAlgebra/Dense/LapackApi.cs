using System.Runtime.CompilerServices;

using Althea.Backend.Cuda.Storage;
using Althea.LinearAlgebra;
using Althea.LinearAlgebra.Dense;

using static Althea.Backend.Cuda.MemoryPointerChecker;

using NM = Althea.Backend.Cuda.LinearAlgebra.Dense.NativeMethods;


namespace Althea.Backend.Cuda.LinearAlgebra.Dense;

public unsafe partial class Api
{
	#region eigen solve
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void ToComp<T>(T* real, T* comp, long n) where T : unmanaged, IBaseNumber<T>
	{
		Storage.NativeMethods.cudaMemset(comp, 0, 2 * n * sizeof(T));
		if (typeof(T) == typeof(Float32))
			NM.cublasScopy_v2(this.cublasHandle, (int)n, real, 1, comp, 2).Check();
		else if (typeof(T) == typeof(Float64))
			NM.cublasDcopy_v2(this.cublasHandle, (int)n, real, 1, comp, 2).Check();
		else
			Storage.Api.PointerStridedCopy(real, 1, comp, 2, MemoryCopyKind.DeviceToDevice, n);
	}

	/// <inheritdoc/>
	public virtual bool EigenStandardMatrixHermitian<T, TS1, TS2, TS3>(long n, bool upper, TS1 A, long lda, TS2 valOut, TS3? vecOut, long ldvec, bool allowDestroy = false) where T : unmanaged, IBinaryFloat<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>
	{
		if (!default(T).CheckBaseSupport())
			return false;
		if (!GetPointer(this, A, n, n, lda, out T* pA))
			return false;
		if (!GetPointer(this, vecOut, n, n, ldvec, out T* pV))
			return false;
		if (!GetPointer(valOut, out T* px, out long nx))
			return false;
		if (nx != n)
			throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(valOut));

		var type = T.Type.ToCudaDataType();
		var uplo = upper ? CuBlasFillMode.Upper : CuBlasFillMode.Lower;
		var mode = pV == null ? CuSolverEigMode.NoVector : CuSolverEigMode.Vector;
		using var ppV = allowDestroy && pV == null ? CudaBuffer.Create(pA, lda, n) : CudaBuffer.Create(pV, ldvec, n);
		using var ppx = T.IsComplexType ? CudaBuffer.Create<T>(n * sizeof(T) / 2) : CudaBuffer.Create(px);
		NM.cusolverDnXsyevd_bufferSize(this.cusolverHandle, null, mode, uplo, n, type, ppV, ppV.LD, type, ppx, type, out var workDevice, out var workHost).Check();
		using var buffer = CudaBuffer.Create(workDevice, workHost);
		NM.cusolverDnXsyevd(this.cusolverHandle, null, mode, uplo, n, type, ppV, ppV.LD, type, ppx, type, buffer.DeviceBuffer, workDevice, buffer.HostBuffer, workHost, buffer.ExtraDeviceInfo).Check();
		SolveMethodKind.Eigenvalue.CheckDeviceInfo(buffer.ExtraDeviceInfo);
		this.ToComp(ppx, px, n);
		return true;
	}

	/// <inheritdoc/>
	public virtual bool EigenGeneralMatrixHermitian<T, TS1, TS2, TS3, TS4>(GeneralEigenType type, long n, bool upper, TS1 A, long lda, TS1 B, long ldb, TS2 valOut, TS3? vecOut, long ldvec, TS4? LUOut, long ldLU, bool allowDestroy = false) where T : unmanaged, IBinaryFloat<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3> where TS4 : class, IStorage<T, TS4>
	{
		if (!GetPointer(this, A, n, n, lda, out T* pA, out int nn, out _, out _))
			return false;
		if (!GetPointer(this, B, n, n, ldb, out T* pB, out _, out _, out _))
			return false;
		if (!GetPointer(this, vecOut, n, n, ldvec, out T* pV, out _, out _, out _))
			return false;
		if (!GetPointer(this, LUOut, n, n, ldLU, out T* pLU, out _, out _, out _))
			return false;
		if (!GetPointer(this, valOut, 1, out T* px, out int nv, out _))
			return false;
		if (nv != nn)
			throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(valOut));

		delegate*<IntPtr, GeneralEigenType, CuSolverEigMode, CuBlasFillMode, int, T*, int, T*, int, T*, out int, CudaSolverStatus> bufFunc;
		delegate*<IntPtr, GeneralEigenType, CuSolverEigMode, CuBlasFillMode, int, T*, int, T*, int, T*, void*, int, void*, CudaSolverStatus> calFunc;
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
		if (bufFunc is null)
			return false;
		var uplo = upper ? CuBlasFillMode.Upper : CuBlasFillMode.Lower;
		var mode = pV == null ? CuSolverEigMode.NoVector : CuSolverEigMode.Vector;
		using var ppV = allowDestroy && pV == null ? CudaBuffer.Create(pA, lda, n) : CudaBuffer.Create(pV, ldvec, n);
		using var ppLU = allowDestroy && pLU == null ? CudaBuffer.Create(pB, ldb, n) : CudaBuffer.Create(pLU, ldLU, n);
		using var ppx = T.IsComplexType ? CudaBuffer.Create<T>(n * sizeof(T) / 2) : CudaBuffer.Create(px);
		if (pA != ppV)
			Storage.NativeMethods.cudaMemcpy2D(ppV, ppV.LD, pA, lda, n * sizeof(T), n, MemoryCopyKind.DeviceToDevice).Check();
		if (pB != ppLU)
			Storage.NativeMethods.cudaMemcpy2D(ppLU, ppLU.LD, pB, ldb, n * sizeof(T), n, MemoryCopyKind.DeviceToDevice).Check();
		bufFunc(this.cusolverHandle, type, mode, uplo, nn, ppV, (int)ppV.LD, ppLU, (int)ppLU.LD, ppx, out var work).Check();
		using var buffer = CudaBuffer.Create<T>(work);
		calFunc(this.cusolverHandle, type, mode, uplo, nn, ppV, (int)ppV.LD, ppLU, (int)ppLU.LD, ppx, buffer.DeviceBuffer, work, buffer.ExtraDeviceInfo).Check();
		SolveMethodKind.GeneralEigen.CheckDeviceInfo(buffer.ExtraDeviceInfo);
		return true;
	}

	/// <inheritdoc/>
	public virtual bool SingularValues<T, TS1, TS2, TS3, TS4>(bool fullU, bool fullV, long m, long n, TS1 A, long lda, TS2? U, long ldu, TS3? Vct, long ldvct, TS4 S, bool allowDestroy = false) where T : unmanaged, IBinaryFloat<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3> where TS4 : class, IStorage<T, TS4>
	{
		if (!default(T).CheckBaseSupport())
			return false;
		long mn = Math.Min(m, n);
		if (!GetPointer(this, A, m, n, lda, out T* pA))
			return false;
		if (!GetPointer(this, U, m, fullU ? m : mn, ldu, out T* pU))
			return false;
		if (!GetPointer(this, Vct, fullV ? n : mn, n, ldvct, out T* pV))
			return false;
		if (!GetPointer(S, out T* px, out long nx))
			return false;
		if (nx != mn)
			throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(S));
		if (pU == pA && pV == pA)
			throw new ArgumentException(Resources.ParameterError.DuplicateValue);
		if (pU == pA && fullU && m > mn)
			throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(fullU));
		if (pV == pA && fullV && n > mn)
			throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(fullV));

		var type = T.Type.ToCudaDataType();
		using var ppA = allowDestroy ? CudaBuffer.Create(pA, lda, n) : CudaBuffer.Create<T>(null, m, n);
		Storage.NativeMethods.cudaMemcpy2D(ppA, ppA.LD, pA, lda, m * sizeof(T), n, MemoryCopyKind.DeviceToDevice).Check();
		using var ppx = T.IsComplexType ? CudaBuffer.Create<T>(n * sizeof(T) / 2) : CudaBuffer.Create(px);
		NM.cusolverDnXgesvd_bufferSize(this.cusolverHandle, null, Conversions.ToSvdChar(pA, pU, fullU), Conversions.ToSvdChar(pA, pV, fullV), m, n, type, ppA, ppA.LD, type, ppx, type, pU, ldu, type, pV, ldvct, type, out long workDevice, out long workHost).Check();
		using var buffer = CudaBuffer.Create(workDevice, workHost);
		NM.cusolverDnXgesvd(this.cusolverHandle, null, Conversions.ToSvdChar(pA, pU, fullU), Conversions.ToSvdChar(pA, pV, fullV), m, n, type, ppA, ppA.LD, type, ppx, type, pU, ldu, type, pV, ldvct, type, buffer.DeviceBuffer, workDevice, buffer.HostBuffer, workHost, buffer.ExtraDeviceInfo).Check();
		SolveMethodKind.SVD.CheckDeviceInfo(buffer.ExtraDeviceInfo);
		this.ToComp(ppx, px, n);
		return true;
	}
	#endregion

	#region linear solve
	/// <inheritdoc/>
	public virtual bool LinearSolveGeneral<T, TS1, TS2>(long n, long nrhs, TS1 A, long lda, TS2 B, long ldb, bool allowDestroy = false) where T : unmanaged, IBinaryFloat<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
	{
		if (!default(T).CheckBaseSupport())
			return false;
		if (!GetPointer(this, A, n, n, lda, out T* pA))
			return false;
		if (!GetPointer(this, B, n, nrhs, ldb, out T* pB))
			return false;

		var type = T.Type.ToCudaDataType();
		// factorize
		NativeMethods.cusolverDnXgetrf_bufferSize(this.cusolverHandle, null, n, n, type, pA, lda, type, out var workDevice, out var workHost).Check();
		using var buffer = CudaBuffer.Create(workDevice + n * sizeof(long), workHost);
		void* pP = ((byte*)buffer.DeviceBuffer + workDevice);
		NativeMethods.cusolverDnXgetrf(this.cusolverHandle, null, n, n, type, pA, lda, pP, type, buffer.DeviceBuffer, workDevice, buffer.HostBuffer, workHost, buffer.ExtraDeviceInfo).Check();
		SolveMethodKind.LU.CheckDeviceInfo(buffer.ExtraDeviceInfo);
		// solve
		NativeMethods.cusolverDnXgetrs(this.cusolverHandle, null, CuBlasOperation.None, n, nrhs, type, pA, lda, pP, type, pB, ldb, buffer.ExtraDeviceInfo).Check();
		SolveMethodKind.LU.CheckDeviceInfo(buffer.ExtraDeviceInfo);
		return true;
	}
	#endregion

	#region QR solve
	/// <inheritdoc/>
	public virtual bool QRDecomposition<T, TS1, TS2>(bool full, long m, long n, TS1 A, long lda, TS2? Q, long ldq) where T : unmanaged, IBinaryFloat<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
	{
		if (!default(T).CheckBaseSupport())
			return false;
		if (!GetPointer(this, A, m, n, lda, out T* pA, out int mm, out int nn, out int llda))
			return false;
		int kk = Math.Min(mm, nn); long colsQ = full ? m : kk;
		if (!GetPointer(this, Q, m, colsQ, ldq, out T* pQ, out _, out int nnQ, out int lldq))
			return false;

		delegate*<IntPtr, int, int, int, T*, int, T*, out int, CudaSolverStatus> bufGetQFunc = default(T) switch
		{
			Float32 => &NM.cusolverDnSorgqr_bufferSize,
			Float64 => &NM.cusolverDnDorgqr_bufferSize,
			Complex<Float32> => &NM.cusolverDnCungqr_bufferSize,
			Complex<Float64> => &NM.cusolverDnZungqr_bufferSize,
			_ => null
		};
		delegate*<IntPtr, int, int, int, T*, int, T*, void*, int, void*, CudaSolverStatus> calGetQFunc = default(T) switch
		{
			Float32 => &NM.cusolverDnSorgqr,
			Float64 => &NM.cusolverDnDorgqr,
			Complex<Float32> => &NM.cusolverDnCungqr,
			Complex<Float64> => &NM.cusolverDnZungqr,
			_ => null
		};
		// get buffer sizes
		using var tau = CudaBuffer.Create(kk * sizeof(T), 0, false);
		var type = T.Type.ToCudaDataType();
		NM.cusolverDnXgeqrf_bufferSize(this.cusolverHandle, null, m, n, type, pA, lda, type, tau.DeviceBuffer, type, out var workDevice, out var workHost).Check();
		bufGetQFunc(this.cusolverHandle, mm, nnQ, kk, pQ, lldq, (T*)tau.DeviceBuffer, out int workDevice2).Check();
		using var buffer = CudaBuffer.Create(Math.Max(workDevice, workDevice2), workHost);
		// implicit QR
		NM.cusolverDnXgeqrf(this.cusolverHandle, null, m, n, type, pA, lda, type, tau.DeviceBuffer, type, buffer.DeviceBuffer, workDevice, buffer.HostBuffer, workHost, buffer.ExtraDeviceInfo).Check();
		SolveMethodKind.QR.CheckDeviceInfo(buffer.ExtraDeviceInfo);
		// copy A to Q
		Storage.NativeMethods.cudaMemcpy2D(pQ, ldq, pA, lda, m, Math.Min(colsQ, n), MemoryCopyKind.DeviceToDevice);
		// form Q
		calGetQFunc(this.cusolverHandle, mm, nnQ, kk, pQ, lldq, (T*)tau.DeviceBuffer, buffer.DeviceBuffer, workDevice2, buffer.ExtraDeviceInfo).Check();
		SolveMethodKind.QR.CheckDeviceInfo(buffer.ExtraDeviceInfo);
		return true;
	}

	/// <inheritdoc/>
	public virtual bool LeastSquareSolve<T, TS1, TS2>(long m, long n, long nrhs, TS1 A, long lda, TS2 B, long ldb, bool allowDestroy = false) where T : unmanaged, IBinaryFloat<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
	{
		if (!default(T).CheckBaseSupport())
			return false;
		if (!GetPointer(this, A, m, n, lda, out T* pA, out int mm, out int nn, out int llda))
			return false;
		if (!GetPointer(this, B, m, nrhs, ldb, out T* pB, out _, out int nnrhs, out int lldb))
			return false;

		delegate*<IntPtr, CuBlasSideMode, CuBlasOperation, int, int, int, T*, int, T*, T*, int, out int, CudaSolverStatus> bufQmulFunc = default(T) switch
		{
			Float32 => &NM.cusolverDnSormqr_bufferSize,
			Float64 => &NM.cusolverDnDormqr_bufferSize,
			Complex<Float32> => &NM.cusolverDnCunmqr_bufferSize,
			Complex<Float64> => &NM.cusolverDnCunmqr_bufferSize,
			_ => null
		};
		delegate*<IntPtr, CuBlasSideMode, CuBlasOperation, int, int, int, T*, int, T*, T*, int, void*, int, void*, CudaSolverStatus> calQmulFunc = default(T) switch
		{
			Float32 => &NM.cusolverDnSormqr,
			Float64 => &NM.cusolverDnDormqr,
			Complex<Float32> => &NM.cusolverDnCunmqr,
			Complex<Float64> => &NM.cusolverDnCunmqr,
			_ => null
		};
		delegate*<IntPtr, CuBlasSideMode, CuBlasFillMode, CuBlasOperation, CuBlasDiagType, int, int, T*, T*, int, T*, int, CudaBlasStatus> triSolveFunc = default(T) switch
		{
			Float32 => &NM.cublasStrsm_v2,
			Float64 => &NM.cublasDtrsm_v2,
			Complex<Float32> => &NM.cublasCtrsm_v2,
			Complex<Float64> => &NM.cublasZtrsm_v2,
			_ => null
		};
		// get buffer sizes
		using var tau = CudaBuffer.Create(Math.Min(m, n) * sizeof(T), 0, false);
		var type = T.Type.ToCudaDataType();
		NM.cusolverDnXgeqrf_bufferSize(this.cusolverHandle, null, m, n, type, pA, lda, type, tau.DeviceBuffer, type, out var workDevice, out var workHost).Check();
		bufQmulFunc(this.cusolverHandle, CuBlasSideMode.Left, CuBlasOperation.None, nn, nnrhs, nn, pA, llda, (T*)tau.DeviceBuffer, pB, lldb, out var workDevice2).Check();
		using var buffer = CudaBuffer.Create(Math.Max(workDevice, workDevice2), workHost);
		// implicit QR
		NM.cusolverDnXgeqrf(this.cusolverHandle, null, m, n, type, pA, lda, type, tau.DeviceBuffer, type, buffer.DeviceBuffer, workDevice, buffer.HostBuffer, workHost, buffer.ExtraDeviceInfo).Check();
		SolveMethodKind.QR.CheckDeviceInfo(buffer.ExtraDeviceInfo);
		// implicit Q^H * B
		calQmulFunc(this.cusolverHandle, CuBlasSideMode.Left, CuBlasOperation.None, mm, nnrhs, nn, pA, llda, (T*)tau.DeviceBuffer, pB, lldb, buffer.DeviceBuffer, workDevice2, buffer.ExtraDeviceInfo).Check();
		SolveMethodKind.QR.CheckDeviceInfo(buffer.ExtraDeviceInfo);
		// triangular solve R * X = Q^H * B
		T one = T.One;
		triSolveFunc(this.cublasHandle, CuBlasSideMode.Left, CuBlasFillMode.Upper, CuBlasOperation.None, CuBlasDiagType.NonUnit, nn, nnrhs, &one, pA, llda, pB, lldb).Check();
		return true;
	}
	#endregion

	#region not supported
	bool ILapackAbstractApi.EigenStandardMatrixGeneral<T, TS1, TS2, TS3, TS4>(long n, TS1 A, long lda, TS2 valsOut, TS2? valsOutImag, TS3? leftVec, long ldvl, TS4? rightVec, long ldvr, bool allowDestroy)
		where TS2 : class
		where TS3 : class
		where TS4 : class => false;
	bool ILapackAbstractApi.EigenGeneralMatrixGeneral<T, TS1, TS2, TS3, TS4>(GeneralEigenType type, long n, TS1 A, long lda, TS1 B, long ldb, TS2 valsOut, TS2? valsOutImag, TS2 valsOutDenom, TS3? leftVec, long ldvl, TS4? rightVec, long ldvr, bool allowDestroy)
		where TS2 : class
		where TS3 : class
		where TS4 : class => false;
	bool ILapackAbstractApi.SchurDecomposition<T, TS1, TS2, TS3>(long n, TS1 A, long lda, TS2? U, long ldu, TS3 valsOut, TS3? valsOutImag)
		where TS2 : class
		where TS3 : class => false;
	bool ILapackAbstractApi.SchurReorder<T, TInd, TS1, TS2, TS3, TSInd>(long n, TS1 A, long lda, TS2? U, long ldu, TS3 vals, TS3? valsImag, TSInd select)
		where TS2 : class
		where TS3 : class => false;
	#endregion
}
