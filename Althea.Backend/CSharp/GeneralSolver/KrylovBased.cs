using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Althea.GeneralSolver;
using Althea.Helpers;
using Althea.LinearAlgebra;
using Althea.Linq;
using Althea.NativeTypes;


// Ignore Spelling: \mathbf \overset \longrightarrow \mathrm \cdot \left \right \varepsilon \mathbb \begin \times \le
namespace Althea.Backend.CSharp.Solver
{
	internal static class KrylovBased
	{
		#region get convergence of Krylov-Schur
		private static unsafe void GetSchur<T>(int n, SpanMatrix<T> H, Span<T> outVals, Span<T> outValsImag, SpanMatrix<T> outSchurT, SpanMatrix<T> outSchurU, Span<int> outConjugatePairs) where T : unmanaged, IFloatingPoint<T>
		{
			H.CopyTo(outSchurT);
			//tex:$\mathbf H \overset{\text{Schur (no ordering)}}{\longrightarrow} \mathbf H_c \cdot \mathbf U$
			fixed (T* ptrVals = outVals, ptrSchurT = outSchurT.UnderlyingSpan, ptrSchurU = outSchurU.UnderlyingSpan, ptrValsIm = outValsImag.IsEmpty ? default : outValsImag)
			{
				Mkl.LinearAlgebra.Dense.DenseApi.HessenbergSchur(SolveVectorMode.Vector, n, ptrVals, ptrValsIm, ptrSchurT, outSchurT.LeadDim, ptrSchurU, outSchurU.LeadDim);
			}
			// get conjugate pairs
			if (!NumberType<T>.IsComplex)
			{
				int countPair = 1;
				for (int i = 0; i < n - 1; i++)
				{
					if (outSchurT[i + 1, i] == T.Zero)
					{
						outConjugatePairs[i] = countPair;
						outConjugatePairs[i + 1] = countPair;
						countPair++;
						i++;
					}
				}
			}
		}

