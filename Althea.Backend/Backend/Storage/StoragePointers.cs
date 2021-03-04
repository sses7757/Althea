using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Althea.Linq;
using Althea.Storage;
using Althea.Resources;


[assembly: CLSCompliant(true)]


namespace Althea.Backend.Storage
{
	/// <summary>
	/// An implementation of <see cref="IMemoryPointer"/>. This is implemented as a class to prevent boxing and unboxing.
	/// </summary>
	public class MemoryPointer : IMemoryPointer
	{
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
		public MemoryPointer(IntPtr pointer, long length, StorageLocation location)
		{
			if (location.Type.GetClassification() != LocationTypeExtension.ClassMemory)
				throw new ArgumentOutOfRangeException(nameof(location), location, Parameter.UnexpectedValue);
			this.Pointer = pointer; this.LengthInBytes = length; this.Location = location;
		}

		/// <summary>
		/// Indicates whether the current object is equal to another object of the same type.
		/// </summary>
		/// <param name="other"> An object to compare with this object.</param>
		/// <returns>True if the current object is equal to the other parameter; otherwise, false.</returns>
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
		public override int GetHashCode() => HashCode.Combine(this.Pointer, this.Location);
	}

	/// <summary>
	/// An implementation of <see cref="IStreamPointer"/> for files. This is implemented as a class to prevent boxing and unboxing.
	/// </summary>
	public class StreamPointer : IStreamPointer
	{
		/// <summary>
		/// Get the native stream this <see cref="StreamPointer"/> as a <see cref="Althea.Storage.Stream"/>.
		/// </summary>
		public Althea.Storage.Stream NativeStream { get; }

		/// <summary>
		/// The storage location of this <see cref="MemoryPointer"/> as a <see cref="StorageLocation"/>
		/// </summary>
		public StorageLocation Location { get; }

		/// <summary>
		/// Create this <see cref="StreamPointer"/> by given <see cref="Althea.Storage.Stream"/>
		/// </summary>
		/// <param name="stream">The given <see cref="Althea.Storage.Stream"/></param>
		/// <param name="location">The <see cref="StorageLocation"/> of this <see cref="StreamPointer"/></param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="location"/>'s <see cref="LocationType"/> is not a stream type</exception>
		public StreamPointer(Althea.Storage.Stream stream, StorageLocation location)
		{
			if (location.Type.GetClassification() != LocationTypeExtension.ClassStream)
				throw new ArgumentOutOfRangeException(nameof(location), location, Parameter.UnexpectedValue);
			this.NativeStream = stream; this.Location = location;
		}

		/// <summary>
		/// When implemented by a derived class, dispose unmanaged and managed resources held by this <see cref="StreamPointer"/>
		/// </summary>
		public virtual void Dispose()
		{
			this.NativeStream.Dispose();
			GC.SuppressFinalize(this);
		}

		string IMainPropertyFormat.StringMain => this.NativeStream.ToString();

		/// <summary>
		/// Indicates whether the current object is equal to another object of the same type.
		/// </summary>
		/// <param name="other"> An object to compare with this object.</param>
		/// <returns>True if the current object is equal to the other parameter; otherwise, false.</returns>
		public bool Equals(IPointer? other)
		{
			if (other is StreamPointer mp)
				return this.NativeStream == mp.NativeStream && this.Location == mp.Location;
			else
				return false;
		}

		/// <summary>
		/// Get the hash code of this <see cref="MemoryPointer"/>
		/// </summary>
		/// <returns>The hash code of this <see cref="MemoryPointer"/></returns>
		public override int GetHashCode() => HashCode.Combine(this.NativeStream, this.Location);
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
		public override long Position {
			get => this.stream.Position;
			set => this.stream.Seek(value, SeekOrigin.Begin);
		}

		/// <summary>
		/// Get a <see cref="bool"/> indicating whether this <see cref="Stream"/> can transfer data with managed C# memory directly or not, always return true.
		/// </summary>
		public override bool CanTransferWithManaged => true;

		private static readonly StorageLocation supportedLocation = new(LocationType.CpuRam, 0);

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
		public FileStream(Uri uri, long length) : base(length)
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
			this.stream.SetLength(length);
			this.stream.Flush();
		}

