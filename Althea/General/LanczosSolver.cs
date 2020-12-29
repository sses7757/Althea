using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using RT = Althea.Runtime.API;
using Althea.Memory;
using Althea.Linq;
using Althea.Arrays;


namespace Althea.General
{
	#region common

	#region restart strategy
	/// <summary>
	/// The strategy adopted by the thick restart Lanczos and Krylov-Shcur algorithm.
	/// </summary>
	public enum RestartStrategy
	{
		/// <summary>
		/// The naïve strategy which only preserve the lowest Ritz eigen-pair and converged ones.
		/// </summary>
		Naive,
		// Ignore Spelling: \mathrm eig \left \right \underset
		/// <summary>
		/// Based on the index of Ritz eigen-pairs, preserve the smallest $k$ ones: <br/>
		/// $$k=n_c+\min{\left\{n_{\mathrm{eig}},\left(p-n_c\right)\left(\frac{2}{5}+\frac{n_{\mathrm{eig}}}{10p}\right)\right\}}$$
		/// </summary>
		//tex:$$k=n_c+\min{\left\{n_{\mathrm{eig}},\left(p-n_c\right)\left(\frac{2}{5}+\frac{n_{\mathrm{eig}}}{10p}\right)\right\}}$$
		IndexBased,
		/// <summary>
		/// Based on the residual of Ritz eigen-pairs, preserve the smallest $k$ ones: <br/>
		/// $$k=\underset{i}{\mathrm{argmax}}\left( \left| s_{n,i} \right| &lt; \max\left\{ \sqrt{\left| s_{n,n_{\mathrm{eig}}} \right|\max_{j}\left| s_{n,j} \right|},2\left| s_{n,n_{\mathrm{eig}}} \right| \right\} \right)$$
		/// </summary>
		//tex:$$k=\underset{i}{\mathrm{argmax}}\left( \left| s_{n,i} \right| < \max\left\{ \sqrt{\left| s_{n,n_{\mathrm{eig}}} \right|\max_{j}\left| s_{n,j} \right|},2\left| s_{n,n_{\mathrm{eig}}} \right| \right\} \right)$$
		CurrentResidualBest,
		/// <summary>
		/// Based on the improvement of residual of Ritz eigen-pairs of single iteration after the restart, preserve the smallest $k$ ones: <br/>
		/// $$k=\max{\left\{n_{\mathrm{eig}},\frac{3p+2n_c}{5}\right\}}$$
		/// </summary>
		//tex:$$k=\max{\left\{n_{\mathrm{eig}},\frac{3p+2n_c}{5}\right\}}$$
		OneStepResidualImprove,
		/// <summary>
		/// Based on the improvement of residual of Ritz eigen-pairs of single iteration after the restart, preserve the smallest $k$ ones: <br/>
		/// $$k=\underset{k}{\max}{\left(p-k\right)\frac{\lambda_{k+1}-\lambda_1}{\lambda_m-\lambda_1}}$$
		/// </summary>
		//tex:$$k=\underset{k}{\max}{\left(p-k\right)\frac{\lambda_{k+1}-\lambda_1}{\lambda_m-\lambda_1}}$$
		WholeIterResidualImprove,
		/// <summary>
		/// The heuristic used by Krylov-Schur algorithm to prevent stagnating
		/// </summary>
		KrylovSchur,
		/// <summary>
		/// User defined strategy
		/// </summary>
		UserDefine,
	}

	/// <summary>
	/// The restart strategy interface
	/// </summary>
	public interface IRestartStrategy
	{
		/// <summary>
		/// Select the indices of which Ritz pairs to preserve.
		/// </summary>
		/// <param name="estimateEigvals">Ritz values, without converged ones</param>
		/// <param name="estimateEigvecs">Ritz vectors, without converged ones</param>
		/// <param name="NConverged">number of converged eigen-pairs</param>
		/// <param name="NTarget">the number of smallest eigen-pairs wanted</param>
		/// <param name="maxIter">max number of iteration</param>
		/// <returns>The indices to preserve <paramref name="estimateEigvals"/> and columns of <paramref name="estimateEigvecs"/></returns>
		int[] PreserveSelect(DoubleComplex[] estimateEigvals, DoubleComplex[][] estimateEigvecs, int NConverged, int NTarget, int maxIter);

		internal delegate int[] DelegatePreserveSelect(DoubleComplex[] estimateEigvals, DoubleComplex[][] estimateEigvecs, int NConverged, int NTarget, int maxIter);
	}

	internal struct BuiltInRestartStrategy : IRestartStrategy
	{
		private readonly RestartStrategy strategy;

		internal BuiltInRestartStrategy(RestartStrategy strategy)
		{
			this.strategy = strategy;
		}

