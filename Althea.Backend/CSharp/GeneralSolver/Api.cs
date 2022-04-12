using System.Runtime.CompilerServices;

using Althea.GeneralSolver;
using Althea.Helpers;
using Althea.LinearAlgebra;
using Althea.Linq;
using Althea.NativeTypes;


namespace Althea.Backend.CSharp.Solver
{
	/// <summary>
	/// The C# back-end of <see cref="IAbstractApi"/> that utilizes other APIs and thus has no specific supporting storage locations
	/// </summary>
	public class Api : IAbstractApi
	{
		#region basic
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

		/// <summary>
		/// Get or set the interval between two <see cref="Log.Write(string, string?, LogLevel)"/> with <see cref="LogLevel.Information"/> when using Krylov subspace algorithms
		/// </summary>
		public TimeSpan InfoLogInterval { get; set; } = TimeSpan.FromSeconds(15);

		/// <summary>
		/// Get or set the maximum number of stagnation steps allowed for Krylov subspace linear system solvers.
		/// </summary>
		public int MaxStagnationSteps { get; set; } = 3;
		#endregion

		#region Kronecker
		/// <inheritdoc/>
		public virtual bool KroneckerMultiplyVector<T, TMat, TVec>(bool multiply, T scalar, TMat leftMatrix, TMat rightMatrix, ref TVec vector, T scalarVector = default) where T : unmanaged, INumber<T> where TMat : class, IConvertibleMatrix<T, TMat, TVec> where TVec : class, IConvertibleVector<T, TVec, TMat>
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
		#endregion

		#region eigen
		/// <inheritdoc/>
		public virtual bool NaiveKrylovSubspaceEigenHermitain<T, TVec>(ref KrylovSubspaceSolveInfo<T, TVec> info, out (double Value, TVec Vector) eigen) where T : unmanaged, IFloatingPoint<T> where TVec : class, IKrylovVector<T, TVec>
		{
			eigen = (0, TVec.Empty);
			eigen = LanczosBased.NaiveLanczos<T, TVec>(info.MatrixFunction, info.InitialVector, info.MaxRestarts, info.CheckMatrixFunction, this.InfoLogInterval);
			return true;
		}

		/// <inheritdoc/>
		public virtual bool RestartKrylovSubspaceEigen<T, TVec>(bool hermitian, ref KrylovSubspaceSolveInfo<T, TVec> info, out int converged) where T : unmanaged, IFloatingPoint<T> where TVec : class, IKrylovVector<T, TVec>
		{
			converged = 0;
			int iter = info.IterationsPerRestart == 0 ? Common.HERM_MAX_ITER : info.IterationsPerRestart;
			if (hermitian)
			{
				var ret = LanczosBased.RestartLanczos(info.MatrixFunction, info.InitialVector, info.MaxRestarts, iter, info.Tolerance, info.ReorthogonalizeMethod, info.UseGapEstimation, info.PreserveSelector, info.CheckMatrixFunction, this.InfoLogInterval, info.Eigenvalues, info.Eigenvectors);
				if (!ret.HasValue)
					return false;
				else
					converged = ret.Value;
			}
			else
			{
				var ret = KrylovBased.KrylovSchur(info.MatrixFunction, info.InitialVector, info.WhichEigenvaluesDesired, info.MaxRestarts, iter, info.Tolerance, info.ReorthogonalizeMethod, info.UseGapEstimation, info.PreserveSelector, info.CheckMatrixFunction, this.InfoLogInterval, info.Eigenvalues, info.EigenvaluesImag, info.Eigenvectors);
				if (!ret.HasValue)
					return false;
				else
					converged = ret.Value;
			}
			return true;
		}
		#endregion

