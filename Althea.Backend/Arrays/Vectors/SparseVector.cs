using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

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
	public class SparseVector<T, TInd> : Althea.Arrays.SparseVector<T, TInd>, IKrylovVector<SparseVector<T, TInd>, T>, IConvertibleVector<SparseVector<T, TInd>, SparseMatrix<T, TInd>, T>
		where T : unmanaged
		where TInd : unmanaged
	{
		#region basic
		private Storage<TInd> m_originalIndex, m_index;

		/// <summary>
		/// Get the index array's storage as a <see cref="Storage{T}"/> of <typeparamref name="TInd"/>
		/// </summary>
		public Storage<TInd> IndexStorage {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.m_index;
		}

		/// <summary>
		/// Get all the index arrays as a <see cref="ReadOnlySpan{T}"/> of <see cref="Storage{T}"/> of <typeparamref name="TInd"/>
		/// </summary>
		public override ReadOnlySpan<Storage<TInd>> IndexArrays {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => MemoryMarshal.CreateReadOnlySpan(ref this.m_index, 1);
		}

		/// <summary>
		/// Get the original index array's storage of this sparse vector.
		/// </summary>
		protected override ReadOnlySpan<IStorage> OriginalIndexStorages {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<Storage<TInd>, IStorage>(ref this.m_originalIndex), 1);
		}

		/// <summary>
		/// Create an empty <see cref="SparseVector{T, TInd}"/>
		/// </summary>
		public SparseVector() : base(0, Storage<T>.Empty, SparseVectorFormat.Coordinated)
		{
			this.m_index = this.m_originalIndex = Storage<TInd>.Empty;
		}

		/// <summary>
		/// Create a <see cref="SparseVector{T, TInd}"/> (of coordinate-format) with given <paramref name="length"/>, <paramref name="valueArray"/> and <paramref name="indexArray"/> the <paramref name="defaultValue"/>
		/// </summary>
		/// <param name="length">The presenting length of this sparse vector</param>
		/// <param name="valueArray">The value array as a <see cref="Storage{T}"/> of <typeparamref name="T"/></param>
		/// <param name="indexArray">The index array as a <see cref="Storage{T}"/> of <typeparamref name="TInd"/></param>
		/// <param name="defaultValue">The default value (the value not specified) of this sparse vector</param>
		/// <param name="stores">The number of stored values, default 0 means the length of <paramref name="valueArray"/></param>
		/// <exception cref="NotSupportedException">If the <typeparamref name="TInd"/> is not an integral type</exception>
		public SparseVector(long length, Storage<T> valueArray, Storage<TInd> indexArray, T defaultValue = default, long stores = 0) : base(length, valueArray, SparseVectorFormat.Coordinated, defaultValue, stores)
		{
			var span = MemoryMarshal.CreateReadOnlySpan(ref indexArray, 1);
			Storage<TInd> refIndexArray = Storage<TInd>.Empty;
			var outSpan = MemoryMarshal.CreateSpan(ref refIndexArray, 1);
			ISparseArray<T, TInd>.CheckIndexArrays(span, stackalloc long[] { stores }, outSpan);
			this.m_originalIndex = indexArray;
			this.m_index = refIndexArray;
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
				long find = LAS.IndexFind(sorted: true, this.m_index, index.FromLong<TInd>());
				if (find < 0)
					return this.DefaultValue;
				else
					return MEM.ToManaged(this.Storage + find);
			}
			set {
				this.CheckIndex(index);
				long find = LAS.IndexFind(sorted: true, this.m_index, index.FromLong<TInd>());
				if (find < 0)
					throw new InvalidOperationException();
				MEM.FromManaged(this.Storage + find, value);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void GetSlice(TInd start, TInd end, ref Storage<T> value, ref Storage<TInd> index, long pack = 1)
		{
			long offset = LAS.IndexBound(index, start, lowerBound: true);
			long length = LAS.IndexBound(index, end, lowerBound: false) - offset;
			value = value.MakeReference(offset * pack, length * pack);
			index = index.MakeReference(offset, length);
			if (!start.IsZero())
			{
				index = index.Clone();
				try
				{
					LAD.PointWiseAddScalar(index, 1, start.GenericNegate());
				}
				catch (Exception)
				{
					index?.Dispose();
					throw;
				}
			}
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void SetSlice(TInd start, TInd end, Storage<T> value, Storage<TInd> index, Storage<T> setValue, Storage<TInd> setIndex, long pack = 1)
		{
			long offset = LAS.IndexBound(index, start, lowerBound: true);
			long length = LAS.IndexBound(index, end, lowerBound: false) - offset;
			if (setValue.Length != length * pack)
				throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(setValue));
			if (setIndex.Length != length)
				throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(setIndex));

			MEM.MemoryCopy(setValue, value + offset * pack);
			index += offset;
			MEM.MemoryCopy(setIndex, index);
			if (!start.IsZero())
				LAD.PointWiseAddScalar(index, 1, start);
		}

		/// <summary>
		/// Get a sub-vector indicated by the given <paramref name="start"/> offset and <paramref name="count"/>
		/// </summary>
		/// <param name="start">The starting offset of the target sub-vector compared to this vector, in <typeparamref name="T"/></param>
		/// <param name="count">The length of the target sub-vector, in <typeparamref name="T"/></param>
		/// <returns>The sub-vector indicated by <paramref name="start"/> and <paramref name="count"/>.</returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="start"/> and/or <paramref name="count"/> is out of range</exception>
		public override SparseVector<T, TInd> GetSlice(long start, long count)
		{
			this.CheckRange(start, count);
			var value = this.Storage; var index = this.m_index;
			GetSlice(start.FromLong<TInd>(), (start + count).FromLong<TInd>(), ref value, ref index);
			return new SparseVector<T, TInd>(count, value, index, this.DefaultValue);
		}

		/// <summary>
		/// Set the sub-vector indicated by the given <paramref name="start"/> offset and <paramref name="count"/> to <paramref name="value"/>
		/// </summary>
		/// <param name="start">The starting offset of the target sub-vector compared to this vector, in <typeparamref name="T"/></param>
		/// <param name="count">The length of the target sub-vector, in <typeparamref name="T"/></param>
		/// <param name="value">The sub-vector to set</param>
		/// <exception cref="ArgumentNullException">If <paramref name="value"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="start"/> and/or <paramref name="count"/> is out of range</exception>
		/// <exception cref="ArgumentException">If <paramref name="value"/> cannot be used to set</exception>
		public override void SetSlice(long start, long count, VectorBase<T> value)
		{
			if (value is null || !value.IsValid())
				throw new ArgumentNullException(nameof(value));
			if (value is not SparseVector<T, TInd> sparse)
				throw new ArgumentException(Resources.Parameter.UnexpectedType, nameof(value));
			this.CheckRange(start, count);
			SetSlice(start.FromLong<TInd>(), (start + count).FromLong<TInd>(), this.Storage, this.m_index, sparse.Storage, sparse.m_index);
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
			Span<long> size = stackalloc long[] { rows, 0 };
			CheckSize(this, size);
			var wrapper = LAS.SparseVectorToMatrix(this, rows, SparseMatrixFormat.COOC);
			try
			{
				var res = wrapper.CheckWrapper<T, TInd>(size[0], size[1]);
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
		public override SparseTensor<T, TInd> ToTensor(ReadOnlySpan<long> size)
		{
			Span<long> newSize = stackalloc long[size.Length];
			size.CopyTo(newSize);
			CheckSize(this, newSize);
			return new(newSize, this.Storage, this.m_index, defaultValue: this.DefaultValue);
		}
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
			if (this.m_index == other.m_index)
				return;
			if (!LAD.PointWiseEquals(this.m_index, 1, other.m_index, 1))
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
					return wrapper.CheckWrapper<T, TInd>(this.Length);
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
		/// Deep clone the sparse vector, the mutable status will not be copied.
		/// </summary>
		/// <returns>The cloned array</returns>
		public override SparseVector<T, TInd> Clone()
		{
			var outIndex = ActualStorage<TInd>.Empty;
			var span = MemoryMarshal.CreateSpan(ref outIndex, 1);
			var value = ((ISparseArray<T, TInd>)this).CreateArraysAlike<T, TInd>(span, copyValues: true);
			return new SparseVector<T, TInd>(this.Length, value, outIndex, this.DefaultValue);
		}

		/// <summary>
		/// Create a new sparse vector with same properties as this one while the underlying storages are not filled.
		/// </summary>
		/// <returns>The new sparse vector alike this one</returns>
		public override SparseVector<T, TInd> NewArrayAlike() => (SparseVector<T, TInd>)base.NewArrayAlike();

		/// <summary>
		/// Create a new sparse vector with same properties as this one while the underlying storages are not filled and the data type is changed to <typeparamref name="TOut"/> while index type changed to <typeparamref name="TIndOut"/>.
		/// </summary>
		/// <typeparam name="TOut">Any unmanaged struct as the new data type</typeparam>
		/// <typeparam name="TIndOut">Any integral-typed unmanaged struct as the new index type</typeparam>
		/// <returns>The new sparse vector alike this one</returns>
		/// <exception cref="TypeMismatchException">If the <typeparamref name="TIndOut"/> is not an integral type</exception>
		public override SparseVector<TOut, TIndOut> NewArrayAlike<TOut, TIndOut>()
		{
			var outIndex = ActualStorage<TIndOut>.Empty;
			var span = MemoryMarshal.CreateSpan(ref outIndex, 1);
			var value = ((ISparseArray<T, TInd>)this).CreateArraysAlike<TOut, TIndOut>(span, copyValues: false);
			return new SparseVector<TOut, TIndOut>(this.Length, value, outIndex, this.DefaultValue.GenericConvert<T, TOut>());
		}
		#endregion

		#region conversion
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
		SparseVector<T, TInd> IKrylovVector<SparseVector<T, TInd>, T>.NewArrayAlike()
		{
			var values = this.Storage.Clone();
			try
			{
				return new(this.Length, values, this.m_index, this.DefaultValue);
			}
			catch (Exception)
			{
				values?.Dispose();
				throw;
			}
		}

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
				MEM.ToManaged(this.m_index as Storage<long> ?? Storage<long>.Empty, indices);
				return;
			}
			// else
			Span<TInd> temp = indices.Length.CheckStackLimitFast<TInd>() ?? stackalloc TInd[indices.Length];
			MEM.ToManaged(this.m_index, temp);
			temp.CopyTo(indices, static a => a.GenericConvert<TInd, long>());
		}

		/// <summary>
		/// The presenting name of <see cref="IndexStorage"/>
		/// </summary>
		protected internal const string IndexStorageName = nameof(IndexStorage);

		/// <summary>
		/// Get all the storages of this array. Only returns <see cref="ValueArray{T}.Storage"/> and <see cref="IndexStorage"/>.
		/// </summary>
		/// <returns>All the storages of the array as an <see cref="IReadOnlyDictionary{TKey, TValue}"/> of <see cref="string"/> and <see cref="IStorage"/></returns>
		public override IReadOnlyDictionary<string, IStorage> GetStorages() => new Dictionary<string, IStorage>(2)
		{
			[StorageName] = this.Storage,
			[IndexStorageName] = this.m_index,
		};

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
