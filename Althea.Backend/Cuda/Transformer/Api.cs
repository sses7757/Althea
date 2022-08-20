using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using Althea.Array;
using Althea.Backend.Cuda.Storage;
using Althea.Helpers;

using static Althea.Backend.Cuda.MemoryPointerChecker;

using NM = Althea.Backend.Cuda.Transformer.NativeMethods;


namespace Althea.Backend.Cuda.Transformer;

/// <summary>
/// The CUDA back-end of the dense tensor algebra <see cref="Althea.Transformer.IAbstractApi"/> that utilizes cuFFT
/// </summary>
public unsafe class Api : Althea.Transformer.IAbstractApi
{
	#region basic
	/// <summary>
	/// The default constructor of <see cref="Api"/> with simple FFT plan cache of size 16
	/// </summary>
	public Api()
	{
		this.cacher = new(16);
	}

	/// <inheritdoc/>
	public bool Disposed { get; protected set; }

	/// <inheritdoc/>
	public virtual void Dispose()
	{
		this.cacher.Dispose();
		this.Disposed = true;
		GC.SuppressFinalize(this);
	}

	private record FftPlan(int Id, long WorkSize) : IDisposable
	{
		public void Dispose()
		{
			// do nothing
		}
	}

	private LimitSizedCacher<FftInfo, FftPlan> cacher;

