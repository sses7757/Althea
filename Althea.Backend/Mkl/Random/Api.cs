using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

using Althea.Backend.Storage;
using Althea.Helpers;
using Althea.Linq;
using Althea.Random;

using static Althea.Backend.Mkl.MemoryPointerChecker;

using NM = Althea.Backend.Mkl.Random.NativeMethods;


namespace Althea.Backend.Mkl.Random
{
	/// <summary>
	/// The MKL back-end of the <see cref="Althea.Random.IAbstractApi"/> that supports filling CPU arrays with a variety kinds of distributions.
	/// </summary>
	/// <remarks>Only use the <see cref="GeneratorType.SFMT19937"/> is used currently, but the other generator type's support can be easily added.<br/>
	/// Only the default generating algorithms are used currently, but other ones can be easily added.<br/>
	/// Since using the same MKL VSL stream results to thread blockage, this class utilizes a <see cref="ThreadLocal{T}"/> generator to make sure that multi-threading in C# works properly.</remarks>
	public unsafe class Api : Althea.Random.IAbstractApi
	{
		#region basic
		private readonly ThreadLocal<(IntPtr stream, uint seed)> generator;

		private IntPtr Stream {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.generator.Value.stream;
		}

		/// <summary>
		/// Create an <see cref="Api"/>
		/// </summary>
		public Api()
		{
			this.generator = new ThreadLocal<(IntPtr, uint)>(InitializeGenerator, true);
		}

