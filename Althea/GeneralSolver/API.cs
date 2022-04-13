using Althea.SourceGenerator;


namespace Althea.GeneralSolver
{
	/// <summary>
	/// The abstract interface for runtime general solver API routines 
	/// </summary>
	[AbstractRuntimeApi]
	public interface IAbstractApi : IAbstractRuntimeApi<IAbstractApi>
	{
		// Ignore Spelling: vec \oplus \otimes
		//tex:
		//Facts about Kronecker sum  times vector:
		//$$(A\oplus B)vec(X)\equiv(A\otimes I+I\otimes B)vec(X)=vec(XA^T+BX)$$
		//Facts about Kronecker product times vector:
		//$$(A\otimes B)vec(X)=vec(BXA^T) \text{ (notice that it is not } A^\dagger\text)$$

		/// <summary>
		/// When implemented by a derived class, compute the product of the Kronecker Multiply or Sum of <paramref name="leftMatrix"/> and <paramref name="rightMatrix"/> and <paramref name="vector"/>:<br/>
		/// <c><paramref name="vector"/> = <paramref name="scalar"/> * (<paramref name="leftMatrix"/> op <paramref name="rightMatrix"/>) * <paramref name="vector"/> + <paramref name="scalarVector"/> * <paramref name="vector"/></c> where '<c>op</c>' is '⨁' if <paramref name="multiply"/> is false or '⨂' otherwise.
		/// </summary>
		/// <typeparam name="TMat">The concrete matrix type as a <see cref="IConvertibleMatrix{T, TMat, TVec}"/></typeparam>
		/// <typeparam name="TVec">The concrete vector type as a <see cref="IConvertibleVector{T, TVec, TMat}"/></typeparam>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
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
		[AbstractApiMethod]
		public abstract bool KroneckerMultiplyVector<T, TMat, TVec>(bool multiply, T scalar, TMat leftMatrix, TMat rightMatrix, ref TVec vector, T scalarVector = default) where T : unmanaged, INumber<T> where TMat : class, IConvertibleMatrix<T, TMat, TVec> where TVec : class, IConvertibleVector<T, TVec, TMat>;

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
		public abstract bool NaiveKrylovSubspaceEigenHermitain<T, TVec>(ref KrylovSubspaceSolveInfo<T, TVec> info, out (double Value, TVec Vector) eigen) where T : unmanaged, IFloatingPoint<T> where TVec : class, IKrylovVector<T, TVec>;

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
		public abstract bool RestartKrylovSubspaceEigen<T, TVec>(bool hermitian, ref KrylovSubspaceSolveInfo<T, TVec> info, out int converged) where T : unmanaged, IFloatingPoint<T> where TVec : class, IKrylovVector<T, TVec>;

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
		public abstract bool RestartKrylovSubspaceLinearSolve<T, TVec>(bool? hermitianOrDefinite, ref KrylovSubspaceSolveInfo<T, TVec> info, out (TVec Vector, double RelativeError) solve) where T : unmanaged, IFloatingPoint<T> where TVec : class, IKrylovVector<T, TVec>;
	}
}