namespace Althea.Random;

// Ignore Spelling: \dfrac \ln \det \lt \alpha' \mbox \dbinom \binom \frac \right \begin \ge \le \cdot erfc
/// <summary>
/// The struct for one-dimensional normal (Gaussian) distributions, implements <see cref="IDisplaceScaleDistribution{T, TSelf}"/>
/// </summary>
/// <typeparam name="T">Any unmanaged floating point type</typeparam>
/// <param name="Displacement">The mean value of this normal distribution (μ)</param>
/// <param name="ScaleFactor">The standard deviation of this normal distribution (σ)</param>
/// <param name="RandomSeed"><inheritdoc/></param>
//tex:Normal distribution PDF: $$P_{\mu,\sigma}(x)=\frac{1}{\sigma\sqrt{2\pi}}\exp{\left( -\frac{(x-\mu)^2}{2\sigma^2} \right)}$$
public readonly record struct NormalDistribution<T>(T Displacement, T ScaleFactor, long? RandomSeed = null) :
	IDisplaceScaleDistribution<T, NormalDistribution<T>>
	where T : unmanaged, IBaseNumber<T>
{
	/// <summary>
	/// Create a standard normal distribution with μ = 0 and σ = 1
	/// </summary>
	public NormalDistribution(long? seed = null) : this(T.Zero, T.One, seed) { }

	static DataType IRandomDistribution<NormalDistribution<T>>.DataTypeAt(int rank) => IRank1Distribution<T, NormalDistribution<T>>.DataTypeAt(rank);
}

/// <summary>
/// The struct for a one-dimensional normal distribution of type <typeparamref name="T"/>, implements <see cref="IDegreeOfFreedomDistribution{T, TSelf}"/>
/// </summary>
/// <typeparam name="T">Any unmanaged floating point type</typeparam>
/// <param name="DegreeOfFreedom">The degree of freedom of χ² distribution</param>
/// <param name="RandomSeed"><inheritdoc/></param>
//tex:$\chi^2$ distribution PDF: $$P_{v}(x)=\begin{cases}\dfrac{x^{(v-2)/2}e^{-x/2}}{2^{v/2}\Gamma(v/2)} & x \ge 0 \\ 0 & x \lt 0 \end{cases}$$
public readonly record struct ChiSquareDistribution<T>(int DegreeOfFreedom, long? RandomSeed = null) :
	IDegreeOfFreedomDistribution<T, ChiSquareDistribution<T>>
	where T : unmanaged, IBaseNumber<T>
{
	/// <summary>
	/// Create a standard χ² distribution with DoF = 1
	/// </summary>
	public ChiSquareDistribution(long? seed = null) : this(1, seed) { }

	static DataType IRandomDistribution<ChiSquareDistribution<T>>.DataTypeAt(int rank) => IRank1Distribution<T, ChiSquareDistribution<T>>.DataTypeAt(rank);
}

/// <summary>
/// The struct for a one-dimensional normal distribution of type <typeparamref name="T"/>, implements <see cref="IDisplaceScaleDistribution{T, TSelf}"/>
/// </summary>
/// <typeparam name="T">Any unmanaged floating point type</typeparam>
/// <param name="Displacement">The mean value of the subjected normal distribution (μ)</param>
/// <param name="ScaleFactor">The standard deviation of the subjected normal distribution (σ)</param>
/// <param name="RandomSeed"><inheritdoc/></param>
//tex:Log normal distribution PDF: $$P_{\mu,\sigma}(x)=\frac{1}{\sqrt{2\pi} \sigma x}\exp{\left[ -\frac{\left( \ln{x} - \mu \right)^2}{2\sigma^2} \right]}$$
public readonly record struct LogNormalDistribution<T>(T Displacement, T ScaleFactor, long? RandomSeed = null) :
	IDisplaceScaleDistribution<T, LogNormalDistribution<T>>
	where T : unmanaged, IBaseNumber<T>
{
	/// <summary>
	/// Create a standard log normal distribution with μ = 0 and σ = 1
	/// </summary>
	public LogNormalDistribution(long? seed = null) : this(T.Zero, T.One, seed) { }

	static DataType IRandomDistribution<LogNormalDistribution<T>>.DataTypeAt(int rank) => IRank1Distribution<T, LogNormalDistribution<T>>.DataTypeAt(rank);
}

