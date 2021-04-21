using System;
using System.Runtime.CompilerServices;

using Althea.Arrays;
using Althea.Helpers;
using Althea.NativeTypes;


namespace Althea.LinearAlgebra
{
	#region interface
	/// <summary>
	/// The interface for runtime linear algebra API routines
	/// </summary>
	internal interface ILinearAlgebraApi
	{
		#region support information
		/// <summary>
		/// When implemented by a derived class, check if the given <paramref name="location"/> is supported by vector unary operations of this implementation or not.
		/// </summary>
		/// <param name="location">The given <see cref="CombinationOfLocations"/></param>
		/// <returns>Whether vector unary operation on <paramref name="location"/> is supported by this <see cref="ILinearAlgebraApi"/>.</returns>
		bool IsSupportedVectorUnary(CombinationOfLocations location);

		/// <summary>
		/// When implemented by a derived class, check if the given <see cref="CombinationOfLocations"/>s are supported by vector binary operations of this implementation or not.
		/// </summary>
		/// <param name="location1">The first given <see cref="CombinationOfLocations"/></param>
		/// <param name="location2">The second given <see cref="CombinationOfLocations"/></param>
		/// <returns>Whether binary operations on <paramref name="location1"/> and <paramref name="location2"/> are supported by this <see cref="ILinearAlgebraApi"/>.</returns>
		bool IsSupportedVectorBinary(CombinationOfLocations location1, CombinationOfLocations location2);

		/// <summary>
		/// When implemented by a derived class, check if the given <see cref="CombinationOfLocations"/>s are supported by vector and matrix binary operations of this implementation or not.
		/// </summary>
		/// <param name="vector">The given <see cref="CombinationOfLocations"/> of the vector</param>
		/// <param name="matrix">The given <see cref="CombinationOfLocations"/> of the matrix</param>
		/// <returns>Whether binary operations on <paramref name="vector"/> and <paramref name="matrix"/> are supported by this <see cref="ILinearAlgebraApi"/>.</returns>
		bool IsSupportedVectorUnaryMatrixUnary(CombinationOfLocations vector, CombinationOfLocations matrix);

		/// <summary>
		/// When implemented by a derived class, check if the given <see cref="CombinationOfLocations"/>s are supported by binary vector and unary matrix operations of this implementation or not.
		/// </summary>
		/// <param name="vector1">The given <see cref="CombinationOfLocations"/> of the first vector</param>
		/// <param name="vector2">The given <see cref="CombinationOfLocations"/> of the second vector</param>
		/// <param name="matrix">The given <see cref="CombinationOfLocations"/> of matrix</param>
		/// <returns>Whether binary vector and unary matrix operations on <paramref name="vector1"/> and <paramref name="vector2"/> and <paramref name="matrix"/> are supported by this <see cref="ILinearAlgebraApi"/>.</returns>
		bool IsSupportedVectorBinaryMatrixUnary(CombinationOfLocations vector1, CombinationOfLocations vector2, CombinationOfLocations matrix);

		/// <summary>
		/// When implemented by a derived class, check if the given <see cref="CombinationOfLocations"/>s are supported by unary vector and binary matrix operations of this implementation or not.
		/// </summary>
		/// <param name="vector">The given <see cref="CombinationOfLocations"/> of the vector</param>
		/// <param name="matrix1">The given <see cref="CombinationOfLocations"/> of the first matrix</param>
		/// <param name="matrix2">The given <see cref="CombinationOfLocations"/> of the second matrix</param>
		/// <returns>Whether unary vector and binary matrix operations on <paramref name="vector"/> and <paramref name="matrix1"/> and <paramref name="matrix2"/> are supported by this <see cref="ILinearAlgebraApi"/>.</returns>
		bool IsSupportedVectorUnaryMatrixBinary(CombinationOfLocations vector, CombinationOfLocations matrix1, CombinationOfLocations matrix2);

