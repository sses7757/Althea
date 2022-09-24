using System.Runtime.InteropServices;

using Althea.LinearAlgebra;
using Althea.UnitTests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using GpuMemF64 = Althea.Storage.PureStorage<Althea.Numerics.Float64, Althea.Backend.Cuda.CudaMemoryPointer<Althea.Backend.Cuda.GpuId0>>;


namespace Althea.Backend.Cuda.LinearAlgebra.Dense.Tests;

[TestClass()]
public unsafe class LapackApiTests
{
	private static readonly Api api = new();

	[TestMethod()]
	[DataRow(true)]
	[DataRow(false)]
	public void EigenStandardMatrixHermitianTest(bool allowDestroy)
	{
		var s1 = GpuHelpers.GenerateFloatData(-10, 10, out var host1);
		var s2 = GpuHelpers.GenerateFloatData(-10, 10, out var host2);

		bool success = api.EigenStandardMatrixHermitian<Float64, GpuMemF64, GpuMemF64, GpuMemF64>(32, true, s1, 32, s2, allowDestroy ? s1 : null, allowDestroy ? 32 : 0, allowDestroy);
		Assert.IsTrue(success);

		Marshal.FreeHGlobal((IntPtr)host1);
		Marshal.FreeHGlobal((IntPtr)host2);
	}

	[TestMethod()]
	[DataRow(true)]
	[DataRow(false)]
	public void EigenGeneralMatrixHermitianTest(bool allowDestroy)
	{
		var s0 = GpuHelpers.GenerateFloatData(-10, 10, out var host0);
		var s1 = GpuHelpers.GenerateFloatData(-10, 10, out var host1);
		var s2 = GpuHelpers.GenerateFloatData(-10, 10, out var host2);
		var s3 = GpuHelpers.GenerateFloatData(-10, 10, out var host3);

		api.GeneralMatricesMultiply(MatrixOperation.None, MatrixOperation.Transpose, 32, 32, 32, (Float64)1, s0, 32, s0, 32, default, s1, 32);
		api.GeneralMatricesMultiply(MatrixOperation.None, MatrixOperation.Transpose, 32, 32, 32, (Float64)1, s3, 32, s3, 32, default, s2, 32);

		bool success = api.EigenGeneralMatrixHermitian<Float64, GpuMemF64, GpuMemF64, GpuMemF64, GpuMemF64>(GeneralEigenType.Type1, 32, true, s1, 32, s2, 32, s3, allowDestroy ? s1 : null, allowDestroy ? 32 : 0, allowDestroy ? s2 : null, allowDestroy ? 32 : 0, allowDestroy);
		Assert.IsTrue(success);

		Marshal.FreeHGlobal((IntPtr)host1);
		Marshal.FreeHGlobal((IntPtr)host2);
		Marshal.FreeHGlobal((IntPtr)host3);
	}

	[TestMethod()]
	[DataRow(true, true, true, 32L, 16L)]
	public void SingularValuesTest(bool allowDestroy, bool fullU, bool fullV, long m, long n)
	{
		var s1 = GpuHelpers.GenerateFloatData(-10, 10, out var host1);
		var s2 = GpuHelpers.GenerateFloatData(-10, 10, out var host2);
		var s3 = GpuHelpers.GenerateFloatData(-10, 10, out var host3);
		var s4 = GpuHelpers.GenerateFloatData(-10, 10, out var host4);

		bool success = api.SingularValues<Float64, GpuMemF64, GpuMemF64, GpuMemF64, GpuMemF64>(fullU, fullV, m, n, s1, 32, s2, 32, s3, 32, s4, allowDestroy);
		Assert.IsTrue(success);

		Marshal.FreeHGlobal((IntPtr)host1);
		Marshal.FreeHGlobal((IntPtr)host2);
		Marshal.FreeHGlobal((IntPtr)host3);
		Marshal.FreeHGlobal((IntPtr)host4);
	}

	[TestMethod()]
	[DataRow(true, 32L, 16L)]
	[DataRow(false, 32L, 16L)]
	public void LinearSolveGeneralTest(bool allowDestroy, long n, long nrhs)
	{
		var s1 = GpuHelpers.GenerateFloatData(-10, 10, out var host1);
		var s2 = GpuHelpers.GenerateFloatData(-10, 10, out var host2);

		bool success = api.LinearSolveGeneral<Float64, GpuMemF64, GpuMemF64>(n, nrhs, s1, 32, s2, 32, allowDestroy);
		Assert.IsTrue(success);

		Marshal.FreeHGlobal((IntPtr)host1);
		Marshal.FreeHGlobal((IntPtr)host2);
	}

	[TestMethod()]
	[DataRow(true, 32L, 16L)]
	[DataRow(false, 32L, 16L)]
	[DataRow(true, 16L, 32L)]
	[DataRow(false, 16L, 32L)]
	public void QRDecompositionTest(bool full, long m, long n)
	{
		var s1 = GpuHelpers.GenerateFloatData(-10, 10, out var host1);
		var s2 = GpuHelpers.GenerateFloatData(-10, 10, out var host2);

		bool success = api.QRDecomposition<Float64, GpuMemF64, GpuMemF64>(full, m, n, s1, 32, s2, 32);
		Assert.IsTrue(success);

		Marshal.FreeHGlobal((IntPtr)host1);
		Marshal.FreeHGlobal((IntPtr)host2);
	}

	[TestMethod()]
	[DataRow(32L, 16L, 8L)]
	[DataRow(32L, 16L, 8L)]
	public void LeastSquareSolveTest(long m, long n, long nrhs)
	{
		var s1 = GpuHelpers.GenerateFloatData(-10, 10, out var host1);
		var s2 = GpuHelpers.GenerateFloatData(-10, 10, out var host2);

		bool success = api.LeastSquareSolve<Float64, GpuMemF64, GpuMemF64>(m, n, nrhs, s1, 32, s2, 32);
		Assert.IsTrue(success);

		Marshal.FreeHGlobal((IntPtr)host1);
		Marshal.FreeHGlobal((IntPtr)host2);
	}
}