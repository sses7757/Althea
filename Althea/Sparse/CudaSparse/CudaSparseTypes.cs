using System;
using System.Runtime.InteropServices;

using Althea.Arrays;
using Althea.Memory;


namespace Althea.SparseBlas.Cuda
{
	#region enum
	/// <summary>
	/// This is a CUDA Sparse status type returned by the library functions and it can have the following values.
	/// </summary>
	public enum Status
	{
		/// <summary>
		/// The operation completed successfully.
		/// </summary>
		Success = 0,
		/// <summary>
		/// The CUDA Sparse library was not initialized. This is usually caused by the lack of a prior  <see cref="NativeMethods.cusparseCreate(ref IntPtr)"/> call, an error in the CUDA Runtime API called by the CUSPARSE routine, or an  error in the hardware setup. To correct: call <see cref="NativeMethods.cusparseCreate(ref IntPtr)"/> prior to the function call; and check that the hardware, an appropriate version of the driver, and the CUSPARSE library are correctly installed.
		/// </summary>
		NotInitialized = 1,
		/// <summary>
		///  "Resource allocation failed inside the CUSPARSE library. This is usually caused by a <see cref="Runtime.Cuda.NativeMethods.cudaMalloc(ref IntPtr, long)"/> failure. To correct: prior to the function call, deallocate previously allocated memory as much as possible.
		/// </summary>
		AllocFailed = 2,
		/// <summary>
		/// "An unsupported value or parameter was passed to the function (a negative vector size, for example). To correct: ensure that all the parameters being passed have valid values.
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
		/// An internal CUDA Sparse operation failed. This error is usually caused by a <see cref="Runtime.Cuda.NativeMethods.cudaMalloc"/> failure. To correct: check that the hardware, an appropriate version of the driver, and the CUDA Sparse library are correctly installed. Also, check that the memory passed as a parameter to the routine is not being deallocated prior to the routine’s completion.
		/// </summary>
		InternalError = 7,
		/// <summary>
		/// The matrix type is not supported by this function. This is usually caused by passing an invalid matrix descriptor to the function. To correct: check that the fields in cusparseMatDescr_t descrA were set correctly.
		/// </summary>
		MatrixTypeNotSupported = 8,
		/// <summary>
		///
		/// </summary>
		ZeroPivot = 9
	}

	/// <summary>
	/// This type indicates the type of matrix stored in sparse storage. Notice that for symmetric, Hermitian and triangular matrices only their lower or upper part is assumed to be stored.
	/// </summary>
	public enum MatrixType
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
	/// This type indicates if the base of the matrix indices is zero or one.
	/// </summary>
	public enum IndexBase
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
	/// This type indicates the index type for representing the sparse matrix indices.
	/// </summary>
	public enum IndexType
	{
		/// <summary>
		/// 16-bit unsigned integer [1, 65535]
		/// </summary>
		UnsignedInt16 = 1,
		/// <summary>
		/// 32-bit signed integer [1, 2^31 - 1]
		/// </summary>
		Integer32 = 2,
		/// <summary>
		/// 64-bit signed integer [1, 2^63 - 1]
		/// </summary>
		Integer64 = 3
	}

	/// <summary>
	/// This type indicates whether the operation is performed only on indices or on data and indices.
	/// </summary>
	public enum Action
	{
		/// <summary>
		/// the operation is performed only on indices.
		/// </summary>
		Symbolic = 0,
		/// <summary>
		/// the operation is performed on data and indices.
		/// </summary>
		Numeric = 1
	}

	/// <summary>
	/// This type indicates whether the elements of a dense matrix should be parsed by rows or by columns (assuming column-major storage in memory of the dense matrix) in function <c>cusparse[S|D|C|Z]nnz</c>. Besides storage format of blocks in BSR format is also controlled by this type.
	/// </summary>
	public enum Direction
	{
		/// <summary>
		/// the matrix should be parsed by rows
		/// </summary>
		Row = 0,
		/// <summary>
		/// the matrix should be parsed by columns
		/// </summary>
		Column = 1
	}

	/// <summary>
	/// The store order of dense matrix, now only <see cref="ColumnMajor"/> is supported
	/// </summary>
	public enum Order
	{
		/// <summary>
		/// Column major storage
		/// </summary>
		ColumnMajor = 1,
		/// <summary>
		/// Row major storage
		/// </summary>
		RowMajor = 2
	}

