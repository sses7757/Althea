using System.Text;

using Althea.Linq;
using Althea.NativeTypes;


namespace Althea.Random
{
	// Ignore Spelling: \det

	/// <summary>
	/// The class for a two-dimensional normal distribution of type <typeparamref name="T"/>, implements <see cref="Rank2RandomDistribution{T1, T2}"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged floating point type</typeparam>
	/// <param name="Mean1">The first mean value (μ<sub>1</sub>)</param>
	/// <param name="Mean2">The second mean value (μ<sub>2</sub>)</param>
	/// <param name="StandardDeviation1">The first standard deviation (σ<sub>1</sub>)</param>
	/// <param name="StandardDeviation2">The second standard deviation (σ<sub>2</sub>)</param>
	/// <param name="Covariance">The covariance between two random variates (ρ)</param>
	/// <param name="RandomSeed"><inheritdoc/></param>
	//tex:Two-dimensional normal distribution PDF: $$P_{\mu_1,\mu_2,\sigma_1,\sigma_2,\rho}(x,y) = \frac{1}{2\pi\sigma_1\sigma_2\sqrt{1-\rho^2}}\exp{\left[-\frac{1}{2\left(1-\rho^2\right)}\left(\frac{{(x-\mu_1)}^2}{\sigma_1^2}-\frac{2\rho(x-\mu_1)(y-\mu_2)}{\sigma_1\sigma_2}+\frac{{(y-\mu_2)}^2}{\sigma_2^2}\right)\right]}$$
	public sealed record BinormalDistribution<T>(T Mean1, T Mean2, T StandardDeviation1, T StandardDeviation2, T Covariance, long? RandomSeed) : Rank2RandomDistribution<T, T>(RandomSeed) where T : unmanaged, IFloatingPoint<T>
	{
		/// <summary>
		/// Create a new bi-normal distribution with μ<sub>1</sub> = μ<sub>2</sub> = 0 and σ<sub>1</sub> = σ<sub>2</sub> = 1
		/// </summary>
		public BinormalDistribution(T covariance, long? seed = null) : this(T.Zero, T.Zero, T.One, T.One, covariance, seed) { }

		/// <inheritdoc/>
		public override bool IsValid() => this.StandardDeviation1 > T.Zero && this.StandardDeviation2 > T.Zero && this.Covariance >= -T.One && this.Covariance <= T.One;
	}

	/// <summary>
	/// The class for a multi-dimensional normal distribution of type <typeparamref name="T"/>, implements <see cref="IRandomDistribution"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged floating point type</typeparam>
	/// <param name="Means">The mean values of all dimensions</param>
	/// <param name="CovarianceMatrix">The covariance matrix of all dimensions</param>
	/// <param name="OriginalCovarianceStored">Whether the original matrix or the upper column-major Cholesky factorization of <see cref="CovarianceMatrix"/> is stored</param>
	/// <param name="RandomSeed"><inheritdoc/></param>
	//tex:Multi-dimensional normal distribution PDF:
	//$$P_{\vec\mu,\Sigma}(\vec x) = \frac{1}{(2\pi)^{D/2}}\frac{1}{\sqrt{\det(\Sigma)}}
	//\exp{\left( -\frac12(\vec x - \vec \mu)^T \Sigma^{-1} (\vec x - \vec \mu) \right)}$$
	//where $D$ is the number of dimensions, $\vec\mu$ is the mean values of all dimensions, $\Sigma$ is the covariance matrix (which is symmetric-definite).
	public sealed record MultiNormalDistribution<T>(T[] Means, T[] CovarianceMatrix, bool OriginalCovarianceStored, long? RandomSeed) : IRandomDistribution, ICheckValid where T : unmanaged, IFloatingPoint<T>
	{
		/// <inheritdoc/>
		public int Rank => this.Means?.Length ?? 0;

		/// <inheritdoc/>
		public static DataType DataTypeAt(int rank) => Unmanaged<T>.DataType;

