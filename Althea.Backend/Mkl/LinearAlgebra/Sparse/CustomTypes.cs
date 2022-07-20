using System.Runtime.CompilerServices;

using Althea.Array;
using Althea.Backend.Mkl.LinearAlgebra.Sparse;


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
		private readonly bool transposed;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator IntPtr(SparseMatrixHandle handle) => handle.handle;

		private static readonly SparseFormat CooFormat = SparseFormat.MatrixCocFormat | SparseFormat.MatrixCorFormat;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SparseMatrixHandle(IntPtr handle, SparseFormat format, bool transposed = false)
		{
			this.handle = handle; this.format = format; this.transposed = transposed;
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
		public static unsafe SparseMatrixHandle TryCreate<T, TInd, TS, TSInd>(ISparseArray<T, TInd, TS, TSInd> source, out bool success)
			where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd>
			where TS : class, IStorage<T, TS> where TSInd : class, IStorage<TInd, TSInd>
		{
			success = false;
			if (!Api.GetPointer(source, out T* pv, out TInd* pr, out var pc, out var nnz))
				return default;
			IntPtr handle;
			if ((source.Format & CooFormat) != SparseFormat.None)
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
			else if ((source.Format & (SparseFormat.MatrixCscFormat | SparseFormat.MatrixCsrFormat)) != SparseFormat.None)
			{
				delegate*<out IntPtr, int, MklInt, MklInt, MklInt*, MklInt*, MklInt*, T*, MklSparseBlasError> createFunc;
				MklInt* pStarts, pInds;
				if (source.Format == SparseFormat.MatrixCsrFormat)
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
				createFunc(out handle, 0, MatrixMajor.Row, source.Size[0], source.Size[1], source.BlockSize[0].AsInt64(), (MklInt*)pr, (MklInt*)pr + 1, (MklInt*)pc, pv).Check();
			}
			success = true;
			return new(handle, source.Format);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly unsafe void Deconstruct<T, TInd, TS, TSInd>(ref SparseArrayWrapper<T, TInd, TS, TSInd> target)
			where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd>
			where TS : class, IStorage<T, TS> where TSInd : class, IStorage<TInd, TSInd>
		{

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
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Check(this MklSparseBlasError error)
		{
			if (error == MklSparseBlasError.Success)
				return;
			throw new StatusException(error);
		}
	}
}