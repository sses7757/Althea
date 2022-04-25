using System.Runtime.CompilerServices;

using Althea.LinearAlgebra;
using Althea.NativeTypes;

using NM = Althea.Backend.Mkl.LinearAlgebra.Dense.NativeMethods;
using NMC = Althea.Backend.Mkl.LinearAlgebra.Dense.CustomNativeMethods;


namespace Althea.Backend.Mkl.LinearAlgebra.Dense
{
	public unsafe partial class Api
	{
		#region vector math
		private static bool AdditionalUnary<T>(UnaryOperationSupplement op, long n, T* px, long strideX, T* py, long strideY) where T : unmanaged, INumber<T>
		{
			delegate*<long, T*, T*, void> func = null;
			delegate*<long, T*, long, T*, long, void> funcI = null;
			func = op switch
			{
				UnaryOperationSupplement.Exp => default(T) switch
				{
					float => &NM.vsExp,
					double => &NM.vdExp,
					Complex<float> => &NM.vcExp,
					Complex<double> => &NM.vzExp,
					_ => null,
				},
				UnaryOperationSupplement.Exp2 => default(T) switch
				{
					float => &NM.vsExp2,
					double => &NM.vdExp2,
					_ => null,
				},
				UnaryOperationSupplement.Exp10 => default(T) switch
				{
					float => &NM.vsExp10,
					double => &NM.vdExp10,
					_ => null,
				},
				UnaryOperationSupplement.ExpM1 => default(T) switch
				{
					float => &NM.vsExpm1,
					double => &NM.vdExpm1,
					_ => null,
				},
				UnaryOperationSupplement.Ln => default(T) switch
				{
					float => &NM.vsLn,
					double => &NM.vdLn,
					Complex<float> => &NM.vcLn,
					Complex<double> => &NM.vzLn,
					_ => null,
				},
				UnaryOperationSupplement.Log2 => default(T) switch
				{
					float => &NM.vsLog2,
					double => &NM.vdLog2,
					_ => null,
				},
				UnaryOperationSupplement.Log10 => default(T) switch
				{
					float => &NM.vsLog10,
					double => &NM.vdLog10,
					Complex<float> => &NM.vcLog10,
					Complex<double> => &NM.vzLog10,
					_ => null,
				},
				UnaryOperationSupplement.Log1p => default(T) switch
				{
					float => &NM.vsLog1p,
					double => &NM.vdLog1p,
					_ => null,
				},
				UnaryOperationSupplement.LogBinary => default(T) switch
				{
					float => &NM.vsLogb,
					double => &NM.vdLogb,
					_ => null,
				},
				UnaryOperationSupplement.Cos => default(T) switch
				{
					float => &NM.vsCos,
					double => &NM.vdCos,
					Complex<float> => &NM.vcCos,
					Complex<double> => &NM.vzCos,
					_ => null,
				},
				UnaryOperationSupplement.Sin => default(T) switch
				{
					float => &NM.vsSin,
					double => &NM.vdSin,
					Complex<float> => &NM.vcSin,
					Complex<double> => &NM.vzSin,
					_ => null,
				},
				UnaryOperationSupplement.Tan => default(T) switch
				{
					float => &NM.vsTan,
					double => &NM.vdTan,
					Complex<float> => &NM.vcTan,
					Complex<double> => &NM.vzTan,
					_ => null,
				},
				UnaryOperationSupplement.ArcCos => default(T) switch
				{
					float => &NM.vsAcos,
					double => &NM.vdAcos,
					Complex<float> => &NM.vcAcos,
					Complex<double> => &NM.vzAcos,
					_ => null,
				},
				UnaryOperationSupplement.ArcSin => default(T) switch
				{
					float => &NM.vsAsin,
					double => &NM.vdAsin,
					Complex<float> => &NM.vcAsin,
					Complex<double> => &NM.vzAsin,
					_ => null,
				},
				UnaryOperationSupplement.ArcTan => default(T) switch
				{
					float => &NM.vsAtan,
					double => &NM.vdAtan,
					Complex<float> => &NM.vcAtan,
					Complex<double> => &NM.vzAtan,
					_ => null,
				},
				UnaryOperationSupplement.Cosh => default(T) switch
				{
					float => &NM.vsCosh,
					double => &NM.vdCosh,
					Complex<float> => &NM.vcCosh,
					Complex<double> => &NM.vzCosh,
					_ => null,
				},
				UnaryOperationSupplement.Sinh => default(T) switch
				{
					float => &NM.vsSinh,
					double => &NM.vdSinh,
					Complex<float> => &NM.vcSinh,
					Complex<double> => &NM.vzSinh,
					_ => null,
				},
				UnaryOperationSupplement.Tanh => default(T) switch
				{
					float => &NM.vsTanh,
					double => &NM.vdTanh,
					Complex<float> => &NM.vcTanh,
					Complex<double> => &NM.vzTanh,
					_ => null,
				},
				UnaryOperationSupplement.ArcCosh => default(T) switch
				{
					float => &NM.vsAcosh,
					double => &NM.vdAcosh,
					Complex<float> => &NM.vcAcosh,
					Complex<double> => &NM.vzAcosh,
					_ => null,
				},
				UnaryOperationSupplement.ArcSinh => default(T) switch
				{
					float => &NM.vsAsinh,
					double => &NM.vdAsinh,
					Complex<float> => &NM.vcAsinh,
					Complex<double> => &NM.vzAsinh,
					_ => null,
				},
				UnaryOperationSupplement.ArcTanh => default(T) switch
				{
					float => &NM.vsAtanh,
					double => &NM.vdAtanh,
					Complex<float> => &NM.vcAtanh,
					Complex<double> => &NM.vzAtanh,
					_ => null,
				},
				_ => null,
			};
			if (func == null)
				return false;
			if (strideX == 1 && strideY == 1)
				func(n, px, py);
			else
				funcI(n, px, strideX, py, strideY);
			return true;
		}