	/// <summary>
	/// Algorithm used to preform sparse matrix dense vector multiplication <see cref="NativeMethods.SpMV{T}"/>.
	/// </summary>
	public enum MatrixVectorAlgorithm
	{
		/// <summary>
		/// Default algorithm for any sparse matrix format
		/// </summary>
		Default = 0,
		/// <summary>
		/// Default algorithm for COO sparse matrix format
		/// </summary>
		COO_Default = 1,
		/// <summary>
		/// Default algorithm for CSR sparse matrix format
		/// </summary>
		CSR_1 = 2,
		/// <summary>
		/// Algorithm 2 for CSR sparse matrix format. May provide better performance for irregular matrices
		/// </summary>
		CSR_2 = 3
	}

	/// <summary>
	/// Algorithm used to preform sparse matrix dense matrix multiplication <see cref="NativeMethods.SpMM{T}"/>.
	/// </summary>
	public enum MatrixMatrixAlgorithm
	{
		/// <summary>
		/// Default algorithm for any sparse matrix format
		/// </summary>
		Default = 0,
		/// <summary>
		/// Default algorithm for COO sparse matrix format. It supports batched computation. May produce slightly different results during different runs with the same input parameters
		/// </summary>
		COO_1 = 1,
		/// <summary>
		/// Algorithm 2 for COO sparse matrix format. It supports batched computation. It provides deterministic result, and requires additional memory
		/// </summary>
		COO_2 = 2,
		/// <summary>
		/// Algorithm 3 for COO sparse matrix format. May provide better performance for large matrices. May produce slightly different results during different runs with the same input parameters
		/// </summary>
		COO_3 = 3,
		/// <summary>
		/// Default algorithm for CSR sparse matrix format
		/// </summary>
		CSR_1 = 4
	}

	/// <summary>
	/// Algorithm used to preform CSR matrix to CSC matrix transformation <see cref="NativeMethods.cusparseCsr2cscEx2"/>.
	/// </summary>
	public enum CSR2CSCAlgorithm
	{
		/// <summary>
		/// requires extra storage proportional to the number of nonzero values, faster than <see cref="Algorithm_2"/> (in general), deterministic
		/// </summary>
		Algorithm_1 = 1,
		/// <summary>
		/// requires extra storage proportional to the number of rows, non-deterministic
		/// </summary>
		Algorithm_2 = 2
	}
	#endregion

