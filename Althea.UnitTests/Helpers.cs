global using System;

global using Althea.Backend.Storage;
global using Althea.Numerics;
global using Althea.Storage;

using System.Runtime.InteropServices;


namespace Althea.UnitTests;

internal sealed class ActualPureStorage<T, TP> : PureStorage<T, TP> where T : unmanaged, IBaseNumber<T> where TP : notnull, IPointer<TP>
{
	internal ActualPureStorage(TP pointer) : base(pointer) { }

	~ActualPureStorage() => this.Dispose(false);
}

internal static unsafe class Helpers
{
	private static readonly Althea.Backend.CSharp.Storage.Api api = new();

	public static PureStorage<Float64, CpuMemoryPointer> GenerateFloatData()
	{
		int length = System.Random.Shared.Next(1024) + 1024;
		Float64* array = (Float64*)Marshal.AllocHGlobal(length * sizeof(Float64));
		for (int i = 0; i < length; i++)
		{
			array[i] = System.Random.Shared.NextDouble();
		}
		return new ActualPureStorage<Float64, CpuMemoryPointer>(new((IntPtr)array, length * sizeof(Float64)));
	}

	public static PureStorage<SignedInt32, CpuMemoryPointer> GenerateIntData(int lower, int upper)
	{
		upper -= lower;
		int length = System.Random.Shared.Next(1024) + 1024;
		SignedInt32* array = (SignedInt32*)Marshal.AllocHGlobal(length * sizeof(SignedInt32));
		for (int i = 0; i < length; i++)
		{
			array[i] = System.Random.Shared.Next(upper) + lower;
		}
		return new ActualPureStorage<SignedInt32, CpuMemoryPointer>(new((IntPtr)array, length * sizeof(SignedInt32)));
	}
}
