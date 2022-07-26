using System.Runtime.CompilerServices;

using Althea.Array;
using Althea.Backend.Mkl.LinearAlgebra.Sparse;
using Althea.Backend.Storage;
using Althea.LinearAlgebra;

using static Althea.Backend.Mkl.MemoryPointerChecker;


namespace Althea.Backend.Mkl.LinearAlgebra.Sparse
{
	#region enum
	/// <summary>
	/// The error enum for MKL sparse BLAS library APIs
	/// </summary>
	public enum MklSparseBlasError
	{
		/// <summary>
		/// The operation was successful
		/// </summary>
		Success = 0,
		/// <summary>
		/// Empty handle or matrix arrays
		/// </summary>
		NotInitialized = 1,
		/// <summary>
		/// Internal error: memory allocation failed
		/// </summary>
		AllocFailed = 2,
		/// <summary>
		/// Invalid input value
		/// </summary>
		InvalidValue = 3,
		/// <summary>
		/// Execution failed due to e.g. 0-diagonal element for triangular solver, etc.
		/// </summary>
		ExecutionFailed = 4,
		/// <summary>
		/// Other internal error
		/// </summary>
		InternalError = 5,
		/// <summary>
		/// The operation is not supported yet, e.g. operation for double precision doesn't support other types
		/// </summary>
		NotSupported = 6,
	}

	internal enum MatrixOp
	{
		None = 10,
		Trans = 11,
		ConjTrans = 12
	}

	internal static class CustomEnumExtensions
	{
		public static MatrixOp ToOp(this MatrixOperation op) => op switch
		{
			MatrixOperation.None => MatrixOp.None,
			MatrixOperation.Transpose => MatrixOp.Trans,
			MatrixOperation.ConjugateTranspose => MatrixOp.ConjTrans,
			_ => MatrixOp.None
		};
	}

	internal enum MatrixType
	{
		General = 20,
		Symmetric = 21,
		Hermitian = 22,
		Triangular = 23,
		Diagonal = 24,
		BlockTriangular = 25,
		BlockDiagonal = 26
	}

	internal enum MatrixFillMode
	{
		Lower = 40,
		Upper = 41,
		Full = 42
	}

	internal enum MatrixDiagType
	{
		NonUnit = 50,           /* triangular matrix with non-unit diagonal */
		Unit = 51            /* triangular matrix with unit diagonal */
	}

	internal enum MatrixMajor
	{
		Row = 101,
		Column = 102
	}

	internal enum MemoryUsage
	{
		None = 80,       /* no memory should be allocated for matrix values and structures; auxiliary structures could be created only for workload balancing, parallelization, etc. */
		Aggresive = 81        /* matrix could be converted to any internal format */
	}

	internal enum Request
	{
		FullMultiply = 90,
		CountNonzeros = 91,
		FinalizeMultiply = 92,
		FullMultiplyNoValues = 93,
		FinalizeMultiplyNoValues = 94
	}

	internal readonly record struct MatrixDescr(MatrixType Type, MatrixFillMode Mode, MatrixDiagType Diag);
	#endregion

