using System.Runtime.InteropServices;

using Althea.UnitTests;
using Althea.LinearAlgebra;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using GpuMemF64 = Althea.Storage.PureStorage<Althea.Numerics.Float64, Althea.Backend.Cuda.CudaMemoryPointer<Althea.Backend.Cuda.GpuId0>>;
using GpuMemC64 = Althea.Storage.PureStorage<Althea.Numerics.Complex<Althea.Numerics.Float64>, Althea.Backend.Cuda.CudaMemoryPointer<Althea.Backend.Cuda.GpuId0>>;
using GpuMemI32 = Althea.Storage.PureStorage<Althea.Numerics.SignedInt32, Althea.Backend.Cuda.CudaMemoryPointer<Althea.Backend.Cuda.GpuId0>>;


namespace Althea.Backend.Cuda.LinearAlgebra.Dense.Tests;

[TestClass()]
public unsafe class BlasApiTests
{
	static void Main(string[] args)
	{
		var test = new BlasApiTests();
		test.AbsoluteValueArgMaxComplexTest(5);
	}

	private static readonly Api api = new();

	[TestMethod()]
	[DataRow(1)]
	[DataRow(5)]
	public void AbsoluteValueArgMaxTest(long stride)
	{
		using var s = GpuHelpers.GenerateFloatData(-10, 10, out var host);

		bool success = api.AbsoluteValueArgMax<Float64, GpuMemF64>(s, stride, out long index);
		Assert.IsTrue(success);

		long real = 0; double realVal = 0;
		for (long i = 0; i < s.Length; i += stride)
		{
			if (Math.Abs(host[i]) > realVal)
			{
				real = i; realVal = Math.Abs(host[i]);
			}
		}
		Assert.AreEqual(real / stride, index);
		Marshal.FreeHGlobal((IntPtr)host);
	}

	[TestMethod()]
	[DataRow(1)]
	[DataRow(5)]
	public void AbsoluteValueArgMaxComplexTest(long stride)
	{
		using var s = GpuHelpers.GenerateFloatData(-10, 10, out var host);

		bool success = api.AbsoluteValueArgMax<Complex<Float64>, GpuMemC64>(s.As<Complex<Float64>>(), stride, out long index);
		Assert.IsTrue(success);

		long real = 0; double realVal = 0;
		Complex<Float64>* array = (Complex<Float64>*)host;
		for (long i = 0; i < s.Length / 2; i += stride)
		{
			if (array[i].MagnitudeSquared > realVal)
			{
				real = i; realVal = array[i].MagnitudeSquared;
			}
		}
		Assert.AreEqual(real / stride, index);
		Marshal.FreeHGlobal((IntPtr)host);
	}

	[TestMethod()]
	[DataRow(1)]
	[DataRow(5)]
	public void AbsoluteValueArgMinTest(long stride)
	{
		using var s = GpuHelpers.GenerateFloatData(-10, 10, out var host);

		bool success = api.AbsoluteValueArgMin<Float64, GpuMemF64>(s, stride, out long index);
		Assert.IsTrue(success);

		long real = 0; double realVal = 100;
		for (long i = 0; i < s.Length; i += stride)
		{
			if (Math.Abs(host[i]) < realVal)
			{
				real = i; realVal = Math.Abs(host[i]);
			}
		}
		Assert.AreEqual(real / stride, index);
		Marshal.FreeHGlobal((IntPtr)host);
	}

	[TestMethod()]
	[DataRow(1)]
	[DataRow(5)]
	public void AbsoluteValueSumTest(long stride)
	{
		using var s = GpuHelpers.GenerateFloatData(-10, 10, out var host);

		bool success = api.AbsoluteValueSum(s, stride, out Float64 sum);
		Assert.IsTrue(success);

		double real = 0;
		for (long i = 0; i < s.Length; i += stride)
		{
			real += Math.Abs(host[i]);
		}
		ValueAssert.AreApproxEqual(real, (double)sum);
		Marshal.FreeHGlobal((IntPtr)host);
	}

