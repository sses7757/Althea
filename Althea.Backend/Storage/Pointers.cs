using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

using Althea.Linq;
using Althea.NativeTypes;
using Althea.Resources;
using Althea.Storage;


namespace Althea.Backend.Storage
{
	#region internal usage
	internal sealed class ManagedPureStorage<T> : PureOrMixedStorage<T> where T : unmanaged
	{
		private readonly PointerSegment pointerSegment;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal unsafe ManagedPureStorage(void* pointer, long length) : base(stackalloc StorageLocation[] { new(LocationType.CpuRam, 0) }, stackalloc[] { length })
		{
			this.pointerSegment = new(MemoryPointer.Create<T>((IntPtr)pointer, length));
		}

		// no disposition
		protected override void Dispose(bool invokedByUser) { }

		public override PointerSegment this[int index] {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => index == 0 ? this.pointerSegment : throw new ArgumentOutOfRangeException(nameof(index), index, Parameter.InvalidValue);
		}

		public override int Count {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => 1;
		}

		public override bool Equals(Storage<T>? obj) => obj is ManagedPureStorage<T> m && ((MemoryPointer)this.pointerSegment.Pointer).Pointer == ((MemoryPointer)m.pointerSegment.Pointer).Pointer;

		public override bool IsValid() => this.pointerSegment.LengthInBytes > 0;

		public override bool IsOffsetValid(long offset, long newLength = 0) => offset >= 0 && newLength >= 0 && offset + newLength < this.pointerSegment.LengthInBytes;

		public override ActualStorage<T> CreateAlike() => throw new InvalidOperationException();

		public override ActualStorage<TOut> CreateAlike<TOut>() => throw new InvalidOperationException();

		public override ReferenceStorage<T> MakeReference(long offset = 0, long newLength = 0) => throw new InvalidOperationException();

		public override ReferenceStorage<TOut> As<TOut>() => throw new InvalidOperationException();

		public override ActualStorage<T> Clone() => throw new InvalidOperationException();
	}
	#endregion

	/// <summary>
	/// An implementation of <see cref="IMemoryPointer"/>. This is implemented as a class to prevent boxing and unboxing.
	/// </summary>
	public class MemoryPointer : IMemoryPointer
	{
		#region basic
		/// <summary>
		/// The native pointer of this <see cref="MemoryPointer"/> as a <see cref="IntPtr"/>
		/// </summary>
		public IntPtr Pointer { get; }

		/// <summary>
		/// Get the original length of this pointer's underlying storage in bytes
		/// </summary>
		public long LengthInBytes { get; }

		/// <summary>
		/// The storage location of this <see cref="MemoryPointer"/> as a <see cref="StorageLocation"/>
		/// </summary>
		public StorageLocation Location { get; }

		/// <summary>
		/// Create a new <see cref="MemoryPointer"/> with given allocated <paramref name="pointer"/> and corresponding <paramref name="length"/>
		/// </summary>
		/// <param name="pointer">The allocated pointer</param>
		/// <param name="length">The length in bytes</param>
		/// <param name="location">The location of this pointer</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="location"/>'s <see cref="LocationType"/> is not a memory type</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public MemoryPointer(IntPtr pointer, long length, StorageLocation location)
		{
			if (location.Type.GetClassification() != LocationTypeExtension.ClassMemory)
				throw new ArgumentOutOfRangeException(nameof(location), location, Parameter.UnexpectedValue);
			this.Pointer = pointer; this.LengthInBytes = length; this.Location = location;
		}

		/// <summary>
		/// Create a new <see cref="MemoryPointer"/> with given allocated <paramref name="pointer"/> on managed memory and corresponding <paramref name="length"/> in <typeparamref name="T"/>
		/// </summary>
		/// <typeparam name="T">Any unmanaged type as the data type</typeparam>
		/// <param name="pointer">The allocated pointer on managed memory</param>
		/// <param name="length">The length in <typeparamref name="T"/></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static MemoryPointer Create<T>(IntPtr pointer, long length) where T : unmanaged
		{
			return new(pointer, length * Const<T>.SizeT, new(LocationType.CpuRam, 0));
		}

