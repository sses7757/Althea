using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;


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
		/// <typeparam name="T">the type of <paramref name="span"/> and <paramref name="array"/></typeparam>
		/// <param name="span">the <see cref="Span{T}"/> to be copied into</param>
		/// <param name="array">the destination array to be copied</param>
		public static void CopyTo<T>(this IReadOnlyList<T> array, Span<T> span)
		{
			if (array is null)
				throw new ArgumentNullException(nameof(array));
			if (span.Length != array.Count)
				throw new ArgumentException(Resource.VectorLength, nameof(array));

			for (int i = 0; i < span.Length; i++)
			{
				span[i] = array[i];
			}
		}


		/// <summary>
		/// Copy <paramref name="span"/> to <paramref name="destArray"/>.
		/// </summary>
		/// <typeparam name="T">the type of <paramref name="span"/> and <paramref name="destArray"/></typeparam>
		/// <param name="span">the <see cref="Span{T}"/> to be copied</param>
		/// <param name="destArray">the destination array to be copied into</param>
		public static void CopyTo<T>(this Span<T> span, T[] destArray)
		{
			if (destArray is null)
				throw new ArgumentNullException(nameof(destArray));
			if (span.Length != destArray.Length)
				throw new ArgumentException(Resource.VectorLength, nameof(destArray));

			for (int i = 0; i < span.Length; i++)
			{
				destArray[i] = span[i];
			}
		}

		/// <summary>
		/// Copy <paramref name="span"/> to <paramref name="destArray"/>.
		/// </summary>
		/// <typeparam name="T">the type of <paramref name="span"/> and <paramref name="destArray"/></typeparam>
		/// <param name="span">the <see cref="ReadOnlySpan{T}"/> to be copied</param>
		/// <param name="destArray">the destination array to be copied into</param>
		public static void CopyTo<T>(this ReadOnlySpan<T> span, T[] destArray)
		{
			if (destArray is null)
				throw new ArgumentNullException(nameof(destArray));
			if (span.Length != destArray.Length)
				throw new ArgumentException(Resource.VectorLength, nameof(destArray));

			for (int i = 0; i < span.Length; i++)
			{
				destArray[i] = span[i];
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
		/// <param name="array">the array to order</param>
		/// <param name="indices">the indices to order, if this is null or empty, <paramref name="array"/> will be returned</param>
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
		/// <param name="array">the array to order</param>
		/// <param name="target">the array to put the reordered result</param>
		/// <param name="indices">the indices to order, if this is null or empty, <paramref name="array"/> will be returned</param>
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
		/// <param name="array">the array to order</param>
		/// <param name="target">the array to put the reordered result</param>
		/// <param name="indices">the indices to order, if this is null or empty, <paramref name="array"/> will be returned</param>
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
		/// <param name="array">the array to order</param>
		/// <param name="target">the array to put the reordered result</param>
		/// <param name="indices">the indices to order, if this is null or empty, <paramref name="array"/> will be returned</param>
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
		/// <param name="array">the array to order</param>
		/// <param name="target">the array to put the reordered result</param>
		/// <param name="indices">the indices to order, if this is null or empty, <paramref name="array"/> will be returned</param>
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
		/// <param name="array">the array to order</param>
		/// <param name="target">the array to put the reordered result</param>
		/// <param name="indices">the indices to order, if this is null or empty, <paramref name="array"/> will be returned</param>
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
		/// <param name="array">the array to order</param>
		/// <param name="target">the array to put the reordered result</param>
		/// <param name="indices">the indices to order, if this is null or empty, <paramref name="array"/> will be returned</param>
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
		/// <param name="array">the array to order</param>
		/// <param name="target">the array to put the reordered result</param>
		/// <param name="indices">The indices to order. May has less elements than <paramref name="array"/>. If so, the rest elements in <paramref name="target"/> remains unchanged.</param>
		public static void InverseOrderTo<T>(this Span<T> array, Span<T> target, int[] indices)
		{
			if (indices is null || indices.Length == 0)
				throw new ArgumentNullException(nameof(indices));
			if (indices.Length > array.Length || indices.Length > target.Length)
				throw new ArgumentOutOfRangeException(nameof(indices));

			for (int i = 0; i < indices.Length; i++)
			{
				target[indices[i]] = array[i];
			}
		}

		/// <summary>
		/// Inverse order the <paramref name="array"/> by <paramref name="indices"/> out-of-place such that <c><paramref name="target"/>[<paramref name="indices"/>] = <paramref name="array"/></c>.
		/// </summary>
		/// <typeparam name="T">any type</typeparam>
		/// <param name="array">the array to order</param>
		/// <param name="target">the array to put the reordered result</param>
		/// <param name="indices">The indices to order. May has less elements than <paramref name="array"/>. If so, the rest elements in <paramref name="target"/> remains unchanged.</param>
		public static void InverseOrderTo<T>(this Span<T> array, Span<T> target, ReadOnlySpan<int> indices)
		{
			if (indices.Length == 0)
				throw new ArgumentNullException(nameof(indices));
			if (indices.Length > array.Length || indices.Length > target.Length)
				throw new ArgumentOutOfRangeException(nameof(indices));

			for (int i = 0; i < indices.Length; i++)
			{
				target[indices[i]] = array[i];
			}
		}

		/// <summary>
		/// Inverse order the <paramref name="array"/> by <paramref name="indices"/> out-of-place such that <c><paramref name="target"/>[<paramref name="indices"/>] = <paramref name="array"/></c>.
		/// </summary>
		/// <typeparam name="T">any type</typeparam>
		/// <param name="array">the array to order</param>
		/// <param name="target">the array to put the reordered result</param>
		/// <param name="indices">The indices to order. May has less elements than <paramref name="array"/>. If so, the rest elements in <paramref name="target"/> remains unchanged.</param>
		public static void InverseOrderTo<T>(this IReadOnlyList<T> array, Span<T> target, IReadOnlyList<int> indices)
		{
			if (array is null || array.Count == 0)
				throw new ArgumentNullException(nameof(array));
			if (indices is null || indices.Count == 0)
				throw new ArgumentNullException(nameof(indices));
			if (indices.Count > array.Count || indices.Count > target.Length)
				throw new ArgumentOutOfRangeException(nameof(indices));

			for (int i = 0; i < indices.Count; i++)
			{
				target[indices[i]] = array[i];
			}
		}

		/// <summary>
		/// Inverse order the <paramref name="array"/> by <paramref name="indices"/> out-of-place such that <c><paramref name="target"/>[<paramref name="indices"/>] = <paramref name="array"/></c>.
		/// </summary>
		/// <typeparam name="T">any type</typeparam>
		/// <param name="array">the array to order</param>
		/// <param name="target">the array to put the reordered result</param>
		/// <param name="indices">The indices to order. May has less elements than <paramref name="array"/>. If so, the rest elements in <paramref name="target"/> remains unchanged.</param>
		public static void InverseOrderTo<T>(this IReadOnlyList<T> array, Span<T> target, ReadOnlySpan<int> indices)
		{
			if (array is null || array.Count == 0)
				throw new ArgumentNullException(nameof(array));
			if (indices.Length == 0)
				throw new ArgumentNullException(nameof(indices));
			if (indices.Length > array.Count || indices.Length > target.Length)
				throw new ArgumentOutOfRangeException(nameof(indices));

			for (int i = 0; i < indices.Length; i++)
			{
				target[indices[i]] = array[i];
			}
		}

		/// <summary>
		/// Inverse order the <paramref name="array"/> by <paramref name="indices"/> out-of-place such that <c><paramref name="target"/>[<paramref name="indices"/>] = <paramref name="array"/></c>.
		/// </summary>
		/// <typeparam name="T">any type</typeparam>
		/// <param name="array">the array to order</param>
		/// <param name="target">the array to put the reordered result</param>
		/// <param name="indices">The indices to order. May has less elements than <paramref name="array"/>. If so, the rest elements in <paramref name="target"/> remains unchanged.</param>
		public static void InverseOrderTo<T>(this Span<T> array, T[] target, ReadOnlySpan<int> indices)
		{
			if (array.Length == 0)
				throw new ArgumentNullException(nameof(array));
			if (indices.Length == 0)
				throw new ArgumentNullException(nameof(indices));
			if (indices.Length > array.Length || indices.Length > target.Length)
				throw new ArgumentOutOfRangeException(nameof(indices));

			for (int i = 0; i < indices.Length; i++)
			{
				target[indices[i]] = array[i];
			}
		}

		/// <summary>
		/// Find the permutation order such that <c><paramref name="array"/>[result] = <paramref name="target"/></c>
		/// </summary>
		/// <typeparam name="T">any type</typeparam>
		/// <param name="array">the array before permutation</param>
		/// <param name="target">the array after the permutation</param>
		/// <param name="perm">the result permutation order to put in as a <see cref="Span{T}"/>, may be overwritten by undesired values if there is no such permutation</param>
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
		/// <param name="perm">the input permutation</param>
		/// <param name="inv">the inverse permutation to put in</param>
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

		#region predicate
		/// <summary>
		/// Check if <paramref name="list"/>'s all elements are sequentially equal to <paramref name="other"/>'s
		/// </summary>
		/// <typeparam name="T">data type</typeparam>
		/// <param name="list">the list to compare</param>
		/// <param name="other">the other list to compare</param>
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
		/// <typeparam name="T">any type</typeparam>
		/// <param name="span">span to get hash code</param>
		/// <returns>the hash code of <paramref name="span"/></returns>
		public static int HashCodeOfSpan<T>(this ReadOnlySpan<T> span) where T : struct
		{
			if (span.Length == 0)
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
		/// <param name="span">the span to fill</param>
		/// <param name="start">start value of the range</param>
		/// <param name="step">step of the range</param>
		public static void FillWithRange(this Span<char> span, char start, int step = 1)
		{
			if (step == 0)
				throw new ArgumentOutOfRangeException(nameof(step), step, Resource.ParaCannotZero);

			for (int i = 0; i < span.Length; i++)
			{
				span[i] = (char)(i * step + start);
			}
		}

		/// <summary>
		/// Generate range of type <see cref="int"/> <see cref="Span{T}"/>.
		/// </summary>
		/// <param name="span">the span to fill</param>
		/// <param name="start">start value of the range</param>
		/// <param name="step">step of the range</param>
		public static void FillWithRange(this Span<int> span, int start, int step = 1)
		{
			if (step == 0)
				throw new ArgumentOutOfRangeException(nameof(step), step, Resource.ParaCannotZero);

			for (int i = 0; i < span.Length; i++)
			{
				span[i] = i * step + start;
			}
		}

		/// <summary>
		/// Generate range of type <see cref="long"/> <see cref="Span{T}"/>.
		/// </summary>
		/// <param name="span">the span to fill</param>
		/// <param name="start">start value of the range</param>
		/// <param name="step">step of the range</param>
		public static void FillWithRange(this Span<long> span, long start, long step = 1)
		{
			if (step == 0)
				throw new ArgumentOutOfRangeException(nameof(step), step, Resource.ParaCannotZero);

			for (int i = 0; i < span.Length; i++)
			{
				span[i] = i * step + start;
			}
		}
		#endregion
	}
}
