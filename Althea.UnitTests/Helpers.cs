global using System;

global using Althea.Backend.Storage;
global using Althea.Numerics;
global using Althea.Storage;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Althea.Backend.Cuda;

using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace Althea.UnitTests;

internal sealed class ActualPureStorage<T, TP> : PureStorage<T, TP> where T : unmanaged, IBaseNumber<T> where TP : notnull, IPointer<TP>
{
	internal ActualPureStorage(TP pointer) : base(pointer) { }
	
	~ActualPureStorage() => this.Dispose(false);
}

internal sealed class NoDisposePureStorage<T, TP> : PureStorage<T, TP> where T : unmanaged, IBaseNumber<T> where TP : notnull, IPointer<TP>
{
	internal NoDisposePureStorage(TP pointer) : base(pointer) { }

	public override void Dispose(bool invokedByUser) { }

	////~ActualPureStorage() => this.Dispose(false);
}

internal static class ValueAssert
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void AreApproxEqual(double expected, double real)
	{
		if (expected == real)
			return;
		if (double.IsNaN(expected) && double.IsNaN(real))
			return;
		if (double.IsInfinity(expected) && double.IsInfinity(real) && double.Sign(expected) == double.Sign(real))
			return;
		Assert.IsFalse(expected == 0 && real != 0);
		Assert.IsTrue(Math.Abs(real - expected) / Math.Abs(expected) < 1E-10);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void AreApproxEqual<T>(T expected, T real) where T : unmanaged, IBaseNumber<T>
	{
		if (expected == real)
			return;
		if (T.IsNaN(expected) && T.IsNaN(real))
			return;
		if (!T.IsFinite(expected) && !T.IsFinite(real) && T.Sign(expected) == T.Sign(real))
			return;
		Assert.IsFalse(expected == default && real != default);
		T threshold = 1E-10.As<T>();
		Assert.IsTrue(T.Abs(real - expected) / T.Abs(expected) < threshold);
	}
}

internal static unsafe class Helpers
{
	private static readonly Backend.CSharp.Storage.Api api = new();

	private static readonly System.Random rand;

	static Helpers()
	{
		Settings.SetImplementation<IAbstractApi>(api);
		rand = new System.Random(0);
	}

	public static void CopyToManaged<T>(this PureStorage<T, CpuMemoryPointer> array, T* values) where T : unmanaged, IBaseNumber<T>
	{
		api.MemoryCopy<T, CpuMemoryPointer, CpuMemoryPointer>(array.Pointer, new(new((IntPtr)values, array.Length * sizeof(T))), out _);
	}

	public static void NoApiCopy<T>(this PureStorage<T, CpuMemoryPointer> source, PureStorage<T, CpuMemoryPointer> dest) where T : unmanaged, IBaseNumber<T>
	{
		api.MemoryCopy<T, CpuMemoryPointer, CpuMemoryPointer>(source.Pointer, dest.Pointer, out _);
	}

	public static PureStorage<Float64, CudaMemoryPointer<GpuId0>> GenerateGpuFloatData(double lower, double upper)
	{
		upper -= lower;
		int length = rand.Next(1024) + 1024;
		Float64* array = (Float64*)Marshal.AllocHGlobal(length * sizeof(Float64));
		for (int i = 0; i < length; i++)
		{
			array[i] = rand.NextDouble() * upper + lower;
		}
		return new ActualPureStorage<Float64, CudaMemoryPointer<GpuId0>>(new((IntPtr)array, length * sizeof(Float64)));
	}

	public static PureStorage<Float64, CpuMemoryPointer> GenerateFloatData(double lower, double upper)
	{
		upper -= lower;
		int length = rand.Next(1024) + 1024;
		Float64* array = (Float64*)Marshal.AllocHGlobal(length * sizeof(Float64));
		for (int i = 0; i < length; i++)
		{
			array[i] = rand.NextDouble() * upper + lower;
		}
		return new ActualPureStorage<Float64, CpuMemoryPointer>(new((IntPtr)array, length * sizeof(Float64)));
	}

	public static PureStorage<SignedInt32, CpuMemoryPointer> GenerateIntData(int lower, int upper)
	{
		upper -= lower;
		int length = rand.Next(1024) + 1024;
		SignedInt32* array = (SignedInt32*)Marshal.AllocHGlobal(length * sizeof(SignedInt32));
		for (int i = 0; i < length; i++)
		{
			array[i] = rand.Next(upper) + lower;
		}
		return new ActualPureStorage<SignedInt32, CpuMemoryPointer>(new((IntPtr)array, length * sizeof(SignedInt32)));
	}
}

internal static unsafe class GpuHelpers
{
	private static readonly Backend.Cuda.Storage.Api api = new(false);

	private static readonly System.Random rand;

	static GpuHelpers()
	{
		Settings.SetImplementation<IAbstractApi>(api);
		rand = new System.Random(0);
	}

	public static void CopyFromManaged<T>(this PureStorage<T, CudaMemoryPointer<GpuId0>> array, T* values) where T : unmanaged, IBaseNumber<T>
	{
		api.MemoryCopy<T, CpuMemoryPointer, CudaMemoryPointer<GpuId0>>(new(new((IntPtr)values, array.Length * sizeof(T))), array.Pointer, out _);
	}

	public static void CopyToManaged<T>(this PureStorage<T, CudaMemoryPointer<GpuId0>> array, T* values) where T : unmanaged, IBaseNumber<T>
	{
		api.MemoryCopy<T, CudaMemoryPointer<GpuId0>, CpuMemoryPointer>(array.Pointer, new(new((IntPtr)values, array.Length * sizeof(T))), out _);
	}

	public static void NoApiCopy<T>(this PureStorage<T, CudaMemoryPointer<GpuId0>> source, PureStorage<T, CudaMemoryPointer<GpuId0>> dest) where T : unmanaged, IBaseNumber<T>
	{
		api.MemoryCopy<T, CudaMemoryPointer<GpuId0>, CudaMemoryPointer<GpuId0>>(source.Pointer, dest.Pointer, out _);
	}

	public static PureStorage<Float64, CudaMemoryPointer<GpuId0>> GenerateFloatData(double lower, double upper, out double* host)
	{
		upper -= lower;
		int length = rand.Next(1024) + 1024;
		Float64* array = (Float64*)Marshal.AllocHGlobal(length * sizeof(Float64));
		for (int i = 0; i < length; i++)
		{
			array[i] = rand.NextDouble() * upper + lower;
		}
		api.Allocate<CudaMemoryPointer<GpuId0>>(length * sizeof(double), out var gpu);
		var s = new ActualPureStorage<Float64, CudaMemoryPointer<GpuId0>>(gpu);
		s.CopyFromManaged(array);
		host = (double*)array;
		return s;
	}

	public static PureStorage<SignedInt32, CudaMemoryPointer<GpuId0>> GenerateIntData(int lower, int upper, out int* host)
	{
		upper -= lower;
		int length = rand.Next(1024) + 1024;
		SignedInt32* array = (SignedInt32*)Marshal.AllocHGlobal(length * sizeof(SignedInt32));
		for (int i = 0; i < length; i++)
		{
			array[i] = rand.Next(upper) + lower;
		}
		api.Allocate<CudaMemoryPointer<GpuId0>>(length * sizeof(int), out var gpu);
		var s = new ActualPureStorage<SignedInt32, CudaMemoryPointer<GpuId0>>(gpu);
		s.CopyFromManaged(array);
		host = (int*)array;
		return s;
	}
}
