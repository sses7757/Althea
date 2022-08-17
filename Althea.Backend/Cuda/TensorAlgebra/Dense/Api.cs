using System.Runtime.CompilerServices;

using Althea.Array;
using Althea.Backend.Cuda.Storage;
using Althea.Helpers;
using Althea.LinearAlgebra;
using Althea.Linq;
using Althea.TensorAlgebra;
using Althea.TensorAlgebra.Dense;

using static Althea.Backend.Cuda.MemoryPointerChecker;

using NM = Althea.Backend.Cuda.TensorAlgebra.Dense.NativeMethods;


namespace Althea.Backend.Cuda.TensorAlgebra.Dense;

/// <summary>
/// The CUDA back-end of the dense tensor algebra <see cref="IBaseAbstractApi"/> that utilizes cuTENSOR with 1.0 ≤ version
/// </summary>
public unsafe class Api : IBindedDevice, IBaseAbstractApi
{
	// TODO: limited-sized cache
	#region basic
	internal readonly CudaTensorHandle handle;

	/// <summary>
	/// Default constructor
	/// </summary>
	public Api()
	{
		NM.cutensorInit(out this.handle).Check();
		this.ContractAlgorithm = ContractionAlgorithm.Default;
		this.BindedDeviceID = Runtime.CurrentDeviceID;
	}

	/// <inheritdoc/>
	public int BindedDeviceID { get; }

	/// <inheritdoc/>
	public bool Disposed { get; protected set; }

