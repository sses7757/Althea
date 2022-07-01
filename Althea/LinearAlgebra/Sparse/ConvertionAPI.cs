using Althea.Array;
using Althea.SourceGenerator;
using Althea.Storage;


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
		public abstract bool VectorSetValuesAt<T, TInd, TS, TSInd>(TS x, T value, TSInd positions) where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd> where TS : class, IStorage<T, TS> where TSInd : class, IStorage<TInd, TSInd>;

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
		public abstract bool VectorSetValuesAt<T, TInd, TS1, TS2, TSInd>(TS1 x, TS2 values, TSInd positions) where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TSInd : class, IStorage<TInd, TSInd>;

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
		public abstract bool VectorGatherValuesAt<T, TInd, TS1, TS2, TSInd>(TS1 x, TS2 values, TSInd positions) where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TSInd : class, IStorage<TInd, TSInd>;

		/// <summary>
		/// When implemented by a derived class, convert sparse vector <paramref name="x"/> to dense vector <paramref name="y"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS1">The storage type used by the value array(s) of sparse vector</typeparam>
		/// <typeparam name="TS2">The storage type used by the dense vector</typeparam>
		/// <typeparam name="TInd">Any unmanaged integer number as the index data type</typeparam>
		/// <typeparam name="TSInd">The storage type used by the index array(s)</typeparam>
		/// <param name="x">The sparse vector as a <see cref="ISparseArray{T, TInd, TS, TSInd}"/></param>
		/// <param name="y">The dense vector as a <typeparamref name="TS2"/> to be overwritten</param>
		/// <param name="strideY">The stride between consecutive elements in <paramref name="y"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		[AbstractApiMethod]
		public abstract bool VectorSparseToDense<T, TInd, TS1, TS2, TSInd>(ISparseArray<T, TInd, TS1, TSInd> x, TS2 y, long strideY) where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TSInd : class, IStorage<TInd, TSInd>;

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
		public abstract bool VectorDenseToSparse<T, TInd, TS1, TS2, TSInd>(TS1 x, long strideX, ref SparseArrayWrapper<T, TInd, TS2, TSInd> y, double threshold = 0) where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd> where TS2 : class, IStorage<T, TS2> where TS1 : class, IStorage<T, TS1> where TSInd : class, IStorage<TInd, TSInd>;
		#endregion

		#region vector and matrix
		/// <summary>
		/// When implemented by a derived class, convert the given sparse <paramref name="vector"/> to a sparse matrix.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TInd1">Any integral-typed unmanaged number as the input index type</typeparam>
		/// <typeparam name="TInd2">Any integral-typed unmanaged number as the output index type</typeparam>
		/// <typeparam name="TS1">The concrete storage type for input value array</typeparam>
		/// <typeparam name="TS2">The concrete storage type for output value array</typeparam>
		/// <typeparam name="TSInd1">The concrete storage type of input index array</typeparam>
		/// <typeparam name="TSInd2">The concrete storage type of output index array</typeparam>
		/// <param name="vector">The input sparse vector</param>
		/// <param name="target">The output sparse matrix with desired format as <see cref="SparseArrayWrapper{TVal, TInd, TSVal, TSInd}.Format"/> and desired size as <see cref="SparseArrayWrapper{TVal, TInd, TSVal, TSInd}.Size"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="vector"/> is null or invalid</exception>
		[AbstractApiMethod]
		public abstract bool SparseVectorToMatrix<T, TInd1, TInd2, TS1, TS2, TSInd1, TSInd2>(ISparseArray<T, TInd1, TS1, TSInd1> vector, ref SparseArrayWrapper<T, TInd2, TS2, TSInd2> target) where T : unmanaged, IBaseNumber<T> where TInd1 : unmanaged, IBinaryInt<TInd1> where TS1 : class, IStorage<T, TS1> where TSInd1 : class, IStorage<TInd1, TSInd1> where TInd2 : unmanaged, IBinaryInt<TInd2> where TS2 : class, IStorage<T, TS2> where TSInd2 : class, IStorage<TInd2, TSInd2>;

		/// <summary>
		/// When implemented by a derived class, convert the given sparse <paramref name="matrix"/> to a sparse vector.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TInd1">Any integral-typed unmanaged number as the input index type</typeparam>
		/// <typeparam name="TInd2">Any integral-typed unmanaged number as the output index type</typeparam>
		/// <typeparam name="TS1">The concrete storage type for input value array</typeparam>
		/// <typeparam name="TS2">The concrete storage type for output value array</typeparam>
		/// <typeparam name="TSInd1">The concrete storage type of input index array</typeparam>
		/// <typeparam name="TSInd2">The concrete storage type of output index array</typeparam>
		/// <param name="matrix">The input sparse matrix</param>
		/// <param name="target">The output sparse vector with desired format as <see cref="SparseArrayWrapper{TVal, TInd, TSVal, TSInd}.Format"/> and desired size as <see cref="SparseArrayWrapper{TVal, TInd, TSVal, TSInd}.Size"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="matrix"/> is null or invalid</exception>
		[AbstractApiMethod]
		public abstract bool SparseMatrixToVector<T, TInd1, TInd2, TS1, TS2, TSInd1, TSInd2>(ISparseArray<T, TInd1, TS1, TSInd1> matrix, ref SparseArrayWrapper<T, TInd2, TS2, TSInd2> target) where T : unmanaged, IBaseNumber<T> where TInd1 : unmanaged, IBinaryInt<TInd1> where TS1 : class, IStorage<T, TS1> where TSInd1 : class, IStorage<TInd1, TSInd1> where TInd2 : unmanaged, IBinaryInt<TInd2> where TS2 : class, IStorage<T, TS2> where TSInd2 : class, IStorage<TInd2, TSInd2>;
		#endregion

		#region matrix
		/// <summary>
		/// When implemented by a derived class, slice the given sparse matrix <paramref name="source"/> with the given <paramref name="slice"/> and write the result to <paramref name="sub"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TInd1">Any integral-typed unmanaged number as the input index type</typeparam>
		/// <typeparam name="TInd2">Any integral-typed unmanaged number as the output index type</typeparam>
		/// <typeparam name="TS1">The concrete storage type for input value array</typeparam>
		/// <typeparam name="TS2">The concrete storage type for output value array</typeparam>
		/// <typeparam name="TSInd1">The concrete storage type of input index array</typeparam>
		/// <typeparam name="TSInd2">The concrete storage type of output index array</typeparam>
		/// <param name="source">The input sparse matrix to be sliced</param>
		/// <param name="slice">The slicing parameters as a <see cref="MatrixSliceWrapper"/></param>
		/// <param name="sub">Output the sliced sub matrix of <paramref name="source"/>. If the storages inside is not null, they will be overwritten.</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="slice"/> is out of range</exception>
		/// <exception cref="ArgumentException">If the storage(s) in <paramref name="slice"/> cannot be overwritten</exception>
		[AbstractApiMethod]
		public abstract bool SparseMatrixGetSlice<T, TInd1, TInd2, TS1, TS2, TSInd1, TSInd2>(ISparseArray<T, TInd1, TS1, TSInd1> source, MatrixSliceWrapper slice, ref SparseArrayWrapper<T, TInd2, TS2, TSInd2> sub) where T : unmanaged, IBaseNumber<T> where TInd1 : unmanaged, IBinaryInt<TInd1> where TS1 : class, IStorage<T, TS1> where TSInd1 : class, IStorage<TInd1, TSInd1> where TInd2 : unmanaged, IBinaryInt<TInd2> where TS2 : class, IStorage<T, TS2> where TSInd2 : class, IStorage<TInd2, TSInd2>;

		/// <summary>
		/// When implemented by a derived class, set the given sparse <paramref name="matrix"/>'s <paramref name="slice"/> to the <paramref name="sub"/> matrix.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TInd1">Any integral-typed unmanaged number as the input index type</typeparam>
		/// <typeparam name="TInd2">Any integral-typed unmanaged number as the output index type</typeparam>
		/// <typeparam name="TS1">The concrete storage type for input value array</typeparam>
		/// <typeparam name="TS2">The concrete storage type for output value array</typeparam>
		/// <typeparam name="TSInd1">The concrete storage type of input index array</typeparam>
		/// <typeparam name="TSInd2">The concrete storage type of output index array</typeparam>
		/// <param name="matrix">The sparse matrix whose <paramref name="slice"/> will be overwritten by <paramref name="sub"/> matrix</param>
		/// <param name="slice">The slicing parameters as a <see cref="MatrixSliceWrapper"/></param>
		/// <param name="sub">The sub sparse matrix to overwrite the sliced <paramref name="matrix"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="matrix"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="slice"/> is out of range</exception>
		/// <exception cref="ArgumentException">If <paramref name="sub"/> cannot overwrite the sliced <paramref name="matrix"/></exception>
		[AbstractApiMethod]
		public abstract bool SparseMatrixSetSlice<T, TInd1, TInd2, TS1, TS2, TSInd1, TSInd2>(ISparseArray<T, TInd2, TS2, TSInd2> matrix, MatrixSliceWrapper slice, ISparseArray<T, TInd1, TS1, TSInd1> sub) where T : unmanaged, IBaseNumber<T> where TInd1 : unmanaged, IBinaryInt<TInd1> where TS1 : class, IStorage<T, TS1> where TSInd1 : class, IStorage<TInd1, TSInd1> where TInd2 : unmanaged, IBinaryInt<TInd2> where TS2 : class, IStorage<T, TS2> where TSInd2 : class, IStorage<TInd2, TSInd2>;

		/// <summary>
		/// When implemented by a derived class, convert the given sparse matrix <paramref name="source"/> to a dense matrix <paramref name="destination"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TInd">Any integral-typed unmanaged number as the input index type</typeparam>
		/// <typeparam name="TS1">The concrete storage type for input value array</typeparam>
		/// <typeparam name="TS2">The concrete storage type for output array</typeparam>
		/// <typeparam name="TSInd">The concrete storage type of input index array</typeparam>
		/// <param name="source">The source sparse matrix to convert from</param>
		/// <param name="destination">The storage of the destination dense matrix of the same size as <paramref name="source"/></param>
		/// <param name="ld">The leading dimension of <paramref name="destination"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="destination"/> is null or invalid</exception>
		[AbstractApiMethod]
		public abstract bool MatrixSparseToDense<T, TInd, TS1, TS2, TSInd>(ISparseArray<T, TInd, TS1, TSInd> source, TS2 destination, long ld) where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd> where TS1 : class, IStorage<T, TS1> where TSInd : class, IStorage<TInd, TSInd> where TS2 : class, IStorage<T, TS2>;

		/// <summary>
		/// When implemented by a derived class, convert the given dense matrix <paramref name="source"/> to a sparse matrix with <paramref name="threshold"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TInd">Any integral-typed unmanaged number as the output index type</typeparam>
		/// <typeparam name="TS1">The concrete storage type for input value array</typeparam>
		/// <typeparam name="TS2">The concrete storage type for output array</typeparam>
		/// <typeparam name="TSInd">The concrete storage type of output index array</typeparam>
		/// <param name="source">The source dense matrix to convert from, its size is in <see cref="SparseArrayWrapper{TVal, TInd, TSVal, TSInd}.Size"/></param>
		/// <param name="ld">The leading dimension of <paramref name="source"/></param>
		/// <param name="threshold">Any element in <paramref name="source"/> less than or equals to this value will be regarded as 0</param>
		/// <param name="target">Output a created new sparse matrix with desired format as <see cref="SparseArrayWrapper{TVal, TInd, TSVal, TSInd}.Format"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="threshold"/>&lt; 0</exception>
		[AbstractApiMethod]
		public abstract bool MatrixDenseToSparse<T, TInd, TS1, TS2, TSInd>(TS1 source, long ld, ref SparseArrayWrapper<T, TInd, TS2, TSInd> target, double threshold = 0) where T : unmanaged, IBaseNumber<T> where TInd : unmanaged, IBinaryInt<TInd> where TS1 : class, IStorage<T, TS1> where TSInd : class, IStorage<TInd, TSInd> where TS2 : class, IStorage<T, TS2>;

		/// <summary>
		/// When implemented by a derived class, prune the given sparse matrix <paramref name="source"/> to a new one by filtering the values less than or equals to <paramref name="threshold"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TInd1">Any integral-typed unmanaged number as the input index type</typeparam>
		/// <typeparam name="TInd2">Any integral-typed unmanaged number as the output index type</typeparam>
		/// <typeparam name="TS1">The concrete storage type for input value array</typeparam>
		/// <typeparam name="TS2">The concrete storage type for output value array</typeparam>
		/// <typeparam name="TSInd1">The concrete storage type of input index array</typeparam>
		/// <typeparam name="TSInd2">The concrete storage type of output index array</typeparam>
		/// <param name="source">The source sparse matrix to convert from</param>
		/// <param name="threshold">Any element in <paramref name="source"/> less than or equals to this value will be regarded as 0</param>
		/// <param name="target">Output a created new sparse matrix of same properties as <paramref name="source"/> while the values (and the index arrays accordingly) are pruned by <paramref name="threshold"/>; or a reference to <paramref name="source"/> if no conversion is necessary</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="threshold"/> &lt; 0</exception>
		[AbstractApiMethod]
		public abstract bool MatrixSparsePrune<T, TInd1, TInd2, TS1, TS2, TSInd1, TSInd2>(ISparseArray<T, TInd1, TS1, TSInd1> source, double threshold, ref SparseArrayWrapper<T, TInd2, TS2, TSInd2> target) where T : unmanaged, IBaseNumber<T> where TInd1 : unmanaged, IBinaryInt<TInd1> where TS1 : class, IStorage<T, TS1> where TSInd1 : class, IStorage<TInd1, TSInd1> where TInd2 : unmanaged, IBinaryInt<TInd2> where TS2 : class, IStorage<T, TS2> where TSInd2 : class, IStorage<TInd2, TSInd2>;

		/// <summary>
		/// When implemented by a derived class, convert the format of the given sparse matrix <paramref name="source"/> to a new one which fits the <see cref="SparseArrayWrapper{TVal, TInd, TSVal, TSInd}.Format"/> in <paramref name="target"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TInd1">Any integral-typed unmanaged number as the input index type</typeparam>
		/// <typeparam name="TInd2">Any integral-typed unmanaged number as the output index type</typeparam>
		/// <typeparam name="TS1">The concrete storage type for input value array</typeparam>
		/// <typeparam name="TS2">The concrete storage type for output value array</typeparam>
		/// <typeparam name="TSInd1">The concrete storage type of input index array</typeparam>
		/// <typeparam name="TSInd2">The concrete storage type of output index array</typeparam>
		/// <param name="source">The source sparse matrix to convert from</param>
		/// <param name="target">Output a created new sparse matrix with desired format; or a reference to <paramref name="source"/> if no conversion is necessary</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> is null or invalid</exception>
		[AbstractApiMethod]
		public abstract bool MatrixSparseFormatConvert<T, TInd1, TInd2, TS1, TS2, TSInd1, TSInd2>(ISparseArray<T, TInd1, TS1, TSInd1> source, ref SparseArrayWrapper<T, TInd2, TS2, TSInd2> target) where T : unmanaged, IBaseNumber<T> where TInd1 : unmanaged, IBinaryInt<TInd1> where TS1 : class, IStorage<T, TS1> where TSInd1 : class, IStorage<TInd1, TSInd1> where TInd2 : unmanaged, IBinaryInt<TInd2> where TS2 : class, IStorage<T, TS2> where TSInd2 : class, IStorage<TInd2, TSInd2>;

		/// <summary>
		/// When implemented by a derived class, reshape the given sparse matrix <paramref name="source"/> to a new one with new size as <see cref="SparseArrayWrapper{TVal, TInd, TSVal, TSInd}.Size"/> in <paramref name="target"/>.
		/// </summary>
		/// <param name="source">The source sparse matrix to convert from</param>
		/// <param name="target">Output a created new sparse matrix with desired size; or a reference to <paramref name="source"/> if no conversion is necessary</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If the size in <paramref name="target"/> is not a reshape of <paramref name="source"/></exception>
		[AbstractApiMethod]
		public abstract bool MatrixSparseReshape<T, TInd1, TInd2, TS1, TS2, TSInd1, TSInd2>(ISparseArray<T, TInd1, TS1, TSInd1> source, ref SparseArrayWrapper<T, TInd2, TS2, TSInd2> target) where T : unmanaged, IBaseNumber<T> where TInd1 : unmanaged, IBinaryInt<TInd1> where TS1 : class, IStorage<T, TS1> where TSInd1 : class, IStorage<TInd1, TSInd1> where TInd2 : unmanaged, IBinaryInt<TInd2> where TS2 : class, IStorage<T, TS2> where TSInd2 : class, IStorage<TInd2, TSInd2>;
		#endregion

		#region index only
		/// <summary>
		/// When implemented by a derived class, sort the values of the given <paramref name="array"/>.
		/// </summary>
		/// <typeparam name="T">Any integral-typed unmanaged number as the index type</typeparam>
		/// <typeparam name="TS">The concrete storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="array">The storage of the array to be sorted</param>
		/// <param name="stride">The stride between consecutive elements in <paramref name="array"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="array"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="stride"/> ≤ 0</exception>
		[AbstractApiMethod]
		public abstract bool Sort<T, TS>(TS array, long stride) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>;

		/// <summary>
		/// When implemented by a derived class, sort the elements of the given integer-typed <paramref name="keys"/> with <paramref name="values"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the key type</typeparam>
		/// <typeparam name="TS">The concrete storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TOther">Any unmanaged number as the value type</typeparam>
		/// <typeparam name="TS2">The concrete storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="keys">The storage of the key array to be sorted</param>
		/// <param name="values">The storage of the value array to be sorted</param>
		/// <param name="strideKeys">The stride between consecutive elements in <paramref name="keys"/></param>
		/// <param name="strideValues">The stride between consecutive elements in <paramref name="values"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="keys"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideKeys"/> or <paramref name="strideValues"/> ≤ 0</exception>
		[AbstractApiMethod]
		public abstract bool Sort<T, TOther, TS, TS2>(TS keys, long strideKeys, TS2 values, long strideValues) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS> where TOther : unmanaged, IBaseNumber<TOther> where TS2 : class, IStorage<TOther, TS2>;

		/// <summary>
		/// When implemented by a derived class, find the minimum and maximum values of the given <paramref name="array"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS">The concrete storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="array">The storage of the integer-typed array</param>
		/// <param name="stride">The stride between consecutive elements in <paramref name="array"/></param>
		/// <param name="minmax">Output the minimum and maximum values</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="array"/> is null or invalid</exception>
		[AbstractApiMethod]
		public abstract bool MinMax<T, TS>(TS array, long stride, out (T Min, T Max) minmax) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>;

		/// <summary>
		/// When implemented by a derived class, find the zero-based index of the target <paramref name="value"/> in the given <paramref name="array"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the index type</typeparam>
		/// <typeparam name="TS">The concrete storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="sorted">Whether <paramref name="array"/> is sorted or not</param>
		/// <param name="stride">The stride between consecutive elements in <paramref name="array"/></param>
		/// <param name="array">The storage of the integer-typed array</param>
		/// <param name="value">The target value to find</param>
		/// <param name="find">Output the zero-based index of the target <paramref name="value"/> in <paramref name="array"/> if it is found; otherwise, output a negative number</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="array"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="stride"/> ≤ 0</exception>
		[AbstractApiMethod]
		public abstract bool IndexOf<T, TS>(TS array, long stride, bool sorted, T value, out long find) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>;

		/// <summary>
		/// When implemented by a derived class, find the zero-based index of the target <paramref name="value"/> as a (inclusive) lower / (exclusive) upper bound in the given <b>sorted</b> <paramref name="array"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the index type</typeparam>
		/// <typeparam name="TS">The concrete storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="array">The storage of the <b>sorted</b> integer-typed array</param>
		/// <param name="stride">The stride between consecutive elements in <paramref name="array"/></param>
		/// <param name="value">The target value to find</param>
		/// <param name="lowerBound">Whether to find the first element in <paramref name="array"/> whose value is not less than <paramref name="value"/> or the first element in <paramref name="array"/> whose value is larger than <paramref name="value"/></param>
		/// <param name="index">Output the zero-based index of the target bound in <paramref name="array"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="array"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="stride"/> ≤ 0</exception>
		[AbstractApiMethod]
		public abstract bool IndexBound<T, TS>(TS array, long stride, T value, bool lowerBound, out long index) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>;

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
		public abstract bool IndexGetAllBounds<T, TOut, TS, TSOut>(TS array, TSOut target, T start, T end, bool lowerBound) where T : unmanaged, IBinaryInt<T> where TOut : unmanaged, IBinaryInt<TOut> where TS : class, IStorage<T, TS> where TSOut : class, IStorage<TOut, TSOut>;

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
		public abstract bool IndexGenerateFromBounds<T, TOut, TS, TSOut>(TS bounds, TSOut target, bool lowerBound, TOut start = default) where T : unmanaged, IBinaryInt<T> where TOut : unmanaged, IBinaryInt<TOut> where TS : class, IStorage<T, TS> where TSOut : class, IStorage<TOut, TSOut>;
		#endregion
	}
}