using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Althea.Helpers;
using Althea.NativeTypes;


namespace Althea.Statistics
{
	#region interface
	/// <summary>
	/// The interface for the meta-data of a random number generator which generates random numbers from random distributions.<br/>
	/// The inherited <see cref="IReadOnlyList{T}"/> of <see cref="DataType"/> indicates the data type(s) of random variable(s) of this distribution.
	/// </summary>
	public interface IRandomDistribution : IReadOnlyList<DataType>
	{
		/// <summary>
		/// When implemented by a derived class, get the random seed of this <see cref="IRandomDistribution"/>
		/// </summary>
		long RandomSeed { get; }

		/// <summary>
		/// When implemented by a derived class, get the string representation of this <see cref="IRandomDistribution"/>
		/// </summary>
		/// <returns>The string representation of this <see cref="IRandomDistribution"/></returns>
		string ToString();
	}
	#endregion

	#region uniform
	/// <summary>
	/// The class for a one-dimensional uniform distribution
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
	public sealed class UniformDistribution<T> : IRandomDistribution, IMainPropertyFormat where T : unmanaged
	{
		/// <summary>
		/// Get the random seed of this uniform distribution
		/// </summary>
		public long RandomSeed { get; }

		/// <summary>
		/// Get the inclusive lower bound of this one-dimensional uniform distribution
		/// </summary>
		public T LowerBound { get; }

		/// <summary>
		/// Get the exclusive upper bound of this one-dimensional uniform distribution
		/// </summary>
		public T UpperBound { get; }

		/// <summary>
		/// Create a new <see cref="UniformDistribution{T}"/> with the given <paramref name="lower"/> and <paramref name="upper"/> bounds and the random <paramref name="seed"/>
		/// </summary>
		/// <param name="upper">The inclusive lower bound of this one-dimensional uniform distribution</param>
		/// <param name="lower">The exclusive upper bound of this one-dimensional uniform distribution</param>
		/// <param name="seed">The random seed of this uniform distribution, default 0 means <see cref="Random.Next()"/></param>
		public UniformDistribution(T upper, T lower = default, long seed = 0)
		{
			this.UpperBound = upper; this.LowerBound = lower; this.RandomSeed = seed == 0 ? new Random().Next() : seed;
		}

		/// <summary>
		/// Get the number of random variables of this uniform distribution
		/// </summary>
		public int Count {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => 1;
		}

		/// <summary>
		/// Get the <see cref="DataType"/> of the random variable at <paramref name="index"/>
		/// </summary>
		/// <param name="index">The index of the random variable</param>
		/// <returns>The <see cref="DataType"/> of the random variable at <paramref name="index"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="index"/> is out of range</exception>
		public DataType this[int index] {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => index != 0 ? throw new ArgumentOutOfRangeException(nameof(index), index, Resources.Parameter.InvalidValue) : Const<T>.DataType;
		}

		/// <summary>
		/// Get the <see cref="IEnumerator{T}"/> of <see cref="DataType"/> of the random variable of this uniform distribution 
		/// </summary>
		/// <returns>The <see cref="IEnumerator{T}"/> of <see cref="DataType"/> of the random variable</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public IEnumerator<DataType> GetEnumerator()
		{
			yield return Const<T>.DataType;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

		string IMainPropertyFormat.StringMain {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => nameof(UniformDistribution<T>);
		}

		IEnumerable<KeyValuePair<string, object?>> IMainPropertyFormat.StringProperties {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => new KeyValuePair<string, object?>[]
			{
				new("Dimension", 1),
				new(nameof(this.RandomSeed), this.RandomSeed),
				new(nameof(DataType), typeof(T).GetGenericString()),
				new(nameof(this.LowerBound), this.LowerBound),
				new(nameof(this.UpperBound), this.UpperBound),
			};
		}

		/// <summary>
		/// Get the string representation of this uniform distribution 
		/// </summary>
		/// <returns>The string representation of this uniform distribution</returns>
		public override string ToString()
		{
			return ((IMainPropertyFormat)this).ToString();
		}
	}
	#endregion

	#region random bytes
	/// <summary>
	/// The class for a one-dimensional distribution that randomizes each bit
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
	public sealed class RandomBitsDistribution<T> : IRandomDistribution, IMainPropertyFormat where T : unmanaged
	{
		/// <summary>
		/// Get the random seed of this uniform distribution
		/// </summary>
		public long RandomSeed { get; }

		/// <summary>
		/// Create a new <see cref="UniformDistribution{T}"/> with the given random <paramref name="seed"/>
		/// </summary>
		/// <param name="seed">The random seed of this uniform distribution, default 0 means <see cref="Random.Next()"/></param>
		public RandomBitsDistribution(long seed = 0)
		{
			this.RandomSeed = seed == 0 ? new Random().Next() : seed;
		}

		/// <summary>
		/// Get the number of random variables of this uniform distribution
		/// </summary>
		public int Count {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => 1;
		}

