using System.Runtime.CompilerServices;

using Althea.Random;
using Althea.Backend.Random;
using Althea.Backend.Mkl.Random;


namespace Althea.Backend.Mkl.Random
{
	#region error
	/// <summary>
	/// The returned status of the MKL random number generator library APIs
	/// </summary>
	public enum MklRngStatus
	{
		/// <summary>
		/// No error.
		/// </summary>
		Success = 0,
		/// <summary>
		/// The feature invoked is not implemented.
		/// </summary>
		FeatureNotImplemented = -1,
		/// <summary>
		/// Unknown error.
		/// </summary>
		Unknown = -2,
		/// <summary>
		/// Input argument value is not valid.
		/// </summary>
		Badargs = -3,
		/// <summary>
		/// System cannot allocate memory.
		/// </summary>
		MemoryFailure = -4,
		/// <summary>
		/// Input pointer argument is NULL.
		/// </summary>
		NullPtr = -5,
		/// <summary>
		/// CPU version is not supported.
		/// </summary>
		CpuNotSupported = -6,
		/// <summary>
		/// BRNG index is not valid.
		/// </summary>
		InvalidBrngIndex = -1000,
		/// <summary>
		/// BRNG does not support Leapfrog method.
		/// </summary>
		LeapfrogUnsupported = -1002,
		/// <summary>
		/// BRNG does not support Skip-Ahead method.
		/// </summary>
		SkipaheadUnsupported = -1003,
		/// <summary>
		/// Two BRNGs are not compatible for the operation.
		/// </summary>
		BrngsIncompatible = -1005,
		/// <summary>
		/// The random stream is invalid.
		/// </summary>
		BadStream = -1006,
		/// <summary>
		/// Registration cannot be completed due to lack of free entries in the table of registered BRNGs.
		/// </summary>
		BrngTableFull = -1007,
		/// <summary>
		/// The value in 'StreamStateSize' field is bad.
		/// </summary>
		BadStreamStateSize = -1008,
		/// <summary>
		/// The value in 'WordSize' field is bad.
		/// </summary>
		BadWordSize = -1009,
		/// <summary>
		/// The value in 'NSeeds' field is bad.
		/// </summary>
		BadNSeeds = -1010,
		/// <summary>
		/// The value in 'NBits' field is bad.
		/// </summary>
		BadNBits = -1011,
		/// <summary>
		/// Period of the generator is exceeded.
		/// </summary>
		QrngPeriodElapsed = -1012,
		/// <summary>
		/// The number of streams of Leapfrog method is too large.
		/// </summary>
		LeapfrogNStreamsTooBig = -1013,
		/// <summary>
		/// BRNG is not supported by the function.
		/// </summary>
		BrngNotSupported = -1014,
		/// <summary>
		/// Callback function for an abstract BRNG returns an invalid number of updated entries in a buffer, that is, less than 0 or larger than 'NMax'.
		/// </summary>
		BadUpdate = -1120,
		/// <summary>
		/// Callback function for an abstract BRNG returns zero as the number of updated entries in a buffer.
		/// </summary>
		NoNumbers = -1121,
		/// <summary>
		/// The abstract random stream is invalid.
		/// </summary>
		InvalidAbstractStream = -1122,
		/// <summary>
		/// Non-deterministic stream not supported
		/// </summary>
		NondetermNotSupported = -1130,
		/// <summary>
		/// Non-deterministic stream too many entries
		/// </summary>
		NondetermNretriesExceeded = -1131,
		/// <summary>
		/// ARS5 stream related errors
		/// </summary>
		Ars5NotSupported = -1140,
		/// <summary>
		/// Error in closing the file.
		/// </summary>
		FileClose = -1100,
		/// <summary>
		/// Error in opening the file.
		/// </summary>
		FileOpen = -1101,
		/// <summary>
		/// Error in writing the file.
		/// </summary>
		FileWrite = -1102,
		/// <summary>
		/// Error in reading the file.
		/// </summary>
		FileRead = -1103,
		/// <summary>
		/// File format is unknown.
		/// </summary>
		BadFileFormat = -1110,
		/// <summary>
		/// File format version is not supported.
		/// </summary>
		UnsupportedFileVer = -1111,
		/// <summary>
		/// Descriptive random stream format is unknown.
		/// </summary>
		BadMemoryFormat = -1200,
	}
	#endregion

	#region other enum
	// Ignore Spelling: Wichmann Mersenne Gumbel Marsaglia Johnk

