using System;
using System.Runtime.CompilerServices;

using Althea.NativeTypes;
using Althea.Random;


namespace Althea.Backend.Random
{
	/// <summary>
	/// The abstract class for one-dimensional distributions which contain the <see cref="Displacement"/> and <see cref="ScaleFactor"/> of type <typeparamref name="T"/> as the parameters, implements <see cref="IRandomDistribution"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged floating point type</typeparam>
	public abstract class DisplaceScaleDistribution<T> : OneDimensionalFloatTypedDistribution<T> where T : unmanaged
	{
		/// <summary>
		/// Get the displacement (<c>a</c>) of this <see cref="DisplaceScaleDistribution{T}"/>
		/// </summary>
		public T Displacement {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
		}

		/// <summary>
		/// Get the scaling factor (<c>β</c>) of this <see cref="DisplaceScaleDistribution{T}"/>
		/// </summary>
		public T ScaleFactor {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
		}

		/// <summary>
		/// Create a standard <see cref="DisplaceScaleDistribution{T}"/> with a=0, β=1, and random seed is not set
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected DisplaceScaleDistribution() : base(null)
		{
			this.Displacement = Const<T>.Zero; this.ScaleFactor = Const<T>.One;
		}

		/// <summary>
		/// Create an <see cref="DisplaceScaleDistribution{T}"/> with given <paramref name="displacement"/>, <paramref name="scaleFactor"/> and the random <paramref name="seed"/>
		/// </summary>
		/// <param name="displacement">The given displacement a</param>
		/// <param name="scaleFactor">The given scale factor β</param>
		/// <param name="seed">The given random seed, default null means has no preferred random seed</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="scaleFactor"/> is not larger than 0</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected DisplaceScaleDistribution(T displacement, T scaleFactor, long? seed = null) : base(seed)
		{
			if (scaleFactor.NativeLessThanOrEqual(default))
				throw new ArgumentOutOfRangeException(nameof(scaleFactor), scaleFactor, Resources.Parameter.MustPositive);
			this.Displacement = displacement; this.ScaleFactor = scaleFactor;
		}

		/// <summary>
		/// Check whether the <paramref name="other"/> <see cref="DisplaceScaleDistribution{T}"/> represents the same distribution as this one
		/// </summary>
		/// <param name="other">The other <see cref="DisplaceScaleDistribution{T}"/> to compare</param>
		/// <returns>this == <paramref name="other"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected bool Equals(DisplaceScaleDistribution<T>? other)
		{
			if (other is null)
				return false;
			if (ReferenceEquals(this, other))
				return true;
			return this.Displacement.IsEqual(other.Displacement) && this.ScaleFactor.IsEqual(other.ScaleFactor) && this.RandomSeed == other.RandomSeed;
		}

		/// <summary>
		/// Check whether the given <paramref name="obj"/> represents the same distribution as this one
		/// </summary>
		/// <param name="obj">The other <see cref="object"/> to compare</param>
		/// <returns>this == <paramref name="obj"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override bool Equals(object? obj)
		{
			return obj is not null && this.Equals(obj as DisplaceScaleDistribution<T>) && obj.GetType() == this.GetType();
		}

		/// <summary>
		/// Get the hash code of this <see cref="DisplaceScaleDistribution{T}"/>
		/// </summary>
		/// <returns>The hash code of this <see cref="DisplaceScaleDistribution{T}"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode()
		{
			return HashCode.Combine(this.Displacement, this.ScaleFactor, this.RandomSeed);
		}

		/// <summary>
		/// Get the string representation of the property-value pairs of this <see cref="DisplaceScaleDistribution{T}"/>
		/// </summary>
		protected override string PropertiesString => base.PropertiesString + $", {nameof(this.Displacement)}={this.Displacement}, {nameof(this.ScaleFactor)}={this.ScaleFactor}";
	}

	/// <summary>
	/// The abstract class for one-dimensional distributions which contain the extract <see cref="ShapeFactor"/> and the parameters of <see cref="DisplaceScaleDistribution{T}"/> of type <typeparamref name="T"/> as the parameters, implements <see cref="IRandomDistribution"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged floating point type</typeparam>
	public abstract class ShapeDisplaceScaleDistribution<T> : DisplaceScaleDistribution<T> where T : unmanaged
	{
		/// <summary>
		/// Get the shaping factor (<c>α</c>) of this <see cref="ShapeDisplaceScaleDistribution{T}"/>
		/// </summary>
		public T ShapeFactor {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
		}

		/// <summary>
		/// Create a standard <see cref="ShapeDisplaceScaleDistribution{T}"/> with a=0, α=1, β=1, and random seed is not set
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected ShapeDisplaceScaleDistribution() : base()
		{
			this.ShapeFactor = Const<T>.One;
		}

