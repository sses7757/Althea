using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Althea.Linq;


namespace Althea.Tensor
{
	/// <summary>
	/// The contraction input struct
	/// </summary>
	public readonly struct ContractionInput : IEquatable<ContractionInput>
	{
		#region properties
		/// <summary>
		/// The left tensor's size / extent
		/// </summary>
		public readonly long[] LeftSize;

		/// <summary>
		/// The right tensor's size / extent
		/// </summary>
		public readonly long[] RightSize;

		/// <summary>
		/// The output tensor's size / extent, not used in <see cref="IEquatable{T}"/>
		/// </summary>
		public readonly long[] OutSize;

		/// <summary>
		/// The contract left indices, sorted by left indices
		/// </summary>
		public readonly int[] LeftContractIndex;
		/// <summary>
		/// The contract right indices, sorted by left indices
		/// </summary>
		public readonly int[] RightContractIndex;

		/// <summary>
		/// The left tensor's free index, sorted by output indices
		/// </summary>
		public readonly int[] LeftFreeIndex;
		/// <summary>
		/// The output tensor's, sorted by output indices
		/// </summary>
		public readonly int[] OutLeftFreeIndex;

		/// <summary>
		/// The right tensor's free index, sorted by output indices
		/// </summary>
		public readonly int[] RightFreeIndex;
		/// <summary>
		/// The output tensor's, sorted by output indices
		/// </summary>
		public readonly int[] OutRightFreeIndex;
		#endregion

		#region create
		/// <summary>
		/// Construct from size and permutations
		/// </summary>
		/// <param name="sizeA">left tensor's size/extent</param>
		/// <param name="sizeB">right tensor's size/extent</param>
		/// <param name="sizeC">output tensor's size/extent</param>
		/// <param name="leftContract">sorted left tensor's contract indices</param>
		/// <param name="rightContract">right tensor's contract indices sorted by <paramref name="leftContract"/></param>
		/// <param name="leftFree">left tensor's free indices sorted by <paramref name="outLeftFree"/></param>
		/// <param name="outLeftFree">output tensor's indices corresponding to left tensor's</param>
		/// <param name="rightFree">right tensor's free indices sorted by <paramref name="outRightFree"/></param>
		/// <param name="outRightFree">output tensor's indices corresponding to right tensor's</param>
		public ContractionInput(long[] sizeA, long[] sizeB, long[] sizeC, int[] leftContract, int[] rightContract, int[] leftFree, int[] outLeftFree, int[] rightFree, int[] outRightFree)
		{
			this.LeftSize = sizeA; this.RightSize = sizeB; this.OutSize = sizeC;
			this.LeftContractIndex = leftContract; this.RightContractIndex = rightContract;
			this.LeftFreeIndex = leftFree; this.OutLeftFreeIndex = outLeftFree;
			this.RightFreeIndex = rightFree; this.OutRightFreeIndex = outRightFree;
			this._hashcode = HashCode.Combine(sizeA.HashCodeOfArray(), sizeB.HashCodeOfArray(), leftContract.HashCodeOfArray(), rightContract.HashCodeOfArray(), leftFree.HashCodeOfArray(), outLeftFree.HashCodeOfArray(), rightFree.HashCodeOfArray(), outRightFree.HashCodeOfArray());
		}
		#endregion

		#region equality
		/// <summary>
		/// Indicates whether this <see cref="ContractionInput"/> and a specified object are equal.
		/// </summary>
		/// <param name="obj">The object to compare with the current instance.</param>
		/// <returns>true if obj and this instance are the same type and represent the same value</returns>
		public override bool Equals(object obj)
		{
			return obj is ContractionInput c && this.Equals(c);
		}

		/// <summary>
		/// Indicates whether this <see cref="ContractionInput"/> and a specified object are equal.
		/// </summary>
		/// <param name="obj">The object to compare with the current instance.</param>
		/// <returns>true if obj and this instance are the same type and represent the same value</returns>
		public bool Equals(ContractionInput obj)
		{
			return this._hashcode == obj._hashcode &&
					this.LeftSize.SequenceEqual(obj.LeftSize) && this.RightSize.SequenceEqual(obj.RightSize) &&
					this.LeftContractIndex.SequenceEqual(obj.LeftContractIndex) && this.RightContractIndex.SequenceEqual(obj.RightContractIndex) &&
					this.LeftFreeIndex.SequenceEqual(obj.LeftFreeIndex) && this.OutLeftFreeIndex.SequenceEqual(obj.OutLeftFreeIndex) &&
					this.RightFreeIndex.SequenceEqual(obj.RightFreeIndex) && this.OutRightFreeIndex.SequenceEqual(obj.OutRightFreeIndex);
		}

		/// <summary>
		/// Get the hash code of plain members.
		/// </summary>
		public static int HashCodeOf(long[] leftSize, long[] rightSize, ReadOnlySpan<int> leftContract, ReadOnlySpan<int> rightContract, ReadOnlySpan<int> leftFree, ReadOnlySpan<int> outLeftFree, ReadOnlySpan<int> rightFree, ReadOnlySpan<int> outRightFree)
		{
			return HashCode.Combine(leftSize.HashCodeOfArray(), rightSize.HashCodeOfArray(), leftContract.HashCodeOfSpan(), rightContract.HashCodeOfSpan(), leftFree.HashCodeOfSpan(), outLeftFree.HashCodeOfSpan(), rightFree.HashCodeOfSpan(), outRightFree.HashCodeOfSpan());
		}

		/// <summary>
		/// Indicates whether this <see cref="ContractionInput"/> and plain members are equal.
		/// </summary>
		public bool Equals(long[] leftSize, long[] rightSize, ReadOnlySpan<int> leftContract, ReadOnlySpan<int> rightContract, ReadOnlySpan<int> leftFree, ReadOnlySpan<int> outLeftFree, ReadOnlySpan<int> rightFree, ReadOnlySpan<int> outRightFree)
		{
			return leftSize.SequenceEqual(this.LeftSize) && rightSize.SequenceEqual(this.RightSize) &&
					leftContract.SequenceEqual(this.LeftContractIndex) && rightContract.SequenceEqual(this.RightContractIndex) &&
					leftFree.SequenceEqual(this.LeftFreeIndex) && outLeftFree.SequenceEqual(this.OutLeftFreeIndex) &&
					rightFree.SequenceEqual(this.RightFreeIndex) && outRightFree.SequenceEqual(this.OutRightFreeIndex);
		}

		private readonly int _hashcode;

		/// <summary>
		/// Returns the hash code for this <see cref="ContractionInput"/>.
		/// </summary>
		/// <returns>A <see cref="int"/> that is the hash code for this <see cref="ContractionInput"/>.</returns>
		public override int GetHashCode()
		{
			return this._hashcode;
		}

		/// <summary>
		/// Equality operator
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator ==(ContractionInput left, ContractionInput right)
		{
			return left.Equals(right);
		}

		/// <summary>
		/// Not-equality operator
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator !=(ContractionInput left, ContractionInput right)
		{
			return !(left == right);
		}
		#endregion
	}