		public int[] PreserveSelect(DoubleComplex[] estimateEigvals, DoubleComplex[][] estimateEigvecs, int NConverged, int NTarget, int maxIter)
		{
			int indexMax = 0;
			int upperCount = estimateEigvals.Length * 2 / 3;
			switch (this.strategy)
			{
				case RestartStrategy.Naive:
					return ArrayLinq.Range(0, Math.Min(Math.Max(maxIter * 2 / 5, NTarget), estimateEigvals.Length)).ToArray();
				case RestartStrategy.IndexBased:
					indexMax = Math.Min(NTarget, (int)((maxIter - NConverged) * (0.4 + NTarget / 10.0 / maxIter)));
					return ArrayLinq.Range(0, Math.Min(indexMax, upperCount)).ToArray();
				case RestartStrategy.CurrentResidualBest:
					if (NTarget >= upperCount)
						return ArrayLinq.Range(0, upperCount).ToArray();
					var lastRow = estimateEigvecs.Select(v => v[^1]).ToArray();
					var lastMax = lastRow.Max(a => a.Abs());
					var lastNeig = lastRow[NTarget - 1].Abs();
					var upperBound = Math.Max(Math.Sqrt(lastMax * lastNeig), 2 * lastNeig);
					for (indexMax = 0; indexMax < upperCount; indexMax++)
					{
						if (lastRow[indexMax].Abs() >= upperBound)
							break;
					}
					indexMax -= NConverged;
					return ArrayLinq.Range(0, indexMax - 1).ToArray();
				case RestartStrategy.OneStepResidualImprove:
					indexMax = Math.Max(NTarget, (int)(0.6 * maxIter + 0.4 * NConverged));
					indexMax -= NConverged;
					return ArrayLinq.Range(0, Math.Min(indexMax, upperCount)).ToArray();
				case RestartStrategy.WholeIterResidualImprove:
					upperCount = Math.Max(NTarget, (int)(0.6 * maxIter + 0.4 * NConverged));
					double maxVal = 0;
					for (int i = 0; i < upperCount; i++)
					{
						double val = (maxIter - i - 1) * (estimateEigvals[i + 1].Abs() - estimateEigvals[0].Abs()) / (estimateEigvals[^1].Abs() - estimateEigvals[0].Abs());
						if (val > maxVal)
						{
							maxVal = val;
							indexMax = i;
						}
					}
					indexMax -= NConverged;
					return ArrayLinq.Range(0, indexMax).ToArray();
				case RestartStrategy.KrylovSchur:
					int k = NTarget + Math.Min(NConverged, (maxIter - NTarget) / 2);
					if (k == 1 && maxIter > 3)
						k = maxIter / 2;
					return ArrayLinq.Range(0, k).ToArray();
				default:
					throw new NotSupportedException();
			}
		}
	}
	#endregion

	#region re-orthogonalize
	/// <summary>
	/// The method used to re-orthogonalize with the previous basis in Lanczos
	/// </summary>
	public enum ReorthogonalizeMethod
	{
		/// <summary>
		/// Selective re-orthogonalize, let the internal heuristic to determine when and which basis to re-orthogonalize
		/// </summary>
		Selective = 0,
		/// <summary>
		/// Do not perform re-orthogonalization, <b>this may lead to serious problems, e.g. Lanczos may never converge</b>
		/// </summary>
		None = -1,
		/// <summary>
		/// Perform full re-orthogonalization at each iteration, this may lead to extra performance loss, especially when the problem size is small. You can use this method when the <see cref="Selective"/> one does not perform well
		/// </summary>
		Full = 1,
		/// <summary>
		/// Perform robust full re-orthogonalization at each iteration, this may lead to extra performance loss, especially when the problem size is small. You can use this method when the <see cref="Selective"/> one does not perform well
		/// </summary>
		RobustFull = 2
	}
	#endregion

	/// <summary>
	/// Some common methods and constants
	/// </summary>
	public static class Common
	{
		#region precision
		/// <summary>
		/// Double type machine precision
		/// </summary>
		public const double DoubleMachinePrecision = 2.220446049250313E-16D;

		/// <summary>
		/// Single type machine precision
		/// </summary>
		public const float SingleMachinePrecision = 1.1920928955078125E-07F;

		/// <summary>
		/// Get the machine precision of <typeparamref name="T"/>
		/// </summary>
		/// <typeparam name="T">the data type</typeparam>
		/// <returns></returns>
		public static double MachinePrecisionOf<T>() where T : struct, IComparable<T>
		{
			var type = default(T).ToDataType();
			if (!type.IsFloat())
				throw new NotSupportedException();
			if (type.Bytes() == 4)
				return SingleMachinePrecision;
			else if (type.Bytes() == 8)
				return DoubleMachinePrecision;
			else
				throw new NotSupportedException();
		}
		internal static double GetPrecision<T>(double tolerance) where T : struct, IComparable<T>
		{
			double ε = MachinePrecisionOf<T>();
			if (tolerance < ε)
				tolerance = ε * 2;
			return tolerance;
		}
		#endregion

		#region parameters check
		internal static void CheckLanczosParas<TVec, T>(Func<TVec, TVec> MatMulVecFunc, TVec initial, long size, int smallestK, ref int maxIter, bool herm = true) where TVec : AbstractArray<T>, IKrylovVector<TVec, T>, new() where T : struct, IComparable<T>
		{
			// check MatMulVecFunc
			if (MatMulVecFunc != null)
			{
				try
				{
					using var testOutput = MatMulVecFunc(initial);
					if (testOutput.Length != size)
						throw new ArgumentException(string.Format(Resource.Culture, Resource.VectorLength, size), nameof(MatMulVecFunc));
				}
				catch (Exception e)
				{
					if (e is ArgumentException ee && ee.ParamName == nameof(MatMulVecFunc))
						throw;
					else
						throw new System.Reflection.TargetInvocationException(e);
				}
			}

			// check smallest k
			if (smallestK <= 0 || smallestK > size)
				throw new ArgumentOutOfRangeException(nameof(smallestK));
			if (smallestK >= Convert.ToInt32(Math.Sqrt(size)))
			{
				Log.Write($"The smallest {smallestK} eigenvalues you want is larger than the desired one, maybe the CUDA eigen solver is a better option.", category: "Lanczos", level: LogLevel.Warning);
			}

			// estimate iteration number
			int estimateIter = Math.Min(maxIter <= 0 ? int.MaxValue : maxIter, Convert.ToInt32(Math.Sqrt(size)));
			if (herm)
				estimateIter = Math.Min(estimateIter, GlobalSettings.IterPerRestartHardLimitHerm);
			else
				estimateIter = Math.Min(estimateIter, GlobalSettings.IterPerRestartHardLimitNonHerm);
			/*
			if (estimateIter < Log.LanczosWarningThreshold)
			{
				Log.Write($"The estimated iteration number {estimateIter} is too small, maybe the CUDA eigen solver is a better option.", category: "Lanczos", level: MsgLevel.Warning);
				estimateIter = (int)size;
			}
			if (maxIter > 0 && maxIter <= estimateIter / 10)
			{
				Log.Write($"the input max iteration number {maxIter} is too small, it will be increased automatically.", category: "Lanczos", level: MsgLevel.Warning);
				maxIter = estimateIter / 10;
			}
			*/
			if (maxIter <= 0) maxIter = estimateIter;

			// estimate memory cost
			var freeMem = RT.HostFreeMemory;
			if (initial is PureArray<T> p && !p.OnHost)
				freeMem = RT.DeviceFreeMemory;
			var estMem = size * maxIter * Storage<T>.SizeOfT;
			if (freeMem * GlobalSettings.FreeMemoryRatio <= estMem)
			{
				Log.Write($"the estimated memory occupation {(estMem / 1024.0 / 1024.0):N1} MiB is to large to fit in the free memory, 'maxIter' will be decreased automatically.", category: "Eigen-solver", level: LogLevel.Warning);
				maxIter = Convert.ToInt32(freeMem * GlobalSettings.FreeMemoryRatio / size / Storage<T>.SizeOfT);
			}
		}
		#endregion

