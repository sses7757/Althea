using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

using Althea.Helpers;
using Althea.Linq;
using Althea.Numerics;


namespace Althea.Random
{
	// Ignore Spelling: \det

	/// <summary>
	/// The struct for a two-dimensional normal distribution of type <typeparamref name="T"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged floating point type</typeparam>
	/// <param name="Mean1">The first mean value (μ<sub>1</sub>)</param>
	/// <param name="Mean2">The second mean value (μ<sub>2</sub>)</param>
	/// <param name="StandardDeviation1">The first standard deviation (σ<sub>1</sub>)</param>
	/// <param name="StandardDeviation2">The second standard deviation (σ<sub>2</sub>)</param>
	/// <param name="Covariance">The covariance between two random variates (ρ)</param>
	/// <param name="RandomSeed"><inheritdoc/></param>
	//tex:Two-dimensional normal distribution PDF: $$P_{\mu_1,\mu_2,\sigma_1,\sigma_2,\rho}(x,y) = \frac{1}{2\pi\sigma_1\sigma_2\sqrt{1-\rho^2}}\exp{\left[-\frac{1}{2\left(1-\rho^2\right)}\left(\frac{{(x-\mu_1)}^2}{\sigma_1^2}-\frac{2\rho(x-\mu_1)(y-\mu_2)}{\sigma_1\sigma_2}+\frac{{(y-\mu_2)}^2}{\sigma_2^2}\right)\right]}$$
	public readonly record struct BinormalDistribution<T>(T Mean1, T Mean2, T StandardDeviation1, T StandardDeviation2, T Covariance, long? RandomSeed = null) :
		IFloatingPointDistribution<T, BinormalDistribution<T>>, IRank2Distribution<T, T, BinormalDistribution<T>>
		where T : unmanaged, IFloatingPointIeee754<T>
	{
		/// <summary>
		/// Create a new bi-normal distribution with μ<sub>1</sub> = μ<sub>2</sub> = 0 and σ<sub>1</sub> = σ<sub>2</sub> = 1
		/// </summary>
		public BinormalDistribution(T covariance, long? seed = null) : this(T.Zero, T.Zero, T.One, T.One, covariance, seed) { }

		bool ICheckValid.IsValid() => ((IFloatingPointDistribution<T, BinormalDistribution<T>>)this).IsValid() && this.StandardDeviation1 > T.Zero && this.StandardDeviation2 > T.Zero && this.Covariance >= -T.One && this.Covariance <= T.One;

		static DataType IRandomDistribution<BinormalDistribution<T>>.DataTypeAt(int rank) => IRank2Distribution<T, T, BinormalDistribution<T>>.DataTypeAt(rank);
	}

