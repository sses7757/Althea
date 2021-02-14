using System;
using System.Text;
using System.Collections.Generic;

using Althea.Linq;
using Althea.Helpers;
using Althea.NativeTypes;

using MEM = Althea.Storage.AbstractApi;
using LAD = Althea.LinearAlgebra.Dense.AbstractApi;


namespace Althea.Arrays
{
	/// <summary>
	/// The abstract array class with the only mutable <see cref="ValueArray{T}.Storage"/> that refers to the actual data storage. There may be more pointer(s) for different indices in a sparse array that inherits <see cref="ValueArray{T}"/>, but they shall be immutable.
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct that implements <see cref="IFormattable"/> and <see cref="IEquatable{T}"/> as the data type</typeparam>
	public abstract class ValueArray<T> : AbstractArray<T>, ICheckValid where T : unmanaged, IFormattable, IEquatable<T>
	{
		#region properties
		/// <summary>
		/// Get the raw storage of this array
		/// </summary>
		public Storage<T> Storage { get; }

		/// <summary>
		/// Get the total actual length of the value array in memory, in <typeparamref name="T"/> rather than bytes
		/// </summary>
		public virtual long ActualLength => this.Storage.Length;

		/// <summary>
		/// Check whether this object is a valid one or not
		/// </summary>
		/// <returns>The validness of this object</returns>
		public virtual bool IsValid() => this.Length > 0 && this.Storage is not null && this.Storage.IsValid();
		#endregion

		#region initialize and destroy
		/// <summary>
		/// Create a new <see cref="ValueArray{T}"/> using preallocated <paramref name="storage"/> and given <paramref name="size"/>
		/// </summary>
		/// <param name="storage">The preallocated <see cref="Storage{T}"/> (can be a <see cref="ReferenceStorage{T}"/>) as the underlying <see cref="Storage"/> of this array</param>
		/// <param name="size">The presenting size of this array</param>
		/// <exception cref="ArgumentException">If the product of <paramref name="size"/> is not the same as the length of <paramref name="storage"/></exception>
		protected ValueArray(Storage<T> storage, ReadOnlySpan<long> size) : base(size)
		{
			if (storage is null || !storage.IsValid())
				throw new ArgumentNullException(nameof(storage));
			if (this.Length != storage.Length)
				throw new ArgumentException(Resources.Parameter.NotSameSize);

			this.Storage = storage;
		}

		/// <summary>
		/// When implemented by a derived class, actually the dispose this array. The default implementation only disposes <see cref="Storage"/>
		/// </summary>
		/// <param name="disposing">Dispose managed resources or not</param>
		protected override void Dispose(bool disposing)
		{
			if (this.Disposed || this.Length == 0 || this.Storage is null)
			{
				return;
			}
			this.Storage.Dispose();
		}
		#endregion

		#region point-wise concrete operations
		/// <summary>
		/// When implemented by a derived class, fill this array's <see cref="Storage"/> with given <paramref name="value"/>. The default implementation utilizes <see cref="MEM.FillWithValue{T}(Storage{T}, T)"/>.
		/// </summary>
		/// <param name="value">The value as <typeparamref name="T"/> to fill</param>
		public virtual void FillWith(T value)
		{
			MEM.SelectImplementation(this.Storage).FillWithValue(this.Storage, value);
		}

		/// <summary>
		/// When implemented by a derived class, point-wisely in-place add this array's <see cref="Storage"/> with given <paramref name="value"/>. The default implementation utilizes <see cref="LAD.PointWiseAddScalar{T}"/>.
		/// </summary>
		/// <param name="value">The scalar as <typeparamref name="T"/> to add</param>
		public virtual void AddScalar(T value)
		{
			LAD.SelectImplementation(this.Storage).PointWiseAddScalar(this.Storage, 1, value);
		}

		/// <summary>
		/// When implemented by a derived class, point-wisely in-place multiply this array's <see cref="Storage"/> with given <paramref name="value"/>. The default implementation utilizes <see cref="LAD.Scale{T}"/>.
		/// </summary>
		/// <param name="value">The scalar as <typeparamref name="T"/> to multiply</param>
		public virtual void Scale(T value)
		{
			LAD.SelectImplementation(this.Storage).Scale(value, this.Storage, 1);
		}

