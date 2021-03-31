using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

using Althea.Arrays;
using Althea.Backend.Storage;
using Althea.Helpers;
using Althea.LinearAlgebra;
using Althea.Linq;
using Althea.NativeTypes;
using Althea.Solver;
using Althea.Storage;

using LAD = Althea.LinearAlgebra.Dense.AbstractApi;


namespace Althea.Backend.CSharp.Solver
{
	internal static class Common
	{
		#region parameters check
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static SpanList<T> ClearList<T>(this SpanList<T> list) where T : IDisposable
		{
			list.Clear(static elem => elem?.Dispose());
			return list;
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
				estimateIter = Math.Min(estimateIter, 35);
			else
				estimateIter = Math.Min(estimateIter, 25);
			if (maxIter <= 0)
				maxIter = estimateIter;
		}
		#endregion

		#region gap
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static double GetGap(double beta, double tol, ReadOnlySpan<ComplexDouble> vals, ReadOnlySpan<ComplexDouble> vecsLastRow, int target = 0, ReadOnlySpan<int> conjugatePairs = default)
		{
			double normA = vals.Max(static v => v.Abs());
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
				r.AddBy(q, weights[i].GenericNegate());
			}
			if (!robust || len <= 4)
				return;

			// one more time will be enough in most cases
			for (int i = len - 1; i >= 0; i--)
			{
				var q = qs[i];
				var dot = q.Dot(r);
				if (dot.ToDouble() == 0)
					continue;
				weights[i] = weights[i].GenericAdd(dot);
				r.AddBy(q, dot.GenericNegate());
			}
			return;
		}
		#endregion
	}

	internal static class LanczosBasedSolver
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
			var alpha = q0.Dot(r);
			α0 = alpha.ToDouble();
			//tex:$\vec r = \vec r - \alpha_0 \vec q_0$
			r.AddBy(q0, alpha.GenericNegate());
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
						info.ResidualScalars[i] = w[i].ToDouble();
					}
				}
			}
			//tex:$\alpha_0 = \vec q^* \vec r$
			var alpha = q0.Dot(r);
			αs.Add(alpha.ToDouble());
			//tex:$\vec r = \vec r - \alpha_0 \vec q_0$
			r.AddBy(q0, alpha.GenericNegate());
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
			αs.Add(newq.Dot(r).ToDouble());
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
		private unsafe static void LanczosTridiagSolve(SpanList<double> αs, SpanList<double> βs, Span<double> eigval, Span<double> eigvec, int vecLD = 0, int firstNResidual = 0)
		{
			// fill matrix
			int N = αs.Count;
			if (vecLD == 0)
				vecLD = N;
			if (firstNResidual > 0)
			{
				// fill the non-tridiagonal part
				for (int i = 0; i < firstNResidual; i++)
				{
					eigvec[i + vecLD * i] = αs[i];
					eigvec[firstNResidual + vecLD * i] = eigvec[i + vecLD * firstNResidual] = βs[i];
				}
			}
			for (int i = firstNResidual; i < N; i++)
			{
				eigvec[i + vecLD * i] = αs[i];
				if (i < N - 1)
				{
					eigvec[i + vecLD * (i + 1)] = βs[i];
					eigvec[(i + 1) + vecLD * i] = βs[i];
				}
			}
			// check NaN
			if (αs.AsSpan().Any(static a => double.IsNaN(a)) || βs.AsSpan().Any(static b => double.IsNaN(b)))
				throw new ArithmeticException(Resources.Other.NanOccured);
			// tridiagonal solve
			fixed (double* matPtr = eigvec, valPtr = eigval)
			{
				var tridiag = new PureStorage<double>(MemoryPointer.Create<double>(new(matPtr), vecLD * N));
				var valsOut = new PureStorage<double>(MemoryPointer.Create<double>(new(valPtr), vecLD * N));
				if (TridiagSolve is null)
				{
					LAD? pre = LAD.Current;
					LAD.EigenSpecialMatrixHermitian(SolveVectorMode.Vector, N, valsOut, tridiag, vecLD);
					LAD? now = LAD.Current;
					if (pre is not null && pre != now)
					{
						Settings.DenseLinearAlgebraImplementation = pre.GetType();
					}
					if (now is not null)
					{
						try
						{
							TridiagSolve = typeof(LAD).GetMethod(nameof(LAD.EigenSpecialMatrixHermitian) + "_", System.Reflection.BindingFlags.NonPublic)?
													  .CreateDelegate<EigensolveDelegate>();
						}
						catch (Exception)
						{
							Log.Write(string.Format(Resource.CannotCreateDelegate, nameof(LAD) + "." + nameof(LAD.EigenSpecialMatrixHermitian)), level: LogLevel.Warning);
						}
					}
				}
				else
				{
					TridiagSolve.Invoke(SolveVectorMode.Vector, N, valsOut, tridiag, vecLD);
				}
			}
		}
		#endregion

		#region restart info
		private readonly ref struct RestartBasicInfo<TVec, T>
			where TVec : class, IKrylovVector<TVec, T>, new()
			where T : unmanaged
		{
			internal readonly TVec ResidualVec;

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
			internal RestartBasicInfo(TVec residual, Span<double> scalarHolder1, Span<double> scalarHolder2, Span<TVec> vectorHolder1, SpanList<TVec> converged)
			{
				this.ResidualVec = residual;
				this.ResidualScalars = new(scalarHolder1);
				this.UnconvergedEigenvalues = new(scalarHolder2);
				this.UnconvergedEigenvectors = new(vectorHolder1);
				this.ConvergedEigenvectors = converged;
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
							r.AddBy(q, q.Dot(r).GenericNegate());
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
							r.AddBy(q, q.Dot(r).GenericNegate());
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
							r.AddBy(q, q.Dot(r).GenericNegate());
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
		private static (bool converge, string message, string trace) LanczosConvergenceCheck(ReadOnlySpan<double> eigval, ReadOnlySpan<double> eigvec, int vecLD, double beta, double tol, int nConverged, bool useGap)
		{
			var iter = eigval.Length - 1;
			// get θ_0  S_j,0 for convergence check
			Span<ComplexDouble> lastRow = stackalloc ComplexDouble[eigval.Length], complexVal = stackalloc ComplexDouble[eigval.Length];
			for (int i = 0; i < eigval.Length; i++)
			{
				lastRow[i] = eigvec[(i + 1) * vecLD - 1];
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
		private static TVec GetRealEigenvector<TVec, T>(TVec r, SpanList<TVec> Q, Span<double> eigvecs)
			where TVec : class, IKrylovVector<TVec, T>, new()
			where T : unmanaged
		{
			Span<T> eigvec = stackalloc T[Q.Count];
			eigvecs[..Q.Count].CopyTo(eigvec, static e => e.FromDouble<T>());
			return r.OperateOn(Q, eigvec);
		}
		#endregion


			////
		internal static (double val, TVec vec) NaiveLanczos<TVec, T>(Func<TVec, TVec> matrixFunction, TVec initial, int maxIter, bool checkFirst)
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
			Span<IntPtr> tempQ = maxIter.CheckStackLimitFast<IntPtr>() ?? stackalloc IntPtr[maxIter];
			var qs = new SpanList<TVec>(tempQ.AsClassType<TVec>());
			var αs = new SpanList<double>(maxIter.CheckStackLimitFast<double>() ?? stackalloc double[maxIter]);
			var βs = new SpanList<double>(maxIter.CheckStackLimitFast<double>() ?? stackalloc double[maxIter]);
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
					Log.Write($"Krylov subspace algorithm: now at iteration {j}, {stopwatch.Elapsed} passed since last output.", level: LogLevel.Trace);
					if (stopwatch.Elapsed >= TimeSpan.FromSeconds(10))
					{
						Log.Write(string.Format(Resource.IterationAndTimeInfo, j, stopwatch.Elapsed.TotalMinutesString()));
						stopwatch.Restart();
					}
					#endregion

					LanczosMainCalc<TVec, T>(matrixFunction, qs, ref r, ref αs, ref βs);
					if (βs[j] == 0)
						break;
					Log.Write(string.Format(Resource.NaiveLanczosFinish, αs[j], βs[j]));

				}
				#endregion

				#region tridiagonal solve
				int n = qs.Count;
				Span<double> eigenvalues = n.CheckStackLimitFast<double>() ?? stackalloc double[n];
				Span<double> eigenvectors = (n * n).CheckStackLimitFast<double>() ?? stackalloc double[n * n];
				LanczosTridiagSolve(αs, βs, eigenvalues, eigenvectors);
				#endregion

				#region output
				var vecOut = GetRealEigenvector<TVec, T>(r, qs, eigenvectors);
				return (eigenvalues[0], vecOut);
				#endregion
			}
			finally
			{
				r?.Dispose(); initial?.Dispose();
				qs.ClearList();
			}
			#endregion
		}


		////
		internal static bool RestartLanczos<TVec, T>(Func<TVec, TVec> matrixFunction, TVec initial, int maxIter, int iterPerRestart, double tolerance, ReorthogonalizeMethod reorthogonalize, bool useGap, IPreserveSelector selector, bool checkFirst, Span<double> outEigvals, Span<TVec> outEigvecs)
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
			Common.CheckParas<TVec, T>(matrixFunction, initial, smallestK, ref maxIter, herm: true);
			// log start
			Log.Write(string.Format(Resource.RestartLanczosStart, initial.Length, maxIter));

			// stopwatch start
			Stopwatch stopwatchStart = Stopwatch.StartNew(), stopwatch = Stopwatch.StartNew();
			#endregion

			#region initialize
			TVec guess = initial.Clone();
			Span<IntPtr> tempQ = stackalloc IntPtr[iterPerRestart];
			var qs = new SpanList<TVec>(tempQ.AsClassType<TVec>());
			Span<double> eigvals = stackalloc double[iterPerRestart];
			Span<double> eigvecs = (iterPerRestart * iterPerRestart).CheckStackLimitFast<double>() ?? stackalloc double[iterPerRestart * iterPerRestart];

			Span<double> tempHolder1 = stackalloc double[iterPerRestart], tempHolder2 = stackalloc double[iterPerRestart];
			Span<IntPtr> tempQ1 = stackalloc IntPtr[iterPerRestart]; Span<IntPtr> tempQ2 = stackalloc IntPtr[smallestK];
			var restartInfo = new RestartBasicInfo<TVec, T>(residual: guess, tempHolder1, tempHolder2, tempQ1.AsClassType<TVec>(), tempQ2.AsClassType<TVec>());

			SpanList<double> alphas = new(stackalloc double[iterPerRestart]), betas = new(stackalloc double[iterPerRestart]);
			#endregion

			#region flow control
			SpanList<double> eigenvalues = new(stackalloc double[smallestK]);
			while (true)
			{
				// calculate
				var converged = RestartLanczosInner(matrixFunction, iterPerRestart, tolerance, reorthogonalize, useGap, ref restartInfo, eigvals, eigvecs, ref qs, ref alphas, ref betas, out TVec r);
				var rNorm = betas[^1]; // TODO: validate ^1

				#region if converge
				if (converged)
				{
					// output newest eigenvalue
					Log.Write($"The newest unconverged eigenvalue is {eigvals[0]}", level: LogLevel.Trace);
					// calculate last eigenvector
					eigenvalues.Add(eigvals[0]);
					var newConverged = GetRealEigenvector<TVec, T>(r, qs, eigvecs);
					newConverged.Normalize();
					restartInfo.ConvergedEigenvectors.Add(newConverged);
					// remove converged from eigen pairs
					eigvals = eigvals[1..];
					eigvecs = eigvecs[1..];

					// if all converged
					if (restartInfo.ConvergedEigenvectors.Count >= smallestK)
					{
						try
						{
							Log.Write($"Converged after total {(totalIterLeft < 0 ? -totalIterLeft : totalIter - totalIterLeft)} iterations.");
							var outputEigenvectors = new TVec[restartInfo.ConvergedEigenvectors.Count];
							restartInfo.ConvergedEigenvectors.CopyTo(outputEigenvectors, 0);
							return (eigenvalues.ToArray(), outputEigenvectors, true);
						}
						finally
						{
							Q.ClearList();
							r.Dispose();
							restartInfo.ResidualVec.Dispose();
							restartInfo.UnconvergedEigenvectors.ClearList();
						}
					}
				}
				#endregion

				#region check maxIter for stop
				if (stopAtMaxIter && maxIter >= totalIterLeft)
				{
					if (!converged)
					{
						try
						{
							Log.Write($"The {eigenvalues.Count.ToOrdinal()} eigen-pair (and larger ones) is (are) not converged.", level: LogLevel.Warning);
							var outputEigenvectors = new TVec[restartInfo.ConvergedEigenvectors.Count];
							restartInfo.ConvergedEigenvectors.CopyTo(outputEigenvectors, 0);
							return (eigenvalues.ToArray(), outputEigenvectors, false);
						}
						finally
						{
							Q.ClearList();
							r.Dispose();
							restartInfo.UnconvergedEigenvectors.ClearList();
						}
					}
				}
				#endregion
				// else, prepare for next while

				#region prepare for restart
				// subtract spent iterations
				totalIterLeft -= maxIter - restartInfo.ResidualScalars.Count;
				// matrix multiply vector function already checked, do not wast time
				Common.CheckLanczosParas<TVec, T>(null, null, size, smallestK, ref maxIter);
				// check max iterations again
				if (maxIter < smallestK)
				{
					Log.Write($"The calculated `{nameof(maxIter)}` is smaller than the desired `{nameof(smallestK)}`, it may never converge.", level: LogLevel.Error);
				}
				#endregion
				#region log output
				if (stopwatch.Elapsed >= Log.LanczosInfoInterval)
				{
					Log.Write($"Total {(totalIterLeft < 0 ? -totalIterLeft : totalIter - totalIterLeft),4} iterations were executed, {stopwatchStart.Elapsed.TotalMinutesString()} passed since start.");
					stopwatch.Restart();
				}
				#endregion

				#region restart
				try
				{
					// select the Ritz pairs to preserve
					var delegatePreserve = strategy == RestartStrategy.UserDefine ? (IRestartStrategy.DelegatePreserveSelect)selector.PreserveSelect : new BuiltInRestartStrategy(strategy).PreserveSelect;
					var preserveIndices = delegatePreserve(Array.ConvertAll(eigvals, a => (DoubleComplex)a), Array.ConvertAll(eigvecs, a => Array.ConvertAll(a, v => (DoubleComplex)v)), restartInfo.ConvergedEigenvectors.Count, smallestK, maxIter);
					if (preserveIndices.Length == 1)
						Log.Write($"Restarting with only one preserved Ritz pair may never improve the result.", level: LogLevel.Warning);
					/*
					// cannot dispose old unconverged vectors and residual vector since they are in Q now
					restartInfo.UnconvergedEigenvectors.ClearList<TVec>();
					restartInfo.ResidualVec.ForceDispose();
					*/
					// preserve converged vectors
					var convergedVecs = restartInfo.ConvergedEigenvectors;
					restartInfo = new RestartBasicInfo<TVec, T>(residual: r, converged: convergedVecs);
					//tex:$\vec{r}$ so that $A\vec{y}_i - \vartheta_i\vec{y}_i = \sigma_i\vec{r}$
					var lastRow = eigvecs.Select(v => v[^1]).ToArray();
					// calculate new unconverged Ritz vectors
					foreach (var i in preserveIndices)
					{
						//tex:$\sigma_i=\beta_0 s_{k,i}$
						restartInfo.ResidualScalars.Add(lastRow[i] * rNorm);
						//tex:$\vartheta_i$ and $\vec{y}_i = Q \vec{s}_i$
						restartInfo.UnconvergedEigenvalues.Add(eigvals[i]);
						var tempSvec = Array.ConvertAll(eigvecs[i], a => a.FromDouble<T>());
						var unconverged = r.OperateOn(Q, tempSvec);
						unconverged.Normalize();
						restartInfo.UnconvergedEigenvectors.Add(unconverged);
					}
				}
				catch (Exception)
				{
					r.Dispose();
					restartInfo.ResidualVec.Dispose();
					restartInfo.UnconvergedEigenvectors.ClearList();
					throw;
				}
				finally
				{
					Q.ClearList();
				}
				#endregion
				// go to next while
			}
			#endregion
		}


		private static bool RestartLanczosInner<TVec, T>(Func<TVec, TVec> matrixFunction, int nIter, double tolerance, ReorthogonalizeMethod reorthogonalize, bool useGap, ref RestartBasicInfo<TVec, T> restartInfo, Span<double> eigvals, Span<double> eigvecs, ref SpanList<TVec> qs, ref SpanList<double> αs, ref SpanList<double> βs, out TVec r)
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
					if (reorthogonalize == ReorthogonalizeMethod.Selective)
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
					else if (reorthogonalize == ReorthogonalizeMethod.Full || reorthogonalize == ReorthogonalizeMethod.RobustFull)
					{
						Common.RobustOrthogonalize<TVec, T>(r, qs, default, reorthogonalize == ReorthogonalizeMethod.RobustFull);
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
						LanczosTridiagSolve(αs, βs, eigvals, eigvecs, vecLD: nIter, firstNResidual: NRitz);
						converge = true;
						break;
					}
					Log.Write($"Main calculation finished, α = {αs[j]}, β = {βs[j]}", level: LogLevel.Trace);
					#endregion

					#region construct tridiagonal and convergence check
					if (j > 2 || nIter - j <= 2)
					{
						// construct tridiagonal matrix and calculate eigenvalue
						LanczosTridiagSolve(αs, βs, eigvals, eigvecs, vecLD: nIter, firstNResidual: NRitz);
						double phi = machinePrecision * 2 * Math.Sqrt(Math.Max(Math.Abs(eigvals[0]), Math.Abs(eigvals[^1])));
						string message, trace;
						// convergence check
						(converge, message, trace) = LanczosConvergenceCheck(eigvals, eigvecs, vecLD: nIter, beta: βs[^1], tolerance, nConverged: restartInfo.ConvergedEigenvectors.Count, useGap);
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
					if (reorthogonalize == ReorthogonalizeMethod.Selective)
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
	}
}
