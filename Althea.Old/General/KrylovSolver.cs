using System;
using System.Collections.Generic;
using System.Diagnostics;

using Althea.Linq;
using Althea.Storage;
using RT = Althea.Runtime.API;


namespace Althea.General
{
	/// <summary>
	/// The solver class for Krylov subspace algorithms for eigen problem and linear system problem
	/// </summary>
	public static class KrylovSolver
	{
		#region basics
		/// <summary>
		/// The inner enum to indicate which eigen-pairs are desired
		/// </summary>
		public enum WhichEigenvalues
		{
			/// <summary>
			/// The eigenvalues with largest absolute values
			/// </summary>
			LargestAbsolute,
			/// <summary>
			/// The eigenvalues with largest real part values
			/// </summary>
			LargestReal,
			/// <summary>
			/// The eigenvalues with largest imaginary part's absolute values
			/// </summary>
			LargestAbsoluteImaginary,
			/// <summary>
			/// The eigenvalues with smallest absolute values
			/// </summary>
			SmallestAbsolute,
			/// <summary>
			/// The eigenvalues with smallest real part values
			/// </summary>
			SmallestReal,
			/// <summary>
			/// The eigenvalues with smallest imaginary part's absolute values
			/// </summary>
			SmallestAbsoluteImaginary
		}
		#endregion


		#region inner loop
		private delegate (bool converge, T[][] convergedVectors) DelegateConvergence<T>(int sizeH, Array.DenseMatrix<T> H, double beta, double tol) where T : struct, IComparable<T>;

		private static (bool converge, T[][] convergedVectors) KrylovSchurInner<TVec, T>(Func<TVec, TVec> MatMulVecFunc, int maxIter, bool robustOrthogonalize, double tol, T[] a, ref double β, Array.DenseMatrix<T> H, ref TVec r, ref List<TVec> qs, DelegateConvergence<T> convergenceTest = null)
			where TVec : AbstractArray<T>, IKrylovVector<TVec, T>, new()
			where T : struct, IComparable<T>
		{
			int Npreserve = qs.Count;
			for (int j = Npreserve; j < maxIter; j++)
			{
				#region main
				//tex: $\vec{q}_j=\vec{r}/\beta$
				r.Scale((1 / β).FromDouble<T>());
				qs.Add(r);
				//tex: $\vec{r}=A\vec{q}_j$
				r = MatMulVecFunc(r);
				//tex:Schmidt orthogonalize, $\vec{r}$ is in-place altered
				T[] w = Common.RobustOrthogonalize<TVec, T>(r, qs, robustOrthogonalize);
				// Ignore Spelling: \begin
				//tex: $H^{(j)}=\left[\begin{matrix}\begin{matrix}H^{\left(j-1\right)}\\{\vec{a}}^\ast\\\end{matrix}&\vec{w}\\\end{matrix}\right]$
				/*H = new Arrays.DenseMatrix<T>(refArray: H, newRows: j + 1, newCols: j + 1, newLD: H.LeadDim);*/
				if (j == 0)
				{
					H[0, 0] = w[0];
				}
				else
				{
					H.FromFortranOrderArray(w, ..(j + 1), j..(j + 1));
					if (j == Npreserve)
					{
						H.FromFortranOrderArray(a, j..(j + 1), ..j);
					}
					else
					{
						H[j, j - 1] = β.FromDouble<T>();
					}
				}
				//tex:$\beta=\|\vec{r}\|,\ \vec{a}^*=\beta \vec{e}_{j}^*$
				β = r.Norm();
				#endregion

				#region convergence test
				if (convergenceTest != null && j > 1)
				{
					var (converge, convergedVectors) = convergenceTest(j + 1, H, β, tol);
					if (converge)
						return (converge, convergedVectors);
				}
				#endregion
			}
			return default; // (false, null)
		}
		#endregion


