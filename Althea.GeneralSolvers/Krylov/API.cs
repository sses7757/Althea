using System;

using Althea.SourceGenerator;
using Althea.Numerics;


namespace Althea.GeneralSolvers.Krylov
{
	/// <summary>
	/// The abstract interface for runtime general Krylov subspace matrix solver API routines 
	/// </summary>
	[AbstractRuntimeApi]
	public interface IAbstractApi : IAbstractRuntimeApi<IAbstractApi>
	{
		/// <summary>
		/// When implemented by a derived class, perform a naïve Krylov subspace algorithm (typically the naïve Lanczos algorithm) to calculate the lowest eigenvalue and eigenvector of a hermitian matrix.
		/// </summary>
		/// <typeparam name="T">Any float-point type unmanaged number as the data type</typeparam>
		/// <typeparam name="TVec">The concrete vector class type hat implements <see cref="IKrylovVector{T, TVec}"/></typeparam>
		/// <param name="info">The <see cref="KrylovSubspaceSolveInfo{T, TVec}"/> used as input information and output container</param>
		/// <param name="eigen">The output approximate lowest eigenvalue and corresponding eigenvector</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <remarks>Only <paramref name="info"/>'s <see cref="KrylovSubspaceSolveInfo{T, TVec}.MatrixFunction"/>, <see cref="KrylovSubspaceSolveInfo{T, TVec}.InitialVector"/> and <see cref="KrylovSubspaceSolveInfo{T, TVec}.MaxRestarts"/> are used as inputs. Its <see cref="KrylovSubspaceSolveInfo{T, TVec}.OtherVector"/> is used as the output eigenvector.</remarks>
		/// <exception cref="ArgumentException">If <paramref name="info"/> contains invalid value</exception>
		[AbstractApiMethod]
		public abstract bool NaiveKrylovSubspaceEigenHermitain<T, TVec>(ref KrylovSubspaceSolveInfo<T, TVec> info, out (double Value, TVec Vector) eigen) where T : unmanaged, IBinaryFloat<T> where TVec : class, IKrylovVector<T, TVec>;

		/// <summary>
		/// When implemented by a derived class, perform a restart Krylov subspace algorithm (typically the Lanczos or the Krylov-Schur algorithm) to solve a hermitian (or a non-hermitian) matrix's lowest several eigenvalues and eigenvectors.
		/// </summary>
		/// <typeparam name="T">Any float-point type unmanaged number as the data type</typeparam>
		/// <typeparam name="TVec">The concrete vector class type hat implements <see cref="IKrylovVector{T, TVec}"/></typeparam>
		/// <param name="hermitian">Whether the given matrix is a hermitian one or a general square matrix</param>
		/// <param name="info">The <see cref="KrylovSubspaceSolveInfo{T, TVec}"/> used as input information and output container</param>
		/// <param name="converged">Output the number of converged eigen-pairs</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <remarks><paramref name="info"/>'s <see cref="KrylovSubspaceSolveInfo{T, TVec}.WhichEigenvaluesDesired"/>, <see cref="KrylovSubspaceSolveInfo{T, TVec}.EigenvaluesImag"/> are not used</remarks>
		/// <exception cref="ArgumentException">If <paramref name="info"/> contains invalid value</exception>
		[AbstractApiMethod]
		public abstract bool RestartKrylovSubspaceEigen<T, TVec>(bool hermitian, ref KrylovSubspaceSolveInfo<T, TVec> info, out int converged) where T : unmanaged, IBinaryFloat<T> where TVec : class, IKrylovVector<T, TVec>;

		/// <summary>
		/// When implemented by a derived class, perform a restart Krylov subspace algorithm (typically the GMRES algorithm) to linear solve a hermitian-definite (or a hermitian or non-hermitian) matrix.
		/// </summary>
		/// <typeparam name="T">Any float-point type unmanaged number as the data type</typeparam>
		/// <typeparam name="TVec">The concrete vector class type hat implements <see cref="IKrylovVector{T, TVec}"/></typeparam>
		/// <param name="hermitianOrDefinite">Whether the given matrix is hermitian (true) or hermitian-definite (false) or a general square matrix (null)</param>
		/// <param name="info">The <see cref="KrylovSubspaceSolveInfo{T, TVec}"/> used as input information and output container</param>
		/// <param name="solve">Output the solve vector and the corresponding relative error</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <remarks><paramref name="info"/>'s eigen related fields are all not used and the <see cref="KrylovSubspaceSolveInfo{T, TVec}.OtherVector"/> is used as the output solve.</remarks>
		/// <exception cref="ArgumentException">If <paramref name="info"/> contains invalid value</exception>
		[AbstractApiMethod]
		public abstract bool RestartKrylovSubspaceLinearSolve<T, TVec>(bool? hermitianOrDefinite, ref KrylovSubspaceSolveInfo<T, TVec> info, out (TVec Vector, double RelativeError) solve) where T : unmanaged, IBinaryFloat<T> where TVec : class, IKrylovVector<T, TVec>;
	}
}