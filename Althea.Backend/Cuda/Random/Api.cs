using System.Dynamic;
using System.Runtime.CompilerServices;

using Althea.Backend.Cuda.LinearAlgebra.Dense;
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
	private GeneratorType type;
	private Ordering order;
	private IntPtr generator;
	private bool canSeed;

	/// <summary>
	/// Get whether the current generator can set seed or not
	/// </summary>
	public bool CanHaveSeed => this.canSeed;

	/// <summary>
	/// Get or set the generator used by this instance API
	/// </summary>
	public (GeneratorType type, Ordering order) Generator
	{
		get => (this.type, this.order);
		set
		{
			var (type, order) = value;
			lock (this)
			{
				NativeMethods.curandCreateGenerator(out var generator, type).Check();
				NativeMethods.curandSetGeneratorOrdering(generator, order).Check();
				NativeMethods.curandDestroyGenerator(this.generator);
				this.generator = generator;
				this.canSeed = (type is >= GeneratorType.PseudoDefault and < GeneratorType.QuasiDefault) && order == Ordering.Pseudoeseded;
				this.type = type; this.order = order;
			}
		}
	}

	/// <summary>
	/// The default constructor of <see cref="Api"/>
	/// </summary>
	public Api() : this(GeneratorType.PseudoDefault, Ordering.Pseudoeseded) { }

	/// <summary>
	/// The full constructor of <see cref="Api"/>
	/// </summary>
	public Api(GeneratorType type = GeneratorType.PseudoDefault, Ordering order = Ordering.Pseudoeseded)
	{
		this.Generator = (type, order);
		this.BindedDeviceID = Runtime.CurrentDeviceID;
		this.Properties = new DynamicProperties(this);
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

	#region dynamic
	/// <inheritdoc/>
	public dynamic Properties { get; }

	/// <inheritdoc/>
	protected sealed class DynamicProperties : Althea.Random.IAbstractApi.DynamicProperties
	{
		internal DynamicProperties(Api @this) : base(@this) { }

		/// <inheritdoc/>
		public override bool TryGetMember(GetMemberBinder binder, out object? result)
		{
			if (binder.Name == nameof(BindedDeviceID) && binder.ReturnType == typeof(int))
			{
				result = (this.api as Api)!.BindedDeviceID;
				return true;
			}
			if (binder.Name == nameof(Generator) && binder.ReturnType == typeof((GeneratorType, Ordering)))
			{
				result = (this.api as Api)!.Generator;
				return true;
			}
			result = null;
			return false;
		}

		/// <inheritdoc/>
		public override bool TrySetMember(SetMemberBinder binder, object? value)
		{
			if (binder.Name == nameof(Generator) && value is ValueTuple<GeneratorType, Ordering> g)
			{
				(this.api as Api)!.Generator = g;
				return true;
			}
			return false;
		}
	}
	#endregion

	#region methods
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe bool Generate<T, TDist>(T* s, long length, in TDist dist) where T : unmanaged, IBaseNumber<T> where TDist : struct, IRank1Distribution<T, TDist>
	{
		if (dist.RandomSeed.HasValue && !this.CanHaveSeed)
			return false;
		if (dist.RandomSeed.HasValue)
			NativeMethods.curandSetPseudoRandomGeneratorSeed(this.generator, (ulong)dist.RandomSeed.Value).Check();
		if (dist is UniformDistribution<T> uniform)
		{
			if (typeof(T) == typeof(Float32))
				NativeMethods.curandGenerateUniform(this.generator, s, length).Check();
			else if (typeof(T) == typeof(Float64))
				NativeMethods.curandGenerateUniformDouble(this.generator, s, length).Check();
			////else if (typeof(T) == typeof(SignedInt32))
			////	NativeMethods.curandGenerate(this.generator, s, length * sizeof(T) / sizeof(int));
			else
				return false;
			T scale = uniform.UpperBound - uniform.LowerBound;
			T offset = uniform.LowerBound;
			if (scale != T.One && !CustomNativeMethods.vecBinaryScalar(T.Type, Althea.LinearAlgebra.BinaryScalarOperation.Multiply, &scale, length, s, 1, s, 1).Check())
				return false;
			if (uniform.LowerBound != T.Zero && !CustomNativeMethods.vecBinaryScalar(T.Type, Althea.LinearAlgebra.BinaryScalarOperation.Add, &offset, length, s, 1, s, 1).Check())
				return false;
		}
		else if (dist is NormalDistribution<T> normal)
		{
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
			if (sizeof(T) == sizeof(int))
				NativeMethods.curandGenerate(this.generator, s, length).Check();
			else if (sizeof(T) == sizeof(long))
				NativeMethods.curandGenerateLongLong(this.generator, s, length).Check();
			else if (sizeof(T) % sizeof(int) == 0)
				NativeMethods.curandGenerate(this.generator, s, length * sizeof(T) / sizeof(int)).Check();
			else
				return false;
		}
		else if (dist is PoissonDistribution<T> poisson)
		{
			if (typeof(T) != typeof(SignedInt32))
				return false;
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
