using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Text;
using Althea;
using Althea.Runtime;

namespace Althea.Solver.Cuda.Dense
{
	/// <summary>
	/// The CUDA Solver's dense library native methods
	/// </summary>
	public static class NativeMethods
	{
		/// <summary>
		/// The CUDA Solver's dense library name
		/// </summary>
		public const string CUSOLVE_API_DLL_NAME = @"cusolver";

		#region Create and destroy
		/// <summary>
		/// This function initializes the cuSolverDN library and creates a handle on the cuSolverDN
		/// context. It must be called before any other cuSolverDN API function is invoked. It
		/// allocates hardware resources necessary for accessing the GPU
		/// </summary>
		/// <param name="handle">the pointer to the handle to the cuSolverDN context.</param>
		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverDnCreate(ref IntPtr handle);

		/// <summary>
		/// This function releases CPU-side resources used by the cuSolverDN library.
		/// </summary>
		/// <param name="handle">handle to the cuSolverDN library context.</param>
		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverDnDestroy(IntPtr handle);
		#endregion


		/// <summary>
		/// The helper function of <see cref="geqrfFunc"/>.
		/// </summary>
		/// <param name="handle">handle to the cuSolverDN library context.</param>
		/// <param name="m">number of rows of matrix <paramref name="A"/>.</param>
		/// <param name="n">number of columns of matrix <paramref name="A"/>.</param>
		/// <param name="A">array of dimension <paramref name="lda"/>×<paramref name="n"/> with <paramref name="lda"/> is not less than <c>max(1, <paramref name="m"/>)</c>.</param>
		/// <param name="lda">leading dimension of two-dimensional array used to store matrix <paramref name="A"/>.</param>
		/// <param name="Lwork">output size of Workspace in T rather than bytes</param>
		/// <returns><see cref="Status"/></returns>
		internal delegate Status geqrfBufFunc(IntPtr handle, int m, int n, IntPtr A, int lda, ref int Lwork);

		/// <summary>
		/// This function computes the QR factorization of matrix.
		/// </summary>
		/// <param name="handle">handle to the cuSolverDN library context.</param>
		/// <param name="m">number of rows of matrix <paramref name="A"/>.</param>
		/// <param name="n">number of columns of matrix <paramref name="A"/>.</param>
		/// <param name="A">matrix of dimension <paramref name="lda"/>×<paramref name="n"/> with <paramref name="lda"/> is not less than <c>max(1, <paramref name="m"/>)</c>. The matrix R is overwritten in upper triangular part of A, including diagonal elements.</param>
		/// <param name="lda">leading dimension of two-dimensional array used to store matrix <paramref name="A"/>.</param>
		/// <param name="TAU">pre-allocated array of dimension at least<c> min(m,n)</c>, contains the scalars of elementary reflection vectors.</param>
		/// <param name="Workspace">working space, T-typed array of size <paramref name="Lwork"/>.</param>
		/// <param name="Lwork">size of <paramref name="Workspace"/> in T rather than bytes</param>
		/// <param name="devInfo">if <c>devInfo = 0</c>, the LU factorization is successful. if <c>devInfo = -i</c>, the i-th parameter is wrong (not counting handle).</param>
		/// <returns><see cref="Status"/></returns>
		internal delegate Status geqrfFunc(IntPtr handle, int m, int n, IntPtr A, int lda, IntPtr TAU, IntPtr Workspace, int Lwork, IntPtr devInfo);

		/// <summary>
		/// The helper function of <see cref="orgqrFunc"/>.
		/// </summary>
		/// <param name="handle">handle to the cuSolverDN library context.</param>
		/// <param name="m">number of rows of matrix <paramref name="A"/>.</param>
		/// <param name="n">number of columns of matrix <paramref name="A"/>.</param>
		/// <param name="k">number of elementary reflections whose product defines the matrix Q, <paramref name="k"/>≤<paramref name="n"/></param>
		/// <param name="A">array of dimension <paramref name="lda"/>×<paramref name="n"/> with <paramref name="lda"/> is not less than <c>max(1, <paramref name="m"/>)</c>.</param>
		/// <param name="lda">leading dimension of two-dimensional array used to store matrix <paramref name="A"/>.</param>
		/// <param name="tau">the scalars of elementary reflection vectors</param>
		/// <param name="lwork">size of working array</param>
		/// <returns><see cref="Status"/></returns>
		internal delegate Status orgqrBufFunc(IntPtr handle, int m, int n, int k, [In] IntPtr A, int lda, [In] IntPtr tau, ref int lwork);

