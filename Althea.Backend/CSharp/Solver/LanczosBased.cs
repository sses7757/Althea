using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

using Althea.Backend.Storage;
using Althea.Helpers;
using Althea.LinearAlgebra;
using Althea.Linq;
using Althea.NativeTypes;
using Althea.Solver;

using LAD = Althea.LinearAlgebra.Dense.AbstractApi;


namespace Althea.Backend.CSharp.Solver
{
	internal static class Common
	{
		#region from T to double
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool ToDoubleCheck<T>(this T value, out double d) where T : unmanaged
		{
			if (Const<T>.IsComplex)
			{
				d = value.NativeRealPart();
				double im = value.NativeImagPart();
				// check whether the imaginary is small enough
				return Math.Abs(im / d) <= Const<T>.MachinePrecisionHalf;
			}
			else
			{
				d = value.ToDouble();
				return double.IsFinite(d);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static double ToDoubleCheck<T>(this T value) where T : unmanaged
		{
			if (Const<T>.IsComplex)
			{
				double re = value.NativeRealPart();
				double im = value.NativeImagPart();
				// check whether the imaginary is small enough (the absolute value equals to the real part in machine precision)
				if (Math.Abs(im / re) > Const<T>.MachinePrecisionHalf)
					throw new ArithmeticException(string.Format(Resource.GenericNotNormalReal, value));
				return re;
			}
			else
			{
				double d = value.ToDouble();
				if (!double.IsFinite(d))
					throw new ArithmeticException(string.Format(Resource.GenericNotNormalReal, value));
				return d;
			}
		}
		#endregion

		#region parameters check
		internal const int HERM_MAX_ITER = 35, NON_HERM_MAX_ITER = 25;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static SpanList<T> ClearList<T>(this SpanList<T> list) where T : IDisposable
		{
			list.Clear(static elem => elem?.Dispose());
			return list;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void ClearSpan<T>(this Span<T> span) where T : IDisposable
		{
			span.ForEach(static elem => elem?.Dispose());
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void CheckParas<TVec, T>(Func<TVec, TVec> matrixFunction, TVec initial, int smallestK, ref int maxIter, bool herm)
			where TVec : class, IKrylovVector<TVec, T>, new()
			where T : unmanaged
		{
			// check MatrixFunction
			if (matrixFunction is null)
				throw new ArgumentNullException(nameof(matrixFunction));
			try
			{
				// test matrix apply
				using var testOutput = matrixFunction.Invoke(initial);
				if (testOutput.Length != initial.Length)
					throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(matrixFunction));
				// test add
				testOutput.AddBy(initial, Const<T>.One);
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
		internal static double GetGap(double beta, double tol, ReadOnlySpan<ComplexDouble> vals, ReadOnlySpan<ComplexDouble> vecsLastRow, int target = 0, ReadOnlySpan<int> conjugatePairs = default, double normA = 0)
		{
			if (normA == 0)
				normA = vals.Max(static v => v.Abs());
			double normTol = 2 * normA * Math.Sqrt(tol); // 2 for error upper bound, 1 for average
			var targetVal = vals[target];
			var targetSji = vecsLastRow[target];
			var targetEta = targetSji.Abs() * beta;
			double gap = double.NaN;
			for (int i = 0; i < vals.Length; i++)
			{
				if (!conjugatePairs.IsEmpty && conjugatePairs[i] == conjugatePairs[target] && conjugatePairs[i] != 0)
					continue;
				var g = (vals[i] - targetVal).Abs();
				if (g < gap && g > normTol)
				{
					//tex:$\eta_i = \left\| \left(A-\vartheta_i^{\left(j\right)}I\right){\vec{y}}_i^{\left(j\right)} \right\| / \|{\vec{y}}_i^{\left(j\right)}\|=\beta_j\left|s_{j,i}^{\left(j\right)}\right|$
					//tex:$|\lambda_i|$ must lies within $\left[|\vartheta_i|-\eta_i,|\vartheta_i|+\eta_i\right]$
					var eta = vecsLastRow[i].Abs() * beta;
					if (g > eta + targetEta) // no overlap, this is a candidate gap
					{
						gap = g;
					}
				}
			}
			// set to the smallest absolute value if cannot be found
			if (double.IsNaN(gap))
				gap = vals.Min(e => Math.Max(normTol, e.Abs()));
			return gap;
		}
		#endregion

		#region orthogonalization
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void RobustOrthogonalize<TVec, T>(TVec r, ReadOnlySpan<TVec> qs, Span<T> weights, bool robust = true)
			where TVec : class, IKrylovVector<TVec, T>, new()
			where T : unmanaged
		{
			if (qs.IsEmpty)
				return;
			int len = qs.Length;
			for (int i = len - 1; i >= 0; i--)
			{
				var q = qs[i];
				weights[i] = q.Dot(r);
				r.AddBy(q, weights[i].NativeNegate());
			}
			if (!robust || len <= 4)
				return;

			// one more time will be enough in most cases
			for (int i = len - 1; i >= 0; i--)
			{
				var q = qs[i];
				var dot = q.Dot(r);
				if (dot.IsZero())
					continue;
				weights[i] = weights[i].NativeAdd(dot);
				r.AddBy(q, dot.NativeNegate());
			}
			return;
		}
		#endregion

		#region set delegate
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void SetDelegate<TApi, TDelegate>(this TApi? pre, TApi? now, string name, ref Delegate? @delegate) where TApi : AbstractApiSelector where TDelegate : Delegate
		{
			if (@delegate is not null)
				return;
			try
			{
				if (pre is not null && pre != now)
				{	// set implementation back
					typeof(TApi).GetMethod("SetImplementation", BindingFlags.Static | BindingFlags.NonPublic)?.Invoke(null, new object[] { pre.GetType() });
				}
				if (now is not null)
				{	// create delegate
					@delegate = now.GetType().GetMethod(name + "_", BindingFlags.NonPublic)?.CreateDelegate<TDelegate>();
					if (@delegate is null)
						throw new MethodAccessException();
				}
			}
			catch (Exception)
			{
				Log.Write(string.Format(Resource.CannotCreateDelegate, nameof(LAD) + "." + nameof(LAD.EigenSpecialMatrixHermitian)), level: LogLevel.Warning);
			}
		}
		#endregion

		#region linear solve helper
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static TVec RSetToBSubAx<TVec, T>(Func<TVec, TVec> A, TVec x, TVec b)
			where TVec : class, IKrylovVector<TVec, T>, new()
			where T : unmanaged
		{
			TVec r = A.Invoke(x);
			try
			{
				r.Scale(Const<T>.MinusOne);
				r.AddBy(b, Const<T>.One);
				return r;
			}
			catch (Exception)
			{
				r?.Dispose();
				throw;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void RSetToBSubAx<TVec, T>(Func<TVec, TVec> A, ref TVec r, TVec x, TVec b)
			where TVec : class, IKrylovVector<TVec, T>, new()
			where T : unmanaged
		{
			r?.Dispose();
			r = A.Invoke(x);
			try
			{
				r.Scale(Const<T>.MinusOne);
				r.AddBy(b, Const<T>.One);
			}
			catch (Exception)
			{
				r?.Dispose();
				throw;
			}
		}

		#endregion
	}

	internal static class LanczosBased
	{
		#region initialize Lanczos
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void LanczosInit<TVec, T>(Func<TVec, TVec> matrixFunction, ref TVec q0, out TVec r, out double α0, out double β0)
			where TVec : class, IKrylovVector<TVec, T>, new()
			where T : unmanaged
		{
			q0.Normalize();
			//tex: $\vec r = A \vec q$
			r = matrixFunction.Invoke(q0);
			//tex:$\alpha_0 = \vec q^* \vec r$
			T alpha = q0.Dot(r);
			α0 = alpha.ToDoubleCheck();
			//tex:$\vec r = \vec r - \alpha_0 \vec q_0$
			r.AddBy(q0, alpha.NativeNegate());
			//tex: $\beta_0=\|\vec r_0\|$
			β0 = r.Norm();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void LanczosInit<TVec, T>(Func<TVec, TVec> matrixFunction, double ψ, out TVec r, ref SpanList<TVec> qs, ref SpanList<double> αs, ref SpanList<double> βs, ref RestartBasicInfo<TVec, T> info)
			where TVec : class, IKrylovVector<TVec, T>, new()
			where T : unmanaged
		{
			// deal with restart Ritz vectors
			int NRitz = info.UnconvergedEigenvalues.Count;
			for (int i = 0; i < NRitz; i++)
			{
				qs.Add(info.UnconvergedEigenvectors[i]);
				αs.Add(info.UnconvergedEigenvalues[i]);
				βs.Add(info.ResidualScalars[i]);
			}

			// set q0
			var q0 = info.ResidualVec;
			q0.Normalize();
			qs.Add(q0);

			// iteration No.0
			//tex: $\vec r = A \vec q$
			r = matrixFunction(q0);
			if (info.ResidualScalars.Count > 0 && info.ResidualScalars.Count == info.UnconvergedEigenvectors.Count)
			{
				//tex:${\vec{r}}={\vec{r}}-\sum_{i=1}^{n}{\sigma_i{\vec{y}}_i}$
				if (info.ResidualScalars.AsSpan().Any(s => s <= Math.Pow(ψ, 0.25/*0.5*/)))
				{   // scalar is not accurate now 
					Span<T> w = stackalloc T[info.UnconvergedEigenvectors.Count];
					Common.RobustOrthogonalize<TVec, T>(r, info.UnconvergedEigenvectors, w);
					for (int i = 0; i < w.Length; i++)
					{
						info.ResidualScalars[i] = w[i].ToDoubleCheck();
					}
				}
			}
			//tex:$\alpha_0 = \vec q^* \vec r$
			T alpha = q0.Dot(r);
			double α = alpha.ToDoubleCheck();
			αs.Add(α);
			//tex:$\vec r = \vec r - \alpha_0 \vec q_0$
			r.AddBy(q0, alpha.NativeNegate());
			//tex: $\beta_0=\|\vec r_0\|$
			βs.Add(r.Norm());
		}
		#endregion

		#region main loop of Lanczos
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void LanczosMainCalc<TVec, T>(Func<TVec, TVec> matrixFunction, TVec q, ref TVec r, ref SpanList<double> αs, ref SpanList<double> βs, ref TVec newq, bool dispose = true)
			where TVec : class, IKrylovVector<TVec, T>, new()
			where T : unmanaged
		{
			//tex: $\vec v=\vec q$
			/*var v = q;*/
			//tex:$\vec q = \vec r / \beta_{j-1}$
			r.Scale((1 / βs[^1]).FromDouble<T>());
			newq = r;
			//tex: $\vec r = A \vec q$
			r = matrixFunction(newq);
			// a new vector is generated here
			//tex:$\alpha_j = \vec q^* \vec r$
			double α = newq.Dot(r).ToDoubleCheck();
			αs.Add(α);
			//tex:$\vec r = \vec r - \alpha_j \vec q - \beta_{j-1} \vec v$
			r.AddBy(newq, (-αs[^1]).FromDouble<T>());
			r.AddBy(q, (-βs[^1]).FromDouble<T>());
			//tex: $\beta_j = \|\vec r\|$
			βs.Add(r.Norm());

			if (dispose)
				q.Dispose();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void LanczosMainCalc<TVec, T>(Func<TVec, TVec> MatMulVecFunc, SpanList<TVec> qs, ref TVec r, ref SpanList<double> αs, ref SpanList<double> βs)
			where TVec : class, IKrylovVector<TVec, T>, new()
			where T : unmanaged
		{
			TVec newq = new();
			LanczosMainCalc<TVec, T>(MatMulVecFunc, qs[^1], ref r, ref αs, ref βs, ref newq, dispose: false);
			qs.Add(newq);
		}
		#endregion

		#region tridiagonal solve Lanczos
		private delegate bool EigensolveDelegate(SolveVectorMode mode, long n, Storage<double> valOut, Storage<double> A, long lda);

		private static EigensolveDelegate? TridiagSolve = null;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private unsafe static void LanczosTridiagSolve(SpanList<double> αs, SpanList<double> βs, Span<double> eigval, SpanMatrix<double> eigvec, int firstNResidual = 0)
		{
			// check NaN
			if (αs.AsSpan().Any(static a => !double.IsFinite(a)) || βs.AsSpan().Any(static b => !double.IsFinite(b)))
				throw new ArithmeticException(Resources.Other.AbnormalOccured);
			// fill matrix
			int N = αs.Count;
			if (firstNResidual > 0)
			{
				// fill the non-tridiagonal part
				for (int i = 0; i < firstNResidual; i++)
				{
					eigvec[i, i] = αs[i];
					eigvec[firstNResidual, i] = eigvec[i, firstNResidual] = βs[i];
				}
			}
			for (int i = firstNResidual; i < N; i++)
			{
				eigvec[i, i] = αs[i];
				if (i < N - 1)
				{
					eigvec[i, i + 1] = eigvec[i + 1, i] = βs[i];
				}
			}
			// tridiagonal solve
			fixed (double* matPtr = eigvec.UnderlyingSpan, valPtr = eigval)
			{
				var tridiag = new ManagedPureStorage<double>(matPtr, eigvec.LeadDim * N);
				var valsOut = new ManagedPureStorage<double>(valPtr, N);
				if (TridiagSolve is null)
				{
					LAD? pre = LAD.Current;
					LAD.EigenSpecialMatrixHermitian(SolveVectorMode.Vector, N, valsOut, tridiag, eigvec.LeadDim);
					LAD? now = LAD.Current;
					Delegate? d = null;
					pre.SetDelegate<LAD, EigensolveDelegate>(now, nameof(LAD.EigenSpecialMatrixHermitian), ref d);
					if (d is EigensolveDelegate dd)
						TridiagSolve = dd;
				}
				else
				{
					TridiagSolve.Invoke(SolveVectorMode.Vector, N, valsOut, tridiag, eigvec.LeadDim);
				}
			}
		}
		#endregion

		#region restart info
		private ref struct RestartBasicInfo<TVec, T>
			where TVec : class, IKrylovVector<TVec, T>, new()
			where T : unmanaged
		{
			internal TVec ResidualVec;

			internal readonly SpanList<double> ResidualScalars;

			internal readonly SpanList<double> UnconvergedEigenvalues;

			internal readonly SpanList<TVec> UnconvergedEigenvectors;

			internal readonly SpanList<TVec> ConvergedEigenvectors;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			internal RestartBasicInfo(TVec residual, Span<double> scalarHolder1, Span<double> scalarHolder2, Span<TVec> vectorHolder1, Span<TVec> vectorHolder2)
			{
				this.ResidualVec = residual;
				this.ResidualScalars = new(scalarHolder1);
				this.UnconvergedEigenvalues = new(scalarHolder2);
				this.UnconvergedEigenvectors = new(vectorHolder1);
				this.ConvergedEigenvectors = new(vectorHolder2);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			internal void Clear(TVec residual)
			{
				this.ResidualVec = residual;
				this.ResidualScalars.Clear();
				this.UnconvergedEigenvalues.Clear();
				this.UnconvergedEigenvectors.Clear();
			}
		}
		#endregion

		#region orthogonality tracker
		private readonly ref struct OrthogonalityTracker
		{
			internal readonly SpanList<double> pre;

			internal readonly SpanList<double> now;

			private readonly double explicitValue;

			private readonly int convergedCount, unconvergedCount;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			internal OrthogonalityTracker(double ψ, int convergedRitz, int unconvergedRitz, Span<double> scalarHolder1, Span<double> scalarHolder2)
			{
				this.explicitValue = ψ;
				this.convergedCount = convergedRitz;
				this.unconvergedCount = unconvergedRitz;
				this.pre = new(scalarHolder1);
				this.now = new(scalarHolder2);
				if (unconvergedRitz + convergedRitz >= 1)
				{
					Span<double> temp1 = scalarHolder1[..(unconvergedRitz + convergedRitz - 1)];
					// 100 for not estimated orthogonality loss of Ritz vector
					temp1.Fill(100 * ψ);
					pre.AddRange(temp1);
					pre.Add(ψ);
				}
				Span<double> temp2 = scalarHolder2[..(unconvergedRitz + convergedRitz)];
				// 100 for not estimated orthogonality loss of Ritz vector
				temp2.Fill(100 * ψ);
				now.AddRange(temp2);
				now.Add(ψ);
				// +1 for iteration No.0
				pre.Add(1);
				now.Add(1);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			internal string Reorthonalize<TVec, T>(TVec r, ReadOnlySpan<TVec> qs, ReadOnlySpan<TVec> converged, double thre1, double thre2)
				where TVec : class, IKrylovVector<TVec, T>, new()
				where T : unmanaged
			{
#if DEBUG
				var stringBuilder = new StringBuilder("\tre-orthogonalize the new basis vector to the ");
				bool hasContent = false;
#endif
				if (this.now[..^1].Any(w => w >= thre1))
				{
					// previous Krylov basis
					//tex:$\vec{r}=\vec{r}-\sum_{k}{\vec{q}(\vec{q}_k^* \vec{r})}$
					for (int k = this.unconvergedCount; k < qs.Length; k++)
					{
						int i = k; // the index of q
						int j = i + this.convergedCount; // the index of this ω
						if (this.now[j] >= thre2)
						{
#if DEBUG
							hasContent = true;
							stringBuilder.Append(k.ToOrdinal());
							stringBuilder.Append(", ");
#endif
							var q = qs[i];
							r.AddBy(q, q.Dot(r).NativeNegate());
							this.now[j] = this.explicitValue;
						}
					}
				}

				// should be more careful with previous converged and unconverged eigenvectors
				if (this.now[..(this.unconvergedCount + this.convergedCount)].Any(w => w >= thre2))
				{
					// previous unconverged eigenvectors
					for (int k = this.convergedCount; k < this.unconvergedCount + this.convergedCount; k++)
					{
						if (this.now[k] >= thre2)
						{
#if DEBUG
							hasContent = true;
							stringBuilder.Append(k.ToOrdinal());
							stringBuilder.Append(", ");
#endif
							var q = qs[k - this.convergedCount];
							r.AddBy(q, q.Dot(r).NativeNegate());
							this.now[k] = this.explicitValue;
						}
					}
					// previous converged eigenvectors
					for (int k = 0; k < this.convergedCount; k++)
					{
						if (this.now[k] >= thre2)
						{
#if DEBUG
							stringBuilder.Append(k.ToOrdinal());
							stringBuilder.Append(", ");
#endif
							var q = converged[k];
							r.AddBy(q, q.Dot(r).NativeNegate());
							this.now[k] = this.explicitValue;
						}
					}
				}
#if DEBUG
				if (hasContent)
				{
					stringBuilder.Remove(stringBuilder.Length - 2, 1);
					stringBuilder.Append("ones.");
					return stringBuilder.ToString();
				}
				else
#endif
					return string.Empty;
			}

			internal void ReorthogonalityUpdate(ReadOnlySpan<double> αs, ReadOnlySpan<double> βs, ReadOnlySpan<double> residuals, double φ)
			{
				Span<double> ωNew = stackalloc double[this.now.Count + 1];
				ωNew[^1] = 1;
				ωNew[^2] = this.explicitValue;
				// the structure of ω is [converged, unconverged, Krylov basis]
				// iteration = unconverged + basis
				var totalCount = this.convergedCount + this.unconvergedCount;
				//tex:$$w_{j+1,k}=\frac{1}{\beta_j}[\beta_k w_{j,k+1}+(\alpha_k-\alpha_j)w_{j,k}+\beta_{k-1}\omega_{j,k-1}-\beta_{j-1}\omega_{j-1,k}]+\phi$$
				for (int k = totalCount; k < αs.Length - 1 + this.convergedCount; k++)
				{
					var i = k - this.convergedCount; // the index of q, α, β
					if (k == 0) // ω[-1] = 0
						ωNew[k] = (βs[i] * this.now[k + 1] + (αs[i] - αs[^1]) * this.now[k] - βs[^2] * this.pre[k]) / βs[^1];
					else
						ωNew[k] = (βs[i] * this.now[k + 1] + (αs[i] - αs[^1]) * this.now[k] + βs[i - 1] * this.now[k - 1] - βs[^2] * this.pre[k]) / βs[^1];
					ωNew[k] = Math.Abs(ωNew[k]) + φ;
				}
				// end for normal basis track
				if (totalCount > 0)
				{
					//tex:$$\sum_l{\sigma_l \omega_{j,-l}} = \left| - \alpha_0 \omega_{j,0} - \beta_0 \omega_{j,1} \right| + \vec{q}_j^* \vec{f}_0$$
					var totalError = Math.Abs(αs[this.unconvergedCount] * ωNew[totalCount] + βs[this.unconvergedCount] * ωNew[totalCount + 1]) + φ;
					////var sumResiduals = residuals.Sum(r => Math.Abs(r));
					var minResidual = residuals.Min(r => Math.Abs(r));
					for (int k = this.convergedCount; k < totalCount; k++)
					{
						var i = k - this.convergedCount; // the index of q, α, β
						ωNew[k] = totalError / minResidual / this.unconvergedCount; // upper bound
					}
					// end for unconverged Ritz vector track
					for (int k = 0; k < this.convergedCount; k++)
					{
						ωNew[k] = totalError / this.explicitValue / this.convergedCount;
					}
					// end for converged eigenvector track
				}
				this.pre.Clear(); this.pre.AddRange(this.now);
				this.now.Clear(); this.now.AddRange(MemoryMarshal.CreateReadOnlySpan(ref ωNew.Ref(), ωNew.Length));
			}
		}
		#endregion

		#region convergence check
		private static (bool converge, string message, string trace) LanczosConvergenceCheck(ReadOnlySpan<double> eigval, SpanMatrix<double> eigvec, double beta, double tol, int nConverged, bool useGap)
		{
			var iter = eigval.Length - 1;
			// get θ_0  S_j,0 for convergence check
			Span<ComplexDouble> lastRow = stackalloc ComplexDouble[eigval.Length], complexVal = stackalloc ComplexDouble[eigval.Length];
			int row = eigvec.Rows - 1;
			for (int i = 0; i < eigval.Length; i++)
			{
				lastRow[i] = eigvec[row, i];
				complexVal[i] = eigval[i];
			}
			double Sj0 = lastRow[0].Real;
			double θ0 = eigval[0];
			var βMulS = beta * Math.Abs(Sj0);
			// get gap
			double gap = useGap ? Common.GetGap(beta, tol, complexVal, lastRow) : Math.Max(Math.Abs(eigval[0]), Math.Abs(eigval[^1]));
			// test convergence
			bool converge = βMulS / gap <= tol;
			// log convergence
			string message;
			if (converge)
				message = string.Format(Resource.LanczosConvergeOnePair, nConverged.ToOrdinal());
			else
				message = string.Empty;
			// trace log
			string trace = $"the {nConverged.ToOrdinal()} eigen-pair has: θ = {θ0}, S = {Math.Abs(Sj0)}, γ = {gap}";
			return (converge, message, trace);
		}
		#endregion

		#region get eigenvector
		private static TVec GetRealEigenvector<TVec, T>(TVec r, SpanList<TVec> Q, SpanMatrix<double> eigvecs)
			where TVec : class, IKrylovVector<TVec, T>, new()
			where T : unmanaged
		{
			Span<T> eigvec = stackalloc T[Q.Count];
			eigvecs[0].CopyTo(eigvec, static e => e.FromDouble<T>());
			return r.OperateOn(Q, eigvec);
		}
		#endregion

		#region preserve select
		private static Span<int> PreserveSelect(IPreserveSelector selector, Span<double> eigvals, SpanMatrix<double> eigvecs, int converged, int target, int iters, Span<int> result)
		{
			Span<ComplexDouble> values = stackalloc ComplexDouble[eigvals.Length];
			Span<ComplexDouble> vectors = eigvecs.PresentingLength.CheckStackLimit<ComplexDouble>() ?? stackalloc ComplexDouble[eigvecs.PresentingLength];
			eigvals.CopyTo(values, static v => v);
			eigvecs.CopyTo(vectors, static v => v);
			int count = selector.PreserveSelect(values, vectors, converged, target, iters, result, withConverged: false);
			return result[..count];
		}
		#endregion

		#region add unconverged vectors
		private static void AddUnconvergedVectors<TVec, T>(ref RestartBasicInfo<TVec, T> info, ReadOnlySpan<TVec> Q, ReadOnlySpan<int> preserve, Span<double> eigvals, SpanMatrix<double> eigvecs, TVec r, double rNorm)
			where TVec : class, IKrylovVector<TVec, T>, new()
			where T : unmanaged
		{
			Span<T> tempSVec = stackalloc T[eigvecs.Rows];
			Span<double> lastRow = stackalloc double[eigvecs.Cols];
			//tex:$\vec{r}$ so that $A\vec{y}_i - \vartheta_i\vec{y}_i = \sigma_i\vec{r}$
			eigvecs.CopyRowTo(eigvecs.Rows - 1, lastRow);
			// calculate new unconverged Ritz vectors
			foreach (var i in preserve)
			{
				//tex:$\sigma_i=\beta_0 s_{k,i}$
				info.ResidualScalars.Add(lastRow[i] * rNorm);
				//tex:$\vartheta_i$ and $\vec{y}_i = Q \vec{s}_i$
				info.UnconvergedEigenvalues.Add(eigvals[i]);
				eigvecs[i].CopyTo(tempSVec, static a => a.FromDouble<T>());
				var unconverged = r.OperateOn(Q, tempSVec);
				unconverged.Normalize();
				info.UnconvergedEigenvectors.Add(unconverged);
			}
		}
		#endregion


		#region naive Lanczos
		internal static (double val, TVec vec) NaiveLanczos<TVec, T>(Func<TVec, TVec> matrixFunction, TVec initial, int maxIter, bool checkFirst, TimeSpan interval)
			where TVec : class, IKrylovVector<TVec, T>, new()
			where T : unmanaged
		{
			#region basic check
			if (matrixFunction is null)
				throw new ArgumentNullException(nameof(matrixFunction));
			if (initial is null)
				throw new ArgumentNullException(nameof(initial));
			if (checkFirst)
				Common.CheckParas<TVec, T>(matrixFunction, initial, smallestK: 1, ref maxIter, herm: true);
			else if (maxIter <= 0)
				throw new ArgumentOutOfRangeException(nameof(maxIter), maxIter, Resources.Parameter.MustPositive);
			#endregion

			#region initialize
			// new inner stop watch and get outer stopwatch
			var stopwatch = Stopwatch.StartNew();
			// transformation matrix Q which will be disposed after return or exception automatically
			Span<IntPtr> tempQ = maxIter.CheckStackLimit<IntPtr>() ?? stackalloc IntPtr[maxIter];
			var qs = new SpanList<TVec>(tempQ.AsClassType<TVec>());
			var αs = new SpanList<double>(maxIter.CheckStackLimit<double>() ?? stackalloc double[maxIter]);
			var βs = new SpanList<double>(maxIter.CheckStackLimit<double>() ?? stackalloc double[maxIter]);
			// intermediate vector
			TVec? r = null;
			#endregion

			#region main
			try
			{
				#region first step of iteration
				// start
				Log.Write(string.Format(Resource.NaiveLanczosStart, initial.Length, maxIter));
				// copy initial vector
				initial = initial.Clone();
				// step 0
				LanczosInit<TVec, T>(matrixFunction, ref initial, out r, out double α0, out double β0);
				αs.Add(α0);
				βs.Add(β0);
				qs.Add(initial);
				#endregion

				#region main loop
				for (int j = 1; j < maxIter; j++)
				{
					#region log output
					Log.Write($"Naïve Lanczos algorithm: now at iteration {j}, {stopwatch.Elapsed} passed since last output.", level: LogLevel.Trace);
					if (stopwatch.Elapsed >= interval)
					{
						Log.Write(string.Format(Resource.IterationAndTimeInfo, j, stopwatch.Elapsed.TotalMinutesString()));
						stopwatch.Restart();
					}
					#endregion

					LanczosMainCalc<TVec, T>(matrixFunction, qs, ref r, ref αs, ref βs);
					if (βs[j] == 0)
						break;
				}
				#endregion

				#region tridiagonal solve
				// log
				Log.Write(string.Format(Resource.NaiveLanczosFinish, αs[^1], βs[^1]));

				int n = qs.Count;
				Span<double> eigenvalues = n.CheckStackLimit<double>() ?? stackalloc double[n];
				Span<double> eigvec = (n * n).CheckStackLimit<double>() ?? stackalloc double[n * n];
				SpanMatrix<double> eigenvectors = new(eigvec, n);
				LanczosTridiagSolve(αs, βs, eigenvalues, eigenvectors);
				#endregion

				#region output
				var vecOut = GetRealEigenvector<TVec, T>(r, qs, eigenvectors);
				return (eigenvalues[0], vecOut);
				#endregion
			}
			finally
			{
				r?.Dispose();
				initial?.Dispose();
				qs.ClearList();
			}
			#endregion
		}
		#endregion


		#region restart lanczos
		internal static int? RestartLanczos<TVec, T>(Func<TVec, TVec> matrixFunction, TVec initial, int maxRestarts, int iterPerRestart, double tolerance, ReorthogonalizeMethod reorthogonalize, bool useGap, IPreserveSelector selector, bool checkFirst, Span<double> outEigvals, Span<TVec> outEigvecs, TimeSpan interval)
			where TVec : class, IKrylovVector<TVec, T>, new()
			where T : unmanaged
		{
			#region basic
			if (initial is null)
				throw new ArgumentNullException(nameof(initial));
			if (tolerance <= 0)
				throw new ArgumentOutOfRangeException(nameof(tolerance), tolerance, Resources.Parameter.MustPositive);
			// check parameters
			int smallestK = outEigvals.Length;
			if (checkFirst)
				Common.CheckParas<TVec, T>(matrixFunction, initial, smallestK, ref iterPerRestart, herm: true);
			else
				iterPerRestart = Math.Min(iterPerRestart, Common.HERM_MAX_ITER);
			// check other
			if (reorthogonalize < ReorthogonalizeMethod.Selective || reorthogonalize > ReorthogonalizeMethod.RobustFull)
				return null; // not support

			// log start
			Log.Write(string.Format(Resource.RestartLanczosStart, initial.Length, maxRestarts));

			// stopwatch start
			Stopwatch stopwatchStart = Stopwatch.StartNew(), stopwatch = Stopwatch.StartNew();
			#endregion

			#region initialize
			TVec guess = initial.Clone();
			Span<IntPtr> tempQ = stackalloc IntPtr[iterPerRestart];
			var qs = new SpanList<TVec>(tempQ.AsClassType<TVec>());
			Span<double> eigvals = stackalloc double[iterPerRestart];
			Span<double> eigvecSpan = (iterPerRestart * iterPerRestart).CheckStackLimit<double>() ?? stackalloc double[iterPerRestart * iterPerRestart];
			SpanMatrix<double> eigvecs = new(eigvecSpan, iterPerRestart);

			Span<double> tempHolder1 = stackalloc double[iterPerRestart], tempHolder2 = stackalloc double[iterPerRestart];
			Span<IntPtr> tempQ1 = stackalloc IntPtr[iterPerRestart]; Span<IntPtr> tempQ2 = stackalloc IntPtr[smallestK];
			var restartInfo = new RestartBasicInfo<TVec, T>(residual: guess, tempHolder1, tempHolder2, tempQ1.AsClassType<TVec>(), tempQ2.AsClassType<TVec>());

			SpanList<double> alphas = new(stackalloc double[iterPerRestart]), betas = new(stackalloc double[iterPerRestart]);
			#endregion

			#region main
			SpanList<double> eigenvalues = new(stackalloc double[smallestK]);
			TVec? r = null;
			try
			{
				#region restart loop
				Span<int> preserveIndicesSpan = stackalloc int[iterPerRestart];

				for (int nRestart = 0; nRestart < maxRestarts; nRestart++)
				{
					// calculate
					var converged = RestartLanczosInner(matrixFunction, iterPerRestart, tolerance, reorthogonalize == ReorthogonalizeMethod.Selective ? null : reorthogonalize == ReorthogonalizeMethod.RobustFull, useGap, ref restartInfo, eigvals, eigvecs, ref qs, ref alphas, ref betas, out r);

					#region if converge
					Span<double> eigvalsNow = eigvals;
					SpanMatrix<double> eigvecsNow = eigvecs;
					if (converged)
					{
						// output newest eigenvalue
						Log.Write($"The newest unconverged eigenvalue is {eigvals[0]}", level: LogLevel.Trace);
						// calculate last eigenvector
						eigenvalues.Add(eigvals[0]);
						var newConverged = GetRealEigenvector<TVec, T>(r, qs, eigvecs);
						newConverged.Normalize();
						restartInfo.ConvergedEigenvectors.Add(newConverged);
						// remove the newly converged one from eigen pairs
						eigvalsNow = eigvals[1..];
						eigvecsNow = eigvecs[1..];

						// if all converged
						if (restartInfo.ConvergedEigenvectors.Count >= smallestK)
						{
							Log.Write(string.Format(Resource.RestartLanczosConverge, nRestart + 1));
							restartInfo.ConvergedEigenvectors.CopyTo(outEigvecs);
							eigenvalues.CopyTo(outEigvals);
							return eigenvalues.Count;
						}
					}
					#endregion

					#region not converge, prepare for next while
					#region log output
					if (stopwatch.Elapsed >= interval)
					{
						Log.Write(string.Format(Resource.IterationAndTimeInfo, (nRestart + 1) * iterPerRestart, stopwatchStart.Elapsed.TotalMinutesString()));
						stopwatch.Restart();
					}
					#endregion

					#region restart preserve selection
					try
					{
						// select the Ritz pairs to preserve
						var preserveIndices = PreserveSelect(selector, eigvalsNow, eigvecsNow, restartInfo.ConvergedEigenvectors.Count, smallestK, iterPerRestart, preserveIndicesSpan);
						if (preserveIndices.Length == 1)
							Log.Write($"Restarting with only one preserved Ritz pair may never improve the result.", level: LogLevel.Warning);
						// cannot dispose old unconverged vectors and residual vector since they are in Q now
						////restartInfo.UnconvergedEigenvectors.ClearList<TVec>();
						////restartInfo.ResidualVec.ForceDispose();
						restartInfo.Clear(residual: r);
						// add unconverged vectors
						AddUnconvergedVectors(ref restartInfo, qs, preserveIndices, eigvalsNow, eigvecsNow, r, rNorm: betas[^1]);
					}
					catch (Exception)
					{
						r?.Dispose();
						restartInfo.ResidualVec?.Dispose();
						restartInfo.UnconvergedEigenvectors.ClearList();
						throw;
					}
					#endregion
					#endregion
				}
				#endregion

				#region not converge
				Log.Write(string.Format(Resource.RestartLanczosFail, maxRestarts, smallestK - eigenvalues.Count));
				restartInfo.ConvergedEigenvectors.CopyTo(outEigvecs);
				eigenvalues.CopyTo(outEigvals);
				return eigenvalues.Count;
				#endregion
			}
			#region dispose
			finally
			{
				guess?.Dispose();
				r?.Dispose();
				restartInfo.ResidualVec?.Dispose();
				qs.ClearList();
				restartInfo.UnconvergedEigenvectors.ClearList();
			}
			#endregion
			#endregion
		}


		private static bool RestartLanczosInner<TVec, T>(Func<TVec, TVec> matrixFunction, int nIter, double tolerance, bool? robustOrth, bool useGap, ref RestartBasicInfo<TVec, T> restartInfo, Span<double> eigvals, SpanMatrix<double> eigvecs, ref SpanList<TVec> qs, ref SpanList<double> αs, ref SpanList<double> βs, out TVec r)
			where TVec : class, IKrylovVector<TVec, T>, new()
			where T : unmanaged
		{
			#region constants
			double machinePrecision = Const<T>.MachinePrecision,
				   thresholdSqrt = Math.Pow(machinePrecision, 0.6/*0.5*/), thresholdPow = Math.Pow(machinePrecision, 0.75),
				   explicitNormalizeError = machinePrecision/* * info.MatrixNorm*/,
				   φ = machinePrecision * 2/* * Math.Sqrt(info.MatrixNorm)*/;
			#endregion

			#region initialize
			// convergence flag
			bool converge = false;

			// clear lists
			qs = qs.ClearList();
			αs.Clear(); βs.Clear();

			// get the number of restart Ritz vectors
			int NRitz = restartInfo.UnconvergedEigenvalues.Count;
			#endregion

			try
			{
				#region first step of iteration
				// start log
				Log.Write($"Restart with max number of iterations = {nIter}", level: LogLevel.Trace);

				// iteration 0
				Span<double> tempHolder1 = stackalloc double[nIter + 1], tempHolder2 = stackalloc double[nIter + 1];
				var tracker = new OrthogonalityTracker(explicitNormalizeError, restartInfo.ConvergedEigenvectors.Count, NRitz, tempHolder1, tempHolder2);
				LanczosInit(matrixFunction, explicitNormalizeError, out r, ref qs, ref αs, ref βs, ref restartInfo);
				#endregion

				// main loop
				int j;
				for (j = NRitz + 1; j < nIter; j++)
				{
					#region re-orthogonalization
					if (!robustOrth.HasValue)
					{
						string strInfo = tracker.Reorthonalize<TVec, T>(r, qs, restartInfo.ConvergedEigenvectors, thresholdSqrt, thresholdPow);
						if (!string.IsNullOrWhiteSpace(strInfo))
						{
							Log.Write(strInfo, level: LogLevel.Trace);
							double pre = βs[^1];
							βs[^1] = r.Norm();
							Log.Write($"Re-orthogonalization of previous basis changes β from {pre} to {βs[^1]}.", level: LogLevel.Trace);
						}
					}
					else
					{
						Common.RobustOrthogonalize<TVec, T>(r, qs, default, robustOrth.Value);
						double pre = βs[^1];
						βs[^1] = r.Norm();
						Log.Write($"Re-orthogonalization of previous basis changes β from {pre} to {βs[^1]}.", level: LogLevel.Trace);
					}
					#endregion

					#region main calculation
					LanczosMainCalc<TVec, T>(matrixFunction, qs, ref r, ref αs, ref βs);
					if (βs[j] == 0)
					{   // invariant subspace found
						// construct tridiagonal matrix and calculate eigenvalue
						LanczosTridiagSolve(αs, βs, eigvals, eigvecs, firstNResidual: NRitz);
						converge = true;
						break;
					}
					Log.Write($"Main calculation finished, α = {αs[j]}, β = {βs[j]}", level: LogLevel.Trace);
					#endregion

					#region construct tridiagonal and convergence check
					if (j > 2 || nIter - j <= 2)
					{
						// construct tridiagonal matrix and calculate eigenvalue
						LanczosTridiagSolve(αs, βs, eigvals, eigvecs, firstNResidual: NRitz);
						double phi = machinePrecision * 2 * Math.Sqrt(Math.Max(Math.Abs(eigvals[0]), Math.Abs(eigvals[^1])));
						string message, trace;
						// convergence check
						(converge, message, trace) = LanczosConvergenceCheck(eigvals, eigvecs, beta: βs[^1], tolerance, nConverged: restartInfo.ConvergedEigenvectors.Count, useGap);
						// log
						Log.Write(trace, level: LogLevel.Trace);
						if (message.Length > 0)
							Log.Write(message, category: "Lanczos");
						// break if converge
						if (converge)
							break; // for j
					}
					#endregion

					#region orthogonality check
					if (!robustOrth.HasValue)
						tracker.ReorthogonalityUpdate(αs, βs, restartInfo.ResidualScalars, φ);
					#endregion
				} // end for main loop

				#region trace log when didn't converge
				if (!converge)
				{
					Log.Write($"The {restartInfo.ConvergedEigenvectors.Count.ToOrdinal()} eigenvalue fails to converge after max number of iterations {nIter}.", level: LogLevel.Trace);
				}
				#endregion

				// output
				return converge;
			}
			catch (Exception)
			{
				qs.ClearList();
				throw;
			}
		}
		#endregion



		#region linear solve helpers
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static (double relativeError, TVec solve)? CheckLinearSolve<TVec, T>(Func<TVec, TVec> matrix, Func<TVec, TVec>? preconditioner, TVec initial, TVec rightSide, ref int maxIter, double tolerance, bool checkFirst, out double normB, out double realTolerance)
			where TVec : class, IKrylovVector<TVec, T>, new()
			where T : unmanaged
		{
			#region basic
			if (initial is null)
				throw new ArgumentNullException(nameof(initial));
			if (rightSide is null)
				throw new ArgumentNullException(nameof(rightSide));
			if (tolerance <= 0)
				throw new ArgumentOutOfRangeException(nameof(tolerance), tolerance, Resources.Parameter.MustPositive);
			if (checkFirst)
			{
				int iter = maxIter;
				Common.CheckParas<TVec, T>(matrix, initial, smallestK: 1, ref iter, herm: true);
				if (preconditioner is not null)
					Common.CheckParas<TVec, T>(preconditioner, initial, smallestK: 1, ref iter, herm: true);
				maxIter = Math.Max(maxIter, iter);
			}
			else if (maxIter <= 0)
				throw new ArgumentOutOfRangeException(nameof(maxIter), maxIter, Resources.Parameter.MustPositive);
			maxIter = Math.Min(maxIter, (int)initial.Length);
			#endregion

			#region shortcut
			normB = rightSide.Norm();
			realTolerance = tolerance * normB;
			if (normB == 0)
			{   // all 0 solution
				TVec solution = rightSide.Clone();
				return (0, solution);
			}
			else
			{
				return null;
			}
			#endregion
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static (double relativeError, TVec solve)? CheckLinearSolveInitial<TVec, T>(Func<TVec, TVec> matrix, TVec initial, TVec b, double normB, double realTolerance, out TVec r, out TVec x, out TVec minResidualVec, out double minResidual)
			where TVec : class, IKrylovVector<TVec, T>, new()
			where T : unmanaged
		{
			#region initial vector check
			x = initial;
			// Ignore Spelling: \mathbf
#pragma warning disable CS8625
			r = null;
#pragma warning restore CS8625
			try
			{
				//tex: $\vec r = \vec b - \mathbf A \vec x_0$
				r = Common.RSetToBSubAx<TVec, T>(matrix, initial, b);
				minResidual = r.Norm();
				if (minResidual <= realTolerance)
				{
					minResidualVec = x;
					return (minResidual / normB, initial.Clone());
				}
				else
				{
					x = initial.Clone();
					minResidualVec = x;
					return null;
				}
			}
			catch (Exception)
			{
				r?.Dispose();
				x?.Dispose();
				throw;
			}
			#endregion
		}
		#endregion


		#region preconditioned conjugate gradient
		internal static (double relativeError, TVec solve) ConjugateGradient<TVec, T>(Func<TVec, TVec> matrix, Func<TVec, TVec>? preconditioner, TVec initial, TVec rightSide, int maxIter, double tolerance, bool checkFirst, TimeSpan interval, int maxStagnation)
			where TVec : class, IKrylovVector<TVec, T>, new()
			where T : unmanaged
		{
			#region basic
			var simpleSolution = CheckLinearSolve<TVec, T>(matrix, preconditioner, initial, rightSide, ref maxIter, tolerance, checkFirst, out double normB, out double realTolerance);
			if (simpleSolution.HasValue)
				return simpleSolution.Value;
			#endregion

			#region initialize
			// log
			Log.Write(string.Format(Resource.PCGStart, initial.Length, maxIter));
			Stopwatch stopwatch = Stopwatch.StartNew();
			// check initial guess
			simpleSolution = CheckLinearSolveInitial<TVec, T>(matrix, initial, rightSide, normB, realTolerance, out TVec r, out TVec x, out TVec solution, out double minResidual);
			if (simpleSolution.HasValue)
				return simpleSolution.Value;
			// otherwise
			double ρ = 1;
			TVec p = r;
			int stagnations = 0;
			#endregion

			#region main loop
			try
			{
				bool success = false;
				for (int i = 0; i < maxIter; i++)
				{
					#region log output
					Log.Write($"Preconditioned Conjugate Gradient algorithm: now at iteration {i}, {stopwatch.Elapsed} passed since last output.", level: LogLevel.Trace);
					if (stopwatch.Elapsed >= interval)
					{
						Log.Write(string.Format(Resource.IterationAndTimeInfo, i, stopwatch.Elapsed.TotalMinutesString()));
						stopwatch.Restart();
					}
					#endregion

					#region calculation and scalar checks
					//tex: Solve $\mathbf M \vec z_i = \vec r_i$ for $\vec z_i$
					TVec z = r;
					if (preconditioner is not null)
						z = preconditioner.Invoke(r);
					double ρOld = ρ;
					ρ = r.Dot(z).ToDoubleCheck();
					if (ρ <= 0)
						throw new ArgumentException(Resource.NotPositiveDefinite, nameof(preconditioner));
					if (i == 0)
					{
						p = z;
					}
					else
					{
						// Ignore Spelling: \dfrac
						//tex: $\beta_i = \dfrac {\vec r_i \cdot \vec z_i} {\vec r_{i - 1} \cdot \vec z_{i-1}}$
						double β = ρ / ρOld;
						if (β == 0)
						{   // failed due to scalar error
							success = false;
							break;
						}
						//tex: $\vec p_i = \vec z_i + \beta_i \vec p_{i - 1}$
						TVec preP = p;
						p = z;
						if (p == r)
							p = p.Clone();
						p.AddBy(preP, β.FromDouble<T>());
						// now, p is certainly a new vector
					}
					//tex: $\vec q = \mathbf A \vec p_i$
					TVec q = matrix.Invoke(p);
					double pDotQ = p.Dot(q).ToDoubleCheck();
					if (pDotQ <= 0)
						throw new ArgumentException(Resource.NotPositiveDefinite, nameof(matrix));
					//tex: $\alpha_i = \dfrac {\vec r_i \cdot \vec z_i} {\vec p_i \cdot \vec q}$
					double α = ρ / pDotQ;
					#endregion

					#region check for stagnation
					if (p.Norm() * α.NativeAbsolute() < Const<T>.MachinePrecision * x.Norm())
						stagnations++;
					else
						stagnations = 0;
					#endregion

					#region prepare next iteration
					//tex: $\vec x = \vec x + \alpha \vec p_i$
					x.AddBy(p, α.FromDouble<T>());
					//tex: $\vec r = \vec r - \alpha \vec q$
					r.AddBy(q, (-α).FromDouble<T>());
					double normR = r.Norm();
					#endregion

					#region check for convergence
					if (normR <= realTolerance)
					{   // check residual vector again
						Common.RSetToBSubAx<TVec, T>(matrix, ref r, x, rightSide);
						double residual = r.Norm();
						if (residual <= realTolerance)
						{
							success = true;
							minResidual = residual;
							break;
						}
					}
					if (stagnations >= maxStagnation)
					{	// failed due to stagnation
						success = false;
						break;
					}
					// otherwise
					if (normR < minResidual)
					{
						minResidual = normR;
						if (x != solution)
							solution.Dispose();
						solution = x.Clone();
					}
					#endregion
				}

				#region return solution of first one with minimal residual
				if (success)
				{
					(solution, x) = (x, solution);
					minResidual /= normB;
				}
				else
				{
					Common.RSetToBSubAx<TVec, T>(matrix, ref r, solution, rightSide);
					double normR = r.Norm();
					if (normR <= minResidual)
					{
						minResidual = normR / normB;
					}
					else
					{
						minResidual /= normB;
						(solution, x) = (x, solution);
					}
				}
				Log.Write(string.Format(Resource.PCGFinish, minResidual, tolerance));
				return (minResidual, solution);
				#endregion
			}
			#region dispose
			catch (Exception)
			{
				solution?.Dispose();
				throw;
			}
			finally
			{
				if (r != solution)
					r?.Dispose();
				if (x != solution)
					x?.Dispose();
				if (p != solution)
					p?.Dispose();
			}
			#endregion
			#endregion
		}
		#endregion


		#region preconditioned minimal residual
		internal static (double relativeError, TVec solve) MinimalResidual<TVec, T>(Func<TVec, TVec> matrix, Func<TVec, TVec>? preconditioner, TVec initial, TVec rightSide, int maxIter, double tolerance, bool checkFirst, TimeSpan interval, int maxStagnation)
			where TVec : class, IKrylovVector<TVec, T>, new()
			where T : unmanaged
		{
			#region basic
			var simpleSolution = CheckLinearSolve<TVec, T>(matrix, preconditioner, initial, rightSide, ref maxIter, tolerance, checkFirst, out double normB, out double realTolerance);
			if (simpleSolution.HasValue)
				return simpleSolution.Value;
			#endregion

			#region initialize
			// log
			Log.Write(string.Format(Resource.MinResStart, initial.Length, maxIter));
			Stopwatch stopwatch = Stopwatch.StartNew();
			// Ignore Spelling: \mathbf
			// check initial guess
			simpleSolution = CheckLinearSolveInitial<TVec, T>(matrix, initial, rightSide, normB, realTolerance, out TVec r, out TVec x, out TVec solution, out double minResidual);
			if (simpleSolution.HasValue)
				return simpleSolution.Value;
			// otherwise
			#endregion

			TVec? v = null, vv = null, oldV = null, olderV = null, Am = null, oldAm = null, olderAm = null, m = null, oldM = null, olderM = null;
			try
			{
				#region prepare the first step
				oldV = r; v = r;
				if (preconditioner is not null)
					v = preconditioner.Invoke(r);
				//tex: $\beta_1 = \vec r \cdot (\mathbf M^{-1} \vec r)$
				double oldβ = oldV.Dot(v).ToDoubleCheck();
				if (oldβ <= 0)
					throw new ArgumentException(Resource.NotPositiveDefinite, nameof(preconditioner));
				oldβ = Math.Sqrt(oldβ);
				//tex: preserve $\prod_i{s_i}$
				double prodSi = oldβ;
				//tex: $\vec v' = \vec v / \beta_1$
				vv = v.Clone(); vv.Scale((1 / oldβ).FromDouble<T>());
				//tex: $\vec v = \mathbf A \vec v'$
				if (v != r)
					v.Dispose();
				v = matrix.Invoke(vv);
				Am = v.Clone();
				double α = vv.Dot(v).ToDoubleCheck();
				v.AddBy(oldV, (α / oldβ).FromDouble<T>());

				#region local re-orthogonalization
				//tex: $\vec v = \vec v - \dfrac {\vec v' \cdot \vec v} {\vec v' \cdot \vec v'} \vec v'$
				v.AddBy(vv, vv.Dot(v).NativeDivide(vv.Dot(vv)).NativeNegate());
				#endregion

				olderV = oldV; oldV = v;
				if (preconditioner is not null)
					v = preconditioner.Invoke(oldV);
				//tex: $\beta^2 = \vec v_{i-1} \cdot (\mathbf M^{-1} \vec v_i)$
				double β = oldV.Dot(v).ToDoubleCheck();
				if (β <= 0)
					throw new ArgumentException(Resource.NotPositiveDefinite, nameof(preconditioner));
				#endregion

				#region first step
				β = Math.Sqrt(β);
				double γbar = α, ε = 0, δbar = β, γ = Math.Sqrt(γbar * γbar + β * β), δ = 0;
				m = vv;
				T γInv = (1 / γ).FromDouble<T>();
				m.Scale(γInv); Am.Scale(γInv);
				double cs = γbar / γ;
				double Si = β / γ;
				x.AddBy(m, (prodSi * cs).FromDouble<T>());
				double oldProdSi = prodSi; prodSi *= Si;

				double normR;
				if (preconditioner is not null)
				{
					r.AddBy(Am, (-oldProdSi * cs).FromDouble<T>());
					normR = r.Norm();
				}
				else
				{
					normR = Math.Abs(prodSi);
				}
				// check for convergence after first step
				if (normR <= realTolerance)
				{
					minResidual = normR / normB;
					solution = x;
					return (minResidual, solution);
				}

				int stagnations = 0;
				bool success = false;
				#endregion

				#region main loop
				for (int i = 1; i < maxIter; i++)
				{
					#region log output
					Log.Write($"Preconditioned Minimal Residual algorithm: now at iteration {i}, {stopwatch.Elapsed} passed since last output.", level: LogLevel.Trace);
					if (stopwatch.Elapsed >= interval)
					{
						Log.Write(string.Format(Resource.IterationAndTimeInfo, i, stopwatch.Elapsed.TotalMinutesString()));
						stopwatch.Restart();
					}
					#endregion

					#region calculation
					//tex: $\vec v' = \vec v / \beta$
					vv = v; v.Scale((1 / β).FromDouble<T>());
					//tex: $\vec v = \mathbf A \vec v / \beta$
					v = matrix.Invoke(vv);
					// change Am
					olderAm?.Dispose();
					olderAm = oldAm;
					oldAm = Am;
					Am = v;
					// orthogonalize v (the key component of Lanczos)
					//tex: $\vec v_i = \vec v_i - t_{i,i-1}\vec v_{i-1} - t_{i,i-2}\vec v_{i-2}$
					v.AddBy(olderV, (-β / oldβ).FromDouble<T>());
					α = vv.Dot(v).ToDoubleCheck();
					v.AddBy(oldV, (-α / β).FromDouble<T>());
					olderV?.Dispose();
					olderV = oldV;
					oldV = v;
					// apply preconditioner
					if (preconditioner is not null)
						v = preconditioner.Invoke(oldV);
					// change scalars
					oldβ = β;
					β = oldV.Dot(v).ToDoubleCheck();
					β = Math.Sqrt(β);
					δ = cs * δbar + Si * α;
					// change m
					olderM?.Dispose();
					olderM = oldM;
					oldM = m;
					// Ignore Spelling: \varepsilon
					//tex: $\vec m_i = \vec v' - \delta \vec m_{i-1} - \varepsilon \vec m_{i-2}$
					m = vv;
					m.AddBy(oldM, (-δ).FromDouble<T>());
					if (ε != 0 && olderM is not null)
						m.AddBy(olderM, (-ε).FromDouble<T>());
					//tex: $(\vec {m_\mathbf A})_i = (\vec {m_\mathbf A})_i - \delta (\vec {m_\mathbf A})_{i-1} - \varepsilon (\vec {m_\mathbf A})_{i-2}$
					Am.AddBy(oldAm, (-δ).FromDouble<T>());
					if (ε != 0 && olderAm is not null)
						Am.AddBy(olderAm, (-ε).FromDouble<T>());
					// change other scalars
					γbar = Si * δbar - cs * α;
					ε = Si * β;
					δbar = -cs * β;
					γ = Math.Sqrt(γbar * γbar + β * β);
					// scale m, Am, cs and Si
					γInv = (1 / γ).FromDouble<T>();
					m.Scale(γInv);
					Am.Scale(γInv);
					cs = γbar / γ;
					Si = β / γ;
					#endregion

					#region check stagnation
					double prodSiCs = prodSi * cs;
					if (prodSiCs == 0 || Math.Abs(prodSiCs) * m.Norm() < Const<T>.MachinePrecision * x.Norm())
						stagnations++;
					else
						stagnations = 0;
					#endregion

					#region update solution x
					x.AddBy(m, prodSiCs.FromDouble<T>());
					oldProdSi = prodSi;
					prodSi *= Si;
					if (preconditioner is not null)
					{
						r.AddBy(Am, (-prodSiCs).FromDouble<T>());
						normR = r.Norm();
					}
					else
					{
						normR = Math.Abs(prodSi);
					}
					#endregion

					#region check for convergence
					if (normR <= realTolerance)
					{
						Common.RSetToBSubAx<TVec, T>(matrix, ref r, x, rightSide);
						normR = r.Norm();
						if (normR <= realTolerance)
						{   // actually converges
							minResidual = normR;
							success = true;
							break;
						}
					}
					if (stagnations >= maxStagnation)
					{
						success = false;
						break;
					}
					// otherwise
					if (normR < minResidual)
					{
						minResidual = normR;
						if (solution != x)
							solution?.Dispose();
						solution = x;
					}
					#endregion
				}
				#endregion

				#region return solution of first one with minimal residual
				if (success)
				{
					(solution, x) = (x, solution);
					minResidual /= normB;
				}
				else
				{
					Common.RSetToBSubAx<TVec, T>(matrix, ref r, solution, rightSide);
					normR = r.Norm();
					if (normR <= minResidual)
					{
						minResidual = normR / normB;
					}
					else
					{
						minResidual /= normB;
						(solution, x) = (x, solution);
					}
				}
				Log.Write(string.Format(Resource.MinResFinish, minResidual, tolerance));
				return (minResidual, solution);
				#endregion
			}
			#region dispose
			catch (Exception)
			{
				solution?.Dispose();
				throw;
			}
			finally
			{
				if (r != solution)
					r?.Dispose();
				if (x != solution)
					x?.Dispose();
				if (v != solution)
					v?.Dispose();
				if (vv != solution)
					vv?.Dispose();
				if (oldV != solution)
					oldV?.Dispose();
				if (olderV != solution)
					olderV?.Dispose();
				if (Am != solution)
					Am?.Dispose();
				if (oldAm != solution)
					oldAm?.Dispose();
				if (olderAm != solution)
					olderAm?.Dispose();
				if (m != solution)
					m?.Dispose();
				if (oldM != solution)
					oldM?.Dispose();
				if (olderM != solution)
					olderM?.Dispose();
			}
			#endregion
			
		}
		#endregion
	}
}
