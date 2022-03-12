using System;

using Althea.Arrays;
using Althea.Helpers;

using Althea.SourceGenerator;


namespace Althea.LinearAlgebra.Sparse
{
	/// <summary>
	/// The abstract class for runtime sparse linear algebra API routines 
	/// </summary>
	[AbstractRuntimeApi]
	public abstract partial class AbstractApi : AbstractRuntimeApi<AbstractApi>
	{
		#region vector
		/// <summary>
		/// When implemented by a derived class, set the <paramref name="x"/>'s values at certain <paramref name="positions"/> to the give <paramref name="value"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TInd">Any integral-typed unmanaged number as the index type</typeparam>
		/// <param name="x">The input vector whose values will be set</param>
		/// <param name="positions">The given positions as a <see cref="Storage{T}"/> of <see cref="int"/></param>
		/// <param name="value">The value to set</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="positions"/> is null or invalid</exception>
		[AbstractApiMethod]
		public abstract bool VectorSetValuesAt<T, TInd>(Storage<T> x, T value, Storage<TInd> positions) where T : unmanaged, INumber<T> where TInd : unmanaged;

		/// <summary>
		/// When implemented by a derived class, scatter (and overwrite) the sparse vector <paramref name="x"/> to the dense vector <paramref name="y"/>: <paramref name="y"/>[<paramref name="x"/>.Indices] = <paramref name="x"/>.<see cref="ISparseArray{T}.Storage">Values</see>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="x">The sparse vector x as a <see cref="ISparseVector{T}"/></param>
		/// <param name="y">The dense vector y as a <see cref="Storage{T}"/> whose elements at <paramref name="x"/>.Indices are overwritten</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		[AbstractApiMethod]
		public abstract bool VectorSparseToDense<T>(ISparseVector<T> x, Storage<T> y) where T : unmanaged, INumber<T>;

		/// <summary>
		/// When implemented by a derived class, gather the dense vector <paramref name="x"/> at the underlying position array of <paramref name="y"/> into the <see cref="ISparseArray{T}.Storage"/> of sparse vector <paramref name="y"/>: <c><paramref name="y"/>.<see cref="ISparseArray{T}.Storage">Storage</see> = <paramref name="x"/>[<paramref name="y"/>.Position]</c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="x">The input dense vector as a <see cref="Storage{T}"/> to gather from</param>
		/// <param name="y">The input (sparse index) and output (value array) sparse vector</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <remarks>This is equivalent to converting dense vector to sparse vector when the sparsity is known</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		[AbstractApiMethod]
		public abstract bool VectorGatherValuesAt<T>(Storage<T> x, ISparseVector<T> y) where T : unmanaged, INumber<T>;

		/// <summary>
		/// When implemented by a derived class, convert a dense vector <paramref name="x"/> to a sparse vector by the given truncation <paramref name="threshold"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="x">The input dense vector as a <see cref="Storage{T}"/></param>
		/// <param name="threshold">Any element in <paramref name="x"/> whose absolute value is less than or equals to <paramref name="threshold"/> will be regarded as zero</param>
		/// <param name="format">The desired <see cref="SparseVectorFormat"/> of the target sparse vector, can be anatomic</param>
		/// <param name="target">Output the created new <see cref="ISparseVector{T}"/> with format fitting <paramref name="format"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="threshold"/> is less than 0</exception>
		[AbstractApiMethod]
		public abstract bool VectorDenseToSparse<T>(Storage<T> x, SparseVectorFormat format, out SparseArrayWrapper<T> target, float threshold = 0) where T : unmanaged, INumber<T>;
		#endregion

		#region vector and matrix
		/// <summary>
		/// When implemented by a derived class, convert the given sparse <paramref name="vector"/> to a sparse matrix of <paramref name="format"/> and presenting number of <paramref name="rows"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="vector">The input sparse vector as a <see cref="ISparseVector{T}"/></param>
		/// <param name="rows">The desired number of rows of the target sparse matrix (the number of columns is calculated from this)</param>
		/// <param name="format">The desired <see cref="SparseMatrixFormat"/> of the target sparse matrix, can be anatomic</param>
		/// <param name="target">Output the created new <see cref="ISparseMatrix{T}"/> with format fitting <paramref name="format"/> and size fitting <paramref name="rows"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="vector"/> is null or invalid</exception>
		[AbstractApiMethod]
		public abstract bool SparseVectorToMatrix<T>(ISparseVector<T> vector, long rows, SparseMatrixFormat format, out SparseArrayWrapper<T> target) where T : unmanaged, INumber<T>;