		private static unsafe void ReorderSchur<T, TVec>(ReadOnlySpan<int> reorder, int preserveCount, SpanMatrix<T> schurT, SpanMatrix<T> schurU, TVec r, ref SpanList<TVec> qs, Span<T> a, T beta) where T : unmanaged, IFloatingPoint<T> where TVec : class, IKrylovVector<T, TVec>
		{
			int rows = reorder.Length;
			Span<int> identity = stackalloc int[reorder.Length].FillWithRange(0);
			//tex:$\mathbf H \overset{\text{Schur (order}=\vec v_\text{preserve}\text{)}}{\longrightarrow} \mathbf H \cdot \mathbf X$
			fixed (T* ptrT = schurT.UnderlyingSpan, ptrU = schurU.UnderlyingSpan)
			fixed (int* ptrIdentity = identity, ptrOrder = reorder)
			{
				Mkl.LinearAlgebra.Dense.DenseApi.SchurReorder(rows, ptrT, schurT.LeadDim, ptrU, schurU.LeadDim, ptrIdentity, ptrOrder);
			}
			int n = preserveCount;
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
					var newq = IKrylovVector<T, TVec>.OperateOn(qs, X1[i]);
					newq.Normalize();
					newQ.Add(newq);
				}
				qs.ClearList();
				qs.AddRange(SpanHelper.CreateReadOnlySpan(in newQ[0], newQ.Count));
			}
			catch (Exception)
			{
				newQ.ClearList();
				throw;
			}
		}
		private static unsafe void SortPairs<T>(int n, WhichEigenvalues which, Span<T> orderedVals, Span<T> orderedValsImag, SpanMatrix<T> orderedVecs, Span<int> conjugatePairs) where T : unmanaged, IFloatingPoint<T>
		{
			Span<T> reordered = stackalloc T[n];
			if (NumberType<T>.IsComplex)
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
					reordered.Sort(columns, orderedVals.AsSwappers(), conjugatePairs.AsSwappers());
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
					reordered.Sort(columns, orderedVals.AsSwappers(), orderedValsImag.AsSwappers(), conjugatePairs.AsSwappers());
				}
			}
		}

		private static unsafe Span<int> GetConverge<T>(SpanMatrix<T> H, T beta, T tol, WhichEigenvalues which, bool useGap, Span<T> orderedVals, Span<T> orderedValsImag, SpanMatrix<T> orderedVecs, SpanMatrix<T> schurT, SpanMatrix<T> schurU, Span<int> converged, Span<T> errorBounds) where T : unmanaged, IFloatingPoint<T>
		{
			#region Schur decomposition first
			int n = H.Rows;
			Span<int> conjugatePairs = stackalloc int[n];
			GetSchur(n, H, orderedVals, orderedValsImag, schurT, schurU, conjugatePairs);
			schurU.CopyTo(orderedVecs);
			#endregion

			#region get eigenvectors from Schur form
			//tex:$\mathbf H \overset{\text{Eigen}}{\longrightarrow} \mathbf V \cdot \mathrm{diag}(\vec a) \cdot \mathbf V^{-1}
			//\text{ where }\mathbf V = \mathbf U \mathbf X, \mathbf H_c \overset{\text{Eigen}}{\longrightarrow} \mathbf X \mathrm{diag}(\vec a) \mathbf X^{-1}$
			fixed (T* ptrSchurT = schurT.UnderlyingSpan, ptrVecs = orderedVecs.UnderlyingSpan)
			{
				Mkl.LinearAlgebra.Dense.DenseApi.SchurEigenvector(SolveVectorMode.Right, n, ptrSchurT, null, 1, ptrVecs, orderedVecs.LeadDim);
			}
			#endregion

			#region sort eigen-pairs
			// use a method to reduce the stack allocation size
			SortPairs(n, which, orderedVals, orderedValsImag, orderedVecs, conjugatePairs);
			#endregion

			#region get converged
			T normA = orderedVals.Max(static v => T.Abs(v));
			Span<T> lastRow = stackalloc T[n];
			orderedVecs.CopyRowTo(n - 1, lastRow);
			var convergedInd = new SpanList<int>(converged);
			for (int i = 0; i < n; i++)
			{
				T absLastVec = T.Abs(orderedVecs[i, n - 1]);
				errorBounds[i] = beta * absLastVec / normA;
				if (useGap && errorBounds[i] < tol)    // get more precise gap first
					errorBounds[i] = beta * absLastVec / Common.GetGap(beta, tol, orderedVals, lastRow, i, conjugatePairs, normA);
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
		#endregion

		#region preserve selection of Krylov-Schur
		private static void PreserveSelect<T>(int nEig, int n, int convergedWithin, SpanMatrix<T> vecs, ReadOnlySpan<T> orderedVals, ReadOnlySpan<T> orderedValsImag, IPreserveSelector selector, Span<int> valsReorder) where T : unmanaged, IFloatingPoint<T>
		{
			var restVals = orderedVals[convergedWithin..];
			var restValsImag = orderedValsImag.IsEmpty ? default : orderedValsImag[convergedWithin..];
			var restVecs = vecs[convergedWithin..];
			Span<int> preserveIndices = stackalloc int[n];
			preserveIndices = selector.PreserveSelect<T>(restVals, restValsImag, restVecs.UnderlyingSpan, convergedWithin, nEig, n, preserveIndices, false);
			int count = preserveIndices.Length;
			if (count == 1)
				Log.Write(Resource.RestartWarn1, category: nameof(KrylovSchur));
			if (count > n / 2)
				Log.Write(Resource.RestartWarn2, category: nameof(KrylovSchur));
			valsReorder[..convergedWithin].FillWithRange(0);
			count += convergedWithin;
			preserveIndices.CopyTo(valsReorder[convergedWithin..count]);
			Span<int> identity = stackalloc int[orderedVals.Length].FillWithRange(0);
			Span<int> diff = stackalloc int[orderedVals.Length - count];
			identity.SetExept(valsReorder[..count], diff);
			diff.CopyTo(valsReorder[count..]);
		}

		// TODO: inline
		private static void ConvergenceTestAndRestart<T, TVec>(int nEig, Span<int> allConverged, Span<T> orderedVals, Span<T> orderedValsImag, SpanMatrix<T> orderedVecs, ref SpanList<T> convergedEigvals, ref SpanList<T> convergedEigvalsImag, SpanMatrix<T> convergedEigvecs, IPreserveSelector selector, SpanMatrix<T> H, TVec r, ref SpanList<TVec> qs, Span<T> a, T beta) where T : unmanaged, IFloatingPoint<T> where TVec : class, IKrylovVector<T, TVec>
		{
			#region get required converged and rest eigen-pairs
			int n = orderedVals.Length, vecLen = orderedVecs.PresentingLength;
			Span<Complex<double>> vals = stackalloc Complex<double>[n];
			Span<Complex<double>> vecSpan = vecLen.CheckStackLimit<Complex<double>>() ?? stackalloc Complex<double>[vecLen];
			SpanMatrix<Complex<double>> vecs = new(vecSpan, orderedVecs.Rows);
			int convergedWithin = 0;
			for (int i = 0; i < nEig; i++)
			{
				if (allConverged.BinarySearch(i) >= 0)
				{
					vals[convergedWithin] = orderedVals[i];
					orderedVecs[i].CopyTo(vecs[convergedWithin]);
					convergedWithin++;
				}
			}
			int _c = convergedWithin;
			for (int i = 0; i < n; i++)
			{
				if (i >= nEig || allConverged.BinarySearch(i) < 0)
				{
					vals[_c] = orderedVals[i];
					orderedVecs[i].CopyTo(vecs[_c]);
					_c++;
				}
			}
			#endregion

			#region select the Ritz pairs to preserve
			// use a separate method to reduce stack allocation
			var preserveVals = PreserveSelect(nEig, n, convergedWithin, allConverged, vals, vecs, orderedVals, selector);
			#endregion

			#region Schur decomposition and prepare for restart
			// use a separate method to reduce stack allocation
			long actualLen = ReorderSchur(MemoryMarshal.CreateReadOnlySpan(ref preserveVals[0], preserveVals.Length), H, r, ref qs, a, beta);
			// log
			Log.Write($"Actual preserve length = {actualLen} (out of desired {preserveVals.Length})", level: LogLevel.Trace);
			#endregion

			#region return
			convergedEigvals.Clear();
			convergedEigvals.AddRange(MemoryMarshal.CreateReadOnlySpan(ref vals[0], convergedWithin));
			vecs.CopyTo(convergedEigvecs.UnderlyingSpan);
			#endregion
		}

		#endregion

		#region get convergence of GMRES
		private delegate bool LSDelegate<T>(long m, long n, long nrhs, Storage<T> A, long lda, Storage<T> B, long ldb) where T : unmanaged, IFloatingPoint<T>;

		private static readonly Dictionary<RuntimeTypeHandle, Delegate> LSSolve = new();

		private static unsafe bool LinearSolveConvergenceCheck<T>(int n, SpanMatrix<T> H, double β0, double β, double tol, Span<T> convergedVec, bool forceCalc = false) where T : unmanaged, IFloatingPoint<T>
		{
			//tex:$\vec e = \mathrm{Eigen}(\mathbf H)$
			Span<Complex<double>> eigenvalues = stackalloc Complex<double>[n];
			H = H.SubMatrix(..n, ..n);
			int n1 = n + 1;
			int lenNewH = n1 * n * Math.Max(sizeof(T), sizeof(Complex<double>));
			Span<byte> newH = lenNewH.CheckStackLimit<byte>() ?? stackalloc byte[lenNewH];
			if (Const<T>.IsComplex && Const<T>.DataTypeClass == DataTypeClassification.FloatPoint_IEEE754 && Const<T>.DataType.Bytes() == sizeof(double))
			{   // T is Complex<double>
				H.CopyTo(MemoryMarshal.CreateSpan(ref Unsafe.As<byte, T>(ref newH[0]), n * n));
			}
			else
			{
				H.CopyTo(MemoryMarshal.CreateSpan(ref Unsafe.As<byte, Complex<double>>(ref newH[0]), n * n), ConstExtension.GetGenericConverter<T, Complex<double>>());
			}
			double estimateNormA;
			fixed (byte* ptrH = newH)
			fixed (Complex<double>* ptrVals = eigenvalues)
			{
				var matH = new ManagedPureStorage<Complex<double>>(ptrH, n * n);
				var vecE = new ManagedPureStorage<Complex<double>>(ptrVals, n);
				if (EigenSolve is not null)
				{
					EigenSolve.Invoke(SolveVectorMode.NoVector, n, vecE, null, 0, null, 0, matH, n);
				}
				else
				{
					LAD.EigenSpecialMatrixGeneral(SolveVectorMode.NoVector, n, vecE, null, 0, null, 0, matH, n);
				}
				LinearAlgebra.Api.AbsoluteValueMax(vecE, out var maxAbsEig);
				estimateNormA = maxAbsEig.Real;
			}
			//tex:$\mathbf H' = \left[\begin{matrix}\mathbf H\\\vec 0^T,\beta\end{matrix}\right]$
			SpanMatrix<T> Hprime = new(MemoryMarshal.CreateSpan(ref Unsafe.As<byte, T>(ref newH[0]), n1 * n), n1);
			H.CopyTo(Hprime.SubMatrix(..n, ..n));
			Hprime[n, n - 1] = β.FromDouble<T>();
			// direct solving by QR is not slower than separate approach
			//tex:$\min_{\vec y^{(n)}}{\mathbf H' \vec y^{(n)} = \beta_0 \vec e_1}$, $\vec e_1 \in \mathbb F^{n+1}$
			Span<T> y = stackalloc T[n1]; y[0] = β0.FromDouble<T>();
			double normY;
			fixed (T* ptrY = y, ptrR = Hprime.UnderlyingSpan, ptrWork = convergedVec)
			{
				var type = typeof(T).TypeHandle;
				var matR = new ManagedPureStorage<T>(ptrR, n1 * n);
				var vecY = new ManagedPureStorage<T>(ptrY, n1);
				var work = new ManagedPureStorage<T>(ptrWork, n);
				if (!LSSolve.ContainsKey(type))
				{
					LAD? pre = LAD.Current;
					LAD.LeastSquareSolve(n1, n, 1, matR, n1, vecY, n1, work);
					LAD? now = LAD.Current;
					Delegate? d = null;
					pre.SetDelegate<LAD, LSDelegate<T>>(now, nameof(LAD.QRDecomposition), ref d);
					if (d is LSDelegate<T> dd)
						LSSolve.Add(type, dd);
				}
				else
				{
					((LSDelegate<T>)LSSolve[type]).Invoke(n1, n, 1, matR, n1, vecY, n1);
				}
				LinearAlgebra.Api.Norm(vecY.MakeReference(0, n), out normY);
			}
			//tex:converge when: $\|\vec r^{(n)}\| = \|\vec y^{(n)}\| \le \|\mathbf A\| \|\vec{b}\| \varepsilon$
			bool converge = normY <= estimateNormA * tol; // tolerance includes norm of b
			if (converge || forceCalc)
			{
				y[..n].CopyTo(convergedVec[..n]);
			}
			return converge;
		}
		#endregion

		#region final calculation
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void FinalCalcRealT<T, TVec>(TVec v, SpanMatrix<T> real, SpanMatrix<T> imag, ReadOnlySpan<TVec> Q, Span<TVec> vectorReal, Span<TVec> vectorImag)
			where TVec : class, IKrylovVector<T, TVec>
			where T : unmanaged, IFloatingPoint<T>
		{
			try
			{
				for (int i = 0; i < real.Cols; i++)
				{   // initial is not important
					vectorReal[i] = v.OperateOn(Q, real[i]);
					double normReal = vectorReal[i].Norm();
					double normImag = 0;
					if (!imag[i].FastAllZeros())
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
		private static void FinalCalcCompT<T, TVec>(TVec v, SpanMatrix<T> comp, ReadOnlySpan<TVec> Q, Span<TVec> vector)
			where TVec : class, IKrylovVector<T, TVec>
			where T : unmanaged, IFloatingPoint<T>
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


		#region Krylov-Schur
		// null return for not support
		internal static int? KrylovSchur<T, TVec>(Func<TVec, TVec> matrixFunction, TVec initial, WhichEigenvalues which, int maxRestarts, int iterPerRestart, double tolerance, ReorthogonalizeMethod reorthogonalize, bool useGap, IPreserveSelector selector, bool checkFirst, Span<Complex<double>> outEigvals, Span<TVec> outEigvecs, Span<TVec> outEigvecsImag, TimeSpan interval)
			where TVec : class, IKrylovVector<T, TVec>
			where T : unmanaged, IFloatingPoint<T>
		{
			#region basic
			if (initial is null)
				throw new ArgumentNullException(nameof(initial));
			if (tolerance <= 0)
				throw new ArgumentOutOfRangeException(nameof(tolerance), tolerance, Resources.Parameter.MustPositive);
			// check parameters
			int smallestK = outEigvals.Length;
			if (checkFirst)
				Common.CheckParas<T, TVec>(matrixFunction, initial, smallestK, ref iterPerRestart, herm: false);
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

			Span<Complex<double>> orderedVals = stackalloc Complex<double>[iterPerRestart];
			Span<Complex<double>> orderedVecSpan = iterSquare.CheckStackLimit<Complex<double>>() ?? stackalloc Complex<double>[iterSquare];
			SpanMatrix<Complex<double>> orderedVecs = new(orderedVecSpan, iterPerRestart);

			Span<int> orgConvergedIndices = stackalloc int[iterPerRestart];
			Span<double> errorBounds = stackalloc double[iterPerRestart];

			SpanList<Complex<double>> convergedEigvals = new(stackalloc Complex<double>[nEig]);
			Span<Complex<double>> convergedEigvecSpan = stackalloc Complex<double>[iterPerRestart * nEig];
			SpanMatrix<Complex<double>> convergedEigvecs = new(convergedEigvecSpan, iterPerRestart);

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
					KrylovSchurInner(matrixFunction, iterPerRestart, robustOrth, a, ref beta, H, ref r, ref qs);
					// get converged ones
					var convergeInds = GetConverge(H, beta, tolerance, which, useGap, orderedVals, orderedVecs, orgConvergedIndices, errorBounds);
					#endregion

					#region convergence test and restart prepare
					ConvergenceTestAndRestart(nEig, convergeInds, orderedVals, orderedVecs, ref convergedEigvals, convergedEigvecs, selector, H, r, ref qs, a, beta);
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
					var real = MemoryMarshal.CreateSpan(ref Unsafe.As<Complex<double>, T>(ref orderedVecSpan[0]), vecsLen);
					var imag = MemoryMarshal.CreateSpan(ref Unsafe.Add(ref Unsafe.As<Complex<double>, T>(ref orderedVecSpan[0]), vecsLen), vecsLen);
					convergedEigvecs.CopyTo(real, ConstExtension.GetRealPartGetter<Complex<double>, T>());
					convergedEigvecs.CopyTo(imag, ConstExtension.GetImagPartGetter<Complex<double>, T>());
					FinalCalcRealT<T, TVec>(r, new(real, iterPerRestart), new(imag, iterPerRestart), qs, outEigvecs, outEigvecsImag);
				}
				else
				{
					if (typeof(T) == typeof(Complex<double>) || typeof(T) == typeof(Complex<double>))
					{
						SpanMatrix<T> temp = new(MemoryMarshal.CreateSpan(ref Unsafe.As<Complex<double>, T>(ref convergedEigvecs[0, 0]), vecsLen), iterPerRestart);
						FinalCalcCompT(r, temp, qs, outEigvecs);
					}
					else
					{
						SpanMatrix<T> temp = new(stackalloc T[vecsLen], iterPerRestart);
						convergedEigvecs.CopyTo(temp.UnderlyingSpan, ConstExtension.GetGenericConverter<Complex<double>, T>());
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

		private static void KrylovSchurInner<T, TVec>(Func<TVec, TVec> matrixFunction, int iters, bool robustOrth, ReadOnlySpan<T> a, ref double β, SpanMatrix<T> H, ref TVec r, ref SpanList<TVec> qs)
			where TVec : class, IKrylovVector<T, TVec>
			where T : unmanaged, IFloatingPoint<T>
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
						H[j, j - 1] = β.FromDouble<T>();
					}
				}
				//tex:$\beta=\|\vec{r}\|,\ \vec{a}^*=\beta \vec{e}_{j}^*$
				β = r.Norm();
				#endregion
			}
		}
		#endregion


		#region Generalized Minimal Residual (GMRES)
		internal static bool GeneralMinimalResidual<T, TVec>(Func<TVec, TVec> matrixFunction, TVec b, TVec initGuess, int maxRestarts, int iterPerRestart, double tolerance, ReorthogonalizeMethod reorthogonalize, bool checkFirst, out TVec solution, out double relativeError, TimeSpan interval, int maxStagnations)
			where TVec : class, IKrylovVector<T, TVec>
			where T : unmanaged, IFloatingPoint<T>
		{
			#region basic
			if (initGuess is null)
				throw new ArgumentNullException(nameof(initGuess));
			if (b is null)
				throw new ArgumentNullException(nameof(b));
			if (tolerance <= 0)
				throw new ArgumentOutOfRangeException(nameof(tolerance), tolerance, Resources.Parameter.MustPositive);
			// check parameters
			if (checkFirst)
				Common.CheckParas<T, TVec>(matrixFunction, initGuess, 1, ref iterPerRestart, herm: false);
			else
				iterPerRestart = Math.Min(iterPerRestart, Common.NON_HERM_MAX_ITER);
			// check other
			relativeError = double.MaxValue; solution = new();
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
			int iterSquare = iterPerRestart * iterPerRestart;
			Span<T> HSpan = iterSquare.CheckStackLimit<T>() ?? stackalloc T[iterSquare];
			SpanMatrix<T> H = new(HSpan, iterPerRestart);
			// calculate first r
			//tex: $\vec r = \vec b - \mathbf A \vec x_0$
			TVec r = Common.RSetToBSubAx<T, TVec>(matrixFunction,initGuess, b);
			double residual = r.Norm(), oldResidual = residual;
			TVec guess = initGuess.Clone();
			double normB = b.Norm();
			double realTolerance = normB * tolerance;
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
					if (oldResidual / residual <= 1)
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
				solution = guess.OperateOn(qs, convergedVec[..qs.Count]);
				solution.AddBy(guess, Const<T>.One);
				relativeError = residual / normB;
				return true;
				#endregion
			}
			#region dispose
			finally
			{
				r?.Dispose();
				guess?.Dispose();
				qs.ClearList();
			}
			#endregion
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static double GetNewInitial<T, TVec>(Func<TVec, TVec> A, TVec b, TVec guess, ref TVec r, ReadOnlySpan<TVec> qs, ReadOnlySpan<T> vec)
			where TVec : class, IKrylovVector<T, TVec>
			where T : unmanaged, IFloatingPoint<T>
		{
			TVec? temp = null;
			try
			{
				//tex:$\vec x_\text{new} = \vec x_\text{old} + \mathbf Q \vec y$
				temp = guess.OperateOn(qs, vec);
				guess.AddBy(temp, Const<T>.One);
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

		private static bool GMResInner<T, TVec>(Func<TVec, TVec> matrixFunction, int iters, bool robustOrth, double tol, ref double β, SpanMatrix<T> H, ref TVec r, ref SpanList<TVec> qs, Span<T> convergedVec)
			where TVec : class, IKrylovVector<T, TVec>
			where T : unmanaged, IFloatingPoint<T>
		{
			double orgBeta = β;
			Span<T> w = stackalloc T[iters];
			for (int j = 0; j < iters; j++)
			{
				#region main
				//tex: $\vec{q}_j=\vec{r}/\beta$
				r.Scale((1 / β).FromDouble<T>());
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
					H[j, j - 1] = β.FromDouble<T>();
				}
				//tex:$\beta=\|\vec{r}\|,\ \vec{a}^*=\beta \vec{e}_{j}^*$
				β = r.Norm();
				#endregion

				#region convergence test
				if (j > 1)
				{
					bool converge = LinearSolveConvergenceCheck(j + 1, H, orgBeta, β, tol, convergedVec, forceCalc: false);
					if (converge)
						return true;
				}
				#endregion
			}
			return false;
		}
		#endregion
	}
}
