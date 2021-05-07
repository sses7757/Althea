using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Althea.Backend.Cuda.LinearAlgebra.Dense;
using Althea.Backend.Cuda.LinearAlgebra.Sparse;
using Althea.Helpers;
using Althea.LinearAlgebra;
using Althea.LinearAlgebra.Sparse;
using Althea.NativeTypes;

namespace Althea.Backend.Cuda
{
	/// <summary>
	/// The static class containing extension methods for <see cref="CudaBlasStatus"/> and <see cref="CudaSolverStatus"/>
	/// </summary>
	public static partial class StatusExtension
	{
		/// <summary>
		/// Check whether the input <see cref="CudaSparseStatus"/> is success or not and throw exception if it is not
		/// </summary>
		/// <param name="err">The <see cref="CudaSparseStatus"/> to be checked</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Check(this CudaSparseStatus err)
		{
			if (err != CudaSparseStatus.Success)
			{
				throw new StatusException(err, new StackTrace(0));
			}
		}

		// TODO: sparse API
		////[MethodImpl(MethodImplOptions.AggressiveInlining)]
		////internal static DenseVectorWrapper ToWrapper<T>(this Arrays.DenseVector<T> vector, SparseApi api) where T : unmanaged
		////{
		////	api.GetPointer(vector.Storage, out IntPtr p, out long length);
		////	return new(api.InternalInfo, length, p, Const<T>.DataType.ToCudaDataType());
		////}
	}
}

namespace Althea.Backend.Cuda.LinearAlgebra.Sparse
{
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
		///  "Resource allocation failed inside the CUSPARSE library. This is usually caused by a <see cref="Storage.NativeMethods.cudaMalloc(out IntPtr, long)"/> failure. To correct: prior to the function call, deallocate previously allocated memory as much as possible.
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
		/// An internal CUDA Sparse operation failed. This error is usually caused by a <see cref="Storage.NativeMethods.cudaMalloc(out IntPtr, long)"/> failure. To correct: check that the hardware, an appropriate version of the driver, and the CUDA Sparse library are correctly installed. Also, check that the memory passed as a parameter to the routine is not being deallocated prior to the routine’s completion.
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

	/// <summary>
	/// This type indicates the type of matrix stored in sparse storage. Notice that for symmetric, Hermitian and triangular matrices only their lower or upper part is assumed to be stored.
	/// </summary>
	internal enum MatrixType
	{
		/// <summary>
		/// the matrix is general.
		/// </summary>
		General = 0,
		/// <summary>
		/// the matrix is symmetric.
		/// </summary>
		Symmetric = 1,
		/// <summary>
		/// the matrix is Hermitian.
		/// </summary>
		Hermitian = 2,
		/// <summary>
		/// the matrix is triangular.
		/// </summary>
		Triangular = 3
	}

	/// <summary>
	/// This enum indicates if the base of the matrix indices is zero or one.
	/// </summary>
	internal enum IndexBase
	{
		/// <summary>
		/// the base index is zero.
		/// </summary>
		Zero = 0,
		/// <summary>
		/// the base index is one.
		/// </summary>
		One = 1
	}

	/// <summary>
	/// This enum indicates the index type for representing the sparse matrix indices.
	/// </summary>
	internal enum IndexType
	{
		/// <summary>
		/// 16-bit unsigned integer [0, 65535]
		/// </summary>
		UnsignedInt16 = 1,
		/// <summary>
		/// 32-bit signed integer [0, 2^31 - 1]
		/// </summary>
		Integer32 = 2,
		/// <summary>
		/// 64-bit signed integer [0, 2^63 - 1]
		/// </summary>
		Integer64 = 3
	}

	/// <summary>
	/// This enum indicates the format of the sparse matrix.
	/// </summary>
	internal enum CudaSparseMatrixFormat
	{
		/// <summary>
		/// The matrix is stored in Compressed Sparse Row (CSR) format
		/// </summary>
		CompressedRowMajor = 1,
		/// <summary>
		/// The matrix is stored in Compressed Sparse Column (CSC) format
		/// </summary>
		CompressedColumnMajor = 2,
		/// <summary>
		/// The matrix is stored in Coordinate (COO) format organized in Structure of Arrays (SoA) layout
		/// </summary>
		Coordinate = 3,
		/// <summary>
		/// The matrix is stored in Coordinate (COO) format organized in Arrays of Structures (AoS) layout
		/// </summary>
		CoordinateAoS = 4,
	}
	
	/// <summary>
	/// This enum indicates the memory layout of a dense matrix. Currently, only column-major layout is supported.
	/// </summary>
	public enum DenseMatrixOrder
	{
		/// <summary>
		/// The matrix is stored in column-major
		/// </summary>
		Column = 1,
		/// <summary>
		/// The matrix is stored in row-major
		/// </summary>
		Row = 2
	}


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
			this.cuSparseHandleInfo = cuSparseHandleInfo; this.Length = length; this.PtrValues = values; this.DataType = dataType;
			this.__align = 0;
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
			this.cuSparseHandleInfo = cuSparseHandleInfo; this.Rows = rows; this.Cols = cols; this.LeadDim = ld;
			this.preserve1 = 1; this.__align = 0; this.preserve0 = 0;
			this.PtrValues = values; this.DataType = dataType; this.MemoryOrder = order;
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
			this.cuSparseHandleInfo = cuSparseHandleInfo; this.Length = length; this.PtrValues = values; this.DataType = dataType;
			this.NonZeros = nnz; this.PtrIndices = indices; this.IndexType = indexType; this.IndexBase = indexBase;
			this.__align = 0;
		}
	}

}