/// <summary>
/// The struct for a one-dimensional normal distribution of type <typeparamref name="T"/>, implements <see cref="IDisplaceScaleShapeDistribution{T, TSelf}"/>
/// </summary>
/// <typeparam name="T">Any unmanaged floating point type</typeparam>
/// <param name="Displacement">The mean value of the subjected normal distribution (μ)</param>
/// <param name="ScaleFactor">The standard deviation of the subjected normal distribution (σ)</param>
/// <param name="ShapeFactor">The skewness factor of skew normal distribution (δ)</param>
/// <param name="RandomSeed"><inheritdoc/></param>
//tex:Log normal distribution PDF: $$P_{\mu,\sigma,\delta}(x) = \frac{1}{\sqrt{2 \pi } \sigma} \exp\left[-\frac{(x-\mu )^2}{2 \sigma ^2}\right] \text{erfc} \left[ \frac{\delta (x-\mu )}{\sqrt{2} \sigma } \right]$$
public readonly record struct SkewNormalDistribution<T>(T Displacement, T ScaleFactor, T ShapeFactor, long? RandomSeed = null) :
	IDisplaceScaleShapeDistribution<T, SkewNormalDistribution<T>>
	where T : unmanaged, IBaseNumber<T>
{
	/// <summary>
	/// Create a standard log normal distribution with μ = 0, σ = 1 and δ = 0
	/// </summary>
	public SkewNormalDistribution(long? seed = null) : this(T.Zero, T.One, T.Zero, seed) { }

	static DataType IRandomDistribution<SkewNormalDistribution<T>>.DataTypeAt(int rank) => IRank1Distribution<T, SkewNormalDistribution<T>>.DataTypeAt(rank);
}

/// <summary>
/// The struct for a one-dimensional exponential distribution of type <typeparamref name="T"/>, implements <see cref="IDisplaceScaleDistribution{T, TSelf}"/>
/// </summary>
/// <typeparam name="T">Any unmanaged floating point type</typeparam>
/// <param name="Displacement">The starting offset value of this exponential distribution (μ)</param>
/// <param name="ScaleFactor">The scale factor of this normal distribution (β)</param>
/// <param name="RandomSeed"><inheritdoc/></param>
//tex:Exponential distribution PDF: $$P_{\mu,\beta}(x) =
//\begin{cases} \dfrac{1}{\beta}\exp{\left( -\dfrac{x-\mu}{\beta} \right)} & x \ge \mu \\
//0 & x \lt \mu \end{cases}$$
public readonly record struct ExponentialDistribution<T>(T Displacement, T ScaleFactor, long? RandomSeed = null) :
	IDisplaceScaleDistribution<T, ExponentialDistribution<T>>
	where T : unmanaged, IBaseNumber<T>
{
	/// <summary>
	/// Create a standard exponential distribution with μ = 0 and β = 1
	/// </summary>
	public ExponentialDistribution(long? seed = null) : this(T.Zero, T.One, seed) { }

	static DataType IRandomDistribution<ExponentialDistribution<T>>.DataTypeAt(int rank) => IRank1Distribution<T, ExponentialDistribution<T>>.DataTypeAt(rank);
}

/// <summary>
/// The struct for a one-dimensional Laplace distribution of type <typeparamref name="T"/>, implements <see cref="IDisplaceScaleDistribution{T, TSelf}"/>
/// </summary>
/// <typeparam name="T">Any unmanaged floating point type</typeparam>
/// <param name="Displacement">The mean value of this Laplace distribution (μ)</param>
/// <param name="ScaleFactor">The scale factor of this Laplace distribution (β)</param>
/// <param name="RandomSeed"><inheritdoc/></param>
//tex:Laplace distribution PDF: $$P_{\mu,\beta}(x) = \frac{1}{\sqrt{2\beta}} \exp{\left( -\frac{|x-\mu|}{\beta} \right)}$$
public readonly record struct LaplaceDistribution<T>(T Displacement, T ScaleFactor, long? RandomSeed = null) :
	IDisplaceScaleDistribution<T, LaplaceDistribution<T>>
	where T : unmanaged, IBaseNumber<T>
{
	/// <summary>
	/// Create a standard Laplace distribution with μ = 0 and β = 1
	/// </summary>
	public LaplaceDistribution(long? seed = null) : this(T.Zero, T.One, seed) { }

	static DataType IRandomDistribution<LaplaceDistribution<T>>.DataTypeAt(int rank) => IRank1Distribution<T, LaplaceDistribution<T>>.DataTypeAt(rank);
}

