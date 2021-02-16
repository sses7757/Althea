using System;
using System.Collections.Generic;

using Althea.Linq;
using Althea.NativeTypes;

using LAD = Althea.LinearAlgebra.Dense.AbstractApi;


namespace Althea.Arrays
{
	internal interface ISparseVector<T> : IReadOnlyList<IStorage>
	{
		long NonZero { get; }

		DataType IndexType { get; }

		T DefaultValue { get; }
	}

	/// <summary>
	/// The abstract sparse vector class with the only mutable <see cref="ValueArray{T}.Storage"/> that refers to the value array storage.
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct that implements <see cref="IFormattable"/> and <see cref="IEquatable{T}"/> as the data type</typeparam>
	/// <typeparam name="TIndex">Any integer-typed unmanaged struct as the index type</typeparam>
	public abstract class SparseVector<T, TIndex> : VectorBase<T>, ISparseVector<T>, ISparseArray<T, TIndex> where T : unmanaged, IFormattable, IEquatable<T> where TIndex : unmanaged
	{
		#region basic
		/// <summary>
		/// When implemented by a derived class, get the number of nonzero values of this sparse array. The default implementation returns the <see cref="ValueArray{T}.ActualLength"/>.
		/// </summary>
		public virtual long NonZero => this.ActualLength;

		/// <summary>
		/// Get or set the default value (the value not specified) of this sparse vector
		/// </summary>
		public T DefaultValue { get; protected set; }

		DataType ISparseVector<T>.IndexType => default(TIndex).ToDataType();

		private readonly Storage<TIndex> m_indexArray;

		private readonly Storage<TIndex>[]? m_indexArrays = null;

		/// <summary>
		/// Create a <see cref="SparseVector{T, TIndex}"/> with given <paramref name="length"/>, <paramref name="valueArray"/> and <paramref name="indexArray"/>
		/// </summary>
		/// <param name="length">The presenting length of this sparse vector</param>
		/// <param name="valueArray">The value array as a <see cref="Storage{T}"/> of <typeparamref name="T"/></param>
		/// <param name="indexArray">The index array as a <see cref="Storage{T}"/> of <typeparamref name="TIndex"/></param>
		/// <param name="defaultValue">The default value (the value not specified) of this sparse vector</param>
		/// <exception cref="NotSupportedException">If the <typeparamref name="TIndex"/> is not an integral type</exception>
		protected SparseVector(long length, Storage<T> valueArray, Storage<TIndex> indexArray, T defaultValue = default) : base(valueArray, length)
		{
			var type = default(TIndex).GetClassification();
			if (type != DataTypeClassification.SignedInteger && type != DataTypeClassification.UnsignedInteger)
				throw new NotSupportedException(Resources.Support.DataType);

			this.m_indexArray = indexArray; this.m_indexArrays = null; this.DefaultValue = defaultValue;
		}

		/// <summary>
		/// Create a <see cref="SparseVector{T, TIndex}"/> with given <paramref name="length"/>, <paramref name="valueArray"/> and <paramref name="indexArrays"/>
		/// </summary>
		/// <param name="length">The presenting length of this sparse vector</param>
		/// <param name="valueArray">The value array as a <see cref="Storage{T}"/> of <typeparamref name="T"/></param>
		/// <param name="defaultValue">The default value (the value not specified) of this sparse vector</param>
		/// <param name="indexArrays">The index array(s) as a list of <see cref="Storage{T}"/> of <typeparamref name="TIndex"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="indexArrays"/> is null or empty</exception>
		/// <exception cref="NotSupportedException">If the <typeparamref name="TIndex"/> is not an integral type</exception>
		protected SparseVector(long length, Storage<T> valueArray, T defaultValue = default, params Storage<TIndex>[] indexArrays) : base(valueArray, length)
		{
			if (indexArrays is null || indexArrays.Length == 0)
				throw new ArgumentNullException(nameof(indexArrays));
			var type = default(TIndex).GetClassification();
			if (type != DataTypeClassification.SignedInteger && type != DataTypeClassification.UnsignedInteger)
				throw new NotSupportedException(Resources.Support.DataType);

			if (indexArrays.Length == 1)
			{
				this.m_indexArray = indexArrays[0]; this.m_indexArrays = null;
			}
			else
			{
				this.m_indexArray = indexArrays[0]; this.m_indexArrays = (Storage<TIndex>[])indexArrays.Clone();
			}
			this.DefaultValue = defaultValue;
		}
		#endregion