		/// <summary>
		/// Indicates whether the current object is equal to another object of the same type.
		/// </summary>
		/// <param name="other"> An object to compare with this object.</param>
		/// <returns>True if the current object is equal to the other parameter; otherwise, false.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(IPointer? other)
		{
			if (other is MemoryPointer mp)
				return this.Pointer == mp.Pointer && this.Location == mp.Location;
			else
				return false;
		}

		/// <summary>
		/// Get the hash code of this <see cref="MemoryPointer"/>
		/// </summary>
		/// <returns>The hash code of this <see cref="MemoryPointer"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode() => HashCode.Combine(this.Pointer, this.Location);

		/// <summary>
		/// Get the string representation of this <see cref="MemoryPointer"/>
		/// </summary>
		/// <returns>The string representation of this <see cref="MemoryPointer"/></returns>
		public override string ToString() => ((IMainPropertyFormat)this).ToString();
		#endregion
	}

	/// <summary>
	/// An implementation of <see cref="IStreamPointer"/> for files. This is implemented as a class to prevent boxing and unboxing.
	/// </summary>
	public class StreamPointer : IStreamPointer
	{
		#region basic
		/// <summary>
		/// Get the native stream this <see cref="StreamPointer"/> as a <see cref="Stream"/>.
		/// </summary>
		public Stream NativeStream { get; }

		/// <summary>
		/// The storage location of this <see cref="StreamPointer"/> as a <see cref="StorageLocation"/>
		/// </summary>
		public StorageLocation Location { get; }

		/// <summary>
		/// Create this <see cref="StreamPointer"/> by given <see cref="Stream"/>
		/// </summary>
		/// <param name="stream">The given <see cref="Stream"/></param>
		/// <param name="location">The <see cref="StorageLocation"/> of this <see cref="StreamPointer"/></param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="location"/>'s <see cref="LocationType"/> is not a stream type</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public StreamPointer(Stream stream, StorageLocation location)
		{
			if (location.Type.GetClassification() != LocationTypeExtension.ClassStream)
				throw new ArgumentOutOfRangeException(nameof(location), location, Parameter.UnexpectedValue);
			this.NativeStream = stream; this.Location = location;
		}

		/// <summary>
		/// When implemented by a derived class, dispose unmanaged and managed resources held by this <see cref="StreamPointer"/>
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public virtual void Dispose()
		{
			this.NativeStream.Dispose();
			GC.SuppressFinalize(this);
		}

		string IMainPropertyFormat.StringMain {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.NativeStream.ToString();
		}

		/// <summary>
		/// Indicates whether the current object is equal to another object of the same type.
		/// </summary>
		/// <param name="other"> An object to compare with this object.</param>
		/// <returns>True if the current object is equal to the other parameter; otherwise, false.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(IPointer? other)
		{
			if (other is StreamPointer mp)
				return this.NativeStream == mp.NativeStream && this.Location == mp.Location;
			else
				return false;
		}

		/// <summary>
		/// Get the hash code of this <see cref="StreamPointer"/>
		/// </summary>
		/// <returns>The hash code of this <see cref="StreamPointer"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode() => HashCode.Combine(this.NativeStream, this.Location);

		/// <summary>
		/// Get the string representation of this <see cref="StreamPointer"/>
		/// </summary>
		/// <returns>The string representation of this <see cref="StreamPointer"/></returns>
		public override string ToString() => ((IMainPropertyFormat)this).ToString();
		#endregion
	}


	/// <summary>
	/// An implementation of <see cref="Stream"/> for local files.
	/// </summary>
	public class FileStream : Stream
	{
		#region basic
		/// <summary>
		/// The underlying <see cref="System.IO.FileStream"/>
		/// </summary>
		protected readonly System.IO.FileStream stream;

		/// <summary>
		/// Get a <see cref="bool"/> indicating whether user can write data to this <see cref="Stream"/>.
		/// </summary>
		public override bool CanWrite {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.stream.CanWrite;
		}

		/// <summary>
		/// Get or set the position (offset) in bytes of this <see cref="FileStream"/>
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">If the value to be set is not less than <see cref="Stream.Length"/></exception>
		public override long Position {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.stream.Position;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => this.stream.Seek(value, SeekOrigin.Begin);
		}

