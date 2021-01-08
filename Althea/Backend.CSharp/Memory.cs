using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Althea.Memory;


#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释

namespace Althea.CSharp.Memory
{
	/// <summary>
	/// The C# back-end of <see cref="AbstractMemoryApi"/>. <b>Can</b> be inherited.
	/// </summary>
	public class MemoryApi : AbstractMemoryApi
	{
		#region basic
		/// <summary>
		/// The default constructor used by reflection
		/// </summary>
		public MemoryApi()
		{
			// do nothing
		}

		public override void Dispose()
		{
			GC.SuppressFinalize(this);
		}
		#endregion

		#region support
		public override StorageLocation SupportedUnaryLocations => StorageLocation.CpuRam;

		public override IReadOnlyList<StorageLocation> SupportedBinaryLocations { get; } = new[] { StorageLocation.CpuRam };

		public override IReadOnlyDictionary<UriScheme, StorageLocation> SupportedUriTransfers { get; } = new Dictionary<UriScheme, StorageLocation>
		{
			[UriScheme.File] = StorageLocation.CpuRam,
		};
		#endregion

		#region properties
		public override (int major, int minor) DriverVersion(StorageLocation location) => default;

		// since this is not implemented yet (see https://github.com/dotnet/runtime/issues/22948), this is a manual implementation
		public override (ulong free, ulong total) FreeAndTotalMemory(MemoryLocation location)
		{
			var memoryInfo = GC.GetGCMemoryInfo();
			ulong total = unchecked((ulong)memoryInfo.TotalAvailableMemoryBytes);
			ulong free = total - unchecked((ulong)Environment.WorkingSet);
			return (free, total);
		}

		public override int MaxDeviceNumber(StorageLocation location) => 1;
		#endregion

		#region memory
		public override IntPtr Allocate(MemoryLocation location, ulong length) => location.Location == StorageLocation.CpuRam ? Marshal.AllocHGlobal(checked((int)length)) : throw new ArgumentOutOfRangeException(nameof(location), Resource.NotSupportedLocation);

		public override bool Free(MemoryLocation location, IntPtr ptr)
		{
			if (location.Location == StorageLocation.CpuRam)
			{
				Marshal.FreeHGlobal(ptr);
				return true;
			}
			return false;
		}

		public override void SetMemoryValue(StoragePointer storage, byte value)
		{
			if (storage.Location.Location != StorageLocation.CpuRam)
				throw new ArgumentOutOfRangeException(nameof(storage));
			unsafe
			{
				Unsafe.InitBlock(storage.UnmangedPointer, value, checked((uint)storage.LengthInBytes));
			}
		}

		public override void SetMemoryValue<T>(StoragePointer storage, T value)
		{
			if (storage.Location.Location != StorageLocation.CpuRam)
				throw new ArgumentOutOfRangeException(nameof(storage));
			storage.AsSpan<T>().Fill(value);
		}

		public override void MemoryCopy(StoragePointer source, StoragePointer dest)
		{
			if (source.Location.Location != StorageLocation.CpuRam)
				throw new ArgumentOutOfRangeException(nameof(source));
			if (dest.Location.Location != StorageLocation.CpuRam)
				throw new ArgumentOutOfRangeException(nameof(dest));
			unsafe
			{
				Unsafe.CopyBlock(source.UnmangedPointer, dest.UnmangedPointer, checked((uint)Math.Min(source.LengthInBytes, dest.LengthInBytes)));
			}
		}

		public override void MemoryCopy2D(StoragePointer source, ulong sourceLD, StoragePointer dest, ulong destLD, ulong height, ulong width)
		{
			if (source.Location.Location != StorageLocation.CpuRam)
				throw new ArgumentOutOfRangeException(nameof(source));
			if (dest.Location.Location != StorageLocation.CpuRam)
				throw new ArgumentOutOfRangeException(nameof(dest));

			if (sourceLD == destLD && sourceLD == height)
			{
				MemoryCopy(source.AsLength(height * width), dest.AsLength(height * width));
				return;
			}
			uint h = checked((uint)height);
			unsafe
			{
				byte* s = (byte*)source.UnmangedPointer;
				byte* end = s + sourceLD * width;
				byte* d = (byte*)dest.UnmangedPointer;
				for (; s < end; s += sourceLD, d += destLD)
				{
					Unsafe.CopyBlock(d, s, h);
				}
			}
		}
		#endregion

		#region URI
		public override IUriWrapper CreateUriStream(Uri uri) => new UriWrapper(uri);
		#endregion
	}

