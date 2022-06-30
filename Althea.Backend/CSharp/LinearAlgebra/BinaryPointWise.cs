using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

using Althea.LinearAlgebra;
using Althea.Linq;


namespace Althea.Backend.CSharp.LinearAlgebra
{
	public unsafe partial class Api
	{
		#region equals
		public virtual partial bool GeneralVectorsEqual<T, TS1, TS2>(TS1 x, long strideX, TS2 y, long strideY, out bool equals) where T : unmanaged, Numerics.INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
		{
			equals = false;
			if (x.Length != y.Length)
				return true;
			if (!GetPointer(x, strideX, out T* px, out int length, out int incx))
				return false;
			if (!GetPointer(y, strideY, out T* py, out int length2, out int incy))
				return false;
			if (px == py)
			{
				equals = true; return true;
			}
			length = Math.Min(length, length2);

			if (incx == 1 && incy == 1)
			{
				if (T.Type.IsInteger())
				{
					length *= sizeof(T);
					equals = new ReadOnlySpan<byte>(px, length).SequenceEqual(new(py, length));
				}
				else
				{
					if (typeof(T) == typeof(Numerics.Single))
						equals = new ReadOnlySpan<float>(px, length).SequenceEqual(new(py, length));
					else if (typeof(T) == typeof(Numerics.Double))
						equals = new ReadOnlySpan<double>(px, length).SequenceEqual(new(py, length));
					else if (typeof(T) == typeof(Complex<Numerics.Single>))
						equals = new ReadOnlySpan<float>(px, length * 2).SequenceEqual(new(py, length * 2));
					else if (typeof(T) == typeof(Complex<Numerics.Double>))
						equals = new ReadOnlySpan<double>(px, length * 2).SequenceEqual(new(py, length * 2));
					else
						return false;
				}
			}
			else
			{
				equals = true;
				for (int i = 0, ix = 0, iy = 0; i < length; i++, ix += incx, iy += incy)
				{
					if (px[ix] != py[iy])
					{
						equals = false;
						break;
					}
				}
			}
			return true;
		}
		#endregion