		/// <summary>
		/// Actually release the unmanaged (and possibly managed) resources held by this class
		/// </summary>
		/// <param name="disposeManaged">Dispose managed resources or not</param>
		protected override void Dispose(bool disposeManaged)
		{
			this.stream.Dispose();
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
			if ((long)managed.Length * Storage<T>.SizeOfT + this.Position > this.Length)
				throw new ArgumentException(Parameter.WrongSize, nameof(managed));

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
		public override void FromManged<T>(ReadOnlySpan<T> managed)
		{
			////if (!this.CanTransferWithManaged)
			////	throw new NotSupportedException(Support.Location);
			if (this.Disposed)
				throw new ObjectDisposedException(nameof(FileStream));
			if ((long)managed.Length * Storage<T>.SizeOfT + this.Position > this.Length)
				throw new ArgumentException(Parameter.WrongSize, nameof(managed));

			this.stream.Write(managed.UncheckAs<T, byte>());
		}
		#endregion
	}

	/// <summary>
	/// The struct for a TCP protocol used by <see cref="TcpStream"/>
	/// </summary>
	public readonly struct TcpProtocol : ICheckValid
	{
		#region delegates
		/// <summary>
		/// The delegate used to generate information needed to initialize a remote file indicated by <paramref name="remotePath"/>.
		/// </summary>
		/// <param name="remotePath">The remote file path as a <see cref="string"/></param>
		/// <param name="length">The length in bytes to initialize</param>
		/// <returns>The information needed to initialize file indicated by <paramref name="remotePath"/> as a <see cref="byte"/> <see cref="Span{T}"/>.</returns>
		/// <remarks>It is recommended to buffer the return byte array and only return part of it as a <see cref="Span{T}"/> to prevent frequent array allocation and destroy.</remarks>
		public delegate Span<byte> DelegateInitializationSend(string remotePath, long length);

		/// <summary>
		/// The delegate used to parse the received <paramref name="data"/> from the response of initialization procedure
		/// </summary>
		/// <param name="data">The response of initialization procedure as a <see cref="byte"/> array</param>
		/// <param name="errorMessage">The possible error message of the initialization procedure. May be null if succeeded.</param>
		/// <returns>Whether the initialization procedure succeeded or not</returns>
		public delegate bool DelegateInitializationAcknowledge(byte[] data, out string? errorMessage);

		/// <summary>
		/// The delegate used to generate information needed to destroy a remote file indicated by <paramref name="remotePath"/>.
		/// </summary>
		/// <param name="remotePath">The remote file path as a <see cref="string"/></param>
		/// <returns>The information needed to destroy file indicated by <paramref name="remotePath"/> as a <see cref="byte"/> <see cref="Span{T}"/>.</returns>
		/// <remarks>It is recommended to buffer the return byte array and only return part of it as a <see cref="Span{T}"/> to prevent frequent array allocation and destroy.</remarks>
		public delegate Span<byte> DelegateDestroySend(string remotePath);

		/// <summary>
		/// The delegate used to generate information needed to send before writing to a remote file indicated by <paramref name="remotePath"/> (the actual writing is done by directly sending bytes needed to be written).
		/// </summary>
		/// <param name="remotePath">The remote file path as a <see cref="string"/></param>
		/// <param name="offset">The start position in bytes to begin writing</param>
		/// <param name="length">The length in bytes to write</param>
		/// <returns>The information needed to send before the real writing procedure as a <see cref="byte"/> <see cref="Span{T}"/>.</returns>
		/// <remarks>It is recommended to buffer the return byte array and only return part of it as a <see cref="Span{T}"/> to prevent frequent array allocation and destroy.</remarks>
		public delegate Span<byte> DelegateBeforeWriteSend(string remotePath, long offset, long length);

		/// <summary>
		/// The delegate used to parse the received <paramref name="data"/> from the response of <see cref="DelegateBeforeWriteSend"/>
		/// </summary>
		/// <param name="data">The response of <see cref="DelegateBeforeWriteSend"/> as a <see cref="byte"/> array</param>
		/// <param name="errorMessage">The possible error message of the initialization procedure. May be null if succeeded.</param>
		/// <returns>Whether the <see cref="DelegateBeforeWriteSend"/> succeeded or not</returns>
		public delegate bool DelegateBeforeWriteAcknowledge(byte[] data, out string? errorMessage);

		/// <summary>
		/// The delegate used to parse the received <paramref name="data"/> from the response of the actual writing procedure
		/// </summary>
		/// <param name="data">The response of the actual writing procedure as a <see cref="byte"/> array</param>
		/// <param name="errorMessage">The possible error message of the initialization procedure. May be null if succeeded.</param>
		/// <returns>Whether the actual writing procedure succeeded or not</returns>
		public delegate bool DelegateAfterWriteAcknowledge(byte[] data, out string? errorMessage);

		/// <summary>
		/// The delegate used to generate information needed to send before reading from a remote file indicated by <paramref name="remotePath"/>.
		/// </summary>
		/// <param name="remotePath">The remote file path as a <see cref="string"/></param>
		/// <param name="offset">The start position in bytes to begin writing</param>
		/// <param name="length">The length in bytes to write</param>
		/// <returns>The information needed to send before the real writing procedure as a <see cref="byte"/> <see cref="Span{T}"/>.</returns>
		/// <remarks>It is recommended to buffer the return byte array and only return part of it as a <see cref="Span{T}"/> to prevent frequent array allocation and destroy.</remarks>
		public delegate Span<byte> DelegateBeforeReadSend(string remotePath, long offset, long length);

		/// <summary>
		/// The delegate used to parse the received <paramref name="data"/> from the response of <see cref="DelegateBeforeReadSend"/>
		/// </summary>
		/// <param name="data">The response of <see cref="DelegateBeforeReadSend"/> as a <see cref="byte"/> array</param>
		/// <param name="errorMessage">The possible error message of the initialization procedure. May be null if succeeded.</param>
		/// <returns>Whether the <see cref="DelegateBeforeReadSend"/> succeeded or not</returns>
		/// <remarks>The actual data read is the response of resending <paramref name="data"/> to the remote server</remarks>
		public delegate bool DelegateBeforeReadAcknowledge(byte[] data, out string? errorMessage);
		#endregion

		#region basic
		/// <summary>
		/// The instance of <see cref="DelegateInitializationSend"/>
		/// </summary>
		public readonly DelegateInitializationSend initializationSend;
		/// <summary>
		/// The instance of <see cref="DelegateInitializationAcknowledge"/>
		/// </summary>
		public readonly DelegateInitializationAcknowledge initializationAcknowledge;
		/// <summary>
		/// The instance of <see cref="DelegateDestroySend"/>
		/// </summary>
		public readonly DelegateDestroySend destroySend;
		/// <summary>
		/// The instance of <see cref="DelegateBeforeWriteSend"/>
		/// </summary>
		public readonly DelegateBeforeWriteSend beforeWriteSend;
		/// <summary>
		/// The instance of <see cref="DelegateBeforeWriteAcknowledge"/>
		/// </summary>
		public readonly DelegateBeforeWriteAcknowledge beforeWriteAcknowledge;
		/// <summary>
		/// The instance of <see cref="DelegateAfterWriteAcknowledge"/>
		/// </summary>
		public readonly DelegateAfterWriteAcknowledge afterWriteAcknowledge;
		/// <summary>
		/// The instance of <see cref="DelegateBeforeReadSend"/>
		/// </summary>
		public readonly DelegateBeforeReadSend beforeReadSend;
		/// <summary>
		/// The instance of <see cref="DelegateBeforeReadAcknowledge"/>
		/// </summary>
		public readonly DelegateBeforeReadAcknowledge beforeReadAcknowledge;
		/// <summary>
		/// The maximum length in bytes of any receive message
		/// </summary>
		public readonly int maxReturnSize;
		/// <summary>
		/// The remote server's port number
		/// </summary>
		public readonly int remotePort;

		/// <summary>
		/// Create a <see cref="TcpProtocol"/> with given initialized delegates
		/// </summary>
		public TcpProtocol(	DelegateInitializationSend initializationSend, DelegateInitializationAcknowledge initializationAcknowledge, DelegateDestroySend destroySend,
							DelegateBeforeWriteSend beforeWriteSend, DelegateBeforeWriteAcknowledge beforeWriteAcknowledge, DelegateAfterWriteAcknowledge afterWriteAcknowledge,
							DelegateBeforeReadSend beforeReadSend, DelegateBeforeReadAcknowledge beforeReadAcknowledge,
							int maxReturnSize, int port)
		{
			this.initializationSend = initializationSend; this.initializationAcknowledge = initializationAcknowledge; this.destroySend = destroySend;
			this.beforeWriteSend = beforeWriteSend; this.beforeWriteAcknowledge = beforeWriteAcknowledge; this.afterWriteAcknowledge = afterWriteAcknowledge;
			this.beforeReadSend = beforeReadSend; this.beforeReadAcknowledge = beforeReadAcknowledge;
			this.maxReturnSize = maxReturnSize; this.remotePort = port;
		}

		/// <summary>
		/// Check whether this struct contains valid protocol
		/// </summary>
		/// <returns>Whether this struct contains valid protocol or not</returns>
		public bool IsValid() => this.maxReturnSize > 0 && this.remotePort > 0 &&
			this.initializationSend is not null && this.initializationAcknowledge is not null && this.destroySend is not null &&
			this.beforeWriteSend is not null && this.beforeWriteAcknowledge is not null && this.afterWriteAcknowledge is not null &&
			this.beforeReadSend is not null && this.beforeReadAcknowledge is not null;
		#endregion
	}

