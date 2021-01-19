using System;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using Althea.Storage;
using Althea.Resources;


namespace Althea.Backend.Storage
{
	/// <summary>
	/// An implementation of <see cref="IMemoryPointer"/>
	/// </summary>
	public readonly struct MemoryPointer : IMemoryPointer
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

		/// <summary>
		/// Get the unmanaged pointer of this <see cref="MemoryPointer"/>
		/// </summary>
		public unsafe void* UnmangedPointer => this.pointer.ToPointer();

		/// <summary>
		/// Get the <see cref="Span{T}"/> representation of this <see cref="MemoryPointer"/>
		/// </summary>
		/// <typeparam name="T">any data type</typeparam>
		/// <returns>The <see cref="Span{T}"/> representation of this <see cref="MemoryPointer"/></returns>
		public unsafe Span<T> AsSpan<T>() => new Span<T>(this.UnmangedPointer, checked((int)this.length));
	}

	/// <summary>
	/// An implementation of <see cref="IStreamPointer"/>
	/// </summary>
	public class UriStreamPointer : IStreamPointer, IDisposable, IAsyncDisposable
	{
		#region basic
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

		bool ICheckValid.IsValid() => this.stream is not null && this.uri is not null && !this.disposed;

		private ulong Offset { set => this.stream.Position = checked((long)value); }

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

		private bool disposed = false;

		/// <summary>
		/// Dispose this <see cref="UriStreamPointer"/>
		/// </summary>
		public void Dispose()
		{
			//this.stream.Close();
			this.stream.Dispose();
			this.disposed = true;
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Dispose this <see cref="UriStreamPointer"/> asynchronously
		/// </summary>
		/// <returns>The <see cref="ValueTask"/> of this asynchronous operation</returns>
		public async ValueTask DisposeAsync()
		{
			await Task.Run(() => this.Dispose());
		}
		#endregion

		#region methods
		private const int BufferSize = 1 << 16;

		private static void CopyStream(Stream input, Stream output, int bytes)
		{
			byte[] buffer = new byte[Math.Min(BufferSize, bytes)];
			int read;
			while (bytes > 0 && (read = input.Read(buffer, 0, buffer.Length)) > 0)
			{
				output.Write(buffer, 0, read);
				bytes -= read;
			}
			output.Flush();
		}
		private static async ValueTask CopyStreamAsync(Stream input, Stream output, int bytes)
		{
			byte[] buffer = new byte[Math.Min(BufferSize, bytes)];
			var bufferMemory = buffer.AsMemory();
			int read;
			while (bytes > 0)
			{
				read = await input.ReadAsync(bufferMemory);
				if (read == buffer.Length)
					await output.WriteAsync(bufferMemory);
				else
					await output.WriteAsync(buffer.AsMemory(0, read));
				bytes -= read;
			}
			await output.FlushAsync();
		}
		private static async ValueTask ReadStreamAsync(Stream stream, IntPtr ptr, ulong length)
		{
			byte[] buffer = new byte[Math.Min(BufferSize, length)];
			var bufferMemory = buffer.AsMemory();
			while (length > 0)
			{
				int read = await stream.ReadAsync(bufferMemory);
				Marshal.Copy(buffer, 0, ptr, read);
				length -= (ulong)read;
				ptr += read;
			}
			stream.Flush();
		}
		private static async ValueTask WriteStreamAsync(Stream stream, IntPtr ptr, ulong length)
		{
			byte[] buffer = new byte[Math.Min(BufferSize, length)];
			var bufferMemory = buffer.AsMemory();
			while (length >= BufferSize)
			{
				Marshal.Copy(ptr, buffer, 0, BufferSize);
				await stream.WriteAsync(bufferMemory);
				length -= BufferSize;
				ptr += BufferSize;
			}
			if (length > 0)
			{
				Marshal.Copy(ptr, buffer, 0, (int)length);
				await stream.WriteAsync(buffer.AsMemory(0, (int)length));
			}
			await stream.FlushAsync();
		}


		public void CopyTo(UriStreamPointer stream, ulong offset, ulong length)
		{
			if (offset + length >= this.LengthInBytes)
				throw new ArgumentOutOfRangeException(nameof(length));
			if (stream is UriStreamPointer d)
			{
				this.Offset = offset;
				CopyStream(this.stream, d.stream, checked((int)length));
			}
			else
			{
				throw new NotSupportedException(Support.Location);
			}
		}

		public async ValueTask CopyToAsync(UriStreamPointer stream, ulong offset, ulong length)
		{
			if (offset + length >= this.LengthInBytes)
				throw new ArgumentOutOfRangeException(nameof(length));
			if (stream is UriStreamPointer d)
			{
				this.Offset = offset;
				await CopyStreamAsync(this.stream, d.stream, checked((int)length));
			}
			else
			{
				throw new NotSupportedException(Support.Location);
			}
		}

		public void Read(StoragePointer pointer, ulong offset)
		{
			if (offset + pointer.LengthInBytes >= this.LengthInBytes)
				throw new ArgumentOutOfRangeException(nameof(offset));
			if (pointer.Location.Location != LocationType.CpuRam)
				throw new NotSupportedException(Support.Location);

			this.Offset = offset;
			this.stream.Read(pointer.AsSpan<byte>());
		}

		public async ValueTask ReadAsync(StoragePointer pointer, ulong offset)
		{
			if (offset + pointer.LengthInBytes >= this.LengthInBytes)
				throw new ArgumentOutOfRangeException(nameof(offset));
			if (pointer.Location.Location != LocationType.CpuRam)
				throw new NotSupportedException(Support.Location);

			this.Offset = offset;
			await ReadStreamAsync(this.stream, pointer.Pointer, pointer.LengthInBytes);
		}

		public void Write(StoragePointer pointer, ulong offset)
		{
			if (offset >= this.LengthInBytes)
				throw new ArgumentOutOfRangeException(nameof(offset));
			if (pointer.Location.Location != LocationType.CpuRam)
				throw new NotSupportedException(Support.Location);

			this.Offset = offset;
			this.stream.Write(pointer.AsSpan<byte>());
		}

		public async ValueTask WriteAsync(StoragePointer pointer, ulong offset)
		{

			if (offset >= this.LengthInBytes)
				throw new ArgumentOutOfRangeException(nameof(offset));
			if (pointer.Location.Location != LocationType.CpuRam)
				throw new NotSupportedException(Support.Location);

			this.Offset = offset;
			await WriteStreamAsync(this.stream, pointer.Pointer, pointer.LengthInBytes);
		}

		public void Resize(ulong newLength)
		{
			if (newLength >= this.LengthInBytes)
				return;
			this.stream.SetLength(checked((long)newLength));
			this.stream.Flush();
		}

		public async ValueTask ResizeAsync(ulong newLength)
		{
			if (newLength >= this.LengthInBytes)
				return;
			await Task.Run(() => this.stream.SetLength(checked((long)newLength)));
			await this.stream.FlushAsync();
		}
		#endregion
	}
}