		/// <inheritdoc/>
		public bool IsValid() => this.Rank >= 3 && this.Means.Length * this.Means.Length == this.CovarianceMatrix.Length && (!this.OriginalCovarianceStored || IsSymmetricPositiveDefinite(this.CovarianceMatrix));

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
		/// Change the <see cref="CovarianceMatrix"/>'s store mode to store the upper column-major Cholesky factorization
		/// </summary>
		public void ToCholeskyStore()
		{
			if (!this.OriginalCovarianceStored)
				return;
			int n = this.Means.Length;
			Span<T> a = stackalloc T[this.CovarianceMatrix.Length], diag = stackalloc T[n];
			new Span<T>(this.CovarianceMatrix).CopyTo(a);
			CholeskyInternal(a, diag, n);
			for (int i = 0; i < n; i++)
			{
				a[i + i * n] = diag[i];
				if (i != n - 1)
					a[(i + 1 + i * n)..((i + 1) * n)].Fill(T.Zero);
			}
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

		private static bool IsSymmetricPositiveDefinite(T[] matrix)
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
			new Span<T>(matrix).CopyTo(a);
			return CholeskyInternal(a, diag, n);
		}

		/// <inheritdoc/>
		public override string ToString()
		{
			StringBuilder builder = new();
			builder.Append(nameof(MultiNormalDistribution<T>)).Append('<').Append(typeof(T).Name).Append('>');
			builder.Append(" { ");
			builder.Append(nameof(Means)).Append(" = [").Append(string.Join(", ", this.Means)).Append(']');
			builder.Append(", ").Append(nameof(CovarianceMatrix));
			if (!this.OriginalCovarianceStored)
				builder.Append(" (Cholesky decomposed)");
			builder.Append(" = [");
			int n = this.Rank;
			for (int i = 0; i < n; i++)
			{
				builder.Append('[');
				for (int j = 0; j < n; j++)
				{
					builder.Append(this.CovarianceMatrix[j + i * n]).Append(',');
				}
				builder.Append(']');
				if (i != n - 1)
					builder.Append(", ");
			}
			builder.Append(']');
			builder.Append(", ").Append(nameof(RandomSeed)).Append(" = ").Append(RandomSeed);
			builder.Append(" }");
			return builder.ToString();
		}
	}

	/// <summary>
	/// The class for a multinomial distribution of type <typeparamref name="T"/>, implements <see cref="IRandomDistribution"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged integral type</typeparam>
	/// <param name="Probabilities">The probabilities of all dimensions</param>
	/// <param name="NTrials">The number of independent multi-dimensional Bernoulli trials (m)</param>
	/// <param name="RandomSeed"><inheritdoc/></param>
	//tex:Multinomial normal distribution PDF:
	//$$P_{m,\vec p}(\vec k) = \frac{m!}{\prod_i{k_i}} \prod_i{p^{k_i}}$$
	public sealed record MultinomialDistribution<T>(decimal[] Probabilities, int NTrials, long? RandomSeed) : IRandomDistribution, ICheckValid where T : unmanaged, IBinaryInteger<T>
	{
		/// <inheritdoc/>
		public int Rank => this.Probabilities?.Length ?? 0;

		/// <inheritdoc/>
		public static DataType DataTypeAt(int rank) => Unmanaged<T>.DataType;

		/// <inheritdoc/>
		public bool IsValid() => this.Rank >= 3 && this.Probabilities.Sum() == 1;

		/// <inheritdoc/>
		public override string ToString()
		{
			StringBuilder builder = new();
			builder.Append(nameof(MultinomialDistribution<T>)).Append('<').Append(typeof(T).Name).Append('>');
			builder.Append(" { ");
			builder.Append(nameof(Probabilities)).Append(" = [").Append(string.Join(", ", this.Probabilities)).Append(']');
			builder.Append(" }");
			return builder.ToString();
		}
	}
}