		private static bool FillWithValue<T>(T* px, long incx, long n, T scalar) where T : unmanaged, INumber<T>
		{
			if (incx == 1 && scalar == T.Zero)
			{
				Unsafe.InitBlockUnaligned(px, 0, (uint)(n * sizeof(T)));
				return true;
			}
			return NMC.vecFillVal(Unmanaged<T>.DataType, n, &scalar, px, incx) == CustomStatus.Success;
		}

		private static bool PowerScalar<T>(long n, T scalar, T* px, long strideX, T* py, long strideY) where T : unmanaged, INumber<T>
		{
			if (scalar == T.Zero)
				return FillWithValue(py, strideY, n, T.One);
			delegate*<long, T*, T*, void> simpleFunc = null;
			delegate*<long, T*, long, T*, long, void> simpleFuncI = null;
			if (scalar == -T.One)
			{
				simpleFunc = default(T) switch
				{
					float => &NM.vsInv,
					double => &NM.vdInv,
					_ => null,
				};
				simpleFuncI = default(T) switch
				{
					float => &NM.vsInvI,
					double => &NM.vdInvI,
					_ => null,
				};
			}
			if (scalar == T.One / (T.One + T.One))
			{
				simpleFunc = default(T) switch
				{
					float => &NM.vsSqrt,
					double => &NM.vdSqrt,
					Complex<float> => &NM.vcSqrt,
					Complex<double> => &NM.vzSqrt,
					_ => null,
				};
				simpleFuncI = default(T) switch
				{
					float => &NM.vsSqrtI,
					double => &NM.vdSqrtI,
					Complex<float> => &NM.vcSqrtI,
					Complex<double> => &NM.vzSqrtI,
					_ => null,
				};
			}
			if (scalar == -T.One / (T.One + T.One))
			{
				simpleFunc = default(T) switch
				{
					float => &NM.vsInvSqrt,
					double => &NM.vdInvSqrt,
					_ => null,
				};
				simpleFuncI = default(T) switch
				{
					float => &NM.vsInvSqrtI,
					double => &NM.vdInvSqrtI,
					_ => null,
				};
			}
			if (scalar == T.One / (T.One + T.One + T.One))
			{
				simpleFunc = default(T) switch
				{
					float => &NM.vsCbrt,
					double => &NM.vdCbrt,
					_ => null,
				};
				simpleFuncI = default(T) switch
				{
					float => &NM.vsCbrtI,
					double => &NM.vdCbrtI,
					_ => null,
				};
			}
			if (scalar == -T.One / (T.One + T.One + T.One))
			{
				simpleFunc = default(T) switch
				{
					float => &NM.vsInvCbrt,
					double => &NM.vdInvCbrt,
					_ => null,
				};
				simpleFuncI = default(T) switch
				{
					float => &NM.vsInvCbrtI,
					double => &NM.vdInvCbrtI,
					_ => null,
				};
			}
			if (scalar == (T.One + T.One) / (T.One + T.One + T.One))
			{
				simpleFunc = default(T) switch
				{
					float => &NM.vsPow2o3,
					double => &NM.vdPow2o3,
					_ => null,
				};
				simpleFuncI = default(T) switch
				{
					float => &NM.vsPow2o3I,
					double => &NM.vdPow2o3I,
					_ => null,
				};
			}
			if (scalar == (T.One + T.One + T.One) / (T.One + T.One))
			{
				simpleFunc = default(T) switch
				{
					float => &NM.vsPow3o2,
					double => &NM.vdPow3o2,
					_ => null,
				};
				simpleFuncI = default(T) switch
				{
					float => &NM.vsPow3o2I,
					double => &NM.vdPow3o2I,
					_ => null,
				};
			}
			if (simpleFunc != null)
			{
				if (strideX == 1 && strideY == 1)
					simpleFunc(n, px, py);
				else
					simpleFuncI(n, px, strideX, py, strideY);
				return true;
			}
			if (strideX == 1 && strideY == 1)
			{
				NM.vPowx<T>? func = default(T) switch
				{
					float => new NM.vPowx<float>(NM.vsPowx) as NM.vPowx<T>,
					double => new NM.vPowx<double>(NM.vdPowx) as NM.vPowx<T>,
					Complex<float> => new NM.vPowx<Complex<float>>(NM.vcPowx) as NM.vPowx<T>,
					Complex<double> => new NM.vPowx<Complex<double>>(NM.vzPowx) as NM.vPowx<T>,
					_ => null,
				};
				func?.Invoke(n, px, scalar, py);
				return func != null;
			}
			else
			{
				NM.vPowxI<T>? func = default(T) switch
				{
					float => new NM.vPowxI<float>(NM.vsPowxI) as NM.vPowxI<T>,
					double => new NM.vPowxI<double>(NM.vdPowxI) as NM.vPowxI<T>,
					Complex<float> => new NM.vPowxI<Complex<float>>(NM.vcPowxI) as NM.vPowxI<T>,
					Complex<double> => new NM.vPowxI<Complex<double>>(NM.vzPowxI) as NM.vPowxI<T>,
					_ => null,
				};
				func?.Invoke(n, px, strideX, scalar, py, strideY);
				return func != null;
			}
		}

