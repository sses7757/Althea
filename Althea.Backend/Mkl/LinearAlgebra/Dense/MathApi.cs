using System;
using System.Runtime.CompilerServices;

using Althea.LinearAlgebra;
using Althea.Numerics;

using NM = Althea.Backend.Mkl.LinearAlgebra.Dense.NativeMethods;
using NMC = Althea.Backend.Mkl.LinearAlgebra.Dense.CustomNativeMethods;


namespace Althea.Backend.Mkl.LinearAlgebra.Dense
{
	public unsafe partial class Api
	{
		#region vector math
		private static bool AdditionalUnary<T>(UnaryOperationSupplement op, long n, T* px, long strideX, T* py, long strideY) where T : unmanaged, INumber<T>
		{
			delegate*<long, T*, T*, void> func;
			delegate*<long, T*, long, T*, long, void> funcI;
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
			funcI = op switch
			{
				UnaryOperationSupplement.Exp => default(T) switch
				{
					float => &NM.vsExpI,
					double => &NM.vdExpI,
					Complex<float> => &NM.vcExpI,
					Complex<double> => &NM.vzExpI,
					_ => null,
				},
				UnaryOperationSupplement.Exp2 => default(T) switch
				{
					float => &NM.vsExp2I,
					double => &NM.vdExp2I,
					_ => null,
				},
				UnaryOperationSupplement.Exp10 => default(T) switch
				{
					float => &NM.vsExp10I,
					double => &NM.vdExp10I,
					_ => null,
				},
				UnaryOperationSupplement.ExpM1 => default(T) switch
				{
					float => &NM.vsExpm1I,
					double => &NM.vdExpm1I,
					_ => null,
				},
				UnaryOperationSupplement.Ln => default(T) switch
				{
					float => &NM.vsLnI,
					double => &NM.vdLnI,
					Complex<float> => &NM.vcLnI,
					Complex<double> => &NM.vzLnI,
					_ => null,
				},
				UnaryOperationSupplement.Log2 => default(T) switch
				{
					float => &NM.vsLog2I,
					double => &NM.vdLog2I,
					_ => null,
				},
				UnaryOperationSupplement.Log10 => default(T) switch
				{
					float => &NM.vsLog10I,
					double => &NM.vdLog10I,
					Complex<float> => &NM.vcLog10I,
					Complex<double> => &NM.vzLog10I,
					_ => null,
				},
				UnaryOperationSupplement.Log1p => default(T) switch
				{
					float => &NM.vsLog1pI,
					double => &NM.vdLog1pI,
					_ => null,
				},
				UnaryOperationSupplement.LogBinary => default(T) switch
				{
					float => &NM.vsLogbI,
					double => &NM.vdLogbI,
					_ => null,
				},
				UnaryOperationSupplement.Cos => default(T) switch
				{
					float => &NM.vsCosI,
					double => &NM.vdCosI,
					Complex<float> => &NM.vcCosI,
					Complex<double> => &NM.vzCosI,
					_ => null,
				},
				UnaryOperationSupplement.Sin => default(T) switch
				{
					float => &NM.vsSinI,
					double => &NM.vdSinI,
					Complex<float> => &NM.vcSinI,
					Complex<double> => &NM.vzSinI,
					_ => null,
				},
				UnaryOperationSupplement.Tan => default(T) switch
				{
					float => &NM.vsTanI,
					double => &NM.vdTanI,
					Complex<float> => &NM.vcTanI,
					Complex<double> => &NM.vzTanI,
					_ => null,
				},
				UnaryOperationSupplement.ArcCos => default(T) switch
				{
					float => &NM.vsAcosI,
					double => &NM.vdAcosI,
					Complex<float> => &NM.vcAcosI,
					Complex<double> => &NM.vzAcosI,
					_ => null,
				},
				UnaryOperationSupplement.ArcSin => default(T) switch
				{
					float => &NM.vsAsinI,
					double => &NM.vdAsinI,
					Complex<float> => &NM.vcAsinI,
					Complex<double> => &NM.vzAsinI,
					_ => null,
				},
				UnaryOperationSupplement.ArcTan => default(T) switch
				{
					float => &NM.vsAtanI,
					double => &NM.vdAtanI,
					Complex<float> => &NM.vcAtanI,
					Complex<double> => &NM.vzAtanI,
					_ => null,
				},
				UnaryOperationSupplement.Cosh => default(T) switch
				{
					float => &NM.vsCoshI,
					double => &NM.vdCoshI,
					Complex<float> => &NM.vcCoshI,
					Complex<double> => &NM.vzCoshI,
					_ => null,
				},
				UnaryOperationSupplement.Sinh => default(T) switch
				{
					float => &NM.vsSinhI,
					double => &NM.vdSinhI,
					Complex<float> => &NM.vcSinhI,
					Complex<double> => &NM.vzSinhI,
					_ => null,
				},
				UnaryOperationSupplement.Tanh => default(T) switch
				{
					float => &NM.vsTanhI,
					double => &NM.vdTanhI,
					Complex<float> => &NM.vcTanhI,
					Complex<double> => &NM.vzTanhI,
					_ => null,
				},
				UnaryOperationSupplement.ArcCosh => default(T) switch
				{
					float => &NM.vsAcoshI,
					double => &NM.vdAcoshI,
					Complex<float> => &NM.vcAcoshI,
					Complex<double> => &NM.vzAcoshI,
					_ => null,
				},
				UnaryOperationSupplement.ArcSinh => default(T) switch
				{
					float => &NM.vsAsinhI,
					double => &NM.vdAsinhI,
					Complex<float> => &NM.vcAsinhI,
					Complex<double> => &NM.vzAsinhI,
					_ => null,
				},
				UnaryOperationSupplement.ArcTanh => default(T) switch
				{
					float => &NM.vsAtanhI,
					double => &NM.vdAtanhI,
					Complex<float> => &NM.vcAtanhI,
					Complex<double> => &NM.vzAtanhI,
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
			return NMC.vecFillVal(Unmanaged<T>.DataType, n, &scalar, px, incx).Check();
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
			delegate*<long, T*, T*, void> func;
			delegate*<long, T*, long, T*, long, void> funcI;
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
			return func != null && func(Unmanaged<T>.DataType, n, &scalar, px, strideX, py, strideY).Check();
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
			return status.Check();
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
			return funcInd != null && funcInd(Unmanaged<T>.DataType, n, px, strideX, out index).Check();
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
			return func != null && func(Unmanaged<T>.DataType, inclusive, n, px, strideX, py, strideY).Check();
		}

		/// <inheritdoc/>
		public virtual bool GeneralVectorsCast<TIn, TOut, TSIn, TSOut>(TSIn source, long strideSource, TSOut destination, long strideDestination) where TIn : unmanaged, INumber<TIn> where TOut : unmanaged, INumber<TOut> where TSIn : class, IStorage<TIn, TSIn> where TSOut : class, IStorage<TOut, TSOut>
		{
			if (!GetPointer(source, strideSource, out TIn* px, out long n))
				return false;
			if (!GetPointer(destination, strideDestination, out TOut* py, out long ny))
				return false;
			n = Math.Min(n, ny);
			return NMC.vecDataConvert(Unmanaged<TIn>.DataType, Unmanaged<TOut>.DataType, true, n, px, strideSource, py, strideDestination).Check();
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
			return NMC.vecsEq(Unmanaged<T>.DataType, n, px, strideX, py, strideY, out equals).Check();
		}
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
			return NMC.matKron(Unmanaged<T>.DataType, &α, pA, lda, ma, na, pB, ldb, mb, nb, &β, pC, ldc).Check();
		}
		#endregion

		#region matrix math
		/// <inheritdoc/>
		public virtual bool GeneralMatrixUnary<T, TS1, TS2>(UnaryOperation op, long rows, long cols, TS1 A, long lda, TS2 B, long ldb) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
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
		public virtual bool GeneralMatrixBinaryScalar<T, TS1, TS2>(BinaryScalarOperation op, long rows, long cols, T scalar, TS1 A, long lda, TS2 B, long ldb) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
		{
			if (rows == lda && rows == ldb)
				return GeneralVectorBinaryScalar(op, scalar, A.MakeReference(0, rows * cols), 1, B, 1);
			if (!GetPointer(A, rows, cols, lda, out T* pA))
				return false;
			if (!GetPointer(B, rows, cols, ldb, out T* pB))
				return false;
			if (op == BinaryScalarOperation.Fill)
				return NMC.matFillVal(Unmanaged<T>.DataType, rows, cols, &scalar, pB, ldb).Check();
			delegate*<DataType, long, long, T*, T*, long, T*, long, CustomStatus> func = op switch
			{
				BinaryScalarOperation.Add => &NMC.matAddScalar,
				BinaryScalarOperation.Multiply => &NMC.matMulScalar,
				BinaryScalarOperation.Truncate => &NMC.matClip,
				_ => null,
			};
			return func != null && func(Unmanaged<T>.DataType, rows, cols, &scalar, pA, lda, pB, ldb).Check();
		}

		/// <inheritdoc/>
		public virtual bool GeneralMatricesBinary<T, TS1, TS2, TS3>(BinaryOperation op, long rows, long cols, TS1 A, long lda, TS2 B, long ldb, TS3 C, long ldc) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>
		{
			if (rows == lda && rows == ldb && rows == ldc)
				return GeneralVectorsBinary<T, TS1, TS2, TS3>(op, A.MakeReference(0, rows * cols), 1, B, 1, C, 1);
			else
				return false;
		}

		/// <inheritdoc/>
		public virtual bool GeneralMatrixReduce<T, TS>(ReduceOperation op, long rows, long cols, TS A, long lda, out T result) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>
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
				status = func(Unmanaged<T>.DataType, rows, cols, pA, lda, &reduce);
				result = reduce;
			}
			if (funcInd is not null)
			{
				status = funcInd(Unmanaged<T>.DataType, rows, cols, pA, lda, out long index);
				result = pA[index];
				if (op == ReduceOperation.AbsoluteMaximum || op == ReduceOperation.AbsoluteMininum)
					result = T.Abs(result);
			}
			return status.Check();
		}

