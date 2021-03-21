using System;
using System.Collections.Generic;

using Althea.Arrays;
using Althea.Helpers;
using Althea.LinearAlgebra;
using Althea.NativeTypes;
using Althea.Solver;

using LAD = Althea.LinearAlgebra.Dense.AbstractApi;
using LAS = Althea.LinearAlgebra.Sparse.AbstractApi;
using MEM = Althea.Storage.AbstractApi;


namespace Althea.Backend.Arrays
{
	/// <summary>
	/// The concrete dense vector class with the only mutable <see cref="ValueArray{T}.Storage"/> that refers to the actual data storage.
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
	public sealed class DenseVector<T> : VectorBase<T>, IKrylovVector<DenseVector<T>, T>, IConvertibleVector<DenseVector<T>, DenseMatrix<T>, T> where T : unmanaged
	{
		#region create and dispose
		/// <summary>
		/// Create an empty dense vector
		/// </summary>
		public DenseVector() : base(Storage<T>.Empty, 0) { }

		/// <summary>
		/// Create a dense vector (may be referenced if <paramref name="storage"/> is a <see cref="ReferenceStorage{T}"/>) from the given <see cref="Storage{T}"/> <paramref name="storage"/> whose <see cref="Storage{T}.Length"/> is the length of this vector.
		/// </summary>
		/// <param name="storage">The given <see cref="Storage{T}"/> as the value array of this vector</param>
		public DenseVector(Storage<T> storage) : base(storage, storage.Length) { }

		/// <summary>
		/// Actually the dispose this array by disposing <see cref="ValueArray{T}.Storage"/>.
		/// </summary>
		/// <param name="disposing">Dispose managed resources or not</param>
		protected override void Dispose(bool disposing) => base.Dispose(disposing);
		#endregion

		#region indexing
		/// <summary>
		/// The basic indexed getter and setter of this vector.
		/// </summary>
		/// <param name="index">The position of the element to get / set</param>
		/// <returns>The element at <paramref name="index"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="index"/> is out of range</exception>
		public override T this[long index] {
			get {
				this.CheckIndex(index);
				return MEM.ToManaged(this.Storage + index);
			}
			set {
				this.CheckIndex(index);
				MEM.FromManaged(this.Storage + index, value);
			}
		}

		/// <summary>
		/// Get a sub-vector indicated by the given <paramref name="start"/> offset and <paramref name="length"/>
		/// </summary>
		/// <param name="start">The starting offset of the target sub-vector compared to this vector, in <typeparamref name="T"/></param>
		/// <param name="length">The length of the target sub-vector, in <typeparamref name="T"/></param>
		/// <returns>The referenced sub-vector indicated by <paramref name="start"/> and <paramref name="length"/>.</returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="start"/> and/or <paramref name="length"/> is out of range</exception>
		public override DenseVector<T> Slice(long start, long length)
		{
			this.CheckRange(start, length);
			return new DenseVector<T>(this.Storage.MakeReference(start, length));
		}
		#endregion

		#region clone related
		/// <summary>
		/// Deep clone the vector. This implementation utilizes <see cref="Storage{T}.Clone"/>.
		/// </summary>
		/// <returns>The cloned vector</returns>
		public override DenseVector<T> Clone()
		{
			var c = this.Storage.Clone();
			return new DenseVector<T>(c);
		}

		/// <summary>
		/// Create a new vector with same properties as this one while the underlying storages are not filled. This implementation utilizes <see cref="Althea.Storage.StorageFactory{T}"/>.
		/// </summary>
		/// <returns>The new vector alike this one</returns>
		public override DenseVector<T> NewArrayAlike()
		{
			var c = Althea.Storage.StorageFactory<T>.CreateAlike(this.Storage);
			return new DenseVector<T>(c);
		}

		/// <summary>
		/// Create a new vector with same properties as this one while the underlying storages are not filled and the data type is changed to <typeparamref name="TOut"/>. This implementation utilizes <see cref="Althea.Storage.StorageFactory{T}"/> of <typeparamref name="TOut"/>.
		/// </summary>
		/// <typeparam name="TOut">Any unmanaged struct as the new data type</typeparam>
		/// <returns>The new vector alike this one</returns>
		public override DenseVector<TOut> NewArrayAlike<TOut>()
		{
			var c = this.Storage.CreateAlike<TOut>();
			return new DenseVector<TOut>(c);
		}
		#endregion

		#region reshape
		/// <summary>
		/// Reshape that simply returns this vector.
		/// </summary>
		/// <returns>This vector</returns>
		public override DenseVector<T> ToVector() => this;