	internal static class CacheCommon
	{
		internal static void Clear<TIn, TAddIn, TOut>(List<int> hashCodes, List<TIn> inputs, List<TAddIn> additions, List<TOut> outputs)
			where TIn : struct
			where TAddIn : struct
			where TOut : struct, IDisposable
		{
			hashCodes.Clear(); inputs.Clear(); additions?.Clear();
			foreach (var item in outputs)
			{
				item.Dispose();
			}
			outputs.Clear();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static (int find, int start, int end) FindStartEndStart(List<int> hashCodes, int hashCode)
		{
			int find = hashCodes.BinarySearch(hashCode);
			if (find >= 0)
			{
				int start = find, end = find + 1;
				for (int i = find; i < hashCodes.Count && hashCodes[i] == hashCode; i++)
				{
					end = i + 1;
				}
				for (int i = find; i >= 0 && hashCodes[i] == hashCode; i--)
				{
					start = i;
				}
				return (find, start, end);
			}
			else
			{
				return (find, 0, 0);
			}
		}

		internal static bool Add<TIn, TAddIn, TOut>(List<int> hashCodes, List<TIn> inputs, List<TAddIn> additions, List<TOut> outputs, TIn input, TAddIn addition, TOut plan)
			where TIn : struct
			where TAddIn : struct
			where TOut : struct, IDisposable
		{
			int hashCode = input.GetHashCode();
			if (!(additions is null))
				hashCode = HashCode.Combine(hashCode, addition.GetHashCode());
			var (find, start, end) = FindStartEndStart(hashCodes, hashCode);
			if (find >= 0)
			{
				for (int i = start; i < end; i++)
				{
					if (inputs[i].Equals(input))
						return false;
				}
				find = end;
			}
			else
			{
				find = ~find;
			}
			if (find < hashCodes.Count)
			{
				hashCodes.Insert(find, hashCode);
				inputs.Insert(find, input);
				additions?.Insert(find, addition);
				outputs.Insert(find, plan);
			}
			else
			{
				hashCodes.Add(hashCode);
				inputs.Add(input);
				additions?.Add(addition);
				outputs.Add(plan);
			}
			return true;
		}
	}

