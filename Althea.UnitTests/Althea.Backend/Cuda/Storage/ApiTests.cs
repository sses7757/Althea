using Microsoft.VisualStudio.TestTools.UnitTesting;

using Ptr = Althea.Backend.Cuda.CudaMemoryPointer<Althea.Backend.Cuda.GpuId0>;
using HostPtr = Althea.Backend.Storage.CpuMemoryPointer;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Newtonsoft.Json.Linq;
using Althea.Numerics;
using Althea.UnitTests;

namespace Althea.Backend.Cuda.Storage.Tests;

[TestClass()]
public unsafe class ApiTests
{
	private static readonly Api api = new(false);
	private static readonly Althea.Backend.CSharp.Storage.Api hostApi = new();

	[TestMethod()]
	[DataRow(1024L)]
	public void AllocateFreeTest(long length)
	{
		bool success = api.Allocate<Ptr>(length, out var result);
		Assert.IsTrue(success);
		Assert.AreEqual(length, result.LengthInBytes);

		success = api.Free(result, out bool valid);
		Assert.IsTrue(success);
		Assert.IsTrue(valid);
	}

	[TestMethod()]
	[DataRow(1024L, (byte)10)]
	public void FillWithByteValueTest(long length, byte value)
	{
		bool success = api.Allocate<Ptr>(length, out var result);
		Assert.IsTrue(success);

		success = api.FillWithValue<Ptr>(result, value);
		Assert.IsTrue(success);

		api.Free(result, out _);
	}

	[TestMethod()]
	[DataRow(1024L, (byte)10, true)]
	[DataRow(1024L, (byte)10, false)]
	public void MemoryCopyHostTest(long length, byte value, bool toHost)
	{
		bool success = api.Allocate<Ptr>(length, out var result);
		Assert.IsTrue(success);
		hostApi.Allocate<HostPtr>(length, out var hostResult);
		if (toHost)
			api.FillWithValue<Ptr>(result, value);
		else
			hostApi.FillWithValue<HostPtr>(hostResult, value);

		long copied;
		if (toHost)
			success = api.MemoryCopy<SignedInt8, Ptr, HostPtr>(result, hostResult, out copied);
		else
			success = api.MemoryCopy<SignedInt8, HostPtr, Ptr>(hostResult, result, out copied);
		Assert.IsTrue(success);
		Assert.AreEqual(length, copied);
		if (toHost)
		{
			byte* p = (byte*)hostResult.Pointer.ToPointer();
			for (int i = 0; i < length; i++)
			{
				Assert.AreEqual(value, p[i]);
			}
		}

		api.Free(result, out _);
		hostApi.Free(hostResult, out _);
	}

	[TestMethod()]
	[DataRow(1024L, (byte)10)]
	public void MemoryCopyGpuTest(long length, byte value)
	{
		api.Allocate<Ptr>(length, out var result);
		api.FillWithValue<Ptr>(result, value);
		api.Allocate<Ptr>(length, out var target);

		bool success = api.MemoryCopy<SignedInt8, Ptr, Ptr>(result, target, out long copied);
		Assert.IsTrue(success);
		Assert.AreEqual(length, copied);

		api.Free(result, out _);
	}

	[TestMethod()]
	[DataRow(1024L, 1.0)]
	public void FillWithValueTest(long length, double value)
	{
		api.Allocate<Ptr>(length * sizeof(double), out var result);
		hostApi.Allocate<HostPtr>(length * sizeof(double), out var test);

		bool success = api.FillWithValue<Float64, Ptr>(result, value);
		Assert.IsTrue(success);

		api.MemoryCopy<Float64, Ptr, HostPtr>(result, test, out _);
		double* p = (double*)test.Pointer.ToPointer();
		for (int i = 0; i < length; i++)
		{
			Assert.AreEqual(value, p[i]);
		}

		api.Free(result, out _);
		hostApi.Free(test, out _);
	}

	[TestMethod()]
	[DataRow(1024L, 2048L)]
	public void MemoryCopy2DTest(long rows, long cols)
	{
		api.Allocate<Ptr>(rows * cols * sizeof(double), out var src);
		api.Allocate<Ptr>(rows / 2 * cols / 2 * sizeof(double), out var dst);

		bool success = api.MemoryCopy2D<Float64, Ptr, Ptr>(new PointerSegment<Ptr>(src) + rows * sizeof(double) / 2, rows, dst, rows / 2, rows / 2, 0, out long copyWidth);
		Assert.IsTrue(success);
		Assert.AreEqual(cols / 2, copyWidth);

		api.Free(src, out _);
		api.Free(dst, out _);
	}

	[TestMethod()]
	[DataRow(1024L, 2048L)]
	public void MemoryCopy2DHostTest(long rows, long cols)
	{
		api.Allocate<Ptr>(rows * cols * sizeof(double), out var src);
		hostApi.Allocate<HostPtr>(rows / 2 * cols / 2 * sizeof(double), out var dst);

		bool success = api.MemoryCopy2D<Float64, Ptr, HostPtr>(new PointerSegment<Ptr>(src) + rows * sizeof(double) / 2, rows, dst, rows / 2, rows / 2, 0, out long copyWidth);
		Assert.IsTrue(success);
		Assert.AreEqual(cols / 2, copyWidth);

		api.Free(src, out _);
		api.Free(dst, out _);
	}

	[TestMethod()]
	public void StridedCopyTest()
	{

	}
}