		#region storage related
		/// <summary>
		/// When implemented by a derived class, check whether this sparse vector is a valid one or not. The default implementation only checks <see cref="AbstractArray{T}.Length"/>, <see cref="NonZero"/>, <see cref="ValueArray{T}.Storage"/> and the underlying index array(s) of this sparse vector.
		/// </summary>
		/// <returns>The validness of this array</returns>
		public override bool IsValid() => base.IsValid() && this.NonZero > 0 && this.m_indexArray is not null && this.m_indexArray.IsValid() && (this.m_indexArrays is null || this.m_indexArrays.All(static a => a is not null && a.IsValid()));

		/// <summary>
		/// When implemented by a derived class, check if this sparse vector share some storage(s) with the <paramref name="other"/> one. The default implementation only compares the <see cref="ValueArray{T}.Storage"/> and the index array(s).
		/// </summary>
		/// <param name="other">The other <see cref="ValueArray{T}"/> to check</param>
		/// <returns>True if they do share some storage, false otherwise</returns>
		public override bool OverlapWith(ValueArray<T> other)
		{
			if (base.OverlapWith(other))
				return true;
			if (other is not ISparseArray<T, TIndex> sparse)
				return false;
			// else
			var list = (IReadOnlyList<Storage<TIndex>>)this;
			var array = (IReadOnlyList<Storage<TIndex>>)sparse;
			for (int i = 0; i < list.Count; i++)
			{
				for (int j = 0; j < array.Count; j++)
				{
					if (list[i].OverlapWith(array[j]))
					{
						return true;
					}
				}
			}
			return false;
		}

		/// <summary>
		/// When implemented by a derived class, dispose this sparse array after excluding the internal storages shared between this array and the target <paramref name="array"/>.  The default implementation only compares <see cref="ValueArray{T}.Storage"/> and the index array(s) passed to the constructor of <see cref="SparseVector{T, TIndex}"/>.
		/// </summary>
		/// <param name="array">The target <see cref="ISparseArray{T, TIndex}"/> to exclude before disposing this sparse vector</param>
		public virtual void DisposeExclude(ISparseArray<T, TIndex> array)
		{
			var list = (IReadOnlyList<Storage<TIndex>>)this;
			var other = (IReadOnlyList<Storage<TIndex>>)array;
			if (!this.Storage.SameOriginAs(array.Storage))
				this.Storage.Dispose();
			for (int i = 0; i < list.Count; i++)
			{
				bool canDispose = true;
				for (int j = 0; j < other.Count; j++)
				{
					if (list[i].SameOriginAs(other[j]))
					{
						canDispose = false;
						break;
					}
				}
				if (canDispose)
					list[i].Dispose();
			}
		}

		/// <summary>
		/// When implemented by a derived class, actually the dispose this array. The default implementation only disposes <see cref="ValueArray{T}.Storage"/> and the index array(s) passed to the constructor of <see cref="SparseVector{T, TIndex}"/>.
		/// </summary>
		/// <param name="disposing">Dispose managed resources or not</param>
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			this.m_indexArray?.Dispose();
			if (this.m_indexArrays is not null)
			{
				for (int i = 0; i < this.m_indexArrays.Length; i++)
				{
					this.m_indexArrays[i]?.Dispose();
				}
			}
		}
		#endregion

		#region IReadOnlyList
		int IReadOnlyCollection<Storage<TIndex>>.Count => this.m_indexArrays?.Length ?? 1;

		int IReadOnlyCollection<IStorage>.Count => this.m_indexArrays?.Length ?? 1;

		IStorage IReadOnlyList<IStorage>.this[int index] => ((IReadOnlyList<Storage<TIndex>>)this)[index];

		Storage<TIndex> IReadOnlyList<Storage<TIndex>>.this[int index] {
			get {
				if (index < 0)
					throw new ArgumentOutOfRangeException(nameof(index), Resources.Parameter.CannotNegative);
				if (index >= this.Count)
					throw new ArgumentOutOfRangeException(nameof(index), Resources.Parameter.InvalidValue);
				return this.m_indexArrays?[index] ?? this.m_indexArray;
			}
		}

		IEnumerator<Storage<TIndex>> IEnumerable<Storage<TIndex>>.GetEnumerator()
		{
			var list = (IReadOnlyList<Storage<TIndex>>)this;
			for (int i = 0; i < list.Count; i++)
			{
				yield return list[i];
			}
		}

		IEnumerator<IStorage> IEnumerable<IStorage>.GetEnumerator() => ((IReadOnlyList<Storage<TIndex>>)this).GetEnumerator();
		#endregion

		#region point-wise method override
		/// <summary>
		/// When implemented by a derived class, fill this sparse array with given <paramref name="value"/>. The default implementation utilizes <see cref="ValueArray{T}.FillWith(T)"/> and sets <see cref="DefaultValue"/> to <paramref name="value"/>.
		/// </summary>
		/// <param name="value">The value as <typeparamref name="T"/> to fill</param>
		public override void FillWith(T value)
		{
			base.FillWith(value);
			this.DefaultValue = value;
		}

