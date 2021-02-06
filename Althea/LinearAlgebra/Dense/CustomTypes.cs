using System;


namespace Althea.LinearAlgebra.Dense
{
	/// <summary>
	/// The <see cref="MatrixFillMode"/> enum indicates which part (lower or upper) of a dense symmetric/hermitian matrix was filled and consequently should be used by the function.
	/// </summary>
	public enum MatrixFillMode
	{
		/// <summary>
		/// the lower part of the matrix is filled
		/// </summary>
		Lower = 0,
		/// <summary>
		/// the upper part of the matrix is filled
		/// </summary>
		Upper = 1
	}
}
