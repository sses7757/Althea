using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;


namespace Althea.Linq
{
	#region immutable set
	/// <summary>
	/// The interface for immutable set
	/// </summary>
	/// <typeparam name="T">the data type</typeparam>
	public interface IImmutableSet<T> : IReadOnlyList<T>, IEquatable<IImmutableSet<T>>
	{
		/// <summary>
		/// Remove all elements in the specified set from the current set.
		/// </summary>
		/// <param name="other">The collection of items to remove from the set.</param>
		/// <param name="comparer">the <see cref="IEqualityComparer{T}"/> to use, default null means <see cref="EqualityComparer{T}.Default"/></param>
		/// <returns>A new set as the result</returns>
		IImmutableSet<T> ExceptWith(IImmutableSet<T> other, IEqualityComparer<T> comparer = null);

		/// <summary>
		/// Pick all elements in the specified set from the current set.
		/// </summary>
		/// <param name="other">The collection of items to intersect from the set.</param>
		/// <param name="comparer">the <see cref="IEqualityComparer{T}"/> to use, default null means <see cref="EqualityComparer{T}.Default"/></param>
		/// <returns>A new set as the result</returns>
		IImmutableSet<T> IntersectWith(IImmutableSet<T> other, IEqualityComparer<T> comparer = null);

		/// <summary>
		/// Generate a new set so that it contains all elements that are present in the current set, in the specified set, or in both.
		/// </summary>
		/// <param name="other">the other set</param>
		/// <param name="comparer">the <see cref="IEqualityComparer{T}"/> to use, default null means <see cref="EqualityComparer{T}.Default"/></param>
		/// <returns>A new set as the result</returns>
		IImmutableSet<T> UnionWith(IImmutableSet<T> other, IEqualityComparer<T> comparer = null);

		/// <summary>
		/// Convert this set to an array
		/// </summary>
		/// <returns>A new array containing the elements</returns>
		T[] ToArray();
	}

	internal sealed class ImmutableSet<T> : IImmutableSet<T>, IEquatable<ImmutableSet<T>>
	{
		#region basic
		private readonly T[] data;

		private readonly int hash;

		public T this[int index] => this.data[index];

		public int Count => this.data.Length;

		internal ImmutableSet(IReadOnlyList<T> data)
		{
			this.data = data.ToArray();
			this.hash = data.HashCodeOfSet();
		}
		#endregion

		#region equality
		public bool Equals(ImmutableSet<T> other)
		{
			if ((this is null) != (other is null))
				return false;
			if (this is null && other is null)
				return true;
			if (this.data.Length != other.data.Length)
				return false;
			if (this.hash != other.hash)
				return false;
			return this.ExceptWith(other, EqualityComparer<T>.Default).Count == 0;
		}

		public bool Equals(IImmutableSet<T> other)
		{
			if ((this is null) != (other is null))
				return false;
			if (this is null && other is null)
				return true;
			if (this.data.Length != other.Count)
				return false;
			return this.ExceptWith(other, EqualityComparer<T>.Default).Count == 0;
		}

		public override bool Equals(object obj)
		{
			return this.Equals(obj as ImmutableSet<T>);
		}

		public override int GetHashCode()
		{
			return this.hash;
		}
		#endregion

		#region set op
		public IImmutableSet<T> ExceptWith(IImmutableSet<T> other, IEqualityComparer<T> comparer)
		{
			if (other is null)
				throw new ArgumentNullException(nameof(other));
			var res = new List<T>(this.Count);
			for (int i = 0; i < this.data.Length; i++)
			{
				if (!other.Contains(this.data[i], comparer))
					res.Add(this.data[i]);
			}
			return new ImmutableSet<T>(res.ToArray());
		}

		public IImmutableSet<T> IntersectWith(IImmutableSet<T> other, IEqualityComparer<T> comparer)
		{
			if (other is null)
				throw new ArgumentNullException(nameof(other));
			var res = new List<T>(this.Count);
			for (int i = 0; i < this.data.Length; i++)
			{
				if (other.Contains(this.data[i], comparer))
					res.Add(this.data[i]);
			}
			return new ImmutableSet<T>(res.ToArray());
		}

		public IImmutableSet<T> UnionWith(IImmutableSet<T> other, IEqualityComparer<T> comparer)
		{
			if (other is null)
				throw new ArgumentNullException(nameof(other));
			var except = other.ExceptWith(this, comparer) as ImmutableSet<T>;
			var res = new T[this.Count + except.Count];
			Array.Copy(this.data, res, this.Count);
			Array.Copy(except.data, 0, res, this.Count, except.Count);
			return new ImmutableSet<T>(res);
		}
		#endregion

		#region other
		public T[] ToArray()
		{
			return this.data.Clone() as T[];
		}

		public IEnumerator<T> GetEnumerator()
		{
			for (int i = 0; i < this.data.Length; i++)
			{
				yield return this.data[i];
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}
		#endregion
	}
	#endregion

	#region ordered list
	/// <summary>
	/// The interface for ordered list
	/// </summary>
	/// <typeparam name="T">any comparable data type</typeparam>
	public interface IOrderedList<T> : IReadOnlyList<T> where T : IComparable<T>
	{
		/// <summary>
		/// Whether this list is ordered ascendingly or descendingly
		/// </summary>
		bool Ascending { get; }

		/// <summary>
		/// Binary search <paramref name="value"/> to get its index of occurrence
		/// </summary>
		/// <param name="value">the value to search</param>
		/// <returns>the index of occurrence</returns>
		int BinarySearch(T value);

		/// <summary>
		/// Calculate the multiplicities of values in this ordered list
		/// </summary>
		/// <returns>the multiplicities of values as a list of <see cref="int"/></returns>
		IReadOnlyList<int> Multiplicities();
	}

	internal sealed class OrderedList<T> : IOrderedList<T> where T : IComparable<T>
	{
		private readonly T[] array;

		public bool Ascending { get; }

		internal OrderedList(T[] array, bool ascend)
		{
			this.array = array; this.Ascending = ascend;
		}

