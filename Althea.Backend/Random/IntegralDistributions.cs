using System;
using System.Runtime.CompilerServices;

using Althea.NativeTypes;
using Althea.Random;


namespace Althea.Backend.Random
{
	// Ignore Spelling: \dfrac \ln \det \lt \alpha' \mbox \dbinom \binom
	/// <summary>
	/// The class for a one-dimensional Poisson distribution of type <typeparamref name="T"/>, implements <see cref="OneDimensionalIntegerTypedDistribution{T}"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged integral type</typeparam>
	//tex:Poisson distribution PDF: $$P_{\lambda}(k)=\frac{\lambda^k e^{-k}}{k!}$$
	public class PoissonDistribution<T> : OneDimensionalIntegerTypedDistribution<T>, IEquatable<PoissonDistribution<T>> where T : unmanaged
	{
		/// <summary>
		/// Get the λ value of this Poisson distribution
		/// </summary>
		public double Lambda {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
		}

		/// <summary>
		/// Create a Poisson distribution with given <paramref name="lambda"/> and the random <paramref name="seed"/>
		/// </summary>
		/// <param name="lambda">The given λ value</param>
		/// <param name="seed">The given random seed, default null means has no preferred random seed</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="lambda"/> is not larger than 0</exception>
		public PoissonDistribution(double lambda, long? seed = null) : base(seed)
		{
			if (lambda <= 0)
				throw new ArgumentOutOfRangeException(nameof(lambda), lambda, Resources.Parameter.MustPositive);
			this.Lambda = lambda;
		}

		/// <summary>
		/// Check whether the <paramref name="other"/> <see cref="PoissonDistribution{T}"/> represents the same distribution as this one
		/// </summary>
		/// <param name="other">The other <see cref="PoissonDistribution{T}"/> to compare</param>
		/// <returns>this == <paramref name="other"/></returns>
		public bool Equals(PoissonDistribution<T>? other)
		{
			if (other is null)
				return false;
			if (ReferenceEquals(this, other))
				return true;
			return this.Lambda.IsEqual(other.Lambda) && this.RandomSeed == other.RandomSeed;
		}

		/// <summary>
		/// Check whether the given <paramref name="obj"/> represents the same distribution as this one
		/// </summary>
		/// <param name="obj">The other <see cref="object"/> to compare</param>
		/// <returns>this == <paramref name="obj"/></returns>
		public override bool Equals(object? obj)
		{
			return this.Equals(obj as PoissonDistribution<T>);
		}

		/// <summary>
		/// Get the hash code of this <see cref="PoissonDistribution{T}"/>
		/// </summary>
		/// <returns>The hash code of this <see cref="PoissonDistribution{T}"/></returns>
		public override int GetHashCode()
		{
			return HashCode.Combine(this.Lambda, this.RandomSeed);
		}

		/// <summary>
		/// Get the string representation of the property-value pairs of this <see cref="PoissonDistribution{T}"/>
		/// </summary>
		protected override string PropertiesString => base.PropertiesString + $", {nameof(this.Lambda)}={this.Lambda}";
	}

	/// <summary>
	/// The class for one-dimensional Bernoulli distribution, implements <see cref="BernoulliBasedDistribution{T}"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged integral type</typeparam>
	public class BernoulliDistribution<T> : BernoulliBasedDistribution<T> where T : unmanaged
	{
		/// <summary>
		/// Create a <see cref="BernoulliDistribution{T}"/> with given <paramref name="p"/> and the random <paramref name="seed"/>
		/// </summary>
		/// <param name="p">The given probability of the trial success</param>
		/// <param name="seed">The given random seed, default null means has no preferred random seed</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="p"/> is not in range (0.0, 1.0)</exception>
		public BernoulliDistribution(double p, long? seed = null) : base(p, seed) { }
	}

	/// <summary>
	/// The class for one-dimensional geometric distribution, implements <see cref="BernoulliBasedDistribution{T}"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged integral type</typeparam>
	//tex:Geometric distribution PDF: $P_p(k)=p(1-p)^k$
	public class GeometricDistribution<T> : BernoulliBasedDistribution<T> where T : unmanaged
	{
		/// <summary>
		/// Create a <see cref="GeometricDistribution{T}"/> with given <paramref name="p"/> and the random <paramref name="seed"/>
		/// </summary>
		/// <param name="p">The given probability of the trial success</param>
		/// <param name="seed">The given random seed, default null means has no preferred random seed</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="p"/> is not in range (0.0, 1.0)</exception>
		public GeometricDistribution(double p, long? seed = null) : base(p, seed) { }
	}

	/// <summary>
	/// The class for one-dimensional binomial distribution, implements <see cref="BernoulliBasedDistribution{T}"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged integral type</typeparam>
	//tex:Binomial distribution PDF: $$P_{p,m}(k)=\binom{k}{m}p^k(1-p)^{m-k}$$
	public class BinomialDistribution<T> : BernoulliBasedDistribution<T>, IEquatable<BinomialDistribution<T>> where T : unmanaged
	{
		/// <summary>
		/// Get the number of trials of this <see cref="BinomialDistribution{T}"/>
		/// </summary>
		public int NTrials {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
		}

