using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Althea.Array;
using Althea.Backend.Storage;
using Althea.Helpers;
using Althea.Linq;

using static Althea.Backend.CSharp.MemoryPointerChecker;


namespace Althea.Backend.CSharp.LinearAlgebra
{
	public unsafe partial class Api
	{
		#region check
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool GetPointerIndexType<T, TS>(TS s, long stride, out T* pointer, out int length, out int inc) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
		{
			if (!T.Type.IsInteger())
			{
				pointer = default; length = 0;
				throw new TypeMismatchException(typeof(T), TypeMismatchException.MismatchReason.NotInteger);
			}
			return GetPointer(s, stride, out pointer, out length, out inc);
		}
		#endregion

		#region find
		public virtual partial bool Sort<T, TS>(TS array, long stride) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
		{
			if (!GetPointer(array, stride, out T* ptr, out int length, out int inc))
				return false;
			if (inc != 1)
				return false;
			new Span<T>(ptr, length).Sort();
			return true;
		}

		public virtual partial bool Sort<T, TOther, TS, TS2>(TS keys, long strideKeys, TS2 values, long strideValues) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS> where TOther : unmanaged, IBaseNumber<TOther> where TS2 : class, IStorage<TOther, TS2>
		{
			if (!GetPointer(keys, strideKeys, out T* k, out int length, out int incK))
				return false;
			if (!GetPointer(values, strideValues, out TOther* v, out int length2, out int incV))
				return false;
			if (length != length2)
				throw new ArgumentException(Resources.ParameterError.NotSameSize);
			if (incK != 1 || incV != 1)
				return false;
			new Span<T>(k, length).Sort(new Span<TOther>(v, length));
			return true;
		}

		public virtual partial bool MinMax<T, TS>(TS array, long stride, out (T Min, T Max) minmax) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
		{
			minmax = default;
			if (!MinMax<T, TS, long>(array, stride, out minmax.Min))
				return false;
			if (!MinMax<T, TS, int>(array, stride, out minmax.Max))
				return false;
			return true;
		}

		private static int BinarySearch<T>(T* x, int incx, int length, T value) where T : unmanaged, IBaseNumber<T>
		{
			int left = 0;
			int right = (length - 1) * incx;
			while (left <= right)
			{
				int mid = (int)((uint)(right + left) >> 1);
				int compare = value.CompareTo(x[mid]);
				if (compare == 0)
				{
					return mid;
				}
				if (compare > 0)
				{
					left = mid + incx;
				}
				else
				{
					right = mid - incx;
				}
			}
			return ~left;
		}

		public virtual partial bool IndexOf<T, TS>(TS array, long stride, bool sorted, T value, out long find) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
		{
			find = -1;
			if (!GetPointer(array, stride, out T* ptr, out int length, out int inc))
				return false;
			if (inc == 1)
			{
				if (sorted)
					find = new ReadOnlySpan<T>(ptr, length).BinarySearch(value);
				else
					find = new ReadOnlySpan<T>(ptr, length).IndexOf(value);
			}
			else
			{
				if (sorted)
					find = BinarySearch(ptr, inc, length, value);
				else
				{
					find = -1;
					for (int i = 0, ix = 0; i < length; i++, ix += inc)
					{
						if (ptr[ix] == value)
						{
							find = i;
							break;
						}	
					}
				}
			}
			return true;
		}
		#endregion

