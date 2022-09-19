using Althea.Numerics;
using Althea.Random;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using HostPtr = Althea.Backend.Storage.CpuMemoryPointer;
using Ptr = Althea.Backend.Cuda.CudaMemoryPointer<Althea.Backend.Cuda.GpuId0>;


namespace Althea.Backend.Cuda.Transformer.Tests;

[TestClass()]
public unsafe class ApiTests
{
	private static readonly Api api = new();
	private static readonly Storage.Api mem = new(false);
	private static readonly CSharp.Storage.Api hostMem = new();
	private static readonly Random.Api rand = new();

	[TestMethod()]
	[DataRow(1024L)]
	public void FourierTransformComplexTest(long length)
	{
		mem.Allocate<Ptr>(length * 2 * sizeof(double), out var result);
		var s = new UnitTests.NoDisposePureStorage<Float64, Ptr>(result);
		rand.FillWithRandom<Float64, PureStorage<Float64, Ptr>, UniformDistribution<Float64>>(s, new(1));
		var wrapper = new Array.DenseArrayWrapper<Complex<Float64>, PureStorage<Complex<Float64>, Ptr>>(s.As<Complex<Float64>>(), stackalloc long[1] { length });

		bool success = api.FourierTransform(true, wrapper, wrapper);
		Assert.IsTrue(success);
		mem.Free(result, out _);
	}

	[TestMethod()]
	public void FourierTransformRealToComplexTest()
	{

	}

	[TestMethod()]
	public void FourierTransformComplexToRealTest()
	{

	}
}