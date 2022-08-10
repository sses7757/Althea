using Althea.LinearAlgebra;

using static Althea.Backend.Cuda.MemoryPointerChecker;

using NMC = Althea.Backend.Cuda.LinearAlgebra.Dense.CustomNativeMethods;


namespace Althea.Backend.Cuda.LinearAlgebra.Dense;

public unsafe partial class Api
{
	#region vector
	/// <inheritdoc/>
	public virtual bool GeneralVectorUnary<T, TS1, TS2>(UnaryOperation op, TS1 x, long strideX, TS2 y, long strideY) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
	{
		if (op == UnaryOperation.Identity || (op == UnaryOperation.Conjugate && !T.IsComplexType))
			return true;
		if (op == UnaryOperation.Negate)
		{
			x.CopyTo<T, TS1, TS2>(y);
			return this.Scale(y, strideY, -T.One);
		}
		if (!GetPointer(this, x, strideX, out T* px, out long n))
			return false;
		if (!GetPointer(this, y, strideY, out T* py, out long ny))
			return false;
		n = Math.Min(n, ny);
		delegate*<DataType, long, T*, long, T*, long, CustomStatus> func;
		func = op switch
		{
			UnaryOperation.Conjugate => &NMC.vecConj,
			UnaryOperation.AbsoluteValue => &NMC.vecAbs,
			_ => null,
		};
		if (func is null)
			return false;
		if (op == UnaryOperation.AbsoluteValue && T.IsComplexType)
			strideY *= 2;
		return func(T.Type, n, px, strideX, py, strideY).Check();
	}

	/// <inheritdoc/>
	public virtual bool GeneralVectorBinaryScalar<T, TS1, TS2>(BinaryScalarOperation op, T scalar, TS1 x, long strideX, TS2 y, long strideY) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
	{
		if (!GetPointer(this, x, strideX, out T* px, out long n))
			return false;
		if (!GetPointer(this, y, strideY, out T* py, out long ny))
			return false;
		n = Math.Min(n, ny);
		if (op == BinaryScalarOperation.Fill)
			return NMC.vecFillVal(T.Type, n, &scalar, py, strideY).Check();
		delegate*<DataType, long, T*, T*, long, T*, long, CustomStatus> func = op switch
		{
			BinaryScalarOperation.Add => &NMC.vecAddScalar,
			BinaryScalarOperation.Multiply => &NMC.vecMulScalar,
			BinaryScalarOperation.Truncate => &NMC.vecClip,
			BinaryScalarOperation.Power => &NMC.vecPowScalar,
			_ => null,
		};
		return func != null && func(T.Type, n, &scalar, px, strideX, py, strideY).Check();
	}

	/// <inheritdoc/>
	public virtual bool GeneralVectorReduce<T, TS>(ReduceOperation op, TS x, long strideX, out T result) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
	{
		if (op == ReduceOperation.Norm)
			return this.Norm(x, strideX, out result);
		result = default;
		if (!GetPointer(this, x, strideX, out T* px, out long n))
			return false;
		T reduce = default;
		delegate*<DataType, long, T*, long, T*, CustomStatus> func = op switch
		{
			ReduceOperation.Add => &NMC.vecSum,
			ReduceOperation.AddAbsolute => &NMC.vecAbsSum,
			ReduceOperation.Multiply => &NMC.vecProd,
			ReduceOperation.MultiplyAbsolute => &NMC.vecAbsProd,
			_ => null,
		};
		delegate*<DataType, long, T*, long, out long, CustomStatus> funcInd = op switch
		{
			ReduceOperation.Maximum => &NMC.vecArgMax,
			ReduceOperation.Mininum => &NMC.vecArgMin,
			ReduceOperation.AbsoluteMaximum => &NMC.vecArgAbsMax,
			ReduceOperation.AbsoluteMininum => &NMC.vecArgAbsMin,
			_ => null,
		};
		CustomStatus status = CustomStatus.NotSupported;
		if (func is not null)
		{
			status = func(T.Type, n, px, strideX, &reduce);
			result = reduce;
		}
		if (funcInd is not null)
		{
			status = funcInd(T.Type, n, px, strideX, out long index);
			result = px[index];
			if (op == ReduceOperation.AbsoluteMaximum || op == ReduceOperation.AbsoluteMininum)
				result = T.Abs(result);
		}
		return status.Check();
	}