		#region add multiply divide
		internal struct B_Multiply { }
		internal struct B_Divide { }
		internal struct B_Add { }
		internal struct B_AddScaled { }
		internal struct B_AddConjugateScaled { }
		private enum BinaryModify
		{
			Multiply,
			Divide,
			Add,
			AddScaled,
			AddConjScaled
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void VectorsBinaryManaged<T, Op>(T* x, int incx, T* y, int incy, T* z, int incz, int length, T scalar) where T : unmanaged, Numerics.INumber<T>
		{
			BinaryModify op;
			if (typeof(Op) == typeof(B_Multiply))
				op = BinaryModify.Multiply;
			else if (typeof(Op) == typeof(B_Divide))
				op = BinaryModify.Divide;
			else if (typeof(Op) == typeof(B_Add))
				op = BinaryModify.Add;
			else if (typeof(Op) == typeof(B_AddScaled))
				op = BinaryModify.AddScaled;
			else
				op = BinaryModify.AddConjScaled;

			for (int i = 0, ix = 0, iy = 0, iz = 0; i < length; i++, ix += incx, iy += incy, iz += incz)
			{
				T a = x[ix], b = y[iy];
				T v;
				// floating point FMA accelerate
				if (typeof(T) == typeof(Numerics.Single) && op == BinaryModify.AddScaled)
				{
					float temp = MathF.FusedMultiplyAdd(*(Numerics.Single*)&scalar, *(Numerics.Single*)&b, *(Numerics.Single*)&a);
					v = *(T*)&temp;
				}
				else if (typeof(T) == typeof(double) && op == BinaryModify.AddScaled)
				{
					double temp = Math.FusedMultiplyAdd(*(double*)&scalar, *(double*)&b, *(double*)&a);
					v = *(T*)&temp;
				}
				else if ((typeof(T) == typeof(Complex<Numerics.Single>) || typeof(T) == typeof(Complex<Numerics.Single>)) && op == BinaryModify.AddScaled)
				{
					Complex<Numerics.Single> temp = Complex<Numerics.Single>.FusedMultiplyAdd(*(Complex<Numerics.Single>*)&scalar, *(Complex<Numerics.Single>*)&b, *(Complex<Numerics.Single>*)&a);
					v = *(T*)&temp;
				}
				else if ((typeof(T) == typeof(Complex<Numerics.Double>) || typeof(T) == typeof(Complex<Numerics.Double>)) && op == BinaryModify.AddScaled)
				{
					Complex<Numerics.Double> temp = Complex<Numerics.Double>.FusedMultiplyAdd(*(Complex<Numerics.Double>*)&scalar, *(Complex<Numerics.Double>*)&b, *(Complex<Numerics.Double>*)&a);
					v = *(T*)&temp;
				}
				else if ((typeof(T) == typeof(Complex<Numerics.Single>) || typeof(T) == typeof(Complex<Numerics.Single>)) && op == BinaryModify.AddConjScaled)
				{
					Complex<Numerics.Single> temp = Complex<Numerics.Single>.FusedMultiplyAdd(*(Complex<Numerics.Single>*)&scalar, (*(Complex<Numerics.Single>*)&b).Conjugate, *(Complex<Numerics.Single>*)&a);
					v = *(T*)&temp;
				}
				else if ((typeof(T) == typeof(Complex<Numerics.Double>) || typeof(T) == typeof(Complex<Numerics.Double>)) && op == BinaryModify.AddConjScaled)
				{
					Complex<Numerics.Double> temp = Complex<Numerics.Double>.FusedMultiplyAdd(*(Complex<Numerics.Double>*)&scalar, (*(Complex<Numerics.Double>*)&b).Conjugate, *(Complex<Numerics.Double>*)&a);
					v = *(T*)&temp;
				}
				// otherwise
				else
				{
					v = op switch
					{
						BinaryModify.Multiply => a * b,
						BinaryModify.Divide => a / b,
						BinaryModify.Add => a + b,
						BinaryModify.AddScaled => a + b * scalar,
						BinaryModify.AddConjScaled => a + T.Conjugate(b) * scalar,
						_ => default,
					};
				}
				z[iz] = v;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void VectorsBinaryReal<T, U, Op>(void* xx, void* yy, void* zz, int length, void* scalarPtr) where T : unmanaged, Numerics.INumber<T> where U : unmanaged, System.Numerics.INumber<U>
		{
			BinaryModify op;
			if (typeof(Op) == typeof(B_Multiply))
				op = BinaryModify.Multiply;
			else if (typeof(Op) == typeof(B_Divide))
				op = BinaryModify.Divide;
			else if (typeof(Op) == typeof(B_Add))
				op = BinaryModify.Add;
			else
				op = BinaryModify.AddScaled;

			// loop
			U* x = (U*)xx, y = (U*)yy, z = (U*)zz, end = x + length;
			var scalar = *(U*)scalarPtr;
			for (; x + Vector<U>.Count <= end; x += Vector<U>.Count, y += Vector<U>.Count, z += Vector<U>.Count)
			{
				Vector<U> currentX = LoadVector(x);
				Vector<U> currentY = LoadVector(y);
				switch (op)
				{
					case BinaryModify.Multiply:
						currentX *= currentY;
						break;
					case BinaryModify.Divide:
						currentX /= currentY;
						break;
					case BinaryModify.Add:
						currentX += currentY;
						break;
					case BinaryModify.AddScaled:
						currentX += currentY * scalar;
						break;
					default:
						break;
				}
				StoreVector(currentX, z);
			}
			// modify left
			if (x < end)
				VectorsBinaryManaged<T, Op>((T*)x, 1, (T*)y, 1, (T*)z, 1, (int)(end - x), *(T*)scalarPtr);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void VectorsBinaryComplex<Op>(Complex<Numerics.Single>* x, Complex<Numerics.Single>* y, Complex<Numerics.Single>* z, int length, Complex<Numerics.Single> scalar)
		{
			BinaryModify op;
			if (typeof(Op) == typeof(B_Multiply))
				op = BinaryModify.Multiply;
			else if (typeof(Op) == typeof(B_Divide))
				op = BinaryModify.Divide;
			else if (typeof(Op) == typeof(B_Add))
				op = BinaryModify.Add;
			else if (typeof(Op) == typeof(B_AddScaled))
				op = BinaryModify.AddScaled;
			else
				op = BinaryModify.AddConjScaled;

			// loop
			Vector256<float> scalarReals = new Vector<float>(scalar.Real).AsVector256();
			Vector256<float> scalarImags = new Vector<float>(scalar.Imaginary).AsVector256();
			int lengthLeft = length, offset = 0;
			while (lengthLeft >= Vector<float>.Count)
			{
				Vector256<float> currentX1 = LoadVector256<float>(x + offset), currentX2 = LoadVector256<float>(x + offset + Vector<float>.Count / 2);
				Vector256<float> currentY1 = LoadVector256<float>(y + offset), currentY2 = LoadVector256<float>(y + offset + Vector<float>.Count / 2);
				switch (op)
				{
					case BinaryModify.Multiply:
						ComplexMultiply<byte, bool>(currentX1, currentX2, currentY1, currentY2, out currentX1, out currentX2);
						break;
					case BinaryModify.Divide:
						ComplexDivide(currentX1, currentX2, currentY1, currentY2, out currentX1, out currentX2);
						break;
					case BinaryModify.Add:
						currentX1 = Avx.Add(currentX1, currentY1);
						currentX2 = Avx.Add(currentX2, currentY2);
						break;
					case BinaryModify.AddScaled:
						ComplexUnpack(currentX1, currentX2, out currentX1, out currentX2);
						ComplexUnpack(currentY1, currentY2, out currentY1, out currentY2);
						UnpackComplexMultiplyAdd<byte>(scalarReals, scalarImags, currentY1, currentY2, ref currentX1, ref currentX2);
						ComplexPack(currentX1, currentX2, out currentX1, out currentX2);
						break;
					case BinaryModify.AddConjScaled:
						ComplexUnpack(currentX1, currentX2, out currentX1, out currentX2);
						ComplexUnpack(currentY1, currentY2, out currentY1, out currentY2);
						currentY2 = -currentY2;
						UnpackComplexMultiplyAdd<byte>(scalarReals, scalarImags, currentY1, currentY2, ref currentX1, ref currentX2);
						ComplexPack(currentX1, currentX2, out currentX1, out currentX2);
						break;
					default:
						break;
				}
				StoreVector256(currentX1, z + offset);
				StoreVector256(currentX2, z + offset + Vector256<float>.Count / 2);
				lengthLeft -= Vector256<float>.Count;
				offset += Vector256<float>.Count;
			}
			// modify left
			if (lengthLeft > 0)
			{
				VectorsBinaryManaged<Complex<Numerics.Single>, Op>(x + offset, 1, y + offset, 1, z + offset, 1, lengthLeft, scalar);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void VectorsBinaryComplex<Op>(Complex<Numerics.Double>* x, Complex<Numerics.Double>* y, Complex<Numerics.Double>* z, int length, Complex<Numerics.Double> scalar)
		{
			BinaryModify op;
			if (typeof(Op) == typeof(B_Multiply))
				op = BinaryModify.Multiply;
			else if (typeof(Op) == typeof(B_Divide))
				op = BinaryModify.Divide;
			else if (typeof(Op) == typeof(B_Add))
				op = BinaryModify.Add;
			else
				op = BinaryModify.AddScaled;

			// loop
			Vector256<double> scalarReals = new Vector<double>(scalar.Real).AsVector256();
			Vector256<double> scalarImags = new Vector<double>(scalar.Imaginary).AsVector256();
			int lengthLeft = length, offset = 0;
			while (lengthLeft >= Vector<double>.Count)
			{
				Vector256<double> currentX1 = LoadVector256<double>(x + offset), currentX2 = LoadVector256<double>(x + offset + Vector<double>.Count / 2);
				Vector256<double> currentY1 = LoadVector256<double>(y + offset), currentY2 = LoadVector256<double>(y + offset + Vector<double>.Count / 2);
				switch (op)
				{
					case BinaryModify.Multiply:
						ComplexMultiply<byte, bool>(currentX1, currentX2, currentY1, currentY2, out currentX1, out currentX2);
						break;
					case BinaryModify.Divide:
						ComplexDivide(currentX1, currentX2, currentY1, currentY2, out currentX1, out currentX2);
						break;
					case BinaryModify.Add:
						currentX1 = Avx.Add(currentX1, currentY1);
						currentX2 = Avx.Add(currentX2, currentY2);
						break;
					case BinaryModify.AddScaled:
						ComplexUnpack(currentX1, currentX2, out currentX1, out currentX2);
						ComplexUnpack(currentY1, currentY2, out currentY1, out currentY2);
						UnpackComplexMultiplyAdd<byte>(scalarReals, scalarImags, currentY1, currentY2, ref currentX1, ref currentX2);
						ComplexPack(currentX1, currentX2, out currentX1, out currentX2);
						break;
					default:
						break;
				}
				StoreVector256(currentX1, z + offset);
				StoreVector256(currentX2, z + offset + Vector256<double>.Count / 2);
				lengthLeft -= Vector256<double>.Count;
				offset += Vector256<double>.Count;
			}
			// modify left
			if (lengthLeft > 0)
			{
				VectorsBinaryManaged<Complex<Numerics.Double>, Op>(x + offset, 1, y + offset, 1, z + offset, 1, lengthLeft, scalar);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool VectorsBinary<T, Op>(T* px, int incx, T* py, int incy, T* pz, int incz, T scalar, int length) where T : unmanaged, Numerics.INumber<T>
		{
			if (incx != 1 || incy != 1 || incz != 1 || !Vector.IsHardwareAccelerated || length <= (Vector<byte>.Count / sizeof(T) * 4))
			{   // no SIMD or too short
				VectorsBinaryManaged<T, Op>(px, incx, py, incy, pz, incz, length, scalar);
			}
			else if (T.IsComplexType)
			{
				if (T.Type.IsInteger() || !Avx.IsSupported)
				{   // no AVX's HorizontalAdd and Unpack (Vector<T> has not corresponding implementation yet)
					VectorsBinaryManaged<T, Op>(px, 1, py, 1, pz, 1, length, scalar);
				}
				else if (typeof(T) == typeof(Complex<Numerics.Single>) || typeof(T) == typeof(Complex<Numerics.Single>))
				{
					VectorsBinaryComplex<Op>((Complex<Numerics.Single>*)px, (Complex<Numerics.Single>*)py, (Complex<Numerics.Single>*)pz, length, *(Complex<Numerics.Single>*)&scalar);
				}
				else // double
				{
					VectorsBinaryComplex<Op>((Complex<Numerics.Double>*)px, (Complex<Numerics.Double>*)py, (Complex<Numerics.Double>*)pz, length, *(Complex<Numerics.Double>*)&scalar);
				}
			}
			else
			{
				delegate*<void*, void*, void*, int, void*, void> func = default(T) switch
				{
					Numerics.Double => &VectorsBinaryReal<Numerics.Double, double, Op>,
					Numerics.Single => &VectorsBinaryReal<Numerics.Single, float, Op>,
					Numerics.Int8 => &VectorsBinaryReal<Numerics.Int8, sbyte, Op>,
					Numerics.Int16 => &VectorsBinaryReal<Numerics.Int16, short, Op>,
					Numerics.Int32 => &VectorsBinaryReal<Numerics.Int32, int, Op>,
					Numerics.Int64 => &VectorsBinaryReal<Numerics.Int64, long, Op>,
					Numerics.UInt8 => &VectorsBinaryReal<Numerics.UInt8, byte, Op>,
					Numerics.UInt16 => &VectorsBinaryReal<Numerics.UInt16, ushort, Op>,
					Numerics.UInt32 => &VectorsBinaryReal<Numerics.UInt32, uint, Op>,
					Numerics.UInt64 => &VectorsBinaryReal<Numerics.UInt64, ulong, Op>,
					_ => null,
				};
				func(px, py, pz, length, &scalar);
			}
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool VectorsBinary<T, TS1, TS2, TS3, Op>(TS1 x, long strideX, TS2 y, long strideY, TS3 z, long strideZ, T scalar) where T : unmanaged, Numerics.INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>
		{
			if (!GetPointer(x, strideX, out T* px, out int lenx, out int incx))
				return false;
			if (!GetPointer(y, strideY, out T* py, out int leny, out int incy))
				return false;
			if (!GetPointer(z, strideZ, out T* pz, out int lenz, out int incz))
				return false;
			// shortcuts
			int length = Math.Min(Math.Min(lenx, leny), lenz);
			if (length == 0)
				return true;
			if (typeof(Op) == typeof(B_Divide) && px == py)
			{
				new Span<T>(py, length).Fill(T.One);
				return true;
			}
			if (typeof(Op) == typeof(B_Multiply) && px == py)
			{
				return PointWisePower(x, strideX, z, strideZ, T.One + T.One);
			}
			if ((typeof(Op) == typeof(B_Add) || typeof(Op) == typeof(B_AddScaled)) && px == py)
			{
				return Default.Scale(x, strideX, scalar + T.One);
			}
			// normal case
			return VectorsBinary<T, Op>(px, incx, py, incy, pz, incz, scalar, length);
		}

		public virtual partial bool GeneralVectorsBinary<T, TS1, TS2, TS3>(BinaryOperation op, TS1 x, long strideX, TS2 y, long strideY, TS3 z, long strideZ) where T : unmanaged, Numerics.INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>
		{
			return op switch
			{
				BinaryOperation.Add => VectorsBinary<T, TS1, TS2, TS3, B_Add>(x, strideX, y, strideY, z, strideZ, default),
				BinaryOperation.Multiply => VectorsBinary<T, TS1, TS2, TS3, B_Multiply>(x, strideX, y, strideY, z, strideZ, default),
				BinaryOperation.Divide => VectorsBinary<T, TS1, TS2, TS3, B_Divide>(x, strideX, y, strideY, z, strideZ, default),
				_ => false,
			};
		}

		public virtual partial bool Add<T, TS1, TS2>(T α, TS1 x, long strideX, TS2 y, long strideY) where T : unmanaged, Numerics.INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
		{
			if (α == T.Zero)
				return true;
			if (α == T.One)
				return VectorsBinary<T, TS1, TS2, TS2, B_Add>(x, strideX, y, strideY, y, strideY, default);
			else
				return VectorsBinary<T, TS1, TS2, TS2, B_AddScaled>(x, strideX, y, strideY, y, strideY, α);
		}
		#endregion


		#region cast
		#region Vector<T> widen
		// Instructions like PMOVSXDQ is not used,
		// since compared to the implementations of 'Vector.Narrow' or 'Vector.Widen', they have almost no improvements.
		// And 'Vector.Narrow' or 'Vector.Widen' of floating point types automatically utilizes instructions like CVTPS2PD.

		// Helper structures are not used since JIT may not optimize them thoroughly

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Vector<U> GenericNoWiden<T, U>(Vector<T> src) where T : unmanaged, Numerics.INumber<T> where U : unmanaged, Numerics.INumber<U>
		{
			var dst = *(Vector<U>*)&src;
			if (typeof(T) == typeof(uint) && typeof(U) == typeof(Numerics.Single))
			{
				var d = Vector.ConvertToSingle((Vector<uint>)src);
				dst = *(Vector<U>*)&d;
			}
			if (typeof(T) == typeof(int) && typeof(U) == typeof(Numerics.Single))
			{
				var d = Vector.ConvertToSingle((Vector<int>)src);
				dst = *(Vector<U>*)&d;
			}
			if (typeof(T) == typeof(ulong) && typeof(U) == typeof(double))
			{
				var d = Vector.ConvertToDouble((Vector<ulong>)src);
				dst = *(Vector<U>*)&d;
			}
			if (typeof(T) == typeof(long) && typeof(U) == typeof(double))
			{
				var d = Vector.ConvertToDouble((Vector<long>)src);
				dst = *(Vector<U>*)&d;
			}
			return dst;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void GenericWidenX2<T, U>(Vector<T> src, out Vector<U> dst1, out Vector<U> dst2) where T : unmanaged, Numerics.INumber<T> where U : unmanaged, Numerics.INumber<U>
		{
			dst1 = dst2 = default;
			if (typeof(T) == typeof(byte) && (typeof(U) == typeof(ushort) || typeof(U) == typeof(short)))
			{
				Vector.Widen((Vector<byte>)src, out var d1, out var d2);
				dst1 = *(Vector<U>*)&d1; dst2 = *(Vector<U>*)&d2;
			}
			if (typeof(T) == typeof(sbyte) && (typeof(U) == typeof(ushort) || typeof(U) == typeof(short)))
			{
				Vector.Widen((Vector<sbyte>)src, out var d1, out var d2);
				dst1 = *(Vector<U>*)&d1; dst2 = *(Vector<U>*)&d2;
			}
			if (typeof(T) == typeof(ushort))
			{
				Vector.Widen((Vector<ushort>)src, out var d1, out var d2);
				if (typeof(U) == typeof(uint) || typeof(U) == typeof(int))
				{
					dst1 = *(Vector<U>*)&d1; dst2 = *(Vector<U>*)&d2;
				}
				else if (typeof(U) == typeof(Numerics.Single))
				{
					var dd1 = Vector.ConvertToSingle(d1);
					var dd2 = Vector.ConvertToSingle(d2);
					dst1 = *(Vector<U>*)&dd1; dst2 = *(Vector<U>*)&dd2;
				}
			}
			if (typeof(T) == typeof(short))
			{
				Vector.Widen((Vector<short>)src, out var d1, out var d2);
				if (typeof(U) == typeof(uint) || typeof(U) == typeof(int))
				{
					dst1 = *(Vector<U>*)&d1; dst2 = *(Vector<U>*)&d2;
				}
				else if (typeof(U) == typeof(Numerics.Single))
				{
					var dd1 = Vector.ConvertToSingle(d1);
					var dd2 = Vector.ConvertToSingle(d2);
					dst1 = *(Vector<U>*)&dd1; dst2 = *(Vector<U>*)&dd2;
				}
			}
			if (typeof(T) == typeof(uint))
			{
				Vector.Widen((Vector<uint>)src, out var d1, out var d2);
				if (typeof(U) == typeof(ulong) || typeof(U) == typeof(long))
				{
					dst1 = *(Vector<U>*)&d1; dst2 = *(Vector<U>*)&d2;
				}
				else if (typeof(U) == typeof(double))
				{
					var dd1 = Vector.ConvertToDouble(d1);
					var dd2 = Vector.ConvertToDouble(d2);
					dst1 = *(Vector<U>*)&dd1; dst2 = *(Vector<U>*)&dd2;
				}
			}
			if (typeof(T) == typeof(int))
			{
				Vector.Widen((Vector<int>)src, out var d1, out var d2);
				if (typeof(U) == typeof(ulong) || typeof(U) == typeof(long))
				{
					dst1 = *(Vector<U>*)&d1; dst2 = *(Vector<U>*)&d2;
				}
				else if (typeof(U) == typeof(double))
				{
					var dd1 = Vector.ConvertToDouble(d1);
					var dd2 = Vector.ConvertToDouble(d2);
					dst1 = *(Vector<U>*)&dd1; dst2 = *(Vector<U>*)&dd2;
				}
			}
			if (typeof(T) == typeof(Numerics.Single) && typeof(U) == typeof(double))
			{
				Vector.Widen((Vector<float>)src, out var d1, out var d2);
				dst1 = *(Vector<U>*)&d1; dst2 = *(Vector<U>*)&d2;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void GenericWidenX4<T, U>(Vector<T> src, out Vector<U> dst1, out Vector<U> dst2, out Vector<U> dst3, out Vector<U> dst4) where T : unmanaged, Numerics.INumber<T> where U : unmanaged, Numerics.INumber<U>
		{
			dst1 = dst2 = dst3 = dst4 = default;
			if (typeof(T) == typeof(byte))
			{
				Vector.Widen((Vector<byte>)src, out var d1, out var d2);
				Vector.Widen(d1, out var d11, out var d12);
				Vector.Widen(d2, out var d21, out var d22);
				if (typeof(U) == typeof(uint) || typeof(U) == typeof(int))
				{
					dst1 = *(Vector<U>*)&d11; dst2 = *(Vector<U>*)&d12;
					dst3 = *(Vector<U>*)&d21; dst4 = *(Vector<U>*)&d22;
				}
				else if (typeof(U) == typeof(Numerics.Single))
				{
					var dd11 = Vector.ConvertToSingle(d11); var dd12 = Vector.ConvertToSingle(d12);
					var dd21 = Vector.ConvertToSingle(d11); var dd22 = Vector.ConvertToSingle(d22);
					dst1 = *(Vector<U>*)&dd11; dst2 = *(Vector<U>*)&dd12;
					dst3 = *(Vector<U>*)&dd21; dst4 = *(Vector<U>*)&dd22;
				}
			}
			if (typeof(T) == typeof(sbyte))
			{
				Vector.Widen((Vector<byte>)src, out var d1, out var d2);
				Vector.Widen(d1, out var d11, out var d12);
				Vector.Widen(d2, out var d21, out var d22);
				if (typeof(U) == typeof(uint) || typeof(U) == typeof(int))
				{
					dst1 = *(Vector<U>*)&d11; dst2 = *(Vector<U>*)&d12;
					dst3 = *(Vector<U>*)&d21; dst4 = *(Vector<U>*)&d22;
				}
				else if (typeof(U) == typeof(Numerics.Single))
				{
					var dd11 = Vector.ConvertToSingle(d11); var dd12 = Vector.ConvertToSingle(d12);
					var dd21 = Vector.ConvertToSingle(d11); var dd22 = Vector.ConvertToSingle(d22);
					dst1 = *(Vector<U>*)&dd11; dst2 = *(Vector<U>*)&dd12;
					dst3 = *(Vector<U>*)&dd21; dst4 = *(Vector<U>*)&dd22;
				}
			}
			if (typeof(T) == typeof(ushort))
			{
				Vector.Widen((Vector<ushort>)src, out var d1, out var d2);
				Vector.Widen(d1, out var d11, out var d12);
				Vector.Widen(d2, out var d21, out var d22);
				if (typeof(U) == typeof(ulong) || typeof(U) == typeof(long))
				{
					dst1 = *(Vector<U>*)&d11; dst2 = *(Vector<U>*)&d12;
					dst3 = *(Vector<U>*)&d21; dst4 = *(Vector<U>*)&d22;
				}
				else if (typeof(U) == typeof(double))
				{
					var dd11 = Vector.ConvertToDouble(d11); var dd12 = Vector.ConvertToDouble(d12);
					var dd21 = Vector.ConvertToDouble(d11); var dd22 = Vector.ConvertToDouble(d22);
					dst1 = *(Vector<U>*)&dd11; dst2 = *(Vector<U>*)&dd12;
					dst3 = *(Vector<U>*)&dd21; dst4 = *(Vector<U>*)&dd22;
				}
			}
			if (typeof(T) == typeof(short))
			{
				Vector.Widen((Vector<short>)src, out var d1, out var d2);
				Vector.Widen(d1, out var d11, out var d12);
				Vector.Widen(d2, out var d21, out var d22);
				if (typeof(U) == typeof(ulong) || typeof(U) == typeof(long))
				{
					dst1 = *(Vector<U>*)&d11; dst2 = *(Vector<U>*)&d12;
					dst3 = *(Vector<U>*)&d21; dst4 = *(Vector<U>*)&d22;
				}
				else if (typeof(U) == typeof(double))
				{
					var dd11 = Vector.ConvertToDouble(d11); var dd12 = Vector.ConvertToDouble(d12);
					var dd21 = Vector.ConvertToDouble(d11); var dd22 = Vector.ConvertToDouble(d22);
					dst1 = *(Vector<U>*)&dd11; dst2 = *(Vector<U>*)&dd12;
					dst3 = *(Vector<U>*)&dd21; dst4 = *(Vector<U>*)&dd22;
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void GenericWidenX8<T, U>(Vector<T> src, out Vector<U> dst1, out Vector<U> dst2, out Vector<U> dst3, out Vector<U> dst4, out Vector<U> dst5, out Vector<U> dst6, out Vector<U> dst7, out Vector<U> dst8) where T : unmanaged, Numerics.INumber<T> where U : unmanaged, Numerics.INumber<U>
		{
			dst1 = dst2 = dst3 = dst4 = dst5 = dst6 = dst7 = dst8 = default;
			if (typeof(T) == typeof(byte))
			{
				Vector.Widen((Vector<byte>)src, out var d1, out var d2);
				Vector.Widen(d1, out var dd1, out var dd2);
				Vector.Widen(d2, out var dd3, out var dd4);
				Vector.Widen(dd1, out var ddd1, out var ddd2);
				Vector.Widen(dd2, out var ddd3, out var ddd4);
				Vector.Widen(dd3, out var ddd5, out var ddd6);
				Vector.Widen(dd4, out var ddd7, out var ddd8);
				if (typeof(U) == typeof(ulong) || typeof(U) == typeof(long))
				{
					dst1 = *(Vector<U>*)&ddd1; dst2 = *(Vector<U>*)&ddd2;
					dst3 = *(Vector<U>*)&ddd3; dst4 = *(Vector<U>*)&ddd4;
					dst5 = *(Vector<U>*)&ddd5; dst6 = *(Vector<U>*)&ddd6;
					dst7 = *(Vector<U>*)&ddd7; dst8 = *(Vector<U>*)&ddd8;
				}
				else if (typeof(U) == typeof(double))
				{
					var fd1 = Vector.ConvertToDouble(ddd1); var fd2 = Vector.ConvertToDouble(ddd2);
					var fd3 = Vector.ConvertToDouble(ddd3); var fd4 = Vector.ConvertToDouble(ddd4);
					var fd5 = Vector.ConvertToDouble(ddd5); var fd6 = Vector.ConvertToDouble(ddd6);
					var fd7 = Vector.ConvertToDouble(ddd7); var fd8 = Vector.ConvertToDouble(ddd8);
					dst1 = *(Vector<U>*)&fd1; dst2 = *(Vector<U>*)&fd2;
					dst3 = *(Vector<U>*)&fd3; dst4 = *(Vector<U>*)&fd4;
					dst5 = *(Vector<U>*)&fd5; dst6 = *(Vector<U>*)&fd6;
					dst7 = *(Vector<U>*)&fd7; dst8 = *(Vector<U>*)&fd8;
				}
			}
			if (typeof(T) == typeof(sbyte))
			{
				Vector.Widen((Vector<sbyte>)src, out var d1, out var d2);
				Vector.Widen(d1, out var dd1, out var dd2);
				Vector.Widen(d2, out var dd3, out var dd4);
				Vector.Widen(dd1, out var ddd1, out var ddd2);
				Vector.Widen(dd2, out var ddd3, out var ddd4);
				Vector.Widen(dd3, out var ddd5, out var ddd6);
				Vector.Widen(dd4, out var ddd7, out var ddd8);
				if (typeof(U) == typeof(ulong) || typeof(U) == typeof(long))
				{
					dst1 = *(Vector<U>*)&ddd1; dst2 = *(Vector<U>*)&ddd2;
					dst3 = *(Vector<U>*)&ddd3; dst4 = *(Vector<U>*)&ddd4;
					dst5 = *(Vector<U>*)&ddd5; dst6 = *(Vector<U>*)&ddd6;
					dst7 = *(Vector<U>*)&ddd7; dst8 = *(Vector<U>*)&ddd8;
				}
				else if (typeof(U) == typeof(double))
				{
					var fd1 = Vector.ConvertToDouble(ddd1); var fd2 = Vector.ConvertToDouble(ddd2);
					var fd3 = Vector.ConvertToDouble(ddd3); var fd4 = Vector.ConvertToDouble(ddd4);
					var fd5 = Vector.ConvertToDouble(ddd5); var fd6 = Vector.ConvertToDouble(ddd6);
					var fd7 = Vector.ConvertToDouble(ddd7); var fd8 = Vector.ConvertToDouble(ddd8);
					dst1 = *(Vector<U>*)&fd1; dst2 = *(Vector<U>*)&fd2;
					dst3 = *(Vector<U>*)&fd3; dst4 = *(Vector<U>*)&fd4;
					dst5 = *(Vector<U>*)&fd5; dst6 = *(Vector<U>*)&fd6;
					dst7 = *(Vector<U>*)&fd7; dst8 = *(Vector<U>*)&fd8;
				}
			}
		}
		#endregion

		#region Vector<T> narrow
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Vector<U> GenericNarrowX2<T, U>(Vector<T> src1, Vector<T> src2) where T : unmanaged, Numerics.INumber<T> where U : unmanaged, Numerics.INumber<U>
		{
			if (typeof(T) == typeof(ushort) && (typeof(U) == typeof(byte) || typeof(U) == typeof(sbyte)))
			{
				var d = Vector.Narrow((Vector<ushort>)src1, (Vector<ushort>)src2);
				return *(Vector<U>*)&d;
			}
			if (typeof(T) == typeof(short) && (typeof(U) == typeof(byte) || typeof(U) == typeof(sbyte)))
			{
				var d = Vector.Narrow((Vector<short>)src1, (Vector<short>)src2);
				return *(Vector<U>*)&d;
			}
			if (typeof(T) == typeof(uint) && (typeof(U) == typeof(ushort) || typeof(U) == typeof(short)))
			{
				var d = Vector.Narrow((Vector<uint>)src1, (Vector<uint>)src2);
				return *(Vector<U>*)&d;
			}
			if (typeof(T) == typeof(int) && (typeof(U) == typeof(ushort) || typeof(U) == typeof(short)))
			{
				var d = Vector.Narrow((Vector<int>)src1, (Vector<int>)src2);
				return *(Vector<U>*)&d;
			}
			if (typeof(T) == typeof(Numerics.Single) && (typeof(U) == typeof(ushort) || typeof(U) == typeof(short)))
			{
				var s1 = Vector.ConvertToInt32((Vector<float>)src1);
				var s2 = Vector.ConvertToInt32((Vector<float>)src2);
				var d = Vector.Narrow(s1, s2);
				return *(Vector<U>*)&d;
			}
			if (typeof(T) == typeof(ulong) && (typeof(U) == typeof(uint) || typeof(U) == typeof(int)))
			{
				var d = Vector.Narrow((Vector<ulong>)src1, (Vector<ulong>)src2);
				return *(Vector<U>*)&d;
			}
			if (typeof(T) == typeof(long) && (typeof(U) == typeof(uint) || typeof(U) == typeof(int)))
			{
				var d = Vector.Narrow((Vector<long>)src1, (Vector<long>)src2);
				return *(Vector<U>*)&d;
			}
			if (typeof(T) == typeof(double))
			{
				if (typeof(U) == typeof(uint) || typeof(U) == typeof(int))
				{
					var s1 = Vector.ConvertToInt64((Vector<double>)src1);
					var s2 = Vector.ConvertToInt64((Vector<double>)src2);
					var d = Vector.Narrow(s1, s2);
					return *(Vector<U>*)&d;
				}
				if (typeof(U) == typeof(Numerics.Single))
				{
					var d = Vector.Narrow((Vector<double>)src1, (Vector<double>)src2);
					return *(Vector<U>*)&d;
				}
			}
			return default;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Vector<U> GenericNarrowX4<T, U>(Vector<T> src1, Vector<T> src2, Vector<T> src3, Vector<T> src4) where T : unmanaged, Numerics.INumber<T> where U : unmanaged, Numerics.INumber<U>
		{
			if (typeof(T) == typeof(uint) && (typeof(U) == typeof(byte) || typeof(U) == typeof(sbyte)))
			{
				var d1 = Vector.Narrow((Vector<uint>)src1, (Vector<uint>)src2);
				var d2 = Vector.Narrow((Vector<uint>)src3, (Vector<uint>)src4);
				var d = Vector.Narrow(d1, d2);
				return *(Vector<U>*)&d;
			}
			if (typeof(T) == typeof(int) && (typeof(U) == typeof(byte) || typeof(U) == typeof(sbyte)))
			{
				var d1 = Vector.Narrow((Vector<int>)src1, (Vector<int>)src2);
				var d2 = Vector.Narrow((Vector<int>)src3, (Vector<int>)src4);
				var d = Vector.Narrow(d1, d2);
				return *(Vector<U>*)&d;
			}
			if (typeof(T) == typeof(Numerics.Single) && (typeof(U) == typeof(byte) || typeof(U) == typeof(sbyte)))
			{
				var s1 = Vector.ConvertToInt32((Vector<float>)src1);
				var s2 = Vector.ConvertToInt32((Vector<float>)src2);
				var s3 = Vector.ConvertToInt32((Vector<float>)src3);
				var s4 = Vector.ConvertToInt32((Vector<float>)src4);
				var d1 = Vector.Narrow(s1, s2);
				var d2 = Vector.Narrow(s3, s4);
				var d = Vector.Narrow(s1, s2);
				return *(Vector<U>*)&d;
			}
			if (typeof(T) == typeof(ulong) && (typeof(U) == typeof(ushort) || typeof(U) == typeof(short)))
			{
				var d1 = Vector.Narrow((Vector<ulong>)src1, (Vector<ulong>)src2);
				var d2 = Vector.Narrow((Vector<ulong>)src3, (Vector<ulong>)src4);
				var d = Vector.Narrow(d1, d2);
				return *(Vector<U>*)&d;
			}
			if (typeof(T) == typeof(long) && (typeof(U) == typeof(ushort) || typeof(U) == typeof(short)))
			{
				var d1 = Vector.Narrow((Vector<long>)src1, (Vector<long>)src2);
				var d2 = Vector.Narrow((Vector<long>)src3, (Vector<long>)src4);
				var d = Vector.Narrow(d1, d2);
				return *(Vector<U>*)&d;
			}
			if (typeof(T) == typeof(double) && (typeof(U) == typeof(ushort) || typeof(U) == typeof(short)))
			{
				var s1 = Vector.ConvertToInt64((Vector<double>)src1);
				var s2 = Vector.ConvertToInt64((Vector<double>)src2);
				var s3 = Vector.ConvertToInt64((Vector<double>)src3);
				var s4 = Vector.ConvertToInt64((Vector<double>)src4);
				var d1 = Vector.Narrow(s1, s2);
				var d2 = Vector.Narrow(s3, s4);
				var d = Vector.Narrow(s1, s2);
				return *(Vector<U>*)&d;
			}
			return default;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Vector<U> GenericNarrowX8<T, U>(Vector<T> src1, Vector<T> src2, Vector<T> src3, Vector<T> src4, Vector<T> src5, Vector<T> src6, Vector<T> src7, Vector<T> src8) where T : unmanaged, Numerics.INumber<T> where U : unmanaged, Numerics.INumber<U>
		{
			if (typeof(T) == typeof(ulong) && (typeof(U) == typeof(byte) || typeof(U) == typeof(sbyte)))
			{
				var d1 = Vector.Narrow((Vector<ulong>)src1, (Vector<ulong>)src2);
				var d2 = Vector.Narrow((Vector<ulong>)src3, (Vector<ulong>)src4);
				var d3 = Vector.Narrow((Vector<ulong>)src5, (Vector<ulong>)src6);
				var d4 = Vector.Narrow((Vector<ulong>)src7, (Vector<ulong>)src8);
				var dd1 = Vector.Narrow(d1, d2);
				var dd2 = Vector.Narrow(d3, d4);
				var d = Vector.Narrow(dd1, dd2);
				return *(Vector<U>*)&d;
			}
			if (typeof(T) == typeof(long) && (typeof(U) == typeof(byte) || typeof(U) == typeof(sbyte)))
			{
				var d1 = Vector.Narrow((Vector<long>)src1, (Vector<long>)src2);
				var d2 = Vector.Narrow((Vector<long>)src3, (Vector<long>)src4);
				var d3 = Vector.Narrow((Vector<long>)src5, (Vector<long>)src6);
				var d4 = Vector.Narrow((Vector<long>)src7, (Vector<long>)src8);
				var dd1 = Vector.Narrow(d1, d2);
				var dd2 = Vector.Narrow(d3, d4);
				var d = Vector.Narrow(dd1, dd2);
				return *(Vector<U>*)&d;
			}
			if (typeof(T) == typeof(double) && (typeof(U) == typeof(byte) || typeof(U) == typeof(sbyte)))
			{
				var s1 = Vector.ConvertToInt64((Vector<double>)src1);
				var s2 = Vector.ConvertToInt64((Vector<double>)src2);
				var s3 = Vector.ConvertToInt64((Vector<double>)src3);
				var s4 = Vector.ConvertToInt64((Vector<double>)src4);
				var s5 = Vector.ConvertToInt64((Vector<double>)src5);
				var s6 = Vector.ConvertToInt64((Vector<double>)src6);
				var s7 = Vector.ConvertToInt64((Vector<double>)src7);
				var s8 = Vector.ConvertToInt64((Vector<double>)src8);
				var d1 = Vector.Narrow(s1, s2);
				var d2 = Vector.Narrow(s3, s4);
				var d3 = Vector.Narrow(s5, s6);
				var d4 = Vector.Narrow(s7, s8);
				var dd1 = Vector.Narrow(d1, d2);
				var dd2 = Vector.Narrow(d3, d4);
				var d = Vector.Narrow(dd1, dd2);
				return *(Vector<U>*)&d;
			}
			return default;
		}
		#endregion

		#region other helper
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void VectorToComplex<T>(Vector<T> zeros, Vector<T> input, out Vector<T> output1, out Vector<T> output2) where T : unmanaged, Numerics.INumber<T>
		{
			switch (sizeof(T))
			{
				case sizeof(byte):
					Vector256<byte> temp1 = Avx2.Permute4x64(input.AsVector256().AsInt64(), 0b11_01_10_00).AsByte();
					output1 = Avx2.UnpackLow(temp1, zeros.AsVector256().AsByte()).As<byte, T>().AsVector();
					output2 = Avx2.UnpackHigh(temp1, zeros.AsVector256().AsByte()).As<byte, T>().AsVector();
					break;
				case sizeof(short):
					Vector256<short> temp2 = Avx2.Permute4x64(input.AsVector256().AsInt64(), 0b11_01_10_00).AsInt16();
					output1 = Avx2.UnpackLow(temp2, zeros.AsVector256().AsInt16()).As<short, T>().AsVector();
					output2 = Avx2.UnpackHigh(temp2, zeros.AsVector256().AsInt16()).As<short, T>().AsVector();
					break;
				case sizeof(int):
					Vector256<int> temp4 = Avx2.Permute4x64(input.AsVector256().AsInt64(), 0b11_01_10_00).AsInt32();
					output1 = Avx2.UnpackLow(temp4, zeros.AsVector256().AsInt32()).As<int, T>().AsVector();
					output2 = Avx2.UnpackHigh(temp4, zeros.AsVector256().AsInt32()).As<int, T>().AsVector();
					break;
				case sizeof(long):
					Vector256<long> temp8 = Avx2.Permute4x64(input.AsVector256().AsInt64(), 0b11_01_10_00).AsInt64();
					output1 = Avx2.UnpackLow(temp8, zeros.AsVector256().AsInt64()).As<long, T>().AsVector();
					output2 = Avx2.UnpackHigh(temp8, zeros.AsVector256().AsInt64()).As<long, T>().AsVector();
					break;
				default:
					output1 = output2 = default;
					break;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void VectorToComplex<T>(Vector<T> zeros, Vector<T> input1, Vector<T> input2, out Vector<T> output1, out Vector<T> output2, out Vector<T> output3, out Vector<T> output4) where T : unmanaged, Numerics.INumber<T>
		{
			VectorToComplex(zeros, input1, out output1, out output2);
			VectorToComplex(zeros, input2, out output3, out output4);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void VectorToComplex<T>(Vector<T> zeros, Vector<T> input1, Vector<T> input2, Vector<T> input3, Vector<T> input4, out Vector<T> output1, out Vector<T> output2, out Vector<T> output3, out Vector<T> output4, out Vector<T> output5, out Vector<T> output6, out Vector<T> output7, out Vector<T> output8) where T : unmanaged, Numerics.INumber<T>
		{
			VectorToComplex(zeros, input1, input2, out output1, out output2, out output3, out output4);
			VectorToComplex(zeros, input3, input4, out output5, out output6, out output7, out output8);
		}
		#endregion
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void VectorCastManaged<T, U>(T* x, int incx, U* y, int incy, int length) where T : unmanaged, Numerics.INumber<T> where U : unmanaged, Numerics.INumber<U>
		{
			for (int i = 0, ix = 0, iy = 0; i < length; i++, ix += incx, iy += incy)
			{
				y[i] = x[i].As<T, U>();
			}
		}

		/*
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void VectorCastReal<T, U, ToComp>(T* src, U* dst, int length) where T : unmanaged, Numerics.INumber<T> where U : unmanaged, Numerics.INumber<U>
		{
			bool toComp = typeof(ToComp) == typeof(bool);
			int lengthLeft = length, offset = 0;
			Vector<U> zeros = Vector<U>.Zero;
			if (sizeof(U) >= sizeof(T))
			{   // widen
				while (lengthLeft >= Vector<T>.Count)
				{
					Vector<T> currentSrc = LoadVector(src + offset);
					if (sizeof(U) == sizeof(T))
					{
						var dst1 = GenericNoWiden<T, U>(currentSrc);
						if (!toComp)
						{
							StoreVector(dst1, dst + offset);
						}
						else
						{
							VectorToComplex(zeros, dst1, out var comp1, out var comp2);
							StoreVector(comp1, comp2, dst + offset);
						}
					}
					if (sizeof(U) == sizeof(T) * 2)
					{
						GenericWidenX2<T, U>(currentSrc, out var dst1, out var dst2);
						if (!toComp)
						{
							StoreVector(dst1, dst2, dst + offset);
						}
						else
						{
							VectorToComplex(zeros, dst1, dst2, out var comp1, out var comp2, out var comp3, out var comp4);
							StoreVector(comp1, comp2, comp3, comp4, dst + offset);
						}
					}
					if (sizeof(U) == sizeof(T) * 4)
					{
						GenericWidenX4<T, U>(currentSrc, out var dst1, out var dst2, out var dst3, out var dst4);
						if (!toComp)
						{
							StoreVector(dst1, dst2, dst3, dst4, dst + offset);
						}
						else
						{
							VectorToComplex(zeros, dst1, dst2, dst3, dst4, out var comp1, out var comp2, out var comp3, out var comp4, out var comp5, out var comp6, out var comp7, out var comp8);
							StoreVector(comp1, comp2, comp3, comp4, comp5, comp6, comp7, comp8, dst + offset);
						}
					}
					if (sizeof(U) == sizeof(T) * 8)
					{
						GenericWidenX8<T, U>(currentSrc, out var dst1, out var dst2, out var dst3, out var dst4, out var dst5, out var dst6, out var dst7, out var dst8);
						if (!toComp)
						{
							StoreVector(dst1, dst2, dst3, dst4, dst5, dst6, dst7, dst8, dst + offset);
						}
						else
						{
							VectorToComplex(zeros, dst1, dst2, dst3, dst4, out var comp1, out var comp2, out var comp3, out var comp4, out var comp5, out var comp6, out var comp7, out var comp8);
							VectorToComplex(zeros, dst5, dst6, dst7, dst8, out var comp9, out var compA, out var compB, out var compC, out var compD, out var compE, out var compF, out var comp0);
							StoreVector(comp1, comp2, comp3, comp4, comp5, comp6, comp7, comp8, dst + offset);
							StoreVector(comp9, compA, compB, compC, compD, compE, compF, comp0, dst + offset + Vector<U>.Count * 8);
						}
					}
					lengthLeft -= Vector<T>.Count;
					offset += Vector<T>.Count;
				}
			}
			else
			{   // narrow
				while (lengthLeft >= Vector<T>.Count)
				{
					Vector<U> currentDst;
					if (sizeof(T) == sizeof(U) * 2)
					{
						LoadVector(src + offset, out var src1, out var src2);
						currentDst = GenericNarrowX2<T, U>(src1, src2);
					}
					else if (sizeof(T) == sizeof(U) * 4)
					{
						LoadVector(src + offset, out var src1, out var src2, out var src3, out var src4);
						currentDst = GenericNarrowX4<T, U>(src1, src2, src3, src4);
					}
					else ////if (sizeof(T) == sizeof(U) * 8)
					{
						LoadVector(src + offset, out var src1, out var src2, out var src3, out var src4, out var src5, out var src6, out var src7, out var src8);
						currentDst = GenericNarrowX8<T, U>(src1, src2, src3, src4, src5, src6, src7, src8);
					}
					if (!toComp)
					{
						StoreVector(currentDst, dst + offset);
					}
					else
					{
						VectorToComplex(zeros, currentDst, out var comp1, out var comp2);
						StoreVector(comp1, comp2, dst + offset);
					}
					lengthLeft -= Vector<U>.Count;
					offset += Vector<U>.Count;
				}
			}
			// modify left
			if (lengthLeft > 0)
			{
				VectorCastManaged(src + offset, 1, dst + offset, 1, lengthLeft);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void VectorCastReal2Real<T, U>(T* src, U* dst, int length) where T : unmanaged, Numerics.INumber<T> where U : unmanaged, Numerics.INumber<U>
			=> VectorCastReal<T, U, byte>(src, dst, length);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void VectorCastReal2Comp<T, U>(T* src, U* dst, int length) where T : unmanaged, Numerics.INumber<T> where U : unmanaged, Numerics.INumber<U>
			=> VectorCastReal<T, U, bool>(src, dst, length);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void VectorCastComplex<U>(Complex<Numerics.Single>* src, U* dst, int length) where U : unmanaged, Numerics.INumber<U>
		{
			int lengthLeft = length, offset = 0;
			if (sizeof(U) >= sizeof(Numerics.Single))
			{
				while (lengthLeft >= Vector256<float>.Count) // Vector256<Complex<Numerics.Single>>.Count * 2
				{
					Vector256<float> abs = ComplexSquareAbsOrder(src + offset);
					abs = Avx.Sqrt(abs);
					if (sizeof(U) == sizeof(Numerics.Single))
					{
						var t = GenericNoWiden<float, U>(abs.AsVector());
						StoreVector(t, dst);
					}
					if (sizeof(U) == sizeof(Numerics.Single) * 2)
					{
						GenericWidenX2<float, U>(abs.AsVector(), out var dst1, out var dst2);
						StoreVector(dst1, dst + offset);
						StoreVector(dst2, dst + offset + Vector256<U>.Count);
					}
					lengthLeft -= Vector256<float>.Count;
					offset += Vector256<float>.Count;
				}
			}
			else
			{
				while (lengthLeft >= Vector256<U>.Count)
				{
					Vector<U> dstNow;
					if (sizeof(U) == sizeof(Numerics.Single) / 2)
					{
						Vector256<float> abs1 = ComplexSquareAbsOrder(src + offset);
						Vector256<float> abs2 = ComplexSquareAbsOrder(src + offset + Vector256<float>.Count / 2);
						abs1 = Avx.Sqrt(abs1); abs2 = Avx.Sqrt(abs2);
						dstNow = GenericNarrowX2<float, U>(abs1.AsVector(), abs2.AsVector());
					}
					else ////if (sizeof(U) == sizeof(Numerics.Single) / 4)
					{
						Vector256<float> abs1 = ComplexSquareAbsOrder(src + offset);
						Vector256<float> abs2 = ComplexSquareAbsOrder(src + offset + Vector256<float>.Count / 2);
						Vector256<float> abs3 = ComplexSquareAbsOrder(src + offset + Vector256<float>.Count);
						Vector256<float> abs4 = ComplexSquareAbsOrder(src + offset + Vector256<float>.Count * 3 / 2);
						abs1 = Avx.Sqrt(abs1); abs2 = Avx.Sqrt(abs2); abs3 = Avx.Sqrt(abs3); abs4 = Avx.Sqrt(abs4);
						dstNow = GenericNarrowX4<float, U>(abs1.AsVector(), abs2.AsVector(), abs3.AsVector(), abs4.AsVector());
					}
					StoreVector(dstNow, dst + offset);
					lengthLeft -= Vector256<U>.Count;
					offset += Vector256<U>.Count;
				}
			}
			// modify left
			if (lengthLeft > 0)
			{
				VectorCastManaged(src + offset, 1, dst + offset, 1, lengthLeft);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void VectorCastComplex<U>(Complex<Numerics.Double>* src, U* dst, int length) where U : unmanaged, Numerics.INumber<U>
		{
			int lengthLeft = length, offset = 0;
			if (sizeof(U) == sizeof(double))
			{
				while (lengthLeft >= Vector256<double>.Count) // Vector256<Complex<Numerics.Double>>.Count * 2
				{
					Vector256<double> abs = ComplexSquareAbsOrder(src + offset);
					abs = Avx.Sqrt(abs);
					var dstNow = GenericNoWiden<double, U>(abs.AsVector());
					StoreVector(dstNow, dst);
					lengthLeft -= Vector256<double>.Count;
					offset += Vector256<double>.Count;
				}
			}
			else
			{
				while (lengthLeft >= Vector256<U>.Count)
				{
					Vector<U> dstNow;
					if (sizeof(U) == sizeof(double) / 2)
					{
						Vector256<double> abs1 = ComplexSquareAbsOrder(src + offset);
						Vector256<double> abs2 = ComplexSquareAbsOrder(src + offset + Vector256<double>.Count / 2);
						abs1 = Avx.Sqrt(abs1); abs2 = Avx.Sqrt(abs2);
						dstNow = GenericNarrowX2<double, U>(abs1.AsVector(), abs2.AsVector());
					}
					else if (sizeof(U) == sizeof(double) / 4)
					{
						Vector256<double> abs1 = ComplexSquareAbsOrder(src + offset);
						Vector256<double> abs2 = ComplexSquareAbsOrder(src + offset + Vector256<double>.Count / 2);
						Vector256<double> abs3 = ComplexSquareAbsOrder(src + offset + Vector256<double>.Count);
						Vector256<double> abs4 = ComplexSquareAbsOrder(src + offset + Vector256<double>.Count * 3 / 2);
						abs1 = Avx.Sqrt(abs1); abs2 = Avx.Sqrt(abs2); abs3 = Avx.Sqrt(abs3); abs4 = Avx.Sqrt(abs4);
						dstNow = GenericNarrowX4<double, U>(abs1.AsVector(), abs2.AsVector(), abs3.AsVector(), abs4.AsVector());
					}
					else //// if (sizeof(U) == sizeof(double) / 8)
					{
						Vector256<double> abs1 = ComplexSquareAbsOrder(src + offset);
						Vector256<double> abs2 = ComplexSquareAbsOrder(src + offset + Vector256<double>.Count / 2);
						Vector256<double> abs3 = ComplexSquareAbsOrder(src + offset + Vector256<double>.Count);
						Vector256<double> abs4 = ComplexSquareAbsOrder(src + offset + Vector256<double>.Count * 3 / 2);
						Vector256<double> abs5 = ComplexSquareAbsOrder(src + offset + Vector256<double>.Count * 2);
						Vector256<double> abs6 = ComplexSquareAbsOrder(src + offset + Vector256<double>.Count * 5 / 2);
						Vector256<double> abs7 = ComplexSquareAbsOrder(src + offset + Vector256<double>.Count * 3);
						Vector256<double> abs8 = ComplexSquareAbsOrder(src + offset + Vector256<double>.Count * 7 / 2);
						abs1 = Avx.Sqrt(abs1); abs2 = Avx.Sqrt(abs2); abs3 = Avx.Sqrt(abs3); abs4 = Avx.Sqrt(abs4);
						abs5 = Avx.Sqrt(abs5); abs6 = Avx.Sqrt(abs6); abs7 = Avx.Sqrt(abs7); abs8 = Avx.Sqrt(abs8);
						dstNow = GenericNarrowX8<double, U>(abs1.AsVector(), abs2.AsVector(), abs3.AsVector(), abs4.AsVector(), abs5.AsVector(), abs6.AsVector(), abs7.AsVector(), abs8.AsVector());
					}
					StoreVector(dstNow, dst + offset);
					lengthLeft -= Vector256<U>.Count;
					offset += Vector256<U>.Count;
				}
			}
			// modify left
			if (lengthLeft > 0)
			{
				VectorCastManaged(src + offset, 1, dst + offset, 1, lengthLeft);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool PointWiseCast<TIn, TOut>(TIn* px, int incx, TOut* py, int incy, int length) where TIn : unmanaged, Numerics.INumber<TIn> where TOut : unmanaged, Numerics.INumber<TOut>
		{
			// shortcuts
			if (typeof(TIn) == typeof(TOut) && px == py)
				return true;
			else if (typeof(TIn) != typeof(TOut) && px == py)
				throw new InvalidOperationException();
			else if (typeof(TIn) == typeof(TOut) && px != py)
			{
				Unsafe.CopyBlockUnaligned(py, px, (uint)(length * sizeof(TIn)));
				return true;
			}
			// normal case
			if (incx != 1 || incy != 1 || !Vector.IsHardwareAccelerated || length <= (Vector<byte>.Count / Math.Max(sizeof(TIn), sizeof(TOut)) * 4))
			{   // no SIMD or too short
				VectorCastManaged(px, incx, py, incy, length);
			}
			else if (!TIn.IsComplexType && !TOut.IsComplexType)
			{
				VectorCastReal2Real(px, py, length);
			}
			else if (TIn.IsComplexType && !TOut.IsComplexType)
			{   // need complex abs
				if (TIn.Type.IsInteger() || !Avx.IsSupported)
				{   // no AVX's HorizontalAdd and Unpack (Vector<T> has not corresponding implementation yet)
					VectorCastManaged(px, 1, py, 1, length);
				}
				else if (typeof(TIn) == typeof(Complex<Numerics.Single>) || typeof(TIn) == typeof(Complex<Numerics.Single>))
				{
					VectorCastComplex((Complex<Numerics.Single>*)px, py, length);
				}
				else
				{
					VectorCastComplex((Complex<Numerics.Double>*)px, py, length);
				}
			}
			else if (!TIn.IsComplexType && TOut.IsComplexType)
			{
				if (!Avx2.IsSupported)
				{   // no AVX2's Permute4x64 and Unpack (Vector<T> has not corresponding implementation yet)
					VectorCastManaged(px, 1, py, 1, length);
				}
				if (typeof(TOut) == typeof(ComplexInteger<byte>))
					VectorCastReal2Comp(px, (byte*)py, length);
				if (typeof(TOut) == typeof(ComplexInteger<ushort>))
					VectorCastReal2Comp(px, (ushort*)py, length);
				if (typeof(TOut) == typeof(ComplexInteger<uint>))
					VectorCastReal2Comp(px, (uint*)py, length);
				if (typeof(TOut) == typeof(ComplexInteger<ulong>))
					VectorCastReal2Comp(px, (ulong*)py, length);
				if (typeof(TOut) == typeof(ComplexInteger<sbyte>))
					VectorCastReal2Comp(px, (sbyte*)py, length);
				if (typeof(TOut) == typeof(ComplexInteger<short>))
					VectorCastReal2Comp(px, (short*)py, length);
				if (typeof(TOut) == typeof(ComplexInteger<int>))
					VectorCastReal2Comp(px, (int*)py, length);
				if (typeof(TOut) == typeof(ComplexInteger<long>))
					VectorCastReal2Comp(px, (long*)py, length);
				if (typeof(TOut) == typeof(Complex<Numerics.Single>))
					VectorCastReal2Comp(px, (Numerics.Single*)py, length);
				if (typeof(TOut) == typeof(Complex<Numerics.Double>))
					VectorCastReal2Comp(px, (double*)py, length);
			}
			else
			{   // both complex
				VectorCastReal2Real(px, py, length * 2);
			}
			return true;
		}
		*/

		public virtual partial bool GeneralVectorsCast<TIn, TOut, TSIn, TSOut>(TSIn source, long strideSource, TSOut destination, long strideDestination) where TIn : unmanaged, Numerics.INumber<TIn> where TOut : unmanaged, Numerics.INumber<TOut> where TSIn : class, IStorage<TIn, TSIn> where TSOut : class, IStorage<TOut, TSOut>
		{
			if (!GetPointer(source, strideSource, out TIn* px, out int lenx, out int incx))
				return false;
			if (!GetPointer(destination, strideDestination, out TOut* py, out int leny, out int incy))
				return false;
			VectorCastManaged(px, incx, py, incy, Math.Min(lenx, leny));
			return true;
		}
		#endregion
	}
}
