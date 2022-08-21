using Althea.LinearAlgebra.Sparse;

using static Althea.Backend.Cuda.MemoryPointerChecker;

using NMC = Althea.Backend.Cuda.LinearAlgebra.Sparse.CustomNativeMethods;


namespace Althea.Backend.Cuda.LinearAlgebra.Sparse;

/// <summary>
/// The CUDA back-end of the sparse linear algebra <see cref="IConversionAbstractApi"/>, <see cref="IComputationAbstractApi"/> and <see cref="IIndexOperationAbstractApi"/> that utilizes cuSPARSE and custom CUDA functions.
/// </summary>
/// <remarks>CUDA stream and blocked-ELL sparse matrix format are not supported yet but can be easily added.</remarks>
public unsafe partial class Api : IBindedDevice, IConversionAbstractApi, IComputationAbstractApi, IIndexOperationAbstractApi
{
	#region basic
	/// <summary>
	/// The actual CUDA library handle used in its API calls
	/// </summary>
	protected readonly IntPtr cusparseHandle;

	/// <inheritdoc/>
	public int BindedDeviceID { get; }

	/// <summary>
	/// Get or set a <see cref="bool"/> to indicate whether this implementation shall use the polar decomposition to perform the singular value decomposition or the legacy QR decomposition to do so.
	/// </summary>
	/// <remarks>The polar decomposition approach is much faster but may leads to larger error(s) when the matrix to be decomposed is (near) singularity.</remarks>
	public bool SvdViaPolarDecomposition { get; set; }

	/// <summary>
	/// The default constructor of <see cref="Api"/>
	/// </summary>
	public Api()
	{
		this.BindedDeviceID = Runtime.CurrentDeviceID;
		NativeMethods.cusparseCreate(out this.cusparseHandle).Check();
		NativeMethods.cusparseSetPointerMode(this.cusparseHandle).Check();
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		NativeMethods.cusparseDestroy(this.cusparseHandle);
		this.Disposed = true;
		GC.SuppressFinalize(this);
	}

	/// <inheritdoc/>
	public bool Disposed { get; protected set; } = false;
	#endregion

	#region index operation
	/// <inheritdoc/>
	public virtual bool Sort<T, TS>(TS array, long stride) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
	{
		if (!GetPointer(this, array, stride, out T* ptr, out var n))
			return false;
		return NMC.vecSort(T.Type, n, ptr, (int)stride) == 0;
	}

	/// <inheritdoc/>
	public virtual bool Sort<T, TOther, TS, TS2>(TS keys, long strideKeys, TS2 values, long strideValues) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS> where TOther : unmanaged, IBaseNumber<TOther> where TS2 : class, IStorage<TOther, TS2>
	{
		if (!GetPointer(this, keys, strideKeys, out T* pk, out var n))
			return false;
		if (!GetPointer(this, values, strideValues, out TOther* pv, out var n2))
			return false;
		if (n2 != n)
			throw new ArgumentException(Resources.ParameterError.NotSameSize);
		return NMC.vecSortBy(T.Type, TOther.Type, n, pk, strideKeys, pv, strideValues) == 0;
	}

	/// <inheritdoc/>
	public virtual bool IndexOf<T, TS>(TS array, long stride, bool sorted, T value, out long find) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
	{
		find = -1;
		if (!GetPointer(this, array, stride, out T* ptr, out var n))
			return false;
		return NMC.vecFind(T.Type, sorted, n, ptr, stride, &value, out find) == 0;
	}

	/// <inheritdoc/>
	public virtual bool BoundOf<T, TS>(TS array, long stride, T value, bool lowerBound, out long index) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
	{
		index = -1;
		if (!GetPointer(this, array, stride, out T* ptr, out var n))
			return false;
		return NMC.vecBound(T.Type, lowerBound, n, ptr, stride, &value, out index) == 0;
	}

	/// <inheritdoc/>
	public virtual bool FillWithRange<T, TS>(TS array, long stride, T start, T step) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
	{
		if (!GetPointer(this, array, stride, out T* ptr, out var n))
			return false;
		return NMC.vecFillRange(T.Type, n, ptr, stride, &start, &step) == 0;
	}
	#endregion
}
