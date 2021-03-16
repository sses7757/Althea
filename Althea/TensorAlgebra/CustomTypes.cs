using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Althea.Arrays;
using Althea.Linq;


namespace Althea.TensorAlgebra
{
	#region enum
	/// <summary>
	/// Binary operations used by tensor point-wise binary operations
	/// </summary>
	public enum BinaryOperation : int
	{
		/// <summary>
		/// Addition of two elements
		/// </summary>
		Addition,
		/// <summary>
		/// Multiplication of two elements
		/// </summary>
		Multiply,
		/// <summary>
		/// Maximum of two elements (only for complex-typed tensors)
		/// </summary>
		Maximum,
		/// <summary>
		/// Minimum of two elements (only for complex-typed tensors)
		/// </summary>
		Mininum
	}

	/// <summary>
	/// Unitary operations of tensor point-wise unary operations
	/// </summary>
	public enum UnaryOperation : int
	{
		/// <summary>
		/// Identity operator (i.e., elements are not changed)
		/// </summary>
		Identity,
		/// <summary>
		/// Complex conjugate (real-typed elements are not changed)
		/// </summary>
		Conjugate,
		/// <summary>
		/// Negation
		/// </summary>
		Negate
	}
	#endregion

	#region wrapper
	/// <summary>
	/// The wrapper for the contraction information of two tensors
	/// </summary>
	public readonly ref struct TensorContractInfo
	{
		#region properties
		private readonly ReadOnlySpan<int> m_leftConc, m_rightConc, m_leftFreeInOut, m_rightFreeInOut;

		/// <summary>
		/// This partial permutation of the left tensor indicate the contract dimensions/indices of the left tensor
		/// </summary>
		public ReadOnlySpan<int> LeftContract {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.m_leftConc;
		}

		/// <summary>
		/// The partial permutation of the right tensor indicate the contract dimensions/indices of the right tensor, corresponding to <see cref="LeftContract"/>
		/// </summary>
		public ReadOnlySpan<int> RightContract {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.m_rightConc;
		}

		/// <summary>
		/// The partial permutation of the output tensor indicate the free (not contracted) dimensions/indices of the left tensor:<br/>
		/// <c>output.Labels[<see cref="LeftFreeInOutput"/>] == left.Labels[<see cref="GetLeftFree"/>]</c>
		/// </summary>
		public ReadOnlySpan<int> LeftFreeInOutput {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.m_leftFreeInOut;
		}

		/// <summary>
		/// The partial permutation of the output tensor indicate the free (not contracted) dimensions/indices of the right tensor:<br/>
		/// <c>output.Labels[<see cref="RightFreeInOutput"/>] == right.Labels[<see cref="GetRightFree"/>]</c>
		/// </summary>
		public ReadOnlySpan<int> RightFreeInOutput {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.m_leftFreeInOut;
		}
		#endregion

		#region methods
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ReadOnlySpan<int> GetFree(ReadOnlySpan<int> free, ReadOnlySpan<int> conc, Span<int> output)
		{
			int freeN = free.Length, rank = conc.Length + freeN;
			if (freeN == 0)
				return ReadOnlySpan<int>.Empty;
			if (output.Length < freeN)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(output));
			int n = 0;
			for (int i = 0; i < rank; i++)
			{
				if (!conc.Contains(i))
					output[n++] = i;
			}
			return output[..n];
		}

		/// <summary>
		/// Get the partial permutation of the left tensor indicating the free (not contracted) dimensions/indices of the left tensor.
		/// </summary>
		/// <param name="output">The <see cref="Span{T}"/> to put the result</param>
		/// <returns>The sliced <paramref name="output"/> of correct length</returns>
		/// <exception cref="ArgumentException">If <paramref name="output"/> is too short</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ReadOnlySpan<int> GetLeftFree(Span<int> output) => GetFree(this.m_leftFreeInOut, this.m_leftConc, output);

		/// <summary>
		/// Get the partial permutation of the right tensor indicating the free (not contracted) dimensions/indices of the right tensor.
		/// </summary>
		/// <param name="output">The <see cref="Span{T}"/> to put the result</param>
		/// <returns>The sliced <paramref name="output"/> of correct length</returns>
		/// <exception cref="ArgumentException">If <paramref name="output"/> is too short</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ReadOnlySpan<int> GetRightFree(Span<int> output) => GetFree(this.m_rightFreeInOut, this.m_rightConc, output);

		/// <summary>
		/// Check whether this wrapper is an invalid one or not
		/// </summary>
		/// <returns>The invalidness of this wrapper</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool IsInvalid() => this.m_leftConc.IsEmpty || this.m_rightConc.IsEmpty || this.m_leftFreeInOut.IsEmpty || this.m_rightFreeInOut.IsEmpty;
		#endregion

		#region create
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private TensorContractInfo(ReadOnlySpan<int> leftConc, ReadOnlySpan<int> rightConc, ReadOnlySpan<int> leftFree, ReadOnlySpan<int> rightFree)
		{
			this.m_leftConc = leftConc; this.m_rightConc = rightConc;
			this.m_leftFreeInOut = leftFree; this.m_rightFreeInOut = rightFree;
		}

