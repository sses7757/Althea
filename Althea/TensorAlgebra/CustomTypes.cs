using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using Althea.Arrays;
using Althea.Linq;
using Althea.Resources;
using Althea.Helpers;
using System.Reflection.Emit;


namespace Althea.TensorAlgebra
{
	#region enum
	/// <summary>
	/// Binary operations used by tensor point-wise binary operations
	/// </summary>
	/// <remarks>All implementations shall support these pre-defined binary operations, but a implementation can add support for more binary operations.</remarks>
	public enum BinaryOperation
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
		/// Maximum of two elements (only for real-typed tensors)
		/// </summary>
		Maximum,
		/// <summary>
		/// Minimum of two elements (only for real-typed tensors)
		/// </summary>
		Mininum
	}

	/// <summary>
	/// Unitary operations of tensor point-wise unary operations
	/// </summary>
	/// <remarks>All implementations shall support these pre-defined unary operations, but a implementation can add support for more unary operations.</remarks>
	public enum UnaryOperation
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
		internal static ReadOnlySpan<int> GetFree(ReadOnlySpan<int> free, ReadOnlySpan<int> conc, Span<int> output)
		{
			int freeN = free.Length, rank = conc.Length + freeN;
			if (freeN == 0)
				return ReadOnlySpan<int>.Empty;
			if (output.Length < freeN)
				throw new ArgumentException(Parameter.WrongSize, nameof(output));
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
		public readonly ReadOnlySpan<int> GetLeftFree(Span<int> output) => GetFree(this.m_leftFreeInOut, this.m_leftConc, output);

		/// <summary>
		/// Get the partial permutation of the right tensor indicating the free (not contracted) dimensions/indices of the right tensor.
		/// </summary>
		/// <param name="output">The <see cref="Span{T}"/> to put the result</param>
		/// <returns>The sliced <paramref name="output"/> of correct length</returns>
		/// <exception cref="ArgumentException">If <paramref name="output"/> is too short</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly ReadOnlySpan<int> GetRightFree(Span<int> output) => GetFree(this.m_rightFreeInOut, this.m_rightConc, output);

		/// <summary>
		/// Check whether this wrapper is an invalid one or not
		/// </summary>
		/// <returns>The invalidness of this wrapper</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly bool IsInvalid() => this.m_leftConc.Length != this.m_rightConc.Length ||
			(this.m_leftConc.IsEmpty && this.m_rightConc.IsEmpty && this.m_leftFreeInOut.IsEmpty && this.m_rightFreeInOut.IsEmpty) ||
			(this.m_leftFreeInOut.IsEmpty && this.m_rightFreeInOut.IsEmpty && this.m_leftConc.Length != this.m_rightConc.Length);

		/// <summary>
		/// Get a possible combination of labels of all three tensors of this <see cref="TensorContractInfo"/>
		/// </summary>
		/// <param name="labelA">The input/output labels of the left tensor</param>
		/// <param name="labelB">The input/output labels of the right tensor</param>
		/// <param name="labelC">The input/output labels of the output tensor</param>
		/// <exception cref="ArgumentException">If the inputs are too short</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly void GetLabels(ref Span<char> labelA, ref Span<char> labelB, ref Span<char> labelC)
		{
			int fa = this.m_leftFreeInOut.Length, fb = this.m_rightFreeInOut.Length;
			int c = this.m_leftConc.Length;
			if (labelC.Length < fa + fb)
				throw new ArgumentException(Parameter.WrongSize, nameof(labelC));
			if (labelA.Length < fa + c)
				throw new ArgumentException(Parameter.WrongSize, nameof(labelA));
			if (labelB.Length < c + fb)
				throw new ArgumentException(Parameter.WrongSize, nameof(labelB));
			labelA = labelA[..(fa + c)];
			labelB = labelB[..(fb + c)];
			labelC = labelC[..(fa + fb)];
			labelC.FillWithLabel();

			Span<int> freeA = stackalloc int[fa];
			Span<int> freeB = stackalloc int[fb];
			this.GetLeftFree(freeA); this.GetRightFree(freeB);
			Span<char> tempLabel = stackalloc char[Math.Max(Math.Max(fa, fb), c)];
			labelC.ReOrderTo(tempLabel, this.m_leftFreeInOut);
			tempLabel.InverseOrderTo(labelA, freeA);
			labelC.ReOrderTo(tempLabel, this.m_rightFreeInOut);
			tempLabel.InverseOrderTo(labelB, freeB);

			labelA.FillZerosWithLabel((char)('a' + fa + fb));
			labelA.ReOrderTo(tempLabel, this.m_leftConc);
			tempLabel.InverseOrderTo(labelB, this.m_rightConc);
		}
		#endregion

		#region override
		/// <summary>
		/// This method always returns false since a ref struct cannot be boxed
		/// </summary>
		public override bool Equals(object? obj) => false;

#pragma warning disable CS0809
		/// <summary>
		/// Not supported. Always throws a <see cref="NotSupportedException"/>.
		/// </summary>
		/// <exception cref="NotSupportedException">Always</exception>
		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("GetHashCode() on " + nameof(TensorContractInfo) + " will always throw an exception.")]
		public override int GetHashCode() => throw new NotSupportedException();
