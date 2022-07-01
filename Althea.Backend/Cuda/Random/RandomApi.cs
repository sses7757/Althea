using System;
using System.Runtime.CompilerServices;

using Althea.Backend.Storage;
using Althea.Numerics;
using Althea.Random;
using Althea.Backend.Random;


#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释
namespace Althea.Backend.Cuda.Random
{
	/// <summary>
	/// The CUDA back-end of the <see cref="AbstractApi"/> that supports filling GPU arrays with one-dimensional uniform, normal, log normal and Poisson distributions.
	/// </summary>
	/// <remarks>Other distributions can be easily supported by utilizing the result of uniform distributions.</remarks>
	public class RandomApi : AbstractApi
	{
		#region basic
		private readonly IntPtr generator;

		public RandomApi()
		{
			NativeMethods.curandCreateGenerator(out this.generator, GeneratorType.PseudoDefault).Check();
			NativeMethods.curandSetGeneratorOrdering(generator, Ordering.Pseudoeseded).Check();
		}

		protected override void Dispose(bool disposeManaged)
		{
			NativeMethods.curandDestroyGenerator(this.generator);
		}
		#endregion

		#region support
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static IntPtr GetPointer<T>(Storage<T> s) where T : unmanaged, IBaseNumber<T>
		{
			if (s is null || !s.IsValid() || s.Count != 1 || !Supported(s.LocationDescription))
				return default;
			if (s[0].Pointer is not IMemoryPointer mp)
				return default;
			if (mp.Pointer == default)
				return default;
			return (IntPtr)(mp.Pointer.ToInt64() + s[0].OffsetInBytes);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool Supported(CombinationOfLocations location)
		{
			if (location.Count != 1)
				return false;
			var loc = location[0];
			return loc.Type == LocationType.GpuRam && loc.Detail == CudaRuntime.CurrentDeviceID;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedUnary(CombinationOfLocations location1) => Supported(location1);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedBinary(CombinationOfLocations location1, CombinationOfLocations location2) => Supported(location1) && Supported(location2);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedTrinary(CombinationOfLocations location1, CombinationOfLocations location2, CombinationOfLocations location3) => Supported(location1) && Supported(location2) && Supported(location3);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedNary(ReadOnlySpan<CombinationOfLocations> locations)
		{
			for (int i = 0; i < locations.Length; i++)
			{
				if (!Supported(locations[i]))
					return false;
			}
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe bool Check<T>(Storage<T> storage!!, IRandomDistribution distribution!!, out IntPtr pointer, out long length, out UniformDistribution<T>? uniform, out RandomBitsDistribution<T>? bits, out NormalDistribution<T>? normal, out LogNormalDistribution<T>? logNormal, out PoissonDistribution<T>? poisson) where T : unmanaged, IBaseNumber<T>
		{
			pointer = default; length = 0;
			if (distribution.Count != 1)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(distribution));
			uniform = distribution as UniformDistribution<T>;
			bits = distribution as RandomBitsDistribution<T>;
			normal = distribution as NormalDistribution<T>;
			logNormal = distribution as LogNormalDistribution<T>;
			poisson = distribution as PoissonDistribution<T>;
			var ss = storage[0];
			if (storage.Count != 1 || ss.Pointer is not IMemoryPointer p)
				return false; // not support
			if (Const<T>.IsComplex)
				return false; // not support

			pointer = (IntPtr)(p.Pointer.ToInt64() + ss.OffsetInBytes); length = (int)ss.LengthInBytes;
			if (uniform is not null || normal is not null || logNormal is not null)
			{
				if (typeof(T) != typeof(float) && typeof(T) != typeof(double))
					return false;
			}
			if (bits is not null)
			{
				if (sizeof(T) != sizeof(int) && sizeof(T) != sizeof(long))
					return false;
			}
			if (poisson is not null)
			{
				if (typeof(T) != typeof(uint) && !(typeof(T) == typeof(int) && poisson.Lambda < int.MaxValue / 2))
					return false;
			}
			if (logNormal is not null)
			{	// extra check
				if (!logNormal.Displacement.IsZero() || !logNormal.ScaleFactor.IsOne())
					return false;
			}
			return true;
		}
		#endregion

		#region methods
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private unsafe void Generate<T>(Storage<T> s, IntPtr p, long length, UniformDistribution<T>? uniform, RandomBitsDistribution<T>? bits, NormalDistribution<T>? normal, LogNormalDistribution<T>? logNormal, PoissonDistribution<T>? poisson) where T : unmanaged, IBaseNumber<T>
		{
			if (uniform is not null)
			{
				if (uniform.RandomSeed.HasValue)
					NativeMethods.curandSetPseudoRandomGeneratorSeed(this.generator, (ulong)uniform.RandomSeed.Value).Check();
				if (typeof(T) == typeof(float))
					NativeMethods.curandGenerateUniform(this.generator, p, length).Check();
				else
					NativeMethods.curandGenerateUniformDouble(this.generator, p, length).Check();
				T scale = uniform.UpperBound.NativeSub(uniform.LowerBound);
				if (!scale.IsOne())
					Althea.LinearAlgebra.Dense.AbstractApi.Scale(s, 1, scale);
				if (!uniform.LowerBound.IsZero())
					Althea.LinearAlgebra.Dense.AbstractApi.PointWiseAddScalar(s, 1, uniform.LowerBound);
			}
			if (normal is not null)
			{
				if (normal.RandomSeed.HasValue)
					NativeMethods.curandSetPseudoRandomGeneratorSeed(this.generator, (ulong)normal.RandomSeed.Value).Check();
				T mean = normal.Mean, stdDev = normal.StandardDeviation;
				if (typeof(T) == typeof(float))
					NativeMethods.curandGenerateNormal(this.generator, p, length, *(float*)&mean, *(float*)&stdDev).Check();
				else
					NativeMethods.curandGenerateNormalDouble(this.generator, p, length, *(double*)&mean, *(double*)&stdDev).Check();
			}
			if (logNormal is not null)
			{
				if (logNormal.RandomSeed.HasValue)
					NativeMethods.curandSetPseudoRandomGeneratorSeed(this.generator, (ulong)logNormal.RandomSeed.Value).Check();
				T mean = logNormal.Mean, stdDev = logNormal.StandardDeviation;
				if (typeof(T) == typeof(float))
					NativeMethods.curandGenerateLogNormal(this.generator, p, length, *(float*)&mean, *(float*)&stdDev).Check();
				else
					NativeMethods.curandGenerateLogNormalDouble(this.generator, p, length, *(double*)&mean, *(double*)&stdDev).Check();
			}
			if (bits is not null)
			{
				if (bits.RandomSeed.HasValue)
					NativeMethods.curandSetPseudoRandomGeneratorSeed(this.generator, (ulong)bits.RandomSeed.Value).Check();
				if (sizeof(T) == sizeof(int))
					NativeMethods.curandGenerate(this.generator, p, length).Check();
				else
					NativeMethods.curandGenerateLongLong(this.generator, p, length).Check();
			}
			if (poisson is not null)
			{
				if (poisson.RandomSeed.HasValue)
					NativeMethods.curandSetPseudoRandomGeneratorSeed(this.generator, (ulong)poisson.RandomSeed.Value).Check();
				NativeMethods.curandGeneratePoisson(this.generator, p, length, poisson.Lambda).Check();
			}
		}

		protected override bool FillWithRandom_<T>(Storage<T> storage, IRandomDistribution distribution)
		{
			if (!Check(storage, distribution, out var p, out long length, out var dist1, out var dist2, out var dist3, out var dist4, out var dist5))
				return false; // not support
			this.Generate(storage, p, length, dist1, dist2, dist3, dist4, dist5);
			return true;
		}

		protected override bool FillWithRandom_<T1, T2>(Storage<T1> storage1, Storage<T2> storage2, IRandomDistribution distribution!!)
		{
			if (distribution.Count != 2)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(distribution));
			if (distribution is not SimpleJointRandomDistribution d)
				return false; // not support
			if (!Check(storage1, d[0], out var p1, out long length1, out var dist11, out var dist12, out var dist13, out var dist14, out var dist15))
				return false; // not support
			if (!Check(storage2, d[1], out var p2, out long length2, out var dist21, out var dist22, out var dist23, out var dist24, out var dist25))
				return false; // not support

			this.Generate(storage1, p1, length1, dist11, dist12, dist13, dist14, dist15);
			this.Generate(storage2, p2, length2, dist21, dist22, dist23, dist24, dist25);
			return true;
		}
		protected override bool FillWithRandom_<T1, T2, T3>(Storage<T1> storage1, Storage<T2> storage2, Storage<T3> storage3, IRandomDistribution distribution!!)
		{
			if (distribution.Count != 3)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(distribution));
			if (distribution is not SimpleJointRandomDistribution d)
				return false; // not support
			if (!Check(storage1, d[0], out var p1, out long length1, out var dist11, out var dist12, out var dist13, out var dist14, out var dist15))
				return false; // not support
			if (!Check(storage2, d[1], out var p2, out long length2, out var dist21, out var dist22, out var dist23, out var dist24, out var dist25))
				return false; // not support
			if (!Check(storage3, d[1], out var p3, out long length3, out var dist31, out var dist32, out var dist33, out var dist34, out var dist35))
				return false; // not support

			this.Generate(storage1, p1, length1, dist11, dist12, dist13, dist14, dist15);
			this.Generate(storage2, p2, length2, dist21, dist22, dist23, dist24, dist25);
			this.Generate(storage3, p3, length3, dist31, dist32, dist33, dist34, dist35);
			return true;
		}

		protected override bool FillWithRandom_(IRandomDistribution distribution, params IStorage[] storages) => false;
		#endregion
	}
}
