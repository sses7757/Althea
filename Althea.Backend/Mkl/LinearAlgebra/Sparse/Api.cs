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
			foreach (var kv in this.mvCache)
				FreeSparseMatrix(kv.Value);
			foreach (var kv in this.mmCache)
				FreeSparseMatrix(kv.Value);
			this.Disposed = true;
			GC.SuppressFinalize(this);
		}

		/// <inheritdoc/>
		public bool Disposed { get; protected set; } = false;

		private readonly Dictionary<(object matrix, MatrixOp trans), IntPtr> mvCache = new();
		private readonly Dictionary<(object matrix, MatrixOp trans, long cols), IntPtr> mmCache = new();

		// TODO: support cache
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
		public virtual bool VectorSparseToDense<T, TInd, TS1, TS2, TSInd>(ISparseArray<T, TInd, TS1, TSInd> x, TS2 y, long strideY) where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TSInd : class, IStorage<TInd, TSInd>
		{
			if (x.Size.Length != 1)
				throw new ArgumentException(Resources.ParameterError.InvalidRank, nameof(x));
			if (strideY != 1 || x.Format != SparseFormat.VectorCooFormat || x.DefaultValue != T.Zero)
				return false;
			if (x.IndexStorages.Length != 1 || x.ValueStorages.Length != 1)
				return false;
			if (!GetPointer(x, out T* px, out var pp, out var n))
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
			func(n, px, pp, py);
			return true;
		}

		/// <inheritdoc/>
		public virtual bool VectorDenseToSparse<T, TInd, TS1, TS2, TSInd>(TS1 x, long strideX, ref SparseArrayWrapper<T, TInd, TS2, TSInd> y, double threshold = 0) where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd> where TS2 : class, IStorage<T, TS2> where TS1 : class, IStorage<T, TS1> where TSInd : class, IStorage<TInd, TSInd>
		{
			if (sizeof(TInd) != sizeof(MklInt))
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
				y.SetValues(n, (valOut as TS2)!, (idxOut as TSInd)!);
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
			if (!GetPointer(vector, out T* px, out var pp, out var nnz))
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
				rowIdxOut = (s as TSInd2)!;
				s = PureStorage<TInd2, CpuMemoryPointer>.Create(nnz);
				pCol = (TInd2*)s.Pointer.Pointer.Pointer;
				colIdxOut = (s as TSInd2)!;
			}
			Buffer.MemoryCopy(px, pVal, nnz * sizeof(T), nnz * sizeof(T));
			NMC.spVecIdxToCooIdxs(pp, (MklInt*)pRow, (MklInt*)pCol, nnz, vector.Size[0]);
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
			NMC.cooIdxsToSpVecIdx((MklInt*)pIdx, pr, pc, nnz, matrix.Size[0]);
			target.SetValues(matrix.Size.Prod(), valOut, idxOut);
			target.Format = SparseFormat.VectorCooFormat;
			target.DefaultValue = T.Zero;
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
			if (!target.ValueStorages.IsEmpty || !target.IndexStorages.IsEmpty)
				return false; // not supported by MKL
			if (target.Size.Length is not 0 and not 2)
				throw new ArgumentException(Resources.ParameterError.InvalidRank, nameof(target));
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
			{
				if (NM.mkl_sparse_convert_csr(handleSrc, targetRow ? MatrixOp.None : MatrixOp.Trans, out handleDst).Check())
					return false;
			}
			else
			{
				if (NM.mkl_sparse_convert_bsr(handleSrc, target.BlockSize[0], targetRow ? MatrixMajor.Row : MatrixMajor.Column, targetRow ? MatrixOp.None : MatrixOp.Trans, out handleDst).Check())
					return false;
			}
			using var dst = new SparseMatrixHandle(handleDst, targetCsr ? SparseFormat.MatrixCsrFormat : SparseFormat.MatrixBsrFormat, targetRow ? MatrixOperation.None : MatrixOperation.Transpose);
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
			if (strideY != 1)
				return false;
			if (!GetPointer(x, out T* px, out var pp, out var nnz))
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
			funcR?.Invoke(nnz, α, px, pp, py);
			funcC?.Invoke(nnz, α, px, pp, py);
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
			if (!GetPointer(x, out T* px, out var pp, out var nnz))
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
				dot = funcR.Invoke(nnz, px, pp, py);
			else
				funcC?.Invoke(nnz, px, pp, py, out dot);
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
			if (!GetPointer(x, out T* px, out var ppx, out var nnzx))
				return false;
			if (!GetPointer(y, out T* py, out var ppy, out var nnzy))
				return false;
			if (target.Size.Length is not 0 and not 1)
				throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(target));
			if (target.IndexStorages.Length is not 0 and not 1 || target.ValueStorages.Length is not 0 and not 1)
				throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(target));

			long bufferSize = NMC.vecSpAddBuffer(T.Type, nnzx, nnzy);
			if (bufferSize < 0)
				return false;
			using var buffer = ArrayPoolBuffers.Create<byte>(bufferSize);
			long nnz = NMC.vecSpAddNnz(T.Type, ppx, px, nnzx, ppy, py, nnzy, &α, buffer);
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
			target.DefaultValue = T.Zero;
			return true;
		}
		#endregion

		#region vector and matrix computation
		private static readonly MatrixDescr GeneralMatrix = new(MatrixType.General, MatrixFillMode.Full, MatrixDiagType.NonUnit);

		/// <inheritdoc/>
		public virtual bool MatrixSparseMultiplyVectorDense<T, TInd, TS1, TS2, TS3, TSInd>(MatrixOperation op, T α, ISparseArray<T, TInd, TS1, TSInd> M, TS2 x, long strideX, T β, TS3 y, long strideY) where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3> where TSInd : class, IStorage<TInd, TSInd>
		{
			if (α == T.Zero)
				throw new ArgumentOutOfRangeException(nameof(α), α, Resources.ParameterError.CannotZero);
			bool conjY = op == MatrixOperation.Conjugate;
			if (strideX != 1 || strideY != 1 || (conjY && β != T.Zero))
				return false; // not supported
			if (!GetPointer(M, out T* pm, out var pr, out var pc, out var nnz))
				return false;
			if (!GetPointer(x, strideX, out T* px, out var nx))
				return false;
			if (!GetPointer(y, strideY, out T* py, out var ny))
				return false;
			var (m, n) = (M.Size[0], M.Size[1]);
			if (!op.CanInPlace())
				(m, n) = (n, m);
			if (n != nx || m != ny)
				throw new ArgumentException(Resources.ParameterError.WrongSize);

			using var descrM = SparseMatrixHandle.TryCreate(M, out bool success);
			if (!success)
				return false;
			var func = default(T) switch
			{
				Float32 => new NM.mkl_sparse__mv<Float32>(NM.mkl_sparse_s_mv) as NM.mkl_sparse__mv<T>,
				Float64 => new NM.mkl_sparse__mv<Float64>(NM.mkl_sparse_d_mv) as NM.mkl_sparse__mv<T>,
				Complex<Float32> => new NM.mkl_sparse__mv<Complex<Float32>>(NM.mkl_sparse_c_mv) as NM.mkl_sparse__mv<T>,
				Complex<Float64> => new NM.mkl_sparse__mv<Complex<Float64>>(NM.mkl_sparse_z_mv) as NM.mkl_sparse__mv<T>,
				_ => null
			};
			if (func is null)
				return false;
			if (func.Invoke(op.ToOp(), α, descrM, GeneralMatrix, px, β, py).Check())
				return false;
			if (conjY)
				Dense.Conjugater.Conjugate(py, ny, 1);
			return true;
		}

		/// <inheritdoc/>
		public virtual bool VectorSparseOuter<T, TInd1, TInd2, TInd3, TS1, TS2, TS3, TSInd1, TSInd2, TSInd3>(bool conjY, ISparseArray<T, TInd1, TS1, TSInd1> x, ISparseArray<T, TInd2, TS2, TSInd2> y, ref SparseArrayWrapper<T, TInd3, TS3, TSInd3> target) where T : unmanaged, IBaseNumber<T> where TInd1 : unmanaged, IBinaryInt<TInd1> where TInd2 : unmanaged, IBinaryInt<TInd2> where TInd3 : unmanaged, IBinaryInt<TInd3> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3> where TSInd1 : class, IStorage<TInd1, TSInd1> where TSInd2 : class, IStorage<TInd2, TSInd2> where TSInd3 : class, IStorage<TInd3, TSInd3>
		{
			if ((target.Format & SparseFormat.MatrixCooFormat) == SparseFormat.None)
				return false; // not supported
			if (!GetPointer(x, out T* px, out var ppx, out var nnzx))
				return false;
			if (!GetPointer(y, out T* py, out var ppy, out var nnzy))
				return false;
			if (sizeof(TInd3) != sizeof(MklInt))
				return false;
			if (target.ValueStorages.Length is not 0 and not 1 || target.IndexStorages.Length is not 0 and not 2)
				throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(target));

			if (NMC.spVecOuterCheck(T.Type) < 0)
				return false;
			TS3 valOut; T* pVal;
			TSInd3 rowIdxOut, colIdxOut; TInd3* pRow, pCol;
			long nnz = nnzx * nnzy;
			if (target.ValueStorages.Length == 1)
			{
				valOut = target.ValueStorages[0].MakeReference();
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
				rowIdxOut = target.IndexStorages[0].MakeReference();
				colIdxOut = target.IndexStorages[1].MakeReference();
				if (!GetPointer(rowIdxOut, out pRow, out var n2) || n2 != nnz)
					return false;
				if (!GetPointer(colIdxOut, out pCol, out n2) || n2 != nnz)
					return false;
			}
			else
			{
				var s = PureStorage<TInd3, CpuMemoryPointer>.Create(nnz);
				pRow = (TInd3*)s.Pointer.Pointer.Pointer;
				rowIdxOut = s as TSInd3 ?? TSInd3.Empty; // never empty
				s = PureStorage<TInd3, CpuMemoryPointer>.Create(nnz);
				pCol = (TInd3*)s.Pointer.Pointer.Pointer;
				colIdxOut = s as TSInd3 ?? TSInd3.Empty; // never empty
			}
			_ = NMC.spVecOuter(T.Type, px, ppx, nnzx, py, ppy, nnzy, pVal, (MklInt*)pRow, (MklInt*)pCol, conjY);
			target.SetValues(x.Size[0], y.Size[0], valOut, rowIdxOut, colIdxOut);
			target.Format = SparseFormat.MatrixCocFormat;
			target.DefaultValue = T.Zero;
			return true;
		}
		#endregion

		#region matrix computation
		/// <inheritdoc/>
		public virtual bool MatrixSparseAddSparse<T, TInd1, TInd2, TInd3, TS1, TS2, TS3, TSInd1, TSInd2, TSInd3>(MatrixOperation opA, MatrixOperation opB, T α, ISparseArray<T, TInd1, TS1, TSInd1>? A, T β, ISparseArray<T, TInd2, TS2, TSInd2>? B, ref SparseArrayWrapper<T, TInd3, TS3, TSInd3> target) where T : unmanaged, IBaseNumber<T> where TInd1 : unmanaged, IBinaryInt<TInd1> where TS1 : class, IStorage<T, TS1> where TSInd1 : class, IStorage<TInd1, TSInd1> where TInd2 : unmanaged, IBinaryInt<TInd2> where TS2 : class, IStorage<T, TS2> where TSInd2 : class, IStorage<TInd2, TSInd2> where TInd3 : unmanaged, IBinaryInt<TInd3> where TS3 : class, IStorage<T, TS3> where TSInd3 : class, IStorage<TInd3, TSInd3>
		{
			if (target.Format != SparseFormat.Any)
				return false; // not supported
			if (sizeof(TInd3) != sizeof(MklInt))
				return false; // not supported
			if (target.ValueStorages.Length != 0 || target.IndexStorages.Length != 0)
				return false; // not supported
			if (α == T.Zero)
				A = null;
			if (β == T.Zero)
				B = null;
			if (A is null && B is null)
				throw new ArgumentException(Resources.ParameterError.CannotAllNull);

			opA = opA.Simplify<T>(); opB = opB.Simplify<T>();
			if (A is not null && B is not null)
			{
				using var descrA = SparseMatrixHandle.TryCreate(A, out bool success);
				if (!success)
					return false;
				using var descrB = SparseMatrixHandle.TryCreate(B, out success);
				if (!success)
					return false;
				var func = default(T) switch
				{
					Float32 => new NM.mkl_sparse__add<Float32>(NM.mkl_sparse_s_add) as NM.mkl_sparse__add<T>,
					Float64 => new NM.mkl_sparse__add<Float64>(NM.mkl_sparse_d_add) as NM.mkl_sparse__add<T>,
					Complex<Float32> => new NM.mkl_sparse__add<Complex<Float32>>(NM.mkl_sparse_c_add) as NM.mkl_sparse__add<T>,
					Complex<Float64> => new NM.mkl_sparse__add<Complex<Float64>>(NM.mkl_sparse_z_add) as NM.mkl_sparse__add<T>,
					_ => null
				};
				if (func is null)
					return false;
				MatrixOp op; MatrixOperation opC;
				SparseMatrixHandle hA = descrA, hB = descrB;
				if (opA.CanInPlace())
				{
					if ((opA == MatrixOperation.Conjugate && opB == MatrixOperation.None) || (opA == MatrixOperation.None && opB == MatrixOperation.Conjugate))
						return false;
					op = opA.HasConjugate() ? opB.Conjugate().ToOp() : opB.ToOp();
					opC = opA;
					(α, β) = (β, α); hA = descrB; hB = descrA;
				}
				else
				{
					if ((opA == MatrixOperation.Transpose && opB == MatrixOperation.ConjugateTranspose) || (opA == MatrixOperation.ConjugateTranspose && opB == MatrixOperation.Transpose))
						return false;
					opC = opB;
					if (opB.HasConjugate())
						opA = opA.Conjugate();
					if (!opB.CanInPlace())
						opA = opA.Transpose();
					op = opA.ToOp();
				}
				T alpha = α / β;
				if (func.Invoke(op, alpha, hA, hB, out IntPtr outC).Check())
					return false;
				using var descrC = new SparseMatrixHandle(outC, B.Format, opC);
				descrC.Deconstruct(ref target);

			}
			else if (A is not null)
			{
				using var descrA = SparseMatrixHandle.TryCreate(A, out bool success);
				if (!success)
					return false;
				if (NM.mkl_sparse_convert_csr(descrA, opA.ToOp(), out IntPtr outC).Check())
					return false;
				using var descrC = new SparseMatrixHandle(outC, SparseFormat.MatrixCsrFormat, opA == MatrixOperation.Conjugate ? opA : MatrixOperation.None);
				descrC.Deconstruct(ref target);
			}
			else if (B is not null)
			{
				using var descrB = SparseMatrixHandle.TryCreate(B, out bool success);
				if (!success)
					return false;
				if (NM.mkl_sparse_convert_csr(descrB, opB.ToOp(), out IntPtr outC).Check())
					return false;
				using var descrC = new SparseMatrixHandle(outC, SparseFormat.MatrixCsrFormat, opB == MatrixOperation.Conjugate ? opB : MatrixOperation.None);
				descrC.Deconstruct(ref target);
			}
			return true;
		}

		/// <inheritdoc/>
		public virtual bool MatrixSparseMultiplySparse<T, TInd1, TInd2, TInd3, TS1, TS2, TS3, TSInd1, TSInd2, TSInd3>(MatrixOperation opA, MatrixOperation opB, T α, ISparseArray<T, TInd1, TS1, TSInd1> A, ISparseArray<T, TInd2, TS2, TSInd2> B, T β, ISparseArray<T, TInd3, TS3, TSInd3>? C, ref SparseArrayWrapper<T, TInd3, TS3, TSInd3> target) where T : unmanaged, IBaseNumber<T> where TInd1 : unmanaged, IBinaryInt<TInd1> where TS1 : class, IStorage<T, TS1> where TSInd1 : class, IStorage<TInd1, TSInd1> where TInd2 : unmanaged, IBinaryInt<TInd2> where TS2 : class, IStorage<T, TS2> where TSInd2 : class, IStorage<TInd2, TSInd2> where TInd3 : unmanaged, IBinaryInt<TInd3> where TS3 : class, IStorage<T, TS3> where TSInd3 : class, IStorage<TInd3, TSInd3>
		{
			if (α == T.Zero)
				throw new ArgumentOutOfRangeException(nameof(α), α, Resources.ParameterError.CannotZero);
			if (target.Format != SparseFormat.Any)
				return false; // not supported
			if (sizeof(TInd3) != sizeof(MklInt))
				return false; // not supported
			if (target.ValueStorages.Length != 0 || target.IndexStorages.Length != 0)
				return false; // not supported
			if (β != T.Zero && C is not null)
				return false; // not supported
			opA = opA.Simplify<T>(); opB = opB.Simplify<T>();
			if ((opA == MatrixOperation.Conjugate && opB == MatrixOperation.None) || (opA == MatrixOperation.None && opB == MatrixOperation.Conjugate))
				return false;
			using var descrA = SparseMatrixHandle.TryCreate(A, out bool success);
			if (!success)
				return false;
			using var descrB = SparseMatrixHandle.TryCreate(B, out success);
			if (!success)
				return false;

			IntPtr outC = default;
			if (NM.mkl_sparse_sp2m(opA.ToOp(), GeneralMatrix, descrA, opB.ToOp(), GeneralMatrix, descrB, Request.FullMultiply, ref outC).Check())
				return false;
			NM.mkl_sparse_order(outC).Check();
			using var descrC = new SparseMatrixHandle(outC, SparseFormat.Any, opA == MatrixOperation.Conjugate ? opA : MatrixOperation.None);
			if (descrC.Deconstruct(ref target).Check())
				return false;
			return true;
		}

		/// <inheritdoc/>
		public virtual bool MatrixSparseMultiplySparse<T, TInd1, TInd2, TS1, TS2, TS3, TSInd1, TSInd2>(MatrixOperation opA, MatrixOperation opB, T α, ISparseArray<T, TInd1, TS1, TSInd1> A, ISparseArray<T, TInd2, TS2, TSInd2> B, T β, TS3 C, long ldc) where T : unmanaged, IBaseNumber<T> where TInd1 : unmanaged, IBinaryInt<TInd1> where TS1 : class, IStorage<T, TS1> where TSInd1 : class, IStorage<TInd1, TSInd1> where TInd2 : unmanaged, IBinaryInt<TInd2> where TS2 : class, IStorage<T, TS2> where TSInd2 : class, IStorage<TInd2, TSInd2> where TS3 : class, IStorage<T, TS3>
		{
			if (α == T.Zero)
				throw new ArgumentOutOfRangeException(nameof(α), α, Resources.ParameterError.CannotZero);
			long m = opA.CanInPlace() ? A.Size[0] : A.Size[1], n = opB.CanInPlace() ? B.Size[1] : B.Size[0];
			if (!GetPointer(C, m, n, ldc, out T* pc))
				return false;
			opA = opA.Simplify<T>(); opB = opB.Simplify<T>();
			if ((opA == MatrixOperation.Conjugate && opB == MatrixOperation.None) || (opA == MatrixOperation.None && opB == MatrixOperation.Conjugate))
				return false;
			using var descrA = SparseMatrixHandle.TryCreate(A, out bool success);
			if (!success)
				return false;
			using var descrB = SparseMatrixHandle.TryCreate(B, out success);
			if (!success)
				return false;

			var func = default(T) switch
			{
				Float32 => new NM.mkl_sparse__sp2md<Float32>(NM.mkl_sparse_s_sp2md) as NM.mkl_sparse__sp2md<T>,
				Float64 => new NM.mkl_sparse__sp2md<Float64>(NM.mkl_sparse_d_sp2md) as NM.mkl_sparse__sp2md<T>,
				Complex<Float32> => new NM.mkl_sparse__sp2md<Complex<Float32>>(NM.mkl_sparse_c_sp2md) as NM.mkl_sparse__sp2md<T>,
				Complex<Float64> => new NM.mkl_sparse__sp2md<Complex<Float64>>(NM.mkl_sparse_z_sp2md) as NM.mkl_sparse__sp2md<T>,
				_ => null
			};
			if (func is null)
				return false;
			if (func.Invoke(opA.ToOp(), GeneralMatrix, descrA, opB.ToOp(), GeneralMatrix, descrB, α, β, pc, MatrixMajor.Column, ldc).Check())
				return false;
			if (opA == MatrixOperation.Conjugate)
				Dense.Conjugater.Conjugate(pc, m, n, ldc);
			return true;
		}

		/// <inheritdoc/>
		public virtual bool MatrixDenseMultiplySparse<T, TInd, TS1, TS2, TS3, TSInd>(MatrixOperation opA, MatrixOperation opB, long m, T α, TS1 A, long lda, ISparseArray<T, TInd, TS2, TSInd> B, T β, TS3 C, long ldc) where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3> where TSInd : class, IStorage<TInd, TSInd>
		{
			if (α == T.Zero)
				throw new ArgumentOutOfRangeException(nameof(α), α, Resources.ParameterError.CannotZero);
			return false;
		}

		/// <inheritdoc/>
		public virtual bool MatrixSparseMultiplyDense<T, TInd, TS1, TS2, TS3, TSInd>(MatrixOperation opA, MatrixOperation opB, long n, T α, ISparseArray<T, TInd, TS1, TSInd> A, TS2 B, long ldb, T β, TS3 C, long ldc) where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3> where TSInd : class, IStorage<TInd, TSInd>
		{
			if (α == T.Zero)
				throw new ArgumentOutOfRangeException(nameof(α), α, Resources.ParameterError.CannotZero);
			if (!opB.CanInPlace())
				return false;
			var (m, k) = opA.CanInPlace() ? (A.Size[0], A.Size[1]) : (A.Size[1], A.Size[0]);
			if (!GetPointer(B, k, n, ldb, out T* pb))
				return false;
			if (!GetPointer(C, m, n, ldc, out T* pc))
				return false;
			using var descrA = SparseMatrixHandle.TryCreate(A, out bool success);
			if (!success)
				return false;

			var func = default(T) switch
			{
				Float32 => new NM.mkl_sparse__mm<Float32>(NM.mkl_sparse_s_mm) as NM.mkl_sparse__mm<T>,
				Float64 => new NM.mkl_sparse__mm<Float64>(NM.mkl_sparse_d_mm) as NM.mkl_sparse__mm<T>,
				Complex<Float32> => new NM.mkl_sparse__mm<Complex<Float32>>(NM.mkl_sparse_c_mm) as NM.mkl_sparse__mm<T>,
				Complex<Float64> => new NM.mkl_sparse__mm<Complex<Float64>>(NM.mkl_sparse_z_mm) as NM.mkl_sparse__mm<T>,
				_ => null
			};
			if (func is null)
				return false;
			if (opB == MatrixOperation.Conjugate)
				opA = opA.Conjugate();
			if (func.Invoke(opA.ToOp(), α, descrA, GeneralMatrix, MatrixMajor.Column, pb, n, ldb, β, pc, ldc).Check())
				return false;
			if (opB == MatrixOperation.Conjugate)
				Dense.Conjugater.Conjugate(pc, m, n, ldc);
			return true;
		}

		/// <inheritdoc/>
		public virtual bool MatrixSparseKronecker<T, TInd1, TInd2, TInd3, TS1, TS2, TS3, TSInd1, TSInd2, TSInd3>(ISparseArray<T, TInd1, TS1, TSInd1> A, ISparseArray<T, TInd2, TS2, TSInd2> B, ref SparseArrayWrapper<T, TInd3, TS3, TSInd3> target) where T : unmanaged, IBaseNumber<T> where TInd1 : unmanaged, IBinaryInt<TInd1> where TS1 : class, IStorage<T, TS1> where TSInd1 : class, IStorage<TInd1, TSInd1> where TInd2 : unmanaged, IBinaryInt<TInd2> where TS2 : class, IStorage<T, TS2> where TSInd2 : class, IStorage<TInd2, TSInd2> where TInd3 : unmanaged, IBinaryInt<TInd3> where TS3 : class, IStorage<T, TS3> where TSInd3 : class, IStorage<TInd3, TSInd3>
		{
			if ((target.Format & SparseFormat.MatrixCocFormat) == SparseFormat.None ||
				(A.Format & SparseFormat.MatrixCooFormat) == SparseFormat.None || (B.Format & SparseFormat.MatrixCooFormat) == SparseFormat.None)
				return false; // not supported
			if (!GetPointer(A, out T* pa, out var pra, out var pca, out var nnza))
				return false;
			if (!GetPointer(B, out T* pb, out var prb, out var pcb, out var nnzb))
				return false;
			if (sizeof(TInd3) != sizeof(MklInt))
				return false;
			if (target.ValueStorages.Length is not 0 and not 1 || target.IndexStorages.Length is not 0 and not 2)
				throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(target));

			if (NMC.spVecOuterCheck(T.Type) < 0)
				return false;
			TS3 valOut; T* pVal;
			TSInd3 rowIdxOut, colIdxOut; TInd3* pRow, pCol;
			long nnz = nnza * nnzb;
			if (target.ValueStorages.Length == 1)
			{
				valOut = target.ValueStorages[0].MakeReference();
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
				rowIdxOut = target.IndexStorages[0].MakeReference();
				colIdxOut = target.IndexStorages[1].MakeReference();
				if (!GetPointer(rowIdxOut, out pRow, out var n2) || n2 != nnz)
					return false;
				if (!GetPointer(colIdxOut, out pCol, out n2) || n2 != nnz)
					return false;
			}
			else
			{
				var s = PureStorage<TInd3, CpuMemoryPointer>.Create(nnz);
				pRow = (TInd3*)s.Pointer.Pointer.Pointer;
				rowIdxOut = s as TSInd3 ?? TSInd3.Empty; // never empty
				s = PureStorage<TInd3, CpuMemoryPointer>.Create(nnz);
				pCol = (TInd3*)s.Pointer.Pointer.Pointer;
				colIdxOut = s as TSInd3 ?? TSInd3.Empty; // never empty
			}
			_ = NMC.CooMatKron(T.Type, pa, pra, pca, nnza, pb, prb, pcb, nnzb, B.Size[0], B.Size[1], pVal, (MklInt*)pRow, (MklInt*)pCol);
			target.SetValues(A.Size[0] * B.Size[0], A.Size[1] * B.Size[1], valOut, rowIdxOut, colIdxOut);
			target.Format = SparseFormat.MatrixCocFormat;
			target.DefaultValue = T.Zero;
			return true;
		}
		#endregion

		#region unsupported computation
		bool IComputationAbstractApi.VectorSparseDotSparse<T, TInd1, TInd2, TS1, TS2, TSInd1, TSInd2>(bool conjX, ISparseArray<T, TInd1, TS1, TSInd1> x, ISparseArray<T, TInd2, TS2, TSInd2> y, out T dot) { dot = default; return false; }
		bool IComputationAbstractApi.VectorSparsePointwiseMultiplyDense<T, TInd, TS1, TS2, TSInd>(ISparseArray<T, TInd, TS1, TSInd> x, TS2 y, long strideY) => false;
		bool IComputationAbstractApi.VectorSparsePointwiseDivideDense<T, TInd, TS1, TS2, TSInd>(ISparseArray<T, TInd, TS1, TSInd> x, TS2 y, long strideY) => false;
		bool IComputationAbstractApi.MatrixDenseMultiplyVectorSparse<T, TInd, TS1, TS2, TS3, TSInd>(MatrixOperation op, T α, long m, TS2 M, long ldm, ISparseArray<T, TInd, TS1, TSInd> x, T β, TS3 y, long strideY) => false;
		bool IComputationAbstractApi.SparseMatrixGetDiag<T, TInd1, TInd2, TS1, TS2, TSInd1, TSInd2>(ISparseArray<T, TInd1, TS1, TSInd1> source, long k, ref SparseArrayWrapper<T, TInd2, TS2, TSInd2> vector) => false;
		bool IComputationAbstractApi.SparseMatrixSetDiag<T, TInd1, TInd2, TS1, TS2, TSInd1, TSInd2>(ISparseArray<T, TInd2, TS2, TSInd2> matrix, long k, ISparseArray<T, TInd1, TS1, TSInd1> vector) => false;
		bool IComputationAbstractApi.MatrixDenseAddSparse<T, TInd, TS1, TS2, TS3, TSInd>(MatrixOperation opA, MatrixOperation opB, T α, TS1? A, long lda, T β, ISparseArray<T, TInd, TS2, TSInd> B, TS3 C, long ldc) where TS1 : class => false;
		#endregion

		#region index operations
		/// <inheritdoc/>
		public virtual bool Sort<T, TS>(TS array, long stride) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
		{
			if (!GetPointer(array, stride, out T* ptr, out var n))
				return false;
			return NMC.vecSort(T.Type, ptr, n, (int)stride) == 0;
		}

		/// <inheritdoc/>
		public virtual bool Sort<T, TOther, TS, TS2>(TS keys, long strideKeys, TS2 values, long strideValues) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS> where TOther : unmanaged, IBaseNumber<TOther> where TS2 : class, IStorage<TOther, TS2>
		{
			if (!GetPointer(keys, strideKeys, out T* pk, out var n))
				return false;
			if (!GetPointer(values, strideValues, out TOther* pv, out var n2))
				return false;
			if (n2 != n)
				throw new ArgumentException(Resources.ParameterError.NotSameSize);
			return NMC.vecSortBy(T.Type, TOther.Type, pk, pv, n, (int)strideKeys, (int)strideValues) == 0;
		}

		/// <inheritdoc/>
		public virtual bool IndexOf<T, TS>(TS array, long stride, bool sorted, T value, out long find) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
		{
			find = -1;
			if (!GetPointer(array, stride, out T* ptr, out var n))
				return false;
			return NMC.vecFind(T.Type, sorted, ptr, n, (int)stride, &value, out find) == 0;
		}

		/// <inheritdoc/>
		public virtual bool BoundOf<T, TS>(TS array, long stride, T value, bool lowerBound, out long index) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
		{
			index = -1;
			if (!GetPointer(array, stride, out T* ptr, out var n))
				return false;
			return NMC.vecBound(T.Type, lowerBound, ptr, n, (int)stride, &value, out index) == 0;
		}

		/// <inheritdoc/>
		public virtual bool FillWithRange<T, TS>(TS array, long stride, T start, T step) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
		{
			if (!GetPointer(array, stride, out T* ptr, out var n))
				return false;
			return NMC.vecFillRange(T.Type, ptr, n, (int)stride, &start, &step) == 0;
		}
		#endregion
	}
}