	/// <summary>
	/// The description struct of the sparse matrix
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct SparseMatrixDescription : IEquatable<SparseMatrixDescription>
	{
		/// <summary>
		/// Type of the matrix, see <see cref="MatrixType"/>
		/// </summary>
		private readonly MatrixType matrixType;

		/// <summary>
		/// Fill mode of the matrix if it is triangular, symmetric or Hermitian, see <see cref="MatrixFillMode"/>, not editable
		/// </summary>
		private readonly MatrixFillMode fillMode;

		/// <summary>
		/// Whether the diagonal elements of the matrix are assumed to be all unit, see <see cref="DiagType"/>
		/// </summary>
		private readonly DiagType diagType;

		/// <summary>
		/// The starting indices of the coordinates, see <see cref="IndexBase"/>, not editable
		/// </summary>
		private readonly IndexBase indexBase;

		/// <summary>
		/// General initializer
		/// </summary>
		/// <param name="matrixType">indicating the <see cref="matrixType"/></param>
		/// <param name="diagType">indicating the <see cref="diagType"/></param>
		public SparseMatrixDescription(MatrixType matrixType = MatrixType.General, DiagType diagType = DiagType.NonUnit)
		{
			this.matrixType = matrixType;
			this.fillMode = MatrixFillMode.Upper;
			this.diagType = diagType;
			this.indexBase = IndexBase.Zero;
		}

		/// <summary>
		/// Factory method, create <see cref="SparseMatrixDescription"/> from <see cref="MatrixBase{T}"/>.
		/// </summary>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <param name="m">the input <see cref="PureArray{T}"/></param>
		/// <param name="forceGeneral">always regard the input matrix as a general one, default true</param>
		/// <returns>The created <see cref="SparseMatrixDescription"/></returns>
		public static SparseMatrixDescription Create<T>(MatrixBase<T> m, bool forceGeneral = true) where T : struct, IComparable<T>
		{
			if (m is null)
				throw new ArgumentNullException(nameof(m), Resource.ArrayCannotNull);
			if (forceGeneral)
				return new SparseMatrixDescription(matrixType: MatrixType.General);
			else
				return new SparseMatrixDescription(matrixType: (m.Hermitian && m.IsRealType) ? MatrixType.Symmetric : (m.Hermitian && !m.IsRealType) ? MatrixType.Hermitian : MatrixType.General);
		}

		/// <summary>
		/// Factory method, create <see cref="SparseMatrixDescription"/> from <see cref="Storage{T}"/>.
		/// </summary>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <param name="m">the input <see cref="Storage{T}"/></param>
		/// <param name="herm">is <paramref name="m"/> Hermitian or not</param>
		/// <param name="forceGeneral">always regard the input matrix as a general one, default true</param>
		/// <returns>The created <see cref="SparseMatrixDescription"/></returns>
		public static SparseMatrixDescription Create<T>(Storage<T> m, bool herm = false, bool forceGeneral = true) where T : struct, IComparable<T>
		{
			if (m is null)
				throw new ArgumentNullException(nameof(m), Resource.ArrayCannotNull);
			if (forceGeneral)
				return new SparseMatrixDescription(matrixType: MatrixType.General);
			else
			{
				bool b = herm && (default(T).ToDataType() == DataType.RealSingle || default(T).ToDataType() == DataType.ComplexSingle);
				return new SparseMatrixDescription(matrixType: b ? MatrixType.Symmetric : b ? MatrixType.Hermitian : MatrixType.General);
			}
		}

		#region equality
		/// <summary>
		/// Override <see cref="object.Equals(object)"/>
		/// </summary>
		/// <param name="obj">another object</param>
		/// <returns>equal or not</returns>
		public override bool Equals(object obj) => obj is SparseMatrixDescription m && m.GetHashCode() == this.GetHashCode();

		/// <summary>
		/// Override <see cref="object.GetHashCode"/>
		/// </summary>
		/// <returns>hash code</returns>
		public override int GetHashCode() => HashCode.Combine(this.matrixType, this.fillMode, this.diagType, this.indexBase);

		/// <summary>
		/// Equal operator
		/// </summary>
		/// <param name="left">left <see cref="SparseMatrixDescription"/></param>
		/// <param name="right">right <see cref="SparseMatrixDescription"/></param>
		/// <returns>equal or not</returns>
		public static bool operator ==(SparseMatrixDescription left, SparseMatrixDescription right) => left.Equals(right);

		/// <summary>
		/// Not equal operator
		/// </summary>
		/// <param name="left">left <see cref="SparseMatrixDescription"/></param>
		/// <param name="right">right <see cref="SparseMatrixDescription"/></param>
		/// <returns>non-equal or not</returns>
		public static bool operator !=(SparseMatrixDescription left, SparseMatrixDescription right) => !(left == right);

		/// <summary>
		/// Override <see cref="IEquatable{T}.Equals(T)"/>
		/// </summary>
		/// <param name="other">other <see cref="SparseMatrixDescription"/></param>
		/// <returns>equal or not</returns>
		public bool Equals(SparseMatrixDescription other) => other.GetHashCode() == this.GetHashCode();
		#endregion
	}

