using System.Runtime.InteropServices;


namespace Althea.Random;

// Ignore Spelling: \det covar \frac \left \right \vec

/// <summary>
/// The struct for a two-dimensional normal distribution of type <typeparamref name="T"/>
/// </summary>
/// <typeparam name="T">Any unmanaged floating point type</typeparam>
/// <param name="Mean1">The first mean value (μ<sub>1</sub>)</param>
/// <param name="Mean2">The second mean value (μ<sub>2</sub>)</param>
/// <param name="StandardDeviation1">The first standard deviation (σ<sub>1</sub>)</param>
/// <param name="StandardDeviation2">The second standard deviation (σ<sub>2</sub>)</param>
/// <param name="Correlation">The correlation coefficient between two random variates (ρ)</param>
/// <param name="RandomSeed"><inheritdoc/></param>
//tex:Two-dimensional normal distribution PDF: $$P_{\mu_1,\mu_2,\sigma_1,\sigma_2,\rho}(x,y) = \frac{1}{2\pi\sigma_1\sigma_2\sqrt{1-\rho^2}}\exp{\left[-\frac{1}{2\left(1-\rho^2\right)}\left(\frac{{(x-\mu_1)}^2}{\sigma_1^2}-\frac{2\rho(x-\mu_1)(y-\mu_2)}{\sigma_1\sigma_2}+\frac{{(y-\mu_2)}^2}{\sigma_2^2}\right)\right]}$$
public readonly record struct BinormalDistribution<T>(T Mean1, T Mean2, T StandardDeviation1, T StandardDeviation2, T Correlation, long? RandomSeed = null) :
	IFloatingPointDistribution<T, BinormalDistribution<T>>, IRank2Distribution<T, T, BinormalDistribution<T>>
	where T : unmanaged, IBinaryFloat<T>
{
	/// <summary>
	/// Create a new bi-normal distribution with μ<sub>1</sub> = μ<sub>2</sub> = 0 and σ<sub>1</sub> = σ<sub>2</sub> = 1
	/// </summary>
	public BinormalDistribution(T correlation, long? seed = null) : this(T.Zero, T.Zero, T.One, T.One, correlation, seed) { }

	bool ICheckValid.IsValid() => IFloatingPointDistribution<T, BinormalDistribution<T>>.IsValid() && this.StandardDeviation1 > T.Zero && this.StandardDeviation2 > T.Zero && this.Correlation >= -T.One && this.Correlation <= T.One;

	static DataType IRandomDistribution<BinormalDistribution<T>>.DataTypeAt(int rank) => IRank2Distribution<T, T, BinormalDistribution<T>>.DataTypeAt(rank);
}

