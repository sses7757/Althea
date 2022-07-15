using System.Runtime.CompilerServices;
using System.Threading;

using Althea.Helpers;
using Althea.LinearAlgebra;

using NM = Althea.Backend.Mkl.LinearAlgebra.Dense.NativeMethods;
using NMC = Althea.Backend.Mkl.LinearAlgebra.Dense.CustomNativeMethods;
using NMT = Althea.Backend.Mkl.LinearAlgebra.Dense.NativeMethodsTemplate;


namespace Althea.Backend.Mkl.LinearAlgebra.Dense
{
	public unsafe partial class Api
	{
		#region BLAS level 1
		/// <summary>
		/// Get the index of the element with horizontal maximum/minimum absolute value (<c>abs(x[i].real) + abs(x[i].imag)</c>) in <paramref name="x"/>
		/// </summary>
		/// <typeparam name="T">Any complex data type</typeparam>
		/// <typeparam name="TS">The storage type</typeparam>
		/// <param name="max">Whether to get the maximum or minimum</param>
		/// <param name="x">The vector to get maximum absolute value's index</param>
		/// <param name="strideX">The spacing between consecutive elements of <paramref name="x"/></param>
		/// <param name="index">The output real index in <paramref name="x"/></param>
		/// <returns>Support or not</returns>
		internal protected static bool HorizontalAbsoluteValueArgMinMax<T, TS>(bool max, TS x, int strideX, out long index) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
		{
			index = -1;
			if (!T.IsComplexType)
				throw new TypeMismatchException(typeof(T), TypeMismatchException.MismatchReason.NotComplex);
			if (!GetPointer(x, strideX, out T* px, out long n))
				return false;
			delegate*<MklInt, T*, MklInt, ulong> func = default(T) switch
			{
				Complex<Float32> => max ? &NM.cblas_icamax : &NM.cblas_icamin,
				Complex<Float64> => max ? &NM.cblas_izamax : &NM.cblas_izamin,
				_ => null,
			};
			if (func is null)
				return false;
			index = (long)func(n, px, strideX) - 1;
			return true;
		}

		/// <summary>
		/// Sum the absolute values (<c>abs(x[i].real) + abs(x[i].imag)</c>) of vector <paramref name="x"/>'s all elements
		/// </summary>
		/// <typeparam name="T">Any complex data type</typeparam>
		/// <typeparam name="TS">The storage type</typeparam>
		/// <param name="x">The vector to be summed</param>
		/// <param name="strideX">The spacing between consecutive elements of <paramref name="x"/></param>
		/// <param name="sum">Output the sum as a <typeparamref name="T"/></param>
		/// <returns>Support or not</returns>
		internal protected static bool HorizontalAbsoluteSum<T, TS>(TS x, int strideX, out T sum) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
		{
			sum = T.Zero;
			T result = T.Zero;
			if (!T.IsComplexType)
				throw new TypeMismatchException(typeof(T), TypeMismatchException.MismatchReason.NotComplex);
			if (!GetPointer(x, strideX, out T* px, out long n))
				return false;
			if (T.Type == DataType.ComplexSingle)
				*(float*)&result = NM.cblas_scasum(n, px, strideX);
			else if (T.Type == DataType.ComplexDouble)
				*(double*)&result = NM.cblas_dzasum(n, px, strideX);
			else
				return false;
			sum = result;
			return true;
		}

		/// <inheritdoc/>
		public virtual bool AbsoluteValueArgMax<T, TS>(TS x, long strideX, out long index) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
		{
			index = -1;
			if (!GetPointer(x, strideX, out T* px, out long n))
				return false;
			delegate*<MklInt, T*, MklInt, ulong> func = null;
			if (typeof(T) == typeof(Float32))
				func = &NM.cblas_isamax;
			if (typeof(T) == typeof(Float64))
				func = &NM.cblas_idamax;
			if (func != null)
			{
				index = (long)func(n, px, strideX);
				return true;
			}
			return NMC.vecArgAbsMax(T.Type, n, px, strideX, out index).Check();
		}

		/// <inheritdoc/>
		public virtual bool AbsoluteValueArgMin<T, TS>(TS x, long strideX, out long index) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
		{
			index = -1;
			if (!GetPointer(x, strideX, out T* px, out long n))
				return false;
			delegate*<MklInt, T*, MklInt, ulong> func = default(T) switch
			{
				Float32 => &NM.cblas_isamin,
				Float64 => &NM.cblas_idamin,
				_ => null,
			};
			if (func != null)
			{
				index = (long)func(n, px, strideX);
				return true;
			}
			return NMC.vecArgAbsMin(T.Type, n, px, strideX, out index).Check();
		}

		/// <inheritdoc/>
		public virtual bool AbsoluteValueSum<T, TS>(TS x, long strideX, out T sum) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
		{
			sum = default; T result = T.Zero;
			if (!GetPointer(x, strideX, out T* px, out long n))
				return false;
			if (typeof(T) == typeof(Float32))
			{
				*(float*)&result = NM.cblas_sasum(n, px, strideX);
			}
			else if (typeof(T) == typeof(Float64))
			{
				*(double*)&result = NM.cblas_dasum(n, px, strideX);
			}
			else if (NMC.vecAbsSum(T.Type, n, px, strideX, &result) != CustomStatus.Success)
				return false;
			sum = result;
			return true;
		}

		/// <inheritdoc/>
		public virtual bool Norm<T, TS>(TS x, long strideX, out T norm) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
		{
			norm = default; T result = T.Zero;
			if (!GetPointer(x, strideX, out T* px, out long n))
				return false;
			if (typeof(T) == typeof(Float32))
				*(float*)&result = NM.cblas_snrm2(n, px, strideX);
			else if (typeof(T) == typeof(Float64))
				*(double*)&result = NM.cblas_dnrm2(n, px, strideX);
			else if (typeof(T) == typeof(Complex<Float32>))
				*(float*)&result = NM.cblas_scnrm2(n, px, strideX);
			else if (typeof(T) == typeof(Complex<Float64>))
				*(double*)&result = NM.cblas_dznrm2(n, px, strideX);
			else
				return false;
			norm = result;
			return true;
		}

		/// <inheritdoc/>
		public virtual bool Scale<T, TS>(TS x, long strideX, T scalar) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
		{
			if (!GetPointer(x, strideX, out T* px, out long n))
				return false;
			var funcRe = default(T) switch
			{
				Float32 => new NM.cblas_scal<Float32>(NM.cblas_sscal) as NM.cblas_scal<T>,
				Float64 => new NM.cblas_scal<Float64>(NM.cblas_dscal) as NM.cblas_scal<T>,
				_ => null,
			};
			var funcCm = default(T) switch
			{
				Complex<Float32> => new NM.cblas_scal_comp<Complex<Float32>>(NM.cblas_cscal) as NM.cblas_scal_comp<T>,
				Complex<Float64> => new NM.cblas_scal_comp<Complex<Float64>>(NM.cblas_zscal) as NM.cblas_scal_comp<T>,
				_ => null,
			};
			funcRe?.Invoke(n, scalar, px, strideX);
			funcCm?.Invoke(n, scalar, px, strideX);
			return funcRe != null || funcCm != null;
		}