		private static (IntPtr, uint) InitializeGenerator()
		{
			NM.vslNewStream(out var stream, GeneratorType.SFMT19937, 0).Check();
			return (stream, 0);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private IntPtr ChangeGeneratorSeed(long? newSeed)
		{
			uint s;
			if (!newSeed.HasValue || (s = (uint)newSeed.Value) == this.generator.Value.seed)
				return this.generator.Value.stream;
			NM.vslNewStream(out var stream, GeneratorType.SFMT19937, s).Check();
			this.generator.Value = (stream, s);
			return stream;
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			foreach (var (stream, _) in this.generator.Values)
			{
				NM.vslDeleteStream(in stream);
			}
			this.generator.Dispose();
			this.Disposed = true;
			GC.SuppressFinalize(this);
		}

		/// <inheritdoc/>
		public bool Disposed { get; protected set; } = false;
		#endregion

		#region get distribution
		const DistributionType INVALID = (DistributionType)(-1);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe bool Check<T, TS, TDist>(TS storage, in TDist distribution, out T* pointer, out long length, out DistributionType type) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS> where TDist : struct, IRandomDistribution<TDist>
		{
			pointer = default; length = 0;
			type = INVALID;
			if (storage is null || !storage.IsValid())
				throw new ArgumentNullException(nameof(storage));
			if (!distribution.IsValid())
				throw new ArgumentNullException(nameof(distribution));
			if (distribution.Rank != 1)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(distribution));
			if (!GetPointer(storage, out pointer, out length))
				return false;

			type = distribution switch
			{
				UniformDistribution<T> => typeof(T) == typeof(Float32) || typeof(T) == typeof(Float64) || typeof(T) == typeof(SignedInt32) || typeof(T) == typeof(UnsignedInt32) ? DistributionType.Uniform : INVALID,
				RandomBitsDistribution<T> => T.Size == sizeof(int) || T.Size == sizeof(long) ? DistributionType.RandomBits : INVALID,

				BetaDistribution<T> => typeof(T) == typeof(Float32) || typeof(T) == typeof(Float64) ? DistributionType.Beta : INVALID,
				CauchyDistribution<T> => typeof(T) == typeof(Float32) || typeof(T) == typeof(Float64) ? DistributionType.Cauchy : INVALID,
				ChiSquareDistribution<T> => typeof(T) == typeof(Float32) || typeof(T) == typeof(Float64) ? DistributionType.ChiSquare : INVALID,
				ExponentialDistribution<T> => typeof(T) == typeof(Float32) || typeof(T) == typeof(Float64) ? DistributionType.Exponential : INVALID,
				GammaDistribution<T> => typeof(T) == typeof(Float32) || typeof(T) == typeof(Float64) ? DistributionType.Gamma : INVALID,
				GumbelDistribution<T> => typeof(T) == typeof(Float32) || typeof(T) == typeof(Float64) ? DistributionType.Gumbel : INVALID,
				LaplaceDistribution<T> => typeof(T) == typeof(Float32) || typeof(T) == typeof(Float64) ? DistributionType.Laplace : INVALID,
				LogNormalDistribution<T> => typeof(T) == typeof(Float32) || typeof(T) == typeof(Float64) ? DistributionType.LogNormal : INVALID,
				NegativeBinomialDistribution<T> => typeof(T) == typeof(int) || typeof(T) == typeof(uint) ? DistributionType.NegativeBinomial : INVALID,
				NormalDistribution<T> => typeof(T) == typeof(Float32) || typeof(T) == typeof(Float64) ? DistributionType.Normal : INVALID,
				RayleighDistribution<T> => typeof(T) == typeof(Float32) || typeof(T) == typeof(Float64) ? DistributionType.Rayleigh : INVALID,
				WeibullDistribution<T> => typeof(T) == typeof(Float32) || typeof(T) == typeof(Float64) ? DistributionType.Weibull : INVALID,

				BernoulliDistribution<T> => typeof(T) == typeof(int) || typeof(T) == typeof(uint) ? DistributionType.Bernoulli : INVALID,
				BinomialDistribution<T> => typeof(T) == typeof(int) || typeof(T) == typeof(uint) ? DistributionType.Binomial : INVALID,
				GeometricDistribution<T> => typeof(T) == typeof(int) || typeof(T) == typeof(uint) ? DistributionType.Geometric : INVALID,
				HypergeometricDistribution<T> => typeof(T) == typeof(int) || typeof(T) == typeof(uint) ? DistributionType.Hypergeometric : INVALID,
				PoissonDistribution<T> => typeof(T) == typeof(int) || typeof(T) == typeof(uint) ? DistributionType.Poisson : INVALID,
				_ => INVALID,
			};
			return type >= 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe bool Check<TDist>(ReadOnlySpan<IStorage> storages, in TDist distribution, Span<IntPtr> pointers, Span<int> lengths, out DistributionType type) where TDist : struct, IRandomDistribution<TDist>
		{
			if (!distribution.IsValid())
				throw new ArgumentNullException(nameof(distribution));
			if (storages.Any(static s => s is null || !s.IsValid()))
				throw new ArgumentNullException(nameof(storages));
			if (distribution.Rank != storages.Length)
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(storages));

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static bool GetPointers<T>(ReadOnlySpan<IStorage> storages, Span<IntPtr> pointers, Span<int> lengths) where T : unmanaged, IBaseNumber<T>
			{
				if (!storages.All(static s => s is PureStorage<T, CpuMemoryPointer>))
					return false;
				for (int i = 0; i < storages.Length; i++)
				{
					var s = storages[i];
					if (!GetPointer((PureStorage<T, CpuMemoryPointer>)s, out T* ptr, out var len))
						return false;
					pointers[i] = (IntPtr)ptr; lengths[i] = (int)len;
				}
				return true;
			}

			type = INVALID;
			type = distribution switch
			{
				MultinomialDistribution<SignedInt32> => GetPointers<SignedInt32>(storages, pointers, lengths) ? DistributionType.Multinomial : INVALID,
				MultinomialDistribution<UnsignedInt32> => GetPointers<UnsignedInt32>(storages, pointers, lengths) ? DistributionType.Multinomial : INVALID,
				MultiNormalDistribution<Float32> => GetPointers<Float32>(storages, pointers, lengths) ? DistributionType.MultiNormal : INVALID,
				MultiNormalDistribution<Float64> => GetPointers<Float64>(storages, pointers, lengths) ? DistributionType.MultiNormal : INVALID,
				BinormalDistribution<Float32> => GetPointers<Float32>(storages, pointers, lengths) ? DistributionType.Binormal : INVALID,
				BinormalDistribution<Float64> => GetPointers<Float64>(storages, pointers, lengths) ? DistributionType.Binormal : INVALID,
				_ => INVALID,
			};
			if (!lengths.AllSame())
				type = INVALID;
			return type != INVALID;
		}
		#endregion

