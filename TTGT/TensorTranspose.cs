using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using CudaCSharp;
using CudaCSharp.Linq;
using CudaCSharp.Memory;
using CudaCSharp.Tensor;
using RT = CudaCSharp.Runtime.API;


namespace TTGT
{
	#region contraction plan struct
	internal readonly struct ContractionPlan : IComparable<ContractionPlan>, IDisposable
	{
		#region basic
		
		// Procedure 1, permute left tensor
		internal readonly int[] LeftPermute;
		
		// Procedure 1, permute right tensor
		internal readonly int[] RightPermute;
		
		// Procedure 2, reshape left tensor to matrix
		internal readonly (long row, long col) LeftReshape;
		
		// Procedure 2, reshape right tensor to matrix
		internal readonly (long row, long col) RightReshape;
		
		// Procedure 3, GEMM of left (and right) matrix, do transpose or not
		internal readonly bool LeftTranspose;
		
		// Procedure 3, GEMM of right (and left) matrix, do transpose or not
		internal readonly bool RightTranspose;
		
		// Procedure 3, GEMM of right (and left) matrix, swap left and right or not
		internal readonly bool SwapLeftRight;
		
		// Procedure 4, reshape output matrix to tensor
		internal readonly long[] OutReshape;
		
		// Procedure 5, permute output tensor
		internal readonly int[] OutPermute;

		internal ContractionPlan(int[] leftPerm, (long row, long col) leftShape, bool leftTrans, int[] rightPerm, (long row, long col) rightShape, bool rightTrans, bool swap, long[] outSize, int[] outPerm)
		{
			if (leftPerm is null || leftPerm.Length == 0)
				throw new ArgumentNullException(nameof(leftPerm));
			if (rightPerm is null || rightPerm.Length == 0)
				throw new ArgumentNullException(nameof(rightPerm));
			if (outSize is null || outSize.Length == 0)
				throw new ArgumentNullException(nameof(outSize));
			if (outPerm is null || outPerm.Length == 0 || outPerm.Length != outSize.Length)
				throw new ArgumentNullException(nameof(outPerm));
			if ((leftTrans != swap ? leftShape.row : leftShape.col) != (rightTrans != swap ? rightShape.col : rightShape.row))
				throw new ArgumentException("Cannot perform matrix multiply");
			long outLen = (leftTrans != swap ? leftShape.col : leftShape.row) * (rightTrans != swap ? rightShape.row : rightShape.col);
			if (outSize.Prod() != outLen)
				throw new ArgumentException("Does not match the matrix shape", nameof(outSize));

			this.LeftPermute = leftPerm; this.RightPermute = rightPerm;
			this.LeftReshape = leftShape; this.RightReshape = rightShape;
			this.LeftTranspose = leftTrans; this.RightTranspose = rightTrans; this.SwapLeftRight = swap;
			this.OutPermute = outPerm; this.OutReshape = outSize;
			this.EstimationTime = GetEstTime(leftPerm, rightPerm, outPerm, swap, leftTrans, rightTrans,
											leftShape.row * leftShape.col, rightShape.row * rightShape.col, outSize.Prod());
		}

		public void Dispose()
		{ 
			// do nothing
		}
		#endregion

		#region equality
		public override bool Equals(object obj)
		{
			if (obj is null)
				return false;
			if (obj is ContractionPlan p)
			{
				if ((this.LeftPermute is null) != (p.LeftPermute is null) || (this.RightPermute is null) != (p.RightPermute is null) ||
					(this.OutReshape is null) != (p.OutReshape is null) || (this.OutPermute is null) != (p.OutPermute is null))
					return false;
				return	(this.LeftPermute == p.LeftPermute || this.LeftPermute.SequenceEqual(p.LeftPermute)) &&
						(this.RightPermute == p.RightPermute || this.RightPermute.SequenceEqual(p.RightPermute)) &&
						(this.LeftReshape == p.LeftReshape) && (this.RightReshape == p.RightReshape) &&
						(this.LeftTranspose == p.LeftTranspose) && (this.RightTranspose == p.RightTranspose) && (this.SwapLeftRight == p.SwapLeftRight) &&
						(this.OutReshape == p.OutReshape || this.OutReshape.SequenceEqual(p.OutReshape)) && 
						(this.OutPermute == p.OutPermute || this.OutPermute.SequenceEqual(p.OutPermute));
			}
			return false;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(LeftPermute.HashCodeOfArray(), RightPermute.HashCodeOfArray(), LeftReshape, RightReshape, HashCode.Combine(LeftTranspose, RightTranspose, SwapLeftRight), OutPermute.HashCodeOfArray(), OutReshape.HashCodeOfArray());
		}

		public static bool operator ==(ContractionPlan left, ContractionPlan right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(ContractionPlan left, ContractionPlan right)
		{
			return !(left == right);
		}
		#endregion

		#region violation
		private const double CoefCannotMultiply = 1.5, CoefContractNotMatch = 1.5;

		/// <summary>
		/// Create a <see cref="ContractionPlan"/> allowing violation of constraints using the minimal number of parameters with respect to the input arguments <paramref name="input"/>
		/// </summary>
		/// <param name="leftPerm">the permutation of left tensor</param>
		/// <param name="rightPerm">the permutation of right tensor</param>
		/// <param name="swap">swap the tensors or not</param>
		/// <param name="input">the constant input arguments in <see cref="ContractionInput"/></param>
		/// <returns>a <see cref="ContractionPlan"/> if the input is valid; or a <see cref="double"/> indicates how much it violates the constraints</returns>
		internal static (ContractionPlan? plan, (double cost, double breach)? value) CreateAllowViolation(int[] leftPerm, int[] rightPerm, bool swap, in ContractionInput input)
		{
			// calculate out perm
			int[] outPerm; long[] outSize;
			var output = input.TryGetOutputFromInput(leftPerm, rightPerm, swap);
			if (!output.HasValue)
				return (null, (0, 1E100));
			(outSize, outPerm) = output.Value;
			double baseTime = GetEstTime(leftPerm, rightPerm, outPerm, swap, swap, !swap,		//swap as leftTrans, !swap as rightTrans
									input.LeftSize.Prod(), input.RightSize.Prod(), input.OutSize.Prod()); // to get the max possible base time

			int concRank = input.LeftContractIndex.Length, freeLenL = input.LeftFreeIndex.Length, freeLenR = input.RightFreeIndex.Length;
			bool leftTrans = input.LeftContractIndex.SetEquals(leftPerm[..concRank]) &&
								input.LeftFreeIndex.SetEquals(leftPerm[^freeLenL..]), // contract at first
				leftNoTrans = input.LeftContractIndex.SetEquals(leftPerm[^concRank..]) &&
								input.LeftFreeIndex.SetEquals(leftPerm[..freeLenL]); // contract at last
			bool rightTrans = input.RightContractIndex.SetEquals(rightPerm[^concRank..]) &&
								input.RightFreeIndex.SetEquals(rightPerm[..freeLenR]), // contract at last
				rightNoTrans = input.RightContractIndex.SetEquals(rightPerm[..concRank]) &&
								input.RightFreeIndex.SetEquals(rightPerm[^freeLenR..]); // contract at first
			if ((!leftNoTrans && !leftTrans) && (!rightNoTrans && !rightTrans))
				return (null, (baseTime, CoefCannotMultiply * (input.LeftSize.Prod() + input.RightSize.Prod())));
			else if (!leftNoTrans && !leftTrans)
				return (null, (baseTime, CoefCannotMultiply * input.LeftSize.Prod()));
			else if (!rightNoTrans && !rightTrans)
				return (null, (baseTime, CoefCannotMultiply * input.RightSize.Prod()));

			// re calculate base time
			baseTime = GetEstTime(leftPerm, rightPerm, outPerm, swap, leftTrans != swap, rightTrans != swap,
									input.LeftSize.Prod(), input.RightSize.Prod(), input.OutSize.Prod());

			int[] leftConc = leftTrans ? leftPerm[..concRank] : leftPerm[^concRank..],
				rightConc = rightTrans ? rightPerm[^concRank..] : rightPerm[..concRank];
			long concLen = input.LeftSize.ReOrder(input.LeftContractIndex).Prod();
			if (!input.LeftContractIndex.Zip(input.RightContractIndex).SetEquals(leftConc.Zip(rightConc)))
				return (null, (baseTime, CoefContractNotMatch * concLen));

			// no violation
			long leftFreeLen = input.LeftSize.ReOrder(input.LeftFreeIndex).Prod(),
				rightFreeLen = input.RightSize.ReOrder(input.RightFreeIndex).Prod();
			return (new ContractionPlan(leftPerm, leftTrans ? (concLen, leftFreeLen) : (leftFreeLen, concLen), leftTrans != swap,
									rightPerm, rightTrans ? (rightFreeLen, concLen) : (concLen, rightFreeLen), rightTrans != swap,
									swap, outSize, outPerm), null);
		}
		#endregion

		#region compare
		// The ratio of time complexity of a non-trivial permutation (cannot be achieved by matrix transposition) to a trivial one (can be achieved by matrix transposition)
		private const double ComplicatedToTrivialRatio = 2.0;
		private const double TransToNonTransRatio = 0.05;

		/// <summary>
		/// The estimated execution time of this plan, <b>not</b> in seconds, can only be used for comparison
		/// </summary>
		public double? EstimationTime { get; }

		private static double GetEstTime(int[] leftPerm, int[] rightPerm, int[] outPerm, bool swap, bool transL, bool transR, long leftLength, long rightLength, long outLength)
		{
			double timeLPerm = leftPerm.SequenceEqual(ArrayLinq.Range(0, leftPerm.Length)) ? 0 :
								leftPerm.IsTrivialPermute().HasValue ? leftLength : leftLength * ComplicatedToTrivialRatio;
			double timeRPerm = rightPerm.SequenceEqual(ArrayLinq.Range(0, rightPerm.Length)) ? 0 :
								rightPerm.IsTrivialPermute().HasValue ? rightLength : rightLength * ComplicatedToTrivialRatio;
			double timeOPerm = outPerm.SequenceEqual(ArrayLinq.Range(0, outPerm.Length)) ? 0 :
								outPerm.IsTrivialPermute().HasValue ? outLength : outLength * ComplicatedToTrivialRatio;
			double leftTrans = leftLength * (transL != swap ? 0 : TransToNonTransRatio);
			double rightTrans = rightLength * (transR != swap ? TransToNonTransRatio : 0);
			return timeLPerm + timeRPerm + timeOPerm + leftTrans + rightTrans;
		}

		/// <summary>
		/// Compare the estimated execution time of this plan to the <paramref name="other"/> plan
		/// </summary>
		/// <param name="other">the other plan</param>
		/// <returns>0 if <c>this == <paramref name="other"/></c>; above zero if the estimation time cost of <c>this > <paramref name="other"/></c>; below zero otherwise</returns>
		public int CompareTo(ContractionPlan other)
		{
			if (this.Equals(other))
				return 0;
			if (!this.EstimationTime.HasValue)
				return 1;
			else if (!other.EstimationTime.HasValue)
				return -1;
			return this.EstimationTime.Value.CompareTo(other.EstimationTime.Value);
		}

		/// <summary>
		/// Smaller operator
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns><paramref name="left"/> has smaller <see cref="EstimationTime"/> than <paramref name="right"/> or not</returns>
		public static bool operator <(ContractionPlan left, ContractionPlan right)
		{
			return left.CompareTo(right) < 0;
		}

		/// <summary>
		/// Smaller or equal operator
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns><paramref name="left"/> has smaller or the same <see cref="EstimationTime"/> than <paramref name="right"/> or not</returns>
		public static bool operator <=(ContractionPlan left, ContractionPlan right)
		{
			return left.CompareTo(right) <= 0;
		}

		/// <summary>
		/// Larger operator
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns><paramref name="left"/> has larger <see cref="EstimationTime"/> than <paramref name="right"/> or not</returns>
		public static bool operator >(ContractionPlan left, ContractionPlan right)
		{
			return left.CompareTo(right) > 0;
		}

		/// <summary>
		/// Larger or equal operator
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns><paramref name="left"/> has larger or the same <see cref="EstimationTime"/> than <paramref name="right"/> or not</returns>
		public static bool operator >=(ContractionPlan left, ContractionPlan right)
		{
			return left.CompareTo(right) >= 0;
		}
		#endregion
	}
	#endregion

	#region contraction static class
	#region checker
	internal static class Checker
	{
		internal static void Check(this CuTT.CuTTResult err, string name)
		{
			if (err != CuTT.CuTTResult.Success)
			{
				throw new StatusException(err, name);
			}
		}

		internal static void Check(this CuTT.CuTTResult err)
		{
			if (err != CuTT.CuTTResult.Success)
			{
				throw new StatusException(err, new System.Diagnostics.StackTrace(0));
			}
		}
	}
	#endregion

	#region extending methods
	internal static class Contraction
	{
		internal static bool IsPermutation(this IReadOnlyList<int> perm) =>
			perm != null && perm.Count != 0 && perm.Except(ArrayLinq.Range(0, perm.Count)).Count == 0;

		internal static bool IsIdentityPermutation(this IReadOnlyList<int> perm) =>
			perm != null && perm.Count != 0 && perm.SequenceEqual(ArrayLinq.Range(0, perm.Count));

		internal static (long row, long col)? IsTrivialPermute(this int[] perm, params long[] size)
		{
			if (!IsPermutation(perm))
				throw new ArgumentException("Not a permutation", nameof(perm));
			if (size != null && size.Length != 0 && perm.Length != size.Length)
				throw new ArgumentException($"{nameof(perm)} and {nameof(size)} have different length");
			if (size != null && size.Length != 0 && size.Contains(1))
				throw new ArgumentOutOfRangeException("Trim the length-1 index first", nameof(size));

			return IsTrivialPermute((ReadOnlySpan<int>)perm, size);
		}

		internal static (long row, long col)? IsTrivialPermute(this ReadOnlySpan<int> perm, long[] size)
		{
			int zeroPos = perm.IndexOf(0);
			for (int i = (zeroPos + 1) % perm.Length, j = 1; i != zeroPos; i = (i + 1) % perm.Length, j++)
			{
				if (perm[i] != j)
					return null;
			}
			if (size == null || size.Length == 0)
				return (0, 0);
			if (zeroPos != 0)
				return (size[..^zeroPos].Prod(), size[^zeroPos..].Prod());
			else
				return (size.Prod(), 1);
		}

		internal static (long[] size, int[] permNeed)? TryGetOutputFromInput(this ContractionInput input, int[] leftPerm, int[] rightPerm, bool swap)
		{
			if (!leftPerm.IsPermutation() || !rightPerm.IsPermutation())
				return null;
			if (leftPerm.Length != input.LeftSize.Length || rightPerm.Length != input.RightSize.Length)
				return null;

			var leftOut = input.LeftFreeIndex.Zip(input.OutLeftFreeIndex).OrderBy(i => Array.IndexOf(leftPerm, i.First)).Select(i => i.Second);
			var rightOut = input.RightFreeIndex.Zip(input.OutRightFreeIndex).OrderBy(i => Array.IndexOf(rightPerm, i.First)).Select(i => i.Second);

			int[] outPermNow = (swap ? rightOut.Concat(leftOut) : leftOut.Concat(rightOut)).ToArray();
			long[] outSizeNow = input.OutSize.InverseOrder(outPermNow);

			return (outSizeNow, outPermNow);
		}
	}
	#endregion
	#endregion


	#region common
	internal readonly struct Common
	{
		#region base
		private readonly CudaCSharp.Blas.IBlas blas;
		private readonly CudaCSharp.Tensor.ITensor caller;

		internal Common(CudaCSharp.Blas.IBlas blas, CudaCSharp.Tensor.ITensor caller)
		{
			this.blas = blas; this.caller = caller;
		}
		#endregion

		#region operations
		internal (DataType type, bool trivial) PermuteDoTrivial<T>(Storage<T> A, long[] sizeA, T α, UnitaryOperation op, Storage<T> B, Span<int> size, ReadOnlySpan<int> permAToB) where T : struct, IComparable<T>
		{
			for (int i = 0; i < sizeA.Length; i++)
			{
				size[i] = (int)sizeA[i];
			}
			var type = default(T).ToDataType();
			// check support
			if (!type.IsFloat() && op != UnitaryOperation.Identity && !α.IsOne())
				throw new NotSupportedException();
			if (op == UnitaryOperation.Negate)
			{
				α = α.GenericNegate();
				op = UnitaryOperation.Identity;
			}
			if (op != UnitaryOperation.Identity && op != UnitaryOperation.Conjugate)
				throw new NotSupportedException();
			// check complex type
			if (type.IsReal() && op == UnitaryOperation.Conjugate)
			{
				op = UnitaryOperation.Identity;
			}

			// check trivial
			var trivial = permAToB.IsTrivialPermute(sizeA);
			if (trivial.HasValue)
			{ // shortcut
				var trans = op == UnitaryOperation.Conjugate ? MatrixOperation.ConjugateTranspose : MatrixOperation.Transpose;
				var (rowL, colL) = trivial.Value;
				int row = checked((int)rowL), col = checked((int)colL);
				this.blas.GeneralMatricesAdd(opA: trans, opB: trans, m: col, n: row,
											α: α, A: A, lda: row,
											β: Scalars<T>.Zero, B: A, ldb: row, // since there are no B, β should be 0, B should be A
											C: B, ldc: col);
				return (type, true);
			}
			return (type, false);
		}

		internal void Contract<T>(T α, Storage<T> A, Storage<T> B, T β, Storage<T> C, Storage<T> D, long[] sizeA, long[] sizeB, long[] sizeC, ReadOnlySpan<int> concA, ReadOnlySpan<int> concB, ReadOnlySpan<int> freeA, ReadOnlySpan<int> freeCA, ReadOnlySpan<int> freeB, ReadOnlySpan<int> freeCB) where T : struct, IComparable<T>
		{
			#region get plan from cache
			ContractionCache<ContractionPlan>.TryGet(sizeA, sizeB, sizeC, concA, concB, freeA, freeCA, freeB, freeCB, out var planNullable, out var input);
			ContractionPlan plan; // final contract plan
			if (planNullable.HasValue)
			{
				plan = planNullable.Value;
			}
			else
			{
				plan = Optimizer.Optimizer.Optimize(in input);
				ContractionCache<ContractionPlan>.Add(input, plan);
			}
			#endregion

			Storage<T> AA = null, BB = null, CC = null;
			try
			{
				#region permute A, B if necessary
				AA = plan.LeftPermute.IsIdentityPermutation() ? A : Storage<T>.Create(sizeA.Prod(), A.OnHost);
				if (AA != A)
					this.caller.Permute(A, sizeA, Scalars<T>.One, UnitaryOperation.Identity, AA, sizeA.ReOrder(plan.LeftPermute), plan.LeftPermute);
				BB = plan.RightPermute.IsIdentityPermutation() ? B : Storage<T>.Create(sizeB.Prod(), B.OnHost);
				if (BB != B)
					this.caller.Permute(B, sizeB, Scalars<T>.One, UnitaryOperation.Identity, BB, sizeB.ReOrder(plan.RightPermute), plan.RightPermute);
				#endregion

				#region get sizes
				int ldA = checked((int)plan.LeftReshape.row), ldB = checked((int)plan.RightReshape.row),
					sdA = checked((int)plan.LeftReshape.col), sdB = checked((int)plan.RightReshape.col);
				bool transA = plan.LeftTranspose, transB = plan.RightTranspose;
				if (plan.SwapLeftRight)
				{
					(ldA, ldB) = (ldB, ldA);
					(sdA, sdB) = (sdB, sdA);
					(transA, transB) = (transB, transA);
					// do not swap AA BB since they may be used to dispose
				}
				int m = transA ? sdA : ldA, k = transA ? ldA : sdA, n = transB ? ldB : sdB;
				#endregion

				#region do GEMM
				bool idenC = plan.OutPermute.IsIdentityPermutation();
				long lenC = sizeC.Prod();
				CC = idenC ? D : Storage<T>.Create(lenC, D.OnHost);
				T gemmBeta = idenC ? β : Scalars<T>.Zero;
				if (idenC)
				{
					// CC = C
					if (!β.IsZero() && D != C)
						RT.CopyTo(source: C, dest: CC, length: sizeC.Prod());
				}
				// CC = α * AA * BB + gemmBeta * CC
				this.blas.GeneralMatricesMultiply(	opA: transA ? MatrixOperation.Transpose : MatrixOperation.None,
													opB: transB ? MatrixOperation.Transpose : MatrixOperation.None,
													m, n, k, α,
													A: plan.SwapLeftRight ? BB : AA, lda: ldA,
													B: plan.SwapLeftRight ? AA : BB, ldb: ldB,
													β: gemmBeta, C: CC, ldc: m);
				#endregion

				#region final permute
				// gemmBeta == 0, do final addition and permute
				if (!idenC)
				{
					int lenD = checked((int)lenC);
					T one = Scalars<T>.One;
					bool zeroBeta = β.IsZero();
					Span<int> outPermInv = stackalloc int[plan.OutPermute.Length];
					plan.OutPermute.InversePermutationTo(outPermInv);
					if (zeroBeta) // no matters whether C == D
					{
						// D = perm(CC)
						this.caller.Permute(CC, plan.OutReshape.ToArray(), one, UnitaryOperation.Identity, D, sizeC, outPermInv);
					}
					else if (C == D) // !zeroBeta
					{
						// DD = perm(CC)
						using var DD = Storage<T>.Create(lenC, D.OnHost);
						this.caller.Permute(CC, plan.OutReshape.ToArray(), one, UnitaryOperation.Identity, DD, sizeC, outPermInv);
						// D = β * D + DD
						if (β.CompareTo(one) != 0)
							this.blas.Scale(lenD, β, D, 1);
						this.blas.VectorGeneralAdd(lenD, one, DD, 1, D, 1);
					}
					else // C != D && !zeroBeta
					{
						// DD = perm(CC)
						using var DD = Storage<T>.Create(lenC, D.OnHost);
						this.caller.Permute(CC, plan.OutReshape.ToArray(), one, UnitaryOperation.Identity, DD, sizeC, outPermInv);
						// D += DD
						this.blas.VectorGeneralAdd(lenD, one, DD, 1, D, 1);
						// D += β * C
						this.blas.VectorGeneralAdd(lenD, β, C, 1, D, 1);
					}
				}
				#endregion
			}
			finally
			{
				if (AA != A) AA?.Dispose();
				if (BB != B) BB?.Dispose();
				if (CC != D) CC?.Dispose();
			}
		}

		internal void Reduce<T>(BinaryOperation reduction, T α, UnitaryOperation opA, Storage<T> A, long[] sizeA, T β, UnitaryOperation opC, Storage<T> C, long[] sizeC, Storage<T> D, ReadOnlySpan<int> permAToC) where T : struct, IComparable<T>
		{
			if (reduction != BinaryOperation.Add)
				throw new NotImplementedException($"Reduction operation {reduction} is not implemented yet.");
			Storage<T> AA = null;
			try
			{
				#region permute A if necessary
				Span<int> idenPart = stackalloc int[sizeA.Length - sizeC.Length];
				idenPart.FillWithRange(start: 0);
				AA = permAToC.SequenceEqual(idenPart) ? A : Storage<T>.Create(sizeA.Prod(), A.OnHost);
				long[] realSizeA = sizeA;
				if (AA != A)
				{
					Span<int> perm = stackalloc int[sizeA.Length];
					permAToC.CopyTo(perm.Slice(0, permAToC.Length));
					int now = permAToC.Length;
					for (int i = 0; i < sizeA.Length; i++)
					{
						if (!permAToC.Contains(i))
							perm[now++] = i;
					}
					realSizeA = sizeA.ReOrder(perm);
					this.caller.Permute(A, sizeA, Scalars<T>.One, opA, AA, realSizeA, perm);
				}
				#endregion

				#region get number of columns and rows
				long cols = realSizeA[^sizeC.Length..].Prod(), rows = realSizeA[..^sizeC.Length].Prod();
				int row = checked((int)rows), col = checked((int)cols);
				#endregion

				#region create ones array
				using var tempOnes = Storage<T>.Create(rows, A.OnHost);
				this.blas.FillWithOnes(tempOnes, rows);
				#endregion

				#region GEMV
				// D = C if necessary
				if (!β.IsZero() && D != C)
				{
					if (opC == UnitaryOperation.Negate)
					{
						β = β.GenericNegate(); opC = UnitaryOperation.Identity;
					}
					else if (opC != UnitaryOperation.Identity && opC != UnitaryOperation.Conjugate)
						throw new NotImplementedException($"Unitary operation {opC} is not implemented yet.");
					RT.CopyTo(source: C, dest: D, length: sizeC.Prod());
					if (opC == UnitaryOperation.Conjugate)
						blas.PointWiseConjugate(D, sizeC.Prod());
				}
				this.blas.GeneralMatrixMultiplyVector(MatrixOperation.Transpose, row, col, α, AA, row, tempOnes, 1, β, D, 1);
				#endregion
			}
			finally
			{
				if (AA != A) AA.Dispose();
			}
		}
		#endregion
	}
	#endregion
}

namespace TTGT.CuTT
{
	#region natives
	internal enum CuTTResult
	{
		Success,
		InvalidPlan,        // Invalid plan handle
		InvalidParameter,   // Invalid input parameter
		InvalidDevice,      // Execution tried on device different than where plan was created
		InternalError,      // Internal error
		UndefinedError,     // Undefined error
	}