		/// <summary>
		/// Get a <see cref="bool"/> indicating whether this <see cref="Stream"/> can transfer data with managed C# memory directly or not, always return true.
		/// </summary>
		public override bool CanTransferWithManaged {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => true;
		}

		private static readonly StorageLocation supportedLocation = new(LocationType.CpuRam, 0);

		/// <summary>
		/// <b>Statically</b> get the supported data transfer locations represented by <see cref="StorageLocation"/>s of this <see cref="Stream"/>
		/// </summary>
		protected override IReadOnlyList<StorageLocation> SupportedTransfers {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
		} = new[] { supportedLocation };

		/// <summary>
		/// <b>Statically</b> get a <see cref="bool"/> indicating whether data transfer with given <paramref name="location"/> is supported by this <see cref="Stream"/>. The default implementation utilizes the <see cref="SupportedTransfers"/>.
		/// </summary>
		/// <param name="location">The given <see cref="StorageLocation"/> to check transfer supporting</param>
		/// <returns>Whether data transfer with <paramref name="location"/> is supported or not</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override bool IsSupported(StorageLocation location) => location == supportedLocation;

		/// <summary>
		/// Create a new <see cref="FileStream"/> with given <see cref="Uri"/> of file
		/// </summary>
		/// <param name="uri">The given <see cref="Uri"/> of file scheme</param>
		/// <param name="length">The initial length in bytes</param>
		/// <param name="readOnly">Shall the file at <paramref name="uri"/> be opened as read-only or not. If this is true, then <paramref name="length"/> will be set to the value of the actual file length.</param>
		/// <exception cref="NotSupportedException">If the scheme of <paramref name="uri"/> is not file or the stream cannot be created by given <paramref name="uri"/></exception>
		/// <exception cref="System.IO.IOException">If other I/O error occurred</exception>
		/// <exception cref="UnauthorizedAccessException">If the give path in <paramref name="uri"/> cannot be created or overwritten</exception>
		public FileStream(Uri uri, long length, bool readOnly = false) : base(length)
		{
			if (uri.GetScheme() != UriScheme.File)
				throw new NotSupportedException(Support.Location);
			var platform = Environment.OSVersion.Platform;
			if (platform == PlatformID.Other)
				throw new NotSupportedException(Support.OperationSystem);
			// check
			string path = uri.LocalPath;
			if (File.Exists(path))
			{
				var flags = File.GetAttributes(path);
				if ((flags & (/*System.IO.FileAttributes.ReadOnly | */FileAttributes.System | FileAttributes.Directory)) != 0)
					throw new NotSupportedException(Support.Location);
			}
			else if (readOnly)
				throw new ArgumentException(Parameter.InvalidValue, nameof(readOnly));
			if (!readOnly)
			{
				string folder = Path.GetDirectoryName(path) ?? "";
				if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
				{
					Directory.CreateDirectory(folder);
				}
			}
			// create
			this.stream = new System.IO.FileStream(path, FileMode.OpenOrCreate, readOnly ? FileAccess.Read : FileAccess.ReadWrite);
			if (!readOnly)
			{
				this.stream.SetLength(length);
				this.stream.Flush();
			}
		}

		/// <summary>
		/// Actually release the unmanaged (and possibly managed) resources held by this class
		/// </summary>
		/// <param name="disposeManaged">Dispose managed resources or not</param>
		protected override void Dispose(bool disposeManaged)
		{
			this.stream.Dispose();
			if (this.stream.CanWrite)
				File.Delete(this.stream.Name);
		}

		/// <summary>
		/// Get the string representation of this <see cref="Stream"/>.
		/// </summary>
		/// <returns>The string representation of this <see cref="Stream"/></returns>
		public override string ToString() => this.stream.Name;
		#endregion

