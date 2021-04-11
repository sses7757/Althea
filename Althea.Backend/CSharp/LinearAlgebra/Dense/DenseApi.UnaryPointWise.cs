using System;
using System.Numerics;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

using Althea.Helpers;
using Althea.LinearAlgebra.Dense;
using Althea.NativeTypes;


namespace Althea.Backend.CSharp.LinearAlgebra
{
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释
	public partial class DenseApi : AbstractApi
	{
		private struct U_AddScalar { }
		private struct U_MultiplyScalar { }
		private struct U_Modulo { }
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

		private static class OtherOp<T> where T : unmanaged
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
				if (Const<T>.IsComplex)
				{
					var method = typeof(T).GetMethod(nameof(Complex<float>.Abs), System.Reflection.BindingFlags.Public);
					if (method is null)
						throw new MethodAccessException();
					IL.Emit(OpCodes.Call, method);
				}
				IL.Emit(OpCodes.Conv_R8);
				IL.Emit(OpCodes.Ldarg_1);
				IL.Emit(OpCodes.Bgt_S, l);
				IL.Emit(OpCodes.Ldloca_S, (byte)0);
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
		private static unsafe void VectorModifyManaged<T, U, Op>(T* x, int length, U scalar) where T : unmanaged where U : unmanaged
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
			Func<T, T, T> mod = OtherOp<T>.ModuloDelegate;
			Func<T, double, T> trunc = OtherOp<T>.TruncateDelegate;