	/// <summary>
	/// An implementation of <see cref="Althea.Storage.Stream"/> for TCP links.
	/// </summary>
	public sealed class TcpStream : Althea.Storage.Stream
	{
		#region basic
		private readonly NetworkStream stream;

		private readonly TcpClient client;

		private readonly Uri uri;

		private readonly TcpProtocol protocol;

		private string RemotePath => this.uri.AbsolutePath;

		/// <summary>
		/// Get or set the position (offset) in bytes of this <see cref="TcpStream"/>
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">If the value to be set is not less than <see cref="Althea.Storage.Stream.Length"/></exception>
		public override long Position { get; set; } = 0;

		/// <summary>
		/// Get a <see cref="bool"/> indicating whether this <see cref="Stream"/> can transfer data with managed C# memory directly or not, always return true.
		/// </summary>
		public override bool CanTransferWithManaged => true;

		private static readonly StorageLocation supportedLocation = new(LocationType.CpuRam, 0);

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

		private readonly byte[] returnCache;

		/// <summary>
		/// Create a new <see cref="TcpStream"/> with given <see cref="Uri"/> of TCP scheme under given <see cref="TcpProtocol"/>
		/// </summary>
		/// <param name="uri">The given <see cref="Uri"/> of file scheme</param>
		/// <param name="length">The initial length in bytes</param>
		/// <param name="protocol">The <see cref="TcpProtocol"/> to use</param>
		/// <exception cref="ArgumentNullException">If <paramref name="protocol"/> is not a valid one</exception>
		/// <exception cref="ArgumentOutOfRangeException">If the <see cref="TcpProtocol.remotePort"/> of <paramref name="protocol"/> is not <see cref="Uri.Port"/> of <paramref name="uri"/> while its <see cref="Uri.IsDefaultPort"/> is false</exception>
		/// <exception cref="NotSupportedException">If the scheme of <paramref name="uri"/> is not file or the stream cannot be created by given <paramref name="uri"/></exception>
		/// <exception cref="SocketException">If other socket error occurred</exception>
		/// <exception cref="IOException">If other I/O error occurred</exception>
		/// <exception cref="UnauthorizedAccessException">If the give path in <paramref name="uri"/> cannot be created or overwritten</exception>
		public TcpStream(Uri uri, long length, TcpProtocol protocol) : base(length)
		{
			if (uri.GetScheme() != UriScheme.TCP)
				throw new NotSupportedException(Support.Location);
			if (!protocol.IsValid())
				throw new ArgumentNullException(nameof(protocol));
			if (!uri.IsDefaultPort && uri.Port != protocol.remotePort)
				throw new ArgumentOutOfRangeException(nameof(uri), uri, Parameter.InvalidValue);

			// set fields
			this.uri = uri;
			this.protocol = protocol;
			this.client = new TcpClient(uri.Host, protocol.remotePort);
			this.stream = this.client.GetStream();
			this.returnCache = new byte[protocol.maxReturnSize];
			// create file
			var init = protocol.initializationSend(this.RemotePath, length);
			this.stream.Write(init);
			this.stream.Read(this.returnCache, 0, this.returnCache.Length);
			bool success = protocol.initializationAcknowledge.Invoke(this.returnCache, out string? errorMessage);
			if (!success)
			{
				this.Dispose(true);
				throw new IOException(errorMessage);
			}
		}

