using System.Runtime.CompilerServices;

using Althea.Array;
using Althea.Backend.Cuda.Storage;
using Althea.Helpers;
using Althea.LinearAlgebra;
using Althea.LinearAlgebra.Sparse;

using static Althea.Backend.Cuda.MemoryPointerChecker;

using NM = Althea.Backend.Cuda.LinearAlgebra.Sparse.NativeMethods;
using NMC = Althea.Backend.Cuda.LinearAlgebra.Sparse.CustomNativeMethods;


namespace Althea.Backend.Cuda.LinearAlgebra.Sparse;

public unsafe partial class Api
{
	#region vector conversion
	/// <inheritdoc/>
	public virtual bool VectorSetValuesAt<T, TInd, TS, TSInd>(TS x, T value, TSInd positions) where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd> where TS : class, IStorage<T, TS> where TSInd : class, IStorage<TInd, TSInd>
	{
		if (!GetPointer(this, x, positions, out T* px, out TInd* pp, out var n))
			return false;
		return sizeof(TInd) == sizeof(int) ? NMC.vecSetValAt_i32(T.Type, px, &value, (int*)pp, n) >= 0 : NMC.vecSetValAt_i64(T.Type, px, &value, (long*)pp, n) >= 0;
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
		if (strideX != 1 || y.Format != SparseFormat.VectorCooFormat || y.DefaultValue != T.Zero)
			return false;
		if (!GetPointer(this, x, out T* px, out var n))
			return false;
		T thre = threshold.As<T>();
		if (y.Size.IsEmpty || y.Size[0] == 0)
		{   // create y
			if (y.ValueStorages.Length is not 0 and not 1 || y.IndexStorages.Length is not 0 and not 1)
				throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(y));
			if ((!y.ValueStorages.IsEmpty && !y.ValueStorages[0].IsValid()) || (!y.IndexStorages.IsEmpty && !y.IndexStorages[0].IsValid()))
				return false;
			long bufSize = sizeof(TInd) == sizeof(int) ? NMC.vecPruneBuffer_i32(T.Type, n) : NMC.vecPruneBuffer_i64(T.Type, n);
			if (bufSize < 0)
				return false;
			using var buf = CudaBuffer.Create(bufSize);
			long nnz = sizeof(TInd) == sizeof(int) ? NMC.vecPruneNnz_i32(T.Type, px, &thre, n, buf) : NMC.vecPruneNnz_i64(T.Type, px, &thre, n, buf);
			bool hasError = true;
			using var valOut = nnz.Create<T, TS2>(ref hasError);
			if (valOut.Invalid) return false;
			using var idxOut = nnz.Create<TInd, TSInd>(ref hasError);
			if (idxOut.Invalid) return false;
			var err = sizeof(TInd) == sizeof(int) ? NMC.vecPruneCal_i32(T.Type, n, buf, nnz, (int*)(TInd*)idxOut, valOut) : NMC.vecPruneCal_i64(T.Type, n, buf, nnz, (long*)(TInd*)idxOut, valOut);
			if (err != 0)
				return false;
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
			long diff = sizeof(TInd) == sizeof(int) ? NMC.vecPruneDirect_i32(T.Type, px, &thre, n, (int*)idxOut, valOut, true, nnz) : NMC.vecPruneDirect_i64(T.Type, px, &thre, n, (long*)idxOut, valOut, true, nnz);
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
		if (sizeof(TInd1) != sizeof(TInd2))
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

		bool hasError = true;
		using var valOut = target.ValueStorages.CreateFromFirst<T, TS2>(nnz, ref hasError);
		if (valOut.Invalid) return false;
		using var rowOut = target.IndexStorages.CreateFromFirst<TInd2, TSInd2>(nnz, ref hasError);
		if (rowOut.Invalid) return false;
		using var colOut = target.IndexStorages.CreateFromSecond<TInd2, TSInd2>(nnz, ref hasError);
		if (colOut.Invalid) return false;
		Storage.NativeMethods.cudaMemcpy(valOut, px, nnz * sizeof(T), MemoryCopyKind.DeviceToDevice);
		if (sizeof(TInd1) == sizeof(int))
			NMC.spVecIdxToCooIdxs_i32((int*)pp, rowOut.As<int>(), colOut.As<int>(), nnz, vector.Size[0]);
		else
			NMC.spVecIdxToCooIdxs_i64((long*)pp, rowOut.As<long>(), colOut.As<long>(), nnz, vector.Size[0]);
		target.SetValues(valOut, rowOut, colOut);
		target.Format = SparseFormat.MatrixCocFormat;
		target.DefaultValue = T.Zero;
		hasError = false;
		return true;
	}

