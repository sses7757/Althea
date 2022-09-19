using Althea.Random;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using HostPtr = Althea.Backend.Storage.CpuMemoryPointer;
using Ptr = Althea.Backend.Cuda.CudaMemoryPointer<Althea.Backend.Cuda.GpuId0>;


namespace Althea.Backend.Cuda.Random.Tests;

[TestClass()]
public unsafe class ApiTests
{
	private static readonly Storage.Api mem = new(false);
	private static readonly CSharp.Storage.Api hostMem = new();
	private static readonly Api apiSeed = new();
	private static readonly Api apiOther = new(GeneratorType.QuasiDefault, Ordering.QuasiDefault);

	static ApiTests()
	{
		Settings.SetImplementation<Althea.Storage.IAbstractApi>(mem);
	}

	private static (Ptr, HostPtr) FillWithRandomTest<T, TDist>(long length, TDist dist) where T : unmanaged, IBaseNumber<T> where TDist : struct, IRank1Distribution<T, TDist>
	{
		Althea.Random.IAbstractApi api = dist.RandomSeed.HasValue ? apiSeed : apiOther;

		mem.Allocate<Ptr>(length * sizeof(T), out var result);
		hostMem.Allocate<HostPtr>(length * sizeof(T), out var test);
		var s = new UnitTests.NoDisposePureStorage<T, Ptr>(result);

		bool success = api.FillWithRandom<T, PureStorage<T, Ptr>, TDist>(s, dist);
		Assert.IsTrue(success);
		Runtime.DeviceSync();
		mem.MemoryCopy<Float32, Ptr, HostPtr>(result, test, out _);
		Runtime.DeviceSync();

		return (result, test);
	}

	[TestMethod()]
	[DataRow(1024L, 1, true)]
	[DataRow(1024L, 2, true)]
	[DataRow(1024L, 3, true)]
	[DataRow(1024L, 4, true)]
	[DataRow(1024L, 5, true)]
	[DataRow(1024L, 6, true)]
	[DataRow(1024L, 7, true)]
	[DataRow(1024L, 8, true)]
	[DataRow(1024L, 1, false)]
	[DataRow(1024L, 2, false)]
	[DataRow(1024L, 3, false)]
	[DataRow(1024L, 4, false)]
	[DataRow(1024L, 5, false)]
	[DataRow(1024L, 6, false)]
	[DataRow(1024L, 7, false)]
	[DataRow(1024L, 8, false)]
	public void FillWithRandomTest(long length, int @case, bool useSeed)
	{
		long? seed = useSeed ? 1 : null;
		if (@case == 1)
		{
			var (s, ss) = FillWithRandomTest<Float32, UniformDistribution<Float32>>(length, new(5.0f, 5.5f, seed));
			float* p = (float*)ss.Pointer.ToPointer();
			for (int i = 0; i < length; i++)
			{
				Assert.IsTrue(p[i] >= 5.0f && p[i] < 5.5f);
			}
			mem.Free(s, out _);
			hostMem.Free(ss, out _);
		}
		else if (@case == 2)
		{
			var (s, ss) = FillWithRandomTest<Float64, UniformDistribution<Float64>>(length, new(5.0, 5.5, seed));
			double* p = (double*)ss.Pointer.ToPointer();
			for (int i = 0; i < length; i++)
			{
				Assert.IsTrue(p[i] >= 5.0 && p[i] < 5.5);
			}
			mem.Free(s, out _);
			hostMem.Free(ss, out _);
		}
		else if (@case == 3)
		{
			var (s, ss) = FillWithRandomTest<Float32, NormalDistribution<Float32>>(length, new(5.0f, 1.0f, seed));
			float* p = (float*)ss.Pointer.ToPointer();
			for (int i = 0; i < length; i++)
			{
				Assert.IsTrue(p[i] > 5.0f - 5 * 1.0f && p[i] < 5.0f + 5 * 1.0f);
			}
			mem.Free(s, out _);
			hostMem.Free(ss, out _);
		}
		else if (@case == 4)
		{
			var (s, ss) = FillWithRandomTest<Float64, NormalDistribution<Float64>>(length, new(5.0, 0.5, seed));
			double* p = (double*)ss.Pointer.ToPointer();
			for (int i = 0; i < length; i++)
			{
				Assert.IsTrue(p[i] > 5.0 - 5 * 1.0 && p[i] < 5.0 + 5 * 1.0);
			}
			mem.Free(s, out _);
			hostMem.Free(ss, out _);
		}
		else if (@case == 5)
		{
			var (s, ss) = FillWithRandomTest<Float32, LogNormalDistribution<Float32>>(length, new(5.0f, 1.0f, seed));
			////float* p = (float*)ss.Pointer.ToPointer();
			////for (int i = 0; i < length; i++)
			////{
			////	Assert.IsTrue(p[i] > 5.0f - 5 * 1.0f && p[i] < 5.0f + 5 * 1.0f);
			////}
			mem.Free(s, out _);
			hostMem.Free(ss, out _);
		}
		else if (@case == 6)
		{
			var (s, ss) = FillWithRandomTest<Float64, LogNormalDistribution<Float64>>(length, new(5.0, 1.0, seed));
			////double* p = (double*)ss.Pointer.ToPointer();
			////for (int i = 0; i < length; i++)
			////{
			////	Assert.IsTrue(p[i] > 5.0 - 5 * 1.0 && p[i] < 5.0 + 5 * 1.0);
			////}
			mem.Free(s, out _);
			hostMem.Free(ss, out _);
		}
		else if (@case == 7)
		{
			var (s, ss) = FillWithRandomTest<SignedInt32, RandomBitsDistribution<SignedInt32>>(length, new(seed));
			mem.Free(s, out _);
			hostMem.Free(ss, out _);
		}
		else if (@case == 8)
		{
			var (s, ss) = FillWithRandomTest<SignedInt32, PoissonDistribution<SignedInt32>>(length, new(2, seed));
			mem.Free(s, out _);
			hostMem.Free(ss, out _);
		}
	}
}