		#region convergence check
		private static (bool converge, T[][] convergedVectors) LinearSolveConvergenceCheck<T>(int sizeH, Array.DenseMatrix<T> H, double β0, double β, double tol, double normB, bool forceCalc = false) where T : struct, IComparable<T>
		{
			DoubleComplex[] eigenvalues;
			if (H.IsSingleType)
			{
				using var eig = H.GetSubmatrix(..sizeH, ..sizeH).Eigenvalue<FloatComplex>();
				eigenvalues = Array.ConvertAll(eig.ToFortranOrderArray(), a => (DoubleComplex)a);
			}
			else
			{
				using var eig = H.GetSubmatrix(..sizeH, ..sizeH).Eigenvalue<DoubleComplex>();
				eigenvalues = eig.ToFortranOrderArray();
			}
			using var HH = new Array.DenseMatrix<T>(sizeH + 1, sizeH, H.OnHost);
			HH.FillWithZeros();
			RT.CopyMatrixTo(source: H, dest: HH, copyNRows: sizeH, copyNCols: sizeH);
			HH[^1, ^1] = β.FromDouble<T>();
			var (Q, R) = HH.QR(full: true);
			using (Q) using (R)
			{
				//tex:$\beta_1\left|U_{1,n+1}^{\left(n\right)}\right|<\|A\|\|\vec{b}\|ε$
				bool converge = β0 * Math.Abs(Q[0, ^1].ToDouble()) < eigenvalues.Max(e => e.Abs()) * normB * tol;
				if (converge || forceCalc)
				{
					//tex: calculate ${\vec{y}}_n=\beta_1\left(R^{\left(n\right)}\right)^{-1}\left(U_{1,1:n}^{\left(n\right)}\right)^\ast$
					using var invR = R.Inverse();
					using var u = Q.GetRowAt(0);
					using var yy = new Array.DenseVector<T>(R.NRows, onHost: H.OnHost);
					yy.Mulβ_AddBy_αopAx(invR, u[0..^1] as Array.DenseVector<T>, β0.FromDouble<T>());
					return (converge, new[] { RT.CopyOutArray(yy) });
				}
				return default;
			}
		}