		/// <inheritdoc/>
		public virtual bool Add<T, TS1, TS2>(T α, TS1 x, long strideX, TS2 y, long strideY) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
		{
			if (!GetPointer(x, strideX, out T* px, out long n))
				return false;
			if (!GetPointer(y, strideY, out T* py, out long n2))
				return false;
			if (n != n2)
				throw new ArgumentException(Resources.ParameterError.NotSameSize);
			var funcRe = default(T) switch
			{
				Float32 => new NM.cblas_axpy<Float32>(NM.cblas_saxpy) as NM.cblas_axpy<T>,
				Float64 => new NM.cblas_axpy<Float64>(NM.cblas_daxpy) as NM.cblas_axpy<T>,
				_ => null,
			};
			var funcCm = default(T) switch
			{
				Complex<Float32> => new NM.cblas_axpy_comp<Complex<Float32>>(NM.cblas_caxpy) as NM.cblas_axpy_comp<T>,
				Complex<Float64> => new NM.cblas_axpy_comp<Complex<Float64>>(NM.cblas_zaxpy) as NM.cblas_axpy_comp<T>,
				_ => null,
			};
			funcRe?.Invoke(n, α, px, strideX, py, strideY);
			funcCm?.Invoke(n, α, px, strideX, py, strideY);
			return funcRe != null || funcCm != null;
		}

		/// <inheritdoc/>
		public virtual bool Dot<T, TS1, TS2>(bool conjX, TS1 x, long strideX, TS2 y, long strideY, out T dot) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
		{
			dot = default;
			if (!GetPointer(x, strideX, out T* px, out long n))
				return false;
			if (!GetPointer(y, strideY, out T* py, out long n2))
				return false;
			if (n != n2)
				throw new ArgumentException(Resources.ParameterError.NotSameSize);
			var funcRe = default(T) switch
			{
				Float32 => new NMT.cblas_dot<Float32>(NM.cblas_sdot) as NMT.cblas_dot<T>,
				Float64 => new NMT.cblas_dot<Float64>(NM.cblas_ddot) as NMT.cblas_dot<T>,
				_ => null,
			};
			var funcCm = default(T) switch
			{
				Complex<Float32> => (conjX ? new NMT.cblas_dot_comp<Complex<Float32>>(NM.cblas_cdotc_sub) : new NMT.cblas_dot_comp<Complex<Float32>>(NM.cblas_cdotu_sub)) as NMT.cblas_dot_comp<T>,
				Complex<Float64> => (conjX ? new NMT.cblas_dot_comp<Complex<Float64>>(NM.cblas_zdotc_sub) : new NMT.cblas_dot_comp<Complex<Float64>>(NM.cblas_zdotu_sub)) as NMT.cblas_dot_comp<T>,
				_ => null,
			};
			dot = funcRe?.Invoke(n, px, strideX, py, strideY) ?? dot;
			funcCm?.Invoke(n, px, strideX, py, strideY, out dot);
			return funcRe != null || funcCm != null;
		}
		#endregion

		#region BLAS level 2
		/// <inheritdoc/>
		public virtual bool GeneralMatrixMultiplyVector<T, TSM, TSV1, TSV2>(MatrixOperation op, long m, long n, T α, TSM A, long lda, TSV1 x, long strideX, T β, TSV2 y, long strideY) where T : unmanaged, IBaseNumber<T> where TSM : class, IStorage<T, TSM> where TSV1 : class, IStorage<T, TSV1> where TSV2 : class, IStorage<T, TSV2>
		{
			if (!GetPointer(x, strideX, out T* px, out _))
				return false;
			if (!GetPointer(y, strideY, out T* py, out _))
				return false;
			if (!GetPointer(A, m, n, lda, out T* pA))
				return false;
			if (px == py && strideX != strideY)
				return false;
			op = op.Simplify<T>();
			var funcRe = default(T) switch
			{
				Float32 => new NM.cblas_gemv<Float32>(NM.cblas_sgemv) as NM.cblas_gemv<T>,
				Float64 => new NM.cblas_gemv<Float64>(NM.cblas_dgemv) as NM.cblas_gemv<T>,
				_ => null,
			};
			var funcCm = default(T) switch
			{
				Complex<Float32> => new NM.cblas_gemv_comp<Complex<Float32>>(NM.cblas_cgemv) as NM.cblas_gemv_comp<T>,
				Complex<Float64> => new NM.cblas_gemv_comp<Complex<Float64>>(NM.cblas_zgemv) as NM.cblas_gemv_comp<T>,
				_ => null,
			};
			using var conj = Conjugater.Create(px, op.CanInPlace() ? n : m, strideX, py, op.CanInPlace() ? m : n, strideY, ref op);
			funcRe?.Invoke(MklMatrixLayout.ColMajor, op.ToMkl(), m, n, α, pA, lda, px, strideX, β, py, strideY);
			funcCm?.Invoke(MklMatrixLayout.ColMajor, op.ToMkl(), m, n, α, pA, lda, px, strideX, β, py, strideY);
			return funcRe != null || funcCm != null;
		}

		/// <inheritdoc/>
		public virtual bool SymmetricMatrixMultiplyVector<T, TSM, TSV1, TSV2>(bool fillUpper, bool hermA, long n, T α, TSM A, long lda, TSV1 x, long strideX, T β, TSV2 y, long strideY) where T : unmanaged, IBaseNumber<T> where TSM : class, IStorage<T, TSM> where TSV1 : class, IStorage<T, TSV1> where TSV2 : class, IStorage<T, TSV2>
		{
			if (!GetPointer(x, strideX, out T* px, out _))
				return false;
			if (!GetPointer(y, strideY, out T* py, out _))
				return false;
			if (!GetPointer(A, n, n, lda, out T* pA))
				return false;
			if (px == py && strideX != strideY)
				return false;
			var funcRe = default(T) switch
			{
				Float32 => new NM.cblas_symv<Float32>(NM.cblas_ssymv) as NM.cblas_symv<T>,
				Float64 => new NM.cblas_symv<Float64>(NM.cblas_dsymv) as NM.cblas_symv<T>,
				_ => null,
			};
			var funcCm = default(T) switch
			{
				Complex<Float32> => (hermA ? new NM.cblas_symv_comp<Complex<Float32>>(NM.cblas_chemv) : new NM.cblas_symv_comp<Complex<Float32>>(NM.cblas_csymv)) as NM.cblas_symv_comp<T>,
				Complex<Float64> => (hermA ? new NM.cblas_symv_comp<Complex<Float64>>(NM.cblas_zhemv) : new NM.cblas_symv_comp<Complex<Float64>>(NM.cblas_zsymv)) as NM.cblas_symv_comp<T>,
				_ => null,
			};
			var fu = fillUpper ? MklFillMode.Upper : MklFillMode.Lower;
			funcRe?.Invoke(MklMatrixLayout.ColMajor, fu, n, α, pA, lda, px, strideX, β, py, strideY);
			funcCm?.Invoke(MklMatrixLayout.ColMajor, fu, n, α, pA, lda, px, strideX, β, py, strideY);
			return funcRe != null || funcCm != null;
		}