		/// <summary>
		/// This function overwrites m×n matrix <paramref name="A"/> by <c>Q = H(1) * H(2) * ... * H(k)</c> where Q is a unitary matrix formed by a sequence of elementary reflection vectors stored in <paramref name="A"/>.
		/// </summary>
		/// <param name="handle">handle to the cuSolverDN library context.</param>
		/// <param name="m">number of rows of matrix <paramref name="A"/>.</param>
		/// <param name="n">number of columns of matrix <paramref name="A"/>.</param>
		/// <param name="k">number of elementary reflections whose product defines the matrix Q, <paramref name="k"/>≤<paramref name="n"/></param>
		/// <param name="A">array of dimension <paramref name="lda"/>×<paramref name="n"/> with <paramref name="lda"/> is not less than <c>max(1, <paramref name="m"/>)</c>.</param>
		/// <param name="lda">leading dimension of two-dimensional array used to store matrix <paramref name="A"/>.</param>
		/// <param name="tau">the scalars of elementary reflection vectors</param>
		/// <param name="work">working space, T-typed array of size <paramref name="lwork"/></param>
		/// <param name="lwork">size of working array</param>
		/// <param name="devInfo">if <c>info = 0</c>, the process is successful. if <c>info = -i</c>, the i-th parameter is wrong (not counting handle).</param>
		/// <returns><see cref="Status"/></returns>
		internal delegate Status orgqrFunc(IntPtr handle, int m, int n, int k, IntPtr A, int lda, IntPtr tau, IntPtr work, int lwork, IntPtr devInfo);
		#region QR
		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverDnSgeqrf_bufferSize(IntPtr handle, int m, int n, IntPtr A, int lda, ref int Lwork);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverDnDgeqrf_bufferSize(IntPtr handle, int m, int n, IntPtr A, int lda, ref int Lwork);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverDnCgeqrf_bufferSize(IntPtr handle, int m, int n, IntPtr A, int lda, ref int Lwork);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverDnZgeqrf_bufferSize(IntPtr handle, int m, int n, IntPtr A, int lda, ref int Lwork);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverDnSgeqrf(IntPtr handle, int m, int n, IntPtr A, int lda, IntPtr TAU, IntPtr Workspace, int Lwork, IntPtr devInfo);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverDnDgeqrf(IntPtr handle, int m, int n, IntPtr A, int lda, IntPtr TAU, IntPtr Workspace, int Lwork, IntPtr devInfo);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverDnCgeqrf(IntPtr handle, int m, int n, IntPtr A, int lda, IntPtr TAU, IntPtr Workspace, int Lwork, IntPtr devInfo);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverDnZgeqrf(IntPtr handle, int m, int n, IntPtr A, int lda, IntPtr TAU, IntPtr Workspace, int Lwork, IntPtr devInfo);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverDnSorgqr_bufferSize(IntPtr handle, int m, int n, int k, [In] IntPtr A, int lda, [In] IntPtr tau, ref int lwork);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverDnDorgqr_bufferSize(IntPtr handle, int m, int n, int k, [In] IntPtr A, int lda, [In] IntPtr tau, ref int lwork);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverDnCorgqr_bufferSize(IntPtr handle, int m, int n, int k, [In] IntPtr A, int lda, [In] IntPtr tau, ref int lwork);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverDnZorgqr_bufferSize(IntPtr handle, int m, int n, int k, [In] IntPtr A, int lda, [In] IntPtr tau, ref int lwork);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverDnSorgqr(IntPtr handle, int m, int n, int k, IntPtr A, int lda, IntPtr tau, IntPtr work, int lwork, IntPtr devInfo);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverDnDorgqr(IntPtr handle, int m, int n, int k, IntPtr A, int lda, IntPtr tau, IntPtr work, int lwork, IntPtr devInfo);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverDnCorgqr(IntPtr handle, int m, int n, int k, IntPtr A, int lda, IntPtr tau, IntPtr work, int lwork, IntPtr devInfo);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverDnZorgqr(IntPtr handle, int m, int n, int k, IntPtr A, int lda, IntPtr tau, IntPtr work, int lwork, IntPtr devInfo);
		#endregion

		/// <summary>
		/// The helper function of <see cref="getrfFunc"/>.
		/// </summary>
		/// <param name="handle">handle to the cuSolverDN library context.</param>
		/// <param name="m">number of rows of matrix.</param>
		/// <param name="n">number of columns of matrix <paramref name="A"/>.</param>
		/// <param name="A">array of dimension <paramref name="lda"/>×<paramref name="n"/> with <paramref name="lda"/> is not less than <c>max(1, <paramref name="m"/>)</c>.</param>
		/// <param name="lda">leading dimension of two-dimensional array used to store matrix <paramref name="A"/>.</param>
		/// <param name="Lwork">size of Workspace</param>
		/// <returns><see cref="Status"/></returns>
		internal delegate Status getrfBufFunc(IntPtr handle, int m, int n, IntPtr A, int lda, ref int Lwork);

