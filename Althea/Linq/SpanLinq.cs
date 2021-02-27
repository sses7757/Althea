using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using Althea.Resources;


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
		/// Copy <paramref name="span"/> to <paramref name="destArray"/>.
		/// </summary>
		/// <typeparam name="T">The type of <paramref name="span"/> and <paramref name="destArray"/></typeparam>
		/// <param name="span">The <see cref="ReadOnlySpan{T}"/> to be copied</param>
		/// <param name="destArray">The destination array to be copied into</param>
		/// <returns>The <paramref name="destArray"/></returns>
		public static T[] CopyTo<T>(this ReadOnlySpan<T> span, T[] destArray)
		{
			if (span.Length != destArray.Length)
				throw new ArgumentException(Parameter.NotSameSize);

			for (int i = 0; i < span.Length; i++)
			{
				destArray[i] = span[i];
			}
			return destArray;
		}

		/// <summary>
		/// Copy <paramref name="span"/> to <paramref name="array"/>.
		/// </summary>
		/// <typeparam name="TIn">The type of <paramref name="span"/></typeparam>
		/// <typeparam name="TOut">The type of <paramref name="array"/></typeparam>
		/// <param name="span">The <see cref="Span{T}"/> to be copied from</param>
		/// <param name="array">The destination array to be copied into</param>
		/// <param name="selector">The converter to each element</param>
		/// <returns>The <paramref name="array"/></returns>
		public static TOut[] CopyTo<TIn, TOut>(this ReadOnlySpan<TIn> span, TOut[] array, Converter<TIn, TOut> selector)
		{
			if (span.Length != array.Length)
				throw new ArgumentException(Parameter.NotSameSize);

			for (int i = 0; i < span.Length; i++)
			{
				array[i] = selector(span[i]);
			}
			return array;
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
			if (span.Length != array.Length)
				throw new ArgumentException(Parameter.NotSameSize);

			for (int i = 0; i < span.Length; i++)
			{
				array[i] = selector(span[i]);
			}
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
			T minVal = list[0];
			for (int i = 0; i < list.Length; i++)
			{
				T val = list[i];
				if (val.CompareTo(minVal) > 0)
					minVal = val;
			}
			return minVal;
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
		/// Re-order the <paramref name="array"/> by <paramref name="indices"/> out-of-place such that <c><paramref name="array"/>[indices] = result</c>.
		/// </summary>
		/// <typeparam name="T">any type</typeparam>
		/// <param name="array">The array to order</param>
		/// <param name="indices">The indices to order, if this is null or empty, <paramref name="array"/> will be returned</param>
		/// <returns>the re-ordered array</returns>
		public static T[] ReOrder<T>(this IReadOnlyList<T> array, ReadOnlySpan<int> indices)
		{
			if (array is null)
				throw new ArgumentNullException(nameof(array));
			if (indices.Length == 0)
				return array.ToArray();

			var output = new T[indices.Length];
			for (int i = 0; i < indices.Length; i++)
			{
				output[i] = array[indices[i]];
			}
			return output;
		}

		/// <summary>
		/// Re-order the <paramref name="array"/> by <paramref name="indices"/> out-of-place such that <c><paramref name="array"/>[indices] = <paramref name="target"/></c>.
		/// </summary>
		/// <typeparam name="T">any type</typeparam>
		/// <param name="array">The array to order</param>
		/// <param name="target">The array to put the reordered result</param>
		/// <param name="indices">The indices to order, if this is null or empty, <paramref name="array"/> will be returned</param>
		/// <returns>the re-ordered array</returns>
		public static void ReOrderTo<T>(this IReadOnlyList<T> array, Span<T> target, int[] indices)
		{
			if (array is null)
				throw new ArgumentNullException(nameof(array));

			if (indices is null || indices.Length == 0)
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
		/// Re-order the <paramref name="array"/> by <paramref name="indices"/> out-of-place such that <c><paramref name="array"/>[indices] = <paramref name="target"/></c>.
		/// </summary>
		/// <typeparam name="T">any type</typeparam>
		/// <param name="array">The array to order</param>
		/// <param name="target">The array to put the reordered result</param>
		/// <param name="indices">The indices to order, if this is null or empty, <paramref name="array"/> will be returned</param>
		/// <returns>the re-ordered array</returns>
		public static void ReOrderTo<T>(this IReadOnlyList<T> array, Span<T> target, Span<int> indices)
		{
			if (array is null)
				throw new ArgumentNullException(nameof(array));

			if (indices.Length == 0)
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
		/// Re-order the <paramref name="array"/> by <paramref name="indices"/> out-of-place such that <c><paramref name="array"/>[indices] = <paramref name="target"/></c>.
		/// </summary>
		/// <typeparam name="T">any type</typeparam>
		/// <param name="array">The array to order</param>
		/// <param name="target">The array to put the reordered result</param>
		/// <param name="indices">The indices to order, if this is null or empty, <paramref name="array"/> will be returned</param>
		/// <returns>the re-ordered array</returns>
		public static void ReOrderTo<T>(this Span<T> array, Span<T> target, int[] indices)
		{
			if (indices is null || indices.Length == 0)
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
		/// Re-order the <paramref name="array"/> by <paramref name="indices"/> out-of-place such that <c><paramref name="array"/>[indices] = <paramref name="target"/></c>.
		/// </summary>
		/// <typeparam name="T">any type</typeparam>
		/// <param name="array">The array to order</param>
		/// <param name="target">The array to put the reordered result</param>
		/// <param name="indices">The indices to order, if this is null or empty, <paramref name="array"/> will be returned</param>
		/// <returns>the re-ordered array</returns>
		public static void ReOrderTo<T>(this Span<T> array, Span<T> target, ReadOnlySpan<int> indices)
		{
			if (indices.Length == 0)
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
		/// Re-order the <paramref name="array"/> by <paramref name="indices"/> out-of-place such that <c><paramref name="array"/>[indices] = <paramref name="target"/></c>.
		/// </summary>
		/// <typeparam name="T">any type</typeparam>
		/// <param name="array">The array to order</param>
		/// <param name="target">The array to put the reordered result</param>
		/// <param name="indices">The indices to order, if this is null or empty, <paramref name="array"/> will be returned</param>
		/// <returns>the re-ordered array</returns>
		public static void ReOrderTo<T>(this ReadOnlySpan<T> array, Span<T> target, int[] indices)
		{
			if (indices is null || indices.Length == 0)
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
		/// Re-order the <paramref name="array"/> by <paramref name="indices"/> out-of-place such that <c><paramref name="array"/>[indices] = <paramref name="target"/></c>.
		/// </summary>
		/// <typeparam name="T">any type</typeparam>
		/// <param name="array">The array to order</param>
		/// <param name="target">The array to put the reordered result</param>
		/// <param name="indices">The indices to order, if this is null or empty, <paramref name="array"/> will be returned</param>
		/// <returns>the re-ordered array</returns>
		public static void ReOrderTo<T>(this Span<T> array, T[] target, ReadOnlySpan<int> indices)
		{
			if (indices.Length == 0)
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
		public static void InverseOrderTo<T>(this Span<T> array, Span<T> target, int[] indices)
		{
			if (indices is null || indices.Length == 0)
				throw new ArgumentNullException(nameof(indices));
			if (indices.Length > array.Length || indices.Length > target.Length)
				throw new ArgumentException(Parameter.WrongSize, nameof(indices));

			for (int i = 0; i < indices.Length; i++)
			{
				target[indices[i]] = array[i];
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
			if (indices.Length == 0)
				throw new ArgumentNullException(nameof(indices));
			if (indices.Length > array.Length || indices.Length > target.Length)
				throw new ArgumentException(Parameter.WrongSize, nameof(indices));

			for (int i = 0; i < indices.Length; i++)
			{
				target[indices[i]] = array[i];
			}
		}

		/// <summary>
		/// Inverse order the <paramref name="array"/> by <paramref name="indices"/> out-of-place such that <c><paramref name="target"/>[<paramref name="indices"/>] = <paramref name="array"/></c>.
		/// </summary>
		/// <typeparam name="T">any type</typeparam>
		/// <param name="array">The array to order</param>
		/// <param name="target">The array to put the reordered result</param>
		/// <param name="indices">The indices to order. May has less elements than <paramref name="array"/>. If so, the rest elements in <paramref name="target"/> remains unchanged.</param>
		public static void InverseOrderTo<T>(this IReadOnlyList<T> array, Span<T> target, IReadOnlyList<int> indices)
		{
			if (array is null || array.Count == 0)
				throw new ArgumentNullException(nameof(array));
			if (indices is null || indices.Count == 0)
				throw new ArgumentNullException(nameof(indices));
			if (indices.Count > array.Count || indices.Count > target.Length)
				throw new ArgumentException(Parameter.WrongSize, nameof(indices));

			for (int i = 0; i < indices.Count; i++)
			{
				target[indices[i]] = array[i];
			}
		}

		/// <summary>
		/// Inverse order the <paramref name="array"/> by <paramref name="indices"/> out-of-place such that <c><paramref name="target"/>[<paramref name="indices"/>] = <paramref name="array"/></c>.
		/// </summary>
		/// <typeparam name="T">any type</typeparam>
		/// <param name="array">The array to order</param>
		/// <param name="target">The array to put the reordered result</param>
		/// <param name="indices">The indices to order. May has less elements than <paramref name="array"/>. If so, the rest elements in <paramref name="target"/> remains unchanged.</param>
		public static void InverseOrderTo<T>(this IReadOnlyList<T> array, Span<T> target, ReadOnlySpan<int> indices)
		{
			if (array is null || array.Count == 0)
				throw new ArgumentNullException(nameof(array));
			if (indices.Length == 0)
				throw new ArgumentNullException(nameof(indices));
			if (indices.Length > array.Count || indices.Length > target.Length)
				throw new ArgumentException(Parameter.WrongSize, nameof(indices));

			for (int i = 0; i < indices.Length; i++)
			{
				target[indices[i]] = array[i];
			}
		}

		/// <summary>
		/// Inverse order the <paramref name="array"/> by <paramref name="indices"/> out-of-place such that <c><paramref name="target"/>[<paramref name="indices"/>] = <paramref name="array"/></c>.
		/// </summary>
		/// <typeparam name="T">any type</typeparam>
		/// <param name="array">The array to order</param>
		/// <param name="target">The array to put the reordered result</param>
		/// <param name="indices">The indices to order. May has less elements than <paramref name="array"/>. If so, the rest elements in <paramref name="target"/> remains unchanged.</param>
		public static void InverseOrderTo<T>(this Span<T> array, T[] target, ReadOnlySpan<int> indices)
		{
			if (array.Length == 0)
				throw new ArgumentNullException(nameof(array));
			if (indices.Length == 0)
				throw new ArgumentNullException(nameof(indices));
			if (indices.Length > array.Length || indices.Length > target.Length)
				throw new ArgumentException(Parameter.WrongSize, nameof(indices));

			for (int i = 0; i < indices.Length; i++)
			{
				target[indices[i]] = array[i];
			}
		}

		/// <summary>
		/// Find the permutation order such that <c><paramref name="array"/>[result] = <paramref name="target"/></c>
		/// </summary>
		/// <typeparam name="T">any type</typeparam>
		/// <param name="array">The array before permutation</param>
		/// <param name="target">The array after the permutation</param>
		/// <param name="perm">The result permutation order to put in as a <see cref="Span{T}"/>, may be overwritten by undesired values if there is no such permutation</param>
		/// <returns>success or not</returns>
		public static bool FindPermutationTo<T>(this IReadOnlyList<T> array, IReadOnlyList<T> target, Span<int> perm)
		{
			if (array is null)
				throw new ArgumentNullException(nameof(array));
			if (target is null || target.Count == 0)
				throw new ArgumentNullException(nameof(target));

			var arr = array.ToArray();
			for (int i = 0; i < target.Count; i++)
			{
				perm[i] = Array.IndexOf(arr, target[i]);
				if (perm[i] < 0)
					return false;
			}
			return true;
		}

		/// <summary>
		/// Find the inverse permutation of <paramref name="perm"/> such that <c>perm[result] == result[perm] == identity permutation</c>
		/// </summary>
		/// <param name="perm">The input permutation</param>
		/// <param name="inv">The inverse permutation to put in</param>
		public static void InversePermutationTo(this IReadOnlyList<int> perm, Span<int> inv)
		{
			if (perm is null || perm.Count > inv.Length)
				throw new ArgumentNullException(nameof(perm));

			for (int i = 0; i < perm.Count; i++)
			{
				inv[perm[i]] = i;
			}
		}
		#endregion

		#region aggregate
		#region concrete prod of Span
		/// <summary>
		/// List product
		/// </summary>
		/// <param name="list"></param>
		/// <returns>Product result, 1 if <paramref name="list"/> is null</returns>
		/// <remarks>extend method of <paramref name="list"/></remarks>
		public static int Prod(this Span<int> list)
		{
			if (list.Length == 0)
				return 1;
			int prod = 1;
			for (int i = 0; i < list.Length; i++)
			{
				prod *= list[i];
			}
			return prod;
		}

		/// <summary>
		/// List product
		/// </summary>
		/// <param name="list"></param>
		/// <returns>Product result, 1 if <paramref name="list"/> is null</returns>
		/// <remarks>extend method of <paramref name="list"/></remarks>
		public static long Prod(this Span<long> list)
		{
			if (list.Length == 0)
				return 1;
			long prod = 1;
			for (int i = 0; i < list.Length; i++)
			{
				prod *= list[i];
			}
			return prod;
		}

		/// <summary>
		/// List product
		/// </summary>
		/// <param name="list"></param>
		/// <returns>Product result, 1 if <paramref name="list"/> is null</returns>
		/// <remarks>extend method of <paramref name="list"/></remarks>
		public static float Prod(this Span<float> list)
		{
			if (list.Length == 0)
				return 1;
			float prod = 1;
			for (int i = 0; i < list.Length; i++)
			{
				prod *= list[i];
			}
			return prod;
		}

		/// <summary>
		/// List product
		/// </summary>
		/// <param name="list"></param>
		/// <returns>Product result, 1 if <paramref name="list"/> is null</returns>
		/// <remarks>extend method of <paramref name="list"/></remarks>
		public static double Prod(this Span<double> list)
		{
			if (list.Length == 0)
				return 1;
			double prod = 1;
			for (int i = 0; i < list.Length; i++)
			{
				prod *= list[i];
			}
			return prod;
		}
		#endregion

		#region concrete sum of Span
		/// <summary>
		/// List summation
		/// </summary>
		/// <param name="list"></param>
		/// <returns>Summation result, 0 if <paramref name="list"/> is null</returns>
		/// <remarks>extend method of <paramref name="list"/></remarks>
		public static int Sum(this Span<int> list)
		{
			if (list.Length == 0)
				return 0;
			int sum = 0;
			for (int i = 0; i < list.Length; i++)
			{
				sum += list[i];
			}
			return sum;
		}

		/// <summary>
		/// List summation
		/// </summary>
		/// <param name="list"></param>
		/// <returns>Summation result, 0 if <paramref name="list"/> is null</returns>
		/// <remarks>extend method of <paramref name="list"/></remarks>
		public static long Sum(this Span<long> list)
		{
			if (list.Length == 0)
				return 0;
			long sum = 0;
			for (int i = 0; i < list.Length; i++)
			{
				sum += list[i];
			}
			return sum;
		}

		/// <summary>
		/// List summation
		/// </summary>
		/// <param name="list"></param>
		/// <returns>Summation result, 0 if <paramref name="list"/> is null</returns>
		/// <remarks>extend method of <paramref name="list"/></remarks>
		public static float Sum(this Span<float> list)
		{
			if (list.Length == 0)
				return 0;
			float sum = 0;
			for (int i = 0; i < list.Length; i++)
			{
				sum += list[i];
			}
			return sum;
		}

		/// <summary>
		/// List summation
		/// </summary>
		/// <param name="list"></param>
		/// <returns>Summation result, 0 if <paramref name="list"/> is null</returns>
		/// <remarks>extend method of <paramref name="list"/></remarks>
		public static double Sum(this Span<double> list)
		{
			if (list.Length == 0)
				return 0;
			double sum = 0;
			for (int i = 0; i < list.Length; i++)
			{
				sum += list[i];
			}
			return sum;
		}
		#endregion

		#region concrete prod of ReadOnlySpan
		/// <summary>
		/// List product
		/// </summary>
		/// <param name="list"></param>
		/// <returns>Product result, 1 if <paramref name="list"/> is null</returns>
		/// <remarks>extend method of <paramref name="list"/></remarks>
		public static int Prod(this ReadOnlySpan<int> list)
		{
			if (list.Length == 0)
				return 1;
			int prod = 1;
			for (int i = 0; i < list.Length; i++)
			{
				prod *= list[i];
			}
			return prod;
		}

		/// <summary>
		/// List product
		/// </summary>
		/// <param name="list"></param>
		/// <returns>Product result, 1 if <paramref name="list"/> is null</returns>
		/// <remarks>extend method of <paramref name="list"/></remarks>
		public static long Prod(this ReadOnlySpan<long> list)
		{
			if (list.Length == 0)
				return 1;
			long prod = 1;
			for (int i = 0; i < list.Length; i++)
			{
				prod *= list[i];
			}
			return prod;
		}

		/// <summary>
		/// List product
		/// </summary>
		/// <param name="list"></param>
		/// <returns>Product result, 1 if <paramref name="list"/> is null</returns>
		/// <remarks>extend method of <paramref name="list"/></remarks>
		public static float Prod(this ReadOnlySpan<float> list)
		{
			if (list.Length == 0)
				return 1;
			float prod = 1;
			for (int i = 0; i < list.Length; i++)
			{
				prod *= list[i];
			}
			return prod;
		}

		/// <summary>
		/// List product
		/// </summary>
		/// <param name="list"></param>
		/// <returns>Product result, 1 if <paramref name="list"/> is null</returns>
		/// <remarks>extend method of <paramref name="list"/></remarks>
		public static double Prod(this ReadOnlySpan<double> list)
		{
			if (list.Length == 0)
				return 1;
			double prod = 1;
			for (int i = 0; i < list.Length; i++)
			{
				prod *= list[i];
			}
			return prod;
		}
		#endregion

		#region concrete sum of ReadOnlySpan
		/// <summary>
		/// List summation
		/// </summary>
		/// <param name="list"></param>
		/// <returns>Summation result, 0 if <paramref name="list"/> is null</returns>
		/// <remarks>extend method of <paramref name="list"/></remarks>
		public static int Sum(this ReadOnlySpan<int> list)
		{
			if (list.Length == 0)
				return 0;
			int sum = 0;
			for (int i = 0; i < list.Length; i++)
			{
				sum += list[i];
			}
			return sum;
		}

		/// <summary>
		/// List summation
		/// </summary>
		/// <param name="list"></param>
		/// <returns>Summation result, 0 if <paramref name="list"/> is null</returns>
		/// <remarks>extend method of <paramref name="list"/></remarks>
		public static long Sum(this ReadOnlySpan<long> list)
		{
			if (list.Length == 0)
				return 0;
			long sum = 0;
			for (int i = 0; i < list.Length; i++)
			{
				sum += list[i];
			}
			return sum;
		}

		/// <summary>
		/// List summation
		/// </summary>
		/// <param name="list"></param>
		/// <returns>Summation result, 0 if <paramref name="list"/> is null</returns>
		/// <remarks>extend method of <paramref name="list"/></remarks>
		public static float Sum(this ReadOnlySpan<float> list)
		{
			if (list.Length == 0)
				return 0;
			float sum = 0;
			for (int i = 0; i < list.Length; i++)
			{
				sum += list[i];
			}
			return sum;
		}

		/// <summary>
		/// List summation
		/// </summary>
		/// <param name="list"></param>
		/// <returns>Summation result, 0 if <paramref name="list"/> is null</returns>
		/// <remarks>extend method of <paramref name="list"/></remarks>
		public static double Sum(this ReadOnlySpan<double> list)
		{
			if (list.Length == 0)
				return 0;
			double sum = 0;
			for (int i = 0; i < list.Length; i++)
			{
				sum += list[i];
			}
			return sum;
		}
		#endregion
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
			for (int i = 0; i < span.Length; i++)
			{
				if (predicator(span[i]))
					return i;
			}
			return -1;
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
			for (int i = span.Length - 1; i >= 0; i--)
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
			for (int i = 0; i < span.Length; i++)
			{
				if (!predicator(span[i]))
					return false;
			}
			return true;
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
			for (int i = 0; i < span.Length; i++)
			{
				if (predicator(span[i]))
					return true;
			}
			return false;
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

		/// <summary>
		/// Check if <paramref name="list"/>'s all elements are sequentially equal to <paramref name="other"/>'s
		/// </summary>
		/// <typeparam name="T">data type</typeparam>
		/// <param name="list">The list to compare</param>
		/// <param name="other">The other list to compare</param>
		/// <returns>Sequentially equals or not</returns>
		public static bool SequenceEqual<T>(this Span<T> list, IReadOnlyList<T> other) where T : IEquatable<T>
		{
			if (other is null)
				return true;
			if (list.Length != other.Count)
				return false;
			for (int i = 0; i < list.Length; i++)
			{
				if (!list[i].Equals(other[i]))
					return false;
			}
			return true;
		}
		#endregion

		#region converter
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
		/// <exception cref="ArgumentNullException">If <paramref name="span"/> is empty</exception>
		/// <exception cref="ArgumentException">If <paramref name="span"/> has length smaller than 2</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Span<T> SetValue<T>(this Span<T> span, T value1, T value2)
		{
			if (span.IsEmpty)
				throw new ArgumentNullException(nameof(span));
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
		/// <exception cref="ArgumentNullException">If <paramref name="span"/> is empty</exception>
		/// <exception cref="ArgumentException">If <paramref name="span"/> has length smaller than 2</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Span<T> SetValue<T>(this Span<T> span, T value1, T value2, T value3)
		{
			if (span.IsEmpty)
				throw new ArgumentNullException(nameof(span));
			if (span.Length < 2)
				throw new ArgumentException(Parameter.WrongSize, nameof(span));
			span[0] = value1; span[1] = value2; span[2] = value3;
			return span;
		}

		/// <summary>
		/// Cast the given <paramref name="span"/> from <typeparamref name="TFrom"/> to <typeparamref name="TTo"/> without checking by directly view the underlying memory in a different way, i.e., the <see cref="ReadOnlySpan{T}.Length"/> will change accordingly.
		/// </summary>
		/// <typeparam name="TFrom">conversion from type, must be a struct</typeparam>
		/// <typeparam name="TTo">conversion to type, must be a struct</typeparam>
		/// <param name="span">The <see cref="ReadOnlySpan{TFrom}"/> to be converted</param>
		/// <returns>The converted <see cref="ReadOnlySpan{TTo}"/> with changed <see cref="ReadOnlySpan{T}.Length"/> if <typeparamref name="TTo"/> is not <typeparamref name="TFrom"/></returns>
		/// <exception cref="ArgumentException">If <c><paramref name="span"/>.<see cref="ReadOnlySpan{T}.Length">Length</see> * <typeparamref name="TFrom"/> / <typeparamref name="TTo"/></c> is not an integer</exception>
		public unsafe static ReadOnlySpan<TTo> UncheckAs<TFrom, TTo>(this ReadOnlySpan<TFrom> span) where TFrom : unmanaged where TTo : unmanaged
		{
			if (sizeof(TTo) == sizeof(TFrom))
			{
				return new ReadOnlySpan<TTo>(Unsafe.AsPointer(ref MemoryMarshal.GetReference(span)), span.Length);
			}
			long size = (long)span.Length * sizeof(TFrom);
			if (size % sizeof(TTo) != 0)
				throw new ArgumentException(Other.CannotDivide);
			return new ReadOnlySpan<TTo>(Unsafe.AsPointer(ref MemoryMarshal.GetReference(span)), (int)(size / sizeof(TTo)));
		}

		/// <summary>
		/// Cast the given <paramref name="span"/> from <typeparamref name="TFrom"/> to <typeparamref name="TTo"/> without checking by directly view the underlying memory in a different way, i.e., the <see cref="Span{T}.Length"/> will change accordingly.
		/// </summary>
		/// <typeparam name="TFrom">conversion from type, must be a struct</typeparam>
		/// <typeparam name="TTo">conversion to type, must be a struct</typeparam>
		/// <param name="span">The <see cref="Span{TFrom}"/> to be converted</param>
		/// <returns>The converted <see cref="Span{TTo}"/> with changed <see cref="Span{T}.Length"/> if <typeparamref name="TTo"/> is not <typeparamref name="TFrom"/></returns>
		/// <exception cref="ArgumentException">If <c><paramref name="span"/>.<see cref="ReadOnlySpan{T}.Length">Length</see> * <typeparamref name="TFrom"/> / <typeparamref name="TTo"/></c> is not an integer</exception>
		public unsafe static Span<TTo> UncheckAs<TFrom, TTo>(this Span<TFrom> span) where TFrom : unmanaged where TTo : unmanaged
		{
			if (sizeof(TTo) == sizeof(TFrom))
			{
				return new Span<TTo>(Unsafe.AsPointer(ref MemoryMarshal.GetReference(span)), span.Length);
			}
			long size = (long)span.Length * sizeof(TFrom);
			if (size % sizeof(TTo) != 0)
				throw new ArgumentException(Other.CannotDivide);
			return new Span<TTo>(Unsafe.AsPointer(ref MemoryMarshal.GetReference(span)), (int)(size / sizeof(TTo)));
		}

		/// <summary>
		/// Cast the given <paramref name="span"/> from <typeparamref name="TFrom"/> to <typeparamref name="TTo"/> by directly view the underlying memory in a different way, i.e., the <see cref="ReadOnlySpan{T}.Length"/> will change accordingly.
		/// </summary>
		/// <typeparam name="TFrom">conversion from type, must be a struct</typeparam>
		/// <typeparam name="TTo">conversion to type, must be a struct</typeparam>
		/// <param name="span">The <see cref="ReadOnlySpan{TFrom}"/> to be converted</param>
		/// <returns>The converted <see cref="ReadOnlySpan{TTo}"/> with changed <see cref="ReadOnlySpan{T}.Length"/> if <typeparamref name="TTo"/> is not <typeparamref name="TFrom"/></returns>
		/// <exception cref="ArgumentException">If <typeparamref name="TFrom"/> or <typeparamref name="TTo"/> contains references or pointers.</exception>
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
		public static Span<TTo> As<TFrom, TTo>(this Span<TFrom> span) where TFrom : struct where TTo : struct
		{
			return MemoryMarshal.Cast<TFrom, TTo>(span);
		}

		// Ignore Spelling: stackalloc
		/// <summary>
		/// Cast the given <paramref name="span"/> of <see cref="IntPtr"/> to a <see cref="Span{T}"/> of reference-type <typeparamref name="T"/>
		/// </summary>
		/// <typeparam name="T">The reference type to cast to</typeparam>
		/// <param name="span">The <see cref="Span{T}"/> of <see cref="IntPtr"/> to cast from</param>
		/// <returns>The <see cref="Span{T}"/> of reference-type <typeparamref name="T"/> casted from <paramref name="span"/></returns>
		/// <example>
		/// <code>
		/// Span&lt;IntPtr&gt; span = stackalloc IntPtr[5];<br/>
		/// Span&lt;Some_Class_Type&gt; temp = span.AsReferenceType&lt;Some_Class_Type&gt;();
		/// </code>
		/// </example>
		public static Span<T> AsReferenceType<T>(this Span<IntPtr> span) where T : class
		{
			return MemoryMarshal.CreateSpan(ref Unsafe.As<IntPtr, T>(ref span.Ref()), span.Length);
		}

		/// <summary>
		/// Returns a reference to the element of the span at index 0.
		/// </summary>
		/// <typeparam name="T">The type of items in the span.</typeparam>
		/// <param name="span">The <see cref="Span{T}"/> from which the reference is retrieved.</param>
		/// <returns>A reference to the element at index 0.</returns>
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
		public static ref T Ref<T>(this ReadOnlySpan<T> span)
		{
			return ref MemoryMarshal.GetReference(span);
		}

		/// <summary>
		/// Get the hash code of an span using CRC method
		/// </summary>
		/// <typeparam name="T">any struct</typeparam>
		/// <param name="span">span to get hash code</param>
		/// <returns>the hash code of <paramref name="span"/></returns>
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
		public static int DistinctCount(this Span<int> list)
		{
			if (list.Length <= 1)
				return list.Length;
			Span<int> temp = stackalloc int[list.Length];
			int now = 0;
			for (int i = 0; i < list.Length; i++)
			{
				if (!temp.Slice(0, now).Contains(list[i]))
					temp[now++] = list[i];
			}
			return now;
		}

		/// <summary>
		/// Count the distinct element(s) in <paramref name="list"/>
		/// </summary>
		/// <param name="list">list to pick</param>
		/// <returns>the number of distinct element(s) <see cref="IReadOnlyList{T}"/></returns>
		public static int DistinctCount(this ReadOnlySpan<int> list)
		{
			if (list.Length <= 1)
				return list.Length;
			Span<int> temp = stackalloc int[list.Length];
			int now = 0;
			for (int i = 0; i < list.Length; i++)
			{
				if (!temp.Slice(0, now).Contains(list[i]))
					temp[now++] = list[i];
			}
			return now;
		}
		#endregion

		#region range
		/// <summary>
		/// Generate range of type <see cref="char"/> <see cref="Span{T}"/>.
		/// </summary>
		/// <param name="span">The span to fill</param>
		/// <param name="start">start value of the range</param>
		/// <param name="step">step of the range</param>
		public static void FillWithRange(this Span<char> span, char start, int step = 1)
		{
			if (step == 0)
				throw new ArgumentOutOfRangeException(nameof(step), step, Parameter.CannotZero);

			for (int i = 0; i < span.Length; i++)
			{
				span[i] = (char)(i * step + start);
			}
		}

		/// <summary>
		/// Generate range of type <see cref="int"/> <see cref="Span{T}"/>.
		/// </summary>
		/// <param name="span">The span to fill</param>
		/// <param name="start">start value of the range</param>
		/// <param name="step">step of the range</param>
		public static void FillWithRange(this Span<int> span, int start, int step = 1)
		{
			if (step == 0)
				throw new ArgumentOutOfRangeException(nameof(step), step, Parameter.CannotZero);

			for (int i = 0; i < span.Length; i++)
			{
				span[i] = i * step + start;
			}
		}

		/// <summary>
		/// Generate range of type <see cref="long"/> <see cref="Span{T}"/>.
		/// </summary>
		/// <param name="span">The span to fill</param>
		/// <param name="start">start value of the range</param>
		/// <param name="step">step of the range</param>
		public static void FillWithRange(this Span<long> span, long start, long step = 1)
		{
			if (step == 0)
				throw new ArgumentOutOfRangeException(nameof(step), step, Parameter.CannotZero);

			for (int i = 0; i < span.Length; i++)
			{
				span[i] = i * step + start;
			}
		}
		#endregion
	}
}