		/// <summary>
		/// When implemented by a derived class, point-wisely in-place add this sparse vector with given <paramref name="value"/>. The default implementation utilizes <see cref="ValueArray{T}.AddScalar(T)"/> and adds <paramref name="value"/> to <see cref="DefaultValue"/>.
		/// </summary>
		/// <param name="value">The scalar as <typeparamref name="T"/> to add</param>
		public override void AddScalar(T value)
		{
			base.AddScalar(value);
			this.DefaultValue = this.DefaultValue.GenericAdd(value);
		}

		/// <summary>
		/// When implemented by a derived class, point-wisely in-place multiply this sparse vector with given <paramref name="value"/>. The default implementation utilizes <see cref="ValueArray{T}.Scale(T)"/> and multiplies <paramref name="value"/> to <see cref="DefaultValue"/>.
		/// </summary>
		/// <param name="value">The scalar as <typeparamref name="T"/> to multiply</param>
		public override void Scale(T value)
		{
			base.Scale(value);
			this.DefaultValue = this.DefaultValue.GenericMultiply(value);
		}

		/// <summary>
		/// When implemented by a derived class, point-wisely in-place conjugate this array's <see cref="Storage"/>. The default implementation utilizes <see cref="ValueArray{T}.Conjugate"/> and sets the <see cref="DefaultValue"/> to its conjugate.
		/// </summary>
		public override void Conjugate()
		{
			base.Conjugate();
			this.DefaultValue = this.DefaultValue.GenericConjugate();
		}

		/// <summary>
		/// When implemented by a derived class, point-wisely in-place exponent this array's <see cref="Storage"/> with given <paramref name="power"/>. The default implementation utilizes <see cref="ValueArray{T}.Power(double)"/> and powers <see cref="DefaultValue"/> by <paramref name="power"/>.
		/// </summary>
		/// <param name="power">The power as a <see cref="double"/></param>
		public override void Power(double power)
		{
			base.Power(power);
			this.DefaultValue = this.DefaultValue.GenericPower(power);
		}

		/// <summary>
		/// When implemented by a derived class, point-wisely in-place exponent this sparse vector with given <paramref name="power"/>. The default implementation utilizes <see cref="ValueArray{T}.Power(T)"/> and powers <see cref="DefaultValue"/> by <paramref name="power"/>.
		/// </summary>
		/// <param name="power">The power as a <typeparamref name="T"/></param>
		public override void Power(T power)
		{
			base.Power(power);
			this.DefaultValue = this.DefaultValue.GenericPower(power);
		}

		/// <summary>
		/// When implemented by a derived class, point-wisely in-place truncate this sparse vector by comparing with given <paramref name="threshold"/>. The default implementation utilizes <see cref="ValueArray{T}.Truncate(double)"/> and sets <see cref="DefaultValue"/> to 0 if it is smaller than or equals to <paramref name="threshold"/>.
		/// </summary>
		/// <param name="threshold">The threshold as a <see cref="double"/>. Any element whose absolute value ≤ <paramref name="threshold"/> will be set to 0.</param>
		public override void Truncate(double threshold)
		{
			base.Truncate(threshold);
			if (!this.DefaultValue.IsZero())
			{
				double abs = this.DefaultValue.GenericAbsolute();
				if (abs <= threshold)
					this.DefaultValue = default;
			}
		}


		/// <summary>
		/// When implemented by a derived class, aggregately sum the elements in this sparse vector. The default implementation only sums <see cref="ValueArray{T}.Storage"/> and <see cref="DefaultValue"/> by utilizing <see cref="ValueArray{T}.Sum(T)"/>.
		/// </summary>
		/// <returns>The aggregate sum of this sparse vector</returns>
		public override T Sum() => this.Sum(this.DefaultValue);

		/// <summary>
		/// When implemented by a derived class, aggregately sum the absolute values of elements in this sparse vector. The default implementation only sums <see cref="ValueArray{T}.Storage"/> and <see cref="DefaultValue"/> by utilizing <see cref="ValueArray{T}.AbsSum(T)"/>.
		/// </summary>
		/// <returns>The aggregate sum of absolute values of this sparse vector</returns>
		public override double AbsSum() => this.AbsSum(this.DefaultValue);

		/// <summary>
		/// When implemented by a derived class, compute the 2-norm (Euclidean norm) of elements in this sparse vector. The default implementation only sums <see cref="ValueArray{T}.Storage"/> and <see cref="DefaultValue"/> by utilizing <see cref="ValueArray{T}.Norm(T)"/>.
		/// </summary>
		/// <returns>The 2-norm of this sparse vector</returns>
		public override double Norm() => this.Norm(this.DefaultValue);