		#region bound
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static int VectorBound<T, Lower>(T* x, int length, T value) where T : unmanaged, IBaseNumber<T>
		{
			bool lower = typeof(Lower) == typeof(bool);
			Vector<T> values = new(value);
			Vector<T> current;
			int lengthLeft = length, offset = 0;
			bool found = false;
			while (lengthLeft >= Vector<T>.Count)
			{
				current = LoadVector(x + offset);
				if ((lower && Vector.GreaterThanOrEqualAny(current, values)) || (!lower && Vector.GreaterThanAny(current, values)))
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
				int find = VectorBoundManaged<T, Lower>(x + offset, 1, len, value);
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
		private static int VectorBoundManaged<T, Lower>(T* x, int incx, int length, T value) where T : unmanaged, IBaseNumber<T>
		{
			bool lower = typeof(Lower) == typeof(bool);
			for (int i = 0, ix = 0; i < length; i++, ix += incx)
			{
				T current = x[ix];
				if ((lower && current >= value) || (!lower && current > value))
				{
					return i;
				}
			}
			// not found
			return lower ? -1 : length;
		}

		public virtual partial bool IndexBound<T, TS>(TS array, long stride, T value, bool lowerBound, out long index) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
		{
			index = -1;
			if (!GetPointer(array, stride, out T* x, out int length, out int inc))
				return false;
			if (length == 0)
				return true;

			if (inc == 1 && Vector.IsHardwareAccelerated && length > Vector<T>.Count * 4)
			{
				if (lowerBound)
					index = VectorBound<T, bool>(x, length, value);
				else
					index = VectorBound<T, byte>(x, length, value);
			}
			else
			{
				if (lowerBound)
					index = VectorBoundManaged<T, bool>(x, inc, length, value);
				else
					index = VectorBoundManaged<T, byte>(x, inc, length, value);
			}
			return true;
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void VectorAllBoundsManaged<T, U, Lower>(T* x, int length, T start, T end, U* y) where T : unmanaged, IBinaryInt<T> where U : unmanaged, IBinaryInt<U>
		{
			bool lower = typeof(Lower) == typeof(bool);
			T value = start;
			for (int i = 0; i < length && value != end; i++)
			{
				T current = x[i];
				if ((lower && current >= value) || (!lower && current > value))
				{
					// direct convert is OK here
					y[0] = *(U*)&i;
					// increase pointer
					y++;
					// increase value
					value++;
				}
			}
		}

		public virtual partial bool IndexGetAllBounds<T, TOut, TS, TSOut>(TS array, TSOut target, T start, T end, bool lowerBound) where T : unmanaged, IBinaryInt<T> where TS : class, IStorage<T, TS> where TOut : unmanaged, IBinaryInt<TOut> where TSOut : class, IStorage<TOut, TSOut>
		{
			if (!GetPointerIndexType(array, 1, out T* x, out int lenx, out _))
				return false;
			if (!GetPointerIndexType(target, 1, out TOut* y, out int leny, out _))
				return false;
			if (leny.As<T>() < end - start)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(target));
			if ((typeof(TOut) == typeof(byte) && leny > byte.MaxValue) ||
				(typeof(TOut) == typeof(sbyte) && leny > sbyte.MaxValue) ||
				(typeof(TOut) == typeof(short) && leny > short.MaxValue) ||
				(typeof(TOut) == typeof(ushort) && leny > ushort.MaxValue))
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(target));

			if (lowerBound)
				VectorAllBoundsManaged<T, TOut, bool>(x, lenx, start, end, y);
			else
				VectorAllBoundsManaged<T, TOut, byte>(x, lenx, start, end, y);
			return true;
		}

		public virtual partial bool IndexGenerateFromBounds<T, TOut, TS, TSOut>(TS bounds, TSOut target, bool lowerBound, TOut start) where T : unmanaged, IBinaryInt<T> where TOut : unmanaged, IBinaryInt<TOut> where TS : class, IStorage<T, TS> where TSOut : class, IStorage<TOut, TSOut>
		{
			if (!GetPointerIndexType(bounds, 1, out T* x, out int lenx, out _))
				return false;
			if (!GetPointerIndexType(target, 1, out TOut* y, out int leny, out _))
				return false;
			if (lowerBound)
			{	// the 'lower' bound array has to contain the length information as well
				x++; lenx--;
			}
			int length = x[lenx - 1].AsInt32();
			if (length > leny)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(target));
			long startL = start.AsInt64();
			if ((typeof(TOut) == typeof(byte) && length + startL > byte.MaxValue) ||
				(typeof(TOut) == typeof(sbyte) && length + startL > sbyte.MaxValue) ||
				(typeof(TOut) == typeof(short) && length + startL > short.MaxValue) ||
				(typeof(TOut) == typeof(ushort) && length + startL > ushort.MaxValue) ||
				(typeof(TOut) == typeof(int) && length + startL > int.MaxValue) ||
				(typeof(TOut) == typeof(uint) && length + startL > uint.MaxValue))
				throw new ArgumentOutOfRangeException(nameof(start), start, Resources.ParameterError.InvalidValue);

			TOut value = start;
			int xPre = x[0].AsInt32(), xNow;
			new Span<TOut>(y, xPre).Fill(value);
			for (int i = 1; i < lenx; i++)
			{
				value++;
				xNow = x[i].AsInt32();
				new Span<TOut>(y + xPre, xNow).Fill(value);
				xPre = xNow;
			}
			return true;
		}
		#endregion