	/// <inheritdoc/>
	public virtual bool SparseMatrixToVector<T, TInd1, TInd2, TS1, TS2, TSInd1, TSInd2>(ISparseArray<T, TInd1, TS1, TSInd1> matrix, ref SparseArrayWrapper<T, TInd2, TS2, TSInd2> target) where T : unmanaged, IBaseNumber<T> where TInd1 : unmanaged, IBinaryInt<TInd1> where TS1 : class, IStorage<T, TS1> where TSInd1 : class, IStorage<TInd1, TSInd1> where TInd2 : unmanaged, IBinaryInt<TInd2> where TS2 : class, IStorage<T, TS2> where TSInd2 : class, IStorage<TInd2, TSInd2>
	{
		if (sizeof(TInd1) != sizeof(TInd2))
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

		bool hasError = true;
		using var valOut = target.ValueStorages.CreateFromFirst<T, TS2>(nnz, ref hasError);
		if (valOut.Invalid) return false;
		using var idxOut = target.IndexStorages.CreateFromFirst<TInd2, TSInd2>(nnz, ref hasError);
		if (idxOut.Invalid) return false;
		if (pm != valOut)
			Storage.NativeMethods.cudaMemcpy(valOut, pm, nnz * sizeof(T), MemoryCopyKind.DeviceToDevice);
		if (sizeof(TInd1) == sizeof(int))
			NMC.cooIdxsToSpVecIdx_i32(idxOut.As<int>(), (int*)pr, (int*)pc, nnz, matrix.Size[0]);
		else
			NMC.cooIdxsToSpVecIdx_i64(idxOut.As<long>(), (long*)pr, (long*)pc, nnz, matrix.Size[0]);
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
		using var buf = CudaBuffer.Create(bufSize);
		return NM.cusparseSparseToDense(this.cusparseHandle, sp, dn, SparseToDenseAlgorithm.Default, buf).Check();
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

		bool hasError = true;
		using var rowOut = format.IsRowMajor ? target.IndexStorages.CreateFromFirst<TInd, TSInd>(target.Size[0] + 1, ref hasError) : default;
		using var colOut = !format.IsRowMajor ? target.IndexStorages.CreateFromSecond<TInd, TSInd>(target.Size[1] + 1, ref hasError) : default;
		using Backend.Storage.ErrorStateBuffer<T, TS2> valOut = default;
		if (threshold == 0)
		{
			using var sp = SparseMatrixWrapper.Create<T, TInd>(format, rows, cols, 0, null, rowOut, colOut, out bool success);
			if (!success) return false;
			using var dn = DenseMatrixWrapper.Create(ps, rows, cols, ld, false, out success);
			if (!success) return false;
			if (!NM.cusparseDenseToSparse_bufferSize(this.cusparseHandle, dn, sp, DenseToSparseAlgorithm.Default, out long bufSize).Check())
				return false;
			using var buf = CudaBuffer.Create(bufSize);
			if (!NM.cusparseDenseToSparse_analysis(this.cusparseHandle, dn, sp, DenseToSparseAlgorithm.Default, buf).Check())
				return false;
			if (!sp.GetSizes(out _, out _, out long nnz))
				return false;
			if (format.IsRowMajor)
			{
				if (!colOut.Update(nnz)) return false;
			}
			else
			{
				if (!rowOut.Update(nnz)) return false;
			}
			if (!valOut.Update(nnz)) return false;
			if (!sp.SetPointers(format, valOut, rowOut, colOut))
				return false;
			if (!NM.cusparseDenseToSparse_convert(this.cusparseHandle, dn, sp, DenseToSparseAlgorithm.Default, buf).Check())
				return false;
		}
		else
		{
			using BaseMatrixWrapper sp = new();
			T thre = threshold.As<T>();
			int nrows = (int)rows, ncols = (int)cols, lld = (int)ld;
			delegate*<IntPtr, int, int, T*, int, T*, BaseMatrixWrapper, int*, out int, void*, CudaSparseStatus> calcNnz = default(T) switch
			{
				Float16 => &NM.cusparseHpruneDense2csrNnz,
				Float32 => &NM.cusparseSpruneDense2csrNnz,
				Float64 => &NM.cusparseDpruneDense2csrNnz,
				_ => null
			};
			delegate*<IntPtr, int, int, T*, int, T*, BaseMatrixWrapper, T*, int*, int*, out long, CudaSparseStatus> getBufSize = default(T) switch
			{
				Float16 => &NM.cusparseHpruneDense2csr_bufferSizeExt,
				Float32 => &NM.cusparseSpruneDense2csr_bufferSizeExt,
				Float64 => &NM.cusparseDpruneDense2csr_bufferSizeExt,
				_ => null
			};
			delegate*<IntPtr, int, int, T*, int, T*, BaseMatrixWrapper, T*, int*, int*, void*, CudaSparseStatus> prune = default(T) switch
			{
				Float16 => &NM.cusparseHpruneDense2csr,
				Float32 => &NM.cusparseSpruneDense2csr,
				Float64 => &NM.cusparseDpruneDense2csr,
				_ => null
			};
			if (!format.IsRowMajor)
			{
				rowOut.Swap(colOut);
			}
			if (!getBufSize(this.cusparseHandle, nrows, ncols, ps, lld, &thre, sp, valOut, rowOut.As<int>(), colOut.As<int>(), out long bufSize).Check())
				return false;
			using var buf = CudaBuffer.Create(bufSize);
			if (!calcNnz(this.cusparseHandle, nrows, ncols, ps, lld, &thre, sp, rowOut.As<int>(), out int nnz, buf).Check())
				return false;
			if (!colOut.Update(nnz)) return false;
			if (!valOut.Update(nnz)) return false;
			if (!prune(this.cusparseHandle, nrows, ncols, ps, lld, &thre, sp, valOut, rowOut.As<int>(), colOut.As<int>(), buf).Check())
				return false;
		}
		target.Format = format;
		target.SetValues(valOut, rowOut, colOut);
		hasError = false;
		return true;
	}

