using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Althea.Helpers;
using Althea.Resources;


namespace Althea.Linq
{
	/// <summary>
	/// A replacement of <see cref="Enumerable"/> based on <see cref="Span{T}"/> and <see cref="ReadOnlySpan{T}"/>
	/// </summary>
	public static class SpanLinq
	{
		#region copy
		/// <summary>
		/// Copy <paramref name="span"/> to <paramref name="array"/>.
		/// </summary>
		/// <typeparam name="TIn">The type of <paramref name="span"/></typeparam>
		/// <typeparam name="TOut">The type of <paramref name="array"/></typeparam>
		/// <param name="span">The <see cref="Span{T}"/> to be copied from</param>
		/// <param name="array">The destination <see cref="Span{T}"/> to be copied into</param>
		/// <param name="selector">The converter to each element</param>
		public static void CopyTo<TIn, TOut>(this Span<TIn> span, Span<TOut> array, Converter<TIn, TOut> selector)
		{
			CopyTo((ReadOnlySpan<TIn>)span, array, selector);
		}

		/// <summary>
		/// Copy <paramref name="span"/> to <paramref name="array"/>.
		/// </summary>
		/// <typeparam name="TIn">The type of <paramref name="span"/></typeparam>
		/// <typeparam name="TOut">The type of <paramref name="array"/></typeparam>
		/// <param name="span">The <see cref="ReadOnlySpan{T}"/> to be copied from</param>
		/// <param name="array">The destination <see cref="Span{T}"/> to be copied into</param>
		/// <param name="selector">The converter to each element</param>
		public static void CopyTo<TIn, TOut>(this ReadOnlySpan<TIn> span, Span<TOut> array, Converter<TIn, TOut> selector)
		{
			if (span.Length > array.Length)
				throw new ArgumentException(ParameterError.WrongSize);

			int len = span.Length;
			for (int i = 0; i < len; i++)
			{
				array[i] = selector(span[i]);
			}
		}

		/// <summary>
		/// Zip convert the <paramref name="span1"/> and <paramref name="span2"/> to <paramref name="array"/>.
		/// </summary>
		/// <typeparam name="TIn1">The type of <paramref name="span1"/></typeparam>
		/// <typeparam name="TIn2">The type of <paramref name="span2"/></typeparam>
		/// <typeparam name="TOut">The type of <paramref name="array"/></typeparam>
		/// <param name="span1">The <see cref="Span{T}"/> to be copied from</param>
		/// <param name="span2">The <see cref="ReadOnlySpan{T}"/> to be copied from</param>
		/// <param name="array">The destination <see cref="Span{T}"/> to be copied into</param>
		/// <param name="selector">The converter to each element</param>
		public static void Zip<TIn1, TIn2, TOut>(this Span<TIn1> span1, ReadOnlySpan<TIn2> span2, Span<TOut> array, Func<TIn1, TIn2, TOut> selector)
		{
			Zip((ReadOnlySpan<TIn1>)span1, span2, array, selector);
		}

		/// <summary>
		/// Zip convert the <paramref name="span1"/> and <paramref name="span2"/> to <paramref name="array"/>.
		/// </summary>
		/// <typeparam name="TIn1">The type of <paramref name="span1"/></typeparam>
		/// <typeparam name="TIn2">The type of <paramref name="span2"/></typeparam>
		/// <typeparam name="TOut">The type of <paramref name="array"/></typeparam>
		/// <param name="span1">The <see cref="ReadOnlySpan{T}"/> to be copied from</param>
		/// <param name="span2">The <see cref="ReadOnlySpan{T}"/> to be copied from</param>
		/// <param name="array">The destination <see cref="Span{T}"/> to be copied into</param>
		/// <param name="selector">The converter to each element</param>
		public static void Zip<TIn1, TIn2, TOut>(this ReadOnlySpan<TIn1> span1, ReadOnlySpan<TIn2> span2, Span<TOut> array, Func<TIn1, TIn2, TOut> selector)
		{
			if (span1.Length > array.Length || span2.Length > array.Length || span1.Length != span2.Length)
				throw new ArgumentException(ParameterError.WrongSize);

			int len = array.Length;
			for (int i = 0; i < len; i++)
			{
				array[i] = selector(span1[i], span2[i]);
			}
		}
		#endregion

		#region min max
		/// <summary>
		/// Find the maximum item of <paramref name="span"/>.
		/// </summary>
		/// <typeparam name="T">The data type of array that can be compared</typeparam>
		/// <param name="span">The span to find maximum</param>
		/// <returns>The maximum item</returns>
		public static T Max<T>(this Span<T> span) where T : IComparable<T>
		{
			return Max((ReadOnlySpan<T>)span);
		}

		/// <summary>
		/// Find the maximum item of <paramref name="span"/>.
		/// </summary>
		/// <typeparam name="T">The data type of array that can be compared</typeparam>
		/// <param name="span">The span to find maximum</param>
		/// <returns>The maximum item</returns>
		public static T Max<T>(this ReadOnlySpan<T> span) where T : IComparable<T>
		{
			T maxVal = span[0];
			int len = span.Length;
			for (int i = 1; i < len; i++)
			{
				T val = span[i];
				if (val.CompareTo(maxVal) > 0)
					maxVal = val;
			}
			return maxVal;
		}

		/// <summary>
		/// Find the minimum item of <paramref name="span"/>.
		/// </summary>
		/// <typeparam name="T">The data type of array that can be compared</typeparam>
		/// <param name="span">The span to find minimum</param>
		/// <returns>The minimum item</returns>
		public static T Min<T>(this Span<T> span) where T : IComparable<T>
		{
			return Min((ReadOnlySpan<T>)span);
		}

		/// <summary>
		/// Find the minimum item of <paramref name="span"/>.
		/// </summary>
		/// <typeparam name="T">The data type of array that can be compared</typeparam>
		/// <param name="span">The span to find minimum</param>
		/// <returns>The minimum item</returns>
		public static T Min<T>(this ReadOnlySpan<T> span) where T : IComparable<T>
		{
			T minVal = span[0];
			int len = span.Length;
			for (int i = 1; i < len; i++)
			{
				T val = span[i];
				if (val.CompareTo(minVal) > 0)
					minVal = val;
			}
			return minVal;
		}

		/// <summary>
		/// Find the maximum item of <paramref name="span"/> after <paramref name="selector"/>.
		/// </summary>
		/// <typeparam name="TOrg">The data type of <paramref name="span"/></typeparam>
		/// <typeparam name="T">The data type of array that can be compared</typeparam>
		/// <param name="span">The span to find maximum</param>
		/// <param name="selector">The selector to apply to each element of <paramref name="span"/></param>
		/// <returns>The maximum item</returns>
		public static T Max<TOrg, T>(this Span<TOrg> span, Converter<TOrg, T> selector) where T : IComparable<T>
		{
			return Max((ReadOnlySpan<TOrg>)span, selector);
		}

		/// <summary>
		/// Find the maximum item of <paramref name="span"/> after <paramref name="selector"/>.
		/// </summary>
		/// <typeparam name="TOrg">The data type of <paramref name="span"/></typeparam>
		/// <typeparam name="T">The data type of array that can be compared</typeparam>
		/// <param name="span">The span to find maximum</param>
		/// <param name="selector">The selector to apply to each element of <paramref name="span"/></param>
		/// <returns>The maximum item</returns>
		public static T Max<TOrg, T>(this ReadOnlySpan<TOrg> span, Converter<TOrg, T> selector) where T : IComparable<T>
		{
			T maxVal = selector.Invoke(span[0]);
			int len = span.Length;
			for (int i = 1; i < len; i++)
			{
				T val = selector.Invoke(span[i]);
				if (val.CompareTo(maxVal) > 0)
					maxVal = val;
			}
			return maxVal;
		}

		/// <summary>
		/// Find the minimum item of <paramref name="span"/> after <paramref name="selector"/>.
		/// </summary>
		/// <typeparam name="TOrg">The data type of <paramref name="span"/></typeparam>
		/// <typeparam name="T">The data type of array that can be compared</typeparam>
		/// <param name="span">The span to find minimum</param>
		/// <param name="selector">The selector to apply to each element of <paramref name="span"/></param>
		/// <returns>The minimum item</returns>
		public static T Min<TOrg, T>(this Span<TOrg> span, Converter<TOrg, T> selector) where T : IComparable<T>
		{
			return Min((ReadOnlySpan<TOrg>)span, selector);
		}

