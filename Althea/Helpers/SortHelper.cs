using System.Runtime.CompilerServices;


namespace Althea.Helpers
{
	#region interface
	/// <summary>
	/// The abstract class that can be used to swap two <typeparamref name="T"/> values in-place
	/// </summary>
	/// <typeparam name="T">The type of the values to be swapped</typeparam>
	public abstract class Swapper<T>
	{
		/// <summary>
		/// Encapsulates a method that swaps two values <paramref name="a"/> and <paramref name="b"/> in-place
		/// </summary>
		/// <param name="a">The reference to first value to be swapped with <paramref name="b"/></param>
		/// <param name="b">The reference to second value to be swapped with <paramref name="a"/></param>
		public delegate void DelegateSwapper(ref T? a, ref T? b);

		internal sealed class DelegateSwapping : Swapper<T>
		{
			internal readonly DelegateSwapper swapper;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			internal DelegateSwapping(DelegateSwapper swapper) => this.swapper = swapper;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public override void Swap(ref T? a, ref T? b) => this.swapper.Invoke(ref a, ref b);
		}

		/// <summary>
		/// Create a <see cref="Swapper{T}"/> using a specified in-place <paramref name="swapper"/> <see cref="DelegateSwapper"/>
		/// </summary>
		/// <param name="swapper">The <see cref="DelegateSwapper"/> used to swap values in-place</param>
		/// <returns>The created <see cref="Swapper{T}"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Swapper<T> Create(DelegateSwapper swapper)
		{
			return new DelegateSwapping(swapper);
		}

		/// <summary>
		/// When implemented by a derived class, swap two values <paramref name="a"/> and <paramref name="b"/> in-place
		/// </summary>
		/// <param name="a">The reference to first value to be swapped with <paramref name="b"/></param>
		/// <param name="b">The reference to second value to be swapped with <paramref name="a"/></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public abstract void Swap(ref T? a, ref T? b);

		internal sealed class AssignmentSwapping : Swapper<T>
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public override void Swap(ref T? a, ref T? b)
			{
				T? c = a;
				a = b;
				b = c;
			}
		}