	/// <summary>
	/// The static class that provides caching <see cref="ContractionInput"/> with corresponding contraction plan <typeparamref name="TPlan"/>.
	/// </summary>
	/// <typeparam name="TPlan">the type of contraction plan</typeparam>
	public static class ContractionCache<TPlan> where TPlan : struct, IDisposable
	{
		private static readonly List<int> HashCodes = new List<int>();

		private static readonly List<ContractionInput> Inputs = new List<ContractionInput>();

		private static readonly List<TPlan> Outputs = new List<TPlan>();

		/// <summary>
		/// Clear all cache of current type <typeparamref name="TPlan"/>.
		/// </summary>
		public static void Clear()
		{
			CacheCommon.Clear(HashCodes, Inputs, (List<int>)null, Outputs);
		}

		/// <summary>
		/// Try to get the contraction plan as <typeparamref name="TPlan"/> from given input parameters.
		/// </summary>
		/// <param name="leftSize">left tensor's size/extent</param>
		/// <param name="rightSize">right tensor's size/extent</param>
		/// <param name="outSize">output tensor's size/extent</param>
		/// <param name="leftContract">sorted left tensor's contract indices</param>
		/// <param name="rightContract">right tensor's contract indices sorted by <paramref name="leftContract"/></param>
		/// <param name="leftFree">left tensor's free indices sorted by <paramref name="outLeftFree"/></param>
		/// <param name="outLeftFree">output tensor's indices corresponding to left tensor's</param>
		/// <param name="rightFree">right tensor's free indices sorted by <paramref name="outRightFree"/></param>
		/// <param name="outRightFree">output tensor's indices corresponding to right tensor's</param>
		/// <param name="plan">the output contraction plan as <typeparamref name="TPlan"/> if the given input parameters are cached; null otherwise</param>
		/// <param name="input">the created <see cref="ContractionInput"/> from given input parameters if they are <b>not</b> cached; or the found one</param>
		public static void TryGet(long[] leftSize, long[] rightSize, long[] outSize, ReadOnlySpan<int> leftContract, ReadOnlySpan<int> rightContract, ReadOnlySpan<int> leftFree, ReadOnlySpan<int> outLeftFree, ReadOnlySpan<int> rightFree, ReadOnlySpan<int> outRightFree, out TPlan? plan, out ContractionInput input)
		{
			int hashCode = ContractionInput.HashCodeOf(leftSize, rightSize, leftContract, rightContract, leftFree, outLeftFree, rightFree, outRightFree);
			var (find, start, end) = CacheCommon.FindStartEndStart(HashCodes, hashCode);
			if (find >= 0)
			{
				for (int i = start; i < end; i++)
				{
					if (Inputs[i].Equals(leftSize, rightSize, leftContract, rightContract, leftFree, outLeftFree, rightFree, outRightFree))
					{
						find = i;
						break;
					}
				}
				plan = Outputs[find];
				input = Inputs[find];
			}
			else
			{
				plan = null;
				input = new ContractionInput(leftSize, rightSize, outSize, leftContract.ToArray(), rightContract.ToArray(), leftFree.ToArray(), outLeftFree.ToArray(), rightFree.ToArray(), outRightFree.ToArray());
			}
		}

