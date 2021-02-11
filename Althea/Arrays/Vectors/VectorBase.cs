using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Althea.Linq;
using Althea.Storage;
using BLAS = Althea.Blas.API;


namespace Althea.Arrays
{
	/// <summary>
	/// The abstract vector class that inherit the <see cref="ValueArray{T}"/>.
	/// </summary>
	/// <typeparam name="T">The supported data types are <see cref="float"/>, <see cref="double"/>, <see cref="FloatComplex"/>, <see cref="DoubleComplex"/>; other types of data causes <see cref="NotSupportedException"/></typeparam>
	public abstract class VectorBase<T> : ValueArray<T>, IVector<VectorBase<T>, MatrixBase<T>, T> where T : struct, IComparable<T>
	{
		#region initialize and destroy
		/// <summary>
		/// The last index of the vector, from <see cref="IVector{T}.LastIndex"/>
		/// </summary>
		public abstract long LastIndex { get; }

		/// <summary>
		/// Full constructor with pre-allocated values.
		/// </summary>
		/// <param name="values"><see cref="Storage{T}"/> of the value array</param>
		/// <param name="length">size of the vector</param>
		protected VectorBase(Storage<T> values, long length) : base(values, new[] { length }) { }

		/// <summary>
		/// Abstract vector data constructor with separate length and actual memory size
		/// </summary>
		/// <param name="actualLength">actual length of vector to allocate on memory</param>
		/// <param name="showLength">The display length of the vector</param>
		/// <param name="onHost">allocate one host memory or device memory</param>
		protected VectorBase(long actualLength, long showLength, bool onHost) : base(actualLength, new long[] { showLength }, onHost) { }

		/// <summary>
		/// Abstract vector reshape constructor
		/// </summary>
		/// <param name="refArray">original array</param>
		/// <param name="actualLength">actual length of vector</param>
		/// <param name="newLength">size of the new vector</param>
		/// <param name="offset">offset to the <see cref="ValueArray{T}.Storage"/> in T rather than bytes</param>
		protected VectorBase(ValueArray<T> refArray, long actualLength, long newLength, long offset = 0) : base(refArray, actualLength, new[] { newLength }, offset) { }
		#endregion


		#region reshape
		/// <summary>
		/// Vector to vector -- just returns this is enough.
		/// </summary>
		/// <returns>this vector</returns>
		/// <seealso cref="ValueArray{T}.ToVector"/>
		public override ValueArray<T> ToVector() => this;
		#endregion


		#region converter
		/// <summary>
		/// Convert this vector to a <see cref="DenseVector{T}"/>. The out-of-place conversion may be performed.
		/// </summary>
		/// <returns>Converted dense vector</returns>
		public abstract DenseVector<T> ToDense();

		/// <summary>
		/// Convert this vector to a <see cref="SparseVector{T}"/>. The out-of-place conversion may be performed.
		/// </summary>
		/// <param name="threshold">values smaller than threshold are regarded as zeros</param>
		/// <returns>Converted sparse vector</returns>
		/// <remarks>If this vector is sparse, this method returns it directly rather than performing prunes.</remarks>
		public abstract SparseVector<T> ToSparse(float threshold = default);
		#endregion


		#region abstract operations (mostly from IVector<TVec, TMat, T>)
		/// <summary>
		/// Operate the matrix whose columns are <paramref name="notJoinedVecs"/> onto a C# array to get a result vector <see cref="VectorBase{T}"/>. From <see cref="General.IKrylovVector{TVec, T}.OperateOn(IReadOnlyList{TVec}, T[])"/>
		/// </summary>
		/// <param name="notJoinedVecs">The columns of the matrix to operate</param>
		/// <param name="input">The input C# array to be operated</param>
		/// <returns><c>[<paramref name="notJoinedVecs"/>] * <paramref name="input"/></c> as <see cref="VectorBase{T}"/>.</returns>
		/// <remarks>this method can actually be static</remarks>
		public abstract VectorBase<T> OperateOn(IReadOnlyList<VectorBase<T>> notJoinedVecs, T[] input);