		/// <summary>
		/// When implemented by a derived class, in-place scale this sparse vector such that its 2-norm (Euclidean norm) is 1. The default implementation utilizes <see cref="ValueArray{T}.Normalize(T)"/>.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">If the <see cref="DefaultValue"/> alone contribute 2-norm exceeding 1.</exception>
		/// <exception cref="DivideByZeroException">If the 2-norm of this array is 0</exception>
		public override void Normalize() => this.Normalize(this.DefaultValue);
		#endregion

		#region clone related
		/// <summary>
		/// When implemented by a derived class, deep clone the array, the mutable status will not be copied.
		/// </summary>
		/// <returns>The cloned array</returns>
		public override abstract SparseVector<T, TIndex> Clone();

		/// <summary>
		/// The helper method of <see cref="Clone()"/> used to create the value array and index array(s) of this sparse vector
		/// </summary>
		/// <param name="valueArray">The cloned output value array</param>
		/// <param name="firstIndexArray">The cloned output first index array</param>
		/// <param name="indexArrays">The cloned output all index arrays, may be null if there is only one index array</param>
		protected void Clone(out ActualStorage<T> valueArray, out ActualStorage<TIndex> firstIndexArray, out ActualStorage<TIndex>[]? indexArrays)
		{
			ActualStorage<T>? value = null;
			ActualStorage<TIndex>? index = null;
			ActualStorage<TIndex>[]? indices = null;
			try
			{
				value = this.Storage.Clone();
				if (this.m_indexArrays is null)
				{
					index = this.m_indexArray.Clone();
				}
				else
				{
					indices = new ActualStorage<TIndex>[this.m_indexArrays.Length];
					for (int i = 0; i < this.m_indexArrays.Length; i++)
					{
						indices[i] = this.m_indexArrays[i].Clone();
					}
					index = indices[0];
				}
				valueArray = value;
				firstIndexArray = index;
				indexArrays = indices;
			}
			catch (Exception)
			{
				value?.Dispose();
				index?.Dispose();
				indices?.ForEach(static a => a?.Dispose());
				throw;
			}
		}

		/// <summary>
		/// When implemented by a derived class, create a new array with same properties as this one while the underlying storages are not filled.
		/// </summary>
		/// <returns>The new array alike this one</returns>
		/// <exception cref="InvalidOperationException"><b>Always</b> throw this exception since the index array(s) are supposed to be immutable</exception>
		public override SparseVector<T, TIndex> NewArrayAlike() => throw new InvalidOperationException();

		/// <summary>
		/// When implemented by a derived class, create a new array with same properties as this one while the underlying storages are not filled and the data type is changed to <typeparamref name="TOut"/>.
		/// </summary>
		/// <typeparam name="TOut">Any unmanaged struct as the new data type</typeparam>
		/// <returns>The new array alike this one</returns>
		/// <exception cref="InvalidOperationException"><b>Always</b> throw this exception since the index array(s) are supposed to be immutable</exception>
		public override SparseVector<TOut, TIndex> NewArrayAlike<TOut>() => throw new InvalidOperationException();

		/// <summary>
		/// When implemented by a derived class, cast this array into another data type <typeparamref name="TOut"/>. The default implementation only casts the <see cref="Storage"/> of this array.
		/// </summary>
		/// <typeparam name="TOut">The data type to cast to</typeparam>
		/// <returns>The new <see cref="ValueArray{TOut}"/> casted from this array or this array if <typeparamref name="TOut"/> == <typeparamref name="T"/></returns>
		public override abstract SparseVector<TOut, TIndex> DataTypeCast<TOut>();

		/// <summary>
		/// The helper method of <see cref="DataTypeCast{TOut}()"/> used to create an storage casted from the value array of this sparse vector
		/// </summary>
		/// <typeparam name="TOut">The data type to cast to</typeparam>
		/// <param name="valueArray">A created storage casted from the value array of this sparse vector, or <see cref="ValueArray{T}.Storage"/> if <typeparamref name="T"/> == <typeparamref name="TOut"/></param>
		protected void DataTypeCast<TOut>(out Storage<TOut> valueArray) where TOut : unmanaged, IFormattable, IEquatable<TOut>
		{
			DataType typeT = default(T).ToDataType(), typeOut = default(TOut).ToDataType();
			if (typeT == typeOut)
			{
				valueArray = this.Storage as Storage<TOut> ?? Storage<TOut>.Empty;
				return;
			}
			valueArray = Althea.Storage.StorageFactory<T>.CreateAlike<TOut>(this.Storage);
			try
			{
				LAD.SelectImplementation(this.Storage, valueArray).PointWiseCast(this.Storage, 1, valueArray, 1);
			}
			catch (Exception)
			{
				valueArray?.Dispose();
				throw;
			}
		}
		#endregion

		#region print

		#endregion

		#region serialization

		#endregion
	}
}