		/// <summary>
		/// Reshape the vector to a matrix with leading dimension = <paramref name="rows"/>
		/// </summary>
		/// <param name="rows">The number of rows of the target matrix; if <paramref name="rows"/> ≤ 0, it is assumed that leadDim = <c>sqrt(<see cref="AbstractArray{T}.Length"/>)</c>.</param>
		/// <returns>The reshaped matrix</returns>
		public override DenseMatrix<T> ToMatrix(long rows = 0)
		{
			Span<long> size = stackalloc long[] { rows, 0 };
			CheckSize(this, size);
			return new DenseMatrix<T>(this.Storage, size[0], size[1]);
		}

		DenseMatrix<T> IConvertibleVector<DenseVector<T>, DenseMatrix<T>, T>.ToMatrix(long rows)
		{
			Span<long> size = stackalloc long[] { rows, 0 };
			CheckSize(this, size);
			return this.ApplyToClone(c => c.ToMatrix(rows));
		}

		/// <summary>
		/// Reshape the array to a tensor with dimensionality = <paramref name="size"/>.
		/// </summary>
		/// <param name="size">The new size/dimensionality with at most one or zero uncertain dimension indicated by a non-positive number.</param>
		/// <returns>The reshaped tensor</returns>
		public override DenseTensor<T> ToTensor(ReadOnlySpan<long> size)
		{
			Span<long> newSize = stackalloc long[size.Length];
			size.CopyTo(newSize);
			CheckSize(this, newSize);
			return new(this.Storage, newSize, newSize);
		}
		#endregion

		#region linear algebra methods
		/// <summary>
		/// When implemented by a derived class, compute the dot (inner) product of this vector and the <paramref name="other"/> vector.
		/// </summary>
		/// <param name="other">The other vector to perform the dot product</param>
		/// <param name="conjugateThis">Whether the dot product is performed on the conjugation of this vector or directly.</param>
		/// <returns>The dot (inner) product result as a <typeparamref name="T"/></returns>
		/// <exception cref="NotSupportedException">If <paramref name="other"/> is neither a <see cref="DenseVector{T}"/> nor a <see cref="Althea.Arrays.SparseVector{T, TIndex}"/></exception>
		/// <exception cref="ArgumentNullException">If <paramref name="other"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="other"/> has different length than this</exception>
		public override T Dot(VectorBase<T> other, bool conjugateThis = true)
		{
			if (other is null || !other.IsValid())
				throw new ArgumentNullException(nameof(other));
			if (other.Length != this.Length)
				throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(other));

			if (other is DenseVector<T>)
				return LAD.Dot(conjugateThis, this.Storage, 1, other.Storage, 1);
			else if (other is ISparseVector<T> sparse)
				return LAS.VectorSparseDotDense(conjugateThis, sparse, this.Storage).GenericConjugate();
			else
				throw new NotSupportedException();
		}

		/// <summary>
		/// When implemented by a derived class, add the <paramref name="other"/> (scaling by <paramref name="scalar"/>) to this vector in-place.
		/// </summary>
		/// <param name="other">The other vector to add</param>
		/// <param name="scalar">The scalar to be multiplied to <paramref name="other"/> of type <typeparamref name="T"/></param>
		/// <exception cref="NotSupportedException">If <paramref name="other"/> is neither a <see cref="DenseVector{T}"/> nor a <see cref="Althea.Arrays.SparseVector{T, TIndex}"/></exception>
		/// <exception cref="ArgumentNullException">If <paramref name="other"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="other"/> has different length than this</exception>
		public void AddByVector(VectorBase<T> other, T scalar)
		{
			if (other is null || !other.IsValid())
				throw new ArgumentNullException(nameof(other));
			if (other.Length != this.Length)
				throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(other));

