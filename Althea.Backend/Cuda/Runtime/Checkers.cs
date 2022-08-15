using System.Runtime.CompilerServices;

using Althea.Array;
using Althea.Linq;

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
	private static bool GetPointerInner<T, TS>(TS s, out T* pointer, out long length, string? sName = null) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
	{
		var ps = Unsafe.As<TS, PureStorage<T, CudaMemoryPointer>>(ref Unsafe.AsRef(in s)).Pointer;
		pointer = ps.Pointer.UnmangedPointer<T>(ps.OffsetInBytes);
		if (pointer == default)
			throw new ArgumentException(Resources.ParameterError.InvalidValue, sName);
		length = ps.LengthInBytes / sizeof(T);
		if (length < 0)
			return false;
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool GetPointerInner<T, TS>(TS s, long stride, out T* pointer, out int length, out int inc, string? sName = null, string? strideName = null) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
	{
		if (stride <= 0)
			throw new ArgumentOutOfRangeException(strideName, strideName, Resources.ParameterError.MustPositive);
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
	private static bool GetPointerInnerLong<T, TS>(TS s, long stride, out T* pointer, out long length, string? sName = null, string? strideName = null) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
	{
		if (stride <= 0)
			throw new ArgumentOutOfRangeException(strideName, strideName, Resources.ParameterError.MustPositive);
		var ps = Unsafe.As<TS, PureStorage<T, CudaMemoryPointer>>(ref Unsafe.AsRef(in s)).Pointer;
		pointer = ps.Pointer.UnmangedPointer<T>(ps.OffsetInBytes);
		if (pointer == default)
			throw new ArgumentException(Resources.ParameterError.InvalidValue, sName);
		length = ps.LengthInBytes / sizeof(T);
		length = (length - 1) / stride + 1;
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static unsafe bool GetPointerInner<T, TS>(TS s, long m, long n, long ld, out T* pointer, string? sName = null, string? mName = null, string? nName = null, string? ldName = null) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
	{
		if (m <= 0)
			throw new ArgumentOutOfRangeException(mName, m, Resources.ParameterError.MustPositive);
		if (n <= 0)
			throw new ArgumentOutOfRangeException(nName, n, Resources.ParameterError.MustPositive);
		if (ld < m)
			throw new ArgumentOutOfRangeException(ldName, ld, Resources.ParameterError.InvalidValue);
		pointer = default;
		if (m > int.MaxValue || n > int.MaxValue || ld > int.MaxValue)
			return false;
		if (s is not PureStorage<T, CudaMemoryPointer> ps)
			return false; // not support
		pointer = ps.Pointer.Pointer.UnmangedPointer<T>(ps.Pointer.OffsetInBytes);
		if (pointer == default)
			throw new ArgumentException(Resources.ParameterError.InvalidValue, sName);
		if ((ps.Length + (ld - m)) / ld < n)
			throw new ArgumentException(Resources.ParameterError.WrongSize);
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static unsafe bool GetPointerLongInner<T, TS>(TS s, long m, long n, long ld, out T* pointer, string? sName = null, string? mName = null, string? nName = null, string? ldName = null) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
	{
		if (m <= 0)
			throw new ArgumentOutOfRangeException(mName, m, Resources.ParameterError.MustPositive);
		if (n <= 0)
			throw new ArgumentOutOfRangeException(nName, n, Resources.ParameterError.MustPositive);
		if (ld < m)
			throw new ArgumentOutOfRangeException(ldName, ld, Resources.ParameterError.InvalidValue);
		pointer = default;
		if (s is not PureStorage<T, CudaMemoryPointer> ps)
			return false; // not support
		pointer = ps.Pointer.Pointer.UnmangedPointer<T>(ps.Pointer.OffsetInBytes);
		if (pointer == default)
			throw new ArgumentException(Resources.ParameterError.InvalidValue, sName);
		if ((ps.Length + (ld - m)) / ld < n)
			throw new ArgumentException(Resources.ParameterError.WrongSize);
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool GetPointerInner<T, TInd, TS, TSInd>(TS s, TSInd sInd, out T* pointer, out TInd* pointerInd, out long length, string? sName = null, string? sIndName = null) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS> where TInd : unmanaged, IBinaryInt<TInd> where TSInd : class, IStorage<TInd, TSInd>
	{
		var ps = Unsafe.As<TS, PureStorage<T, CudaMemoryPointer>>(ref Unsafe.AsRef(in s)).Pointer;
		var psInd = Unsafe.As<TSInd, PureStorage<TInd, CudaMemoryPointer>>(ref Unsafe.AsRef(in sInd)).Pointer;
		pointer = ps.Pointer.UnmangedPointer<T>(ps.OffsetInBytes);
		if (pointer == default)
			throw new ArgumentException(Resources.ParameterError.InvalidValue, sName);
		pointerInd = psInd.Pointer.UnmangedPointer<TInd>(psInd.OffsetInBytes);
		if (pointerInd == default)
			throw new ArgumentException(Resources.ParameterError.InvalidValue, sIndName);
		length = ps.LengthInBytes / sizeof(T);
		if (length < 0)
			return false;
		if (psInd.LengthInBytes != ps.LengthInBytes)
			throw new ArgumentException(Resources.ParameterError.NotSameSize, sIndName);
		return true;
	}


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static unsafe bool GetPointer<T, TInd, TS, TSInd>(IBindedDevice api, ISparseArray<T, TInd, TS, TSInd> vector, out T* pointer, out TInd* pointerInd, out long nnz, [CallerArgumentExpression("vector")] string? vectorName = null) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS> where TInd : unmanaged, IBinaryInt<TInd> where TSInd : class, IStorage<TInd, TSInd>
	{
		pointer = default; pointerInd = default; nnz = 0;
		if (api.BindedDeviceID != Runtime.CurrentDeviceID)
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
		if (!TInd.Type.IsInteger() || (sizeof(TInd) != sizeof(int) && sizeof(TInd) != sizeof(long)))
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
		var psVal = Unsafe.As<TS, PureStorage<T, CudaMemoryPointer>>(ref Unsafe.AsRef(in matrix.ValueStorages[0])).Pointer;
		var psInd1 = Unsafe.As<TSInd, PureStorage<TInd, CudaMemoryPointer>>(ref Unsafe.AsRef(in matrix.IndexStorages[0])).Pointer;
		var psInd2 = Unsafe.As<TSInd, PureStorage<TInd, CudaMemoryPointer>>(ref Unsafe.AsRef(in matrix.IndexStorages[1])).Pointer;
		pointer = psVal.Pointer.UnmangedPointer<T>(psVal.OffsetInBytes);
		if (pointer == default)
			throw new ArgumentException(Resources.ParameterError.InvalidValue, matrixName);
		pointerRow = psInd1.Pointer.UnmangedPointer<TInd>(psInd1.OffsetInBytes);
		if (pointerRow == default)
			throw new ArgumentException(Resources.ParameterError.InvalidValue, matrixName);
		pointerCol = psInd2.Pointer.UnmangedPointer<TInd>(psInd2.OffsetInBytes);
		if (pointerCol == default)
			throw new ArgumentException(Resources.ParameterError.InvalidValue, matrixName);
		nnz = psVal.LengthInBytes / sizeof(T);
		long rowLen = psInd1.LengthInBytes / sizeof(TInd), colLen = psInd2.LengthInBytes / sizeof(TInd);
		if (nnz < 0)
			return false;
		////if ((matrix.Format.Class == SparseFormat.Type.Coordinated && (nnz != rowLen || nnz != colLen)) ||
		////	(matrix.Format == SparseFormat.MatrixCscFormat && (nnz != rowLen || matrix.Size[1] != colLen)) ||
		////	(matrix.Format == SparseFormat.MatrixCsrFormat && (matrix.Size[0] != rowLen || nnz != colLen)))
		////	throw new ArgumentException(Resources.ParameterError.NotSameSize, matrixName);
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static unsafe bool GetPointerIncludeBlocked<T, TInd, TS, TSInd>(IBindedDevice api, ISparseArray<T, TInd, TS, TSInd> matrix, out T* pointer, out TInd* pointerRow, out TInd* pointerCol, out long nnz, [CallerArgumentExpression("matrix")] string? matrixName = null) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS> where TInd : unmanaged, IBinaryInt<TInd> where TSInd : class, IStorage<TInd, TSInd>
	{
		pointer = default; pointerRow = pointerCol = default; nnz = 0;
		if (api.BindedDeviceID != Runtime.CurrentDeviceID)
			return false;
		if (!TInd.Type.IsInteger() || (sizeof(TInd) != sizeof(int) && sizeof(TInd) != sizeof(long)))
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
		var psVal = Unsafe.As<TS, PureStorage<T, CudaMemoryPointer>>(ref Unsafe.AsRef(in matrix.ValueStorages[0])).Pointer;
		var psInd1 = Unsafe.As<TSInd, PureStorage<TInd, CudaMemoryPointer>>(ref Unsafe.AsRef(in matrix.IndexStorages[0])).Pointer;
		var psInd2 = Unsafe.As<TSInd, PureStorage<TInd, CudaMemoryPointer>>(ref Unsafe.AsRef(in matrix.IndexStorages[1])).Pointer;
		pointer = psVal.Pointer.UnmangedPointer<T>(psVal.OffsetInBytes);
		if (pointer == default)
			throw new ArgumentException(Resources.ParameterError.InvalidValue, matrixName);
		pointerRow = psInd1.Pointer.UnmangedPointer<TInd>(psInd1.OffsetInBytes);
		if (pointerRow == default)
			throw new ArgumentException(Resources.ParameterError.InvalidValue, matrixName);
		pointerCol = psInd2.Pointer.UnmangedPointer<TInd>(psInd2.OffsetInBytes);
		if (pointerCol == default)
			throw new ArgumentException(Resources.ParameterError.InvalidValue, matrixName);
		nnz = psVal.LengthInBytes / sizeof(T);
		long rowLen = psInd1.LengthInBytes / sizeof(TInd), colLen = psInd2.LengthInBytes / sizeof(TInd);
		if (nnz < 0)
			return false;
		////if ((matrix.Format.Class == SparseFormat.Type.Coordinated && (nnz != rowLen || nnz != colLen)) ||
		////	(matrix.Format == SparseFormat.MatrixCscFormat && (nnz != rowLen || matrix.Size[1] != colLen)) ||
		////	(matrix.Format == SparseFormat.MatrixCsrFormat && (matrix.Size[0] != rowLen || nnz != colLen)) ||
		////	(matrix.Format == SparseFormat.MatrixBscFormat && (nnz / matrix.BlockSize.Prod() != rowLen || matrix.Size[1] / matrix.BlockSize[1] != colLen)) ||
		////	(matrix.Format == SparseFormat.MatrixBsrFormat && (nnz / matrix.BlockSize.Prod() != colLen || matrix.Size[0] / matrix.BlockSize[0] != rowLen)))
		////	throw new ArgumentException(Resources.ParameterError.NotSameSize, matrixName);
		return true;
	}
}