		/// <inheritdoc/>
		public virtual bool GeneralVectorUnary<T, TS1, TS2>(UnaryOperation op, TS1 x, long strideX, TS2 y, long strideY) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
		{
			if (op == UnaryOperation.Identity || (op == UnaryOperation.Conjugate && !NumberType<T>.IsComplex))
				return true;
			if (op == UnaryOperation.Negate)
			{
				x.CopyTo<T, TS1, TS2>(y);
				return this.Scale(y, strideY, -T.One);
			}
			if (!GetPointer(x, strideX, out T* px, out long n))
				return false;
			if (!GetPointer(y, strideY, out T* py, out long ny))
				return false;
			n = Math.Min(n, ny);
			delegate*<long, T*, T*, void> func = null;
			delegate*<long, T*, long, T*, long, void> funcI = null;
			func = op switch
			{
				UnaryOperation.Conjugate => default(T) switch
				{
					Complex<float> => &NM.vcConj,
					Complex<double> => &NM.vzConj,
					_ => null,
				},
				UnaryOperation.AbsoluteValue => default(T) switch
				{
					float => &NM.vsAbs,
					double => &NM.vdAbs,
					Complex<float> => &NM.vcAbs,
					Complex<double> => &NM.vzAbs,
					_ => null,
				},
				_ => null,
			};
			funcI = op switch
			{
				UnaryOperation.Conjugate => default(T) switch
				{
					Complex<float> => &NM.vcConjI,
					Complex<double> => &NM.vzConjI,
					_ => null,
				},
				UnaryOperation.AbsoluteValue => default(T) switch
				{
					float => &NM.vsAbsI,
					double => &NM.vdAbsI,
					Complex<float> => &NM.vcAbsI,
					Complex<double> => &NM.vzAbsI,
					_ => null,
				},
				_ => null,
			};
			if (func == null)
				return AdditionalUnary((UnaryOperationSupplement)op, n, px, strideX, py, strideY);
			if (op == UnaryOperation.AbsoluteValue && NumberType<T>.IsComplex)
				strideY *= 2;
			if (strideX == 1 && strideY == 1)
				func(n, px, py);
			else
				funcI(n, px, strideX, py, strideY);
			return true;
		}

