using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Althea.Helpers;
using Althea.Linq;
using Althea.NativeTypes;


namespace Althea.Solver
{
	#region interface
	/// <summary>
	/// The interface of vector that contains the operation needed for Krylov-subspace methods such as Lanczos and Krylov-Schur solver.
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
	/// <typeparam name="TVec">The vector type</typeparam>
	public interface IKrylovVector<TVec, T> : IDisposable
		where TVec : class, IKrylovVector<TVec, T>, IDisposable, new()
		where T : unmanaged
	{
		/// <summary>
		/// The total presenting length of this vector
		/// </summary>
		long Length { get; }

		/// <summary>
		/// Create a new vector alike this one
		/// </summary>
		/// <returns>The new vector alike this one</returns>
		TVec NewArrayAlike();

		/// <summary>
		/// Fill this vector with the given <paramref name="value"/>
		/// </summary>
		/// <param name="value">The value to fill</param>
		void FillWith(T value);

		/// <summary>
		/// When implemented by a derived class, point-wisely in-place multiply this vector with given <paramref name="value"/>.
		/// </summary>
		/// <param name="value">The scalar as <typeparamref name="T"/> to multiply</param>
		void Scale(T value);

		/// <summary>
		/// When implemented by a derived class, compute the 2-norm (Euclidean norm) of elements in this vector.
		/// </summary>
		/// <returns>The 2-norm of this vector</returns>
		double Norm();

		/// <summary>
		/// When implemented by a derived class, in-place scale this vector such that its 2-norm (Euclidean norm) is one.
		/// </summary>
		/// <exception cref="DivideByZeroException">If the 2-norm of this array is 0</exception>
		void Normalize();

		/// <summary>
		/// When implemented by a derived class, compute dot (inner) product of this vector and <paramref name="other"/> vector. The conjugate of this vector shall be actually used.
		/// </summary>
		/// <param name="other">The other <typeparamref name="TVec"/> to perform the dot product</param>
		/// <returns>The dot (inner) product result as a <typeparamref name="T"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="other"/> is null or invalid</exception>
		T Dot(TVec other);

		/// <summary>
		/// When implemented by a derived class, add the <paramref name="other"/> (scaling by <paramref name="scalar"/>) to this vector in-place.
		/// </summary>
		/// <param name="other">The other <typeparamref name="TVec"/> to add</param>
		/// <param name="scalar">The scalar to be multiplied to <paramref name="other"/> of type <typeparamref name="T"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="other"/> is null or invalid</exception>
		void AddBy(TVec other, T scalar);

		/// <summary>
		/// When implemented by a derived class, replace this vector's content with the <paramref name="other"/> vector in-place.
		/// </summary>
		/// <param name="other">The other <typeparamref name="TVec"/> to replace from</param>
		/// <exception cref="ArgumentNullException">If <paramref name="other"/> is null or invalid</exception>
		/// <exception cref="InvalidOperationException">If the replacement cannot be done in-place due to reason(s) such as different sparsities between this and <paramref name="other"/></exception>
		void ReplaceBy(TVec other);

		/// <summary>
		/// When implemented by a derived class, multiply the matrix whose columns are indicated by <paramref name="unjoinedVectors"/> to a dense vector indicated by a <see cref="ReadOnlySpan{T}"/> and obtain the result vector as a <typeparamref name="TVec"/>.
		/// </summary>
		/// <param name="unjoinedVectors">The columns of the matrix to be multiplied</param>
		/// <param name="input">The input dense vector to be multiplied as a <see cref="ReadOnlySpan{T}"/></param>
		/// <returns>The product of <paramref name="unjoinedVectors"/> and <paramref name="input"/> as a <typeparamref name="TVec"/></returns>
		/// <remarks>The method shall be basically static, the information of this vector shall only be used to verify the consistency of <paramref name="unjoinedVectors"/></remarks>
		/// <exception cref="ArgumentNullException">If any of <paramref name="unjoinedVectors"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="input"/> and <paramref name="unjoinedVectors"/> have different size, or any element of <paramref name="unjoinedVectors"/> has different size than this vector</exception>
		TVec OperateOn(ReadOnlySpan<TVec> unjoinedVectors, ReadOnlySpan<T> input)
		{
			if (unjoinedVectors.IsEmpty)
				throw new ArgumentNullException(nameof(unjoinedVectors));
			if (input.IsEmpty)
				throw new ArgumentNullException(nameof(input));
			if (unjoinedVectors.Length != input.Length)
				throw new ArgumentException(Resources.Parameter.NotSameSize);

			// sort first to reduce errors
			int length = input.Length;
			Span<(T, IntPtr)> temp = length.CheckStackLimit<(T, IntPtr)>() ?? stackalloc (T, IntPtr)[length];
			Span<(T val, TVec vec)> values = MemoryMarshal.CreateSpan(ref Unsafe.As<(T, IntPtr), (T, TVec)>(ref temp[0]), length);
			Span<double> keys = length.CheckStackLimit<double>() ?? stackalloc double[length];
			TVec[] vectors = unjoinedVectors.ToArray();
			for (int i = 0; i < length; i++)
			{
				values[i] = (input[i], unjoinedVectors[i]);
				keys[i] = Const<T>.AbsoluteDelegate.Invoke(input[i]);
			}
			keys.Sort(values);

			long vecLen = this.Length;
			var vec = this.NewArrayAlike();
			try
			{
				vec.FillWith(default);
				for (int i = 0; i < length; i++)
				{
					var dnvec = values[i].vec;
					var val = values[i].val;
					if (dnvec is null)
						throw new ArgumentNullException(nameof(unjoinedVectors));
					if (dnvec.Length != vecLen)
						throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(unjoinedVectors));
					if (!val.IsZero())
						vec.AddBy(dnvec, val);
				}
				return vec;
			}
			catch (Exception)
			{
				vec.Dispose();
				throw;
			}
		}
	}
	#endregion

	#region Krylov subspace algorithms restart strategy
	/// <summary>
	/// The strategy adopted by the thick restart Krylov subspace algorithms.
	/// </summary>
	public enum RestartStrategy
	{
		/// <summary>
		/// The naïve strategy which only preserve the lowest Ritz eigen-pair and converged ones.
		/// </summary>
		Naive,
		// Ignore Spelling: \mathrm eig \left \right \underset
		/// <summary>
		/// Based on the index of Ritz eigen-pairs, preserve the smallest $k$ ones: <br/>
		/// $$k=n_c+\min{\left\{n_{\mathrm{eig}},\left(p-n_c\right)\left(\frac{2}{5}+\frac{n_{\mathrm{eig}}}{10p}\right)\right\}}$$
		/// </summary>
		//tex:$$k=n_c+\min{\left\{n_{\mathrm{eig}},\left(p-n_c\right)\left(\frac{2}{5}+\frac{n_{\mathrm{eig}}}{10p}\right)\right\}}$$
		IndexBased,
		/// <summary>
		/// Based on the residual of Ritz eigen-pairs, preserve the smallest $k$ ones: <br/>
		/// $$k=\underset{i}{\mathrm{argmax}}\left( \left| s_{n,i} \right| &lt; \max\left\{ \sqrt{\left| s_{n,n_{\mathrm{eig}}} \right|\max_{j}\left| s_{n,j} \right|},2\left| s_{n,n_{\mathrm{eig}}} \right| \right\} \right)$$
		/// </summary>
		//tex:$$k=\underset{i}{\mathrm{argmax}}\left( \left| s_{n,i} \right| < \max\left\{ \sqrt{\left| s_{n,n_{\mathrm{eig}}} \right|\max_{j}\left| s_{n,j} \right|},2\left| s_{n,n_{\mathrm{eig}}} \right| \right\} \right)$$
		CurrentResidualBest,
		/// <summary>
		/// Based on the improvement of residual of Ritz eigen-pairs of single iteration after the restart, preserve the smallest $k$ ones: <br/>
		/// $$k=\max{\left\{n_{\mathrm{eig}},\frac{3p+2n_c}{5}\right\}}$$
		/// </summary>
		//tex:$$k=\max{\left\{n_{\mathrm{eig}},\frac{3p+2n_c}{5}\right\}}$$
		OneStepResidualImprove,
		/// <summary>
		/// Based on the improvement of residual of Ritz eigen-pairs of single iteration after the restart, preserve the smallest $k$ ones: <br/>
		/// $$k=\underset{k}{\max}{\left(p-k\right)\frac{\lambda_{k+1}-\lambda_1}{\lambda_m-\lambda_1}}$$
		/// </summary>
		//tex:$$k=\underset{k}{\max}{\left(p-k\right)\frac{\lambda_{k+1}-\lambda_1}{\lambda_m-\lambda_1}}$$
		WholeIterResidualImprove,
		/// <summary>
		/// The heuristic used by Krylov-Schur algorithm to prevent stagnating
		/// </summary>
		KrylovSchur,
		/// <summary>
		/// User-defined restart strategy, see <see cref="IRestartStrategy"/>
		/// </summary>
		UserDefine,
	}

	/// <summary>
	/// The interface for a user-defined (or a built in) restart strategy
	/// </summary>
	public interface IRestartStrategy
	{
		/// <summary>
		/// When implemented by a derived class, compute which Ritz pairs to preserve according to the current restart strategy.
		/// </summary>
		/// <param name="estimateEigvals">The Ritz values, without converged ones</param>
		/// <param name="estimateEigvecs">The Ritz vectors, without converged ones. This shall be a square matrix.</param>
		/// <param name="nConverged">THe number of converged eigen-pairs</param>
		/// <param name="nTarget">The number of smallest eigen-pairs wanted</param>
		/// <param name="maxIter">The maximum number of iteration</param>
		/// <param name="output">The span used to put the result indices: preserve <paramref name="estimateEigvals"/>[<paramref name="output"/>] and <paramref name="estimateEigvecs"/>[<paramref name="estimateEigvecs"/>]</param>
		/// <returns><paramref name="output"/>[..preserved_count]</returns>
		/// <remarks>This method will only be invoked internally.</remarks>
		Span<int> PreserveSelect(Span<Complex<double>> estimateEigvals, Span<Complex<double>> estimateEigvecs, int nConverged, int nTarget, int maxIter, Span<int> output);
	}

	/// <summary>
	/// The built-in <see cref="IRestartStrategy"/> that implements the built-in <see cref="RestartStrategy"/>s.
	/// </summary>
	public sealed class BuiltInRestartStrategy : IRestartStrategy
	{
		private readonly RestartStrategy strategy;

		/// <summary>
		/// Create a <see cref="BuiltInRestartStrategy"/> with given <paramref name="strategy"/>
		/// </summary>
		/// <param name="strategy"></param>
		public BuiltInRestartStrategy(RestartStrategy strategy)
		{
			this.strategy = strategy;
		}

		Span<int> IRestartStrategy.PreserveSelect(Span<Complex<double>> estimateEigvals, Span<Complex<double>> estimateEigvecs, int nConverged, int nTarget, int maxIter, Span<int> output)
		{
			int indexMax = 0;
			int upperCount = estimateEigvals.Length * 2 / 3;
			switch (this.strategy)
			{
				case RestartStrategy.Naive:
					return ArrayLinq.Range(0, Math.Min(Math.Max(maxIter * 2 / 5, nTarget), estimateEigvals.Length)).ToArray();
				case RestartStrategy.IndexBased:
					indexMax = Math.Min(nTarget, (int)((maxIter - nConverged) * (0.4 + nTarget / 10.0 / maxIter)));
					return ArrayLinq.Range(0, Math.Min(indexMax, upperCount)).ToArray();
				case RestartStrategy.CurrentResidualBest:
					if (nTarget >= upperCount)
						return ArrayLinq.Range(0, upperCount).ToArray();
					var lastRow = estimateEigvecs.Select(v => v[^1]).ToArray();
					var lastMax = lastRow.Max(a => a.Abs());
					var lastNeig = lastRow[nTarget - 1].Abs();
					var upperBound = Math.Max(Math.Sqrt(lastMax * lastNeig), 2 * lastNeig);
					for (indexMax = 0; indexMax < upperCount; indexMax++)
					{
						if (lastRow[indexMax].Abs() >= upperBound)
							break;
					}
					indexMax -= nConverged;
					return ArrayLinq.Range(0, indexMax - 1).ToArray();
				case RestartStrategy.OneStepResidualImprove:
					indexMax = Math.Max(nTarget, (int)(0.6 * maxIter + 0.4 * nConverged));
					indexMax -= nConverged;
					return ArrayLinq.Range(0, Math.Min(indexMax, upperCount)).ToArray();
				case RestartStrategy.WholeIterResidualImprove:
					upperCount = Math.Max(nTarget, (int)(0.6 * maxIter + 0.4 * nConverged));
					double maxVal = 0;
					for (int i = 0; i < upperCount; i++)
					{
						double val = (maxIter - i - 1) * (estimateEigvals[i + 1].Abs() - estimateEigvals[0].Abs()) / (estimateEigvals[^1].Abs() - estimateEigvals[0].Abs());
						if (val > maxVal)
						{
							maxVal = val;
							indexMax = i;
						}
					}
					indexMax -= nConverged;
					return ArrayLinq.Range(0, indexMax).ToArray();
				case RestartStrategy.KrylovSchur:
					int k = nTarget + Math.Min(nConverged, (maxIter - nTarget) / 2);
					if (k == 1 && maxIter > 3)
						k = maxIter / 2;
					return ArrayLinq.Range(0, k).ToArray();
				default:
					throw new NotSupportedException();
			}
		}
	}
	#endregion
}