		#region linear solve
		/// <inheritdoc/>
		public virtual bool RestartKrylovSubspaceLinearSolve<T, TVec>(bool? hermitianOrDefinite, ref KrylovSubspaceSolveInfo<T, TVec> info, out (TVec Vector, double RelativeError) solve) where T : unmanaged, IFloatingPoint<T> where TVec : class, IKrylovVector<T, TVec>
		{
			if (info.OtherVector is null)
				throw new ArgumentNullException(nameof(info), nameof(info.OtherVector));
			solve = (TVec.Empty, 1);
			if (!hermitianOrDefinite.HasValue)
			{
				var success = KrylovBased.GeneralMinimalResidual<T, TVec>(info.MatrixFunction, info.OtherVector, info.InitialVector, info.MaxRestarts, info.IterationsPerRestart, info.Tolerance, info.ReorthogonalizeMethod, info.CheckMatrixFunction, this.InfoLogInterval, this.MaxStagnationSteps, out solve.Vector, out solve.RelativeError);
				return success;
			}
			else if (hermitianOrDefinite.Value)
			{
				solve = LanczosBased.MinimalResidual<T, TVec>(info.MatrixFunction, info.PreconditionMatrixFunction, info.InitialVector, info.OtherVector, info.MaxRestarts, info.Tolerance, info.CheckMatrixFunction, this.InfoLogInterval, this.MaxStagnationSteps);
			}
			else
			{
				solve = LanczosBased.ConjugateGradient<T, TVec>(info.MatrixFunction, info.PreconditionMatrixFunction, info.InitialVector, info.OtherVector, info.MaxRestarts, info.Tolerance, info.CheckMatrixFunction, this.InfoLogInterval, this.MaxStagnationSteps);
			}
			return true;
		}
		#endregion
	}


