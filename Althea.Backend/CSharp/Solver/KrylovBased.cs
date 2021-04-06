using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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
		private delegate bool SchurDelegate<T>(SolveVectorMode jobu, long n, Storage<T> A, long lda, Storage<T>? U, long ldu, out long actualNumber, Storage<ComplexDouble>? orderVal = null) where T : unmanaged;

		private static readonly Dictionary<RuntimeTypeHandle, Delegate> SchurSolve = new();

		private delegate bool EigenDelegate(SolveVectorMode mode, long n, Storage<ComplexDouble> valOut, Storage<ComplexDouble>? leftVec, long ldvl, Storage<ComplexDouble>? rightVec, long ldvr, Storage<ComplexDouble> A, long lda);

		private static EigenDelegate? EigenSolve = null;

		private delegate bool MultiplyDelegate(MatrixOperation opA, MatrixOperation opB, long m, long n, long k, ComplexDouble α, Storage<ComplexDouble> A, long lda, Storage<ComplexDouble> B, long ldb, ComplexDouble β, Storage<ComplexDouble> C, long ldc);

		private static MultiplyDelegate? MatrixMultiply = null;

		// Ignore Spelling: \mathbf \overset \longrightarrow
		private unsafe static Span<int> GetConverge<T>(SpanMatrix<T> H, double beta, double tol, WhichEigenvalues which, bool useGap, Span<ComplexDouble> orderedVals, SpanMatrix<ComplexDouble> orderedVecs, Span<int> converged, Span<double> errorBounds) where T : unmanaged
		{
			#region Schur decomposition first
			int n = H.Rows;
			Span<ComplexDouble> Hc = H.PresentingLength.CheckStackLimit<ComplexDouble>() ?? stackalloc ComplexDouble[H.PresentingLength];
			Span<ComplexDouble> USchur = H.PresentingLength.CheckStackLimit<ComplexDouble>() ?? stackalloc ComplexDouble[H.PresentingLength];
			Span<int> conjugatePairs = stackalloc int[n];
			GetSchur(n, H, Hc, USchur, conjugatePairs);
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
					errorBounds[i] = beta * absLastVec / Common.GetGap(beta, tol, orderedVals, lastRow, target: i, conjugatePairs, normA);
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

		private static unsafe void GetSchur<T>(int n, SpanMatrix<T> H, Span<ComplexDouble> outHc, Span<ComplexDouble> outUSchur, Span<int> outConjugatePairs) where T : unmanaged
		{
			int lenH = H.PresentingLength;
			Span<T> Hc = lenH.CheckStackLimit<T>() ?? stackalloc T[lenH];
			Span<T> USchur = lenH.CheckStackLimit<T>() ?? stackalloc T[lenH];
			fixed (T* ptrHc = Hc, ptrUSchur = USchur)
			{
				var matHc = new PureStorage<T>(MemoryPointer.Create<T>(new(ptrHc), lenH));
				var matUSchur = new PureStorage<T>(MemoryPointer.Create<T>(new(ptrUSchur), lenH));
				//tex:$\mathbf H \overset{\text{Schur (no ordering)}}{\longrightarrow} \mathbf H_c \mathbf U$
				var type = typeof(T).TypeHandle;
				if (!SchurSolve.ContainsKey(type))
				{
					LAD? pre = LAD.Current;
					LAD.SchurDecomposition(SolveVectorMode.Vector, n, matHc, n, matUSchur, n);
					LAD? now = LAD.Current;
					Delegate? d = null;
					pre.SetDelegate<LAD, SchurDelegate<T>>(now, nameof(LAD.SchurDecomposition), ref d);
					if (d is SchurDelegate<T> dd)
						SchurSolve.Add(type, dd);
				}
				else
				{
					((SchurDelegate<T>)SchurSolve[type]).Invoke(SolveVectorMode.Vector, n, matHc, n, matUSchur, n, out _);
				}
			}
			if (Const<T>.IsComplex && Const<T>.DataTypeClass == DataTypeClassification.FloatPoint_IEEE754 && Const<T>.DataType.Bytes() == sizeof(double))
			{   // T is ComplexDouble
				Hc.CopyTo(MemoryMarshal.CreateSpan(ref Unsafe.As<ComplexDouble, T>(ref outHc[0]), lenH));
				USchur.CopyTo(MemoryMarshal.CreateSpan(ref Unsafe.As<ComplexDouble, T>(ref outUSchur[0]), lenH));
			}
			else
			{   // convert
				Hc.CopyTo(outHc, ConstExtension.GetGenericConverter<T, ComplexDouble>());
				USchur.CopyTo(outUSchur, ConstExtension.GetGenericConverter<T, ComplexDouble>());
			}
			// get conjugate pairs
			if (!Const<T>.IsComplex)
			{
				int _countPair = 1;
				for (int i = 0; i < n - 1; i++)
				{
					if (!Hc[(i + 1) + i * n].IsZero())
					{   // "(i + 1) + i * n" for the sub-diagonal
						outConjugatePairs[i] = _countPair;
						outConjugatePairs[i + 1] = _countPair;
						_countPair++;
						i++;
					}
				}
			}
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
		private static void ConvergenceTestAndRestart<TVec, T>(int nEig, Span<int> allConverged, Span<ComplexDouble> orderedVals, SpanMatrix<ComplexDouble> orderedVecs, ref SpanList<ComplexDouble> convergedEigvals, SpanMatrix<ComplexDouble> convergedEigvecs, IPreserveSelector selector, SpanMatrix<T> H, TVec r, ref SpanList<TVec> qs, Span<T> a, double beta)
			where TVec : class, IKrylovVector<TVec, T>, new()
			where T : unmanaged
		{
			#region get required converged and rest eigen-pairs
			int n = orderedVals.Length, vecLen = orderedVecs.PresentingLength;
			Span<ComplexDouble> vals = stackalloc ComplexDouble[n];
			Span<ComplexDouble> vecSpan = vecLen.CheckStackLimit<ComplexDouble>() ?? stackalloc ComplexDouble[vecLen];
			SpanMatrix<ComplexDouble> vecs = new(vecSpan, orderedVecs.Rows);
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

		private static Span<ComplexDouble> PreserveSelect(int nEig, int n, int convergedWithin, Span<int> allConverged, Span<ComplexDouble> vals, SpanMatrix<ComplexDouble> vecs, Span<ComplexDouble> orderedVals, IPreserveSelector selector)
		{
			var restVals = vals[convergedWithin..];
			var restVecs = vecs[convergedWithin..];
			Span<int> preserveIndices = stackalloc int[n];
			int preserveCount = selector.PreserveSelect(restVals, restVecs.UnderlyingSpan, convergedWithin, nEig, n, output: preserveIndices, withConverged: false);
			preserveIndices = preserveIndices[..preserveCount];
			if (preserveCount == 1)
				Log.Write(Resource.RestartWarn1, category: nameof(KrylovSchur));
			if (preserveCount > n / 2)
				Log.Write(Resource.RestartWarn2, category: nameof(KrylovSchur));
			preserveCount = convergedWithin;
			foreach (var item in preserveIndices)
			{
				vals[preserveCount++] = restVals[item];
			}
			// add all converged ones to prevent stagnation
			if (convergedWithin < allConverged.Length)
			{
				for (int i = 0; i < nEig; i++)
				{
					if (allConverged.BinarySearch(i) >= 0 && !vals[..preserveCount].Contains(orderedVals[i]))
					{
						vals[preserveCount++] = orderedVals[i];
					}
				}
			}
			return vals[..preserveCount];
		}

		private unsafe static long ReorderSchur<TVec, T>(ReadOnlySpan<ComplexDouble> preserveVals, SpanMatrix<T> H, TVec r, ref SpanList<TVec> qs, Span<T> a, double beta)
			where TVec : class, IKrylovVector<TVec, T>, new()
			where T : unmanaged
		{
			// Ignore Spelling: \left \right
			int n = H.Rows, lenH = H.PresentingLength;
			long actualLen;
			//tex:$\mathbf H \overset{\text{Schur (order}=\vec v_\text{preserve}\text{)}}{\longrightarrow} \mathbf H \mathbf X$
			Span<T> X = lenH.CheckStackLimit<T>() ?? stackalloc T[lenH];
			fixed (T* ptrX = X, ptrH = H.UnderlyingSpan)
			fixed (ComplexDouble* ptrVals = preserveVals)
			{
				var matH = new PureStorage<T>(MemoryPointer.Create<T>(new(ptrH), lenH));
				var matX = new PureStorage<T>(MemoryPointer.Create<T>(new(ptrX), lenH));
				var orderVal = new PureStorage<ComplexDouble>(MemoryPointer.Create<ComplexDouble>(new(ptrVals), preserveVals.Length));
				var type = typeof(T).TypeHandle;
				if (SchurSolve.ContainsKey(type))
				{
					((SchurDelegate<T>)SchurSolve[type]).Invoke(SolveVectorMode.Vector, n, matH, n, matX, n, out actualLen, orderVal);
				}
				else
				{
					actualLen = LAD.SchurDecomposition(SolveVectorMode.Vector, n, matH, n, matX, n, orderVal);
				}
			}
			n = (int)actualLen;
			//tex:${\vec{a}}^\ast={\vec{a}}^\ast X^\prime$
			var X1 = new SpanMatrix<T>(X, n)[..n];
			T betaT = beta.FromDouble<T>();
			X1.CopyRowTo(n - 1, a);
			a.Scale(betaT);
			//tex:$H^{\left(n_r\right)}=T_1$ (clear all except the first $n\times n$)
			for (int i = n; i < H.Rows; i++)
			{
				H[i].Clear();
				H.SetRowFrom(i, H[i]);
			}
			//tex:$Q^{\left(n_r\right)}=Q^{\left(k\right)}X_1$
			Span<IntPtr> tempQ = stackalloc IntPtr[qs.Capacity];
			SpanList<TVec> newQ = new(tempQ.AsClassType<TVec>());
			try
			{
				for (int i = 0; i < n; i++)
				{
					var newq = r.OperateOn(qs, X1[i]);
					newq.Normalize();
					newQ.Add(newq);
				}
				qs.ClearList();
				qs.AddRange(MemoryMarshal.CreateReadOnlySpan(ref newQ[0], newQ.Count));
				return actualLen;
			}
			catch (Exception)
			{
				newQ.ClearList();
				throw;
			}
		}
		#endregion

		#region get convergence of GMRES
		private delegate bool QRDelegate<T>(bool full, long m, long n, Storage<T> A, long lda, Storage<T> Q, long ldq) where T : unmanaged;

		private static readonly Dictionary<RuntimeTypeHandle, Delegate> QRSolve = new();

		private delegate bool TriangularSolveDelegate<T>(bool leftA, bool fillUpper, bool unitDiag, MatrixOperation op, long m, long n, T α, Storage<T> A, long lda, Storage<T> B, long ldb) where T : unmanaged;

		private static readonly Dictionary<RuntimeTypeHandle, Delegate> TriangularSolve = new();


		private static unsafe bool LinearSolveConvergenceCheck<T>(int n, SpanMatrix<T> H, double β0, double β, double tol, Span<T> convergedVec, bool forceCalc = false) where T : unmanaged
		{
			// Ignore Spelling: \varepsilon \mathbb
			//tex:$\vec e = \mathrm{Eigen}(\mathbf H)$
			Span<ComplexDouble> eigenvalues = stackalloc ComplexDouble[n];
			H = H.SubMatrix(..n, ..n);
			int n1 = n + 1;
			int lenNewH = n1 * n * Math.Max(sizeof(T), sizeof(ComplexDouble));
			Span<byte> newH = lenNewH.CheckStackLimit<byte>() ?? stackalloc byte[lenNewH];
			if (Const<T>.IsComplex && Const<T>.DataTypeClass == DataTypeClassification.FloatPoint_IEEE754 && Const<T>.DataType.Bytes() == sizeof(double))
			{   // T is ComplexDouble
				H.CopyTo(MemoryMarshal.CreateSpan(ref Unsafe.As<byte, T>(ref newH[0]), n * n));
			}
			else
			{
				H.CopyTo(MemoryMarshal.CreateSpan(ref Unsafe.As<byte, ComplexDouble>(ref newH[0]), n * n), ConstExtension.GetGenericConverter<T, ComplexDouble>());
			}
			fixed (byte* ptrH = newH)
			fixed (ComplexDouble* ptrVals = eigenvalues)
			{
				var matH = new PureStorage<ComplexDouble>(MemoryPointer.Create<ComplexDouble>(new(ptrH), n * n));
				var vecE = new PureStorage<ComplexDouble>(MemoryPointer.Create<ComplexDouble>(new(ptrH), n));
				if (EigenSolve is not null)
				{
					EigenSolve.Invoke(SolveVectorMode.NoVector, n, vecE, null, 0, null, 0, matH, n);
				}
				else
				{
					LAD.EigenSpecialMatrixGeneral(SolveVectorMode.NoVector, n, vecE, null, 0, null, 0, matH, n);
				}
			}
			//tex:$\mathbf H' = \left[\begin{matrix}\mathbf H\\\vec 0^T,\beta\end{matrix}\right]$
			SpanMatrix<T> R = new(MemoryMarshal.CreateSpan(ref Unsafe.As<byte, T>(ref newH[0]), n1 * n), n1);
			H.CopyTo(R.SubMatrix(..n, ..n));
			R[n, n - 1] = β.FromDouble<T>();
			//tex:$\mathbf H' \overset{\text{QR}}\longrightarrow \mathbf U^{(n)} \mathbf R$, $\mathbf U^{(n)} \in \mathbb F^{(n+1) \times (n+1)}$
			Span<T> U = (n1 * n1).CheckStackLimit<T>() ?? stackalloc T[n1 * n1];
			var type = typeof(T).TypeHandle;
			fixed (T* ptrU = U, ptrR = R.UnderlyingSpan)
			{
				var matR = new PureStorage<T>(MemoryPointer.Create<T>(new(ptrR), n1 * n));
				var matU = new PureStorage<T>(MemoryPointer.Create<T>(new(ptrU), n1 * n1));
				if (!QRSolve.ContainsKey(type))
				{
					LAD? pre = LAD.Current;
					LAD.QRDecomposition(full: true, n1, n, matR, n1, matU, n1);
					LAD? now = LAD.Current;
					Delegate? d = null;
					pre.SetDelegate<LAD, QRDelegate<T>>(now, nameof(LAD.QRDecomposition), ref d);
					if (d is QRDelegate<T> dd)
						QRSolve.Add(type, dd);
				}
				else
				{
					((QRDelegate<T>)QRSolve[type]).Invoke(full: true, n1, n, matR, n1, matU, n1);
				}
			}
			//tex:$\beta_0 \left|U_{1,n+1}^{\left(n\right)}\right| < \|A\| \|\vec{b}\| \varepsilon$
			bool converge = β0 * Math.Abs(U[n * n1].ToDouble()) < eigenvalues.Max(static e => e.Abs()) * tol;
			T β0T = β0.FromDouble<T>();
			if (converge || forceCalc)
			{
				//tex:solve for $\vec y_n$: $\mathbf R \vec y_n = \beta_0 \vec U_{1,1:n}^{\left(n\right)}$
				Span<T> y = convergedVec[..n];
				// copy the first row of 'U' (the right hand side to be solved) to 'y'
				new SpanMatrix<T>(U[..(n * n1)], n1).CopyRowTo(0, y);
				fixed (T* ptrR = R.UnderlyingSpan, ptrY = y)
				{
					var matR = new PureStorage<T>(MemoryPointer.Create<T>(new(ptrR), n1 * n));
					var vecY = new PureStorage<T>(MemoryPointer.Create<T>(new(ptrY), n));
					if (!TriangularSolve.ContainsKey(type))
					{
						LAD? pre = LAD.Current;
						LAD.TriangularMatrixSolve(leftA: true, fillUpper: true, unitDiag: false, MatrixOperation.None, n, 1, β0T, A: matR, lda: n1, B: vecY, ldb: n);
						LAD? now = LAD.Current;
						Delegate? d = null;
						pre.SetDelegate<LAD, TriangularSolveDelegate<T>>(now, nameof(LAD.QRDecomposition), ref d);
						if (d is TriangularSolveDelegate<T> dd)
							TriangularSolve.Add(type, dd);
					}
					else
					{
						((TriangularSolveDelegate<T>)TriangularSolve[type]).Invoke(true, true, false, MatrixOperation.None, n, 1, β0T, matR, n1, vecY, n);
					}
				}
			}
			return converge;
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


		#region Krylov-Schur
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
				Common.CheckParas<TVec, T>(matrixFunction, initial, smallestK, ref iterPerRestart, herm: false);
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

		private static void KrylovSchurInner<TVec, T>(Func<TVec, TVec> matrixFunction, int iters, bool robustOrth, ReadOnlySpan<T> a, ref double β, SpanMatrix<T> H, ref TVec r, ref SpanList<TVec> qs)
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
		internal static bool GeneralMinimalResidual<TVec, T>(Func<TVec, TVec> matrixFunction, TVec b, TVec initGuess, int maxRestarts, int iterPerRestart, double tolerance, ReorthogonalizeMethod reorthogonalize, bool checkFirst, out TVec solution, out double relativeError, TimeSpan interval, int maxStagnations)
			where TVec : class, IKrylovVector<TVec, T>, new()
			where T : unmanaged
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
				Common.CheckParas<TVec, T>(matrixFunction, initGuess, 1, ref iterPerRestart, herm: false);
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
			TVec r = Common.RSetToBSubAx<TVec, T>(matrixFunction,initGuess, b);
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
					residual = GetNewInitial<TVec, T>(matrixFunction, b, guess, ref r, qs, convergedVec[..qs.Count]);
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
		private static double GetNewInitial<TVec, T>(Func<TVec, TVec> A, TVec b, TVec guess, ref TVec r, ReadOnlySpan<TVec> qs, ReadOnlySpan<T> vec)
			where TVec : class, IKrylovVector<TVec, T>, new()
			where T : unmanaged
		{
			TVec? temp = null;
			try
			{
				//tex:$\vec x_\text{new} = \vec x_\text{old} + \mathbf Q \vec y$
				temp = guess.OperateOn(qs, vec);
				guess.AddBy(temp, Const<T>.One);
				temp.Dispose();
				//tex:$\vec r_\text{new} = \vec b - \mathbf A \vec x_\text{new}$
				Common.RSetToBSubAx<TVec, T>(A, ref r, guess, b);
				return r.Norm();
			}
			finally
			{
				temp?.Dispose();
			}
		}

		private static bool GMResInner<TVec, T>(Func<TVec, TVec> matrixFunction, int iters, bool robustOrth, double tol, ref double β, SpanMatrix<T> H, ref TVec r, ref SpanList<TVec> qs, Span<T> convergedVec)
			where TVec : class, IKrylovVector<TVec, T>, new()
			where T : unmanaged
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
