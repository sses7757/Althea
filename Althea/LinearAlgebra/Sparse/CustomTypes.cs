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
	/// The interface to store the other information used by <see cref="SparseArrayWrapper{T}"/>
	/// </summary>
	public interface IOtherInfo : IReadOnlyList<object> { }

	/// <summary>
	/// The simple wrapper structure for any sparse array which is typically used as outputs of API methods.
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
	public readonly ref struct SparseArrayWrapper<T> where T : unmanaged
	{
		private readonly Storage<T> values;

		private readonly ReadOnlySpan<IStorage> indices;

		private readonly int format;

		private readonly IOtherInfo? info;

		private readonly T defaultValue;

		/// <summary>
		/// Get the value array storage of this sparse array wrapper as a <see cref="Storage{T}"/> of <typeparamref name="T"/>
		/// </summary>
		public Storage<T> ValueStorage {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.values;
		}

		/// <summary>
		/// Get the storages of index arrays of this sparse array wrapper as a <see cref="ReadOnlySpan{T}"/> of <see cref="IStorage"/>
		/// </summary>
		public ReadOnlySpan<IStorage> IndexStorages {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.indices;
		}

		/// <summary>
		/// Get the <see cref="SparseVectorFormat"/> of this sparse array as if this array is a sparse vector
		/// </summary>
		public SparseVectorFormat VectorFormat {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => (SparseVectorFormat)this.format;
		}

		/// <summary>
		/// Get the <see cref="SparseMatrixFormat"/> of this sparse array as if this array is a sparse matrix
		/// </summary>
		public SparseMatrixFormat MatrixFormat {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => (SparseMatrixFormat)this.format;
		}

		/// <summary>
		/// Get the <see cref="TensorAlgebra.Sparse.SparseTensorFormat"/> of this sparse array as if this array is a sparse tensor
		/// </summary>
		public TensorAlgebra.Sparse.SparseTensorFormat TensorFormat {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => (TensorAlgebra.Sparse.SparseTensorFormat)this.format;
		}

		/// <summary>
		/// Get the <see cref="IOtherInfo"/> of this sparse array
		/// </summary>
		public IOtherInfo? OtherInfo {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.info;
		}

		/// <summary>
		/// Get the default value of this sparse array
		/// </summary>
		public T DefaultValue {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.defaultValue;
		}

		/// <summary>
		/// Create a <see cref="SparseArrayWrapper{T}"/> with given <paramref name="values"/>, <paramref name="indices"/> and <paramref name="format"/>
		/// </summary>
		/// <param name="values">The value array storage</param>
		/// <param name="indices">The storages of index arrays</param>
		/// <param name="format">The format as a <see cref="int"/></param>
		/// <param name="defaultValue">The default value of this sparse array</param>
		/// <param name="info">The <see cref="IOtherInfo"/> storing other information, can be null</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SparseArrayWrapper(Storage<T> values, ReadOnlySpan<IStorage> indices, int format, T defaultValue, IOtherInfo? info = null)
		{
			this.values = values; this.indices = indices; this.format = format; this.defaultValue = defaultValue; this.info = info;
		}

		/// <summary>
		/// Dispose this wrapper.
		/// </summary>
		/// <remarks>When the <see cref="SparseArrayWrapper{T}"/> was created from <see cref="ISparseArray{T}"/>, it shall only contains referenced storages. Therefore, the disposition in such situation does nothing.<br/>
		/// However, it will dispose the storages created inside the method implementations of <see cref="AbstractApi"/>.</remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Dispose()
		{
			this.values?.Dispose();
			for (int i = 0; i < this.indices.Length; i++)
			{
				this.indices[i]?.Dispose();
			}
		}
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
		/// Get the non-blocked corresponding <see cref="SparseMatrixFormat"/> of the given (pre-defined, atomic) <paramref name="format"/>
		/// </summary>
		/// <param name="format">The atomic pre-defined <see cref="SparseMatrixFormat"/> whose non-blocked corresponding format is about be obtained</param>
		/// <returns>The non-blocked corresponding <see cref="SparseMatrixFormat"/> of the given <paramref name="format"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="format"/> is anatomic or not pre-defined</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static SparseMatrixFormat NonBlockedCorresponding(this SparseMatrixFormat format)
		{
			return format switch
			{
				SparseMatrixFormat.COOR => format,
				SparseMatrixFormat.COOC => format,
				SparseMatrixFormat.BCOR => SparseMatrixFormat.COOR,
				SparseMatrixFormat.BCOC => SparseMatrixFormat.COOC,
				SparseMatrixFormat.CSC => format,
				SparseMatrixFormat.CSR => format,
				SparseMatrixFormat.BSC => SparseMatrixFormat.CSC,
				SparseMatrixFormat.BSR => SparseMatrixFormat.CSR,
				_ => throw new ArgumentOutOfRangeException(nameof(format), format, Resources.Parameter.InvalidValue),
			};
		}

		/// <summary>
		/// Get the blocked corresponding <see cref="SparseMatrixFormat"/> of the given (pre-defined, atomic) <paramref name="format"/>
		/// </summary>
		/// <param name="format">The atomic pre-defined <see cref="SparseMatrixFormat"/> whose blocked corresponding format is about be obtained</param>
		/// <returns>The blocked corresponding <see cref="SparseMatrixFormat"/> of the given <paramref name="format"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="format"/> is anatomic or not pre-defined</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static SparseMatrixFormat BlockedCorresponding(this SparseMatrixFormat format)
		{
			return format switch
			{
				SparseMatrixFormat.COOR => SparseMatrixFormat.BCOR,
				SparseMatrixFormat.COOC => SparseMatrixFormat.BCOC,
				SparseMatrixFormat.BCOR => format,
				SparseMatrixFormat.BCOC => format,
				SparseMatrixFormat.CSC => SparseMatrixFormat.BSC,
				SparseMatrixFormat.CSR => SparseMatrixFormat.BSR,
				SparseMatrixFormat.BSC => format,
				SparseMatrixFormat.BSR => format,
				_ => throw new ArgumentOutOfRangeException(nameof(format), format, Resources.Parameter.InvalidValue),
			};
		}

		/// <summary>
		/// Get the column-major corresponding <see cref="SparseMatrixFormat"/> of the given (pre-defined, atomic) <paramref name="format"/>
		/// </summary>
		/// <param name="format">The atomic pre-defined <see cref="SparseMatrixFormat"/> whose column-major corresponding format is about be obtained</param>
		/// <returns>The column-major corresponding <see cref="SparseMatrixFormat"/> of the given <paramref name="format"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="format"/> is anatomic or not pre-defined</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static SparseMatrixFormat ColumnMajorCorresponding(this SparseMatrixFormat format)
		{
			return format switch
			{
				SparseMatrixFormat.COOR => SparseMatrixFormat.COOC,
				SparseMatrixFormat.COOC => format,
				SparseMatrixFormat.BCOR => SparseMatrixFormat.BCOC,
				SparseMatrixFormat.BCOC => format,
				SparseMatrixFormat.CSC => format,
				SparseMatrixFormat.CSR => SparseMatrixFormat.CSC,
				SparseMatrixFormat.BSC => format,
				SparseMatrixFormat.BSR => SparseMatrixFormat.BSC,
				_ => throw new ArgumentOutOfRangeException(nameof(format), format, Resources.Parameter.InvalidValue),
			};
		}

		/// <summary>
		/// Get the row-major corresponding <see cref="SparseMatrixFormat"/> of the given (pre-defined, atomic) <paramref name="format"/>
		/// </summary>
		/// <param name="format">The atomic pre-defined <see cref="SparseMatrixFormat"/> whose row-major corresponding format is about be obtained</param>
		/// <returns>The row-major corresponding <see cref="SparseMatrixFormat"/> of the given <paramref name="format"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="format"/> is anatomic or not pre-defined</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static SparseMatrixFormat RowMajorCorresponding(this SparseMatrixFormat format)
		{
			return format switch
			{
				SparseMatrixFormat.COOR => format,
				SparseMatrixFormat.COOC => SparseMatrixFormat.COOR,
				SparseMatrixFormat.BCOR => format,
				SparseMatrixFormat.BCOC => SparseMatrixFormat.BCOR,
				SparseMatrixFormat.CSC => SparseMatrixFormat.CSR,
				SparseMatrixFormat.CSR => format,
				SparseMatrixFormat.BSC => SparseMatrixFormat.CSR,
				SparseMatrixFormat.BSR => format,
				_ => throw new ArgumentOutOfRangeException(nameof(format), format, Resources.Parameter.InvalidValue),
			};
		}

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
			int c = f.PopCount();
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
			int c = f.PopCount();
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
