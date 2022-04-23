using System.Numerics;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

using Althea.Backend.Storage;
using Althea.Linq;
using Althea.LinearAlgebra;
using Althea.NativeTypes;


namespace Althea.Backend.CSharp.LinearAlgebra
{
	public unsafe partial class Api
	{
		internal struct U_AddScalar { }
		internal struct U_MultiplyScalar { }
		internal struct U_Modulo { }
		private struct U_PowerT { }
		private struct U_PowerDouble { }
		private struct U_Truncate { }
		private struct U_Conjugate { }
		private struct U_Sqrt { }
		private struct U_Square { }
		private struct U_Reciprocal { }
		private enum Modify
		{
			AddScalar,
			MultiplyScalar,
			Modulo,
			PowerT,
			PowerDouble,
			Truncate,
			Conjugate,
			Sqrt,
			Square,
			Reciprocal,
		}

		private static class OtherOp<T> where T : unmanaged, INumber<T>
		{
			internal static readonly Func<T, T, T> ModuloDelegate;
			internal static readonly Func<T, double, T> TruncateDelegate;

			static OtherOp()
			{
				DynamicMethod methodMod = new("Modulo", typeof(T), new[] { typeof(T), typeof(T) });
				ILGenerator IL = methodMod.GetILGenerator();
				IL.Emit(OpCodes.Ldarg_0);
				IL.Emit(OpCodes.Ldarg_1);
				switch (Type.GetTypeCode(typeof(T)))
				{
					case TypeCode.Byte:
					case TypeCode.UInt16:
					case TypeCode.Char:
					case TypeCode.UInt32:
					case TypeCode.UInt64:
						IL.Emit(OpCodes.Rem_Un);
						break;
					case TypeCode.SByte:
					case TypeCode.Int16:
					case TypeCode.Int32:
					case TypeCode.Int64:
						IL.Emit(OpCodes.Rem);
						break;
					default:
						break;
				}
				IL.Emit(OpCodes.Ret);
				ModuloDelegate = methodMod.CreateDelegate<Func<T, T, T>>();

				DynamicMethod methodTruncate = new("Truncate", typeof(T), new[] { typeof(T), typeof(T) });
				IL = methodMod.GetILGenerator();
				Label l = IL.DefineLabel();
				IL.DeclareLocal(typeof(T));
				IL.Emit(OpCodes.Ldarg_0);
				if (NumberType<T>.IsComplex)
				{
					var method = typeof(T).GetProperty(nameof(Complex<float>.Magnitude), System.Reflection.BindingFlags.Public)?.GetGetMethod();
					if (method is null)
						throw new MethodAccessException();
					IL.Emit(OpCodes.Call, method);
				}
				IL.Emit(OpCodes.Conv_R8);
				IL.Emit(OpCodes.Ldarg_1);
				IL.Emit(OpCodes.Bgt_S, l);
				IL.Emit(OpCodes.Ldloca_S, 0);
				IL.Emit(OpCodes.Initobj, typeof(T));
				IL.Emit(OpCodes.Ldloc_0);
				IL.Emit(OpCodes.Ret);
				IL.MarkLabel(l);
				IL.Emit(OpCodes.Ldarg_0);
				IL.Emit(OpCodes.Ret);
				TruncateDelegate = methodTruncate.CreateDelegate<Func<T, double, T>>();
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void VectorModifyFloatManaged<T, U, Op>(T* x, int incx, int length, U scalar) where T : unmanaged, IFloatingPoint<T> where U : unmanaged, IFloatingPoint<U>
		{
			Modify op;
			if (typeof(Op) == typeof(U_PowerT))
				op = Modify.PowerT;
			else if (typeof(Op) == typeof(U_PowerDouble))
				op = Modify.PowerDouble;
			else if (typeof(Op) == typeof(U_Sqrt))
				op = Modify.Sqrt;
			else
				op = Modify.Conjugate;
			
			[MethodImpl(MethodImplOptions.AggressiveOptimization)]
			static T Conj(T x)
			{
				U r = *(U*)&x, i = *((U*)&x + 1);
				*((U*)&x + 1) = -i;
				return x;
			}

			// JIT shall in-line / eliminate all switches and type conditions as if they do not exist
			T scalarT = scalar.As<U, T>();
			for (int i = 0, ix = 0; i < length; i++, ix += incx)
			{
				x[ix] = op switch
				{
					Modify.PowerT or Modify.PowerDouble => T.Pow(x[ix], scalarT),
					Modify.Conjugate => Conj(x[ix]),
					Modify.Sqrt => T.Sqrt(x[ix]),
					_ => default,
				};
			}
		}

		private delegate void VectorModifyFloatDelegate<T, U>(T* x, int length, U scalar) where T : unmanaged, INumber<T> where U : unmanaged, INumber<U>;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void VectorModifyManaged<T, U, Op>(T* x, int incx, int length, U scalar) where T : unmanaged, INumber<T> where U : unmanaged, INumber<U>
		{
			Modify op;
			if (typeof(Op) == typeof(U_AddScalar))
				op = Modify.AddScalar;
			else if (typeof(Op) == typeof(U_MultiplyScalar))
				op = Modify.MultiplyScalar;
			else if (typeof(Op) == typeof(U_Modulo))
				op = Modify.Modulo;
			else if (typeof(Op) == typeof(U_PowerT))
				op = Modify.PowerT;
			else if (typeof(Op) == typeof(U_PowerDouble))
				op = Modify.PowerDouble;
			else if (typeof(Op) == typeof(U_Truncate))
				op = Modify.Truncate;
			else if (typeof(Op) == typeof(U_Sqrt))
				op = Modify.Sqrt;
			else if (typeof(Op) == typeof(U_Square))
				op = Modify.Square;
			else if (typeof(Op) == typeof(U_Reciprocal))
				op = Modify.Reciprocal;
			else
				op = Modify.Conjugate;
			if (op == Modify.PowerT || op == Modify.PowerDouble || op == Modify.Sqrt)
			{
				var func = typeof(Api).GetMethod(nameof(VectorModifyFloatManaged), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)?.MakeGenericMethod(new[] { typeof(T), typeof(U), typeof(Op) })?.CreateDelegate<VectorModifyFloatDelegate<T, U>>();
				if (func is null)
					throw new MethodAccessException();
				func(x, length, scalar);
				return;
			}
			Func<T, T, T> mod = OtherOp<T>.ModuloDelegate;
			Func<T, double, T> trunc = OtherOp<T>.TruncateDelegate;

			// JIT shall in-line / eliminate all switches and type conditions as if they do not exist
			T scalarT = typeof(T) == typeof(U) ? *(T*)&scalar : default;
			for (int i = 0, ix = 0; i < length; i++, ix += incx)
			{
				if (op == Modify.Modulo)
				{
					T a = x[ix];
					if (typeof(T) == typeof(uint))
					{ uint v = (*(uint*)&a) % (*(uint*)&scalar); x[ix] = *(T*)&v; }
					else if (typeof(T) == typeof(ulong))
					{ ulong v = (*(ulong*)&a) % (*(ulong*)&scalar); x[ix] = *(T*)&v; }
					else if (typeof(T) == typeof(int))
					{ int v = (*(int*)&a) % (*(int*)&scalar); x[ix] = *(T*)&v; }
					else if (typeof(T) == typeof(long))
					{ long v = (*(long*)&a) % (*(long*)&scalar); x[ix] = *(T*)&v; }
					else
						x[ix] = mod(a, scalarT);
				}
				else if (op == Modify.Truncate)
				{
					T a = x[ix];
					double scalarD = *(double*)&scalar, scalarDS = scalarD * scalarD;
					if (typeof(T) == typeof(uint))
					{ uint v = (*(uint*)&a) > scalarD ? (*(uint*)&a) : 0; x[ix] = *(T*)&v; }
					else if (typeof(T) == typeof(ulong))
					{ ulong v = (*(ulong*)&a) > scalarD ? (*(ulong*)&a) : 0; x[ix] = *(T*)&v; }
					else if (typeof(T) == typeof(double))
					{ int v = (*(int*)&a) > scalarD ? (*(int*)&a) : 0; x[ix] = *(T*)&v; }
					else if (typeof(T) == typeof(long))
					{ long v = (*(long*)&a) > scalarD ? (*(long*)&a) : 0; x[ix] = *(T*)&v; }
					else if (typeof(T) == typeof(float))
					{ float v = (*(float*)&a) > scalarD ? (*(float*)&a) : 0; x[ix] = *(T*)&v; }
					else if (typeof(T) == typeof(double))
					{ double v = (*(double*)&a) > scalarD ? (*(double*)&a) : 0; x[ix] = *(T*)&v; }
					else if (typeof(T) == typeof(Complex<float>) || typeof(T) == typeof(Complex<float>))
					{ Complex<float> v = (*(Complex<float>*)&a).MagnitudeSquared > scalarDS ? (*(Complex<float>*)&a) : 0; x[ix] = *(T*)&v; }
					else if (typeof(T) == typeof(Complex<double>) || typeof(T) == typeof(Complex<double>))
					{ Complex<double> v = (*(Complex<double>*)&a).MagnitudeSquared > scalarDS ? (*(Complex<double>*)&a) : 0; x[ix] = *(T*)&v; }
					else
						x[ix] = trunc(a, scalarD);
				}
				else
				{
					x[ix] = op switch
					{
						Modify.AddScalar => x[ix] + scalarT,
						Modify.MultiplyScalar => x[ix] * scalarT,
						Modify.Conjugate => x[ix].Conjugate(),
						Modify.Square => x[ix] * x[ix],
						Modify.Reciprocal => T.One / x[ix],
						_ => default,
					};
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void VectorModifyReal<T, U, Op>(T* x, int length, U scalar) where T : unmanaged, INumber<T> where U : unmanaged, INumber<U>
		{
			T scalarT = scalar.As<U, T>();
			Modify op;
			if (typeof(Op) == typeof(U_AddScalar))
				op = Modify.AddScalar;
			else if (typeof(Op) == typeof(U_MultiplyScalar))
				op = Modify.MultiplyScalar;
			else if (typeof(Op) == typeof(U_Modulo))
				op = Modify.Modulo;
			else if (typeof(Op) == typeof(U_Truncate))
				op = Modify.Truncate;
			else if (typeof(Op) == typeof(U_Sqrt))
				op = Modify.Sqrt;
			else if (typeof(Op) == typeof(U_Square))
				op = Modify.Square;
			else if (typeof(Op) == typeof(U_Reciprocal))
				op = Modify.Reciprocal;
			else // not possible here
				op = Modify.Conjugate;

			// loop
			Vector<T> scalars = new(scalarT), ones = Vector<T>.One;
			int lengthLeft = length, offset = 0;
			while (lengthLeft >= Vector<T>.Count)
			{
				Vector<T> current = LoadVector(x + offset);
				switch (op)
				{
					case Modify.AddScalar:
						current += new Vector<T>(scalarT);
						break;
					case Modify.MultiplyScalar:
						current *= scalarT;
						break;
					case Modify.Modulo:
						var temp = current / scalars;
						temp *= scalarT;
						current -= temp;
						break;
					case Modify.Sqrt:
						current = Vector.SquareRoot(current);
						break;
					case Modify.Square:
						current *= current;
						break;
					case Modify.Reciprocal:
						current = ones / current;
						break;
					case Modify.Truncate:
						var abs = Vector.Abs(current);
						var compare = Vector.GreaterThan(abs, new Vector<T>(scalarT));
						current = Vector.ConditionalSelect(compare, current, Vector<T>.Zero);
						break;
					default:
						break;
				}
				StoreVector(current, x + offset);
				lengthLeft -= Vector<T>.Count;
				offset += Vector<T>.Count;
			}
			// modify left
			if (lengthLeft > 0)
			{
				VectorModifyManaged<T, U, Op>(x + offset, 1, lengthLeft, scalar);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void VectorModifyCompex<U, Op>(Complex<float>* x, int length, U scalar) where U : unmanaged, INumber<U>
		{
			Complex<float> scalarT = scalar.As<U, Complex<float>>();
			Modify op;
			if (typeof(Op) == typeof(U_AddScalar))
				op = Modify.AddScalar;
			else if (typeof(Op) == typeof(U_MultiplyScalar))
				op = Modify.MultiplyScalar;
			else if (typeof(Op) == typeof(U_Modulo))
				op = Modify.Modulo; // not possible here
			else if (typeof(Op) == typeof(U_Sqrt))
				op = Modify.Sqrt;
			else if (typeof(Op) == typeof(U_Square))
				op = Modify.Square;
			else if (typeof(Op) == typeof(U_Truncate))
				op = Modify.Truncate;
			else if (typeof(Op) == typeof(U_Reciprocal))
				op = Modify.Reciprocal;
			else
				op = Modify.Conjugate;

			// shortcut
			if (op == Modify.AddScalar && scalarT.Imaginary == 0)
			{
				VectorModifyReal<float, float, Op>((float*)x, length * 2, scalarT.Real);
				return;
			}
			// normal
			int lengthLeft = length, offset = 0;
			if (op != Modify.Truncate)
			{
				Vector256<float> scalars = default;
				Span<Complex<float>> _temp = new(&scalars, Vector256<float>.Count / 2);
				Vector256<float> oneMinusOnes = default;
				Span<float> _temp2 = new(&oneMinusOnes, Vector256<float>.Count);
				for (int i = 0; i < Vector256<float>.Count; i += 2)
				{
					_temp2[i] = 1; _temp2[i + 1] = -1;
				}
				_temp.Fill(scalarT);
				// loop
				while (lengthLeft >= Vector256<float>.Count / 2) // Vector256<Complex<float>>.Count
				{
					Vector256<float> current = LoadVector256<float>(x + offset);
					current = op switch
					{
						Modify.AddScalar => Avx.Add(current, scalars),
						Modify.MultiplyScalar => Avx.Multiply(current, scalars),
						Modify.Sqrt => Avx.Sqrt(current),
						Modify.Square => Avx.Multiply(current, current),
						Modify.Conjugate => Avx.Multiply(current, oneMinusOnes),
						Modify.Reciprocal => Avx.Reciprocal(current),
						_ => current,
					};
					StoreVector256(current, x + offset);
					lengthLeft -= Vector256<float>.Count;
					offset += Vector256<float>.Count;
				}
			}
			else
			{
				Vector256<float> zeros = Vector<float>.Zero.AsVector256();
				Vector256<float> scalarSquares = default;
				Span<float> _temp = new(&scalarSquares, Vector256<float>.Count / 2);
				_temp.Fill((*(float*)&scalar) * (*(float*)&scalar));
				// loop
				while (lengthLeft >= Vector256<float>.Count) // Vector256<Complex<float>>.Count * 2
				{
					// {a[0].r, a[0].i, ..., a[3].i}
					Vector256<float> current1 = LoadVector256<float>(x + offset);
					// {a[4].r, a[4].i, ..., a[7].i}
					Vector256<float> current2 = LoadVector256<float>(x + offset + Vector256<float>.Count / 2);
					// abs(a[{0, 1, 4, 5, 2, 3, 6, 7}])
					Vector256<float> currentAbs = ComplexSquareAbsNoOrder(current1, current2);
					// abs(a[{0, 1, 4, 5, 2, 3, 6, 7}]) > threshold
					Vector256<float> compare = Avx.CompareNotGreaterThan(currentAbs, scalarSquares);
					// has "Not" since AVX compare is reversed

					// abs(a[{0, 0, 1, 1, 2, 2, 3, 3}]) > threshold
					Vector256<float> compare1 = Avx.UnpackLow(compare, compare);
					// abs(a[{4, 4, 5, 5, 6, 6, 7, 7}]) > threshold
					Vector256<float> compare2 = Avx.UnpackHigh(compare, compare);
					current1 = Avx.BlendVariable(current1, zeros, compare1);
					current2 = Avx.BlendVariable(current2, zeros, compare2);

					StoreVector256(current1, x + offset);
					StoreVector256(current1, x + offset + Vector256<float>.Count / 2);
					lengthLeft -= Vector256<float>.Count;
					offset += Vector256<float>.Count;
				}
			}
			// modify left
			if (lengthLeft > 0)
			{
				VectorModifyManaged<Complex<float>, U, Op>(x, 1, length, scalar);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void VectorModifyCompex<U, Op>(Complex<double>* x, int length, U scalar) where U : unmanaged, INumber<U>
		{
			Complex<double> scalarT = scalar.As<U, Complex<double>>();
			Modify op;
			if (typeof(Op) == typeof(U_AddScalar))
				op = Modify.AddScalar;
			else if (typeof(Op) == typeof(U_MultiplyScalar))
				op = Modify.MultiplyScalar;
			else if (typeof(Op) == typeof(U_Modulo))
				op = Modify.Modulo; // not possible here
			else if (typeof(Op) == typeof(U_Sqrt))
				op = Modify.Sqrt;
			else if (typeof(Op) == typeof(U_Square))
				op = Modify.Square;
			else if (typeof(Op) == typeof(U_Truncate))
				op = Modify.Truncate;
			else if (typeof(Op) == typeof(U_Reciprocal))
				op = Modify.Reciprocal;
			else
				op = Modify.Conjugate;

			// shortcut
			if (op == Modify.AddScalar && scalarT.Imaginary == 0)
			{
				VectorModifyReal<double, double, Op>((double*)x, length * 2, scalarT.Real);
				return;
			}
			// normal
			int lengthLeft = length, offset = 0;
			if (op != Modify.Truncate)
			{
				Vector256<double> scalars = default;
				Span<Complex<double>> _temp = new(&scalars, Vector256<double>.Count / 2);
				Vector256<double> oneMinusOnes = default;
				Span<double> _temp2 = new(&oneMinusOnes, Vector256<double>.Count);
				for (int i = 0; i < Vector256<double>.Count; i += 2)
				{
					_temp2[i] = 1; _temp2[i + 1] = -1;
				}
				_temp.Fill(scalarT);
				Vector256<double> ones = Vector<double>.One.AsVector256();
				// loop
				while (lengthLeft >= Vector256<double>.Count / 2) // Vector256<Complex<double>>.Count
				{
					Vector256<double> current = LoadVector256<double>(x + offset);
					current = op switch
					{
						Modify.AddScalar => Avx.Add(current, scalars),
						Modify.MultiplyScalar => Avx.Multiply(current, scalars),
						Modify.Sqrt => Avx.Sqrt(current),
						Modify.Square => Avx.Multiply(current, current),
						Modify.Conjugate => Avx.Multiply(current, oneMinusOnes),
						Modify.Reciprocal => Avx.Divide(ones, current),
						_ => current,
					};
					StoreVector256(current, x + offset);
					lengthLeft -= Vector256<double>.Count;
					offset += Vector256<double>.Count;
				}
			}
			else
			{
				Vector256<double> zeros = Vector<double>.Zero.AsVector256();
				Vector256<double> scalarSquares = default;
				Span<double> _temp = new(&scalarSquares, Vector256<double>.Count / 2);
				_temp.Fill((*(double*)&scalar) * (*(double*)&scalar));
				// loop
				while (lengthLeft >= Vector256<double>.Count) // Vector256<Complex<double>>.Count * 2
				{
					// {a[0].r, a[0].i, ..., a[1].i}
					Vector256<double> current1 = LoadVector256<double>(x + offset);
					// {a[2].r, a[2].i, ..., a[3].i}
					Vector256<double> current2 = LoadVector256<double>(x + offset + Vector256<double>.Count / 2);
					// abs(a[{0, 2, 1, 3}])
					Vector256<double> currentAbs = ComplexSquareAbsNoOrder(current1, current2);
					// abs(a[{0, 2, 1, 3}]) > threshold
					Vector256<double> compare = Avx.CompareNotGreaterThan(currentAbs, scalarSquares);
					// has "Not" since AVX compare is reversed
					// {0, 0, 1, 1}
					Vector256<double> compare1 = Avx.UnpackLow(compare, compare);
					// {2, 2, 3, 3}
					Vector256<double> compare2 = Avx.UnpackHigh(compare, compare);
					current1 = Avx.BlendVariable(current1, zeros, compare1);
					current2 = Avx.BlendVariable(current1, zeros, compare2);
					StoreVector256(current1, x + offset);
					StoreVector256(current1, x + offset + Vector256<double>.Count / 2);
					lengthLeft -= Vector256<double>.Count;
					offset += Vector256<double>.Count;
				}
			}
			// modify left
			if (lengthLeft > 0)
			{
				VectorModifyManaged<Complex<double>, U, Op>(x, 1, length, scalar);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool VectorModify<T, U, Op>(T* px, int incx, int length, U scalar) where T : unmanaged, INumber<T> where U : unmanaged, INumber<U>
		{
			if (incx != 1 || !Vector.IsHardwareAccelerated || length <= (Vector<byte>.Count / sizeof(T) * 4))
			{   // no SIMD or too short
				VectorModifyManaged<T, U, Op>(px, incx, length, scalar);
				return true;
			}

			if (NumberType<T>.IsComplex)
			{
				if (Unmanaged<T>.DataType.IsInteger() || !Avx.IsSupported)
				{   // no AVX's HorizontalAdd and Unpack (Vector<T> has not corresponding implementation yet)
					VectorModifyManaged<T, U, Op>(px, 1, length, scalar);
				}
				else if (typeof(T) == typeof(Complex<float>) || typeof(T) == typeof(Complex<float>))
				{
					VectorModifyCompex<U, Op>((Complex<float>*)px, length, scalar);
				}
				else // double
				{
					VectorModifyCompex<U, Op>((Complex<double>*)px, length, scalar);
				}
			}
			else
			{
				VectorModifyReal<T, U, Op>(px, length, scalar);
			}
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool VectorModify<T, TS, U, Op>(TS x, long strideX, U scalar) where T : unmanaged, INumber<T> where U : unmanaged, INumber<U> where TS : class, IStorage<T, TS>
		{
			if (!GetPointer(x, strideX, out T* px, out int length, out int inc))
				return false;
			if (length == 0)
				return true;
			return VectorModify<T, U, Op>(px, inc, length, scalar);
		}
		
		public virtual partial bool Scale<T, TS>(TS x, long strideX, T scalar) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS> => VectorModify<T, TS, T, U_MultiplyScalar>(x, strideX, scalar);

		public virtual partial bool GeneralVectorUnary<T, TS>(UnaryOperation op, TS x, long strideX) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>
		{
			return op switch
			{
				UnaryOperation.Identity => true,
				UnaryOperation.Conjugate => !NumberType<T>.IsComplex || VectorModify<T, TS, T, U_Conjugate>(x, strideX, default),
				UnaryOperation.Negate => VectorModify<T, TS, T, U_MultiplyScalar>(x, strideX, -T.One),
				UnaryOperation.AbsoluteValue => false,
				_ => false,
			};
		}

		public virtual partial bool GeneralVectorBinaryScalar<T, TS>(BinaryScalarOperation op, T scalar, TS x, long strideX) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>
		{
			return op switch
			{
				BinaryScalarOperation.Add => VectorModify<T, TS, T, U_AddScalar>(x, strideX, scalar),
				BinaryScalarOperation.Multiply => VectorModify<T, TS, T, U_MultiplyScalar>(x, strideX, scalar),
				BinaryScalarOperation.Power => PointWisePower(x, strideX, scalar),
				BinaryScalarOperation.Maximum => false,
				BinaryScalarOperation.Mininum => false,
				BinaryScalarOperation.Fill => FillWithValue(x, strideX, scalar),
				BinaryScalarOperation.Truncate => VectorModify<T, TS, double, U_Truncate>(x, strideX, scalar.As<T, double>()),
				_ => false,
			};
		}

		private static bool FillWithValue<T, TS>(TS x, long strideX, T value) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>
		{
			if (!GetPointer(x, strideX, out T* px, out int length, out int inc) || x is not PureStorage<T, CpuMemoryPointer> ps)
				return false;
			if (inc == 1)
				return Storage.Api.Default.FillWithValue(ps.Pointer, value);
			for (int i = 0, ix = 0; i < length; i++, ix += inc)
			{
				px[ix] = value;
			}
			return true;
		}

		private static bool PointWisePower<T, TS>(TS x, long stride, T p) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>
		{
			if (p == T.Zero)
				return FillWithValue(x, stride, T.One);
			if (p == T.One)
				return true;
			if (p == -T.One)
				return VectorModify<T, TS, T, U_Reciprocal>(x, stride, p);
			if (p == (T.One + T.One))
				return VectorModify<T, TS, T, U_Square>(x, stride, p);
			if (p == T.One / (T.One + T.One))
				return VectorModify<T, TS, T, U_Sqrt>(x, stride, p);
			if (p.Conjugate() == p)
				return VectorModify<T, TS, double, U_PowerDouble>(x, stride, p.As<T, double>());
			return VectorModify<T, TS, T, U_PowerT>(x, stride, p);
		}

		/// <summary>
		/// Compute <c><paramref name="x"/> = <paramref name="x"/> .% <paramref name="mod"/></c> (point-wise modulo).
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS">The actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="x">The vector to be powered in-place</param>
		/// <param name="stride">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="mod">The mod as a <typeparamref name="T"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="stride"/> ≤ 0</exception>
		public virtual bool PointWiseModulo<T, TS>(TS x, long stride, T mod) where T : unmanaged, IBinaryInteger<T> where TS : class, IStorage<T, TS> => VectorModify<T, TS, T, U_Modulo>(x, stride, mod);
	}
}