		#region gap
		internal static double GetGap(double beta, double tol, DoubleComplex[] vals, DoubleComplex[] vecsLastRow, int target = 0, int[] conjugatePairs = null)
		{
			double normA = vals.Max(v => v.Abs());
			double normTol = 2 * normA * Math.Sqrt(tol); // 2 for error upper bound, 1 for average
			var targetVal = vals[target];
			var targetSji = vecsLastRow[target];
			var targetEta = targetSji.Abs() * beta;
			double gap = double.MaxValue;
			for (int i = 0; i < vals.Length; i++)
			{
				if (conjugatePairs != null && conjugatePairs[i] == conjugatePairs[target] && conjugatePairs[i] != 0)
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
			if (gap == double.MaxValue) gap = vals.Min(e => Math.Max(normTol, e.Abs()));
			return gap;
		}
		#endregion

		#region orthogonalization
		/// <summary>
		/// Robust orthogonalize <paramref name="r"/> against all vectors in list <paramref name="qs"/>.
		/// </summary>
		/// <typeparam name="T">the data type, see <see cref="AbstractArray{T}"/> for more information</typeparam>
		/// <typeparam name="TVec">the general dense vector type that inherits <see cref="AbstractArray{T}"/>, <see cref="IKrylovVector{TVec, T}"/> and must be a concrete class type</typeparam>
		/// <param name="r">the vector (<typeparamref name="TVec"/>) to orthogonalize <b>in-place</b></param>
		/// <param name="qs">the vectors (list of <typeparamref name="TVec"/>) to orthogonalize against</param>
		/// <param name="robust">perform robust orthogonalization or normal one</param>
		/// <returns>the coefficients of <c>&lt;<paramref name="r"/>,<paramref name="qs"/>&gt;</c></returns>
		public static T[] RobustOrthogonalize<TVec, T>(TVec r, IReadOnlyList<TVec> qs, bool robust = true)
			where TVec : AbstractArray<T>, IKrylovVector<TVec, T>, new()
			where T : struct, IComparable<T>
		{
			if (qs is null || qs.Count == 0)
				return Array.Empty<T>();
			T[] w = new T[qs.Count];
			for (int i = qs.Count - 1; i >= 0; i--)
			{
				var q = qs[i];
				w[i] = q.Dot(r);
				r.AddBy_αx(q, w[i].GenericNegate());
			}
			if (!robust || qs.Count <= 4)
				return w;

			// one more time will be enough in most cases
			for (int i = qs.Count - 1; i >= 0; i--)
			{
				var q = qs[i];
				var dot = q.Dot(r);
				if (dot.ToDouble() == 0)
					continue;
				w[i] = w[i].GenericAdd(dot);
				r.AddBy_αx(q, dot.GenericNegate());
			}
			return w;
		}
		#endregion
	}

	#endregion


	/// <summary>
	/// The Lanczos solvers API.
	/// </summary>
	public static class LanczosSolver
	{
		#region used by all Lanczos algorithms
		private readonly struct LanczosInfo
		{
			internal readonly ReorthogonalizeMethod Reorthogonalize;

			internal readonly bool OrthogonalizeConverged;

			internal readonly bool UseGapRatherThanMatrixNorm;

			internal readonly double Tolerance;

			internal LanczosInfo(ReorthogonalizeMethod reorthogonalize, bool orthogonalizeConverged, bool useGap, double tolerance)
			{
				this.OrthogonalizeConverged = orthogonalizeConverged;
				this.Reorthogonalize = reorthogonalize;
				this.UseGapRatherThanMatrixNorm = useGap;
				this.Tolerance = tolerance;
			}
		}
		#endregion


		#region struct
		private readonly struct RestartBasicInfo<TVec, T> where TVec : AbstractArray<T>, IKrylovVector<TVec, T>, new() where T : struct, IComparable<T>
		{
			internal readonly List<double> ResidualScalars;

			internal readonly TVec ResidualVec;

			internal readonly List<double> UnconvergedEigenvalues;

			internal readonly List<TVec> UnconvergedEigenvectors;

			internal readonly List<TVec> ConvergedEigenvectors;

			internal RestartBasicInfo(TVec residual)
			{
				this.ResidualScalars = new List<double>();
				this.UnconvergedEigenvalues = new List<double>();
				this.ResidualVec = residual;
				this.UnconvergedEigenvectors = new List<TVec>();
				this.ConvergedEigenvectors = new List<TVec>();
			}

			internal RestartBasicInfo(TVec residual, List<TVec> converged)
			{
				this.ResidualScalars = new List<double>();
				this.UnconvergedEigenvalues = new List<double>();
				this.ResidualVec = residual;
				this.UnconvergedEigenvectors = new List<TVec>();
				this.ConvergedEigenvectors = converged;
			}
		}
		#endregion