		/// <summary>
		/// This function computes the LU factorization of a <paramref name="m"/>×<paramref name="n"/> matrix <paramref name="A"/> $P A=L U$ where P is a permutation matrix, L is a lower triangular matrix with unit diagonal, and U is an upper triangular matrix.
		/// </summary>
		/// <param name="handle">handle to the cuSolverDN library context.</param>
		/// <param name="m">number of rows of matrix <paramref name="A"/>.</param>
		/// <param name="n">number of columns of matrix <paramref name="A"/>.</param>
		/// <param name="A">array of dimension <paramref name="lda"/>×<paramref name="n"/> with <paramref name="lda"/> is not less than <c>max(1, <paramref name="m"/>)</c>.</param>
		/// <param name="lda">leading dimension of two-dimensional array used to store matrix A.</param>
		/// <param name="Workspace">working space, array of size <c>Lwork</c>.</param>
		/// <param name="devIpiv">array of size at least <c>min(<paramref name="m"/>, <paramref name="n"/>)</c>, containing pivot indices.</param>
		/// <param name="devInfo">if <c>devInfo = 0</c>, the LU factorization is successful; if <c>devInfo = -i</c>, the i-th parameter is wrong; if <c>devInfo = i</c>, the <c>U(i,i) = 0</c>.</param>
		/// <returns><see cref="Status"/></returns>
		internal delegate Status getrfFunc(IntPtr handle, int m, int n, IntPtr A, int lda, IntPtr Workspace, IntPtr devIpiv, IntPtr devInfo);
		#region LU Factorization
		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverDnSgetrf_bufferSize(IntPtr handle, int m, int n, IntPtr A, int lda, ref int Lwork);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverDnDgetrf_bufferSize(IntPtr handle, int m, int n, IntPtr A, int lda, ref int Lwork);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverDnCgetrf_bufferSize(IntPtr handle, int m, int n, IntPtr A, int lda, ref int Lwork);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverDnZgetrf_bufferSize(IntPtr handle, int m, int n, IntPtr A, int lda, ref int Lwork);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverDnSgetrf(IntPtr handle, int m, int n, IntPtr A, int lda, IntPtr Workspace, IntPtr devIpiv, IntPtr devInfo);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverDnDgetrf(IntPtr handle, int m, int n, IntPtr A, int lda, IntPtr Workspace, IntPtr devIpiv, IntPtr devInfo);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverDnCgetrf(IntPtr handle, int m, int n, IntPtr A, int lda, IntPtr Workspace, IntPtr devIpiv, IntPtr devInfo);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverDnZgetrf(IntPtr handle, int m, int n, IntPtr A, int lda, IntPtr Workspace, IntPtr devIpiv, IntPtr devInfo);
		#endregion

		/// <summary>
		/// This function solves a linear system of multiple right-hand sides $A^{\text{op}} X = B$.
		/// </summary>
		/// <param name="handle">handle to the cuSolverDN library context.</param>
		/// <param name="op">operation A^{\text{op}} that is non- or (conj.) transpose.</param>
		/// <param name="n">number of rows and columns of matrix A.</param>
		/// <param name="nrhs">number of right-hand sides.</param>
		/// <param name="A">array of dimension <paramref name="lda"/>×<paramref name="n"/> with <paramref name="lda"/> is not less than <c>max(1, <paramref name="n"/>)</c>.</param>
		/// <param name="lda">leading dimension of two-dimensional array used to store matrix <paramref name="A"/>.</param>
		/// <param name="devIpiv">array of size at least n, containing pivot indices.</param>
		/// <param name="B">array of dimension <paramref name="ldb"/>×<paramref name="n"/> with <paramref name="lda"/> is not less than <c>max(1, <paramref name="n"/>)</c>.</param>
		/// <param name="ldb">leading dimension of two-dimensional array used to store matrix <paramref name="B"/>.</param>
		/// <param name="devInfo">if devInfo = 0, the operation is successful. if devInfo = -i, the i-th parameter is wrong.</param>
		/// <returns><see cref="Status"/></returns>
		internal delegate Status getrsFunc(IntPtr handle, MatrixOperation op, int n, int nrhs, IntPtr A, int lda, IntPtr devIpiv, IntPtr B, int ldb, IntPtr devInfo);
		#region LU solve
		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverDnSgetrs(IntPtr handle, MatrixOperation trans, int n, int nrhs, IntPtr A, int lda, IntPtr devIpiv, IntPtr B, int ldb, IntPtr devInfo);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverDnDgetrs(IntPtr handle, MatrixOperation trans, int n, int nrhs, IntPtr A, int lda, IntPtr devIpiv, IntPtr B, int ldb, IntPtr devInfo);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverDnCgetrs(IntPtr handle, MatrixOperation trans, int n, int nrhs, IntPtr A, int lda, IntPtr devIpiv, IntPtr B, int ldb, IntPtr devInfo);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverDnZgetrs(IntPtr handle, MatrixOperation trans, int n, int nrhs, IntPtr A, int lda, IntPtr devIpiv, IntPtr B, int ldb, IntPtr devInfo);
		#endregion