		/// <summary>
		/// Get the <see cref="DataType"/> of the random variable at <paramref name="index"/>
		/// </summary>
		/// <param name="index">The index of the random variable</param>
		/// <returns>The <see cref="DataType"/> of the random variable at <paramref name="index"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="index"/> is out of range</exception>
		public DataType this[int index] {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => index != 0 ? throw new ArgumentOutOfRangeException(nameof(index), index, Resources.Parameter.InvalidValue) : Const<T>.DataType;
		}

		/// <summary>
		/// Get the <see cref="IEnumerator{T}"/> of <see cref="DataType"/> of the random variable of this uniform distribution 
		/// </summary>
		/// <returns>The <see cref="IEnumerator{T}"/> of <see cref="DataType"/> of the random variable</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public IEnumerator<DataType> GetEnumerator()
		{
			yield return Const<T>.DataType;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

		string IMainPropertyFormat.StringMain {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => nameof(UniformDistribution<T>);
		}

		IEnumerable<KeyValuePair<string, object?>> IMainPropertyFormat.StringProperties {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => new KeyValuePair<string, object?>[]
			{
				new("Dimension", 1),
				new(nameof(this.RandomSeed), this.RandomSeed),
				new(nameof(DataType), typeof(T).GetGenericString()),
			};
		}

		/// <summary>
		/// Get the string representation of this uniform distribution 
		/// </summary>
		/// <returns>The string representation of this uniform distribution</returns>
		public override string ToString()
		{
			return ((IMainPropertyFormat)this).ToString();
		}
	}
	#endregion

	#region combine
	/// <summary>
	/// The class for a multi-variate random distribution as a simple joint of several <see cref="IRandomDistribution"/>s whose random seed is simply the sum of all children <see cref="IRandomDistribution.RandomSeed"/>s.
	/// </summary>
	public sealed class SimpleJointRandomDistribution : IRandomDistribution, IReadOnlyList<IRandomDistribution>
	{
		private readonly long m_seed;

		private readonly FixedClassBuffer_16<IRandomDistribution> m_distributions;

		private readonly int m_count;

		/// <summary>
		/// Create a new <see cref="SimpleJointRandomDistribution"/> from the given <paramref name="distributions"/>
		/// </summary>
		/// <param name="distributions">The list of <see cref="IRandomDistribution"/> used to create a <see cref="SimpleJointRandomDistribution"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="distributions"/> is null or empty</exception>
		/// <exception cref="ArgumentException">If any of <paramref name="distributions"/> has rank larger than 1</exception>
		public SimpleJointRandomDistribution(params IRandomDistribution[] distributions)
		{
			if (distributions is null || distributions.Length == 0)
				throw new ArgumentNullException(nameof(distributions));

			this.m_count = distributions.Length;
			this.m_distributions = new(distributions);
			this.m_seed = 0;
			for (int i = 0; i < distributions.Length; i++)
			{
				var dist = distributions[i];
				if (dist.Count != 1)
					throw new ArgumentException(Resources.Parameter.WrongSize, nameof(distributions));
				this.m_seed = unchecked(this.m_seed + dist.RandomSeed);
				if (dist is SimpleJointRandomDistribution joint)
					this.m_distributions[i] = joint.m_distributions[0];
				else
					this.m_distributions[i] = dist;
			}
		}

		/// <summary>
		/// Get the <see cref="DataType"/> of the random variable at <paramref name="index"/>
		/// </summary>
		/// <param name="index">The index of the random variable</param>
		/// <returns>The <see cref="DataType"/> of the random variable at <paramref name="index"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="index"/> is out of range</exception>
		public DataType this[int index] {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => index < 0 || index >= this.m_count ?
					throw new ArgumentOutOfRangeException(nameof(index), index, Resources.Parameter.InvalidValue) :
					this.m_distributions[index][0];
		}

		/// <summary>
		/// Get the random seed of this joint distribution
		/// </summary>
		public long RandomSeed {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.m_seed;
		}

		/// <summary>
		/// Get the number of random variables of this joint distribution
		/// </summary>
		public int Count {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.m_count;
		}

		IRandomDistribution IReadOnlyList<IRandomDistribution>.this[int index] {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => index < 0 || index >= this.m_count ?
					throw new ArgumentOutOfRangeException(nameof(index), index, Resources.Parameter.InvalidValue) :
					this.m_distributions[index];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		IEnumerator<IRandomDistribution> IEnumerable<IRandomDistribution>.GetEnumerator()
		{
			for (int i = 0; i < this.m_count; i++)
			{
				yield return this.m_distributions[i];
			}
		}

		/// <summary>
		/// Get the <see cref="IEnumerator{T}"/> of <see cref="DataType"/> of the random variable of this uniform distribution 
		/// </summary>
		/// <returns>The <see cref="IEnumerator{T}"/> of <see cref="DataType"/> of the random variable</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public IEnumerator<DataType> GetEnumerator()
		{
			for (int i = 0; i < this.m_count; i++)
			{
				yield return this.m_distributions[i][0];
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

		/// <summary>
		/// Get the string representation of this joint distribution 
		/// </summary>
		/// <returns>The string representation of this joint distribution</returns>
		public override string ToString()
		{
			if (this.m_count == 1)
				return this.m_distributions[0].ToString();
			else
				return $"{{({string.Join("), (", this.m_distributions)})}}";
		}
	}
	#endregion
}