#pragma warning restore CS0809

		/// <summary>
		/// Equality comparer
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(TensorContractInfo left, TensorContractInfo right)
		{
			if (left.IsInvalid() && right.IsInvalid())
				return true;
			if (left.IsInvalid() != right.IsInvalid())
				return false;
			return	left.m_leftConc.SequenceEqual(right.m_leftConc) && left.m_rightConc.SequenceEqual(right.m_rightConc) &&
					left.m_leftFreeInOut.SequenceEqual(right.m_leftFreeInOut) && left.m_rightFreeInOut.SequenceEqual(right.m_rightFreeInOut);
		}

		/// <summary>
		/// Inequality comparer
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(TensorContractInfo left, TensorContractInfo right)
		{
			return !(left == right);
		}

		/// <summary>
		/// Returns the string representation of this <see cref="TensorContractInfo"/>
		/// </summary>
		/// <returns>The string representation of this <see cref="TensorContractInfo"/></returns>
		public override string ToString()
		{
			const string INVALID = "Invalid " + nameof(TensorContractInfo);

			if (this.IsInvalid())
				return INVALID;
			else if (this.m_leftFreeInOut.IsEmpty && this.m_rightFreeInOut.IsEmpty)
			{	// reduction to scalar
				int r = this.m_leftConc.Length;
				Span<char> labelA = stackalloc char[r].FillWithLabel();
				Span<char> labelB = stackalloc char[r];
				Span<int> perm = stackalloc int[r];
				this.m_leftConc.FindPermutationTo(this.m_rightConc, perm);
				labelA.ReOrderTo(labelB, perm);
				return $"Full reduction of tensor A[{labelA.SpanJoin(',')}] and tensor B[{labelB.SpanJoin(',')}] to a scalar.";
			}
			else if (this.m_leftConc.IsEmpty && this.m_rightConc.IsEmpty)
			{   // outer
				int ra = this.m_leftFreeInOut.Length, rb = this.m_rightFreeInOut.Length;
				Span<char> labelA = stackalloc char[ra];
				Span<char> labelB = stackalloc char[rb];
				Span<char> labelC = stackalloc char[ra + rb].FillWithLabel();
				labelC.ReOrderTo(labelA, this.m_leftFreeInOut);
				labelC.ReOrderTo(labelB, this.m_rightFreeInOut);
				if (ra == 0)
					return $"Permute from tensor B[{labelB.SpanJoin(',')}] to tensor C[{labelC.SpanJoin(',')}].";
				else if (rb == 0)
					return $"Permute from tensor A[{labelA.SpanJoin(',')}] to tensor C[{labelC.SpanJoin(',')}].";
				else
					return $"Full outer product of tensor A[{labelA.SpanJoin(',')}] and tensor B[{labelB.SpanJoin(',')}] to tensor C[{labelC.SpanJoin(',')}].";
			}
			else
			{	// normal contraction
				int fa = this.m_leftFreeInOut.Length, fb = this.m_rightFreeInOut.Length;
				int c = this.m_leftConc.Length;
				Span<char> labelC = stackalloc char[fa + fb].FillWithLabel();
				Span<char> labelA = stackalloc char[c + fa];
				Span<char> labelB = stackalloc char[c + fb];
				this.GetLabels(ref labelA, ref labelB, ref labelC);
				return $"Contract tensor A[{labelA.SpanJoin(',')}] and tensor B[{labelB.SpanJoin(',')}] to tensor C[{labelC.SpanJoin(',')}].";
			}
		}
		#endregion

		#region create
		/// <summary>
		/// Create a <see cref="TensorContractInfo"/> with the given direct information
		/// </summary>
		/// <param name="leftConc">See <see cref="LeftContract"/></param>
		/// <param name="rightConc">See <see cref="RightContract"/></param>
		/// <param name="leftFree">See <see cref="LeftFreeInOutput"/></param>
		/// <param name="rightFree">See <see cref="RightFreeInOutput"/></param>
		/// <remarks>This constructor does not perform any check to the given parameters</remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public TensorContractInfo(ReadOnlySpan<int> leftConc, ReadOnlySpan<int> rightConc, ReadOnlySpan<int> leftFree, ReadOnlySpan<int> rightFree)
		{
			this.m_leftConc = leftConc; this.m_rightConc = rightConc;
			this.m_leftFreeInOut = leftFree; this.m_rightFreeInOut = rightFree;
		}

		// Ignore Spelling: stackalloc
		/// <summary>
		/// Create a <see cref="TensorContractInfo"/> with the given <paramref name="left"/>, <paramref name="right"/> and <paramref name="output"/> tensors
		/// </summary>
		/// <param name="left">The input left <see cref="ILabeledTensor"/></param>
		/// <param name="right">The input right <see cref="ILabeledTensor"/></param>
		/// <param name="output">The output <see cref="ILabeledTensor"/></param>
		/// <param name="leftConc">A <see cref="Span{T}"/> of length equaling to contraction rank, to be filled</param>
		/// <param name="rightConc">A <see cref="Span{T}"/> of length equaling to contraction rank, to be filled</param>
		/// <param name="leftFree">A <see cref="Span{T}"/> of length equaling to <paramref name="left"/>'s rank minus contraction rank, to be filled</param>
		/// <param name="rightFree">A <see cref="Span{T}"/> of length equaling to <paramref name="right"/>'s rank minus contraction rank, to be filled</param>
		/// <remarks>The contraction rank can be obtained by <see cref="GetContractRank"/> which assumes that all tensors' labels are valid ones.</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="left"/> or <paramref name="right"/> or <paramref name="output"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="left"/> and <paramref name="right"/> cannot contract and overwrite <paramref name="output"/></exception>
		/// <example><code>
		/// int rank = <see cref="TensorContractInfo"/>.<see cref="GetContractRank(ILabeledTensor, ILabeledTensor)"/>;
		/// <see cref="Span{T}"/> leftConc = stackalloc int[rank];
		/// <see cref="Span{T}"/> rightConc = stackalloc int[rank];
		/// <see cref="Span{T}"/> leftFree = stackalloc int[left.Rank - rank];
		/// <see cref="Span{T}"/> rightFree = stackalloc int[right.Rank - rank];
		/// <see cref="TensorContractInfo"/> info = new(left, right, output, leftConc, rightConc, leftFree, rightFree);
		/// <see cref="Dense.AbstractApi.Contract{T}(Dense.DenseTensorWrapper{T}, Dense.DenseTensorWrapper{T}, Dense.DenseTensorWrapper{T}, TensorContractInfo)"/>;
		/// </code></example>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public TensorContractInfo(ILabeledTensor left, ILabeledTensor right, ILabeledTensor output, Span<int> leftConc, Span<int> rightConc, Span<int> leftFree, Span<int> rightFree)
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
		/// <param name="left">The input left <see cref="ILabeledTensor"/></param>
		/// <param name="right">The input right <see cref="ILabeledTensor"/></param>
		/// <returns>The contraction part's rank</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int GetContractRank(ILabeledTensor left, ILabeledTensor right)
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
						throw new ArgumentException(Parameter.WrongSize, nameof(sizeA));
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
				throw new ArgumentException(Parameter.NotSameSize, nameof(sizeC));
			for (int i = 0; i < rankC; i++)
			{
				int ind = labelC.IndexOf(freeMode[i]);
				if (ind < 0 || sizeC[ind] != freeSize[i])
					throw new ArgumentException(Parameter.WrongSize, nameof(sizeC));
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

		/// <summary>
		/// Create a <see cref="TensorContractInfo"/> from the given out-of-place binary contraction information
		/// </summary>
		/// <param name="sizeA">The input size of tensor A</param>
		/// <param name="labelA">The input label of tensor A</param>
		/// <param name="sizeB">The input size of tensor B</param>
		/// <param name="labelB">The input label of tensor B</param>
		/// <param name="concA">A <see cref="Span{T}"/> of length equaling to contraction rank, to be filled</param>
		/// <param name="concB">A <see cref="Span{T}"/> of length equaling to contraction rank, to be filled</param>
		/// <param name="freeCA">A <see cref="Span{T}"/> of length equaling to left free rank minus contraction rank, to be filled</param>
		/// <param name="freeCB">A <see cref="Span{T}"/> of length equaling to right free rank minus contraction rank, to be filled</param>
		/// <param name="outSize">A <see cref="Span{T}"/> of length equaling to the output rank, to be filled by the output's size</param>
		/// <param name="outLabels">A <see cref="Span{T}"/> of length equaling to the output rank, to be filled by the output's labels</param>
		/// <param name="outputLabels">The desired output tensor's labels, default empty means simple union of <paramref name="labelA"/> and <paramref name="labelB"/></param>
		/// <returns>The <see cref="TensorContractInfo"/> created from the given parameters</returns>
		/// <remarks>This method assumes that all labels are sets</remarks>
		public static TensorContractInfo GetBinaryContractInfo(ReadOnlySpan<long> sizeA, ReadOnlySpan<char> labelA, ReadOnlySpan<long> sizeB, ReadOnlySpan<char> labelB, Span<int> concA, Span<int> concB, Span<int> freeCA, Span<int> freeCB, Span<long> outSize, Span<char> outLabels, ReadOnlySpan<char> outputLabels = default)
		{
			int commonRank = concA.Length, rankA = sizeA.Length, rankB = sizeB.Length, rankC = rankA + rankB - commonRank;
			int freeARank = rankA - commonRank, freeBRank = rankB - commonRank;
			// check sizes
			if (labelA.Length != rankA)
				throw new ArgumentException(Parameter.NotSameSize, nameof(labelA));
			if (labelB.Length != rankB)
				throw new ArgumentException(Parameter.NotSameSize, nameof(labelB));
			if (concB.Length != commonRank)
				throw new ArgumentException(Parameter.NotSameSize, nameof(concB));
			if (freeCA.Length != freeARank)
				throw new ArgumentException(Parameter.NotSameSize, nameof(freeCA));
			if (freeCB.Length != freeARank)
				throw new ArgumentException(Parameter.NotSameSize, nameof(freeCB));
			if (outSize.Length != rankC)
				throw new ArgumentException(Parameter.NotSameSize, nameof(outSize));
			if (outLabels.Length != rankC)
				throw new ArgumentException(Parameter.NotSameSize, nameof(outLabels));
			if (!outputLabels.ElementsUnique())
				throw new ArgumentException(Parameter.DuplicateValue, nameof(outputLabels));
			// check output label
			Span<char> simpleLabelC = stackalloc char[rankA + rankB];
			simpleLabelC = labelA.SetUnion(labelB, simpleLabelC);
			if (!outputLabels.IsEmpty && !simpleLabelC.SetEquals(outputLabels))
				throw new ArgumentException(Parameter.InvalidValue, nameof(outputLabels));
			// check contraction size
			Span<char> commonLabel = stackalloc char[Math.Min(rankA, rankB)];
			commonLabel = labelA.SetIntersect(labelB, commonLabel);
			labelA.SetIntersectIndex(commonLabel, concA); labelB.SetIntersectIndex(commonLabel, concB);
			Span<long> concSizeA = stackalloc long[commonRank], concSizeB = stackalloc long[commonRank];
			sizeA.ReOrderTo(concSizeA, concA); sizeB.ReOrderTo(concSizeB, concB);
			if (!concSizeA.SequenceEqual(concSizeB))
				throw new ArgumentException(Parameter.WrongSize, nameof(sizeB));
			// get output permutation
			ReadOnlySpan<char> outLabel = outputLabels.IsEmpty ? simpleLabelC : outputLabels;
			outLabel.CopyTo(outLabels);
			outLabel.SetIntersectIndex(labelA, freeCA); outLabel.SetIntersectIndex(labelB, freeCB);
			// get output size
			Span<int> freeA = stackalloc int[freeARank], freeB = stackalloc int[freeBRank];
			Span<int> identityPerm = stackalloc int[Math.Max(rankA, rankB)].FillWithRange(0);
			identityPerm[..rankA].SetExept(concA, freeA); identityPerm[..rankB].SetExept(concB, freeB);
			Span<long> freeSizeA = stackalloc long[freeARank], freeSizeB = stackalloc long[freeBRank];
			sizeA.ReOrderTo(freeSizeA, freeA); sizeB.ReOrderTo(concSizeB, freeB);
			freeSizeA.InverseOrderTo(outSize, freeCA); freeSizeB.InverseOrderTo(outSize, freeCB);
			// get contraction info
			return new(concA, concB, freeCA, freeCB);
		}
		#endregion
	}

	/// <summary>
	/// The <see cref="TensorContractInfo"/> that can be stored on stack and heap
	/// </summary>
	public readonly struct StorableContractInfo : ICheckValid, IEquatable<StorableContractInfo>
	{
		#region basic
		private readonly FixedBuffer_64<int> m_leftConc, m_rightConc, m_leftFreeInOut, m_rightFreeInOut;

		private readonly int m_concLen, m_leftFreeLen, m_rightFreeLen;

		/// <summary>
		/// This partial permutation of the left tensor indicate the contract dimensions/indices of the left tensor
		/// </summary>
		public ReadOnlySpan<int> LeftContract {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.m_leftConc.AsSpan(this.m_concLen);
		}

		/// <summary>
		/// The partial permutation of the right tensor indicate the contract dimensions/indices of the right tensor, corresponding to <see cref="LeftContract"/>
		/// </summary>
		public ReadOnlySpan<int> RightContract {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.m_rightConc.AsSpan(this.m_concLen);
		}

		/// <summary>
		/// The partial permutation of the output tensor indicate the free (not contracted) dimensions/indices of the left tensor:<br/>
		/// <c>output.Labels[<see cref="LeftFreeInOutput"/>] == left.Labels[<see cref="GetLeftFree()"/>]</c>
		/// </summary>
		public ReadOnlySpan<int> LeftFreeInOutput {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.m_leftFreeInOut.AsSpan(this.m_leftFreeLen);
		}

		/// <summary>
		/// The partial permutation of the output tensor indicate the free (not contracted) dimensions/indices of the right tensor:<br/>
		/// <c>output.Labels[<see cref="RightFreeInOutput"/>] == right.Labels[<see cref="GetRightFree()"/>]</c>
		/// </summary>
		public ReadOnlySpan<int> RightFreeInOutput {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.m_leftFreeInOut.AsSpan(this.m_rightFreeLen);
		}
		#endregion

		#region methods
		/// <summary>
		/// Get the partial permutation of the left tensor indicating the free (not contracted) dimensions/indices of the left tensor.
		/// </summary>
		/// <param name="output">The <see cref="Span{T}"/> to put the result</param>
		/// <returns>The sliced <paramref name="output"/> of correct length</returns>
		/// <exception cref="ArgumentException">If <paramref name="output"/> is too short</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly ReadOnlySpan<int> GetLeftFree(Span<int> output) => TensorContractInfo.GetFree(this.LeftFreeInOutput, this.LeftContract, output);

		/// <summary>
		/// Get the partial permutation of the right tensor indicating the free (not contracted) dimensions/indices of the right tensor.
		/// </summary>
		/// <param name="output">The <see cref="Span{T}"/> to put the result</param>
		/// <returns>The sliced <paramref name="output"/> of correct length</returns>
		/// <exception cref="ArgumentException">If <paramref name="output"/> is too short</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly ReadOnlySpan<int> GetRightFree(Span<int> output) => TensorContractInfo.GetFree(this.RightFreeInOutput, this.RightContract, output);

		/// <summary>
		/// Get the partial permutation of the left tensor indicating the free (not contracted) dimensions/indices of the left tensor.
		/// </summary>
		/// <returns>The container of the partial permutation of the left tensor indicating the free (not contracted) dimensions/indices of the left tensor.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly FixedBuffer_64<int> GetLeftFree()
		{
			FixedBuffer_64<int> result = default;
			TensorContractInfo.GetFree(this.LeftFreeInOutput, this.LeftContract, result.AsSpan());
			return result;
		}

		/// <summary>
		/// Get the partial permutation of the right tensor indicating the free (not contracted) dimensions/indices of the right tensor.
		/// </summary>
		/// <returns>The container of the partial permutation of the right tensor indicating the free (not contracted) dimensions/indices of the right tensor.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly FixedBuffer_64<int> GetRightFree()
		{
			FixedBuffer_64<int> result = default;
			TensorContractInfo.GetFree(this.RightFreeInOutput, this.RightContract, result.AsSpan());
			return result;
		}

		/// <summary>
		/// Check whether this wrapper is an invalid one or not
		/// </summary>
		/// <returns>The invalidness of this wrapper</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly bool IsValid() => this.m_concLen > 0 || this.m_leftFreeLen > 0 || this.m_rightFreeLen > 0;

		/// <summary>
		/// Get a possible combination of labels of all three tensors of this <see cref="StorableContractInfo"/>
		/// </summary>
		/// <param name="labelA">The input/output labels of the left tensor</param>
		/// <param name="labelB">The input/output labels of the right tensor</param>
		/// <param name="labelC">The input/output labels of the output tensor</param>
		/// <exception cref="ArgumentException">If the inputs are too short</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly void GetLabels(ref Span<char> labelA, ref Span<char> labelB, ref Span<char> labelC)
		{
			int fa = this.m_leftFreeLen, fb = this.m_rightFreeLen;
			int c = this.m_concLen;
			if (labelC.Length < fa + fb)
				throw new ArgumentException(Parameter.WrongSize, nameof(labelC));
			if (labelA.Length < fa + c)
				throw new ArgumentException(Parameter.WrongSize, nameof(labelA));
			if (labelB.Length < c + fb)
				throw new ArgumentException(Parameter.WrongSize, nameof(labelB));
			labelA = labelA[..(fa + c)];
			labelB = labelB[..(fb + c)];
			labelC = labelC[..(fa + fb)];
			labelC.FillWithLabel();

			Span<int> freeA = stackalloc int[fa];
			Span<int> freeB = stackalloc int[fb];
			this.GetLeftFree(freeA); this.GetRightFree(freeB);
			Span<char> tempLabel = stackalloc char[Math.Max(Math.Max(fa, fb), c)];
			labelC.ReOrderTo(tempLabel, this.LeftFreeInOutput);
			tempLabel.InverseOrderTo(labelA, freeA);
			labelC.ReOrderTo(tempLabel, this.RightFreeInOutput);
			tempLabel.InverseOrderTo(labelB, freeB);

			labelA.FillZerosWithLabel((char)('a' + fa + fb));
			labelA.ReOrderTo(tempLabel, this.LeftContract);
			tempLabel.InverseOrderTo(labelB, this.RightContract);
		}

		/// <summary>
		/// Get a possible combination of labels of all three tensors of this <see cref="StorableContractInfo"/>
		/// </summary>
		/// <returns>The containers of the labels of left, right and output tensors, respectively.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly (FixedBuffer_32<char> labelA, FixedBuffer_32<char> labelB, FixedBuffer_32<char> labelC) GetLabels()
		{
			FixedBuffer_32<char> labelA = default, labelB = default, labelC = default;
			Span<char> lA = labelA.AsSpan(), lB = labelB.AsSpan(), lC = labelC.AsSpan();
			this.GetLabels(ref lA, ref lB, ref lC);
			return (labelA, labelB, labelC);
		}
		#endregion

		#region equality
		/// <summary>
		/// Check whether the <paramref name="other"/> <see cref="StorableContractInfo"/> is the same as this one
		/// </summary>
		/// <param name="other">The other <see cref="StorableContractInfo"/> to compare</param>
		/// <returns>this == <paramref name="other"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(StorableContractInfo other)
		{
			if (this.IsValid() != other.IsValid())
				return false;
			if (!this.IsValid() && !other.IsValid())
				return true;
			return	this.m_concLen == other.m_concLen &&
					this.m_leftFreeLen == other.m_leftFreeLen &&
					this.m_rightFreeLen == other.m_rightFreeLen &&
					this.LeftContract.SequenceEqual(other.LeftContract) &&
					this.RightContract.SequenceEqual(other.RightContract) &&
					this.LeftFreeInOutput.SequenceEqual(other.LeftFreeInOutput) &&
					this.RightFreeInOutput.SequenceEqual(other.RightFreeInOutput);
		}

		/// <summary>
		/// Indicates whether this <see cref="StorableContractInfo"/> and a specified object are equal.
		/// </summary>
		/// <param name="obj">The object to compare with the current instance.</param>
		/// <returns>True if obj and this instance are the same type and represent the same value; false otherwise.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override bool Equals(object? obj)
		{
			return obj is StorableContractInfo info && this.Equals(info);
		}

		/// <summary>
		/// Returns the hash code for this <see cref="StorableContractInfo"/>.
		/// </summary>
		/// <returns>the hash code for this <see cref="StorableContractInfo"/>.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode()
		{
			if (!this.IsValid())
				return 0;
			return HashCode.Combine(this.m_concLen, this.m_leftFreeLen, this.m_rightFreeLen, this.LeftContract.HashCodeOfSpan(), this.RightContract.HashCodeOfSpan(), this.LeftFreeInOutput.HashCodeOfSpan(), this.RightFreeInOutput.HashCodeOfSpan());
		}

		/// <summary>
		/// Equality operator
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(StorableContractInfo left, StorableContractInfo right)
		{
			return left.Equals(right);
		}

		/// <summary>
		/// Inequality operator
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(StorableContractInfo left, StorableContractInfo right)
		{
			return !(left == right);
		}
		#endregion

		#region convert
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private StorableContractInfo(ReadOnlySpan<int> concA, ReadOnlySpan<int> concB, ReadOnlySpan<int> freeCA, ReadOnlySpan<int> freeCB)
		{
			this.m_concLen = concA.Length; this.m_leftFreeLen = freeCA.Length; this.m_rightFreeLen = freeCB.Length;
			this.m_leftConc = default; this.m_leftConc.CopyFromSpan(concA);
			this.m_rightConc = default; this.m_rightConc.CopyFromSpan(concB);
			this.m_leftFreeInOut = default; this.m_leftFreeInOut.CopyFromSpan(freeCA);
			this.m_rightFreeInOut = default; this.m_rightFreeInOut.CopyFromSpan(freeCB);
		}

		/// <summary>
		/// Create a <see cref="StorableContractInfo"/> with the given <paramref name="left"/>, <paramref name="right"/> and <paramref name="output"/> tensors
		/// </summary>
		/// <param name="left">The input left <see cref="ILabeledTensor"/></param>
		/// <param name="right">The input right <see cref="ILabeledTensor"/></param>
		/// <param name="output">The output <see cref="ILabeledTensor"/></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public StorableContractInfo(ILabeledTensor left, ILabeledTensor right, ILabeledTensor output)
		{
			this.m_concLen = TensorContractInfo.GetContractRank(left, right);
			this.m_leftFreeLen = left.Rank - this.m_concLen; this.m_rightFreeLen = right.Rank - this.m_concLen;
			this.m_leftConc = this.m_rightConc = this.m_leftFreeInOut = this.m_rightFreeInOut = default;
			_ = new TensorContractInfo(left, right, output, this.m_leftConc.AsSpan(this.m_concLen), this.m_rightConc.AsSpan(this.m_concLen), this.m_leftFreeInOut.AsSpan(this.m_leftFreeLen), this.m_rightFreeInOut.AsSpan(this.m_rightFreeLen));
		}

		/// <summary>
		/// Implicitly convert from a <see cref="TensorContractInfo"/>
		/// </summary>
		/// <param name="info">The given <see cref="TensorContractInfo"/> to be converted</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator StorableContractInfo(TensorContractInfo info)
		{
			return info.IsInvalid() ? default : new(info.LeftContract, info.RightContract, info.LeftFreeInOutput, info.RightFreeInOutput);
		}

		/// <summary>
		/// Implicitly convert to a <see cref="TensorContractInfo"/>
		/// </summary>
		/// <param name="info">The given <see cref="StorableContractInfo"/> to be converted</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator TensorContractInfo(StorableContractInfo info)
		{
			return info.IsValid() ? new TensorContractInfo(info.LeftContract, info.RightContract, info.LeftFreeInOutput, info.RightFreeInOutput) : default;
		}
		#endregion

		#region other
		/// <summary>
		/// Returns the string representation of this <see cref="StorableContractInfo"/>
		/// </summary>
		/// <returns>The string representation of this <see cref="StorableContractInfo"/></returns>
		public override string ToString()
		{
			return ((TensorContractInfo)this).ToString();
		}
		#endregion
	}
	#endregion

	#region extension
	/// <summary>
	/// The static class for check methods other than contract ones which are already implied in <see cref="TensorContractInfo"/> and other extension methods
	/// </summary>
	public static class ContractInfoExtension
	{
		/// <summary>
		/// Check the <paramref name="tensor"/> reduction indicated by the given <paramref name="order"/>
		/// </summary>
		/// <param name="tensor">The <see cref="ILabeledTensor"/> to be reduced</param>
		/// <param name="order">The <see cref="TensorOrder"/> to indicate the reduction dimensions</param>
		/// <param name="reducePerm">The <see cref="Span{T}"/> of length equaling the rank. Filled with the reduce dimensions of actual reduction rank at exit.</param>
		/// <param name="size">The <see cref="Span{T}"/> of length equaling the rank. Filled with the actual output size of actual output rank at exit.</param>
		/// <param name="labels">The <see cref="Span{T}"/> of length equaling the rank. Filled with the actual output labels of actual output rank at exit.</param>
		/// <returns>The <paramref name="reducePerm"/> of actual reduction rank</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Span<int> CheckReduce(this ILabeledTensor tensor, TensorOrder order, Span<int> reducePerm, ref Span<long> size, ref Span<char> labels)
		{
			if (tensor is null)
				throw new ArgumentNullException(nameof(tensor));
			int rank = tensor.Rank;
			if (size.Length != rank)
				throw new ArgumentException(Parameter.WrongSize, nameof(size));
			if (labels.Length != rank)
				throw new ArgumentException(Parameter.WrongSize, nameof(labels));
			if (reducePerm.Length != rank)
				throw new ArgumentException(Parameter.WrongSize, nameof(reducePerm));
			// get reduce permutation
			reducePerm = order.GetIntSpanOrder(tensor, reducePerm, allowPartial: true);
			// get output permutation
			int outRank = rank - reducePerm.Length;
			Span<int> outPerm = stackalloc int[outRank];
			Span<int> identityPerm = stackalloc int[rank].FillWithRange(0);
			identityPerm.SetExept(reducePerm, outPerm);
			// get output members
			if (outRank == 0)
			{
				size = size.SetValue(1)[..1]; labels = default;
			}
			else
			{
				tensor.Size.ReOrderTo(size, outPerm);
				tensor.Labels.ReOrderTo(labels, outPerm);
				size = size[..outRank]; labels = labels[..outRank];
			}
			return reducePerm;
		}

		private static readonly char[] EnglishLetters = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(System.Linq.Enumerable.Range('a', 26), static l => (char)l));

		private static readonly char[] GreekLetters = new[] { 'α', 'β', 'γ', 'δ', 'ε', 'ζ', 'η', 'θ', 'ι', 'κ', 'λ', 'μ', 'ν', 'ξ', 'ο', 'π', 'ρ', 'σ', 'τ', 'υ', 'φ', 'χ', 'ψ', 'ω' };

		/// <summary>
		/// Fill the given <see cref="Span{T}"/> of <see cref="char"/>s with the English alphabet followed by the Greek alphabet
		/// </summary>
		/// <param name="span">The <paramref name="span"/> to be filled with letters</param>
		/// <returns>The filled <paramref name="span"/></returns>
		/// <exception cref="ArgumentException">If <paramref name="span"/> is too large</exception>
		public static Span<char> FillWithLabel(this Span<char> span)
		{
			if (span.IsEmpty)
				return span;
			if (span.Length > EnglishLetters.Length + GreekLetters.Length)
				throw new ArgumentException(Parameter.WrongSize, nameof(span));

			new ReadOnlySpan<char>(EnglishLetters, 0, Math.Min(EnglishLetters.Length, span.Length)).CopyTo(span);
			if (span.Length <= EnglishLetters.Length)
				return span;
			new ReadOnlySpan<char>(GreekLetters, 0, span.Length - EnglishLetters.Length).CopyTo(span[EnglishLetters.Length..]);
			return span;
		}

		/// <summary>
		/// Fill the given <see cref="Span{T}"/> of <see cref="char"/>s with the English alphabet followed by the Greek alphabet started by <paramref name="from"/>
		/// </summary>
		/// <param name="span">The <paramref name="span"/> to be filled with letters</param>
		/// <param name="from">The starting letter</param>
		/// <returns>The filled <paramref name="span"/></returns>
		/// <exception cref="ArgumentException">If <paramref name="span"/> is too large</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="from"/> is neither a English letter nor a Greek letter</exception>
		public static Span<char> FillZerosWithLabel(this Span<char> span, char from)
		{
			if (span.IsEmpty)
				return span;
			if (!EnglishLetters.Contains(from) || !GreekLetters.Contains(from))
				throw new ArgumentOutOfRangeException(nameof(from), from, Parameter.InvalidValue);

			int n = span.Length;
			int c = -1;
			for (int i = 0; i < n; i++)
			{
				if (span[i] != 0)
					continue;
				span[i] = from;
				if (from == 'z')
				{
					c++;
					if (c >= GreekLetters.Length)
						throw new ArgumentException(Parameter.WrongSize, nameof(span));
					from = GreekLetters[c];
				}
				else
				{
					from++;
				}
			}
			return span;
		}
	}
	#endregion
}
