using System;
using System.Runtime.CompilerServices;

using Althea.NativeTypes;
using Althea.Random;


namespace Althea.Backend.Random
{
	// Ignore Spelling: \dfrac \ln \det \lt \alpha' \mbox \dbinom \binom
	/// <summary>
	/// The class for a one-dimensional normal distribution of type <typeparamref name="T"/>, implements <see cref="OneDimensionalFloatTypedDistribution{T}"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged floating point type</typeparam>
	//tex:Normal distribution PDF: $$P_{\mu,\sigma}(x)=\frac{1}{\sigma\sqrt{2\pi}}\exp{\left( -\frac{(x-\mu)^2}{2\sigma^2} \right)}$$
	public class NormalDistribution<T> : OneDimensionalFloatTypedDistribution<T>, IEquatable<NormalDistribution<T>> where T : unmanaged, INumber<T>
	{
		/// <summary>
		/// Get the mean value of this normal distribution
		/// </summary>
		public T Mean {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
		}

		/// <summary>
		/// Get the standard deviation value of this normal distribution
		/// </summary>
		public T StandardDeviation {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
		}

		/// <summary>
		/// Create a standard normal distribution with mean = 0 and standard deviation = 1, and random seed is not set
		/// </summary>
		public NormalDistribution() : base(null)
		{
			this.Mean = Const<T>.Zero; this.StandardDeviation = Const<T>.One;
		}

		/// <summary>
		/// Create a normal distribution with given <paramref name="mean"/>, <paramref name="stddev"/> and the random <paramref name="seed"/>
		/// </summary>
		/// <param name="mean">The given mean value</param>
		/// <param name="stddev">The given standard deviation</param>
		/// <param name="seed">The given random seed, default null means has no preferred random seed</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="stddev"/> is not larger than 0</exception>
		public NormalDistribution(T mean, T stddev, long? seed = null) : base(seed)
		{
			if (stddev.NativeLessThanOrEqual(default))
				throw new ArgumentOutOfRangeException(nameof(stddev), stddev, Resources.Parameter.MustPositive);
			this.StandardDeviation = stddev; this.Mean = mean;
		}

		/// <summary>
		/// Check whether the <paramref name="other"/> <see cref="NormalDistribution{T}"/> represents the same distribution as this one
		/// </summary>
		/// <param name="other">The other <see cref="NormalDistribution{T}"/> to compare</param>
		/// <returns>this == <paramref name="other"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(NormalDistribution<T>? other)
		{
			if (other is null)
				return false;
			if (ReferenceEquals(this, other))
				return true;
			return this.RandomSeed == other.RandomSeed && this.Mean.IsEqual(other.Mean) && this.StandardDeviation.IsEqual(other.StandardDeviation);
		}

		/// <summary>
		/// Check whether the given <paramref name="obj"/> represents the same distribution as this one
		/// </summary>
		/// <param name="obj">The other <see cref="object"/> to compare</param>
		/// <returns>this == <paramref name="obj"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override bool Equals(object? obj)
		{
			return this.Equals(obj as NormalDistribution<T>);
		}

		/// <summary>
		/// Get the hash code of this <see cref="NormalDistribution{T}"/>
		/// </summary>
		/// <returns>The hash code of this <see cref="NormalDistribution{T}"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode()
		{
			return HashCode.Combine(this.Mean, this.StandardDeviation, this.RandomSeed);
		}

		/// <summary>
		/// Get the string representation of the property-value pairs of this <see cref="NormalDistribution{T}"/>
		/// </summary>
		protected override string PropertiesString => base.PropertiesString + $", {nameof(this.Mean)}={this.Mean}, {nameof(this.StandardDeviation)}={this.StandardDeviation}";
	}

	/// <summary>
	/// The class for a one-dimensional normal distribution of type <typeparamref name="T"/>, implements <see cref="OneDimensionalFloatTypedDistribution{T}"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged floating point type</typeparam>
	//tex:$\chi^2$ distribution PDF: $$P_{v}(x)=\begin{cases}\dfrac{x^{(v-2)/2}e^{-x/2}}{2^{v/2}\Gamma(v/2)} & x \ge 0 \\ 0 & x \lt 0 \end{cases}$$
	public class ChiSquareDistribution<T> : OneDimensionalFloatTypedDistribution<T>, IEquatable<ChiSquareDistribution<T>> where T : unmanaged, INumber<T>
	{
		/// <summary>
		/// Get the degree of freedom of this χ² distribution
		/// </summary>
		public int DegreeOfFreedom {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
		}

