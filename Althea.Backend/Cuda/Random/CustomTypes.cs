using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using Althea.Helpers;
using Althea.NativeTypes;
using Althea.Random;


namespace Althea.Backend.Cuda.Random
{
	#region distributions
	/// <summary>
	/// The class for a one-dimensional (log) normal distribution of type <typeparamref name="T"/>, implements <see cref="IRandomDistribution"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged floating point type</typeparam>
	public class NormalOrLogNormalDistribution<T> : IRandomDistribution, IEquatable<NormalOrLogNormalDistribution<T>> where T : unmanaged
	{
		/// <summary>
		/// Get the mean value of this (log) normal distribution
		/// </summary>
		public T Mean {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
		}

		/// <summary>
		/// Get the standard deviation value of this (log) normal distribution
		/// </summary>
		public T StandardDeviation {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
		}

		/// <summary>
		/// Get the desired random seed of this distribution
		/// </summary>
		public long? RandomSeed {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
		} = null;

		/// <summary>
		/// Get a <see cref="bool"/> indicating whether this is a log normal or simply normal distribution
		/// </summary>
		public bool IsLogNormal {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
		} = false;

		/// <summary>
		/// Create a standard normal distribution with mean = 0 and standard deviation = 1, and random seed is not set
		/// </summary>
		public NormalOrLogNormalDistribution()
		{
			this.Mean = Const<T>.Zero; this.StandardDeviation = Const<T>.One;
		}

		/// <summary>
		/// Create a (log) normal distribution with given <paramref name="mean"/>, <paramref name="stddev"/> and the random <paramref name="seed"/>
		/// </summary>
		/// <param name="mean">The given mean value</param>
		/// <param name="stddev">The given standard deviation</param>
		/// <param name="logNormal">Whether creating a log normal or simply normal distribution</param>
		/// <param name="seed">The given random seed, default null means has no preferred random seed</param>
		public NormalOrLogNormalDistribution(T mean, T stddev, bool logNormal = false, long? seed = null)
		{
			this.StandardDeviation = stddev; this.Mean = mean; this.IsLogNormal = logNormal; this.RandomSeed = seed;
		}

		/// <summary>
		/// Get the <see cref="DataType"/> of <typeparamref name="T"/>
		/// </summary>
		/// <param name="index">The index, must be 0</param>
		/// <returns>The <see cref="DataType"/> of <typeparamref name="T"/></returns>
		public DataType this[int index] {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				if (index != 0)
					throw new ArgumentOutOfRangeException(nameof(index), index, Resources.Parameter.InvalidValue);
				return Const<T>.DataType;
			}
		}

		/// <summary>
		/// Get the rank / number of dimensions of this distribution, always 1
		/// </summary>
		public int Count {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => 1;
		}

		/// <summary>
		/// Get the enumerator of the <see cref="DataType"/> of the dimensions of this distribution
		/// </summary>
		public IEnumerator<DataType> GetEnumerator()
		{
			yield return Const<T>.DataType;
		}

		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

		/// <summary>
		/// Check whether the <paramref name="other"/> <see cref="NormalOrLogNormalDistribution{T}"/> represents the same distribution as this one
		/// </summary>
		/// <param name="other">The other <see cref="NormalOrLogNormalDistribution{T}"/> to compare</param>
		/// <returns>this == <paramref name="other"/></returns>
		public bool Equals(NormalOrLogNormalDistribution<T>? other)
		{
			if (other is null)
				return false;
			if (ReferenceEquals(this, other))
				return true;
			return this.IsLogNormal == other.IsLogNormal && this.Mean.IsEqual(other.Mean) && this.StandardDeviation.IsEqual(other.StandardDeviation) && this.RandomSeed == other.RandomSeed;
		}

		/// <summary>
		/// Check whether the given <paramref name="obj"/> represents the same distribution as this one
		/// </summary>
		/// <param name="obj">The other <see cref="object"/> to compare</param>
		/// <returns>this == <paramref name="obj"/></returns>
		public override bool Equals(object? obj)
		{
			return this.Equals(obj as NormalOrLogNormalDistribution<T>);
		}

		/// <summary>
		/// Get the hash code of this <see cref="NormalOrLogNormalDistribution{T}"/>
		/// </summary>
		/// <returns>The hash code of this <see cref="NormalOrLogNormalDistribution{T}"/></returns>
		public override int GetHashCode()
		{
			return HashCode.Combine(this.IsLogNormal, this.Mean, this.StandardDeviation, this.RandomSeed);
		}

