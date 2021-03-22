using System;
using System.Collections.Generic;

using Althea.NativeTypes;
using Althea.Resources;


namespace Althea.Linq
{
	/// <summary>
	/// A replacement of <see cref="System.Linq.Enumerable"/> to reduce GC stress.<br/>
	/// Most methods of this class is based on <see cref="IReadOnlyList{T}"/> and is implemented by <see cref="Array"/>.
	/// </summary>
	public static partial class ArrayLinq
	{
		#region min max
		/// <summary>
		/// Find the maximum item of <paramref name="list"/>.
		/// </summary>
		/// <typeparam name="T">data type of array that can be compared</typeparam>
		/// <param name="list">list to find maximum</param>
		/// <returns>the maximum item</returns>
		public static T Max<T>(this IReadOnlyList<T> list) where T : IComparable<T>
		{
			T maxVal = list[0];
			for (int i = 0; i < list.Count; i++)
			{
				T val = list[i];
				if (val.CompareTo(maxVal) > 0)
					maxVal = val;
			}
			return maxVal;
		}

		/// <summary>
		/// Find the maximum item of <paramref name="list"/> by <paramref name="selector"/>.
		/// </summary>
		/// <typeparam name="T">The input data type</typeparam>
		/// <typeparam name="TOut">data type of array that can be compared</typeparam>
		/// <param name="list">list to find maximum</param>
		/// <param name="selector">The selector used to convert <typeparamref name="T"/> to <typeparamref name="TOut"/></param>
		/// <returns>the maximum item</returns>
		public static TOut Max<T, TOut>(this IReadOnlyList<T> list, Converter<T, TOut> selector) where TOut : IComparable<TOut>
		{
			if (selector is null)
				throw new ArgumentNullException(nameof(selector));
			TOut maxVal = selector(list[0]);
			for (int i = 0; i < list.Count; i++)
			{
				TOut val = selector(list[i]);
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
		public static T Min<T>(this IReadOnlyList<T> list) where T : IComparable<T>
		{
			T minVal = list[0];
			for (int i = 0; i < list.Count; i++)
			{
				T val = list[i];
				if (val.CompareTo(minVal) < 0)
					minVal = val;
			}
			return minVal;
		}

		/// <summary>
		/// Find the minimum item of <paramref name="list"/> by <paramref name="selector"/>.
		/// </summary>
		/// <typeparam name="T">The input data type</typeparam>
		/// <typeparam name="TOut">data type of array that can be compared</typeparam>
		/// <param name="list">list to find minimum</param>
		/// <param name="selector">The selector used to convert <typeparamref name="T"/> to <typeparamref name="TOut"/></param>
		/// <returns>the minimum item</returns>
		public static TOut Min<T, TOut>(this IReadOnlyList<T> list, Converter<T, TOut> selector) where TOut : IComparable<TOut>
		{
			if (selector is null)
				throw new ArgumentNullException(nameof(selector));

			TOut maxVal = selector(list[0]);
			for (int i = 0; i < list.Count; i++)
			{
				TOut val = selector(list[i]);
				if (val.CompareTo(maxVal) < 0)
					maxVal = val;
			}
			return maxVal;
		}
		#endregion

		#region aggregate
		/// <summary>
		/// General list aggregate.
		/// </summary>
		/// <typeparam name="TIn">input list type</typeparam>
		/// <typeparam name="TOut">output list type</typeparam>
		/// <param name="list">The list to accumulate</param>
		/// <param name="func">accumulate function, input is the element of list and the accumulation value</param>
		/// <param name="init">initial output value</param>
		/// <returns>Aggregate result <typeparamref name="TOut"/></returns>
		/// <remarks>extend method of <paramref name="list"/></remarks>
		public static TOut Aggregate<TIn, TOut>(this IReadOnlyList<TIn> list, Aggregator<TIn, TOut> func, TOut init)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
			if (func is null)
				throw new ArgumentNullException(nameof(func));
			for (int i = 0; i < list.Count; i++)
			{
				init = func(list[i], init);
			}
			return init;
		}

		/// <summary>
		/// General list accumulation.
		/// </summary>
		/// <typeparam name="TIn">input list type</typeparam>
		/// <typeparam name="TOut">output list type</typeparam>
		/// <param name="list">The list to accumulate</param>
		/// <param name="func">accumulate function, input is the element of list and the accumulation value</param>
		/// <param name="init">initial output value</param>
		/// <returns>Accumulated result <see cref="IReadOnlyList{TOut}"/> which contains the <paramref name="init"/> as the first element</returns>
		/// <remarks>extend method of <paramref name="list"/></remarks>
		public static IReadOnlyList<TOut> Accumulate<TIn, TOut>(this IReadOnlyList<TIn> list, Aggregator<TIn, TOut> func, TOut init)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
			if (func is null)
				throw new ArgumentNullException(nameof(func));
			var output = new TOut[list.Count + 1];
			output[0] = init;
			for (int i = 0; i < list.Count; i++)
			{
				output[i + 1] = func(list[i], output[i]);
			}
			return output;
		}

