using System.Linq.Expressions;

using Althea.Array;
using Althea.LinearAlgebra;
using Althea.SourceGenerator;
using Althea.Storage;


namespace Althea.TensorAlgebra.Dense
{
	/// <summary>
	/// The abstract interface for extended runtime dense tensor algebra API routines 
	/// </summary>
	[AbstractRuntimeApi]
	public interface IExtendAbstractApi : IAbstractRuntimeApi<IExtendAbstractApi>
	{
		/// <summary>
		/// When implemented by a derived class, perform binary operation <paramref name="op"/> to each element in <paramref name="tensor"/> with <paramref name="value"/> and write the result in-place.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS">The storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="tensor">The dense tensor as a <see cref="DenseArrayWrapper{T, TS}"/> to be in-place modified</param>
		/// <param name="value">The value as the second input in <paramref name="op"/> of type <typeparamref name="T"/>.</param>
		/// <param name="op">The <see cref="BinaryScalarOperation"/> to use</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="tensor"/> is invalid</exception>
		[AbstractApiMethod]
		public abstract bool PointwiseBinaryScalar<T, TS>(BinaryScalarOperation op, DenseArrayWrapper<T, TS> tensor, T value) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>;

		/// <summary>
		/// When implemented by a derived class, fully reduce <paramref name="tensor"/> according to <paramref name="op"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS">The storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="op">The <see cref="ReduceOperation"/> to use</param>
		/// <param name="tensor">The dense tensor as a <see cref="DenseArrayWrapper{T, TS}"/> to reduce</param>
		/// <param name="result">Output the reduction result</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="tensor"/> is invalid</exception>
		[AbstractApiMethod]
		public abstract bool FullReduce<T, TS>(ReduceOperation op, DenseArrayWrapper<T, TS> tensor, out T result) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>;

		/// <summary>
		/// When implemented by a derived class, reduce <paramref name="tensor"/>'s <paramref name="axis"/> dimension according to <paramref name="op"/> and overwrite <paramref name="result"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS">The storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="op">The <see cref="ReduceOperation"/> to use</param>
		/// <param name="axis">The dimension of <paramref name="tensor"/> to be reduced</param>
		/// <param name="tensor">The dense tensor as a <see cref="DenseArrayWrapper{T, TS}"/> to reduce</param>
		/// <param name="result">Output the reduction result <see cref="DenseArrayWrapper{T, TS}"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="tensor"/> is invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="result"/>'s size is not <paramref name="tensor"/>'s without <paramref name="axis"/></exception>
		[AbstractApiMethod]
		public abstract bool AxisReduce<T, TS>(ReduceOperation op, int axis, DenseArrayWrapper<T, TS> tensor, DenseArrayWrapper<T, TS> result) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>;

		/// <summary>
		/// When implemented by a derived class, partial scan <paramref name="tensor"/>'s <paramref name="axis"/> dimension according to <paramref name="op"/> and overwrite <paramref name="result"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS">The storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="op">The <see cref="ReduceOperation"/> to use</param>
		/// <param name="inclusive">Whether the first elements are the ones from <paramref name="tensor"/> or identity of <paramref name="op"/></param>
		/// <param name="axis">The dimension of <paramref name="tensor"/> to be scanned</param>
		/// <param name="tensor">The dense tensor as a <see cref="DenseArrayWrapper{T, TS}"/> to scan</param>
		/// <param name="result">Output the scanning result <see cref="DenseArrayWrapper{T, TS}"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="tensor"/> is invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="result"/>'s size is not <paramref name="tensor"/>'s without <paramref name="axis"/></exception>
		[AbstractApiMethod]
		public abstract bool AxisScan<T, TS>(ReduceOperation op, bool inclusive, int axis, DenseArrayWrapper<T, TS> tensor, DenseArrayWrapper<T, TS> result) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>;

		/// <summary>
		/// When implemented by a derived class, fully reduce <paramref name="tensor"/> according to <paramref name="op"/> and return the index of reduction result.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS">The storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="op">The <see cref="ReduceOperation"/> to use</param>
		/// <param name="tensor">The dense tensor as a <see cref="DenseArrayWrapper{T, TS}"/> to reduce</param>
		/// <param name="index">Output the reduction result's index compared to <paramref name="tensor"/>'s <see cref="DenseArrayWrapper{T, TS}.ValueStorage"/> in <typeparamref name="T"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="tensor"/> is invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="op"/> is not aggregation operation like <see cref="ReduceOperation.Maximum"/></exception>
		[AbstractApiMethod]
		public abstract bool ArgFullReduce<T, TS>(ReduceOperation op, DenseArrayWrapper<T, TS> tensor, out long index) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>;

		/// <summary>
		/// When implemented by a derived class, check if all elements in <paramref name="A"/> and <paramref name="B"/> are equal: <c><paramref name="A"/>[i] == <paramref name="B"/>[i]</c> for all <c>i</c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS1">The first actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The second actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="A">The first tensor to be checked</param>
		/// <param name="B">The second tensor to be checked</param>
		/// <param name="equals">Output <see cref="bool"/> indicating whether all elements in <paramref name="A"/> and <paramref name="B"/> are equal</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="B"/> is null or invalid</exception>
		[AbstractApiMethod]
		public abstract bool PointwiseEqual<T, TS1, TS2>(DenseArrayWrapper<T, TS1> A, DenseArrayWrapper<T, TS2> B, out bool equals) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