		#region methods
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private unsafe void Fill1D<T, TDist>(T* ptr, long n, in TDist distribution, DistributionType type) where T : unmanaged, IBaseNumber<T> where TDist : struct, IRandomDistribution<TDist>
		{
			IntPtr p = (IntPtr)ptr;
			T shape1, shape2, scale, displace, mean, sigma;
			switch (type)
			{
				case DistributionType.Uniform:
					ref var uniform = ref SpanHelper.As<TDist, UniformDistribution<T>>(distribution);
					T lb = uniform.LowerBound, ub = uniform.UpperBound;
					if (typeof(T) == typeof(Float32))
						NM.vsRngUniform(MklRngMethodUniform.Standard, this.Stream, n, p.AsF4(), lb.AsF4(), ub.AsF4()).Check();
					if (typeof(T) == typeof(Float64))
						NM.vdRngUniform(MklRngMethodUniform.Standard, this.Stream, n, p.AsF8(), lb.AsF8(), ub.AsF8()).Check();
					if (typeof(T) == typeof(SignedInt32) || (typeof(T) == typeof(UnsignedInt32) && ub.AsU4() <= int.MaxValue))
						NM.viRngUniform(MklRngMethodUniform.Standard, this.Stream, n, p.AsI4(), lb.AsI4(), ub.AsI4()).Check();
					break;
				case DistributionType.RandomBits:
					if (sizeof(T) == sizeof(SignedInt32))
						NM.viRngUniformBits32(MklRngMethodUniformBits.Standard, this.Stream, n, p.AsU4()).Check();
					if (sizeof(T) == sizeof(SignedInt64))
						NM.viRngUniformBits64(MklRngMethodUniformBits.Standard, this.Stream, n, p.AsU8()).Check();
					break;

				case DistributionType.Beta:
					ref var beta = ref SpanHelper.As<TDist, BetaDistribution<T>>(distribution);
					shape1 = beta.ShapeFactor; shape2 = beta.ShapeFactorOther; scale = beta.ScaleFactor; displace = beta.Displacement;
					if (typeof(T) == typeof(Float32))
						NM.vsRngBeta(MklRngMethodBeta.CJA, this.Stream, n, p.AsF4(), shape1.AsF4(), shape2.AsF4(), displace.AsF4(), scale.AsF4()).Check();
					if (typeof(T) == typeof(Float64))
						NM.vdRngBeta(MklRngMethodBeta.CJA, this.Stream, n, p.AsF8(), shape1.AsF8(), shape2.AsF8(), displace.AsF8(), scale.AsF8()).Check();
					break;
				case DistributionType.Cauchy:
					ref var cauchy =ref SpanHelper.As<TDist, CauchyDistribution<T>>(distribution);
					scale = cauchy.ScaleFactor; displace = cauchy.Displacement;
					if (typeof(T) == typeof(Float32))
						NM.vsRngCauchy(MklRngMethodCauchy.ICDF, this.Stream, n, p.AsF4(), displace.AsF4(), scale.AsF4()).Check();
					if (typeof(T) == typeof(Float64))
						NM.vdRngCauchy(MklRngMethodCauchy.ICDF, this.Stream, n, p.AsF8(), displace.AsF8(), scale.AsF8()).Check();
					break;
				case DistributionType.ChiSquare:
					ref var chi2 = ref SpanHelper.As<TDist, ChiSquareDistribution<T>>(distribution);
					if (typeof(T) == typeof(Float32))
						NM.vsRngChiSquare(MklRngMethodChiSquare.Chi2Gamma, this.Stream, n, p.AsF4(), chi2.DegreeOfFreedom).Check();
					if (typeof(T) == typeof(Float64))
						NM.vdRngChiSquare(MklRngMethodChiSquare.Chi2Gamma, this.Stream, n, p.AsF8(), chi2.DegreeOfFreedom).Check();
					break;
				case DistributionType.Exponential:
					ref var exp = ref SpanHelper.As<TDist, ExponentialDistribution<T>>(distribution);
					scale = exp.ScaleFactor; displace = exp.Displacement;
					if (typeof(T) == typeof(Float32))
						NM.vsRngExponential(MklRngMethodExponential.ICDF, this.Stream, n, p.AsF4(), displace.AsF4(), scale.AsF4()).Check();
					if (typeof(T) == typeof(Float64))
						NM.vdRngExponential(MklRngMethodExponential.ICDF, this.Stream, n, p.AsF8(), displace.AsF8(), scale.AsF8()).Check();
					break;
				case DistributionType.Gamma:
					ref var gamma = ref SpanHelper.As<TDist, GammaDistribution<T>>(distribution);
					shape1 = gamma.ShapeFactor; scale = gamma.ScaleFactor; displace = gamma.Displacement;
					if (typeof(T) == typeof(Float32))
						NM.vsRngGamma(MklRngMethodGamma.GNorm, this.Stream, n, p.AsF4(), shape1.AsF4(), displace.AsF4(), scale.AsF4()).Check();
					if (typeof(T) == typeof(Float64))
						NM.vdRngGamma(MklRngMethodGamma.GNorm, this.Stream, n, p.AsF8(), shape1.AsF8(), displace.AsF8(), scale.AsF8()).Check();
					break;
				case DistributionType.Gumbel:
					ref var gumbel = ref SpanHelper.As<TDist, GumbelDistribution<T>>(distribution);
					scale = gumbel.ScaleFactor; displace = gumbel.Displacement;
					if (typeof(T) == typeof(Float32))
						NM.vsRngGumbel(MklRngMethodGumbel.ICDF, this.Stream, n, p.AsF4(), displace.AsF4(), scale.AsF4()).Check();
					if (typeof(T) == typeof(Float64))
						NM.vdRngGumbel(MklRngMethodGumbel.ICDF, this.Stream, n, p.AsF8(), displace.AsF8(), scale.AsF8()).Check();
					break;
				case DistributionType.Laplace:
					ref var laplace = ref SpanHelper.As<TDist, LaplaceDistribution<T>>(distribution);
					scale = laplace.ScaleFactor; displace = laplace.Displacement;
					if (typeof(T) == typeof(Float32))
						NM.vsRngLaplace(MklRngMethodLaplace.ICDF, this.Stream, n, p.AsF4(), displace.AsF4(), scale.AsF4()).Check();
					if (typeof(T) == typeof(Float64))
						NM.vdRngLaplace(MklRngMethodLaplace.ICDF, this.Stream, n, p.AsF8(), displace.AsF8(), scale.AsF8()).Check();
					break;
				case DistributionType.LogNormal:
					ref var lognormal = ref SpanHelper.As<TDist, LogNormalDistribution<T>>(distribution);
					scale = T.One; displace = T.Zero; mean = lognormal.Displacement; sigma = lognormal.ScaleFactor;
					if (typeof(T) == typeof(Float32))
						NM.vsRngLognormal(MklRngMethodLogNormal.BoxMuller2, this.Stream, n, p.AsF4(), mean.AsF4(), sigma.AsF4(), displace.AsF4(), scale.AsF4()).Check();
					if (typeof(T) == typeof(Float64))
						NM.vdRngLognormal(MklRngMethodLogNormal.BoxMuller2, this.Stream, n, p.AsF8(), mean.AsF4(), sigma.AsF4(), displace.AsF8(), scale.AsF8()).Check();
					break;
				case DistributionType.Normal:
					ref var normal = ref SpanHelper.As<TDist, NormalDistribution<T>>(distribution);
					mean = normal.Displacement; sigma = normal.ScaleFactor;
					if (typeof(T) == typeof(Float32))
						NM.vsRngGaussian(MklRngMethodGaussian.BoxMuller, this.Stream, n, p.AsF4(), mean.AsF4(), sigma.AsF4()).Check();
					if (typeof(T) == typeof(Float64))
						NM.vdRngGaussian(MklRngMethodGaussian.BoxMuller, this.Stream, n, p.AsF8(), mean.AsF4(), sigma.AsF4()).Check();
					break;
				case DistributionType.Rayleigh:
					ref var rayleigh = ref SpanHelper.As<TDist, RayleighDistribution<T>>(distribution);
					scale = rayleigh.ScaleFactor; displace = rayleigh.Displacement;
					if (typeof(T) == typeof(Float32))
						NM.vsRngRayleigh(MklRngMethodRayleigh.ICDF, this.Stream, n, p.AsF4(), displace.AsF4(), scale.AsF4()).Check();
					if (typeof(T) == typeof(Float64))
						NM.vdRngRayleigh(MklRngMethodRayleigh.ICDF, this.Stream, n, p.AsF8(), displace.AsF8(), scale.AsF8()).Check();
					break;
				case DistributionType.Weibull:
					ref var weibull = ref SpanHelper.As<TDist, WeibullDistribution<T>>(distribution);
					shape1 = weibull.ShapeFactor; scale = weibull.ScaleFactor; displace = weibull.Displacement;
					if (typeof(T) == typeof(Float32))
						NM.vsRngWeibull(MklRngMethodWeibull.ICDF, this.Stream, n, p.AsF4(), shape1.AsF4(), displace.AsF4(), scale.AsF4()).Check();
					if (typeof(T) == typeof(Float64))
						NM.vdRngWeibull(MklRngMethodWeibull.ICDF, this.Stream, n, p.AsF8(), shape1.AsF8(), displace.AsF8(), scale.AsF8()).Check();
					break;

				case DistributionType.Bernoulli:
					NM.viRngBernoulli(MklRngMethodBernoulli.ICDF, this.Stream, n, p.AsI4(), (double)SpanHelper.As<TDist, BernoulliDistribution<T>>(distribution).Probability).Check();
					break;
				case DistributionType.Binomial:
					NM.viRngBinomial(MklRngMethodBinomial.BTPE, this.Stream, n, p.AsI4(), SpanHelper.As<TDist, BinomialDistribution<T>>(distribution).NTrials, (double)SpanHelper.As<TDist, BinomialDistribution<T>>(distribution).Probability).Check();
					break;
				case DistributionType.NegativeBinomial:
					NM.viRngNegbinomial(MklRngMethodNegativeBinomial.NBar, this.Stream, n, p.AsI4(), SpanHelper.As<TDist, NegativeBinomialDistribution<T>>(distribution).SuccessCount, (double)SpanHelper.As<TDist, NegativeBinomialDistribution<T>>(distribution).Probability).Check();
					break;
				case DistributionType.Geometric:
					NM.viRngGeometric(MklRngMethodGeometric.ICDF, this.Stream, n, p.AsI4(), (double)SpanHelper.As<TDist, GeometricDistribution<T>>(distribution).Probability).Check();
					break;
				case DistributionType.Hypergeometric:
					ref var hyper = ref SpanHelper.As<TDist, HypergeometricDistribution<T>>(distribution);
					NM.viRngHypergeometric(MklRngMethodHypergeometric.H2PE, this.Stream, n, p.AsI4(), hyper.TotalSize, hyper.SampleSize, hyper.MarkSize).Check();
					break;
				case DistributionType.Poisson:
					NM.viRngPoisson(MklRngMethodPoisson.PTPE, this.Stream, n, p.AsI4(), (double)SpanHelper.As<TDist, PoissonDistribution<T>>(distribution).Lambda).Check();
					break;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private unsafe void FillND<TDist>(in TDist distribution, Span<IntPtr> pointers, int length) where TDist : struct, IRandomDistribution<TDist>
		{
			void* p = Unsafe.AsPointer(ref pointers[0]);
			if (distribution is MultinomialDistribution<SignedInt32> or MultinomialDistribution<UnsignedInt32>)
			{
				ref var nomial = ref SpanHelper.As<TDist, MultinomialDistribution<SignedInt32>>(distribution);
				Span<double> prob = stackalloc double[nomial.Rank];
				nomial.Probabilities.CopyTo(prob, static d => (double)d);
				NM.viRngMultinomial(MklRngMethodMultinomial.MultiPoisson, this.Stream, length, (int**)p, nomial.NTrials, nomial.Rank, prob).Check();
			}
			else if (distribution is BinormalDistribution<Float32> binormal)
			{
				MultiNormalDistribution<Float32> dist = new(stackalloc Float32[] { binormal.Mean1, binormal.Mean2 },
					stackalloc Float32[]
					{
						binormal.StandardDeviation1, 0f,
						binormal.StandardDeviation2 * binormal.Correlation,
						binormal.StandardDeviation2 * MathF.Sqrt(1.0f - binormal.Correlation * binormal.Correlation)
				}, false, binormal.RandomSeed);
				FillND(dist, pointers, length);
				return;
			}
			else if (distribution is MultiNormalDistribution<Float32>)
			{
				ref var normal = ref SpanHelper.As<TDist, MultiNormalDistribution<Float32>>(in distribution);
				int rank = normal.Rank;
				Span<Float32> sigma = stackalloc Float32[rank * rank];
				if (normal.OriginalCovarianceStored)
				{
					normal.GetCholesky(sigma);
				}
				else
				{
					sigma = MemoryMarshal.CreateSpan(ref normal.CovarianceMatrix.Ref(), rank * rank);
				}
				NM.vsRngGaussianMV(MklRngMethodGaussian.BoxMuller, this.Stream, length, (float**)p, rank, MklRngMatrixStorage.Full, normal.Means.UncheckAs<Float32, float>(), sigma.UncheckAs<Float32, float>()).Check();
			}
			else if (distribution is MultiNormalDistribution<Float64>)
			{
				ref var normal = ref SpanHelper.As<TDist, MultiNormalDistribution<Float64>>(in distribution);
				int rank = normal.Rank;
				Span<Float64> sigma = stackalloc Float64[rank * rank];
				if (normal.OriginalCovarianceStored)
				{
					normal.GetCholesky(sigma);
				}
				else
				{
					sigma = MemoryMarshal.CreateSpan(ref normal.CovarianceMatrix.Ref(), rank * rank);
				}
				NM.vdRngGaussianMV(MklRngMethodGaussian.BoxMuller, this.Stream, length, (double**)p, rank, MklRngMatrixStorage.Full, normal.Means.UncheckAs<Float64, double>(), sigma.UncheckAs<Float64, double>()).Check();
			}
		}

		/// <inheritdoc/>
		public virtual unsafe bool FillWithRandom<T, TS, TDist>(TS storage, in TDist distribution) where T : unmanaged, IBaseNumber<T> where TS : class, IStorage<T, TS> where TDist : struct, IRank1Distribution<T, TDist>
		{
			if (!Check(storage, distribution, out T* ptr, out var len, out var type))
				return false;
			Fill1D(ptr, len, distribution, type);
			return true;
		}

		/// <inheritdoc/>
		public virtual bool FillWithRandom<T1, T2, TS1, TS2, TDist>(TS1 storage1, TS2 storage2, in TDist distribution) where T1 : unmanaged, IBaseNumber<T1> where T2 : unmanaged, IBaseNumber<T2> where TS1 : class, IStorage<T1, TS1> where TS2 : class, IStorage<T2, TS2> where TDist : struct, IRank2Distribution<T1, T2, TDist>
		{
			Span<IStorage> storages = stackalloc IntPtr[2].AsClassType<IStorage>().SetValue(storage1, storage2);
			Span<IntPtr> ptrs = stackalloc IntPtr[2]; Span<int> lens = stackalloc int[2];
			if (!Check(storages, distribution, ptrs, lens, out _))
				return false;
			FillND(distribution, ptrs, lens[0]);
			return true;
		}

		/// <inheritdoc/>
		public virtual bool FillWithRandom<T1, T2, T3, TS1, TS2, TS3, TDist>(TS1 storage1, TS2 storage2, TS3 storage3, in TDist distribution) where T1 : unmanaged, IBaseNumber<T1> where T2 : unmanaged, IBaseNumber<T2> where T3 : unmanaged, IBaseNumber<T3> where TS1 : class, IStorage<T1, TS1> where TS2 : class, IStorage<T2, TS2> where TS3 : class, IStorage<T3, TS3> where TDist : struct, IRank3Distribution<T1, T2, T3, TDist>
		{
			Span<IStorage> storages = stackalloc IntPtr[3].AsClassType<IStorage>().SetValue(storage1, storage2, storage3);
			Span<IntPtr> ptrs = stackalloc IntPtr[3]; Span<int> lens = stackalloc int[3];
			if (!Check(storages, distribution, ptrs, lens, out _))
				return false;
			FillND(distribution, ptrs, lens[0]);
			return true;
		}

		/// <inheritdoc/>
		public virtual bool FillWithRandom<TDist>(ReadOnlySpan<IStorage> storages, in TDist distribution) where TDist : struct, IRandomDistribution<TDist>
		{
			Span<IntPtr> ptrs = stackalloc IntPtr[distribution.Rank]; Span<int> lens = stackalloc int[distribution.Rank];
			if (!Check(storages, distribution, ptrs, lens, out _))
				return false;
			FillND(distribution, ptrs, lens[0]);
			return true;
		}
		#endregion
	}

	internal static class TempConversions
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static unsafe float* AsF4(this IntPtr v) => (float*)v;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static unsafe double* AsF8(this IntPtr v) => (double*)v;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static unsafe int* AsI4(this IntPtr v) => (int*)v;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static unsafe uint* AsU4(this IntPtr v) => (uint*)v;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static unsafe ulong* AsU8(this IntPtr v) => (ulong*)v;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static unsafe float AsF4<T>(this T v) where T : unmanaged, IBaseNumber<T> => *(float*)&v;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static unsafe double AsF8<T>(this T v) where T : unmanaged, IBaseNumber<T> => *(double*)&v;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static unsafe int AsI4<T>(this T v) where T : unmanaged, IBaseNumber<T> => *(int*)&v;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static unsafe uint AsU4<T>(this T v) where T : unmanaged, IBaseNumber<T> => *(uint*)&v;
	}
}