	/// <summary>
	/// The sparse vector wrapper of CUDA sparse vector <c>spVecDescr</c>.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct SparseVectorWrapper : IDisposable, IEquatable<SparseVectorWrapper>
	{
		private readonly IntPtr pointer;

		/// <summary>
		/// This function initializes the sparse vector descriptor <c>spVecDescr</c>.
		/// </summary>
		/// <param name="size">Size of the sparse vector</param>
		/// <param name="nnz">Number of non-zero entries of the sparse vector</param>
		/// <param name="indices">Indices of the sparse vector. Array of size <paramref name="nnz"/></param>
		/// <param name="values">Values of the sparse vector. Array of size <paramref name="nnz"/></param>
		/// <param name="idxType">Enumerator specifying the data type of <paramref name="indices"/></param>
		/// <param name="idxBase">Enumerator specifying the base index of <paramref name="indices"/></param>
		/// <param name="valueType">Enumerator specifying the data type of <paramref name="values"/></param>
		public SparseVectorWrapper(long size, long nnz, IntPtr indices, IntPtr values, IndexType idxType = IndexType.Integer32, IndexBase idxBase = IndexBase.Zero, CudaDataType valueType = CudaDataType.RealFloat64)
		{
			this.pointer = new IntPtr();
			NativeMethods.cusparseCreateSpVec(ref this.pointer, size, nnz, indices, values, idxType, idxBase, valueType).Check();
		}

		/// <summary>
		/// The factory method that creates from a generic <see cref="SparseVector{T}"/>.
		/// </summary>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <param name="vec">the <see cref="SparseVector{T}"/> to create from</param>
		/// <returns>The created <see cref="SparseVectorWrapper"/>.</returns>
		public static SparseVectorWrapper Create<T>(SparseVector<T> vec) where T : struct, IComparable<T>
		{
			if (vec is null)
				throw new ArgumentNullException(nameof(vec), Resource.ArrayCannotNull);
			return new SparseVectorWrapper(vec.Length, vec.NonZero, vec.IndexPointer, vec.Pointer, valueType: default(T).ToDataType().ToCudaDataType());
		}

		/// <summary>
		/// The factory method that creates from a generic <see cref="SparseVectorWrapper{T}"/>.
		/// </summary>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <param name="vec">the <see cref="SparseVectorWrapper{T}"/> to create from</param>
		/// <param name="length">the length of <paramref name="vec"/></param>
		/// <returns>The created <see cref="SparseVectorWrapper"/>.</returns>
		public static SparseVectorWrapper Create<T>(SparseVectorWrapper<T> vec, long length) where T : struct, IComparable<T>
		{
			return new SparseVectorWrapper(length, vec.Values.Length, vec.Indices, vec.Values, valueType: default(T).ToDataType().ToCudaDataType());
		}

		/// <summary>
		/// Releases the host memory allocated for the sparse vector descriptor <see cref="SparseVectorWrapper"/>.
		/// </summary>
		public void Dispose()
		{
			NativeMethods.cusparseDestroySpVec(this.pointer);
		}

		/// <summary>
		/// Retrieve and set sparse vector values
		/// </summary>
		public IntPtr ValuePointer {
			get {
				var v = new IntPtr();
				NativeMethods.cusparseSpVecGetValues(this.pointer, ref v);
				return v;
			}
			set => NativeMethods.cusparseSpVecSetValues(this.pointer, value);
		}

		#region equality
		/// <summary>
		/// Override <see cref="object.Equals(object)"/>
		/// </summary>
		/// <param name="obj">another object</param>
		/// <returns>equal or not</returns>
		public override bool Equals(object obj) => obj is SparseVectorWrapper m && m.GetHashCode() == this.GetHashCode();

		/// <summary>
		/// Override <see cref="object.GetHashCode"/>
		/// </summary>
		/// <returns>hash code</returns>
		public override int GetHashCode() => this.pointer.GetHashCode();

		/// <summary>
		/// Equal operator
		/// </summary>
		/// <param name="left">left <see cref="SparseVectorWrapper"/></param>
		/// <param name="right">right <see cref="SparseVectorWrapper"/></param>
		/// <returns>equal or not</returns>
		public static bool operator ==(SparseVectorWrapper left, SparseVectorWrapper right) => left.Equals(right);

		/// <summary>
		/// Not equal operator
		/// </summary>
		/// <param name="left">left <see cref="SparseVectorWrapper"/></param>
		/// <param name="right">right <see cref="SparseVectorWrapper"/></param>
		/// <returns>non-equal or not</returns>
		public static bool operator !=(SparseVectorWrapper left, SparseVectorWrapper right) => !(left == right);

		/// <summary>
		/// Override <see cref="IEquatable{T}.Equals(T)"/>
		/// </summary>
		/// <param name="other">other <see cref="SparseVectorWrapper"/></param>
		/// <returns>equal or not</returns>
		public bool Equals(SparseVectorWrapper other) => other.GetHashCode() == this.GetHashCode();
		#endregion
	}

