using System.Runtime.CompilerServices;
using System.Threading;

using Althea.Backend.Storage;
using Althea.Helpers;
using Althea.LinearAlgebra;
using Althea.LinearAlgebra.Dense;
using Althea.NativeTypes;

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
			if (m != ld)
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
				long min = Math.Min(m, n), max = Math.Max(m, n);
				Storage.Api.PointerStridedCopy(px, strideX, py, strideY, Math.Min(m, n));
				bool actualSquare = op.CanInPlace() ? ((m > n) == fillUpper) : ((n > m) == !fillUpper);
				using var conj = Conjugater.Create(py, actualSquare ? min : m, strideY, ref op);
				func(MklMatrixLayout.ColMajor, fu, op.ToMkl(), ud, min, pA, lda, py, strideY);
				if (actualSquare)
				{
					if (op.CanInPlace() == fillUpper)
						this.FillWithValue(y + min * strideY, strideY, T.Zero);
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
			if (β != T.Zero || (pB == pC && (!opB.CanInPlace() || rowB != m || colB != n || ldb != ldc || rowA != colA)))
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
			if (funcRe == null && funcCm == null)
				return false;
			var lr = leftA ? MklBlasSideMode.Left : MklBlasSideMode.Right;
			var fu = fillUpper ? MklFillMode.Upper : MklFillMode.Lower;
			var ud = unitDiag ? MklBlasDiagType.Unit : MklBlasDiagType.NonUnit;
			bool actualSquare = op.CanInPlace() ? ((m > n) == fillUpper) : ((n > m) == !fillUpper);
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
						this.GeneralMatrixFill(C + minA, ldc, T.Zero, m - minA, n);
					else
						this.GeneralMatrixFill(C + minA * ldc, ldc, T.Zero, m, n - minA);
				}
			}
			/*
			else
			{
				if (op.CanInPlace() == fillUpper)
					this.GeneralMatrixMultiplyVector(op, max - min, min, T.One, A + min * (op.CanInPlace() ? 1 : lda), lda, y + min * strideY, strideY, T.Zero, y + min * strideY, strideY);
				else
					this.GeneralMatrixMultiplyVector(op, min, max - min, T.One, A + min * (op.CanInPlace() ? lda : 1), lda, x + min * strideX, strideX, T.One, y, strideY);
			}
			 */
			else
			{
				if (opA.CanInPlace() == fillUpper)
				{
					if (leftA)
						this.GeneralMatricesMultiply(opA, opB, m - colA, n, opA.CanInPlace() ? colA : rowA, α, A + colA, lda, B, ldb, T.Zero, C + colA, ldc);
					else
						this.GeneralMatricesMultiply(opB, opA, m - colA, n, opB.CanInPlace() ? colB : rowB, α, B, ldb, A, lda, T.Zero, C + colA, ldc);
				}
				else if (nn < n)
				{
					if (leftA)
						this.GeneralMatricesMultiply(opA, opB, m, n - nn, opA.CanInPlace() ? colA : rowA, α, A, lda, B, ldb, T.One, C + nn * ldc, ldc);
				}
			}
			if (conjugated)
				Conjugater.Conjugate(pC, mm, nn, ldc);
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
	}
}
