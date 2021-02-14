using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Althea.Linq;
using Althea.NativeTypes;


namespace Althea.Arrays
{
	/// <summary>
	/// The abstract vector class with the only mutable <see cref="ValueArray{T}.Storage"/> that refers to the actual data storage. There may be more pointer(s) for different indices in a sparse vector that inherits <see cref="VectorBase{T}"/>, but they shall be immutable.
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct that implements <see cref="IFormattable"/> and <see cref="IEquatable{T}"/> as the data type</typeparam>
	public abstract class VectorBase<T> : ValueArray<T>, IReadOnlyList<T> where T : unmanaged, IFormattable, IEquatable<T>
	{
		#region initialize
		/// <summary>
		/// Construct a <see cref="VectorBase{T}"/> by preallocated <paramref name="values"/> and the given <paramref name="length"/>
		/// </summary>
		/// <param name="values">The preallocated <see cref="Storage{T}"/> of the value array</param>
		/// <param name="length">The size of the vector</param>
		protected VectorBase(Storage<T> values, long length) : base(values, stackalloc long[1].SetValue(length)) { }
		#endregion

		#region reshape
		/// <summary>
		/// Reshape this array to a vector. Returns this vector directly.
		/// </summary>
		/// <returns> Returns this vector directly.</returns>
		public override ValueArray<T> ToVector() => this;
		#endregion

		#region converter
		/// <summary>
		/// When implemented by a derived class, convert this vector to a <see cref="DenseVector{T}"/>.
		/// </summary>
		/// <returns>The new converted dense vector, or this array if this array is a <see cref="DenseVector{T}"/></returns>
		public abstract DenseVector<T> ToDense();
		#endregion

		#region operators
		/// <summary>
		/// Vector point-wise multiply (not vector inner product).
		/// </summary>
		/// <param name="left">left operand, will be overwritten if it is in-place</param>
		/// <param name="right">right operand</param>
		/// <returns>$$\vec{v}_1\circ\vec{v}_2 \equiv \{\vec{v}_1^i \vec{v}_2^i\}_i$$</returns>
		public static VectorBase<T> operator *(VectorBase<T> left, VectorBase<T> right)
		{
			if (left is null)
				throw new ArgumentNullException(nameof(left));
			if (right is null)
				throw new ArgumentNullException(nameof(right));
			if (left.OnHost != right.OnHost)
				throw new ArgumentException(Resource.RequireSamePos);

			return left.ApplyToClone(l => l.PointWiseMultiply(right));
		}

		/// <summary>
		/// Vector scaling -- number multiplication.
		/// </summary>
		/// <param name="v">vector, will be overwritten if it is in-place</param>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		/// <returns>$$\vec{v}_{\text{result}} = \alpha \vec{v}$$</returns>
		public static VectorBase<T> operator *(VectorBase<T> v, T α)
		{
			if (v is null)
				throw new ArgumentNullException(nameof(v), Resource.ArrayCannotNull);
			return v.ApplyToClone(c => BLAS.VectorScale(c, α));
		}


		/// <summary>
		/// Vector negation.
		/// </summary>
		/// <param name="v"></param>
		/// <returns>$$\vec{v}_{\text{result}=-\vec{v}$$</returns>
		public static VectorBase<T> operator -(VectorBase<T> v) => v * Scalars<T>.MinusOne;

		/// <summary>
		/// Vector scaling -- number multiplication.
		/// </summary>
		/// <param name="v">vector</param>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		/// <returns>$$\vec{v}_{\text{result}} = \alpha \vec{v}$$</returns>
		public static VectorBase<T> operator *(T α, VectorBase<T> v) => v * α;


		/// <summary>
		/// Vector scaling -- number division.
		/// </summary>
		/// <param name="v">vector</param>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		/// <returns>$$\vec{v}_{\text{result}} = \frac{1}{\alpha}\vec{v}$$</returns>
		public static VectorBase<T> operator /(VectorBase<T> v, T α) => v * α.GenericReciprocal();

		/// <summary>
		/// Vector point-wise division.
		/// </summary>
		/// <param name="left">left operand, will be overwritten if it is in-place</param>
		/// <param name="right">right operand</param>
		/// <returns>$$\vec{v}_1 ./ \vec{v}_2 \equiv \{\vec{v}_1^i / \vec{v}_2^i\}_i$$</returns>
		public static VectorBase<T> operator /(VectorBase<T> left, VectorBase<T> right)
		{
			if (left is null)
				throw new ArgumentNullException(nameof(left), Resource.ArrayCannotNull);
			if (right is null)
				throw new ArgumentNullException(nameof(right), Resource.ArrayCannotNull);
			if (left.OnHost != right.OnHost)
				throw new ArgumentException(Resource.RequireSamePos);

			return left.ApplyToClone(l => l.PointWiseDivide(right));
		}