			// JIT shall in-line / eliminate all switches and type conditions as if they do not exist
			for (int i = 0; i < length; i++)
			{
				if (op == Modify.Modulo)
				{
					T a = x[i];
					if (typeof(T) == typeof(uint))
					{ uint v = (*(uint*)&a) % (*(uint*)&scalar); x[i] = *(T*)&v; }
					else if (typeof(T) == typeof(ulong))
					{ ulong v = (*(ulong*)&a) % (*(ulong*)&scalar); x[i] = *(T*)&v; }
					else if (typeof(T) == typeof(int))
					{ int v = (*(int*)&a) % (*(int*)&scalar); x[i] = *(T*)&v; }
					else if (typeof(T) == typeof(long))
					{ long v = (*(long*)&a) % (*(long*)&scalar); x[i] = *(T*)&v; }
					else
						x[i] = mod(a, *(T*)&scalar);
				}
				else if (op == Modify.Truncate)
				{
					T a = x[i];
					double scalarD = *(double*)&scalar, scalarDS = scalarD * scalarD;
					if (typeof(T) == typeof(uint))
					{ uint v = (*(uint*)&a) > scalarD ? (*(uint*)&a) : 0; x[i] = *(T*)&v; }
					else if (typeof(T) == typeof(ulong))
					{ ulong v = (*(ulong*)&a) > scalarD ? (*(ulong*)&a) : 0; x[i] = *(T*)&v; }
					else if (typeof(T) == typeof(double))
					{ int v = (*(int*)&a) > scalarD ? (*(int*)&a) : 0; x[i] = *(T*)&v; }
					else if (typeof(T) == typeof(long))
					{ long v = (*(long*)&a) > scalarD ? (*(long*)&a) : 0; x[i] = *(T*)&v; }
					else if (typeof(T) == typeof(float))
					{ float v = (*(float*)&a) > scalarD ? (*(float*)&a) : 0; x[i] = *(T*)&v; }
					else if (typeof(T) == typeof(double))
					{ double v = (*(double*)&a) > scalarD ? (*(double*)&a) : 0; x[i] = *(T*)&v; }
					else if (typeof(T) == typeof(Complex<float>) || typeof(T) == typeof(ComplexSingle))
					{ ComplexSingle v = (*(ComplexSingle*)&a).SquareAbs() > scalarDS ? (*(ComplexSingle*)&a) : 0; x[i] = *(T*)&v; }
					else if (typeof(T) == typeof(Complex<double>) || typeof(T) == typeof(ComplexDouble))
					{ ComplexDouble v = (*(ComplexDouble*)&a).SquareAbs() > scalarDS ? (*(ComplexDouble*)&a) : 0; x[i] = *(T*)&v; }
					else
						x[i] = trunc(a, *(double*)&scalar);
				}
				else
				{
					x[i] = op switch
					{
						Modify.AddScalar => x[i].NativeAdd(*(T*)&scalar),
						Modify.MultiplyScalar => x[i].NativeMultiply(*(T*)&scalar),
						Modify.PowerT => x[i].NativePower(*(T*)&scalar),
						Modify.PowerDouble => x[i].NativePower(*(double*)&scalar),
						Modify.Conjugate => x[i].NativeConjugate(),
						Modify.Sqrt => x[i].NativeSqrt(),
						Modify.Square => x[i].NativeMultiply(x[i]),
						Modify.Reciprocal => x[i].NativeReciprocal(),
						_ => default,
					};
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe void VectorModifyReal<T, U, Op>(T* x, int length, U scalar) where T : unmanaged where U : unmanaged
		{
			T scalarT = scalar.NativeConvert<U, T>();
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
				VectorModifyManaged<T, U, U_PowerDouble>(x + offset, lengthLeft, scalar);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe void VectorModifyCompexSingle<U, Op>(ComplexSingle* x, int length, U scalar) where U : unmanaged
		{
			ComplexSingle scalarT = scalar.NativeConvert<U, ComplexSingle>();
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
			if (op == Modify.AddScalar && scalarT.Imag == 0)
			{
				VectorModifyReal<float, float, Op>((float*)x, length * 2, scalarT.Real);
				return;
			}
			// normal
			int lengthLeft = length, offset = 0;
			if (op != Modify.Truncate)
			{
				Vector256<float> scalars = default;
				Span<ComplexSingle> _temp = new(&scalars, Vector256<float>.Count / 2);
				Vector256<float> oneMinusOnes = default;
				Span<float> _temp2 = new(&oneMinusOnes, Vector256<float>.Count);
				for (int i = 0; i < Vector256<float>.Count; i += 2)
				{
					_temp2[i] = 1; _temp2[i + 1] = -1;
				}
				_temp.Fill(scalarT);
				// loop
				while (lengthLeft >= Vector256<float>.Count / 2) // Vector256<ComplexSingle>.Count
				{
					Vector256<float> current = LoadVector256<float>(x + offset);
					switch (op)
					{
						case Modify.AddScalar:
							current = Avx.Add(current, scalars);
							break;
						case Modify.MultiplyScalar:
							current = Avx.Multiply(current, scalars);
							break;
						case Modify.Sqrt:
							current = Avx.Sqrt(current);
							break;
						case Modify.Square:
							current = Avx.Multiply(current, current);
							break;
						case Modify.Conjugate:
							current = Avx.Multiply(current, oneMinusOnes);
							break;
						case Modify.Reciprocal:
							current = Avx.Reciprocal(current);
							break;
						default:
							break;
					}
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
				while (lengthLeft >= Vector256<float>.Count) // Vector256<ComplexSingle>.Count * 2
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
				VectorModifyManaged<ComplexSingle, U, Op>(x, length, scalar);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe void VectorModifyCompexDouble<U, Op>(ComplexDouble* x, int length, U scalar) where U : unmanaged
		{
			ComplexDouble scalarT = scalar.NativeConvert<U, ComplexDouble>();
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
			if (op == Modify.AddScalar && scalarT.Imag == 0)
			{
				VectorModifyReal<double, double, Op>((double*)x, length * 2, scalarT.Real);
				return;
			}
			// normal
			int lengthLeft = length, offset = 0;
			if (op != Modify.Truncate)
			{
				Vector256<double> scalars = default;
				Span<ComplexDouble> _temp = new(&scalars, Vector256<double>.Count / 2);
				Vector256<double> oneMinusOnes = default;
				Span<double> _temp2 = new(&oneMinusOnes, Vector256<double>.Count);
				for (int i = 0; i < Vector256<double>.Count; i += 2)
				{
					_temp2[i] = 1; _temp2[i + 1] = -1;
				}
				_temp.Fill(scalarT);
				Vector256<double> ones = Vector<double>.One.AsVector256();
				// loop
				while (lengthLeft >= Vector256<double>.Count / 2) // Vector256<ComplexDouble>.Count
				{
					Vector256<double> current = LoadVector256<double>(x + offset);
					switch (op)
					{
						case Modify.AddScalar:
							current = Avx.Add(current, scalars);
							break;
						case Modify.MultiplyScalar:
							current = Avx.Multiply(current, scalars);
							break;
						case Modify.Sqrt:
							current = Avx.Sqrt(current);
							break;
						case Modify.Square:
							current = Avx.Multiply(current, current);
							break;
						case Modify.Conjugate:
							current = Avx.Multiply(current, oneMinusOnes);
							break;
						case Modify.Reciprocal:
							current = Avx.Divide(ones, current);
							break;
						default:
							break;
					}
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
				while (lengthLeft >= Vector256<double>.Count) // Vector256<ComplexDouble>.Count * 2
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
				VectorModifyManaged<ComplexDouble, U, Op>(x, length, scalar);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static unsafe bool VectorModify<T, U, Op>(Storage<T> x, U scalar) where T : unmanaged where U : unmanaged
		{
			if (!GetPointer(x, out T* px, out int length))
				return false;
			if (length == 0)
			{
				return true;
			}
			if (!Vector.IsHardwareAccelerated || length <= (Vector<byte>.Count / sizeof(T) * 4))
			{	// no SIMD or too short
				VectorModifyManaged<T, U, Op>(px, length, scalar);
				return true;
			}

			if (Const<T>.IsComplex)
			{
				if (Const<T>.IsIntegralType || !Avx.IsSupported)
				{	// no AVX's HorizontalAdd and Unpack (Vector<T> has not corresponding implementation yet)
					VectorModifyManaged<T, U, Op>(px, length, scalar);
				}
				else if (typeof(T) == typeof(float))
				{
					VectorModifyCompexSingle<U, Op>((ComplexSingle*)px, length, scalar);
				}
				else // double
				{
					VectorModifyCompexDouble<U, Op>((ComplexDouble*)px, length, scalar);
				}
			}
			else
			{
				VectorModifyReal<T, U, Op>(px, length, scalar);
			}
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static bool PointWiseAddScalar<T>(Storage<T> x, T scalr) where T : unmanaged
		{
			return VectorModify<T, T, U_AddScalar>(x, scalr);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static bool PointWiseModulo<T>(Storage<T> x, T mod) where T : unmanaged
		{
			if (Const<T>.IsComplex || !Const<T>.IsIntegralType)
				throw new TypeMismatchException(typeof(T), TypeMismatchException.MismatchReason.NotInteger);
			return VectorModify<T, T, U_Modulo>(x, mod);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static bool PointWiseConjugate<T>(Storage<T> x) where T : unmanaged
		{
			if (Const<T>.IsComplex)
				return VectorModify<T, T, U_Conjugate>(x, default);
			else
				return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static bool PointWisePower<T>(Storage<T> x, double p) where T : unmanaged
		{
			if (p == 0)
			{
				try
				{
					Althea.Storage.AbstractApi.FillWithValue(x, Const<T>.One);
					return true;
				}
				catch (Exception)
				{
					return false;
				}
			}
			else if (p == 0.5)
				return VectorModify<T, double, U_Sqrt>(x, p);
			else if (p == 2)
				return VectorModify<T, double, U_Square>(x, p);
			else if (p == 1)
				return true;
			else if (p == -1)
				return VectorModify<T, double, U_Reciprocal>(x, p);
			else
				return VectorModify<T, double, U_PowerDouble>(x, p);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static bool PointWisePower<T>(Storage<T> x, T p) where T : unmanaged
		{
			if (p.IsZero())
			{
				try
				{
					Althea.Storage.AbstractApi.FillWithValue(x, Const<T>.One);
					return true;
				}
				catch (Exception)
				{
					return false;
				}
			}
			else if (!Const<T>.IsIntegralType && p.IsEqual(Const<T>.Half))
				return VectorModify<T, T, U_Sqrt>(x, p);
			else if (p.IsEqual(Const<T>.Two))
				return VectorModify<T, T, U_Square>(x, p);
			else if (p.IsOne())
				return true;
			else if (p.IsEqual(Const<T>.MinusOne))
				return VectorModify<T, T, U_Reciprocal>(x, p);
			else
				return VectorModify<T, T, U_PowerT>(x, p);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static bool Scale<T>(Storage<T> x, T scalar) where T : unmanaged
		{
			return VectorModify<T, T, U_MultiplyScalar>(x, scalar);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal new static bool TruncateArray<T>(Storage<T> x, double threshold) where T : unmanaged
		{
			return VectorModify<T, double, U_Truncate>(x, threshold);
		}
	}
#pragma warning restore CS1591 // 缺少对公共可见类型或成员的 XML 注释
}