		/// <summary>
		/// Create a <see cref="ChiSquareDistribution{T}"/> distribution with given <paramref name="DoF"/> and the random <paramref name="seed"/>
		/// </summary>
		/// <param name="DoF">The given degree of freedom</param>
		/// <param name="seed">The given random seed, default null means has no preferred random seed</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="DoF"/> is less than 1</exception>
		public ChiSquareDistribution(int DoF, long? seed = null) : base(seed)
		{
			if (DoF <= 0)
				throw new ArgumentOutOfRangeException(nameof(DoF), DoF, Resources.Parameter.MustPositive);
			this.DegreeOfFreedom = DoF;
		}

		/// <summary>
		/// Check whether the <paramref name="other"/> <see cref="ChiSquareDistribution{T}"/> represents the same distribution as this one
		/// </summary>
		/// <param name="other">The other <see cref="ChiSquareDistribution{T}"/> to compare</param>
		/// <returns>this == <paramref name="other"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(ChiSquareDistribution<T>? other)
		{
			if (other is null)
				return false;
			if (ReferenceEquals(this, other))
				return true;
			return this.RandomSeed == other.RandomSeed && this.DegreeOfFreedom == other.DegreeOfFreedom;
		}

		/// <summary>
		/// Check whether the given <paramref name="obj"/> represents the same distribution as this one
		/// </summary>
		/// <param name="obj">The other <see cref="object"/> to compare</param>
		/// <returns>this == <paramref name="obj"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override bool Equals(object? obj)
		{
			return this.Equals(obj as ChiSquareDistribution<T>);
		}

		/// <summary>
		/// Get the hash code of this <see cref="ChiSquareDistribution{T}"/>
		/// </summary>
		/// <returns>The hash code of this <see cref="ChiSquareDistribution{T}"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode()
		{
			return HashCode.Combine(this.DegreeOfFreedom, this.RandomSeed);
		}

		/// <summary>
		/// Get the string representation of the property-value pairs of this <see cref="ChiSquareDistribution{T}"/>
		/// </summary>
		protected override string PropertiesString => base.PropertiesString + $", {nameof(this.DegreeOfFreedom)}={this.DegreeOfFreedom}";
	}

	/// <summary>
	/// The class for a one-dimensional normal distribution of type <typeparamref name="T"/>, implements <see cref="DisplaceScaleDistribution{T}"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged floating point type</typeparam>
	//tex:Log normal distribution PDF: $$P_{\mu,\sigma,b,\beta}(x)=\frac{1}{\sigma(x-b)\sqrt{2\pi}}\exp{\left[ -\frac{\left( \ln{\frac{x-b}{\beta}} - \mu \right)^2}{2\sigma^2} \right]}$$
	public class LogNormalDistribution<T> : DisplaceScaleDistribution<T>, IEquatable<LogNormalDistribution<T>> where T : unmanaged, INumber<T>
	{
		/// <summary>
		/// Get the mean value (<c>μ</c>) of this log normal distribution's subject normal distribution
		/// </summary>
		public T Mean {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
		}

		/// <summary>
		/// Get the standard deviation (<c>σ</c>) value of this log normal distribution's subject normal distribution
		/// </summary>
		public T StandardDeviation {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
		}

		/// <summary>
		/// Create a standard normal distribution with μ=0, σ=1, b=0, β=1, and random seed is not set
		/// </summary>
		public LogNormalDistribution() : base()
		{
			this.Mean = Const<T>.Zero; this.StandardDeviation = Const<T>.One;
		}

