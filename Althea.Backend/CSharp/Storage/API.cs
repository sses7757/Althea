using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Althea.Backend.Storage;


namespace Althea.Backend.CSharp.Storage;

/// <summary>
/// The C# back-end of <see cref="IAbstractApi"/> that supports storage locations of CPU memory.
/// </summary>
public class Api : IAbstractApi, Althea.LinearAlgebra.Dense.ICopyAbstractApi
{
	#region basic
	void IDisposable.Dispose()
	{
		this.Disposed = true;
		GC.SuppressFinalize(this);
	}

	/// <inheritdoc/>
	public bool Disposed { get; protected set; } = false;

	/// <summary>
	/// Get the default <see cref="Api"/>.
	/// </summary>
	internal protected static readonly Api Default = new();
	#endregion

	#region operations
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool CheckType<TP>() where TP : IPointer<TP> => typeof(TP) == typeof(CpuMemoryPointer);

	/// <inheritdoc/>
	public unsafe bool Allocate<TP>(long length, out TP result) where TP : IPointer<TP>
	{
		if (length < 0)
			throw new ArgumentOutOfRangeException(nameof(length), length, Resources.ParameterError.CannotNegative);
		result = TP.Default;
		if (!CheckType<TP>())
			return false;
		var ptr = Marshal.AllocHGlobal((IntPtr)length);
		result = new CpuMemoryPointer(ptr, length).AsGeneric<TP>();
		return true;
	}

	/// <inheritdoc/>
	public bool Free<TP>(TP pointer, out bool valid) where TP : IPointer<TP>
	{
		valid = pointer.IsValid();
		if (!valid || !CheckType<TP>())
			return false;
		var ptr = pointer.FromGenericCpu();
		Marshal.FreeHGlobal(ptr.Pointer);
		return true;
	}

	/// <inheritdoc/>
	public bool FillWithValue<TP>(PointerSegment<TP> pointer, byte value) where TP : IPointer<TP>
	{
		if (!CheckType<TP>())
			return false;
		var span = pointer.Pointer.FromGenericCpu().AsSpan<UnsignedInt8, TP>(pointer);
		span.Fill(value);
		return true;
	}

	/// <inheritdoc/>
	public bool FillWithValue<T, TP>(PointerSegment<TP> pointer, T value)
		where T : unmanaged, IBaseNumber<T>
		where TP : IPointer<TP>
	{
		if (!CheckType<TP>())
			return false;
		var span = pointer.Pointer.FromGenericCpu().AsSpan<T, TP>(pointer);
		span.Fill(value);
		return true;
	}

	/// <inheritdoc/>
	public bool MemoryCopy<T, TP1, TP2>(PointerSegment<TP1> source, PointerSegment<TP2> destination, out long actualCopied)
		where T : unmanaged, IBaseNumber<T>
		where TP1 : IPointer<TP1>
		where TP2 : IPointer<TP2>
	{
		actualCopied = 0;
		if (!CheckType<TP1>() || !CheckType<TP2>())
			return false;
		var srcSpan = source.Pointer.FromGenericCpu().AsSpan<UnsignedInt8, TP1>(source);
		var dstSpan = destination.Pointer.FromGenericCpu().AsSpan<UnsignedInt8, TP2>(destination);
		int copy = Math.Min(srcSpan.Length, dstSpan.Length);
		srcSpan[..copy].CopyTo(dstSpan[..copy]);
		actualCopied = copy;
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static unsafe void MemoryCopy2D(void* srcPtr, void* dstPtr, long srcLD, long dstLD, long width, long height)
	{
		byte* src = (byte*)srcPtr, dst = (byte*)dstPtr, end = src + srcLD * width;
		for (; src < end; src += srcLD, dst += dstLD)
		{
			Buffer.MemoryCopy(src, dst, height, height);
		}
	}

	/// <inheritdoc/>
	public virtual bool MemoryCopy2D<T, TP1, TP2>(PointerSegment<TP1> source, long sourceLD, PointerSegment<TP2> destination, long destinationLD, long height, long width, out long copyWidth)
		where T : unmanaged, IBaseNumber<T>
		where TP1 : IPointer<TP1>
		where TP2 : IPointer<TP2>
	{
		copyWidth = 0;
		if (!CheckType<TP1>() || !CheckType<TP2>())
			return false;
		copyWidth = Math.Min((source.LengthInBytes + (sourceLD - height)) / sourceLD, (destination.LengthInBytes + (destinationLD - height)) / destinationLD);
		copyWidth = Math.Min(copyWidth, width);
		long srcOff = source.OffsetInBytes, dstOff = destination.OffsetInBytes;
		CpuMemoryPointer src = source.Pointer.FromGenericCpu(), dst = destination.Pointer.FromGenericCpu();
		unsafe
		{
			MemoryCopy2D(src.NativePointer(srcOff), dst.NativePointer(dstOff), sourceLD, destinationLD, width, height);
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static unsafe void StridedCopy<T>(T* src, T* dst, int srcInc, int dstInc, int count) where T : unmanaged, IBaseNumber<T>
	{
		T* end = src + srcInc * count;
		if (srcInc == 1)
		{
			for (; src < end; src++, dst += dstInc)
			{
				*dst = *src;
			}
		}
		else if (dstInc == 1)
		{
			for (; src < end; src += srcInc, dst++)
			{
				*dst = *src;
			}
		}
		else
		{
			for (; src < end; src += srcInc, dst += dstInc)
			{
				*dst = *src;
			}
		}
	}

	/// <inheritdoc/>
	public virtual bool StridedCopy<T, TP1, TP2>(PointerSegment<TP1> source, long strideSource, PointerSegment<TP2> destination, long strideDestination, out long actualCopied)
		where T : unmanaged, IBaseNumber<T>
		where TP1 : IPointer<TP1>
		where TP2 : IPointer<TP2>
	{
		actualCopied = 0;
		if (!CheckType<TP1>() || !CheckType<TP2>())
			return false;
		actualCopied = Math.Min((source.LengthInBytes / T.Size - 1) / strideSource + 1, (destination.LengthInBytes / T.Size - 1) / strideDestination + 1);
		unsafe
		{
			StridedCopy((T*)source.Pointer.FromGenericCpu().NativePointer(source.OffsetInBytes), (T*)destination.Pointer.FromGenericCpu().NativePointer(destination.OffsetInBytes), (int)strideSource, (int)strideDestination, (int)actualCopied);
		}
		return true;
	}
	#endregion
}