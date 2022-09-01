using System.Dynamic;

using Althea.Array;
using Althea.Helpers;

using static Althea.Backend.Mkl.MemoryPointerChecker;


namespace Althea.Backend.Mkl.Transformer;

/// <summary>
/// The MKL back-end of <see cref="Althea.Transformer.IAbstractApi"/> that supports storage locations of CPU memory.
/// </summary>
public unsafe class Api : Althea.Transformer.IAbstractApi
{
	#region basic
	/// <summary>
	/// The default constructor
	/// </summary>
	public Api()
	{
		this.cacher = new(16);
		this.Properties = new DynamicProperties(this);
	}

	/// <inheritdoc/>
	public bool Disposed { get; protected set; } = false;

	/// <inheritdoc/>
	public void Dispose()
	{
		this.cacher.Dispose();
		this.Disposed = true;
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Get or set a <see cref="bool"/> indicating whether this <see cref="Api"/> instance shall cache invoked FFT plans for future use or not. This only works for 1D FFT.
	/// </summary>
	public bool CachePlans { get; set; } = false;

	/// <summary>
	/// Get or set the number of cached FFT plans
	/// </summary>
	public int CachingCapacity
	{
		get => this.cacher.Capacity;
		set => this.cacher.Capacity = value;
	}

	private LimitSizedCacher<(DataType type, bool? inplace, bool forward, double scale, long length), DftiDescriptor> cacher;
	#endregion

	#region dynamic
	/// <inheritdoc/>
	public dynamic Properties { get; }

	/// <inheritdoc/>
	protected sealed class DynamicProperties : Althea.Transformer.IAbstractApi.DynamicProperties
	{
		internal DynamicProperties(Api @this) : base(@this) { }

		/// <inheritdoc/>
		public override bool TryGetMember(GetMemberBinder binder, out object? result)
		{
			if (binder.Name == nameof(CachePlans) && binder.ReturnType == typeof(bool))
			{
				result = (this.api as Api)!.CachePlans;
				return true;
			}
			if (binder.Name == nameof(CachingCapacity) && binder.ReturnType == typeof(int))
			{
				result = (this.api as Api)!.CachingCapacity;
				return true;
			}
			result = null;
			return false;
		}

		/// <inheritdoc/>
		public override bool TrySetMember(SetMemberBinder binder, object? value)
		{
			if (binder.Name == nameof(CachePlans) && value is bool b)
			{
				(this.api as Api)!.CachePlans = b;
				return true;
			}
			if (binder.Name == nameof(CachingCapacity) && value is int i)
			{
				(this.api as Api)!.CachingCapacity = i;
				return true;
			}
			return false;
		}
	}
	#endregion

	#region operations
	/// <inheritdoc/>
	public virtual bool FourierTransform<T, TS1, TS2>(bool forward, DenseArrayWrapper<T, TS1> input, DenseArrayWrapper<T, TS2> output) where T : unmanaged, IBinaryFloat<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
	{
		if (!input.Size.SequenceEqual(output.Size))
			throw new ArgumentException(Resources.ParameterError.NotSameSize);
		if (!GetPointer(input.ValueStorage, input.Size, input.OuterSize, out T* pIn, out var len))
			return false;
		if (!GetPointer(output.ValueStorage, output.Size, output.OuterSize, out T* pOut, out _))
			return false;

		DftiDescriptor descr;
		var scale = Math.ReciprocalSqrtEstimate(len);
		if (CachePlans && input.Rank == 1)
		{
			var key = (T.Type, pIn == pOut, forward, scale, len);
			if (!this.cacher.TryGetValue(key, out descr))
			{
				if (!DftiDescriptor.TryCreate(forward, scale, input, output, out descr))
					return false;
				this.cacher.Add(key, descr);
			}
		}
		else
		{
			if (!DftiDescriptor.TryCreate(forward, scale, input, output, out descr))
				return false;
		}
		descr.Compute(forward, pIn, pOut);
		return true;
	}

	/// <inheritdoc/>
	public virtual bool FourierTransform<T, TS1, TS2>(DenseArrayWrapper<T, TS1> input, DenseArrayWrapper<Complex<T>, TS2> output) where T : unmanaged, IBinaryFloat<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<Complex<T>, TS2>
	{
		if (!GetPointer(input.ValueStorage, input.Size, input.OuterSize, out T* pIn, out var len))
			return false;
		if (!GetPointer(output.ValueStorage, output.Size, output.OuterSize, out Complex<T>* pOut, out _))
			return false;

		var scale = Math.ReciprocalSqrtEstimate(len);
		DftiDescriptor descr;
		if (CachePlans && input.Rank == 1)
		{
			var key = (T.Type, pIn == pOut, true, scale, len);
			if (!this.cacher.TryGetValue(key, out descr))
			{
				if (!DftiDescriptor.TryCreate(scale, input, output, out descr))
					return false;
				this.cacher.Add(key, descr);
			}
		}
		else
		{
			if (!DftiDescriptor.TryCreate(scale, input, output, out descr))
				return false;
		}
		descr.Compute(pIn, pOut);
		return true;
	}

	/// <inheritdoc/>
	public virtual bool FourierTransform<T, TS1, TS2>(DenseArrayWrapper<Complex<T>, TS1> input, DenseArrayWrapper<T, TS2> output) where T : unmanaged, IBinaryFloat<T> where TS1 : class, IStorage<Complex<T>, TS1> where TS2 : class, IStorage<T, TS2>
	{
		if (!GetPointer(input.ValueStorage, input.Size, input.OuterSize, out Complex<T>* pIn, out var len))
			return false;
		if (!GetPointer(output.ValueStorage, output.Size, output.OuterSize, out T* pOut, out _))
			return false;

		var scale = Math.ReciprocalSqrtEstimate(len);
		DftiDescriptor descr;
		if (CachePlans && input.Rank == 1)
		{
			var key = (T.Type, pIn == pOut, false, scale, len);
			if (!this.cacher.TryGetValue(key, out descr))
			{
				if (!DftiDescriptor.TryCreate(scale, input, output, out descr))
					return false;
				this.cacher.Add(key, descr);
			}
		}
		else
		{
			if (!DftiDescriptor.TryCreate(scale, input, output, out descr))
				return false;
		}
		descr.Compute(pIn, pOut);
		return true;
	}
	#endregion
}
