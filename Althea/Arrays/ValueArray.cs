using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Althea.Helpers;
using Althea.Linq;
using Althea.NativeTypes;
using Althea.Storage;

using LACopy = Althea.LinearAlgebra.Dense.CopyApiSelector;
using Blas = Althea.LinearAlgebra.Dense.BlasApiSelector;
using ExtBlas = Althea.LinearAlgebra.Dense.ExtendBlasApiSelector;
using Mem = Althea.Storage.ApiSelector;


namespace Althea.Arrays
{
	/// <summary>
	/// The abstract interface whose only value storage is of type <typeparamref name="TS"/> while there may be other index storage(s).
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TS">The storage type used by the value <see cref="Storage"/></typeparam>
	/// <typeparam name="TSelf">The concrete type that implements this <see cref="ISingleValueStorageArray{T, TS, TSelf}"/></typeparam>
	/// <remarks>All inherited classes shall be of column major if not specified.</remarks>
	public interface ISingleValueStorageArray<T, TS, TSelf> : ICheckValid, IDisposable, IMainPropertyFormattable<TSelf>
		where T : unmanaged, INumber<T>
		where TS : class, IStorage<T, TS>
		where TSelf : class, ISingleValueStorageArray<T, TS, TSelf>
	{
		#region basic
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
		#endregion

		#region static
		/// <summary>
		/// When implemented by a derived class, statically get an empty array of type <typeparamref name="TSelf"/>.
		/// </summary>
		public abstract static TSelf Empty { get; }

		private static int stride = 0;

		private static int StridedVectorStride 
		{
			get
			{
				if (stride == 0)
				{
					if (TSelf.Empty is IPitchedArray<T> p && p.HasPitch && p.Size.Length == 1)
						stride = checked((int)p.Strides[0]);
					else
						stride = 1;
				}
				return stride;
			}
		}
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
		public virtual void AddScalar(T value) => ExtBlas.PointWiseAddScalar(this.Storage, StridedVectorStride, value);

		/// <summary>
		/// When implemented by a derived class, point-wisely in-place multiply this array's <see cref="Storage"/> with given <paramref name="value"/>. The default simply scales <see cref="Storage"/> with <paramref name="value"/>.
		/// </summary>
		/// <param name="value">The scalar as <typeparamref name="T"/> to multiply</param>
		public virtual void Scale(T value) => Blas.Scale(this.Storage, StridedVectorStride, value);

		/// <summary>
		/// When implemented by a derived class, point-wisely in-place conjugate this array's <see cref="Storage"/>. The default simply conjugates <see cref="Storage"/>.
		/// </summary>
		public virtual void Conjugate() => ExtBlas.PointWiseConjugate<T, TS>(this.Storage, StridedVectorStride);

		/// <summary>
		/// When implemented by a derived class, point-wisely in-place exponent this array's <see cref="Storage"/> with given <paramref name="power"/>. The default simply exponentiates <see cref="Storage"/> with <paramref name="power"/>.
		/// </summary>
		/// <param name="power">The power as a <see cref="double"/></param>
		public virtual void Power(double power) => ExtBlas.PointWisePower<T, TS>(this.Storage, StridedVectorStride, power);

		/// <summary>
		/// When implemented by a derived class, point-wisely in-place exponent this array's <see cref="Storage"/> with given <paramref name="power"/>. The default simply exponentiates <see cref="Storage"/> with <paramref name="power"/>.
		/// </summary>
		/// <param name="power">The power as a <typeparamref name="T"/></param>
		public virtual void Power(T power) => ExtBlas.PointWisePower(this.Storage, StridedVectorStride, power);

		/// <summary>
		/// When implemented by a derived class, point-wisely in-place truncate this array's <see cref="Storage"/> by comparing with given <paramref name="threshold"/>. The default simply truncates <see cref="Storage"/> with <paramref name="threshold"/>.
		/// </summary>
		/// <param name="threshold">The threshold as a <see cref="double"/>. Any element in <see cref="Storage"/> whose absolute value ≤ <paramref name="threshold"/> will be set to 0.</param>
		public virtual void Truncate(double threshold) => ExtBlas.TruncateArray<T, TS>(this.Storage, StridedVectorStride, threshold);
		#endregion

		#region simple aggregation operations
		/// <summary>
		/// When implemented by a derived class, aggregately sum the elements in this array. The default implementation only sums <see cref="Storage"/>, which is also valid if the actual derived class is <see cref="ISparseArray{T}"/>. The default simply sums <see cref="Storage"/>.
		/// </summary>
		/// <returns>The aggregate sum of this array</returns>
		public virtual T Sum() => ExtBlas.AggregateSum<T, TS>(this.Storage, StridedVectorStride);

		/// <summary>
		/// When implemented by a derived class, aggregately sum the absolute values of elements in this array. The default implementation only sums <see cref="Storage"/>, which is also valid if the actual derived class is <see cref="ISparseArray{T}"/>. The default simply sums <see cref="Storage"/>'s absolute values.
		/// </summary>
		/// <returns>The aggregate sum of absolute values of this array</returns>
		public virtual double AbsSum() => Blas.AbsoluteValueSum<T, TS>(this.Storage, StridedVectorStride);

		/// <summary>
		/// When implemented by a derived class, compute the 2-norm (Euclidean norm) of elements in this array. The default implementation only sums <see cref="Storage"/>, which is also valid if the actual derived class is <see cref="ISparseArray{T}"/>. The default simply calculates <see cref="Storage"/>'s 2-norm.
		/// </summary>
		/// <returns>The 2-norm of this array</returns>
		public virtual double Norm() => Blas.Norm<T, TS>(this.Storage, StridedVectorStride);

		/// <summary>
		/// When implemented by a derived class, in-place scale this array's <see cref="Storage"/> such that its 2-norm (Euclidean norm) is 1, which is also valid if the actual derived class is <see cref="ISparseArray{T}"/>. The default implementation utilizes the <see cref="Norm()"/> and <see cref="Scale(T)"/>.
		/// </summary>
		public virtual void Normalize() => this.Scale(T.One / T.Create(this.Norm()));

		/// <summary>
		/// When implemented by a derived class, get the maximum one of all absolute values of the elements in this array. The default implementation only get the maximum absolute value of <see cref="Storage"/>, which is also valid if the actual derived class is <see cref="ISparseArray{T}"/>. The default implementation simply calculates <see cref="Storage"/>'s argument absolute-value maximum.
		/// </summary>
		/// <returns>The maximum one of all absolute values of the elements in this array</returns>
		public virtual T AbsMax() => T.Abs((this.Storage + Blas.AbsoluteValueArgMax<T, TS>(this.Storage, StridedVectorStride)).ToManaged<T, TS>());

		/// <summary>
		/// When implemented by a derived class, get the minimum one of all absolute values of the elements in this array. The default implementation only get the minimum absolute value of <see cref="Storage"/>, which is also valid if the actual derived class is <see cref="ISparseArray{T}"/>. The default implementation simply calculates <see cref="Storage"/>'s argument absolute-value minimum.
		/// </summary>
		/// <returns>The minimum one of all absolute values of the elements in this array</returns>
		public virtual T AbsMin() => T.Abs((this.Storage + Blas.AbsoluteValueArgMin<T, TS>(this.Storage, StridedVectorStride)).ToManaged<T, TS>());
		#endregion

		#region reshape (mostly abstract)
		/// <summary>
		/// Check the new size (dimensionality) to reshape to with respect to the original <paramref name="array"/> and find out the uncertain dimension.
		/// </summary>
		/// <param name="array">The original array as a <see cref="ValueArray{T}"/> to check</param>
		/// <param name="newSize">The new size as a <see cref="Span{T}"/> to check which can have at most one uncertain dimension indicated by a non-positive number. Overwritten by the new size without uncertain dimension at exit.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="newSize"/> is of length 0</exception>
		/// <exception cref="ArgumentException">If <paramref name="newSize"/> is of length 2 and are all non-positive while the length of <paramref name="array"/> is not a perfect square; or <paramref name="newSize"/> has more than one uncertain dimensions</exception>
		/// <exception cref="ArgumentOutOfRangeException">If the product of <paramref name="newSize"/> is not the same as the presenting length of <paramref name="array"/></exception>
		protected static void CheckSize(ValueArray<T> array, Span<long> newSize)
		{
			if (newSize.Length == 0)
				throw new ArgumentNullException(nameof(newSize));
			// shortcut
			if (newSize.SequenceEqual(array.Size))
				return;

			if (newSize.Length == 2 && newSize[0] <= 0 && newSize[StridedVectorStride] <= 0)
			{	// try to convert to a square matrix
				if (!array.Length.IsPerfectSquare())
				{
					throw new ArgumentException(Resources.Other.PerfectSquare, nameof(array));
				}
				var leadDim = Convert.ToInt64(Math.Sqrt(array.Length));
				newSize[0] = newSize[StridedVectorStride] = leadDim;
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

		/// <summary>
		/// When implemented by a derived class, reshape the array to a <paramref name="newSize"/>.
		/// </summary>
		/// <param name="newSize">The new size/dimensionality. You can have at most one uncertain dimension, indicated by a non-positive number.</param>
		/// <returns>The reshaped array which may be a referenced array or may not</returns>
		public virtual ValueArray<T> Reshape(ReadOnlySpan<long> newSize)
		{
			Span<long> size = stackalloc long[newSize.Length];
			newSize.CopyTo(size);
			CheckSize(this, size);
			if (this.Size.SequenceEqual(newSize))
				return this;
			return newSize.Length switch
			{
				0 => throw new ArgumentException(Resources.Parameter.ZeroSize, nameof(newSize)),
				1 => this.ToVector(),
				2 => this.ToMatrix(newSize[0]),
				_ => this.ToTensor(size: size),
			};
		}

		/// <summary>
		/// When implemented by a derived class, reshape this array to a vector
		/// </summary>
		/// <returns>The referenced vector reshaped from this array</returns>
		public abstract ValueArray<T> ToVector();

		/// <summary>
		/// When implemented by a derived class, reshape the array to a matrix with leading dimension = <paramref name="rows"/>
		/// </summary>
		/// <param name="rows">The number of rows of the target matrix; if <paramref name="rows"/> ≤ 0, it is assumed that leadDim = <c>sqrt(<see cref="AbstractArray{T}.Length"/>)</c>.</param>
		/// <returns>The reshaped matrix</returns>
		public abstract ValueArray<T> ToMatrix(long rows = 0);

		/// <summary>
		/// When implemented by a derived class, reshape the array to a tensor with dimensionality = <paramref name="size"/>.
		/// </summary>
		/// <param name="size">The new size/dimensionality with at most one or zero uncertain dimension indicated by a non-positive number.</param>
		/// <returns>The reshaped tensor</returns>
		public abstract ValueArray<T> ToTensor(ReadOnlySpan<long> size);
		#endregion

		#region new methods and overrides
		/// <summary>
		/// When implemented by a derived class, check if this <see cref="ValueArray{T}"/> share some storage with the <paramref name="other"/> one. The default implementation only compares the <see cref="Storage"/>s.
		/// </summary>
		/// <param name="other">The other <see cref="ValueArray{T}"/> to check</param>
		/// <returns>True if they do share some storage, false otherwise</returns>
		public virtual bool OverlapWith(ValueArray<T> other)
		{
			if (other is ValueArray<T> arr)
			{
				if (ReferenceEquals(this, arr))
					return true;
				else
					return this.Storage.OverlapWith(arr.Storage);
			}
			else
				return false;
		}

		/// <summary>
		/// The string representation terms
		/// </summary>
		protected enum StringTerms
		{
			/// <summary>
			/// Add the term for the string representation of the current data type(s)
			/// </summary>
			DataType,
			/// <summary>
			/// Add the term for the string representation of the all storages obtained from <see cref="GetStorages"/>
			/// </summary>
			Storages,
			/// <summary>
			/// Add the term for the string representation of the current presenting size
			/// </summary>
			Size
		}

		string IMainPropertyFormattable.StringMain {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.GetType().GetGenericString(full: false) ?? this.GetType().Name;
		}

		IEnumerable<KeyValuePair<string, object?>> IMainPropertyFormattable.StringProperties {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				var other = this.GetMetaData();
				var storages = this.GetStorages();
				var terms = new KeyValuePair<string, object?>[1 + storages.Count + other.Count];
				terms[0] = new(nameof(this.Size), this.Size.SpanJoin('x'));
				int now = 1;
				foreach (var kv in storages)
				{
					terms[now++] = new(kv.Key, kv.Value);
				}
				foreach (var kv in other)
				{
					terms[now++] = new(kv.Key, kv.Value);
				}
				return terms;
			}
		}

		/// <summary>
		/// Get the string representation of this array with new terms and existed ones (existed ones are shown at first).
		/// </summary>
		/// <param name="terms">The additional terms, null means all pairs in <see cref="GetMetaData"/></param>
		/// <param name="include">The include terms, default null means all</param>
		/// <returns>The string representation</returns>
		protected string ToString(IReadOnlyDictionary<string, object>? terms, params StringTerms[] include)
		{
			// shortcut
			if ((include is null || include.Length == 0) && terms is null)
				return ((IMainPropertyFormattable)this).ToString();
			// default values
			if (include is null || include.Length == 0)
				include = new[] { StringTerms.DataType, StringTerms.Size, StringTerms.Storages };
			terms ??= this.GetMetaData();
			// get type name of this array
			var type = this.GetType();
			string name;
			if (include.Contains(StringTerms.DataType))
			{
				name = type.GetGenericString(full: false) ?? type.Name;
			}
			else
			{
				name = type.FullName ?? type.Name;
				if (type.IsGenericType)
				{
					name = name.Replace($"`{type.GenericTypeArguments.Length}", "");
				}
			}
			// output include terms and other terms
			if (include.Contains(StringTerms.Storages))
			{
				bool hasSize = include.Contains(StringTerms.Size);
				var storages = this.GetStorages();
				var newTerms = new KeyValuePair<string, object?>[(hasSize ? 1 : 0) + storages.Count + terms.Count];
				if (hasSize)
					newTerms[0] = new(nameof(this.Size), this.Size.SpanJoin('x'));
				int now = hasSize ? 1 : 0;
				foreach (var kv in storages)
				{
					newTerms[now++] = new(kv.Key, kv.Value);
				}
				foreach (var kv in terms)
				{
					newTerms[now++] = new(kv.Key, kv.Value);
				}
				return IMainPropertyFormattable.Combine(name, newTerms);
			}
			else
			{
				bool hasSize = include.Contains(StringTerms.Size);
				var newTerms = new KeyValuePair<string, object?>[(hasSize ? 1 : 0) + terms.Count];
				if (hasSize)
					newTerms[0] = new(nameof(this.Size), this.Size.SpanJoin('x'));
				int now = hasSize ? 1 : 0;
				foreach (var kv in terms)
				{
					newTerms[now++] = new(kv.Key, kv.Value);
				}
				return IMainPropertyFormattable.Combine(name, newTerms);
			}
		}

		/// <summary>
		/// Override <see cref="AbstractArray{T}.ToString"/> to get the string representation of this array.
		/// </summary>
		/// <returns>String representation of this array</returns>
		public override string ToString()
		{
			return this.ToString(terms: null);
		}

		/// <summary>
		/// When implemented by a derived class, get the hash code this array. The default implementation only takes <see cref="Storage"/> and <see cref="AbstractArray{T}.Size"/> into account.
		/// </summary>
		/// <returns>The hash code computed by <see cref="Storage"/> and <see cref="AbstractArray{T}.Size"/></returns>
		public override int GetHashCode() => HashCode.Combine(this.Storage, this.Size.HashCodeOfSpan());

		/// <summary>
		/// When implemented by a derived class, check whether this object is equal to another one. The default implementation only compares <see cref="Storage"/>s.
		/// </summary>
		/// <param name="obj">The other object to compare with</param>
		/// <returns>True if this == <paramref name="obj"/></returns>
		public override bool Equals(object? obj)
		{
			if (obj is null || obj is not ValueArray<T> a)
				return false;
			else
				return this.Storage == a.Storage;
		}
		#endregion

		#region clone relate
		/// <summary>
		/// When implemented by a derived class, deep clone the array, the mutable status will not be copied.
		/// </summary>
		/// <returns>The cloned array</returns>
		public override abstract ValueArray<T> Clone();

		/// <summary>
		/// When implemented by a derived class, create a new array with same properties as this one while the underlying storages are not filled.
		/// </summary>
		/// <returns>The new array alike this one</returns>
		public abstract ValueArray<T> NewArrayAlike();

		/// <summary>
		/// When implemented by a derived class, create a new array with same properties as this one while the underlying storages are not filled and the data type is changed to <typeparamref name="TOut"/>.
		/// </summary>
		/// <typeparam name="TOut">Any unmanaged number as the new data type</typeparam>
		/// <returns>The new array alike this one</returns>
		public abstract ValueArray<TOut> NewArrayAlike<TOut>() where TOut : unmanaged;

		/// <summary>
		/// When implemented by a derived class, cast this array into another data type <typeparamref name="TOut"/>. The default implementation only casts the <see cref="Storage"/> of this array.
		/// </summary>
		/// <typeparam name="TOut">The data type to cast to</typeparam>
		/// <returns>The new <see cref="ValueArray{TOut}"/> casted from this array or this array if <typeparamref name="TOut"/> == <typeparamref name="T"/></returns>
		public override ValueArray<TOut> DataTypeCast<TOut>()
		{
			if (typeof(T) == typeof(TOut))
			{
#pragma warning disable CS8603 // the 'as' here cannot return null
				return this as ValueArray<TOut>;
#pragma warning restore CS8603
			}
			var alike = this.NewArrayAlike<TOut>();
			try
			{
				LAD.PointWiseCast(this.Storage, StridedVectorStride, alike.Storage, StridedVectorStride);
				return alike;
			}
			catch (Exception)
			{
				alike?.Dispose();
				throw;
			}
		}
		#endregion

		#region override operators
		/// <summary>
		/// Compare the <see cref="Storage"/> of the given <paramref name="array"/> with a given <paramref name="value"/> to check whether all elements in <paramref name="array"/>'s <see cref="Storage"/> is with in the range of <c><paramref name="value"/> + (-1e-6, +1e-6)</c>.
		/// </summary>
		/// <param name="array">The given <see cref="ValueArray{T}"/> to be compared</param>
		/// <param name="value">The given value in <typeparamref name="T"/> to compare</param>
		/// <returns>True if all elements in <paramref name="array"/>'s <see cref="Storage"/> is with in the range of <c><paramref name="value"/> + (-1e-6, +1e-6)</c>; false otherwise.</returns>
		public static bool operator ==(ValueArray<T> array, T value)
		{
			if (array is null || !array.IsValid())
				return false;
			long index;
			if (!value.IsZero())
			{
				using var clone = array.Storage.Clone();
				LAD.PointWiseAddScalar(clone, StridedVectorStride, value.NativeNegate());
				index = LAD.AbsoluteValueArgMax(clone, StridedVectorStride);
			}
			else
			{
				index = LAD.AbsoluteValueArgMax(array.Storage, StridedVectorStride);
			}
			double val = Const<T>.AbsoluteDelegate.Invoke(Mem.ToManaged(array.Storage + index))
							.ToDouble();
			return val <= 1E-6;
		}

		/// <summary>
		/// Compare the <see cref="Storage"/> of the given <paramref name="array"/> with a given <paramref name="value"/> to check whether all elements in <paramref name="array"/>'s <see cref="Storage"/> is with in the range of <c><paramref name="value"/> + (-1e-6, +1e-6)</c>.
		/// </summary>
		/// <param name="array">The given <see cref="ValueArray{T}"/> to be compared</param>
		/// <param name="value">The given value in <typeparamref name="T"/> to compare</param>
		/// <returns>True if all elements in <paramref name="array"/>'s <see cref="Storage"/> is with in the range of <c><paramref name="value"/> + (-1e-6, +1e-6)</c>; false otherwise.</returns>
		public static bool operator ==(T value, ValueArray<T> array) => array == value;

		/// <summary>
		/// Compare the <see cref="Storage"/> of the given <paramref name="array"/> with a given <paramref name="value"/> to check whether all elements in <paramref name="array"/>'s <see cref="Storage"/> is with in the range of <c><paramref name="value"/> + (-1e-6, +1e-6)</c>.
		/// </summary>
		/// <param name="array">The given <see cref="ValueArray{T}"/> to be compared</param>
		/// <param name="value">The given value in <typeparamref name="T"/> to compare</param>
		/// <returns>True if any element in <paramref name="array"/>'s <see cref="Storage"/> is not with in the range of <c><paramref name="value"/> + (-1e-6, +1e-6)</c>; false otherwise.</returns>
		public static bool operator !=(ValueArray<T> array, T value) => !(array == value);

		/// <summary>
		/// Compare the <see cref="Storage"/> of the given <paramref name="array"/> with a given <paramref name="value"/> to check whether all elements in <paramref name="array"/>'s <see cref="Storage"/> is with in the range of <c><paramref name="value"/> + (-1e-6, +1e-6)</c>.
		/// </summary>
		/// <param name="array">The given <see cref="ValueArray{T}"/> to be compared</param>
		/// <param name="value">The given value in <typeparamref name="T"/> to compare</param>
		/// <returns>True if any element in <paramref name="array"/>'s <see cref="Storage"/> is not with in the range of <c><paramref name="value"/> + (-1e-6, +1e-6)</c>; false otherwise.</returns>
		public static bool operator !=(T value, ValueArray<T> array) => !(array == value);
		#endregion

		#region serialization
		/// <summary>
		/// The pointer name that <b>shall</b> be used in <see cref="GetStorages"/>.
		/// </summary>
		public const string StorageName = nameof(Storage);

		/// <summary>
		/// When implemented by a derived class, get all the storages of this array. The <see cref="Storage"/> must be associated with key <see cref="StorageName"/>.
		/// </summary>
		/// <returns>All the storages of the array as an <see cref="IReadOnlyDictionary{TKey, TValue}"/> of <see cref="string"/> and <see cref="IStorage"/></returns>
		public abstract IReadOnlyDictionary<string, IStorage> GetStorages();

		/// <summary>
		/// When implemented by a derived class, get other requisite informations for re-constructing the array of that derived class type.
		/// </summary>
		/// <returns>Other requisite informations used to re-construct this array</returns>
		public abstract IReadOnlyDictionary<string, object> GetMetaData();
		#endregion
	}
}