		public int Count => this.array.Length;

		public T this[int index] => this.array[index];

		public int BinarySearch(T value)
		{
			return Array.BinarySearch(this.array, value);
		}

		private int[] _multiplicity = null;

		public IReadOnlyList<int> Multiplicities()
		{
			if (!(this._multiplicity is null))
				return this._multiplicity;
			var mul = new List<int>(this.array.Length);
			T now = this.array[0];
			int mNow = 1;
			for (int i = 1; i < this.array.Length; i++)
			{
				if (this.array[i].CompareTo(now) != 0)
				{
					mul.Add(mNow);
					mNow = 0;
					now = this.array[i];
				}
				else
				{
					mNow++;
				}
			}
			this._multiplicity = mul.ToArray();
			return this._multiplicity;
		}

		public IEnumerator<T> GetEnumerator()
		{
			return (this.array as IReadOnlyList<T>).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.array.GetEnumerator();
		}
	}
	#endregion

	#region immutable grouping
	/// <summary>
	/// The interface for read-only grouping that represents a list of objects that have a common key. Similar as <see cref="System.Linq.IGrouping{TKey, TElement}"/>
	/// </summary>
	/// <typeparam name="TKey">The type of the key</typeparam>
	/// <typeparam name="TElement">The type of the values</typeparam>
	public interface IReadOnlyGrouping<TKey, TElement> : IReadOnlyList<TElement>
	{
		/// <summary>
		/// Gets the key of this <see cref="IReadOnlyGrouping{TKey, TElement}"/>
		/// </summary>
		TKey Key { get; }
	}

	internal readonly struct ReadOnlyGrouping<TKey, TElement> : IReadOnlyGrouping<TKey, TElement>
	{
		public TKey Key { get; }

		private readonly TElement[] values;

		internal ReadOnlyGrouping(TKey key, TElement[] values)
		{
			this.Key = key;
			this.values = values;
		}

		public int Count => this.values.Length;

		public TElement this[int index] => this.values[index];

		public IEnumerator<TElement> GetEnumerator() => ((IReadOnlyList<TElement>)this.values).GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => this.values.GetEnumerator();
	}
	#endregion

	#region delegates
	/// <summary>
	/// Represents a method that aggregate <paramref name="value"/> and <paramref name="aggergatedValue"/>.
	/// </summary>
	/// <typeparam name="T">the input type</typeparam>
	/// <typeparam name="TAggergate">the aggregated value's type</typeparam>
	/// <param name="value">the value to be aggregated</param>
	/// <param name="aggergatedValue">the previously aggregated value to be aggregated with <paramref name="value"/></param>
	/// <returns>the aggregation of <paramref name="value"/> and <paramref name="aggergatedValue"/></returns>
	public delegate TAggergate Aggregator<in T, TAggergate>(T value, TAggergate aggergatedValue);

	/// <summary>
	/// Represents a method that check if the two input values are the same.
	/// </summary>
	/// <typeparam name="TL">the left input type</typeparam>
	/// <typeparam name="TR">the right input type</typeparam>
	/// <param name="left">the left value to compare</param>
	/// <param name="right">the right value to compare</param>
	/// <returns><paramref name="left"/> equals <paramref name="right"/> or not</returns>
	public delegate bool EqualComparer<in TL, in TR>(TL left, TR right);

	/// <summary>
	/// Represents a method that receives a <paramref name="value"/> of a list and the corresponding <paramref name="index"/> and outputs the convert result.
	/// </summary>
	/// <typeparam name="TIn">the input value's type</typeparam>
	/// <typeparam name="TOut">the output type</typeparam>
	/// <param name="value">the value</param>
	/// <param name="index">the corresponding index</param>
	/// <returns>the converted value as a <typeparamref name="TOut"/></returns>
	public delegate TOut Selector<in TIn, out TOut>(TIn value, int index);

	/// <summary>
	/// Represents a method that receives a <paramref name="value"/> of a list and the corresponding <paramref name="index"/> and outputs the predication result.
	/// </summary>
	/// <typeparam name="TIn">the input value's type</typeparam>
	/// <param name="value">the value</param>
	/// <param name="index">the corresponding index</param>
	/// <returns>the predication result as a <see cref="bool"/></returns>
	public delegate bool IndexPredicator<in TIn>(TIn value, int index);

	/// <summary>
	/// Represents a method that converts two objects of possible different types to another type.
	/// </summary>
	/// <typeparam name="T1">the first input's type</typeparam>
	/// <typeparam name="T2">the second input's type</typeparam>
	/// <typeparam name="TOut">the output type</typeparam>
	/// <param name="input1">the first input parameter to be converted</param>
	/// <param name="input2">the second input parameter to be converted</param>
	/// <returns>the converted output as a <typeparamref name="TOut"/></returns>
	public delegate TOut ZipConverter<in T1, in T2, out TOut>(T1 input1, T2 input2);

	/// <summary>
	/// Represents a method that converts three objects of possible different types to another type.
	/// </summary>
	/// <typeparam name="T1">the first input's type</typeparam>
	/// <typeparam name="T2">the second input's type</typeparam>
	/// <typeparam name="T3">the third input's type</typeparam>
	/// <typeparam name="TOut">the output type</typeparam>
	/// <param name="input1">the first input parameter to be converted</param>
	/// <param name="input2">the second input parameter to be converted</param>
	/// <param name="input3">the third input parameter to be converted</param>
	/// <returns>the converted output as a <typeparamref name="TOut"/></returns>
	public delegate TOut ZipConverter<in T1, in T2, in T3, out TOut>(T1 input1, T2 input2, T3 input3);
	#endregion