		/// <summary>
		/// When implemented by a derived class, point-wisely in-place conjugate this array's <see cref="Storage"/>. The default implementation utilizes <see cref="LAD.PointWiseConjugate{T}"/>.
		/// </summary>
		public virtual void Conjugate(T value)
		{
			LAD.SelectImplementation(this.Storage).PointWiseConjugate(this.Storage, 1);
		}

		/// <summary>
		/// When implemented by a derived class, point-wisely in-place exponent this array's <see cref="Storage"/> with given <paramref name="power"/>. The default implementation utilizes <see cref="LAD.PointWisePower{T}(Storage{T}, int, double)"/>.
		/// </summary>
		/// <param name="power">The power as a <see cref="double"/></param>
		public virtual void Power(double power)
		{
			LAD.SelectImplementation(this.Storage).PointWisePower(this.Storage, 1, power);
		}

		/// <summary>
		/// When implemented by a derived class, point-wisely in-place exponent this array's <see cref="Storage"/> with given <paramref name="power"/>. The default implementation utilizes <see cref="LAD.PointWisePower{T}(Storage{T}, int, T)"/>.
		/// </summary>
		/// <param name="power">The power as a <typeparamref name="T"/></param>
		public virtual void Power(T power)
		{
			LAD.SelectImplementation(this.Storage).PointWisePower(this.Storage, 1, power);
		}

		/// <summary>
		/// When implemented by a derived class, point-wisely in-place truncate this array's <see cref="Storage"/> by comparing with given <paramref name="threshold"/>. The default implementation utilizes <see cref="LAD.PointWisePower{T}(Storage{T}, int, T)"/>.
		/// </summary>
		/// <param name="threshold">The threshold as a <see cref="float"/>. Any element in <see cref="Storage"/> whose absolute value ≤ <paramref name="threshold"/> will be set to 0.</param>
		public virtual void Truncate(float threshold)
		{
			LAD.SelectImplementation(this.Storage).TruncateArray(this.Storage, threshold);
		}

		/// <summary>
		/// When implemented by a derived class, aggregately sum the elements in this array. The default implementation only sums <see cref="Storage"/>, which is also valid if the actual derived class is dense or its emitted values are all <paramref name="sparseDefault"/>. <see cref="Storage"/> utilizes <see cref="LAD.AggregateSum{T}"/>.
		/// </summary>
		/// <param name="sparseDefault">The default emitted value if this array is a sparse array</param>
		/// <returns>The aggregate sum of this array</returns>
		public virtual T Sum(T sparseDefault = default)
		{
			T sum = LAD.SelectImplementation(this.Storage).AggregateSum(this.Storage, 1);
			if (this.Length == this.ActualLength || sparseDefault.IsZero())
				return sum;
			else
				return (this.Length - this.ActualLength) * (dynamic)sparseDefault + sum;
		}

		/// <summary>
		/// When implemented by a derived class, aggregately sum the absolute values of elements in this array. The default implementation only sums <see cref="Storage"/>, which is also valid if the actual derived class is dense or its emitted values are all <paramref name="sparseDefault"/>. <see cref="Storage"/> utilizes <see cref="LAD.AbsoluteValueSum{T}"/>.
		/// </summary>
		/// <param name="sparseDefault">The default (emitted) value if this array is a sparse array</param>
		/// <returns>The aggregate sum of absolute values of this array</returns>
		public virtual double AbsSum(T sparseDefault = default)
		{
			double sum = LAD.SelectImplementation(this.Storage).AbsoluteValueSum(this.Storage, 1);
			if (this.Length == this.ActualLength || sparseDefault.IsZero())
				return sum;
			else
				return (this.Length - this.ActualLength) * sparseDefault.GenericAbsolute().ToDouble() + sum;
		}

