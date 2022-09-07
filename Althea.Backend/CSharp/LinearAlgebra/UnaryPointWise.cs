using System.Numerics;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

using Althea.Backend.Storage;
using Althea.Helpers;
using Althea.LinearAlgebra;

using static Althea.Backend.CSharp.MemoryPointerChecker;


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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void VectorModifyFloatManaged<T, Op>(T* x, int incx, T* y, int incy, int length, T scalar) where T : unmanaged, IBinaryFloat<T>
	{
		// JIT shall in-line / eliminate all switches and type conditions as if they do not exist
		for (int i = 0, ix = 0, iy = 0; i < length; i++, ix += incx, iy += incy)
		{
			y[iy] = default(Op) switch
			{
				U_PowerT or U_PowerDouble => T.Pow(x[ix], scalar),
				U_Conjugate => T.Conjugate(x[ix]),
				U_Sqrt => T.Sqrt(x[ix]),
				_ => default,
			};
		}
	}

	private delegate void VectorModifyFloatDelegate<T>(T* x, int incx, T* y, int incy, int length, T scalar) where T : unmanaged, IBaseNumber<T>;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool VectorModifyManaged<T, Op>(T* x, int incx, T* y, int incy, int length, T scalar) where T : unmanaged, IBaseNumber<T>
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
			func(x, incx, y, incy, length, scalar);
			return true;
		}

		// JIT shall in-line / eliminate all switches and type conditions as if they do not exist
		for (int i = 0, ix = 0, iy = 0; i < length; i++, ix += incx, iy += incy)
		{
			if (op == Modify.Modulo)
			{
				T a = x[ix];
				if (typeof(T) == typeof(UnsignedInt32))
				{ UnsignedInt32 v = (*(UnsignedInt32*)&a) % (*(UnsignedInt32*)&scalar); y[iy] = *(T*)&v; }
				else if (typeof(T) == typeof(UnsignedInt64))
				{ UnsignedInt64 v = (*(UnsignedInt64*)&a) % (*(UnsignedInt64*)&scalar); y[iy] = *(T*)&v; }
				else if (typeof(T) == typeof(SignedInt32))
				{ SignedInt32 v = (*(SignedInt32*)&a) % (*(SignedInt32*)&scalar); y[iy] = *(T*)&v; }
				else if (typeof(T) == typeof(SignedInt64))
				{ SignedInt64 v = (*(SignedInt64*)&a) % (*(SignedInt64*)&scalar); y[iy] = *(T*)&v; }
				else
					return false;
			}
			else if (op == Modify.Truncate)
			{
				T a = x[ix];
				T scalarT = T.Abs(scalar);
				Float64 scalarD = scalarT.AsDouble(), scalarDS = scalarD * scalarD;
				if (T.IsComplexType)
				{
					if (typeof(T) == typeof(Complex<Float32>))
					{ Complex<Float32> v = (*(Complex<Float32>*)&a).MagnitudeSquared > scalarDS ? (*(Complex<Float32>*)&a) : default; y[iy] = *(T*)&v; }
					else if (typeof(T) == typeof(Complex<Float64>) || typeof(T) == typeof(Complex<Float64>))
					{ Complex<Float64> v = (*(Complex<Float64>*)&a).MagnitudeSquared > scalarDS ? (*(Complex<Float64>*)&a) : default; y[iy] = *(T*)&v; }
					else
						return false;
				}
				else
				{
					y[iy] = T.Abs(a) > scalarT ? a : default;
				}
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
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool VectorModifyReal<T, U, Op>(void* xx, void* yy, int length, void* scalarPtr) where T : unmanaged, IBaseNumber<T> where U : unmanaged, System.Numerics.INumber<U>
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
		if (op == Modify.Truncate)
			scalar = U.Abs(scalar);
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
			return VectorModifyManaged<T, Op>((T*)x, 1, (T*)y, 1, (int)(end - x), *(T*)&scalar);
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool VectorModifyComplex<U, Op>(Complex<Float32>* x, Complex<Float32>* y, int length, U scalar) where U : unmanaged, IBaseNumber<U>
	{
		Complex<Float32> scalarT = scalar.As<U, Complex<Float32>>();
		Modify op;
		if (typeof(Op) == typeof(U_AddScalar))
			op = Modify.AddScalar;
		else if (typeof(Op) == typeof(U_MultiplyScalar))
			op = Modify.MultiplyScalar;
		else if (typeof(Op) == typeof(U_Truncate))
			op = Modify.Truncate;
		else if (typeof(Op) == typeof(U_Reciprocal))
			op = Modify.Reciprocal;
		else if (typeof(Op) == typeof(U_Conjugate))
			op = Modify.Conjugate;
		else
			return false;

		// shortcut
		if (op == Modify.AddScalar && scalarT.Imaginary == scalarT.Real)
		{
			VectorModifyReal<Float32, float, Op>(x, y, length * 2, &scalarT);
			return true;
		}
		if (op == Modify.MultiplyScalar && scalarT.Imaginary == 0)
		{
			VectorModifyReal<Float32, float, Op>(x, y, length * 2, &scalarT);
			return true;
		}
		// normal
		int lengthLeft = length, offset = 0;
		if (op == Modify.Conjugate)
		{
			Vector256<float> oneMinusOnes = default;
			Span<float> _temp = new(&oneMinusOnes, Vector256<float>.Count);
			_temp[1] = _temp[3] = -1;
			// loop
			while (lengthLeft >= Vector256<float>.Count / 2) // Vector256<Complex<float>>.Count
			{
				Vector256<float> current = LoadVector256<float>(x + offset);
				current = Avx.Multiply(current, oneMinusOnes);
				StoreVector256(current, y + offset);
				lengthLeft -= Vector256<float>.Count / 2;
				offset += Vector256<float>.Count / 2;
			}
		}
		else if (op == Modify.AddScalar)
		{
			Vector256<float> scalars = new Vector<float>(*(float*)&scalarT).AsVector256();
			Span<float> _temp = new(&scalars, Vector256<float>.Count);
			_temp[1] = _temp[3] = ((float*)&scalarT)[1];
			// loop
			while (lengthLeft >= Vector256<float>.Count / 2) // Vector256<Complex<float>>.Count
			{
				Vector256<float> current = LoadVector256<float>(x + offset);
				current = Avx.Add(current, scalars);
				StoreVector256(current, y + offset);
				lengthLeft -= Vector256<float>.Count / 2;
				offset += Vector256<float>.Count / 2;
			}
		}
		else if (op == Modify.MultiplyScalar)
		{
			float sRe = *(float*)&scalarT, sIm = ((float*)&scalarT)[1];
			// loop
			while (lengthLeft >= Vector256<float>.Count) // Vector256<Complex<float>>.Count * 2
			{
				var x0 = LoadVector256<float>(x + offset);
				var x1 = LoadVector256<float>(x + offset + Vector256<float>.Count / 2);
				ComplexUnpack(x0, x1, out var realX, out var imagX);
				ComplexMultiplyUnpacked(realX, imagX, sRe, sIm, ref x0, ref x1);
				ComplexPack(x0, x1, out var y0, out var y1);
				StoreVector256(y0, y + offset);
				StoreVector256(y1, y + offset + Vector256<float>.Count / 2);
				lengthLeft -= Vector256<float>.Count;
				offset += Vector256<float>.Count;
			}
		}
		else if (op == Modify.Reciprocal)
		{
			Vector256<float> oneMinusOnes = default;
			Span<float> _temp = new(&oneMinusOnes, Vector256<float>.Count);
			_temp[1] = _temp[3] = -1;
			// loop
			while (lengthLeft >= Vector256<float>.Count) // Vector256<Complex<float>>.Count * 2
			{
				var abs = ComplexSquareAbsNoOrderSingle(x + offset);
				var left = LoadVector256<float>(x + offset);
				var right = LoadVector256<float>(x + offset + Vector256<float>.Count / 2);
				left *= oneMinusOnes; right *= oneMinusOnes;
				left /= abs; right /= abs;
				StoreVector256(left, y + offset);
				StoreVector256(right, y + offset + Vector256<float>.Count / 2);
				lengthLeft -= Vector256<float>.Count;
				offset += Vector256<float>.Count;
			}
		}
		else if (op == Modify.Truncate)
		{
			Vector256<float> zeros = Vector<float>.Zero.AsVector256();
			Vector256<float> scalarSquares = default;
			Span<float> _temp = new(&scalarSquares, Vector256<float>.Count / 2);
			_temp.Fill((*(float*)&scalar) * (*(float*)&scalar));
			// loop
			while (lengthLeft >= Vector256<float>.Count) // Vector256<Complex<float>>.Count * 2
			{
				// {a[0].r, a[0].i, ..., a[1].i}
				Vector256<float> current1 = LoadVector256<float>(x + offset);
				// {a[2].r, a[2].i, ..., a[3].i}
				Vector256<float> current2 = LoadVector256<float>(x + offset + Vector256<float>.Count / 2);
				// abs(a[{0, 2, 1, 3}])
				Vector256<float> currentAbs = ComplexSquareAbsNoOrder(current1, current2);
				// abs(a[{0, 2, 1, 3}]) > threshold
				Vector256<float> compare = Avx.CompareNotGreaterThan(currentAbs, scalarSquares);
				// has "Not" since AVX compare is reversed
				// {0, 0, 1, 1}
				Vector256<float> compare1 = Avx.UnpackLow(compare, compare);
				// {2, 2, 3, 3}
				Vector256<float> compare2 = Avx.UnpackHigh(compare, compare);
				current1 = Avx.BlendVariable(current1, zeros, compare1);
				current2 = Avx.BlendVariable(current1, zeros, compare2);
				StoreVector256(current1, y + offset);
				StoreVector256(current1, y + offset + Vector256<float>.Count / 2);
				lengthLeft -= Vector256<float>.Count;
				offset += Vector256<float>.Count;
			}
		}
		// modify left
		if (lengthLeft > 0)
			return VectorModifyManaged<Complex<Float32>, Op>(x + offset, 1, y + offset, 1, lengthLeft, *(Complex<Float32>*)&scalar);
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool VectorModifyComplex<U, Op>(Complex<Float64>* x, Complex<Float64>* y, int length, U scalar) where U : unmanaged, IBaseNumber<U>
	{
		Complex<Float64> scalarT = scalar.As<U, Complex<Float64>>();
		Modify op;
		if (typeof(Op) == typeof(U_AddScalar))
			op = Modify.AddScalar;
		else if (typeof(Op) == typeof(U_MultiplyScalar))
			op = Modify.MultiplyScalar;
		else if (typeof(Op) == typeof(U_Truncate))
			op = Modify.Truncate;
		else if (typeof(Op) == typeof(U_Reciprocal))
			op = Modify.Reciprocal;
		else if (typeof(Op) == typeof(U_Conjugate))
			op = Modify.Conjugate;
		else
			return false;

		// shortcut
		if (op == Modify.AddScalar && scalarT.Imaginary == scalarT.Real)
		{
			VectorModifyReal<Float64, double, Op>(x, y, length * 2, &scalarT);
			return true;
		}
		if (op == Modify.MultiplyScalar && scalarT.Imaginary == 0)
		{
			VectorModifyReal<Float64, double, Op>(x, y, length * 2, &scalarT);
			return true;
		}
		// normal
		int lengthLeft = length, offset = 0;
		if (op == Modify.Conjugate)
		{
			Vector256<double> oneMinusOnes = default;
			Span<double> _temp = new(&oneMinusOnes, Vector256<double>.Count);
			_temp[0] = _temp[2] = 1;
			_temp[1] = _temp[3] = -1;
			// loop
			while (lengthLeft >= Vector256<double>.Count / 2) // Vector256<Complex<double>>.Count
			{
				var current = LoadVector256<double>(x + offset);
				current = Avx.Multiply(current, oneMinusOnes);
				StoreVector256(current, y + offset);
				lengthLeft -= Vector256<double>.Count / 2;
				offset += Vector256<double>.Count / 2;
			}
		}
		else if (op == Modify.AddScalar)
		{
			var scalars = new Vector<double>(*(double*)&scalarT).AsVector256();
			Span<double> _temp = new(&scalars, Vector256<double>.Count);
			_temp[1] = _temp[3] = ((double*)&scalarT)[1];
			// loop
			while (lengthLeft >= Vector256<double>.Count / 2) // Vector256<Complex<double>>.Count
			{
				var current = LoadVector256<double>(x + offset);
				current = Avx.Add(current, scalars);
				StoreVector256(current, y + offset);
				lengthLeft -= Vector256<double>.Count / 2;
				offset += Vector256<double>.Count / 2;
			}
		}
		else if (op == Modify.MultiplyScalar)
		{
			double sRe = *(double*)&scalarT, sIm = ((double*)&scalarT)[1];
			// loop
			while (lengthLeft >= Vector256<double>.Count) // Vector256<Complex<double>>.Count * 2
			{
				var x0 = LoadVector256<double>(x + offset);
				var x1 = LoadVector256<double>(x + offset + Vector256<double>.Count / 2);
				ComplexUnpack(x0, x1, out var realX, out var imagX);
				ComplexMultiplyUnpacked(realX, imagX, sRe, sIm, ref x0, ref x1);
				ComplexPack(x0, x1, out var y0, out var y1);
				StoreVector256(y0, y + offset);
				StoreVector256(y1, y + offset + Vector256<double>.Count / 2);
				lengthLeft -= Vector256<double>.Count;
				offset += Vector256<double>.Count;
			}
		}
		else if (op == Modify.Reciprocal)
		{
			Vector256<double> oneMinusOnes = default;
			Span<double> _temp = new(&oneMinusOnes, Vector256<double>.Count);
			_temp[1] = _temp[3] = -1;
			// loop
			while (lengthLeft >= Vector256<double>.Count) // Vector256<Complex<double>>.Count * 2
			{
				var abs = ComplexSquareAbsNoOrderDouble(x + offset);
				var left = LoadVector256<double>(x + offset);
				var right = LoadVector256<double>(x + offset + Vector256<double>.Count / 2);
				left *= oneMinusOnes; right *= oneMinusOnes;
				left /= abs; right /= abs;
				StoreVector256(left, y + offset);
				StoreVector256(right, y + offset + Vector256<double>.Count / 2);
				lengthLeft -= Vector256<double>.Count;
				offset += Vector256<double>.Count;
			}
		}
		else if (op == Modify.Truncate)
		{
			double scalarSquare = scalarT.MagnitudeSquared;
			var zeros = Vector<double>.Zero.AsVector256();
			var scalarSquares = new Vector<double>(scalarSquare).AsVector256();
			// loop
			while (lengthLeft >= Vector256<double>.Count) // Vector256<Complex<double>>.Count * 2
			{
				// {a[0].r, a[0].i, ..., a[1].i}
				var current1 = LoadVector256<double>(x + offset);
				// {a[2].r, a[2].i, ..., a[3].i}
				var current2 = LoadVector256<double>(x + offset + Vector256<double>.Count / 2);
				// abs(a[{0, 2, 1, 3}])
				var currentAbs = ComplexSquareAbsNoOrder(current1, current2);
				// abs(a[{0, 2, 1, 3}]) > threshold
				var compare = Avx.CompareNotGreaterThan(currentAbs, scalarSquares);
				// has "Not" since AVX compare is reversed
				// {0, 0, 1, 1}
				var compare1 = Avx.UnpackLow(compare, compare);
				// {2, 2, 3, 3}
				var compare2 = Avx.UnpackHigh(compare, compare);
				current1 = Avx.BlendVariable(current1, zeros, compare1);
				current2 = Avx.BlendVariable(current2, zeros, compare2);
				StoreVector256(current1, y + offset);
				StoreVector256(current2, y + offset + Vector256<double>.Count / 2);
				lengthLeft -= Vector256<double>.Count;
				offset += Vector256<double>.Count;
			}
		}
		// modify left
		if (lengthLeft > 0)
			return VectorModifyManaged<Complex<Float64>, Op>(x + offset, 1, y + offset, 1, lengthLeft, *(Complex<Float64>*)&scalar);
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool VectorModify<T, Op>(T* px, int incx, T* py, int incy, int length, T scalar) where T : unmanaged, IBaseNumber<T>
	{
		if (incx != 1 || incy != 1 || !Vector.IsHardwareAccelerated || length <= (Vector<byte>.Count / sizeof(T) * 4))
		{   // no SIMD or too short
			return VectorModifyManaged<T, Op>(px, incx, py, incy, length, scalar);
		}

		if (T.IsComplexType)
		{
			if (T.Type.IsInteger() || !Avx.IsSupported)
			{   // no AVX's HorizontalAdd and Unpack (Vector<T> has not corresponding implementation yet)
				return VectorModifyManaged<T, Op>(px, 1, py, 1, length, scalar);
			}
			else if (typeof(T) == typeof(Complex<Float32>))
			{
				return VectorModifyComplex<T, Op>((Complex<Float32>*)px, (Complex<Float32>*)py, length, scalar);
			}
			else // double
			{
				return VectorModifyComplex<T, Op>((Complex<Float64>*)px, (Complex<Float64>*)py, length, scalar);
			}
		}
		else
		{
			delegate*<void*, void*, int, void*, bool> func = default(T) switch
			{
				Float64 => &VectorModifyReal<Float64, double, Op>,
				Float32 => &VectorModifyReal<Float32, float, Op>,
				SignedInt8 => &VectorModifyReal<SignedInt8, sbyte, Op>,
				SignedInt16 => &VectorModifyReal<SignedInt16, short, Op>,
				SignedInt32 => &VectorModifyReal<SignedInt32, int, Op>,
				SignedInt64 => &VectorModifyReal<SignedInt64, long, Op>,
				UnsignedInt8 => &VectorModifyReal<UnsignedInt8, byte, Op>,
				UnsignedInt16 => &VectorModifyReal<UnsignedInt16, ushort, Op>,
				UnsignedInt32 => &VectorModifyReal<UnsignedInt32, uint, Op>,
				UnsignedInt64 => &VectorModifyReal<UnsignedInt64, ulong, Op>,
				_ => null,
			};
			return func(px, py, length, &scalar);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool VectorModify<T, TS1, TS2, Op>(TS1 x, long strideX, TS2 y, long strideY, T scalar) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
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
	
	public virtual partial bool Scale<T, TS>(TS x, long strideX, T scalar) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS> => VectorModify<T, TS, TS, U_MultiplyScalar>(x, strideX, x, strideX, scalar);

	public virtual partial bool GeneralVectorUnary<T, TS1, TS2>(ManagedEnum<UnaryOperation> op, TS1 x, long strideX, TS2 y, long strideY) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
	{
		return op.Value switch
		{
			UnaryOperation.Identity => true,
			UnaryOperation.Conjugate => !T.IsComplexType || VectorModify<T, TS1, TS2, U_Conjugate>(x, strideX, y, strideY, default),
			UnaryOperation.Negate => VectorModify<T, TS1, TS2, U_MultiplyScalar>(x, strideX, y, strideY, -T.One),
			UnaryOperation.AbsoluteValue => false,
			_ => false,
		};
	}

	public virtual partial bool GeneralVectorBinaryScalar<T, TS1, TS2>(ManagedEnum<BinaryScalarOperation> op, T scalar, TS1 x, long strideX, TS2 y, long strideY) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
	{
		return op.Value switch
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

	private static bool FillWithValue<T, TS>(TS x, long strideX, T value) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
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

	private static bool PointWisePower<T, TS1, TS2>(TS1 x, long strideX, TS2 y, long strideY, T p) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
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