/// <summary>
/// The struct for a one-dimensional Weibull distribution of type <typeparamref name="T"/>, implements <see cref="IDisplaceScaleShapeDistribution{T, TSelf}"/>
/// </summary>
/// <typeparam name="T">Any unmanaged floating point type</typeparam>
/// <param name="Displacement">The starting offset of this Weibull distribution (μ)</param>
/// <param name="ScaleFactor">The scale factor of this Weibull distribution (β)</param>
/// <param name="ShapeFactor">The shape factor of this Weibull distribution (β)</param>
/// <param name="RandomSeed"><inheritdoc/></param>
//tex:Weibull distribution PDF: $$P_{\mu,\alpha,\beta}(x) =
//\begin{cases} \dfrac{\alpha}{\beta^\alpha} (x-\mu)^{\alpha-1} \exp{\left[ -\left(\dfrac{x-\mu}{\beta}\right)^\alpha \right]} & x \ge \mu \\
//0 & x \lt \mu\end{cases}$$
public readonly record struct WeibullDistribution<T>(T Displacement, T ScaleFactor, T ShapeFactor, long? RandomSeed = null) :
	IDisplaceScaleShapeDistribution<T, WeibullDistribution<T>>
	where T : unmanaged, IBaseNumber<T>
{
	/// <summary>
	/// Create a standard Laplace distribution with μ = 0, β = 1 and α = 1
	/// </summary>
	public WeibullDistribution(long? seed = null) : this(T.Zero, T.One, T.One, seed) { }

	static DataType IRandomDistribution<WeibullDistribution<T>>.DataTypeAt(int rank) => IRank1Distribution<T, WeibullDistribution<T>>.DataTypeAt(rank);
}

/// <summary>
/// The struct for a one-dimensional Cauchy distribution of type <typeparamref name="T"/>, implements <see cref="IDisplaceScaleDistribution{T, TSelf}"/>
/// </summary>
/// <typeparam name="T">Any unmanaged floating point type</typeparam>
/// <param name="Displacement">The mean value of this Cauchy distribution (μ)</param>
/// <param name="ScaleFactor">The scale factor of this Cauchy distribution (β)</param>
/// <param name="RandomSeed"><inheritdoc/></param>
//tex:Cauchy distribution PDF: $$P_{\mu,\beta}(x) = \frac{1}{\pi\beta\left[ 1 + \left( \frac{x-\mu}{\beta} \right)^2 \right]}$$
public readonly record struct CauchyDistribution<T>(T Displacement, T ScaleFactor, long? RandomSeed = null) :
	IDisplaceScaleDistribution<T, CauchyDistribution<T>>
	where T : unmanaged, IBaseNumber<T>
{
	/// <summary>
	/// Create a standard Cauchy distribution with μ = 0 and β = 1
	/// </summary>
	public CauchyDistribution(long? seed = null) : this(T.Zero, T.One, seed) { }

	static DataType IRandomDistribution<CauchyDistribution<T>>.DataTypeAt(int rank) => IRank1Distribution<T, CauchyDistribution<T>>.DataTypeAt(rank);
}

/// <summary>
/// The struct for a one-dimensional Rayleigh distribution of type <typeparamref name="T"/>, implements <see cref="IDisplaceScaleDistribution{T, TSelf}"/>
/// </summary>
/// <typeparam name="T">Any unmanaged floating point type</typeparam>
/// <param name="Displacement">The mean value of this Rayleigh distribution (μ)</param>
/// <param name="ScaleFactor">The scale factor of this Rayleigh distribution (β)</param>
/// <param name="RandomSeed"><inheritdoc/></param>
//tex:Rayleigh distribution PDF: $$P_{\mu,\beta}(x) = \frac{2(x-\mu)}{\beta^2}\exp{\left[ - \left( \frac{x-\mu}{\beta} \right)^2 \right]}$$
public readonly record struct RayleighDistribution<T>(T Displacement, T ScaleFactor, long? RandomSeed = null) :
	IDisplaceScaleDistribution<T, RayleighDistribution<T>>
	where T : unmanaged, IBaseNumber<T>
{
	/// <summary>
	/// Create a standard Cauchy distribution with μ = 0 and β = 1
	/// </summary>
	public RayleighDistribution(long? seed = null) : this(T.Zero, T.One, seed) { }

	static DataType IRandomDistribution<RayleighDistribution<T>>.DataTypeAt(int rank) => IRank1Distribution<T, RayleighDistribution<T>>.DataTypeAt(rank);
}