	/// <summary>
	/// The dense vector wrapper of CUDA dense vector <c>dnVecDescr</c>.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct DenseVectorWrapper : IDisposable, IEquatable<DenseVectorWrapper>
	{
		private readonly IntPtr pointer;

		/// <summary>
		/// This function initializes the dense vector descriptor <c>dnVecDescr</c>.
		/// </summary>
		/// <param name="size">Size of the dense vector</param>
		/// <param name="values">Values of the dense vector. Array of size <paramref name="size"/></param>
		/// <param name="valueType">Enumerator specifying the data type of <paramref name="values"/></param>
		public DenseVectorWrapper(long size, IntPtr values, CudaDataType valueType = CudaDataType.RealFloat64)
		{
			this.pointer = new IntPtr();
			NativeMethods.cusparseCreateDnVec(ref this.pointer, size, values, valueType).Check();
		}

		/// <summary>
		/// The factory method that creates from a generic <see cref="DenseVector{T}"/>.
		/// </summary>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <param name="vec">the <see cref="DenseVector{T}"/> to create from</param>
		/// <returns>The created <see cref="DenseVectorWrapper"/>.</returns>
		public static DenseVectorWrapper Create<T>(DenseVector<T> vec) where T : struct, IComparable<T>
		{
			if (vec is null)
				throw new ArgumentNullException(nameof(vec), Resource.ArrayCannotNull);
			return new DenseVectorWrapper(vec.Length, vec.Pointer, valueType: default(T).ToDataType().ToCudaDataType());
		}

		/// <summary>
		/// The factory method that creates from a generic <see cref="Storage{T}"/>.
		/// </summary>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <param name="vec">the <see cref="Storage{T}"/> to create from</param>
		/// <param name="length">the length of <paramref name="vec"/></param>
		/// <returns>The created <see cref="DenseVectorWrapper"/>.</returns>
		public static DenseVectorWrapper Create<T>(Storage<T> vec, long length) where T : struct, IComparable<T>
		{
			if (vec is null)
				throw new ArgumentNullException(nameof(vec), Resource.ArrayCannotNull);
			return new DenseVectorWrapper(length, vec, valueType: default(T).ToDataType().ToCudaDataType());
		}

		/// <summary>
		/// Releases the host memory allocated for the dense vector descriptor <see cref="SparseVectorWrapper"/>.
		/// </summary>
		public void Dispose()
		{
			NativeMethods.cusparseDestroyDnVec(this.pointer);
		}

		/// <summary>
		/// Retrieve and set dense vector values
		/// </summary>
		public IntPtr ValuePointer {
			get {
				var v = new IntPtr();
				NativeMethods.cusparseDnVecGetValues(this.pointer, ref v);
				return v;
			}
			set => NativeMethods.cusparseDnVecSetValues(this.pointer, value);
		}

		#region equality
		/// <summary>
		/// Override <see cref="object.Equals(object)"/>
		/// </summary>
		/// <param name="obj">another object</param>
		/// <returns>equal or not</returns>
		public override bool Equals(object obj) => obj is DenseVectorWrapper m && m.GetHashCode() == this.GetHashCode();

		/// <summary>
		/// Override <see cref="object.GetHashCode"/>
		/// </summary>
		/// <returns>hash code</returns>
		public override int GetHashCode() => this.pointer.GetHashCode();

		/// <summary>
		/// Equal operator
		/// </summary>
		/// <param name="left">left <see cref="DenseVectorWrapper"/></param>
		/// <param name="right">right <see cref="DenseVectorWrapper"/></param>
		/// <returns>equal or not</returns>
		public static bool operator ==(DenseVectorWrapper left, DenseVectorWrapper right) => left.Equals(right);

		/// <summary>
		/// Not equal operator
		/// </summary>
		/// <param name="left">left <see cref="DenseVectorWrapper"/></param>
		/// <param name="right">right <see cref="DenseVectorWrapper"/></param>
		/// <returns>non-equal or not</returns>
		public static bool operator !=(DenseVectorWrapper left, DenseVectorWrapper right) => !(left == right);

		/// <summary>
		/// Override <see cref="IEquatable{T}.Equals(T)"/>
		/// </summary>
		/// <param name="other">other <see cref="DenseVectorWrapper"/></param>
		/// <returns>equal or not</returns>
		public bool Equals(DenseVectorWrapper other) => other.GetHashCode() == this.GetHashCode();
		#endregion
	}

	/// <summary>
	/// The sparse vector wrapper of CUDA sparse matrix <c>spMatDescr</c>.
	/// </summary>
	[StructLayout(LayoutKind.Sequential, Size = 8)]
	public struct SparseMatrixWrapper : IDisposable, IEquatable<SparseMatrixWrapper>
	{
		private readonly IntPtr pointer;

		/// <summary>
		/// Is this wrapper contains a matrix with format <see cref="SparseMatrixFormat.Compressed"/>
		/// </summary>
		public bool IsCompressed { get; }

		/// <summary>
		/// The <see cref="PowerOperation"/> about to apply on this matrix
		/// </summary>
		public PowerOperation Operation { get; }

