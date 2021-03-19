using System;
using System.Collections.Generic;
using System.Dynamic;

using Althea.Arrays;


namespace Althea.Solver
{
	/// <summary>
	/// The abstract class for runtime general solver API routines 
	/// </summary>
	public abstract partial class AbstractApi : AbstractRuntimeApi
	{
		#region basic
		/// <summary>
		/// Get the current using <see cref="AbstractApi"/>.
		/// </summary>
		/// <remarks><b>DO NOT</b> invoke methods of this property directly unless you are sure about what you are doing; otherwise, there may be exceptions and / or unnoticeable bugs.</remarks>
		public static AbstractApi? Current => RecentAPIs.First?.Value;

		private static readonly LinkedList<AbstractApi> RecentAPIs = new();

		internal static bool SetImplementation(Type implementation) => SetImplementation(RecentAPIs, implementation);
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
		#endregion


		#region abstract methods that actually do computations
		// Ignore Spelling: vec
		//tex:
		//Facts about Kronecker sum  times vector:
		//$$(A\oplus B)vec(X)\equiv(A\otimes I+I\otimes B)vec(X)=vec(XA^T+BX)$$
		//Facts about Kronecker product times vector:
		//$$(A\otimes B)vec(X)=vec(BXA^T) \text{ (notice that it is not } A^\dagger\text)$$

		/// <summary>
		/// When implemented by a derived class, compute the product of the Kronecker Sum of <paramref name="leftMatrix"/> and <paramref name="rightMatrix"/> and <paramref name="vector"/>:<br/>
		/// <c><paramref name="vector"/> = <paramref name="scalar"/> * (<paramref name="leftMatrix"/> ⨁ <paramref name="rightMatrix"/>) * <paramref name="vector"/> + <paramref name="scalarVector"/> * <paramref name="vector"/></c>
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="scalar">The scalar to multiply to the multiplication result</param>
		/// <param name="leftMatrix">The input left matrix to perform the Kronecker sum</param>
		/// <param name="rightMatrix">The input right matrix to perform the Kronecker sum</param>
		/// <param name="vector">The input / output vector</param>
		/// <param name="scalarVector">The scalar to multiply to the <paramref name="vector"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="leftMatrix"/> or <paramref name="rightMatrix"/> or <paramref name="vector"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If the sizes mismatch</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="scalar"/> is 0</exception>
		protected abstract bool KroneckerSumMultiplyVector_<T>(T scalar, MatrixBase<T> leftMatrix, MatrixBase<T> rightMatrix, ref VectorBase<T> vector, T scalarVector = default) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, compute the product of the Kronecker Product of <paramref name="leftMatrix"/> and <paramref name="rightMatrix"/> and <paramref name="vector"/>:<br/>
		/// <c><paramref name="vector"/> = <paramref name="scalar"/> * (<paramref name="leftMatrix"/> ⨂ <paramref name="rightMatrix"/>) * <paramref name="vector"/> + <paramref name="scalarVector"/> * <paramref name="vector"/></c>
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="scalar">The scalar to multiply to the multiplication result</param>
		/// <param name="leftMatrix">The input left matrix to perform the Kronecker product</param>
		/// <param name="rightMatrix">The input right matrix to perform the Kronecker product</param>
		/// <param name="vector">The input / output vector</param>
		/// <param name="scalarVector">The scalar to multiply to the <paramref name="vector"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="leftMatrix"/> or <paramref name="rightMatrix"/> or <paramref name="vector"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If the sizes mismatch</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="scalar"/> is 0</exception>
		protected abstract bool KroneckerProdMultiplyVector_<T>(T scalar, MatrixBase<T> leftMatrix, MatrixBase<T> rightMatrix, ref VectorBase<T> vector, T scalarVector = default) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, perform the naïve Lanczos algorithm to calculate the lowest eigenvalue and eigenvector of a hermitian matrix represented by <paramref name="matrixFunction"/> starting from the <paramref name="initial"/> vector.
		/// </summary>
		/// <typeparam name="TVec">The concrete vector class type</typeparam>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="matrixFunction">The function that represents the multiplication of the target matrix and any input vector <typeparamref name="TVec"/> which returns the multiplication result as a <typeparamref name="TVec"/></param>
		/// <param name="initial">The initial vector as a <typeparamref name="TVec"/></param>
		/// <param name="maxIter">The maximum number of iterations which may be decreased when out of memory</param>
		/// <param name="checkFirst">Whether to check the <paramref name="matrixFunction"/> and <paramref name="maxIter"/> first. If true, it may introduce some overhead</param>
		/// <param name="eigenvalue">The output approximate lowest eigenvalue</param>
		/// <param name="eigenvector">The output approximate lowest eigenvalue's corresponding approximate eigenvector as a <typeparamref name="TVec"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="initial"/> or <paramref name="matrixFunction"/> is null or invalid</exception>
		/// <remarks>All the temporary vectors shall be created by <see cref="IKrylovVector{TVec, T}.NewArrayAlike"/> of <paramref name="initial"/>, hence, using vector with simple storage location(s) may help reduce overheads.</remarks>
		/// <exception cref="ArgumentException">If the internal check fails</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="maxIter"/> is too large to fit in the memory</exception>
		protected abstract bool NaiveLanczos<TVec, T>(Func<TVec, TVec> matrixFunction, TVec initial, int maxIter, bool checkFirst, out double eigenvalue, out TVec eigenvector) where TVec : class, IKrylovVector<TVec, T>, new() where T : unmanaged;

