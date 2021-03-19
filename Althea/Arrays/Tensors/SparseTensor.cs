using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

using Althea.Helpers;
using Althea.LinearAlgebra.Sparse;
using Althea.Linq;
using Althea.NativeTypes;
using Althea.TensorAlgebra.Sparse;


namespace Althea.Arrays
{
	/// <summary>
	/// The abstract sparse tensor class with the only mutable <see cref="ValueArray{T}.Storage"/> that refers to the value array storage.
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct that implements <see cref="IFormattable"/> and <see cref="IEquatable{T}"/> as the data type</typeparam>
	/// <typeparam name="TInd">Any integer-typed unmanaged struct as the index type</typeparam>
	public abstract class SparseTensor<T, TInd> : TensorBase<T>, ISparseTensor<T>, ISparseArray<T, TInd>
		where T : unmanaged
		where TInd : unmanaged
	{
		#region basic
		static SparseTensor()
		{
			if (!Const<TInd>.IsIntegralType)
				throw new TypeMismatchException(typeof(TInd), TypeMismatchException.MismatchReason.NotInteger);
		}

		// previously defined 324 bytes
		private readonly FixedClassBuffer_8<Storage<TInd>> m_originalIndexArrays;
		// offset = 324 + 64
		private readonly SizedFixedClassBuffer_8<Storage<TInd>> m_indexArrays;
		// offset = 324 + 64 + 68
		private readonly SparseTensorFormat m_format;
		// offset = 324 + 136
		private T m_defaultValue;
		// this defines extra (136 + size of T) bytes

		/// <summary>
		/// When implemented by a derived class, get the number of stored values of this sparse tensor. The default implementation returns the <see cref="ValueArray{T}.ActualLength"/>.
		/// </summary>
		public virtual long NStored {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.ActualLength;
		}

		/// <summary>
		/// Get the sparse format of this sparse tensor as a <see cref="SparseTensorFormat"/>
		/// </summary>
		public SparseTensorFormat Format {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.m_format;
		}

		/// <summary>
		/// Get or set the default value (the values which are not specified) of this sparse tensor
		/// </summary>
		public T DefaultValue {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.m_defaultValue;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => this.m_defaultValue = value;
		}

		/// <summary>
		/// Get all the index arrays as an array of <see cref="Storage{T}"/> of <typeparamref name="TInd"/>
		/// </summary>
		public SizedFixedClassBuffer_8<Storage<TInd>> IndexArrays {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.m_indexArrays;
		}

		/// <summary>
		/// Create a <see cref="SparseTensor{T, TInd}"/> with given <paramref name="size"/>, <paramref name="valueArray"/> and <paramref name="indexArrays"/>
		/// </summary>
		/// <param name="size">The presenting size of this sparse tensor</param>
		/// <param name="valueArray">The value array as a <see cref="Storage{T}"/> of <typeparamref name="T"/></param>
		/// <param name="indexArrays">The index array(s) as a <see cref="SizedFixedClassBuffer_8{T}"/> of <see cref="Storage{T}"/> of <typeparamref name="TInd"/></param>
		/// <param name="format">The <see cref="SparseTensorFormat"/> of this sparse tensor, must be atomic</param>
		/// <param name="labels">The presenting labels of each dimension of this tensor, an empty one means auto generate as <c>{'a', 'b', ...}</c></param>
		/// <param name="stores">The number of stored values, default 0 means the length of <paramref name="valueArray"/></param>
		/// <param name="indexRealLengths">The actual presenting length of each array in <paramref name="indexArrays"/>, default empty means the lengths of <paramref name="indexArrays"/></param>
		/// <param name="defaultValue">The default value (the value not specified) of this sparse tensor, default 0</param>
		/// <exception cref="TypeMismatchException">If the <typeparamref name="TInd"/> is not an integral type</exception>
		/// <exception cref="ArgumentException">If <paramref name="labels"/>'s length is neither 0 nor the same as the rank; or the lengths of <paramref name="indexArrays"/> and <paramref name="indexRealLengths"/> are not the same</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="valueArray"/> or any array in <paramref name="indexArrays"/> is null or empty</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="format"/> is not atomic</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected SparseTensor(ReadOnlySpan<long> size, Storage<T> valueArray, SizedFixedClassBuffer_8<Storage<TInd>> indexArrays, SparseTensorFormat format,
							 ReadOnlySpan<char> labels = default, long stores = 0, ReadOnlySpan<long> indexRealLengths = default, T defaultValue = default)
			: base(valueArray, size, labels, stores)
		{
			if (!format.IsAtomic())
				throw new ArgumentOutOfRangeException(nameof(format), format, Resources.Parameter.InvalidValue);
			if (!indexRealLengths.IsEmpty && indexArrays.Count != indexRealLengths.Length)
				throw new ArgumentException(Resources.Parameter.NotSameSize);
			if (indexArrays.Any(static a => a is null || !a.IsValid()))
				throw new ArgumentNullException(nameof(indexArrays));

			this.m_originalIndexArrays = indexArrays;
			this.m_indexArrays = new SizedFixedClassBuffer_8<Storage<TInd>>(indexArrays.Count);
			for (int i = 0; i < indexArrays.Count; i++)
			{
				long len = indexRealLengths.IsEmpty ? 0 : indexRealLengths[i];
				if (len < 0)
					throw new ArgumentOutOfRangeException(nameof(indexRealLengths), len, Resources.Parameter.CannotNegative);
				if (len == long.MaxValue)
					len = this.ActualLength;
				if (len == 0)
					this.m_indexArrays[i] = indexArrays[i].MakeReference();
				else
					this.m_indexArrays[i] = indexArrays[i].MakeReference(newLength: len);
			}
			this.m_format = format; this.m_defaultValue = defaultValue;
		}
		#endregion

