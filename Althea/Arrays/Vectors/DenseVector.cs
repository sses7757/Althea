using System;
using System.Collections.Generic;

using Althea.Helpers;
using Althea.NativeTypes;

using MEM = Althea.Storage.AbstractApi;
using LAD = Althea.LinearAlgebra.Dense.AbstractApi;
using LAS = Althea.LinearAlgebra.Sparse.AbstractApi;


namespace Althea.Arrays
{
	/// <summary>
	/// The concrete dense vector class with the only mutable <see cref="ValueArray{T}.Storage"/> that refers to the actual data storage.
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct that implements <see cref="IFormattable"/> and <see cref="IEquatable{T}"/> as the data type</typeparam>
	public class DenseVector<T> : VectorBase<T>, IKrylovVector<DenseVector<T>, T> where T : unmanaged, IFormattable, IEquatable<T>
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
		/// Create a referenced dense vector from the given <paramref name="array"/> and <paramref name="offset"/> and <paramref name="length"/>.
		/// </summary>
		/// <param name="array">The given <see cref="ValueArray{T}"/> whose <see cref="ValueArray{T}.Storage"/> acts as the value array of this vector</param>
		/// <param name="offset">The offset to <paramref name="array"/>'s <see cref="ValueArray{T}.Storage"/> in <typeparamref name="T"/></param>
		/// <param name="length">The new presenting length of this vector</param>
		public DenseVector(ValueArray<T> array, long offset = 0, long length = 0) : base(array.Storage.MakeReference(offset, length), length) { }

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
				if (index < 0 || index >= this.Length)
					throw new ArgumentOutOfRangeException(nameof(index), Resources.Parameter.InvalidValue);
				return MEM.SelectImplementation(this.Storage).ToManaged(this.Storage.MakeReference(offset: index));
			}
			set {
				if (index < 0 || index >= this.Length)
					throw new ArgumentOutOfRangeException(nameof(index), Resources.Parameter.InvalidValue);
				MEM.SelectImplementation(this.Storage).FromManaged(this.Storage.MakeReference(offset: index), value);
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
			if (start < 0 || start >= this.Length)
				throw new ArgumentOutOfRangeException(nameof(start), Resources.Parameter.InvalidValue);
			if (length < 0)
				throw new ArgumentOutOfRangeException(nameof(length), Resources.Parameter.MustPositive);
			if (start + length > this.Length)
				throw new ArgumentOutOfRangeException(nameof(length), Resources.Parameter.InvalidValue);

			return new DenseVector<T>(this, start, length);
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
			var c = Althea.Storage.StorageFactory<T>.CreateAlike<TOut>(this.Storage);
			return new DenseVector<TOut>(c);
		}
		#endregion

		#region conversion methods
		/// <summary>
		/// Convert this vector to a <see cref="DenseVector{T}"/>.
		/// </summary>
		/// <returns>This vector</returns>
		public override DenseVector<T> ToDense() => this;

		/// <summary>
		/// Reshape the vector to a matrix with leading dimension = <paramref name="leadDim"/>
		/// </summary>
		/// <param name="leadDim">The leading dimension of target matrix; if <paramref name="leadDim"/> ≤ 0, it is assumed that leadDim = <c>sqrt(<see cref="AbstractArray{T}.Length"/>)</c>.</param>
		/// <returns>The reshaped matrix</returns>
		public override ValueArray<T> ToMatrix(long leadDim = 0)
		{
			Span<long> size = stackalloc long[2];
			size[0] = leadDim;
			CheckSize(this, size);
			return new DenseMatrix<T>(this.Storage, size[0], size[1]);
		}

		/// <summary>
		/// Reshape the array to a tensor with dimensionality = <paramref name="size"/>.
		/// </summary>
		/// <param name="size">The new size/dimensionality with at most one or zero uncertain dimension indicated by a non-positive number.</param>
		/// <returns>The reshaped tensor</returns>
		public override ValueArray<T> ToTensor(ReadOnlySpan<long> size)
		{
			Span<long> newSize = stackalloc long[size.Length];
			size.CopyTo(newSize);
			CheckSize(this, newSize);
			return new DenseTensor<T>(pointer: this.Storage, newSize);
		}
		#endregion

		#region linear algebra methods
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
				return LAD.SelectImplementation<T>(this.Storage, other.Storage).Dot(conjugateThis, this.Storage, 1, other.Storage, 1);
			else if (other is ISparseVector<T> sparse)
				return LAS.SelectImplementation(this.Storage, sparse).VectorSparseDotDense(conjugateThis, sparse, this.Storage).GenericConjugate();
			else
				throw new NotSupportedException();
		}

		/// <summary>
		/// When implemented by a derived class, add the <paramref name="other"/> (scaling by <paramref name="scalar"/>) to this vector in-place.
		/// </summary>
		/// <param name="other">The other vector to add</param>
		/// <param name="scalar">The scalar to be multiplied to <paramref name="other"/> of type <typeparamref name="T"/></param>
		/// <exception cref="NotSupportedException">If <paramref name="other"/> is neither a <see cref="DenseVector{T}"/> nor a <see cref="SparseVector{T, TIndex}"/></exception>
		/// <exception cref="ArgumentNullException">If <paramref name="other"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="other"/> has different length than this</exception>
		public void AddByVector(VectorBase<T> other, T scalar)
		{
			if (other is null || !other.IsValid())
				throw new ArgumentNullException(nameof(other));
			if (other.Length != this.Length)
				throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(other));

			if (other is DenseVector<T>)
				LAD.SelectImplementation<T>(this.Storage, other.Storage).VectorGeneralAdd(scalar, other.Storage, 1, this.Storage, 1);
			else if (other is ISparseVector<T> sparse)
				LAS.SelectImplementation(this.Storage, other).VectorSparseAddToDense(scalar, sparse, this.Storage);
			else
				throw new NotSupportedException();
		}

		/// <summary>
		/// When implemented by a derived class, compute the addition of the <paramref name="other"/> vector (scaling by <paramref name="scalar"/>) and this vector.
		/// </summary>
		/// <param name="other">The other vector to add</param>
		/// <param name="scalar">The scalar to be multiplied to <paramref name="other"/> of type <typeparamref name="T"/></param>
		/// <returns>The addition result of this + <paramref name="scalar"/> * <paramref name="other"/></returns>
		/// <exception cref="NotSupportedException">If <paramref name="other"/> is neither a <see cref="DenseVector{T}"/> nor a <see cref="SparseVector{T, TIndex}"/></exception>
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
		/// <exception cref="NotSupportedException">If <paramref name="vector"/> is neither a <see cref="DenseVector{T}"/> nor a <see cref="SparseVector{T, TIndex}"/>, or <paramref name="matrix"/> is neither <see cref="DenseMatrix{T}"/> nor <see cref="SparseMatrix{T}"/></exception>
		/// <exception cref="ArgumentNullException">If <paramref name="matrix"/> or <paramref name="vector"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If this or <paramref name="vector"/> has incompatible length with <paramref name="matrix"/></exception>
		public void AddByMatrixMultiplyVector(MatrixBase<T> matrix, VectorBase<T> vector, T α, T β = default, LinearAlgebra.MatrixOperation operation = LinearAlgebra.MatrixOperation.None)
		{
			if (matrix is null || !matrix.IsValid())
				throw new ArgumentNullException(nameof(matrix));
			if (vector is null || !vector.IsValid())
				throw new ArgumentNullException(nameof(vector));
			if (vector.Length != (operation == LinearAlgebra.MatrixOperation.None ? matrix.NCols : matrix.NRows))
				throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(matrix));
			if (this.Length != (operation == LinearAlgebra.MatrixOperation.None ? matrix.NRows : matrix.NCols))
				throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(matrix));

			var dnMat = matrix as DenseMatrix<T>;
			var spMat = matrix as ISparseMatrix<T>;
			var dnVec = vector as DenseVector<T>;
			var spVec = vector as ISparseVector<T>;
			if (dnMat is not null && dnVec is not null)
			{
				LAD.SelectImplementation<T>(this.Storage, dnMat.Storage, dnVec.Storage).GeneralMatrixMultiplyVector(operation, dnMat.NRows, dnMat.NCols, α, dnMat.Storage, dnMat.LeadDim, dnVec.Storage, 1, β, this.Storage, 1);
			}
			else if (spMat is not null && dnVec is not null)
			{
				LAS.SelectImplementation(this.Storage, dnVec.Storage, spMat).MatrixSparseMultiplyVectorDense(operation, α, spMat, dnVec.Storage, β, this.Storage);
			}
			else if (dnMat is not null && spVec is not null)
			{
				LAS.SelectImplementation(this.Storage, dnMat.Storage, spVec).MatrixDenseMultiplyVectorSparse(operation, α, dnMat.NRows, dnMat.NCols, dnMat.Storage, spVec, β, this.Storage);
			}
			else if (spMat is not null && spVec is not null)
			{
				using var dense = vector.ToDense();
				this.AddMatrixMultiplyVector(matrix, dense, α, β, operation);
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
		public override DenseVector<T> AddMatrixMultiplyVector(MatrixBase<T> matrix, VectorBase<T> vector, T α, T β = default, LinearAlgebra.MatrixOperation operation = LinearAlgebra.MatrixOperation.None) => this.ApplyToClone(v => v.AddByMatrixMultiplyVector(matrix, vector, α, β, operation));

		/// <summary>
		/// When implemented by a derived class, aggregately sum the elements in this dense vector. The default implementation only sums <see cref="ValueArray{T}.Storage"/>.
		/// </summary>
		/// <returns>The aggregate sum of this sparse vector</returns>
		public override T Sum() => this.Sum();

		/// <summary>
		/// When implemented by a derived class, aggregately sum the absolute values of elements in this dense vector. The default implementation only sums <see cref="ValueArray{T}.Storage"/>.
		/// </summary>
		/// <returns>The aggregate sum of absolute values of this sparse vector</returns>
		public override double AbsSum() => this.AbsSum();

		/// <summary>
		/// When implemented by a derived class, compute the 2-norm (Euclidean norm) of elements in this dense vector. The default implementation only sums <see cref="ValueArray{T}.Storage"/>.
		/// </summary>
		/// <returns>The 2-norm of this sparse vector</returns>
		public override double Norm() => this.Norm();

		/// <summary>
		/// When implemented by a derived class, in-place scale this dense vector such that its 2-norm (Euclidean norm) is 1. The default implementation utilizes <see cref="ValueArray{T}.Normalize(T)"/>.
		/// </summary>
		/// <exception cref="DivideByZeroException">If the 2-norm of this array is 0</exception>
		public override void Normalize() => this.Normalize();
		#endregion

		#region IKrylovVector
		void IKrylovVector<DenseVector<T>, T>.Scale(T value) => this.Scale(value);

		double IKrylovVector<DenseVector<T>, T>.Norm() => this.Norm();

		void IKrylovVector<DenseVector<T>, T>.Normalize() => this.Normalize();

		T IKrylovVector<DenseVector<T>, T>.Dot(DenseVector<T> other) => this.Dot(other);

		void IKrylovVector<DenseVector<T>, T>.AddByVector(DenseVector<T> other, T scalar) => this.AddByVector(other, scalar);

		/// <summary>
		/// When implemented by a derived class, replace this vector's content with the <paramref name="other"/> vector in-place.
		/// </summary>
		/// <param name="other">The other dense vector to replace from</param>
		/// <exception cref="InvalidOperationException">If the replacement cannot be done in-place due to reason(s) such as different sparsities between this and <paramref name="other"/></exception>
		public void ReplaceBy(DenseVector<T> other)
		{
			if (this.Length != other.Length)
				throw new InvalidOperationException(Resources.Parameter.NotSameSize);

			MEM.SelectImplementation(this.Storage, other.Storage).MemoryCopy(other.Storage, this.Storage);
		}

		/// <summary>
		/// When implemented by a derived class, multiply the matrix whose columns are indicated by <paramref name="unjoinedVectors"/> to a dense vector indicated by a <see cref="ReadOnlySpan{T}"/> and obtain the result vector as a <see cref="VectorBase{T}"/>.
		/// </summary>
		/// <param name="unjoinedVectors">The columns of the matrix to be multiplied</param>
		/// <param name="input">The input dense vector to be multiplied as a <see cref="ReadOnlySpan{T}"/></param>
		/// <returns>The product of <paramref name="unjoinedVectors"/> and <paramref name="input"/> as a <see cref="VectorBase{T}"/></returns>
		/// <remarks>The method shall be basically static, the information of this vector shall only be used to verify the consistency of <paramref name="unjoinedVectors"/></remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="unjoinedVectors"/> or any of its element is null or invalid, or <paramref name="input"/> is empty</exception>
		/// <exception cref="ArgumentException">If <paramref name="input"/> and <paramref name="unjoinedVectors"/> have different size, or any element of <paramref name="unjoinedVectors"/> has different size than this vector</exception>
		/// <exception cref="ObjectDisposedException">If any element of <paramref name="unjoinedVectors"/> is disposed</exception>
		public DenseVector<T> OperateOn(IReadOnlyList<DenseVector<T>> unjoinedVectors, ReadOnlySpan<T> input)
		{
			if (unjoinedVectors is null || unjoinedVectors.Count == 0)
				throw new ArgumentNullException(nameof(unjoinedVectors));
			if (input.IsEmpty)
				throw new ArgumentNullException(nameof(input));
			if (unjoinedVectors.Count != input.Length)
				throw new ArgumentException(Resources.Parameter.NotSameSize);

			// sort first to reduce errors
			int length = input.Length;
			Span<T> values = length * Storage<T>.SizeOfT <= Settings.StackAllocLimit ? stackalloc T[length] : new T[length];
			Span<double> keys = length * Storage<T>.SizeOfT <= Settings.StackAllocLimit ? stackalloc double[length] : new double[length];
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
						vec.AddVector(dnvec, values[i]);
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

		#region equality
		/// <summary>
		/// When implemented by a derived class, get the hash code this dense vector. The default implementation only takes <see cref="ValueArray{T}.Storage"/>'s hash code.
		/// </summary>
		/// <returns>The hash code of <see cref="ValueArray{T}.Storage"/></returns>
		public override int GetHashCode() => this.Storage.GetHashCode();

		/// <summary>
		/// When implemented by a derived class, check whether this object is equal to another one. The default implementation only compares <see cref="ValueArray{T}.Storage"/>.
		/// </summary>
		/// <param name="obj">The other object to compare with</param>
		/// <returns>True if this == <paramref name="obj"/></returns>
		public override bool Equals(object? obj)
		{
			return obj is DenseVector<T> dv && this.Storage == dv.Storage;
		}
		#endregion

		#region print
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

			string detail = ":" + Environment.NewLine;
			int length = (int)Math.Min(settings.ArrayLength, this.Length);
			Span<T> managed = length * Storage<T>.SizeOfT <= Settings.StackAllocLimit ? stackalloc T[length] : new T[length];
			MEM.SelectImplementation(this.Storage).ToManaged(this.Storage, managed);
			detail += managed.ToVectorString(precision: settings.Precision);
			if (this.Length > managed.Length)
				detail += Environment.NewLine + $"...{this.Length - managed.Length} more elements";
			return description + detail;
		}
		#endregion

		#region serialization
		/// <summary>
		/// Get all the storages of this array. Only returns the <see cref="ValueArray{T}.Storage"/>.
		/// </summary>
		/// <returns>All the storages of the array as an <see cref="IReadOnlyDictionary{TKey, TValue}"/> of <see cref="string"/> and <see cref="IStorage"/></returns>
		public override IReadOnlyDictionary<string, IStorage> GetPointers() => new Dictionary<string, IStorage> { [StorageName] = this.Storage };

		/// <summary>
		/// Get other requisite informations for re-constructing the array of that derived class type.
		/// </summary>
		/// <returns>Other requisite informations used to re-construct this array, an empty dictionary.</returns>
		public override IReadOnlyDictionary<string, object> GetOtherInfo() => new Dictionary<string, object>(0);
		#endregion
	}
}