		#region implementations
		/// <summary>
		/// Clears all buffers for this stream and causes any buffered data to be written to the underlying device.
		/// </summary>
		/// <exception cref="IOException">If a general I/O error occurred</exception>
		/// <exception cref="ObjectDisposedException">If this is already disposed</exception>
		/// <exception cref="UnauthorizedAccessException">If this <see cref="Stream"/> was created read-only</exception>
		public override void Flush()
		{
			if (this.Disposed)
				throw new ObjectDisposedException(nameof(FileStream));
			if (!this.stream.CanWrite)
				throw new UnauthorizedAccessException();
			this.stream.Flush();
		}

		/// <summary>
		/// Check the parameters of <see cref="ToMemory(PointerSegment)"/>
		/// </summary>
		/// <returns>The <see cref="PointerSegment.Pointer"/> as a <see cref="IMemoryPointer"/></returns>
		protected IMemoryPointer ToMemoryCheck(PointerSegment memory)
		{
			if (!memory.IsValid())
				throw new ArgumentNullException(nameof(memory));
			if (this.IsSupported(memory.Location))
				throw new NotSupportedException(Support.Location);
			if (memory.Pointer is not IMemoryPointer mp)
				throw new NotSupportedException(Support.Location);
			if (this.Disposed)
				throw new ObjectDisposedException(nameof(FileStream));
			if (memory.LengthInBytes + this.Position > this.Length)
				throw new ArgumentException(Parameter.WrongSize, nameof(memory));
			return mp;
		}

		/// <summary>
		/// Check the parameters of <see cref="ToManged{T}(Span{T})"/>
		/// </summary>
		protected void ToMangedCheck<T>(Span<T> managed) where T : unmanaged
		{
			if (!this.CanTransferWithManaged)
				throw new NotSupportedException(Support.Location);
			if (this.Disposed)
				throw new ObjectDisposedException(nameof(FileStream));
			if ((long)managed.Length * Const<T>.SizeT + this.Position > this.Length)
				throw new ArgumentException(Parameter.WrongSize, nameof(managed));
		}

		/// <summary>
		/// Read data from this <see cref="Stream"/> started from <see cref="Position"/> byte and write them to the given <see cref="PointerSegment"/> <paramref name="memory"/>.
		/// </summary>
		/// <param name="memory">The <see cref="PointerSegment"/> to write to</param>
		/// <remarks>When finished, the <see cref="Position"/> shall be advanced by the number of bytes read.</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="memory"/> is not valid</exception>
		/// <exception cref="ArgumentException">If <paramref name="memory"/>.<see cref="PointerSegment.LengthInBytes">Length</see> exceeds the boundary of this <see cref="Stream"/></exception>
		/// <exception cref="NotSupportedException">If <paramref name="memory"/>.<see cref="PointerSegment.Location">Location</see> is not supported</exception>
		/// <exception cref="IOException">If a general I/O error occurred</exception>
		/// <exception cref="ObjectDisposedException">If this is already disposed</exception>
		public override void ToMemory(PointerSegment memory)
		{
			var mp = ToMemoryCheck(memory);
			this.ToManged(mp.AsSpan<byte>(memory));
		}

		/// <summary>
		/// Read data from this <see cref="Stream"/> started from <see cref="Position"/> and write them to the given <paramref name="managed"/> memory as a<see cref="Span{T}"/>.
		/// </summary>
		/// <param name="managed">The managed memory as a <see cref="Span{T}"/> to write into</param>
		/// <remarks>When finished, the <see cref="Position"/> shall be advanced by the number of bytes read.</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="managed"/> is not valid (for example, has zero length)</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="managed"/>'s length exceeds the boundary</exception>
		/// <exception cref="NotSupportedException">If <see cref="CanTransferWithManaged"/> is false</exception>
		/// <exception cref="IOException">If a general I/O error occurred</exception>
		/// <exception cref="ObjectDisposedException">If this is already disposed</exception>
		public override void ToManged<T>(Span<T> managed)
		{
			ToMangedCheck(managed);
			this.stream.Read(managed.UncheckAs<T, byte>());
		}

