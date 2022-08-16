using System.Numerics;
using System.Runtime.CompilerServices;

using Althea.Array;
using Althea.Backend.Storage;
using Althea.LinearAlgebra;
using Althea.Linq;
using Althea.Storage;

using static Althea.Backend.Cuda.LinearAlgebra.Sparse.NativeMethods;


namespace Althea.Backend.Cuda;

internal static unsafe partial class MemoryPointerChecker
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T* GetPointerDirect<T, TS>(this TS s, [CallerArgumentExpression("s")] string? sName = null) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
	{
		var ps = Unsafe.As<TS, PureStorage<T, CudaMemoryPointer>>(ref Unsafe.AsRef(in s)).Pointer;
		T* pointer = ps.Pointer.UnmangedPointer<T>(ps.OffsetInBytes);
		return pointer;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool GetPointer<T, TS>(TS s, out T* pointer, out long length, [CallerArgumentExpression("s")] string? sName = null) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
	{
		pointer = default; length = 0;
		if (s is null || !s.IsValid())
			throw new ArgumentNullException(sName);
		if (!CheckPointer<T, TS>(s))
			return false;
		var ps = Unsafe.As<TS, PureStorage<T, CudaMemoryPointer>>(ref Unsafe.AsRef(in s)).Pointer;
		pointer = ps.Pointer.UnmangedPointer<T>(ps.OffsetInBytes);
		if (pointer == default)
			throw new ArgumentException(Resources.ParameterError.InvalidValue, sName);
		length = ps.LengthInBytes / sizeof(T);
		return length >= 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool GetPointer<T, TS>(IBindedDevice api, TS s, out T* pointer, out long length, [CallerArgumentExpression("s")] string? sName = null) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
	{
		pointer = default; length = 0;
		if (api.BindedDeviceID != Runtime.CurrentDeviceID)
			return false;
		if (s is null || !s.IsValid())
			throw new ArgumentNullException(sName);
		if (!CheckPointer<T, TS>(s))
			return false;
		var ps = Unsafe.As<TS, PureStorage<T, CudaMemoryPointer>>(ref Unsafe.AsRef(in s)).Pointer;
		pointer = ps.Pointer.UnmangedPointer<T>(ps.OffsetInBytes);
		if (pointer == default)
			throw new ArgumentException(Resources.ParameterError.InvalidValue, sName);
		length = ps.LengthInBytes / sizeof(T);
		return length >= 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool GetPointer<T, TS>(IBindedDevice api, TS s, long stride, out T* pointer, out long length, [CallerArgumentExpression("s")] string? sName = null, [CallerArgumentExpression("stride")] string? strideName = null) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
	{
		pointer = default; length = 0;
		if (api.BindedDeviceID != Runtime.CurrentDeviceID)
			return false;
		if (s is null || !s.IsValid())
			throw new ArgumentNullException(sName);
		if (stride <= 0)
			throw new ArgumentOutOfRangeException(strideName, strideName, Resources.ParameterError.MustPositive);
		if (!CheckPointer<T, TS>(s))
			return false;
		var ps = Unsafe.As<TS, PureStorage<T, CudaMemoryPointer>>(ref Unsafe.AsRef(in s)).Pointer;
		pointer = ps.Pointer.UnmangedPointer<T>(ps.OffsetInBytes);
		if (pointer == default)
			throw new ArgumentException(Resources.ParameterError.InvalidValue, sName);
		length = ps.LengthInBytes / sizeof(T);
		length = (length - 1) / stride + 1;
		if (length > int.MaxValue || length < 0)
			return false;
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool GetPointer<T, TS>(IBindedDevice api, TS s, long stride, out T* pointer, out int length, out int inc, [CallerArgumentExpression("s")] string? sName = null, [CallerArgumentExpression("stride")] string? strideName = null) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
	{
		pointer = default; length = 0; inc = 0;
		if (api.BindedDeviceID != Runtime.CurrentDeviceID)
			return false;
		if (s is null || !s.IsValid())
			throw new ArgumentNullException(sName);
		if (stride <= 0)
			throw new ArgumentOutOfRangeException(strideName, strideName, Resources.ParameterError.MustPositive);
		if (!CheckPointer<T, TS>(s))
			return false;
		var ps = Unsafe.As<TS, PureStorage<T, CudaMemoryPointer>>(ref Unsafe.AsRef(in s)).Pointer;
		pointer = ps.Pointer.UnmangedPointer<T>(ps.OffsetInBytes);
		if (pointer == default)
			throw new ArgumentException(Resources.ParameterError.InvalidValue, sName);
		long n = ps.LengthInBytes / sizeof(T);
		n = (n - 1) / stride + 1;
		length = (int)n; inc = (int)stride;
		if (n > int.MaxValue || n < 0 || stride > int.MaxValue)
			return false;
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool GetPointer<T, TS>(IBindedDevice api, TS? s, long m, long n, long ld, out T* pointer, out int mm, out int nn, out int lld, [CallerArgumentExpression("s")] string? sName = null, [CallerArgumentExpression("m")] string? mName = null, [CallerArgumentExpression("n")] string? nName = null, [CallerArgumentExpression(@"ld")] string? ldName = null) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
	{
		pointer = default; mm = (int)m; nn = (int)n; lld = (int)ld;
		if (api.BindedDeviceID != Runtime.CurrentDeviceID)
			return false;
		if (s is null || !s.IsValid())
			return true;
		if (m > int.MaxValue || n > int.MaxValue || ld > int.MaxValue)
			return false;
		if (m <= 0)
			throw new ArgumentOutOfRangeException(mName, m, Resources.ParameterError.MustPositive);
		if (n <= 0)
			throw new ArgumentOutOfRangeException(nName, n, Resources.ParameterError.MustPositive);
		if (ld < m)
			throw new ArgumentOutOfRangeException(ldName, ld, Resources.ParameterError.InvalidValue);
		if (!CheckPointer<T, TS>(s))
			return false;
		var ps = Unsafe.As<TS, PureStorage<T, CudaMemoryPointer>>(ref Unsafe.AsRef(in s)).Pointer;
		pointer = ps.Pointer.UnmangedPointer<T>(ps.OffsetInBytes);
		if (pointer == default)
			throw new ArgumentException(Resources.ParameterError.InvalidValue, sName);
		long len = ps.LengthInBytes / sizeof(T);
		if ((len + (ld - m)) / ld < n)
			throw new ArgumentException(Resources.ParameterError.WrongSize);
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool GetPointer<T, TS>(IBindedDevice api, TS? s, MatrixOperation op, long m, long n, long ld, out T* pointer, [CallerArgumentExpression("s")] string? sName = null, [CallerArgumentExpression("m")] string? mName = null, [CallerArgumentExpression("n")] string? nName = null, [CallerArgumentExpression(@"ld")] string? ldName = null) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
	{
		if (!op.CanInPlace())
		{
			(m, n) = (n, m);
		}
		return GetPointer(api, s, m, n, ld, out pointer, sName, mName, nName, ldName);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool GetPointer<T, TS>(IBindedDevice api, TS? s, MatrixOperation op, long m, long n, long ld, out T* pointer, out int mm, out int nn, out int lld, [CallerArgumentExpression("s")] string? sName = null, [CallerArgumentExpression("m")] string? mName = null, [CallerArgumentExpression("n")] string? nName = null, [CallerArgumentExpression(@"ld")] string? ldName = null) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
	{
		if (!op.CanInPlace())
		{
			(m, n) = (n, m);
		}
		return GetPointer(api, s, m, n, ld, out pointer, out mm, out nn, out lld, sName, mName, nName, ldName);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool GetPointer<T, TS>(IBindedDevice api, TS? s, long m, long n, long ld, out T* pointer, [CallerArgumentExpression("s")] string? sName = null, [CallerArgumentExpression("m")] string? mName = null, [CallerArgumentExpression("n")] string? nName = null, [CallerArgumentExpression(@"ld")] string? ldName = null) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
	{
		pointer = default;
		if (api.BindedDeviceID != Runtime.CurrentDeviceID)
			return false;
		if (s is null || !s.IsValid())
			return true;
		if (m <= 0)
			throw new ArgumentOutOfRangeException(mName, m, Resources.ParameterError.MustPositive);
		if (n <= 0)
			throw new ArgumentOutOfRangeException(nName, n, Resources.ParameterError.MustPositive);
		if (ld < m)
			throw new ArgumentOutOfRangeException(ldName, ld, Resources.ParameterError.InvalidValue);
		if (!CheckPointer<T, TS>(s))
			return false;
		var ps = Unsafe.As<TS, PureStorage<T, CudaMemoryPointer>>(ref Unsafe.AsRef(in s)).Pointer;
		pointer = ps.Pointer.UnmangedPointer<T>(ps.OffsetInBytes);
		if (pointer == default)
			throw new ArgumentException(Resources.ParameterError.InvalidValue, sName);
		long len = ps.LengthInBytes / sizeof(T);
		if ((len + (ld - m)) / ld < n)
			throw new ArgumentException(Resources.ParameterError.WrongSize);
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static unsafe bool GetPointer<T, TInd, TS, TSInd>(IBindedDevice api, TS s, TSInd sInd, out T* pointer, out TInd* pointerInd, out long length, [CallerArgumentExpression("s")] string? sName = null, [CallerArgumentExpression("sInd")] string? sIndName = null) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS> where TInd : unmanaged, IBinaryInt<TInd> where TSInd : class, IStorage<TInd, TSInd>
	{
		pointer = default; pointerInd = default; length = 0;
		if (api.BindedDeviceID != Runtime.CurrentDeviceID)
			return false;
		if (sizeof(TInd) != sizeof(int) && sizeof(TInd) != sizeof(long))
			return false;
		if (s is null || !s.IsValid())
			throw new ArgumentNullException(sName);
		if (sInd is null || !sInd.IsValid())
			throw new ArgumentNullException(sIndName);
		if (!CheckPointer<T, TInd, TS, TSInd>(s, sInd))
			return false;
		var ps = Unsafe.As<TS, PureStorage<T, CudaMemoryPointer>>(ref Unsafe.AsRef(in s)).Pointer;
		var psInd = Unsafe.As<TSInd, PureStorage<TInd, CudaMemoryPointer>>(ref Unsafe.AsRef(in sInd)).Pointer;
		pointer = ps.Pointer.UnmangedPointer<T>(ps.OffsetInBytes);
		if (pointer == default)
			throw new ArgumentException(Resources.ParameterError.InvalidValue, sName);
		pointerInd = psInd.Pointer.UnmangedPointer<TInd>(psInd.OffsetInBytes);
		if (pointerInd == default)
			throw new ArgumentException(Resources.ParameterError.InvalidValue, sIndName);
		if (psInd.LengthInBytes != ps.LengthInBytes)
			throw new ArgumentException(Resources.ParameterError.NotSameSize, sIndName);
		length = ps.LengthInBytes / sizeof(T);
		return length >= 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static unsafe bool GetPointer<T, TInd, TS, TSInd>(IBindedDevice api, TS s, TSInd sInd1, TSInd sInd2, out T* pointer, out TInd* pointerInd1, out TInd* pointerInd2, out long length, [CallerArgumentExpression("s")] string? sName = null, [CallerArgumentExpression("sInd1")] string? sInd1Name = null, [CallerArgumentExpression("sInd2")] string? sInd2Name = null) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS> where TInd : unmanaged, IBinaryInt<TInd> where TSInd : class, IStorage<TInd, TSInd>
	{
		pointer = default; pointerInd1 = default; pointerInd2 = default; length = 0;
		if (api.BindedDeviceID != Runtime.CurrentDeviceID)
			return false;
		if (sizeof(TInd) != sizeof(int) && sizeof(TInd) != sizeof(long))
			return false;
		if (s is null || !s.IsValid())
			throw new ArgumentNullException(sName);
		if (sInd1 is null || !sInd1.IsValid())
			throw new ArgumentNullException(sInd1Name);
		if (sInd2 is null || !sInd2.IsValid())
			throw new ArgumentNullException(sInd2Name);
		if (!CheckPointer<T, TInd, TS, TSInd>(s, sInd1, sInd2))
			return false;
		var ps = Unsafe.As<TS, PureStorage<T, CudaMemoryPointer>>(ref Unsafe.AsRef(in s)).Pointer;
		var psInd1 = Unsafe.As<TSInd, PureStorage<TInd, CudaMemoryPointer>>(ref Unsafe.AsRef(in sInd1)).Pointer;
		var psInd2 = Unsafe.As<TSInd, PureStorage<TInd, CudaMemoryPointer>>(ref Unsafe.AsRef(in sInd2)).Pointer;
		pointer = ps.Pointer.UnmangedPointer<T>(ps.OffsetInBytes);
		if (pointer == default)
			throw new ArgumentException(Resources.ParameterError.InvalidValue, sName);
		pointerInd1 = psInd1.Pointer.UnmangedPointer<TInd>(psInd1.OffsetInBytes);
		if (pointerInd1 == default)
			throw new ArgumentException(Resources.ParameterError.InvalidValue, sInd1Name);
		pointerInd2 = psInd2.Pointer.UnmangedPointer<TInd>(psInd2.OffsetInBytes);
		if (pointerInd2 == default)
			throw new ArgumentException(Resources.ParameterError.InvalidValue, sInd2Name);
		length = ps.LengthInBytes / sizeof(T);
		return length >= 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static unsafe bool GetPointer<T, TInd, TS, TSInd>(IBindedDevice api, ISparseArray<T, TInd, TS, TSInd> vector, out T* pointer, out TInd* pointerInd, out long nnz, [CallerArgumentExpression("vector")] string? vectorName = null) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS> where TInd : unmanaged, IBinaryInt<TInd> where TSInd : class, IStorage<TInd, TSInd>
	{
		pointer = default; pointerInd = default; nnz = 0;
		if (api.BindedDeviceID != Runtime.CurrentDeviceID)
			return false;
		if (sizeof(TInd) != sizeof(int) && sizeof(TInd) != sizeof(long))
			return false;
		if (vector is null)
			throw new ArgumentNullException(vectorName);
		if (vector.Size.Length != 1)
			throw new ArgumentException(Resources.ParameterError.InvalidRank, vectorName);
		if (vector.Size[0] <= 0)
			throw new ArgumentException(Resources.ParameterError.WrongSize, vectorName);
		if (vector.Format != SparseFormat.VectorCooFormat)
			return false;
		if (vector.ValueStorages.Length != 1)
			return false;
		if (vector.IndexStorages.Length != 1)
			return false;
		return GetPointer(api, vector.ValueStorages[0], vector.IndexStorages[0], out pointer, out pointerInd, out nnz);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static unsafe bool GetPointer<T, TInd, TS, TSInd>(IBindedDevice api, ISparseArray<T, TInd, TS, TSInd> matrix, out T* pointer, out TInd* pointerRow, out TInd* pointerCol, out long nnz, [CallerArgumentExpression("matrix")] string? matrixName = null) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS> where TInd : unmanaged, IBinaryInt<TInd> where TSInd : class, IStorage<TInd, TSInd>
	{
		pointer = default; pointerRow = pointerCol = default; nnz = 0;
		if (api.BindedDeviceID != Runtime.CurrentDeviceID)
			return false;
		if (sizeof(TInd) != sizeof(int) && sizeof(TInd) != sizeof(long))
			return false;
		if (matrix is null)
			throw new ArgumentNullException(matrixName);
		if (matrix.Size.Length != 2)
			throw new ArgumentException(Resources.ParameterError.InvalidRank, matrixName);
		if (matrix.Size[0] <= 0 || matrix.Size[1] <= 0)
			throw new ArgumentException(Resources.ParameterError.WrongSize, matrixName);
		if ((matrix.Format & SupportFormat) == SparseFormat.None || !matrix.Format.IsAtomic)
			return false;
		if (matrix.ValueStorages.Length != 1 || matrix.IndexStorages.Length != 2)
			return false;
		return GetPointer(api, matrix.ValueStorages[0], matrix.IndexStorages[0], matrix.IndexStorages[1], out pointer, out pointerRow, out pointerCol, out nnz);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool GetPointerIncludeBlocked<T, TInd, TS, TSInd>(IBindedDevice api, ISparseArray<T, TInd, TS, TSInd> matrix, out T* pointer, out TInd* pointerRow, out TInd* pointerCol, out long nnz, [CallerArgumentExpression("matrix")] string? matrixName = null) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS> where TInd : unmanaged, IBinaryInt<TInd> where TSInd : class, IStorage<TInd, TSInd>
	{
		pointer = default; pointerRow = pointerCol = default; nnz = 0;
		if (api.BindedDeviceID != Runtime.CurrentDeviceID)
			return false;
		if (sizeof(TInd) != sizeof(int) && sizeof(TInd) != sizeof(long))
			return false;
		if (matrix is null)
			throw new ArgumentNullException(matrixName);
		if (matrix.Size.Length != 2)
			throw new ArgumentException(Resources.ParameterError.InvalidRank, matrixName);
		if (matrix.Size[0] <= 0 || matrix.Size[1] <= 0)
			throw new ArgumentException(Resources.ParameterError.WrongSize, matrixName);
		if ((matrix.Format & (SupportFormat | SparseFormat.MatrixBscFormat | SparseFormat.MatrixBsrFormat)) == SparseFormat.None || !matrix.Format.IsAtomic)
			return false;
		if (matrix.ValueStorages.Length != 1 || matrix.IndexStorages.Length != 2)
			return false;
		if (matrix.Format.BlockType == SparseFormat.Blocking.Simple && matrix.BlockSize.Length != 2)
			throw new ArgumentException(Resources.ParameterError.WrongSize, matrixName);
		return GetPointer(api, matrix.ValueStorages[0], matrix.IndexStorages[0], matrix.IndexStorages[1], out pointer, out pointerRow, out pointerCol, out nnz);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static unsafe bool GetPointer<T, TS>(IBindedDevice api, TS s, ReadOnlySpan<long> size, ReadOnlySpan<long> outerSize, out T* pointer,[CallerArgumentExpression("s")] string? sName = null, [CallerArgumentExpression("size")] string? sizeName = null, [CallerArgumentExpression("outerSize")] string? outerSizeName = null) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
	{
		pointer = default;
		if (api.BindedDeviceID != Runtime.CurrentDeviceID)
			return false;
		if (s is null || !s.IsValid())
			throw new ArgumentNullException(sName);
		if (size.AnyNonPositive())
			throw new ArgumentOutOfRangeException(sizeName, size.ToArray(), Resources.ParameterError.MustPositive);
		if (outerSize.AnyNonPositive())
			throw new ArgumentOutOfRangeException(outerSizeName, outerSize.ToArray(), Resources.ParameterError.MustPositive);
		if (!outerSize.SequenceLargerEqualThan(size))
			throw new ArgumentException(Resources.ParameterError.InvalidValue, outerSizeName);
		if (!GetPointer(s, out pointer, out long n, sName))
			return false;
		if (n < size.Prod())
			throw new ArgumentException(Resources.ParameterError.WrongSize, sName);
		return true;
	}
}
