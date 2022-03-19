using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Althea.Helpers;
using Althea.NativeTypes;


namespace Althea.Random
{
	#region abstract
	/// <summary>
	/// The interface for the meta-data of a random number generator which generates random numbers from a certain random distribution.<br/>
	/// The inherited <see cref="IReadOnlyList{T}"/> of <see cref="DataType"/> indicates the data type(s) of random variable(s) of this distribution.
	/// </summary>
	public interface IRandomDistribution : IReadOnlyList<DataType>
	{
		/// <summary>
		/// When implemented by a derived class, get the random seed of this <see cref="IRandomDistribution"/>. Null means let the internal implementation determine.
		/// </summary>
		long? RandomSeed { get; }

		/// <summary>
		/// When implemented by a derived class, get the string representation of this <see cref="IRandomDistribution"/>
		/// </summary>
		/// <returns>The string representation of this <see cref="IRandomDistribution"/></returns>
		string ToString();
	}

	/// <summary>
	/// The abstract class for distributions whose data type(s) contain a real type <typeparamref name="T"/>
	/// </summary>
	/// <typeparam name="T">An unmanaged real type as one of the data type(s)</typeparam>
	public abstract class RealTypedDistribution<T> : IRandomDistribution where T : unmanaged, INumber<T>
	{
		/// <summary>
		/// Get the random seed of this <see cref="RealTypedDistribution{T}"/>. Null means let the internal implementation determine.
		/// </summary>
		public long? RandomSeed {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
		}

		static RealTypedDistribution()
		{
			if (Const<T>.IsComplex)
				throw new TypeMismatchException(typeof(T), TypeMismatchException.MismatchReason.NotReal);
		}

		/// <summary>
		/// Initialize a <see cref="RealTypedDistribution{T}"/> with given random <paramref name="seed"/>
		/// </summary>
		/// <param name="seed">The given random seed</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected RealTypedDistribution(long? seed = null)
		{
			this.RandomSeed = seed;
		}

		/// <summary>
		/// When implemented by a derived class, get the rank / number of dimensions of this distribution, always 1
		/// </summary>
		public abstract int Count { get; }

		/// <summary>
		/// When implemented by a derived class, get the <see cref="DataType"/> at dimension <paramref name="index"/>
		/// </summary>
		/// <param name="index">The index, must be 0</param>
		/// <returns>The <see cref="DataType"/> of <typeparamref name="T"/></returns>
		public abstract DataType this[int index] { get; }

		/// <summary>
		/// When implemented by a derived class, get the enumerator of the <see cref="DataType"/> of the dimensions of this distribution
		/// </summary>
		public abstract IEnumerator<DataType> GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

		/// <summary>
		/// When implemented by a derived class, get the string representation of the property-value pairs of this <see cref="RealTypedDistribution{T}"/>
		/// </summary>
		protected virtual string PropertiesString => $"DataType={typeof(T).GetGenericString()}, Dimension={this.Count}" + (this.RandomSeed.HasValue ? $", {nameof(this.RandomSeed)}={this.RandomSeed}" : "");

		/// <summary>
		/// Get the string representation of this <see cref="RealTypedDistribution{T}"/>
		/// </summary>
		/// <returns>The string representation of this <see cref="RealTypedDistribution{T}"/></returns>
		public override string ToString()
		{
			return this.GetType().Name + "[" + this.PropertiesString + "]";
		}
	}

	/// <summary>
	/// The abstract class for distributions whose data type(s) contain a floating point type <typeparamref name="T"/>
	/// </summary>
	/// <typeparam name="T">An unmanaged floating point type as one of the data type(s)</typeparam>
	public abstract class FloatTypedDistribution<T> : RealTypedDistribution<T> where T : unmanaged, INumber<T>
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void Check()
		{
			if (Const<T>.DataTypeClass == DataTypeClassification.SignedInteger || Const<T>.DataTypeClass == DataTypeClassification.UnsignedInteger)
				throw new TypeMismatchException(typeof(T), TypeMismatchException.MismatchReason.NotFloat);
		}

		static FloatTypedDistribution()
		{
			Check();
		}

