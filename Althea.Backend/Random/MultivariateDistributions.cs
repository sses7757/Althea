using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

using Althea.Backend.Storage;
using Althea.Helpers;
using Althea.Linq;
using Althea.NativeTypes;
using Althea.Random;


namespace Althea.Backend.Random
{
	// Ignore Spelling: \det
	/// <summary>
	/// The class for a multi-dimensional normal distribution of type <typeparamref name="T"/>, implements <see cref="RealTypedDistribution{T}"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged floating point type</typeparam>
	//tex:Multi-dimensional normal distribution PDF:
	//$$P_{\vec\mu,\Sigma}(\vec x) = \frac{1}{(2\pi)^{D/2}}\frac{1}{\sqrt{\det(\Sigma)}}
	//\exp{\left( -\frac12(\vec x - \vec \mu)^T \Sigma^{-1} (\vec x - \vec \mu) \right)}$$
	//where $D$ is the number of dimensions, $\vec\mu$ is the mean values of all dimensions, $\Sigma$ is the covariance matrix (which is symmetric-definite).
	public class MultiNormalDistribution<T> : RealTypedDistribution<T>, IEquatable<MultiNormalDistribution<T>> where T : unmanaged, INumber<T>
	{
		private readonly T[] mean, covariance;

		/// <summary>
		/// Get the rank / number of dimensions of this <see cref="MultiNormalDistribution{T}"/>
		/// </summary>
		public int Rank {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.mean.Length;
		}

		/// <summary>
		/// Get the mean values of this <see cref="MultiNormalDistribution{T}"/>
		/// </summary>
		public ReadOnlySpan<T> Mean {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.mean;
		}

		/// <summary>
		/// Get the <see cref="StorageType"/> indicating how the <see cref="Covariance"/> is being stored
		/// </summary>
		public StorageType CovarianceStorage {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
		}

		/// <summary>
		/// Get the covariance matrix (or one of its equivalent storage) of this <see cref="MultiNormalDistribution{T}"/>
		/// </summary>
		public ReadOnlySpan<T> Covariance {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.covariance;
		}

		/// <summary>
		/// Indicates how the <see cref="Covariance"/> is stored in the underlying array
		/// </summary>
		public enum StorageType
		{
			/// <summary>
			/// The whole covariance matrix is stored (row or column major is irrelevant due to the symmetry); the length of <see cref="Covariance"/> is <see cref="Rank"/> * <see cref="Rank"/>
			/// </summary>
			Full,
			/// <summary>
			/// The covariance matrix is a diagonal matrix and only the diagonal elements are stored; the length of <see cref="Covariance"/> is <see cref="Rank"/>
			/// </summary>
			Diagonal,
			/// <summary>
			/// The Cholesky factorization of the covariance matrix is stored; the length of <see cref="Covariance"/> is <see cref="Rank"/> * <see cref="Rank"/>
			/// </summary>
			CholeskyFull,
			/// <summary>
			/// The covariance matrix is a diagonal matrix and only the square roots of the diagonal elements are stored; the length of <see cref="Covariance"/> is <see cref="Rank"/>
			/// </summary>
			CholeskyDiagonal,
		}

		/// <summary>
		/// Create a standard <see cref="MultiNormalDistribution{T}"/> with means = 0 and covariance = identity, and random seed is not set
		/// </summary>
		/// <param name="rank">The number of dimensions</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="rank"/> is less than 2</exception>
		public MultiNormalDistribution(int rank) : base(null)
		{
			if (rank < 2)
				throw new ArgumentOutOfRangeException(nameof(rank), rank, Resources.Parameter.InvalidValue);
			this.mean = new T[rank];
			this.CovarianceStorage = StorageType.Diagonal;
			this.covariance = new T[rank];
			((Span<T>)this.covariance).Fill(Const<T>.One);
		}

