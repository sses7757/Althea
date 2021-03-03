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
	/// The concrete sparse vector class with the only mutable <see cref="ValueArray{T}.Storage"/> that refers to the value array storage and the <see cref="SparseVector{T, TInd}.IndexStorage"/> that refers to the <b>sorted</b> index array storage.
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
		public Storage<TInd> IndexStorage => this.m_indexArrays[0];

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
		/// <param name="stores">The number of stored values, default 0 means the length of <paramref name="valueArray"/></param>
		/// <exception cref="NotSupportedException">If the <typeparamref name="TInd"/> is not an integral type</exception>
		public SparseVector(long length, Storage<T> valueArray, Storage<TInd> indexArray,T defaultValue = default, long stores = 0) : base(length, valueArray, indexArray, SparseVectorFormat.Coordinated, defaultValue, stores) { }

		private SparseVector<T, TInd> CreateFunction(long length, long nonDefaults, SparseVectorFormat format, T defaultValue)
		{
			if (length <= 0)
				throw new ArgumentOutOfRangeException(nameof(length), length, Resources.Parameter.MustPositive);
			if (nonDefaults <= 0)
				throw new ArgumentOutOfRangeException(nameof(nonDefaults), nonDefaults, Resources.Parameter.MustPositive);
			if (format != SparseVectorFormat.Coordinated)
				throw new ArgumentOutOfRangeException(nameof(format), format, Resources.Parameter.InvalidValue);

			Storage<T>? value = null; Storage<TInd>? index = null;
			try
			{
				value = Storage<T>.Create(this.Storage[0].Location, nonDefaults);
				index = Storage<TInd>.Create(this.IndexStorage[0].Location, nonDefaults);
				return new SparseVector<T, TInd>(length, value, index, defaultValue);
			}
			catch (Exception)
			{
				value?.Dispose(); index?.Dispose();
				throw;
			}
		}
		#endregion

		#region indexer
		/// <summary>
		/// The basic indexed getter and setter of this vector.
		/// </summary>
		/// <param name="index">The position of the element to get / set</param>
		/// <returns>The element at <paramref name="index"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="index"/> is out of range</exception>
		/// <exception cref="InvalidOperationException">If this[<paramref name="index"/>] returns the <see cref="AbstractSparseVector{T, TInd}.DefaultValue"/> which cannot be set individually</exception>
		public override T this[long index] {
			get {
				this.CheckIndex(index);
				long find = LAS.IndexFind(sorted: true, this.IndexStorage, index.GenericConvert<TInd, long>());
				if (find < 0)
					return this.DefaultValue;
				else
					return MEM.ToManaged(this.Storage.MakeReference(offset: find));
			}
			set {
				this.CheckIndex(index);
				long find = LAS.IndexFind(sorted: true, this.IndexStorage, index.GenericConvert<TInd, long>());
				if (find < 0)
					throw new InvalidOperationException();
				MEM.FromManaged(this.Storage.MakeReference(offset: find), value);
			}
		}

		/// <summary>
		/// Get a sub-vector indicated by the given <paramref name="start"/> offset and <paramref name="length"/>
		/// </summary>
		/// <param name="start">The starting offset of the target sub-vector compared to this vector, in <typeparamref name="T"/></param>
		/// <param name="length">The length of the target sub-vector, in <typeparamref name="T"/></param>
		/// <returns>The referenced sub-vector indicated by <paramref name="start"/> and <paramref name="length"/>.</returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="start"/> and/or <paramref name="length"/> is out of range</exception>
		public override SparseVector<T, TInd> Slice(long start, long length)
		{
			this.CheckRange(start, length);
			long lowerBound = LAS.IndexBound(this.IndexStorage, start.GenericConvert<TInd, long>(), lowerBound: true);
			long upperBound = LAS.IndexBound(this.IndexStorage, (start + length).GenericConvert<TInd, long>(), lowerBound: false);
			return new SparseVector<T, TInd>(length,
											 this.Storage.MakeReference(offset: lowerBound, newLength: upperBound - lowerBound),
											 this.IndexStorage.MakeReference(offset: lowerBound, newLength: upperBound - lowerBound),
											 this.DefaultValue);
		}
		#endregion

		#region reshape
		/// <summary>
		/// Convert this sparse vector to a dense vector whose <see cref="Storage{T}"/> is <paramref name="denseStorage"/>
		/// </summary>
		/// <param name="denseStorage">The <see cref="Storage{T}"/> of the dense vector to overwrite</param>
		/// <exception cref="ArgumentNullException">If <paramref name="denseStorage"/> is null or has length less than <see cref="AbstractArray{T}.Length"/> of this</exception>
		public override void ToDense(Storage<T> denseStorage)
		{
			if (denseStorage is null || denseStorage.IsOffsetValid(0, this.Length))
				throw new ArgumentNullException(nameof(denseStorage));
			LAS.VectorSparseToDense(this, denseStorage);
		}

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
			if (!LAD.PointWiseEquals(this.IndexStorage, 1, other.IndexStorage, 1))
				throw new InvalidOperationException(Resources.Other.DifferentSparsity);
		}

		/// <summary>
		/// Compute the dot (inner) product of this vector and the <paramref name="other"/> vector.
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
				return LAS.VectorSparseDotDense(conjugateThis, this, other.Storage);
			else if (other is SparseVector<T, TInd> sparse)
				return LAS.VectorSparseDotSparse(conjugateThis, this, sparse);
			else
				throw new NotSupportedException();
		}

		/// <summary>
		/// Compute the addition of the <paramref name="other"/> vector (scaling by <paramref name="scalar"/>) and this vector.
		/// </summary>
		/// <param name="other">The other vector to add</param>
		/// <param name="scalar">The scalar to be multiplied to <paramref name="other"/> of type <typeparamref name="T"/></param>
		/// <returns>The addition result of this + <paramref name="scalar"/> * <paramref name="other"/></returns>
		/// <exception cref="NotSupportedException">If <paramref name="other"/> is neither a <see cref="DenseVector{T}"/> nor a <see cref="SparseVector{T, TIndex}"/></exception>
		public override VectorBase<T> AddVector(VectorBase<T> other, T scalar)
		{
			if (other is null || !other.IsValid())
				throw new ArgumentNullException(nameof(other));
			if (other.Length != this.Length)
				throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(other));

			if (other is SparseVector<T, TInd> sparse)
				return (SparseVector<T, TInd>)LAS.VectorSparseAddSparse(this, sparse, SparseVectorFormat.Coordinated, createFunc: this.CreateFunction);
			else if (other is DenseVector<T>)
				return other.ApplyToClone(d => LAS.VectorSparseAddToDense(Scalars<T>.One, this, d.Storage));
			else
				throw new NotSupportedException();
		}

		/// <summary>
		/// Compute the addition of the multiplication result of the given <paramref name="matrix"/> and <paramref name="vector"/> (scaled by <paramref name="α"/>) with this vector (scaled by <paramref name="β"/>).
		/// </summary>
		/// <param name="matrix">The input matrix to be multiplied</param>
		/// <param name="vector">The input vector to be multiplied</param>
		/// <param name="α">The scalar to be multiplied to the <paramref name="matrix"/> of type <typeparamref name="T"/></param>
		/// <param name="β">The scalar to be multiplied to this vector of type <typeparamref name="T"/></param>
		/// <param name="operation">The simple operation to be applied to <paramref name="matrix"/> before computation as a <see cref="MatrixOperation"/></param>
		/// <returns>The addition result of <paramref name="β"/> * this + <paramref name="α"/> * <paramref name="operation"/>(<paramref name="matrix"/>) * <paramref name="vector"/></returns>
		/// <exception cref="NotSupportedException">If <paramref name="vector"/> is neither a <see cref="DenseVector{T}"/> nor a <see cref="SparseVector{T, TIndex}"/> or <paramref name="matrix"/> is neither a <see cref="DenseMatrix{T}"/> nor a <see cref="ISparseMatrix{T}"/></exception>
		public override VectorBase<T> AddMatrixMultiplyVector(MatrixBase<T> matrix, VectorBase<T> vector, T α, T β = default, MatrixOperation operation = MatrixOperation.None)
		{
			if (matrix is null || !matrix.IsValid())
				throw new ArgumentNullException(nameof(matrix));
			if (vector is null || !vector.IsValid())
				throw new ArgumentNullException(nameof(vector));
			if (vector.Length != (operation == MatrixOperation.None ? matrix.NCols : matrix.NRows))
				throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(matrix));
			if (this.Length != (operation == MatrixOperation.None ? matrix.NRows : matrix.NCols))
				throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(matrix));

			Storage<T>? dense = null;
			try
			{
				if (vector is DenseVector<T> d)
					dense = Althea.Storage.StorageFactory<T>.CreateAlike(d.Storage);
				else
					dense = Storage<T>.Create(this.Storage[0].Location, this.Length);
				this.ToDense(dense);
				var dnVec = new DenseVector<T>(dense);
				dnVec.AddByMatrixMultiplyVector(matrix, vector, α, β, operation);
				return dnVec;
			}
			catch (Exception)
			{
				dense?.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Add the <paramref name="other"/> (scaling by <paramref name="scalar"/>) to this vector in-place.
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
			LAD.VectorGeneralAdd(scalar, other.Storage, 1, this.Storage, 1);
		}
		#endregion

		#region clone related
		/// <summary>
		/// Deep clone the sparse vector, the mutable status will not be copied.
		/// </summary>
		/// <returns>The cloned array</returns>
		public override SparseVector<T, TInd> Clone()
		{
			var indexArrays = ((ISparseArray<T, TInd>)this).NewArraysAlike<T, TInd>(out ActualStorage<T> value, copyContent: true);
			return new SparseVector<T, TInd>(this.Length, value, indexArrays[0], this.DefaultValue);
		}

		/// <summary>
		/// Create a new sparse vector with same properties as this one while the underlying storages are not filled.
		/// </summary>
		/// <returns>The new sparse vector alike this one</returns>
		public override SparseVector<T, TInd> NewArrayAlike()
		{
			var indexArrays = ((ISparseArray<T, TInd>)this).NewArraysAlike<T, TInd>(out ActualStorage<T> value, copyContent: false);
			return new SparseVector<T, TInd>(this.Length, value, indexArrays[0], this.DefaultValue);
		}

		/// <summary>
		/// Create a new sparse vector with same properties as this one while the underlying storages are not filled and the data type is changed to <typeparamref name="TOut"/>.
		/// </summary>
		/// <typeparam name="TOut">Any unmanaged struct as the new data type</typeparam>
		/// <returns>The new sparse vector alike this one</returns>
		public override SparseVector<TOut, TInd> NewArrayAlike<TOut>()
		{
			var indexArrays = ((ISparseArray<T, TInd>)this).NewArraysAlike<TOut, TInd>(out ActualStorage<TOut> value, copyContent: true);
			return new SparseVector<TOut, TInd>(this.Length, value, indexArrays[0], this.DefaultValue.GenericConvert<TOut, T>());
		}

		/// <summary>
		/// Create a new sparse vector with same properties as this one while the underlying storages are not filled and the data type is changed to <typeparamref name="TOut"/> while index type changed to <typeparamref name="TIndOut"/>.
		/// </summary>
		/// <typeparam name="TOut">Any unmanaged struct as the new data type</typeparam>
		/// <typeparam name="TIndOut">Any integral-typed unmanaged struct as the new index type</typeparam>
		/// <returns>The new sparse vector alike this one</returns>
		/// <exception cref="TypeMismatchException">If the <typeparamref name="TIndOut"/> is not an integral type</exception>
		public override SparseVector<TOut, TIndOut> NewArrayAlike<TOut, TIndOut>()
		{
			var indexArrays = ((ISparseArray<T, TInd>)this).NewArraysAlike<TOut, TIndOut>(out ActualStorage<TOut> value, copyContent: true);
			return new SparseVector<TOut, TIndOut>(this.Length, value, indexArrays[0], this.DefaultValue.GenericConvert<TOut, T>());
		}
		#endregion

		#region IKrylovVector
		void IKrylovVector<SparseVector<T, TInd>, T>.Scale(T value) => this.Scale(value);

		double IKrylovVector<SparseVector<T, TInd>, T>.Norm() => this.Norm();

		void IKrylovVector<SparseVector<T, TInd>, T>.Normalize() => this.Normalize();

		T IKrylovVector<SparseVector<T, TInd>, T>.Dot(SparseVector<T, TInd> other) => this.Dot(other);

		void IKrylovVector<SparseVector<T, TInd>, T>.AddBy(SparseVector<T, TInd> other, T scalar) => this.AddByVector(other, scalar);

		/// <summary>
		/// Replace this vector's content with the <paramref name="other"/> vector in-place. The default implementation only works when this and <paramref name="other"/> have same sparsity.
		/// </summary>
		/// <param name="other">The other dense vector to replace from</param>
		/// <exception cref="InvalidOperationException">If the replacement cannot be done in-place due to reason(s) such as different sparsities between this and <paramref name="other"/></exception>
		public virtual void ReplaceBy(SparseVector<T, TInd> other)
		{
			this.CheckSparsity(other);
			MEM.MemoryCopy(other.Storage, this.Storage);
		}

		/// <summary>
		/// Multiply the matrix whose columns are indicated by <paramref name="unjoinedVectors"/> to a sparse vector indicated by a <see cref="ReadOnlySpan{T}"/> and obtain the result vector as a <see cref="SparseVector{T, TInd}"/>. The default implementation only works when this and all vectors in <paramref name="unjoinedVectors"/> have same sparsity.
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
	}
}