		/// <summary>
		/// When implemented by a derived class, cast the given tensor from type <typeparamref name="TIn"/> to type <typeparamref name="TOut"/>.
		/// </summary>
		/// <typeparam name="TIn">Any unmanaged number as the input data type</typeparam>
		/// <typeparam name="TOut">Any unmanaged number as the output data type</typeparam>
		/// <typeparam name="TSIn">The input actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TSOut">The output actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="source">The source tensor</param>
		/// <param name="destination">The destination tensor</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="destination"/> is null or invalid</exception>
		[AbstractApiMethod]
		public abstract bool PointwiseCast<TIn, TOut, TSIn, TSOut>(DenseArrayWrapper<TIn, TSIn> source, DenseArrayWrapper<TOut, TSOut> destination) where TIn : unmanaged, IBaseNumber<TIn> where TOut : unmanaged, IBaseNumber<TOut> where TSIn : class, IStorage<TIn, TSIn> where TSOut : class, IStorage<TOut, TSOut>;
	}


	/// <summary>
	/// The abstract interface for extended runtime dense tensor algebra API routines which involves runtime parsing <see cref="Expression"/>s.
	/// </summary>
	/// <remarks>Since <see cref="Expression"/> is a class (therefore introduces GC) and must be parsed before calculation, it is not recommended to use this API for non-critical situations.</remarks>
	[AbstractRuntimeApi]
	public interface IDynamicExtendAbstractApi : IAbstractRuntimeApi<IDynamicExtendAbstractApi>
	{
		/// <summary>
		/// When implemented by a derived class, compute <c><paramref name="outputs"/>[i] = <paramref name="op"/>(<paramref name="inputs"/>[i], <paramref name="scalars"/>)</c>.
		/// </summary>
		/// <typeparam name="TIn">Any unmanaged number struct as the input data type</typeparam>
		/// <typeparam name="TOut">Any unmanaged number struct as the output data type</typeparam>
		/// <typeparam name="TScalar">Any unmanaged number struct as the scalar data type</typeparam>
		/// <typeparam name="TSIn">The input actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TSOut">The output actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="op">The <see cref="Expression{TDelegate}"/> of <see cref="Func{TIn, TScalar, TOut}"/> to apply to each elements of <paramref name="inputs"/> and <paramref name="scalars"/></param>
		/// <param name="inputs">The input tensors to apply <paramref name="op"/></param>
		/// <param name="scalars">The scalar inputs</param>
		/// <param name="outputs">The output tensors to store the results</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If any of the tensors is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If any of the sizes or strides ≤ 0</exception>
		/// <exception cref="ArgumentException">If <paramref name="op"/> is not a point-wise operation whose input matches (<paramref name="inputs"/>, <paramref name="scalars"/>) and output matches <paramref name="outputs"/></exception>
		[AbstractApiMethod]
		public abstract bool PointwiseOperate<TIn, TOut, TScalar, TSIn, TSOut>(Expression<Func<TIn[], TScalar[], TOut[]>> op, in DenseArrayWrapper<TIn, TSIn> inputs, ReadOnlySpan<TScalar> scalars, in DenseArrayWrapper<TOut, TSOut> outputs) where TIn : unmanaged, IBaseNumber<TIn> where TOut : unmanaged, IBaseNumber<TOut> where TScalar : unmanaged, IBaseNumber<TScalar> where TSIn : class, IStorage<TIn, TSIn> where TSOut : class, IStorage<TOut, TSOut>;

		/// <summary>
		/// When implemented by a derived class, compute reduction: <c><paramref name="results"/> = <paramref name="op"/>(<paramref name="inputs"/>[i], <paramref name="results"/>)</c> for all <c>i</c>.
		/// </summary>
		/// <typeparam name="TIn">Any unmanaged number struct as the input data type</typeparam>
		/// <typeparam name="TOut">Any unmanaged number struct as the output data type</typeparam>
		/// <typeparam name="TS">The actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="op">The <see cref="Expression{TDelegate}"/> of <see cref="Func{TIn, TReuceOut, TOut}"/> to apply to each elements of <paramref name="inputs"/> and <paramref name="results"/></param>
		/// <param name="inputs">The input tensors to apply <paramref name="op"/></param>
		/// <param name="results">Output the reduction result of <paramref name="inputs"/> under <paramref name="op"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If any of the tensors is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If any of the strides ≤ 0</exception>
		/// <exception cref="ArgumentException">If <paramref name="op"/> is not a reduction operation whose input matches (<paramref name="inputs"/>, <paramref name="results"/>) and output matches <paramref name="results"/></exception>
		[AbstractApiMethod]
		public abstract bool FullReduce<TIn, TOut, TS>(Expression op, in DenseArrayWrapper<TIn, TS> inputs, Span<TOut> results) where TIn : unmanaged, IBaseNumber<TIn> where TOut : unmanaged, IBaseNumber<TOut> where TS : class, IStorage<TIn, TS>;
	}
}
