using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Althea.Arrays;
using Althea.Helpers;
using Althea.LinearAlgebra;
using Althea.LinearAlgebra.Sparse;
using Althea.Linq;
using Althea.NativeTypes;
using Althea.Solver;

using LAD = Althea.LinearAlgebra.Dense.AbstractApi;
using LAS = Althea.LinearAlgebra.Sparse.AbstractApi;
using MEM = Althea.Storage.AbstractApi;


namespace Althea.Backend.Arrays
{
	/// <summary>
	/// The concrete sparse vector class with the only mutable <see cref="ValueArray{T}.Storage"/> that refers to the value array storage and the <see cref="SparseVector{T, TInd}.IndexStorage"/> that refers to the <b>sorted</b> index array storage.
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
	/// <typeparam name="TInd">Any integer-typed unmanaged struct as the index type</typeparam>
	/// <remarks>The only supported format is <see cref="SparseVectorFormat.Coordinated"/> and the <see cref="SparseVector{T, TInd}.IndexStorage"/> is sorted. Any external operation that disturbs such order may result in unexpected consequences.</remarks>
	public sealed class SparseVector<T, TInd> : Althea.Arrays.SparseVector<T, TInd>, IKrylovVector<SparseVector<T, TInd>, T>
		where T : unmanaged
		where TInd : unmanaged
	{
		#region basic
		/// <summary>
		/// Get the index array's storage as a <see cref="Storage{T}"/> of <typeparamref name="TInd"/>
		/// </summary>
		public Storage<TInd> IndexStorage {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.m_indexArrays[0];
		}

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
		#endregion

		#region helper
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static SparseVector<T, TInd> CheckWrapper(long length, T def, SparseArrayWrapper<T> wrapper)
		{
			if (wrapper.ValueStorage is null || wrapper.ValueStorage.Length <= 0)
				throw new ArgumentOutOfRangeException(nameof(wrapper), wrapper.ValueStorage?.Length, Resources.Parameter.ZeroSize);
			if (wrapper.IndexStorages is null || wrapper.IndexStorages.Count <= 0)
				throw new ArgumentOutOfRangeException(nameof(wrapper), wrapper.IndexStorages?.Count, Resources.Parameter.ZeroSize);
			if (wrapper.IndexStorages.Count != 1)
				throw new ArgumentOutOfRangeException(nameof(wrapper), wrapper.IndexStorages.Count, Resources.Parameter.WrongSize);
			if (wrapper.IndexStorages[0] is not Storage<TInd> indices)
				throw new ArgumentOutOfRangeException(nameof(wrapper), wrapper.IndexStorages[0], Resources.Parameter.UnexpectedType);
			if (wrapper.ValueStorage.Length > length)
				throw new ArgumentOutOfRangeException(nameof(length), length, Resources.Parameter.InvalidValue);
			if (wrapper.VectorFormat != SparseVectorFormat.Coordinated)
				throw new ArgumentOutOfRangeException(nameof(wrapper), wrapper.VectorFormat, Resources.Parameter.InvalidValue);

			return new SparseVector<T, TInd>(length, wrapper.ValueStorage, indices, def);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static Althea.Arrays.SparseMatrix<T, TInd> CheckWrapper(long rows, long cols, T def, SparseArrayWrapper<T> wrapper)
		{
			if (wrapper.ValueStorage is null || wrapper.ValueStorage.Length <= 0)
				throw new ArgumentOutOfRangeException(nameof(wrapper), wrapper.ValueStorage?.Length, Resources.Parameter.ZeroSize);
			if (wrapper.IndexStorages is null || wrapper.IndexStorages.Count <= 0)
				throw new ArgumentOutOfRangeException(nameof(wrapper), wrapper.IndexStorages?.Count, Resources.Parameter.ZeroSize);
			if (wrapper.IndexStorages.Count != 2)
				throw new ArgumentOutOfRangeException(nameof(wrapper), wrapper.IndexStorages.Count, Resources.Parameter.WrongSize);
			if (wrapper.IndexStorages[0] is not Storage<TInd> rowIndex)
				throw new ArgumentOutOfRangeException(nameof(wrapper), wrapper.IndexStorages[0], Resources.Parameter.UnexpectedType);
			if (wrapper.IndexStorages[1] is not Storage<TInd> colIndex)
				throw new ArgumentOutOfRangeException(nameof(wrapper), wrapper.IndexStorages[1], Resources.Parameter.UnexpectedType);
			if (wrapper.ValueStorage.Length > rows * cols)
				throw new ArgumentException(Resources.Parameter.WrongSize);

			if ((wrapper.MatrixFormat & FormatExtension.NonBlocked) == wrapper.MatrixFormat)
				return new SparseMatrix<T, TInd>(rows, cols, wrapper.ValueStorage, rowIndex, colIndex, wrapper.MatrixFormat, def);
			else if ((wrapper.MatrixFormat & FormatExtension.Blocked) == wrapper.MatrixFormat)
			{
				if (wrapper.OtherInfo is not BlockedSparseMatrixOtherInfo info)
					throw new ArgumentOutOfRangeException(nameof(wrapper), wrapper.OtherInfo, Resources.Parameter.UnexpectedType);
				return new BlockedSparseMatrix<T, TInd>(rows, cols, info.BlockRows, info.BlockCols, wrapper.ValueStorage, rowIndex, colIndex, wrapper.MatrixFormat, def);
			}
			else
				throw new ArgumentOutOfRangeException(nameof(wrapper), wrapper.VectorFormat, Resources.Parameter.InvalidValue);
		}
		#endregion

		#region indexer
		/// <summary>
		/// The basic indexed getter and setter of this vector.
		/// </summary>
		/// <param name="index">The position of the element to get / set</param>
		/// <returns>The element at <paramref name="index"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="index"/> is out of range</exception>
		/// <exception cref="InvalidOperationException">If this[<paramref name="index"/>] returns the <see cref="Althea.Arrays.SparseVector{T, TInd}.DefaultValue"/> which cannot be set individually</exception>
		public override T this[long index] {
			get {
				this.CheckIndex(index);
				long find = LAS.IndexFind(sorted: true, this.IndexStorage, index.FromLong<TInd>());
				if (find < 0)
					return this.DefaultValue;
				else
					return MEM.ToManaged(this.Storage + find);
			}
			set {
				this.CheckIndex(index);
				long find = LAS.IndexFind(sorted: true, this.IndexStorage, index.FromLong<TInd>());
				if (find < 0)
					throw new InvalidOperationException();
				MEM.FromManaged(this.Storage + find, value);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void Slice(TInd start, TInd end, ref Storage<T> value, ref Storage<TInd> index, long pack = 1)
		{
			long offset = LAS.IndexBound(index, start, lowerBound: true);
			long length = LAS.IndexBound(index, end, lowerBound: false) - offset;
			value = value.MakeReference(offset * pack, length * pack);
			index = index.MakeReference(offset, length);
		}

		/// <summary>
		/// Get a sub-vector indicated by the given <paramref name="start"/> offset and <paramref name="count"/>
		/// </summary>
		/// <param name="start">The starting offset of the target sub-vector compared to this vector, in <typeparamref name="T"/></param>
		/// <param name="count">The length of the target sub-vector, in <typeparamref name="T"/></param>
		/// <returns>The referenced sub-vector indicated by <paramref name="start"/> and <paramref name="count"/>.</returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="start"/> and/or <paramref name="count"/> is out of range</exception>
		public override SparseVector<T, TInd> Slice(long start, long count)
		{
			this.CheckRange(start, count);
			var value = this.Storage; var index = this.IndexStorage;
			Slice(start.FromLong<TInd>(), (start + count).FromLong<TInd>(), ref value, ref index);
			return new SparseVector<T, TInd>(count, value, index, this.DefaultValue);
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

		/// <summary>
		/// Convert the sparse vector to a matrix with leading dimension = <paramref name="rows"/>
		/// </summary>
		/// <param name="rows">The number of rows of the target matrix; if <paramref name="rows"/> ≤ 0, it is assumed that leadDim = <c>sqrt(<see cref="AbstractArray{T}.Length"/>)</c>.</param>
		/// <returns>The reshaped matrix (a newly created one)</returns>
		public override SparseMatrix<T, TInd> ToMatrix(long rows = 0)
		{
			Span<long> size = stackalloc long[2].SetValue(rows);
			CheckSize(this, size);
			var wrapper = LAS.SparseVectorToMatrix(this, rows, SparseMatrixFormat.COOC);
			try
			{
				var res = CheckWrapper(size[0], size[1], this.DefaultValue, wrapper);
				if (res is not SparseMatrix<T, TInd> ss)
					throw new InvalidOperationException(Resources.Support.Format);
				return ss;
			}
			catch (Exception)
			{
				wrapper.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Reshape the array to a tensor with dimensionality = <paramref name="size"/>.
		/// </summary>
		/// <param name="size">The new size/dimensionality with at most one or zero uncertain dimension indicated by a non-positive number.</param>
		/// <returns>The reshaped tensor</returns>
		public override ValueArray<T> ToTensor(ReadOnlySpan<long> size) { }
		#endregion

		#region linear algebra
		/// <summary>
		/// Check the sparsity of this sparse vector and the <paramref name="other"/> one
		/// </summary>
		/// <param name="other">The other sparse vector to check sparsity</param>
		/// <exception cref="ArgumentNullException">If <paramref name="other"/> is null or invalid</exception>
		/// <exception cref="InvalidOperationException">If the <paramref name="other"/> vector has different sparsity from this one</exception>
		public void CheckSparsity(SparseVector<T, TInd> other)
		{
			if (other is null || !other.IsValid())
				throw new ArgumentNullException(nameof(other));
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
			{
				var wrapper = LAS.VectorSparseAddSparse(this, sparse, SparseVectorFormat.Coordinated);
				try
				{
					return CheckWrapper(this.Length, this.DefaultValue, wrapper);
				}
				catch (Exception)
				{
					wrapper.Dispose();
					throw;
				}
			}
			else if (other is DenseVector<T>)
				return other.ApplyToClone(d => LAS.VectorSparseAddToDense(Const<T>.One, this, d.Storage));
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
			if (other is not SparseVector<T, TInd> sparse)
				throw new NotSupportedException();
			this.CheckSparsity(sparse);

			LAD.VectorGeneralAdd(scalar, other.Storage, 1, this.Storage, 1);
		}
		#endregion

		#region clone related
		/// <summary>
		/// Create a new sparse vector with same properties as this one while the underlying storages are not filled.
		/// </summary>
		/// <returns>The new sparse vector alike this one</returns>
		public override SparseVector<T, TInd> NewArrayAlike()
		{
			var indexArrays = ((ISparseArray<T, TInd>)this).NewArraysAlike<T, TInd>(out ActualStorage<T> value, copyValues: false);
			return new SparseVector<T, TInd>(this.Length, value, indexArrays[0], this.DefaultValue);
		}

		/// <summary>
		/// Create a new sparse vector with same properties as this one while the underlying storages are not filled and the data type is changed to <typeparamref name="TOut"/>.
		/// </summary>
		/// <typeparam name="TOut">Any unmanaged struct as the new data type</typeparam>
		/// <returns>The new sparse vector alike this one</returns>
		public override SparseVector<TOut, TInd> NewArrayAlike<TOut>()
		{
			var indexArrays = ((ISparseArray<T, TInd>)this).NewArraysAlike<TOut, TInd>(out ActualStorage<TOut> value, copyValues: true);
			return new SparseVector<TOut, TInd>(this.Length, value, indexArrays[0], this.DefaultValue.GenericConvert<T, TOut>());
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
			var indexArrays = ((ISparseArray<T, TInd>)this).NewArraysAlike<TOut, TIndOut>(out ActualStorage<TOut> value, copyValues: true);
			return new SparseVector<TOut, TIndOut>(this.Length, value, indexArrays[0], this.DefaultValue.GenericConvert<T, TOut>());
		}
		#endregion

		#region conversion
		/// <summary>
		/// Deep clone the sparse vector, the mutable status will not be copied.
		/// </summary>
		/// <returns>The cloned array</returns>
		public override SparseVector<T, TInd> Clone()
		{
			var indexArrays = ((ISparseArray<T, TInd>)this).NewArraysAlike<T, TInd>(out ActualStorage<T> value, copyValues: true);
			return new SparseVector<T, TInd>(this.Length, value, indexArrays[0], this.DefaultValue);
		}

		/// <summary>
		/// Convert this sparse vector to another sparse vector with <see cref="Althea.Arrays.SparseVector{T, TInd}.Format"/> fitting <paramref name="format"/>
		/// </summary>
		/// <param name="format">The target format, can be anatomic</param>
		/// <returns>Since no format other than <see cref="SparseVectorFormat.Coordinated"/> is supported internally, simply returns this vector or throw exception</returns>
		/// <exception cref="NotSupportedException">If <paramref name="format"/> does not contains flag <see cref="SparseVectorFormat.Coordinated"/></exception>
		public override SparseVector<T, TInd> ToFormat(SparseVectorFormat format)
		{
			if ((format & SparseVectorFormat.Coordinated) == 0)
				throw new NotSupportedException(Resources.Support.Format);
			return this;
		}
		#endregion

		#region IKrylovVector
		T IKrylovVector<SparseVector<T, TInd>, T>.Dot(SparseVector<T, TInd> other) => this.Dot(other);

		void IKrylovVector<SparseVector<T, TInd>, T>.AddBy(SparseVector<T, TInd> other, T scalar) => this.AddByVector(other, scalar);

		/// <summary>
		/// Replace this vector's content with the <paramref name="other"/> vector in-place. The default implementation only works when this and <paramref name="other"/> have same sparsity.
		/// </summary>
		/// <param name="other">The other <see cref="SparseVector{T, TInd}"/> to replace from</param>
		/// <exception cref="InvalidOperationException">If the replacement cannot be done in-place due to reason(s) such as different sparsities between this and <paramref name="other"/></exception>
		public void ReplaceBy(SparseVector<T, TInd> other)
		{
			this.CheckSparsity(other);
			MEM.MemoryCopy(other.Storage, this.Storage);
		}
		#endregion

		#region protected overrides
		/// <summary>
		/// The helper method used in <see cref="Althea.Arrays.SparseVector{T, TInd}.Print(PrintSettings?)"/> to get the first several indices of this sparse vector
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
			Span<TInd> temp = indices.Length.CheckStackLimit<TInd>() ?? stackalloc TInd[indices.Length];
			MEM.ToManaged(this.IndexStorage, temp);
			temp.CopyTo(indices, static a => a.GenericConvert<TInd, long>());
		}

		/// <summary>
		/// The helper method used by <see cref="Althea.Arrays.SparseVector{T, TInd}.GetStorages"/> to get the index storages' names. Only used when the sparse array contains more than one index storages.
		/// </summary>
		/// <param name="orderOfIndexStorage">The index of all index storages of this sparse vector</param>
		/// <returns>The name the index storage indicated by the given <paramref name="orderOfIndexStorage"/></returns>
		protected override string IndexStorageNameOf(int orderOfIndexStorage) => IndexStorageName;

		/// <summary>
		/// Get other requisite informations for re-constructing the sparse vector of that derived class type. The default implementation returns <see cref="Althea.Arrays.SparseVector{T, TInd}.DefaultValue"/>.
		/// </summary>
		/// <returns>Other requisite informations used to re-construct this sparse vector.</returns>
		public override IReadOnlyDictionary<string, object> GetMetaData() => new Dictionary<string, object>(1)
		{
			[DefaultValueName] = this.DefaultValue,
		};
		#endregion
	}
}
