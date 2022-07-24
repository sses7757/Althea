using System.Runtime.CompilerServices;

using Althea.Array;
using Althea.Backend.Storage;
using Althea.LinearAlgebra;
using Althea.LinearAlgebra.Sparse;
using Althea.Linq;

using static Althea.Backend.Mkl.MemoryPointerChecker;

using NM = Althea.Backend.Mkl.LinearAlgebra.Sparse.NativeMethods;
using NMC = Althea.Backend.Mkl.LinearAlgebra.Sparse.CustomNativeMethods;


namespace Althea.Backend.Mkl.LinearAlgebra.Sparse
{
	/// <summary>
	/// The MKL back-end of <see cref="IConversionAbstractApi"/>, <see cref="IComputationAbstractApi"/> and <see cref="IIndexOperationAbstractApi"/> that supports storage locations of CPU memory.
	/// </summary>
	public unsafe partial class Api : IConversionAbstractApi, IComputationAbstractApi, IIndexOperationAbstractApi
	{
		#region basic
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void FreeSparseMatrix(IntPtr handle)
		{
			var err = NM.mkl_sparse_destroy(handle);
			if (err != MklSparseBlasError.Success)
				Helpers.Log.Write($"Error in disposing MKL Sparse BLAS and LAPACK library: {err}", level: Helpers.LogLevel.Error);
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			foreach (var kv in this.mvCache.Concat(this.svCache))
				FreeSparseMatrix(kv.Value);
			foreach (var kv in this.mmCache.Concat(this.smCache))
				FreeSparseMatrix(kv.Value);
			this.Disposed = true;
			GC.SuppressFinalize(this);
		}

		/// <inheritdoc/>
		public bool Disposed { get; set; } = false;

		private readonly Dictionary<(object matrix, MatrixOp trans), IntPtr> mvCache = new();
		private readonly Dictionary<(object matrix, MatrixOp trans), IntPtr> svCache = new();
		private readonly Dictionary<(object matrix, MatrixOp trans, long cols), IntPtr> mmCache = new();
		private readonly Dictionary<(object matrix, MatrixOp trans, long cols), IntPtr> smCache = new();
		#endregion

		#region vector conversion
		/// <inheritdoc/>
		public virtual bool VectorSetValuesAt<T, TInd, TS, TSInd>(TS x, T value, TSInd positions) where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd> where TS : class, IStorage<T, TS> where TSInd : class, IStorage<TInd, TSInd>
		{
			if (!GetPointer(x, positions, out T* px, out TInd* pp, out var n))
				return false;
			return NMC.vecSetValAt(T.Type, px, &value, (MklInt*)pp, n) >= 0;
		}

		/// <inheritdoc/>
		public virtual bool VectorSetValuesAt<T, TInd, TS1, TS2, TSInd>(TS1 x, TS2 values, TSInd positions) where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TSInd : class, IStorage<TInd, TSInd>
		{
			if (!GetPointer(values, positions, out T* px, out TInd* pp, out var n))
				return false;
			if (!GetPointer(x, out T* py, out var n2))
				return false;
			if (n2 < n)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(values));

			delegate*<MklInt, T*, MklInt*, T*, void> func = default(T) switch
			{
				Float32 => &NM.cblas_ssctr,
				Float64 => &NM.cblas_dsctr,
				Complex<Float32> => &NM.cblas_csctr,
				Complex<Float64> => &NM.cblas_zsctr,
				_ => null
			};
			if (func is null)
				return false;
			func(n, px, (MklInt*)pp, py);
			return true;
		}

		/// <inheritdoc/>
		public virtual bool VectorGatherValuesAt<T, TInd, TS1, TS2, TSInd>(TS1 x, TS2 values, TSInd positions) where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TSInd : class, IStorage<TInd, TSInd>
		{
			if (!GetPointer(values, positions, out T* py, out TInd* pp, out var n))
				return false;
			if (!GetPointer(x, out T* px, out var n2))
				return false;
			if (n2 < n)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(values));