		/// <summary>
		/// Left matrix right vector multiplication, <b>out-of-place</b>.
		/// </summary>
		/// <param name="v">vector</param>
		/// <param name="M">matrix</param>
		/// <returns>$$\vec{w} = M \vec{v}$$</returns>
		/// <remarks>Temporary vector other than the returned one might be created as well</remarks>
		public static VectorBase<T> operator *(MatrixBase<T> M, VectorBase<T> v)
		{
			if (v is null)
				throw new ArgumentNullException(nameof(v), Resource.ArrayCannotNull);
			if (M is null)
				throw new ArgumentNullException(nameof(M), Resource.ArrayCannotNull);
			if (v.OnHost != M.OnHost)
				throw new ArgumentException(Resource.RequireSamePos);
			var result = new DenseVector<T>(M.NRows, v.OnHost);
			try
			{
				result.Mulβ_AddBy_αopAx(M, v, Scalars<T>.One);
				return result;
			}
			catch (Exception)
			{
				result?.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Left vector right matrix multiplication, <b>out-of-place</b>.
		/// </summary>
		/// <param name="v">vector</param>
		/// <param name="M">matrix</param>
		/// <returns>$$\vec{w} = \vec{v}^H M$$</returns>
		public static VectorBase<T> operator *(VectorBase<T> v, MatrixBase<T> M)
		{
			if (v is null)
				throw new ArgumentNullException(nameof(v), Resource.ArrayCannotNull);
			if (M is null)
				throw new ArgumentNullException(nameof(M), Resource.ArrayCannotNull);
			if (v.OnHost != M.OnHost)
				throw new ArgumentException(Resource.RequireSamePos);
			var result = new DenseVector<T>(M.NCols, v.OnHost);
			try
			{
				//tex:$\vec{v}^* M \equiv (M^* \vec{v})^*$
				result.Mulβ_AddBy_αopAx(M, v, Scalars<T>.One, op: MatrixOperation.ConjugateTranspose);
				BLAS.PointWiseConjugate(result);
				return result;
			}
			catch (Exception)
			{
				result?.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Element wise power.
		/// </summary>
		/// <param name="v">array</param>
		/// <param name="p">a <see cref="double"/> power</param>
		/// <remarks>only real types are supported</remarks>
		/// <returns>$$a_{\text{result}}_i={(a_i)}^p$$</returns>
		public static VectorBase<T> operator ^(VectorBase<T> v, double p)
		{
			if (v is null)
				throw new ArgumentNullException(nameof(v), Resource.ArrayCannotNull);

			return v.ApplyToClone(c => BLAS.PointWisePower(c, p));
		}

		/// <summary>
		/// Vector addition.
		/// </summary>
		/// <param name="left">left operand, will be overwritten if it is in-place</param>
		/// <param name="right">right operand</param>
		/// <returns>$$\vec{v}_{\text{result}}=\vec{v}_1 + \vec{v}_2$$</returns>
		public static VectorBase<T> operator +(VectorBase<T> left, VectorBase<T> right)
		{
			if (left is null)
				throw new ArgumentNullException(nameof(left), Resource.ArrayCannotNull);
			if (right is null)
				throw new ArgumentNullException(nameof(right), Resource.ArrayCannotNull);

			return left.ApplyToClone(l => l.AddBy_αx(right, Scalars<T>.One));
		}

		/// <summary>
		/// Vector subtraction.
		/// </summary>
		/// <param name="left">left operand, will be overwritten if it is in-place</param>
		/// <param name="right">right operand</param>
		/// <returns>$$\vec{v}_{\text{result}=\vec{v}_1 - \vec{v}_2$$</returns>
		public static VectorBase<T> operator -(VectorBase<T> left, VectorBase<T> right)
		{
			if (left is null)
				throw new ArgumentNullException(nameof(left), Resource.ArrayCannotNull);
			if (right is null)
				throw new ArgumentNullException(nameof(right), Resource.ArrayCannotNull);

			return left.ApplyToClone(l => l.AddBy_αx(right, Scalars<T>.MinusOne));
		}
		#endregion


		#region indexers
		/// <summary>
		/// Check the range then return offset and count.
		/// </summary>
		/// <param name="range">The input <see cref="Range"/></param>
		/// <returns>offset and count</returns>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="range"/> is out of range</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected (long offset, long count) CheckRange(Range range)
		{
			var (offset, count) = range.GetOffsetAndCount(this.Length);
			if (offset < 0 || offset >= this.Length)
				throw new ArgumentOutOfRangeException(nameof(range), range, Resource.RangeStartWrong);
			if (count <= 0 || offset + count - 1 > this.LastIndex)
				throw new ArgumentOutOfRangeException(nameof(range), range, Resource.RangeCountWrong);
			return (offset, count);
		}

		/// <summary>
		/// Check the index then return the offset.
		/// </summary>
		/// <param name="index">The input <see cref="Index"/></param>
		/// <returns>offset</returns>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="index"/> is out of range</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected long CheckRange(Index index)
		{
			long offset = index.GetPosition(this.Length);
			if (offset < 0 || offset >= this.Length)
				throw new ArgumentOutOfRangeException(nameof(index), index, Resource.IndexWrong);
			return offset;
		}

		/// <summary>
		/// Check the indices then return the offsets.
		/// </summary>
		/// <param name="indices">The input array of <see cref="Index"/></param>
		/// <returns>offsets</returns>
		/// <exception cref="ArgumentOutOfRangeException">if any <paramref name="indices"/> is out of range</exception>
		/// <exception cref="ArgumentException">if <paramref name="indices"/> they are not unique</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected long[] CheckRange(Index[] indices)
		{
			var newindices = indices.Select(i => CheckRange(i));
			if (newindices.Count != newindices.Distinct().Count)
				throw new ArgumentException(Resource.DuplicateIndices, nameof(indices));
			return newindices.ToArray();
		}

		/// <summary>
		/// Check the ranges then return offsets.
		/// </summary>
		/// <param name="ranges">The input array of <see cref="Range"/></param>
		/// <returns>offsets</returns>
		/// <exception cref="ArgumentOutOfRangeException">if any <paramref name="ranges"/> is out of range</exception>
		/// <exception cref="ArgumentException">if <paramref name="ranges"/> overlap</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected long[] CheckRange(Range[] ranges)
		{
			var newRanges = ranges.Select(r => CheckRange(r));
			var indices = newRanges.SelectMany(r => ArrayLinq.Range(r.offset, r.count));
			if (indices.Count != indices.Distinct().Count)
				throw new ArgumentException(Resource.DuplicateIndices, nameof(ranges));
			return indices.ToArray();
		}

		/// <summary>
		/// Basic indexer of vector, from <see cref="IVector{T}"/>.
		/// </summary>
		/// <param name="i">position indicated by <see cref="Index"/></param>
		/// <returns>an instance of the data type <typeparamref name="T"/></returns>
		/// <remarks>Since a value cannot hold reference, altering the retrieved value does not change this array's value at that position</remarks>
		public abstract T this[Index i] { get; set; }

		/// <summary>
		/// Single range indexer of vector, new in <see cref="VectorBase{T}"/>.
		/// </summary>
		/// <param name="r">The <see cref="Range"/> of index</param>
		/// <returns>The reference <see cref="VectorBase{T}"/> of the selected range</returns>
		/// <remarks>Range [a, b) are inclusive and exclusive respectively. See <see cref="Range"/> and <see cref="Index"/> for more information</remarks>
		public abstract VectorBase<T> this[Range r] { get; set; }

		/// <summary>
		/// Multiple position indexer of vector, new in <see cref="VectorBase{T}"/>.
		/// </summary>
		/// <param name="indices">positions indicated by array of <see cref="Index"/></param>
		/// <returns>All the values at the indices are copied to a new <see cref="DenseVector{T}"/></returns>
		/// <remarks>Since values are copied to a new <see cref="VectorBase{T}"/>, altering the retrieved values does not change this array's values at these positions</remarks>
		public abstract DenseVector<T> this[params Index[] indices] { get; set; }

		/// <summary>
		/// Multiple range indexer of vector, new in <see cref="VectorBase{T}"/>.
		/// </summary>
		/// <param name="ranges">The array of <see cref="Range"/> of indices</param>
		/// <returns>All the values inside the ranges are copied to a new <see cref="DenseVector{T}"/></returns>
		/// <remarks>This indexer copies the values in the ranges, altering the retrieved values does not change this array's values at these positions</remarks>
		public abstract DenseVector<T> this[params Range[] ranges] { get; set; }
		#endregion
	}
}
