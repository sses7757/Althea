using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Althea.Array;
using Althea.Helpers;


namespace Althea.Backend.Mkl.Transformer;

/// <summary>
/// MKL Discrete Fourier Transform library API
/// </summary>
public static class NativeMethods
{
	[DllImport(Mkl.NativeMethods.MKL_DLL_NAME, EntryPoint = "DftiCreateDescriptor")]
	internal static extern MklDftError DftiCreateDescriptor1D(out IntPtr handle, MklDftPrecision precision, MklDftDomain domain, long dim, long length);

	[DllImport(Mkl.NativeMethods.MKL_DLL_NAME, EntryPoint = "DftiCreateDescriptor")]
	internal static extern MklDftError DftiCreateDescriptorND(out IntPtr handle, MklDftPrecision precision, MklDftDomain domain, long dim, in long size);

	[DllImport(Mkl.NativeMethods.MKL_DLL_NAME, EntryPoint = "DftiSetValue")]
	internal static extern MklDftError DftiSetForwardScale(IntPtr handle, int param = 4, double scale = 1.0);

	[DllImport(Mkl.NativeMethods.MKL_DLL_NAME, EntryPoint = "DftiSetValue")]
	internal static extern MklDftError DftiSetBackwardScale(IntPtr handle, int param = 5, double scale = 1.0);

	[DllImport(Mkl.NativeMethods.MKL_DLL_NAME, EntryPoint = "DftiSetValue")]
	internal static extern MklDftError DftiSetForwardScale(IntPtr handle, int param = 4, float scale = 1.0f);

	[DllImport(Mkl.NativeMethods.MKL_DLL_NAME, EntryPoint = "DftiSetValue")]
	internal static extern MklDftError DftiSetBackwardScale(IntPtr handle, int param = 5, float scale = 1.0f);

	[DllImport(Mkl.NativeMethods.MKL_DLL_NAME, EntryPoint = "DftiSetValue")]
	internal static extern MklDftError DftiSetComplexStorage(IntPtr handle, int param = 8, MklDftStorage storage = MklDftStorage.ComplexAsComplex);

	[DllImport(Mkl.NativeMethods.MKL_DLL_NAME, EntryPoint = "DftiSetValue")]
	internal static extern MklDftError DftiSetConjugateEvenStorage(IntPtr handle, int param = 10, MklDftStorage storage = MklDftStorage.ComplexAsComplex);

	[DllImport(Mkl.NativeMethods.MKL_DLL_NAME, EntryPoint = "DftiSetValue")]
	internal static extern MklDftError DftiSetPlacement(IntPtr handle, int param = 11, MklDftPlacement placement = MklDftPlacement.InPlace);

	[DllImport(Mkl.NativeMethods.MKL_DLL_NAME, EntryPoint = "DftiSetValue")]
	internal static extern MklDftError DftiSetInputStride(IntPtr handle, int param = 12, in long strides = default);

	[DllImport(Mkl.NativeMethods.MKL_DLL_NAME, EntryPoint = "DftiSetValue")]
	internal static extern MklDftError DftiSetOutputStride(IntPtr handle, int param = 13, in long strides = default);