		/// <summary>
		/// When implemented by a derived class, convert the given sparse <paramref name="format"/> to a sparse vector of given <paramref name="format"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="matrix">The input sparse matrix as a <see cref="ISparseMatrix{T}"/></param>
		/// <param name="format">The desired <see cref="SparseVectorFormat"/> of the target sparse vector, can be anatomic</param>
		/// <param name="target">Output the created new <see cref="ISparseVector{T}"/> with format fitting <paramref name="format"/> and desired properties (the length is the product of <see cref="IMatrixMetric.NRows"/> and <see cref="IMatrixMetric.NCols"/>)</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="matrix"/> is null or invalid</exception>
		[AbstractApiMethod]
		public abstract bool SparseMatrixToVector<T>(ISparseMatrix<T> matrix, SparseVectorFormat format, out SparseArrayWrapper<T> target) where T : unmanaged, INumber<T>;
		#endregion

		#region matrix
		/// <summary>
		/// When implemented by a derived class, convert the given sparse matrix <paramref name="source"/> to a dense matrix <paramref name="destination"/>.
		/// </summary>
		/// <param name="source">The source sparse matrix to convert from</param>
		/// <param name="destination">The storage of the destination dense matrix of the same size as <paramref name="source"/></param>
		/// <param name="ld">The leading dimension of <paramref name="destination"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="destination"/> is null or invalid</exception>
		[AbstractApiMethod]
		public abstract bool MatrixSparseToDense<T>(ISparseMatrix<T> source, Storage<T> destination, long ld) where T : unmanaged, INumber<T>;

		/// <summary>
		/// When implemented by a derived class, convert the given dense matrix <paramref name="source"/> to a sparse matrix of the given <paramref name="format"/>.
		/// </summary>
		/// <param name="m">The number of rows of <paramref name="source"/></param>
		/// <param name="n">The number of columns of <paramref name="source"/></param>
		/// <param name="source">The source dense matrix to convert from</param>
		/// <param name="ld">The leading dimension of <paramref name="source"/></param>
		/// <param name="format">The destination <see cref="SparseMatrixFormat"/> of the target sparse matrix, must be atomic</param>
		/// <param name="threshold">Any element in <paramref name="source"/> less than or equals to this value will be regarded as 0</param>
		/// <param name="target">Output a created new <see cref="ISparseMatrix{T}"/> of the given properties</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="threshold"/> is less than 0 or <paramref name="format"/> is not atomic</exception>
		[AbstractApiMethod]
		public abstract bool MatrixDenseToSparse<T>(long m, long n, Storage<T> source, long ld, SparseMatrixFormat format, out SparseArrayWrapper<T> target, float threshold = 0) where T : unmanaged, INumber<T>;

		/// <summary>
		/// When implemented by a derived class, prune the given sparse matrix <paramref name="source"/> to a new one by filtering the values less than or equals to <paramref name="threshold"/>.
		/// </summary>
		/// <param name="source">The source sparse matrix to convert from</param>
		/// <param name="threshold">Any element in <paramref name="source"/> less than or equals to this value will be regarded as 0</param>
		/// <param name="target">Output a created new <see cref="ISparseMatrix{T}"/> of same properties as <paramref name="source"/> while the values (and the index arrays accordingly) are pruned by <paramref name="threshold"/>; or <paramref name="source"/> it self if no conversion is necessary</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="threshold"/> is less than 0</exception>
		[AbstractApiMethod]
		public abstract bool MatrixSparsePrune<T>(ISparseMatrix<T> source, float threshold, out SparseArrayWrapper<T> target) where T : unmanaged, INumber<T>;