	/// <inheritdoc/>
	public virtual bool GeneralVectorArgReduce<T, TS>(ReduceOperation op, TS x, long strideX, out long index) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
	{
		if ((op == ReduceOperation.AbsoluteMaximum || op == ReduceOperation.AbsoluteMininum) && (typeof(T) == typeof(Float32) || typeof(T) == typeof(Float64)))
		{
			return op == ReduceOperation.AbsoluteMaximum ? this.AbsoluteValueArgMax<T, TS>(x, strideX, out index) : this.AbsoluteValueArgMin<T, TS>(x, strideX, out index);
		}
		index = -1;
		if (!GetPointer(this, x, strideX, out T* px, out long n))
			return false;
		if (op >= ReduceOperation.Add && op <= ReduceOperation.Norm)
			throw new ArgumentOutOfRangeException(nameof(op), op, Resources.ParameterError.InvalidValue);
		delegate*<DataType, long, T*, long, out long, CustomStatus> funcInd = op switch
		{
			ReduceOperation.Maximum => &NMC.vecArgMax,
			ReduceOperation.Mininum => &NMC.vecArgMin,
			ReduceOperation.AbsoluteMaximum => &NMC.vecArgAbsMax,
			ReduceOperation.AbsoluteMininum => &NMC.vecArgAbsMin,
			_ => null,
		};
		return funcInd != null && funcInd(T.Type, n, px, strideX, out index).Check();
	}

	/// <inheritdoc/>
	public virtual bool GeneralVectorsBinary<T, TS1, TS2, TS3>(BinaryOperation op, TS1 x, long strideX, TS2 y, long strideY, TS3 z, long strideZ) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>
	{
		return false;
	}

