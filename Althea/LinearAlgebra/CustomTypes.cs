using System;
using System.Collections.Generic;

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
	/// The exception to be thrown when two generic types are mismatched for some reason
	/// </summary>
	[Serializable]
	public sealed class TypeMismatchException : Exception
	{
		/// <summary>
		/// The enum to indicate the type mismatch reason
		/// </summary>
		public enum MismatchReason
		{
			/// <summary>
			/// Cannot convert the first type to the second one
			/// </summary>
			CannotConvert,
			/// <summary>
			/// The second type is not a real type correspondence of the first one
			/// </summary>
			IsNotRealCorrespondence,
			/// <summary>
			/// The second type is not a complex type correspondence of the first one
			/// </summary>
			IsNotComplexCorrespondence,
			/// <summary>
			/// The given type is not an integral type
			/// </summary>
			NotInteger,
		}

		/// <summary>
		/// Empty <see cref="TypeMismatchException"/>
		/// </summary>
		public TypeMismatchException() { }

		/// <summary>
		/// Create a <see cref="TypeMismatchException"/> with given mismatch type and mismatch reason and inner exception
		/// </summary>
		/// <param name="type">The mismatch type</param>
		/// <param name="storages">The list of <see cref="IStorage"/> to get the real <see cref="Type"/> corresponding of <paramref name="type"/></param>
		/// <param name="reason">The mismatch reason</param>
		/// <param name="inner">The inner exception</param>
		public TypeMismatchException(DataType type, IReadOnlyList<IStorage> storages, MismatchReason reason, Exception? inner) :
			base(GetMessage(GetTypeFrom(type, storages), null, reason), inner) { }

		/// <summary>
		/// Create a <see cref="TypeMismatchException"/> with given mismatch type and mismatch reason and inner exception
		/// </summary>
		/// <param name="type">The mismatch type</param>
		/// <param name="reason">The mismatch reason</param>
		/// <param name="inner">The inner exception</param>
		public TypeMismatchException(Type type, MismatchReason reason, Exception? inner) : base(GetMessage(type, null, reason), inner) { }

		/// <summary>
		/// Create a <see cref="TypeMismatchException"/> with given mismatch types and mismatch reason and inner exception
		/// </summary>
		/// <param name="from">The first mismatch type</param>
		/// <param name="to">The second mismatch type</param>
		/// <param name="reason">The mismatch reason</param>
		/// <param name="inner">The inner exception</param>
		public TypeMismatchException(Type from, Type to, MismatchReason reason, Exception? inner) : base(GetMessage(from, to, reason), inner) { }

		/// <summary>
		/// Create a <see cref="TypeMismatchException"/> with given mismatch types and mismatch reason
		/// </summary>
		/// <param name="from">The first mismatch type</param>
		/// <param name="to">The second mismatch type</param>
		/// <param name="reason">The mismatch reason</param>
		public TypeMismatchException(Type from, Type to, MismatchReason reason) : this(from, to, reason, null) { }

		private static Type GetTypeFrom(DataType type, IReadOnlyList<IStorage> storages)
		{
			if (storages is null || storages.Count == 0)
				return new object().GetType();
			for (int i = 0; i < storages.Count; i++)
			{
				Type t = storages[i].GetType();
				if (t.ToDataType() == type)
					return t;
			}
			return storages[0].GetType();
		}

		private static string GetMessage(Type from, Type? to, MismatchReason reason)
		{
			string format = reason switch
			{
				MismatchReason.CannotConvert => Resources.Exception.MismatchCannotConvert,
				MismatchReason.IsNotRealCorrespondence => Resources.Exception.MismatchNotRealCorrespondence,
				MismatchReason.IsNotComplexCorrespondence => Resources.Exception.MismatchNotComplexCorrespondence,
				MismatchReason.NotInteger => Resources.Exception.MismatchNotInteger,
				_ => Resources.Exception.MismatchOtherReason
			};
			string? fromString = from.GetGenericString(), toString = to?.GetGenericString();
			return toString is null ? string.Format(format, fromString) : string.Format(format, fromString, toString);
		}
	}

	/// <summary>
	/// The exception to be thrown when a matrix solve method failed due to several reasons
	/// </summary>
	[Serializable]
	public sealed class MatrixSolveAlgorithmException : Exception
	{
		private static string GetDescription(SolveMethodKind kind)
		{
			return kind switch
			{
				SolveMethodKind.Cholesky => Resources.Exception.MatrixSolveCholesky,
				SolveMethodKind.LU => Resources.Exception.MatrixSolveLU,
				SolveMethodKind.QR => Resources.Exception.MatrixSolveQR,
				SolveMethodKind.BunchKaufman => Resources.Exception.MatrixSolveBunchKaufman,
				SolveMethodKind.SVD => Resources.Exception.MatrixSolveSVD,
				SolveMethodKind.Eigenvalue => Resources.Exception.MatrixSolveEigen,
				SolveMethodKind.Schur => Resources.Exception.MatrixSolveSchur,
				SolveMethodKind.GeneralEigen => Resources.Exception.MatrixSolveGeneralEigen,
				SolveMethodKind.Jacobi => Resources.Exception.MatrixSolveJacobi,
				_ => "",
			};
		}

		/// <summary>
		/// Constructor of <see cref="MatrixSolveAlgorithmException"/>
		/// </summary>
		/// <param name="kind">which kind of CUDA solver is used (in <see cref="SolveMethodKind"/>)</param>
		/// <param name="i">returned device info</param>
		public MatrixSolveAlgorithmException(SolveMethodKind kind, int i) : base(string.Format(GetDescription(kind), i)) { }

		/// <summary>
		/// Empty <see cref="MatrixSolveAlgorithmException"/>
		/// </summary>
		public MatrixSolveAlgorithmException() { }

		/// <summary>
		/// <see cref="MatrixSolveAlgorithmException"/> with custom message
		/// </summary>
		/// <param name="message"></param>
		public MatrixSolveAlgorithmException(string message) : base(message) { }

		/// <summary>
		/// <see cref="MatrixSolveAlgorithmException"/> with custom message and inner exception
		/// </summary>
		/// <param name="message"></param>
		/// <param name="innerException"></param>
		public MatrixSolveAlgorithmException(string message, Exception innerException) : base(message, innerException) { }
	}
	#endregion

	#region enum
	/// <summary>
	/// The <see cref="SolveMethodKind"/> enum indicates the classification of a matrix-solving method
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
	/// The <see cref="DiagType"/> enum indicates whether the main diagonal of the dense matrix is unity and consequently should not be touched or modified by the function.
	/// </summary>
	public enum DiagType
	{
		/// <summary>
		/// the matrix diagonal has non-unit elements
		/// </summary>
		NonUnit = 0,
		/// <summary>
		/// the matrix diagonal has unit elements
		/// </summary>
		Unit = 1
	}

	/// <summary>
	/// The <see cref="MatrixOperation"/> enum indicates which simple operation shall be performed to the matrix before the required complicated operation being executed. This may help reduce time or space complexity.
	/// </summary>
	public enum MatrixOperation
	{
		/// <summary>
		/// the non-transpose operation
		/// </summary>
		None = 0,
		/// <summary>
		/// the transpose operation
		/// </summary>
		Transpose = 1,
		/// <summary>
		/// the conjugate transpose operation
		/// </summary>
		ConjugateTranspose = 2,
		/// <summary>
		/// the conjugate only operation
		/// </summary>
		Conjugate = 3,
	}

	// TODO: ????
	/// <summary>
	/// Used in overloading operators
	/// </summary>
	public enum PowerOperation
	{
		/// <summary>
		/// Nothing
		/// </summary>
		None = 0,
		/// <summary>
		/// Transpose only
		/// </summary>
		Transpose = ~0, // -1
		/// <summary>
		/// Conjugate only
		/// </summary>
		Conjugate = int.MaxValue,
		/// <summary>
		/// conjugate transpose
		/// </summary>
		Dagger = ~int.MaxValue // int.MinValue
	}
	#endregion


	#region converters
	// TODO: move to Backend.Cuda.Blas
	internal static class TypeConverters
	{
		internal static sbyte ToChar(this SVDStore store)
		{
			return store switch
			{
				SVDStore.All => (sbyte)'A',
				SVDStore.Economic => (sbyte)'S',
				SVDStore.Overwrite => (sbyte)'O',
				SVDStore.None => (sbyte)'N',
				_ => throw new NotSupportedException(),
			};
		}

		internal static MatrixOperation CheckOP<T>(this MatrixOperation input, Arrays.IMatrix<T> mat) where T : unmanaged, IFormattable, IEquatable<T>
		{
			if (mat is null)
				return default;
			bool isComplex = default(T).IsComplex();
			switch (input)
			{
				case MatrixOperation.Transpose:
					if (mat.Hermitian && !isComplex)
						return MatrixOperation.None;
					else
						return MatrixOperation.Transpose;
				case MatrixOperation.ConjugateTranspose:
					if (mat.Hermitian)
						return MatrixOperation.None;
					else if (!isComplex)
						return MatrixOperation.Transpose;
					else
						return MatrixOperation.ConjugateTranspose;
				case MatrixOperation.Conjugate:
					if (!isComplex)
						return MatrixOperation.None;
					else if (mat.Hermitian)
						return MatrixOperation.Transpose;
					else
						return MatrixOperation.Conjugate;
				default:
					return default;
			}
		}
	}
	#endregion
}
