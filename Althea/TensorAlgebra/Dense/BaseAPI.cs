using Althea.Array;
using Althea.Helpers;
using Althea.LinearAlgebra;
using Althea.SourceGenerator;
using Althea.Storage;


namespace Althea.TensorAlgebra.Dense
{
	/// <summary>
	/// The abstract interface for basic runtime dense tensor algebra API routines 
	/// </summary>
	[AbstractRuntimeApi]
	public interface IBaseAbstractApi : IAbstractRuntimeApi<IBaseAbstractApi>
	{
		/// <summary>
		/// When implemented by a derived class, compute the tensor permutation from the <paramref name="source"/> tensor to the <paramref name="destination"/> tensor with the given <paramref name="permutationOrder"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS1">The input storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The output storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="source">The source dense tensor as a <see cref="DenseTensorWrapper{T, TS}"/></param>
		/// <param name="destination">The destination dense tensor as a <see cref="DenseArrayWrapper{T, TS}"/></param>
		/// <param name="permutationOrder">The permutation order as a <see cref="ReadOnlySpan{T}"/> of <see cref="int"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <remarks>If <paramref name="permutationOrder"/> is an identity permutation, this method shall simply perform (pitched) tensor copy</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="destination"/> or <paramref name="permutationOrder"/> is invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="permutationOrder"/> is not a full permutation order or the sizes mismatches</exception>
		[AbstractApiMethod]
		public abstract bool Permute<T, TS1, TS2>(DenseTensorWrapper<T, TS1> source, DenseArrayWrapper<T, TS2> destination, ReadOnlySpan<int> permutationOrder) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

		/// <summary>
		/// When implemented by a derived class, compute the point-wise binary operation for input <paramref name="leftPerm"/>(<paramref name="left"/>) and <paramref name="rightPerm"/>(<paramref name="right"/>) tensors and stored the result to the <paramref name="destination"/> tensor
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS1">The first input storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The second input storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS3">The output storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="binary">The <see cref="BinaryOperation"/> to be applied to <paramref name="left"/> and <paramref name="right"/> tensors</param>
		/// <param name="left">The left input dense tensor as a <see cref="DenseTensorWrapper{T, TS}"/>, can be invalid</param>
		/// <param name="right">The right input dense tensor as a <see cref="DenseTensorWrapper{T, TS}"/>, can be invalid</param>
		/// <param name="leftPerm">The full permutation order to be applied to <paramref name="left"/> before the binary operation, can be empty if <paramref name="left"/> is invalid</param>
		/// <param name="rightPerm">The full permutation order to be applied to <paramref name="right"/> before the binary operation, can be empty if <paramref name="right"/> is invalid</param>
		/// <param name="destination">The destination dense tensor as a <see cref="DenseArrayWrapper{T, TS}"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="destination"/> is invalid or both <paramref name="left"/> and <paramref name="right"/> are invalid</exception>
		/// <exception cref="ArgumentException">If the given tensors have different sizes under their permutations; or <paramref name="left"/> and <paramref name="right"/> are both invalid</exception>
		[AbstractApiMethod]
		public abstract bool OperationBinary<T, TS1, TS2, TS3>(ManagedEnum<BinaryOperation> binary, DenseTensorWrapper<T, TS1> left, ReadOnlySpan<int> leftPerm, DenseTensorWrapper<T, TS2> right, ReadOnlySpan<int> rightPerm, DenseArrayWrapper<T, TS3> destination) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>;

