using System;
using System.Runtime.CompilerServices;

using Althea.Backend.Storage;
using Althea.Storage;

using static Althea.Backend.Storage.ConcretePointersExtension;


#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释
namespace Althea.Backend.Mkl.Storage
{
	/// <summary>
	/// The MKL back-end of <see cref="AbstractApi"/> that supports storage locations of CPU and file.
	/// </summary>
	public class StorageApi : CSharp.Storage.StorageApi
	{
		public override (int major, int minor) DriverVersion(StorageLocation location)
		{
			if (location.Type == LocationType.CpuRam)
				return MklRuntime.GetDriverVersion();
			else
				return default;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static unsafe bool PointerMemoryCopy2D(IntPtr source, long sourceLD, IntPtr destination, long destinationLD, long height, long width)
		{
			// shortcut
			if (sourceLD == destinationLD && sourceLD == height)
			{
				Unsafe.CopyBlockUnaligned((void*)destination, (void*)source, (uint)(height * width));
				return true;
			}
			if (sourceLD % sizeof(float) == 0 && destinationLD % sizeof(float) == 0 && height % sizeof(float) == 0)
			{
				LinearAlgebra.Dense.NativeMethods.MKL_Somatcopy((byte)'C', (byte)'N', height / sizeof(float), width, 1, source, sourceLD / sizeof(float), destination, destinationLD / sizeof(float));
				return true;
			}
			return false;
		}

		protected override bool MemoryCopy2D_(PointerSegment source, long sourceLD, PointerSegment destination, long destinationLD, long height, long width)
		{
			Copy2DCheck(source, sourceLD, destination, destinationLD, height, width);
			// shortcut
			if (sourceLD == destinationLD && sourceLD == height)
			{
				return this.MemoryCopy_(source.AsLength(height * width), destination.AsLength(height * width), out _);
			}
			// normal cases
			long srcOff = source.GetPointerOffsetManaged(out IMemoryPointer? srcMP, out _);
			long dstOff = destination.GetPointerOffsetManaged(out IMemoryPointer? dstMP, out _);
			if (srcOff == NOT_SUPPORT || dstOff == NOT_SUPPORT)
				return false;
			if (srcMP is not null && dstMP is not null)
			{
				if (PointerMemoryCopy2D(srcMP.OffsetPointer(srcOff), sourceLD, dstMP.OffsetPointer(dstOff), destinationLD, height, width))
					return true;
			}
			return base.MemoryCopy2D_(source, sourceLD, destination, destinationLD, height, width);
		}
	}
}
