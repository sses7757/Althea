using System.Runtime.CompilerServices;

using Althea.LinearAlgebra;

using static Althea.Backend.Cuda.MemoryPointerChecker;

using NM = Althea.Backend.Cuda.LinearAlgebra.Dense.NativeMethods;
using NMC = Althea.Backend.Cuda.LinearAlgebra.Dense.CustomNativeMethods;


namespace Althea.Backend.Cuda.LinearAlgebra.Dense;

public unsafe partial class Api
{
	#region level 1
	/// <summary>
	/// Get the index of the element with horizontal minimum/maximum absolute value (<c>abs(x[i].real) + abs(x[i].imag)</c>) in <paramref name="x"/>
	/// </summary>
	/// <typeparam name="T">Any complex data type</typeparam>
	/// <typeparam name="TS">The actual storage type of <paramref name="x"/></typeparam>
	/// <param name="min">Compute minimum or maximum</param>
	/// <param name="x">The vector to get minimum/maximum absolute value's index</param>
	/// <param name="strideX">The spacing between consecutive elements of <paramref name="x"/></param>
	/// <param name="index">The output real index in <paramref name="x"/></param>
	/// <returns>Support or not</returns>
	protected internal bool HorizontalAbsoluteValueArgMinMax<T, TS>(bool min, TS x, long strideX, out long index) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
	{
		index = -1;
		if (!GetPointer(this, x, strideX, out T* px, out int n, out int inc))
			return false;
		delegate*<IntPtr, int, T*, int, out int, CudaBlasStatus> func = default(T) switch
		{
			Complex<Float32> when min => &NM.cublasIcamin,
			Complex<Float64> when min => &NM.cublasIzamin,
			Complex<Float32> when !min => &NM.cublasIcamin,
			Complex<Float64> when !min => &NM.cublasIzamax,
			_ => null,
		};
		if (func is null)
			return false;
		func(this.cublasHandle, n, px, inc, out int result).Check();
		index = result - 1;
		return true;
	}

	/// <inheritdoc/>
	public virtual bool AbsoluteValueArgMax<T, TS>(TS x, long strideX, out long index) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
	{
		index = -1;
		if (!GetPointer(this, x, strideX, out T* px, out int n, out int inc))
			return false;
		delegate*<IntPtr, int, T*, int, out int, CudaBlasStatus> func;
		func = default(T) switch
		{
			Float32 => &NM.cublasIsamax,
			Float64 => &NM.cublasIdamax,
			_ => null,
		};
		if (func is not null)
		{
			func(this.cublasHandle, n, px, inc, out int result).Check();
			index = result - 1;
		}
		else
		{
			return NMC.vecArgReduce(T.Type, ReduceOperation.AbsoluteMaximum, n, px, inc, out index).Check();
		}
		return true;
	}