	[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
	internal static extern MklDftError DftiCommitDescriptor(IntPtr handle);

	[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
	internal static extern MklDftError DftiFreeDescriptor(ref IntPtr handle);

	[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
	internal static unsafe extern MklDftError DftiComputeForward(IntPtr handle, void* array);

	[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
	internal static unsafe extern MklDftError DftiComputeForward(IntPtr handle, void* input, void* output);

	[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
	internal static unsafe extern MklDftError DftiComputeBackward(IntPtr handle, void* array);

	[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
	internal static unsafe extern MklDftError DftiComputeBackward(IntPtr handle, void* input, void* output);
}

internal record struct DftiDescriptor : IDisposable
{
	private IntPtr handle;

	public readonly IntPtr Handle => this.handle;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly unsafe void Compute<T>(bool forward, T* input, T* output) where T : unmanaged, IBinaryFloat<T>
	{
		MklDftError err;
		if (forward)
		{
			if (input == output)
				err = NativeMethods.DftiComputeForward(this.handle, input);
			else
				err = NativeMethods.DftiComputeForward(this.handle, input, output);
		}
		else
		{
			if (input == output)
				err = NativeMethods.DftiComputeBackward(this.handle, input);
			else
				err = NativeMethods.DftiComputeBackward(this.handle, input, output);
		}
		err.Check();
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly unsafe void Compute<T>(T* input, Complex<T>* output) where T : unmanaged, IBinaryFloat<T>
	{
		NativeMethods.DftiComputeForward(this.handle, input, output).Check();

	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly unsafe void Compute<T>(Complex<T>* input, T* output) where T : unmanaged, IBinaryFloat<T>
	{
		NativeMethods.DftiComputeBackward(this.handle, input, output).Check();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private DftiDescriptor(bool single, bool complex, bool complexAsReal, bool inPlace, bool forward, double scale, int dim, ReadOnlySpan<long> size, ReadOnlySpan<long> inStride, ReadOnlySpan<long> outStride)
	{
		if (dim == 1)
		{
			NativeMethods.DftiCreateDescriptor1D(out this.handle, single ? MklDftPrecision.Single : MklDftPrecision.Double, complex ? MklDftDomain.Complex : MklDftDomain.Real, 1, size[0]).Check();
		}
		else
		{
			NativeMethods.DftiCreateDescriptorND(out this.handle, single ? MklDftPrecision.Single : MklDftPrecision.Double, complex ? MklDftDomain.Complex : MklDftDomain.Real, dim, in size[0]).Check();
		}
		NativeMethods.DftiSetPlacement(this.handle, placement: inPlace ? MklDftPlacement.InPlace : MklDftPlacement.OutOfPlace).Check();
		if (!inStride.IsEmpty)
			NativeMethods.DftiSetInputStride(this.handle, strides: in inStride[0]).Check();
		if (!outStride.IsEmpty)
			NativeMethods.DftiSetOutputStride(this.handle, strides: in outStride[0]).Check();
		if (scale != 1)
		{
			if (single)
			{
				if (forward)
					NativeMethods.DftiSetForwardScale(this.handle, scale: (float)scale).Check();
				else
					NativeMethods.DftiSetBackwardScale(this.handle, scale: (float)scale).Check();
			}
			else
			{
				if (forward)
					NativeMethods.DftiSetForwardScale(this.handle, scale: scale).Check();
				else
					NativeMethods.DftiSetBackwardScale(this.handle, scale: scale).Check();
			}
		}
		if (complex)
			NativeMethods.DftiSetComplexStorage(this.handle, storage: complexAsReal ? MklDftStorage.ComplexAsReal : MklDftStorage.ComplexAsComplex).Check();
		else
			NativeMethods.DftiSetConjugateEvenStorage(this.handle, storage: complexAsReal ? MklDftStorage.ComplexAsReal : MklDftStorage.ComplexAsComplex).Check();
		NativeMethods.DftiCommitDescriptor(this.handle);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryCreate<T, TS1, TS2>(bool forward, double scale, DenseArrayWrapper<T, TS1> input, DenseArrayWrapper<T, TS2> output, out DftiDescriptor descriptor) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
	{
		descriptor = default;
		if (scale == 0)
			return false;
		if (T.Type.Class() != DataTypeClassification.BinaryFloat_IEEE754 ||
			T.Type.Tuple() != DataTypeTuple.Real || T.Type.Tuple() != DataTypeTuple.Complex ||
			T.Type.Bytes() != 4 || T.Type.Bytes() != 8)
			return false;
		if (!input.Size.SequenceEqual(output.Size))
			return false;
		var complex = T.Type.Tuple() == DataTypeTuple.Complex;
		descriptor = new(T.Type.Bytes() == 4, complex, !complex, input.ValueStorage == output.ValueStorage, forward, scale, input.Rank, input.Size, input.Size.SequenceEqual(input.OuterSize) ? default : input.Strides, output.Size.SequenceEqual(output.OuterSize) ? default : output.Strides);
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryCreate<T, TSR, TSC>(double scale, DenseArrayWrapper<T, TSR> input, DenseArrayWrapper<Complex<T>, TSC> output, out DftiDescriptor descriptor) where T : unmanaged, IBinaryFloat<T> where TSR : class, IStorage<T, TSR> where TSC : class, IStorage<Complex<T>, TSC>
	{
		descriptor = default;
		if (scale == 0)
			return false;
		if (T.Type.Class() != DataTypeClassification.BinaryFloat_IEEE754 ||
			T.Type.Tuple() != DataTypeTuple.Real ||
			T.Type.Bytes() != 4 || T.Type.Bytes() != 8)
			return false;
		if (!input.Size.SequenceEqual(output.Size))
			return false;
		descriptor = new(T.Type.Bytes() == 4, false, false, false, true, scale, input.Rank, input.Size, input.Size.SequenceEqual(input.OuterSize) ? default : input.Strides, output.Size.SequenceEqual(output.OuterSize) ? default : output.Strides);
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryCreate<T, TSR, TSC>(double scale, DenseArrayWrapper<Complex<T>, TSC> input, DenseArrayWrapper<T, TSR> output, out DftiDescriptor descriptor) where T : unmanaged, IBinaryFloat<T> where TSR : class, IStorage<T, TSR> where TSC : class, IStorage<Complex<T>, TSC>
	{
		descriptor = default;
		if (scale == 0)
			return false;
		if (T.Type.Class() != DataTypeClassification.BinaryFloat_IEEE754 ||
			T.Type.Tuple() != DataTypeTuple.Real ||
			T.Type.Bytes() != 4 || T.Type.Bytes() != 8)
			return false;
		if (!input.Size.SequenceEqual(output.Size))
			return false;
		descriptor = new(T.Type.Bytes() == 4, false, false, false, false, scale, input.Rank, input.Size, input.Size.SequenceEqual(input.OuterSize) ? default : input.Strides, output.Size.SequenceEqual(output.OuterSize) ? default : output.Strides);
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose()
	{
		NativeMethods.DftiFreeDescriptor(ref this.handle);
	}
}