	internal static class Common
	{
		#region from T to real
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool ToRealCheck<T>(this T value, out T real) where T : unmanaged, IFloatingPoint<T>
		{
			if (NumberType<T>.IsComplex)
			{
				T conj = value.Conjugate();
				real = (value + conj) / (T.One + T.One);
				double re = real.As<T, double>();
				double im = ((value - conj) / (T.One + T.One)).As<T, double>();
				// check whether the imaginary is small enough
				return Math.Abs(re / im) <= Math.Sqrt(NumberType<T>.MachinePrecision);
			}
			else
			{
				real = value;
				return T.IsFinite(real);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static T ToRealCheck<T>(this T value) where T : unmanaged, IFloatingPoint<T>
		{
			if (NumberType<T>.IsComplex)
			{
				T conj = value.Conjugate();
				T real = (value + conj) / (T.One + T.One);
				double re = real.As<T, double>();
				double im = ((value - conj) / (T.One + T.One)).As<T, double>();
				// check whether the imaginary is small enough
				if (Math.Abs(re / im) > Math.Sqrt(NumberType<T>.MachinePrecision))
					throw new ArithmeticException(string.Format(Resource.GenericNotNormalReal, value));
				return real;
			}
			else
			{
				if (!T.IsFinite(value))
					throw new ArithmeticException(string.Format(Resource.GenericNotNormalReal, value));
				return value;
			}
		}
		#endregion

		#region parameters check
		internal const int HERM_MAX_ITER = 35, NON_HERM_MAX_ITER = 25, MAX_EIGS = 6;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static SpanList<T> ClearList<T>(this SpanList<T> list) where T : IDisposable
		{
			foreach (var item in list)
			{
				item?.Dispose();
			}
			return list;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void CheckParas<T, TVec>(Func<TVec, TVec> matrixFunction, TVec initial, int smallestK, ref int maxIter, bool herm) where T : unmanaged, IFloatingPoint<T> where TVec : class, IKrylovVector<T, TVec>
		{
			// check MatrixFunction
			if (matrixFunction is null)
				throw new ArgumentNullException(nameof(matrixFunction));
			try
			{
				// test matrix apply
				using var testOutput = matrixFunction.Invoke(initial);
				if (testOutput.Length != initial.Length)
					throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(matrixFunction));
				// test add
				testOutput.AddBy(initial, T.One);
			}
			catch (Exception e)
			{
				if (e is ArgumentException ee && ee.ParamName == nameof(matrixFunction))
					throw;
				else
					throw new System.Reflection.TargetInvocationException(e);
			}

			// check smallest k
			if (smallestK <= 0 || smallestK > initial.Length)
				throw new ArgumentOutOfRangeException(nameof(smallestK));
			int sqrtSize = Convert.ToInt32(Math.Sqrt(initial.Length));
			if (smallestK >= sqrtSize)
			{
				Log.Write(string.Format(Resource.TooMuchEigenvaluesRequired, smallestK), level: LogLevel.Warning);
			}

			// estimate iteration number
			int estimateIter = Math.Min(maxIter <= 0 ? int.MaxValue : maxIter, sqrtSize);
			if (herm)
				estimateIter = Math.Min(estimateIter, HERM_MAX_ITER);
			else
				estimateIter = Math.Min(estimateIter, NON_HERM_MAX_ITER);
			if (maxIter <= 0)
				maxIter = estimateIter;
		}
		#endregion

		#region gap
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static T GetGap<T>(T beta, T tol, ReadOnlySpan<T> vals, ReadOnlySpan<T> vecsLastRow, int target = 0, ReadOnlySpan<int> conjugatePairs = default, T normA = default) where T : unmanaged, IFloatingPoint<T>
		{
			if (normA == T.Zero)
				normA = vals.Max(static v => T.Abs(v));
			T normTol = (T.One + T.One) * normA * T.Sqrt(tol); // 2 for error upper bound, 1 for average
			var targetVal = vals[target];
			var targetSji = vecsLastRow[target];
			var targetEta = T.Abs(targetSji) * beta;
			T gap = T.NaN;
			for (int i = 0; i < vals.Length; i++)
			{
				if (!conjugatePairs.IsEmpty && conjugatePairs[i] == conjugatePairs[target] && conjugatePairs[i] != 0)
					continue;
				var g = T.Abs(vals[i] - targetVal);
				if (g < gap && g > normTol)
				{
					//tex:$\eta_i = \left\| \left(A-\vartheta_i^{\left(j\right)}I\right){\vec{y}}_i^{\left(j\right)} \right\| / \|{\vec{y}}_i^{\left(j\right)}\|=\beta_j\left|s_{j,i}^{\left(j\right)}\right|$
					//tex:$|\lambda_i|$ must lies within $\left[|\vartheta_i|-\eta_i,|\vartheta_i|+\eta_i\right]$
					var eta = T.Abs(vecsLastRow[i]) * beta;
					if (g > eta + targetEta) // no overlap, this is a candidate gap
					{
						gap = g;
					}
				}
			}
			// set to the smallest absolute value if cannot be found
			if (T.IsNaN(gap))
				gap = vals.Min(e => T.Max(normTol, T.Abs(e)));
			return gap;
		}
		#endregion

		#region orthogonalization
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void RobustOrthogonalize<T, TVec>(TVec r, ReadOnlySpan<TVec> qs, Span<T> weights, bool robust = true) where T : unmanaged, IFloatingPoint<T> where TVec : class, IKrylovVector<T, TVec>
		{
			if (qs.IsEmpty)
				return;
			int len = qs.Length;
			for (int i = len - 1; i >= 0; i--)
			{
				var q = qs[i];
				weights[i] = q.Dot(r);
				r.AddBy(q, -weights[i]);
			}
			if (!robust || len <= 4)
				return;

			// one more time will be enough in most cases
			for (int i = len - 1; i >= 0; i--)
			{
				var q = qs[i];
				var dot = q.Dot(r);
				if (dot == T.Zero)
					continue;
				weights[i] = weights[i] + dot;
				r.AddBy(q, -dot);
			}
			return;
		}
		#endregion

		#region linear solve helper
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static TVec RSetToBSubAx<T, TVec>(Func<TVec, TVec> A, TVec x, TVec b)
			where TVec : class, IKrylovVector<T, TVec>
			where T : unmanaged, IFloatingPoint<T>
		{
			TVec r = A.Invoke(x);
			try
			{
				r.Scale(-T.One);
				r.AddBy(b, T.One);
				return r;
			}
			catch (Exception)
			{
				r?.Dispose();
				throw;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void RSetToBSubAx<T, TVec>(Func<TVec, TVec> A, ref TVec r, TVec x, TVec b)
			where TVec : class, IKrylovVector<T, TVec>
			where T : unmanaged, IFloatingPoint<T>
		{
			r?.Dispose();
			r = A.Invoke(x);
			try
			{
				r.Scale(-T.One);
				r.AddBy(b, T.One);
			}
			catch (Exception)
			{
				r?.Dispose();
				throw;
			}
		}
		#endregion
	}
}