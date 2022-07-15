using Althea.Backend.CSharp.Storage;
using Althea.LinearAlgebra;

using NM = Althea.Backend.Mkl.LinearAlgebra.Dense.NativeMethods;


namespace Althea.Backend.Mkl.LinearAlgebra.Dense
{
	/// <remarks>The general SVD and general Schur decompositions are not supported, but can be added simply.</remarks>
	public unsafe partial class Api
	{
		#region eigen-problems
		/// <inheritdoc/>
		public virtual bool EigenStandardMatrixHermitian<T, TS1, TS2, TS3>(long n, bool upper, TS1 A, long lda, TS2 valOut, TS3? vecOut, long ldvec, bool allowDestroy = false) where T : unmanaged, IBinaryFloat<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>
		{
			if (!GetPointer(A, n, n, lda, out T* pA))
				return false;
			if (!GetPointer(vecOut, n, n, ldvec, out T* pV))
				return false;
			if (!GetPointer(valOut, 1, out T* px, out long nx))
				return false;
			if (nx < n)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(valOut));
			delegate*<MklMatrixLayout, MklVectorModeChar, MklFillModeChar, MklInt, T*, MklInt, T*, MklLapackInfo> func = default(T) switch
			{
				Float32 => &NM.LAPACKE_ssyev,
				Float64 => &NM.LAPACKE_dsyev,
				Complex<Float32> => &NM.LAPACKE_cheev,
				Complex<Float64> => &NM.LAPACKE_zheev,
				_ => null
			};
			if (func == null)
				return false;
			using var ppV = allowDestroy && pV == null ? Buffers.Create(pA, lda, n) : Buffers.Create(pV, ldvec, n);
			using var ppx = T.IsComplexType ? Buffers.Create<T>(n * sizeof(T) / 2) : Buffers.Create(px);
			Storage.Api.PointerMemoryCopy2D(pA, lda, ppV, ppV.ld, n, n);
			func(MklMatrixLayout.ColMajor, pV == null ? MklVectorModeChar.Vector : MklVectorModeChar.NoVector, upper ? MklFillModeChar.Upper : MklFillModeChar.Lower, n, ppV, ppV.ld, ppx).Check(SolveMethodKind.Eigenvalue);
			RealToComp(ppx, px, n);
			return true;
		}

