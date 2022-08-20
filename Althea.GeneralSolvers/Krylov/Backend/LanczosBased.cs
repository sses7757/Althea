using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

using Althea.Backend.CSharp.LinearAlgebra;
using Althea.Helpers;
using Althea.LinearAlgebra;
using Althea.Numerics;


// Ignore Spelling: \dfrac \cdot \alpha \mathbf \varepsilon \begin \ddots \cdots
namespace Althea.GeneralSolvers.Krylov.Backend;

internal static class LanczosBased
{
	#region restart info
	private ref struct RestartBasicInfo<T, TVec> where T : unmanaged, IBinaryFloat<T> where TVec : class, IKrylovVector<T, TVec>
	{
		internal TVec ResidualVec;

		internal readonly SpanList<T> ResidualScalars;

		internal readonly SpanList<T> UnconvergedEigenvalues;

		internal readonly SpanList<TVec> UnconvergedEigenvectors;

		internal readonly SpanList<TVec> ConvergedEigenvectors;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal RestartBasicInfo(TVec residual, Span<T> scalarHolder1, Span<T> scalarHolder2, Span<TVec> vectorHolder1, Span<TVec> vectorHolder2)
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

	#region initialize Lanczos
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void LanczosInit<T, TVec>(Func<TVec, TVec> matrixFunction, ref TVec q0, out TVec r, out T α0, out T β0) where T : unmanaged, IBinaryFloat<T> where TVec : class, IKrylovVector<T, TVec>
	{
		q0.Normalize();
		//tex: $\vec r = A \vec q$
		r = matrixFunction.Invoke(q0);
		//tex:$\alpha_0 = \vec q^* \vec r$
		T alpha = q0.Dot(r);
		α0 = alpha.ToRealCheck();
		//tex:$\vec r = \vec r - \alpha_0 \vec q_0$
		r.AddBy(q0, -alpha);
		//tex: $\beta_0=\|\vec r_0\|$
		β0 = r.Norm();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void LanczosInit<T, TVec>(Func<TVec, TVec> matrixFunction, T ψ, out TVec r, ref SpanList<TVec> qs, ref SpanList<T> αs, ref SpanList<T> βs, ref RestartBasicInfo<T, TVec> info) where T : unmanaged, IBinaryFloat<T> where TVec : class, IKrylovVector<T, TVec>
	{
		T oneFourth = T.One / ((T.One + T.One) + (T.One + T.One));

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
			if (info.ResidualScalars.AsSpan().Any(s => s <= T.Pow(ψ, oneFourth/*0.5*/)))
			{   // scalar is not accurate now 
				Span<T> w = stackalloc T[info.UnconvergedEigenvectors.Count];
				Common.RobustOrthogonalize<T, TVec>(r, info.UnconvergedEigenvectors, w);
				for (int i = 0; i < w.Length; i++)
				{
					info.ResidualScalars[i] = w[i].ToRealCheck();
				}
			}
		}
		//tex:$\alpha_0 = \vec q^* \vec r$
		T alpha = q0.Dot(r);
		T α = alpha.ToRealCheck();
		αs.Add(α);
		//tex:$\vec r = \vec r - \alpha_0 \vec q_0$
		r.AddBy(q0, -alpha);
		//tex: $\beta_0=\|\vec r_0\|$
		βs.Add(r.Norm());
	}
	#endregion

	#region main loop of Lanczos
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void LanczosMainCalc<T, TVec>(Func<TVec, TVec> matrixFunction, TVec q, ref TVec r, ref SpanList<T> αs, ref SpanList<T> βs, ref TVec newq, bool dispose = true) where T : unmanaged, IBinaryFloat<T> where TVec : class, IKrylovVector<T, TVec>
	{
		//tex: $\vec v=\vec q$
		/*var v = q;*/
		//tex:$\vec q = \vec r / \beta_{j-1}$
		r.Scale(T.One / βs[^1]);
		newq = r;
		//tex: $\vec r = A \vec q$
		r = matrixFunction(newq);
		// a new vector is generated here
		//tex:$\alpha_j = \vec q^* \vec r$
		T α = newq.Dot(r).ToRealCheck();
		αs.Add(α);
		//tex:$\vec r = \vec r - \alpha_j \vec q - \beta_{j-1} \vec v$
		r.AddBy(newq, -αs[^1]);
		r.AddBy(q, -βs[^1]);
		//tex: $\beta_j = \|\vec r\|$
		βs.Add(r.Norm());
		if (dispose)
			q.Dispose();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void LanczosMainCalc<T, TVec>(Func<TVec, TVec> MatMulVecFunc, SpanList<TVec> qs, ref TVec r, ref SpanList<T> αs, ref SpanList<T> βs) where T : unmanaged, IBinaryFloat<T> where TVec : class, IKrylovVector<T, TVec>
	{
		TVec newq = TVec.Empty;
		LanczosMainCalc(MatMulVecFunc, qs[^1], ref r, ref αs, ref βs, ref newq, dispose: false);
		qs.Add(newq);
	}
	#endregion

	#region tridiagonal solve Lanczos
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static unsafe void LanczosTridiagSolve<T>(SpanList<T> αs, SpanList<T> βs, Span<T> eigval, SpanMatrix<T> eigvec, int firstNResidual = 0) where T : unmanaged, IBinaryFloat<T>
	{
		// check NaN
		if (αs.AsSpan().Any(static a => !T.IsFinite(a)) || βs.AsSpan().Any(static b => !T.IsFinite(b)))
			throw new ArithmeticException(Resources.ArithmeticError.AbnormalOccured);
		// fill matrix
		//tex: $$\left[\begin{matrix}\vartheta_1&&&\sigma_1&&&\\&\ddots&&\vdots&&&\\&&\vartheta_n&\sigma_n&&&\\\sigma_1&\cdots&\sigma_n&\alpha_1&\beta_1&&\\&&&\beta_1&\alpha_2&\ddots&\\&&&&\ddots&\ddots&\beta_{p-1}\\&&&&&\beta_{p-1}&\alpha_p\\\end{matrix}\right]$$
		int N = αs.Count;
		for (int i = 0; i < N; i++)
		{
			eigvec[i].Fill(T.Zero);
			eigvec[i, i] = T.One;
		}
		if (firstNResidual > 0)
		{
			// fill the non-tridiagonal part
			for (int i = 0; i < firstNResidual; i++)
			{
				eigvec[i, i] = αs[i];
				eigvec[firstNResidual, i] = eigvec[i, firstNResidual] = βs[i];
			}
			eigvec[firstNResidual, firstNResidual] = αs[firstNResidual];
		}
		Span<T> offDiag = stackalloc T[N];
		// reduce top-left part to tridiagonal
		if (firstNResidual != 0)
		{
			MatrixSolvers.HermitianMatrixToTridiagonal(eigvec[..(firstNResidual + 1), ..(firstNResidual + 1)], eigval[..(firstNResidual + 1)], offDiag[..(firstNResidual + 1)]);
		}
		αs[firstNResidual..].CopyTo(eigval[firstNResidual..]);
		βs[firstNResidual..].CopyTo(offDiag[(firstNResidual + 1)..]);
		// tridiagonal solve
		var info = MatrixSolvers.HermitianTridiagonalEigensolve(eigval, offDiag, eigvec[..N, ..N]);
		if (info != 0)
			throw new MatrixSolveAlgorithmException(SolveMethodKind.Eigenvalue, info);
	}
	#endregion

	#region orthogonality tracker
	private readonly ref struct OrthogonalityTracker<T> where T : unmanaged, IBinaryFloat<T>
	{
		private static readonly T TWO = T.One + T.One;
		private static readonly T FIVE = T.One + T.One + T.One + T.One + T.One;
		private static readonly T ONE_HAND = T.Pow(FIVE * TWO, TWO);

		internal readonly SpanList<T> pre;

		internal readonly SpanList<T> now;

		private readonly T explicitValue;

		private readonly int convergedCount, unconvergedCount;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal OrthogonalityTracker(T ψ, int convergedRitz, int unconvergedRitz, Span<T> scalarHolder1, Span<T> scalarHolder2)
		{
			this.explicitValue = ψ;
			this.convergedCount = convergedRitz;
			this.unconvergedCount = unconvergedRitz;
			this.pre = new(scalarHolder1);
			this.now = new(scalarHolder2);
			if (unconvergedRitz + convergedRitz >= 1)
			{
				Span<T> temp1 = scalarHolder1[..(unconvergedRitz + convergedRitz - 1)];
				// 100 for not estimated orthogonality loss of Ritz vector
				temp1.Fill(ONE_HAND * ψ);
				pre.AddRange(temp1);
				pre.Add(ψ);
			}
			Span<T> temp2 = scalarHolder2[..(unconvergedRitz + convergedRitz)];
			// 100 for not estimated orthogonality loss of Ritz vector
			temp2.Fill(ONE_HAND * ψ);
			now.AddRange(temp2);
			now.Add(ψ);
			// +1 for iteration No.0
			pre.Add(T.One);
			now.Add(T.One);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal string Reorthonalize<TVec>(TVec r, ReadOnlySpan<TVec> qs, ReadOnlySpan<TVec> converged, T thre1, T thre2) where TVec : class, IKrylovVector<T, TVec>
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
						r.AddBy(q, -q.Dot(r));
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
						r.AddBy(q, -q.Dot(r));
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
						r.AddBy(q, -q.Dot(r));
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

		internal void ReorthogonalityUpdate(ReadOnlySpan<T> αs, ReadOnlySpan<T> βs, ReadOnlySpan<T> residuals, T φ)
		{
			Span<T> ωNew = stackalloc T[this.now.Count + 1];
			ωNew[^1] = T.One;
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
				ωNew[k] = T.Abs(ωNew[k]) + φ;
			}
			// end for normal basis track
			if (totalCount > 0)
			{
				//tex:$$\sum_l{\sigma_l \omega_{j,-l}} = \left| - \alpha_0 \omega_{j,0} - \beta_0 \omega_{j,1} \right| + \vec{q}_j^* \vec{f}_0$$
				T totalError = T.Abs(αs[this.unconvergedCount] * ωNew[totalCount] + βs[this.unconvergedCount] * ωNew[totalCount + 1]) + φ;
				////var sumResiduals = residuals.Sum(r => Math.Abs(r));
				T minResidual = residuals.Min(r => T.Abs(r));
				for (int k = this.convergedCount; k < totalCount; k++)
				{
					int i = k - this.convergedCount; // the index of q, α, β
					ωNew[k] = totalError / minResidual / this.unconvergedCount.As<T>(); // upper bound
				}
				// end for unconverged Ritz vector track
				for (int k = 0; k < this.convergedCount; k++)
				{
					ωNew[k] = totalError / this.explicitValue / this.convergedCount.As<T>();
				}
				// end for converged eigenvector track
			}
			this.pre.Clear(); this.pre.AddRange(this.now);
			this.now.Clear(); this.now.AddRange(MemoryMarshal.CreateReadOnlySpan(ref ωNew.Ref(), ωNew.Length));
		}
	}
	#endregion

	#region convergence check
	private static (bool converge, string message, string trace) LanczosConvergenceCheck<T>(ReadOnlySpan<T> eigval, SpanMatrix<T> eigvec, T beta, T tol, int nConverged, bool useGap) where T : unmanaged, IBinaryFloat<T>
	{
		int iter = eigval.Length - 1;
		// get θ_0  S_j,0 for convergence check
		Span<T> lastRow = stackalloc T[eigval.Length], complexVal = stackalloc T[eigval.Length];
		int row = eigvec.Rows - 1;
		for (int i = 0; i < eigval.Length; i++)
		{
			lastRow[i] = eigvec[row, i];
			complexVal[i] = eigval[i];
		}
		T Sj0 = lastRow[0].ToReal();
		T θ0 = eigval[0];
		T βMulS = beta * T.Abs(Sj0);
		// get gap
		T gap = useGap ? Common.GetGap(beta, tol, complexVal, lastRow) : T.Max(T.Abs(eigval[0]), T.Abs(eigval[^1]));
		// test convergence
		bool converge = βMulS / gap <= tol;
		// log convergence
		string message;
		if (converge)
			message = string.Format(Resource.LanczosConvergeOnePair, nConverged.ToOrdinal());
		else
			message = string.Empty;
		// trace log
		string trace = $"the {nConverged.ToOrdinal()} eigen-pair has: θ = {θ0}, S = {T.Abs(Sj0)}, γ = {gap}";
		return (converge, message, trace);
	}
	#endregion

	#region add unconverged vectors
	private static void AddUnconvergedVectors<T, TVec>(ref RestartBasicInfo<T, TVec> info, ReadOnlySpan<TVec> Q, ReadOnlySpan<int> preserve, Span<T> eigvals, SpanMatrix<T> eigvecs, TVec r, T rNorm) where T : unmanaged, IBinaryFloat<T> where TVec : class, IKrylovVector<T, TVec>
	{
		Span<T> lastRow = stackalloc T[eigvecs.Cols];
		//tex:$\vec{r}$ so that $A\vec{y}_i - \vartheta_i\vec{y}_i = \sigma_i\vec{r}$
		eigvecs.CopyRowTo(eigvecs.Rows - 1, lastRow);
		// calculate new unconverged Ritz vectors
		foreach (var i in preserve)
		{
			//tex:$\sigma_i=\beta_0 s_{k,i}$
			info.ResidualScalars.Add(lastRow[i] * rNorm);
			//tex:$\vartheta_i$ and $\vec{y}_i = Q \vec{s}_i$
			info.UnconvergedEigenvalues.Add(eigvals[i]);
			var unconverged = IKrylovVector<T, TVec>.OperateOn(Q, eigvecs[i]);
			unconverged.Normalize();
			info.UnconvergedEigenvectors.Add(unconverged);
		}
	}
	#endregion


	#region naive Lanczos
	internal static (T val, TVec vec) NaiveLanczos<T, TVec>(Func<TVec, TVec> matrixFunction, TVec initial, int maxIter, bool checkFirst, TimeSpan interval) where T : unmanaged, IBinaryFloat<T> where TVec : class, IKrylovVector<T, TVec>
	{

		#region basic check
		if (checkFirst)
			Common.CheckParas<T, TVec>(matrixFunction, initial, 1, ref maxIter, true);
		else if (maxIter <= 0)
			throw new ArgumentOutOfRangeException(nameof(maxIter), maxIter, Resources.ParameterError.MustPositive);
		#endregion

		#region initialize
		// new inner stop watch and get outer stopwatch
		var stopwatch = Stopwatch.StartNew();
		// transformation matrix Q which will be disposed after return or exception automatically
		Span<IntPtr> tempQ = stackalloc IntPtr[maxIter];
		var qs = new SpanList<TVec>(tempQ.AsClassType<TVec>());
		var αs = new SpanList<T>(stackalloc T[maxIter]);
		var βs = new SpanList<T>(stackalloc T[maxIter]);
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
			LanczosInit(matrixFunction, ref initial, out r, out T α0, out T β0);
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

				LanczosMainCalc(matrixFunction, qs, ref r, ref αs, ref βs);
				if (βs[j] == T.Zero)
					break;
			}
			#endregion

			#region tridiagonal solve
			// log
			Log.Write(string.Format(Resource.NaiveLanczosFinish, αs[^1], βs[^1]));

			int n = qs.Count;
			Span<T> eigenvalues = stackalloc T[n];
			Span<T> eigvec = stackalloc T[n * n];
			SpanMatrix<T> eigenvectors = new(eigvec, n);
			LanczosTridiagSolve(αs, βs, eigenvalues, eigenvectors);
			#endregion

			#region output
			var vecOut = IKrylovVector<T, TVec>.OperateOn(qs.UnderlyingSpan, eigenvectors[0]);
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
	internal static int? RestartLanczos<T, TVec>(Func<TVec, TVec> matrixFunction, TVec initial, int maxRestarts, int iterPerRestart, double tolerance, ReorthogonalizeMethod reorthogonalize, bool useGap, IPreserveSelector selector, bool checkFirst, TimeSpan interval, Span<T> outEigvals, Span<TVec> outEigvecs) where T : unmanaged, IBinaryFloat<T> where TVec : class, IKrylovVector<T, TVec>
	{

		#region basic
		if (tolerance <= 0)
			throw new ArgumentOutOfRangeException(nameof(tolerance), tolerance, Resources.ParameterError.MustPositive);
		// check parameters
		int smallestK = outEigvals.Length;
		if (checkFirst)
			Common.CheckParas<T, TVec>(matrixFunction, initial, smallestK, ref iterPerRestart, herm: true);
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
		Span<T> eigvals = stackalloc T[iterPerRestart];
		Span<T> eigvecSpan = stackalloc T[iterPerRestart * iterPerRestart];
		SpanMatrix<T> eigvecs = new(eigvecSpan, iterPerRestart);

		Span<T> tempHolder1 = stackalloc T[iterPerRestart], tempHolder2 = stackalloc T[iterPerRestart];
		Span<IntPtr> tempQ1 = stackalloc IntPtr[iterPerRestart]; Span<IntPtr> tempQ2 = stackalloc IntPtr[smallestK];
		var restartInfo = new RestartBasicInfo<T, TVec>(residual: guess, tempHolder1, tempHolder2, tempQ1.AsClassType<TVec>(), tempQ2.AsClassType<TVec>());

		SpanList<T> αs = new(stackalloc T[iterPerRestart]), βs = new(stackalloc T[iterPerRestart]);
		#endregion

		#region main
		SpanList<T> eigenvalues = new(stackalloc T[smallestK]);
		TVec r = TVec.Empty;
		try
		{
			#region restart loop
			Span<int> preserveIndicesSpan = stackalloc int[iterPerRestart];

			for (int nRestart = 0; nRestart < maxRestarts; nRestart++)
			{
				// calculate
				var converged = RestartLanczosInner(matrixFunction, iterPerRestart, tolerance, reorthogonalize == ReorthogonalizeMethod.Selective ? null : reorthogonalize == ReorthogonalizeMethod.RobustFull, useGap, ref restartInfo, eigvals, eigvecs, ref qs, ref αs, ref βs, out r);

				#region if converge
				Span<T> eigvalsNow = eigvals;
				SpanMatrix<T> eigvecsNow = eigvecs;
				if (converged)
				{
					// output newest eigenvalue
					Log.Write($"The newest unconverged eigenvalue is {eigvals[0]}", level: LogLevel.Trace);
					// calculate last eigenvector
					eigenvalues.Add(eigvals[0]);
					var newConverged = IKrylovVector<T, TVec>.OperateOn(qs, eigvecs[0]);
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
					int preserveCount = selector.PreserveSelect<T>(WhichEigenvalues.LargestReal, eigvalsNow, default, eigvecsNow, restartInfo.ConvergedEigenvectors.Count, smallestK, iterPerRestart, preserveIndicesSpan);
					if (preserveCount == 1)
						Log.Write($"Restarting with only one preserved Ritz pair may never improve the result.", level: LogLevel.Warning);
					// cannot dispose old unconverged vectors and residual vector since they are in Q now
					////restartInfo.UnconvergedEigenvectors.ClearList<TVec>();
					////restartInfo.ResidualVec.ForceDispose();
					restartInfo.Clear(r);
					// add unconverged vectors
					AddUnconvergedVectors(ref restartInfo, qs, preserveIndicesSpan[..preserveCount], eigvalsNow, eigvecsNow, r, rNorm: βs[^1]);
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


	private static bool RestartLanczosInner<T, TVec>(Func<TVec, TVec> matrixFunction, int nIter, double tolerance, bool? robustOrth, bool useGap, ref RestartBasicInfo<T, TVec> restartInfo, Span<T> eigvals, SpanMatrix<T> eigvecs, ref SpanList<TVec> qs, ref SpanList<T> αs, ref SpanList<T> βs, out TVec r) where T : unmanaged, IBinaryFloat<T> where TVec : class, IKrylovVector<T, TVec>
	{
		#region constants
		double _machinePrecision = T.MachinePrecision.AsDouble(),
			   _thresholdSqrt = Math.Pow(_machinePrecision, 0.6/*0.5*/), _thresholdPow = Math.Pow(_machinePrecision, 0.75),
			   _explicitNormalizeError = _machinePrecision/* * info.MatrixNorm*/,
			   _φ = _machinePrecision * 2/* * Math.Sqrt(info.MatrixNorm)*/;
		T explicitNormalizeError = _explicitNormalizeError.As<T>();
		T thresholdSqrt = _thresholdSqrt.As<T>();
		T thresholdPow = _thresholdPow.As<T>();
		T φ = _φ.As<T>();
		T tol = tolerance.As<T>();
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
			Span<T> tempHolder1 = stackalloc T[nIter + 1], tempHolder2 = stackalloc T[nIter + 1];
			var tracker = new OrthogonalityTracker<T>(explicitNormalizeError, restartInfo.ConvergedEigenvectors.Count, NRitz, tempHolder1, tempHolder2);
			LanczosInit(matrixFunction, explicitNormalizeError, out r, ref qs, ref αs, ref βs, ref restartInfo);
			#endregion

			// main loop
			int j;
			for (j = NRitz + 1; j < nIter; j++)
			{
				#region re-orthogonalization
				if (!robustOrth.HasValue)
				{
					string strInfo = tracker.Reorthonalize(r, qs, restartInfo.ConvergedEigenvectors, thresholdSqrt, thresholdPow);
					if (!string.IsNullOrWhiteSpace(strInfo))
					{
						Log.Write(strInfo, level: LogLevel.Trace);
						T pre = βs[^1];
						βs[^1] = r.Norm();
						Log.Write($"Re-orthogonalization of previous basis changes β from {pre} to {βs[^1]}.", level: LogLevel.Trace);
					}
				}
				else
				{
					Common.RobustOrthogonalize<T, TVec>(r, qs, default, robustOrth.Value);
					T pre = βs[^1];
					βs[^1] = r.Norm();
					Log.Write($"Re-orthogonalization of previous basis changes β from {pre} to {βs[^1]}.", level: LogLevel.Trace);
				}
				#endregion

				#region main calculation
				LanczosMainCalc(matrixFunction, qs, ref r, ref αs, ref βs);
				if (βs[j] == T.Zero)
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
					T phi = φ * T.Sqrt(T.Max(T.Abs(eigvals[0]), T.Abs(eigvals[^1])));
					string message, trace;
					// convergence check
					(converge, message, trace) = LanczosConvergenceCheck<T>(eigvals, eigvecs, beta: βs[^1], tol, nConverged: restartInfo.ConvergedEigenvectors.Count, useGap);
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
	private static TVec? CheckLinearSolve<T, TVec>(Func<TVec, TVec> matrix, Func<TVec, TVec>? preconditioner, TVec initial, TVec rightSide, ref int maxIter, double tolerance, bool checkFirst, out T normB, out T realTolerance) where T : unmanaged, IBinaryFloat<T> where TVec : class, IKrylovVector<T, TVec>
	{
		#region basic
		if (tolerance <= 0)
			throw new ArgumentOutOfRangeException(nameof(tolerance), tolerance, Resources.ParameterError.MustPositive);
		if (checkFirst)
		{
			int iter = maxIter;
			Common.CheckParas<T, TVec>(matrix, initial, smallestK: 1, ref iter, herm: true);
			if (preconditioner is not null)
				Common.CheckParas<T, TVec>(preconditioner, initial, smallestK: 1, ref iter, herm: true);
			maxIter = Math.Max(maxIter, iter);
		}
		else if (maxIter <= 0)
			throw new ArgumentOutOfRangeException(nameof(maxIter), maxIter, Resources.ParameterError.MustPositive);
		maxIter = Math.Min(maxIter, (int)initial.Length);
		#endregion

		#region shortcut
		normB = rightSide.Norm();
		realTolerance = tolerance.As<T>() * normB;
		if (normB == T.Zero)
		{   // all 0 solution
			TVec solution = rightSide.Clone();
			return solution;
		}
		else
		{
			return null;
		}
		#endregion
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static (double relativeError, TVec? solve) CheckLinearSolveInitial<T, TVec>(Func<TVec, TVec> matrix, TVec initial, TVec b, T normB, T realTolerance, out TVec r, out TVec x, out TVec minResidualVec, out T minResidual) where T : unmanaged, IBinaryFloat<T> where TVec : class, IKrylovVector<T, TVec>
	{
		#region initial vector check
		x = initial;
		r = TVec.Empty;
		try
		{
			//tex: $\vec r = \vec b - \mathbf A \vec x_0$
			r = Common.RSetToBSubAx<T, TVec>(matrix, initial, b);
			minResidual = r.Norm();
			if (minResidual <= realTolerance)
			{
				minResidualVec = x;
				return ((minResidual / normB).AsDouble(), initial.Clone());
			}
			else
			{
				x = initial.Clone();
				minResidualVec = x;
				return default;
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
	internal static (TVec Solve, double RelativeError) ConjugateGradient<T, TVec>(Func<TVec, TVec> matrix, Func<TVec, TVec>? preconditioner, TVec initial, TVec rightSide, int maxIter, double tolerance, bool checkFirst, TimeSpan interval, int maxStagnation) where T : unmanaged, IBinaryFloat<T> where TVec : class, IKrylovVector<T, TVec>
	{
		#region basic
		var simpleSolution = CheckLinearSolve<T, TVec>(matrix, preconditioner, initial, rightSide, ref maxIter, tolerance, checkFirst, out T normB, out T realTolerance);
		if (simpleSolution is not null)
			return (simpleSolution, 0);
		#endregion

		#region initialize
		// log
		Log.Write(string.Format(Resource.PCGStart, initial.Length, maxIter));
		Stopwatch stopwatch = Stopwatch.StartNew();
		// check initial guess
		var (relativeError, solve) = CheckLinearSolveInitial(matrix, initial, rightSide, normB, realTolerance, out TVec r, out TVec x, out TVec solution, out T minResidual);
		if (solve is not null)
			return (solve, relativeError);
		// otherwise
		T ρ = T.One;
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
				T ρOld = ρ;
				ρ = r.Dot(z).ToRealCheck();
				if (ρ <= T.Zero)
					throw new ArgumentException(Resource.NotPositiveDefinite, nameof(preconditioner));
				if (i == 0)
				{
					p = z;
				}
				else
				{

					//tex: $\beta_i = \dfrac {\vec r_i \cdot \vec z_i} {\vec r_{i - 1} \cdot \vec z_{i-1}}$
					T β = ρ / ρOld;
					if (β == T.Zero)
					{   // failed due to scalar error
						success = false;
						break;
					}
					//tex: $\vec p_i = \vec z_i + \beta_i \vec p_{i - 1}$
					TVec preP = p;
					p = z;
					if (p == r)
						p = p.Clone();
					p.AddBy(preP, β);
					// now, p is certainly a new vector
				}
				//tex: $\vec q = \mathbf A \vec p_i$
				TVec q = matrix.Invoke(p);
				T pDotQ = p.Dot(q).ToRealCheck();
				if (pDotQ <= T.Zero)
					throw new ArgumentException(Resource.NotPositiveDefinite, nameof(matrix));
				//tex: $\alpha_i = \dfrac {\vec r_i \cdot \vec z_i} {\vec p_i \cdot \vec q}$
				T α = ρ / pDotQ;
				#endregion

				#region check for stagnation
				if (p.Norm() * T.Abs(α) < T.MachinePrecision * x.Norm())
					stagnations++;
				else
					stagnations = 0;
				#endregion

				#region prepare next iteration
				//tex: $\vec x = \vec x + \alpha \vec p_i$
				x.AddBy(p, α);
				//tex: $\vec r = \vec r - \alpha \vec q$
				r.AddBy(q, -α);
				T normR = r.Norm();
				#endregion

				#region check for convergence
				if (normR <= realTolerance)
				{   // check residual vector again
					Common.RSetToBSubAx<T, TVec>(matrix, ref r, x, rightSide);
					T residual = r.Norm();
					if (residual <= realTolerance)
					{
						success = true;
						minResidual = residual;
						break;
					}
				}
				if (stagnations >= maxStagnation)
				{   // failed due to stagnation
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
				Common.RSetToBSubAx<T, TVec>(matrix, ref r, solution, rightSide);
				T normR = r.Norm();
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
			return (solution, minResidual.AsDouble());
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
	internal static (TVec Solve, double RelativeError) MinimalResidual<T, TVec>(Func<TVec, TVec> matrix, Func<TVec, TVec>? preconditioner, TVec initial, TVec rightSide, int maxIter, double tolerance, bool checkFirst, TimeSpan interval, int maxStagnation) where T : unmanaged, IBinaryFloat<T> where TVec : class, IKrylovVector<T, TVec>
	{
		#region basic
		var simpleSolution = CheckLinearSolve<T, TVec>(matrix, preconditioner, initial, rightSide, ref maxIter, tolerance, checkFirst, out T normB, out T realTolerance);
		if (simpleSolution is not null)
			return (simpleSolution, 0);
		#endregion

		#region initialize
		// log
		Log.Write(string.Format(Resource.MinResStart, initial.Length, maxIter));
		Stopwatch stopwatch = Stopwatch.StartNew();
		// check initial guess
		var (relativeError, solve) = CheckLinearSolveInitial<T, TVec>(matrix, initial, rightSide, normB, realTolerance, out TVec r, out TVec x, out TVec solution, out T minResidual);
		if (solve is not null)
			return (solve, relativeError);
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
			T oldβ = oldV.Dot(v).ToRealCheck();
			if (oldβ <= T.Zero)
				throw new ArgumentException(Resource.NotPositiveDefinite, nameof(preconditioner));
			oldβ = T.Sqrt(oldβ);
			//tex: preserve $\prod_i{s_i}$
			T prodSi = oldβ;
			//tex: $\vec v' = \vec v / \beta_1$
			vv = v.Clone(); vv.Scale(T.One / oldβ);
			//tex: $\vec v = \mathbf A \vec v'$
			if (v != r)
				v.Dispose();
			v = matrix.Invoke(vv);
			Am = v.Clone();
			T α = vv.Dot(v).ToRealCheck();
			v.AddBy(oldV, α / oldβ);

			#region local re-orthogonalization
			//tex: $\vec v = \vec v - \dfrac {\vec v' \cdot \vec v} {\vec v' \cdot \vec v'} \vec v'$
			v.AddBy(vv, -vv.Dot(v) / vv.Dot(vv));
			#endregion

			olderV = oldV; oldV = v;
			if (preconditioner is not null)
				v = preconditioner.Invoke(oldV);
			//tex: $\beta^2 = \vec v_{i-1} \cdot (\mathbf M^{-1} \vec v_i)$
			T β = oldV.Dot(v).ToRealCheck();
			if (β <= T.Zero)
				throw new ArgumentException(Resource.NotPositiveDefinite, nameof(preconditioner));
			#endregion

			#region first step
			β = T.Sqrt(β);
			T γbar = α, ε = T.Zero, δbar = β, γ = T.Sqrt(γbar * γbar + β * β), δ = T.Zero;
			m = vv;
			T γInv = (T.One / γ);
			m.Scale(γInv); Am.Scale(γInv);
			T cs = γbar / γ;
			T Si = β / γ;
			x.AddBy(m, prodSi * cs);
			T oldProdSi = prodSi; prodSi *= Si;

			T normR;
			if (preconditioner is not null)
			{
				r.AddBy(Am, -oldProdSi * cs);
				normR = r.Norm();
			}
			else
			{
				normR = T.Abs(prodSi);
			}
			// check for convergence after first step
			if (normR <= realTolerance)
			{
				minResidual = normR / normB;
				solution = x;
				return (solution, minResidual.AsDouble());
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
				vv = v; v.Scale(T.One / β);
				//tex: $\vec v = \mathbf A \vec v / \beta$
				v = matrix.Invoke(vv);
				// change Am
				olderAm?.Dispose();
				olderAm = oldAm;
				oldAm = Am;
				Am = v;
				// orthogonalize v (the key component of Lanczos)
				//tex: $\vec v_i = \vec v_i - t_{i,i-1}\vec v_{i-1} - t_{i,i-2}\vec v_{i-2}$
				v.AddBy(olderV, -β / oldβ);
				α = vv.Dot(v).ToRealCheck();
				v.AddBy(oldV, -α / β);
				olderV?.Dispose();
				olderV = oldV;
				oldV = v;
				// apply preconditioner
				if (preconditioner is not null)
					v = preconditioner.Invoke(oldV);
				// change scalars
				oldβ = β;
				β = oldV.Dot(v).ToRealCheck();
				β = T.Sqrt(β);
				δ = cs * δbar + Si * α;
				// change m
				olderM?.Dispose();
				olderM = oldM;
				oldM = m;
				//tex: $\vec m_i = \vec v' - \delta \vec m_{i-1} - \varepsilon \vec m_{i-2}$
				m = vv;
				m.AddBy(oldM, -δ);
				if (ε != T.Zero && olderM is not null)
					m.AddBy(olderM, -ε);
				//tex: $(\vec {m_\mathbf A})_i = (\vec {m_\mathbf A})_i - \delta (\vec {m_\mathbf A})_{i-1} - \varepsilon (\vec {m_\mathbf A})_{i-2}$
				Am.AddBy(oldAm, -δ);
				if (ε != T.Zero && olderAm is not null)
					Am.AddBy(olderAm, -ε);
				// change other scalars
				γbar = Si * δbar - cs * α;
				ε = Si * β;
				δbar = -cs * β;
				γ = T.Sqrt(γbar * γbar + β * β);
				// scale m, Am, cs and Si
				γInv = T.One / γ;
				m.Scale(γInv);
				Am.Scale(γInv);
				cs = γbar / γ;
				Si = β / γ;
				#endregion

				#region check stagnation
				T prodSiCs = prodSi * cs;
				if (prodSiCs == T.Zero || T.Abs(prodSiCs) * m.Norm() < T.MachinePrecision * x.Norm())
					stagnations++;
				else
					stagnations = 0;
				#endregion

				#region update solution x
				x.AddBy(m, prodSiCs);
				oldProdSi = prodSi;
				prodSi *= Si;
				if (preconditioner is not null)
				{
					r.AddBy(Am, -prodSiCs);
					normR = r.Norm();
				}
				else
				{
					normR = T.Abs(prodSi);
				}
				#endregion

				#region check for convergence
				if (normR <= realTolerance)
				{
					Common.RSetToBSubAx<T, TVec>(matrix, ref r, x, rightSide);
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
				Common.RSetToBSubAx<T, TVec>(matrix, ref r, solution, rightSide);
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
			return (solution, minResidual.AsDouble());
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
