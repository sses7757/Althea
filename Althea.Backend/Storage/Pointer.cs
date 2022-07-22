using System.Runtime.CompilerServices;


namespace Althea.Backend.Storage;

#region abstract
/// <summary>
/// The interface for an immutable pointer at any possible memory storage which can be described by a <see cref="IntPtr"/>.
/// </summary>
public interface IMemoryPointer<TSelf> : IPointer<TSelf> where TSelf : IMemoryPointer<TSelf>
{
	/// <summary>
	/// When implemented by a derived class, get the raw pointer of this <see cref="IMemoryPointer{TSelf}"/> as a <see cref="IntPtr"/>.
	/// </summary>
	IntPtr Pointer { get; }

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	bool ICheckValid.IsValid() => this.Pointer != default && this.LengthInBytes > 0;
}
#endregion

/// <summary>
/// An simple implementation of <see cref="IMemoryPointer{TSelf}"/> on a unified CPU memory.
/// </summary>
/// <param name="Pointer">The native pointer of this <see cref="CpuMemoryPointer"/> as a <see cref="IntPtr"/>.</param>
/// <param name="LengthInBytes">The original length of this pointer's underlying storage in bytes.</param>
public readonly record struct CpuMemoryPointer(IntPtr Pointer, long LengthInBytes) : IMemoryPointer<CpuMemoryPointer>
{
	#region basic
	/// <inheritdoc/>
	public static StorageLocation Location => new(LocationType.CpuRam, 0);

	/// <inheritdoc/>
	public static CpuMemoryPointer Default => default;

	/// <summary>
	/// Create a new <see cref="CpuMemoryPointer"/> with given allocated <paramref name="pointer"/> on managed memory and corresponding <paramref name="length"/> in <typeparamref name="T"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged type as the data type</typeparam>
	/// <param name="pointer">The allocated pointer on managed memory</param>
	/// <param name="length">The length in <typeparamref name="T"/></param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static CpuMemoryPointer Create<T>(IntPtr pointer, long length) where T : unmanaged, IBaseNumber<T> => new(pointer, length * T.Size);
	#endregion

	#region string
	static string IMainPropertyFormattable<CpuMemoryPointer>.StringMain => nameof(CpuMemoryPointer);

	static IEnumerable<string> IMainPropertyFormattable<CpuMemoryPointer>.PropertyNames => new[] { nameof(Pointer) };

	IEnumerable<object?> IMainPropertyFormattable<CpuMemoryPointer>.PropertyValues => new[] { this.Pointer.ToString("X") };
	#endregion

	#region extension
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TP AsGeneric<TP>() where TP : IPointer<TP> => Unsafe.As<CpuMemoryPointer, TP>(ref Unsafe.AsRef(in this));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe T* UnmangedPointer<T>(long offset = 0) where T : unmanaged, IBaseNumber<T> => (T*)this.Pointer.ToPointer() + offset;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe void* NativePointer(long offset = 0) => (byte*)this.Pointer.ToPointer() + offset;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe void* OffsetPointer(long offset = 0) => (byte*)this.Pointer.ToPointer() + offset;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe Span<T> AsSpan<T>(long offset = 0, int length = 0) where T : unmanaged, IBaseNumber<T> => new(this.UnmangedPointer<T>(offset), length);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal Span<T> AsSpan<T, TP>(PointerSegment<TP> pointerSegment) where T : unmanaged, IBaseNumber<T> where TP : IPointer<TP> => this.AsSpan<T>(pointerSegment.OffsetInBytes / T.Size, (int)(pointerSegment.LengthInBytes / T.Size));
	#endregion
}

/// <summary>
/// An simple implementation of <see cref="IMemoryPointer{TSelf}"/> on a unified GPU memory.
/// </summary>
/// <param name="Pointer">The native pointer of this <see cref="GpuMemoryPointer"/> as a <see cref="IntPtr"/>.</param>
/// <param name="LengthInBytes">The original length of this pointer's underlying storage in bytes.</param>
public readonly record struct GpuMemoryPointer(IntPtr Pointer, long LengthInBytes) : IMemoryPointer<GpuMemoryPointer>
{
	#region basic
	/// <inheritdoc/>
	public static StorageLocation Location => new(LocationType.GpuRam, 0);

	/// <inheritdoc/>
	public static GpuMemoryPointer Default => default;

	/// <summary>
	/// Create a new <see cref="GpuMemoryPointer"/> with given allocated <paramref name="pointer"/> on managed memory and corresponding <paramref name="length"/> in <typeparamref name="T"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged type as the data type</typeparam>
	/// <param name="pointer">The allocated pointer on managed memory</param>
	/// <param name="length">The length in <typeparamref name="T"/></param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static GpuMemoryPointer Create<T>(IntPtr pointer, long length) where T : unmanaged, IBaseNumber<T> => new(pointer, length * T.Size);
	#endregion

	#region string
	static string IMainPropertyFormattable<GpuMemoryPointer>.StringMain => nameof(GpuMemoryPointer);

	static IEnumerable<string> IMainPropertyFormattable<GpuMemoryPointer>.PropertyNames => new[] { nameof(Pointer) };

	IEnumerable<object?> IMainPropertyFormattable<GpuMemoryPointer>.PropertyValues => new[] { this.Pointer.ToString("X") };
	#endregion

	#region extension
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TP AsGeneric<TP>() where TP : IPointer<TP> => Unsafe.As<GpuMemoryPointer, TP>(ref Unsafe.AsRef(in this));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe T* UnmangedPointer<T>(long offset = 0) where T : unmanaged, IBaseNumber<T> => (T*)this.Pointer.ToPointer() + offset;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe void* NativePointer(long offset = 0) => (byte*)this.Pointer.ToPointer() + offset;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe void* OffsetPointer(long offset = 0) => (byte*)this.Pointer.ToPointer() + offset;
	#endregion
}

#region extension methods
internal static partial class MemoryPointerExtension
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static CpuMemoryPointer FromGenericCpu<TP>(this TP pointer) where TP : IPointer<TP> => Unsafe.As<TP, CpuMemoryPointer>(ref pointer);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static GpuMemoryPointer FromGenericGpu<TP>(this TP pointer) where TP : IPointer<TP> => Unsafe.As<TP, GpuMemoryPointer>(ref pointer);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static unsafe PointerSegment<CpuMemoryPointer> AsPointerSegment<T>(this Span<T> span, T* pointer) where T : unmanaged, IBaseNumber<T> => new(CpuMemoryPointer.Create<T>((IntPtr)pointer, span.Length));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static unsafe PointerSegment<CpuMemoryPointer> AsPointerSegment<T>(this ReadOnlySpan<T> span, T* pointer) where T : unmanaged, IBaseNumber<T> => new(CpuMemoryPointer.Create<T>((IntPtr)pointer, span.Length));
}
#endregion