		/// <inheritdoc/>
		public virtual bool GeneralVectorBinaryScalar<T, TS1, TS2>(BinaryScalarOperation op, T scalar, TS1 x, long strideX, TS2 y, long strideY) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
		{
			if (!GetPointer(x, strideX, out T* px, out long n))
				return false;
			if (!GetPointer(y, strideY, out T* py, out long ny))
				return false;
			n = Math.Min(n, ny);
			if (op == BinaryScalarOperation.Fill)
				return FillWithValue(py, strideY, n, scalar);
			if (op == BinaryScalarOperation.Power)
				return PowerScalar(n, scalar, px, strideX, py, strideY);
			delegate*<DataType, long, T*, T*, long, T*, long, CustomStatus> func = op switch
			{
				BinaryScalarOperation.Add => &NMC.vecAddScalar,
				BinaryScalarOperation.Multiply => &NMC.vecMulScalar,
				BinaryScalarOperation.Truncate => &NMC.vecClip,
				_ => null,
			};
			return func != null && func(Unmanaged<T>.DataType, n, &scalar, px, strideX, py, strideY) == CustomStatus.Success;
		}

		/// <inheritdoc/>
		public virtual bool GeneralVectorReduce<T, TS>(ReduceOperation op, TS x, long strideX, out T result) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>
		{
			if (op == ReduceOperation.Norm)
				return this.Norm(x, strideX, out result);
			result = default;
			if (!GetPointer(x, strideX, out T* px, out long n))
				return false;
			T reduce = T.Zero;
			if (op == ReduceOperation.AddAbsolute)
			{
				if (typeof(T) == typeof(float))
					*(float*)&reduce = NM.cblas_sasum(n, px, strideX);
				else if (typeof(T) == typeof(double))
					*(double*)&reduce = NM.cblas_dasum(n, px, strideX);
			}
			if (reduce != T.Zero)
			{
				result = reduce;
				return true;
			}
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
				status = func(Unmanaged<T>.DataType, n, px, strideX, &reduce);
				result = reduce;
			}
			if (funcInd is not null)
			{
				status = funcInd(Unmanaged<T>.DataType, n, px, strideX, out long index);
				result = px[index];
				if (op == ReduceOperation.AbsoluteMaximum || op == ReduceOperation.AbsoluteMininum)
					result = T.Abs(result);
			}
			return status == CustomStatus.Success;
		}

		/// <inheritdoc/>
		public virtual bool GeneralVectorArgReduce<T, TS>(ReduceOperation op, TS x, long strideX, out long index) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>
		{
			if ((op == ReduceOperation.AbsoluteMaximum || op == ReduceOperation.AbsoluteMininum) && (typeof(T) == typeof(float) || typeof(T) == typeof(double)))
			{
				return op == ReduceOperation.AbsoluteMaximum ? this.AbsoluteValueArgMax<T, TS>(x, strideX, out index) : this.AbsoluteValueArgMin<T, TS>(x, strideX, out index);
			}
			index = -1;
			if (!GetPointer(x, strideX, out T* px, out long n))
				return false;
			delegate*<DataType, long, T*, long, T*, CustomStatus> func = op switch
			{
				ReduceOperation.Add => &NMC.vecSum,
				ReduceOperation.AddAbsolute => &NMC.vecAbsSum,
				ReduceOperation.Multiply => &NMC.vecProd,
				ReduceOperation.MultiplyAbsolute => &NMC.vecAbsProd,
				_ => null,
			};
			if (func != null)
				throw new ArgumentOutOfRangeException(nameof(op), op, Resources.ParameterError.InvalidValue);
			delegate*<DataType, long, T*, long, out long, CustomStatus> funcInd = op switch
			{
				ReduceOperation.Maximum => &NMC.vecArgMax,
				ReduceOperation.Mininum => &NMC.vecArgMin,
				ReduceOperation.AbsoluteMaximum => &NMC.vecArgAbsMax,
				ReduceOperation.AbsoluteMininum => &NMC.vecArgAbsMin,
				_ => null,
			};
			return funcInd != null && funcInd(Unmanaged<T>.DataType, n, px, strideX, out index) == CustomStatus.Success;
		}