		/// <summary>
		/// Find the minimum item of <paramref name="span"/> after <paramref name="selector"/>.
		/// </summary>
		/// <typeparam name="TOrg">The data type of <paramref name="span"/></typeparam>
		/// <typeparam name="T">The data type of array that can be compared</typeparam>
		/// <param name="span">The span to find minimum</param>
		/// <param name="selector">The selector to apply to each element of <paramref name="span"/></param>
		/// <returns>The minimum item</returns>
		public static T Min<TOrg, T>(this ReadOnlySpan<TOrg> span, Converter<TOrg, T> selector) where T : IComparable<T>
		{
			T minVal = selector.Invoke(span[0]);
			int len = span.Length;
			for (int i = 1; i < len; i++)
			{
				T val = selector.Invoke(span[i]);
				if (val.CompareTo(minVal) < 0)
					minVal = val;
			}
			return minVal;
		}
		#endregion

		#region permutation
		/// <summary>
		/// Re-order the <paramref name="array"/> by <paramref name="indices"/> out-of-place such that <c><paramref name="array"/>[indices] = <paramref name="target"/></c>.
		/// </summary>
		/// <typeparam name="T">any type</typeparam>
		/// <param name="array">The array to order</param>
		/// <param name="target">The array to put the reordered result</param>
		/// <param name="indices">The indices to order, if this is null or empty, <paramref name="array"/> will be returned</param>
		public static void ReOrderTo<T>(this Span<T> array, Span<T> target, ReadOnlySpan<int> indices)
		{
			ReOrderTo((ReadOnlySpan<T>)array, target, indices);
		}

		/// <summary>
		/// Re-order the <paramref name="array"/> by <paramref name="indices"/> out-of-place such that <c><paramref name="array"/>[indices] = <paramref name="target"/></c>.
		/// </summary>
		/// <typeparam name="T">any type</typeparam>
		/// <param name="array">The array to order</param>
		/// <param name="target">The array to put the reordered result</param>
		/// <param name="indices">The indices to order, if this is empty, <paramref name="array"/> will be returned</param>
		public static void ReOrderTo<T>(this ReadOnlySpan<T> array, Span<T> target, ReadOnlySpan<int> indices)
		{
			if (indices.IsEmpty)
			{
				array.CopyTo(target);
			}
			else
			{
				int len = indices.Length;
				for (int i = 0; i < len; i++)
				{
					target[i] = array[indices[i]];
				}
			}
		}

		/// <summary>
		/// Inverse order the <paramref name="array"/> by <paramref name="indices"/> out-of-place such that <c><paramref name="target"/>[<paramref name="indices"/>] = <paramref name="array"/></c>.
		/// </summary>
		/// <typeparam name="T">any type</typeparam>
		/// <param name="array">The array to order</param>
		/// <param name="target">The array to put the reordered result</param>
		/// <param name="indices">The indices to order. May has less elements than <paramref name="array"/>. If so, the rest elements in <paramref name="target"/> remains unchanged.</param>
		public static void InverseOrderTo<T>(this Span<T> array, Span<T> target, ReadOnlySpan<int> indices)
		{
			InverseOrderTo((ReadOnlySpan<T>)array, target, indices);
		}

		/// <summary>
		/// Inverse order the <paramref name="array"/> by <paramref name="indices"/> out-of-place such that <c><paramref name="target"/>[<paramref name="indices"/>] = <paramref name="array"/></c>.
		/// </summary>
		/// <typeparam name="T">any type</typeparam>
		/// <param name="array">The array to order</param>
		/// <param name="target">The array to put the reordered result</param>
		/// <param name="indices">The indices to order. May has less elements than <paramref name="array"/>. If so, the rest elements in <paramref name="target"/> remains unchanged.</param>
		public static void InverseOrderTo<T>(this ReadOnlySpan<T> array, Span<T> target, ReadOnlySpan<int> indices)
		{
			if (indices.IsEmpty)
				throw new ArgumentNullException(nameof(indices));
			if (indices.Length > array.Length || indices.Length > target.Length)
				throw new ArgumentException(ParameterError.WrongSize, nameof(indices));

			int len = indices.Length;
			for (int i = 0; i < len; i++)
			{
				target[indices[i]] = array[i];
			}
		}

		/// <summary>
		/// Find the permutation order such that <c><paramref name="array"/>[<paramref name="perm"/>] == <paramref name="target"/></c> at exit
		/// </summary>
		/// <typeparam name="T">any type</typeparam>
		/// <param name="array">The array before permutation</param>
		/// <param name="target">The array after the permutation</param>
		/// <param name="perm">The result permutation order to put in as a <see cref="Span{T}"/>, may be overwritten by undesired values if there is no such permutation</param>
		/// <returns>The success or not</returns>
		public static bool FindPermutationTo<T>(this Span<T> array, ReadOnlySpan<T> target, Span<int> perm) where T : IEquatable<T>
		{
			return FindPermutationTo((ReadOnlySpan<T>)array, target, perm);
		}

		/// <summary>
		/// Find the permutation order such that <c><paramref name="array"/>[<paramref name="perm"/>] == <paramref name="target"/></c> at exit
		/// </summary>
		/// <typeparam name="T">any type</typeparam>
		/// <param name="array">The array before permutation</param>
		/// <param name="target">The array after the permutation</param>
		/// <param name="perm">The result permutation order to put in as a <see cref="Span{T}"/>, may be overwritten by undesired values if there is no such permutation</param>
		/// <returns>The success or not</returns>
		public static bool FindPermutationTo<T>(this ReadOnlySpan<T> array, ReadOnlySpan<T> target, Span<int> perm) where T : IEquatable<T>
		{
			if (array.IsEmpty)
				throw new ArgumentNullException(nameof(array));
			if (target.IsEmpty)
				throw new ArgumentNullException(nameof(target));

			int len = target.Length;
			for (int i = 0; i < len; i++)
			{
				perm[i] = array.IndexOf(target[i]);
				if (perm[i] < 0)
					return false;
			}
			return true;
		}

		/// <summary>
		/// Find the inverse permutation of <paramref name="perm"/> such that <c><paramref name="perm"/>[<paramref name="inv"/>] == <paramref name="inv"/>[<paramref name="perm"/>] == identity permutation</c> at exit
		/// </summary>
		/// <param name="perm">The input permutation</param>
		/// <param name="inv">The output inverse permutation of <paramref name="perm"/></param>
		public static void InversePermutationTo(this Span<int> perm, Span<int> inv)
		{
			InversePermutationTo((ReadOnlySpan<int>)perm, inv);
		}

		/// <summary>
		/// Find the inverse permutation of <paramref name="perm"/> such that <c><paramref name="perm"/>[<paramref name="inv"/>] == <paramref name="inv"/>[<paramref name="perm"/>] == identity permutation</c> at exit
		/// </summary>
		/// <param name="perm">The input permutation</param>
		/// <param name="inv">The output inverse permutation of <paramref name="perm"/></param>
		public static void InversePermutationTo(this ReadOnlySpan<int> perm, Span<int> inv)
		{
			if (perm.IsEmpty)
				throw new ArgumentNullException(nameof(perm));
			if (perm.Length != inv.Length)
				throw new ArgumentException(ParameterError.NotSameSize, nameof(inv));

			int len = perm.Length;
			for (int i = 0; i < len; i++)
			{
				inv[perm[i]] = i;
			}
		}
		#endregion

		#region aggregate of Span
		/// <summary>
		/// List accumulate summation.
		/// </summary>
		/// <param name="span">The span to accumulate</param>
		/// <param name="result">The output accumulated result. If this has length larger than <paramref name="span"/>, both end will be preserved</param>
		/// <param name="init">The initial value</param>
		/// <param name="inclusive">Whether to include lower-index end (true) or the upper-index end (false)</param>
		/// <returns><paramref name="result"/>[..<paramref name="span"/>.<see cref="Span{T}.Length">Length</see>] or <paramref name="result"/>[..(<paramref name="span"/>.<see cref="Span{T}.Length">Length</see> + 1)]</returns>
		public static ReadOnlySpan<T> AccumulateSum<T>(this Span<T> span, Span<T> result, T init = default, bool inclusive = true) where T : unmanaged, IAdditionOperators<T, T, T>
		{
			return AccumulateSum((ReadOnlySpan<T>)span, result, init, inclusive);
		}