		/// <summary>
		/// When implemented by a derived class, check if the given <paramref name="location"/> is supported by matrix unary operations of this implementation or not.
		/// </summary>
		/// <param name="location">The given <see cref="CombinationOfLocations"/></param>
		/// <returns>Whether matrix unary operation on <paramref name="location"/> is supported by this <see cref="ILinearAlgebraApi"/>.</returns>
		bool IsSupportedMatrixUnary(CombinationOfLocations location);

		/// <summary>
		/// When implemented by a derived class, check if the given <see cref="CombinationOfLocations"/>s are supported by matrix binary operations of this implementation or not.
		/// </summary>
		/// <param name="location1">The first given <see cref="CombinationOfLocations"/></param>
		/// <param name="location2">The second given <see cref="CombinationOfLocations"/></param>
		/// <returns>Whether binary operations on <paramref name="location1"/> and <paramref name="location2"/> are supported by this <see cref="ILinearAlgebraApi"/>.</returns>
		bool IsSupportedMatrixBinary(CombinationOfLocations location1, CombinationOfLocations location2);

		/// <summary>
		/// When implemented by a derived class, check if the given <see cref="CombinationOfLocations"/>s are supported by matrix trinary operations of this implementation or not.
		/// </summary>
		/// <param name="location1">The first given <see cref="CombinationOfLocations"/></param>
		/// <param name="location2">The second given <see cref="CombinationOfLocations"/></param>
		/// <param name="location3">The third given <see cref="CombinationOfLocations"/></param>
		/// <returns>Whether trinary operations on <paramref name="location1"/> and <paramref name="location2"/> are supported by this <see cref="ILinearAlgebraApi"/>.</returns>
		bool IsSupportedMatrixTrinary(CombinationOfLocations location1, CombinationOfLocations location2, CombinationOfLocations location3);
		#endregion
	}
	#endregion


	#region exception
	/// <summary>
	/// The exception to be thrown when a matrix solving method of a LAPACK or LAPACK-like library failed due to several reasons
	/// </summary>
	[Serializable]
	public class MatrixSolveAlgorithmException : Exception
	{
		private static string GetDescription(SolveMethodKind kind, int info)
		{
			if (info == 0)
				return string.Empty; // no error
			if (info < 0)
				return $"The {(-info).ToOrdinal()} input parameter of method '{kind}' is invalid.";
			string? message = kind switch
			{
				SolveMethodKind.Cholesky => Resources.Exception.MatrixSolveCholesky,
				SolveMethodKind.LU => Resources.Exception.MatrixSolveLU,
				SolveMethodKind.BunchKaufman => Resources.Exception.MatrixSolveBunchKaufman,
				SolveMethodKind.SVD => Resources.Exception.MatrixSolveSVD,
				SolveMethodKind.Eigenvalue => Resources.Exception.MatrixSolveEigen,
				_ => null,
			};
			if (message is not null)
				return string.Format(message, info);
			return kind switch
			{
				SolveMethodKind.QR => Resources.Exception.MatrixSolveQR,
				SolveMethodKind.Schur => Resources.Exception.MatrixSolveSchur,
				SolveMethodKind.GeneralEigen => Resources.Exception.MatrixSolveGeneralEigen,
				SolveMethodKind.Jacobi => Resources.Exception.MatrixSolveJacobi,
				_ => $"Unknown method with error info = {info}",
			};
		}

		/// <summary>
		/// Constructor of <see cref="MatrixSolveAlgorithmException"/>
		/// </summary>
		/// <param name="kind">which kind of LAPACK-like solver is used (in <see cref="SolveMethodKind"/>)</param>
		/// <param name="i">The returned LAPACK-like solver information</param>
		public MatrixSolveAlgorithmException(SolveMethodKind kind, int i) : base(GetDescription(kind, i)) { }

		/// <summary>
		/// Empty <see cref="MatrixSolveAlgorithmException"/>
		/// </summary>
		public MatrixSolveAlgorithmException() { }

		/// <summary>
		/// <see cref="MatrixSolveAlgorithmException"/> with custom message
		/// </summary>
		public MatrixSolveAlgorithmException(string message) : base(message) { }