	/// <summary>
	/// A replacement of <see cref="System.Linq.Enumerable"/> to reduce GC stress.<br/>
	/// Most methods of this class is based on <see cref="IReadOnlyList{T}"/> and is implemented by <see cref="Array"/>.
	/// </summary>
	public static class ArrayLinq
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
			if (list is null)
				throw new ArgumentNullException(nameof(list));
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
		/// <typeparam name="T">the input data type</typeparam>
		/// <typeparam name="TOut">data type of array that can be compared</typeparam>
		/// <param name="list">list to find maximum</param>
		/// <param name="selector">the selector used to convert <typeparamref name="T"/> to <typeparamref name="TOut"/></param>
		/// <returns>the maximum item</returns>
		public static TOut Max<T, TOut>(this IReadOnlyList<T> list, Converter<T, TOut> selector) where TOut : IComparable<TOut>
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
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
			if (list is null)
				throw new ArgumentNullException(nameof(list));
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
		/// <typeparam name="T">the input data type</typeparam>
		/// <typeparam name="TOut">data type of array that can be compared</typeparam>
		/// <param name="list">list to find minimum</param>
		/// <param name="selector">the selector used to convert <typeparamref name="T"/> to <typeparamref name="TOut"/></param>
		/// <returns>the minimum item</returns>
		public static TOut Min<T, TOut>(this IReadOnlyList<T> list, Converter<T, TOut> selector) where TOut : IComparable<TOut>
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
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
		/// <typeparam name="TSort">the data type that can be compared</typeparam>
		/// <param name="list">list to find maximum</param>
		/// <param name="selector">conversion function</param>
		/// <returns>the maximum item</returns>
		public static T MaxBy<T, TSort>(this IReadOnlyList<T> list, Converter<T, TSort> selector) where TSort : IComparable<TSort>
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
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
		/// <typeparam name="TSort">the data type that can be compared</typeparam>
		/// <param name="list">list to find minimum</param>
		/// <param name="selector">conversion function</param>
		/// <returns>the minimum item</returns>
		public static T MinBy<T, TSort>(this IReadOnlyList<T> list, Converter<T, TSort> selector) where TSort : IComparable<TSort>
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
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
		/// <param name="list">the list to accumulate</param>
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
		/// <param name="list">the list to accumulate</param>
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
		/// <param name="list">the list to accumulate</param>
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
		/// <param name="list">the list to accumulate</param>
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
		/// <param name="list">the list to accumulate</param>
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
		/// <param name="list">the list to accumulate</param>
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
		/// <param name="list">the list to accumulate</param>
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
		/// <param name="list">the list to accumulate</param>
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
		/// <param name="list">the list to accumulate</param>
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
		/// <param name="list">the list to accumulate</param>
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
		/// <param name="list">the list to accumulate</param>
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

		/// <summary>
		/// Complex list summation
		/// </summary>
		/// <param name="list"></param>
		/// <returns>Summation result, 0 if <paramref name="list"/> is null</returns>
		/// <remarks>extend method of <paramref name="list"/></remarks>
		public static FloatComplex Sum(this IReadOnlyList<FloatComplex> list)
		{
			if (list is null || list.Count == 0)
				return 0;
			FloatComplex prod = 1;
			for (int i = 0; i < list.Count; i++)
			{
				prod *= list[i];
			}
			return prod;
		}

		/// <summary>
		/// Complex list summation
		/// </summary>
		/// <param name="list"></param>
		/// <returns>Summation result, 0 if <paramref name="list"/> is null</returns>
		/// <remarks>extend method of <paramref name="list"/></remarks>
		public static DoubleComplex Sum(this IReadOnlyList<DoubleComplex> list)
		{
			if (list is null || list.Count == 0)
				return 0;
			DoubleComplex prod = 1;
			for (int i = 0; i < list.Count; i++)
			{
				prod *= list[i];
			}
			return prod;
		}
		#endregion

		#region concrete selector sum
		/// <summary>
		/// List summation by <paramref name="selector"/>
		/// </summary>
		/// <param name="list"></param>
		/// <param name="selector">the selector to apply to each element</param>
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
		/// <param name="selector">the selector to apply to each element</param>
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
		/// <param name="selector">the selector to apply to each element</param>
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
		/// <param name="selector">the selector to apply to each element</param>
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

