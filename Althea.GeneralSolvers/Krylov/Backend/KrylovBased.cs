using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Althea.Array;
using Althea.Backend.CSharp.LinearAlgebra;
using Althea.Helpers;
using Althea.Numerics;


// Ignore Spelling: \vec \frac \mathbf \overset \longrightarrow \mathrm \cdot \left \right \varepsilon \mathbb \begin \times \le diag
namespace Althea.GeneralSolvers.Krylov.Backend;

internal static class KrylovBased
{
	#region common

	#region inner
	private static void KrylovSchurInner<T, TVec>(Func<TVec, TVec> matrixFunction, int iters, bool robustOrth, ReadOnlySpan<T> a, ref T β, SpanMatrix<T> H, ref TVec r, ref SpanList<TVec> qs) where T : unmanaged, IBinaryFloat<T> where TVec : class, IBaseVector<T, TVec>
	{
		Span<T> w = stackalloc T[iters];
		int nPreserve = qs.Count;
		for (int j = nPreserve; j < iters; j++)
		{
			#region main
			//tex: $\vec{q}_j=\vec{r}/\beta$
			r.Scale(T.One / β);
			qs.Add(r);
			//tex: $\vec{r}=A\vec{q}_j$
			r = matrixFunction.Invoke(r);
			//tex:Schmidt orthogonalize, $\vec{r}$ is in-place altered
			Common.RobustOrthogonalize(r, qs, w, robustOrth);
			//tex: $H^{(j)} = \left[\begin{matrix}\begin{matrix}H^{\left(j-1\right)}\\{\vec{a}}^T\\\end{matrix}&\vec{w}\\\end{matrix}\right]$
			if (j == 0)
			{
				H[0, 0] = w[0];
			}
			else
			{
				w[..qs.Count].CopyTo(H[j]);
				if (j == nPreserve)
				{
					H.SetRowFrom(j, a);
				}
				else
				{
					H[j, j - 1] = β;
				}
			}
			//tex:$\beta=\|\vec{r}\|,\ \vec{a}^*=\beta \vec{e}_{j}^*$
			β = r.Norm();
			#endregion
		}
	}
	#endregion

	#region final calculation
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Span<TVec> FinalCalc<T, TVec>(SpanMatrix<T> vecs, ReadOnlySpan<TVec> Q, Span<TVec> vector) where T : unmanaged, IBinaryFloat<T> where TVec : class, IBaseVector<T, TVec>
	{
		try
		{
			for (int i = 0; i < vecs.Cols; i++)
			{
				vector[i] = Q.OperateOn<T, TVec>(vecs[i]);
				vector[i].Normalize();
			}
			return vector;
		}
		catch (Exception)
		{
			vector.ClearList();
			throw;
		}
	}
	#endregion

	#endregion


	#region Krylov-Schur

