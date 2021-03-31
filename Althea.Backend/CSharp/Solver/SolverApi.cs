using System;

using Althea.Solver;
using Althea.NativeTypes;


#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释
namespace Althea.Backend.CSharp.Solver
{
	/// <summary>
	/// The C# back-end of <see cref="AbstractApi"/> that utilizes other APIs and thus has no specific supporting storage locations
	/// </summary>
	public class SolverApi : AbstractApi
	{
		#region basic
		/// <summary>
		/// The default constructor used by reflection
		/// </summary>
		public SolverApi()
		{
			// do nothing
		}

		protected override void Dispose(bool disposeManaged)
		{
			// do nothing
		}
		#endregion

		#region Kronecker
		protected override bool KroneckerMultiplyVector_<TMat, TVec, T>(bool multiply, T scalar, TMat leftMatrix, TMat rightMatrix, ref TVec vector, T scalarVector = default)
		{
			if (scalar.IsZero())
				throw new ArgumentOutOfRangeException(nameof(scalar), scalar, Resources.Parameter.CannotZero);
			if (multiply && (leftMatrix.NRows != leftMatrix.NCols ||
							rightMatrix.NRows != rightMatrix.NCols ||
							vector.Length != leftMatrix.NRows * rightMatrix.NRows))
				throw new ArgumentException(Resources.Parameter.WrongSize);
			if (!multiply && (vector.Length != leftMatrix.NCols * rightMatrix.NCols))
				throw new ArgumentException(Resources.Parameter.WrongSize);
			if (!scalarVector.IsZero() && vector.Length != leftMatrix.NRows * rightMatrix.NRows)
				throw new ArgumentException(Resources.Parameter.InvalidValue, nameof(scalarVector));

			using var V = vector.ToMatrix(rightMatrix.NRows);
			if (multiply)
			{
				using var V_At = V.OutOfPlaceMultiply(leftMatrix, Const<T>.One, opRight: LinearAlgebra.MatrixOperation.Transpose);
				if (vector.Length == leftMatrix.NRows * rightMatrix.NRows && V.CanOperateInPlace)
				{
					V.InPlaceFusedMultiplyAdd(rightMatrix, V_At, scalar, scalarVector);
					vector = V.ToVector();
				}
				else if (scalarVector.IsZero())
				{
					using var B_V_At = rightMatrix.OutOfPlaceMultiply(V_At, scalar);
					vector = B_V_At.ToVector();
				}
				else
				{
					using var B_V_At = V.OutOfPlaceFusedMultiplyAdd(rightMatrix, V_At, scalar, scalarVector);
					vector = B_V_At.ToVector();
				}
			}
			else
			{
				using var V_At = V.OutOfPlaceMultiply(leftMatrix, Const<T>.One, opRight: LinearAlgebra.MatrixOperation.Transpose);
				if (V_At.CanOperateInPlace)
				{
					V_At.InPlaceFusedMultiplyAdd(rightMatrix, V, Const<T>.One, Const<T>.One);
					V_At.InPlaceAdd(V, scalar, scalarVector);
					vector = V_At.ToVector();
				}
				else
				{
					using var V_At__B_V = V_At.OutOfPlaceFusedMultiplyAdd(rightMatrix, V, Const<T>.One, Const<T>.One);
					using var temp = V_At__B_V.OutOfPlaceAdd(V, scalar, scalarVector);
					vector = temp.ToVector();
				}
			}

			return true;
		}
		#endregion

		#region eigen hermitian
		protected override bool NaiveKrylovSubspaceEigenHermitain_<TVec, T>(ref KrylovSubspaceSolveInfo<TVec, T> info, out double eigenvalue) => throw new NotImplementedException();

		protected override bool RestartKrylovSubspaceEigenHermitian_<TVec, T>(ref KrylovSubspaceSolveInfo<TVec, T> info, out int converged) => throw new NotImplementedException();
		#endregion

		#region eigen non-hermitian
		protected override bool RestartKrylovSubspaceEigenGeneral_<TVec, T>(ref KrylovSubspaceSolveInfo<TVec, T> info, out int converged) => throw new NotImplementedException();
		#endregion

		#region linear solve
		protected override bool RestartKrylovSubspaceLinearSolve_<TVec, T>(bool hermitian, ref KrylovSubspaceSolveInfo<TVec, T> info, out double relativeError) => throw new NotImplementedException();
		#endregion
	}
}
#pragma warning restore CS1591 // 缺少对公共可见类型或成员的 XML 注释