		/// <summary>
		/// Actually release the unmanaged (and possibly managed) resources held by this class
		/// </summary>
		/// <param name="disposeManaged">Dispose managed resources or not</param>
		protected override void Dispose(bool disposeManaged)
		{
			var destroy = this.protocol.destroySend.Invoke(this.RemotePath);
			try
			{
				this.stream.Write(destroy);
			}
			finally
			{
				this.stream.Dispose();
				this.client.Dispose();
			}
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
		/// <exception cref="IOException">If other I/O error occurred</exception>
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
		/// <exception cref="IOException">If other I/O error occurred</exception>
		/// <exception cref="ObjectDisposedException">If this is already disposed</exception>
		public override void ToManged<T>(Span<T> managed)
		{
			////if (!this.CanTransferWithManaged)
			////	throw new NotSupportedException(Support.Location);
			if (this.Disposed)
				throw new ObjectDisposedException(nameof(FileStream));
			long size = (long)managed.Length * Storage<T>.SizeOfT;
			if (size + this.Position > this.Length)
				throw new ArgumentException(Parameter.WrongSize, nameof(managed));

			// before read
			var sendData = this.protocol.beforeReadSend.Invoke(this.RemotePath, this.Position, size);
			this.stream.Write(sendData);
			this.stream.Read(this.returnCache, 0, this.returnCache.Length);
			bool success = this.protocol.beforeReadAcknowledge.Invoke(this.returnCache, out string? errorMessage);
			if (!success)
				throw new IOException(errorMessage);
			// read
			Array.Fill<byte>(this.returnCache, 0, 0, Math.Min(this.returnCache.Length, 1024));
			this.stream.Write(this.returnCache, 0, Math.Min(this.returnCache.Length, 1024));
			var read = this.stream.Read(managed.UncheckAs<T, byte>());
			if (read != size)
				throw new IOException(Parameter.NotSameSize);
			// advance position
			this.Position += size;
		}

		/// <summary>
		/// Read data from the given <see cref="PointerSegment"/> <paramref name="memory"/> and write them to this <see cref="Stream"/> started from <see cref="Position"/> byte.
		/// </summary>
		/// <param name="memory">The <see cref="PointerSegment"/> to read from</param>
		/// <remarks>When finished, the <see cref="Position"/> shall be advanced by the number of bytes written.</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="memory"/> is not valid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="memory"/>.<see cref="PointerSegment.LengthInBytes">Length</see> exceeds the boundary of this <see cref="Stream"/></exception>
		/// <exception cref="NotSupportedException">If <paramref name="memory"/>.<see cref="PointerSegment.Location">Location</see> is not supported</exception>
		/// <exception cref="IOException">If other I/O error occurred</exception>
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
			this.FromManged<byte>(mp.AsSpan<byte>(memory));
		}

