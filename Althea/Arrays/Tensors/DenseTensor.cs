using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

using Althea.Linq;
using Althea.Storage;

using RT = Althea.Runtime.API;
using BLAS = Althea.Blas.API;
using TENSOR = Althea.Tensor.API;


namespace Althea.Arrays
{
	/// <summary>
	/// The general dense tensor class that inherit the <see cref="ValueArray{T}"/> and implements <see cref="IDenseArray{T}"/>.
	/// </summary>
	/// <typeparam name="T">the supported data types are <see cref="float"/>, <see cref="double"/>, <see cref="FloatComplex"/>, <see cref="DoubleComplex"/>; other types of data causes <see cref="NotSupportedException"/></typeparam>
	public sealed class DenseTensor<T> : ValueArray<T>, ITensor<DenseTensor<T>, T>, ITensorAsMatrix<DenseTensor<T>, T>, IDenseArray<T>
		where T : struct, IComparable<T>
	{
		#region new members
		/// <summary>
		/// Rank of the multidimensional array
		/// </summary>
		public int Rank => Size.Count;

		/// <summary>
		/// The actual length of <see cref="DenseTensor{T}"/> is its <see cref="AbstractArray{T}.Length"/>
		/// </summary>
		public override long ActualLength => this.Length;

		private IReadOnlyList<char> _label;

		/// <summary>
		/// The label to mark each index of this tensor
		/// </summary>
		public IReadOnlyList<char> Label {
			get {
				if (this._label is null)
				{   // lazy initialization
					this._label = ArrayLinq.Range('a', this.Rank).ToArray();
				}
				return this._label;
			}
			set {
				if (value is null)
					throw new ArgumentNullException(nameof(value));
				if (value.Count != this.Rank)
					throw new ArgumentException(Resource.TensorWrongSize, nameof(value));
				if (value.Distinct().Count != value.Count)
					throw new ArgumentException(Resource.DuplicateLabels, nameof(value));
				this._label = value;
			}
		}

		/// <summary>
		/// The last index of this tensor as a vector
		/// </summary>
		public long LastIndex => this.Length - 1;

		/// <summary>
		/// Set the label to mark each index of this tensor
		/// </summary>
		/// <param name="label">label to set</param>
		public void SetLabel(params char[] label) => this.Label = label;
		#endregion


		#region initialize and destroy
		/// <summary>
		/// Empty tensor constructor
		/// </summary>
		public DenseTensor() : this(new[] { 0L }, onHost: false) { }

		/// <summary>
		/// Create a new tensor
		/// </summary>
		/// <param name="size">size array</param>
		/// <param name="onHost">allocate on host or device memory</param>
		public DenseTensor(int[] size, bool onHost = false) : this(Array.ConvertAll(size, s => (long)s), label: null, onHost: onHost) { }

		private static long[] ToLongSize(ITuple size)
		{
			if (size is null || size.Length == 0)
				throw new ArgumentNullException(nameof(size));
			long[] output = new long[size.Length];
			for (int i = 0; i < size.Length; i++)
			{
				if (size[i] is int si && si > 0)
					output[i] = si;
				else if (size[i] is long sl && sl > 0)
					output[i] = sl;
				else
					throw new ArgumentException(Resource.TensorWrongSize);
			}
			return output;
		}

		/// <summary>
		/// Create a new tensor
		/// </summary>
		/// <param name="size">size tuple</param>
		/// <param name="onHost">allocate on host or device memory</param>
		public DenseTensor(ITuple size, bool onHost = false) : this(ToLongSize(size), label: null, onHost: onHost) { }

		/// <summary>
		/// Create a new tensor
		/// </summary>
		/// <param name="size">size array</param>
		/// <param name="label">the label</param>
		/// <param name="onHost">allocate on host or device memory</param>
		public DenseTensor(IReadOnlyList<long> size, IReadOnlyList<char> label = null, bool onHost = false) : base(size.Prod(), size, onHost)
		{
			if (size is null || size.Count == 0)
				throw new ArgumentNullException(nameof(size));
			if (!(label is null || label.Count != size.Count))
				this.Label = label;
		}

		/// <summary>
		/// Create a new tensor with pre-allocated pointer
		/// </summary>
		/// <param name="size">size array</param>
		/// <param name="pointer">the pre-allocated data pointer</param>
		public DenseTensor(Storage<T> pointer, IReadOnlyList<long> size) : base(pointer, size)
		{
			if (size is null || size.Count == 0)
				throw new ArgumentNullException(nameof(size));
		}

		/// <summary>
		/// Reference initializer
		/// </summary>
		/// <param name="refArray">the reference array</param>
		/// <param name="newSize">new size of this tensor</param>
		/// <param name="offset">offset to <paramref name="refArray"/>'s <see cref="ValueArray{T}.Storage"/></param>
		public DenseTensor(ValueArray<T> refArray, long[] newSize, long offset = 0) : base(refArray, newSize.Prod(), newSize, offset)
		{
			if (newSize is null || newSize.Length == 0)
				throw new ArgumentNullException(nameof(newSize));
		}

		/// <summary>
		/// Clone initializer
		/// </summary>
		/// <param name="tensor"></param>
		public DenseTensor(DenseTensor<T> tensor) : base(tensor is null ? throw new ArgumentNullException(nameof(tensor)) : tensor.Length, tensor.Size, tensor.OnHost)
		{
			try
			{
				RT.CopyTo(source: tensor, dest: this, length: tensor.Length);
			}
			catch (Exception)
			{
				this.Dispose();
				throw;
			}
		}
		#endregion


		#region dense array interface
		/// <summary>
		/// Convert the values of this tensor to a C# array.
		/// </summary>
		/// <param name="ranges">the range with max value = length of this vector, default is all</param>
		/// <returns>C# array of type <typeparamref name="T"/> containing the values of this vector</returns>
		/// <remarks>currently, only a all <see cref="Range.All"/> <paramref name="ranges"/> is supported</remarks>
		public T[] ToFortranOrderArray(params Range[] ranges)
		{
			if (ranges is null || ranges.Length == 0)
				ranges = ArrayLinq.Repeat(Range.All, this.Rank).ToArray();
			if (ranges.Length != this.Rank)
				throw new ArgumentException(Resource.VectorWrongSize, nameof(ranges));
			if (!ranges.All(r => r.Equals(Range.All)))
				throw new NotSupportedException();

			return RT.CopyOutArray(this, length: this.Length, offset: 0);
		}

		/// <summary>
		/// Copy the <paramref name="values"/> into this dense tensor.
		/// </summary>
		/// <param name="values">the value array of element type <typeparamref name="T"/></param>
		/// <param name="ranges">the ranges of each dimension, default is all</param>
		/// <remarks>currently, only a all <see cref="Range.All"/> <paramref name="ranges"/> is supported</remarks>
		public void FromFortranOrderArray(T[] values, params Range[] ranges)
		{
			if (values is null)
				throw new ArgumentNullException(nameof(values));
			if (ranges is null || ranges.Length == 0)
				ranges = ArrayLinq.Repeat(Range.All, this.Rank).ToArray();
			if (ranges.Length != this.Rank)
				throw new ArgumentException(Resource.VectorWrongSize, nameof(ranges));
			if (!ranges.All(r => r.Equals(Range.All)))
				throw new NotSupportedException();

			RT.CopyIntoArray(this, values, length: this.Length, offset: 0);
		}
		#endregion


		#region Lanczos interface
		/// <summary>
		/// Replace this tensor's content with <paramref name="another"/> <b>in-place</b>.
		/// </summary>
		/// <param name="another">another <see cref="DenseTensor{T}"/> to replace from</param>
		public void ReplaceBy(DenseTensor<T> another)
		{
			if (another is null || another == EmptyDnTen)
				throw new ArgumentNullException(nameof(another), Resource.ArrayCannotNull);
			if (!this.Size.SequenceEqual(another.Size))
				throw new ArgumentException(Resource.TensorWrongSize, nameof(another));
			RT.CopyTo(source: another, dest: this);
		}

		/// <summary>
		/// Vector inner product, compute $\vec{v}_{\text{this}} \cdot \vec{v}_{\text{other}} \equiv \vec{v}_{\text{this}}^H (\text{or }\vec{v}_{\text{this}}^H) \vec{v}_{\text{other}}$.
		/// </summary>
		/// <param name="other">the other tensor</param>
		/// <param name="conjugateThis">perform non- or conjugate transpose to this vector</param>
		/// <returns>The inner product result</returns>
		public T Dot(DenseTensor<T> other, bool? conjugateThis = null)
		{
			if (other is null || other == EmptyDnTen)
				throw new ArgumentNullException(nameof(other), Resource.ArrayCannotNull);
			if (!this.Size.SequenceEqual(other.Size))
				throw new ArgumentException(Resource.TensorWrongSize, nameof(other));
			return BLAS.VectorDot(this, other, conjugateThis);
		}

		/// <summary>
		/// Compute $\vec{v}_{\text{this}} = \vec{v}_{\text{this}} + \alpha \vec{x}$.
		/// </summary>
		/// <param name="x">vector</param>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		public void AddBy_αx(DenseTensor<T> x, T α)
		{
			if (x is null || x == EmptyDnTen)
				throw new ArgumentNullException(nameof(x), Resource.ArrayCannotNull);
			if (!this.Size.SequenceEqual(x.Size))
				throw new ArgumentException(Resource.TensorWrongSize, nameof(x));
			BLAS.VectorAddBy(this, x, α);
		}

		/// <summary>
		/// Operate the matrix whose columns are <paramref name="notJoinedVecs"/> onto a C# array to get a result tensor.
		/// </summary>
		/// <param name="notJoinedVecs">the columns of the matrix to operate</param>
		/// <param name="input">the input C# array to be operated</param>
		/// <returns><c>[<paramref name="notJoinedVecs"/>] * <paramref name="input"/></c> as tensor.</returns>
		/// <remarks>this method is actually static</remarks>
		public DenseTensor<T> OperateOn(IReadOnlyList<DenseTensor<T>> notJoinedVecs, T[] input)
		{
			if (input is null || input.Length == 0)
				throw new ArgumentNullException(nameof(input));
			if (notJoinedVecs is null || notJoinedVecs.Count != input.Length)
				throw new ArgumentNullException(nameof(notJoinedVecs), Resource.ArrayCannotNull);
			if (notJoinedVecs.Any(v => !v.Size.SequenceEqual(this.Size)))
				throw new ArgumentException(Resource.TensorWrongSize, nameof(notJoinedVecs));

			// sort first to reduce errors
			var sort = input.Select((x, i) => new KeyValuePair<int, T>(i, x)).OrderBy(x => x.Value).Select(x => x.Key);

			var tensor = this.NewArrayAlike() as DenseTensor<T>;
			try
			{
				tensor.FillWithZeros();
				foreach (var i in sort)
				{
					var dnten = notJoinedVecs[i];
					if (dnten is null || !dnten.Size.SequenceEqual(this.Size))
						throw new ArgumentException(Resource.VectorWrongSize, nameof(notJoinedVecs));
					if (dnten.OnHost != this.OnHost || dnten.Disposed)
						throw new ArgumentException(Resource.VectorWrongValue, nameof(notJoinedVecs));
					if (!input[i].IsZero())
						tensor.AddBy_αx(dnten, input[i]);
				}
				return tensor;
			}
			catch (Exception)
			{
				tensor.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Scale this vector in-place, i.e. $\vec{v}_{\text{this}} = \alpha \vec{v}_{\text{this}}$ <b>in-place</b>.
		/// </summary>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		public void Scale(T α)
		{
			BLAS.VectorScale(this, α);
		}

		/// <summary>
		/// 2-norm of this vector, i.e. $\|\vec{v}\| = \sqrt{\sum_i{\vec{v}_i^2}}$.
		/// </summary>
		/// <returns>The 2-norm of this vector.</returns>
		public double Norm()
		{
			return BLAS.VectorNorm(this);
		}

		/// <summary>
		/// Normalize this vector <b>in-place</b> to make it norm-one, i.e. $\vec{v} = \vec{v} / \|\vec{v}\|$.
		/// </summary>
		public void Normalize()
		{
			double norm = BLAS.VectorNorm(this);
			T scalar = (1 / norm).FromDouble<T>();
			BLAS.VectorScale(this, scalar);
		}
		#endregion


		#region implement converters
		private static DenseTensor<T> FromDense(ValueArray<T> m, long[] size)
		{
			if (m is null || m == EmptyDnTen)
				return null;
			var tensor = new DenseTensor<T>(m.Storage, size ?? m.Size);
			m.Disposed = true;
			GC.SuppressFinalize(m); // for performance
			return tensor;
		}

		/// <summary>
		/// Take out the data array as a new <see cref="DenseVector{T}"/>.
		/// </summary>
		/// <returns>A new <see cref="DenseVector{T}"/> containing the referenced data array of this one.</returns>
		public override DenseVector<T> AsDenseVector() => new DenseVector<T>(this, this.Length);

		/// <summary>
		/// Deep clone the array, the mutable status such as <see cref="Label"/> will also be copied.
		/// </summary>
		/// <returns>The cloned array</returns>
		public override object Clone()
		{
			var ten = new DenseTensor<T>(this)
			{
				Label = this.Label
			};
			return ten;
		}

		/// <summary>
		/// Create a new array like this one (with same type and other info) while the data type is <typeparamref name="TOut"/>
		/// </summary>
		/// <typeparam name="TOut">the new data type</typeparam>
		/// <returns>the new array</returns>
		public override ValueArray<TOut> NewArrayAlike<TOut>() => new DenseTensor<TOut>(this.Size, label: this.Label, onHost: this.OnHost);

		/// <summary>
		/// Create a new array with same properties as this one
		/// </summary>
		/// <returns>The array alike this one.</returns>
		public override AbstractArray<T> NewArrayAlike() => new DenseTensor<T>(this.Size, label: this.Label, onHost: this.OnHost);

		/// <summary>
		/// Convert this array to another memory.
		/// </summary>
		/// <returns>a new <see cref="ValueArray{T}"/> with same value as this one if this array is on host memory</returns>
		public override ValueArray<T> ToTheOtherMemory()
		{
			var newTensor = new DenseTensor<T>(this.Size, label: this.Label, onHost: !this.OnHost);
			try
			{
				RT.CopyTo(source: this, dest: newTensor);
				return newTensor;
			}
			catch (Exception)
			{
				newTensor.Dispose();
				throw;
			}
		}
		#endregion


		#region define operators
		/// <summary>
		/// Addition operator for two tensors.
		/// </summary>
		/// <param name="left">left operand, if <paramref name="left"/> is in-place, it will be overwritten</param>
		/// <param name="right">right operand</param>
		/// <returns>the addition result</returns>
		/// <remarks>the <see cref="Label"/> of operands will be ignored</remarks>
		public static DenseTensor<T> operator +(DenseTensor<T> left, DenseTensor<T> right)
		{
			if (left is null || left == EmptyDnTen)
				throw new ArgumentNullException(nameof(left), Resource.ArrayCannotNull);
			if (right is null || right == EmptyDnTen)
				throw new ArgumentNullException(nameof(left), Resource.ArrayCannotNull);
			if (left.OnHost != right.OnHost)
				throw new ArgumentException(Resource.RequireSamePos);
			if (!left.Size.SequenceEqual(right.Size))
				throw new ArgumentException(Resource.TensorWrongSize);

			return left.ApplyToClone(l =>
			{
				l.AddBy_αx(right, Scalars<T>.One);
				////l.PointwiseOperation(BinaryOperation.Add, Scalars<T>.One, UnitaryOperation.Identity, right, TensorOrder.Identity, Scalars<T>.One, C: left, orderC: TensorOrder.Identity);
			});
		}

		/// <summary>
		/// Subtraction operator for two tensors.
		/// </summary>
		/// <param name="left">left operand, if <paramref name="left"/> is in-place, it will be overwritten</param>
		/// <param name="right">right operand</param>
		/// <returns>the subtraction result</returns>
		/// <remarks>the <see cref="Label"/> of operands will be ignored</remarks>
		public static DenseTensor<T> operator -(DenseTensor<T> left, DenseTensor<T> right)
		{
			if (left is null || left == EmptyDnTen)
				throw new ArgumentNullException(nameof(left), Resource.ArrayCannotNull);
			if (right is null || right == EmptyDnTen)
				throw new ArgumentNullException(nameof(left), Resource.ArrayCannotNull);
			if (left.OnHost != right.OnHost)
				throw new ArgumentException(Resource.RequireSamePos);
			if (!left.Size.SequenceEqual(right.Size))
				throw new ArgumentException(Resource.TensorWrongSize);

			return left.ApplyToClone(l =>
			{
				l.AddBy_αx(right, Scalars<T>.MinusOne);
				////l.PointwiseOperation(BinaryOperation.Add, Scalars<T>.One, UnitaryOperation.Identity, right, TensorOrder.Identity, Scalars<T>.One, UnitaryOperation.Negate, left, TensorOrder.Identity);
			});
		}

		/// <summary>
		/// Negation operator for a tensor.
		/// </summary>
		/// <param name="left">left operand, if <paramref name="left"/> is in-place, it will be overwritten</param>
		/// <returns>the negation result</returns>
		/// <remarks>the <see cref="Label"/> of operand will be ignored</remarks>
		public static DenseTensor<T> operator -(DenseTensor<T> left)
		{
			if (left is null || left == EmptyDnTen)
				throw new ArgumentNullException(nameof(left), Resource.ArrayCannotNull);

			return left.ApplyToClone(result => result.Scale(Scalars<T>.MinusOne));
		}

		/// <summary>
		/// Scaling operator for a tensor.
		/// </summary>
		/// <param name="left">left operand, if <paramref name="left"/> is in-place, it will be overwritten</param>
		/// <param name="α">the scalar to multiply</param>
		/// <returns>the scaling result</returns>
		/// <remarks>the <see cref="Label"/> of operand will be ignored</remarks>
		public static DenseTensor<T> operator *(DenseTensor<T> left, T α)
		{
			if (left is null || left == EmptyDnTen)
				throw new ArgumentNullException(nameof(left), Resource.ArrayCannotNull);

			return left.ApplyToClone(result => result.Scale(α));
		}

		/// <summary>
		/// Scaling operator for a tensor.
		/// </summary>
		/// <param name="left">left operand, if <paramref name="left"/> is in-place, it will be overwritten</param>
		/// <param name="α">the scalar to multiply</param>
		/// <returns>the scaling result</returns>
		/// <remarks>the <see cref="Label"/> of operand will be ignored</remarks>
		public static DenseTensor<T> operator *(T α, DenseTensor<T> left) => left * α;


		

		/// <summary>
		/// Contraction operator for two tensors, <b>out-of-place</b>.
		/// </summary>
		/// <param name="left">left operand</param>
		/// <param name="right">right operand</param>
		/// <returns>the contraction result</returns>
		/// <remarks>the <see cref="Label"/> of operands will utilized</remarks>
		public static DenseTensor<T> operator *(DenseTensor<T> left, DenseTensor<T> right)
		{
			var (newLabel, newSize) = TENSOR.OutOfPlaceContractCheck(Scalars<T>.One, left, right, out _);

			// calculate
			var result = new DenseTensor<T>(newSize, label: newLabel, onHost: left.OnHost);
			try
			{
				result.Contract(Scalars<T>.One, left, right);
				return result;
			}
			catch (Exception)
			{
				result.Dispose();
				throw;
			}
		}
		#endregion

		#region interface operators
		#region explicit method
		DenseTensor<T> ITensor<DenseTensor<T>, T>.ConjugateOutOfPlace() => base.ConjugateOutOfPlace() as DenseTensor<T>;

		DenseTensor<T> ITensor<DenseTensor<T>, T>.Reshape(params long[] size)
		{
			if (size is null || size.Length == 0)
				throw new ArgumentNullException(nameof(size));
			if (size.Prod() != this.Length)
				throw new ArgumentOutOfRangeException(nameof(size));

			return new DenseTensor<T>(this, size);
		}
		#endregion

		/// <summary>
		/// Permute <paramref name="tensor"/> by <paramref name="order"/> and replace to this tensor
		/// </summary>
		/// <param name="tensor">the tensor to be permuted</param>
		/// <param name="order">the new permutation <see cref="TensorOrder"/>, zero-based</param>
		public void Permute(DenseTensor<T> tensor, TensorOrder order)
		{
			tensor.Permute(Scalars<T>.One, UnitaryOperation.Identity, order, overwrite: this);
		}

		/// <summary>
		/// Permute (general transpose) and this tensor to form a <b>new</b> one.
		/// </summary>
		/// <param name="order">the new permutation <see cref="TensorOrder"/>, zero-based</param>
		/// <returns>the result tensor, a new <see cref="DenseTensor{T}"/></returns>
		public DenseTensor<T> OperatorPermute(TensorOrder order)
		{
			return this.Permute(Scalars<T>.One, UnitaryOperation.Identity, order);
		}

		/// <summary>
		/// Contraction operator for two tensors: this as left and <paramref name="right"/>.
		/// </summary>
		/// <param name="right">right operand</param>
		/// <returns>the contraction result, out-of-place</returns>
		/// <param name="order">the order of the result tensor, default is the alphabetic order</param>
		/// <remarks>the <see cref="ITensor.Label"/> of operands will be utilized</remarks>
		public DenseTensor<T> OperatorContract(DenseTensor<T> right, params char[] order)
		{
			var (newLabel, newSize) = TENSOR.OutOfPlaceContractCheck(Scalars<T>.One, this, right, out _);
			if (order is null || order.Length == 0)
				order = newLabel;
			if (order.Length != newLabel.Length)
				throw new ArgumentException(Resource.TensorWrongIndex, nameof(order));

			// calculate the new size
			if (!order.SequenceEqual(newLabel))
			{
				Span<int> permute = stackalloc int[order.Length];
				bool success = newLabel.FindPermutationTo(order, permute);
				if (!success)
					throw new ArgumentException(Resource.TensorWrongIndex, nameof(order));
				newSize = newSize.ReOrder(permute);
			}
			// calculate
			var result = new DenseTensor<T>(newSize, label: order, onHost: this.OnHost);
			try
			{
				result.Contract(Scalars<T>.One, this, right);
				return result;
			}
			catch (Exception)
			{
				result.Dispose();
				throw;
			}
		}
		#endregion


		#region operations
		/// <summary>
		/// Return a reference <see cref="DenseTensor{T}"/> of this one with same properties
		/// </summary>
		/// <returns>A reference <see cref="DenseTensor{T}"/> of this one</returns>
		public DenseTensor<T> MakeReference() => new DenseTensor<T>(this, this.Size.ToArray());

		void ITensor<T>.DualInPlace() { /*do nothing*/ }

		/// <summary>
		/// Tensor product between this tensor and <paramref name="other"/> tensor to form a <b>new</b> tensor.
		/// </summary>
		/// <param name="other">the other <see cref="DenseTensor{T}"/> to product</param>
		/// <param name="α">the scalar to multiply</param>
		/// <param name="conjugateOther">perform conjugate to <paramref name="other"/> or not, default is true for complex and false otherwise</param>
		/// <param name="overwrite">the overwrite result <see cref="DenseTensor{T}"/></param>
		/// <returns>the result tensor, a new <see cref="DenseTensor{T}"/> if <paramref name="overwrite"/> does not meet conditions</returns>
		public DenseTensor<T> TensorProduct(DenseTensor<T> other, T α, bool? conjugateOther = null, DenseTensor<T> overwrite = null)
		{
			if (other is null || other == EmptyDnTen)
				throw new ArgumentNullException(nameof(other), Resource.ArrayCannotNull);
			if (this.OnHost != other.OnHost)
				throw new ArgumentException(Resource.RequireSamePos);
			DenseTensor<T> output = null;
			try
			{
				if (overwrite is null || !this.Size.Concat(other.Size).SequenceEqual(overwrite.Size))
					output = new DenseTensor<T>(this.Size.Concat(other.Size).ToArray(), label: null, onHost: this.OnHost);
				else if (overwrite != null && this.OnHost != overwrite.OnHost)
					throw new ArgumentException(Resource.RequireSamePos, nameof(overwrite));
				else
					output = overwrite;
				BLAS.VectorOuterProduct(this, other, overwrite.ToMatrix(this.Length) as DenseMatrix<T>, α, conjugateOther);
				return output;
			}
			catch (Exception)
			{
				if (output != overwrite) output?.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Permute (general transpose) and scale this tensor to form a <b>new</b> tensor: $A_{i_0,i_1,...,i_n} = \alpha \Psi(A_{\Pi(i_0,i_1,...,i_n)})$.
		/// </summary>
		/// <param name="newOrder">the new permutation <see cref="TensorOrder"/>, zero-based</param>
		/// <param name="α">the scalar to multiply</param>
		/// <param name="op">the <see cref="UnitaryOperation"/> <c>Ψ</c> to apply on each element before scaling</param>
		/// <param name="overwrite">the overwrite result <see cref="DenseTensor{T}"/></param>
		/// <returns>the result tensor, a new <see cref="DenseTensor{T}"/> if <paramref name="overwrite"/> does not meet conditions</returns>
		public DenseTensor<T> Permute(T α, UnitaryOperation op, TensorOrder newOrder, DenseTensor<T> overwrite = null)
		{
			DenseTensor<T> output = null;
			try
			{
				var order = newOrder.GetIntArrayOrder(this);
				var reorderSize = this.Size.ReOrder(order).ToArray();
				if (overwrite is null || !reorderSize.SequenceEqual(overwrite.Size))
					output = new DenseTensor<T>(reorderSize, label: newOrder.GetCharArrayOrder(this), onHost: this.OnHost);
				else if (overwrite != null && this.OnHost != overwrite.OnHost)
					throw new ArgumentException(Resource.RequireSamePos, nameof(overwrite));
				else
					output = overwrite;
				TENSOR.Permute(this, α, op, newOrder, output);
				return output;
			}
			catch (Exception)
			{
				if (output != overwrite) output?.Dispose();
				throw;
			}
		}
		
		/// <summary>
		/// Partial reduction of tensor <paramref name="A"/>: $D_{\Pi^C(i_0,i_1,...,i_n)} = \alpha \Phi(\Psi_A(A_{\Pi^A(i_0,i_1,...,i_n)})) + \beta \Psi_C(C_{\Pi^C(i_0,i_1,...,i_n)})$. The missing indices of <paramref name="A"/> compared to <paramref name="C"/> will be aggregated according to <paramref name="reduction"/>.
		/// </summary>
		/// <param name="reduction">the reduce <see cref="BinaryOperation"/> <c>Φ</c></param>
		/// <param name="α">scalar α</param>
		/// <param name="opA"><see cref="UnitaryOperation"/> <c>Ψ<sub>A</sub></c></param>
		/// <param name="A"><see cref="DenseTensor{T}"/> A</param>
		/// <param name="β">scalar β, default 0</param>
		/// <param name="opC"><see cref="UnitaryOperation"/> <c>Ψ<sub>C</sub></c>, default identity</param>
		/// <param name="C"><see cref="DenseTensor{T}"/> C, default null</param>
		/// <remarks>If <paramref name="C"/> is null, or <paramref name="β"/> is zero, this tensor itself will be used instead of <paramref name="C"/>.</remarks>
		public void Reduce(BinaryOperation reduction, T α, UnitaryOperation opA, DenseTensor<T> A, T β = default, UnitaryOperation opC = UnitaryOperation.Identity, DenseTensor<T> C = null)
		{
			TENSOR.Reduce(reduction, α, opA, A, β, opC, C, this);
		}

		/// <summary>
		/// Contract two tensors <paramref name="A"/> and <paramref name="B"/>: $\text{this}_{i_0,i_1,...,i_n} = \alpha \sum_{j_a = k_b}{A_{j_0,j_1,...,j_p} \cdot B_{k_0,k_1,...,k_q}} + \beta C_{i_0,i_1,...,i_n}$;
		/// </summary>
		/// <param name="α">scalar α</param>
		/// <param name="A"><see cref="DenseTensor{T}"/> A</param>
		/// <param name="B"><see cref="DenseTensor{T}"/> B</param>
		/// <param name="β">scalar β, default 0</param>
		/// <param name="C"><see cref="DenseTensor{T}"/> C, default null means this</param>
		/// <remarks>If <paramref name="C"/> is null, or <paramref name="β"/> is zero, this tensor itself will be used instead of <paramref name="C"/>.</remarks>
		public void Contract(T α, DenseTensor<T> A, DenseTensor<T> B, T β = default, DenseTensor<T> C = null)
		{
			TENSOR.Contract(α, A, B, β, C, this);
		}
		#endregion


		#region matrix operation and decompositions
		/// <summary>
		/// Multiply this tensor as a matrix with the <paramref name="right"/> tensor as another matrix.
		/// </summary>
		/// <param name="right">the other <see cref="DenseTensor{T}"/> as a matrix</param>
		/// <param name="partitionLeft">a <see cref="Index"/> to indicate the first <paramref name="partitionLeft"/> (exclude) indices of this tensor will be regarded as the row and others column</param>
		/// <param name="partitionRight">a <see cref="Index"/> to indicate the first <paramref name="partitionRight"/> (exclude) indices of tensor <paramref name="right"/> will be regarded as the row and others column</param>
		/// <param name="leftOp">the <see cref="MatrixOperation"/> to apply on this one</param>
		/// <param name="rightOp">the <see cref="MatrixOperation"/> to apply on <paramref name="right"/></param>
		/// <returns>the multiplication result, out-of-place</returns>
		public DenseTensor<T> OperatorMatrixMultiply(DenseTensor<T> right, Index partitionLeft, Index partitionRight, MatrixOperation leftOp = MatrixOperation.None, MatrixOperation rightOp = MatrixOperation.None)
		{
			if (this.OnHost != right.OnHost)
				throw new ArgumentException(Resource.RequireSamePos, nameof(right));
			int pl = (int)partitionLeft.GetPosition(this.Rank);
			int pr = (int)partitionRight.GetPosition(right.Rank);

			var (m, n) = (this.SizeProd[pl], this.Length / this.SizeProd[pl]);
			if (leftOp != MatrixOperation.None)
				(m, n) = (n, m);
			var (p, q) = (right.SizeProd[pr], right.Length / right.SizeProd[pr]);
			if (rightOp != MatrixOperation.None)
				(p, q) = (q, p);
			if (n != p)
				throw new ArgumentException(Resource.TensorWrongSize, nameof(right));
			var outSizeL = leftOp == MatrixOperation.None ? this.Size.Take(pl) : this.Size.TakeLast(this.Rank - pl);
			var outSizeR = rightOp != MatrixOperation.None ? right.Size.Take(pr) : right.Size.TakeLast(right.Rank - pr);

			var output = new DenseMatrix<T>(m, q, this.OnHost);
			try
			{
				using var l = this.ToMatrix(this.SizeProd[pl]) as DenseMatrix<T>;
				using var r = right.ToMatrix(right.SizeProd[pr]) as DenseMatrix<T>;
				output.Mulβ_AddBy_αAB(l, r, Scalars<T>.One, opA: leftOp, opB: rightOp);
				return FromDense(output, outSizeL.Concat(outSizeR).ToArray());
			}
			catch (Exception)
			{
				output.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Compute the singular value decomposition (SVD) of this tensor and corresponding the left and/or right singular vectors: $A = U S V^*$ <b>out-of-place</b>, where $A$ is this matrix.
		/// </summary>
		/// <param name="partition">a <see cref="Index"/> to indicate the first <paramref name="partition"/> (exclude) indices of this tensor will be regarded as the row and others column</param>
		/// <param name="calcU">calculate the left singular vectors or not, if false, the return <c>U</c> will be null</param>
		/// <param name="calcV">calculate the right singular vectors or not, if false, the return <c>Vct</c> will be null</param>
		/// <returns>the singular values as a <see cref="double"/> array and left, right singular vectors</returns>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		public (double[] S, DenseTensor<T> U, DenseTensor<T> Vct) SingularValues(Index partition, bool calcU = true, bool calcV = true)
		{
			int p = (int)partition.GetPosition(this.Rank);

			var leftSize = this.Size.Take(p); var rightSize = this.Size.TakeLast(this.Rank - p);
			long leftLength = this.SizeProd[p], rightLength = this.Length / leftLength;
			var middleSize = new[] { Math.Min(leftLength, rightLength) };
			var Usize = leftSize.Concat(middleSize).ToArray();
			var VctSize = middleSize.Concat(rightSize).ToArray();

			using var mat = this.ToMatrix(leftLength) as DenseMatrix<T>;
			var (S, U, Vct) = mat.SingularValues(null, null, null, calcU, calcV);
			using (S)
				return (Array.ConvertAll(S.ToFortranOrderArray(), s => s.ToDouble()), FromDense(U, Usize), FromDense(Vct, VctSize));
		}

		/// <summary>
		/// Compute the singular value decomposition (SVD) of this tensor and corresponding the left and/or right singular vectors: $A = U S V^*$ <b>out-of-place</b>, where $A$ is this matrix. Not necessarily sorted descending by singular values. Then truncate the singular values $S$ and vectors $U$, $V^*$ to preserve at most <paramref name="maxPreserve"/> entries.
		/// </summary>
		/// <param name="partition">a <see cref="Index"/> to indicate the first <paramref name="partition"/> (exclude) indices of this tensor will be regarded as the row and others column</param>
		/// <param name="maxPreserve">the maximum number of singular values and vectors to preserve, must be positive</param>
		/// <returns>The singular values and left, right singular vectors with at most <paramref name="maxPreserve"/> entries.</returns>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="partition"/> is out of range</exception>
		public (DenseTensor<T> S, DenseTensor<T> U, DenseTensor<T> Vct) SingularValuesTruncate(Index partition, int maxPreserve)
		{
			int p = (int)partition.GetPosition(this.Rank);

			var leftSize = this.Size.Take(p); var rightSize = this.Size.TakeLast(this.Rank - p);
			long leftLength = this.SizeProd[p], rightLength = this.Length / leftLength;
			var middleSize = new[] { Math.Min(Math.Min(leftLength, rightLength), maxPreserve) };
			var Usize = leftSize.Concat(middleSize).ToArray();
			var VctSize = middleSize.Concat(rightSize).ToArray();

			using var mat = this.ToMatrix(leftLength) as DenseMatrix<T>;
			var (S, U, Vct) = mat.SingularValues(null, null, null, calcU: true, calcV: true);
			if (maxPreserve >= leftLength || maxPreserve >= rightLength)
			{
				var returnS = new DenseTensor<T>(new[] { middleSize[0], middleSize[0] }, onHost: this.OnHost);
				try
				{
					(returnS.ToMatrix() as DenseMatrix<T>).SetDiag(0, S);
					return (returnS, FromDense(U, Usize), FromDense(Vct, VctSize));
				}
				catch (Exception)
				{
					returnS?.Dispose();
					throw;
				}
				finally
				{
					S.Dispose();
				}
			}
			// else
			using (S) using (U) using (Vct)
			{
				var arrayS = S.ToFortranOrderArray();
				var arrayU = U.GetColumns();
				var arrayV = Vct.GetRows();
				try
				{
					var combine = arrayU.Zip(arrayV).ToArray();
					Array.Sort(keys: arrayS, items: combine);
					arrayS = arrayS.Reverse().ToArray();
					combine = combine.Reverse().ToArray();
					arrayS = arrayS[..maxPreserve];
					combine = combine[..maxPreserve];
					// copy to return U
					var returnU = new DenseMatrix<T>(U.NRows, U.NCols, U.OnHost);
					returnU.FromColumnVectors(Array.ConvertAll(combine, c => c.First));
					// copy to return V
					using var returnV = new DenseMatrix<T>(Vct.NCols, Vct.NRows, Vct.OnHost);
					returnV.FromColumnVectors(Array.ConvertAll(combine, c => c.Second));
					var returnVct = returnV.Transpose();
					// copy to return S
					var returnS = new DenseTensor<T>(new[] { middleSize[0], middleSize[0] }, onHost: this.OnHost);
					using var vecS = new DenseVector<T>(arrayS.Length, S.OnHost);
					vecS.FromFortranOrderArray(arrayS);
					(returnS.ToMatrix() as DenseMatrix<T>).SetDiag(0, S);
					// return
					return (returnS, FromDense(returnU, Usize), FromDense(returnVct, VctSize));
				}
				finally
				{
					arrayU.ClearList();
					arrayV.ClearList();
				}
			}
		}

		/// <summary>
		/// QR factorize this tensor <b>out-of-place</b>.
		/// </summary>
		/// <param name="partition">a <see cref="Index"/> to indicate the first <paramref name="partition"/> (exclude) indices of this tensor will be regarded as the row and others column</param>
		/// <param name="full">perform full factorization or not</param>
		/// <returns>the Q matrix and R matrix</returns>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		public (DenseTensor<T> Q, DenseTensor<T> R) QR(Index partition, bool full = false)
		{
			int p = (int)partition.GetPosition(this.Rank);

			var leftSize = this.Size.Take(p); var rightSize = this.Size.TakeLast(this.Rank - p);
			long leftLength = this.SizeProd[p], rightLength = this.Length / leftLength;
			full = full && leftLength > rightLength; // for 'fat' matrices, full == economic
			var middleSize = new[] { full ? leftLength : Math.Min(leftLength, rightLength) };
			var Qsize = leftSize.Concat(middleSize).ToArray();
			var Rsize = middleSize.Concat(rightSize).ToArray();

			using var mat = this.ToMatrix(leftLength) as DenseMatrix<T>;
			var (Q, R) = mat.QR(full, null, null);
			return (FromDense(Q, Qsize), FromDense(R, Rsize));
		}

		/// <summary>
		/// (Conjugate) transpose this tensor <b>out-of-place</b>.
		/// </summary>
		/// <param name="partition">a <see cref="Index"/> to indicate the first <paramref name="partition"/> (exclude) indices of this tensor will be regarded as the row and others column</param>
		/// <param name="conjugate">conjugate or not, default null means true for complex type (<see cref="IComplex{T}"/>)</param>
		/// <returns>the (conjugate) transpose of this tensor with <c>Size = this.Size[<paramref name="partition"/>..] concatenate this.Size[..<paramref name="partition"/>]</c></returns>
		public DenseTensor<T> Transpose(Index partition, bool? conjugate = null)
		{
			int p = (int)partition.GetPosition(this.Rank);
			using var mat = this.ToMatrix(this.SizeProd[p]) as DenseMatrix<T>;
			var matTranspose = (conjugate ?? !default(T).ToDataType().IsReal()) ? mat.ConjugateTranspose() : mat.Transpose();
			return FromDense(matTranspose, this.Size.TakeLast(this.Rank - p).Concat(this.Size.Take(p)).ToArray());
		}

		/// <summary>
		/// Calculate the trace of this tensor as a matrix.
		/// </summary>
		/// <returns>the trace of this tensor as a matrix</returns>
		/// <exception cref="InvalidOperationException">if this tensor's shape is not a square matrix</exception>
		public T Trace()
		{
			if (this.Rank != 2 || this.Size[0] != this.Size[1])
				throw new InvalidOperationException();
			using var diag = ((DenseMatrix<T>)this.ToMatrix(this.Size[0])).GetDiag(0);
			return diag.Sum();
		}

		/// <summary>
		/// Shift all the eigenvalues of this tensor by adding <paramref name="shift"/> to each diagonal elements of this tensor as a matrix.
		/// </summary>
		/// <param name="shift">the shift value, if it is zero, no operation shall be performed</param>
		/// <exception cref="InvalidOperationException">if this tensor's shape is not a square matrix</exception>
		public void EigenvalueShift(T shift)
		{
			if (this.Rank != 2 || this.Size[0] != this.Size[1])
				throw new InvalidOperationException();
			// shortcut
			if (shift.CompareTo(Scalars<T>.Zero) == 0)
				return;
			using var ones = new DenseVector<T>(this.Size[0], this.OnHost);
			ones.FillWithOnes();
			BLAS.VectorAddBy(y: (DenseMatrix<T>)this.ToMatrix(this.Size[0]), x: ones, α: shift, strideY: (int)ones.Length + 1);
		}
		#endregion


		#region indexers
		/// <summary>
		/// Get or set an element in of this tensor like a vector.
		/// </summary>
		/// <param name="i">the index in memory</param>
		/// <returns>the value at index <paramref name="i"/></returns>
		public T this[Index i] {
			get {
				long ind = i.GetPosition(this.Length);
				return RT.CopyOut(this, offset: ind);
			}
			set {
				long ind = i.GetPosition(this.Length);
				RT.CopyInto(this, value, offset: ind);
			}
		}

		private long CheckIndexer(Index[] pos, int startRank = 0)
		{
			if (pos is null)
				throw new ArgumentNullException(nameof(pos));
			if (pos.Length != this.Rank - startRank)
				throw new ArgumentOutOfRangeException(nameof(pos));

			long offset = 0;
			for (int i = startRank, j = 0; i < this.Rank; i++, j++)
			{
				var off = pos[j].GetPosition(this.Size[i]);
				if (off >= this.Size[i])
					throw new ArgumentOutOfRangeException(nameof(pos));
				else
					offset += this.SizeProd[i] * off;
			}
			return offset;
		}

		private long[] GetPos(long offset, int atRank = 0)
		{
			long[] pos = new long[this.Rank - atRank];
			for (int i = this.Rank - 1, j = pos.Length - 1; i >= atRank; i--, j--)
			{
				pos[j] = offset / this.SizeProd[i];
				offset %= this.SizeProd[i];
			}
			return pos;
		}

		/// <summary>
		/// Get or set an element in of this tensor.
		/// </summary>
		/// <param name="pos">the indices of each rank</param>
		/// <returns>the value at <paramref name="pos"/></returns>
		public T this[params Index[] pos] {
			get {
				var offset = this.CheckIndexer(pos);
				return RT.CopyOut(this, offset);
			}
			set {
				var offset = this.CheckIndexer(pos);
				RT.CopyInto(this, value, offset);
			}
		}

		/// <summary>
		/// Get the sub tensor formed by the first N rank of this tensor.
		/// </summary>
		/// <param name="firstNRank">first N ranks to set or get</param>
		/// <param name="restPos">rest of the tensor's rank's position <see cref="Index"/></param>
		/// <returns>the sub <see cref="DenseTensor{T}"/> of the <paramref name="firstNRank"/> at <paramref name="restPos"/></returns>
		public DenseTensor<T> GetSpan(int firstNRank, params Index[] restPos)
		{
			var offset = this.CheckIndexer(restPos, firstNRank);
			return new DenseTensor<T>(this, this.Size.Take(firstNRank).ToArray(), offset);
		}

		/// <summary>
		/// Set the sub tensor formed by the first N rank of this tensor.
		/// </summary>
		/// <param name="value">the value to set</param>
		/// <param name="firstNRank">first N ranks to set or get</param>
		/// <param name="restPos">rest of the tensor's rank's position <see cref="Index"/></param>
		/// <returns>the sub <see cref="DenseTensor{T}"/> of the <paramref name="firstNRank"/> at <paramref name="restPos"/></returns>
		public void SetSpan(DenseTensor<T> value, int firstNRank, params Index[] restPos)
		{
			if (value is null || value == EmptyDnTen)
				throw new ArgumentNullException(nameof(value), Resource.ArrayCannotNull);
			if (!this.Size.Take(firstNRank).SequenceEqual(value.Size))
				throw new ArgumentException(Resource.TensorWrongSize, nameof(value));
			var offset = this.CheckIndexer(restPos, firstNRank);
			RT.CopyTo(source: value, dest: this, length: value.Length, offsetDest: offset);
		}
		#endregion


		#region print
		/// <summary>
		/// Override <see cref="ValueArray{T}.ToString()"/> to get the string representation of this array.
		/// </summary>
		/// <returns>String representation of this array</returns>
		public override string ToString()
		{
			return base.ToString(new Dictionary<string, object> { ["label"] = "{" + string.Join(',', this.Label) + "}" });
		}

		/// <summary>
		/// Print out the tensor as well as its elements.
		/// </summary>
		/// <param name="overrideSetting"><see cref="AbstractArray{T}.Print(IReadOnlyDictionary{PrintSetting, int})"/></param>
		/// <returns>String representation of this tensor</returns>
		public override string Print(IReadOnlyDictionary<PrintSetting, int> overrideSetting = null)
		{
			string description = this.ToString();
			if (this.Disposed)
				return description;
			description += ":" + Environment.NewLine;

			if (this.Rank == 1)
			{
				var vec = (this.ToVector() as DenseVector<T>).Print(overrideSetting);
				var vecStrs = vec.Split(Environment.NewLine, 2);
				return description + vecStrs[1];
			}
			else if (this.Rank == 2)
			{
				var mat = (this.ToMatrix(this.Size[0]) as DenseMatrix<T>).Print(overrideSetting);
				var matStrs = mat.Split(Environment.NewLine, 2);
				return description + matStrs[1];
			}
			// else
			int maxCount = (!(overrideSetting is null) && overrideSetting.ContainsKey(PrintSetting.ArrayLength)) ? overrideSetting[PrintSetting.ArrayLength] : GlobalSettings.PrintConfig[PrintSetting.ArrayLength];
			int count = 0;
			StringBuilder sb = new StringBuilder(description);
			for (long offset = 0; offset < this.Length; offset += this.SizeProd[2])
			{
				if (count >= maxCount)
				{
					sb.AppendLine($"... {this.Length / this.SizeProd[2] - count} more sub-tensors");
					break;
				}
				sb.AppendLine($"Tensor[.., .., {string.Join(", ", this.GetPos(offset, 2))}] =");
				var mat = new DenseMatrix<T>(this, this.Size[0], this.Size[1], offset: offset).Print(overrideSetting);
				var matStrs = mat.Split(Environment.NewLine, 2);
				matStrs[1] = matStrs[1].TrimEnd();
				sb.Append(matStrs[1]);
				if (!matStrs[1].EndsWith(Environment.NewLine))
					sb.AppendLine();
				count++;
			}
			return sb.ToString();
		}
		#endregion


		#region host converter
		/// <summary>
		/// Convert C# array, size and on-host to <see cref="DenseTensor{T}"/>.
		/// </summary>
		/// <param name="input">C# array of <typeparamref name="T"/> as value, <see cref="ITuple"/> as size and <see cref="bool"/> as on-host</param>
		public static explicit operator DenseTensor<T>((T[] value, ITuple size, bool onHost) input)
		{
			var (value, size, onHost) = input;
			var tensor = new DenseTensor<T>(size, onHost);
			try
			{
				if (tensor.Length != value.LongLength)
					throw new ArgumentException(Resource.TensorWrongSize, nameof(size));
				RT.CopyIntoArray(tensor, value);
				return tensor;
			}
			catch (Exception)
			{
				tensor.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Convert C# array, size and on-host to <see cref="DenseTensor{T}"/>.
		/// </summary>
		/// <param name="input">C# array of <typeparamref name="T"/> as value, <see cref="int"/> array as size and <see cref="bool"/> as on-host</param>
		public static explicit operator DenseTensor<T>((T[] value, int[] size, bool onHost) input)
		{
			var (value, size, onHost) = input;
			var tensor = new DenseTensor<T>(size, onHost);
			try
			{
				if (tensor.Length != value.LongLength)
					throw new ArgumentException(Resource.TensorWrongSize, nameof(size));
				RT.CopyIntoArray(tensor, value);
				return tensor;
			}
			catch (Exception)
			{
				tensor.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Convert C# array, size and on-host to <see cref="DenseTensor{T}"/>.
		/// </summary>
		/// <param name="input">C# array of <typeparamref name="T"/> as value, <see cref="IReadOnlyList{Int64}"/> as size and <see cref="bool"/> as on-host</param>
		public static explicit operator DenseTensor<T>((T[] value, IReadOnlyList<long> size, bool onHost) input)
		{
			var (value, size, onHost) = input;
			var tensor = new DenseTensor<T>(size, null, onHost);
			try
			{
				if (tensor.Length != value.LongLength)
					throw new ArgumentException(Resource.TensorWrongSize, nameof(size));
				RT.CopyIntoArray(tensor, value);
				return tensor;
			}
			catch (Exception)
			{
				tensor.Dispose();
				throw;
			}
		}
		#endregion


		#region serialize
		/// <summary>
		/// Get the pointers of this instance.
		/// </summary>
		/// <returns>the pointers</returns>
		public override IReadOnlyDictionary<string, IStorage> GetPointers() => DenseTensorFactory.GetPointers(this);

		/// <summary>
		/// Get other requisite informations for re-constructing this array.
		/// </summary>
		/// <returns>other requisite informations</returns>
		public override IReadOnlyDictionary<string, object> GetOtherInfo() => DenseTensorFactory.GetOtherInfo(this);
		#endregion
	}
}