		#region sparse vector
		public virtual partial bool VectorSetValuesAt<T, TInd, TS, TSInd>(TS x, T value, TSInd positions) where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd> where TS : class, IStorage<T, TS> where TSInd : class, IStorage<TInd, TSInd>
		{
			if (!GetPointer(x, 1, out T* px, out int length, out _))
				return false;
			if (!GetPointerIndexType(positions, 1, out TInd* pp, out int lengthPos, out _))
				return false;
			if (length < lengthPos)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(positions));
			if (TInd.IsComplexType)
				throw new TypeMismatchException(typeof(TInd), TypeMismatchException.MismatchReason.NotReal);

			switch (sizeof(TInd))
			{
				case 1:
					for (int i = 0; i < lengthPos; i++)
					{
						px[((byte*)pp)[i]] = value;
					}
					break;
				case 2:
					for (int i = 0; i < lengthPos; i++)
					{
						px[((ushort*)pp)[i]] = value;
					}
					break;
				case 4:
					for (int i = 0; i < lengthPos; i++)
					{
						px[((uint*)pp)[i]] = value;
					}
					break;
				case 8:
					for (int i = 0; i < lengthPos; i++)
					{
						px[((ulong*)pp)[i]] = value;
					}
					break;
				default:
					return false;
			}
			return true;
		}

		public virtual partial bool VectorSetValuesAt<T, TInd, TS1, TS2, TSInd>(TS1 x, TS2 values, TSInd positions) where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TSInd : class, IStorage<TInd, TSInd>
		{
			if (!GetPointer(x, 1, out T* px, out int length, out _))
				return false;
			if (!GetPointer(values, 1, out T* py, out int length2, out _))
				return false;
			if (!GetPointerIndexType(positions, 1, out TInd* pp, out int lengthPos, out _))
				return false;
			if (length < lengthPos || length2 != lengthPos)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(positions));
			if (TInd.IsComplexType)
				throw new TypeMismatchException(typeof(TInd), TypeMismatchException.MismatchReason.NotReal);

