using System;
using System.Collections.Generic;

using Althea.Linq;
using Althea.Arrays;
using Althea.Helpers;
using Althea.NativeTypes;
using Althea.LinearAlgebra;
using Althea.LinearAlgebra.Sparse;

using MEM = Althea.Storage.AbstractApi;
using LAD = Althea.LinearAlgebra.Dense.AbstractApi;
using LAS = Althea.LinearAlgebra.Sparse.AbstractApi;


namespace Althea.Backend.Arrays
{
	/// <summary>
	/// The concrete sparse vector class with the only mutable <see cref="ValueArray{T}.Storage"/> that refers to the value array storage and the <see cref="SparseVector{T, TInd}.IndexStorage"/> that refers to the sorted index array storage.
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct that implements <see cref="IFormattable"/> and <see cref="IEquatable{T}"/> as the data type</typeparam>
	/// <typeparam name="TInd">Any integer-typed unmanaged struct as the index type</typeparam>
	public class SparseVector<T, TInd> : AbstractSparseVector<T, TInd>, IKrylovVector<SparseVector<T, TInd>, T>
		where T : unmanaged, IFormattable, IEquatable<T>
		where TInd : unmanaged
	{
		#region basic
		/// <summary>
		/// Get the index array's storage as a <see cref="Storage{T}"/> of <typeparamref name="TInd"/>
		/// </summary>
		public Storage<TInd> IndexStorage => this.m_indexArray;

		/// <summary>
		/// Create an empty <see cref="SparseVector{T, TInd}"/>
		/// </summary>
		public SparseVector() : base(0, Storage<T>.Empty, Storage<TInd>.Empty, SparseVectorFormat.Coordinated) { }

		/// <summary>
		/// Create a <see cref="SparseVector{T, TInd}"/> (of coordinate-format) with given <paramref name="length"/>, <paramref name="valueArray"/> and <paramref name="indexArray"/> the <paramref name="defaultValue"/>
		/// </summary>
		/// <param name="length">The presenting length of this sparse vector</param>
		/// <param name="valueArray">The value array as a <see cref="Storage{T}"/> of <typeparamref name="T"/></param>
		/// <param name="indexArray">The index array as a <see cref="Storage{T}"/> of <typeparamref name="TInd"/></param>
		/// <param name="defaultValue">The default value (the value not specified) of this sparse vector</param>
		/// <exception cref="NotSupportedException">If the <typeparamref name="TInd"/> is not an integral type</exception>
		public SparseVector(long length, Storage<T> valueArray, Storage<TInd> indexArray,T defaultValue = default) : base(length, valueArray, indexArray, SparseVectorFormat.Coordinated, defaultValue) { }
		#endregion

		#region indexer
		/// <summary>
		/// When implemented by a derived class, provide the basic indexed getter and setter of this sparse vector
		/// </summary>
		/// <param name="index">The position of the element to get / set</param>
		/// <returns>The element at <paramref name="index"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="index"/> is out of range</exception>
		/// <exception cref="InvalidOperationException">If the value at <paramref name="index"/> is not stored</exception>
		public override T this[long index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

		/// <summary>
		/// When implemented by a derived class, get a sub-vector indicated by the given <paramref name="start"/> offset and <paramref name="length"/>
		/// </summary>
		/// <param name="start">The starting offset of the target sub-vector compared to this vector, in <typeparamref name="T"/></param>
		/// <param name="length">The length of the target sub-vector, in <typeparamref name="T"/></param>
		/// <returns>The sub-vector indicated by <paramref name="start"/> and <paramref name="length"/>. Shall be a referenced vector if possible.</returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="start"/> and/or <paramref name="length"/> is out of range</exception>
		public override VectorBase<T> Slice(long start, long length) => throw new NotImplementedException();
		#endregion

		#region reshape
		/// <summary>
		/// Convert this sparse vector to a dense vector
		/// </summary>
		/// <returns>The converted <see cref="Backend.Arrays.DenseVector{T}"/></returns>
		public override Backend.Arrays.DenseVector<T> ToDense() => throw new NotImplementedException();

		public override ValueArray<T> ToMatrix(long leadDim = 0) => throw new NotImplementedException();
		public override ValueArray<T> ToTensor(ReadOnlySpan<long> size) => throw new NotImplementedException();
		#endregion

		#region linear algebra
		/// <summary>
		/// Check the sparsity of this sparse vector and the <paramref name="other"/> one
		/// </summary>
		/// <param name="other">The other sparse vector to check sparsity</param>
		/// <exception cref="InvalidOperationException">If the <paramref name="other"/> vector has different sparsity from this one</exception>
		protected void CheckSparsity(SparseVector<T, TInd> other)
		{
			if (this.Length != other.Length || this.NStored != other.NStored)
				throw new InvalidOperationException(Resources.Parameter.NotSameSize);
			// check same indices
			if (this.IndexStorage == other.IndexStorage)
				return;
			if (!LAD.SelectImplementation<TInd>(this.IndexStorage, other.IndexStorage).PointWiseEquals(this.IndexStorage, 1, other.IndexStorage, 1))
				throw new InvalidOperationException(Resources.Other.DifferentSparsity);
		}

		/// <summary>
		/// When implemented by a derived class, compute the dot (inner) product of this vector and the <paramref name="other"/> vector.
		/// </summary>
		/// <param name="other">The other vector to perform the dot product</param>
		/// <param name="conjugateThis">Whether the dot product is performed on the conjugation of this vector or directly.</param>
		/// <returns>The dot (inner) product result as a <typeparamref name="T"/></returns>
		/// <exception cref="NotSupportedException">If <paramref name="other"/> is neither a <see cref="DenseVector{T}"/> nor a <see cref="SparseVector{T, TIndex}"/></exception>
		/// <exception cref="ArgumentNullException">If <paramref name="other"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="other"/> has different length than this</exception>
		public override T Dot(VectorBase<T> other, bool conjugateThis = true)
		{
			if (other is null || !other.IsValid())
				throw new ArgumentNullException(nameof(other));
			if (other.Length != this.Length)
				throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(other));

			if (other is DenseVector<T>)
				return LAS.SelectImplementation(other.Storage, this).VectorSparseDotDense(conjugateThis, this, other.Storage);
			else if (other is SparseVector<T, TInd> sparse)
				return LAS.SelectImplementation(this, sparse).VectorSparseDotSparse(conjugateThis, this, sparse);
			else
				throw new NotSupportedException();
		}

		/// <summary>
		/// When implemented by a derived class, compute the addition of the <paramref name="other"/> vector (scaling by <paramref name="scalar"/>) and this vector.
		/// </summary>
		/// <param name="other">The other vector to add</param>
		/// <param name="scalar">The scalar to be multiplied to <paramref name="other"/> of type <typeparamref name="T"/></param>
		/// <returns>The addition result of this + <paramref name="scalar"/> * <paramref name="other"/></returns>
		public override VectorBase<T> AddVector(VectorBase<T> other, T scalar) => throw new NotImplementedException();

		/// <summary>
		/// When implemented by a derived class, compute the addition of the multiplication result of the given <paramref name="matrix"/> and <paramref name="vector"/> (scaled by <paramref name="α"/>) with this vector (scaled by <paramref name="β"/>).
		/// </summary>
		/// <param name="matrix">The input matrix to be multiplied</param>
		/// <param name="vector">The input vector to be multiplied</param>
		/// <param name="α">The scalar to be multiplied to the <paramref name="matrix"/> of type <typeparamref name="T"/></param>
		/// <param name="β">The scalar to be multiplied to this vector of type <typeparamref name="T"/></param>
		/// <param name="operation">The simple operation to be applied to <paramref name="matrix"/> before computation as a <see cref="MatrixOperation"/></param>
		/// <returns>The addition result of <paramref name="β"/> * this + <paramref name="α"/> * <paramref name="operation"/>(<paramref name="matrix"/>) * <paramref name="vector"/></returns>
		public override VectorBase<T> AddMatrixMultiplyVector(MatrixBase<T> matrix, VectorBase<T> vector, T α, T β = default, MatrixOperation operation = MatrixOperation.None) => throw new NotImplementedException();

		/// <summary>
		/// When implemented by a derived class, add the <paramref name="other"/> (scaling by <paramref name="scalar"/>) to this vector in-place.
		/// </summary>
		/// <param name="other">The other vector to add</param>
		/// <param name="scalar">The scalar to be multiplied to <paramref name="other"/> of type <typeparamref name="T"/></param>
		/// <exception cref="NotSupportedException">If <paramref name="other"/> is not a <see cref="SparseVector{T, TIndex}"/></exception>
		/// <exception cref="ArgumentNullException">If <paramref name="other"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="other"/> has different length than this</exception>
		/// <exception cref="InvalidOperationException">If this and <paramref name="other"/> have different sparsities thus this operation cannot be done in-place</exception>
		public void AddByVector(VectorBase<T> other, T scalar)
		{
			if (other is null || !other.IsValid())
				throw new ArgumentNullException(nameof(other));
			if (other.Length != this.Length)
				throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(other));
			if (other is not SparseVector<T, TInd> sparse)
				throw new NotSupportedException();

			this.CheckSparsity(sparse);
			LAD.SelectImplementation<T>(this.Storage, other.Storage).VectorGeneralAdd(scalar, other.Storage, 1, this.Storage, 1);
		}
		#endregion