		/// <summary>
		/// Add a pair of <see cref="ContractionInput"/> <paramref name="input"/> and corresponding <typeparamref name="TPlan"/> <paramref name="plan"/>
		/// </summary>
		/// <param name="input">the input information as a <see cref="ContractionInput"/></param>
		/// <param name="plan">the contraction plan as a <typeparamref name="TPlan"/></param>
		/// <returns>true if <paramref name="input"/> is not in current cache, false otherwise</returns>
		public static bool Add(ContractionInput input, TPlan plan)
		{
			return CacheCommon.Add(HashCodes, Inputs, (List<int>)null, Outputs, input, default, plan);
		}
	}

	/// <summary>
	/// The static class that provides caching <see cref="ContractionInput"/> and <typeparamref name="TAdditionInput"/> with corresponding contraction plan <typeparamref name="TPlan"/>.
	/// </summary>
	/// <typeparam name="TAdditionInput">the type of additional information used to identify a contraction input (along with <see cref="ContractionInput"/>)</typeparam>
	/// <typeparam name="TPlan">the type of contraction plan</typeparam>
	public static class ContractionCache<TAdditionInput, TPlan>
		where TAdditionInput : struct, IEquatable<TAdditionInput>
		where TPlan : struct, IDisposable
	{
		private static readonly List<int> HashCodes = new List<int>();

		private static readonly List<ContractionInput> Inputs = new List<ContractionInput>();

		private static readonly List<TAdditionInput> Additions = new List<TAdditionInput>();

		private static readonly List<TPlan> Outputs = new List<TPlan>();

		/// <summary>
		/// Clear all cache of current type <typeparamref name="TPlan"/>.
		/// </summary>
		public static void Clear()
		{
			CacheCommon.Clear(HashCodes, Inputs, Additions, Outputs);
		}

		/// <summary>
		/// Try to get the contraction plan as <typeparamref name="TPlan"/> from given input parameters.
		/// </summary>
		/// <param name="leftSize">left tensor's size/extent</param>
		/// <param name="rightSize">right tensor's size/extent</param>
		/// <param name="outSize">output tensor's size/extent</param>
		/// <param name="leftContract">sorted left tensor's contract indices</param>
		/// <param name="rightContract">right tensor's contract indices sorted by <paramref name="leftContract"/></param>
		/// <param name="leftFree">left tensor's free indices sorted by <paramref name="outLeftFree"/></param>
		/// <param name="outLeftFree">output tensor's indices corresponding to left tensor's</param>
		/// <param name="rightFree">right tensor's free indices sorted by <paramref name="outRightFree"/></param>
		/// <param name="outRightFree">output tensor's indices corresponding to right tensor's</param>
		/// <param name="addition">the additional information as a <typeparamref name="TAdditionInput"/></param>
		/// <param name="plan">the output contraction plan as <typeparamref name="TPlan"/> if the given input parameters are cached; null otherwise</param>
		/// <param name="input">the created <see cref="ContractionInput"/> from given input parameters if they are <b>not</b> cached; or the found one</param>
		public static void TryGet(long[] leftSize, long[] rightSize, long[] outSize, ReadOnlySpan<int> leftContract, ReadOnlySpan<int> rightContract, ReadOnlySpan<int> leftFree, ReadOnlySpan<int> outLeftFree, ReadOnlySpan<int> rightFree, ReadOnlySpan<int> outRightFree, TAdditionInput addition, out TPlan? plan, out ContractionInput input)
		{
			int hashCode = ContractionInput.HashCodeOf(leftSize, rightSize, leftContract, rightContract, leftFree, outLeftFree, rightFree, outRightFree);
			hashCode = HashCode.Combine(hashCode, addition.GetHashCode());
			var (find, start, end) = CacheCommon.FindStartEndStart(HashCodes, hashCode);
			if (find >= 0)
			{
				for (int i = start; i < end; i++)
				{
					if (Inputs[i].Equals(leftSize, rightSize, leftContract, rightContract, leftFree, outLeftFree, rightFree, outRightFree))
					{
						find = i;
						break;
					}
				}
				plan = Outputs[find];
				input = Inputs[find];
			}
			else
			{
				plan = null;
				input = new ContractionInput(leftSize, rightSize, outSize, leftContract.ToArray(), rightContract.ToArray(), leftFree.ToArray(), outLeftFree.ToArray(), rightFree.ToArray(), outRightFree.ToArray());
			}
		}

		/// <summary>
		/// Add a pair of <see cref="ContractionInput"/> <paramref name="input"/> and corresponding <typeparamref name="TPlan"/> <paramref name="plan"/>
		/// </summary>
		/// <param name="input">the input information as a <see cref="ContractionInput"/></param>
		/// <param name="addition">the additional information as a <typeparamref name="TAdditionInput"/></param>
		/// <param name="plan">the contraction plan as a <typeparamref name="TPlan"/></param>
		/// <returns>true if <paramref name="input"/> is not in current cache, false otherwise</returns>
		public static bool Add(ContractionInput input, TAdditionInput addition, TPlan plan)
		{
			return CacheCommon.Add(HashCodes, Inputs, Additions, Outputs, input, addition, plan);
		}
	}


