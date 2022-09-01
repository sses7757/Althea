using System;
using System.Dynamic;
using System.Runtime.CompilerServices;

using Althea.Helpers;
using Althea.Numerics;


namespace Althea.GeneralSolvers.Krylov.Backend;

/// <summary>
/// The C# back-end of <see cref="IAbstractApi"/> that utilizes other APIs and thus has no specific supporting storage locations
/// </summary>
public class Api : IAbstractApi
{
	#region basic
	/// <summary>
	/// The default constructor
	/// </summary>
	public Api()
	{
		this.Properties = new DynamicProperties(this);
	}

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

	#region dynamic
	/// <inheritdoc/>
	public dynamic Properties { get; }

	/// <inheritdoc/>
	protected sealed class DynamicProperties : IAbstractApi.DynamicProperties
	{
		internal DynamicProperties(Api @this) : base(@this) { }

		/// <inheritdoc/>
		public override bool TryGetMember(GetMemberBinder binder, out object? result)
		{
			if (binder.Name == nameof(InfoLogInterval) && binder.ReturnType == typeof(TimeSpan))
			{
				result = (this.api as Api)!.InfoLogInterval;
				return true;
			}
			if (binder.Name == nameof(MaxStagnationSteps) && binder.ReturnType == typeof(int))
			{
				result = (this.api as Api)!.MaxStagnationSteps;
				return true;
			}
			result = null;
			return false;
		}

		/// <inheritdoc/>
		public override bool TrySetMember(SetMemberBinder binder, object? value)
		{
			if (binder.Name == nameof(InfoLogInterval) && value is TimeSpan t)
			{
				(this.api as Api)!.InfoLogInterval = t;
				return true;
			}
			if (binder.Name == nameof(MaxStagnationSteps) && value is int i)
			{
				(this.api as Api)!.MaxStagnationSteps = i;
				return true;
			}
			return false;
		}
	}
	#endregion

	#region eigen
	/// <inheritdoc/>
	public virtual bool NaiveKrylovSubspaceEigenHermitain<T, TVec>(ref KrylovSubspaceSolveInfo<T, TVec> info, out (T Value, TVec Vector) eigen) where T : unmanaged, IBinaryFloat<T> where TVec : class, IKrylovVector<T, TVec>
	{
		eigen = (default, TVec.Empty);
		eigen = LanczosBased.NaiveLanczos<T, TVec>(info.MatrixFunction, info.InitialVector, info.MaxRestarts, info.CheckMatrixFunction, this.InfoLogInterval);
		return true;
	}