		private static ((DoubleComplex val, DoubleComplex[] vec)[] ordered, List<int> convergedInd, double[] errorBounds) GetConverge<T>(Array.DenseMatrix<T> H, double beta, double tol, WhichEigenvalues which, bool useGap = true)
			where T : struct, IComparable<T>
		{
			#region Schur decomposition first
			using var Hc = H.Clone() as Array.DenseMatrix<T>;
			using var USchur = Hc.NewArrayAlike() as Array.DenseMatrix<T>;
			Solver.API.Schur(Hc, U: USchur);
			int[] conjugatePairs = new int[Hc.NRows];
			int _countPair = 1;
			using (var HsubDiag = Hc.GetDiag(-1))
			{
				T[] hostHsubDiag = HsubDiag.ToFortranOrderArray();
				for (int i = 0; i < Hc.NRows - 1; i++)
				{
					if (!hostHsubDiag[i].IsZero())
					{
						conjugatePairs[i] = _countPair;
						conjugatePairs[i + 1] = _countPair;
						_countPair++;
						i++;
					}
				}
			}
			#endregion

			#region get eigenvalues and eigenvectors
			DoubleComplex[] vals;
			DoubleComplex[][] vecs;
			if (Hc.IsSingleType)
			{
				var (eigvalSingle, _eigvecL, eigvecSingle) = Hc.Eigensystem<FloatComplex>();
				using (eigvalSingle) using (eigvecSingle) using (_eigvecL)
				{
					using var eigvecReal = eigvecSingle.NewArrayAlike() as Array.DenseMatrix<FloatComplex>;
					if (Hc.IsRealType) // X = U * X
					{
						using var Ucomp = new Array.DenseMatrix<FloatComplex>(H.NRows, H.NRows, onHost: true);
						Blas.API.PointWiseToComplex(src: USchur.AsDenseVector(), dst: Ucomp.AsDenseVector());
						eigvecReal.Mulβ_AddBy_αAB(Ucomp, eigvecSingle, 1);
					}
					else
					{
						eigvecReal.Mulβ_AddBy_αAB(USchur as Array.DenseMatrix<FloatComplex>, eigvecSingle, 1);
					}
					var valSingle = RT.CopyOutArray(eigvalSingle);
					var vecSingle = RT.CopyOutArray(eigvecReal);
					vals = Array.ConvertAll(valSingle, v => (DoubleComplex)v);
					vecs = Array.ConvertAll(vecSingle, v => (DoubleComplex)v).ToJagged(vals.Length);
				}
			}
			else
			{
				var (eigvalDouble, _eigvecL, eigvecDouble) = Hc.Eigensystem<DoubleComplex>();
				using (eigvalDouble) using (eigvecDouble) using (_eigvecL)
				{
					using var eigvecReal = eigvecDouble.NewArrayAlike() as Array.DenseMatrix<DoubleComplex>;
					if (Hc.IsRealType) // X = U * X
					{
						using var Ucomp = new Array.DenseMatrix<DoubleComplex>(H.NRows, H.NRows, onHost: true);
						Blas.API.PointWiseToComplex(src: USchur.AsDenseVector(), dst: Ucomp.AsDenseVector());
						eigvecReal.Mulβ_AddBy_αAB(Ucomp, eigvecDouble, 1);
					}
					else
					{
						eigvecReal.Mulβ_AddBy_αAB(USchur as Array.DenseMatrix<DoubleComplex>, eigvecDouble, 1);
					}
					vals = RT.CopyOutArray(eigvalDouble);
					vecs = RT.CopyOutArray(eigvecReal).ToJagged(vals.Length);
				}
			}
			#endregion

			#region sort eigen-pairs
			var _identityPermutation = ArrayLinq.Range(0, vals.Length);
			var _orderedPermute = which switch
			{
				WhichEigenvalues.LargestAbsolute => vals.Zip(_identityPermutation).OrderByDescending(a => a.First.Abs()),
				WhichEigenvalues.LargestReal => vals.Zip(_identityPermutation).OrderByDescending(a => a.First.Real()),
				WhichEigenvalues.LargestAbsoluteImaginary => vals.Zip(_identityPermutation).OrderByDescending(a => Math.Abs(a.First.Imaginary())),
				WhichEigenvalues.SmallestAbsolute => vals.Zip(_identityPermutation).OrderBy(a => a.First.Abs()),
				WhichEigenvalues.SmallestReal => vals.Zip(_identityPermutation).OrderBy(a => a.First.Real()),
				WhichEigenvalues.SmallestAbsoluteImaginary => vals.Zip(_identityPermutation).OrderBy(a => Math.Abs(a.First.Imaginary())),
				_ => throw new NotSupportedException()
			};
			var _reorder = _orderedPermute.Select(a => a.Second).ToArray();
			vals = _orderedPermute.Select(a => a.First).ToArray();
			vecs = vecs.ReOrder(_reorder);
			conjugatePairs = conjugatePairs.ReOrder(_reorder);
			#endregion

			#region get converged
			double normA = vals.Max(v => v.Abs());
			var lastRow = vecs.Select(v => v[^1]).ToArray();
			var errors = new double[vals.Length];
			var convergedInd = new List<int>();
			for (int i = 0; i < vals.Length; i++)
			{
				errors[i] = beta * vecs[i][^1].Abs() / normA;
				if (useGap && errors[i] < tol)    // get more precise gap first
					errors[i] = beta * vecs[i][^1].Abs() / Common.GetGap(beta, normA, vals, lastRow, target: i, conjugatePairs: conjugatePairs);
				if (errors[i] < tol)    // still converged
					convergedInd.Add(i);
			}
			// recheck make sure conjugate pair both converge
			if (convergedInd.Count > 0)
			{
				for (int i = 0; i < vals.Length; i++)
				{
					if (convergedInd.Contains(i) && conjugatePairs[i] != 0)
					{
						int ind = Array.IndexOf(conjugatePairs, conjugatePairs[i]);
						if (ind == i)
							ind = Array.LastIndexOf(conjugatePairs, conjugatePairs[i]);
						if (!convergedInd.Contains(ind))
							convergedInd.Remove(i); // remove this one
					}
				}
			}
			return (vals.Zip(vecs).ToArray(), convergedInd, errors);
			#endregion
		}
		#endregion