	/// <summary>
	/// The permutation input struct
	/// </summary>
	public readonly struct PermuteInput : IEquatable<PermuteInput>
	{
		#region create
		/// <summary>
		/// The permutation order
		/// </summary>
		public readonly int[] Perm;
		/// <summary>
		/// The original tensor's size / extent
		/// </summary>
		public readonly int[] Size;

		/// <summary>
		/// Construct from permutation and size
		/// </summary>
		/// <param name="perm">the permutation order</param>
		/// <param name="size">the original tensor's size / extent</param>
		public PermuteInput(int[] perm, int[] size)
		{
			this.Perm = perm; this.Size = size;
		}
		#endregion

		#region equality
		/// <summary>
		/// Indicates whether this <see cref="PermuteInput"/> and a specified object are equal.
		/// </summary>
		/// <param name="obj">The object to compare with the current instance.</param>
		/// <returns>true if obj and this instance are the same type and represent the same value</returns>
		public override bool Equals(object obj)
		{
			if (obj is PermuteInput p)
				return this.Equals(p);
			return false;
		}

		/// <summary>
		/// Indicates whether this <see cref="PermuteInput"/> and a specified object are equal.
		/// </summary>
		/// <param name="other">The object to compare with the current instance.</param>
		/// <returns>true if obj and this instance are the same type and represent the same value</returns>
		public bool Equals(PermuteInput other)
		{
			if (this.Perm is null || this.Size is null || other.Perm is null || other.Size is null)
				return false;
			return (this.Perm == other.Perm || this.Perm.SequenceEqual(other.Perm)) && (this.Size == other.Size || this.Size.SequenceEqual(other.Size));
		}

		/// <summary>
		/// Indicates whether this <see cref="PermuteInput"/> and plain members are equal.
		/// </summary>
		public bool Equals(ReadOnlySpan<int> perm, ReadOnlySpan<int> size)
		{
			if (this.Perm is null || this.Size is null)
				return false;
			return perm.SequenceEqual(this.Perm) && size.SequenceEqual(this.Size);
		}

		/// <summary>
		/// Get the hash code of plain members.
		/// </summary>
		public static int HashCodeOf(ReadOnlySpan<int> perm, ReadOnlySpan<int> size)
		{
			return HashCode.Combine(perm.HashCodeOfSpan(), size.HashCodeOfSpan());
		}

		/// <summary>
		/// Returns the hash code for this <see cref="PermuteInput"/>.
		/// </summary>
		/// <returns>A <see cref="int"/> that is the hash code for this <see cref="PermuteInput"/>.</returns>
		public override int GetHashCode()
		{
			return HashCode.Combine(this.Perm.HashCodeOfArray(), this.Size.HashCodeOfArray());
		}
		
		/// <summary>
		/// Equality operator
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator ==(PermuteInput left, PermuteInput right)
		{
			return left.Equals(right);
		}

		/// <summary>
		/// Not-equality operator
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator !=(PermuteInput left, PermuteInput right)
		{
			return !(left == right);
		}
		#endregion
	}