		/// <inheritdoc/>
		public virtual bool TriangularMatrixMultiplyVector<T, TSM, TSV1, TSV2>(bool fillUpper, bool unitDiag, MatrixOperation op, long m, long n, TSM A, long lda, T α, TSV1 x, long strideX, T β, TSV2 y, long strideY) where T : unmanaged, IBaseNumber<T> where TSM : class, IStorage<T, TSM> where TSV1 : class, IStorage<T, TSV1> where TSV2 : class, IStorage<T, TSV2>
		{
			if (!GetPointer(x, strideX, out T* px, out _))
				return false;
			if (!GetPointer(y, strideY, out T* py, out _))
				return false;
			if (!GetPointer(A, m, n, lda, out T* pA))
				return false;
			if (β != T.Zero || (px == py && (strideX != strideY || m != n)))
				return false;
			op = op.Simplify<T>();
			delegate*<MklMatrixLayout, MklFillMode, MklOperation, MklBlasDiagType, MklInt, T*, MklInt, T*, MklInt, void> func = default(T) switch
			{
				Float32 => &NM.cblas_strmv,
				Float64 => &NM.cblas_dtrmv,
				Complex<Float32> => &NM.cblas_ctrmv,
				Complex<Float64> => &NM.cblas_ztrmv,
				_ => null,
			};
			if (func == null)
				return false;
			var fu = fillUpper ? MklFillMode.Upper : MklFillMode.Lower;
			var ud = unitDiag ? MklBlasDiagType.Unit : MklBlasDiagType.NonUnit;
			if (px == py)
			{   // x = alpha * op(A) * x
				using var conj = Conjugater.Create(px, n, strideX, ref op);
				func(MklMatrixLayout.ColMajor, fu, op.ToMkl(), ud, n, pA, lda, px, strideX);
				if (α != T.One)
					this.Scale(x, strideX, α);
			}
			else
			{
				long min = Math.Min(m, n), max = Math.Max(m, n);
				Storage.Api.PointerStridedCopy(px, strideX, py, strideY, Math.Min(m, n));
				bool actualSquare = op.CanInPlace() ? ((m > n) == fillUpper) : ((n > m) == !fillUpper);
				using var conj = Conjugater.Create(py, actualSquare ? min : m, strideY, ref op);
				func(MklMatrixLayout.ColMajor, fu, op.ToMkl(), ud, min, pA, lda, py, strideY);
				if (actualSquare)
				{
					if (op.CanInPlace() == fillUpper)
						FillWithValue(py + min * strideY, strideY, n, T.Zero);
				}
				else
				{
					if (op.CanInPlace() == fillUpper)
						this.GeneralMatrixMultiplyVector(op, max - min, min, T.One, A + min * (op.CanInPlace() ? 1 : lda), lda, y + min * strideY, strideY, T.Zero, y + min * strideY, strideY);
					else
						this.GeneralMatrixMultiplyVector(op, min, max - min, T.One, A + min * (op.CanInPlace() ? lda : 1), lda, x + min * strideX, strideX, T.One, y, strideY);
				}
				if (α != T.One)
					this.Scale(y, strideY, α);
			}
			return true;
		}

		/// <inheritdoc/>
		public virtual bool GeneralRankOneUpdate<T, TSM, TSV1, TSV2>(bool conjY, long m, long n, T α, TSV1 x, long strideX, TSV2 y, long strideY, T β, TSM A, long lda) where T : unmanaged, IBaseNumber<T> where TSM : class, IStorage<T, TSM> where TSV1 : class, IStorage<T, TSV1> where TSV2 : class, IStorage<T, TSV2>
		{
			if (!GetPointer(x, strideX, out T* px, out _))
				return false;
			if (!GetPointer(y, strideY, out T* py, out _))
				return false;
			if (!GetPointer(A, m, n, lda, out T* pA))
				return false;
			if (β != T.One)
				return false;
			var funcRe = default(T) switch
			{
				Float32 => new NMT.cblas_ger<Float32>(NM.cblas_sger) as NMT.cblas_ger<T>,
				Float64 => new NMT.cblas_ger<Float64>(NM.cblas_dger) as NMT.cblas_ger<T>,
				_ => null,
			};
			var funcCm = default(T) switch
			{
				Complex<Float32> => (conjY ? new NMT.cblas_ger_comp<Complex<Float32>>(NM.cblas_cgerc) : new NMT.cblas_ger_comp<Complex<Float32>>(NM.cblas_cgeru)) as NMT.cblas_ger_comp<T>,
				Complex<Float64> => (conjY ? new NMT.cblas_ger_comp<Complex<Float64>>(NM.cblas_zgerc) : new NMT.cblas_ger_comp<Complex<Float64>>(NM.cblas_zgeru)) as NMT.cblas_ger_comp<T>,
				_ => null,
			};
			funcRe?.Invoke(MklMatrixLayout.ColMajor, m, n, α, px, strideX, py, strideY, pA, lda);
			funcCm?.Invoke(MklMatrixLayout.ColMajor, m, n, α, px, strideX, py, strideY, pA, lda);
			return funcRe != null || funcCm != null;
		}

		/// <inheritdoc/>
		public virtual bool SymmetricRankOneUpdate<T, TSM, TSV>(bool fillUpper, bool conjX, long n, T α, TSV x, long strideX, T β, TSM A, long lda) where T : unmanaged, IBaseNumber<T> where TSM : class, IStorage<T, TSM> where TSV : class, IStorage<T, TSV>
		{
			if (!GetPointer(x, strideX, out T* px, out _))
				return false;
			if (!GetPointer(A, n, n, lda, out T* pA))
				return false;
			if (T.IsComplexType && !conjX)
				return false;
			var funcRe = default(T) switch
			{
				Float32 => new NM.cblas_syr<Float32>(NM.cblas_ssyr) as NM.cblas_syr<T>,
				Float64 => new NM.cblas_syr<Float64>(NM.cblas_dsyr) as NM.cblas_syr<T>,
				_ => null,
			};
			var funcCm = default(T) switch
			{
				Complex<Float32> => new NM.cblas_her_comp<Complex<Float32>>(NM.cblas_cher) as NM.cblas_her_comp<T>,
				Complex<Float64> => new NM.cblas_her_comp<Complex<Float64>>(NM.cblas_zher) as NM.cblas_her_comp<T>,
				_ => null,
			};
			funcRe?.Invoke(MklMatrixLayout.ColMajor, fillUpper ? MklFillMode.Upper : MklFillMode.Lower, n, α, px, strideX, pA, lda);
			funcCm?.Invoke(MklMatrixLayout.ColMajor, fillUpper ? MklFillMode.Upper : MklFillMode.Lower, n, α, px, strideX, pA, lda);
			return funcRe != null || funcCm != null;
		}

