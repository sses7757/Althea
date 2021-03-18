using System;
using System.Runtime.CompilerServices;

using Althea.Arrays;
using Althea.NativeTypes;
using Althea.Linq;


namespace Althea.TensorAlgebra.Dense
{
	#region wrapper
	/// <summary>
	/// The wrapper for a (possibly pitched) dense tensor
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
	public readonly ref struct DenseTensorWrapper<T> where T : unmanaged
	{
		private readonly Storage<T> m_values;

		private readonly ReadOnlySpan<long> m_size, m_outerSize;

		private readonly UnaryOperation m_op;

		private readonly T m_scalar;

		/// <summary>
		/// The value array of this tensor as a <see cref="Storage{T}"/> of <typeparamref name="T"/>
		/// </summary>
		public Storage<T> ValueStorage {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.m_values;
		}

		/// <summary>
		/// The presenting size of this tensor as a <see cref="ReadOnlySpan{T}"/> of <typeparamref name="T"/>
		/// </summary>
		public ReadOnlySpan<long> Size {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.m_size;
		}

		/// <summary>
		/// The outer size (actual size of all dimensions) of this tensor as a <see cref="ReadOnlySpan{T}"/> of <typeparamref name="T"/>
		/// </summary>
		/// <remarks>If there is not pitch, <see cref="OuterSize"/> == <see cref="Size"/> (reference equals)</remarks>
		public ReadOnlySpan<long> OuterSize {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.m_outerSize;
		}

		/// <summary>
		/// The <see cref="UnaryOperation"/> which is about to be applied to this tensor if this wrapper is used as an input
		/// </summary>
		public UnaryOperation Operation {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.m_op;
		}

		/// <summary>
		/// The scalar which is about to be applied to this tensor if this wrapper is used as an input. If this wrapper is a pure input while <see cref="Scalar"/> is 0, this wrapper shall be considered as a null input.
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
		public bool IsInvalid() => this.m_values is null || !this.m_values.IsValid() || this.m_size.IsEmpty || this.m_outerSize.IsEmpty || this.m_size.Length != this.m_outerSize.Length;

		/// <summary>
		/// Check whether this wrapper is an invalid one or not when it is an input parameter
		/// </summary>
		/// <returns>The invalidness of this wrapper</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool IsInputInvalid() => this.m_scalar.IsZero() || this.IsInvalid();

		/// <summary>
		/// Create a new <see cref="DenseTensorWrapper{T}"/> with all given parameters and scalar set to 1
		/// </summary>
		/// <param name="value">The given dense storage</param>
		/// <param name="size">The presenting size</param>
		/// <param name="outerSize">The actual outer size</param>
		/// <param name="operation">The <see cref="UnaryOperation"/> which is about to be applied to this wrapper if it is used as an input</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public DenseTensorWrapper(Storage<T> value, ReadOnlySpan<long> size, ReadOnlySpan<long> outerSize, UnaryOperation operation = UnaryOperation.Identity)
		{
			this.m_values = value; this.m_size = size;
			if (outerSize.SequenceEqual(size))
				outerSize = size;
			this.m_outerSize = outerSize;
			this.m_op = operation;
			this.m_scalar = Const<T>.One;
		}

		/// <summary>
		/// Create a new <see cref="DenseTensorWrapper{T}"/> with all given parameters
		/// </summary>
		/// <param name="value">The given dense storage</param>
		/// <param name="size">The presenting size</param>
		/// <param name="outerSize">The actual outer size</param>
		/// <param name="operation">The <see cref="UnaryOperation"/> which is about to be applied to this wrapper if it is used as an input</param>
		/// <param name="scalar">The scalar which is about to be applied to this wrapper if it is used as an input. 0 will <b>not</b> be replaced by 1.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public DenseTensorWrapper(Storage<T> value, ReadOnlySpan<long> size, ReadOnlySpan<long> outerSize, T scalar, UnaryOperation operation = UnaryOperation.Identity)
		{
			this.m_values = value; this.m_size = size;
			if (outerSize.SequenceEqual(size))
				outerSize = size;
			this.m_outerSize = outerSize;
			this.m_op = operation;
			this.m_scalar = scalar;
		}

		/// <summary>
		/// Create a new <see cref="DenseTensorWrapper{T}"/> with a given dense <paramref name="tensor"/>
		/// </summary>
		/// <param name="tensor">The given dense <see cref="TensorBase{T}"/></param>
		/// <param name="operation">The <see cref="UnaryOperation"/> which is about to be applied to this wrapper if it is used as an input</param>
		/// <param name="scalar">The scalar which is about to be applied to this wrapper if it is used as an input. Default 0 will be replaced by 1.</param>
		/// <exception cref="ArgumentException">If <paramref name="tensor"/> is a <see cref="ISparseArray{T}"/></exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public DenseTensorWrapper(TensorBase<T>? tensor, UnaryOperation operation = UnaryOperation.Identity, T scalar = default)
		{
			if (tensor is null)
			{
				this = default; this.m_values = Storage<T>.Empty;
				return;
			}
			if (tensor is ISparseArray<T>)
				throw new ArgumentException(Resources.Parameter.UnexpectedType, nameof(tensor));

			var outerSize = tensor is IPitchedArray<T> p ? p.OuterSize : tensor.Size;
			if (scalar.IsZero())
				scalar = Const<T>.One;
			this = new(tensor.Storage, tensor.Size, outerSize, scalar, operation);
		}
	}
	#endregion
}