		/// <summary>
		/// Create a <see cref="BinomialDistribution{T}"/> with given <paramref name="p"/> and the random <paramref name="seed"/>
		/// </summary>
		/// <param name="p">The given probability of the trial success</param>
		/// <param name="nTrials">The given total number of Bernoulli trials</param>
		/// <param name="seed">The given random seed, default null means has no preferred random seed</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="p"/> is not in range (0.0, 1.0); or <paramref name="nTrials"/> is less than 2</exception>
		public BinomialDistribution(double p, int nTrials, long? seed = null) : base(p, seed)
		{
			if (nTrials <= 1)
				throw new ArgumentOutOfRangeException(nameof(nTrials), nTrials, Resources.Parameter.InvalidValue);
			this.NTrials = nTrials;
		}

		/// <summary>
		/// Check whether the <paramref name="other"/> <see cref="BinomialDistribution{T}"/> represents the same distribution as this one
		/// </summary>
		/// <param name="other">The other <see cref="BinomialDistribution{T}"/> to compare</param>
		/// <returns>this == <paramref name="other"/></returns>
		public bool Equals(BinomialDistribution<T>? other)
		{
			if (other is null)
				return false;
			if (ReferenceEquals(this, other))
				return true;
			return this.NTrials == other.NTrials && base.Equals(other);
		}

		/// <summary>
		/// Check whether the given <paramref name="obj"/> represents the same distribution as this one
		/// </summary>
		/// <param name="obj">The other <see cref="object"/> to compare</param>
		/// <returns>this == <paramref name="obj"/></returns>
		public override bool Equals(object? obj)
		{
			return this.Equals(obj as BinomialDistribution<T>);
		}

		/// <summary>
		/// Get the hash code of this <see cref="BinomialDistribution{T}"/>
		/// </summary>
		/// <returns>The hash code of this <see cref="BinomialDistribution{T}"/></returns>
		public override int GetHashCode()
		{
			return HashCode.Combine(this.NTrials, base.GetHashCode());
		}

		/// <summary>
		/// Get the string representation of the property-value pairs of this <see cref="BinomialDistribution{T}"/>
		/// </summary>
		protected override string PropertiesString => base.PropertiesString + $", {nameof(this.NTrials)}={this.NTrials}";
	}

	/// <summary>
	/// The class for one-dimensional negative binomial distribution, implements <see cref="BernoulliBasedDistribution{T}"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged integral type</typeparam>
	//tex:Negative binomial distribution PDF: $$P_{r,p}(k)=\frac{\Gamma(r+k)}{k!\Gamma(r)}p^{r}(1-p)^{k}$$
	public class NegativeBinomialDistribution<T> : BernoulliBasedDistribution<T>, IEquatable<NegativeBinomialDistribution<T>> where T : unmanaged
	{
		/// <summary>
		/// Get the number of success Bernoulli trials of this <see cref="NegativeBinomialDistribution{T}"/>
		/// </summary>
		public double SuccessCount {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
		}

		/// <summary>
		/// Create a <see cref="NegativeBinomialDistribution{T}"/> with given <paramref name="p"/> and the random <paramref name="seed"/>
		/// </summary>
		/// <param name="p">The given probability of the trial success</param>
		/// <param name="nSuccess">The given number of success Bernoulli trials</param>
		/// <param name="seed">The given random seed, default null means has no preferred random seed</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="p"/> is not in range (0.0, 1.0); or <paramref name="nSuccess"/> is not larger than 0</exception>
		public NegativeBinomialDistribution(double p, double nSuccess, long? seed = null) : base(p, seed)
		{
			if (nSuccess <= 0)
				throw new ArgumentOutOfRangeException(nameof(nSuccess), nSuccess, Resources.Parameter.MustPositive);
			this.SuccessCount = nSuccess;
		}

		/// <summary>
		/// Check whether the <paramref name="other"/> <see cref="NegativeBinomialDistribution{T}"/> represents the same distribution as this one
		/// </summary>
		/// <param name="other">The other <see cref="NegativeBinomialDistribution{T}"/> to compare</param>
		/// <returns>this == <paramref name="other"/></returns>
		public bool Equals(NegativeBinomialDistribution<T>? other)
		{
			if (other is null)
				return false;
			if (ReferenceEquals(this, other))
				return true;
			return this.SuccessCount == other.SuccessCount && base.Equals(other);
		}

		/// <summary>
		/// Check whether the given <paramref name="obj"/> represents the same distribution as this one
		/// </summary>
		/// <param name="obj">The other <see cref="object"/> to compare</param>
		/// <returns>this == <paramref name="obj"/></returns>
		public override bool Equals(object? obj)
		{
			return this.Equals(obj as NegativeBinomialDistribution<T>);
		}