		/// <summary>
		/// Read data from the given <paramref name="managed"/> memory as a<see cref="ReadOnlySpan{T}"/> and write them to this <see cref="Stream"/> started from <see cref="Position"/>.
		/// </summary>
		/// <param name="managed">The managed memory as a <see cref="ReadOnlySpan{T}"/> to read from</param>
		/// <remarks>When finished, the <see cref="Position"/> shall be advanced by the number of bytes written.</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="managed"/> is not valid (for example, has zero length)</exception>
		/// <exception cref="ArgumentException">If <paramref name="managed"/>'s length exceeds the boundary</exception>
		/// <exception cref="NotSupportedException">If <see cref="CanTransferWithManaged"/> is false</exception>
		/// <exception cref="IOException">If other I/O error occurred</exception>
		/// <exception cref="ObjectDisposedException">If this is already disposed</exception>
		public override void FromManged<T>(ReadOnlySpan<T> managed)
		{
			////if (!this.CanTransferWithManaged)
			////	throw new NotSupportedException(Support.Location);
			if (this.Disposed)
				throw new ObjectDisposedException(nameof(FileStream));
			long size = (long)managed.Length * Storage<T>.SizeOfT;
			if (size + this.Position > this.Length)
				throw new ArgumentException(Parameter.WrongSize, nameof(managed));

			// before write
			var sendData = this.protocol.beforeWriteSend.Invoke(this.RemotePath, this.Position, size);
			this.stream.Write(sendData);
			this.stream.Read(this.returnCache, 0, this.returnCache.Length);
			bool success = this.protocol.beforeWriteAcknowledge.Invoke(this.returnCache, out string? errorMessage);
			if (!success)
				throw new IOException(errorMessage);
			// write
			this.stream.Write(managed.UncheckAs<T, byte>());
			this.stream.Read(this.returnCache, 0, this.returnCache.Length);
			success = this.protocol.afterWriteAcknowledge.Invoke(this.returnCache, out errorMessage);
			if (!success)
				throw new IOException(errorMessage);
			// advance position
			this.Position += size;
		}
		#endregion
	}