	[TestMethod()]
	[DataRow(1)]
	[DataRow(5)]
	public void NormTest(long stride)
	{
		using var s = GpuHelpers.GenerateFloatData(-10, 10, out var host);

		bool success = api.Norm(s, stride, out Float64 norm);
		Assert.IsTrue(success);

		double real = 0;
		for (long i = 0; i < s.Length; i += stride)
		{
			real += host[i] * host[i];
		}
		ValueAssert.AreApproxEqual(Math.Sqrt(real), (double)norm);
		Marshal.FreeHGlobal((IntPtr)host);
	}

	[TestMethod()]
	[DataRow(1)]
	[DataRow(5)]
	public void NormComplexTest(long stride)
	{
		using var s = GpuHelpers.GenerateFloatData(-10, 10, out var host);

		bool success = api.Norm(s.As<Complex<Float64>>(), stride, out Complex<Float64> norm);
		Assert.IsTrue(success);
		Assert.IsTrue(norm.Imaginary == 0);

		double real = 0;
		Complex<Float64>* array = (Complex<Float64>*)host;
		for (long i = 0; i < s.Length / 2; i += stride)
		{
			real += array[i].MagnitudeSquared;
		}
		ValueAssert.AreApproxEqual(Math.Sqrt(real), (double)norm.Real);
		Marshal.FreeHGlobal((IntPtr)host);
	}

	[TestMethod()]
	[DataRow(1)]
	[DataRow(5)]
	public void DotTest(long stride)
	{
		using var s1 = GpuHelpers.GenerateFloatData(-10, 10, out var host1);
		using var s2 = GpuHelpers.GenerateFloatData(-10, 10, out var host2);

		bool success = api.Dot(false, s1, stride, s2, stride, out Float64 dot);
		Assert.IsTrue(success);

		double real = 0;
		for (long i = 0; i < Math.Min(s1.Length, s2.Length); i += stride)
		{
			real += host1[i] * host2[i];
		}
		ValueAssert.AreApproxEqual(real, (double)dot);
		Marshal.FreeHGlobal((IntPtr)host1);
		Marshal.FreeHGlobal((IntPtr)host2);
	}

	[TestMethod()]
	[DataRow(1, true)]
	[DataRow(5, true)]
	[DataRow(1, false)]
	[DataRow(5, false)]
	public void DotComplexTest(long stride, bool conj)
	{
		using var s1 = GpuHelpers.GenerateFloatData(-10, 10, out var host1);
		using var s2 = GpuHelpers.GenerateFloatData(-10, 10, out var host2);

		bool success = api.Dot(conj, s1.As<Complex<Float64>>(), stride, s2.As<Complex<Float64>>(), stride, out Complex<Float64> dot);
		Assert.IsTrue(success);

		Complex<Float64> real = default;
		Complex<Float64>* array1 = (Complex<Float64>*)host1;
		Complex<Float64>* array2 = (Complex<Float64>*)host2;
		for (long i = 0; i < Math.Min(s1.Length, s2.Length) / 2; i += stride)
		{
			real += (conj ? array1[i].Conjugate : array1[i]) * array2[i];
		}
		ValueAssert.AreApproxEqual(real, dot);
		Marshal.FreeHGlobal((IntPtr)host1);
		Marshal.FreeHGlobal((IntPtr)host2);
	}

	[TestMethod()]
	[DataRow(1)]
	[DataRow(5)]
	public void ScaleTest(long stride)
	{
		using var s = GpuHelpers.GenerateFloatData(-10, 10, out var host);

		bool success = api.Scale(s, stride, (Float64)5.0);
		Assert.IsTrue(success);

		double* test = stackalloc double[(int)s.Length];
		GpuHelpers.CopyToManaged(s, (Float64*)test);
		for (long i = 0; i < s.Length; i += stride)
		{
			ValueAssert.AreApproxEqual(host[i] * 5, test[i]);
		}
		Marshal.FreeHGlobal((IntPtr)host);
	}