		/// <summary>
		/// List accumulate product.
		/// </summary>
		/// <param name="span">The span to accumulate</param>
		/// <param name="result">The output accumulated result. If this has length larger than <paramref name="span"/>, both end will be preserved</param>
		/// <param name="init">The initial value, default 0 will be replaced by 1</param>
		/// <param name="inclusive">Whether to include lower-index end (true) or the upper-index end (false)</param>
		/// <returns><paramref name="result"/>[..<paramref name="span"/>.<see cref="Span{T}.Length">Length</see>] or <paramref name="result"/>[..(<paramref name="span"/>.<see cref="Span{T}.Length">Length</see> + 1)]</returns>
		public static ReadOnlySpan<T> AccumulateProd<T>(this Span<T> span, Span<T> result, T init = default, bool inclusive = true) where T : unmanaged, IMultiplyOperators<T, T, T>, IMultiplicativeIdentity<T, T>, IAdditiveIdentity<T, T>, IEqualityOperators<T, T, bool>
		{
			return AccumulateProd((ReadOnlySpan<T>)span, result, init, inclusive);
		}

		/// <summary>
		/// List summation.
		/// </summary>
		/// <param name="span">The span to accumulate</param>
		/// <returns>The summation result</returns>
		public static T Sum<T>(this Span<T> span) where T : unmanaged, IAdditionOperators<T, T, T>
		{
			return Sum((ReadOnlySpan<T>)span);
		}

		/// <summary>
		/// List product.
		/// </summary>
		/// <param name="span">The span to accumulate</param>
		/// <returns>The product result</returns>
		public static T Prod<T>(this Span<T> span) where T : unmanaged, IMultiplyOperators<T, T, T>
		{
			return Prod((ReadOnlySpan<T>)span);
		}

		/// <summary>
		/// List summation by <paramref name="selector"/>.
		/// </summary>
		/// <param name="span">The span to accumulate</param>
		/// <param name="selector">The selector to apply to each element</param>
		/// <returns>The summation result</returns>
		public static T Sum<TOrg, T>(this Span<TOrg> span, Converter<TOrg, T> selector) where T : unmanaged, IAdditionOperators<T, T, T>
		{
			return Sum((ReadOnlySpan<TOrg>)span, selector);
		}

		/// <summary>
		/// List product by <paramref name="selector"/>.
		/// </summary>
		/// <param name="span">The span to accumulate</param>
		/// <param name="selector">The selector to apply to each element</param>
		/// <returns>The product result</returns>
		public static T Prod<TOrg, T>(this Span<TOrg> span, Converter<TOrg, T> selector) where T : unmanaged, IMultiplyOperators<T, T, T>
		{
			return Prod((ReadOnlySpan<TOrg>)span, selector);
		}

		/// <summary>
		/// List bitwise-and.
		/// </summary>
		/// <param name="span">The span to accumulate</param>
		/// <returns>The bitwise-and result for all elements in <paramref name="span"/></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static T AndAll<T>(this Span<T> span) where T : IBitwiseOperators<T, T, T>
		{
			return AndAll((ReadOnlySpan<T>)span);
		}

		/// <summary>
		/// List bitwise-or.
		/// </summary>
		/// <param name="span">The span to accumulate</param>
		/// <returns>The bitwise-or result for all elements in <paramref name="span"/></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static T OrAll<T>(this Span<T> span) where T : IBitwiseOperators<T, T, T>
		{
			return OrAll((ReadOnlySpan<T>)span);
		}
		#endregion

		#region aggregate of ReadOnlySpan
		/// <summary>
		/// List accumulate summation.
		/// </summary>
		/// <param name="span">The span to accumulate</param>
		/// <param name="result">The output accumulated result. If this has length larger than <paramref name="span"/>, both end will be preserved</param>
		/// <param name="init">The initial value</param>
		/// <param name="inclusive">Whether to include lower-index end (true) or the upper-index end (false)</param>
		/// <returns><paramref name="result"/>[..<paramref name="span"/>.<see cref="Span{T}.Length">Length</see>] or <paramref name="result"/>[..(<paramref name="span"/>.<see cref="Span{T}.Length">Length</see> + 1)]</returns>
		public static ReadOnlySpan<T> AccumulateSum<T>(this ReadOnlySpan<T> span, Span<T> result, T init = default, bool inclusive = true) where T : unmanaged, IAdditionOperators<T, T, T>
		{
			if (span.IsEmpty)
				throw new ArgumentNullException(nameof(span));
			if (result.Length < span.Length)
				throw new ArgumentException(ParameterError.WrongSize, nameof(result));

			int len = span.Length;
			if (result.Length > len)
			{
				result[0] = init;
				for (int i = 0; i < len; i++)
				{
					result[i + 1] = span[i] + result[i];
				}
				return result;
			}
			// else
			if (inclusive)
			{
				result[0] = init; len--;
				for (int i = 0; i < len; i++)
				{
					result[i + 1] = span[i] + result[i];
				}
			}
			else
			{
				result[0] = init + span[0];
				for (int i = 1; i < len; i++)
				{
					result[i] = span[i] + result[i - 1];
				}
			}
			return result[..span.Length];
		}

		/// <summary>
		/// List accumulate product.
		/// </summary>
		/// <param name="span">The span to accumulate</param>
		/// <param name="result">The output accumulated result. If this has length larger than <paramref name="span"/>, both end will be preserved</param>
		/// <param name="init">The initial value, default 0 will be replaced by 1</param>
		/// <param name="inclusive">Whether to include lower-index end (true) or the upper-index end (false)</param>
		/// <returns><paramref name="result"/>[..<paramref name="span"/>.<see cref="Span{T}.Length">Length</see>] or <paramref name="result"/>[..(<paramref name="span"/>.<see cref="ReadOnlySpan{T}.Length">Length</see> + 1)]</returns>
		public static ReadOnlySpan<T> AccumulateProd<T>(this ReadOnlySpan<T> span, Span<T> result, T init = default, bool inclusive = true) where T : unmanaged, IMultiplyOperators<T, T, T>, IMultiplicativeIdentity<T, T>, IAdditiveIdentity<T, T>, IEqualityOperators<T, T, bool>
		{
			if (span.IsEmpty)
				throw new ArgumentNullException(nameof(span));
			if (result.Length < span.Length)
				throw new ArgumentException(ParameterError.WrongSize, nameof(result));
			if (init == T.AdditiveIdentity)
				init = T.MultiplicativeIdentity;

			int len = span.Length;
			if (result.Length > len)
			{
				result[0] = init;
				for (int i = 0; i < len; i++)
				{
					result[i + 1] = span[i] * result[i];
				}
				return result;
			}
			// else
			if (inclusive)
			{
				result[0] = init; len--;
				for (int i = 0; i < len; i++)
				{
					result[i + 1] = span[i] * result[i];
				}
			}
			else
			{
				result[0] = init * span[0];
				for (int i = 1; i < len; i++)
				{
					result[i] = span[i] * result[i - 1];
				}
			}
			return result[..span.Length];
		}

		/// <summary>
		/// List summation.
		/// </summary>
		/// <param name="span">The span to accumulate</param>
		/// <returns>The summation result</returns>
		public static T Sum<T>(this ReadOnlySpan<T> span) where T : unmanaged, IAdditionOperators<T, T, T>
		{
			if (span.IsEmpty)
				throw new ArgumentNullException(nameof(span));

			int len = span.Length;
			T result = span[0];
			for (int i = 1; i < len; i++)
			{
				result = span[i] + result;
			}
			return result;
		}

		/// <summary>
		/// List product.
		/// </summary>
		/// <param name="span">The span to accumulate</param>
		/// <returns>The product result</returns>
		public static T Prod<T>(this ReadOnlySpan<T> span) where T : unmanaged, IMultiplyOperators<T, T, T>
		{
			if (span.IsEmpty)
				throw new ArgumentNullException(nameof(span));

			int len = span.Length;
			T result = span[0];
			for (int i = 1; i < len; i++)
			{
				result = span[i] * result;
			}
			return result;
		}

		/// <summary>
		/// List summation by <paramref name="selector"/>.
		/// </summary>
		/// <param name="span">The span to accumulate</param>
		/// <param name="selector">The selector to apply to each element</param>
		/// <returns>The summation result</returns>
		public static T Sum<TOrg, T>(this ReadOnlySpan<TOrg> span, Converter<TOrg, T> selector) where T : unmanaged, IAdditionOperators<T, T, T>
		{
			if (span.IsEmpty)
				throw new ArgumentNullException(nameof(span));

			int len = span.Length;
			T result = selector.Invoke(span[0]);
			for (int i = 1; i < len; i++)
			{
				result = selector.Invoke(span[i]) + result;
			}
			return result;
		}

