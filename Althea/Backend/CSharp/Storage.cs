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

		#region private methods
		private bool IsSupported(StorageLocation location) => location == CpuAlone || location == FileAlone || (this.TempFTPFolder is not null && location == FTPAlone);
		private long GetPointerOffset<T>(PointerSegment pointer, out IMemoryPointer memoryPointer, out IStreamPointer streamPointer, bool @throw = true) where 
			T : unmanaged
		{
			memoryPointer = null; streamPointer = null;
			// check first
			if (!pointer.IsValid() || pointer.OffsetInBytes % Storage<T>.SizeOfT != 0 || pointer.LengthInBytes % Storage<T>.SizeOfT != 0)
			{
				if (@throw)
					throw new NotSupportedException(Support.Location);
				return 0;
			}
			// cast
			if (pointer.Location == CpuAlone && pointer.Pointer is IMemoryPointer mp)
			{
				memoryPointer = mp;
			}
			else if (pointer.Location == FileAlone && pointer.Pointer is IStreamPointer sp1)
			{
				streamPointer = sp1;
			}
			else if (this.TempFTPFolder is not null && pointer.Location == FTPAlone && pointer.Pointer is IStreamPointer sp2)
			{
				streamPointer = sp2;
			}
			else if (@throw)
			{
				throw new NotSupportedException(Support.Location);
			}
			return (long)(pointer.OffsetInBytes / Storage<T>.SizeOfT);
		}

		private long GetPointerOffset(PointerSegment pointer, out IMemoryPointer memoryPointer, out IStreamPointer streamPointer, bool @throw = true) => this.GetPointerOffset<byte>(pointer, out memoryPointer, out streamPointer, @throw);

		#endregion

		#region low-level storage operations
		public override PointerSegment Allocate(StorageLocation location, ulong length)
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
			return new PointerSegment(location, pointer);
		}

		public override bool Free(PointerSegment pointer, bool disposeManaged)
		{
			var offset = this.GetPointerOffset(pointer, out IMemoryPointer mp, out IStreamPointer sp, @throw: false);
			if (offset != 0)
				return false;

			if (mp is not null)
			{
				Marshal.FreeHGlobal(mp.Pointer);
				return true;
			}
			else if (sp is not null)
			{
				sp.NativeStream.Dispose();
				return true;
			}
			else
			{
				return false;
			}
		}

		public unsafe override void SetMemoryValue(PointerSegment pointer, byte value)
		{
			var offset = this.GetPointerOffset(pointer, out IMemoryPointer mp, out IStreamPointer sp);
			if (mp is not null)
			{
				Unsafe.InitBlock(mp.NativePointer(offset), value, checked((uint)pointer.LengthInBytes));
			}
			else
			{
				sp.SetValues(offset, (long)pointer.LengthInBytes, value);
			}
		}

		public override void SetMemoryValue<T>(PointerSegment pointer, T value)
		{
			var offset = this.GetPointerOffset<T>(pointer, out IMemoryPointer mp, out IStreamPointer sp);
			if (mp is not null)
			{
				mp.AsSpan<T>(pointer).Fill(value);
			}
			else
			{
				sp.SetValues(offset, (long)(pointer.LengthInBytes / Storage<T>.SizeOfT), value);
			}
		}

		public override void MemoryCopy(PointerSegment source, PointerSegment destination)
		{
			long srcOff = this.GetPointerOffset(source, out IMemoryPointer srcMP, out IStreamPointer srcSP);
			long dstOff = this.GetPointerOffset(destination, out IMemoryPointer dstMP, out IStreamPointer dstSP);

			uint copyLength = checked((uint)Math.Min(source.LengthInBytes, destination.LengthInBytes));
			if (srcMP is not null && dstMP is not null)
			{
				unsafe
				{
					Unsafe.CopyBlock(srcMP.NativePointer(srcOff), dstMP.NativePointer(dstOff), copyLength);
				}
			}
			else if (srcMP is not null && dstSP is not null)
			{
				dstSP.Write(dstOff, srcMP.AsSpan<byte>(source));
			}
			else if (srcSP is not null && dstMP is not null)
			{
				srcSP.Read(srcOff, dstMP.AsSpan<byte>(destination));
			}
			else // both stream pointers
			{
				srcSP.CopyTo(srcOff, dstSP, dstOff, copyLength);
			}
		}

		public override void MemoryCopy2D(PointerSegment source, ulong sourceLD, PointerSegment destination, ulong destinationLD, ulong height, ulong width)
		{
			if (sourceLD == 0)
				throw new ArgumentOutOfRangeException(nameof(sourceLD), Parameter.MustPositive);
			if (destinationLD == 0)
				throw new ArgumentOutOfRangeException(nameof(destinationLD), Parameter.MustPositive);
			if (width == 0)
				throw new ArgumentOutOfRangeException(nameof(width), Parameter.MustPositive);
			if (height == 0)
				throw new ArgumentOutOfRangeException(nameof(height), Parameter.MustPositive);
			if (height > sourceLD || height > destinationLD)
				throw new ArgumentException(Parameter.InvalidValue, nameof(height));
			if (sourceLD * width > source.LengthInBytes)
				throw new ArgumentException(Parameter.WrongSize, nameof(source));
			if (destinationLD * width > destination.LengthInBytes)
				throw new ArgumentException(Parameter.WrongSize, nameof(destination));
			// shortcut
			if (sourceLD == destinationLD && sourceLD == height)
			{
				this.MemoryCopy(source.AsLength(height * width), destination.AsLength(height * width));
				return;
			}
			// normal cases
			long srcOff = this.GetPointerOffset(source, out IMemoryPointer srcMP, out IStreamPointer srcSP);
			long dstOff = this.GetPointerOffset(destination, out IMemoryPointer dstMP, out IStreamPointer dstSP);
			if (srcMP is not null && dstMP is not null)
			{
				uint hh = checked((uint)height);
				unsafe
				{
					byte* srcPtr = (byte*)srcMP.NativePointer(srcOff);
					byte* endPtr = srcPtr + (sourceLD * width);
					byte* dstPtr = (byte*)dstMP.NativePointer(dstOff);
					for (; srcPtr < endPtr; srcPtr += sourceLD, dstPtr += destinationLD)
					{
						Unsafe.CopyBlock(dstPtr, srcPtr, hh);
					}
				}
				return;
			}
			long end = (long)(source.OffsetInBytes + sourceLD * width);
			long srcLD = (long)sourceLD, dstLD = (long)destinationLD;
			int h = checked((int)height);
			if (srcMP is not null && dstSP is not null)
			{
				for (; srcOff < end; srcOff += srcLD, dstOff += dstLD)
				{
					dstSP.Write(dstOff, srcMP.AsSpan<byte>(srcOff, h));
				}
			}
			else if (srcSP is not null && dstMP is not null)
			{
				for (; srcOff < end; srcOff += srcLD, dstOff += dstLD)
				{
					srcSP.Read(srcOff, dstMP.AsSpan<byte>(dstOff, h));
				}
			}
			else // both stream pointers
			{
				for (; srcOff < end; srcOff += srcLD, dstOff += dstLD)
				{
					srcSP.CopyTo(srcOff, dstSP, dstOff, h);
				}
			}
		}
		#endregion

		#region low-level storage and manged operations
		public override T ToManaged<T>(PointerSegment source)
		{
			long offset = this.GetPointerOffset<T>(source, out IMemoryPointer mp, out IStreamPointer sp);
			if (mp is not null)
			{
				unsafe { return Unsafe.Read<T>(mp.UnmangedPointer<T>(offset)); }
			}
			else
			{
				Span<T> span = stackalloc T[1];
				sp.Read(offset, span.UncheckAs<T, byte>());
				return span[0];
			}
		}

		public override void FromManaged<T>(PointerSegment destination, T value)
		{
			long offset = this.GetPointerOffset<T>(destination, out IMemoryPointer mp, out IStreamPointer sp);
			if (mp is not null)
			{
				unsafe { Unsafe.Write(mp.UnmangedPointer<T>(offset), value); }
			}
			else
			{
				Span<T> span = stackalloc T[1];
				span[0] = value;
				sp.Write(offset, span.UncheckAs<T, byte>());
			}
		}

		public override void ToManaged<T>(PointerSegment source, ArraySegment<T> destination)
		{
			long offset = this.GetPointerOffset<T>(source, out IMemoryPointer mp, out IStreamPointer sp);
			if (mp is not null)
			{
				int copyLength = checked((int)Math.Min(source.LengthInBytes / Storage<T>.SizeOfT, (ulong)destination.Count));
				unsafe { mp.AsSpan<T>(offset, copyLength).CopyTo(destination.AsSpan(0, copyLength)); }
			}
			else
			{
				sp.Read(offset, destination.AsSpan().UncheckAs<T, byte>());
			}
		}

		public override void FromManaged<T>(PointerSegment destination, ArraySegment<T> values)
		{
			long offset = this.GetPointerOffset<T>(destination, out IMemoryPointer mp, out IStreamPointer sp);
			if (mp is not null)
			{
				int copyLength = checked((int)Math.Min(destination.LengthInBytes / Storage<T>.SizeOfT, (ulong)values.Count));
				unsafe { values.AsSpan(0, copyLength).CopyTo(mp.AsSpan<T>(offset, copyLength)); }
			}
			else
			{
				sp.Write(offset, values.AsSpan().UncheckAs<T, byte>());
			}
		}

		public override void ToManaged2D<T>(PointerSegment source, ulong leadDim, ulong height, ulong width, ArraySegment<T> destination, ulong destinationLeadDim = 0)
		{
			if (!source.IsValid())
				throw new ArgumentNullException(nameof(source));
			if (leadDim == 0)
				throw new ArgumentOutOfRangeException(nameof(leadDim), Parameter.MustPositive);
			if (width == 0)
				throw new ArgumentOutOfRangeException(nameof(width), Parameter.MustPositive);
			if (height == 0)
				throw new ArgumentOutOfRangeException(nameof(height), Parameter.MustPositive);
			if (height > leadDim || height > destinationLeadDim)
				throw new ArgumentException(Parameter.InvalidValue, nameof(height));
			if (leadDim * width > (source.Pointer.LengthInBytes - source.OffsetInBytes) / Storage<T>.SizeOfT)
				throw new ArgumentException(Parameter.WrongSize, nameof(source));
			if (leadDim * width > (ulong)destination.Count)
				throw new ArgumentException(Parameter.WrongSize, nameof(destination));
			if (destinationLeadDim == 0)
				destinationLeadDim = height;
			// shortcut
			if (leadDim == height && destinationLeadDim == height)
			{
				this.ToManaged(source.AsLength(height * width), destination);
				return;
			}
			// normal cases
			long start = this.GetPointerOffset<T>(source, out IMemoryPointer mp, out IStreamPointer sp);
			int h = checked((int)height), dstLD = checked((int)destinationLeadDim);
			long srcLD = (long)leadDim, max = (long)(leadDim * width) + start;
			int dstOffset = 0;
			
			if (mp is not null)
			{
				for (long srcOffset = start; srcOffset < max; srcOffset += srcLD, dstOffset += dstLD)
				{
					unsafe { mp.AsSpan<T>(srcOffset, h).CopyTo(destination.AsSpan(dstOffset, h)); }
				}
			}
			else
			{
				for (long srcOffset = 0; srcOffset < max; srcOffset += srcLD, dstOffset += dstLD)
				{
					sp.Read(srcOffset, destination.AsSpan(dstOffset, h).UncheckAs<T, byte>());
				}
			}
		}

		public override void FromManaged2D<T>(PointerSegment destination, ulong leadDim, ulong height, ulong width, ArraySegment<T> values, ulong valuesLeadDim)
		{
			if (!destination.IsValid())
				throw new ArgumentNullException(nameof(destination));
			if (leadDim == 0)
				throw new ArgumentOutOfRangeException(nameof(leadDim), Parameter.MustPositive);
			if (width == 0)
				throw new ArgumentOutOfRangeException(nameof(width), Parameter.MustPositive);
			if (height == 0)
				throw new ArgumentOutOfRangeException(nameof(height), Parameter.MustPositive);
			if (height > leadDim || height > valuesLeadDim)
				throw new ArgumentException(Parameter.InvalidValue, nameof(height));
			if (leadDim * width > (destination.Pointer.LengthInBytes - destination.OffsetInBytes) / Storage<T>.SizeOfT)
				throw new ArgumentException(Parameter.WrongSize, nameof(destination));
			if (leadDim * width > (ulong)values.Count)
				throw new ArgumentException(Parameter.WrongSize, nameof(values));
			if (valuesLeadDim == 0)
				valuesLeadDim = height;
			// shortcut
			if (leadDim == height && valuesLeadDim == height)
			{
				this.ToManaged(destination.AsLength(height * width), values);
				return;
			}
			// normal case
			long start = this.GetPointerOffset<T>(destination, out IMemoryPointer mp, out IStreamPointer sp);
			int h = checked((int)height), srcLD = checked((int)(valuesLeadDim == 0 ? height : valuesLeadDim));
			long dstLD = (long)leadDim, max = (long)(leadDim * width) + start;
			int srcOffset = 0;

			if (mp is not null)
			{
				for (long dstOffset = start; dstOffset < max; dstOffset += dstLD, srcOffset += srcLD)
				{
					unsafe { values.AsSpan(srcOffset, h).CopyTo(mp.AsSpan<T>(dstOffset, h)); }
				}
			}
			else
			{
				for (long dstOffset = start; dstOffset < max; dstOffset += dstLD, srcOffset += srcLD)
				{
					sp.Write(dstOffset, values.AsSpan(srcOffset, h).UncheckAs<T, byte>());
				}
			}
		}
		#endregion
	}
}