		/// <summary>
		/// <see cref="MatrixSolveAlgorithmException"/> with custom message and inner exception
		/// </summary>
		public MatrixSolveAlgorithmException(string message, Exception innerException) : base(message, innerException) { }
	}
	#endregion


	#region enum
	/// <summary>
	/// The <see cref="SolveMethodKind"/> enum indicates the classification of a matrix-solving method of a LAPACK or LAPACK-like library
	/// </summary>
	public enum SolveMethodKind
	{
		// Ignore Spelling: potr getrf geqrf sytrf gesvd sygvd syevd syevj sygvj gesvdj
		/// <summary>
		/// Cholesky factorization, [S|D|C|Z]potr[f|i]
		/// </summary>
		Cholesky,
		/// <summary>
		/// LU factorization, [S|D|C|Z]getrf
		/// </summary>
		LU,
		/// <summary>
		/// QR factorization, [S|D|C|Z]geqrf
		/// </summary>
		QR,
		/// <summary>
		/// Schur decomposition, [S|D|C|Z]gees
		/// </summary>
		Schur,
		/// <summary>
		/// Bunch Kaufman factorization, [S|D|C|Z]sytrf
		/// </summary>
		BunchKaufman,
		/// <summary>
		/// Singular value decomposition, [S|D|C|Z]gesvd
		/// </summary>
		SVD,
		/// <summary>
		/// Eigenvalue decomposition, [S|D|C|Z]syevd[x]
		/// </summary>
		Eigenvalue,
		/// <summary>
		/// General eigenvalue decomposition, [S|D|C|Z]sygvd[x]
		/// </summary>
		GeneralEigen,
		/// <summary>
		/// Singular value decomposition via Jacobi method, [S|D|C|Z]syevj, [S|D|C|Z]sygvj, [S|D|C|Z]gesvdj
		/// </summary>
		Jacobi
	}

	/// <summary>
	/// The <see cref="GeneralEigenType"/> enum indicates which kind of general eigenvalue problem is provided
	/// </summary>
	public enum GeneralEigenType
	{
		//tex: $A x = \lambda B x$

		/// <summary>
		/// A*x = λ*B*x
		/// </summary>
		Type1 = 1,

		//tex: $A B x = \lambda x$

		/// <summary>
		/// A*B*x = λ*x
		/// </summary>
		Type2 = 2,

		//tex: $B A x = \lambda x$

		/// <summary>
		/// B*A*x = λ*x
		/// </summary>
		Type3 = 3
	}

	/// <summary>
	/// The <see cref="SolveVectorMode"/> enum indicates which eigen- (or other type of) vector matrices obtained from standard or general eigenvalue solvers or other solvers shall be stored
	/// </summary>
	public enum SolveVectorMode
	{
		/// <summary>
		/// Do not compute the eigenvectors
		/// </summary>
		NoVector = 0,
		/// <summary>
		/// Compute the eigenvectors (both left and right)
		/// </summary>
		Vector = 1,
		/// <summary>
		/// Compute only the left eigenvectors
		/// </summary>
		LeftOnly = 2,
		/// <summary>
		/// Compute only the right eigenvectors
		/// </summary>
		RightOnly = 3
	}

	/// <summary>
	/// The <see cref="SVDStore"/> enum indicates which of the vectors of a singular value decomposition shall be stored and where it shall be stored
	/// </summary>
	public enum SVDStore
	{
		/// <summary>
		/// All the columns / rows are stored even if some of them are not necessary
		/// </summary>
		All = 0,
		/// <summary>
		/// Economical storage -- only the columns / rows which are necessary are stored
		/// </summary>
		Economic = 1,
		/// <summary>
		/// Economical overwrite storage -- only the columns / rows which are necessary are stored into the original matrix
		/// </summary>
		Overwrite = 2,
		/// <summary>
		/// None of the columns / rows are stored
		/// </summary>
		None = 3
	}

