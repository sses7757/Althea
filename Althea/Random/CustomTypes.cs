using Althea.NativeTypes;


namespace Althea.Random
{
	#region abstract
	/// <summary>
	/// The interface for the meta-data of a random number generator which generates random numbers from a certain random distribution.
	/// </summary>
	public interface IRandomDistribution
	{
		/// <summary>
		/// When implemented by a derived class, statically get a the <see cref="DataType"/> of the given <paramref name="rank"/>.
		/// </summary>
		abstract static DataType DataTypeAt(int rank);

		/// <summary>
		/// When implemented by a derived class, statically get the rank of this <see cref="IRandomDistribution"/>.
		/// </summary>
		abstract static int Rank { get; }

		/// <summary>
		/// When implemented by a derived class, get the random seed of this <see cref="IRandomDistribution"/>. Null means let the internal implementation determine.
		/// </summary>
		long? RandomSeed { get; }
	}
	#endregion

	#region concrete ones
	/// <summary>
	/// The class for a one-dimensional uniform distributions.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	public class UniformDistribution<T> : Rank1RandomDistribution<T> where T : unmanaged, INumber<T>
	{
		/// <summary>
		/// Get the inclusive lower bound of this one-dimensional uniform distribution
		/// </summary>
		public T LowerBound { get; }

		/// <summary>
		/// Get the exclusive upper bound of this one-dimensional uniform distribution
		/// </summary>
		public T UpperBound { get; }

		/// <inheritdoc/>
		public override long? RandomSeed { get; }

		/// <summary>
		/// Create a new <see cref="UniformDistribution{T}"/> with the given <paramref name="lower"/> and <paramref name="upper"/> bounds and the random <paramref name="seed"/>
		/// </summary>
		/// <param name="upper">The inclusive lower bound of this one-dimensional uniform distribution</param>
		/// <param name="lower">The exclusive upper bound of this one-dimensional uniform distribution</param>
		/// <param name="seed">The random seed of this uniform distribution, default null means let the internal implementation determine</param>
		public UniformDistribution(T upper, T lower = default, long? seed = null)
		{
			this.UpperBound = upper; this.LowerBound = lower; this.RandomSeed = seed;
		}

		/// <summary>
		/// Create a new <see cref="UniformDistribution{T}"/> with the given random <paramref name="seed"/> and lower and upper bond equaling to 0 and 1 respectively.
		/// </summary>
		/// <param name="seed">The random seed of this uniform distribution, default null means let the internal implementation determine</param>
		public UniformDistribution(long? seed = null)
		{
			this.UpperBound = T.One; this.LowerBound = T.Zero; this.RandomSeed = seed;
		}
	}

	/// <summary>
	/// The class for one-dimensional distributions that randomize each bit.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	public class RandomBitsDistribution<T> : Rank1RandomDistribution<T> where T : unmanaged, INumber<T>
	{
		/// <inheritdoc/>
		public override long? RandomSeed { get; }

		/// <summary>
		/// Create a new <see cref="RandomBitsDistribution{T}"/> with the given random <paramref name="seed"/>
		/// </summary>
		/// <param name="seed">The random seed of this uniform distribution, default null means let the internal implementation determine</param>
		public RandomBitsDistribution(long? seed = null)
		{
			this.RandomSeed = seed;
		}
	}

	// Ignore Spelling: \dfrac \ln \det \lt \alpha' \mbox \dbinom \binom \frac \right
	/// <summary>
	/// The class for one-dimensional normal (Gaussian) distributions.
	/// </summary>
	/// <typeparam name="T">Any unmanaged floating point type</typeparam>
	//tex:Normal distribution PDF: $$P_{\mu,\sigma}(x)=\frac{1}{\sigma\sqrt{2\pi}}\exp{\left( -\frac{(x-\mu)^2}{2\sigma^2} \right)}$$
	public class NormalDistribution<T> : Rank1RandomDistribution<T>, IEqualityOperators<NormalDistribution<T>, NormalDistribution<T>> where T : unmanaged, IFloatingPoint<T>
	{
		/// <summary>
		/// Get the mean value of this normal distribution
		/// </summary>
		public T Mean { get; }

		/// <summary>
		/// Get the standard deviation value of this normal distribution
		/// </summary>
		public T StandardDeviation { get; }

		/// <inheritdoc/>
		public override long? RandomSeed { get; }

		/// <summary>
		/// Create a standard normal distribution with mean = 0 and standard deviation = 1, and random seed is not set
		/// </summary>
		public NormalDistribution()
		{
			this.Mean = T.Zero; this.StandardDeviation = T.One;
		}

		/// <summary>
		/// Create a normal distribution with given <paramref name="mean"/>, <paramref name="stddev"/> and the random <paramref name="seed"/>
		/// </summary>
		/// <param name="mean">The given mean value</param>
		/// <param name="stddev">The given standard deviation</param>
		/// <param name="seed">The given random seed, default null means has no preferred random seed</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="stddev"/> is not larger than 0</exception>
		public NormalDistribution(T mean, T stddev, long? seed = null)
		{
			if (stddev <= T.Zero)
				throw new ArgumentOutOfRangeException(nameof(stddev), stddev, Resources.ParameterError.MustPositive);
			this.StandardDeviation = stddev; this.Mean = mean; this.RandomSeed = seed;
		}

		/// <inheritdoc/>
		public bool Equals(NormalDistribution<T>? other)
		{
			if (other is null)
				return false;
			if (ReferenceEquals(this, other))
				return true;
			return this.RandomSeed == other.RandomSeed && this.Mean == other.Mean && this.StandardDeviation == other.StandardDeviation;
		}

		/// <inheritdoc/>
		public override bool Equals(object? obj) => this.Equals(obj as NormalDistribution<T>);

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			return HashCode.Combine(this.Mean, this.StandardDeviation, this.RandomSeed);
		}

		/// <inheritdoc/>
		public static bool operator ==(NormalDistribution<T>? left, NormalDistribution<T>? right) => left is null && right is null || left is not null && left.Equals(right);

		/// <inheritdoc/>
		public static bool operator !=(NormalDistribution<T>? left, NormalDistribution<T>? right) => !(left == right);
	}
	#endregion
}
