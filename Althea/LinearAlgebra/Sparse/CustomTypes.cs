using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Althea.Arrays;
using Althea.Helpers;


namespace Althea.LinearAlgebra.Sparse
{
	#region enum
	/// <summary>
	/// The <see cref="SparseVectorFormat"/> enum indicates the format specification of a sparse vector. Each bit flag indicates an atomic format.
	/// </summary>
	[Flags]
	public enum SparseVectorFormat : int
	{
		/// <summary>
		/// The Coordinate Format that stores each non-zero element and its <b>zero-based</b> index in separate storages.
		/// </summary>
		/// <remarks>For default implementations, this is the only supported sparse vector format</remarks>
		Coordinated = 1 << 0,
	}

	/// <summary>
	/// The <see cref="SparseMatrixFormat"/> enum indicates the format specification of a sparse matrix. Each bit flag indicates an atomic format.
	/// </summary>
	/// <remarks>All the internally defined formats are 3-array variations rather than 4-array ones.<br/>
	/// All non-compressed formats shall lies within the left-most 16 bits, and compressed ones shall lies within the right-most 16 bits.<br/>
	/// All the row major formats shall lies within the even bits, and column major ones shall lies within the odd bits.</remarks>
	[Flags]
	public enum SparseMatrixFormat : int
	{
		/// <summary>
		/// Coordinate Row-major Format (COR) that stores each non-zero element's <c>x</c> and <c>y</c> coordinates which are sorted in the row-first order.
		/// </summary>
		COOR = 1 << 0,
		/// <summary>
		/// Coordinate Column-major Format (COC) that stores each non-zero element's <c>x</c> and <c>y</c> coordinates which are sorted in the column-first order. The transpose of a <see cref="COOR"/> format is the <see cref="COOC"/> format.
		/// </summary>
		COOC = 1 << 1,
		/// <summary>
		/// Block Coordinate Row-major Format (BCOR) that stores each non-zero block's <c>x</c> and <c>y</c> coordinates which are sorted in the row-first order.
		/// </summary>
		BCOR = 1 << 2,
		/// <summary>
		/// Block Coordinate Column-major Format (BCOR) that stores each non-zero block's <c>x</c> and <c>y</c> coordinates which are sorted in the column-first order.
		/// </summary>
		BCOC = 1 << 3,

		/// <summary>
		/// Compressed Sparse Column Format (CSC). The only way the <see cref="CSC"/> differs from the <see cref="CSR"/> format is that the column index array instead of row index array stores the end-of-column (not end-of-row) offsets. The transpose of a <see cref="CSR"/> format is the <see cref="CSC"/> format.
		/// </summary>
		CSC = 1 << 31,
		/// <summary>
		/// Compressed Sparse Row Format (CSR). The only way the <see cref="CSR"/> differs from the <see cref="COOR"/> format is that the array containing the row indices is compressed, that is, the row index array only stores the end-of-row offsets with <c>size == number_of_rows + 1</c> whose first element is 0.
		/// </summary>
		CSR = 1 << 30,
		/// <summary>
		/// Block Sparse Column Format (BSC). The only way the <see cref="BSC"/> differs from the <see cref="BSR"/> format is that the column index array instead of row index array stores the end-of-column (not end-of-row) offsets of blocks. The transpose of a <see cref="BSR"/> format is the <see cref="BSC"/> format.
		/// </summary>
		BSC = 1 << 29,
		/// <summary>
		/// Block Sparse Row Format (BSR). The only way the <see cref="BSR"/> differs from the <see cref="CSR"/> format is that instead of indexing values, <see cref="BSR"/> indexes the dense block sub-matrices. Therefore, this requires additional parameters: number of non-zero blocks instead of non-zero values, number of block matrix rows, number of block matrix columns, end-of-row offsets are counted in blocks and therefore is of <c>size == number_of_rows / block_rows + 1</c>.
		/// </summary>
		BSR = 1 << 28,
	}
	#endregion