		/// <summary>
		/// Check the parameters of <see cref="FromMemory(PointerSegment)"/>
		/// </summary>
		/// <returns>The <see cref="PointerSegment.Pointer"/> as a <see cref="IMemoryPointer"/></returns>
		protected IMemoryPointer FromMemoryCheck(PointerSegment memory)
		{
			if (this.Disposed)
				throw new ObjectDisposedException(nameof(FileStream));
			if (!this.stream.CanWrite)
				throw new UnauthorizedAccessException(Resource.CannotWrite);
			if (!memory.IsValid())
				throw new ArgumentNullException(nameof(memory));
			if (this.IsSupported(memory.Location))
				throw new NotSupportedException(Support.Location);
			if (memory.Pointer is not IMemoryPointer mp)
				throw new NotSupportedException(Support.Location);
			if (memory.LengthInBytes + this.Position > this.Length)
				throw new ArgumentException(Parameter.WrongSize, nameof(memory));
			return mp;
		}

		/// <summary>
		/// Check the parameters of <see cref="FromManged{T}(ReadOnlySpan{T})"/>
		/// </summary>
		protected void FromManagedCheck<T>(ReadOnlySpan<T> managed) where T : unmanaged
		{
			if (!this.CanTransferWithManaged)
				throw new NotSupportedException(Support.Location);
			if (!this.stream.CanWrite)
				throw new UnauthorizedAccessException();
			if (this.Disposed)
				throw new ObjectDisposedException(nameof(FileStream));
			if ((long)managed.Length * Const<T>.SizeT + this.Position > this.Length)
				throw new ArgumentException(Parameter.WrongSize, nameof(managed));
		}

		/// <summary>
		/// Read data from the given <see cref="PointerSegment"/> <paramref name="memory"/> and write them to this <see cref="Stream"/> started from <see cref="Position"/> byte.
		/// </summary>
		/// <param name="memory">The <see cref="PointerSegment"/> to read from</param>
		/// <remarks>When finished, the <see cref="Position"/> shall be advanced by the number of bytes written.</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="memory"/> is not valid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="memory"/>.<see cref="PointerSegment.LengthInBytes">Length</see> exceeds the boundary of this <see cref="Stream"/></exception>
		/// <exception cref="NotSupportedException">If <paramref name="memory"/>.<see cref="PointerSegment.Location">Location</see> is not supported</exception>
		/// <exception cref="IOException">If a general I/O error occurred</exception>
		/// <exception cref="ObjectDisposedException">If this is already disposed</exception>
		/// <exception cref="UnauthorizedAccessException">If this <see cref="Stream"/> was created read-only</exception>
		public override void FromMemory(PointerSegment memory)
		{
			var mp = FromMemoryCheck(memory);
			this.FromManged<byte>(mp.AsSpan<byte>(memory));
		}

		/// <summary>
		/// Read data from the given <paramref name="managed"/> memory as a<see cref="Span{T}"/> and write them to this <see cref="Stream"/> started from <see cref="Position"/>.
		/// </summary>
		/// <param name="managed">The managed memory as a <see cref="ReadOnlySpan{T}"/> to read from</param>
		/// <remarks>When finished, the <see cref="Position"/> shall be advanced by the number of bytes written.</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="managed"/> is not valid (for example, has zero length)</exception>
		/// <exception cref="ArgumentException">If <paramref name="managed"/>'s length exceeds the boundary</exception>
		/// <exception cref="NotSupportedException">If <see cref="CanTransferWithManaged"/> is false</exception>
		/// <exception cref="IOException">If a general I/O error occurred</exception>
		/// <exception cref="ObjectDisposedException">If this is already disposed</exception>
		/// <exception cref="UnauthorizedAccessException">If this <see cref="Stream"/> was created read-only</exception>
		public override void FromManged<T>(ReadOnlySpan<T> managed)
		{
			FromManagedCheck(managed);
			this.stream.Write(managed.UncheckAs<T, byte>());
		}
		#endregion
	}