		/// <summary>
		/// Create a log normal distribution with given <paramref name="mean"/>, <paramref name="stddev"/>, <paramref name="displacement"/>, <paramref name="scaleFactor"/> and the random <paramref name="seed"/>
		/// </summary>
		/// <param name="mean">The given mean value μ</param>
		/// <param name="stddev">The given standard deviation σ</param>
		/// <param name="displacement">The given displacement b</param>
		/// <param name="scaleFactor">The given scale factor β</param>
		/// <param name="seed">The given random seed, default null means has no preferred random seed</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="stddev"/> is not larger than 0</exception>
		public LogNormalDistribution(T mean, T stddev, T displacement, T scaleFactor, long? seed = null) : base(displacement, scaleFactor, seed)
		{
			if (stddev.NativeLessThanOrEqual(default))
				throw new ArgumentOutOfRangeException(nameof(stddev), stddev, Resources.Parameter.MustPositive);
			this.Mean = mean; this.StandardDeviation = stddev;
		}

		/// <summary>
		/// Check whether the <paramref name="other"/> <see cref="LogNormalDistribution{T}"/> represents the same distribution as this one
		/// </summary>
		/// <param name="other">The other <see cref="LogNormalDistribution{T}"/> to compare</param>
		/// <returns>this == <paramref name="other"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(LogNormalDistribution<T>? other)
		{
			if (other is null)
				return false;
			if (ReferenceEquals(this, other))
				return true;
			return this.Mean.IsEqual(other.Mean) && this.StandardDeviation.IsEqual(other.StandardDeviation) && base.Equals(other);
		}

		/// <summary>
		/// Check whether the given <paramref name="obj"/> represents the same distribution as this one
		/// </summary>
		/// <param name="obj">The other <see cref="object"/> to compare</param>
		/// <returns>this == <paramref name="obj"/></returns>
		public override bool Equals(object? obj)
		{
			return this.Equals(obj as LogNormalDistribution<T>);
		}

		/// <summary>
		/// Get the hash code of this <see cref="LogNormalDistribution{T}"/>
		/// </summary>
		/// <returns>The hash code of this <see cref="LogNormalDistribution{T}"/></returns>
		public override int GetHashCode()
		{
			return HashCode.Combine(this.Mean, this.StandardDeviation, base.GetHashCode());
		}

		/// <summary>
		/// Get the string representation of the property-value pairs of this <see cref="LogNormalDistribution{T}"/>
		/// </summary>
		protected override string PropertiesString => base.PropertiesString + $", {nameof(this.Mean)}={this.Mean}, {nameof(this.StandardDeviation)}={this.StandardDeviation}";
	}

	/// <summary>
	/// The class for a one-dimensional exponential distribution of type <typeparamref name="T"/>, implements <see cref="DisplaceScaleDistribution{T}"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged floating point type</typeparam>
	//tex:Exponential distribution PDF: $$P_{a,\beta}(x) =
	//\begin{cases} \dfrac{1}{\beta}\exp{\left( -\dfrac{x-a}{\beta} \right)} & x \ge a \\
	//0 & x \lt a \end{cases}$$
	public class ExponentialDistribution<T> : DisplaceScaleDistribution<T> where T : unmanaged, INumber<T>
	{
		/// <summary>
		/// Create a standard <see cref="ExponentialDistribution{T}"/> with a=0, β=1, and random seed is not set
		/// </summary>
		public ExponentialDistribution() : base() { }

		/// <summary>
		/// Create an <see cref="ExponentialDistribution{T}"/> with given <paramref name="displacement"/>, <paramref name="scaleFactor"/> and the random <paramref name="seed"/>
		/// </summary>
		/// <param name="displacement">The given displacement a</param>
		/// <param name="scaleFactor">The given scale factor β</param>
		/// <param name="seed">The given random seed, default null means has no preferred random seed</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="scaleFactor"/> is not larger than 0</exception>
		public ExponentialDistribution(T displacement, T scaleFactor, long? seed = null) : base(displacement, scaleFactor, seed) { }
	}

	/// <summary>
	/// The class for a one-dimensional Laplace distribution of type <typeparamref name="T"/>, implements <see cref="DisplaceScaleDistribution{T}"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged floating point type</typeparam>
	//tex:Laplace distribution PDF: $$P_{a,\beta}(x) = \frac{1}{\sqrt{2\beta}} \exp{\left( -\frac{|x-a|}{\beta} \right)}$$
	public class LaplaceDistribution<T> : DisplaceScaleDistribution<T> where T : unmanaged, INumber<T>
	{
		/// <summary>
		/// Create a standard <see cref="LaplaceDistribution{T}"/> with a=0, β=1, and random seed is not set
		/// </summary>
		public LaplaceDistribution() : base() { }