	/// <summary>
	/// The C# back-end of <see cref="IUriWrapper"/>. <b>Can</b> be inherited.
	/// </summary>
	public class UriWrapper : IUriWrapper
	{
		#region basic
		private readonly FileStream file;

		public Uri OriginalUri { get; }

		public ulong Length => (ulong)this.file.Length;

		private ulong Offset { set => this.file.Position = checked((long)value); }

		public UriWrapper(Uri uri)
		{
			// checks
			if (uri.Scheme != Uri.UriSchemeFile)
				throw new NotSupportedException(Resource.NotSupportedLocation);
			string path = uri.LocalPath;
			if (Directory.Exists(path))
				throw new ArgumentOutOfRangeException(nameof(uri), Resource.NotSupportedLocation);
			else if (File.Exists(path))
			{
				var flags = File.GetAttributes(path);
				if ((flags & (FileAttributes.ReadOnly | FileAttributes.System | FileAttributes.Directory)) != 0)
					throw new ArgumentOutOfRangeException(nameof(uri), Resource.NotSupportedLocation);
			}
			// create
			file = new FileStream(path, FileMode.Create, FileAccess.ReadWrite);
			this.OriginalUri = uri;
		}

		public void Dispose()
		{
			this.file.Close();
			this.file.Dispose();
		}

		public async ValueTask DisposeAsync()
		{
			await Task.Run(() => this.file.Close());
			await this.file.DisposeAsync();
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


		public void CopyTo(IUriWrapper stream, ulong offset, ulong length)
		{
			if (offset + length >= this.Length)
				throw new ArgumentOutOfRangeException(nameof(length));
			if (stream is UriWrapper d)
			{
				this.Offset = offset;
				CopyStream(this.file, d.file, checked((int)length));
			}
			else
			{
				throw new ArgumentException(Resource.NotSupportedLocation, nameof(stream));
			}
		}

		public async ValueTask CopyToAsync(IUriWrapper stream, ulong offset, ulong length)
		{
			if (offset + length >= this.Length)
				throw new ArgumentOutOfRangeException(nameof(length));
			if (stream is UriWrapper d)
			{
				this.Offset = offset;
				await CopyStreamAsync(this.file, d.file, checked((int)length));
			}
			else
			{
				throw new ArgumentException(Resource.NotSupportedLocation, nameof(stream));
			}
		}

		public void Read(StoragePointer pointer, ulong offset)
		{
			if (offset + pointer.LengthInBytes >= this.Length)
				throw new ArgumentOutOfRangeException(nameof(offset));
			if (pointer.Location.Location != StorageLocation.CpuRam)
				throw new NotSupportedException(Resource.NotSupportedLocation);

			this.Offset = offset;
			this.file.Read(pointer.AsSpan<byte>());
		}

		public async ValueTask ReadAsync(StoragePointer pointer, ulong offset)
		{
			if (offset + pointer.LengthInBytes >= this.Length)
				throw new ArgumentOutOfRangeException(nameof(offset));
			if (pointer.Location.Location != StorageLocation.CpuRam)
				throw new NotSupportedException(Resource.NotSupportedLocation);

			this.Offset = offset;
			await ReadStreamAsync(this.file, pointer.Pointer, pointer.LengthInBytes);
		}

		public void Write(StoragePointer pointer, ulong offset)
		{
			if (offset >= this.Length)
				throw new ArgumentOutOfRangeException(nameof(offset));
			if (pointer.Location.Location != StorageLocation.CpuRam)
				throw new NotSupportedException(Resource.NotSupportedLocation);

			this.Offset = offset;
			this.file.Write(pointer.AsSpan<byte>());
		}

		public async ValueTask WriteAsync(StoragePointer pointer, ulong offset)
		{

			if (offset >= this.Length)
				throw new ArgumentOutOfRangeException(nameof(offset));
			if (pointer.Location.Location != StorageLocation.CpuRam)
				throw new NotSupportedException(Resource.NotSupportedLocation);

			this.Offset = offset;
			await WriteStreamAsync(this.file, pointer.Pointer, pointer.LengthInBytes);
		}

		public void Resize(ulong newLength)
		{
			if (newLength >= this.Length)
				return;
			this.file.SetLength(checked((long)newLength));
			this.file.Flush();
		}

		public async ValueTask ResizeAsync(ulong newLength)
		{
			if (newLength >= this.Length)
				return;
			await Task.Run(() => this.file.SetLength(checked((long)newLength)));
			await this.file.FlushAsync();
		}
		#endregion
	}
}
