using System.Runtime.CompilerServices;

using Althea.Numerics;


namespace Althea.Backend.Storage
{
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
	public readonly struct CpuMemoryPointer : IMemoryPointer<CpuMemoryPointer>
	{
		#region basic
		/// <summary>
		/// The native pointer of this <see cref="CpuMemoryPointer"/> as a <see cref="IntPtr"/>.
		/// </summary>
		public IntPtr Pointer { get; }

		/// <summary>
		/// Get the original length of this pointer's underlying storage in bytes.
		/// </summary>
		public long LengthInBytes { get; }

		/// <inheritdoc/>
		public static StorageLocation Location => new(LocationType.CpuRam, 0);

		/// <inheritdoc/>
		public static CpuMemoryPointer Default => default;

		/// <summary>
		/// Create a new <see cref="CpuMemoryPointer"/> with given allocated <paramref name="pointer"/> and corresponding <paramref name="length"/>.
		/// </summary>
		/// <param name="pointer">The allocated pointer</param>
		/// <param name="length">The length in bytes</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public CpuMemoryPointer(IntPtr pointer, long length)
		{
			this.Pointer = pointer; this.LengthInBytes = length;
		}

		/// <summary>
		/// Create a new <see cref="CpuMemoryPointer"/> with given allocated <paramref name="pointer"/> on managed memory and corresponding <paramref name="length"/> in <typeparamref name="T"/>
		/// </summary>
		/// <typeparam name="T">Any unmanaged type as the data type</typeparam>
		/// <param name="pointer">The allocated pointer on managed memory</param>
		/// <param name="length">The length in <typeparamref name="T"/></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static CpuMemoryPointer Create<T>(IntPtr pointer, long length) where T : unmanaged, IBaseNumber<T> => new(pointer, length * T.Size);
		#endregion

		#region equality
		/// <inheritdoc/>
		public bool Equals(CpuMemoryPointer other) => this.Pointer == other.Pointer;

		/// <inheritdoc/>
		public override bool Equals(object? obj) => obj is CpuMemoryPointer ptr && this.Pointer == ptr.Pointer;

		/// <inheritdoc/>
		public override int GetHashCode() => this.Pointer.GetHashCode();

		/// <inheritdoc/>
		public static bool operator ==(CpuMemoryPointer left, CpuMemoryPointer right) => left.Equals(right);

		/// <inheritdoc/>
		public static bool operator !=(CpuMemoryPointer left, CpuMemoryPointer right) => !left.Equals(right);
		#endregion

		#region string
		static string IMainPropertyFormattable<CpuMemoryPointer>.StringMain => nameof(CpuMemoryPointer);

		static IEnumerable<string> IMainPropertyFormattable<CpuMemoryPointer>.PropertyNames => new[] { nameof(Pointer) };

		IEnumerable<object?> IMainPropertyFormattable<CpuMemoryPointer>.PropertyValues => new[] { this.Pointer.ToString("X") };

		/// <summary>
		/// Get the string representation of this <see cref="CpuMemoryPointer"/>
		/// </summary>
		/// <returns>The string representation of this <see cref="CpuMemoryPointer"/></returns>
		public override string ToString() => IMainPropertyFormattable<CpuMemoryPointer>.ToString(in this);
		#endregion

		#region extension
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal TP AsGeneric<TP>() where TP : IPointer<TP> => Unsafe.As<CpuMemoryPointer, TP>(ref Unsafe.AsRef(in this));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal unsafe T* UnmangedPointer<T>(long offset = 0) where T : unmanaged, IBaseNumber<T> => (T*)this.Pointer.ToPointer() + offset;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal unsafe void* NativePointer(long offset = 0) => (byte*)this.Pointer.ToPointer() + offset;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal unsafe IntPtr OffsetPointer(long offset = 0) => (IntPtr)((byte*)this.Pointer.ToPointer() + offset);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal unsafe Span<T> AsSpan<T>(long offset = 0, int length = 0) where T : unmanaged, IBaseNumber<T> => new(this.UnmangedPointer<T>(offset), length);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal Span<T> AsSpan<T, TP>(PointerSegment<TP> pointerSegment) where T : unmanaged, IBaseNumber<T> where TP : IPointer<TP> => this.AsSpan<T>(pointerSegment.OffsetInBytes / T.Size, (int)(pointerSegment.LengthInBytes / T.Size));
		#endregion
	}

	#region extension methods
	internal static partial class CpuMemoryPointerExtension
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static CpuMemoryPointer FromGeneric<TP>(this TP pointer) where TP : IPointer<TP> => Unsafe.As<TP, CpuMemoryPointer>(ref pointer);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static unsafe PointerSegment<CpuMemoryPointer> AsPointerSegment<T>(this Span<T> span, T* pointer) where T : unmanaged, IBaseNumber<T> => new(CpuMemoryPointer.Create<T>((IntPtr)pointer, span.Length));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static unsafe PointerSegment<CpuMemoryPointer> AsPointerSegment<T>(this ReadOnlySpan<T> span, T* pointer) where T : unmanaged, IBaseNumber<T> => new(CpuMemoryPointer.Create<T>((IntPtr)pointer, span.Length));
	}
	#endregion
}