/// <summary>
/// The struct for a multi-dimensional normal distribution of type <typeparamref name="T"/>
/// </summary>
/// <typeparam name="T">Any unmanaged floating point type</typeparam>
/// <param name="OriginalCovarianceStored">Whether the original matrix or the upper column-major Cholesky factorization of <see cref="CovarianceMatrix"/> is stored</param>
/// <param name="RandomSeed"><inheritdoc/></param>
//tex:Multi-dimensional normal distribution PDF:
//$$P_{\vec\mu,\Sigma}(\vec x) = \frac{1}{(2\pi)^{D/2}}\frac{1}{\sqrt{\det(\Sigma)}}
//\exp{\left( -\frac12(\vec x - \vec \mu)^T \Sigma^{-1} (\vec x - \vec \mu) \right)}$$
//where $D$ is the number of dimensions, $\vec\mu$ is the mean values of all dimensions, $\Sigma$ is the covariance matrix (which is symmetric-definite).
[StructLayout(LayoutKind.Sequential)]
public readonly record struct MultiNormalDistribution<T>(bool OriginalCovarianceStored, long? RandomSeed = null) : IFloatingPointDistribution<T, MultiNormalDistribution<T>>, IRandomDistribution<MultiNormalDistribution<T>>
	where T : unmanaged, IBinaryFloat<T>
{
	#region basic
	private readonly T[] mean, covar;

	/// <inheritdoc/>
	public int Rank => this.mean.Length;

	/// <summary>
	/// The mean values of all dimensions
	/// </summary>
	public ReadOnlySpan<T> Means => this.mean;

	/// <summary>
	/// The covariance matrix of all dimensions stored in a 1D array
	/// </summary>
	public ReadOnlySpan<T> CovarianceMatrix => this.covar;

	static DataType IRandomDistribution<MultiNormalDistribution<T>>.DataTypeAt(int rank) => T.Type;

	bool ICheckValid.IsValid() => IFloatingPointDistribution<T, MultiNormalDistribution<T>>.IsValid() && this.Rank >= 3 && (!this.OriginalCovarianceStored || IsSymmetricPositiveDefinite(this.CovarianceMatrix));

	/// <summary>
	/// Create a multi-normal distribution of given <paramref name="means"/> and <paramref name="covar"/>.
	/// </summary>
	/// <param name="means">The mean values of all dimensions</param>
	/// <param name="covar">The covariance matrix (or its upper column-major Cholesky factorization) of all dimensions stored in a 1D array</param>
	/// <param name="originalCovar">Whether <paramref name="covar"/> is the original matrix or the Cholesky factorization</param>
	/// <param name="seed">The random seed</param>
	/// <exception cref="ArgumentException">If the size(s) is/are invalid</exception>
	public MultiNormalDistribution(ReadOnlySpan<T> means, ReadOnlySpan<T> covar, bool originalCovar, long? seed) : this(originalCovar, seed)
	{
		int rank = means.Length;
		if (rank < 2)
			throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(means));
		if (covar.Length != rank * rank)
			throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(covar));
		this.mean = means.ToArray();
		this.covar = covar.ToArray();
	}
	#endregion

	#region Cholesky
	private static bool CholeskyInternal(Span<T> a, Span<T> diag, int n)
	{
		for (int i = 0; i < n; i++)
		{
			for (int j = i; j < n; j++)
			{
				T sum = a[i * n + j];
				for (int k = i - 1; k >= 0; k--)
					sum -= a[i * n + k] * a[j * n + k];
				if (i == j)
				{
					if (sum <= T.Zero)
						return false;
					diag[i] = T.Sqrt(sum);
				}
				else
				{
					a[j * n + i] = sum / diag[i];
				}
			}
		}
		return true;
	}

	/// <summary>
	/// Compute the <see cref="CovarianceMatrix"/>'s upper column-major Cholesky factorization and store to <paramref name="cholesky"/>
	/// </summary>
	/// <returns>Success or not</returns>
	public readonly bool GetCholesky(Span<T> cholesky)
	{
		if (!this.OriginalCovarianceStored)
			return false;
		int n = this.Means.Length;
		if (cholesky.Length < n * n)
			return false;
		Span<T> diag = stackalloc T[n];
		this.CovarianceMatrix.CopyTo(cholesky);
		if (!CholeskyInternal(cholesky, diag, n))
			return false;
		for (int i = 0; i < n; i++)
		{
			cholesky[i + i * n] = diag[i];
			if (i != n - 1)
				cholesky[(i + 1 + i * n)..((i + 1) * n)].Fill(T.Zero);
		}
		return true;
	}

	private static void GetCholeskyInverse(Span<T> a, Span<T> diag)
	{
		int n = diag.Length;
		for (int i = 0; i < n; i++)
		{
			a[i * n + i] = T.One / diag[i];
			for (int j = i + 1; j < n; j++)
			{
				T sum = T.Zero;
				for (int k = i; k < j; k++)
					sum -= a[j * n + k] * a[k * n + i];
				a[j * n + i] = sum / diag[j];
			}
		}
	}

	private static bool IsSymmetricPositiveDefinite(ReadOnlySpan<T> matrix)
	{
		int n = (int)Math.Round(Math.Sqrt(matrix.Length));
		// symmetric
		for (int i = 0; i < n; i++)
		{
			for (int j = i + 1; j < n; j++)
			{
				if (matrix[i + j * n] != matrix[j + i * n])
					return false;
			}
		}
		// positive definite
		Span<T> a = stackalloc T[matrix.Length], diag = stackalloc T[n];
		matrix.CopyTo(a);
		return CholeskyInternal(a, diag, n);
	}
	#endregion
}

/// <summary>
/// The struct for a multinomial distribution of type <typeparamref name="T"/>
/// </summary>
/// <typeparam name="T">Any unmanaged integral type</typeparam>
/// <param name="NTrials">The number of trials <c>m</c></param>
/// <param name="RandomSeed"><inheritdoc/></param>
//tex:Multinomial normal distribution PDF:
//$$P_{m,\vec p}(\vec k) = \frac{m!}{\prod_i{k_i}} \prod_i{p^{k_i}}$$
public readonly record struct MultinomialDistribution<T>(int NTrials, long? RandomSeed = null) : IFloatingPointDistribution<T, MultinomialDistribution<T>>, IRandomDistribution<MultinomialDistribution<T>>
	where T : unmanaged, IBinaryInt<T>
{
	#region basic
	private readonly decimal[] probs;

	/// <inheritdoc/>
	public int Rank => this.probs.Length;

	/// <summary>
	/// The probabilities values of all dimensions
	/// </summary>
	public ReadOnlySpan<decimal> Probabilities => this.probs;

	static DataType IRandomDistribution<MultinomialDistribution<T>>.DataTypeAt(int rank) => T.Type;

	bool ICheckValid.IsValid() => IFloatingPointDistribution<T, MultinomialDistribution<T>>.IsValid() && this.Rank >= 3;

	/// <summary>
	/// Create a multinomial distribution of given <paramref name="probs"/>.
	/// </summary>
	/// <param name="trials">The number of trials</param>
	/// <param name="probs">The probability values of all dimensions</param>
	/// <param name="seed">The random seed</param>
	/// <exception cref="ArgumentException">If the size(s) is/are invalid</exception>
	public MultinomialDistribution(int trials, ReadOnlySpan<decimal> probs, long? seed) : this(trials, seed)
	{
		if (trials <= 0)
			throw new ArgumentOutOfRangeException(nameof(trials), trials, Resources.ParameterError.MustPositive);
		int rank = probs.Length;
		if (rank < 3)
			throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(probs));
		if (probs.Length != rank)
			throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(probs));
		this.probs = probs.ToArray();
	}
	#endregion
}