		/// <inheritdoc/>
		public virtual bool SymmetricRankTwoUpdate<T, TSM, TSV1, TSV2>(bool fillUpper, bool conjugate, long n, T α, TSV1 x, long strideX, TSV2 y, long strideY, T β, TSM A, long lda) where T : unmanaged, IBaseNumber<T> where TSM : class, IStorage<T, TSM> where TSV1 : class, IStorage<T, TSV1> where TSV2 : class, IStorage<T, TSV2>
		{
			if (!GetPointer(x, strideX, out T* px, out _))
				return false;
			if (!GetPointer(y, strideY, out T* py, out _))
				return false;
			if (!GetPointer(A, n, n, lda, out T* pA))
				return false;
			if (T.IsComplexType && !conjugate)
				return false;
			var funcRe = default(T) switch
			{
				Float32 => new NM.cblas_syr2<Float32>(NM.cblas_ssyr2) as NM.cblas_syr2<T>,
				Float64 => new NM.cblas_syr2<Float64>(NM.cblas_dsyr2) as NM.cblas_syr2<T>,
				_ => null,
			};
			var funcCm = default(T) switch
			{
				Complex<Float32> => new NM.cblas_her2_comp<Complex<Float32>>(NM.cblas_cher2) as NM.cblas_her2_comp<T>,
				Complex<Float64> => new NM.cblas_her2_comp<Complex<Float64>>(NM.cblas_zher2) as NM.cblas_her2_comp<T>,
				_ => null,
			};
			var fu = fillUpper ? MklFillMode.Upper : MklFillMode.Lower;
			funcRe?.Invoke(MklMatrixLayout.ColMajor, fu, n, α, px, strideX, py, strideY, pA, lda);
			funcCm?.Invoke(MklMatrixLayout.ColMajor, fu, n, α, px, strideX, py, strideY, pA, lda);
			return funcRe != null || funcCm != null;
		}
		#endregion

		#region BLAS level 3
		/// <inheritdoc/>
		public virtual bool GeneralMatricesMultiply<T, TS1, TS2, TS3>(MatrixOperation opA, MatrixOperation opB, long m, long n, long k, T α, TS1 A, long lda, TS2 B, long ldb, T β, TS3 C, long ldc) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>
		{
			if (α == T.Zero)
				throw new ArgumentOutOfRangeException(nameof(α), Resources.ParameterError.CannotZero);
			opA = opA.Simplify<T>(); opB = opB.Simplify<T>();
			if (!GetPointer(A, opA.CanInPlace() ? m : k, opA.CanInPlace() ? k : m, lda, out T* pA))
				return false;
			if (!GetPointer(B, opB.CanInPlace() ? k : n, opB.CanInPlace() ? n : k, ldb, out T* pB))
				return false;
			if (!GetPointer(C, m, n, ldc, out T* pC))
				return false;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			bool NoJitGemm()
			{
				var funcRe = default(T) switch
				{
					Float32 => new NM.cblas_gemm<Float32>(NM.cblas_sgemm) as NM.cblas_gemm<T>,
					Float64 => new NM.cblas_gemm<Float64>(NM.cblas_dgemm) as NM.cblas_gemm<T>,
					_ => null,
				};
				var funcCm = default(T) switch
				{
					Complex<Float32> when !this.ComplexGemmUseGemm3M => new NM.cblas_gemm_comp<Complex<Float32>>(NM.cblas_cgemm) as NM.cblas_gemm_comp<T>,
					Complex<Float32> when this.ComplexGemmUseGemm3M => new NM.cblas_gemm_comp<Complex<Float32>>(NM.cblas_cgemm3m) as NM.cblas_gemm_comp<T>,
					Complex<Float64> when !this.ComplexGemmUseGemm3M => new NM.cblas_gemm_comp<Complex<Float64>>(NM.cblas_zgemm) as NM.cblas_gemm_comp<T>,
					Complex<Float64> when this.ComplexGemmUseGemm3M => new NM.cblas_gemm_comp<Complex<Float64>>(NM.cblas_zgemm3m) as NM.cblas_gemm_comp<T>,
					_ => null,
				};
				if (opA == MatrixOperation.Conjugate && opB == MatrixOperation.Conjugate)
				{
					funcRe?.Invoke(MklMatrixLayout.ColMajor, opA.ToMkl(), opB.ToMkl(), m, n, k, α, pA, lda, pB, ldb, β, pC, ldc);
					funcCm?.Invoke(MklMatrixLayout.ColMajor, opA.ToMkl(), opB.ToMkl(), m, n, k, α, pA, lda, pB, ldb, β, pC, ldc);
					Conjugater.Conjugate(pC, m, n, ldc);
				}
				else
				{
					using var conjA = Conjugater.Create(pA, opA.CanInPlace() ? m : k, opA.CanInPlace() ? k : m, lda, ref opA);
					using var conjB = Conjugater.Create(pB, opB.CanInPlace() ? k : n, opB.CanInPlace() ? n : k, ldb, ref opB);
					funcRe?.Invoke(MklMatrixLayout.ColMajor, opA.ToMkl(), opB.ToMkl(), m, n, k, α, pA, lda, pB, ldb, β, pC, ldc);
					funcCm?.Invoke(MklMatrixLayout.ColMajor, opA.ToMkl(), opB.ToMkl(), m, n, k, α, pA, lda, pB, ldb, β, pC, ldc);
				}
				return funcRe != null || funcCm != null;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			bool JitGemm((MatrixOperation opA, MatrixOperation opB, long m, long n, long k, Complex<Float64> α, Complex<Float64> β, long lda, long ldb, long ldc) key, (IntPtr jitter, ReaderWriterLockSlim locker) jit)
			{
				// compute
				jit.locker.EnterReadLock();
				try
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
						return false;
					var func = getGemmFunc(jit.jitter);
					func(jit.jitter, pA, pB, pC);
				}
				finally
				{
					jit.locker.ExitReadLock();
				}
				// dispose old if necessary
				lock (this)
				{
					this.compiledQueue.Enqueue(key);
					if (this.compiledQueue.Count >= this.GemmJitSize)
					{
						key = this.compiledQueue.Dequeue();
						this.compiled.Remove(key, out jit);
						jit.locker.EnterWriteLock();
						try
						{
							var err = NM.mkl_jit_destroy(jit.jitter);
							if (err != MklJitStatus.Success)
								throw new StatusException(err);
						}
						finally
						{
							jit.locker.ExitWriteLock();
							jit.locker.Dispose();
						}
					}
				}
				return true;
			}

			if (!this.GemmJitCache || opA == MatrixOperation.Conjugate || opB == MatrixOperation.Conjugate)
			{
				return NoJitGemm();
			}
			else
			{
				var key = (opA, opB, m, n, k, α.As<T, Complex<Float64>>(), β.As<T, Complex<Float64>>(), lda, ldb, ldc);
				if (this.candidates.TryGetValue(key, out int hitCount))
					this.candidates[key] = ++hitCount;
				else if (!this.compiled.TryGetValue(key, out var jit))
				{
					this.candidatesQueue.Enqueue(key);
					this.candidates[key] = hitCount = 1;
					if (this.candidatesQueue.Count >= this.GemmJitCandidateSize)
					{
						var keyNew = this.candidatesQueue.Dequeue();
						this.candidates.Remove(keyNew);
					}
				}
				else
					return JitGemm(key, jit);
				if (hitCount < this.GemmJitThreshold)
					return NoJitGemm();
				// compile
				var funcRe = default(T) switch
				{
					Float32 => new NM.mkl_jit_create_gemm<Float32>(NM.mkl_jit_create_sgemm) as NM.mkl_jit_create_gemm<T>,
					Float64 => new NM.mkl_jit_create_gemm<Float64>(NM.mkl_jit_create_dgemm) as NM.mkl_jit_create_gemm<T>,
					_ => null,
				};
				var funcCm = default(T) switch
				{
					Complex<Float32> => new NM.mkl_jit_create_gemm_comp<Complex<Float32>>(NM.mkl_jit_create_cgemm) as NM.mkl_jit_create_gemm_comp<T>,
					Complex<Float64> => new NM.mkl_jit_create_gemm_comp<Complex<Float64>>(NM.mkl_jit_create_zgemm) as NM.mkl_jit_create_gemm_comp<T>,
					_ => null,
				};
				if (funcRe == null && funcCm == null)
					return false;
				IntPtr jitter = default;
				funcRe?.Invoke(out jitter, MklMatrixLayout.ColMajor, opA.ToMkl(), opB.ToMkl(), m, n, k, α, lda, ldb, β, ldc);
				funcCm?.Invoke(out jitter, MklMatrixLayout.ColMajor, opA.ToMkl(), opB.ToMkl(), m, n, k, α, lda, ldb, β, ldc);
				this.candidates.Remove(key);
				var jitNew = (jitter, new ReaderWriterLockSlim());
				return JitGemm(key, jitNew);
			}
		}