		/// <summary>
		/// List accumulate summation.
		/// </summary>
		/// <param name="list">The list to accumulate</param>
		/// <param name="init">The initial value</param>
		/// <param name="inclusive">Whether to include lower-index end (true) or the upper-index end (false) or both (null)</param>
		/// <returns>The output accumulated result</returns>
		public static IReadOnlyList<T> AccumulateSum<T>(this IReadOnlyList<T> list, T init = default, bool? inclusive = true) where T : unmanaged
		{
			if (list is null || list.Count == 0)
				throw new ArgumentNullException(nameof(list));

			int len = list.Count;
			if (inclusive is null)
			{
				T[] res = new T[len + 1];
				res[0] = init;
				for (int i = 0; i < len; i++)
				{
					res[i + 1] = Const<T>.AddDelegate.Invoke(list[i], res[i]);
				}
				return res;
			}

			T[] result = new T[len];
			if (inclusive.Value)
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
			return result;
		}

		/// <summary>
		/// List accumulate product.
		/// </summary>
		/// <param name="list">The list to accumulate</param>
		/// <param name="init">The initial value, default 0 will be replaced by 1</param>
		/// <param name="inclusive">Whether to include lower-index end (true) or the upper-index end (false) or both (null)</param>
		/// <returns>The output accumulated result</returns>
		public static IReadOnlyList<T> AccumulateProd<T>(this IReadOnlyList<T> list, T init = default, bool? inclusive = true) where T : unmanaged
		{
			if (list is null || list.Count == 0)
				throw new ArgumentNullException(nameof(list));
			if (init.IsZero())
				init = Const<T>.One;

			int len = list.Count;
			if (inclusive is null)
			{
				T[] res = new T[len + 1];
				res[0] = init;
				for (int i = 0; i < len; i++)
				{
					res[i + 1] = Const<T>.MultiplyDelegate.Invoke(list[i], res[i]);
				}
				return res;
			}

			T[] result = new T[len];
			if (inclusive.Value)
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
			return result;
		}

		/// <summary>
		/// General list accumulation without the <paramref name="init"/> as an output element.
		/// </summary>
		/// <typeparam name="TIn">input list type</typeparam>
		/// <typeparam name="TOut">output list type</typeparam>
		/// <param name="list">The list to accumulate</param>
		/// <param name="func">accumulate function, input is the element of list and the accumulation value</param>
		/// <param name="init">initial output value</param>
		/// <returns>Accumulated result <see cref="IReadOnlyList{TOut}"/> with <c><paramref name="func"/>(<paramref name="list"/>[0], <paramref name="init"/>)</c> as first element</returns>
		/// <remarks>extend method of <paramref name="list"/></remarks>
		public static IReadOnlyList<TOut> AccumulateNoInit<TIn, TOut>(this IReadOnlyList<TIn> list, Aggregator<TIn, TOut> func, TOut init)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
			if (func is null)
				throw new ArgumentNullException(nameof(func));
			var output = new TOut[list.Count];
			for (int i = 0; i < list.Count; i++)
			{
				init = func(list[i], init);
				output[i] = init;
			}
			return output;
		}