	/// <inheritdoc/>
	public virtual bool GeneralVectorsScan<T, TS1, TS2>(BinaryOperation op, TS1 x, long strideX, TS2 y, long strideY, bool inclusive) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
	{
		if (!GetPointer(this, x, strideX, out T* px, out long n))
			return false;
		if (!GetPointer(this, y, strideY, out T* py, out long ny))
			return false;
		if (ny < n)
			throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(y));
		delegate*<DataType, bool, long, T*, long, T*, long, CustomStatus> func = op == BinaryOperation.Add ? &NMC.vecParSum : op == BinaryOperation.Multiply ? &NMC.vecParProd : null;
		return func != null && func(T.Type, inclusive, n, px, strideX, py, strideY).Check();
	}

	/// <inheritdoc/>
	public virtual bool GeneralVectorsCast<TIn, TOut, TSIn, TSOut>(TSIn source, long strideSource, TSOut destination, long strideDestination) where TIn : unmanaged, IBaseNumber<TIn> where TOut : unmanaged, IBaseNumber<TOut> where TSIn : class, IStorage<TIn, TSIn> where TSOut : class, IStorage<TOut, TSOut>
	{
		if (!GetPointer(this, source, strideSource, out TIn* px, out long n))
			return false;
		if (!GetPointer(this, destination, strideDestination, out TOut* py, out long ny))
			return false;
		n = Math.Min(n, ny);
		return NMC.vecDataConvert(TIn.Type, TOut.Type, true, n, px, strideSource, py, strideDestination).Check();
	}

	/// <inheritdoc/>
	public virtual bool GeneralVectorsEqual<T, TS1, TS2>(TS1 x, long strideX, TS2 y, long strideY, out bool equals) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
	{
		equals = false;
		if (!GetPointer(this, x, strideX, out T* px, out long n))
			return false;
		if (!GetPointer(this, y, strideY, out T* py, out long ny))
			return false;
		if (ny != n)
			return true;
		return NMC.vecsEq(T.Type, n, px, strideX, py, strideY, out equals).Check();
	}
	#endregion

	#region matrix
	/// <inheritdoc/>
	public virtual bool MatrixKronecker<T, TS1, TS2, TS3>(long ma, long na, long mb, long nb, T α, TS1 A, long lda, TS2 B, long ldb, T β, TS3 C, long ldc) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>
	{
		if (!GetPointer(this, A, ma, na, lda, out T* pA))
			return false;
		if (!GetPointer(this, B, mb, nb, ldb, out T* pB))
			return false;
		if (!GetPointer(this, C, ma * mb, na * nb, ldc, out T* pC))
			return false;
		return NMC.matKron(T.Type, &α, pA, lda, ma, na, pB, ldb, mb, nb, &β, pC, ldc).Check();
	}

	/// <inheritdoc/>
	public virtual bool GeneralMatrixUnary<T, TS1, TS2>(UnaryOperation op, long rows, long cols, TS1 A, long lda, TS2 B, long ldb) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
	{
		if (rows == lda && rows == ldb)
			return GeneralVectorUnary<T, TS1, TS2>(op, A.MakeReference(0, rows * cols), 1, B, 1);
		if (!GetPointer(this, A, rows, cols, lda, out T* pA))
			return false;
		if (!GetPointer(this, B, rows, cols, ldb, out T* pB))
			return false;
		switch (op)
		{
			case UnaryOperation.Identity:
				return true;
			case UnaryOperation.Conjugate:
				return NMC.matConj(T.Type, rows, cols, pA, lda, pB, ldb).Check();
			case UnaryOperation.Negate:
				T m1 = -T.One;
				return NMC.matMulScalar(T.Type, rows, cols, &m1, pA, lda, pB, ldb).Check();
			default:
				return false;
		}
	}

	/// <inheritdoc/>
	public virtual bool GeneralMatrixBinaryScalar<T, TS1, TS2>(BinaryScalarOperation op, long rows, long cols, T scalar, TS1 A, long lda, TS2 B, long ldb) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
	{
		if (rows == lda && rows == ldb)
			return GeneralVectorBinaryScalar(op, scalar, A.MakeReference(0, rows * cols), 1, B, 1);
		if (!GetPointer(this, A, rows, cols, lda, out T* pA))
			return false;
		if (!GetPointer(this, B, rows, cols, ldb, out T* pB))
			return false;
		if (op == BinaryScalarOperation.Fill)
			return NMC.matFillVal(T.Type, rows, cols, &scalar, pB, ldb).Check();
		delegate*<DataType, long, long, T*, T*, long, T*, long, CustomStatus> func = op switch
		{
			BinaryScalarOperation.Add => &NMC.matAddScalar,
			BinaryScalarOperation.Multiply => &NMC.matMulScalar,
			BinaryScalarOperation.Truncate => &NMC.matClip,
			_ => null,
		};
		return func != null && func(T.Type, rows, cols, &scalar, pA, lda, pB, ldb).Check();
	}

	/// <inheritdoc/>
	public virtual bool GeneralMatricesBinary<T, TS1, TS2, TS3>(BinaryOperation op, long rows, long cols, TS1 A, long lda, TS2 B, long ldb, TS3 C, long ldc) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>
	{
		if (rows == lda && rows == ldb && rows == ldc)
			return GeneralVectorsBinary<T, TS1, TS2, TS3>(op, A.MakeReference(0, rows * cols), 1, B, 1, C, 1);
		else
			return false;
	}

	/// <inheritdoc/>
	public virtual bool GeneralMatrixReduce<T, TS>(ReduceOperation op, long rows, long cols, TS A, long lda, out T result) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
	{
		if (rows == lda)
			return GeneralVectorReduce<T, TS>(op, A.MakeReference(0, rows * cols), 1, out result);
		result = default;
		if (!GetPointer(this, A, rows, cols, lda, out T* pA))
			return false;
		T reduce = T.Zero;
		delegate*<DataType, long, long, T*, long, T*, CustomStatus> func = op switch
		{
			ReduceOperation.Add => &NMC.matSum,
			ReduceOperation.AddAbsolute => &NMC.matAbsSum,
			ReduceOperation.Multiply => &NMC.matProd,
			ReduceOperation.MultiplyAbsolute => &NMC.matAbsProd,
			ReduceOperation.Norm => &NMC.matAsVecNorm,
			_ => null,
		};
		delegate*<DataType, long, long, T*, long, out long, CustomStatus> funcInd = op switch
		{
			ReduceOperation.Maximum => &NMC.matArgMax,
			ReduceOperation.Mininum => &NMC.matArgMin,
			ReduceOperation.AbsoluteMaximum => &NMC.matArgAbsMax,
			ReduceOperation.AbsoluteMininum => &NMC.matArgAbsMin,
			_ => null,
		};
		CustomStatus status = CustomStatus.NotSupported;
		if (func is not null)
		{
			status = func(T.Type, rows, cols, pA, lda, &reduce);
			result = reduce;
		}
		if (funcInd is not null)
		{
			status = funcInd(T.Type, rows, cols, pA, lda, out long index);
			result = pA[index];
			if (op == ReduceOperation.AbsoluteMaximum || op == ReduceOperation.AbsoluteMininum)
				result = T.Abs(result);
		}
		return status.Check();
	}

	/// <inheritdoc/>
	public virtual bool GeneralMatrixArgReduce<T, TS>(ReduceOperation op, long rows, long cols, TS A, long lda, out long index) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
	{
		if (rows == lda)
			return GeneralVectorArgReduce<T, TS>(op, A.MakeReference(0, rows * cols), 1, out index);
		index = -1;
		if (!GetPointer(this, A, rows, cols, lda, out T* pA))
			return false;
		if (op >= ReduceOperation.Add && op <= ReduceOperation.Norm)
			throw new ArgumentOutOfRangeException(nameof(op), op, Resources.ParameterError.InvalidValue);
		delegate*<DataType, long, long, T*, long, out long, CustomStatus> funcInd = op switch
		{
			ReduceOperation.Maximum => &NMC.matArgMax,
			ReduceOperation.Mininum => &NMC.matArgMin,
			ReduceOperation.AbsoluteMaximum => &NMC.matArgAbsMax,
			ReduceOperation.AbsoluteMininum => &NMC.matArgAbsMin,
			_ => null,
		};
		return funcInd != null && funcInd(T.Type, rows, cols, pA, lda, out index).Check();
	}

	/// <inheritdoc/>
	public virtual bool GeneralMatrixColumnReduce<T, TS1, TS2>(ReduceOperation op, long rows, long cols, TS1 A, long lda, TS2 x, long strideX) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
	{
		if (!GetPointer(this, A, rows, cols, lda, out T* pA))
			return false;
		if (!GetPointer(this, x, strideX, out T* px, out long n))
			return false;
		if (n < cols)
			throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(x));
		delegate*<DataType, long, long, T*, long, T*, long, CustomStatus> func = op switch
		{
			ReduceOperation.Add => &NMC.matColsSum,
			ReduceOperation.AddAbsolute => &NMC.matColsAbsSum,
			ReduceOperation.Multiply => &NMC.matColsProd,
			ReduceOperation.MultiplyAbsolute => &NMC.matColsAbsProd,
			ReduceOperation.Norm => &NMC.matColsNorm,
			_ => null
		};
		return func != null && func(T.Type, rows, cols, pA, lda, px, strideX).Check();
	}

	/// <inheritdoc/>
	public virtual bool GeneralMatrixColumnScan<T, TS1, TS2>(BinaryOperation op, bool inclusive, long rows, long cols, TS1 A, long lda, TS2 B, long ldb) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
	{
		if (!GetPointer(this, A, rows, cols, lda, out T* pA))
			return false;
		if (!GetPointer(this, B, rows, cols, ldb, out T* pB))
			return false;
		delegate*<DataType, bool, long, long, T*, long, T*, long, CustomStatus> func = op switch
		{
			BinaryOperation.Add => &NMC.matColsParSum,
			BinaryOperation.Multiply => &NMC.matColsParProd,
			_ => null
		};
		return func != null && func(T.Type, inclusive, rows, cols, pA, lda, pB, ldb).Check();
	}

	/// <inheritdoc/>
	public virtual bool GeneralMatricesEqual<T, TS1, TS2>(long rows, long cols, TS1 A, long lda, TS2 B, long ldb, out bool equals) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
	{
		if (rows == lda && rows == ldb)
			return GeneralVectorsEqual<T, TS1, TS2>(A.MakeReference(0, rows * cols), 1, B, 1, out equals);
		equals = false;
		if (!GetPointer(this, A, rows, cols, lda, out T* pA))
			return false;
		if (!GetPointer(this, B, rows, cols, ldb, out T* pB))
			return false;
		return NMC.matsEq(T.Type, rows, cols, pA, lda, pB, ldb, out equals).Check();
	}

	/// <inheritdoc/>
	public virtual bool GeneralMatrixCast<TIn, TOut, TSIn, TSOut>(long rows, long cols, TSIn source, long lds, TSOut destination, long ldd) where TIn : unmanaged, IBaseNumber<TIn> where TOut : unmanaged, IBaseNumber<TOut> where TSIn : class, IStorage<TIn, TSIn> where TSOut : class, IStorage<TOut, TSOut>
	{
		if (rows == lds && rows == ldd)
			return GeneralVectorsCast<TIn, TOut, TSIn, TSOut>(source.MakeReference(0, rows * cols), 1, destination, 1);
		if (!GetPointer(this, source, rows, cols, lds, out TIn* pA))
			return false;
		if (!GetPointer(this, destination, rows, cols, ldd, out TOut* pB))
			return false;
		return NMC.matDataConvert(TIn.Type, TOut.Type, true, rows, cols, pA, lds, pB, ldd).Check();
	}
	#endregion

	#region half matrix math
	/// <inheritdoc/>
	public virtual bool HalfMatrixUnary<T, TS1, TS2>(UnaryOperation op, bool upper, bool unitDiag, long rows, long cols, TS1 A, long lda, TS2 B, long ldb) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
	{
		if (!GetPointer(this, A, rows, cols, lda, out T* pA))
			return false;
		if (!GetPointer(this, B, rows, cols, ldb, out T* pB))
			return false;
		switch (op)
		{
			case UnaryOperation.Identity:
				return true;
			case UnaryOperation.Conjugate:
				T one = T.One;
				return NMC.triMatMulCopy(T.Type, upper, !unitDiag, MatrixOperation.Conjugate, rows, cols, &one, pA, lda, pB, ldb).Check();
			case UnaryOperation.Negate:
				T negOne = -T.One;
				return NMC.triMatMulCopy(T.Type, upper, !unitDiag, MatrixOperation.None, rows, cols, &negOne, pA, lda, pB, ldb).Check();
			default:
				return false;
		}
	}

	/// <inheritdoc/>
	public virtual bool HalfMatrixBinaryScalar<T, TS1, TS2>(BinaryScalarOperation op, bool upper, bool unitDiag, long rows, long cols, T scalar, TS1 A, long lda, TS2 B, long ldb) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
	{
		if (!GetPointer(this, A, rows, cols, lda, out T* pA))
			return false;
		if (!GetPointer(this, B, rows, cols, ldb, out T* pB))
			return false;
		return op switch
		{
			BinaryScalarOperation.Add => NMC.triMatAddCopy(T.Type, upper, !unitDiag, MatrixOperation.None, rows, cols, &scalar, pA, lda, pB, ldb).Check(),
			BinaryScalarOperation.Multiply => NMC.triMatMulCopy(T.Type, upper, !unitDiag, MatrixOperation.None, rows, cols, &scalar, pA, lda, pB, ldb).Check(),
			BinaryScalarOperation.Fill => NMC.triMatFillVal(T.Type, upper, unitDiag, rows, cols, &scalar, pB, ldb).Check(),
			_ => false,
		};
	}

	/// <inheritdoc/>
	public virtual bool HalfMatricesBinary<T, TS1, TS2, TS3>(BinaryOperation op, bool upper, bool unitDiag, long rows, long cols, TS1 A, long lda, TS2 B, long ldb, TS3 C, long ldc) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>
	{
		if (!GetPointer(this, A, rows, cols, lda, out T* pA))
			return false;
		if (!GetPointer(this, B, rows, cols, ldb, out T* pB))
			return false;
		if (!GetPointer(this, C, rows, cols, ldc, out T* pC))
			return false;
		T one = T.One;
		if (op == BinaryOperation.Add)
			return NMC.triMatAdd(T.Type, unitDiag, upper, MatrixOperation.None, MatrixOperation.None, rows, cols, &one, pA, lda, &one, pB, ldb, pC, ldc).Check();
		else
			return false;
	}

	/// <inheritdoc/>
	public virtual bool HalfMatrixReduce<T, TS>(ReduceOperation op, bool upper, bool triangular, bool unitDiagOrHerm, long rows, long cols, TS A, long lda, out T result) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
	{
		if (triangular && rows != cols)
			throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(cols));
		result = default;
		if (!GetPointer(this, A, rows, cols, lda, out T* pA))
			return false;
		if (triangular && (op == ReduceOperation.Multiply || op == ReduceOperation.MultiplyAbsolute || op == ReduceOperation.AbsoluteMininum))
		{
			result = T.Zero;
			return true;
		}
		T reduce = T.Zero;
		delegate*<DataType, bool, bool, long, long, T*, long, T*, CustomStatus> triFunc = op switch
		{
			ReduceOperation.Add => &NMC.triMatSum,
			ReduceOperation.AddAbsolute => &NMC.triMatAbsSum,
			ReduceOperation.Norm => &NMC.triMatAsVecNorm,
			_ => null,
		};
		delegate*<DataType, bool, bool, long, long, T*, long, out long, CustomStatus> funcInd = op switch
		{
			ReduceOperation.Maximum => &NMC.triMatArgMax,
			ReduceOperation.Mininum => &NMC.triMatArgMin,
			ReduceOperation.AbsoluteMaximum => &NMC.triMatArgAbsMax,
			_ => null,
		};
		delegate*<DataType, bool, bool, long, T*, long, T*, CustomStatus> symFunc = op switch
		{
			ReduceOperation.Add => &NMC.symmMatSum,
			ReduceOperation.AddAbsolute => &NMC.symmMatAbsSum,
			ReduceOperation.Multiply => &NMC.symmMatProd,
			ReduceOperation.MultiplyAbsolute => &NMC.symmMatAbsProd,
			ReduceOperation.Norm => &NMC.symmMatAsVecNorm,
			_ => null,
		};
		CustomStatus status = CustomStatus.NotSupported;
		if (triangular)
		{
			if (triFunc != null)
			{
				status = triFunc(T.Type, upper, unitDiagOrHerm, rows, cols, pA, lda, &reduce);
				result = reduce;
			}
			if (funcInd != null)
			{
				status = funcInd(T.Type, upper, unitDiagOrHerm, rows, cols, pA, lda, out long index);
				result = pA[index];
				if (op == ReduceOperation.AbsoluteMaximum || op == ReduceOperation.AbsoluteMininum)
					result = T.Abs(result);
			}
		}
		else
		{
			if (symFunc != null)
			{
				status = symFunc(T.Type, upper, false, rows, pA, lda, &reduce);
				result = reduce;
			}
			if (funcInd != null)
			{
				status = funcInd(T.Type, upper, false, rows, cols, pA, lda, out long index);
				result = pA[index];
				if (op == ReduceOperation.AbsoluteMaximum || op == ReduceOperation.AbsoluteMininum)
					result = T.Abs(result);
			}
		}
		return status.Check();
	}

	/// <inheritdoc/>
	public virtual bool HalfMatrixArgReduce<T, TS>(ReduceOperation op, bool upper, bool triangular, bool unitDiagOrHerm, long rows, long cols, TS A, long lda, out long index) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
	{
		if (triangular && rows != cols)
			throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(cols));
		index = -1;
		if (!GetPointer(this, A, rows, cols, lda, out T* pA))
			return false;
		if (op >= ReduceOperation.Add && op <= ReduceOperation.Norm)
			throw new ArgumentOutOfRangeException(nameof(op), op, Resources.ParameterError.InvalidValue);
		delegate*<DataType, bool, bool, long, long, T*, long, out long, CustomStatus> funcInd = op switch
		{
			ReduceOperation.Maximum => &NMC.triMatArgMax,
			ReduceOperation.Mininum => &NMC.triMatArgMin,
			ReduceOperation.AbsoluteMaximum => &NMC.triMatArgAbsMax,
			ReduceOperation.AbsoluteMininum => &NMC.triMatArgAbsMin,
			_ => null,
		};
		return funcInd != null && funcInd(T.Type, upper, triangular && unitDiagOrHerm, rows, cols, pA, lda, out index).Check();
	}

	/// <inheritdoc/>
	public virtual bool HalfMatrixColumnReduce<T, TS1, TS2>(ReduceOperation op, bool upper, bool triangular, bool unitDiagOrHerm, long rows, long cols, TS1 A, long lda, TS2 x, long strideX) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
	{
		if (triangular && rows != cols)
			throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(cols));
		if (triangular && (op == ReduceOperation.Multiply || op == ReduceOperation.MultiplyAbsolute))
		{
			x.MakeReference(0, cols - 1).FillWith(T.Zero);
			this.GeneralVectorReduce(op, A.MakeReference((cols - 1) * lda, rows), 1, out T res);
			x.MakeReference(cols - 1, 1).FromManaged(res);
			return true;
		}
		if (!GetPointer(this, A, rows, cols, lda, out T* pA))
			return false;
		if (!GetPointer(this, x, strideX, out T* px, out long n))
			return false;
		if (n < cols)
			throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(x));
		delegate*<DataType, bool, bool, long, long, T*, long, T*, long, CustomStatus> triFunc = op switch
		{
			ReduceOperation.Add => &NMC.triMatColsSum,
			ReduceOperation.AddAbsolute => &NMC.triMatColsAbsSum,
			ReduceOperation.Norm => &NMC.triMatColsNorm,
			_ => null
		};
		delegate*<DataType, bool, bool, long, T*, long, T*, long, CustomStatus> symFunc = op switch
		{
			ReduceOperation.Add => &NMC.symmMatColsSum,
			ReduceOperation.AddAbsolute => &NMC.symmMatColsAbsSum,
			ReduceOperation.Multiply => &NMC.symmMatColsProd,
			ReduceOperation.MultiplyAbsolute => &NMC.symmMatColsAbsProd,
			ReduceOperation.Norm => &NMC.symmMatColsNorm,
			_ => null
		};
		if (triangular)
			return triFunc != null && triFunc(T.Type, upper, unitDiagOrHerm, rows, cols, pA, lda, px, strideX).Check();
		else
			return symFunc != null && symFunc(T.Type, upper, unitDiagOrHerm, rows, pA, lda, px, strideX).Check();
	}

	/// <inheritdoc/>
	public virtual bool HalfMatrixColumnScan<T, TS1, TS2>(BinaryOperation op, bool inclusive, bool upper, bool triangular, bool unitDiagOrHerm, long rows, long cols, TS1 A, long lda, TS2 B, long ldb) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
	{
		if (triangular && rows != cols)
			throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(cols));
		if (!GetPointer(this, A, rows, cols, lda, out T* pA))
			return false;
		if (!GetPointer(this, B, rows, cols, ldb, out T* pB))
			return false;
		delegate*<DataType, bool, bool, bool, long, long, T*, long, T*, long, CustomStatus> triFunc = op switch
		{
			BinaryOperation.Add => &NMC.triMatColsParSum,
			_ => null
		};
		delegate*<DataType, bool, bool, bool, long, T*, long, T*, long, CustomStatus> symFunc = op switch
		{
			BinaryOperation.Add => &NMC.symmMatColsParSum,
			BinaryOperation.Multiply => &NMC.symmMatColsParProd,
			_ => null
		};
		if (triangular)
			return triFunc != null && triFunc(T.Type, inclusive, upper, unitDiagOrHerm, rows, cols, pA, lda, pB, ldb).Check();
		else
			return symFunc != null && symFunc(T.Type, inclusive, upper, unitDiagOrHerm, rows, pA, lda, pB, ldb).Check();
	}

	/// <inheritdoc/>
	public virtual bool HalfMatricesEqual<T, TS1, TS2>(bool upper, bool ignoreDiag, long rows, long cols, TS1 A, long lda, TS2 B, long ldb, out bool equals) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
	{
		equals = false;
		if (!GetPointer(this, A, rows, cols, lda, out T* pA))
			return false;
		if (!GetPointer(this, B, rows, cols, ldb, out T* pB))
			return false;
		return NMC.triMatsEq(T.Type, upper, ignoreDiag, rows, cols, pA, lda, pB, ldb, out equals).Check();
	}

	/// <inheritdoc/>
	public virtual bool HalfMatrixCast<TIn, TOut, TSIn, TSOut>(bool upper, bool ignoreDiag, long rows, long cols, TSIn source, long lds, TSOut destination, long ldd) where TIn : unmanaged, IBaseNumber<TIn> where TOut : unmanaged, IBaseNumber<TOut> where TSIn : class, IStorage<TIn, TSIn> where TSOut : class, IStorage<TOut, TSOut>
	{
		if (!GetPointer(this, source, rows, cols, lds, out TIn* pA))
			return false;
		if (!GetPointer(this, destination, rows, cols, ldd, out TOut* pB))
			return false;
		return NMC.triMatDataConvert(TIn.Type, TOut.Type, true, upper, ignoreDiag, rows, cols, pA, lds, pB, ldd).Check();
	}

	/// <inheritdoc/>
	public virtual bool TriangularMatricesAdd<T, TS1, TS2, TS3>(bool unitDiag, bool upper, MatrixOperation opA, MatrixOperation opB, long m, long n, T α, TS1? A, long lda, T β, TS2? B, long ldb, TS3 C, long ldc) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>
	{
		if (!GetPointer(this, A, opA.CanInPlace() ? m : n, opA.CanInPlace() ? n : m, lda, out T* pA))
			return false;
		if (!GetPointer(this, B, opB.CanInPlace() ? m : n, opB.CanInPlace() ? n : m, ldb, out T* pB))
			return false;
		if (!GetPointer(this, C, m, n, ldc, out T* pC))
			return false;
		return NMC.triMatAdd(T.Type, unitDiag, upper, opA, opB, m, n, &α, pA, lda, &β, pB, ldb, pC, ldc).Check();
	}

	/// <inheritdoc/>
	public virtual bool SymmetricMatricesAdd<T, TS1, TS2, TS3>(bool upperA, bool upperB, bool upperC, MatrixOperation opA, MatrixOperation opB, long n, T α, TS1? A, long lda, T β, TS2? B, long ldb, TS3 C, long ldc) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>
	{
		if (!GetPointer(this, A, n, n, lda, out T* pA))
			return false;
		if (!GetPointer(this, B, n, n, ldb, out T* pB))
			return false;
		if (!GetPointer(this, C, n, n, ldc, out T* pC))
			return false;
		return NMC.symmMatAdd(T.Type, upperA, upperB, upperC, opA, opB, n, &α, pA, lda, &β, pB, ldb, pC, ldc).Check();
	}

	/// <inheritdoc/>
	public virtual bool TriangularMatricesMultiply<T, TS1, TS2, TS3>(bool unitDiag, bool upper, MatrixOperation opA, MatrixOperation opB, long m, long n, long k, T α, TS1 A, long lda, TS2 B, long ldb, T β, TS3 C, long ldc) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>
	{
		if (α == T.Zero)
			throw new ArgumentOutOfRangeException(nameof(α), Resources.ParameterError.CannotZero);
		opA = opA.Simplify<T>(); opB = opB.Simplify<T>();
		if (!GetPointer(this, A, opA.CanInPlace() ? m : k, opA.CanInPlace() ? k : m, lda, out T* pA))
			return false;
		if (!GetPointer(this, B, opB.CanInPlace() ? k : n, opB.CanInPlace() ? n : k, ldb, out T* pB))
			return false;
		if (!GetPointer(this, C, m, n, ldc, out T* pC))
			return false;
		return NMC.triMatMul(T.Type, unitDiag, upper, opA, opB, m, n, k, &α, pA, lda, pB, ldb, &β, pC, ldc).Check();
	}

	/// <inheritdoc/>
	public virtual bool SymmetricMatricesMultiply<T, TS1, TS2, TS3>(bool upperA, bool upperB, bool hermA, bool hermB, MatrixOperation opA, MatrixOperation opB, long n, T α, TS1 A, long lda, TS2 B, long ldb, T β, TS3 C, long ldc) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>
	{
		if (!GetPointer(this, A, n, n, lda, out T* pA))
			return false;
		if (!GetPointer(this, B, n, n, ldb, out T* pB))
			return false;
		if (!GetPointer(this, C, n, n, ldc, out T* pC))
			return false;
		return NMC.symmMatMul(T.Type, upperA, upperB, hermA, hermB, opA, opB, n, &α, pA, lda, pB, ldb, &β, pC, ldc).Check();
	}

	/// <inheritdoc/>
	public virtual bool SymmetricMatrixToNormal<T, TS>(bool upper, bool hermitian, long n, TS A, long lda) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
	{
		if (!GetPointer(this, A, n, n, lda, out T* pA))
			return false;
		return NMC.matMakeHerm(T.Type, upper, hermitian, n, pA, lda).Check();
	}

	/// <inheritdoc/>
	public virtual bool HalfMatrixClearPart<T, TS>(bool clearDiag, bool clearLower, long m, long n, TS A, long lda) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
	{
		if (!GetPointer(this, A, m, n, lda, out T* pA))
			return false;
		return NMC.triMatClear(T.Type, clearLower, clearDiag, m, n, pA, lda).Check();
	}

	/// <inheritdoc/>
	public virtual bool HalfMatrixCopy<T, TS1, TS2>(bool upper, bool copyDiag, MatrixOperation opA, long m, long n, TS1 A, long lda, TS2 B, long ldb) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
	{
		if (!GetPointer(this, A, opA.CanInPlace() ? m : n, opA.CanInPlace() ? n : m, lda, out T* pA))
			return false;
		if (!GetPointer(this, B, m, n, ldb, out T* pB))
			return false;
		T one = T.One;
		return NMC.triMatMulCopy(T.Type, upper, copyDiag, opA, m, n, &one, pA, lda, pB, ldb).Check();
	}
	#endregion
}