	[TestMethod()]
	[DataRow(1)]
	[DataRow(5)]
	public void AddTest(long stride)
	{
		using var s1 = GpuHelpers.GenerateFloatData(-10, 10, out var host1);
		using var s2 = GpuHelpers.GenerateFloatData(-10, 10, out var host2);

		bool success = api.Add((Float64)5.0, s1, stride, s2, stride);
		Assert.IsTrue(success);

		double* test = stackalloc double[(int)Math.Min(s1.Length, s2.Length)];
		GpuHelpers.CopyToManaged(s2, (Float64*)test);
		for (long i = 0; i < Math.Min(s1.Length, s2.Length); i += stride)
		{
			ValueAssert.AreApproxEqual(host1[i] * 5 + host2[i], test[i]);
		}
		Marshal.FreeHGlobal((IntPtr)host1);
		Marshal.FreeHGlobal((IntPtr)host2);
	}

	[TestMethod()]
	[DataRow(MatrixOperation.None, 64L, 1)]
	[DataRow(MatrixOperation.None, 64L, 5)]
	[DataRow(MatrixOperation.Transpose, 64L, 1)]
	[DataRow(MatrixOperation.Transpose, 64L, 5)]
	public void GeneralMatrixMultiplyVectorTest(MatrixOperation op, long ld, long stride)
	{
		using var s1 = GpuHelpers.GenerateFloatData(-10, 10, out var host1);
		using var s2 = GpuHelpers.GenerateFloatData(-10, 10, out var host2);
		using var s3 = GpuHelpers.GenerateFloatData(-10, 10, out var host3);

		bool success = api.GeneralMatrixMultiplyVector(op, ld, s1.Length / ld, (Float64)0.5, s1, ld, s2, stride, (Float64)0.5, s3, stride);
		Assert.IsTrue(success);

		Marshal.FreeHGlobal((IntPtr)host1);
		Marshal.FreeHGlobal((IntPtr)host2);
		Marshal.FreeHGlobal((IntPtr)host3);
	}

	[TestMethod()]
	[DataRow(true, 64L, 1)]
	[DataRow(true, 64L, 5)]
	[DataRow(false, 64L, 1)]
	[DataRow(false, 64L, 5)]
	public void SymmetricMatrixMultiplyVectorTest(bool upper, long ld, long stride)
	{
		using var s1 = GpuHelpers.GenerateFloatData(-10, 10, out var host1);
		using var s2 = GpuHelpers.GenerateFloatData(-10, 10, out var host2);
		using var s3 = GpuHelpers.GenerateFloatData(-10, 10, out var host3);

		bool success = api.SymmetricMatrixMultiplyVector(upper, false, s1.Length / ld, (Float64)0.5, s1, ld, s2, stride, (Float64)0.5, s3, stride);
		Assert.IsTrue(success);

		Marshal.FreeHGlobal((IntPtr)host1);
		Marshal.FreeHGlobal((IntPtr)host2);
		Marshal.FreeHGlobal((IntPtr)host3);
	}

	[TestMethod()]
	[DataRow(MatrixOperation.None, true, 64L, 1)]
	[DataRow(MatrixOperation.None, true, 64L, 5)]
	[DataRow(MatrixOperation.None, false, 64L, 1)]
	[DataRow(MatrixOperation.None, false, 64L, 5)]
	[DataRow(MatrixOperation.Transpose, true, 64L, 1)]
	[DataRow(MatrixOperation.Transpose, true, 64L, 5)]
	[DataRow(MatrixOperation.Transpose, false, 64L, 1)]
	[DataRow(MatrixOperation.Transpose, false, 64L, 5)]
	[DataRow(MatrixOperation.None, true, 16L, 1)]
	[DataRow(MatrixOperation.None, true, 16L, 5)]
	[DataRow(MatrixOperation.None, false, 16L, 1)]
	[DataRow(MatrixOperation.None, false, 16L, 5)]
	[DataRow(MatrixOperation.Transpose, true, 16L, 1)]
	[DataRow(MatrixOperation.Transpose, true, 16L, 5)]
	[DataRow(MatrixOperation.Transpose, false, 16L, 1)]
	[DataRow(MatrixOperation.Transpose, false, 16L, 5)]
	public void TriangularMatrixMultiplyVectorTest(MatrixOperation op, bool upper, long ld, long stride)
	{
		using var s1 = GpuHelpers.GenerateFloatData(-10, 10, out var host1);
		using var s2 = GpuHelpers.GenerateFloatData(-10, 10, out var host2);
		using var s3 = GpuHelpers.GenerateFloatData(-10, 10, out var host3);

		bool success = api.TriangularMatrixMultiplyVector(upper, false, op, ld, s1.Length / ld, s1, ld, (Float64)0.5, s2, stride, default, s3, 1);
		Assert.IsTrue(success);

		Marshal.FreeHGlobal((IntPtr)host1);
		Marshal.FreeHGlobal((IntPtr)host2);
		Marshal.FreeHGlobal((IntPtr)host3);
	}

