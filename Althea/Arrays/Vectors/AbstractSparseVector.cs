using System;
using System.Collections.Generic;

using Althea.Linq;
using Althea.Helpers;
using Althea.NativeTypes;
using Althea.LinearAlgebra.Sparse;

using MEM = Althea.Storage.AbstractApi;
using LAD = Althea.LinearAlgebra.Dense.AbstractApi;


namespace Althea.Arrays
{
	/// <summary>
	/// The abstract sparse vector class with the only mutable <see cref="ValueArray{T}.Storage"/> that refers to the value array storage.
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct that implements <see cref="IFormattable"/> and <see cref="IEquatable{T}"/> as the data type</typeparam>
	/// <typeparam name="TInd">Any integer-typed unmanaged struct as the index type</typeparam>
	public abstract class AbstractSparseVector<T, TInd> : VectorBase<T>, ISparseVector<T>, ISparseArray<T, TInd>
		where T : unmanaged, IFormattable, IEquatable<T>
		where TInd : unmanaged
	{
		#region basic
		/// <summary>
		/// When implemented by a derived class, get the number of stored values of this sparse array. The default implementation returns the <see cref="ValueArray{T}.ActualLength"/>.
		/// </summary>
		public virtual long NStored => this.ActualLength;

		/// <summary>
		/// Get the sparse format of this sparse vector as a <see cref="SparseVectorFormat"/>
		/// </summary>
		public SparseVectorFormat Format { get; }

		/// <summary>
		/// Get or set the default value (the value not specified) of this sparse vector
		/// </summary>
		public T DefaultValue { get; protected set; }

		/// <summary>
		/// The <see cref="DataType"/> of the type parameter <typeparamref name="TInd"/>
		/// </summary>
		protected static readonly DataType indexDataType = default(TInd).ToDataType();

		DataType ISparseArray<T>.IndexType => indexDataType;

		/// <summary>
		/// The member of first index array as a <see cref="Storage{T}"/> of <typeparamref name="TInd"/>
		/// </summary>
		protected readonly Storage<TInd> m_indexArray;

		/// <summary>
		/// The member of all the index arrays as an array of <see cref="Storage{T}"/> of <typeparamref name="TInd"/>, is null if there is only one index array
		/// </summary>
		protected readonly Storage<TInd>[]? m_indexArrays = null;

		/// <summary>
		/// Create a <see cref="AbstractSparseVector{T, TInd}"/> with given <paramref name="length"/>, <paramref name="valueArray"/> and <paramref name="indexArray"/>
		/// </summary>
		/// <param name="length">The presenting length of this sparse vector</param>
		/// <param name="valueArray">The value array as a <see cref="Storage{T}"/> of <typeparamref name="T"/></param>
		/// <param name="indexArray">The index array as a <see cref="Storage{T}"/> of <typeparamref name="TInd"/></param>
		/// <param name="format">The <see cref="SparseVectorFormat"/> of this sparse vector, must be atomic</param>
		/// <param name="defaultValue">The default value (the value not specified) of this sparse vector</param>
		/// <exception cref="NotSupportedException">If the <typeparamref name="TInd"/> is not an real integral type</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="format"/> is not atomic</exception>
		protected AbstractSparseVector(long length, Storage<T> valueArray, Storage<TInd> indexArray, SparseVectorFormat format, T defaultValue = default) : base(valueArray, length)
		{
			var type = default(TInd).GetClassification();
			if (type.IsComplex() || (type != DataTypeClassification.SignedInteger && type != DataTypeClassification.UnsignedInteger))
				throw new NotSupportedException(Resources.Support.DataType);
			if (!format.IsAtomic())
				throw new ArgumentOutOfRangeException(nameof(format), Resources.Parameter.InvalidValue);

			this.m_indexArray = indexArray; this.m_indexArrays = null;
			this.Format = format; this.DefaultValue = defaultValue;
		}

		/// <summary>
		/// Create a <see cref="AbstractSparseVector{T, TInd}"/> with given <paramref name="length"/>, <paramref name="valueArray"/> and <paramref name="indexArrays"/>
		/// </summary>
		/// <param name="length">The presenting length of this sparse vector</param>
		/// <param name="valueArray">The value array as a <see cref="Storage{T}"/> of <typeparamref name="T"/></param>
		/// <param name="indexArrays">The index array(s) as a list of <see cref="Storage{T}"/> of <typeparamref name="TInd"/></param>
		/// <param name="format">The <see cref="SparseVectorFormat"/> of this sparse vector, must be atomic</param>
		/// <param name="defaultValue">The default value (the value not specified) of this sparse vector</param>
		/// <exception cref="ArgumentNullException">If <paramref name="indexArrays"/> is null or empty</exception>
		/// <exception cref="NotSupportedException">If the <typeparamref name="TInd"/> is not an integral type</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="format"/> is not atomic</exception>
		protected AbstractSparseVector(long length, Storage<T> valueArray, Storage<TInd>[] indexArrays, SparseVectorFormat format, T defaultValue = default) : base(valueArray, length)
		{
			if (indexArrays is null || indexArrays.Length == 0)
				throw new ArgumentNullException(nameof(indexArrays));
			var type = default(TInd).GetClassification();
			if (type != DataTypeClassification.SignedInteger && type != DataTypeClassification.UnsignedInteger)
				throw new NotSupportedException(Resources.Support.DataType);
			if (!format.IsAtomic())
				throw new ArgumentOutOfRangeException(nameof(format), Resources.Parameter.InvalidValue);

			if (indexArrays.Length == 1)
			{
				this.m_indexArray = indexArrays[0]; this.m_indexArrays = null;
			}
			else
			{
				this.m_indexArray = indexArrays[0]; this.m_indexArrays = (Storage<TInd>[])indexArrays.Clone();
			}
			this.Format = format; this.DefaultValue = defaultValue;
		}
		#endregion

		#region storage related
		/// <summary>
		/// When implemented by a derived class, check whether this sparse vector is a valid one or not. The default implementation only checks <see cref="AbstractArray{T}.Length"/>, <see cref="NStored"/>, <see cref="ValueArray{T}.Storage"/> and the underlying index array(s) of this sparse vector.
		/// </summary>
		/// <returns>The validness of this array</returns>
		public override bool IsValid() => base.IsValid() && this.NStored > 0 && this.m_indexArray is not null && this.m_indexArray.IsValid() && (this.m_indexArrays is null || this.m_indexArrays.All(static a => a is not null && a.IsValid()));

		/// <summary>
		/// When implemented by a derived class, check if this sparse vector share some storage(s) with the <paramref name="other"/> one. The default implementation only compares the <see cref="ValueArray{T}.Storage"/> and the index array(s).
		/// </summary>
		/// <param name="other">The other <see cref="ValueArray{T}"/> to check</param>
		/// <returns>True if they do share some storage, false otherwise</returns>
		public override bool OverlapWith(ValueArray<T> other)
		{
			if (base.OverlapWith(other))
				return true;
			if (other is not ISparseArray<T, TInd> sparse)
				return false;
			// else
			var list = (IReadOnlyList<Storage<TInd>>)this;
			var array = (IReadOnlyList<Storage<TInd>>)sparse;
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
		/// When implemented by a derived class, dispose this sparse array after excluding the internal storages shared between this array and the target <paramref name="array"/>. The default implementation only compares <see cref="ISparseArray{T}.Storage"/> and the index array(s) implied in <see cref="ISparseArray{T}"/>.
		/// </summary>
		/// <param name="array">The target <see cref="ISparseArray{T}"/> to exclude before disposing this sparse vector</param>
		public virtual void DisposeExclude(ISparseArray<T> array)
		{
			var list = (IReadOnlyList<IStorage>)this;
			var other = (IReadOnlyList<IStorage>)array;
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

		void ISparseArray<T, TInd>.DisposeExclude(ISparseArray<T, TInd> array) => this.DisposeExclude(array);

		/// <summary>
		/// When implemented by a derived class, actually the dispose this array. The default implementation only disposes <see cref="ValueArray{T}.Storage"/> and the index array(s) passed to the constructor of <see cref="AbstractSparseVector{T, TInd}"/>.
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
		int IReadOnlyCollection<Storage<TInd>>.Count => this.m_indexArrays?.Length ?? 1;

		int IReadOnlyCollection<IStorage>.Count => this.m_indexArrays?.Length ?? 1;

		IStorage IReadOnlyList<IStorage>.this[int index] => ((IReadOnlyList<Storage<TInd>>)this)[index];

		Storage<TInd> IReadOnlyList<Storage<TInd>>.this[int index] {
			get {
				if (index < 0)
					throw new ArgumentOutOfRangeException(nameof(index), Resources.Parameter.CannotNegative);
				if (index >= this.Count)
					throw new ArgumentOutOfRangeException(nameof(index), Resources.Parameter.InvalidValue);
				return this.m_indexArrays?[index] ?? this.m_indexArray;
			}
		}

		IEnumerator<Storage<TInd>> IEnumerable<Storage<TInd>>.GetEnumerator()
		{
			var list = (IReadOnlyList<Storage<TInd>>)this;
			for (int i = 0; i < list.Count; i++)
			{
				yield return list[i];
			}
		}

		IEnumerator<IStorage> IEnumerable<IStorage>.GetEnumerator() => ((IReadOnlyList<Storage<TInd>>)this).GetEnumerator();
		#endregion

		#region new method
		/// <summary>
		/// Convert this sparse vector to a dense vector
		/// </summary>
		/// <returns>The converted <see cref="Backend.Arrays.DenseVector{T}"/></returns>
		public abstract Backend.Arrays.DenseVector<T> ToDense();
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
		/// When implemented by a derived class, deep clone the sparse vector, the mutable status will not be copied.
		/// </summary>
		/// <returns>The cloned sparse vector</returns>
		public override abstract AbstractSparseVector<T, TInd> Clone();

		/// <summary>
		/// The helper method of <see cref="Clone()"/> used to clone the value array and index array(s) of this sparse vector
		/// </summary>
		/// <param name="valueArray">The cloned output value array</param>
		/// <param name="firstIndexArray">The cloned output first index array</param>
		/// <param name="indexArrays">The cloned output all index arrays, may be null if there is only one index array</param>
		protected void Clone(out ActualStorage<T> valueArray, out ActualStorage<TInd> firstIndexArray, out ActualStorage<TInd>[]? indexArrays)
		{
			ActualStorage<T>? value = null;
			ActualStorage<TInd>? index = null;
			ActualStorage<TInd>[]? indices = null;
			try
			{
				value = this.Storage.Clone();
				if (this.m_indexArrays is null)
				{
					index = this.m_indexArray.Clone();
				}
				else
				{
					indices = new ActualStorage<TInd>[this.m_indexArrays.Length];
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
		/// The helper method of <see cref="NewArrayAlike()"/> used to create new storages alike the value array and index array(s) of this sparse vector
		/// </summary>
		/// <param name="valueArray">The output alike new storage of value array</param>
		/// <param name="firstIndexArray">The output alike new storage  first index array</param>
		/// <param name="indexArrays">The output alike new storages of all index arrays, may be null if there is only one index array</param>
		protected void NewArrayAlike<TOut>(out ActualStorage<TOut> valueArray, out ActualStorage<TInd> firstIndexArray, out ActualStorage<TInd>[]? indexArrays) where TOut : unmanaged
		{
			ActualStorage<TOut>? value = null;
			ActualStorage<TInd>? index = null;
			ActualStorage<TInd>[]? indices = null;
			try
			{
				value = Althea.Storage.StorageFactory<T>.CreateAlike<TOut>(this.Storage);
				if (this.m_indexArrays is null)
				{
					index = Althea.Storage.StorageFactory<TInd>.CreateAlike(this.m_indexArray);
				}
				else
				{
					indices = new ActualStorage<TInd>[this.m_indexArrays.Length];
					for (int i = 0; i < this.m_indexArrays.Length; i++)
					{
						indices[i] = Althea.Storage.StorageFactory<TInd>.CreateAlike(this.m_indexArrays[i]);
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
		/// When implemented by a derived class, create a new sparse vector with same properties as this one while the underlying storages are not filled.
		/// </summary>
		/// <returns>The new sparse vector alike this one</returns>
		public override abstract AbstractSparseVector<T, TInd> NewArrayAlike();

		/// <summary>
		/// When implemented by a derived class, create a new sparse vector with same properties as this one while the underlying storages are not filled and the data type is changed to <typeparamref name="TOut"/>.
		/// </summary>
		/// <typeparam name="TOut">Any unmanaged struct as the new data type</typeparam>
		/// <returns>The new sparse vector alike this one</returns>
		public override abstract AbstractSparseVector<TOut, TInd> NewArrayAlike<TOut>();

		/// <summary>
		/// When implemented by a derived class, cast this array into another data type <typeparamref name="TOut"/>. The default implementation only casts the <see cref="Storage"/> of this array.
		/// </summary>
		/// <typeparam name="TOut">The data type to cast to</typeparam>
		/// <returns>The new <see cref="ValueArray{TOut}"/> casted from this array or this array if <typeparamref name="TOut"/> == <typeparamref name="T"/></returns>
		public override abstract AbstractSparseVector<TOut, TInd> DataTypeCast<TOut>();

		/// <summary>
		/// The helper method of <see cref="DataTypeCast{TOut}()"/> used to create an storage casted from the value array of this sparse vector
		/// </summary>
		/// <typeparam name="TOut">The data type to cast to</typeparam>
		/// <param name="valueArray">A created storage casted from the value array of this sparse vector, or <see cref="ValueArray{T}.Storage"/> if <typeparamref name="T"/> == <typeparamref name="TOut"/></param>
		protected void DataTypeCast<TOut>(out Storage<TOut> valueArray) where TOut : unmanaged, IFormattable, IEquatable<TOut>
		{
			if (typeof(T) == typeof(TOut))
			{
				valueArray = this.Storage as Storage<TOut> ?? Storage<TOut>.Empty;
				return;
			}
			valueArray = Althea.Storage.StorageFactory<T>.CreateAlike<TOut>(this.Storage);
			try
			{
				LAD.PointWiseCast(this.Storage, 1, valueArray, 1);
			}
			catch (Exception)
			{
				valueArray?.Dispose();
				throw;
			}
		}
		#endregion

		#region equality
		/// <summary>
		/// When implemented by a derived class, get the hash code this sparse vector. The default implementation only takes <see cref="ValueArray{T}.Storage"/> and the index array(s) used to construct this sparse vector into account.
		/// </summary>
		/// <returns>The hash code of <see cref="ValueArray{T}.Storage"/> and the index array(s) used to construct this sparse vector</returns>
		public override int GetHashCode() => this.m_indexArrays is null ? HashCode.Combine(this.Storage, this.m_indexArray) : HashCode.Combine(this.Storage, this.m_indexArrays.HashCodeOfArray());

		/// <summary>
		/// When implemented by a derived class, check whether this sparse vector is equal to another one. The default implementation only compares <see cref="ValueArray{T}.Storage"/> and the index array(s) used to construct this sparse vector.
		/// </summary>
		/// <param name="obj">The other object to compare with</param>
		/// <returns>True if this == <paramref name="obj"/></returns>
		public override bool Equals(object? obj)
		{
			if (!(obj is AbstractSparseVector<T, TInd> sv && this.Storage == sv.Storage))
				return false;
			var list1 = (IReadOnlyList<Storage<TInd>>)this;
			var list2 = (IReadOnlyList<Storage<TInd>>)sv;
			return list1.SequenceEqual(list2);
		}
		#endregion

		#region print
		/// <summary>
		/// The helper method used in <see cref="Print(PrintSettings?)"/> to get the first several indices of this sparse vector
		/// </summary>
		/// <param name="indices">The <see cref="Span{T}"/> of <see cref="long"/> used to store the indices</param>
		protected abstract void GetIndices(Span<long> indices);

		/// <summary>
		/// When implemented by a derived class, print out this sparse vector.
		/// </summary>
		/// <param name="overrideSetting">Override global settings in <see cref="Settings"/></param>
		/// <returns>The detailed string representation of this sparse vector</returns>
		public override string Print(PrintSettings? overrideSetting = null)
		{
			string description = this.ToString();
			if (this.Disposed)
				return description;

			var settings = overrideSetting ?? Settings.PrintSetting;

			string detail = ":" + Environment.NewLine;
			// get managed arrays
			int length = (int)Math.Min(settings.ArrayLength, this.NStored);
			Span<T> values = length.CheckStockLimit<T>() ?? stackalloc T[length];
			MEM.ToManaged(this.Storage, values);
			Span<long> indices = length.CheckStockLimit<long>() ?? stackalloc long[length];
			this.GetIndices(indices);
			// to vector string
			detail += values.ToSparseVectorString(indices, precision: settings.Precision);
			if (this.Length > values.Length)
				detail += Environment.NewLine + $"...{this.NStored - values.Length} more stored elements";
			return description + detail;
		}
		#endregion

		#region serialization
		/// <summary>
		/// The helper method used by <see cref="GetPointers"/> to get the index storages' names. Only used when the sparse array contains more than one index storages.
		/// </summary>
		/// <param name="orderOfIndexStorage">The index of all index storages of this sparse vector</param>
		/// <returns>The name the index storage indicated by the given <paramref name="orderOfIndexStorage"/></returns>
		protected abstract string IndexStorageNameOf(int orderOfIndexStorage);

		/// <summary>
		/// The name of the index storage to be used when the sparse array only contains one index storage
		/// </summary>
		protected const string IndexStorageName = @"IndexStorage";

		/// <summary>
		/// When implemented by a derived class, get all the storages of this sparse vector. The default implementation returns the <see cref="ValueArray{T}.Storage"/> and the index array(s) (whose names are from <see cref="IndexStorageNameOf(int)"/>) used to construct this sparse vector.
		/// </summary>
		/// <returns>All the storages of the array as an <see cref="IReadOnlyDictionary{TKey, TValue}"/> of <see cref="string"/> and <see cref="IStorage"/></returns>
		public override IReadOnlyDictionary<string, IStorage> GetPointers()
		{
			if (this.m_indexArrays is null)
			{
				return new Dictionary<string, IStorage>(2) { [StorageName] = this.Storage, [IndexStorageName] = this.m_indexArray };
			}
			var dict = new Dictionary<string, IStorage>(this.m_indexArrays.Length + 1) { [StorageName] = this.Storage };
			for (int i = 0; i < this.m_indexArrays.Length; i++)
			{
				dict.Add(this.IndexStorageNameOf(i), this.m_indexArrays[i]);
			}
			return dict;
		}
		#endregion
	}
}
