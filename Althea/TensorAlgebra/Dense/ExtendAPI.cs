using Althea.Array;
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
		/// <param name="op">The <see cref="BinaryOperation"/> to use</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="tensor"/> is invalid</exception>
		[AbstractApiMethod]
		public abstract bool PointWiseBinary<T, TS>(DenseArrayWrapper<T, TS> tensor, T value, BinaryOperation op) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <summary>
		/// When implemented by a derived class, calculate the 2-norm of all elements in <paramref name="tensor"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS">The storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="tensor">The dense tensor as a <see cref="DenseArrayWrapper{T, TS}"/></param>
		/// <param name="result">Output the 2-norm result as a <typeparamref name="T"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="tensor"/> is invalid</exception>
		[AbstractApiMethod]
		public abstract bool Norm<T, TS>(DenseArrayWrapper<T, TS> tensor, out T result) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <summary>
		/// When implemented by a derived class, perform unary operation <paramref name="op"/> to each element in <paramref name="tensor"/> and aggregate by <paramref name="reduce"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS">The storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="tensor">The dense tensor as a <see cref="DenseArrayWrapper{T, TS}"/></param>
		/// <param name="op">The <see cref="UnaryOperation"/> to apply to each element in <paramref name="tensor"/></param>
		/// <param name="reduce">The <see cref="BinaryOperation"/> to use</param>
		/// <param name="result">Output the reduction result as a <typeparamref name="T"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="tensor"/> is invalid</exception>
		[AbstractApiMethod]
		public abstract bool PointWiseAggregation<T, TS>(DenseArrayWrapper<T, TS> tensor, UnaryOperation op, BinaryOperation reduce, out T result) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <summary>
		/// When implemented by a derived class, perform unary operation <paramref name="op"/> to each element in <paramref name="tensor"/> and return the index of aggregation operation <paramref name="reduce"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS">The storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="tensor">The dense tensor as a <see cref="DenseArrayWrapper{T, TS}"/></param>
		/// <param name="op">The <see cref="UnaryOperation"/> to apply to each element in <paramref name="tensor"/></param>
		/// <param name="reduce">The <see cref="BinaryOperation"/> to use</param>
		/// <param name="index">Output the reduction index result as a <typeparamref name="T"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="tensor"/> is invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="reduce"/> is not aggregation operation like <see cref="BinaryOperation.Maximum"/></exception>
		[AbstractApiMethod]
		public abstract bool PointWiseArgAggregation<T, TS>(DenseArrayWrapper<T, TS> tensor, UnaryOperation op, BinaryOperation reduce, out long index) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;
	}
}
