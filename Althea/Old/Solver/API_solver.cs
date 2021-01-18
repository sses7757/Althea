using System;
using System.Collections.Generic;

using Althea.Linq;
using Althea.Array;
using RT = Althea.Runtime.API;
using Althea.Storage;

namespace Althea.Solver
{
	/// <summary>
	/// The Solver API library wrapper
	/// </summary>
	public static class API
	{
		#region base
		/// <summary>
		/// Static class initializer
		/// </summary>
		static API()
		{
			if (GlobalSettings.SolverGPU != null)
				GPUconstructor = GlobalSettings.SolverGPU.GetConstructor(Array.Empty<Type>());
			else
				GPUconstructor = typeof(Cuda.CudaSolver).GetConstructor(Array.Empty<Type>());
			if (GlobalSettings.SolverCPU != null)
				CPUconstructor = GlobalSettings.SolverCPU.GetConstructor(Array.Empty<Type>());
			else
				CPUconstructor = typeof(Mkl.MklSolver).GetConstructor(Array.Empty<Type>());
			Initialize();
		}

		/// <summary>
		/// Reset the Solver libraries
		/// </summary>
		public static void Reset()
		{
			try
			{
				GPU.Dispose();
				CPU.Dispose();
			}
			catch (StatusException e)
			{
				Log.Write($"Error at reseting Solver library \"{e.Message}\":" + Environment.NewLine + e.StackTrace, level: LogLevel.Error);
			}
			finally
			{
				Initialize();
			}
		}

		/// <summary>
		/// Singleton Solver API of GPU routine
		/// </summary>
		/// <remarks>once you use this property, the underlying adapter will be initialized</remarks>
		public static ISolver GPU => _GPUInit.Value;

		/// <summary>
		/// Singleton Solver API of CPU routine
		/// </summary>
		/// <remarks>once you use this property, the underlying adapter will be initialized</remarks>
		public static ISolver CPU => _CPUInit.Value;

		private static readonly System.Reflection.ConstructorInfo GPUconstructor, CPUconstructor;

		private static Lazy<ISolver> _GPUInit, _CPUInit;

		private static void Initialize()
		{
			_GPUInit = new Lazy<ISolver>(() => GPUconstructor.Invoke(Array.Empty<object>()) as ISolver, true);
			_CPUInit = new Lazy<ISolver>(() => CPUconstructor.Invoke(Array.Empty<object>()) as ISolver, true);
		}
		#endregion


		#region decompose
		internal static bool CheckEigenType<T, TOut>(this MatrixBase<T> A, MatrixBase<T> B)
			where T : struct, IComparable<T>
			where TOut : struct, IComparable<TOut>
		{
			DataType typeIn = default(T).ToDataType();
			if (!typeIn.IsFloat())
				return false;
			if (A.Hermitian && (B is null || B.Hermitian))
			{
				if (default(TOut).ToDataType() != typeIn.RealCorrespond())
					return false;
			}
			else
			{
				if (default(TOut).ToDataType() != typeIn.ComplexCorrespond())
					return false;
			}
			return true;
		}


