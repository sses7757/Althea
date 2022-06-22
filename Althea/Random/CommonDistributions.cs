namespace Althea.Random
{
	/// <summary>
	/// The class for a one-dimensional uniform distributions.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <param name="LowerBound">The inclusive lower bound of this one-dimensional uniform distribution</param>
	/// <param name="UpperBound">The exclusive upper bound of this one-dimensional uniform distribution</param>
	/// <param name="RandomSeed"><inheritdoc/></param>
	public record UniformDistribution<T>(T LowerBound, T UpperBound, long? RandomSeed) : Rank1RandomDistribution<T>(RandomSeed) where T : unmanaged, INumber<T>
	{
		/// <summary>
		/// Create a new <see cref="UniformDistribution{T}"/> with the given random <paramref name="seed"/> and lower and upper bond equaling to 0 and 1 respectively.
		/// </summary>
		/// <param name="seed">The random seed of this uniform distribution, default null means let the internal implementation determine</param>
		public UniformDistribution(long? seed = null) : this(T.Zero, T.One, seed) { }

		/// <inheritdoc/>
		public override bool IsValid() => this.UpperBound > this.LowerBound;
	}

	/// <summary>
	/// The class for one-dimensional distributions that randomize each bit.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <param name="RandomSeed"><inheritdoc/></param>
	public record RandomBitsDistribution<T>(long? RandomSeed) : Rank1RandomDistribution<T>(RandomSeed) where T : unmanaged, INumber<T>
	{
		/// <inheritdoc/>
		public override bool IsValid() => true;
	}
}