	/// <inheritdoc/>
	public virtual bool MatrixSparsePrune<T, TInd1, TInd2, TS1, TS2, TSInd1, TSInd2>(ISparseArray<T, TInd1, TS1, TSInd1> source, double threshold, ref SparseArrayWrapper<T, TInd2, TS2, TSInd2> target) where T : unmanaged, IBaseNumber<T> where TInd1 : unmanaged, IBinaryInt<TInd1> where TS1 : class, IStorage<T, TS1> where TSInd1 : class, IStorage<TInd1, TSInd1> where TInd2 : unmanaged, IBinaryInt<TInd2> where TS2 : class, IStorage<T, TS2> where TSInd2 : class, IStorage<TInd2, TSInd2>
	{
		if (sizeof(TInd1) != sizeof(int) || sizeof(TInd2) != sizeof(int))
			return false;
		if (source.DefaultValue != T.Zero || target.DefaultValue != T.Zero)
			return false;
		if ((source.Format != SparseFormat.MatrixCscFormat && source.Format != SparseFormat.MatrixCsrFormat) || (target.Format & (SparseFormat.MatrixCsrFormat | SparseFormat.MatrixCsrFormat)) == SparseFormat.None)
			return false;
		if (target.ValueStorages.Length != 0 || target.IndexStorages.Length != 0)
			return false;
		if (!GetPointer(this, source, out var psVal, out var psRow1, out var psCol1, out var nnz))
			return false;
		int* psRow = (int*)psRow1, psCol = (int*)psCol1;
		if (source.Size[0] > int.MaxValue || source.Size[1] > int.MaxValue || nnz > int.MaxValue)
			return false;
		int m = (int)source.Size[0], n = (int)source.Size[1], nnzA = (int)nnz;
		if (!source.Format.IsRowMajor)
		{
			var temp = psRow; psRow = psCol; psCol = temp;
			(m, n) = (n, m);
		}
		T thre = threshold.As<T>();
		bool hasError = true;
		using var rowOut = (m + 1L).Create<TInd2, TSInd2, int>(ref hasError);
		if (rowOut.Invalid) return false;
		TS2 outVal; TSInd2 outCol;
		using BaseMatrixWrapper descrA = new();
		using BaseMatrixWrapper descrC = new();
		if (typeof(T) == typeof(Float16))
		{
			if (!NM.cusparseHpruneCsr2csr_bufferSizeExt(this.cusparseHandle, m, n, nnzA, descrA, psVal, psRow, psCol, &thre, descrC, null, rowOut, null, out long bufSize).Check())
				return false;
			using var buf = CudaBuffer.Create(bufSize);
			if (!NM.cusparseHpruneCsr2csrNnz(this.cusparseHandle, m, n, nnzA, descrA, psVal, psRow, psCol, &thre, descrC, rowOut, out int nnzC, buf).Check())
				return false;
			using var colOut = ((long)nnzC).Create<TInd2, TSInd2, int>(ref hasError);
			if (colOut.Invalid) return false;
			using var valOut = ((long)nnzC).Create<T, TS2>(ref hasError);
			if (valOut.Invalid) return false;
			if (!NM.cusparseHpruneCsr2csr(this.cusparseHandle, m, n, nnzA, descrA, psVal, psRow, psCol, &thre, descrC, valOut, rowOut, colOut, buf).Check())
				return false;
			outVal = valOut; outCol = colOut;
			hasError = false;
		}
		else
		{
			var nnzFunc = default(T) switch
			{
				Float32 => new NM.cusparsennz_compress<Float32>(NM.cusparseSnnz_compress) as NM.cusparsennz_compress<T>,
				Float64 => new NM.cusparsennz_compress<Float64>(NM.cusparseDnnz_compress) as NM.cusparsennz_compress<T>,
				Complex<Float32> => new NM.cusparsennz_compress<Complex<Float32>>(NM.cusparseCnnz_compress) as NM.cusparsennz_compress<T>,
				Complex<Float64> => new NM.cusparsennz_compress<Complex<Float64>>(NM.cusparseZnnz_compress) as NM.cusparsennz_compress<T>,
				_ => null
			};
			var pruneFunc = default(T) switch
			{
				Float32 => new NM.cusparsecsr2csr_compress<Float32>(NM.cusparseScsr2csr_compress) as NM.cusparsecsr2csr_compress<T>,
				Float64 => new NM.cusparsecsr2csr_compress<Float64>(NM.cusparseDcsr2csr_compress) as NM.cusparsecsr2csr_compress<T>,
				Complex<Float32> => new NM.cusparsecsr2csr_compress<Complex<Float32>>(NM.cusparseCcsr2csr_compress) as NM.cusparsecsr2csr_compress<T>,
				Complex<Float64> => new NM.cusparsecsr2csr_compress<Complex<Float64>>(NM.cusparseZcsr2csr_compress) as NM.cusparsecsr2csr_compress<T>,
				_ => null
			};
			if (nnzFunc is null || pruneFunc is null)
				return false;
			using var nnzPerRow = CudaBuffer.Create(m * sizeof(int), 0, false);
			if (!nnzFunc.Invoke(this.cusparseHandle, m, descrA, psVal, psRow, (int*)nnzPerRow.DeviceBuffer, out int nnzC, thre).Check())
				return false;
			using var colOut = ((long)nnzC).Create<TInd2, TSInd2, int>(ref hasError);
			if (colOut.Invalid) return false;
			using var valOut = ((long)nnzC).Create<T, TS2>(ref hasError);
			if (valOut.Invalid) return false;
			if (!pruneFunc.Invoke(this.cusparseHandle, m, n, descrA, psVal, psCol, psRow, nnzA, (int*)nnzPerRow.DeviceBuffer, valOut, colOut, rowOut, thre).Check())
				return false;
			outVal = valOut; outCol = colOut;
			hasError = false;
		}
		TSInd2 outRow = rowOut;
		if (!source.Format.IsRowMajor)
		{
			(outRow, outCol) = (outCol, outRow);
			(m, n) = (n, m);
		}
		target.SetValues(m, n, outVal, outRow, outCol);
		target.Format = source.Format;
		return true;
	}