		/// <summary>
		/// When implemented by a derived class, compute the 2-norm (Euclidean norm) of elements in this array. The default implementation only sums <see cref="Storage"/>, which is also valid if the actual derived class is dense or its emitted values are all <paramref name="sparseDefault"/>. <see cref="Storage"/> utilizes <see cref="LAD.Norm{T}"/>.
		/// </summary>
		/// <param name="sparseDefault">The default (emitted) value if this array is a sparse array</param>
		/// <returns>The 2-norm of this array</returns>
		public virtual double Norm(T sparseDefault = default)
		{
			double norm = LAD.SelectImplementation(this.Storage).Norm(this.Storage, 1);
			if (this.Length == this.ActualLength || sparseDefault.IsZero())
			{
				return norm;
			}
			else
			{
				norm *= norm;
				double abs = sparseDefault.GenericAbsolute().ToDouble();
				norm += abs * abs * (this.Length - this.ActualLength);
				return Math.Sqrt(norm);
			}
		}

		/// <summary>
		/// When implemented by a derived class, in-place scale this array's <see cref="Storage"/> such that its 2-norm (Euclidean norm) is 1. The default implementation utilizes the <see cref="Norm(T)"/> and <see cref="Scale(T)"/>.
		/// </summary>
		/// <param name="sparseDefault">The default (emitted) value if this array is a sparse array</param>
		/// <exception cref="ArgumentOutOfRangeException">If the default values indicated by <paramref name="sparseDefault"/> alone contribute 2-norm exceeding 1.</exception>
		/// <exception cref="DivideByZeroException">If the 2-norm of this array is 0</exception>
		public virtual void Normalize(T sparseDefault = default)
		{
			if (this.Length == this.ActualLength || sparseDefault.IsZero())
			{
				double norm = this.Norm();
				if (norm == 0)
					throw new DivideByZeroException();
				this.Scale((1 / norm).FromDouble<T>());
			}
			else
			{
				T defaultNormSquare = (this.Length - this.ActualLength) * (sparseDefault * (dynamic)sparseDefault);
				double defaultNormDouble = defaultNormSquare.ToDouble();
				if (defaultNormDouble > 1)
					throw new ArgumentOutOfRangeException(nameof(sparseDefault), Resources.Parameter.InvalidValue);
				if (defaultNormDouble == 1)
				{
					this.FillWith(default);
				}
				double norm = this.Norm();
				if (norm == 0)
					throw new DivideByZeroException();
				this.Scale((Math.Sqrt(1 - defaultNormDouble) / norm).FromDouble<T>());
			}
		}
		#endregion

		#region reshape (mostly abstract)
		/// <summary>
		/// Take out the data <see cref="Storage"/> to form a new referenced <see cref="DenseVector{T}"/>.
		/// </summary>
		/// <returns>A new referenced <see cref="DenseVector{T}"/> containing the data <see cref="Storage"/> of this one.</returns>
		public DenseVector<T> AsDenseVector() => new DenseVector<T>(this, this.ActualLength);

