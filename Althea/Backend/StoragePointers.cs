using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;

using Althea.Linq;
using Althea.Storage;
using Althea.Resources;


namespace Althea.Backend.Storage
{
	/// <summary>
	/// An implementation of <see cref="IMemoryPointer"/>. This is implemented as a class to prevent boxing and unboxing.
	/// </summary>
	public class MemoryPointer : IMemoryPointer
	{
		private readonly IntPtr pointer;

		private readonly ulong length;

		IntPtr IMemoryPointer.Pointer => this.pointer;

		/// <summary>
		/// Get the original length of this pointer's underlying storage in bytes
		/// </summary>
		public ulong LengthInBytes => this.length;

		/// <summary>
		/// Create a new <see cref="MemoryPointer"/> with given allocated <paramref name="pointer"/> and corresponding <paramref name="length"/>
		/// </summary>
		/// <param name="pointer">The allocated pointer</param>
		/// <param name="length">The length in bytes</param>
		public MemoryPointer(IntPtr pointer, ulong length)
		{
			this.pointer = pointer; this.length = length;
		}
	}

	/// <summary>
	/// An implementation of <see cref="IStreamPointer"/> for files. This is implemented as a class to prevent boxing and unboxing.
	/// </summary>
	public class StreamPointer : IStreamPointer
	{
		/// <summary>
		/// The actual <see cref="Althea.Storage.Stream"/> field
		/// </summary>
		protected readonly Althea.Storage.Stream stream;

		/// <summary>
		/// Get the native stream this <see cref="StreamPointer"/> as a <see cref="Althea.Storage.Stream"/>.
		/// </summary>
		public Althea.Storage.Stream NativeStream => this.stream;

		/// <summary>
		/// Create this <see cref="StreamPointer"/> by given <see cref="Althea.Storage.Stream"/>
		/// </summary>
		/// <param name="stream">The given <see cref="Althea.Storage.Stream"/></param>
		public StreamPointer(Althea.Storage.Stream stream) => this.stream = stream;

		/// <summary>
		/// When implemented by a derived class, dispose unmanaged and managed resources held by this <see cref="StreamPointer"/>
		/// </summary>
		public virtual void Dispose()
		{
			this.stream.Dispose();
			GC.SuppressFinalize(this);
		}

		string IMainPropertyFormat.StringMain => this.stream.ToString();
	}


	/// <summary>
	/// An implementation of <see cref="Althea.Storage.Stream"/> for files.
	/// </summary>
	public sealed class FileStream : Althea.Storage.Stream
	{
		#region basic
		private readonly System.IO.FileStream stream;

		/// <summary>
		/// Get or set the position (offset) in bytes of this <see cref="FileStream"/>
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">If the value to be set is not less than <see cref="Althea.Storage.Stream.Length"/></exception>
		public override ulong Position {
			get => (ulong)this.stream.Position;
			set => this.stream.Seek((long)value, SeekOrigin.Begin);
		}

		/// <summary>
		/// Get a <see cref="bool"/> indicating whether this <see cref="Stream"/> can transfer data with managed C# memory directly or not, always return true.
		/// </summary>
		public override bool CanTransferWithManaged => true;

		private static readonly StorageLocation supportedLocation = new StorageLocation(LocationType.CpuRam, 0);

		/// <summary>
		/// <b>Statically</b> get the supported data transfer locations represented by <see cref="StorageLocation"/>s of this <see cref="Stream"/>
		/// </summary>
		public override IReadOnlyList<StorageLocation> SupportedTransfers { get; } = new[] { supportedLocation };

		/// <summary>
		/// <b>Statically</b> get a <see cref="bool"/> indicating whether data transfer with given <paramref name="location"/> is supported by this <see cref="Stream"/>. The default implementation utilizes the <see cref="SupportedTransfers"/>.
		/// </summary>
		/// <param name="location">The given <see cref="StorageLocation"/> to check transfer supporting</param>
		/// <returns>Whether data transfer with <paramref name="location"/> is supported or not</returns>
		public override bool IsSupported(StorageLocation location) => location == supportedLocation;

