using System.Runtime.InteropServices;

using Althea.Array;
using Althea.LinearAlgebra;
using Althea.TensorAlgebra;
using Althea.TensorAlgebra.Dense;
using Althea.UnitTests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using GpuMemF64 = Althea.Storage.PureStorage<Althea.Numerics.Float64, Althea.Backend.Cuda.CudaMemoryPointer<Althea.Backend.Cuda.GpuId0>>;


namespace Althea.Backend.Cuda.TensorAlgebra.Dense.Tests;

[TestClass()]
public unsafe class ApiTests
{
	private static readonly Api api = new();

	[TestMethod()]
	public void PermuteTest()
	{
		var s1 = GpuHelpers.GenerateFloatData(-10, 10, out var host1);
		var s2 = GpuHelpers.GenerateFloatData(-10, 10, out var host2);
		var arr1 = new DenseTensorWrapper<Float64, GpuMemF64>(s1, stackalloc long[] { 5, 10, 5, 4 });
		var arr2 = new DenseArrayWrapper<Float64, GpuMemF64>(s2, stackalloc long[] { 4, 10, 5, 5 });

		bool success = api.Permute(arr1, arr2, stackalloc int[] { 3, 1, 2, 0 });
		Assert.IsTrue(success);

		Marshal.FreeHGlobal((IntPtr)host1);
		Marshal.FreeHGlobal((IntPtr)host2);
	}

	[TestMethod()]
	public void OperationBinaryTest()
	{
		var s1 = GpuHelpers.GenerateFloatData(-10, 10, out var host1);
		var s2 = GpuHelpers.GenerateFloatData(-10, 10, out var host2);
		var s3 = GpuHelpers.GenerateFloatData(-10, 10, out var host3);
		var arr1 = new DenseTensorWrapper<Float64, GpuMemF64>(s1, stackalloc long[] { 5, 10, 5, 4 });
		var arr2 = new DenseTensorWrapper<Float64, GpuMemF64>(s2, stackalloc long[] { 4, 10, 5, 5 });
		var arr3 = new DenseArrayWrapper<Float64, GpuMemF64>(s3, stackalloc long[] { 4, 10, 5, 5 });

		bool success = api.OperationBinary(BinaryOperation.Add, arr1, stackalloc int[] { 3, 1, 2, 0 }, arr2, stackalloc int[] { 0, 1, 2, 3 }, arr3);
		Assert.IsTrue(success);

		Marshal.FreeHGlobal((IntPtr)host1);
		Marshal.FreeHGlobal((IntPtr)host2);
		Marshal.FreeHGlobal((IntPtr)host3);
	}

	[TestMethod()]
	public void ReduceTest()
	{
		var s1 = GpuHelpers.GenerateFloatData(-10, 10, out var host1);
		var s2 = GpuHelpers.GenerateFloatData(-10, 10, out var host2);
		var arr1 = new DenseTensorWrapper<Float64, GpuMemF64>(s1, stackalloc long[] { 5, 10, 5, 4 });
		var arr2 = new DenseTensorWrapper<Float64, GpuMemF64>(s2, stackalloc long[] { 10 });

		bool success = api.Reduce(ReduceOperation.Add, arr1, arr2, stackalloc int[] { 0, 2, 3 });
		Assert.IsTrue(success);

		Marshal.FreeHGlobal((IntPtr)host1);
		Marshal.FreeHGlobal((IntPtr)host2);
	}

	[TestMethod()]
	public void ContractTest()
	{
		var s1 = GpuHelpers.GenerateFloatData(-10, 10, out var host1);
		var s2 = GpuHelpers.GenerateFloatData(-10, 10, out var host2);
		var s3 = GpuHelpers.GenerateFloatData(-10, 10, out var host3);
		var arr1 = new DenseTensorWrapper<Float64, GpuMemF64>(s1, stackalloc long[] { 5, 10, 5, 4 });
		var arr2 = new DenseTensorWrapper<Float64, GpuMemF64>(s2, stackalloc long[] { 4, 10, 5, 5 });
		var arr3 = new DenseTensorWrapper<Float64, GpuMemF64>(s3, stackalloc long[] { 10, 10 });
		Span<int> concA = stackalloc int[] { 0, 2, 3 }, concB = stackalloc int[] { 3, 2, 0 }, freeCA = stackalloc int[] { 0 }, freeCB = stackalloc int[] { 1 };
		var info = new TensorContractInfo(concA, concB, freeCA, freeCB);

		bool success = api.Contract(arr1, arr2, arr3, info);
		Assert.IsTrue(success);

		Marshal.FreeHGlobal((IntPtr)host1);
		Marshal.FreeHGlobal((IntPtr)host2);
		Marshal.FreeHGlobal((IntPtr)host3);
	}
}