		/// <summary>
		/// Lanczos algorithm for Hermitian matrix's partial (especially the lowest eigenvalues) eigen-problem.
		/// </summary>
		/// <param name="MatMulVecFunc">a function that receives a dense vector input and give the result of the multiplication of the Hermitian matrix and the input vector</param>
		/// <param name="initial">The initial vector</param>
		/// <param name="smallestK">only the smallest k eigenvalues are the target, we DO NOT recommend a larger k since Lanczos is not designed for it</param>
		/// <param name="tolerance">The tolerance of the Lanczos iterative solver, default 0 means <c>machine precision * 5</c></param>
		/// <param name="maxIter">max iteration number, if <paramref name="maxIter"/> ≤ 0, it will be auto calculated and the thick restart strategy will be used to compute multiple eigen-pairs until they are all converged; otherwise, the computation stops at total number of iterations = <paramref name="maxIter"/> while some of the eigen-pairs may not be calculated at all</param>
		/// <param name="reorthogonalize">perform re-orthogonalization or not, default is <c>true</c>, (notice that Lanczos algorithm is extremely numerical unstable without it)</param>
		/// <param name="useGap">use the estimated gap in the convergence criteria or use the matrix norm, default true</param>
		/// <param name="strategy">The restart strategy to use, if it is <see cref="RestartStrategy.UserDefine"/>, the <paramref name="selector"/> must be indicated</param>
		/// <param name="selector">used for selecting the preservation Ritz pairs only when <paramref name="strategy"/> is <see cref="RestartStrategy.UserDefine"/></param>
		/// <returns>An array of <see cref="double"/> as the eigenvalues and an array of <typeparamref name="TVec"/> as corresponding eigenvectors and the convergence.</returns>
		/// <typeparam name="T">The data type, see <see cref="AbstractArray{T}"/> for more information</typeparam>
		/// <typeparam name="TVec">The general dense vector type that inherits <see cref="AbstractArray{T}"/>, <see cref="IKrylovVector{TVec, T}"/> and must be a concrete class type</typeparam>
		/// <exception cref="ArgumentException">if any of the arguments is wrong</exception>
		/// <exception cref="InvalidOperationException">if the <paramref name="MatMulVecFunc"/> throws inner exceptions</exception>
		/// <exception cref="InsufficientMemoryException">if the <paramref name="smallestK"/> is too large to be calculated within free memory</exception>
		/// <remarks>Currently, if some eigen-pairs are not converged after maximum number of iterations, they will not be returned.</remarks>
		protected abstract bool Lanczos<TVec, T>(Func<TVec, TVec> MatMulVecFunc, TVec initial, int smallestK = 1, int maxIter = 0, double tolerance = 0, ReorthogonalizeMethod reorthogonalize = ReorthogonalizeMethod.Selective, bool useGap = true, RestartStrategy strategy = RestartStrategy.Naive, IRestartStrategy selector = null)
		#endregion
	}
}