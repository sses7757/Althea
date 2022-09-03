using System;

using Althea.LinearAlgebra;
using Althea.Numerics;


namespace Althea.GeneralSolvers.Kronecker.Backend;

/// <summary>
/// The C# back-end of <see cref="IAbstractApi"/> that utilizes other APIs and thus has no specific supporting storage locations
/// </summary>
public class Api : IAbstractApi
{
	void IDisposable.Dispose()
	{
		this.Disposed = true;
		GC.SuppressFinalize(this);
	}

	/// <inheritdoc/>
	public bool Disposed { get; set; } = false;

	/// <summary>
	/// Get the default <see cref="Api"/>.
	/// </summary>
	internal protected static readonly Api Default = new();

	/// <inheritdoc/>
	public dynamic Properties { get; } = new IAbstractApi.NoDynamicProperties();

	/// <inheritdoc/>
	public virtual bool KroneckerMultiplyVector<T, TMat, TVec>(bool multiply, T scalar, TMat leftMatrix, TMat rightMatrix, ref TVec vector, T scalarVector = default) where T : unmanaged, IBaseNumber<T> where TMat : class, IConvertibleMatrix<T, TMat, TVec> where TVec : class, IConvertibleVector<T, TVec, TMat>
	{
		if (scalar == T.Zero)
			throw new ArgumentOutOfRangeException(nameof(scalar), scalar, Resources.ParameterError.CannotZero);
		if (multiply && (leftMatrix.NRows != leftMatrix.NCols ||
						rightMatrix.NRows != rightMatrix.NCols ||
						vector.Length != leftMatrix.NRows * rightMatrix.NRows))
			throw new ArgumentException(Resources.ParameterError.WrongSize);
		if (!multiply && (vector.Length != leftMatrix.NCols * rightMatrix.NCols))
			throw new ArgumentException(Resources.ParameterError.WrongSize);
		if (scalarVector != T.Zero && vector.Length != leftMatrix.NRows * rightMatrix.NRows)
			throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(scalarVector));

		using var V = vector.ToMatrix(rightMatrix.NRows);
		using var V_At = TMat.MultiplyMatries(V, leftMatrix, T.One, default, MatrixOperation.Transpose);
		if (multiply)
		{
			if (vector.Length == leftMatrix.NRows * rightMatrix.NRows)
			{
				TMat.MultiplyMatries(rightMatrix, V_At, scalar, scalarVector, V);
				vector = V.ToVector();
			}
			else
			{
				using var B_V_At = TMat.MultiplyMatries(rightMatrix, V_At, scalar);
				vector = B_V_At.ToVector();
			}
		}
		else
		{
			TMat.MultiplyMatries(rightMatrix, V, T.One, T.One, V_At);
			TMat.AddMatrices(V, scalar, V_At, scalarVector, V_At);
			vector = V_At.ToVector();
		}
		return true;
	}
}
