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

	internal static class KrylovBased
	{
		#region get convergence of Krylov-Schur
		private delegate bool SchurDelegate(SolveVectorMode jobu, long n, Storage<ComplexDouble> A, long lda, Storage<ComplexDouble>? U, long ldu, out long actualNumber, Storage<ComplexDouble>? orderVal = null);

		private static SchurDelegate? SchurSolve = null;

		private delegate bool EigenDelegate(SolveVectorMode mode, long n, Storage<ComplexDouble> valOut, Storage<ComplexDouble>? leftVec, long ldvl, Storage<ComplexDouble>? rightVec, long ldvr, Storage<ComplexDouble> A, long lda);

		private static EigenDelegate? EigenSolve = null;

		private delegate bool MultiplyDelegate(MatrixOperation opA, MatrixOperation opB, long m, long n, long k, ComplexDouble α, Storage<ComplexDouble> A, long lda, Storage<ComplexDouble> B, long ldb, ComplexDouble β, Storage<ComplexDouble> C, long ldc);

		private static MultiplyDelegate? MatrixMultiply = null;

		// Ignore Spelling: \mathbf \overset \longrightarrow
		private unsafe static ReadOnlySpan<int> GetConverge<T>(SpanMatrix<T> H, double beta, double tol, WhichEigenvalues which, bool useGap, Span<ComplexDouble> orderedVals, SpanMatrix<ComplexDouble> orderedVecs, Span<int> converged, Span<double> errorBounds) where T : unmanaged
		{
			#region Schur decomposition first
			int n = H.Rows;
			Span<ComplexDouble> Hc = H.PresentingLength.CheckStackLimit<ComplexDouble>() ?? stackalloc ComplexDouble[H.PresentingLength];
			Span<ComplexDouble> USchur = H.PresentingLength.CheckStackLimit<ComplexDouble>() ?? stackalloc ComplexDouble[H.PresentingLength];
			if (Const<T>.IsComplex && Const<T>.DataTypeClass == DataTypeClassification.FloatPoint_IEEE754 && Const<T>.DataType.Bytes() == sizeof(double))
			{   // T is ComplexDouble
				H.CopyTo(MemoryMarshal.CreateSpan(ref Unsafe.As<ComplexDouble, T>(ref Hc[0]), H.PresentingLength));
			}
			else
			{	// convert
				H.CopyTo(Hc, ConstExtension.GetGenericConverter<T, ComplexDouble>());
			}
			fixed (ComplexDouble* ptrHc = Hc, ptrUSchur = USchur)
			{
				var matHc = new PureStorage<ComplexDouble>(MemoryPointer.Create<ComplexDouble>(new(ptrHc), H.PresentingLength));
				var matUSchur = new PureStorage<ComplexDouble>(MemoryPointer.Create<ComplexDouble>(new(ptrUSchur), H.PresentingLength));
				var type = typeof(T).TypeHandle;
				//tex:$\mathbf H \overset{\text{Schur (no ordering)}}{\longrightarrow} \mathbf H_c \mathbf U$
				if (SchurSolve is null)
				{
					LAD? pre = LAD.Current;
					LAD.SchurDecomposition(SolveVectorMode.Vector, n, matHc, n, matUSchur, n);
					LAD? now = LAD.Current;
					Delegate? d = null;
					pre.SetDelegate<LAD, SchurDelegate>(now, nameof(LAD.SchurDecomposition), ref d);
					SchurSolve = (SchurDelegate?)d;
				}
				else
				{
					SchurSolve.Invoke(SolveVectorMode.Vector, n, matHc, n, matUSchur, n, out _);
				}
			}
			Span<int> conjugatePairs = stackalloc int[n];
			int _countPair = 1;
			for (int i = 0; i < n - 1; i++)
			{
				if (!Hc[(i + 1) + i * n].IsZero())
				{
					conjugatePairs[i] = _countPair;
					conjugatePairs[i + 1] = _countPair;
					_countPair++;
					i++;
				}
			}
			#endregion

			#region get eigenvalues and eigenvectors
			//tex:$\mathbf H \overset{\text{Eigen}}{\longrightarrow} \mathbf V \mathrm{diag}(\vec a) \mathbf V^{-1}
			//\text{ where }\mathbf V = \mathbf U \mathbf X, \mathbf H_c \overset{\text{Eigen}}{\longrightarrow} \mathbf X \mathrm{diag}(\vec a) \mathbf X^{-1}$
			fixed (ComplexDouble* ptrHc = Hc, ptrUSchur = USchur)
			fixed (ComplexDouble* ptrVals = orderedVals, ptrVecs = orderedVecs.UnderlyingSpan)
			{
				var matHc = new PureStorage<ComplexDouble>(MemoryPointer.Create<ComplexDouble>(new(ptrHc), H.PresentingLength));
				var matU = new PureStorage<ComplexDouble>(MemoryPointer.Create<ComplexDouble>(new(ptrUSchur), H.PresentingLength));
				var vecVal = new PureStorage<ComplexDouble>(MemoryPointer.Create<ComplexDouble>(new(ptrVals), n));
				var matVec = new PureStorage<ComplexDouble>(MemoryPointer.Create<ComplexDouble>(new(ptrVecs), H.PresentingLength));
				//tex:$\mathbf H_c \overset{\text{Eigen}}{\longrightarrow} \mathbf X \mathrm{diag}(\vec a) \mathbf X^{-1}$
				if (EigenSolve is null)
				{
					LAD? pre = LAD.Current;
					LAD.EigenSpecialMatrixGeneral(SolveVectorMode.RightOnly, n, vecVal, matVec, n, null, 0, matHc, n);
					LAD? now = LAD.Current;
					Delegate? d = null;
					pre.SetDelegate<LAD, EigenDelegate>(now, nameof(LAD.EigenSpecialMatrixGeneral), ref d);
					EigenSolve = (EigenDelegate?)d;
				}
				else
				{
					EigenSolve.Invoke(SolveVectorMode.RightOnly, n, vecVal, matVec, n, null, 0, matHc, n);
				}
				//tex:$\mathbf V = \mathbf U \mathbf X$
				if (MatrixMultiply is null)
				{
					LAD? pre = LAD.Current;
					LAD.GeneralMatricesMultiply(MatrixOperation.None, MatrixOperation.None, n, n, n, 1, matU, n, matVec, n, default, matHc, n);
					LAD? now = LAD.Current;
					Delegate? d = null;
					pre.SetDelegate<LAD, MultiplyDelegate>(now, nameof(LAD.GeneralMatricesMultiply), ref d);
					MatrixMultiply = (MultiplyDelegate?)d;
				}
				else
				{
					MatrixMultiply.Invoke(MatrixOperation.None, MatrixOperation.None, n, n, n, 1, matU, n, matVec, n, default, matHc, n);
				}
			}
			#endregion

			#region sort eigen-pairs
			// use a method to reduce the stack allocation size
			SortPairs(n, which, orderedVals, orderedVecs, conjugatePairs);
			#endregion

			#region get converged
			double normA = orderedVals.Max(static v => v.Abs());
			Span<ComplexDouble> lastRow = stackalloc ComplexDouble[n];
			orderedVecs.CopyRowTo(n - 1, lastRow);
			var convergedInd = new SpanList<int>(converged);
			for (int i = 0; i < n; i++)
			{
				double absLastVec = orderedVecs[i, n - 1].Abs();
				errorBounds[i] = beta * absLastVec / normA;
				if (useGap && errorBounds[i] < tol)    // get more precise gap first
					errorBounds[i] = beta * absLastVec / Common.GetGap(beta, normA, orderedVals, lastRow, target: i, conjugatePairs: conjugatePairs);
				if (errorBounds[i] < tol)    // still converged
					convergedInd.Add(i);
			}
			// double check to make sure that all conjugate pairs converge the same time
			if (convergedInd.Count > 0)
			{
				for (int i = 0; i < n; i++)
				{
					var s = convergedInd.AsSpan();
					if (s.Contains(i) && conjugatePairs[i] != 0)
					{
						int ind = conjugatePairs.IndexOf(conjugatePairs[i]);
						if (ind == i)
							ind = conjugatePairs.LastIndexOf(conjugatePairs[i]);
						if (!s.Contains(ind))
							convergedInd.Remove(i); // remove this one
					}
				}
			}
			return convergedInd;
			#endregion
		}



		private static void SortPairs(int n, WhichEigenvalues which, Span<ComplexDouble> orderedVals, SpanMatrix<ComplexDouble> orderedVecs, Span<int> conjugatePairs)
		{
			Span<double> ordered = stackalloc double[n], ordered2 = stackalloc double[n];
			switch (which)
			{
				case WhichEigenvalues.LargestAbsolute:
					orderedVals.CopyTo(ordered, static v => -v.Abs());
					break;
				case WhichEigenvalues.LargestReal:
					orderedVals.CopyTo(ordered, static v => -v.Real);
					break;
				case WhichEigenvalues.LargestAbsoluteImaginary:
					orderedVals.CopyTo(ordered, static v => -Math.Abs(v.Imag));
					break;
				case WhichEigenvalues.SmallestAbsolute:
					orderedVals.CopyTo(ordered, static v => v.Abs());
					break;
				case WhichEigenvalues.SmallestReal:
					orderedVals.CopyTo(ordered, static v => v.Real);
					break;
				case WhichEigenvalues.SmallestAbsoluteImaginary:
					orderedVals.CopyTo(ordered, static v => Math.Abs(v.Imag));
					break;
				default:
					throw new NotSupportedException();
			}
			ordered.CopyTo(ordered2);
			ordered2.Sort(orderedVals);
			ordered.CopyTo(ordered2);
			ordered.Sort(conjugatePairs);
			ordered.CopyTo(ordered2);
			var arrays = orderedVecs.ToArray();
			ordered.Sort((Span<ComplexDouble[]>)arrays);
			orderedVecs.FromArray(arrays);
		}
		#endregion

		#region preserve selection of Krylov-Schur
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void ConvergenceTestAndRestart<TVec, T>(ref SpanList<ComplexDouble> convergedEigvals, SpanMatrix<ComplexDouble> convergedEigvecs, ref SpanList<TVec> qs, Span<T> a)
			where TVec : class, IKrylovVector<TVec, T>, new()
			where T : unmanaged
		{
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
			using var X = new Arrays.DenseMatrix<T>(H.NRows, H.NCols, onHost: true);
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
		#endregion

		#region final calculation
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void FinalCalcRealT<TVec, T>(TVec v, SpanMatrix<T> real, SpanMatrix<T> imag, ReadOnlySpan<TVec> Q, Span<TVec> vectorReal, Span<TVec> vectorImag)
			where TVec : class, IKrylovVector<TVec, T>, new()
			where T : unmanaged
		{
			try
			{
				for (int i = 0; i < real.Cols; i++)
				{   // initial is not important
					vectorReal[i] = v.OperateOn(Q, real[i]);
					double normReal = vectorReal[i].Norm();
					double normImag = 0;
					if (!imag[i].AllZeros())
					{
						vectorImag[i] = v.OperateOn(Q, imag[i]);
						normImag = vectorImag[i].Norm();
					}
					double norm = Math.Sqrt(normReal * normReal + normImag * normImag);
					T normInv = (1 / norm).FromDouble<T>();
					vectorReal[i].Scale(normInv);
					vectorImag[i]?.Scale(normInv);
				}
			}
			catch (Exception)
			{
				vectorReal.ClearSpan();
				vectorImag.ClearSpan();
				throw;
			}
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void FinalCalcCompT<TVec, T>(TVec v, SpanMatrix<T> comp, ReadOnlySpan<TVec> Q, Span<TVec> vector)
			where TVec : class, IKrylovVector<TVec, T>, new()
			where T : unmanaged
		{
			try
			{
				for (int i = 0; i < comp.Rows; i++)
				{
					vector[i] = v.OperateOn(Q, comp[i]);
					vector[i].Normalize();
				}
			}
			catch (Exception)
			{
				vector.ClearSpan();
				throw;
			}
		}
		#endregion


		// null return for not support
		internal static int? KrylovSchur<TVec, T>(Func<TVec, TVec> matrixFunction, TVec initial, WhichEigenvalues which, int maxRestarts, int iterPerRestart, double tolerance, ReorthogonalizeMethod reorthogonalize, bool useGap, IPreserveSelector selector, bool checkFirst, Span<ComplexDouble> outEigvals, Span<TVec> outEigvecs, Span<TVec> outEigvecsImag, TimeSpan interval)
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
				iterPerRestart = Math.Min(iterPerRestart, Common.NON_HERM_MAX_ITER);
			// check other
			int nEig = outEigvals.Length;
			if (iterPerRestart <= nEig)
				return null; // not support
			if (nEig > 6)
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
			double beta = initial.Norm();
			Span<T> a = stackalloc T[iterPerRestart]; a[0] = beta.FromDouble<T>();
			Span<IntPtr> tempQ = stackalloc IntPtr[iterPerRestart];
			var qs = new SpanList<TVec>(tempQ.AsClassType<TVec>());
			int iterSquare = iterPerRestart * iterPerRestart;
			Span<T> HSpan = iterSquare.CheckStackLimit<T>() ?? stackalloc T[iterSquare];
			SpanMatrix<T> H = new(HSpan, iterPerRestart);

			Span<ComplexDouble> orderedVals = stackalloc ComplexDouble[iterPerRestart];
			Span<ComplexDouble> orderedVecSpan = iterSquare.CheckStackLimit<ComplexDouble>() ?? stackalloc ComplexDouble[iterSquare];
			SpanMatrix<ComplexDouble> orderedVecs = new(orderedVecSpan, iterPerRestart);

			Span<int> orgConvergedIndices = stackalloc int[iterPerRestart];
			Span<double> errorBounds = stackalloc double[iterPerRestart];

			SpanList<ComplexDouble> convergedEigvals = new(stackalloc ComplexDouble[nEig]);
			Span<ComplexDouble> convergedEigvecSpan = stackalloc ComplexDouble[iterPerRestart * nEig];
			SpanMatrix<ComplexDouble> convergedEigvecs = new(convergedEigvecSpan, iterPerRestart);

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
					KrylovSchurInner(matrixFunction, iterPerRestart, robustOrth, tolerance, a, ref beta, ref H, ref r, ref qs);
					// get converged ones
					var convergeInds = GetConverge(H, beta, tolerance, which, useGap, orderedVals, orderedVecs, orgConvergedIndices, errorBounds);
					#endregion

					#region convergence test and restart prepare
					ConvergenceTestAndRestart(ref convergedEigvals, convergedEigvecs, ref qs, a);
					if (convergedEigvals.Count >= nEig)
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
					Log.Write($"Krylov-Schur: First unconverged eigenvalue is {orderedVals.FirstOfSetExept(convergedEigvals)}", level: LogLevel.Trace);
					#endregion
				}

				#region check success
				if (!success)
				{
					Log.Write(string.Format(Resource.KrylovSchurFail, maxRestarts, nEig - convergedEigvals.Count));
				}
				#endregion

				#region return
				nEig = Math.Min(nEig, convergedEigvals.Count);
				convergedEigvecs = convergedEigvecs[..nEig];
				int vecsLen = iterPerRestart * nEig;
				if (Const<T>.IsComplex)
				{
					var real = MemoryMarshal.CreateSpan(ref Unsafe.As<ComplexDouble, T>(ref orderedVecSpan[0]), vecsLen);
					var imag = MemoryMarshal.CreateSpan(ref Unsafe.Add(ref Unsafe.As<ComplexDouble, T>(ref orderedVecSpan[0]), vecsLen), vecsLen);
					convergedEigvecs.CopyTo(real, ConstExtension.GetRealPartGetter<ComplexDouble, T>());
					convergedEigvecs.CopyTo(imag, ConstExtension.GetImagPartGetter<ComplexDouble, T>());
					FinalCalcRealT<TVec, T>(r, new(real, iterPerRestart), new(imag, iterPerRestart), qs, outEigvecs, outEigvecsImag);
				}
				else
				{
					if (typeof(T) == typeof(ComplexDouble) || typeof(T) == typeof(Complex<double>))
					{
						SpanMatrix<T> temp = new(MemoryMarshal.CreateSpan(ref Unsafe.As<ComplexDouble, T>(ref convergedEigvecs[0, 0]), vecsLen), iterPerRestart);
						FinalCalcCompT(r, temp, qs, outEigvecs);
					}
					else
					{
						SpanMatrix<T> temp = new(stackalloc T[vecsLen], iterPerRestart);
						convergedEigvecs.CopyTo(temp.UnderlyingSpan, ConstExtension.GetGenericConverter<ComplexDouble, T>());
						FinalCalcCompT(r, temp, qs, outEigvecs);
					}
				}
				convergedEigvals.CopyTo(outEigvals);
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


		private static void KrylovSchurInner<TVec, T>(Func<TVec, TVec> matrixFunction, int iters, bool robustOrth, double tolerance, ReadOnlySpan<T> a, ref double β, ref SpanMatrix<T> H, ref TVec r, ref SpanList<TVec> qs)
			where TVec : class, IKrylovVector<TVec, T>, new()
			where T : unmanaged
		{
			Span<T> w = stackalloc T[iters];
			int nPreserve = qs.Count;
			for (int j = nPreserve; j < iters; j++)
			{
				#region main
				//tex: $\vec{q}_j=\vec{r}/\beta$
				r.Scale((1 / β).FromDouble<T>());
				qs.Add(r);
				//tex: $\vec{r}=A\vec{q}_j$
				r = matrixFunction.Invoke(r);
				//tex:Schmidt orthogonalize, $\vec{r}$ is in-place altered
				Common.RobustOrthogonalize(r, qs, w, robustOrth);
				// Ignore Spelling: \begin
				//tex: $H^{(j)}=\left[\begin{matrix}\begin{matrix}H^{\left(j-1\right)}\\{\vec{a}}^\ast\\\end{matrix}&\vec{w}\\\end{matrix}\right]$
				/*H = new Arrays.DenseMatrix<T>(refArray: H, newRows: j + 1, newCols: j + 1, newLD: H.LeadDim);*/
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
						H[j, j - 1] = β.FromDouble<T>();
					}
				}
				//tex:$\beta=\|\vec{r}\|,\ \vec{a}^*=\beta \vec{e}_{j}^*$
				β = r.Norm();
				#endregion

				#region convergence test
				////if (convergenceTest != null && j > 1)
				////{
				////	var (converge, convergedVectors) = convergenceTest(j + 1, H, β, tol);
				////	if (converge)
				////		return (converge, convergedVectors);
				////}
				#endregion
			}
		}
	}
}