	[TestMethod()]
	[DataRow(64L, 1)]
	[DataRow(64L, 5)]
	public void GeneralRankOneUpdateTest(long ld, long stride)
	{
		using var s1 = GpuHelpers.GenerateFloatData(-10, 10, out var host1);
		using var s2 = GpuHelpers.GenerateFloatData(-10, 10, out var host2);
		using var s3 = GpuHelpers.GenerateFloatData(-10, 10, out var host3);

		bool success = api.GeneralRankOneUpdate(false, ld, s1.Length / ld, (Float64)0.5, s2, stride, s3, stride, (Float64)0.5, s1, ld);
		Assert.IsTrue(success);

		Marshal.FreeHGlobal((IntPtr)host1);
		Marshal.FreeHGlobal((IntPtr)host2);
		Marshal.FreeHGlobal((IntPtr)host3);
	}

	[TestMethod()]
	[DataRow(true, 64L, 1)]
	[DataRow(true, 64L, 5)]
	[DataRow(false, 64L, 1)]
	[DataRow(false, 64L, 5)]
	public void SymmetricRankOneUpdateTest(bool upper, long ld, long stride)
	{
		using var s1 = GpuHelpers.GenerateFloatData(-10, 10, out var host1);
		using var s2 = GpuHelpers.GenerateFloatData(-10, 10, out var host2);

		bool success = api.SymmetricRankOneUpdate(upper, false, s1.Length / ld, (Float64)0.5, s2, stride, (Float64)0.5, s1, ld);
		Assert.IsTrue(success);

		Marshal.FreeHGlobal((IntPtr)host1);
		Marshal.FreeHGlobal((IntPtr)host2);
	}

	[TestMethod()]
	[DataRow(true, 64L, 1)]
	[DataRow(true, 64L, 5)]
	[DataRow(false, 64L, 1)]
	[DataRow(false, 64L, 5)]
	public void SymmetricRankTwoUpdateTest(bool upper, long ld, long stride)
	{
		using var s1 = GpuHelpers.GenerateFloatData(-10, 10, out var host1);
		using var s2 = GpuHelpers.GenerateFloatData(-10, 10, out var host2);
		using var s3 = GpuHelpers.GenerateFloatData(-10, 10, out var host3);

		bool success = api.SymmetricRankTwoUpdate(upper, false, s1.Length / ld, (Float64)0.5, s2, stride, s3, stride, (Float64)0.5, s1, ld);
		Assert.IsTrue(success);

		Marshal.FreeHGlobal((IntPtr)host1);
		Marshal.FreeHGlobal((IntPtr)host2);
		Marshal.FreeHGlobal((IntPtr)host3);
	}

	[TestMethod()]
	public void GeneralMatricesMultiplyTest()
	{

	}

	[TestMethod()]
	public void SymmetricMatrixMultiplyGeneralTest()
	{

	}

	[TestMethod()]
	public void TriangularMatrixSolveTest()
	{

	}

	[TestMethod()]
	public void TriangularMatrixMultiplyGeneralTest()
	{

	}

	[TestMethod()]
	public void SymmetricRankKUpdateTest()
	{

	}

	[TestMethod()]
	public void SymmetricRankTwoKUpdateTest()
	{

	}

	[TestMethod()]
	public void GeneralMatricesAddTest()
	{

	}

	[TestMethod()]
	public void DiagonalMatrixMultiplyGeneralTest()
	{

	}

	[TestMethod()]
	public void SymmetricRankKUpdateVariantTest()
	{

	}
}