		/// <summary>
		/// Check the new size (dimensionality) to reshape to with respect to the original <paramref name="array"/> and find out the uncertain dimension.
		/// </summary>
		/// <param name="array">The original array as a <see cref="ValueArray{T}"/> to check</param>
		/// <param name="newSize">The new size as a <see cref="Span{T}"/> to check which can have at most one uncertain dimension indicated by a non-positive number. Overwritten by the new size without uncertain dimension at exit.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="newSize"/> is of length 0</exception>
		/// <exception cref="ArgumentException">If <paramref name="newSize"/> is of length 2 and are all non-positive while the length of <paramref name="array"/> is not a perfect square</exception>
		/// <exception cref="ArgumentOutOfRangeException">If the product of <paramref name="newSize"/> is not the same as the presenting length of <paramref name="array"/></exception>
		protected static void CheckSize(ValueArray<T> array, Span<long> newSize)
		{
			if (newSize.Length == 0)
				throw new ArgumentNullException(nameof(newSize));
			if (newSize.Length == 2 && newSize[0] <= 0 && newSize[1] <= 0) // try to convert to a square matrix
			{
				if (!array.Length.IsPerfectSquare())
				{
					throw new ArgumentException(Resources.Other.PerfectSquare, nameof(array));
				}
				var leadDim = Convert.ToInt64(Math.Sqrt(array.Length));
				newSize[0] = newSize[1] = leadDim;
			}
			int firstFind = newSize.IndexOf(static r => r <= 0);
			if (firstFind < 0)
			{
				// no uncertain index
				if (newSize.Prod() != array.Length)
					throw new ArgumentOutOfRangeException(nameof(newSize));
				return;
			}
			int lastFind = newSize.LastIndexOf(static r => r <= 0);
			if (lastFind == firstFind)
			{
				// only one uncertainty
				newSize[firstFind] = 1;
				var prod = newSize.Prod();
				var remain = array.Length % prod;
				if (remain != 0)
					throw new ArgumentOutOfRangeException(nameof(newSize));
				else
					newSize[firstFind] = array.Length / prod;
			}
			else
			{
				// more than one uncertain indices
				throw new ArgumentOutOfRangeException(nameof(newSize));
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
			if (this.Size == newSize)
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
		/// When implemented by a derived class, reshape the array to a matrix with leading dimension = <paramref name="leadDim"/>
		/// </summary>
		/// <param name="leadDim">The leading dimension of target matrix; if <paramref name="leadDim"/> ≤ 0, it is assumed that leadDim = <c>sqrt(<see cref="AbstractArray{T}.Length"/>)</c>.</param>
		/// <returns>The reshaped matrix</returns>
		public abstract ValueArray<T> ToMatrix(long leadDim = 0);

		/// <summary>
		/// When implemented by a derived class, reshape the array to a tensor with dimensionality = <paramref name="size"/>.
		/// </summary>
		/// <param name="size">The new size/dimensionality with at most one or zero uncertain dimension indicated by a non-positive number.</param>
		/// <returns>The reshaped tensor</returns>
		public abstract ValueArray<T> ToTensor(Span<long> size);

		/// <summary>
		/// When implemented by a derived class, create a new array with same properties as this one while the underlying storages are not filled.
		/// </summary>
		/// <returns>The new array alike this one</returns>
		public abstract ValueArray<T> NewArrayAlike();

		/// <summary>
		/// When implemented by a derived class, create a new array with same properties as this one while the underlying storages are not filled and the data type is changed to <typeparamref name="TOut"/>.
		/// </summary>
		/// <typeparam name="TOut">Any unmanaged struct as the new data type</typeparam>
		/// <returns>The new array alike this one</returns>
		public abstract ValueArray<TOut> NewArrayAlike<TOut>() where TOut : unmanaged, IFormattable, IEquatable<TOut>;

		/// <summary>
		/// When implemented by a derived class, cast this array into another data type <typeparamref name="TOut"/>. The default implementation only casts the <see cref="Storage"/> of this array.
		/// </summary>
		/// <typeparam name="TOut">The data type to cast to</typeparam>
		/// <returns>The new <see cref="ValueArray{TOut}"/> casted from this array or this array if <typeparamref name="TOut"/> == <typeparamref name="T"/></returns>
		public override ValueArray<TOut> DataTypeCast<TOut>()
		{
			DataType typeT = default(T).ToDataType(), typeOut = default(TOut).ToDataType();
			if (typeT == typeOut)
			{
				var ret = this as ValueArray<TOut>;
				return ret ?? new DenseVector<TOut>(Storage<TOut>.Empty, 0);
			}
			var alike = this.NewArrayAlike<TOut>();
			LAD.SelectImplementation(this.Storage, alike.Storage).PointWiseCast(this.Storage, 1, alike.Storage, 1);
			return alike;
		}
		#endregion

		#region new methods and overrides
		/// <summary>
		/// When implemented by a derived class, check if this <see cref="ValueArray{T}"/> share some storage with the <paramref name="other"/> one
		/// </summary>
		/// <param name="other">The other <see cref="AbstractArray{T}"/> to check</param>
		/// <returns>True if they do share some storage, false otherwise</returns>
		/// <remarks>The default implementation only compares the <see cref="Storage"/>s</remarks>
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
			/// Add the term for the string representation of the current data type
			/// </summary>
			DataType,
			/// <summary>
			/// Add the term for the string representation of the current <see cref="Storage"/>
			/// </summary>
			Stroage,
			/// <summary>
			/// Add the term for the string representation of the current presenting size
			/// </summary>
			Size
		}

		/// <summary>
		/// Get the string representation of this array with new terms and existed ones (existed ones are shown at first).
		/// </summary>
		/// <param name="terms">The additional terms</param>
		/// <param name="include">The include terms, default null means all</param>
		/// <returns>the string representation</returns>
		protected string ToString(IReadOnlyDictionary<string, object>? terms, params StringTerms[] include)
		{
			// default values
			if (include is null || include.Length == 0)
				include = new[] { StringTerms.DataType, StringTerms.Stroage, StringTerms.Size };
			if (terms is null)
				terms = new Dictionary<string, object>(0);
			// get type name of this array
			var type = this.GetType();
			string? name;
			if (include.Contains(StringTerms.DataType))
			{
				name = type.GetGenericString();
			}
			else
			{
				name = type.Name;
				if (type.IsGenericType)
				{
					name = name.Replace($"`{type.GenericTypeArguments.Length}", "");
				}
			}
			// output include terms and other terms
			StringBuilder output = new StringBuilder(name);
			output.Append(this.Disposed ? " (disposed) " : " ").Append('[');
			foreach (var item in include)
			{
				output.Append(item switch
				{
					StringTerms.Stroage => $"value_storage={{{this.Storage}}}",
					StringTerms.Size => $"size={string.Join("x", this.Size)}",
					_ => "",
				});
				output.Append(", ");
			}
			foreach (var item in terms)
			{
				output.Append(item.Key).Append('=').Append(item.Value).Append(", ");
			}
			output.Remove(output.Length - 2, 2).Append(']');
			return output.ToString();
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
		/// When implemented by a derived class, get the hash code this array. The default 
		/// </summary>
		/// <returns>The hash code computed by <see cref="Storage"/> and <see cref="AbstractArray{T}.Size"/></returns>
		public override int GetHashCode() => HashCode.Combine(this.Storage, this.Size);

		/// <summary>
		/// When implemented by a derived class, check whether this object is equal to another one. The default implementation utilizes <see cref="AbstractArray{T}.Equals(object?)"/> and additionally compares <see cref="Storage"/>s.
		/// </summary>
		/// <param name="obj">The other object to compare with</param>
		/// <returns>True if this == <paramref name="obj"/></returns>
		public override bool Equals(object? obj)
		{
			if (obj is null || obj is not ValueArray<T> a)
				return false;
			else
				return base.Equals(obj) && this.Storage == a.Storage;
		}
		#endregion

		#region override operators
		/// <summary>
		/// When implemented by a derived class, deep clone the array, the mutable status will not be copied.
		/// </summary>
		/// <returns>The cloned array</returns>
		public override abstract ValueArray<T> Clone();

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
			var lad = LAD.SelectImplementation(array.Storage);
			long index;
			if (!value.IsZero())
			{
				using var clone = array.Storage.Clone();
				lad.PointWiseAddScalar(clone, 1, value.GenericNegate());
				index = lad.AbsoluteValueArgMax(clone, 1);
			}
			else
			{
				index = lad.AbsoluteValueArgMax(array.Storage, 1);
			}
			double val = MEM.SelectImplementation(array.Storage)
							.ToManaged(array.Storage.MakeReference(offset: index))
							.GenericAbsolute()
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
		/// The pointer name that <b>shall</b> be used in <see cref="GetPointers"/>.
		/// </summary>
		public const string StorageName = nameof(Storage);

		/// <summary>
		/// When implemented by a derived class, get all the storages of this array. The <see cref="Storage"/> must be associated with key <see cref="StorageName"/>.
		/// </summary>
		/// <returns>All the storages of the array as an <see cref="IReadOnlyDictionary{TKey, TValue}"/> of <see cref="string"/> and <see cref="IStorage"/></returns>
		public abstract IReadOnlyDictionary<string, IStorage> GetPointers();

		/// <summary>
		/// When implemented by a derived class, get other requisite informations for re-constructing the array of that derived class type.
		/// </summary>
		/// <returns>Other requisite informations used to re-construct this array</returns>
		public abstract IReadOnlyDictionary<string, object> GetOtherInfo();
		#endregion
	}
}
