using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Althea.Helpers;
using Althea.LinearAlgebra.Sparse;
using Althea.NativeTypes;


namespace Althea.Arrays
{
	/// <summary>
	/// The abstract sparse matrix class with the only mutable <see cref="ValueArray{T}.Storage"/> that refers to the value array storage.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TInd">Any integer-typed unmanaged number as the index type</typeparam>
	public abstract class BaseSparseMatrix<T, TInd> : BaseMatrix<T>, ISparseMatrix<T>, ISparseArray<T, TInd>
		where T : unmanaged, INumber<T>
		where TInd : unmanaged
	{
		#region basic
		static BaseSparseMatrix()
		{
			if (!Const<TInd>.IsIntegralType)
				throw new TypeMismatchException(typeof(TInd), TypeMismatchException.MismatchReason.NotInteger);
		}

		private readonly SparseMatrixFormat m_format;

		private T m_defaultValue;

		/// <summary>
		/// When implemented by a derived class, get the number of stored values of this sparse matrix. The default implementation returns the <see cref="ValueArray{T}.ActualLength"/>.
		/// </summary>
		public virtual long NStored {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.ActualLength;
		}

		/// <summary>
		/// Get the sparse format of this sparse matrix as a <see cref="SparseMatrixFormat"/>
		/// </summary>
		public SparseMatrixFormat Format {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.m_format;
		}

		/// <summary>
		/// Get or set the default value (the values which are not specified) of this sparse matrix
		/// </summary>
		public T DefaultValue {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.m_defaultValue;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => this.m_defaultValue = value;
		}

		/// <summary>
		/// When implemented by a derived class, get all the index arrays as a <see cref="ReadOnlySpan{T}"/> of <see cref="Storage{T}"/> of <typeparamref name="TInd"/>
		/// </summary>
		public abstract ReadOnlySpan<Storage<TInd>> IndexArrays { get; }

		/// <summary>
		/// When implemented by a derived class, get the original index array(s)' storage(s) of this sparse array.
		/// </summary>
		protected abstract ReadOnlySpan<IStorage> OriginalIndexStorages { get; }

		ReadOnlySpan<IStorage> ISparseArray<T>.OriginalIndexStorages {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.OriginalIndexStorages;
		}

		/// <summary>
		/// Create a <see cref="BaseSparseMatrix{T, TInd}"/> with given <paramref name="rows"/>, <paramref name="cols"/> and <paramref name="valueArray"/>
		/// </summary>
		/// <param name="rows">The presenting number of rows of this sparse matrix</param>
		/// <param name="cols">The presenting number of columns of this sparse matrix</param>
		/// <param name="valueArray">The value array as a <see cref="Storage{T}"/> of <typeparamref name="T"/></param>
		/// <param name="format">The <see cref="SparseVectorFormat"/> of this sparse matrix, must be atomic</param>
		/// <param name="defaultValue">The default value (the value not specified) of this sparse matrix</param>
		/// <param name="stores">The number of stored values, default 0 means the length of <paramref name="valueArray"/></param>
		/// <exception cref="TypeMismatchException">If the <typeparamref name="TInd"/> is not an real integral type</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="valueArray"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="format"/> is not atomic; or <paramref name="stores"/> is out of the length range of <paramref name="valueArray"/> or larger than the presenting length of this matrix</exception>
		protected BaseSparseMatrix(long rows, long cols, Storage<T> valueArray, SparseMatrixFormat format, T defaultValue = default, long stores = 0) :
			base(valueArray, rows, cols, stores)
		{
			if (!format.IsAtomic())
				throw new ArgumentOutOfRangeException(nameof(format), format, Resources.Parameter.InvalidValue);
			this.m_format = format;
			this.m_defaultValue = defaultValue;
		}
		#endregion

		#region storage related
		/// <summary>
		/// When implemented by a derived class, check whether this sparse matrix is a valid one or not. The default implementation only utilizes the default implementation of <see cref="ICheckValid.IsValid"/> in <see cref="ISparseArray{T}"/>.
		/// </summary>
		/// <returns>The validness of this array</returns>
		public override bool IsValid() => ((ISparseArray<T>)this).IsValid();

		/// <summary>
		/// When implemented by a derived class, check if this sparse matrix share some storage(s) with the <paramref name="other"/> one. The default implementation only utilizes the default implementation of <see cref="ISparseArray{T, TIndex}.OverlapWith(ISparseArray{T, TIndex})"/>
		/// </summary>
		/// <param name="other">The other <see cref="ValueArray{T}"/> to check</param>
		/// <returns>True if they do share some storage, false otherwise</returns>
		public override bool OverlapWith(ValueArray<T> other) => other is ISparseArray<T, TInd> sparse && ((ISparseArray<T, TInd>)this).OverlapWith(sparse);

		/// <summary>
		/// When implemented by a derived class, actually the dispose this array. The default implementation only disposes <see cref="ValueArray{T}.Storage"/> and the index array(s) passed to the constructor of <see cref="BaseSparseVector{T, TInd}"/>.
		/// </summary>
		/// <param name="disposing">Dispose managed resources or not</param>
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			if (!this.Disposed && !this.IndexArrays.IsEmpty)
			{
				((ISparseArray<T>)this).Dispose();
			}
		}
		#endregion

		#region clone related
		/// <summary>
		/// When implemented by a derived class, deep clone the sparse matrix, the mutable status will not be copied.
		/// </summary>
		/// <returns>The cloned sparse matrix</returns>
		public override abstract BaseSparseMatrix<T, TInd> Clone();

		/// <summary>
		/// When implemented by a derived class, create a new sparse matrix with same properties as this one while the underlying storages are not filled.
		/// </summary>
		/// <returns>The new sparse matrix alike this one</returns>
		public override BaseSparseMatrix<T, TInd> NewArrayAlike() => this.NewArrayAlike<T, TInd>();