		/// <summary>
		/// This function is the helper function of <see cref="syevdFunc"/>.
		/// </summary>
		/// <param name="handle">handle to the cuSolverDN library context.</param>
		/// <param name="jobz">specifies options to either compute eigenvalue only or compute eigen-pair.</param>
		/// <param name="uplo">specifies which part of <paramref name="A"/> is stored.</param>
		/// <param name="n">number of rows (or columns) of matrix <paramref name="A"/>.</param>
		/// <param name="A">array of dimension <paramref name="lda"/>×<paramref name="n"/> with <paramref name="lda"/> is not less than <c>max(1, <paramref name="n"/>)</c>.</param>
		/// <param name="lda">leading dimension of two-dimensional array used to store matrix <paramref name="A"/>.</param>
		/// <param name="eigVecOut">a real array of length <paramref name="n"/>. The eigenvalue values of <paramref name="A"/>, sorted so that <c>W(i) >= W(i+1)</c>.</param>
		/// <param name="lwork">size of work buffer.</param>
		/// <returns><see cref="Status"/></returns>
		internal delegate Status syevdBufFunc(IntPtr handle, EigMode jobz, MatrixFillMode uplo, int n, IntPtr A, int lda, IntPtr eigVecOut, ref int lwork);

		/// <summary>
		/// This function computes eigenvalues and eigenvectors of a symmetric <paramref name="n"/>×<paramref name="n"/> matrix <paramref name="A"/>.
		/// </summary>
		/// <param name="handle">handle to the cuSolverDN library context.</param>
		/// <param name="jobz">specifies options to either compute eigenvalue only or compute eigen-pair.</param>
		/// <param name="uplo">specifies which part of <paramref name="A"/> is stored.</param>
		/// <param name="n">number of rows (or columns) of matrix <paramref name="A"/>.</param>
		/// <param name="A">array of dimension <paramref name="lda"/>×<paramref name="n"/> with <paramref name="lda"/> is not less than <c>max(1, <paramref name="n"/>)</c>.</param>
		/// <param name="lda">leading dimension of two-dimensional array used to store matrix <paramref name="A"/>.</param>
		/// <param name="eigVecOut">a real array of length <paramref name="n"/>. The eigenvalue values of <paramref name="A"/>, sorted so that <c>W(i) >= W(i+1)</c>.</param>
		/// <param name="work">work buffer</param>
		/// <param name="lwork">size of <paramref name="work"/>.</param>
		/// <param name="info">if <c>devInfo = 0</c>, the operation is successful. if <c>devInfo = -i</c>, the i-th
		/// parameter is wrong. if <c>devInfo = i > 0</c>, devInfo indicates either <c>potrf</c> or <c>syevd</c> is wrong.</param>
		/// <returns><see cref="Status"/></returns>
		internal delegate Status syevdFunc(IntPtr handle, EigMode jobz, MatrixFillMode uplo, int n, IntPtr A, int lda, IntPtr eigVecOut, IntPtr work, int lwork, IntPtr info);
		#region standard symmetric (Hermitian) eigenvalue solver by divide-and-conquer
		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverDnSsyevd_bufferSize(IntPtr handle, EigMode jobz, MatrixFillMode uplo, int n, IntPtr A, int lda, IntPtr eigVecOut, ref int lwork);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverDnDsyevd_bufferSize(IntPtr handle, EigMode jobz, MatrixFillMode uplo, int n, IntPtr A, int lda, IntPtr eigVecOut, ref int lwork);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverDnCheevd_bufferSize(IntPtr handle, EigMode jobz, MatrixFillMode uplo, int n, IntPtr A, int lda, IntPtr eigVecOut, ref int lwork);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverDnZheevd_bufferSize(IntPtr handle, EigMode jobz, MatrixFillMode uplo, int n, IntPtr A, int lda, IntPtr eigVecOut, ref int lwork);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverDnSsyevd(IntPtr handle, EigMode jobz, MatrixFillMode uplo, int n, IntPtr A, int lda, IntPtr eigVecOut, IntPtr work, int lwork, IntPtr info);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverDnDsyevd(IntPtr handle, EigMode jobz, MatrixFillMode uplo, int n, IntPtr A, int lda, IntPtr eigVecOut, IntPtr work, int lwork, IntPtr info);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverDnCheevd(IntPtr handle, EigMode jobz, MatrixFillMode uplo, int n, IntPtr A, int lda, IntPtr eigVecOut, IntPtr work, int lwork, IntPtr info);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverDnZheevd(IntPtr handle, EigMode jobz, MatrixFillMode uplo, int n, IntPtr A, int lda, IntPtr eigVecOut, IntPtr work, int lwork, IntPtr info);
		#endregion