		/// <summary>
		/// List product by <paramref name="selector"/>.
		/// </summary>
		/// <param name="span">The span to accumulate</param>
		/// <param name="selector">The selector to apply to each element</param>
		/// <returns>The product result</returns>
		public static T Prod<TOrg, T>(this ReadOnlySpan<TOrg> span, Converter<TOrg, T> selector) where T : unmanaged, IMultiplyOperators<T, T, T>
		{
			if (span.IsEmpty)
				throw new ArgumentNullException(nameof(span));

			int len = span.Length;
			T result = selector.Invoke(span[0]);
			for (int i = 1; i < len; i++)
			{
				result = selector.Invoke(span[i]) * result;
			}
			return result;
		}

		/// <summary>
		/// List bitwise-and.
		/// </summary>
		/// <param name="span">The span to accumulate</param>
		/// <returns>The bitwise-and result for all elements in <paramref name="span"/></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static T AndAll<T>(this ReadOnlySpan<T> span) where T : IBitwiseOperators<T, T, T>
		{
			if (span.IsEmpty)
				throw new ArgumentNullException(nameof(span));

			int len = span.Length;
			T result = span[0];
			for (int i = 1; i < len; i++)
			{
				result &= span[i];
			}
			return result;
		}

		/// <summary>
		/// List bitwise-or.
		/// </summary>
		/// <param name="span">The span to accumulate</param>
		/// <returns>The bitwise-or result for all elements in <paramref name="span"/></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static T OrAll<T>(this ReadOnlySpan<T> span) where T : IBitwiseOperators<T, T, T>
		{
			if (span.IsEmpty)
				throw new ArgumentNullException(nameof(span));

			int len = span.Length;
			T result = span[0];
			for (int i = 1; i < len; i++)
			{
				result |= span[i];
			}
			return result;
		}
		#endregion

		#region indexing
		/// <summary>
		/// Find the index of the first occurrence where <paramref name="predicator"/> gives true for all elements in <paramref name="span"/>
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="span">The span to find in</param>
		/// <param name="predicator">The predicator to check occurrence</param>
		/// <returns>The index of the first occurrence where <paramref name="predicator"/> gives true or -1 if not found</returns>
		public static int IndexOf<T>(this Span<T> span, Predicate<T> predicator)
		{
			return IndexOf((ReadOnlySpan<T>)span, predicator);
		}

		/// <summary>
		/// Find the index of the first occurrence where <paramref name="predicator"/> gives true for all elements in <paramref name="span"/>
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="span">The span to find in</param>
		/// <param name="predicator">The predicator to check occurrence</param>
		/// <returns>The index of the first occurrence where <paramref name="predicator"/> gives true or -1 if not found</returns>
		public static int IndexOf<T>(this ReadOnlySpan<T> span, Predicate<T> predicator)
		{
			int len = span.Length;
			for (int i = 0; i < len; i++)
			{
				if (predicator(span[i]))
					return i;
			}
			return -1;
		}

		/// <summary>
		/// Find the index of the first occurrence of <paramref name="value"/> in <paramref name="span"/> compared by the given <paramref name="comparer"/>
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="span">The span to find in</param>
		/// <param name="value">The value to find</param>
		/// <param name="comparer">The <see cref="EqualityComparer{T}"/> used to compare elements</param>
		/// <returns>The index of the first occurrence of <paramref name="value"/> or -1 if not found</returns>
		public static int IndexOf<T>(this Span<T> span, T value, EqualityComparer<T>? comparer)
		{
			return IndexOf((ReadOnlySpan<T>)span, value, comparer);
		}

		/// <summary>
		/// Find the index of the first occurrence of <paramref name="value"/> in <paramref name="span"/> compared by the given <paramref name="comparer"/>
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="span">The span to find in</param>
		/// <param name="value">The value to find</param>
		/// <param name="comparer">The <see cref="EqualityComparer{T}"/> used to compare elements</param>
		/// <returns>The index of the first occurrence of <paramref name="value"/> or -1 if not found</returns>
		public static int IndexOf<T>(this ReadOnlySpan<T> span, T value, EqualityComparer<T>? comparer)
		{
			comparer ??= EqualityComparer<T>.Default;
			int len = span.Length;
			for (int i = 0; i < len; i++)
			{
				if (comparer.Equals(span[i], value))
					return i;
			}
			return -1;
		}

		/// <summary>
		/// Find the index of the last occurrence where <paramref name="predicator"/> gives true for all elements in <paramref name="span"/>
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="span">The span to find in</param>
		/// <param name="predicator">The predicator to check occurrence</param>
		/// <returns>The index of the last occurrence where <paramref name="predicator"/> gives true or -1 if not found</returns>
		public static int LastIndexOf<T>(this Span<T> span, Predicate<T> predicator)
		{
			return LastIndexOf((ReadOnlySpan<T>)span, predicator);
		}

		/// <summary>
		/// Find the index of the last occurrence where <paramref name="predicator"/> gives true for all elements in <paramref name="span"/>
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="span">The span to find in</param>
		/// <param name="predicator">The predicator to check occurrence</param>
		/// <returns>The index of the last occurrence where <paramref name="predicator"/> gives true or -1 if not found</returns>
		public static int LastIndexOf<T>(this ReadOnlySpan<T> span, Predicate<T> predicator)
		{
			for (int i = span.Length - 1; i >= 0; i--)
			{
				if (predicator(span[i]))
					return i;
			}
			return -1;
		}

		/// <summary>
		/// Find the index of the last occurrence of <paramref name="value"/> in <paramref name="span"/> compared by the given <paramref name="comparer"/>
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="span">The span to find in</param>
		/// <param name="value">The value to find</param>
		/// <param name="comparer">The <see cref="EqualityComparer{T}"/> used to compare elements</param>
		/// <returns>The index of the last occurrence of <paramref name="value"/> or -1 if not found</returns>
		public static int LastIndexOf<T>(this Span<T> span, T value, EqualityComparer<T>? comparer)
		{
			return LastIndexOf((ReadOnlySpan<T>)span, value, comparer);
		}

		/// <summary>
		/// Find the index of the last occurrence of <paramref name="value"/> in <paramref name="span"/> compared by the given <paramref name="comparer"/>
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="span">The span to find in</param>
		/// <param name="value">The value to find</param>
		/// <param name="comparer">The <see cref="EqualityComparer{T}"/> used to compare elements</param>
		/// <returns>The index of the last occurrence of <paramref name="value"/> or -1 if not found</returns>
		public static int LastIndexOf<T>(this ReadOnlySpan<T> span, T value, EqualityComparer<T>? comparer)
		{
			comparer ??= EqualityComparer<T>.Default;
			for (int i = span.Length - 1; i >= 0; i--)
			{
				if (comparer.Equals(span[i], value))
					return i;
			}
			return -1;
		}

		/// <summary>
		/// Find the index of the first occurrence in a <paramref name="sortedSpan"/> which is larger than or equals to <paramref name="value"/>.
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="sortedSpan">The sorted span to find in</param>
		/// <param name="value">The value to find</param>
		/// <returns>The first occurrence in <paramref name="sortedSpan"/> which is larger than or equals to <paramref name="value"/> or the length of <paramref name="sortedSpan"/> if there is no such element.</returns>
		public static int LowerBound<T>(this Span<T> sortedSpan, T value) where T : IComparable<T>, IEquatable<T>
		{
			return LowerBound((ReadOnlySpan<T>)sortedSpan, value);
		}

		/// <summary>
		/// Find the index of the first occurrence in a <paramref name="sortedSpan"/> which is larger than <paramref name="value"/>.
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="sortedSpan">The sorted span to find in</param>
		/// <param name="value">The value to find</param>
		/// <returns>The first occurrence in <paramref name="sortedSpan"/> which is larger than <paramref name="value"/> or the length of <paramref name="sortedSpan"/> if there is no such element.</returns>
		public static int UpperBound<T>(this Span<T> sortedSpan, T value) where T : IComparable<T>, IEquatable<T>
		{
			return UpperBound((ReadOnlySpan<T>)sortedSpan, value);
		}

		/// <summary>
		/// Find the index of the first occurrence in a <paramref name="sortedSpan"/> which is larger than or equals to <paramref name="value"/>.
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="sortedSpan">The sorted span to find in</param>
		/// <param name="value">The value to find</param>
		/// <returns>The first occurrence in <paramref name="sortedSpan"/> which is larger than or equals to <paramref name="value"/> or the length of <paramref name="sortedSpan"/> if there is no such element.</returns>
		public static int LowerBound<T>(this ReadOnlySpan<T> sortedSpan, T value) where T : IComparable<T>, IEquatable<T>
		{
			int find = sortedSpan.BinarySearch(value);
			if (find == 0)
				return find;
			if (find > 0)
				return sortedSpan[..(find + 1)].IndexOf(value);
			else
				return ~find;
		}

