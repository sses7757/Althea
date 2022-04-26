using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Althea.Backend.Storage;
using Althea.LinearAlgebra;
using Althea.LinearAlgebra.Dense;
using Althea.NativeTypes;

using NM = Althea.Backend.Mkl.LinearAlgebra.Dense.NativeMethods;


namespace Althea.Backend.Mkl.LinearAlgebra.Dense
{
	internal readonly unsafe ref struct Buffer<T> where T : unmanaged
	{
		internal readonly long ld;
		private readonly T* ptr;
		private readonly bool alloc;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal Buffer(T* org, long ld, long n)
		{
			if (org == null)
			{
				this.ptr = (T*)Marshal.AllocHGlobal((IntPtr)(n * n));
				this.ld = n;
				this.alloc = true;
			}
			else
			{
				this.ptr = org;
				this.ld = ld;
				this.alloc = false;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Dispose()
		{
			if (this.alloc)
				Marshal.FreeHGlobal((IntPtr)this.ptr);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator T*(Buffer<T> buffer) => buffer.ptr;
	}

	public unsafe partial class Api
	{
		#region eigen-problems
		/// <inheritdoc/>
		public virtual bool EigenStandardMatrixHermitian<T, TS1, TS2, TS3>(SolveVectorMode mode, long n, bool upper, TS1 A, long lda, TS2 valOut, TS3? vecOut, long ldvec) where T : unmanaged, IFloatingPoint<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>
		{
			if (!GetPointer(A, n, n, lda, out T* pA))
				return false;
			if (!GetPointer(vecOut, n, n, ldvec, out T* pV))
				return false;
			if (!GetPointer(valOut, 1, out T* px, out long nx))
				return false;
			if (nx < n)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(valOut));
			delegate*<MklMatrixLayout, MklVectorModeChar, MklFillModeChar, long, T*, long, T*, MklLapackInfo> func = default(T) switch
			{
				float => &NM.LAPACKE_ssyev,
				double => &NM.LAPACKE_dsyev,
				Complex<float> => &NM.LAPACKE_cheev,
				Complex<double> => &NM.LAPACKE_zheev,
				_ => null
			};
			if (func == null)
				return false;
			using var ppV = new Buffer<T>(pV, ldvec, n);
			Storage.Api.PointerMemoryCopy2D(pA, lda, ppV, ppV.ld, n, n);
			func(MklMatrixLayout.ColMajor, mode.ToChar(), upper ? MklFillModeChar.Upper : MklFillModeChar.Lower, n, ppV, ldvec, px).Check(SolveMethodKind.Eigenvalue);
			return true;
		}

		/// <inheritdoc/>
		public virtual bool EigenGeneralMatrixHermitian<T, TS1, TS2, TS3>(GeneralEigenType type, SolveVectorMode mode, long n, bool upper, TS1 A, long lda, TS1 B, long ldb, TS2 valOut, TS3? vecOut, long ldvec) where T : unmanaged, IFloatingPoint<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>;

		/// <inheritdoc/>
		public virtual bool EigenStandardMatrixGeneral<T, TS1, TS2, TS3, TS4>(SolveVectorMode mode, long n, TS1 A, long lda, TS2 valOut, TS2? valImagOut, TS3? leftVec, long ldvl, TS4? rightVec, long ldvr) where T : unmanaged, IFloatingPoint<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3> where TS4 : class, IStorage<T, TS4>;

		/// <inheritdoc/>
		public virtual bool EigenGeneralMatrixGeneral<T, TS1, TS2, TS3, TS4>(GeneralEigenType type, SolveVectorMode mode, long n, TS1 A, long lda, TS1 B, long ldb, TS2 valOut, TS2? valImagOut, TS2 valDenomOut, TS3? leftVec, long ldvl, TS4? rightVec, long ldvr) where T : unmanaged, IFloatingPoint<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3> where TS4 : class, IStorage<T, TS4>;
		#endregion

		#region other decompositions
		/// <inheritdoc/>
		public virtual bool SingularValues<T, TS1, TS2, TS3, TS4>(SVDStore storeU, SVDStore storeV, long m, long n, TS1 A, long lda, TS2? U, long ldu, TS3? Vct, long ldvct, TS4 S) where T : unmanaged, IFloatingPoint<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3> where TS4 : class, IStorage<T, TS4>;

		/// <inheritdoc/>
		public virtual bool StandardSchurDecomposition<T, TS1, TS2, TS3>(SolveVectorMode mode, long n, TS1 A, long lda, TS2? U, long ldu, TS3 valOut, TS3? valImagOut) where T : unmanaged, IFloatingPoint<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>;

		/// <inheritdoc/>
		public virtual bool StandardSchurReorder<T, TInd, TS1, TS2, TSInd>(long n, TS1 A, long lda, TS2? U, long ldu, TSInd order) where T : unmanaged, IFloatingPoint<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TInd : unmanaged, IBinaryInteger<TInd> where TSInd : class, IStorage<TInd, TSInd>;

		/// <inheritdoc/>
		public virtual bool GeneralSchurDecomposition<T, TS1, TS2, TS3, TS4>(SolveVectorMode mode, long n, TS1 A, long lda, TS1 B, long ldb, TS2? Ul, long ldul, TS4? Ur, long ldur, TS4 valOut, TS4? valImagOut, TS4 valDenomOut) where T : unmanaged, IFloatingPoint<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3> where TS4 : class, IStorage<T, TS4>;

		/// <inheritdoc/>
		public virtual bool GeneralSchurReorder<T, TInd, TS1, TS2, TS3, TSInd>(long n, TS1 A, long lda, TS1 B, long ldb, TS2? Ul, long ldul, TS3? Ur, long ldur, TSInd order) where T : unmanaged, IFloatingPoint<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3> where TInd : unmanaged, IBinaryInteger<TInd> where TSInd : class, IStorage<TInd, TSInd>;
		#endregion

		#region linear solve
		/// <inheritdoc/>
		public virtual bool LinearSolveGeneral<T, TS1, TS2>(MatrixOperation op, long n, long nrhs, TS1 A, long lda, TS2 B, long ldb) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;
		#endregion

		#region QR solve
		/// <inheritdoc/>
		public virtual bool QRDecomposition<T, TS1, TS2>(bool full, long m, long n, TS1 A, long lda, TS2? Q, long ldq) where T : unmanaged, IFloatingPoint<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

		/// <inheritdoc/>
		public virtual bool LeastSquareSolve<T, TS1, TS2>(long m, long n, long nrhs, TS1 A, long lda, TS2 B, long ldb) where T : unmanaged, IFloatingPoint<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;
		#endregion
	}
}
