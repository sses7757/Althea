using System.Runtime.CompilerServices;
using System.Threading;

using Althea.Backend.Mkl.LinearAlgebra.Dense;
using Althea.Storage;
using Althea.Linq;
using Althea.Numerics;
using Althea.Random;


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
			NativeMethods.vslNewStream(out var stream, GeneratorType.SFMT19937, 0).Check();
			return (stream, 0);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private IntPtr ChangeGeneratorSeed(long? newSeed)
		{
			uint s;
			if (!newSeed.HasValue || (s = (uint)newSeed.Value) == this.generator.Value.seed)
				return this.generator.Value.stream;
			NativeMethods.vslNewStream(out var stream, GeneratorType.SFMT19937, s).Check();
			this.generator.Value = (stream, s);
			return stream;
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			foreach (var (stream, _) in this.generator.Values)
			{
				NativeMethods.vslDeleteStream(in stream);
			}
			this.generator.Dispose();
			this.Disposed = true;
			GC.SuppressFinalize(this);
		}

		/// <inheritdoc/>
		public bool Disposed { get; set; } = false;


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool GetPointer<T, TS>(TS s, out T* pointer, out long length, [CallerArgumentExpression("s")] string? sName = null) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>
		{
			pointer = default; length = 0;
			if (s is null || !s.IsValid())
				throw new ArgumentNullException(sName);
			if (s is not PureStorage<T, CpuMemoryPointer> ps)
				return false; // not support
			ps.Pointer.Pointer.UnmangedPointer<T>(ps.Pointer.OffsetInBytes);
			if (pointer == default)
				throw new ArgumentException(Resources.ParameterError.InvalidValue, sName);
			length = ps.Length;
			return true;
		}
		#endregion

		#region get distribution
		const DistributionType INVALID = (DistributionType)(-1);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe bool Check<T, TS>(TS storage, IRandomDistribution distribution!!, out T* pointer, out long length, out DistributionType type) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>
		{
			pointer = default; length = 0;
			type = INVALID;
			if (storage is null || !storage.IsValid())
				throw new ArgumentNullException(nameof(storage));
			if (distribution.Rank != 1)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(distribution));
			if (!GetPointer(storage, out pointer, out length))
				return false;

			type = distribution switch
			{
				UniformDistribution<T> => typeof(T) == typeof(float) || typeof(T) == typeof(double) || typeof(T) == typeof(int) || typeof(T) == typeof(uint) ? DistributionType.Uniform : INVALID,
				RandomBitsDistribution<T> => T.Size == sizeof(int) || T.Size == sizeof(long) ? DistributionType.RandomBits : INVALID,

				BetaDistribution<T> => typeof(T) == typeof(float) || typeof(T) == typeof(double) ? DistributionType.Beta : INVALID,
				CauchyDistribution<T> => typeof(T) == typeof(float) || typeof(T) == typeof(double) ? DistributionType.Cauchy : INVALID,
				ChiSquareDistribution<T> => typeof(T) == typeof(float) || typeof(T) == typeof(double) ? DistributionType.ChiSquare : INVALID,
				ExponentialDistribution<T> => typeof(T) == typeof(float) || typeof(T) == typeof(double) ? DistributionType.Exponential : INVALID,
				GammaDistribution<T> => typeof(T) == typeof(float) || typeof(T) == typeof(double) ? DistributionType.Gamma : INVALID,
				GumbelDistribution<T> => typeof(T) == typeof(float) || typeof(T) == typeof(double) ? DistributionType.Gumbel : INVALID,
				LaplaceDistribution<T> => typeof(T) == typeof(float) || typeof(T) == typeof(double) ? DistributionType.Laplace : INVALID,
				LogNormalDistribution<T> => typeof(T) == typeof(float) || typeof(T) == typeof(double) ? DistributionType.LogNormal : INVALID,
				NegativeBinomialDistribution<T> => typeof(T) == typeof(int) || typeof(T) == typeof(uint) ? DistributionType.NegativeBinomial : INVALID,
				NormalDistribution<T> => typeof(T) == typeof(float) || typeof(T) == typeof(double) ? DistributionType.Normal : INVALID,
				RayleighDistribution<T> => typeof(T) == typeof(float) || typeof(T) == typeof(double) ? DistributionType.Rayleigh : INVALID,
				WeibullDistribution<T> => typeof(T) == typeof(float) || typeof(T) == typeof(double) ? DistributionType.Weibull : INVALID,

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
		private static bool Check(ReadOnlySpan<IStorage> storages, IRandomDistribution distribution!!, Span<IntPtr> pointers, Span<int> lengths, out DistributionType type)
		{
			if (storages.Any(static s => s is null || !s.IsValid()))
				throw new ArgumentNullException(nameof(storages));
			if (distribution.Count != storages.Length)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(distribution));

			type = (DistributionType)(-1);
			for (int i = 0; i < storages.Length; i++)
			{
				var ss = storages[i][0];
				if (storages[i].Count != 1 || ss.Pointer is not IMemoryPointer p)
					return false; // not support
				if (!storages[i].DataType.IsReal())
					return false; // not support
				int sizeT = storages[i].DataType.Bytes();
				if (ss.LengthInBytes / sizeT > int.MaxValue)
					return false; // not support
				var pointer = (IntPtr)(p.Pointer.ToInt64() + ss.OffsetInBytes);
				var length = (int)(ss.LengthInBytes / sizeT);
				pointers[i] = pointer; lengths[i] = length;
			}

			type = distribution switch
			{
				MultinomialDistribution<int> => storages.All(static s => s.DataType == DataType.RealInt32) ? DistributionType.Multinomial : INVALID,
				MultinomialDistribution<uint> => storages.All(static s => s.DataType == DataType.RealUInt32) ? DistributionType.Multinomial : INVALID,
				MultiNormalDistribution<float> => storages.All(static s => s.DataType == DataType.RealSingle) ? DistributionType.MultiNormal : INVALID,
				MultiNormalDistribution<double> => storages.All(static s => s.DataType == DataType.RealDouble) ? DistributionType.MultiNormal : INVALID,
				SimpleJointRandomDistribution => DistributionType.SimpleJoint,
				_ => INVALID,
			};
			if (type != DistributionType.SimpleJoint && type != INVALID && !lengths.AllSame())
				type = INVALID;
			return type >= 0;
		}
		#endregion

		#region methods
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private unsafe void Fill1D<T>(IntPtr p, int n, IRandomDistribution distribution, DistributionType type) where T : unmanaged, INumber<T>
		{
			T shape1, shape2, scale, displace, mean, sigma;
			switch (type)
			{
				case DistributionType.Uniform:
					var uniform = (UniformDistribution<T>)distribution;
					T lb = uniform.LowerBound, ub = uniform.UpperBound;
					if (typeof(T) == typeof(float))
						NativeMethods.vsRngUniform(MklRngMethodUniform.Standard, this.Stream, n, p.AsF4(), lb.AsF4(), ub.AsF4()).Check();
					if (typeof(T) == typeof(double))
						NativeMethods.vdRngUniform(MklRngMethodUniform.Standard, this.Stream, n, p.AsF8(), lb.AsF8(), ub.AsF8()).Check();
					if (typeof(T) == typeof(int) || (typeof(T) == typeof(uint) && ub.AsU4() <= int.MaxValue))
						NativeMethods.viRngUniform(MklRngMethodUniform.Standard, this.Stream, n, p.AsI4(), lb.AsI4(), ub.AsI4()).Check();
					break;
				case DistributionType.RandomBits:
					if (sizeof(T) == sizeof(int))
						NativeMethods.viRngUniformBits32(MklRngMethodUniformBits.Standard, this.Stream, n, p.AsU4()).Check();
					if (sizeof(T) == sizeof(long))
						NativeMethods.viRngUniformBits64(MklRngMethodUniformBits.Standard, this.Stream, n, p.AsU8()).Check();
					break;

				case DistributionType.Beta:
					var beta = (BetaDistribution<T>)distribution;
					shape1 = beta.ShapeFactor; shape2 = beta.ShapeFactorOther; scale = beta.ScaleFactor; displace = beta.Displacement;
					if (typeof(T) == typeof(float))
						NativeMethods.vsRngBeta(MklRngMethodBeta.CJA, this.Stream, n, p.AsF4(), shape1.AsF4(), shape2.AsF4(), displace.AsF4(), scale.AsF4()).Check();
					if (typeof(T) == typeof(double))
						NativeMethods.vdRngBeta(MklRngMethodBeta.CJA, this.Stream, n, p.AsF8(), shape1.AsF8(), shape2.AsF8(), displace.AsF8(), scale.AsF8()).Check();
					break;
				case DistributionType.Cauchy:
					var cauchy = (CauchyDistribution<T>)distribution;
					scale = cauchy.ScaleFactor; displace = cauchy.Displacement;
					if (typeof(T) == typeof(float))
						NativeMethods.vsRngCauchy(MklRngMethodCauchy.ICDF, this.Stream, n, p.AsF4(), displace.AsF4(), scale.AsF4()).Check();
					if (typeof(T) == typeof(double))
						NativeMethods.vdRngCauchy(MklRngMethodCauchy.ICDF, this.Stream, n, p.AsF8(), displace.AsF8(), scale.AsF8()).Check();
					break;
				case DistributionType.ChiSquare:
					var chi2 = (ChiSquareDistribution<T>)distribution;
					if (typeof(T) == typeof(float))
						NativeMethods.vsRngChiSquare(MklRngMethodChiSquare.Chi2Gamma, this.Stream, n, p.AsF4(), chi2.DegreeOfFreedom).Check();
					if (typeof(T) == typeof(double))
						NativeMethods.vdRngChiSquare(MklRngMethodChiSquare.Chi2Gamma, this.Stream, n, p.AsF8(), chi2.DegreeOfFreedom).Check();
					break;
				case DistributionType.Exponential:
					var exp = (ExponentialDistribution<T>)distribution;
					scale = exp.ScaleFactor; displace = exp.Displacement;
					if (typeof(T) == typeof(float))
						NativeMethods.vsRngExponential(MklRngMethodExponential.ICDF, this.Stream, n, p.AsF4(), displace.AsF4(), scale.AsF4()).Check();
					if (typeof(T) == typeof(double))
						NativeMethods.vdRngExponential(MklRngMethodExponential.ICDF, this.Stream, n, p.AsF8(), displace.AsF8(), scale.AsF8()).Check();
					break;
				case DistributionType.Gamma:
					var gamma = (GammaDistribution<T>)distribution;
					shape1 = gamma.ShapeFactor; scale = gamma.ScaleFactor; displace = gamma.Displacement;
					if (typeof(T) == typeof(float))
						NativeMethods.vsRngGamma(MklRngMethodGamma.GNorm, this.Stream, n, p.AsF4(), shape1.AsF4(), displace.AsF4(), scale.AsF4()).Check();
					if (typeof(T) == typeof(double))
						NativeMethods.vdRngGamma(MklRngMethodGamma.GNorm, this.Stream, n, p.AsF8(), shape1.AsF8(), displace.AsF8(), scale.AsF8()).Check();
					break;
				case DistributionType.Gumbel:
					var gumbel = (GumbelDistribution<T>)distribution;
					scale = gumbel.ScaleFactor; displace = gumbel.Displacement;
					if (typeof(T) == typeof(float))
						NativeMethods.vsRngGumbel(MklRngMethodGumbel.ICDF, this.Stream, n, p.AsF4(), displace.AsF4(), scale.AsF4()).Check();
					if (typeof(T) == typeof(double))
						NativeMethods.vdRngGumbel(MklRngMethodGumbel.ICDF, this.Stream, n, p.AsF8(), displace.AsF8(), scale.AsF8()).Check();
					break;
				case DistributionType.Laplace:
					var laplace = (LaplaceDistribution<T>)distribution;
					scale = laplace.ScaleFactor; displace = laplace.Displacement;
					if (typeof(T) == typeof(float))
						NativeMethods.vsRngLaplace(MklRngMethodLaplace.ICDF, this.Stream, n, p.AsF4(), displace.AsF4(), scale.AsF4()).Check();
					if (typeof(T) == typeof(double))
						NativeMethods.vdRngLaplace(MklRngMethodLaplace.ICDF, this.Stream, n, p.AsF8(), displace.AsF8(), scale.AsF8()).Check();
					break;
				case DistributionType.LogNormal:
					var lognormal = (LogNormalDistribution<T>)distribution;
					scale = lognormal.ScaleFactor; displace = lognormal.Displacement; mean = lognormal.Mean; sigma = lognormal.StandardDeviation;
					if (typeof(T) == typeof(float))
						NativeMethods.vsRngLognormal(MklRngMethodLogNormal.BoxMuller2, this.Stream, n, p.AsF4(), mean.AsF4(), sigma.AsF4(), displace.AsF4(), scale.AsF4()).Check();
					if (typeof(T) == typeof(double))
						NativeMethods.vdRngLognormal(MklRngMethodLogNormal.BoxMuller2, this.Stream, n, p.AsF8(), mean.AsF4(), sigma.AsF4(), displace.AsF8(), scale.AsF8()).Check();
					break;
				case DistributionType.Normal:
					var normal = (NormalDistribution<T>)distribution;
					mean = normal.Mean; sigma = normal.StandardDeviation;
					if (typeof(T) == typeof(float))
						NativeMethods.vsRngGaussian(MklRngMethodGaussian.BoxMuller, this.Stream, n, p.AsF4(), mean.AsF4(), sigma.AsF4()).Check();
					if (typeof(T) == typeof(double))
						NativeMethods.vdRngGaussian(MklRngMethodGaussian.BoxMuller, this.Stream, n, p.AsF8(), mean.AsF4(), sigma.AsF4()).Check();
					break;
				case DistributionType.Rayleigh:
					var rayleigh = (RayleighDistribution<T>)distribution;
					scale = rayleigh.ScaleFactor; displace = rayleigh.Displacement;
					if (typeof(T) == typeof(float))
						NativeMethods.vsRngRayleigh(MklRngMethodRayleigh.ICDF, this.Stream, n, p.AsF4(), displace.AsF4(), scale.AsF4()).Check();
					if (typeof(T) == typeof(double))
						NativeMethods.vdRngRayleigh(MklRngMethodRayleigh.ICDF, this.Stream, n, p.AsF8(), displace.AsF8(), scale.AsF8()).Check();
					break;
				case DistributionType.Weibull:
					var weibull = (WeibullDistribution<T>)distribution;
					shape1 = weibull.ShapeFactor; scale = weibull.ScaleFactor; displace = weibull.Displacement;
					if (typeof(T) == typeof(float))
						NativeMethods.vsRngWeibull(MklRngMethodWeibull.ICDF, this.Stream, n, p.AsF4(), shape1.AsF4(), displace.AsF4(), scale.AsF4()).Check();
					if (typeof(T) == typeof(double))
						NativeMethods.vdRngWeibull(MklRngMethodWeibull.ICDF, this.Stream, n, p.AsF8(), shape1.AsF8(), displace.AsF8(), scale.AsF8()).Check();
					break;

				case DistributionType.Bernoulli:
					NativeMethods.viRngBernoulli(MklRngMethodBernoulli.ICDF, this.Stream, n, p.AsI4(), ((BernoulliDistribution<T>)distribution).Probability).Check();
					break;
				case DistributionType.Binomial:
					NativeMethods.viRngBinomial(MklRngMethodBinomial.BTPE, this.Stream, n, p.AsI4(), ((BinomialDistribution<T>)distribution).NTrials, ((BinomialDistribution<T>)distribution).Probability).Check();
					break;
				case DistributionType.NegativeBinomial:
					NativeMethods.viRngNegbinomial(MklRngMethodNegativeBinomial.NBar, this.Stream, n, p.AsI4(), ((NegativeBinomialDistribution<T>)distribution).SuccessCount, ((NegativeBinomialDistribution<T>)distribution).Probability).Check();
					break;
				case DistributionType.Geometric:
					NativeMethods.viRngGeometric(MklRngMethodGeometric.ICDF, this.Stream, n, p.AsI4(), ((GeometricDistribution<T>)distribution).Probability).Check();
					break;
				case DistributionType.Hypergeometric:
					var hyper = (HypergeometricDistribution<T>)distribution;
					NativeMethods.viRngHypergeometric(MklRngMethodHypergeometric.H2PE, this.Stream, n, p.AsI4(), hyper.LostSize, hyper.SampleSize, hyper.MarkSize).Check();
					break;
				case DistributionType.Poisson:
					NativeMethods.viRngPoisson(MklRngMethodPoisson.PTPE, this.Stream, n, p.AsI4(), ((PoissonDistribution<T>)distribution).Lambda).Check();
					break;

				default:
					break;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private unsafe void FillND(IRandomDistribution distribution, Span<IntPtr> pointers, int length)
		{
			void* p = Unsafe.AsPointer(ref pointers[0]);
			MklRngMatrixStorage storage = default; MklLapackInfo info;
			if (distribution is MultinomialDistribution<int> nomialI)
			{
				NativeMethods.viRngMultinomial(MklRngMethodMultinomial.MultiPoisson, this.Stream, length, (int**)p, nomialI.NTrials, nomialI.Rank, in nomialI.Probabilities[0]).Check();
			}
			else if (distribution is MultinomialDistribution<uint> nomialU)
			{
				NativeMethods.viRngMultinomial(MklRngMethodMultinomial.MultiPoisson, this.Stream, length, (int**)p, nomialU.NTrials, nomialU.Rank, in nomialU.Probabilities[0]).Check();
			}
			else if (distribution is MultiNormalDistribution<float> normalS)
			{
				ReadOnlySpan<float> matrix;
				int rank = normalS.Rank;
				switch (normalS.CovarianceStorage)
				{
					case MultiNormalDistribution<float>.StorageType.Original:
						storage = MklRngMatrixStorage.Full;
						// factorize
						float[] tempFull = new float[rank * rank];
						normalS.Covariance.CopyTo(tempFull);
						info = NativeMethods.LAPACKE_spotrf(MklMatrixLayout.ColMajor, MklFillModeChar.Upper, rank, tempFull, rank);
						Althea.LinearAlgebra.SolveMethodKind.Cholesky.CheckLapackInfo(info);
						matrix = tempFull;
						break;
					case MultiNormalDistribution<float>.StorageType.Diagonal:
						storage = MklRngMatrixStorage.Diagonal;
						// sqrt
						float[] tempDiag = new float[rank];
						normalS.Covariance.CopyTo(tempDiag, static c => MathF.Sqrt(c));
						matrix = tempDiag;
						break;
					case MultiNormalDistribution<float>.StorageType.CholeskyFull:
						storage = MklRngMatrixStorage.Full;
						matrix = normalS.Covariance;
						break;
					case MultiNormalDistribution<float>.StorageType.CholeskyDiagonal:
						storage = MklRngMatrixStorage.Diagonal;
						matrix = normalS.Covariance;
						break;
					default:
						matrix = default;
						break;
				}
				NativeMethods.vsRngGaussianMV(MklRngMethodGaussian.BoxMuller, this.Stream, length, (float**)p, rank, storage, in normalS.Mean[0], in matrix[0]);
			}
			else if (distribution is MultiNormalDistribution<double> normalD)
			{
				ReadOnlySpan<double> matrix;
				int rank = normalD.Rank;
				switch (normalD.CovarianceStorage)
				{
					case MultiNormalDistribution<double>.StorageType.Original:
						storage = MklRngMatrixStorage.Full;
						// factorize
						double[] tempFull = new double[rank * rank];
						normalD.Covariance.CopyTo(tempFull);
						info = NativeMethods.LAPACKE_dpotrf(MklMatrixLayout.ColMajor, MklFillModeChar.Upper, rank, tempFull, rank);
						Althea.LinearAlgebra.SolveMethodKind.Cholesky.CheckLapackInfo(info);
						matrix = tempFull;
						break;
					case MultiNormalDistribution<double>.StorageType.Diagonal:
						storage = MklRngMatrixStorage.Diagonal;
						// sqrt
						double[] tempDiag = new double[rank];
						normalD.Covariance.CopyTo(tempDiag, static c => Math.Sqrt(c));
						matrix = tempDiag;
						break;
					case MultiNormalDistribution<double>.StorageType.CholeskyFull:
						storage = MklRngMatrixStorage.Full;
						matrix = normalD.Covariance;
						break;
					case MultiNormalDistribution<double>.StorageType.CholeskyDiagonal:
						storage = MklRngMatrixStorage.Diagonal;
						matrix = normalD.Covariance;
						break;
					default:
						matrix = default;
						break;
				}
				NativeMethods.vdRngGaussianMV(MklRngMethodGaussian.BoxMuller, this.Stream, length, (double**)p, rank, storage, in normalD.Mean[0], in matrix[0]);
			}
		}

		protected override unsafe bool FillWithRandom_<T>(Storage<T> storage, IRandomDistribution distribution)
		{
			if (!Check(storage, distribution, out var p, out int n, out var type))
				return false;
			this.Fill1D<T>(p, n, distribution, type);
			return true;
		}

		protected override bool FillWithRandom_<T1, T2>(Storage<T1> storage1, Storage<T2> storage2, IRandomDistribution distribution)
		{
			Span<IStorage> storages = stackalloc IntPtr[2].AsClassType<IStorage>().SetValue(storage1, storage2);
			Span<IntPtr> pointers = stackalloc IntPtr[2];
			Span<int> lengths = stackalloc int[2];
			if (!Check(storages, distribution, pointers, lengths, out var type))
				return false;
			if (type == DistributionType.SimpleJoint)
			{
				var joint = (SimpleJointRandomDistribution)distribution;
				if (!Check(storage1, joint[0], out var p1, out var n1, out var type1))
					return false;
				if (!Check(storage2, joint[1], out var p2, out var n2, out var type2))
					return false;
				this.Fill1D<T1>(p1, n1, joint[0], type1);
				this.Fill1D<T2>(p2, n2, joint[1], type2);
				return true;
			}
			this.FillND(distribution, pointers, lengths[0]);
			return true;
		}

		protected override bool FillWithRandom_<T1, T2, T3>(Storage<T1> storage1, Storage<T2> storage2, Storage<T3> storage3, IRandomDistribution distribution)
		{
			Span<IStorage> storages = stackalloc IntPtr[3].AsClassType<IStorage>().SetValue(storage1, storage2, storage3);
			Span<IntPtr> pointers = stackalloc IntPtr[3];
			Span<int> lengths = stackalloc int[3];
			if (!Check(storages, distribution, pointers, lengths, out var type))
				return false;
			if (type == DistributionType.SimpleJoint)
			{
				var joint = (SimpleJointRandomDistribution)distribution;
				if (!Check(storage1, joint[0], out var p1, out var n1, out var type1))
					return false;
				if (!Check(storage2, joint[1], out var p2, out var n2, out var type2))
					return false;
				if (!Check(storage3, joint[2], out var p3, out var n3, out var type3))
					return false;
				this.Fill1D<T1>(p1, n1, joint[0], type1);
				this.Fill1D<T2>(p2, n2, joint[1], type2);
				this.Fill1D<T3>(p3, n3, joint[2], type3);
				return true;
			}
			this.FillND(distribution, pointers, lengths[0]);
			return true;
		}

		protected override bool FillWithRandom_(IRandomDistribution distribution, params IStorage[] storages)
		{
			Span<IntPtr> pointers = stackalloc IntPtr[storages.Length];
			Span<int> lengths = stackalloc int[storages.Length];
			if (!Check(storages, distribution, pointers, lengths, out var type))
				return false;
			if (type == DistributionType.SimpleJoint)
			{
				return false;
			}
			this.FillND(distribution, pointers, lengths[0]);
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
		internal static unsafe float AsF4<T>(this T v) where T : unmanaged, INumber<T> => *(float*)&v;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static unsafe double AsF8<T>(this T v) where T : unmanaged, INumber<T> => *(double*)&v;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static unsafe int AsI4<T>(this T v) where T : unmanaged, INumber<T> => *(int*)&v;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static unsafe uint AsU4<T>(this T v) where T : unmanaged, INumber<T> => *(uint*)&v;
	}
}