		/// <inheritdoc/>
		public virtual bool EigenGeneralMatrixHermitian<T, TS1, TS2, TS3, TS4>(GeneralEigenType type, long n, bool upper, TS1 A, long lda, TS1 B, long ldb, TS2 valOut, TS3? vecOut, long ldvec, TS4? LUOut, long ldLU, bool allowDestroy = false) where T : unmanaged, IBinaryFloat<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3> where TS4 : class, IStorage<T, TS4>
		{
			if (!GetPointer(A, n, n, lda, out T* pA))
				return false;
			if (!GetPointer(B, n, n, ldb, out T* pB))
				return false;
			if (!GetPointer(vecOut, n, n, ldvec, out T* pV))
				return false;
			if (!GetPointer(LUOut, n, n, ldLU, out T* pLU))
				return false;
			if (!GetPointer(valOut, 1, out T* px, out long nx))
				return false;
			if (nx < n)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(valOut));
			delegate*<MklMatrixLayout, GeneralEigenType, MklVectorModeChar, MklFillModeChar, MklInt, T*, MklInt, T*, MklInt, T*, MklLapackInfo> func = default(T) switch
			{
				Float32 => &NM.LAPACKE_ssygv,
				Float64 => &NM.LAPACKE_dsygv,
				Complex<Float32> => &NM.LAPACKE_chegv,
				Complex<Float64> => &NM.LAPACKE_zhegv,
				_ => null
			};
			if (func == null)
				return false;
			using var ppV = allowDestroy && pV == null ? Buffers.Create(pA, lda, n) : Buffers.Create(pV, ldvec, n);
			using var ppLU = allowDestroy && pLU == null ? Buffers.Create(pB, ldb, n) : Buffers.Create(pLU, ldLU, n);
			using var ppx = T.IsComplexType ? Buffers.Create<T>(n * sizeof(T) / 2) : Buffers.Create(px);
			Storage.Api.PointerMemoryCopy2D(pA, lda, ppV, ppV.ld, n, n);
			Storage.Api.PointerMemoryCopy2D(pB, ldb, ppLU, ppLU.ld, n, n);
			func(MklMatrixLayout.ColMajor, type, vecOut is null ? MklVectorModeChar.NoVector : MklVectorModeChar.Vector, upper ? MklFillModeChar.Upper : MklFillModeChar.Lower, n, ppV, ppV.ld, ppLU, ppLU.ld, ppx).Check(SolveMethodKind.GeneralEigen);
			RealToComp(ppx, px, n);
			return true;
		}

		/// <inheritdoc/>
		public virtual bool EigenStandardMatrixGeneral<T, TS1, TS2, TS3, TS4>(long n, TS1 A, long lda, TS2 valsOut, TS2? valsOutImag, TS3? leftVec, long ldvl, TS4? rightVec, long ldvr, bool allowDestroy = false) where T : unmanaged, IBinaryFloat<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3> where TS4 : class, IStorage<T, TS4>
		{
			if (!GetPointer(A, n, n, lda, out T* pA))
				return false;
			if (!GetPointer(leftVec, n, n, ldvl, out T* pVl))
				return false;
			if (!GetPointer(rightVec, n, n, ldvr, out T* pVr))
				return false;
			if (!GetPointer(valsOut, 1, out T* px, out long nx))
				return false;
			if (!T.IsComplexType && valsOutImag is null)
				throw new ArgumentNullException(nameof(valsOutImag));
			T* pxx = null;
			if (valsOutImag is not null && !GetPointer(valsOutImag, 1, out pxx, out long nx2))
			{
				if (nx2 != nx)
					throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(valsOutImag));
				return false;
			}
			if (nx < n)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(valsOut));
			delegate*<MklMatrixLayout, MklVectorModeChar, MklVectorModeChar, MklInt, T*, MklInt, T*, T*, T*, MklInt, T*, MklInt, MklLapackInfo> funcRe = default(T) switch
			{
				Float32 => &NM.LAPACKE_sgeev,
				Float64 => &NM.LAPACKE_dgeev,
				_ => null
			};
			delegate*<MklMatrixLayout, MklVectorModeChar, MklVectorModeChar, MklInt, T*, MklInt, T*, T*, MklInt, T*, MklInt, MklLapackInfo> funcIm = default(T) switch
			{
				Complex<Float32> => &NM.LAPACKE_cgeev,
				Complex<Float64> => &NM.LAPACKE_zgeev,
				_ => null
			};
			if (funcRe == null && funcIm == null)
				return false;
			using var ppA = allowDestroy ? Buffers.Create(pA, lda, n) : Buffers.Create<T>(null, n, n);
			Storage.Api.PointerMemoryCopy2D(pA, lda, ppA, ppA.ld, n, n);
			MklLapackInfo info;
			if (funcRe != null)
			{
				info = funcRe(MklMatrixLayout.ColMajor, leftVec is null ? MklVectorModeChar.NoVector : MklVectorModeChar.Vector, rightVec is null ? MklVectorModeChar.NoVector : MklVectorModeChar.Vector, n, ppA, ppA.ld, px, pxx, pVl, ldvl, pVr, ldvr);
			}
			else
			{
				info = funcIm(MklMatrixLayout.ColMajor, leftVec is null ? MklVectorModeChar.NoVector : MklVectorModeChar.Vector, rightVec is null ? MklVectorModeChar.NoVector : MklVectorModeChar.Vector, n, ppA, ppA.ld, px, pVl, ldvl, pVr, ldvr);
			}
			info.Check(SolveMethodKind.NonSymmetricEigenvalue);
			return true;
		}

		/// <inheritdoc/>
		public virtual bool EigenGeneralMatrixGeneral<T, TS1, TS2, TS3, TS4>(GeneralEigenType type, long n, TS1 A, long lda, TS1 B, long ldb, TS2 valsOut, TS2? valsOutImag, TS2 valsOutDenom, TS3? leftVec, long ldvl, TS4? rightVec, long ldvr, bool allowDestroy = false) where T : unmanaged, IBinaryFloat<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3> where TS4 : class, IStorage<T, TS4>
		{
			if (!GetPointer(A, n, n, lda, out T* pA))
				return false;
			if (!GetPointer(B, n, n, ldb, out T* pB))
				return false;
			if (!GetPointer(leftVec, n, n, ldvl, out T* pVl))
				return false;
			if (!GetPointer(rightVec, n, n, ldvr, out T* pVr))
				return false;
			if (!GetPointer(valsOut, 1, out T* px, out long nx))
				return false;
			if (!GetPointer(valsOutDenom, 1, out T* pxd, out long nxd))
				return false;
			if (nxd != nx)
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(valsOutDenom));
			if (!T.IsComplexType && valsOutImag is null)
				throw new ArgumentNullException(nameof(valsOutImag));
			T* pxx = null;
			if (valsOutImag is not null && !GetPointer(valsOutImag, 1, out pxx, out long nx2))
			{
				if (nx2 != nx)
					throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(valsOutImag));
				return false;
			}
			if (nx < n)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(valsOut));
			delegate*<MklMatrixLayout, MklVectorModeChar, MklVectorModeChar, MklInt, T*, MklInt, T*, MklInt, T*, T*, T*, T*, MklInt, T*, MklInt, MklLapackInfo> funcRe = default(T) switch
			{
				Float32 => &NM.LAPACKE_sggev,
				Float64 => &NM.LAPACKE_dggev,
				_ => null
			};
			delegate*<MklMatrixLayout, MklVectorModeChar, MklVectorModeChar, MklInt, T*, MklInt, T*, MklInt, T*, T*, T*, MklInt, T*, MklInt, MklLapackInfo> funcIm = default(T) switch
			{
				Complex<Float32> => &NM.LAPACKE_cggev,
				Complex<Float64> => &NM.LAPACKE_zggev,
				_ => null
			};
			if (funcRe == null && funcIm == null)
				return false;
			using var ppA = allowDestroy ? Buffers.Create<T>(pA, lda, n) : Buffers.Create<T>(null, n, n);
			using var ppB = allowDestroy ? Buffers.Create<T>(pB, ldb, n) : Buffers.Create<T>(null, n, n);
			Storage.Api.PointerMemoryCopy2D(pA, lda, ppA, ppA.ld, n, n);
			Storage.Api.PointerMemoryCopy2D(pB, ldb, ppB, ppB.ld, n, n);
			MklLapackInfo info;
			if (funcRe != null)
			{
				info = funcRe(MklMatrixLayout.ColMajor, leftVec is null ? MklVectorModeChar.NoVector : MklVectorModeChar.Vector, rightVec is null ? MklVectorModeChar.NoVector : MklVectorModeChar.Vector, n, ppA, ppA.ld, ppB, ppB.ld, px, pxx, pxd, pVl, ldvl, pVr, ldvr);
			}
			else
			{
				info = funcIm(MklMatrixLayout.ColMajor, leftVec is null ? MklVectorModeChar.NoVector : MklVectorModeChar.Vector, rightVec is null ? MklVectorModeChar.NoVector : MklVectorModeChar.Vector, n, ppA, ppA.ld, ppB, ppB.ld, px, pxd, pVl, ldvl, pVr, ldvr);
			}
			info.Check(SolveMethodKind.NonSymmetricGeneralEigenvalue);
			return true;
		}
		#endregion

		#region other decompositions
		/// <inheritdoc/>
		public virtual bool SingularValues<T, TS1, TS2, TS3, TS4>(bool fullU, bool fullV, long m, long n, TS1 A, long lda, TS2? U, long ldu, TS3? Vct, long ldvct, TS4 S, bool allowDestroy = false) where T : unmanaged, IBinaryFloat<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3> where TS4 : class, IStorage<T, TS4>
		{
			long mn = Math.Min(m, n);
			if (!GetPointer(A, m, n, lda, out T* pA))
				return false;
			if (!GetPointer(U, m, fullU ? m : mn, ldu, out T* pU))
				return false;
			if (!GetPointer(Vct, fullV ? n : mn, n, ldvct, out T* pV))
				return false;
			if (!GetPointer(S, 1, out T* px, out long nx))
				return false;
			if (nx < mn)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(S));
			if (pU == pA && pV == pA)
				throw new ArgumentException(Resources.ParameterError.DuplicateValue);
			if (pU == pA && fullU && m > mn)
				throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(fullU));
			if (pV == pA && fullV && n > mn)
				throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(fullV));
			delegate*<MklMatrixLayout, MklSvdModeChar, MklSvdModeChar, MklInt, MklInt, T*, MklInt, T*, T*, MklInt, T*, MklInt, T*, MklLapackInfo> func = default(T) switch
			{
				Float32 => &NM.LAPACKE_sgesvd,
				Float64 => &NM.LAPACKE_dgesvd,
				Complex<Float32> => &NM.LAPACKE_cgesvd,
				Complex<Float64> => &NM.LAPACKE_zgesvd,
				_ => null
			};
			if (func == null)
				return false;
			using var ppA = allowDestroy ? Buffers.Create(pA, lda, n) : Buffers.Create<T>(null, m, n);
			Storage.Api.PointerMemoryCopy2D(pA, lda, ppA, ppA.ld, m, n);
			using var ppx = T.IsComplexType ? Buffers.Create<T>(n * sizeof(T) / 2) : Buffers.Create(px);
			using var pSurperb = T.IsComplexType ? Buffers.Create<T>(n * sizeof(T) / 2) : Buffers.Create<T>(n * sizeof(T));
			func(MklMatrixLayout.ColMajor,
				pU == pA ? MklSvdModeChar.Overwrite : fullU ? MklSvdModeChar.All : pU == null ? MklSvdModeChar.None : MklSvdModeChar.Store,
				pV == pA ? MklSvdModeChar.Overwrite : fullV ? MklSvdModeChar.All : pV == null ? MklSvdModeChar.None : MklSvdModeChar.Store,
				m, n, ppA, ppA.ld, ppx, pU, ldu, pV, ldvct, pSurperb).Check(SolveMethodKind.SVD);
			RealToComp(ppx, px, n);
			return true;
		}

		/// <inheritdoc/>
		public virtual bool SchurDecomposition<T, TS1, TS2, TS3>(long n, TS1 A, long lda, TS2? U, long ldu, TS3 valOut, TS3? valImagOut) where T : unmanaged, IBinaryFloat<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>
		{
			if (!GetPointer(A, n, n, lda, out T* pA))
				return false;
			if (!GetPointer(U, n, n, ldu, out T* pU))
				return false;
			if (!GetPointer(valOut, 1, out T* px, out long nx))
				return false;
			if (!T.IsComplexType && valImagOut is null)
				throw new ArgumentNullException(nameof(valImagOut));
			T* pxx = null;
			if (valImagOut is not null && !GetPointer(valImagOut, 1, out pxx, out long nx2))
			{
				if (nx2 != nx)
					throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(valImagOut));
				return false;
			}
			if (nx < n)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(valOut));
			delegate*<MklMatrixLayout, MklVectorModeChar, MklSortModeChar, delegate* unmanaged<void*, void*, MklInt>, MklInt, T*, MklInt, out MklInt, T*, T*, T*, MklInt, MklLapackInfo> funcRe = default(T) switch
			{
				Float32 => &NM.LAPACKE_sgees,
				Float64 => &NM.LAPACKE_dgees,
				_ => null
			};
			delegate*<MklMatrixLayout, MklVectorModeChar, MklSortModeChar, delegate* unmanaged<void*, MklInt>, MklInt, T*, MklInt, out MklInt, T*, T*, MklInt, MklLapackInfo> funcIm = default(T) switch
			{
				Complex<Float32> => &NM.LAPACKE_cgees,
				Complex<Float64> => &NM.LAPACKE_zgees,
				_ => null
			};
			if (funcRe == null && funcIm == null)
				return false;
			MklLapackInfo info;
			if (funcRe != null)
			{
				info = funcRe(MklMatrixLayout.ColMajor, pU == null ? MklVectorModeChar.NoVector : MklVectorModeChar.Vector, MklSortModeChar.NoSort, null, n, pA, lda, out _, px, pxx, pU, ldu);
			}
			else
			{
				info = funcIm(MklMatrixLayout.ColMajor, pU == null ? MklVectorModeChar.NoVector : MklVectorModeChar.Vector, MklSortModeChar.NoSort, null, n, pA, lda, out _, px, pU, ldu);
			}
			info.Check(SolveMethodKind.Schur);
			return true;
		}

		/// <inheritdoc/>
		public virtual bool SchurReorder<T, TInd, TS1, TS2, TS3, TSInd>(long n, TS1 A, long lda, TS2? U, long ldu, TS3 vals, TS3? valsImag, TSInd select) where T : unmanaged, IBinaryFloat<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3> where TInd : unmanaged, IBaseNumber<TInd> where TSInd : class, IStorage<TInd, TSInd>
		{
			if (!TInd.Type.IsInteger() || TInd.Size != sizeof(MklInt))
				return false;
			if (!GetPointer(A, n, n, lda, out T* pA))
				return false;
			if (!GetPointer(U, n, n, ldu, out T* pU))
				return false;
			if (!GetPointer(select, 1, out TInd* pSelect, out long nn))
				return false;
			MklInt* ps = (MklInt*)pSelect;
			if (nn != n)
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(select));
			if (!GetPointer(vals, 1, out T* px, out long nx))
				return false;
			if (!T.IsComplexType && valsImag is null)
				throw new ArgumentNullException(nameof(valsImag));
			T* pxx = null;
			if (valsImag is not null && !GetPointer(valsImag, 1, out pxx, out long nx2))
			{
				if (nx2 != nx)
					throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(valsImag));
				return false;
			}
			delegate*<MklMatrixLayout, MklSchurReorderConditionNumberModeChar, MklVectorModeChar, MklInt*, MklInt, T*, MklInt, T*, MklInt, T*, T*, out MklInt, void*, void*, MklLapackInfo> funcRe = default(T) switch
			{
				Float32 => &NM.LAPACKE_strsen,
				Float64 => &NM.LAPACKE_dtrsen,
				_ => null
			};
			delegate*<MklMatrixLayout, MklSchurReorderConditionNumberModeChar, MklVectorModeChar, MklInt*, MklInt, T*, MklInt, T*, MklInt, T*, out MklInt, void*, void*, MklLapackInfo> funcIm = default(T) switch
			{
				Complex<Float32> => &NM.LAPACKE_ctrsen,
				Complex<Float64> => &NM.LAPACKE_ztrsen,
				_ => null
			};
			if (funcRe == null && funcIm == null)
				return false;
			MklLapackInfo info;
			if (funcRe != null)
			{
				info = funcRe(MklMatrixLayout.ColMajor, MklSchurReorderConditionNumberModeChar.None, pU == null ? MklVectorModeChar.NoVector : MklVectorModeChar.Vector, ps, n, pA, lda, pU, ldu, px, pxx, out _, null, null);
			}
			else
			{
				info = funcIm(MklMatrixLayout.ColMajor, MklSchurReorderConditionNumberModeChar.None, pU == null ? MklVectorModeChar.NoVector : MklVectorModeChar.Vector, ps, n, pA, lda, pU, ldu, px, out _, null, null);
			}
			info.Check(SolveMethodKind.SchurReorder);
			return true;
		}
		#endregion

		#region linear solve
		/// <inheritdoc/>
		public virtual bool LinearSolveGeneral<T, TS1, TS2>(long n, long nrhs, TS1 A, long lda, TS2 B, long ldb, bool allowDestroy = false) where T : unmanaged, IBinaryFloat<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
		{
			if (!GetPointer(A, n, n, lda, out T* pA))
				return false;
			if (!GetPointer(B, n, nrhs, ldb, out T* pB))
				return false;
			delegate*<MklMatrixLayout, MklInt, MklInt, T*, MklInt, MklInt*, T*, MklInt, MklLapackInfo> func = default(T) switch
			{
				Float32 => &NM.LAPACKE_sgesv,
				Float64 => &NM.LAPACKE_dgesv,
				Complex<Float32> => &NM.LAPACKE_cgesv,
				Complex<Float64> => &NM.LAPACKE_zgesv,
				_ => null
			};
			if (func == null)
				return false;
			using var ipiv = Buffers.Create<MklInt>(n * sizeof(MklInt));
			using var ppA = allowDestroy ? Buffers.Create(pA, lda, n) : Buffers.Create<T>(null, n, n);
			func(MklMatrixLayout.ColMajor, n, nrhs, ppA, ppA.ld, ipiv, pB, ldb).Check(SolveMethodKind.LU);
			return true;
		}
		#endregion

		#region QR solve
		/// <inheritdoc/>
		public virtual bool QRDecomposition<T, TS1, TS2>(bool full, long m, long n, TS1 A, long lda, TS2? Q, long ldq) where T : unmanaged, IBinaryFloat<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
		{
			if (!GetPointer(A, m, n, lda, out T* pA))
				return false;
			var (rowQ, colQ) = (m, n);
			if (m > n && full)
				colQ = m;
			if (m < n)
			{
				colQ = m; full = false;
			}
			long mn = Math.Min(m, n);
			if (!GetPointer(Q, rowQ, colQ, ldq, out T* pQ))
				return false;
			delegate*<MklMatrixLayout, MklInt, MklInt, T*, MklInt, T*, MklLapackInfo> facFunc = default(T) switch
			{
				Float32 => &NM.LAPACKE_sgeqrf,
				Float64 => &NM.LAPACKE_dgeqrf,
				Complex<Float32> => &NM.LAPACKE_cgeqrf,
				Complex<Float64> => &NM.LAPACKE_zgeqrf,
				_ => null
			};
			delegate*<MklMatrixLayout, MklInt, MklInt, MklInt, T*, MklInt, T*, MklLapackInfo> getQFunc = default(T) switch
			{
				Float32 => &NM.LAPACKE_sorgqr,
				Float64 => &NM.LAPACKE_dorgqr,
				Complex<Float32> => &NM.LAPACKE_cungqr,
				Complex<Float64> => &NM.LAPACKE_zungqr,
				_ => null
			};
			if (facFunc == null)
				return false;
			using var tau = Buffers.Create<T>(mn * sizeof(T));
			facFunc(MklMatrixLayout.ColMajor, m, n, pA, lda, tau).Check(SolveMethodKind.QR);
			Storage.Api.PointerMemoryCopy2D(pA, lda, pQ, ldq, m, mn);
			getQFunc(MklMatrixLayout.ColMajor, m, full ? m : mn, mn, pQ, ldq, tau).Check(SolveMethodKind.QR);
			return true;
		}

		/// <inheritdoc/>
		public virtual bool LeastSquareSolve<T, TS1, TS2>(long m, long n, long nrhs, TS1 A, long lda, TS2 B, long ldb, bool allowDestroy = false) where T : unmanaged, IBinaryFloat<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
		{
			if (!GetPointer(A, m, n, lda, out T* pA))
				return false;
			if (!GetPointer(B, n, nrhs, ldb, out T* pB))
				return false;
			delegate*<MklMatrixLayout, MklOperationChar, MklInt, MklInt, MklInt, T*, MklInt, T*, MklInt, MklLapackInfo> func = default(T) switch
			{
				Float32 => &NM.LAPACKE_sgels,
				Float64 => &NM.LAPACKE_dgels,
				Complex<Float32> => &NM.LAPACKE_cgels,
				Complex<Float64> => &NM.LAPACKE_zgels,
				_ => null
			};
			if (func == null)
				return false;
			using var ppA = allowDestroy ? Buffers.Create(pA, lda, n) : Buffers.Create<T>(null, m, n);
			Storage.Api.PointerMemoryCopy2D(pA, lda, ppA, ppA.ld, m, n);
			func(MklMatrixLayout.ColMajor, MklOperationChar.NoneTranspose, m, n, nrhs, ppA, ppA.ld, pB, ldb).Check(SolveMethodKind.QR);
			return true;
		}
		#endregion
	}
}
