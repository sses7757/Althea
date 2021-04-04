using System;
using System.Collections.Generic;
using System.Dynamic;


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


		#region static methods as dispatchers
		/// <summary>
		/// Compute the product of the Kronecker Multiply or Sum of <paramref name="leftMatrix"/> and <paramref name="rightMatrix"/> and <paramref name="vector"/>:<br/>
		/// <c><paramref name="vector"/> = <paramref name="scalar"/> * (<paramref name="leftMatrix"/> op <paramref name="rightMatrix"/>) * <paramref name="vector"/> + <paramref name="scalarVector"/> * <paramref name="vector"/></c> where '<c>op</c>' is '⨁' if <paramref name="multiply"/> is false or '⨂' otherwise.
		/// </summary>
		/// <typeparam name="TMat">The concrete matrix type as a <see cref="IMultipliableMatrix{TMat, TVec, T}"/></typeparam>
		/// <typeparam name="TVec">The concrete vector type as a <see cref="IConvertibleVector{TVec, TMat, T}"/></typeparam>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="multiply">Whether to perform Kronecker Multiply or Kronecker Sum</param>
		/// <param name="scalar">The scalar to multiply to the multiplication result</param>
		/// <param name="leftMatrix">The input left matrix to perform the Kronecker sum</param>
		/// <param name="rightMatrix">The input right matrix to perform the Kronecker sum</param>
		/// <param name="vector">The input / output vector</param>
		/// <param name="scalarVector">The scalar to multiply to the <paramref name="vector"/></param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="leftMatrix"/> or <paramref name="rightMatrix"/> or <paramref name="vector"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If the sizes mismatch</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="scalar"/> is 0</exception>
		public static void KroneckerMultiplyVector<TMat, TVec, T>(bool multiply, T scalar, TMat leftMatrix, TMat rightMatrix, ref TVec vector, T scalarVector = default)
			where TMat : class, IMultipliableMatrix<TMat, TVec, T>, IDisposable, new()
			where TVec : class, IConvertibleVector<TVec, TMat, T>, IDisposable, new()
			where T : unmanaged
		{
			bool success = false;
			LinkedListNode<AbstractApi>? node = RecentAPIs.First;
			while (!success)
			{
				if (node is null)
					break;
				success = node.Value.KroneckerMultiplyVector_(multiply, scalar, leftMatrix, rightMatrix, ref vector, scalarVector);
				node = node?.Next;
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// Perform a naïve Krylov subspace algorithm (typically the naïve Lanczos algorithm) to calculate the lowest eigenvalue and eigenvector of a hermitian matrix.
		/// </summary>
		/// <typeparam name="T">Any float-point type unmanaged struct as the data type</typeparam>
		/// <typeparam name="TVec">The concrete vector class type hat implements <see cref="IKrylovVector{TVec, T}"/></typeparam>
		/// <param name="info">The <see cref="KrylovSubspaceSolveInfo{TVec, T}"/> used as input information and output container</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <remarks>Only <paramref name="info"/>'s <see cref="KrylovSubspaceSolveInfo{TVec, T}.MatrixFunction"/>, <see cref="KrylovSubspaceSolveInfo{TVec, T}.InitialVector"/> and <see cref="KrylovSubspaceSolveInfo{TVec, T}.MaxRestarts"/> are used as inputs.</remarks>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentException">If <paramref name="info"/> contains invalid value</exception>
		public static (double eigenvalue, TVec eigenvector) NaiveKrylovSubspaceEigenHermitain<TVec, T>(ref KrylovSubspaceSolveInfo<TVec, T> info)
			where TVec : class, IKrylovVector<TVec, T>, new()
			where T : unmanaged
		{
			double eigenvalue = default;
			TVec? eigenvector = null;
			bool success = false;
			LinkedListNode<AbstractApi>? node = RecentAPIs.First;
			while (!success)
			{
				if (node is null)
					break;
				success = node.Value.NaiveKrylovSubspaceEigenHermitain_(ref info, out eigenvalue, out eigenvector);
				node = node?.Next;
			}
			if (success && node is not null && eigenvector is not null)
				SetImplementation(RecentAPIs, node.Value);
			else
				throw new InvalidOperationException(Resources.Backend.NotAvailable);
			return (eigenvalue, eigenvector);
		}

		/// <summary>
		/// Perform a restart Krylov subspace algorithm (typically the Lanczos or the Krylov-Schur algorithm) to solve a hermitian (or a non-hermitian) matrix's lowest several eigenvalues and eigenvectors.
		/// </summary>
		/// <typeparam name="T">Any float-point type unmanaged struct as the data type</typeparam>
		/// <typeparam name="TVec">The concrete vector class type hat implements <see cref="IKrylovVector{TVec, T}"/></typeparam>
		/// <param name="hermitian">Whether the given matrix is a hermitian one or a general square matrix</param>
		/// <param name="info">The <see cref="KrylovSubspaceSolveInfo{TVec, T}"/> used as input information and output container</param>
		/// <returns>The number of converged eigen-pairs</returns>
		/// <remarks><paramref name="info"/>'s <see cref="KrylovSubspaceSolveInfo{TVec, T}.WhichEigenvaluesDesired"/>, <see cref="KrylovSubspaceSolveInfo{TVec, T}.EigenvaluesComplex"/> and <see cref="KrylovSubspaceSolveInfo{TVec, T}.EigenvectorsImag"/> are not used</remarks>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentException">If <paramref name="info"/> contains invalid value</exception>
		public static int RestartKrylovSubspaceEigen<TVec, T>(bool hermitian, ref KrylovSubspaceSolveInfo<TVec, T> info)
			where TVec : class, IKrylovVector<TVec, T>, new()
			where T : unmanaged
		{
			int result = 0;
			bool success = false;
			LinkedListNode<AbstractApi>? node = RecentAPIs.First;
			while (!success)
			{
				if (node is null)
					break;
				success = node.Value.RestartKrylovSubspaceEigen_(hermitian, ref info, out result);
				node = node?.Next;
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
			else
				throw new InvalidOperationException(Resources.Backend.NotAvailable);
			return result;
		}

		/// <summary>
		/// Perform a restart Krylov subspace algorithm (typically the MinRes or PCG or GMRES algorithm) to linear solve a hermitian-definite (or a hermitian or non-hermitian) matrix.
		/// </summary>
		/// <typeparam name="T">Any float-point type unmanaged struct as the data type</typeparam>
		/// <typeparam name="TVec">The concrete vector class type hat implements <see cref="IKrylovVector{TVec, T}"/></typeparam>
		/// <param name="hermitianOrDefinite">Whether the given matrix is hermitian (true) or hermitian-definite (false) or a general square matrix (null)</param>
		/// <param name="info">The <see cref="KrylovSubspaceSolveInfo{TVec, T}"/> used as input information and output container</param>
		/// <returns>The relative error of the output solution and the approximate solution as a <typeparamref name="TVec"/></returns>
		/// <remarks><paramref name="info"/>'s eigen related fields are all not used.</remarks>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentException">If <paramref name="info"/> contains invalid value</exception>
		public static (double relativeError, TVec solve) RestartKrylovSubspaceLinearSolve<TVec, T>(bool? hermitianOrDefinite, ref KrylovSubspaceSolveInfo<TVec, T> info)
			where TVec : class, IKrylovVector<TVec, T>, new()
			where T : unmanaged
		{
			double error = 0;
			TVec? solve = null;
			bool success = false;
			LinkedListNode<AbstractApi>? node = RecentAPIs.First;
			while (!success)
			{
				if (node is null)
					break;
				success = node.Value.RestartKrylovSubspaceLinearSolve_(hermitianOrDefinite, ref info, out error, out solve);
				node = node?.Next;
			}
			if (success && node is not null && solve is not null)
				SetImplementation(RecentAPIs, node.Value);
			else
				throw new InvalidOperationException(Resources.Backend.NotAvailable);
			return (error, solve);
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
		/// When implemented by a derived class, compute the product of the Kronecker Multiply or Sum of <paramref name="leftMatrix"/> and <paramref name="rightMatrix"/> and <paramref name="vector"/>:<br/>
		/// <c><paramref name="vector"/> = <paramref name="scalar"/> * (<paramref name="leftMatrix"/> op <paramref name="rightMatrix"/>) * <paramref name="vector"/> + <paramref name="scalarVector"/> * <paramref name="vector"/></c> where '<c>op</c>' is '⨁' if <paramref name="multiply"/> is false or '⨂' otherwise.
		/// </summary>
		/// <typeparam name="TMat">The concrete matrix type as a <see cref="IMultipliableMatrix{TMat, TVec, T}"/></typeparam>
		/// <typeparam name="TVec">The concrete vector type as a <see cref="IConvertibleVector{TVec, TMat, T}"/></typeparam>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="multiply">Whether to perform Kronecker Multiply or Kronecker Sum</param>
		/// <param name="scalar">The scalar to multiply to the multiplication result</param>
		/// <param name="leftMatrix">The input left matrix to perform the Kronecker multiply/sum</param>
		/// <param name="rightMatrix">The input right matrix to perform the Kronecker multiply/sum</param>
		/// <param name="vector">The input / output vector</param>
		/// <param name="scalarVector">The scalar to multiply to the <paramref name="vector"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="leftMatrix"/> or <paramref name="rightMatrix"/> or <paramref name="vector"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If the sizes mismatch</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="scalar"/> is 0</exception>
		protected abstract bool KroneckerMultiplyVector_<TMat, TVec, T>(bool multiply, T scalar, TMat leftMatrix, TMat rightMatrix, ref TVec vector, T scalarVector = default)
			where TMat : class, IMultipliableMatrix<TMat, TVec, T>, IDisposable, new()
			where TVec : class, IConvertibleVector<TVec, TMat, T>, IDisposable, new()
			where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, perform a naïve Krylov subspace algorithm (typically the naïve Lanczos algorithm) to calculate the lowest eigenvalue and eigenvector of a hermitian matrix.
		/// </summary>
		/// <typeparam name="T">Any float-point type unmanaged struct as the data type</typeparam>
		/// <typeparam name="TVec">The concrete vector class type hat implements <see cref="IKrylovVector{TVec, T}"/></typeparam>
		/// <param name="info">The <see cref="KrylovSubspaceSolveInfo{TVec, T}"/> used as input information and output container</param>
		/// <param name="eigenvalue">The output approximate lowest eigenvalue</param>
		/// <param name="eigenvector">The output approximate lowest eigenvector</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <remarks>Only <paramref name="info"/>'s <see cref="KrylovSubspaceSolveInfo{TVec, T}.MatrixFunction"/>, <see cref="KrylovSubspaceSolveInfo{TVec, T}.InitialVector"/> and <see cref="KrylovSubspaceSolveInfo{TVec, T}.MaxRestarts"/> are used as inputs. Its <see cref="KrylovSubspaceSolveInfo{TVec, T}.OtherVector"/> is used as the output eigenvector.</remarks>
		/// <exception cref="ArgumentException">If <paramref name="info"/> contains invalid value</exception>
		protected abstract bool NaiveKrylovSubspaceEigenHermitain_<TVec, T>(ref KrylovSubspaceSolveInfo<TVec, T> info, out double eigenvalue, out TVec eigenvector) where TVec : class, IKrylovVector<TVec, T>, new() where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, perform a restart Krylov subspace algorithm (typically the Lanczos or the Krylov-Schur algorithm) to solve a hermitian (or a non-hermitian) matrix's lowest several eigenvalues and eigenvectors.
		/// </summary>
		/// <typeparam name="T">Any float-point type unmanaged struct as the data type</typeparam>
		/// <typeparam name="TVec">The concrete vector class type hat implements <see cref="IKrylovVector{TVec, T}"/></typeparam>
		/// <param name="hermitian">Whether the given matrix is a hermitian one or a general square matrix</param>
		/// <param name="info">The <see cref="KrylovSubspaceSolveInfo{TVec, T}"/> used as input information and output container</param>
		/// <param name="converged">Output the number of converged eigen-pairs</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <remarks><paramref name="info"/>'s <see cref="KrylovSubspaceSolveInfo{TVec, T}.WhichEigenvaluesDesired"/>, <see cref="KrylovSubspaceSolveInfo{TVec, T}.EigenvaluesComplex"/> and <see cref="KrylovSubspaceSolveInfo{TVec, T}.EigenvectorsImag"/> are not used</remarks>
		/// <exception cref="ArgumentException">If <paramref name="info"/> contains invalid value</exception>
		protected abstract bool RestartKrylovSubspaceEigen_<TVec, T>(bool hermitian, ref KrylovSubspaceSolveInfo<TVec, T> info, out int converged)
			where TVec : class, IKrylovVector<TVec, T>, new()
			where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, perform a restart Krylov subspace algorithm (typically the GMRES algorithm) to linear solve a hermitian-definite (or a hermitian or non-hermitian) matrix.
		/// </summary>
		/// <typeparam name="T">Any float-point type unmanaged struct as the data type</typeparam>
		/// <typeparam name="TVec">The concrete vector class type hat implements <see cref="IKrylovVector{TVec, T}"/></typeparam>
		/// <param name="hermitianOrDefinite">Whether the given matrix is hermitian (true) or hermitian-definite (false) or a general square matrix (null)</param>
		/// <param name="info">The <see cref="KrylovSubspaceSolveInfo{TVec, T}"/> used as input information and output container</param>
		/// <param name="relativeError">Output the relative error of the output</param>
		/// <param name="solve">Output the solve vector as a <typeparamref name="TVec"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <remarks><paramref name="info"/>'s eigen related fields are all not used and the <see cref="KrylovSubspaceSolveInfo{TVec, T}.OtherVector"/> is used as the output solve.</remarks>
		/// <exception cref="ArgumentException">If <paramref name="info"/> contains invalid value</exception>
		protected abstract bool RestartKrylovSubspaceLinearSolve_<TVec, T>(bool? hermitianOrDefinite, ref KrylovSubspaceSolveInfo<TVec, T> info, out double relativeError, out TVec solve)
			where TVec : class, IKrylovVector<TVec, T>, new()
			where T : unmanaged;
		#endregion
	}
}