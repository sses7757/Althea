using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

using Althea.LinearAlgebra.Dense;
using Althea.Linq;
using Althea.NativeTypes;


namespace Althea.Backend.CSharp.LinearAlgebra
{
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释
	public partial class DenseApi : AbstractApi
	{
		#region equals
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static unsafe bool PointWiseEquals<T>(Storage<T> x, Storage<T> y, out bool equals) where T : unmanaged
		{
			equals = false;
			if (x.Length != y.Length)
				return true;
			if (!GetPointer(x, out T* px, out int length))
				return false;
			if (!GetPointer(y, out T* py, out _))
				return false;
			if (px == py)
			{
				equals = true; return true;
			}

			if (Const<T>.IsIntegralType)
			{
				equals = sizeof(T) switch
				{
					sizeof(byte) => new ReadOnlySpan<byte>(px, length).SequenceEqual(new(py, length)),
					sizeof(short) => new ReadOnlySpan<short>(px, length).SequenceEqual(new(py, length)),
					sizeof(int) => new ReadOnlySpan<int>(px, length).SequenceEqual(new(py, length)),
					sizeof(long) => new ReadOnlySpan<long>(px, length).SequenceEqual(new(py, length)),
					sizeof(long) * 2 => new ReadOnlySpan<long>(px, length * 2).SequenceEqual(new(py, length * 2)),
					_ => false,
				};
			}
			else
			{
				equals = sizeof(T) switch
				{
					sizeof(float) => new ReadOnlySpan<float>(px, length).SequenceEqual(new(py, length)),
					sizeof(double) when Const<T>.IsComplex => new ReadOnlySpan<float>(px, length * 2).SequenceEqual(new(py, length * 2)),
					sizeof(double) when !Const<T>.IsComplex => new ReadOnlySpan<double>(px, length).SequenceEqual(new(py, length)),
					sizeof(double) * 2 => new ReadOnlySpan<double>(px, length * 2).SequenceEqual(new(py, length * 2)),
					_ => false,
				};
			}
			return true;
		}
		#endregion


		#region add multiply divide
		private struct B_Multiply { }
		private struct B_Divide { }
		private struct B_Add { }
		private struct B_AddScaled { }
		private enum BinaryModify
		{
			Multiply,
			Divide,
			Add,
			AddScaled
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe void VectorsBinaryManaged<T, Op>(T* x, T* y, int length, T scalar) where T : unmanaged
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