		/// <inheritdoc/>
		public virtual bool SymmetricMatrixMultiplyGeneral<T, TS1, TS2, TS3>(bool fillUpper, bool leftA, bool hermA, MatrixOperation opA, MatrixOperation opB, long m, long n, T α, TS1 A, long lda, TS2 B, long ldb, T β, TS3 C, long ldc) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>
		{
			if (α == T.Zero)
				throw new ArgumentOutOfRangeException(nameof(α), Resources.ParameterError.CannotZero);
			opA = opA.Simplify<T>(hermA); opB = opB.Simplify<T>();
			if (!GetPointer(A, leftA ? m : n, leftA ? m : n, lda, out T* pA))
				return false;
			if (!GetPointer(B, opB.CanInPlace() ? m : n, opB.CanInPlace() ? n : m, ldb, out T* pB))
				return false;
			if (!GetPointer(C, m, n, ldc, out T* pC))
				return false;
			var funcRe = default(T) switch
			{
				Float32 => new NM.cblas_symm<Float32>(NM.cblas_ssymm) as NM.cblas_symm<T>,
				Float64 => new NM.cblas_symm<Float64>(NM.cblas_dsymm) as NM.cblas_symm<T>,
				_ => null,
			};
			var funcCm = default(T) switch
			{
				Complex<Float32> when hermA => new NM.cblas_symm_comp<Complex<Float32>>(NM.cblas_chemm) as NM.cblas_symm_comp<T>,
				Complex<Float32> when !hermA => new NM.cblas_symm_comp<Complex<Float32>>(NM.cblas_csymm) as NM.cblas_symm_comp<T>,
				Complex<Float64> when hermA => new NM.cblas_symm_comp<Complex<Float64>>(NM.cblas_zhemm) as NM.cblas_symm_comp<T>,
				Complex<Float64> when !hermA => new NM.cblas_symm_comp<Complex<Float64>>(NM.cblas_zsymm) as NM.cblas_symm_comp<T>,
				_ => null,
			};
			if (funcRe == null && funcCm == null)
				return false;
			var side = leftA ? MklBlasSideMode.Left : MklBlasSideMode.Right;
			var uplo = fillUpper ? MklFillMode.Upper : MklFillMode.Lower;
			if (!opB.CanInPlace())
			{
				if (m != ldc)
					return false;
				// pre
				opA = (opB == MatrixOperation.Transpose ? opA.Transpose() : opA.Conjugate().Transpose()).Simplify<T>(hermA);
				using var conjA = Conjugater.Create(pA, leftA ? m : n, leftA ? m : n, lda, ref opA);
				// multiply
				funcRe?.Invoke(MklMatrixLayout.ColMajor, side, uplo, m, n, α, pA, lda, pB, ldb, β, pC, ldc);
				funcCm?.Invoke(MklMatrixLayout.ColMajor, side, uplo, m, n, α, pA, lda, pB, ldb, β, pC, ldc);
				// post
				Conjugater.Transpose(pC, m, n, ldc, opB);
			}
			else if (opA == MatrixOperation.Conjugate)
			{   // T is complex
				// pre
				if (opB == MatrixOperation.None)
					Conjugater.Conjugate(pA, leftA ? m : n, leftA ? m : n, lda);
				// multiply
				funcCm?.Invoke(MklMatrixLayout.ColMajor, side, uplo, m, n, α, pA, lda, pB, ldb, β, pC, ldc);
				// post
				if (opB == MatrixOperation.None)
					Conjugater.Conjugate(pA, leftA ? m : n, leftA ? m : n, lda);
				else
					Conjugater.Conjugate(pC, m, n, ldc);
			}
			else
			{
				using var conjB = Conjugater.Create(pA, opB.CanInPlace() ? m : n, opB.CanInPlace() ? n : m, ldb, ref opB);
				funcRe?.Invoke(MklMatrixLayout.ColMajor, side, uplo, m, n, α, pA, lda, pB, ldb, β, pC, ldc);
				funcCm?.Invoke(MklMatrixLayout.ColMajor, side, uplo, m, n, α, pA, lda, pB, ldb, β, pC, ldc);
			}
			return true;
		}

