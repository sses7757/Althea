using System;

using Althea.Array;
using Althea.LinearAlgebra;

namespace Althea.GeneralSolvers.Kronecker
{
	/// <summary>
	/// The interface of matrices that can be converted to vectors.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TMat">The concrete matrix type</typeparam>
	/// <typeparam name="TVec">The concrete vector type</typeparam>
	public interface IConvertibleMatrix<T, TMat, TVec> : IMatrixMetric, IDisposable,
		IMatrixAddOperations<T, TMat, TMat, TMat>, IMatrixMultiplyOperations<T, TMat, TMat, TMat>
		where TMat : class, IConvertibleMatrix<T, TMat, TVec>, IDisposable
		where TVec : class, IConvertibleVector<T, TVec, TMat>, IDisposable
		where T : unmanaged, INumber<T>
	{
		/// <summary>
		/// When implemented by a derived class, convert this matrix to a vector of type <typeparamref name="TVec"/>
		/// </summary>
		/// <returns>The converted vector as a <typeparamref name="TVec"/> that shall NOT contain any referenced storage.</returns>
		TVec ToVector();
	}

	/// <summary>
	/// The interface of vectors that can be converted to matrices.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TMat">The concrete matrix type</typeparam>
	/// <typeparam name="TVec">The concrete vector type</typeparam>
	public interface IConvertibleVector<T, TVec, TMat> : IVectorMetric, IDisposable
		where TMat : class, IConvertibleMatrix<T, TMat, TVec>, IDisposable
		where TVec : class, IConvertibleVector<T, TVec, TMat>, IDisposable
		where T : unmanaged, INumber<T>
	{
		/// <summary>
		/// When implemented by a derived class, convert this vector to a matrix of type <typeparamref name="TMat"/>
		/// </summary>
		/// <param name="rows">The number of rows of the target matrix</param>
		/// <returns>The converted matrix as a <typeparamref name="TMat"/> that shall NOT contain any referenced storage.</returns>
		TMat ToMatrix(long rows);
	}
}