		/// <summary>
		/// Create a <see cref="MultiNormalDistribution{T}"/> with given <paramref name="mean"/>, <paramref name="covar"/> and the random <paramref name="seed"/>
		/// </summary>
		/// <param name="mean">The given mean values</param>
		/// <param name="storageType">The <see cref="StorageType"/> of <paramref name="covar"/></param>
		/// <param name="covar">The given covariance matrix</param>
		/// <param name="seed">The given random seed, default null means has no preferred random seed</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="storageType"/> is out of range</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="mean"/> or <paramref name="covar"/> is null or empty</exception>
		/// <exception cref="ArgumentException">If <paramref name="mean"/> and <paramref name="covar"/> have incompatible size, or indicate a rank less than 2</exception>
		public MultiNormalDistribution(ReadOnlySpan<T> mean, StorageType storageType, ReadOnlySpan<T> covar, long? seed = null) : base(seed)
		{
			if (storageType < StorageType.Full || storageType > StorageType.CholeskyDiagonal)
				throw new ArgumentOutOfRangeException(nameof(storageType), storageType, Resources.Parameter.InvalidValue);
			if (mean.IsEmpty)
				throw new ArgumentNullException(nameof(mean));
			if (covar.IsEmpty)
				throw new ArgumentNullException(nameof(covar));
			if ((storageType == StorageType.CholeskyDiagonal || storageType == StorageType.Diagonal) && mean.Length != covar.Length)
				throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(covar));
			if ((storageType == StorageType.CholeskyFull || storageType == StorageType.Full) && mean.Length * mean.Length != covar.Length)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(covar));
			if (mean.Length < 2)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(mean));

			this.mean = mean.ToArray();
			this.covariance = covar.ToArray();
			this.CovarianceStorage = storageType;
		}

		/// <summary>
		/// Get the <see cref="DataType"/> of <typeparamref name="T"/>
		/// </summary>
		/// <param name="index">The index, must be 0</param>
		/// <returns>The <see cref="DataType"/> of <typeparamref name="T"/></returns>
		public override DataType this[int index] {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				if (index < 0 || index >= this.Rank)
					throw new ArgumentOutOfRangeException(nameof(index), index, Resources.Parameter.InvalidValue);
				return Const<T>.DataType;
			}
		}

		/// <summary>
		/// Same as <see cref="Rank"/>
		/// </summary>
		public override int Count {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.Rank;
		}

		/// <summary>
		/// Get the enumerator of the <see cref="DataType"/> of the dimensions of this distribution
		/// </summary>
		public override IEnumerator<DataType> GetEnumerator()
		{
			for (int i = 0; i < this.Rank; i++)
			{
				yield return Const<T>.DataType;
			}
		}

		/// <summary>
		/// Check whether the <paramref name="other"/> <see cref="MultiNormalDistribution{T}"/> represents the same distribution as this one
		/// </summary>
		/// <param name="other">The other <see cref="MultiNormalDistribution{T}"/> to compare</param>
		/// <returns>this == <paramref name="other"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe bool Equals(MultiNormalDistribution<T>? other)
		{
			if (other is null)
				return false;
			if (ReferenceEquals(this, other))
				return true;
			if (this.Rank != other.Rank || this.CovarianceStorage != other.CovarianceStorage || this.RandomSeed != other.RandomSeed)
				return false;
			int length = this.CovarianceStorage == StorageType.Full || this.CovarianceStorage == StorageType.CholeskyFull ? this.Rank * this.Rank : this.Rank;
			fixed (T* m1 = this.mean, m2 = other.mean, c1 = this.covariance, c2 = other.covariance)
			{
				CSharp.LinearAlgebra.Api.PointWiseEquals(new ManagedPureStorage<T>(m1, this.Rank), new ManagedPureStorage<T>(m2, this.Rank), out bool equals);
				if (!equals)
					return false;
				CSharp.LinearAlgebra.Api.PointWiseEquals(new ManagedPureStorage<T>(c1, length), new ManagedPureStorage<T>(c2, length), out equals);
				return equals;
			}
		}

		/// <summary>
		/// Check whether the given <paramref name="obj"/> represents the same distribution as this one
		/// </summary>
		/// <param name="obj">The other <see cref="object"/> to compare</param>
		/// <returns>this == <paramref name="obj"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override bool Equals(object? obj)
		{
			return this.Equals(obj as MultiNormalDistribution<T>);
		}

		/// <summary>
		/// Get the hash code of this <see cref="MultiNormalDistribution{T}"/>
		/// </summary>
		/// <returns>The hash code of this <see cref="MultiNormalDistribution{T}"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode()
		{
			return HashCode.Combine(this.Rank, this.CovarianceStorage, this.Mean.HashCodeOfSpan(), this.Covariance.HashCodeOfSpan(), this.RandomSeed);
		}

		/// <summary>
		/// Get the string representation of the property-value pairs of this <see cref="MultiNormalDistribution{T}"/>
		/// </summary>
		protected override string PropertiesString {
			get {
				string b = base.PropertiesString;
				string means = $", {nameof(this.Mean)}={{{this.Mean.SpanJoin(',')}}}, ";
				int n = this.Rank;
				string covarName = nameof(this.Covariance) + "=";
				string prefix = new(' ', covarName.Length);
				string covar = this.CovarianceStorage switch
				{
					StorageType.Full => this.Covariance.ToMatrixString(n, prefix: prefix)[prefix.Length..],
					StorageType.Diagonal => $"diag{{{this.Covariance.SpanJoin(',')}}}",
					StorageType.CholeskyFull => ((ReadOnlySpan<T>)ExtensionHelper.MatrixMultiply(n, n, n, this.Covariance, this.Covariance, transRight: true)).ToMatrixString(n, prefix: prefix)[prefix.Length..],
					StorageType.CholeskyDiagonal => $"diag{{{string.Join(',', System.Linq.Enumerable.Select(this.covariance, static c => c.NativeMultiply(c)))}}}",
					_ => string.Empty,
				};
				return b + means + covarName + covar;
			}
		}
	}

	/// <summary>
	/// The class for a multinomial distribution of type <typeparamref name="T"/>, implements <see cref="IntegerTypedDistribution{T}"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged integral type</typeparam>
	//tex:Multinomial normal distribution PDF:
	//$$P_{m,\vec p}(\vec k) = \frac{m!}{\prod_i{k_i}} \prod_i{p^{k_i}}$$
	public class MultinomialDistribution<T> : IntegerTypedDistribution<T>, IEquatable<MultinomialDistribution<T>> where T : unmanaged, INumber<T>
	{
		private readonly double[] probabilities;

		/// <summary>
		/// Get the rank / number of dimensions of this <see cref="MultinomialDistribution{T}"/>
		/// </summary>
		public int Rank {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.probabilities.Length;
		}

		/// <summary>
		/// Get the probabilities of this <see cref="MultinomialDistribution{T}"/>
		/// </summary>
		public ReadOnlySpan<double> Probabilities {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.probabilities;
		}

		/// <summary>
		/// Get the total number of trials of this <see cref="MultinomialDistribution{T}"/>
		/// </summary>
		public int NTrials {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
		}

		/// <summary>
		/// Create a <see cref="MultinomialDistribution{T}"/> with given <paramref name="probabilities"/>, <paramref name="nTrials"/> and the random <paramref name="seed"/>
		/// </summary>
		/// <param name="probabilities">The given probabilities</param>
		/// <param name="nTrials">The given total number of trials</param>
		/// <param name="seed">The given random seed, default null means has no preferred random seed</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="nTrials"/> is less than 1</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="probabilities"/> is null or empty</exception>
		/// <exception cref="ArgumentException">If <paramref name="probabilities"/>'s sum is not 1, or it indicates a rank less than 2</exception>
		public MultinomialDistribution(ReadOnlySpan<double> probabilities, int nTrials, long? seed = null) : base(seed)
		{
			if (nTrials <= 0)
				throw new ArgumentOutOfRangeException(nameof(nTrials), nTrials, Resources.Parameter.MustPositive);
			if (probabilities.IsEmpty)
				throw new ArgumentNullException(nameof(probabilities));
			if (probabilities.Length < 2)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(probabilities));
			if (probabilities.Sum() != 1)
				throw new ArgumentException(Resources.Parameter.InvalidValue, nameof(probabilities));

			this.probabilities = probabilities.ToArray();
			this.NTrials = nTrials;
		}

		/// <summary>
		/// Get the <see cref="DataType"/> of <typeparamref name="T"/>
		/// </summary>
		/// <param name="index">The index, must be 0</param>
		/// <returns>The <see cref="DataType"/> of <typeparamref name="T"/></returns>
		public override DataType this[int index] {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				if (index < 0 || index >= this.Rank)
					throw new ArgumentOutOfRangeException(nameof(index), index, Resources.Parameter.InvalidValue);
				return Const<T>.DataType;
			}
		}

		/// <summary>
		/// Same as <see cref="Rank"/>
		/// </summary>
		public override int Count {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.Rank;
		}

		/// <summary>
		/// Get the enumerator of the <see cref="DataType"/> of the dimensions of this distribution
		/// </summary>
		public override IEnumerator<DataType> GetEnumerator()
		{
			for (int i = 0; i < this.Rank; i++)
			{
				yield return Const<T>.DataType;
			}
		}

		/// <summary>
		/// Check whether the <paramref name="other"/> <see cref="MultinomialDistribution{T}"/> represents the same distribution as this one
		/// </summary>
		/// <param name="other">The other <see cref="MultinomialDistribution{T}"/> to compare</param>
		/// <returns>this == <paramref name="other"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe bool Equals(MultinomialDistribution<T>? other)
		{
			if (other is null)
				return false;
			if (ReferenceEquals(this, other))
				return true;
			if (this.Rank != other.Rank || this.NTrials != other.NTrials || this.RandomSeed != other.RandomSeed)
				return false;
			return this.Probabilities.SequenceEqual(other.Probabilities);
		}

		/// <summary>
		/// Check whether the given <paramref name="obj"/> represents the same distribution as this one
		/// </summary>
		/// <param name="obj">The other <see cref="object"/> to compare</param>
		/// <returns>this == <paramref name="obj"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override bool Equals(object? obj)
		{
			return this.Equals(obj as MultinomialDistribution<T>);
		}

		/// <summary>
		/// Get the hash code of this <see cref="MultinomialDistribution{T}"/>
		/// </summary>
		/// <returns>The hash code of this <see cref="MultinomialDistribution{T}"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode()
		{
			return HashCode.Combine(this.Rank, this.NTrials, this.Probabilities.HashCodeOfSpan(), this.RandomSeed);
		}

		/// <summary>
		/// Get the string representation of the property-value pairs of this <see cref="MultinomialDistribution{T}"/>
		/// </summary>
		protected override string PropertiesString => base.PropertiesString + $", {nameof(this.NTrials)}={this.NTrials}, {nameof(this.Probabilities)}={{{this.Probabilities.SpanJoin(',')}}}";
	}
}