		/// <summary>
		/// When implemented by a derived class, convert the format of the given sparse matrix <paramref name="source"/> to a new one which fits <paramref name="format"/>.
		/// </summary>
		/// <param name="source">The source sparse matrix to convert from</param>
		/// <param name="format">The target <see cref="SparseMatrixFormat"/>, can be anatomic</param>
		/// <param name="target">Output a created new <see cref="ISparseMatrix{T}"/> of desired <paramref name="format"/> while representing the same matrix as <paramref name="source"/>; or <paramref name="source"/> it self if no conversion is necessary</param>
		/// <param name="otherInfo">The target sparse matrix's <see cref="IOtherInfo"/>, default null means letting the internal implementation determine</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> is null or invalid</exception>
		[AbstractApiMethod]
		public abstract bool MatrixSparseFormatConvert<T>(ISparseMatrix<T> source, SparseMatrixFormat format, out SparseArrayWrapper<T> target, IOtherInfo? otherInfo = null) where T : unmanaged, INumber<T>;

		/// <summary>
		/// When implemented by a derived class, reshape the given sparse matrix <paramref name="source"/> to a new one with <paramref name="newNRows"/>.
		/// </summary>
		/// <param name="source">The source sparse matrix to convert from</param>
		/// <param name="newNRows">The target sparse matrix's number of rows</param>
		/// <param name="target">Output a created new <see cref="ISparseMatrix{T}"/> of desired <paramref name="newNRows"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="newNRows"/> is out of range</exception>
		[AbstractApiMethod]
		public abstract bool MatrixSparseReshape<T>(ISparseMatrix<T> source, long newNRows, out SparseArrayWrapper<T> target) where T : unmanaged, INumber<T>;
		#endregion

		#region index only
		/// <summary>
		/// When implemented by a derived class, find the maximum value of the given <b>sorted</b> integer-typed <paramref name="array"/>.
		/// </summary>
		/// <typeparam name="TInd">Any integral-typed unmanaged number as the index type</typeparam>
		/// <param name="array">The storage of the integer-typed array</param>
		/// <param name="max">Output the maximum value</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="array"/> is null or invalid</exception>
		/// <exception cref="TypeMismatchException">If <typeparamref name="TInd"/> is not an integral type</exception>
		[AbstractApiMethod]
		public abstract bool IndexMax<TInd>(Storage<TInd> array, out TInd max) where TInd : unmanaged;

		/// <summary>
		/// When implemented by a derived class, find the minimum value of the given <b>sorted</b> integer-typed <paramref name="array"/>.
		/// </summary>
		/// <typeparam name="TInd">Any integral-typed unmanaged number as the index type</typeparam>
		/// <param name="array">The storage of the integer-typed array</param>
		/// <param name="min">Output the minimum value</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="array"/> is null or invalid</exception>
		/// <exception cref="TypeMismatchException">If <typeparamref name="TInd"/> is not an integral type</exception>
		[AbstractApiMethod]
		public abstract bool IndexMin<TInd>(Storage<TInd> array, out TInd min) where TInd : unmanaged;

		/// <summary>
		/// When implemented by a derived class, find the zero-based index of the target <paramref name="value"/> in the given <b>sorted</b> integer-typed <paramref name="array"/>.
		/// </summary>
		/// <typeparam name="TInd">Any integral-typed unmanaged number as the index type</typeparam>
		/// <param name="sorted">Whether <paramref name="array"/> is sorted or not</param>
		/// <param name="array">The storage of the integer-typed array</param>
		/// <param name="value">The target value to find</param>
		/// <param name="find">Output the zero-based index of the target <paramref name="value"/> in <paramref name="array"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="array"/> is null or invalid</exception>
		/// <exception cref="TypeMismatchException">If <typeparamref name="TInd"/> is not an integral type</exception>
		[AbstractApiMethod]
		public abstract bool IndexFind<TInd>(bool sorted, Storage<TInd> array, TInd value, out long find) where TInd : unmanaged;

