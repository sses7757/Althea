using System.Collections.Generic;

using Althea.Helpers;
using Althea.Random;
using Althea.UnitTests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MemF64 = Althea.Storage.PureStorage<Althea.Numerics.Float64, Althea.Backend.Storage.CpuMemoryPointer>;


namespace Althea.Backend.Mkl.Random.Tests;
[TestClass()]
public unsafe class ApiTests
{
	private static readonly Api api = new();

	private static void Fill1DTest<T, TDist>(in TDist dist) where T : unmanaged, IBaseNumber<T> where TDist : struct, IRank1Distribution<T, TDist>
	{
		using var s = CpuHelpers.GenerateFloatData(-10, 10);
		var ss = s.As<T>();

		bool success = api.FillWithRandom<T, PureStorage<T, CpuMemoryPointer>, TDist>(ss, dist);
		Assert.IsTrue(success);
	}

	[TestMethod()]
	[DataRow(0, 0, true)]
	[DataRow(1, 0, true)]
	[DataRow(2, 0, true)]
	[DataRow(3, 0, true)]
	[DataRow(4, 0, true)]
	[DataRow(5, 0, true)]
	[DataRow(6, 0, true)]
	[DataRow(7, 0, true)]
	[DataRow(8, 0, true)]
	[DataRow(9, 0, true)]
	[DataRow(10, 0, true)]
	[DataRow(11, 0, true)]
	[DataRow(12, 0, true)]
	[DataRow(13, 0, true)]
	[DataRow(14, 0, true)]
	[DataRow(15, 0, true)]
	[DataRow(16, 0, true)]
	[DataRow(17, 0, true)]
	[DataRow(18, 0, true)]
	[DataRow(1, 2, true)]
	[DataRow(1, 1, true)]
	[DataRow(2, 1, true)]
	[DataRow(3, 1, true)]
	[DataRow(4, 1, true)]
	[DataRow(5, 1, true)]
	[DataRow(6, 1, true)]
	[DataRow(7, 1, true)]
	[DataRow(8, 1, true)]
	[DataRow(9, 1, true)]
	[DataRow(10, 1, true)]
	[DataRow(11, 1, true)]
	[DataRow(12, 1, true)]
	[DataRow(13, 1, true)]
	public void FillWithRandomTest(int dist, int type, bool useSeed)
	{
		long? seed = useSeed ? 1 : null;
		if (dist == 0)
		{
			if (type == 0)
				Fill1DTest<SignedInt32, RandomBitsDistribution<SignedInt32>>(new(seed));
		}
		else if (dist == 1)
		{
			if (type == 0)
				Fill1DTest<Float32, UniformDistribution<Float32>>(new(5.0f, 5.5f, seed));
			else if (type == 1)
				Fill1DTest<Float64, UniformDistribution<Float64>>(new(5.0, 5.5, seed));
			else if (type == 2)
				Fill1DTest<SignedInt32, UniformDistribution<SignedInt32>>(new(5, 10, seed));
		}
		else if (dist == 2)
		{
			if (type == 0)
				Fill1DTest<Float32, BetaDistribution<Float32>>(new(5.0f, 1.5f, 2.0f, 2.0f, seed));
			else if (type == 1)
				Fill1DTest<Float64, BetaDistribution<Float64>>(new(5.0, 1.5, 2.0, 2.0, seed));
		}
		else if (dist == 3)
		{
			if (type == 0)
				Fill1DTest<Float32, CauchyDistribution<Float32>>(new(5.0f, 1.5f, seed));
			else if (type == 1)
				Fill1DTest<Float64, CauchyDistribution<Float64>>(new(5.0, 1.5, seed));
		}
		else if (dist == 4)
		{
			if (type == 0)
				Fill1DTest<Float32, ChiSquareDistribution<Float32>>(new(10, seed));
			else if (type == 1)
				Fill1DTest<Float64, ChiSquareDistribution<Float64>>(new(10, seed));
		}
		else if (dist == 5)
		{
			if (type == 0)
				Fill1DTest<Float32, ExponentialDistribution<Float32>>(new(5.0f, 2.0f, seed));
			else if (type == 1)
				Fill1DTest<Float64, ExponentialDistribution<Float64>>(new(5.0, 2.0, seed));
		}
		else if (dist == 6)
		{
			if (type == 0)
				Fill1DTest<Float32, GammaDistribution<Float32>>(new(5.0f, 2.0f, 1.5f, seed));
			else if (type == 1)
				Fill1DTest<Float64, GammaDistribution<Float64>>(new(5.0, 2.0, 1.5, seed));
		}
		else if (dist == 7)
		{
			if (type == 0)
				Fill1DTest<Float32, GumbelDistribution<Float32>>(new(5.0f, 2.0f, seed));
			else if (type == 1)
				Fill1DTest<Float64, GumbelDistribution<Float64>>(new(5.0, 2.0, seed));
		}
		else if (dist == 8)
		{
			if (type == 0)
				Fill1DTest<Float32, LaplaceDistribution<Float32>>(new(5.0f, 2.0f, seed));
			else if (type == 1)
				Fill1DTest<Float64, LaplaceDistribution<Float64>>(new(5.0, 2.0, seed));
		}
		else if (dist == 9)
		{
			if (type == 0)
				Fill1DTest<Float32, LogNormalDistribution<Float32>>(new(5.0f, 2.0f, seed));
			else if (type == 1)
				Fill1DTest<Float64, LogNormalDistribution<Float64>>(new(5.0, 2.0, seed));
		}
		else if (dist == 10)
		{
			if (type == 0)
				Fill1DTest<SignedInt32, NegativeBinomialDistribution<SignedInt32>>(new(0.7m, 10, seed));
		}
		else if (dist == 11)
		{
			if (type == 0)
				Fill1DTest<Float32, NormalDistribution<Float32>>(new(5.0f, 2.0f, seed));
			else if (type == 1)
				Fill1DTest<Float64, NormalDistribution<Float64>>(new(5.0, 2.0, seed));
		}
		else if (dist == 12)
		{
			if (type == 0)
				Fill1DTest<Float32, RayleighDistribution<Float32>>(new(5.0f, 2.0f, seed));
			else if (type == 1)
				Fill1DTest<Float64, RayleighDistribution<Float64>>(new(5.0, 2.0, seed));
		}
		else if (dist == 13)
		{
			if (type == 0)
				Fill1DTest<Float32, WeibullDistribution<Float32>>(new(5.0f, 2.0f, 1.5f, seed));
			else if (type == 1)
				Fill1DTest<Float64, WeibullDistribution<Float64>>(new(5.0, 2.0, 1.5, seed));
		}
		else if (dist == 14)
		{
			if (type == 0)
				Fill1DTest<SignedInt32, BernoulliDistribution<SignedInt32>>(new(0.7m, seed));
		}
		else if (dist == 15)
		{
			if (type == 0)
				Fill1DTest<SignedInt32, BinomialDistribution<SignedInt32>>(new(0.7m, 10, seed));
		}
		else if (dist == 16)
		{
			if (type == 0)
				Fill1DTest<SignedInt32, GeometricDistribution<SignedInt32>>(new(0.7m, seed));
		}
		else if (dist == 17)
		{
			if (type == 0)
				Fill1DTest<SignedInt32, HypergeometricDistribution<SignedInt32>>(new(100, 20, 10, seed));
		}
		else if (dist == 18)
		{
			if (type == 0)
				Fill1DTest<SignedInt32, PoissonDistribution<SignedInt32>>(new(5m, seed));
		}
	}