		/// <summary>
		/// Create an <see cref="LaplaceDistribution{T}"/> with given <paramref name="displacement"/>, <paramref name="scaleFactor"/> and the random <paramref name="seed"/>
		/// </summary>
		/// <param name="displacement">The given displacement a</param>
		/// <param name="scaleFactor">The given scale factor β</param>
		/// <param name="seed">The given random seed, default null means has no preferred random seed</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="scaleFactor"/> is not larger than 0</exception>
		public LaplaceDistribution(T displacement, T scaleFactor, long? seed = null) : base(displacement, scaleFactor, seed) { }
	}

	/// <summary>
	/// The class for a one-dimensional Weibull distribution of type <typeparamref name="T"/>, implements <see cref="ShapeDisplaceScaleDistribution{T}"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged floating point type</typeparam>
	//tex:Weibull distribution PDF: $$P_{a,\alpha,\beta}(x) =
	//\begin{cases} \dfrac{\alpha}{\beta^\alpha} (x-a)^{\alpha-1} \exp{\left[ -\left(\dfrac{x-a}{\beta}\right)^\alpha \right]} & x \ge a \\
	//0 & x \lt a\end{cases}$$
	public class WeibullDistribution<T> : ShapeDisplaceScaleDistribution<T> where T : unmanaged, INumber<T>
	{
		/// <summary>
		/// Create a standard <see cref="WeibullDistribution{T}"/> with a=0,α=1, β=1, and random seed is not set
		/// </summary>
		public WeibullDistribution() : base() { }

		/// <summary>
		/// Create an <see cref="WeibullDistribution{T}"/> with given <paramref name="displacement"/>, <paramref name="scaleFactor"/> and the random <paramref name="seed"/>
		/// </summary>
		/// <param name="displacement">The given displacement a</param>
		/// <param name="shapeFactor">The given scale factor α</param>
		/// <param name="scaleFactor">The given scale factor β</param>
		/// <param name="seed">The given random seed, default null means has no preferred random seed</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="scaleFactor"/> or <paramref name="shapeFactor"/> is not larger than 0</exception>
		public WeibullDistribution(T displacement, T shapeFactor, T scaleFactor, long? seed = null) : base(displacement, shapeFactor, scaleFactor, seed) { }
	}

	/// <summary>
	/// The class for a one-dimensional Cauchy distribution of type <typeparamref name="T"/>, implements <see cref="DisplaceScaleDistribution{T}"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged floating point type</typeparam>
	//tex:Cauchy distribution PDF: $$P_{a,\beta}(x) = \frac{1}{\pi\beta\left[ 1 + \left( \frac{x-a}{\beta} \right)^2 \right]}$$
	public class CauchyDistribution<T> : DisplaceScaleDistribution<T> where T : unmanaged, INumber<T>
	{
		/// <summary>
		/// Create a standard <see cref="CauchyDistribution{T}"/> with a=0, β=1, and random seed is not set
		/// </summary>
		public CauchyDistribution() : base() { }

		/// <summary>
		/// Create an <see cref="CauchyDistribution{T}"/> with given <paramref name="displacement"/>, <paramref name="scaleFactor"/> and the random <paramref name="seed"/>
		/// </summary>
		/// <param name="displacement">The given displacement a</param>
		/// <param name="scaleFactor">The given scale factor β</param>
		/// <param name="seed">The given random seed, default null means has no preferred random seed</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="scaleFactor"/> is not larger than 0</exception>
		public CauchyDistribution(T displacement, T scaleFactor, long? seed = null) : base(displacement, scaleFactor, seed) { }
	}