		/// <summary>
		/// List summation by <paramref name="selector"/>
		/// </summary>
		/// <param name="list"></param>
		/// <param name="selector">the selector to apply to each element</param>
		/// <returns>Summation result, 0 if <paramref name="list"/> is null</returns>
		/// <remarks>extend method of <paramref name="list"/></remarks>
		public static FloatComplex Sum<T>(this IReadOnlyList<T> list, Converter<T, FloatComplex> selector)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
			FloatComplex sum = 0;
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
		/// <param name="selector">the selector to apply to each element</param>
		/// <returns>Summation result, 0 if <paramref name="list"/> is null</returns>
		/// <remarks>extend method of <paramref name="list"/></remarks>
		public static DoubleComplex Sum<T>(this IReadOnlyList<T> list, Converter<T, DoubleComplex> selector)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
			DoubleComplex sum = 0;
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
		/// <param name="list">the list to compare</param>
		/// <param name="other">the other list to compare</param>
		/// <returns>Sequentially equals or not</returns>
		public static bool SequenceEqual<T>(this IReadOnlyList<T> list, IReadOnlyList<T> other) where T : IEquatable<T>
		{
			if (object.ReferenceEquals(list, other))
				return true;
			if ((list is null) != (other is null))
				return false;
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
		/// <param name="list">the list to compare</param>
		/// <param name="other">the other list to compare</param>
		/// <param name="comparer">the <see cref="IEqualityComparer{T}"/> to use, default null means <see cref="EqualityComparer{T}.Default"/></param>
		/// <returns>Sequentially equals or not</returns>
		public static bool SequenceEqual<T>(this IReadOnlyList<T> list, IReadOnlyList<T> other, IEqualityComparer<T> comparer = null)
		{
			if (object.ReferenceEquals(list, other))
				return true;
			if ((list is null) != (other is null))
				return false;
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
		/// <typeparam name="TL">the left input type</typeparam>
		/// <typeparam name="TR">the right input type</typeparam>
		/// <param name="list">the list to compare</param>
		/// <param name="other">the other list to compare</param>
		/// <param name="equalityComparer">the function used to compare equality</param>
		/// <returns>Sequentially equals or not</returns>
		public static bool SequenceEqual<TL, TR>(this IReadOnlyList<TL> list, IReadOnlyList<TR> other, EqualComparer<TL, TR> equalityComparer)
		{
			if (object.ReferenceEquals(list, other))
				return true;
			if ((list is null) != (other is null))
				return false;
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
		/// <param name="list">the list to compare</param>
		/// <param name="other">the other list to compare</param>
		/// <returns>Sequentially equals or not</returns>
		public static bool SequenceEqual(this IReadOnlyList<byte> list, IReadOnlyList<byte> other)
		{
			if (object.ReferenceEquals(list, other))
				return true;
			if ((list is null) != (other is null))
				return false;
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
		/// <param name="list">the list to compare</param>
		/// <param name="other">the other list to compare</param>
		/// <returns>Sequentially equals or not</returns>
		public static bool SequenceEqual(this IReadOnlyList<char> list, IReadOnlyList<char> other)
		{
			if (object.ReferenceEquals(list, other))
				return true;
			if ((list is null) != (other is null))
				return false;
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
		/// <param name="list">the list to compare</param>
		/// <param name="other">the other list to compare</param>
		/// <returns>Sequentially equals or not</returns>
		public static bool SequenceEqual(this IReadOnlyList<int> list, IReadOnlyList<int> other)
		{
			if (object.ReferenceEquals(list, other))
				return true;
			if ((list is null) != (other is null))
				return false;
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
		/// <param name="list">the list to compare</param>
		/// <param name="other">the other list to compare</param>
		/// <returns>Sequentially equals or not</returns>
		public static bool SequenceEqual(this IReadOnlyList<long> list, IReadOnlyList<long> other)
		{
			if (object.ReferenceEquals(list, other))
				return true;
			if ((list is null) != (other is null))
				return false;
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
		/// <param name="list">the list to predicate</param>
		/// <param name="predicator">the predicator delegate</param>
		/// <returns>Predicate result</returns>
		/// <remarks>extend method of <paramref name="list"/></remarks>
		public static bool All<T>(this IReadOnlyList<T> list, Predicate<T> predicator)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
			if (predicator is null)
				throw new ArgumentNullException(nameof(predicator));

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
		/// <param name="list">the list to predicate</param>
		/// <param name="predicator">the predicator delegate</param>
		/// <returns>Predicate result</returns>
		/// <remarks>extend method of <paramref name="list"/></remarks>
		public static bool Any<T>(this IReadOnlyList<T> list, Predicate<T> predicator)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
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
			if (list is null)
				throw new ArgumentNullException(nameof(list));
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
			if (list is null)
				throw new ArgumentNullException(nameof(list));
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
			if (list is null)
				throw new ArgumentNullException(nameof(list));
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
			if (list is null)
				throw new ArgumentNullException(nameof(list));
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
		/// <param name="list">the list to take</param>
		/// <returns>the first element or default</returns>
		public static T FirstOrDefault<T>(this IReadOnlyList<T> list)
		{
			if (list is null || list.Count == 0)
				return default;
			return list[0];
		}

		/// <summary>
		/// General list converter.
		/// </summary>
		/// <typeparam name="TIn">input list type</typeparam>
		/// <typeparam name="TOut">output list type</typeparam>
		/// <param name="list">the list to convert</param>
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
		/// <param name="list">the list to convert</param>
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
		/// <param name="list">the list to convert</param>
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
		/// <param name="list">the list to convert</param>
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
		/// <param name="list">the list to convert</param>
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
		/// <param name="list">the list to convert</param>
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
		/// <param name="list">the list to convert</param>
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
		/// <typeparam name="T">the data type</typeparam>
		/// <param name="list">list to pick</param>
		/// <param name="predicator">the predicator used to pick</param>
		/// <returns>the result <see cref="IReadOnlyList{T}"/></returns>
		public static IReadOnlyList<T> Where<T>(this IReadOnlyList<T> list, Predicate<T> predicator)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
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
		/// <typeparam name="T">the data type</typeparam>
		/// <param name="list">list to pick</param>
		/// <param name="predicator">the predicator used to pick</param>
		/// <returns>the result <see cref="IReadOnlyList{T}"/></returns>
		public static IReadOnlyList<T> Where<T>(this IReadOnlyList<T> list, IndexPredicator<T> predicator)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
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
		/// <typeparam name="T">the data type</typeparam>
		/// <param name="list">the list to find in</param>
		/// <param name="value">the value to find</param>
		/// <returns>the zero-based indices or empty if not founded</returns>
		public static int[] IndicesOf<T>(this IReadOnlyList<T> list, T value) where T : IEquatable<T>
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));

			List<int> indices = new List<int>(list.Count);
			for (int i = 0; i < list.Count; i++)
				if (value.Equals(list[i]))
					indices.Add(i);
			return indices.ToArray();
		}

		/// <summary>
		/// Get the index of the first occurrence of <paramref name="value"/> in <paramref name="list"/>
		/// </summary>
		/// <typeparam name="T">the data type</typeparam>
		/// <param name="list">the list to find in</param>
		/// <param name="value">the value to find</param>
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
		/// <typeparam name="T">the data type</typeparam>
		/// <param name="list">the list to find in</param>
		/// <param name="value">the value to find</param>
		/// <param name="comparer">the <see cref="IEqualityComparer{T}"/> to use, default null means <see cref="EqualityComparer{T}.Default"/></param>
		/// <returns>the zero-based indices or empty if not founded</returns>
		public static int[] IndicesOf<T>(this IReadOnlyList<T> list, T value, IEqualityComparer<T> comparer = null)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));

			comparer ??= EqualityComparer<T>.Default;
			List<int> indices = new List<int>(list.Count);
			for (int i = 0; i < list.Count; i++)
				if (comparer.Equals(list[i], value))
					indices.Add(i);
			return indices.ToArray();
		}

		/// <summary>
		/// Get the index of the first occurrence of <paramref name="value"/> in <paramref name="list"/>
		/// </summary>
		/// <typeparam name="T">the data type</typeparam>
		/// <param name="list">the list to find in</param>
		/// <param name="value">the value to find</param>
		/// <param name="comparer">the <see cref="IEqualityComparer{T}"/> to use, default null means <see cref="EqualityComparer{T}.Default"/></param>
		/// <returns>the zero-based index or -1 if not founded</returns>
		public static int IndexOf<T>(this IReadOnlyList<T> list, T value, IEqualityComparer<T> comparer = null)
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
		/// <typeparam name="T">the data type</typeparam>
		/// <typeparam name="TOut">the selected data type</typeparam>
		/// <param name="list">the list to find in</param>
		/// <param name="selector">the selector used to convert <paramref name="list"/> before comparison</param>
		/// <param name="value">the value to find</param>
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
		/// <typeparam name="T">the data type</typeparam>
		/// <typeparam name="TOut">the selected data type</typeparam>
		/// <param name="list">the list to find in</param>
		/// <param name="selector">the selector used to convert <paramref name="list"/> before comparison</param>
		/// <param name="value">the value to find</param>
		/// <param name="comparer">the <see cref="IEqualityComparer{T}"/> to use, default null means <see cref="EqualityComparer{T}.Default"/></param>
		/// <returns>the zero-based index or -1 if not founded</returns>
		public static int IndexOf<T, TOut>(this IReadOnlyList<T> list, Converter<T, TOut> selector, TOut value, IEqualityComparer<TOut> comparer = null)
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
		/// <typeparam name="T">the data type</typeparam>
		/// <param name="list">list to pick</param>
		/// <param name="predicator">the predicator used to pick</param>
		/// <returns>the result <see cref="IReadOnlyList{T}"/></returns>
		public static IReadOnlyList<T> IndexWhere<T>(this IReadOnlyList<T> list, Predicate<int> predicator)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
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
		/// <typeparam name="T">the data type</typeparam>
		/// <param name="list">list to count in</param>
		/// <param name="predicator">the predicator used to count</param>
		/// <returns>the count</returns>
		public static int Count<T>(this IReadOnlyList<T> list, Predicate<T> predicator)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
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
			if (list1 is null && list2 is null)
				throw new ArgumentNullException(nameof(list1));
			else if (list2 is null)
				return list1;
			else if (list1 is null)
				return list2;

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
		/// <typeparam name="T">the data type</typeparam>
		/// <param name="list">list to take in</param>
		/// <param name="from">the inclusive index to take from</param>
		/// <param name="count">number of elements to take</param>
		/// <returns>the result list</returns>
		public static IReadOnlyList<T> TakeRange<T>(this IReadOnlyList<T> list, int from, int count)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
			if (from < 0 || from >= list.Count)
				throw new ArgumentOutOfRangeException(nameof(from));
			if (count + from > list.Count)
				throw new ArgumentOutOfRangeException(nameof(count));
			
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
		/// <typeparam name="T">the data type</typeparam>
		/// <param name="list">list to take in</param>
		/// <param name="count">number of elements to take</param>
		/// <returns>the result list</returns>
		public static IReadOnlyList<T> Take<T>(this IReadOnlyList<T> list, int count)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
			if (count > list.Count)
				throw new ArgumentOutOfRangeException(nameof(count));
			
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
		/// <typeparam name="T">the data type</typeparam>
		/// <param name="list">list to take in</param>
		/// <param name="count">number of elements to take</param>
		/// <returns>the result list</returns>
		public static IReadOnlyList<T> TakeLast<T>(this IReadOnlyList<T> list, int count)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
			if (count > list.Count)
				throw new ArgumentOutOfRangeException(nameof(count));
			
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
		/// <typeparam name="T">the data type</typeparam>
		/// <param name="list">list to take in</param>
		/// <param name="count">number of elements to take</param>
		/// <returns>the result list</returns>
		public static IReadOnlyList<T> Skip<T>(this IReadOnlyList<T> list, int count)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
			if (count > list.Count)
				throw new ArgumentOutOfRangeException(nameof(count));
			
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
		/// <typeparam name="T">the data type</typeparam>
		/// <param name="list">list to take in</param>
		/// <param name="count">number of elements to take</param>
		/// <returns>the result list</returns>
		public static IReadOnlyList<T> SkipLast<T>(this IReadOnlyList<T> list, int count)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
			if (count > list.Count)
				throw new ArgumentOutOfRangeException(nameof(count));
			
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
				throw new ArgumentException(Resource.ArraySize);
			
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
				throw new ArgumentException(Resource.ArraySize);
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
				throw new ArgumentException(Resource.ArraySize);
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
		/// <typeparam name="T">the data type</typeparam>
		/// <param name="list">list to reverse</param>
		/// <returns>the result <see cref="IReadOnlyList{T}"/></returns>
		public static IReadOnlyList<T> Reverse<T>(this IReadOnlyList<T> list)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));