	private readonly struct FftInfo : IEquatable<FftInfo>
	{
		private readonly FixedBuffer_60<int> size;
		private readonly short rank, deviceId;
		private readonly FixedBuffer_60<int> outerSizeIn;
		private readonly DataType typeIn;
		private readonly FixedBuffer_60<int> outerSizeOut;
		private readonly bool outTypeSame, hasOuterIn, hasOuterOut;

		public readonly Span<int> Size
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.size.AsSpan(this.rank);
		}
		public readonly Span<int> OuterSizeIn
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.hasOuterIn ? this.outerSizeIn.AsSpan(this.rank) : default;
		}
		public readonly Span<int> OuterSizeOut
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.hasOuterOut ? this.outerSizeOut.AsSpan(this.rank) : default;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private FftInfo(ReadOnlySpan<int> size, ReadOnlySpan<int> outerSizeIn, ReadOnlySpan<int> outerSizeOut, DataType type, bool outTypeSame)
		{
			this.size.CopyFromSpan(size);
			this.hasOuterIn = !outerSizeIn.IsEmpty;
			if (this.hasOuterIn)
				this.outerSizeIn.CopyFromSpan(outerSizeIn);
			this.hasOuterOut = !outerSizeOut.IsEmpty;
			if (this.hasOuterOut)
				this.outerSizeOut.CopyFromSpan(outerSizeOut);
			this.rank = (short)size.Length;
			this.typeIn = type;
			this.outTypeSame = outTypeSame;
			this.deviceId = (short)Runtime.CurrentDeviceID;
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly void Deconstruct(Span<long> size, ref Span<long> outerSizeIn, ref Span<long> outerSizeOut, out DataType @in, out DataType @out, out DataType compute)
		{
			for (int i = 0; i < this.rank; i++)
			{
				size[i] = this.size[i];
			}
			if (this.hasOuterIn)
			{
				for (int i = 0; i < this.rank; i++)
				{
					outerSizeIn[i] = this.outerSizeIn[i];
				}
			}
			else
			{
				outerSizeIn = default;
			}
			if (this.hasOuterOut)
			{
				for (int i = 0; i < this.rank; i++)
				{
					outerSizeOut[i] = this.outerSizeOut[i];
				}
			}
			else
			{
				outerSizeOut = default;
			}
			@in = this.typeIn;
			@out = this.outTypeSame ? @in : @in.ChangeComplex();
			compute = @in.ToComplex();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly bool Equals(FftInfo info)
		{
			return this.rank == info.rank && this.deviceId == info.deviceId && this.typeIn == info.typeIn && this.outTypeSame == info.outTypeSame && this.Size.SequenceEqual(info.Size) && this.OuterSizeIn.SequenceEqual(info.OuterSizeIn) && this.OuterSizeOut.SequenceEqual(info.OuterSizeOut);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override readonly bool Equals(object? obj) => obj is FftInfo info && this.Equals(info);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override readonly int GetHashCode() => HashCode.Combine(this.rank, this.deviceId, this.typeIn, this.outTypeSame, this.Size.HashCodeOfSpan(), this.OuterSizeIn.HashCodeOfSpan(), this.OuterSizeOut.HashCodeOfSpan());

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool TryCreate(ReadOnlySpan<long> sizeL, ReadOnlySpan<long> outerSizerInL, ReadOnlySpan<long> outerSizeOutL, DataType type, bool outTypeSame, out FftInfo info)
		{
			info = default;
			int r = sizeL.Length;
			Span<int> size = stackalloc int[r], outerSizeIn = stackalloc int[r], outerSizeOut = stackalloc int[r];
			for (int i = 0, j = r - 1; i < r; i++, j--)
			{
				if (sizeL[i] > int.MaxValue || outerSizerInL[i] > int.MaxValue || outerSizeOutL[i] > int.MaxValue)
					return false;
				size[j] = (int)sizeL[i];
				outerSizeIn[j] = (int)outerSizerInL[i];
				outerSizeOut[j] = (int)outerSizeOutL[i];
			}
			if (size.SequenceEqual(outerSizeIn))
				outerSizeIn = default;
			if (size.SequenceEqual(outerSizeOut))
				outerSizeOut = default;
			info = new(size, outerSizeIn, outerSizeOut, type, outTypeSame);
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool TryCreate<T, U, TS, US>(DenseArrayWrapper<T, TS> input, DenseArrayWrapper<U, US> output, out FftInfo info) where T : unmanaged, IBinaryFloat<T> where U : unmanaged, IBinaryFloat<U> where TS : class, IStorage<T, TS> where US : class, IStorage<U, US>
		{
			info = default;
			if (typeof(T) == typeof(U))
			{
				if (!T.IsComplexType)
					return false; // not support
			}
			DataType type = T.Type.ToCuda();
			if (type < 0)
				return false; // not support
			if (!input.Size.SequenceEqual(output.Size))
				throw new ArgumentException(Resources.ParameterError.NotSameSize);
			return TryCreate(input.Size, input.OuterSize, output.OuterSize, type, true, out info);
		}
	}
	#endregion

	#region methods
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool GetPlanAndBufferSize<T, U, TS, US>(DenseArrayWrapper<T, TS> input, DenseArrayWrapper<U, US> output, [MaybeNullWhen(false)] out FftPlan plan) where T : unmanaged, IBinaryFloat<T> where U : unmanaged, IBinaryFloat<U> where TS : class, IStorage<T, TS> where US : class, IStorage<U, US>
	{
		plan = default;
		if (!FftInfo.TryCreate(input, output, out var info))
			return false;
		if (this.cacher.TryGetValue(info, out plan))
			return true;
		if (!NM.cufftCreate(out var planId).Check())
			return false;
		Span<long> size = stackalloc long[input.Rank], outerIn = stackalloc long[input.Rank], outerOut = stackalloc long[input.Rank];
		info.Deconstruct(size, ref outerIn, ref outerOut, out var typeIn, out var typeOut, out var typeCompute);
		if (!NM.cufftXtMakePlanMany(planId, input.Rank, size, outerIn, 1, 0, typeIn, outerOut, 1, 0, typeOut, 1, out var workSize, typeCompute).Check())
			return false;
		if (!NM.cufftSetAutoAllocation(planId, 0).Check())
			return false;
		plan = new(planId, workSize);
		return this.cacher.Add(info, plan);
	}

	/// <inheritdoc/>
	public virtual bool FourierTransform<T, TS1, TS2>(bool forward, DenseArrayWrapper<T, TS1> input, DenseArrayWrapper<T, TS2> output) where T : unmanaged, IBinaryFloat<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
	{
		if (!GetPointer(null, input.ValueStorage, input.Size, input.OuterSize, out T* pIn))
			return false;
		if (!GetPointer(null, output.ValueStorage, output.Size, output.OuterSize, out T* pOut))
			return false;
		if (!GetPlanAndBufferSize(input, output, out var plan))
			return false;
		lock (plan)
		{
			using var buf = CudaBuffer.Create(plan.WorkSize);
			if (!NM.cufftSetWorkArea(plan.Id, buf).Check())
				return false;
			return NM.cufftXtExec(plan.Id, pIn, pOut, forward ? FftDirection.Forward : FftDirection.Backward).Check();
		}
	}

	/// <inheritdoc/>
	public virtual bool FourierTransform<T, TS1, TS2>(DenseArrayWrapper<T, TS1> input, DenseArrayWrapper<Complex<T>, TS2> output) where T : unmanaged, IBinaryFloat<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<Complex<T>, TS2>
	{
		if (!GetPointer(null, input.ValueStorage, input.Size, input.OuterSize, out T* pIn))
			return false;
		if (!GetPointer(null, output.ValueStorage, output.Size, output.OuterSize, out Complex<T>* pOut))
			return false;
		if (!GetPlanAndBufferSize(input, output, out var plan))
			return false;
		lock (plan)
		{
			using var buf = CudaBuffer.Create(plan.WorkSize);
			if (!NM.cufftSetWorkArea(plan.Id, buf).Check())
				return false;
			return NM.cufftXtExec(plan.Id, pIn, pOut, FftDirection.Forward).Check();
		}
	}

	/// <inheritdoc/>
	public virtual bool FourierTransform<T, TS1, TS2>(DenseArrayWrapper<Complex<T>, TS1> input, DenseArrayWrapper<T, TS2> output) where T : unmanaged, IBinaryFloat<T> where TS1 : class, IStorage<Complex<T>, TS1> where TS2 : class, IStorage<T, TS2>
	{
		if (!GetPointer(null, input.ValueStorage, input.Size, input.OuterSize, out Complex<T>* pIn))
			return false;
		if (!GetPointer(null, output.ValueStorage, output.Size, output.OuterSize, out T* pOut))
			return false;
		if (!GetPlanAndBufferSize(input, output, out var plan))
			return false;
		lock (plan)
		{
			using var buf = CudaBuffer.Create(plan.WorkSize);
			if (!NM.cufftSetWorkArea(plan.Id, buf).Check())
				return false;
			return NM.cufftXtExec(plan.Id, pIn, pOut, FftDirection.Backward).Check();
		}
	}
	#endregion
}
