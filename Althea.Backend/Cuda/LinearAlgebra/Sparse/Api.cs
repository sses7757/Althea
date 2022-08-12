using Althea.Array;
using Althea.Backend.Cuda.Storage;
using Althea.Backend.Mkl;
using Althea.LinearAlgebra.Sparse;
using Althea.Linq;

using static Althea.Backend.Cuda.MemoryPointerChecker;

using NM = Althea.Backend.Cuda.LinearAlgebra.Sparse.NativeMethods;
using NMC = Althea.Backend.Cuda.LinearAlgebra.Sparse.CustomNativeMethods;


namespace Althea.Backend.Cuda.LinearAlgebra.Sparse;

/// <summary>
/// The CUDA back-end of the sparse linear algebra <see cref="IConversionAbstractApi"/>, <see cref="IComputationAbstractApi"/> and <see cref="IIndexOperationAbstractApi"/> that utilizes cuSPARSE and custom CUDA functions.
/// </summary>
/// <remarks>CUDA stream and blocked-ELL sparse matrix format are not supported yet but can be easily added.</remarks>
public unsafe class Api : IBindedDevice, IConversionAbstractApi, IComputationAbstractApi, IIndexOperationAbstractApi
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

	#region vector conversion
	/// <inheritdoc/>
	public virtual bool VectorSetValuesAt<T, TInd, TS, TSInd>(TS x, T value, TSInd positions) where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd> where TS : class, IStorage<T, TS> where TSInd : class, IStorage<TInd, TSInd>
	{
		if (!GetPointer(this, x, positions, out T* px, out TInd* pp, out var n))
			return false;
		if (sizeof(TInd) != sizeof(MklInt))
			return false;
		return NMC.vecSetValAt(T.Type, px, &value, (MklInt*)pp, n) >= 0;
	}

	/// <inheritdoc/>
	public virtual bool VectorSetValuesAt<T, TInd, TS1, TS2, TSInd>(TS1 x, TS2 values, TSInd positions) where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TSInd : class, IStorage<TInd, TSInd>
	{
		if (!GetPointer(this, values, positions, out T* px, out TInd* pp, out var n))
			return false;
		if (!GetPointer(x, out T* py, out var n2))
			return false;
		if (n2 < n)
			throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(values));

		using var dn = DenseVectorWrapper.Create(py, n2, out bool success);
		if (!success) return false;
		using var sp = SparseVectorWrapper.Create(px, pp, n2, n, out success);
		if (!success) return false;
		return NM.cusparseScatter(this.cusparseHandle, sp, dn).Check();
	}

	/// <inheritdoc/>
	public virtual bool VectorGatherValuesAt<T, TInd, TS1, TS2, TSInd>(TS1 x, TS2 values, TSInd positions) where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TSInd : class, IStorage<TInd, TSInd>
	{
		if (!GetPointer(this, values, positions, out T* px, out TInd* pp, out var n))
			return false;
		if (!GetPointer(x, out T* py, out var n2))
			return false;
		if (n2 < n)
			throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(values));

		using var dn = DenseVectorWrapper.Create(py, n2, out bool success);
		if (!success) return false;
		using var sp = SparseVectorWrapper.Create(px, pp, n2, n, out success);
		if (!success) return false;
		return NM.cusparseGather(this.cusparseHandle, dn, sp).Check();
	}

	/// <inheritdoc/>
	public virtual bool VectorSparseToDense<T, TInd, TS1, TS2, TSInd>(ISparseArray<T, TInd, TS1, TSInd> x, TS2 y, long strideY) where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TSInd : class, IStorage<TInd, TSInd>
	{
		if (!GetPointer(y, out T* py, out var n))
			return false;
		using var sp = SparseVectorWrapper.Create(this, x, out bool success);
		if (!success) return false;
		using var dn = DenseVectorWrapper.Create(py, n, out success);
		if (!success) return false;
		return NM.cusparseScatter(this.cusparseHandle, sp, dn).Check();
	}

	/// <inheritdoc/>
	public virtual bool VectorDenseToSparse<T, TInd, TS1, TS2, TSInd>(TS1 x, long strideX, ref SparseArrayWrapper<T, TInd, TS2, TSInd> y, double threshold = 0) where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd> where TS2 : class, IStorage<T, TS2> where TS1 : class, IStorage<T, TS1> where TSInd : class, IStorage<TInd, TSInd>
	{
		if (sizeof(TInd) != sizeof(MklInt))
			return false;
		if (strideX != 1 || y.Format != SparseFormat.VectorCooFormat || y.DefaultValue != T.Zero)
			return false;
		if (!GetPointer(this, x, 1, out T* px, out var n))
			return false;
		T thre = threshold.As<T>();
		if (y.Size.IsEmpty || y.Size[0] == 0)
		{   // create y
			if (y.ValueStorages.Length is not 0 and not 1 || y.IndexStorages.Length is not 0 and not 1)
				throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(y));
			if ((!y.ValueStorages.IsEmpty && !y.ValueStorages[0].IsValid()) || (!y.IndexStorages.IsEmpty && !y.IndexStorages[0].IsValid()))
				return false;
			long bufSize = NMC.vecPruneBuffer(T.Type, n);
			if (bufSize < 0)
				return false;
			using var buf = CudaBuffer.Create(bufSize, 0, false);
			long nnz = NMC.vecPruneNnz(T.Type, px, &thre, n, buf);
			var valOut = TS2.Create(stackalloc[] { nnz });
			var idxOut = TSInd.Create(stackalloc[] { nnz });
			GetPointer(valOut, out T* outVal, out _);
			GetPointer(idxOut, out TInd* outIdx, out _);
			_ = NMC.vecPruneCal(T.Type, n, buf, nnz, (MklInt*)outIdx, outVal);
			y.SetValues(n, valOut, idxOut);
		}
		else
		{   // in-place modify y
			if (y.Size[0] != x.Length)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(y));
			if (y.ValueStorages.Length != 1 || y.IndexStorages.Length != 1)
				throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(y));
			if (y.ValueStorages.Length != 1 || y.IndexStorages.Length != 1)
				throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(y));
			if (!GetPointer(this, y.ValueStorages[0], y.IndexStorages[0], out T* valOut, out TInd* idxOut, out var nnz))
				return false;
			long diff = NMC.vecPruneDirect(T.Type, px, &thre, n, (MklInt*)idxOut, valOut, true, nnz);
			if (diff != 0) // cannot extend existing pointers
				return false;
		}
		return true;
	}
	#endregion

	#region vector matrix conversion
	/// <inheritdoc/>
	public virtual bool SparseVectorToMatrix<T, TInd1, TInd2, TS1, TS2, TSInd1, TSInd2>(ISparseArray<T, TInd1, TS1, TSInd1> vector, ref SparseArrayWrapper<T, TInd2, TS2, TSInd2> target) where T : unmanaged, IBaseNumber<T> where TInd1 : unmanaged, IBinaryInt<TInd1> where TS1 : class, IStorage<T, TS1> where TSInd1 : class, IStorage<TInd1, TSInd1> where TInd2 : unmanaged, IBinaryInt<TInd2> where TS2 : class, IStorage<T, TS2> where TSInd2 : class, IStorage<TInd2, TSInd2>
	{
		if (sizeof(TInd1) != sizeof(MklInt))
			return false;
		if (typeof(TInd1) != typeof(TInd2))
			return false;
		if (vector.Format != SparseFormat.VectorCooFormat || vector.DefaultValue != T.Zero || (target.Format & SparseFormat.MatrixCocFormat) == SparseFormat.None || target.DefaultValue != T.Zero)
			return false;
		if (vector.Size.Length != 1)
			throw new ArgumentException(Resources.ParameterError.InvalidRank, nameof(vector));
		if (target.Size.Length != 2 || target.Size.Prod() != vector.Size[0])
			throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(target));
		if (vector.IndexStorages.Length != 1 || vector.ValueStorages.Length != 1)
			throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(vector));
		if (!GetPointer(this, vector, out T* px, out TInd1* pp, out var nnz))
			return false;
		if (vector.Size[0] < nnz)
			throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(vector));
		if (target.ValueStorages.Length is not 0 and not 1 || target.IndexStorages.Length is not 0 and not 2)
			throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(target));

		TS2 valOut; T* pVal;
		TSInd2 rowIdxOut, colIdxOut; TInd2* pRow, pCol;
		if (target.ValueStorages.Length == 1)
		{
			valOut = target.ValueStorages[0];
			if (!GetPointer(valOut, out pVal, out var n2) || n2 != nnz)
				return false;
		}
		else
		{
			valOut = TS2.Create(stackalloc[] { nnz });
			if (!GetPointer(valOut, out pVal, out _))
			{
				valOut.Dispose();
				return false;
			}
		}
		if (target.IndexStorages.Length == 2)
		{
			rowIdxOut = target.IndexStorages[0]; colIdxOut = target.IndexStorages[1];
			if (!GetPointer(rowIdxOut, out pRow, out var n2) || n2 != nnz)
				return false;
			if (!GetPointer(colIdxOut, out pCol, out n2) || n2 != nnz)
				return false;
		}
		else
		{
			rowIdxOut = TSInd2.Create(stackalloc[] { nnz });
			if (!GetPointer(rowIdxOut, out pRow, out _))
			{
				rowIdxOut.Dispose();
				return false;
			}
			colIdxOut = TSInd2.Create(stackalloc[] { nnz });
			GetPointer(colIdxOut, out pCol, out _);
		}
		Storage.NativeMethods.cudaMemcpy(pVal, px, nnz * sizeof(T), MemoryCopyKind.DeviceToDevice);
		NMC.spVecIdxToCooIdxs((MklInt*)pp, (MklInt*)pRow, (MklInt*)pCol, nnz, vector.Size[0]);
		target.SetValues(valOut, rowIdxOut, colIdxOut);
		target.Format = SparseFormat.MatrixCocFormat;
		target.DefaultValue = T.Zero;
		return true;
	}

	/// <inheritdoc/>
	public virtual bool SparseMatrixToVector<T, TInd1, TInd2, TS1, TS2, TSInd1, TSInd2>(ISparseArray<T, TInd1, TS1, TSInd1> matrix, ref SparseArrayWrapper<T, TInd2, TS2, TSInd2> target) where T : unmanaged, IBaseNumber<T> where TInd1 : unmanaged, IBinaryInt<TInd1> where TS1 : class, IStorage<T, TS1> where TSInd1 : class, IStorage<TInd1, TSInd1> where TInd2 : unmanaged, IBinaryInt<TInd2> where TS2 : class, IStorage<T, TS2> where TSInd2 : class, IStorage<TInd2, TSInd2>
	{
		if (sizeof(TInd1) != sizeof(MklInt))
			return false;
		if (typeof(TInd1) != typeof(TInd2))
			return false;
		if ((target.Format & SparseFormat.VectorCooFormat) == SparseFormat.None || target.DefaultValue != T.Zero || matrix.Format != SparseFormat.MatrixCocFormat || matrix.DefaultValue != T.Zero)
			return false;
		if (target.Size.Length is not 0 and not 1)
			throw new ArgumentException(Resources.ParameterError.InvalidRank, nameof(target));
		if (matrix.Size.Length != 2)
			throw new ArgumentException(Resources.ParameterError.InvalidRank, nameof(matrix));
		if (!GetPointer(this, matrix, out T* pm, out var pr, out var pc, out var nnz))
			return false;
		if (matrix.ValueStorages.Length != 1 || matrix.IndexStorages.Length != 2)
			throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(matrix));
		if (target.IndexStorages.Length is not 0 and not 1 || target.ValueStorages.Length is not 0 and not 1)
			throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(target));

		TS2 valOut; T* pVal;
		TSInd2 idxOut; TInd2* pIdx;
		if (target.ValueStorages.Length == 1)
		{
			valOut = target.ValueStorages[0];
			if (!GetPointer(valOut, out pVal, out var n2) || n2 != nnz)
				return false;
		}
		else
		{
			valOut = TS2.Create(stackalloc[] { nnz });
			if (!GetPointer(valOut, out pVal, out _))
			{
				valOut.Dispose();
				return false;
			}
		}
		if (target.IndexStorages.Length == 2)
		{
			idxOut = target.IndexStorages[0];
			if (!GetPointer(idxOut, out pIdx, out var n2) || n2 != nnz)
				return false;
		}
		else
		{
			idxOut = TSInd2.Create(stackalloc[] { nnz });
			if (!GetPointer(idxOut, out pIdx, out _))
			{
				idxOut.Dispose();
				return false;
			}
		}
		Storage.NativeMethods.cudaMemcpy(pVal, pm, nnz * sizeof(T), MemoryCopyKind.DeviceToDevice);
		NMC.cooIdxsToSpVecIdx((MklInt*)pIdx, (MklInt*)pr, (MklInt*)pc, nnz, matrix.Size[0]);
		target.SetValues(matrix.Size.Prod(), valOut, idxOut);
		target.Format = SparseFormat.VectorCooFormat;
		target.DefaultValue = T.Zero;
		return true;
	}
	#endregion

	#region matrix conversion
	/// <inheritdoc/>
	public virtual bool MatrixSparseToDense<T, TInd, TS1, TS2, TSInd>(ISparseArray<T, TInd, TS1, TSInd> source, TS2 destination, long ld) where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd> where TS1 : class, IStorage<T, TS1> where TSInd : class, IStorage<TInd, TSInd> where TS2 : class, IStorage<T, TS2>
	{
		if (!GetPointer(this, destination, source.Size[0], source.Size[1], ld, out T* pd))
			return false;
		using var sp = SparseMatrixWrapper.Create(this, source, out bool success);
		if (!success) return false;
		using var dn = DenseMatrixWrapper.Create(pd, source.Size[0], source.Size[1], ld, false, out success);
		if (!success) return false;
		if (!NM.cusparseSparseToDense_bufferSize(this.cusparseHandle, sp, dn, SparseToDenseAlgorithm.Default, out long bufSize).Check())
			return false;
		using var buf = CudaBuffer.Create(bufSize, 0, false);
		return NM.cusparseSparseToDense(this.cusparseHandle, sp, dn, SparseToDenseAlgorithm.Default, buf.DeviceBuffer).Check();
	}

	/// <inheritdoc/>
	public virtual bool MatrixDenseToSparse<T, TInd, TS1, TS2, TSInd>(TS1 source, long ld, ref SparseArrayWrapper<T, TInd, TS2, TSInd> target, double threshold = 0) where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd> where TS1 : class, IStorage<T, TS1> where TSInd : class, IStorage<TInd, TSInd> where TS2 : class, IStorage<T, TS2>
	{
		if (sizeof(TInd) != sizeof(int) && sizeof(TInd) != sizeof(long))
			return false;
		if (target.DefaultValue != T.Zero)
			return false;
		if (!GetPointer(this, source, target.Size[0], target.Size[1], ld, out T* ps))
			return false;
		if ((target.Format & NM.SupportFormat) == SparseFormat.None)
			return false;
		if (target.ValueStorages.Length != 0 || target.ValueStorages[0] is not null || target.IndexStorages.Length is not 0 and not 2)
			return false; // not support in-place
		if (target.Format.Class != SparseFormat.Type.Compressed &&
			(threshold != 0 || sizeof(TInd) != sizeof(int) ||
			(typeof(T) != typeof(Float16) && typeof(T) != typeof(Float32) && typeof(T) != typeof(Float64))))
			return false; // no prune supported for other formats
		var format = target.Format;
		if ((format.Class & SparseFormat.Type.Compressed) != 0)
			format = format.WithCompressed;
		if (format.IsRowMajor)
			format = format.WithRowMajor;
		if ((format.BlockType & SparseFormat.Blocking.Element) != 0)
			format = format.WithElementBlocking;
		long rows = target.Size[0], cols = target.Size[1];

		TS2 valOut;
		TSInd rowOut = TSInd.Empty, colOut = TSInd.Empty;
		T* outVal = null;
		TInd* outRow = null, outCol = null;
		if (format.IsRowMajor)
		{
			if (target.IndexStorages.Length == 0 || target.IndexStorages[0] is null)
			{
				rowOut = TSInd.Create(stackalloc[] { target.Size[0] + 1 });
				if (!GetPointer(rowOut, out outRow, out _))
				{
					rowOut.Dispose();
					return false;
				}
			}
			else
			{
				rowOut = target.IndexStorages[0];
				if (!GetPointer(rowOut, out outRow, out _))
					return false;
			}
		}
		else
		{
			if (target.IndexStorages.Length == 0 || target.IndexStorages[1] is null)
			{
				colOut = TSInd.Create(stackalloc[] { target.Size[1] + 1 });
				if (!GetPointer(colOut, out outCol, out _))
				{
					colOut.Dispose();
					return false;
				}
			}
			else
			{
				colOut = target.IndexStorages[1];
				if (!GetPointer(colOut, out outCol, out _))
					return false;
			}
		}
		using var sp = SparseMatrixWrapper.Create(format, rows, cols, 0, outVal, outRow, outCol, out bool success);
		if (!success) return false;
		if (threshold == 0)
		{
			using var dn = DenseMatrixWrapper.Create(ps, rows, cols, ld, false, out success);
			if (!success) return false;
			if (!NM.cusparseDenseToSparse_bufferSize(this.cusparseHandle, dn, sp, DenseToSparseAlgorithm.Default, out long bufSize).Check())
				return false;
			using var buf = CudaBuffer.Create(bufSize, 0, false);
			if (!NM.cusparseDenseToSparse_analysis(this.cusparseHandle, dn, sp, DenseToSparseAlgorithm.Default, buf.DeviceBuffer).Check())
				return false;
			if (!sp.GetSizes(out _, out _, out long nnz))
				return false;
			if (format.IsRowMajor)
			{
				colOut = TSInd.Create(stackalloc[] { nnz });
				if (!GetPointer(colOut, out outCol, out _))
				{
					colOut.Dispose();
					return false;
				}
			}
			else
			{
				rowOut = TSInd.Create(stackalloc[] { nnz });
				if (!GetPointer(rowOut, out outRow, out _))
				{
					rowOut.Dispose();
					return false;
				}
			}
			valOut = TS2.Create(stackalloc[] { nnz });
			if (!GetPointer(valOut, out outVal, out _))
			{
				valOut.Dispose();
				return false;
			}
			if (!sp.SetPointers(format, outVal, outRow, outCol))
				return false;
			if (!NM.cusparseDenseToSparse_convert(this.cusparseHandle, dn, sp, DenseToSparseAlgorithm.Default, buf.DeviceBuffer).Check())
				return false;
		}
		else
		{
			T thre = threshold.As<T>();
			int nrows = (int)rows, ncols = (int)cols, lld = (int)ld;
			delegate*<IntPtr, int, int, T*, int, T*, SparseMatrixWrapper, int*, out int, void*, CudaSparseStatus> calcNnz = default(T) switch
			{
				Float16 => &NM.cusparseHpruneDense2csrNnz,
				Float32 => &NM.cusparseSpruneDense2csrNnz,
				Float64 => &NM.cusparseDpruneDense2csrNnz,
				_ => null
			};
			delegate*<IntPtr, int, int, T*, int, T*, SparseMatrixWrapper, T*, int*, int*, out long, CudaSparseStatus> getBufSize = default(T) switch
			{
				Float16 => &NM.cusparseHpruneDense2csr_bufferSizeExt,
				Float32 => &NM.cusparseSpruneDense2csr_bufferSizeExt,
				Float64 => &NM.cusparseDpruneDense2csr_bufferSizeExt,
				_ => null
			};
			delegate*<IntPtr, int, int, T*, int, T*, SparseMatrixWrapper, T*, int*, int*, void*, CudaSparseStatus> prune = default(T) switch
			{
				Float16 => &NM.cusparseHpruneDense2csr,
				Float32 => &NM.cusparseSpruneDense2csr,
				Float64 => &NM.cusparseDpruneDense2csr,
				_ => null
			};
			if (!format.IsRowMajor)
			{
				(rowOut, colOut) = (colOut, rowOut);
				var temp = outRow; outRow = outCol; outCol = temp;
			}
			if (!getBufSize(this.cusparseHandle, nrows, ncols, ps, lld, &thre, sp, outVal, (int*)outRow, (int*)outCol, out long bufSize).Check())
				return false;
			using var buf = CudaBuffer.Create(bufSize, 0, false);
			if (!calcNnz(this.cusparseHandle, nrows, ncols, ps, lld, &thre, sp, (int*)outRow, out int nnz, buf.DeviceBuffer).Check())
				return false;
			colOut = TSInd.Create(stackalloc long[] { nnz });
			if (!GetPointer(colOut, out outCol, out _))
			{
				colOut.Dispose();
				return false;
			}
			valOut = TS2.Create(stackalloc long[] { nnz });
			if (!GetPointer(valOut, out outVal, out _))
			{
				valOut.Dispose();
				return false;
			}
			if (!prune(this.cusparseHandle, nrows, ncols, ps, lld, &thre, sp, outVal, (int*)outRow, (int*)outCol, buf.DeviceBuffer).Check())
				return false;
		}
		target.Format = format;
		target.SetValues(valOut, rowOut, colOut);
		return true;
	}
	#endregion
}
