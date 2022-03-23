using System;

using Althea.Arrays;
using Althea.Helpers;
using Althea.Storage;

using Althea.SourceGenerator;


namespace Althea.LinearAlgebra.Sparse
{
	/// <summary>
	/// The abstract interface for runtime sparse linear algebra conversion API routines 
	/// </summary>
	[AbstractRuntimeApi]
	public interface IConversionAbstractApi : IAbstractRuntimeApi<IConversionAbstractApi>
	{
		#region vector
		/// <summary>
		/// When implemented by a derived class, set the <paramref name="x"/>'s values at certain <paramref name="positions"/> to the given <paramref name="value"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TInd">Any integral-typed unmanaged number as the index type</typeparam>
		/// <typeparam name="TS">The concrete storage type of data type <typeparamref name="T"/> that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TSInd">The concrete storage type of data type <typeparamref name="TInd"/> that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="x">The input vector as a <typeparamref name="TS"/> whose values will be set</param>
		/// <param name="positions">The given positions as a <typeparamref name="TSInd"/></param>
		/// <param name="value">The value to set</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="positions"/> is null or invalid</exception>
		[AbstractApiMethod]
		public abstract bool VectorSetValuesAt<T, TInd, TS, TSInd>(TS x, T value, TSInd positions) where T : unmanaged, INumber<T> where TInd : unmanaged, IBinaryInteger<TInd> where TS : class, IStorage<T, TS> where TSInd : class, IStorage<TInd, TSInd>;

		/// <summary>
		/// When implemented by a derived class, set the <paramref name="x"/>'s values at certain <paramref name="positions"/> to the given <paramref name="values"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TInd">Any integral-typed unmanaged number as the index type</typeparam>
		/// <typeparam name="TS1">The first concrete storage type of data type <typeparamref name="T"/> that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The first concrete storage type of data type <typeparamref name="T"/> that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TSInd">The concrete storage type of data type <typeparamref name="TInd"/> that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="x">The input vector as a <typeparamref name="TS1"/> whose values will be set</param>
		/// <param name="positions">The given positions as a <typeparamref name="TSInd"/></param>
		/// <param name="values">The values to set as a <typeparamref name="TS2"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="positions"/> is null or invalid</exception>
		[AbstractApiMethod]
		public abstract bool VectorSetValuesAt<T, TInd, TS1, TS2, TSInd>(TS1 x, TS2 values, TSInd positions) where T : unmanaged, INumber<T> where TInd : unmanaged, IBinaryInteger<TInd> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TSInd : class, IStorage<TInd, TSInd>;

		/// <summary>
		/// When implemented by a derived class, gather the <paramref name="x"/>'s values at certain <paramref name="positions"/> to the given <paramref name="values"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TInd">Any integral-typed unmanaged number as the index type</typeparam>
		/// <typeparam name="TS1">The first concrete storage type of data type <typeparamref name="T"/> that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The first concrete storage type of data type <typeparamref name="T"/> that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TSInd">The concrete storage type of data type <typeparamref name="TInd"/> that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="x">The input vector as a <typeparamref name="TS1"/> whose values will be gathered</param>
		/// <param name="positions">The given positions as a <typeparamref name="TSInd"/></param>
		/// <param name="values">The output vector as a <typeparamref name="TS2"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="positions"/> is null or invalid</exception>
		[AbstractApiMethod]
		public abstract bool VectorGatherValuesAt<T, TInd, TS1, TS2, TSInd>(TS1 x, TS2 values, TSInd positions) where T : unmanaged, INumber<T> where TInd : unmanaged, IBinaryInteger<TInd> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TSInd : class, IStorage<TInd, TSInd>;

		/// <summary>
		/// When implemented by a derived class, convert sparse vector <paramref name="x"/> to dense vector <paramref name="y"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS1">The storage type used by the value array(s) of sparse vector</typeparam>
		/// <typeparam name="TS2">The storage type used by the dense vector</typeparam>
		/// <typeparam name="TInd">Any unmanaged integer number as the index data type</typeparam>
		/// <typeparam name="TSInd">The storage type used by the index array(s)</typeparam>
		/// <param name="x">The sparse vector as a <see cref="SparseArrayWrapper{TVal, TInd, TSVal, TSInd}"/></param>
		/// <param name="y">Output the dense vector as a <typeparamref name="TS2"/>: if <paramref name="y"/> is invalid, a new one will be allocated and returned; otherwise, it will simply be overwritten</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		[AbstractApiMethod]
		public abstract bool VectorSparseToDense<T, TInd, TS1, TS2, TSInd>(in SparseArrayWrapper<T, TInd, TS1, TSInd> x, ref TS2 y) where T : unmanaged, INumber<T> where TInd : unmanaged, IBinaryInteger<TInd> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TSInd : class, IStorage<TInd, TSInd>;