	/// <summary>
	/// The <see cref="MatrixOperation"/> enum indicates which simple operation shall be performed to the matrix before some complicated operation being executed to reduce time or space complexity.
	/// </summary>
	/// <remarks>There are two independent (orthogonal) cyclic unary operations:<br/>
	/// add <see cref="Transpose"/>: <c>unchecked(<see cref="None"/> + <see cref="Transpose"/> + <see cref="Transpose"/>) == <see cref="None"/></c><br/>
	/// bit-wise not: <c>~<see cref="None"/> == <see cref="Conjugate"/>, ~(~<see cref="Conjugate"/>) == <see cref="None"/></c><br/>
	/// As a result, the <see cref="MatrixOperation"/> enum and the unchecked 32-bit integer addition forms an (algebraic) group.</remarks>
	public enum MatrixOperation : int
	{
		/// <summary>
		/// No operation
		/// </summary>
		None = 0,
		/// <summary>
		/// The transpose only operation
		/// </summary>
		/// <remarks><c>unchecked(<see cref="Transpose"/> + <see cref="Transpose"/>) == <see cref="None"/></c></remarks>
		Transpose = int.MinValue,
		/// <summary>
		/// The conjugate only operation
		/// </summary>
		/// <remarks><c>~<see cref="Conjugate"/> == <see cref="None"/></c></remarks>
		Conjugate = -1,
		/// <summary>
		/// The conjugate transpose operation
		/// </summary>
		/// <remarks><c><see cref="ConjugateTranspose"/> == unchecked(<see cref="Conjugate"/> + <see cref="Transpose"/>) == ~<see cref="Transpose"/></c></remarks>
		ConjugateTranspose = int.MaxValue,
	}

	/// <summary>
	/// Static class of extension methods of <see cref="MatrixOperation"/>
	/// </summary>
	public static class MatrixOperationExtension
	{
		/// <summary>
		/// Check whether the given <paramref name="operation"/> can be performed in-place, i.e., no transposition involved
		/// </summary>
		/// <param name="operation">The <see cref="MatrixOperation"/> to check</param>
		/// <returns>Whether <paramref name="operation"/> can be performed in-place</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool CanInPlace(this MatrixOperation operation) => operation == MatrixOperation.None || operation == MatrixOperation.Conjugate;

		/// <summary>
		/// Transpose the given <paramref name="operation"/>
		/// </summary>
		/// <param name="operation">The <see cref="MatrixOperation"/> to be transposed</param>
		/// <returns>The result <see cref="MatrixOperation"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static MatrixOperation Transpose(this MatrixOperation operation) => unchecked(operation + (int)MatrixOperation.Transpose);

		/// <summary>
		/// Conjugate the given <paramref name="operation"/>
		/// </summary>
		/// <param name="operation">The <see cref="MatrixOperation"/> to be conjugated</param>
		/// <returns>The result <see cref="MatrixOperation"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static MatrixOperation Conjugate(this MatrixOperation operation) => ~operation;

		/// <summary>
		/// Simplify the given <paramref name="input"/> <see cref="MatrixOperation"/> with type <typeparamref name="T"/> and <paramref name="hermitian"/>
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="input">The input <see cref="MatrixOperation"/> to be simplified</param>
		/// <param name="hermitian">Whether the target matrix is neither symmetric nor hermitian (null) or simply symmetric (false) or hermitian (true)</param>
		/// <returns>The simplified <paramref name="input"/> as a <see cref="MatrixOperation"/>. If <paramref name="hermitian"/> is not null, only <see cref="MatrixOperation.None"/> and <see cref="MatrixOperation.Conjugate"/> are possible outputs.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static MatrixOperation Simplify<T>(this MatrixOperation input, bool? hermitian = null) where T : unmanaged
		{
			bool isComplex = Const<T>.IsComplex;
			bool symm = hermitian.HasValue && !hermitian.Value, herm = isComplex && hermitian.HasValue && hermitian.Value;
			switch (input)
			{
				case MatrixOperation.Transpose:
					if (symm)
						return MatrixOperation.None;
					else if (herm)
						return MatrixOperation.Conjugate;
					else
						return MatrixOperation.Transpose;
				case MatrixOperation.ConjugateTranspose:
					if (herm)
						return MatrixOperation.None;
					else if (symm)
						return MatrixOperation.Conjugate;
					else if (!isComplex)
						return MatrixOperation.Transpose;
					else
						return MatrixOperation.ConjugateTranspose;
				case MatrixOperation.Conjugate:
					if (!isComplex)
						return MatrixOperation.None;
					else if (herm)
						return MatrixOperation.Conjugate;
					else
						return MatrixOperation.Conjugate;
				default:
					return MatrixOperation.None;
			}
		}
	}
	#endregion


