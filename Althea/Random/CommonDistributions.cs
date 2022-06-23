using Althea.NativeTypes;


namespace Althea.Random
{
	/// <summary>
	/// The struct for a one-dimensional uniform distributions.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <param name="LowerBound">The inclusive lower bound of this one-dimensional uniform distribution</param>
	/// <param name="UpperBound">The exclusive upper bound of this one-dimensional uniform distribution</param>
	/// <param name="RandomSeed">See <see cref="IRandomDistribution{TSelf}.RandomSeed"/></param>
	public readonly record struct UniformDistribution<T>(T LowerBound, T UpperBound, long? RandomSeed) : IRank1Distribution<T, UniformDistribution<T>>
		where T : unmanaged, INumber<T>
	{
		/// <summary>
		/// Create a new <see cref="UniformDistribution{T}"/> with the given random <paramref name="seed"/> and lower and upper bond equaling to 0 and 1 respectively.
		/// </summary>
		/// <param name="seed">The random seed of this uniform distribution, default null means let the internal implementation determine</param>
		public UniformDistribution(long? seed = null) : this(T.Zero, T.One, seed) { }

		static DataType IRandomDistribution<UniformDistribution<T>>.DataTypeAt(int rank) => IRank1Distribution<T, UniformDistribution<T>>.DataTypeAt(rank);

		/// <inheritdoc/>
		public bool IsValid() => this.UpperBound > this.LowerBound;
	}

	/// <summary>
	/// The struct for one-dimensional distribution that randomize each bit.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <param name="RandomSeed">See <see cref="IRandomDistribution{TSelf}.RandomSeed"/></param>
	public readonly record struct RandomBitsDistribution<T>(long? RandomSeed) : IRank1Distribution<T, RandomBitsDistribution<T>>
		where T : unmanaged, INumber<T>
	{
		/// <inheritdoc/>
		public bool IsValid() => true;

		static DataType IRandomDistribution<RandomBitsDistribution<T>>.DataTypeAt(int rank) => IRank1Distribution<T, RandomBitsDistribution<T>>.DataTypeAt(rank);
	}
}
