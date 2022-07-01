namespace Althea.Random
{
	// Ignore Spelling: \dfrac \ln \det \lt \alpha \mbox \dbinom \binom
	/// <summary>
	/// The struct for a one-dimensional Poisson distribution of type <typeparamref name="T"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged integral type</typeparam>
	/// <param name="Lambda">The λ value of Poisson distribution</param>
	/// <param name="RandomSeed"><inheritdoc/></param>
	//tex:Poisson distribution PDF: $$P_{\lambda}(k)=\frac{\lambda^k e^{-k}}{k!}$$
	public readonly record struct PoissonDistribution<T>(decimal Lambda, long? RandomSeed = null) :
		IIntegralDistribution<T, PoissonDistribution<T>>, IRank1Distribution<T, PoissonDistribution<T>>
		where T : unmanaged, IBaseNumber<T>
	{
		/// <summary>
		/// Create a standard Poisson distribution with λ = 1
		/// </summary>
		public PoissonDistribution(long? seed = null) : this(1, seed) { }

		bool ICheckValid.IsValid() => ((IIntegralDistribution<T, PoissonDistribution<T>>)this).IsValid() && this.Lambda > 0;

		static DataType IRandomDistribution<PoissonDistribution<T>>.DataTypeAt(int rank) => IRank1Distribution<T, PoissonDistribution<T>>.DataTypeAt(rank);
	}

	/// <summary>
	/// The struct for one-dimensional Bernoulli distribution, implements <see cref="IBernoulliBasedDistribution{T, TSelf}"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged integral type</typeparam>
	/// <param name="Probability">The probability of a Bernoulli trial succeeding (p)</param>
	/// <param name="RandomSeed"><inheritdoc/></param>
	public readonly record struct BernoulliDistribution<T>(decimal Probability, long? RandomSeed = null) :
		IBernoulliBasedDistribution<T, BernoulliDistribution<T>>
		where T : unmanaged, IBaseNumber<T>
	{
		/// <summary>
		/// Create a standard Bernoulli distribution with p = 0.5
		/// </summary>
		public BernoulliDistribution(long? seed = null) : this(0.5m, seed) { }

		static DataType IRandomDistribution<BernoulliDistribution<T>>.DataTypeAt(int rank) => IRank1Distribution<T, BernoulliDistribution<T>>.DataTypeAt(rank);
	}

	/// <summary>
	/// The struct for one-dimensional geometric distribution, implements <see cref="IBernoulliBasedDistribution{T, TSelf}"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged integral type</typeparam>
	//tex:Geometric distribution PDF: $P_p(k)=p(1-p)^k$
	public readonly record struct GeometricDistribution<T>(decimal Probability, long? RandomSeed = null) :
		IBernoulliBasedDistribution<T, GeometricDistribution<T>>
		where T : unmanaged, IBaseNumber<T>
	{
		/// <summary>
		/// Create a standard geometric distribution with p = 0.5
		/// </summary>
		public GeometricDistribution(long? seed = null) : this(0.5m, seed) { }

		static DataType IRandomDistribution<GeometricDistribution<T>>.DataTypeAt(int rank) => IRank1Distribution<T, GeometricDistribution<T>>.DataTypeAt(rank);
	}

	/// <summary>
	/// The struct for one-dimensional binomial distribution, implements <see cref="IBernoulliBasedDistribution{T, TSelf}"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged integral type</typeparam>
	/// <param name="Probability">The probability of a Bernoulli trial succeeding (p)</param>
	/// <param name="NTrials">The number of independent Bernoulli trials (m)</param>
	/// <param name="RandomSeed"><inheritdoc/></param>
	//tex:Binomial distribution PDF: $$P_{p,m}(k)=\binom{k}{m}p^k(1-p)^{m-k}$$
	public readonly record struct BinomialDistribution<T>(decimal Probability, int NTrials, long? RandomSeed = null) :
		IBernoulliBasedDistribution<T, BinomialDistribution<T>>
		where T : unmanaged, IBaseNumber<T>
	{
		/// <summary>
		/// Create a standard binomial distribution with p = 0.5 and m = 1
		/// </summary>
		public BinomialDistribution(long? seed = null) : this(0.5m, 2, seed) { }

		bool ICheckValid.IsValid() => ((IBernoulliBasedDistribution<T, BinomialDistribution<T>>)this).IsValid() && this.NTrials > 1;

		static DataType IRandomDistribution<BinomialDistribution<T>>.DataTypeAt(int rank) => IRank1Distribution<T, BinomialDistribution<T>>.DataTypeAt(rank);
	}

	/// <summary>
	/// The struct for one-dimensional negative binomial distribution, implements <see cref="IBernoulliBasedDistribution{T, TSelf}"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged integral type</typeparam>
	/// <param name="Probability">The probability of a Bernoulli trial succeeding (p)</param>
	/// <param name="SuccessCount">The number of success Bernoulli trials (r)</param>
	/// <param name="RandomSeed"><inheritdoc/></param>
	//tex:Negative binomial distribution PDF: $$P_{r,p}(k)=\frac{\Gamma(r+k)}{k!\Gamma(r)}p^{r}(1-p)^{k}$$
	public readonly record struct NegativeBinomialDistribution<T>(decimal Probability, int SuccessCount, long? RandomSeed = null) :
		IBernoulliBasedDistribution<T, NegativeBinomialDistribution<T>>
		where T : unmanaged, IBaseNumber<T>
	{
		/// <summary>
		/// Create a standard negative binomial distribution with p = 0.5 and r = 1
		/// </summary>
		public NegativeBinomialDistribution(long? seed = null) : this(0.5m, 1, seed) { }

		bool ICheckValid.IsValid() => ((IBernoulliBasedDistribution<T, NegativeBinomialDistribution<T>>)this).IsValid() && this.SuccessCount > 0;

		static DataType IRandomDistribution<NegativeBinomialDistribution<T>>.DataTypeAt(int rank) => IRank1Distribution<T, NegativeBinomialDistribution<T>>.DataTypeAt(rank);
	}

	/// <summary>
	/// The struct for a one-dimensional hyper-geometric distribution of type <typeparamref name="T"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged integral type</typeparam>
	/// <param name="TotalSize">The total size of the hyper-geometric distribution (s)</param>
	/// <param name="MarkSize">The size of success trials in <paramref name="TotalSize"/> (m)</param>
	/// <param name="SampleSize">The size of the samples to be taken in <paramref name="TotalSize"/> (l)</param>
	/// <param name="RandomSeed"><inheritdoc/></param>
	//tex:Hyper-geometric distribution PDF: $$P_{l,s,m}(k)=\frac{\binom{k}{m}\binom{s-k}{l-m}}{\binom{s}{l}}$$
	public readonly record struct HypergeometricDistribution<T>(int TotalSize, int MarkSize, int SampleSize, long? RandomSeed = null) :
		IIntegralDistribution<T, HypergeometricDistribution<T>>, IRank1Distribution<T, HypergeometricDistribution<T>>
		where T : unmanaged, IBaseNumber<T>
	{
		/// <summary>
		/// Create a standard hyper-geometric distribution with s = 2 and m = l = 1
		/// </summary>
		public HypergeometricDistribution(long? seed = null) : this(2, 1, 1, seed) { }

		bool ICheckValid.IsValid() => ((IIntegralDistribution<T, HypergeometricDistribution<T>>)this).IsValid() && this.TotalSize > 0 && this.MarkSize > 0 && this.SampleSize > 0 && this.MarkSize <= this.TotalSize && this.SampleSize <= this.TotalSize;

		static DataType IRandomDistribution<HypergeometricDistribution<T>>.DataTypeAt(int rank) => IRank1Distribution<T, HypergeometricDistribution<T>>.DataTypeAt(rank);
	}
}
