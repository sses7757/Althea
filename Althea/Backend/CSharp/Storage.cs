using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Althea.Linq;
using Althea.Storage;
using Althea.Resources;
using Althea.Backend.Storage;


#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释

namespace Althea.Backend.CSharp.Storage
{
	/// <summary>
	/// The C# back-end of <see cref="AbstractApi"/> that support storage locations of CPU and file and (possible) FTP.
	/// </summary>
	public class StorageApi : AbstractApi
	{
		#region basic
		/// <summary>
		/// The default constructor used by reflection
		/// </summary>
		public StorageApi()
		{
			// do nothing
		}

		protected override void Dispose(bool disposeManaged)
		{
			// do nothing
		}

		/// <summary>
		/// Get or set the folder to put the temporary files, default is <see cref="Path.GetTempPath"/>
		/// </summary>
		/// <remarks>If you set an invalid value, there may be exception thrown</remarks>
		public string TempFileFolder { get; set; } = Path.GetTempPath();

		/// <summary>
		/// Get or set the host and folder to put the temporary files in FTP, default null means that this <see cref="StorageApi"/> does not support FTP storage location
		/// </summary>
		/// <remarks>If you set an invalid value, there may be exception thrown</remarks>
		public Uri TempFTPFolder { get; set; } = null;
		#endregion

		#region support
		private bool IsSupported(StorageLocation location) => location == CpuAlone || location == FileAlone || (this.TempFTPFolder is not null && location == FTPAlone);

		private static readonly StorageLocation CpuAlone = new StorageLocation(LocationType.CpuRam, 0);
		private static readonly StorageLocation FileAlone = new StorageLocation(LocationType.Uri, (int)UriScheme.File);
		private static readonly StorageLocation FTPAlone = new StorageLocation(LocationType.Uri, (int)UriScheme.File);

		private static readonly IReadOnlyList<CombinationOfLocations> NoFTPUnary = GenerateUnaryLoactions(CpuAlone, FileAlone),
			WithFTPUnary = GenerateUnaryLoactions(CpuAlone, FileAlone, FTPAlone);

		private static readonly IReadOnlyList<ImmutableTwoElementSet<CombinationOfLocations>> NoFTPBinary = GenerateBinaryLoactions(CpuAlone, FileAlone),
			WithFTPBinary = GenerateBinaryLoactions(CpuAlone, FileAlone, FTPAlone);

		public override IReadOnlyList<CombinationOfLocations> SupportedUnaryLocations => this.TempFTPFolder is null ? NoFTPUnary : WithFTPUnary;

		public override IReadOnlyList<ImmutableTwoElementSet<CombinationOfLocations>> SupportedBinaryLocations => this.TempFTPFolder is null ? NoFTPBinary : WithFTPBinary;

		public override IReadOnlyList<CombinationOfLocations> SupportedManagedTransfer => this.SupportedUnaryLocations;
		#endregion

		#region properties
		public override (int major, int minor) DriverVersion(LocationType location) => default;

		// since this is not implemented yet (see https://github.com/dotnet/runtime/issues/22948), this is a manual implementation
		public override (ulong free, ulong total) FreeAndTotalMemory(StorageLocation location)
		{
			var memoryInfo = GC.GetGCMemoryInfo();
			ulong total = unchecked((ulong)memoryInfo.TotalAvailableMemoryBytes);
			ulong free = total - unchecked((ulong)Environment.WorkingSet);
			return (free, total);
		}

		public override int MaxDeviceNumber(LocationType location) => 1;
		#endregion

		#region low-level storage operations
		public override StoragePointer Allocate(StorageLocation location, ulong length)
		{
			if (!this.IsSupported(location))
				throw new NotSupportedException(Support.Location);

			IPointer pointer; // box struct to interface
			if (location == CpuAlone)
			{
				var ptr = Marshal.AllocHGlobal(checked((int)length));
				pointer = new MemoryPointer(ptr, length);
			}
			else
			{
				Uri uri;
				if (location == FileAlone)
				{
					string file = Path.Combine(this.TempFileFolder, Guid.NewGuid().ToString());
					uri = new Uri(Uri.UriSchemeFile + ":///" + file);
				}
				else // FTP
				{
					var builder = new UriBuilder(this.TempFTPFolder);
					builder.Path = Path.Combine(builder.Path, Guid.NewGuid().ToString());
					uri = builder.Uri;
				}
				pointer = new UriStreamPointer(uri);
			}
			return new StoragePointer(location, pointer);
		}