		/// <inheritdoc/>
		public virtual bool GeneralVectorsBinary<T, TS1, TS2, TS3>(BinaryOperation op, TS1 x, long strideX, TS2 y, long strideY, TS3 z, long strideZ) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>
		{
			if (!GetPointer(x, strideX, out T* px, out long n))
				return false;
			if (!GetPointer(y, strideY, out T* py, out long ny))
				return false;
			if (!GetPointer(z, strideZ, out T* pz, out long nz))
				return false;
			n = Math.Min(n, Math.Min(ny, nz));
			delegate*<long, T*, T*, T*, void> func = op switch
			{
				BinaryOperation.Add => default(T) switch
				{
					float => &NM.vsAdd,
					double => &NM.vdAdd,
					Complex<float> => &NM.vcAdd,
					Complex<double> => &NM.vzAdd,
					_ => null
				},
				BinaryOperation.Multiply => default(T) switch
				{
					float => &NM.vsMul,
					double => &NM.vdMul,
					Complex<float> => &NM.vcMul,
					Complex<double> => &NM.vzMul,
					_ => null
				},
				BinaryOperation.Divide => default(T) switch
				{
					float => &NM.vsDiv,
					double => &NM.vdDiv,
					Complex<float> => &NM.vcDiv,
					Complex<double> => &NM.vzDiv,
					_ => null
				},
				BinaryOperation.Power => default(T) switch
				{
					float => &NM.vsPow,
					double => &NM.vdPow,
					Complex<float> => &NM.vcPow,
					Complex<double> => &NM.vzPow,
					_ => null
				},
				BinaryOperation.Maximum => default(T) switch
				{
					float => &NM.vsFmax,
					double => &NM.vdFmax,
					_ => null
				},
				BinaryOperation.Mininum => default(T) switch
				{
					float => &NM.vsFmin,
					double => &NM.vdFmin,
					_ => null
				},
				BinaryOperation.AbsoluteMaximum => default(T) switch
				{
					float => &NM.vsMaxMag,
					double => &NM.vdMaxMag,
					_ => null
				},
				BinaryOperation.AbsoluteMininum => default(T) switch
				{
					float => &NM.vsMinMag,
					double => &NM.vdMinMag,
					_ => null
				},
				_ => null
			};
			delegate*<long, T*, long, T*, long, T*, long, void> funcI = op switch
			{
				BinaryOperation.Add => default(T) switch
				{
					float => &NM.vsAddI,
					double => &NM.vdAddI,
					Complex<float> => &NM.vcAddI,
					Complex<double> => &NM.vzAddI,
					_ => null
				},
				BinaryOperation.Multiply => default(T) switch
				{
					float => &NM.vsMulI,
					double => &NM.vdMulI,
					Complex<float> => &NM.vcMulI,
					Complex<double> => &NM.vzMulI,
					_ => null
				},
				BinaryOperation.Divide => default(T) switch
				{
					float => &NM.vsDivI,
					double => &NM.vdDivI,
					Complex<float> => &NM.vcDivI,
					Complex<double> => &NM.vzDivI,
					_ => null
				},
				BinaryOperation.Power => default(T) switch
				{
					float => &NM.vsPowI,
					double => &NM.vdPowI,
					Complex<float> => &NM.vcPowI,
					Complex<double> => &NM.vzPowI,
					_ => null
				},
				BinaryOperation.Maximum => default(T) switch
				{
					float => &NM.vsFmaxI,
					double => &NM.vdFmaxI,
					_ => null
				},
				BinaryOperation.Mininum => default(T) switch
				{
					float => &NM.vsFminI,
					double => &NM.vdFminI,
					_ => null
				},
				BinaryOperation.AbsoluteMaximum => default(T) switch
				{
					float => &NM.vsMaxMagI,
					double => &NM.vdMaxMagI,
					_ => null
				},
				BinaryOperation.AbsoluteMininum => default(T) switch
				{
					float => &NM.vsMinMagI,
					double => &NM.vdMinMagI,
					_ => null
				},
				_ => null
			};
			if (func == null)
				return false;
			if (strideX == 1 && strideY == 1)
				func(n, px, py, pz);
			else
				funcI(n, px, strideX, py, strideY, pz, strideZ);
			return true;
		}