		/// <summary>
		/// Calculate the eigenvalues (and eigenvectors) of given matrix <paramref name="A"/> for the special eigen-problem -- $A V = V \Lambda$, or matrices pair <paramref name="A"/>, <paramref name="B"/> for the general one -- $A V = \Lambda B V$ or $A B V = \Lambda V$ or $B A V = \Lambda V$.
		/// </summary>
		/// <typeparam name="T">input data type, see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <typeparam name="TCorr"><paramref name="valOut"/> data type, must be corresponding real types of <typeparamref name="T"/> if <paramref name="A"/>, <paramref name="B"/> are Hermitian or corresponding complex types of <typeparamref name="T"/> otherwise</typeparam>
		/// <param name="valOut">the output eigenvalues, must be preallocated</param>
		/// <param name="leftVecOut">the output left eigenvectors, if both <paramref name="A"/> and <paramref name="B"/> are Hermitian, <paramref name="A"/> rather than <paramref name="leftVecOut"/> is used to store eigenvectors</param>
		/// <param name="rightVecOut">the output right eigenvectors, if both <paramref name="A"/> and <paramref name="B"/> are Hermitian, <paramref name="A"/> rather than <paramref name="rightVecOut"/> is used to store eigenvectors</param>
		/// <param name="A">the input/output <see cref="DenseMatrix{T}"/> to calculate eigensystem; destroyed during the calculation if <paramref name="mode"/> is <see cref="EigMode.NoVector"/> or replaced by the eigenvectors if this is a Hermitian problem</param>
		/// <param name="B">the input <see cref="DenseMatrix{T}"/> to calculate general eigen-problem; if <c><paramref name="B"/> is null</c>, the normal eigen is performed and <paramref name="eigType"/> is not used; otherwise, the general one is performed</param>
		/// <param name="mode">the <see cref="EigMode"/> to indicate whether the eigenvectors should be calculated</param>
		/// <param name="eigType">the <see cref="EigType"/> to indicate positions of <paramref name="A"/> and <paramref name="B"/></param>
		/// <exception cref="ArgumentNullException">if <paramref name="A"/> or <paramref name="valOut"/> is null</exception>
		/// <exception cref="ArgumentException">if the <paramref name="valOut"/> is too short or <typeparamref name="T"/> and <typeparamref name="TCorr"/> do not match</exception>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> or <typeparamref name="TCorr"/> is not one of the supported type</exception>
		/// <exception cref="StatusException">if the internal calculation returns error status</exception>
		/// <exception cref="MatrixAlgorithmException">if the internal calculation fails</exception>
		public static void EigenSolve<T, TCorr>(DenseVector<TCorr> valOut, DenseMatrix<T> A, DenseMatrix<TCorr> leftVecOut, DenseMatrix<TCorr> rightVecOut, DenseMatrix<T> B = null, EigType eigType = EigType.Type1, EigMode mode = EigMode.NoVector)
			where T : struct, IComparable<T>
			where TCorr : struct, IComparable<TCorr>
		{
			if (A is null || A == PureArray<T>.EmptyDnMat)
				throw new ArgumentNullException(nameof(A), Resource.ArrayCannotNull);
			if (valOut is null || valOut == PureArray<TCorr>.EmptyDnVec)
				throw new ArgumentNullException(nameof(valOut), Resource.ArrayCannotNull);
			if (A.IsSingleType != valOut.IsSingleType)
				throw new ArgumentException(Resource.GenericTypeError, nameof(valOut));
			if (valOut.Length < A.NRows)
				throw new ArgumentException(Resource.VectorTooShort, nameof(valOut));
			if (!CheckEigenType<T, TCorr>(A, B))
				throw new NotSupportedException(Resource.DataTypeNotSupport);

			var onHostA = CudaCSharpHelpers.CheckOnHost(A);
			var onHostV = CudaCSharpHelpers.CheckOnHost(valOut);
			if (onHostA != onHostV)
				throw new ArgumentException(Resource.RequireSamePos, nameof(valOut));

			if (B is null)
			{
				if (A.Hermitian)
				{
					var func = onHostA ? new ISolver.DelegateEigenSpecialHermitianMatrix<T, TCorr>(CPU.EigenSpecialHermitianMatrix) : GPU.EigenSpecialHermitianMatrix;
					func(A.IntRows, valOut.Pointer, A.Pointer, A.IntLeadDim, mode == EigMode.NoVector ? mode : EigMode.Vector);
				}
				else
				{
					if (leftVecOut is null)
						leftVecOut = PureArray<TCorr>.EmptyDnMat;
					if (rightVecOut is null)
						rightVecOut = PureArray<TCorr>.EmptyDnMat;
					if ((mode == EigMode.Vector || mode == EigMode.LeftOnly) && (leftVecOut == PureArray<TCorr>.EmptyDnMat || leftVecOut.OnHost != A.OnHost))
						throw new ArgumentNullException(nameof(leftVecOut), Resource.ArrayCannotNull);
					if ((mode == EigMode.Vector || mode == EigMode.RightOnly) && (rightVecOut == PureArray<TCorr>.EmptyDnMat || rightVecOut.OnHost != A.OnHost))
						throw new ArgumentNullException(nameof(leftVecOut), Resource.ArrayCannotNull);
					var func = onHostA ? new ISolver.DelegateEigenSpecialGeneralMatrix<T, TCorr>(CPU.EigenSpecialGeneralMatrix) : GPU.EigenSpecialGeneralMatrix;
					func(A.IntRows, valOut.Pointer, 
						(mode == EigMode.Vector || mode == EigMode.LeftOnly) ? leftVecOut.Pointer : null, leftVecOut.IntLeadDim,
						(mode == EigMode.Vector || mode == EigMode.RightOnly) ? rightVecOut.Pointer : null, rightVecOut.IntLeadDim,
						A.Pointer, A.IntLeadDim, mode);
				}
			}
			else
			{
				var onHostB = CudaCSharpHelpers.CheckOnHost(B);
				if (onHostA != onHostB)
					throw new ArgumentException(Resource.RequireSamePos, nameof(B));

				if (A.Hermitian && B.Hermitian)
				{
					var func = onHostA ? new ISolver.DelegateEigenGeneralHermitianMatrix<T, TCorr>(CPU.EigenGeneralHermitianMatrix) : GPU.EigenGeneralHermitianMatrix;
					func(A.IntRows, valOut.Pointer, A.Pointer, A.IntLeadDim, B.Pointer, B.IntLeadDim, eigType, mode);
				}
				else
				{
					if (leftVecOut is null)
						leftVecOut = PureArray<TCorr>.EmptyDnMat;
					if (rightVecOut is null)
						rightVecOut = PureArray<TCorr>.EmptyDnMat;
					if ((mode == EigMode.Vector || mode == EigMode.LeftOnly) && (leftVecOut == PureArray<TCorr>.EmptyDnMat || leftVecOut.OnHost != A.OnHost))
						throw new ArgumentNullException(nameof(leftVecOut), Resource.ArrayCannotNull);
					if ((mode == EigMode.Vector || mode == EigMode.RightOnly) && (rightVecOut == PureArray<TCorr>.EmptyDnMat || rightVecOut.OnHost != A.OnHost))
						throw new ArgumentNullException(nameof(leftVecOut), Resource.ArrayCannotNull);
					var func = onHostA ? new ISolver.DelegateEigenGeneralGeneralMatrix<T, TCorr>(CPU.EigenGeneralGeneralMatrix) : GPU.EigenGeneralGeneralMatrix;
					func(A.IntRows, valOut.Pointer,
						(mode == EigMode.Vector || mode == EigMode.LeftOnly) ? leftVecOut.Pointer : null, leftVecOut.IntLeadDim,
						(mode == EigMode.Vector || mode == EigMode.RightOnly) ? rightVecOut.Pointer : null, rightVecOut.IntLeadDim,
						A.Pointer, A.IntLeadDim, B.Pointer, B.IntLeadDim, eigType, mode);
				}
			}
		}