		public override bool Free(StoragePointer pointer, bool disposeManaged)
		{
			if (pointer.Location == CpuAlone && pointer.Pointer is MemoryPointer mp)
			{
				Marshal.FreeHGlobal(mp.Pointer);
				return true;
			}
			else if (pointer.Pointer is UriStreamPointer sp)
			{
				sp.Dispose();
				return true;
			}
			else
			{
				return false;
			}
		}

		public unsafe override void SetMemoryValue(StoragePointer pointer, byte value)
		{
			if (pointer.Location == CpuAlone && pointer.Pointer is MemoryPointer mp)
			{
				Unsafe.InitBlock(mp.Pointer.ToPointer(), value, checked((uint)pointer.LengthInBytes));
			}
			else if (pointer.Pointer is UriStreamPointer sp)
			{
				// TODO: UriStreamPointer set value
			}
			else
			{
				throw new NotSupportedException(Support.Location);
			}
		}

		public override void SetMemoryValue<T>(StoragePointer pointer, T value)
		{
			if (pointer.Location == CpuAlone && pointer.Pointer is MemoryPointer mp)
			{
				mp.AsSpan<T>().Fill(value);
			}
			else if (pointer.Pointer is UriStreamPointer sp)
			{
				// TODO: UriStreamPointer set value
			}
			else
			{
				throw new NotSupportedException(Support.Location);
			}
		}

		public override void MemoryCopy(StoragePointer source, StoragePointer destination)
		{
			// TODO
			unsafe
			{
				Unsafe.CopyBlock(source.UnmangedPointer, destination.UnmangedPointer, checked((uint)Math.Min(source.LengthInBytes, destination.LengthInBytes)));
			}
		}

		public override void MemoryCopy2D(StoragePointer source, ulong sourceLD, StoragePointer destination, ulong destLD, ulong height, ulong width)
		{
			if (source.Location.Location != LocationType.CpuRam || destination.Location.Location != LocationType.CpuRam)
				throw new NotSupportedException(Support.Location);
			if (sourceLD == 0)
				throw new ArgumentOutOfRangeException(nameof(sourceLD), Parameter.MustPositive);
			if (destLD == 0)
				throw new ArgumentOutOfRangeException(nameof(destLD), Parameter.MustPositive);
			if (width == 0)
				throw new ArgumentOutOfRangeException(nameof(width), Parameter.MustPositive);
			if (height == 0)
				throw new ArgumentOutOfRangeException(nameof(height), Parameter.MustPositive);
			if (height > sourceLD || height > destLD)
				throw new ArgumentException(Parameter.InvalidValue, nameof(height));

			if (sourceLD == destLD && sourceLD == height)
			{
				MemoryCopy(source.AsLength(height * width), destination.AsLength(height * width));
				return;
			}

			// TODO
			uint h = checked((uint)height);
			unsafe
			{
				byte* s = (byte*)source.UnmangedPointer;
				byte* end = s + sourceLD * width;
				byte* d = (byte*)destination.UnmangedPointer;
				for (; s < end; s += sourceLD, d += destLD)
				{
					Unsafe.CopyBlock(d, s, h);
				}
			}
		}
		#endregion

		#region low-level storage and manged operations
		public override T ToManaged<T>(StoragePointer source) => throw new NotImplementedException();

		public override void FromManaged<T>(StoragePointer destination, T value) => throw new NotImplementedException();

		public override void ToManaged<T>(StoragePointer source, ArraySegment<T> destination) => throw new NotImplementedException();

		public override void FromManaged<T>(StoragePointer destination, ArraySegment<T> values) => throw new NotImplementedException();

		public override void ToManaged2D<T>(StoragePointer source, ulong leadDim, ulong height, ulong width, ArraySegment<T> destination) => throw new NotImplementedException();

		public override void FromManaged2D<T>(StoragePointer destination, ulong leadDim, ulong height, ulong width, ArraySegment<T> values) => throw new NotImplementedException();
		#endregion
	}
}
