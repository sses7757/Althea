using System;
using System.Collections.Generic;

using Althea.LinearAlgebra;
using Althea.LinearAlgebra.Dense;
using Althea.Storage;

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
		#region basic
		/// <summary>
		/// When implemented by a derived class, get the stride between consecutive elements of this vector in <typeparamref name="T"/>.
		/// </summary>
		int Stride { get; }
		#endregion

		#region indexing
		T IBaseVector<T, TSelf>.this[long index]
		{
			get
			{
				this.CheckIndex(index);
				return (this.Storage + index * this.Stride).ToManaged<T, TS>();
			}
			set
			{
				this.CheckIndex(index);
				(this.Storage + index * this.Stride).FromManaged(value);
			}
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			long length = ((IVectorMetric)this).Length;
			T[] buffer = new T[Math.Min(length, 8192)];
			long offset = 0;
			while (offset < length)
			{
				(this.Storage + offset * this.Stride).ToManagedStride<T, TS>(this.Stride, buffer);
				for (int i = 0; i < buffer.Length; i++)
				{
					yield return buffer[i];
				}
				offset += buffer.LongLength;
			}
		}
		#endregion

		#region point-wise operations
		void IValueArray<T, TSelf>.FillWith(T value) => ExtBlas.FillWithValue(this.Storage, value, this.Stride);

		void IValueArray<T, TSelf>.AddScalar(T value) => ExtBlas.PointWiseAddScalar(this.Storage, this.Stride, value);

		void IValueArray<T, TSelf>.Scale(T value) => Blas.Scale(this.Storage, this.Stride, value);

		void IValueArray<T, TSelf>.Conjugate() => ExtBlas.PointWiseConjugate<T, TS>(this.Storage, this.Stride);

		void IValueArray<T, TSelf>.Power(T power) => ExtBlas.PointWisePower(this.Storage, this.Stride, power);

		void IValueArray<T, TSelf>.Truncate(double threshold) => ExtBlas.TruncateArray<T, TS>(this.Storage, this.Stride, threshold);
		#endregion

		#region simple aggregation operations
		T IValueArray<T, TSelf>.Sum() => ExtBlas.AggregateSum<T, TS>(this.Storage, this.Stride);

		T IValueArray<T, TSelf>.AbsSum() => Blas.AbsoluteValueSum<T, TS>(this.Storage, this.Stride);

		T IValueArray<T, TSelf>.Norm() => Blas.Norm<T, TS>(this.Storage, this.Stride);

		void IValueArray<T, TSelf>.Normalize() => this.Scale(T.One / this.Norm());

		T IValueArray<T, TSelf>.ValueWithMaxAbs() => (this.Storage + Blas.AbsoluteValueArgMax<T, TS>(this.Storage, this.Stride)).ToManaged<T, TS>();

		T IValueArray<T, TSelf>.ValueWithMinAbs() => (this.Storage + Blas.AbsoluteValueArgMin<T, TS>(this.Storage, this.Stride)).ToManaged<T, TS>();
		#endregion
	}
}
