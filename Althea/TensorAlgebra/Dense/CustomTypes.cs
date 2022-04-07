using System.Runtime.CompilerServices;

using Althea.Array;
using Althea.Helpers;
using Althea.Linq;
using Althea.Storage;


namespace Althea.TensorAlgebra.Dense
{
	/// <summary>
	/// The computation wrapper for a (possibly pitched) dense tensor
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TS">The concrete storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
	public readonly ref struct DenseTensorWrapper<T, TS> where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>
	{
		#region basic
		private readonly TS m_values;

		private readonly ReadOnlySpan<long> m_size, m_outerSize, m_strides;

		private readonly UnaryOperation m_op;

		private readonly T m_scalar;

		/// <summary>
		/// Get the value array of this tensor as a <typeparamref name="TS"/>
		/// </summary>
		public readonly TS ValueStorage {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.m_values;
		}

		/// <summary>
		/// Get the presenting size of this tensor as a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/>
		/// </summary>
		public readonly ReadOnlySpan<long> Size {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.m_size;
		}

		/// <summary>
		/// Get the rank (number of dimensions) of this tensor as a <see cref="int"/>
		/// </summary>
		public readonly int Rank {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.m_size.Length;
		}

		/// <summary>
		/// Get the outer size (actual size of all dimensions) of this tensor as a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/>
		/// </summary>
		/// <remarks>If there is not pitch, <see cref="OuterSize"/> == <see cref="Size"/> (reference equals)</remarks>
		public readonly ReadOnlySpan<long> OuterSize {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.m_outerSize;
		}

		/// <summary>
		/// Get the strides between consecutive elements in each dimension of this tensor as a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/>
		/// </summary>
		/// <remarks>If there is not pitch, <see cref="Strides"/> is empty</remarks>
		public readonly ReadOnlySpan<long> Strides {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.m_outerSize;
		}

		/// <summary>
		/// Get the <see cref="UnaryOperation"/> which is about to be applied to this tensor if this wrapper is used as an input
		/// </summary>
		public readonly UnaryOperation Operation {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.m_op;
		}

		/// <summary>
		/// Get the scalar which is about to be applied to this tensor if this wrapper is used as an input. If this wrapper is a pure input while <see cref="Scalar"/> is 0, this wrapper shall be considered as a null input.
		/// </summary>
		public readonly T Scalar {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.m_scalar;
		}

		/// <summary>
		/// Check whether this wrapper is an invalid one or not
		/// </summary>
		/// <returns>The invalidness of this wrapper</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly bool IsInvalid() => this.m_values is null || !this.m_values.IsValid() || this.m_size.IsEmpty || this.m_outerSize.IsEmpty || this.m_strides.IsEmpty || this.m_size.Length != this.m_outerSize.Length;

		/// <summary>
		/// Check whether this wrapper is an invalid one or not when it is an input parameter
		/// </summary>
		/// <returns>The invalidness of this wrapper</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly bool IsInputInvalid() => this.m_scalar == T.Zero || this.IsInvalid();
		#endregion

		#region equality
		/// <summary>
		/// Check whether this <see cref="DenseTensorWrapper{T, TS}"/> is identical to the <paramref name="other"/> one
		/// </summary>
		/// <param name="other">The other <see cref="DenseTensorWrapper{T, TS}"/> to compare</param>
		/// <returns>this == <paramref name="other"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly bool Equals(DenseTensorWrapper<T, TS> other)
		{
			return this.m_values == other.m_values && this.m_size.SequenceEqual(other.m_size) && this.m_outerSize.SequenceEqual(other.m_outerSize) && this.m_op == other.m_op && this.m_scalar == other.m_scalar;
		}

		/// <summary>
		/// Check whether this <see cref="DenseTensorWrapper{T, TS}"/> has identical size (and outer size) as the <paramref name="other"/> one
		/// </summary>
		/// <param name="other">The other <see cref="DenseTensorWrapper{T, TS}"/> to compare</param>
		/// <returns>this == <paramref name="other"/> for sizes</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly bool SizeEquals(DenseTensorWrapper<T, TS> other)
		{
			return this.m_size.SequenceEqual(other.m_size) && this.m_outerSize.SequenceEqual(other.m_outerSize);
		}

		/// <summary>
		/// Equality operator
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(DenseTensorWrapper<T, TS> left, DenseTensorWrapper<T, TS> right)
		{
			return left.Equals(right);
		}

		/// <summary>
		/// Inequality operator
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(DenseTensorWrapper<T, TS> left, DenseTensorWrapper<T, TS> right)
		{
			return !left.Equals(right);
		}

		/// <summary>
		/// Always returns false since a ref struct cannot be boxed
		/// </summary>
		public override bool Equals(object? obj) => false;

		/// <summary>
		/// Always throws <see cref="InvalidOperationException"/> since a ref struct cannot be stored on heap
		/// </summary>
		public override int GetHashCode() => throw new InvalidOperationException();

		/// <summary>
		/// Get the string representation of this <see cref="DenseTensorWrapper{T, TS}"/>
		/// </summary>
		/// <returns>The string representation of this <see cref="DenseTensorWrapper{T, TS}"/></returns>
		public override string ToString()
		{
			return nameof(DenseTensorWrapper<T, TS>) + $"[ValueStorage={this.m_values}, Size={this.m_size.SpanJoin('x')}" + (this.m_size == this.m_outerSize ? "]" : $"OuterSize={this.m_outerSize.SpanJoin('x')}]");
		}
		#endregion

		#region create
		/// <summary>
		/// Create a new <see cref="DenseTensorWrapper{T, TS}"/> with all given parameters and scalar set to 1 and assuming that there is not pitch.
		/// </summary>
		/// <param name="value">The given dense storage</param>
		/// <param name="size">The presenting size / extent of all dimensions</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public DenseTensorWrapper(TS value, ReadOnlySpan<long> size) : this(value, size, size, default, UnaryOperation.Identity, T.One) { }

		/// <summary>
		/// Create a new <see cref="DenseTensorWrapper{T, TS}"/> with all given parameters.
		/// </summary>
		/// <param name="value">The given dense storage</param>
		/// <param name="size">The presenting size / extent of all dimensions</param>
		/// <param name="operation">The <see cref="UnaryOperation"/> which is about to be applied to this wrapper if it is used as an input</param>
		/// <param name="scalar">The scalar which is about to be applied to this wrapper</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public DenseTensorWrapper(TS value, ReadOnlySpan<long> size, UnaryOperation operation, T scalar) : this(value, size, size, default, operation, scalar) { }

		/// <summary>
		/// Create a new <see cref="DenseTensorWrapper{T, TS}"/> with all given parameters and scalar set to 1.
		/// </summary>
		/// <param name="value">The given dense storage</param>
		/// <param name="size">The presenting size / extent of all dimensions</param>
		/// <param name="outerSize">The actual outer size, will be replaced by <paramref name="size"/> if <paramref name="size"/> sequence equals <paramref name="outerSize"/></param>
		/// <param name="strides">The strides between consecutive elements in each dimension, will be replaced by empty if <paramref name="size"/> sequence equals <paramref name="outerSize"/></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public DenseTensorWrapper(TS value, ReadOnlySpan<long> size, ReadOnlySpan<long> outerSize, ReadOnlySpan<long> strides) : this(value, size, outerSize, strides, UnaryOperation.Identity, T.One) { }

		/// <summary>
		/// Create a new <see cref="DenseTensorWrapper{T, TS}"/> with all parameters.
		/// </summary>
		/// <param name="value">The given dense storage</param>
		/// <param name="size">The presenting size / extent of all dimensions</param>
		/// <param name="outerSize">The actual outer size, will be replaced by <paramref name="size"/> if <paramref name="size"/> sequence equals <paramref name="outerSize"/></param>
		/// <param name="strides">The strides between consecutive elements in each dimension, will be replaced by empty if <paramref name="size"/> sequence equals <paramref name="outerSize"/></param>
		/// <param name="operation">The <see cref="UnaryOperation"/> which is about to be applied to this wrapper if it is used as an input</param>
		/// <param name="scalar">The scalar which is about to be applied to this wrapper if it is used as an input. 0 will <b>not</b> be replaced by 1.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public DenseTensorWrapper(TS value, ReadOnlySpan<long> size, ReadOnlySpan<long> outerSize, ReadOnlySpan<long> strides, UnaryOperation operation, T scalar)
		{
			this.m_values = value; this.m_size = size;
			if (outerSize.SequenceEqual(size))
			{
				outerSize = size;
				strides = default;
			}
			this.m_outerSize = outerSize;
			this.m_strides = strides;
			this.m_op = operation;
			this.m_scalar = scalar;
		}

		/// <summary>
		/// Create a new <see cref="DenseTensorWrapper{T, TS}"/> with a given dense <paramref name="tensor"/>.
		/// </summary>
		/// <param name="tensor">The given dense tensor as a <see cref="ILabeledTensor{T}"/></param>
		/// <param name="operation">The <see cref="UnaryOperation"/> which is about to be applied to this wrapper if it is used as an input</param>
		/// <param name="scalar">The scalar which is about to be applied to this wrapper if it is used as an input. Default null means 1.</param>
		/// <exception cref="ArgumentException">If <paramref name="tensor"/> is a <see cref="ISparseArray{T}"/></exception>
		/// <exception cref="ArgumentNullException">If <paramref name="tensor"/> is not a <see cref="IDenseArray{T, TS}"/></exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public DenseTensorWrapper(ILabeledTensor<T>? tensor, UnaryOperation operation = UnaryOperation.Identity, T? scalar = null)
		{
			if (tensor is null)
			{
				this = default; this.m_values = TS.Empty;
				return;
			}
			if (tensor is not IDenseArray<T, TS> dense)
				throw new ArgumentException(Resources.ParameterError.UnexpectedType, nameof(tensor));
			if (tensor is ISparseArray<T>)
				throw new ArgumentException(Resources.ParameterError.UnexpectedType, nameof(tensor));

			ReadOnlySpan<long> outerSize, strides;
			if (dense.OuterSize.SequenceEqual(dense.Size))
			{
				outerSize = dense.Size; strides = default;
			}
			else
			{
				outerSize = dense.OuterSize; strides = dense.Strides;
			}
			this = new(dense.Storage, tensor.Size, outerSize, strides, operation, scalar ?? T.One);
		}

		/// <summary>
		/// Create a new <see cref="DenseTensorWrapper{T, TS}"/> with the given original <paramref name="tensor"/> and new <paramref name="operation"/> and <paramref name="scalar"/>.
		/// </summary>
		/// <param name="tensor">The original <see cref="DenseTensorWrapper{T, TS}"/></param>
		/// <param name="operation">The new <see cref="UnaryOperation"/></param>
		/// <param name="scalar">The new scalar</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public DenseTensorWrapper(DenseTensorWrapper<T, TS> tensor, UnaryOperation operation, T scalar)
		{
			this.m_values = tensor.m_values;
			this.m_size = tensor.m_size;
			this.m_outerSize = tensor.m_outerSize;
			this.m_strides = tensor.m_strides;
			this.m_op = operation;
			this.m_scalar = scalar;
		}

		/// <summary>
		/// Implicitly convert a <see cref="DenseArrayWrapper{T, TS}"/> to a <see cref="DenseTensorWrapper{T, TS}"/>.
		/// </summary>
		public static implicit operator DenseTensorWrapper<T, TS>(DenseArrayWrapper<T, TS> array) => new(array.ValueStorage, array.Size, array.OuterSize, array.Strides);
		#endregion
	}
}
