using System.Numerics;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

using Althea.Backend.Storage;
using Althea.Linq;
using Althea.LinearAlgebra;


namespace Althea.Backend.CSharp.LinearAlgebra;

public unsafe partial class Api
{
	internal struct U_AddScalar { }
	internal struct U_MultiplyScalar { }
	internal struct U_Modulo { }
	internal struct U_PowerT { }
	internal struct U_PowerDouble { }
	internal struct U_Truncate { }
	internal struct U_Conjugate { }
	internal struct U_Sqrt { }
	internal struct U_Square { }
	internal struct U_Reciprocal { }
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

	private static class OtherOp<T> where T : unmanaged, Numerics.INumber<T>
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
			if (T.IsComplexType)
			{
				var method = typeof(T).GetProperty(nameof(Complex<Numerics.Single>.Magnitude), System.Reflection.BindingFlags.Public)?.GetGetMethod();
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
	private static void VectorModifyFloatManaged<T, Op>(T* x, int incx, T* y, int incy, int length, T scalar) where T : unmanaged, IBinaryFloat<T>
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

		// JIT shall in-line / eliminate all switches and type conditions as if they do not exist
		for (int i = 0, ix = 0, iy = 0; i < length; i++, ix += incx, iy += incy)
		{
			y[iy] = op switch
			{
				Modify.PowerT or Modify.PowerDouble => T.Pow(x[ix], scalar),
				Modify.Conjugate => T.Conjugate(x[ix]),
				Modify.Sqrt => T.Sqrt(x[ix]),
				_ => default,
			};
		}
	}