	/// <inheritdoc/>
	public virtual void Dispose()
	{
		lock (this)
		{
			this.contractCache.Clear();
		}
		this.Disposed = true;
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Get or set the algorithm used in the contractions
	/// </summary>
	public ContractionAlgorithm ContractAlgorithm {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get {
			if (this._algorithmFind.Invalid)
			{
				this._algorithmFind = new(this.handle, ContractionAlgorithm.Default);
			}
			return this._algorithmFind.Algorithm == ContractionAlgorithm.GETT ? (ContractionAlgorithm)this._algorithmFind.GETTAlgorithm : this._algorithmFind.Algorithm;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set {
			ContractFind.TryCreate(this.handle, value, out this._algorithmFind);
		}
	}

	private ContractFind _algorithmFind = default;
	#endregion

	#region contract cache
	internal readonly struct DenseTensorDescription : IEquatable<DenseTensorDescription>
	{
		private readonly FixedBuffer_60<int> size;
		private readonly int rank;
		private readonly FixedBuffer_60<int> outerSize;
		private readonly CuTensorUnary op;


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private DenseTensorDescription(ReadOnlySpan<int> size, ReadOnlySpan<int> outerSize, CuTensorUnary op)
		{
			this.rank = size.Length;
			this.op = op;
			this.size = this.outerSize = default;
			this.size.CopyFromSpan(size);
			this.outerSize.CopyFromSpan(outerSize);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static DenseTensorDescription Create<T, TS>(DenseTensorWrapper<T, TS> tensor) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
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
		public override bool Equals(object? obj) => obj is DenseTensorDescription find && this.Equals(find);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe override int GetHashCode() => HashCode.Combine(this.rank, this.op, this.size.AsSpan(this.rank).HashCodeOfSpan(), this.outerSize.AsSpan(this.rank).HashCodeOfSpan());

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(DenseTensorDescription a, DenseTensorDescription b) => a.Equals(b);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(DenseTensorDescription a, DenseTensorDescription b) => !a.Equals(b);
	}

	private readonly struct ContractionInfo : IEquatable<ContractionInfo>
	{
		private readonly DenseTensorDescription A, B, C;

		private readonly StorableContractInfo info;

		private readonly ContractionAlgorithm algorithm;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private ContractionInfo(in DenseTensorDescription A, in DenseTensorDescription B, in DenseTensorDescription C, in StorableContractInfo info, ContractionAlgorithm algorithm)
		{
			this.A = A; this.B = B; this.C = C; this.info = info; this.algorithm = algorithm;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ContractionInfo Create<T, TS1, TS2, TS3>(DenseTensorWrapper<T, TS1> A, DenseTensorWrapper<T, TS2> B, DenseTensorWrapper<T, TS3> C, TensorContractInfo info, ContractionAlgorithm algorithm) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>
		{
			var dA = DenseTensorDescription.Create(A);
			var dB = DenseTensorDescription.Create(B);
			var dC = DenseTensorDescription.Create(C);
			StorableContractInfo dInfo = info;
			return new(dA, dB, dC, dInfo, algorithm);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe bool Equals(ContractionInfo other) => this.A == other.A && this.B == other.B && this.C == other.C && this.info == other.info && this.algorithm == other.algorithm;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override bool Equals(object? obj) => obj is ContractionInfo find && this.Equals(find);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe override int GetHashCode() => HashCode.Combine(this.A, this.B, this.C, this.info, this.algorithm);
	}

	private readonly Dictionary<ContractionInfo, (ContractPlan plan, long workspace)> contractCache = new();
	#endregion

	#region methods
	/// <inheritdoc/>
	public virtual bool Permute<T, TS1, TS2>(DenseTensorWrapper<T, TS1> source, DenseArrayWrapper<T, TS2> destination, ReadOnlySpan<int> permutationOrder) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
	{
		if (!GetPointer(this, source.ValueStorage, source.Size, source.OuterSize, out T* pA))
			return false;
		if (!GetPointer(this, destination.ValueStorage, destination.Size, destination.OuterSize, out T* pB))
			return false;
		if (!TensorDescription.TryCreate(this.handle, source, out var descrA))
			return false;
		if (!TensorDescription.TryCreate<T, TS2>(this.handle, destination, out var descrB))
			return false;
		int r = source.Rank;
		Span<int> modeA = stackalloc int[r].FillWithRange(1);
		Span<int> modeB = stackalloc int[r];
		modeA.ReOrderTo(modeB, permutationOrder);
		T alpha = source.Scalar;
		return NM.cutensorPermutation(this.handle, &alpha, pA, &descrA, modeA, pB, &descrB, modeB, descrA.dataType, null).Check();
	}

	/// <inheritdoc/>
	public virtual bool OperationBinary<T, TS1, TS2, TS3>(BinaryOperation binary, DenseTensorWrapper<T, TS1> left, ReadOnlySpan<int> leftPerm, DenseTensorWrapper<T, TS2> right, ReadOnlySpan<int> rightPerm, DenseArrayWrapper<T, TS3> destination) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>
	{
		CuTensorBinary opAB = binary.ToCudaOp();
		if (opAB == 0)
			return false;
		if (!GetPointer(this, left.ValueStorage, left.Size, left.OuterSize, out T* pA))
			return false;
		if (!GetPointer(this, right.ValueStorage, right.Size, right.OuterSize, out T* pB))
			return false;
		if (!GetPointer(this, destination.ValueStorage, destination.Size, destination.OuterSize, out T* pC))
			return false;
		if (!TensorDescription.TryCreate(this.handle, left, out var descrA))
			return false;
		if (!TensorDescription.TryCreate(this.handle, right, out var descrB))
			return false;
		if (!TensorDescription.TryCreate<T, TS3>(this.handle, destination, out var descrC))
			return false;
		int r = destination.Rank;
		Span<int> modeC = stackalloc int[r].FillWithRange(1);
		Span<int> modeA = stackalloc int[r], modeB = stackalloc int[r];
		if (leftPerm.Length == r)
			modeC.InverseOrderTo(modeA, leftPerm);
		if (rightPerm.Length == r)
			modeC.InverseOrderTo(modeB, rightPerm);
		T alpha = left.Scalar, beta = right.Scalar;
		return NM.cutensorElementwiseBinary(this.handle, &alpha, pA, &descrA, modeA, &beta, pB, &descrB, modeB, pC, &descrC, modeC, opAB, descrA.dataType, null).Check();
	}

	/// <inheritdoc/>
	public virtual bool Reduce<T, TS1, TS2>(ReduceOperation reduce, DenseTensorWrapper<T, TS1> source, DenseTensorWrapper<T, TS2> destination, ReadOnlySpan<int> reduceDimensions) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
	{
		CuTensorBinary opRed = reduce.ToCudaOp();
		if (opRed == 0)
			return false;
		if (!GetPointer(this, source.ValueStorage, source.Size, source.OuterSize, out T* pA))
			return false;
		if (!GetPointer(this, destination.ValueStorage, destination.Size, destination.OuterSize, out T* pB))
			return false;
		if (!TensorDescription.TryCreate(this.handle, source, out var descrA))
			return false;
		if (!TensorDescription.TryCreate(this.handle, destination, out var descrB))
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
		if (!NM.cutensorReductionGetWorkspace(this.handle, pA, &descrA, modeA, pB, &descrB, modeB, pB, &descrB, modeB, opRed, computeType, out long workspace).Check())
			return false;
		T alpha = source.Scalar, beta = destination.Scalar;
		using var buffer = CudaBuffer.Create(workspace, extraDeviceInfo: false);
		return NM.cutensorReduction(this.handle, &alpha, pA, &descrA, modeA, &beta, pB, &descrB, modeB, pB, &descrB, modeB, opRed, computeType, buffer.DeviceBuffer, workspace, default).Check();
	}

	/// <inheritdoc/>
	public virtual bool Contract<T, TS1, TS2, TS3>(DenseTensorWrapper<T, TS1> left, DenseTensorWrapper<T, TS2> right, DenseTensorWrapper<T, TS3> destination, TensorContractInfo info) where T : unmanaged, IBaseNumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>
	{
		if (left.Scalar == T.Zero || right.Scalar == T.Zero)
			return this.Permute(destination, (DenseArrayWrapper<T, TS3>)destination, stackalloc int[destination.Rank].FillWithRange(0));
		if (!GetPointer(this, left.ValueStorage, left.Size, left.OuterSize, out T* pA))
			return false;
		if (!GetPointer(this, right.ValueStorage, right.Size, right.OuterSize, out T* pB))
			return false;
		if (!GetPointer(this, destination.ValueStorage, destination.Size, destination.OuterSize, out T* pC))
			return false;
		var key = ContractionInfo.Create(left, right, destination, info, this.ContractAlgorithm);
		if (!contractCache.ContainsKey(key))
		{
			if (!ContractDescription.TryCreate(this, left, right, destination, destination, info, out var descr))
				return false;
			if (!ContractPlan.TryCreate(this.handle, &descr, in this._algorithmFind, out var plan0, out long workspace0))
				return false;
			contractCache.Add(key, (plan0, workspace0));
		}
		var (plan, workspace) = contractCache[key];
		using var buffer = CudaBuffer.Create(workspace);
		T alpha = left.Scalar * right.Scalar, beta = destination.Scalar;
		return NM.cutensorContraction(this.handle, in plan, &alpha, pA, pB, &beta, pC, pC, buffer, workspace, null).Check();
	}

	/*
	protected internal unsafe bool OperationTrinary<T>(CuTensorBinary binaryAB, CuTensorBinary binaryABC, DenseTensorWrapper<T> A, Span<int> permA, DenseTensorWrapper<T> B, Span<int> permB, DenseTensorWrapper<T> C, Span<int> permC, DenseTensorWrapper<T> destination, Span<int> permD) where T : unmanaged, IBaseNumber<T>
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
		NM.cutensorElementwiseTrinary(this.handle, &alpha, pA, &descrA, modeA, &beta, pB, &descrB, modeB, &gamma, pC, &descrC, modeC, pD, &descrD, modeD, binaryAB, binaryABC, descrA.dataType, default).Check();
		return true;
	}
	*/
	#endregion
}