	#region get convergence of Krylov-Schur
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static unsafe void GetSchur<T>(int n, ReadOnlySpanMatrix<T> H, Span<T> eigenvalues, Span<T> eigenvaluesImag, SpanMatrix<T> schurT, SpanMatrix<T> schurU) where T : unmanaged, IBinaryFloat<T>
	{
		H.CopyTo(schurT);
		//tex:$\mathbf H \overset{\text{Schur (no ordering)}}{\longrightarrow} \mathbf H_c \cdot \mathbf U$
		Span<T> work = T.IsComplexType ? stackalloc T[2 * n] : default;
		MatrixSolvers.HessenbergSchurFactorize(schurT, schurU, eigenvalues, eigenvaluesImag, work);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static unsafe int GetConverge<T>(SpanMatrix<T> H, T beta, T tol, WhichEigenvalues which, bool useGap, Span<T> eigenvalues, Span<T> eigenvaluesImag, SpanMatrix<T> eigenvectors, SpanMatrix<T> schurT, SpanMatrix<T> schurU, Span<T> errorBounds) where T : unmanaged, IBinaryFloat<T>
	{
		#region Schur decomposition first
		int n = H.Rows;
		GetSchur(n, H, eigenvalues, eigenvaluesImag, schurT, schurU);
		#endregion

		#region get eigenvectors from Schur form
		//tex:$\mathbf H \overset{\text{Eigen}}{\longrightarrow} \mathbf V \cdot \mathrm{diag}(\vec a) \cdot \mathbf V^{-1}
		//\text{ where }\mathbf V = \mathbf U \mathbf X, \mathbf H_c \overset{\text{Eigen}}{\longrightarrow} \mathbf X \mathrm{diag}(\vec a) \mathbf X^{-1}$
		Span<T> work = stackalloc T[n * 9];
		MatrixSolvers.SchurFormEigensolve(schurT, schurU, eigenvectors, work, eigenvalues, eigenvaluesImag);
		#endregion

		#region sort eigen-pairs
		Span<T> orderedVals = stackalloc T[n], orderedValsImag = T.IsComplexType ? default : stackalloc T[n];
		SpanMatrix<T> orderedVecs = new(stackalloc T[n * n], n);
		eigenvalues.CopyTo(orderedVals); eigenvaluesImag.CopyTo(orderedValsImag);
		eigenvectors.CopyTo(orderedVecs);
		Common.SortPairs(n, which, orderedVals, orderedValsImag, orderedVecs);
		#endregion

		#region get converged
		T normA = orderedVals.Max(static v => T.Abs(v));
		Span<T> lastRow = stackalloc T[n];
		orderedVecs.CopyRowTo(n - 1, lastRow);
		var convergedInd = new SpanList<int>(stackalloc int[n]);
		for (int i = 0; i < n; i++)
		{
			T absLastVec = T.Abs(orderedVecs[i, n - 1]);
			errorBounds[i] = beta * absLastVec / normA;
			if (useGap && errorBounds[i] < tol)    // get more precise gap first
				errorBounds[i] = beta * absLastVec / Common.GetGap(beta, tol, orderedVals, lastRow, i, normA);
			if (errorBounds[i] < tol)    // still converged
				convergedInd.Add(i);
		}
		return convergedInd.Count;
		#endregion
	}
	#endregion

	#region preserve selection of Krylov-Schur
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int PreserveSelect<T>(int nEig, int convergedWithin, WhichEigenvalues which, ReadOnlySpan<T> eigenvalues, ReadOnlySpan<T> eigenvaluesImag, SpanMatrix<T> eigenvectors, IPreserveSelector selector, Span<int> selectOrder) where T : unmanaged, IBinaryFloat<T>
	{
		int count = selector.PreserveSelect(which, eigenvalues, eigenvaluesImag, eigenvectors, convergedWithin, nEig, selectOrder.Length, selectOrder);
		if (count == 1)
			Log.Write(Resource.RestartWarn1, nameof(KrylovSchur), LogLevel.Warning);
		if (count > eigenvalues.Length / 2)
			Log.Write(Resource.RestartWarn2, nameof(KrylovSchur), LogLevel.Warning);
		return count;
	}

	private static unsafe void ReorderSchur<T, TVec>(Span<int> select, int preserveCount, SpanMatrix<T> schurT, SpanMatrix<T> schurU, Span<T> eigenvalues, Span<T> eigenvaluesImag, TVec r, ref SpanList<TVec> qs, Span<T> a, T beta) where T : unmanaged, IBinaryFloat<T> where TVec : class, IBaseVector<T, TVec>
	{
		int rows = select.Length, n = preserveCount;
		//tex:$\mathbf H \overset{\text{Schur (order}=\vec v_\text{preserve}\text{)}}{\longrightarrow} \mathbf H \cdot \mathbf X$
		Span<T> work = T.IsComplexType ? stackalloc T[3 * rows] : default;
		MatrixSolvers.ReorderSchurForm(select, schurT, schurU, eigenvalues, eigenvaluesImag, work);
		//tex:${\vec{a}}^\ast={\vec{a}}^\ast X^\prime$
		var X1 = new SpanMatrix<T>(schurU.UnderlyingSpan, n, schurU.LeadDim);
		X1.CopyRowTo(n - 1, a);
		a.Scale(beta);
		//tex:$H^{\left(n_r\right)}=T_1$ (clear all except the first $n\times n$)
		for (int i = n; i < rows; i++)
		{
			schurT[i].Clear();
			schurT.SetRowFrom(i, schurT[i]);
		}
		//tex:$Q^{\left(n_r\right)}=Q^{\left(k\right)}X_1$
		Span<IntPtr> tempQ = stackalloc IntPtr[qs.Capacity];
		SpanList<TVec> newQ = new(tempQ.AsClassType<TVec>());
		try
		{
			for (int i = 0; i < n; i++)
			{
				var newq = qs.UnderlyingSpan.OperateOn<T, TVec>(X1[i]);
				newq.Normalize();
				newQ.Add(newq);
			}
			qs.ClearList();
			qs.AddRange(MemoryMarshal.CreateReadOnlySpan(ref newQ[0], newQ.Count));
		}
		catch (Exception)
		{
			newQ.ClearList();
			throw;
		}
	}
	#endregion

	// null return for not support
	internal static int? KrylovSchur<T, TVec>(Func<TVec, TVec> matrixFunction, TVec initial, WhichEigenvalues which, int maxRestarts, int iterPerRestart, double tolerance, ReorthogonalizeMethod reorthogonalize, bool useGap, IPreserveSelector selector, bool checkFirst, TimeSpan interval, Span<T> outEigvals, Span<T> outEigvalsImag, Span<TVec> outEigvecs) where T : unmanaged, IBinaryFloat<T> where TVec : class, IBaseVector<T, TVec>
	{
		#region basic
		if (tolerance <= 0)
			throw new ArgumentOutOfRangeException(nameof(tolerance), tolerance, Resources.ParameterError.MustPositive);
		T tol = tolerance.As<T>();
		// check parameters
		int nEig = outEigvals.Length - 1;
		if (checkFirst)
			Common.CheckParas<T, TVec>(matrixFunction, initial, nEig, ref iterPerRestart, herm: false);
		else
			iterPerRestart = Math.Min(iterPerRestart, Common.NON_HERM_MAX_ITER);
		// check other
		if (iterPerRestart <= nEig + 1)
			return null; // not support
		if (nEig > Common.MAX_EIGS)
			return null; // not support
		if (which < WhichEigenvalues.LargestAbsolute || which > WhichEigenvalues.SmallestAbsoluteImaginary)
			return null; // not support
		if (selector.Strategy != RestartStrategy.KrylovSchur)
			return null; // not support
		if (reorthogonalize != ReorthogonalizeMethod.Full || reorthogonalize != ReorthogonalizeMethod.RobustFull)
			return null; // not support
		bool robustOrth = reorthogonalize == ReorthogonalizeMethod.RobustFull;

		// log start
		Log.Write(string.Format(Resource.KrylovSchurStart, initial.Length, maxRestarts));

		// stopwatch start
		Stopwatch stopwatchStart = Stopwatch.StartNew(), stopwatch = Stopwatch.StartNew();
		#endregion

		#region initial
		// managed arrays
		T β = initial.Norm();
		Span<T> a = stackalloc T[iterPerRestart]; a[0] = β;
		Span<IntPtr> tempQ = stackalloc IntPtr[iterPerRestart];
		var qs = new SpanList<TVec>(tempQ.AsClassType<TVec>());
		SpanMatrix<T> H = new(stackalloc T[iterPerRestart * iterPerRestart], iterPerRestart);

		Span<T> eigenvalues = stackalloc T[iterPerRestart];
		Span<T> eigenvaluesImag = outEigvalsImag.IsEmpty ? default : stackalloc T[iterPerRestart];
		SpanMatrix<T> eigenvectors = new(stackalloc T[iterPerRestart * iterPerRestart], iterPerRestart);

		Span<int> selectOrder = stackalloc int[iterPerRestart];
		Span<T> errorBounds = stackalloc T[iterPerRestart];

		SpanList<T> convergedEigvals = new(stackalloc T[nEig + 1]);
		SpanList<T> convergedEigvalsImag = outEigvalsImag.IsEmpty ? default : new(stackalloc T[nEig + 1]);
		SpanMatrix<T> convergedEigvecs = new(stackalloc T[iterPerRestart * (nEig + 1)], iterPerRestart);
		SpanMatrix<T> schurT = new(stackalloc T[iterPerRestart * iterPerRestart], iterPerRestart);
		SpanMatrix<T> schurU = new(stackalloc T[iterPerRestart * iterPerRestart], iterPerRestart);

		// preserve original initial vector
		TVec r = initial.Clone();
		#endregion

		#region main
		try
		{
			bool success = false;
			for (int nRestart = 0; nRestart < maxRestarts; nRestart++)
			{
				#region calculate
				// inner loop calculation
				KrylovSchurInner(matrixFunction, iterPerRestart, robustOrth, a, ref β, H, ref r, ref qs);
				// get converged ones
				var converged = GetConverge(H, β, tol, which, useGap, eigenvalues, eigenvaluesImag, eigenvectors, schurT, schurU, errorBounds);
				#endregion
				
				#region select the Ritz pairs to preserve
				// use a separate method to reduce stack allocation
				int preserveCount = PreserveSelect(nEig, converged, which, eigenvalues, eigenvaluesImag, eigenvectors, selector, selectOrder);
				#endregion

				#region Schur decomposition and prepare for restart
				// use a separate method to reduce stack allocation
				ReorderSchur(selectOrder, preserveCount, schurT, schurU, eigenvalues, eigenvaluesImag, r, ref qs, a, β);
				// log
				Log.Write($"Preserved eigen-pair(s) = {preserveCount}", level: LogLevel.Trace);
				#endregion

				#region restart prepare
				convergedEigvals.Clear();
				convergedEigvals.AddRange(eigenvalues[..converged]);
				if (!convergedEigvalsImag.IsEmpty)
				{
					convergedEigvalsImag.Clear();
					convergedEigvalsImag.AddRange(eigenvaluesImag[..converged]);
				}
				eigenvectors[..converged].CopyTo(convergedEigvecs);
				if (converged >= nEig)
				{
					Log.Write(string.Format(Resource.KrylovSchurConverge, nRestart + 1));
					success = true;
					break;
				}
				#endregion

				#region log output
				if (stopwatch.Elapsed >= interval)
				{
					Log.Write(string.Format(Resource.IterationAndTimeInfo, (nRestart + 1) * iterPerRestart, stopwatchStart.Elapsed.TotalMinutesString()));
					stopwatch.Restart();
				}
				Log.Write($"Krylov-Schur: Estimate relative error is {errorBounds[..nEig].Max()}", level: LogLevel.Trace);
				Log.Write($"Krylov-Schur: First unconverged eigenvalue is {eigenvalues.FirstOfSetExept(convergedEigvals)}", level: LogLevel.Trace);
				#endregion
			}

			#region check success
			if (!success)
			{
				Log.Write(string.Format(Resource.KrylovSchurFail, maxRestarts, nEig - convergedEigvals.Count));
			}
			#endregion

			#region return
			if (!convergedEigvalsImag.IsEmpty && convergedEigvalsImag[^1] != T.Zero)
				nEig++;
			nEig = Math.Min(nEig, convergedEigvals.Count);
			convergedEigvals[..nEig].CopyTo(outEigvals);
			convergedEigvalsImag[..nEig].CopyTo(outEigvalsImag);
			convergedEigvecs = convergedEigvecs[..nEig];
			FinalCalc(convergedEigvecs, qs.AsSpan(), outEigvecs);
			return nEig;
			#endregion
		}
		finally
		{
			qs.ClearList();
			r?.Dispose();
		}
		#endregion
	}
	#endregion


	#region Generalized Minimal Residual (GMRES)

	#region get convergence of GMRES
	private static unsafe bool LinearSolveConvergenceCheck<T>(int n, SpanMatrix<T> H, T β0, T β, T tol, Span<T> convergedVec, bool forceCalc = false) where T : unmanaged, IBinaryFloat<T>
	{
		H = H.SubMatrix(..n, ..n);
		int n1 = n + 1;
		T normH = T.Zero;
		SpanMatrix<T> Hprime = new(stackalloc T[n1 * n], n, n1);
		H.CopyTo(Hprime[..n, ..n]);
		// get norm of H
		Span<T> vals = stackalloc T[n], valsIm = T.IsComplexType ? default : stackalloc T[n];
		MatrixSolvers.HessenbergSchurFactorize(Hprime, default, vals, valsIm);
		if (T.IsComplexType)
		{
			for (int i = 0; i < n; i++)
			{
				T abs = T.Abs(Hprime[i, i]);
				if (normH < abs)
					normH = abs;
			}
		}
		else
		{
			for (int i = 0; i < n; i++)
			{
				T abs;
				T a = Hprime[i, i];
				T c = Hprime[i + 1, i];
				if (c == T.Zero)
				{
					abs = T.Abs(a);
				}
				else
				{
					T b = Hprime[i, i + 1];
					abs = T.Sqrt(a * a - b * c);
					i++;
				}
				if (normH < abs)
					normH = abs;
			}
		}
		//tex:$\mathbf H' = \left[\begin{matrix}\mathbf H\\\vec 0^T,\beta\end{matrix}\right]$
		H.CopyTo(Hprime[..n, ..n]);
		Hprime[n, n - 1] = β;
		//tex:$\min_{\vec y^{(n)}}{\mathbf H' \vec y^{(n)} = \beta_0 \vec e_1}$, $\vec e_1 \in \mathbb F^{n+1}$
		Span<T> y = stackalloc T[n1];
		Span<T> diagR = stackalloc T[n];
		y.Fill(T.Zero); y[0] = β0;
		MatrixSolvers.QrFactorize(Hprime, diagR);
		MatrixSolvers.QrQtMultiply(Hprime, new(y, n1));
		MatrixSolvers.QrLinearSolve(Hprime, diagR, new(y, n1));
		T normY = T.Zero;
		for (int i = 0; i < n1; i++)
		{
			normY += y[i] * y[i];
		}
		normY = T.Sqrt(normY);
		//tex:converge when: $\|\vec r^{(n)}\| = \|\vec y^{(n)}\| \le \|\mathbf A\| \|\vec{b}\| \varepsilon$
		bool converge = normY <= normH * tol; // tolerance includes norm of b
		if (converge || forceCalc)
		{
			y[..n].CopyTo(convergedVec[..n]);
		}
		return converge;
	}
	#endregion

	#region GMRES inner
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static T GetNewInitial<T, TVec>(Func<TVec, TVec> A, TVec b, TVec guess, ref TVec r, ReadOnlySpan<TVec> qs, ReadOnlySpan<T> vec) where T : unmanaged, IBinaryFloat<T> where TVec : class, IBaseVector<T, TVec>
	{
		TVec? temp = null;
		try
		{
			//tex:$\vec x_\text{new} = \vec x_\text{old} + \mathbf Q \vec y$
			temp = qs.OperateOn(vec);
			guess.AddBy(temp, T.One);
			temp.Dispose();
			//tex:$\vec r_\text{new} = \vec b - \mathbf A \vec x_\text{new}$
			Common.RSetToBSubAx<T, TVec>(A, ref r, guess, b);
			return r.Norm();
		}
		finally
		{
			temp?.Dispose();
		}
	}

	private static bool GMResInner<T, TVec>(Func<TVec, TVec> matrixFunction, int iters, bool robustOrth, T tol, ref T β, SpanMatrix<T> H, ref TVec r, ref SpanList<TVec> qs, Span<T> convergedVec) where T : unmanaged, IBinaryFloat<T> where TVec : class, IBaseVector<T, TVec>
	{
		T orgBeta = β;
		Span<T> w = stackalloc T[iters];
		for (int j = 0; j < iters; j++)
		{
			#region main
			//tex: $\vec{q}_j=\vec{r}/\beta$
			r.Scale(T.One / β);
			qs.Add(r);
			//tex: $\vec{r}=A\vec{q}_j$
			r = matrixFunction.Invoke(r);
			//tex:Schmidt orthogonalize, $\vec{r}$ is in-place altered
			Common.RobustOrthogonalize(r, qs, w, robustOrth);
			//tex: $H^{(j)} = \left[\begin{matrix}\begin{matrix}H^{\left(j-1\right)}\\{\beta_j}\\\end{matrix}&\vec{w}\\\end{matrix}\right]$
			if (j == 0)
			{
				H[0, 0] = w[0];
			}
			else
			{
				w[..qs.Count].CopyTo(H[j]);
				H[j, j - 1] = β;
			}
			//tex:$\beta=\|\vec{r}\|,\ \vec{a}^*=\beta \vec{e}_{j}^*$
			β = r.Norm();
			#endregion

			#region convergence test
			if (j > 1)
			{
				bool converge = LinearSolveConvergenceCheck(j + 1, H, orgBeta, β, tol, convergedVec, false);
				if (converge)
					return true;
			}
			#endregion
		}
		return false;
	}
	#endregion

	internal static bool GeneralMinimalResidual<T, TVec>(Func<TVec, TVec> matrixFunction, TVec b, TVec initGuess, int maxRestarts, int iterPerRestart, double tolerance, ReorthogonalizeMethod reorthogonalize, bool checkFirst, TimeSpan interval, int maxStagnations, out TVec solution, out double relativeError)
		where TVec : class, IBaseVector<T, TVec>
		where T : unmanaged, IBinaryFloat<T>
	{
		#region basic
		if (tolerance <= 0)
			throw new ArgumentOutOfRangeException(nameof(tolerance), tolerance, Resources.ParameterError.MustPositive);
		// check parameters
		if (checkFirst)
			Common.CheckParas<T, TVec>(matrixFunction, initGuess, 1, ref iterPerRestart, herm: false);
		else
			iterPerRestart = Math.Min(iterPerRestart, Common.NON_HERM_MAX_ITER);
		// check other
		T tol = tolerance.As<T>();
		relativeError = 1;
		solution = TVec.Empty;
		if (b.Length != initGuess.Length)
			return false; // not support
		if (reorthogonalize != ReorthogonalizeMethod.Full || reorthogonalize != ReorthogonalizeMethod.RobustFull)
			return false; // not support
		bool robustOrth = reorthogonalize == ReorthogonalizeMethod.RobustFull;

		// log start
		Log.Write(string.Format(Resource.GMRESStart, initGuess.Length, maxRestarts));

		// stopwatch start
		Stopwatch stopwatchStart = Stopwatch.StartNew(), stopwatch = Stopwatch.StartNew();
		#endregion

		#region initial
		// projection matrix
		SpanMatrix<T> H = new(stackalloc T[iterPerRestart * iterPerRestart], iterPerRestart);
		// calculate first r
		//tex: $\vec r = \vec b - \mathbf A \vec x_0$
		TVec r = Common.RSetToBSubAx<T, TVec>(matrixFunction, initGuess, b);
		T residual = r.Norm(), oldResidual = residual;
		TVec guess = initGuess.Clone();
		T normB = b.Norm();
		T realTolerance = normB * tol;
		// stack arrays
		Span<IntPtr> tempQ = stackalloc IntPtr[iterPerRestart];
		var qs = new SpanList<TVec>(tempQ.AsClassType<TVec>());
		Span<T> convergedVec = stackalloc T[iterPerRestart];
		// for restart
		bool converge = false;
		int stagnations = 0;
		#endregion

		try
		{
			#region main
			for (int nRestart = 0; nRestart < maxRestarts; nRestart++)
			{
				#region calculation and convergence check
				// dispose old ones
				qs.ClearList(); // the old residual vector is destroyed here since it is the first of Q
				H.UnderlyingSpan.Clear(); // clear H
										  // calculate
				converge = GMResInner(matrixFunction, iterPerRestart, robustOrth, realTolerance, ref residual, H, ref r, ref qs, convergedVec);
				if (converge)
				{
					Log.Write(string.Format(Resource.GMRESConverge, nRestart + 1));
					break;
				}
				#endregion

				#region log output
				if (stopwatch.Elapsed >= interval)
				{
					Log.Write(string.Format(Resource.IterationAndTimeInfo, nRestart + 1, stopwatch.Elapsed.TotalMinutesString()));
					stopwatch.Restart();
				}
				#endregion

				#region restart
				converge = LinearSolveConvergenceCheck(qs.Count, H, oldResidual, residual, realTolerance, convergedVec, forceCalc: true);
				residual = GetNewInitial<T, TVec>(matrixFunction, b, guess, ref r, qs, convergedVec[..qs.Count]);
				// actually converges
				if (residual < realTolerance)
				{
					Log.Write(string.Format(Resource.GMRESConverge, nRestart + 1));
					break;
				}
				// stagnation detect
				if (oldResidual / residual <= T.One)
				{
					stagnations++;
				}
				else
				{
					stagnations = 0;
					oldResidual = residual;
				}
				if (stagnations >= maxStagnations)
				{
					Log.Write(string.Format(Resource.GMRESFail, nRestart + 1, oldResidual));
					break;
				}
				#endregion
			}
			#endregion

			#region return
			if (!converge)
			{
				Log.Write(string.Format(Resource.GMRESFail, maxRestarts, oldResidual));
			}
			else
			{
				LinearSolveConvergenceCheck(qs.Count, H, oldResidual, residual, realTolerance, convergedVec, forceCalc: true);
			}
			solution = qs.UnderlyingSpan.OperateOn<T, TVec>(convergedVec[..qs.Count]);
			solution.AddBy(guess, T.One);
			relativeError = (residual / normB).AsDouble();
			return true;
			#endregion
		}
		finally
		{
			r?.Dispose();
			guess?.Dispose();
			qs.ClearList();
		}
	}
	#endregion
}
