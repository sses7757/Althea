using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Althea.Arrays;
using Althea.Helpers;
using Althea.NativeTypes;

namespace Althea.TensorAlgebra.Sparse
{
	#region enum
	/// <summary>
	/// The <see cref="SparseTensorFormat"/> enum indicates the format specification of a sparse tensor. Each bit flag indicates an atomic format.
	/// </summary>
	[Flags]
	public enum SparseTensorFormat : int
	{
		/// <summary>
		/// The simple Coordinate Sparse Format that stores each non-zero element and its <b>zero-based</b> index in separate storages.
		/// </summary>
		Coordinated = 1 << 0,
		/// <summary>
		/// The Block Coordinated Sparse Format that divides the tensor into block tensors of fixed size and store their <b>zero-based</b> indices in a storage.<br/>
		/// This is similar to the <see cref="LinearAlgebra.Sparse.SparseMatrixFormat.BCOC"/>.
		/// </summary>
		BlockCoordinated = 1 << 1,
		/// <summary>
		/// The Variable Block Sparse Format that only differs from <see cref="BlockCoordinated"/> by letting the block tensors have variable sizes while the alignments are still necessary.<br/>
		/// Therefore the implementation shall contains an extra (size == rank) array whose elements are aligned lengths (or accumulated ones) of that rank.
		/// </summary>
		VariableBlockCoordinated = 1 << 2,
		/////// <summary>
		/////// The Abelian Block Sparse Format that labels block tensors with 'charges' within a certain Abelian group with a fixed summation.<br/>
		/////// This only differs from <see cref="VariableBlockCoordinated"/> by adding charge constraints.
		/////// </summary>
		////Abelian = 1 << 3,
	}
	#endregion

	#region wrapper
	/// <summary>
	/// The computation wrapper for a sparse tensor
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
	public readonly ref struct SparseTensorWrapper<T> where T : unmanaged
	{
		private readonly Storage<T> m_values;

		private readonly IReadOnlyList<IStorage> m_indexArrays;

		private readonly ReadOnlySpan<long> m_size;

		private readonly DataType m_indexType;

		private readonly SparseTensorFormat m_format;

		private readonly UnaryOperation m_op;

		private readonly T m_default, m_scalar;

		/// <summary>
		/// Get the value array storage of this sparse tensor wrapper
		/// </summary>
		public Storage<T> ValueStorage {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.m_values;
		}

		/// <summary>
		/// Get the index arrays' storages of this sparse tensor wrapper as a list of <see cref="IStorage"/>
		/// </summary>
		public IReadOnlyList<IStorage> IndexArrays {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.m_indexArrays;
		}

		/// <summary>
		/// Get the (major) data type of the index array(s) of this sparse tensor wrapper as a <see cref="DataType"/>
		/// </summary>
		public DataType IndexType {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.m_indexType;
		}

		/// <summary>
		/// When implemented by a derived class, get or set the default value of this sparse tensor wrapper as a <typeparamref name="T"/>
		/// </summary>
		public T DefaultValue {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.m_default;
		}

		/// <summary>
		/// Get the format of this sparse tensor wrapper as a <see cref="SparseTensorFormat"/>
		/// </summary>
		public SparseTensorFormat Format {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.m_format;
		}

		/// <summary>
		/// Get the presenting size of this tensor as a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/>
		/// </summary>
		public ReadOnlySpan<long> Size {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.m_size;
		}

		/// <summary>
		/// Get the <see cref="UnaryOperation"/> which is about to be applied to this tensor if this wrapper is used as an input
		/// </summary>
		public UnaryOperation Operation {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.m_op;
		}

		/// <summary>
		/// Get the scalar which is about to be applied to this tensor if this wrapper is used as an input. If this wrapper is a pure input while <see cref="Scalar"/> is 0, this wrapper shall be considered as a null input.
		/// </summary>
		public T Scalar {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.m_scalar;
		}

		/// <summary>
		/// Check whether this wrapper is an invalid one or not
		/// </summary>
		/// <returns>The invalidness of this wrapper</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool IsInvalid() => this.m_values is null || this.m_indexArrays is null || this.m_size.IsEmpty || this.m_indexArrays.Count == 0 || !this.m_values.IsValid();

		/// <summary>
		/// Check whether this wrapper is an invalid one or not when it is an input parameter
		/// </summary>
		/// <returns>The invalidness of this wrapper</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool IsInputInvalid() => this.m_scalar.IsZero() || this.IsInvalid();

		/// <summary>
		/// Create a new <see cref="SparseTensorWrapper{T}"/> with all given parameters and scalar set to 1
		/// </summary>
		/// <param name="tensor">The given sparse tensor</param>
		/// <param name="operation">The <see cref="UnaryOperation"/> which is about to be applied to this wrapper if it is used as an input</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SparseTensorWrapper(ISparseTensor<T> tensor, UnaryOperation operation = UnaryOperation.Identity)
		{
			this = new(tensor, Const<T>.One, operation);
		}

		/// <summary>
		/// Create a new <see cref="SparseTensorWrapper{T}"/> with all given parameters
		/// </summary>
		/// <param name="value">The given sparse tensor</param>
		/// <param name="operation">The <see cref="UnaryOperation"/> which is about to be applied to this wrapper if it is used as an input</param>
		/// <param name="scalar">The scalar which is about to be applied to this wrapper if it is used as an input. 0 will <b>not</b> be replaced by 1.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SparseTensorWrapper(ISparseTensor<T> value, T scalar, UnaryOperation operation = UnaryOperation.Identity)
		{
			this.m_values = value.Storage;
			this.m_indexArrays = value;
			this.m_format = value.Format;
			this.m_default = value.DefaultValue;
			this.m_indexType = value.IndexType;
			this.m_size = value.Size;
			this.m_op = operation;
			this.m_scalar = scalar;
		}
	}
	#endregion

