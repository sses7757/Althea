using System;
using System.Collections.Generic;

using Althea.NativeTypes;


namespace Althea.Arrays
{
	/// <summary>
	/// The interface of vector that contains the operation needed for Krylov-subspace methods such as Lanczos and Krylov-Schur solver.
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
	/// <typeparam name="TVec">The vector type</typeparam>
	public interface IKrylovVector<TVec, T>
		where TVec : class, IKrylovVector<TVec, T>, new()
		where T : unmanaged, IEquatable<T>
	{
		#region operation
		/// <summary>
		/// When implemented by a derived class, point-wisely in-place multiply this vector with given <paramref name="value"/>.
		/// </summary>
		/// <param name="value">The scalar as <typeparamref name="T"/> to multiply</param>
		void Scale(T value);

		/// <summary>
		/// When implemented by a derived class, compute the 2-norm (Euclidean norm) of elements in this vector.
		/// </summary>
		/// <returns>The 2-norm of this vector</returns>
		double Norm();

		/// <summary>
		/// When implemented by a derived class, in-place scale this vector such that its 2-norm (Euclidean norm) is one.
		/// </summary>
		/// <exception cref="DivideByZeroException">If the 2-norm of this array is 0</exception>
		void Normalize();

		/// <summary>
		/// When implemented by a derived class, compute dot (inner) product of this vector and <paramref name="other"/> vector. The conjugate of this vector shall be actually used.
		/// </summary>
		/// <param name="other">The other <typeparamref name="TVec"/> to perform the dot product</param>
		/// <returns>The dot (inner) product result as a <typeparamref name="T"/></returns>
		T Dot(TVec other);

		/// <summary>
		/// When implemented by a derived class, add the <paramref name="other"/> (scaling by <paramref name="scalar"/>) to this vector in-place.
		/// </summary>
		/// <param name="other">The other <typeparamref name="TVec"/> to add</param>
		/// <param name="scalar">The scalar to be multiplied to <paramref name="other"/> of type <typeparamref name="T"/></param>
		void AddByVector(TVec other, T scalar);

		/// <summary>
		/// When implemented by a derived class, replace this vector's content with the <paramref name="other"/> vector in-place.
		/// </summary>
		/// <param name="other">The other <typeparamref name="TVec"/> to replace from</param>
		/// <exception cref="InvalidOperationException">If the replacement cannot be done in-place due to reason(s) such as different sparsities between this and <paramref name="other"/></exception>
		void ReplaceBy(TVec other);

		/// <summary>
		/// When implemented by a derived class, multiply the matrix whose columns are indicated by <paramref name="unjoinedVectors"/> to a dense vector indicated by a <see cref="ReadOnlySpan{T}"/> and obtain the result vector as a <see cref="VectorBase{T}"/>.
		/// </summary>
		/// <param name="unjoinedVectors">The columns of the matrix to be multiplied</param>
		/// <param name="input">The input dense vector to be multiplied as a <see cref="ReadOnlySpan{T}"/></param>
		/// <returns>The product of <paramref name="unjoinedVectors"/> and <paramref name="input"/> as a <see cref="VectorBase{T}"/></returns>
		/// <remarks>The method shall be basically static, the information of this vector shall only be used to verify the consistency of <paramref name="unjoinedVectors"/></remarks>
		TVec OperateOn(IReadOnlyList<TVec> unjoinedVectors, ReadOnlySpan<T> input);
		#endregion
	}

	#region sparse arrays related
	/// <summary>
	/// Simple interface for sparse arrays, inherits <see cref="IReadOnlyList{T}"/> of <see cref="IStorage"/>. The index type is not indicated
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
	public interface ISparseArray<T> : IReadOnlyList<IStorage> where T : unmanaged
	{
		#region property
		/// <summary>
		/// When implemented by a derived class, get the value array storage of this sparse array.
		/// </summary>
		Storage<T> Storage { get; }

		/// <summary>
		/// When implemented by a derived class, get the number of non-default values (the values that are actually stored) of this sparse array. The default implementation returns the <see cref="Storage{T}.Length"/> of <see cref="Storage"/>
		/// </summary>
		long NStored => this.Storage.Length;