	/// <summary>
	/// The MKL random number generator type.
	/// </summary>
	public enum GeneratorType
	{
		/// <summary>
		/// A 31-bit multiplicative congruential generator.
		/// </summary>
		MCG31 = 1 << 20,
		/// <summary>
		/// A generalized feedback shift register generator.
		/// </summary>
		R250 = 2 << 20,
		/// <summary>
		/// A combined multiple recursive generator with two components of order 3.
		/// </summary>
		MRG32K3A = 3 << 20,
		/// <summary>
		/// A 59-bit multiplicative congruential generator.
		/// </summary>
		MCG59 = 4 << 20,
		/// <summary>
		/// A set of 273 Wichmann-Hill combined multiplicative congruential generators.
		/// </summary>
		WichmannHill = 5 << 20,
		/// <summary>
		/// A Mersenne Twister pseudo-random number generator.
		/// </summary>
		MT19937 = 8 << 20,
		/// <summary>
		/// A set of 6024 Mersenne Twister pseudo-random number generators.
		/// </summary>
		MT2203 = 9 << 20,
		/// <summary>
		/// A SIMD-oriented Fast Mersenne Twister pseudo-random number generator.
		/// </summary>
		SFMT19937 = 13 << 20,
		/// <summary>
		/// A 32-bit Gray code-based generator producing low-discrepancy sequences for dimensions 1 ≤ s ≤ 40; user-defined dimensions are also available.
		/// </summary>
		SOBOL = 6 << 20,
		/// <summary>
		/// A 32-bit Gray code-based generator producing low-discrepancy sequences for dimensions 1 ≤ s ≤ 318; user-defined dimensions are also available.
		/// </summary>
		NIEDERR = 7 << 20,
		/// <summary>
		/// An abstract random number generator for integer arrays.
		/// </summary>
		IntegerAbstract = 10 << 20,
		/// <summary>
		/// An abstract random number generator for double precision floating-point arrays.
		/// </summary>
		DoubleAbstract = 11 << 20,
		/// <summary>
		/// An abstract random number generator for single precision floating-point arrays.
		/// </summary>
		SingleAbstract = 12 << 20,
		/// <summary>
		/// A non-deterministic random number generator. May not be supported.
		/// </summary>
		NonDeterministic = 14 << 20,
		/// <summary>
		/// A Philox4x32-10 counter-based pseudo-random number generator.
		/// </summary>
		Philox4x32_10 = 16 << 20,
		/// <summary>
		/// An ARS-5 counter-based pseudo-random number generator that uses instructions from the AES-NI set. May not be supported.
		/// </summary>
		ARS5 = 15 << 20,
	}

	/// <summary>
	/// The MKL RNG matrix storage type
	/// </summary>
	internal enum MklRngMatrixStorage
	{
		/// <summary>
		/// The whole matrix is stored
		/// </summary>
		Full,
		/// <summary>
		/// Lower/higher triangular matrix is packed in 1-dimensional array
		/// </summary>
		Packed,
		/// <summary>
		/// Diagonal elements are packed in 1-dimensional array
		/// </summary>
		Diagonal
	}

	/// <summary>
	/// The distribution types supported by the MKL RNG
	/// </summary>
	internal enum DistributionType
	{
		/// <summary>
		/// <see cref="UniformDistribution{T}"/>
		/// </summary>
		Uniform,
		/// <summary>
		/// <see cref="RandomBitsDistribution{T}"/>
		/// </summary>
		RandomBits,
		/// <summary>
		/// <see cref="BernoulliDistribution{T}"/>
		/// </summary>
		Bernoulli,
		/// <summary>
		/// <see cref="BetaDistribution{T}"/>
		/// </summary>
		Beta,
		/// <summary>
		/// <see cref="BinomialDistribution{T}"/>
		/// </summary>
		Binomial,
		/// <summary>
		/// <see cref="CauchyDistribution{T}"/>
		/// </summary>
		Cauchy,
		/// <summary>
		/// <see cref="ChiSquareDistribution{T}"/>
		/// </summary>
		ChiSquare,
		/// <summary>
		/// <see cref="ExponentialDistribution{T}"/>
		/// </summary>
		Exponential,
		/// <summary>
		/// <see cref="GammaDistribution{T}"/>
		/// </summary>
		Gamma,
		/// <summary>
		/// <see cref="GeometricDistribution{T}"/>
		/// </summary>
		Geometric,
		/// <summary>
		/// <see cref="GumbelDistribution{T}"/>
		/// </summary>
		Gumbel,
		/// <summary>
		/// <see cref="HypergeometricDistribution{T}"/>
		/// </summary>
		Hypergeometric,
		/// <summary>
		/// <see cref="LaplaceDistribution{T}"/>
		/// </summary>
		Laplace,
		/// <summary>
		/// <see cref="LogNormalDistribution{T}"/>
		/// </summary>
		LogNormal,
		/// <summary>
		/// <see cref="MultinomialDistribution{T}"/>
		/// </summary>
		Multinomial,
		/// <summary>
		/// <see cref="MultiNormalDistribution{T}"/>
		/// </summary>
		MultiNormal,
		/// <summary>
		/// <see cref="NegativeBinomialDistribution{T}"/>
		/// </summary>
		NegativeBinomial,
		/// <summary>
		/// <see cref="NormalDistribution{T}"/>
		/// </summary>
		Normal,
		/// <summary>
		/// <see cref="PoissonDistribution{T}"/>
		/// </summary>
		Poisson,
		/// <summary>
		/// <see cref="RayleighDistribution{T}"/>
		/// </summary>
		Rayleigh,
		/// <summary>
		/// <see cref="WeibullDistribution{T}"/>
		/// </summary>
		Weibull,
	}
	#endregion

