using System;
using System.Runtime.CompilerServices;

using Althea.Linq;
using Althea.Arrays;
using Althea.Helpers;
using Althea.Resources;


namespace Althea.TensorAlgebra
{
	/// <summary>
	/// The permutation order struct of tensors.
	/// </summary>
	public readonly struct TensorOrder : ICloneable<TensorOrder>, IEquatable<TensorOrder>
	{
		#region private enum
		private enum OrderType : short
		{
			Empty = 0,
			Index,
			Char,
			RangeAll,
			RangeStart,
			RangeEnd
		}
		#endregion

		#region static
		/// <summary>
		/// The identity permutation (in fact this is the default value)
		/// </summary>
		public static TensorOrder Identity => default;

		private const short MAX_RANK = 64 / (2 + 2);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static short ToShort(Index index) => checked((short)(index.IsFromEnd ? ~index.Value : index.Value));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static int FromShort(short s, int rank)
		{
			int offset = s < 0 ? rank + s + 1 : s;
			if (offset >= rank || offset < 0)
				throw new ArgumentOutOfRangeException(nameof(s), s, Parameter.InvalidValue);
			return offset;
		}
		#endregion

		#region initialize and clone
		private readonly FixedBuffer_64<(short, OrderType)> order;

		private TensorOrder(FixedBuffer_64<(short, OrderType)> order) => this.order = order;

		/// <summary>
		/// Create an order from a general tuple whose element must be <see cref="short"/>, <see cref="int"/>, <see cref="long"/>, <see cref="Index"/> or <see cref="Range"/> (base-zero order index and range, cannot be negative) or <see cref="char"/> (character label which can only be checked when calling <see cref="GetIntArrayOrder"/> or <see cref="GetCharArrayOrder"/>).
		/// </summary>
		/// <param name="tuple">The general tuple of any length to indicate the permutation order; specially, if an element is <see cref="Range.All"/>, it is regarded as the rest of the indices in ascending order.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="tuple"/> is null or of zero length</exception>
		/// <exception cref="ArgumentOutOfRangeException">If any of the <paramref name="tuple"/> elements is not of the allowed types</exception>
		/// <exception cref="ArgumentException">If the length of <paramref name="tuple"/> is larger than 16 -- the maximum allowed value; or if there are duplicated indices or more than one <see cref="Range.All"/> in <paramref name="tuple"/></exception>
		public TensorOrder(ITuple tuple)
		{
			if (tuple is null || tuple.Length == 0)
				throw new ArgumentNullException(nameof(tuple));
			if (tuple.Length > MAX_RANK)
				throw new ArgumentException(Parameter.WrongSize, nameof(tuple));

			this.order = new FixedBuffer_64<(short, OrderType)>();
			int current = 0;
			for (int i = 0; i < tuple.Length; i++)
			{
				var val = tuple[i];
				if (val is null)
					throw new ArgumentNullException(nameof(tuple));
				(short, OrderType) newOne;
				if (val is byte || val is sbyte || val is short || val is ushort || val is int || val is uint || val is long || val is ulong)
				{
					if ((dynamic)val < 0 || (dynamic)val >= MAX_RANK)
						throw new ArgumentOutOfRangeException(nameof(tuple), tuple, Parameter.CannotNegative);
					newOne = (checked((short)(dynamic)val), OrderType.Index);
				}
				else if (val is char c)
				{
					newOne = ((short)c, OrderType.Char);
				}
				else if (val is Index id)
				{
					newOne = (ToShort(id), OrderType.Index);
				}
				else if (tuple[i] is Range r)
				{
					if (r.Equals(Range.All) && this.order.Contains(OrderType.RangeAll, static o => o.Item2))
						throw new ArgumentException(Parameter.DuplicateIndices, nameof(tuple));
					if (r.Start.Equals(r.End))
						throw new ArgumentException(Parameter.DuplicateIndices, nameof(tuple));
					newOne = (ToShort(r.Start), OrderType.RangeStart);
					// check
					if (this.order.Contains(newOne))
						throw new ArgumentException(Parameter.DuplicateIndices, nameof(tuple));
					else
						this.order[current++] = newOne;
					newOne = (ToShort(r.End), OrderType.RangeEnd);
				}
				else
				{
					throw new NotSupportedException(Support.DataType);
				}
				// check
				if (this.order.Contains(newOne))
					throw new ArgumentException(Parameter.DuplicateIndices, nameof(tuple));
				else
					this.order[current++] = newOne;
			}
		}

		/// <summary>
		/// Create an order from a given (zero-based) permutation order as an array of <see cref="Index"/>.
		/// </summary>
		/// <param name="indices">The zero-based permutation order as an array of <see cref="Index"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="indices"/> is null or of zero length</exception>
		/// <exception cref="ArgumentException">If the length of <paramref name="indices"/> is larger than 16 -- the maximum allowed value; or if there are duplicated indices or more than one <see cref="Range.All"/> in <paramref name="indices"/></exception>
		public TensorOrder(params Index[] indices)
		{
			Span<int> inds = stackalloc int[indices.Length];
			for (int i = 0; i < inds.Length; i++)
			{
				inds[i] = indices[i].Value;
				if (indices[i].IsFromEnd)
					inds[i] = ~inds[i];
			}
			this = new(inds);
		}

		/// <summary>
		/// Create an order from a given permutation order as an array of <see cref="char"/>.
		/// </summary>
		/// <param name="chars">The permutation order as an array of <see cref="char"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="chars"/> is null or of zero length</exception>
		/// <exception cref="ArgumentException">If the length of <paramref name="chars"/> is larger than 16 -- the maximum allowed value; or if there are duplicated indices or more than one <see cref="Range.All"/> in <paramref name="chars"/></exception>
		public TensorOrder(params char[] chars) : this((ReadOnlySpan<char>)chars) { }

		/// <summary>
		/// Create an order from a given (zero-based) permutation order as a <see cref="ReadOnlySpan{T}"/> of <see cref="int"/>.
		/// </summary>
		/// <param name="indices">The zero-based permutation order as a <see cref="ReadOnlySpan{T}"/> of <see cref="int"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="indices"/> is null or of zero length</exception>
		/// <exception cref="ArgumentException">If the length of <paramref name="indices"/> is larger than 16 -- the maximum allowed value; or if there are duplicated indices or more than one <see cref="Range.All"/> in <paramref name="indices"/></exception>
		/// <exception cref="ArgumentOutOfRangeException">If any element in <paramref name="indices"/> is larger than 16</exception>
		public TensorOrder(ReadOnlySpan<int> indices)
		{
			if (indices.Length > MAX_RANK)
				throw new ArgumentException(Parameter.WrongSize, nameof(indices));
			if (!indices.ElementsUnique())
				throw new ArgumentException(Parameter.DuplicateIndices, nameof(indices));

			this.order = new FixedBuffer_64<(short, OrderType)>();
			for (int i = 0; i < indices.Length; i++)
			{
				var id = indices[i];
				if (id >= MAX_RANK)
					throw new ArgumentOutOfRangeException(nameof(indices), id, Parameter.InvalidValue);
				var newOne = ((short)id, OrderType.Index);
				this.order[i] = newOne;
			}
		}

		/// <summary>
		/// Create an order from a given permutation order as a <see cref="ReadOnlySpan{T}"/> of <see cref="char"/>.
		/// </summary>
		/// <param name="chars">The permutation order as a <see cref="ReadOnlySpan{T}"/> of <see cref="char"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="chars"/> is null or of zero length</exception>
		/// <exception cref="ArgumentException">If the length of <paramref name="chars"/> is larger than 16 -- the maximum allowed value; or if there are duplicated indices or more than one <see cref="Range.All"/> in <paramref name="chars"/></exception>
		public TensorOrder(ReadOnlySpan<char> chars)
		{
			if (chars.Length > MAX_RANK)
				throw new ArgumentException(Parameter.WrongSize, nameof(chars));
			if (!chars.ElementsUnique())
				throw new ArgumentException(Parameter.DuplicateIndices, nameof(chars));

			this.order = new FixedBuffer_64<(short, OrderType)>();
			for (int i = 0; i < chars.Length; i++)
			{
				var newOne = ((short)chars[i], OrderType.Char);
				this.order[i] = newOne;
			}
		}

		/// <summary>
		/// Clone this structure
		/// </summary>
		/// <returns>The cloned new <see cref="TensorOrder"/></returns>
		public TensorOrder Clone()
		{
			return new TensorOrder(this.order);
		}
		#endregion

		#region get result
		/// <summary>
		/// Get the actual permutation order in <see cref="int"/> <see cref="Span{T}"/> provided with the tensor rank
		/// </summary>
		/// <param name="tensor">The target tensor</param>
		/// <param name="allowPartial">Whether to allow the actual permutation order to be a partial order one or not, default false</param>
		/// <param name="outputPermutation">The preallocated <see cref="Span{T}"/> of <see cref="int"/> used to store the output the permutation order</param>
		/// <returns>The output the permutation order which is the first actual rank elements of <paramref name="outputPermutation"/></returns>
		/// <remarks>If this <see cref="TensorOrder"/> is a default value, an identity permutation will be returned</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="tensor"/> or its <see cref="ILabeledTensor.Labels"/> is null</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="tensor"/>.<see cref="ILabeledTensor.Rank">Rank</see> is too small or <paramref name="tensor"/>.<see cref="ILabeledTensor.Labels">Label</see> does not contain all of the <see cref="char"/> label(s) of this <see cref="TensorOrder"/></exception>
		/// <exception cref="ArgumentException">If <paramref name="tensor"/> leads to duplicated result permutation order</exception>
		public Span<int> GetIntSpanOrder(ILabeledTensor tensor, Span<int> outputPermutation, bool allowPartial = false)
		{
			if (tensor is null)
				throw new ArgumentNullException(nameof(tensor));
			int rank = tensor.Rank;
			int length = this.order.NonDefaults;
			if (rank < length)
				throw new ArgumentOutOfRangeException(nameof(tensor), rank, Parameter.InvalidValue);
			if (outputPermutation.Length != rank)
				throw new ArgumentException(Parameter.NotSameSize, nameof(outputPermutation));
			var label = tensor.Labels;
			if (label.Length != rank)
				throw new ArgumentNullException(nameof(tensor));

			// shortcut
			if (length == 0)
				return outputPermutation.FillWithRange(0);

			// fill the output permutation
			var orderSpan = this.order.AsSpan();
			int actualRank = 0;
			int rangeStart = 0;
			for (int i = 0; i < length; i++)
			{
				var item = orderSpan[i];
				switch (item.Item2)
				{
					case OrderType.Index:
						var offset = FromShort(item.Item1, rank);
						outputPermutation[actualRank++] = offset;
						break;
					case OrderType.Char:
						int find = label.IndexOf((char)item.Item1);
						if (find < 0)
							throw new ArgumentOutOfRangeException(nameof(tensor), item.Item1, Parameter.UnexpectedValue);
						outputPermutation[actualRank++] = find;
						break;
					case OrderType.RangeAll:
						outputPermutation[actualRank++] = int.MaxValue; // a place holder
						break;
					case OrderType.RangeStart:
						rangeStart = FromShort(item.Item1, rank);
						break;
					case OrderType.RangeEnd:
						int rangeEnd = FromShort(item.Item1, rank);
						if (rangeEnd <= rangeStart)
							throw new ArgumentOutOfRangeException(nameof(tensor), rank, Parameter.InvalidValue);
						int count = rangeEnd - rangeStart;
						outputPermutation.Slice(actualRank, count).FillWithRange(rangeStart);
						actualRank += count;
						break;
					default:
						throw new InvalidOperationException();
				}
			}

			// check duplicate
			if (outputPermutation.Slice(0, actualRank).DistinctCount() < actualRank)
				throw new ArgumentException(Parameter.DuplicateIndices, nameof(tensor));
			// replace the all range
			if (outputPermutation.Contains(int.MaxValue))
			{
				Span<int> @explicit = stackalloc int[actualRank - 1];
				int now = 0;
				Span<int> difference = stackalloc int[rank - actualRank + 1];
				int index = 0;
				for (int i = 0; i < actualRank; i++)
				{
					if (outputPermutation[i] != int.MaxValue)
						@explicit[now++] = outputPermutation[i];
					else
						index = i;
				}
				now = 0;
				for (int i = 0; i < rank; i++)
				{
					if (!@explicit.Contains(i))
						difference[now++] = i;
				}
				Span<int> temp = stackalloc int[rank];
				outputPermutation.CopyTo(temp);
				difference.CopyTo(outputPermutation.Slice(index, difference.Length));
				temp[(index + 1)..].CopyTo(outputPermutation[(index + difference.Length)..]);
				actualRank = rank;
			}
			// check partial
			if (!allowPartial && actualRank < rank)
				throw new ArgumentException(Parameter.WrongSize, nameof(tensor));
			// return
			return outputPermutation[..actualRank];
		}

		/// <summary>
		/// Get the actual permutation order in <see cref="int"/> array provided with the tensor rank
		/// </summary>
		/// <param name="tensor">The target tensor</param>
		/// <param name="allowPartial">allow the actual permutation order to be a partial one or not, default false</param>
		/// <returns>an array of <see cref="int"/> of base-zero indicating the permutation order</returns>
		/// <exception cref="ArgumentNullException">if <paramref name="tensor"/> or its <see cref="ILabeledTensor.Labels"/> is null</exception>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="tensor"/>'s <see cref="ILabeledTensor.Rank"/> is too small</exception>
		/// <exception cref="ArgumentException">if <paramref name="tensor"/> leads to duplicated result permutation order or ts <see cref="ILabeledTensor.Labels"/> does not contains the label here</exception>
		public int[] GetIntArrayOrder(ILabeledTensor tensor, bool allowPartial = false)
		{
			if (tensor is null)
				throw new ArgumentNullException(nameof(tensor));

			var array = new int[tensor.Rank];
			var output = this.GetIntSpanOrder(tensor, array, allowPartial);
			return output.ToArray();
		}

		/// <summary>
		/// Get the actual permutation order in <see cref="char"/> array provided with the tensor rank
		/// </summary>
		/// <param name="tensor">The target tensor</param>
		/// <param name="allowPartial">allow the actual permutation order to be a partial one or not, default false</param>
		/// <returns>an array of <see cref="char"/> corresponding the <see cref="ILabeledTensor.Labels"/> of <paramref name="tensor"/> indicating the permutation order</returns>
		/// <exception cref="ArgumentNullException">if <paramref name="tensor"/> is null</exception>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="tensor"/>'s <see cref="ILabeledTensor.Rank"/> is too small</exception>
		/// <exception cref="ArgumentException">if <paramref name="tensor"/> leads to duplicated result permutation order</exception>
		public char[] GetCharArrayOrder(ILabeledTensor tensor, bool allowPartial = false)
		{
			if (tensor is null)
				throw new ArgumentNullException(nameof(tensor));

			Span<int> span = stackalloc int[tensor.Rank];
			span = this.GetIntSpanOrder(tensor, span, allowPartial);
			char[] labels = new char[span.Length];
			span.CopyTo(labels, s => tensor.GetLabel(s));
			return labels;
		}
		#endregion

		#region converters
		#region repetitive int tuple converters
		/// <summary>
		/// Implicitly convert from span. See <see cref="TensorOrder(ReadOnlySpan{int})"/> for more detail.
		/// </summary>
		/// <param name="span">The general span to indicate the permutation order</param>
		public static implicit operator TensorOrder(Span<int> span) => new(span);

		/// <summary>
		/// Implicitly convert from span. See <see cref="TensorOrder(ReadOnlySpan{int})"/> for more detail.
		/// </summary>
		/// <param name="span">The general span to indicate the permutation order</param>
		public static implicit operator TensorOrder(ReadOnlySpan<int> span) => new(span);

		/// <summary>
		/// Implicitly convert from tuple. See <see cref="TensorOrder(ReadOnlySpan{int})"/> for more detail.
		/// </summary>
		/// <param name="tuple">The general tuple to indicate the permutation order</param>
		public static implicit operator TensorOrder(ValueTuple<int, int> tuple) => new(stackalloc int[2].FromStruct(tuple));

		/// <summary>
		/// Implicitly convert from tuple. See <see cref="TensorOrder(ReadOnlySpan{int})"/> for more detail.
		/// </summary>
		/// <param name="tuple">The general tuple to indicate the permutation order</param>
		public static implicit operator TensorOrder(ValueTuple<int, int, int> tuple) => new(stackalloc int[3].FromStruct(tuple));

		/// <summary>
		/// Implicitly convert from tuple. See <see cref="TensorOrder(ReadOnlySpan{int})"/> for more detail.
		/// </summary>
		/// <param name="tuple">The general tuple to indicate the permutation order</param>
		public static implicit operator TensorOrder(ValueTuple<int, int, int, int> tuple) => new(stackalloc int[4].FromStruct(tuple));

		/// <summary>
		/// Implicitly convert from tuple. See <see cref="TensorOrder(ReadOnlySpan{int})"/> for more detail.
		/// </summary>
		/// <param name="tuple">The general tuple to indicate the permutation order</param>
		public static implicit operator TensorOrder(ValueTuple<int, int, int, int, int> tuple) => new(stackalloc int[5].FromStruct(tuple));

		/// <summary>
		/// Implicitly convert from tuple. See <see cref="TensorOrder(ReadOnlySpan{int})"/> for more detail.
		/// </summary>
		/// <param name="tuple">The general tuple to indicate the permutation order</param>
		public static implicit operator TensorOrder(ValueTuple<int, int, int, int, int, int> tuple) => new(stackalloc int[6].FromStruct(tuple));

		/// <summary>
		/// Implicitly convert from tuple. See <see cref="TensorOrder(ReadOnlySpan{int})"/> for more detail.
		/// </summary>
		/// <param name="tuple">The general tuple to indicate the permutation order</param>
		public static implicit operator TensorOrder(ValueTuple<int, int, int, int, int, int, int> tuple) => new(stackalloc int[7].FromStruct(tuple));
		#endregion

		#region repetitive char tuple converters
		/// <summary>
		/// Implicitly convert from span. See <see cref="TensorOrder(ReadOnlySpan{char})"/> for more detail.
		/// </summary>
		/// <param name="span">The general span to indicate the permutation order</param>
		public static implicit operator TensorOrder(Span<char> span) => new(span);

		/// <summary>
		/// Implicitly convert from span. See <see cref="TensorOrder(ReadOnlySpan{char})"/> for more detail.
		/// </summary>
		/// <param name="span">The general span to indicate the permutation order</param>
		public static implicit operator TensorOrder(ReadOnlySpan<char> span) => new(span);

		/// <summary>
		/// Implicitly convert from tuple. See <see cref="TensorOrder(ReadOnlySpan{char})"/> for more detail.
		/// </summary>
		/// <param name="tuple">The general tuple to indicate the permutation order</param>
		public static implicit operator TensorOrder(ValueTuple<char, char> tuple) => new(stackalloc char[2].FromStruct(tuple));

		/// <summary>
		/// Implicitly convert from tuple. See <see cref="TensorOrder(ReadOnlySpan{char})"/> for more detail.
		/// </summary>
		/// <param name="tuple">The general tuple to indicate the permutation order</param>
		public static implicit operator TensorOrder(ValueTuple<char, char, char> tuple) => new(stackalloc char[3].FromStruct(tuple));

		/// <summary>
		/// Implicitly convert from tuple. See <see cref="TensorOrder(ReadOnlySpan{char})"/> for more detail.
		/// </summary>
		/// <param name="tuple">The general tuple to indicate the permutation order</param>
		public static implicit operator TensorOrder(ValueTuple<char, char, char, char> tuple) => new(stackalloc char[4].FromStruct(tuple));

		/// <summary>
		/// Implicitly convert from tuple. See <see cref="TensorOrder(ReadOnlySpan{char})"/> for more detail.
		/// </summary>
		/// <param name="tuple">The general tuple to indicate the permutation order</param>
		public static implicit operator TensorOrder(ValueTuple<char, char, char, char, char> tuple) => new(stackalloc char[5].FromStruct(tuple));

		/// <summary>
		/// Implicitly convert from tuple. See <see cref="TensorOrder(ReadOnlySpan{char})"/> for more detail.
		/// </summary>
		/// <param name="tuple">The general tuple to indicate the permutation order</param>
		public static implicit operator TensorOrder(ValueTuple<char, char, char, char, char, char> tuple) => new(stackalloc char[6].FromStruct(tuple));

		/// <summary>
		/// Implicitly convert from tuple. See <see cref="TensorOrder(ReadOnlySpan{char})"/> for more detail.
		/// </summary>
		/// <param name="tuple">The general tuple to indicate the permutation order</param>
		public static implicit operator TensorOrder(ValueTuple<char, char, char, char, char, char, char> tuple) => new(stackalloc char[7].FromStruct(tuple));
		#endregion

		#region repetitive int and range tuple converters
		/// <summary>
		/// Implicitly convert from tuple. See <see cref="TensorOrder(ITuple)"/> for more detail.
		/// </summary>
		/// <param name="tuple">The general tuple to indicate the permutation order</param>
		public static implicit operator TensorOrder(ValueTuple<int, Range> tuple) => new(tuple);

		/// <summary>
		/// Implicitly convert from tuple. See <see cref="TensorOrder(ITuple)"/> for more detail.
		/// </summary>
		/// <param name="tuple">The general tuple to indicate the permutation order</param>
		public static implicit operator TensorOrder(ValueTuple<int, int, Range> tuple) => new(tuple);

		/// <summary>
		/// Implicitly convert from tuple. See <see cref="TensorOrder(ITuple)"/> for more detail.
		/// </summary>
		/// <param name="tuple">The general tuple to indicate the permutation order</param>
		public static implicit operator TensorOrder(ValueTuple<int, int, int, Range> tuple) => new(tuple);

		/// <summary>
		/// Implicitly convert from tuple. See <see cref="TensorOrder(ITuple)"/> for more detail.
		/// </summary>
		/// <param name="tuple">The general tuple to indicate the permutation order</param>
		public static implicit operator TensorOrder(ValueTuple<int, int, int, int, Range> tuple) => new(tuple);

		/// <summary>
		/// Implicitly convert from tuple. See <see cref="TensorOrder(ITuple)"/> for more detail.
		/// </summary>
		/// <param name="tuple">The general tuple to indicate the permutation order</param>
		public static implicit operator TensorOrder(ValueTuple<int, int, int, int, int, Range> tuple) => new(tuple);

		/// <summary>
		/// Implicitly convert from tuple. See <see cref="TensorOrder(ITuple)"/> for more detail.
		/// </summary>
		/// <param name="tuple">The general tuple to indicate the permutation order</param>
		public static implicit operator TensorOrder(ValueTuple<int, int, int, int, int, int, Range> tuple) => new(tuple);

		/// <summary>
		/// Implicitly convert from tuple. See <see cref="TensorOrder(ITuple)"/> for more detail.
		/// </summary>
		/// <param name="tuple">The general tuple to indicate the permutation order</param>
		public static implicit operator TensorOrder(ValueTuple<int, int, int, int, int, int, int, Range> tuple) => new(tuple);
		#endregion

		#region repetitive char and range tuple converters
		/// <summary>
		/// Implicitly convert from tuple. See <see cref="TensorOrder(ITuple)"/> for more detail.
		/// </summary>
		/// <param name="tuple">The general tuple to indicate the permutation order</param>
		public static implicit operator TensorOrder(ValueTuple<char, Range> tuple) => new(tuple);

		/// <summary>
		/// Implicitly convert from tuple. See <see cref="TensorOrder(ITuple)"/> for more detail.
		/// </summary>
		/// <param name="tuple">The general tuple to indicate the permutation order</param>
		public static implicit operator TensorOrder(ValueTuple<char, char, Range> tuple) => new(tuple);

		/// <summary>
		/// Implicitly convert from tuple. See <see cref="TensorOrder(ITuple)"/> for more detail.
		/// </summary>
		/// <param name="tuple">The general tuple to indicate the permutation order</param>
		public static implicit operator TensorOrder(ValueTuple<char, char, char, Range> tuple) => new(tuple);

		/// <summary>
		/// Implicitly convert from tuple. See <see cref="TensorOrder(ITuple)"/> for more detail.
		/// </summary>
		/// <param name="tuple">The general tuple to indicate the permutation order</param>
		public static implicit operator TensorOrder(ValueTuple<char, char, char, char, Range> tuple) => new(tuple);

		/// <summary>
		/// Implicitly convert from tuple. See <see cref="TensorOrder(ITuple)"/> for more detail.
		/// </summary>
		/// <param name="tuple">The general tuple to indicate the permutation order</param>
		public static implicit operator TensorOrder(ValueTuple<char, char, char, char, char, Range> tuple) => new(tuple);

		/// <summary>
		/// Implicitly convert from tuple. See <see cref="TensorOrder(ITuple)"/> for more detail.
		/// </summary>
		/// <param name="tuple">The general tuple to indicate the permutation order</param>
		public static implicit operator TensorOrder(ValueTuple<char, char, char, char, char, char, Range> tuple) => new(tuple);

		/// <summary>
		/// Implicitly convert from tuple. See <see cref="TensorOrder(ITuple)"/> for more detail.
		/// </summary>
		/// <param name="tuple">The general tuple to indicate the permutation order</param>
		public static implicit operator TensorOrder(ValueTuple<char, char, char, char, char, char, char, Range> tuple) => new(tuple);
		#endregion

		#region other converters
		/// <summary>
		/// Implicitly convert from <see cref="Index"/> array. See <see cref="TensorOrder(Index[])"/> for more detail.
		/// </summary>
		/// <param name="order">The <see cref="Index"/> array to indicate the permutation order</param>
		public static implicit operator TensorOrder(Index[] order) => new(order);

		/// <summary>
		/// Implicitly convert from <see cref="int"/> array. See <see cref="TensorOrder(Index[])"/> for more detail.
		/// </summary>
		/// <param name="order">The <see cref="int"/> array to indicate the permutation order</param>
		public static implicit operator TensorOrder(int[] order) => new(order);

		/// <summary>
		/// Implicitly convert from <see cref="char"/> array. See <see cref="TensorOrder(Index[])"/> for more detail.
		/// </summary>
		/// <param name="order">The <see cref="char"/> array to indicate the permutation order</param>
		public static implicit operator TensorOrder(char[] order) => new(order);
		#endregion
		#endregion

		#region equalities
		/// <summary>
		/// Equality with another <see cref="object"/>
		/// </summary>
		/// <param name="obj">another <see cref="object"/></param>
		/// <returns>equal or not</returns>
		public override bool Equals(object? obj)
		{
			if (obj is TensorOrder o)
				return this.Equals(o);
			else
				return false;
		}

		/// <summary>
		/// Equality with another <see cref="TensorOrder"/>
		/// </summary>
		/// <param name="other">another <see cref="TensorOrder"/></param>
		/// <returns>equal or not</returns>
		public bool Equals(TensorOrder other)
		{
			return this.order.Equals(other.order);
		}

		/// <summary>
		/// Get the hash code
		/// </summary>
		/// <returns>The hash code</returns>
		public override int GetHashCode() => this.order.GetHashCode();

		/// <summary>
		/// Equality operator
		/// </summary>
		public static bool operator ==(TensorOrder left, TensorOrder right)
		{
			return left.Equals(right);
		}

		/// <summary>
		/// Inequality operator
		/// </summary>
		public static bool operator !=(TensorOrder left, TensorOrder right)
		{
			return !(left == right);
		}
		#endregion
	}
}
