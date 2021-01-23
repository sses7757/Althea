using System;
using System.Collections;
using System.Collections.Generic;


namespace Althea.Linq
{
	#region immutable set

	#region interfaces
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
		IImmutableSet<T> ExceptWith(IImmutableSet<T> other, IEqualityComparer<T>? comparer = null);

		/// <summary>
		/// Pick all elements in the specified set from the current set.
		/// </summary>
		/// <param name="other">The collection of items to intersect from the set.</param>
		/// <param name="comparer">the <see cref="IEqualityComparer{T}"/> to use, default null means <see cref="EqualityComparer{T}.Default"/></param>
		/// <returns>A new set as the result</returns>
		IImmutableSet<T> IntersectWith(IImmutableSet<T> other, IEqualityComparer<T>? comparer = null);

		/// <summary>
		/// Generate a new set so that it contains all elements that are present in the current set, in the specified set, or in both.
		/// </summary>
		/// <param name="other">the other set</param>
		/// <param name="comparer">the <see cref="IEqualityComparer{T}"/> to use, default null means <see cref="EqualityComparer{T}.Default"/></param>
		/// <returns>A new set as the result</returns>
		IImmutableSet<T> UnionWith(IImmutableSet<T> other, IEqualityComparer<T>? comparer = null);

		/// <summary>
		/// Convert this set to an array
		/// </summary>
		/// <returns>A new array containing the elements</returns>
		T[] ToArray();
	}

	internal interface IStructImmutableSet<T> : IImmutableSet<T>
	{
		ImmutableSet<T> ToNormal() => new ImmutableSet<T>(this.ToArray());
	}
	#endregion

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
		public bool Equals(ImmutableSet<T>? other)
		{
			if (other is not null)
			{
				if (this.data.Length != other.data.Length)
					return false;
				if (this.hash != other.hash)
					return false;
				return this.ExceptWith(other, EqualityComparer<T>.Default).Count == 0;
			}
			else
			{
				return false;
			}
		}

		public bool Equals(IImmutableSet<T>? other)
		{
			if (other is not null)
			{
				if (this.data.Length != other.Count)
					return false;
				return this.ExceptWith(other, EqualityComparer<T>.Default).Count == 0;
			}
			else
			{
				return false;
			}
		}

		public override bool Equals(object? obj)
		{
			return this.Equals(obj as ImmutableSet<T>);
		}

		public static bool operator ==(ImmutableSet<T>? left, ImmutableSet<T>? right)
		{
			if (left is null == right is null)
				return true;
			else
				return (left is not null && left.Equals(right)) || (right is not null && right.Equals(left));
		}

		public static bool operator !=(ImmutableSet<T>? left, ImmutableSet<T>? right)
		{
			return !(left == right);
		}

		public override int GetHashCode()
		{
			return this.hash;
		}
		#endregion

		#region set op
		public IImmutableSet<T> ExceptWith(IImmutableSet<T> other, IEqualityComparer<T>? comparer)
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

		public IImmutableSet<T> IntersectWith(IImmutableSet<T> other, IEqualityComparer<T>? comparer)
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

		public IImmutableSet<T> UnionWith(IImmutableSet<T> other, IEqualityComparer<T>? comparer)
		{
			if (other is null)
				throw new ArgumentNullException(nameof(other));
			var except = (ImmutableSet<T>)other.ExceptWith(this, comparer);
			var res = new T[this.Count + except.Count];
			Array.Copy(this.data, res, this.Count);
			Array.Copy(except.data, 0, res, this.Count, except.Count);
			return new ImmutableSet<T>(res);
		}
		#endregion

		#region other
		public T[] ToArray()
		{
			return (T[])this.data.Clone();
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

#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释
	/// <summary>
	/// The immutable set which only contains zero or one element
	/// </summary>
	/// <typeparam name="T">any data type</typeparam>
	public readonly struct ImmutableZeroOneElementSet<T> : IStructImmutableSet<T>, IEquatable<ImmutableZeroOneElementSet<T>>
	{
		#region basic
		private readonly bool hasValue;

		private readonly T data;

		public T this[int index] => this.hasValue && index == 0 ? this.data : throw new ArgumentOutOfRangeException(nameof(index));

		public int Count => this.hasValue ? 1 : 0;

		/// <summary>
		/// Create a <see cref="ImmutableZeroOneElementSet{T}"/> with given one element
		/// </summary>
		/// <param name="data">the given element</param>
		public ImmutableZeroOneElementSet(T data)
		{
			this.hasValue = true;
			this.data = data;
		}
		#endregion

		#region equality
		public bool Equals(ImmutableZeroOneElementSet<T> other)
		{
			if (this.Count != other.Count)
				return false;
			if (!this.hasValue)
				return true;
			var c = EqualityComparer<T>.Default;
			return c.Equals(this.data, other.data);
		}

		public bool Equals(IImmutableSet<T>? other)
		{
			if (other is null)
				return false;
			if (other.Count != this.Count)
				return false;
			if (other is ImmutableZeroOneElementSet<T> set)
				return this.Equals(set);
			var c = EqualityComparer<T>.Default;
			return c.Equals(this.data, other[0]);
		}

		public override bool Equals(object? obj)
		{
			return this.Equals(obj as IImmutableSet<T>);
		}

		/// <summary>
		/// Equality operator
		/// </summary>
		public static bool operator ==(ImmutableZeroOneElementSet<T> left, ImmutableZeroOneElementSet<T> right)
		{
			return left.Equals(right);
		}

		/// <summary>
		/// Inequality operator
		/// </summary>
		public static bool operator !=(ImmutableZeroOneElementSet<T> left, ImmutableZeroOneElementSet<T> right)
		{
			return !(left == right);
		}

		public override int GetHashCode()
		{
			return this.hasValue ? HashCode.Combine(this.data) : 0;
		}
		#endregion

		#region set op
		private bool GetContains(IImmutableSet<T> other, IEqualityComparer<T>? comparer)
		{
			if (other is null)
				throw new ArgumentNullException(nameof(other));
			if (!this.hasValue)
				return false;
			bool contain = other.Contains(this.data, comparer);
			return contain;
		}

		public IImmutableSet<T> ExceptWith(IImmutableSet<T> other, IEqualityComparer<T>? comparer = null)
		{
			var contain = this.GetContains(other, comparer);
			if (contain || !this.hasValue)
				return new ImmutableZeroOneElementSet<T>();
			else
				return new ImmutableZeroOneElementSet<T>(this.data);
		}

		public IImmutableSet<T> IntersectWith(IImmutableSet<T> other, IEqualityComparer<T>? comparer = null)
		{
			var contain = this.GetContains(other, comparer);
			if (!contain && this.hasValue)
				return new ImmutableZeroOneElementSet<T>(this.data);
			else
				return new ImmutableZeroOneElementSet<T>();
		}

		public IImmutableSet<T> UnionWith(IImmutableSet<T> other, IEqualityComparer<T>? comparer = null)
		{
			var contain = this.GetContains(other, comparer);
			if (contain || !this.hasValue)
			{
				return other;
			}
			else if (other.Count >= 3)
			{
				if (other is IStructImmutableSet<T> set)
					return set.ToNormal().UnionWith(this, comparer);
				else
					return other.UnionWith(this, comparer);
			}
			else if (other.Count == 2)
			{
				return new ImmutableThreeElementSet<T>(this.data, other[0], other[1]);
			}
			else if (other.Count == 1)
			{
				return new ImmutableTwoElementSet<T>(this.data, other[0]);
			}
			else // other.Count == 0
			{
				return new ImmutableZeroOneElementSet<T>(this.data);
			}
		}
		#endregion

		#region other
		public T[] ToArray()
		{
			return this.hasValue ? new[] { this.data } : Array.Empty<T>();
		}

		public IEnumerator<T> GetEnumerator()
		{
			if (this.hasValue)
				yield return this.data;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		/// <summary>
		/// Explicitly convert a <see cref="ImmutableZeroOneElementSet{T}"/> to a <typeparamref name="T"/>. The default value will be returned if <paramref name="set"/> is empty.
		/// </summary>
		/// <param name="set">The <see cref="ImmutableZeroOneElementSet{T}"/> to convert</param>
		public static explicit operator T?(ImmutableZeroOneElementSet<T> set) => set.hasValue ? set.data : default;

		/// <summary>
		/// Implicitly convert a <typeparamref name="T"/> to a <see cref="ImmutableZeroOneElementSet{T}"/> with zero or one element.
		/// </summary>
		/// <param name="data">The <typeparamref name="T"/> to convert</param>
		public static implicit operator ImmutableZeroOneElementSet<T>(T? data) => data is null ? new ImmutableZeroOneElementSet<T>() : new ImmutableZeroOneElementSet<T>(data);
		#endregion
	}

	/// <summary>
	/// The immutable set which only contains exactly two elements
	/// </summary>
	/// <typeparam name="T">any data type</typeparam>
	public readonly struct ImmutableTwoElementSet<T> : IImmutableSet<T>, IEquatable<ImmutableTwoElementSet<T>>
	{
		#region basic
		private readonly T data1, data2;

		public T this[int index] => index == 0 ? data1 : index == 1 ? data2 : throw new ArgumentOutOfRangeException(nameof(index));

		public int Count => 2;

		/// <summary>
		/// Create a <see cref="ImmutableTwoElementSet{T}"/> with given two elements
		/// </summary>
		/// <param name="data1">the first given element</param>
		/// <param name="data2">the second given element</param>
		/// <exception cref="InvalidOperationException">if <paramref name="data1"/> == <paramref name="data2"/></exception>
		public ImmutableTwoElementSet(T data1, T data2)
		{
			if (EqualityComparer<T>.Default.Equals(data1, data2))
				throw new InvalidOperationException(Resources.Parameter.DuplicateValue);
			this.data1 = data1; this.data2 = data2;
		}
		#endregion

		#region equality
		public bool Equals(ImmutableTwoElementSet<T> other)
		{
			var c = EqualityComparer<T>.Default;
			return (c.Equals(this.data1, other.data1) || c.Equals(this.data1, other.data2)) &&
					(c.Equals(this.data2, other.data1) || c.Equals(this.data2, other.data2));
		}

		public bool Equals(IImmutableSet<T>? other)
		{
			if (other is null)
				return false;
			if (other.Count != this.Count)
				return false;
			if (other is ImmutableTwoElementSet<T> set)
				return this.Equals(set);
			var c = EqualityComparer<T>.Default;
			return (c.Equals(this.data1, other[0]) || c.Equals(this.data1, other[1])) &&
					(c.Equals(this.data2, other[0]) || c.Equals(this.data2, other[1]));
		}

		public override bool Equals(object? obj)
		{
			return this.Equals(obj as IImmutableSet<T>);
		}

		/// <summary>
		/// Equality operator
		/// </summary>
		public static bool operator ==(ImmutableTwoElementSet<T> left, ImmutableTwoElementSet<T> right)
		{
			return left.Equals(right);
		}

		/// <summary>
		/// Inequality operator
		/// </summary>
		public static bool operator !=(ImmutableTwoElementSet<T> left, ImmutableTwoElementSet<T> right)
		{
			return !(left == right);
		}

		public override int GetHashCode()
		{
			return unchecked(this.data1?.GetHashCode() ?? 0 + this.data2?.GetHashCode() ?? 0);
		}
		#endregion

		#region set op
		private (bool, bool) GetContains(IImmutableSet<T> other, IEqualityComparer<T>? comparer)
		{
			if (other is null)
				throw new ArgumentNullException(nameof(other));
			bool contain1 = other.Contains(this.data1, comparer);
			bool contain2 = other.Contains(this.data2, comparer);
			return (contain1, contain2);
		}

		public IImmutableSet<T> ExceptWith(IImmutableSet<T> other, IEqualityComparer<T>? comparer = null)
		{
			var (contain1, contain2) = this.GetContains(other, comparer);
			if (contain1 && contain2)
				return new ImmutableZeroOneElementSet<T>();
			else if (contain1)
				return new ImmutableZeroOneElementSet<T>(this.data2);
			else if (contain2)
				return new ImmutableZeroOneElementSet<T>(this.data1);
			else
				return new ImmutableTwoElementSet<T>(this.data1, this.data2);
		}

		public IImmutableSet<T> IntersectWith(IImmutableSet<T> other, IEqualityComparer<T>? comparer = null)
		{
			var (contain1, contain2) = this.GetContains(other, comparer);
			if (contain1 && contain2)
				return new ImmutableTwoElementSet<T>(this.data1, this.data2);
			else if (contain1)
				return new ImmutableZeroOneElementSet<T>(this.data1);
			else if (contain2)
				return new ImmutableZeroOneElementSet<T>(this.data2);
			else
				return new ImmutableZeroOneElementSet<T>();
		}

		public IImmutableSet<T> UnionWith(IImmutableSet<T> other, IEqualityComparer<T>? comparer = null)
		{
			var (contain1, contain2) = this.GetContains(other, comparer);
			int count = this.Count + other.Count - (contain1 ? 1 : 0) - (contain2 ? 1 : 0);
			if (count > 3)
			{
				if (other is IStructImmutableSet<T> set)
					return set.ToNormal().UnionWith(this, comparer);
				else
					return other.UnionWith(this, comparer);
			}
			else if (count == 2)
			{
				return new ImmutableTwoElementSet<T>(this.data1, this.data2);
			}
			else if (count == 3)
			{
				if (contain1 && contain2)
					return new ImmutableThreeElementSet<T>(other[0], other[1], other[2]);
				else if (contain1)
					return new ImmutableThreeElementSet<T>(this.data2, other[0], other[1]);
				else if (contain2)
					return new ImmutableThreeElementSet<T>(this.data1, other[0], other[1]);
				else
					return new ImmutableThreeElementSet<T>(this.data1, this.data2, other[0]);
			}
			else // count == 0 or 1 is not possible
			{
				return new ImmutableZeroOneElementSet<T>();
			}
		}
		#endregion

		#region other
		public T[] ToArray()
		{
			return new[] { this.data1, this.data2 };
		}

		public IEnumerator<T> GetEnumerator()
		{
			yield return this.data1;
			yield return this.data2;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		/// <summary>
		/// Implicitly convert a <see cref="ImmutableTwoElementSet{T}"/> to a <see cref="ValueTuple{T1, T2}"/>.
		/// </summary>
		/// <param name="set">The <see cref="ImmutableTwoElementSet{T}"/> to convert</param>
		public static implicit operator ValueTuple<T, T>(ImmutableTwoElementSet<T> set) => (set.data1, set.data2);

		/// <summary>
		/// Implicitly convert a <see cref="ValueTuple{T1, T2}"/> to a <see cref="ImmutableTwoElementSet{T}"/>.
		/// </summary>
		/// <param name="v">The <see cref="ValueTuple{T1, T2}"/> to convert</param>
		public static implicit operator ImmutableTwoElementSet<T>(ValueTuple<T, T>  v) => new ImmutableTwoElementSet<T>(v.Item1, v.Item2);
		#endregion
	}

	/// <summary>
	/// The immutable set which only contains exactly three elements
	/// </summary>
	/// <typeparam name="T">any data type</typeparam>
	public readonly struct ImmutableThreeElementSet<T> : IImmutableSet<T>, IEquatable<ImmutableThreeElementSet<T>>
	{
		#region basic
		private readonly T data1, data2, data3;

		public T this[int index] => index == 0 ? data1 : index == 1 ? data2 : index == 2 ? data3 : throw new ArgumentOutOfRangeException(nameof(index));

		public int Count => 3;

		/// <summary>
		/// Create a <see cref="ImmutableThreeElementSet{T}"/> with given three elements
		/// </summary>
		/// <param name="data1">the first given element</param>
		/// <param name="data2">the second given element</param>
		/// <param name="data3">the third given element</param>
		/// <exception cref="InvalidOperationException">if <paramref name="data1"/> == <paramref name="data2"/> or <paramref name="data2"/> == <paramref name="data3"/></exception>
		public ImmutableThreeElementSet(T data1, T data2, T data3)
		{
			var c = EqualityComparer<T>.Default;
			if (c.Equals(data1, data2) || c.Equals(data2, data3))
				throw new InvalidOperationException(Resources.Parameter.DuplicateValue);
			this.data1 = data1; this.data2 = data2; this.data3 = data3;
		}
		#endregion

		#region equality
		public bool Equals(ImmutableThreeElementSet<T> other)
		{
			var c = EqualityComparer<T>.Default;
			return	(c.Equals(this.data1, other.data1) || c.Equals(this.data1, other.data2) || c.Equals(this.data1, other.data3)) &&
					(c.Equals(this.data2, other.data1) || c.Equals(this.data2, other.data2) || c.Equals(this.data2, other.data3)) &&
					(c.Equals(this.data3, other.data1) || c.Equals(this.data3, other.data2) || c.Equals(this.data3, other.data3));
		}

		public bool Equals(IImmutableSet<T>? other)
		{
			if (other is null)
				return false;
			if (other.Count != this.Count)
				return false;
			if (other is ImmutableThreeElementSet<T> set)
				return this.Equals(set);
			var c = EqualityComparer<T>.Default;
			return	(c.Equals(this.data1, other[0]) || c.Equals(this.data1, other[1]) || c.Equals(this.data1, other[2])) &&
					(c.Equals(this.data2, other[0]) || c.Equals(this.data2, other[1]) || c.Equals(this.data2, other[2])) &&
					(c.Equals(this.data3, other[0]) || c.Equals(this.data3, other[1]) || c.Equals(this.data3, other[2]));
		}

		public override bool Equals(object? obj)
		{
			return this.Equals(obj as IImmutableSet<T>);
		}

		/// <summary>
		/// Equality operator
		/// </summary>
		public static bool operator ==(ImmutableThreeElementSet<T> left, ImmutableThreeElementSet<T> right)
		{
			return left.Equals(right);
		}

		/// <summary>
		/// Inequality operator
		/// </summary>
		public static bool operator !=(ImmutableThreeElementSet<T> left, ImmutableThreeElementSet<T> right)
		{
			return !(left == right);
		}

		public override int GetHashCode()
		{
			return unchecked(this.data1?.GetHashCode() ?? 0 + this.data2?.GetHashCode() ?? 0 + this.data3?.GetHashCode() ?? 0);
		}
		#endregion

		#region set op
		private (bool, bool, bool) GetContains(IImmutableSet<T> other, IEqualityComparer<T>? comparer)
		{
			if (other is null)
				throw new ArgumentNullException(nameof(other));
			bool contain1 = other.Contains(this.data1, comparer);
			bool contain2 = other.Contains(this.data2, comparer);
			bool contain3 = other.Contains(this.data3, comparer);
			return (contain1, contain2, contain3);
		}

		public IImmutableSet<T> ExceptWith(IImmutableSet<T> other, IEqualityComparer<T>? comparer = null)
		{
			var (contain1, contain2, contain3) = this.GetContains(other, comparer);
			if (contain1 && contain2 && contain3)
				return new ImmutableZeroOneElementSet<T>();
			else if (contain1 && contain2)
				return new ImmutableZeroOneElementSet<T>(this.data3);
			else if (contain2 && contain3)
				return new ImmutableZeroOneElementSet<T>(this.data1);
			else if (contain3 && contain1)
				return new ImmutableZeroOneElementSet<T>(this.data2);
			else if (contain1)
				return new ImmutableTwoElementSet<T>(this.data2, this.data3);
			else if (contain2)
				return new ImmutableTwoElementSet<T>(this.data1, this.data3);
			else if (contain3)
				return new ImmutableTwoElementSet<T>(this.data1, this.data2);
			else
				return new ImmutableThreeElementSet<T>(this.data1, this.data2, this.data3);
		}

		public IImmutableSet<T> IntersectWith(IImmutableSet<T> other, IEqualityComparer<T>? comparer = null)
		{
			var (contain1, contain2, contain3) = this.GetContains(other, comparer);
			if (contain1 && contain2 && contain3)
				return new ImmutableThreeElementSet<T>(this.data1, this.data2, this.data3);
			else if (contain1 && contain2)
				return new ImmutableTwoElementSet<T>(this.data1, this.data2);
			else if (contain2 && contain3)
				return new ImmutableTwoElementSet<T>(this.data2, this.data3);
			else if (contain3 && contain1)
				return new ImmutableTwoElementSet<T>(this.data3, this.data1);
			else if (contain1)
				return new ImmutableZeroOneElementSet<T>(this.data1);
			else if (contain2)
				return new ImmutableZeroOneElementSet<T>(this.data2);
			else if (contain3)
				return new ImmutableZeroOneElementSet<T>(this.data3);
			else
				return new ImmutableZeroOneElementSet<T>();
		}

		public IImmutableSet<T> UnionWith(IImmutableSet<T> other, IEqualityComparer<T>? comparer = null)
		{
			var (contain1, contain2, contain3) = this.GetContains(other, comparer);
			int count = this.Count + other.Count - (contain1 ? 1 : 0) - (contain2 ? 1 : 0) - (contain3 ? 1 : 0);
			if (count > 3)
			{
				if (other is IStructImmutableSet<T> set)
					return set.ToNormal().UnionWith(this, comparer);
				else
					return other.UnionWith(this, comparer);
			}
			else if (count == 3)
			{
				return new ImmutableThreeElementSet<T>(this.data1, this.data2, this.data3);
			}
			else // count == 0 or 1 or 2 is not possible
			{
				return new ImmutableZeroOneElementSet<T>();
			}
		}
		#endregion

		#region other
		public T[] ToArray()
		{
			return new[] { this.data1, this.data2 };
		}

		public IEnumerator<T> GetEnumerator()
		{
			yield return this.data1;
			yield return this.data2;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		/// <summary>
		/// Implicitly convert a <see cref="ImmutableThreeElementSet{T}"/> to a <see cref="ValueTuple{T1, T2, T3}"/>.
		/// </summary>
		/// <param name="set">The <see cref="ImmutableThreeElementSet{T}"/> to convert</param>
		public static implicit operator ValueTuple<T, T, T>(ImmutableThreeElementSet<T> set) => (set.data1, set.data2, set.data3);

		/// <summary>
		/// Implicitly convert a <see cref="ValueTuple{T1, T2, T3}"/> to a <see cref="ImmutableThreeElementSet{T}"/>.
		/// </summary>
		/// <param name="v">The <see cref="ValueTuple{T1, T2, T3}"/> to convert</param>
		public static implicit operator ImmutableThreeElementSet<T>(ValueTuple<T, T, T> v) => new ImmutableThreeElementSet<T>(v.Item1, v.Item2, v.Item3);
		#endregion
	}

#pragma warning restore CS1591 // 缺少对公共可见类型或成员的 XML 注释
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

		private int[]? _multiplicity = null;

		public IReadOnlyList<int> Multiplicities()
		{
			if (this._multiplicity is not null)
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

	internal class ReadOnlyGrouping<TKey, TElement> : IReadOnlyGrouping<TKey, TElement>
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
}