		/// <summary>
		/// This function initializes the sparse matrix descriptor <c>spMatDescr</c> in the CSR format.
		/// </summary>
		/// <param name="rows">Number of rows of the sparse matrix</param>
		/// <param name="cols">Number of columns of the sparse matrix</param>
		/// <param name="nnz">Number of non-zero entries of the sparse matrix</param>
		/// <param name="isCSR">Is this matrix a CSR matrix or CSC</param>
		/// <param name="rowPtr">Row offsets of the sparse matrix. Array of size <c><paramref name="rows"/> + 1</c></param>
		/// <param name="colPtr">Column indices of the sparse matrix. Array of size <paramref name="nnz"/></param>
		/// <param name="values">Values of the sparse matrix. Array of size <paramref name="nnz"/></param>
		/// <param name="rowPtrType">Enumerator specifying the data type of <paramref name="rowPtr"/></param>
		/// <param name="colPtrType">Enumerator specifying the data type of <paramref name="colPtr"/></param>
		/// <param name="idxBase">Enumerator specifying the base index of <paramref name="rowPtr"/> and <paramref name="colPtr"/></param>
		/// <param name="valueType">Enumerator specifying the data type of <paramref name="values"/></param>
		/// <param name="op">The <see cref="PowerOperation"/> about to apply on this matrix</param>
		public SparseMatrixWrapper(bool isCSR, long rows, long cols, long nnz, IntPtr rowPtr, IntPtr colPtr, IntPtr values, IndexType rowPtrType = IndexType.Integer32, IndexType colPtrType = IndexType.Integer32, IndexBase idxBase = IndexBase.Zero, CudaDataType valueType = CudaDataType.RealFloat64, PowerOperation op = PowerOperation.None)
		{
			this.pointer = new IntPtr();
			this.IsCompressed = true;
			this.Operation = isCSR ? op : ~op; // see PowerOperation for the reason
			(rows, cols, rowPtr, colPtr, rowPtrType, colPtrType) = isCSR ? (rows, cols, rowPtr, colPtr, rowPtrType, colPtrType) : (cols, rows, colPtr, rowPtr, colPtrType, rowPtrType);
			NativeMethods.cusparseCreateCsr(ref this.pointer, rows, cols, nnz, rowPtr, colPtr, values, rowPtrType, colPtrType, idxBase, valueType).Check();
		}

		/// <summary>
		/// This function initializes the sparse matrix descriptor <c>spMatDescr</c> in the COO format.
		/// </summary>
		/// <param name="rows">Number of rows of the sparse matrix</param>
		/// <param name="cols">Number of columns of the sparse matrix</param>
		/// <param name="nnz">Number of non-zero entries of the sparse matrix</param>
		/// <param name="cooRowInd">Row offsets of the sparse matrix. Array of size <c><paramref name="rows"/> + 1</c></param>
		/// <param name="cooColInd">Column indices of the sparse matrix. Array of size <paramref name="nnz"/></param>
		/// <param name="cooValues">Values of the sparse matrix. Array of size <paramref name="nnz"/></param>
		/// <param name="indexType">Enumerator specifying the data type of <paramref name="cooRowInd"/> and <paramref name="cooColInd"/></param>
		/// <param name="idxBase">Enumerator specifying the base index of <paramref name="cooRowInd"/> and <paramref name="cooColInd"/></param>
		/// <param name="valueType">Enumerator specifying the data type of <paramref name="cooValues"/></param>
		/// <param name="op">The <see cref="PowerOperation"/> about to apply on this matrix</param>
		public SparseMatrixWrapper(long rows, long cols, long nnz, IntPtr cooRowInd, IntPtr cooColInd, IntPtr cooValues, IndexType indexType = IndexType.Integer32, IndexBase idxBase = IndexBase.Zero, CudaDataType valueType = CudaDataType.RealFloat64, PowerOperation op = PowerOperation.None)
		{
			this.pointer = new IntPtr();
			this.IsCompressed = false;
			this.Operation = op;
			NativeMethods.cusparseCreateCoo(ref this.pointer, rows, cols, nnz, cooRowInd, cooColInd, cooValues, indexType, idxBase, valueType).Check();
		}

		/// <summary>
		/// The factory method that creates from a generic <see cref="SparseMatrix{T}"/>.
		/// </summary>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <param name="matrix">the <see cref="SparseMatrix{T}"/> to create from</param>
		/// <param name="op">The <see cref="PowerOperation"/> about to apply on <paramref name="matrix"/></param>
		/// <returns>The created <see cref="SparseMatrixWrapper"/></returns>
		public static SparseMatrixWrapper Create<T>(SparseMatrix<T> matrix, PowerOperation op = PowerOperation.None) where T : struct, IComparable<T>
		{
			if (matrix is null)
				throw new ArgumentNullException(nameof(matrix), Resource.ArrayCannotNull);
			if ((matrix.Format & SparseMatrixFormat.Coordinated) != 0)
			{
				return new SparseMatrixWrapper(matrix.NRows, matrix.NCols, matrix.NonZero, matrix.RowPointer, matrix.ColumnPointer, matrix.Pointer, valueType: default(T).ToDataType().ToCudaDataType(), op: op);
			}
			else if ((matrix.Format & SparseMatrixFormat.Compressed) != 0)
			{
				return new SparseMatrixWrapper(isCSR: matrix.Format == SparseMatrixFormat.CSR, matrix.NRows, matrix.NCols, matrix.NonZero, matrix.RowPointer, matrix.ColumnPointer, matrix.Pointer, valueType: default(T).ToDataType().ToCudaDataType(), op: op);
			}
			else
			{
				throw new NotSupportedException(Resource.DataTypeNotSupport);
			}
		}