		#region storage related
		/// <summary>
		/// When implemented by a derived class, check whether this sparse tensor is a valid one or not. The default implementation only utilizes the default implementation of <see cref="ICheckValid.IsValid"/> in <see cref="ISparseArray{T}"/>.
		/// </summary>
		/// <returns>The validness of this array</returns>
		public override bool IsValid() => ((ISparseArray<T>)this).IsValid();

		/// <summary>
		/// When implemented by a derived class, check if this sparse tensor share some storage(s) with the <paramref name="other"/> one. The default implementation only utilizes the default implementation of <see cref="ISparseArray{T, TIndex}.OverlapWith(ISparseArray{T, TIndex})"/>
		/// </summary>
		/// <param name="other">The other <see cref="ValueArray{T}"/> to check</param>
		/// <returns>True if they do share some storage, false otherwise</returns>
		public override bool OverlapWith(ValueArray<T> other) => other is ISparseArray<T, TInd> sparse && ((ISparseArray<T, TInd>)this).OverlapWith(sparse);

		/// <summary>
		/// When implemented by a derived class, dispose this sparse array after excluding the internal storages shared between this array and the target <paramref name="array"/>. The default implementation only utilizes the default implementation of <see cref="ISparseArray{T}.DisposeExclude(ISparseArray{T})"/>.
		/// </summary>
		/// <param name="array">The target <see cref="ISparseArray{T}"/> to exclude before disposing this sparse tensor</param>
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

		#region clone related
		/// <summary>
		/// When implemented by a derived class, deep clone the sparse tensor, the mutable status will not be copied.
		/// </summary>
		/// <returns>The cloned sparse tensor</returns>
		public override abstract SparseTensor<T, TInd> Clone();

		/// <summary>
		/// When implemented by a derived class, create a new sparse tensor with same properties as this one while the underlying storages are not filled.
		/// </summary>
		/// <returns>The new sparse tensor alike this one</returns>
		public override abstract SparseTensor<T, TInd> NewArrayAlike();