	#region sparse matrix MKL handle
	internal readonly ref struct SparseMatrixHandle
	{
		private readonly IntPtr handle;
		private readonly SparseFormat format;
		private readonly MatrixOperation op;

		public readonly bool IsEmpty => this.handle == default;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator IntPtr(SparseMatrixHandle handle) => handle.handle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SparseMatrixHandle(IntPtr handle, SparseFormat format, MatrixOperation op = MatrixOperation.None)
		{
			this.handle = handle; this.format = format; this.op = op;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Dispose()
		{
			if (this.handle == default)
				return;
			var err = NativeMethods.mkl_sparse_destroy(this.handle);
			if (err != MklSparseBlasError.Success)
				Helpers.Log.Write($"Error in disposing MKL sparse matrix handle: {err}", level: Helpers.LogLevel.Error);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe SparseMatrixHandle TryCreate<T, TInd, TS, TSInd>(ISparseArray<T, TInd, TS, TSInd>? source, out bool success)
			where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd>
			where TS : class, IStorage<T, TS> where TSInd : class, IStorage<TInd, TSInd>
		{
			success = false;
			if (source is null)
				return default;
			if (!source.BlockSize.IsEmpty && source.BlockSize[0] != source.BlockSize[1])
				return default; // not supported by MKL
			if (!GetPointer(source, out T* pv, out var pr, out var pc, out var nnz))
				return default;
			SparseFormat format = source.Format;
			IntPtr handle;
			if ((format & SparseFormat.MatrixCooFormat) != SparseFormat.None)
			{
				delegate*<out IntPtr, int, MklInt, MklInt, MklInt, MklInt*, MklInt*, T*, MklSparseBlasError> createFunc = default(T) switch
				{
					Float32 => &NativeMethods.mkl_sparse_s_create_coo,
					Float64 => &NativeMethods.mkl_sparse_d_create_coo,
					Complex<Float32> => &NativeMethods.mkl_sparse_c_create_coo,
					Complex<Float64> => &NativeMethods.mkl_sparse_z_create_coo,
					_ => null
				};
				if (createFunc is null)
					return default;
				createFunc(out handle, 0, source.Size[0], source.Size[1], nnz, (MklInt*)pr, (MklInt*)pc, pv).Check();
			}
			else if ((format & (SparseFormat.MatrixCscFormat | SparseFormat.MatrixCsrFormat)) != SparseFormat.None)
			{
				delegate*<out IntPtr, int, MklInt, MklInt, MklInt*, MklInt*, MklInt*, T*, MklSparseBlasError> createFunc;
				MklInt* pStarts, pInds;
				if (format == SparseFormat.MatrixCsrFormat)
				{
					createFunc = default(T) switch
					{
						Float32 => &NativeMethods.mkl_sparse_s_create_csr,
						Float64 => &NativeMethods.mkl_sparse_d_create_csr,
						Complex<Float32> => &NativeMethods.mkl_sparse_c_create_csr,
						Complex<Float64> => &NativeMethods.mkl_sparse_z_create_csr,
						_ => null
					};
					pStarts = (MklInt*)pr; pInds = (MklInt*)pc;
				}
				else
				{
					createFunc = default(T) switch
					{
						Float32 => &NativeMethods.mkl_sparse_s_create_csc,
						Float64 => &NativeMethods.mkl_sparse_d_create_csc,
						Complex<Float32> => &NativeMethods.mkl_sparse_c_create_csc,
						Complex<Float64> => &NativeMethods.mkl_sparse_z_create_csc,
						_ => null
					};
					pStarts = (MklInt*)pc; pInds = (MklInt*)pr;
				}
				if (createFunc is null)
					return default;
				createFunc(out handle, 0, source.Size[0], source.Size[1], pStarts, pStarts + 1, pInds, pv).Check();
			}
			else // BSR
			{
				delegate*<out IntPtr, int, MatrixMajor, MklInt, MklInt, MklInt, MklInt*, MklInt*, MklInt*, T*, MklSparseBlasError> createFunc = default(T) switch
				{
					Float32 => &NativeMethods.mkl_sparse_s_create_bsr,
					Float64 => &NativeMethods.mkl_sparse_d_create_bsr,
					Complex<Float32> => &NativeMethods.mkl_sparse_c_create_bsr,
					Complex<Float64> => &NativeMethods.mkl_sparse_z_create_bsr,
					_ => null
				};
				if (createFunc is null)
					return default;
				createFunc(out handle, 0, MatrixMajor.Row, source.Size[0], source.Size[1], source.BlockSize[0], (MklInt*)pr, (MklInt*)pr + 1, (MklInt*)pc, pv).Check();
			}
			success = true;
			return new(handle, format);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly unsafe MklSparseBlasError Deconstruct<T, TInd, TS, TSInd>(ref SparseArrayWrapper<T, TInd, TS, TSInd> target)
			where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd>
			where TS : class, IStorage<T, TS> where TSInd : class, IStorage<TInd, TSInd>
		{
			bool transposed = !this.op.CanInPlace(), conjugate = this.op.HasConjugate();
			T* pVals; long nnz;
			if ((this.format & (SparseFormat.MatrixCscFormat | SparseFormat.MatrixCsrFormat)) != SparseFormat.None)
			{
				delegate*<IntPtr, out int, out MklInt, out MklInt, out MklInt*, out MklInt*, out MklInt*, out void*, MklSparseBlasError> createFunc;
				MklInt rows, cols; MklInt* pStarts, pInds; void* pv;
				if ((format & SparseFormat.MatrixCsrFormat) != SparseFormat.None)
				{
					createFunc = default(T) switch
					{
						Float32 => &NativeMethods.mkl_sparse_s_export_csr,
						Float64 => &NativeMethods.mkl_sparse_d_export_csr,
						Complex<Float32> => &NativeMethods.mkl_sparse_c_export_csr,
						Complex<Float64> => &NativeMethods.mkl_sparse_z_export_csr,
						_ => null
					};
					if (createFunc is null)
						goto CSC;
					if (createFunc(this.handle, out _, out rows, out cols, out pStarts, out _, out pInds, out pv) != MklSparseBlasError.Success)
						goto CSC;
					goto CS_END;
				}
				CSC:
				{
					createFunc = default(T) switch
					{
						Float32 => &NativeMethods.mkl_sparse_s_export_csc,
						Float64 => &NativeMethods.mkl_sparse_d_export_csc,
						Complex<Float32> => &NativeMethods.mkl_sparse_c_export_csc,
						Complex<Float64> => &NativeMethods.mkl_sparse_z_export_csc,
						_ => null
					};
					if (createFunc is null)
						goto BSR;
					if (createFunc(this.handle, out _, out rows, out cols, out pStarts, out _, out pInds, out pv) != MklSparseBlasError.Success)
						goto BSR;
				}
				CS_END:
				if ((format == SparseFormat.MatrixCsrFormat) != transposed)
				{
					var temp = pStarts; pStarts = pInds; pInds = temp;
				}
				if (transposed)
					(rows, cols) = (cols, rows);
				nnz = pStarts[rows] - pStarts[0];
				long rowSize, colSize;
				if ((format == SparseFormat.MatrixCsrFormat) == transposed)
				{
					rowSize = rows; colSize = nnz;
				}
				else
				{
					rowSize = nnz; colSize = rows;
				}
				TS vals = new Backend.Storage.ActualPureStorage<T, CpuMemoryPointer>(new CpuMemoryPointer((IntPtr)pv, nnz * sizeof(T))) as TS ?? TS.Empty;
				TSInd rowInds = new Backend.Storage.ActualPureStorage<TInd, CpuMemoryPointer>(new CpuMemoryPointer((IntPtr)pStarts, rowSize * sizeof(TInd))) as TSInd ?? TSInd.Empty;
				TSInd colInds = new Backend.Storage.ActualPureStorage<TInd, CpuMemoryPointer>(new CpuMemoryPointer((IntPtr)pInds, colSize * sizeof(TInd))) as TSInd ?? TSInd.Empty;
				target.SetValues(rows, cols, vals, rowInds, colInds);
				target.Format = transposed ? format : format.WithTransposedMajor;
				pVals = (T*)pv;
				goto END;
			}
			BSR:
			{
				delegate*<IntPtr, out int, out MatrixMajor, out MklInt, out MklInt, out MklInt, out MklInt*, out MklInt*, out MklInt*, out void*, MklSparseBlasError> createFunc = default(T) switch
				{
					Float32 => &NativeMethods.mkl_sparse_s_export_bsr,
					Float64 => &NativeMethods.mkl_sparse_d_export_bsr,
					Complex<Float32> => &NativeMethods.mkl_sparse_c_export_bsr,
					Complex<Float64> => &NativeMethods.mkl_sparse_z_export_bsr,
					_ => null
				};
				if (createFunc is null)
					return MklSparseBlasError.NotSupported;
				var err = createFunc(handle, out _, out _, out var rows, out var cols, out var bs, out var pStarts, out _, out var pInds, out var pv);
				if (err != MklSparseBlasError.Success)
					return err;
				long blockSize = bs;
				nnz = (pStarts[rows] - pStarts[0]);
				long rowSize = rows / blockSize, colSize = nnz;
				nnz *= blockSize * blockSize;
				if (transposed)
				{
					(rows, cols) = (cols, rows);
					(rowSize, colSize) = (colSize, rowSize);
					var temp = pStarts; pStarts = pInds; pInds = temp;
				}
				TS vals = new Backend.Storage.ActualPureStorage<T, CpuMemoryPointer>(new CpuMemoryPointer((IntPtr)pv, nnz * sizeof(T))) as TS ?? TS.Empty;
				TSInd rowInds = new Backend.Storage.ActualPureStorage<TInd, CpuMemoryPointer>(new CpuMemoryPointer((IntPtr)pStarts, rowSize * sizeof(TInd))) as TSInd ?? TSInd.Empty;
				TSInd colInds = new Backend.Storage.ActualPureStorage<TInd, CpuMemoryPointer>(new CpuMemoryPointer((IntPtr)pInds, colSize * sizeof(TInd))) as TSInd ?? TSInd.Empty;
				target.SetValues(rows, cols, blockSize, blockSize, vals, rowInds, colInds);
				target.Format = transposed ? format : format.WithTransposedMajor;
				pVals = (T*)pv;
			}
			END:
			if (conjugate)
				Dense.Conjugater.Conjugate(pVals, nnz, 1);
			target.DefaultValue = T.Zero;
			return MklSparseBlasError.Success;
		}
	}
	#endregion
}

namespace Althea.Backend.Mkl
{
	public static partial class StatusExtension
	{
		/// <summary>
		/// Check the given <see cref="MklSparseBlasError"/>
		/// </summary>
		/// <returns>Whether <paramref name="error"/> is <see cref="MklSparseBlasError.NotSupported"/> or not</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Check(this MklSparseBlasError error)
		{
			if (error == MklSparseBlasError.Success)
				return false;
			if (error == MklSparseBlasError.NotSupported)
				return true;
			throw new StatusException(error);
		}
	}
}