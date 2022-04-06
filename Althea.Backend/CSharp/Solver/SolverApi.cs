using System;
using System.Runtime.CompilerServices;

using Althea.Solver;
using Althea.NativeTypes;
using Althea.Helpers;
using Althea.LinearAlgebra;


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

		/// <summary>
		/// Get or set the interval between two <see cref="Log.Write(string, string?, LogLevel)"/> with <see cref="LogLevel.Information"/> when using Krylov subspace algorithms
		/// </summary>
		public TimeSpan InfoLogInterval { get; set; } = TimeSpan.FromSeconds(15);

		/// <summary>
		/// Get or set the maximum number of stagnation steps allowed for Krylov subspace linear system solvers.
		/// </summary>
		public int MaxStagnationSteps { get; set; } = 3;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool CheckT<T>() where T : unmanaged
		{
			return Const<T>.IsIntegralType;
		}
		#endregion

		#region Kronecker
		protected override bool KroneckerMultiplyVector_<TMat, TVec, T>(bool multiply, T scalar, TMat leftMatrix, TMat rightMatrix, ref TVec vector, T scalarVector = default)
		{
			return KroneckerMultiplyVector(multiply, scalar, leftMatrix, rightMatrix, ref vector, scalarVector);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected internal static new bool KroneckerMultiplyVector<TMat, TVec, T>(bool multiply, T scalar, TMat leftMatrix, TMat rightMatrix, ref TVec vector, T scalarVector = default)
			where TMat : class, IConvertibleMatrix<TMat, TVec, T>, IDisposable, new()
			where TVec : class, IConvertibleVector<TVec, TMat, T>, IDisposable, new()
			where T : unmanaged
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
				using var V_At = V.OutOfPlaceMultiply(leftMatrix, Const<T>.One, opRight: MatrixOperation.Transpose);
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
				using var V_At = V.OutOfPlaceMultiply(leftMatrix, Const<T>.One, opRight: MatrixOperation.Transpose);
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

		#region eigen
		protected override bool NaiveKrylovSubspaceEigenHermitain_<TVec, T>(ref KrylovSubspaceSolveInfo<TVec, T> info, out double eigenvalue, out TVec eigenvector)
		{
			return NaiveKrylovSubspaceEigenHermitain<TVec, T>(ref info, out eigenvalue, out eigenvector, this.InfoLogInterval);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static bool NaiveKrylovSubspaceEigenHermitain<TVec, T>(ref KrylovSubspaceSolveInfo<TVec, T> info, out double eigenvalue, out TVec eigenvector, TimeSpan interval) where TVec : class, IKrylovVector<TVec, T>, new() where T : unmanaged
		{
			(eigenvalue, eigenvector) = (0, new());
			if (CheckT<T>())
				return false;
			(eigenvalue, eigenvector) = LanczosBased.NaiveLanczos<TVec, T>(info.MatrixFunction, info.InitialVector, info.MaxRestarts, info.CheckMatrixFunction, interval);
			return true;
		}

		protected override bool RestartKrylovSubspaceEigen_<TVec, T>(bool hermitian, ref KrylovSubspaceSolveInfo<TVec, T> info, out int converged)
		{
			return RestartKrylovSubspaceEigen<TVec, T>(hermitian, ref info, out converged, this.InfoLogInterval);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static bool RestartKrylovSubspaceEigen<TVec, T>(bool hermitian, ref KrylovSubspaceSolveInfo<TVec, T> info, out int converged, TimeSpan interval) where TVec : class, IKrylovVector<TVec, T>, new() where T : unmanaged
		{
			converged = 0;
			if (CheckT<T>())
				return false;

			int iter = info.IterationsPerRestart == 0 ? Common.HERM_MAX_ITER : info.IterationsPerRestart;
			if (hermitian)
			{
				var eigvals = info.Eigenvalues[..info.NumberEigenvaluesDesired];
				var ret = LanczosBased.RestartLanczos<TVec, T>(info.MatrixFunction, info.InitialVector, info.MaxRestarts, iter, info.Tolerance, info.ReorthogonalizeMethod, info.UseGapEstimation, info.PreserveSelector, info.CheckMatrixFunction, eigvals, info.Eigenvectors, interval);
				if (!ret.HasValue)
					return false;
				else
					converged = ret.Value;
			}
			else
			{
				var eigvals = info.EigenvaluesComplex[..info.NumberEigenvaluesDesired];
				var ret = KrylovBased.KrylovSchur<TVec, T>(info.MatrixFunction, info.InitialVector, info.WhichEigenvaluesDesired, info.MaxRestarts, iter, info.Tolerance, info.ReorthogonalizeMethod, info.UseGapEstimation, info.PreserveSelector, info.CheckMatrixFunction, eigvals, info.Eigenvectors, info.EigenvectorsImag, interval);
				if (!ret.HasValue)
					return false;
				else
					converged = ret.Value;
			}
			return true;
		}
		#endregion

		#region linear solve
		protected override bool RestartKrylovSubspaceLinearSolve_<TVec, T>(bool? hermitianOrDefinite, ref KrylovSubspaceSolveInfo<TVec, T> info, out double relativeError, out TVec solve)
		{
			return RestartKrylovSubspaceLinearSolve(hermitianOrDefinite, ref info, out relativeError, out solve, this.InfoLogInterval, this.MaxStagnationSteps);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal protected static bool RestartKrylovSubspaceLinearSolve<TVec, T>(bool? hermitianOrDefinite, ref KrylovSubspaceSolveInfo<TVec, T> info, out double relativeError, out TVec solve, TimeSpan interval, int maxStagnations) where TVec : class, IKrylovVector<TVec, T>, new() where T : unmanaged
		{
			if (info.OtherVector is null)
				throw new ArgumentNullException(nameof(info), nameof(info.OtherVector));
			(relativeError, solve) = (1, new());
			if (CheckT<T>())
				return false;
			if (!hermitianOrDefinite.HasValue)
			{
				var success = KrylovBased.GeneralMinimalResidual<TVec, T>(info.MatrixFunction, info.OtherVector, info.InitialVector, info.MaxRestarts, info.IterationsPerRestart, info.Tolerance, info.ReorthogonalizeMethod, info.CheckMatrixFunction, out solve, out relativeError, interval, maxStagnations);
				return success;
			}
			else if (hermitianOrDefinite.Value)
			{
				(relativeError, solve) = LanczosBased.MinimalResidual<TVec, T>(info.MatrixFunction, info.PreconditionMatrixFunction, info.InitialVector, info.OtherVector, info.MaxRestarts, info.Tolerance, info.CheckMatrixFunction, interval, maxStagnations);
			}
			else
			{
				(relativeError, solve) = LanczosBased.ConjugateGradient<TVec, T>(info.MatrixFunction, info.PreconditionMatrixFunction, info.InitialVector, info.OtherVector, info.MaxRestarts, info.Tolerance, info.CheckMatrixFunction, interval, maxStagnations);
			}
			return true;
		}
		#endregion
	}
}
#pragma warning restore CS1591 // 缺少对公共可见类型或成员的 XML 注释