		/// <summary>
		/// Create a new <see cref="FileStream"/> with given <see cref="Uri"/> of file
		/// </summary>
		/// <param name="uri">The given <see cref="Uri"/> of file scheme</param>
		/// <param name="length">The initial length in bytes</param>
		/// <exception cref="NotSupportedException">If the scheme of <paramref name="uri"/> is not file or the stream cannot be created by given <paramref name="uri"/></exception>
		/// <exception cref="IOException">If other I/O error occurred</exception>
		/// <exception cref="UnauthorizedAccessException">If the give path in <paramref name="uri"/> cannot be created or overwritten</exception>
		public FileStream(Uri uri, ulong length) : base(length)
		{
			if (uri.GetScheme() != UriScheme.File)
				throw new NotSupportedException(Support.Location);
			// check
			string path = uri.LocalPath;
			if (File.Exists(path))
			{
				var flags = File.GetAttributes(path);
				if ((flags & (FileAttributes.ReadOnly | FileAttributes.System | FileAttributes.Directory)) != 0)
					throw new NotSupportedException(Support.Location);
			}
			else
				throw new NotSupportedException(Support.Location);
			string folder = Path.GetDirectoryName(path) ?? "";
			if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
			{
				Directory.CreateDirectory(folder);
			}
			// create
			this.stream = new System.IO.FileStream(path, FileMode.Create, FileAccess.ReadWrite);
			this.stream.SetLength((long)length);
			this.stream.Flush();
		}

		/// <summary>
		/// Actually release the unmanaged (and possibly managed) resources held by this class
		/// </summary>
		/// <param name="disposeManaged">Dispose managed resources or not</param>
		protected override void Dispose(bool disposeManaged) => this.stream.Dispose();

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
		public override void Flush()
		{
			if (this.Disposed)
				throw new ObjectDisposedException(nameof(FileStream));
			this.stream.Flush();
		}

		/// <summary>
		/// Read data from this <see cref="Stream"/> started from <see cref="Position"/> byte and write them to the given <see cref="PointerSegment"/> <paramref name="memory"/>.
		/// </summary>
		/// <param name="memory">The <see cref="PointerSegment"/> to write to</param>
		/// <remarks>When finished, the <see cref="Position"/> shall be advanced by the number of bytes read.</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="memory"/> is not valid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="memory"/>.<see cref="PointerSegment.LengthInBytes">Length</see> exceeds the boundary of this <see cref="Stream"/></exception>
		/// <exception cref="NotSupportedException">If <paramref name="memory"/>.<see cref="PointerSegment.Location">Location</see> is not supported</exception>
		/// <exception cref="IOException">If a general I/O error occurred</exception>
		/// <exception cref="ObjectDisposedException">If this is already disposed</exception>
		public override void ToMemory(PointerSegment memory)
		{
			if (!memory.IsValid())
				throw new ArgumentNullException(nameof(memory));
			if (this.IsSupported(memory.Location))
				throw new NotSupportedException(Support.Location);
			if (memory.Pointer is not IMemoryPointer mp)
				throw new NotSupportedException(Support.Location);
			// other checks in ToManged
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
			////if (!this.CanTransferWithManaged)
			////	throw new NotSupportedException(Support.Location);
			if (this.Disposed)
				throw new ObjectDisposedException(nameof(FileStream));
			if ((ulong)managed.Length * Storage<T>.SizeOfT + this.Position > this.Length)
				throw new ArgumentOutOfRangeException(nameof(managed));

			this.stream.Read(managed.UncheckAs<T, byte>());
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
		public override void FromMemory(PointerSegment memory)
		{
			if (!memory.IsValid())
				throw new ArgumentNullException(nameof(memory));
			if (this.IsSupported(memory.Location))
				throw new NotSupportedException(Support.Location);
			if (memory.Pointer is not IMemoryPointer mp)
				throw new NotSupportedException(Support.Location);
			// other checks in FromManged
			this.FromManged(mp.AsSpan<byte>(memory));
		}

		/// <summary>
		/// Read data from the given <paramref name="managed"/> memory as a<see cref="Span{T}"/> and write them to this <see cref="Stream"/> started from <see cref="Position"/>.
		/// </summary>
		/// <param name="managed">The managed memory as a <see cref="Span{T}"/> to read from</param>
		/// <remarks>When finished, the <see cref="Position"/> shall be advanced by the number of bytes written.</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="managed"/> is not valid (for example, has zero length)</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="managed"/>'s length exceeds the boundary</exception>
		/// <exception cref="NotSupportedException">If <see cref="CanTransferWithManaged"/> is false</exception>
		/// <exception cref="IOException">If a general I/O error occurred</exception>
		/// <exception cref="ObjectDisposedException">If this is already disposed</exception>
		public override void FromManged<T>(Span<T> managed)
		{
			////if (!this.CanTransferWithManaged)
			////	throw new NotSupportedException(Support.Location);
			if (this.Disposed)
				throw new ObjectDisposedException(nameof(FileStream));
			if ((ulong)managed.Length * Storage<T>.SizeOfT + this.Position > this.Length)
				throw new ArgumentOutOfRangeException(nameof(managed));

			this.stream.Write(managed.UncheckAs<T, byte>());
		}
		#endregion
	}

