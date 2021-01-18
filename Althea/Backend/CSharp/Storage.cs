using System;
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
	/// The C# back-end of <see cref="AbstractApi"/>. <b>Can</b> be inherited.
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
		#endregion

		#region support
		private static readonly StorageLocation CpuAlone = new StorageLocation(LocationType.CpuRam, 0);
		private static readonly StorageLocation FileAlone = new StorageLocation(LocationType.Uri, (int)UriScheme.File);
		private static readonly StorageLocation FTPAlone = new StorageLocation(LocationType.Uri, (int)UriScheme.FTP);

		public override IReadOnlyList<CombinationOfLocations> SupportedUnaryLocations { get; } = new[]
		{
			CpuAlone,
			FileAlone,
			FTPAlone,
			new CombinationOfLocations(CombinationType.PureOrMixed, CpuAlone, FileAlone),
		};

		public override IReadOnlyList<ImmutableTwoElementSet<CombinationOfLocations>> SupportedBinaryLocations { get; } = new[]
		{
			new ImmutableTwoElementSet<CombinationOfLocations>(CpuAlone, CpuAlone)
		};
		public override IReadOnlyList<CombinationOfLocations> SupportedManagedTransfer { get; } = new[]
		{
			CpuAlone,
			FileAlone,
			new CombinationOfLocations(CombinationType.PureOrMixed, new[] { CpuAlone, FileAlone })
		};
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

		#region low-level memory operations
		public override StoragePointer Allocate(StorageLocation location, ulong length)
		{
			if (location.Location != LocationType.CpuRam)
				throw new NotSupportedException(Support.Location);
			return Marshal.AllocHGlobal(checked((int)length));
		}

		public override bool Free(StoragePointer pointer, bool disposeManaged)
		{
			if (location.Location == LocationType.CpuRam)
			{
				Marshal.FreeHGlobal(ptr);
				return true;
			}
			return false;
		}

		public override void SetMemoryValue(StoragePointer storage, byte value)
		{
			if (storage.Location.Location != LocationType.CpuRam)
				throw new NotSupportedException(Support.Location);
			unsafe
			{
				Unsafe.InitBlock(storage.UnmangedPointer, value, checked((uint)storage.LengthInBytes));
			}
		}

		public override void SetMemoryValue<T>(StoragePointer storage, T value)
		{
			if (storage.Location.Location != LocationType.CpuRam)
				throw new NotSupportedException(Support.Location);
			storage.AsSpan<T>().Fill(value);
		}

		public override void MemoryCopy(StoragePointer source, StoragePointer destination)
		{
			if (source.Location.Location != LocationType.CpuRam || destination.Location.Location != LocationType.CpuRam)
				throw new NotSupportedException(Support.Location);
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
				throw new ArgumentOutOfRangeException(nameof(sourceLD), Resources.Parameter.MustPositive);
			if (destLD == 0)
				throw new ArgumentOutOfRangeException(nameof(destLD), Resources.Parameter.MustPositive);
			if (width == 0)
				throw new ArgumentOutOfRangeException(nameof(width), Resources.Parameter.MustPositive);
			if (height == 0)
				throw new ArgumentOutOfRangeException(nameof(height), Resources.Parameter.MustPositive);
			if (height > sourceLD || height > destLD)
				throw new ArgumentException(Resources.Parameter.InvalidValue, nameof(height));

			if (sourceLD == destLD && sourceLD == height)
			{
				MemoryCopy(source.AsLength(height * width), destination.AsLength(height * width));
				return;
			}
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

		#region URI
		public override IUriWrapper CreateUriStream(Uri uri) => new UriWrapper(uri);
		#endregion
	}
}
