using System;
using System.Collections.Generic;
using System.Dynamic;


namespace Althea.TensorAlgebra.Dense
{
	/// <summary>
	/// The abstract class for runtime dense tensor algebra API routines 
	/// </summary>
	public abstract partial class AbstractApi : AbstractRuntimeApi
	{
		#region basic
		/// <summary>
		/// Get the currently using <see cref="AbstractApi"/>.
		/// </summary>
		/// <remarks><b>DO NOT</b> invoke methods of this property directly unless you are sure about what you are doing; otherwise, there may be exceptions and / or unnoticeable bugs.</remarks>
		public static AbstractApi? Current => RecentAPIs.First?.Value;

		private static readonly LinkedList<AbstractApi> RecentAPIs = new();

		/// <summary>
		/// Set the currently using <see cref="AbstractApi"/> to the given <paramref name="implementation"/>
		/// </summary>
		/// <param name="implementation">The <see cref="Type"/> of the given implementation of <see cref="AbstractApi"/></param>
		/// <returns>Success or not.</returns>
		public static bool SetImplementation(Type implementation) => SetImplementation(RecentAPIs, implementation);

		/// <summary>
		/// Set the currently using <see cref="AbstractApi"/> to the given <paramref name="implementation"/>
		/// </summary>
		/// <param name="implementation">The instance of an implementation of <see cref="AbstractApi"/></param>
		/// <returns>Success or not.</returns>
		internal static bool SetImplementation(AbstractApi? implementation) => SetImplementation(RecentAPIs, implementation);
		#endregion


		#region dynamic invocation
		/// <summary>
		/// Get the dynamic object used to dynamically invoke method(s) not listed explicitly here (the methods extra defined in derived classes)
		/// </summary>
		/// <remarks>
		/// Due to the limitations of dynamic invocation, <c>ref</c>, <c>in</c>, <c>out</c> and <c>ref struct</c>, etc. are not supported and non of the input arguments can be null.<br/>
		/// Since there are internal caching for <see cref="DynamicObject.TryInvokeMember(InvokeMemberBinder, object[], out object)"/>, the average repeated dynamic invocation may cost around 1 microsecond.
		/// </remarks>
		/// <example><code>
		/// long number = AbstractApi.Dynamic.CholeskyDecompose(...);
		/// </code></example>
		public static dynamic Dynamic => singletonDynamic;

		private static readonly DynamicInvocations singletonDynamic = new();

		private sealed class DynamicInvocations : DynamicInvocation
		{
			public override bool TryInvokeMember(InvokeMemberBinder binder, object?[]? args, out object? result)
			{
				result = DynamicInvokeExtraMethod(RecentAPIs, binder.Name, args);
				return true;
			}
		}
		#endregion


		#region support information
		/// <summary>
		/// When implemented by a derived class, check if the given <see cref="CombinationOfLocations"/>s are supported by tensor binary operations of this implementation or not.
		/// </summary>
		/// <param name="location1">The first given <see cref="CombinationOfLocations"/></param>
		/// <param name="location2">The second given <see cref="CombinationOfLocations"/></param>
		/// <returns>Whether binary operations on <paramref name="location1"/> and <paramref name="location2"/> are supported by this <see cref="AbstractApi"/>.</returns>
		protected abstract bool IsSupportedTensorBinary(CombinationOfLocations location1, CombinationOfLocations location2);

		/// <summary>
		/// When implemented by a derived class, check if the given <see cref="CombinationOfLocations"/>s are supported by tensor trinary operations of this implementation or not.
		/// </summary>
		/// <param name="location1">The first given <see cref="CombinationOfLocations"/></param>
		/// <param name="location2">The second given <see cref="CombinationOfLocations"/></param>
		/// <param name="location3">The third given <see cref="CombinationOfLocations"/></param>
		/// <returns>Whether trinary operations on <paramref name="location1"/>, <paramref name="location2"/> and <paramref name="location3"/> are supported by this <see cref="AbstractApi"/>.</returns>
		protected abstract bool IsSupportedTensorTrinary(CombinationOfLocations location1, CombinationOfLocations location2, CombinationOfLocations location3);
		#endregion