		/// <summary>
		/// Create an <see cref="ShapeDisplaceScaleDistribution{T}"/> with given <paramref name="displacement"/>, <paramref name="shapeFactor"/>, <paramref name="scaleFactor"/> and the random <paramref name="seed"/>
		/// </summary>
		/// <param name="displacement">The given displacement a</param>
		/// <param name="shapeFactor">The given shape factor α</param>
		/// <param name="scaleFactor">The given scale factor β</param>
		/// <param name="seed">The given random seed, default null means has no preferred random seed</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="scaleFactor"/> or <paramref name="shapeFactor"/> is not larger than 0</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected ShapeDisplaceScaleDistribution(T displacement, T shapeFactor, T scaleFactor, long? seed = null) : base(displacement, scaleFactor, seed)
		{
			if (scaleFactor.NativeLessThanOrEqual(default))
				throw new ArgumentOutOfRangeException(nameof(scaleFactor), scaleFactor, Resources.Parameter.MustPositive);
			this.ShapeFactor = shapeFactor;
		}

		/// <summary>
		/// Check whether the <paramref name="other"/> <see cref="ShapeDisplaceScaleDistribution{T}"/> represents the same distribution as this one
		/// </summary>
		/// <param name="other">The other <see cref="ShapeDisplaceScaleDistribution{T}"/> to compare</param>
		/// <returns>this == <paramref name="other"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected bool Equals(ShapeDisplaceScaleDistribution<T>? other)
		{
			if (other is null)
				return false;
			if (ReferenceEquals(this, other))
				return true;
			return this.ShapeFactor.IsEqual(other.ShapeFactor) && base.Equals(other);
		}

		/// <summary>
		/// Check whether the given <paramref name="obj"/> represents the same distribution as this one
		/// </summary>
		/// <param name="obj">The other <see cref="object"/> to compare</param>
		/// <returns>this == <paramref name="obj"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override bool Equals(object? obj)
		{
			return obj is not null && this.Equals(obj as ShapeDisplaceScaleDistribution<T>) && obj.GetType() == this.GetType();
		}

		/// <summary>
		/// Get the hash code of this <see cref="ShapeDisplaceScaleDistribution{T}"/>
		/// </summary>
		/// <returns>The hash code of this <see cref="ShapeDisplaceScaleDistribution{T}"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode()
		{
			return HashCode.Combine(this.ShapeFactor, base.GetHashCode());
		}

		/// <summary>
		/// Get the string representation of the property-value pairs of this <see cref="ShapeDisplaceScaleDistribution{T}"/>
		/// </summary>
		protected override string PropertiesString => base.PropertiesString + $", {nameof(this.ShapeFactor)}={this.ShapeFactor}";
	}

	/// <summary>
	/// The abstract class for one-dimensional distributions based on the Bernoulli trial, implements <see cref="OneDimensionalIntegerTypedDistribution{T}"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged integral type</typeparam>
	public abstract class BernoulliBasedDistribution<T> : OneDimensionalIntegerTypedDistribution<T> where T : unmanaged
	{
		/// <summary>
		/// Get the probability of the trial success of this <see cref="BernoulliBasedDistribution{T}"/>
		/// </summary>
		public double Probability {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
		}

		/// <summary>
		/// Create a <see cref="BernoulliBasedDistribution{T}"/> with given <paramref name="p"/> and the random <paramref name="seed"/>
		/// </summary>
		/// <param name="p">The given probability of the trial success</param>
		/// <param name="seed">The given random seed, default null means has no preferred random seed</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="p"/> is not in range (0.0, 1.0)</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected BernoulliBasedDistribution(double p, long? seed = null) : base(seed)
		{
			if (p <= 0 || p >= 1)
				throw new ArgumentOutOfRangeException(nameof(p), p, Resources.Parameter.InvalidValue);
			this.Probability = p;
		}

		/// <summary>
		/// Check whether the <paramref name="other"/> <see cref="BernoulliBasedDistribution{T}"/> represents the same distribution as this one
		/// </summary>
		/// <param name="other">The other <see cref="BernoulliBasedDistribution{T}"/> to compare</param>
		/// <returns>this == <paramref name="other"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected bool Equals(BernoulliBasedDistribution<T>? other)
		{
			if (other is null)
				return false;
			if (ReferenceEquals(this, other))
				return true;
			return this.Probability == other.Probability && this.RandomSeed == other.RandomSeed;
		}

		/// <summary>
		/// Check whether the given <paramref name="obj"/> represents the same distribution as this one
		/// </summary>
		/// <param name="obj">The other <see cref="object"/> to compare</param>
		/// <returns>this == <paramref name="obj"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override bool Equals(object? obj)
		{
			return obj is not null && this.Equals(obj as BernoulliBasedDistribution<T>) && obj.GetType() == this.GetType();
		}

		/// <summary>
		/// Get the hash code of this <see cref="BernoulliBasedDistribution{T}"/>
		/// </summary>
		/// <returns>The hash code of this <see cref="BernoulliBasedDistribution{T}"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode()
		{
			return HashCode.Combine(this.Probability, this.RandomSeed);
		}

		/// <summary>
		/// Get the string representation of the property-value pairs of this <see cref="BernoulliBasedDistribution{T}"/>
		/// </summary>
		protected override string PropertiesString => base.PropertiesString + $", {nameof(this.Probability)}={this.Probability}";
	}
}