		/// <summary>
		/// Get the hash code of this <see cref="NegativeBinomialDistribution{T}"/>
		/// </summary>
		/// <returns>The hash code of this <see cref="NegativeBinomialDistribution{T}"/></returns>
		public override int GetHashCode()
		{
			return HashCode.Combine(this.SuccessCount, base.GetHashCode());
		}

		/// <summary>
		/// Get the string representation of the property-value pairs of this <see cref="NegativeBinomialDistribution{T}"/>
		/// </summary>
		protected override string PropertiesString => base.PropertiesString + $", {nameof(this.SuccessCount)}={this.SuccessCount}";
	}

	/// <summary>
	/// The class for a one-dimensional hyper-geometric distribution of type <typeparamref name="T"/>, implements <see cref="OneDimensionalIntegerTypedDistribution{T}"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged integral type</typeparam>
	//tex:Hyper-geometric distribution PDF: $$P_{l,s,m}(k)=\frac{\binom{k}{m}\binom{s-k}{l-m}}{\binom{s}{l}}$$
	public class HypergeometricDistribution<T> : OneDimensionalIntegerTypedDistribution<T>, IEquatable<HypergeometricDistribution<T>> where T : unmanaged
	{
		/// <summary>
		/// Get the lost size (<c>l</c>) of this <see cref="HypergeometricDistribution{T}"/>
		/// </summary>
		public int LostSize {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
		}

		/// <summary>
		/// Get the sample size (<c>s</c>) of this <see cref="HypergeometricDistribution{T}"/>
		/// </summary>
		public int SampleSize {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
		}

		/// <summary>
		/// Get the marked elements size (<c>m</c>) of this <see cref="HypergeometricDistribution{T}"/>
		/// </summary>
		public int MarkSize {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
		}

		/// <summary>
		/// Create a <see cref="HypergeometricDistribution{T}"/> with given <paramref name="l"/>, <paramref name="s"/>, <paramref name="m"/> and the random <paramref name="seed"/>
		/// </summary>
		/// <param name="l">The given lot size</param>
		/// <param name="s">The given sample size</param>
		/// <param name="m">The given marked elements size</param>
		/// <param name="seed">The given random seed, default null means has no preferred random seed</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="l"/> or <paramref name="s"/> or <paramref name="m"/> is not larger than 0; or <c><paramref name="l"/> &lt; max(<paramref name="s"/>,<paramref name="m"/>)</c></exception>
		public HypergeometricDistribution(int l, int s, int m, long? seed = null) : base(seed)
		{
			if (l <= 0)
				throw new ArgumentOutOfRangeException(nameof(l), l, Resources.Parameter.MustPositive);
			if (s <= 0)
				throw new ArgumentOutOfRangeException(nameof(s), s, Resources.Parameter.MustPositive);
			if (m <= 0)
				throw new ArgumentOutOfRangeException(nameof(m), m, Resources.Parameter.MustPositive);
			if (l < Math.Max(s, m))
				throw new ArgumentOutOfRangeException(nameof(l), l, Resources.Parameter.MustPositive);
			this.LostSize = l; this.SampleSize = s; this.MarkSize = m;
		}

		/// <summary>
		/// Check whether the <paramref name="other"/> <see cref="HypergeometricDistribution{T}"/> represents the same distribution as this one
		/// </summary>
		/// <param name="other">The other <see cref="HypergeometricDistribution{T}"/> to compare</param>
		/// <returns>this == <paramref name="other"/></returns>
		public bool Equals(HypergeometricDistribution<T>? other)
		{
			if (other is null)
				return false;
			if (ReferenceEquals(this, other))
				return true;
			return this.LostSize == other.LostSize && this.SampleSize == other.SampleSize && this.MarkSize == other.MarkSize && this.RandomSeed == other.RandomSeed;
		}

		/// <summary>
		/// Check whether the given <paramref name="obj"/> represents the same distribution as this one
		/// </summary>
		/// <param name="obj">The other <see cref="object"/> to compare</param>
		/// <returns>this == <paramref name="obj"/></returns>
		public override bool Equals(object? obj)
		{
			return this.Equals(obj as HypergeometricDistribution<T>);
		}

		/// <summary>
		/// Get the hash code of this <see cref="HypergeometricDistribution{T}"/>
		/// </summary>
		/// <returns>The hash code of this <see cref="HypergeometricDistribution{T}"/></returns>
		public override int GetHashCode()
		{
			return HashCode.Combine(this.LostSize, this.SampleSize, this.MarkSize, this.RandomSeed);
		}

		/// <summary>
		/// Get the string representation of the property-value pairs of this <see cref="HypergeometricDistribution{T}"/>
		/// </summary>
		protected override string PropertiesString => base.PropertiesString + $", {nameof(this.LostSize)}={this.LostSize}, {nameof(this.SampleSize)}={this.SampleSize}, {nameof(this.MarkSize)}={this.MarkSize}";
	}
}