		/// <summary>
		/// When implemented by a derived class, find the zero-based index of the target <paramref name="value"/> as a (inclusive) lower / (exclusive) upper bound in the given <b>sorted</b> integer-typed <paramref name="array"/>.
		/// </summary>
		/// <typeparam name="TInd">Any integral-typed unmanaged number as the index type</typeparam>
		/// <param name="array">The storage of the <b>sorted</b> integer-typed array</param>
		/// <param name="value">The target value to find</param>
		/// <param name="lowerBound">Whether to find the first element in <paramref name="array"/> whose value is not less than <paramref name="value"/> or the first element in <paramref name="array"/> whose value is larger than <paramref name="value"/></param>
		/// <param name="index">Output the zero-based index of the target bound in <paramref name="array"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <remarks>If not found, <paramref name="index"/> shall be -1 if <paramref name="lowerBound"/> is true or <paramref name="array"/>.<see cref="Storage{T}.Length">Length</see> otherwise.</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="array"/> is null or invalid</exception>
		/// <exception cref="TypeMismatchException">If <typeparamref name="TInd"/> is not an integral type</exception>
		[AbstractApiMethod]
		public abstract bool IndexBound<TInd>(Storage<TInd> array, TInd value, bool lowerBound, out long index) where TInd : unmanaged;

		/// <summary>
		/// When implemented by a derived class, find the zero-based indices from <paramref name="start"/> to <paramref name="end"/> as (inclusive) lower / (exclusive) upper bounds in the given <b>sorted</b> integer-typed <paramref name="array"/> and store the result to <paramref name="target"/>.
		/// </summary>
		/// <typeparam name="TInd">Any integral-typed unmanaged number as the index type</typeparam>
		/// <typeparam name="TIndOut">Any integral-typed unmanaged number as the output index type</typeparam>
		/// <param name="array">The storage of the <b>sorted</b> integer-typed array</param>
		/// <param name="target">The storage of the result indices, must has length larger than <paramref name="end"/> - <paramref name="start"/></param>
		/// <param name="start">The inclusive start value to find</param>
		/// <param name="end">The inclusive end value to find</param>
		/// <param name="lowerBound">Whether to find the index of the first element in <paramref name="array"/> who is not less than the given value or the first who is larger than the given value</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <remarks>If not found, the corresponding index in <paramref name="target"/> shall be -1 if <paramref name="lowerBound"/> is true or <paramref name="array"/>.<see cref="Storage{T}.Length">Length</see> otherwise.</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="array"/> or <paramref name="target"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="target"/>'s length is too short or <paramref name="end"/> is less than <paramref name="start"/></exception>
		/// <exception cref="TypeMismatchException">If <typeparamref name="TInd"/> or <typeparamref name="TIndOut"/> is not an integral type</exception>
		[AbstractApiMethod]
		public abstract bool IndexGetAllBounds<TInd, TIndOut>(Storage<TInd> array, Storage<TIndOut> target, TInd start, TInd end, bool lowerBound)
			where TInd : unmanaged
			where TIndOut : unmanaged;

		/// <summary>
		/// When implemented by a derived class, reverse the operation of <see cref="IndexGetAllBounds_"/> to get the sorted <paramref name="target"/> array from the given <paramref name="bounds"/>.
		/// </summary>
		/// <typeparam name="TInd">Any integral-typed unmanaged number as the bound index type</typeparam>
		/// <typeparam name="TIndOut">Any integral-typed unmanaged number as the output index type</typeparam>
		/// <param name="bounds">The storage of the bound index array, usually generated from <see cref="IndexGetAllBounds_"/></param>
		/// <param name="target">The storage of the result indices, must has length ≥ the last element in <paramref name="bounds"/></param>
		/// <param name="start">The start value to fill in <paramref name="target"/></param>
		/// <param name="lowerBound">Whether to fill the <paramref name="target"/> with <paramref name="bounds"/> regarded as lower bounds or upper bounds</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="bounds"/> or <paramref name="target"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="target"/>'s length is too short</exception>
		/// <exception cref="TypeMismatchException">If <typeparamref name="TInd"/> or <typeparamref name="TIndOut"/> is not an integral type</exception>
		[AbstractApiMethod]
		public abstract bool IndexGenerateFromBounds<TInd, TIndOut>(Storage<TInd> bounds, Storage<TIndOut> target, bool lowerBound, TIndOut start = default)
			where TInd : unmanaged
			where TIndOut : unmanaged;
		#endregion
	}
}