		#region clone related
		/// <summary>
		/// When implemented by a derived class, deep clone the sparse vector, the mutable status will not be copied.
		/// </summary>
		/// <returns>The cloned array</returns>
		public override SparseVector<T, TInd> Clone()
		{
			this.Clone(out ActualStorage<T> value, out ActualStorage<TInd> index, out _);
			return new SparseVector<T, TInd>(this.Length, value, index, this.DefaultValue);
		}

		/// <summary>
		/// When implemented by a derived class, create a new sparse vector with same properties as this one while the underlying storages are not filled.
		/// </summary>
		/// <returns>The new sparse vector alike this one</returns>
		public override SparseVector<T, TInd> NewArrayAlike()
		{
			this.NewArrayAlike(out ActualStorage<T> value, out ActualStorage<TInd> index, out _);
			return new SparseVector<T, TInd>(this.Length, value, index, this.DefaultValue);
		}

		/// <summary>
		/// When implemented by a derived class, create a new sparse vector with same properties as this one while the underlying storages are not filled and the data type is changed to <typeparamref name="TOut"/>.
		/// </summary>
		/// <typeparam name="TOut">Any unmanaged struct as the new data type</typeparam>
		/// <returns>The new sparse vector alike this one</returns>
		public override SparseVector<TOut, TInd> NewArrayAlike<TOut>()
		{
			this.NewArrayAlike(out ActualStorage<TOut> value, out ActualStorage<TInd> index, out _);
			return new SparseVector<TOut, TInd>(this.Length, value, index, this.DefaultValue.GenericConvert<TOut, T>());
		}

