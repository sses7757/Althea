using System;
using System.Dynamic;
using System.Collections.Generic;

using Althea.Linq;
using Althea.Helpers;
using Althea.NativeTypes;


namespace Althea.LinearAlgebra.Dense
{
	/// <summary>
	/// The abstract class for runtime dense linear algebra API routines 
	/// </summary>
	public abstract partial class AbstractApi : AbstractApiSelector
	{
		#region dynamic invocation
		/// <summary>
		/// Get the dynamic object used to dynamically invoke method(s) not listed explicitly here (the methods extra defined in derived classes)
		/// </summary>
		/// <remarks>
		/// Due to the limitations of dynamic invocation, <c>ref</c>, <c>in</c>, <c>out</c> and <c>ref struct</c>, etc. are not supported and non of the input arguments can be null.<br/>
		/// Since there are internal caching for <see cref="DynamicObject.TryInvokeMember(InvokeMemberBinder, object[], out object)"/>, the average repeated dynamic invocation may cost around 1 microsecond.
		/// </remarks>
		/// <example><code>
		/// long number = AbstractApi.Dynamic.CholeskyDecompose(...);
		/// </code></example>
		public static dynamic Dynamic => singletonDynamic;

		private static readonly DynamicInvocations singletonDynamic = new();

		private sealed class DynamicInvocations : DynamicInvocation
		{
			public override bool TryInvokeMember(InvokeMemberBinder binder, object?[]? args, out object? result)
			{
				result = DynamicInvokeExtraMethod(RecentAPIs, binder.Name, args);
				return true;
			}
		}
		#endregion


		#region support information
		/// <summary>
		/// Check if the given regular-typed locations <paramref name="normals"/> and its real-corresponding locations <paramref name="reals"/> are supported by such operations of this implementation or not.
		/// </summary>
		/// <param name="normals">The regular input type's <see cref="CombinationOfLocations"/>s</param>
		/// <param name="reals">The real input type's <see cref="CombinationOfLocations"/>s</param>
		/// <returns>Whether mix-typed operations indicated by <paramref name="normals"/> and <paramref name="reals"/> are supported by this <see cref="AbstractApi"/>.</returns>
		/// <remarks>
		/// The operations:
		/// <list type="bullet">
		/// <item><see cref="EigenSpecialMatrixHermitian{T, TReal}(SolveVectorMode, long, Storage{TReal}, Storage{T}, long)"/></item>
		/// <item><see cref="EigenGeneralMatrixHermitian_{T, TReal}(GeneralEigenType, SolveVectorMode, long, Storage{TReal}, Storage{T}, long, Storage{T}, long)"/></item>
		/// <item><see cref="SingularValues{T, TReal}(SVDStore, SVDStore, long, long, Storage{T}, long, Storage{TReal}, Storage{T}?, long, Storage{T}?, long)"/></item>
		/// <item>etc.</item>
		/// </list>
		/// </remarks>
		protected abstract bool IsSupportedNormalTypeRealType(Span<CombinationOfLocations> normals, Span<CombinationOfLocations> reals);

		/// <summary>
		/// Check if the given regular-typed locations <paramref name="normals"/> and its complex-corresponding locations <paramref name="complexes"/> are supported by such operations of this implementation or not.
		/// </summary>
		/// <param name="normals">The regular input type's <see cref="CombinationOfLocations"/>s</param>
		/// <param name="complexes">The complex input type's <see cref="CombinationOfLocations"/>s</param>
		/// <returns>Whether mix-typed operations indicated by <paramref name="normals"/> and <paramref name="complexes"/> are supported by this <see cref="AbstractApi"/>.</returns>
		/// <remarks>
		/// The operations:
		/// <list type="bullet">
		/// <item><see cref="EigenSpecialMatrixGeneral{T, TComplex}(SolveVectorMode, long, Storage{TComplex}, Storage{TComplex}?, long, Storage{TComplex}?, long, Storage{T}, long)"/></item>
		/// <item><see cref="EigenGeneralMatrixGeneral{T, TComplex}(GeneralEigenType, SolveVectorMode, long, Storage{TComplex}, Storage{T}, Storage{TComplex}?, long, Storage{TComplex}?, long, Storage{T}, long, Storage{T}, long)"/></item>
		/// <item>etc.</item>
		/// </list>
		/// </remarks>
		protected abstract bool IsSupportedNormalTypeComplexType(Span<CombinationOfLocations> normals, Span<CombinationOfLocations> complexes);

		/// <summary>
		/// Check if the given matrix-unary and index-unary locations are supported by such operations of this implementation or not.
		/// </summary>
		/// <param name="matrix">The <see cref="CombinationOfLocations"/> of the operand matrix</param>
		/// <param name="index">The <see cref="CombinationOfLocations"/> of the operand index array</param>
		/// <param name="indexType">The <see cref="DataType"/> of the operand index array</param>
		/// <returns>Whether mix-typed operations indicated by <paramref name="matrix"/>, <paramref name="index"/> and <paramref name="indexType"/> are supported by this <see cref="AbstractApi"/>.</returns>
		protected abstract bool IsSupportedMatrixUnaryIndexUnary(CombinationOfLocations matrix, CombinationOfLocations index, DataType indexType);

		/// <summary>
		/// Check if the given matrix-binary and index-unary locations are supported by such operations of this implementation or not.
		/// </summary>
		/// <param name="matrix1">The <see cref="CombinationOfLocations"/> of the first operand matrix</param>
		/// <param name="matrix2">The <see cref="CombinationOfLocations"/> of the second operand matrix</param>
		/// <param name="index">The <see cref="CombinationOfLocations"/> of the operand index array</param>
		/// <param name="indexType">The <see cref="DataType"/> of the operand index array</param>
		/// <returns>Whether mix-typed operations indicated by <paramref name="matrix1"/>, <paramref name="matrix2"/>, <paramref name="index"/> and <paramref name="indexType"/> are supported by this <see cref="AbstractApi"/>.</returns>
		protected abstract bool IsSupportedMatrixBinaryIndexUnary(CombinationOfLocations matrix1, CombinationOfLocations matrix2, CombinationOfLocations index, DataType indexType);
		#endregion