	#region extension methods
	internal static class ConcretePointersExtension
	{
		#region extension
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static unsafe T* UnmangedPointer<T>(this IMemoryPointer p, long offset = 0) where T : unmanaged => (T*)p.Pointer.ToPointer() + offset;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static unsafe void* NativePointer(this IMemoryPointer p, long offset = 0) => (byte*)p.Pointer.ToPointer() + offset;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static unsafe IntPtr OffsetPointer(this IMemoryPointer p, long offset = 0) => (IntPtr)((byte*)p.Pointer.ToPointer() + offset);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static unsafe Span<T> AsSpan<T>(this IMemoryPointer p, long offset = 0, int length = 0) where T : unmanaged => new(p.UnmangedPointer<T>(offset), length <= 0 ? checked((int)(p.LengthInBytes / sizeof(T))) : length);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static Span<T> AsSpan<T>(this IMemoryPointer p, PointerSegment pointerSegment) where T : unmanaged => p.AsSpan<T>(checked(pointerSegment.OffsetInBytes / Const<T>.SizeT), checked((int)(pointerSegment.LengthInBytes / Const<T>.SizeT)));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static unsafe PointerSegment AsPointerSegment<T>(this Span<T> span, T* pointer) where T : unmanaged => new(MemoryPointer.Create<T>((IntPtr)pointer, span.Length));
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static unsafe PointerSegment AsPointerSegment<T>(this ReadOnlySpan<T> span, T* pointer) where T : unmanaged => new(MemoryPointer.Create<T>((IntPtr)pointer, span.Length));
		#endregion


		#region cast
		public static readonly StorageLocation CpuAlone = new(LocationType.CpuRam, 0);
		public static readonly StorageLocation FileAlone = new(LocationType.Uri, (int)UriScheme.File);

