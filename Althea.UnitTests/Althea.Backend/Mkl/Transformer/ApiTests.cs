using Microsoft.VisualStudio.TestTools.UnitTesting;

using Althea.Array;
using Althea.UnitTests;

using MemF64 = Althea.Storage.PureStorage<Althea.Numerics.Float64, Althea.Backend.Storage.CpuMemoryPointer>;
using MemC64 = Althea.Storage.PureStorage<Althea.Numerics.Complex<Althea.Numerics.Float64>, Althea.Backend.Storage.CpuMemoryPointer>;


namespace Althea.Backend.Mkl.Transformer.Tests;

[TestClass()]
public class ApiTests
{
	private static readonly Api api = new();

	// TODO: always SEH from MKL DFTI
	[TestMethod()]
	[DataRow(true)]
	[DataRow(false)]
	public void FourierTransformComplexToComplexTest(bool forward)
	{
		using var s1 = CpuHelpers.GenerateFloatData(-10, 10);
		using var s2 = CpuHelpers.GenerateFloatData(-10, 10);
		var ss1 = s1.As<Complex<Float64>>();
		var ss2 = s2.As<Complex<Float64>>();
		var w1 = new DenseArrayWrapper<Complex<Float64>, MemC64>(ss1, stackalloc long[] { 100, 8 });
		var w2 = new DenseArrayWrapper<Complex<Float64>, MemC64>(ss2, stackalloc long[] { 100, 8 });

		bool success = api.FourierTransform(forward, w1, w2);
		Assert.IsTrue(success);
	}

	[TestMethod()]
	public void FourierTransformComplexToRealTest()
	{

	}

	[TestMethod()]
	public void FourierTransformRealToComplexTest()
	{

	}
}