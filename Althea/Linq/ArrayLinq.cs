using System;
using System.Collections.Generic;

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

		/// <summary>
		/// Find the maximum item of <paramref name="list"/> by conversion function <paramref name="selector"/>.
		/// </summary>
		/// <typeparam name="T">data type of array</typeparam>
		/// <typeparam name="TSort">The data type that can be compared</typeparam>
		/// <param name="list">list to find maximum</param>
		/// <param name="selector">conversion function</param>
		/// <returns>the maximum item</returns>
		public static T MaxBy<T, TSort>(this IReadOnlyList<T> list, Converter<T, TSort> selector) where TSort : IComparable<TSort>
		{
			if (selector is null)
				throw new ArgumentNullException(nameof(selector));

			TSort maxVal = selector(list[0]);
			int maxInd = 0;
			for (int i = 0; i < list.Count; i++)
			{
				TSort val = selector(list[i]);
				if (val.CompareTo(maxVal) > 0)
				{
					maxInd = i; maxVal = val;
				}
			}
			return list[maxInd];
		}

		/// <summary>
		/// Find the minimum item of <paramref name="list"/> by conversion function <paramref name="selector"/>.
		/// </summary>
		/// <typeparam name="T">data type of array</typeparam>
		/// <typeparam name="TSort">The data type that can be compared</typeparam>
		/// <param name="list">list to find minimum</param>
		/// <param name="selector">conversion function</param>
		/// <returns>the minimum item</returns>
		public static T MinBy<T, TSort>(this IReadOnlyList<T> list, Converter<T, TSort> selector) where TSort : IComparable<TSort>
		{
			if (selector is null)
				throw new ArgumentNullException(nameof(selector));
			TSort minVal = selector(list[0]);
			int minInd = 0;
			for (int i = 0; i < list.Count; i++)
			{
				TSort val = selector(list[i]);
				if (val.CompareTo(minVal) < 0)
				{
					minInd = i; minVal = val;
				}
			}
			return list[minInd];
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

		#region concrete accumulate sum
		/// <summary>
		/// List accumulate summation.
		/// </summary>
		/// <param name="list">The list to accumulate</param>
		/// <param name="init">initial output value</param>
		/// <returns>Accumulated result <see cref="IReadOnlyList{TOut}"/> which contains the <paramref name="init"/> as the first element</returns>
		/// <remarks>extend method of <paramref name="list"/></remarks>
		public static IReadOnlyList<int> AccumulateSum(this IReadOnlyList<int> list, int init = 0)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
			var output = new int[list.Count + 1];
			output[0] = init;
			for (int i = 0; i < list.Count; i++)
			{
				output[i + 1] = list[i] + output[i];
			}
			return output;
		}

		/// <summary>
		/// List accumulate summation.
		/// </summary>
		/// <param name="list">The list to accumulate</param>
		/// <param name="init">initial output value</param>
		/// <returns>Accumulated result <see cref="IReadOnlyList{TOut}"/> which contains the <paramref name="init"/> as the first element</returns>
		/// <remarks>extend method of <paramref name="list"/></remarks>
		public static IReadOnlyList<long> AccumulateSum(this IReadOnlyList<long> list, long init = 0)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
			var output = new long[list.Count + 1];
			output[0] = init;
			for (int i = 0; i < list.Count; i++)
			{
				output[i + 1] = list[i] + output[i];
			}
			return output;
		}

		/// <summary>
		/// List accumulate summation.
		/// </summary>
		/// <param name="list">The list to accumulate</param>
		/// <param name="init">initial output value</param>
		/// <returns>Accumulated result <see cref="IReadOnlyList{TOut}"/> which contains the <paramref name="init"/> as the first element</returns>
		/// <remarks>extend method of <paramref name="list"/></remarks>
		public static IReadOnlyList<float> AccumulateSum(this IReadOnlyList<float> list, float init = 0)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
			var output = new float[list.Count + 1];
			output[0] = init;
			for (int i = 0; i < list.Count; i++)
			{
				output[i + 1] = list[i] + output[i];
			}
			return output;
		}

		/// <summary>
		/// List accumulate summation.
		/// </summary>
		/// <param name="list">The list to accumulate</param>
		/// <param name="init">initial output value</param>
		/// <returns>Accumulated result <see cref="IReadOnlyList{TOut}"/> which contains the <paramref name="init"/> as the first element</returns>
		/// <remarks>extend method of <paramref name="list"/></remarks>
		public static IReadOnlyList<double> AccumulateSum(this IReadOnlyList<double> list, double init = 0)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
			var output = new double[list.Count + 1];
			output[0] = init;
			for (int i = 0; i < list.Count; i++)
			{
				output[i + 1] = list[i] + output[i];
			}
			return output;
		}
		#endregion

		#region concrete accumulate prod
		/// <summary>
		/// List accumulate product.
		/// </summary>
		/// <param name="list">The list to accumulate</param>
		/// <param name="init">initial output value</param>
		/// <returns>Accumulated result <see cref="IReadOnlyList{TOut}"/> which contains the <paramref name="init"/> as the first element</returns>
		/// <remarks>extend method of <paramref name="list"/></remarks>
		public static IReadOnlyList<int> AccumulateProd(this IReadOnlyList<int> list, int init = 1)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
			var output = new int[list.Count + 1];
			output[0] = init;
			for (int i = 0; i < list.Count; i++)
			{
				output[i + 1] = list[i] * output[i];
			}
			return output;
		}

		/// <summary>
		/// List accumulate product.
		/// </summary>
		/// <param name="list">The list to accumulate</param>
		/// <param name="init">initial output value</param>
		/// <returns>Accumulated result <see cref="IReadOnlyList{TOut}"/> which contains the <paramref name="init"/> as the first element</returns>
		/// <remarks>extend method of <paramref name="list"/></remarks>
		public static IReadOnlyList<long> AccumulateProd(this IReadOnlyList<long> list, long init = 1)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
			var output = new long[list.Count + 1];
			output[0] = init;
			for (int i = 0; i < list.Count; i++)
			{
				output[i + 1] = list[i] * output[i];
			}
			return output;
		}

		/// <summary>
		/// List accumulate product.
		/// </summary>
		/// <param name="list">The list to accumulate</param>
		/// <param name="init">initial output value</param>
		/// <returns>Accumulated result <see cref="IReadOnlyList{TOut}"/> which contains the <paramref name="init"/> as the first element</returns>
		/// <remarks>extend method of <paramref name="list"/></remarks>
		public static IReadOnlyList<float> AccumulateProd(this IReadOnlyList<float> list, float init = 1)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
			var output = new float[list.Count + 1];
			output[0] = init;
			for (int i = 0; i < list.Count; i++)
			{
				output[i + 1] = list[i] * output[i];
			}
			return output;
		}

		/// <summary>
		/// List accumulate product.
		/// </summary>
		/// <param name="list">The list to accumulate</param>
		/// <param name="init">initial output value</param>
		/// <returns>Accumulated result <see cref="IReadOnlyList{TOut}"/> which contains the <paramref name="init"/> as the first element</returns>
		/// <remarks>extend method of <paramref name="list"/></remarks>
		public static IReadOnlyList<double> AccumulateProd(this IReadOnlyList<double> list, double init = 1)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
			var output = new double[list.Count + 1];
			output[0] = init;
			for (int i = 0; i < list.Count; i++)
			{
				output[i + 1] = list[i] * output[i];
			}
			return output;
		}
		#endregion

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

		#region concrete prod
		/// <summary>
		/// List product
		/// </summary>
		/// <param name="list"></param>
		/// <returns>Product result, 1 if <paramref name="list"/> is null</returns>
		/// <remarks>extend method of <paramref name="list"/></remarks>
		public static int Prod(this IReadOnlyList<int> list)
		{
			if (list is null || list.Count == 0)
				return 1;
			int prod = 1;
			for (int i = 0; i < list.Count; i++)
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
		public static long Prod(this IReadOnlyList<long> list)
		{
			if (list is null || list.Count == 0)
				return 1;
			long prod = 1;
			for (int i = 0; i < list.Count; i++)
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
		public static float Prod(this IReadOnlyList<float> list)
		{
			if (list is null || list.Count == 0)
				return 1;
			float prod = 1;
			for (int i = 0; i < list.Count; i++)
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
		public static double Prod(this IReadOnlyList<double> list)
		{
			if (list is null || list.Count == 0)
				return 1;
			double prod = 1;
			for (int i = 0; i < list.Count; i++)
			{
				prod *= list[i];
			}
			return prod;
		}
		#endregion

		#region concrete sum
		/// <summary>
		/// List summation
		/// </summary>
		/// <param name="list"></param>
		/// <returns>Summation result, 0 if <paramref name="list"/> is null</returns>
		/// <remarks>extend method of <paramref name="list"/></remarks>
		public static int Sum(this IReadOnlyList<int> list)
		{
			if (list is null || list.Count == 0)
				return 0;
			int sum = 0;
			for (int i = 0; i < list.Count; i++)
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
		public static long Sum(this IReadOnlyList<long> list)
		{
			if (list is null || list.Count == 0)
				return 0;
			long sum = 0;
			for (int i = 0; i < list.Count; i++)
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
		public static float Sum(this IReadOnlyList<float> list)
		{
			if (list is null || list.Count == 0)
				return 0;
			float sum = 0;
			for (int i = 0; i < list.Count; i++)
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
		public static double Sum(this IReadOnlyList<double> list)
		{
			if (list is null || list.Count == 0)
				return 0;
			double sum = 0;
			for (int i = 0; i < list.Count; i++)
			{
				sum += list[i];
			}
			return sum;
		}
		#endregion

		#region concrete selector sum
		/// <summary>
		/// List summation by <paramref name="selector"/>
		/// </summary>
		/// <param name="list"></param>
		/// <param name="selector">The selector to apply to each element</param>
		/// <returns>Summation result, 0 if <paramref name="list"/> is null</returns>
		/// <remarks>extend method of <paramref name="list"/></remarks>
		public static int Sum<T>(this IReadOnlyList<T> list, Converter<T, int> selector)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
			int sum = 0;
			for (int i = 0; i < list.Count; i++)
			{
				sum += selector(list[i]);
			}
			return sum;
		}

		/// <summary>
		/// List summation by <paramref name="selector"/>
		/// </summary>
		/// <param name="list"></param>
		/// <param name="selector">The selector to apply to each element</param>
		/// <returns>Summation result, 0 if <paramref name="list"/> is null</returns>
		/// <remarks>extend method of <paramref name="list"/></remarks>
		public static long Sum<T>(this IReadOnlyList<T> list, Converter<T, long> selector)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
			long sum = 0;
			for (int i = 0; i < list.Count; i++)
			{
				sum += selector(list[i]);
			}
			return sum;
		}

		/// <summary>
		/// List summation by <paramref name="selector"/>
		/// </summary>
		/// <param name="list"></param>
		/// <param name="selector">The selector to apply to each element</param>
		/// <returns>Summation result, 0 if <paramref name="list"/> is null</returns>
		/// <remarks>extend method of <paramref name="list"/></remarks>
		public static float Sum<T>(this IReadOnlyList<T> list, Converter<T, float> selector)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
			float sum = 0;
			for (int i = 0; i < list.Count; i++)
			{
				sum += selector(list[i]);
			}
			return sum;
		}

		/// <summary>
		/// List summation by <paramref name="selector"/>
		/// </summary>
		/// <param name="list"></param>
		/// <param name="selector">The selector to apply to each element</param>
		/// <returns>Summation result, 0 if <paramref name="list"/> is null</returns>
		/// <remarks>extend method of <paramref name="list"/></remarks>
		public static double Sum<T>(this IReadOnlyList<T> list, Converter<T, double> selector)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
			double sum = 0;
			for (int i = 0; i < list.Count; i++)
			{
				sum += selector(list[i]);
			}
			return sum;
		}
		#endregion

		#region concrete selector prod
		/// <summary>
		/// List product by <paramref name="selector"/>
		/// </summary>
		/// <param name="list"></param>
		/// <param name="selector">The selector to apply to each element</param>
		/// <returns>Product result, 0 if <paramref name="list"/> is null</returns>
		/// <remarks>extend method of <paramref name="list"/></remarks>
		public static int Prod<T>(this IReadOnlyList<T> list, Converter<T, int> selector)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
			int sum = 0;
			for (int i = 0; i < list.Count; i++)
			{
				sum += selector(list[i]);
			}
			return sum;
		}

		/// <summary>
		/// List product by <paramref name="selector"/>
		/// </summary>
		/// <param name="list"></param>
		/// <param name="selector">The selector to apply to each element</param>
		/// <returns>Product result, 0 if <paramref name="list"/> is null</returns>
		/// <remarks>extend method of <paramref name="list"/></remarks>
		public static long Prod<T>(this IReadOnlyList<T> list, Converter<T, long> selector)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
			long sum = 0;
			for (int i = 0; i < list.Count; i++)
			{
				sum += selector(list[i]);
			}
			return sum;
		}

		/// <summary>
		/// List product by <paramref name="selector"/>
		/// </summary>
		/// <param name="list"></param>
		/// <param name="selector">The selector to apply to each element</param>
		/// <returns>Product result, 0 if <paramref name="list"/> is null</returns>
		/// <remarks>extend method of <paramref name="list"/></remarks>
		public static float Prod<T>(this IReadOnlyList<T> list, Converter<T, float> selector)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
			float sum = 0;
			for (int i = 0; i < list.Count; i++)
			{
				sum += selector(list[i]);
			}
			return sum;
		}

		/// <summary>
		/// List product by <paramref name="selector"/>
		/// </summary>
		/// <param name="list"></param>
		/// <param name="selector">The selector to apply to each element</param>
		/// <returns>Product result, 0 if <paramref name="list"/> is null</returns>
		/// <remarks>extend method of <paramref name="list"/></remarks>
		public static double Prod<T>(this IReadOnlyList<T> list, Converter<T, double> selector)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
			double sum = 0;
			for (int i = 0; i < list.Count; i++)
			{
				sum += selector(list[i]);
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
		/// <param name="list">The list to compare</param>
		/// <param name="other">The other list to compare</param>
		/// <returns>Sequentially equals or not</returns>
		public static bool SequenceEqual<T>(this IReadOnlyList<T> list, IReadOnlyList<T> other) where T : IEquatable<T>
		{
			if (ReferenceEquals(list, other))
				return true;
			if (list.Count != other.Count)
				return false;
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

		#region concrete type SequenceEqual
		/// <summary>
		/// Check if <paramref name="list"/>'s all elements are Sequentially equal to <paramref name="other"/>'s
		/// </summary>
		/// <param name="list">The list to compare</param>
		/// <param name="other">The other list to compare</param>
		/// <returns>Sequentially equals or not</returns>
		public static bool SequenceEqual(this IReadOnlyList<byte> list, IReadOnlyList<byte> other)
		{
			if (ReferenceEquals(list, other))
				return true;
			if (list.Count != other.Count)
				return false;

			for (int i = 0; i < list.Count; i++)
			{
				if (list[i] != other[i])
					return false;
			}
			return true;
		}

		/// <summary>
		/// Check if <paramref name="list"/>'s all elements are Sequentially equal to <paramref name="other"/>'s
		/// </summary>
		/// <param name="list">The list to compare</param>
		/// <param name="other">The other list to compare</param>
		/// <returns>Sequentially equals or not</returns>
		public static bool SequenceEqual(this IReadOnlyList<char> list, IReadOnlyList<char> other)
		{
			if (ReferenceEquals(list, other))
				return true;
			if (list.Count != other.Count)
				return false;

			for (int i = 0; i < list.Count; i++)
			{
				if (list[i] != other[i])
					return false;
			}
			return true;
		}

		/// <summary>
		/// Check if <paramref name="list"/>'s all elements are Sequentially equal to <paramref name="other"/>'s
		/// </summary>
		/// <param name="list">The list to compare</param>
		/// <param name="other">The other list to compare</param>
		/// <returns>Sequentially equals or not</returns>
		public static bool SequenceEqual(this IReadOnlyList<int> list, IReadOnlyList<int> other)
		{
			if (ReferenceEquals(list, other))
				return true;
			if (list.Count != other.Count)
				return false;

			for (int i = 0; i < list.Count; i++)
			{
				if (list[i] != other[i])
					return false;
			}
			return true;
		}

		/// <summary>
		/// Check if <paramref name="list"/>'s all elements are Sequentially equal to <paramref name="other"/>'s
		/// </summary>
		/// <param name="list">The list to compare</param>
		/// <param name="other">The other list to compare</param>
		/// <returns>Sequentially equals or not</returns>
		public static bool SequenceEqual(this IReadOnlyList<long> list, IReadOnlyList<long> other)
		{
			if (ReferenceEquals(list, other))
				return true;
			if (list.Count != other.Count)
				return false;

			for (int i = 0; i < list.Count; i++)
			{
				if (list[i] != other[i])
					return false;
			}
			return true;
		}
		#endregion

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

		#region concrete type Select
		/// <summary>
		/// General list converter.
		/// </summary>
		/// <typeparam name="T">input list type</typeparam>
		/// <param name="list">The list to convert</param>
		/// <param name="selector">selector function used to convert</param>
		/// <returns>Result after conversion</returns>
		/// <remarks>extend method of <paramref name="list"/></remarks>
		public static IReadOnlyList<Index> Select<T>(this IReadOnlyList<T> list, Converter<T, Index> selector)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
			if (selector is null)
				throw new ArgumentNullException(nameof(selector));

			if (list is T[] a)
			{
				return Array.ConvertAll(a, selector);
			}
			else if (list is List<T> l)
			{
				return l.ConvertAll(selector);
			}
			var output = new Index[list.Count];
			for (int i = 0; i < list.Count; i++)
			{
				output[i] = selector(list[i]);
			}
			return output;
		}

		/// <summary>
		/// General list converter.
		/// </summary>
		/// <typeparam name="T">input list type</typeparam>
		/// <param name="list">The list to convert</param>
		/// <param name="selector">selector function used to convert</param>
		/// <returns>Result after conversion</returns>
		/// <remarks>extend method of <paramref name="list"/></remarks>
		public static IReadOnlyList<char> Select<T>(this IReadOnlyList<T> list, Converter<T, char> selector)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
			if (selector is null)
				throw new ArgumentNullException(nameof(selector));

			if (list is T[] a)
			{
				return Array.ConvertAll(a, selector);
			}
			else if (list is List<T> l)
			{
				return l.ConvertAll(selector);
			}
			var output = new char[list.Count];
			for (int i = 0; i < list.Count; i++)
			{
				output[i] = selector(list[i]);
			}
			return output;
		}

		/// <summary>
		/// General list converter.
		/// </summary>
		/// <typeparam name="TIn">input list type</typeparam>
		/// <param name="list">The list to convert</param>
		/// <param name="selector">selector function used to convert</param>
		/// <returns>Result after conversion</returns>
		/// <remarks>extend method of <paramref name="list"/></remarks>
		public static IReadOnlyList<int> Select<TIn>(this IReadOnlyList<TIn> list, Converter<TIn, int> selector)
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
			var output = new int[list.Count];
			for (int i = 0; i < list.Count; i++)
			{
				output[i] = selector(list[i]);
			}
			return output;
		}

		/// <summary>
		/// General list converter.
		/// </summary>
		/// <typeparam name="TIn">input list type</typeparam>
		/// <param name="list">The list to convert</param>
		/// <param name="selector">selector function used to convert</param>
		/// <returns>Result after conversion</returns>
		/// <remarks>extend method of <paramref name="list"/></remarks>
		public static IReadOnlyList<long> Select<TIn>(this IReadOnlyList<TIn> list, Converter<TIn, long> selector)
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
			var output = new long[list.Count];
			for (int i = 0; i < list.Count; i++)
			{
				output[i] = selector(list[i]);
			}
			return output;
		}
		#endregion

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
		public static IReadOnlyList<T> Where<T>(this IReadOnlyList<T> list, IndexPredicator<T> predicator)
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
		/// Get all the indices of the occurrences of <paramref name="value"/> in <paramref name="list"/>
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="list">The list to find in</param>
		/// <param name="value">The value to find</param>
		/// <returns>the zero-based indices or empty if not founded</returns>
		public static int[] IndicesOf<T>(this IReadOnlyList<T> list, T value) where T : IEquatable<T>
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));

			List<int> indices = new(list.Count);
			for (int i = 0; i < list.Count; i++)
				if (value.Equals(list[i]))
					indices.Add(i);
			return indices.ToArray();
		}

		/// <summary>
		/// Get the index of the first occurrence of <paramref name="value"/> in <paramref name="list"/>
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="list">The list to find in</param>
		/// <param name="value">The value to find</param>
		/// <returns>the zero-based index or -1 if not founded</returns>
		public static int IndexOf<T>(this IReadOnlyList<T> list, T value) where T : IEquatable<T>
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));

			if (list is T[] a)
			{
				return Array.IndexOf(a, value);
			}
			else if (list is List<T> l)
			{
				return l.IndexOf(value);
			}
			for (int i = 0; i < list.Count; i++)
				if (value.Equals(list[i]))
					return i;
			return -1;
		}

		/// <summary>
		/// Get all the indices of the occurrences of <paramref name="value"/> in <paramref name="list"/>
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="list">The list to find in</param>
		/// <param name="value">The value to find</param>
		/// <param name="comparer">The <see cref="IEqualityComparer{T}"/> to use, default null means <see cref="EqualityComparer{T}.Default"/></param>
		/// <returns>the zero-based indices or empty if not founded</returns>
		public static int[] IndicesOf<T>(this IReadOnlyList<T> list, T value, IEqualityComparer<T>? comparer = null)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));

			comparer ??= EqualityComparer<T>.Default;
			List<int> indices = new(list.Count);
			for (int i = 0; i < list.Count; i++)
				if (comparer.Equals(list[i], value))
					indices.Add(i);
			return indices.ToArray();
		}

		/// <summary>
		/// Get the index of the first occurrence of <paramref name="value"/> in <paramref name="list"/>
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="list">The list to find in</param>
		/// <param name="value">The value to find</param>
		/// <param name="comparer">The <see cref="IEqualityComparer{T}"/> to use, default null means <see cref="EqualityComparer{T}.Default"/></param>
		/// <returns>the zero-based index or -1 if not founded</returns>
		public static int IndexOf<T>(this IReadOnlyList<T> list, T value, IEqualityComparer<T>? comparer = null)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));

			if (comparer is null)
			{
				if (list is T[] a)
				{
					return Array.IndexOf(a, value);
				}
				else if (list is List<T> l)
				{
					return l.IndexOf(value);
				}
			}
			comparer ??= EqualityComparer<T>.Default;
			for (int i = 0; i < list.Count; i++)
				if (comparer.Equals(list[i], value))
					return i;
			return -1;
		}

		/// <summary>
		/// Get the index of the first occurrence of <paramref name="value"/> in <paramref name="list"/>
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <typeparam name="TOut">The selected data type</typeparam>
		/// <param name="list">The list to find in</param>
		/// <param name="selector">The selector used to convert <paramref name="list"/> before comparison</param>
		/// <param name="value">The value to find</param>
		/// <returns>the zero-based index or -1 if not founded</returns>
		public static int IndexOf<T, TOut>(this IReadOnlyList<T> list, Converter<T, TOut> selector, TOut value) where TOut : IEquatable<TOut>
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));

			for (int i = 0; i < list.Count; i++)
				if (value.Equals(selector(list[i])))
					return i;
			return -1;
		}

		/// <summary>
		/// Get the index of the first occurrence of <paramref name="value"/> in <paramref name="list"/>
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <typeparam name="TOut">The selected data type</typeparam>
		/// <param name="list">The list to find in</param>
		/// <param name="selector">The selector used to convert <paramref name="list"/> before comparison</param>
		/// <param name="value">The value to find</param>
		/// <param name="comparer">The <see cref="IEqualityComparer{T}"/> to use, default null means <see cref="EqualityComparer{T}.Default"/></param>
		/// <returns>the zero-based index or -1 if not founded</returns>
		public static int IndexOf<T, TOut>(this IReadOnlyList<T> list, Converter<T, TOut> selector, TOut value, IEqualityComparer<TOut>? comparer = null)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));

			comparer ??= EqualityComparer<TOut>.Default;
			for (int i = 0; i < list.Count; i++)
				if (comparer.Equals(selector(list[i]), value))
					return i;
			return -1;
		}

		/// <summary>
		/// Pick the index of element(s) <c>i</c> in <paramref name="list"/> where <c><paramref name="predicator"/>(i) == true</c>
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="list">list to pick</param>
		/// <param name="predicator">The predicator used to pick</param>
		/// <returns>the result <see cref="IReadOnlyList{T}"/></returns>
		public static IReadOnlyList<T> IndexWhere<T>(this IReadOnlyList<T> list, Predicate<int> predicator)
		{
			if (predicator is null)
				throw new ArgumentNullException(nameof(predicator));

			var res = new List<T>(list.Count);
			for (int i = 0; i < list.Count; i++)
			{
				if (predicator(i))
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
		/// Concatenate two lists
		/// </summary>
		/// <typeparam name="T">input type</typeparam>
		/// <param name="list1">input list 1</param>
		/// <param name="list2">input list 2</param>
		/// <returns>The concatenated <see cref="IReadOnlyList{ValueTuple}"/></returns>
		public static IReadOnlyList<T> Concat<T>(this IReadOnlyList<T> list1, IReadOnlyList<T> list2)
		{
			var res = new T[list1.Count + list2.Count];
			if (list1 is T[] a1 && list2 is T[] a2)
			{
				Array.Copy(sourceArray: a1, destinationArray: res, length: a1.Length);
				Array.Copy(sourceArray: a2, sourceIndex: 0, destinationArray: res, destinationIndex: a1.Length, length: a2.Length);
			}
			else
			{
				for (int i = 0; i < list1.Count; i++)
				{
					res[i] = list1[i];
				}
				for (int i = list1.Count; i < res.Length; i++)
				{
					res[i] = list2[i - list1.Count];
				}
			}
			return res;
		}

		/// <summary>
		/// Take first <paramref name="count"/> of <paramref name="list"/>
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="list">list to take in</param>
		/// <param name="from">The inclusive index to take from</param>
		/// <param name="count">number of elements to take</param>
		/// <returns>the result list</returns>
		public static IReadOnlyList<T> TakeRange<T>(this IReadOnlyList<T> list, int from, int count)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
			if (from < 0 || from >= list.Count)
				throw new ArgumentOutOfRangeException(nameof(from), from, Parameter.InvalidValue);
			if (count + from > list.Count)
				throw new ArgumentOutOfRangeException(nameof(count), count, Parameter.InvalidValue);

			if (list is T[] a)
			{
				return a[from..(count + from)];
			}
			var res = new T[count];
			for (int i = 0; i < count; i++)
			{
				res[i] = list[i];
			}
			return res;
		}

		/// <summary>
		/// Take first <paramref name="count"/> of <paramref name="list"/>
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="list">list to take in</param>
		/// <param name="count">number of elements to take</param>
		/// <returns>the result list</returns>
		public static IReadOnlyList<T> Take<T>(this IReadOnlyList<T> list, int count)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
			if (count > list.Count)
				throw new ArgumentOutOfRangeException(nameof(count), count, Parameter.InvalidValue);

			if (list is T[] a)
			{
				return a[..count];
			}
			var res = new T[count];
			for (int i = 0; i < count; i++)
			{
				res[i] = list[i];
			}
			return res;
		}

		/// <summary>
		/// Take last <paramref name="count"/> of <paramref name="list"/>
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="list">list to take in</param>
		/// <param name="count">number of elements to take</param>
		/// <returns>the result list</returns>
		public static IReadOnlyList<T> TakeLast<T>(this IReadOnlyList<T> list, int count)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
			if (count > list.Count)
				throw new ArgumentOutOfRangeException(nameof(count), count, Parameter.InvalidValue);

			if (list is T[] a)
			{
				return a[^count..];
			}
			var res = new T[count];
			for (int i = list.Count - count, j = 0; i < list.Count; i++, j++)
			{
				res[j] = list[i];
			}
			return res;
		}

		/// <summary>
		/// Skip first <paramref name="count"/> of <paramref name="list"/>
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="list">list to take in</param>
		/// <param name="count">number of elements to take</param>
		/// <returns>the result list</returns>
		public static IReadOnlyList<T> Skip<T>(this IReadOnlyList<T> list, int count)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
			if (count > list.Count)
				throw new ArgumentOutOfRangeException(nameof(count), count, Parameter.InvalidValue);

			if (list is T[] a)
			{
				return a[count..];
			}
			var res = new T[list.Count - count];
			for (int i = count; i < list.Count; i++)
			{
				res[i - count] = list[i];
			}
			return res;
		}

		/// <summary>
		/// Skip last <paramref name="count"/> of <paramref name="list"/>
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="list">list to take in</param>
		/// <param name="count">number of elements to take</param>
		/// <returns>the result list</returns>
		public static IReadOnlyList<T> SkipLast<T>(this IReadOnlyList<T> list, int count)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
			if (count > list.Count)
				throw new ArgumentOutOfRangeException(nameof(count), count, Parameter.InvalidValue);

			if (list is T[] a)
			{
				return a[..^count];
			}
			var res = new T[list.Count - count];
			for (int i = 0; i < res.Length; i++)
			{
				res[i] = list[i];
			}
			return res;
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
		/// Reverse the order of <paramref name="list"/>
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="list">list to reverse</param>
		/// <returns>the result <see cref="IReadOnlyList{T}"/></returns>
		public static IReadOnlyList<T> Reverse<T>(this IReadOnlyList<T> list)
		{
			if (list is T[] a)
			{
				var copy = (T[])a.Clone();
				Array.Reverse(copy);
				return copy;
			}
			else if (list is List<T> l)
			{
				var copy = l.ToArray();
				Array.Reverse(copy);
				return copy;
			}
			var res = new T[list.Count];
			for (int i = 0; i < list.Count; i++)
			{
				res[list.Count - i - 1] = list[i];
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

		/// <summary>
		/// Convert the input <see cref="IReadOnlyList{T}"/> <paramref name="list"/> to a <see cref="List{T}"/>
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="list">list to convert</param>
		/// <returns><paramref name="list"/> itself if it is a <see cref="List{T}"/> or a copied <see cref="List{T}"/></returns>
		public static List<T> ToList<T>(this IReadOnlyList<T> list)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));

			if (list is List<T> a)
				return a;
			return new List<T>(list);
		}
		#endregion

		#region sort
		/// <summary>
		/// Sort <paramref name="list"/> directly
		/// </summary>
		/// <typeparam name="T">The input type</typeparam>
		/// <param name="list">The input list</param>
		/// <returns>the ordered list</returns>
		public static IOrderedList<T> Sort<T>(this IReadOnlyList<T> list) where T : IComparable<T>
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));

			if (list.Count <= 1)
				return new OrderedList<T>(list.ToArray(), true);
			var array = list.ToCopiedArray();
			Array.Sort(array);
			return new OrderedList<T>(array, true);
		}

		/// <summary>
		/// Sort <paramref name="list"/> directly
		/// </summary>
		/// <typeparam name="T">The input type</typeparam>
		/// <param name="list">The input list</param>
		/// <returns>the descending ordered list</returns>
		public static IOrderedList<T> SortByDescending<T>(this IReadOnlyList<T> list) where T : IComparable<T>
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));

			if (list.Count <= 1)
				return new OrderedList<T>(list.ToArray(), false);
			var array = list.ToCopiedArray();
			Array.Sort(array);
			Array.Reverse(array);
			return new OrderedList<T>(array, false);
		}


		/// <summary>
		/// Index the distinct element(s) in <paramref name="list"/> using the <see cref="IComparable{T}"/> interface to sort such that <c><paramref name="list"/>.<see cref="Distinct{T}(IReadOnlyList{T}, IEqualityComparer{T})">Distinct</see>().<see cref="Array.Sort{T}(T[])">Sort</see>().<see cref="ReOrder{T}(IReadOnlyList{T}, int[])">ReOrder</see>(result) == <paramref name="list"/></c>.
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="list">list to pick</param>
		/// <returns>the result indices as an <see cref="int"/> array</returns>
		public static int[] ToDistinctIndex<T>(this IReadOnlyList<T> list) where T : IComparable<T>
		{
			// sort distinct array
			var sorted = list.Distinct().ToArray();
			Array.Sort(sorted);
			// find permutation from distinct list to sorted list
			return sorted.FindPermutationOfSorted(list);
		}

		/// <summary>
		/// Order <paramref name="list"/> with key generated by <paramref name="selector"/>
		/// </summary>
		/// <typeparam name="T">The input type</typeparam>
		/// <typeparam name="TOut">The selector output type that can be compared</typeparam>
		/// <param name="list">The input list</param>
		/// <param name="selector"></param>
		/// <returns>the ordered list</returns>
		public static IReadOnlyList<T> OrderBy<T, TOut>(this IReadOnlyList<T> list, Converter<T, TOut> selector) where TOut : IComparable<TOut>
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
			if (selector is null)
				throw new ArgumentNullException(nameof(selector));

			if (list.Count <= 1)
				return list;
			var keys = list.Select(selector).ToArray();
			var array = list.ToCopiedArray();
			Array.Sort(keys, array);
			return array;
		}

		/// <summary>
		/// Descend order <paramref name="list"/> with key generated by <paramref name="selector"/>
		/// </summary>
		/// <typeparam name="T">The input type</typeparam>
		/// <typeparam name="TOut">The selector output type that can be compared</typeparam>
		/// <param name="list">The input list</param>
		/// <param name="selector"></param>
		/// <returns>the descending ordered list</returns>
		public static IReadOnlyList<T> OrderByDescending<T, TOut>(this IReadOnlyList<T> list, Converter<T, TOut> selector) where TOut : IComparable<TOut>
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
			if (selector is null)
				throw new ArgumentNullException(nameof(selector));

			if (list.Count <= 1)
				return list;
			var keys = list.Select(selector).ToArray();
			var array = list.ToCopiedArray();
			Array.Sort(keys, array);
			return array.Reverse();
		}
		#endregion

		#region array sort and index
		/// <summary>
		/// Sort (unstable) the <paramref name="items"/> by <paramref name="keys"/> <b>in-place</b>. Equivalent to <see cref="Array.Sort{TKey, TValue}(TKey[], TValue[])"/>.
		/// </summary>
		/// <typeparam name="TKey">The type of the elements of the <paramref name="keys"/>.</typeparam>
		/// <typeparam name="TItem">The type of the elements of the <paramref name="items"/>.</typeparam>
		/// <param name="keys">The one-dimensional, zero-based array that contains the keys to sort.</param>
		/// <param name="items">The one-dimensional, zero-based array that contains the items that correspond to the keys in keys, or null to sort only keys.</param>
		public static void SortWith<TKey, TItem>(this TKey[] keys, TItem[] items) where TKey : IComparable<TKey>
		{
			Array.Sort(keys, items);
		}

		/// <summary>
		/// Sort (unstable) the <paramref name="items"/> (<b>in-place</b>) by <paramref name="keys"/> (<b>not changed</b>).
		/// </summary>
		/// <typeparam name="TKey">The type of the elements of the <paramref name="keys"/>.</typeparam>
		/// <typeparam name="TItem">The type of the elements of the <paramref name="items"/>.</typeparam>
		/// <param name="keys">The one-dimensional, zero-based array that contains the keys to sort. Will not be altered.</param>
		/// <param name="items">The one-dimensional, zero-based array that contains the items that correspond to the keys in keys, or null to sort only keys.</param>
		public static void SortCopyWith<TKey, TItem>(this TKey[] keys, TItem[] items) where TKey : IComparable<TKey>
		{
			keys = (TKey[])keys.Clone();
			Array.Sort(keys, items);
		}

		/// <summary>
		/// Sort (unstable) the <paramref name="keys"/> <b>in-place</b> and return the permutation order from original array to sorted array.
		/// </summary>
		/// <typeparam name="T">The type of the elements of the <paramref name="keys"/>.</typeparam>
		/// <param name="keys">The one-dimensional, zero-based array that contains the keys to sort.</param>
		/// <returns>The permutation array as an <see cref="int"/> array such that <c><paramref name="keys"/>[result] == sorted_<paramref name="keys"/></c></returns>
		public static int[] SortWithIndex<T>(this T[] keys) where T : IComparable<T>
		{
			if (keys is null || keys.Length == 0)
				throw new ArgumentNullException(nameof(keys));

			int[] perm = Range(0, keys.Length).ToArray();
			Array.Sort(keys: keys, items: perm);
			return perm;
		}

		/// <summary>
		/// Sort (stable) the <paramref name="items"/> by <paramref name="keys"/> <b>in-place</b>.
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
			var newKeys = Array.ConvertAll(temp, static t => t.First);
			var newItems = Array.ConvertAll(temp, static t => t.Second);
			Array.Copy(sourceArray: newKeys, destinationArray: keys, length: keys.Length);
			Array.Copy(sourceArray: newItems, destinationArray: items, length: items.Length);
		}

		/// <summary>
		/// Sort (stable) the <paramref name="keys"/> <b>in-place</b> and return the permutation order from original array to sorted array.
		/// </summary>
		/// <typeparam name="T">The type of the elements of the <paramref name="keys"/>.</typeparam>
		/// <param name="keys">The one-dimensional, zero-based array that contains the keys to sort.</param>
		/// <param name="inPlace">In-place alter <paramref name="keys"/> or not. If false, only the permutation will be returned.</param>
		/// <returns>The permutation array as an <see cref="int"/> array such that <c><paramref name="keys"/>[result] == sorted_<paramref name="keys"/></c></returns>
		public static int[] StableSortWithIndex<T>(this T[] keys, bool inPlace = true) where T : IComparable<T>
		{
			if (keys is null || keys.Length == 0)
				throw new ArgumentNullException(nameof(keys));

			var items = System.Linq.Enumerable.Range(0, keys.Length);
			var ordered = System.Linq.Enumerable.OrderBy(System.Linq.Enumerable.Zip(keys, items), static k => k.First);
			var temp = System.Linq.Enumerable.ToArray(ordered);
			if (inPlace)
			{
				var newKeys = Array.ConvertAll(temp, static t => t.First);
				Array.Copy(sourceArray: newKeys, destinationArray: keys, length: keys.Length);
			}
			var newItems = Array.ConvertAll(temp, static t => t.Second);
			return newItems;
		}
		#endregion

		#region set operations
		/// <summary>
		/// Check whether <paramref name="set"/> and <paramref name="list"/> contains same elements
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="set">The set to check</param>
		/// <param name="list">The list to check</param>
		/// <param name="comparer">The <see cref="IEqualityComparer{T}"/> to use, default null means <see cref="EqualityComparer{T}.Default"/></param>
		/// <returns></returns>
		public static bool SetEquals<T>(this IImmutableSet<T> set, IReadOnlyList<T> list, IEqualityComparer<T>? comparer = null)
		{
			if (set is null)
				throw new ArgumentNullException(nameof(set));

			comparer ??= EqualityComparer<T>.Default;
			var distinct = list.ToImmutableSet(comparer);
			if (distinct.Count != list.Count)
				return false; // since list is not a set
			return set.Equals(distinct);
		}

		/// <summary>
		/// Check whether <paramref name="list1"/> and <paramref name="list2"/> contains same elements
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="list1">The first list to check</param>
		/// <param name="list2">The second list to check</param>
		/// <param name="comparer">The <see cref="IEqualityComparer{T}"/> to use, default null means <see cref="EqualityComparer{T}.Default"/></param>
		/// <returns></returns>
		public static bool SetEquals<T>(this IReadOnlyList<T> list1, IReadOnlyList<T> list2, IEqualityComparer<T>? comparer = null)
		{
			if (list1 is null)
				throw new ArgumentNullException(nameof(list1));

			comparer ??= EqualityComparer<T>.Default;
			var distinct = list2.ToImmutableSet(comparer);
			if (distinct.Count != list2.Count)
				return false; // since list is not a set
			return list1.ToImmutableSet(comparer).Equals(distinct);
		}

		/// <summary>
		/// Check if <paramref name="list"/>'s elements are unique
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="list">list to pick</param>
		/// <returns><paramref name="list"/>'s elements are unique or not</returns>
		public static bool ElementsUnique<T>(this IReadOnlyList<T> list) where T : IEquatable<T>
		{
			var res = new List<T>(list.Count);
			for (int i = 0; i < list.Count; i++)
			{
				if (!res.Contains(list[i]))
					res.Add(list[i]);
				else
					return false;
			}
			return true;
		}

		/// <summary>
		/// Check if <paramref name="list"/>'s elements are unique by given <paramref name="selector"/>
		/// </summary>
		/// <typeparam name="TFrom">The data type of <paramref name="list"/></typeparam>
		/// <typeparam name="TTo">The data type of output</typeparam>
		/// <param name="list">list to pick</param>
		/// <param name="selector">The selector to convert each element in <paramref name="list"/></param>
		/// <returns><paramref name="list"/>'s elements are unique or not</returns>
		public static bool ElementsUnique<TFrom, TTo>(this IReadOnlyList<TFrom> list, Converter<TFrom, TTo> selector) where TTo : IEquatable<TTo>
		{
			var res = new List<TTo>(list.Count);
			for (int i = 0; i < list.Count; i++)
			{
				TTo v = selector(list[i]);
				if (!res.Contains(v))
					res.Add(v);
				else
					return false;
			}
			return true;
		}

		/// <summary>
		/// Check if <paramref name="list"/>'s elements are unique
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="list">list to pick</param>
		/// <param name="comparer">The <see cref="IEqualityComparer{T}"/> to use, default null means <see cref="EqualityComparer{T}.Default"/></param>
		/// <returns><paramref name="list"/>'s elements are unique or not</returns>
		public static bool ElementsUnique<T>(this IReadOnlyList<T> list, IEqualityComparer<T>? comparer = null)
		{
			comparer ??= EqualityComparer<T>.Default;
			var res = new List<T>(list.Count);
			for (int i = 0; i < list.Count; i++)
			{
				if (!res.Contains(list[i], comparer))
					res.Add(list[i]);
				else
					return false;
			}
			return true;
		}

		/// <summary>
		/// Pick the distinct element(s) in <paramref name="list"/>
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="list">list to pick</param>
		/// <param name="comparer">The <see cref="IEqualityComparer{T}"/> to use, default null means <see cref="EqualityComparer{T}.Default"/></param>
		/// <returns>the result <see cref="IReadOnlyList{T}"/></returns>
		public static IReadOnlyList<T> Distinct<T>(this IReadOnlyList<T> list, IEqualityComparer<T>? comparer = null)
		{
			comparer ??= EqualityComparer<T>.Default;
			var res = new List<T>(list.Count);
			for (int i = 0; i < list.Count; i++)
			{
				if (!res.Contains(list[i], comparer))
					res.Add(list[i]);
			}
			return res.ToArray();
		}

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
		/// Remove element(s) <c>e</c> in <paramref name="list"/> where <c>e</c> is in <paramref name="other"/> to form a new <see cref="IReadOnlyList{T}"/>
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="list">list to remove from</param>
		/// <param name="other">The list used to compare</param>
		/// <param name="comparer">The <see cref="IEqualityComparer{T}"/> to use, default null means <see cref="EqualityComparer{T}.Default"/></param>
		/// <returns>the result <see cref="IReadOnlyList{T}"/></returns>
		public static IReadOnlyList<T> Except<T>(this IReadOnlyList<T> list, IReadOnlyList<T> other, IEqualityComparer<T>? comparer = null)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
			if (other is null)
				throw new ArgumentNullException(nameof(other));

			comparer ??= EqualityComparer<T>.Default;
			var set1 = list.ToImmutableSet(comparer);
			var set2 = other.ToImmutableSet(comparer);
			return set1.ExceptWith(set2).ToArray();
		}

		/// <summary>
		/// Get the element(s) <c>e</c> in <paramref name="list"/> where <c>e</c> is also in <paramref name="other"/> to form a new <see cref="IReadOnlyList{T}"/>
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="list">list to get from</param>
		/// <param name="other">The list used to compare</param>
		/// <param name="comparer">The <see cref="IEqualityComparer{T}"/> to use, default null means <see cref="EqualityComparer{T}.Default"/></param>
		/// <returns>the result <see cref="IReadOnlyList{T}"/></returns>
		public static IReadOnlyList<T> Intersect<T>(this IReadOnlyList<T> list, IReadOnlyList<T> other, IEqualityComparer<T>? comparer = null)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
			if (other is null)
				throw new ArgumentNullException(nameof(other));

			comparer ??= EqualityComparer<T>.Default;
			var set1 = list.ToImmutableSet(comparer);
			var set2 = other.ToImmutableSet(comparer);
			return set1.IntersectWith(set2).ToArray();
		}

		/// <summary>
		/// Get the union list of <paramref name="list"/> and <paramref name="other"/> to form a new <see cref="IReadOnlyList{T}"/>
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="list">first list</param>
		/// <param name="other">second list</param>
		/// <param name="comparer">The <see cref="IEqualityComparer{T}"/> to use, default null means <see cref="EqualityComparer{T}.Default"/></param>
		/// <returns>the result <see cref="IReadOnlyList{T}"/></returns>
		public static IReadOnlyList<T> Union<T>(this IReadOnlyList<T> list, IReadOnlyList<T> other, IEqualityComparer<T>? comparer = null)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
			if (other is null)
				throw new ArgumentNullException(nameof(other));

			comparer ??= EqualityComparer<T>.Default;
			var set1 = list.ToImmutableSet(comparer);
			var set2 = other.ToImmutableSet(comparer);
			return set1.UnionWith(set2).ToArray();
		}

		/// <summary>
		/// Group the given <paramref name="list"/> by <paramref name="keySelector"/> and the values of groups are given by the values in <paramref name="list"/>.
		/// </summary>
		/// <typeparam name="T">The input type</typeparam>
		/// <typeparam name="TKey">The key type</typeparam>
		/// <param name="list">The input list to group</param>
		/// <param name="keySelector">The converter from <typeparamref name="T"/> to <typeparamref name="TKey"/></param>
		/// <returns>an <see cref="IReadOnlyList{T}"/> of <see cref="IReadOnlyGrouping{TKey, TElement}"/>s</returns>
		public static IReadOnlyList<IReadOnlyGrouping<TKey, T>> GroupBy<T, TKey>(this IReadOnlyList<T> list, Converter<T, TKey> keySelector)
		{
			static T identity(T input) => input;
			return list.GroupBy(keySelector, identity);
		}

		/// <summary>
		/// Group the given <paramref name="list"/> by <paramref name="keySelector"/> and the values of groups are given by <paramref name="valueSelector"/>.
		/// </summary>
		/// <typeparam name="T">The input type</typeparam>
		/// <typeparam name="TKey">The key type</typeparam>
		/// <typeparam name="TValue">The value type</typeparam>
		/// <param name="list">The input list to group</param>
		/// <param name="keySelector">The converter from <typeparamref name="T"/> to <typeparamref name="TKey"/></param>
		/// <param name="valueSelector">The converter from <typeparamref name="T"/> to <typeparamref name="TValue"/></param>
		/// <returns>an <see cref="IReadOnlyList{T}"/> of <see cref="IReadOnlyGrouping{TKey, TElement}"/>s</returns>
		public static IReadOnlyList<IReadOnlyGrouping<TKey, TValue>> GroupBy<T, TKey, TValue>(this IReadOnlyList<T> list, Converter<T, TKey> keySelector, Converter<T, TValue> valueSelector)
		{
			if (keySelector is null)
				throw new ArgumentNullException(nameof(keySelector));
			if (valueSelector is null)
				throw new ArgumentNullException(nameof(valueSelector));

			List<TKey> keys = new();
			List<List<TValue>> values = new();
			for (int i = 0; i < list.Count; i++)
			{
				var key = keySelector(list[i]);
				int find = keys.IndexOf(key);
				if (find >= 0)
				{
					values[find].Add(valueSelector(list[i]));
				}
				else
				{
					keys.Add(key);
					values.Add(new List<TValue> { valueSelector(list[i]) });
				}
			}
			ReadOnlyGrouping<TKey, TValue>[] result = new ReadOnlyGrouping<TKey, TValue>[keys.Count];
			for (int i = 0; i < result.Length; i++)
			{
				result[i] = new ReadOnlyGrouping<TKey, TValue>(keys[i], values[i].ToArray());
			}
			return result;
		}
		#endregion

		#region permutation
		/// <summary>
		/// Find the permutation order such that <c><paramref name="array"/>[result] = <paramref name="target"/></c>
		/// </summary>
		/// <typeparam name="T">any type</typeparam>
		/// <param name="array">The array before permutation</param>
		/// <param name="target">The array after the permutation</param>
		/// <returns>the permutation order as an <see cref="int"/> array, or empty array if <c>∃ a∈<paramref name="target"/>, a∉<paramref name="array"/></c></returns>
		public static int[] FindPermutation<T>(this IReadOnlyList<T> array, IReadOnlyList<T> target)
		{
			if (array is null)
				throw new ArgumentNullException(nameof(array));
			if (target is null || target.Count == 0)
				throw new ArgumentNullException(nameof(target));

			var arr = array.ToArray();
			int[] order = new int[target.Count];
			for (int i = 0; i < target.Count; i++)
			{
				order[i] = Array.IndexOf(arr, target[i]);
				if (order[i] < 0)
					return Array.Empty<int>(); // cannot find permutation
			}
			return order;
		}

		/// <summary>
		/// Find the permutation order such that <c><paramref name="sorted"/>[result] = <paramref name="target"/></c>.
		/// </summary>
		/// <typeparam name="T">any type</typeparam>
		/// <param name="sorted">The array before permutation. must be sorted</param>
		/// <param name="target">The array after the permutation</param>
		/// <returns>the permutation order as an <see cref="int"/> array, or empty array if <c>∃ a∈<paramref name="target"/>, a∉<paramref name="sorted"/></c></returns>
		public static int[] FindPermutationOfSorted<T>(this IReadOnlyList<T> sorted, IReadOnlyList<T> target)
		{
			if (sorted is null)
				throw new ArgumentNullException(nameof(sorted));
			if (target is null || target.Count == 0)
				throw new ArgumentNullException(nameof(target));

			var arr = sorted.ToArray();
			int[] order = new int[target.Count];
			for (int i = 0; i < target.Count; i++)
			{
				order[i] = Array.BinarySearch(arr, target[i]);
				if (order[i] < 0)
					return Array.Empty<int>(); // cannot find permutation
			}
			return order;
		}

		/// <summary>
		/// Re-order the <paramref name="array"/> by <paramref name="indices"/> out-of-place such that <c><paramref name="array"/>[indices] = result</c>.
		/// </summary>
		/// <typeparam name="T">any type</typeparam>
		/// <param name="array">The array to order</param>
		/// <param name="indices">The indices to order, if this is null or empty, <paramref name="array"/> will be returned</param>
		/// <returns>the re-ordered array</returns>
		public static T[] ReOrder<T>(this IReadOnlyList<T> array, params int[] indices)
		{
			if (array is null)
				throw new ArgumentNullException(nameof(array));
			if (indices is null || indices.Length == 0)
				return array.ToArray();

			var output = new T[indices.Length];
			for (int i = 0; i < indices.Length; i++)
			{
				output[i] = array[indices[i]];
			}
			return output;
		}

		/// <summary>
		/// Re-order the <paramref name="array"/> by <paramref name="indices"/> out-of-place such that <c><paramref name="array"/>[indices] = result</c>.
		/// </summary>
		/// <typeparam name="T">any type</typeparam>
		/// <param name="array">The array to order</param>
		/// <param name="indices">The indices to order, if this is null or empty, <paramref name="array"/> will be returned</param>
		/// <returns>the re-ordered array</returns>
		public static T[] ReOrder<T>(this IReadOnlyList<T> array, IReadOnlyList<int> indices)
		{
			if (array is null)
				throw new ArgumentNullException(nameof(array));
			if (indices is null || indices.Count == 0)
				return array.ToArray();

			var output = new T[indices.Count];
			for (int i = 0; i < indices.Count; i++)
			{
				output[i] = array[indices[i]];
			}
			return output;
		}

		/// <summary>
		/// Inverse order the <paramref name="array"/> by <paramref name="indices"/> out-of-place such that <c>result[<paramref name="indices"/>] = <paramref name="array"/></c>.
		/// </summary>
		/// <typeparam name="T">any type</typeparam>
		/// <param name="array">The array to order</param>
		/// <param name="indices">The indices to order, may has less elements than <paramref name="array"/></param>
		/// <returns>the re-ordered array</returns>
		public static T[] InverseOrder<T>(this IReadOnlyList<T> array, IReadOnlyList<int> indices)
		{
			if (array is null)
				throw new ArgumentNullException(nameof(array));
			if (indices is null || indices.Count == 0)
				throw new ArgumentNullException(nameof(indices));
			int N = indices.Max() + 1;
			if (N > array.Count)
				throw new ArgumentOutOfRangeException(nameof(indices), indices, Parameter.InvalidValue);

			var output = new T[N];
			for (int i = 0; i < N; i++)
			{
				output[indices[i]] = array[i];
			}
			return output;
		}

		/// <summary>
		/// Find the inverse permutation of <paramref name="perm"/> such that <c>perm[result] == result[perm] == identity permutation</c>
		/// </summary>
		/// <param name="perm">The input permutation</param>
		public static int[] InversePermutation(this IReadOnlyList<int> perm)
		{
			if (perm is null)
				throw new ArgumentNullException(nameof(perm));
			int[] inv = new int[perm.Count];
			for (int i = 0; i < perm.Count; i++)
			{
				inv[perm[i]] = i;
			}
			return inv;
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
		/// Convert <paramref name="list"/> to an <see cref="IImmutableSet{T}"/> by removing duplicate element(s)
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="list">list to convert</param>
		/// <param name="comparer">The <see cref="IEqualityComparer{T}"/> to use, default null means <see cref="EqualityComparer{T}.Default"/></param>
		/// <returns>the result <see cref="IImmutableSet{T}"/></returns>
		public static IImmutableSet<T> ToImmutableSet<T>(this IReadOnlyList<T> list, IEqualityComparer<T>? comparer = null)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
			if (list is IImmutableSet<T> s)
				return s;
			var distinct = list.Distinct(comparer);
			return new ImmutableSet<T>(distinct);
		}

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

		/// <summary>
		/// Act on each element of a list.
		/// </summary>
		/// <typeparam name="T">any type</typeparam>
		/// <param name="list">input list</param>
		/// <param name="action">action whose parameters are the value and the index of the array respectively</param>
		public static void ForEach<T>(this IReadOnlyList<T> list, Action<T, int> action)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
			for (int i = 0; i < list.Count; i++)
			{
				action(list[i], i);
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

		#region range
		/// <summary>
		/// Generate a <see cref="IReadOnlyList{T}"/> by repeating <paramref name="val"/> for <paramref name="count"/> times
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="count">The count to repeat</param>
		/// <param name="val">The value to repeat</param>
		/// <returns>the result <see cref="IReadOnlyList{T}"/></returns>
		public static IReadOnlyList<T> Repeat<T>(T val, int count)
		{
			if (count < 0)
				throw new ArgumentOutOfRangeException(nameof(count), count, Parameter.CannotNegative);
			if (count == 0)
				return Array.Empty<T>();
			var res = new T[count];
			for (int i = 0; i < count; i++)
			{
				res[i] = val;
			}
			return res;
		}

		/// <summary>
		/// Generate generic-typed (<typeparamref name="T"/>) range as an <see cref="IReadOnlyList{T}"/> with.
		/// </summary>
		/// <typeparam name="T">The data type that must be able to self increment (operator <c>++</c> or <c>+=</c>)</typeparam>
		/// <param name="start">start value of the range</param>
		/// <param name="count">count of the range</param>
		/// <param name="step">step, default means use operator <c>++</c>, otherwise, use operator <c>+=</c></param>
		/// <returns><see cref="IReadOnlyList{T}"/></returns>
		/// <exception cref="InvalidOperationException">if <typeparamref name="T"/> dose not have operator <c>++</c> or <c>+=</c></exception>
		public static IReadOnlyList<T> Range<T>(T start, int count, T step = default) where T : struct, IComparable<T>
		{
			bool selfAdd = step.CompareTo(default) == 0;
			try
			{
				dynamic a = start;
				if (selfAdd)
					a++;
				else
					a += step;
			}
			catch (System.Exception)
			{
				throw new InvalidOperationException(Other.CannotAdd);
			}
			var res = new T[count];
			dynamic s = start;
			for (int i = 0; i < count; i++)
			{
				res[i] = s;
				if (selfAdd)
					s++;
				else
					s += step;
			}
			return res;
		}

		/// <summary>
		/// Generate range of type <see cref="long"/> <see cref="IReadOnlyList{T}"/>.
		/// </summary>
		/// <param name="start">start value of the range</param>
		/// <param name="count">count of the range</param>
		/// <param name="step">step of the range</param>
		/// <returns><see cref="IReadOnlyList{T}"/> with <c>T</c> is <see cref="long"/></returns>
		public static IReadOnlyList<long> Range(long start, long count, long step = 1)
		{
			if (step == 0)
				throw new ArgumentOutOfRangeException(nameof(step), step, Parameter.CannotZero);
			var res = new long[count];
			for (long i = 0; i < count; i++)
			{
				res[i] = i * step + start;
			}
			return res;
		}

		/// <summary>
		/// Generate range of type <see cref="int"/> <see cref="IReadOnlyList{T}"/>.
		/// </summary>
		/// <param name="start">start value of the range</param>
		/// <param name="count">count of the range</param>
		/// <param name="step">step of the range</param>
		/// <returns><see cref="IReadOnlyList{T}"/> with <c>T</c> is <see cref="int"/></returns>
		public static IReadOnlyList<int> Range(int start, int count, int step = 1)
		{
			if (step == 0)
				throw new ArgumentOutOfRangeException(nameof(step), step, Parameter.CannotZero);
			var res = new int[count];
			for (int i = 0; i < count; i++)
			{
				res[i] = i * step + start;
			}
			return res;
		}

		/// <summary>
		/// Generate range of type <see cref="char"/> <see cref="IReadOnlyList{T}"/>.
		/// </summary>
		/// <param name="start">start value of the range</param>
		/// <param name="count">count of the range</param>
		/// <param name="step">step of the range</param>
		/// <returns><see cref="IReadOnlyList{T}"/> with <c>T</c> is <see cref="char"/></returns>
		public static IReadOnlyList<char> Range(char start, int count, int step = 1)
		{
			if (step == 0)
				throw new ArgumentOutOfRangeException(nameof(step), step, Parameter.CannotZero);

			var res = new char[count];
			for (int i = 0; i < count; i++)
			{
				res[i] = checked((char)(i * step + start));
			}
			return res;
		}
		#endregion
	}
}
