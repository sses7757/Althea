using System;
using System.Collections.Generic;

using Althea.Linq;
using Althea.Helpers;
using Althea.NativeTypes;
using Althea.LinearAlgebra.Sparse;

using MEM = Althea.Storage.AbstractApi;


namespace Althea.Arrays
{
	/// <summary>
	/// The abstract sparse vector class with the only mutable <see cref="ValueArray{T}.Storage"/> that refers to the value array storage.
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct that implements <see cref="IFormattable"/> and <see cref="IEquatable{T}"/> as the data type</typeparam>
	/// <typeparam name="TInd">Any integer-typed unmanaged struct as the index type</typeparam>
	public abstract class SparseVector<T, TInd> : VectorBase<T>, ISparseVector<T>, ISparseArray<T, TInd>
		where T : unmanaged, IFormattable, IEquatable<T>
		where TInd : unmanaged
	{
		#region basic
		// offset = 0
		private readonly FixedClassBuffer_8<Storage<TInd>> m_originalIndexArrays;
		// offset = 64
		/// <summary>
		/// The member of all the index arrays as an array of <see cref="Storage{T}"/> of <typeparamref name="TInd"/>, is null if there is only one index array
		/// </summary>
		protected readonly SizedFixedClassBuffer_8<Storage<TInd>> m_indexArrays;

		// offset = 132
		private readonly SparseVectorFormat m_format;

		// offset = 136
		private T m_defaultValue;
		// offset = 136 + size of T

		/// <summary>
		/// When implemented by a derived class, get the number of stored values of this sparse vector. The default implementation returns the <see cref="ValueArray{T}.ActualLength"/>.
		/// </summary>
		public virtual long NStored => this.ActualLength;

		/// <summary>
		/// Get the sparse format of this sparse vector as a <see cref="SparseVectorFormat"/>
		/// </summary>
		public SparseVectorFormat Format => this.m_format;

		/// <summary>
		/// Get or set the default value (the value not specified) of this sparse vector
		/// </summary>
		public T DefaultValue { get => this.m_defaultValue; protected internal set => this.m_defaultValue = value; }

		T ISparseArray<T>.DefaultValue { get => this.DefaultValue; set => this.DefaultValue = value; }

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private static void CheckTypeFormat(SparseVectorFormat format)
		{
			if (!default(TInd).IsIntegralType())
				throw new TypeMismatchException(typeof(TInd), TypeMismatchException.MismatchReason.NotInteger);
			if (!format.IsAtomic())
				throw new ArgumentOutOfRangeException(nameof(format), format, Resources.Parameter.InvalidValue);
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private SparseVector(long length, Storage<T> valueArray, long stores, SizedFixedClassBuffer_8<Storage<TInd>> indexArrays, ReadOnlySpan<long> indexRealLengths, SparseVectorFormat format, T defaultValue) : base(valueArray, length, stores)
		{
			CheckTypeFormat(format);
			if (indexArrays.Count != indexRealLengths.Length)
				throw new ArgumentException(Resources.Parameter.NotSameSize);
			if (indexArrays.Any(static a => a is null || !a.IsValid()))
				throw new ArgumentNullException(nameof(indexArrays));

			this.m_originalIndexArrays = indexArrays;
			this.m_indexArrays = new SizedFixedClassBuffer_8<Storage<TInd>>(indexArrays.Count);
			for (int i = 0; i < indexArrays.Count; i++)
			{
				long len = indexRealLengths[i];
				if (len < 0)
					throw new ArgumentOutOfRangeException(nameof(indexRealLengths), len, Resources.Parameter.CannotNegative);
				if (len == 0)
					this.m_indexArrays[i] = indexArrays[i].MakeReference();
				else
					this.m_indexArrays[i] = indexArrays[i].MakeReference(newLength: len);
			}
			this.m_format = format; this.m_defaultValue = defaultValue;
		}

		/// <summary>
		/// Create a <see cref="SparseVector{T, TInd}"/> with given <paramref name="length"/>, <paramref name="valueArray"/> and <paramref name="indexArray"/>
		/// </summary>
		/// <param name="length">The presenting length of this sparse vector</param>
		/// <param name="valueArray">The value array as a <see cref="Storage{T}"/> of <typeparamref name="T"/></param>
		/// <param name="indexArray">The index array as a <see cref="Storage{T}"/> of <typeparamref name="TInd"/></param>
		/// <param name="format">The <see cref="SparseVectorFormat"/> of this sparse vector, must be atomic</param>
		/// <param name="defaultValue">The default value (the value not specified) of this sparse vector</param>
		/// <param name="stores">The number of stored values, default 0 means the length of <paramref name="valueArray"/></param>
		/// <exception cref="TypeMismatchException">If the <typeparamref name="TInd"/> is not an real integral type</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="valueArray"/> or <paramref name="indexArray"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="format"/> is not atomic; or <paramref name="stores"/> is out of the length range of <paramref name="valueArray"/> or <paramref name="indexArray"/> or larger than the presenting length of this vector</exception>
		protected SparseVector(long length, Storage<T> valueArray, Storage<TInd> indexArray, SparseVectorFormat format, T defaultValue = default, long stores = 0) :
			this(length, valueArray, stores, indexArray, stackalloc long[1].SetValue(stores), format, defaultValue) { }

		/// <summary>
		/// Create a <see cref="SparseVector{T, TInd}"/> with given <paramref name="length"/>, <paramref name="valueArray"/> and <paramref name="indexArrays"/>
		/// </summary>
		/// <param name="length">The presenting length of this sparse vector</param>
		/// <param name="valueArray">The value array as a <see cref="Storage{T}"/> of <typeparamref name="T"/></param>
		/// <param name="indexArrays">The index array(s) as a <see cref="SizedFixedClassBuffer_8{T}"/> of <see cref="Storage{T}"/> of <typeparamref name="TInd"/></param>
		/// <param name="realIndexArrayLengths">The actual presenting length of each array in <paramref name="indexArrays"/></param>
		/// <param name="format">The <see cref="SparseVectorFormat"/> of this sparse vector, must be atomic</param>
		/// <param name="defaultValue">The default value (the value not specified) of this sparse vector</param>
		/// <param name="stores">The number of stored values, default 0 means the length of <paramref name="valueArray"/></param>
		/// <exception cref="TypeMismatchException">If the <typeparamref name="TInd"/> is not an integral type</exception>
		/// <exception cref="ArgumentException">If the lengths of <paramref name="indexArrays"/> and <paramref name="realIndexArrayLengths"/> are not the same</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="valueArray"/> or any array in <paramref name="indexArrays"/> is null or empty</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="format"/> is not atomic</exception>
		protected SparseVector(long length, Storage<T> valueArray, SizedFixedClassBuffer_8<Storage<TInd>> indexArrays, Span<long> realIndexArrayLengths, SparseVectorFormat format, T defaultValue = default, long stores = 0) :
			this(length, valueArray, stores, indexArrays, realIndexArrayLengths, format, defaultValue) { }
		#endregion

		#region storage related
		/// <summary>
		/// When implemented by a derived class, check whether this sparse vector is a valid one or not. The default implementation only utilizes the default implementation of <see cref="ICheckValid.IsValid"/> in <see cref="ISparseArray{T}"/>.
		/// </summary>
		/// <returns>The validness of this array</returns>
		public override bool IsValid() => ((ISparseArray<T>)this).IsValid();

		/// <summary>
		/// When implemented by a derived class, check if this sparse vector share some storage(s) with the <paramref name="other"/> one. The default implementation only utilizes the default implementation of <see cref="ISparseArray{T, TIndex}.OverlapWith(ISparseArray{T, TIndex})"/>
		/// </summary>
		/// <param name="other">The other <see cref="ValueArray{T}"/> to check</param>
		/// <returns>True if they do share some storage, false otherwise</returns>
		public override bool OverlapWith(ValueArray<T> other) => other is ISparseArray<T, TInd> sparse && ((ISparseArray<T, TInd>)this).OverlapWith(sparse);

		/// <summary>
		/// When implemented by a derived class, dispose this sparse array after excluding the internal storages shared between this array and the target <paramref name="array"/>. The default implementation only utilizes the default implementation of <see cref="ISparseArray{T}.DisposeExclude(ISparseArray{T})"/>.
		/// </summary>
		/// <param name="array">The target <see cref="ISparseArray{T}"/> to exclude before disposing this sparse vector</param>
		public virtual void DisposeExclude(ISparseArray<T> array) => ((ISparseArray<T>)this).DisposeExclude(array);

		/// <summary>
		/// When implemented by a derived class, actually the dispose this array. The default implementation only disposes <see cref="ValueArray{T}.Storage"/> and the index array(s) passed to the constructor of <see cref="SparseVector{T, TInd}"/>.
		/// </summary>
		/// <param name="disposing">Dispose managed resources or not</param>
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			for (int i = 0; i < this.m_indexArrays.Count; i++)
			{
				this.m_originalIndexArrays[i]?.Dispose();
			}
		}
		#endregion

		#region IReadOnlyList of ISparseArray
		int IReadOnlyCollection<Storage<TInd>>.Count => this.m_indexArrays.Count;

		int IReadOnlyCollection<IStorage>.Count => this.m_indexArrays.Count;

		IStorage IReadOnlyList<IStorage>.this[int index] => this.m_indexArrays[index];

		Storage<TInd> IReadOnlyList<Storage<TInd>>.this[int index] => this.m_indexArrays[index];

		IEnumerator<Storage<TInd>> IEnumerable<Storage<TInd>>.GetEnumerator() => this.m_indexArrays.GetEnumerator();

		IEnumerator<IStorage> IEnumerable<IStorage>.GetEnumerator() => ((IReadOnlyList<Storage<TInd>>)this).GetEnumerator();
		#endregion

		#region clone related
		/// <summary>
		/// When implemented by a derived class, deep clone the sparse vector, the mutable status will not be copied.
		/// </summary>
		/// <returns>The cloned sparse vector</returns>
		public override abstract SparseVector<T, TInd> Clone();

		/// <summary>
		/// When implemented by a derived class, create a new sparse vector with same properties as this one while the underlying storages are not filled.
		/// </summary>
		/// <returns>The new sparse vector alike this one</returns>
		public override abstract SparseVector<T, TInd> NewArrayAlike();

		/// <summary>
		/// When implemented by a derived class, create a new sparse vector with same properties as this one while the underlying storages are not filled and the data type is changed to <typeparamref name="TOut"/>.
		/// </summary>
		/// <typeparam name="TOut">Any unmanaged struct as the new data type</typeparam>
		/// <returns>The new sparse vector alike this one</returns>
		public override abstract SparseVector<TOut, TInd> NewArrayAlike<TOut>();

		/// <summary>
		/// When implemented by a derived class, create a new sparse vector with same properties as this one while the underlying storages are not filled and the data type is changed to <typeparamref name="TOut"/> while index type changed to <typeparamref name="TIndOut"/>.
		/// </summary>
		/// <typeparam name="TOut">Any unmanaged struct as the new data type</typeparam>
		/// <typeparam name="TIndOut">Any integral-typed unmanaged struct as the new index type</typeparam>
		/// <returns>The new sparse vector of type (<typeparamref name="TOut"/>, <typeparamref name="TIndOut"/>) alike this one</returns>
		/// <exception cref="TypeMismatchException">If the <typeparamref name="TIndOut"/> is not an integral type</exception>
		public abstract SparseVector<TOut, TIndOut> NewArrayAlike<TOut, TIndOut>()
			where TOut : unmanaged, IFormattable, IEquatable<TOut>
			where TIndOut : unmanaged, IEquatable<TIndOut>;

		/// <summary>
		/// When implemented by a derived class, cast this sparse vector into another data type <typeparamref name="TOut"/>. The default implementation only casts the <see cref="Storage"/> of this array.
		/// </summary>
		/// <typeparam name="TOut">Any unmanaged struct as the new data type</typeparam>
		/// <typeparam name="TIndOut">Any integral-typed unmanaged struct as the new index type</typeparam>
		/// <returns>The new <see cref="SparseVector{T, TInd}"/> of (<typeparamref name="TOut"/>, <typeparamref name="TIndOut"/>) casted from this array or this array if <typeparamref name="TOut"/> == <typeparamref name="T"/> and <typeparamref name="TIndOut"/> == <typeparamref name="TInd"/></returns>
		public virtual SparseVector<TOut, TIndOut> DataTypeCast<TOut, TIndOut>()
			where TOut : unmanaged, IFormattable, IEquatable<TOut>
			where TIndOut : unmanaged, IEquatable<TIndOut>
		{
			var matrix = this.NewArrayAlike<TOut, TIndOut>();
			try
			{
				((ISparseArray<T, TInd>)this).TypeCast(matrix);
				return matrix;
			}
			catch (Exception)
			{
				matrix?.Dispose();
				throw;
			}
		}
		ISparseArray<TOut, TIndexOut> ISparseArray<T, TInd>.NewArrayAlike<TOut, TIndexOut>() => this.NewArrayAlike<TOut, TIndexOut>();

		ISparseArray<TOut, TIndexOut> ISparseArray<T, TInd>.DataTypeCast<TOut, TIndexOut>() => this.DataTypeCast<TOut, TIndexOut>();
		#endregion

		#region conversion
		/// <summary>
		/// When implemented by a derived class, convert this sparse vector to a dense vector whose <see cref="Storage{T}"/> is <paramref name="denseStorage"/>
		/// </summary>
		/// <param name="denseStorage">The <see cref="Storage{T}"/> of the dense vector to overwrite</param>
		/// <exception cref="ArgumentNullException">If <paramref name="denseStorage"/> is null or has length less than <see cref="AbstractArray{T}.Length"/> of this</exception>
		public abstract void ToDense(Storage<T> denseStorage);

		/// <summary>
		/// When implemented by a derived class, convert this sparse vector to another sparse vector with <see cref="Format"/> fitting <paramref name="format"/>
		/// </summary>
		/// <param name="format">The target format, can be anatomic</param>
		/// <returns>The converted <see cref="SparseVector{T, TInd}"/> whose <see cref="Format"/> fits the given <paramref name="format"/>, or this one if no conversion is necessary</returns>
		public abstract SparseVector<T, TInd> ToFormat(SparseVectorFormat format);
		#endregion

		#region equality
		/// <summary>
		/// When implemented by a derived class, get the hash code this sparse vector. The default implementation only utilizes the default implementation of <see cref="ISparseArray{T, TIndex}.GetHashCode"/>.
		/// </summary>
		/// <returns>The hash code of this sparse vector</returns>
		public override int GetHashCode() => ((ISparseArray<T, TInd>)this).GetHashCode();

		/// <summary>
		/// When implemented by a derived class, check whether this sparse vector is equal to another one. The default implementation only utilizes the default implementation of <see cref="ISparseArray{T, TIndex}.Equals(object?)"/>.
		/// </summary>
		/// <param name="obj">The other object to compare with</param>
		/// <returns>True if this == <paramref name="obj"/></returns>
		public override bool Equals(object? obj) => ((ISparseArray<T, TInd>)this).Equals(obj);
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
			Span<T> values = length.CheckStackLimit<T>() ?? stackalloc T[length];
			MEM.ToManaged(this.Storage, values);
			Span<long> indices = length.CheckStackLimit<long>() ?? stackalloc long[length];
			this.GetIndices(indices);
			// to vector string
			detail += values.ToSparseVectorString(indices, precision: settings.Precision);
			if (this.Length > values.Length)
				detail += Environment.NewLine + string.Format(Resources.Print.MoreStored, this.NStored - values.Length);
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
			if (this.m_indexArrays.Count == 1)
			{
				return new Dictionary<string, IStorage>(2) { [StorageName] = this.Storage, [IndexStorageName] = this.m_indexArrays[0] };
			}
			var dict = new Dictionary<string, IStorage>(this.m_indexArrays.Count + 1) { [StorageName] = this.Storage };
			for (int i = 0; i < this.m_indexArrays.Count; i++)
			{
				dict.Add(this.IndexStorageNameOf(i), this.m_indexArrays[i]);
			}
			return dict;
		}

		/// <summary>
		/// When implemented by a derived class, get other requisite informations for re-constructing the sparse vector of that derived class type. The default implementation returns <see cref="DefaultValue"/> and <see cref="Format"/>.
		/// </summary>
		/// <returns>Other requisite informations used to re-construct this sparse vector.</returns>
		public override IReadOnlyDictionary<string, object> GetOtherInfo() => new Dictionary<string, object>(2)
		{
			[nameof(DefaultValue)] = this.m_defaultValue,
			[nameof(Format)] = this.m_format,
		};
		#endregion
	}
}