		/// <summary>
		/// When implemented by a derived class, create a new sparse matrix with same properties as this one while the underlying storages are not filled and the data type is changed to <typeparamref name="TOut"/>.
		/// </summary>
		/// <typeparam name="TOut">Any unmanaged number as the new data type</typeparam>
		/// <returns>The new sparse matrix alike this one</returns>
		public override BaseSparseMatrix<TOut, TInd> NewArrayAlike<TOut>() => this.NewArrayAlike<TOut, TInd>();

		/// <summary>
		/// When implemented by a derived class, create a new sparse matrix with same properties as this one while the underlying storages are not filled and the data type is changed to <typeparamref name="TOut"/> while index type changed to <typeparamref name="TIndOut"/>.
		/// </summary>
		/// <typeparam name="TOut">Any unmanaged number as the new data type</typeparam>
		/// <typeparam name="TIndOut">Any integral-typed unmanaged number as the new index type</typeparam>
		/// <returns>The new sparse matrix alike this one</returns>
		/// <exception cref="TypeMismatchException">If the <typeparamref name="TIndOut"/> is not an integral type</exception>
		public abstract BaseSparseMatrix<TOut, TIndOut> NewArrayAlike<TOut, TIndOut>()
			where TOut : unmanaged
			where TIndOut : unmanaged;

		/// <summary>
		/// When implemented by a derived class, cast this sparse matrix into another data type <typeparamref name="TOut"/>. The default implementation only casts the <see cref="Storage"/> of this array.
		/// </summary>
		/// <typeparam name="TOut">Any unmanaged number as the new data type</typeparam>
		/// <typeparam name="TIndOut">Any integral-typed unmanaged number as the new index type</typeparam>
		/// <returns>The new <see cref="BaseSparseMatrix{T, TInd}"/> of (<typeparamref name="TOut"/>, <typeparamref name="TIndOut"/>) casted from this array or this array if <typeparamref name="TOut"/> == <typeparamref name="T"/> and <typeparamref name="TIndOut"/> == <typeparamref name="TInd"/></returns>
		public virtual BaseSparseMatrix<TOut, TIndOut> DataTypeCast<TOut, TIndOut>()
			where TOut : unmanaged
			where TIndOut : unmanaged
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
		/// When implemented by a derived class, convert this sparse matrix to a dense matrix whose <see cref="Storage{T}"/> is <paramref name="denseStorage"/>
		/// </summary>
		/// <param name="denseStorage">The <see cref="Storage{T}"/> of the dense matrix to overwrite</param>
		/// <param name="leadDim">The leading dimension of the target dense matrix, default 0 means <see cref="BaseMatrix{T}.NRows"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="denseStorage"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="leadDim"/> is less than <see cref="BaseMatrix{T}.NRows"/></exception>
		/// <exception cref="ArgumentException">If <paramref name="leadDim"/> * <see cref="BaseMatrix{T}.NCols"/> &gt; <paramref name="denseStorage"/>.<see cref="Storage{T}.Length">Length</see></exception>
		public abstract void ToDense(Storage<T> denseStorage, long leadDim = 0);

		/// <summary>
		/// When implemented by a derived class, convert this sparse matrix to another sparse matrix with <see cref="Format"/> fitting <paramref name="format"/>
		/// </summary>
		/// <param name="format">The target format, can be anatomic</param>
		/// <param name="otherInfo">The target sparse matrix's <see cref="IOtherInfo"/>, default null means letting the internal implementation determine</param>
		/// <returns>The converted <see cref="BaseSparseMatrix{T, TInd}"/> whose <see cref="Format"/> fits the given <paramref name="format"/>, or this one if no conversion is necessary</returns>
		public abstract BaseSparseMatrix<T, TInd> ToFormat(SparseMatrixFormat format, IOtherInfo? otherInfo = null);
		#endregion

		#region equality
		/// <summary>
		/// When implemented by a derived class, get the hash code this sparse matrix. The default implementation only utilizes the default implementation of <see cref="ISparseArray{T, TIndex}.GetHashCode"/>.
		/// </summary>
		/// <returns>The hash code of this sparse matrix</returns>
		public override int GetHashCode() => ((ISparseArray<T, TInd>)this).GetHashCode();

		/// <summary>
		/// When implemented by a derived class, check whether this sparse matrix is equal to another one. The default implementation only utilizes the default implementation of <see cref="ISparseArray{T, TIndex}.Equals(object?)"/>.
		/// </summary>
		/// <param name="obj">The other object to compare with</param>
		/// <returns>True if this == <paramref name="obj"/></returns>
		public override bool Equals(object? obj) => ((ISparseArray<T, TInd>)this).Equals(obj);
		#endregion

		#region serialization
		/// <summary>
		/// The presenting name of the <see cref="DefaultValue"/>.
		/// </summary>
		public const string DefaultValueName = nameof(DefaultValue);

		/// <summary>
		/// The presenting name of the <see cref="Format"/>.
		/// </summary>
		public const string FormatName = nameof(Format);

		/// <summary>
		/// When implemented by a derived class, get other requisite informations for re-constructing the sparse matrix of that derived class type. The default implementation returns the <see cref="DefaultValue"/> and <see cref="Format"/>.
		/// </summary>
		/// <returns>Other requisite informations used to re-construct this sparse matrix</returns>
		public override IReadOnlyDictionary<string, object> GetMetaData() => new Dictionary<string, object>(2)
		{
			[DefaultValueName] = this.m_defaultValue,
			[FormatName] = this.m_format,
		};
		#endregion
	}
}