		public const long INVALID = -1, NOT_SUPPORT = -2;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static long GetPointerOffsetManaged<T>(this PointerSegment pointer, out IMemoryPointer? memoryPointer, out IStreamPointer? streamPointer, bool @throw = true) where
			T : unmanaged
		{
			memoryPointer = null; streamPointer = null;
			// check first
			if (!pointer.IsValid() || pointer.OffsetInBytes % Const<T>.SizeT != 0 || pointer.LengthInBytes % Const<T>.SizeT != 0)
			{
				if (@throw)
					throw new ArgumentNullException(nameof(pointer));
				return INVALID;
			}
			// cast
			if (pointer.Location == CpuAlone && pointer.Pointer is IMemoryPointer mp)
			{
				memoryPointer = mp;
			}
			else if (pointer.Location == FileAlone && pointer.Pointer is IStreamPointer { NativeStream: FileStream } sp)
			{
				streamPointer = sp;
			}
			else
			{
				return NOT_SUPPORT;
			}
			return pointer.OffsetInBytes / Const<T>.SizeT;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static long GetPointerOffsetManaged(this PointerSegment pointer, out IMemoryPointer? memoryPointer, out IStreamPointer? streamPointer, bool @throw = true) => GetPointerOffsetManaged<byte>(pointer, out memoryPointer, out streamPointer, @throw);


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static long GetPointerOffsetCuda<T>(this PointerSegment pointer, out IMemoryPointer? memoryPointer, out IStreamPointer? streamPointer, bool @throw = true) where
			T : unmanaged
		{
			memoryPointer = null; streamPointer = null;
			// check first
			if (!pointer.IsValid() || pointer.OffsetInBytes % Const<T>.SizeT != 0 || pointer.LengthInBytes % Const<T>.SizeT != 0)
			{
				if (@throw)
					throw new ArgumentNullException(nameof(pointer));
				return INVALID;
			}
			// cast
			var loc = pointer.Location; var ptr = pointer.Pointer; var locType = loc.Type;
			if (((locType == LocationType.CpuRam) || (locType == LocationType.GpuRam && loc.LocationDetail == Cuda.CudaRuntime.CurrentDeviceID)) && ptr is IMemoryPointer mp)
			{
				memoryPointer = mp;
			}
			else if (loc == FileAlone && ptr is IStreamPointer { NativeStream: Cuda.Storage.CudaFileStream } sp)
			{
				streamPointer = sp;
			}
			else
			{
				return NOT_SUPPORT;
			}
			return pointer.OffsetInBytes / Const<T>.SizeT;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static long GetPointerOffsetCuda(this PointerSegment pointer, out IMemoryPointer? memoryPointer, out IStreamPointer? streamPointer, bool @throw = true) => GetPointerOffsetManaged<byte>(pointer, out memoryPointer, out streamPointer, @throw);
		#endregion


		#region copy
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void StreamAndMemoryCopy(long srcOff, long dstOff, long copy, PointerSegment source, PointerSegment destination, IMemoryPointer? srcMP, IStreamPointer? srcSP, IMemoryPointer? dstMP, IStreamPointer? dstSP)
		{
			if (srcMP is not null && dstSP is not null)
			{
				dstSP.NativeStream.Position = dstOff;
				dstSP.NativeStream.FromMemory(source.AsLength(copy));
			}
			else if (srcSP is not null && dstMP is not null)
			{
				srcSP.NativeStream.Position = srcOff;
				srcSP.NativeStream.ToMemory(destination.AsLength(copy));
			}
			else if (srcSP is not null && dstSP is not null)
			{
				srcSP.NativeStream.Position = srcOff;
				dstSP.NativeStream.Position = dstOff;
				srcSP.NativeStream.CopyTo(dstSP.NativeStream, copy);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Copy2DCheck(PointerSegment source, long sourceLD, PointerSegment destination, long destinationLD, long height, long width)
		{
			if (sourceLD == 0)
				throw new ArgumentOutOfRangeException(nameof(sourceLD), sourceLD, Parameter.MustPositive);
			if (destinationLD == 0)
				throw new ArgumentOutOfRangeException(nameof(destinationLD), destinationLD, Parameter.MustPositive);
			if (width == 0)
				throw new ArgumentOutOfRangeException(nameof(width), width, Parameter.MustPositive);
			if (height == 0)
				throw new ArgumentOutOfRangeException(nameof(height), height, Parameter.MustPositive);
			if (height > sourceLD || height > destinationLD)
				throw new ArgumentException(Parameter.InvalidValue, nameof(height));
			if (sourceLD * width > source.LengthInBytes)
				throw new ArgumentException(Parameter.WrongSize, nameof(source));
			if (destinationLD * width > destination.LengthInBytes)
				throw new ArgumentException(Parameter.WrongSize, nameof(destination));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void StreamAndMemoryCopy2D(long srcLD, long dstLD, long height, long width, long srcOff, long dstOff, PointerSegment source, PointerSegment destination, IMemoryPointer? srcMP, IStreamPointer? srcSP, IMemoryPointer? dstMP, IStreamPointer? dstSP)
		{
			long end = source.OffsetInBytes + srcLD * width;
			if (srcMP is not null && dstSP is not null)
			{
				for (; srcOff < end; srcOff += srcLD, dstOff += dstLD)
				{
					dstSP.NativeStream.Position = dstOff;
					dstSP.NativeStream.FromMemory(source.MoveBy(srcOff, height));
				}
			}
			else if (srcSP is not null && dstMP is not null)
			{
				for (; srcOff < end; srcOff += srcLD, dstOff += dstLD)
				{
					srcSP.NativeStream.Position = srcOff;
					srcSP.NativeStream.ToMemory(destination.MoveBy(dstOff, height));
				}
			}
			else if (srcSP is not null && dstSP is not null)
			{
				for (; srcOff < end; srcOff += srcLD, dstOff += dstLD)
				{
					srcSP.NativeStream.Position = srcOff;
					dstSP.NativeStream.Position = dstOff;
					srcSP.NativeStream.CopyTo(dstSP.NativeStream, height);
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static (long srcLen, long dstLen) StridedCopyCheck<T>(PointerSegment source, int incrementSource, PointerSegment destination, int incrementDestination) where T : unmanaged
		{
			long srcLen = source.LengthInBytes / Const<T>.SizeT, dstLen = destination.LengthInBytes / Const<T>.SizeT;
			if (incrementSource <= 0 || incrementSource >= srcLen)
				throw new ArgumentOutOfRangeException(nameof(incrementSource), incrementSource, Parameter.InvalidValue);
			if (incrementDestination <= 0 || incrementDestination >= dstLen)
				throw new ArgumentOutOfRangeException(nameof(incrementDestination), incrementDestination, Parameter.InvalidValue);
			return (srcLen, dstLen);
		}
		#endregion
	}
	#endregion
}