		/// <summary>
		/// When implemented by a derived class, convert dense vector <paramref name="x"/> to sparse vector <paramref name="x"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS2">The storage type used by the value array(s) of sparse vector</typeparam>
		/// <typeparam name="TS1">The storage type used by the dense vector</typeparam>
		/// <typeparam name="TInd">Any unmanaged integer number as the index data type</typeparam>
		/// <typeparam name="TSInd">The storage type used by the index array(s)</typeparam>
		/// <param name="x">The dense vector as a <typeparamref name="TS1"/></param>
		/// <param name="y">Output the sparse vector as a <see cref="SparseArrayWrapper{TVal, TInd, TSVal, TSInd}"/> whose <see cref="SparseArrayWrapper{TVal, TInd, TSVal, TSInd}.DefaultValue"/> and <see cref="SparseArrayWrapper{TVal, TInd, TSVal, TSInd}.Format"/> is used: if <paramref name="y"/>'s storages are invalid, new ones will be allocated and returned; otherwise, they will simply be overwritten</param>
		/// <param name="strideX">The stride between consecutive elements between <paramref name="y"/></param>
		/// <param name="threshold">The threshold used to truncate <paramref name="x"/> to sparse array: the values with <c>abs(default) - <paramref name="threshold"/> ≤ abs(v) ≤ abs(default) + <paramref name="threshold"/></c> are truncated to default value</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="y"/> or <paramref name="y"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> is out of range or <paramref name="threshold"/> &lt; 0</exception>
		[AbstractApiMethod]
		public abstract bool VectorDenseToSparse<T, TInd, TS2, TS1, TSInd>(ref SparseArrayWrapper<T, TInd, TS2, TSInd> y, TS1 x, long strideX = 1, double threshold = 0) where T : unmanaged, INumber<T> where TInd : unmanaged, IBinaryInteger<TInd> where TS2 : class, IStorage<T, TS2> where TS1 : class, IStorage<T, TS1> where TSInd : class, IStorage<TInd, TSInd>;
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
		/// When implemented by a derived class, find the minimum and maximum values of the given integer-typed <paramref name="array"/>.
		/// </summary>
		/// <typeparam name="T">Any integral-typed unmanaged number as the index type</typeparam>
		/// <typeparam name="TS">The concrete storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="sorted">Whether <paramref name="array"/> is sorted or not</param>
		/// <param name="array">The storage of the integer-typed array</param>
		/// <param name="minmax">Output the minimum and maximum values</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="array"/> is null or invalid</exception>
		[AbstractApiMethod]
		public abstract bool IndexMinMax<T, TS>(TS array, bool sorted, out (T Min, T Max) minmax) where T : unmanaged, IBinaryInteger<T> where TS : class, IStorage<T, TS>;

		/// <summary>
		/// When implemented by a derived class, find the zero-based index of the target <paramref name="value"/> in the given <b>sorted</b> integer-typed <paramref name="array"/>.
		/// </summary>
		/// <typeparam name="T">Any integral-typed unmanaged number as the index type</typeparam>
		/// <typeparam name="TS">The concrete storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="sorted">Whether <paramref name="array"/> is sorted or not</param>
		/// <param name="array">The storage of the integer-typed array</param>
		/// <param name="value">The target value to find</param>
		/// <param name="find">Output the zero-based index of the target <paramref name="value"/> in <paramref name="array"/> if it is found; otherwise, output a negative number</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="array"/> is null or invalid</exception>
		[AbstractApiMethod]
		public abstract bool IndexFind<T, TS>(TS array, bool sorted, T value, out long find) where T : unmanaged, IBinaryInteger<T> where TS : class, IStorage<T, TS>;