		#region eigen problem
		/// <summary>
		/// The Krylov-Schur algorithm for non-Hermitian matrix's partial (especially the extreme eigenvalues) eigen-problem.
		/// </summary>
		/// <param name="MatMulVecFunc">a function that receives a dense vector input and give the result of the multiplication of the non-Hermitian matrix and the input vector</param>
		/// <param name="initial">The initial vector</param>
		/// <param name="nEig">only the top <paramref name="nEig"/> eigenvalues are the target, we DO NOT recommend a large value since the Krylov-Schur algorithm is not designed for it</param>
		/// <param name="which">a <see cref="WhichEigenvalues"/> to indicate which eigen-pairs are desired</param>
		/// <param name="tolerance">The threshold of convergence, default 0 means <c>machine precision * 5</c></param>
		/// <param name="maxRestarts">max number of restarts</param>
		/// <param name="iterPerRestart">iteration number per restart, default 0 means auto calculation</param>
		/// <param name="robustOrthogonalize">perform robust orthogonalization or not, default <c>true</c></param>
		/// <param name="useGap">use the estimated gap in the convergence criteria or use the matrix norm, default true</param>
		/// <param name="strategy">The restart strategy to use, if it is <see cref="RestartStrategy.UserDefine"/>, the <paramref name="restartStrategy"/> must be indicated</param>
		/// <param name="restartStrategy">used for selecting the preservation Ritz pairs only when <paramref name="strategy"/> is <see cref="RestartStrategy.UserDefine"/></param>
		/// <returns>An array of <see cref="DoubleComplex"/> as the eigenvalues and an array of <typeparamref name="TVec"/> as corresponding eigenvectors (and a possible imaginary part of eigenvectors if <typeparamref name="T"/> is not a complex type) and the convergence at last.</returns>
		/// <typeparam name="T">The data type, see <see cref="AbstractArray{T}"/> for more information</typeparam>
		/// <typeparam name="TVec">The general dense vector type that inherits <see cref="AbstractArray{T}"/>, <see cref="IKrylovVector{TVec, T}"/> and must be a concrete class type</typeparam>
		/// <exception cref="ArgumentException">if any of the arguments is wrong</exception>
		/// <exception cref="InvalidOperationException">if the <paramref name="MatMulVecFunc"/> throws inner exceptions</exception>
		/// <exception cref="InsufficientMemoryException">if the <paramref name="nEig"/> is too large to be calculated within free memory</exception>
		/// <remarks>Currently, if some eigen-pairs are not converged after maximum number of iterations, they will not be returned.</remarks>
		public static (DoubleComplex[] values, TVec[] vectors, TVec[] vectorsIm, bool converge) KrylovSchur<TVec, T>(Func<TVec, TVec> MatMulVecFunc, TVec initial, int nEig = 1, WhichEigenvalues which = WhichEigenvalues.LargestAbsolute, int maxRestarts = int.MaxValue, int iterPerRestart = 0, double tolerance = 0, bool robustOrthogonalize = true, bool useGap = true, RestartStrategy strategy = RestartStrategy.KrylovSchur, IRestartStrategy restartStrategy = null)
			where TVec : AbstractArray<T>, IKrylovVector<TVec, T>, new()
			where T : struct, IComparable<T>
		{
			#region basics
			if (initial is null)
				throw new ArgumentNullException(nameof(initial), Resource.ArrayCannotNull);
			if (strategy == RestartStrategy.UserDefine && restartStrategy is null)
				throw new ArgumentNullException(nameof(restartStrategy));
			if (tolerance < 0)
				throw new ArgumentOutOfRangeException(nameof(tolerance));
			tolerance = Common.GetPrecision<T>(tolerance);
			long size = initial.Length;

			// check parameters
			Common.CheckLanczosParas<TVec, T>(MatMulVecFunc, initial, size, nEig, ref iterPerRestart, herm: false);

			// stopwatch
			var stopwatchStart = Stopwatch.StartNew();
			var stopwatch = Stopwatch.StartNew();
			#endregion

			#region flow control

			#region initial
			Log.Write($"Starting with matrix size = {size}, iterations per restart = {iterPerRestart}");
			double beta = initial.Norm();
			T[] a = new[] { beta.FromDouble<T>() };
			List<TVec> qs = new(iterPerRestart);
			Array.DenseMatrix<T> H = new Array.DenseMatrix<T>(iterPerRestart, iterPerRestart, onHost: true);
			H.FillWithZeros();
			initial = initial.Clone() as TVec; // preserve original initial vector
			#endregion

			try
			{
				(DoubleComplex val, DoubleComplex[] vec)[] converged = Array.Empty<(DoubleComplex, DoubleComplex[])>();
				bool converge = false;
				int restarts = 0;
				while (maxRestarts > restarts)
				{
					restarts++;

					KrylovSchurInner(MatMulVecFunc, iterPerRestart, robustOrthogonalize, tolerance, a, ref beta, H, ref initial, ref qs);

					#region log output
					if (stopwatch.Elapsed >= Log.LanczosInfoInterval)
					{
						Log.Write($"Now at restart {restarts}, {stopwatchStart.Elapsed.TotalMinutesString()} passed since start.");
						stopwatch.Restart();
					}
					#endregion

					#region recheck iteration number
					// matrix multiply vector function already checked, do not wast time
					Common.CheckLanczosParas<TVec, T>(null, null, size, nEig, ref iterPerRestart, herm: false);
					// check max iterations again
					if (iterPerRestart < nEig)
					{
						Log.Write($"The calculated `{nameof(iterPerRestart)}` is smaller than the desired `{nameof(nEig)}`, it may never converge.", level: LogLevel.Error);
					}
					#endregion

					#region convergence test
					var (ordered, convergedInd, errors) = GetConverge(H, beta, tolerance, which, useGap);
					var convergedWithIn = convergedInd.Where(i => i < nEig).ToList();
					if (convergedWithIn.Count == nEig)
					{
						converged = ordered[..nEig];
						Log.Write($"Converged after {restarts} restarts.");
						converge = true;
						break;
					}
					if (maxRestarts == restarts) // not converge
					{
						Log.Write($"Failed to converge ({convergedWithIn.Count} of {nEig} converged), first unconverged eigen-pair's relative error is {errors[converged.Length]}", level: LogLevel.Warning);
						converged = ordered[..nEig];
						break;
					}
					Log.Write($"Estimate relative error is {errors[..nEig].Max()}", level: LogLevel.Trace);
					Log.Write($"First unconverged eigenvalue is {ordered.IndexWhere(i => !convergedInd.Contains(i))[0].val}", level: LogLevel.Trace);
					#endregion

					#region select the Ritz pairs to preserve
					var rest = ordered.IndexWhere(i => !convergedWithIn.Contains(i));
					var restVals = rest.Select(a => a.val).ToArray();
					var restVecs = rest.Select(a => a.vec).ToArray();
					var delegatePreserve = strategy == RestartStrategy.UserDefine ? (IRestartStrategy.DelegatePreserveSelect)restartStrategy.PreserveSelect : new BuiltInRestartStrategy(strategy).PreserveSelect;
					var preserveIndices = delegatePreserve(restVals, restVecs, convergedWithIn.Count, nEig, iterPerRestart);
					if (preserveIndices.Length == 1)
						Log.Write($"Restarting with only one preserved Ritz pair may never improve the result.", level: LogLevel.Warning);
					if (preserveIndices.Length > iterPerRestart / 2)
						Log.Write($"Restart preserving more than half Ritz pairs may not improve the result.", level: LogLevel.Warning);
					var preserveList = new List<DoubleComplex>(preserveIndices.Length + convergedWithIn.Count);
					preserveList.AddRange(ordered.Select(a => a.val).Except(restVals));
					foreach (var item in preserveIndices)
					{
						preserveList.Add(restVals[item]);
					}
					// prevent stagnation
					DoubleComplex[] preserveVals;
					if (convergedInd.Count < converged.Length)
					{
						preserveList.Add(restVals.Except(preserveList)[0]);
					}
					preserveVals = preserveList.ToArray();
					#endregion

					#region Schur decomposition and prepare for restart
					// Ignore Spelling: \left \right
					using var X = new Array.DenseMatrix<T>(H.NRows, H.NCols, onHost: true);
					int actualLen = Solver.API.Schur(H, orderVal: preserveVals, U: X);
					var X1 = X.GetColumnRange(..actualLen);
					//tex:${\vec{a}}^\ast={\vec{a}}^\ast X^\prime$
					dynamic betaT = (dynamic)beta.FromDouble<T>();
					a = Array.ConvertAll(X1.ToFortranOrderArray(^1..^0, ..), x => (T)(x * betaT));
					//tex:$H^{\left(n_r\right)}=T_1$
					var T1 = H.ToFortranOrderArray(..actualLen, ..actualLen);
					H.FillWithZeros();
					H.FromFortranOrderArray(T1, ..actualLen, ..actualLen);
					//tex:$Q^{\left(n_r\right)}=Q^{\left(k\right)}X_1$
					var hostX1 = RT.CopyOutArray(X1).ToJagged(X.NRows);
					var newQ = new List<TVec>(iterPerRestart);
					try
					{
						foreach (var x in hostX1)
						{
							var newq = initial.OperateOn(qs, x);
							newq.Normalize();
							newQ.Add(newq);
						}
						qs.ClearList();
						qs = newQ;
					}
					catch (Exception)
					{
						newQ.ClearList();
						throw;
					}
					// log
					Log.Write($"Actual preserve length = {actualLen} (out of desired {preserveVals.Length})", level: LogLevel.Trace);
					#endregion

					converged = ordered.Where((_, i) => convergedInd.Contains(i)).ToArray();
				}

				#region local functions
				static (TVec[] real, TVec[] imag) finalCalcReal(TVec initial, T[][] real, T[][] imag, IReadOnlyList<TVec> Q)
				{
					TVec[] vectorReal = new TVec[real.Length];
					TVec[] vectorImag = new TVec[real.Length];
					try
					{
						for (int i = 0; i < real.Length; i++)
						{	// initial is not important
							vectorReal[i] = initial.OperateOn(Q, real[i]);
							if (imag[i] != null)
								vectorImag[i] = initial.OperateOn(Q, imag[i]);
							var norm = Math.Sqrt(Math.Pow(vectorReal[i].Norm(), 2) + (imag[i] != null ? Math.Pow(vectorImag[i].Norm(), 2) : 0));
							var normInv = (1 / norm).FromDouble<T>();
							vectorReal[i].Scale(normInv); vectorImag[i]?.Scale(normInv);
						}
					}
					catch (Exception)
					{
						Array.ForEach(vectorReal, v => v?.Dispose());
						Array.ForEach(vectorImag, v => v?.Dispose());
						throw;
					}
					return (vectorReal, vectorImag);
				}
				static TVec[] finalCalcComp(TVec initial, T[][] comp, IReadOnlyList<TVec> Q)
				{
					TVec[] vector = new TVec[comp.Length];
					try
					{
						for (int i = 0; i < comp.Length; i++)
						{
							vector[i] = initial.OperateOn(Q, comp[i]);
							vector[i].Normalize();
						}
					}
					catch (Exception)
					{
						Array.ForEach(vector, v => v?.Dispose());
						throw;
					}
					return vector;
				}
				#endregion

				#region return
				var finalConvergedVals = Array.ConvertAll(converged, a => a.val);
				var finalConvergedVecs = Array.ConvertAll(converged, a => a.vec);
				var calculateImag = Array.ConvertAll(converged, a => !initial.IsRealType || a.val.Imaginary() != 0);
				if (default(T).ToDataType().IsReal())
				{
					if (default(T).ToDataType() == DataType.RealDouble)
					{
						var realD = Array.ConvertAll(finalConvergedVecs, v => Array.ConvertAll(v, a => a.Real()));
						var imagD = finalConvergedVecs.Zip(calculateImag, (vec, calc) => calc ? Array.ConvertAll(vec, a => a.Imaginary()) : null).ToArray();
						var (realDD, imagDD) = finalCalcReal(initial, realD as T[][], imagD as T[][], qs);
						return (finalConvergedVals, realDD, imagDD, converge);
					}
					else
					{
						var real = Array.ConvertAll(finalConvergedVecs, v => Array.ConvertAll(v, a => a.Real().FromDouble<T>()));
						var imag = finalConvergedVecs.Zip(calculateImag, (vec, calc) => calc ? Array.ConvertAll(vec, a => a.Imaginary().FromDouble<T>()) : null).ToArray();
						var (realFinal, imagFinal) = finalCalcReal(initial, real, imag, qs);
						return (finalConvergedVals, realFinal, imagFinal, converge);
					}
				}
				else
				{
					if (finalConvergedVecs is T[][])
					{
						return (finalConvergedVals, finalCalcComp(initial, finalConvergedVecs as T[][], qs), null, converge);
					}
					else
					{
						var comp = Array.ConvertAll(finalConvergedVecs, v => Array.ConvertAll(v, a => a.GenericConvert<T, DoubleComplex>()));
						return (finalConvergedVals, finalCalcComp(initial, comp, qs), null, converge);
					}
				}
				#endregion
			}
			finally
			{
				qs.ClearList();
				initial.Dispose();
				H?.Dispose(); // dispose the source array
			}
			#endregion
		}
		#endregion


