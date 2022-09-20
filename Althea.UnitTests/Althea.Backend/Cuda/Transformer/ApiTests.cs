using Althea.Random;

using Microsoft.VisualStudio.TestTools.UnitTesting;

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
	[DataRow(1024L, 512L)]
	public void FourierTransformComplexTest(long rows, long cols)
	{
		mem.Allocate<Ptr>(rows * cols * 2 * sizeof(double), out var result);
		var s = new UnitTests.NoDisposePureStorage<Float64, Ptr>(result);
		rand.FillWithRandom<Float64, PureStorage<Float64, Ptr>, UniformDistribution<Float64>>(s, new(1));
		var wrapper = new Array.DenseArrayWrapper<Complex<Float64>, PureStorage<Complex<Float64>, Ptr>>(s.As<Complex<Float64>>(), stackalloc long[] { rows, cols });

		bool success = api.FourierTransform(true, wrapper, wrapper);
		Assert.IsTrue(success);
		Runtime.DeviceSync();

		mem.Free(result, out _);
	}

	[TestMethod()]
	[DataRow(1024L, 512L)]
	public void FourierTransformRealToComplexTest(long rows, long cols)
	{
		mem.Allocate<Ptr>(rows * cols * sizeof(double), out var src);
		mem.Allocate<Ptr>(rows * cols * 2 * sizeof(double), out var dst);
		var s = new UnitTests.NoDisposePureStorage<Float64, Ptr>(src);
		var d = new UnitTests.NoDisposePureStorage<Float64, Ptr>(dst);
		rand.FillWithRandom<Float64, PureStorage<Float64, Ptr>, UniformDistribution<Float64>>(s, new(1));
		var wrapperDst = new Array.DenseArrayWrapper<Complex<Float64>, PureStorage<Complex<Float64>, Ptr>>(d.As<Complex<Float64>>(), stackalloc long[] { rows, cols });
		var wrapperSrc = new Array.DenseArrayWrapper<Float64, PureStorage<Float64, Ptr>>(s, stackalloc long[] { rows, cols });

		bool success = api.FourierTransform(wrapperSrc, wrapperDst);
		Assert.IsTrue(success);
		Runtime.DeviceSync();

		mem.Free(src, out _);
		mem.Free(dst, out _);
	}

	[TestMethod()]
	[DataRow(1024L, 512L)]
	public void FourierTransformComplexToRealTest(long rows, long cols)
	{
		mem.Allocate<Ptr>(rows * cols * sizeof(double), out var dst);
		mem.Allocate<Ptr>(rows * cols * 2 * sizeof(double), out var src);
		var s = new UnitTests.NoDisposePureStorage<Float64, Ptr>(src);
		var d = new UnitTests.NoDisposePureStorage<Float64, Ptr>(dst);
		rand.FillWithRandom<Float64, PureStorage<Float64, Ptr>, UniformDistribution<Float64>>(s, new(1));
		var wrapperSrc = new Array.DenseArrayWrapper<Complex<Float64>, PureStorage<Complex<Float64>, Ptr>>(s.As<Complex<Float64>>(), stackalloc long[] { rows, cols });
		var wrapperDst = new Array.DenseArrayWrapper<Float64, PureStorage<Float64, Ptr>>(d, stackalloc long[] { rows, cols });

		bool success = api.FourierTransform(wrapperSrc, wrapperDst);
		Assert.IsTrue(success);
		Runtime.DeviceSync();

		mem.Free(src, out _);
		mem.Free(dst, out _);
	}
}