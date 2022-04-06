using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Althea.Array;
using Althea.Helpers;
using Althea.Linq;
using Althea.Resources;


namespace Althea.TensorAlgebra
{
	/// <summary>
	/// The struct act as elements used to create <see cref="TensorOrder"/>s.
	/// </summary>
	public readonly struct OrderElement : IEqualityOperators<OrderElement, OrderElement>
	{
		#region basic
		internal readonly TensorOrder.Union main, auxi;

		private OrderElement(TensorOrder.Union u1, TensorOrder.Union u2 = default)
		{
			this.main = u1;
			this.auxi = u2;
		}

		/// <summary>
		/// Implicitly convert a <see cref="byte"/> as an index to a <see cref="OrderElement"/>.
		/// </summary>
		/// <param name="val">The input value as an index</param>
		public static implicit operator OrderElement(byte val) => new(new(val, TensorOrder.OrderType.Index));

		/// <summary>
		/// Implicitly convert a <see cref="short"/> as an index to a <see cref="OrderElement"/>.
		/// </summary>
		/// <param name="val">The input value as an index</param>
		public static implicit operator OrderElement(short val) => new(new(val, TensorOrder.OrderType.Index));

		/// <summary>
		/// Implicitly convert a <see cref="int"/> as an index to a <see cref="OrderElement"/>.
		/// </summary>
		/// <param name="val">The input value as an index</param>
		public static implicit operator OrderElement(int val) => new(new(checked((short)val), TensorOrder.OrderType.Index));

		/// <summary>
		/// Implicitly convert a <see cref="long"/> as an index to a <see cref="OrderElement"/>.
		/// </summary>
		/// <param name="val">The input value as an index</param>
		public static implicit operator OrderElement(long val) => new(new(checked((short)val), TensorOrder.OrderType.Index));

#pragma warning disable CS3001
		/// <summary>
		/// Implicitly convert a <see cref="sbyte"/> as an index to a <see cref="OrderElement"/>.
		/// </summary>
		/// <param name="val">The input value as an index</param>
		public static implicit operator OrderElement(sbyte val) => new(new(val, TensorOrder.OrderType.Index));


		/// <summary>
		/// Implicitly convert a <see cref="ushort"/> as an index to a <see cref="OrderElement"/>.
		/// </summary>
		/// <param name="val">The input value as an index</param>
		public static implicit operator OrderElement(ushort val) => new(new(checked((short)val), TensorOrder.OrderType.Index));

		/// <summary>
		/// Implicitly convert a <see cref="int"/> as an index to a <see cref="OrderElement"/>.
		/// </summary>
		/// <param name="val">The input value as an index</param>
		public static implicit operator OrderElement(uint val) => new(new(checked((short)val), TensorOrder.OrderType.Index));

		/// <summary>
		/// Implicitly convert a <see cref="long"/> as an index to a <see cref="OrderElement"/>.
		/// </summary>
		/// <param name="val">The input value as an index</param>
		public static implicit operator OrderElement(ulong val) => new(new(checked((short)val), TensorOrder.OrderType.Index));
#pragma warning restore CS3001

		/// <summary>
		/// Implicitly convert a <see cref="char"/> as a character index to a <see cref="OrderElement"/>.
		/// </summary>
		/// <param name="val">The input value as a character index</param>
		public static implicit operator OrderElement(char val) => new(new(val, TensorOrder.OrderType.Char));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static TensorOrder.Union FromIndex(Index val, TensorOrder.OrderType type = TensorOrder.OrderType.Index) => new(checked((short)(val.IsFromEnd ? ~val.Value : val.Value)), type, true);

		/// <summary>
		/// Implicitly convert a <see cref="Index"/> as an index to a <see cref="OrderElement"/>.
		/// </summary>
		/// <param name="val">The input value as an index</param>
		public static implicit operator OrderElement(Index val) => new(FromIndex(val));

		/// <summary>
		/// Implicitly convert a <see cref="Range"/> as a range of indices to a <see cref="OrderElement"/>.
		/// </summary>
		/// <param name="val">The input value as a range of indices</param>
		public static implicit operator OrderElement(Range val)
		{
			if (val.Equals(Range.All))
				return new(new(0, TensorOrder.OrderType.RangeAll));
			else
				return new(FromIndex(val.Start, TensorOrder.OrderType.RangeStart), FromIndex(val.End, TensorOrder.OrderType.RangeEnd));
		}
		#endregion

		#region equality
		/// <summary>
		/// Checks whether this <see cref="OrderElement"/> is the same as the <paramref name="other"/> one.
		/// </summary>
		/// <param name="other">The other <see cref="OrderElement"/> to compare</param>
		/// <returns>True if <c>this == <paramref name="other"/></c>; false otherwise.</returns>
		public bool Equals(OrderElement other) => this.main == other.main && this.auxi == other.auxi;

		/// <summary>
		/// Equality operator
		/// </summary>
		public static bool operator ==(OrderElement left, OrderElement right) => left.Equals(right);

		/// <summary>
		/// Inequality operator
		/// </summary>
		public static bool operator !=(OrderElement left, OrderElement right) => !left.Equals(right);

		/// <summary>
		/// Checks whether this <see cref="OrderElement"/> is the same as the <paramref name="obj"/>.
		/// </summary>
		public override bool Equals(object? obj) => obj is OrderElement o && this.Equals(o);

		/// <summary>
		/// Get the hash code of this <see cref="OrderElement"/>.
		/// </summary>
		public override int GetHashCode() => HashCode.Combine(this.main, this.auxi);
		#endregion
	}