		/// <summary>
		/// The factory method that creates from a generic <see cref="SparseMatrixWrapper{T}"/>.
		/// </summary>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <param name="matrix">the <see cref="SparseMatrixWrapper{T}"/> to create from</param>
		/// <param name="format">the <see cref="SparseMatrixFormat"/> of matrix</param>
		/// <param name="m">the number of rows of <paramref name="matrix"/></param>
		/// <param name="n">the number of columns of <paramref name="matrix"/></param>
		/// <param name="op">The <see cref="PowerOperation"/> about to apply on <paramref name="matrix"/></param>
		/// <returns>The created <see cref="SparseMatrixWrapper"/></returns>
		public static SparseMatrixWrapper Create<T>(SparseMatrixWrapper<T> matrix, long m, long n, SparseMatrixFormat format, PowerOperation op = PowerOperation.None) where T : struct, IComparable<T>
		{
			if (matrix == default)
				throw new ArgumentNullException(nameof(matrix), Resource.ArrayCannotNull);
			if ((format & SparseMatrixFormat.Coordinated) != 0)
			{
				return new SparseMatrixWrapper(m, n, matrix.Values.Length, matrix.Row, matrix.Column, matrix.Values, valueType: default(T).ToDataType().ToCudaDataType(), op: op);
			}
			else if ((format & SparseMatrixFormat.Compressed) != 0)
			{
				return new SparseMatrixWrapper(isCSR: format == SparseMatrixFormat.CSR, m, n, matrix.Values.Length, matrix.Row, matrix.Column, matrix.Values, valueType: default(T).ToDataType().ToCudaDataType(), op: op);
			}
			else
			{
				throw new NotSupportedException(Resource.DataTypeNotSupport);
			}
		}

		/// <summary>
		/// Releases the host memory allocated for the sparse matrix descriptor <see cref="SparseMatrixWrapper"/>.
		/// </summary>
		public void Dispose()
		{
			NativeMethods.cusparseDestroySpMat(this.pointer);
		}

		/// <summary>
		/// Retrieve and set sparse matrix values
		/// </summary>
		public IntPtr ValuePointer {
			get {
				var v = new IntPtr();
				NativeMethods.cusparseSpMatGetValues(this.pointer, ref v);
				return v;
			}
			set => NativeMethods.cusparseSpMatSetValues(this.pointer, value);
		}

		#region equality
		/// <summary>
		/// Override <see cref="object.Equals(object)"/>
		/// </summary>
		/// <param name="obj">another object</param>
		/// <returns>equal or not</returns>
		public override bool Equals(object obj) => obj is SparseMatrixWrapper m && m.GetHashCode() == this.GetHashCode();

		/// <summary>
		/// Override <see cref="object.GetHashCode"/>
		/// </summary>
		/// <returns>hash code</returns>
		public override int GetHashCode() => this.pointer.GetHashCode();

		/// <summary>
		/// Equal operator
		/// </summary>
		/// <param name="left">left <see cref="SparseMatrixWrapper"/></param>
		/// <param name="right">right <see cref="SparseMatrixWrapper"/></param>
		/// <returns>equal or not</returns>
		public static bool operator ==(SparseMatrixWrapper left, SparseMatrixWrapper right) => left.Equals(right);

		/// <summary>
		/// Not equal operator
		/// </summary>
		/// <param name="left">left <see cref="SparseMatrixWrapper"/></param>
		/// <param name="right">right <see cref="SparseMatrixWrapper"/></param>
		/// <returns>non-equal or not</returns>
		public static bool operator !=(SparseMatrixWrapper left, SparseMatrixWrapper right) => !(left == right);

		/// <summary>
		/// Override <see cref="IEquatable{T}.Equals(T)"/>
		/// </summary>
		/// <param name="other">other <see cref="SparseMatrixWrapper"/></param>
		/// <returns>equal or not</returns>
		public bool Equals(SparseMatrixWrapper other) => other.GetHashCode() == this.GetHashCode();
		#endregion
	}