		/// <inheritdoc/>
		public virtual bool GeneralVectorsScan<T, TS1, TS2>(BinaryOperation op, TS1 x, long strideX, TS2 y, long strideY, bool inclusive) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
		{
			if (!GetPointer(x, strideX, out T* px, out long n))
				return false;
			if (!GetPointer(y, strideY, out T* py, out long ny))
				return false;
			if (ny < n)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(y));
			delegate*<DataType, bool, long, T*, long, T*, long, CustomStatus> func = op == BinaryOperation.Add ? &NMC.vecParSum : op == BinaryOperation.Multiply ? &NMC.vecParProd : null;
			return func != null && func(Unmanaged<T>.DataType, inclusive, n, px, strideX, py, strideY) == CustomStatus.Success;
		}

		/// <inheritdoc/>
		public virtual bool GeneralVectorsCast<TIn, TOut, TSIn, TSOut>(TSIn source, long strideSource, TSOut destination, long strideDestination) where TIn : unmanaged, INumber<TIn> where TOut : unmanaged, INumber<TOut> where TSIn : class, IStorage<TIn, TSIn> where TSOut : class, IStorage<TOut, TSOut>
		{
			if (!GetPointer(source, strideSource, out TIn* px, out long n))
				return false;
			if (!GetPointer(destination, strideDestination, out TOut* py, out long ny))
				return false;
			n = Math.Min(n, ny);
			return NMC.vecDataConvert(Unmanaged<TIn>.DataType, Unmanaged<TOut>.DataType, true, n, px, strideSource, py, strideDestination) == CustomStatus.Success;
		}

		/// <inheritdoc/>
		public virtual bool GeneralVectorsEqual<T, TS1, TS2>(TS1 x, long strideX, TS2 y, long strideY, out bool equals) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
		{
			equals = false;
			if (!GetPointer(x, strideX, out T* px, out long n))
				return false;
			if (!GetPointer(y, strideY, out T* py, out long ny))
				return false;
			if (ny != n)
				return true;
			return NMC.vecsEq(Unmanaged<T>.DataType, n, px, strideX, py, strideY, out equals) == CustomStatus.Success;
		}
		#endregion

		#region matrix math
		public virtual bool GeneralMatrixUnary<T, TS1, TS2>(UnaryOperation op, long rows, long cols, TS1 A, long lda, TS2 B, long ldb) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

		public virtual bool GeneralMatrixReduce<T, TS>(ReduceOperation op, long rows, long cols, TS A, long lda, out T result) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		public virtual bool GeneralMatrixArgReduce<T, TS>(ReduceOperation op, long rows, long cols, TS A, long lda, out long index) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		public virtual bool GeneralMatrixColumnReduce<T, TS1, TS2>(ReduceOperation op, long rows, long cols, TS1 A, long lda, TS2 x, long strideX) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

		public virtual bool GeneralMatrixBinaryScalar<T, TS1, TS2>(BinaryScalarOperation op, long rows, long cols, T scalar, TS1 A, long lda, TS2 B, long ldb) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

		public virtual bool GeneralMatricesBinary<T, TS1, TS2, TS3>(BinaryOperation op, long rows, long cols, TS1 A, long lda, TS2 B, long ldb, TS3 C, long ldc) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>;

		public virtual bool GeneralMatrixColumnScan<T, TS1, TS2>(BinaryOperation op, long rows, long cols, TS1 A, long lda, TS2 B, long ldb, bool inclusive) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

		public virtual bool GeneralMatricesEqual<T, TS1, TS2>(long rows, long cols, TS1 A, long lda, TS2 B, long ldb, out bool equals) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

		public virtual bool GeneralMatrixCast<TIn, TOut, TSIn, TSOut>(long rows, long cols, TSIn source, long lds, TSOut destination, long ldd) where TIn : unmanaged, INumber<TIn> where TOut : unmanaged, INumber<TOut> where TSIn : class, IStorage<TIn, TSIn> where TSOut : class, IStorage<TOut, TSOut>;
		#endregion

		#region matrix extended
		/// <inheritdoc/>
		public virtual bool MatrixKronecker<T, TS1, TS2, TS3>(long ma, long na, long mb, long nb, T α, TS1 A, long lda, TS2 B, long ldb, T β, TS3 C, long ldc) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>
		{
			if (!GetPointer(A, ma, na, lda, out T* pA))
				return false;
			if (!GetPointer(B, mb, nb, ldb, out T* pB))
				return false;
			if (!GetPointer(C, ma * mb, na * nb, ldc, out T* pC))
				return false;
			return NMC.matKron(Unmanaged<T>.DataType, &α, pA, lda, ma, na, pB, ldb, mb, nb, &β, pC, ldc) == CustomStatus.Success;
		}
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
	}
}