			if (list is T[] a)
			{
				var copy = a.Clone() as T[];
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
		/// <typeparam name="T">the data type</typeparam>
		/// <param name="list">list to convert</param>
		/// <returns><paramref name="list"/> itself if it is an array or a copied array</returns>
		public static T[] ToArray<T>(this IReadOnlyList<T> list)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
			
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
		/// <typeparam name="T">the data type</typeparam>
		/// <param name="list">list to convert</param>
		/// <returns>a copied array</returns>
		public static T[] ToCopiedArray<T>(this IReadOnlyList<T> list)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
			
			if (list is T[] a)
				return a.Clone() as T[];
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
		/// <typeparam name="T">the data type</typeparam>
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
		/// <typeparam name="T">the input type</typeparam>
		/// <param name="list">the input list</param>
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
		/// <typeparam name="T">the input type</typeparam>
		/// <param name="list">the input list</param>
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
		/// <typeparam name="T">the data type</typeparam>
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
		/// <typeparam name="T">the input type</typeparam>
		/// <typeparam name="TOut">the selector output type that can be compared</typeparam>
		/// <param name="list">the input list</param>
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
		/// <typeparam name="T">the input type</typeparam>
		/// <typeparam name="TOut">the selector output type that can be compared</typeparam>
		/// <param name="list">the input list</param>
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

			var ordered = System.Linq.Enumerable.OrderBy(System.Linq.Enumerable.Zip(keys, items), k => k.First);
			var temp = System.Linq.Enumerable.ToArray(ordered);
			var newKeys = Array.ConvertAll(temp, t => t.First);
			var newItems = Array.ConvertAll(temp, t => t.Second);
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
			var ordered = System.Linq.Enumerable.OrderBy(System.Linq.Enumerable.Zip(keys, items), k => k.First);
			var temp = System.Linq.Enumerable.ToArray(ordered);
			if (inPlace)
			{
				var newKeys = Array.ConvertAll(temp, t => t.First);
				Array.Copy(sourceArray: newKeys, destinationArray: keys, length: keys.Length);
			}
			var newItems = Array.ConvertAll(temp, t => t.Second);
			return newItems;
		}
		#endregion