		/// <summary>
		/// Find the index of the first occurrence in a <paramref name="sortedSpan"/> which is larger than <paramref name="value"/>.
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="sortedSpan">The sorted span to find in</param>
		/// <param name="value">The value to find</param>
		/// <returns>The first occurrence in <paramref name="sortedSpan"/> which is larger than <paramref name="value"/> or the length of <paramref name="sortedSpan"/> if there is no such element.</returns>
		public static int UpperBound<T>(this ReadOnlySpan<T> sortedSpan, T value) where T : IComparable<T>, IEquatable<T>
		{
			int find = sortedSpan.BinarySearch(value);
			if (find < 0)
				return ~find;
			int newFind = sortedSpan[(find + 1)..].LastIndexOf(value);
			return newFind >= 0 ? newFind + 1 : sortedSpan.Length;
		}
		#endregion

		#region predicate
		/// <summary>
		/// Check if all bytes in the given <paramref name="value"/> are the same.
		/// </summary>
		/// <typeparam name="T">The data type whose address can be obtained</typeparam>
		/// <param name="value">The input value to check</param>
		/// <returns>True if all bytes in the given <paramref name="value"/> are the same; false otherwise.</returns>
		public static unsafe bool AllBytesSame<T>(this T value) where T : unmanaged
		{
			return new ReadOnlySpan<byte>(&value, sizeof(T)).AllSame();
		}

		/// <summary>
		/// Check if all elements of <paramref name="span"/> are zeros by checking if all the bytes in <paramref name="span"/> are 0
		/// </summary>
		/// <param name="span">The span to check</param>
		/// <returns>All elements in <paramref name="span"/> are zeros</returns>
		public static bool FastAllZeros<T>(this Span<T> span) where T : unmanaged
		{
			return FastAllZeros((ReadOnlySpan<T>)span);
		}

