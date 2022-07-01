using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Althea.Backend.Storage;
using Althea.Helpers;
using Althea.Linq;
using Althea.Numerics;
using Althea.TensorAlgebra;
using Althea.TensorAlgebra.Dense;


#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释
namespace Althea.Backend.Cuda.TensorAlgebra.Dense
{
	/// <summary>
	/// The CUDA back-end of the dense tensor algebra <see cref="AbstractApi"/> that utilizes cuTENSOR with 1.0 ≤ version ≤ 1.2 (and maybe future versions)
	/// </summary>
	/// <remarks>Unlike the <see cref="LinearAlgebra.Dense.DenseApi"/> that binds a instance with a specific CUDA device, this class changes the underlying handle when the <see cref="CudaRuntime.CurrentDeviceID"/> is changed.<br/>
	/// CUDA stream is not supported, but it can be easily added.</remarks>
	public class DenseApi : AbstractApi
	{
		#region basic
		private static readonly CudaTensorHandle[] handles = new CudaTensorHandle[CudaRuntime.DeviceCount];

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

		/// <summary>
		/// Get or set the algorithm used in the contractions
		/// </summary>
		public ContractionAlgorithm ContractAlgorithm {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				if (this._algorithmFind.Equals(default))
				{
					this._algorithmFind = new(this.handle, ContractionAlgorithm.Default);
				}
				return this._algorithmFind.algorithm == ContractionAlgorithm.GETT ? (ContractionAlgorithm)this._algorithmFind.GETTSpecificAlgorithm : this._algorithmFind.algorithm;
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set {
				ContractFind.TryCreate(this.handle, value, out this._algorithmFind);
			}
		}

		private ContractFind _algorithmFind = default;
		#endregion

