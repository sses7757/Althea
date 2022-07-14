using System.Runtime.CompilerServices;

using Althea.Array;
using Althea.Backend.Storage;
using Althea.Linq;


namespace Althea.Backend.Mkl.Transformer
{
	/// <summary>
	/// The MKL back-end of <see cref="Althea.Transformer.IAbstractApi"/> that supports storage locations of CPU memory.
	/// </summary>
	public unsafe class Api : Althea.Transformer.IAbstractApi
	{
		#region basic
		/// <inheritdoc/>
		public bool Disposed { get; set; } = false;

		/// <inheritdoc/>
		public void Dispose()
		{
			this.Disposed = true;
			foreach (var kv in this.cache)
			{
				kv.Value.Dispose();
			}
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Get or set a <see cref="bool"/> indicating whether this <see cref="Api"/> instance shall cache invoked FFT plans for future use or not. This only works for 1D FFT.
		/// </summary>
		public bool CacheFftPlan { get; set; } = false;

		private readonly Dictionary<(DataType type, bool? inplace, bool forward, double scale, long length), DftiDescriptor> cache = new();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool GetPointer<T, TS>(TS s, ReadOnlySpan<long> size, ReadOnlySpan<long> outerSize, out T* pointer, out long length, [CallerArgumentExpression("s")] string? sName = null, [CallerArgumentExpression("size")] string? sizeName = null, [CallerArgumentExpression("outerSize")] string? outerSizeName = null) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS>
		{
			pointer = default; length = size.Prod();
			if (s is null || !s.IsValid())
				throw new ArgumentNullException(sName);
			if (size.Any(static s => s <= 0))
				throw new ArgumentOutOfRangeException(sizeName, size.ToArray(), Resources.ParameterError.MustPositive);
			if (outerSize.Any(static s => s <= 0))
				throw new ArgumentOutOfRangeException(outerSizeName, outerSize.ToArray(), Resources.ParameterError.MustPositive);
			if (!outerSize.SequenceLargerEqualThan(size))
				throw new ArgumentException(Resources.ParameterError.InvalidValue, outerSizeName);
			if (s is not PureStorage<T, CpuMemoryPointer> ps)
				return false; // not support
			ps.Pointer.Pointer.UnmangedPointer<T>(ps.Pointer.OffsetInBytes);
			if (pointer == default)
				throw new ArgumentException(Resources.ParameterError.InvalidValue, sName);
			return true;
		}
		#endregion

		#region operations
		/// <inheritdoc/>
		public virtual bool FourierTransform<T, TS1, TS2>(bool forward, DenseArrayWrapper<T, TS1> input, DenseArrayWrapper<T, TS2> output) where T : unmanaged, IBinaryFloat<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
		{
			if (!GetPointer(input.ValueStorage, input.Size, input.OuterSize, out T* pIn, out var len))
				return false;
			if (!GetPointer(output.ValueStorage, output.Size, output.OuterSize, out T* pOut, out _))
				return false;

			var scale = Math.ReciprocalSqrtEstimate(len);
			DftiDescriptor descr;
			if (CacheFftPlan && input.Rank == 1)
			{
				var key = (T.Type, pIn == pOut, forward, scale, len);
				if (!this.cache.TryGetValue(key, out descr))
				{
					if (!DftiDescriptor.TryCreate(forward, scale, input, output, out descr))
						return false;
					this.cache.Add(key, descr);
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
			if (CacheFftPlan && input.Rank == 1)
			{
				var key = (T.Type, pIn == pOut, true, scale, len);
				if (!this.cache.TryGetValue(key, out descr))
				{
					if (!DftiDescriptor.TryCreate(scale, input, output, out descr))
						return false;
					this.cache.Add(key, descr);
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
			if (CacheFftPlan && input.Rank == 1)
			{
				var key = (T.Type, pIn == pOut, false, scale, len);
				if (!this.cache.TryGetValue(key, out descr))
				{
					if (!DftiDescriptor.TryCreate(scale, input, output, out descr))
						return false;
					this.cache.Add(key, descr);
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
}