		#region orthogonality tracker
		private readonly struct OrthogonalityTracker
		{
			internal readonly List<double> pre;
			internal readonly List<double> now;
			private readonly double explicitValue;
			private readonly int convergedCount, unconvergedCount;

			internal OrthogonalityTracker(double ψ, int convergedRitz, int unconvergedRitz)
			{
				this.explicitValue = ψ;
				this.convergedCount = convergedRitz;
				this.unconvergedCount = unconvergedRitz;
				this.pre = new List<double>();
				this.now = new List<double>();
				if (unconvergedRitz + convergedRitz >= 1)
				{
					pre.AddRange(ArrayLinq.Repeat(100 * ψ, unconvergedRitz + convergedRitz - 1));
					// 100 for not estimated orthogonality loss of Ritz vector
					pre.Add(ψ);
				}
				now.AddRange(ArrayLinq.Repeat(100 * ψ, unconvergedRitz + convergedRitz));
				now.Add(ψ); // +1 for iteration No.0
				pre.Add(1);
				now.Add(1);
			}

			internal string Reorthonalize<TVec, T>(TVec r, IReadOnlyList<TVec> qs, IReadOnlyList<TVec> converged, double thre1, double thre2) where TVec : AbstractArray<T>, IKrylovVector<TVec, T>, new() where T : struct, IComparable<T>
			{
				var stringBuilder = new StringBuilder("\tre-orthogonalize the new basis vector to the ");
				bool hasContent = false;
				if (this.now.SkipLast(1).Any(w => w >= thre1))
				{
					hasContent = true;
					// previous Krylov basis
					//tex:$\vec{r}=\vec{r}-\sum_{k}{\vec{q}(\vec{q}_k^* \vec{r})}$
					for (int k = this.unconvergedCount; k < qs.Count; k++)
					{
						int i = k; // the index of q
						int j = i + this.convergedCount; // the index of this ω
						if (this.now[j] >= thre2)
						{
							stringBuilder.Append(k.ToOrdinal());
							stringBuilder.Append(", ");
							var q = qs[i];
							r.AddBy_αx(q, q.Dot(r).GenericNegate());
							this.now[j] = this.explicitValue;
						}
					}
				}

				// should be more careful with previous converged and unconverged eigenvectors
				if (this.now.Take(this.unconvergedCount + this.convergedCount).Any(w => w >= thre2))
				{
					hasContent = true;
					// previous unconverged eigenvectors
					for (int k = this.convergedCount; k < this.unconvergedCount + this.convergedCount; k++)
					{
						if (this.now[k] >= thre2)
						{
							stringBuilder.Append(k.ToOrdinal());
							stringBuilder.Append(", ");
							var q = qs[k - this.convergedCount];
							r.AddBy_αx(q, q.Dot(r).GenericNegate());
							this.now[k] = this.explicitValue;
						}
					}
					// previous converged eigenvectors
					for (int k = 0; k < this.convergedCount; k++)
					{
						if (this.now[k] >= thre2)
						{
							stringBuilder.Append(k.ToOrdinal());
							stringBuilder.Append(", ");
							var q = converged[k];
							r.AddBy_αx(q, q.Dot(r).GenericNegate());
							this.now[k] = this.explicitValue;
						}
					}
				}
				if (hasContent)
				{
					stringBuilder.Remove(stringBuilder.Length - 2, 1);
					stringBuilder.Append("ones.");
					return stringBuilder.ToString();
				}
				else
					return "";
			}

			internal void ReorthogonalityUpdate(IReadOnlyList<double> αs, IReadOnlyList<double> βs, IReadOnlyList<double> residuals, double φ)
			{
				var ωNew = new double[this.now.Count + 1];
				ωNew[^1] = 1;
				ωNew[^2] = this.explicitValue;
				// the structure of ω is [converged, unconverged, Krylov basis]
				// iteration = unconverged + basis
				var totalCount = this.convergedCount + this.unconvergedCount;
				//tex:$$w_{j+1,k}=\frac{1}{\beta_j}[\beta_k w_{j,k+1}+(\alpha_k-\alpha_j)w_{j,k}+\beta_{k-1}\omega_{j,k-1}-\beta_{j-1}\omega_{j-1,k}]+\phi$$
				for (int k = totalCount; k < αs.Count - 1 + this.convergedCount; k++)
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
					//tex:$$\sum_l{\sigma_l \omega_{j,-l}} = \left| -\alpha_0 \omega_{j,0} - \beta_0 \omega_{j,1} \right| + \vec{q}_j^* \vec{f}_0$$
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
				this.now.Clear(); this.now.AddRange(ωNew);
			}
		}
		#endregion

		#region initialize
		private static void LanczosInit<TVec, T>(Func<TVec, TVec> MatMulVecFunc, ref TVec q0, out TVec r, out double α0, out double β0)
			where TVec : AbstractArray<T>, IKrylovVector<TVec, T>, new() where T : struct, IComparable<T>
		{
			q0.Normalize();
			//tex: $\vec r = A \vec q$
			r = MatMulVecFunc(q0);
			//tex:$\alpha_0 = \vec q^* \vec r$
			var alpha = q0.Dot(r);
			α0 = alpha.ToDouble();
			//tex:$\vec r = \vec r - \alpha_0 \vec q_0$
			r.AddBy_αx(q0, alpha.GenericNegate());
			//tex: $\beta_0=\|\vec r_0\|$
			β0 = r.Norm();
		}

