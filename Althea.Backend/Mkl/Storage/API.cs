using System.Runtime.CompilerServices;

using Althea.Backend.Storage;

using MklDn = Althea.Backend.Mkl.LinearAlgebra.Dense.NativeMethods;


namespace Althea.Backend.Mkl.Storage;

/// <summary>
/// The MKL back-end of <see cref="IAbstractApi"/> that supports storage locations of CPU memory.
/// </summary>
public unsafe class Api : CSharp.Storage.Api
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool CheckType<TP>() where TP : IPointer<TP> => typeof(TP) == typeof(CpuMemoryPointer);

	private delegate void BlasCopy<T>(long n, T* src, long incSrc, T* dst, long incDst) where T : unmanaged, IBaseNumber<T>;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool PointerStridedCopy<T>(T* source, long strideSource, T* destination, long strideDestination, long count) where T : unmanaged, IBaseNumber<T>
	{
		// shortcut
		if (strideSource == 1 && strideDestination == 1)
		{
			Buffer.MemoryCopy(source, destination, count, count);
			return true;
		}
		delegate*<MklInt, T*, MklInt, T*, MklInt, void> func = default(T) switch
		{
			Float32 => &MklDn.cblas_scopy,
			Float64 => &MklDn.cblas_dcopy,
			Complex<Float32> => &MklDn.cblas_ccopy,
			Complex<Float64> => &MklDn.cblas_zcopy,
			_ => null
		};
		if (func is not null)
		{
			func(count, source, strideSource, destination, strideDestination);
			return true;
		}
		return LinearAlgebra.Dense.CustomNativeMethods.vecStridedCopy(T.Type, count, source, strideSource, destination, strideDestination) != LinearAlgebra.Dense.CustomStatus.NotSupported;
	}

	/// <inheritdoc/>
	public override bool StridedCopy<T, TP1, TP2>(PointerSegment<TP1> source, long strideSource, PointerSegment<TP2> destination, long strideDestination, out long actualCopied)
	{
		actualCopied = 0;
		if (!CheckType<TP1>() || !CheckType<TP2>())
			return false;
		long srcOff = source.OffsetInBytes, dstOff = destination.OffsetInBytes;
		CpuMemoryPointer src = source.Pointer.FromGenericCpu(), dst = destination.Pointer.FromGenericCpu();
		actualCopied = Math.Min(((src.LengthInBytes - srcOff) / sizeof(T) - 1) / strideSource + 1, ((dst.LengthInBytes - dstOff) / sizeof(T) - 1) / strideDestination + 1);
		return PointerStridedCopy((T*)src.NativePointer(srcOff), strideSource, (T*)dst.NativePointer(dstOff), strideDestination, actualCopied);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool PointerMemoryCopy2D<T>(T* source, long sourceLD, T* destination, long destinationLD, long height, long width, Althea.LinearAlgebra.MatrixOperation op = Althea.LinearAlgebra.MatrixOperation.None, T scale = default) where T : unmanaged, IBaseNumber<T>
	{
		if (scale == default)
			scale = T.One;
		if (!Althea.LinearAlgebra.MatrixOperationExtension.CanInPlace(op))
			(height, width) = (width, height);
		if (source == destination)
		{
			if (sourceLD != destinationLD)
				return false;
			if (op == Althea.LinearAlgebra.MatrixOperation.None && scale == T.One)
				return true;
			MklDn.MKL_imatcopy<T>? funcI = default(T) switch
			{
				Float32 => new MklDn.MKL_imatcopy<Float32>(MklDn.MKL_Simatcopy) as MklDn.MKL_imatcopy<T>,
				Float64 => new MklDn.MKL_imatcopy<Float64>(MklDn.MKL_Dimatcopy) as MklDn.MKL_imatcopy<T>,
				Complex<Float32> => new MklDn.MKL_imatcopy<Complex<Float32>>(MklDn.MKL_Cimatcopy) as MklDn.MKL_imatcopy<T>,
				Complex<Float64> => new MklDn.MKL_imatcopy<Complex<Float64>>(MklDn.MKL_Zimatcopy) as MklDn.MKL_imatcopy<T>,
				_ => null
			};
			funcI?.Invoke(LinearAlgebra.Dense.MklMatrixLayoutChar.ColMajor, LinearAlgebra.Dense.MklBlasExtension.ToMklChar(op), height, width, scale, source, sourceLD);
			return funcI != null;
		}
		// shortcut
		if (scale == T.One && op == Althea.LinearAlgebra.MatrixOperation.None && sourceLD == destinationLD && sourceLD == height && height * width <= uint.MaxValue)
		{
			Buffer.MemoryCopy(source, destination, height * width, height * width);
			return true;
		}
		MklDn.MKL_omatcopy<T>? func = default(T) switch
		{
			Float32 => new MklDn.MKL_omatcopy<Float32>(MklDn.MKL_Somatcopy) as MklDn.MKL_omatcopy<T>,
			Float64 => new MklDn.MKL_omatcopy<Float64>(MklDn.MKL_Domatcopy) as MklDn.MKL_omatcopy<T>,
			Complex<Float32> => new MklDn.MKL_omatcopy<Complex<Float32>>(MklDn.MKL_Comatcopy) as MklDn.MKL_omatcopy<T>,
			Complex<Float64> => new MklDn.MKL_omatcopy<Complex<Float64>>(MklDn.MKL_Zomatcopy) as MklDn.MKL_omatcopy<T>,
			_ => null
		};
		func?.Invoke(LinearAlgebra.Dense.MklMatrixLayoutChar.ColMajor, LinearAlgebra.Dense.MklBlasExtension.ToMklChar(op), height, width, scale, source, sourceLD, destination, destinationLD);
		return func != null;
	}

	/// <inheritdoc/>
	public override bool MemoryCopy2D<T, TP1, TP2>(PointerSegment<TP1> source, long sourceLD, PointerSegment<TP2> destination, long destinationLD, long height, long width, out long copyWidth)
	{
		copyWidth = 0;
		if (!CheckType<TP1>() || !CheckType<TP2>())
			return false;
		copyWidth = Math.Min((source.LengthInBytes + (sourceLD - height)) / sourceLD, (destination.LengthInBytes + (destinationLD - height)) / destinationLD);
		copyWidth = Math.Min(copyWidth, width);
		long srcOff = source.OffsetInBytes, dstOff = destination.OffsetInBytes;
		CpuMemoryPointer src = source.Pointer.FromGenericCpu(), dst = destination.Pointer.FromGenericCpu();
		return PointerMemoryCopy2D((T*)src.NativePointer(srcOff), sourceLD, (T*)dst.NativePointer(dstOff), destinationLD, height, width);
	}
}