	#region wrapper
	/// <summary>
	/// The simple wrapper structure for any sparse array (usually <see cref="SparseVector{T, TInd}"/> and <see cref="SparseMatrix{T, TInd}"/>) which is typically used as outputs of methods in <see cref="AbstractApi"/>.
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
	public readonly struct SparseArrayWrapper<T> : IDisposable where T : unmanaged
	{
		/// <summary>
		/// The interface to store the other information used by <see cref="SparseArrayWrapper{T}"/>
		/// </summary>
		public interface IOtherInfo : IReadOnlyList<object> { }

		private readonly Storage<T> values;

		private readonly SizedFixedClassBuffer_8<IStorage> indices;

		private readonly int format;

		private readonly IOtherInfo? info;

		/// <summary>
		/// Dispose this wrapper.
		/// </summary>
		/// <remarks>When the <see cref="SparseArrayWrapper{T}"/> was created from <see cref="ISparseArray{T}"/>, it shall only contains referenced storages. Therefore, the disposition in such situation does nothing.<br/>
		/// However, it will dispose the storages created inside the method implementations of <see cref="AbstractApi"/>.</remarks>
		public void Dispose()
		{
			this.values?.Dispose();
			for (int i = 0; i < this.indices.Count; i++)
			{
				this.indices[i]?.Dispose();
			}
		}

		/// <summary>
		/// Get the value array storage of this sparse array wrapper as a <see cref="Storage{T}"/> of <typeparamref name="T"/>
		/// </summary>
		public Storage<T> ValueStorage => this.values;

		/// <summary>
		/// Get the storages of index arrays of this sparse array wrapper as a <see cref="IReadOnlyList{T}"/> of <see cref="IStorage"/>
		/// </summary>
		public IReadOnlyList<IStorage> IndexStorages => this.indices;

		/// <summary>
		/// Get the <see cref="SparseVectorFormat"/> of this sparse array as if this array is a sparse vector
		/// </summary>
		public SparseVectorFormat VectorFormat => (SparseVectorFormat)this.format;

		/// <summary>
		/// Get the <see cref="SparseMatrixFormat"/> of this sparse array as if this array is a sparse matrix
		/// </summary>
		public SparseMatrixFormat MatrixFormat => (SparseMatrixFormat)this.format;

		/// <summary>
		/// Get the <see cref="IOtherInfo"/> of this sparse array
		/// </summary>
		public IOtherInfo? OtherInfo => this.info;

		/// <summary>
		/// Create a <see cref="SparseArrayWrapper{T}"/> with given <paramref name="values"/>, <paramref name="indices"/> and <paramref name="format"/>
		/// </summary>
		/// <param name="values">The value array storage</param>
		/// <param name="indices">The storages of index arrays</param>
		/// <param name="format">The format as a <see cref="int"/></param>
		/// <param name="info">The <see cref="IOtherInfo"/> storing other information, can be null</param>
		public SparseArrayWrapper(Storage<T> values, IReadOnlyList<IStorage> indices, int format, IOtherInfo? info = null)
		{
			this.values = values; this.indices = new SizedFixedClassBuffer_8<IStorage>(indices); this.format = format; this.info = info;
		}

		/// <summary>
		/// Create a <see cref="SparseArrayWrapper{T}"/> with the given sparse <paramref name="vector"/>
		/// </summary>
		/// <param name="vector">The given <see cref="ISparseVector{T}"/> used to create</param>
		/// <param name="info">The <see cref="IOtherInfo"/> storing other information, can be null</param>
		/// <returns>The <see cref="SparseArrayWrapper{T}"/> created from <paramref name="vector"/></returns>
		public static SparseArrayWrapper<T> Create(ISparseVector<T> vector, IOtherInfo? info = null) => new(vector.Storage, vector, (int)vector.Format, info);

		/// <summary>
		/// Create a <see cref="SparseArrayWrapper{T}"/> with the given sparse <paramref name="matrix"/>
		/// </summary>
		/// <param name="matrix">The given <see cref="ISparseMatrix{T}"/> used to create</param>
		/// <param name="info">The <see cref="IOtherInfo"/> storing other information, can be null</param>
		/// <returns>The <see cref="SparseArrayWrapper{T}"/> created from <paramref name="matrix"/></returns>
		public static SparseArrayWrapper<T> Create(ISparseMatrix<T> matrix, IOtherInfo? info = null) => new(matrix.Storage, matrix, (int)matrix.Format, info);
	}
	#endregion


