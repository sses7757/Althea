using System;

using Althea.Storage;


namespace Althea.Backend.Cuda.Storage
{
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释
	/// <summary>
	/// The CUDA back-end of the <see cref="AbstractApi"/> that supports data transfer between GPU, CPU and managed memories. May support GPUDirect® Storage that directly transfer data between files and GPU if the corresponding ABI is found.
	/// </summary>
	public class StorageApi : AbstractApi
	{
		#region basic
		public StorageApi()
		{
			// TODO: cuFILE
		}

		protected override void Dispose(bool disposeManaged)
		{
			// TODO: cuFILE
		}
		#endregion

		#region driver info
		public override (int major, int minor) DriverVersion(StorageLocation location) => throw new NotImplementedException();

		public override (long free, long total) FreeAndTotalMemory(StorageLocation location) => throw new NotImplementedException();
		#endregion

		#region support
		public override bool IsSupportedLocation(StorageLocation location) => throw new NotImplementedException();

		protected override bool IsSupportedBinary(CombinationOfLocations location1, CombinationOfLocations location2) => throw new NotImplementedException();

		protected override bool IsSupportedUnary(CombinationOfLocations location) => throw new NotImplementedException();

		protected override bool CanTransferWithManaged(CombinationOfLocations location) => throw new NotImplementedException();
		#endregion

		#region allocate free
		protected override bool Allocate_(StorageLocation location, long length, out PointerSegment result) => throw new NotImplementedException();

		protected override bool Free_(PointerSegment pointer, out bool valid) => throw new NotImplementedException();

		protected override PointerSegment AllocateFileAt(string path, long lengthInBytes) => throw new NotImplementedException();
		#endregion

		#region fill
		protected override bool FillWithValue_(PointerSegment pointer, byte value) => throw new NotImplementedException();

		protected override bool FillWithValue_<T>(PointerSegment pointer, T value) => throw new NotImplementedException();
		#endregion

		#region copy
		protected override bool MemoryCopy_(PointerSegment source, PointerSegment destination, out long actualCopied) => throw new NotImplementedException();

		protected override bool MemoryCopy2D_(PointerSegment source, long sourceLD, PointerSegment destination, long destinationLD, long height, long width) => throw new NotImplementedException();

		protected override bool FromManaged2D_<T>(PointerSegment destination, long leadDim, long height, long width, ReadOnlySpan<T> values, long valuesLeadDim = 0) => throw new NotImplementedException();
		
		protected override bool FromManaged_<T>(PointerSegment destination, T value) => throw new NotImplementedException();
		
		protected override bool FromManaged_<T>(PointerSegment destination, ReadOnlySpan<T> values, out long actualCopied) => throw new NotImplementedException();
		
		protected override bool ToManaged2D_<T>(PointerSegment source, long leadDim, long height, long width, Span<T> destination, long destinationLeadDim = 0) => throw new NotImplementedException();
		
		protected override bool ToManaged_<T>(PointerSegment source, out T value) => throw new NotImplementedException();
		
		protected override bool ToManaged_<T>(PointerSegment source, Span<T> destination, out long actualCopied) => throw new NotImplementedException();
		#endregion

		#region strided copy
		protected override bool StridedCopy_<T>(PointerSegment source, int incrementSource, PointerSegment destination, int incrementDestination, out long actualCopied) => throw new NotImplementedException();
		#endregion
	}
#pragma warning restore CS1591 // 缺少对公共可见类型或成员的 XML 注释
}
