using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Althea.Linq;
using Althea.Storage;
using Althea.Resources;
using Althea.Backend.Storage;

using static Althea.Backend.Storage.ConcretePointersExtension;


#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释

namespace Althea.Backend.CSharp.Storage
{
	/// <summary>
	/// The C# back-end of <see cref="AbstractApi"/> that support storage locations of CPU and file and (possible) TCP.
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
		/// Get or set the host and folder to put the temporary files using TCP, default null means that this <see cref="StorageApi"/> does not support TCP storage location
		/// </summary>
		/// <remarks>If you set an invalid value, there may be exception thrown</remarks>
		public Uri? TempTcpFolder { get; set; } = null;
		#endregion

		#region private
		private static readonly CombinationOfLocations[] NoTCPUnary
			= GenerateUnaryLoactions(stackalloc StorageLocation[2].SetValue(CpuAlone, FileAlone));
		private static readonly CombinationOfLocations[] WithTCPUnary
			= GenerateUnaryLoactions(stackalloc StorageLocation[3].SetValue(CpuAlone, FileAlone, TcpAlone));

		private static readonly ImmutableTwoElementSet<CombinationOfLocations>[] NoTCPBinary
			= GenerateBinaryLoactions(stackalloc StorageLocation[2].SetValue(CpuAlone, FileAlone));
		private static readonly ImmutableTwoElementSet<CombinationOfLocations>[] WithTCBinary
			= GenerateBinaryLoactions(stackalloc StorageLocation[3].SetValue(CpuAlone, FileAlone, TcpAlone));
		#endregion

		#region support
		protected override bool IsSupportedUnary(CombinationOfLocations location) => this.TempTcpFolder is null ? NoTCPUnary.Contains(location) : WithTCPUnary.Contains(location);

		protected override bool IsSupportedBinary(CombinationOfLocations location1, CombinationOfLocations location2) => this.TempTcpFolder is null ? NoTCPBinary.Contains((location1, location2)) : WithTCBinary.Contains((location1, location2));

		protected override bool CanTransferWithManaged(CombinationOfLocations location) => this.IsSupportedUnary(location);
		#endregion

		#region properties
		public override bool IsSupportedLocation(StorageLocation location) => location == CpuAlone || location == FileAlone || (this.TempTcpFolder is not null && location == TcpAlone);

		public override (int major, int minor) DriverVersion(StorageLocation location) => default;

		// since this is not implemented yet (see https://github.com/dotnet/runtime/issues/22948), this is a manual implementation
		public override (long free, long total) FreeAndTotalMemory(StorageLocation location)
		{
			var memoryInfo = GC.GetGCMemoryInfo();
			long total = memoryInfo.TotalAvailableMemoryBytes;
			long free = total - Environment.WorkingSet;
			return (free, total);
		}
		#endregion

		#region low-level storage operations
		protected override bool Allocate_(StorageLocation location, long length, out PointerSegment result)
		{
			if (!this.IsSupportedLocation(location))
			{
				result = default; return false;
			}

			IPointer pointer; // box struct to interface
			if (location == CpuAlone)
			{
				var ptr = Marshal.AllocHGlobal(checked((int)length));
				pointer = new MemoryPointer(ptr, length, location);
			}
			else
			{
				if (location == FileAlone)
				{
					string file = Path.Combine(this.TempFileFolder, Guid.NewGuid().ToString());
					Uri uri = new Uri(Uri.UriSchemeFile + Uri.SchemeDelimiter + file);
					pointer = new StreamPointer(new Backend.Storage.FileStream(uri, length), location);
				}
				else if (this.TempTcpFolder is not null) // TCP
				{
					var builder = new UriBuilder(this.TempTcpFolder);
					builder.Path = Path.Combine(builder.Path, Guid.NewGuid().ToString());
					pointer = new StreamPointer(new TcpStream(builder.Uri, length, Backend.Storage.Tcp.DefaultTcpMethods.DefaultTcpProtocol), location);
				}
				else
				{
					result = default; return false;
				}
			}
			result = new PointerSegment(pointer); return true;
		}