	/// <inheritdoc/>
	public virtual bool RestartKrylovSubspaceEigen<T, TVec>(bool hermitian, ref KrylovSubspaceSolveInfo<T, TVec> info, out int converged) where T : unmanaged, IBinaryFloat<T> where TVec : class, IKrylovVector<T, TVec>
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
	public virtual bool RestartKrylovSubspaceLinearSolve<T, TVec>(bool? hermitianOrDefinite, ref KrylovSubspaceSolveInfo<T, TVec> info, out (TVec Vector, double RelativeError) solve) where T : unmanaged, IBinaryFloat<T> where TVec : class, IKrylovVector<T, TVec>
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
	internal static bool ToRealCheck<T>(this T value, out T real) where T : unmanaged, IBinaryFloat<T>
	{
		if (T.IsComplexType)
		{
			T conj = T.Conjugate(value);
			real = (value + conj) / (T.One + T.One);
			T imag = (value - conj) / (T.One + T.One);
			// check whether the imaginary is small enough
			return T.Abs(real / imag) <= T.Sqrt(T.MachinePrecision);
		}
		else
		{
			real = value;
			return T.IsFinite(real);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static T ToRealCheck<T>(this T value) where T : unmanaged, IBinaryFloat<T>
	{
		if (T.IsComplexType)
		{
			T conj = T.Conjugate(value);
			T real = (value + conj) / (T.One + T.One);
			T imag = (value - conj) / (T.One + T.One);
			// check whether the imaginary is small enough
			if (T.Abs(real / imag) > T.Sqrt(T.MachinePrecision))
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
	internal static void CheckParas<T, TVec>(Func<TVec, TVec> matrixFunction, TVec initial, int smallestK, ref int maxIter, bool herm) where T : unmanaged, IBinaryFloat<T> where TVec : class, IKrylovVector<T, TVec>
	{
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
	// Ignore Spelling: \right \vec
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static T GetGap<T>(T beta, T tol, ReadOnlySpan<T> vals, ReadOnlySpan<T> vecsLastRow, int target = 0, T normA = default) where T : unmanaged, IBinaryFloat<T>
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
	internal static void RobustOrthogonalize<T, TVec>(TVec r, ReadOnlySpan<TVec> qs, Span<T> weights, bool robust = true) where T : unmanaged, IBinaryFloat<T> where TVec : class, IKrylovVector<T, TVec>
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
		where T : unmanaged, IBinaryFloat<T>
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
		where T : unmanaged, IBinaryFloat<T>
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

	#region sort eigenvalues

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static unsafe void SortPairs<T>(int n, WhichEigenvalues which, Span<T> orderedVals, Span<T> orderedValsImag, SpanMatrix<T> orderedVecs) where T : unmanaged, IBinaryFloat<T>
	{
		Span<T> reordered = stackalloc T[n];
		if (T.IsComplexType)
		{
			switch (which)
			{
				case WhichEigenvalues.LargestAbsolute:
					orderedVals.CopyTo(reordered, static v => -T.Abs(v));
					break;
				case WhichEigenvalues.LargestReal:
					orderedVals.CopyTo(reordered, static v => -v.ToReal());
					break;
				case WhichEigenvalues.LargestAbsoluteImaginary:
					orderedVals.CopyTo(reordered, static v => -T.Abs(v.ToImag()));
					break;
				case WhichEigenvalues.SmallestAbsolute:
					orderedVals.CopyTo(reordered, static v => T.Abs(v));
					break;
				case WhichEigenvalues.SmallestReal:
					orderedVals.CopyTo(reordered, static v => v.ToImag());
					break;
				case WhichEigenvalues.SmallestAbsoluteImaginary:
					orderedVals.CopyTo(reordered, static v => T.Abs(v.ToImag()));
					break;
				default:
					throw new NotSupportedException();
			}
			fixed (T* p = orderedVecs.UnderlyingSpan)
			{
				Span<SpanMatrix<T>.ColumnSwapping> columns = stackalloc SpanMatrix<T>.ColumnSwapping[n];
				columns = orderedVecs.AsColumnSwappings(new(p), columns);
				reordered.Sort(columns, orderedVals.AsSwappers());
			}
		}
		else
		{
			switch (which)
			{
				case WhichEigenvalues.LargestAbsolute:
					orderedVals.Zip<T, T, T>(orderedValsImag, reordered, static (re, im) => -new Complex<T>(re, im).MagnitudeSquared);
					break;
				case WhichEigenvalues.LargestReal:
					orderedVals.CopyTo(reordered, static v => -v);
					break;
				case WhichEigenvalues.LargestAbsoluteImaginary:
					orderedValsImag.CopyTo(reordered, static v => -T.Abs(v));
					break;
				case WhichEigenvalues.SmallestAbsolute:
					orderedVals.Zip<T, T, T>(orderedValsImag, reordered, static (re, im) => new Complex<T>(re, im).MagnitudeSquared);
					break;
				case WhichEigenvalues.SmallestReal:
					orderedVals.CopyTo(reordered);
					break;
				case WhichEigenvalues.SmallestAbsoluteImaginary:
					orderedValsImag.CopyTo(reordered, static v => T.Abs(v));
					break;
				default:
					throw new NotSupportedException();
			}
			fixed (T* p = orderedVecs.UnderlyingSpan)
			{
				Span<SpanMatrix<T>.ColumnSwapping> columns = stackalloc SpanMatrix<T>.ColumnSwapping[n];
				columns = orderedVecs.AsColumnSwappings(new(p), columns);
				reordered.Sort(columns, orderedVals.AsSwappers(), orderedValsImag.AsSwappers());
			}
		}
	}
	#endregion
}