	#region extension methods
	/// <summary>
	/// The static class for extension methods of <see cref="SparseVectorFormat"/> and <see cref="SparseMatrixFormat"/>
	/// </summary>
	public static class FormatExtension
	{
		#region constants
		/// <summary>
		/// The coordinated formats for sparse matrices.
		/// </summary>
		public const SparseMatrixFormat Coordinated = SparseMatrixFormat.COOR | SparseMatrixFormat.COOC | SparseMatrixFormat.BCOR | SparseMatrixFormat.BCOC;

		/// <summary>
		/// The compressed formats for sparse matrices.
		/// </summary>
		public const SparseMatrixFormat Compressed = SparseMatrixFormat.CSR | SparseMatrixFormat.CSC | SparseMatrixFormat.BSR | SparseMatrixFormat.BSC;

		/// <summary>
		/// The row majored formats for sparse matrices.
		/// </summary>
		public const SparseMatrixFormat RowMajor = SparseMatrixFormat.COOR | SparseMatrixFormat.CSR | SparseMatrixFormat.BSR | SparseMatrixFormat.BCOR;

		/// <summary>
		/// The column majored formats for sparse matrices.
		/// </summary>
		public const SparseMatrixFormat ColumnMajor = SparseMatrixFormat.COOC | SparseMatrixFormat.CSC | SparseMatrixFormat.BSC | SparseMatrixFormat.BCOC;

		/// <summary>
		/// The sparse matrices formats that stores the elements one by one rather than block by block
		/// </summary>
		public const SparseMatrixFormat NonBlocked = SparseMatrixFormat.COOR | SparseMatrixFormat.COOC | SparseMatrixFormat.CSR | SparseMatrixFormat.CSC;

		/// <summary>
		/// The sparse matrices formats that stores the elements block by block rather than one by one
		/// </summary>
		public const SparseMatrixFormat Blocked = SparseMatrixFormat.BCOR | SparseMatrixFormat.BCOC | SparseMatrixFormat.BSR | SparseMatrixFormat.BSC;

		/// <summary>
		/// The internally defined formats for sparse matrices.
		/// </summary>
		public const SparseMatrixFormat PreDefined = NonBlocked | Blocked;


		/// <summary>
		/// All of the possible <see cref="SparseVectorFormat"/>s. Value = 111...111
		/// </summary>
		/// <remarks>Since the atom formats are all orthogonal in binary, this kind of definition becomes useful.<br/>
		/// If some custom formats are defined afterwards, this trick should still be used.</remarks>
		public const SparseVectorFormat VectorAny = (SparseVectorFormat)~0;

		/// <summary>
		/// All of the possible <see cref="SparseMatrixFormat"/>s. Value = 111...111
		/// </summary>
		/// <remarks>Since the atom formats are all orthogonal in binary, this kind of definition becomes useful.<br/>
		/// If some custom formats are defined afterwards, this trick should still be used.</remarks>
		public const SparseMatrixFormat MatrixAny = (SparseMatrixFormat)~0;
		#endregion

		#region methods
		/// <summary>
		/// Check whether the given <see cref="SparseVectorFormat"/> is an atomic format or not
		/// </summary>
		/// <param name="format">The given <see cref="SparseVectorFormat"/> to check</param>
		/// <returns>True if <paramref name="format"/> is an atomic format, i.e. a power of two; false otherwise.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsAtomic(this SparseVectorFormat format) => ((int)format).IsPowerOfTwo();