		protected override bool Free_(PointerSegment pointer, bool disposeManaged, out bool valid)
		{
			var offset = pointer.GetPointerOffset(out IMemoryPointer? mp, out IStreamPointer? sp, @throw: false);
			if (offset == INVALID)
			{
				valid = false; return true;
			}
			if (offset == NOT_SUPPORT)
			{
				valid = true; return false;
			}

			if (mp is not null)
			{
				Marshal.FreeHGlobal(mp.Pointer);
			}
			else if (sp is not null)
			{
				sp.Dispose();
			}
			valid = true; return true;
		}

		protected unsafe override bool FillWithValue_(PointerSegment pointer, byte value)
		{
			var offset = pointer.GetPointerOffset(out IMemoryPointer? mp, out IStreamPointer? sp);
			if (offset == NOT_SUPPORT)
				return false;

			if (mp is not null)
			{
				Unsafe.InitBlock(mp.NativePointer(offset), value, checked((uint)pointer.LengthInBytes));
			}
			else if (sp is not null)
			{
				sp.NativeStream.Position = offset;
				sp.NativeStream.SetValues(value, pointer.LengthInBytes);
			}
			return true;
		}

		protected override bool FillWithValue_<T>(PointerSegment pointer, T value)
		{
			var offset = pointer.GetPointerOffset<T>(out IMemoryPointer? mp, out IStreamPointer? sp);
			if (offset == NOT_SUPPORT)
				return false;

			if (mp is not null)
			{
				mp.AsSpan<T>(pointer).Fill(value);
			}
			else if (sp is not null)
			{
				sp.NativeStream.Position = offset;
				sp.NativeStream.SetValues<T>(value, pointer.LengthInBytes / Storage<T>.SizeOfT);
			}
			return true;
		}