		/// <summary>
		/// When implemented by a derived class, cast this array into another data type <typeparamref name="TOut"/>. The default implementation only casts the <see cref="Storage"/> of this array.
		/// </summary>
		/// <typeparam name="TOut">The data type to cast to</typeparam>
		/// <returns>The new <see cref="ValueArray{TOut}"/> casted from this array or this array if <typeparamref name="TOut"/> == <typeparamref name="T"/></returns>
		public override SparseVector<TOut, TInd> DataTypeCast<TOut>()
		{
			this.DataTypeCast(out Storage<TOut> value);
			return new SparseVector<TOut, TInd>(this.Length, value, this.IndexStorage, this.DefaultValue.GenericConvert<TOut, T>());
		}
		#endregion

		#region IKrylovVector
		void IKrylovVector<SparseVector<T, TInd>, T>.Scale(T value) => this.Scale(value);

		double IKrylovVector<SparseVector<T, TInd>, T>.Norm() => this.Norm();

		void IKrylovVector<SparseVector<T, TInd>, T>.Normalize() => this.Normalize();

		T IKrylovVector<SparseVector<T, TInd>, T>.Dot(SparseVector<T, TInd> other) => this.Dot(other);

		void IKrylovVector<SparseVector<T, TInd>, T>.AddByVector(SparseVector<T, TInd> other, T scalar) => this.AddByVector(other, scalar);

		/// <summary>
		/// When implemented by a derived class, replace this vector's content with the <paramref name="other"/> vector in-place. The default implementation only works when this and <paramref name="other"/> have same sparsity.
		/// </summary>
		/// <param name="other">The other dense vector to replace from</param>
		/// <exception cref="InvalidOperationException">If the replacement cannot be done in-place due to reason(s) such as different sparsities between this and <paramref name="other"/></exception>
		public virtual void ReplaceBy(SparseVector<T, TInd> other)
		{
			this.CheckSparsity(other);
			MEM.MemoryCopy(other.Storage, this.Storage);
		}

