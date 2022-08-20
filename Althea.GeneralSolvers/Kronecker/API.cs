using System;

using Althea.SourceGenerator;
using Althea.Numerics;


namespace Althea.GeneralSolvers.Kronecker
{
	/// <summary>
	/// The abstract interface for runtime Kronecker multiply related API routines 
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
		/// <param name="multiply">Whether to perform Kronecker multiply or Kronecker sum</param>
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
		public abstract bool KroneckerMultiplyVector<T, TMat, TVec>(bool multiply, T scalar, TMat leftMatrix, TMat rightMatrix, ref TVec vector, T scalarVector = default) where T : unmanaged, IBaseNumber<T> where TMat : class, IConvertibleMatrix<T, TMat, TVec> where TVec : class, IConvertibleVector<T, TVec, TMat>;
	}
}