		/// <summary>
		/// Get a default swapper for the type specified by the generic argument <typeparamref name="T"/> that simply swaps the values by assignments.
		/// </summary>
		public static Swapper<T> Default { get; } = new AssignmentSwapping();
	}

	/// <summary>
	/// The static class that provides the sort utilities for <see cref="Span{T}"/>
	/// </summary>
	public static class SortHelper
	{
		/// <summary>
		/// Sort the <paramref name="keys"/> with <paramref name="values"/> using the given <typeparamref name="TKey"/> <paramref name="comparer"/> and <typeparamref name="TValue"/> <paramref name="swapper"/>
		/// </summary>
		/// <typeparam name="TKey">The type of the keys</typeparam>
		/// <typeparam name="TValue">The type of the values</typeparam>
		/// <param name="keys">The keys as a <see cref="Span{T}"/> of <typeparamref name="TKey"/></param>
		/// <param name="values">The values as a <see cref="Span{T}"/> of <typeparamref name="TValue"/></param>
		/// <param name="comparer">The <see cref="Comparer{T}"/> of <typeparamref name="TKey"/> used to compare the elements in <paramref name="keys"/>. Default null means <see cref="Comparer{T}.Default"/>.</param>
		/// <param name="swapper">The <see cref="Swapper{T}"/> of <typeparamref name="TValue"/> used to swap the elements in <paramref name="values"/> (<paramref name="keys"/> always uses a default swapper). Default null means <see cref="Swapper{T}.Default"/>.</param>
		public static void Sort<TKey, TValue>(this Span<TKey> keys, Span<TValue?> values, Comparer<TKey>? comparer = null, Swapper<TValue>? swapper = null)
		{
			SortHelper<TKey, TValue>.Sort(keys, values, comparer ?? Comparer<TKey>.Default, swapper ?? Swapper<TValue>.Default);
		}

		/// <summary>
		/// Sort the <paramref name="keys"/> with (<paramref name="values1"/>, <paramref name="values2"/>) using the given <typeparamref name="TKey"/> <paramref name="comparer"/> and (<typeparamref name="TValue1"/> <paramref name="swapper1"/>, <typeparamref name="TValue2"/> <paramref name="swapper2"/>)
		/// </summary>
		/// <typeparam name="TKey">The type of the keys</typeparam>
		/// <typeparam name="TValue1">The type of the first value array</typeparam>
		/// <typeparam name="TValue2">The type of the second value array</typeparam>
		/// <param name="keys">The keys as a <see cref="Span{T}"/> of <typeparamref name="TKey"/></param>
		/// <param name="values1">The first value array as a <see cref="Span{T}"/> of <typeparamref name="TValue1"/></param>
		/// <param name="values2">The second value array as a <see cref="Span{T}"/> of <typeparamref name="TValue2"/></param>
		/// <param name="comparer">The <see cref="Comparer{T}"/> of <typeparamref name="TKey"/> used to compare the elements in <paramref name="keys"/>. Default null means <see cref="Comparer{T}.Default"/>.</param>
		/// <param name="swapper1">The <see cref="Swapper{T}"/> of <typeparamref name="TValue1"/> used to swap the elements in <paramref name="values1"/>. Default null means <see cref="Swapper{T}.Default"/>.</param>
		/// <param name="swapper2">The <see cref="Swapper{T}"/> of <typeparamref name="TValue2"/> used to swap the elements in <paramref name="values2"/>. Default null means <see cref="Swapper{T}.Default"/>.</param>
		public static void Sort<TKey, TValue1, TValue2>(this Span<TKey> keys, Span<TValue1?> values1, Span<TValue2?> values2, Comparer<TKey>? comparer = null, Swapper<TValue1>? swapper1 = null, Swapper<TValue2>? swapper2 = null)
		{
			SortHelper<TKey, TValue1, TValue2>.Sort(keys, values1, values2, comparer ?? Comparer<TKey>.Default, swapper1 ?? Swapper<TValue1>.Default, swapper2 ?? Swapper<TValue2>.Default);
		}

		/// <summary>
		/// Sort the <paramref name="keys"/> with (<paramref name="values1"/>, <paramref name="values2"/>, <paramref name="values3"/>) using the given <typeparamref name="TKey"/> <paramref name="comparer"/> and (<typeparamref name="TValue1"/> <paramref name="swapper1"/>, <typeparamref name="TValue2"/> <paramref name="swapper2"/>, <typeparamref name="TValue3"/> <paramref name="swapper3"/>)
		/// </summary>
		/// <typeparam name="TKey">The type of the keys</typeparam>
		/// <typeparam name="TValue1">The type of the first value array</typeparam>
		/// <typeparam name="TValue2">The type of the second value array</typeparam>
		/// <typeparam name="TValue3">The type of the third value array</typeparam>
		/// <param name="keys">The keys as a <see cref="Span{T}"/> of <typeparamref name="TKey"/></param>
		/// <param name="values1">The first value array as a <see cref="Span{T}"/> of <typeparamref name="TValue1"/></param>
		/// <param name="values2">The second value array as a <see cref="Span{T}"/> of <typeparamref name="TValue2"/></param>
		/// <param name="values3">The third value array as a <see cref="Span{T}"/> of <typeparamref name="TValue3"/></param>
		/// <param name="comparer">The <see cref="Comparer{T}"/> of <typeparamref name="TKey"/> used to compare the elements in <paramref name="keys"/>. Default null means <see cref="Comparer{T}.Default"/>.</param>
		/// <param name="swapper1">The <see cref="Swapper{T}"/> of <typeparamref name="TValue1"/> used to swap the elements in <paramref name="values1"/>. Default null means <see cref="Swapper{T}.Default"/>.</param>
		/// <param name="swapper2">The <see cref="Swapper{T}"/> of <typeparamref name="TValue2"/> used to swap the elements in <paramref name="values2"/>. Default null means <see cref="Swapper{T}.Default"/>.</param>
		/// <param name="swapper3">The <see cref="Swapper{T}"/> of <typeparamref name="TValue3"/> used to swap the elements in <paramref name="values3"/>. Default null means <see cref="Swapper{T}.Default"/>.</param>
		public static void Sort<TKey, TValue1, TValue2, TValue3>(this Span<TKey> keys, Span<TValue1?> values1, Span<TValue2?> values2, Span<TValue3?> values3, Comparer<TKey>? comparer = null, Swapper<TValue1>? swapper1 = null, Swapper<TValue2>? swapper2 = null, Swapper<TValue3>? swapper3 = null)
		{
			SortHelper<TKey, TValue1, TValue2, TValue3>.Sort(keys, values1, values2, values3, comparer ?? Comparer<TKey>.Default, swapper1 ?? Swapper<TValue1>.Default, swapper2 ?? Swapper<TValue2>.Default, swapper3 ?? Swapper<TValue3>.Default);
		}
	}
	#endregion


	#region implementations
	internal static class SortHelper<TKey, TVal1>
	{
		internal static void Sort(Span<TKey> keys, Span<TVal1?> values, Comparer<TKey> comparer, Swapper<TVal1> swapper)
		{
			if (keys.Length > 1)
			{
				IntroSort(keys, values, depthLimit: 2 * (keys.Length.Log2() + 1), comparer, swapper);
			}
		}

		private static void SwapIfGreaterWithValues(Span<TKey> keys, Span<TVal1?> values, Comparer<TKey> comparer, Swapper<TVal1> swapper, int i, int j)
		{
			if (comparer.Compare(keys[i], keys[j]) > 0)
			{
				TKey val = keys[i];
				keys[i] = keys[j];
				keys[j] = val;
				swapper.Swap(ref values[i], ref values[j]);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void Swap(Span<TKey> keys, Span<TVal1?> values, Swapper<TVal1> swapper, int i, int j)
		{
			TKey val = keys[i];
			keys[i] = keys[j];
			keys[j] = val;
			swapper.Swap(ref values[i], ref values[j]);
		}

		private static void IntroSort(Span<TKey> keys, Span<TVal1?> values, int depthLimit, Comparer<TKey> comparer, Swapper<TVal1> swapper)
		{
			int left = keys.Length;
			while (left > 1)
			{
				if (left <= 16)
				{
					switch (left)
					{
						case 2:
							SwapIfGreaterWithValues(keys, values, comparer, swapper, 0, 1);
							break;
						case 3:
							SwapIfGreaterWithValues(keys, values, comparer, swapper, 0, 1);
							SwapIfGreaterWithValues(keys, values, comparer, swapper, 0, 2);
							SwapIfGreaterWithValues(keys, values, comparer, swapper, 1, 2);
							break;
						default:
							InsertionSort(keys[..left], values[..left], comparer, swapper);
							break;
					}
					break;
				}
				if (depthLimit == 0)
				{
					HeapSort(keys[..left], values[..left], comparer, swapper);
					break;
				}
				depthLimit--;
				int partition = PickPivotAndPartition(keys[..left], values[..left], comparer, swapper);
				IntroSort(keys[(partition + 1)..left], values[(partition + 1)..left], depthLimit, comparer, swapper);
				left = partition;
			}
		}

		private static int PickPivotAndPartition(Span<TKey> keys, Span<TVal1?> values, Comparer<TKey> comparer, Swapper<TVal1> swapper)
		{
			int maxN = keys.Length - 1;
			int halfN = maxN >> 1;
			SwapIfGreaterWithValues(keys, values, comparer, swapper, 0, halfN);
			SwapIfGreaterWithValues(keys, values, comparer, swapper, 0, maxN);
			SwapIfGreaterWithValues(keys, values, comparer, swapper, halfN, maxN);
			TKey val = keys[halfN];
			Swap(keys, values, swapper, halfN, maxN - 1);
			int count = 0;
			int left = maxN - 1;
			while (count < left)
			{
				while (comparer.Compare(keys[++count], val) < 0)
				{ }
				while (comparer.Compare(val, keys[--left]) < 0)
				{ }
				if (count >= left)
				{
					break;
				}
				Swap(keys, values, swapper, count, left);
			}
			if (count != maxN - 1)
			{
				Swap(keys, values, swapper, count, maxN - 1);
			}
			return count;
		}

		private static void HeapSort(Span<TKey> keys, Span<TVal1?> values, Comparer<TKey> comparer, Swapper<TVal1> swapper)
		{
			int length = keys.Length;
			for (int i = length / 2 - 1; i >= 0; i--)
			{
				MaximizeHeap(keys, values, i, length, comparer, swapper);
			}
			for (int i = length - 1; i > 0; i--)
			{
				Swap(keys, values, swapper, 0, i);
				MaximizeHeap(keys, values, 0, i, comparer, swapper);
			}
		}

		private static void MaximizeHeap(Span<TKey> keys, Span<TVal1?> values, int start, int end, Comparer<TKey> comparer, Swapper<TVal1> swapper)
		{
			int parent = start, child = parent * 2 + 1;
			while (child < end)
			{
				if (child + 1 < end && comparer.Compare(keys[child], keys[child + 1]) < 0)
					child++;
				if (comparer.Compare(keys[parent], keys[child]) > 0)
					return;
				Swap(keys, values, swapper, parent, child);
				parent = child;
				child = parent * 2 + 1;
			}
		}

		private static void InsertionSort(Span<TKey> keys, Span<TVal1?> values, Comparer<TKey> comparer, Swapper<TVal1> swapper)
		{
			for (int i = 1; i < keys.Length - 1; i++)
			{
				TKey key0 = keys[i + 1];
				int j = i - 1;
				while (j >= 0 && comparer.Compare(key0, keys[j]) < 0)
				{
					keys[j + 1] = keys[j];
					swapper.Swap(ref values[j + 1], ref values[j]);
					j--;
				}
				keys[j + 1] = key0;
			}
		}
	}

	internal static class SortHelper<TKey, TVal1, TVal2>
	{
		internal static void Sort(Span<TKey> keys, Span<TVal1?> values1, Span<TVal2?> values2, Comparer<TKey> comparer, Swapper<TVal1> swapper1, Swapper<TVal2> swapper2)
		{
			if (keys.Length > 1)
			{
				IntroSort(keys, values1, values2, depthLimit: 2 * (keys.Length.Log2() + 1), comparer, swapper1, swapper2);
			}
		}

		private static void SwapIfGreaterWithValues(Span<TKey> keys, Span<TVal1?> values1, Span<TVal2?> values2, Comparer<TKey> comparer, Swapper<TVal1> swapper1, Swapper<TVal2> swapper2, int i, int j)
		{
			if (comparer.Compare(keys[i], keys[j]) > 0)
			{
				TKey val = keys[i];
				keys[i] = keys[j];
				keys[j] = val;
				swapper1.Swap(ref values1[i], ref values1[j]);
				swapper2.Swap(ref values2[i], ref values2[j]);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void Swap(Span<TKey> keys, Span<TVal1?> values1, Span<TVal2?> values2, Swapper<TVal1> swapper1, Swapper<TVal2> swapper2, int i, int j)
		{
			TKey val = keys[i];
			keys[i] = keys[j];
			keys[j] = val;
			swapper1.Swap(ref values1[i], ref values1[j]);
			swapper2.Swap(ref values2[i], ref values2[j]);
		}

		private static void IntroSort(Span<TKey> keys, Span<TVal1?> values1, Span<TVal2?> values2, int depthLimit, Comparer<TKey> comparer, Swapper<TVal1> swapper1, Swapper<TVal2> swapper2)
		{
			int left = keys.Length;
			while (left > 1)
			{
				if (left <= 16)
				{
					switch (left)
					{
						case 2:
							SwapIfGreaterWithValues(keys, values1, values2, comparer, swapper1, swapper2, 0, 1);
							break;
						case 3:
							SwapIfGreaterWithValues(keys, values1, values2, comparer, swapper1, swapper2, 0, 1);
							SwapIfGreaterWithValues(keys, values1, values2, comparer, swapper1, swapper2, 0, 2);
							SwapIfGreaterWithValues(keys, values1, values2, comparer, swapper1, swapper2, 1, 2);
							break;
						default:
							InsertionSort(keys[..left], values1[..left], values2[..left], comparer, swapper1, swapper2);
							break;
					}
					break;
				}
				if (depthLimit == 0)
				{
					HeapSort(keys[..left], values1[..left], values2[..left], comparer, swapper1, swapper2);
					break;
				}
				depthLimit--;
				int partition = PickPivotAndPartition(keys[..left], values1[..left], values2[..left], comparer, swapper1, swapper2);
				IntroSort(keys[(partition + 1)..left], values1[(partition + 1)..left], values2[(partition + 1)..left], depthLimit, comparer, swapper1, swapper2);
				left = partition;
			}
		}

		private static int PickPivotAndPartition(Span<TKey> keys, Span<TVal1?> values1, Span<TVal2?> values2, Comparer<TKey> comparer, Swapper<TVal1> swapper1, Swapper<TVal2> swapper2)
		{
			int maxN = keys.Length - 1;
			int halfN = maxN >> 1;
			SwapIfGreaterWithValues(keys, values1, values2, comparer, swapper1, swapper2, 0, halfN);
			SwapIfGreaterWithValues(keys, values1, values2, comparer, swapper1, swapper2, 0, maxN);
			SwapIfGreaterWithValues(keys, values1, values2, comparer, swapper1, swapper2, halfN, maxN);
			TKey val = keys[halfN];
			Swap(keys, values1, values2, swapper1, swapper2, halfN, maxN - 1);
			int count = 0;
			int left = maxN - 1;
			while (count < left)
			{
				while (comparer.Compare(keys[++count], val) < 0)
				{ }
				while (comparer.Compare(val, keys[--left]) < 0)
				{ }
				if (count >= left)
				{
					break;
				}
				Swap(keys, values1, values2, swapper1, swapper2, count, left);
			}
			if (count != maxN - 1)
			{
				Swap(keys, values1, values2, swapper1, swapper2, count, maxN - 1);
			}
			return count;
		}

		private static void HeapSort(Span<TKey> keys, Span<TVal1?> values1, Span<TVal2?> values2, Comparer<TKey> comparer, Swapper<TVal1> swapper1, Swapper<TVal2> swapper2)
		{
			int length = keys.Length;
			for (int i = length / 2 - 1; i >= 0; i--)
			{
				MaximizeHeap(keys, values1, values2, i, length, comparer, swapper1, swapper2);
			}
			for (int i = length - 1; i > 0; i--)
			{
				Swap(keys, values1, values2, swapper1, swapper2, 0, i);
				MaximizeHeap(keys, values1, values2, 0, i, comparer, swapper1, swapper2);
			}
		}

		private static void MaximizeHeap(Span<TKey> keys, Span<TVal1?> values1, Span<TVal2?> values2, int start, int end, Comparer<TKey> comparer, Swapper<TVal1> swapper1, Swapper<TVal2> swapper2)
		{
			int parent = start, child = parent * 2 + 1;
			while (child < end)
			{
				if (child + 1 < end && comparer.Compare(keys[child], keys[child + 1]) < 0)
					child++;
				if (comparer.Compare(keys[parent], keys[child]) > 0)
					return;
				Swap(keys, values1, values2, swapper1, swapper2, parent, child);
				parent = child;
				child = parent * 2 + 1;
			}
		}

		private static void InsertionSort(Span<TKey> keys, Span<TVal1?> values1, Span<TVal2?> values2, Comparer<TKey> comparer, Swapper<TVal1> swapper1, Swapper<TVal2> swapper2)
		{
			for (int i = 1; i < keys.Length - 1; i++)
			{
				TKey key0 = keys[i + 1];
				int j = i - 1;
				while (j >= 0 && comparer.Compare(key0, keys[j]) < 0)
				{
					keys[j + 1] = keys[j];
					swapper1.Swap(ref values1[j + 1], ref values1[j]);
					swapper2.Swap(ref values2[j + 1], ref values2[j]);
					j--;
				}
				keys[j + 1] = key0;
			}
		}
	}

	internal static class SortHelper<TKey, TVal1, TVal2, TVal3>
	{
		internal static void Sort(Span<TKey> keys, Span<TVal1?> values1, Span<TVal2?> values2, Span<TVal3?> values3, Comparer<TKey> comparer, Swapper<TVal1> swapper1, Swapper<TVal2> swapper2, Swapper<TVal3> swapper3)
		{
			if (keys.Length > 1)
			{
				IntroSort(keys, values1, values2, values3, depthLimit: 2 * (keys.Length.Log2() + 1), comparer, swapper1, swapper2, swapper3);
			}
		}

		private static void SwapIfGreaterWithValues(Span<TKey> keys, Span<TVal1?> values1, Span<TVal2?> values2, Span<TVal3?> values3, Comparer<TKey> comparer, Swapper<TVal1> swapper1, Swapper<TVal2> swapper2, Swapper<TVal3> swapper3, int i, int j)
		{
			if (comparer.Compare(keys[i], keys[j]) > 0)
			{
				TKey val = keys[i];
				keys[i] = keys[j];
				keys[j] = val;
				swapper1.Swap(ref values1[i], ref values1[j]);
				swapper2.Swap(ref values2[i], ref values2[j]);
				swapper3.Swap(ref values3[i], ref values3[j]);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void Swap(Span<TKey> keys, Span<TVal1?> values1, Span<TVal2?> values2, Span<TVal3?> values3, Swapper<TVal1> swapper1, Swapper<TVal2> swapper2, Swapper<TVal3> swapper3, int i, int j)
		{
			TKey val = keys[i];
			keys[i] = keys[j];
			keys[j] = val;
			swapper1.Swap(ref values1[i], ref values1[j]);
			swapper2.Swap(ref values2[i], ref values2[j]);
			swapper3.Swap(ref values3[i], ref values3[j]);
		}

		private static void IntroSort(Span<TKey> keys, Span<TVal1?> values1, Span<TVal2?> values2, Span<TVal3?> values3, int depthLimit, Comparer<TKey> comparer, Swapper<TVal1> swapper1, Swapper<TVal2> swapper2, Swapper<TVal3> swapper3)
		{
			int left = keys.Length;
			while (left > 1)
			{
				if (left <= 16)
				{
					switch (left)
					{
						case 2:
							SwapIfGreaterWithValues(keys, values1, values2, values3, comparer, swapper1, swapper2, swapper3, 0, 1);
							break;
						case 3:
							SwapIfGreaterWithValues(keys, values1, values2, values3, comparer, swapper1, swapper2, swapper3, 0, 1);
							SwapIfGreaterWithValues(keys, values1, values2, values3, comparer, swapper1, swapper2, swapper3, 0, 2);
							SwapIfGreaterWithValues(keys, values1, values2, values3, comparer, swapper1, swapper2, swapper3, 1, 2);
							break;
						default:
							InsertionSort(keys[..left], values1[..left], values2[..left], values3[..left], comparer, swapper1, swapper2, swapper3);
							break;
					}
					break;
				}
				if (depthLimit == 0)
				{
					HeapSort(keys[..left], values1[..left], values2[..left], values3[..left], comparer, swapper1, swapper2, swapper3);
					break;
				}
				depthLimit--;
				int partition = PickPivotAndPartition(keys[..left], values1[..left], values2[..left], values3[..left], comparer, swapper1, swapper2, swapper3);
				IntroSort(keys[(partition + 1)..left], values1[(partition + 1)..left], values2[(partition + 1)..left], values3[(partition + 1)..left], depthLimit, comparer, swapper1, swapper2, swapper3);
				left = partition;
			}
		}

		private static int PickPivotAndPartition(Span<TKey> keys, Span<TVal1?> values1, Span<TVal2?> values2, Span<TVal3?> values3, Comparer<TKey> comparer, Swapper<TVal1> swapper1, Swapper<TVal2> swapper2, Swapper<TVal3> swapper3)
		{
			int maxN = keys.Length - 1;
			int halfN = maxN >> 1;
			SwapIfGreaterWithValues(keys, values1, values2, values3, comparer, swapper1, swapper2, swapper3, 0, halfN);
			SwapIfGreaterWithValues(keys, values1, values2, values3, comparer, swapper1, swapper2, swapper3, 0, maxN);
			SwapIfGreaterWithValues(keys, values1, values2, values3, comparer, swapper1, swapper2, swapper3, halfN, maxN);
			TKey val = keys[halfN];
			Swap(keys, values1, values2, values3, swapper1, swapper2, swapper3, halfN, maxN - 1);
			int count = 0;
			int left = maxN - 1;
			while (count < left)
			{
				while (comparer.Compare(keys[++count], val) < 0)
				{ }
				while (comparer.Compare(val, keys[--left]) < 0)
				{ }
				if (count >= left)
				{
					break;
				}
				Swap(keys, values1, values2, values3, swapper1, swapper2, swapper3, count, left);
			}
			if (count != maxN - 1)
			{
				Swap(keys, values1, values2, values3, swapper1, swapper2, swapper3, count, maxN - 1);
			}
			return count;
		}

		private static void HeapSort(Span<TKey> keys, Span<TVal1?> values1, Span<TVal2?> values2, Span<TVal3?> values3, Comparer<TKey> comparer, Swapper<TVal1> swapper1, Swapper<TVal2> swapper2, Swapper<TVal3> swapper3)
		{
			int length = keys.Length;
			for (int i = length / 2 - 1; i >= 0; i--)
			{
				MaximizeHeap(keys, values1, values2, values3, i, length, comparer, swapper1, swapper2, swapper3);
			}
			for (int i = length - 1; i > 0; i--)
			{
				Swap(keys, values1, values2, values3, swapper1, swapper2, swapper3, 0, i);
				MaximizeHeap(keys, values1, values2, values3, 0, i, comparer, swapper1, swapper2, swapper3);
			}
		}

		private static void MaximizeHeap(Span<TKey> keys, Span<TVal1?> values1, Span<TVal2?> values2, Span<TVal3?> values3, int start, int end, Comparer<TKey> comparer, Swapper<TVal1> swapper1, Swapper<TVal2> swapper2, Swapper<TVal3> swapper3)
		{
			int parent = start, child = parent * 2 + 1;
			while (child < end)
			{
				if (child + 1 < end && comparer.Compare(keys[child], keys[child + 1]) < 0)
					child++;
				if (comparer.Compare(keys[parent], keys[child]) > 0)
					return;
				Swap(keys, values1, values2, values3, swapper1, swapper2, swapper3, parent, child);
				parent = child;
				child = parent * 2 + 1;
			}
		}

		private static void InsertionSort(Span<TKey> keys, Span<TVal1?> values1, Span<TVal2?> values2, Span<TVal3?> values3, Comparer<TKey> comparer, Swapper<TVal1> swapper1, Swapper<TVal2> swapper2, Swapper<TVal3> swapper3)
		{
			for (int i = 1; i < keys.Length - 1; i++)
			{
				TKey key0 = keys[i + 1];
				int j = i - 1;
				while (j >= 0 && comparer.Compare(key0, keys[j]) < 0)
				{
					keys[j + 1] = keys[j];
					swapper1.Swap(ref values1[j + 1], ref values1[j]);
					swapper2.Swap(ref values2[j + 1], ref values2[j]);
					swapper3.Swap(ref values3[j + 1], ref values3[j]);
					j--;
				}
				keys[j + 1] = key0;
			}
		}
	}
	#endregion
}
