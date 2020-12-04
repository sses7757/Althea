using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Althea.Linq;


namespace Althea.Arrays
{
	/// <summary>
	/// The permutation order struct of tensors.
	/// </summary>
	public readonly struct TensorOrder : ICloneable, IEquatable<TensorOrder>
	{
		#region initialize and clone
		/// <summary>
		/// The identity permutation
		/// </summary>
		public static TensorOrder Identity { get => new TensorOrder(new ValueType[] { Range.All }); }

		private readonly ValueType[] order;

		private TensorOrder(ValueType[] order) => this.order = order;

		/// <summary>
		/// Create an order from a general tuple whose element must be <see cref="short"/>, <see cref="int"/>, <see cref="long"/>, <see cref="Index"/> or <see cref="Range"/> (base-zero order index and range, cannot be negative) or <see cref="char"/> (character label which can only be checked when calling <see cref="GetIntArrayOrder"/> or <see cref="GetCharArrayOrder"/>).
		/// </summary>
		/// <param name="tuple">the general tuple of any length to indicate the permutation order; specially, if an element is <see cref="Range.All"/>, it is regarded as the rest of the indices in ascending order.</param>
		/// <exception cref="ArgumentNullException">if <paramref name="tuple"/> is null or of zero length</exception>
		/// <exception cref="ArgumentOutOfRangeException">if any of the <paramref name="tuple"/> elements is not of the allowed types</exception>
		/// <exception cref="ArgumentException">if there are duplicated indices or more than one <see cref="Range.All"/> in <paramref name="tuple"/></exception>
		public TensorOrder(ITuple tuple)
		{
			if (tuple is null || tuple.Length == 0)
				throw new ArgumentNullException(nameof(tuple));
			var temp = new List<ValueType>(tuple.Length);
			for (int i = 0; i < tuple.Length; i++)
			{
				ValueType newOne;
				if (tuple[i] is int it)
					newOne = (Index)(it < 0 ? throw new ArgumentOutOfRangeException(nameof(tuple), Resource.ParaCannotNegative) : it);
				else if (tuple[i] is long lt)
					newOne = (Index)checked((int)(lt < 0 ? throw new ArgumentOutOfRangeException(nameof(tuple), Resource.ParaCannotNegative) : lt));
				else if (tuple[i] is short st)
					newOne = (Index)(st < 0 ? throw new ArgumentOutOfRangeException(nameof(tuple), Resource.ParaCannotNegative) : st);
				else if (tuple[i] is Index || tuple[i] is char)
					newOne = (ValueType)tuple[i];
				else if (tuple[i] is Range r)
				{
					if (r.Equals(Range.All) && temp.Contains(Range.All))
						throw new ArgumentException(Resource.DuplicateIndices, nameof(tuple));
					else
						newOne = r;
				}
				else
					throw new ArgumentOutOfRangeException(nameof(tuple), "Other kinds of order" + Resource.BaseNotSupport);
				// check
				if (temp.Contains(newOne))
					throw new ArgumentException(Resource.DuplicateIndices, nameof(tuple));
				else
					temp.Add(newOne);
			}
			this.order = temp.ToArray();
		}

		/// <summary>
		/// Create an order from a given <see cref="Index"/> (zero-based) permutation order.
		/// </summary>
		/// <param name="order">the zero-based permutation order</param>
		/// <exception cref="ArgumentNullException">if <paramref name="order"/> is null or of zero length</exception>
		public TensorOrder(params Index[] order)
		{
			if (order.Distinct().Count < order.Length)
				throw new ArgumentException(Resource.DuplicateIndices, nameof(order));
			this.order = Array.ConvertAll(order, o => (ValueType)o);
		}

		/// <summary>
		/// Clone this order
		/// </summary>
		/// <returns>a new <see cref="TensorOrder"/></returns>
		public object Clone()
		{
			return new TensorOrder(this.order);
		}
		#endregion

		#region get result
		/// <summary>
		/// Get the actual permutation order in <see cref="int"/> <see cref="Span{T}"/> provided with the tensor rank
		/// </summary>
		/// <param name="tensor">the target tensor</param>
		/// <param name="allowPartial">allow the actual permutation order to be a partial one or not, default false</param>
		/// <param name="span">a <see cref="Span{T}"/> of <see cref="int"/> of base-zero indicating the permutation order</param>
		/// <param name="actualRank">the actual rank of output <paramref name="span"/>, may be less than <paramref name="tensor"/>'s <see cref="ITensor.Rank"/> if <paramref name="allowPartial"/> is true</param>
		/// <exception cref="ArgumentNullException">if <paramref name="tensor"/> or its <see cref="ITensor.Label"/> is null</exception>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="tensor"/>'s <see cref="ITensor.Rank"/> is too small</exception>
		/// <exception cref="ArgumentException">if <paramref name="tensor"/> leads to duplicated result permutation order or ts <see cref="ITensor.Label"/> does not contains the label here</exception>
		public void GetIntSpanOrder(ITensor tensor, Span<int> span, out int actualRank, bool allowPartial = false)
		{
			if (tensor is null)
				throw new ArgumentNullException(nameof(tensor));
			int rank = tensor.Rank;
			if (rank < this.order.Length)
				throw new ArgumentOutOfRangeException(nameof(tensor));
			if (span.Length != rank)
				throw new ArgumentException(Resource.VectorLength, nameof(span));
			if (tensor.Label is null || tensor.Label.Count != rank)
				throw new ArgumentNullException(nameof(tensor));
			var label = tensor.Label.ToList();

			actualRank = 0;
			foreach (var item in this.order)
			{
				if (item is Index idx)
				{
					var offset = idx.GetOffset(rank);
					if (offset >= rank || offset < 0)
						throw new ArgumentOutOfRangeException(nameof(tensor));
					span[actualRank++] = offset;
				}
				else if (item is Range range)
				{
					if (range.Equals(Range.All))
					{
						span[actualRank++] = int.MaxValue; // a place holder
					}
					else
					{
						var (offset, count) = range.GetOffsetAndLength(rank);
						if (offset + count > rank || offset < 0)
							throw new ArgumentOutOfRangeException(nameof(tensor));
						span.Slice(actualRank, count).FillWithRange(offset);
						actualRank += count;
					}
				}
				else if (item is char c)
				{
					int find = label.IndexOf(c);
					if (find < 0)
						throw new ArgumentOutOfRangeException(nameof(tensor));
					span[actualRank++] = find;
				}
				else // never here
					throw new NotSupportedException();
			}
			// check duplicate
			if (span.Slice(0, actualRank).DistinctCount() < actualRank)
				throw new ArgumentException(Resource.DuplicateIndices, nameof(tensor));
			// replace the all range
			if (span.Contains(int.MaxValue))
			{
				Span<int> @explicit = stackalloc int[actualRank - 1];
				int now = 0;
				Span<int> difference = stackalloc int[rank - actualRank + 1];
				int index = 0;
				for (int i = 0; i < actualRank; i++)
				{
					if (span[i] != int.MaxValue)
						@explicit[now++] = span[i];
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
				span.CopyTo(temp);
				difference.CopyTo(span.Slice(index, difference.Length));
				temp.Slice(index + 1).CopyTo(span.Slice(index + difference.Length));
				actualRank = rank;
			}
			// check partial
			if (!allowPartial && actualRank < rank)
				throw new ArgumentException(Resource.NotEnoughIndices, nameof(tensor));
		}

		/// <summary>
		/// Get the actual permutation order in <see cref="int"/> array provided with the tensor rank
		/// </summary>
		/// <param name="tensor">the target tensor</param>
		/// <param name="allowPartial">allow the actual permutation order to be a partial one or not, default false</param>
		/// <returns>an array of <see cref="int"/> of base-zero indicating the permutation order</returns>
		/// <exception cref="ArgumentNullException">if <paramref name="tensor"/> or its <see cref="ITensor.Label"/> is null</exception>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="tensor"/>'s <see cref="ITensor.Rank"/> is too small</exception>
		/// <exception cref="ArgumentException">if <paramref name="tensor"/> leads to duplicated result permutation order or ts <see cref="ITensor.Label"/> does not contains the label here</exception>
		public int[] GetIntArrayOrder(ITensor tensor, bool allowPartial = false)
		{
			var array = new int[tensor.Rank];
			this.GetIntSpanOrder(tensor, array, out int actualRank, allowPartial);
			if (actualRank < tensor.Rank)
				return array[..actualRank];
			else
				return array;
		}

		/// <summary>
		/// Get the actual permutation order in <see cref="char"/> array provided with the tensor rank
		/// </summary>
		/// <param name="tensor">the target tensor</param>
		/// <param name="allowPartial">allow the actual permutation order to be a partial one or not, default false</param>
		/// <returns>an array of <see cref="char"/> corresponding the <see cref="ITensor.Label"/> of <paramref name="tensor"/> indicating the permutation order</returns>
		/// <exception cref="ArgumentNullException">if <paramref name="tensor"/> is null</exception>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="tensor"/>'s <see cref="ITensor.Rank"/> is too small</exception>
		/// <exception cref="ArgumentException">if <paramref name="tensor"/> leads to duplicated result permutation order</exception>
		public char[] GetCharArrayOrder(ITensor tensor, bool allowPartial = false)
		{
			var intOrder = this.GetIntArrayOrder(tensor, allowPartial);
			return intOrder.Select(o => tensor.Label[o]).ToArray();
		}
		#endregion

		#region converters
		#region repetitive int tuple converters
		/// <summary>
		/// Implicitly convert from tuple. See <see cref="TensorOrder(ITuple)"/> for more detail.
		/// </summary>
		/// <param name="tuple">the general tuple to indicate the permutation order</param>
		public static implicit operator TensorOrder(ValueTuple<int, int> tuple) => new TensorOrder(tuple);

		/// <summary>
		/// Implicitly convert from tuple. See <see cref="TensorOrder(ITuple)"/> for more detail.
		/// </summary>
		/// <param name="tuple">the general tuple to indicate the permutation order</param>
		public static implicit operator TensorOrder(ValueTuple<int, int, int> tuple) => new TensorOrder(tuple);

		/// <summary>
		/// Implicitly convert from tuple. See <see cref="TensorOrder(ITuple)"/> for more detail.
		/// </summary>
		/// <param name="tuple">the general tuple to indicate the permutation order</param>
		public static implicit operator TensorOrder(ValueTuple<int, int, int, int> tuple) => new TensorOrder(tuple);

		/// <summary>
		/// Implicitly convert from tuple. See <see cref="TensorOrder(ITuple)"/> for more detail.
		/// </summary>
		/// <param name="tuple">the general tuple to indicate the permutation order</param>
		public static implicit operator TensorOrder(ValueTuple<int, int, int, int, int> tuple) => new TensorOrder(tuple);

		/// <summary>
		/// Implicitly convert from tuple. See <see cref="TensorOrder(ITuple)"/> for more detail.
		/// </summary>
		/// <param name="tuple">the general tuple to indicate the permutation order</param>
		public static implicit operator TensorOrder(ValueTuple<int, int, int, int, int, int> tuple) => new TensorOrder(tuple);

		/// <summary>
		/// Implicitly convert from tuple. See <see cref="TensorOrder(ITuple)"/> for more detail.
		/// </summary>
		/// <param name="tuple">the general tuple to indicate the permutation order</param>
		public static implicit operator TensorOrder(ValueTuple<int, int, int, int, int, int, int> tuple) => new TensorOrder(tuple);

		/// <summary>
		/// Implicitly convert from tuple. See <see cref="TensorOrder(ITuple)"/> for more detail.
		/// </summary>
		/// <param name="tuple">the general tuple to indicate the permutation order</param>
		public static implicit operator TensorOrder(ValueTuple<int, int, int, int, int, int, int, int> tuple) => new TensorOrder(tuple);
		#endregion

		#region repetitive char tuple converters
		/// <summary>
		/// Implicitly convert from tuple. See <see cref="TensorOrder(ITuple)"/> for more detail.
		/// </summary>
		/// <param name="tuple">the general tuple to indicate the permutation order</param>
		public static implicit operator TensorOrder(ValueTuple<char, char> tuple) => new TensorOrder(tuple);

		/// <summary>
		/// Implicitly convert from tuple. See <see cref="TensorOrder(ITuple)"/> for more detail.
		/// </summary>
		/// <param name="tuple">the general tuple to indicate the permutation order</param>
		public static implicit operator TensorOrder(ValueTuple<char, char, char> tuple) => new TensorOrder(tuple);

		/// <summary>
		/// Implicitly convert from tuple. See <see cref="TensorOrder(ITuple)"/> for more detail.
		/// </summary>
		/// <param name="tuple">the general tuple to indicate the permutation order</param>
		public static implicit operator TensorOrder(ValueTuple<char, char, char, char> tuple) => new TensorOrder(tuple);

		/// <summary>
		/// Implicitly convert from tuple. See <see cref="TensorOrder(ITuple)"/> for more detail.
		/// </summary>
		/// <param name="tuple">the general tuple to indicate the permutation order</param>
		public static implicit operator TensorOrder(ValueTuple<char, char, char, char, char> tuple) => new TensorOrder(tuple);

		/// <summary>
		/// Implicitly convert from tuple. See <see cref="TensorOrder(ITuple)"/> for more detail.
		/// </summary>
		/// <param name="tuple">the general tuple to indicate the permutation order</param>
		public static implicit operator TensorOrder(ValueTuple<char, char, char, char, char, char> tuple) => new TensorOrder(tuple);

		/// <summary>
		/// Implicitly convert from tuple. See <see cref="TensorOrder(ITuple)"/> for more detail.
		/// </summary>
		/// <param name="tuple">the general tuple to indicate the permutation order</param>
		public static implicit operator TensorOrder(ValueTuple<char, char, char, char, char, char, char> tuple) => new TensorOrder(tuple);

		/// <summary>
		/// Implicitly convert from tuple. See <see cref="TensorOrder(ITuple)"/> for more detail.
		/// </summary>
		/// <param name="tuple">the general tuple to indicate the permutation order</param>
		public static implicit operator TensorOrder(ValueTuple<char, char, char, char, char, char, char, char> tuple) => new TensorOrder(tuple);
		#endregion

		#region repetitive int and range tuple converters
		/// <summary>
		/// Implicitly convert from tuple. See <see cref="TensorOrder(ITuple)"/> for more detail.
		/// </summary>
		/// <param name="tuple">the general tuple to indicate the permutation order</param>
		public static implicit operator TensorOrder(ValueTuple<int, Range> tuple) => new TensorOrder(tuple);

		/// <summary>
		/// Implicitly convert from tuple. See <see cref="TensorOrder(ITuple)"/> for more detail.
		/// </summary>
		/// <param name="tuple">the general tuple to indicate the permutation order</param>
		public static implicit operator TensorOrder(ValueTuple<int, int, Range> tuple) => new TensorOrder(tuple);

		/// <summary>
		/// Implicitly convert from tuple. See <see cref="TensorOrder(ITuple)"/> for more detail.
		/// </summary>
		/// <param name="tuple">the general tuple to indicate the permutation order</param>
		public static implicit operator TensorOrder(ValueTuple<int, int, int, Range> tuple) => new TensorOrder(tuple);

		/// <summary>
		/// Implicitly convert from tuple. See <see cref="TensorOrder(ITuple)"/> for more detail.
		/// </summary>
		/// <param name="tuple">the general tuple to indicate the permutation order</param>
		public static implicit operator TensorOrder(ValueTuple<int, int, int, int, Range> tuple) => new TensorOrder(tuple);

		/// <summary>
		/// Implicitly convert from tuple. See <see cref="TensorOrder(ITuple)"/> for more detail.
		/// </summary>
		/// <param name="tuple">the general tuple to indicate the permutation order</param>
		public static implicit operator TensorOrder(ValueTuple<int, int, int, int, int, Range> tuple) => new TensorOrder(tuple);

		/// <summary>
		/// Implicitly convert from tuple. See <see cref="TensorOrder(ITuple)"/> for more detail.
		/// </summary>
		/// <param name="tuple">the general tuple to indicate the permutation order</param>
		public static implicit operator TensorOrder(ValueTuple<int, int, int, int, int, int, Range> tuple) => new TensorOrder(tuple);

		/// <summary>
		/// Implicitly convert from tuple. See <see cref="TensorOrder(ITuple)"/> for more detail.
		/// </summary>
		/// <param name="tuple">the general tuple to indicate the permutation order</param>
		public static implicit operator TensorOrder(ValueTuple<int, int, int, int, int, int, int, Range> tuple) => new TensorOrder(tuple);
		#endregion

		#region repetitive char and range tuple converters
		/// <summary>
		/// Implicitly convert from tuple. See <see cref="TensorOrder(ITuple)"/> for more detail.
		/// </summary>
		/// <param name="tuple">the general tuple to indicate the permutation order</param>
		public static implicit operator TensorOrder(ValueTuple<char, Range> tuple) => new TensorOrder(tuple);

		/// <summary>
		/// Implicitly convert from tuple. See <see cref="TensorOrder(ITuple)"/> for more detail.
		/// </summary>
		/// <param name="tuple">the general tuple to indicate the permutation order</param>
		public static implicit operator TensorOrder(ValueTuple<char, char, Range> tuple) => new TensorOrder(tuple);

		/// <summary>
		/// Implicitly convert from tuple. See <see cref="TensorOrder(ITuple)"/> for more detail.
		/// </summary>
		/// <param name="tuple">the general tuple to indicate the permutation order</param>
		public static implicit operator TensorOrder(ValueTuple<char, char, char, Range> tuple) => new TensorOrder(tuple);

		/// <summary>
		/// Implicitly convert from tuple. See <see cref="TensorOrder(ITuple)"/> for more detail.
		/// </summary>
		/// <param name="tuple">the general tuple to indicate the permutation order</param>
		public static implicit operator TensorOrder(ValueTuple<char, char, char, char, Range> tuple) => new TensorOrder(tuple);

		/// <summary>
		/// Implicitly convert from tuple. See <see cref="TensorOrder(ITuple)"/> for more detail.
		/// </summary>
		/// <param name="tuple">the general tuple to indicate the permutation order</param>
		public static implicit operator TensorOrder(ValueTuple<char, char, char, char, char, Range> tuple) => new TensorOrder(tuple);

		/// <summary>
		/// Implicitly convert from tuple. See <see cref="TensorOrder(ITuple)"/> for more detail.
		/// </summary>
		/// <param name="tuple">the general tuple to indicate the permutation order</param>
		public static implicit operator TensorOrder(ValueTuple<char, char, char, char, char, char, Range> tuple) => new TensorOrder(tuple);

		/// <summary>
		/// Implicitly convert from tuple. See <see cref="TensorOrder(ITuple)"/> for more detail.
		/// </summary>
		/// <param name="tuple">the general tuple to indicate the permutation order</param>
		public static implicit operator TensorOrder(ValueTuple<char, char, char, char, char, char, char, Range> tuple) => new TensorOrder(tuple);
		#endregion

		/// <summary>
		/// Implicitly convert from <see cref="Index"/> array. See <see cref="TensorOrder(Index[])"/> for more detail.
		/// </summary>
		/// <param name="order">the <see cref="Index"/> array to indicate the permutation order</param>
		public static implicit operator TensorOrder(Index[] order) => new TensorOrder(order);

		/// <summary>
		/// Implicitly convert from <see cref="int"/> array. See <see cref="TensorOrder(Index[])"/> for more detail.
		/// </summary>
		/// <param name="order">the <see cref="int"/> array to indicate the permutation order</param>
		public static implicit operator TensorOrder(int[] order) => new TensorOrder(order.Select(o => (Index)o).ToArray());

		/// <summary>
		/// Implicitly convert from <see cref="char"/> array. See <see cref="TensorOrder(Index[])"/> for more detail.
		/// </summary>
		/// <param name="order">the <see cref="char"/> array to indicate the permutation order</param>
		public static implicit operator TensorOrder(char[] order) => new TensorOrder(Array.ConvertAll(order, o => (ValueType)o));
		#endregion

		#region equalities
		/// <summary>
		/// Equality with another <see cref="object"/>
		/// </summary>
		/// <param name="obj">another <see cref="object"/></param>
		/// <returns>equal or not</returns>
		public override bool Equals(object obj)
		{
			if (obj is null || !(obj is TensorOrder))
				return false;
			return this.Equals((TensorOrder)obj);
		}

		/// <summary>
		/// Equality with another <see cref="TensorOrder"/>
		/// </summary>
		/// <param name="other">another <see cref="TensorOrder"/></param>
		/// <returns>equal or not</returns>
		public bool Equals(TensorOrder other)
		{
			if (this.order is null && other.order is null)
				return true;
			else if (this.order is null != other.order is null)
				return false;
			else
				return this.order.SequenceEqual(other.order);
		}

		/// <summary>
		/// Get the hash code
		/// </summary>
		/// <returns>hash code</returns>
		public override int GetHashCode()
		{
			return HashCode.Combine(order);
		}

		/// <summary>
		/// Equality operator
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns>equal or not</returns>
		public static bool operator ==(TensorOrder left, TensorOrder right)
		{
			return left.Equals(right);
		}

		/// <summary>
		/// Not-equality operator
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns>not equal or equal</returns>
		public static bool operator !=(TensorOrder left, TensorOrder right)
		{
			return !(left == right);
		}
		#endregion
	}
}