		/// <inheritdoc/>
		public virtual bool TriangularMatrixSolve<T, TS1, TS2>(bool leftA, bool fillUpper, bool unitDiag, MatrixOperation op, long m, long n, T α, TS1 A, long lda, TS2 B, long ldb) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
		{
			if (α == T.Zero)
				throw new ArgumentOutOfRangeException(nameof(α), Resources.ParameterError.CannotZero);
			op = op.Simplify<T>();
			if (!GetPointer(A, m, m, lda, out T* pA))
				return false;
			if (!GetPointer(B, m, n, ldb, out T* pB))
				return false;
			var funcRe = default(T) switch
			{
				Float32 => new NM.cblas_trsm<Float32>(NM.cblas_strsm) as NM.cblas_trsm<T>,
				Float64 => new NM.cblas_trsm<Float64>(NM.cblas_dtrsm) as NM.cblas_trsm<T>,
				_ => null,
			};
			var funcCm = default(T) switch
			{
				Complex<Float32> => new NM.cblas_trsm_comp<Complex<Float32>>(NM.cblas_ctrsm) as NM.cblas_trsm_comp<T>,
				Complex<Float64> => new NM.cblas_trsm_comp<Complex<Float64>>(NM.cblas_ztrsm) as NM.cblas_trsm_comp<T>,
				_ => null,
			};
			var lr = leftA ? MklBlasSideMode.Left : MklBlasSideMode.Right;
			var fu = fillUpper ? MklFillMode.Upper : MklFillMode.Lower;
			var ud = unitDiag ? MklBlasDiagType.Unit : MklBlasDiagType.NonUnit;
			using var conjA = Conjugater.Create(pA, m, m, lda, ref op);
			funcRe?.Invoke(MklMatrixLayout.ColMajor, lr, fu, op.ToMkl(), ud, m, n, α, pA, lda, pB, ldb);
			funcCm?.Invoke(MklMatrixLayout.ColMajor, lr, fu, op.ToMkl(), ud, m, n, α, pA, lda, pB, ldb);
			return funcRe != null || funcCm != null;
		}

		/// <inheritdoc/>
		public virtual bool TriangularMatrixMultiplyGeneral<T, TS1, TS2, TS3>(bool leftA, bool fillUpper, bool unitDiag, MatrixOperation opA, MatrixOperation opB, long m, long n, long k, T α, TS1 A, long lda, TS2 B, long ldb, T β, TS3 C, long ldc) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>
		{
			long rowA, colA, rowB, colB;
			(rowA, colA) = opA.CanInPlace() ? (m, k) : (k, m);
			(rowB, colB) = opB.CanInPlace() ? (k, n) : (n, k);
			if (!leftA)
			{
				((rowA, colA), (rowB, colB)) = ((rowB, colB), (rowA, colA));
			}
			if (!GetPointer(A, rowA, colA, lda, out T* pA))
				return false;
			if (!GetPointer(B, rowB, colB, ldb, out T* pB))
				return false;
			if (!GetPointer(C, m, n, ldc, out T* pC))
				return false;
			if (β != T.Zero || (pB == pC && (!opB.CanInPlace() || rowB != m || colB != n || ldb != ldc || rowA != colA)))
				return false;
			opA = opA.Simplify<T>();
			var funcRe = default(T) switch
			{
				Float32 => new NM.cblas_trmm<Float32>(NM.cblas_strmm) as NM.cblas_trmm<T>,
				Float64 => new NM.cblas_trmm<Float64>(NM.cblas_dtrmm) as NM.cblas_trmm<T>,
				_ => null,
			};
			var funcCm = default(T) switch
			{
				Complex<Float32> => new NM.cblas_trmm_comp<Complex<Float32>>(NM.cblas_ctrmm) as NM.cblas_trmm_comp<T>,
				Complex<Float64> => new NM.cblas_trmm_comp<Complex<Float64>>(NM.cblas_ztrmm) as NM.cblas_trmm_comp<T>,
				_ => null,
			};
			if (funcRe == null && funcCm == null)
				return false;
			var lr = leftA ? MklBlasSideMode.Left : MklBlasSideMode.Right;
			var fu = fillUpper ? MklFillMode.Upper : MklFillMode.Lower;
			var ud = unitDiag ? MklBlasDiagType.Unit : MklBlasDiagType.NonUnit;
			bool actualSquare = opA.CanInPlace() ? ((m > n) == fillUpper) : ((n > m) == !fillUpper);
			bool conjugated = false;
			long mm = Math.Min(rowB, m), nn = Math.Min(colB, n);
			if (opA == MatrixOperation.Conjugate)
			{
				opB = opB.Conjugate().Simplify<T>();
				opA = MatrixOperation.None;
				conjugated = true;
			}
			if (pB == pC)
			{
				if (opB == MatrixOperation.Conjugate)
					Conjugater.Conjugate(pB, m, n, ldb);
			}
			else
			{
				Storage.Api.PointerMemoryCopy2D(pB, ldb, pC, ldc, mm, nn, opB);
			}
			funcRe?.Invoke(MklMatrixLayout.ColMajor, lr, fu, opA.ToMkl(), ud, mm, nn, α, pA, lda, pC, ldc);
			funcCm?.Invoke(MklMatrixLayout.ColMajor, lr, fu, opA.ToMkl(), ud, mm, nn, α, pA, lda, pC, ldc);
			long minA = Math.Min(rowA, colA), maxA = Math.Max(rowA, colA);
			if (actualSquare)
			{
				if (opA.CanInPlace() == fillUpper)
				{
					if (leftA)
						this.GeneralMatrixBinaryScalar(BinaryScalarOperation.Fill, m - minA, n, T.Zero, C, ldc, C + minA, ldc);
					else
						this.GeneralMatrixBinaryScalar(BinaryScalarOperation.Fill, m, n - minA, T.Zero, C, ldc, C + minA * ldc, ldc);
				}
			}
			else
			{
				if (opA.CanInPlace() == fillUpper)
				{
					A += minA * (opA.CanInPlace() ? lda : 1);
					if (leftA)
						this.GeneralMatricesMultiply(opA, opB, m, n, maxA - minA, α, A, lda, B + minA * (opB.CanInPlace() ? 1 : ldb), ldb, T.One, C, ldc);
					else
						this.GeneralMatricesMultiply(opB, opA, m, n - minA, opB.CanInPlace() ? colB : rowB, α, B, ldb, A, lda, T.Zero, C + minA * ldc, ldc);
				}
				else
				{
					A += minA * (opA.CanInPlace() ? 1 : lda);
					if (leftA)
						this.GeneralMatricesMultiply(opA, opB, m - minA, n, opA.CanInPlace() ? colA : rowA, α, A, lda, B, ldb, T.Zero, C + minA * ldc, ldc);
					else
						this.GeneralMatricesMultiply(opB, opA, m, n, opB.CanInPlace() ? colB : rowB, α, B + minA * (opB.CanInPlace() ? ldb : 1), ldb, A, lda, T.One, C, ldc);
				}
			}
			if (conjugated)
				Conjugater.Conjugate(pC, mm, nn, ldc);
			return true;
		}