	/// <summary>
	/// The dense vector wrapper of CUDA dense matrix <c>dnMatDescr</c>.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct DenseMatrixWrapper : IDisposable, IEquatable<DenseMatrixWrapper>
	{
		private readonly IntPtr pointer;

		/// <summary>
		/// This function initializes the dense matrix descriptor <c>dnMatDescr</c>.
		/// </summary>
		/// <param name="rows">Number of rows of the dense matrix</param>
		/// <param name="cols">Number of columns of the dense matrix</param>
		/// <param name="ld">Leading dimension dense matrix</param>
		/// <param name="values">Values of the dense matrix. Array of size <paramref name="rows"/> * <paramref name="cols"/></param>
		/// <param name="valueType">Enumerator specifying the data type of <paramref name="values"/></param>
		public DenseMatrixWrapper(long rows, long cols, long ld, IntPtr values, CudaDataType valueType = CudaDataType.RealFloat64)
		{
			this.pointer = new IntPtr();
			NativeMethods.cusparseCreateDnMat(ref this.pointer, rows, cols, ld, values, valueType, Order.ColumnMajor).Check();
		}

		/// <summary>
		/// The factory method that creates from a generic <see cref="DenseMatrix{T}"/>.
		/// </summary>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <param name="matrix">the <see cref="DenseMatrix{T}"/> to create from</param>
		/// <returns>The created <see cref="DenseMatrixWrapper"/></returns>
		public static DenseMatrixWrapper Create<T>(DenseMatrix<T> matrix) where T : struct, IComparable<T>
		{
			if (matrix is null)
				throw new ArgumentNullException(nameof(matrix), Resource.ArrayCannotNull);
			return new DenseMatrixWrapper(matrix.NRows, matrix.NCols, matrix.LeadDim, matrix.Pointer, default(T).ToDataType().ToCudaDataType());
		}

		/// <summary>
		/// The factory method that creates from a pointer.
		/// </summary>
		/// <typeparam name="T">see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <param name="matrix">the <see cref="Storage{T}"/> to create from</param>
		/// <param name="ld">leading dimension</param>
		/// <param name="m">number of rows</param>
		/// <param name="n">number of columns</param>
		/// <returns>The created <see cref="DenseMatrixWrapper"/></returns>
		public static DenseMatrixWrapper Create<T>(Storage<T> matrix, long m, long n, long ld) where T : struct, IComparable<T>
		{
			if (matrix is null)
				throw new ArgumentNullException(nameof(matrix), Resource.ArrayCannotNull);
			return new DenseMatrixWrapper(m, n, ld, matrix, default(T).ToDataType().ToCudaDataType());
		}

		/// <summary>
		/// Releases the host memory allocated for the dense matrix descriptor <see cref="SparseMatrixWrapper"/>.
		/// </summary>
		public void Dispose()
		{
			NativeMethods.cusparseDestroyDnMat(this.pointer);
		}

		/// <summary>
		/// Retrieve and set dense matrix values
		/// </summary>
		public IntPtr ValuePointer {
			get {
				var v = new IntPtr();
				NativeMethods.cusparseDnMatGetValues(this.pointer, ref v);
				return v;
			}
			set => NativeMethods.cusparseDnMatSetValues(this.pointer, value);
		}

		#region equality
		/// <summary>
		/// Override <see cref="object.Equals(object)"/>
		/// </summary>
		/// <param name="obj">another object</param>
		/// <returns>equal or not</returns>
		public override bool Equals(object obj) => obj is DenseMatrixWrapper m && m.GetHashCode() == this.GetHashCode();

		/// <summary>
		/// Override <see cref="object.GetHashCode"/>
		/// </summary>
		/// <returns>hash code</returns>
		public override int GetHashCode() => this.pointer.GetHashCode();

		/// <summary>
		/// Equal operator
		/// </summary>
		/// <param name="left">left <see cref="DenseMatrixWrapper"/></param>
		/// <param name="right">right <see cref="DenseMatrixWrapper"/></param>
		/// <returns>equal or not</returns>
		public static bool operator ==(DenseMatrixWrapper left, DenseMatrixWrapper right) => left.Equals(right);

		/// <summary>
		/// Not equal operator
		/// </summary>
		/// <param name="left">left <see cref="DenseMatrixWrapper"/></param>
		/// <param name="right">right <see cref="DenseMatrixWrapper"/></param>
		/// <returns>non-equal or not</returns>
		public static bool operator !=(DenseMatrixWrapper left, DenseMatrixWrapper right) => !(left == right);

		/// <summary>
		/// Override <see cref="IEquatable{T}.Equals(T)"/>
		/// </summary>
		/// <param name="other">other <see cref="DenseMatrixWrapper"/></param>
		/// <returns>equal or not</returns>
		public bool Equals(DenseMatrixWrapper other) => other.GetHashCode() == this.GetHashCode();
		#endregion
	}
}
