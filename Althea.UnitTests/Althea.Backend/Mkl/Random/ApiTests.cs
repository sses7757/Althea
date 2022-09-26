using Microsoft.VisualStudio.TestTools.UnitTesting;

using Althea.Array;
using Althea.Random;
using Althea.UnitTests;

using MemF64 = Althea.Storage.PureStorage<Althea.Numerics.Float64, Althea.Backend.Storage.CpuMemoryPointer>;
using MemI32 = Althea.Storage.PureStorage<Althea.Numerics.SignedInt32, Althea.Backend.Storage.CpuMemoryPointer>;


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
	[DataRow(1, true)]
	[DataRow(2, true)]
	[DataRow(3, true)]
	[DataRow(4, true)]
	[DataRow(5, true)]
	[DataRow(6, true)]
	[DataRow(7, true)]
	[DataRow(8, true)]
	[DataRow(1, false)]
	[DataRow(2, false)]
	[DataRow(3, false)]
	[DataRow(4, false)]
	[DataRow(5, false)]
	[DataRow(6, false)]
	[DataRow(7, false)]
	[DataRow(8, false)]
	public void FillWithRandomTest(int dist, int type, bool useSeed)
	{
		/*
		UniformDistribution<T> => typeof(T) == typeof(Float32) || typeof(T) == typeof(Float64) || typeof(T) == typeof(SignedInt32) || typeof(T) == typeof(UnsignedInt32) ? DistributionType.Uniform : INVALID,
		RandomBitsDistribution<T> => T.Size == sizeof(int) || T.Size == sizeof(long) ? DistributionType.RandomBits : INVALID,

		BetaDistribution<T> => typeof(T) == typeof(Float32) || typeof(T) == typeof(Float64) ? DistributionType.Beta : INVALID,
		CauchyDistribution<T> => typeof(T) == typeof(Float32) || typeof(T) == typeof(Float64) ? DistributionType.Cauchy : INVALID,
		ChiSquareDistribution<T> => typeof(T) == typeof(Float32) || typeof(T) == typeof(Float64) ? DistributionType.ChiSquare : INVALID,
		ExponentialDistribution<T> => typeof(T) == typeof(Float32) || typeof(T) == typeof(Float64) ? DistributionType.Exponential : INVALID,
		GammaDistribution<T> => typeof(T) == typeof(Float32) || typeof(T) == typeof(Float64) ? DistributionType.Gamma : INVALID,
		GumbelDistribution<T> => typeof(T) == typeof(Float32) || typeof(T) == typeof(Float64) ? DistributionType.Gumbel : INVALID,
		LaplaceDistribution<T> => typeof(T) == typeof(Float32) || typeof(T) == typeof(Float64) ? DistributionType.Laplace : INVALID,
		LogNormalDistribution<T> => typeof(T) == typeof(Float32) || typeof(T) == typeof(Float64) ? DistributionType.LogNormal : INVALID,
		NegativeBinomialDistribution<T> => typeof(T) == typeof(int) || typeof(T) == typeof(uint) ? DistributionType.NegativeBinomial : INVALID,
		NormalDistribution<T> => typeof(T) == typeof(Float32) || typeof(T) == typeof(Float64) ? DistributionType.Normal : INVALID,
		RayleighDistribution<T> => typeof(T) == typeof(Float32) || typeof(T) == typeof(Float64) ? DistributionType.Rayleigh : INVALID,
		WeibullDistribution<T> => typeof(T) == typeof(Float32) || typeof(T) == typeof(Float64) ? DistributionType.Weibull : INVALID,

		BernoulliDistribution<T> => typeof(T) == typeof(int) || typeof(T) == typeof(uint) ? DistributionType.Bernoulli : INVALID,
		BinomialDistribution<T> => typeof(T) == typeof(int) || typeof(T) == typeof(uint) ? DistributionType.Binomial : INVALID,
		GeometricDistribution<T> => typeof(T) == typeof(int) || typeof(T) == typeof(uint) ? DistributionType.Geometric : INVALID,
		HypergeometricDistribution<T> => typeof(T) == typeof(int) || typeof(T) == typeof(uint) ? DistributionType.Hypergeometric : INVALID,
		PoissonDistribution<T> => typeof(T) == typeof(int) || typeof(T) == typeof(uint) ? DistributionType.Poisson : INVALID,
		_ => INVALID,
		 */
		long? seed = useSeed ? 1 : null;
		if (dist == 1)
		{
			switch (type)
			{
				case 0:
					Fill1DTest<Float32, UniformDistribution<Float32>>(new(5.0f, 5.5f, seed));
					break;
				case 1:
					Fill1DTest<Float64, UniformDistribution<Float64>>(new(5.0, 5.5, seed));
					break;
				case 3:
					Fill1DTest<SignedInt32, UniformDistribution<SignedInt32>>(new(5, 10, seed));
					break;
			}
		}
		else if (dist == 2)
		{
			Fill1DTest<Float64, UniformDistribution<Float64>>(new(5.0, 5.5, seed));
		}
		else if (dist == 3)
		{
			Fill1DTest<Float32, NormalDistribution<Float32>>(new(5.0f, 1.0f, seed));
		}
		else if (dist == 4)
		{
			Fill1DTest<Float64, NormalDistribution<Float64>>(new(5.0, 0.5, seed));
		}
		else if (dist == 5)
		{
			Fill1DTest<Float32, LogNormalDistribution<Float32>>(new(5.0f, 1.0f, seed));
		}
		else if (dist == 6)
		{
			Fill1DTest<Float64, LogNormalDistribution<Float64>>(new(5.0, 1.0, seed));
		}
		else if (dist == 7)
		{
			Fill1DTest<SignedInt32, RandomBitsDistribution<SignedInt32>>(new(seed));
		}
		else if (dist == 8)
		{
			Fill1DTest<SignedInt32, PoissonDistribution<SignedInt32>>(new(2, seed));
		}
	}

	[TestMethod()]
	public void FillWithRandomTest1()
	{

	}

	[TestMethod()]
	public void FillWithRandomTest2()
	{

	}

	[TestMethod()]
	public void FillWithRandomTest3()
	{

	}
}