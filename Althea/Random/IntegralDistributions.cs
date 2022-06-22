namespace Althea.Random
{
	// Ignore Spelling: \dfrac \ln \det \lt \alpha \mbox \dbinom \binom
	/// <summary>
	/// The class for a one-dimensional Poisson distribution of type <typeparamref name="T"/>, implements <see cref="Rank1RandomDistribution{T}"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged integral type</typeparam>
	/// <param name="Lambda">The λ value of Poisson distribution</param>
	/// <param name="RandomSeed"><inheritdoc/></param>
	//tex:Poisson distribution PDF: $$P_{\lambda}(k)=\frac{\lambda^k e^{-k}}{k!}$$
	public sealed record PoissonDistribution<T>(decimal Lambda, long? RandomSeed) : Rank1RandomDistribution<T>(RandomSeed) where T : unmanaged, INumber<T>
	{
		/// <inheritdoc/>

		public override bool IsValid() => BernoulliBasedDistribution<T>.TypeValid() && this.Lambda > 0;
	}

	/// <summary>
	/// The class for one-dimensional Bernoulli distribution, implements <see cref="BernoulliBasedDistribution{T}"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged integral type</typeparam>
	public sealed record BernoulliDistribution<T> : BernoulliBasedDistribution<T> where T : unmanaged, INumber<T>
	{
	}

	/// <summary>
	/// The class for one-dimensional geometric distribution, implements <see cref="BernoulliBasedDistribution{T}"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged integral type</typeparam>
	//tex:Geometric distribution PDF: $P_p(k)=p(1-p)^k$
	public sealed record GeometricDistribution<T> : BernoulliBasedDistribution<T> where T : unmanaged, INumber<T>
	{
	}

	/// <summary>
	/// The class for one-dimensional binomial distribution, implements <see cref="BernoulliBasedDistribution{T}"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged integral type</typeparam>
	/// <param name="Probability"><inheritdoc/></param>
	/// <param name="RandomSeed"><inheritdoc/></param>
	/// <param name="NTrials">The number of independent Bernoulli trials (m)</param>
	//tex:Binomial distribution PDF: $$P_{p,m}(k)=\binom{k}{m}p^k(1-p)^{m-k}$$
	public sealed record BinomialDistribution<T>(decimal Probability, int NTrials, long? RandomSeed) : BernoulliBasedDistribution<T>(Probability, RandomSeed) where T : unmanaged, INumber<T>
	{
		/// <inheritdoc/>
		public override bool IsValid() => base.IsValid() && this.NTrials > 1;
	}

	/// <summary>
	/// The class for one-dimensional negative binomial distribution, implements <see cref="BernoulliBasedDistribution{T}"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged integral type</typeparam>
	/// <param name="Probability"><inheritdoc/></param>
	/// <param name="RandomSeed"><inheritdoc/></param>
	/// <param name="SuccessCount">The number of success Bernoulli trials (r)</param>
	//tex:Negative binomial distribution PDF: $$P_{r,p}(k)=\frac{\Gamma(r+k)}{k!\Gamma(r)}p^{r}(1-p)^{k}$$
	public sealed record NegativeBinomialDistribution<T>(decimal Probability, int SuccessCount, long? RandomSeed) : BernoulliBasedDistribution<T>(Probability, RandomSeed) where T : unmanaged, INumber<T>
	{
		/// <inheritdoc/>
		public override bool IsValid() => base.IsValid() && this.SuccessCount >= 1;
	}

	/// <summary>
	/// The class for a one-dimensional hyper-geometric distribution of type <typeparamref name="T"/>, implements <see cref="Rank1RandomDistribution{T}"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged integral type</typeparam>
	/// <param name="TotalSize">The total size of the hyper-geometric distribution (s)</param>
	/// <param name="MarkSize">The size of success trials in <paramref name="TotalSize"/> (m)</param>
	/// <param name="SampleSize">The size of the samples to be taken in <paramref name="TotalSize"/> (l)</param>
	/// <param name="RandomSeed"><inheritdoc/></param>
	//tex:Hyper-geometric distribution PDF: $$P_{l,s,m}(k)=\frac{\binom{k}{m}\binom{s-k}{l-m}}{\binom{s}{l}}$$
	public sealed record HypergeometricDistribution<T>(int TotalSize, int MarkSize, int SampleSize, long? RandomSeed) : Rank1RandomDistribution<T>(RandomSeed) where T : unmanaged, INumber<T>
	{
		/// <inheritdoc/>
		public override bool IsValid() => BernoulliBasedDistribution<T>.TypeValid() && this.TotalSize > 0 && this.MarkSize > 0 && this.SampleSize > 0 && this.MarkSize < this.TotalSize && this.SampleSize < this.TotalSize;
	}
}
