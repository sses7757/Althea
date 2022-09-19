using System.Runtime.CompilerServices;

using Althea.Backend.Storage;


namespace Althea.Backend.Cuda;

/// <summary>
/// The interface for statically get the memory allocation position's GPU ID.
/// </summary>
public interface IGpuId
{
	/// <summary>
	/// When implemented by a derived class, statically get the memory allocation position's GPU ID.
	/// </summary>
	abstract static short GpuId { get; }
}

internal readonly record struct CudaMemoryPointer(IntPtr Pointer, long LengthInBytes) : IMemoryPointer<CudaMemoryPointer>
{
	static StorageLocation IPointer<CudaMemoryPointer>.Location => new(LocationType.GpuRam, 0);

	static CudaMemoryPointer IPointer<CudaMemoryPointer>.Default => default;

	/// <inheritdoc/>
	public override readonly string ToString() => $"{nameof(CudaMemoryPointer)} {{ {nameof(Pointer)} = 0x{this.Pointer:X}, {nameof(LengthInBytes)} = {this.LengthInBytes} }}";

	#region extension
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool IsValid() => this.Pointer != default;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal readonly TP AsGeneric<TP>() where TP : IPointer<TP> => Unsafe.As<CudaMemoryPointer, TP>(ref Unsafe.AsRef(in this));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal readonly CudaMemoryPointer<TD> Generic<TD>() where TD : IGpuId => Unsafe.As<CudaMemoryPointer, CudaMemoryPointer<TD>>(ref Unsafe.AsRef(in this));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal readonly unsafe T* UnmangedPointer<T>(long offset = 0) where T : unmanaged, IBaseNumber<T> => (T*)this.Pointer.ToPointer() + offset;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal readonly unsafe void* OffsetPointer(long offset = 0) => (byte*)this.Pointer.ToPointer() + offset;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal readonly unsafe void* NativePointer<TP>(PointerSegment<TP> ps) where TP : IPointer<TP> => (byte*)this.Pointer.ToPointer() + ps.OffsetInBytes;
	#endregion
}

/// <summary>
/// An simple implementation of <see cref="IMemoryPointer{TSelf}"/> on a unified GPU memory.
/// </summary>
/// <param name="Pointer">The native pointer of this <see cref="CudaMemoryPointer{TD}"/> as a <see cref="IntPtr"/>.</param>
/// <param name="LengthInBytes">The original length of this pointer's underlying storage in bytes.</param>
public readonly record struct CudaMemoryPointer<TD>(IntPtr Pointer, long LengthInBytes) : IMemoryPointer<CudaMemoryPointer<TD>> where TD : IGpuId
{
	#region basic
	/// <inheritdoc/>
	public static StorageLocation Location => new(LocationType.GpuRam, TD.GpuId);

	static CudaMemoryPointer<TD> IPointer<CudaMemoryPointer<TD>>.Default => default;

	/// <summary>
	/// Create a new <see cref="CudaMemoryPointer{TD}"/> with given allocated <paramref name="pointer"/> on managed memory and corresponding <paramref name="length"/> in <typeparamref name="T"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged type as the data type</typeparam>
	/// <param name="pointer">The allocated pointer on managed memory</param>
	/// <param name="length">The length in <typeparamref name="T"/></param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static CudaMemoryPointer<TD> Create<T>(IntPtr pointer, long length) where T : unmanaged, IBaseNumber<T> => new(pointer, length * T.Size);

	/// <inheritdoc/>
	public override readonly string ToString() => $"{nameof(CudaMemoryPointer)}<{typeof(TD).Name}> {{ {nameof(Pointer)} = 0x{this.Pointer:X}, {nameof(LengthInBytes)} = {this.LengthInBytes} }}";
	#endregion
}