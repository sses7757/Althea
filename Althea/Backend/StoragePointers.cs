using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

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
	public class FileStreamPointer : IStreamPointer
	{
		private readonly FileStream stream;

		private readonly Uri uri;

		/// <summary>
		/// Get the raw stream of this <see cref="FileStreamPointer"/> used for reading as a <see cref="Stream"/>
		/// </summary>
		protected override Stream ReadStream => this.stream;

		/// <summary>
		/// Get the raw stream of this <see cref="FileStreamPointer"/> used for writing as a <see cref="Stream"/>
		/// </summary>
		protected override Stream WriteStream => this.stream;

		/// <summary>
		/// Get the raw URI of this <see cref="FileStreamPointer"/> as a <see cref="Uri"/>
		/// </summary>
		public Uri OriginalUri => this.uri;

		/// <summary>
		/// The basic description of this <see cref="IStreamPointer"/> as a <see cref="string"/>, such as <see cref="Uri.ToString"/>
		/// </summary>
		protected override string Description => this.OriginalUri.ToString();

		/// <summary>
		/// Create a new <see cref="FileStreamPointer"/> with given <see cref="Uri"/> of file
		/// </summary>
		/// <param name="uri">The given <see cref="Uri"/> of file scheme</param>
		/// <param name="length">The initial length in bytes</param>
		/// <exception cref="NotSupportedException">If the scheme of <paramref name="uri"/> is not file or the stream cannot be created by given <paramref name="uri"/></exception>
		/// <exception cref="IOException">If other I/O error occurred</exception>
		/// <exception cref="UnauthorizedAccessException">If the give path in <paramref name="uri"/> cannot be created or overwritten</exception>
		public FileStreamPointer(Uri uri, ulong length)
		{
			if (uri.Scheme != Uri.UriSchemeFile)
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
			this.stream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite);
			this.uri = uri;
			this.stream.SetLength((long)length);
			this.stream.Flush();
		}



		#region new default implementations
		/// <summary>
		/// Write values to this <see cref="IStreamPointer"/> starting from <paramref name="offset"/>
		/// </summary>
		/// <param name="offset">The offset in bytes to start writing</param>
		/// <param name="value">The values to write as a <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/></param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offset"/> exceeds the boundary</exception>
		/// <exception cref="IOException">If an I/O error occurs</exception>
		/// <exception cref="NotSupportedException">If the <see cref="WriteStream"/> does not support seeking (when <c><see cref="ReadStream"/>.<see cref="Stream.Position">Offset</see> != <paramref name="offset"/></c>) or writing</exception>
		/// <exception cref="ObjectDisposedException">If the <see cref="WriteStream"/> is already closed</exception>
		/// <remarks>When overridden by a derived class, <see cref="OnWriteFinish"/> shall be invoked at last.</remarks>
		public virtual void Write(long offset, ReadOnlySpan<byte> value)
		{
			try
			{
				if (offset < 0)
					throw new ArgumentOutOfRangeException(nameof(offset), Parameter.CannotNegative);
				if (value.Length <= 0)
					throw new ArgumentOutOfRangeException(nameof(value), Parameter.MustPositive);
				if (offset >= this.WriteStream.Length)
					throw new ArgumentOutOfRangeException(nameof(offset), Parameter.InvalidValue);

				if (offset != this.WriteStream.Position)
					this.WriteStream.Position = offset;
				this.WriteStream.Write(value);
				this.WriteStream.Flush();
			}
			finally
			{
				this.OnWriteFinish();
			}
		}

		/// <summary>
		/// Read values from this <see cref="IStreamPointer"/> starting from <paramref name="offset"/>
		/// </summary>
		/// <param name="offset">The offset in bytes to start reading</param>
		/// <param name="target">The target <see cref="Span{T}"/> of <see cref="byte"/> to overwrite the read values</param>
		/// <returns>The actual number of values read</returns>
		/// <exception cref="ArgumentException">If the <see cref="ReadStream"/> is too short</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offset"/> exceeds the boundary</exception>
		/// <exception cref="IOException">If an I/O error occurs</exception>
		/// <exception cref="NotSupportedException">If the <see cref="ReadStream"/> does not support seeking (when <c><see cref="ReadStream"/>.<see cref="Stream.Position">Offset</see> != <paramref name="offset"/></c>) or reading</exception>
		/// <exception cref="ObjectDisposedException">If the <see cref="ReadStream"/> is already closed</exception>
		/// <remarks>When overridden by a derived class, <see cref="OnReadFinish"/> shall be invoked at last.</remarks>
		public virtual int Read(long offset, Span<byte> target)
		{
			try
			{
				if (offset < 0)
					throw new ArgumentOutOfRangeException(nameof(offset), Parameter.CannotNegative);
				if (target.Length <= 0)
					throw new ArgumentOutOfRangeException(nameof(target), Parameter.MustPositive);
				if (offset + target.Length > this.ReadStream.Length)
					throw new ArgumentException(Parameter.WrongSize);

				if (offset != this.ReadStream.Position)
					this.ReadStream.Position = offset;
				return this.ReadStream.Read(target);
			}
			finally
			{
				this.OnReadFinish();
			}
		}
		#endregion
	}


	/// <summary>
	/// An implementation of <see cref="IStreamPointer"/> for FTP. This is implemented as a class to prevent boxing and unboxing.
	/// </summary>
	public class TcpStreamPointer : IStreamPointer
	{
		#region URI
		private readonly Uri uri;

		/// <summary>
		/// Get the raw URI of this <see cref="FileStreamPointer"/> as a <see cref="Uri"/>
		/// </summary>
		public Uri OriginalUri => this.uri;

		/// <summary>
		/// The basic description of this <see cref="IStreamPointer"/> as a <see cref="string"/>, such as <see cref="Uri.ToString"/>
		/// </summary>
		protected override string Description => this.OriginalUri.ToString();

		private readonly NetworkCredential credential;
		#endregion

		#region FTP web
		private FtpWebResponse read = null;

		private Stream readStream = null;

		private static FtpWebResponse GetResponse(FtpWebRequest request, bool @throw = true)
		{
			var response = (FtpWebResponse)request.GetResponse();
			if (response.StatusCode != FtpStatusCode.CommandOK && response.StatusCode != FtpStatusCode.FileActionOK)
			{
				response.Close();
				if (@throw)
					throw new WebException(response.StatusDescription);
			}
			return response;
		}

		/// <summary>
		/// Get the raw stream of this <see cref="FileStreamPointer"/> used for reading as a <see cref="Stream"/>
		/// </summary>
		protected override Stream ReadStream {
			get {
				if (this.readStream is not null)
				{
					return this.readStream;
				}
				try
				{
					FtpWebRequest request = (FtpWebRequest)WebRequest.Create(uri.AbsoluteUri);
					request.Method = WebRequestMethods.Ftp.DownloadFile;
					this.read = GetResponse(request);
					this.readStream = this.read.GetResponseStream();
					return this.readStream;
				}
				catch (Exception)
				{
					this.OnReadFinish();
					throw;
				}
			}
		}

		/// <summary>
		/// Get the raw stream of this <see cref="FileStreamPointer"/> used for writing as a <see cref="Stream"/>. Always returns null since FTP does not support overwritten part of the file.
		/// </summary>
		protected override Stream WriteStream => null;

		/// <summary>
		/// Get a <see cref="bool"/> indicating whether this pointer can be read or not
		/// </summary>
		public override bool CanRead => true;

		/// <summary>
		/// This <see cref="TcpStreamPointer"/> cannot be written since FTP does not support that.
		/// </summary>
		public override bool CanWrite => false;

		/// <summary>
		/// This <see cref="TcpStreamPointer"/> cannot be read with offset since FTP does not support that.
		/// </summary>
		public override bool CanReadOffset => false;

		/// <summary>
		/// This <see cref="TcpStreamPointer"/> cannot be written with offset since FTP does not support that.
		/// </summary>
		public override bool CanWriteOffset => false;

		/// <summary>
		/// The method to be implemented to actually dispose the resources held by this <see cref="IStreamPointer"/>
		/// </summary>
		/// <param name="disposing">Dispose managed resources or not</param>
		protected override void Dispose(bool disposing)
		{
			this.OnReadFinish();
		}

		/// <summary>
		/// Perform the operations need when a <see cref="ReadStream"/> just stopped using by disconnecting with the remote FTP server.
		/// </summary>
		protected override void OnReadFinish()
		{
			this.readStream?.Dispose();
			this.readStream = null;
			this.read?.Close();
			this.read = null;
		}
		#endregion

		#region create
		/// <summary>
		/// Create a new <see cref="TcpStreamPointer"/> with given <see cref="Uri"/> of file
		/// </summary>
		/// <param name="uri">The given <see cref="Uri"/> of TCP scheme</param>
		/// <param name="length">The initial length in bytes</param>
		/// <param name="credential"><see cref="NetworkCredential"/> used to login the TCP server, default null</param>
		/// <exception cref="ArgumentNullException">If <paramref name="uri"/> is null</exception>
		/// <exception cref="NotSupportedException">If the scheme of <paramref name="uri"/> is not TCP or the stream cannot be created by given <paramref name="uri"/></exception>
		/// <exception cref="WebException">If an web error occurred</exception>
		public TcpStreamPointer(Uri uri, ulong length, NetworkCredential credential = null)
		{
			if (uri is null)
				throw new ArgumentNullException(nameof(uri));
			if (uri.GetScheme() != UriScheme.TCP)
				throw new NotSupportedException(Support.Location);
			// check
			TcpClient client = new TcpClient(uri.Host, uri.Port);
			client.GetStream();
		}
		#endregion
	}
}