	/// <summary>
	/// The class for a one-dimensional Rayleigh distribution of type <typeparamref name="T"/>, implements <see cref="DisplaceScaleDistribution{T}"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged floating point type</typeparam>
	//tex:Rayleigh distribution PDF: $$P_{a,\beta}(x) = \frac{2(x-a)}{\beta^2}\exp{\left[ - \left( \frac{x-a}{\beta} \right)^2 \right]}$$
	public class RayleighDistribution<T> : DisplaceScaleDistribution<T> where T : unmanaged, INumber<T>
	{
		/// <summary>
		/// Create a standard <see cref="RayleighDistribution{T}"/> with a=0, β=1, and random seed is not set
		/// </summary>
		public RayleighDistribution() : base() { }

		/// <summary>
		/// Create an <see cref="RayleighDistribution{T}"/> with given <paramref name="displacement"/>, <paramref name="scaleFactor"/> and the random <paramref name="seed"/>
		/// </summary>
		/// <param name="displacement">The given displacement a</param>
		/// <param name="scaleFactor">The given scale factor β</param>
		/// <param name="seed">The given random seed, default null means has no preferred random seed</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="scaleFactor"/> is not larger than 0</exception>
		public RayleighDistribution(T displacement, T scaleFactor, long? seed = null) : base(displacement, scaleFactor, seed) { }
	}

	/// <summary>
	/// The class for a one-dimensional Gumbel distribution of type <typeparamref name="T"/>, implements <see cref="DisplaceScaleDistribution{T}"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged floating point type</typeparam>
	//tex:Gumbel distribution PDF: $$P_{a,\beta}(x) = \frac{1}{\beta} \exp{\left( \frac{x-a}{\beta} \right)} \cdot
	//\exp{\left[ -\exp{\left( \frac{x-a}{\beta} \right)} \right]}$$
	public class GumbelDistribution<T> : DisplaceScaleDistribution<T> where T : unmanaged, INumber<T>
	{
		/// <summary>
		/// Create a standard <see cref="GumbelDistribution{T}"/> with a=0, β=1, and random seed is not set
		/// </summary>
		public GumbelDistribution() : base() { }

		/// <summary>
		/// Create an <see cref="GumbelDistribution{T}"/> with given <paramref name="displacement"/>, <paramref name="scaleFactor"/> and the random <paramref name="seed"/>
		/// </summary>
		/// <param name="displacement">The given displacement a</param>
		/// <param name="scaleFactor">The given scale factor β</param>
		/// <param name="seed">The given random seed, default null means has no preferred random seed</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="scaleFactor"/> is not larger than 0</exception>
		public GumbelDistribution(T displacement, T scaleFactor, long? seed = null) : base(displacement, scaleFactor, seed) { }
	}

	/// <summary>
	/// The class for a one-dimensional gamma distribution of type <typeparamref name="T"/>, implements <see cref="ShapeDisplaceScaleDistribution{T}"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged floating point type</typeparam>
	//tex:Gamma distribution PDF: $$P_{a,\alpha,\beta}(x) = \begin{cases}
	//\dfrac{1}{\Gamma(\alpha)\beta^\alpha} (x-a)^{\alpha-1} \exp{\left( -\dfrac{x-a}{\beta} \right)} & x \ge a \\
	//0 & x \lt a \end{cases}$$
	//where $\Gamma(a)$ is the complete gamma function.
	public class GammaDistribution<T> : ShapeDisplaceScaleDistribution<T> where T : unmanaged, INumber<T>
	{
		/// <summary>
		/// Create a standard <see cref="GammaDistribution{T}"/> with a=0,α=1, β=1, and random seed is not set
		/// </summary>
		public GammaDistribution() : base() { }

		/// <summary>
		/// Create an <see cref="GammaDistribution{T}"/> with given <paramref name="displacement"/>, <paramref name="scaleFactor"/> and the random <paramref name="seed"/>
		/// </summary>
		/// <param name="displacement">The given displacement a</param>
		/// <param name="shapeFactor">The given scale factor α</param>
		/// <param name="scaleFactor">The given scale factor β</param>
		/// <param name="seed">The given random seed, default null means has no preferred random seed</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="scaleFactor"/> or <paramref name="shapeFactor"/> is not larger than 0</exception>
		public GammaDistribution(T displacement, T shapeFactor, T scaleFactor, long? seed = null) : base(displacement, shapeFactor, scaleFactor, seed) { }
	}

