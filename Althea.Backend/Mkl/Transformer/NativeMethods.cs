using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Althea.Array;
using Althea.Helpers;


namespace Althea.Backend.Mkl.Transformer
{
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
		internal static extern MklDftError DftiSetComplexStorage(IntPtr handle, int param = 8, MklDftStorage storage = MklDftStorage.ComplexAsComplex);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME, EntryPoint = "DftiSetValue")]
		internal static extern MklDftError DftiSetPlacement(IntPtr handle, int param = 11, MklDftPlacement placement = MklDftPlacement.InPlace);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME, EntryPoint = "DftiSetValue")]
		internal static extern MklDftError DftiSetInputStride(IntPtr handle, int param = 12, in long strides = default);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME, EntryPoint = "DftiSetValue")]
		internal static extern MklDftError DftiSetOutputStride(IntPtr handle, int param = 13, in long strides = default);

		[DllImport(Mkl.NativeMethods.MKL_DLL_NAME)]
		internal static extern MklDftError DftiFreeDescriptor(ref IntPtr handle);
	}

	internal struct DftiDescriptor : IEqualityOperators<DftiDescriptor, DftiDescriptor>, IDisposable
	{
		private IntPtr handle;

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
			if (forward)
				NativeMethods.DftiSetForwardScale(this.handle, scale: scale).Check();
			else
				NativeMethods.DftiSetBackwardScale(this.handle, scale: scale).Check();
			if (complexAsReal)
				NativeMethods.DftiSetComplexStorage(this.handle, storage: MklDftStorage.ComplexAsReal).Check();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool TryCreate<T, TS>(bool forward, double scale, DenseArrayWrapper<T, TS> input, DenseArrayWrapper<T, TS> output, out DftiDescriptor descriptor) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
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
			descriptor = new(T.Type.Bytes() == 4, T.Type.Tuple() == DataTypeTuple.Complex, false, input == output, forward, scale, input.Rank, input.Size, input.Strides, output.Strides);
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
			descriptor = new(T.Type.Bytes() == 4, true, true, false, true, scale, input.Rank, input.Size, input.Strides, output.Strides);
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
			descriptor = new(T.Type.Bytes() == 4, true, true, false, false, scale, input.Rank, input.Size, input.Strides, output.Strides);
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Dispose()
		{
			var err = NativeMethods.DftiFreeDescriptor(ref this.handle);
			if (err != MklDftError.Success)
				Log.Write($"Error in MKL DFT descriptor disposition: {err}", level: LogLevel.Error);
		}

		/// <inheritdoc/>
		public static bool operator ==(DftiDescriptor left, DftiDescriptor right) => left.Equals(right);
		/// <inheritdoc/>
		public static bool operator !=(DftiDescriptor left, DftiDescriptor right) => !left.Equals(right);

		/// <inheritdoc/>
		public bool Equals(DftiDescriptor other) => this.handle == other.handle;

		/// <inheritdoc/>
		public override bool Equals(object? obj) => obj is DftiDescriptor descriptor && Equals(descriptor);

		/// <inheritdoc/>
		public override int GetHashCode() => this.handle.GetHashCode();
	}
}