	#region wrapper
	/// <summary>
	/// The wrapper struct used to store the matrix slicing parameters
	/// </summary>
	public readonly ref struct MatrixSliceWrapper
	{
		private readonly long offsetRow, countRow, offsetCol, countCol;

		/// <summary>
		/// Get the starting offset of the row to take
		/// </summary>
		public long OffsetRow {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.offsetRow;
		}
		/// <summary>
		/// Get the starting offset of the column to take
		/// </summary>
		public long OffsetCol {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.offsetCol;
		}
		/// <summary>
		/// Get the number of the rows to take
		/// </summary>
		public long CountRow {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.countRow;
		}
		/// <summary>
		/// Get the number of the rows to take
		/// </summary>
		public long CountCol {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.countCol;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private MatrixSliceWrapper(long offsetRow, long countRow, long offsetCol, long countCol)
		{
			this.offsetRow = offsetRow; this.countRow = countRow; this.offsetCol = offsetCol; this.countCol = countCol;
		}

		/// <summary>
		/// Create a <see cref="MatrixSliceWrapper"/> after checking parameters with the given <paramref name="matrix"/> and <paramref name="sub"/> matrix
		/// </summary>
		/// <param name="offsetRow">The starting offset of the row to take</param>
		/// <param name="countRow">The number of the rows to take</param>
		/// <param name="offsetCol">The starting offset of the columns to take</param>
		/// <param name="countCol">The number of the columns to take</param>
		/// <param name="matrix">The matrix to take slice from</param>
		/// <param name="sub">The sub-matrix to be overwritten by the sliced <paramref name="matrix"/> or to overwrite the <paramref name="matrix"/>'s slice</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static MatrixSliceWrapper Create<T>(long offsetRow, long countRow, long offsetCol, long countCol, IMatrix matrix, IMatrix? sub = null)
		{
			// check matrix
			if (matrix is null)
				throw new ArgumentNullException(nameof(matrix));
			if (offsetRow < 0)
				throw new ArgumentOutOfRangeException(nameof(offsetRow), offsetRow, Resources.Parameter.CannotNegative);
			if (offsetRow >= matrix.NRows)
				throw new ArgumentOutOfRangeException(nameof(offsetRow), offsetRow, Resources.Parameter.InvalidValue);
			if (countRow <= 0)
				throw new ArgumentOutOfRangeException(nameof(countRow), countRow, Resources.Parameter.CannotNegative);
			if (countRow + offsetRow > matrix.NRows)
				throw new ArgumentOutOfRangeException(nameof(countRow), countRow, Resources.Parameter.InvalidValue);
			if (offsetCol < 0)
				throw new ArgumentOutOfRangeException(nameof(offsetCol), offsetCol, Resources.Parameter.CannotNegative);
			if (offsetCol >= matrix.NCols)
				throw new ArgumentOutOfRangeException(nameof(offsetCol), offsetCol, Resources.Parameter.InvalidValue);
			if (countCol <= 0)
				throw new ArgumentOutOfRangeException(nameof(countCol), countCol, Resources.Parameter.CannotNegative);
			if (countCol + offsetCol > matrix.NCols)
				throw new ArgumentOutOfRangeException(nameof(countCol), countCol, Resources.Parameter.InvalidValue);
			// check sub
			if (sub is not null)
			{
				if (countRow < sub.NRows)
					throw new ArgumentException(Resources.Parameter.WrongSize, nameof(sub));
				if (countCol < sub.NCols)
					throw new ArgumentException(Resources.Parameter.WrongSize, nameof(sub));
			}
			// return
			return new(offsetRow, countRow, offsetCol, countCol);
		}
	}
	#endregion
}