	#region extension methods
	internal static class ConcretePointersExtension
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static unsafe T* UnmangedPointer<T>(this IMemoryPointer p, long offset = 0) where T : unmanaged => (T*)p.Pointer.ToPointer() + offset;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static unsafe void* NativePointer(this IMemoryPointer p, long offset = 0) => (byte*)p.Pointer.ToPointer() + offset;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static unsafe Span<T> AsSpan<T>(this IMemoryPointer p, long offset = 0, int length = 0) where T : unmanaged => new(p.UnmangedPointer<T>(offset), length <= 0 ? checked((int)(p.LengthInBytes / sizeof(T))) : length);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static Span<T> AsSpan<T>(this IMemoryPointer p, PointerSegment pointerSegment) where T : unmanaged => p.AsSpan<T>(checked(pointerSegment.OffsetInBytes / Storage<T>.SizeOfT), checked((int)(pointerSegment.LengthInBytes / Storage<T>.SizeOfT)));


		public static readonly StorageLocation CpuAlone = new(LocationType.CpuRam, 0);
		public static readonly StorageLocation FileAlone = new(LocationType.Uri, (int)UriScheme.File);
		public static readonly StorageLocation TcpAlone = new(LocationType.Uri, (int)UriScheme.TCP);

		public const long INVALID = -1, NOT_SUPPORT = -2;

		public static long GetPointerOffset<T>(this PointerSegment pointer, out IMemoryPointer? memoryPointer, out IStreamPointer? streamPointer, bool @throw = true) where
			T : unmanaged
		{
			memoryPointer = null; streamPointer = null;
			// check first
			if (!pointer.IsValid() || pointer.OffsetInBytes % Storage<T>.SizeOfT != 0 || pointer.LengthInBytes % Storage<T>.SizeOfT != 0)
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
			else if (pointer.Location == FileAlone && pointer.Pointer is IStreamPointer { NativeStream: FileStream } sp1)
			{
				streamPointer = sp1;
			}
			else if (pointer.Location == TcpAlone && pointer.Pointer is IStreamPointer { NativeStream: TcpStream } sp2)
			{
				streamPointer = sp2;
			}
			else
			{
				return NOT_SUPPORT;
			}
			return pointer.OffsetInBytes / Storage<T>.SizeOfT;
		}

		public static long GetPointerOffset(this PointerSegment pointer, out IMemoryPointer? memoryPointer, out IStreamPointer? streamPointer, bool @throw = true) => GetPointerOffset<byte>(pointer, out memoryPointer, out streamPointer, @throw);
	}
	#endregion
}


namespace Althea.Backend.Storage.Tcp
{
	#region records
	/// <summary>
	/// The default send message data
	/// </summary>
	public record DefaultSendData
	{
		/// <summary>
		/// The remote file path
		/// </summary>
		public string RemotePath { get; init; } = string.Empty;

		/// <summary>
		/// Initialize or destroy. True for initialize, false for destroy, null for read or write
		/// </summary>
		public bool? InitOrDestroy { get; init; } = null;

