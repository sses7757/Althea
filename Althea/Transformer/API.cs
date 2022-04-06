using Althea.Array;
using Althea.Helpers;
using Althea.NativeTypes;
using Althea.SourceGenerator;
using Althea.Storage;


namespace Althea.Transformer
{
	/// <summary>
	/// The abstract interface for runtime transformer APIs.
	/// </summary>
	[AbstractRuntimeApi]
	public interface IAbstractApi : IAbstractRuntimeApi<IAbstractApi>
	{
		/// <summary>
		/// When implemented by a derived class, perform the (inverse) Fourier transform to the given <paramref name="input"/> array and write the result to the given <paramref name="output"/> array.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number type as the data type</typeparam>
		/// <typeparam name="TS1">The input storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The output storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="forward">Whether to compute the Fourier transform (true) or the inverse Fourier transform (false)</param>
		/// <param name="input">The input dense array as a <see cref="DenseArrayWrapper{T, TS}"/></param>
		/// <param name="output">The output dense array as a <see cref="DenseArrayWrapper{T, TS}"/>, can be the same as <paramref name="input"/> for in-place calculation</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentException">If <paramref name="output"/>'s rank is different from <paramref name="input"/>'s rank</exception>
		[AbstractApiMethod]
		public abstract bool FourierTransform<T, TS1, TS2>(bool forward, DenseArrayWrapper<T, TS1> input, DenseArrayWrapper<T, TS2> output) where T : unmanaged, IFloatingPoint<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

		/// <summary>
		/// When implemented by a derived class, perform the Fourier transform (from real type to complex type) to the given <paramref name="input"/> array and write the result to the given <paramref name="output"/> array.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number type as the data type</typeparam>
		/// <typeparam name="TS1">The input storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The output storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="input">The input dense array of <typeparamref name="T"/> as a <see cref="DenseArrayWrapper{T, TS}"/></param>
		/// <param name="output">The output dense array of <see cref="Complex{T}"/> as a <see cref="DenseArrayWrapper{T, TS}"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentException">If <paramref name="output"/>'s rank is different from <paramref name="input"/>'s rank</exception>
		/// <exception cref="TypeMismatchException">If <typeparamref name="T"/> is a complex type</exception>
		[AbstractApiMethod]
		public abstract bool FourierTransform<T, TS1, TS2>(DenseArrayWrapper<T, TS1> input, DenseArrayWrapper<Complex<T>, TS2> output) where T : unmanaged, IFloatingPoint<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<Complex<T>, TS2>;

		/// <summary>
		/// When implemented by a derived class, perform the inverse Fourier transform (from complex type to real type) to the given <paramref name="input"/> array and write the result to the given <paramref name="output"/> array.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number type as the data type</typeparam>
		/// <typeparam name="TS1">The input storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The output storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="input">The input dense array as of <see cref="Complex{T}"/> a <see cref="DenseArrayWrapper{T, TS}"/></param>
		/// <param name="output">The output dense array of <typeparamref name="T"/> as a <see cref="DenseArrayWrapper{T, TS}"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentException">If <paramref name="output"/>'s rank is different from <paramref name="input"/>'s rank</exception>
		/// <exception cref="TypeMismatchException">If <typeparamref name="T"/> is a complex type</exception>
		[AbstractApiMethod]
		public abstract bool FourierTransform<T, TS1, TS2>(DenseArrayWrapper<Complex<T>, TS1> input, DenseArrayWrapper<T, TS2> output) where T : unmanaged, IFloatingPoint<T> where TS1 : class, IStorage<Complex<T>, TS1> where TS2 : class, IStorage<T, TS2>;
	}
}
