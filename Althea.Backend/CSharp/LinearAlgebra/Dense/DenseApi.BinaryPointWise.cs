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
			equals = true;
			if (x == y)
				return true;
			equals = false;
			if (x.Length != y.Length)
				return true;
			if (!GetPointer(x, out T* px, out int length))
				return false;
			if (!GetPointer(y, out T* py, out _))
				return false;

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
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static unsafe bool PointWiseDivide<T>(Storage<T> x, Storage<T> y) where T : unmanaged
		{
			if (!GetPointer(x, out T* px, out int lenx))
				return false;
			if (!GetPointer(y, out T* py, out int leny))
				return false;
			// shortcuts
			int length = Math.Min(lenx, leny);
			if (length == 0)
				return true;
			if (px == py)
			{
				new Span<T>(py, length).Fill(Const<T>.One);
				return true;
			}
			// normal case

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static unsafe bool PointWiseMultiply<T>(Storage<T> x, Storage<T> y) where T : unmanaged
		{
			if (!GetPointer(x, out T* px, out int lenx))
				return false;
			if (!GetPointer(y, out T* py, out int leny))
				return false;
			// shortcuts
			int length = Math.Min(lenx, leny);
			if (length == 0)
				return true;
			if (px == py)
			{
				return PointWisePower(x, 2);
			}
			// normal case

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static unsafe bool VectorGeneralAdd<T>(T α, Storage<T> x, Storage<T> y) where T : unmanaged
		{
			if (!GetPointer(x, out T* px, out int lenx))
				return false;
			if (!GetPointer(y, out T* py, out int leny))
				return false;
			// shortcuts
			if (α.IsZero())
				return true;
			int length = Math.Min(lenx, leny);
			if (length == 0)
				return true;
			if (px == py)
				return Scale(y, α.GenericAdd(Const<T>.One));
			// normal case

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