		/// <inheritdoc/>
		public virtual bool SymmetricRankKUpdate<T, TS1, TS2>(bool fillUpper, MatrixOperation op, bool conjA, long n, long k, T α, TS1 A, long lda, T β, TS2 C, long ldc) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
		{
			if (!GetPointer(A, op.CanInPlace() ? n : k, op.CanInPlace() ? k : n, lda, out T* pA))
				return false;
			if (!GetPointer(C, n, n, ldc, out T* pC))
				return false;
			op = op.Simplify<T>();
			var funcRe = default(T) switch
			{
				Float32 => new NM.cblas_syrk<Float32>(NM.cblas_ssyrk) as NM.cblas_syrk<T>,
				Float64 => new NM.cblas_syrk<Float64>(NM.cblas_dsyrk) as NM.cblas_syrk<T>,
				_ => null,
			};
			var funcCm = default(T) switch
			{
				Complex<Float32> when conjA => new NM.cblas_syrk_comp<Complex<Float32>>(NM.cblas_cherk) as NM.cblas_syrk_comp<T>,
				Complex<Float32> when !conjA => new NM.cblas_syrk_comp<Complex<Float32>>(NM.cblas_csyrk) as NM.cblas_syrk_comp<T>,
				Complex<Float64> when conjA => new NM.cblas_syrk_comp<Complex<Float64>>(NM.cblas_zherk) as NM.cblas_syrk_comp<T>,
				Complex<Float64> when !conjA => new NM.cblas_syrk_comp<Complex<Float64>>(NM.cblas_zsyrk) as NM.cblas_syrk_comp<T>,
				_ => null,
			};
			var ul = fillUpper ? MklFillMode.Upper : MklFillMode.Lower;
			funcRe?.Invoke(MklMatrixLayout.ColMajor, ul, op.ToMkl(), n, k, α, pA, lda, β, pC, ldc);
			funcCm?.Invoke(MklMatrixLayout.ColMajor, ul, op.ToMkl(), n, k, α, pA, lda, β, pC, ldc);
			if (op == MatrixOperation.Conjugate)
				this.TriangularMatricesAdd(false, fillUpper, op, default, n, n, T.One, C, ldc, default, (TS1?)null, 1, C, ldc);
			return funcRe != null || funcCm != null;
		}

		/// <inheritdoc/>
		public virtual bool SymmetricRankTwoKUpdate<T, TS1, TS2, TS3>(bool fillUpper, MatrixOperation op, bool conjugate, long n, long k, T α, TS1 A, long lda, TS2 B, long ldb, T β, TS3 C, long ldc) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>
		{
			if (!GetPointer(A, op.CanInPlace() ? n : k, op.CanInPlace() ? k : n, lda, out T* pA))
				return false;
			if (!GetPointer(B, op.CanInPlace() ? k : n, op.CanInPlace() ? n : k, lda, out T* pB))
				return false;
			if (!GetPointer(C, n, n, ldc, out T* pC))
				return false;
			op = op.Simplify<T>();
			var funcRe = default(T) switch
			{
				Float32 => new NM.cblas_syr2k<Float32>(NM.cblas_ssyr2k) as NM.cblas_syr2k<T>,
				Float64 => new NM.cblas_syr2k<Float64>(NM.cblas_dsyr2k) as NM.cblas_syr2k<T>,
				_ => null,
			};
			var funcCm = default(T) switch
			{
				Complex<Float32> when conjugate => new NM.cblas_syr2k_comp<Complex<Float32>>(NM.cblas_cher2k) as NM.cblas_syr2k_comp<T>,
				Complex<Float32> when !conjugate => new NM.cblas_syr2k_comp<Complex<Float32>>(NM.cblas_csyr2k) as NM.cblas_syr2k_comp<T>,
				Complex<Float64> when conjugate => new NM.cblas_syr2k_comp<Complex<Float64>>(NM.cblas_zher2k) as NM.cblas_syr2k_comp<T>,
				Complex<Float64> when !conjugate => new NM.cblas_syr2k_comp<Complex<Float64>>(NM.cblas_zsyr2k) as NM.cblas_syr2k_comp<T>,
				_ => null,
			};
			var ul = fillUpper ? MklFillMode.Upper : MklFillMode.Lower;
			funcRe?.Invoke(MklMatrixLayout.ColMajor, ul, op.ToMkl(), n, k, α, pA, lda, pB, ldb, β, pC, ldc);
			funcCm?.Invoke(MklMatrixLayout.ColMajor, ul, op.ToMkl(), n, k, α, pA, lda, pB, ldb, β, pC, ldc);
			if (op == MatrixOperation.Conjugate)
				this.TriangularMatricesAdd(false, fillUpper, op, default, n, n, T.One, C, ldc, default, (TS1?)null, 1, C, ldc);
			return funcRe != null || funcCm != null;
		}
		#endregion

		#region BLAS like
		/// <inheritdoc/>
		public virtual bool GeneralMatricesAdd<T, TS1, TS2, TS3>(MatrixOperation opA, MatrixOperation opB, long m, long n, T α, TS1? A, long lda, T β, TS2? B, long ldb, TS3 C, long ldc) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>
		{
			if (!GetPointer(A, opA.CanInPlace() ? m : n, opA.CanInPlace() ? n : m, lda, out T* pA))
				return false;
			if (!GetPointer(B, opB.CanInPlace() ? m : n, opB.CanInPlace() ? n : m, ldb, out T* pB))
				return false;
			if (!GetPointer(C, m, n, ldc, out T* pC))
				return false;
			if (A is null || B is null || α == T.Zero || β == T.Zero)
			{
				var func = default(T) switch
				{
					Float32 => new NM.MKL_omatcopy<Float32>(NM.MKL_Somatcopy) as NM.MKL_omatcopy<T>,
					Float64 => new NM.MKL_omatcopy<Float64>(NM.MKL_Domatcopy) as NM.MKL_omatcopy<T>,
					Complex<Float32> => new NM.MKL_omatcopy<Complex<Float32>>(NM.MKL_Comatcopy) as NM.MKL_omatcopy<T>,
					Complex<Float64> => new NM.MKL_omatcopy<Complex<Float64>>(NM.MKL_Zomatcopy) as NM.MKL_omatcopy<T>,
					_ => null,
				};
				if (A is null || α == T.Zero)
				{
					pA = pB; lda = ldb; opA = opB; α = β;
				}
				func?.Invoke(MklMatrixLayoutChar.ColMajor, opA.ToMklChar(), opA.CanInPlace() ? m : n, opA.CanInPlace() ? n : m, α, pA, lda, pC, ldc);
				return func != null;
			}
			else
			{
				var func = default(T) switch
				{
					Float32 => new NM.MKL_omatadd<Float32>(NM.MKL_Somatadd) as NM.MKL_omatadd<T>,
					Float64 => new NM.MKL_omatadd<Float64>(NM.MKL_Domatadd) as NM.MKL_omatadd<T>,
					Complex<Float32> => new NM.MKL_omatadd<Complex<Float32>>(NM.MKL_Comatadd) as NM.MKL_omatadd<T>,
					Complex<Float64> => new NM.MKL_omatadd<Complex<Float64>>(NM.MKL_Zomatadd) as NM.MKL_omatadd<T>,
					_ => null,
				};
				func?.Invoke(MklMatrixLayoutChar.ColMajor, opA.ToMklChar(), opB.ToMklChar(), m, n, α, pA, lda, β, pB, ldb, pC, ldc);
				return func != null;
			}
		}