		#region set operations
		/// <summary>
		/// Check whether <paramref name="set"/> and <paramref name="list"/> contains same elements
		/// </summary>
		/// <typeparam name="T">the data type</typeparam>
		/// <param name="set">the set to check</param>
		/// <param name="list">the list to check</param>
		/// <param name="comparer">the <see cref="IEqualityComparer{T}"/> to use, default null means <see cref="EqualityComparer{T}.Default"/></param>
		/// <returns></returns>
		public static bool SetEquals<T>(this IImmutableSet<T> set, IReadOnlyList<T> list, IEqualityComparer<T> comparer = null)
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
		/// <typeparam name="T">the data type</typeparam>
		/// <param name="list1">the set to check</param>
		/// <param name="list2">the list to check</param>
		/// <param name="comparer">the <see cref="IEqualityComparer{T}"/> to use, default null means <see cref="EqualityComparer{T}.Default"/></param>
		/// <returns></returns>
		public static bool SetEquals<T>(this IReadOnlyList<T> list1, IReadOnlyList<T> list2, IEqualityComparer<T> comparer = null)
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
		/// <typeparam name="T">the data type</typeparam>
		/// <param name="list">list to pick</param>
		/// <returns><paramref name="list"/>'s elements are unique or not</returns>
		public static bool ElementsUnique<T>(this IReadOnlyList<T> list) where T : IEquatable<T>
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));

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
		/// Check if <paramref name="list"/>'s elements are unique
		/// </summary>
		/// <typeparam name="T">the data type</typeparam>
		/// <param name="list">list to pick</param>
		/// <param name="comparer">the <see cref="IEqualityComparer{T}"/> to use, default null means <see cref="EqualityComparer{T}.Default"/></param>
		/// <returns><paramref name="list"/>'s elements are unique or not</returns>
		public static bool ElementsUnique<T>(this IReadOnlyList<T> list, IEqualityComparer<T> comparer = null)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));

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
		/// <typeparam name="T">the data type</typeparam>
		/// <param name="list">list to pick</param>
		/// <param name="comparer">the <see cref="IEqualityComparer{T}"/> to use, default null means <see cref="EqualityComparer{T}.Default"/></param>
		/// <returns>the result <see cref="IReadOnlyList{T}"/></returns>
		public static IReadOnlyList<T> Distinct<T>(this IReadOnlyList<T> list, IEqualityComparer<T> comparer = null)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));

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
		/// <typeparam name="T">the data type</typeparam>
		/// <param name="list">list to check in</param>
		/// <param name="element">element to check</param>
		/// <param name="comparer">the <see cref="IEqualityComparer{T}"/> to use, default null means <see cref="EqualityComparer{T}.Default"/></param>
		/// <returns><paramref name="list"/> contains <paramref name="element"/> or not</returns>
		public static bool Contains<T>(this IReadOnlyList<T> list, T element, IEqualityComparer<T> comparer = null)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
			
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
		/// Remove element(s) <c>e</c> in <paramref name="list"/> where <c>e</c> is in <paramref name="other"/> to form a new <see cref="IReadOnlyList{T}"/>
		/// </summary>
		/// <typeparam name="T">the data type</typeparam>
		/// <param name="list">list to remove from</param>
		/// <param name="other">the list used to compare</param>
		/// <param name="comparer">the <see cref="IEqualityComparer{T}"/> to use, default null means <see cref="EqualityComparer{T}.Default"/></param>
		/// <returns>the result <see cref="IReadOnlyList{T}"/></returns>
		public static IReadOnlyList<T> Except<T>(this IReadOnlyList<T> list, IReadOnlyList<T> other, IEqualityComparer<T> comparer = null)
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
		/// <typeparam name="T">the data type</typeparam>
		/// <param name="list">list to get from</param>
		/// <param name="other">the list used to compare</param>
		/// <param name="comparer">the <see cref="IEqualityComparer{T}"/> to use, default null means <see cref="EqualityComparer{T}.Default"/></param>
		/// <returns>the result <see cref="IReadOnlyList{T}"/></returns>
		public static IReadOnlyList<T> Intersect<T>(this IReadOnlyList<T> list, IReadOnlyList<T> other, IEqualityComparer<T> comparer = null)
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
		/// <typeparam name="T">the data type</typeparam>
		/// <param name="list">first list</param>
		/// <param name="other">second list</param>
		/// <param name="comparer">the <see cref="IEqualityComparer{T}"/> to use, default null means <see cref="EqualityComparer{T}.Default"/></param>
		/// <returns>the result <see cref="IReadOnlyList{T}"/></returns>
		public static IReadOnlyList<T> Union<T>(this IReadOnlyList<T> list, IReadOnlyList<T> other, IEqualityComparer<T> comparer = null)
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
		/// <typeparam name="T">the input type</typeparam>
		/// <typeparam name="TKey">the key type</typeparam>
		/// <param name="list">the input list to group</param>
		/// <param name="keySelector">the converter from <typeparamref name="T"/> to <typeparamref name="TKey"/></param>
		/// <returns>an <see cref="IReadOnlyList{T}"/> of <see cref="IReadOnlyGrouping{TKey, TElement}"/>s</returns>
		public static IReadOnlyList<IReadOnlyGrouping<TKey, T>> GroupBy<T, TKey>(this IReadOnlyList<T> list, Converter<T, TKey> keySelector)
		{
			static T identity(T input) => input;
			return list.GroupBy(keySelector, identity);
		}

		/// <summary>
		/// Group the given <paramref name="list"/> by <paramref name="keySelector"/> and the values of groups are given by <paramref name="valueSelector"/>.
		/// </summary>
		/// <typeparam name="T">the input type</typeparam>
		/// <typeparam name="TKey">the key type</typeparam>
		/// <typeparam name="TValue">the value type</typeparam>
		/// <param name="list">the input list to group</param>
		/// <param name="keySelector">the converter from <typeparamref name="T"/> to <typeparamref name="TKey"/></param>
		/// <param name="valueSelector">the converter from <typeparamref name="T"/> to <typeparamref name="TValue"/></param>
		/// <returns>an <see cref="IReadOnlyList{T}"/> of <see cref="IReadOnlyGrouping{TKey, TElement}"/>s</returns>
		public static IReadOnlyList<IReadOnlyGrouping<TKey, TValue>> GroupBy<T, TKey, TValue>(this IReadOnlyList<T> list, Converter<T, TKey> keySelector, Converter<T, TValue> valueSelector)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
			if (keySelector is null)
				throw new ArgumentNullException(nameof(keySelector));
			if (valueSelector is null)
				throw new ArgumentNullException(nameof(valueSelector));

			List<TKey> keys = new List<TKey>();
			List<List<TValue>> values = new List<List<TValue>>();
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
		/// <param name="array">the array before permutation</param>
		/// <param name="target">the array after the permutation</param>
		/// <returns>the permutation order as an <see cref="int"/> array, or null if <c>∃ a∈<paramref name="target"/>, a∉<paramref name="array"/></c></returns>
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
					return null; // cannot find permutation
			}
			return order;
		}

		/// <summary>
		/// Find the permutation order such that <c><paramref name="sorted"/>[result] = <paramref name="target"/></c>.
		/// </summary>
		/// <typeparam name="T">any type</typeparam>
		/// <param name="sorted">the array before permutation. must be sorted</param>
		/// <param name="target">the array after the permutation</param>
		/// <returns>the permutation order as an <see cref="int"/> array, or null if <c>∃ a∈<paramref name="target"/>, a∉<paramref name="sorted"/></c></returns>
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
					return null; // cannot find permutation
			}
			return order;
		}

		/// <summary>
		/// Re-order the <paramref name="array"/> by <paramref name="indices"/> out-of-place such that <c><paramref name="array"/>[indices] = result</c>.
		/// </summary>
		/// <typeparam name="T">any type</typeparam>
		/// <param name="array">the array to order</param>
		/// <param name="indices">the indices to order, if this is null or empty, <paramref name="array"/> will be returned</param>
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
		/// <param name="array">the array to order</param>
		/// <param name="indices">the indices to order, if this is null or empty, <paramref name="array"/> will be returned</param>
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
		/// <param name="array">the array to order</param>
		/// <param name="indices">the indices to order, may has less elements than <paramref name="array"/></param>
		/// <returns>the re-ordered array</returns>
		public static T[] InverseOrder<T>(this IReadOnlyList<T> array, IReadOnlyList<int> indices)
		{
			if (array is null)
				throw new ArgumentNullException(nameof(array));
			if (indices is null || indices.Count == 0)
				throw new ArgumentNullException(nameof(indices));
			int N = indices.Max() + 1;
			if (N > array.Count)
				throw new ArgumentOutOfRangeException(nameof(indices));

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
		/// <param name="perm">the input permutation</param>
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
		private static readonly Random random = new Random();

		/// <summary>
		/// Random shuffle the <paramref name="list"/> by random generator <paramref name="rand"/>
		/// </summary>
		/// <typeparam name="T">the data type</typeparam>
		/// <param name="list">the list to be shuffled</param>
		/// <param name="rand">random generator <see cref="Random"/>, default null means internal one</param>
		/// <returns>the new shuffled list</returns>
		public static IReadOnlyList<T> Shuffle<T>(this IReadOnlyList<T> list, Random rand = null)
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
		/// <param name="count">the length of array to generate</param>
		/// <param name="minValue">The inclusive lower bound of the random number returned.</param>
		/// <param name="maxValue">The exclusive upper bound of the random number returned. maxValue must be greater than or equal to <c><paramref name="minValue"/> + <paramref name="count"/></c>.</param>
		/// <returns>the generate a random <see cref="int"/> array</returns>
		public static int[] RandomUniqueArray(int count, int minValue, int maxValue)
		{
			if (count <= 0)
				throw new ArgumentOutOfRangeException(nameof(count));
			if (maxValue < minValue + count)
				throw new ArgumentOutOfRangeException(nameof(maxValue));

			HashSet<int> set = new HashSet<int>(count);
			while (set.Count != count)
				set.Add(random.Next(minValue, maxValue));
			return System.Linq.Enumerable.ToArray(set);
		}

		/// <summary>
		/// Generate a random <see cref="int"/> array of length <paramref name="count"/> whose elements' sum is <paramref name="sum"/>.
		/// </summary>
		/// <param name="count">the length of array to generate</param>
		/// <param name="sum">the desired sum of elements generated</param>
		/// <returns>the generate a random <see cref="int"/> array whose elements are all positive</returns>
		public static int[] RandomPositiveArrayOfSum(int count, int sum)
		{
			if (count <= 0)
				throw new ArgumentOutOfRangeException(nameof(count));
			if (sum <= 0)
				throw new ArgumentOutOfRangeException(nameof(sum));

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
		/// <param name="count">the length of array to generate</param>
		/// <param name="sum">the desired sum of elements generated</param>
		/// <returns>the generate a random <see cref="int"/> array whose elements are all non-negative</returns>
		public static int[] RandomNonNegativeArrayOfSum(int count, int sum)
		{
			if (count <= 0)
				throw new ArgumentOutOfRangeException(nameof(count));
			if (sum < count)
				throw new ArgumentOutOfRangeException(nameof(sum));

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
		/// <typeparam name="T">the data type</typeparam>
		/// <param name="list">list to convert</param>
		/// <param name="comparer">the <see cref="IEqualityComparer{T}"/> to use, default null means <see cref="EqualityComparer{T}.Default"/></param>
		/// <returns>the result <see cref="IImmutableSet{T}"/></returns>
		public static IImmutableSet<T> ToImmutableSet<T>(this IReadOnlyList<T> list, IEqualityComparer<T> comparer = null)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));
			if (list is IImmutableSet<T> s)
				return s;
			var distinct = list.Distinct(comparer);
			return new ImmutableSet<T>(distinct);
		}

		/// <summary>
		/// Convert the input array to a <see cref="long"/> array
		/// </summary>
		/// <typeparam name="T">input type that implements <see cref="IConvertible"/></typeparam>
		/// <param name="array">input <typeparamref name="T"/> array</param>
		/// <returns>a new <see cref="long"/> array</returns>
		public static long[] ToLongs<T>(this T[] array) where T : IConvertible => Array.ConvertAll(array, a => a.ToInt64(Resource.Culture));

		/// <summary>
		/// Convert the input array to a <see cref="int"/> array
		/// </summary>
		/// <typeparam name="T">input type that implements <see cref="IConvertible"/></typeparam>
		/// <param name="array">input <typeparamref name="T"/> array</param>
		/// <returns>a new <see cref="int"/> array</returns>
		public static int[] ToInts<T>(this T[] array) where T : IConvertible => Array.ConvertAll(array, a => a.ToInt32(Resource.Culture));

		/// <summary>
		/// Convert the input array to a <see cref="int"/> array
		/// </summary>
		/// <typeparam name="T">input type that implements <see cref="IConvertible"/></typeparam>
		/// <param name="array">input <typeparamref name="T"/> array</param>
		/// <returns>a new <see cref="int"/> array</returns>
		public static int[] ToInts<T>(this IReadOnlyList<T> array) where T : IConvertible => Array.ConvertAll(array.ToArray(), a => a.ToInt32(Resource.Culture));

		/// <summary>
		/// Convert a 1D <see cref="float"/> array to <see cref="FloatComplex"/> array by taking two items to form one.
		/// </summary>
		/// <param name="input">input <see cref="float"/> array</param>
		/// <returns>a new <see cref="FloatComplex"/> array made out of <paramref name="input"/></returns>
		/// <remarks>extend method of <paramref name="input"/></remarks>
		public static FloatComplex[] ToComplexArray(this float[] input)
		{
			if (input is null)
				throw new ArgumentNullException(nameof(input));
			var complexArray = new FloatComplex[input.LongLength / 2];
			for (long i = 0; i < input.LongLength; i += 2)
			{
				complexArray[i / 2] = new FloatComplex(input[i], input[i + 1]);
			}
			return complexArray;
		}

		/// <summary>
		/// Convert a 1D <see cref="double"/> array to <see cref="DoubleComplex"/> array by taking two items to form one.
		/// </summary>
		/// <param name="input">input <see cref="double"/> array</param>
		/// <returns>a new <see cref="FloatComplex"/> array made out of <paramref name="input"/></returns>
		/// <remarks>extend method of <paramref name="input"/></remarks>
		public static DoubleComplex[] ToComplexArray(this double[] input)
		{
			if (input is null)
				throw new ArgumentNullException(nameof(input));
			var complexArray = new DoubleComplex[input.LongLength / 2];
			for (long i = 0; i < input.LongLength; i += 2)
			{
				complexArray[i / 2] = new DoubleComplex(input[i], input[i + 1]);
			}
			return complexArray;
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
			{
				hc = unchecked(hc * CRC_CONST + (array[i] is null ? 0 : array[i].GetHashCode())); // CRC
			}
			return hc;
		}

		/// <summary>
		/// Get the hash code of an array using CRC method
		/// </summary>
		/// <typeparam name="T">any type</typeparam>
		/// <param name="array">array to get hash code</param>
		/// <param name="hashCodeConverter">the converter used to get the hash code of each element</param>
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
				hc = unchecked(hc + (set[i] is null ? 0 : set[i].GetHashCode()));
			}
			return hc;
		}

		/// <summary>
		/// Get the hash code of a set (order-independent) using ADD method
		/// </summary>
		/// <typeparam name="T">any type</typeparam>
		/// <param name="set">array to get hash code</param>
		/// <param name="hashCodeConverter">the converter used to get the hash code of each element</param>
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
		/// <typeparam name="T">the data type</typeparam>
		/// <param name="count">the count to repeat</param>
		/// <param name="val">the value to repeat</param>
		/// <returns>the result <see cref="IReadOnlyList{T}"/></returns>
		public static IReadOnlyList<T> Repeat<T>(T val, int count)
		{
			if (count < 0)
				throw new ArgumentOutOfRangeException(nameof(count), count, Resource.ParaCannotNegative);
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
		/// <typeparam name="T">the data type that must be able to self increment (operator <c>++</c> or <c>+=</c>)</typeparam>
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
			catch (Exception)
			{
				throw new InvalidOperationException();
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
				throw new ArgumentOutOfRangeException(nameof(step), step, Resource.ParaCannotZero);
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
				throw new ArgumentOutOfRangeException(nameof(step), step, Resource.ParaCannotZero);
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
				throw new ArgumentOutOfRangeException(nameof(step), step, Resource.ParaCannotZero);

			var res = new char[count];
			for (int i = 0; i < count; i++)
			{
				res[i] = checked((char)(i * step + start));
			}
			return res;
		}
		#endregion
	}

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