		/// <summary>
		/// This function is the helper function of <see cref="sygvdFunc"/>.
		/// </summary>
		/// <param name="handle">handle to the cuSolverDN library context.</param>
		/// <param name="itype">Specifies the problem type to be solved.</param>
		/// <param name="jobz">specifies options to either compute eigenvalue only or compute eigen-pair.</param>
		/// <param name="uplo">specifies which part of <paramref name="A"/> and <paramref name="B"/> are stored.</param>
		/// <param name="n">number of rows (or columns) of matrix <paramref name="A"/> and <paramref name="B"/>.</param>
		/// <param name="A">array of dimension <paramref name="lda"/>×<paramref name="n"/> with <paramref name="lda"/> is not less than <c>max(1, <paramref name="n"/>)</c>.</param>
		/// <param name="lda">leading dimension of two-dimensional array used to store matrix <paramref name="A"/>.</param>
		/// <param name="B">array of dimension <paramref name="ldb"/>×<paramref name="n"/> with <paramref name="ldb"/> is not less than <c>max(1, <paramref name="n"/>)</c>.</param>
		/// <param name="ldb">leading dimension of two-dimensional array used to store matrix <paramref name="B"/>.</param>
		/// <param name="eigVecOut">a real array of length <paramref name="n"/>. The eigenvalue values of <paramref name="A"/>, sorted so that <c>W(i) >= W(i+1)</c>.</param>
		/// <param name="lwork">size of work</param>
		/// <returns><see cref="Status"/></returns>
		internal delegate Status sygvdBufFunc(IntPtr handle, EigType itype, EigMode jobz, MatrixFillMode uplo, int n, IntPtr A, int lda, IntPtr B, int ldb, IntPtr eigVecOut, ref int lwork);

		/// <summary>
		/// This function computes eigenvalues and eigenvectors of a symmetric <paramref name="n"/>×<paramref name="n"/> matrix <paramref name="A"/>.
		/// </summary>
		/// <param name="handle">handle to the cuSolverDN library context.</param>
		/// <param name="jobz">specifies options to either compute eigenvalue only or compute eigen-pair.</param>
		/// <param name="itype">Specifies the problem type to be solved.</param>
		/// <param name="uplo">specifies which part of <paramref name="A"/> is stored.</param>
		/// <param name="n">number of rows (or columns) of matrix <paramref name="A"/>.</param>
		/// <param name="A">array of dimension <paramref name="lda"/>×<paramref name="n"/> with <paramref name="lda"/> is not less than <c>max(1, <paramref name="n"/>)</c>.</param>
		/// <param name="lda">leading dimension of two-dimensional array used to store matrix <paramref name="A"/>.</param>
		/// <param name="B">array of dimension <paramref name="ldb"/>×<paramref name="n"/> with <paramref name="ldb"/> is not less than <c>max(1, <paramref name="n"/>)</c>.</param>
		/// <param name="ldb">leading dimension of two-dimensional array used to store matrix <paramref name="B"/>.</param>
		/// <param name="eigVecOut">a real array of length <paramref name="n"/>. The eigenvalue values of <paramref name="A"/>, sorted so that <c>W(i) >= W(i+1)</c>.</param>
		/// <param name="work">work buffer</param>
		/// <param name="lwork">size of <paramref name="work"/>.</param>
		/// <param name="info">if <c>devInfo = 0</c>, the operation is successful. if <c>devInfo = -i</c>, the i-th
		/// parameter is wrong. if <c>devInfo = i > 0</c>, devInfo indicates either <c>potrf</c> or <c>sygvd</c> is wrong.</param>
		/// <returns><see cref="Status"/></returns>
		internal delegate Status sygvdFunc(IntPtr handle, EigType itype, EigMode jobz, MatrixFillMode uplo, int n, IntPtr A, int lda, IntPtr B, int ldb, IntPtr eigVecOut, IntPtr work, int lwork, IntPtr info);
		#region generalized symmetric (Hermitian) eigenvalue solver by divide-and-conquer
		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverDnSsygvd_bufferSize(IntPtr handle, EigType itype, EigMode jobz, MatrixFillMode uplo, int n, IntPtr A, int lda, IntPtr B, int ldb, IntPtr W, ref int lwork);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverDnDsygvd_bufferSize(IntPtr handle, EigType itype, EigMode jobz, MatrixFillMode uplo, int n, IntPtr A, int lda, IntPtr B, int ldb, IntPtr W, ref int lwork);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverDnChegvd_bufferSize(IntPtr handle, EigType itype, EigMode jobz, MatrixFillMode uplo, int n, IntPtr A, int lda, IntPtr B, int ldb, IntPtr W, ref int lwork);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverDnZhegvd_bufferSize(IntPtr handle, EigType itype, EigMode jobz, MatrixFillMode uplo, int n, IntPtr A, int lda, IntPtr B, int ldb, IntPtr W, ref int lwork);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverDnSsygvd(IntPtr handle, EigType itype, EigMode jobz, MatrixFillMode uplo, int n, IntPtr A, int lda, IntPtr B, int ldb, IntPtr W, IntPtr work, int lwork, IntPtr info);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverDnDsygvd(IntPtr handle, EigType itype, EigMode jobz, MatrixFillMode uplo, int n, IntPtr A, int lda, IntPtr B, int ldb, IntPtr W, IntPtr work, int lwork, IntPtr info);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverDnChegvd(IntPtr handle, EigType itype, EigMode jobz, MatrixFillMode uplo, int n, IntPtr A, int lda, IntPtr B, int ldb, IntPtr W, IntPtr work, int lwork, IntPtr info);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverDnZhegvd(IntPtr handle, EigType itype, EigMode jobz, MatrixFillMode uplo, int n, IntPtr A, int lda, IntPtr B, int ldb, IntPtr W, IntPtr work, int lwork, IntPtr info);
		#endregion


