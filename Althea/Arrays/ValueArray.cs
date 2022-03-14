using System;

using Althea.Helpers;
using Althea.Linq;
using Althea.Storage;

using Blas = Althea.LinearAlgebra.Dense.BlasApiSelector;
using ExtBlas = Althea.LinearAlgebra.Dense.ExtendBlasApiSelector;


namespace Althea.Arrays
{
	/// <summary>
	/// The abstract interface whose only value storage is of type <typeparamref name="TS"/> while there may be other index storage(s).
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TS">The storage type used by the value <see cref="Storage"/></typeparam>
	/// <typeparam name="TSelf">The concrete type that implements this <see cref="ISingleValueStorageArray{T, TS, TSelf}"/></typeparam>
	/// <remarks>All inherited classes shall be of column major if not specified.</remarks>
	public interface ISingleValueStorageArray<T, TS, TSelf> : ICheckValid, IDisposable, ICloneable<TSelf>, IMainPropertyFormattable<TSelf>, IEqualityOperators<TSelf, TSelf>
		where T : unmanaged, INumber<T>
		where TS : class, IStorage<T, TS>
		where TSelf : class, ISingleValueStorageArray<T, TS, TSelf>
	{
		#region basic
		/// <summary>
		/// When implemented by a derived class, get the size (in <typeparamref name="T"/>) of this array (the extent at each dimension) as a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/>.
		/// </summary>
		protected ReadOnlySpan<long> Size { get; }

		/// <summary>
		/// When implemented by a derived class, get the presenting length (in <typeparamref name="T"/>) of this array.
		/// </summary>
		long Length { get; }

		/// <summary>
		/// When implemented by a derived class, get the original storage of this array. This is only used for disposition.
		/// </summary>
		protected TS OriginalStorage { get; }

		/// <summary>
		/// Get the referenced value array storage of this array.
		/// </summary>
		public TS Storage => OriginalStorage.MakeReference();

		/// <summary>
		/// Get the total number of the visible values in memory in <typeparamref name="T"/>. The default implementation simply returns <see cref="Storage"/>.<see cref="IStorage{T, TSelf}.Length">Length</see>.
		/// </summary>
		public virtual long ActualLength => this.OriginalStorage.Length;

		void IDisposable.Dispose()
		{
			if (this.OriginalStorage is null)
			{
				return;
			}
			this.OriginalStorage.Dispose();
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// When implemented by a derived class, statically get an empty array of type <typeparamref name="TSelf"/>.
		/// </summary>
		public abstract static TSelf Empty { get; }
		#endregion

		#region point-wise operations
		/// <summary>
		/// When implemented by a derived class, fill this array's <see cref="Storage"/> with given <paramref name="value"/>. The default implementation simply fills <see cref="Storage"/>.
		/// </summary>
		/// <param name="value">The value as a <typeparamref name="T"/> to fill</param>
		public virtual unsafe void FillWith(T value) => this.Storage.FillWith(value);

		/// <summary>
		/// When implemented by a derived class, point-wisely in-place add this array's <see cref="Storage"/> with given <paramref name="value"/>. The default simply adds <see cref="Storage"/> with <paramref name="value"/>.
		/// </summary>
		/// <param name="value">The scalar as <typeparamref name="T"/> to add</param>
		public virtual void AddScalar(T value) => ExtBlas.PointWiseAddScalar(this.Storage, 1, value);

		/// <summary>
		/// When implemented by a derived class, point-wisely in-place multiply this array's <see cref="Storage"/> with given <paramref name="value"/>. The default simply scales <see cref="Storage"/> with <paramref name="value"/>.
		/// </summary>
		/// <param name="value">The scalar as <typeparamref name="T"/> to multiply</param>
		public virtual void Scale(T value) => Blas.Scale(this.Storage, 1, value);

		/// <summary>
		/// When implemented by a derived class, point-wisely in-place conjugate this array's <see cref="Storage"/>. The default simply conjugates <see cref="Storage"/>.
		/// </summary>
		public virtual void Conjugate() => ExtBlas.PointWiseConjugate<T, TS>(this.Storage, 1);

		/// <summary>
		/// When implemented by a derived class, point-wisely in-place exponent this array's <see cref="Storage"/> with given <paramref name="power"/>. The default simply exponentiates <see cref="Storage"/> with <paramref name="power"/>.
		/// </summary>
		/// <param name="power">The power as a <see cref="double"/></param>
		public virtual void Power(double power) => ExtBlas.PointWisePower<T, TS>(this.Storage, 1, power);

		/// <summary>
		/// When implemented by a derived class, point-wisely in-place exponent this array's <see cref="Storage"/> with given <paramref name="power"/>. The default simply exponentiates <see cref="Storage"/> with <paramref name="power"/>.
		/// </summary>
		/// <param name="power">The power as a <typeparamref name="T"/></param>
		public virtual void Power(T power) => ExtBlas.PointWisePower(this.Storage, 1, power);

		/// <summary>
		/// When implemented by a derived class, point-wisely in-place truncate this array's <see cref="Storage"/> by comparing with given <paramref name="threshold"/>. The default simply truncates <see cref="Storage"/> with <paramref name="threshold"/>.
		/// </summary>
		/// <param name="threshold">The threshold as a <see cref="double"/>. Any element in <see cref="Storage"/> whose absolute value ≤ <paramref name="threshold"/> will be set to 0.</param>
		public virtual void Truncate(double threshold) => ExtBlas.TruncateArray<T, TS>(this.Storage, 1, threshold);
		#endregion

		#region simple aggregation operations
		/// <summary>
		/// When implemented by a derived class, aggregately sum the elements in this array. The default implementation only sums <see cref="Storage"/>, which is also valid if the actual derived class is <see cref="ISparseArray{T}"/>. The default simply sums <see cref="Storage"/>.
		/// </summary>
		/// <returns>The aggregate sum of this array.</returns>
		public virtual T Sum() => ExtBlas.AggregateSum<T, TS>(this.Storage, 1);

		/// <summary>
		/// When implemented by a derived class, aggregately sum the absolute values of elements in this array. The default implementation only sums <see cref="Storage"/>, which is also valid if the actual derived class is <see cref="ISparseArray{T}"/>. The default simply sums <see cref="Storage"/>'s absolute values.
		/// </summary>
		/// <returns>The aggregate sum of absolute values of this array.</returns>
		public virtual T AbsSum() => Blas.AbsoluteValueSum<T, TS>(this.Storage, 1);

		/// <summary>
		/// When implemented by a derived class, compute the 2-norm (Euclidean norm) of elements in this array. The default implementation only sums <see cref="Storage"/>, which is also valid if the actual derived class is <see cref="ISparseArray{T}"/>. The default simply calculates <see cref="Storage"/>'s 2-norm.
		/// </summary>
		/// <returns>The 2-norm of this array.</returns>
		public virtual T Norm() => Blas.Norm<T, TS>(this.Storage, 1);

		/// <summary>
		/// When implemented by a derived class, in-place scale this array's <see cref="Storage"/> such that its 2-norm (Euclidean norm) is 1, which is also valid if the actual derived class is <see cref="ISparseArray{T}"/>. The default implementation utilizes the <see cref="Norm()"/> and <see cref="Scale(T)"/>.
		/// </summary>
		public virtual void Normalize() => this.Scale(T.One / this.Norm());

		/// <summary>
		/// When implemented by a derived class, get the element whose absolute value is maximum in this array. The default implementation simply calculates <see cref="Storage"/>'s argument absolute-value maximum.
		/// </summary>
		/// <returns>The element whose absolute value is maximum in this array.</returns>
		public virtual T ValueWithMaxAbs() => (this.Storage + Blas.AbsoluteValueArgMax<T, TS>(this.Storage, 1)).ToManaged<T, TS>();

		/// <summary>
		/// When implemented by a derived class, get the element whose absolute value is minimum in this array. The default implementation simply calculates <see cref="Storage"/>'s argument absolute-value minimum.
		/// </summary>
		/// <returns>The element whose absolute value is minimum in this array.</returns>
		public virtual T ValueWithMinAbs() => (this.Storage + Blas.AbsoluteValueArgMin<T, TS>(this.Storage, 1)).ToManaged<T, TS>();

		/// <summary>
		/// Compare the <see cref="Storage"/> of this array with a given <paramref name="value"/> to check whether all elements in <see cref="Storage"/> is the same as <paramref name="value"/>.
		/// </summary>
		/// <param name="value">The given value in <typeparamref name="T"/> to compare</param>
		/// <returns>True if all elements in <see cref="Storage"/> is the same as <paramref name="value"/>; false otherwise.</returns>
		public virtual bool ValueAllEquals(T value)
		{
			if (!this.IsValid())
				return false;
			T max = this.ValueWithMaxAbs();
			if (value == T.Zero)
				return max == T.Zero;
			T min = this.ValueWithMinAbs();
			return max == value && min == value;
		}
		#endregion

		#region reshape check
		/// <summary>
		/// Check the new size (dimensionality) to reshape to with respect to the original <paramref name="array"/> and find out the uncertain dimension.
		/// </summary>
		/// <param name="array">The original array as a <typeparamref name="TSelf"/> to check</param>
		/// <param name="newSize">The new size as a <see cref="Span{T}"/> to check which can have at most one uncertain dimension indicated by a non-positive number. Overwritten by the new size without uncertain dimension at exit.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="newSize"/> is of length 0</exception>
		/// <exception cref="ArgumentException">If <paramref name="newSize"/> is of length 2 and are all non-positive while the length of <paramref name="array"/> is not a perfect square; or <paramref name="newSize"/> has more than one uncertain dimensions</exception>
		/// <exception cref="ArgumentOutOfRangeException">If the product of <paramref name="newSize"/> is not the same as the presenting length of <paramref name="array"/></exception>
		protected static void CheckSize(TSelf array, Span<long> newSize)
		{
			if (newSize.Length == 0)
				throw new ArgumentNullException(nameof(newSize));
			// shortcut
			if (newSize.SequenceEqual(array.Size))
				return;

			if (newSize.Length == 2 && newSize[0] <= 0 && newSize[1] <= 0)
			{	// try to convert to a square matrix
				if (!array.Length.IsPerfectSquare())
				{
					throw new ArgumentException(Resources.Other.PerfectSquare, nameof(array));
				}
				var leadDim = Convert.ToInt64(Math.Sqrt(array.Length));
				newSize[0] = newSize[1] = leadDim;
			}
			int firstFind = newSize.IndexOf(static r => r <= 0);
			if (firstFind < 0)
			{	// no uncertain index
				if (newSize.Prod() != array.Length)
					throw new ArgumentOutOfRangeException(nameof(newSize), newSize.Prod(), Resources.Parameter.InvalidValue);
				return;
			}
			int lastFind = newSize.LastIndexOf(static r => r <= 0);
			if (lastFind == firstFind)
			{	// only one uncertainty
				newSize[firstFind] = 1;
				var prod = newSize.Prod();
				var remain = array.Length % prod;
				if (remain != 0)
					throw new ArgumentOutOfRangeException(nameof(newSize), remain, Resources.Parameter.InvalidValue);
				else
					newSize[firstFind] = array.Length / prod;
			}
			else
			{	// more than one uncertain indices
				throw new ArgumentException(Resources.Parameter.UnexpectedValue, nameof(newSize));
			}
		}
		#endregion

		#region clone relate
		/// <summary>
		/// When implemented by a derived class, create a new array with same properties as this one while the underlying storages are not filled.
		/// </summary>
		/// <returns>The new array alike this one</returns>
		TSelf NewArrayAlike();

		/// <summary>
		/// When implemented by a derived class, create a new array with same properties as this one while the underlying storages are not filled and the data type is changed to <typeparamref name="TOut"/>.
		/// </summary>
		/// <typeparam name="TOut">Any unmanaged number as the new data type</typeparam>
		/// <typeparam name="TSOut">The output storage type</typeparam>
		/// <typeparam name="TOther">The output array type</typeparam>
		/// <returns>The new array alike this one but with data type <typeparamref name="TOut"/>.</returns>
		TOther NewArrayAlike<TOut, TSOut, TOther>() where TOut : unmanaged, INumber<TOut> where TSOut : class, IStorage<TOut, TSOut> where TOther : class, ISingleValueStorageArray<TOut, TSOut, TOther>;

		/// <summary>
		/// When implemented by a derived class, cast this array into another data type <typeparamref name="TOut"/>. The default implementation only casts the <see cref="Storage"/> of this array.
		/// </summary>
		/// <typeparam name="TOut">The data type to cast to</typeparam>
		/// <typeparam name="TSOut">The output storage type</typeparam>
		/// <typeparam name="TOther">The output array type</typeparam>
		/// <returns>The new <typeparamref name="TOther"/> casted from this array or this array if <typeparamref name="TOut"/> == <typeparamref name="T"/></returns>
		public virtual TOther DataTypeCast<TOut, TSOut, TOther>() where TOut : unmanaged, INumber<TOut> where TSOut : class, IStorage<TOut, TSOut> where TOther : class, ISingleValueStorageArray<TOut, TSOut, TOther>
		{
			if (typeof(T) == typeof(TOut))
			{
				return (TOther)this;
			}
			var alike = this.NewArrayAlike<TOut, TSOut, TOther>();
			try
			{
				ExtBlas.PointWiseCast<T, TOut, TS, TSOut>(this.Storage, 1, alike.Storage, 1);
				return alike;
			}
			catch (Exception)
			{
				alike.Dispose();
				throw;
			}
		}
		#endregion

		#region creation
		/// <summary>
		/// When implemented by a derived class, get all the storages of this array.
		/// </summary>
		/// <returns>All the storages of the array as an <see cref="IReadOnlyDictionary{TKey, TValue}"/> of <see cref="string"/> and <see cref="IStorage"/>.</returns>
		IReadOnlyDictionary<string, IStorage> GetStorages();

		/// <summary>
		/// When implemented by a derived class, get other requisite informations for reconstructing the array of that derived class type.
		/// </summary>
		/// <returns>Other requisite informations used to reconstruct this array.</returns>
		IReadOnlyDictionary<string, object>? GetMetaData();

		/// <summary>
		/// When implemented by a derived factory class, reconstruct a <typeparamref name="TSelf"/> of the derived factory's corresponding array type using <paramref name="size"/>, <paramref name="storages"/> as well as <paramref name="otherInfo"/>.
		/// </summary>
		/// <param name="size">The size of the <typeparamref name="TSelf"/> about to create</param>
		/// <param name="storages">All the original storage(s) of the array(s) of the<typeparamref name="TSelf"/> about to create from <see cref="GetStorages"/></param>
		/// <param name="otherInfo">Other information obtained from <see cref="GetMetaData"/></param>
		/// <returns>The reconstructed <typeparamref name="TSelf"/> of the derived factory's corresponding array type.</returns>
		/// <exception cref="ArgumentException">If the any of the arguments is invalid</exception>
		TSelf CreateArray(ReadOnlySpan<long> size, IReadOnlyDictionary<string, IStorage> storages, IReadOnlyDictionary<string, object>? otherInfo = null);
		#endregion
	}
}