			if (other is DenseVector<T>)
				LAD.VectorGeneralAdd(scalar, other.Storage, 1, this.Storage, 1);
			else if (other is ISparseVector<T> sparse)
				LAS.VectorSparseAddToDense(scalar, sparse, this.Storage);
			else
				throw new NotSupportedException();
		}

		/// <summary>
		/// When implemented by a derived class, compute the addition of the <paramref name="other"/> vector (scaling by <paramref name="scalar"/>) and this vector.
		/// </summary>
		/// <param name="other">The other vector to add</param>
		/// <param name="scalar">The scalar to be multiplied to <paramref name="other"/> of type <typeparamref name="T"/></param>
		/// <returns>The addition result of this + <paramref name="scalar"/> * <paramref name="other"/></returns>
		/// <exception cref="NotSupportedException">If <paramref name="other"/> is neither a <see cref="DenseVector{T}"/> nor a <see cref="Althea.Arrays.SparseVector{T, TIndex}"/></exception>
		/// <exception cref="ArgumentNullException">If <paramref name="other"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="other"/> has different length than this</exception>
		public override DenseVector<T> AddVector(VectorBase<T> other, T scalar) => this.ApplyToClone(v => v.AddByVector(other, scalar));

		/// <summary>
		/// When implemented by a derived class, add the multiplication result of the given <paramref name="matrix"/> and <paramref name="vector"/> (scaled by <paramref name="α"/>) to this vector (scaled by <paramref name="β"/>) in-place.
		/// </summary>
		/// <param name="matrix">The input matrix to be multiplied</param>
		/// <param name="vector">The input vector to be multiplied</param>
		/// <param name="α">The scalar to be multiplied to the <paramref name="matrix"/> of type <typeparamref name="T"/></param>
		/// <param name="β">The scalar to be multiplied to this vector of type <typeparamref name="T"/></param>
		/// <param name="operation">The simple operation to be applied to <paramref name="matrix"/> before computation as a <see cref="LinearAlgebra.MatrixOperation"/></param>
		/// <returns>The addition result of <paramref name="β"/> * this + <paramref name="α"/> * <paramref name="operation"/>(<paramref name="matrix"/>) * <paramref name="vector"/></returns>
		/// <exception cref="NotSupportedException">If <paramref name="vector"/> is neither a <see cref="DenseVector{T}"/> nor a <see cref="Althea.Arrays.SparseVector{T, TIndex}"/>, or <paramref name="matrix"/> is neither <see cref="DenseMatrix{T}"/> nor <see cref="ISparseMatrix{T}"/></exception>
		/// <exception cref="ArgumentNullException">If <paramref name="matrix"/> or <paramref name="vector"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If this or <paramref name="vector"/> has incompatible length with <paramref name="matrix"/></exception>
		public void AddByMatrixMultiplyVector(MatrixBase<T> matrix, VectorBase<T> vector, T α, T β = default, MatrixOperation operation = MatrixOperation.None)
		{
			if (matrix is null || !matrix.IsValid())
				throw new ArgumentNullException(nameof(matrix));
			if (vector is null || !vector.IsValid())
				throw new ArgumentNullException(nameof(vector));
			if (vector.Length != (operation == MatrixOperation.None ? matrix.NCols : matrix.NRows))
				throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(matrix));
			if (this.Length != (operation == MatrixOperation.None ? matrix.NRows : matrix.NCols))
				throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(matrix));

			var dnMat = matrix as IDenseMatrix<T>;
			var spMat = matrix as ISparseMatrix<T>;
			var dnVec = vector as DenseVector<T>;
			var spVec = vector as ISparseVector<T>;
			if (dnMat is not null && dnVec is not null)
			{
				if (dnMat is SymmetricDenseMatrix<T> symm)
				{
					operation = operation.Simplify<T>(symm.Hermitian);
					SymmetricDenseMatrix<T> s = symm;
					if (operation == MatrixOperation.Conjugate)
						s = symm.Clone();
					try
					{
						LAD.SymmHermMatrixMultiplyVector(s.StoredUpper, s.Hermitian, s.NRows,
														 α, s.Storage, s.LeadDim,
														 dnVec.Storage, 1, β, this.Storage, 1);
						return;
					}
					finally
					{
						if (!ReferenceEquals(s, symm))
							s?.Dispose();
					}
				}
				LAD.GeneralMatrixMultiplyVector(operation, dnMat.NRows, dnMat.NCols, α, matrix.Storage, dnMat.LeadDim, dnVec.Storage, 1, β, this.Storage, 1);
			}
			else if (spMat is not null && dnVec is not null)
			{
				LAS.MatrixSparseMultiplyVectorDense(operation, α, spMat, dnVec.Storage, β, this.Storage);
			}
			else if (dnMat is not null && spVec is not null)
			{
				if (dnMat is SymmetricDenseMatrix<T> symm)
					symm.ToNormal();
				LAS.MatrixDenseMultiplyVectorSparse(operation, α, operation == MatrixOperation.None ? dnMat.NRows : dnMat.NCols, matrix.Storage, dnMat.LeadDim, spVec, β, this.Storage);
			}
			else if (spMat is not null && spVec is not null)
			{
				using var dense = Althea.Storage.StorageFactory<T>.CreateAlike(this.Storage);
				spVec.ToDense(dense);
				this.AddMatrixMultiplyVector(matrix, new DenseVector<T>(dense), α, β, operation);
			}
			else
				throw new NotSupportedException();
		}

		/// <summary>
		/// When implemented by a derived class, compute the addition of the multiplication result of the given <paramref name="matrix"/> and <paramref name="vector"/> (scaled by <paramref name="α"/>) with this vector (scaled by <paramref name="β"/>).
		/// </summary>
		/// <param name="matrix">The input matrix to be multiplied</param>
		/// <param name="vector">The input vector to be multiplied</param>
		/// <param name="α">The scalar to be multiplied to the <paramref name="matrix"/> of type <typeparamref name="T"/></param>
		/// <param name="β">The scalar to be multiplied to this vector of type <typeparamref name="T"/></param>
		/// <param name="operation">The simple operation to be applied to <paramref name="matrix"/> before computation as a <see cref="LinearAlgebra.MatrixOperation"/></param>
		/// <returns>The addition result of <paramref name="β"/> * this + <paramref name="α"/> * <paramref name="operation"/>(<paramref name="matrix"/>) * <paramref name="vector"/></returns>
		public override DenseVector<T> AddMatrixMultiplyVector(MatrixBase<T> matrix, VectorBase<T> vector, T α, T β = default, LinearAlgebra.MatrixOperation operation = MatrixOperation.None) => this.ApplyToClone(v => v.AddByMatrixMultiplyVector(matrix, vector, α, β, operation));
		#endregion

		#region IKrylovVector
		T IKrylovVector<DenseVector<T>, T>.Dot(DenseVector<T> other) => this.Dot(other);

		void IKrylovVector<DenseVector<T>, T>.AddBy(DenseVector<T> other, T scalar) => this.AddByVector(other, scalar);

		/// <summary>
		/// Replace this vector's content with the <paramref name="other"/> vector in-place.
		/// </summary>
		/// <param name="other">The other dense vector to replace from</param>
		/// <exception cref="InvalidOperationException">If the replacement cannot be done in-place due to reason(s) such as different sparsities between this and <paramref name="other"/></exception>
		public void ReplaceBy(DenseVector<T> other)
		{
			if (this.Length != other.Length)
				throw new InvalidOperationException(Resources.Parameter.NotSameSize);

			MEM.MemoryCopy(other.Storage, this.Storage);
		}
		#endregion

		#region equality
		/// <summary>
		/// Get the hash code this dense vector. The default implementation only takes <see cref="ValueArray{T}.Storage"/>'s hash code.
		/// </summary>
		/// <returns>The hash code of <see cref="ValueArray{T}.Storage"/></returns>
		public override int GetHashCode() => this.Storage.GetHashCode();

		/// <summary>
		/// Check whether this object is equal to another one. The default implementation only compares <see cref="ValueArray{T}.Storage"/>.
		/// </summary>
		/// <param name="obj">The other object to compare with</param>
		/// <returns>True if this == <paramref name="obj"/></returns>
		public override bool Equals(object? obj)
		{
			return obj is DenseVector<T> dv && this.Storage == dv.Storage;
		}
		#endregion

		#region print
		internal static string ActualPrint(Storage<T> storage, long actual, PrintSettings settings)
		{
			// get managed array
			int length = (int)Math.Min(settings.ArrayLength, actual);
			Span<T> managed = length.CheckStackLimit<T>() ?? stackalloc T[length];
			MEM.ToManaged(storage, managed);
			// to dense vector string
			string str = managed.ToVectorString(precision: settings.Precision);
			if (actual > managed.Length)
				str += Environment.NewLine + string.Format(Resources.Print.MoreStored, actual - managed.Length);
			return str;
		}

		/// <summary>
		/// Print out the vector.
		/// </summary>
		/// <param name="overrideSetting">Override global settings in <see cref="Settings"/></param>
		/// <returns>The detailed string representation</returns>
		public override string Print(PrintSettings? overrideSetting = null)
		{
			string description = this.ToString();
			if (this.Disposed)
				return description;

			var settings = overrideSetting ?? Settings.PrintSetting;
			return description + ":" + Environment.NewLine + ActualPrint(this.Storage, this.Length, settings);
		}
		#endregion

		#region serialization
		/// <summary>
		/// Get all the storages of this array. Only returns the <see cref="ValueArray{T}.Storage"/>.
		/// </summary>
		/// <returns>All the storages of the array as an <see cref="IReadOnlyDictionary{TKey, TValue}"/> of <see cref="string"/> and <see cref="IStorage"/></returns>
		public override IReadOnlyDictionary<string, IStorage> GetStorages() => new Dictionary<string, IStorage> { [StorageName] = this.Storage };

		/// <summary>
		/// Get other requisite informations for re-constructing the array of that derived class type.
		/// </summary>
		/// <returns>Other requisite informations used to re-construct this array, an empty dictionary.</returns>
		public override IReadOnlyDictionary<string, object> GetMetaData() => new Dictionary<string, object>(0);
		#endregion
	}
}