	/// <summary>
	/// The permutation order of tensors as a ref struct.
	/// </summary>
	/// <example><c><see cref="TensorOrder"/> order = ('c', 0, ^1, 1..^4, ..);</c></example>
	public readonly ref partial struct TensorOrder
	{
		#region basic
		internal enum OrderType : short
		{
			Empty = 0,
			Index,
			Char,
			RangeAll,
			RangeStart,
			RangeEnd
		}

		[StructLayout(LayoutKind.Explicit, Size = sizeof(int))]
		internal readonly struct Union : IEqualityOperators<Union, Union>
		{
			#region basic
			[FieldOffset(0)]
			internal readonly char c;
			[FieldOffset(0)]
			internal readonly short index;
			[FieldOffset(2)]
			internal readonly OrderType type;

			internal Union(short index, OrderType type, bool actualIndex = false)
			{
				this.c = default;
				if (index > MAX_RANK || (!actualIndex && index < 0))
					throw new ArgumentOutOfRangeException(nameof(index), index, ParameterError.InvalidValue);
				this.index = index;
				this.type = type;
			}

			internal Union(char c, OrderType type)
			{
				this.index = default;
				this.c = c;
				this.type = type;
			}
			#endregion

			#region equality
			/// <summary>
			/// Checks whether this <see cref="Union"/> is the same as the <paramref name="other"/> one.
			/// </summary>
			/// <param name="other">The other <see cref="Union"/> to compare</param>
			/// <returns>True if <c>this == <paramref name="other"/></c>; false otherwise.</returns>
			public bool Equals(Union other) => this.index == other.index && this.type == other.type;

			/// <summary>
			/// Equality operator
			/// </summary>
			public static bool operator ==(Union left, Union right) => left.Equals(right);

			/// <summary>
			/// Inequality operator
			/// </summary>
			public static bool operator !=(Union left, Union right) => !left.Equals(right);

			/// <summary>
			/// Checks whether this <see cref="Union"/> is the same as the <paramref name="obj"/>.
			/// </summary>
			/// <param name="obj">The other <see cref="object"/> to compare</param>
			/// <returns>True if <c>this == <paramref name="obj"/></c>; false otherwise.</returns>
			public override bool Equals(object? obj) => obj is Union u && this.Equals(u);

			/// <summary>
			/// Get the hash code of this <see cref="Union"/>.
			/// </summary>
			public override int GetHashCode() => HashCode.Combine(this.index, this.type);
			#endregion
		}

		/// <summary>
		/// The identity permutation (in fact this is the default value)
		/// </summary>
		public static TensorOrder Identity => default;

		private const short MAX_RANK = 64 / (2 + 2);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static int FromShort(short s, int rank)
		{
			int offset = s < 0 ? rank + s + 1 : s;
			if (offset >= rank || offset < 0)
				throw new ArgumentOutOfRangeException(nameof(s), s, ParameterError.InvalidValue);
			return offset;
		}

		private readonly FixedBuffer_64<Union> order;

		private TensorOrder(FixedBuffer_64<Union> order) => this.order = order;
		#endregion

		#region get result
		/// <summary>
		/// Get the actual permutation order in <see cref="int"/> <see cref="Span{T}"/> provided with the tensor rank
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="tensor">The target tensor</param>
		/// <param name="allowPartial">Whether to allow the actual permutation order to be a partial order one or not, default false</param>
		/// <param name="outputPermutation">The preallocated <see cref="Span{T}"/> of <see cref="int"/> used to store the output the permutation order</param>
		/// <returns>The output the permutation order which is the first actual rank elements of <paramref name="outputPermutation"/></returns>
		/// <remarks>If this <see cref="TensorOrder"/> is a default value, an identity permutation will be returned</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="tensor"/> or its <see cref="ILabeledTensor{T}.Labels"/> is null</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="tensor"/>.<see cref="ILabeledTensor{T}.Rank">Rank</see> is too small or <paramref name="tensor"/>.<see cref="ILabeledTensor{T}.Labels">Label</see> does not contain all of the <see cref="char"/> label(s) of this <see cref="TensorOrder"/></exception>
		/// <exception cref="ArgumentException">If <paramref name="tensor"/> leads to duplicated result permutation order</exception>
		public Span<int> GetOrder<T>(ILabeledTensor<T> tensor!!, Span<int> outputPermutation, bool allowPartial = false) where T : unmanaged, INumber<T>
		{
			int rank = tensor.Rank;
			int length = this.order.NonDefaults;
			if (rank < length)
				throw new ArgumentOutOfRangeException(nameof(tensor), rank, ParameterError.InvalidValue);
			if (outputPermutation.Length != rank)
				throw new ArgumentException(ParameterError.NotSameSize, nameof(outputPermutation));
			var label = tensor.Labels;
			if (label.Length != rank)
				throw new ArgumentException(ParameterError.WrongSize, nameof(tensor));

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
				switch (item.type)
				{
					case OrderType.Index:
						var offset = FromShort(item.index, rank);
						outputPermutation[actualRank++] = offset;
						break;
					case OrderType.Char:
						int find = label.IndexOf(item.c);
						if (find < 0)
							throw new ArgumentOutOfRangeException(nameof(tensor), item.index, ParameterError.UnexpectedValue);
						outputPermutation[actualRank++] = find;
						break;
					case OrderType.RangeAll:
						outputPermutation[actualRank++] = int.MaxValue; // a place holder
						break;
					case OrderType.RangeStart:
						rangeStart = FromShort(item.index, rank);
						break;
					case OrderType.RangeEnd:
						int rangeEnd = FromShort(item.index, rank);
						if (rangeEnd <= rangeStart)
							throw new ArgumentOutOfRangeException(nameof(tensor), rank, ParameterError.InvalidValue);
						int count = rangeEnd - rangeStart;
						outputPermutation.Slice(actualRank, count).FillWithRange(rangeStart);
						actualRank += count;
						break;
					default:
						throw new InvalidOperationException();
				}
			}

			// check duplicate
			if (outputPermutation[..actualRank].DistinctCount() < actualRank)
				throw new ArgumentException(ParameterError.DuplicateIndices, nameof(tensor));
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
				throw new ArgumentException(ParameterError.WrongSize, nameof(tensor));
			// return
			return outputPermutation[..actualRank];
		}