		/// <summary>
		/// When implemented by a derived class, get the (major) data type of the index array(s) of this sparse array as a <see cref="DataType"/>
		/// </summary>
		DataType IndexType { get; }

		/// <summary>
		/// When implemented by a derived class, get the default value of this sparse array
		/// </summary>
		T DefaultValue { get; protected internal set; }
		#endregion

		#region dispose
		/// <summary>
		/// When implemented by a derived class, dispose this sparse array after excluding the internal storages shared between this array and the target <paramref name="array"/>.
		/// </summary>
		/// <param name="array">The target <see cref="ISparseArray{T}"/> to exclude before disposing</param>
		void DisposeExclude(ISparseArray<T> array);
		#endregion
	}

	/// <summary>
	/// Simple interface for sparse arrays, inherits <see cref="IReadOnlyList{T}"/> of <see cref="Storage{T}"/> of <typeparamref name="TIndex"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
	/// <typeparam name="TIndex">Any integer-typed unmanaged struct as the index data type</typeparam>
	public interface ISparseArray<T, TIndex> : ISparseArray<T>, IReadOnlyList<Storage<TIndex>>
		where T : unmanaged, IEquatable<T>
		where TIndex : unmanaged
	{
		#region dispose
		/// <summary>
		/// When implemented by a derived class, dispose this sparse array after excluding the internal storages shared between this array and the target <paramref name="array"/>.
		/// </summary>
		/// <param name="array">The target <see cref="ISparseArray{T, TIndex}"/> to exclude before disposing</param>
		void DisposeExclude(ISparseArray<T, TIndex> array);
		#endregion
	}
	/// <summary>
	/// The interface for sparse vectors without indicating the index data type. The value array is <see cref="ISparseArray{T}.Storage"/> and index array(s) is/are the inherited <see cref="IReadOnlyList{T}"/> of <see cref="IStorage"/>s.
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
	public interface ISparseVector<T> : ISparseArray<T> where T : unmanaged
	{
		#region property
		/// <summary>
		/// When implemented by a derived class, get the presenting length of this sparse vector
		/// </summary>
		long Length { get; }

		/// <summary>
		/// When implemented by a derived class, get the format of this sparse vector as a <see cref="LinearAlgebra.Sparse.SparseVectorFormat"/>
		/// </summary>
		LinearAlgebra.Sparse.SparseVectorFormat Format { get; }
		#endregion

		#region conversion
		/// <summary>
		/// Convert this sparse vector to a dense vector whose <see cref="Storage{T}"/> is <paramref name="denseStorage"/>
		/// </summary>
		/// <param name="denseStorage">The <see cref="Storage{T}"/> of the dense vector to overwrite</param>
		void ToDense(Storage<T> denseStorage);
		#endregion
	}

	/// <summary>
	/// The interface for sparse matrices without indicating the index data type. The value array is <see cref="ISparseArray{T}.Storage"/> and index array(s) is/are the inherited <see cref="IReadOnlyList{T}"/> of <see cref="IStorage"/>s.
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
	public interface ISparseMatrix<T> : ISparseArray<T> where T : unmanaged
	{
		#region property
		/// <summary>
		/// When implemented by a derived class, get the presenting number of rows of this sparse matrix
		/// </summary>
		long NRows { get; }

		/// <summary>
		/// When implemented by a derived class, get the presenting number of columns of this sparse matrix
		/// </summary>
		long NCols { get; }

		/// <summary>
		/// When implemented by a derived class, get the format of this sparse matrix as a <see cref="LinearAlgebra.Sparse.SparseMatrixFormat"/>
		/// </summary>
		LinearAlgebra.Sparse.SparseMatrixFormat Format { get; }
		#endregion

		#region conversion
		/// <summary>
		/// Convert this sparse matrix to a dense matrix whose <see cref="Storage{T}"/> is <paramref name="denseStorage"/>
		/// </summary>
		/// <param name="denseStorage">The <see cref="Storage{T}"/> of the dense matrix to overwrite</param>
		/// <param name="leadDim">The leading dimension of the target dense matrix</param>
		void ToDense(Storage<T> denseStorage, long leadDim);
		#endregion
	}
	#endregion
}