		/// <summary>
		/// This function computes the singular value decomposition (SVD) of a matrix <paramref name="A"/> and corresponding the left and/or right singular vectors: $A = U S V^*$.
		/// </summary>
		/// <param name="jobu">specifies options for computing all or part of the matrix <paramref name="U"/></param>
		/// <param name="jobvt">specifies options for computing all or part of the matrix <paramref name="Vct"/>, same as <paramref name="jobu"/></param>
		/// <param name="A">input <see cref="DenseMatrix{T}"/> with size</param>
		/// <param name="S">output singular values <see cref="DenseVector{T}"/></param>
		/// <param name="U">output left unitary <see cref="DenseMatrix{T}"/>, must be pre-allocated if <paramref name="jobu"/> is not <see cref="SVDStore.Overwrite"/> neither <see cref="SVDStore.None"/></param>
		/// <param name="Vct">output right unitary (<c>V<sup>*</sup></c>) <see cref="DenseMatrix{T}"/>, must be pre-allocated if <paramref name="jobvt"/> is not <see cref="SVDStore.Overwrite"/> neither <see cref="SVDStore.None"/></param>
		/// <exception cref="ArgumentNullException">if <paramref name="A"/> or <paramref name="S"/> is null, or the <paramref name="U"/>, <paramref name="Vct"/> do not follows the previous rules</exception>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> or <typeparamref name="TReal"/> is not one of the supported type</exception>
		/// <exception cref="StatusException">if the internal calculation returns error status</exception>
		public static void SingularValues<T, TReal>(DenseMatrix<T> A, DenseVector<TReal> S, DenseMatrix<T> U, DenseMatrix<T> Vct, SVDStore jobu = SVDStore.Economic, SVDStore jobvt = SVDStore.Economic) where T : struct, IComparable<T> where TReal : struct, IComparable<TReal>
		{
			if (A is null || A == PureArray<T>.EmptyDnMat)
				throw new ArgumentNullException(nameof(A), Resource.ArrayCannotNull);
			if (S is null || S == PureArray<TReal>.EmptyDnVec)
				throw new ArgumentNullException(nameof(S), Resource.ArrayCannotNull);
			long m = Math.Min(A.NRows, A.NCols);
			if (S.Length < m)
				throw new ArgumentException(Resource.VectorTooShort, nameof(S));
			if (jobu != SVDStore.Overwrite && jobu != SVDStore.None && (U is null || U == PureArray<T>.EmptyDnMat))
				throw new ArgumentNullException(nameof(U), Resource.ArrayCannotNull);
			if (jobvt != SVDStore.Overwrite && jobvt != SVDStore.None && (Vct is null || Vct == PureArray<T>.EmptyDnMat))
				throw new ArgumentNullException(nameof(U), Resource.ArrayCannotNull);
			if (jobu == SVDStore.Overwrite && jobvt == SVDStore.Overwrite)
				throw new ArgumentException($"Overwriting both {nameof(U)} and {nameof(Vct)} to {nameof(A)}" + Resource.BaseNotSupport);

			(long row, long col) dimU = (0, 0);
			if (jobu == SVDStore.All)
				dimU = (A.NRows, A.NRows);
			else if (jobu == SVDStore.Economic)
				dimU = A.NRows >= A.NCols ? (A.NRows, A.NCols) : (A.NRows, A.NRows);
			if ((jobu == SVDStore.All || jobu == SVDStore.Economic) && (U.NRows < dimU.row || U.NCols < dimU.col))
				throw new ArgumentException(Resource.MatrixWrongSize, nameof(U));

			(long row, long col) dimVct = (0, 0);
			if (jobvt == SVDStore.All)
				dimVct = (A.NCols, A.NCols);
			else if (jobvt == SVDStore.Economic)
				dimVct = A.NRows >= A.NCols ? (A.NCols, A.NCols) : (A.NRows, A.NCols);
			if ((jobvt == SVDStore.All || jobvt == SVDStore.Economic) && (Vct.NRows < dimVct.row || Vct.NCols < dimVct.col))
				throw new ArgumentException(Resource.MatrixWrongSize, nameof(Vct));

			if (jobu == SVDStore.Overwrite || jobu == SVDStore.None)
				U = A;
			if (jobvt == SVDStore.Overwrite || jobvt == SVDStore.None)
				Vct = A;
			var corrType = default(TReal).ToDataType();
			bool support = default(T).ToDataType() switch
			{
				DataType.RealSingle => corrType == DataType.RealSingle,
				DataType.ComplexSingle => corrType == DataType.RealSingle,
				DataType.RealDouble => corrType == DataType.RealDouble,
				DataType.ComplexDouble => corrType == DataType.RealDouble,
				_ => false
			};
			if (!support)
				throw new NotSupportedException(Resource.DataTypeNotSupport);
			var onHostA = CudaCSharpHelpers.CheckOnHost(A, U, Vct);
			var onHostS = CudaCSharpHelpers.CheckOnHost(S);
			if (onHostA != onHostS)
				throw new ArgumentException(Resource.RequireSamePos, nameof(S));

			// calculate the conjugate transpose of A if A is a 'fat' matrix
			if (A.NRows < A.NCols)
			{
				using var Act = A.ConjugateTranspose();
				using var Uct = U == A ? null : new DenseMatrix<T>(dimU.col, dimU.row, onHost: U.OnHost);
				using var V = Vct == A ? null : new DenseMatrix<T>(dimVct.col, dimVct.row, onHost: Vct.OnHost);
				SingularValues(Act, S, V, Uct, jobu, jobvt);
				DenseMatrix<T> refU = new DenseMatrix<T>(U, dimU.row, dimU.col), refVct = new DenseMatrix<T>(Vct, dimVct.row, dimVct.col);
				if (Uct != null)
					Blas.API.MatrixGeneralAdd(A: Uct, B: PureArray<T>.EmptyDnMat, C: refU, α: Scalars<T>.One, opA: MatrixOperation.ConjugateTranspose);
				if (V != null)
					Blas.API.MatrixGeneralAdd(A: V, B: PureArray<T>.EmptyDnMat, C: refVct, α: Scalars<T>.One, opA: MatrixOperation.ConjugateTranspose);
				return;
			}

			var func = onHostA ? new ISolver.DelegateSingularValues<T, TReal>(CPU.SingularValues) : GPU.SingularValues;
			func(jobu, jobvt, A.IntRows, A.IntCols, A.Pointer, A.IntLeadDim, S.Pointer, U.Pointer, U.IntLeadDim, Vct.Pointer, Vct.IntLeadDim);
		}