		// Ignore Spelling: stackalloc
		/// <summary>
		/// Create a <see cref="TensorContractInfo"/> with the given <paramref name="left"/>, <paramref name="right"/> and <paramref name="output"/> tensors
		/// </summary>
		/// <param name="left">The input left <see cref="ITensor"/></param>
		/// <param name="right">The input right <see cref="ITensor"/></param>
		/// <param name="output">The output <see cref="ITensor"/></param>
		/// <param name="leftConc">A <see cref="Span{T}"/> of length equaling to contraction rank, to be filled</param>
		/// <param name="rightConc">A <see cref="Span{T}"/> of length equaling to contraction rank, to be filled</param>
		/// <param name="leftFree">A <see cref="Span{T}"/> of length equaling to <paramref name="left"/>'s rank minus contraction rank, to be filled</param>
		/// <param name="rightFree">A <see cref="Span{T}"/> of length equaling to <paramref name="right"/>'s rank minus contraction rank, to be filled</param>
		/// <remarks>The contraction rank can be obtained by <see cref="GetContractRank"/> which assumes that all tensors' labels are valid ones.</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="left"/> or <paramref name="right"/> or <paramref name="output"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="left"/> and <paramref name="right"/> cannot contract and overwrite <paramref name="output"/></exception>
		/// <example><code>
		/// int rank = <see cref="TensorContractInfo"/>.<see cref="GetContractRank(ITensor, ITensor)"/>;
		/// <see cref="Span{T}"/> leftConc = stackalloc int[rank];
		/// <see cref="Span{T}"/> rightConc = stackalloc int[rank];
		/// <see cref="Span{T}"/> leftFree = stackalloc int[left.Rank - rank];
		/// <see cref="Span{T}"/> rightFree = stackalloc int[right.Rank - rank];
		/// <see cref="TensorContractInfo"/> info = new(left, right, output, leftConc, rightConc, leftFree, rightFree);
		/// <see cref="Dense.AbstractApi.Contract{T}(Dense.DenseTensorWrapper{T}, Dense.DenseTensorWrapper{T}, Dense.DenseTensorWrapper{T}, TensorContractInfo)"/>;
		/// </code></example>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public TensorContractInfo(ITensor left, ITensor right, ITensor output, Span<int> leftConc, Span<int> rightConc, Span<int> leftFree, Span<int> rightFree)
		{
			if (left is null || !left.IsValid())
				throw new ArgumentNullException(nameof(left));
			if (right is null || !right.IsValid())
				throw new ArgumentNullException(nameof(right));
			if (output is null || !output.IsValid())
				throw new ArgumentNullException(nameof(output));

			// create spans
			ContractCheck(left.Size, left.Labels, right.Size, right.Labels, output.Size, output.Labels, leftConc, rightConc, leftFree, rightFree);
			// create
			this = new(leftConc, rightConc, leftFree, rightFree);
		}
		#endregion

		#region checks
		/// <summary>
		/// Check the labels of the given <paramref name="left"/> and <paramref name="right"/> tensors and get the contraction part's rank
		/// </summary>
		/// <param name="left">The input left <see cref="ITensor"/></param>
		/// <param name="right">The input right <see cref="ITensor"/></param>
		/// <returns>The contraction part's rank</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int GetContractRank(ITensor left, ITensor right)
		{
			ReadOnlySpan<char> labelA = left.Labels, labelB = right.Labels;
			int commonRank = 0, rankA = left.Rank;
			for (int i = 0; i < rankA; i++)
			{
				if (labelB.Contains(labelA[i]))
					commonRank++;
			}
			return commonRank;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void ContractCheck(ReadOnlySpan<long> sizeA, ReadOnlySpan<char> labelA, ReadOnlySpan<long> sizeB, ReadOnlySpan<char> labelB, ReadOnlySpan<long> sizeC, ReadOnlySpan<char> labelC, Span<int> concA, Span<int> concB, Span<int> freeCA, Span<int> freeCB)
		{
			// get common mode and size & left and right contraction indices
			int commonRank = concA.Length, rankA = sizeA.Length, rankB = sizeB.Length, rankC = sizeC.Length;
			int freeARank = rankA - commonRank, freeBRank = rankB - commonRank;
			Span<char> commonMode = stackalloc char[commonRank];
			Span<long> commonSize = stackalloc long[commonRank];
			int now = 0;
			for (int i = 0; i < rankA; i++)
			{
				var ind = labelB.IndexOf(labelA[i]);
				if (ind >= 0)
				{
					if (sizeB[ind] != sizeA[i])
						throw new ArgumentException(Resources.Parameter.WrongSize, nameof(sizeA));
					commonMode[now] = labelA[i]; commonSize[now] = sizeA[i];
					concA[now] = i; concB[now++] = ind;
				}
			}
			// get free mode and size
			Span<char> freeMode = stackalloc char[rankC];
			Span<long> freeSize = stackalloc long[rankC];
			now = 0;
			for (int i = 0; i < rankA; i++)
			{
				if (!commonMode.Contains(labelA[i]))
				{
					freeMode[now] = labelA[i]; freeSize[now++] = sizeA[i];
				}
			}
			for (int i = 0; i < rankB; i++)
			{
				if (!commonMode.Contains(labelB[i]))
				{
					freeMode[now] = labelB[i]; freeSize[now++] = sizeB[i];
				}
			}
			// check free mode and size with C
			if (now != rankC)
				throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(sizeC));
			for (int i = 0; i < rankC; i++)
			{
				int ind = labelC.IndexOf(freeMode[i]);
				if (ind < 0 || sizeC[ind] != freeSize[i])
					throw new ArgumentException(Resources.Parameter.WrongSize, nameof(sizeC));
			}
			// get left and right free indices
			int nowA = 0, nowB = 0;
			for (int i = 0; i < freeARank; i++)
			{
				var ind = labelC.IndexOf(freeMode[i]);
				freeCA[nowA++] = ind;
			}
			for (int i = 0, j = freeARank; i < freeBRank; i++, j++)
			{
				var ind = labelC.IndexOf(freeMode[j]);
				freeCB[nowB++] = ind;
			}
		}
		#endregion
	}
	#endregion
}