		/// <summary>
		/// Read or write. True for read, false for write, null for initialize or destroy
		/// </summary>
		public bool? ReadOrWrite { get; init; } = null;

		/// <summary>
		/// The offset in bytes
		/// </summary>
		public long Offset { get; init; }

		/// <summary>
		/// The length in bytes
		/// </summary>
		public long Length { get; init; }
	}

	/// <summary>
	/// The default receive message data
	/// </summary>
	public record DefaultReceiveData
	{
		/// <summary>
		/// Operation success or not
		/// </summary>
		public bool Success { get; init; }

		/// <summary>
		/// The returned error message with no longer than 65000 bytes
		/// </summary>
		public string? ErrorMessage { get; init; }
	}
	#endregion

	#region default methods
	/// <summary>
	/// The class containing default TCP stream methods
	/// </summary>
	public static class DefaultTcpMethods
	{
		/// <summary>
		/// The remote server port intended to used
		/// </summary>
		public const int PORT = 6439;//// new Random().Next(1, ushort.MaxValue);

		/// <summary>
		/// The default <see cref="TcpProtocol"/> which utilize the protocol defined by <see cref="DefaultSendData"/> and <see cref="DefaultReceiveData"/>
		/// </summary>
		public static readonly TcpProtocol DefaultTcpProtocol = new(DefaultInitializationSend, DefaultAcknowledge, DefaultDestroySend,
																	DefaultBeforeWriteSend, DefaultAcknowledge, DefaultAcknowledge,
																	DefaultBeforeReadSend, DefaultAcknowledge,
																	1 << 16, PORT);

		private readonly static byte[] static_buffer = new byte[1 << 20];

		private static Span<byte> DefaultSend(DefaultSendData data)
		{
			var json = JsonSerializer.Serialize(data);
			Span<byte> span = static_buffer;
			int n = Encoding.UTF8.GetBytes(json, span);
			return span[..n];
		}

		/// <summary>
		/// The default implementation of <see cref="TcpProtocol.DelegateInitializationSend"/>
		/// </summary>
		public static Span<byte> DefaultInitializationSend(string remotePath, long length)
		{
			var data = new DefaultSendData { InitOrDestroy = true, RemotePath = remotePath, Length = length };
			return DefaultSend(data);
		}

		/// <summary>
		/// The default implementation of <see cref="TcpProtocol.DelegateInitializationAcknowledge"/>, <see cref="TcpProtocol.DelegateBeforeWriteAcknowledge"/> and <see cref="TcpProtocol.DelegateBeforeReadAcknowledge"/>
		/// </summary>
		public static bool DefaultAcknowledge(byte[] data, out string? errorMessage)
		{
			var json = Encoding.UTF8.GetString(data);
			var receive = JsonSerializer.Deserialize<DefaultReceiveData>(json);
			if (receive is null)
			{
				errorMessage = Other.CannotDeserialize;
				return false;
			}
			errorMessage = receive.ErrorMessage;
			return receive.Success;
		}

		/// <summary>
		/// The default implementation of <see cref="TcpProtocol.DelegateDestroySend"/>
		/// </summary>
		public static Span<byte> DefaultDestroySend(string remotePath)
		{
			var data = new DefaultSendData { InitOrDestroy = false, RemotePath = remotePath };
			return DefaultSend(data);
		}

		/// <summary>
		/// The default implementation of <see cref="TcpProtocol.DelegateBeforeWriteSend"/>
		/// </summary>
		public static Span<byte> DefaultBeforeWriteSend(string remotePath, long offset, long length)
		{
			var data = new DefaultSendData { ReadOrWrite = false, RemotePath = remotePath, Offset = offset, Length = length };
			return DefaultSend(data);
		}

		/// <summary>
		/// The default implementation of <see cref="TcpProtocol.DelegateBeforeReadSend"/>
		/// </summary>
		public static Span<byte> DefaultBeforeReadSend(string remotePath, long offset, long length)
		{
			var data = new DefaultSendData { ReadOrWrite = true, RemotePath = remotePath, Offset = offset, Length = length };
			return DefaultSend(data);
		}
	}
	#endregion
}