			for (int i = 0; i < length; i++)
			{
				T a = x[i], b = y[i];
				T v;
				// floating point FMA accelerate
				if (typeof(T) == typeof(float) && op == BinaryModify.AddScaled)
				{
					float temp = MathF.FusedMultiplyAdd(*(float*)&scalar, *(float*)&b, *(float*)&a);
					v = *(T*)&temp;
				}
				else if (typeof(T) == typeof(double) && op == BinaryModify.AddScaled)
				{
					double temp = Math.FusedMultiplyAdd(*(double*)&scalar, *(double*)&b, *(double*)&a);
					v = *(T*)&temp;
				}
				else if ((typeof(T) == typeof(ComplexSingle) || typeof(T) == typeof(Complex<float>)) && op == BinaryModify.AddScaled)
				{
					ComplexSingle temp = (*(ComplexSingle*)&a).AddProduct(*(ComplexSingle*)&scalar, *(ComplexSingle*)&b);
					v = *(T*)&temp;
				}
				else if ((typeof(T) == typeof(ComplexDouble) || typeof(T) == typeof(Complex<double>)) && op == BinaryModify.AddScaled)
				{
					ComplexDouble temp = (*(ComplexDouble*)&a).AddProduct(*(ComplexDouble*)&scalar, *(ComplexDouble*)&b);
					v = *(T*)&temp;
				}
				// otherwise
				else
				{
					v = op switch
					{
						BinaryModify.Multiply => a.NativeMultiply(b),
						BinaryModify.Divide => a.NativeDivide(b),
						BinaryModify.Add => a.NativeAdd(b),
						BinaryModify.AddScaled => a.NativeAdd(b.NativeMultiply(scalar)),
						_ => default,
					};
				}
				x[i] = v;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe void VectorsBinaryReal<T, Op>(T* x, T* y, int length, T scalar) where T : unmanaged
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
			Vector256<T> scalars = new Vector<T>(scalar).AsVector256();
			int lengthLeft = length, offset = 0;
			while (lengthLeft >= Vector<T>.Count)
			{
				Vector<T> currentX = LoadVector(x + offset);
				Vector<T> currentY = LoadVector(y + offset);
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
						if (typeof(T) == typeof(float) && Vector<T>.Count == Vector256<T>.Count && Fma.IsSupported)
						{
							currentX = Fma.MultiplyAdd(currentY.AsVector256().AsSingle(), scalars.AsSingle(), currentX.AsVector256().AsSingle()).As<float, T>().AsVector();
						}
						if (typeof(T) == typeof(double) && Vector<T>.Count == Vector256<T>.Count && Fma.IsSupported)
						{
							currentX = Fma.MultiplyAdd(currentY.AsVector256().AsDouble(), scalars.AsDouble(), currentX.AsVector256().AsDouble()).As<double, T>().AsVector();
						}
						else
						{
							currentX += currentY * scalar;
						}
						break;
					default:
						break;
				}
				StoreVector(currentX, x + offset);
				lengthLeft -= Vector<T>.Count;
				offset += Vector<T>.Count;
			}
			// modify left
			if (lengthLeft > 0)
			{
				VectorsBinaryManaged<T, Op>(x + offset, y + offset, lengthLeft, scalar);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe void VectorsBinaryComplexSingle<Op>(ComplexSingle* x, ComplexSingle* y, int length, ComplexSingle scalar)
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
			Vector256<float> scalarReals = new Vector<float>(scalar.Real).AsVector256();
			Vector256<float> scalarImags = new Vector<float>(scalar.Imag).AsVector256();
			int lengthLeft = length, offset = 0;
			while (lengthLeft >= Vector<float>.Count)
			{
				Vector256<float> currentX1 = LoadVector256<float>(x + offset), currentX2 = LoadVector256<float>(x + offset + Vector<float>.Count / 2);
				Vector256<float> currentY1 = LoadVector256<float>(y + offset), currentY2 = LoadVector256<float>(y + offset + Vector<float>.Count / 2);
				switch (op)
				{
					case BinaryModify.Multiply:
						ComplexMultiply<byte>(currentX1, currentX2, currentY1, currentY2, out currentX1, out currentX2);
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
				StoreVector256(currentX1, x + offset);
				StoreVector256(currentX2, x + offset + Vector256<float>.Count / 2);
				lengthLeft -= Vector256<float>.Count;
				offset += Vector256<float>.Count;
			}
			// modify left
			if (lengthLeft > 0)
			{
				VectorsBinaryManaged<ComplexSingle, Op>(x + offset, y + offset, lengthLeft, scalar);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe void VectorsBinaryComplexDouble<Op>(ComplexDouble* x, ComplexDouble* y, int length, ComplexDouble scalar)
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
			Vector256<double> scalarImags = new Vector<double>(scalar.Imag).AsVector256();
			int lengthLeft = length, offset = 0;
			while (lengthLeft >= Vector<double>.Count)
			{
				Vector256<double> currentX1 = LoadVector256<double>(x + offset), currentX2 = LoadVector256<double>(x + offset + Vector<double>.Count / 2);
				Vector256<double> currentY1 = LoadVector256<double>(y + offset), currentY2 = LoadVector256<double>(y + offset + Vector<double>.Count / 2);
				switch (op)
				{
					case BinaryModify.Multiply:
						ComplexMultiply<byte>(currentX1, currentX2, currentY1, currentY2, out currentX1, out currentX2);
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
				StoreVector256(currentX1, x + offset);
				StoreVector256(currentX2, x + offset + Vector256<double>.Count / 2);
				lengthLeft -= Vector256<double>.Count;
				offset += Vector256<double>.Count;
			}
			// modify left
			if (lengthLeft > 0)
			{
				VectorsBinaryManaged<ComplexDouble, Op>(x + offset, y + offset, lengthLeft, scalar);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static unsafe bool VectorsBinary<T, Op>(Storage<T> x, Storage<T> y, T scalar) where T : unmanaged
		{
			if (!GetPointer(x, out T* px, out int lenx))
				return false;
			if (!GetPointer(y, out T* py, out int leny))
				return false;
			// shortcuts
			int length = Math.Min(lenx, leny);
			if (length == 0)
				return true;
			if (typeof(Op) == typeof(B_Divide) && px == py)
			{
				new Span<T>(py, length).Fill(Const<T>.One);
				return true;
			}
			if (typeof(Op) == typeof(B_Multiply) && px == py)
			{
				return PointWisePower(x, 2);
			}
			if ((typeof(Op) == typeof(B_Add) || typeof(Op) == typeof(B_AddScaled)) && px == py)
			{
				return Scale(x, scalar.NativeAdd(Const<T>.One));
			}
			// normal case
			if (!Vector.IsHardwareAccelerated || length <= (Vector<byte>.Count / sizeof(T) * 4))
			{   // no SIMD or too short
				VectorsBinaryManaged<T, Op>(px, py, length, scalar);
			}
			else if (Const<T>.IsComplex)
			{
				if (Const<T>.IsIntegralType || !Avx.IsSupported)
				{   // no AVX's HorizontalAdd and Unpack (Vector<T> has not corresponding implementation yet)
					VectorsBinaryManaged<T, Op>(px, py, length, scalar);
				}
				else if (typeof(T) == typeof(float))
				{
					VectorsBinaryComplexSingle<Op>((ComplexSingle*)px, (ComplexSingle*)py, length, *(ComplexSingle*)&scalar);
				}
				else // double
				{
					VectorsBinaryComplexDouble<Op>((ComplexDouble*)px, (ComplexDouble*)py, length, *(ComplexDouble*)&scalar);
				}
			}
			else
			{
				VectorsBinaryReal<T, Op>(px, py, length, scalar);
			}
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static unsafe bool PointWiseDivide<T>(Storage<T> x, Storage<T> y) where T : unmanaged
		{
			return VectorsBinary<T, B_Divide>(x, y, default);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static unsafe bool PointWiseMultiply<T>(Storage<T> x, Storage<T> y) where T : unmanaged
		{
			return VectorsBinary<T, B_Multiply>(x, y, default);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static unsafe bool VectorGeneralAdd<T>(T α, Storage<T> x, Storage<T> y) where T : unmanaged
		{
			if (α.IsZero())
				return true;
			if (α.IsOne())
				return VectorsBinary<T, B_Add>(y, x, default);
			else
				return VectorsBinary<T, B_AddScaled>(y, x, α);
		}
		#endregion


		#region cast
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static unsafe bool PointWiseCast<T, TOut>(Storage<T> x, Storage<TOut> y) where T : unmanaged where TOut : unmanaged
		{
			if (!GetPointer(x, out T* px, out int lenx))
				return false;
			if (!GetPointer(y, out TOut* py, out int leny))
				return false;
			int length = Math.Min(lenx, leny);
			// shortcuts
			if (typeof(T) == typeof(TOut) && px == py)
				return true;
			else if (typeof(T) != typeof(TOut) && px == py)
				throw new InvalidOperationException();
			else if (typeof(T) == typeof(TOut) && px != py)
			{
				Unsafe.CopyBlock(px, py, (uint)(length * sizeof(T)));
				return true;
			}
			// normal case

		}
		#endregion
	}
#pragma warning restore CS1591 // 缺少对公共可见类型或成员的 XML 注释
}