		/// <summary>
		/// The helper function of <see cref="svdFunc"/>.
		/// </summary>
		/// <param name="handle">handle to the cuSolverDN library context.</param>
		/// <param name="m">number of rows of matrix</param>
		/// <param name="n">number of columns of matrix</param>
		/// <param name="lenwork">returned working array length</param>
		/// <returns><see cref="Status"/></returns>
		internal delegate Status svdBufFunc(IntPtr handle, int m, int n, ref int lenwork);

		/// <summary>
		/// This function computes the singular value decomposition (SVD) of a matrix <paramref name="A"/> and corresponding the left and/or right singular vectors.
		/// </summary>
		/// <param name="handle">handle to the cuSolverDN library context.</param>
		/// <param name="jobu">specifies options for computing all or part of the matrix <paramref name="U"/>:
		/// <list type="bullet">
		/// <item><description>'A' → All <paramref name="m"/> columns are stored</description></item>
		/// <item><description>'S' → The first <c>min(<paramref name="m"/>, <paramref name="n"/>)</c> columns are stored</description></item>
		/// <item><description>'O' → The first <c>min(<paramref name="m"/>, <paramref name="n"/>)</c> columns are overwritten in <paramref name="A"/></description></item>
		/// <item><description>'N' → None of the columns are stored</description></item>
		/// </list>
		/// </param>
		/// <param name="jobvt">specifies options for computing all or part of the matrix <paramref name="VT"/>, same as <paramref name="jobu"/></param>
		/// <param name="m">number of rows of matrix</param>
		/// <param name="n">number of columns of matrix</param>
		/// <param name="A">matrix with size <paramref name="m"/>×<paramref name="n"/> and leading dimension <paramref name="lda"/></param>
		/// <param name="lda">leading dimension of <paramref name="A"/></param>
		/// <param name="S">output singular values of size <c>min(<paramref name="m"/>, <paramref name="n"/>)</c>, must be pre-allocated and of corresponding real type</param>
		/// <param name="U">left unitary matrix with size <paramref name="ldu"/>×<paramref name="m"/>, must be pre-allocated</param>
		/// <param name="ldu">leading dimension of <paramref name="U"/></param>
		/// <param name="VT">left unitary matrix with size <paramref name="ldvt"/>×<paramref name="n"/>, must be pre-allocated</param>
		/// <param name="ldvt">leading dimension of <paramref name="VT"/></param>
		/// <param name="work">working buffer with size <paramref name="lenwork"/></param>
		/// <param name="lenwork">size of working buffer</param>
		/// <param name="realwork">real-typed working buffer of size <c>min(<paramref name="m"/>, <paramref name="n"/>) - 1</c> which contains the unconverged super-diagonal elements of an upper bidiagonal matrix if <c><paramref name="devInfo"/> &gt; 0</c>.</param>
		/// <param name="devInfo">if <c><paramref name="devInfo"/> == 0</c>, the operation is successful; if <c><paramref name="devInfo"/> == -i</c>, the <c>i</c><sup>th</sup> parameter is wrong (not counting handle); if <c><paramref name="devInfo"/> &gt; 0</c>, it indicates how many super-diagonals of an intermediate bidiagonal form did not converge to zero.</param>
		/// <returns><see cref="Status"/></returns>
		/// <remarks>Only support <paramref name="m"/>&gt;<paramref name="n"/></remarks>
		internal delegate Status svdFunc(IntPtr handle, sbyte jobu, sbyte jobvt, int m, int n, IntPtr A, int lda, IntPtr S, IntPtr U, int ldu, IntPtr VT, int ldvt, IntPtr work, int lenwork, IntPtr realwork, IntPtr devInfo);
		#region singular value decomposition
		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverDnSgesvd_bufferSize(IntPtr handle, int m, int n, ref int lwork);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverDnDgesvd_bufferSize(IntPtr handle, int m, int n, ref int lwork);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverDnCgesvd_bufferSize(IntPtr handle, int m, int n, ref int lwork);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverDnZgesvd_bufferSize(IntPtr handle, int m, int n, ref int lwork);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverDnSgesvd(IntPtr handle, sbyte jobu, sbyte jobvt, int m, int n, IntPtr A, int lda, IntPtr S, IntPtr U, int ldu, IntPtr VT, int ldvt, IntPtr work, int lwork, IntPtr rwork, IntPtr devInfo);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverDnDgesvd(IntPtr handle, sbyte jobu, sbyte jobvt, int m, int n, IntPtr A, int lda, IntPtr S, IntPtr U, int ldu, IntPtr VT, int ldvt, IntPtr work, int lwork, IntPtr rwork, IntPtr devInfo);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverDnCgesvd(IntPtr handle, sbyte jobu, sbyte jobvt, int m, int n, IntPtr A, int lda, IntPtr S, IntPtr U, int ldu, IntPtr VT, int ldvt, IntPtr work, int lwork, IntPtr rwork, IntPtr devInfo);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverDnZgesvd(IntPtr handle, sbyte jobu, sbyte jobvt, int m, int n, IntPtr A, int lda, IntPtr S, IntPtr U, int ldu, IntPtr VT, int ldvt, IntPtr work, int lwork, IntPtr rwork, IntPtr devInfo);
		#endregion
	}
}
namespace Althea.Solver.Cuda.Sparse
{
	/// <summary>
	/// The CUDA Solver's dense library native methods
	/// </summary>
	public static class NativeMethods
	{
		/// <summary>
		/// The CUDA Solver's sparse library name
		/// </summary>
		public const string CUSOLVE_API_DLL_NAME = @"cusolver";