	internal static class NativeMethods
	{
		public const string CUTT_DLL_NAME = @"cuTT";

#pragma warning disable IDE1006 // naming
		/// <summary>
		/// Create the permutation plan using heuristic method
		/// </summary>
		/// <param name="handle">Returned handle to cuTT plan</param>
		/// <param name="rank">Rank of the tensor</param>
		/// <param name="size">Dimensions / size of the tensor</param>
		/// <param name="permutation">Transpose permutation, e.g. {0,3,1,2}</param>
		/// <param name="sizeofType">Size of the elements of the tensor in bytes (must 4 or 8)</param>
		/// <param name="stream">CUDA stream (0 if no stream is used)</param>
		/// <param name="estTime">returned estimation execution time</param>
		/// <returns>Success/unsuccessful code <see cref="CuTTResult"/></returns>
		[DllImport(CUTT_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CuTTResult cuttPlan(ref uint handle, int rank, in int size, in int permutation, long sizeofType, IntPtr stream, ref double estTime);

		/// <summary>
		/// Destroy the plan
		/// </summary>
		/// <param name="handle">Handle to the cuTT plan</param>
		/// <returns>Success/unsuccessful code <see cref="CuTTResult"/></returns>
		[DllImport(CUTT_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CuTTResult cuttDestroy(uint handle);

		/// <summary>
		/// Execute plan out-of-place
		/// </summary>
		/// <param name="handle">Handle to cuTT plan</param>
		/// <param name="idata">Input data</param>
		/// <param name="odata">Output data</param>
		/// <returns>Success/unsuccessful code <see cref="CuTTResult"/></returns>
		[DllImport(CUTT_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern CuTTResult cuttExecute(uint handle, IntPtr idata, IntPtr odata);
#pragma warning restore IDE1006 // naming
	}
	#endregion

	/// <summary>
	/// The class that inherits <see cref="CudaCSharp.Tensor.ITensor"/> with underlying library "CuTT"
	/// </summary>
	public sealed class CUTensor : CudaCSharp.Tensor.ITensor
	{
		#region base
		private readonly Common common;

		/// <summary>
		/// default constructor
		/// </summary>
		public CUTensor()
		{
			this.common = new Common(CudaCSharp.Blas.API.GPU, this);
		}

		private readonly struct CuttPlan : IDisposable
		{
			internal readonly uint handle;

			public void Dispose()
			{
				try
				{
					NativeMethods.cuttDestroy(handle).Check();
				}
				catch (StatusException e)
				{
					Log.Write(e.Message, level: LogLevel.Error);
				}
			}

			internal CuttPlan(uint handle) => this.handle = handle;
		}

		/// <summary>
		/// default disposition
		/// </summary>
		public void Dispose()
		{
			PermuteCache<CuttPlan>.Clear();
		}
		#endregion

		#region operations
		/// <summary>
		/// Permute (general transpose) and scale this tensor to form a new tensor: $B_{i_0,i_1,...,i_n} = \alpha \Psi(A_{\Pi(i_0,i_1,...,i_n)})$.
		/// </summary>
		/// <param name="α">the scalar to multiply</param>
		/// <param name="op">the <see cref="UnitaryOperation"/> <c>Ψ</c> to apply on each element before scaling</param>
		/// <param name="A">the source tensor</param>
		/// <param name="sizeA">size/extent of <paramref name="A"/></param>
		/// <param name="B">the output tensor</param>
		/// <param name="sizeB">size/extent of <paramref name="B"/></param>
		/// <param name="permAToB">the permutation order from <paramref name="A"/> to <paramref name="B"/></param>
		public void Permute<T>(Storage<T> A, long[] sizeA, T α, UnitaryOperation op, Storage<T> B, long[] sizeB, ReadOnlySpan<int> permAToB) where T : struct, IComparable<T>
		{
			Span<int> size = stackalloc int[sizeA.Length + 1];
			var (type, trivial) = this.common.PermuteDoTrivial(A, sizeA, α, op, B, size, permAToB);
			if (trivial) // trivial transpose done in PermuteDoTrivial
				return;
			Span<int> perm = stackalloc int[permAToB.Length + 1];
			// check complex type
			if (!type.IsReal())
			{
				// get new size
				Span<int> temp = stackalloc int[sizeA.Length];
				size.Slice(0, temp.Length).CopyTo(temp);
				size[0] = 2; temp.CopyTo(size.Slice(1));
				// get new perm
				perm[0] = 0;
				for (int i = 0; i < permAToB.Length; i++)
				{
					perm[i + 1] = permAToB[i] + 1;
				}
				type = type.RealCorrespond();
			}
			else
			{
				permAToB.CopyTo(perm.Slice(0, permAToB.Length));
				size = size.Slice(0, sizeA.Length);
			}

			// non-trivial
			long sizeOfT = type.Bytes();
			uint handle = 0;
			PermuteCache<CuttPlan>.TryGet(perm, size, out var plan, out var input);
			if (plan.HasValue)
				handle = plan.Value.handle;
			else
			{
				double estTime = 0;
				NativeMethods.cuttPlan(ref handle, size.Length, in size.Ref(), in perm.Ref(), sizeOfT, IntPtr.Zero, ref estTime).Check();
				PermuteCache<CuttPlan>.Add(input, new CuttPlan(handle));
			}
			NativeMethods.cuttExecute(handle, A, B).Check(); // perform transpose
			if (!α.IsOne() || op == UnitaryOperation.Conjugate)
			{
				if (!α.IsOne())
					CudaCSharp.Blas.API.GPU.Scale(size.Prod(), α, B, 1);
				if (op == UnitaryOperation.Conjugate)
					CudaCSharp.Blas.API.GPU.PointWiseConjugate(B, size.Prod());
			}
		}

		/// <summary>
		/// Contract two tensors <paramref name="A"/> and <paramref name="B"/>: $D_{i_0,i_1,...,i_n} = \alpha \sum_{j_a = k_b}{A_{j_0,j_1,...,j_p} \cdot B_{k_0,k_1,...,k_q}} + \beta C_{i_0,i_1,...,i_n}$;
		/// </summary>
		/// <param name="α">scalar α</param>
		/// <param name="A">tensor A</param>
		/// <param name="β">scalar β</param>
		/// <param name="B">tensor B</param>
		/// <param name="C">tensor C</param>
		/// <param name="D">output tensor D</param>
		/// <param name="sizeA">left tensor's size/extent</param>
		/// <param name="sizeB">right tensor's size/extent</param>
		/// <param name="sizeC">output tensor's size/extent</param>
		/// <param name="concA">sorted left tensor's contract indices</param>
		/// <param name="concB">right tensor's contract indices sorted by <paramref name="concA"/></param>
		/// <param name="freeA">left tensor's free indices sorted by <paramref name="freeCA"/></param>
		/// <param name="freeCA">output tensor's indices corresponding to left tensor's</param>
		/// <param name="freeB">right tensor's free indices sorted by <paramref name="freeCB"/></param>
		/// <param name="freeCB">output tensor's indices corresponding to right tensor's</param>
		public void Contract<T>(T α, Storage<T> A, Storage<T> B, T β, Storage<T> C, Storage<T> D, long[] sizeA, long[] sizeB, long[] sizeC, ReadOnlySpan<int> concA, ReadOnlySpan<int> concB, ReadOnlySpan<int> freeA, ReadOnlySpan<int> freeCA, ReadOnlySpan<int> freeB, ReadOnlySpan<int> freeCB) where T : struct, IComparable<T>
		{
			this.common.Contract(α, A, B, β, C, D, sizeA, sizeB, sizeC, concA, concB, freeA, freeCA, freeB, freeCB);
		}

		/// <summary>
		/// Partial reduction of tensor <paramref name="A"/>: $D_{\Pi^C(i_0,i_1,...,i_n)} = \alpha \Phi(\Psi_A(A_{\Pi^A(i_0,i_1,...,i_n)})) + \beta \Psi_C(C_{\Pi^C(i_0,i_1,...,i_n)})$. The indices not in <paramref name="permAToC"/> of <paramref name="A"/> will be aggregated according to <paramref name="reduction"/>.
		/// </summary>
		/// <param name="reduction">the reduce <see cref="BinaryOperation"/> <c>Φ</c></param>
		/// <param name="α">scalar α</param>
		/// <param name="opA"><see cref="UnitaryOperation"/> <c>Ψ<sub>A</sub></c></param>
		/// <param name="A">tensor A</param>
		/// <param name="sizeA">size/extent of <paramref name="A"/></param>
		/// <param name="β">scalar β, default 0</param>
		/// <param name="opC"><see cref="UnitaryOperation"/> <c>Ψ<sub>C</sub></c></param>
		/// <param name="C">tensor C</param>
		/// <param name="sizeC">size/extent of <paramref name="C"/></param>
		/// <param name="D">output tensor D</param>
		/// <param name="permAToC">the permutation order from <paramref name="A"/> to <paramref name="C"/></param>
		public void Reduce<T>(BinaryOperation reduction, T α, UnitaryOperation opA, Storage<T> A, long[] sizeA, T β, UnitaryOperation opC, Storage<T> C, long[] sizeC, Storage<T> D, ReadOnlySpan<int> permAToC) where T : struct, IComparable<T>
		{
			this.common.Reduce(reduction, α, opA, A, sizeA, β, opC, C, sizeC, D, permAToC);
		}
		#endregion
	}
}

namespace TTGT.HpTT
{
	#region natives
	internal static class NativeMethods
	{
		public const string HPTT_DLL_NAME = @"hpTT";

		/// <summary>
		/// Computes the out-of-place tensor transposition of A into B.<br/>
		/// A tensor transposition plan is a data structure that encodes the execution of the tensor transposition.
		/// HPTT supports tensor transpositions of the form:
		/// $B_{\pi(i_0, i_1,...)} = \alpha * A_{ i_0,i_1,...} + \beta * B_{\pi(i_0,i_1,...)}.$
		/// </summary>
		/// <typeparam name="T">the data type</typeparam>
		/// <param name="perm">permutation of size <paramref name="dim"/> representing the permutation of the indices. For instance, perm[] = { 1, 0, 2 } denotes the following transposition: $B_{i1,i0,i2} := A_{ i0,i1,i2 }$.</param>
		/// <param name="dim">dimensionality / rank of the tensors</param>
		/// <param name="alpha">α scaling factor for A</param>
		/// <param name="A">the input tensor A</param>
		/// <param name="sizeA">sizes/extents of each dimension of A</param>
		/// <param name="outerSizeA">The outer-sizes of each dimension of A. This parameter may be NULL, indicating that the outer-size is equal to sizeA. If This is not NULL, <c><paramref name="outerSizeA"/>[i] ≥ <paramref name="sizeA"/>[i], 0 ≤ i &lt; <paramref name="dim"/></c> must hold.</param>
		/// <param name="beta">β scaling factor for B</param>
		/// <param name="B">the output tensor B</param>
		/// <param name="outerSizeB">The outer-sizes of each dimension of B. This parameter may be NULL, indicating that the outer-size is equal to sizeA. If This is not NULL, <c><paramref name="outerSizeB"/>[i] ≥ <paramref name="perm"/>(<paramref name="sizeA"/>)[i], 0 ≤ i &lt; <paramref name="dim"/></c> must hold.</param>
		/// <param name="numThreads">number of threads that participate in this tensor transposition.</param>
		/// <param name="useRowMajor">indicates whether a row-major memory layout should be used (0 = column-major)</param>
		internal delegate void TensorTranspose<T>(in int perm, int dim, T alpha, IntPtr A, in int sizeA, int[] outerSizeA, T beta, IntPtr B, int[] outerSizeB, int numThreads, int useRowMajor);

#pragma warning disable IDE1006 // naming
		[DllImport(HPTT_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void sTensorTranspose(in int perm, int dim, float alpha, IntPtr A, in int sizeA, int[] outerSizeA, float beta, IntPtr B, int[] outerSizeB, int numThreads, int useRowMajor);

		[DllImport(HPTT_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void dTensorTranspose(in int perm, int dim, double alpha, IntPtr A, in int sizeA, int[] outerSizeA, double beta, IntPtr B, int[] outerSizeB, int numThreads, int useRowMajor);

		[DllImport(HPTT_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void cTensorTranspose(in int perm, int dim, FloatComplex alpha, IntPtr A, in int sizeA, int[] outerSizeA, FloatComplex beta, IntPtr B, int[] outerSizeB, int numThreads, int useRowMajor);

		[DllImport(HPTT_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern void zTensorTranspose(in int perm, int dim, DoubleComplex alpha, IntPtr A, in int sizeA, int[] outerSizeA, DoubleComplex beta, IntPtr B, int[] outerSizeB, int numThreads, int useRowMajor);
	}
#pragma warning restore IDE1006 // naming
	#endregion

	/// <summary>
	/// The class that inherits <see cref="CudaCSharp.Tensor.ITensor"/> with underlying library "HpTT"
	/// </summary>
	public sealed class HPTensor : CudaCSharp.Tensor.ITensor
	{
		#region base
		private readonly Common common;

		private readonly int Ncores = Environment.ProcessorCount;

		/// <summary>
		/// default constructor
		/// </summary>
		public HPTensor()
		{
			this.common = new Common(CudaCSharp.Blas.API.CPU, this);
		}

		/// <summary>
		/// default disposition
		/// </summary>
		public void Dispose()
		{
			// do nothing
		}
		#endregion

		#region operations
		/// <summary>
		/// Permute (general transpose) and scale this tensor to form a new tensor: $B_{i_0,i_1,...,i_n} = \alpha \Psi(A_{\Pi(i_0,i_1,...,i_n)})$.
		/// </summary>
		/// <param name="α">the scalar to multiply</param>
		/// <param name="op">the <see cref="UnitaryOperation"/> <c>Ψ</c> to apply on each element before scaling</param>
		/// <param name="A">the source tensor</param>
		/// <param name="sizeA">size/extent of <paramref name="A"/></param>
		/// <param name="B">the output tensor</param>
		/// <param name="sizeB">size/extent of <paramref name="B"/></param>
		/// <param name="permAToB">the permutation order from <paramref name="A"/> to <paramref name="B"/></param>
		public void Permute<T>(Storage<T> A, long[] sizeA, T α, UnitaryOperation op, Storage<T> B, long[] sizeB, ReadOnlySpan<int> permAToB) where T : struct, IComparable<T>
		{
			Span<int> size = stackalloc int[sizeA.Length];
			var (type, trivial) = this.common.PermuteDoTrivial(A, sizeA, α, op, B, size, permAToB);
			if (trivial)
			{	// trivial transpose done in PermuteDoTrivial
				return;
			}

			// non-trivial
			NativeMethods.TensorTranspose<T> func = type switch
			{
				DataType.RealSingle => new NativeMethods.TensorTranspose<float>(NativeMethods.sTensorTranspose) as NativeMethods.TensorTranspose<T>,
				DataType.RealDouble => new NativeMethods.TensorTranspose<double>(NativeMethods.dTensorTranspose) as NativeMethods.TensorTranspose<T>,
				DataType.ComplexSingle => new NativeMethods.TensorTranspose<FloatComplex>(NativeMethods.cTensorTranspose) as NativeMethods.TensorTranspose<T>,
				DataType.ComplexDouble => new NativeMethods.TensorTranspose<DoubleComplex>(NativeMethods.zTensorTranspose) as NativeMethods.TensorTranspose<T>,
				_ => null
			};
			if (func is null && α.IsOne())
			{   // support other data types such as int and long
				func = type.Bytes() switch
				{
					4 => new NativeMethods.TensorTranspose<float>(NativeMethods.sTensorTranspose) as NativeMethods.TensorTranspose<T>,
					8 => new NativeMethods.TensorTranspose<double>(NativeMethods.dTensorTranspose) as NativeMethods.TensorTranspose<T>,
					16 => new NativeMethods.TensorTranspose<DoubleComplex>(NativeMethods.zTensorTranspose) as NativeMethods.TensorTranspose<T>,
					_ => null,
				};
			}
			if (func is null)
				throw new NotSupportedException();
			func(in permAToB.Ref(), permAToB.Length, α, A, in size.Ref(), null, Scalars<T>.Zero, B, null, Ncores, useRowMajor: 0);
		}

		/// <summary>
		/// Contract two tensors <paramref name="A"/> and <paramref name="B"/>: $D_{i_0,i_1,...,i_n} = \alpha \sum_{j_a = k_b}{A_{j_0,j_1,...,j_p} \cdot B_{k_0,k_1,...,k_q}} + \beta C_{i_0,i_1,...,i_n}$;
		/// </summary>
		/// <param name="α">scalar α</param>
		/// <param name="A">tensor A</param>
		/// <param name="β">scalar β</param>
		/// <param name="B">tensor B</param>
		/// <param name="C">tensor C</param>
		/// <param name="D">output tensor D</param>
		/// <param name="sizeA">left tensor's size/extent</param>
		/// <param name="sizeB">right tensor's size/extent</param>
		/// <param name="sizeC">output tensor's size/extent</param>
		/// <param name="concA">sorted left tensor's contract indices</param>
		/// <param name="concB">right tensor's contract indices sorted by <paramref name="concA"/></param>
		/// <param name="freeA">left tensor's free indices sorted by <paramref name="freeCA"/></param>
		/// <param name="freeCA">output tensor's indices corresponding to left tensor's</param>
		/// <param name="freeB">right tensor's free indices sorted by <paramref name="freeCB"/></param>
		/// <param name="freeCB">output tensor's indices corresponding to right tensor's</param>
		public void Contract<T>(T α, Storage<T> A, Storage<T> B, T β, Storage<T> C, Storage<T> D, long[] sizeA, long[] sizeB, long[] sizeC, ReadOnlySpan<int> concA, ReadOnlySpan<int> concB, ReadOnlySpan<int> freeA, ReadOnlySpan<int> freeCA, ReadOnlySpan<int> freeB, ReadOnlySpan<int> freeCB) where T : struct, IComparable<T>
		{
			this.common.Contract(α, A, B, β, C, D, sizeA, sizeB, sizeC, concA, concB, freeA, freeCA, freeB, freeCB);
		}

		/// <summary>
		/// Partial reduction of tensor <paramref name="A"/>: $D_{\Pi^C(i_0,i_1,...,i_n)} = \alpha \Phi(\Psi_A(A_{\Pi^A(i_0,i_1,...,i_n)})) + \beta \Psi_C(C_{\Pi^C(i_0,i_1,...,i_n)})$. The indices not in <paramref name="permAToC"/> of <paramref name="A"/> will be aggregated according to <paramref name="reduction"/>.
		/// </summary>
		/// <param name="reduction">the reduce <see cref="BinaryOperation"/> <c>Φ</c></param>
		/// <param name="α">scalar α</param>
		/// <param name="opA"><see cref="UnitaryOperation"/> <c>Ψ<sub>A</sub></c></param>
		/// <param name="A">tensor A</param>
		/// <param name="sizeA">size/extent of <paramref name="A"/></param>
		/// <param name="β">scalar β, default 0</param>
		/// <param name="opC"><see cref="UnitaryOperation"/> <c>Ψ<sub>C</sub></c></param>
		/// <param name="C">tensor C</param>
		/// <param name="sizeC">size/extent of <paramref name="C"/></param>
		/// <param name="D">output tensor D</param>
		/// <param name="permAToC">the permutation order from <paramref name="A"/> to <paramref name="C"/></param>
		public void Reduce<T>(BinaryOperation reduction, T α, UnitaryOperation opA, Storage<T> A, long[] sizeA, T β, UnitaryOperation opC, Storage<T> C, long[] sizeC, Storage<T> D, ReadOnlySpan<int> permAToC) where T : struct, IComparable<T>
		{
			this.common.Reduce(reduction, α, opA, A, sizeA, β, opC, C, sizeC, D, permAToC);
		}
		#endregion
	}
}