	/// <summary>
	/// The class for a one-dimensional beta distribution of type <typeparamref name="T"/>, implements <see cref="ShapeDisplaceScaleDistribution{T}"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged floating point type</typeparam>
	//tex:Beta distribution PDF: $$P_{a,\alpha,\alpha',\beta}(x) = \begin{cases}
	//\dfrac{(x-a)^{\alpha-1} (\beta+a-x)^{\alpha'-1}}{B(\alpha,\alpha')\beta^{\alpha+\alpha'-1}} & a \le x \lt a+\beta \\
	//0 & x \lt a \mbox{ or } x \ge a+\beta \end{cases}$$
	//where $B(p,q)$ is the complete beta function.
	public class BetaDistribution<T> : ShapeDisplaceScaleDistribution<T>, IEquatable<BetaDistribution<T>> where T : unmanaged, INumber<T>
	{
		/// <summary>
		/// Get the second shaping factor (<c>α'</c>) of this <see cref="BetaDistribution{T}"/>
		/// </summary>
		public T ShapeFactorOther {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
		}

		/// <summary>
		/// Create a standard <see cref="BetaDistribution{T}"/> with a=0, α=1, α'=1, β=1, and random seed is not set
		/// </summary>
		public BetaDistribution() : base()
		{
			this.ShapeFactorOther = Const<T>.One;
		}

		/// <summary>
		/// Create an <see cref="BetaDistribution{T}"/> with given <paramref name="displacement"/>, <paramref name="shapeFactor1"/>, <paramref name="shapeFactor2"/>, <paramref name="scaleFactor"/> and the random <paramref name="seed"/>
		/// </summary>
		/// <param name="displacement">The given displacement a</param>
		/// <param name="shapeFactor1">The given first shape factor α</param>
		/// <param name="shapeFactor2">The given second shape factor α'</param>
		/// <param name="scaleFactor">The given scale factor β</param>
		/// <param name="seed">The given random seed, default null means has no preferred random seed</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="scaleFactor"/> or <paramref name="shapeFactor1"/> or <paramref name="shapeFactor2"/> is not larger than 0</exception>
		public BetaDistribution(T displacement, T shapeFactor1, T shapeFactor2, T scaleFactor, long? seed = null) : base(displacement, shapeFactor1, scaleFactor, seed)
		{
			if (shapeFactor2.NativeLessThanOrEqual(default))
				throw new ArgumentOutOfRangeException(nameof(shapeFactor2), shapeFactor2, Resources.Parameter.MustPositive);
			this.ShapeFactorOther = shapeFactor2;
		}

		/// <summary>
		/// Check whether the <paramref name="other"/> <see cref="BetaDistribution{T}"/> represents the same distribution as this one
		/// </summary>
		/// <param name="other">The other <see cref="BetaDistribution{T}"/> to compare</param>
		/// <returns>this == <paramref name="other"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(BetaDistribution<T>? other)
		{
			if (other is null)
				return false;
			if (ReferenceEquals(this, other))
				return true;
			return this.ShapeFactorOther.IsEqual(other.ShapeFactorOther) && base.Equals(other);
		}

		/// <summary>
		/// Check whether the given <paramref name="obj"/> represents the same distribution as this one
		/// </summary>
		/// <param name="obj">The other <see cref="object"/> to compare</param>
		/// <returns>this == <paramref name="obj"/></returns>
		public override bool Equals(object? obj)
		{
			return this.Equals(obj as BetaDistribution<T>);
		}

		/// <summary>
		/// Get the hash code of this <see cref="BetaDistribution{T}"/>
		/// </summary>
		/// <returns>The hash code of this <see cref="BetaDistribution{T}"/></returns>
		public override int GetHashCode()
		{
			return HashCode.Combine(this.ShapeFactorOther, base.GetHashCode());
		}

		/// <summary>
		/// Get the string representation of the property-value pairs of this <see cref="BetaDistribution{T}"/>
		/// </summary>
		protected override string PropertiesString => base.PropertiesString + $", {nameof(this.ShapeFactorOther)}={this.ShapeFactorOther}";
	}
}