		/// <summary>
		/// List summation.
		/// </summary>
		/// <param name="list">The list to accumulate</param>
		/// <returns>The summation result</returns>
		public static T Sum<T>(this IReadOnlyList<T> list) where T : unmanaged
		{
			if (list is null || list.Count == 0)
				throw new ArgumentNullException(nameof(list));

			int len = list.Count;
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
		/// <param name="list">The list to accumulate</param>
		/// <returns>The product result</returns>
		public static T Prod<T>(this IReadOnlyList<T> list) where T : unmanaged
		{
			if (list is null || list.Count == 0)
				throw new ArgumentNullException(nameof(list));

			int len = list.Count;
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
		public static T Sum<TOrg, T>(this IReadOnlyList<TOrg> list, Converter<TOrg, T> selector) where T : unmanaged
		{
			if (list is null || list.Count == 0)
				throw new ArgumentNullException(nameof(list));

			int len = list.Count;
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
		public static T Prod<TOrg, T>(this IReadOnlyList<TOrg> list, Converter<TOrg, T> selector) where T : unmanaged
		{
			if (list is null || list.Count == 0)
				throw new ArgumentNullException(nameof(list));

			int len = list.Count;
			T result = selector.Invoke(list[0]);
			for (int i = 1; i < len; i++)
			{
				result = Const<T>.MultiplyDelegate.Invoke(selector.Invoke(list[i]), result);
			}
			return result;
		}
		#endregion

		#region predicate
		/// <summary>
		/// Check if <paramref name="list"/>'s all elements are sequentially equal to <paramref name="other"/>'s
		/// </summary>
		/// <typeparam name="T">data type</typeparam>
		/// <param name="list">The list to compare</param>
		/// <param name="other">The other list to compare</param>
		/// <returns>Sequentially equals or not</returns>
		public static bool SequenceEqual<T>(this IReadOnlyList<T> list, IReadOnlyList<T> other) where T : IEquatable<T>
		{
			if (ReferenceEquals(list, other))
				return true;
			if (list.Count != other.Count)
				return false;
			if (list is T[] a1 && other is T[] a2)
				return ((ReadOnlySpan<T>)a1).SequenceEqual(a2);
			for (int i = 0; i < list.Count; i++)
			{
				if (!list[i].Equals(other[i]))
					return false;
			}
			return true;
		}

		/// <summary>
		/// Check if <paramref name="list"/>'s all elements are sequentially equal to <paramref name="other"/>'s
		/// </summary>
		/// <typeparam name="T">data type</typeparam>
		/// <param name="list">The list to compare</param>
		/// <param name="other">The other list to compare</param>
		/// <param name="comparer">The <see cref="IEqualityComparer{T}"/> to use, default null means <see cref="EqualityComparer{T}.Default"/></param>
		/// <returns>Sequentially equals or not</returns>
		public static bool SequenceEqual<T>(this IReadOnlyList<T> list, IReadOnlyList<T> other, IEqualityComparer<T>? comparer = null)
		{
			if (ReferenceEquals(list, other))
				return true;
			if (list.Count != other.Count)
				return false;
			comparer ??= EqualityComparer<T>.Default;
			for (int i = 0; i < list.Count; i++)
			{
				if (!comparer.Equals(list[i], other[i]))
					return false;
			}
			return true;
		}

		/// <summary>
		/// Check if <paramref name="list"/>'s all elements are sequentially equal to <paramref name="other"/>'s
		/// </summary>
		/// <typeparam name="TL">The left input type</typeparam>
		/// <typeparam name="TR">The right input type</typeparam>
		/// <param name="list">The list to compare</param>
		/// <param name="other">The other list to compare</param>
		/// <param name="equalityComparer">The function used to compare equality</param>
		/// <returns>Sequentially equals or not</returns>
		public static bool SequenceEqual<TL, TR>(this IReadOnlyList<TL> list, IReadOnlyList<TR> other, EqualComparer<TL, TR> equalityComparer)
		{
			if (ReferenceEquals(list, other))
				return true;
			if (list.Count != other.Count)
				return false;
			if (equalityComparer is null)
				throw new ArgumentNullException(nameof(equalityComparer));

			for (int i = 0; i < list.Count; i++)
			{
				if (!equalityComparer(list[i], other[i]))
					return false;
			}
			return true;
		}

		/// <summary>
		/// Check if all elements of <paramref name="list"/> <c>e</c>, <c><paramref name="predicator"/>(e) == true</c>
		/// </summary>
		/// <param name="list">The list to predicate</param>
		/// <param name="predicator">The predicator delegate</param>
		/// <returns>Predicate result</returns>
		/// <remarks>extend method of <paramref name="list"/></remarks>
		public static bool All<T>(this IReadOnlyList<T> list, Predicate<T> predicator)
		{
			if (list is T[] a)
			{
				return Array.TrueForAll(a, predicator);
			}
			else if (list is List<T> l)
			{
				return l.TrueForAll(predicator);
			}
			for (int i = 0; i < list.Count; i++)
			{
				if (!predicator(list[i]))
					return false;
			}
			return true;
		}

		/// <summary>
		/// Check if any element of <paramref name="list"/> <c>e</c>, <c><paramref name="predicator"/>(e) == true</c>
		/// </summary>
		/// <param name="list">The list to predicate</param>
		/// <param name="predicator">The predicator delegate</param>
		/// <returns>Predicate result</returns>
		/// <remarks>extend method of <paramref name="list"/></remarks>
		public static bool Any<T>(this IReadOnlyList<T> list, Predicate<T> predicator)
		{
			if (predicator is null)
				throw new ArgumentNullException(nameof(predicator));

			if (list is T[] a)
			{
				return Array.Exists(a, predicator);
			}
			else if (list is List<T> l)
			{
				return l.Exists(predicator);
			}
			for (int i = 0; i < list.Count; i++)
			{
				if (predicator(list[i]))
					return true;
			}
			return false;
		}

		/// <summary>
		/// Boolean list has any true or not
		/// </summary>
		/// <param name="list">boolean list</param>
		/// <returns>any true or not</returns>
		public static bool AnyTrue(this IReadOnlyList<bool> list)
		{
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i])
					return true;
			}
			return false;
		}

		/// <summary>
		/// Boolean list has any false or not
		/// </summary>
		/// <param name="list">boolean list</param>
		/// <returns>any false or not</returns>
		public static bool AnyFalse(this IReadOnlyList<bool> list)
		{
			for (int i = 0; i < list.Count; i++)
			{
				if (!list[i])
					return true;
			}
			return false;
		}

