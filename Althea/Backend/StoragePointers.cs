using System;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

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
	/// An implementation of <see cref="IStreamPointer"/>. This is implemented as a class to prevent boxing and unboxing.
	/// </summary>
	public class UriStreamPointer : IStreamPointer
	{
		private readonly Stream stream;

		private readonly Uri uri;

		/// <summary>
		/// Get the raw stream of this <see cref="UriStreamPointer"/> as a <see cref="Stream"/>
		/// </summary>
		public Stream NativeStream => this.stream;

		/// <summary>
		/// Get the raw URI of this <see cref="UriStreamPointer"/> as a <see cref="Uri"/>
		/// </summary>
		public Uri OriginalUri => this.uri;

		/// <summary>
		/// The original length of this pointer's underlying storage in bytes
		/// </summary>
		public ulong LengthInBytes => (ulong)this.stream.Length;

		/// <summary>
		/// The basic description of this <see cref="IStreamPointer"/> as a <see cref="string"/>, such as <see cref="Uri.ToString"/>
		/// </summary>
		public string Description => this.OriginalUri.ToString();

		/// <summary>
		/// Create a new <see cref="UriStreamPointer"/> with given <see cref="Uri"/>
		/// </summary>
		/// <param name="uri">The given <see cref="Uri"/></param>
		/// <exception cref="NotSupportedException">If the scheme of <paramref name="uri"/> is not supported or the stream cannot be created by given <paramref name="uri"/></exception>
		public UriStreamPointer(Uri uri)
		{
			// checks
			if (uri.Scheme != Uri.UriSchemeFile)
				throw new NotSupportedException(Support.Location);
			string path = uri.LocalPath;
			if (Directory.Exists(path))
				throw new NotSupportedException(Support.Location);
			else if (File.Exists(path))
			{
				var flags = File.GetAttributes(path);
				if ((flags & (FileAttributes.ReadOnly | FileAttributes.System | FileAttributes.Directory)) != 0)
					throw new NotSupportedException(Support.Location);
			}
			// create
			this.stream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite);
			this.uri = uri;
		}
	}
}