		/// <summary>
		/// Check if all elements of <paramref name="span"/> are zeros by checking if all the bytes in <paramref name="span"/> are 0
		/// </summary>
		/// <param name="span">The span to check</param>
		/// <returns>All elements in <paramref name="span"/> are zeros</returns>
		public static unsafe bool FastAllZeros<T>(this ReadOnlySpan<T> span) where T : unmanaged
		{
			int size = Math.Min(span.Length, 8) * sizeof(T);
			ReadOnlySpan<byte> spanFirst = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, byte>(ref span.Ref()), size);
			ReadOnlySpan<byte> testBlock = stackalloc byte[size];
			if (!spanFirst.SequenceEqual(testBlock))
				return false;
			int spanLen = span.Length * sizeof(T);
			if (size == spanLen)
				return true;
			spanFirst = MemoryMarshal.CreateReadOnlySpan(ref spanFirst.Ref(), spanLen - size);
			ReadOnlySpan<byte> spanLast = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.Add(ref spanFirst.Ref(), size), spanLen - size);
			return spanFirst.SequenceEqual(spanLast);
			// Ignore Spelling: memcmp
			//// similar to C code "memcmp(memoryBlock, memoryBlock + 8, memoryBlockSize - 8)"
		}

		/// <summary>
		/// Check if all elements of <paramref name="span"/> are zeros by comparing elements in <paramref name="span"/> individually
		/// </summary>
		/// <param name="span">The span to check</param>
		/// <returns>All elements in <paramref name="span"/> are zeros</returns>
		public static bool AllZeros<T>(this Span<T> span) where T : unmanaged, IEquatable<T>, IAdditiveIdentity<T, T>
		{
			return AllZeros((ReadOnlySpan<T>)span);
		}

		/// <summary>
		/// Check if all elements of <paramref name="span"/> are zeros by comparing elements in <paramref name="span"/> individually
		/// </summary>
		/// <param name="span">The span to check</param>
		/// <returns>All elements in <paramref name="span"/> are zeros</returns>
		public static bool AllZeros<T>(this ReadOnlySpan<T> span) where T : unmanaged, IEquatable<T>, IAdditiveIdentity<T, T>
		{
			int n = span.Length;
			for (int i = 0; i < n; i++)
			{
				if (!span[i].Equals(T.AdditiveIdentity))
					return false;
			}
			return true;
		}

		/// <summary>
		/// Check if all elements of <paramref name="span"/> are the same
		/// </summary>
		/// <param name="span">The span to check</param>
		/// <returns>All elements in <paramref name="span"/> are the same</returns>
		public static unsafe bool AllSame<T>(this Span<T> span) where T : unmanaged, IEquatable<T>
		{
			return AllSame((ReadOnlySpan<T>)span);
		}

		/// <summary>
		/// Check if all elements of <paramref name="span"/> are the same
		/// </summary>
		/// <param name="span">The span to check</param>
		/// <returns>All elements in <paramref name="span"/> are the same</returns>
		public static unsafe bool AllSame<T>(this ReadOnlySpan<T> span) where T : unmanaged, IEquatable<T>
		{
			if (span.Length <= 1)
				return true;
			T v = span[0];
			int len = span.Length;
			for (int i = 1; i < len; i++)
			{
				if (!span[i].Equals(v))
					return false;
			}
			return true;
		}

		/// <summary>
		/// Check if all elements of <paramref name="span"/> fits the <paramref name="predicator"/>
		/// </summary>
		/// <param name="span">The span to predicate</param>
		/// <param name="predicator">The predicator delegate</param>
		/// <returns>Predicate result</returns>
		public static bool All<T>(this Span<T> span, Predicate<T> predicator)
		{
			return All((ReadOnlySpan<T>)span, predicator);
		}

		/// <summary>
		/// Check if all elements of <paramref name="span"/> fits the <paramref name="predicator"/>
		/// </summary>
		/// <param name="span">The span to predicate</param>
		/// <param name="predicator">The predicator delegate</param>
		/// <returns>Predicate result</returns>
		public static bool All<T>(this ReadOnlySpan<T> span, Predicate<T> predicator)
		{
			int len = span.Length;
			for (int i = 0; i < len; i++)
			{
				if (!predicator(span[i]))
					return false;
			}
			return true;
		}

		/// <summary>
		/// Check if any element of <paramref name="span"/> fits the <paramref name="predicator"/>
		/// </summary>
		/// <param name="span">The span to predicate</param>
		/// <param name="predicator">The predicator delegate</param>
		/// <returns>Predicate result</returns>
		public static bool Any<T>(this Span<T> span, Predicate<T> predicator)
		{
			return Any((ReadOnlySpan<T>)span, predicator);
		}

		/// <summary>
		/// Check if any element of <paramref name="span"/> fits the <paramref name="predicator"/>
		/// </summary>
		/// <param name="span">The span to predicate</param>
		/// <param name="predicator">The predicator delegate</param>
		/// <returns>Predicate result</returns>
		public static bool Any<T>(this ReadOnlySpan<T> span, Predicate<T> predicator)
		{
			int len = span.Length;
			for (int i = 0; i < len; i++)
			{
				if (predicator(span[i]))
					return true;
			}
			return false;
		}

		/// <summary>
		/// Check if <paramref name="span"/>'s all elements are sequentially equal to <paramref name="other"/>'s
		/// </summary>
		/// <typeparam name="TL">The left input type</typeparam>
		/// <typeparam name="TR">The right input type</typeparam>
		/// <param name="span">The span to compare</param>
		/// <param name="other">The other span to compare</param>
		/// <param name="equalityComparer">The function used to compare equality</param>
		/// <returns>Sequentially equals or not</returns>
		public static bool SequenceEqual<TL, TR>(this Span<TL> span, ReadOnlySpan<TR> other, Func<TL, TR, bool> equalityComparer)
		{
			return SequenceEqual((ReadOnlySpan<TL>)span, other, equalityComparer);
		}

		/// <summary>
		/// Check if <paramref name="span"/>'s all elements are sequentially equal to <paramref name="other"/>'s
		/// </summary>
		/// <typeparam name="TL">The left input type</typeparam>
		/// <typeparam name="TR">The right input type</typeparam>
		/// <param name="span">The span to compare</param>
		/// <param name="other">The other span to compare</param>
		/// <param name="equalityComparer">The function used to compare equality</param>
		/// <returns>Sequentially equals or not</returns>
		public static bool SequenceEqual<TL, TR>(this ReadOnlySpan<TL> span, ReadOnlySpan<TR> other, Func<TL, TR, bool> equalityComparer)
		{
			int len = span.Length;
			if (len != other.Length)
				return false;
			if (equalityComparer is null)
				throw new ArgumentNullException(nameof(equalityComparer));

			for (int i = 0; i < len; i++)
			{
				if (!equalityComparer(span[i], other[i]))
					return false;
			}
			return true;
		}

		/// <summary>
		/// Check if <paramref name="span"/>'s all elements are sequentially larger or equal than <paramref name="other"/>'s
		/// </summary>
		/// <param name="span">The span to compare</param>
		/// <param name="other">The other span to compare</param>
		/// <returns>Sequentially larger or equals or not</returns>
		public static bool SequenceLargerEqualThan<T>(this Span<T> span, ReadOnlySpan<T> other) where T : IComparisonOperators<T, T, bool>
		{
			return SequenceLargerEqualThan((ReadOnlySpan<T>)span, other);
		}

		/// <summary>
		/// Check if <paramref name="span"/>'s all elements are sequentially larger or equal than <paramref name="other"/>'s
		/// </summary>
		/// <param name="span">The span to compare</param>
		/// <param name="other">The other span to compare</param>
		/// <returns>Sequentially larger or equals or not</returns>
		public static bool SequenceLargerEqualThan<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> other) where T : IComparisonOperators<T, T, bool>
		{
			int len = span.Length;
			if (len != other.Length)
				return false;

			for (int i = 0; i < len; i++)
			{
				if (span[i] < other[i])
					return false;
			}
			return true;
		}

		/// <summary>
		/// Get the first element in <paramref name="span"/> who fits the <paramref name="predicate"/> or the default value if not found
		/// </summary>
		/// <typeparam name="T">Any struct as the data type</typeparam>
		/// <param name="span">The span to find in</param>
		/// <param name="predicate">The <see cref="Predicate{T}"/> used to find the element</param>
		/// <returns>The first element in <paramref name="span"/> who fits the <paramref name="predicate"/> or the default value if not found</returns>
		public static T FirstOrDefault<T>(this Span<T> span, Predicate<T> predicate) where T : struct
		{
			return FirstOrDefault((ReadOnlySpan<T>)span, predicate);
		}

		/// <summary>
		/// Get the first element in <paramref name="span"/> who fits the <paramref name="predicate"/> or the default value if not found
		/// </summary>
		/// <typeparam name="T">Any struct as the data type</typeparam>
		/// <param name="span">The span to find in</param>
		/// <param name="predicate">The <see cref="Predicate{T}"/> used to find the element</param>
		/// <returns>The first element in <paramref name="span"/> who fits the <paramref name="predicate"/> or the default value if not found</returns>
		public static T FirstOrDefault<T>(this ReadOnlySpan<T> span, Predicate<T> predicate) where T : struct
		{
			int len = span.Length;
			if (len == 0)
				return default;
			for (int i = 0; i < len; i++)
			{
				T val = span[i];
				if (predicate(val))
					return val;
			}
			return default;
		}
		#endregion

		#region set value and hash
		/// <summary>
		/// Set the first element of <see cref="Span{T}"/> to the given <paramref name="value"/>
		/// </summary>
		/// <typeparam name="T">any data type</typeparam>
		/// <param name="span">The <see cref="Span{T}"/> to set</param>
		/// <param name="value">The value of type <typeparamref name="T"/></param>
		/// <returns>The <paramref name="span"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="span"/> is empty</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Span<T> SetValue<T>(this Span<T> span, T value)
		{
			if (span.IsEmpty)
				throw new ArgumentNullException(nameof(span));
			span[0] = value;
			return span;
		}

		/// <summary>
		/// Set the first and the second element of <see cref="Span{T}"/> to the given <paramref name="value1"/> and <paramref name="value2"/>
		/// </summary>
		/// <typeparam name="T">any data type</typeparam>
		/// <param name="span">The <see cref="Span{T}"/> to set</param>
		/// <param name="value1">The first value of type <typeparamref name="T"/></param>
		/// <param name="value2">The second value of type <typeparamref name="T"/></param>
		/// <returns>The <paramref name="span"/></returns>
		/// <exception cref="ArgumentException">If <paramref name="span"/> has length smaller than 2</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Span<T> SetValue<T>(this Span<T> span, T value1, T value2)
		{
			if (span.Length < 2)
				throw new ArgumentException(ParameterError.WrongSize, nameof(span));
			span[0] = value1; span[1] = value2;
			return span;
		}

		/// <summary>
		/// Set the first to the third element of <see cref="Span{T}"/> to the given <paramref name="value1"/> and <paramref name="value2"/>
		/// </summary>
		/// <typeparam name="T">any data type</typeparam>
		/// <param name="span">The <see cref="Span{T}"/> to set</param>
		/// <param name="value1">The first value of type <typeparamref name="T"/></param>
		/// <param name="value2">The second value of type <typeparamref name="T"/></param>
		/// <param name="value3">The third value of type <typeparamref name="T"/></param>
		/// <returns>The <paramref name="span"/></returns>
		/// <exception cref="ArgumentException">If <paramref name="span"/> has length smaller than 2</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Span<T> SetValue<T>(this Span<T> span, T value1, T value2, T value3)
		{
			if (span.Length < 3)
				throw new ArgumentException(ParameterError.WrongSize, nameof(span));
			span[0] = value1; span[1] = value2; span[2] = value3;
			return span;
		}

		/// <summary>
		/// Get the hash code of an span using CRC method
		/// </summary>
		/// <typeparam name="T">any struct</typeparam>
		/// <param name="span">The span to get hash code</param>
		/// <returns>The hash code of <paramref name="span"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int HashCodeOfSpan<T>(this Span<T> span) where T : notnull
		{
			return HashCodeOfSpan((ReadOnlySpan<T>)span);
		}

		internal const int CRC_CONST = 314159;

		/// <summary>
		/// Get the hash code of an span using CRC method
		/// </summary>
		/// <typeparam name="T">any struct</typeparam>
		/// <param name="span">The span to get hash code</param>
		/// <returns>The hash code of <paramref name="span"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int HashCodeOfSpan<T>(this ReadOnlySpan<T> span) where T : notnull
		{
			if (span.IsEmpty)
				return 0; // hash code of empty
			int hc = span.Length;
			for (int i = 0; i < span.Length; ++i)
			{
				hc = unchecked(hc * CRC_CONST + span[i].GetHashCode()); // CRC
			}
			return hc;
		}
		#endregion

		#region set operations
		/// <summary>
		/// Check if <paramref name="span"/>'s elements are unique
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="span">The span to pick</param>
		/// <returns><paramref name="span"/>'s elements are unique or not</returns>
		public static bool ElementsUnique<T>(this Span<T> span) where T : unmanaged, IEquatable<T>
		{
			return ElementsUnique((ReadOnlySpan<T>)span);
		}

		/// <summary>
		/// Check if <paramref name="span"/>'s elements are unique
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="span">The span to pick</param>
		/// <returns><paramref name="span"/>'s elements are unique or not</returns>
		public static bool ElementsUnique<T>(this ReadOnlySpan<T> span) where T : unmanaged, IEquatable<T>
		{
			if (span.Length <= 1)
				return true;
			int len = span.Length;
			using var tempArray = len.CheckStackLimit<T>();
			Span<T> temp = tempArray.IsEmpty ? stackalloc T[len] : tempArray.Data;
			var slice = temp[..0];
			int now = 0;
			for (int i = 0; i < len; i++)
			{
				if (!slice.Contains(span[i]))
				{
					temp[now++] = span[i];
					slice = temp[..now];
				}
				else
					return false;
			}
			return true;
		}

		/// <summary>
		/// Get the hash code of a set (order-independent) using ADD method
		/// </summary>
		/// <typeparam name="T">any struct</typeparam>
		/// <param name="set">The set to get hash code</param>
		/// <returns>The hash code of <paramref name="set"/></returns>
		public static int HashCodeOfSet<T>(this Span<T> set) where T : notnull
		{
			return HashCodeOfSet((ReadOnlySpan<T>)set);
		}

		/// <summary>
		/// Get the hash code of a set (order-independent) using ADD method
		/// </summary>
		/// <typeparam name="T">any struct</typeparam>
		/// <param name="set">The set to get hash code</param>
		/// <returns>The hash code of <paramref name="set"/></returns>
		public static int HashCodeOfSet<T>(this ReadOnlySpan<T> set) where T : notnull
		{
			if (set.IsEmpty)
				return 0; // hash code of null
			int hc = 0, len = set.Length;
			for (int i = 0; i < len; ++i)
			{
				hc = unchecked(hc + set[i].GetHashCode());
			}
			return hc;
		}

		/// <summary>
		/// Count the distinct element(s) in <paramref name="span"/>
		/// </summary>
		/// <param name="span">The span to pick</param>
		/// <returns>The number of distinct element(s) <see cref="IReadOnlyList{T}"/></returns>
		public static int DistinctCount<T>(this Span<T> span) where T : unmanaged, IEquatable<T>
		{
			return DistinctCount((ReadOnlySpan<T>)span);
		}

		/// <summary>
		/// Count the distinct element(s) in <paramref name="span"/>
		/// </summary>
		/// <param name="span">The span to pick</param>
		/// <returns>The number of distinct element(s) <see cref="IReadOnlyList{T}"/></returns>
		public static int DistinctCount<T>(this ReadOnlySpan<T> span) where T : unmanaged, IEquatable<T>
		{
			int len = span.Length;
			if (len <= 1)
				return len;
			if (len > 256)
			{
				var set = new HashSet<T>(len);
				for (int i = 0; i < len; i++)
				{
					set.Add(span[i]);
				}
				return set.Count;
			}
			Span<T> temp = stackalloc T[len];
			Span<T> slice = temp[..0];
			int now = 0;
			for (int i = 0; i < len; i++)
			{
				if (!slice.Contains(span[i]))
				{
					temp[now++] = span[i];
					slice = temp[..now];
				}
			}
			return now;
		}

		/// <summary>
		/// Compute the set minus of <paramref name="set"/> and <paramref name="except"/> and store the result in <paramref name="output"/>
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="except">The set whose elements presented in <paramref name="set"/> will not be copied to <paramref name="output"/></param>
		/// <param name="set">The set whose elements not presented in <paramref name="except"/> will be copied to <paramref name="output"/></param>
		/// <param name="output">The output set</param>
		/// <returns><paramref name="output"/>[..real_length]</returns>
		/// <exception cref="ArgumentException">If the lengths are incompatible</exception>
		public static Span<T> SetExept<T>(this Span<T> set, ReadOnlySpan<T> except, Span<T> output) where T : IEquatable<T>
		{
			return SetExept((ReadOnlySpan<T>)set, except, output);
		}

		/// <summary>
		/// Compute the set minus of <paramref name="set"/> and <paramref name="except"/> and store the result in <paramref name="output"/>
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="except">The set whose elements presented in <paramref name="set"/> will not be copied to <paramref name="output"/></param>
		/// <param name="set">The set whose elements not presented in <paramref name="except"/> will be copied to <paramref name="output"/></param>
		/// <param name="output">The output set</param>
		/// <returns><paramref name="output"/>[..real_length]</returns>
		/// <exception cref="ArgumentException">If the lengths are incompatible</exception>
		public static Span<T> SetExept<T>(this ReadOnlySpan<T> set, ReadOnlySpan<T> except, Span<T> output) where T : IEquatable<T>
		{
			// shortcut
			if (except.IsEmpty)
			{
				set.CopyTo(output);
				return output[..set.Length];
			}
			// else
			int now = 0, len = set.Length;
			for (int i = 0; i < len; i++)
			{
				if (!except.Contains(set[i]))
				{
					output[now++] = set[i];
				}
			}
			return output[..now];
		}

		/// <summary>
		/// Get the first element in the set minus of <paramref name="set"/> and <paramref name="except"/> or default value if not found
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="except">The set to be subtracted from <paramref name="set"/></param>
		/// <param name="set">The set to be subtracted by <paramref name="except"/></param>
		/// <returns>The first element in the set minus of <paramref name="set"/> and <paramref name="except"/> or default value if not found</returns>
		public static T FirstOfSetExept<T>(this Span<T> set, ReadOnlySpan<T> except) where T : struct, IEquatable<T>
		{
			return FirstOfSetExept((ReadOnlySpan<T>)set, except);
		}

		/// <summary>
		/// Get the first element in the set minus of <paramref name="set"/> and <paramref name="except"/> or default value if not found
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="except">The set to be subtracted from <paramref name="set"/></param>
		/// <param name="set">The set to be subtracted by <paramref name="except"/></param>
		/// <returns>The first element in the set minus of <paramref name="set"/> and <paramref name="except"/> or default value if not found</returns>
		public static T FirstOfSetExept<T>(this ReadOnlySpan<T> set, ReadOnlySpan<T> except) where T : struct, IEquatable<T>
		{
			// shortcut
			if (except.IsEmpty)
			{
				return set.IsEmpty ? default : set[0];
			}
			// else
			int len = set.Length;
			for (int i = 0; i < len; i++)
			{
				T val = set[i];
				if (!except.Contains(val))
					return val;
			}
			return default;
		}

		/// <summary>
		/// Compute the set intersection of <paramref name="set1"/> and <paramref name="set2"/> and store the result in <paramref name="output"/>
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="set1">The first set to get intersection set</param>
		/// <param name="set2">The second set to get intersection set</param>
		/// <param name="output">The output set</param>
		/// <returns><paramref name="output"/>[..real_length]</returns>
		/// <exception cref="ArgumentException">If the lengths are incompatible</exception>
		public static Span<T> SetIntersect<T>(this Span<T> set1, ReadOnlySpan<T> set2, Span<T> output) where T : IEquatable<T>
		{
			return SetIntersect((ReadOnlySpan<T>)set1, set2, output);
		}

		/// <summary>
		/// Compute the set intersection of <paramref name="set1"/> and <paramref name="set2"/> and store the result in <paramref name="output"/>
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="set1">The first set to get intersection set</param>
		/// <param name="set2">The second set to get intersection set</param>
		/// <param name="output">The output set</param>
		/// <returns><paramref name="output"/>[..real_length]</returns>
		/// <exception cref="ArgumentException">If the lengths are incompatible</exception>
		public static Span<T> SetIntersect<T>(this ReadOnlySpan<T> set1, ReadOnlySpan<T> set2, Span<T> output) where T : IEquatable<T>
		{
			// shortcut
			if (set2.IsEmpty || set1.IsEmpty)
			{
				return Span<T>.Empty;
			}
			// else
			int now = 0, len = set1.Length;
			for (int i = 0; i < len; i++)
			{
				if (set2.Contains(set1[i]))
				{
					output[now++] = set1[i];
				}
			}
			return output[..now];
		}

		/// <summary>
		/// Compute the set intersection of <paramref name="set1"/> and <paramref name="set2"/> and store the result indices in <paramref name="outpuIndex"/> which makes <c><paramref name="set1"/>[<paramref name="outpuIndex"/>] == intersect_set</c>.
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="set1">The first set to get intersection set</param>
		/// <param name="set2">The second set to get intersection set</param>
		/// <param name="outpuIndex">The output indices</param>
		/// <returns><paramref name="outpuIndex"/>[..real_length]</returns>
		/// <exception cref="ArgumentException">If the lengths are incompatible</exception>
		public static Span<int> SetIntersectIndex<T>(this Span<T> set1, ReadOnlySpan<T> set2, Span<int> outpuIndex) where T : IEquatable<T>
		{
			return SetIntersectIndex((ReadOnlySpan<T>)set1, set2, outpuIndex);
		}

		/// <summary>
		/// Compute the set intersection of <paramref name="set1"/> and <paramref name="set2"/> and store the result indices in <paramref name="outpuIndex"/> which makes <c><paramref name="set1"/>[<paramref name="outpuIndex"/>] == intersect_set</c>.
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="set1">The first set to get intersection set</param>
		/// <param name="set2">The second set to get intersection set</param>
		/// <param name="outpuIndex">The output indices</param>
		/// <returns><paramref name="outpuIndex"/>[..real_length]</returns>
		/// <exception cref="ArgumentException">If the lengths are incompatible</exception>
		public static Span<int> SetIntersectIndex<T>(this ReadOnlySpan<T> set1, ReadOnlySpan<T> set2, Span<int> outpuIndex) where T : IEquatable<T>
		{
			// shortcut
			if (set2.IsEmpty || set1.IsEmpty)
			{
				return Span<int>.Empty;
			}
			// else
			int now = 0, len = set1.Length;
			for (int i = 0; i < len; i++)
			{
				if (set2.Contains(set1[i]))
				{
					outpuIndex[now++] = i;
				}
			}
			outpuIndex = outpuIndex[..now];
			return outpuIndex[..now];
		}

		/// <summary>
		/// Compute the set intersection of <paramref name="set1"/> and <paramref name="set2"/> and store the result in <paramref name="output"/> and result indices in <paramref name="outpuIndex"/> which makes <c><paramref name="set1"/>[<paramref name="outpuIndex"/>] == <paramref name="output"/></c>.
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="set1">The first set to get intersection set</param>
		/// <param name="set2">The second set to get intersection set</param>
		/// <param name="output">The output set, replaced by <paramref name="output"/>[..return] at exit</param>
		/// <param name="outpuIndex">The output indices, replaced by <paramref name="outpuIndex"/>[..return] at exit</param>
		/// <returns>The real length of <paramref name="output"/> and <paramref name="outpuIndex"/></returns>
		/// <exception cref="ArgumentException">If the lengths are incompatible</exception>
		public static int SetIntersectWithIndex<T>(this Span<T> set1, ReadOnlySpan<T> set2, ref Span<T> output, ref Span<int> outpuIndex) where T : IEquatable<T>
		{
			return SetIntersectWithIndex((ReadOnlySpan<T>)set1, set2, ref output, ref outpuIndex);
		}

		/// <summary>
		/// Compute the set intersection of <paramref name="set1"/> and <paramref name="set2"/> and store the result in <paramref name="output"/> and result indices in <paramref name="outpuIndex"/> which makes <c><paramref name="set1"/>[<paramref name="outpuIndex"/>] == <paramref name="output"/></c>.
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="set1">The first set to get intersection set</param>
		/// <param name="set2">The second set to get intersection set</param>
		/// <param name="output">The output set, replaced by <paramref name="output"/>[..return] at exit</param>
		/// <param name="outpuIndex">The output indices, replaced by <paramref name="outpuIndex"/>[..return] at exit</param>
		/// <returns>The real length of <paramref name="output"/> and <paramref name="outpuIndex"/></returns>
		public static int SetIntersectWithIndex<T>(this ReadOnlySpan<T> set1, ReadOnlySpan<T> set2, ref Span<T> output, ref Span<int> outpuIndex) where T : IEquatable<T>
		{
			// shortcut
			if (set2.IsEmpty || set1.IsEmpty)
			{
				return 0;
			}
			// else
			int now = 0, len = set1.Length;
			for (int i = 0; i < len; i++)
			{
				if (set2.Contains(set1[i]))
				{
					output[now] = set1[i];
					outpuIndex[now++] = i;
				}
			}
			output = output[..now];
			outpuIndex = outpuIndex[..now];
			return now;
		}

		/// <summary>
		/// Compute the set union of <paramref name="set1"/> and <paramref name="set2"/> and store the result in <paramref name="output"/>
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="set1">The first set to get union set</param>
		/// <param name="set2">The second set to get union set</param>
		/// <param name="output">The output set</param>
		/// <returns><paramref name="output"/>[..real_length]</returns>
		/// <exception cref="ArgumentException">If the lengths are incompatible</exception>
		public static Span<T> SetUnion<T>(this Span<T> set1, ReadOnlySpan<T> set2, Span<T> output) where T : IEquatable<T>
		{
			return SetUnion((ReadOnlySpan<T>)set1, set2, output);
		}

		/// <summary>
		/// Compute the set union of <paramref name="set1"/> and <paramref name="set2"/> and store the result in <paramref name="output"/>
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="set1">The first set to get union set</param>
		/// <param name="set2">The second set to get union set</param>
		/// <param name="output">The output set</param>
		/// <returns><paramref name="output"/>[..real_length]</returns>
		/// <exception cref="ArgumentException">If the lengths are incompatible</exception>
		public static Span<T> SetUnion<T>(this ReadOnlySpan<T> set1, ReadOnlySpan<T> set2, Span<T> output) where T : IEquatable<T>
		{
			// shortcut
			if (set1.IsEmpty)
			{
				set2.CopyTo(output);
				return output[..set2.Length];
			}
			// else
			set1.CopyTo(output);
			// shortcut
			if (set2.IsEmpty)
			{
				return output[..set1.Length];
			}
			// else
			int now = set1.Length, len = set2.Length;
			for (int i = 0; i < len; i++)
			{
				if (!set1.Contains(set2[i]))
				{
					output[now++] = set2[i];
				}
			}
			return output[..now];
		}

		/// <summary>
		/// Check whether the given <paramref name="set1"/> and <paramref name="set2"/> represent the same set
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="set1">The first set to check set equals</param>
		/// <param name="set2">The second set to check set equals</param>
		/// <returns><paramref name="set1"/> and <paramref name="set2"/> represent the same set or not</returns>
		/// <remarks>This method assumes that both <paramref name="set1"/> and <paramref name="set2"/> are actually sets.</remarks>
		public static bool SetEquals<T>(this Span<T> set1, ReadOnlySpan<T> set2) where T : IEquatable<T>
		{
			return SetEquals((ReadOnlySpan<T>)set1, set2);
		}

		/// <summary>
		/// Check whether the given <paramref name="set1"/> and <paramref name="set2"/> represent the same set
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="set1">The first set to check set equals</param>
		/// <param name="set2">The second set to check set equals</param>
		/// <returns><paramref name="set1"/> and <paramref name="set2"/> represent the same set or not</returns>
		/// <remarks>This method assumes that both <paramref name="set1"/> and <paramref name="set2"/> are actually sets.</remarks>
		public static bool SetEquals<T>(this ReadOnlySpan<T> set1, ReadOnlySpan<T> set2) where T : IEquatable<T>
		{
			// shortcut
			if (set1.Length != set2.Length)
				return false;
			if ((set1.IsEmpty && set2.IsEmpty) || set1 == set2)
				return true;
			// else
			int len = set1.Length;
			for (int i = 0; i < len; i++)
			{
				if (!set2.Contains(set1[i]))
				{
					return false;
				}
			}
			return true;
		}
		#endregion

		#region range
		/// <summary>
		/// Generate range of type <typeparamref name="T"/> to the target <paramref name="span"/>
		/// </summary>
		/// <typeparam name="T">Any unmanaged type as the data type</typeparam>
		/// <param name="span">The span to fill</param>
		/// <param name="start">The start value of the range</param>
		/// <param name="step">The step of the range, default 0 will be replaced by 1</param>
		/// <returns>The input <paramref name="span"/></returns>
		public static Span<T> FillWithRange<T>(this Span<T> span, T start, T step = default) where T : unmanaged, IAdditiveIdentity<T, T>, IMultiplicativeIdentity<T, T>, IAdditionOperators<T, T, T>
		{
			if (span.IsEmpty)
				throw new ArgumentNullException(nameof(span));
			span[0] = start;
			if (step.Equals(T.AdditiveIdentity))
				step = T.MultiplicativeIdentity;
			for (int i = 1; i < span.Length; i++)
			{
				span[i] = span[i - 1] + step;
			}
			return span;
		}

		/// <summary>
		/// Act on each element of a span.
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="span">The input span to be operated</param>
		/// <param name="action">The action whose parameter is the element in the span</param>
		public static void ForEach<T>(this Span<T> span, Action<T> action)
		{
			ForEach((ReadOnlySpan<T>)span, action);
		}

		/// <summary>
		/// Act on each element of a span.
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="span">The input span to be operated</param>
		/// <param name="action">The action whose parameter is the element in the span</param>
		public static void ForEach<T>(this ReadOnlySpan<T> span, Action<T> action)
		{
			int len = span.Length;
			if (len <= 0)
				return;
			for (int i = 0; i < len; i++)
			{
				action(span[i]);
			}
		}

		/// <summary>
		/// Scale the values in <paramref name="span"/> by <paramref name="scalar"/> in-place
		/// </summary>
		/// <typeparam name="T">Any supported unmanaged number as the data type</typeparam>
		/// <param name="span">The span to be scaled in-place</param>
		/// <param name="scalar">The scalar to multiply to each element in <paramref name="span"/></param>
		public static void Scale<T>(this Span<T> span, T scalar) where T : unmanaged, IEquatable<T>, IAdditiveIdentity<T, T>, IMultiplyOperators<T, T, T>
		{
			int len = span.Length;
			if (len <= 0)
				return;
			if (scalar.Equals(T.AdditiveIdentity))
			{
				span.Clear();
				return;
			}
			for (int i = 0; i < len; i++)
			{
				span[i] *= scalar;
			}
		}
		#endregion
	}
}