		/// <summary>
		/// QR factorize the given matrix <paramref name="A"/>.
		/// </summary>
		/// <param name="A">input matrix to be factorized</param>
		/// <param name="Q">the output unitary matrix, full factorization or not depends on its size</param>
		/// <param name="tri">the output trapezoidal matrix whose lower part may be not meaningful</param>
		/// <remarks>LQ decomposition can be done via QR factorizing the conjugate transpose of <paramref name="A"/>.<br/>
		/// RQ decomposition can be done via QR factorizing the inverse of <paramref name="A"/> or the up-down reverse of <paramref name="A"/>.<br/>
		/// QL decomposition can be done via QR factorizing the inverse of conjugate transpose of <paramref name="A"/> of the left-right reverse of <paramref name="A"/>.</remarks>
		public static void QR<T>(DenseMatrix<T> A, DenseMatrix<T> Q, DenseMatrix<T> tri) where T : struct, IComparable<T>
		{
			if (A is null || A == PureArray<T>.EmptyDnMat)
				throw new ArgumentNullException(nameof(A), Resource.ArrayCannotNull);
			if (tri is null || tri == PureArray<T>.EmptyDnMat)
				throw new ArgumentNullException(nameof(tri), Resource.ArrayCannotNull);
			if (A.NRows >= A.NCols && (Q.NRows != A.NRows || (Q.NCols != A.NCols && Q.NCols != A.NRows)))
				throw new ArgumentException(Resource.MatrixWrongSize, nameof(Q));
			if (A.NRows < A.NCols && (Q.NRows != A.NRows || Q.NCols != A.NRows))
				throw new ArgumentException(Resource.MatrixWrongSize, nameof(Q));
			if (tri.NRows != Math.Min(A.NRows, A.NCols))
				throw new ArgumentException(Resource.MatrixWrongSize, nameof(tri));

			bool full = Q.NRows >= A.NRows && Q.NCols >= A.NCols;
			var onHost = CudaCSharpHelpers.CheckOnHost(A, tri);
			var func = onHost ? new ISolver.DelegateQRDecomposition<T>(CPU.QRDecomposition) : GPU.QRDecomposition;
			tri.FillWithZeros(); // remove possible NaNs, this is OK since triangular matrix will be overwritten
			if (full)
			{
				Q.FillWithZeros(); // remove possible NaNs at right most columns, this is OK since Q will be overwritten
				RT.CopyMatrixTo(source: A, dest: Q, copyNRows: A.NRows, copyNCols: A.NCols);
				func(full, A.IntRows, A.IntCols, Q.Pointer, Q.IntLeadDim, tri.Pointer, tri.IntLeadDim);
			}
			else
			{
				using var copy = Storage<T>.Create(A.ActualLength, A.OnHost);
				RT.CopyTo(source: A.Pointer, dest: copy, length: A.ActualLength);
				func(full, A.IntRows, A.IntCols, copy, A.IntLeadDim, tri.Pointer, tri.IntLeadDim);
				RT.CopyMatrixTo(source: copy, dest: Q.Pointer, srcLD: A.LeadDim, dstLD: Q.LeadDim, copyNRows: Q.NRows, copyNCols: Q.NCols);
			}
		}

