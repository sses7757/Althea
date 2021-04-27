using System;
using System.Runtime.CompilerServices;

using Althea.Backend.Storage;
using Althea.TensorAlgebra;
using Althea.TensorAlgebra.Dense;


#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释
namespace Althea.Backend.Cuda.TensorAlgebra.Dense
{
	/// <summary>
	/// The CUDA back-end of the dense tensor algebra <see cref="AbstractApi"/> that utilizes cuTENSOR with 1.0 ≤ version ≤ 1.2 (and maybe future versions)
	/// </summary>
	/// <remarks>Unlike the <see cref="LinearAlgebra.Dense.DenseApi"/> that binds a instance with a specific CUDA device, this class changes the underlying handle when the <see cref="CudaRuntime.CurrentDeviceID"/> is changed.</remarks>
	public class DenseApi : AbstractApi
	{
		#region basic
		private static readonly CudaTensorHandle[] handles = new CudaTensorHandle[CudaRuntime.DeviceNumber];

		private CudaTensorHandle handle;

		public DenseApi()
		{
			int id = CudaRuntime.CurrentDeviceID;
			if (handles[id] is null)
			{
				handles[id] = new CudaTensorHandle();
			}
			handle = handles[id];
			CudaRuntime.OnDeviceChange += this.CudaRuntime_OnDeviceChange;
		}

		private void CudaRuntime_OnDeviceChange(int previousID, int currentID)
		{
			if (handles[currentID] is null)
			{
				handles[currentID] = new CudaTensorHandle();
			}
			this.handle = handles[currentID];
		}

		protected override void Dispose(bool disposeManaged)
		{
#pragma warning disable CS8625
			this.handle = default;
#pragma warning restore CS8625
		}
		#endregion

		#region support
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static IntPtr GetPointer<T>(Storage<T> s) where T : unmanaged
		{
			if (s is null || !s.IsValid() || s.Count != 1)
				return default;
			if (s[0].Pointer is not IMemoryPointer mp)
				return default;
			if (mp.Pointer == default)
				return default;
			return (IntPtr)(mp.Pointer.ToInt64() + s[0].OffsetInBytes);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool Supported(CombinationOfLocations location) => location.Count == 1 && location[0].Type == LocationType.GpuRam;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedTensorBinary(CombinationOfLocations location1, CombinationOfLocations location2) => Supported(location1) && Supported(location2);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedTensorTrinary(CombinationOfLocations location1, CombinationOfLocations location2, CombinationOfLocations location3) => Supported(location1) && Supported(location2) && Supported(location3);
		#endregion

		#region methods
		protected override bool Contract_<T>(DenseTensorWrapper<T> left, DenseTensorWrapper<T> right, DenseTensorWrapper<T> destination, TensorContractInfo info) => throw new NotImplementedException();

		protected override bool OperationBinary_<T>(Althea.TensorAlgebra.BinaryOperation binary, DenseTensorWrapper<T> left, Span<int> leftPerm, DenseTensorWrapper<T> right, Span<int> rightPerm, DenseTensorWrapper<T> destination) => throw new NotImplementedException();

		protected override bool Permute_<T>(DenseTensorWrapper<T> source, DenseTensorWrapper<T> destination, ReadOnlySpan<int> permutationOrder) => throw new NotImplementedException();

		protected override bool Reduce_<T>(Althea.TensorAlgebra.BinaryOperation reduce, DenseTensorWrapper<T> source, DenseTensorWrapper<T> destination, ReadOnlySpan<int> reduceDimensions) => throw new NotImplementedException();
		#endregion
	}
}