		/// <summary>
		/// 2-norm of this vector, i.e. $\|\vec{v}\| = \sqrt{\sum_i{\vec{v}_i^2}}$. From <see cref="IVector{T}.Norm"/>.
		/// </summary>
		/// <returns>The 2-norm of this vector.</returns>
		public virtual double Norm()
		{
			double norm = BLAS.VectorNorm(this);
			return norm;
		}

		/// <summary>
		/// Normalize this vector to make it norm-one <b>in-place</b>, i.e. $\vec{v} = \vec{v} / \|\vec{v}\|$.
		/// </summary>
		public virtual void Normalize()
		{
			double norm = BLAS.VectorNorm(this);
			T scalar = (1 / norm).FromDouble<T>();
			BLAS.VectorScale(this, scalar);
		}

		/// <summary>
		/// Compute $\vec{v}_{\text{this}} = \alpha \vec{v}_{\text{this}}$ <b>in-place</b>.
		/// </summary>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		public virtual void Scale(T α) => BLAS.VectorScale(this, α);

		/// <summary>
		/// Replace the values of this vector by the one from <paramref name="other"/>.
		/// </summary>
		/// <param name="other">The <see cref="VectorBase{T}"/> used to replace</param>
		public abstract void ReplaceBy(VectorBase<T> other);

		/// <summary>
		/// Vector inner product, compute $\vec{v}_{\text{this}} \cdot \vec{v}_{\text{other}} \equiv \vec{v}_{\text{this}}^H \vec{v}_{\text{other}}$.
		/// </summary>
		/// <param name="other">The other <see cref="VectorBase{T}"/></param>
		/// <param name="conjugateThis">perform non- or conjugate transpose to this vector</param>
		/// <returns>The inner product result</returns>
		/// <remarks>This method is symmetric (semi-symmetric, e.g. the conjugate relation, when data type is a complex type) for this vector and the other vector.</remarks>
		public abstract T Dot(VectorBase<T> other, bool? conjugateThis = null);

		/// <summary>
		/// Compute $\vec{v}_{\text{this}}\circ\vec{v}_{\text{other}} \equiv \{\vec{v}_{\text{this}}^i \vec{v}_{\text{other}}^i\}_i$.
		/// </summary>
		/// <param name="other">The other <see cref="VectorBase{T}"/></param>
		/// <remarks>This method is symmetric since only the sparse vector one may be modified.</remarks>
		public abstract void PointWiseMultiply(VectorBase<T> other);

		/// <summary>
		/// Compute $\vec{v}_{\text{this}} ./ \vec{v}_{\text{other}} \equiv \{\vec{v}_{\text{this}}^i \vec{v}_{\text{other}}^i\}_i$.
		/// </summary>
		/// <param name="other">The other <see cref="VectorBase{T}"/></param>
		public abstract void PointWiseDivide(VectorBase<T> other);

		/// <summary>
		/// Compute $\vec{v}_{\text{tobeDiv}} ./ \vec{v}_{\text{this}} \equiv \{\vec{v}_{\text{tobeDiv}}^i \vec{v}_{\text{this}}^i\}_i$.
		/// </summary>
		/// <param name="tobeDiv">The vector to be divided (to be in-place altered)</param>
		/// <remarks>The opposite of <see cref="PointWiseDivide"/>, only the classes directly inherits <see cref="VectorBase{T}"/> need to implement this method. This method is used by built-in <see cref="DenseVector{T}"/> and <see cref="SparseVector{T}"/> to implement <see cref="PointWiseDivide"/>.</remarks>
		internal protected abstract void PointWiseDivide_Opposite(VectorBase<T> tobeDiv);

		/// <summary>
		/// Compute $\vec{v}_{\text{this}} = \vec{v}_{\text{this}} + \alpha \vec{x}$.
		/// </summary>
		/// <param name="x">vector</param>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		public abstract void AddBy_αx(VectorBase<T> x, T α);

