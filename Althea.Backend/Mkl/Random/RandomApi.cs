using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Althea.Backend.Storage;
using Althea.NativeTypes;
using Althea.Random;


#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释
namespace Althea.Backend.Mkl.Random
{
	/// <summary>
	/// The MKL back-end of the <see cref="AbstractApi"/> that supports filling CPU arrays with a variety kinds of distributions.
	/// </summary>
	/// <remarks>Since using the same MKL VSL stream results to thread blockage, this class utilizes a <see cref="ThreadLocal{T}"/> to make sure that multi-threading in C# works properly.</remarks>
	public class RandomApi : AbstractApi
	{
		#region basic
		private readonly ThreadLocal<(IntPtr stream, uint seed)> generator;

		public RandomApi()
		{
			this.generator = new ThreadLocal<(IntPtr, uint)>(InitializeGenerator, trackAllValues: true);
		}

		private static (IntPtr, uint) InitializeGenerator()
		{
			NativeMethods.vslNewStream(out var stream, GeneratorType.SFMT19937, 0).Check();
			return (stream, 0);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private IntPtr ChangeGeneratorSeed(long? newSeed)
		{
			uint s;
			if (!newSeed.HasValue || (s = (uint)newSeed.Value) == this.generator.Value.seed)
				return this.generator.Value.stream;
			NativeMethods.vslNewStream(out var stream, GeneratorType.SFMT19937, s).Check();
			this.generator.Value = (stream, s);
			return stream;
		}

		protected override void Dispose(bool disposeManaged)
		{
			foreach (var (stream, _) in this.generator.Values)
			{
				NativeMethods.vslDeleteStream(in stream);
			}
			this.generator.Dispose();
		}
		#endregion

		#region support
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static IntPtr GetPointer<T>(Storage<T> s) where T : unmanaged
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
			return loc.Type == LocationType.CpuRam;
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
		#endregion

		#region get distribution
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe bool Check<T>(Storage<T> storage, IRandomDistribution distribution, out IntPtr pointer, out long length,
			out UniformDistribution<T>? uniform,
			out RandomBitsDistribution<T>? bits,
			out NormalDistribution<T>? normal,
			out PoissonDistribution<T>? poisson) where T : unmanaged
		{
			pointer = default; length = 0;
			if (storage is null)
				throw new ArgumentNullException(nameof(storage));
			if (distribution is null)
				throw new ArgumentNullException(nameof(distribution));
			if (distribution.Count != 1)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(distribution));
			uniform = distribution as UniformDistribution<T>;
			bits = distribution as RandomBitsDistribution<T>;
			normal = distribution as NormalDistribution<T>;
			poisson = distribution as PoissonDistribution<T>;
			var ss = storage[0];
			if (storage.Count != 1 || ss.Pointer is not IMemoryPointer p)
				return false; // not support
			if (Const<T>.IsComplex)
				return false; // not support

			pointer = (IntPtr)(p.Pointer.ToInt64() + ss.OffsetInBytes); length = (int)ss.LengthInBytes;
			if (uniform is not null || normal is not null)
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
			return true;
		}
		#endregion
	}
}
