using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace Althea.Backend.CSharp.LinearAlgebra.Tests;

[TestClass()]
public unsafe class ApiTests
{
	private static readonly Api api = new();

	[TestMethod()]
	public void AbsoluteValueArgMaxTest()
	{
		var array = UnitTests.Helpers.GenerateFloatData();
		bool success = api.AbsoluteValueArgMax<Float64, PureStorage<Float64, CpuMemoryPointer>>(array, 1, out long index);
		Assert.IsTrue(success);

		Span<double> values = new(array.Pointer.Pointer.Pointer.ToPointer(), (int)array.Length);
		long realIndex = 0; double max = 0;
		for (int i = 0; i < values.Length; i++)
		{
			if (Math.Abs(values[i]) > max)
			{
				realIndex = i; max = Math.Abs(values[i]);
			}
		}
		Assert.AreEqual(realIndex, index);
	}

	[TestMethod()]
	public void AbsoluteValueArgMinTest()
	{
		Assert.Fail();
	}

	[TestMethod()]
	public void AbsoluteValueSumTest()
	{
		Assert.Fail();
	}

	[TestMethod()]
	public void NormTest()
	{
		Assert.Fail();
	}

	[TestMethod()]
	public void ScaleTest()
	{
		Assert.Fail();
	}

	[TestMethod()]
	public void AddTest()
	{
		Assert.Fail();
	}

	[TestMethod()]
	public void DotTest()
	{
		Assert.Fail();
	}

	[TestMethod()]
	public void GeneralVectorsEqualTest()
	{
		Assert.Fail();
	}

	[TestMethod()]
	public void GeneralVectorUnaryTest()
	{
		Assert.Fail();
	}

	[TestMethod()]
	public void GeneralVectorBinaryScalarTest()
	{
		Assert.Fail();
	}

	[TestMethod()]
	public void GeneralVectorReduceTest()
	{
		Assert.Fail();
	}

	[TestMethod()]
	public void GeneralVectorArgReduceTest()
	{
		Assert.Fail();
	}

	[TestMethod()]
	public void GeneralVectorsBinaryTest()
	{
		Assert.Fail();
	}

	[TestMethod()]
	public void GeneralVectorsScanTest()
	{
		Assert.Fail();
	}

	[TestMethod()]
	public void GeneralVectorsCastTest()
	{
		Assert.Fail();
	}

	[TestMethod()]
	public void SortTest()
	{
		Assert.Fail();
	}

	[TestMethod()]
	public void SortTest1()
	{
		Assert.Fail();
	}

	[TestMethod()]
	public void IndexOfTest()
	{
		Assert.Fail();
	}

	[TestMethod()]
	public void BoundOfTest()
	{
		Assert.Fail();
	}

	[TestMethod()]
	public void FillWithRangeTest()
	{
		Assert.Fail();
	}

	[TestMethod()]
	public void VectorSetValuesAtTest()
	{
		Assert.Fail();
	}

	[TestMethod()]
	public void VectorSetValuesAtTest1()
	{
		Assert.Fail();
	}

	[TestMethod()]
	public void VectorGatherValuesAtTest()
	{
		Assert.Fail();
	}

	[TestMethod()]
	public void VectorSparseToDenseTest()
	{
		Assert.Fail();
	}

	[TestMethod()]
	public void VectorDenseToSparseTest()
	{
		Assert.Fail();
	}

	[TestMethod()]
	public void EigenStandardMatrixHermitianTest()
	{
		Assert.Fail();
	}

	[TestMethod()]
	public void SchurDecompositionTest()
	{
		Assert.Fail();
	}

	[TestMethod()]
	public void SchurReorderTest()
	{
		Assert.Fail();
	}

	[TestMethod()]
	public void EigenStandardMatrixGeneralTest()
	{
		Assert.Fail();
	}

	[TestMethod()]
	public void LinearSolveGeneralTest()
	{
		Assert.Fail();
	}

	[TestMethod()]
	public void QRDecompositionTest()
	{
		Assert.Fail();
	}

	[TestMethod()]
	public void LeastSquareSolveTest()
	{
		Assert.Fail();
	}
}