	/// <summary>
	/// An implementation of <see cref="Althea.Storage.Stream"/> for TCP links.
	/// </summary>
	public sealed class TcpStream : Althea.Storage.Stream
	{
		#region delegates
		/// <summary>
		/// The delegate used to generate information needed to initialize a remote file indicated by <paramref name="remotePath"/>.
		/// </summary>
		/// <param name="remotePath">The remote file path as a <see cref="string"/></param>
		/// <param name="length">The length in bytes to initialize</param>
		/// <returns>The information needed to initialize file indicated by <paramref name="remotePath"/> as a <see cref="byte"/> array.</returns>
		public delegate byte[] InitializationSend(string remotePath, ulong length);

		/// <summary>
		/// The delegate used to parse the received <paramref name="data"/> from the response of initialization procedure
		/// </summary>
		/// <param name="data">The response of initialization procedure as a <see cref="byte"/> array</param>
		/// <param name="errorMessage">The possible error message of the initialization procedure. May be null if succeeded.</param>
		/// <returns>Whether the initialization procedure succeeded or not</returns>
		public delegate bool InitializationAcknowledge(byte[] data, out string? errorMessage);

		/// <summary>
		/// The delegate used to generate information needed to send before writing to a remote file indicated by <paramref name="remotePath"/> (the actual writing is done by directly sending bytes needed to be written).
		/// </summary>
		/// <param name="remotePath">The remote file path as a <see cref="string"/></param>
		/// <param name="offset">The start position in bytes to begin writing</param>
		/// <param name="length">The length in bytes to write</param>
		/// <returns>The information needed to send before the real writing procedure as a <see cref="byte"/> array.</returns>
		public delegate byte[] BeforeWriteSend(string remotePath, ulong offset, ulong length);

		/// <summary>
		/// The delegate used to parse the received <paramref name="data"/> from the response of <see cref="BeforeWriteSend"/>
		/// </summary>
		/// <param name="data">The response of <see cref="BeforeWriteSend"/> as a <see cref="byte"/> array</param>
		/// <param name="errorMessage">The possible error message of the initialization procedure. May be null if succeeded.</param>
		/// <returns>Whether the <see cref="BeforeWriteSend"/> succeeded or not</returns>
		public delegate bool BeforeWriteAcknowledge(byte[] data, out string? errorMessage);

		/// <summary>
		/// The delegate used to parse the received <paramref name="data"/> from the response of the actual writing procedure
		/// </summary>
		/// <param name="data">The response of the actual writing procedure as a <see cref="byte"/> array</param>
		/// <param name="errorMessage">The possible error message of the initialization procedure. May be null if succeeded.</param>
		/// <returns>Whether the actual writing procedure succeeded or not</returns>
		public delegate bool AfterWriteAcknowledge(byte[] data, out string? errorMessage);

		/// <summary>
		/// The delegate used to generate information needed to send before reading from a remote file indicated by <paramref name="remotePath"/>.
		/// </summary>
		/// <param name="remotePath">The remote file path as a <see cref="string"/></param>
		/// <param name="offset">The start position in bytes to begin writing</param>
		/// <param name="length">The length in bytes to write</param>
		/// <returns>The information needed to send before the real writing procedure as a <see cref="byte"/> array.</returns>
		public delegate byte[] BeforeReadSend(string remotePath, ulong offset, ulong length);