		/// <summary>
		/// When implemented by a derived class, find the zero-based index of the target <paramref name="value"/> as a (inclusive) lower / (exclusive) upper bound in the given <b>sorted</b> integer-typed <paramref name="array"/>.
		/// </summary>
		/// <typeparam name="T">Any integral-typed unmanaged number as the index type</typeparam>
		/// <typeparam name="TS">The concrete storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="array">The storage of the <b>sorted</b> integer-typed array</param>
		/// <param name="value">The target value to find</param>
		/// <param name="lowerBound">Whether to find the first element in <paramref name="array"/> whose value is not less than <paramref name="value"/> or the first element in <paramref name="array"/> whose value is larger than <paramref name="value"/></param>
		/// <param name="index">Output the zero-based index of the target bound in <paramref name="array"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <remarks>If not found, <paramref name="index"/> shall be -1 if <paramref name="lowerBound"/> is true or <paramref name="array"/>.<see cref="IStorage{T, TSelf}.Length">Length</see> otherwise.</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="array"/> is null or invalid</exception>
		[AbstractApiMethod]
		public abstract bool IndexBound<T, TS>(TS array, T value, bool lowerBound, out long index) where T : unmanaged, IBinaryInteger<T> where TS : class, IStorage<T, TS>;

		/// <summary>
		/// When implemented by a derived class, find the zero-based indices from <paramref name="start"/> to <paramref name="end"/> as (inclusive) lower / (exclusive) upper bounds in the given <b>sorted</b> integer-typed <paramref name="array"/> and store the result to <paramref name="target"/>.
		/// </summary>
		/// <typeparam name="T">Any integral-typed unmanaged number as the index type</typeparam>
		/// <typeparam name="TS">The input concrete storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TOut">Any integral-typed unmanaged number as the output index type</typeparam>
		/// <typeparam name="TSOut">The output concrete storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="array">The storage of the <b>sorted</b> integer-typed array</param>
		/// <param name="target">The storage of the result indices, must has length larger than <paramref name="end"/> - <paramref name="start"/></param>
		/// <param name="start">The inclusive start value to find</param>
		/// <param name="end">The exclusive end value to find</param>
		/// <param name="lowerBound">Whether to find the index of the first element in <paramref name="array"/> who is not less than the given value or the first who is larger than the given value</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <remarks>If not found, the corresponding index in <paramref name="target"/> shall be -1 if <paramref name="lowerBound"/> is true or <paramref name="array"/>.<see cref="IStorage{T, TSelf}.Length">Length</see> otherwise.</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="array"/> or <paramref name="target"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="target"/>'s length is too short or <paramref name="end"/> is less than <paramref name="start"/></exception>
		[AbstractApiMethod]
		public abstract bool IndexGetAllBounds<T, TOut, TS, TSOut>(TS array, TSOut target, T start, T end, bool lowerBound) where T : unmanaged, IBinaryInteger<T> where TOut : unmanaged, IBinaryInteger<TOut> where TS : class, IStorage<T, TS> where TSOut : class, IStorage<T, TSOut>;

		/// <summary>
		/// When implemented by a derived class, reverse the operation of <see cref="IndexGetAllBounds"/> to get the sorted <paramref name="target"/> array from the given <paramref name="bounds"/>.
		/// </summary>
		/// <typeparam name="T">Any integral-typed unmanaged number as the bound index type</typeparam>
		/// <typeparam name="TS">The input concrete storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TOut">Any integral-typed unmanaged number as the output index type</typeparam>
		/// <typeparam name="TSOut">The output concrete storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="bounds">The storage of the bound index array, usually generated from <see cref="IndexGetAllBounds"/></param>
		/// <param name="target">The storage of the result indices, must has length ≥ the last element in <paramref name="bounds"/></param>
		/// <param name="start">The start value to fill in <paramref name="target"/></param>
		/// <param name="lowerBound">Whether to fill the <paramref name="target"/> with <paramref name="bounds"/> regarded as lower bounds or upper bounds</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="bounds"/> or <paramref name="target"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="target"/>'s length is too short</exception>
		[AbstractApiMethod]
		public abstract bool IndexGenerateFromBounds<T, TOut, TS, TSOut>(TS bounds, TSOut target, bool lowerBound, TOut start = default) where T : unmanaged, IBinaryInteger<T> where TOut : unmanaged, IBinaryInteger<TOut> where TS : class, IStorage<T, TS> where TSOut : class, IStorage<T, TSOut>;
		#endregion
	}
}