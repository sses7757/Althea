using System.Runtime.CompilerServices;

using Althea.Backend.Storage;
using Althea.NativeTypes;
using Althea.Storage;


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
		internal static bool PointerStridedCopy<T>(T* source, int strideSource, T* destination, int strideDestination, int count) where T : unmanaged, INumber<T>
		{
			// shortcut
			if (strideSource == 1 && strideDestination == 1)
			{
				Unsafe.CopyBlockUnaligned(destination, source, (uint)count);
				return true;
			}
			switch (sizeof(T))
			{
				case sizeof(float):
					LinearAlgebra.Dense.NativeMethods.cblas_scopy(count, (IntPtr)source, strideSource, (IntPtr)destination, strideDestination);
					break;
				case sizeof(double):
					if (Unmanaged<T>.DataType == DataType.ComplexSingle)
						LinearAlgebra.Dense.NativeMethods.cblas_ccopy(count, (IntPtr)source, strideSource, (IntPtr)destination, strideDestination);
					else
						LinearAlgebra.Dense.NativeMethods.cblas_dcopy(count, (IntPtr)source, strideSource, (IntPtr)destination, strideDestination);
					break;
				case sizeof(double) * 2:
					LinearAlgebra.Dense.NativeMethods.cblas_zcopy(count, (IntPtr)source, strideSource, (IntPtr)destination, strideDestination);
					break;
				default:
					return false;
			}
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
			
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool PointerMemoryCopy2D(IntPtr source, long sourceLD, IntPtr destination, long destinationLD, long height, long width)
		{
			// shortcut
			if (sourceLD == destinationLD && sourceLD == height)
			{
				Unsafe.CopyBlockUnaligned((void*)destination, (void*)source, (uint)(height * width));
				return true;
			}
			if (sourceLD % sizeof(float) == 0 && destinationLD % sizeof(float) == 0 && height % sizeof(float) == 0)
			{
				LinearAlgebra.Dense.NativeMethods.MKL_Somatcopy(LinearAlgebra.Dense.MklMatrixLayoutChar.ColMajor, LinearAlgebra.Dense.MklOperationChar.NoneTranspose, height / sizeof(float), width, 1, source, sourceLD / sizeof(float), destination, destinationLD / sizeof(float));
				return true;
			}
			return false;
		}

		/// <inheritdoc/>
		public override bool MemoryCopy2D<TP1, TP2>(PointerSegment<TP1> source, long sourceLD, PointerSegment<TP2> destination, long destinationLD, long height, long width, out long copyWidth)
		{
			copyWidth = 0;
			if (!CheckType<TP1>() || !CheckType<TP2>())
				return false;
			copyWidth = Math.Min((source.LengthInBytes + (sourceLD - height)) / sourceLD, (destination.LengthInBytes + (destinationLD - height)) / destinationLD);
			copyWidth = Math.Min(copyWidth, width);
			long srcOff = source.OffsetInBytes, dstOff = destination.OffsetInBytes;
			CpuMemoryPointer src = source.Pointer.FromGeneric(), dst = destination.Pointer.FromGeneric();

		}
	}
}
