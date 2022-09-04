using Althea.Backend.Storage;

using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace Althea.Backend.CSharp.Storage.Tests;

[TestClass()]
public unsafe class ApiTests
{
	private static readonly Api api = new();

	private static CpuMemoryPointer Allocate()
	{
		bool success = api.Allocate<CpuMemoryPointer>(1024, out var pointer);
		Assert.IsTrue(success);
		return pointer;
	}

	private static void Free(CpuMemoryPointer pointer)
	{
		bool success = api.Free(pointer, out bool valid);
		Assert.IsTrue(success);
		Assert.IsTrue(valid);
	}

	[TestMethod()]
	public void AllocateFreeTest()
	{
		Free(Allocate());
	}

	[TestMethod()]
	public void FillWithByteValueTest()
	{
		const byte VAL = 5;

		var pointer = Allocate();
		bool success = api.FillWithValue<CpuMemoryPointer>(pointer, VAL);
		Assert.IsTrue(success);

		byte* ptr = (byte*)pointer.Pointer.ToPointer();
		for (long i = 0; i < pointer.LengthInBytes; i++)
		{
			if (ptr[i] != VAL)
				Assert.Fail();
		}
		Free(pointer);
	}

	[TestMethod()]
	public void FillWithValueTest()
	{
		Float64 VAL = 5;

		var pointer = Allocate();
		bool success = api.FillWithValue<Float64, CpuMemoryPointer>(pointer, VAL);
		Assert.IsTrue(success);

		Float64* ptr = (Float64*)pointer.Pointer.ToPointer();
		for (long i = 0; i < pointer.LengthInBytes / sizeof(Float64); i++)
		{
			if (ptr[i] != VAL)
				Assert.Fail();
		}
		Free(pointer);
	}

	[TestMethod()]
	public void MemoryCopyTest()
	{
		Float64 VAL = 5;

		var pointer1 = Allocate();
		var pointer2 = Allocate();
		bool success = api.FillWithValue<Float64, CpuMemoryPointer>(pointer1, VAL);
		Assert.IsTrue(success);

		success = api.MemoryCopy<Float64, CpuMemoryPointer, CpuMemoryPointer>(pointer1, pointer2, out long copied);
		Assert.IsTrue(success);

		Float64* ptr = (Float64*)pointer2.Pointer.ToPointer();
		for (long i = 0; i < pointer2.LengthInBytes / sizeof(Float64); i++)
		{
			if (ptr[i] != VAL)
				Assert.Fail();
		}
		Free(pointer1);
		Free(pointer2);
	}

	[TestMethod()]
	public void MemoryCopy2DTest()
	{
		Float64 VAL = 5;
		const int LD = 20, H = 10, OFFSET = 5;

		PointerSegment<CpuMemoryPointer> pointer1 = Allocate();
		PointerSegment<CpuMemoryPointer> pointer2 = Allocate();
		bool success = api.FillWithValue(pointer1, VAL);
		Assert.IsTrue(success);
		success = api.FillWithValue(pointer2, 0);
		Assert.IsTrue(success);

		success = api.MemoryCopy2D<Float64, CpuMemoryPointer, CpuMemoryPointer>(pointer1, LD, pointer2 + OFFSET * sizeof(Float64), LD, H, pointer1.LengthInBytes / LD / sizeof(Float64), out long copied);
		Assert.IsTrue(success);

		Float64* ptr = (Float64*)pointer2.Pointer.Pointer.ToPointer() + OFFSET;
		for (long i = 0; i < pointer2.LengthInBytes / sizeof(Float64) / LD * LD; i++)
		{
			if (i % LD >= H)
			{
				if (ptr[i] != 0)
					Assert.Fail();
			}
			else
			{
				if (ptr[i] != VAL)
					Assert.Fail();
			}
		}
		Free(pointer1.Pointer);
		Free(pointer2.Pointer);
	}

	[TestMethod()]
	public void StridedCopyTest()
	{
		Float64 VAL = 5;
		const int STRIDE = 8, OFFSET = 5;

		PointerSegment<CpuMemoryPointer> pointer1 = Allocate();
		PointerSegment<CpuMemoryPointer> pointer2 = Allocate();
		bool success = api.FillWithValue(pointer1, VAL);
		Assert.IsTrue(success);
		success = api.FillWithValue(pointer2, 0);
		Assert.IsTrue(success);

		success = api.StridedCopy<Float64, CpuMemoryPointer, CpuMemoryPointer>(pointer1, STRIDE, pointer2 + OFFSET * sizeof(Float64), STRIDE, out long copied);
		Assert.AreEqual(pointer2.LengthInBytes / sizeof(Float64) / STRIDE, copied);
		Assert.IsTrue(success);

		Float64* ptr = (Float64*)pointer2.Pointer.Pointer.ToPointer() + OFFSET;
		for (long i = 0; i < pointer2.LengthInBytes / sizeof(Float64) / STRIDE * STRIDE - OFFSET; i++)
		{
			if (i % STRIDE != 0)
			{
				if (ptr[i] != 0)
					Assert.Fail();
			}
			else
			{
				if (ptr[i] != VAL)
					Assert.Fail();
			}
		}
		Free(pointer1.Pointer);
		Free(pointer2.Pointer);
	}
}