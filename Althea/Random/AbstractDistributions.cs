using System.Runtime.CompilerServices;


namespace Althea.Random;

#region interface
/// <summary>
/// The interface for the meta-data of a random number generator which generates random numbers from a certain random distribution.
/// </summary>
/// <typeparam name="TSelf">The type of actual implementation struct</typeparam>
public interface IRandomDistribution<TSelf> : ICheckValid where TSelf : struct, IRandomDistribution<TSelf>
{
	/// <summary>
	/// When implemented by a derived class, statically get a the <see cref="DataType"/> of the given <paramref name="rank"/>.
	/// </summary>
	abstract static DataType DataTypeAt(int rank);

	/// <summary>
	/// When implemented by a derived class, get the rank of this <typeparamref name="TSelf"/>.
	/// </summary>
	int Rank { get; }

	/// <summary>
	/// When implemented by a derived class, get the random seed of this <typeparamref name="TSelf"/>. Null means let the internal implementation determine.
	/// </summary>
	long? RandomSeed { get; }

	/// <summary>
	/// Statically check whether the <paramref name="dataType"/> is of given <paramref name="type"/>.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static bool CheckTypeValid(DataTypeClassification type, DataType dataType) => (dataType & DataTypeExtension.MakeDataType(DataTypeTuple.Real, type, unchecked((DataTypeSize)ushort.MaxValue))) != 0;
}
#endregion

#region abstracts
/// <summary>
/// The abstract interface for any-dimensional distributions whose data type is a floating point one, inherits <see cref="IRandomDistribution{TSelf}"/>.
/// </summary>
/// <typeparam name="T">Any unmanaged floating point type for all dimensions</typeparam>
/// <typeparam name="TSelf">The type of actual implementation struct</typeparam>
public interface IFloatingPointDistribution<T, TSelf> : IRandomDistribution<TSelf> where T : unmanaged, IBaseNumber<T> where TSelf : struct, IFloatingPointDistribution<T, TSelf>
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	bool ICheckValid.IsValid() => CheckTypeValid(DataTypeClassification.BinaryFloat_IEEE754, T.Type);
}

/// <summary>
/// The abstract interface for any-dimensional distributions whose data type is an integral one, inherits <see cref="IRandomDistribution{TSelf}"/>.
/// </summary>
/// <typeparam name="T">Any unmanaged floating point type for all dimensions</typeparam>
/// <typeparam name="TSelf">The type of actual implementation struct</typeparam>
public interface IIntegralDistribution<T, TSelf> : IRandomDistribution<TSelf> where T : unmanaged, IBaseNumber<T> where TSelf : struct, IIntegralDistribution<T, TSelf>
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	bool ICheckValid.IsValid() => CheckTypeValid(DataTypeClassification.UnsignedInteger | DataTypeClassification.SignedInteger, T.Type);
}

/// <summary>
/// The abstract class for one-dimensional distributions which contain the <see cref="Displacement"/> and <see cref="ScaleFactor"/> of type <typeparamref name="T"/> as the parameters, inherits <see cref="IFloatingPointDistribution{T, TSelf}"/>.
/// </summary>
/// <typeparam name="T">Any unmanaged floating point type</typeparam>
/// <typeparam name="TSelf">The type of actual implementation struct</typeparam>
public interface IDisplaceScaleDistribution<T, TSelf> : IFloatingPointDistribution<T, TSelf>, IRank1Distribution<T, TSelf>
	where T : unmanaged, IBaseNumber<T> where TSelf : struct, IDisplaceScaleDistribution<T, TSelf>
{
	/// <summary>
	/// The displacement value (μ)
	/// </summary>
	public T Displacement { get; }
	/// <summary>
	/// The scaling factor (β)
	/// </summary>
	public T ScaleFactor { get; }

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	bool ICheckValid.IsValid() => this.IsValid() && this.ScaleFactor > T.Zero;
}

/// <summary>
/// The abstract class for one-dimensional distributions which contain the <see cref="ShapeFactor"/> of type <typeparamref name="T"/> and the parameters of <see cref="IDisplaceScaleDistribution{T, TSelf}"/> as the parameters, inherits <see cref="IDisplaceScaleDistribution{T, TSelf}"/>.
/// </summary>
/// <typeparam name="T">Any unmanaged floating point type</typeparam>
/// <typeparam name="TSelf">The type of actual implementation struct</typeparam>
public interface IDisplaceScaleShapeDistribution<T, TSelf> : IDisplaceScaleDistribution<T, TSelf>
	where T : unmanaged, IBaseNumber<T> where TSelf : struct, IDisplaceScaleShapeDistribution<T, TSelf>
{
	/// <summary>
	/// The shape factor (α)
	/// </summary>
	public T ShapeFactor { get; }
}

/// <summary>
/// The abstract class for one-dimensional distributions which contains <see cref="DegreeOfFreedom"/> as the parameter, inherits <see cref="IFloatingPointDistribution{T, TSelf}"/>
/// </summary>
/// <typeparam name="T">Any unmanaged floating point type</typeparam>
/// <typeparam name="TSelf">The type of actual implementation struct</typeparam>
public interface IDegreeOfFreedomDistribution<T, TSelf> : IFloatingPointDistribution<T, TSelf>, IRank1Distribution<T, TSelf>
	where T : unmanaged, IBaseNumber<T> where TSelf : struct, IDegreeOfFreedomDistribution<T, TSelf>
{
	/// <summary>
	/// The degree of freedom
	/// </summary>
	public int DegreeOfFreedom { get; }

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	bool ICheckValid.IsValid() => this.IsValid() && this.DegreeOfFreedom > 0;
}

/// <summary>
/// The abstract class for one-dimensional distributions based on the Bernoulli trial, inherits <see cref="IIntegralDistribution{T, TSelf}"/>.
/// </summary>
/// <typeparam name="T">Any unmanaged integral type</typeparam>
/// <typeparam name="TSelf">The type of actual implementation struct</typeparam>
public interface IBernoulliBasedDistribution<T, TSelf> : IIntegralDistribution<T, TSelf>, IRank1Distribution<T, TSelf>
	where T : unmanaged, IBaseNumber<T> where TSelf : struct, IBernoulliBasedDistribution<T, TSelf>
{
	/// <summary>
	/// The probability of a Bernoulli trial succeeding (p)
	/// </summary>
	public decimal Probability { get; }

	bool ICheckValid.IsValid() => this.IsValid() && this.Probability > 0 && this.Probability < 1;
}
#endregion