		/// <summary>
		/// When implemented by a derived class, create a new sparse tensor with same properties as this one while the underlying storages are not filled and the data type is changed to <typeparamref name="TOut"/>.
		/// </summary>
		/// <typeparam name="TOut">Any unmanaged struct as the new data type</typeparam>
		/// <returns>The new sparse tensor alike this one</returns>
		public override SparseTensor<TOut, TInd> NewArrayAlike<TOut>() => this.NewArrayAlike<TOut, TInd>();

		/// <summary>
		/// When implemented by a derived class, create a new sparse tensor with same properties as this one while the underlying storages are not filled and the data type is changed to <typeparamref name="TOut"/> while index type changed to <typeparamref name="TIndOut"/>.
		/// </summary>
		/// <typeparam name="TOut">Any unmanaged struct as the new data type</typeparam>
		/// <typeparam name="TIndOut">Any integral-typed unmanaged struct as the new index type</typeparam>
		/// <returns>The new sparse tensor alike this one</returns>
		/// <exception cref="TypeMismatchException">If the <typeparamref name="TIndOut"/> is not an integral type</exception>
		public abstract SparseTensor<TOut, TIndOut> NewArrayAlike<TOut, TIndOut>()
			where TOut : unmanaged
			where TIndOut : unmanaged;

		/// <summary>
		/// When implemented by a derived class, cast this sparse tensor into another data type <typeparamref name="TOut"/>. The default implementation only casts the <see cref="Storage"/> of this array.
		/// </summary>
		/// <typeparam name="TOut">Any unmanaged struct as the new data type</typeparam>
		/// <typeparam name="TIndOut">Any integral-typed unmanaged struct as the new index type</typeparam>
		/// <returns>The new <see cref="SparseTensor{T, TInd}"/> of (<typeparamref name="TOut"/>, <typeparamref name="TIndOut"/>) casted from this array or this array if <typeparamref name="TOut"/> == <typeparamref name="T"/> and <typeparamref name="TIndOut"/> == <typeparamref name="TInd"/></returns>
		public virtual SparseTensor<TOut, TIndOut> DataTypeCast<TOut, TIndOut>()
			where TOut : unmanaged
			where TIndOut : unmanaged
		{
			var tensor = this.NewArrayAlike<TOut, TIndOut>();
			try
			{
				((ISparseArray<T, TInd>)this).TypeCast(tensor);
				return tensor;
			}
			catch (Exception)
			{
				tensor?.Dispose();
				throw;
			}
		}

		ISparseArray<TOut, TIndexOut> ISparseArray<T, TInd>.NewArrayAlike<TOut, TIndexOut>() => this.NewArrayAlike<TOut, TIndexOut>();

		ISparseArray<TOut, TIndexOut> ISparseArray<T, TInd>.DataTypeCast<TOut, TIndexOut>() => this.DataTypeCast<TOut, TIndexOut>();
		#endregion

		#region conversion
		/// <summary>
		/// When implemented by a derived class, convert this sparse tensor to a dense tensor whose <see cref="Storage{T}"/> is <paramref name="denseStorage"/>
		/// </summary>
		/// <param name="denseStorage">The <see cref="Storage{T}"/> of the dense tensor to overwrite</param>
		/// <param name="outerSize">The outer size of the target dense tensor, default empty means the same as <see cref="TensorBase{T}.Size"/> of this one</param>
		/// <exception cref="ArgumentNullException">If <paramref name="denseStorage"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="outerSize"/> is less than <see cref="TensorBase{T}.Size"/></exception>
		/// <exception cref="ArgumentException">If product(<paramref name="outerSize"/>) &gt; <paramref name="denseStorage"/>.<see cref="Storage{T}.Length">Length</see></exception>
		public abstract void ToDense(Storage<T> denseStorage, ReadOnlySpan<long> outerSize = default);