		#region linear solve
		private static (TVec newGuess, TVec newInit, double newNorm) GetNewInitial<TVec, T>(Func<TVec, TVec> MatMulVecFunc, TVec b, TVec oldGuess, List<TVec> qs, T[] vec, bool disposeOld = true)
			where TVec : AbstractArray<T>, IKrylovVector<TVec, T>, new()
			where T : struct, IComparable<T>
		{
			TVec newGuess = null, newInit = null;
			try
			{
				newGuess = oldGuess.OperateOn(qs, vec);
				newGuess.AddBy_αx(oldGuess, Scalars<T>.One); // x_new = x_old + Q * y_n
				newInit = MatMulVecFunc(newGuess);
				newInit.AddBy_αx(b, Scalars<T>.MinusOne);
				newInit.Scale(Scalars<T>.MinusOne); // r_new = b - A * x_new
				double newNorm = newInit.Norm();
				return (newGuess, newInit, newNorm);
			}
			catch (Exception)
			{
				newInit?.Dispose();
				newGuess?.Dispose();
				throw;
			}
			finally
			{
				if (disposeOld) oldGuess.Dispose();
			}
		}

		/// <summary>
		/// The Krylov subspace algorithm for non-Hermitian matrix's linear system problem, a.k.a. GMRES (solve <c>x</c> for <paramref name="MatMulVecFunc"/>(<c>x</c>) = <paramref name="b"/>).
		/// </summary>
		/// <param name="MatMulVecFunc">a function that receives a dense vector input and give the result of the multiplication of the non-Hermitian matrix and the input vector</param>
		/// <param name="b">The vector b</param>
		/// <param name="initGuess">The initial guess vector</param>
		/// <param name="tolerance">The threshold of convergence, default 0 means <c>machine precision * 5</c></param>
		/// <param name="maxRestarts">max number of restarts</param>
		/// <param name="iterPerRestart">iteration number per restart, default 0 means auto calculation</param>
		/// <param name="robustOrthogonalize">perform robust orthogonalization or not, default <c>true</c></param>
		/// <returns>A <typeparamref name="TVec"/> as corresponding solve and a <see cref="double"/> as the relative error.</returns>
		/// <typeparam name="T">The data type, see <see cref="AbstractArray{T}"/> for more information</typeparam>
		/// <typeparam name="TVec">The general dense vector type that inherits <see cref="AbstractArray{T}"/>, <see cref="IKrylovVector{TVec, T}"/> and must be a concrete class type</typeparam>
		/// <exception cref="ArgumentException">if any of the arguments is wrong</exception>
		/// <exception cref="InvalidOperationException">if the <paramref name="MatMulVecFunc"/> throws inner exceptions</exception>
		/// <remarks>Currently, if some eigen-pairs are not converged after maximum number of iterations, they will not be returned.</remarks>
		public static (TVec solve, double error) LinearSolve<TVec, T>(Func<TVec, TVec> MatMulVecFunc, TVec b, TVec initGuess, int maxRestarts = int.MaxValue, int iterPerRestart = 0, double tolerance = 0, bool robustOrthogonalize = true)
			where TVec : AbstractArray<T>, IKrylovVector<TVec, T>, new()
			where T : struct, IComparable<T>
		{
			#region basics
			if (initGuess is null)
				throw new ArgumentNullException(nameof(initGuess));
			if (initGuess.Length != b.Length)
				throw new ArgumentException(Resource.ArraySize, nameof(b));
			if (tolerance < 0)
				throw new ArgumentOutOfRangeException(nameof(tolerance));
			tolerance = Common.GetPrecision<T>(tolerance);
			long size = initGuess.Length;

			// stopwatch
			var stopwatchStart = Stopwatch.StartNew();
			var stopwatch = Stopwatch.StartNew();
			#endregion

			#region flow control

			#region initial
			// check parameters
			Common.CheckLanczosParas<TVec, T>(MatMulVecFunc, initGuess, size, 1, ref iterPerRestart, herm: false);
			Log.Write($"Starting with matrix size = {size}, iterations per restart = {iterPerRestart}");

			// projection matrix
			Array.DenseMatrix<T> H = new Array.DenseMatrix<T>(iterPerRestart, iterPerRestart, onHost: /*(b is Arrays.PureArray<T> p) ? p.OnHost : */true);
			H.FillWithZeros();

			// calculate first r
			TVec solve = null, guess = initGuess.Clone() as TVec;
			TVec residual = MatMulVecFunc(initGuess);
			residual.AddBy_αx(b, Scalars<T>.MinusOne);
			residual.Scale(Scalars<T>.MinusOne);
			double beta = residual.Norm(), normResidual = beta;

			// auto managed list
			double normB = b.Norm();
			List<TVec> qs = new(iterPerRestart);

			// for restart
			T[][] vecs = null;
			bool converge = false;
			int restarts = 0;
			#endregion

			try
			{
				int stagnantCount = 0;
				while (maxRestarts > restarts)
				{
					restarts++;
					#region main
					(converge, vecs) = KrylovSchurInner(MatMulVecFunc, iterPerRestart, robustOrthogonalize, tolerance, a: null, ref beta, H, ref residual, ref qs, convergenceTest: (sizeH, HH, lastBeta, tol) => LinearSolveConvergenceCheck(sizeH, HH, normResidual, lastBeta, tol, normB));
					residual.Dispose(); // the reference will be overwritten later
					if (converge)
					{
						Log.Write($"Converged after {restarts} restarts.");
						break;
					}
					if (!converge && restarts >= maxRestarts)
					{
						Log.Write($"Failed to converge after {restarts} restarts.", level: LogLevel.Warning);
						(_, vecs) = LinearSolveConvergenceCheck(H.IntRows, H, normResidual, beta, tolerance, normB, forceCalc: true);
						break;
					}
					#endregion

					#region log output
					if (stopwatch.Elapsed >= Log.LanczosInfoInterval)
					{
						Log.Write($"Now at restart {restarts}, {stopwatchStart.Elapsed.TotalMinutesString()} passed since start.");
						stopwatch.Restart();
					}
					#endregion

					#region recheck iteration number
					// matrix multiply vector function already checked, do not wast time
					Common.CheckLanczosParas<TVec, T>(null, null, size, 1, ref iterPerRestart, herm: false);
					#endregion

					#region restart
					(_, vecs) = LinearSolveConvergenceCheck(H.IntRows, H, normResidual, beta, tolerance, normB, forceCalc: true);

					(guess, residual, beta) = GetNewInitial(MatMulVecFunc, b, guess, qs, vecs[0], disposeOld: true);

					// actually converge
					if (beta < tolerance * normB)
					{
						Log.Write($"Converged after {restarts} restarts.");
						break;
					}
					// stagnation detect
					if (normResidual / beta <= 1)
					{
						stagnantCount++;
					}
					else
					{
						stagnantCount = 0;
						normResidual = beta;
					}
					if (stagnantCount >= 2)
					{
						Log.Write($"Stagnation detected after {restarts} restarts (usually due to the near-singularity of matrix), algorithm stops.");
						break;
					}
					// dispose old ones
					qs.ClearList(); // the old initial vector is destroyed here since it is the first q vector
					H.FillWithZeros(); // remove info from H
					#endregion
				}
				#region return
				solve = guess.OperateOn(qs, vecs[0]);
				solve.AddBy_αx(guess, Scalars<T>.One); // x_new = x_old + Q * y_n
				return (solve, beta);
				#endregion
			}
			#region exceptions
			catch (Exception)
			{
				solve?.Dispose();
				throw;
			}
			finally // after return
			{
				qs.ClearList();
				H?.Dispose();
				residual?.Dispose();
				guess?.Dispose();
			}
			#endregion

			#endregion
		}
		#endregion
	}
}
