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
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ReadOnlySpan<int> GetConverge<T>(SpanMatrix<T> H, double beta, double tol, WhichEigenvalues which, bool useGap, Span<ComplexDouble> orderedVals, SpanMatrix<ComplexDouble> orderedVecs, Span<int> converged, Span<double> errorBounds) where T : unmanaged
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

		#region preserve selection of Krylov-Schur
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void ConvergenceTestAndRestart<TVec, T>(ref SpanList<ComplexDouble> convergedEigvals, SpanMatrix<ComplexDouble> convergedEigvecs, ref SpanList<TVec> qs, Span<T> a)
			where TVec : class, IKrylovVector<TVec, T>, new()
			where T : unmanaged
		{

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
			Span<T> HSpan = iterSquare.CheckStackLimitFast<T>() ?? stackalloc T[iterSquare];
			SpanMatrix<T> H = new(HSpan, iterPerRestart);

			Span<ComplexDouble> orderedVals = stackalloc ComplexDouble[iterPerRestart];
			Span<ComplexDouble> orderedVecSpan = iterSquare.CheckStackLimitFast<ComplexDouble>() ?? stackalloc ComplexDouble[iterSquare];
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
					KrylovSchurInner(matrixFunction, iterPerRestart, robustOrth, tolerance, a, ref beta, ref H, ref initial, ref qs);
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


		private static void KrylovSchurInner<TVec, T>(Func<TVec, TVec> matrixFunction, int iters, bool robustOrth, double tolerance, ReadOnlySpan<T> a, ref double beta, ref SpanMatrix<T> h, ref TVec initial, ref SpanList<TVec> qs)
			where TVec : class, IKrylovVector<TVec, T>, new()
			where T : unmanaged
		{

		}
	}
}
