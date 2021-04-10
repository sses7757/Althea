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
		private enum AddScalar { }
		private enum MultiplyScalar { }
		private enum Modulo { }
		private enum PowerT { }
		private enum PowerDouble { }
		private enum Truncate { }
		private enum Conjugate { }
		private enum Sqrt { }
		private enum Square { }
		private enum Reciprocal { }
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
			if (typeof(Op) == typeof(AddScalar))
				op = Modify.AddScalar;
			else if (typeof(Op) == typeof(MultiplyScalar))
				op = Modify.MultiplyScalar;
			else if (typeof(Op) == typeof(Modulo))
				op = Modify.Modulo;
			else if (typeof(Op) == typeof(PowerT))
				op = Modify.PowerT;
			else if (typeof(Op) == typeof(PowerDouble))
				op = Modify.PowerDouble;
			else if (typeof(Op) == typeof(Truncate))
				op = Modify.Truncate;
			else if (typeof(Op) == typeof(Sqrt))
				op = Modify.Sqrt;
			else if (typeof(Op) == typeof(Square))
				op = Modify.Square;
			else if (typeof(Op) == typeof(Reciprocal))
				op = Modify.Reciprocal;
			else
				op = Modify.Conjugate;
#pragma warning disable CS8600, CS8602
			Func<T, T, T> opT = op switch
			{
				Modify.AddScalar => BinaryArithmeticOperation.Addition.GetArithmeticOperation<T>(),
				Modify.MultiplyScalar => BinaryArithmeticOperation.Multiply.GetArithmeticOperation<T>(),
				Modify.Modulo => OtherOp<T>.ModuloDelegate,
				Modify.PowerT => BinaryArithmeticOperation.Exponentiation.GetArithmeticOperation<T>(),
				_ => null,
			};
			Func<T, double, T> opD = op switch
			{
				Modify.PowerDouble => ConstExtension.GetRealPowerOperation<T>(),
				Modify.Truncate => OtherOp<T>.TruncateDelegate,
				_ => null,
			};
			Func<T, T>? uOp = op switch
			{
				Modify.Sqrt => UnaryArithmeticOperation.SquareRoot.GetArithmeticOperation<T>(),
				Modify.Square => static v => BinaryArithmeticOperation.Multiply.GetArithmeticOperation<T>().Invoke(v, v),
				Modify.Reciprocal => UnaryArithmeticOperation.Reciprocal.GetArithmeticOperation<T>(),
				Modify.Conjugate => UnaryArithmeticOperation.Conjugate.GetArithmeticOperation<T>(),
				_ => null,
			};

			// JIT shall in-line / eliminate all switches and type conditions as if they do not exist
			for (int i = 0; i < length; i++)
			{
				T a = x[i];
				if (typeof(T) == typeof(uint))
				{
					uint v = op switch
					{
						Modify.AddScalar => (*(uint*)&a) + (*(uint*)&scalar),
						Modify.MultiplyScalar => (*(uint*)&a) * (*(uint*)&scalar),
						Modify.Modulo => (*(uint*)&a) % (*(uint*)&scalar),
						Modify.PowerT => (uint)Math.Pow(*(uint*)&a, *(uint*)&scalar),
						Modify.PowerDouble => (uint)Math.Pow(*(uint*)&a, *(double*)&scalar),
						Modify.Truncate => (*(uint*)&a) > (*(double*)&scalar) ? (*(uint*)&a) : 0,
						Modify.Sqrt => (uint)Math.Sqrt(*(uint*)&a),
						Modify.Square => (*(uint*)&a) * (*(uint*)&a),
						Modify.Reciprocal => 1 / (*(uint*)&a),
						_ => default,
					};
					x[i] = *(T*)&v;
				}
				if (typeof(T) == typeof(ulong))
				{
					ulong v = op switch
					{
						Modify.AddScalar => (*(ulong*)&a) + (*(ulong*)&scalar),
						Modify.MultiplyScalar => (*(ulong*)&a) * (*(ulong*)&scalar),
						Modify.Modulo => (*(ulong*)&a) % (*(ulong*)&scalar),
						Modify.PowerT => (ulong)Math.Pow(*(ulong*)&a, *(ulong*)&scalar),
						Modify.PowerDouble => (ulong)Math.Pow(*(ulong*)&a, *(double*)&scalar),
						Modify.Truncate => (*(ulong*)&a) > (*(double*)&scalar) ? (*(ulong*)&a) : 0,
						Modify.Sqrt => (ulong)Math.Sqrt(*(ulong*)&a),
						Modify.Square => (*(ulong*)&a) * (*(ulong*)&a),
						Modify.Reciprocal => 1 / (*(ulong*)&a),
						_ => default,
					};
					x[i] = *(T*)&v;
				}
				if (typeof(T) == typeof(int))
				{
					int v = op switch
					{
						Modify.AddScalar => (*(int*)&a) + (*(int*)&scalar),
						Modify.MultiplyScalar => (*(int*)&a) * (*(int*)&scalar),
						Modify.Modulo => (*(int*)&a) % (*(int*)&scalar),
						Modify.PowerT => (int)Math.Pow(*(int*)&a, *(int*)&scalar),
						Modify.PowerDouble => (int)Math.Pow(*(int*)&a, *(double*)&scalar),
						Modify.Truncate => (*(int*)&a) > (*(double*)&scalar) ? (*(int*)&a) : 0,
						Modify.Sqrt => (int)Math.Sqrt(*(int*)&a),
						Modify.Square => (*(int*)&a) * (*(int*)&a),
						Modify.Reciprocal => 1 / (*(int*)&a),
						_ => default,
					};
					x[i] = *(T*)&v;
				}
				if (typeof(T) == typeof(long))
				{
					long v = op switch
					{
						Modify.AddScalar => (*(long*)&a) + (*(long*)&scalar),
						Modify.MultiplyScalar => (*(long*)&a) * (*(long*)&scalar),
						Modify.Modulo => (*(long*)&a) % (*(long*)&scalar),
						Modify.PowerT => (long)Math.Pow(*(long*)&a, *(long*)&scalar),
						Modify.PowerDouble => (long)Math.Pow(*(long*)&a, *(double*)&scalar),
						Modify.Truncate => (*(long*)&a) > (*(double*)&scalar) ? (*(long*)&a) : 0,
						Modify.Sqrt => (long)Math.Sqrt(*(long*)&a),
						Modify.Square => (*(long*)&a) * (*(long*)&a),
						Modify.Reciprocal => 1 / (*(long*)&a),
						_ => default,
					};
					x[i] = *(T*)&v;
				}
				if (typeof(T) == typeof(float))
				{
					float v = op switch
					{
						Modify.AddScalar => (*(float*)&a) + (*(float*)&scalar),
						Modify.MultiplyScalar => (*(float*)&a) * (*(float*)&scalar),
						Modify.PowerT => MathF.Pow(*(float*)&a, *(float*)&scalar),
						Modify.PowerDouble => MathF.Pow(*(float*)&a, (float)(*(double*)&scalar)),
						Modify.Truncate => (*(float*)&a) > (*(double*)&scalar) ? (*(float*)&a) : 0,
						Modify.Sqrt => MathF.Sqrt(*(float*)&a),
						Modify.Square => (*(float*)&a) * (*(float*)&a),
						Modify.Reciprocal => 1 / (*(float*)&a),
						_ => default,
					};
					x[i] = *(T*)&v;
				}
				if (typeof(T) == typeof(double))
				{
					double v = op switch
					{
						Modify.AddScalar => (*(double*)&a) + (*(double*)&scalar),
						Modify.MultiplyScalar => (*(double*)&a) * (*(double*)&scalar),
						Modify.PowerT or Modify.PowerDouble => Math.Pow(*(double*)&a, *(double*)&scalar),
						Modify.Truncate => (*(double*)&a) > (*(double*)&scalar) ? (*(double*)&a) : 0,
						Modify.Sqrt => Math.Sqrt(*(double*)&a),
						Modify.Square => (*(double*)&a) * (*(double*)&a),
						Modify.Reciprocal => 1 / (*(double*)&a),
						_ => default,
					};
					x[i] = *(T*)&v;
				}
				if (typeof(T) == typeof(ComplexSingle) || typeof(T) == typeof(Complex<float>))
				{
					ComplexSingle v = op switch
					{
						Modify.AddScalar => (*(ComplexSingle*)&a) + (*(ComplexSingle*)&scalar),
						Modify.MultiplyScalar => (*(ComplexSingle*)&a) * (*(ComplexSingle*)&scalar),
						Modify.PowerT => (*(ComplexSingle*)&a).Pow(*(ComplexSingle*)&scalar),
						Modify.PowerDouble => (*(ComplexSingle*)&a).Pow((float)(*(double*)&scalar)),
						Modify.Truncate => (*(ComplexSingle*)&a).Abs() > (*(double*)&scalar) ? (*(ComplexSingle*)&a) : 0,
						Modify.Conjugate => (*(ComplexSingle*)&a).Conjugate(),
						Modify.Sqrt => (*(ComplexSingle*)&a).Sqrt(),
						Modify.Square => (*(ComplexSingle*)&a) * (*(ComplexSingle*)&a),
						Modify.Reciprocal => 1 / (*(ComplexSingle*)&a),
						_ => default,
					};
					x[i] = *(T*)&v;
				}
				if (typeof(T) == typeof(ComplexDouble) || typeof(T) == typeof(Complex<double>))
				{
					ComplexDouble v = op switch
					{
						Modify.AddScalar => (*(ComplexDouble*)&a) + (*(ComplexDouble*)&scalar),
						Modify.MultiplyScalar => (*(ComplexDouble*)&a) * (*(ComplexDouble*)&scalar),
						Modify.PowerT => (*(ComplexDouble*)&a).Pow(*(ComplexDouble*)&scalar),
						Modify.PowerDouble => (*(ComplexDouble*)&a).Pow(*(double*)&scalar),
						Modify.Truncate => (*(ComplexDouble*)&a).Abs() > (*(double*)&scalar) ? (*(ComplexDouble*)&a) : 0,
						Modify.Conjugate => (*(ComplexDouble*)&a).Conjugate(),
						Modify.Sqrt => (*(ComplexDouble*)&a).Sqrt(),
						Modify.Square => (*(ComplexDouble*)&a) * (*(ComplexDouble*)&a),
						Modify.Reciprocal => 1 / (*(ComplexDouble*)&a),
						_ => default,
					};
					x[i] = *(T*)&v;
				}
				else
				{
					x[i] = op switch
					{
						Modify.AddScalar or Modify.MultiplyScalar or Modify.Modulo or Modify.PowerT => opT(a, *(T*)&scalar),
						Modify.PowerDouble or Modify.Truncate => opD(a, *(double*)&scalar),
						Modify.Conjugate or Modify.Sqrt or Modify.Square or Modify.Reciprocal => uOp(a),
						_ => default,
					};
				}
			}