		#region static methods as dispatchers
		/// <summary>
		/// Compute the tensor permutation from the <paramref name="source"/> tensor to the <paramref name="destination"/> tensor with the given <paramref name="permutationOrder"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="source">The source dense tensor as a <see cref="DenseTensorWrapper{T}"/></param>
		/// <param name="destination">The destination dense tensor as a <see cref="DenseTensorWrapper{T}"/></param>
		/// <param name="permutationOrder">The permutation order as a <see cref="ReadOnlySpan{T}"/> of <see cref="int"/></param>
		/// <remarks>If <paramref name="permutationOrder"/> is an identity permutation, this method simply performs (pitched) tensor copy</remarks>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="destination"/> or <paramref name="permutationOrder"/> is invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="permutationOrder"/> is not a full permutation order</exception>
		public static void Permute<T>(DenseTensorWrapper<T> source, DenseTensorWrapper<T> destination, ReadOnlySpan<int> permutationOrder) where T : unmanaged
		{
			CombinationOfLocations location1 = source.ValueStorage.LocationDescription, location2 = destination.ValueStorage.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedTensorBinary(location1, location2), node);
				success = node.Value.Permute_(source, destination, permutationOrder);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// Compute the point-wise binary operation for input <paramref name="leftPerm"/>(<paramref name="left"/>) and <paramref name="rightPerm"/>(<paramref name="right"/>) tensors and stored the result to the <paramref name="destination"/> tensor
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="binary">The <see cref="BinaryOperation"/> to be applied to <paramref name="left"/> and <paramref name="right"/> tensors</param>
		/// <param name="left">The left input dense tensor as a <see cref="DenseTensorWrapper{T}"/>, can be invalid</param>
		/// <param name="right">The right input dense tensor as a <see cref="DenseTensorWrapper{T}"/>, can be invalid</param>
		/// <param name="leftPerm">The full permutation order to be applied to <paramref name="left"/> before the binary operation, can be empty if <paramref name="left"/> is invalid</param>
		/// <param name="rightPerm">The full permutation order to be applied to <paramref name="right"/> before the binary operation, can be empty if <paramref name="right"/> is invalid</param>
		/// <param name="destination">The destination dense tensor as a <see cref="DenseTensorWrapper{T}"/>, its <see cref="DenseTensorWrapper{T}.Operation"/> and <see cref="DenseTensorWrapper{T}.Scalar"/> are ignored.</param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="destination"/> is invalid</exception>
		/// <exception cref="ArgumentException">If the given tensors have different sizes under their permutations; or <paramref name="left"/> and <paramref name="right"/> are both invalid</exception>
		public static void OperationBinary<T>(BinaryOperation binary, DenseTensorWrapper<T> left, Span<int> leftPerm, DenseTensorWrapper<T> right, Span<int> rightPerm, DenseTensorWrapper<T> destination) where T : unmanaged
		{
			bool leftValid = !left.IsInputInvalid(), rightValid = !right.IsInputInvalid();
			if (!leftValid && !rightValid)
				throw new ArgumentException(Resources.Parameter.CannotAllNull);
			if (destination.IsInvalid())
				throw new ArgumentNullException(nameof(destination));

			CombinationOfLocations? location1 = leftValid ? left.ValueStorage.LocationDescription : null,
									location2 = rightValid ? right.ValueStorage.LocationDescription : null;
			CombinationOfLocations location3 = destination.ValueStorage.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs,
					a => location1 is null || location2 is null ? 
						 a.IsSupportedTensorBinary(location1 ?? location2 ?? default, location3) :
						 a.IsSupportedTensorTrinary(location1.Value, location2.Value, location3),
					node);
				success = node.Value.OperationBinary_(binary, left, leftPerm, right, rightPerm, destination);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// Compute the tensor reduction from the <paramref name="source"/> tensor to the <paramref name="destination"/> tensor with the given <paramref name="reduceDimensions"/>:<br/>
		/// <c><paramref name="destination"/>.<see cref="DenseTensorWrapper{T}.ValueStorage">Storage</see> = <paramref name="source"/>.<see cref="DenseTensorWrapper{T}.Scalar">Scalar</see> * <paramref name="reduce"/>(<paramref name="source"/>.<see cref="DenseTensorWrapper{T}.Operation">Op</see>(<paramref name="source"/>.<see cref="DenseTensorWrapper{T}.ValueStorage">Storage</see>[<paramref name="reduceDimensions"/>])) + <paramref name="destination"/>.<see cref="DenseTensorWrapper{T}.Scalar">Scalar</see> * <paramref name="destination"/>.<see cref="DenseTensorWrapper{T}.Operation">Op</see>(<paramref name="destination"/>.<see cref="DenseTensorWrapper{T}.ValueStorage">Storage</see>)</c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="reduce">The (symmetric) reduction operation as a <see cref="BinaryOperation"/></param>
		/// <param name="source">The source dense tensor as a <see cref="DenseTensorWrapper{T}"/> to be reduced</param>
		/// <param name="destination">The destination dense tensor as a <see cref="DenseTensorWrapper{T}"/></param>
		/// <param name="reduceDimensions">The values in this <b>set</b> (as a <see cref="ReadOnlySpan{T}"/> of <see cref="int"/>) are the dimensions of which <paramref name="source"/> tensor are reduced</param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="destination"/> or <paramref name="reduceDimensions"/> is invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="reduceDimensions"/> is not a partial permutation order or the sizes mismatches</exception>
		public static void Reduce<T>(BinaryOperation reduce, DenseTensorWrapper<T> source, DenseTensorWrapper<T> destination, ReadOnlySpan<int> reduceDimensions) where T : unmanaged
		{
			CombinationOfLocations location1 = source.ValueStorage.LocationDescription, location2 = destination.ValueStorage.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedTensorBinary(location1, location2), node);
				success = node.Value.Reduce_(reduce, source, destination, reduceDimensions);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// Compute the tensor contraction of the <paramref name="left"/> and <paramref name="right"/> tensors and store the result to the <paramref name="destination"/> tensor:<br/>
		/// <c><paramref name="destination"/>.<see cref="DenseTensorWrapper{T}.ValueStorage">Storage</see> = <paramref name="left"/>.<see cref="DenseTensorWrapper{T}.Scalar">Scalar</see> * <paramref name="right"/>.<see cref="DenseTensorWrapper{T}.Scalar">Scalar</see> * contract(<paramref name="left"/>.<see cref="DenseTensorWrapper{T}.Operation">Op</see>(<paramref name="left"/>.<see cref="DenseTensorWrapper{T}.ValueStorage">Storage</see>), <paramref name="right"/>.<see cref="DenseTensorWrapper{T}.Operation">Op</see>(<paramref name="right"/>.<see cref="DenseTensorWrapper{T}.ValueStorage">Storage</see>)) + <paramref name="destination"/>.<see cref="DenseTensorWrapper{T}.Scalar">Scalar</see> * <paramref name="destination"/>.<see cref="DenseTensorWrapper{T}.Operation">Op</see>(<paramref name="destination"/>.<see cref="DenseTensorWrapper{T}.ValueStorage">Storage</see>)</c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="left">The left input dense tensor as a <see cref="DenseTensorWrapper{T}"/> to be contracted</param>
		/// <param name="right">The right input dense tensor as a <see cref="DenseTensorWrapper{T}"/> to be contracted</param>
		/// <param name="destination">The destination dense tensor as a <see cref="DenseTensorWrapper{T}"/></param>
		/// <param name="info">The <see cref="TensorContractInfo"/> indicating how the contraction shall be performed</param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="left"/> or <paramref name="right"/> or <paramref name="destination"/> or <paramref name="info"/> is invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="info"/> mismatches the given tensors</exception>
		public static void Contract<T>(DenseTensorWrapper<T> left, DenseTensorWrapper<T> right, DenseTensorWrapper<T> destination, TensorContractInfo info) where T : unmanaged
		{
			CombinationOfLocations location1 = left.ValueStorage.LocationDescription, location2 = right.ValueStorage.LocationDescription, location3 = destination.ValueStorage.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedTensorTrinary(location1, location2, location3), node);
				success = node.Value.Contract_(left, right, destination, info);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}
		#endregion