		/// <inheritdoc/>
		public virtual bool GeneralMatrixArgReduce<T, TS>(ReduceOperation op, long rows, long cols, TS A, long lda, out long index) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>
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
			return funcInd != null && funcInd(Unmanaged<T>.DataType, rows, cols, pA, lda, out index).Check();
		}

		/// <inheritdoc/>
		public virtual bool GeneralMatrixColumnReduce<T, TS1, TS2>(ReduceOperation op, long rows, long cols, TS1 A, long lda, TS2 x, long strideX) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
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
			return func != null && func(Unmanaged<T>.DataType, rows, cols, pA, lda, px, strideX).Check();
		}

		/// <inheritdoc/>
		public virtual bool GeneralMatrixColumnScan<T, TS1, TS2>(BinaryOperation op, bool inclusive, long rows, long cols, TS1 A, long lda, TS2 B, long ldb) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
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
			return func != null && func(Unmanaged<T>.DataType, inclusive, rows, cols, pA, lda, pB, ldb).Check();
		}

		/// <inheritdoc/>
		public virtual bool GeneralMatricesEqual<T, TS1, TS2>(long rows, long cols, TS1 A, long lda, TS2 B, long ldb, out bool equals) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
		{
			if (rows == lda && rows == ldb)
				return GeneralVectorsEqual<T, TS1, TS2>(A.MakeReference(0, rows * cols), 1, B, 1, out equals);
			equals = false;
			if (!GetPointer(A, rows, cols, lda, out T* pA))
				return false;
			if (!GetPointer(B, rows, cols, ldb, out T* pB))
				return false;
			return NMC.matsEq(Unmanaged<T>.DataType, rows, cols, pA, lda, pB, ldb, out equals).Check();
		}

		/// <inheritdoc/>
		public virtual bool GeneralMatrixCast<TIn, TOut, TSIn, TSOut>(long rows, long cols, TSIn source, long lds, TSOut destination, long ldd) where TIn : unmanaged, INumber<TIn> where TOut : unmanaged, INumber<TOut> where TSIn : class, IStorage<TIn, TSIn> where TSOut : class, IStorage<TOut, TSOut>
		{
			if (rows == lds && rows == ldd)
				return GeneralVectorsCast<TIn, TOut, TSIn, TSOut>(source.MakeReference(0, rows * cols), 1, destination, 1);
			if (!GetPointer(source, rows, cols, lds, out TIn* pA))
				return false;
			if (!GetPointer(destination, rows, cols, ldd, out TOut* pB))
				return false;
			return NMC.matDataConvert(Unmanaged<TIn>.DataType, Unmanaged<TOut>.DataType, true, rows, cols, pA, lds, pB, ldd).Check();
		}
		#endregion

		#region half matrix math
		/// <inheritdoc/>
		public virtual bool HalfMatrixUnary<T, TS1, TS2>(UnaryOperation op, bool upper, bool unitDiag, long rows, long cols, TS1 A, long lda, TS2 B, long ldb) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
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
					return NMC.triMatMulCopy(Unmanaged<T>.DataType, upper, !unitDiag, MatrixOperation.Conjugate, rows, cols, &one, pA, lda, pB, ldb).Check();
				case UnaryOperation.Negate:
					T negOne = -T.One;
					return NMC.triMatMulCopy(Unmanaged<T>.DataType, upper, !unitDiag, MatrixOperation.None, rows, cols, &negOne, pA, lda, pB, ldb).Check();
				default:
					return false;
			}
		}

		/// <inheritdoc/>
		public virtual bool HalfMatrixBinaryScalar<T, TS1, TS2>(BinaryScalarOperation op, bool upper, bool unitDiag, long rows, long cols, T scalar, TS1 A, long lda, TS2 B, long ldb) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
		{
			if (!GetPointer(A, rows, cols, lda, out T* pA))
				return false;
			if (!GetPointer(B, rows, cols, ldb, out T* pB))
				return false;
			return op switch
			{
				BinaryScalarOperation.Add => NMC.triMatAddCopy(Unmanaged<T>.DataType, upper, !unitDiag, MatrixOperation.None, rows, cols, &scalar, pA, lda, pB, ldb).Check(),
				BinaryScalarOperation.Multiply => NMC.triMatMulCopy(Unmanaged<T>.DataType, upper, !unitDiag, MatrixOperation.None, rows, cols, &scalar, pA, lda, pB, ldb).Check(),
				BinaryScalarOperation.Fill => NMC.triMatFillVal(Unmanaged<T>.DataType, upper, unitDiag, rows, cols, &scalar, pB, ldb).Check(),
				_ => false,
			};
		}

		/// <inheritdoc/>
		public virtual bool HalfMatricesBinary<T, TS1, TS2, TS3>(BinaryOperation op, bool upper, bool unitDiag, long rows, long cols, TS1 A, long lda, TS2 B, long ldb, TS3 C, long ldc) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>
		{
			if (!GetPointer(A, rows, cols, lda, out T* pA))
				return false;
			if (!GetPointer(B, rows, cols, ldb, out T* pB))
				return false;
			if (!GetPointer(C, rows, cols, ldc, out T* pC))
				return false;
			T one = T.One;
			if (op == BinaryOperation.Add)
				return NMC.triMatAdd(Unmanaged<T>.DataType, unitDiag, upper, MatrixOperation.None, MatrixOperation.None, rows, cols, &one, pA, lda, &one, pB, ldb, pC, ldc).Check();
			else
				return false;
		}

		/// <inheritdoc/>
		public virtual bool HalfMatrixReduce<T, TS>(ReduceOperation op, bool upper, bool triangular, bool unitDiagOrHerm, long rows, long cols, TS A, long lda, out T result) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>
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
					status = triFunc(Unmanaged<T>.DataType, upper, unitDiagOrHerm, rows, cols, pA, lda, &reduce);
					result = reduce;
				}
				if (funcInd != null)
				{
					status = funcInd(Unmanaged<T>.DataType, upper, unitDiagOrHerm, rows, cols, pA, lda, out long index);
					result = pA[index];
					if (op == ReduceOperation.AbsoluteMaximum || op == ReduceOperation.AbsoluteMininum)
						result = T.Abs(result);
				}
			}
			else
			{
				if (symFunc != null)
				{
					status = symFunc(Unmanaged<T>.DataType, upper, false, rows, pA, lda, &reduce);
					result = reduce;
				}
				if (funcInd != null)
				{
					status = funcInd(Unmanaged<T>.DataType, upper, false, rows, cols, pA, lda, out long index);
					result = pA[index];
					if (op == ReduceOperation.AbsoluteMaximum || op == ReduceOperation.AbsoluteMininum)
						result = T.Abs(result);
				}
			}
			return status.Check();
		}

		/// <inheritdoc/>
		public virtual bool HalfMatrixArgReduce<T, TS>(ReduceOperation op, bool upper, bool triangular, bool unitDiagOrHerm, long rows, long cols, TS A, long lda, out long index) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>
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
			return funcInd != null && funcInd(Unmanaged<T>.DataType, upper, triangular && unitDiagOrHerm, rows, cols, pA, lda, out index).Check();
		}

		/// <inheritdoc/>
		public virtual bool HalfMatrixColumnReduce<T, TS1, TS2>(ReduceOperation op, bool upper, bool triangular, bool unitDiagOrHerm, long rows, long cols, TS1 A, long lda, TS2 x, long strideX) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
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
				return triFunc != null && triFunc(Unmanaged<T>.DataType, upper, unitDiagOrHerm, rows, cols, pA, lda, px, strideX).Check();
			else
				return symFunc != null && symFunc(Unmanaged<T>.DataType, upper, unitDiagOrHerm, rows, pA, lda, px, strideX).Check();
		}

		/// <inheritdoc/>
		public virtual bool HalfMatrixColumnScan<T, TS1, TS2>(BinaryOperation op, bool inclusive, bool upper, bool triangular, bool unitDiagOrHerm, long rows, long cols, TS1 A, long lda, TS2 B, long ldb) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
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
				return triFunc != null && triFunc(Unmanaged<T>.DataType, inclusive, upper, unitDiagOrHerm, rows, cols, pA, lda, pB, ldb).Check();
			else
				return symFunc != null && symFunc(Unmanaged<T>.DataType, inclusive, upper, unitDiagOrHerm, rows, pA, lda, pB, ldb).Check();
		}

		/// <inheritdoc/>
		public virtual bool HalfMatricesEqual<T, TS1, TS2>(bool upper, bool ignoreDiag, long rows, long cols, TS1 A, long lda, TS2 B, long ldb, out bool equals) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
		{
			equals = false;
			if (!GetPointer(A, rows, cols, lda, out T* pA))
				return false;
			if (!GetPointer(B, rows, cols, ldb, out T* pB))
				return false;
			return NMC.triMatsEq(Unmanaged<T>.DataType, upper, ignoreDiag, rows, cols, pA, lda, pB, ldb, out equals).Check();
		}

		/// <inheritdoc/>
		public virtual bool HalfMatrixCast<TIn, TOut, TSIn, TSOut>(bool upper, bool ignoreDiag, long rows, long cols, TSIn source, long lds, TSOut destination, long ldd) where TIn : unmanaged, INumber<TIn> where TOut : unmanaged, INumber<TOut> where TSIn : class, IStorage<TIn, TSIn> where TSOut : class, IStorage<TOut, TSOut>
		{
			if (!GetPointer(source, rows, cols, lds, out TIn* pA))
				return false;
			if (!GetPointer(destination, rows, cols, ldd, out TOut* pB))
				return false;
			return NMC.triMatDataConvert(Unmanaged<TIn>.DataType, Unmanaged<TOut>.DataType, true, upper, ignoreDiag, rows, cols, pA, lds, pB, ldd).Check();
		}
		#endregion
	}
}