			switch (sizeof(TInd))
			{
				case 1:
					for (int i = 0; i < lengthPos; i++)
					{
						px[((byte*)pp)[i]] = py[i];
					}
					break;
				case 2:
					for (int i = 0; i < lengthPos; i++)
					{
						px[((ushort*)pp)[i]] = py[i];
					}
					break;
				case 4:
					for (int i = 0; i < lengthPos; i++)
					{
						px[((uint*)pp)[i]] = py[i];
					}
					break;
				case 8:
					for (int i = 0; i < lengthPos; i++)
					{
						px[((ulong*)pp)[i]] = py[i];
					}
					break;
				default:
					return false;
			}
			return true;
		}

		public virtual partial bool VectorGatherValuesAt<T, TInd, TS1, TS2, TSInd>(TS1 x, TS2 values, TSInd positions) where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TSInd : class, IStorage<TInd, TSInd>
		{
			if (!GetPointer(x, 1, out T* px, out int length, out _))
				return false;
			if (!GetPointer(values, 1, out T* py, out int length2, out _))
				return false;
			if (!GetPointerIndexType(positions, 1, out TInd* pp, out int lengthPos, out _))
				return false;
			if (length < lengthPos || length2 != lengthPos)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(positions));
			if (TInd.IsComplexType)
				throw new TypeMismatchException(typeof(TInd), TypeMismatchException.MismatchReason.NotReal);

			switch (sizeof(TInd))
			{
				case 1:
					for (int i = 0; i < lengthPos; i++)
					{
						py[i] = px[((byte*)pp)[i]];
					}
					break;
				case 2:
					for (int i = 0; i < lengthPos; i++)
					{
						py[i] = px[((ushort*)pp)[i]];
					}
					break;
				case 4:
					for (int i = 0; i < lengthPos; i++)
					{
						py[i] = px[((uint*)pp)[i]];
					}
					break;
				case 8:
					for (int i = 0; i < lengthPos; i++)
					{
						py[i] = px[((ulong*)pp)[i]];
					}
					break;
				default:
					return false;
			}
			return true;
		}

		public virtual partial bool VectorSparseToDense<T, TInd, TS1, TS2, TSInd>(ISparseArray<T, TInd, TS1, TSInd> x, TS2 y!!, long strideY) where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TSInd : class, IStorage<TInd, TSInd>
		{
			if (strideY != 1)
				return false;
			if (x.Format != new SparseFormat(SparseFormat.Type.Coordinated, SparseFormat.Blocking.Element, SparseFormat.Major.None))
				return false;
			if (y.Length != x.Size[0])
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(y));
			if (!FillWithValue(y, 1, T.Zero))
				return false;
			return this.VectorSetValuesAt<T, TInd, TS2, TS1, TSInd>(y, x.ValueStorages[0], x.IndexStorages[0]);
		}

		public virtual partial bool VectorDenseToSparse<T, TInd, TS1, TS2, TSInd>(TS1 x, long strideX, ref SparseArrayWrapper<T, TInd, TS2, TSInd> y, double threshold) where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TSInd : class, IStorage<TInd, TSInd>
		{
			if ((y.Format & SparseFormat.VectorCooFormat) == SparseFormat.None)
				return false;
			if (sizeof(TInd) != 4 && sizeof(TInd) != 8)
				return false;
			if (!y.Size.IsEmpty && x.Length != y.Size[0])
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(y));
			if (!y.IndexStorages.IsEmpty && y.IndexStorages[0].Length != y.ValueStorages[0].Length)
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(y));
			if (!GetPointer(x, strideX, out T* px, out int length, out int inc))
				return false;

			T thre = Math.Abs(threshold).As<T>();
			int nnz = 0;
			for (int i = 0, ix = 0; i < length; i++, ix += inc)
			{
				if (T.Abs(px[ix]) <= thre)
					nnz++;
			}
			T* py;
			TInd* pp;
			if (y.IndexStorages.IsEmpty)
			{
				py = (T*)Marshal.AllocHGlobal(nnz * sizeof(T));
				pp = (TInd*)Marshal.AllocHGlobal(nnz * sizeof(TInd));
			}
			else
			{
				if (!GetPointer(y.ValueStorages[0], 1, out py, out int len, out _))
					return false;
				if (len != nnz)
					throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(y));
				if (!GetPointerIndexType(y.IndexStorages[0], 1, out pp, out len, out _))
					return false;
				if (len != nnz)
					throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(y));
			}
			nnz = 0;
			if (sizeof(TInd) == 4)
			{
				for (int i = 0, ix = 0; i < length; i++, ix += inc)
				{
					var vx = px[ix];
					if (T.Abs(vx) <= thre)
					{
						py[nnz] = vx;
						pp[nnz++] = *(TInd*)&i;
					}
				}
			}
			else
			{
				for (long i = 0, ix = 0; i < length; i++, ix += strideX)
				{
					var vx = px[ix];
					if (T.Abs(vx) <= thre)
					{
						py[nnz] = vx;
						pp[nnz++] = *(TInd*)&i;
					}
				}
			}
			TS2 vals = new Backend.Storage.ActualPureStorage<T, CpuMemoryPointer>(new CpuMemoryPointer((IntPtr)py, nnz * sizeof(T))) as TS2 ?? TS2.Empty;
			TSInd inds = new Backend.Storage.ActualPureStorage<TInd, CpuMemoryPointer>(new CpuMemoryPointer((IntPtr)pp, nnz * sizeof(TInd))) as TSInd ?? TSInd.Empty;
			y.SetValues(length, vals, inds);
			return true;
		}
		#endregion
	}
}