	#region methods
	internal static class MklRngExtension
	{
		internal const int AccurateFlag = 1 << 30;
	}

	/// <summary>
	/// The MKL random number generation method for uniform distribution generators
	/// </summary>
	internal enum MklRngMethodUniform
	{
		/// <summary>
		/// Standard method
		/// </summary>
		Standard = 0,
		/// <summary>
		/// Accurate method
		/// </summary>
		Accurate = Standard | MklRngExtension.AccurateFlag,
	}
	/// <summary>
	/// The MKL random number generation method for uniform bits distribution generators
	/// </summary>
	internal enum MklRngMethodUniformBits
	{
		/// <summary>
		/// Standard method
		/// </summary>
		Standard = 0,
	}
	/// <summary>
	/// The MKL random number generation method for (one- or multi- dimensional) Gaussian/normal distribution generators
	/// </summary>
	internal enum MklRngMethodGaussian
	{
		/// <summary>
		/// Generates normally distributed random number <c>x</c> through the pair of uniformly distributed numbers <c>u₁</c> and <c>u₂</c> according to the formula:
		/// <c>x = sqrt(-ln(u₁)) * sin(2*π*u₂)</c>
		/// </summary>
		BoxMuller,
		/// <summary>
		/// Generates pair of normally distributed random numbers <c>x₁</c> and <c>x₂</c> through the pair of uniformly distributed numbers <c>u₁</c> and <c>u₂</c> according to the formulas:
		/// <c>x₁ = sqrt(-ln(u₁)) * sin(2*π*u₂)</c>, <c>x₂ = sqrt(-ln(u₁)) * cos(2*π*u₂)</c>
		/// </summary>
		BoxMuller2,
		/// <summary>
		/// The inverse cumulative distribution function method
		/// </summary>
		ICDF,
	}
	/// <summary>
	/// The MKL random number generation method for exponential distribution generators
	/// </summary>
	internal enum MklRngMethodExponential
	{
		/// <summary>
		/// The inverse cumulative distribution function method
		/// </summary>
		ICDF,
		/// <summary>
		/// The accurate inverse cumulative distribution function method
		/// </summary>
		ICDFAccurate = ICDF | MklRngExtension.AccurateFlag,
	}
	/// <summary>
	/// The MKL random number generation method for Laplace distribution generators
	/// </summary>
	internal enum MklRngMethodLaplace
	{
		/// <summary>
		/// The inverse cumulative distribution function method
		/// </summary>
		ICDF,
	}
	/// <summary>
	/// The MKL random number generation method for Weibull distribution generators
	/// </summary>
	internal enum MklRngMethodWeibull
	{
		/// <summary>
		/// The inverse cumulative distribution function method
		/// </summary>
		ICDF,
		/// <summary>
		/// The accurate inverse cumulative distribution function method
		/// </summary>
		ICDFAccurate = ICDF | MklRngExtension.AccurateFlag,
	}
	/// <summary>
	/// The MKL random number generation method for Cauchy distribution generators
	/// </summary>
	internal enum MklRngMethodCauchy
	{
		/// <summary>
		/// The inverse cumulative distribution function method
		/// </summary>
		ICDF,
	}
	/// <summary>
	/// The MKL random number generation method for Rayleigh distribution generators
	/// </summary>
	internal enum MklRngMethodRayleigh
	{
		/// <summary>
		/// The inverse cumulative distribution function method
		/// </summary>
		ICDF,
		/// <summary>
		/// The accurate inverse cumulative distribution function method
		/// </summary>
		ICDFAccurate = ICDF | MklRngExtension.AccurateFlag,
	}
	/// <summary>
	/// The MKL random number generation method for log Gaussian/normal distribution generators
	/// </summary>
	internal enum MklRngMethodLogNormal
	{
		/// <summary>
		/// Generates pair of normally distributed random numbers <c>x₁</c> and <c>x₂</c> through the pair of uniformly distributed numbers <c>u₁</c> and <c>u₂</c> according to the formulas:
		/// <c>x₁ = sqrt(-ln(u₁)) * sin(2*π*u₂)</c>, <c>x₂ = sqrt(-ln(u₁)) * cos(2*π*u₂)</c>
		/// </summary>
		BoxMuller2,
		/// <summary>
		/// The inverse cumulative distribution function method
		/// </summary>
		ICDF,
		/// <summary>
		/// The accurate <see cref="BoxMuller2"/> method
		/// </summary>
		BoxMuller2Accurate = BoxMuller2 | MklRngExtension.AccurateFlag,
		/// <summary>
		/// The accurate inverse cumulative distribution function method
		/// </summary>
		ICDFAccurate = ICDF | MklRngExtension.AccurateFlag,
	}
	/// <summary>
	/// The MKL random number generation method for Gumbel distribution generators
	/// </summary>
	internal enum MklRngMethodGumbel
	{
		/// <summary>
		/// The inverse cumulative distribution function method
		/// </summary>
		ICDF,
	}
	/// <summary>
	/// The MKL random number generation method for Gamma distribution generators
	/// </summary>
	internal enum MklRngMethodGamma
	{
		/// <summary>
		/// <list type="table">
		/// <listheader><term>α</term>			<description>  Actual algorithm</description></listheader>
		/// <item><term>α &gt; 1</term>			<description>  algorithm of Marsaglia is used, nonlinear transformation of Gaussian numbers based on acceptance/rejection method with squeezes</description></item>
		/// <item><term>0.6 ≤ α &lt; 1</term>	<description>  rejection from the Weibull distribution is used</description></item>
		/// <item><term>α &lt; 0.6</term>		<description>  transformation of exponential power distribution (EPD) is used, EPD random numbers are generated by means of acceptance/rejection technique</description></item>
		/// <item><term>α == 1</term>			<description>  gamma distribution reduces to exponential distribution</description></item>
		/// </list>
		/// </summary>
		GNorm,
		/// <summary>
		/// The accurate <see cref="GNorm"/> method
		/// </summary>
		GNormAccurate = GNorm | MklRngExtension.AccurateFlag,
	}
	/// <summary>
	/// The MKL random number generation method for Beta distribution generators
	/// </summary>
	internal enum MklRngMethodBeta
	{
		/// <summary>
		/// CJA - stands for first letters of Cheng, Johnk, and Atkinson:
		/// <list type="table">
		/// <listheader><term>p, q</term>						<description>  Actual algorithm</description></listheader>
		/// <item><term>min(p,q) &gt; 1</term>					<description>  Cheng's method: generation of beta random numbers of the second kind based on acceptance/rejection technique and its transformation to beta random numbers of the first kind</description></item>
		/// <item><term>max(p,q) &lt; 1</term>					<description>  Method of Johnk and Atkinson:<br/>
		/// If <c>q + K*p^2+C ≤ 0, K=0.852..., C=-0.956...</c>, use algorithm of Johnk: beta distributed random number is generated as <c>u₁^(1/p) / (u₁^(1/p) + u₂^(1/q))</c> if <c>u₁^(1/p)+u₂^(1/q) ≤ 1</c>;<br/>
		/// otherwise switching algorithm of Atkinson: interval (0,1) is divided into two domains (0,t) and (t,1), on each interval acceptance/rejection technique with convenient majoring function is used</description></item>
		/// <item><term>min(p,q) &lt; 1, max(p,q) &gt; 1</term>	<description>  Method of Atkinson is used with another point t, see short description above</description></item>
		/// <item><term>Otherwise</term>						<description>  Use the ICDF</description></item>
		/// </list>
		/// </summary>
		CJA,
		/// <summary>
		/// The accurate <see cref="CJA"/> method
		/// </summary>
		CJAAccurate = CJA | MklRngExtension.AccurateFlag,
	}
	/// <summary>
	/// The MKL random number generation method for ChiSquare distribution generators
	/// </summary>
	internal enum MklRngMethodChiSquare
	{
		/// <summary>
		/// <list type="table">
		/// <listheader><term>Degree of freedom v</term><description>  Actual algorithm</description></listheader>
		/// <item><term>v = 1 or v = 3</term>			<description>  chi-square distributed random number is generated as a sum of squares of v independent normal random numbers</description></item>
		/// <item><term>v is even or v = 16</term>		<description>  chi-square distributed random number is generated using the following formula: <c>x = -2*ln(u[0]*...*u[v/2-1])</c>, where u[i] are random numbers uniformly distributed over the interval (0,1)</description></item>
		/// <item><term>v > 16 or (v is odd and v > 3)</term><description>  chi-square distribution reduces to gamma distribution</description></item>
		/// </list>
		/// </summary>
		Chi2Gamma,
	}
	/// <summary>
	/// The MKL random number generation method for Bernoulli distribution generators
	/// </summary>
	internal enum MklRngMethodBernoulli
	{
		/// <summary>
		/// The inverse cumulative distribution function method
		/// </summary>
		ICDF,
	}
	/// <summary>
	/// The MKL random number generation method for geometric distribution generators
	/// </summary>
	internal enum MklRngMethodGeometric
	{
		/// <summary>
		/// The inverse cumulative distribution function method
		/// </summary>
		ICDF,
	}
	/// <summary>
	/// The MKL random number generation method for binomial distribution generators
	/// </summary>
	internal enum MklRngMethodBinomial
	{
		/// <summary>
		/// For <c>ntrial*min(p,1-p) &gt; 30</c> acceptance/rejection method with decomposition onto 4 regions: 2 parallelograms, triangle, left exponential tail and right exponential tail. Otherwise table lookup method is used
		/// </summary>
		BTPE,
	}
	/// <summary>
	/// The MKL random number generation method for hyper geometric distribution generators
	/// </summary>
	internal enum MklRngMethodHypergeometric
	{
		/// <summary>
		/// For <c>ntrial*min(p,1-p) &gt; 30</c> acceptance/rejection method with decomposition onto 3 regions: rectangular, left exponential tail and right exponential tail. Otherwise table lookup method is used
		/// </summary>
		H2PE,
	}
	/// <summary>
	/// The MKL random number generation method for Poisson distribution generators
	/// </summary>
	internal enum MklRngMethodPoisson
	{
		/// <summary>
		/// For <c>λ ≥ 27</c>, acceptance/rejection method with decomposition onto 4 regions: 2 parallelograms, triangle, left exponential tail and right exponential tail. Otherwise table lookup method is used
		/// </summary>
		PTPE,
		/// <summary>
		/// For <c>λ ≥ 1</c>, this method is based on Poisson inverse CDF approximation by Gaussian inverse CDF; otherwise, table lookup method is used.
		/// </summary>
		PossionNorm,
	}
	/// <summary>
	/// The MKL random number generation method for Poisson distribution with varying mean generators
	/// </summary>
	internal enum MklRngMethodPoissonVariableMean
	{
		/// <summary>
		/// For <c>λ ≥ 1</c>, this method is based on Poisson inverse CDF approximation by Gaussian inverse CDF; otherwise, table lookup method is used.
		/// </summary>
		PossionNorm,
	}
	/// <summary>
	/// The MKL random number generation method for negative binomial distribution generators
	/// </summary>
	internal enum MklRngMethodNegativeBinomial
	{
		/// <summary>
		/// For <c>(a-1)*(1-p)/p ≥ 100</c>, acceptance/rejection method is used with decomposition onto 5 regions: rectangular, 2 trapezoid, left exponential tail and right exponential tail. Otherwise table lookup method is used.
		/// </summary>
		NBar,
	}
	/// <summary>
	/// The MKL random number generation method for negative multinomial distribution generators
	/// </summary>
	internal enum MklRngMethodMultinomial
	{
		/// <summary>
		/// Poisson Approximation of Multinomial Distribution method.
		/// </summary>
		MultiPoisson,
	}
	#endregion
}

namespace Althea.Backend.Mkl
{
	/// <summary>
	/// The static class for checking <see cref="MklRngStatus"/>
	/// </summary>
	public static partial class StatusExtension
	{
		/// <summary>
		/// Check the given <see cref="MklRngStatus"/>
		/// </summary>
		/// <param name="status">The given <see cref="MklRngStatus"/> to be checked</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Check(this MklRngStatus status)
		{
			if (status != MklRngStatus.Success)
				throw new StatusException(status);
		}
	}
}