	////[TestMethod()]
	////public void FillWithRandom2DTest()
	////{
	////	using var s1 = CpuHelpers.GenerateFloatData(-10, 10);
	////	using var s2 = CpuHelpers.GenerateFloatData(-10, 10);

	////	bool success = api.FillWithRandom<Float64, Float64, MemF64, MemF64, BinormalDistribution<Float64>>(s1, s2, new(5.0, 1.0, 2.0, 3.0, 0.5));
	////	Assert.IsTrue(success);
	////}

	////[TestMethod()]
	////public void FillWithRandomNDTest()
	////{
	////	using var s1 = CpuHelpers.GenerateFloatData(-10, 10);
	////	using var s2 = CpuHelpers.GenerateFloatData(-10, 10);
	////	using var s3 = CpuHelpers.GenerateFloatData(-10, 10);
	////	Span<IStorage> ss = stackalloc IntPtr[3].AsClassType<IStorage>();
	////	ss.SetValue(s1, s2, s3);

	////	bool success = api.FillWithRandom<MultiNormalDistribution<Float64>>(ss, new(stackalloc Float64[] { 5.0, 2.0, 1.0 }, stackalloc Float64[] { 1.0, 0.2, 0.3, 0.2, 2.0, 0.5, 0.3, 0.5, 3.0 }, true, null));
	////	Assert.IsTrue(success);
	////}
}