		/// <summary>
		/// Decompose the given <see cref="SparseVectorFormat"/> into atomic <paramref name="result"/>s.
		/// </summary>
		/// <param name="format">The given <see cref="SparseVectorFormat"/> to be decomposed</param>
		/// <param name="result">The <see cref="Span{T}"/> to put the results</param>
		/// <returns>The sliced <paramref name="result"/> whose length is the number of atomic formats</returns>
		/// <exception cref="ArgumentException">If the length of <paramref name="result"/> is less than the number of atomic formats</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Span<SparseVectorFormat> Decompose(this SparseVectorFormat format, Span<SparseVectorFormat> result)
		{
			if (format == 0)
				return Span<SparseVectorFormat>.Empty;
			int f = (int)format;
			byte c = f.CountBitSet();
			if (result.Length < c)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(result));
			result = result[..c]; c = 0;
			if (f.IsPowerOfTwo())
			{
				result[0] = format; return result;
			}
			for (byte i = 0; i < 32; i++)
			{
				if (f.IsBitSet(i))
				{
					result[c++] = (SparseVectorFormat)(1 << i);
				}
			}
			return result;
		}

		/// <summary>
		/// Check whether the given <see cref="SparseMatrixFormat"/> is an atomic format or not
		/// </summary>
		/// <param name="format">The given <see cref="SparseMatrixFormat"/> to check</param>
		/// <returns>True if <paramref name="format"/> is an atomic format, i.e. a power of two; false otherwise.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsAtomic(this SparseMatrixFormat format) => ((int)format).IsPowerOfTwo();

		/// <summary>
		/// Decompose the given <see cref="SparseMatrixFormat"/> into atomic <paramref name="result"/>s.
		/// </summary>
		/// <param name="format">The given <see cref="SparseMatrixFormat"/> to be decomposed</param>
		/// <param name="result">The <see cref="Span{T}"/> to put the results</param>
		/// <returns>The sliced <paramref name="result"/> whose length is the number of atomic formats</returns>
		/// <exception cref="ArgumentException">If the length of <paramref name="result"/> is less than the number of atomic formats</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Span<SparseMatrixFormat> Decompose(this SparseMatrixFormat format, Span<SparseMatrixFormat> result)
		{
			if (format == 0)
				return Span<SparseMatrixFormat>.Empty;
			int f = (int)format;
			byte c = f.CountBitSet();
			if (result.Length < c)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(result));
			result = result[..c]; c = 0;
			if (f.IsPowerOfTwo())
			{
				result[0] = format; return result;
			}	
			for (byte i = 0; i < 32; i++)
			{
				if (f.IsBitSet(i))
				{
					result[c++] = (SparseMatrixFormat)(1 << i);
				}
			}
			return result;
		}

		/// <summary>
		/// Check whether the given <see cref="SparseMatrixFormat"/> is a row major format or not
		/// </summary>
		/// <param name="format">The given <see cref="SparseMatrixFormat"/> to check, can be anatomic</param>
		/// <returns>True if <paramref name="format"/> is a row major format, i.e. <c><paramref name="format"/>.<see cref="Decompose(SparseMatrixFormat, Span{SparseMatrixFormat})">Decompose</see>().All(f => log2(f) % 2 == 0)</c> [or equivalently <c>(format | 0xAAAA_AAAA) == 0xAAAA_AAAA</c>]; false otherwise.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsRowMajor(this SparseMatrixFormat format) => (((int)format) | 0xAAAA_AAAA) == 0xAAAA_AAAA;

		/// <summary>
		/// Check whether the given <see cref="SparseMatrixFormat"/> is a compressed format or not
		/// </summary>
		/// <param name="format">The given <see cref="SparseMatrixFormat"/> to check, can be anatomic</param>
		/// <returns>True if <paramref name="format"/> is a compressed format, i.e. <c>((format &gt;&gt; 16) &lt;&lt; 16) == format</c> ; false otherwise.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsCompressed(this SparseMatrixFormat format) => (((int)format >> 16) << 16) == (int)format;
		#endregion
	}
	#endregion
}
