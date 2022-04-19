using System.Runtime.CompilerServices;

using Althea.Backend.Storage;
using Althea.Helpers;
using Althea.LinearAlgebra;
using Althea.LinearAlgebra.Dense;
using Althea.NativeTypes;
using Althea.Storage;

using NM = Althea.Backend.Mkl.LinearAlgebra.Dense.NativeMethods;
using NMC = Althea.Backend.Mkl.LinearAlgebra.Dense.CustomNativeMethods;
using NMT = Althea.Backend.Mkl.LinearAlgebra.Dense.NativeMethodsTemplate;


namespace Althea.Backend.Mkl.LinearAlgebra.Dense
{
	#region conjugate helper
	internal readonly unsafe ref struct Conjugater1<T> where T : unmanaged, INumber<T>
	{
		private readonly T* ptr;
		private readonly long n, inc;
		private readonly delegate*<long, T*, long, T*, long, void> conj;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal Conjugater1(T* ptr, long n, long inc, ref MatrixOperation op)
		{
			this.ptr = ptr; this.n = n; this.inc = inc;
			this.conj = null;
			if (op == MatrixOperation.Conjugate)
			{
				op = MatrixOperation.None;
				this.conj = typeof(T) == typeof(Complex<float>) ? &NM.vcConjI : typeof(T) == typeof(Complex<double>) ? &NM.vzConjI : null;
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

	internal readonly unsafe ref struct Conjugater2<T> where T : unmanaged, INumber<T>
	{
		private readonly T* ptr1, ptr2;
		private readonly long len1, len2, inc1, inc2;
		private readonly delegate*<long, T*, long, T*, long, void> conj;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal Conjugater2(T* ptr1, long len1, long inc1, T* ptr2, long len2, long inc2, ref MatrixOperation op)
		{
			this.ptr1 = ptr1; this.ptr2 = ptr2; this.len1 = len1; this.len2 = len2; this.inc1 = inc1; this.inc2 = inc2;
			this.conj = null;
			if (op == MatrixOperation.Conjugate)
			{
				op = MatrixOperation.None;
				this.conj = typeof(T) == typeof(Complex<float>) ? &NM.vcConjI : typeof(T) == typeof(Complex<double>) ? &NM.vzConjI : null;
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

	internal readonly unsafe ref struct ConjugaterMat1<T> where T : unmanaged, INumber<T>
	{
		private readonly T* ptr;
		private readonly long m, n, ld;
		private readonly NM.MKL_imatcopy<T>? func;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal ConjugaterMat1(T* ptr, long m, long n, long ld, ref MatrixOperation op)
		{
			this.ptr = ptr; this.m = m; this.n = n; this.ld = ld;
			if (op == MatrixOperation.Conjugate)
			{
				op = MatrixOperation.None;
				this.func = default(T) switch
				{
					float => new NM.MKL_imatcopy<float>(NM.MKL_Simatcopy) as NM.MKL_imatcopy<T>,
					double => new NM.MKL_imatcopy<double>(NM.MKL_Dimatcopy) as NM.MKL_imatcopy<T>,
					Complex<float> => new NM.MKL_imatcopy<Complex<float>>(NM.MKL_Cimatcopy) as NM.MKL_imatcopy<T>,
					Complex<double> => new NM.MKL_imatcopy<Complex<double>>(NM.MKL_Zimatcopy) as NM.MKL_imatcopy<T>,
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
		internal unsafe static Conjugater1<T> Create<T>(T* ptr, long n, long inc, ref MatrixOperation op) where T : unmanaged, INumber<T> => new(ptr, n, inc, ref op);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal unsafe static Conjugater2<T> Create<T>(T* ptr1, long len1, long inc1, T* ptr2, long len2, long inc2, ref MatrixOperation op) where T : unmanaged, INumber<T> => new(ptr1, len1, inc1, ptr2, len2, inc2, ref op);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal unsafe static ConjugaterMat1<T> Create<T>(T* ptr, long m, long n, long ld, ref MatrixOperation op) where T : unmanaged, INumber<T> => new(ptr, m, n, ld, ref op);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal unsafe static void Conjugate<T>(T* ptr, long m, long n, long ld) where T : unmanaged, INumber<T>
		{
			var func = default(T) switch
			{
				float => new NM.MKL_imatcopy<float>(NM.MKL_Simatcopy) as NM.MKL_imatcopy<T>,
				double => new NM.MKL_imatcopy<double>(NM.MKL_Dimatcopy) as NM.MKL_imatcopy<T>,
				Complex<float> => new NM.MKL_imatcopy<Complex<float>>(NM.MKL_Cimatcopy) as NM.MKL_imatcopy<T>,
				Complex<double> => new NM.MKL_imatcopy<Complex<double>>(NM.MKL_Zimatcopy) as NM.MKL_imatcopy<T>,
				_ => null
			};
			func?.Invoke(MklMatrixLayoutChar.ColMajor, MklOperationChar.Conjugate, m, n, T.One, ptr, ld);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal unsafe static bool Transpose<T>(T* ptr, long m, long n, long ld, MatrixOperation op) where T : unmanaged, INumber<T>
		{
			if (m > ld || n > ld)
				return false;
			var func = default(T) switch
			{
				float => new NM.MKL_imatcopy<float>(NM.MKL_Simatcopy) as NM.MKL_imatcopy<T>,
				double => new NM.MKL_imatcopy<double>(NM.MKL_Dimatcopy) as NM.MKL_imatcopy<T>,
				Complex<float> => new NM.MKL_imatcopy<Complex<float>>(NM.MKL_Cimatcopy) as NM.MKL_imatcopy<T>,
				Complex<double> => new NM.MKL_imatcopy<Complex<double>>(NM.MKL_Zimatcopy) as NM.MKL_imatcopy<T>,
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
	public unsafe class Api : IBlasAbstractApi, IExtendBlasAbstractApi, IHalfMatrixBlasAbstractApi, ILapackAbstractApi
	{
		#region basic
		void IDisposable.Dispose()
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
		/// Get the default <see cref="Api"/>.
		/// </summary>
		internal protected static readonly Api Default = new();

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

		private readonly Dictionary<(MatrixOperation opA, MatrixOperation opB, long m, long n, long k, Complex<double> α, Complex<double> β, long lda, long ldb, long ldc), int> candidates = new(128);
		private readonly Queue<(MatrixOperation opA, MatrixOperation opB, long m, long n, long k, Complex<double> α, Complex<double> β, long lda, long ldb, long ldc)> candidatesQueue = new(128);

		private readonly Dictionary<(MatrixOperation opA, MatrixOperation opB, long m, long n, long k, Complex<double> α, Complex<double> β, long lda, long ldb, long ldc), (IntPtr jitter, ReaderWriterLockSlim locker)> compiled = new(16);
		private readonly Queue<(MatrixOperation opA, MatrixOperation opB, long m, long n, long k, Complex<double> α, Complex<double> β, long lda, long ldb, long ldc)> compiledQueue = new(16);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool GetPointer<T, TS>(TS s, long stride, out T* pointer, out long length) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>
		{
			pointer = default; length = 0;
			if (s is null || !s.IsValid())
				throw new ArgumentNullException(nameof(s));
			if (stride <= 0)
				throw new ArgumentOutOfRangeException(nameof(stride), stride, Resources.ParameterError.MustPositive);
			if (s is not PureStorage<T, CpuMemoryPointer> ps)
				return false; // not support
			ps.Pointer.Pointer.UnmangedPointer<T>(ps.Pointer.OffsetInBytes);
			if (pointer == default)
				throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(s));
			length = ps.Length;
			length = (length - 1) / stride + 1;
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool GetPointer<T, TS>(TS? s, long m, long n, long ld, out T* pointer) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>
		{
			pointer = default;
			if (s is null || !s.IsValid())
				return true;
			if (m <= 0)
				throw new ArgumentOutOfRangeException(nameof(m), m, Resources.ParameterError.MustPositive);
			if (n <= 0)
				throw new ArgumentOutOfRangeException(nameof(n), n, Resources.ParameterError.MustPositive);
			if (ld < m)
				throw new ArgumentOutOfRangeException(nameof(ld), ld, Resources.ParameterError.InvalidValue);
			if (s is not PureStorage<T, CpuMemoryPointer> ps)
				return false; // not support
			pointer = ps.Pointer.Pointer.UnmangedPointer<T>(ps.Pointer.OffsetInBytes);
			if (pointer == default)
				throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(s));
			if ((ps.Length + (ld - m)) / ld < n)
				throw new ArgumentException(Resources.ParameterError.InvalidValue);
			return true;
		}
		#endregion

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
		internal protected static bool HorizontalAbsoluteValueArgMinMax<T, TS>(bool max, TS x, int strideX, out long index) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>
		{
			index = -1;
			if (!NumberType<T>.IsComplex)
				throw new TypeMismatchException(typeof(T), TypeMismatchException.MismatchReason.NotComplex);
			if (!GetPointer(x, strideX, out T* px, out long n))
				return false;
			delegate*<long, T*, long, long> func = default(T) switch
			{
				Complex<float> => max ? &NM.cblas_icamax : &NM.cblas_icamin,
				Complex<double> => max ? &NM.cblas_izamax : &NM.cblas_izamin,
				_ => null,
			};
			if (func is null)
				return false;
			index = func(n, px, strideX) - 1;
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
		internal protected static bool HorizontalAbsoluteSum<T, TS>(TS x, int strideX, out T sum) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>
		{
			sum = T.Zero;
			T result = T.Zero;
			if (!NumberType<T>.IsComplex)
				throw new TypeMismatchException(typeof(T), TypeMismatchException.MismatchReason.NotComplex);
			if (!GetPointer(x, strideX, out T* px, out long n))
				return false;
			if (Unmanaged<T>.DataType == DataType.ComplexSingle)
				*(float*)&result = NM.cblas_scasum(n, px, strideX);
			else if (Unmanaged<T>.DataType == DataType.ComplexDouble)
				*(double*)&result = NM.cblas_dzasum(n, px, strideX);
			else
				return false;
			sum = result;
			return true;
		}

		/// <inheritdoc/>
		public virtual bool AbsoluteValueArgMax<T, TS>(TS x, long strideX, out long index) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>
		{
			index = -1;
			if (!GetPointer(x, strideX, out T* px, out long n))
				return false;
			delegate*<long, T*, long, long> func = null;
			if (typeof(T) == typeof(float))
				func = &NM.cblas_isamax;
			if (typeof(T) == typeof(double))
				func = &NM.cblas_idamax;
			if (func != null)
			{
				index = func(n, px, strideX);
				return true;
			}
			return NMC.vecArgAbsMax(Unmanaged<T>.DataType, n, px, strideX, out index) == CustomStatus.Success;
		}

		/// <inheritdoc/>
		public virtual bool AbsoluteValueArgMin<T, TS>(TS x, long strideX, out long index) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>
		{
			index = -1;
			if (!GetPointer(x, strideX, out T* px, out long n))
				return false;
			delegate*<long, T*, long, long> func = default(T) switch
			{
				float => &NM.cblas_isamin,
				double => &NM.cblas_idamin,
				_ => null,
			};
			if (func != null)
			{
				index = func(n, px, strideX);
				return true;
			}
			return NMC.vecArgAbsMin(Unmanaged<T>.DataType, n, px, strideX, out index) == CustomStatus.Success;
		}

		/// <inheritdoc/>
		public virtual bool AbsoluteValueSum<T, TS>(TS x, long strideX, out T sum) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>
		{
			sum = default; T result = T.Zero;
			if (!GetPointer(x, strideX, out T* px, out long n))
				return false;
			if (typeof(T) == typeof(float))
			{
				*(float*)&result = NM.cblas_sasum(n, px, strideX);
			}
			else if (typeof(T) == typeof(double))
			{
				*(double*)&result = NM.cblas_dasum(n, px, strideX);
			}
			else if (NMC.vecAbsSum(Unmanaged<T>.DataType, n, px, strideX, &result) != CustomStatus.Success)
				return false;
			sum = result;
			return true;
		}

		/// <inheritdoc/>
		public virtual bool Norm<T, TS>(TS x, long strideX, out T norm) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>
		{
			norm = default; T result = T.Zero;
			if (!GetPointer(x, strideX, out T* px, out long n))
				return false;
			if (typeof(T) == typeof(float))
				*(float*)&result = NM.cblas_snrm2(n, px, strideX);
			else if (typeof(T) == typeof(double))
				*(double*)&result = NM.cblas_dnrm2(n, px, strideX);
			else if (typeof(T) == typeof(Complex<float>))
				*(float*)&result = NM.cblas_scnrm2(n, px, strideX);
			else if (typeof(T) == typeof(Complex<double>))
				*(double*)&result = NM.cblas_dznrm2(n, px, strideX);
			else
				return false;
			norm = result;
			return true;
		}

		/// <inheritdoc/>
		public virtual bool Scale<T, TS>(TS x, long strideX, T scalar) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>
		{
			if (!GetPointer(x, strideX, out T* px, out long n))
				return false;
			NM.cblas_scal<T>? funcRe = default(T) switch
			{
				float => new NM.cblas_scal<float>(NM.cblas_sscal) as NM.cblas_scal<T>,
				double => new NM.cblas_scal<double>(NM.cblas_dscal) as NM.cblas_scal<T>,
				_ => null,
			};
			NM.cblas_scal_comp<T>? funcCm = default(T) switch
			{
				Complex<float> => new NM.cblas_scal_comp<Complex<float>>(NM.cblas_cscal) as NM.cblas_scal_comp<T>,
				Complex<double> => new NM.cblas_scal_comp<Complex<double>>(NM.cblas_zscal) as NM.cblas_scal_comp<T>,
				_ => null,
			};
			funcRe?.Invoke(n, scalar, px, strideX);
			funcCm?.Invoke(n, scalar, px, strideX);
			return funcRe != null || funcCm != null;
		}

		/// <inheritdoc/>
		public virtual bool Add<T, TS1, TS2>(T α, TS1 x, long strideX, TS2 y, long strideY) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
		{
			if (!GetPointer(x, strideX, out T* px, out long n))
				return false;
			if (!GetPointer(y, strideY, out T* py, out long n2))
				return false;
			if (n != n2)
				throw new ArgumentException(Resources.ParameterError.NotSameSize);
			NM.cblas_axpy<T>? funcRe = default(T) switch
			{
				float => new NM.cblas_axpy<float>(NM.cblas_saxpy) as NM.cblas_axpy<T>,
				double => new NM.cblas_axpy<double>(NM.cblas_daxpy) as NM.cblas_axpy<T>,
				_ => null,
			};
			NM.cblas_axpy_comp<T>? funcCm = default(T) switch
			{
				Complex<float> => new NM.cblas_axpy_comp<Complex<float>>(NM.cblas_caxpy) as NM.cblas_axpy_comp<T>,
				Complex<double> => new NM.cblas_axpy_comp<Complex<double>>(NM.cblas_zaxpy) as NM.cblas_axpy_comp<T>,
				_ => null,
			};
			funcRe?.Invoke(n, α, px, strideX, py, strideY);
			funcCm?.Invoke(n, α, px, strideX, py, strideY);
			return funcRe != null || funcCm != null;
		}

		/// <inheritdoc/>
		public virtual bool Dot<T, TS1, TS2>(bool conjX, TS1 x, long strideX, TS2 y, long strideY, out T dot) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
		{
			dot = default;
			if (!GetPointer(x, strideX, out T* px, out long n))
				return false;
			if (!GetPointer(y, strideY, out T* py, out long n2))
				return false;
			if (n != n2)
				throw new ArgumentException(Resources.ParameterError.NotSameSize);
			NMT.cblas_dot<T>? funcRe = default(T) switch
			{
				float => new NMT.cblas_dot<float>(NM.cblas_sdot) as NMT.cblas_dot<T>,
				double => new NMT.cblas_dot<double>(NM.cblas_ddot) as NMT.cblas_dot<T>,
				_ => null,
			};
			NMT.cblas_dot_comp<T>? funcCm = default(T) switch
			{
				Complex<float> => (conjX ? new NMT.cblas_dot_comp<Complex<float>>(NM.cblas_cdotc_sub) : new NMT.cblas_dot_comp<Complex<float>>(NM.cblas_cdotu_sub)) as NMT.cblas_dot_comp<T>,
				Complex<double> => (conjX ? new NMT.cblas_dot_comp<Complex<double>>(NM.cblas_zdotc_sub) : new NMT.cblas_dot_comp<Complex<double>>(NM.cblas_zdotu_sub)) as NMT.cblas_dot_comp<T>,
				_ => null,
			};
			dot = funcRe?.Invoke(n, px, strideX, py, strideY) ?? dot;
			funcCm?.Invoke(n, px, strideX, py, strideY, out dot);
			return funcRe != null || funcCm != null;
		}
		#endregion

		#region BLAS level 2
		/// <inheritdoc/>
		public virtual bool GeneralMatrixMultiplyVector<T, TSM, TSV1, TSV2>(MatrixOperation op, long m, long n, T α, TSM A, long lda, TSV1 x, long strideX, T β, TSV2 y, long strideY) where T : unmanaged, INumber<T> where TSM : class, IStorage<T, TSM> where TSV1 : class, IStorage<T, TSV1> where TSV2 : class, IStorage<T, TSV2>
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
			NM.cblas_gemv<T>? funcRe = default(T) switch
			{
				float => new NM.cblas_gemv<float>(NM.cblas_sgemv) as NM.cblas_gemv<T>,
				double => new NM.cblas_gemv<double>(NM.cblas_dgemv) as NM.cblas_gemv<T>,
				_ => null,
			};
			NM.cblas_gemv_comp<T>? funcCm = default(T) switch
			{
				Complex<float> => new NM.cblas_gemv_comp<Complex<float>>(NM.cblas_cgemv) as NM.cblas_gemv_comp<T>,
				Complex<double> => new NM.cblas_gemv_comp<Complex<double>>(NM.cblas_zgemv) as NM.cblas_gemv_comp<T>,
				_ => null,
			};
			using var conj = Conjugater.Create(px, op.CanInPlace() ? n : m, strideX, py, op.CanInPlace() ? m : n, strideY, ref op);
			funcRe?.Invoke(MklMatrixLayout.ColMajor, op.ToMkl(), m, n, α, pA, lda, px, strideX, β, py, strideY);
			funcCm?.Invoke(MklMatrixLayout.ColMajor, op.ToMkl(), m, n, α, pA, lda, px, strideX, β, py, strideY);
			return funcRe != null || funcCm != null;
		}

		/// <inheritdoc/>
		public virtual bool SymmetricMatrixMultiplyVector<T, TSM, TSV1, TSV2>(bool fillUpper, bool hermA, long n, T α, TSM A, long lda, TSV1 x, long strideX, T β, TSV2 y, long strideY) where T : unmanaged, INumber<T> where TSM : class, IStorage<T, TSM> where TSV1 : class, IStorage<T, TSV1> where TSV2 : class, IStorage<T, TSV2>
		{
			if (!GetPointer(x, strideX, out T* px, out _))
				return false;
			if (!GetPointer(y, strideY, out T* py, out _))
				return false;
			if (!GetPointer(A, n, n, lda, out T* pA))
				return false;
			if (px == py && strideX != strideY)
				return false;
			NM.cblas_symv<T>? funcRe = default(T) switch
			{
				float => new NM.cblas_symv<float>(NM.cblas_ssymv) as NM.cblas_symv<T>,
				double => new NM.cblas_symv<double>(NM.cblas_dsymv) as NM.cblas_symv<T>,
				_ => null,
			};
			NM.cblas_symv_comp<T>? funcCm = default(T) switch
			{
				Complex<float> => (hermA ? new NM.cblas_symv_comp<Complex<float>>(NM.cblas_chemv) : new NM.cblas_symv_comp<Complex<float>>(NM.cblas_csymv)) as NM.cblas_symv_comp<T>,
				Complex<double> => (hermA ? new NM.cblas_symv_comp<Complex<double>>(NM.cblas_zhemv) : new NM.cblas_symv_comp<Complex<double>>(NM.cblas_zsymv)) as NM.cblas_symv_comp<T>,
				_ => null,
			};
			var fu = fillUpper ? MklFillMode.Upper : MklFillMode.Lower;
			funcRe?.Invoke(MklMatrixLayout.ColMajor, fu, n, α, pA, lda, px, strideX, β, py, strideY);
			funcCm?.Invoke(MklMatrixLayout.ColMajor, fu, n, α, pA, lda, px, strideX, β, py, strideY);
			return funcRe != null || funcCm != null;
		}

		/// <inheritdoc/>
		public virtual bool TriangularMatrixMultiplyVector<T, TSM, TSV1, TSV2>(bool fillUpper, bool unitDiag, MatrixOperation op, long m, long n, TSM A, long lda, T α, TSV1 x, long strideX, T β, TSV2 y, long strideY) where T : unmanaged, INumber<T> where TSM : class, IStorage<T, TSM> where TSV1 : class, IStorage<T, TSV1> where TSV2 : class, IStorage<T, TSV2>
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
			delegate*<MklMatrixLayout, MklFillMode, MklOperation, MklBlasDiagType, long, T*, long, T*, long, void> func = default(T) switch
			{
				float => &NM.cblas_strmv,
				double => &NM.cblas_dtrmv,
				Complex<float> => &NM.cblas_ctrmv,
				Complex<double> => &NM.cblas_ztrmv,
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
				Storage.Api.PointerStridedCopy(px, strideX, py, strideY, Math.Min(m, n));
				bool actualSquare = (m > n) == (fillUpper == op.CanInPlace());
				using var conj = Conjugater.Create(py, actualSquare ? Math.Min(m, n) : m, strideY, ref op);
				func(MklMatrixLayout.ColMajor, fu, op.ToMkl(), ud, Math.Min(m, n), pA, lda, py, strideY);
				if (!actualSquare)
				{
					if (m > n)
						this.GeneralMatrixMultiplyVector(op, m - n, n, T.One, A + n, lda, y + n * strideY, strideY, T.Zero, y + n * strideY, strideY);
					else
						this.GeneralMatrixMultiplyVector(op, m, n - m, T.One, A + m * lda, lda, x + m * strideX, strideX, T.One, y, strideY);
				}
				if (α != T.One)
					this.Scale(y, strideY, α);
			}
			return true;
		}

		/// <inheritdoc/>
		public virtual bool GeneralRankOneUpdate<T, TSM, TSV1, TSV2>(bool conjY, long m, long n, T α, TSV1 x, long strideX, TSV2 y, long strideY, T β, TSM A, long lda) where T : unmanaged, INumber<T> where TSM : class, IStorage<T, TSM> where TSV1 : class, IStorage<T, TSV1> where TSV2 : class, IStorage<T, TSV2>
		{
			if (!GetPointer(x, strideX, out T* px, out _))
				return false;
			if (!GetPointer(y, strideY, out T* py, out _))
				return false;
			if (!GetPointer(A, m, n, lda, out T* pA))
				return false;
			if (β != T.One)
				return false;
			NMT.cblas_ger<T>? funcRe = default(T) switch
			{
				float => new NMT.cblas_ger<float>(NM.cblas_sger) as NMT.cblas_ger<T>,
				double => new NMT.cblas_ger<double>(NM.cblas_dger) as NMT.cblas_ger<T>,
				_ => null,
			};
			NMT.cblas_ger_comp<T>? funcCm = default(T) switch
			{
				Complex<float> => (conjY ? new NMT.cblas_ger_comp<Complex<float>>(NM.cblas_cgerc) : new NMT.cblas_ger_comp<Complex<float>>(NM.cblas_cgeru)) as NMT.cblas_ger_comp<T>,
				Complex<double> => (conjY ? new NMT.cblas_ger_comp<Complex<double>>(NM.cblas_zgerc) : new NMT.cblas_ger_comp<Complex<double>>(NM.cblas_zgeru)) as NMT.cblas_ger_comp<T>,
				_ => null,
			};
			funcRe?.Invoke(MklMatrixLayout.ColMajor, m, n, α, px, strideX, py, strideY, pA, lda);
			funcCm?.Invoke(MklMatrixLayout.ColMajor, m, n, α, px, strideX, py, strideY, pA, lda);
			return funcRe != null || funcCm != null;
		}

		/// <inheritdoc/>
		public virtual bool SymmetricRankOneUpdate<T, TSM, TSV>(bool fillUpper, bool conjX, long n, T α, TSV x, long strideX, T β, TSM A, long lda) where T : unmanaged, INumber<T> where TSM : class, IStorage<T, TSM> where TSV : class, IStorage<T, TSV>
		{
			if (!GetPointer(x, strideX, out T* px, out _))
				return false;
			if (!GetPointer(A, n, n, lda, out T* pA))
				return false;
			if (NumberType<T>.IsComplex && !conjX)
				return false;
			NM.cblas_syr<T>? funcRe = default(T) switch
			{
				float => new NM.cblas_syr<float> (NM.cblas_ssyr) as NM.cblas_syr<T>,
				double => new NM.cblas_syr<double>(NM.cblas_dsyr) as NM.cblas_syr<T>,
				_ => null,
			};
			NM.cblas_her_comp<T>? funcCm = default(T) switch
			{
				Complex<float> => new NM.cblas_her_comp<Complex<float>>(NM.cblas_cher) as NM.cblas_her_comp<T>,
				Complex<double> => new NM.cblas_her_comp<Complex<double>>(NM.cblas_zher) as NM.cblas_her_comp<T>,
				_ => null,
			};
			funcRe?.Invoke(MklMatrixLayout.ColMajor, fillUpper ? MklFillMode.Upper : MklFillMode.Lower, n, α, px, strideX, pA, lda);
			funcCm?.Invoke(MklMatrixLayout.ColMajor, fillUpper ? MklFillMode.Upper : MklFillMode.Lower, n, α, px, strideX, pA, lda);
			return funcRe != null || funcCm != null;
		}

		/// <inheritdoc/>
		public virtual bool SymmetricRankTwoUpdate<T, TSM, TSV1, TSV2>(bool fillUpper, bool conjugate, long n, T α, TSV1 x, long strideX, TSV2 y, long strideY, T β, TSM A, long lda) where T : unmanaged, INumber<T> where TSM : class, IStorage<T, TSM> where TSV1 : class, IStorage<T, TSV1> where TSV2 : class, IStorage<T, TSV2>
		{
			if (!GetPointer(x, strideX, out T* px, out _))
				return false;
			if (!GetPointer(y, strideY, out T* py, out _))
				return false;
			if (!GetPointer(A, n, n, lda, out T* pA))
				return false;
			if (NumberType<T>.IsComplex && !conjugate)
				return false;
			NM.cblas_syr2<T>? funcRe = default(T) switch
			{
				float => new NM.cblas_syr2<float>(NM.cblas_ssyr2) as NM.cblas_syr2<T>,
				double => new NM.cblas_syr2<double>(NM.cblas_dsyr2) as NM.cblas_syr2<T>,
				_ => null,
			};
			NM.cblas_her2_comp<T>? funcCm = default(T) switch
			{
				Complex<float> => new NM.cblas_her2_comp<Complex<float>>(NM.cblas_cher2) as NM.cblas_her2_comp<T>,
				Complex<double> => new NM.cblas_her2_comp<Complex<double>>(NM.cblas_zher2) as NM.cblas_her2_comp<T>,
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
		public virtual bool GeneralMatricesMultiply<T, TS1, TS2, TS3>(MatrixOperation opA, MatrixOperation opB, long m, long n, long k, T α, TS1 A, long lda, TS2 B, long ldb, T β, TS3 C, long ldc) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>
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
				NM.cblas_gemm<T>? funcRe = default(T) switch
				{
					float => new NM.cblas_gemm<float>(NM.cblas_sgemm) as NM.cblas_gemm<T>,
					double => new NM.cblas_gemm<double>(NM.cblas_dgemm) as NM.cblas_gemm<T>,
					_ => null,
				};
				NM.cblas_gemm_comp<T>? funcCm = default(T) switch
				{
					Complex<float> when !this.ComplexGemmUseGemm3M => new NM.cblas_gemm_comp<Complex<float>>(NM.cblas_cgemm) as NM.cblas_gemm_comp<T>,
					Complex<float> when this.ComplexGemmUseGemm3M => new NM.cblas_gemm_comp<Complex<float>>(NM.cblas_cgemm3m) as NM.cblas_gemm_comp<T>,
					Complex<double> when !this.ComplexGemmUseGemm3M => new NM.cblas_gemm_comp<Complex<double>>(NM.cblas_zgemm) as NM.cblas_gemm_comp<T>,
					Complex<double> when this.ComplexGemmUseGemm3M => new NM.cblas_gemm_comp<Complex<double>>(NM.cblas_zgemm3m) as NM.cblas_gemm_comp<T>,
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
			bool JitGemm((MatrixOperation opA, MatrixOperation opB, long m, long n, long k, Complex<double> α, Complex<double> β, long lda, long ldb, long ldc) key, (IntPtr jitter, ReaderWriterLockSlim locker) jit)
			{
				// compute
				jit.locker.EnterReadLock();
				try
				{
					delegate*<IntPtr, delegate* unmanaged<IntPtr, T*, T*, T*, void>> getGemmFunc = default(T) switch
					{
						float => &NM.mkl_jit_get_sgemm_ptr,
						double => &NM.mkl_jit_get_dgemm_ptr,
						Complex<float> => &NM.mkl_jit_get_cgemm_ptr,
						Complex<double> => &NM.mkl_jit_get_zgemm_ptr,
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
				var key = (opA, opB, m, n, k, α.As<T, Complex<double>>(), β.As<T, Complex<double>>(), lda, ldb, ldc);
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
				NM.mkl_jit_create_gemm<T>? funcRe = default(T) switch
				{
					float => new NM.mkl_jit_create_gemm<float>(NM.mkl_jit_create_sgemm) as NM.mkl_jit_create_gemm<T>,
					double => new NM.mkl_jit_create_gemm<double>(NM.mkl_jit_create_dgemm) as NM.mkl_jit_create_gemm<T>,
					_ => null,
				};
				NM.mkl_jit_create_gemm_comp<T>? funcCm = default(T) switch
				{
					Complex<float> => new NM.mkl_jit_create_gemm_comp<Complex<float>>(NM.mkl_jit_create_cgemm) as NM.mkl_jit_create_gemm_comp<T>,
					Complex<double> => new NM.mkl_jit_create_gemm_comp<Complex<double>>(NM.mkl_jit_create_zgemm) as NM.mkl_jit_create_gemm_comp<T>,
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
		public virtual bool SymmetricMatrixMultiplyGeneral<T, TS1, TS2, TS3>(bool fillUpper, bool leftA, bool hermA, MatrixOperation opA, MatrixOperation opB, long m, long n, T α, TS1 A, long lda, TS2 B, long ldb, T β, TS3 C, long ldc) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>
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
			NM.cblas_symm<T>? funcRe = default(T) switch
			{
				float => new NM.cblas_symm<float>(NM.cblas_ssymm) as NM.cblas_symm<T>,
				double => new NM.cblas_symm<double>(NM.cblas_dsymm) as NM.cblas_symm<T>,
				_ => null,
			};
			NM.cblas_symm_comp<T>? funcCm = default(T) switch
			{
				Complex<float> when hermA => new NM.cblas_symm_comp<Complex<float>>(NM.cblas_chemm) as NM.cblas_symm_comp<T>,
				Complex<float> when !hermA => new NM.cblas_symm_comp<Complex<float>>(NM.cblas_csymm) as NM.cblas_symm_comp<T>,
				Complex<double> when hermA => new NM.cblas_symm_comp<Complex<double>>(NM.cblas_zhemm) as NM.cblas_symm_comp<T>,
				Complex<double> when !hermA => new NM.cblas_symm_comp<Complex<double>>(NM.cblas_zsymm) as NM.cblas_symm_comp<T>,
				_ => null,
			};
			if (funcRe == null && funcCm == null)
				return false;
			var side = leftA ? MklBlasSideMode.Left : MklBlasSideMode.Right;
			var uplo = fillUpper ? MklFillMode.Upper : MklFillMode.Lower;
			if (!opB.CanInPlace())
			{
				if (m > ldc || n > ldc)
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
		public virtual bool TriangularMatrixSolve<T, TS1, TS2>(bool leftA, bool fillUpper, bool unitDiag, MatrixOperation op, long m, long n, T α, TS1 A, long lda, TS2 B, long ldb) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
		{
			if (α == T.Zero)
				throw new ArgumentOutOfRangeException(nameof(α), Resources.ParameterError.CannotZero);
			op = op.Simplify<T>();
			if (!GetPointer(A, m ,m, lda, out T* pA))
				return false;
			if (!GetPointer(B, m, n, ldb, out T* pB))
				return false;
			NM.cblas_trsm<T>? funcRe = default(T) switch
			{
				float => new NM.cblas_trsm<float>(NM.cblas_strsm) as NM.cblas_trsm<T>,
				double => new NM.cblas_trsm<double>(NM.cblas_dtrsm) as NM.cblas_trsm<T>,
				_ => null,
			};
			NM.cblas_trsm_comp<T>? funcCm = default(T) switch
			{
				Complex<float> => new NM.cblas_trsm_comp<Complex<float>>(NM.cblas_ctrsm) as NM.cblas_trsm_comp<T>,
				Complex<double> => new NM.cblas_trsm_comp<Complex<double>>(NM.cblas_ztrsm) as NM.cblas_trsm_comp<T>,
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
		public virtual bool TriangularMatrixMultiplyGeneral<T, TS1, TS2, TS3>(bool leftA, bool fillUpper, bool unitDiag, MatrixOperation opA, MatrixOperation opB, long m, long n, long k, T α, TS1 A, long lda, TS2 B, long ldb, T β, TS3 C, long ldc) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>
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
			if (β != T.Zero || (pB == pC && (!opB.CanInPlace() || rowB != m || colB != n || ldb != ldc)))
				return false;
			opA = opA.Simplify<T>();
			NM.cblas_trmm<T>? funcRe = default(T) switch
			{
				float => new NM.cblas_trmm<float>(NM.cblas_strmm) as NM.cblas_trmm<T>,
				double => new NM.cblas_trmm<double>(NM.cblas_dtrmm) as NM.cblas_trmm<T>,
				_ => null,
			};
			NM.cblas_trmm_comp<T>? funcCm = default(T) switch
			{
				Complex<float> => new NM.cblas_trmm_comp<Complex<float>>(NM.cblas_ctrmm) as NM.cblas_trmm_comp<T>,
				Complex<double> => new NM.cblas_trmm_comp<Complex<double>>(NM.cblas_ztrmm) as NM.cblas_trmm_comp<T>,
				_ => null,
			};
			var fu = fillUpper ? MklFillMode.Upper : MklFillMode.Lower;
			var ud = unitDiag ? MklBlasDiagType.Unit : MklBlasDiagType.NonUnit;
			if (pB == pC)
			{   // B = alpha * opA(A) * opB(B)  or  B = alpha * opB(B) * opA(A)
				
			}
			else
			{
				Storage.Api.PointerStridedCopy(px, strideX, py, strideY, Math.Min(m, n));
				bool actualSquare = (m > n) == (fillUpper == op.CanInPlace());
				using var conj = Conjugater.Create(py, actualSquare ? Math.Min(m, n) : m, strideY, ref op);
				func(MklMatrixLayout.ColMajor, fu, op.ToMkl(), ud, Math.Min(m, n), pA, lda, py, strideY);
				if (!actualSquare)
				{
					if (m > n)
						this.GeneralMatrixMultiplyVector(op, m - n, n, T.One, A + n, lda, y + n * strideY, strideY, T.Zero, y + n * strideY, strideY);
					else
						this.GeneralMatrixMultiplyVector(op, m, n - m, T.One, A + m * lda, lda, x + m * strideX, strideX, T.One, y, strideY);
				}
				if (α != T.One)
					this.Scale(y, strideY, α);
			}
			return true;
		}

		/// <inheritdoc/>
		public virtual bool SymmetricRankKUpdate<T, TS1, TS2>(bool fillUpper, MatrixOperation op, bool conjA, long n, long k, T α, TS1 A, long lda, T β, TS2 C, long ldc) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

		/// <inheritdoc/>
		public virtual bool SymmetricRankTwoKUpdate<T, TS1, TS2, TS3>(bool fillUpper, MatrixOperation op, bool conjugate, long n, long k, T α, TS1 A, long lda, TS2 B, long ldb, T β, TS3 C, long ldc) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>;

		/// <inheritdoc/>
		public virtual bool GeneralRankKUpdate<T, TS1, TS2, TS3>(MatrixOperation op, bool conjB, long n, long k, T α, TS1 A, long lda, TS2 B, long ldb, T β, TS3 C, long ldc) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>;
		#endregion

		#region BLAS like
		/// <inheritdoc/>
		public virtual bool GeneralMatricesAdd<T, TS1, TS2, TS3>(MatrixOperation opA, MatrixOperation opB, long m, long n, T α, TS1? A, long lda, T β, TS2? B, long ldb, TS3 C, long ldc) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>;

		/// <inheritdoc/>
		public virtual bool DiagonalMatrixMultiplyGeneral<T, TS1, TS2, TS3>(bool leftA, MatrixOperation opA, bool conjX, long m, long n, T α, TS1 A, long lda, TS2 x, long strideX, T β, TS3 C, long ldc) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>;
		#endregion

		#region vector math
		/// <inheritdoc/>
		public virtual bool FillWithValue<T, TS>(TS x, long strideX, T value) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <inheritdoc/>
		public virtual bool PointWiseEquals<T, TS1, TS2>(TS1 x, long strideX, TS2 y, long strideY, out bool equals) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

		/// <inheritdoc/>
		public virtual bool PointWiseMultiply<T, TS1, TS2>(TS1 x, long strideX, TS2 y, long strideY) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

		/// <inheritdoc/>
		public virtual bool PointWiseDivide<T, TS1, TS2>(TS1 x, long strideX, TS2 y, long strideY) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

		/// <inheritdoc/>
		public virtual bool PointWisePower<T, TS>(TS x, long stride, T p) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <inheritdoc/>
		public virtual bool PointWiseConjugate<T, TS>(TS x, long stride) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <inheritdoc/>
		public virtual bool PointWiseAddScalar<T, TS>(TS x, long stride, T scalar) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <inheritdoc/>
		public virtual bool PointWiseCast<TIn, TOut, TSIn, TSOut>(TSIn source, long strideSource, TSOut destination, long strideDestination) where TIn : unmanaged, INumber<TIn> where TOut : unmanaged, INumber<TOut> where TSIn : class, IStorage<TIn, TSIn> where TSOut : class, IStorage<TOut, TSOut>;

		/// <inheritdoc/>
		public virtual bool PointWiseTruncate<T, TS>(TS x, long stride, double threshold) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <inheritdoc/>
		public virtual bool AggregateSum<T, TS>(TS x, long stride, out T sum) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <inheritdoc/>
		public virtual bool AggregateProduct<T, TS>(TS x, long stride, out T product) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <inheritdoc/>
		public virtual bool PartialSum<T, TS1, TS2>(TS1 x, long strideX, TS2 y, long strideY, bool inclusive) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

		/// <inheritdoc/>
		public virtual bool PartialProduct<T, TS1, TS2>(TS1 x, long strideX, TS2 y, long strideY, bool inclusive) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;
		#endregion

		#region matrix math
		/// <inheritdoc/>
		public virtual bool GeneralMatrixFill<T, TS>(TS A, long ld, T value, long rows, long cols) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <inheritdoc/>
		public virtual bool GeneralMatricesEquals<T, TS1, TS2>(TS1 A, long lda, TS2 B, long ldb, long rows, long cols, out bool equals) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

		/// <inheritdoc/>
		public virtual bool GeneralMatricesMultiply<T, TS1, TS2>(TS1 A, long lda, TS2 B, long ldb, long rows, long cols) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

		/// <inheritdoc/>
		public virtual bool GeneralMatricesDivide<T, TS1, TS2>(TS1 A, long lda, TS2 B, long ldb, long rows, long cols) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

		/// <inheritdoc/>
		public virtual bool GeneralMatrixPower<T, TS>(TS A, long ld, T p, long rows, long cols) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <inheritdoc/>
		public virtual bool GeneralMatrixAddScalar<T, TS>(TS A, long ld, T scalar, long rows, long cols) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <inheritdoc/>
		public virtual bool GeneralMatrixCast<TIn, TOut, TSIn, TSOut>(TSIn source, long lds, TSOut destination, long ldd, long rows, long cols) where TIn : unmanaged, INumber<TIn> where TOut : unmanaged, INumber<TOut> where TSIn : class, IStorage<TIn, TSIn> where TSOut : class, IStorage<TOut, TSOut>;

		/// <inheritdoc/>
		public virtual bool GeneralMatrixTruncate<T, TS>(TS A, long ld, double threshold, long rows, long cols) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <inheritdoc/>
		public virtual bool GeneralMatrixSum<T, TS>(TS A, long ld, long rows, long cols, out T sum) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <inheritdoc/>
		public virtual bool GeneralMatrixAbsSum<T, TS>(TS A, long ld, long rows, long cols, out T sum) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <inheritdoc/>
		public virtual bool GeneralMatrixNorm<T, TS>(TS A, long ld, long rows, long cols, out T norm) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <inheritdoc/>
		public virtual bool GeneralMatrixProduct<T, TS>(TS A, long ld, long rows, long cols, out T product) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <inheritdoc/>
		public virtual bool GeneralMatrixAbsArgMax<T, TS>(TS A, long ld, long rows, long cols, out long index) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <inheritdoc/>
		public virtual bool GeneralMatrixAbsArgMin<T, TS>(TS A, long ld, long rows, long cols, out long index) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <inheritdoc/>
		public virtual bool GeneralMatrixSumColumns<T, TS1, TS2>(TS1 A, long ld, long rows, long cols, TS2 x, long stride) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

		/// <inheritdoc/>
		public virtual bool GeneralMatrixProductColumns<T, TS1, TS2>(TS1 A, long ld, long rows, long cols, TS2 x, long stride) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;
		#endregion

		#region matrix extended
		/// <inheritdoc/>
		public virtual bool MatrixKronecker<T, TS1, TS2, TS3>(long ma, long na, long mb, long nb, T α, TS1 A, long lda, TS2 B, long ldb, T β, TS3 C, long ldc) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>;
		#endregion

		#region half matrix basic
		/// <inheritdoc/>
		public virtual bool TriangularMatricesAdd<T, TS1, TS2, TS3>(bool unitDiag, bool upper, MatrixOperation opA, MatrixOperation opB, long m, long n, T α, TS1? A, long lda, T β, TS2? B, long ldb, TS3 C, long ldc) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>;

		/// <inheritdoc/>
		public virtual bool SymmetricMatricesAdd<T, TS1, TS2, TS3>(bool upperA, bool upperB, bool upperC, MatrixOperation opA, MatrixOperation opB, long n, T α, TS1? A, long lda, T β, TS2? B, long ldb, TS3 C, long ldc) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>;

		/// <inheritdoc/>
		public virtual bool TriangularMatricesMultiply<T, TS1, TS2, TS3>(bool unitDiag, bool upper, MatrixOperation opA, MatrixOperation opB, long m, long n, long k, T α, TS1 A, long lda, TS2 B, long ldb, T β, TS3 C, long ldc) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>;

		/// <inheritdoc/>
		public virtual bool SymmetricMatricesMultiply<T, TS1, TS2, TS3>(bool upperA, bool upperB, bool hermA, bool hermB, MatrixOperation opA, MatrixOperation opB, long n, T α, TS1 A, long lda, TS2 B, long ldb, T β, TS3 C, long ldc) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>;

		/// <inheritdoc/>
		public virtual bool SymmetricMatrixToNormal<T, TS>(bool upper, bool hermitian, long n, TS A, long lda) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <inheritdoc/>
		public virtual bool HalfMatrixClearPart<T, TS>(bool clearDiag, bool clearLower, long m, long n, TS A, long lda) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <inheritdoc/>
		public virtual bool HalfMatrixCopy<T, TS1, TS2>(bool upper, bool copyDiag, MatrixOperation opA, long m, long n, TS1 A, long lda, TS2 B, long ldb) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;
		#endregion

		#region half matrix math
		/// <inheritdoc/>
		public virtual bool HalfMatrixFill<T, TS>(bool unitDiag, TS A, bool upperA, long ld, T value, long rows, long cols) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <inheritdoc/>
		public virtual bool HalfMatricesEquals<T, TS1, TS2>(bool unitDiag, TS1 A, bool upperA, long lda, TS2 B, bool upperB, long ldb, long rows, long cols, out bool equals) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

		/// <inheritdoc/>
		public virtual bool HalfMatricesMultiply<T, TS1, TS2>(bool unitDiag, TS1 A, bool upperA, long lda, TS2 B, bool upperB, long ldb, long rows, long cols) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

		/// <inheritdoc/>
		public virtual bool HalfMatricesDivide<T, TS1, TS2>(bool unitDiag, TS1 A, bool upperA, long lda, TS2 B, bool upperB, long ldb, long rows, long cols) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

		/// <inheritdoc/>
		public virtual bool HalfMatrixPower<T, TS>(bool unitDiag, TS A, bool upperA, long ld, T p, long rows, long cols) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <inheritdoc/>
		public virtual bool HalfMatrixAddScalar<T, TS>(bool unitDiag, TS A, bool upperA, long ld, T scalar, long rows, long cols) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <inheritdoc/>
		public virtual bool HalfMatrixCast<TIn, TOut, TSIn, TSOut>(bool unitDiag, TSIn source, bool upperSrc, long lds, TSOut destination, bool upperDst, long ldd, long rows, long cols) where TIn : unmanaged, INumber<TIn> where TOut : unmanaged, INumber<TOut> where TSIn : class, IStorage<TIn, TSIn> where TSOut : class, IStorage<TOut, TSOut>;

		/// <inheritdoc/>
		public virtual bool HalfMatrixTruncate<T, TS>(bool unitDiag, TS A, bool upperA, long ld, double threshold, long rows, long cols) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <inheritdoc/>
		public virtual bool TriangularMatrixSum<T, TS>(bool unitDiag, TS A, bool upperA, long ld, long rows, long cols, out T sum) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <inheritdoc/>
		public virtual bool TriangularMatrixAbsSum<T, TS>(bool unitDiag, TS A, bool upperA, long ld, long rows, long cols, out T sum) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <inheritdoc/>
		public virtual bool SymmetricMatrixSum<T, TS>(bool herm, TS A, bool upperA, long ld, long n, out T sum) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <inheritdoc/>
		public virtual bool SymmetricMatrixAbsSum<T, TS>(bool herm, TS A, bool upperA, long ld, long n, out T sum) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <inheritdoc/>
		public virtual bool TriangularMatrixNorm<T, TS>(bool unitDiag, TS A, bool upperA, long ld, long rows, long cols, out T norm) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <inheritdoc/>
		public virtual bool SymmetricMatrixNorm<T, TS>(bool herm, TS A, bool upperA, long ld, long n, out T norm) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <inheritdoc/>
		public virtual bool SymmetricMatrixProduct<T, TS>(bool herm, TS A, bool upperA, long ld, long n, out T product) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <inheritdoc/>
		public virtual bool TriangularMatrixAbsArgMax<T, TS>(bool unitDiag, TS A, bool upperA, long ld, long rows, long cols, out long index) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <inheritdoc/>
		public virtual bool SymmetricMatrixAbsArgMax<T, TS>(bool herm, TS A, bool upperA, long ld, long n, out long index) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <inheritdoc/>
		public virtual bool SymmetricMatrixAbsArgMin<T, TS>(bool herm, TS A, bool upperA, long ld, long n, out long index) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <inheritdoc/>
		public virtual bool HalfMatrixSumColumns<T, TS1, TS2>(bool? herm, bool unitDiag, TS1 A, bool upperA, long ld, long rows, long cols, TS2 x, long stride) where T : 
		/// <inheritdoc/>
		public virtual bool SymmetricMatrixProductColumns<T, TS1, TS2>(bool herm, TS1 A, bool upperA, long ld, long n, TS2 x, long stride) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1>;
		#endregion

		#region eigen-problems
		/// <inheritdoc/>
		public virtual bool EigenStandardMatrixHermitian<T, TS1, TS2, TS3>(SolveVectorMode mode, long n, TS1 A, long lda, TS2 valOut, TS3? vecOut, long ldvec) where T : unmanaged, IFloatingPoint<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>;

		/// <inheritdoc/>
		public virtual bool EigenGeneralMatrixHermitian<T, TS1, TS2, TS3>(GeneralEigenType type, SolveVectorMode mode, long n, TS1 A, long lda, TS1 B, long ldb, TS2 valOut, TS3? vecOut, long ldvec) where T : unmanaged, IFloatingPoint<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>;

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


		/*
		#region BLAS level 1
		/// <summary>
		/// Get the index of the element with horizontal maximum absolute value (<c>abs(x[i].real) + abs(x[i].imag)</c>) in <paramref name="x"/>
		/// </summary>
		/// <typeparam name="T">Any complex data type</typeparam>
		/// <param name="x">The vector to get maximum absolute value's index</param>
		/// <param name="strideX">The spacing between consecutive elements of <paramref name="x"/></param>
		/// <param name="index">The output real index in <paramref name="x"/></param>
		/// <returns>Support or not</returns>
		internal protected static bool HorizontalAbsoluteValueArgMax<T>(Storage<T> x, int strideX, out long index) where T : unmanaged, INumber<T>
		{
			index = -1;
			if (!Const<T>.IsComplex)
				return false;
			if (!CheckPointer(x, out var px, out var n, strideX))
				return false;
			delegate*<int, IntPtr, int, long> func;
			func = Const<T>.DataType switch
			{
				DataType.ComplexSingle => &NM.cblas_icamax,
				DataType.ComplexDouble => &NM.cblas_izamax,
				_ => null,
			};
			if (func is null)
				return false;
			index = func(n, px, strideX) - 1;
			return true;
		}

		/// <summary>
		/// Get the index of the element with horizontal minimum absolute value (<c>abs(x[i].real) + abs(x[i].imag)</c>) in <paramref name="x"/>
		/// </summary>
		/// <typeparam name="T">Any complex data type</typeparam>
		/// <param name="x">The vector to get minimum absolute value's index</param>
		/// <param name="strideX">The spacing between consecutive elements of <paramref name="x"/></param>
		/// <param name="index">The output real index in <paramref name="x"/></param>
		/// <returns>Support or not</returns>
		internal protected static bool HorizontalAbsoluteValueArgMin<T>(Storage<T> x, int strideX, out long index) where T : unmanaged, INumber<T>
		{
			index = -1;
			if (!Const<T>.IsComplex)
				return false;
			if (!CheckPointer(x, out var px, out var n, strideX))
				return false;
			delegate*<int, IntPtr, int, long> func;
			func = Const<T>.DataType switch
			{
				DataType.ComplexSingle => &NM.cblas_icamin,
				DataType.ComplexDouble => &NM.cblas_izamin,
				_ => null,
			};
			if (func is null)
				return false;
			index = func(n, px, strideX) - 1;
			return true;
		}

		/// <summary>
		/// Sum the absolute values (<c>abs(x[i].real) + abs(x[i].imag)</c>) of vector <paramref name="x"/>'s all elements
		/// </summary>
		/// <typeparam name="T">Any complex data type</typeparam>
		/// <param name="x">The vector to be summed</param>
		/// <param name="strideX">The spacing between consecutive elements of <paramref name="x"/></param>
		/// <param name="sum">Output the sum as a <see cref="double"/></param>
		/// <returns>Support or not</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static bool HorizontalAbsoluteSum<T>(Storage<T> x, int strideX, out double sum) where T : unmanaged, INumber<T>
		{
			sum = 0;
			if (!Const<T>.IsComplex)
				return false;
			if (!CheckPointer(x, out var px, out var n, strideX))
				return false;
			if (Const<T>.DataType == DataType.ComplexSingle)
			{
				sum = NM.cblas_scasum(n, px, strideX);
			}
			else if (Const<T>.DataType == DataType.ComplexDouble)
			{
				sum = NM.cblas_dzasum(n, px, strideX);
			}
			else
				return false;
			return true;
		}

		protected override unsafe bool AbsoluteValueArgMax_<T>(Storage<T> x, int strideX, out long index)
		{
			index = -1;
			if (!CheckPointer(x, out var px, out var n, strideX))
				return false;
			delegate*<int, IntPtr, int, long> func;
			func = Const<T>.DataType switch
			{
				DataType.RealSingle => &NM.cblas_isamax,
				DataType.RealDouble => &NM.cblas_idamax,
				_ => null,
			};
			if (func is null)
				return false;
			index = func(n, px, strideX) - 1;
			return true;
		}

		protected override unsafe bool AbsoluteValueArgMin_<T>(Storage<T> x, int strideX, out long index)
		{
			index = -1;
			if (!CheckPointer(x, out var px, out var n, strideX))
				return false;
			delegate*<int, IntPtr, int, long> func;
			func = Const<T>.DataType switch
			{
				DataType.RealSingle => &NM.cblas_isamin,
				DataType.RealDouble => &NM.cblas_idamin,
				_ => null,
			};
			if (func is null)
				return false;
			index = func(n, px, strideX) - 1;
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool AbsSumOrNorm<T, Sum>(Storage<T> x, int strideX, out double sum) where T : unmanaged, INumber<T>
		{
			bool doSum = typeof(Sum) == typeof(bool);
			sum = 0;
			if (!CheckPointer(x, out var px, out var n, strideX))
				return false;
			delegate*<int, IntPtr, int, float> funcS;
			delegate*<int, IntPtr, int, double> funcD;
			funcS = Const<T>.DataType switch
			{
				DataType.RealSingle => doSum ? &NM.cblas_sasum : &NM.cblas_snrm2,
				DataType.ComplexSingle => doSum ? null : &NM.cblas_scnrm2,
				_ => null,
			};
			funcD = Const<T>.DataType switch
			{
				DataType.RealDouble => doSum ? &NM.cblas_dasum : &NM.cblas_dnrm2,
				DataType.ComplexSingle => doSum ? null : &NM.cblas_dznrm2,
				_ => null,
			};
			if (funcS is not null)
			{
				sum = funcS(n, px, strideX);
			}
			else if(funcD is not null)
			{
				sum = funcD(n, px, strideX);
			}
			else
				return false;
			return true;
		}

		protected override unsafe bool AbsoluteValueSum_<T>(Storage<T> x, int strideX, out double sum)
		{
			return AbsSumOrNorm<T, bool>(x, strideX, out sum);
		}

		protected override unsafe bool Norm_<T>(Storage<T> x, int strideX, out double norm)
		{
			return AbsSumOrNorm<T, byte>(x, strideX, out norm);
		}

		protected override unsafe bool Dot_<T>(bool conjX, Storage<T> x, int strideX, Storage<T> y, int strideY, out T dot)
		{
			dot = default;
			if (!CheckPointer(x, out var px, out var n1, strideX))
				return false;
			if (!CheckPointer(y, out var py, out var n2, strideY))
				return false;
			int n = Math.Min(n1, n2);
			delegate*<int, IntPtr, int, IntPtr, int, T*, void> func;
			switch (Const<T>.DataType)
			{
				case DataType.RealSingle:
					float dotS = NM.cblas_sdot(n, px, strideX, py, strideY);
					dot = *(T*)&dotS;
					return true;
				case DataType.RealDouble:
					double dotD = NM.cblas_ddot(n, px, strideX, py, strideY);
					dot = *(T*)&dotD;
					return true;
				case DataType.ComplexSingle:
					func = conjX ? &NM.cblas_cdotc_sub : &NM.cblas_cdotu_sub;
					break;
				case DataType.ComplexDouble:
					func = conjX ? &NM.cblas_zdotc_sub : &NM.cblas_zdotu_sub;
					break;
				default:
					return false;
			}
			T dotC;
			func(n, px, strideX, py, strideY, &dotC);
			dot = dotC;
			return true;
		}

		protected override unsafe bool Scale_<T>(Storage<T> x, int strideX, T scalar)
		{
			if (!CheckPointer(x, out var px, out var n, strideX))
				return false;
			delegate*<int, T*, IntPtr, int, void> func;
			switch (Const<T>.DataType)
			{
				case DataType.RealSingle:
					NM.cblas_sscal(n, *(float*)&scalar, px, strideX);
					return true;
				case DataType.RealDouble:
					NM.cblas_dscal(n, *(double*)&scalar, px, strideX);
					return true;
				case DataType.ComplexSingle:
					func = &NM.cblas_cscal;
					break;
				case DataType.ComplexDouble:
					func = &NM.cblas_zscal;
					break;
				default:
					return false;
			}
			func(n, &scalar, px, strideX);
			return true;
		}

		protected override unsafe bool VectorGeneralAdd_<T>(T α, Storage<T> x, int strideX, Storage<T> y, int strideY)
		{
			if (!CheckPointer(x, out var px, out var n1, strideX))
				return false;
			if (!CheckPointer(y, out var py, out var n2, strideY))
				return false;
			int n = Math.Min(n1, n2);
			delegate*<int, T*, IntPtr, int, IntPtr, int, void> func;
			switch (Const<T>.DataType)
			{
				case DataType.RealSingle:
					NM.cblas_saxpy(n, *(float*)&α, px, strideX, py, strideY);
					return true;
				case DataType.RealDouble:
					NM.cblas_daxpy(n, *(double*)&α, px, strideX, py, strideY);
					return true;
				case DataType.ComplexSingle:
					func = &NM.cblas_caxpy;
					break;
				case DataType.ComplexDouble:
					func = &NM.cblas_zaxpy;
					break;
				default:
					return false;
			}
			func(n, &α, px, strideX, py, strideY);
			return true;
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
			NM.vecProd(Const<T>.DataType, px, n, stride, &result);
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
			NM.vecSum(Const<T>.DataType, px, n, stride, &result);
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
			NM.vecParProd(Const<T>.DataType, px, py, n, inclusive, strideX, strideY);
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
			NM.vecParSum(Const<T>.DataType, px, py, n, inclusive, strideX, strideY);
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
			NM.vecAddScalar(Const<T>.DataType, px, &scalr, n, stride);
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
			NM.vecDataConvert(Const<T>.DataType, Const<TOut>.DataType, px, py, n, incSrc, incDst, true);
			return true;
		}

		protected override bool PointWiseConjugate_<T>(Storage<T> x, int stride)
		{
			if (!Const<T>.IsPreDefinedNoHalf)
				return false;
			if (!CheckPointerLong(x, out var px, out var n, stride))
				return false;
			NM.vecConj(Const<T>.DataType, px, n, stride);
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
			NM.vecsMulDiv(Const<T>.DataType, px, py, n, strideX, strideY, false);
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
			equals = NM.vecsEq(Const<T>.DataType, px, py, n, strideX, strideY);
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
			NM.vecsMulDiv(Const<T>.DataType, px, py, n, strideX, strideY, true);
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
				NM.vecFillVal(Const<T>.DataType, px, &one, n, stride);
				return true;
			}
			if (p == 2)
			{
				NM.vecsMulDiv(Const<T>.DataType, px, px, n, stride, stride, true);
				return true;
			}
			T pp = p.FromDouble<T>(); // for complex type, (&pp)[0..sizeof(T)/2] == (T::value_type)p
			NM.vecPowSameType(Const<T>.DataType, px, &pp, n, stride);
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
				NM.vecFillVal(Const<T>.DataType, px, &one, n, stride);
				return true;
			}
			if (p.IsEqual(Const<T>.Two))
			{
				NM.vecsMulDiv(Const<T>.DataType, px, px, n, stride, stride, true);
				return true;
			}
			NM.vecPowSameType(Const<T>.DataType, px, &p, n, stride);
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
			NM.vecClip(Const<T>.DataType, px, &pp, n, stride);
			return true;
		}
		#endregion


		#region BLAS level 2
		// Ignore Spelling: func
		protected override unsafe bool GeneralMatrixMultiplyVector_<T>(MatrixOperation op, long m, long n, T α, Storage<T> A, long lda, Storage<T> x, int strideX, T β, Storage<T> y, int strideY)
		{
			if (!CheckPointer(x, out var px, out var nx, strideX))
				return false;
			if (!CheckPointer(y, out var py, out var ny, strideY))
				return false;
			if (!CheckPointer(A, m, n, lda, out var pA, out int mm, out int nn, out int llda))
				return false;
			var opMkl = op.ToMkl();
			if (opMkl == MklOperation.ConjugateAlone)
				return false;
			////if (nx < (opMkl == MklBlasOperation.None ? nn : mm))
			////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(x));
			////if (ny < (opMkl == MklBlasOperation.None ? nn : mm))
			////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(y));

			delegate*<MklMatrixLayout, MklOperation, int, int, T*, IntPtr, int, IntPtr, int, T*, IntPtr, int, void> func;
			switch (Const<T>.DataType)
			{
				case DataType.RealSingle:
					NM.cblas_sgemv(MklMatrixLayout.ColMajor, opMkl, mm,nn, *(float*)&α, pA, llda, px, strideX, *(float*)&β, py, strideY);
					return true;
				case DataType.RealDouble:
					NM.cblas_dgemv(MklMatrixLayout.ColMajor, opMkl, mm, nn, *(double*)&α, pA, llda, px, strideX, *(double*)&β, py, strideY);
					return true;
				case DataType.ComplexSingle:
					func = &NM.cblas_cgemv;
					break;
				case DataType.ComplexDouble:
					func = &NM.cblas_zgemv;
					break;
				default:
					return false;
			}
			func(MklMatrixLayout.ColMajor, opMkl, mm, nn, &α, pA, llda, px, strideX, &β, py, strideY);
			return true;
		}

		protected override unsafe bool SymmHermMatrixMultiplyVector_<T>(bool fillUpper, bool hermA, long n, T α, Storage<T> A, long lda, Storage<T> x, int strideX, T β, Storage<T> y, int strideY)
		{
			if (!Const<T>.DataType.CheckBaseSupport())
				return false;
			if (!CheckPointer(x, out var px, out var nx, strideX))
				return false;
			if (!CheckPointer(y, out var py, out var ny, strideY))
				return false;
			if (!CheckPointer(A, n, n, lda, out var pA, out _, out int nn, out int llda))
				return false;
			if (!hermA && Const<T>.IsComplex)
				return false;

			MklFillMode fill = fillUpper ? MklFillMode.Upper : MklFillMode.Lower;
			delegate*<MklMatrixLayout, MklFillMode, int, T*, IntPtr, int, IntPtr, int, T*, IntPtr, int, void> func;
			switch (Const<T>.DataType)
			{
				case DataType.RealSingle:
					NM.cblas_ssymv(MklMatrixLayout.ColMajor, fill, nn, *(float*)&α, pA, llda, px, strideX, *(float*)&β, py, strideY);
					return true;
				case DataType.RealDouble:
					NM.cblas_dsymv(MklMatrixLayout.ColMajor, fill, nn, *(double*)&α, pA, llda, px, strideX, *(double*)&β, py, strideY);
					return true;
				case DataType.ComplexSingle:
					func = &NM.cblas_chemv;
					break;
				case DataType.ComplexDouble:
					func = &NM.cblas_zhemv;
					break;
				default:
					return false;
			}
			func(MklMatrixLayout.ColMajor, fill, nn, &α, pA, llda, px, strideX, &β, py, strideY);
			return true;
		}

		protected override unsafe bool GenralRankOneUpdate_<T>(bool conjY, long m, long n, T α, Storage<T> x, int strideX, Storage<T> y, int strideY, T β, Storage<T> A, long lda)
		{
			if (!Const<T>.DataType.CheckBaseSupport())
				return false;
			if (!CheckPointer(x, out var px, out var nx, strideX))
				return false;
			if (!CheckPointer(y, out var py, out var ny, strideY))
				return false;
			if (!CheckPointer(A, m, n, lda, out var pA, out int mm, out int nn, out int llda))
				return false;
			////if (nx < mm)
			////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(x));
			////if (ny < nn)
			////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(y));

			delegate*<MklMatrixLayout, int, int, T*, IntPtr, int, IntPtr, int, IntPtr, int, void> func;
			switch (Const<T>.DataType)
			{
				case DataType.RealSingle:
					NM.cblas_sger(MklMatrixLayout.ColMajor, mm, nn, *(float*)&α,  px, strideX, py, strideY, pA, llda);
					return true;
				case DataType.RealDouble:
					NM.cblas_dger(MklMatrixLayout.ColMajor, mm, nn, *(double*)&α, px, strideX, py, strideY, pA, llda);
					return true;
				case DataType.ComplexSingle:
					func = conjY ? &NM.cblas_cgerc : &NM.cblas_cgerc;
					break;
				case DataType.ComplexDouble:
					func = conjY ? &NM.cblas_zgerc : &NM.cblas_zgerc;
					break;
				default:
					return false;
			}
			// scale A
			this.GeneralMatricesAdd_(MatrixOperation.None, MatrixOperation.None, m, n, β, A, lda, Const<T>.Zero, null, 0, A, lda);
			// add to A
			func(MklMatrixLayout.ColMajor, mm, nn, &α, px, strideX, py, strideY, pA, llda);
			return true;
		}

		protected override unsafe bool SymmHermRankOneUpdate_<T>(bool fillUpper, bool conjX, long n, T α, Storage<T> x, int strideX, T β, Storage<T> A, long lda)
		{
			if (!Const<T>.DataType.CheckBaseSupport())
				return false;
			if (!CheckPointer(x, out var px, out var nx, strideX))
				return false;
			if (!CheckPointer(A, n, n, lda, out var pA, out _, out int nn, out int llda))
				return false;
			if (!conjX && Const<T>.IsComplex)
				return false;
			////if (nx < nn)
			////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(x));

			MklFillMode fill = fillUpper ? MklFillMode.Upper : MklFillMode.Lower;
			delegate*<MklMatrixLayout, MklFillMode, int, T*, IntPtr, int, IntPtr, int, void> func;
			switch (Const<T>.DataType)
			{
				case DataType.RealSingle:
					NM.cblas_ssyr(MklMatrixLayout.ColMajor, fill, nn, *(float*)&α, px, strideX, pA, llda);
					return true;
				case DataType.RealDouble:
					NM.cblas_dsyr(MklMatrixLayout.ColMajor, fill, nn, *(double*)&α, px, strideX, pA, llda);
					return true;
				case DataType.ComplexSingle:
					func = &NM.cblas_cher;
					break;
				case DataType.ComplexDouble:
					func = &NM.cblas_zher;
					break;
				default:
					return false;
			}
			// scale A
			this.GeneralMatricesAdd_(MatrixOperation.None, MatrixOperation.None, n, n, β, A, lda, Const<T>.Zero, null, 0, A, lda);
			// add to A
			func(MklMatrixLayout.ColMajor, fill, nn, &α, px, strideX, pA, llda);
			return true;
		}

		protected override unsafe bool SymmHermRankTwoUpdate_<T>(bool fillUpper, bool conjugate, long n, T α, Storage<T> x, int strideX, Storage<T> y, int strideY, T β, Storage<T> A, long lda)
		{
			if (!Const<T>.DataType.CheckBaseSupport())
				return false;
			if (!CheckPointer(x, out var px, out var nx, strideX))
				return false;
			if (!CheckPointer(y, out var py, out var ny, strideY))
				return false;
			if (!CheckPointer(A, n, n, lda, out var pA, out _, out int nn, out int llda))
				return false;
			if (!conjugate && Const<T>.IsComplex)
				return false;
			////if (nx < nn)
			////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(x));
			////if (ny < nn)
			////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(y));

			MklFillMode fill = fillUpper ? MklFillMode.Upper : MklFillMode.Lower;
			delegate*<MklMatrixLayout, MklFillMode, int, T*, IntPtr, int, IntPtr, int, IntPtr, int, void> func;
			switch (Const<T>.DataType)
			{
				case DataType.RealSingle:
					NM.cblas_ssyr2(MklMatrixLayout.ColMajor, fill, nn, *(float*)&α, px, strideX, py, strideY, pA, llda);
					return true;
				case DataType.RealDouble:
					NM.cblas_dsyr2(MklMatrixLayout.ColMajor, fill, nn, *(double*)&α, px, strideX, py, strideY, pA, llda);
					return true;
				case DataType.ComplexSingle:
					func = &NM.cblas_cher2;
					break;
				case DataType.ComplexDouble:
					func = &NM.cblas_zher2;
					break;
				default:
					return false;
			}
			// scale A
			this.GeneralMatricesAdd_(MatrixOperation.None, MatrixOperation.None, n, n, β, A, lda, Const<T>.Zero, null, 0, A, lda);
			// add to A
			func(MklMatrixLayout.ColMajor, fill, nn, &α, px, strideX, py, strideY, pA, llda);
			return true;
		}

		protected override unsafe bool TriangularMatrixMultiplyVector_<T>(bool fillUpper, bool unitDiag, MatrixOperation op, long n, Storage<T> A, long lda, Storage<T> x, int strideX)
		{
			if (!Const<T>.DataType.CheckBaseSupport())
				return false;
			if (!CheckPointer(x, out var px, out var nx, strideX))
				return false;
			if (!CheckPointer(A, n, n, lda, out var pA, out _, out int nn, out int llda))
				return false;
			var opMkl = op.ToMkl();
			if (opMkl == MklOperation.ConjugateAlone)
				return false;
			////if (nx < nn)
			////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(x));

			MklFillMode fill = fillUpper ? MklFillMode.Upper : MklFillMode.Lower;
			MklBlasDiagType diag = unitDiag ? MklBlasDiagType.Unit : MklBlasDiagType.NonUnit;
			delegate*<MklMatrixLayout, MklFillMode, MklOperation, MklBlasDiagType, int, IntPtr, int, IntPtr, int, void> func;
			switch (Const<T>.DataType)
			{
				case DataType.RealSingle:
					NM.cblas_strmv(MklMatrixLayout.ColMajor, fill, opMkl, diag, nn, px, strideX, pA, llda);
					return true;
				case DataType.RealDouble:
					NM.cblas_dtrmv(MklMatrixLayout.ColMajor, fill, opMkl, diag, nn, px, strideX, pA, llda);
					return true;
				case DataType.ComplexSingle:
					func = &NM.cblas_ctrmv;
					break;
				case DataType.ComplexDouble:
					func = &NM.cblas_ztrmv;
					break;
				default:
					return false;
			}
			func(MklMatrixLayout.ColMajor, fill, opMkl, diag, nn, px, strideX, pA, llda);
			return true;
		}
		#endregion


		#region BLAS like level 2
		protected override unsafe bool DiagonalMatrixMultiplyGeneral_<T>(bool leftA, long m, long n, T α, Storage<T> A, long lda, Storage<T> x, int strideX, T β, Storage<T> C, long ldc)
		{
			if (!Const<T>.DataType.CheckBaseSupport())
				return false;
			if (!CheckPointer(x, out var px, out var nx, strideX))
				return false;
			if (!CheckPointer(A, m, n, lda, out var pA, out int mm, out int nn, out int llda))
				return false;
			if (!CheckPointer(C, m, n, ldc, out var pC, out _, out _, out int lldc))
				return false;
			////if (nx < (leftA ? nn : mm))
			////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(x));

			delegate*<MklMatrixLayout, in MklBlasSideMode, in int, in int, in IntPtr, in int, in IntPtr, in int, ref IntPtr, in int, int, in int, void> func;
			func = Const<T>.DataType switch
			{
				DataType.RealSingle => &NM.cblas_sdgmm_batch,
				DataType.RealDouble => &NM.cblas_ddgmm_batch,
				DataType.ComplexSingle => &NM.cblas_cdgmm_batch,
				DataType.ComplexDouble => &NM.cblas_zdgmm_batch,
				_ => null,
			};
			IntPtr cacheC = default;
			if (!β.IsZero())
				cacheC = Marshal.AllocHGlobal((IntPtr)(sizeof(T) * m * n));
			var oldC = new ManagedPureStorage<T>(cacheC, m * n);
			try
			{
				// cache C
				if (!β.IsZero())
				{
					if (!this.GeneralMatricesAdd_(MatrixOperation.None, MatrixOperation.None, m, n, Const<T>.One, C, ldc, default, default, default, oldC, m))
						return false;
				}
				// overwrite C by diagonal multiply result
				var side = leftA ? MklBlasSideMode.Right : MklBlasSideMode.Left;
				int one = 1;
				func(MklMatrixLayout.ColMajor, in side, in mm, in nn, in pA, in llda, in px, in strideX, ref pC, in lldc, 1, in one);
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
					Marshal.FreeHGlobal(cacheC);
			}
		}
		#endregion


		#region BLAS level 3
		protected override unsafe bool TriangularMatrixSolve_<T>(bool leftA, bool fillUpper, bool unitDiag, MatrixOperation op, long m, long n, T α, Storage<T> A, long lda, Storage<T> B, long ldb)
		{
			if (!CheckPointer(A, m, m, lda, out var pA, out int mm, out _, out int llda))
				return false;
			if (!CheckPointer(B, m, n, ldb, out var pB, out _, out int nn, out int lldb))
				return false;
			if (α.IsZero()) // result is 0
				return this.GeneralMatricesAdd_(MatrixOperation.None, MatrixOperation.None, m, n, α, B, ldb, default, default, default, B, ldb);
			var opMkl = op.ToMkl();
			if (opMkl == MklOperation.ConjugateAlone)
				return false;

			var side = leftA ? MklBlasSideMode.Right : MklBlasSideMode.Left;
			var fill = fillUpper ? MklFillMode.Upper : MklFillMode.Lower;
			var diag = unitDiag ? MklBlasDiagType.Unit : MklBlasDiagType.NonUnit;
			delegate*<MklMatrixLayout, MklBlasSideMode, MklFillMode, MklOperation, MklBlasDiagType, int, int, T*, IntPtr, int, IntPtr, int, void> func;
			switch (Const<T>.DataType)
			{
				case DataType.RealSingle:
					NM.cblas_strsm(MklMatrixLayout.ColMajor, side, fill, opMkl, diag, mm, nn, *(float*)&α, pA, llda, pB, lldb);
					return true;
				case DataType.RealDouble:
					NM.cblas_dtrsm(MklMatrixLayout.ColMajor, side, fill, opMkl, diag, mm, nn, *(double*)&α, pA, llda, pB, lldb);
					return true;
				case DataType.ComplexSingle:
					func = &NM.cblas_ctrsm;
					break;
				case DataType.ComplexDouble:
					func = &NM.cblas_ztrsm;
					break;
				default:
					return false;
			}
			func(MklMatrixLayout.ColMajor, side, fill, opMkl, diag, mm, nn, &α, pA, llda, pB, lldb);
			return true;
		}

		protected override unsafe bool TriangularMatrixMultiply_<T>(bool leftA, bool fillUpper, bool unitDiag, MatrixOperation op, long m, long n, T α, Storage<T> A, long lda, Storage<T> B, long ldb, Storage<T> C, long ldc)
		{
			var opMkl = op.ToMkl();
			if (opMkl == MklOperation.ConjugateAlone)
				return false;
			if (!CheckPointer(B, m, n, ldb, out var pB, out int mm, out int nn, out int lldb))
				return false;
			if (!CheckPointer(C, m, n, ldc, out var pC, out _, out _, out int lldc))
				return false;
			if (!CheckPointer(A, leftA ? m : n, leftA ? m : n, lda, out var pA, out _, out _, out int llda))
				return false;
			if (α.IsZero()) // result is 0
				return this.GeneralMatricesAdd_(MatrixOperation.None, MatrixOperation.None, m, n, α, C, ldc, default, default, default, C, ldc);
			////if (pC == pB && ldc != ldb)
			////	throw new ArgumentException(Resources.Parameter.InvalidValue, nameof(ldc));
			if (pC != pB)
			{   // copy B to C
				if (!this.GeneralMatricesAdd_(MatrixOperation.None, MatrixOperation.None, m, n, Const<T>.One, B, ldb, default, default, default, C, ldc))
					return false;
				lldb = lldc; pB = pC;
			}

			var side = leftA ? MklBlasSideMode.Right : MklBlasSideMode.Left;
			var fill = fillUpper ? MklFillMode.Upper : MklFillMode.Lower;
			var diag = unitDiag ? MklBlasDiagType.Unit : MklBlasDiagType.NonUnit;
			delegate*<MklMatrixLayout, MklBlasSideMode, MklFillMode, MklOperation, MklBlasDiagType, int, int, T*, IntPtr, int, IntPtr, int, void> func;
			switch (Const<T>.DataType)
			{
				case DataType.RealSingle:
					NM.cblas_strmm(MklMatrixLayout.ColMajor, side, fill, opMkl, diag, mm, nn, *(float*)&α, pA, llda, pB, lldb);
					return true;
				case DataType.RealDouble:
					NM.cblas_dtrmm(MklMatrixLayout.ColMajor, side, fill, opMkl, diag, mm, nn, *(double*)&α, pA, llda, pB, lldb);
					return true;
				case DataType.ComplexSingle:
					func = &NM.cblas_ctrmm;
					break;
				case DataType.ComplexDouble:
					func = &NM.cblas_ztrmm;
					break;
				default:
					return false;
			}
			func(MklMatrixLayout.ColMajor, side, fill, opMkl, diag, mm, nn, &α, pA, llda, pB, lldb);
			return true;
		}

		protected override unsafe bool GeneralMatricesAdd_<T>(MatrixOperation opA, MatrixOperation opB, long m, long n, T α, Storage<T>? A, long lda, T β, Storage<T>? B, long ldb, Storage<T> C, long ldc)
		{
			if (!CheckPointer(A, opA, m, n, lda, out var opcA, out var pA, out _, out _, out int llda))
				return false;
			if (!CheckPointer(B, opB, m, n, ldb, out var opcB, out var pB, out _, out _, out int lldb))
				return false;
			if (!CheckPointer(C, m, n, ldc, out var pC, out int mm, out int nn, out int lldc))
				return false;
			// shortcut
			if ((A is null || α.IsZero()) || (B is null || β.IsZero()))
			{
				if ((A is null || α.IsZero()) && opB == MatrixOperation.None && β.IsOne())
				{   // copy B to C
					Storage.API.PointerMemoryCopy2D(pC, ldc * sizeof(T), pB, ldb * sizeof(T), m * sizeof(T), n);
					return true;
				}
				if ((B is null || β.IsZero()) && opA == MatrixOperation.None && α.IsOne())
				{   // copy A to C
					Storage.API.PointerMemoryCopy2D(pC, ldc * sizeof(T), pA, lda * sizeof(T), m * sizeof(T), n);
					return true;
				}
				// matrix copy
				if (A is null || α.IsZero())
				{
					pA = pB; llda = lldb; α = β; opcA = opcB;
				}
				if (pA != pC)
				{
					var cpyFunc = Const<T>.DataType switch
					{
						DataType.RealSingle => new NM.omatcopy<float>(NM.MKL_Somatcopy) as NM.omatcopy<T>,
						DataType.RealDouble => new NM.omatcopy<double>(NM.MKL_Domatcopy) as NM.omatcopy<T>,
						DataType.ComplexSingle => new NM.omatcopy<ComplexSingle>(NM.MKL_Comatcopy) as NM.omatcopy<T>,
						DataType.ComplexDouble => new NM.omatcopy<ComplexDouble>(NM.MKL_Zomatcopy) as NM.omatcopy<T>,
						_ => null,
					};
					if (cpyFunc is null)
						return false;
					cpyFunc(MklMatrixLayoutChar.ColMajor, opcA.ToChar(), mm, nn, α, pA, llda, pC, lldc);
				}
				else
				{
					if (lda != ldc)
						throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(lda));
					var cpyFunc = Const<T>.DataType switch
					{
						DataType.RealSingle => new NM.imatcopy<float>(NM.MKL_Simatcopy) as NM.imatcopy<T>,
						DataType.RealDouble => new NM.imatcopy<double>(NM.MKL_Dimatcopy) as NM.imatcopy<T>,
						DataType.ComplexSingle => new NM.imatcopy<ComplexSingle>(NM.MKL_Cimatcopy) as NM.imatcopy<T>,
						DataType.ComplexDouble => new NM.imatcopy<ComplexDouble>(NM.MKL_Zimatcopy) as NM.imatcopy<T>,
						_ => null,
					};
					if (cpyFunc is null)
						return false;
					cpyFunc(MklMatrixLayoutChar.ColMajor, opcA.ToChar(), mm, nn, α, pA, llda);
				}
			}
			// both matrices are not null
			var func = Const<T>.DataType switch
			{
				DataType.RealSingle => new NM.omatadd<float>(NM.MKL_Somatadd) as NM.omatadd<T>,
				DataType.RealDouble => new NM.omatadd<double>(NM.MKL_Domatadd) as NM.omatadd<T>,
				DataType.ComplexSingle => new NM.omatadd<ComplexSingle>(NM.MKL_Comatadd) as NM.omatadd<T>,
				DataType.ComplexDouble => new NM.omatadd<ComplexDouble>(NM.MKL_Zomatadd) as NM.omatadd<T>,
				_ => null,
			};
			if (func is null)
				return false;
			func(MklMatrixLayoutChar.ColMajor, opcA.ToChar(), opcB.ToChar(), mm, nn, α, pA, llda, β, pB, lldb, pC, lldc);
			return true;
		}

		protected override unsafe bool GeneralMatricesMultiply_<T>(MatrixOperation opA, MatrixOperation opB, long m, long n, long k, T α, Storage<T> A, long lda, Storage<T> B, long ldb, T β, Storage<T> C, long ldc)
		{
			if (!CheckPointer(A, opA, m, k, lda, out var opcA, out var pA, out _, out int kk, out int llda))
				return false;
			if (!CheckPointer(B, opB, k, n, ldb, out var opcB, out var pB, out _, out _, out int lldb))
				return false;
			if (!CheckPointer(C, m, n, ldc, out var pC, out int mm, out int nn, out int lldc))
				return false;

			delegate*<MklMatrixLayout, MklOperation, MklOperation, int, int, int, T*, IntPtr, int, IntPtr, int, T*, IntPtr, int, void> func;
			switch (Const<T>.DataType)
			{
				case DataType.RealSingle:
					NM.cblas_sgemm(MklMatrixLayout.ColMajor, opcA, opcB, mm, nn, kk, *(float*)&α, pA, llda, pB, lldb, *(float*)&β, pC, lldc);
					return true;
				case DataType.RealDouble:
					NM.cblas_dgemm(MklMatrixLayout.ColMajor, opcA, opcB, mm, nn, kk, *(double*)&α, pA, llda, pB, lldb, *(double*)&β, pC, lldc);
					return true;
				case DataType.ComplexSingle:
					func = this.ComplexGemmUseGemm3m ? &NM.cblas_cgemm3m : &NM.cblas_cgemm;
					break;
				case DataType.ComplexDouble:
					func = this.ComplexGemmUseGemm3m ? &NM.cblas_zgemm3m : &NM.cblas_zgemm;
					break;
				default:
					return false;
			}
			func(MklMatrixLayout.ColMajor, opcA, opcB, mm, nn, kk, &α, pA, llda, pB, lldb, &β, pC, lldc);
			return true;
		}

		protected override unsafe bool SymmHermMatrixMultiplyGeneral_<T>(bool fillUpper, bool leftA, bool hermA, long m, long n, T α, Storage<T> A, long lda, Storage<T> B, long ldb, T β, Storage<T> C, long ldc)
		{
			if (!CheckPointer(A, leftA ? m : n, leftA ? m : n, lda, out var pA, out _, out _, out int llda))
				return false;
			if (!CheckPointer(B, m, n, ldb, out var pB, out _, out _, out int lldb))
				return false;
			if (!CheckPointer(C, m, n, ldc, out var pC, out int mm, out int nn, out int lldc))
				return false;

			var side = leftA ? MklBlasSideMode.Left : MklBlasSideMode.Right;
			var fill = fillUpper ? MklFillMode.Upper : MklFillMode.Lower;
			delegate*<MklMatrixLayout, MklBlasSideMode, MklFillMode, int, int, T*, IntPtr, int, IntPtr, int, T*, IntPtr, int, void> func;
			switch (Const<T>.DataType)
			{
				case DataType.RealSingle:
					NM.cblas_ssymm(MklMatrixLayout.ColMajor, side, fill, mm, nn, *(float*)&α, pA, llda, pB, lldb, *(float*)&β, pC, lldc);
					return true;
				case DataType.RealDouble:
					NM.cblas_dsymm(MklMatrixLayout.ColMajor, side, fill, mm, nn, *(double*)&α, pA, llda, pB, lldb, *(double*)&β, pC, lldc);
					return true;
				case DataType.ComplexSingle:
					func = hermA ? &NM.cblas_csymm : &NM.cblas_chemm;
					break;
				case DataType.ComplexDouble:
					func = hermA ? &NM.cblas_zsymm : &NM.cblas_zhemm;
					break;
				default:
					return false;
			}
			func(MklMatrixLayout.ColMajor, side, fill, mm, nn, &α, pA, llda, pB, lldb, &β, pC, lldc);
			return true;
		}

		protected override unsafe bool RankKUpdate_<T>(bool fillUpper, MatrixOperation op, bool conjA, long n, long k, T α, Storage<T> A, long lda, T β, Storage<T> C, long ldc)
		{
			if (!CheckPointer(A, op, n, k, lda, out var opMkl, out var pA, out int nn, out int kk, out int llda))
				return false;
			if (!CheckPointer(C, n, n, ldc, out var pC, out _, out _, out int lldc))
				return false;

			var fill = fillUpper ? MklFillMode.Upper : MklFillMode.Lower;
			delegate*<MklMatrixLayout, MklFillMode, MklOperation, int, int, T*, IntPtr, int, T*, IntPtr, int, void> func;
			switch (Const<T>.DataType)
			{
				case DataType.RealSingle:
					NM.cblas_ssyrk(MklMatrixLayout.ColMajor, fill, opMkl, nn, kk, *(float*)&α, pA, llda, *(float*)&β, pC, lldc);
					return true;
				case DataType.RealDouble:
					NM.cblas_dsyrk(MklMatrixLayout.ColMajor, fill, opMkl, nn, kk, *(double*)&α, pA, llda,  *(double*)&β, pC, lldc);
					return true;
				case DataType.ComplexSingle:
					func = conjA ? &NM.cblas_cherk : &NM.cblas_csyrk;
					break;
				case DataType.ComplexDouble:
					func = conjA ? &NM.cblas_zherk : &NM.cblas_zsyrk;
					break;
				default:
					return false;
			}
			func(MklMatrixLayout.ColMajor, fill, opMkl, nn, kk, &α, pA, llda, &β, pC, lldc);
			return true;
		}

		protected override unsafe bool RankTwoKUpdate_<T>(bool fillUpper, MatrixOperation op, bool conjugate, long n, long k, T α, Storage<T> A, long lda, Storage<T> B, long ldb, T β, Storage<T> C, long ldc)
		{
			if (!CheckPointer(A, op, n, k, lda, out var opMkl, out var pA, out int nn, out int kk, out int llda))
				return false;
			if (!CheckPointer(B, op, n, k, lda, out _, out var pB, out _, out _, out int lldb))
				return false;
			if (!CheckPointer(C, n, n, ldc, out var pC, out _, out _, out int lldc))
				return false;

			var fill = fillUpper ? MklFillMode.Upper : MklFillMode.Lower;
			delegate*<MklMatrixLayout, MklFillMode, MklOperation, int, int, T*, IntPtr, int, IntPtr, int, T*, IntPtr, int, void> func;
			switch (Const<T>.DataType)
			{
				case DataType.RealSingle:
					NM.cblas_ssyr2k(MklMatrixLayout.ColMajor, fill, opMkl, nn, kk, *(float*)&α, pA, llda, pB, lldb, *(float*)&β, pC, lldc);
					return true;
				case DataType.RealDouble:
					NM.cblas_dsyr2k(MklMatrixLayout.ColMajor, fill, opMkl, nn, kk, *(double*)&α, pA, llda, pB, lldb, *(double*)&β, pC, lldc);
					return true;
				case DataType.ComplexSingle:
					func = conjugate ? &NM.cblas_cher2k : &NM.cblas_csyr2k;
					break;
				case DataType.ComplexDouble:
					func = conjugate ? &NM.cblas_zher2k : &NM.cblas_zsyr2k;
					break;
				default:
					return false;
			}
			func(MklMatrixLayout.ColMajor, fill, opMkl, nn, kk, &α, pA, llda, pB, lldb, &β, pC, lldc);
			return true;
		}

		protected override unsafe bool RankKUpdateVariant_<T>(bool fillUpper, MatrixOperation op, bool conjB, long n, long k, T α, Storage<T> A, long lda, Storage<T> B, long ldb, T β, Storage<T> C, long ldc)
		{
			return false;
		}
		#endregion


		#region custom level 3
		protected override bool MatrixCopyUpperLowerParts_<T>(bool storedUpper, bool hermitian, long n, Storage<T> A, long lda)
		{
			if (!Const<T>.IsPreDefinedNoHalf)
				return false;
			if (!CheckPointerLong(A, n, lda, out var pA))
				return false;
			NM.matMakeHerm(Const<T>.DataType, pA, lda, n, storedUpper, hermitian);
			return true;
		}

		protected override bool MatrixClearUpperLowerPart_<T>(bool clearLower, long n, Storage<T> A, long lda)
		{
			if (!Const<T>.IsPreDefinedNoHalf)
				return false;
			if (!CheckPointerLong(A, n, lda, out var pA))
				return false;
			NM.matTriClear(Const<T>.DataType, pA, lda, n, clearLower);
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
			NM.matKron(Const<T>.DataType, pA, lda, ma, na, pB, ldb, mb, nb, pC, ldc, &α, &β);
			return true;
		}
		#endregion


		#region solve
		#region linear solve
		protected override unsafe bool LinearSolve_<T, TInd>(MatrixOperation op, long n, long nrhs, Storage<T> A, long lda, Storage<T> B, long ldb, Storage<TInd>? work = null)
		{
			if (!CheckPointer(A, n, n, lda, out var pA, out int nn, out _, out int llda))
				return false;
			if (!CheckPointer(B, n, nrhs, ldb, out var pB, out _, out int nnrhs, out int lldb))
				return false;
			if (!CheckPointer(work, out var pW, out int nw))
				return false;
			////if (nw > 0 && nw < nn)
			////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(work));
			
			delegate*<MklMatrixLayout, int, int, IntPtr, int, IntPtr, IntPtr, int, MklLapackInfo> func;
			func = Const<T>.DataType switch
			{
				DataType.RealSingle => &NM.LAPACKE_sgesv,
				DataType.RealDouble => &NM.LAPACKE_dgesv,
				DataType.ComplexSingle => &NM.LAPACKE_cgesv,
				DataType.ComplexDouble => &NM.LAPACKE_zgesv,
				_ => null,
			};
			if (func is null)
				return false;
			// calculate
			IntPtr tau;
			if (pW == default)
				tau = Marshal.AllocHGlobal((IntPtr)(n * sizeof(T)));
			else
				tau = pW;
			try
			{
				var info = func(MklMatrixLayout.ColMajor, nn, nnrhs, pA, llda, tau, pB, lldb);
				SolveMethodKind.LU.CheckLapackInfo(info);
				return true;
			}
			finally
			{
				if (tau != pW)
					Marshal.FreeHGlobal(tau);
			}
		}
		#endregion

		#region QR
		// modify these methods to use a unified buffer for (maybe) better performance
		protected override unsafe bool LeastSquareSolve_<T>(long m, long n, long nrhs, Storage<T> A, long lda, Storage<T> B, long ldb, Storage<T>? work = null)
		{
			if (!CheckPointer(A, m, n, lda, out var pA, out int mm, out int nn, out int llda))
				return false;
			if (!CheckPointer(B, n, nrhs, ldb, out var pB, out _, out int nnrhs, out int lldb))
				return false;

			delegate*<MklMatrixLayout, MklOperationChar, int, int, int, IntPtr, int, IntPtr, int, MklLapackInfo> func;
			func = Const<T>.DataType switch
			{
				DataType.RealSingle => &NM.LAPACKE_sgels,
				DataType.RealDouble => &NM.LAPACKE_dgels,
				DataType.ComplexSingle => &NM.LAPACKE_cgels,
				DataType.ComplexDouble => &NM.LAPACKE_zgels,
				_ => null,
			};
			if (func is null)
				return false;
			var info = func(MklMatrixLayout.ColMajor, MklOperationChar.NoneTranspose, mm, nn, nnrhs, pA, llda, pB, lldb);
			SolveMethodKind.QR.CheckLapackInfo(info);
			return true;
		}

		protected override unsafe bool QRDecomposition_<T>(bool full, long m, long n, Storage<T> A, long lda, Storage<T> Q, long ldq, Storage<T>? work = null)
		{
			if (!Const<T>.DataType.CheckBaseSupport())
				return false;
			if (!CheckPointer(A, m, n, lda, out var pA, out int mm, out int nn, out int llda))
				return false;
			int kk = Math.Min(mm, nn); long colsQ = full ? m : kk;
			if (!CheckPointer(Q, m, colsQ, ldq, out var pQ, out _, out int nnQ, out int lldq))
				return false;
			if (!CheckPointer(work, out var pW, out int nw))
				return false;
			////if (nw > 0 && nw < nn)
			////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(work));

			delegate*<MklMatrixLayout, int, int, IntPtr, int, IntPtr, MklLapackInfo> qrfunc;
			qrfunc = Const<T>.DataType switch
			{
				DataType.RealSingle => &NM.LAPACKE_sgeqrf,
				DataType.RealDouble => &NM.LAPACKE_dgeqrf,
				DataType.ComplexSingle => &NM.LAPACKE_cgeqrf,
				DataType.ComplexDouble => &NM.LAPACKE_zgeqrf,
				_ => null,
			};
			if (qrfunc is null)
				return false;
			delegate*<MklMatrixLayout, int, int, int, IntPtr, int, IntPtr, MklLapackInfo> gqfunc;
			gqfunc = Const<T>.DataType switch
			{
				DataType.RealSingle => &NM.LAPACKE_sorgqr,
				DataType.RealDouble => &NM.LAPACKE_dorgqr,
				DataType.ComplexSingle => &NM.LAPACKE_cungqr,
				DataType.ComplexDouble => &NM.LAPACKE_zungqr,
				_ => null,
			};
			// calculate
			IntPtr tau;
			if (pW == default)
				tau = Marshal.AllocHGlobal((IntPtr)(kk * sizeof(T)));
			else
				tau = pW;
			try
			{
				// implicit QR
				var info = qrfunc(MklMatrixLayout.ColMajor, mm, nn, pA, llda, tau);
				SolveMethodKind.QR.CheckLapackInfo(info);
				// copy A to Q
				Storage.API.PointerMemoryCopy2D(pA, lda, pQ, ldq,  m, Math.Min(colsQ, n));
				// form Q
				info = gqfunc(MklMatrixLayout.ColMajor, mm, nnQ, kk, pQ, lldq, tau);
				SolveMethodKind.QR.CheckLapackInfo(info);
				return true;
			}
			finally
			{
				if (tau != pW)
					Marshal.FreeHGlobal(tau);
			}
		}
		#endregion

		#region simple eigen
		protected override unsafe bool EigenSpecialMatrixHermitian_<T, TReal>(SolveVectorMode mode, long n, Storage<TReal> valOut, Storage<T> A, long lda)
		{
			if ((!Const<T>.IsComplex && typeof(T) != typeof(TReal)) ||
				(Const<T>.IsComplex && (Const<TReal>.IsComplex || typeof(T).GenericTypeArguments[0] != typeof(TReal))))
				throw new TypeMismatchException(typeof(T), typeof(TReal), TypeMismatchException.MismatchReason.IsNotRealCorrespondence);

			if (!CheckPointer(A, n, n, lda, out var pA, out int nn, out _, out int llda))
				return false;
			if (!CheckPointer(valOut, out var pV, out int nv))
				return false;
			////if (nv < nn)
			////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(valOut));

			delegate*<MklMatrixLayout, MklVectorModeChar, MklFillModeChar, int, IntPtr, int, IntPtr, MklLapackInfo> func;
			func = Const<T>.DataType switch
			{
				DataType.RealSingle => &NM.LAPACKE_ssyev,
				DataType.RealDouble => &NM.LAPACKE_dsyev,
				DataType.ComplexSingle => &NM.LAPACKE_cheev,
				DataType.ComplexDouble => &NM.LAPACKE_zheev,
				_ => null,
			};
			if (func is null)
				return false;
			var info = func(MklMatrixLayout.ColMajor, mode.ToChar(), MklFillModeChar.Upper, nn, pA, llda, pV);
			SolveMethodKind.Eigenvalue.CheckLapackInfo(info);
			return true;
		}

		protected override unsafe bool EigenGeneralMatrixHermitian_<T, TReal>(GeneralEigenType eigType, SolveVectorMode mode, long n, Storage<TReal> valOut, Storage<T> A, long lda, Storage<T> B, long ldb)
		{
			if ((!Const<T>.IsComplex && typeof(T) != typeof(TReal)) ||
				(Const<T>.IsComplex && (Const<TReal>.IsComplex || typeof(T).GenericTypeArguments[0] != typeof(TReal))))
				throw new TypeMismatchException(typeof(T), typeof(TReal), TypeMismatchException.MismatchReason.IsNotRealCorrespondence);

			if (!CheckPointer(A, n, n, lda, out var pA, out int nn, out _, out int llda))
				return false;
			if (!CheckPointer(B, n, n, ldb, out var pB, out _, out _, out int lldb))
				return false;
			if (!CheckPointer(valOut, out var pV, out int nv))
				return false;
			////if (nv < nn)
			////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(valOut));

			delegate*<MklMatrixLayout, GeneralEigenType, MklVectorModeChar, MklFillModeChar, int, IntPtr, int, IntPtr, int, IntPtr, MklLapackInfo> func;
			func = Const<T>.DataType switch
			{
				DataType.RealSingle => &NM.LAPACKE_ssygv,
				DataType.RealDouble => &NM.LAPACKE_dsygv,
				DataType.ComplexSingle => &NM.LAPACKE_chegv,
				DataType.ComplexDouble => &NM.LAPACKE_zhegv,
				_ => null,
			};
			if (func is null)
				return false;
			var info = func(MklMatrixLayout.ColMajor, eigType, mode.ToChar(), MklFillModeChar.Upper, nn, pA, llda, pB, lldb, pV);
			SolveMethodKind.GeneralEigen.CheckLapackInfo(info);
			return true;
		}
		#endregion

		#region general eigen
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private unsafe bool CopyEigenToComplex<T, TComplex>(MklVectorModeChar modeL, MklVectorModeChar modeR, long n, int nn, IntPtr pV, T* valR, T* valI, Storage<TComplex>? leftVec, IntPtr pVl, long ldvl, T* vecL, Storage<TComplex>? rightVec, IntPtr pVr, long ldvr, T* vecR) where T : unmanaged, INumber<T> where TComplex : unmanaged
		{
			// copy eigenvalues
			Storage.API.PointerStridedCopy(valR, 1, (T*)pV, 2, nn);
			Storage.API.PointerStridedCopy(valI, 1, 1 + (T*)pV, 2, nn);
			// expand cases for better performance
			float* floatValI = (float*)valI; double* doubleValI = (double*)valI;
			if (leftVec is not null && rightVec is not null)
			{
				// set eigenvectors to zeros
				if (!this.GeneralMatricesAdd_(default, default, n, n, default, leftVec, ldvl, default, default, default, leftVec, ldvl))
					return false;
				if (!this.GeneralMatricesAdd_(default, default, n, n, default, rightVec, ldvr, default, default, default, rightVec, ldvr))
					return false;
				ldvl *= 2; ldvr *= 2;
				// copy eigenvectors
				for (int i = 0; i < nn; i++)
				{
					// copy real parts in both cases
					Storage.API.PointerStridedCopy(vecL + n * i, 1, (T*)pVl + i * ldvl, 2, nn);
					Storage.API.PointerStridedCopy(vecR + n * i, 1, (T*)pVr + i * ldvr, 2, nn);
					// check real or complex eigen-pair
					if ((typeof(T) == typeof(float) && floatValI[i] != 0) || (typeof(T) == typeof(double) && doubleValI[i] != 0))
					{   // the i-th and (i+1)-th eigen-pairs are complex conjugate pairs
						// left
						T* ptr = (T*)pVl + (i * ldvl + 1), ptr2 = ptr + ldvl;
						Storage.API.PointerStridedCopy(vecL + n * (i + 1), 1, ptr, 2, nn);
						Storage.API.PointerStridedCopy(vecL + n * (i + 1), 1, ptr2, 2, nn);
						if (typeof(T) == typeof(float))
							NM.cblas_sscal(nn, -1, (IntPtr)ptr2, 2);
						else
							NM.cblas_dscal(nn, -1, (IntPtr)ptr2, 2);
						// right
						ptr = (T*)pVr + (i * ldvr + 1); ptr2 = ptr + ldvr;
						Storage.API.PointerStridedCopy(vecR + n * (i + 1), 1, ptr, 2, nn);
						Storage.API.PointerStridedCopy(vecR + n * (i + 1), 1, ptr2, 2, nn);
						if (typeof(T) == typeof(float))
							NM.cblas_sscal(nn, -1, (IntPtr)ptr2, 2);
						else
							NM.cblas_dscal(nn, -1, (IntPtr)ptr2, 2);
					}
				}
			}
			else if (leftVec is not null && rightVec is null)
			{
				// set eigenvectors to zeros
				if (!this.GeneralMatricesAdd_(default, default, n, n, default, leftVec, ldvl, default, default, default, leftVec, ldvl))
					return false;
				ldvl *= 2; ldvr *= 2;
				// copy eigenvectors
				for (int i = 0; i < nn; i++)
				{
					// copy real parts in both cases
					Storage.API.PointerStridedCopy(vecL + n * i, 1, (T*)pVl + i * ldvl, 2, nn);
					// check real or complex eigen-pair
					if ((typeof(T) == typeof(float) && floatValI[i] != 0) || (typeof(T) == typeof(double) && doubleValI[i] != 0))
					{   // the i-th and (i+1)-th eigen-pairs are complex conjugate pairs
						T* ptr = (T*)pVl + (i * ldvl + 1), ptr2 = ptr + ldvl;
						Storage.API.PointerStridedCopy(vecL + n * (i + 1), 1, ptr, 2, nn);
						Storage.API.PointerStridedCopy(vecL + n * (i + 1), 1, ptr2, 2, nn);
						if (typeof(T) == typeof(float))
							NM.cblas_sscal(nn, -1, (IntPtr)ptr2, 2);
						else
							NM.cblas_dscal(nn, -1, (IntPtr)ptr2, 2);
					}
				}
			}
			else if (leftVec is null && rightVec is not null)
			{
				// set eigenvectors to zeros
				if (!this.GeneralMatricesAdd_(default, default, n, n, default, rightVec, ldvr, default, default, default, rightVec, ldvr))
					return false;
				ldvl *= 2; ldvr *= 2;
				// copy eigenvectors
				for (int i = 0; i < nn; i++)
				{
					// copy real parts in both cases
					Storage.API.PointerStridedCopy(vecR + n * i, 1, (T*)pVr + i * ldvr, 2, nn);
					// check real or complex eigen-pair
					if ((typeof(T) == typeof(float) && floatValI[i] != 0) || (typeof(T) == typeof(double) && doubleValI[i] != 0))
					{   // the i-th and (i+1)-th eigen-pairs are complex conjugate pairs
						T* ptr = (T*)pVr + (i * ldvr + 1), ptr2 = ptr + ldvr;
						Storage.API.PointerStridedCopy(vecR + n * (i + 1), 1, ptr, 2, nn);
						Storage.API.PointerStridedCopy(vecR + n * (i + 1), 1, ptr2, 2, nn);
						if (typeof(T) == typeof(float))
							NM.cblas_sscal(nn, -1, (IntPtr)ptr2, 2);
						else
							NM.cblas_dscal(nn, -1, (IntPtr)ptr2, 2);
					}
				}
			}
			else
			{
				// no copy
			}
			return true;
			
			////// set eigenvectors to zeros
			////if (leftVec is not null)
			////{
			////	if (!this.GeneralMatricesAdd_(default, default, n, n, default, leftVec, ldvl, default, default, default, leftVec, ldvl))
			////		return false;
			////}
			////if (rightVec is not null)
			////{
			////	if (!this.GeneralMatricesAdd_(default, default, n, n, default, rightVec, ldvr, default, default, default, rightVec, ldvr))
			////		return false;
			////}
			////ldvl *= 2; ldvr *= 2;
			////// copy eigenvectors
			////for (int i = 0; i < nn; i++)
			////{
			////	// copy real parts in both cases
			////	if (leftVec is not null)
			////	{
			////		Storage.StorageApi.PointerStridedCopy(vecL + n * i, 1, (T*)pVl + i * ldvl, 2, nn);
			////	}
			////	if (rightVec is not null)
			////	{
			////		Storage.StorageApi.PointerStridedCopy(vecR + n * i, 1, (T*)pVr + i * ldvr, 2, nn);
			////	}
			////	// check real or complex eigen-pair
			////	if (valI[i].IsZero())
			////	{   // the i-th eigen-pair is real
			////		// do nothing
			////	}
			////	else
			////	{   // the i-th and (i+1)-th eigen-pairs are complex conjugate pairs
			////		if (leftVec is not null)
			////		{
			////			T* ptr = (T*)pVl + (i * ldvl + 1), ptr2 = ptr + ldvl;
			////			Storage.StorageApi.PointerStridedCopy(vecL + n * (i + 1), 1, ptr, 2, nn);
			////			Storage.StorageApi.PointerStridedCopy(vecL + n * (i + 1), 1, ptr2, 2, nn);
			////			if (typeof(T) == typeof(float))
			////				NativeMethods.cblas_sscal(nn, -1, (IntPtr)ptr2, 2);
			////			else
			////				NativeMethods.cblas_dscal(nn, -1, (IntPtr)ptr2, 2);
			////		}
			////		if (rightVec is not null)
			////		{
			////			T* ptr = (T*)pVr + (i * ldvr + 1), ptr2 = ptr + ldvr;
			////			Storage.StorageApi.PointerStridedCopy(vecR + n * (i + 1), 1, ptr, 2, nn);
			////			Storage.StorageApi.PointerStridedCopy(vecR + n * (i + 1), 1, ptr2, 2, nn);
			////			if (typeof(T) == typeof(float))
			////				NativeMethods.cblas_sscal(nn, -1, (IntPtr)ptr2, 2);
			////			else
			////				NativeMethods.cblas_dscal(nn, -1, (IntPtr)ptr2, 2);
			////		}
			////	}
			////}
			
		}

		protected override unsafe bool EigenSpecialMatrixGeneral_<T, TComplex>(SolveVectorMode mode, long n, Storage<TComplex> valOut, Storage<TComplex>? leftVec, long ldvl, Storage<TComplex>? rightVec, long ldvr, Storage<T> A, long lda)
		{
			if ((Const<T>.IsComplex && typeof(T) != typeof(TComplex)) ||
				(!Const<T>.IsComplex && (!Const<TComplex>.IsComplex || typeof(T) != typeof(TComplex).GenericTypeArguments[0])))
				throw new TypeMismatchException(typeof(T), typeof(TComplex), TypeMismatchException.MismatchReason.IsNotComplexCorrespondence);

			if (!CheckPointer(A, n, n, lda, out var pA, out int nn, out _, out int llda))
				return false;
			if (!CheckPointer(leftVec, n, n, ldvl, out var pVl, out _, out _, out int lldvl))
				return false;
			if (!CheckPointer(rightVec, n, n, ldvr, out var pVr, out _, out _, out int lldvr))
				return false;
			if (!CheckPointer(valOut, out var pV, out int nv))
				return false;
			////if (nv < nn)
			////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(valOut));

			delegate*<MklMatrixLayout, MklVectorModeChar, MklVectorModeChar, int, IntPtr, int, T*, T*, T*, int, T*, int, MklLapackInfo> funcR = null;
			delegate*<MklMatrixLayout, MklVectorModeChar, MklVectorModeChar, int, IntPtr, int, IntPtr, IntPtr, int, IntPtr, int, MklLapackInfo> funcC = null;
			if (typeof(T) == typeof(float))
			{
				funcR = &NM.LAPACKE_sgeev;
			}
			else if (typeof(T) == typeof(double))
			{
				funcR = &NM.LAPACKE_dgeev;
			}
			else
			{
				switch (Const<T>.DataType)
				{
					case DataType.ComplexSingle:
						funcC = &NM.LAPACKE_cgeev;
						break;
					case DataType.ComplexDouble:
						funcC = &NM.LAPACKE_zgeev;
						break;
					default:
						break;
				}
			}
			if (funcR is null && funcC is null)
				return false;
			var (modeL, modeR) = mode.ToLRChar();
			if (funcC is not null)
			{	// complex typed T
				var info = funcC(MklMatrixLayout.ColMajor, modeL, modeR, nn, pA, llda, pV, pVl, lldvl, pVr, lldvr);
				SolveMethodKind.GeneralEigen.CheckLapackInfo(info);
				return true;
			}
			// real typed T
			// buffer
			using var buffer = CpuBuffer.Create((2 * n + (modeL == MklVectorModeChar.Vector ? n * n : 0) + (modeR == MklVectorModeChar.Vector ? n * n : 0)) * sizeof(T));
			fixed (byte* buf = buffer.Buffer)
			{
				T* valR = (T*)buf, valI = (T*)buf + n, vecL = (T*)buf + 2 * n, vecR = (T*)buf + 2 * n + (modeL == MklVectorModeChar.Vector ? n * n : 0);
				// calculate
				var info = funcR(MklMatrixLayout.ColMajor, modeL, modeR, nn, pA, llda, valR, valI, vecL, lldvl, vecR, lldvr);
				SolveMethodKind.NonSymmetricEigenvalue.CheckLapackInfo(info);
				// copy
				return this.CopyEigenToComplex(modeL, modeR, n, nn, pV, valR, valI, leftVec, pVl, ldvl, vecL, rightVec, pVr, ldvr, vecR);
			}
		}

		protected override unsafe bool EigenGeneralMatrixGeneral_<T, TComplex>(GeneralEigenType type, SolveVectorMode mode, long n, Storage<TComplex> α, Storage<T> β, Storage<TComplex>? leftVec, long ldvl, Storage<TComplex>? rightVec, long ldvr, Storage<T> A, long lda, Storage<T> B, long ldb)
		{
			if ((Const<T>.IsComplex && typeof(T) != typeof(TComplex)) ||
				(!Const<T>.IsComplex && (!Const<TComplex>.IsComplex || typeof(T) != typeof(TComplex).GenericTypeArguments[0])))
				throw new TypeMismatchException(typeof(T), typeof(TComplex), TypeMismatchException.MismatchReason.IsNotComplexCorrespondence);

			if (type != GeneralEigenType.Type1)
				return false;
			if (!CheckPointer(A, n, n, lda, out var pA, out int nn, out _, out int llda))
				return false;
			if (!CheckPointer(B, n, n, ldb, out var pB, out _, out _, out int lldb))
				return false;
			if (!CheckPointer(leftVec, n, n, ldvl, out var pVl, out _, out _, out int lldvl))
				return false;
			if (!CheckPointer(rightVec, n, n, ldvr, out var pVr, out _, out _, out int lldvr))
				return false;
			if (!CheckPointer(α, out var pVa, out int nva))
				return false;
			if (!CheckPointer(β, out var pVb, out int nvb))
				return false;
			////if (nva < nn)
			////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(α));
			////if (nvb < nn)
			////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(β));

			delegate*<MklMatrixLayout, MklVectorModeChar, MklVectorModeChar, int, IntPtr, int, IntPtr, int, T*, T*, IntPtr, T*, int, T*, int, MklLapackInfo> funcR = null;
			delegate*<MklMatrixLayout, MklVectorModeChar, MklVectorModeChar, int, IntPtr, int, IntPtr, int, IntPtr, IntPtr, IntPtr, int, IntPtr, int, MklLapackInfo> funcC = null;
			if (typeof(T) == typeof(float))
			{
				funcR = &NM.LAPACKE_sggev;
			}
			else if (typeof(T) == typeof(double))
			{
				funcR = &NM.LAPACKE_dggev;
			}
			else
			{
				if (Const<T>.DataType == DataType.ComplexSingle)
					funcC = &NM.LAPACKE_cggev;
				else if (Const<T>.DataType == DataType.ComplexDouble)
					funcC = &NM.LAPACKE_zggev;
			}
			if (funcR is null && funcC is null)
				return false;
			var (modeL, modeR) = mode.ToLRChar();
			if (funcC is not null)
			{   // complex typed T
				var info = funcC(MklMatrixLayout.ColMajor, modeL, modeR, nn, pA, llda, pB, lldb, pVa, pVb, pVl, lldvl, pVr, lldvr);
				SolveMethodKind.GeneralEigen.CheckLapackInfo(info);
				return true;
			}
			// real typed T
			// buffer
			using var buffer = CpuBuffer.Create((2 * n + (modeL == MklVectorModeChar.Vector ? n * n : 0) + (modeR == MklVectorModeChar.Vector ? n * n : 0)) * sizeof(T));
			fixed (byte* buf = buffer.Buffer)
			{
				T* valR = (T*)buf, valI = (T*)buf + n, vecL = (T*)buf + 2 * n, vecR = (T*)buf + 2 * n + (modeL == MklVectorModeChar.Vector ? n * n : 0);
				// calculate
				var info = funcR(MklMatrixLayout.ColMajor, modeL, modeR, nn, pA, llda, pB, lldb, valR, valI, pVb, vecL, lldvl, vecR, lldvr);
				SolveMethodKind.NonSymmetricGenearlEigenvalue.CheckLapackInfo(info);
				// copy
				return this.CopyEigenToComplex(modeL, modeR, n, nn, pVa, valR, valI, leftVec, pVl, ldvl, vecL, rightVec, pVr, ldvr, vecR);
			}
		}
		#endregion

		#region other decompose
		protected override unsafe bool SingularValues_<T, TReal>(SVDStore storeU, SVDStore storeV, long m, long n, Storage<T> A, long lda, Storage<TReal> S, Storage<T>? U, long ldu, Storage<T>? Vct, long ldvct)
		{
			if (storeU == SVDStore.Overwrite && storeV == SVDStore.Overwrite)
				throw new ArgumentException(Resources.Parameter.DuplicateValue, nameof(storeU));

			MklSvdModeChar jobU = storeU.ToChar(), jobV = storeV.ToChar();
			if (jobU == 0 || jobV == 0)
				return false;
			if (!CheckPointer(A, m, n, lda, out var pA, out int mm, out int nn, out int llda))
				return false;
			int kk = Math.Min(mm, nn);
			if (!CheckPointer(U, storeU == SVDStore.All ? m : kk, m, ldu, out var pU, out int mmU, out _, out int lldu))
				return false;
			if (!CheckPointer(Vct, n, storeV == SVDStore.All ? n : kk, ldvct, out var pV, out _, out int nnV, out int lldv))
				return false;
			if (!CheckPointer(S, out var pS, out int ns))
				return false;
			////if (ns < kk)
			////	throw new ArgumentException(Resources.Parameter.WrongSize, nameof(S));

			delegate*<MklMatrixLayout, MklSvdModeChar, MklSvdModeChar, int, int, IntPtr, int, IntPtr, IntPtr, int, IntPtr, int, byte[], MklLapackInfo> func;
			func = Const<T>.DataType switch
			{
				DataType.RealSingle => &NM.LAPACKE_sgesvd,
				DataType.RealDouble => &NM.LAPACKE_dgesvd,
				DataType.ComplexSingle => &NM.LAPACKE_cgesvd,
				DataType.ComplexDouble => &NM.LAPACKE_zgesvd,
				_ => null,
			};
			if (func is null)
				return false;
			using var buffer = CpuBuffer.Create<T>(Const<T>.IsComplex ? kk : (kk / 2));
			var info = func(MklMatrixLayout.ColMajor, jobU, jobV, mm, nn, pA, llda, pS, pU, lldu, pV, lldv, buffer.Buffer);
			SolveMethodKind.GeneralEigen.CheckLapackInfo(info);
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static int ApproxIndexOfSingle(ComplexSingle* array, int len, ComplexSingle value)
		{
			for (int i = 0; i < len; i++)
			{
				var diff = array[i] - value;
				float diffMax = Math.Max(Math.Abs(diff.Real), Math.Abs(diff.Imag));
				float max = Math.Max(Math.Abs(array[i].Real), Math.Abs(array[i].Imag));
				if (diffMax / max < 0.00007011098358136203F)
					return i;
			}
			return -1;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static int ApproxIndexOfDouble(ComplexDouble* array, int len, ComplexDouble value)
		{
			for (int i = 0; i < len; i++)
			{
				var diff = array[i] - value;
				double diffMax = Math.Max(Math.Abs(diff.Real), Math.Abs(diff.Imag));
				double max = Math.Max(Math.Abs(array[i].Real), Math.Abs(array[i].Imag));
				if (diffMax / max < 5.477420592293901E-7)
					return i;
			}
			return -1;
		}

		protected override unsafe bool SchurDecomposition_<T>(SolveVectorMode jobu, long n, Storage<T> A, long lda, Storage<T>? U, long ldu, out long actualNumber, Storage<ComplexDouble>? orderVal = null)
		{
			actualNumber = 0;
			if (!CheckPointer(A, n, n, lda, out var pA, out int nn, out _, out int llda))
				return false;
			if (!CheckPointer(U, n, n, ldu, out var pU, out _, out _, out int lldu))
				return false;
			if (!CheckPointer(orderVal, out var pO, out int orderLen))
				return false;
			if (orderLen >= n)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(orderVal));

			delegate*<MklMatrixLayout, MklVectorModeChar, MklSortModeChar, NM.SchurSelect2?, int, IntPtr, int, out int, T*, T*, IntPtr, int, MklLapackInfo> funcR = null;
			delegate*<MklMatrixLayout, MklVectorModeChar, MklSortModeChar, NM.SchurSelect1?, int, IntPtr, int, out int, T*, IntPtr, int, MklLapackInfo> funcC = null;
			if (typeof(T) == typeof(float))
			{
				funcR = &NM.LAPACKE_sgees;
			}
			else if (typeof(T) == typeof(double))
			{
				funcR = &NM.LAPACKE_dgees;
			}
			else
			{
				if (Const<T>.DataType == DataType.ComplexSingle)
					funcC = &NM.LAPACKE_cgees;
				else if (Const<T>.DataType == DataType.ComplexDouble)
					funcC = &NM.LAPACKE_zgees;
			}
			if (funcR is null && funcC is null)
				return false;

			var mode = jobu.ToChar();
			var sort = orderVal is null ? MklSortModeChar.NoSort : MklSortModeChar.Sort;
			int getEigNumber;
			MklLapackInfo info;
			using var buffer = Const<T>.IsComplex ? CpuBuffer.Create<T>(nn + orderLen) : CpuBuffer.Create<T>((nn + orderLen) * 2);
			fixed (byte* buf = buffer.Buffer)
			{
				if (funcC is not null)
				{
					// calculate
					NM.SchurSelect1? selector;
					if (orderVal is null)
					{
						selector = null;
					}
					else if (Const<T>.DataType == DataType.ComplexSingle)
					{
						// covert to correct type
						ComplexSingle* selectValues = (ComplexSingle*)buf + n;
						CSharp.LinearAlgebra.Api.PointWiseCast(orderVal, new ManagedPureStorage<ComplexSingle>(selectValues, orderLen));
						// local function
						int Selector(void* pVal)
						{
							ComplexSingle val = *(ComplexSingle*)pVal;
							return ApproxIndexOfSingle(selectValues, orderLen, val);
						}
						selector = Selector;
					}
					else // complex double
					{
						// covert to correct type
						ComplexDouble* selectValues = (ComplexDouble*)buf + n;
						Unsafe.CopyBlockUnaligned(selectValues, (void*)pO, (uint)(n * sizeof(ComplexDouble)));
						// local function
						int Selector(void* pVal)
						{
							ComplexDouble val = *(ComplexDouble*)pVal;
							return ApproxIndexOfDouble(selectValues, orderLen, val);
						}
						selector = Selector;
					}
					info = funcC(MklMatrixLayout.ColMajor, mode, sort, selector, nn, pA, llda, out getEigNumber, (T*)buf, pU, lldu);
				}
				else
				{
					NM.SchurSelect2? selector;
					if (orderVal is null)
					{
						selector = null;
					}
					else if (typeof(T) == typeof(float))
					{
						// covert to correct type
						ComplexSingle* selectValues = (ComplexSingle*)buf + n;
						CSharp.LinearAlgebra.Api.PointWiseCast(orderVal, new ManagedPureStorage<ComplexSingle>(selectValues, orderLen));
						// local function
						int Selector(void* pValR, void* pValI)
						{
							ComplexSingle val = new(*(float*)pValR, *(float*)pValI);
							return ApproxIndexOfSingle(selectValues, orderLen, val);
						}
						selector = Selector;
					}
					else // double
					{
						// covert to correct type
						ComplexDouble* selectValues = (ComplexDouble*)buf + n;
						Unsafe.CopyBlockUnaligned(selectValues, (void*)pO, (uint)(n * sizeof(ComplexDouble)));
						// local function
						int Selector(void* pValR, void* pValI)
						{
							ComplexDouble val = new(*(double*)pValR, *(double*)pValI);
							return ApproxIndexOfDouble(selectValues, orderLen, val);
						}
						selector = Selector;
					}
					info = funcR(MklMatrixLayout.ColMajor, mode, sort, selector, nn, pA, llda, out getEigNumber, (T*)buf, (T*)buf + n, pU, lldu);
				}
			}
			SolveMethodKind.Schur.CheckLapackInfo(info);
			actualNumber = getEigNumber;
			return true;
		}
		#endregion
		#endregion
		*/
	}
}
