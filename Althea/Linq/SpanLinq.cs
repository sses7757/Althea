using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Althea.NativeTypes;
using Althea.Resources;
using Althea.Helpers;


namespace Althea.Linq
{
	/// <summary>
	/// A replacement of <see cref="System.Linq.Enumerable"/> based on <see cref="Span{T}"/> and <see cref="ReadOnlySpan{T}"/>
	/// </summary>
	public static class SpanLinq
	{
		#region to / from array
		/// <summary>
		/// Copy <paramref name="array"/> to <paramref name="span"/>.
		/// </summary>
		/// <typeparam name="T">The type of <paramref name="span"/> and <paramref name="array"/></typeparam>
		/// <param name="span">The <see cref="Span{T}"/> to be copied into</param>
		/// <param name="array">The destination array to be copied</param>
		/// <returns>The <paramref name="span"/></returns>
		public static Span<T> CopyTo<T>(this IReadOnlyList<T> array, Span<T> span)
		{
			if (span.Length != array.Count)
				throw new ArgumentException(Parameter.NotSameSize);

			for (int i = 0; i < span.Length; i++)
			{
				span[i] = array[i];
			}
			return span;
		}

		/// <summary>
		/// Copy <paramref name="array"/> to <paramref name="span"/>.
		/// </summary>
		/// <typeparam name="TIn">The type of <paramref name="array"/></typeparam>
		/// <typeparam name="TOut">The type of <paramref name="span"/></typeparam>
		/// <param name="span">The <see cref="Span{T}"/> to be copied into</param>
		/// <param name="array">The destination array to be copied</param>
		/// <param name="selector">The converter to each element</param>
		/// <returns>The <paramref name="span"/></returns>
		public static Span<TOut> CopyTo<TIn, TOut>(this IReadOnlyList<TIn> array, Span<TOut> span, Converter<TIn, TOut> selector)
		{
			if (span.Length != array.Count)
				throw new ArgumentException(Parameter.NotSameSize);

			for (int i = 0; i < span.Length; i++)
			{
				span[i] = selector(array[i]);
			}
			return span;
		}

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
			if (span.Length != array.Length)
				throw new ArgumentException(Parameter.NotSameSize);

			for (int i = 0; i < span.Length; i++)
			{
				array[i] = selector(span[i]);
			}
		}
		#endregion

		#region min max
		/// <summary>
		/// Find the maximum item of <paramref name="list"/>.
		/// </summary>
		/// <typeparam name="T">data type of array that can be compared</typeparam>
		/// <param name="list">list to find maximum</param>
		/// <returns>the maximum item</returns>
		public static T Max<T>(this Span<T> list) where T : IComparable<T>
		{
			return Max((ReadOnlySpan<T>)list);
		}

		/// <summary>
		/// Find the maximum item of <paramref name="list"/>.
		/// </summary>
		/// <typeparam name="T">data type of array that can be compared</typeparam>
		/// <param name="list">list to find maximum</param>
		/// <returns>the maximum item</returns>
		public static T Max<T>(this ReadOnlySpan<T> list) where T : IComparable<T>
		{
			T maxVal = list[0];
			for (int i = 0; i < list.Length; i++)
			{
				T val = list[i];
				if (val.CompareTo(maxVal) > 0)
					maxVal = val;
			}
			return maxVal;
		}

		/// <summary>
		/// Find the minimum item of <paramref name="list"/>.
		/// </summary>
		/// <typeparam name="T">data type of array that can be compared</typeparam>
		/// <param name="list">list to find minimum</param>
		/// <returns>the minimum item</returns>
		public static T Min<T>(this Span<T> list) where T : IComparable<T>
		{
			return Min((ReadOnlySpan<T>)list);
		}