		private static OrthogonalityTracker LanczosInit<TVec, T>(Func<TVec, TVec> MatMulVecFunc, out TVec r, ref List<TVec> qs, List<double> αs, List<double> βs, double ψ, RestartBasicInfo<TVec, T> info)
			where TVec : AbstractArray<T>, IKrylovVector<TVec, T>, new() where T : struct, IComparable<T>
		{
			// deal with restart Ritz vectors
			int NRitz = info.UnconvergedEigenvalues.Count;
			for (int i = 0; i < NRitz; i++)
			{
				qs.Add(info.UnconvergedEigenvectors[i]);
				αs.Add(info.UnconvergedEigenvalues[i]);
				βs.Add(info.ResidualScalars[i]);
			}
			// deal with ω
			var tracker = new OrthogonalityTracker(ψ, info.ConvergedEigenvectors.Count, NRitz);

			// set q0
			var q0 = info.ResidualVec;
			q0.Normalize();
			qs.Add(q0);

			// iteration No.0
			//tex: $\vec r = A \vec q$
			r = MatMulVecFunc(q0);
			if (info.ResidualScalars.Count > 0 && info.ResidualScalars.Count == info.UnconvergedEigenvectors.Count)
			{
				//tex:${\vec{r}}={\vec{r}}-\sum_{i=1}^{n}{\sigma_i{\vec{y}}_i}$
				if ((info.ResidualScalars as IReadOnlyList<double>).Any(s => s <= Math.Pow(ψ, 0.25/*0.5*/)))
				{   // scalar is not accurate now 
					T[] w = Common.RobustOrthogonalize<TVec, T>(r, info.UnconvergedEigenvectors);
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
			r.AddBy_αx(q0, alpha.GenericNegate());
			//tex: $\beta_0=\|\vec r_0\|$
			βs.Add(r.Norm());

			return tracker;
		}
		#endregion

		#region main loop calculation
		private static void LanczosMainCalc<TVec, T>(Func<TVec, TVec> MatMulVecFunc, TVec q, ref TVec r, List<double> αs, List<double> βs, ref TVec newq, bool dispose = true) where TVec : AbstractArray<T>, IKrylovVector<TVec, T>, new() where T : struct, IComparable<T>
		{
			//tex: $\vec v=\vec q$
			/*var v = q;*/
			//tex:$\vec q = \vec r / \beta_{j-1}$
			r.Scale((1 / βs[^1]).FromDouble<T>());
			newq = r;
			//tex: $\vec r = A \vec q$
			r = MatMulVecFunc(newq); // a new vector is generated here
			//tex:$\alpha_j = \vec q^* \vec r$
			αs.Add(newq.Dot(r).ToDouble());
			//tex:$\vec r = \vec r - \alpha_j \vec q - \beta_{j-1} \vec v$
			r.AddBy_αx(newq, (-αs[^1]).FromDouble<T>());
			r.AddBy_αx(q, (-βs[^1]).FromDouble<T>());
			//tex: $\beta_j = \|\vec r\|$
			βs.Add(r.Norm());

			if (dispose) q.Dispose();
		}

		private static void LanczosMainCalc<TVec, T>(Func<TVec, TVec> MatMulVecFunc, IList<TVec> qs, ref TVec r, List<double> αs, List<double> βs)
			where TVec : AbstractArray<T>, IKrylovVector<TVec, T>, new() where T : struct, IComparable<T>
		{
			TVec newq = null;
			LanczosMainCalc<TVec, T>(MatMulVecFunc, qs[^1], ref r, αs, βs, ref newq, dispose: false);
			qs.Add(newq);
		}
		#endregion

		#region tridiagonal matrix
		private static (double[] eigval, double[][] eigvec) LanczosTridiagSolve(List<double> αs, List<double> βs, bool onHost, int firstNResidual = 0)
		{
			var N = αs.Count;
			double[] temp = new double[N * N];
			if (firstNResidual > 0)
			{
				// fill the non-tridiagonal part
				for (int i = 0; i < firstNResidual; i++)
				{
					temp[i + N * i] = αs[i];
					temp[firstNResidual + N * i] = temp[i + N * firstNResidual] = βs[i];
				}
			}
			for (int i = firstNResidual; i < N; i++)
			{
				temp[i + N * i] = αs[i];
				if (i < N - 1)
				{
					temp[i + N * (i + 1)] = βs[i];
					temp[(i + 1) + N * i] = βs[i];
				}
			}

			if (αs.Any(a => double.IsNaN(a)))
			{
				throw new ArithmeticException($"alpha list has {double.NaN}" + Environment.NewLine + string.Join(Environment.NewLine, αs));
			}
			if (βs.Any(b => double.IsNaN(b)))
			{
				throw new ArithmeticException($"beta list has {double.NaN}" + Environment.NewLine + string.Join(Environment.NewLine, βs));
			}

			using var tridiag = new DenseMatrix<double>(N, N, onHost: onHost, herm: true);
			tridiag.FromFortranOrderArray(temp);
			var (valsD, vecsD) = tridiag.EigensystemHerm();
			using (valsD) using (vecsD)
			{
				var vals = valsD.ToFortranOrderArray();
				var vecs = vecsD.ToFortranOrderArray().ToJagged(N);
				return (vals, vecs);
			}
		}
		#endregion

		#region convergence check
		private static (bool converge, string message, string trace) LanczosConvergenceCheck(double[] eigval, double[][] eigvec, double beta, double tol, int NConverged, bool useGap)
		{
			var iter = eigval.Length - 1;
			// get θ_0  S_j,0 for convergence check
			var lastRow = eigvec.Select(v => v[^1]).ToArray();
			double Sj0 = lastRow[0];
			double θ0 = eigval[0];
			var βMulS = beta * Math.Abs(Sj0);
			// get gap
			double gap = useGap ? Common.GetGap(beta, tol, Array.ConvertAll(eigval, e => (DoubleComplex)e), Array.ConvertAll(lastRow, e => (DoubleComplex)e)) : Math.Max(Math.Abs(eigval[0]), Math.Abs(eigval[^1]));
			// test convergence
			bool converge = βMulS / gap <= tol;
			// log convergence
			string message;
			if (converge)
				message = $"The {NConverged.ToOrdinal()} eigen-pair converges.";
			else
				message = "";
			// trace log
			string trace = $"the {NConverged.ToOrdinal()} eigen-pair has: θ = {θ0}, S = {Math.Abs(Sj0)}, γ = {gap}";
			return (converge, message, trace);
		}
		#endregion


		#region naïve Lanczos
		/// <summary>
		/// The naïve Lanczos algorithm to calculate the lowest eigenvalue of the target matrix.
		/// </summary>
		/// <typeparam name="TVec">The concrete vector class type</typeparam>
		/// <typeparam name="T">the data type, see <see cref="AbstractArray{T}"/> for more information</typeparam>
		/// <param name="MatMulVecFunc">the function that represents the multiplication of the target matrix and the </param>
		/// <param name="initial">the initial vector</param>
		/// <param name="maxIter">maximum number of iterations, will be auto decreased if there are not enough memory</param>
		/// <param name="checkFirst">check the <paramref name="MatMulVecFunc"/> and <paramref name="maxIter"/> first, may lead to some extra time usage</param>
		/// <returns>The approximate lowest eigenvalue and the and eigenvector</returns>
		public static (double val, TVec vec) NaiveLanczos<TVec, T>(Func<TVec, TVec> MatMulVecFunc, TVec initial, int maxIter, bool checkFirst = true)
			where TVec : AbstractArray<T>, IKrylovVector<TVec, T>, new()
			where T : struct, IComparable<T>
		{
			if (MatMulVecFunc is null)
				throw new ArgumentNullException(nameof(MatMulVecFunc));
			if (initial is null)
				throw new ArgumentNullException(nameof(initial));
			long size = initial.Length;
			if (checkFirst)
				Common.CheckLanczosParas<TVec, T>(MatMulVecFunc, initial, size, 1, ref maxIter);

			#region initialize
			// new inner stop watch and get outer stopwatch
			var stopwatch = Stopwatch.StartNew();

			// transformation matrix Q which will be disposed after return or exception automatically
			var qs = new List<TVec>(maxIter);
			var αs = new List<double>(maxIter);
			var βs = new List<double>(maxIter);
			#endregion

			#region first step of iteration
			// start
			Log.Write($"Restart with max number of iteration = {maxIter}", level: LogLevel.Trace);

			// copy initial vector
			initial = initial.Clone() as TVec;

			// step 1
			LanczosInit<TVec, T>(MatMulVecFunc, ref initial, out TVec r, out double α0, out double β0);
			αs.Add(α0); βs.Add(β0);
			qs.Add(initial);
			#endregion

			#region main
			try
			{
				// main loop
				for (int j = 1; j < maxIter; j++)
				{
					#region log output
					Log.Write($"Now at iteration {j}, {stopwatch.Elapsed} passed since last output.", level: LogLevel.Trace);
					if (stopwatch.Elapsed >= Log.LanczosInfoInterval)
					{
						Log.Write($"now at iteration {j}, {stopwatch.Elapsed.TotalMinutesString()} passed since start.");
						stopwatch.Restart();
					}
					#endregion

					// main calculation
					LanczosMainCalc<TVec, T>(MatMulVecFunc, qs, ref r, αs, βs);
					if (βs[j] == 0)
						break;
					Log.Write($"Main calculation finished, α = {αs[j]}, β = {βs[j]}", level: LogLevel.Trace);

				} // end for main loop

				// construct tridiagonal
				var (θ, S) = LanczosTridiagSolve(αs, βs, !(initial is PureArray<T> p) || p.OnHost);

				// output
				var vecOut = r.OperateOn(qs, Array.ConvertAll(S[0], a => a.FromDouble<T>()));
				return (θ[0], vecOut);
			}
			finally
			{
				r.Dispose();
				qs.ClearList();
			}
			#endregion
		}
		#endregion


		#region restart Lanczos
		private static (bool converged, double[] eigvals, double[][] eigvecs, List<TVec> Q, TVec r, double beta) RestartLanczosInner<TVec, T>(Func<TVec, TVec> MatMulVecFunc, long size, int maxIter, LanczosInfo info, RestartBasicInfo<TVec, T> restartInfo)
			where TVec : AbstractArray<T>, IKrylovVector<TVec, T>, new()
			where T : struct, IComparable<T>
		{
			#region thresholds
			int checkPerN = GlobalSettings.CheckConvergePer;
			// constants
			double machinePrecision = Common.MachinePrecisionOf<T>();
			double thresholdSqrt = Math.Pow(machinePrecision, 0.6/*0.5*/), thresholdPow = Math.Pow(machinePrecision, GlobalSettings.ReorthogonizePower);
			double explicitNormalizeError = machinePrecision/* * info.MatrixNorm*/;
			double φ = machinePrecision * 2/* * Math.Sqrt(info.MatrixNorm)*/;
			#endregion

			#region initialize
			// convergence flag
			bool converge = false;

			// the eigenvalues and eigenvectors
			double[] eigvals = null;
			double[][] eigvecs = null;

			// transformation matrix Q which will be disposed after return or exception automatically
			var qs = new List<TVec>(maxIter);
			var αs = new List<double>(maxIter);
			var βs = new List<double>(maxIter);

			// restart Ritz vectors' count
			int NRitz = restartInfo.UnconvergedEigenvalues.Count;
			#endregion

			#region first step of iteration
			// start
			Log.Write($"Restart with max number of iterations = {maxIter}", level: LogLevel.Trace);

			// step 1
			var ωTracker = LanczosInit(MatMulVecFunc, out TVec r, ref qs, αs, βs, explicitNormalizeError, restartInfo);
			#endregion

			// main loop
			int j;
			for (j = NRitz + 1; j < maxIter; j++)
			{
				#region re-orthogonalization
				if (info.Reorthogonalize == ReorthogonalizeMethod.Selective)
				{
					string strInfo = ωTracker.Reorthonalize<TVec, T>(r, qs, restartInfo.ConvergedEigenvectors, thresholdSqrt, thresholdPow);
					if (!string.IsNullOrWhiteSpace(strInfo))
					{
						Log.Write(strInfo, level: LogLevel.Trace);
						double pre = βs[^1];
						βs[^1] = r.Norm();
						Log.Write($"Reorthogonalization of previous basis changes β from {pre} to {βs[^1]}.", level: LogLevel.Trace);
					}
				}
				else if (info.Reorthogonalize == ReorthogonalizeMethod.Full || info.Reorthogonalize == ReorthogonalizeMethod.RobustFull)
				{
					Common.RobustOrthogonalize<TVec, T>(r, qs, info.Reorthogonalize == ReorthogonalizeMethod.RobustFull);
					double pre = βs[^1];
					βs[^1] = r.Norm();
					Log.Write($"Reorthogonalization of previous basis changes β from {pre} to {βs[^1]}.", level: LogLevel.Trace);
				}
				#endregion

				#region test
				////for (int i = 0; i < qs.Count; i++)
				////{
				////	var q = qs[i];
				////	var dot = r.Dot(q).GenericNegate();
				////	if (Math.Abs(dot.ToDouble()) > Math.Pow(machinePrecision, 0.5) * β s[^1])
				////	{ }
				////	if (!dot.IsZero())
				////	{
				////		r.AddBy_αx(q, dot);
				////	}
				////}
				////βs[^1] = r.Norm();
				// TODO: the orthogonality tracker of |A| very different from 0 have problem
				#endregion

				#region main calculation
				LanczosMainCalc<TVec, T>(MatMulVecFunc, qs, ref r, αs, βs);
				if (βs[j] == 0)
				{	// invariant subspace found
					// construct tridiagonal matrix and calculate eigenvalue
					(eigvals, eigvecs) = LanczosTridiagSolve(αs, βs, !(r is PureArray<T> p) || p.OnHost, firstNResidual: NRitz);
					converge = true;
					break;
				}
				Log.Write($"Main calculation finished, α = {αs[j]}, β = {βs[j]}", level: LogLevel.Trace);
				#endregion

				#region construct tridiagonal and convergence check
				// reduce check convergence frequency
				checkPerN = Math.Max(checkPerN, j / 100);

				if (j > 2 && j % checkPerN == 0 || maxIter - j <= 2)
				{
					// construct tridiagonal matrix and calculate eigenvalue
					(eigvals, eigvecs) = LanczosTridiagSolve(αs, βs, !(r is PureArray<T> p) || p.OnHost, firstNResidual: NRitz);
					φ = machinePrecision * 2 * Math.Sqrt(Math.Max(Math.Abs(eigvals[0]), Math.Abs(eigvals[^1])));
					string message, trace;
					// convergence check
					(converge, message, trace) = LanczosConvergenceCheck(eigvals, eigvecs, βs[^1], info.Tolerance, restartInfo.ConvergedEigenvectors.Count, info.UseGapRatherThanMatrixNorm);
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
				if (info.Reorthogonalize == ReorthogonalizeMethod.Selective)
					ωTracker.ReorthogonalityUpdate(αs, βs, restartInfo.ResidualScalars, φ);
				#endregion
			} // end for main loop

			#region trace log when didn't converge
			if (!converge)
			{
				Log.Write($"The {restartInfo.ConvergedEigenvectors.Count.ToOrdinal()} eigenvalue fails to converge after max number of iterations {maxIter}.", level: LogLevel.Trace);
			}
			#endregion

			// output
			return (converge, eigvals, eigvecs, qs, r, βs[j - 1]);
		}


		/// <summary>
		/// Lanczos algorithm for Hermitian matrix's partial (especially the lowest eigenvalues) eigen-problem.
		/// </summary>
		/// <param name="MatMulVecFunc">a function that receives a dense vector input and give the result of the multiplication of the Hermitian matrix and the input vector</param>
		/// <param name="initial">the initial vector</param>
		/// <param name="smallestK">only the smallest k eigenvalues are the target, we DO NOT recommend a larger k since Lanczos is not designed for it</param>
		/// <param name="tolerance">the tolerance of the Lanczos iterative solver, default 0 means <c>machine precision * 5</c></param>
		/// <param name="maxIter">max iteration number, if <paramref name="maxIter"/> ≤ 0, it will be auto calculated and the thick restart strategy will be used to compute multiple eigen-pairs until they are all converged; otherwise, the computation stops at total number of iterations = <paramref name="maxIter"/> while some of the eigen-pairs may not be calculated at all</param>
		/// <param name="reorthogonalize">perform re-orthogonalization or not, default is <c>true</c>, (notice that Lanczos algorithm is extremely numerical unstable without it)</param>
		/// <param name="useGap">use the estimated gap in the convergence criteria or use the matrix norm, default true</param>
		/// <param name="strategy">the restart strategy to use, if it is <see cref="RestartStrategy.UserDefine"/>, the <paramref name="selector"/> must be indicated</param>
		/// <param name="selector">used for selecting the preservation Ritz pairs only when <paramref name="strategy"/> is <see cref="RestartStrategy.UserDefine"/></param>
		/// <returns>An array of <see cref="double"/> as the eigenvalues and an array of <typeparamref name="TVec"/> as corresponding eigenvectors and the convergence.</returns>
		/// <typeparam name="T">the data type, see <see cref="AbstractArray{T}"/> for more information</typeparam>
		/// <typeparam name="TVec">the general dense vector type that inherits <see cref="AbstractArray{T}"/>, <see cref="IKrylovVector{TVec, T}"/> and must be a concrete class type</typeparam>
		/// <exception cref="ArgumentException">if any of the arguments is wrong</exception>
		/// <exception cref="InvalidOperationException">if the <paramref name="MatMulVecFunc"/> throws inner exceptions</exception>
		/// <exception cref="InsufficientMemoryException">if the <paramref name="smallestK"/> is too large to be calculated within free memory</exception>
		/// <remarks>Currently, if some eigen-pairs are not converged after maximum number of iterations, they will not be returned.</remarks>
		public static (double[] values, TVec[] vectors, bool converge) Lanczos<TVec, T>(Func<TVec, TVec> MatMulVecFunc, TVec initial, int smallestK = 1, int maxIter = 0, double tolerance = 0, ReorthogonalizeMethod reorthogonalize = ReorthogonalizeMethod.Selective, bool useGap = true, RestartStrategy strategy = RestartStrategy.Naive, IRestartStrategy selector = null)
			where TVec : AbstractArray<T>, IKrylovVector<TVec, T>, new()
			where T : struct, IComparable<T>
		{
			#region basics
			if (initial is null)
				throw new ArgumentNullException(nameof(initial), Resource.ArrayCannotNull);
			if (strategy == RestartStrategy.UserDefine && selector is null)
				throw new ArgumentNullException(nameof(selector));
			if (tolerance < 0)
				throw new ArgumentOutOfRangeException(nameof(tolerance));
			tolerance = Common.GetPrecision<T>(tolerance);
			long size = initial.Length;

			int totalIterLeft = maxIter <= 0 ? 0 : maxIter, totalIter = maxIter;
			bool stopAtMaxIter = maxIter > 0;
			// check parameters
			Common.CheckLanczosParas<TVec, T>(MatMulVecFunc, initial, size, smallestK, ref maxIter);
			// log start
			Log.Write($"Starting with matrix size = {size}, max number of iterations = {(totalIterLeft == 0 ? "infinity" : totalIterLeft.ToString())}");

			// stopwatch start
			Stopwatch stopwatchStart = Stopwatch.StartNew(), stopwatch = Stopwatch.StartNew();
			#endregion

			#region estimate norm of matrix first
			TVec guess = initial.Clone() as TVec;
			////if (normA == 0)
			////{
			////	guess = null;
			////	TVec guessMinus = null;
			////	try
			////	{
			////		(normA, guess) = NaiveLanczos<TVec, T>(MatMulVecFunc, initial, size, 10, checkFirst: false);
			////		while (double.IsNaN(normA) || double.IsInfinity(normA))
			////		{
			////			guess.Dispose();
			////			(normA, guess) = NaiveLanczos<TVec, T>(MatMulVecFunc, initial, size, 10, checkFirst: false);
			////		}

			////		TVec minusMat(TVec v)
			////		{
			////			var res = MatMulVecFunc(v);
			////			res.Scale(Scalars<T>.MinusOne);
			////			return res;
			////		}
			////		double normMinusA = double.NaN;
			////		(normMinusA, guessMinus) = NaiveLanczos<TVec, T>(minusMat, initial, size, 10, checkFirst: false);
			////		while (double.IsNaN(normMinusA) || double.IsInfinity(normMinusA))
			////		{
			////			guessMinus.Dispose();
			////			(normMinusA, guessMinus) = NaiveLanczos<TVec, T>(minusMat, initial, size, 10, checkFirst: false);
			////		}

			////		normA = Math.Max(Math.Abs(normA), Math.Abs(normMinusA));
			////	}
			////	catch (Exception)
			////	{
			////		guess?.Dispose();
			////		throw;
			////	}
			////	finally
			////	{
			////		guessMinus?.Dispose();
			////	}
			////}
			////else
			////{
			////	guess = initial.Clone() as TVec;
			////}
			////Log.Write($"Estimation of the matrix norm is {normA}, time cost = {stopwatchStart.Elapsed.TotalSeconds}s.", level: MsgLevel.Trace);
			#endregion

			#region flow control
			var info = new LanczosInfo(reorthogonalize: reorthogonalize, orthogonalizeConverged: false, useGap: useGap, tolerance: tolerance);
			var restartInfo = new RestartBasicInfo<TVec, T>(residual: guess);
			List<double> eigenvalues = new List<double>(smallestK);
			while (true)
			{
				// calculate
				var (converged, eigvals, eigvecs, Q, r, rNorm) = RestartLanczosInner(MatMulVecFunc, size, maxIter, info, restartInfo);

				#region if converge
				if (converged)
				{
					// output newest eigenvalue
					Log.Write($"The newest unconverged eigenvalue is {eigvals[0]}", level: LogLevel.Trace);
					// calculate last eigenvector
					eigenvalues.Add(eigvals[0]);
					var tempSvec = Array.ConvertAll(eigvecs[0], a => a.FromDouble<T>());
					var newConverged = r.OperateOn(Q, tempSvec);
					newConverged.Normalize();
					restartInfo.ConvergedEigenvectors.Add(newConverged);
					// remove converged from eigen pairs
					eigvals = eigvals[1..];
					eigvecs = eigvecs[1..];

					// if converged
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
		#endregion
	}
}
