using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

using Althea.Helpers;
using Althea.LinearAlgebra.Dense;
using Althea.Linq;
using Althea.NativeTypes;


namespace Althea.Backend.CSharp.LinearAlgebra
{
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释
	public partial class Api : AbstractApi
	{

		#region index APIs
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe bool GetPointerIndexType<T>(Storage<T> s, out T* pointer, out int length) where T : unmanaged, INumber<T>
		{
			if (Const<T>.IsIntegralType)
			{
				pointer = default; length = 0;
				throw new TypeMismatchException(typeof(T), TypeMismatchException.MismatchReason.NotInteger);
			}
			return GetPointer(s, out pointer, out length);
		}

		#region bound
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe int VectorBound<T, Lower>(T* x, int length, T value) where T : unmanaged, INumber<T>
		{
			bool lower = typeof(Lower) == typeof(bool);
			Vector<T> values = new(value);
			Vector<T> current;
			int lengthLeft = length, offset = 0;
			bool found = false;
			while (lengthLeft >= Vector<T>.Count)
			{
				current = LoadVector(x + offset);
				if ((lower && Vector.GreaterThanOrEqualAny(current, values)) ||
					(!lower && Vector.GreaterThanAny(current, values)))
				{
					found = true;
					break;
				}
				lengthLeft -= Vector<T>.Count;
				offset += Vector<T>.Count;
			}
			if (found || lengthLeft > 0)
			{
				int len = found ? Vector<T>.Count : lengthLeft;
				int find = VectorBoundManaged<T, Lower>(x + offset, len, value);
				if (found)
				{
					return find + offset;
				}
				if (find < 0)
					return -1;
				if (find >= len)
					return length;
				return find + offset;
			}
			else
			{
				return lower ? -1 : length;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe int VectorBoundManaged<T, Lower>(T* x, int length, T value) where T : unmanaged, INumber<T>
		{
			bool lower = typeof(Lower) == typeof(bool);
			for (int i = 0; i < length; i++)
			{
				T current = x[i];
				if ((lower && current.NativeGreaterThanOrEqual(value)) ||
					(!lower && current.NativeGreaterThan(value)))
				{
					return i;
				}
			}
			// not found
			return lower ? -1 : length;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static unsafe bool IndexBound<T>(Storage<T> array, T value, bool lowerBound, out long index) where T : unmanaged, INumber<T>
		{
			index = -1;
			if (!GetPointerIndexType(array, out T* x, out int length))
				return false;
			if (length == 0)
				return true;

			if (Vector.IsHardwareAccelerated && length > Vector<T>.Count * 4)
			{
				if (lowerBound)
					index = VectorBound<T, bool>(x, length, value);
				else
					index = VectorBound<T, byte>(x, length, value);
			}
			else
			{
				if (lowerBound)
					index = VectorBoundManaged<T, bool>(x, length, value);
				else
					index = VectorBoundManaged<T, byte>(x, length, value);
			}
			return true;
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe void VectorAllBoundsManaged<T, U, Lower>(T* x, int length, T start, T end, U* y) where T : unmanaged, INumber<T> where U : unmanaged
		{
			bool lower = typeof(Lower) == typeof(bool);
			T value = start;
			for (int i = 0; i < length; i++)
			{
				T current = x[i];
				if ((lower && current.NativeGreaterThanOrEqual(value)) ||
					(!lower && current.NativeGreaterThan(value)))
				{
					// direct convert is OK here
					y[0] = *(U*)&i;
					// increase pointer
					y++;
					// increase value
					value = value.NativeIncrement();
					if (value.IsEqual(end))
						break;
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static unsafe bool IndexGetAllBounds<T, TOut>(Storage<T> array, Storage<TOut> target, T start, T end, bool lowerBound) where T : unmanaged, INumber<T> where TOut : unmanaged
		{
			if (!GetPointerIndexType(array, out T* x, out int lenx))
				return false;
			if (!GetPointerIndexType(target, out TOut* y, out int leny))
				return false;
			if (leny < end.NativeSub(start).ToLong())
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(target));
			if ((typeof(TOut) == typeof(byte) && leny > byte.MaxValue) ||
				(typeof(TOut) == typeof(sbyte) && leny > sbyte.MaxValue) ||
				(typeof(TOut) == typeof(short) && leny > short.MaxValue) ||
				(typeof(TOut) == typeof(ushort) && leny > ushort.MaxValue))
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(target));

			if (lowerBound)
				VectorAllBoundsManaged<T, TOut, bool>(x, lenx, start, end, y);
			else
				VectorAllBoundsManaged<T, TOut, byte>(x, lenx, start, end, y);
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static unsafe bool IndexGenerateFromBounds<T, TOut>(Storage<T> bounds, Storage<TOut> target, bool lowerBound, TOut start) where T : unmanaged, INumber<T> where TOut : unmanaged
		{
			if (!GetPointerIndexType(bounds, out T* x, out int lenx))
				return false;
			if (!GetPointerIndexType(target, out TOut* y, out int leny))
				return false;
			if (lowerBound)
			{	// the 'lower' bound array has to contain the length information as well
				x++; lenx--;
			}
			int length = x[lenx - 1].ToInt();
			if (length > leny)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(target));
			long startL = start.ToLong();
			if ((typeof(TOut) == typeof(byte) && length + startL > byte.MaxValue) ||
				(typeof(TOut) == typeof(sbyte) && length + startL > sbyte.MaxValue) ||
				(typeof(TOut) == typeof(short) && length + startL > short.MaxValue) ||
				(typeof(TOut) == typeof(ushort) && length + startL > ushort.MaxValue) ||
				(typeof(TOut) == typeof(int) && length + startL > int.MaxValue) ||
				(typeof(TOut) == typeof(uint) && length + startL > uint.MaxValue))
				throw new ArgumentOutOfRangeException(nameof(start), start, Resources.Parameter.InvalidValue);

			TOut value = start;
			int xPre = x[0].ToInt(), xNow;
			new Span<TOut>(y, x[0].ToInt()).Fill(value);
			for (int i = 1; i < lenx; i++)
			{
				value.NativeIncrement();
				xNow = x[i].ToInt();
				new Span<TOut>(y + xPre, xNow).Fill(value);
				xPre = xNow;
			}
			return true;
		}
		#endregion

		#region find
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe int VectorFindManaged<T>(T* x, int length, T value) where T : unmanaged, INumber<T>
		{
			return sizeof(T) switch
			{
				sizeof(byte) => new ReadOnlySpan<byte>(x, length).IndexOf(*(byte*)&value),
				sizeof(short) => new ReadOnlySpan<short>(x, length).IndexOf(*(short*)&value),
				sizeof(int) => new ReadOnlySpan<int>(x, length).IndexOf(*(int*)&value),
				sizeof(long) => new ReadOnlySpan<long>(x, length).BinarySearch(*(long*)&value),
				_ => -1,
			};
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe int VectorFind<T>(T* x, int length, T value) where T : unmanaged, INumber<T>
		{
			Vector<T> values = new(value);
			Vector<T> current;
			int lengthLeft = length, offset = 0;
			bool found = false;
			while (lengthLeft >= Vector<T>.Count)
			{
				current = LoadVector(x + offset);
				if (Vector.EqualsAny(current, values))
				{
					found = true;
					break;
				}
				lengthLeft -= Vector<T>.Count;
				offset += Vector<T>.Count;
			}
			if (found || lengthLeft > 0)
			{
				int len = found ? Vector<T>.Count : lengthLeft;
				int find = VectorFindManaged(x + offset, len, value);
				return find >= 0 ? (find + offset) : -1;
			}
			else
			{
				return -1;
			}	
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static unsafe bool IndexFind<T>(bool sorted, Storage<T> array, T value, out long find) where T : unmanaged, INumber<T>
		{
			find = -1;
			if (!GetPointerIndexType(array, out T* x, out int length))
				return false;
			if (length == 0)
				return true;

			if (sorted)
			{
				find = sizeof(T) switch
				{
					sizeof(byte) => new ReadOnlySpan<byte>(x, length).BinarySearch(*(byte*)&value),
					sizeof(short) => new ReadOnlySpan<short>(x, length).BinarySearch(*(short*)&value),
					sizeof(int) => new ReadOnlySpan<int>(x, length).BinarySearch(*(int*)&value),
					sizeof(long) => new ReadOnlySpan<long>(x, length).BinarySearch(*(long*)&value),
					_ => -1,
				};
			}
			else
			{
				if (Vector.IsHardwareAccelerated && length > Vector<T>.Count * 4)
				{
					find = VectorFind(x, length, value);
				}
				else
				{
					find = VectorFindManaged(x, length, value);
				}
			}
			return true;
		}
		#endregion
		#endregion
	}
#pragma warning restore CS1591 // 缺少对公共可见类型或成员的 XML 注释
}