	/// <inheritdoc/>
	public virtual bool AbsoluteValueArgMin<T, TS>(TS x, long strideX, out long index) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
	{
		index = -1;
		if (!GetPointer(this, x, strideX, out T* px, out int n, out int inc))
			return false;
		delegate*<IntPtr, int, T*, int, out int, CudaBlasStatus> func;
		func = default(T) switch
		{
			Float32 => &NM.cublasIsamin,
			Float64 => &NM.cublasIdamin,
			_ => null,
		};
		if (func is not null)
		{
			func(this.cublasHandle, n, px, inc, out int result).Check();
			index = result - 1;
		}
		else
		{
			return NMC.vecArgReduce(T.Type, ReduceOperation.AbsoluteMininum, n, px, inc, out index).Check();
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool AbsSumOrNorm<T, TS, Sum>(TS x, long strideX, out T sum) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
	{
		bool doSum = typeof(Sum) == typeof(bool);
		sum = default;
		if (!GetPointer(this, x, strideX, out T* px, out int n, out int inc))
			return false;
		delegate*<IntPtr, int, T*, int, float*, CudaBlasStatus> funcS;
		delegate*<IntPtr, int, T*, int, double*, CudaBlasStatus> funcD;
		funcS = default(T) switch
		{
			Float32 when doSum => &NM.cublasSasum,
			Float32 when !doSum => &NM.cublasSnrm2,
			Complex<Float32> when !doSum => &NM.cublasScnrm2,
			_ => null,
		};
		funcD = default(T) switch
		{
			Float64 when doSum => &NM.cublasDasum,
			Float64 when !doSum => &NM.cublasDnrm2,
			Complex<Float64> when !doSum => &NM.cublasDznrm2,
			_ => null,
		};
		if (funcS is null && funcD is null)
		{
			T result = default;
			var hresult = doSum ? NMC.vecUnaryReduce(T.Type, ReduceOperation.AddAbsolute, n, px, inc, &result) : CustomStatus.NotSupported;
			sum = result;
			return hresult.Check();
		}
		else
		{
			float resultS; double resultD;
			if (funcS is not null)
			{
				funcS(this.cublasHandle, n, px, inc, &resultS).Check();
				sum = ((double)resultS).As<T>();
			}
			if (funcD is not null)
			{
				funcD(this.cublasHandle, n, px, inc, &resultD).Check();
				sum = resultD.As<T>();
			}
		}
		return true;
	}

	/// <inheritdoc/>
	public virtual bool AbsoluteValueSum<T, TS>(TS x, long strideX, out T sum) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS> => AbsSumOrNorm<T, TS, bool>(x, strideX, out sum);

	/// <inheritdoc/>
	public virtual bool Norm<T, TS>(TS x, long strideX, out T norm) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS> => AbsSumOrNorm<T, TS, byte>(x, strideX, out norm);

	/// <inheritdoc/>
	public virtual bool Dot<T, TS1, TS2>(bool conjX, TS1 x, long strideX, TS2 y, long strideY, out T dot) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
	{
		dot = default;
		if (!GetPointer(this, x, strideX, out T* px, out int n1, out int incx))
			return false;
		if (!GetPointer(this, y, strideY, out T* py, out int n2, out int incy))
			return false;
		int n = Math.Min(n1, n2);
		delegate*<IntPtr, int, T*, int, T*, int, T*, CudaBlasStatus> func;
		func = default(T) switch
		{
			Float32 => &NM.cublasSdot,
			Float64 => &NM.cublasDdot,
			Complex<Float32> => conjX ? &NM.cublasCdotc : &NM.cublasCdotu,
			Complex<Float64> => conjX ? &NM.cublasZdotc : &NM.cublasZdotu,
			_ => null,
		};
		T result;
		if (func is not null)
		{
			func(this.cublasHandle, n, px, incx, py, incy, &result).Check();
		}
		else if (T.Type == DataType.RealFloat16 || T.Type == DataType.ComplexFloat16)
		{
			CudaDataType type = T.Type.ToCudaDataType();
			if (conjX)
				NM.cublasDotcEx(this.cublasHandle, n, px, type, incx, py, type, incy, &result, type, type).Check();
			else
				NM.cublasDotEx(this.cublasHandle, n, px, type, incx, py, type, incy, &result, type, type).Check();
		}
		else
			return false;
		dot = result;
		return true;
	}

	/// <inheritdoc/>
	public virtual bool Scale<T, TS>(TS x, long strideX, T scalar) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
	{
		if (!GetPointer(this, x, strideX, out T* px, out int n, out int inc))
			return false;
		delegate*<IntPtr, int, T*, T*, int, CudaBlasStatus> func;
		func = default(T) switch
		{
			Float32 => &NM.cublasSscal,
			Float64 => &NM.cublasDscal,
			Complex<Float32> => &NM.cublasCscal,
			Complex<Float64> => &NM.cublasZscal,
			_ => null,
		};
		if (func is not null)
		{
			func(this.cublasHandle, n, &scalar, px, inc).Check();
		}
		else if (T.Type == DataType.RealFloat16 || T.Type == DataType.ComplexFloat16)
		{
			CudaDataType type = T.Type == DataType.RealFloat16 ? CudaDataType.RealFloat16 : CudaDataType.ComplexFloat16;
			NM.cublasScalEx(this.cublasHandle, n, &scalar, type, px, type, inc, type).Check();
		}
		else
		{
			return NMC.vecBinaryScalar(T.Type, BinaryScalarOperation.Multiply, &scalar, n, px, inc, px, inc).Check();
		}
		return true;
	}

	/// <inheritdoc/>
	public virtual bool Add<T, TS1, TS2>(T α, TS1 x, long strideX, TS2 y, long strideY) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
	{
		if (!GetPointer(this, x, strideX, out T* px, out int n1, out int incx))
			return false;
		if (!GetPointer(this, y, strideY, out T* py, out int n2, out int incy))
			return false;
		int n = Math.Min(n1, n2);
		delegate*<IntPtr, int, T*, T*, int, T*, int, CudaBlasStatus> func;
		func = default(T) switch
		{
			Float32 => &NM.cublasSaxpy,
			Float64 => &NM.cublasDaxpy,
			Complex<Float32> => &NM.cublasCaxpy,
			Complex<Float64> => &NM.cublasZaxpy,
			_ => null,
		};
		if (func is not null)
		{
			func(this.cublasHandle, n, &α, px, incx, py, incy).Check();
		}
		else if (T.Type == DataType.RealFloat16 || T.Type == DataType.ComplexFloat16)
		{
			CudaDataType type = T.Type == DataType.RealFloat16 ? CudaDataType.RealFloat16 : CudaDataType.ComplexFloat16;
			NM.cublasAxpyEx(this.cublasHandle, n, &α, type, px, type, incx, py, type, incy, type).Check();
		}
		else
			return false;
		return true;
	}
	#endregion

	#region level 2
	/// <inheritdoc/>
	public virtual bool GeneralMatrixMultiplyVector<T, TSM, TSV1, TSV2>(MatrixOperation op, long m, long n, T α, TSM A, long lda, TSV1 x, long strideX, T β, TSV2 y, long strideY) where T : unmanaged, IBaseNumber<T> where TSM : class, IStorage<T, TSM> where TSV1 : class, IStorage<T, TSV1> where TSV2 : class, IStorage<T, TSV2>
	{
		if (!GetPointer(this, x, strideX, out T* px, out int nx, out int incx))
			return false;
		if (!GetPointer(this, y, strideY, out T* py, out int ny, out int incy))
			return false;
		if (!GetPointer(this, A, m, n, lda, out T* pA, out int mm, out int nn, out int llda))
			return false;
		if ((op.CanInPlace() ? nn : mm) != nx || (op.CanInPlace() ? mm : nn) != ny)
			throw new ArgumentException(Resources.ParameterError.WrongSize);

		delegate*<IntPtr, CuBlasOperation, int, int, T*, T*, int, T*, int, T*, T*, int, CudaBlasStatus> func;
		func = default(T) switch
		{
			Float32 => &NM.cublasSgemv,
			Float64 => &NM.cublasDgemv,
			Complex<Float32> => &NM.cublasCgemv,
			Complex<Float64> => &NM.cublasZgemv,
			_ => null,
		};
		if (func is null)
			return false;
		using var conjX = Conjugater.Create(px, nx, incx, op);
		func(this.cublasHandle, op.ToCuda(), mm, nn, &α, pA, llda, px, incx, &β, py, incy).Check();
		if (op == MatrixOperation.Conjugate)
			Conjugater.Conjugate(py, ny, incy);
		return true;
	}

	/// <inheritdoc/>
	public virtual bool SymmetricMatrixMultiplyVector<T, TSM, TSV1, TSV2>(bool fillUpper, bool hermA, long n, T α, TSM A, long lda, TSV1 x, long strideX, T β, TSV2 y, long strideY) where T : unmanaged, IBaseNumber<T> where TSM : class, IStorage<T, TSM> where TSV1 : class, IStorage<T, TSV1> where TSV2 : class, IStorage<T, TSV2>
	{
		if (!GetPointer(this, x, strideX, out T* px, out int nx, out int incx))
			return false;
		if (!GetPointer(this, y, strideY, out T* py, out int ny, out int incy))
			return false;
		if (!GetPointer(this, A, n, n, lda, out T* pA, out _, out int nn, out int llda))
			return false;
		if (nx != nn || ny != nn)
			throw new ArgumentException(Resources.ParameterError.WrongSize);

		delegate*<IntPtr, CuBlasFillMode, int, T*, T*, int, T*, int, T*, T*, int, CudaBlasStatus> func;
		func = default(T) switch
		{
			Float32 => &NM.cublasSsymv,
			Float64 => &NM.cublasDsymv,
			Complex<Float32> => hermA ? &NM.cublasChemv : &NM.cublasCsymv,
			Complex<Float64> => hermA ? &NM.cublasZhemv : &NM.cublasZsymv,
			_ => null,
		};
		if (func is null)
			return false;
		func(this.cublasHandle, fillUpper ? CuBlasFillMode.Upper : CuBlasFillMode.Lower, nn, &α, pA, llda, px, incx, &β, py, incy).Check();
		return true;
	}

	/// <inheritdoc/>
	public virtual bool TriangularMatrixMultiplyVector<T, TSM, TSV1, TSV2>(bool fillUpper, bool unitDiag, MatrixOperation op, long m, long n, TSM A, long lda, T α, TSV1 x, long strideX, T β, TSV2 y, long strideY) where T : unmanaged, IBaseNumber<T> where TSM : class, IStorage<T, TSM> where TSV1 : class, IStorage<T, TSV1> where TSV2 : class, IStorage<T, TSV2>
	{
		if (!GetPointer(this, x, strideX, out T* px, out int nx, out int incx))
			return false;
		if (!GetPointer(this, y, strideY, out T* py, out int ny, out int incy))
			return false;
		if (!GetPointer(this, A, m, n, lda, out T* pA, out int mm, out int nn, out int llda))
			return false;
		if (nx != nn || ny != nn)
			throw new ArgumentException(Resources.ParameterError.WrongSize);

		delegate*<IntPtr, CuBlasFillMode, CuBlasOperation, CuBlasDiagType, int, T*, int, T*, int, CudaBlasStatus> func;
		func = default(T) switch
		{
			Float32 => &NM.cublasStrmv,
			Float64 => &NM.cublasDtrmv,
			Complex<Float32> => &NM.cublasCtrmv,
			Complex<Float64> => &NM.cublasZtrmv,
			_ => null,
		};
		if (func is null)
			return false;
		op = op.Simplify<T>();
		var fu = fillUpper ? CuBlasFillMode.Upper : CuBlasFillMode.Lower;
		var ud = unitDiag ? CuBlasDiagType.Unit : CuBlasDiagType.NonUnit;
		if (px == py)
		{   // x = alpha * op(A) * x
			using var conj = Conjugater.Create(px, nx, incx, op);
			func(this.cublasHandle, fu, op.ToCuda(), ud, nx, pA, llda, px, incx);
			if (α != T.One)
				this.Scale(x, strideX, α);
		}
		else
		{
			int min = Math.Min(mm, nn), max = Math.Max(mm, nn);
			this.StridedCopy(px, py, min, incx, incy);
			bool actualSquare = op.CanInPlace() ? ((m > n) == fillUpper) : ((n > m) == !fillUpper);
			using var conj = Conjugater.Create(py, actualSquare ? min : mm, incy, op);
			func(this.cublasHandle, fu, op.ToCuda(), ud, min, pA, llda, py, incy);
			if (actualSquare)
			{
				if (op.CanInPlace() == fillUpper)
				{
					if (incy == 1)
						Storage.NativeMethods.cudaMemset(py + min * strideY, 0, n * sizeof(T)).Check();
					else
					{
						T zero = T.Zero;
						NMC.vecFillVal(T.Type, n, &zero, py + min * incy, incy).Check();
					}
				}
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
		if (!GetPointer(this, x, strideX, out T* px, out int nx, out int incx))
			return false;
		if (!GetPointer(this, y, strideY, out T* py, out int ny, out int incy))
			return false;
		if (!GetPointer(this, A, m, n, lda, out T* pA, out int mm, out int nn, out int llda))
			return false;
		if (nx != mm || ny != nn)
			throw new ArgumentException(Resources.ParameterError.WrongSize);

		delegate*<IntPtr, int, int, T*, T*, int, T*, int, T*, int, CudaBlasStatus> func;
		func = default(T) switch
		{
			Float32 => &NM.cublasSger,
			Float64 => &NM.cublasDger,
			Complex<Float32> => conjY ? &NM.cublasCgerc : &NM.cublasCgerc,
			Complex<Float64> => conjY ? &NM.cublasZgerc : &NM.cublasZgerc,
			_ => null,
		};
		if (func is null)
			return false;
		if (β != T.One) // scale A
			this.GeneralMatricesAdd(MatrixOperation.None, MatrixOperation.None, m, n, β, A, lda, default, (TSM?)null, default, A, lda);
		func(this.cublasHandle, mm, nn, &α, px, incx, py, incy, pA, llda).Check();
		return true;
	}

	/// <inheritdoc/>
	public virtual bool SymmetricRankOneUpdate<T, TSM, TSV>(bool fillUpper, bool conjX, long n, T α, TSV x, long strideX, T β, TSM A, long lda) where T : unmanaged, IBaseNumber<T> where TSM : class, IStorage<T, TSM> where TSV : class, IStorage<T, TSV>
	{
		if (!GetPointer(this, x, strideX, out T* px, out int nx, out int incx))
			return false;
		if (!GetPointer(this, A, n, n, lda, out T* pA, out _, out int nn, out int llda))
			return false;
		if (nx != nn)
			throw new ArgumentException(Resources.ParameterError.WrongSize);

		delegate*<IntPtr, CuBlasFillMode, int, T*, T*, int, T*, int, CudaBlasStatus> func;
		func = default(T) switch
		{
			Float32 => &NM.cublasSsyr,
			Float64 => &NM.cublasDsyr,
			Complex<Float32> => conjX ? &NM.cublasCher : &NM.cublasCsyr,
			Complex<Float64> => conjX ? &NM.cublasZher : &NM.cublasZsyr,
			_ => null,
		};
		if (func is null)
			return false;
		if (β != T.One) // scale A
			this.GeneralMatricesAdd(MatrixOperation.None, MatrixOperation.None, n, n, β, A, lda, default, (TSM?)null, default, A, lda);
		func(this.cublasHandle, fillUpper ? CuBlasFillMode.Upper : CuBlasFillMode.Lower, nn, &α, px, incx, pA, llda).Check();
		return true;
	}

	/// <inheritdoc/>
	public virtual bool SymmetricRankTwoUpdate<T, TSM, TSV1, TSV2>(bool fillUpper, bool conjugate, long n, T α, TSV1 x, long strideX, TSV2 y, long strideY, T β, TSM A, long lda) where T : unmanaged, IBaseNumber<T> where TSM : class, IStorage<T, TSM> where TSV1 : class, IStorage<T, TSV1> where TSV2 : class, IStorage<T, TSV2>
	{
		if (!GetPointer(this, x, strideX, out T* px, out int nx, out int incx))
			return false;
		if (!GetPointer(this, y, strideY, out T* py, out int ny, out int incy))
			return false;
		if (!GetPointer(this, A, n, n, lda, out T* pA, out _, out int nn, out int llda))
			return false;
		if (nx != nn)
			throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(x));
		if (ny != nn)
			throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(y));

		delegate*<IntPtr, CuBlasFillMode, int, T*, T*, int, T*, int, T*, int, CudaBlasStatus> func;
		func = default(T) switch
		{
			Float32 => &NM.cublasSsyr2,
			Float64 => &NM.cublasSsyr2,
			Complex<Float32> => conjugate ? &NM.cublasCher2 : &NM.cublasCsyr2,
			Complex<Float64> => conjugate ? &NM.cublasZher2 : &NM.cublasZsyr2,
			_ => null,
		};
		if (β != T.One) // scale A
			this.GeneralMatricesAdd(MatrixOperation.None, MatrixOperation.None, n, n, β, A, lda, default, (TSM?)null, default, A, lda);
		func(this.cublasHandle, fillUpper ? CuBlasFillMode.Upper : CuBlasFillMode.Lower, nn, &α, px, incx, py, incy, pA, llda).Check();
		return true;
	}
	#endregion

	#region level 3
	/// <inheritdoc/>
	public virtual bool GeneralMatricesMultiply<T, TS1, TS2, TS3>(MatrixOperation opA, MatrixOperation opB, long m, long n, long k, T α, TS1 A, long lda, TS2 B, long ldb, T β, TS3 C, long ldc) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>
	{
		if (!GetPointer(this, A, opA, m, k, lda, out T* pA, out _, out int kk, out int llda))
			return false;
		if (!GetPointer(this, B, opB, k, n, ldb, out T* pB, out _, out _, out int lldb))
			return false;
		if (!GetPointer(this, C, m, n, ldc, out T* pC, out int mm, out int nn, out int lldc))
			return false;

		delegate*<IntPtr, CuBlasOperation, CuBlasOperation, int, int, int, T*, T*, int, T*, int, T*, T*, int, CudaBlasStatus> func;
		func = default(T) switch
		{
			Float32 => &NM.cublasSgemm,
			Float64 => &NM.cublasDgemm,
			Complex<Float32> => this.ComplexGemmUseGemm3m ? &NM.cublasCgemm3m : &NM.cublasCgemm,
			Complex<Float64> => this.ComplexGemmUseGemm3m ? &NM.cublasZgemm3m : &NM.cublasZgemm,
			_ => null,
		};
		if (func is not null || T.Type == DataType.RealFloat16 || T.Type == BrainHalf.RealBrainHalfType)
		{
			// nothing
		}
		else
			return false;

		opA = opA.Simplify<T>(); opB = opB.Simplify<T>();
		if ((opA == MatrixOperation.Conjugate) != (opB == MatrixOperation.Conjugate))
			return false;
		if (opA == MatrixOperation.Conjugate && β != T.Zero)
			Conjugater.Conjugate(pC, mm, nn, lldc);
		if (func is not null)
		{
			func(this.cublasHandle, opA.ToCuda(), opB.ToCuda(), mm, nn, kk, &α, pA, llda, pB, lldb, &β, pC, lldc).Check();
		}
		else
		{
			var type = T.Type.ToCudaDataType();
			CuBlasComputeType cType = type switch
			{
				////CudaDataType.RealFloat32 or CudaDataType.ComplexFloat32 => CuBlasComputeType.Compute32F,
				////CudaDataType.RealFloat64 or CudaDataType.ComplexFloat64 => CuBlasComputeType.Compute64F,
				CudaDataType.RealFloat16 => CuBlasComputeType.Compute16F,
				CudaDataType.RealBrainFloat16 => CuBlasComputeType.Compute32F,
				_ => default,
			};
			NM.cublasGemmEx(this.cublasHandle, opA.ToCuda(), opB.ToCuda(), mm, nn, kk, &α, pA, type, llda, pB, type, lldb, &β, pC, type, lldc, cType, CuBlasGemmAlgorithm.Default).Check();
		}
		if (opA == MatrixOperation.Conjugate)
			Conjugater.Conjugate(pC, mm, nn, lldc);
		return true;
	}

	/// <inheritdoc/>
	public virtual bool SymmetricMatrixMultiplyGeneral<T, TS1, TS2, TS3>(bool fillUpper, bool leftA, bool hermA, MatrixOperation opA, MatrixOperation opB, long m, long n, T α, TS1 A, long lda, TS2 B, long ldb, T β, TS3 C, long ldc) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>
	{
		if (α == T.Zero)
			throw new ArgumentOutOfRangeException(nameof(α), Resources.ParameterError.CannotZero);
		opA = opA.Simplify<T>(hermA); opB = opB.Simplify<T>();
		if (!GetPointer(this, A, leftA ? m : n, leftA ? m : n, lda, out T* pA, out _, out _, out int llda))
			return false;
		if (!GetPointer(this, B, opB, m, n, ldb, out T* pB, out _, out _, out int lldb))
			return false;
		if (!GetPointer(this, C, m, n, ldc, out T* pC, out int mm, out int nn, out int lldc))
			return false;

		delegate*<IntPtr, CuBlasSideMode, CuBlasFillMode, int, int, T*, T*, int, T*, int, T*, T*, int, CudaBlasStatus> func;
		func = default(T) switch
		{
			Float32 => &NM.cublasSsymm,
			Float64 => &NM.cublasDsymm,
			Complex<Float32> => hermA ? &NM.cublasChemm : &NM.cublasCsymm,
			Complex<Float64> => hermA ? &NM.cublasZhemm : &NM.cublasZsymm,
			_ => null,
		};
		var side = leftA ? CuBlasSideMode.Left : CuBlasSideMode.Right;
		var uplo = fillUpper ? CuBlasFillMode.Upper : CuBlasFillMode.Lower;
		if (!opB.CanInPlace())
		{
			if (m != ldc)
				return false;
			// pre
			if (opA != MatrixOperation.None)
				return false;
			// multiply
			func(this.cublasHandle, side, uplo, mm, nn, &α, pA, llda, pB, lldb, &β, pC, lldc);
			// post
		}
		else if (opA == MatrixOperation.Conjugate)
		{   // T is complex
			// pre
			if (opB == MatrixOperation.None)
				return false;
			// multiply
			func(this.cublasHandle, side, uplo, mm, nn, &α, pA, llda, pB, lldb, &β, pC, lldc);
			// post
			Conjugater.Conjugate(pC, mm, nn, lldc);
		}
		else
		{
			if (opB != MatrixOperation.None)
				return false;
			func(this.cublasHandle, side, uplo, mm, nn, &α, pA, llda, pB, lldb, &β, pC, lldc);
		}
		return true;
	}

	/// <inheritdoc/>
	public virtual bool TriangularMatrixSolve<T, TS1, TS2>(bool leftA, bool fillUpper, bool unitDiag, MatrixOperation op, long m, long n, T α, TS1 A, long lda, TS2 B, long ldb) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
	{
		if (!GetPointer(this, A, m, m, lda, out T* pA, out int mm, out _, out int llda))
			return false;
		if (!GetPointer(this, B, m, n, ldb, out T* pB, out _, out int nn, out int lldb))
			return false;
		if (α == T.Zero) // result is 0
			return this.GeneralMatricesAdd(MatrixOperation.None, MatrixOperation.None, m, n, α, B, ldb, default, (TS1?)null, default, B, ldb);
		if (op == MatrixOperation.Conjugate)
			return false;

		delegate*<IntPtr, CuBlasSideMode, CuBlasFillMode, CuBlasOperation, CuBlasDiagType, int, int, T*, T*, int, T*, int, CudaBlasStatus> func;
		func = default(T) switch
		{
			Float32 => &NativeMethods.cublasStrsm,
			Float64 => &NativeMethods.cublasDtrsm,
			Complex<Float32> => &NativeMethods.cublasCtrsm,
			Complex<Float64> => &NativeMethods.cublasZtrsm,
			_ => null,
		};
		func(this.cublasHandle, leftA ? CuBlasSideMode.Right : CuBlasSideMode.Left, fillUpper ? CuBlasFillMode.Upper : CuBlasFillMode.Lower, op.ToCuda(), unitDiag ? CuBlasDiagType.Unit : CuBlasDiagType.NonUnit, mm, nn, &α, pA, llda, pB, lldb).Check();
		return true;
	}

	/// <inheritdoc/>
	public virtual bool TriangularMatrixMultiplyGeneral<T, TS1, TS2, TS3>(bool leftA, bool fillUpper, bool unitDiag, MatrixOperation opA, MatrixOperation opB, long m, long n, long k, T α, TS1 A, long lda, TS2 B, long ldb, T β, TS3 C, long ldc) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>
	{
		int rowA, colA, rowB, colB;
		(rowA, colA) = opA.CanInPlace() ? ((int)m, (int)k) : ((int)k, (int)m);
		(rowB, colB) = opB.CanInPlace() ? ((int)k, (int)n) : ((int)n, (int)k);
		if (!leftA)
		{
			((rowA, colA), (rowB, colB)) = ((rowB, colB), (rowA, colA));
		}
		if (!GetPointer(this, A, rowA, colA, lda, out T* pA, out _, out _, out int llda))
			return false;
		if (!GetPointer(this, B, rowB, colB, ldb, out T* pB, out _, out _, out int lldb))
			return false;
		if (!GetPointer(this, C, m, n, ldc, out T* pC, out _, out _, out int lldc))
			return false;
		if (β != T.Zero || (pB == pC && (!opB.CanInPlace() || rowB != m || colB != n || ldb != ldc || rowA != colA)))
			return false;
		opA = opA.Simplify<T>();
		delegate*<IntPtr, CuBlasSideMode, CuBlasFillMode, CuBlasOperation, CuBlasDiagType, int, int, T*, T*, int, T*, int, T*, int, CudaBlasStatus> func;
		func = default(T) switch
		{
			Float32 => &NM.cublasStrmm,
			Float64 => &NM.cublasDtrmm,
			Complex<Float32> => &NM.cublasCtrmm,
			Complex<Float64> => &NM.cublasZtrmm,
			_ => null,
		};
		if (func is null)
			return false;
		var lr = leftA ? CuBlasSideMode.Left : CuBlasSideMode.Right;
		var fu = fillUpper ? CuBlasFillMode.Upper : CuBlasFillMode.Lower;
		var ud = unitDiag ? CuBlasDiagType.Unit : CuBlasDiagType.NonUnit;
		bool actualSquare = opA.CanInPlace() ? ((m > n) == fillUpper) : ((n > m) == !fillUpper);
		bool conjugated = false;
		int mm = Math.Min(rowB, (int)m), nn = Math.Min(colB, (int)n);
		if (opA == MatrixOperation.Conjugate)
		{
			opB = opB.Conjugate().Simplify<T>();
			opA = MatrixOperation.None;
			conjugated = true;
		}
		func(this.cublasHandle, lr, fu, opA.ToCuda(), ud, mm, nn, &α, pA, llda, pB, lldb, pC, lldc).Check();
		long minA = Math.Min(rowA, colA), maxA = Math.Max(rowA, colA);
		Mkl.LinearAlgebra.Dense.Api.TriangularMatrixMultiplyGeneralPostProcess(this, actualSquare, leftA, fillUpper, opA, opB, m, n, minA, maxA, colA, rowA, colB, rowB, α, A, lda, B, ldb, C, ldc);
		if (conjugated)
			Conjugater.Conjugate(pC, mm, nn, lldc);
		return true;
	}

	/// <inheritdoc/>
	public virtual bool SymmetricRankKUpdate<T, TS1, TS2>(bool fillUpper, MatrixOperation op, bool conjA, long n, long k, T α, TS1 A, long lda, T β, TS2 C, long ldc) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
	{
		if (!GetPointer(this, A, op, n, k, lda, out T* pA, out int nn, out int kk, out int llda))
			return false;
		if (!GetPointer(this, C, n, n, ldc, out T* pC, out _, out _, out int lldc))
			return false;
		op = op.Simplify<T>();
		if (op == MatrixOperation.Conjugate && β != T.Zero)
			return false;
		
		delegate*<IntPtr, CuBlasFillMode, CuBlasOperation, int, int, T*, T*, int, T*, T*, int, CudaBlasStatus> func;
		func = default(T) switch
		{
			Float32 => &NativeMethods.cublasSsyrk,
			Float64 => &NativeMethods.cublasDsyrk,
			Complex<Float32> => conjA ? &NativeMethods.cublasCherk : &NativeMethods.cublasCsyrk,
			Complex<Float64> => conjA ? &NativeMethods.cublasZherk : &NativeMethods.cublasZsyrk,
			_ => null,
		};
		if (func is null)
			return false;
		func(this.cublasHandle, fillUpper ? CuBlasFillMode.Upper : CuBlasFillMode.Lower, op.ToCuda(), nn, kk, &α, pA, llda, &β, pC, lldc).Check();
		if (op == MatrixOperation.Conjugate)
			Conjugater.Conjugate(pC, nn, kk, lldc);
		return true;
	}

	/// <inheritdoc/>
	public virtual bool SymmetricRankTwoKUpdate<T, TS1, TS2, TS3>(bool fillUpper, MatrixOperation op, bool conjugate, long n, long k, T α, TS1 A, long lda, TS2 B, long ldb, T β, TS3 C, long ldc) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>
	{
		if (!GetPointer(this, A, op, n, k, lda, out T* pA, out int nn, out int kk, out int llda))
			return false;
		if (!GetPointer(this, B, op, n, k, ldb, out T* pB, out _, out _, out int lldb))
			return false;
		if (!GetPointer(this, C, n, n, ldc, out T* pC, out _, out _, out int lldc))
			return false;
		op = op.Simplify<T>();
		if (op == MatrixOperation.Conjugate && β != T.Zero)
			return false;

		delegate*<IntPtr, CuBlasFillMode, CuBlasOperation, int, int, T*, T*, int, T*, int, T*, T*, int, CudaBlasStatus> func;
		func = default(T) switch
		{
			Float32 => &NativeMethods.cublasSsyr2k,
			Float64 => &NativeMethods.cublasDsyr2k,
			Complex<Float32> => conjugate ? &NativeMethods.cublasCher2k : &NativeMethods.cublasCsyr2k,
			Complex<Float64> => conjugate ? &NativeMethods.cublasZher2k : &NativeMethods.cublasZsyr2k,
			_ => null,
		};
		if (func is null)
			return false;
		func(this.cublasHandle, fillUpper ? CuBlasFillMode.Upper : CuBlasFillMode.Lower, op.ToCuda(), nn, kk, &α, pA, llda, pB, lldb, &β, pC, lldc).Check();
		if (op == MatrixOperation.Conjugate)
			Conjugater.Conjugate(pC, nn, kk, lldc);
		return true;
	}
	#endregion

	#region BLAS-like
	/// <inheritdoc/>
	public virtual bool GeneralMatricesAdd<T, TS1, TS2, TS3>(MatrixOperation opA, MatrixOperation opB, long m, long n, T α, TS1? A, long lda, T β, TS2? B, long ldb, TS3 C, long ldc) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>
	{
		if (!GetPointer(this, A, opA, m, n, lda, out T* pA, out _, out _, out int llda))
			return false;
		if (!GetPointer(this, B, opB, m, n, ldb, out T* pB, out _, out _, out int lldb))
			return false;
		if (!GetPointer(this, C, m, n, ldc, out T* pC, out int mm, out int nn, out int lldc))
			return false;

		// shortcut
		if ((A is null || α == T.Zero) != (B is null || β == T.Zero))
		{
			if ((A is null || α == T.Zero) && opB == MatrixOperation.None && β == T.One)
			{   // copy B to C
				Storage.NativeMethods.cudaMemcpy2D(pC, ldc * sizeof(T), pB, ldb * sizeof(T), m * sizeof(T), n, Storage.MemoryCopyKind.DeviceToDevice).Check();
			}
			else if ((B is null || β == T.Zero) && opA == MatrixOperation.None && α == T.One)
			{   // copy A to C
				Storage.NativeMethods.cudaMemcpy2D(pC, ldc * sizeof(T), pA, lda * sizeof(T), m * sizeof(T), n, Storage.MemoryCopyKind.DeviceToDevice).Check();
			}
			return true;
		}
		// normal
		opA = opA.Simplify<T>(); opB = opB.Simplify<T>();
		if ((opA == MatrixOperation.Conjugate) != (opB == MatrixOperation.Conjugate))
			return false;
		if (llda <= 0) llda = 1;
		if (lldb <= 0) lldb = 1;
		delegate*<IntPtr, CuBlasOperation, CuBlasOperation, int, int, T*, T*, int, T*, T*, int, T*, int, CudaBlasStatus> func;
		func = default(T) switch
		{
			Float32 => &NativeMethods.cublasSgeam,
			Float64 => &NativeMethods.cublasDgeam,
			Complex<Float32> => &NativeMethods.cublasCgeam,
			Complex<Float64> => &NativeMethods.cublasZgeam,
			_ => null,
		};
		func(this.cublasHandle, opA.ToCuda(), opB.ToCuda(), mm, nn, &α, pA, llda, &β, pB, lldb, pC, lldc).Check();
		if (opA == MatrixOperation.Conjugate)
			Conjugater.Conjugate(pC, mm, nn, lldc);
		return true;
	}

	/// <inheritdoc/>
	public virtual bool DiagonalMatrixMultiplyGeneral<T, TS1, TS2, TS3>(bool leftA, MatrixOperation opA, bool conjX, long m, long n, T α, TS1 A, long lda, TS2 x, long strideX, T β, TS3 C, long ldc) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>
	{
		if (!GetPointer(this, x, strideX, out T* px, out int nx, out int incx))
			return false;
		if (!GetPointer(this, A, opA, m, n, lda, out T* pA, out int mm, out int nn, out int llda))
			return false;
		if (!GetPointer(this, C, m, n, ldc, out T* pC, out _, out _, out int lldc))
			return false;
		if (nx < (leftA == opA.CanInPlace() ? n : m))
			throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(x));
		if (β != T.Zero)
			return false;

		delegate*<IntPtr, CuBlasSideMode, int, int, T*, int, T*, int, T*, int, CudaBlasStatus> func;
		func = default(T) switch
		{
			Float32 => &NativeMethods.cublasSdgmm,
			Float64 => &NativeMethods.cublasDdgmm,
			Complex<Float32> => &NativeMethods.cublasCdgmm,
			Complex<Float64> => &NativeMethods.cublasZdgmm,
			_ => null,
		};
		// overwrite C by diagonal multiply result
		func(this.cublasHandle, leftA ? CuBlasSideMode.Right : CuBlasSideMode.Left, mm, nn, pA, llda, px, incx, pC, lldc).Check();
		// C = α * C
		if (α != T.One)
			return this.GeneralMatricesAdd(MatrixOperation.None, MatrixOperation.None, m, n, α, C, ldc, default, (TS3?)null, default, C, ldc);
		return true;
	}

	/// <summary>
	/// See <see cref="SymmetricRankKUpdate{T, TS1, TS2}(bool, MatrixOperation, bool, long, long, T, TS1, long, T, TS2, long)"/> except we are computing <c><paramref name="C"/> = <paramref name="α"/> * <paramref name="op"/>(<paramref name="A"/>) * <paramref name="op"/>(<paramref name="B"/>)^pow + <paramref name="β"/> * <paramref name="C"/></c> can be used when <paramref name="B"/> is in such way that the result <paramref name="C"/> is guaranteed to be symmetric/hermitian.
	/// </summary>
	public virtual bool SymmetricRankKUpdateVariant<T, TS1, TS2, TS3>(bool fillUpper, MatrixOperation op, bool conjB, long n, long k, T α, TS1 A, long lda, TS2 B, long ldb, T β, TS3 C, long ldc) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>
	{
		if (!GetPointer(this, A, op, n, k, lda, out T* pA, out int nn, out int kk, out int llda))
			return false;
		if (!GetPointer(this, B, op, n, k, lda, out T* pB, out _, out _, out int lldb))
			return false;
		if (!GetPointer(this, C, n, n, ldc, out T* pC, out _, out _, out int lldc))
			return false;
		op = op.Simplify<T>();
		if (op == MatrixOperation.Conjugate && β != T.Zero)
			return false;

		delegate*<IntPtr, CuBlasFillMode, CuBlasOperation, int, int, T*, T*, int, T*, int, T*, T*, int, CudaBlasStatus> func;
		func = default(T) switch
		{
			Float32 => &NativeMethods.cublasSsyrkx,
			Float64 => &NativeMethods.cublasDsyrkx,
			Complex<Float32> => conjB ? &NativeMethods.cublasCherkx : &NativeMethods.cublasCsyrkx,
			Complex<Float64> => conjB ? &NativeMethods.cublasZherkx : &NativeMethods.cublasZsyrkx,
			_ => null,
		};
		func(this.cublasHandle, fillUpper ? CuBlasFillMode.Upper : CuBlasFillMode.Lower, op.ToCuda(), nn, kk, &α, pA, llda, pB, lldb, &β, pC, lldc).Check();
		if (op == MatrixOperation.Conjugate)
			Conjugater.Conjugate(pC, nn, kk, lldc);
		return true;
	}
	#endregion
}