		/// <summary>
		/// Get the actual permutation order in <see cref="int"/> array provided with the tensor rank
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="tensor">The target tensor</param>
		/// <param name="allowPartial">allow the actual permutation order to be a partial one or not, default false</param>
		/// <returns>an array of <see cref="int"/> of base-zero indicating the permutation order</returns>
		/// <exception cref="ArgumentNullException">if <paramref name="tensor"/> or its <see cref="ILabeledTensor{T}.Labels"/> is null</exception>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="tensor"/>'s <see cref="ILabeledTensor{T}.Rank"/> is too small</exception>
		/// <exception cref="ArgumentException">if <paramref name="tensor"/> leads to duplicated result permutation order or ts <see cref="ILabeledTensor{T}.Labels"/> does not contains the label here</exception>
		public int[] GetOrder<T>(ILabeledTensor<T> tensor, bool allowPartial = false) where T : unmanaged, INumber<T>
		{
			if (tensor is null)
				throw new ArgumentNullException(nameof(tensor));

			var array = new int[tensor.Rank];
			var output = this.GetOrder(tensor, array, allowPartial);
			return output.ToArray();
		}

		/// <summary>
		/// Get the actual permutation order in <see cref="char"/> array provided with the tensor rank
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="tensor">The target tensor</param>
		/// <param name="allowPartial">allow the actual permutation order to be a partial one or not, default false</param>
		/// <returns>an array of <see cref="char"/> corresponding the <see cref="ILabeledTensor{T}.Labels"/> of <paramref name="tensor"/> indicating the permutation order</returns>
		/// <exception cref="ArgumentNullException">if <paramref name="tensor"/> is null</exception>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="tensor"/>'s <see cref="ILabeledTensor{T}.Rank"/> is too small</exception>
		/// <exception cref="ArgumentException">if <paramref name="tensor"/> leads to duplicated result permutation order</exception>
		public char[] GetCharOrder<T>(ILabeledTensor<T> tensor, bool allowPartial = false) where T : unmanaged, INumber<T>
		{
			if (tensor is null)
				throw new ArgumentNullException(nameof(tensor));

			Span<int> span = stackalloc int[tensor.Rank];
			span = this.GetOrder(tensor, span, allowPartial);
			char[] labels = new char[span.Length];
			span.CopyTo(labels, s => tensor.GetLabel(s));
			return labels;
		}
		#endregion