		/// <summary>
		/// When implemented by a derived class, multiply the matrix whose columns are indicated by <paramref name="unjoinedVectors"/> to a sparse vector indicated by a <see cref="ReadOnlySpan{T}"/> and obtain the result vector as a <see cref="SparseVector{T, TInd}"/>. The default implementation only works when this and all vectors in <paramref name="unjoinedVectors"/> have same sparsity.
		/// </summary>
		/// <param name="unjoinedVectors">The columns of the matrix to be multiplied</param>
		/// <param name="input">The input dense vector to be multiplied as a <see cref="ReadOnlySpan{T}"/></param>
		/// <returns>The product of <paramref name="unjoinedVectors"/> and <paramref name="input"/> as a <see cref="SparseVector{T, TInd}"/></returns>
		/// <remarks>The method shall be basically static, the information of this vector shall only be used to verify the consistency of <paramref name="unjoinedVectors"/></remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="unjoinedVectors"/> or any of its element is null or invalid, or <paramref name="input"/> is empty</exception>
		/// <exception cref="ArgumentException">If <paramref name="input"/> and <paramref name="unjoinedVectors"/> have different size, or any element of <paramref name="unjoinedVectors"/> has different size than this vector</exception>
		/// <exception cref="ObjectDisposedException">If any element of <paramref name="unjoinedVectors"/> is disposed</exception>
		/// <exception cref="InvalidOperationException">If the operation cannot be done due to different sparsities between this and <paramref name="unjoinedVectors"/></exception>
		public SparseVector<T, TInd> OperateOn(IReadOnlyList<SparseVector<T, TInd>> unjoinedVectors, ReadOnlySpan<T> input)
		{
			if (unjoinedVectors is null || unjoinedVectors.Count == 0)
				throw new ArgumentNullException(nameof(unjoinedVectors));
			if (input.IsEmpty)
				throw new ArgumentNullException(nameof(input));
			if (unjoinedVectors.Count != input.Length)
				throw new ArgumentException(Resources.Parameter.NotSameSize);

			// sort first to reduce errors
			int length = input.Length;
			Span<T> values = length.CheckStockLimit<T>() ?? stackalloc T[length];
			Span<double> keys = length.CheckStockLimit<double>() ?? stackalloc double[length];
			for (int i = 0; i < length; i++)
			{
				values[i] = input[i];
				keys[i] = input[i].GenericAbsolute();
			}
			keys.Sort(values);

			var vec = this.NewArrayAlike();
			try
			{
				vec.FillWith(default);
				for (int i = 0; i < length; i++)
				{
					var dnvec = unjoinedVectors[i];
					if (dnvec is null || !dnvec.IsValid())
						throw new ArgumentNullException(nameof(unjoinedVectors));
					if (dnvec.Length != this.Length)
						throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(unjoinedVectors));
					if (dnvec.Disposed)
						throw new ObjectDisposedException(nameof(unjoinedVectors));
					if (!values[i].IsZero())
						vec.AddByVector(dnvec, values[i]);
				}
				return vec;
			}
			catch (Exception)
			{
				vec.Dispose();
				throw;
			}
		}
		#endregion

		#region protected overrides
		/// <summary>
		/// The helper method used in <see cref="AbstractSparseVector{T, TInd}.Print(PrintSettings?)"/> to get the first several indices of this sparse vector
		/// </summary>
		/// <param name="indices">The <see cref="Span{T}"/> of <see cref="long"/> used to store the indices</param>
		protected override void GetIndices(Span<long> indices)
		{
			if (typeof(TInd) == typeof(long))
			{
				MEM.ToManaged(this.IndexStorage as Storage<long> ?? Storage<long>.Empty, indices);
				return;
			}
			// else
			Span<TInd> temp = indices.Length.CheckStockLimit<TInd>() ?? stackalloc TInd[indices.Length];
			MEM.ToManaged(this.IndexStorage, temp);
			temp.CopyTo(indices, static a => a.ReflectionConvert<TInd, long>());
		}

		/// <summary>
		/// The helper method used by <see cref="AbstractSparseVector{T, TInd}.GetPointers"/> to get the index storages' names. Only used when the sparse array contains more than one index storages.
		/// </summary>
		/// <param name="orderOfIndexStorage">The index of all index storages of this sparse vector</param>
		/// <returns>The name the index storage indicated by the given <paramref name="orderOfIndexStorage"/></returns>
		protected override string IndexStorageNameOf(int orderOfIndexStorage) => IndexStorageName;
		#endregion

		#region serialization
		/// <summary>
		/// When implemented by a derived class, get other requisite informations for re-constructing the sparse array of that derived class type.
		/// </summary>
		/// <returns>Other requisite informations used to re-construct this array. Always an empty dictionary.</returns>
		public override IReadOnlyDictionary<string, object> GetOtherInfo() => new Dictionary<string, object>(0);
		#endregion
	}
}