			delegate*<MklInt, T*, T*, MklInt*, void> func = default(T) switch
			{
				Float32 => &NM.cblas_sgthr,
				Float64 => &NM.cblas_dgthr,
				Complex<Float32> => &NM.cblas_cgthr,
				Complex<Float64> => &NM.cblas_zgthr,
				_ => null
			};
			if (func is null)
				return false;
			func(n, py, px, (MklInt*)pp);
			return true;
		}

		/// <inheritdoc/>
		public virtual bool VectorSparseToDense<T, TInd, TS1, TS2, TSInd>(ISparseArray<T, TInd, TS1, TSInd> x!!, TS2 y, long strideY) where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TSInd : class, IStorage<TInd, TSInd>
		{
			if (x.Size.Length != 1)
				throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(x));
			if (strideY != 1 || x.Format != SparseFormat.VectorCooFormat || x.DefaultValue != T.Zero)
				return false;
			if (x.IndexStorages.Length != 1 || x.ValueStorages.Length != 1)
				return false;
			if (!GetPointer(x.ValueStorages[0], x.IndexStorages[0], out T* px, out TInd* pp, out var n))
				return false;
			if (!GetPointer(y, out T* py, out var n2))
				return false;
			if (n2 < n || x.Size[0] != n2)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(x));

			delegate*<MklInt, T*, MklInt*, T*, void> func = default(T) switch
			{
				Float32 => &NM.cblas_ssctr,
				Float64 => &NM.cblas_dsctr,
				Complex<Float32> => &NM.cblas_csctr,
				Complex<Float64> => &NM.cblas_zsctr,
				_ => null
			};
			if (func is null)
				return false;
			new Span<T>(py, (int)n2).Fill(x.DefaultValue);
			func(n, px, (MklInt*)pp, py);
			return true;
		}

		/// <inheritdoc/>
		public virtual bool VectorDenseToSparse<T, TInd, TS1, TS2, TSInd>(TS1 x, long strideX, ref SparseArrayWrapper<T, TInd, TS2, TSInd> y, double threshold = 0) where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd> where TS2 : class, IStorage<T, TS2> where TS1 : class, IStorage<T, TS1> where TSInd : class, IStorage<TInd, TSInd>
		{
			if (!TInd.Type.IsInteger() || TInd.Size != sizeof(MklInt))
				return false;
			if (strideX != 1 || y.Format != SparseFormat.VectorCooFormat || y.DefaultValue != T.Zero)
				return false;
			if (!GetPointer(x, out T* px, out var n))
				return false;
			T thre = threshold.As<T>();
			if (y.Size.IsEmpty || y.Size[0] == 0)
			{	// create y
				if (y.ValueStorages.Length is not 0 and not 1 || y.IndexStorages.Length is not 0 and not 1)
					throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(y));
				if ((!y.ValueStorages.IsEmpty && !y.ValueStorages[0].IsValid()) || (!y.IndexStorages.IsEmpty && !y.IndexStorages[0].IsValid()))
					return false;
				long bufSize = NMC.vecPruneBuffer(T.Type, n);
				if (bufSize < 0)
					return false;
				using var buf = ArrayPoolBuffers.Create<byte>(bufSize);
				long nnz = NMC.vecPruneNnz(T.Type, px, &thre, n, buf);
				var valOut = PureStorage<T, CpuMemoryPointer>.Create(nnz);
				var idxOut = PureStorage<TInd, CpuMemoryPointer>.Create(nnz);
				_ = NMC.vecPruneCal(T.Type, n, buf, nnz, (MklInt*)idxOut.Pointer.Pointer.Pointer, (void*)valOut.Pointer.Pointer.Pointer);
				y.SetValues(n, valOut as TS2 ?? TS2.Empty, idxOut as TSInd ?? TSInd.Empty); // never empty
			}
			else
			{   // in-place modify y
				if (y.Size[0] != x.Length)
					throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(y));
				if (y.ValueStorages.Length != 1 || y.IndexStorages.Length != 1)
					throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(y));
				if (y.ValueStorages.Length != 1 || y.IndexStorages.Length != 1)
					throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(y));
				if (!GetPointer(y.ValueStorages[0], y.IndexStorages[0], out T* valOut, out TInd* idxOut, out var nnz))
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
		public virtual bool SparseVectorToMatrix<T, TInd1, TInd2, TS1, TS2, TSInd1, TSInd2>(ISparseArray<T, TInd1, TS1, TSInd1> vector!!, ref SparseArrayWrapper<T, TInd2, TS2, TSInd2> target) where T : unmanaged, IBaseNumber<T> where TInd1 : unmanaged, IBinaryInt<TInd1> where TS1 : class, IStorage<T, TS1> where TSInd1 : class, IStorage<TInd1, TSInd1> where TInd2 : unmanaged, IBinaryInt<TInd2> where TS2 : class, IStorage<T, TS2> where TSInd2 : class, IStorage<TInd2, TSInd2>
		{
			if (!TInd1.Type.IsInteger() || TInd1.Size != sizeof(MklInt))
				return false;
			if (typeof(TInd1) != typeof(TInd2))
				return false;
			if (vector.Format != SparseFormat.VectorCooFormat || vector.DefaultValue != T.Zero || (target.Format & SparseFormat.MatrixCocFormat) == SparseFormat.None || target.DefaultValue != T.Zero)
				return false;
			if (vector.Size.Length != 1)
				throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(vector));
			if (target.Size.Length != 2 || target.Size.Prod() != vector.Size[0])
				throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(target));
			if (vector.IndexStorages.Length != 1 || vector.ValueStorages.Length != 1)
				throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(vector));
			if (!GetPointer(vector.ValueStorages[0], vector.IndexStorages[0], out T* px, out TInd1* pp, out var nnz))
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
				var s = PureStorage<T, CpuMemoryPointer>.Create(nnz);
				pVal = (T*)s.Pointer.Pointer.Pointer;
				valOut = s as TS2 ?? TS2.Empty; // never empty
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
				var s = PureStorage<TInd2, CpuMemoryPointer>.Create(nnz);
				pRow = (TInd2*)s.Pointer.Pointer.Pointer;
				rowIdxOut = s as TSInd2 ?? TSInd2.Empty; // never empty
				s = PureStorage<TInd2, CpuMemoryPointer>.Create(nnz);
				pCol = (TInd2*)s.Pointer.Pointer.Pointer;
				colIdxOut = s as TSInd2 ?? TSInd2.Empty; // never empty
			}
			Buffer.MemoryCopy(px, pVal, nnz * sizeof(T), nnz * sizeof(T));
			NMC.spVecIdxToCooIdxs((MklInt*)pp, (MklInt*)pRow, (MklInt*)pCol, nnz, vector.Size[0]);
			target.SetValues(valOut, rowIdxOut, colIdxOut);
			target.Format = SparseFormat.MatrixCocFormat;
			return true;
		}

		/// <inheritdoc/>
		public virtual bool SparseMatrixToVector<T, TInd1, TInd2, TS1, TS2, TSInd1, TSInd2>(ISparseArray<T, TInd1, TS1, TSInd1> matrix, ref SparseArrayWrapper<T, TInd2, TS2, TSInd2> target) where T : unmanaged, IBaseNumber<T> where TInd1 : unmanaged, IBinaryInt<TInd1> where TS1 : class, IStorage<T, TS1> where TSInd1 : class, IStorage<TInd1, TSInd1> where TInd2 : unmanaged, IBinaryInt<TInd2> where TS2 : class, IStorage<T, TS2> where TSInd2 : class, IStorage<TInd2, TSInd2>
		{
			if (!TInd1.Type.IsInteger() || TInd1.Size != sizeof(MklInt))
				return false;
			if (typeof(TInd1) != typeof(TInd2))
				return false;
			if ((target.Format & SparseFormat.VectorCooFormat) == SparseFormat.None || target.DefaultValue != T.Zero || matrix.Format != SparseFormat.MatrixCocFormat || matrix.DefaultValue != T.Zero)
				return false;
			if (target.Size.Length is not 0 and not 1)
				throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(target));
			if (matrix.Size.Length != 2)
				throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(matrix));
			if (!GetPointer(matrix, out T* pm, out var pr, out var pc, out var nnz))
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
				var s = PureStorage<T, CpuMemoryPointer>.Create(nnz);
				pVal = (T*)s.Pointer.Pointer.Pointer;
				valOut = s as TS2 ?? TS2.Empty; // never empty
			}
			if (target.IndexStorages.Length == 2)
			{
				idxOut = target.IndexStorages[0];
				if (!GetPointer(idxOut, out pIdx, out var n2) || n2 != nnz)
					return false;
			}
			else
			{
				var s = PureStorage<TInd2, CpuMemoryPointer>.Create(nnz);
				pIdx = (TInd2*)s.Pointer.Pointer.Pointer;
				idxOut = s as TSInd2 ?? TSInd2.Empty; // never empty
			}
			Buffer.MemoryCopy(pm, pVal, nnz * sizeof(T), nnz * sizeof(T));
			NMC.cooIdxsToSpVecIdx((MklInt*)pIdx, (MklInt*)pr, (MklInt*)pc, nnz, matrix.Size[0]);
			target.SetValues(matrix.Size.Prod(), valOut, idxOut);
			target.Format = SparseFormat.VectorCooFormat;
			return true;
		}
		#endregion

		#region matrix conversion
		private static readonly SparseFormat CompressedFormat = SparseFormat.MatrixCscFormat | SparseFormat.MatrixCsrFormat | SparseFormat.MatrixBscFormat | SparseFormat.MatrixBsrFormat;
		private static readonly SparseFormat SupportInputFormat = SparseFormat.MatrixCocFormat | SparseFormat.MatrixCorFormat | SparseFormat.MatrixCsrFormat | SparseFormat.MatrixCscFormat | SparseFormat.MatrixBsrFormat;

		/// <inheritdoc/>
		public virtual bool MatrixSparseFormatConvert<T, TInd1, TInd2, TS1, TS2, TSInd1, TSInd2>(ISparseArray<T, TInd1, TS1, TSInd1> source, ref SparseArrayWrapper<T, TInd2, TS2, TSInd2> target) where T : unmanaged, IBaseNumber<T> where TInd1 : unmanaged, IBinaryInt<TInd1> where TS1 : class, IStorage<T, TS1> where TSInd1 : class, IStorage<TInd1, TSInd1> where TInd2 : unmanaged, IBinaryInt<TInd2> where TS2 : class, IStorage<T, TS2> where TSInd2 : class, IStorage<TInd2, TSInd2>
		{
			if ((target.Format & CompressedFormat) == SparseFormat.None || (source.Format & SupportInputFormat) == SparseFormat.None || source.DefaultValue != T.Zero || target.DefaultValue != T.Zero)
				return false;
			if (typeof(TInd1) != typeof(TInd2))
				return false;
			if (!source.BlockSize.IsEmpty && source.BlockSize[0] != source.BlockSize[1])
				return false; // not supported by MKL
			if (!target.ValueStorages.IsEmpty || !target.IndexStorages.IsEmpty)
				return false; // not supported by MKL
			if (target.Size.Length is not 0 and not 2)
				throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(target));
			if (target.Format.BlockType == SparseFormat.Blocking.Simple && (target.BlockSize.Length != 2 || target.BlockSize[0] <= 0 || target.BlockSize[1] <= 0))
				throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(target));
			if (target.Format.BlockType == SparseFormat.Blocking.Simple && target.BlockSize[0] != target.BlockSize[1])
				return false; // not supported by MKL

			using var handleSrc = SparseMatrixHandle.TryCreate(source, out bool success);
			if (!success)
				return false;
			IntPtr handleDst;
			bool targetRow = (target.Format.MajorType & SparseFormat.Major.Row) != 0;
			bool targetCsr = (target.Format.BlockType & SparseFormat.Blocking.Element) != 0;
			if (targetCsr)
				NM.mkl_sparse_convert_csr(handleSrc, targetRow ? MatrixOp.None : MatrixOp.Trans, out handleDst).Check();
			else
				NM.mkl_sparse_convert_bsr(handleSrc, target.BlockSize[0], targetRow ? MatrixMajor.Row : MatrixMajor.Column, targetRow ? MatrixOp.None : MatrixOp.Trans, out handleDst).Check();
			using var dst = new SparseMatrixHandle(handleDst, targetCsr ? SparseFormat.MatrixCsrFormat : SparseFormat.MatrixBsrFormat, !targetRow);
			dst.Deconstruct(ref target);
			return true;
		}

		bool IConversionAbstractApi.SparseMatrixGetSlice<T, TInd1, TInd2, TS1, TS2, TSInd1, TSInd2>(ISparseArray<T, TInd1, TS1, TSInd1> source, MatrixSliceWrapper slice, ref SparseArrayWrapper<T, TInd2, TS2, TSInd2> sub) => false;

		bool IConversionAbstractApi.SparseMatrixSetSlice<T, TInd1, TInd2, TS1, TS2, TSInd1, TSInd2>(ISparseArray<T, TInd2, TS2, TSInd2> matrix, MatrixSliceWrapper slice, ISparseArray<T, TInd1, TS1, TSInd1> sub) => false;

		bool IConversionAbstractApi.MatrixSparseToDense<T, TInd, TS1, TS2, TSInd>(ISparseArray<T, TInd, TS1, TSInd> source, TS2 destination, long ld) => false;

		bool IConversionAbstractApi.MatrixDenseToSparse<T, TInd, TS1, TS2, TSInd>(TS1 source, long ld, ref SparseArrayWrapper<T, TInd, TS2, TSInd> target, double threshold) => false;

		bool IConversionAbstractApi.MatrixSparsePrune<T, TInd1, TInd2, TS1, TS2, TSInd1, TSInd2>(ISparseArray<T, TInd1, TS1, TSInd1> source, double threshold, ref SparseArrayWrapper<T, TInd2, TS2, TSInd2> target) => false;

		bool IConversionAbstractApi.MatrixSparseReshape<T, TInd1, TInd2, TS1, TS2, TSInd1, TSInd2>(ISparseArray<T, TInd1, TS1, TSInd1> source, ref SparseArrayWrapper<T, TInd2, TS2, TSInd2> target) => false;
		#endregion

		#region vector computation
		/// <inheritdoc/>
		public virtual bool VectorSparseAddToDense<T, TInd, TS1, TS2, TSInd>(T α, ISparseArray<T, TInd, TS1, TSInd> x, TS2 y, long strideY) where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TSInd : class, IStorage<TInd, TSInd>
		{
			if (x.Size.Length != 1)
				throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(x));
			if (strideY != 1 || x.Format != SparseFormat.VectorCooFormat || x.DefaultValue != T.Zero)
				return false;
			if (x.IndexStorages.Length != 1 || x.ValueStorages.Length != 1)
				return false;
			if (!GetPointer(x.ValueStorages[0], x.IndexStorages[0], out T* px, out TInd* pp, out var nnz))
				return false;
			if (!GetPointer(y, strideY, out T* py, out var n))
				return false;
			if (n < nnz || x.Size[0] != n)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(x));

			var funcR = default(T) switch
			{
				Float32 => new NM.cblas_axpyi<Float32>(NM.cblas_saxpyi) as NM.cblas_axpyi<T>,
				Float64 => new NM.cblas_axpyi<Float64>(NM.cblas_daxpyi) as NM.cblas_axpyi<T>,
				_ => null
			};
			var funcC = default(T) switch
			{
				Complex<Float32> => new NM.cblas_axpyi_comp<Complex<Float32>>(NM.cblas_caxpyi) as NM.cblas_axpyi_comp<T>,
				Complex<Float64> => new NM.cblas_axpyi_comp<Complex<Float64>>(NM.cblas_zaxpyi) as NM.cblas_axpyi_comp<T>,
				_ => null
			};
			if (funcR is null && funcC is null)
				return false;
			funcR?.Invoke(nnz, α, px, (MklInt*)pp, py);
			funcC?.Invoke(nnz, α, px, (MklInt*)pp, py);
			return true;
		}

		/// <inheritdoc/>
		public virtual bool VectorSparseDotDense<T, TInd, TS1, TS2, TSInd>(bool conjX, ISparseArray<T, TInd, TS1, TSInd> x, TS2 y, long strideY, out T dot) where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TSInd : class, IStorage<TInd, TSInd>
		{
			dot = default;
			if (x.Size.Length != 1)
				throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(x));
			if (strideY != 1 || x.Format != SparseFormat.VectorCooFormat || x.DefaultValue != T.Zero)
				return false;
			if (x.IndexStorages.Length != 1 || x.ValueStorages.Length != 1)
				throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(x));
			if (!GetPointer(x.ValueStorages[0], x.IndexStorages[0], out T* px, out TInd* pp, out var nnz))
				return false;
			if (!GetPointer(y, strideY, out T* py, out var n))
				return false;
			if (n < nnz || x.Size[0] != n)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(x));

			var funcR = default(T) switch
			{
				Float32 => new NM.cblas_doti<Float32>(NM.cblas_sdoti) as NM.cblas_doti<T>,
				Float64 => new NM.cblas_doti<Float64>(NM.cblas_ddoti) as NM.cblas_doti<T>,
				_ => null
			};
			var funcC = default(T) switch
			{
				Complex<Float32> when conjX => new NM.cblas_doti_comp<Complex<Float32>>(NM.cblas_cdotci_sub) as NM.cblas_doti_comp<T>,
				Complex<Float64> when conjX => new NM.cblas_doti_comp<Complex<Float64>>(NM.cblas_zdotci_sub) as NM.cblas_doti_comp<T>,
				Complex<Float32> when !conjX => new NM.cblas_doti_comp<Complex<Float32>>(NM.cblas_cdotui_sub) as NM.cblas_doti_comp<T>,
				Complex<Float64> when !conjX => new NM.cblas_doti_comp<Complex<Float64>>(NM.cblas_zdotui_sub) as NM.cblas_doti_comp<T>,
				_ => null
			};
			if (funcR is null && funcC is null)
				return false;
			if (funcR is not null)
				dot = funcR.Invoke(nnz, px, (MklInt*)pp, py);
			else
				funcC?.Invoke(nnz, px, (MklInt*)pp, py, out dot);
			return true;
		}

		/// <inheritdoc/>
		public virtual bool VectorSparseAddSparse<T, TInd1, TInd2, TInd3, TS1, TS2, TS3, TSInd1, TSInd2, TSInd3>(T α, ISparseArray<T, TInd1, TS1, TSInd1> x, ISparseArray<T, TInd2, TS2, TSInd2> y, ref SparseArrayWrapper<T, TInd3, TS3, TSInd3> target) where T : unmanaged, IBaseNumber<T> where TInd1 : unmanaged, IBinaryInt<TInd1> where TInd2 : unmanaged, IBinaryInt<TInd2> where TInd3 : unmanaged, IBinaryInt<TInd3> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3> where TSInd1 : class, IStorage<TInd1, TSInd1> where TSInd2 : class, IStorage<TInd2, TSInd2> where TSInd3 : class, IStorage<TInd3, TSInd3>
		{
			if (x.Size.Length != 1 || y.Size.Length != 1)
				throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(x));
			if (x.Size[0] != y.Size[0])
				throw new ArgumentException(Resources.ParameterError.NotSameSize);
			if (x.Format != SparseFormat.VectorCooFormat || y.Format != SparseFormat.VectorCooFormat || (target.Format & SparseFormat.VectorCooFormat) == SparseFormat.None || x.DefaultValue != T.Zero || y.DefaultValue != T.Zero || target.DefaultValue != T.Zero)
				return false;
			if (x.IndexStorages.Length != 1 || x.ValueStorages.Length != 1)
				throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(x));
			if (y.IndexStorages.Length != 1 || y.ValueStorages.Length != 1)
				throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(y));
			if (!GetPointer(x.ValueStorages[0], x.IndexStorages[0], out T* px, out TInd1* ppx, out var nnzx))
				return false;
			if (!GetPointer(y.ValueStorages[0], y.IndexStorages[0], out T* py, out TInd2* ppy, out var nnzy))
				return false;
			if (target.Size.Length is not 0 and not 1)
				throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(target));
			if (target.IndexStorages.Length is not 0 and not 1 || target.ValueStorages.Length is not 0 and not 1)
				throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(target));

			long bufferSize = NMC.vecSpAddBuffer(T.Type, nnzx, nnzy);
			if (bufferSize < 0)
				return false;
			using var buffer = ArrayPoolBuffers.Create<byte>(bufferSize);
			long nnz = NMC.vecSpAddNnz(T.Type, (MklInt*)ppx, px, nnzx, (MklInt*)ppy, py, nnzy, &α, buffer);
			TS3 valOut; T* pVal;
			TSInd3 idxOut; TInd3* pIdx;
			if (target.ValueStorages.Length == 1)
			{
				valOut = target.ValueStorages[0];
				if (!GetPointer(valOut, out pVal, out var n2) || n2 != nnz)
					return false;
			}
			else
			{
				var s = PureStorage<T, CpuMemoryPointer>.Create(nnz);
				pVal = (T*)s.Pointer.Pointer.Pointer;
				valOut = s as TS3 ?? TS3.Empty; // never empty
			}
			if (target.IndexStorages.Length == 2)
			{
				idxOut = target.IndexStorages[0];
				if (!GetPointer(idxOut, out pIdx, out var n2) || n2 != nnz)
					return false;
			}
			else
			{
				var s = PureStorage<TInd3, CpuMemoryPointer>.Create(nnz);
				pIdx = (TInd3*)s.Pointer.Pointer.Pointer;
				idxOut = s as TSInd3 ?? TSInd3.Empty; // never empty
			}
			_ = NMC.vecSpAddCal(T.Type, buffer, nnzx + nnzy, nnz, (MklInt*)pIdx, pVal);
			target.SetValues(x.Size[0], valOut, idxOut);
			target.Format = SparseFormat.VectorCooFormat;
			return true;
		}
		#endregion

		#region vector and matrix computation
		/// <inheritdoc/>
		public virtual bool MatrixSparseMultiplyVectorDense<T, TInd, TS1, TS2, TS3, TSInd>(MatrixOperation op, T α, ISparseArray<T, TInd, TS1, TSInd> M, TS2 x, long strideX, T β, TS3 y, long strideY) where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3> where TSInd : class, IStorage<TInd, TSInd>
		{

		}
		#endregion
	}
}