		#region support
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static IntPtr GetPointer<T>(Storage<T> s) where T : unmanaged, IBaseNumber<T>
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
		private static bool Supported(CombinationOfLocations location)
		{
			if (location.Count != 1)
				return false;
			var loc = location[0];
			return loc.Type == LocationType.GpuRam && loc.Detail == CudaRuntime.CurrentDeviceID;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedTensorBinary(CombinationOfLocations location1, CombinationOfLocations location2) => Supported(location1) && Supported(location2);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected override bool IsSupportedTensorTrinary(CombinationOfLocations location1, CombinationOfLocations location2, CombinationOfLocations location3) => Supported(location1) && Supported(location2) && Supported(location3);
		#endregion

		#region contract cache
		internal readonly struct DenseTensorDescription : IEquatable<DenseTensorDescription>
		{
			private readonly FixedBuffer_64<int> size, outerSize;

			private readonly UnaryOperation op;

			private readonly int rank;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private DenseTensorDescription(ReadOnlySpan<int> size, ReadOnlySpan<int> outerSize, UnaryOperation op)
			{
				this.rank = size.Length;
				this.op = op;
				this.size = this.outerSize = default;
				this.size.CopyFromSpan(size);
				this.outerSize.CopyFromSpan(outerSize);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static DenseTensorDescription Create<T>(DenseTensorWrapper<T> tensor) where T : unmanaged, IBaseNumber<T>
			{
				int r = tensor.Rank;
				Span<int> size = stackalloc int[r], outerSize = stackalloc int[r];
				ReadOnlySpan<long> sizeL = tensor.Size, outerSizeL = tensor.OuterSize;
				for (int i = 0; i < r; i++)
				{
					size[i] = checked((int)sizeL[i]);
					outerSize[i] = checked((int)outerSizeL[i]);
				}
				return new(size, outerSize, tensor.Operation.ToCudaOp());
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public unsafe bool Equals(DenseTensorDescription other)
			{
				return this.rank == other.rank && this.op == other.op &&
					this.size.AsSpan(this.rank).SequenceEqual(other.size.AsSpan(this.rank)) &&
					this.outerSize.AsSpan(this.rank).SequenceEqual(other.outerSize.AsSpan(this.rank));
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public override bool Equals(object? obj)
			{
				return obj is DenseTensorDescription find && this.Equals(find);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public unsafe override int GetHashCode()
			{
				return HashCode.Combine(this.rank, this.op, this.size.AsSpan(this.rank).HashCodeOfSpan(), this.outerSize.AsSpan(this.rank).HashCodeOfSpan());
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static bool operator ==(DenseTensorDescription a, DenseTensorDescription b)
			{
				return a.Equals(b);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static bool operator !=(DenseTensorDescription a, DenseTensorDescription b)
			{
				return !a.Equals(b);
			}
		}

		private readonly struct ContractionDescription : IEquatable<ContractionDescription>
		{
			private readonly DenseTensorDescription A, B, C;

			private readonly StorableContractInfo info;

			private readonly ContractionAlgorithm algorithm;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private ContractionDescription(in DenseTensorDescription A, in DenseTensorDescription B, in DenseTensorDescription C, in StorableContractInfo info, ContractionAlgorithm algorithm)
			{
				this.A = A; this.B = B; this.C = C; this.info = info; this.algorithm = algorithm;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static ContractionDescription Create<T>(DenseTensorWrapper<T> A, DenseTensorWrapper<T> B, DenseTensorWrapper<T> C, TensorContractInfo info, ContractionAlgorithm algorithm) where T : unmanaged, IBaseNumber<T>
			{
				var dA = DenseTensorDescription.Create(A);
				var dB = DenseTensorDescription.Create(B);
				var dC = DenseTensorDescription.Create(C);
				StorableContractInfo dInfo = info;
				return new(dA, dB, dC, dInfo, algorithm);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public unsafe bool Equals(ContractionDescription other)
			{
				return this.A == other.A && this.B == other.B && this.C == other.C && this.info == other.info && this.algorithm == other.algorithm;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public override bool Equals(object? obj)
			{
				return obj is ContractionDescription find && this.Equals(find);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public unsafe override int GetHashCode()
			{
				return HashCode.Combine(this.A, this.B, this.C, this.info, this.algorithm);
			}
		}

		private static readonly Dictionary<ContractionDescription, (ContractPlan plan, long workspace)> _cacheConc = new();
		#endregion

		#region methods
		protected override unsafe bool Contract_<T>(DenseTensorWrapper<T> left, DenseTensorWrapper<T> right, DenseTensorWrapper<T> destination, TensorContractInfo info)
		{
			IntPtr pA = GetPointer(left.ValueStorage), pB = GetPointer(right.ValueStorage), pC = GetPointer(destination.ValueStorage);
			if (pA == default || pB == default || pC == default)
				return false;
			if (left.Scalar.IsZero() || right.Scalar.IsZero()) // fill with 0
				return this.Permute_(new DenseTensorWrapper<T>(destination), destination, stackalloc int[destination.Rank].FillWithRange(0));

			var key = ContractionDescription.Create(left, right, destination, info, this.ContractAlgorithm);
			if (!_cacheConc.ContainsKey(key))
			{
				if (!ContractDescription.Create(this.handle, left, right, destination, destination, info, out var descr))
					return false;
				if (!ContractPlan.Create(this.handle, in descr, in this._algorithmFind, out var plan0, out long workspace0))
					return false;
				_cacheConc.Add(key, (plan0, workspace0));
			}
			var (plan, workspace) = _cacheConc[key];
			using var buffer = CudaBuffer.Create(workspace, extraDeviceInfo: false);
			T alpha = left.Scalar.NativeMultiply(right.Scalar), beta = destination.Scalar;
			NativeMethods.cutensorContraction(this.handle, in plan, &alpha, pA, pB, &beta, pC, pC, buffer.DeviceBuffer, workspace, default).Check();
			return true;
		}

		protected override unsafe bool OperationBinary_<T>(Althea.TensorAlgebra.BinaryOperation binary, DenseTensorWrapper<T> left, Span<int> leftPerm, DenseTensorWrapper<T> right, Span<int> rightPerm, DenseTensorWrapper<T> destination)
		{
			BinaryOperation opAB = binary.ToCudaOp();
			if (opAB == 0)
				return false;
			IntPtr pA = GetPointer(left.ValueStorage), pB = GetPointer(right.ValueStorage), pC = GetPointer(destination.ValueStorage);
			if ((pA == default && pB == default) || pC == default)
				return false;
			if (!TensorDescription.Create(this.handle, destination, out var descrC))
				return false;
			TensorDescription.Create(this.handle, left, out var desrA);
			TensorDescription.Create(this.handle, right, out var descrB);
			int r = destination.Rank;
			Span<int> modeC = stackalloc int[r].FillWithRange(1);
			Span<int> modeA = stackalloc int[r], modeB = stackalloc int[r];
			if (leftPerm.Length == r)
				modeC.InverseOrderTo(modeA, leftPerm);
			if (rightPerm.Length == r)
				modeC.InverseOrderTo(modeB, rightPerm);
			T alpha = left.Scalar, beta = right.Scalar;
			NativeMethods.cutensorElementwiseBinary(this.handle, &alpha, pA, in desrA, in modeA[0], &beta, pB, in descrB, in modeB[0], pC, in descrC, in modeC[0], opAB, desrA.dataType, default).Check();
			return true;
		}

		protected internal unsafe bool OperationTrinary<T>(BinaryOperation binaryAB, BinaryOperation binaryABC, DenseTensorWrapper<T> A, Span<int> permA, DenseTensorWrapper<T> B, Span<int> permB, DenseTensorWrapper<T> C, Span<int> permC, DenseTensorWrapper<T> destination, Span<int> permD) where T : unmanaged, IBaseNumber<T>
		{
			IntPtr pA = GetPointer(A.ValueStorage), pB = GetPointer(B.ValueStorage), pC = GetPointer(C.ValueStorage), pD = GetPointer(destination.ValueStorage);
			if ((pA == default && pB == default && pC == default) || pC == default)
				return false;
			if (!TensorDescription.Create(this.handle, destination, out var descrD))
				return false;
			TensorDescription.Create(this.handle, A, out var descrA);
			TensorDescription.Create(this.handle, B, out var descrB);
			TensorDescription.Create(this.handle, C, out var descrC);
			int r = destination.Rank;
			Span<int> tempMode = stackalloc int[r].FillWithRange(1);
			Span<int> modeD = stackalloc int[r];
			tempMode.ReOrderTo(modeD, permD);
			Span<int> modeA = stackalloc int[r], modeB = stackalloc int[r], modeC = stackalloc int[r];
			if (permA.Length == r)
				modeD.InverseOrderTo(modeA, permA);
			if (permB.Length == r)
				modeD.InverseOrderTo(modeB, permB);
			if (permC.Length == r)
				modeD.InverseOrderTo(modeC, permC);
			T alpha = A.Scalar, beta = B.Scalar, gamma = C.Scalar;
			NativeMethods.cutensorElementwiseTrinary(this.handle, &alpha, pA, in descrA, in modeA[0], &beta, pB, in descrB, in modeB[0], &gamma, pC, in descrC, in modeC[0], pD, in descrD, in modeD[0], binaryAB, binaryABC, descrA.dataType, default).Check();
			return true;
		}

		protected override unsafe bool Permute_<T>(DenseTensorWrapper<T> source, DenseTensorWrapper<T> destination, ReadOnlySpan<int> permutationOrder)
		{
			IntPtr pA = GetPointer(source.ValueStorage), pB = GetPointer(destination.ValueStorage);
			if (pA == default || pB == default)
				return false;
			if (!TensorDescription.Create(this.handle, source, out var descrA))
				return false;
			if (!TensorDescription.Create(this.handle, destination, out var descrB))
				return false;
			int r = source.Rank;
			Span<int> modeA = stackalloc int[r].FillWithRange(1);
			Span<int> modeB = stackalloc int[r];
			modeA.ReOrderTo(modeB, permutationOrder);
			T alpha = source.Scalar;
			NativeMethods.cutensorPermutation(this.handle, &alpha, pA, in descrA, in modeA[0], pB, in descrB, in modeB[0], descrA.dataType, default).Check();
			return true;
		}

		protected override unsafe bool Reduce_<T>(Althea.TensorAlgebra.BinaryOperation reduce, DenseTensorWrapper<T> source, DenseTensorWrapper<T> destination, ReadOnlySpan<int> reduceDimensions)
		{
			BinaryOperation opRed = reduce.ToCudaOp();
			if (opRed == 0)
				return false;
			IntPtr pA = GetPointer(source.ValueStorage), pB = GetPointer(destination.ValueStorage);
			if (pA == default || pB == default)
				return false;
			if (!TensorDescription.Create(this.handle, source, out var descrA))
				return false;
			if (!TensorDescription.Create(this.handle, destination, out var descrB))
				return false;
			Span<int> modeA = stackalloc int[source.Rank].FillWithRange(1);
			Span<int> modeB = stackalloc int[destination.Rank];
			int c = 0;
			for (int i = 0; i < modeA.Length; i++)
			{
				if (!reduceDimensions.Contains(i))
					modeB[c++] = modeA[i];
			}
			var computeType = descrA.dataType.ToComputeType();
			NativeMethods.cutensorReductionGetWorkspace(this.handle, pA, in descrA, in modeA[0], pB, in descrB, in modeB[0], pB, in descrB, in modeB[0], opRed, computeType, out long workspace).Check();
			T alpha = source.Scalar, beta = destination.Scalar;
			using var buffer = CudaBuffer.Create(workspace, extraDeviceInfo: false);
			NativeMethods.cutensorReduction(this.handle, &alpha, pA, in descrA, in modeA[0], &beta, pB, in descrB, in modeB[0], pB, in descrB, in modeB[0], opRed, computeType, buffer.DeviceBuffer, workspace, default).Check();
			return true;
		}

		protected internal unsafe bool Reduce<T>(BinaryOperation reduce, DenseTensorWrapper<T> source, DenseTensorWrapper<T> destination, ReadOnlySpan<char> labelSource, ReadOnlySpan<char> labelDestination) where T : unmanaged, IBaseNumber<T>
		{
			IntPtr pA = GetPointer(source.ValueStorage), pB = GetPointer(destination.ValueStorage);
			if (pA == default || pB == default)
				return false;
			if (!TensorDescription.Create(this.handle, source, out var descrA))
				return false;
			if (!TensorDescription.Create(this.handle, destination, out var descrB))
				return false;
			Span<int> modeA = stackalloc int[source.Rank];
			Span<int> modeB = stackalloc int[destination.Rank];
			labelSource.CopyTo(modeA, static c => c);
			labelDestination.CopyTo(modeB, static c => c);
			var computeType = descrA.dataType.ToComputeType();
			NativeMethods.cutensorReductionGetWorkspace(this.handle, pA, in descrA, in modeA[0], pB, in descrB, in modeB[0], pB, in descrB, in modeB[0], reduce, computeType, out long workspace).Check();
			T alpha = source.Scalar, beta = Const<T>.Zero;
			using var buffer = CudaBuffer.Create(workspace, extraDeviceInfo: false);
			NativeMethods.cutensorReduction(this.handle, &alpha, pA, in descrA, in modeA[0], &beta, pB, in descrB, in modeB[0], pB, in descrB, in modeB[0], reduce, computeType, buffer.DeviceBuffer, workspace, default).Check();
			return true;
		}
		#endregion
	}
}