		protected override bool MemoryCopy_(PointerSegment source, PointerSegment destination, out long copied)
		{
			long srcOff = source.GetPointerOffset(out IMemoryPointer? srcMP, out IStreamPointer? srcSP);
			long dstOff = destination.GetPointerOffset(out IMemoryPointer? dstMP, out IStreamPointer? dstSP);
			if (srcOff == NOT_SUPPORT || dstOff == NOT_SUPPORT)
			{
				copied = 0; return false;
			}

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
				dstSP.NativeStream.Position = dstOff;
				dstSP.NativeStream.FromMemory(source);
			}
			else if (srcSP is not null && dstMP is not null)
			{
				srcSP.NativeStream.Position = srcOff;
				srcSP.NativeStream.ToMemory(destination);
			}
			else if (srcSP is not null && dstSP is not null)
			{
				srcSP.NativeStream.Position = srcOff;
				dstSP.NativeStream.Position = dstOff;
				srcSP.NativeStream.CopyTo(dstSP.NativeStream, source.LengthInBytes);
			}
			copied = copyLength; return true;
		}

		protected override bool MemoryCopy2D_(PointerSegment source, long sourceLD, PointerSegment destination, long destinationLD, long height, long width)
		{
			if (sourceLD == 0)
				throw new ArgumentOutOfRangeException(nameof(sourceLD), sourceLD, Parameter.MustPositive);
			if (destinationLD == 0)
				throw new ArgumentOutOfRangeException(nameof(destinationLD), destinationLD, Parameter.MustPositive);
			if (width == 0)
				throw new ArgumentOutOfRangeException(nameof(width), width, Parameter.MustPositive);
			if (height == 0)
				throw new ArgumentOutOfRangeException(nameof(height), height, Parameter.MustPositive);
			if (height > sourceLD || height > destinationLD)
				throw new ArgumentException(Parameter.InvalidValue, nameof(height));
			if (sourceLD * width > source.LengthInBytes)
				throw new ArgumentException(Parameter.WrongSize, nameof(source));
			if (destinationLD * width > destination.LengthInBytes)
				throw new ArgumentException(Parameter.WrongSize, nameof(destination));
			// shortcut
			if (sourceLD == destinationLD && sourceLD == height)
			{
				return this.MemoryCopy_(source.AsLength(height * width), destination.AsLength(height * width), out _);
			}
			// normal cases
			long srcOff = source.GetPointerOffset(out IMemoryPointer? srcMP, out IStreamPointer? srcSP);
			long dstOff = destination.GetPointerOffset(out IMemoryPointer? dstMP, out IStreamPointer? dstSP);
			if (srcOff == NOT_SUPPORT || dstOff == NOT_SUPPORT)
				return false;

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
				return true;
			}
			long end = source.OffsetInBytes + sourceLD * width;
			long srcLD = sourceLD, dstLD = destinationLD;
			int h = checked((int)height);
			if (srcMP is not null && dstSP is not null)
			{
				for (; srcOff < end; srcOff += srcLD, dstOff += dstLD)
				{
					dstSP.NativeStream.Position = dstOff;
					dstSP.NativeStream.FromManged<byte>(srcMP.AsSpan<byte>(srcOff, h));
				}
			}
			else if (srcSP is not null && dstMP is not null)
			{
				for (; srcOff < end; srcOff += srcLD, dstOff += dstLD)
				{
					srcSP.NativeStream.Position = srcOff;
					srcSP.NativeStream.ToManged(dstMP.AsSpan<byte>(dstOff, h));
				}
			}
			else if (srcSP is not null && dstSP is not null)
			{
				for (; srcOff < end; srcOff += srcLD, dstOff += dstLD)
				{
					srcSP.NativeStream.Position = srcOff;
					dstSP.NativeStream.Position = dstOff;
					srcSP.NativeStream.CopyTo(dstSP.NativeStream, height);
				}
			}
			return true;
		}

		protected override bool StridedCopy_<T>(PointerSegment source, int incrementSource, PointerSegment destination, int incrementDestination, out long copied)
		{
			long srcLen = source.LengthInBytes / Storage<T>.SizeOfT, dstLen = destination.LengthInBytes / Storage<T>.SizeOfT;
			if (incrementSource <= 0 || incrementSource >= srcLen)
				throw new ArgumentOutOfRangeException(nameof(incrementSource), incrementSource, Parameter.InvalidValue);
			if (incrementDestination <= 0 || incrementDestination >= dstLen)
				throw new ArgumentOutOfRangeException(nameof(incrementDestination), incrementDestination, Parameter.InvalidValue);

			// shortcut
			if (incrementSource == 1 && incrementDestination == 1)
			{
				return this.MemoryCopy_(source, destination, out copied);
			}
			// other cases
			long copyLength = Math.Min((srcLen - 1) / incrementSource + 1, (dstLen - 1) / incrementDestination + 1);
			long srcOff = source.GetPointerOffset(out IMemoryPointer? srcMP, out IStreamPointer? srcSP);
			long dstOff = destination.GetPointerOffset(out IMemoryPointer? dstMP, out IStreamPointer? dstSP);
			if (srcOff == NOT_SUPPORT || dstOff == NOT_SUPPORT)
			{
				copied = 0; return false;
			}

			if (srcMP is not null && dstMP is not null)
			{
				Span<T> srcSpan = srcMP.AsSpan<T>(source), dstSpan = dstMP.AsSpan<T>(destination);
				for (int i = 0; i < copyLength; i++)
				{
					dstSpan[i * incrementDestination] = srcSpan[i * incrementSource];
				}
			}
			else if (srcMP is not null && dstSP is not null)
			{
				Span<T> srcSpan = srcMP.AsSpan<T>(source);
				Span<T> temp = stackalloc T[1];
				for (int i = 0; i < copyLength; i++)
				{
					dstSP.NativeStream.Position = dstOff;
					temp[0] = srcSpan[i * incrementSource];
					dstSP.NativeStream.FromManged<T>(temp);
					dstOff += incrementDestination;
				}
			}
			else if (srcSP is not null && dstMP is not null)
			{
				Span<T> dstSpan = dstMP.AsSpan<T>(destination);
				Span<T> temp = stackalloc T[1];
				for (int i = 0; i < copyLength; i++)
				{
					srcSP.NativeStream.Position = srcOff;
					srcSP.NativeStream.ToManged(temp);
					dstSpan[i * incrementDestination] = temp[0];
					srcOff += incrementSource;
				}
			}
			else if (srcSP is not null && dstSP is not null)
			{
				Span<T> temp = stackalloc T[1];
				for (int i = 0; i < copyLength; i++)
				{
					srcSP.NativeStream.Position = srcOff;
					dstSP.NativeStream.Position = dstOff;
					srcSP.NativeStream.ToManged(temp);
					dstSP.NativeStream.FromManged<T>(temp);
					srcOff += incrementSource;
					dstOff += incrementDestination;
				}
			}
			copied = copyLength; return true;
		}
		#endregion

		#region low-level storage and manged operations
		protected override bool ToManaged_<T>(PointerSegment source, out T result)
		{
			result = default;
			long offset = source.GetPointerOffset<T>(out IMemoryPointer? mp, out IStreamPointer? sp);
			if (offset == NOT_SUPPORT)
				return false;

			if (mp is not null)
			{
				unsafe
				{
					result = Unsafe.Read<T>(mp.UnmangedPointer<T>(offset));
				}
			}
			else if (sp is not null)
			{
				Span<T> span = stackalloc T[1];
				sp.NativeStream.Position = offset;
				sp.NativeStream.ToManged(span);
				result = span[0];
			}
			return true;
		}

		protected override bool FromManaged_<T>(PointerSegment destination, T value)
		{
			long offset = destination.GetPointerOffset<T>(out IMemoryPointer? mp, out IStreamPointer? sp);
			if (offset == NOT_SUPPORT)
				return false;

			if (mp is not null)
			{
				unsafe { Unsafe.Write(mp.UnmangedPointer<T>(offset), value); }
			}
			else if (sp is not null)
			{
				Span<T> span = stackalloc T[1];
				span[0] = value;
				sp.NativeStream.Position = offset;
				sp.NativeStream.FromManged<T>(span);
			}
			return true;
		}

		private static long ToManaged<T>(IMemoryPointer? mp, IStreamPointer? sp, long offsetSrc, Span<T> destination, int offsetDst, int copyLength) where T : unmanaged
		{
			var managedSpan = destination.Slice(offsetDst, copyLength);
			if (mp is not null)
			{
				mp.AsSpan<T>(offsetSrc, copyLength).CopyTo(managedSpan);
			}
			else if (sp is not null)
			{
				sp.NativeStream.Position = offsetSrc;
				sp.NativeStream.ToManged(managedSpan);
			}
			return copyLength;
		}

		private static long FromManaged<T>(IMemoryPointer? mp, IStreamPointer? sp, long offsetDst, ReadOnlySpan<T> source, int offsetSrc, int copyLength) where T : unmanaged
		{
			var managedSpan = source.Slice(offsetSrc, copyLength);
			if (mp is not null)
			{
				managedSpan.CopyTo(mp.AsSpan<T>(offsetDst, copyLength));
			}
			else if (sp is not null)
			{
				sp.NativeStream.Position = offsetSrc;
				sp.NativeStream.FromManged(managedSpan);
			}
			return copyLength;
		}

		protected override bool ToManaged_<T>(PointerSegment source, Span<T> destination, out long copied)
		{
			long offset = source.GetPointerOffset<T>(out IMemoryPointer? mp, out IStreamPointer? sp);
			if (offset == NOT_SUPPORT)
			{
				copied = 0; return false;
			}

			int copyLength = checked((int)Math.Min(source.LengthInBytes / Storage<T>.SizeOfT, destination.Length));
			copied = ToManaged(mp, sp, offset, destination, 0, copyLength);
			return true;
		}

		protected override bool FromManaged_<T>(PointerSegment destination, ReadOnlySpan<T> values, out long copied)
		{
			long offset = destination.GetPointerOffset<T>(out IMemoryPointer? mp, out IStreamPointer? sp);
			if (offset == NOT_SUPPORT)
			{
				copied = 0; return false;
			}

			int copyLength = checked((int)Math.Min(destination.LengthInBytes / Storage<T>.SizeOfT, values.Length));
			copied = FromManaged(mp, sp, offset, values, 0, copyLength);
			return true;
		}

		protected override bool ToManaged2D_<T>(PointerSegment source, long leadDim, long height, long width, Span<T> destination, long destinationLeadDim = 0)
		{
			if (leadDim == 0)
				throw new ArgumentOutOfRangeException(nameof(leadDim), leadDim, Parameter.MustPositive);
			if (width == 0)
				throw new ArgumentOutOfRangeException(nameof(width), width, Parameter.MustPositive);
			if (height == 0)
				throw new ArgumentOutOfRangeException(nameof(height), height, Parameter.MustPositive);
			if (height > leadDim || height > destinationLeadDim)
				throw new ArgumentException(Parameter.InvalidValue, nameof(height));

			long start = source.GetPointerOffset<T>(out IMemoryPointer? mp, out IStreamPointer? sp);
			if (start == NOT_SUPPORT)
				return false;

			if (leadDim * width > (source.Pointer.LengthInBytes - source.OffsetInBytes) / Storage<T>.SizeOfT)
				throw new ArgumentException(Parameter.WrongSize, nameof(source));
			if (leadDim * width > destination.Length)
				throw new ArgumentException(Parameter.WrongSize, nameof(destination));
			if (destinationLeadDim == 0)
				destinationLeadDim = height;
			// shortcut
			if (leadDim == height && destinationLeadDim == height)
			{
				ToManaged(mp, sp, start, destination, 0, checked((int)(height * width)));
				return true;
			}
			// normal cases
			int h = checked((int)height), dstLD = checked((int)destinationLeadDim);
			long srcLD = leadDim, max = leadDim * width + start;
			int dstOffset = 0;
			for (long srcOffset = start; srcOffset < max; srcOffset += srcLD, dstOffset += dstLD)
			{
				ToManaged(mp, sp, srcOffset, destination, dstOffset, h);
			}
			return true;
		}

		protected override bool FromManaged2D_<T>(PointerSegment destination, long leadDim, long height, long width, ReadOnlySpan<T> values, long valuesLeadDim)
		{
			if (leadDim == 0)
				throw new ArgumentOutOfRangeException(nameof(leadDim), leadDim, Parameter.MustPositive);
			if (width == 0)
				throw new ArgumentOutOfRangeException(nameof(width), width, Parameter.MustPositive);
			if (height == 0)
				throw new ArgumentOutOfRangeException(nameof(height), height, Parameter.MustPositive);
			if (height > leadDim || height > valuesLeadDim)
				throw new ArgumentException(Parameter.InvalidValue, nameof(height));

			long start = destination.GetPointerOffset<T>(out IMemoryPointer? mp, out IStreamPointer? sp);
			if (start == NOT_SUPPORT)
				return false;

			if (leadDim * width > (destination.Pointer.LengthInBytes - destination.OffsetInBytes) / Storage<T>.SizeOfT)
				throw new ArgumentException(Parameter.WrongSize, nameof(destination));
			if (leadDim * width > values.Length)
				throw new ArgumentException(Parameter.WrongSize, nameof(values));
			if (valuesLeadDim == 0)
				valuesLeadDim = height;
			// shortcut
			if (leadDim == height && valuesLeadDim == height)
			{
				FromManaged(mp, sp, start, values, 0, checked((int)(height * width)));
				return true;
			}
			// normal case
			int h = checked((int)height), srcLD = checked((int)valuesLeadDim);
			long dstLD = leadDim, max = leadDim * width + start;
			int srcOffset = 0;
			for (long dstOffset = start; dstOffset < max; dstOffset += dstLD, srcOffset += srcLD)
			{
				FromManaged(mp, sp, dstOffset, values, srcOffset, h);
			}
			return true;
		}
		#endregion
	}
}
#pragma warning restore CS1591 // 缺少对公共可见类型或成员的 XML 注释