		/// <summary>
		/// Schur decompose the given matrix <paramref name="A"/>.
		/// </summary>
		/// <param name="A">matrix to be decomposed, replaced by the Schur matrix after return</param>
		/// <param name="U">the output unitary matrix, default null means do not compute</param>
		/// <param name="order">the orders the factorization so that selected eigenvalues are at the top left of Schur form. Default null means identity permutation, which is sorted descending by the modulus of eigenvalues</param>
		/// <param name="orderVal">the value order of the factorization so that selected eigenvalues are at the top left of Schur form. Default null means use <paramref name="order"/></param>
		/// <returns>the actual number of eigenvalues returned</returns>
		/// <remarks>if both <paramref name="order"/> and <paramref name="orderVal"/> are indicated, only <paramref name="orderVal"/> will be used</remarks>
		public static int Schur<T>(DenseMatrix<T> A, int[] order = null, DoubleComplex[] orderVal = null, DenseMatrix<T> U = null) where T : struct, IComparable<T>
		{
			if (A is null || A == PureArray<T>.EmptyDnMat)
				throw new ArgumentNullException(nameof(A), Resource.ArrayCannotNull);
			if (U == PureArray<T>.EmptyDnMat)
				throw new ArgumentNullException(nameof(U), Resource.ArrayCannotNull);
			if (A.NRows != A.NCols)
				throw new ArgumentException(Resource.MatMustSquare, nameof(A));
			if (U != null && U.NRows != U.NCols)
				throw new ArgumentException(Resource.MatMustSquare, nameof(U));
			if (U != null && U.NRows != A.NRows)
				throw new ArgumentException(Resource.MatrixWrongSize, nameof(U));

			var onHost = CudaCSharpHelpers.CheckOnHost(A, U);

			// calculate value order first
			if (orderVal is null && order != null)
			{
				if (order.Max() >= A.NRows || order.Min() < 0 || order.Distinct().Count < order.Length)
					throw new ArgumentOutOfRangeException(nameof(order));
				DoubleComplex[] vals;
				if (A.IsSingleType)
				{
					using var _tempVal = new DenseVector<FloatComplex>(A.NRows, A.OnHost);
					EigenSolve(_tempVal, A, null, null, mode: EigMode.NoVector);
					vals = Array.ConvertAll(RT.CopyOutArray(_tempVal), a => (DoubleComplex)a);
				}
				else
				{
					using var _tempVal = new DenseVector<DoubleComplex>(A.NRows, A.OnHost);
					EigenSolve(_tempVal, A, null, null, mode: EigMode.NoVector);
					vals = RT.CopyOutArray(_tempVal);
				}
				vals = vals.OrderByDescending(v => v.Abs()).ToArray();
				var list = new List<DoubleComplex>(order.Length);
				foreach (var item in order)
				{
					list.Add(vals[item]);
				}
				orderVal = list.ToArray();
			}
			
			var func = onHost ? new ISolver.DelegateSchurDecomposition<T>(CPU.SchurDecomposition) : GPU.SchurDecomposition;
			int find;
			if (U is null)
				find = func(A.IntRows, A.Pointer, A.IntLeadDim, A.Pointer, 1, EigMode.NoVector, orderVal);
			else
				find = func(A.IntRows, A.Pointer, A.IntLeadDim, U.Pointer, U.IntLeadDim, EigMode.Vector, orderVal);
			return find > 0 ? find : orderVal.Length;
		}
		#endregion


