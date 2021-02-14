using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Althea.Linq;
using Althea.Helpers;
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

		#region indexing
		/// <summary>
		/// Provide legacy support of C# duck type for <c>this[<see cref="Index"/>]</c> and <c>this[<see cref="Range"/>]</c>
		/// </summary>
		public int Count => (int)this.Length;

		/// <summary>
		/// When implemented by a derived class, provide the basic indexed getter and setter of this vector
		/// </summary>
		/// <param name="index">The position of the element to get / set</param>
		/// <returns>The element at <paramref name="index"/></returns>
		public abstract T this[long index] { get; set; }

		/// <summary>
		/// Provide legacy support of <see cref="this[long]"/> and C# duck type for <c>this[<see cref="Index"/>]</c>
		/// </summary>
		public T this[int index] => this[(long)index];

		/// <summary>
		/// When implemented by a derived class, get a sub-vector indicated by the given <paramref name="start"/> offset and <paramref name="length"/>
		/// </summary>
		/// <param name="start">The starting offset of the target sub-vector compared to this vector, in <typeparamref name="T"/></param>
		/// <param name="length">The length of the target sub-vector, in <typeparamref name="T"/></param>
		/// <returns>The sub-vector indicated by <paramref name="start"/> and <paramref name="length"/>. Shall be a referenced vector if possible.</returns>
		public abstract VectorBase<T> Slice(long start, long length);

		/// <summary>
		/// Provide legacy support of C# duck type for <c>this[<see cref="Range"/>]</c>
		/// </summary>
		public VectorBase<T> Slice(int start, int length) => this.Slice(start, length);

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			var a = this[5..^5];
			for (long i = 0; i < this.Length; i++)
			{
				yield return this[i];
			}
		}

		IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<T>)this).GetEnumerator();
		#endregion

		#region operators
		/// <summary>
		/// Create a new <see cref="VectorBase{T}"/> which is the multiplication result of the given <paramref name="vector"/> and <paramref name="scalar"/>
		/// </summary>
		/// <param name="vector">The original vector to multiply</param>
		/// <param name="scalar">The scalar of type <typeparamref name="T"/> to multiply</param>
		/// <returns>A new <see cref="VectorBase{T}"/> which is the multiplication result of the given <paramref name="vector"/> and <paramref name="scalar"/></returns>
		public static VectorBase<T> operator *(VectorBase<T> vector, T scalar)
		{
			if (vector is null || !vector.IsValid())
				throw new ArgumentNullException(nameof(vector));
			return vector.ApplyToClone(v => v.AddScalar(scalar));
		}

		/// <summary>
		/// Create a new <see cref="VectorBase{T}"/> which is the negation result of the given <paramref name="vector"/>
		/// </summary>
		/// <param name="vector">The original vector to negate</param>
		/// <returns>A new <see cref="VectorBase{T}"/> which is the negation result of the given <paramref name="vector"/></returns>
		public static VectorBase<T> operator -(VectorBase<T> vector) => vector * Scalars<T>.MinusOne;

		/// <summary>
		/// Create a new <see cref="VectorBase{T}"/> which is the multiplication result of the given <paramref name="vector"/> and <paramref name="scalar"/>
		/// </summary>
		/// <param name="vector">The original vector to multiply</param>
		/// <param name="scalar">The scalar of type <typeparamref name="T"/> to multiply</param>
		/// <returns>A new <see cref="VectorBase{T}"/> which is the multiplication result of the given <paramref name="vector"/> and <paramref name="scalar"/></returns>
		public static VectorBase<T> operator *(T scalar, VectorBase<T> vector) => vector * scalar;

		/// <summary>
		/// Create a new <see cref="VectorBase{T}"/> which is the division result of the given <paramref name="vector"/> and <paramref name="scalar"/>
		/// </summary>
		/// <param name="vector">The original vector to be divided</param>
		/// <param name="scalar">The scalar of type <typeparamref name="T"/> to divide</param>
		/// <returns>A new <see cref="VectorBase{T}"/> which is the multiplication result of the given <paramref name="vector"/> and <paramref name="scalar"/></returns>
		public static VectorBase<T> operator /(VectorBase<T> vector, T scalar) => vector * scalar.GenericReciprocal();

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
	}
}