		/// <summary>
		/// When implemented by a derived class, convert this sparse tensor to another sparse tensor with <see cref="Format"/> fitting <paramref name="format"/>
		/// </summary>
		/// <param name="format">The target format, can be anatomic</param>
		/// <param name="otherInfo">The target sparse tensor's <see cref="IOtherInfo"/>, default null means letting the internal implementation determine</param>
		/// <returns>The converted <see cref="SparseTensor{T, TInd}"/> whose <see cref="Format"/> fits the given <paramref name="format"/>, or this one if no conversion is necessary</returns>
		/// <exception cref="InvalidOperationException">The default implementation <b>always</b> throws this exception since the sparse tensor format conversions usually have high costs.</exception>
		public virtual SparseTensor<T, TInd> ToFormat(SparseTensorFormat format, IOtherInfo? otherInfo = null) => throw new InvalidOperationException();
		#endregion

		#region equality
		/// <summary>
		/// When implemented by a derived class, get the hash code this sparse tensor. The default implementation only utilizes the default implementation of <see cref="ISparseArray{T, TIndex}.GetHashCode"/>.
		/// </summary>
		/// <returns>The hash code of this sparse tensor</returns>
		public override int GetHashCode() => ((ISparseArray<T, TInd>)this).GetHashCode();

		/// <summary>
		/// When implemented by a derived class, check whether this sparse tensor is equal to another one. The default implementation only utilizes the default implementation of <see cref="ISparseArray{T, TIndex}.Equals(object?)"/>.
		/// </summary>
		/// <param name="obj">The other object to compare with</param>
		/// <returns>True if this == <paramref name="obj"/></returns>
		public override bool Equals(object? obj) => ((ISparseArray<T, TInd>)this).Equals(obj);
		#endregion

		#region serialization
		/// <summary>
		/// The helper method used by <see cref="GetStorages"/> to get the index storages' names.
		/// </summary>
		/// <param name="orderOfIndexStorage">The index of all index storages of this sparse tensor</param>
		/// <returns>The name the index storage indicated by the given <paramref name="orderOfIndexStorage"/></returns>
		protected abstract string IndexStorageNameOf(int orderOfIndexStorage);

		/// <summary>
		/// The presenting name of the <see cref="DefaultValue"/>.
		/// </summary>
		public const string DefaultValueName = nameof(DefaultValue);

		/// <summary>
		/// The presenting name of the <see cref="Format"/>.
		/// </summary>
		public const string FormatName = nameof(Format);

		/// <summary>
		/// When implemented by a derived class, get all the storages of this sparse tensor. The default implementation returns the <see cref="ValueArray{T}.Storage"/> and the index array(s) (whose names are from <see cref="IndexStorageNameOf(int)"/>) used to construct this sparse tensor. If there are exactly 2 index arrays, the default implementation only works correctly when they are (general) row and column index arrays respectively.
		/// </summary>
		/// <returns>All the storages of the array as an <see cref="IReadOnlyDictionary{TKey, TValue}"/> of <see cref="string"/> and <see cref="IStorage"/></returns>
		public override IReadOnlyDictionary<string, IStorage> GetStorages()
		{
			var dict = new Dictionary<string, IStorage>(this.m_indexArrays.Count + 1) { [StorageName] = this.Storage };
			for (int i = 0; i < this.m_indexArrays.Count; i++)
			{
				dict.Add(this.IndexStorageNameOf(i), this.m_indexArrays[i]);
			}
			return dict;
		}

		/// <summary>
		/// When implemented by a derived class, get other requisite informations for re-constructing the sparse tensor of that derived class type. The default implementation returns the <see cref="DefaultValue"/> and <see cref="Format"/>.
		/// </summary>
		/// <returns>Other requisite informations used to re-construct this sparse tensor</returns>
		public override IReadOnlyDictionary<string, object> GetMetaData() => new Dictionary<string, object>(2)
		{
			[DefaultValueName] = this.m_defaultValue,
			[FormatName] = this.m_format,
		};
		#endregion
	}
}