		#region linear solve
		/// <summary>
		/// Solve a series of linear systems: $A X = B$, where each column pair of X and <paramref name="B"/> is a linear system. <br/>
		/// Both <paramref name="A"/> and <paramref name="B"/> are in-place where <paramref name="A"/> is replaced by its LU decomposition and <paramref name="B"/> the solution X.
		/// </summary>
		/// <typeparam name="T">input data type, see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <param name="A">the coefficient <see cref="DenseMatrix{T}"/></param>
		/// <param name="B">each column of this <see cref="DenseMatrix{T}"/> is the vector at right; overwritten by solution X in the end</param>
		/// <exception cref="ArgumentNullException">if any of the array is null</exception>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type</exception>
		/// <exception cref="StatusException">if the calculation of CUDA Solver onHosts returns error status</exception>
		/// <exception cref="MatrixAlgorithmException">if the internal calculation fails, mainly caused when <paramref name="A"/> has no inverse</exception>
		public static void LinearSolve<T>(DenseMatrix<T> A, DenseMatrix<T> B) where T : struct, IComparable<T>
		{
			if (A is null || A == PureArray<T>.EmptyDnMat)
				throw new ArgumentNullException(nameof(A), Resource.ArrayCannotNull);
			if (B is null || B == PureArray<T>.EmptyDnMat)
				throw new ArgumentNullException(nameof(B), Resource.ArrayCannotNull);
			if (A.NRows != A.NCols || A.NCols != B.NCols)
				throw new ArgumentException(Resource.CannotOperate + $"{nameof(A)} is not square or {nameof(B)} has incompatible leading dimension.");

			var onHost = CudaCSharpHelpers.CheckOnHost(A, B);
			var func = onHost ? new ISolver.DelegateLinearSolve<T>(CPU.LinearSolve) : GPU.LinearSolve;
			func(A.IntRows, B.IntRows, A.Pointer, A.IntLeadDim, B.Pointer, B.IntLeadDim);
		}
		#endregion