	#region extension methods
	/// <summary>
	/// The static class for extension methods of <see cref="SparseTensorFormat"/>
	/// </summary>
	public static class FormatExtension
	{
		#region constants
		/// <summary>
		/// All of the possible <see cref="SparseTensorFormat"/>s. Value = 111...111
		/// </summary>
		/// <remarks>Since the atom formats are all orthogonal in binary, this kind of definition becomes useful.<br/>
		/// If some custom formats are defined afterwards, this trick should still be used.</remarks>
		public const SparseTensorFormat Any = (SparseTensorFormat)~0;
		#endregion

		#region methods
		/// <summary>
		/// Check whether the given <see cref="SparseTensorFormat"/> is an atomic format or not
		/// </summary>
		/// <param name="format">The given <see cref="SparseTensorFormat"/> to check</param>
		/// <returns>True if <paramref name="format"/> is an atomic format, i.e. a power of two; false otherwise.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsAtomic(this SparseTensorFormat format) => ((int)format).IsPowerOfTwo();

		/// <summary>
		/// Decompose the given <see cref="SparseTensorFormat"/> into atomic <paramref name="result"/>s.
		/// </summary>
		/// <param name="format">The given <see cref="SparseTensorFormat"/> to be decomposed</param>
		/// <param name="result">The <see cref="Span{T}"/> to put the results</param>
		/// <returns>The sliced <paramref name="result"/> whose length is the number of atomic formats</returns>
		/// <exception cref="ArgumentException">If the length of <paramref name="result"/> is less than the number of atomic formats</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Span<SparseTensorFormat> Decompose(this SparseTensorFormat format, Span<SparseTensorFormat> result)
		{
			if (format == 0)
				return Span<SparseTensorFormat>.Empty;
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
					result[c++] = (SparseTensorFormat)(1 << i);
				}
			}
			return result;
		}
		#endregion
	}
	#endregion
}
