using Microsoft.VisualStudio.TestTools.UnitTesting;

using Althea.UnitTests;
using Althea.Random;

namespace Althea.Backend.CSharp.Random.Tests;

[TestClass()]
public unsafe class ApiTests
{
	private static readonly Api api = new();

	[TestMethod()]
	[DataRow(-0.5, 0.5)]
	public void FillWithUniformTest(double low, double high)
	{
		using var array = UnitTests.Helpers.GenerateFloatData(-100, 100);

		bool success = api.FillWithRandom<Float64, PureStorage<Float64, CpuMemoryPointer>, UniformDistribution<Float64>>(array, new(low, high, null));
		Assert.IsTrue(success);

		double* test = (double*)array.Pointer.Pointer.Pointer.ToPointer();
		for (int i = 0; i < array.Length; i++)
		{
			Assert.IsTrue(test[i] >= low && test[i] < high);
		}
	}
}