		#region abstract methods that actually do computations
		/// <summary>
		/// When implemented by a derived class, compute the tensor permutation from the <paramref name="source"/> tensor to the <paramref name="destination"/> tensor with the given <paramref name="permutationOrder"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="source">The source dense tensor as a <see cref="DenseTensorWrapper{T}"/></param>
		/// <param name="destination">The destination dense tensor as a <see cref="DenseTensorWrapper{T}"/>, its <see cref="DenseTensorWrapper{T}.Operation"/> and <see cref="DenseTensorWrapper{T}.Scalar"/> are ignored.</param>
		/// <param name="permutationOrder">The permutation order as a <see cref="ReadOnlySpan{T}"/> of <see cref="int"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <remarks>If <paramref name="permutationOrder"/> is an identity permutation, this method shall simply perform (pitched) tensor copy</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="destination"/> or <paramref name="permutationOrder"/> is invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="permutationOrder"/> is not a full permutation order or the sizes mismatches</exception>
		protected abstract bool Permute_<T>(DenseTensorWrapper<T> source, DenseTensorWrapper<T> destination, ReadOnlySpan<int> permutationOrder) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, compute the point-wise binary operation for input <paramref name="leftPerm"/>(<paramref name="left"/>) and <paramref name="rightPerm"/>(<paramref name="right"/>) tensors and stored the result to the <paramref name="destination"/> tensor
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="binary">The <see cref="BinaryOperation"/> to be applied to <paramref name="left"/> and <paramref name="right"/> tensors</param>
		/// <param name="left">The left input dense tensor as a <see cref="DenseTensorWrapper{T}"/>, can be invalid</param>
		/// <param name="right">The right input dense tensor as a <see cref="DenseTensorWrapper{T}"/>, can be invalid</param>
		/// <param name="leftPerm">The full permutation order to be applied to <paramref name="left"/> before the binary operation, can be empty if <paramref name="left"/> is invalid</param>
		/// <param name="rightPerm">The full permutation order to be applied to <paramref name="right"/> before the binary operation, can be empty if <paramref name="right"/> is invalid</param>
		/// <param name="destination">The destination dense tensor as a <see cref="DenseTensorWrapper{T}"/>, its <see cref="DenseTensorWrapper{T}.Operation"/> and <see cref="DenseTensorWrapper{T}.Scalar"/> are ignored.</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="destination"/> is invalid</exception>
		/// <exception cref="ArgumentException">If the given tensors have different sizes under their permutations; or <paramref name="left"/> and <paramref name="right"/> are both invalid</exception>
		protected abstract bool OperationBinary_<T>(BinaryOperation binary, DenseTensorWrapper<T> left, Span<int> leftPerm, DenseTensorWrapper<T> right, Span<int> rightPerm, DenseTensorWrapper<T> destination) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, compute the tensor reduction from the <paramref name="source"/> tensor to the <paramref name="destination"/> tensor with the given <paramref name="reduceDimensions"/>:<br/>
		/// <c><paramref name="destination"/>.<see cref="DenseTensorWrapper{T}.ValueStorage">Storage</see> = <paramref name="source"/>.<see cref="DenseTensorWrapper{T}.Scalar">Scalar</see> * <paramref name="reduce"/>(<paramref name="source"/>.<see cref="DenseTensorWrapper{T}.Operation">Op</see>(<paramref name="source"/>.<see cref="DenseTensorWrapper{T}.ValueStorage">Storage</see>[<paramref name="reduceDimensions"/>])) + <paramref name="destination"/>.<see cref="DenseTensorWrapper{T}.Scalar">Scalar</see> * <paramref name="destination"/>.<see cref="DenseTensorWrapper{T}.Operation">Op</see>(<paramref name="destination"/>.<see cref="DenseTensorWrapper{T}.ValueStorage">Storage</see>)</c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="reduce">The (symmetric) reduction operation as a <see cref="BinaryOperation"/></param>
		/// <param name="source">The source dense tensor as a <see cref="DenseTensorWrapper{T}"/> to be reduced</param>
		/// <param name="destination">The destination dense tensor as a <see cref="DenseTensorWrapper{T}"/></param>
		/// <param name="reduceDimensions">The values in this <b>set</b> (as a <see cref="ReadOnlySpan{T}"/> of <see cref="int"/>) are the dimensions of which <paramref name="source"/> tensor are reduced</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="destination"/> or <paramref name="reduceDimensions"/> is invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="reduceDimensions"/> is not a partial permutation order or the sizes mismatches</exception>
		protected abstract bool Reduce_<T>(BinaryOperation reduce, DenseTensorWrapper<T> source, DenseTensorWrapper<T> destination, ReadOnlySpan<int> reduceDimensions) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, compute the tensor contraction of the <paramref name="left"/> and <paramref name="right"/> tensors and store the result to the <paramref name="destination"/> tensor:<br/>
		/// <c><paramref name="destination"/>.<see cref="DenseTensorWrapper{T}.ValueStorage">Storage</see> = <paramref name="left"/>.<see cref="DenseTensorWrapper{T}.Scalar">Scalar</see> * <paramref name="right"/>.<see cref="DenseTensorWrapper{T}.Scalar">Scalar</see> * contract(<paramref name="left"/>.<see cref="DenseTensorWrapper{T}.Operation">Op</see>(<paramref name="left"/>.<see cref="DenseTensorWrapper{T}.ValueStorage">Storage</see>), <paramref name="right"/>.<see cref="DenseTensorWrapper{T}.Operation">Op</see>(<paramref name="right"/>.<see cref="DenseTensorWrapper{T}.ValueStorage">Storage</see>)) + <paramref name="destination"/>.<see cref="DenseTensorWrapper{T}.Scalar">Scalar</see> * <paramref name="destination"/>.<see cref="DenseTensorWrapper{T}.Operation">Op</see>(<paramref name="destination"/>.<see cref="DenseTensorWrapper{T}.ValueStorage">Storage</see>)</c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="left">The left input dense tensor as a <see cref="DenseTensorWrapper{T}"/> to be contracted</param>
		/// <param name="right">The right input dense tensor as a <see cref="DenseTensorWrapper{T}"/> to be contracted</param>
		/// <param name="destination">The destination dense tensor as a <see cref="DenseTensorWrapper{T}"/></param>
		/// <param name="info">The <see cref="TensorContractInfo"/> indicating how the contraction shall be performed</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="left"/> or <paramref name="right"/> or <paramref name="destination"/> or <paramref name="info"/> is invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="info"/> mismatches the given tensors</exception>
		protected abstract bool Contract_<T>(DenseTensorWrapper<T> left, DenseTensorWrapper<T> right, DenseTensorWrapper<T> destination, TensorContractInfo info) where T : unmanaged;
		#endregion
	}
}