		/// <summary>
		/// The delegate used to parse the received <paramref name="data"/> from the response of <see cref="BeforeReadSend"/>
		/// </summary>
		/// <param name="data">The response of <see cref="BeforeReadSend"/> as a <see cref="byte"/> array</param>
		/// <param name="errorMessage">The possible error message of the initialization procedure. May be null if succeeded.</param>
		/// <returns>Whether the <see cref="BeforeReadSend"/> succeeded or not</returns>
		/// <remarks>The actual data read is the response of resending <paramref name="data"/> to the remote server</remarks>
		public delegate bool BeforeReadAcknowledge(byte[] data, out string? errorMessage);
		#endregion

		#region basic
		private readonly NetworkStream stream;

		private readonly TcpClient client;

		private readonly Uri uri;


		/// <summary>
		/// Get or set the position (offset) in bytes of this <see cref="TcpStream"/>
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">If the value to be set is not less than <see cref="Althea.Storage.Stream.Length"/></exception>
		public override ulong Position { get; set; }

		/// <summary>
		/// Get a <see cref="bool"/> indicating whether this <see cref="Stream"/> can transfer data with managed C# memory directly or not, always return true.
		/// </summary>
		public override bool CanTransferWithManaged => true;

		private static readonly StorageLocation supportedLocation = new StorageLocation(LocationType.CpuRam, 0);

		/// <summary>
		/// <b>Statically</b> get the supported data transfer locations represented by <see cref="StorageLocation"/>s of this <see cref="Stream"/>
		/// </summary>
		public override IReadOnlyList<StorageLocation> SupportedTransfers { get; } = new[] { supportedLocation };

		/// <summary>
		/// <b>Statically</b> get a <see cref="bool"/> indicating whether data transfer with given <paramref name="location"/> is supported by this <see cref="Stream"/>. The default implementation utilizes the <see cref="SupportedTransfers"/>.
		/// </summary>
		/// <param name="location">The given <see cref="StorageLocation"/> to check transfer supporting</param>
		/// <returns>Whether data transfer with <paramref name="location"/> is supported or not</returns>
		public override bool IsSupported(StorageLocation location) => location == supportedLocation;

		/// <summary>
		/// Create a new <see cref="TcpStream"/> with given <see cref="Uri"/> of TCP
		/// </summary>
		/// <param name="uri">The given <see cref="Uri"/> of file scheme</param>
		/// <param name="length">The initial length in bytes</param>
		/// <exception cref="NotSupportedException">If the scheme of <paramref name="uri"/> is not file or the stream cannot be created by given <paramref name="uri"/></exception>
		/// <exception cref="SocketException">If other socket error occurred</exception>
		/// <exception cref="UnauthorizedAccessException">If the give path in <paramref name="uri"/> cannot be created or overwritten</exception>
		public TcpStream(Uri uri, ulong length, ) : base(length)
		{
			if (uri.GetScheme() != UriScheme.TCP)
				throw new NotSupportedException(Support.Location);

			this.uri = uri;
			this.client = new TcpClient(uri.Host, uri.Port);
			this.stream = this.client.GetStream();
			// check

			// create

		}

		/// <summary>
		/// Actually release the unmanaged (and possibly managed) resources held by this class
		/// </summary>
		/// <param name="disposeManaged">Dispose managed resources or not</param>
		protected override void Dispose(bool disposeManaged)
		{
			this.stream.Dispose();
			this.client.Dispose();
		}

		/// <summary>
		/// Get the string representation of this <see cref="Stream"/>.
		/// </summary>
		/// <returns>The string representation of this <see cref="Stream"/></returns>
		public override string ToString() => this.uri.AbsoluteUri;
		#endregion

		#region implementations
		/// <summary>
		/// Clears all buffers for this stream and causes any buffered data to be written to the underlying device. Currently does nothing.
		/// </summary>
		public override void Flush() { }