		/// <summary>
		/// Boolean list all true or not
		/// </summary>
		/// <param name="list">boolean list</param>
		/// <returns>all true or not</returns>
		public static bool AllTrue(this IReadOnlyList<bool> list)
		{
			for (int i = 0; i < list.Count; i++)
			{
				if (!list[i])
					return false;
			}
			return true;
		}

		/// <summary>
		/// Boolean list all false or not
		/// </summary>
		/// <param name="list">boolean list</param>
		/// <returns>all false or not</returns>
		public static bool AllFalse(this IReadOnlyList<bool> list)
		{
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i])
					return false;
			}
			return true;
		}
		#endregion

		#region basic SQL statements
		/// <summary>
		/// Get the first element of <paramref name="list"/> is available or default otherwise
		/// </summary>
		/// <typeparam name="T">data type</typeparam>
		/// <param name="list">The list to take</param>
		/// <returns>the first element or default</returns>
		public static T? FirstOrDefault<T>(this IReadOnlyList<T> list) where T : class
		{
			if (list is null || list.Count == 0)
				return default;
			return list[0];
		}

		/// <summary>
		/// Append an <paramref name="element"/> to the end of <paramref name="list"/>
		/// </summary>
		/// <typeparam name="T">data type</typeparam>
		/// <param name="list">The list to be appended</param>
		/// <param name="element">The value to append</param>
		/// <returns>a new list after appending <paramref name="element"/></returns>
		public static IReadOnlyList<T> Append<T>(this IReadOnlyList<T> list, T element)
		{
			if (list is null || list.Count == 0)
				return new[] { element };
			T[] newArray = new T[list.Count + 1];
			Array.Copy(list.ToArray(), newArray, list.Count);
			newArray[^1] = element;
			return newArray;
		}

		/// <summary>
		/// General list converter.
		/// </summary>
		/// <typeparam name="TIn">input list type</typeparam>
		/// <typeparam name="TOut">output list type</typeparam>
		/// <param name="list">The list to convert</param>
		/// <param name="selector">selector function used to convert</param>
		/// <returns>Result after conversion</returns>
		/// <remarks>extend method of <paramref name="list"/></remarks>
		public static IReadOnlyList<TOut> Select<TIn, TOut>(this IReadOnlyList<TIn> list, Converter<TIn, TOut> selector)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
			if (selector is null)
				throw new ArgumentNullException(nameof(selector));

			if (list is TIn[] a)
			{
				return Array.ConvertAll(a, selector);
			}
			else if (list is List<TIn> l)
			{
				return l.ConvertAll(selector);
			}
			var output = new TOut[list.Count];
			for (int i = 0; i < list.Count; i++)
			{
				output[i] = selector(list[i]);
			}
			return output;
		}

		/// <summary>
		/// General list converter with index as second input of <paramref name="selector"/>.
		/// </summary>
		/// <typeparam name="TIn">input list type</typeparam>
		/// <typeparam name="TOut">output list type</typeparam>
		/// <param name="list">The list to convert</param>
		/// <param name="selector">selector function used to convert</param>
		/// <returns>Result after conversion</returns>
		/// <remarks>extend method of <paramref name="list"/></remarks>
		public static IReadOnlyList<TOut> Select<TIn, TOut>(this IReadOnlyList<TIn> list, Selector<TIn, TOut> selector)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
			if (selector is null)
				throw new ArgumentNullException(nameof(selector));

			var output = new TOut[list.Count];
			for (int i = 0; i < list.Count; i++)
			{
				output[i] = selector(list[i], i);
			}
			return output;
		}

		/// <summary>
		/// General list converter that concatenates the <paramref name="selector"/>'s outputs.
		/// </summary>
		/// <typeparam name="TIn">input list type</typeparam>
		/// <typeparam name="TOut">output list type</typeparam>
		/// <param name="list">The list to convert</param>
		/// <param name="selector">selector function used to convert</param>
		/// <returns>Result after conversion</returns>
		/// <remarks>extend method of <paramref name="list"/></remarks>
		public static IReadOnlyList<TOut> SelectMany<TIn, TOut>(this IReadOnlyList<TIn> list, Converter<TIn, IReadOnlyList<TOut>> selector)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
			if (selector is null)
				throw new ArgumentNullException(nameof(selector));

			var output = new IReadOnlyList<TOut>[list.Count];
			var offsets = new int[list.Count + 1];
			for (int i = 0; i < list.Count; i++)
			{
				output[i] = selector(list[i]);
				offsets[i + 1] = output[i].Count + offsets[i];
			}
			var res = new TOut[offsets[^1]];
			for (int i = 0; i < list.Count; i++)
				Array.Copy(sourceArray: output[i].ToArray(), sourceIndex: 0, destinationArray: res, destinationIndex: offsets[i], length: output[i].Count);
			return res;
		}

		/// <summary>
		/// Pick the element(s) <c>e</c> in <paramref name="list"/> where <c><paramref name="predicator"/>(e) == true</c>
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="list">list to pick</param>
		/// <param name="predicator">The predicator used to pick</param>
		/// <returns>the result <see cref="IReadOnlyList{T}"/></returns>
		public static IReadOnlyList<T> Where<T>(this IReadOnlyList<T> list, Predicate<T> predicator)
		{
			if (predicator is null)
				throw new ArgumentNullException(nameof(predicator));

			if (list is T[] a)
			{
				return Array.FindAll(a, predicator);
			}
			else if (list is List<T> l)
			{
				return l.FindAll(predicator);
			}
			var res = new List<T>(list.Count);
			for (int i = 0; i < list.Count; i++)
			{
				if (predicator(list[i]))
					res.Add(list[i]);
			}
			return res.ToArray();
		}

		/// <summary>
		/// Pick the element(s) and index <c>e, i</c> in <paramref name="list"/> where <c><paramref name="predicator"/>(e, i) == true</c>
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="list">list to pick</param>
		/// <param name="predicator">The predicator used to pick</param>
		/// <returns>the result <see cref="IReadOnlyList{T}"/></returns>
		public static IReadOnlyList<T> Where<T>(this IReadOnlyList<T> list, IndexdPredicator<T> predicator)
		{
			if (predicator is null)
				throw new ArgumentNullException(nameof(predicator));
			var res = new List<T>(list.Count);
			for (int i = 0; i < list.Count; i++)
			{
				if (predicator(list[i], i))
					res.Add(list[i]);
			}
			return res.ToArray();
		}

		/// <summary>
		/// Count the element(s) <c>e</c> in <paramref name="list"/> where <c><paramref name="predicator"/>(e) == true</c>
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="list">list to count in</param>
		/// <param name="predicator">The predicator used to count</param>
		/// <returns>the count</returns>
		public static int Count<T>(this IReadOnlyList<T> list, Predicate<T> predicator)
		{
			if (predicator is null)
				throw new ArgumentNullException(nameof(predicator));

			int count = 0;
			for (int i = 0; i < list.Count; i++)
			{
				if (predicator(list[i]))
					count++;
			}
			return count;
		}

		/// <summary>
		/// Zip two list to form a list of <see cref="ValueTuple{T1, T2}"/>
		/// </summary>
		/// <typeparam name="T1">input type 1</typeparam>
		/// <typeparam name="T2">input type 2</typeparam>
		/// <param name="list1">input list 1</param>
		/// <param name="list2">input list 2</param>
		/// <returns>The concatenated <see cref="IReadOnlyList{ValueTuple}"/></returns>
		public static IReadOnlyList<(T1 First, T2 Second)> Zip<T1, T2>(this IReadOnlyList<T1> list1, IReadOnlyList<T2> list2)
		{
			if (list1 is null || list1.Count == 0)
				throw new ArgumentNullException(nameof(list1));
			if (list2 is null || list2.Count == 0)
				throw new ArgumentNullException(nameof(list2));
			if (list1.Count != list2.Count)
				throw new ArgumentException(Parameter.NotSameSize);

			var res = new (T1, T2)[list1.Count];
			for (int i = 0; i < list1.Count; i++)
			{
				res[i] = (list1[i], list2[i]);
			}
			return res;
		}

		/// <summary>
		/// Zip two list and convert according to <paramref name="func"/>
		/// </summary>
		/// <typeparam name="T1">input type 1</typeparam>
		/// <typeparam name="T2">input type 2</typeparam>
		/// <typeparam name="TOut">output type</typeparam>
		/// <param name="list1">input list 1</param>
		/// <param name="list2">input list 2</param>
		/// <param name="func">convert function</param>
		/// <returns>The concatenated <see cref="IReadOnlyList{TOut}"/></returns>
		public static IReadOnlyList<TOut> Zip<T1, T2, TOut>(this IReadOnlyList<T1> list1, IReadOnlyList<T2> list2, ZipConverter<T1, T2, TOut> func)
		{
			if (list1 is null || list1.Count == 0)
				throw new ArgumentNullException(nameof(list1));
			if (list2 is null || list2.Count == 0)
				throw new ArgumentNullException(nameof(list2));
			if (list1.Count != list2.Count)
				throw new ArgumentException(Parameter.NotSameSize);
			if (func is null)
				throw new ArgumentNullException(nameof(func));
			var res = new TOut[list1.Count];
			for (int i = 0; i < list1.Count; i++)
			{
				res[i] = func(list1[i], list2[i]);
			}
			return res;
		}

		/// <summary>
		/// Zip three list and convert according to <paramref name="func"/>
		/// </summary>
		/// <typeparam name="T1">input type 1</typeparam>
		/// <typeparam name="T2">input type 2</typeparam>
		/// <typeparam name="T3">input type 3</typeparam>
		/// <typeparam name="TOut">output type</typeparam>
		/// <param name="list1">input list 1</param>
		/// <param name="list2">input list 2</param>
		/// <param name="list3">input list 3</param>
		/// <param name="func">convert function</param>
		/// <returns>The concatenated <see cref="IReadOnlyList{TOut}"/></returns>
		public static IReadOnlyList<TOut> Zip<T1, T2, T3, TOut>(this IReadOnlyList<T1> list1, IReadOnlyList<T2> list2, IReadOnlyList<T3> list3, ZipConverter<T1, T2, T3, TOut> func)
		{
			if (list1 is null || list1.Count == 0)
				throw new ArgumentNullException(nameof(list1));
			if (list2 is null || list2.Count == 0)
				throw new ArgumentNullException(nameof(list2));
			if (list3 is null || list3.Count == 0)
				throw new ArgumentNullException(nameof(list3));
			if (list1.Count != list2.Count || list1.Count != list3.Count)
				throw new ArgumentException(Parameter.NotSameSize);
			if (func is null)
				throw new ArgumentNullException(nameof(func));

			var res = new TOut[list1.Count];
			for (int i = 0; i < list1.Count; i++)
			{
				res[i] = func(list1[i], list2[i], list3[i]);
			}
			return res;
		}

		/// <summary>
		/// Convert the input <see cref="IReadOnlyList{T}"/> <paramref name="list"/> to an array
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="list">list to convert</param>
		/// <returns><paramref name="list"/> itself if it is an array or a copied array</returns>
		public static T[] ToArray<T>(this IReadOnlyList<T> list)
		{
			if (list is T[] a)
				return a;
			if (list is List<T> l)
				return l.ToArray();
			var res = new T[list.Count];
			for (int i = 0; i < list.Count; i++)
			{
				res[i] = list[i];
			}
			return res;
		}

		/// <summary>
		/// Convert the input <see cref="IReadOnlyList{T}"/> <paramref name="list"/> to a copied array
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="list">list to convert</param>
		/// <returns>a copied array</returns>
		public static T[] ToCopiedArray<T>(this IReadOnlyList<T> list)
		{
			if (list is T[] a)
				return (T[])a.Clone();
			if (list is List<T> l)
				return l.ToArray();
			var res = new T[list.Count];
			for (int i = 0; i < list.Count; i++)
			{
				res[i] = list[i];
			}
			return res;
		}
		#endregion

		#region stable sort
		/// <summary>
		/// Stably sort the <paramref name="items"/> by <paramref name="keys"/> <b>in-place</b>.
		/// </summary>
		/// <typeparam name="TKey">The type of the elements of the <paramref name="keys"/>.</typeparam>
		/// <typeparam name="TItem">The type of the elements of the <paramref name="items"/>.</typeparam>
		/// <param name="keys">The one-dimensional, zero-based array that contains the keys to sort.</param>
		/// <param name="items">The one-dimensional, zero-based array that contains the items that correspond to the keys in keys, or null to sort only keys.</param>
		public static void StableSortWith<TKey, TItem>(this TKey[] keys, TItem[] items) where TKey : IComparable<TKey>
		{
			if (keys is null || keys.Length == 0)
				throw new ArgumentNullException(nameof(keys));
			if (items is null || items.Length == 0)
				throw new ArgumentNullException(nameof(items));

			var ordered = System.Linq.Enumerable.OrderBy(System.Linq.Enumerable.Zip(keys, items), static k => k.First);
			var temp = System.Linq.Enumerable.ToArray(ordered);
			temp.CopyTo(keys, static t => t.First);
			temp.CopyTo(items, static t => t.Second);
		}
		#endregion

		#region set operations
		/// <summary>
		/// Check whether <paramref name="list"/> contains <paramref name="element"/>
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="list">list to check in</param>
		/// <param name="element">element to check</param>
		/// <returns><paramref name="list"/> contains <paramref name="element"/> or not</returns>
		public static bool Contains<T>(this IReadOnlyList<T> list, T element) where T : IEquatable<T>
		{
			if (list is List<T> l)
			{
				return l.Contains(element);
			}
			for (int i = 0; i < list.Count; i++)
			{
				if (element.Equals(list[i]))
					return true;
			}
			return false;
		}

		/// <summary>
		/// Check whether <paramref name="list"/> contains <paramref name="element"/>
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="list">list to check in</param>
		/// <param name="element">element to check</param>
		/// <param name="comparer">The <see cref="IEqualityComparer{T}"/> to use, default null means <see cref="EqualityComparer{T}.Default"/></param>
		/// <returns><paramref name="list"/> contains <paramref name="element"/> or not</returns>
		public static bool Contains<T>(this IReadOnlyList<T> list, T element, IEqualityComparer<T>? comparer = null)
		{
			if (comparer is null)
			{
				if (list is List<T> l)
				{
					return l.Contains(element);
				}
			}
			comparer ??= EqualityComparer<T>.Default;
			for (int i = 0; i < list.Count; i++)
			{
				if (comparer.Equals(list[i], element))
					return true;
			}
			return false;
		}

		/// <summary>
		/// Check whether <paramref name="list"/> contains <paramref name="element"/>
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <typeparam name="TCompare">The type used to compare</typeparam>
		/// <param name="list">list to check in</param>
		/// <param name="element">element to check</param>
		/// <param name="selector">The converter applied to <paramref name="list"/> before comparisons</param>
		/// <param name="comparer">The <see cref="IEqualityComparer{T}"/> to use, default null means <see cref="EqualityComparer{T}.Default"/></param>
		/// <returns><paramref name="list"/> contains <paramref name="element"/> or not</returns>
		public static bool Contains<T, TCompare>(this IReadOnlyList<T> list, TCompare element, Converter<T, TCompare> selector, IEqualityComparer<TCompare>? comparer = null)
		{
			comparer ??= EqualityComparer<TCompare>.Default;
			for (int i = 0; i < list.Count; i++)
			{
				if (comparer.Equals(selector(list[i]), element))
					return true;
			}
			return false;
		}

		/// <summary>
		/// Check whether <paramref name="list"/> contains <paramref name="element"/>
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <typeparam name="TCompare">The type used to compare</typeparam>
		/// <param name="list">list to check in</param>
		/// <param name="element">element to check</param>
		/// <param name="selector">The converter applied to <paramref name="list"/> before comparisons</param>
		/// <returns><paramref name="list"/> contains <paramref name="element"/> or not</returns>
		public static bool Contains<T, TCompare>(this IReadOnlyList<T> list, TCompare element, Converter<T, TCompare> selector) where TCompare : IEquatable<TCompare>
		{
			for (int i = 0; i < list.Count; i++)
			{
				if (element.Equals(selector(list[i])))
					return true;
			}
			return false;
		}

		/// <summary>
		/// Get the first element <c>e</c> in <paramref name="list"/> where <c>e</c> is also in <paramref name="other"/>
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="list">list to get from</param>
		/// <param name="other">The list used to compare</param>
		/// <returns>The first intersect element or default</returns>
		public static T? FirstIntersect<T>(this IReadOnlyList<T> list, IReadOnlyList<T> other) where T : IEquatable<T>
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
			if (other is null)
				throw new ArgumentNullException(nameof(other));

			for (int i = 0; i < list.Count; i++)
			{
				if (other.Contains(list[i]))
					return list[i];
			}
			return default;
		}
		#endregion

		#region randoms
		private static readonly Random random = new();

		/// <summary>
		/// Random shuffle the <paramref name="list"/> by random generator <paramref name="rand"/>
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="list">The list to be shuffled</param>
		/// <param name="rand">random generator <see cref="Random"/>, default null means internal one</param>
		/// <returns>the new shuffled list</returns>
		public static IReadOnlyList<T> Shuffle<T>(this IReadOnlyList<T> list, Random? rand = null)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
			rand ??= random;
			var res = list.ToCopiedArray();
			int n = list.Count;
			while (n > 1)
			{
				n--;
				int k = rand.Next(n + 1);
				T value = list[k];
				res[k] = list[n];
				res[n] = value;
			}
			return res;
		}

		/// <summary>
		/// Generate a random <see cref="int"/> array of length <paramref name="count"/> whose elements are unique and with in range [<paramref name="minValue"/>, <paramref name="minValue"/>).
		/// </summary>
		/// <param name="count">The length of array to generate</param>
		/// <param name="minValue">The inclusive lower bound of the random number returned.</param>
		/// <param name="maxValue">The exclusive upper bound of the random number returned. maxValue must be greater than or equal to <c><paramref name="minValue"/> + <paramref name="count"/></c>.</param>
		/// <returns>the generate a random <see cref="int"/> array</returns>
		public static int[] RandomUniqueArray(int count, int minValue, int maxValue)
		{
			if (count <= 0)
				throw new ArgumentOutOfRangeException(nameof(count), count, Parameter.InvalidValue);
			if (maxValue < minValue + count)
				throw new ArgumentOutOfRangeException(nameof(maxValue), maxValue, Parameter.InvalidValue);

			HashSet<int> set = new(count);
			while (set.Count != count)
				set.Add(random.Next(minValue, maxValue));
			return System.Linq.Enumerable.ToArray(set);
		}

		/// <summary>
		/// Generate a random <see cref="int"/> array of length <paramref name="count"/> whose elements' sum is <paramref name="sum"/>.
		/// </summary>
		/// <param name="count">The length of array to generate</param>
		/// <param name="sum">The desired sum of elements generated</param>
		/// <returns>the generate a random <see cref="int"/> array whose elements are all positive</returns>
		public static int[] RandomPositiveArrayOfSum(int count, int sum)
		{
			if (count <= 0)
				throw new ArgumentOutOfRangeException(nameof(count), count, Parameter.InvalidValue);
			if (sum <= 0)
				throw new ArgumentOutOfRangeException(nameof(sum), sum, Parameter.InvalidValue);

			int[] partition = RandomUniqueArray(count - 1, 1, sum);
			Array.Sort(partition);
			int[] arr = new int[count];
			arr[0] = partition[0];
			for (int i = 1; i < count - 1; i++)
			{
				arr[i] = partition[i] - partition[i - 1];
			}
			arr[count - 1] = sum - partition[^1];
			return arr;
		}

		/// <summary>
		/// Generate a random <see cref="int"/> array of length <paramref name="count"/> whose elements' sum is <paramref name="sum"/>.
		/// </summary>
		/// <param name="count">The length of array to generate</param>
		/// <param name="sum">The desired sum of elements generated</param>
		/// <returns>the generate a random <see cref="int"/> array whose elements are all non-negative</returns>
		public static int[] RandomNonNegativeArrayOfSum(int count, int sum)
		{
			if (count <= 0)
				throw new ArgumentOutOfRangeException(nameof(count), count, Parameter.InvalidValue);
			if (sum < count)
				throw new ArgumentOutOfRangeException(nameof(sum), sum, Parameter.InvalidValue);

			int[] arr = new int[count];
			for (int i = 0; i < sum; i++)
			{
				arr[random.Next(0, sum) % count]++;
			}
			return arr;
		}
		#endregion

		#region converters
		/// <summary>
		/// Act on each element of a list.
		/// </summary>
		/// <typeparam name="T">any type</typeparam>
		/// <param name="list">input list</param>
		/// <param name="action">action whose parameters are the value and the index of the array respectively</param>
		public static void ForEach<T>(this IReadOnlyList<T> list, Action<T> action)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
			for (int i = 0; i < list.Count; i++)
			{
				action(list[i]);
			}
		}

		internal const int CRC_CONST = 314159;

		/// <summary>
		/// Get the hash code of an array using CRC method
		/// </summary>
		/// <typeparam name="T">any type</typeparam>
		/// <param name="array">array to get hash code</param>
		/// <returns>the hash code of <paramref name="array"/></returns>
		public static int HashCodeOfArray<T>(this IReadOnlyList<T> array)
		{
			if (array is null || array.Count == 0)
				return HashCode.Combine(array); // hash code of null
			int hc = array.Count;
			for (int i = 0; i < array.Count; ++i)
			{	// CRC
				var a = array[i];
				if (a is null)
					hc = unchecked(hc * CRC_CONST);
				else
					hc = unchecked(hc * CRC_CONST + a.GetHashCode());
			}
			return hc;
		}

		/// <summary>
		/// Get the hash code of an array using CRC method
		/// </summary>
		/// <typeparam name="T">any type</typeparam>
		/// <param name="array">array to get hash code</param>
		/// <param name="hashCodeConverter">The converter used to get the hash code of each element</param>
		/// <returns>the hash code of <paramref name="array"/></returns>
		public static int HashCodeOfArray<T>(this IReadOnlyList<T> array, Converter<T, int> hashCodeConverter)
		{
			if (array is null || array.Count == 0)
				return HashCode.Combine(array); // hash code of null
			int hc = array.Count;
			for (int i = 0; i < array.Count; ++i)
			{
				hc = unchecked(hc * CRC_CONST + hashCodeConverter(array[i])); // CRC
			}
			return hc;
		}

		/// <summary>
		/// Get the hash code of a set (order-independent) using ADD method
		/// </summary>
		/// <typeparam name="T">any type</typeparam>
		/// <param name="set">array to get hash code</param>
		/// <returns>the hash code of <paramref name="set"/></returns>
		public static int HashCodeOfSet<T>(this IReadOnlyList<T> set)
		{
			if (set is null || set.Count == 0)
				return HashCode.Combine(set); // hash code of null
			int hc = 0;
			for (int i = 0; i < set.Count; ++i)
			{
				// CRC
				var a = set[i];
				if (a is not null)
					hc = unchecked(hc + a.GetHashCode());
			}
			return hc;
		}

		/// <summary>
		/// Get the hash code of a set (order-independent) using ADD method
		/// </summary>
		/// <typeparam name="T">any type</typeparam>
		/// <param name="set">array to get hash code</param>
		/// <param name="hashCodeConverter">The converter used to get the hash code of each element</param>
		/// <returns>the hash code of <paramref name="set"/></returns>
		public static int HashCodeOfSet<T>(this IReadOnlyList<T> set, Converter<T, int> hashCodeConverter)
		{
			if (set is null || set.Count == 0)
				return HashCode.Combine(set); // hash code of null
			int hc = 0;
			for (int i = 0; i < set.Count; ++i)
			{
				hc = unchecked(hc + hashCodeConverter(set[i]));
			}
			return hc;
		}
		#endregion
	}
}