		#region create and destroy
		/// <summary>
		/// This function initializes the cuSolverSP library and creates a handle on the cuSolver
		/// context. It must be called before any other cuSolverSP API function is invoked. It
		/// allocates hardware resources necessary for accessing the GPU.
		/// </summary>
		/// <param name="handle">the pointer to the handle to the cuSolverSP context.</param>
		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverSpCreate(ref IntPtr handle);

		/// <summary>
		/// This function releases CPU-side resources used by the cuSolverSP library.
		/// </summary>
		/// <param name="handle">the handle to the cuSolverSP context.</param>
		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverSpDestroy(IntPtr handle);
		#endregion

		/// <summary>
		/// This function solves the linear system $A \vec{x} = \vec{b}$ where A is an <paramref name="m"/>×<paramref name="m"/> sparse matrix of <see cref="SparseMatrixFormat.CSR"/>. <paramref name="b"/> is the right-hand-side vector of size <paramref name="m"/>, and <paramref name="x"/> is the solution vector of size <paramref name="m"/>. The supported matrix type is <see cref="SparseBlas.Cuda.MatrixType.General"/>. <br/>
		/// The linear system is solved by sparse QR factorization,  $A = Q R$. If A is singular under given tolerance, then some diagonal elements of R is zero, i.e. $|R(j, j)| &lt; \text{tolerance}$ for some <c>j</c>. <br/>
		/// The output parameter singularity is the smallest index of such j. If A is non-singular, singularity is -1. The singularity is base-0, independent of base index of A.For example, if 2nd column of A is the same as first column, then A is singular and singularity = 1 which means <c>R(1,1)≈0</c>.
		/// </summary>
		/// <param name="handle">handle to the cuSolverSP library context.</param>
		/// <param name="m">number of rows and columns of matrix A.</param>
		/// <param name="nnz">number of non-zeros of matrix A.</param>
		/// <param name="descrA">the <see cref="SparseBlas.Cuda.SparseMatrixDescription"/> of matrix A.</param>
		/// <param name="csrValA">array of <paramref name="nnz"/> non-zero elements of matrix A.</param>
		/// <param name="csrRowPtrA">integer array of <paramref name="m"/> + 1 elements that contains the start of every row and the end of the last row plus one.</param>
		/// <param name="csrColIndA">integer array of <paramref name="nnz"/> column indices of the nonzero elements of matrix A.</param>
		/// <param name="b">right hand side vector of size <paramref name="m"/>.</param>
		/// <param name="tol">tolerance to decide if singular or not.</param>
		/// <param name="reorder">no ordering if it is 0. Otherwise, <c>symrcm, symamd, csrmetisnd</c> is used to reduce zero fill-in.</param>
		/// <param name="x">output solution vector of size <paramref name="m"/>.</param>
		/// <param name="singularity">output singularity, it is -1 if A is invertible. Otherwise, it is first index j such that <c>R(1,1)≈0</c></param>
		/// <returns><see cref="Status"/></returns>
		internal delegate Status SpQRSolveFunc<T>(IntPtr handle, int m, int nnz, SparseBlas.Cuda.SparseMatrixDescription descrA, IntPtr csrValA, IntPtr csrRowPtrA, IntPtr csrColIndA, IntPtr b, T tol, int reorder, IntPtr x, ref int singularity);
		#region simple linear solver based on QR
		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverSpScsrlsvqr(IntPtr handle, int m, int nnz, SparseBlas.Cuda.SparseMatrixDescription descrA, IntPtr csrValA, IntPtr csrRowPtrA, IntPtr csrColIndA, IntPtr b, float tol, int reorder, IntPtr x, ref int singularity);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverSpDcsrlsvqr(IntPtr handle, int m, int nnz, SparseBlas.Cuda.SparseMatrixDescription descrA, IntPtr csrValA, IntPtr csrRowPtrA, IntPtr csrColIndA, IntPtr b, double tol, int reorder, IntPtr x, ref int singularity);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverSpCcsrlsvqr(IntPtr handle, int m, int nnz, SparseBlas.Cuda.SparseMatrixDescription descrA, IntPtr csrValA, IntPtr csrRowPtrA, IntPtr csrColIndA, IntPtr b, float tol, int reorder, IntPtr x, ref int singularity);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverSpZcsrlsvqr(IntPtr handle, int m, int nnz, SparseBlas.Cuda.SparseMatrixDescription descrA, IntPtr csrValA, IntPtr csrRowPtrA, IntPtr csrColIndA, IntPtr b, double tol, int reorder, IntPtr x, ref int singularity);
		#endregion

