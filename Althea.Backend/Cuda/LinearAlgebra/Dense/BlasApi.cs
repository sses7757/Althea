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
			return NMC.vecArgAbsMax(T.Type, n, px, inc, out index).Check();
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
			return NMC.vecArgAbsMin(T.Type, n, px, inc, out index).Check();
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
			var hresult = doSum ? NMC.vecAbsSum(T.Type, n, px, inc, &result) : CustomStatus.NotSupported;
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
			return NMC.vecMulScalar(T.Type, n, &scalar, px, inc, px, inc).Check();
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
			this.GeneralMatricesAdd(MatrixOperation.None, MatrixOperation.None, m, n, β, A, lda, T.Zero, null, 0, A, lda);
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
			this.GeneralMatricesAdd(MatrixOperation.None, MatrixOperation.None, n, n, β, A, lda, T.Zero, null, 0, A, lda);
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
			this.GeneralMatricesAdd(MatrixOperation.None, MatrixOperation.None, n, n, β, A, lda, T.Zero, null, 0, A, lda);
		func(this.cublasHandle, fillUpper ? CuBlasFillMode.Upper : CuBlasFillMode.Lower, nn, &α, px, incx, py, incy, pA, llda).Check();
		return true;
	}
	#endregion

	#region level 3

	#endregion
}