		/// <summary>
		/// Initialize a <see cref="FloatTypedDistribution{T}"/> with given random <paramref name="seed"/>
		/// </summary>
		/// <param name="seed">The given random seed</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected FloatTypedDistribution(long? seed = null) : base(seed) { }
	}

	/// <summary>
	/// The abstract class for distributions whose data type(s) contain an integral type <typeparamref name="T"/>
	/// </summary>
	/// <typeparam name="T">An unmanaged integral type as one of the data type(s)</typeparam>
	public abstract class IntegerTypedDistribution<T> : RealTypedDistribution<T> where T : unmanaged, INumber<T>
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void Check()
		{
			if (Const<T>.DataTypeClass != DataTypeClassification.SignedInteger && Const<T>.DataTypeClass != DataTypeClassification.UnsignedInteger)
				throw new TypeMismatchException(typeof(T), TypeMismatchException.MismatchReason.NotInteger);
		}

		static IntegerTypedDistribution()
		{
			Check();
		}

		/// <summary>
		/// Initialize a <see cref="IntegerTypedDistribution{T}"/> with given random <paramref name="seed"/>
		/// </summary>
		/// <param name="seed">The given random seed</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected IntegerTypedDistribution(long? seed = null) : base(seed) { }
	}

	/// <summary>
	/// The abstract class for one-dimensional distributions whose data type is a real type <typeparamref name="T"/>
	/// </summary>
	/// <typeparam name="T">An unmanaged real type as the data type</typeparam>
	public abstract class OneDimensionalRealTypedDistribution<T> : RealTypedDistribution<T> where T : unmanaged, INumber<T>
	{
		/// <summary>
		/// Initialize a <see cref="OneDimensionalRealTypedDistribution{T}"/> with given random <paramref name="seed"/>
		/// </summary>
		/// <param name="seed">The given random seed</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected OneDimensionalRealTypedDistribution(long? seed = null) : base(seed) { }

		/// <summary>
		/// Get the rank / number of dimensions of this distribution, always 1
		/// </summary>
		public override int Count {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => 1;
		}

		/// <summary>
		/// Get the <see cref="DataType"/> at dimension <paramref name="index"/>
		/// </summary>
		/// <param name="index">The index, must be 0</param>
		/// <returns>The <see cref="DataType"/> of <typeparamref name="T"/></returns>
		public override DataType this[int index] {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => index == 0 ? Const<T>.DataType : throw new ArgumentOutOfRangeException(nameof(index), index, Resources.ParameterError.InvalidValue);
		}

		/// <summary>
		/// Get the enumerator of the <see cref="DataType"/> of the dimensions of this distribution
		/// </summary>
		public override IEnumerator<DataType> GetEnumerator()
		{
			yield return Const<T>.DataType;
		}
	}

	/// <summary>
	/// The abstract class for one-dimensional distributions whose data type is a floating point type <typeparamref name="T"/>
	/// </summary>
	/// <typeparam name="T">An unmanaged floating point type as the data type</typeparam>
	public abstract class OneDimensionalFloatTypedDistribution<T> : OneDimensionalRealTypedDistribution<T> where T : unmanaged, INumber<T>
	{
		static OneDimensionalFloatTypedDistribution()
		{
			FloatTypedDistribution<T>.Check();
		}

		/// <summary>
		/// Initialize a <see cref="OneDimensionalFloatTypedDistribution{T}"/> with given random <paramref name="seed"/>
		/// </summary>
		/// <param name="seed">The given random seed</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected OneDimensionalFloatTypedDistribution(long? seed = null) : base(seed) { }
	}

	/// <summary>
	/// The abstract class for one-dimensional distributions whose data type is an integral type <typeparamref name="T"/>
	/// </summary>
	/// <typeparam name="T">An unmanaged integral type as the data type</typeparam>
	public abstract class OneDimensionalIntegerTypedDistribution<T> : OneDimensionalRealTypedDistribution<T> where T : unmanaged, INumber<T>
	{
		static OneDimensionalIntegerTypedDistribution()
		{
			IntegerTypedDistribution<T>.Check();
		}

		/// <summary>
		/// Initialize a <see cref="OneDimensionalIntegerTypedDistribution{T}"/> with given random <paramref name="seed"/>
		/// </summary>
		/// <param name="seed">The given random seed</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected OneDimensionalIntegerTypedDistribution(long? seed = null) : base(seed) { }
	}
	#endregion

	#region uniform
	/// <summary>
	/// The class for a one-dimensional uniform distribution
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	public class UniformDistribution<T> : OneDimensionalRealTypedDistribution<T> where T : unmanaged, INumber<T>
	{
		/// <summary>
		/// Get the inclusive lower bound of this one-dimensional uniform distribution
		/// </summary>
		public T LowerBound {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
		}

		/// <summary>
		/// Get the exclusive upper bound of this one-dimensional uniform distribution
		/// </summary>
		public T UpperBound {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
		}

		/// <summary>
		/// Create a new <see cref="UniformDistribution{T}"/> with the given <paramref name="lower"/> and <paramref name="upper"/> bounds and the random <paramref name="seed"/>
		/// </summary>
		/// <param name="upper">The inclusive lower bound of this one-dimensional uniform distribution</param>
		/// <param name="lower">The exclusive upper bound of this one-dimensional uniform distribution</param>
		/// <param name="seed">The random seed of this uniform distribution, default null means let the internal implementation determine</param>
		public UniformDistribution(T upper, T lower = default, long? seed = null) : base(seed)
		{
			this.UpperBound = upper; this.LowerBound = lower;
		}

		/// <summary>
		/// Create a new <see cref="UniformDistribution{T}"/> with the given random <paramref name="seed"/> and lower and upper bond equaling to 0 and 1 respectively.
		/// </summary>
		/// <param name="seed">The random seed of this uniform distribution, default null means let the internal implementation determine</param>
		public UniformDistribution(long? seed = null) : base(seed)
		{
			this.UpperBound = Const<T>.One; this.LowerBound = Const<T>.Zero;
		}

		/// <summary>
		/// Get the string representation of the property-value pairs of this <see cref="UniformDistribution{T}"/>
		/// </summary>
		protected override string PropertiesString => base.PropertiesString + $", {nameof(this.LowerBound)}={this.LowerBound}, {nameof(this.UpperBound)}={this.UpperBound}";
	}
	#endregion

	#region random bits
	/// <summary>
	/// The class for a one-dimensional distribution that randomizes each bit
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	public class RandomBitsDistribution<T> : OneDimensionalRealTypedDistribution<T> where T : unmanaged, INumber<T>
	{
		/// <summary>
		/// Create a new <see cref="UniformDistribution{T}"/> with the given random <paramref name="seed"/>
		/// </summary>
		/// <param name="seed">The random seed of this uniform distribution, default null means let the internal implementation determine</param>
		public RandomBitsDistribution(long? seed = null) : base(seed) { }
	}
	#endregion

	#region combine
	/// <summary>
	/// The class for a multi-variate random distribution as a simple joint of several <see cref="IRandomDistribution"/>s whose random seed is simply the sum of all children <see cref="IRandomDistribution.RandomSeed"/>s.
	/// </summary>
	public class SimpleJointRandomDistribution : IRandomDistribution, IReadOnlyList<IRandomDistribution>
	{
		private readonly long? m_seed;

		private readonly IRandomDistribution[] m_distributions;

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
			this.m_distributions = (IRandomDistribution[])distributions.Clone();
			this.m_seed = 0;
			for (int i = 0; i < distributions.Length; i++)
			{
				var dist = distributions[i];
				if (dist.Count != 1)
					throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(distributions));
				this.m_seed = unchecked(this.m_seed + dist.RandomSeed ?? 0);
				if (dist is SimpleJointRandomDistribution joint)
					this.m_distributions[i] = joint.m_distributions[0];
				else
					this.m_distributions[i] = dist;
			}
			if (this.m_seed == 0)
				this.m_seed = null;
		}

		DataType IReadOnlyList<DataType>.this[int index] {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => index < 0 || index >= this.m_count ?
					throw new ArgumentOutOfRangeException(nameof(index), index, Resources.ParameterError.InvalidValue) :
					this.m_distributions[index][0];
		}

		/// <summary>
		/// Get the random seed of this joint distribution, null means let the internal implementation determine
		/// </summary>
		public long? RandomSeed {
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

		/// <summary>
		/// Get the <see cref="IRandomDistribution"/> of the random distribution at <paramref name="index"/>
		/// </summary>
		/// <param name="index">The index of the random distribution</param>
		/// <returns>The <see cref="IRandomDistribution"/> of the random distribution at <paramref name="index"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="index"/> is out of range</exception>
		public IRandomDistribution this[int index] {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => index < 0 || index >= this.m_count ?
					throw new ArgumentOutOfRangeException(nameof(index), index, Resources.ParameterError.InvalidValue) :
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
				return $"{{({string.Join<IRandomDistribution>("), (", this.m_distributions)})}}";
		}
	}
	#endregion
}
