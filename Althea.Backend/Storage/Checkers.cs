using System.Runtime.CompilerServices;

using Althea.Array;


namespace Althea.Backend.Storage;

internal static class CpuMemoryPointerChecker
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static unsafe bool GetPointer<T, TS>(TS s, long stride, out T* pointer, out int length, out int inc, [CallerArgumentExpression("s")] string? sName = null, [CallerArgumentExpression("stride")] string? strideName = null) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
	{
		pointer = default; length = 0; inc = (int)stride;
		if (s is null || !s.IsValid())
			throw new ArgumentNullException(sName);
		if (stride <= 0)
			throw new ArgumentOutOfRangeException(strideName, stride, Resources.ParameterError.MustPositive);
		if (stride > int.MaxValue)
			return false;
		if (s is not PureStorage<T, CpuMemoryPointer> ps)
			return false; // not support
		pointer = ps.Pointer.Pointer.UnmangedPointer<T>(ps.Pointer.OffsetInBytes);
		if (pointer == default)
			throw new ArgumentException(Resources.ParameterError.InvalidValue, sName);
		long len = ps.Length;
		len = (len - 1) / stride + 1;
		if (len > int.MaxValue || length < 0)
			return false;
		length = (int)len;
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static unsafe bool GetPointer<T, TS>(TS s, long stride, out T* pointer, out long length, [CallerArgumentExpression("s")] string? sName = null, [CallerArgumentExpression("stride")] string? strideName = null) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
	{
		pointer = default; length = 0;
		if (s is null || !s.IsValid())
			throw new ArgumentNullException(sName);
		if (stride <= 0)
			throw new ArgumentOutOfRangeException(strideName, stride, Resources.ParameterError.MustPositive);
		if (stride > int.MaxValue)
			return false;
		if (s is not PureStorage<T, CpuMemoryPointer> ps)
			return false; // not support
		pointer = ps.Pointer.Pointer.UnmangedPointer<T>(ps.Pointer.OffsetInBytes);
		if (pointer == default)
			throw new ArgumentException(Resources.ParameterError.InvalidValue, sName);
		length = ps.Length;
		length = (length - 1) / stride + 1;
		if (length > int.MaxValue || length < 0)
			return false;
		return true;
	}

	// Ignore Spelling: ld
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static unsafe bool GetPointer<T, TS>(TS? s, long m, long n, long ld, out T* pointer, out int mm, out int nn, out int lld, [CallerArgumentExpression("s")] string? sName = null, [CallerArgumentExpression("m")] string? mName = null, [CallerArgumentExpression("n")] string? nName = null, [CallerArgumentExpression("ld")] string? ldName = null) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
	{
		pointer = default; mm = (int)m; nn = (int)n; lld = (int)ld;
		if (s is null || !s.IsValid())
			return true;
		if (m <= 0)
			throw new ArgumentOutOfRangeException(mName, m, Resources.ParameterError.MustPositive);
		if (n <= 0)
			throw new ArgumentOutOfRangeException(nName, n, Resources.ParameterError.MustPositive);
		if (ld < m)
			throw new ArgumentOutOfRangeException(ldName, ld, Resources.ParameterError.InvalidValue);
		if (m > int.MaxValue || n > int.MaxValue || ld > int.MaxValue)
			return false;
		if (s is not PureStorage<T, CpuMemoryPointer> ps)
			return false; // not support
		pointer = ps.Pointer.Pointer.UnmangedPointer<T>(ps.Pointer.OffsetInBytes);
		if (pointer == default)
			throw new ArgumentException(Resources.ParameterError.InvalidValue, sName);
		if ((ps.Length + (ld - m)) / ld < n)
			throw new ArgumentException(Resources.ParameterError.WrongSize);
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static unsafe bool GetPointer<T, TS>(TS? s, long m, long n, long ld, out T* pointer, [CallerArgumentExpression("s")] string? sName = null, [CallerArgumentExpression("m")] string? mName = null, [CallerArgumentExpression("n")] string? nName = null, [CallerArgumentExpression("ld")] string? ldName = null) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
	{
		pointer = default;
		if (s is null || !s.IsValid())
			return true;
		if (m <= 0)
			throw new ArgumentOutOfRangeException(mName, m, Resources.ParameterError.MustPositive);
		if (n <= 0)
			throw new ArgumentOutOfRangeException(nName, n, Resources.ParameterError.MustPositive);
		if (ld < m)
			throw new ArgumentOutOfRangeException(ldName, ld, Resources.ParameterError.InvalidValue);
		if (m > int.MaxValue || n > int.MaxValue || ld > int.MaxValue)
			return false;
		if (s is not PureStorage<T, CpuMemoryPointer> ps)
			return false; // not support
		pointer = ps.Pointer.Pointer.UnmangedPointer<T>(ps.Pointer.OffsetInBytes);
		if (pointer == default)
			throw new ArgumentException(Resources.ParameterError.InvalidValue, sName);
		if ((ps.Length + (ld - m)) / ld < n)
			throw new ArgumentException(Resources.ParameterError.WrongSize);
		return true;
	}


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static unsafe bool GetPointer<T, TS>(TS s, out T* pointer, out long length, [CallerArgumentExpression("s")] string? sName = null) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
	{
		pointer = default; length = 0;
		if (s is null || !s.IsValid())
			throw new ArgumentNullException(sName);
		if (s is not PureStorage<T, CpuMemoryPointer> ps)
			return false; // not support
		pointer = ps.Pointer.Pointer.UnmangedPointer<T>(ps.Pointer.OffsetInBytes);
		if (pointer == default)
			throw new ArgumentException(Resources.ParameterError.InvalidValue, sName);
		length = ps.Length;
		if (length > int.MaxValue || length < 0)
			return false;
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static unsafe bool GetPointer<T, TInd, TS, TSInd>(TS s, TSInd sInd, out T* pointer, out TInd* pointerInd, out long length, [CallerArgumentExpression("s")] string? sName = null, [CallerArgumentExpression("sInd")] string? sIndName = null) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS> where TInd : unmanaged, IBinaryInt<TInd> where TSInd : class, IStorage<TInd, TSInd>
	{
		pointer = default; pointerInd = default; length = 0;
		if (!TInd.Type.IsInteger() || TInd.Size != sizeof(int))
			return false;
		if (s is null || !s.IsValid())
			throw new ArgumentNullException(sName);
		if (sInd is null || !sInd.IsValid())
			throw new ArgumentNullException(sIndName);
		if (s is not PureStorage<T, CpuMemoryPointer> ps || sInd is not PureStorage<TInd, CpuMemoryPointer> psInd)
			return false; // not support
		pointer = ps.Pointer.Pointer.UnmangedPointer<T>(ps.Pointer.OffsetInBytes);
		if (pointer == default)
			throw new ArgumentException(Resources.ParameterError.InvalidValue, sName);
		pointerInd = psInd.Pointer.Pointer.UnmangedPointer<TInd>(psInd.Pointer.OffsetInBytes);
		if (pointerInd == default)
			throw new ArgumentException(Resources.ParameterError.InvalidValue, sIndName);
		length = ps.Length;
		if (length > int.MaxValue || length < 0)
			return false;
		if (psInd.Length != length)
			throw new ArgumentException(Resources.ParameterError.NotSameSize, sIndName);
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static unsafe bool GetPointer<T, TInd, TS, TSInd>(ISparseArray<T, TInd, TS, TSInd> matrix, out T* pointer, out TInd* pointerRow, out TInd* pointerCol, out long nnz, [CallerArgumentExpression("matrix")] string? matrixName = null) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS> where TInd : unmanaged, IBinaryInt<TInd> where TSInd : class, IStorage<TInd, TSInd>
	{
		pointer = default; pointerRow = pointerCol = default; nnz = 0;
		if (!TInd.Type.IsInteger() || TInd.Size != sizeof(int))
			return false;
		if (matrix is null)
			throw new ArgumentNullException(matrixName);
		if (matrix.Size.Length != 2 || matrix.Size[0] <= 0 || matrix.Size[1] <= 0)
			throw new ArgumentException(Resources.ParameterError.WrongSize, matrixName);
		if ((matrix.Format.Class & (SparseFormat.Type.Coordinated | SparseFormat.Type.Compressed)) == 0 ||
			(matrix.Format.BlockType & (SparseFormat.Blocking.Element | SparseFormat.Blocking.Simple)) == 0 ||
			(matrix.Format.MajorType & (SparseFormat.Major.Column | SparseFormat.Major.Row)) == 0)
			return false;
		if (matrix.Format.BlockType == SparseFormat.Blocking.Simple &&
			(matrix.BlockSize.Length != 2 || matrix.BlockSize[0] <= 0 || matrix.BlockSize[1] <= 0))
			throw new ArgumentException(Resources.ParameterError.WrongSize, matrixName);
		if (matrix.ValueStorages.Length != 1 || matrix.ValueStorages[0] is not PureStorage<T, CpuMemoryPointer> ps)
			return false;
		pointer = ps.Pointer.Pointer.UnmangedPointer<T>(ps.Pointer.OffsetInBytes);
		if (pointer == default)
			throw new ArgumentException(Resources.ParameterError.InvalidValue, matrixName);
		nnz = ps.Length;
		if (matrix.IndexStorages.Length != 2 || matrix.IndexStorages[0] is not PureStorage<TInd, CpuMemoryPointer> psRow || matrix.IndexStorages[1] is not PureStorage<TInd, CpuMemoryPointer> psCol)
			return false;
		pointerRow = psRow.Pointer.Pointer.UnmangedPointer<TInd>(psRow.Pointer.OffsetInBytes);
		pointerCol = psCol.Pointer.Pointer.UnmangedPointer<TInd>(psCol.Pointer.OffsetInBytes);
		if (pointerRow == null || pointerCol == null)
			throw new ArgumentException(Resources.ParameterError.InvalidValue, matrixName);
		return true;
	}
}
