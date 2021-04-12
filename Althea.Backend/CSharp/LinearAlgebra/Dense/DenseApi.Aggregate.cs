using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

using Althea.LinearAlgebra.Dense;
using Althea.NativeTypes;


namespace Althea.Backend.CSharp.LinearAlgebra
{
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释
	public partial class DenseApi : AbstractApi
	{
		#region (absolute) sum product
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe bool VectorAbsSumProdManaged<T, Test>(T* x, int length, out T aggregateNoAbs, out double aggregateAbs) where T : unmanaged
		{
			aggregateNoAbs = default; aggregateAbs = 0;
			bool doSum = typeof(Test) == typeof(int) || typeof(Test) == typeof(long);
			bool doAbs = typeof(Test) == typeof(int) || typeof(Test) == typeof(uint);
			if (doAbs)
			{
				aggregateAbs = doSum ? 0 : 1;
				for (int i = 0; i < length; i++)
				{
					double v = x[i].NativeAbsolute();
					if (doSum)
						aggregateAbs += v;
					else
						aggregateAbs *= v;
				}
			}
			else
			{
				T result = doSum ? default : Const<T>.One;
				for (int i = 0; i < length; i++)
				{
					T v = x[i];
					// some frequent type speedups
					if (doSum)
					{
						result = result.NativeAdd(v);
					}
					else
					{
						result = result.NativeMultiply(v);
					}
				}
			}
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe void VectorAbsSumProdReal<T, Test>(T* x, int length, out T aggregateNoAbs, out double aggregateAbs) where T : unmanaged
		{
			aggregateNoAbs = default; aggregateAbs = 0;
			bool doSum = typeof(Test) == typeof(int) || typeof(Test) == typeof(long);
			bool doAbs = typeof(Test) == typeof(int) || typeof(Test) == typeof(uint);
			// initial
			Vector<T> aggregate = LoadVector(x);
			if (doAbs)
				aggregate = Vector.Abs(aggregate);
			// loop
			int lengthLeft = length - Vector<T>.Count, offset = Vector<T>.Count;
			while (lengthLeft >= Vector<T>.Count)
			{
				Vector<T> current = LoadVector(x + offset);
				if (doAbs)
					current = Vector.Abs(current);
				if (doSum)
					aggregate += current;
				else
					aggregate *= current;
				lengthLeft -= Vector<T>.Count;
				offset += Vector<T>.Count;
			}
			// reduce left
			if (lengthLeft > 0)
			{
				VectorAbsSumProdManaged<T, Test>(x + offset, lengthLeft, out aggregateNoAbs, out aggregateAbs);
			}
			else if (!doSum)
			{
				aggregateNoAbs = Const<T>.One; aggregateAbs = 1;
			}
			// return
			if (doSum)
			{
				double result = Vector.Dot(aggregate, Vector<T>.One).ToDouble();
				aggregateAbs += result;
			}
			else
			{
				T result = aggregate[0];
				for (int i = 1; i < Vector<T>.Count; i++)
				{
					result = result.NativeMultiply(aggregate[i]);
				}
				aggregateNoAbs = result.NativeMultiply(aggregateNoAbs);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe void VectorAbsSumProdCompexSingle<Test>(ComplexSingle* x, int length, out ComplexSingle aggregateNoAbs, out double aggregateAbs)
		{
			aggregateNoAbs = default; aggregateAbs = 0;
			bool doSum = typeof(Test) == typeof(int) || typeof(Test) == typeof(long);
			bool doAbs = typeof(Test) == typeof(int) || typeof(Test) == typeof(uint);
			if (doAbs)
			{
				// initialize
				Vector256<float> aggregate = ComplexSquareAbsNoOrder(x);
				if (doSum)
					aggregate = Avx.Sqrt(aggregate);
				// loop
				int lengthLeft = length - Vector256<float>.Count, offset = Vector256<float>.Count;
				while (lengthLeft >= Vector256<float>.Count) // Vector256<ComplexSingle>.Count * 2
				{
					Vector256<float> squares = ComplexSquareAbsNoOrder(x + offset);
					if (doSum)
					{
						squares = Avx.Sqrt(squares);
						aggregate = Avx.Add(aggregate, squares);
					}
					else
					{
						aggregate = Avx.Multiply(aggregate, squares);
					}
					lengthLeft -= Vector256<float>.Count;
					offset += Vector256<float>.Count;
				}
				// reduce main
				float result = ((float*)&aggregate)[0];
				for (int i = 1; i < Vector256<float>.Count; i++)
				{
					float v = ((float*)&aggregate)[i];
					if (doSum)
					{
						result += v;
					}
					else
					{
						result *= v;
					}
				}
				if (!doSum)
				{   // sqrt
					result = MathF.Sqrt(result);
				}
				// reduce left
				if (lengthLeft > 0)
				{
					for (; offset < length; offset++)
					{
						float v = x[offset].Abs();
						if (doSum)
						{
							result += v;
						}
						else
						{
							result *= v;
						}
					}
				}
				aggregateAbs = result;
			}
			else
			{
				// initialize
				Vector256<float> aggregate1 = LoadVector256<float>(x), aggregate2 = LoadVector256<float>(x + Vector256<float>.Count / 2);
				// loop
				int lengthLeft = length - Vector256<float>.Count, offset = Vector256<float>.Count;
				while (lengthLeft >= Vector256<float>.Count) // Vector256<ComplexSingle>.Count * 2
				{
					Vector256<float> current1 = LoadVector256<float>(x + offset);
					Vector256<float> current2 = LoadVector256<float>(x + offset + Vector256<float>.Count / 2);
					if (doSum)
					{
						aggregate1 = Avx.Add(aggregate1, current1);
						aggregate2 = Avx.Add(aggregate2, current2);
					}
					else
					{   // TODO: improve performance by using 'aggregate1 = reals' and 'aggregate2 = imaginaries'
						ComplexMultiply<byte>(aggregate1, aggregate2, current1, current2, out aggregate1, out aggregate2);
					}
					lengthLeft -= Vector256<float>.Count;
					offset += Vector256<float>.Count;
				}
				// reduce main
				ComplexSingle result = ((ComplexSingle*)&aggregate1)[0];
				for (int i = 1; i < Vector256<float>.Count / 2; i++)
				{
					ComplexSingle v = ((ComplexSingle*)&aggregate1)[i];
					if (doSum)
					{
						result += v;
					}
					else
					{
						result *= v;
					}
				}
				for (int i = 0; i < Vector256<float>.Count / 2; i++)
				{
					ComplexSingle v = ((ComplexSingle*)&aggregate2)[i];
					if (doSum)
					{
						result += v;
					}
					else
					{
						result *= v;
					}
				}
				// reduce left
				if (lengthLeft > 0)
				{
					for (; offset < length; offset++)
					{
						ComplexSingle v = x[offset];
						if (doSum)
						{
							result += v;
						}
						else
						{
							result *= v;
						}
					}
				}
				aggregateNoAbs = result;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe void VectorAbsSumProdCompexDouble<Test>(ComplexDouble* x, int length, out ComplexDouble aggregateNoAbs, out double aggregateAbs)
		{
			aggregateNoAbs = default; aggregateAbs = 0;
			bool doSum = typeof(Test) == typeof(int) || typeof(Test) == typeof(long);
			bool doAbs = typeof(Test) == typeof(int) || typeof(Test) == typeof(uint);
			if (doAbs)
			{
				// initialize
				Vector256<double> aggregate = ComplexSquareAbsNoOrder(x);
				if (doSum)
					aggregate = Avx.Sqrt(aggregate);
				// loop
				int lengthLeft = length - Vector256<double>.Count, offset = Vector256<double>.Count;
				while (lengthLeft >= Vector256<double>.Count) // Vector256<ComplexDouble>.Count * 2
				{
					Vector256<double> squares = ComplexSquareAbsNoOrder(x + offset);
					if (doSum)
					{
						squares = Avx.Sqrt(squares);
						aggregate = Avx.Add(aggregate, squares);
					}
					else
					{
						aggregate = Avx.Multiply(aggregate, squares);
					}
					lengthLeft -= Vector256<double>.Count;
					offset += Vector256<double>.Count;
				}
				// reduce main
				double result = ((double*)&aggregate)[0];
				for (int i = 1; i < Vector256<double>.Count; i++)
				{
					double v = ((double*)&aggregate)[i];
					if (doSum)
					{
						result += v;
					}
					else
					{
						result *= v;
					}
				}
				if (!doSum)
				{   // sqrt
					result = Math.Sqrt(result);
				}
				// reduce left
				if (lengthLeft > 0)
				{
					for (; offset < length; offset++)
					{
						double v = x[offset].Abs();
						if (doSum)
						{
							result += v;
						}
						else
						{
							result *= v;
						}
					}
				}
				aggregateAbs = result;
			}
			else
			{
				// initialize
				Vector256<double> aggregate1 = LoadVector256<double>(x), aggregate2 = LoadVector256<double>(x + Vector256<double>.Count / 2);
				// loop
				int lengthLeft = length - Vector256<double>.Count, offset = Vector256<double>.Count;
				while (lengthLeft >= Vector256<double>.Count) // Vector256<ComplexDouble>.Count * 2
				{
					Vector256<double> current1 = LoadVector256<double>(x + offset);
					Vector256<double> current2 = LoadVector256<double>(x + offset + Vector256<double>.Count / 2);
					if (doSum)
					{
						aggregate1 = Avx.Add(aggregate1, current1);
						aggregate2 = Avx.Add(aggregate2, current2);
					}
					else
					{   // TODO: improve performance by using 'aggregate1 = reals' and 'aggregate2 = imaginaries'
						ComplexMultiply<byte>(aggregate1, aggregate2, current1, current2, out aggregate1, out aggregate2);
					}
					lengthLeft -= Vector256<double>.Count;
					offset += Vector256<double>.Count;
				}
				// reduce main
				ComplexDouble result = ((ComplexDouble*)&aggregate1)[0];
				for (int i = 1; i < Vector256<double>.Count / 2; i++)
				{
					ComplexDouble v = ((ComplexDouble*)&aggregate1)[i];
					if (doSum)
					{
						result += v;
					}
					else
					{
						result *= v;
					}
				}
				for (int i = 0; i < Vector256<double>.Count / 2; i++)
				{
					ComplexDouble v = ((ComplexDouble*)&aggregate2)[i];
					if (doSum)
					{
						result += v;
					}
					else
					{
						result *= v;
					}
				}
				// reduce left
				if (lengthLeft > 0)
				{
					for (; offset < length; offset++)
					{
						ComplexDouble v = x[offset];
						if (doSum)
						{
							result += v;
						}
						else
						{
							result *= v;
						}
					}
				}
				aggregateNoAbs = result;
			}
		}

		//// Test == int, uint, long, ulong   for   AbsSum, AbsProd, Sum, Prod
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static unsafe bool AbsSumProd<T, Test>(Storage<T> x, out T aggregateNoAbs, out double aggregateAbs) where T : unmanaged
		{
			aggregateNoAbs = default; aggregateAbs = 0;
			if (!GetPointer(x, out T* px, out int length))
				return false;
			if (length == 0)
			{
				if (typeof(Test) == typeof(uint) || typeof(Test) == typeof(ulong))
				{
					aggregateAbs = 1; aggregateNoAbs = Const<T>.One;
				}
				return true;
			}
			if (length == 1)
			{
				aggregateNoAbs = px[0]; aggregateAbs = aggregateNoAbs.ToDouble();
				return true;
			}
			if (!Vector.IsHardwareAccelerated || length <= (Vector<byte>.Count / sizeof(T) * 4)) // no SIMD or too short
				return VectorAbsSumProdManaged<T, Test>(px, length, out aggregateNoAbs, out aggregateAbs);

			if (Const<T>.IsComplex)
			{
				if (Const<T>.IsIntegralType || !Avx.IsSupported) // no AVX's HorizontalAdd and Unpack (Vector<T> has not corresponding implementation yet)
					return VectorAbsSumProdManaged<T, Test>(px, length, out aggregateNoAbs, out aggregateAbs);
				if (typeof(T) == typeof(Complex<float>) || typeof(T) == typeof(ComplexSingle))
				{
					VectorAbsSumProdCompexSingle<Test>((ComplexSingle*)px, length, out var temp, out aggregateAbs);
					aggregateNoAbs = *(T*)&temp;
				}
				else // double
				{
					VectorAbsSumProdCompexDouble<Test>((ComplexDouble*)px, length, out var temp, out aggregateAbs);
					aggregateNoAbs = *(T*)&temp;
				}
			}
			else
			{
				VectorAbsSumProdReal<T, Test>(px, length, out aggregateNoAbs, out aggregateAbs);
			}
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static bool AbsoluteValueSum<T>(Storage<T> x, out double sum) where T : unmanaged
		{
			return AbsSumProd<T, int>(x, out _, out sum);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static bool AbsoluteValueProduct<T>(Storage<T> x, out double product) where T : unmanaged
		{
			return AbsSumProd<T, uint>(x, out _, out product);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static bool AggregateSum<T>(Storage<T> x, out T sum) where T : unmanaged
		{
			return AbsSumProd<T, long>(x, out sum, out _);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static bool AggregateProduct<T>(Storage<T> x, out T product) where T : unmanaged
		{
			return AbsSumProd<T, ulong>(x, out product, out _);
		}
		#endregion


		#region inner
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe T VectorInnerManaged<T, Dot, Conj>(T* x, T* y, int length) where T : unmanaged
		{
			T result = default;
			bool doDot = typeof(Dot) == typeof(bool);
			bool doCon = typeof(Conj) == typeof(bool);

			for (int i = 0; i < length; i++)
			{
				T a = x[i], b;
				// float type FMA acceleration
				if (typeof(T) == typeof(float))
				{
					float vv;
					if (doDot)
						vv = MathF.FusedMultiplyAdd(*(float*)&a, *(float*)&b, *(float*)&result);
					else
						vv = MathF.FusedMultiplyAdd(*(float*)&a, *(float*)&a, *(float*)&result);
					result = *(T*)&vv;
					continue;
				}
				if (typeof(T) == typeof(double))
				{
					double vv;
					if (doDot)
						vv = Math.FusedMultiplyAdd(*(double*)&a, *(double*)&b, *(double*)&result);
					else
						vv = Math.FusedMultiplyAdd(*(double*)&a, *(double*)&a, *(double*)&result);
					result = *(T*)&vv;
					continue;
				}
				if (typeof(T) == typeof(ComplexSingle) || typeof(T) == typeof(Complex<float>))
				{
					ComplexSingle vv;
					if (doDot && doCon)
						vv = (*(ComplexSingle*)&result).AddConjugateProduct(*(ComplexSingle*)&a, *(ComplexSingle*)&b);
					else if (doDot && !doCon)
						vv = (*(ComplexSingle*)&result).AddProduct(*(ComplexSingle*)&a, *(ComplexSingle*)&b);
					else
						vv = (*(ComplexSingle*)&result).AddSquareAbs(*(ComplexSingle*)&a);
					result = *(T*)&vv;
					continue;
				}
				if (typeof(T) == typeof(ComplexDouble) || typeof(T) == typeof(Complex<double>))
				{
					ComplexDouble vv;
					if (doDot && doCon)
						vv = (*(ComplexDouble*)&result).AddConjugateProduct(*(ComplexDouble*)&a, *(ComplexDouble*)&b);
					else if (doDot && !doCon)
						vv = (*(ComplexDouble*)&result).AddProduct(*(ComplexDouble*)&a, *(ComplexDouble*)&b);
					else
						vv = (*(ComplexDouble*)&result).AddSquareAbs(*(ComplexDouble*)&a);
					result = *(T*)&vv;
					continue;
				}
				// normal case
				if (doDot)
				{
					b = y[i];
					if (doCon)
						result = result.NativeAdd(a.NativeConjugate().NativeMultiply(b));
					else
						result = result.NativeAdd(a.NativeMultiply(b));
				}
				else
				{
					result = result.NativeAdd(a.NativeMultiply(a));
				}
			}
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe T VectorInnerReal<T, Dot>(T* x, T* y, int length) where T : unmanaged
		{
			bool doDot = typeof(Dot) == typeof(bool);
			// reduce to Vector<T>.Count sums
			Vector<T> sum = Vector<T>.Zero;
			int lengthLeft = length, offset = 0;
			while (lengthLeft >= Vector<T>.Count)
			{
				Vector<T> a = LoadVector(x + offset), b = LoadVector(y + offset);
				if (!doDot)
					b = a;
				if (typeof(T) == typeof(float) && Fma.IsSupported)
				{
					if (Vector<T>.Count == Vector256<T>.Count)
					{
						sum = Fma.MultiplyAdd(a.AsVector256().AsSingle(), b.AsVector256().AsSingle(), sum.AsVector256().AsSingle()).As<float, T>().AsVector();
						goto CONTINUE;
					}
					else if(Vector<T>.Count == Vector128<T>.Count)
					{
						sum = Fma.MultiplyAdd(a.AsVector128().AsSingle(), b.AsVector128().AsSingle(), sum.AsVector128().AsSingle()).As<float, T>().AsVector();
						goto CONTINUE;
					}
				}
				if (typeof(T) == typeof(double) && Fma.IsSupported)
				{
					if (Vector<T>.Count == Vector256<T>.Count)
					{
						sum = Fma.MultiplyAdd(a.AsVector256().AsDouble(), b.AsVector256().AsDouble(), sum.AsVector256().AsDouble()).As<double, T>().AsVector();
						goto CONTINUE;
					}
					else if (Vector<T>.Count == Vector128<T>.Count)
					{
						sum = Fma.MultiplyAdd(a.AsVector128().AsDouble(), b.AsVector128().AsDouble(), sum.AsVector128().AsDouble()).As<double, T>().AsVector();
						goto CONTINUE;
					}
				}
				// no FMA
				sum += a * b;
			CONTINUE:
				lengthLeft -= Vector<T>.Count;
				offset += Vector<T>.Count;
			}
			// reduce left
			T dotLeft = default;
			if (lengthLeft > 0)
			{
				Vector<T> leftA = Vector<T>.Zero, leftB = Vector<T>.Zero;
				// the following two lines shall be unrolled by JIT at runtime
				Unsafe.CopyBlock(&leftA, x + offset, (uint)(lengthLeft * sizeof(T)));
				Unsafe.CopyBlock(&leftB, y + offset, (uint)(lengthLeft * sizeof(T)));
				dotLeft = Vector.Dot(leftA, leftB);
				// this implementation has some performance loss compare to the direct dot
				// but it is suitable for all generic type T that Vector<T> supports
			}
			// this implementation has some performance loss, same reason as above
			T dotMain = Vector.Dot(sum, Vector<T>.One);
			// return
			return dotMain.NativeAdd(dotLeft);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe ComplexSingle VectorInnerCompexSingle<Dot, Conj>(ComplexSingle* x, ComplexSingle* y, int length)
		{
			ComplexSingle innerResult;
			bool doDot = typeof(Dot) == typeof(bool);
			bool doConj = typeof(Conj) == typeof(bool);
			if (!doDot)
			{
				// initialize
				Vector256<float> aggregate = ComplexSquareAbsNoOrder(x);
				// loop
				int lengthLeft = length - Vector256<float>.Count, offset = Vector256<float>.Count;
				while (lengthLeft >= Vector256<float>.Count) // Vector256<ComplexSingle>.Count * 2
				{
					Vector256<float> squares = ComplexSquareAbsNoOrder(x + offset);
					aggregate = Avx.Add(aggregate, squares);
					lengthLeft -= Vector256<float>.Count;
					offset += Vector256<float>.Count;
				}
				// reduce main
				float result = ((float*)&aggregate)[0];
				for (int i = 1; i < Vector256<float>.Count; i++)
				{
					float v = ((float*)&aggregate)[i];
					result += v;
				}
				// reduce left
				if (lengthLeft > 0)
				{
					for (; offset < length; offset++)
					{
						float v = x[offset].SquareAbs();
						result += v;
					}
				}
				innerResult = result;
			}
			else
			{
				// initialize
				Vector256<float> aggregateReal = Vector<float>.Zero.AsVector256(), aggregateImag = Vector<float>.Zero.AsVector256();
				ComplexMultiplyAdd<Conj>(x, y, ref aggregateReal, ref aggregateImag);
				// loop
				int lengthLeft = length - Vector256<float>.Count, offset = Vector256<float>.Count;
				while (lengthLeft >= Vector256<float>.Count) // Vector256<ComplexSingle>.Count * 2
				{
					ComplexMultiplyAdd<Conj>(x + offset, y + offset, ref aggregateReal, ref aggregateImag);
					lengthLeft -= Vector256<float>.Count;
					offset += Vector256<float>.Count;
				}
				// reduce main
				float* reals = (float*)&aggregateReal, imags = (float*)&aggregateImag;
				ComplexSingle result = new(reals[0], imags[0]);
				for (int i = 1; i < Vector256<float>.Count; i++)
				{
					ComplexSingle v = new(reals[i], imags[i]);
					result += v;
				}
				// reduce left
				if (lengthLeft > 0)
				{
					for (; offset < length; offset++)
					{
						ComplexSingle v1 = x[offset], v2 = y[offset];
						result = result.AddProduct(v1, v2);
					}
				}
				innerResult = result;
			}
			return innerResult;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe ComplexDouble VectorInnerComplexDouble<Dot, Conj>(ComplexDouble* x, ComplexDouble* y, int length)
		{
			ComplexDouble innerResult;
			bool doDot = typeof(Dot) == typeof(bool);
			if (!doDot)
			{
				// initialize
				Vector256<double> aggregate = ComplexSquareAbsNoOrder(x);
				// loop
				int lengthLeft = length - Vector256<double>.Count, offset = Vector256<double>.Count;
				while (lengthLeft >= Vector256<double>.Count) // Vector256<ComplexDouble>.Count * 2
				{
					Vector256<double> squares = ComplexSquareAbsNoOrder(x + offset);
					aggregate = Avx.Add(aggregate, squares);
					lengthLeft -= Vector256<double>.Count;
					offset += Vector256<double>.Count;
				}
				// reduce main
				double result = ((double*)&aggregate)[0];
				for (int i = 1; i < Vector256<double>.Count; i++)
				{
					double v = ((double*)&aggregate)[i];
					result += v;
				}
				// reduce left
				if (lengthLeft > 0)
				{
					for (; offset < length; offset++)
					{
						double v = x[offset].SquareAbs();
						result += v;
					}
				}
				innerResult = result;
			}
			else
			{
				// initialize
				Vector256<double> aggregateReal = Vector<double>.Zero.AsVector256(), aggregateImag = Vector<double>.Zero.AsVector256();
				ComplexMultiplyAdd<Conj>(x, y, ref aggregateReal, ref aggregateImag);
				// loop
				int lengthLeft = length - Vector256<double>.Count, offset = Vector256<double>.Count;
				while (lengthLeft >= Vector256<double>.Count) // Vector256<ComplexSingle>.Count * 2
				{
					ComplexMultiplyAdd<Conj>(x + offset, y + offset, ref aggregateReal, ref aggregateImag);
					lengthLeft -= Vector256<double>.Count;
					offset += Vector256<double>.Count;
				}
				// reduce main
				double* reals = (double*)&aggregateReal, imags = (double*)&aggregateImag;
				ComplexDouble result = new(reals[0], imags[0]);
				for (int i = 1; i < Vector256<double>.Count; i++)
				{
					ComplexDouble v = new(reals[i], imags[i]);
					result += v;
				}
				// reduce left
				if (lengthLeft > 0)
				{
					for (; offset < length; offset++)
					{
						ComplexDouble v1 = x[offset], v2 = y[offset];
						result += v1 * v2;
					}
				}
				innerResult = result;
			}
			return innerResult;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static unsafe bool Inner<T, Dot>(bool conjX, Storage<T> x, Storage<T> y, out T dot) where T : unmanaged
		{
			dot = default;
			if (!GetPointer(x, out T* px, out int lenx))
				return false;
			if (!GetPointer(y, out T* py, out int leny))
				return false;
			int length = Math.Min(lenx, leny);
			if (length == 0)
				return true;

			if (!Vector.IsHardwareAccelerated || length <= (Vector<byte>.Count / sizeof(T) * 4) ||
				(Const<T>.IsComplex && (Const<T>.IsIntegralType || !Avx.IsSupported)))
			{ // no SIMD or too short
			  // no AVX's HorizontalAdd and Unpack (Vector<T> has not corresponding implementation yet)
				if (conjX)
					dot = VectorInnerManaged<T, Dot, bool>(px, py, length);
				else
					dot = VectorInnerManaged<T, Dot, byte>(px, py, length);
				return true;
			}

			if (Const<T>.IsComplex)
			{
				if (typeof(T) == typeof(Complex<float>) || typeof(T) == typeof(ComplexSingle))
				{
					var xx = (ComplexSingle*)px; var yy = (ComplexSingle*)py;
					ComplexSingle temp;
					if (conjX)
						VectorInnerCompexSingle<Dot, bool>(xx, yy, length);
					else
						VectorInnerCompexSingle<Dot, byte>(xx, yy, length);
					dot = *(T*)&temp;
				}
				else // double
				{
					var xx = (ComplexDouble*)px; var yy = (ComplexDouble*)py;
					ComplexDouble temp;
					if (conjX)
						VectorInnerComplexDouble<Dot, bool>(xx, yy, length);
					else
						VectorInnerComplexDouble<Dot, byte>(xx, yy, length);
					dot = *(T*)&temp;
				}
			}
			else
			{
				dot = VectorInnerReal<T, Dot>(px, py, length);
			}
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static unsafe bool Dot<T>(bool conjX, Storage<T> x, Storage<T> y, out T dot) where T : unmanaged
		{
			return Inner<T, bool>(conjX, x, y, out dot);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static bool Norm<T>(Storage<T> x, out double norm) where T : unmanaged
		{
			norm = 0;
			if (!Inner<T, byte>(conjX: true, x, x, out T dot))
				return false;
			norm = Math.Sqrt(dot.ToDouble());
			return true;
		}
		#endregion


		#region partial sum product
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe void VectorParSumProdManaged<T, Sum, HasPre>(T* x, T* y, int length) where T : unmanaged
		{
			bool doSum = typeof(Sum) == typeof(bool);
			bool hasPre = typeof(HasPre) == typeof(bool);

			T result;
			if (hasPre)
				result = y[-1];
			else
				result = doSum ? default : Const<T>.One;
			for (int i = 0; i < length; i++)
			{
				T v = x[i];
				// some frequent type speedups
				if (doSum)
				{
					y[i] = result.NativeAdd(v);
				}
				else
				{
					y[i] = result.NativeMultiply(v);
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static unsafe bool ParSumProd<T, Sum>(Storage<T> x, Storage<T> y, bool inclusive) where T : unmanaged
		{
			if (!GetPointer(x, out T* px, out int lenx))
				return false;
			if (!GetPointer(y, out T* py, out int leny))
				return false;
			int length = Math.Min(lenx, leny);
			// shortcuts
			if (length == 0)
				return true;
			if (inclusive)
			{
				py[0] = px[0];
			}
			else
			{
				if (typeof(Sum) == typeof(bool))
					py[0] = default;
				else
					py[0] = Const<T>.One;
			}
			if (length == 1)
				return true;
			// normal case
			if (!inclusive)
			{
				py++; length--;
			}
			VectorParSumProdManaged<T, Sum, byte>(px, py, length);
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static bool PartialProduct<T>(Storage<T> x, Storage<T> y, bool inclusive) where T : unmanaged
		{
			return ParSumProd<T, byte>(x, y, inclusive);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static bool PartialSum<T>(Storage<T> x, Storage<T> y, bool inclusive) where T : unmanaged
		{
			return ParSumProd<T, bool>(x, y, inclusive);
		}
		#endregion
	}
#pragma warning restore CS1591 // 缺少对公共可见类型或成员的 XML 注释
}