		#region sparse matrix shift inverse eigen
		/*
		/// <summary>
		/// Calculate one eigen-pair of a <see cref="SparseMatrix{T}"/> with <see cref="SparseMatrixFormat.CSR"/> $A \vec{x} = \lambda \vec{x}$ using shift-inverse method.
		/// </summary>
		/// <typeparam name="T">input data type, see <see cref="PureArray{T}"/> for supported data types</typeparam>
		/// <typeparam name="TReal">the real corresponding of <typeparamref name="T"/></typeparam>
		/// <param name="A">the input <see cref="SparseMatrix{T}"/> to calculate eigen-pair</param>
		/// <param name="λ0">scalar of type <typeparamref name="T"/> as initial eigenvalue guess</param>
		/// <param name="maxIter">max number of iterations</param>
		/// <param name="tolerance">the tolerance for judging convergence</param>
		/// <returns>The eigenvalue nearest to <paramref name="λ0"/> and the corresponding eigen-pair.</returns>
		/// <exception cref="ArgumentNullException">if <paramref name="A"/> is null</exception>
		/// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not one of the supported type or the matrix is not square CSR matrix</exception>
		/// <exception cref="StatusException">if the calculation of CUDA Solver <c>csreigvsi</c> returns error status</exception>
		/// <remarks>The basic process of the shift-inverse method:
		/// <list type="number">
		/// <item>solve $(A - \lambda_0 I) \vec{x}^{(k+1)} = \vec{x}^{(k)}$ and normalize $\vec{x}^{(k+1)}$</item>
		/// <item>compute approximate eigenvalue $\mu^{(k+1)} = {\vec{x}^{(k+1)}}^H  A  \vec{x}^{(k+1)}$</item>
		/// <item>if $\|A\vec{x}^{(k+1)} - \mu^{(k+1)} \vec{x}^{(k+1)}\| \ge tolerance$ then repeat at $k+1$</item>
		/// </list>
		/// </remarks>
		public static (T value, DenseVector<T> vector) SparseEigen<T, TReal>(SparseMatrix<T> A, T λ0, int maxIter, float tolerance) where T : struct, IComparable<T> where TReal : struct, IComparable<TReal>
		{
			if (A is null || A == PureArray<T>.EmptyDnMat)
				throw new ArgumentNullException(nameof(A), Resource.ArrayCannotNull);
			if (A.NRows != A.NCols)
				throw new ArgumentException(Resource.MatMustSquare, nameof(A));
			if (A.Format != SparseMatrixFormat.CSR)
				throw new ArgumentException(string.Format(Resource.Culture, Resource.SpMatMustFormat, SparseMatrixFormat.CSR), nameof(A));

			var descrA = SparseBLAS.SparseMatrixDescription.Create(A);
			using var initial = new DenseVector<T>(A.NRows);
			inital.FillWithRandoms();
			using var resVec = new PurePointer<T>(A.NRows);
			T resVal = default;

			Sparse.NativeMethods.SpEigFunc<T, TReal> func = (default(T).ToDataType()) switch
			{
				DataType.RealSingle => new Sparse.NativeMethods.SpEigFunc<float, float>(Sparse.NativeMethods.cusolverSpScsreigvsi) as Sparse.NativeMethods.SpEigFunc<T, TReal>,
				DataType.RealDouble => new Sparse.NativeMethods.SpEigFunc<double, double>(Sparse.NativeMethods.cusolverSpDcsreigvsi) as Sparse.NativeMethods.SpEigFunc<T, TReal>,
				DataType.ComplexSingle => new Sparse.NativeMethods.SpEigFunc<FloatComplex, float>(Sparse.NativeMethods.cusolverSpCcsreigvsi) as Sparse.NativeMethods.SpEigFunc<T, TReal>,
				DataType.ComplexDouble => new Sparse.NativeMethods.SpEigFunc<DoubleComplex, double>(Sparse.NativeMethods.cusolverSpZcsreigvsi) as Sparse.NativeMethods.SpEigFunc<T, TReal>,
				_ => throw new NotSupportedException(Resource.DataTypeNotSupport)
			};
			func(Context.DenseHandle, A.IntRows, A.IntNNZ, descrA, A.Pointer, A.RowPointer, A.ColumnPointer, λ0, inital.Pointer, maxIter, tolerance.GenericConvert<TReal, float>(), ref resVal, resVec).Check();

			return (resVal, new DenseVector<T>(resVec, A.NRows));
		}
		*/
		#endregion
	}
}