	private delegate void VectorModifyFloatDelegate<T>(T* x, int length, T scalar) where T : unmanaged, Numerics.INumber<T>;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void VectorModifyManaged<T, Op>(T* x, int incx, T* y, int incy, int length, T scalar) where T : unmanaged, Numerics.INumber<T>
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
			var func = typeof(Api).GetMethod(nameof(VectorModifyFloatManaged), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)?.MakeGenericMethod(new[] { typeof(T), typeof(Op) })?.CreateDelegate<VectorModifyFloatDelegate<T>>();
			if (func is null)
				throw new MethodAccessException();
			func(x, length, scalar);
			return;
		}
		Func<T, T, T> mod = OtherOp<T>.ModuloDelegate;
		Func<T, double, T> trunc = OtherOp<T>.TruncateDelegate;

		// JIT shall in-line / eliminate all switches and type conditions as if they do not exist
		for (int i = 0, ix = 0, iy = 0; i < length; i++, ix += incx, iy += incy)
		{
			if (op == Modify.Modulo)
			{
				T a = x[ix];
				if (typeof(T) == typeof(Numerics.UInt32))
				{ Numerics.UInt32 v = (*(Numerics.UInt32*)&a) % (*(Numerics.UInt32*)&scalar); y[iy] = *(T*)&v; }
				else if (typeof(T) == typeof(Numerics.UInt64))
				{ Numerics.UInt64 v = (*(Numerics.UInt64*)&a) % (*(Numerics.UInt64*)&scalar); y[iy] = *(T*)&v; }
				else if (typeof(T) == typeof(Numerics.Int32))
				{ Numerics.Int32 v = (*(Numerics.Int32*)&a) % (*(Numerics.Int32*)&scalar); y[iy] = *(T*)&v; }
				else if (typeof(T) == typeof(Numerics.Int64))
				{ Numerics.Int64 v = (*(Numerics.Int64*)&a) % (*(Numerics.Int64*)&scalar); y[iy] = *(T*)&v; }
				else
					y[iy] = mod(a, scalar);
			}
			else if (op == Modify.Truncate)
			{
				T a = x[ix];
				Numerics.Double scalarD = scalar.AsDouble(), scalarDS = scalarD * scalarD;
				if (typeof(T) == typeof(Numerics.UInt32))
				{ Numerics.UInt32 v = (*(Numerics.UInt32*)&a) > scalarD ? (*(Numerics.UInt32*)&a) : 0; y[iy] = *(T*)&v; }
				else if (typeof(T) == typeof(Numerics.UInt64))
				{ Numerics.UInt64 v = (*(Numerics.UInt64*)&a) > scalarD ? (*(Numerics.UInt64*)&a) : 0; y[iy] = *(T*)&v; }
				else if (typeof(T) == typeof(Numerics.Double))
				{ int v = (*(Numerics.Int32*)&a) > scalarD ? (*(Numerics.Int32*)&a) : 0; y[iy] = *(T*)&v; }
				else if (typeof(T) == typeof(Numerics.Int64))
				{ Numerics.Int64 v = (*(Numerics.Int64*)&a) > scalarD ? (*(Numerics.Int64*)&a) : 0; y[iy] = *(T*)&v; }
				else if (typeof(T) == typeof(Numerics.Single))
				{ Numerics.Single v = (*(Numerics.Single*)&a) > scalarD ? (*(Numerics.Single*)&a) : 0; y[iy] = *(T*)&v; }
				else if (typeof(T) == typeof(Numerics.Double))
				{ Numerics.Double v = (*(Numerics.Double*)&a) > scalarD ? (*(Numerics.Double*)&a) : 0; y[iy] = *(T*)&v; }
				else if (typeof(T) == typeof(Complex<Numerics.Single>))
				{ Complex<Numerics.Single> v = (*(Complex<Numerics.Single>*)&a).MagnitudeSquared > scalarDS ? (*(Complex<Numerics.Single>*)&a) : default; y[iy] = *(T*)&v; }
				else if (typeof(T) == typeof(Complex<Numerics.Double>) || typeof(T) == typeof(Complex<Numerics.Double>))
				{ Complex<Numerics.Double> v = (*(Complex<Numerics.Double>*)&a).MagnitudeSquared > scalarDS ? (*(Complex<Numerics.Double>*)&a) : default; y[iy] = *(T*)&v; }
				else
					y[iy] = trunc(a, scalarD);
			}
			else
			{
				y[iy] = op switch
				{
					Modify.AddScalar => x[ix] + scalar,
					Modify.MultiplyScalar => x[ix] * scalar,
					Modify.Conjugate => T.Conjugate(x[ix]),
					Modify.Square => x[ix] * x[ix],
					Modify.Reciprocal => T.One / x[ix],
					_ => default,
				};
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void VectorModifyReal<T, U, Op>(void* xx, void* yy, int length, void* scalarPtr) where T : unmanaged, Numerics.INumber<T> where U : unmanaged, System.Numerics.INumber<U>
	{
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
		U scalar = *(U*)scalarPtr;
		U* x = (U*)xx, y = (U*)yy, end = x + length;
		Vector<U> scalars = new(scalar), ones = Vector<U>.One;
		for (; x + Vector<U>.Count <= end; x += Vector<U>.Count, y += Vector<U>.Count)
		{
			Vector<U> current = LoadVector(x);
			switch (op)
			{
				case Modify.AddScalar:
					current += scalars;
					break;
				case Modify.MultiplyScalar:
					current *= scalar;
					break;
				case Modify.Modulo:
					var temp = current / scalars;
					temp *= scalar;
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
					var compare = Vector.GreaterThan(abs, scalars);
					current = Vector.ConditionalSelect(compare, current, Vector<U>.Zero);
					break;
				default:
					break;
			}
			StoreVector(current, y);
		}
		// modify left
		if (x < end)
			VectorModifyManaged<T, Op>((T*)x, 1, (T*)y, 1, (int)(end - x), *(T*)&scalar);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void VectorModifyCompex<U, Op>(Complex<Numerics.Single>* x, Complex<Numerics.Single>* y, int length, U scalar) where U : unmanaged, Numerics.INumber<U>
	{
		Complex<Numerics.Single> scalarT = scalar.As<U, Complex<Numerics.Single>>();
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
			VectorModifyReal<Numerics.Single, float, Op>(x, y, length * 2, &scalarT);
			return;
		}
		// normal
		int lengthLeft = length, offset = 0;
		if (op != Modify.Truncate)
		{
			Vector256<float> scalars = default;
			Span<Complex<Numerics.Single>> _temp = new(&scalars, Vector256<float>.Count / 2);
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
				StoreVector256(current, y + offset);
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

				StoreVector256(current1, y + offset);
				StoreVector256(current1, y + offset + Vector256<float>.Count / 2);
				lengthLeft -= Vector256<float>.Count;
				offset += Vector256<float>.Count;
			}
		}
		// modify left
		if (lengthLeft > 0)
		{
			VectorModifyManaged<Complex<Numerics.Single>, Op>(x + offset, 1, y + offset, 1, lengthLeft, *(Complex<Numerics.Single>*)&scalar);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void VectorModifyCompex<U, Op>(Complex<Numerics.Double>* x, Complex<Numerics.Double>* y, int length, U scalar) where U : unmanaged, Numerics.INumber<U>
	{
		Complex<Numerics.Double> scalarT = scalar.As<U, Complex<Numerics.Double>>();
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
			VectorModifyReal<Numerics.Double, double, Op>(x, y, length * 2, &scalarT);
			return;
		}
		// normal
		int lengthLeft = length, offset = 0;
		if (op != Modify.Truncate)
		{
			Vector256<double> scalars = default;
			Span<Complex<Numerics.Double>> _temp = new(&scalars, Vector256<double>.Count / 2);
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
				StoreVector256(current, y + offset);
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
				StoreVector256(current1, y + offset);
				StoreVector256(current1, y + offset + Vector256<double>.Count / 2);
				lengthLeft -= Vector256<double>.Count;
				offset += Vector256<double>.Count;
			}
		}
		// modify left
		if (lengthLeft > 0)
		{
			VectorModifyManaged<Complex<Numerics.Double>, Op>(x + offset, 1, y + offset, 1, lengthLeft, *(Complex<Numerics.Double>*)&scalar);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool VectorModify<T, Op>(T* px, int incx, T* py, int incy, int length, T scalar) where T : unmanaged, Numerics.INumber<T>
	{
		if (incx != 1 || incy != 1 || !Vector.IsHardwareAccelerated || length <= (Vector<byte>.Count / sizeof(T) * 4))
		{   // no SIMD or too short
			VectorModifyManaged<T, Op>(px, incx, py, incy, length, scalar);
			return true;
		}

		if (T.IsComplexType)
		{
			if (T.Type.IsInteger() || !Avx.IsSupported)
			{   // no AVX's HorizontalAdd and Unpack (Vector<T> has not corresponding implementation yet)
				VectorModifyManaged<T, Op>(px, 1, py, 1, length, scalar);
			}
			else if (typeof(T) == typeof(Complex<Numerics.Single>))
			{
				VectorModifyCompex<T, Op>((Complex<Numerics.Single>*)px, (Complex<Numerics.Single>*)py, length, scalar);
			}
			else // double
			{
				VectorModifyCompex<T, Op>((Complex<Numerics.Double>*)px, (Complex<Numerics.Double>*)py, length, scalar);
			}
		}
		else
		{
			delegate*<void*, void*, int, void*, void> func = default(T) switch
			{
				Numerics.Double => &VectorModifyReal<Numerics.Double, double, Op>,
				Numerics.Single => &VectorModifyReal<Numerics.Single, float, Op>,
				Numerics.Int8 => &VectorModifyReal<Numerics.Int8, sbyte, Op>,
				Numerics.Int16 => &VectorModifyReal<Numerics.Int16, short, Op>,
				Numerics.Int32 => &VectorModifyReal<Numerics.Int32, int, Op>,
				Numerics.Int64 => &VectorModifyReal<Numerics.Int64, long, Op>,
				Numerics.UInt8 => &VectorModifyReal<Numerics.UInt8, byte, Op>,
				Numerics.UInt16 => &VectorModifyReal<Numerics.UInt16, ushort, Op>,
				Numerics.UInt32 => &VectorModifyReal<Numerics.UInt32, uint, Op>,
				Numerics.UInt64 => &VectorModifyReal<Numerics.UInt64, ulong, Op>,
				_ => null,
			};
			func(px, py, length, &scalar);
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool VectorModify<T, TS1, TS2, Op>(TS1 x, long strideX, TS2 y, long strideY, T scalar) where T : unmanaged, Numerics.INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
	{
		if (!GetPointer(x, strideX, out T* px, out int length, out int incX))
			return false;
		if (!GetPointer(y, strideY, out T* py, out int ny, out int incY))
			return false;
		length = Math.Min(length, ny);
		if (length == 0)
			return true;
		return VectorModify<T, Op>(px, incX, py, incY, length, scalar);
	}
	
	public virtual partial bool Scale<T, TS>(TS x, long strideX, T scalar) where T : unmanaged, Numerics.INumber<T> where TS : class, IStorage<T, TS> => VectorModify<T, TS, TS, U_MultiplyScalar>(x, strideX, x, strideX, scalar);

	public virtual partial bool GeneralVectorUnary<T, TS1, TS2>(UnaryOperation op, TS1 x, long strideX, TS2 y, long strideY) where T : unmanaged, Numerics.INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
	{
		return op switch
		{
			UnaryOperation.Identity => true,
			UnaryOperation.Conjugate => !T.IsComplexType || VectorModify<T, TS1, TS2, U_Conjugate>(x, strideX, y, strideY, default),
			UnaryOperation.Negate => VectorModify<T, TS1, TS2, U_MultiplyScalar>(x, strideX, y, strideY, -T.One),
			UnaryOperation.AbsoluteValue => false,
			_ => false,
		};
	}

	public virtual partial bool GeneralVectorBinaryScalar<T, TS1, TS2>(BinaryScalarOperation op, T scalar, TS1 x, long strideX, TS2 y, long strideY) where T : unmanaged, Numerics.INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
	{
		return op switch
		{
			BinaryScalarOperation.Add => VectorModify<T, TS1, TS2, U_AddScalar>(x, strideX, y, strideY, scalar),
			BinaryScalarOperation.Multiply => VectorModify<T, TS1, TS2, U_MultiplyScalar>(x, strideX, y, strideY, scalar),
			BinaryScalarOperation.Power => PointWisePower(x, strideX, y, strideY, scalar),
			BinaryScalarOperation.Maximum => false,
			BinaryScalarOperation.Mininum => false,
			BinaryScalarOperation.Fill => FillWithValue(y, strideY, scalar),
			BinaryScalarOperation.Truncate => VectorModify<T, TS1, TS2, U_Truncate>(x, strideX, y, strideY, scalar),
			_ => false,
		};
	}

	private static bool FillWithValue<T, TS>(TS x, long strideX, T value) where T : unmanaged, Numerics.INumber<T> where TS : class, IStorage<T, TS>
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

	private static bool PointWisePower<T, TS1, TS2>(TS1 x, long strideX, TS2 y, long strideY, T p) where T : unmanaged, Numerics.INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
	{
		if (p == T.Zero)
			return FillWithValue(x, strideX, T.One);
		if (p == T.One)
			return true;
		if (p == -T.One)
			return VectorModify<T, TS1, TS2, U_Reciprocal>(x, strideX, y, strideY, p);
		if (p == (T.One + T.One))
			return VectorModify<T, TS1, TS2, U_Square>(x, strideX, y, strideY, p);
		if (p == T.One / (T.One + T.One))
			return VectorModify<T, TS1, TS2, U_Sqrt>(x, strideX, y, strideY, p);
		if (T.Conjugate(p) == p)
			return VectorModify<T, TS1, TS2, U_PowerDouble>(x, strideX, y, strideY, p);
		return VectorModify<T, TS1, TS2, U_PowerT>(x, strideX, y, strideY, p);
	}
}