		/// <summary>
		/// Compute $\vec{y} = \vec{y} + \alpha \vec{v}_{\text{this}}$, always in-place for non-sparse <paramref name="y"/>.
		/// </summary>
		/// <param name="y"><see cref="VectorBase{T}"/> to be altered by this method</param>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		/// <remarks>The opposite of <see cref="AddBy_αx"/>, only the classes directly inherits <see cref="VectorBase{T}"/> need to implement this method. This method is used by built-in <see cref="DenseVector{T}"/> and <see cref="SparseVector{T}"/> to implement <see cref="AddBy_αx"/>.</remarks>
		internal protected abstract void AddBy_αx_Opposite(VectorBase<T> y, T α);

		/// <summary>
		/// Compute $\vec{y}_{\text{this}} = \beta \cdot \vec{y}_{\text{this}} + \alpha \cdot A^{\text{op}} \vec{x}$.
		/// </summary>
		/// <param name="x">The input <see cref="VectorBase{T}"/></param>
		/// <param name="A">The input <see cref="MatrixBase{T}"/></param>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		/// <param name="β">scalar of type <typeparamref name="T"/></param>
		/// <param name="op"><see cref="MatrixOperation"/> applied to <paramref name="A"/></param>
		public abstract void Mulβ_AddBy_αopAx(MatrixBase<T> A, VectorBase<T> x, T α, T β = default, MatrixOperation op = MatrixOperation.None);

		/// <summary>
		/// Compute $\vec{y} = \beta \cdot \vec{y} + \alpha \cdot A \vec{v}_{\text{this}}$, always in-place for non-sparse <paramref name="y"/>.
		/// </summary>
		/// <param name="A"><see cref="MatrixBase{T}"/></param>
		/// <param name="y"><see cref="VectorBase{T}"/> to be in-place altered</param>
		/// <param name="α">scalar of type <typeparamref name="T"/></param>
		/// <param name="β">scalar of type <typeparamref name="T"/></param>
		/// <param name="op"><see cref="MatrixOperation"/> applied to <paramref name="A"/></param>
		/// <returns>The altered <paramref name="y"/> for non-sparse <paramref name="y"/> or some new sparse vector.</returns>
		/// <remarks>The opposite of <see cref="Mulβ_AddBy_αopAx"/>, only the classes directly inherits <see cref="VectorBase{T}"/> need to implement this method. This method is used by built-in <see cref="DenseVector{T}"/> and <see cref="SparseVector{T}"/> to implement <see cref="Mulβ_AddBy_αopAx"/>.</remarks>
		internal protected abstract void Mulβ_AddBy_αopAx_Opposite(MatrixBase<T> A, VectorBase<T> y, T α, T β = default, MatrixOperation op = MatrixOperation.None);

		/// <summary>
		/// Compute $M_{\text{result}} = \vec{v}_{\text{this}} \vec{v}_{\text{other}}^T$ or $M_{\text{result}} = \vec{v}_{\text{this}} \vec{v}_{\text{other}}^H$ out-of-place. From <see cref="IVector{TVec, TMat, T}.OuterProduct"/>
		/// </summary>
		/// <param name="other">The other input <see cref="VectorBase{T}"/></param>
		/// <param name="conjugateOther">perform non- or conjugate transpose to <paramref name="other"/></param>
		/// <param name="overwrite">The <see cref="MatrixBase{T}"/> to overwrite as result, default null</param>
		/// <returns>The result <see cref="MatrixBase{T}"/> or <paramref name="overwrite"/> if it is not null</returns>
		public abstract MatrixBase<T> OuterProduct(VectorBase<T> other, bool? conjugateOther = null, MatrixBase<T> overwrite = null);
		#endregion


		#region defined operators
		/// <summary>
		/// Vector point-wise multiply (not vector inner product).
		/// </summary>
		/// <param name="left">left operand, will be overwritten if it is in-place</param>
		/// <param name="right">right operand</param>
		/// <returns>$$\vec{v}_1\circ\vec{v}_2 \equiv \{\vec{v}_1^i \vec{v}_2^i\}_i$$</returns>
		public static VectorBase<T> operator *(VectorBase<T> left, VectorBase<T> right)
		{
			if (left is null)
				throw new ArgumentNullException(nameof(left), Resource.ArrayCannotNull);
			if (right is null)
				throw new ArgumentNullException(nameof(right), Resource.ArrayCannotNull);
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