		/// <summary>
		/// Find the minimum item of <paramref name="list"/>.
		/// </summary>
		/// <typeparam name="T">data type of array that can be compared</typeparam>
		/// <param name="list">list to find minimum</param>
		/// <returns>the minimum item</returns>
		public static T Min<T>(this ReadOnlySpan<T> list) where T : IComparable<T>
		{
			T minVal = list[0];
			for (int i = 0; i < list.Length; i++)
			{
				T val = list[i];
				if (val.CompareTo(minVal) > 0)
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
		/// <returns>the re-ordered array</returns>
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
		/// <returns>the re-ordered array</returns>
		public static void ReOrderTo<T>(this ReadOnlySpan<T> array, Span<T> target, ReadOnlySpan<int> indices)
		{
			if (indices.IsEmpty)
			{
				array.CopyTo(target);
			}
			else
			{
				for (int i = 0; i < indices.Length; i++)
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
				throw new ArgumentException(Parameter.WrongSize, nameof(indices));

			for (int i = 0; i < indices.Length; i++)
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
		/// <returns>success or not</returns>
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
		/// <returns>success or not</returns>
		public static bool FindPermutationTo<T>(this ReadOnlySpan<T> array, ReadOnlySpan<T> target, Span<int> perm) where T : IEquatable<T>
		{
			if (array.IsEmpty)
				throw new ArgumentNullException(nameof(array));
			if (target.IsEmpty)
				throw new ArgumentNullException(nameof(target));

			for (int i = 0; i < target.Length; i++)
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
				throw new ArgumentException(Parameter.NotSameSize, nameof(inv));

			for (int i = 0; i < perm.Length; i++)
			{
				inv[perm[i]] = i;
			}
		}
		#endregion

		#region aggregate of Span
		/// <summary>
		/// List accumulate summation.
		/// </summary>
		/// <param name="list">The list to accumulate</param>
		/// <param name="result">The output accumulated result. If this has length larger than <paramref name="list"/>, both end will be preserved</param>
		/// <param name="init">The initial value</param>
		/// <param name="inclusive">Whether to include lower-index end (true) or the upper-index end (false)</param>
		/// <returns><paramref name="result"/>[..<paramref name="list"/>.<see cref="Span{T}.Length">Length</see>] or <paramref name="result"/>[..(<paramref name="list"/>.<see cref="Span{T}.Length">Length</see> + 1)]</returns>
		public static ReadOnlySpan<T> AccumulateSum<T>(this Span<T> list, Span<T> result, T init = default, bool inclusive = true) where T : unmanaged
		{
			return AccumulateSum((ReadOnlySpan<T>)list, result, init, inclusive);
		}

		/// <summary>
		/// List accumulate product.
		/// </summary>
		/// <param name="list">The list to accumulate</param>
		/// <param name="result">The output accumulated result. If this has length larger than <paramref name="list"/>, both end will be preserved</param>
		/// <param name="init">The initial value, default 0 will be replaced by 1</param>
		/// <param name="inclusive">Whether to include lower-index end (true) or the upper-index end (false)</param>
		/// <returns><paramref name="result"/>[..<paramref name="list"/>.<see cref="Span{T}.Length">Length</see>] or <paramref name="result"/>[..(<paramref name="list"/>.<see cref="Span{T}.Length">Length</see> + 1)]</returns>
		public static ReadOnlySpan<T> AccumulateProd<T>(this Span<T> list, Span<T> result, T init = default, bool inclusive = true) where T : unmanaged
		{
			return AccumulateProd((ReadOnlySpan<T>)list, result, init, inclusive);
		}

		/// <summary>
		/// List summation.
		/// </summary>
		/// <param name="list">The span to accumulate</param>
		/// <returns>The summation result</returns>
		public static T Sum<T>(this Span<T> list) where T : unmanaged
		{
			return Sum((ReadOnlySpan<T>)list);
		}

		/// <summary>
		/// List product.
		/// </summary>
		/// <param name="list">The span to accumulate</param>
		/// <returns>The product result</returns>
		public static T Prod<T>(this Span<T> list) where T : unmanaged
		{
			return Prod((ReadOnlySpan<T>)list);
		}

		/// <summary>
		/// List summation by <paramref name="selector"/>.
		/// </summary>
		/// <param name="list">The list to accumulate</param>
		/// <param name="selector">The selector to apply to each element</param>
		/// <returns>The summation result</returns>
		public static T Sum<TOrg, T>(this Span<TOrg> list, Converter<TOrg, T> selector) where T : unmanaged
		{
			return Sum((ReadOnlySpan<TOrg>)list, selector);
		}

		/// <summary>
		/// List product by <paramref name="selector"/>.
		/// </summary>
		/// <param name="list">The list to accumulate</param>
		/// <param name="selector">The selector to apply to each element</param>
		/// <returns>The product result</returns>
		public static T Prod<TOrg, T>(this Span<TOrg> list, Converter<TOrg, T> selector) where T : unmanaged
		{
			return Prod((ReadOnlySpan<TOrg>)list, selector);
		}
		#endregion

		#region aggregate of ReadOnlySpan
		/// <summary>
		/// List accumulate summation.
		/// </summary>
		/// <param name="list">The list to accumulate</param>
		/// <param name="result">The output accumulated result. If this has length larger than <paramref name="list"/>, both end will be preserved</param>
		/// <param name="init">The initial value</param>
		/// <param name="inclusive">Whether to include lower-index end (true) or the upper-index end (false)</param>
		/// <returns><paramref name="result"/>[..<paramref name="list"/>.<see cref="Span{T}.Length">Length</see>] or <paramref name="result"/>[..(<paramref name="list"/>.<see cref="Span{T}.Length">Length</see> + 1)]</returns>
		public static ReadOnlySpan<T> AccumulateSum<T>(this ReadOnlySpan<T> list, Span<T> result, T init = default, bool inclusive = true) where T : unmanaged
		{
			if (list.IsEmpty)
				throw new ArgumentNullException(nameof(list));
			if (result.Length < list.Length)
				throw new ArgumentException(Parameter.WrongSize, nameof(result));

			int len = list.Length;
			if (result.Length > len)
			{
				result[0] = init;
				for (int i = 0; i < len; i++)
				{
					result[i + 1] = Const<T>.AddDelegate.Invoke(list[i], result[i]);
				}
				return result;
			}
			// else
			if (inclusive)
			{
				result[0] = init; len--;
				for (int i = 0; i < len; i++)
				{
					result[i + 1] = Const<T>.AddDelegate.Invoke(list[i], result[i]);
				}
			}
			else
			{
				result[0] = Const<T>.AddDelegate.Invoke(init, list[0]);
				for (int i = 1; i < len; i++)
				{
					result[i] = Const<T>.AddDelegate.Invoke(list[i], result[i - 1]);
				}
			}
			return result[..list.Length];
		}

		/// <summary>
		/// List accumulate product.
		/// </summary>
		/// <param name="list">The list to accumulate</param>
		/// <param name="result">The output accumulated result. If this has length larger than <paramref name="list"/>, both end will be preserved</param>
		/// <param name="init">The initial value, default 0 will be replaced by 1</param>
		/// <param name="inclusive">Whether to include lower-index end (true) or the upper-index end (false)</param>
		/// <returns><paramref name="result"/>[..<paramref name="list"/>.<see cref="Span{T}.Length">Length</see>] or <paramref name="result"/>[..(<paramref name="list"/>.<see cref="ReadOnlySpan{T}.Length">Length</see> + 1)]</returns>
		public static ReadOnlySpan<T> AccumulateProd<T>(this ReadOnlySpan<T> list, Span<T> result, T init = default, bool inclusive = true) where T : unmanaged
		{
			if (list.IsEmpty)
				throw new ArgumentNullException(nameof(list));
			if (result.Length < list.Length)
				throw new ArgumentException(Parameter.WrongSize, nameof(result));
			if (init.IsZero())
				init = Const<T>.One;

			int len = list.Length;
			if (result.Length > len)
			{
				result[0] = init;
				for (int i = 0; i < len; i++)
				{
					result[i + 1] = Const<T>.MultiplyDelegate.Invoke(list[i], result[i]);
				}
				return result;
			}
			// else
			if (inclusive)
			{
				result[0] = init; len--;
				for (int i = 0; i < len; i++)
				{
					result[i + 1] = Const<T>.MultiplyDelegate.Invoke(list[i], result[i]);
				}
			}
			else
			{
				result[0] = Const<T>.MultiplyDelegate.Invoke(init, list[0]);
				for (int i = 1; i < len; i++)
				{
					result[i] = Const<T>.MultiplyDelegate.Invoke(list[i], result[i - 1]);
				}
			}
			return result[..list.Length];
		}

		/// <summary>
		/// List summation.
		/// </summary>
		/// <param name="list">The span to accumulate</param>
		/// <returns>The summation result</returns>
		public static T Sum<T>(this ReadOnlySpan<T> list) where T : unmanaged
		{
			if (list.IsEmpty)
				throw new ArgumentNullException(nameof(list));

			int len = list.Length;
			T result = list[0];
			for (int i = 1; i < len; i++)
			{
				result = Const<T>.AddDelegate.Invoke(list[i], result);
			}
			return result;
		}

		/// <summary>
		/// List product.
		/// </summary>
		/// <param name="list">The span to accumulate</param>
		/// <returns>The product result</returns>
		public static T Prod<T>(this ReadOnlySpan<T> list) where T : unmanaged
		{
			if (list.IsEmpty)
				throw new ArgumentNullException(nameof(list));

			int len = list.Length;
			T result = list[0];
			for (int i = 1; i < len; i++)
			{
				result = Const<T>.MultiplyDelegate.Invoke(list[i], result);
			}
			return result;
		}

		/// <summary>
		/// List summation by <paramref name="selector"/>.
		/// </summary>
		/// <param name="list">The list to accumulate</param>
		/// <param name="selector">The selector to apply to each element</param>
		/// <returns>The summation result</returns>
		public static T Sum<TOrg, T>(this ReadOnlySpan<TOrg> list, Converter<TOrg, T> selector) where T : unmanaged
		{
			if (list.IsEmpty)
				throw new ArgumentNullException(nameof(list));

			int len = list.Length;
			T result = selector.Invoke(list[0]);
			for (int i = 1; i < len; i++)
			{
				result = Const<T>.AddDelegate.Invoke(selector.Invoke(list[i]), result);
			}
			return result;
		}

		/// <summary>
		/// List product by <paramref name="selector"/>.
		/// </summary>
		/// <param name="list">The list to accumulate</param>
		/// <param name="selector">The selector to apply to each element</param>
		/// <returns>The product result</returns>
		public static T Prod<TOrg, T>(this ReadOnlySpan<TOrg> list, Converter<TOrg, T> selector) where T : unmanaged
		{
			if (list.IsEmpty)
				throw new ArgumentNullException(nameof(list));

			int len = list.Length;
			T result = selector.Invoke(list[0]);
			for (int i = 1; i < len; i++)
			{
				result = Const<T>.MultiplyDelegate.Invoke(selector.Invoke(list[i]), result);
			}
			return result;
		}
		#endregion

		#region indexing
		/// <summary>
		/// Find the index of the first occurrence where <paramref name="predicator"/> gives true for all elements in <paramref name="span"/>
		/// </summary>
		/// <param name="span">The span to find in</param>
		/// <param name="predicator">The predicator to check occurrence</param>
		/// <returns>The index of the first occurrence where <paramref name="predicator"/> gives true or -1 if not found</returns>
		/// <remarks>extend method of <paramref name="span"/></remarks>
		public static int IndexOf<T>(this Span<T> span, Predicate<T> predicator)
		{
			return IndexOf((ReadOnlySpan<T>)span, predicator);
		}

		/// <summary>
		/// Find the index of the first occurrence where <paramref name="predicator"/> gives true for all elements in <paramref name="span"/>
		/// </summary>
		/// <param name="span">The span to find in</param>
		/// <param name="predicator">The predicator to check occurrence</param>
		/// <returns>The index of the first occurrence where <paramref name="predicator"/> gives true or -1 if not found</returns>
		/// <remarks>extend method of <paramref name="span"/></remarks>
		public static int IndexOf<T>(this ReadOnlySpan<T> span, Predicate<T> predicator)
		{
			for (int i = 0; i < span.Length; i++)
			{
				if (predicator(span[i]))
					return i;
			}
			return -1;
		}

		/// <summary>
		/// Find the index of the last occurrence where <paramref name="predicator"/> gives true for all elements in <paramref name="span"/>
		/// </summary>
		/// <param name="span">The span to find in</param>
		/// <param name="predicator">The predicator to check occurrence</param>
		/// <returns>The index of the last occurrence where <paramref name="predicator"/> gives true or -1 if not found</returns>
		/// <remarks>extend method of <paramref name="span"/></remarks>
		public static int LastIndexOf<T>(this Span<T> span, Predicate<T> predicator)
		{
			return LastIndexOf((ReadOnlySpan<T>)span, predicator);
		}

		/// <summary>
		/// Find the index of the last occurrence where <paramref name="predicator"/> gives true for all elements in <paramref name="span"/>
		/// </summary>
		/// <param name="span">The span to find in</param>
		/// <param name="predicator">The predicator to check occurrence</param>
		/// <returns>The index of the last occurrence where <paramref name="predicator"/> gives true or -1 if not found</returns>
		/// <remarks>extend method of <paramref name="span"/></remarks>
		public static int LastIndexOf<T>(this ReadOnlySpan<T> span, Predicate<T> predicator)
		{
			for (int i = span.Length - 1; i >= 0; i--)
			{
				if (predicator(span[i]))
					return i;
			}
			return -1;
		}
		#endregion

		#region predicate
		/// <summary>
		/// Check if all elements of <paramref name="span"/> <c>e</c>, <c><paramref name="predicator"/>(e) == true</c>
		/// </summary>
		/// <param name="span">The span to predicate</param>
		/// <param name="predicator">The predicator delegate</param>
		/// <returns>Predicate result</returns>
		/// <remarks>extend method of <paramref name="span"/></remarks>
		public static bool All<T>(this Span<T> span, Predicate<T> predicator)
		{
			return All((ReadOnlySpan<T>)span, predicator);
		}

		/// <summary>
		/// Check if all elements of <paramref name="span"/> <c>e</c>, <c><paramref name="predicator"/>(e) == true</c>
		/// </summary>
		/// <param name="span">The span to predicate</param>
		/// <param name="predicator">The predicator delegate</param>
		/// <returns>Predicate result</returns>
		/// <remarks>extend method of <paramref name="span"/></remarks>
		public static bool All<T>(this ReadOnlySpan<T> span, Predicate<T> predicator)
		{
			for (int i = 0; i < span.Length; i++)
			{
				if (!predicator(span[i]))
					return false;
			}
			return true;
		}

		/// <summary>
		/// Check if any element of <paramref name="span"/> <c>e</c>, <c><paramref name="predicator"/>(e) == true</c>
		/// </summary>
		/// <param name="span">The span to predicate</param>
		/// <param name="predicator">The predicator delegate</param>
		/// <returns>Predicate result</returns>
		/// <remarks>extend method of <paramref name="span"/></remarks>
		public static bool Any<T>(this Span<T> span, Predicate<T> predicator)
		{
			return Any((ReadOnlySpan<T>)span, predicator);
		}

		/// <summary>
		/// Check if any element of <paramref name="span"/> <c>e</c>, <c><paramref name="predicator"/>(e) == true</c>
		/// </summary>
		/// <param name="span">The span to predicate</param>
		/// <param name="predicator">The predicator delegate</param>
		/// <returns>Predicate result</returns>
		/// <remarks>extend method of <paramref name="span"/></remarks>
		public static bool Any<T>(this ReadOnlySpan<T> span, Predicate<T> predicator)
		{
			for (int i = 0; i < span.Length; i++)
			{
				if (predicator(span[i]))
					return true;
			}
			return false;
		}
		#endregion

		#region converter
		/// <summary>
		/// Convert the given <paramref name="span"/> to a new <typeparamref name="TStruct"/> by copying the values byte by byte
		/// </summary>
		/// <typeparam name="T">The data type of span</typeparam>
		/// <typeparam name="TStruct">The output struct type</typeparam>
		/// <param name="span">The <see cref="Span{T}"/> to copy from</param>
		/// <returns>The created <typeparamref name="TStruct"/></returns>
		/// <exception cref="ArgumentException">If the size of <typeparamref name="TStruct"/> is larger than the size of <paramref name="span"/></exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static TStruct ToStruct<T, TStruct>(this Span<T> span) where T : unmanaged where TStruct : struct
		{
			return ToStruct<T, TStruct>((ReadOnlySpan<T>)span);
		}

		/// <summary>
		/// Convert the given <paramref name="span"/> to a new <typeparamref name="TStruct"/> by copying the values byte by byte
		/// </summary>
		/// <typeparam name="T">The data type of span</typeparam>
		/// <typeparam name="TStruct">The output struct type</typeparam>
		/// <param name="span">The <see cref="ReadOnlySpan{T}"/> to copy from</param>
		/// <returns>The created <typeparamref name="TStruct"/></returns>
		/// <exception cref="ArgumentException">If the size of <typeparamref name="TStruct"/> is larger than the size of <paramref name="span"/></exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static TStruct ToStruct<T, TStruct>(this ReadOnlySpan<T> span) where T : unmanaged where TStruct : struct
		{
			span.ToStruct(out TStruct s);
			return s;
		}

		/// <summary>
		/// Convert the given <paramref name="span"/> to a new <typeparamref name="TStruct"/> by copying the values byte by byte
		/// </summary>
		/// <typeparam name="T">The data type of span</typeparam>
		/// <typeparam name="TStruct">The output struct type</typeparam>
		/// <param name="span">The <see cref="ReadOnlySpan{T}"/> to copy from</param>
		/// <param name="struct">The output <typeparamref name="TStruct"/></param>
		/// <exception cref="ArgumentException">If the size of <typeparamref name="TStruct"/> is larger than the size of <paramref name="span"/></exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe void ToStruct<T, TStruct>(this ReadOnlySpan<T> span, out TStruct @struct) where T : unmanaged where TStruct : struct
		{
			int size = Unsafe.SizeOf<TStruct>();
			if (size > span.Length)
				throw new ArgumentException(Parameter.WrongSize, nameof(span));
			@struct = default;
			fixed (void* t = &span.Ref())
			{
				Unsafe.CopyBlock(Unsafe.AsPointer(ref @struct), t, (uint)size);
			}
		}

		/// <summary>
		/// Copy the values in <paramref name="struct"/> to <paramref name="span"/> byte by byte
		/// </summary>
		/// <typeparam name="T">The data type of span</typeparam>
		/// <typeparam name="TStruct">The input struct type</typeparam>
		/// <param name="span">The span to copy to</param>
		/// <param name="struct">The structure to copy</param>
		/// <returns>The <paramref name="span"/></returns>
		/// <exception cref="ArgumentException">If the size of <typeparamref name="TStruct"/> is larger than the size of <paramref name="span"/></exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe Span<T> FromStruct<T, TStruct>(this Span<T> span, TStruct @struct) where T : unmanaged where TStruct : struct
		{
			int size = Unsafe.SizeOf<TStruct>();
			if (size > span.Length)
				throw new ArgumentException(Parameter.WrongSize, nameof(span));
			fixed (void* t = &span[0])
			{
				Unsafe.CopyBlock(t, Unsafe.AsPointer(ref @struct), (uint)size);
			}
			return span;
		}

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
				throw new ArgumentException(Parameter.WrongSize, nameof(span));
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
				throw new ArgumentException(Parameter.WrongSize, nameof(span));
			span[0] = value1; span[1] = value2; span[2] = value3;
			return span;
		}

		/// <summary>
		/// Returns a reference to the element of the span at index 0.
		/// </summary>
		/// <typeparam name="T">The type of items in the span.</typeparam>
		/// <param name="span">The <see cref="Span{T}"/> from which the reference is retrieved.</param>
		/// <returns>A reference to the element at index 0.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T Ref<T>(this Span<T> span)
		{
			return ref MemoryMarshal.GetReference(span);
		}

		/// <summary>
		/// Returns a reference to the element of the span at index 0.
		/// </summary>
		/// <typeparam name="T">The type of items in the span.</typeparam>
		/// <param name="span">The <see cref="ReadOnlySpan{T}"/> from which the reference is retrieved.</param>
		/// <returns>A reference to the element at index 0.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T Ref<T>(this ReadOnlySpan<T> span)
		{
			return ref MemoryMarshal.GetReference(span);
		}

		/// <summary>
		/// Cast the given <paramref name="span"/> from <typeparamref name="TFrom"/> to <typeparamref name="TTo"/> without checking by directly view the underlying memory in a different way, i.e., the <see cref="ReadOnlySpan{T}.Length"/> will change accordingly.
		/// </summary>
		/// <typeparam name="TFrom">conversion from type, must be a struct</typeparam>
		/// <typeparam name="TTo">conversion to type, must be a struct</typeparam>
		/// <param name="span">The <see cref="ReadOnlySpan{TFrom}"/> to be converted</param>
		/// <returns>The converted <see cref="ReadOnlySpan{TTo}"/> with changed <see cref="ReadOnlySpan{T}.Length"/> if <typeparamref name="TTo"/> is not <typeparamref name="TFrom"/></returns>
		/// <exception cref="ArgumentException">If <c><paramref name="span"/>.<see cref="ReadOnlySpan{T}.Length">Length</see> * <typeparamref name="TFrom"/> / <typeparamref name="TTo"/></c> is not an integer</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static ReadOnlySpan<TTo> UncheckAs<TFrom, TTo>(this ReadOnlySpan<TFrom> span) where TFrom : unmanaged where TTo : unmanaged
		{
			if (sizeof(TTo) == sizeof(TFrom))
			{
				return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<TFrom, TTo>(ref span.Ref()), span.Length);
			}
			long size = (long)span.Length * sizeof(TFrom);
			if (size % sizeof(TTo) != 0)
				throw new ArgumentException(Other.CannotDivide);
			return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<TFrom, TTo>(ref span.Ref()), (int)(size / sizeof(TTo)));
		}

		/// <summary>
		/// Cast the given <paramref name="span"/> from <typeparamref name="TFrom"/> to <typeparamref name="TTo"/> without checking by directly view the underlying memory in a different way, i.e., the <see cref="Span{T}.Length"/> will change accordingly.
		/// </summary>
		/// <typeparam name="TFrom">conversion from type, must be a struct</typeparam>
		/// <typeparam name="TTo">conversion to type, must be a struct</typeparam>
		/// <param name="span">The <see cref="Span{TFrom}"/> to be converted</param>
		/// <returns>The converted <see cref="Span{TTo}"/> with changed <see cref="Span{T}.Length"/> if <typeparamref name="TTo"/> is not <typeparamref name="TFrom"/></returns>
		/// <exception cref="ArgumentException">If <c><paramref name="span"/>.<see cref="ReadOnlySpan{T}.Length">Length</see> * <typeparamref name="TFrom"/> / <typeparamref name="TTo"/></c> is not an integer</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static Span<TTo> UncheckAs<TFrom, TTo>(this Span<TFrom> span) where TFrom : unmanaged where TTo : unmanaged
		{
			if (sizeof(TTo) == sizeof(TFrom))
			{
				return MemoryMarshal.CreateSpan(ref Unsafe.As<TFrom, TTo>(ref span.Ref()), span.Length);
			}
			long size = (long)span.Length * sizeof(TFrom);
			if (size % sizeof(TTo) != 0)
				throw new ArgumentException(Other.CannotDivide);
			return MemoryMarshal.CreateSpan(ref Unsafe.As<TFrom, TTo>(ref span.Ref()), (int)(size / sizeof(TTo)));
		}

		/// <summary>
		/// Cast the given <paramref name="span"/> from <typeparamref name="TFrom"/> to <typeparamref name="TTo"/> by directly view the underlying memory in a different way, i.e., the <see cref="ReadOnlySpan{T}.Length"/> will change accordingly.
		/// </summary>
		/// <typeparam name="TFrom">conversion from type, must be a struct</typeparam>
		/// <typeparam name="TTo">conversion to type, must be a struct</typeparam>
		/// <param name="span">The <see cref="ReadOnlySpan{TFrom}"/> to be converted</param>
		/// <returns>The converted <see cref="ReadOnlySpan{TTo}"/> with changed <see cref="ReadOnlySpan{T}.Length"/> if <typeparamref name="TTo"/> is not <typeparamref name="TFrom"/></returns>
		/// <exception cref="ArgumentException">If <typeparamref name="TFrom"/> or <typeparamref name="TTo"/> contains references or pointers.</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ReadOnlySpan<TTo> As<TFrom, TTo>(this ReadOnlySpan<TFrom> span) where TFrom : struct where TTo : struct
		{
			return MemoryMarshal.Cast<TFrom, TTo>(span);
		}

		/// <summary>
		/// Cast the given <paramref name="span"/> from <typeparamref name="TFrom"/> to <typeparamref name="TTo"/> by directly view the underlying memory in a different way, i.e., the <see cref="Span{T}.Length"/> will change accordingly.
		/// </summary>
		/// <typeparam name="TFrom">conversion from type, must be a struct</typeparam>
		/// <typeparam name="TTo">conversion to type, must be a struct</typeparam>
		/// <param name="span">The <see cref="Span{TFrom}"/> to be converted</param>
		/// <returns>The converted <see cref="Span{TTo}"/> with changed <see cref="Span{T}.Length"/> if <typeparamref name="TTo"/> is not <typeparamref name="TFrom"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Span<TTo> As<TFrom, TTo>(this Span<TFrom> span) where TFrom : struct where TTo : struct
		{
			return MemoryMarshal.Cast<TFrom, TTo>(span);
		}

		/// <summary>
		/// Get the hash code of an span using CRC method
		/// </summary>
		/// <typeparam name="T">any struct</typeparam>
		/// <param name="span">span to get hash code</param>
		/// <returns>the hash code of <paramref name="span"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int HashCodeOfSpan<T>(this Span<T> span) where T : struct
		{
			return HashCodeOfSpan((ReadOnlySpan<T>)span);
		}

		/// <summary>
		/// Get the hash code of an span using CRC method
		/// </summary>
		/// <typeparam name="T">any struct</typeparam>
		/// <param name="span">span to get hash code</param>
		/// <returns>the hash code of <paramref name="span"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int HashCodeOfSpan<T>(this ReadOnlySpan<T> span) where T : struct
		{
			if (span.IsEmpty)
				return 0; // hash code of empty
			int hc = span.Length;
			for (int i = 0; i < span.Length; ++i)
			{
				hc = unchecked(hc * ArrayLinq.CRC_CONST + span[i].GetHashCode()); // CRC
			}
			return hc;
		}
		#endregion

		#region set operations
		/// <summary>
		/// Check if <paramref name="span"/>'s elements are unique
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="span">span to pick</param>
		/// <returns><paramref name="span"/>'s elements are unique or not</returns>
		public static bool ElementsUnique<T>(this Span<T> span) where T : IEquatable<T>
		{
			return ElementsUnique((ReadOnlySpan<T>)span);
		}

		/// <summary>
		/// Check if <paramref name="span"/>'s elements are unique
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="span">span to pick</param>
		/// <returns><paramref name="span"/>'s elements are unique or not</returns>
		public static bool ElementsUnique<T>(this ReadOnlySpan<T> span) where T : IEquatable<T>
		{
			var res = new List<T>(span.Length);
			for (int i = 0; i < span.Length; i++)
			{
				if (!res.Contains(span[i]))
					res.Add(span[i]);
				else
					return false;
			}
			return true;
		}

		/// <summary>
		/// Get the hash code of a set (order-independent) using ADD method
		/// </summary>
		/// <typeparam name="T">any struct</typeparam>
		/// <param name="set">set to get hash code</param>
		/// <returns>the hash code of <paramref name="set"/></returns>
		public static int HashCodeOfSet<T>(this Span<T> set) where T : struct
		{
			return HashCodeOfSet((ReadOnlySpan<T>)set);
		}

		/// <summary>
		/// Get the hash code of a set (order-independent) using ADD method
		/// </summary>
		/// <typeparam name="T">any struct</typeparam>
		/// <param name="set">set to get hash code</param>
		/// <returns>the hash code of <paramref name="set"/></returns>
		public static int HashCodeOfSet<T>(this ReadOnlySpan<T> set) where T : struct
		{
			if (set.IsEmpty)
				return 0; // hash code of null
			int hc = 0;
			for (int i = 0; i < set.Length; ++i)
			{
				hc = unchecked(hc + set[i].GetHashCode());
			}
			return hc;
		}

		/// <summary>
		/// Count the distinct element(s) in <paramref name="list"/>
		/// </summary>
		/// <param name="list">list to pick</param>
		/// <returns>the number of distinct element(s) <see cref="IReadOnlyList{T}"/></returns>
		public static int DistinctCount<T>(this Span<T> list) where T : unmanaged, IEquatable<T>
		{
			return DistinctCount((ReadOnlySpan<T>)list);
		}

		/// <summary>
		/// Count the distinct element(s) in <paramref name="list"/>
		/// </summary>
		/// <param name="list">list to pick</param>
		/// <returns>the number of distinct element(s) <see cref="IReadOnlyList{T}"/></returns>
		public static int DistinctCount<T>(this ReadOnlySpan<T> list) where T : unmanaged, IEquatable<T>
		{
			if (list.Length <= 1)
				return list.Length;
			Span<T> temp = stackalloc T[list.Length];
			Span<T> slice = temp.Slice(0, 0);
			int now = 0;
			for (int i = 0; i < list.Length; i++)
			{
				if (!slice.Contains(list[i]))
				{
					temp[now++] = list[i];
					slice = temp.Slice(0, now);
				}
			}
			return now;
		}


		/// <summary>
		/// Compute the complement set of <paramref name="span"/> compared to <paramref name="fullSet"/> and store the result in <paramref name="complement"/>
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="span">The set whose complement set will be obtained</param>
		/// <param name="fullSet">The full set</param>
		/// <param name="complement">The output complement set</param>
		/// <exception cref="ArgumentException">If <paramref name="span"/> or <paramref name="fullSet"/> is not a set; or the lengths are incompatible</exception>
		public static void ComplementSet<T>(this Span<T> span, ReadOnlySpan<T> fullSet, Span<T> complement) where T : IEquatable<T>
		{
			ComplementSet((ReadOnlySpan<T>)span, fullSet, complement);
		}

		/// <summary>
		/// Compute the complement set of <paramref name="span"/> compared to <paramref name="fullSet"/> and store the result in <paramref name="complement"/>
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="span">The set whose complement set will be obtained</param>
		/// <param name="fullSet">The full set</param>
		/// <param name="complement">The output complement set</param>
		/// <exception cref="ArgumentException">If <paramref name="span"/> or <paramref name="fullSet"/> is not a set; or the lengths are incompatible</exception>
		public static void ComplementSet<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> fullSet, Span<T> complement) where T : IEquatable<T>
		{
			if (fullSet.Length < span.Length)
				throw new ArgumentException(Parameter.WrongSize, nameof(fullSet));
			if (fullSet.Length == span.Length)
				return;
			if (complement.Length != fullSet.Length - span.Length)
				throw new ArgumentException(Parameter.WrongSize, nameof(complement));
			// shortcut
			if (span.IsEmpty)
			{
				fullSet.CopyTo(complement);
			}
			// else
			int now = 0;
			for (int i = 0; i < fullSet.Length; i++)
			{
				if (!span.Contains(fullSet[i]))
				{
					complement[now++] = fullSet[i];
				}
			}
			if (now != complement.Length)
				throw new ArgumentException(Parameter.DuplicateValue);
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
		public static Span<T> FillWithRange<T>(this Span<T> span, T start, T step = default) where T : unmanaged
		{
			if (span.IsEmpty)
				throw new ArgumentNullException(nameof(span));
			if (step.IsZero())
				step = Const<T>.One;

			span[0] = start;
			for (int i = 1; i < span.Length; i++)
			{
				span[i] = Const<T>.AddDelegate(span[i - 1], step);
			}
			return span;
		}
		#endregion
	}
}