		#region static methods as dispatchers
		#region eigen-problems
		/// <summary>
		/// Calculate the eigenvalues (and eigenvectors) of given hermitian matrix <paramref name="A"/> for the special eigen-problem.
		/// </summary>
		/// <param name="mode">The <see cref="SolveVectorMode"/> to indicate which eigenvectors should be calculated, any value other than <see cref="SolveVectorMode.NoVector"/> will be regarded as <see cref="SolveVectorMode.Vector"/></param>
		/// <typeparam name="T">Any unmanaged struct as the input data type</typeparam>
		/// <typeparam name="TReal">Any unmanaged struct as the real corresponding type of <typeparamref name="T"/></typeparam>
		/// <param name="n">The number of rows and columns of <paramref name="A"/> of type <typeparamref name="T"/></param>
		/// <param name="valOut">The preallocated output eigenvalues of type <typeparamref name="TReal"/></param>
		/// <param name="A">The input/output hermitian matrix to calculate the special eigen-problem; destroyed during the calculation if <paramref name="mode"/> is <see cref="SolveVectorMode.NoVector"/> or replaced by the eigenvectors otherwise.</param>
		/// <param name="lda">The leading dimension of <paramref name="A"/></param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="valOut"/> or <paramref name="A"/> is null or invalid</exception>
		/// <exception cref="TypeMismatchException">If <typeparamref name="TReal"/> is not a real type correspondence of <typeparamref name="T"/></exception>
		/// <exception cref="MatrixSolveAlgorithmException">If the internal solver failed due to some reason</exception>
		public static void EigenSpecialMatrixHermitian<T, TReal>(SolveVectorMode mode, long n, Storage<TReal> valOut, Storage<T> A, long lda) where T : unmanaged where TReal : unmanaged
		{
			bool Local_Supported(AbstractApi api)
			{
				Span<CombinationOfLocations> normals = stackalloc CombinationOfLocations[] { A.LocationDescription };
				Span<CombinationOfLocations> reals = stackalloc CombinationOfLocations[] { valOut.LocationDescription };
				return api.IsSupportedNormalTypeRealType(normals, reals);
			}

			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(Local_Supported, node);
				success = node.Value.EigenSpecialMatrixHermitian_(mode, n, valOut, A, lda);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// Calculate the eigenvalues (and eigenvectors) of given hermitian matrix pair <paramref name="A"/>, <paramref name="B"/> for the general eigen-problem.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the input data type</typeparam>
		/// <typeparam name="TReal">Any unmanaged struct as the real corresponding type of <typeparamref name="T"/></typeparam>
		/// <param name="mode">The <see cref="SolveVectorMode"/> to indicate which eigenvectors should be calculated, any value other than <see cref="SolveVectorMode.NoVector"/> will be regarded as <see cref="SolveVectorMode.Vector"/></param>
		/// <param name="type">The <see cref="GeneralEigenType"/> to indicate positions of <paramref name="A"/> and <paramref name="B"/></param>
		/// <param name="n">The number of rows and columns of <paramref name="A"/> and <paramref name="B"/></param>
		/// <param name="valOut">The preallocated output eigenvalues of type <typeparamref name="TReal"/></param>
		/// <param name="A">The input/output hermitian matrix to calculate the general eigen-problem; destroyed during the calculation if <paramref name="mode"/> is <see cref="SolveVectorMode.NoVector"/>; or replaced by the eigenvectors otherwise.</param>
		/// <param name="lda">The leading dimension of <paramref name="A"/></param>
		/// <param name="B">Another input hermitian matrix to calculate the general eigen-problem</param>
		/// <param name="ldb">The leading dimension of <paramref name="B"/></param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="valOut"/> or <paramref name="A"/> or <paramref name="B"/> is null or invalid</exception>
		/// <exception cref="TypeMismatchException">If <typeparamref name="TReal"/> is not a real type correspondence of <typeparamref name="T"/></exception>
		/// <exception cref="MatrixSolveAlgorithmException">If the internal solver failed due to some reason</exception>
		public static void EigenGeneralMatrixHermitian<T, TReal>(GeneralEigenType type, SolveVectorMode mode, long n, Storage<TReal> valOut, Storage<T> A, long lda, Storage<T> B, long ldb) where T : unmanaged where TReal : unmanaged
		{
			bool Local_Supported(AbstractApi api)
			{
				Span<CombinationOfLocations> normals = stackalloc CombinationOfLocations[] { A.LocationDescription, B.LocationDescription };
				Span<CombinationOfLocations> reals = stackalloc CombinationOfLocations[] { valOut.LocationDescription };
				return api.IsSupportedNormalTypeRealType(normals, reals);
			}

			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(Local_Supported, node);
				success = node.Value.EigenGeneralMatrixHermitian_(type, mode, n, valOut, A, lda, B, ldb);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// Calculate the eigenvalues (and eigenvectors) of given general matrix <paramref name="A"/> for the special eigen-problem.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the input data type</typeparam>
		/// <typeparam name="TComplex">Any unmanaged struct as the complex corresponding type of <typeparamref name="T"/></typeparam>
		/// <param name="mode">The <see cref="SolveVectorMode"/> to indicate which eigenvectors should be calculated</param>
		/// <param name="n">The number of rows and columns of <paramref name="A"/></param>
		/// <param name="valOut">The preallocated output eigenvalues of type <typeparamref name="TComplex"/></param>
		/// <param name="leftVec">The preallocated output left eigenvectors of type <typeparamref name="TComplex"/>, can be null if <paramref name="mode"/> does not indicate the output of left eigenvectors.</param>
		/// <param name="ldvl">The leading dimension of <paramref name="leftVec"/></param>
		/// <param name="ldvr">The leading dimension of <paramref name="rightVec"/></param>
		/// <param name="rightVec">The preallocated output right eigenvectors of type <typeparamref name="TComplex"/>, can be null if <paramref name="mode"/> does not indicate the output of left eigenvectors.</param>
		/// <param name="A">The input general matrix to calculate the special eigen-problem of a </param>
		/// <param name="lda">leading dimension of <paramref name="A"/></param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="valOut"/> or <paramref name="A"/> is null or invalid</exception>
		/// <exception cref="TypeMismatchException">If <typeparamref name="TComplex"/> is not a complex type correspondence of <typeparamref name="T"/></exception>
		/// <exception cref="MatrixSolveAlgorithmException">If the internal solver failed due to some reason</exception>
		public static void EigenSpecialMatrixGeneral<T, TComplex>(SolveVectorMode mode, long n, Storage<TComplex> valOut, Storage<TComplex>? leftVec, long ldvl, Storage<TComplex>? rightVec, long ldvr, Storage<T> A, long lda) where T : unmanaged where TComplex : unmanaged, ICustomNativeType<TComplex>
		{
			bool Local_Supported(AbstractApi api)
			{
				Span<CombinationOfLocations> normals = stackalloc CombinationOfLocations[] {
					A.LocationDescription
				};
				Span<CombinationOfLocations> complexes = stackalloc CombinationOfLocations[3].SetValue(valOut.LocationDescription);
				if (leftVec is not null && rightVec is not null)
				{
					complexes.SetValue(valOut.LocationDescription, leftVec.LocationDescription, rightVec.LocationDescription);
				}
				else if (leftVec is null && rightVec is not null)
				{
					complexes.SetValue(valOut.LocationDescription, rightVec.LocationDescription);
					complexes = complexes[0..2];
				}
				else if (leftVec is not null && rightVec is null)
				{
					complexes.SetValue(valOut.LocationDescription, leftVec.LocationDescription);
					complexes = complexes[0..2];
				}
				else
				{
					complexes.SetValue(valOut.LocationDescription);
					complexes = complexes[0..1];
				}
				return api.IsSupportedNormalTypeRealType(normals, complexes);
			}

			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(Local_Supported, node);
				success = node.Value.EigenSpecialMatrixGeneral_(mode, n, valOut, leftVec, ldvl, rightVec, ldvr, A, lda);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// Calculate the eigenvalues (and eigenvectors) of given general matrix pair <paramref name="A"/>, <paramref name="B"/> for the general eigen-problem. The output eigenvalues are separated to prevent possible over- or under- flow.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the input data type</typeparam>
		/// <typeparam name="TComplex">Any unmanaged struct as the complex corresponding type of <typeparamref name="T"/></typeparam>
		/// <param name="type">The <see cref="GeneralEigenType"/> to indicate positions of <paramref name="A"/> and <paramref name="B"/></param>
		/// <param name="mode">The <see cref="SolveVectorMode"/> to indicate which eigenvectors should be calculated</param>
		/// <param name="n">The number of rows and columns of <paramref name="A"/> and <paramref name="B"/></param>
		/// <param name="α">The output numerator of the eigenvalues, must be preallocated, of corresponding complex type <typeparamref name="TComplex"/></param>
		/// <param name="β">The output denominator of the eigenvalues, must be preallocated, of type <typeparamref name="T"/></param>
		/// <param name="leftVec">The output left eigenvectors, must be preallocated, of corresponding complex type</param>
		/// <param name="ldvl">The leading dimension of <paramref name="leftVec"/></param>
		/// <param name="ldvr">The leading dimension of <paramref name="rightVec"/></param>
		/// <param name="rightVec">The output right eigenvectors, must be preallocated, of corresponding complex type</param>
		/// <param name="A">The input general matrix to calculate eigensystem</param>
		/// <param name="lda">The leading dimension of <paramref name="A"/></param>
		/// <param name="B">The input general matrix to calculate general eigen-problem; if <c><paramref name="B"/> is null</c>, the normal eigen is performed and <paramref name="type"/> is not used; otherwise, the general one is performed</param>
		/// <param name="ldb">The leading dimension of <paramref name="B"/></param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="α"/> or <paramref name="β"/> or <paramref name="A"/> or <paramref name="B"/> is null or invalid</exception>
		/// <exception cref="TypeMismatchException">If <typeparamref name="TComplex"/> is not a complex type correspondence of <typeparamref name="T"/></exception>
		/// <exception cref="MatrixSolveAlgorithmException">If the internal solver failed due to some reason</exception>
		public static void EigenGeneralMatrixGeneral<T, TComplex>(GeneralEigenType type, SolveVectorMode mode, long n, Storage<TComplex> α, Storage<T> β, Storage<TComplex>? leftVec, long ldvl, Storage<TComplex>? rightVec, long ldvr, Storage<T> A, long lda, Storage<T> B, long ldb) where T : unmanaged where TComplex : unmanaged, ICustomNativeType<TComplex>
		{
			bool Local_Supported(AbstractApi api)
			{
				Span<CombinationOfLocations> normals = stackalloc CombinationOfLocations[] { A.LocationDescription, B.LocationDescription, β.LocationDescription };
				Span<CombinationOfLocations> complexes = stackalloc CombinationOfLocations[3].SetValue(α.LocationDescription);
				if (leftVec is not null && rightVec is not null)
				{
					complexes.SetValue(α.LocationDescription, leftVec.LocationDescription, rightVec.LocationDescription);
				}
				else if (leftVec is null && rightVec is not null)
				{
					complexes.SetValue(α.LocationDescription, rightVec.LocationDescription);
					complexes = complexes[0..2];
				}
				else if (leftVec is not null && rightVec is null)
				{
					complexes.SetValue(α.LocationDescription, leftVec.LocationDescription);
					complexes = complexes[0..2];
				}
				else
				{
					complexes.SetValue(α.LocationDescription);
					complexes = complexes[0..1];
				}
				return api.IsSupportedNormalTypeRealType(normals, complexes);
			}

			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(Local_Supported, node);
				success = node.Value.EigenGeneralMatrixGeneral_(type, mode, n, α, β, leftVec, ldvl, rightVec, ldvr, A, lda, B, ldb);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}
		#endregion

		#region linear solve
		/// <summary>
		/// Solve a series of linear systems: <c><paramref name="op"/>(<paramref name="A"/>) * X == <paramref name="B"/></c>. Where each column pair of X and <paramref name="B"/> together with <paramref name="op"/>(<paramref name="A"/>) is a linear system.<br/>
		/// Both <paramref name="A"/> and <paramref name="B"/> are in-place: <paramref name="A"/> may be replaced by its LU decomposition, and <paramref name="B"/> shall be replaced by the solution X.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <typeparam name="TInd">Any integral type unmanaged struct as the data type</typeparam>
		/// <param name="op">The <see cref="MatrixOperation"/> indicates the simple operation to the <paramref name="A"/></param>
		/// <param name="n">The number of rows and columns of matrix <paramref name="A"/></param>
		/// <param name="nrhs">The number of right-hand sides, a.k.a. the number of linear systems.</param>
		/// <param name="A">The input/output coefficient matrix; may be overwritten by its LU decomposition after exit.</param>
		/// <param name="lda">The leading dimension of <paramref name="A"/></param>
		/// <param name="B">The input/output matrix whose each column is a vector at right-hand side; will be overwritten by solution X after exit.</param>
		/// <param name="ldb">The leading dimension of <paramref name="B"/></param>
		/// <param name="work">The pre-allocated working space of size at least <paramref name="n"/>, default null means internal allocate</param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="B"/> is null or invalid</exception>
		/// <exception cref="MatrixSolveAlgorithmException">If the internal solver failed due to some reason</exception>
		public static void LinearSolve<T, TInd>(MatrixOperation op, long n, long nrhs, Storage<T> A, long lda, Storage<T> B, long ldb, Storage<TInd>? work = null) where T : unmanaged where TInd : unmanaged
		{
			CombinationOfLocations location1 = A.LocationDescription, location2 = B.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(a => a.IsSupportedMatrixBinary(location1, location2), node);
				success = node.Value.LinearSolve_(op, n, nrhs, A, lda, B, ldb, work);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}
		#endregion

		#region QR solve
		/// <summary>
		/// Compute the complete QR factorization the given matrix <paramref name="A"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="full">Whether to perform full factorization or not</param>
		/// <param name="m">The number of rows of matrix <paramref name="A"/></param>
		/// <param name="n">The number of columns of matrix <paramref name="A"/></param>
		/// <param name="A">The input/output matrix to be factorized of leading dimension <paramref name="lda"/> and size <paramref name="m"/>×<paramref name="n"/> whose upper triangular part will be overwritten by the triangular matrix at exit (rest part may be filled with other values).</param>
		/// <param name="lda">The leading dimension of <paramref name="A"/></param>
		/// <param name="Q">The preallocated output unitary matrix of leading dimension <paramref name="ldq"/></param>
		/// <param name="ldq">The leading dimension of <paramref name="Q"/></param>
		/// <param name="work">The pre-allocated working space of size at least <paramref name="n"/>, default null means internal allocate</param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="Q"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="Q"/> do not contain enough space to be overwritten</exception>
		/// <exception cref="MatrixSolveAlgorithmException">If the internal solver failed due to some reason</exception>
		public static void QRDecomposition<T>(bool full, long m, long n, Storage<T> A, long lda, Storage<T> Q, long ldq, Storage<T>? work) where T : unmanaged
		{
			CombinationOfLocations location1 = A.LocationDescription, location2 = Q.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(a => a.IsSupportedMatrixBinary(location1, location2), node);
				success = node.Value.QRDecomposition_(full, m, n, A, lda, Q, ldq, work);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// Least square solve a series of linear systems: <c><paramref name="A"/> * X == <paramref name="B"/></c>. Where each column pair of X and <paramref name="B"/> together with <paramref name="A"/> is a overdetermined linear system.<br/>
		/// Both <paramref name="A"/> and <paramref name="B"/> are in-place: <paramref name="A"/> may be replaced by its implicit QR decomposition, and <paramref name="B"/> shall be replaced by the solution X.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="m">The number of rows of matrix <paramref name="A"/>, must be larger than <paramref name="n"/></param>
		/// <param name="n">The number of columns of matrix <paramref name="A"/></param>
		/// <param name="nrhs">The number of right-hand sides, a.k.a. the number of overdetermined linear systems.</param>
		/// <param name="A">The input/output coefficient matrix; may be overwritten by its implicit QR decomposition after exit.</param>
		/// <param name="lda">The leading dimension of <paramref name="A"/></param>
		/// <param name="B">The input/output matrix whose each column is a vector at right-hand side; will be overwritten by solution X after exit.</param>
		/// <param name="ldb">The leading dimension of <paramref name="B"/></param>
		/// <param name="work">The pre-allocated working space of size at least <paramref name="n"/>, default null means internal allocate</param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="B"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="m"/> ≤ <paramref name="n"/></exception>
		/// <exception cref="MatrixSolveAlgorithmException">If the internal solver failed due to some reason</exception>
		public static void LeastSquareSolve<T>(long m, long n, long nrhs, Storage<T> A, long lda, Storage<T> B, long ldb, Storage<T>? work) where T : unmanaged
		{
			CombinationOfLocations location1 = A.LocationDescription, location2 = B.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(a => a.IsSupportedMatrixBinary(location1, location2), node);
				success = node.Value.LeastSquareSolve_(m, n, nrhs, A, lda, B, ldb, work);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}
		#endregion

		#region other decompositions
		/// <summary>
		/// Compute the singular value decomposition (SVD) of a matrix <paramref name="A"/> and corresponding the left and/or right singular vectors: <paramref name="A"/> = <paramref name="U"/> <paramref name="S"/> <paramref name="Vct"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the input data type</typeparam>
		/// <typeparam name="TReal">Any unmanaged struct as the real corresponding type of <typeparamref name="T"/></typeparam>
		/// <param name="storeU">The <see cref="SVDStore"/> to specify options for computing all or part of the matrix <paramref name="U"/></param>
		/// <param name="storeV">The <see cref="SVDStore"/> to specify options for computing all or part of the matrix <paramref name="Vct"/></param>
		/// <param name="m">The number of rows of matrix</param>
		/// <param name="n">The number of columns of matrix</param>
		/// <param name="A">The input (and possible output) matrix of size <paramref name="m"/>×<paramref name="n"/> and leading dimension <paramref name="lda"/>. Will be overwritten by singular vectors if <paramref name="storeU"/> or <paramref name="storeV"/> is <see cref="SVDStore.Overwrite"/>.</param>
		/// <param name="lda">The leading dimension of <paramref name="A"/></param>
		/// <param name="S">The preallocated output singular values as a vector of size at least <c>min(<paramref name="m"/>, <paramref name="n"/>)</c></param>
		/// <param name="U">The preallocated output left unitary matrix with size <paramref name="ldu"/>×<paramref name="m"/>, can be null if <paramref name="storeU"/> is <see cref="SVDStore.Overwrite"/> or <see cref="SVDStore.Overwrite"/>.</param>
		/// <param name="ldu">The leading dimension of <paramref name="U"/></param>
		/// <param name="Vct">The preallocated output right unitary ("ct" for conjugate transpose) matrix with size <paramref name="ldvct"/>×<paramref name="n"/>, can be null if <paramref name="storeV"/> is <see cref="SVDStore.Overwrite"/> or <see cref="SVDStore.Overwrite"/>.</param>
		/// <param name="ldvct">The leading dimension of <paramref name="Vct"/></param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="S"/> or <paramref name="A"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="storeU"/> and <paramref name="storeV"/> are both <see cref="SVDStore.Overwrite"/></exception>
		/// <exception cref="TypeMismatchException">If <typeparamref name="TReal"/> is not a real type correspondence of <typeparamref name="T"/></exception>
		/// <exception cref="MatrixSolveAlgorithmException">If the internal solver failed due to some reason</exception>
		public static void SingularValues<T, TReal>(SVDStore storeU, SVDStore storeV, long m, long n, Storage<T> A, long lda, Storage<TReal> S, Storage<T>? U, long ldu, Storage<T>? Vct, long ldvct) where T : unmanaged where TReal : unmanaged
		{
			bool Local_Supported(AbstractApi api)
			{
				Span<CombinationOfLocations> normals = stackalloc CombinationOfLocations[3];
				if (U is not null && Vct is not null)
				{
					normals.SetValue(A.LocationDescription, U.LocationDescription, Vct.LocationDescription);
				}
				else if (U is null && Vct is not null)
				{
					normals.SetValue(A.LocationDescription, Vct.LocationDescription);
					normals = normals[0..2];
				}
				else if (U is not null && Vct is null)
				{
					normals.SetValue(A.LocationDescription, U.LocationDescription);
					normals = normals[0..2];
				}
				else
				{
					normals.SetValue(A.LocationDescription);
					normals = normals[0..1];
				}
				Span<CombinationOfLocations> reals = stackalloc CombinationOfLocations[] {
					S.LocationDescription
				};
				return api.IsSupportedNormalTypeRealType(normals, reals);
			}

			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(Local_Supported, node);
				success = node.Value.SingularValues_(storeU, storeV, m, n, A, lda, S, U, ldu, Vct, ldvct);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// Compute the Schur decomposition of given matrix <paramref name="A"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the input/output data type</typeparam>
		/// <param name="jobu">The <see cref="SolveVectorMode"/> to indicate whether to calculate Schur vectors or not. Any value other than <see cref="SolveVectorMode.NoVector"/> will be regarded as <see cref="SolveVectorMode.Vector"/>.</param>
		/// <param name="n">The number of rows and columns of matrix <paramref name="A"/></param>
		/// <param name="A">The input/output matrix to be decomposed of leading dimension <paramref name="lda"/> and size <paramref name="n"/>×<paramref name="n"/>, overwritten by the triangular Schur matrix at exit</param>
		/// <param name="lda">The leading dimension of <paramref name="A"/></param>
		/// <param name="U">The preallocated output Schur vectors of leading dimension <paramref name="ldu"/> and size <paramref name="n"/>×<paramref name="n"/>, can be null if <paramref name="jobu"/> is <see cref="SolveVectorMode.NoVector"/>.</param>
		/// <param name="ldu">The leading dimension of <paramref name="U"/></param>
		/// <param name="orderVal">The eigenvalues in this array will be selected to at the top left of Schur form. Default null means no particular order is preferred.</param>
		/// <returns>The actual number of eigenvalues returned</returns>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="orderVal"/> has duplicate values or its length is larger than <paramref name="n"/></exception>
		/// <exception cref="MatrixSolveAlgorithmException">If the internal solver failed due to some reason</exception>
		public static long SchurDecomposition<T>(SolveVectorMode jobu, long n, Storage<T> A, long lda, Storage<T>? U, long ldu, Storage<ComplexDouble>? orderVal = null) where T : unmanaged
		{
			bool Local_Supported(AbstractApi api)
			{
				Span<CombinationOfLocations> normals = stackalloc CombinationOfLocations[2];
				Span<CombinationOfLocations> complexes = stackalloc CombinationOfLocations[1];
				if (orderVal is null)
				{
					if (U is null)
						return api.IsSupportedMatrixUnary(A.LocationDescription);
					else
						return api.IsSupportedMatrixBinary(A.LocationDescription, U.LocationDescription);
				}
				// else
				if (U is null)
				{
					normals.SetValue(A.LocationDescription);
					normals = normals[0..1];
				}
				else
				{
					normals.SetValue(A.LocationDescription, U.LocationDescription);
				}
				complexes.SetValue(orderVal.LocationDescription);
				return api.IsSupportedNormalTypeRealType(normals, complexes);
			}

			long result = default;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(Local_Supported, node);
				success = node.Value.SchurDecomposition_(jobu, n, A, lda, U, ldu, out result, orderVal);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
			return result;
		}
		#endregion
		#endregion


		#region abstract methods that actually do computations
		#region eigen-problems
		/// <summary>
		/// When implemented by a derived class, calculate the eigenvalues (and eigenvectors) of given hermitian matrix <paramref name="A"/> for the special eigen-problem.
		/// </summary>
		/// <param name="mode">The <see cref="SolveVectorMode"/> to indicate which eigenvectors should be calculated, any value other than <see cref="SolveVectorMode.NoVector"/> will be regarded as <see cref="SolveVectorMode.Vector"/></param>
		/// <typeparam name="T">Any unmanaged struct as the input data type</typeparam>
		/// <typeparam name="TReal">Any unmanaged struct as the real corresponding type of <typeparamref name="T"/></typeparam>
		/// <param name="n">The number of rows and columns of <paramref name="A"/> of type <typeparamref name="T"/></param>
		/// <param name="valOut">The preallocated output eigenvalues of type <typeparamref name="TReal"/></param>
		/// <param name="A">The input/output hermitian matrix to calculate the special eigen-problem; destroyed during the calculation if <paramref name="mode"/> is <see cref="SolveVectorMode.NoVector"/> or replaced by the eigenvectors otherwise.</param>
		/// <param name="lda">The leading dimension of <paramref name="A"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="valOut"/> or <paramref name="A"/> is null or invalid</exception>
		/// <exception cref="TypeMismatchException">If <typeparamref name="TReal"/> is not a real type correspondence of <typeparamref name="T"/></exception>
		/// <exception cref="MatrixSolveAlgorithmException">If the internal solver failed due to some reason</exception>
		protected abstract bool EigenSpecialMatrixHermitian_<T, TReal>(SolveVectorMode mode, long n, Storage<TReal> valOut, Storage<T> A, long lda) where T : unmanaged where TReal : unmanaged;

		/// <summary>
		/// When implemented by a derived class, calculate the eigenvalues (and eigenvectors) of given symmetric-definite / hermitian-definite matrix pair <paramref name="A"/>, <paramref name="B"/> for the general eigen-problem.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the input data type</typeparam>
		/// <typeparam name="TReal">Any unmanaged struct as the real corresponding type of <typeparamref name="T"/></typeparam>
		/// <param name="mode">The <see cref="SolveVectorMode"/> to indicate which eigenvectors should be calculated, any value other than <see cref="SolveVectorMode.NoVector"/> will be regarded as <see cref="SolveVectorMode.Vector"/></param>
		/// <param name="type">The <see cref="GeneralEigenType"/> to indicate positions of <paramref name="A"/> and <paramref name="B"/></param>
		/// <param name="n">The number of rows and columns of <paramref name="A"/> and <paramref name="B"/></param>
		/// <param name="valOut">The preallocated output eigenvalues of type <typeparamref name="TReal"/></param>
		/// <param name="A">The input/output symmetric/hermitian positive-definite matrix to calculate the general eigen-problem; destroyed during the calculation if <paramref name="mode"/> is <see cref="SolveVectorMode.NoVector"/>; or replaced by the eigenvectors otherwise.</param>
		/// <param name="lda">The leading dimension of <paramref name="A"/></param>
		/// <param name="B">Another input/output symmetric/hermitian positive-definite matrix to calculate the general eigen-problem; may be destroyed during the calculation</param>
		/// <param name="ldb">The leading dimension of <paramref name="B"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="valOut"/> or <paramref name="A"/> or <paramref name="B"/> is null or invalid</exception>
		/// <exception cref="TypeMismatchException">If <typeparamref name="TReal"/> is not a real type correspondence of <typeparamref name="T"/></exception>
		/// <exception cref="MatrixSolveAlgorithmException">If the internal solver failed due to some reason</exception>
		protected abstract bool EigenGeneralMatrixHermitian_<T, TReal>(GeneralEigenType type, SolveVectorMode mode, long n, Storage<TReal> valOut, Storage<T> A, long lda, Storage<T> B, long ldb) where T : unmanaged where TReal : unmanaged;

		/// <summary>
		/// When implemented by a derived class, calculate the eigenvalues (and eigenvectors) of given general matrix <paramref name="A"/> for the special eigen-problem.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the input data type</typeparam>
		/// <typeparam name="TComplex">Any unmanaged struct as the complex corresponding type of <typeparamref name="T"/></typeparam>
		/// <param name="mode">The <see cref="SolveVectorMode"/> to indicate which eigenvectors should be calculated</param>
		/// <param name="n">The number of rows and columns of <paramref name="A"/></param>
		/// <param name="valOut">The preallocated output eigenvalues of type <typeparamref name="TComplex"/></param>
		/// <param name="leftVec">The preallocated output left eigenvectors of type <typeparamref name="TComplex"/>, can be null if <paramref name="mode"/> does not indicate the output of left eigenvectors.</param>
		/// <param name="ldvl">The leading dimension of <paramref name="leftVec"/></param>
		/// <param name="ldvr">The leading dimension of <paramref name="rightVec"/></param>
		/// <param name="rightVec">The preallocated output right eigenvectors of type <typeparamref name="TComplex"/>, can be null if <paramref name="mode"/> does not indicate the output of left eigenvectors.</param>
		/// <param name="A">The input general matrix to calculate the special eigen-problem of a </param>
		/// <param name="lda">leading dimension of <paramref name="A"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="valOut"/> or <paramref name="A"/> is null or invalid</exception>
		/// <exception cref="TypeMismatchException">If <typeparamref name="TComplex"/> is not a complex type correspondence of <typeparamref name="T"/></exception>
		/// <exception cref="MatrixSolveAlgorithmException">If the internal solver failed due to some reason</exception>
		protected abstract bool EigenSpecialMatrixGeneral_<T, TComplex>(SolveVectorMode mode, long n, Storage<TComplex> valOut, Storage<TComplex>? leftVec, long ldvl, Storage<TComplex>? rightVec, long ldvr, Storage<T> A, long lda) where T : unmanaged where TComplex : unmanaged, ICustomNativeType<TComplex>;

		/// <summary>
		/// When implemented by a derived class, calculate the eigenvalues (and eigenvectors) of given general matrix pair <paramref name="A"/>, <paramref name="B"/> for the general eigen-problem. The output eigenvalues are separated to prevent possible over- or under- flow.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the input data type</typeparam>
		/// <typeparam name="TComplex">Any unmanaged struct as the complex corresponding type of <typeparamref name="T"/></typeparam>
		/// <param name="type">The <see cref="GeneralEigenType"/> to indicate positions of <paramref name="A"/> and <paramref name="B"/></param>
		/// <param name="mode">The <see cref="SolveVectorMode"/> to indicate which eigenvectors should be calculated</param>
		/// <param name="n">The number of rows and columns of <paramref name="A"/> and <paramref name="B"/></param>
		/// <param name="α">The output numerator of the eigenvalues, must be preallocated, of corresponding complex type <typeparamref name="TComplex"/></param>
		/// <param name="β">The output denominator of the eigenvalues, must be preallocated, of type <typeparamref name="T"/></param>
		/// <param name="leftVec">The output left eigenvectors, must be preallocated, of corresponding complex type</param>
		/// <param name="ldvl">The leading dimension of <paramref name="leftVec"/></param>
		/// <param name="ldvr">The leading dimension of <paramref name="rightVec"/></param>
		/// <param name="rightVec">The output right eigenvectors, must be preallocated, of corresponding complex type</param>
		/// <param name="A">The input general matrix to calculate eigensystem</param>
		/// <param name="lda">The leading dimension of <paramref name="A"/></param>
		/// <param name="B">The input general matrix to calculate general eigen-problem; if <c><paramref name="B"/> is null</c>, the normal eigen is performed and <paramref name="type"/> is not used; otherwise, the general one is performed</param>
		/// <param name="ldb">The leading dimension of <paramref name="B"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="α"/> or <paramref name="β"/> or <paramref name="A"/> or <paramref name="B"/> is null or invalid</exception>
		/// <exception cref="TypeMismatchException">If <typeparamref name="TComplex"/> is not a complex type correspondence of <typeparamref name="T"/></exception>
		/// <exception cref="MatrixSolveAlgorithmException">If the internal solver failed due to some reason</exception>
		protected abstract bool EigenGeneralMatrixGeneral_<T, TComplex>(GeneralEigenType type, SolveVectorMode mode, long n, Storage<TComplex> α, Storage<T> β, Storage<TComplex>? leftVec, long ldvl, Storage<TComplex>? rightVec, long ldvr, Storage<T> A, long lda, Storage<T> B, long ldb) where T : unmanaged where TComplex : unmanaged, ICustomNativeType<TComplex>;
		#endregion

		#region linear solve
		/// <summary>
		/// When implemented by a derived class, solve a series of linear systems: <c><paramref name="op"/>(<paramref name="A"/>) * X == <paramref name="B"/></c>. Where each column pair of X and <paramref name="B"/> together with <paramref name="op"/>(<paramref name="A"/>) is a linear system.<br/>
		/// Both <paramref name="A"/> and <paramref name="B"/> are in-place: <paramref name="A"/> may be replaced by its LU decomposition, and <paramref name="B"/> shall be replaced by the solution X.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <typeparam name="TInd">Any integral type unmanaged struct as the data type</typeparam>
		/// <param name="op">The <see cref="MatrixOperation"/> indicates the simple operation to the <paramref name="A"/></param>
		/// <param name="n">The number of rows and columns of matrix <paramref name="A"/></param>
		/// <param name="nrhs">The number of right-hand sides, a.k.a. the number of linear systems.</param>
		/// <param name="A">The input/output coefficient matrix; may be overwritten by its LU decomposition after exit.</param>
		/// <param name="lda">The leading dimension of <paramref name="A"/></param>
		/// <param name="B">The input/output matrix whose each column is a vector at right-hand side; will be overwritten by solution X after exit.</param>
		/// <param name="ldb">The leading dimension of <paramref name="B"/></param>
		/// <param name="work">The pre-allocated working space of size at least <paramref name="n"/>, default null means internal allocate</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="B"/> is null or invalid</exception>
		/// <exception cref="MatrixSolveAlgorithmException">If the internal solver failed due to some reason</exception>
		protected abstract bool LinearSolve_<T, TInd>(MatrixOperation op, long n, long nrhs, Storage<T> A, long lda, Storage<T> B, long ldb, Storage<TInd>? work = null) where T : unmanaged where TInd : unmanaged;
		#endregion

		#region QR solve
		/// <summary>
		/// When implemented by a derived class, compute the complete QR factorization the given matrix <paramref name="A"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="full">Whether to perform full factorization or not</param>
		/// <param name="m">The number of rows of matrix <paramref name="A"/></param>
		/// <param name="n">The number of columns of matrix <paramref name="A"/></param>
		/// <param name="A">The input/output matrix to be factorized of leading dimension <paramref name="lda"/> and size <paramref name="m"/>×<paramref name="n"/> whose upper triangular part will be overwritten by the triangular matrix at exit (rest part may be filled with other values).</param>
		/// <param name="lda">The leading dimension of <paramref name="A"/></param>
		/// <param name="Q">The preallocated output unitary matrix of leading dimension <paramref name="ldq"/></param>
		/// <param name="ldq">The leading dimension of <paramref name="Q"/></param>
		/// <param name="work">The pre-allocated working space of size at least <c>min(<paramref name="m"/>,<paramref name="n"/>)</c>, default null means internal allocate</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="Q"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="Q"/> do not contain enough space to be overwritten</exception>
		/// <exception cref="MatrixSolveAlgorithmException">If the internal solver failed due to some reason</exception>
		protected abstract bool QRDecomposition_<T>(bool full, long m, long n, Storage<T> A, long lda, Storage<T> Q, long ldq, Storage<T>? work = null) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, least square solve a series of linear systems: <c><paramref name="A"/> * X == <paramref name="B"/></c>. Where each column pair of X and <paramref name="B"/> together with <paramref name="A"/> is a overdetermined linear system.<br/>
		/// Both <paramref name="A"/> and <paramref name="B"/> are in-place: <paramref name="A"/> may be replaced by its implicit QR decomposition, and <paramref name="B"/> shall be replaced by the solution X.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="m">The number of rows of matrix <paramref name="A"/>, must be larger than <paramref name="n"/></param>
		/// <param name="n">The number of columns of matrix <paramref name="A"/></param>
		/// <param name="nrhs">The number of right-hand sides, a.k.a. the number of overdetermined linear systems.</param>
		/// <param name="A">The input/output coefficient matrix; may be overwritten by its implicit QR decomposition after exit.</param>
		/// <param name="lda">The leading dimension of <paramref name="A"/></param>
		/// <param name="B">The input/output matrix whose each column is a vector at right-hand side; will be overwritten by solution X after exit.</param>
		/// <param name="ldb">The leading dimension of <paramref name="B"/></param>
		/// <param name="work">The pre-allocated working space of size at least <paramref name="n"/>, default null means internal allocate</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="B"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="m"/> ≤ <paramref name="n"/></exception>
		/// <exception cref="MatrixSolveAlgorithmException">If the internal solver failed due to some reason</exception>
		protected abstract bool LeastSquareSolve_<T>(long m, long n, long nrhs, Storage<T> A, long lda, Storage<T> B, long ldb, Storage<T>? work = null) where T : unmanaged;
		#endregion

		#region other decompositions
		/// <summary>
		/// When implemented by a derived class, compute the singular value decomposition (SVD) of a matrix <paramref name="A"/> and corresponding the left and/or right singular vectors: <paramref name="A"/> = <paramref name="U"/> <paramref name="S"/> <paramref name="Vct"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the input data type</typeparam>
		/// <typeparam name="TReal">Any unmanaged struct as the real corresponding type of <typeparamref name="T"/></typeparam>
		/// <param name="storeU">The <see cref="SVDStore"/> to specify options for computing all or part of the matrix <paramref name="U"/></param>
		/// <param name="storeV">The <see cref="SVDStore"/> to specify options for computing all or part of the matrix <paramref name="Vct"/></param>
		/// <param name="m">The number of rows of matrix</param>
		/// <param name="n">The number of columns of matrix</param>
		/// <param name="A">The input (and possible output) matrix of size <paramref name="m"/>×<paramref name="n"/> and leading dimension <paramref name="lda"/>. Will be overwritten by singular vectors if <paramref name="storeU"/> or <paramref name="storeV"/> is <see cref="SVDStore.Overwrite"/>.</param>
		/// <param name="lda">The leading dimension of <paramref name="A"/></param>
		/// <param name="S">The preallocated output singular values as a vector of size at least <c>min(<paramref name="m"/>, <paramref name="n"/>)</c></param>
		/// <param name="U">The preallocated output left unitary matrix with size <paramref name="ldu"/>×<paramref name="m"/>, can be null if <paramref name="storeU"/> is <see cref="SVDStore.Overwrite"/> or <see cref="SVDStore.Overwrite"/>.</param>
		/// <param name="ldu">The leading dimension of <paramref name="U"/></param>
		/// <param name="Vct">The preallocated output right unitary ("ct" for conjugate transpose) matrix with size <paramref name="ldvct"/>×<paramref name="n"/>, can be null if <paramref name="storeV"/> is <see cref="SVDStore.Overwrite"/> or <see cref="SVDStore.Overwrite"/>.</param>
		/// <param name="ldvct">The leading dimension of <paramref name="Vct"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="S"/> or <paramref name="A"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="storeU"/> and <paramref name="storeV"/> are both <see cref="SVDStore.Overwrite"/></exception>
		/// <exception cref="TypeMismatchException">If <typeparamref name="TReal"/> is not a real type correspondence of <typeparamref name="T"/></exception>
		/// <exception cref="MatrixSolveAlgorithmException">If the internal solver failed due to some reason</exception>
		protected abstract bool SingularValues_<T, TReal>(SVDStore storeU, SVDStore storeV, long m, long n, Storage<T> A, long lda, Storage<TReal> S, Storage<T>? U, long ldu, Storage<T>? Vct, long ldvct) where T : unmanaged where TReal : unmanaged;

		/// <summary>
		/// compute the Schur decomposition of given matrix <paramref name="A"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the input/output data type</typeparam>
		/// <param name="jobu">The <see cref="SolveVectorMode"/> to indicate whether to calculate Schur vectors or not. Any value other than <see cref="SolveVectorMode.NoVector"/> will be regarded as <see cref="SolveVectorMode.Vector"/>.</param>
		/// <param name="n">The number of rows and columns of matrix <paramref name="A"/></param>
		/// <param name="A">The input/output matrix to be decomposed of leading dimension <paramref name="lda"/> and size <paramref name="n"/>×<paramref name="n"/>, overwritten by the triangular Schur matrix at exit</param>
		/// <param name="lda">The leading dimension of <paramref name="A"/></param>
		/// <param name="U">The preallocated output Schur vectors of leading dimension <paramref name="ldu"/> and size <paramref name="n"/>×<paramref name="n"/>, can be null if <paramref name="jobu"/> is <see cref="SolveVectorMode.NoVector"/>.</param>
		/// <param name="ldu">The leading dimension of <paramref name="U"/></param>
		/// <param name="orderVal">The eigenvalues in this array will be selected to at the top left of Schur form. Default null means no particular order is preferred.</param>
		/// <param name="actualNumber">Output the actual number of eigenvalues returned</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="orderVal"/> has duplicate values or its length is larger than <paramref name="n"/></exception>
		/// <exception cref="MatrixSolveAlgorithmException">If the internal solver failed due to some reason</exception>
		protected abstract bool SchurDecomposition_<T>(SolveVectorMode jobu, long n, Storage<T> A, long lda, Storage<T>? U, long ldu, out long actualNumber, Storage<ComplexDouble>? orderVal = null) where T : unmanaged;
		#endregion
		#endregion
	}
}