/// <summary>
/// The struct for a one-dimensional Gumbel distribution of type <typeparamref name="T"/>, implements <see cref="IDisplaceScaleDistribution{T, TSelf}"/>
/// </summary>
/// <typeparam name="T">Any unmanaged floating point type</typeparam>
/// <param name="Displacement">The offset value of this Rayleigh distribution (μ)</param>
/// <param name="ScaleFactor">The scale factor of this Rayleigh distribution (β)</param>
/// <param name="RandomSeed"><inheritdoc/></param>
//tex:Gumbel distribution PDF: $$P_{\mu,\beta}(x) = \frac{1}{\beta} \exp{\left( \frac{x-\mu}{\beta} \right)} \cdot
//\exp{\left[ -\exp{\left( \frac{x-\mu}{\beta} \right)} \right]}$$
public readonly record struct GumbelDistribution<T>(T Displacement, T ScaleFactor, long? RandomSeed = null) :
	IDisplaceScaleDistribution<T, GumbelDistribution<T>>
	where T : unmanaged, IBaseNumber<T>
{
	/// <summary>
	/// Create a standard Gumbel distribution with μ = 0 and β = 1
	/// </summary>
	public GumbelDistribution(long? seed = null) : this(T.Zero, T.One, seed) { }

	static DataType IRandomDistribution<GumbelDistribution<T>>.DataTypeAt(int rank) => IRank1Distribution<T, GumbelDistribution<T>>.DataTypeAt(rank);
}

/// <summary>
/// The struct for a one-dimensional gamma distribution of type <typeparamref name="T"/>, implements <see cref="IDisplaceScaleShapeDistribution{T, TSelf}"/>
/// </summary>
/// <typeparam name="T">Any unmanaged floating point type</typeparam>
/// <param name="Displacement">The starting offset of this Gamma distribution (μ)</param>
/// <param name="ScaleFactor">The scale factor of this Gamma distribution (β)</param>
/// <param name="ShapeFactor">The shape factor of this Gamma distribution (β)</param>
/// <param name="RandomSeed"><inheritdoc/></param>
//tex:Gamma distribution PDF: $$P_{\mu,\alpha,\beta}(x) = \begin{cases}
//\dfrac{1}{\Gamma(\alpha)\beta^\alpha} (x-\mu)^{\alpha-1} \exp{\left( -\dfrac{x-\mu}{\beta} \right)} & x \ge \mu \\
//0 & x \lt \mu \end{cases}$$
//where $\Gamma(a)$ is the complete gamma function.
public readonly record struct GammaDistribution<T>(T Displacement, T ScaleFactor, T ShapeFactor, long? RandomSeed = null) :
	IDisplaceScaleShapeDistribution<T, GammaDistribution<T>>
	where T : unmanaged, IBaseNumber<T>
{
	/// <summary>
	/// Create a standard Gamma distribution with μ = 0, β = 1 and α = 1
	/// </summary>
	public GammaDistribution(long? seed = null) : this(T.Zero, T.One, T.One, seed) { }

	static DataType IRandomDistribution<GammaDistribution<T>>.DataTypeAt(int rank) => IRank1Distribution<T, GammaDistribution<T>>.DataTypeAt(rank);
}

/// <summary>
/// The struct for a one-dimensional beta distribution of type <typeparamref name="T"/>, implements <see cref="IDisplaceScaleShapeDistribution{T, TSelf}"/>
/// </summary>
/// <typeparam name="T">Any unmanaged floating point type</typeparam>
/// <param name="Displacement">The displacement factor (μ)</param>
/// <param name="ScaleFactor">The scaling factor (β)</param>
/// <param name="ShapeFactor">The first shaping factor (α)</param>
/// <param name="ShapeFactorOther">The second shaping factor (α')</param>
/// <param name="RandomSeed"><inheritdoc/></param>
//tex:Beta distribution PDF: $$P_{\mu,\alpha,\alpha',\beta}(x) = \begin{cases}
//\dfrac{(x-\mu)^{\alpha-1} (\beta+\mu-x)^{\alpha'-1}}{B(\alpha,\alpha')\beta^{\alpha+\alpha'-1}} & \mu \le x \lt \mu+\beta \\
//0 & x \lt \mu \mbox{ or } x \ge \mu+\beta \end{cases}$$
//where $B(p,q)$ is the complete beta function.
public readonly record struct BetaDistribution<T>(T Displacement, T ScaleFactor, T ShapeFactor, T ShapeFactorOther, long? RandomSeed = null) :
	IDisplaceScaleShapeDistribution<T, BetaDistribution<T>>
	where T : unmanaged, IBaseNumber<T>
{
	/// <summary>
	/// Create a standard <see cref="BetaDistribution{T}"/> with μ = 0, α = 1, α' = 1 and β = 1
	/// </summary>
	public BetaDistribution(long? seed = null) : this(T.Zero, T.One, T.One, T.One, seed) { }

	bool ICheckValid.IsValid() => IDisplaceScaleDistribution<T, BetaDistribution<T>>.IsValid(this) && this.ShapeFactor > T.Zero && this.ShapeFactorOther > T.Zero;

	static DataType IRandomDistribution<BetaDistribution<T>>.DataTypeAt(int rank) => IRank1Distribution<T, BetaDistribution<T>>.DataTypeAt(rank);
}
