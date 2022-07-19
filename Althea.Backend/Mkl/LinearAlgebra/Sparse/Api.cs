using System.Runtime.CompilerServices;

using Althea.Array;
using Althea.Backend.CSharp.Storage;
using Althea.Backend.Storage;
using Althea.LinearAlgebra;
using Althea.LinearAlgebra.Sparse;
using Althea.Linq;

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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool GetPointer<T, TS>(TS s, out T* pointer, out long length, [CallerArgumentExpression("s")] string? sName = null) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
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
			if (length > MklInt.MaxValue || length < 0)
				return false;
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool GetPointer<T, TInd, TS, TSInd>(TS s, TSInd sInd, out T* pointer, out TInd* pointerInd, out long length, [CallerArgumentExpression("s")] string? sName = null, [CallerArgumentExpression("sInd")] string? sIndName = null) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS> where TInd : unmanaged, IBinaryInt<TInd> where TSInd : class, IStorage<TInd, TSInd>
		{
			pointer = default; pointerInd = default; length = 0;
			if (!TInd.Type.IsInteger() || TInd.Size != sizeof(MklInt))
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
			if (length > MklInt.MaxValue || length < 0)
				return false;
			if (psInd.Length != length)
				throw new ArgumentException(Resources.ParameterError.NotSameSize, sIndName);
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool GetPointer<T, TInd, TS, TSInd>(ISparseArray<T, TInd, TS, TSInd> matrix, out T* pointer, out TInd* pointerRow, out TInd* pointerCol, out long nnz, [CallerArgumentExpression("matrix")] string? matrixName = null) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS> where TInd : unmanaged, IBinaryInt<TInd> where TSInd : class, IStorage<TInd, TSInd>
		{
			pointer = default; pointerRow = pointerCol = default; nnz = 0;
			if (!TInd.Type.IsInteger() || TInd.Size != sizeof(MklInt))
				return false;
			if (matrix is null)
				throw new ArgumentNullException(matrixName);
			if ((matrix.Format.Class & (SparseFormat.Type.Coordinated | SparseFormat.Type.Compressed)) == 0 ||
				(matrix.Format.BlockType & (SparseFormat.Blocking.Element | SparseFormat.Blocking.Simple)) == 0 ||
				(matrix.Format.MajorType & (SparseFormat.Major.Column | SparseFormat.Major.Row)) == 0)
				return false;
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

		private static readonly SparseFormat VectorFormat = new(SparseFormat.Type.Coordinated, SparseFormat.Blocking.Element);

		/// <inheritdoc/>
		public virtual bool VectorSparseToDense<T, TInd, TS1, TS2, TSInd>(ISparseArray<T, TInd, TS1, TSInd> x!!, TS2 y, long strideY) where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TSInd : class, IStorage<TInd, TSInd>
		{
			if (x.Size.Length != 1)
				throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(x));
			if (strideY != 1 || x.Format != VectorFormat)
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
			if (strideX != 1 || y.Format != VectorFormat || y.DefaultValue != T.Zero)
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
				using var buf = Buffers.Create<byte>(bufSize);
				long nnz = NMC.vecPruneNnz(T.Type, px, &thre, n, buf);
				var valOut = PureStorage<T, CpuMemoryPointer>.Create(stackalloc long[] { nnz });
				var idxOut = PureStorage<TInd, CpuMemoryPointer>.Create(stackalloc long[] { nnz });
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
		private static readonly SparseFormat CooFormat = new(SparseFormat.Type.Coordinated, SparseFormat.Blocking.Element, SparseFormat.Major.Column);

		/// <inheritdoc/>
		public virtual bool SparseVectorToMatrix<T, TInd1, TInd2, TS1, TS2, TSInd1, TSInd2>(ISparseArray<T, TInd1, TS1, TSInd1> vector!!, ref SparseArrayWrapper<T, TInd2, TS2, TSInd2> target) where T : unmanaged, IBaseNumber<T> where TInd1 : unmanaged, IBinaryInt<TInd1> where TS1 : class, IStorage<T, TS1> where TSInd1 : class, IStorage<TInd1, TSInd1> where TInd2 : unmanaged, IBinaryInt<TInd2> where TS2 : class, IStorage<T, TS2> where TSInd2 : class, IStorage<TInd2, TSInd2>
		{
			if (!TInd1.Type.IsInteger() || TInd1.Size != sizeof(MklInt))
				return false;
			if (typeof(TInd1) != typeof(TInd2))
				return false;
			if (vector.Format != VectorFormat || (target.Format & CooFormat) != CooFormat)
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
				var s = PureStorage<T, CpuMemoryPointer>.Create(stackalloc long[] { nnz });
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
				var s = PureStorage<TInd2, CpuMemoryPointer>.Create(stackalloc long[] { nnz });
				pRow = (TInd2*)s.Pointer.Pointer.Pointer;
				rowIdxOut = s as TSInd2 ?? TSInd2.Empty; // never empty
				s = PureStorage<TInd2, CpuMemoryPointer>.Create(stackalloc long[] { nnz });
				pCol = (TInd2*)s.Pointer.Pointer.Pointer;
				colIdxOut = s as TSInd2 ?? TSInd2.Empty; // never empty
			}
			Buffer.MemoryCopy(px, pVal, nnz * sizeof(T), nnz * sizeof(T));
			NMC.spVecIdxToCooIdxs((MklInt*)pp, (MklInt*)pRow, (MklInt*)pCol, nnz, vector.Size[0]);
			target.SetValues(valOut, rowIdxOut, colIdxOut);
			target.Format = CooFormat;
			return true;
		}

		/// <inheritdoc/>
		public virtual bool SparseMatrixToVector<T, TInd1, TInd2, TS1, TS2, TSInd1, TSInd2>(ISparseArray<T, TInd1, TS1, TSInd1> matrix, ref SparseArrayWrapper<T, TInd2, TS2, TSInd2> target) where T : unmanaged, IBaseNumber<T> where TInd1 : unmanaged, IBinaryInt<TInd1> where TS1 : class, IStorage<T, TS1> where TSInd1 : class, IStorage<TInd1, TSInd1> where TInd2 : unmanaged, IBinaryInt<TInd2> where TS2 : class, IStorage<T, TS2> where TSInd2 : class, IStorage<TInd2, TSInd2>
		{
			if (!TInd1.Type.IsInteger() || TInd1.Size != sizeof(MklInt))
				return false;
			if (typeof(TInd1) != typeof(TInd2))
				return false;
			if ((target.Format & VectorFormat) != VectorFormat || matrix.Format != CooFormat)
				return false;
			if (target.Size.Length is not 0 and not 1)
				throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(target));
			if (matrix.Size.Length != 2)
				throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(matrix));
			if (!GetPointer(matrix, out T* pm, out TInd1* pr, out var pc, out var nnz))
				return false;
			if (matrix.ValueStorages.Length != 1 || matrix.IndexStorages.Length != 2)
				throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(matrix));
			if (target.IndexStorages.Length is not 0 and not 1 || target.ValueStorages.Length is not 0 and not 1)
				throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(matrix));

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
				var s = PureStorage<T, CpuMemoryPointer>.Create(stackalloc long[] { nnz });
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
				var s = PureStorage<TInd2, CpuMemoryPointer>.Create(stackalloc long[] { nnz });
				pIdx = (TInd2*)s.Pointer.Pointer.Pointer;
				idxOut = s as TSInd2 ?? TSInd2.Empty; // never empty
			}
			Buffer.MemoryCopy(pm, pVal, nnz * sizeof(T), nnz * sizeof(T));
			NMC.cooIdxsToSpVecIdx((MklInt*)pIdx, (MklInt*)pr, (MklInt*)pc, nnz, matrix.Size[0]);
			target.SetValues(matrix.Size.Prod(), valOut, idxOut);
			target.Format = VectorFormat;
			return true;
		}
		#endregion

		#region matrix conversion
		/// <inheritdoc/>
		public virtual bool MatrixSparseFormatConvert<T, TInd1, TInd2, TS1, TS2, TSInd1, TSInd2>(ISparseArray<T, TInd1, TS1, TSInd1> source, ref SparseArrayWrapper<T, TInd2, TS2, TSInd2> target) where T : unmanaged, IBaseNumber<T> where TInd1 : unmanaged, IBinaryInt<TInd1> where TS1 : class, IStorage<T, TS1> where TSInd1 : class, IStorage<TInd1, TSInd1> where TInd2 : unmanaged, IBinaryInt<TInd2> where TS2 : class, IStorage<T, TS2> where TSInd2 : class, IStorage<TInd2, TSInd2>
		{

		}

		bool IConversionAbstractApi.SparseMatrixGetSlice<T, TInd1, TInd2, TS1, TS2, TSInd1, TSInd2>(ISparseArray<T, TInd1, TS1, TSInd1> source, MatrixSliceWrapper slice, ref SparseArrayWrapper<T, TInd2, TS2, TSInd2> sub) => false;

		bool IConversionAbstractApi.SparseMatrixSetSlice<T, TInd1, TInd2, TS1, TS2, TSInd1, TSInd2>(ISparseArray<T, TInd2, TS2, TSInd2> matrix, MatrixSliceWrapper slice, ISparseArray<T, TInd1, TS1, TSInd1> sub) => false;

		bool IConversionAbstractApi.MatrixSparseToDense<T, TInd, TS1, TS2, TSInd>(ISparseArray<T, TInd, TS1, TSInd> source, TS2 destination, long ld) => false;

		bool IConversionAbstractApi.MatrixDenseToSparse<T, TInd, TS1, TS2, TSInd>(TS1 source, long ld, ref SparseArrayWrapper<T, TInd, TS2, TSInd> target, double threshold) => false;

		bool IConversionAbstractApi.MatrixSparsePrune<T, TInd1, TInd2, TS1, TS2, TSInd1, TSInd2>(ISparseArray<T, TInd1, TS1, TSInd1> source, double threshold, ref SparseArrayWrapper<T, TInd2, TS2, TSInd2> target) => false;

		bool IConversionAbstractApi.MatrixSparseReshape<T, TInd1, TInd2, TS1, TS2, TSInd1, TSInd2>(ISparseArray<T, TInd1, TS1, TSInd1> source, ref SparseArrayWrapper<T, TInd2, TS2, TSInd2> target) => false;
		#endregion
	}
}
