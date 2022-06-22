namespace Althea.Random
{

	// Ignore Spelling: \dfrac \ln \det \lt \alpha' \mbox \dbinom \binom \frac \right \begin \ge \le \cdot erfc
	/// <summary>
	/// The class for one-dimensional normal (Gaussian) distributions.
	/// </summary>
	/// <typeparam name="T">Any unmanaged floating point type</typeparam>
	/// <param name="Mean">The mean value of this normal distribution (μ)</param>
	/// <param name="StandardDeviation">The standard deviation of this normal distribution (σ)</param>
	/// <param name="RandomSeed"><inheritdoc/></param>
	//tex:Normal distribution PDF: $$P_{\mu,\sigma}(x)=\frac{1}{\sigma\sqrt{2\pi}}\exp{\left( -\frac{(x-\mu)^2}{2\sigma^2} \right)}$$
	public sealed record NormalDistribution<T>(T Mean, T StandardDeviation, long? RandomSeed) : DisplaceScaleDistribution<T>(Mean, StandardDeviation, RandomSeed) where T : unmanaged, INumber<T>
	{
		/// <summary>
		/// Create a standard normal distribution with μ=0 and σ=1
		/// </summary>
		public NormalDistribution(long? seed = null) : this(T.Zero, T.One, seed) { }
	}

	/// <summary>
	/// The class for a one-dimensional normal distribution of type <typeparamref name="T"/>, implements <see cref="Rank1RandomDistribution{T}"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged floating point type</typeparam>
	//tex:$\chi^2$ distribution PDF: $$P_{v}(x)=\begin{cases}\dfrac{x^{(v-2)/2}e^{-x/2}}{2^{v/2}\Gamma(v/2)} & x \ge 0 \\ 0 & x \lt 0 \end{cases}$$
	public sealed record ChiSquareDistribution<T> : DegreeOfFreedomDistribution<T> where T : unmanaged, INumber<T>
	{
	}

	/// <summary>
	/// The class for a one-dimensional normal distribution of type <typeparamref name="T"/>, implements <see cref="DisplaceScaleDistribution{T}"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged floating point type</typeparam>
	/// <param name="Mean">The mean value of log normal distribution's subject normal distribution (μ)</param>
	/// <param name="StandardDeviation">The standard deviation of log normal distribution's subject normal distribution (σ)</param>
	/// <param name="RandomSeed"><inheritdoc/></param>
	//tex:Log normal distribution PDF: $$P_{\mu,\sigma}(x)=\frac{1}{\sqrt{2\pi} \sigma x}\exp{\left[ -\frac{\left( \ln{x} - \mu \right)^2}{2\sigma^2} \right]}$$
	public sealed record LogNormalDistribution<T>(T Mean, T StandardDeviation, long? RandomSeed) : DisplaceScaleDistribution<T>(Mean, StandardDeviation, RandomSeed) where T : unmanaged, INumber<T>
	{
		/// <summary>
		/// Create a standard log normal distribution with μ=0 and σ=1
		/// </summary>
		public LogNormalDistribution(long? seed = null) : this(T.Zero, T.One, seed) { }
	}

	/// <summary>
	/// The class for a one-dimensional normal distribution of type <typeparamref name="T"/>, implements <see cref="DisplaceScaleDistribution{T}"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged floating point type</typeparam>
	/// <param name="Mean">The mean value of skew normal distribution's subject normal distribution (μ)</param>
	/// <param name="StandardDeviation">The standard deviation of skew normal distribution's subject normal distribution (σ)</param>
	/// <param name="Skewness">The skewness factor of skew normal distribution (δ)</param>
	/// <param name="RandomSeed"><inheritdoc/></param>
	//tex:Log normal distribution PDF: $$P_{\mu,\sigma,\delta}(x) = \frac{1}{\sqrt{2 \pi } \sigma} \exp\left[-\frac{(x-\mu )^2}{2 \sigma ^2}\right] \text{erfc} \left[ \frac{\delta (x-\mu )}{\sqrt{2} \sigma } \right]$$
	public sealed record SkewNormalDistribution<T>(T Mean, T StandardDeviation, T Skewness, long? RandomSeed) : ShapeDisplaceScaleDistribution<T>(Mean, StandardDeviation, Skewness, RandomSeed) where T : unmanaged, INumber<T>
	{
		/// <summary>
		/// Create a standard log normal distribution with μ=0, σ=1 and δ=0
		/// </summary>
		public SkewNormalDistribution(long? seed = null) : this(T.Zero, T.One, T.Zero, seed) { }
	}

	/// <summary>
	/// The class for a one-dimensional exponential distribution of type <typeparamref name="T"/>, implements <see cref="DisplaceScaleDistribution{T}"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged floating point type</typeparam>
	//tex:Exponential distribution PDF: $$P_{\mu,\beta}(x) =
	//\begin{cases} \dfrac{1}{\beta}\exp{\left( -\dfrac{x-\mu}{\beta} \right)} & x \ge \mu \\
	//0 & x \lt \mu \end{cases}$$
	public sealed record ExponentialDistribution<T> : DisplaceScaleDistribution<T> where T : unmanaged, INumber<T>
	{
	}

	/// <summary>
	/// The class for a one-dimensional Laplace distribution of type <typeparamref name="T"/>, implements <see cref="DisplaceScaleDistribution{T}"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged floating point type</typeparam>
	//tex:Laplace distribution PDF: $$P_{\mu,\beta}(x) = \frac{1}{\sqrt{2\beta}} \exp{\left( -\frac{|x-\mu|}{\beta} \right)}$$
	public sealed record LaplaceDistribution<T> : DisplaceScaleDistribution<T> where T : unmanaged, INumber<T>
	{
	}

	/// <summary>
	/// The class for a one-dimensional Weibull distribution of type <typeparamref name="T"/>, implements <see cref="ShapeDisplaceScaleDistribution{T}"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged floating point type</typeparam>
	//tex:Weibull distribution PDF: $$P_{\mu,\alpha,\beta}(x) =
	//\begin{cases} \dfrac{\alpha}{\beta^\alpha} (x-\mu)^{\alpha-1} \exp{\left[ -\left(\dfrac{x-\mu}{\beta}\right)^\alpha \right]} & x \ge a \\
	//0 & x \lt a\end{cases}$$
	public sealed record WeibullDistribution<T> : ShapeDisplaceScaleDistribution<T> where T : unmanaged, INumber<T>
	{
	}

	/// <summary>
	/// The class for a one-dimensional Cauchy distribution of type <typeparamref name="T"/>, implements <see cref="DisplaceScaleDistribution{T}"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged floating point type</typeparam>
	//tex:Cauchy distribution PDF: $$P_{\mu,\beta}(x) = \frac{1}{\pi\beta\left[ 1 + \left( \frac{x-\mu}{\beta} \right)^2 \right]}$$
	public sealed record CauchyDistribution<T> : DisplaceScaleDistribution<T> where T : unmanaged, INumber<T>
	{
	}

	/// <summary>
	/// The class for a one-dimensional Rayleigh distribution of type <typeparamref name="T"/>, implements <see cref="DisplaceScaleDistribution{T}"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged floating point type</typeparam>
	//tex:Rayleigh distribution PDF: $$P_{\mu,\beta}(x) = \frac{2(x-\mu)}{\beta^2}\exp{\left[ - \left( \frac{x-\mu}{\beta} \right)^2 \right]}$$
	public sealed record RayleighDistribution<T> : DisplaceScaleDistribution<T> where T : unmanaged, INumber<T>
	{
	}

	/// <summary>
	/// The class for a one-dimensional Gumbel distribution of type <typeparamref name="T"/>, implements <see cref="DisplaceScaleDistribution{T}"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged floating point type</typeparam>
	//tex:Gumbel distribution PDF: $$P_{\mu,\beta}(x) = \frac{1}{\beta} \exp{\left( \frac{x-\mu}{\beta} \right)} \cdot
	//\exp{\left[ -\exp{\left( \frac{x-\mu}{\beta} \right)} \right]}$$
	public sealed record GumbelDistribution<T> : DisplaceScaleDistribution<T> where T : unmanaged, INumber<T>
	{
	}

	/// <summary>
	/// The class for a one-dimensional gamma distribution of type <typeparamref name="T"/>, implements <see cref="ShapeDisplaceScaleDistribution{T}"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged floating point type</typeparam>
	//tex:Gamma distribution PDF: $$P_{\mu,\alpha,\beta}(x) = \begin{cases}
	//\dfrac{1}{\Gamma(\alpha)\beta^\alpha} (x-\mu)^{\alpha-1} \exp{\left( -\dfrac{x-\mu}{\beta} \right)} & x \ge \mu \\
	//0 & x \lt \mu \end{cases}$$
	//where $\Gamma(a)$ is the complete gamma function.
	public sealed record GammaDistribution<T> : ShapeDisplaceScaleDistribution<T> where T : unmanaged, INumber<T>
	{
	}

	/// <summary>
	/// The class for a one-dimensional beta distribution of type <typeparamref name="T"/>, implements <see cref="ShapeDisplaceScaleDistribution{T}"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged floating point type</typeparam>
	/// <param name="Displacement">The displacement factor (μ)</param>
	/// <param name="ScaleFactor">The scaling factor (β)</param>
	/// <param name="ShapeFactor1">The first shaping factor (α)</param>
	/// <param name="ShapeFactor2">The second shaping factor (α')</param>
	/// <param name="RandomSeed"><inheritdoc/></param>
	//tex:Beta distribution PDF: $$P_{\mu,\alpha,\alpha',\beta}(x) = \begin{cases}
	//\dfrac{(x-\mu)^{\alpha-1} (\beta+\mu-x)^{\alpha'-1}}{B(\alpha,\alpha')\beta^{\alpha+\alpha'-1}} & \mu \le x \lt \mu+\beta \\
	//0 & x \lt \mu \mbox{ or } x \ge \mu+\beta \end{cases}$$
	//where $B(p,q)$ is the complete beta function.
	public sealed record BetaDistribution<T>(T Displacement, T ScaleFactor, T ShapeFactor1, T ShapeFactor2, long? RandomSeed) : ShapeDisplaceScaleDistribution<T>(Displacement, ScaleFactor, ShapeFactor1, RandomSeed) where T : unmanaged, INumber<T>
	{
		/// <summary>
		/// Create a standard <see cref="BetaDistribution{T}"/> with μ=0, α=1, α'=1, β=1
		/// </summary>
		public BetaDistribution(long? seed = null) : this(T.Zero, T.One, T.One, T.One, seed) { }

		/// <inheritdoc/>
		public override bool IsValid() => this.ScaleFactor > T.Zero && this.ShapeFactor1 > T.Zero && this.ShapeFactor2 > T.Zero;
	}
}