		/// <summary>
		/// Read data from this <see cref="Stream"/> started from <see cref="Position"/> byte and write them to the given <see cref="PointerSegment"/> <paramref name="memory"/>.
		/// </summary>
		/// <param name="memory">The <see cref="PointerSegment"/> to write to</param>
		/// <remarks>When finished, the <see cref="Position"/> shall be advanced by the number of bytes read.</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="memory"/> is not valid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="memory"/>.<see cref="PointerSegment.LengthInBytes">Length</see> exceeds the boundary of this <see cref="Stream"/></exception>
		/// <exception cref="NotSupportedException">If <paramref name="memory"/>.<see cref="PointerSegment.Location">Location</see> is not supported</exception>
		/// <exception cref="SocketException">If other socket error occurred</exception>
		/// <exception cref="ObjectDisposedException">If this is already disposed</exception>
		public override void ToMemory(PointerSegment memory)
		{
			if (!memory.IsValid())
				throw new ArgumentNullException(nameof(memory));
			if (this.IsSupported(memory.Location))
				throw new NotSupportedException(Support.Location);
			if (memory.Pointer is not IMemoryPointer mp)
				throw new NotSupportedException(Support.Location);
			// other checks in ToManged
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
		/// <exception cref="SocketException">If other socket error occurred</exception>
		/// <exception cref="ObjectDisposedException">If this is already disposed</exception>
		public override void ToManged<T>(Span<T> managed)
		{
			////if (!this.CanTransferWithManaged)
			////	throw new NotSupportedException(Support.Location);
			if (this.Disposed)
				throw new ObjectDisposedException(nameof(FileStream));
			if ((ulong)managed.Length * Storage<T>.SizeOfT + this.Position > this.Length)
				throw new ArgumentOutOfRangeException(nameof(managed));

			this.stream.Read(managed.UncheckAs<T, byte>());
		}

		/// <summary>
		/// Read data from the given <see cref="PointerSegment"/> <paramref name="memory"/> and write them to this <see cref="Stream"/> started from <see cref="Position"/> byte.
		/// </summary>
		/// <param name="memory">The <see cref="PointerSegment"/> to read from</param>
		/// <remarks>When finished, the <see cref="Position"/> shall be advanced by the number of bytes written.</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="memory"/> is not valid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="memory"/>.<see cref="PointerSegment.LengthInBytes">Length</see> exceeds the boundary of this <see cref="Stream"/></exception>
		/// <exception cref="NotSupportedException">If <paramref name="memory"/>.<see cref="PointerSegment.Location">Location</see> is not supported</exception>
		/// <exception cref="SocketException">If other socket error occurred</exception>
		/// <exception cref="ObjectDisposedException">If this is already disposed</exception>
		public override void FromMemory(PointerSegment memory)
		{
			if (!memory.IsValid())
				throw new ArgumentNullException(nameof(memory));
			if (this.IsSupported(memory.Location))
				throw new NotSupportedException(Support.Location);
			if (memory.Pointer is not IMemoryPointer mp)
				throw new NotSupportedException(Support.Location);
			// other checks in FromManged
			this.FromManged(mp.AsSpan<byte>(memory));
		}

		/// <summary>
		/// Read data from the given <paramref name="managed"/> memory as a<see cref="Span{T}"/> and write them to this <see cref="Stream"/> started from <see cref="Position"/>.
		/// </summary>
		/// <param name="managed">The managed memory as a <see cref="Span{T}"/> to read from</param>
		/// <remarks>When finished, the <see cref="Position"/> shall be advanced by the number of bytes written.</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="managed"/> is not valid (for example, has zero length)</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="managed"/>'s length exceeds the boundary</exception>
		/// <exception cref="NotSupportedException">If <see cref="CanTransferWithManaged"/> is false</exception>
		/// <exception cref="SocketException">If other socket error occurred</exception>
		/// <exception cref="ObjectDisposedException">If this is already disposed</exception>
		public override void FromManged<T>(Span<T> managed)
		{
			////if (!this.CanTransferWithManaged)
			////	throw new NotSupportedException(Support.Location);
			if (this.Disposed)
				throw new ObjectDisposedException(nameof(FileStream));
			if ((ulong)managed.Length * Storage<T>.SizeOfT + this.Position > this.Length)
				throw new ArgumentOutOfRangeException(nameof(managed));

			this.stream.Write(managed.UncheckAs<T, byte>());
		}
		#endregion
	}
}