		#region equalities
		/// <summary>
		/// Always returns false since ref struct cannot be boxed.
		/// </summary>
		public override bool Equals(object? obj) => false;

		/// <summary>
		/// Checks whether this <see cref="TensorOrder"/> is the same as the <paramref name="other"/> one
		/// </summary>
		/// <param name="other">The other <see cref="TensorOrder"/> to compare</param>
		/// <returns>True if <c>this == <paramref name="other"/></c>; false otherwise.</returns>
		public bool Equals(TensorOrder other) => this.order.Equals(other.order);

		/// <summary>
		/// Always throws <see cref="InvalidOperationException"/> since ref struct cannot be stored on heap.
		/// </summary>
		public override int GetHashCode() => throw new InvalidOperationException();

		/// <summary>
		/// Equality operator
		/// </summary>
		public static bool operator ==(TensorOrder left, TensorOrder right) => left.Equals(right);

		/// <summary>
		/// Inequality operator
		/// </summary>
		public static bool operator !=(TensorOrder left, TensorOrder right) => !left.Equals(right);
		#endregion

		#region conversion
		/// <summary>
		/// Create a new <see cref="TensorOrder"/> with given <see cref="OrderElement"/>s as the order.
		/// </summary>
		/// <param name="elements">The input <see cref="OrderElement"/>s in which <see cref="Range.All"/> represents all remaining indices</param>
		/// <returns>The created <see cref="TensorOrder"/> from given <see cref="OrderElement"/>s.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="elements"/> is empty or null</exception>
		/// <exception cref="ArgumentException">If the <see cref="OrderElement"/>s imply duplicate indices</exception>
		/// <exception cref="ArgumentOutOfRangeException">If any of the <paramref name="elements"/> &lt; 0 or ≥ max_rank</exception>
		/// <exception cref="NotSupportedException">If the total size &gt; max_rank</exception>
		public static TensorOrder Create(params OrderElement[] elements)
		{
			if (elements is null || elements.Length == 0)
				throw new ArgumentNullException(nameof(elements));
			FixedBuffer_64<Union> order = default;
			byte n = 0;
			foreach (var e in elements)
			{
				if (n >= MAX_RANK)
					throw new NotSupportedException();
				if (order.AsSpan(n).Contains(e.main))
					throw new ArgumentException(ParameterError.DuplicateIndices, nameof(elements));
				order[n++] = e.main;
				if (e.auxi != default)
				{
					if (n >= MAX_RANK)
						throw new NotSupportedException();
					if (order.AsSpan(n).Contains(e.auxi))
						throw new ArgumentException(ParameterError.DuplicateIndices, nameof(elements));
					order[n++] = e.auxi;
				}
			}
			return new(order);
		}

		/// <summary>
		/// Create a new <see cref="TensorOrder"/> with given <see cref="OrderElement"/> as the order.
		/// </summary>
		/// <param name="a">The input <see cref="OrderElement"/></param>
		/// <returns>The created <see cref="TensorOrder"/> from given <see cref="OrderElement"/>s.</returns>
		/// <exception cref="ArgumentException">If the <see cref="OrderElement"/> imply duplicate indices</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="a"/> &lt; 0 or ≥ max_rank</exception>
		public static implicit operator TensorOrder(OrderElement a)
		{
			FixedBuffer_64<Union> order = default;
			byte n = 0;
			order[n++] = a.main;
			if (a.auxi != default)
			{
				if (order.AsSpan(n).Contains(a.auxi))
					throw new ArgumentException(ParameterError.DuplicateIndices, nameof(a));
				order[n++] = a.auxi;
			}
			return new(order);
		}
		#endregion
	}
}