	/// <summary>
	/// The struct for a multi-dimensional normal distribution of type <typeparamref name="T"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged floating point type</typeparam>
	//tex:Multi-dimensional normal distribution PDF:
	//$$P_{\vec\mu,\Sigma}(\vec x) = \frac{1}{(2\pi)^{D/2}}\frac{1}{\sqrt{\det(\Sigma)}}
	//\exp{\left( -\frac12(\vec x - \vec \mu)^T \Sigma^{-1} (\vec x - \vec \mu) \right)}$$
	//where $D$ is the number of dimensions, $\vec\mu$ is the mean values of all dimensions, $\Sigma$ is the covariance matrix (which is symmetric-definite).
	[StructLayout(LayoutKind.Sequential)]
	public readonly struct MultiNormalDistribution<T> : IEqualityOperators<MultiNormalDistribution<T>, MultiNormalDistribution<T>>,
		IFloatingPointDistribution<T, MultiNormalDistribution<T>>, IRandomDistribution<MultiNormalDistribution<T>>
		where T : unmanaged, IFloatingPointIeee754<T>
	{
		#region basic
		private readonly int rank;
		private readonly bool hasSeed, originalCovar;
		private readonly long seed;

		/// <inheritdoc/>
		public int Rank => this.rank;

		/// <inheritdoc/>
		public long? RandomSeed => this.hasSeed ? this.seed : null;

		/// <summary>
		/// Whether the original matrix or the upper column-major Cholesky factorization of <see cref="CovarianceMatrix"/> is stored
		/// </summary>
		public bool OriginalCovarianceStored => this.originalCovar;

		/// <summary>
		/// The mean values of all dimensions
		/// </summary>
		public ReadOnlySpan<T> Means => this.P_Means;

		/// <summary>
		/// The covariance matrix of all dimensions stored in a 1D array
		/// </summary>
		public ReadOnlySpan<T> CovarianceMatrix => this.P_CovarianceMatrix;

		private Span<T> P_Means => SpanHelper.CreateSpanFromReadOnly<long, T>(in this.seed, 1, this.rank);

		private Span<T> P_CovarianceMatrix => SpanHelper.CreateSpanFromReadOnly<long, T>(in this.seed, 1, this.rank * this.rank);

		static DataType IRandomDistribution<MultiNormalDistribution<T>>.DataTypeAt(int rank) => Unmanaged<T>.DataType;

		bool ICheckValid.IsValid() => ((IFloatingPointDistribution<T, MultiNormalDistribution<T>>)this).IsValid() && this.rank >= 3 && (!this.OriginalCovarianceStored || IsSymmetricPositiveDefinite(this.CovarianceMatrix));

		private unsafe MultiNormalDistribution(int rank, bool originalCovar, long? seed)
		{
			this.rank = rank;
			this.originalCovar = originalCovar;
			this.hasSeed = seed.HasValue; this.seed = seed ?? 0;
		}

		/// <summary>
		/// Statically get the size in bytes of the <see cref="Span{T}"/> to create which will be used to store <see cref="MultinomialDistribution{T}"/>.
		/// </summary>
		/// <param name="rank">The rank, must be positive</param>
		/// <returns>The size of the <see cref="Span{T}"/> to be created by invoker in bytes</returns>
		public static unsafe int DataSize(int rank) => rank <= 0 ? throw new ArgumentOutOfRangeException(nameof(rank)) : sizeof(MultiNormalDistribution<T>) + sizeof(T) * (rank + rank * rank);

		// Ignore Spelling: covar
		/// <summary>
		/// Create a multi-normal distribution of given <paramref name="means"/> and <paramref name="covar"/>.
		/// </summary>
		/// <param name="data">The <see cref="Span{T}"/> of <see cref="byte"/> used to store the actual <see cref="MultiNormalDistribution{T}"/>, must has length ≥ <see cref="DataSize(int)"/></param>
		/// <param name="means">The mean values of all dimensions</param>
		/// <param name="covar">The covariance matrix (or its upper column-major Cholesky factorization) of all dimensions stored in a 1D array</param>
		/// <param name="originalCovar">Whether <paramref name="covar"/> is the original matrix or the Cholesky factorization</param>
		/// <param name="seed">The random seed</param>
		/// <exception cref="ArgumentException">If the size(s) is/are invalid</exception>
		/// <example><code>
		/// <see cref="Span{T}"/> data = stackalloc byte[<see cref="MultiNormalDistribution{T}"/>.DataSize(rank)];
		/// <see cref="MultiNormalDistribution{T}.Create"/>(data, means, covar, originalCovar, seed);
		/// ref var dist = ref Unsafe.As&lt;byte, <see cref="MultiNormalDistribution{T}"/>&gt;(ref data[0]);
		/// </code></example>
		public static void Create(Span<byte> data, ReadOnlySpan<T> means, ReadOnlySpan<T> covar, bool originalCovar, long? seed)
		{
			int rank = means.Length;
			if (rank < 3)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(means));
			if (covar.Length != rank * rank)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(covar));
			if (data.Length < DataSize(rank))
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(data));
			ref var dist = ref Unsafe.As<byte, MultiNormalDistribution<T>>(ref data[0]);
			dist = new(rank, originalCovar, seed);
			means.CopyTo(dist.P_Means);
			covar.CopyTo(dist.P_CovarianceMatrix);
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
		/// Change the <see cref="CovarianceMatrix"/>'s store mode to store the upper column-major Cholesky factorization
		/// </summary>
		public void ToCholeskyStore()
		{
			if (!this.OriginalCovarianceStored)
				return;
			int n = this.Means.Length;
			Span<T> a = stackalloc T[this.CovarianceMatrix.Length], diag = stackalloc T[n];
			this.CovarianceMatrix.CopyTo(a);
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

		#region other
		/// <inheritdoc/>
		public override string ToString()
		{
			var means = this.Means;
			var covar = this.CovarianceMatrix;
			int n = this.rank;
			StringBuilder builder = new();
			builder.Append(nameof(MultiNormalDistribution<T>))
				   .Append('<')
				   .Append(typeof(T).Name)
				   .Append('>');
			builder.Append(" { ");
			builder.Append(nameof(Means))
				   .Append(" = [")
				   .Append(means.SpanJoin(", "))
				   .Append(']');
			builder.Append(", ").Append(nameof(CovarianceMatrix));
			if (!this.originalCovar)
				builder.Append(" (Cholesky decomposed)");
			builder.Append(" = [");
			for (int i = 0; i < n; i++)
			{
				builder.Append('[');
				for (int j = 0; j < n; j++)
				{
					builder.Append(covar[j + i * n])
						   .Append(',');
				}
				builder.Append(']');
				if (i != n - 1)
					builder.Append(", ");
			}
			builder.Append(']');
			builder.Append(", ")
				   .Append(nameof(RandomSeed))
				   .Append(" = ")
				   .Append(RandomSeed);
			builder.Append(" }");
			return builder.ToString();
		}

		/// <inheritdoc/>
		public bool Equals(MultiNormalDistribution<T> other) => this.rank == other.rank && this.RandomSeed == other.RandomSeed && this.originalCovar == other.originalCovar && this.Means.SequenceEqual(other.Means) && this.CovarianceMatrix.SequenceEqual(other.CovarianceMatrix);

		/// <inheritdoc/>
		public static bool operator ==(MultiNormalDistribution<T> left, MultiNormalDistribution<T> right) => left.Equals(right);

		/// <inheritdoc/>
		public static bool operator !=(MultiNormalDistribution<T> left, MultiNormalDistribution<T> right) => !(left == right);

		/// <inheritdoc/>
		public override bool Equals(object? obj) => obj is MultiNormalDistribution<T> dist && Equals(dist);

		/// <inheritdoc/>
		public override int GetHashCode() => HashCode.Combine(this.rank, this.originalCovar, this.RandomSeed, this.Means.HashCodeOfSpan(), this.CovarianceMatrix.HashCodeOfSpan());
		#endregion
	}

	/// <summary>
	/// The struct for a multinomial distribution of type <typeparamref name="T"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged integral type</typeparam>
	//tex:Multinomial normal distribution PDF:
	//$$P_{m,\vec p}(\vec k) = \frac{m!}{\prod_i{k_i}} \prod_i{p^{k_i}}$$
	public readonly struct MultinomialDistribution<T> : IEqualityOperators<MultinomialDistribution<T>, MultinomialDistribution<T>>,
		IFloatingPointDistribution<T, MultinomialDistribution<T>>, IRandomDistribution<MultinomialDistribution<T>>
		where T : unmanaged, IBinaryInteger<T>
	{
		#region basic
		private readonly int rank;
		private readonly bool hasSeed;
		private readonly long seed;

		/// <inheritdoc/>
		public int Rank => this.rank;

		/// <inheritdoc/>
		public long? RandomSeed => this.hasSeed ? this.seed : null;

		/// <summary>
		/// The probabilities values of all dimensions
		/// </summary>
		public ReadOnlySpan<T> Probabilities => this.P_Probabilities;

		private Span<T> P_Probabilities => SpanHelper.CreateSpanFromReadOnly<long, T>(in this.seed, 1, this.rank);

		static DataType IRandomDistribution<MultinomialDistribution<T>>.DataTypeAt(int rank) => Unmanaged<T>.DataType;

		bool ICheckValid.IsValid() => ((IFloatingPointDistribution<T, MultinomialDistribution<T>>)this).IsValid() && this.rank >= 3;

		private unsafe MultinomialDistribution(int rank, long? seed)
		{
			this.rank = rank;
			this.hasSeed = seed.HasValue; this.seed = seed ?? 0;
		}

		/// <summary>
		/// Statically get the size in bytes of the <see cref="Span{T}"/> to create which will be used to store <see cref="MultinomialDistribution{T}"/>.
		/// </summary>
		/// <param name="rank">The rank, must be positive</param>
		/// <returns>The size of the <see cref="Span{T}"/> to be created by invoker in bytes</returns>
		public static unsafe int DataSize(int rank) => rank <= 0 ? throw new ArgumentOutOfRangeException(nameof(rank)) : sizeof(MultinomialDistribution<T>) + sizeof(T) * rank;

		// Ignore Spelling: covar
		/// <summary>
		/// Create a multi-normal distribution of given <paramref name="probs"/>.
		/// </summary>
		/// <param name="data">The <see cref="Span{T}"/> of <see cref="byte"/> used to store the actual <see cref="MultinomialDistribution{T}"/>, must has length ≥ <see cref="DataSize(int)"/></param>
		/// <param name="probs">The probability values of all dimensions</param>
		/// <param name="seed">The random seed</param>
		/// <exception cref="ArgumentException">If the size(s) is/are invalid</exception>
		/// <example><code>
		/// <see cref="Span{T}"/> data = stackalloc byte[<see cref="MultiNormalDistribution{T}"/>.DataSize(rank)];
		/// <see cref="MultiNormalDistribution{T}.Create"/>(data, means, covar, originalCovar, seed);
		/// ref var dist = ref Unsafe.As&lt;byte, <see cref="MultiNormalDistribution{T}"/>&gt;(ref data[0]);
		/// </code></example>
		public static void Create(Span<byte> data, ReadOnlySpan<T> probs, long? seed)
		{
			int rank = probs.Length;
			if (rank < 3)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(probs));
			if (probs.Length != rank)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(probs));
			if (data.Length < DataSize(rank))
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(data));
			ref var dist = ref Unsafe.As<byte, MultinomialDistribution<T>>(ref data[0]);
			dist = new(rank, seed);
			probs.CopyTo(dist.P_Probabilities);
		}
		#endregion

		#region other
		/// <inheritdoc/>
		public override string ToString()
		{
			var probs = this.Probabilities;
			int n = this.rank;
			StringBuilder builder = new();
			builder.Append(nameof(MultinomialDistribution<T>))
				   .Append('<')
				   .Append(typeof(T).Name)
				   .Append('>');
			builder.Append(" { ");
			builder.Append(nameof(Probabilities))
				   .Append(" = [")
				   .Append(probs.SpanJoin(", "))
				   .Append(']');
			builder.Append(", ")
				   .Append(nameof(RandomSeed))
				   .Append(" = ")
				   .Append(RandomSeed);
			builder.Append(" }");
			return builder.ToString();
		}

		/// <inheritdoc/>
		public bool Equals(MultinomialDistribution<T> other) => this.rank == other.rank && this.RandomSeed == other.RandomSeed && this.Probabilities.SequenceEqual(other.Probabilities);

		/// <inheritdoc/>
		public static bool operator ==(MultinomialDistribution<T> left, MultinomialDistribution<T> right) => left.Equals(right);

		/// <inheritdoc/>
		public static bool operator !=(MultinomialDistribution<T> left, MultinomialDistribution<T> right) => !(left == right);

		/// <inheritdoc/>
		public override bool Equals(object? obj) => obj is MultinomialDistribution<T> dist && Equals(dist);

		/// <inheritdoc/>
		public override int GetHashCode() => HashCode.Combine(this.rank, this.RandomSeed, this.Probabilities.HashCodeOfSpan());
		#endregion
	}
}