		/// <inheritdoc/>
		public virtual bool DiagonalMatrixMultiplyGeneral<T, TS1, TS2, TS3>(bool leftA, MatrixOperation opA, bool conjX, long m, long n, T α, TS1 A, long lda, TS2 x, long strideX, T β, TS3 C, long ldc) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>
		{
			if (!GetPointer(A, opA.CanInPlace() ? m : n, opA.CanInPlace() ? n : m, lda, out T* pA))
				return false;
			if (!GetPointer(C, m, n, ldc, out T* pC))
				return false;
			if (!GetPointer(x, strideX, out T* pX, out long lenx))
				return false;
			if (lenx < (leftA == opA.CanInPlace() ? n : m))
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(x));
			delegate*<MklMatrixLayout, MklBlasSideMode, long, long, T*, long, long, T*, long, long, T*, long, long, long, void> func = default(T) switch
			{
				Float32 => &NM.cblas_sdgmm_batch_strided,
				Float64 => &NM.cblas_ddgmm_batch_strided,
				Complex<Float32> => &NM.cblas_cdgmm_batch_strided,
				Complex<Float64> => &NM.cblas_zdgmm_batch_strided,
				_ => null,
			};
			if (func == null)
				return false;
			conjX &= T.IsComplexType;
			opA = opA.Simplify<T>();
			conjX = opA.HasConjugate() ^ conjX;
			if (!opA.CanInPlace())
			{
				leftA = !leftA;
				if (m != n)
					return false; // cannot in-place transpose C
			}
			// pre
			if (conjX)
				Conjugater.Conjugate(pX, lenx, strideX);
			// compute
			func(MklMatrixLayout.ColMajor, leftA ? MklBlasSideMode.Left : MklBlasSideMode.Right, m, n, pA, lda, 1, pX, strideX, 1, pC, ldc, 1, 1);
			// post
			if (conjX)
				Conjugater.Conjugate(pX, lenx, strideX);
			if (opA != MatrixOperation.None)
				Conjugater.Transpose(pC, m, n, ldc, opA);
			return true;
		}
		#endregion

		#region half matrix basic
		/// <inheritdoc/>
		public virtual bool TriangularMatricesAdd<T, TS1, TS2, TS3>(bool unitDiag, bool upper, MatrixOperation opA, MatrixOperation opB, long m, long n, T α, TS1? A, long lda, T β, TS2? B, long ldb, TS3 C, long ldc) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>
		{
			if (!GetPointer(A, opA.CanInPlace() ? m : n, opA.CanInPlace() ? n : m, lda, out T* pA))
				return false;
			if (!GetPointer(B, opB.CanInPlace() ? m : n, opB.CanInPlace() ? n : m, ldb, out T* pB))
				return false;
			if (!GetPointer(C, m, n, ldc, out T* pC))
				return false;
			return NMC.triMatAdd(T.Type, unitDiag, upper, opA, opB, m, n, &α, pA, lda, &β, pB, ldb, pC, ldc).Check();
		}

		/// <inheritdoc/>
		public virtual bool SymmetricMatricesAdd<T, TS1, TS2, TS3>(bool upperA, bool upperB, bool upperC, MatrixOperation opA, MatrixOperation opB, long n, T α, TS1? A, long lda, T β, TS2? B, long ldb, TS3 C, long ldc) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>
		{
			if (!GetPointer(A, n, n, lda, out T* pA))
				return false;
			if (!GetPointer(B, n, n, ldb, out T* pB))
				return false;
			if (!GetPointer(C, n, n, ldc, out T* pC))
				return false;
			return NMC.symmMatAdd(T.Type, upperA, upperB, upperC, opA, opB, n, &α, pA, lda, &β, pB, ldb, pC, ldc).Check();
		}

		/// <inheritdoc/>
		public virtual bool TriangularMatricesMultiply<T, TS1, TS2, TS3>(bool unitDiag, bool upper, MatrixOperation opA, MatrixOperation opB, long m, long n, long k, T α, TS1 A, long lda, TS2 B, long ldb, T β, TS3 C, long ldc) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>
		{
			if (α == T.Zero)
				throw new ArgumentOutOfRangeException(nameof(α), Resources.ParameterError.CannotZero);
			opA = opA.Simplify<T>(); opB = opB.Simplify<T>();
			if (!GetPointer(A, opA.CanInPlace() ? m : k, opA.CanInPlace() ? k : m, lda, out T* pA))
				return false;
			if (!GetPointer(B, opB.CanInPlace() ? k : n, opB.CanInPlace() ? n : k, ldb, out T* pB))
				return false;
			if (!GetPointer(C, m, n, ldc, out T* pC))
				return false;
			return NMC.triMatMul(T.Type, unitDiag, upper, opA, opB, m, n, k, &α, pA, lda, pB, ldb, &β, pC, ldc).Check();
		}

		/// <inheritdoc/>
		public virtual bool SymmetricMatricesMultiply<T, TS1, TS2, TS3>(bool upperA, bool upperB, bool hermA, bool hermB, MatrixOperation opA, MatrixOperation opB, long n, T α, TS1 A, long lda, TS2 B, long ldb, T β, TS3 C, long ldc) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>
		{
			if (!GetPointer(A, n, n, lda, out T* pA))
				return false;
			if (!GetPointer(B, n, n, ldb, out T* pB))
				return false;
			if (!GetPointer(C, n, n, ldc, out T* pC))
				return false;
			return NMC.symmMatMul(T.Type, upperA, upperB, hermA, hermB, opA, opB, n, &α, pA, lda, pB, ldb, &β, pC, ldc).Check();
		}

		/// <inheritdoc/>
		public virtual bool SymmetricMatrixToNormal<T, TS>(bool upper, bool hermitian, long n, TS A, long lda) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
		{
			if (!GetPointer(A, n, n, lda, out T* pA))
				return false;
			return NMC.matMakeHerm(T.Type, upper, hermitian, n, pA, lda).Check();
		}

		/// <inheritdoc/>
		public virtual bool HalfMatrixClearPart<T, TS>(bool clearDiag, bool clearLower, long m, long n, TS A, long lda) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
		{
			if (!GetPointer(A, m, n, lda, out T* pA))
				return false;
			return NMC.triMatClear(T.Type, clearLower, clearDiag, m, n, pA, lda).Check();
		}

		/// <inheritdoc/>
		public virtual bool HalfMatrixCopy<T, TS1, TS2>(bool upper, bool copyDiag, MatrixOperation opA, long m, long n, TS1 A, long lda, TS2 B, long ldb) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
		{
			if (!GetPointer(A, opA.CanInPlace() ? m : n, opA.CanInPlace() ? n : m, lda, out T* pA))
				return false;
			if (!GetPointer(B, m, n, ldb, out T* pB))
				return false;
			T one = T.One;
			return NMC.triMatMulCopy(T.Type, upper, copyDiag, opA, m, n, &one, pA, lda, pB, ldb).Check();
		}
		#endregion
	}
}