		/// <summary>
		/// This function solves the simple eigenvalue problem $A \vec{x} = \lambda \vec{x}$ by shift-inverse method.
		/// </summary>
		/// <param name="handle">handle to the cuSolverSP library context.</param>
		/// <param name="m">number of rows and columns of matrix A.</param>
		/// <param name="nnz">number of non-zeros of matrix A.</param>
		/// <param name="descrA">the <see cref="SparseBlas.Cuda.SparseMatrixDescription"/> of matrix A.</param>
		/// <param name="csrValA">array of <paramref name="nnz"/> non-zero elements of matrix A.</param>
		/// <param name="csrRowPtrA">integer array of <paramref name="m"/> + 1 elements that contains the start of every row and the end of the last row plus one.</param>
		/// <param name="csrColIndA">integer array of <paramref name="nnz"/> column indices of the nonzero elements of matrix A.</param>
		/// <param name="mu0">initial guess of eigenvalue.</param>
		/// <param name="x0">initial guess of eigenvector, a vector of size <paramref name="m"/>, can be chosen randomly.</param>
		/// <param name="maxIter">maximum iterations in shift-inverse method.</param>
		/// <param name="tol">tolerance for convergence.</param>
		/// <param name="mu">output approximated eigenvalue nearest <paramref name="mu0"/> under tolerance.</param>
		/// <param name="x">output approximated eigenvector of size <paramref name="m"/>.</param>
		/// <returns><see cref="Status"/></returns>
		internal delegate Status SpEigFunc<T, TReal>(IntPtr handle, int m, int nnz, SparseBlas.Cuda.SparseMatrixDescription descrA, [In] IntPtr csrValA, [In] IntPtr csrRowPtrA, [In] IntPtr csrColIndA, T mu0, [In] IntPtr x0, int maxIter, TReal tol, ref T mu, IntPtr x);
		#region shift inverse eigenvalue
		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverSpScsreigvsi(IntPtr handle, int m, int nnz, SparseBlas.Cuda.SparseMatrixDescription descrA, [In] IntPtr csrValA, [In] IntPtr csrRowPtrA, [In] IntPtr csrColIndA, float mu0, [In] IntPtr x0, int maxIter, float tol, ref float mu, IntPtr x);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverSpDcsreigvsi(IntPtr handle, int m, int nnz, SparseBlas.Cuda.SparseMatrixDescription descrA, [In] IntPtr csrValA, [In] IntPtr csrRowPtrA, [In] IntPtr csrColIndA, double mu0, [In] IntPtr x0, int maxIter, double tol, ref double mu, IntPtr x);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverSpCcsreigvsi(IntPtr handle, int m, int nnz, SparseBlas.Cuda.SparseMatrixDescription descrA, [In] IntPtr csrValA, [In] IntPtr csrRowPtrA, [In] IntPtr csrColIndA, FloatComplex mu0, [In] IntPtr x0, int maxIter, float tol, ref FloatComplex mu, IntPtr x);

		[DllImport(CUSOLVE_API_DLL_NAME)]
		//[NativeMethodBoundary]
		internal static extern Status cusolverSpZcsreigvsi(IntPtr handle, int m, int nnz, SparseBlas.Cuda.SparseMatrixDescription descrA, [In] IntPtr csrValA, [In] IntPtr csrRowPtrA, [In] IntPtr csrColIndA, DoubleComplex mu0, [In] IntPtr x0, int maxIter, double tol, ref DoubleComplex mu, IntPtr x);
		#endregion
	}
}
