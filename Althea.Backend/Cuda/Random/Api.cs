using System.Runtime.CompilerServices;

using Althea.Random;

using static Althea.Backend.Cuda.MemoryPointerChecker;


namespace Althea.Backend.Cuda.Random;

/// <summary>
/// The CUDA back-end of the <see cref="Althea.Random.IAbstractApi"/> that supports filling GPU arrays with one-dimensional uniform, normal, log normal and Poisson distributions.
/// </summary>
/// <remarks>Other distributions can be easily supported by utilizing the result of uniform distributions.</remarks>
public class Api : IBindedDevice, Althea.Random.IAbstractApi
{
	#region basic
	private readonly IntPtr generator;
	private readonly bool canHaveSeed;

	/// <summary>
	/// The default constructor of <see cref="Api"/>
	/// </summary>
	public Api() : this(GeneratorType.PseudoDefault) { }

	/// <summary>
	/// The full constructor of <see cref="Api"/>
	/// </summary>
	public Api(GeneratorType type = GeneratorType.PseudoDefault, Ordering order = Ordering.Pseudoeseded)
	{
		NativeMethods.curandCreateGenerator(out this.generator, type).Check();
		NativeMethods.curandSetGeneratorOrdering(generator, order).Check();
		this.canHaveSeed = type is >= GeneratorType.PseudoDefault and <= GeneratorType.QuasiDefault && order == Ordering.Pseudoeseded;
		this.BindedDeviceID = Runtime.CurrentDeviceID;
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		NativeMethods.curandDestroyGenerator(this.generator);
		this.Disposed = true;
		GC.SuppressFinalize(this);
	}

	/// <inheritdoc/>
	public bool Disposed { get; protected set; } = false;

	/// <inheritdoc/>
	public int BindedDeviceID { get; }
	#endregion

	#region methods
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe bool Generate<T, TDist>(T* s, long length, in TDist dist) where T : unmanaged, IBaseNumber<T> where TDist : struct, IRank1Distribution<T, TDist>
	{
		if (dist is UniformDistribution<T> uniform)
		{
			if (uniform.RandomSeed.HasValue)
				NativeMethods.curandSetPseudoRandomGeneratorSeed(this.generator, (ulong)uniform.RandomSeed.Value).Check();
			if (typeof(T) == typeof(Float32))
				NativeMethods.curandGenerateUniform(this.generator, s, length).Check();
			else if (typeof(T) == typeof(Float64))
				NativeMethods.curandGenerateUniformDouble(this.generator, s, length).Check();
			else if (T.Type.IsInteger())
				NativeMethods.curandGenerate(this.generator, s, length * sizeof(T) / sizeof(int));
			else
				return false;
			T scale = uniform.UpperBound - uniform.LowerBound;
			T offset = uniform.LowerBound;
			if (scale != T.One)
			{
				if (T.Type.IsInteger())
					return false;
				else if (LinearAlgebra.Dense.CustomNativeMethods.vecBinaryScalar(T.Type, Althea.LinearAlgebra.BinaryScalarOperation.Multiply, &scale, length, s, 1, s, 1) == LinearAlgebra.Dense.CustomStatus.NotSupported)
					return false;
			}
			if (uniform.LowerBound != T.Zero && LinearAlgebra.Dense.CustomNativeMethods.vecBinaryScalar(T.Type, Althea.LinearAlgebra.BinaryScalarOperation.Add, &offset, length, s, 1, s, 1) == LinearAlgebra.Dense.CustomStatus.NotSupported)
				return false;
		}
		else if (dist is NormalDistribution<T> normal)
		{
			if (normal.RandomSeed.HasValue)
				NativeMethods.curandSetPseudoRandomGeneratorSeed(this.generator, (ulong)normal.RandomSeed.Value).Check();
			T mean = normal.Displacement, stdDev = normal.ScaleFactor;
			if (typeof(T) == typeof(Float32))
				NativeMethods.curandGenerateNormal(this.generator, s, length, *(float*)&mean, *(float*)&stdDev).Check();
			else if (typeof(T) == typeof(Float64))
				NativeMethods.curandGenerateNormalDouble(this.generator, s, length, *(double*)&mean, *(double*)&stdDev).Check();
			else
				return false;
		}
		else if (dist is LogNormalDistribution<T> logNormal)
		{
			if (logNormal.RandomSeed.HasValue)
				NativeMethods.curandSetPseudoRandomGeneratorSeed(this.generator, (ulong)logNormal.RandomSeed.Value).Check();
			T mean = logNormal.Displacement, stdDev = logNormal.ScaleFactor;
			if (typeof(T) == typeof(Float32))
				NativeMethods.curandGenerateLogNormal(this.generator, s, length, *(float*)&mean, *(float*)&stdDev).Check();
			else if (typeof(T) == typeof(Float64))
				NativeMethods.curandGenerateLogNormalDouble(this.generator, s, length, *(double*)&mean, *(double*)&stdDev).Check();
			else
				return false;
		}
		else if (dist is RandomBitsDistribution<T> bits)
		{
			if (bits.RandomSeed.HasValue)
				NativeMethods.curandSetPseudoRandomGeneratorSeed(this.generator, (ulong)bits.RandomSeed.Value).Check();
			if (sizeof(T) == sizeof(int))
				NativeMethods.curandGenerate(this.generator, s, length).Check();
			else if (sizeof(T) == sizeof(long))
				NativeMethods.curandGenerateLongLong(this.generator, s, length).Check();
			else
				return false;
		}
		else if (dist is PoissonDistribution<T> poisson)
		{
			if (typeof(T) != typeof(SignedInt32))
				return false;
			if (poisson.RandomSeed.HasValue)
				NativeMethods.curandSetPseudoRandomGeneratorSeed(this.generator, (ulong)poisson.RandomSeed.Value).Check();
			NativeMethods.curandGeneratePoisson(this.generator, s, length, (double)poisson.Lambda).Check();
		}
		else
			return false;
		return true;
	}

	/// <inheritdoc/>
	public virtual unsafe bool FillWithRandom<T, TS, TDist>(TS storage, in TDist distribution) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS> where TDist : struct, IRank1Distribution<T, TDist>
	{
		if (!GetPointer(this, storage, out T* ptr, out var n))
			return false;
		return Generate(ptr, n, in distribution);
	}

	bool Althea.Random.IAbstractApi.FillWithRandom<T1, T2, TS1, TS2, TDist>(TS1 storage1, TS2 storage2, in TDist distribution) => false;
	bool Althea.Random.IAbstractApi.FillWithRandom<T1, T2, T3, TS1, TS2, TS3, TDist>(TS1 storage1, TS2 storage2, TS3 storage3, in TDist distribution) => false;
	bool Althea.Random.IAbstractApi.FillWithRandom<TDist>(ReadOnlySpan<IStorage> storages, in TDist distribution) => false;
	#endregion
}