		/// <summary>
		/// Get the string representation of this <see cref="NormalOrLogNormalDistribution{T}"/>
		/// </summary>
		/// <returns>The string representation of this <see cref="NormalOrLogNormalDistribution{T}"/></returns>
		public override string ToString()
		{
			return (this.IsLogNormal ? "Log" : "") + nameof(NormalOrLogNormalDistribution<T>) + $"[DataType={typeof(T).GetGenericString()}, {nameof(this.Mean)}={this.Mean}, {nameof(this.StandardDeviation)}={this.StandardDeviation}" + (this.RandomSeed.HasValue ? $", {nameof(this.RandomSeed)}={this.RandomSeed}]" : "]");
		}
	}


	/// <summary>
	/// The class for a one-dimensional Poisson distribution of type <typeparamref name="T"/>, implements <see cref="IRandomDistribution"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged integral type</typeparam>
	public class PoissonDistribution<T> : IRandomDistribution, IEquatable<PoissonDistribution<T>> where T : unmanaged
	{
		/// <summary>
		/// Get the λ value of this Poisson distribution
		/// </summary>
		public double Lambda {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
		}

		/// <summary>
		/// Get the desired random seed of this distribution
		/// </summary>
		public long? RandomSeed {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
		} = null;

		/// <summary>
		/// Create a Poisson distribution with given <paramref name="lambda"/> and the random <paramref name="seed"/>
		/// </summary>
		/// <param name="lambda">The given λ value</param>
		/// <param name="seed">The given random seed, default null means has no preferred random seed</param>
		public PoissonDistribution(double lambda, long? seed = null)
		{
			this.Lambda = lambda; this.RandomSeed = seed;
		}

		/// <summary>
		/// Get the <see cref="DataType"/> of <typeparamref name="T"/>
		/// </summary>
		/// <param name="index">The index, must be 0</param>
		/// <returns>The <see cref="DataType"/> of <typeparamref name="T"/></returns>
		public DataType this[int index] {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				if (index != 0)
					throw new ArgumentOutOfRangeException(nameof(index), index, Resources.Parameter.InvalidValue);
				return Const<T>.DataType;
			}
		}

		/// <summary>
		/// Get the rank / number of dimensions of this distribution, always 1
		/// </summary>
		public int Count {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => 1;
		}

		/// <summary>
		/// Get the enumerator of the <see cref="DataType"/> of the dimensions of this distribution
		/// </summary>
		public IEnumerator<DataType> GetEnumerator()
		{
			yield return Const<T>.DataType;
		}

		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

		/// <summary>
		/// Check whether the <paramref name="other"/> <see cref="PoissonDistribution{T}"/> represents the same distribution as this one
		/// </summary>
		/// <param name="other">The other <see cref="PoissonDistribution{T}"/> to compare</param>
		/// <returns>this == <paramref name="other"/></returns>
		public bool Equals(PoissonDistribution<T>? other)
		{
			if (other is null)
				return false;
			if (ReferenceEquals(this, other))
				return true;
			return this.Lambda.IsEqual(other.Lambda) && this.RandomSeed == other.RandomSeed;
		}

		/// <summary>
		/// Check whether the given <paramref name="obj"/> represents the same distribution as this one
		/// </summary>
		/// <param name="obj">The other <see cref="object"/> to compare</param>
		/// <returns>this == <paramref name="obj"/></returns>
		public override bool Equals(object? obj)
		{
			return this.Equals(obj as PoissonDistribution<T>);
		}

		/// <summary>
		/// Get the hash code of this <see cref="PoissonDistribution{T}"/>
		/// </summary>
		/// <returns>The hash code of this <see cref="PoissonDistribution{T}"/></returns>
		public override int GetHashCode()
		{
			return HashCode.Combine(this.Lambda, this.RandomSeed);
		}

		/// <summary>
		/// Get the string representation of this <see cref="PoissonDistribution{T}"/>
		/// </summary>
		/// <returns>The string representation of this <see cref="PoissonDistribution{T}"/></returns>
		public override string ToString()
		{
			return nameof(PoissonDistribution<T>) + $"[DataType={typeof(T).GetGenericString()}, {nameof(this.Lambda)}={this.Lambda}" + (this.RandomSeed.HasValue ? $", {nameof(this.RandomSeed)}={this.RandomSeed}]" : "]");
		}
	}
	#endregion

	#region error
	/// <summary>
	/// The returned status of CUDA Random API calls
	/// </summary>
	public enum CudaRandomError
	{
		/// <summary>
		/// No errors.
		/// </summary>
		Success = 0,
		/// <summary>
		/// Header file and linked library version do not match.
		/// </summary>
		VersionMismatch = 100,
		/// <summary>
		/// Generator not initialized.
		/// </summary>
		NotInitialized = 101,
		/// <summary>
		/// Memory allocation failed.
		/// </summary>
		AllocationFailed = 102,
		/// <summary>
		/// Generator is wrong type.
		/// </summary>
		TypeError = 103,
		/// <summary>
		/// Argument out of range.
		/// </summary>
		OutOfRange = 104,
		/// <summary>
		/// Length requested is not a multiple of dimension.
		/// </summary>
		LengthNotMultiple = 105,
		/// <summary>
		/// GPU does not have double precision required by MRG32k3a.
		/// </summary>
		DoublePrecisionRequired = 106,
		/// <summary>
		/// Kernel launch failure.
		/// </summary>
		LaunchFailure = 201,
		/// <summary>
		/// Preexisting failure on library entry.
		/// </summary>
		PreexistingFailure = 202,
		/// <summary>
		/// Initialization of CUDA failed.
		/// </summary>
		InitializationFailed = 203,
		/// <summary>
		/// Architecture mismatch, GPU does not support requested feature.
		/// </summary>
		ArchMismatch = 204,
		/// <summary>
		/// Internal library error.
		/// </summary>
		InternalError = 999
	}

	/// <summary>
	/// The static class containing extension methods for <see cref="CudaRandomError"/>
	/// </summary>
	public static partial class StatusExtension
	{
		/// <summary>
		/// Check whether the input <see cref="CudaRandomError"/> is success or not and throw exception if it is not
		/// </summary>
		/// <param name="err">The <see cref="CudaRandomError"/> to be checked</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Check(this CudaRandomError err)
		{
			if (err != CudaRandomError.Success)
			{
				throw new StatusException(err, new StackTrace(0));
			}
		}
	}
	#endregion

	#region other enum
	/// <summary>
	/// The CUDA Random generator types
	/// </summary>
	internal enum GeneratorType
	{
		/// <summary>
		/// 
		/// </summary>
		Test = 0,
		/// <summary>
		/// Default pseudo-random generator.
		/// </summary>
		PseudoDefault = 100,
		/// <summary>
		/// XORWOW pseudo-random generator.
		/// </summary>
		PseudoXORWOW = 101,
		/// <summary>
		/// MRG32k3a pseudo-random generator.
		/// </summary>
		PseudoMRG32K3A = 121,
		/// <summary>
		/// Mersenne Twister pseudo-random generator.
		/// </summary>
		PseudoMTGP32 = 141,
		/// <summary>
		/// Mersenne Twister MT19937 pseudo-random generator.
		/// </summary>
		PseudoMT19937 = 142,
		/// <summary>
		/// PseudoPhilox4_32_10 quasi-random generator.
		/// </summary>
		PseudoPhilox4_32_10 = 161,
		/// <summary>
		/// Default quasi-random generator.
		/// </summary>
		QuasiDefault = 200,
		/// <summary>
		/// Sobol32 quasi-random generator.
		/// </summary>
		QuasiSobol32 = 201,
		/// <summary>
		/// Scrambled Sobol32 quasi-random generator.
		/// </summary>
		QuasiScrambledSobol32 = 202,
		/// <summary>
		/// Sobol64 quasi-random generator.
		/// </summary>
		QuasiSobol64 = 203,
		/// <summary>
		/// Scrambled Sobol64 quasi-random generator.
		/// </summary>
		QuasiScrambledSobol64 = 204
	}

	/// <summary>
	/// The CUDA Random orderings of results in memory
	/// </summary>
	internal enum Ordering
	{
		/// <summary>
		/// Best ordering for pseudo-random results.
		/// </summary>
		PseudoBest = 100,
		/// <summary>
		/// Specific default 4096 thread sequence for pseudo-random results.
		/// </summary>
		PseudoDefault = 101,
		/// <summary>
		/// Specific seeding pattern for fast lower quality pseudo-random results.
		/// </summary>
		Pseudoeseded = 102,
		/// <summary>
		/// Specific n-dimensional ordering for quasi-random results.
		/// </summary>
		QuasiDefault = 201
	}
	#endregion
}