#pragma warning restore CS8600, CS8602
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe void VectorModifyReal<T, U, Op>(T* x, int length, U scalar) where T : unmanaged where U : unmanaged
		{
			T scalarT = scalar.GenericConvert<U, T>();
			Modify op;
			if (typeof(Op) == typeof(AddScalar))
				op = Modify.AddScalar;
			else if (typeof(Op) == typeof(MultiplyScalar))
				op = Modify.MultiplyScalar;
			else if (typeof(Op) == typeof(Modulo))
				op = Modify.Modulo;
			else if (typeof(Op) == typeof(Truncate))
				op = Modify.Truncate;
			else if (typeof(Op) == typeof(Sqrt))
				op = Modify.Sqrt;
			else if (typeof(Op) == typeof(Square))
				op = Modify.Square;
			else if (typeof(Op) == typeof(Reciprocal))
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
				VectorModifyManaged<T, U, PowerDouble>(x + offset, lengthLeft, scalar);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe void VectorModifyCompexSingle<U, Op>(ComplexSingle* x, int length, U scalar) where U : unmanaged
		{
			ComplexSingle scalarT = scalar.GenericConvert<U, ComplexSingle>();
			Modify op;
			if (typeof(Op) == typeof(AddScalar))
				op = Modify.AddScalar;
			else if (typeof(Op) == typeof(MultiplyScalar))
				op = Modify.MultiplyScalar;
			else if (typeof(Op) == typeof(Modulo))
				op = Modify.Modulo; // not possible here
			else if (typeof(Op) == typeof(Sqrt))
				op = Modify.Sqrt;
			else if (typeof(Op) == typeof(Square))
				op = Modify.Square;
			else if (typeof(Op) == typeof(Truncate))
				op = Modify.Truncate;
			else if (typeof(Op) == typeof(Reciprocal))
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
			{	// TODO: the extra 6 shuffles may lead to performance worse than scalar implementation, test it
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
					// {abs(a[0]), abs(a[1]), ..., abs(a[7])}
					Vector256<float> currentAbs = ComplexSquareAbs(current1, current2);
					// {abs(a[0]) > threshold, abs(a[1]) > threshold, ..., abs(a[7]) > threshold}
					Vector256<float> compare = Avx.CompareNotGreaterThan(currentAbs, scalarSquares);
					// has "Not" since AVX compare is reversed

					// {abs(a[0]) > threshold, abs(a[0]) > threshold, abs(a[2]) > threshold, abs(a[2]) > threshold,...}
					Vector256<float> compare1 = Avx.DuplicateEvenIndexed(compare);
					// {abs(a[1]) > threshold, abs(a[1]) > threshold, abs(a[3]) > threshold, abs(a[3]) > threshold,...}
					Vector256<float> compare2 = Avx.DuplicateOddIndexed(compare);
					// {00, 11, 44, 55}
					Vector256<double> temp1 = Avx.UnpackLow(compare1.AsDouble(), compare2.AsDouble());
					// {22, 33, 66, 77}
					Vector256<double> temp2 = Avx.UnpackHigh(compare1.AsDouble(), compare2.AsDouble());
					// {0, 0, 1, 1, 2, 2, 3, 3}
					compare1 = Avx.Permute2x128(temp1, temp2, 0b00_10_00_00).AsSingle();
					// {4, 4, 5, 5, 6, 6, 7, 7}
					compare2 = Avx.Permute2x128(temp1, temp2, 0b00_11_00_01).AsSingle();
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
			ComplexDouble scalarT = scalar.GenericConvert<U, ComplexDouble>();
			Modify op;
			if (typeof(Op) == typeof(AddScalar))
				op = Modify.AddScalar;
			else if (typeof(Op) == typeof(MultiplyScalar))
				op = Modify.MultiplyScalar;
			else if (typeof(Op) == typeof(Modulo))
				op = Modify.Modulo; // not possible here
			else if (typeof(Op) == typeof(Sqrt))
				op = Modify.Sqrt;
			else if (typeof(Op) == typeof(Square))
				op = Modify.Square;
			else if (typeof(Op) == typeof(Truncate))
				op = Modify.Truncate;
			else if (typeof(Op) == typeof(Reciprocal))
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
					// {abs(a[0]), abs(a[1]), ..., abs(a[3])}
					Vector256<double> currentAbs = ComplexSquareAbs(current1, current2);
					// {abs(a[0]) > threshold, abs(a[1]) > threshold, ..., abs(a[3]) > threshold}
					Vector256<double> compare = Avx.CompareNotGreaterThan(currentAbs, scalarSquares);
					// has "Not" since AVX compare is reversed
					// {abs(a[0]) > threshold, abs(a[0]) > threshold, ..., abs(a[1]) > threshold}
					Vector256<double> compare1 = Avx.UnpackLow(compare, compare);
					// {abs(a[2]) > threshold, abs(a[2]) > threshold, ..., abs(a[3]) > threshold}
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
			return VectorModify<T, T, AddScalar>(x, scalr);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static bool PointWiseModulo<T>(Storage<T> x, T mod) where T : unmanaged
		{
			if (Const<T>.IsComplex || !Const<T>.IsIntegralType)
				throw new TypeMismatchException(typeof(T), TypeMismatchException.MismatchReason.NotInteger);
			return VectorModify<T, T, Modulo>(x, mod);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static bool PointWiseConjugate<T>(Storage<T> x) where T : unmanaged
		{
			if (Const<T>.IsComplex)
				return VectorModify<T, T, Conjugate>(x, default);
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
				return VectorModify<T, double, Sqrt>(x, p);
			else if (p == 2)
				return VectorModify<T, double, Square>(x, p);
			else if (p == 1)
				return true;
			else if (p == -1)
				return VectorModify<T, double, Reciprocal>(x, p);
			else
				return VectorModify<T, double, PowerDouble>(x, p);
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
				return VectorModify<T, T, Sqrt>(x, p);
			else if (p.IsEqual(Const<T>.Two))
				return VectorModify<T, T, Square>(x, p);
			else if (p.IsOne())
				return true;
			else if (p.IsEqual(Const<T>.MinusOne))
				return VectorModify<T, T, Reciprocal>(x, p);
			else
				return VectorModify<T, T, PowerT>(x, p);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static bool Scale<T>(Storage<T> x, T scalar) where T : unmanaged
		{
			return VectorModify<T, T, MultiplyScalar>(x, scalar);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal new static bool TruncateArray<T>(Storage<T> x, double threshold) where T : unmanaged
		{
			return VectorModify<T, double, Truncate>(x, threshold);
		}
	}
#pragma warning restore CS1591 // 缺少对公共可见类型或成员的 XML 注释
}