	#region local converter
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private FormatConverter<T, TInd1, TInd2, TS1, TS2, TSInd1, TSInd2> GetConverter<T, TInd1, TInd2, TS1, TS2, TSInd1, TSInd2>(Api @this, ISparseArray<T, TInd1, TS1, TSInd1> source, in SparseArrayWrapper<T, TInd2, TS2, TSInd2> target) where T : unmanaged, IBaseNumber<T> where TInd1 : unmanaged, IBinaryInt<TInd1> where TS1 : class, IStorage<T, TS1> where TSInd1 : class, IStorage<TInd1, TSInd1> where TInd2 : unmanaged, IBinaryInt<TInd2> where TS2 : class, IStorage<T, TS2> where TSInd2 : class, IStorage<TInd2, TSInd2> => new(@this);

	private readonly ref struct FormatConverter<T, TInd1, TInd2, TS1, TS2, TSInd1, TSInd2> where T : unmanaged, IBaseNumber<T> where TInd1 : unmanaged, IBinaryInt<TInd1> where TS1 : class, IStorage<T, TS1> where TSInd1 : class, IStorage<TInd1, TSInd1> where TInd2 : unmanaged, IBinaryInt<TInd2> where TS2 : class, IStorage<T, TS2> where TSInd2 : class, IStorage<TInd2, TSInd2>
	{
		private readonly IntPtr handle;
		private readonly Api @this;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public FormatConverter(Api @this)
		{
			this.handle = @this.cusparseHandle; this.@this = @this;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly bool FormatConvertGetMeta(ISparseArray<T, TInd1, TS1, TSInd1> source, in SparseArrayWrapper<T, TInd2, TS2, TSInd2> target, out T* psVal, out int* psRow, out int* psCol, out int nnz, out int m, out int n, out int mb, out int nb, out int mbs, out int nbs, out bool colMajor)
		{
			psVal = null; psRow = psCol = null;
			m = (int)source.Size[0]; n = (int)source.Size[1];
			nnz = 0; mb = nb = mbs = nbs = 1; colMajor = false;
			if (source.Size[0] > int.MaxValue || source.Size[1] > int.MaxValue)
				return false;
			if (!GetPointerIncludeBlocked(@this, source, out psVal, out var psRow1, out var psCol1, out var nnz1))
				return false;
			if (nnz1 > int.MaxValue)
				return false;
			psRow = (int*)psRow1; psCol = (int*)psCol1; nnz = (int)nnz1;
			if (!target.BlockSize.IsEmpty)
			{
				mb = (int)target.BlockSize[0]; nb = (int)target.BlockSize[1];
				if (!source.BlockSize.IsEmpty)
				{
					mbs = (int)source.BlockSize[0]; nbs = (int)source.BlockSize[1];
				}
			}
			else if (!source.BlockSize.IsEmpty)
			{
				mb = (int)source.BlockSize[0]; nb = (int)source.BlockSize[1];
			}
			m /= mbs; n /= nbs; nnz /= mbs * nbs;
			colMajor = !source.Format.IsRowMajor;
			if (colMajor)
			{
				(m, n) = (n, m); (mb, nb) = (nb, mb); (mbs, nbs) = (nbs, mbs);
				var temp = psRow; psRow = psCol; psCol = temp;
			}
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly bool FormatConvertGetMetaNoTrans(ISparseArray<T, TInd1, TS1, TSInd1> source, out T* psVal, out int* psRow, out int* psCol, out int nnz, out int m, out int n, out bool colMajor)
		{
			psVal = null; psRow = psCol = null;
			m = (int)source.Size[0]; n = (int)source.Size[1];
			nnz = 0; colMajor = false;
			if (source.Size[0] > int.MaxValue || source.Size[1] > int.MaxValue)
				return false;
			if (!GetPointerIncludeBlocked(@this, source, out psVal, out var psRow1, out var psCol1, out var nnz1))
				return false;
			if (nnz1 > int.MaxValue)
				return false;
			psRow = (int*)psRow1; psCol = (int*)psCol1; nnz = (int)nnz1;
			colMajor = !source.Format.IsRowMajor;
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void FormatConvertSetValues(ref SparseArrayWrapper<T, TInd2, TS2, TSInd2> target, bool transpose, int m, int n, int mb, int nb, TS2 valOut, TSInd2 rowOut, TSInd2 colOut, SparseFormat dstFmt)
		{
			if (transpose)
			{
				(m, n) = (n, m); (mb, nb) = (nb, mb);
				dstFmt = dstFmt.WithTransposedMajor;
				(rowOut, colOut) = (colOut, rowOut);
			}
			target.Format = dstFmt;
			if (dstFmt.BlockType == SparseFormat.Blocking.Element)
				target.SetValues(m, n, valOut, rowOut, colOut);
			else
				target.SetValues(m, n, mb, nb, valOut, rowOut, colOut);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly bool FormatConvertBsr2Bsr(ISparseArray<T, TInd1, TS1, TSInd1> source, ref SparseArrayWrapper<T, TInd2, TS2, TSInd2> target)
		{
			bool hasError = true; // always true until correctly returned
			bool success = FormatConvertGetMeta(source, target, out var psVal, out var psRow, out var psCol, out int nnz, out int m, out int n, out int mbt, out int nbt, out int mbs, out int nbs, out bool colMajor);
			if (!success) return false;
			delegate*<IntPtr, SparseMatrixOrder, int, int, int, BaseMatrixWrapper, T*, int*, int*, int, int, int, int, out int, CudaSparseStatus> getBufSize = default(T) switch
			{
				Float32 => &NM.cusparseSgebsr2gebsr_bufferSize,
				Float64 => &NM.cusparseDgebsr2gebsr_bufferSize,
				Complex<Float32> => &NM.cusparseCgebsr2gebsr_bufferSize,
				Complex<Float64> => &NM.cusparseZgebsr2gebsr_bufferSize,
				_ => null
			};
			delegate*<IntPtr, SparseMatrixOrder, int, int, int, BaseMatrixWrapper, T*, int*, int*, int, int, BaseMatrixWrapper, T*, int*, int*, int, int, void*, CudaSparseStatus> calcFunc = default(T) switch
			{
				Float32 => &NM.cusparseSgebsr2gebsr,
				Float64 => &NM.cusparseDgebsr2gebsr,
				Complex<Float32> => &NM.cusparseCgebsr2gebsr,
				Complex<Float64> => &NM.cusparseZgebsr2gebsr,
				_ => null
			};
			if (getBufSize is null)
				return false;
			var dir = colMajor ? SparseMatrixOrder.Column : SparseMatrixOrder.Row;
			using BaseMatrixWrapper descr = new();
			if (!getBufSize(this.handle, dir, m, n, nnz, descr, psVal, psRow, psCol, mbs, nbs, mbt, nbt, out var bufSize).Check())
				return false;
			using var buf = CudaBuffer.Create(bufSize);
			using var rowOut = (m + 1L).Create<TInd2, TSInd2, int>(ref hasError);
			if (rowOut.Invalid) return false;
			if (!NM.cusparseXgebsr2gebsrNnz(this.handle, dir, m, n, nnz, descr, psRow, psCol, mbs, nbs, descr, rowOut, mbt, nbt, out int nnzC, buf).Check())
				return false;
			using var colOut = ((long)nnzC).Create<TInd2, TSInd2, int>(ref hasError);
			if (colOut.Invalid) return false;
			using var valOut = ((long)nnzC * mbt * nbt).Create<T, TS2>(ref hasError);
			if (valOut.Invalid) return false;
			if (!calcFunc(this.handle, dir, m, n, nnz, descr, psVal, psRow, psCol, mbs, nbs, descr, valOut, rowOut, colOut, mbt, nbt, buf).Check())
				return false;
			FormatConvertSetValues(ref target, colMajor, m * mbs, n * nbs, mbt, nbt, valOut, rowOut, colOut, SparseFormat.MatrixBsrFormat);
			hasError = true;
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly bool FormatConvertCooTrans(ISparseArray<T, TInd1, TS1, TSInd1> source, ref SparseArrayWrapper<T, TInd2, TS2, TSInd2> target)
		{
			bool hasError = true; // always true until correctly returned
			bool success = FormatConvertGetMetaNoTrans(source, out var psVal, out var psRow, out var psCol, out int nnz, out int m, out int n, out bool colMajor);
			if (!success) return false;
			using var valOut = ((long)nnz).Create<T, TS2>(ref hasError);
			if (valOut.Invalid) return false;
			using var rowOut = ((long)nnz).Create<TInd2, TSInd2, int>(ref hasError);
			if (rowOut.Invalid) return false;
			using var colOut = ((long)nnz).Create<TInd2, TSInd2, int>(ref hasError);
			if (colOut.Invalid) return false;
			Storage.NativeMethods.cudaMemcpy(rowOut, psRow, nnz * sizeof(int), MemoryCopyKind.DeviceToDevice).Check();
			Storage.NativeMethods.cudaMemcpy(colOut, psCol, nnz * sizeof(int), MemoryCopyKind.DeviceToDevice).Check();
			if (!NM.cusparseXcoosort_bufferSizeExt(this.handle, m, n, nnz, rowOut, colOut, out var bufSize).Check())
				return false;
			using var buf = CudaBuffer.Create(bufSize + sizeof(int) * nnz, 0, false);
			var p = (int*)((byte*)buf + bufSize);
			if (!NM.cusparseCreateIdentityPermutation(this.handle, nnz, p).Check())
				return false;
			var err = colMajor ? NM.cusparseXcoosortByRow(this.handle, m, n, nnz, rowOut, colOut, p, buf) : NM.cusparseXcoosortByColumn(this.handle, m, n, nnz, rowOut, colOut, p, buf);
			if (!err.Check())
				return false;
			using var sp = SparseVectorWrapper.Create(psVal, (TInd2*)p, nnz, nnz, out success);
			if (!success) return false;
			using var dn = DenseVectorWrapper.Create<T>(valOut, nnz, out success);
			if (!success) return false;
			if (!NM.cusparseGather(this.handle, dn, sp).Check())
				return false;
			FormatConvertSetValues(ref target, false, m, n, 1, 1, valOut, rowOut, colOut, colMajor ? SparseFormat.MatrixCorFormat : SparseFormat.MatrixCocFormat);
			hasError = false;
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly bool FormatConvertCsrTrans(ISparseArray<T, TInd1, TS1, TSInd1> source, ref SparseArrayWrapper<T, TInd2, TS2, TSInd2> target)
		{
			bool hasError = true; // always true until correctly returned
			bool success = FormatConvertGetMeta(source, target, out var psVal, out var psRow, out var psCol, out int nnz, out int m, out int n, out _, out _, out _, out _, out bool colMajor);
			if (!success) return false;
			using var valOut = ((long)nnz).Create<T, TS2>(ref hasError);
			if (valOut.Invalid) return false;
			using var rowOut = ((long)nnz).Create<TInd2, TSInd2, int>(ref hasError);
			if (rowOut.Invalid) return false;
			using var colOut = (n + 1L).Create<TInd2, TSInd2, int>(ref hasError);
			if (colOut.Invalid) return false;
			var type = T.Type.ToCudaDataType();
			if (!NM.cusparseCsr2cscEx2_bufferSize(this.handle, m, n, nnz, psVal, psRow, psCol, valOut, colOut, rowOut, type, SparseAction.ValuesAndIndices, IndexBase.Zero, Csr2CscAlgorithm.Algorithm1, out var bufSize).Check())
				return false;
			using var buf = CudaBuffer.Create(bufSize);
			if (!NM.cusparseCsr2cscEx2(this.handle, m, n, nnz, psVal, psRow, psCol, valOut, colOut, rowOut, type, SparseAction.ValuesAndIndices, IndexBase.Zero, Csr2CscAlgorithm.Algorithm1, buf).Check())
				return false;
			FormatConvertSetValues(ref target, colMajor, m, n, 1, 1, valOut, rowOut, colOut, SparseFormat.MatrixCscFormat);
			hasError = false;
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly bool FormatConvertCoo2Csr(ISparseArray<T, TInd1, TS1, TSInd1> source, ref SparseArrayWrapper<T, TInd2, TS2, TSInd2> target)
		{
			bool hasError = true; // always true until correctly returned
			bool success = FormatConvertGetMeta(source, target, out var psVal, out var psRow, out var psCol, out int nnz, out int m, out int n, out _, out _, out _, out _, out bool colMajor);
			using var rowOut = (m + 1L).Create<TInd2, TSInd2, int>(ref hasError);
			if (rowOut.Invalid) return false;
			if (!NM.cusparseXcoo2csr(this.handle, psRow, nnz, m, rowOut, IndexBase.Zero).Check())
				return false;
			var valOut = (source.ValueStorages[0] as TS2)!;
			var colOut = colMajor ? (source.IndexStorages[0] as TSInd2)! : (source.IndexStorages[1] as TSInd2)!;
			FormatConvertSetValues(ref target, colMajor, m, n, 1, 1, valOut, rowOut, colOut, SparseFormat.MatrixCsrFormat);
			hasError = false;
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly bool FormatConvertCsr2Coo(ISparseArray<T, TInd1, TS1, TSInd1> source, ref SparseArrayWrapper<T, TInd2, TS2, TSInd2> target)
		{
			bool hasError = true; // always true until correctly returned
			bool success = FormatConvertGetMeta(source, target, out var psVal, out var psRow, out var psCol, out int nnz, out int m, out int n, out _, out _, out _, out _, out bool colMajor);
			using var rowOut = ((long)nnz).Create<TInd2, TSInd2, int>(ref hasError);
			if (rowOut.Invalid) return false;
			if (!NM.cusparseXcsr2coo(this.handle, psRow, nnz, m, rowOut, IndexBase.Zero).Check())
				return false;
			var valOut = (source.ValueStorages[0] as TS2)!;
			var colOut = colMajor ? (source.IndexStorages[0] as TSInd2)! : (source.IndexStorages[1] as TSInd2)!;
			FormatConvertSetValues(ref target, colMajor, m, n, 1, 1, valOut, rowOut, colOut, SparseFormat.MatrixCorFormat);
			hasError = false;
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly bool FormatConvertCsr2Bsr(ISparseArray<T, TInd1, TS1, TSInd1> source, ref SparseArrayWrapper<T, TInd2, TS2, TSInd2> target)
		{
			bool hasError = true; // always true until correctly returned
			bool success = FormatConvertGetMeta(source, target, out var psVal, out var psRow, out var psCol, out int nnz, out int m, out int n, out int mb, out int nb, out _, out _, out bool colMajor);
			delegate*<IntPtr, SparseMatrixOrder, int, int, BaseMatrixWrapper, T*, int*, int*, int, int, out int, CudaSparseStatus> getBufSize = default(T) switch
			{
				Float32 => &NM.cusparseScsr2gebsr_bufferSize,
				Float64 => &NM.cusparseDcsr2gebsr_bufferSize,
				Complex<Float32> => &NM.cusparseCcsr2gebsr_bufferSize,
				Complex<Float64> => &NM.cusparseZcsr2gebsr_bufferSize,
				_ => null
			};
			delegate*<IntPtr, SparseMatrixOrder, int, int, BaseMatrixWrapper, T*, int*, int*, BaseMatrixWrapper, T*, int*, int*, int, int, void*, CudaSparseStatus> calcFunc = default(T) switch
			{
				Float32 => &NM.cusparseScsr2gebsr,
				Float64 => &NM.cusparseDcsr2gebsr,
				Complex<Float32> => &NM.cusparseCcsr2gebsr,
				Complex<Float64> => &NM.cusparseZcsr2gebsr,
				_ => null
			};
			if (getBufSize is null)
				return false;
			using BaseMatrixWrapper descr = new();
			var dir = colMajor ? SparseMatrixOrder.Column : SparseMatrixOrder.Row;
			if (!getBufSize(this.handle, dir, m, n, descr, psVal, psRow, psCol, mb, nb, out int bufSize).Check())
				return false;
			using var buf = CudaBuffer.Create(bufSize);
			using var rowOut = (m / mb + 1L).Create<TInd2, TSInd2, int>(ref hasError);
			if (rowOut.Invalid) return false;
			if (!NM.cusparseXcsr2gebsrNnz(this.handle, dir, m, n, descr, psRow, psCol, descr, rowOut, mb, nb, out int nnzC, buf).Check())
				return false;
			using var colOut = ((long)nnzC).Create<TInd2, TSInd2, int>(ref hasError);
			if (colOut.Invalid) return false;
			using var valOut = ((long)nnzC * mb * nb).Create<T, TS2>(ref hasError);
			if (valOut.Invalid) return false;
			if (!calcFunc(this.handle, dir, m, n, descr, psVal, psRow, psCol, descr, valOut, rowOut, colOut, mb, nb, buf).Check())
				return false;
			FormatConvertSetValues(ref target, colMajor, m, n, mb, nb, valOut, rowOut, colOut, SparseFormat.MatrixBsrFormat);
			hasError = false;
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly bool FormatConvertBsr2Csr(ISparseArray<T, TInd1, TS1, TSInd1> source, ref SparseArrayWrapper<T, TInd2, TS2, TSInd2> target)
		{
			bool hasError = true; // always true until correctly returned
			bool success = FormatConvertGetMeta(source, target, out var psVal, out var psRow, out var psCol, out int nnz, out int m, out int n, out _, out _, out int mb, out int nb, out bool colMajor);
			long nnzA = (long)nnz * mb * nb;
			delegate*<IntPtr, SparseMatrixOrder, int, int, BaseMatrixWrapper, T*, int*, int*, int, int, BaseMatrixWrapper, T*, int*, int*, CudaSparseStatus> calcFunc = default(T) switch
			{
				Float32 => &NM.cusparseSgebsr2csr,
				Float64 => &NM.cusparseDgebsr2csr,
				Complex<Float32> => &NM.cusparseCgebsr2csr,
				Complex<Float64> => &NM.cusparseZgebsr2csr,
				_ => null
			};
			if (calcFunc is null)
				return false;
			using var rowOut = (m * mb + 1L).Create<TInd2, TSInd2, int>(ref hasError);
			if (rowOut.Invalid) return false;
			using var colOut = nnzA.Create<TInd2, TSInd2, int>(ref hasError);
			if (colOut.Invalid) return false;
			using var valOut = nnzA.Create<T, TS2>(ref hasError);
			if (valOut.Invalid) return false;
			var dir = colMajor ? SparseMatrixOrder.Column : SparseMatrixOrder.Row;
			using BaseMatrixWrapper descr = new();
			if (!calcFunc(this.handle, dir, m, n, descr, psVal, psRow, psCol, mb, nb, descr, valOut, rowOut, colOut).Check())
				return false;
			FormatConvertSetValues(ref target, colMajor, m, n, 1, 1, valOut, rowOut, colOut, SparseFormat.MatrixCsrFormat);
			hasError = false;
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly bool Convert(ISparseArray<T, TInd1, TS1, TSInd1> source, ref SparseArrayWrapper<T, TInd2, TS2, TSInd2> target)
		{
			// same-format same-major
			if ((target.Format & source.Format) != SparseFormat.None)
			{
				target.Format = source.Format;
				if (target.Format.BlockType == SparseFormat.Blocking.Simple)
				{
					if (target.BlockSize.IsEmpty || target.BlockSize.SequenceEqual(source.BlockSize))
					{
						target.SetValues(source.Size[0], source.Size[1],
										source.BlockSize[0], source.BlockSize[1],
										(source.ValueStorages[0] as TS2)!,
										(source.IndexStorages[0] as TSInd2)!,
										(source.IndexStorages[1] as TSInd2)!);
					}
					else
					{
						return this.FormatConvertBsr2Bsr(source, ref target);
					}
				}
				else
				{
					target.SetValues(source.Size[0], source.Size[1],
								(source.ValueStorages[0] as TS2)!,
								(source.IndexStorages[0] as TSInd2)!,
								(source.IndexStorages[1] as TSInd2)!);
				}
				return true;
			}
			// same-format cross-major
			if ((target.Format.MajorType == SparseFormat.Major.Row && (target.Format & source.Format.WithRowMajor) != SparseFormat.None) ||
				(target.Format.MajorType == SparseFormat.Major.Column && (target.Format & source.Format.WithColumnMajor) != SparseFormat.None))
			{
				if (source.Format.WithRowMajor == SparseFormat.MatrixCorFormat)
				{
					return this.FormatConvertCooTrans(source, ref target);
				}
				else if (source.Format.WithRowMajor == SparseFormat.MatrixCsrFormat)
				{
					return this.FormatConvertCsrTrans(source, ref target);
				}
				else
					return false;
			}
			// cross-format same-major
			Span<SparseFormat> colFormats = stackalloc SparseFormat[] { SparseFormat.MatrixCocFormat, SparseFormat.MatrixCscFormat, SparseFormat.MatrixBscFormat };
			Span<SparseFormat> rowFormats = stackalloc SparseFormat[colFormats.Length];
			colFormats.CopyTo(rowFormats, static f => f.WithRowMajor);
			SparseFormat colAll = colFormats.OrAll(), rowAll = rowFormats.OrAll();
			int i = 0;
			for (; i < colFormats.Length; i++)
			{
				if ((source.Format == colFormats[i] && (target.Format & colAll) != SparseFormat.None) ||
					(source.Format == rowFormats[i] && (target.Format & rowAll) != SparseFormat.None))
					goto CALC;
			}
			return false;
		CALC:
			SparseFormat srcFmt = source.Format.WithRowMajor;
			if (srcFmt == SparseFormat.MatrixCorFormat)
			{
				if ((target.Format.WithRowMajor & SparseFormat.MatrixCsrFormat) == SparseFormat.None)
					return false;
				return this.FormatConvertCoo2Csr(source, ref target);
			}
			else if (srcFmt == SparseFormat.MatrixCsrFormat)
			{
				if ((target.Format.WithRowMajor & SparseFormat.MatrixCorFormat) != SparseFormat.None)
				{
					return this.FormatConvertCsr2Coo(source, ref target);
				}
				else
				{
					return this.FormatConvertCsr2Bsr(source, ref target);
				}
			}
			else
			{
				if ((target.Format.WithRowMajor & SparseFormat.MatrixCsrFormat) == SparseFormat.None)
					return false;
				return this.FormatConvertBsr2Csr(source, ref target);
			}
		}
	}
	#endregion

	/// <inheritdoc/>
	public virtual bool MatrixSparseFormatConvert<T, TInd1, TInd2, TS1, TS2, TSInd1, TSInd2>(ISparseArray<T, TInd1, TS1, TSInd1> source, ref SparseArrayWrapper<T, TInd2, TS2, TSInd2> target) where T : unmanaged, IBaseNumber<T> where TInd1 : unmanaged, IBinaryInt<TInd1> where TS1 : class, IStorage<T, TS1> where TSInd1 : class, IStorage<TInd1, TSInd1> where TInd2 : unmanaged, IBinaryInt<TInd2> where TS2 : class, IStorage<T, TS2> where TSInd2 : class, IStorage<TInd2, TSInd2>
	{
		if (sizeof(TInd1) != sizeof(int) || sizeof(TInd2) != sizeof(int))
			return false;
		if (typeof(TS2) != typeof(TS1) || typeof(TSInd2) != typeof(TSInd1))
			return false;
		if (source.DefaultValue != T.Zero || target.DefaultValue != T.Zero)
			return false;
		if (target.ValueStorages.Length != 0 || target.IndexStorages.Length != 0)
			return false;
		if ((source.Format & NM.SupportFormatIncludeBlocked) == SparseFormat.None || (target.Format & NM.SupportFormatIncludeBlocked) == SparseFormat.None)
			return false;
		var converter = GetConverter(this, source, target);
		return converter.Convert(source, ref target);
	}
	#endregion

	#region not supported
	bool IConversionAbstractApi.SparseMatrixGetSlice<T, TInd1, TInd2, TS1, TS2, TSInd1, TSInd2>(ISparseArray<T, TInd1, TS1, TSInd1> source, MatrixSliceWrapper slice, ref SparseArrayWrapper<T, TInd2, TS2, TSInd2> sub) => false;
	bool IConversionAbstractApi.SparseMatrixSetSlice<T, TInd1, TInd2, TS1, TS2, TSInd1, TSInd2>(ISparseArray<T, TInd2, TS2, TSInd2> matrix, MatrixSliceWrapper slice, ISparseArray<T, TInd1, TS1, TSInd1> sub) => false;
	bool IConversionAbstractApi.MatrixSparseReshape<T, TInd1, TInd2, TS1, TS2, TSInd1, TSInd2>(ISparseArray<T, TInd1, TS1, TSInd1> source, ref SparseArrayWrapper<T, TInd2, TS2, TSInd2> target) => false;
	#endregion
}
