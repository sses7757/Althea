using System.Diagnostics;
using System.Runtime.CompilerServices;

using Althea.Array;
using Althea.Backend.Cuda.LinearAlgebra.Sparse;


namespace Althea.Backend.Cuda
{
	public static partial class StatusExtension
	{
		/// <summary>
		/// Check whether the input <see cref="CudaSparseStatus"/> is success or not and throw exception if it is not
		/// </summary>
		/// <param name="err">The <see cref="CudaSparseStatus"/> to be checked</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Check(this CudaSparseStatus err)
		{
			if (err == CudaSparseStatus.NotSupported)
				return false;
			if (err != CudaSparseStatus.Success)
				throw new StatusException(err, new StackTrace(0));
			return true;
		}
	}
}

namespace Althea.Backend.Cuda.LinearAlgebra.Sparse
{
	internal static class Conversions
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool CheckBaseSupport<T>(this T value) where T : unmanaged, IBaseNumber<T>
		{
			return value switch
			{
				Float32 or Float64 or
				Complex<Float32> or Complex<Float64> => true,
				_ => false,
			};
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool CheckExSupport<T>(this T value) where T : unmanaged, IBaseNumber<T>
		{
			return value switch
			{
				Float32 or Float64 or Float16 or
				Complex<Float32> or Complex<Float64> or Complex<Float16> => true,
				_ => false,
			};
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool CheckEx2Support<T>(this T value) where T : unmanaged, IBaseNumber<T>
		{
			return value switch
			{
				Float32 or Float64 or Float16 or BrainHalf or
				Complex<Float32> or Complex<Float64> or Complex<Float16> or Complex<BrainHalf> => true,
				_ => false,
			};
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static CudaDataType ToComputeType(this CudaDataType type)
		{
			return type switch
			{
				CudaDataType.RealFloat16 or CudaDataType.RealBrainFloat16 => CudaDataType.RealFloat32,
				CudaDataType.ComplexFloat16 or CudaDataType.ComplexBrainFloat16 => CudaDataType.ComplexFloat32,
				_ => type
			};
		}
	}

	/// <summary>
	/// The returned status (errors) of the cuSparse (CUDA Sparse) API calls
	/// </summary>
	public enum CudaSparseStatus
	{
		/// <summary>
		/// The operation completed successfully.
		/// </summary>
		Success = 0,
		/// <summary>
		/// The CUDA Sparse library was not initialized. This is usually caused by the lack of a prior  <see cref="NativeMethods.cusparseCreate(out IntPtr)"/> call, an error in the CUDA Runtime API called by the CUSPARSE routine, or an  error in the hardware setup. To correct: call <see cref="NativeMethods.cusparseCreate(out IntPtr)"/> prior to the function call; and check that the hardware, an appropriate version of the driver, and the CUSPARSE library are correctly installed.
		/// </summary>
		NotInitialized = 1,
		/// <summary>
		///  "Resource allocation failed inside the CUSPARSE library. This is usually caused by a <see cref="Storage.NativeMethods.cudaMalloc(out void*, long)"/> failure. To correct: prior to the function call, deallocate previously allocated memory as much as possible.
		/// </summary>
		AllocFailed = 2,
		/// <summary>
		/// An unsupported value or parameter was passed to the function (a negative vector size, for example). To correct: ensure that all the parameters being passed have valid values.
		/// </summary>
		InvalidValue = 3,
		/// <summary>
		/// The function requires a feature absent from the device architecture; usually caused by the lack of support for atomic operations or double precision. To correct: compile and run the application on a device with appropriate compute capability, which is 1.1 for 32-bit atomic operations and 1.3 for double precision.
		/// </summary>
		ArchMismatch = 4,
		/// <summary>
		/// An access to GPU memory space failed, which is usually caused by a failure to bind a texture. To correct: prior to the function call, unbind any previously bound textures.
		/// </summary>
		MappingError = 5,
		/// <summary>
		/// The GPU program failed to execute. This is often caused by a launch failure of the kernel on the GPU, which can be caused by multiple reasons. To correct: check that the hardware, an appropriate version of the driver, and the CUDA Sparse library are correctly installed.
		/// </summary>
		ExecutionFailed = 6,
		/// <summary>
		/// An internal CUDA Sparse operation failed. This error is usually caused by a <see cref="Storage.NativeMethods.cudaMalloc(out void*, long)"/> failure. To correct: check that the hardware, an appropriate version of the driver, and the CUDA Sparse library are correctly installed. Also, check that the memory passed as a parameter to the routine is not being deallocated prior to the routine’s completion.
		/// </summary>
		InternalError = 7,
		/// <summary>
		/// The matrix type is not supported by this function. This is usually caused by passing an invalid matrix descriptor to the function. To correct: check that the fields in cusparseMatDescr_t descrA were set correctly.
		/// </summary>
		MatrixTypeNotSupported = 8,
		/// <summary>
		/// The input pivot index array is zero.
		/// </summary>
		ZeroPivot = 9,
		/// <summary>
		/// The operation or data type combination is currently not supported by the function.
		/// </summary>
		NotSupported = 10,
		/// <summary>
		/// The resources for the computation, such as GPU global or shared memory, are not sufficient to complete the operation. The error can also indicate that the current computation mode (e.g. bit size of sparse matrix indices) does not allow to handle the given input.
		/// </summary>
		InsufficientResource = 11,
	}


#pragma warning disable CS1591
#pragma warning disable CS3001, CS3002

	public enum PointerMode
	{
		Host = 0,
		Device = 1
	}

	public enum SparseToDenseAlgorithm
	{
		Default = 0,
	}
	public enum DenseToSparseAlgorithm
	{
		Default = 0,
	}

	public enum SparseMVAlgorithm
	{
		Default = 0,
		CsrAlgorithm1 = 2,
		CsrAlgorithm2 = 3,
		CooAlgorithm1 = 1,
		CooAlgorithm2 = 4
	}

	public enum SparseMMAlgorithm
	{
		Default = 0,
		CooAlgorithm1 = 1,
		CooAlgorithm2 = 2,
		CooAlgorithm3 = 3,
		CooAlgorithm4 = 5,
		CsrAlgorithm1 = 4,
		CsrAlgorithm2 = 6,
		CsrAlgorithm3 = 12,
		BlockEllAlgorithm1 = 13
	}

	public enum SparseGemmAlgorithm {
		Default = 0,
		CsrDeterministic = 1,
		CsrNonDeterministic = 2
	}

	public enum Csr2CscAlgorithm
	{
		Algorithm1 = 1, // faster than V2 (in general), deterministic
		Algorithm2 = 2  // low memory requirement, non-deterministic
	}

	public enum MatrixType
	{
		General = 0,
		Symmetric = 1,
		Hermitian = 2,
		Triangular = 3
	}

	public enum SparseAction
	{
		OnlyIndices = 0,
		ValuesAndIndices = 1,
	}

	public enum IndexBase
	{
		Zero = 0,
		One = 1
	}

	public enum IndexType
	{
		UnsignedInt16 = 1,
		Integer32 = 2,
		Integer64 = 3
	}

	public enum MatrixFormat
	{
		CSR = 1,
		CSC = 2,
		Coo = 3,
		CooAoS = 4,
		BlockedEll = 5
	}

	public enum DenseMatrixOrder
	{
		Column = 1,
		Row = 2
	}

	public enum SparseMatrixOrder
	{
		Row = 0,
		Column = 1
	}

	public readonly unsafe ref struct DenseVectorWrapper
	{
		private readonly IntPtr handle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private DenseVectorWrapper(IntPtr handle) => this.handle = handle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Dispose()
		{
			if (this.handle == default)
				return;
			NativeMethods.cusparseDestroyDnVec(this.handle).Check();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static DenseVectorWrapper Create<T>(T* values, long size, out bool success) where T : unmanaged, IBaseNumber<T>
		{
			success = NativeMethods.cusparseCreateDnVec(out IntPtr descr, size, values, T.Type.ToCudaDataType()).Check();
			return success ? new(descr) : default;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static DenseVectorWrapper Create<T, TS>(IBindedDevice api, TS array, out bool success) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
		{
			success = false;
			if (!MemoryPointerChecker.GetPointer(api, array,  out T* p, out var n))
				return default;
			return Create(p, n, out success);
		}
	}

	public readonly unsafe ref struct DenseMatrixWrapper
	{
		private readonly IntPtr handle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private DenseMatrixWrapper(IntPtr handle) => this.handle = handle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Dispose()
		{
			if (this.handle == default)
				return;
			NativeMethods.cusparseDestroyDnMat(this.handle).Check();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static DenseMatrixWrapper Create<T>(T* values, long rows, long cols, long ld, bool transpose, out bool success) where T : unmanaged, IBaseNumber<T>
		{
			DenseMatrixOrder order = DenseMatrixOrder.Column;
			if (transpose)
			{
				order = DenseMatrixOrder.Row;
				(rows, cols) = (cols, rows);
			}
			success = NativeMethods.cusparseCreateDnMat(out IntPtr descr, rows, cols, ld, values, T.Type.ToCudaDataType(), order).Check();
			return success ? new(descr) : default;
		}
	}

	public readonly unsafe ref struct SparseVectorWrapper
	{
		private readonly IntPtr handle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private SparseVectorWrapper(IntPtr handle) => this.handle = handle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Dispose()
		{
			if (this.handle == default)
				return;
			NativeMethods.cusparseDestroySpVec(this.handle).Check();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static SparseVectorWrapper Create<T, TInd>(T* values, TInd* indices, long size, long nnz, out bool success) where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBaseNumber<TInd>
		{
			var idxType = sizeof(TInd) == sizeof(int) ? IndexType.Integer32 : IndexType.Integer64;
			success = NativeMethods.cusparseCreateSpVec(out IntPtr descr, size, nnz, indices, values, idxType, IndexBase.Zero, T.Type.ToCudaDataType()).Check();
			return success ? new(descr) : default;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static SparseVectorWrapper Create<T, TInd, TS, TSInd>(IBindedDevice api, ISparseArray<T, TInd, TS, TSInd> vector, out bool success) where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd> where TS : class, IStorage<T, TS> where TSInd : class, IStorage<TInd, TSInd>
		{
			if (!MemoryPointerChecker.GetPointer(api, vector, out T* vals, out TInd* inds, out long nnz))
			{
				success = false;
				return default;
			}
			return Create(vals, inds, vector.Size[0], nnz, out success);
		}
	}

	public readonly unsafe ref struct BaseMatrixWrapper
	{
		private readonly IntPtr handle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Dispose()
		{
			if (this.handle == default)
				return;
			NativeMethods.cusparseDestroyMatDescr(this.handle).Check();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public BaseMatrixWrapper()
		{
			NativeMethods.cusparseCreateMatDescr(out this.handle).Check();
		}
	}

	public readonly unsafe ref struct SparseMatrixWrapper
	{
		private readonly IntPtr handle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private SparseMatrixWrapper(IntPtr handle) => this.handle = handle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Dispose()
		{
			if (this.handle == default)
				return;
			NativeMethods.cusparseDestroySpMat(this.handle).Check();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static SparseMatrixWrapper Create<T, TInd>(SparseFormat format, long rows, long cols, long nnz, T* vals, TInd* rowInd, TInd* colInd, out bool success) where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd>
		{
			IntPtr descr;
			var idxType = sizeof(TInd) == sizeof(int) ? IndexType.Integer32 : IndexType.Integer64;
			if (format.Class == SparseFormat.Type.Coordinated)
				success = NativeMethods.cusparseCreateCoo(out descr, rows, cols, nnz, rowInd, colInd, vals, idxType, IndexBase.Zero, T.Type.ToCudaDataType()).Check();
			else if (format.MajorType == SparseFormat.Major.Row)
				success = NativeMethods.cusparseCreateCsr(out descr, rows, cols, nnz, rowInd, colInd, vals, idxType, idxType, IndexBase.Zero, T.Type.ToCudaDataType()).Check();
			else
				success = NativeMethods.cusparseCreateCsc(out descr, rows, cols, nnz, colInd, rowInd, vals, idxType, idxType, IndexBase.Zero, T.Type.ToCudaDataType()).Check();
			return success ? new(descr) : default;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static SparseMatrixWrapper Create<T, TInd, TS, TSInd>(IBindedDevice api, ISparseArray<T, TInd, TS, TSInd> matrix, out bool success, out long nnz, out T* vals, out TInd* rowInd, out TInd* colInd) where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd> where TS : class, IStorage<T, TS> where TSInd : class, IStorage<TInd, TSInd>
		{
			if (!MemoryPointerChecker.GetPointer(api, matrix, out vals, out rowInd, out colInd, out nnz))
			{
				success = false;
				return default;
			}
			return Create(matrix.Format, matrix.Size[0], matrix.Size[1], nnz, vals, rowInd, colInd, out success);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static SparseMatrixWrapper Create<T, TInd, TS, TSInd>(IBindedDevice api, ISparseArray<T, TInd, TS, TSInd> matrix, out bool success) where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd> where TS : class, IStorage<T, TS> where TSInd : class, IStorage<TInd, TSInd>
		{
			return Create(api, matrix, out success, out _, out _, out _, out _);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly bool GetSizes(out long rows, out long cols, out long nnz)
		{
			return NativeMethods.cusparseSpMatGetSize(this.handle, out rows, out cols, out nnz).Check();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly bool SetPointers(SparseFormat format, void* values, void* rows, void* cols)
		{
			if (format.Class == SparseFormat.Type.Coordinated)
				return NativeMethods.cusparseCooSetPointers(this.handle, rows, cols, values).Check();
			if (!format.IsCompressed)
				return false;
			if (format.IsRowMajor)
				return NativeMethods.cusparseCsrSetPointers(this.handle, rows, cols, values).Check();
			else
				return NativeMethods.cusparseCscSetPointers(this.handle, cols, rows, values).Check();
		}
	}

	public readonly unsafe ref struct SparseGemmDescriptor
	{
		private readonly IntPtr handle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Dispose()
		{
			if (this.handle == default)
				return;
			NativeMethods.cusparseSpGEMM_destroyDescr(this.handle).Check();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SparseGemmDescriptor()
		{
			NativeMethods.cusparseSpGEMM_createDescr(out this.handle).Check();
		}
	}

	/*
	// The following sparse/dense vector/matrix descriptors are guessed from the cuSparse 11.3 APIs.
	// Since the APIs always allocate them on unmanaged heap which is unnecessary in most cases and may cause performance loss,
	//   using a structure that can be allocated on stack is a better option.
	// However, they vary drastically from 11.1 to 11.3 and probably will still be. Therefore, the Cuda.LinearAlgebra.Sparse is currently not available.
	[StructLayout(LayoutKind.Sequential)]
	internal readonly struct DenseVectorWrapper
	{
		private readonly long cuSparseHandleInfo;

		public readonly long Length;

		public readonly IntPtr PtrValues;

		public readonly CudaDataType DataType;

		private readonly int __align;

		public DenseVectorWrapper(long cuSparseHandleInfo, long length, IntPtr values, CudaDataType dataType)
		{
			this.cuSparseHandleInfo = cuSparseHandleInfo; Length = length; PtrValues = values; DataType = dataType;
			__align = 0;
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	internal readonly struct DenseMatrixWrapper
	{
		private readonly long cuSparseHandleInfo;

		public readonly long Rows;

		public readonly long Cols;

		public readonly CudaDataType DataType;

		private readonly int __align;

		private readonly long preserve0;

		public readonly long LeadDim;

		public readonly IntPtr PtrValues;

		private readonly int preserve1;

		public readonly DenseMatrixOrder MemoryOrder;

		public DenseMatrixWrapper(long cuSparseHandleInfo, long rows, long cols, long ld, IntPtr values, CudaDataType dataType, DenseMatrixOrder order)
		{
			this.cuSparseHandleInfo = cuSparseHandleInfo; Rows = rows; Cols = cols; LeadDim = ld;
			preserve1 = 1; __align = 0; preserve0 = 0;
			PtrValues = values; DataType = dataType; MemoryOrder = order;
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	internal readonly struct SparseVectorWrapper
	{
		private readonly long cuSparseHandleInfo;

		public readonly long Length;

		public readonly long NonZeros;

		public readonly IntPtr PtrIndices;

		public readonly IntPtr PtrValues;

		public readonly CudaDataType DataType;

		public readonly IndexType IndexType;

		public readonly IndexBase IndexBase;

		private readonly int __align;

		public SparseVectorWrapper(long cuSparseHandleInfo, long length, IntPtr values, CudaDataType dataType, long nnz, IntPtr indices, IndexType indexType, IndexBase indexBase)
		{
			this.cuSparseHandleInfo = cuSparseHandleInfo; Length = length; PtrValues = values; DataType = dataType;
			NonZeros = nnz; PtrIndices = indices; IndexType = indexType; IndexBase = indexBase;
			__align = 0;
		}
	}
	*/
}
