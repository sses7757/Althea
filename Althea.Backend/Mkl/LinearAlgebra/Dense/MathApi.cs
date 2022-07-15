using System.Runtime.CompilerServices;

using Althea.LinearAlgebra;

using NM = Althea.Backend.Mkl.LinearAlgebra.Dense.NativeMethods;
using NMC = Althea.Backend.Mkl.LinearAlgebra.Dense.CustomNativeMethods;


namespace Althea.Backend.Mkl.LinearAlgebra.Dense
{
	public unsafe partial class Api
	{
		#region vector math
		private static bool AdditionalUnary<T>(UnaryOperationSupplement op, long n, T* px, long strideX, T* py, long strideY) where T : unmanaged, IBaseNumber<T>
		{
			delegate*<MklInt, T*, T*, void> func;
			delegate*<MklInt, T*, MklInt, T*, MklInt, void> funcI;
			func = op switch
			{
				UnaryOperationSupplement.Exp => default(T) switch
				{
					Float32 => &NM.vsExp,
					Float64 => &NM.vdExp,
					Complex<Float32> => &NM.vcExp,
					Complex<Float64> => &NM.vzExp,
					_ => null,
				},
				UnaryOperationSupplement.Exp2 => default(T) switch
				{
					Float32 => &NM.vsExp2,
					Float64 => &NM.vdExp2,
					_ => null,
				},
				UnaryOperationSupplement.Exp10 => default(T) switch
				{
					Float32 => &NM.vsExp10,
					Float64 => &NM.vdExp10,
					_ => null,
				},
				UnaryOperationSupplement.ExpM1 => default(T) switch
				{
					Float32 => &NM.vsExpm1,
					Float64 => &NM.vdExpm1,
					_ => null,
				},
				UnaryOperationSupplement.Ln => default(T) switch
				{
					Float32 => &NM.vsLn,
					Float64 => &NM.vdLn,
					Complex<Float32> => &NM.vcLn,
					Complex<Float64> => &NM.vzLn,
					_ => null,
				},
				UnaryOperationSupplement.Log2 => default(T) switch
				{
					Float32 => &NM.vsLog2,
					Float64 => &NM.vdLog2,
					_ => null,
				},
				UnaryOperationSupplement.Log10 => default(T) switch
				{
					Float32 => &NM.vsLog10,
					Float64 => &NM.vdLog10,
					Complex<Float32> => &NM.vcLog10,
					Complex<Float64> => &NM.vzLog10,
					_ => null,
				},
				UnaryOperationSupplement.Log1p => default(T) switch
				{
					Float32 => &NM.vsLog1p,
					Float64 => &NM.vdLog1p,
					_ => null,
				},
				UnaryOperationSupplement.LogBinary => default(T) switch
				{
					Float32 => &NM.vsLogb,
					Float64 => &NM.vdLogb,
					_ => null,
				},
				UnaryOperationSupplement.Cos => default(T) switch
				{
					Float32 => &NM.vsCos,
					Float64 => &NM.vdCos,
					Complex<Float32> => &NM.vcCos,
					Complex<Float64> => &NM.vzCos,
					_ => null,
				},
				UnaryOperationSupplement.Sin => default(T) switch
				{
					Float32 => &NM.vsSin,
					Float64 => &NM.vdSin,
					Complex<Float32> => &NM.vcSin,
					Complex<Float64> => &NM.vzSin,
					_ => null,
				},
				UnaryOperationSupplement.Tan => default(T) switch
				{
					Float32 => &NM.vsTan,
					Float64 => &NM.vdTan,
					Complex<Float32> => &NM.vcTan,
					Complex<Float64> => &NM.vzTan,
					_ => null,
				},
				UnaryOperationSupplement.ArcCos => default(T) switch
				{
					Float32 => &NM.vsAcos,
					Float64 => &NM.vdAcos,
					Complex<Float32> => &NM.vcAcos,
					Complex<Float64> => &NM.vzAcos,
					_ => null,
				},
				UnaryOperationSupplement.ArcSin => default(T) switch
				{
					Float32 => &NM.vsAsin,
					Float64 => &NM.vdAsin,
					Complex<Float32> => &NM.vcAsin,
					Complex<Float64> => &NM.vzAsin,
					_ => null,
				},
				UnaryOperationSupplement.ArcTan => default(T) switch
				{
					Float32 => &NM.vsAtan,
					Float64 => &NM.vdAtan,
					Complex<Float32> => &NM.vcAtan,
					Complex<Float64> => &NM.vzAtan,
					_ => null,
				},
				UnaryOperationSupplement.Cosh => default(T) switch
				{
					Float32 => &NM.vsCosh,
					Float64 => &NM.vdCosh,
					Complex<Float32> => &NM.vcCosh,
					Complex<Float64> => &NM.vzCosh,
					_ => null,
				},
				UnaryOperationSupplement.Sinh => default(T) switch
				{
					Float32 => &NM.vsSinh,
					Float64 => &NM.vdSinh,
					Complex<Float32> => &NM.vcSinh,
					Complex<Float64> => &NM.vzSinh,
					_ => null,
				},
				UnaryOperationSupplement.Tanh => default(T) switch
				{
					Float32 => &NM.vsTanh,
					Float64 => &NM.vdTanh,
					Complex<Float32> => &NM.vcTanh,
					Complex<Float64> => &NM.vzTanh,
					_ => null,
				},
				UnaryOperationSupplement.ArcCosh => default(T) switch
				{
					Float32 => &NM.vsAcosh,
					Float64 => &NM.vdAcosh,
					Complex<Float32> => &NM.vcAcosh,
					Complex<Float64> => &NM.vzAcosh,
					_ => null,
				},
				UnaryOperationSupplement.ArcSinh => default(T) switch
				{
					Float32 => &NM.vsAsinh,
					Float64 => &NM.vdAsinh,
					Complex<Float32> => &NM.vcAsinh,
					Complex<Float64> => &NM.vzAsinh,
					_ => null,
				},
				UnaryOperationSupplement.ArcTanh => default(T) switch
				{
					Float32 => &NM.vsAtanh,
					Float64 => &NM.vdAtanh,
					Complex<Float32> => &NM.vcAtanh,
					Complex<Float64> => &NM.vzAtanh,
					_ => null,
				},
				_ => null,
			};
			funcI = op switch
			{
				UnaryOperationSupplement.Exp => default(T) switch
				{
					Float32 => &NM.vsExpI,
					Float64 => &NM.vdExpI,
					Complex<Float32> => &NM.vcExpI,
					Complex<Float64> => &NM.vzExpI,
					_ => null,
				},
				UnaryOperationSupplement.Exp2 => default(T) switch
				{
					Float32 => &NM.vsExp2I,
					Float64 => &NM.vdExp2I,
					_ => null,
				},
				UnaryOperationSupplement.Exp10 => default(T) switch
				{
					Float32 => &NM.vsExp10I,
					Float64 => &NM.vdExp10I,
					_ => null,
				},
				UnaryOperationSupplement.ExpM1 => default(T) switch
				{
					Float32 => &NM.vsExpm1I,
					Float64 => &NM.vdExpm1I,
					_ => null,
				},
				UnaryOperationSupplement.Ln => default(T) switch
				{
					Float32 => &NM.vsLnI,
					Float64 => &NM.vdLnI,
					Complex<Float32> => &NM.vcLnI,
					Complex<Float64> => &NM.vzLnI,
					_ => null,
				},
				UnaryOperationSupplement.Log2 => default(T) switch
				{
					Float32 => &NM.vsLog2I,
					Float64 => &NM.vdLog2I,
					_ => null,
				},
				UnaryOperationSupplement.Log10 => default(T) switch
				{
					Float32 => &NM.vsLog10I,
					Float64 => &NM.vdLog10I,
					Complex<Float32> => &NM.vcLog10I,
					Complex<Float64> => &NM.vzLog10I,
					_ => null,
				},
				UnaryOperationSupplement.Log1p => default(T) switch
				{
					Float32 => &NM.vsLog1pI,
					Float64 => &NM.vdLog1pI,
					_ => null,
				},
				UnaryOperationSupplement.LogBinary => default(T) switch
				{
					Float32 => &NM.vsLogbI,
					Float64 => &NM.vdLogbI,
					_ => null,
				},
				UnaryOperationSupplement.Cos => default(T) switch
				{
					Float32 => &NM.vsCosI,
					Float64 => &NM.vdCosI,
					Complex<Float32> => &NM.vcCosI,
					Complex<Float64> => &NM.vzCosI,
					_ => null,
				},
				UnaryOperationSupplement.Sin => default(T) switch
				{
					Float32 => &NM.vsSinI,
					Float64 => &NM.vdSinI,
					Complex<Float32> => &NM.vcSinI,
					Complex<Float64> => &NM.vzSinI,
					_ => null,
				},
				UnaryOperationSupplement.Tan => default(T) switch
				{
					Float32 => &NM.vsTanI,
					Float64 => &NM.vdTanI,
					Complex<Float32> => &NM.vcTanI,
					Complex<Float64> => &NM.vzTanI,
					_ => null,
				},
				UnaryOperationSupplement.ArcCos => default(T) switch
				{
					Float32 => &NM.vsAcosI,
					Float64 => &NM.vdAcosI,
					Complex<Float32> => &NM.vcAcosI,
					Complex<Float64> => &NM.vzAcosI,
					_ => null,
				},
				UnaryOperationSupplement.ArcSin => default(T) switch
				{
					Float32 => &NM.vsAsinI,
					Float64 => &NM.vdAsinI,
					Complex<Float32> => &NM.vcAsinI,
					Complex<Float64> => &NM.vzAsinI,
					_ => null,
				},
				UnaryOperationSupplement.ArcTan => default(T) switch
				{
					Float32 => &NM.vsAtanI,
					Float64 => &NM.vdAtanI,
					Complex<Float32> => &NM.vcAtanI,
					Complex<Float64> => &NM.vzAtanI,
					_ => null,
				},
				UnaryOperationSupplement.Cosh => default(T) switch
				{
					Float32 => &NM.vsCoshI,
					Float64 => &NM.vdCoshI,
					Complex<Float32> => &NM.vcCoshI,
					Complex<Float64> => &NM.vzCoshI,
					_ => null,
				},
				UnaryOperationSupplement.Sinh => default(T) switch
				{
					Float32 => &NM.vsSinhI,
					Float64 => &NM.vdSinhI,
					Complex<Float32> => &NM.vcSinhI,
					Complex<Float64> => &NM.vzSinhI,
					_ => null,
				},
				UnaryOperationSupplement.Tanh => default(T) switch
				{
					Float32 => &NM.vsTanhI,
					Float64 => &NM.vdTanhI,
					Complex<Float32> => &NM.vcTanhI,
					Complex<Float64> => &NM.vzTanhI,
					_ => null,
				},
				UnaryOperationSupplement.ArcCosh => default(T) switch
				{
					Float32 => &NM.vsAcoshI,
					Float64 => &NM.vdAcoshI,
					Complex<Float32> => &NM.vcAcoshI,
					Complex<Float64> => &NM.vzAcoshI,
					_ => null,
				},
				UnaryOperationSupplement.ArcSinh => default(T) switch
				{
					Float32 => &NM.vsAsinhI,
					Float64 => &NM.vdAsinhI,
					Complex<Float32> => &NM.vcAsinhI,
					Complex<Float64> => &NM.vzAsinhI,
					_ => null,
				},
				UnaryOperationSupplement.ArcTanh => default(T) switch
				{
					Float32 => &NM.vsAtanhI,
					Float64 => &NM.vdAtanhI,
					Complex<Float32> => &NM.vcAtanhI,
					Complex<Float64> => &NM.vzAtanhI,
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

		private static bool FillWithValue<T>(T* px, long incx, long n, T scalar) where T : unmanaged, IBaseNumber<T>
		{
			if (incx == 1 && scalar == T.Zero)
			{
				Unsafe.InitBlockUnaligned(px, 0, (uint)(n * sizeof(T)));
				return true;
			}
			return NMC.vecFillVal(T.Type, n, &scalar, px, incx).Check();
		}

		private static bool PowerScalar<T>(long n, T scalar, T* px, long strideX, T* py, long strideY) where T : unmanaged, IBaseNumber<T>
		{
			if (scalar == T.Zero)
				return FillWithValue(py, strideY, n, T.One);
			delegate*<MklInt, T*, T*, void> simpleFunc = null;
			delegate*<MklInt, T*, MklInt, T*, MklInt, void> simpleFuncI = null;
			if (scalar == -T.One)
			{
				simpleFunc = default(T) switch
				{
					Float32 => &NM.vsInv,
					Float64 => &NM.vdInv,
					_ => null,
				};
				simpleFuncI = default(T) switch
				{
					Float32 => &NM.vsInvI,
					Float64 => &NM.vdInvI,
					_ => null,
				};
			}
			if (scalar == T.One / (T.One + T.One))
			{
				simpleFunc = default(T) switch
				{
					Float32 => &NM.vsSqrt,
					Float64 => &NM.vdSqrt,
					Complex<Float32> => &NM.vcSqrt,
					Complex<Float64> => &NM.vzSqrt,
					_ => null,
				};
				simpleFuncI = default(T) switch
				{
					Float32 => &NM.vsSqrtI,
					Float64 => &NM.vdSqrtI,
					Complex<Float32> => &NM.vcSqrtI,
					Complex<Float64> => &NM.vzSqrtI,
					_ => null,
				};
			}
			if (scalar == -T.One / (T.One + T.One))
			{
				simpleFunc = default(T) switch
				{
					Float32 => &NM.vsInvSqrt,
					Float64 => &NM.vdInvSqrt,
					_ => null,
				};
				simpleFuncI = default(T) switch
				{
					Float32 => &NM.vsInvSqrtI,
					Float64 => &NM.vdInvSqrtI,
					_ => null,
				};
			}
			if (scalar == T.One / (T.One + T.One + T.One))
			{
				simpleFunc = default(T) switch
				{
					Float32 => &NM.vsCbrt,
					Float64 => &NM.vdCbrt,
					_ => null,
				};
				simpleFuncI = default(T) switch
				{
					Float32 => &NM.vsCbrtI,
					Float64 => &NM.vdCbrtI,
					_ => null,
				};
			}
			if (scalar == -T.One / (T.One + T.One + T.One))
			{
				simpleFunc = default(T) switch
				{
					Float32 => &NM.vsInvCbrt,
					Float64 => &NM.vdInvCbrt,
					_ => null,
				};
				simpleFuncI = default(T) switch
				{
					Float32 => &NM.vsInvCbrtI,
					Float64 => &NM.vdInvCbrtI,
					_ => null,
				};
			}
			if (scalar == (T.One + T.One) / (T.One + T.One + T.One))
			{
				simpleFunc = default(T) switch
				{
					Float32 => &NM.vsPow2o3,
					Float64 => &NM.vdPow2o3,
					_ => null,
				};
				simpleFuncI = default(T) switch
				{
					Float32 => &NM.vsPow2o3I,
					Float64 => &NM.vdPow2o3I,
					_ => null,
				};
			}
			if (scalar == (T.One + T.One + T.One) / (T.One + T.One))
			{
				simpleFunc = default(T) switch
				{
					Float32 => &NM.vsPow3o2,
					Float64 => &NM.vdPow3o2,
					_ => null,
				};
				simpleFuncI = default(T) switch
				{
					Float32 => &NM.vsPow3o2I,
					Float64 => &NM.vdPow3o2I,
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
					Float32 => new NM.vPowx<Float32>(NM.vsPowx) as NM.vPowx<T>,
					Float64 => new NM.vPowx<Float64>(NM.vdPowx) as NM.vPowx<T>,
					Complex<Float32> => new NM.vPowx<Complex<Float32>>(NM.vcPowx) as NM.vPowx<T>,
					Complex<Float64> => new NM.vPowx<Complex<Float64>>(NM.vzPowx) as NM.vPowx<T>,
					_ => null,
				};
				func?.Invoke(n, px, scalar, py);
				return func != null;
			}
			else
			{
				NM.vPowxI<T>? func = default(T) switch
				{
					Float32 => new NM.vPowxI<Float32>(NM.vsPowxI) as NM.vPowxI<T>,
					Float64 => new NM.vPowxI<Float64>(NM.vdPowxI) as NM.vPowxI<T>,
					Complex<Float32> => new NM.vPowxI<Complex<Float32>>(NM.vcPowxI) as NM.vPowxI<T>,
					Complex<Float64> => new NM.vPowxI<Complex<Float64>>(NM.vzPowxI) as NM.vPowxI<T>,
					_ => null,
				};
				func?.Invoke(n, px, strideX, scalar, py, strideY);
				return func != null;
			}
		}

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
			if (!GetPointer(x, strideX, out T* px, out long n))
				return false;
			if (!GetPointer(y, strideY, out T* py, out long ny))
				return false;
			n = Math.Min(n, ny);
			delegate*<MklInt, T*, T*, void> func;
			delegate*<MklInt, T*, MklInt, T*, MklInt, void> funcI;
			func = op switch
			{
				UnaryOperation.Conjugate => default(T) switch
				{
					Complex<Float32> => &NM.vcConj,
					Complex<Float64> => &NM.vzConj,
					_ => null,
				},
				UnaryOperation.AbsoluteValue => default(T) switch
				{
					Float32 => &NM.vsAbs,
					Float64 => &NM.vdAbs,
					Complex<Float32> => &NM.vcAbs,
					Complex<Float64> => &NM.vzAbs,
					_ => null,
				},
				_ => null,
			};
			funcI = op switch
			{
				UnaryOperation.Conjugate => default(T) switch
				{
					Complex<Float32> => &NM.vcConjI,
					Complex<Float64> => &NM.vzConjI,
					_ => null,
				},
				UnaryOperation.AbsoluteValue => default(T) switch
				{
					Float32 => &NM.vsAbsI,
					Float64 => &NM.vdAbsI,
					Complex<Float32> => &NM.vcAbsI,
					Complex<Float64> => &NM.vzAbsI,
					_ => null,
				},
				_ => null,
			};
			if (func == null)
				return AdditionalUnary((UnaryOperationSupplement)op, n, px, strideX, py, strideY);
			if (op == UnaryOperation.AbsoluteValue && T.IsComplexType)
				strideY *= 2;
			if (strideX == 1 && strideY == 1)
				func(n, px, py);
			else
				funcI(n, px, strideX, py, strideY);
			return true;
		}

		/// <inheritdoc/>
		public virtual bool GeneralVectorBinaryScalar<T, TS1, TS2>(BinaryScalarOperation op, T scalar, TS1 x, long strideX, TS2 y, long strideY) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
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
			return func != null && func(T.Type, n, &scalar, px, strideX, py, strideY).Check();
		}

		/// <inheritdoc/>
		public virtual bool GeneralVectorReduce<T, TS>(ReduceOperation op, TS x, long strideX, out T result) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
		{
			if (op == ReduceOperation.Norm)
				return this.Norm(x, strideX, out result);
			result = default;
			if (!GetPointer(x, strideX, out T* px, out long n))
				return false;
			T reduce = T.Zero;
			if (op == ReduceOperation.AddAbsolute)
			{
				if (typeof(T) == typeof(Float32))
					*(float*)&reduce = NM.cblas_sasum(n, px, strideX);
				else if (typeof(T) == typeof(Float64))
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
			if (!GetPointer(x, strideX, out T* px, out long n))
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
			if (!GetPointer(x, strideX, out T* px, out long n))
				return false;
			if (!GetPointer(y, strideY, out T* py, out long ny))
				return false;
			if (!GetPointer(z, strideZ, out T* pz, out long nz))
				return false;
			n = Math.Min(n, Math.Min(ny, nz));
			delegate*<MklInt, T*, T*, T*, void> func = op switch
			{
				BinaryOperation.Add => default(T) switch
				{
					Float32 => &NM.vsAdd,
					Float64 => &NM.vdAdd,
					Complex<Float32> => &NM.vcAdd,
					Complex<Float64> => &NM.vzAdd,
					_ => null
				},
				BinaryOperation.Multiply => default(T) switch
				{
					Float32 => &NM.vsMul,
					Float64 => &NM.vdMul,
					Complex<Float32> => &NM.vcMul,
					Complex<Float64> => &NM.vzMul,
					_ => null
				},
				BinaryOperation.Divide => default(T) switch
				{
					Float32 => &NM.vsDiv,
					Float64 => &NM.vdDiv,
					Complex<Float32> => &NM.vcDiv,
					Complex<Float64> => &NM.vzDiv,
					_ => null
				},
				BinaryOperation.Power => default(T) switch
				{
					Float32 => &NM.vsPow,
					Float64 => &NM.vdPow,
					Complex<Float32> => &NM.vcPow,
					Complex<Float64> => &NM.vzPow,
					_ => null
				},
				BinaryOperation.Maximum => default(T) switch
				{
					Float32 => &NM.vsFmax,
					Float64 => &NM.vdFmax,
					_ => null
				},
				BinaryOperation.Mininum => default(T) switch
				{
					Float32 => &NM.vsFmin,
					Float64 => &NM.vdFmin,
					_ => null
				},
				BinaryOperation.AbsoluteMaximum => default(T) switch
				{
					Float32 => &NM.vsMaxMag,
					Float64 => &NM.vdMaxMag,
					_ => null
				},
				BinaryOperation.AbsoluteMininum => default(T) switch
				{
					Float32 => &NM.vsMinMag,
					Float64 => &NM.vdMinMag,
					_ => null
				},
				_ => null
			};
			delegate*<MklInt, T*, MklInt, T*, MklInt, T*, MklInt, void> funcI = op switch
			{
				BinaryOperation.Add => default(T) switch
				{
					Float32 => &NM.vsAddI,
					Float64 => &NM.vdAddI,
					Complex<Float32> => &NM.vcAddI,
					Complex<Float64> => &NM.vzAddI,
					_ => null
				},
				BinaryOperation.Multiply => default(T) switch
				{
					Float32 => &NM.vsMulI,
					Float64 => &NM.vdMulI,
					Complex<Float32> => &NM.vcMulI,
					Complex<Float64> => &NM.vzMulI,
					_ => null
				},
				BinaryOperation.Divide => default(T) switch
				{
					Float32 => &NM.vsDivI,
					Float64 => &NM.vdDivI,
					Complex<Float32> => &NM.vcDivI,
					Complex<Float64> => &NM.vzDivI,
					_ => null
				},
				BinaryOperation.Power => default(T) switch
				{
					Float32 => &NM.vsPowI,
					Float64 => &NM.vdPowI,
					Complex<Float32> => &NM.vcPowI,
					Complex<Float64> => &NM.vzPowI,
					_ => null
				},
				BinaryOperation.Maximum => default(T) switch
				{
					Float32 => &NM.vsFmaxI,
					Float64 => &NM.vdFmaxI,
					_ => null
				},
				BinaryOperation.Mininum => default(T) switch
				{
					Float32 => &NM.vsFminI,
					Float64 => &NM.vdFminI,
					_ => null
				},
				BinaryOperation.AbsoluteMaximum => default(T) switch
				{
					Float32 => &NM.vsMaxMagI,
					Float64 => &NM.vdMaxMagI,
					_ => null
				},
				BinaryOperation.AbsoluteMininum => default(T) switch
				{
					Float32 => &NM.vsMinMagI,
					Float64 => &NM.vdMinMagI,
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
		public virtual bool GeneralVectorsScan<T, TS1, TS2>(BinaryOperation op, TS1 x, long strideX, TS2 y, long strideY, bool inclusive) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
		{
			if (!GetPointer(x, strideX, out T* px, out long n))
				return false;
			if (!GetPointer(y, strideY, out T* py, out long ny))
				return false;
			if (ny < n)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(y));
			delegate*<DataType, bool, long, T*, long, T*, long, CustomStatus> func = op == BinaryOperation.Add ? &NMC.vecParSum : op == BinaryOperation.Multiply ? &NMC.vecParProd : null;
			return func != null && func(T.Type, inclusive, n, px, strideX, py, strideY).Check();
		}

		/// <inheritdoc/>
		public virtual bool GeneralVectorsCast<TIn, TOut, TSIn, TSOut>(TSIn source, long strideSource, TSOut destination, long strideDestination) where TIn : unmanaged, IBaseNumber<TIn> where TOut : unmanaged, IBaseNumber<TOut> where TSIn : class, IStorage<TIn, TSIn> where TSOut : class, IStorage<TOut, TSOut>
		{
			if (!GetPointer(source, strideSource, out TIn* px, out long n))
				return false;
			if (!GetPointer(destination, strideDestination, out TOut* py, out long ny))
				return false;
			n = Math.Min(n, ny);
			return NMC.vecDataConvert(TIn.Type, TOut.Type, true, n, px, strideSource, py, strideDestination).Check();
		}

		/// <inheritdoc/>
		public virtual bool GeneralVectorsEqual<T, TS1, TS2>(TS1 x, long strideX, TS2 y, long strideY, out bool equals) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
		{
			equals = false;
			if (!GetPointer(x, strideX, out T* px, out long n))
				return false;
			if (!GetPointer(y, strideY, out T* py, out long ny))
				return false;
			if (ny != n)
				return true;
			return NMC.vecsEq(T.Type, n, px, strideX, py, strideY, out equals).Check();
		}
		#endregion

		#region matrix extended
		/// <inheritdoc/>
		public virtual bool MatrixKronecker<T, TS1, TS2, TS3>(long ma, long na, long mb, long nb, T α, TS1 A, long lda, TS2 B, long ldb, T β, TS3 C, long ldc) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>
		{
			if (!GetPointer(A, ma, na, lda, out T* pA))
				return false;
			if (!GetPointer(B, mb, nb, ldb, out T* pB))
				return false;
			if (!GetPointer(C, ma * mb, na * nb, ldc, out T* pC))
				return false;
			return NMC.matKron(T.Type, &α, pA, lda, ma, na, pB, ldb, mb, nb, &β, pC, ldc).Check();
		}
		#endregion

		#region matrix math
		/// <inheritdoc/>
		public virtual bool GeneralMatrixUnary<T, TS1, TS2>(UnaryOperation op, long rows, long cols, TS1 A, long lda, TS2 B, long ldb) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
		{
			if (rows == lda && rows == ldb)
				return GeneralVectorUnary<T, TS1, TS2>(op, A.MakeReference(0, rows * cols), 1, B, 1);
			if (!GetPointer(A, rows, cols, lda, out T* pA))
				return false;
			if (!GetPointer(B, rows, cols, ldb, out T* pB))
				return false;
			switch (op)
			{
				case UnaryOperation.Identity:
					return true;
				case UnaryOperation.Conjugate:
					if (pA == pB)
						return Conjugater.Conjugate(pB, rows, cols, ldb);
					else
						return Storage.Api.PointerMemoryCopy2D(pA, lda, pB, ldb, rows, cols, MatrixOperation.Conjugate);
				case UnaryOperation.Negate:
					if (pA == pB)
						return Conjugater.Scale(pB, rows, cols, ldb, -T.One);
					else
						return Storage.Api.PointerMemoryCopy2D(pA, lda, pB, ldb, rows, cols, default, -T.One);
				default:
					return false;
			}
		}

		/// <inheritdoc/>
		public virtual bool GeneralMatrixBinaryScalar<T, TS1, TS2>(BinaryScalarOperation op, long rows, long cols, T scalar, TS1 A, long lda, TS2 B, long ldb) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
		{
			if (rows == lda && rows == ldb)
				return GeneralVectorBinaryScalar(op, scalar, A.MakeReference(0, rows * cols), 1, B, 1);
			if (!GetPointer(A, rows, cols, lda, out T* pA))
				return false;
			if (!GetPointer(B, rows, cols, ldb, out T* pB))
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
			if (!GetPointer(A, rows, cols, lda, out T* pA))
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
			if (!GetPointer(A, rows, cols, lda, out T* pA))
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
			if (!GetPointer(A, rows, cols, lda, out T* pA))
				return false;
			if (!GetPointer(x, strideX, out T* px, out long n))
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
			if (!GetPointer(A, rows, cols, lda, out T* pA))
				return false;
			if (!GetPointer(B, rows, cols, ldb, out T* pB))
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
			if (!GetPointer(A, rows, cols, lda, out T* pA))
				return false;
			if (!GetPointer(B, rows, cols, ldb, out T* pB))
				return false;
			return NMC.matsEq(T.Type, rows, cols, pA, lda, pB, ldb, out equals).Check();
		}

		/// <inheritdoc/>
		public virtual bool GeneralMatrixCast<TIn, TOut, TSIn, TSOut>(long rows, long cols, TSIn source, long lds, TSOut destination, long ldd) where TIn : unmanaged, IBaseNumber<TIn> where TOut : unmanaged, IBaseNumber<TOut> where TSIn : class, IStorage<TIn, TSIn> where TSOut : class, IStorage<TOut, TSOut>
		{
			if (rows == lds && rows == ldd)
				return GeneralVectorsCast<TIn, TOut, TSIn, TSOut>(source.MakeReference(0, rows * cols), 1, destination, 1);
			if (!GetPointer(source, rows, cols, lds, out TIn* pA))
				return false;
			if (!GetPointer(destination, rows, cols, ldd, out TOut* pB))
				return false;
			return NMC.matDataConvert(TIn.Type, TOut.Type, true, rows, cols, pA, lds, pB, ldd).Check();
		}
		#endregion

		#region half matrix math
		/// <inheritdoc/>
		public virtual bool HalfMatrixUnary<T, TS1, TS2>(UnaryOperation op, bool upper, bool unitDiag, long rows, long cols, TS1 A, long lda, TS2 B, long ldb) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
		{
			if (!GetPointer(A, rows, cols, lda, out T* pA))
				return false;
			if (!GetPointer(B, rows, cols, ldb, out T* pB))
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
			if (!GetPointer(A, rows, cols, lda, out T* pA))
				return false;
			if (!GetPointer(B, rows, cols, ldb, out T* pB))
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
			if (!GetPointer(A, rows, cols, lda, out T* pA))
				return false;
			if (!GetPointer(B, rows, cols, ldb, out T* pB))
				return false;
			if (!GetPointer(C, rows, cols, ldc, out T* pC))
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
			if (!GetPointer(A, rows, cols, lda, out T* pA))
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
			if (!GetPointer(A, rows, cols, lda, out T* pA))
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
			if (!GetPointer(A, rows, cols, lda, out T* pA))
				return false;
			if (!GetPointer(x, strideX, out T* px, out long n))
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
			if (!GetPointer(A, rows, cols, lda, out T* pA))
				return false;
			if (!GetPointer(B, rows, cols, ldb, out T* pB))
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
			if (!GetPointer(A, rows, cols, lda, out T* pA))
				return false;
			if (!GetPointer(B, rows, cols, ldb, out T* pB))
				return false;
			return NMC.triMatsEq(T.Type, upper, ignoreDiag, rows, cols, pA, lda, pB, ldb, out equals).Check();
		}

		/// <inheritdoc/>
		public virtual bool HalfMatrixCast<TIn, TOut, TSIn, TSOut>(bool upper, bool ignoreDiag, long rows, long cols, TSIn source, long lds, TSOut destination, long ldd) where TIn : unmanaged, IBaseNumber<TIn> where TOut : unmanaged, IBaseNumber<TOut> where TSIn : class, IStorage<TIn, TSIn> where TSOut : class, IStorage<TOut, TSOut>
		{
			if (!GetPointer(source, rows, cols, lds, out TIn* pA))
				return false;
			if (!GetPointer(destination, rows, cols, ldd, out TOut* pB))
				return false;
			return NMC.triMatDataConvert(TIn.Type, TOut.Type, true, upper, ignoreDiag, rows, cols, pA, lds, pB, ldd).Check();
		}
		#endregion
	}
}
