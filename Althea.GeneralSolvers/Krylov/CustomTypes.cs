using System;
using System.Runtime.CompilerServices;

using Althea.Helpers;
using Althea.Numerics;


namespace Althea.GeneralSolvers.Krylov;

#region interface
/// <summary>
/// The interface of vector that contains the operation needed for Krylov-subspace methods such as Lanczos and Krylov-Schur solver.
/// </summary>
/// <typeparam name="TVec">The concrete vector type</typeparam>
/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
public interface IKrylovVector<T, TVec> : ICreateAlike<TVec>, IDisposable
	where TVec : class, IKrylovVector<T, TVec>
	where T : unmanaged, IBaseNumber<T>
{
	/// <summary>
	/// When implemented by a derived class, get the total presenting length of this vector
	/// </summary>
	long Length { get; }

	/// <summary>
	/// When implemented by a derived class, fill this vector with the given <paramref name="value"/>
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
	T Norm();

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
	/// When implemented by a derived class, statically get an empty <typeparamref name="TVec"/>.
	/// </summary>
	abstract static TVec Empty { get; }

	/// <summary>
	/// Statically multiply the matrix whose columns are indicated by <paramref name="unjoinedVectors"/> to a dense vector indicated by a <see cref="ReadOnlySpan{T}"/> and obtain the result vector as a <typeparamref name="TVec"/>.
	/// </summary>
	/// <param name="unjoinedVectors">The columns of the matrix to be multiplied</param>
	/// <param name="input">The input dense vector to be multiplied as a <see cref="ReadOnlySpan{T}"/></param>
	/// <returns>The product of <paramref name="unjoinedVectors"/> and <paramref name="input"/> as a <typeparamref name="TVec"/></returns>
	/// <remarks>The method shall be basically static, the information of this vector shall only be used to verify the consistency of <paramref name="unjoinedVectors"/></remarks>
	/// <exception cref="ArgumentNullException">If any of <paramref name="unjoinedVectors"/> is null or invalid</exception>
	/// <exception cref="ArgumentException">If <paramref name="input"/> and <paramref name="unjoinedVectors"/> have different size, or any element of <paramref name="unjoinedVectors"/> has different size than this vector</exception>
	static TVec OperateOn(ReadOnlySpan<TVec> unjoinedVectors, ReadOnlySpan<T> input)
	{
		if (unjoinedVectors.IsEmpty)
			throw new ArgumentNullException(nameof(unjoinedVectors));
		if (input.IsEmpty)
			throw new ArgumentNullException(nameof(input));
		if (unjoinedVectors.Length != input.Length)
			throw new ArgumentException(Resources.ParameterError.NotSameSize);

		// sort first to reduce errors
		int cols = input.Length;
		using var tempArray = cols.CheckStackLimit<(T, IntPtr)>();
		Span<(T, IntPtr)> temp = tempArray.IsEmpty ? stackalloc (T, IntPtr)[cols] : tempArray.Data;
		using var tempKeys = cols.CheckStackLimit<T>();
		Span<T> keys = tempKeys.IsEmpty ? stackalloc T[cols] : tempKeys.Data;
		Span<(T val, TVec vec)> values = SpanHelper.CreateSpan(ref Unsafe.As<(T, IntPtr), (T, TVec)>(ref temp[0]), cols);
		for (int i = 0; i < cols; i++)
		{
			values[i] = (input[i], unjoinedVectors[i]);
			keys[i] = input[i] * T.Conjugate(input[i]);
		}
		keys.Sort(values);

		long vecLen = unjoinedVectors[0].Length;
		var vec = unjoinedVectors[0].CreateAlike();
		try
		{
			vec.FillWith(T.Zero);
			for (int i = 0; i < cols; i++)
			{
				var dnvec = values[i].vec;
				var val = values[i].val;
				if (dnvec is null)
					throw new ArgumentNullException(nameof(unjoinedVectors));
				if (dnvec.Length != vecLen)
					throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(unjoinedVectors));
				if (val != T.Zero)
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


#region enum
/// <summary>
/// The <see cref="RestartStrategy"/> indicates which strategy shall be adopted by the thick restart Krylov subspace algorithms.
/// </summary>
/// <remarks>Other non built-in strategies are possible and they work as long as there exists implementation supporting them.</remarks>
public enum RestartStrategy : byte
{
	/// <summary>
	/// The naïve strategy which only preserve the lowest Ritz eigen-pair and converged ones.
	/// </summary>
	Naive,
	// Ignore Spelling: \mathrm eig \left \right \underset \frac argmax
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
	/// Based on the improvement of residual of Ritz eigen-pairs of all iterations after the restart, preserve the smallest $k$ ones: <br/>
	/// $$k=\underset{k}{\max}{\left(p-k\right)\frac{\lambda_{k+1}-\lambda_1}{\lambda_m-\lambda_1}}$$
	/// </summary>
	//tex:$$k=\underset{k}{\max}{\left(p-k\right)\frac{\lambda_{k+1}-\lambda_1}{\lambda_m-\lambda_1}}$$
	WholeIterResidualImprove,
	/// <summary>
	/// The heuristic used by Krylov-Schur algorithm to prevent stagnating
	/// </summary>
	KrylovSchur,
}

/// <summary>
/// The <see cref="ReorthogonalizeMethod"/> indicates which method shall be used to re-orthogonalize with the previous basis in Krylov subspace algorithms.
/// </summary>
/// <remarks>Other non built-in methods are possible and they work as long as there exists implementation supporting them.</remarks>
public enum ReorthogonalizeMethod : byte
{
	/// <summary>
	/// Do not perform re-orthogonalization, <b>this may lead to serious problems, e.g. Lanczos may never converge</b>
	/// </summary>
	None,
	/// <summary>
	/// Selective re-orthogonalize, let the internal heuristic to determine when and which basis to re-orthogonalize
	/// </summary>
	Selective,
	/// <summary>
	/// Perform full re-orthogonalization at each iteration, this may lead to extra performance loss, especially when the problem size is small. You can use this method when the <see cref="Selective"/> one does not perform well
	/// </summary>
	Full,
	/// <summary>
	/// Perform robust full re-orthogonalization at each iteration, this may lead to extra performance loss, especially when the problem size is small. You can use this method when the <see cref="Selective"/> one does not perform well
	/// </summary>
	RobustFull
}

/// <summary>
/// The <see cref="WhichEigenvalues"/> indicates which eigen-pairs are desired in Krylov subspace algorithms of non-hermitian matrices.
/// </summary>
/// <remarks>Other non built-in desired parts are possible and they work as long as there exists implementation supporting them.</remarks>
public enum WhichEigenvalues : byte
{
	/// <summary>
	/// The eigenvalues with largest absolute values are desired
	/// </summary>
	LargestAbsolute,
	/// <summary>
	/// The eigenvalues with largest real part values are desired
	/// </summary>
	LargestReal,
	/// <summary>
	/// The eigenvalues with largest imaginary part's absolute values are desired
	/// </summary>
	LargestAbsoluteImaginary,
	/// <summary>
	/// The eigenvalues with smallest absolute values are desired
	/// </summary>
	SmallestAbsolute,
	/// <summary>
	/// The eigenvalues with smallest real part values are desired
	/// </summary>
	SmallestReal,
	/// <summary>
	/// The eigenvalues with smallest imaginary part's absolute values are desired
	/// </summary>
	SmallestAbsoluteImaginary
}
#endregion


#region Krylov subspace algorithms restart strategy
/// <summary>
/// The interface for a user-defined (or a built in) restart strategy
/// </summary>
public interface IPreserveSelector
{
	/// <summary>
	/// When implemented by a derived class, get the <see cref="RestartStrategy"/> of this selector
	/// </summary>
	RestartStrategy Strategy { get; }

	/// <summary>
	/// When implemented by a derived class, compute which Ritz pairs to preserve according to the current restart strategy.
	/// </summary>
	/// <typeparam name="T">Any unmanaged floating point number as the data type</typeparam>
	/// <param name="which">The <see cref="WhichEigenvalues"/> to indicate which eigenvalues are desired</param>
	/// <param name="estimateEigvals">The Ritz values, with or without converged ones</param>
	/// <param name="estimateEigvalsImag"><paramref name="estimateEigvals"/>'s imaginary part if the eigenvalues are complexes or empty otherwise</param>
	/// <param name="estimateEigvecs">The Ritz vectors, with or without converged ones. This shall be a square matrix of column major.</param>
	/// <param name="nConverged">The number of converged eigen-pairs</param>
	/// <param name="nTarget">The number of smallest eigen-pairs wanted</param>
	/// <param name="maxIter">The maximum number of iteration (per restart)</param>
	/// <param name="selectOrder">The span used to put the result: the <paramref name="estimateEigvals"/>[i] with larger <paramref name="selectOrder"/>[i] will be selected</param>
	/// <param name="withConverged">Whether <paramref name="estimateEigvals"/> and <paramref name="estimateEigvecs"/> contains the first <paramref name="nConverged"/> ones</param>
	/// <returns>The number of eigen-pairs which shall be selected.</returns>
	/// <remarks>This method will only be invoked internally.</remarks>
	int PreserveSelect<T>(WhichEigenvalues which, ReadOnlySpan<T> estimateEigvals, ReadOnlySpan<T> estimateEigvalsImag, ReadOnlySpanMatrix<T> estimateEigvecs, int nConverged, int nTarget, int maxIter, Span<int> selectOrder, bool withConverged = true) where T : unmanaged, IBinaryFloat<T>;
}

internal sealed class BuiltInPreserveSelector : IPreserveSelector
{
	public RestartStrategy Strategy { get; }

	public BuiltInPreserveSelector(RestartStrategy strategy)
	{
		this.Strategy = strategy;
	}

	public int PreserveSelect<T>(WhichEigenvalues which, ReadOnlySpan<T> estimateEigvals, ReadOnlySpan<T> estimateEigvalsImag, ReadOnlySpanMatrix<T> estimateEigvecs, int nConverged, int nTarget, int maxIter, Span<int> selectOrder, bool withConverged = true) where T : unmanaged, IBinaryFloat<T>
	{
		int n = estimateEigvals.Length;
		int indexMax = 0;
		int upperCount = n * 2 / 3;
		if (nTarget >= upperCount)
		{
			indexMax = nTarget;
			goto FINAL;
		}
		switch (this.Strategy)
		{
			case RestartStrategy.Naive:
				indexMax = Math.Min(Math.Max(maxIter * 2 / 5, nTarget), n);
				if (withConverged)
					indexMax = Math.Max(indexMax, nConverged);
				break;
			case RestartStrategy.IndexBased:
				indexMax = Math.Min(nTarget, (int)((maxIter - nConverged) * (0.4 + nTarget / 10.0 / maxIter)));
				indexMax = Math.Min(indexMax, upperCount);
				if (withConverged)
					indexMax = Math.Max(indexMax, nConverged);
				break;
			case RestartStrategy.CurrentResidualBest:
				using (var temp = n.CheckStackLimit<T>())
				{
					Span<T> lastRow = temp.IsEmpty ? stackalloc T[n] : temp.Data;
					for (int i = 0; i < n; i++)
					{
						lastRow[i] = estimateEigvecs[^1, i];
					}
					T lastMax = lastRow.Max(static v => T.Abs(v));
					T lastNeig = T.Abs(lastRow[nTarget - 1]);
					T upperBound = T.Max(T.Sqrt(lastMax * lastNeig), (T.One + T.One) * lastNeig);
					for (indexMax = 0; indexMax < upperCount; indexMax++)
					{
						if (T.Abs(lastRow[indexMax]) >= upperBound)
							break;
					}
				}
				if (!withConverged)
					indexMax -= nConverged;
				indexMax--;
				break;
			case RestartStrategy.OneStepResidualImprove:
				indexMax = Math.Max(nTarget, (int)(0.6 * maxIter + 0.4 * nConverged));
				if (!withConverged)
					indexMax -= nConverged;
				indexMax = Math.Min(indexMax, upperCount);
				break;
			case RestartStrategy.WholeIterResidualImprove:
				upperCount = Math.Max(nTarget, (int)(0.6 * maxIter + 0.4 * nConverged));
				if (estimateEigvalsImag.IsEmpty)
				{
					T abs0 = T.Abs(estimateEigvals[0]), gap = T.Abs(estimateEigvals[^1]) - abs0;
					T maxVal = T.Zero;
					for (int i = 0; i < upperCount; i++)
					{
						T val = (maxIter - i - 1).As<T>() * (T.Abs(estimateEigvals[i + 1]) - abs0) / gap;
						if (val > maxVal)
						{
							maxVal = val;
							indexMax = i;
						}
					}
				}
				else
				{
					T abs0 = new Complex<T>(estimateEigvals[0], estimateEigvalsImag[0]).Magnitude;
					T gap = new Complex<T>(estimateEigvals[^1], estimateEigvalsImag[^1]).Magnitude - abs0;
					T maxVal = T.Zero;
					for (int i = 0; i < upperCount; i++)
					{
						T val = (maxIter - i - 1).As<T>() * (new Complex<T>(estimateEigvals[i + 1], estimateEigvalsImag[i + 1]).Magnitude - abs0) / gap;
						if (val > maxVal)
						{
							maxVal = val;
							indexMax = i;
						}
					}
				}
				break;
			case RestartStrategy.KrylovSchur:
				indexMax = nTarget + Math.Min(nConverged, (maxIter - nTarget) / 2);
				if (indexMax == 1 && maxIter > 3)
					indexMax = maxIter / 2;
				break;
			default:
				throw new NotSupportedException();
		}
	FINAL:
		Span<T> orderedVals = stackalloc T[n], orderedValsImag = T.IsComplexType ? default : stackalloc T[n];
		SpanMatrix<T> orderedVecs = new(stackalloc T[n * n], n);
		estimateEigvals.CopyTo(orderedVals); estimateEigvalsImag.CopyTo(orderedValsImag);
		estimateEigvecs.CopyTo(orderedVecs);
		Backend.Common.SortPairs(n, which, orderedVals, orderedValsImag, orderedVecs);
		return indexMax;
	}
}
#endregion


#region wrapper
/// <summary>
/// The information ref struct used as input and output of Krylov subspace methods.
/// </summary>
/// <typeparam name="T">Any floating point number as the data type</typeparam>
/// <typeparam name="TVec">The concrete vector class type hat implements <see cref="IKrylovVector{TVec, T}"/></typeparam>
public readonly ref struct KrylovSubspaceSolveInfo<T, TVec> where T : unmanaged, IBinaryFloat<T> where TVec : class, IKrylovVector<T, TVec>
{
	#region fields
	/// <summary>
	/// The function that represents the multiplication of the target matrix and any input vector <typeparamref name="TVec"/> which returns the multiplication result as a <typeparamref name="TVec"/>
	/// </summary>
	public readonly Func<TVec, TVec> MatrixFunction;

	/// <summary>
	/// The function that represents the hermitian positive definite preconditioner's matrix-solve function which returns the result of its inverse multiplying the input vector <typeparamref name="TVec"/>
	/// </summary>
	public readonly Func<TVec, TVec>? PreconditionMatrixFunction;

	/// <summary>
	/// The initial vector as a <typeparamref name="TVec"/>
	/// </summary>
	public readonly TVec InitialVector;

	/// <summary>
	/// The other vector used as a <typeparamref name="TVec"/>
	/// </summary>
	public readonly TVec? OtherVector;

	/// <summary>
	/// The tolerance of the convergence, default 0 means <c><see cref="IBaseNumber{TSelf}.MachinePrecision"/> ^ 0.9</c>
	/// </summary>
	public readonly double Tolerance = Math.Pow(T.MachinePrecision.AsDouble(), 0.9);

	/// <summary>
	/// The <see cref="IPreserveSelector"/> used for selecting the preservation Ritz pairs, default null means <c>new <see cref="BuiltInPreserveSelector(RestartStrategy)">BuiltInRestartStrategy</see>(<see cref="RestartStrategy"/>)</c>.
	/// </summary>
	public readonly IPreserveSelector PreserveSelector;

	/// <summary>
	/// The output converged eigenvalues sorted by the given order of <see cref="WhichEigenvaluesDesired"/>. The length will be set to the number of converged eigenvalues at exit.
	/// </summary>
	public readonly Span<T> Eigenvalues;

	/// <summary>
	/// Another <see cref="Eigenvalues"/> if the matrix is not hermitian and <typeparamref name="T"/> is not a complex.
	/// </summary>
	/// <remarks>When <see cref="EigenvaluesImag"/>[i] is not, <see cref="EigenvaluesImag"/>[i + 1] shall be its negation.</remarks>
	public readonly Span<T> EigenvaluesImag;

	/// <summary>
	/// The output converged eigenvectors, sorted with <see cref="Eigenvalues"/> or <see cref="EigenvaluesImag"/>.
	/// </summary>
	/// <remarks>For real-typed <typeparamref name="T"/>, when <c><see cref="EigenvaluesImag"/>[i]</c> is not 0, <c><see cref="EigenvaluesImag"/>[i]</c> will contain the corresponding real parts of the actual <c>eigenvector[i]</c> and <c>eigenvector[i + 1]</c>, while <c><see cref="EigenvaluesImag"/>[i + 1]</c> will contain the imaginary parts for <c>eigenvector[i]</c> and <c>-eigenvector[i + 1]</c>.</remarks>
	public readonly Span<TVec> Eigenvectors;

	/// <summary>
	/// Only the top <see cref="NumberEigenvaluesDesired"/> eigen-pairs of <see cref="WhichEigenvaluesDesired"/> are the targets. DO NOT set a large value since the Krylov subspace algorithms are not designed for it.
	/// </summary>
	public readonly int NumberEigenvaluesDesired;

	/// <summary>
	/// The maximum number of restarts, must be positive. Or the total number of iterations if the implementation solver does not restarts.
	/// </summary>
	public readonly int MaxRestarts;

	/// <summary>
	/// The iteration number per restart, default 0 means letting internal implementation determine
	/// </summary>
	public readonly int IterationsPerRestart;

	/// <summary>
	/// The <see cref="ReorthogonalizeMethod"/> to indicate which re-orthogonalization method to use
	/// </summary>
	public readonly ReorthogonalizeMethod ReorthogonalizeMethod;

	/// <summary>
	/// The <see cref="WhichEigenvalues"/> to indicate which kind of eigenvalues (and the corresponding eigenvectors) are desired
	/// </summary>
	public readonly WhichEigenvalues WhichEigenvaluesDesired;

	/// <summary>
	/// Whether to use the estimated gap in the convergence criteria or use the matrix norm, default true. If the gap can be especially difficult to estimate, this shall be set to false.
	/// </summary>
	public readonly bool UseGapEstimation;

	/// <summary>
	/// Whether to check the <see cref="MatrixFunction"/> with <see cref="InitialVector"/> at first. If true, there will be some performance loss.
	/// </summary>
	public readonly bool CheckMatrixFunction;
	#endregion

	#region create
	/// <summary>
	/// Create a <see cref="KrylovSubspaceSolveInfo{TVec, T}"/> of the given naïve hermitian matrix eigen-solve problem
	/// </summary>
	/// <exception cref="TypeMismatchException">If <typeparamref name="T"/> is not a floating-point type</exception>
	/// <exception cref="ArgumentOutOfRangeException">I  <paramref name="maxIter"/> is out of range</exception>
	/// <exception cref="ArgumentNullException">If <paramref name="initial"/> or <paramref name="matrixFunction"/> is null</exception>
	public KrylovSubspaceSolveInfo(Func<TVec, TVec> matrixFunction, TVec initial, int maxIter, bool check = true)
	{
		if (maxIter <= 0)
			throw new ArgumentOutOfRangeException(nameof(maxIter), maxIter, Resources.ParameterError.MustPositive);
		this.MatrixFunction = matrixFunction;
		this.PreconditionMatrixFunction = null;
		this.InitialVector = initial;
		this.OtherVector = null;
		this.NumberEigenvaluesDesired = 1;
		this.WhichEigenvaluesDesired = default;
		this.MaxRestarts = maxIter;
		this.IterationsPerRestart = 0;
		this.ReorthogonalizeMethod = default;
		this.UseGapEstimation = default;
		this.CheckMatrixFunction = check;
		this.PreserveSelector = new BuiltInPreserveSelector(default);

		this.Eigenvalues = default;
		this.EigenvaluesImag = default;
		this.Eigenvectors = default;
	}

	/// <summary>
	/// Create a <see cref="KrylovSubspaceSolveInfo{TVec, T}"/> of the given non-hermitian matrix eigen-solve problem
	/// </summary>
	/// <exception cref="TypeMismatchException">If <typeparamref name="T"/> is not a floating-point type</exception>
	/// <exception cref="ArgumentOutOfRangeException">If <paramref name="iterPerRestart"/>, <paramref name="maxRestarts"/>, <paramref name="nEig"/> or <paramref name="tolerance"/> is out of range</exception>
	/// <exception cref="ArgumentNullException">If <paramref name="initial"/> or <paramref name="matrixFunction"/> is null</exception>
	/// <exception cref="ArgumentException">If any of <paramref name="outputEigenvalues"/>, <paramref name="outputRealEigenvectors"/> is too short</exception>
	public KrylovSubspaceSolveInfo(Func<TVec, TVec> matrixFunction, TVec initial,
								   Span<T> outputEigenvalues, Span<T> outputEigenvaluesImag,
								   Span<TVec> outputRealEigenvectors,
								   int nEig = 1, WhichEigenvalues which = WhichEigenvalues.LargestAbsolute,
								   int maxRestarts = int.MaxValue, int iterPerRestart = 0, double tolerance = 0,
								   ReorthogonalizeMethod reorthogonalize = ReorthogonalizeMethod.RobustFull,
								   IPreserveSelector? selector = null, bool useGap = true, bool check = true)
	{
		if (iterPerRestart < 0)
			throw new ArgumentOutOfRangeException(nameof(iterPerRestart), iterPerRestart, Resources.ParameterError.CannotNegative);
		if (tolerance < 0)
			throw new ArgumentOutOfRangeException(nameof(tolerance), tolerance, Resources.ParameterError.CannotNegative);
		if (maxRestarts <= 0)
			throw new ArgumentOutOfRangeException(nameof(maxRestarts), maxRestarts, Resources.ParameterError.MustPositive);
		if (nEig <= 0)
			throw new ArgumentOutOfRangeException(nameof(nEig), nEig, Resources.ParameterError.MustPositive);
		if (outputEigenvalues.Length < nEig)
			throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(outputEigenvalues));
		if (outputRealEigenvectors.Length < nEig)
			throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(outputRealEigenvectors));

		this.MatrixFunction = matrixFunction;
		this.PreconditionMatrixFunction = null;
		this.InitialVector = initial;
		this.OtherVector = null;
		this.NumberEigenvaluesDesired = nEig;
		this.WhichEigenvaluesDesired = which;
		this.MaxRestarts = maxRestarts;
		this.IterationsPerRestart = iterPerRestart;
		if (tolerance != 0)
			this.Tolerance = tolerance;
		this.ReorthogonalizeMethod = reorthogonalize;
		this.UseGapEstimation = useGap;
		this.CheckMatrixFunction = check;
		this.PreserveSelector = selector ?? new BuiltInPreserveSelector(RestartStrategy.KrylovSchur);

		this.Eigenvalues = outputEigenvalues;
		this.EigenvaluesImag = outputEigenvaluesImag;
		this.Eigenvectors = outputRealEigenvectors;
	}

	/// <summary>
	/// Create a <see cref="KrylovSubspaceSolveInfo{TVec, T}"/> of the given hermitian matrix eigen-solve problem
	/// </summary>
	/// <exception cref="TypeMismatchException">If <typeparamref name="T"/> is not a floating-point type</exception>
	/// <exception cref="ArgumentOutOfRangeException">If <paramref name="iterPerRestart"/>, <paramref name="maxRestarts"/>, <paramref name="nEig"/> or <paramref name="tolerance"/> is out of range</exception>
	/// <exception cref="ArgumentNullException">If <paramref name="initial"/> or <paramref name="matrixFunction"/> is null</exception>
	/// <exception cref="ArgumentException">If any of <paramref name="outputEigenvalues"/> or <paramref name="outputEigenvectors"/> is too short</exception>
	public KrylovSubspaceSolveInfo(Func<TVec, TVec> matrixFunction, TVec initial,
								   Span<T> outputEigenvalues, Span<TVec> outputEigenvectors,
								   int nEig = 1, int maxRestarts = int.MaxValue, int iterPerRestart = 0, double tolerance = 0,
								   ReorthogonalizeMethod reorthogonalize = ReorthogonalizeMethod.RobustFull,
								   IPreserveSelector? selector = null, bool useGap = true, bool check = true)
	{
		if (iterPerRestart < 0)
			throw new ArgumentOutOfRangeException(nameof(iterPerRestart), iterPerRestart, Resources.ParameterError.CannotNegative);
		if (tolerance < 0)
			throw new ArgumentOutOfRangeException(nameof(tolerance), tolerance, Resources.ParameterError.CannotNegative);
		if (maxRestarts <= 0)
			throw new ArgumentOutOfRangeException(nameof(maxRestarts), maxRestarts, Resources.ParameterError.MustPositive);
		if (nEig <= 0)
			throw new ArgumentOutOfRangeException(nameof(nEig), nEig, Resources.ParameterError.MustPositive);
		if (outputEigenvalues.Length < nEig)
			throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(outputEigenvalues));
		if (outputEigenvectors.Length < nEig)
			throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(outputEigenvectors));

		this.MatrixFunction = matrixFunction;
		this.PreconditionMatrixFunction = null;
		this.InitialVector = initial;
		this.OtherVector = null;
		this.NumberEigenvaluesDesired = nEig;
		this.WhichEigenvaluesDesired = WhichEigenvalues.SmallestReal;
		this.MaxRestarts = maxRestarts;
		this.IterationsPerRestart = iterPerRestart;
		if (tolerance != 0)
			this.Tolerance = tolerance;
		this.ReorthogonalizeMethod = reorthogonalize;
		this.UseGapEstimation = useGap;
		this.CheckMatrixFunction = check;
		this.PreserveSelector = selector ?? new BuiltInPreserveSelector(RestartStrategy.KrylovSchur);

		this.Eigenvalues = outputEigenvalues;
		this.EigenvaluesImag = default;
		this.Eigenvectors = outputEigenvectors;
	}

	/// <summary>
	/// Create a <see cref="KrylovSubspaceSolveInfo{TVec, T}"/> of the given (non-)hermitian matrix solve problem.<br/>
	/// The hermitian positive definite preconditioner <paramref name="M"/> can be null to represent no preconditioning.<br/>
	/// The solver shall effectively solve the linear system <c>Hˉ¹ * A * (H')ˉ¹ * y == Hˉ¹ * b</c> for <c>y</c> where <c>y = H' * <paramref name="rightSide"/></c> and <c>M = H * H'</c>.
	/// </summary>
	/// <exception cref="TypeMismatchException">If <typeparamref name="T"/> is not a floating-point type</exception>
	/// <exception cref="ArgumentOutOfRangeException">If <paramref name="iterPerRestart"/>, <paramref name="maxRestarts"/> or <paramref name="tolerance"/> is out of range</exception>
	/// <exception cref="ArgumentNullException">If <paramref name="rightSide"/> or <paramref name="initial"/> or <paramref name="matrixFunction"/> is null</exception>
	public KrylovSubspaceSolveInfo(Func<TVec, TVec> matrixFunction, TVec rightSide, TVec initial, Func<TVec, TVec>? M = null,
								   int maxRestarts = int.MaxValue, int iterPerRestart = 0, double tolerance = 0,
								   ReorthogonalizeMethod reorthogonalize = ReorthogonalizeMethod.RobustFull,
								   IPreserveSelector? selector = null, bool useGap = true, bool check = true)
	{
		if (iterPerRestart < 0)
			throw new ArgumentOutOfRangeException(nameof(iterPerRestart), iterPerRestart, Resources.ParameterError.CannotNegative);
		if (tolerance < 0)
			throw new ArgumentOutOfRangeException(nameof(tolerance), tolerance, Resources.ParameterError.CannotNegative);
		if (maxRestarts <= 0)
			throw new ArgumentOutOfRangeException(nameof(maxRestarts), maxRestarts, Resources.ParameterError.MustPositive);
		this.MatrixFunction = matrixFunction;
		this.PreconditionMatrixFunction = M;
		this.InitialVector = initial;
		this.OtherVector = rightSide;
		this.NumberEigenvaluesDesired = 1;
		this.WhichEigenvaluesDesired = default;
		this.MaxRestarts = maxRestarts;
		this.IterationsPerRestart = iterPerRestart;
		if (tolerance != 0)
			this.Tolerance = tolerance;
		this.ReorthogonalizeMethod = reorthogonalize;
		this.UseGapEstimation = useGap;
		this.CheckMatrixFunction = check;
		this.PreserveSelector = selector ?? new BuiltInPreserveSelector(RestartStrategy.KrylovSchur);

		this.Eigenvalues = default;
		this.EigenvaluesImag = default;
		this.Eigenvectors = default;
	}

	/// <summary>
	/// Create a <see cref="KrylovSubspaceSolveInfo{TVec, T}"/> from an <paramref name="old"/> one
	/// </summary>
	/// <exception cref="TypeMismatchException">If <typeparamref name="T"/> is not a floating-point type</exception>
	/// <exception cref="ArgumentNullException">If <paramref name="initial"/> or <paramref name="matrixFunction"/> is null</exception>
	public KrylovSubspaceSolveInfo(Func<TVec, TVec> matrixFunction, Func<TVec, TVec>? M, TVec initial, TVec? other, ref KrylovSubspaceSolveInfo<T, TVec> old)
	{
		this.MatrixFunction = matrixFunction;
		this.PreconditionMatrixFunction = M ?? old.PreconditionMatrixFunction;
		this.InitialVector = initial;
		this.OtherVector = other;
		this.NumberEigenvaluesDesired = old.NumberEigenvaluesDesired;
		this.WhichEigenvaluesDesired = old.WhichEigenvaluesDesired;
		this.MaxRestarts = old.MaxRestarts;
		this.IterationsPerRestart = old.IterationsPerRestart;
		this.Tolerance = old.Tolerance;
		this.ReorthogonalizeMethod = old.ReorthogonalizeMethod;
		this.UseGapEstimation = old.UseGapEstimation;
		this.CheckMatrixFunction = old.CheckMatrixFunction;
		this.PreserveSelector = old.PreserveSelector;

		this.Eigenvalues = old.Eigenvalues;
		this.EigenvaluesImag = old.EigenvaluesImag;
		this.Eigenvectors = old.Eigenvectors;
	}
	#endregion
}
#endregion