		/// <summary>
		/// When implemented by a derived class, compute the tensor reduction from the <paramref name="source"/> tensor to the <paramref name="destination"/> tensor with the given <paramref name="reduceDimensions"/>:<br/>
		/// <c><paramref name="destination"/>.<see cref="DenseTensorWrapper{T, TS}.ValueStorage">Storage</see> = <paramref name="source"/>.<see cref="DenseTensorWrapper{T, TS}.Scalar">Scalar</see> * <paramref name="reduce"/>(<paramref name="source"/>.<see cref="DenseTensorWrapper{T, TS}.Operation">Op</see>(<paramref name="source"/>.<see cref="DenseTensorWrapper{T, TS}.ValueStorage">Storage</see>[<paramref name="reduceDimensions"/>])) + <paramref name="destination"/>.<see cref="DenseTensorWrapper{T, TS}.Scalar">Scalar</see> * <paramref name="destination"/>.<see cref="DenseTensorWrapper{T, TS}.Operation">Op</see>(<paramref name="destination"/>.<see cref="DenseTensorWrapper{T, TS}.ValueStorage">Storage</see>)</c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS1">The input storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The output storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="reduce">The (symmetric) reduction operation as a <see cref="ReduceOperation"/></param>
		/// <param name="source">The source dense tensor as a <see cref="DenseTensorWrapper{T, TS}"/> to be reduced</param>
		/// <param name="destination">The destination dense tensor as a <see cref="DenseTensorWrapper{T, TS}"/></param>
		/// <param name="reduceDimensions">The values in this <b>set</b> (as a <see cref="ReadOnlySpan{T}"/> of <see cref="int"/>) are the dimensions of which <paramref name="source"/> tensor are reduced</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="destination"/> or <paramref name="reduceDimensions"/> is invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="reduceDimensions"/> is not a partial permutation order or the sizes mismatches</exception>
		[AbstractApiMethod]
		public abstract bool Reduce<T, TS1, TS2>(ManagedEnum<ReduceOperation> reduce, DenseTensorWrapper<T, TS1> source, DenseTensorWrapper<T, TS2> destination, ReadOnlySpan<int> reduceDimensions) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

		/// <summary>
		/// When implemented by a derived class, compute the tensor contraction of the <paramref name="left"/> and <paramref name="right"/> tensors and store the result to the <paramref name="destination"/> tensor:<br/>
		/// <c><paramref name="destination"/>.<see cref="DenseTensorWrapper{T, TS}.ValueStorage">Storage</see> = <paramref name="left"/>.<see cref="DenseTensorWrapper{T, TS}.Scalar">Scalar</see> * <paramref name="right"/>.<see cref="DenseTensorWrapper{T, TS}.Scalar">Scalar</see> * contract(<paramref name="left"/>.<see cref="DenseTensorWrapper{T, TS}.Operation">Op</see>(<paramref name="left"/>.<see cref="DenseTensorWrapper{T, TS}.ValueStorage">Storage</see>), <paramref name="right"/>.<see cref="DenseTensorWrapper{T, TS}.Operation">Op</see>(<paramref name="right"/>.<see cref="DenseTensorWrapper{T, TS}.ValueStorage">Storage</see>)) + <paramref name="destination"/>.<see cref="DenseTensorWrapper{T, TS}.Scalar">Scalar</see> * <paramref name="destination"/>.<see cref="DenseTensorWrapper{T, TS}.Operation">Op</see>(<paramref name="destination"/>.<see cref="DenseTensorWrapper{T, TS}.ValueStorage">Storage</see>)</c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS1">The first input storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The second input storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS3">The output storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="left">The left input dense tensor as a <see cref="DenseTensorWrapper{T, TS}"/> to be contracted</param>
		/// <param name="right">The right input dense tensor as a <see cref="DenseTensorWrapper{T, TS}"/> to be contracted</param>
		/// <param name="destination">The destination dense tensor as a <see cref="DenseTensorWrapper{T, TS}"/></param>
		/// <param name="info">The <see cref="TensorContractInfo"/> indicating how the contraction shall be performed</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="left"/> or <paramref name="right"/> or <paramref name="destination"/> or <paramref name="info"/> is invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="info"/> mismatches the given tensors</exception>
		[AbstractApiMethod]
		public abstract bool Contract<T, TS1, TS2, TS3>(DenseTensorWrapper<T, TS1> left, DenseTensorWrapper<T, TS2> right, DenseTensorWrapper<T, TS3> destination, TensorContractInfo info) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>;
	}
}