	/// <summary>
	/// The static class that provides caching <see cref="PermuteInput"/> with corresponding permutation plan <typeparamref name="TPlan"/>.
	/// </summary>
	/// <typeparam name="TPlan">the type of contraction plan</typeparam>
	public static class PermuteCache<TPlan> where TPlan : struct, IDisposable
	{
		private static readonly List<int> HashCodes = new List<int>();

		private static readonly List<PermuteInput> Inputs = new List<PermuteInput>();

		private static readonly List<TPlan> Outputs = new List<TPlan>();

		/// <summary>
		/// Clear all cache of current type <typeparamref name="TPlan"/>.
		/// </summary>
		public static void Clear()
		{
			CacheCommon.Clear(HashCodes, Inputs, (List<int>)null, Outputs);
		}

		/// <summary>
		/// Try to get the contraction plan as <typeparamref name="TPlan"/> from given input parameters.
		/// </summary>
		/// <param name="perm">the permutation order</param>
		/// <param name="size">the original tensor's size / extent</param>
		/// <param name="plan">the output contraction plan as <typeparamref name="TPlan"/> if the given input parameters are cached; null otherwise</param>
		/// <param name="input">the created <see cref="PermuteInput"/> from given input parameters if they are <b>not</b> cached; or the found one</param>
		public static void TryGet(ReadOnlySpan<int> perm, ReadOnlySpan<int> size, out TPlan? plan, out PermuteInput input)
		{
			int hashCode = PermuteInput.HashCodeOf(perm, size);
			var (find, start, end) = CacheCommon.FindStartEndStart(HashCodes, hashCode);
			if (find >= 0)
			{
				for (int i = start; i < end; i++)
				{
					if (Inputs[i].Equals(perm, size))
					{
						find = i;
						break;
					}
				}
				plan = Outputs[find];
				input = Inputs[find];
			}
			else
			{
				plan = null;
				input = new PermuteInput(perm.ToArray(), size.ToArray());
			}
		}

		/// <summary>
		/// Add a pair of <see cref="PermuteInput"/> <paramref name="input"/> and corresponding <typeparamref name="TPlan"/> <paramref name="plan"/>
		/// </summary>
		/// <param name="input">the input information as a <see cref="PermuteInput"/></param>
		/// <param name="plan">the contraction plan as a <typeparamref name="TPlan"/></param>
		/// <returns>true if <paramref name="input"/> is not in current cache, false otherwise</returns>
		public static bool Add(PermuteInput input, TPlan plan)
		{
			return CacheCommon.Add(HashCodes, Inputs, (List<int>)null, Outputs, input, default, plan);
		}
	}
}
