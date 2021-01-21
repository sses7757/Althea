using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

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

		/// <summary>
		/// Get the raw pointer of this <see cref="IMemoryPointer"/> as a <see cref="IntPtr"/>
		/// </summary>
		public IntPtr Pointer => this.pointer;

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
	/// An implementation of <see cref="AbstractStreamPointer"/> for files. This is implemented as a class to prevent boxing and unboxing.
	/// </summary>
	public class FileStreamPointer : AbstractStreamPointer
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
		/// The basic description of this <see cref="AbstractStreamPointer"/> as a <see cref="string"/>, such as <see cref="Uri.ToString"/>
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

		/// <summary>
		/// The method to be implemented to actually dispose the resources held by this <see cref="FileStreamPointer"/>
		/// </summary>
		/// <param name="disposing">Dispose managed resources or not</param>
		protected override void Dispose(bool disposing) => this.stream.Dispose();
	}


	/// <summary>
	/// An implementation of <see cref="AbstractStreamPointer"/> for FTP. This is implemented as a class to prevent boxing and unboxing.
	/// </summary>
	public class FTPStreamPointer : AbstractStreamPointer
	{
		#region URI
		private readonly Uri uri;

		/// <summary>
		/// Get the raw URI of this <see cref="FileStreamPointer"/> as a <see cref="Uri"/>
		/// </summary>
		public Uri OriginalUri => this.uri;

		/// <summary>
		/// The basic description of this <see cref="AbstractStreamPointer"/> as a <see cref="string"/>, such as <see cref="Uri.ToString"/>
		/// </summary>
		protected override string Description => this.OriginalUri.ToString();

		private readonly NetworkCredential credential;
		#endregion

		#region FTP web
		private FtpWebResponse read = null;

		private Stream readStream = null;

		private static FtpWebResponse GetResponse(FtpWebRequest request)
		{
			var response = (FtpWebResponse)request.GetResponse();
			if (response.StatusCode != FtpStatusCode.CommandOK && response.StatusCode != FtpStatusCode.FileActionOK)
			{
				response.Close();
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
		/// This <see cref="FTPStreamPointer"/> cannot be written since FTP does not support that.
		/// </summary>
		public override bool CanWrite => false;

		/// <summary>
		/// This <see cref="FTPStreamPointer"/> cannot be read with offset since FTP does not support that.
		/// </summary>
		public override bool CanReadOffset => false;

		/// <summary>
		/// This <see cref="FTPStreamPointer"/> cannot be written with offset since FTP does not support that.
		/// </summary>
		public override bool CanWriteOffset => false;

		/// <summary>
		/// The method to be implemented to actually dispose the resources held by this <see cref="AbstractStreamPointer"/>
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
		/// Create a new <see cref="FTPStreamPointer"/> with given <see cref="Uri"/> of file
		/// </summary>
		/// <param name="uri">The given <see cref="Uri"/> of file scheme</param>
		/// <param name="length">The initial length in bytes</param>
		/// <param name="credential"><see cref="NetworkCredential"/> used to login the FTP server, default null</param>
		/// <exception cref="NotSupportedException">If the scheme of <paramref name="uri"/> is not FTP or the stream cannot be created by given <paramref name="uri"/></exception>
		/// <exception cref="WebException">If an web error occurred</exception>
		public FTPStreamPointer(Uri uri, ulong length, NetworkCredential credential = null)
		{
			if (uri.Scheme != Uri.UriSchemeFtp)
				throw new NotSupportedException(Support.Location);
			// check
			FtpWebRequest request = (FtpWebRequest)WebRequest.Create(uri.AbsoluteUri);
			request.Method = WebRequestMethods.Ftp.MakeDirectory;
			if (credential is not null)
				request.Credentials = credential;
			GetResponse(request).Close();
			
		}
		#endregion
	}
}

/*
				Stream CreateStream()
				{
					string path = uri.AbsolutePath;
					string folder = Path.GetDirectoryName(path) ?? "";
					// create given folder
					FtpWebRequest request = (FtpWebRequest)WebRequest.Create(uri.AbsoluteUri);
					request.Method = WebRequestMethods.Ftp.MakeDirectory;
					if (credential is not null)
						request.Credentials = credential;
					using var response = (FtpWebResponse)request.GetResponse();
					if (response.StatusCode != FtpStatusCode.CommandOK)
						throw new WebException(response.StatusDescription);
					WebClient webClient = new WebClient
					{
						BaseAddress = uri.Host
					};
				}
				this.initialization = new Task<Stream>(CreateStream);
 */