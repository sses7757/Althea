using System.Runtime.CompilerServices;

using Althea.NativeTypes;


namespace Althea.Random
{
	#region interface
	/// <summary>
	/// The interface for the meta-data of a random number generator which generates random numbers from a certain random distribution.
	/// </summary>
	public interface IRandomDistribution : ICheckValid
	{
		/// <summary>
		/// When implemented by a derived class, statically get a the <see cref="DataType"/> of the given <paramref name="rank"/>.
		/// </summary>
		abstract static DataType DataTypeAt(int rank);

		/// <summary>
		/// When implemented by a derived class, get the rank of this <see cref="IRandomDistribution"/>.
		/// </summary>
		int Rank { get; }

		/// <summary>
		/// When implemented by a derived class, get the random seed of this <see cref="IRandomDistribution"/>. Null means let the internal implementation determine.
		/// </summary>
		long? RandomSeed { get; }
	}
	#endregion

	#region abstracts
	/// <summary>
	/// The abstract class for one-dimensional distributions which contain the <see cref="Displacement"/> and <see cref="ScaleFactor"/> of type <typeparamref name="T"/> as the parameters, inherits <see cref="Rank1RandomDistribution{T}"/>.
	/// </summary>
	/// <typeparam name="T">Any unmanaged floating point type</typeparam>
	/// <param name="Displacement">The displacement factor (μ)</param>
	/// <param name="ScaleFactor">The scaling factor (β)</param>
	/// <param name="RandomSeed"><inheritdoc/></param>
	public abstract record DisplaceScaleDistribution<T>(T Displacement, T ScaleFactor, long? RandomSeed) : Rank1RandomDistribution<T>(RandomSeed) where T : unmanaged, INumber<T>
	{
		/// <summary>
		/// Create a standard <see cref="DisplaceScaleDistribution{T}"/> with μ = 0, β = 1
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected DisplaceScaleDistribution(long? seed = null) : this(T.Zero, T.One, seed) { }

		/// <summary>
		/// Statically check the type <typeparamref name="T"/>'s validness for floating point real type constraint
		/// </summary>
		public static bool TypeValid() => (Unmanaged<T>.DataType & DataTypeExtension.MakeDataType(DataTypeTuple.Real, DataTypeClassification.FloatPoint_IEEE754, unchecked((DataTypeSize)ushort.MaxValue))) != 0;

		/// <inheritdoc/>
		public override bool IsValid() => TypeValid() && this.ScaleFactor > T.Zero;
	}

	/// <summary>
	/// The abstract class for one-dimensional distributions which contain the <see cref="ShapeFactor"/> of type <typeparamref name="T"/> and the parameters of <see cref="DisplaceScaleDistribution{T}"/> as the parameters, inherits <see cref="DisplaceScaleDistribution{T}"/>.
	/// </summary>
	/// <typeparam name="T">Any unmanaged floating point type</typeparam>
	/// <param name="Displacement">The displacement factor (μ)</param>
	/// <param name="ScaleFactor">The scaling factor (β)</param>
	/// <param name="ShapeFactor">The shaping factor (α)</param>
	/// <param name="RandomSeed"><inheritdoc/></param>
	public abstract record ShapeDisplaceScaleDistribution<T>(T Displacement, T ScaleFactor, T ShapeFactor, long? RandomSeed) : DisplaceScaleDistribution<T>(Displacement, ScaleFactor, RandomSeed)  where T : unmanaged, INumber<T>
	{
		/// <summary>
		/// Create a standard <see cref="ShapeDisplaceScaleDistribution{T}"/> with μ = 0, α = 1, β = 1
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected ShapeDisplaceScaleDistribution(long? seed = null) : this(T.Zero, T.One, T.Zero, seed) { }
	}

	/// <summary>
	/// The abstract class for one-dimensional distributions which contains <see cref="DegreeOfFreedom"/> as the parameter, inherits <see cref="Rank1RandomDistribution{T}"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged floating point type</typeparam>
	/// <param name="DegreeOfFreedom">The degree of freedom</param>
	/// <param name="RandomSeed"><inheritdoc/></param>
	public abstract record DegreeOfFreedomDistribution<T>(int DegreeOfFreedom, long? RandomSeed) : Rank1RandomDistribution<T>(RandomSeed) where T : unmanaged, INumber<T>
	{
		/// <summary>
		/// Create a standard <see cref="DegreeOfFreedomDistribution{T}"/> with DoF = 1
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected DegreeOfFreedomDistribution(long? seed = null) : this(1, seed) { }

		/// <inheritdoc/>
		public override bool IsValid() => DisplaceScaleDistribution<T>.TypeValid() && this.DegreeOfFreedom > 0;
	}

	/// <summary>
	/// The abstract class for one-dimensional distributions based on the Bernoulli trial, inherits <see cref="Rank1RandomDistribution{T}"/>.
	/// </summary>
	/// <typeparam name="T">Any unmanaged integral type</typeparam>
	/// <param name="Probability">The probability of a Bernoulli trial succeeding (p)</param>
	/// <param name="RandomSeed"><inheritdoc/></param>
	public abstract record BernoulliBasedDistribution<T>(decimal Probability, long? RandomSeed) : Rank1RandomDistribution<T>(RandomSeed) where T : unmanaged, INumber<T>
	{
		/// <summary>
		/// Create a standard <see cref="BernoulliBasedDistribution{T}"/> with p = 0.5
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected BernoulliBasedDistribution(long? seed = null) : this(0.5m, seed) { }

		/// <summary>
		/// Statically check the type <typeparamref name="T"/>'s validness for integral real type constraint
		/// </summary>
		public static bool TypeValid() => (Unmanaged<T>.DataType & DataTypeExtension.MakeDataType(DataTypeTuple.Real, DataTypeClassification.UnsignedInteger | DataTypeClassification.UnsignedInteger, unchecked((DataTypeSize)ushort.MaxValue))) != 0;

		/// <inheritdoc/>
		public override bool IsValid() => TypeValid() && this.Probability > 0 && this.Probability < 1;
	}
	#endregion
}
