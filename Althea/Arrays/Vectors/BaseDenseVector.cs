using System;
using System.Collections.Generic;

using Althea.Helpers;
using Althea.Storage;
using Althea.NativeTypes;

using Blas = Althea.LinearAlgebra.Dense.BlasApiSelector;
using ExtBlas = Althea.LinearAlgebra.Dense.ExtendBlasApiSelector;


namespace Althea.Arrays
{
	/// <summary>
	/// The dense vector interface whose only storage is of type <typeparamref name="TS"/>.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TS">The storage type used by the value <see cref="Storage"/></typeparam>
	/// <typeparam name="TSelf">The concrete type that implements this <see cref="IDenseVector{T, TS, TSelf}"/></typeparam>
	public interface IDenseVector<T, TS, TSelf> : IBaseVector<T, TSelf>, ISingleValueStorageArray<T, TS, TSelf>
		where T : unmanaged, INumber<T>
		where TS : class, IStorage<T, TS>
		where TSelf : class, IDenseVector<T, TS, TSelf>
	{
		#region indexing
		T IBaseVector<T, TSelf>.this[long index]
		{
			get
			{
				this.CheckIndex(index);
				return (this.Storage + index).ToManaged<T, TS>();
			}
			set
			{
				this.CheckIndex(index);
				(this.Storage + index).FromManaged(value);
			}
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			long length = this.Storage.Length;
			T[] buffer = new T[Math.Min(length, 8192)];
			long offset = 0;
			while (offset < length)
			{
				(this.Storage + offset).ToManaged<T, TS>(buffer);
				for (int i = 0; i < buffer.Length; i++)
				{
					yield return buffer[i];
				}
				offset += buffer.LongLength;
			}
		}
		#endregion

		#region linear algebra methods
		T IBaseVector<T, TSelf>.Dot<TOther>(TOther other, bool conjugateThis)
		{
			if (other is null || !other.IsValid())
				throw new ArgumentNullException(nameof(other));
			if (other is TSelf o)
				return Blas.Dot<T, TS, TS>(conjugateThis, this.Storage, 1, o.Storage, 1);
			else
				throw new NotSupportedException(Resources.Support.DataType);
		}

		void IBaseVector<T, TSelf>.AddBy<TOther>(TOther other, T scalar)
		{
			if (other is null || !other.IsValid())
				throw new ArgumentNullException(nameof(other));
			if (other is TSelf o)
				Blas.Add(scalar, o.Storage, 1, this.Storage, 1);
			else
				throw new NotSupportedException(Resources.Support.DataType);
		}

		/// <summary>
		/// Create a new <typeparamref name="TSelf"/> which is the point-wise exponentiation result of the given <paramref name="vector"/> and <paramref name="power"/>.
		/// </summary>
		/// <param name="vector">The original vector whose elements are the bases</param>
		/// <param name="power">The power acting as the exponent of type <see cref="double"/></param>
		/// <returns>A new <typeparamref name="TSelf"/> which is the point-wise exponentiate result of the given <paramref name="vector"/> and <paramref name="power"/></returns>
		protected static TSelf OutOfPlacePow(TSelf vector, double power)
		{
			if (vector is null || !vector.IsValid())
				throw new ArgumentNullException(nameof(vector));

			return vector.ApplyToClone(v => v.Power(power));
		}

		/// <summary>
		/// Create a new <typeparamref name="TSelf"/> which is the point-wise exponentiation result of the given <paramref name="vector"/> and <paramref name="power"/>.
		/// </summary>
		/// <param name="vector">The original vector whose elements are the bases</param>
		/// <param name="power">The power acting as the exponent of type <typeparamref name="T"/></param>
		/// <returns>A new <typeparamref name="TSelf"/> which is the point-wise exponentiate result of the given <paramref name="vector"/> and <paramref name="power"/></returns>
		protected static TSelf OutOfPlacePow(TSelf vector, T power)
		{
			if (vector is null || !vector.IsValid())
				throw new ArgumentNullException(nameof(vector));

			return vector.ApplyToClone(v => v.Power(power));
		}

		/// <summary>
		/// Create a new <typeparamref name="TSelf"/> which is the (point-wise) addition result of the given <paramref name="left"/> and <paramref name="right"/> vectors.
		/// </summary>
		/// <param name="left">One original vector as the left operand</param>
		/// <param name="right">One original vector as the right operand</param>
		/// <returns>A new <typeparamref name="TSelf"/> which is the addition result of the given <paramref name="left"/> and <paramref name="right"/> vectors</returns>
		protected static TSelf OutOfPlaceAdd(TSelf left, TSelf right)
		{
			if (left is null || !left.IsValid())
				throw new ArgumentNullException(nameof(left));
			if (right is null || !right.IsValid())
				throw new ArgumentNullException(nameof(right));

			return left.ApplyToClone(c => c.AddBy(right, T.One));
		}

		/// <summary>
		/// Create a new <typeparamref name="TSelf"/> which is the (point-wise) subtraction result of the given <paramref name="left"/> and <paramref name="right"/> vectors.
		/// </summary>
		/// <param name="left">One original vector as the left operand</param>
		/// <param name="right">One original vector as the right operand</param>
		/// <returns>A new <typeparamref name="TSelf"/> which is the subtraction result of the given <paramref name="left"/> and <paramref name="right"/> vectors</returns>
		protected static TSelf OutOfPlaceSub(TSelf left, TSelf right)
		{
			if (left is null || !left.IsValid())
				throw new ArgumentNullException(nameof(left));
			if (right is null || !right.IsValid())
				throw new ArgumentNullException(nameof(right));
			if (Unmanaged<T>.DataType.IsUnsignedInteger())
				throw new InvalidOperationException(Resources.Other.CannotNegate);

			return left.ApplyToClone(c => c.AddBy(right, -T.One));
		}

		/// <summary>
		/// Create a new <typeparamref name="TSelf"/> which is the multiplication result of the given <paramref name="vector"/> and <paramref name="scalar"/>
		/// </summary>
		/// <param name="vector">The original vector to multiply</param>
		/// <param name="scalar">The scalar of type <typeparamref name="T"/> to multiply</param>
		/// <returns>A new <typeparamref name="TSelf"/> which is the multiplication result of the given <paramref name="vector"/> and <paramref name="scalar"/></returns>
		protected static TSelf OutOfPlaceScale(TSelf vector, T scalar)
		{
			if (vector is null || !vector.IsValid())
				throw new ArgumentNullException(nameof(vector));

			return vector.ApplyToClone(v => v.Scale(scalar));
		}

		/// <summary>
		/// Create a new <typeparamref name="TSelf"/> which is the negation result of the given <paramref name="vector"/>
		/// </summary>
		/// <param name="vector">The original vector to negate</param>
		/// <returns>A new <typeparamref name="TSelf"/> which is the negation result of the given <paramref name="vector"/></returns>
		protected static TSelf OutOfPlaceNeg(TSelf vector)
		{
			if (vector is null || !vector.IsValid())
				throw new ArgumentNullException(nameof(vector));
			if (Unmanaged<T>.DataType.IsUnsignedInteger())
				throw new InvalidOperationException(Resources.Other.CannotNegate);

			return vector.ApplyToClone(v => v.Scale(-T.One));
		}
		#endregion
	}
}
