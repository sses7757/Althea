using System.Runtime.CompilerServices;

using Althea.Backend.Storage;
using Althea.NativeTypes;
using Althea.Storage;

using MklDn = Althea.Backend.Mkl.LinearAlgebra.Dense.NativeMethods;


namespace Althea.Backend.Mkl.Storage
{
	/// <summary>
	/// The MKL back-end of <see cref="IAbstractApi"/> that supports storage locations of CPU memory.
	/// </summary>
	public unsafe class Api : CSharp.Storage.Api
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool CheckType<TP>() where TP : IPointer<TP> => typeof(TP) == typeof(CpuMemoryPointer);

		private delegate void BlasCopy<T>(long n, T* src, long incSrc, T* dst, long incDst) where T : unmanaged, INumber<T>;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool PointerStridedCopy<T>(T* source, long strideSource, T* destination, long strideDestination, long count) where T : unmanaged, INumber<T>
		{
			// shortcut
			if (strideSource == 1 && strideDestination == 1)
			{
				Unsafe.CopyBlockUnaligned(destination, source, (uint)count);
				return true;
			}
			delegate*<long, T*, long, T*, long, void> func = null;
			if (typeof(T) == typeof(float))
				func = &MklDn.cblas_scopy;
			if (typeof(T) == typeof(double))
				func = &MklDn.cblas_dcopy;
			if (typeof(T) == typeof(Complex<float>))
				func = &MklDn.cblas_ccopy;
			if (typeof(T) == typeof(Complex<double>))
				func = &MklDn.cblas_zcopy;
			if (func == null)
				return false;
			func(count, source, strideSource, destination, strideDestination);
			return true;
		}

		/// <inheritdoc/>
		public override bool StridedCopy<T, TP1, TP2>(PointerSegment<TP1> source, long strideSource, PointerSegment<TP2> destination, long strideDestination, out long actualCopied)
		{
			actualCopied = 0;
			if (!CheckType<TP1>() || !CheckType<TP2>())
				return false;
			long srcOff = source.OffsetInBytes, dstOff = destination.OffsetInBytes;
			CpuMemoryPointer src = source.Pointer.FromGeneric(), dst = destination.Pointer.FromGeneric();
			actualCopied = Math.Min(((src.LengthInBytes - srcOff) / sizeof(T) - 1) / strideSource + 1, ((dst.LengthInBytes - dstOff) / sizeof(T) - 1) / strideDestination + 1);
			return PointerStridedCopy((T*)src.NativePointer(srcOff), strideSource, (T*)dst.NativePointer(dstOff), strideDestination, actualCopied);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool PointerMemoryCopy2D<T>(T* source, long sourceLD, T* destination, long destinationLD, long height, long width) where T : unmanaged, INumber<T>
		{
			// shortcut
			if (sourceLD == destinationLD && sourceLD == height && height * width <= uint.MaxValue)
			{
				Unsafe.CopyBlockUnaligned(destination, source, (uint)(height * width));
				return true;
			}
			MklDn.MKL_omatcopy<T>? func = null;
			if (typeof(T) == typeof(float))
				func = new MklDn.MKL_omatcopy<float>(MklDn.MKL_Somatcopy) as MklDn.MKL_omatcopy<T>;
			if (typeof(T) == typeof(double))
				func = new MklDn.MKL_omatcopy<double>(MklDn.MKL_Domatcopy) as MklDn.MKL_omatcopy<T>;
			if (typeof(T) == typeof(Complex<float>))
				func = new MklDn.MKL_omatcopy<Complex<float>>(MklDn.MKL_Comatcopy) as MklDn.MKL_omatcopy<T>;
			if (typeof(T) == typeof(Complex<double>))
				func = new MklDn.MKL_omatcopy<Complex<double>>(MklDn.MKL_Zomatcopy) as MklDn.MKL_omatcopy<T>;
			if (func == null)
				return false;
			func(LinearAlgebra.Dense.MklMatrixLayoutChar.ColMajor, LinearAlgebra.Dense.MklOperationChar.NoneTranspose, height, width, T.One, source, sourceLD, destination, destinationLD);
			return true;
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
			CpuMemoryPointer src = source.Pointer.FromGeneric(), dst = destination.Pointer.FromGeneric();
			return PointerMemoryCopy2D((T*)src.NativePointer(srcOff), sourceLD, (T*)dst.NativePointer(dstOff), destinationLD, height, width);
		}
	}
}
