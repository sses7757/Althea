using System.Runtime.CompilerServices;

using Althea.Linq;
using Althea.LinearAlgebra;
using Althea.TensorAlgebra;


namespace Althea.Arrays
{
	#region vector
	/// <summary>
	/// The interface for vectors' out-of-place conversions.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TVec1">The first concrete type that implements <see cref="IBaseVector{T, TSelf}"/></typeparam>
	/// <typeparam name="TVec2">The second concrete type that implements <see cref="IBaseVector{T, TSelf}"/></typeparam>
	public interface IVectorConversion<T, TVec1, TVec2>
		where T : unmanaged, INumber<T>
		where TVec1 : class, IBaseVector<T, TVec1>
		where TVec2 : class, IBaseVector<T, TVec2>
	{
		/// <summary>
		/// When implemented by a derived class, statically convert the <paramref name="input"/> <typeparamref name="TVec1"/> to a new <typeparamref name="TVec2"/>.
		/// </summary>
		/// <param name="input">The input vector of type <typeparamref name="TVec1"/> to convert</param>
		/// <returns>A created new vector of <typeparamref name="TVec2"/>.</returns>
		public abstract static TVec2 Convert(TVec1 input);

		/// <summary>
		/// When implemented by a derived class, statically convert the <paramref name="input"/> <typeparamref name="TVec2"/> to a new <typeparamref name="TVec1"/>.
		/// </summary>
		/// <param name="input">The input vector of type <typeparamref name="TVec2"/> to convert</param>
		/// <returns>A created new vector of <typeparamref name="TVec1"/>.</returns>
		public abstract static TVec1 Convert(TVec2 input);
	}

	/// <summary>
	/// The interface for vectors' in-place operations.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TVec1">The current concrete type that implements <see cref="IBaseVector{T, TSelf}"/></typeparam>
	/// <typeparam name="TVec2">The other concrete type that implements <see cref="IBaseVector{T, TSelf}"/></typeparam>
	public interface IVectorOperations<T, in TVec1, in TVec2>
		where T : unmanaged, INumber<T>
		where TVec1 : class, IBaseVector<T, TVec1>
		where TVec2 : class, IBaseVector<T, TVec2>
	{
		/// <summary>
		/// When implemented by a derived class, statically compute the dot (inner) product of <paramref name="left"/> and <paramref name="right"/>.
		/// </summary>
		/// <param name="left">The left vector to perform the dot product</param>
		/// <param name="right">The right vector to perform the dot product</param>
		/// <param name="conjugateLeft">Whether the dot product is performed on the conjugation of <paramref name="left"/> or directly.</param>
		/// <returns>The dot (inner) product result as a <typeparamref name="T"/></returns>
		public abstract static T Dot(TVec1 left, TVec2 right, bool conjugateLeft = true);

		/// <summary>
		/// When implemented by a derived class, statically compute the in-place addition of the <paramref name="left"/> vector (scaling by <paramref name="scalar"/>) and <paramref name="right"/> vector.
		/// </summary>
		/// <param name="left">The vector be added to</param>
		/// <param name="right">The other vector to add</param>
		/// <param name="scalar">The scalar to be multiplied to <paramref name="right"/> of type <typeparamref name="T"/></param>
		public abstract static void AddBy(TVec1 left, TVec2 right, T scalar);
	}

	/// <summary>
	/// The interface for vectors' unary vector out-of-place operators.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TVec1">The input vector type that implements <see cref="IBaseVector{T, TSelf}"/></typeparam>
	/// <typeparam name="TVec2">The output vector type that implements <see cref="IBaseVector{T, TSelf}"/></typeparam>
	public interface IVectorUnaryOperators<T, in TVec1, out TVec2>
		where T : unmanaged, INumber<T>
		where TVec1 : class, IBaseVector<T, TVec1>, IVectorUnaryOperators<T, TVec1, TVec2>
		where TVec2 : class, IBaseVector<T, TVec2>
	{
		/// <summary>
		/// When implemented by a derived class, create a new <typeparamref name="TVec2"/> which is the negation result of the given <paramref name="vector"/>
		/// </summary>
		/// <param name="vector">The original vector to negate</param>
		/// <returns>A new <typeparamref name="TVec2"/> which is the negation result of the given <paramref name="vector"/></returns>
		/// <exception cref="InvalidOperationException">If <typeparamref name="T"/> is an unsigned type</exception>
		public abstract static TVec2 operator -(TVec1 vector);

		/// <summary>
		/// When implemented by a derived class, create a new <typeparamref name="TVec2"/> which is the multiplication result of the given <paramref name="vector"/> and <paramref name="scalar"/>
		/// </summary>
		/// <param name="vector">The original vector to multiply</param>
		/// <param name="scalar">The scalar of type <typeparamref name="T"/> to multiply</param>
		/// <returns>A new <typeparamref name="TVec2"/> which is the multiplication result of the given <paramref name="vector"/> and <paramref name="scalar"/></returns>
		public abstract static TVec2 operator *(TVec1 vector, T scalar);

		/// <summary>
		/// When implemented by a derived class, create a new <typeparamref name="TVec2"/> which is the multiplication result of the given <paramref name="vector"/> and <paramref name="scalar"/>
		/// </summary>
		/// <param name="vector">The original vector to multiply</param>
		/// <param name="scalar">The scalar of type <typeparamref name="T"/> to multiply</param>
		/// <returns>A new <typeparamref name="TVec2"/> which is the multiplication result of the given <paramref name="vector"/> and <paramref name="scalar"/></returns>
		public abstract static TVec2 operator *(T scalar, TVec1 vector);

		/// <summary>
		/// When implemented by a derived class, create a new <typeparamref name="TVec2"/> which is the division result of the given <paramref name="vector"/> and <paramref name="scalar"/>
		/// </summary>
		/// <param name="vector">The original vector to be divided</param>
		/// <param name="scalar">The scalar of type <typeparamref name="T"/> to divide</param>
		/// <returns>A new <typeparamref name="TVec2"/> which is the multiplication result of the given <paramref name="vector"/> and <paramref name="scalar"/></returns>
		public abstract static TVec2 operator /(TVec1 vector, T scalar);
	}

	/// <summary>
	/// The interface for vectors' binary out-of-place operators.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TVec1">The first input vector type that implements <see cref="IBaseVector{T, TSelf}"/></typeparam>
	/// <typeparam name="TVec2">The second input vector type that implements <see cref="IBaseVector{T, TSelf}"/></typeparam>
	/// <typeparam name="TVec3">The output vector type that implements <see cref="IBaseVector{T, TSelf}"/></typeparam>
	public interface IVectorBinaryOperators<T, in TVec1, in TVec2, out TVec3>
		where T : unmanaged, INumber<T>
		where TVec1 : class, IBaseVector<T, TVec1>, IVectorBinaryOperators<T, TVec1, TVec2, TVec3>
		where TVec2 : class, IBaseVector<T, TVec2>
		where TVec3 : class, IBaseVector<T, TVec3>
	{
		/// <summary>
		/// When implemented by a derived class, create a new <typeparamref name="TVec3"/> which is the (point-wise) addition result of the given <paramref name="left"/> and <paramref name="right"/> vectors.
		/// </summary>
		/// <param name="left">One original vector as the left operand</param>
		/// <param name="right">One original vector as the right operand</param>
		/// <returns>A new <typeparamref name="TVec3"/> which is the addition result of the given <paramref name="left"/> and <paramref name="right"/> vectors</returns>
		public abstract static TVec3 operator +(TVec1 left, TVec2 right);

		/// <summary>
		/// When implemented by a derived class, create a new <typeparamref name="TVec3"/> which is the (point-wise) subtraction result of the given <paramref name="left"/> and <paramref name="right"/> vectors.
		/// </summary>
		/// <param name="left">One original vector as the left operand</param>
		/// <param name="right">One original vector as the right operand</param>
		/// <returns>A new <typeparamref name="TVec3"/> which is the subtraction result of the given <paramref name="left"/> and <paramref name="right"/> vectors</returns>
		public abstract static TVec3 operator -(TVec1 left, TVec2 right);
	}
	#endregion

	#region vector and matrix
	/// <summary>
	/// The interface for operations that multiply vectors and matrices of different types.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TVec1">The input vector type that implements <see cref="IBaseVector{T, TSelf}"/></typeparam>
	/// <typeparam name="TVec2">The output vector type that implements <see cref="IBaseVector{T, TSelf}"/></typeparam>
	/// <typeparam name="TMat">The input matrix type that implements <see cref="IBaseMatrix{T, TSelf}"/></typeparam>
	public interface IMatrixVectorMultiplyOperations<T, in TVec1, in TVec2, in TMat>
		where T : unmanaged, INumber<T>
		where TVec1 : class, IBaseVector<T, TVec1>
		where TVec2 : class, IBaseVector<T, TVec2>
		where TMat : class, IBaseMatrix<T, TMat>
	{
		/// <summary>
		/// Check the input parameters of <see cref="MatrixMultiplyVector(TMat, TVec1, TVec2, T, T, MatrixOperation)"/> and <see cref="VectorMultiplyMatrix(TVec1, TMat, TVec2, T, T, MatrixOperation)"/>.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static void CheckMatMulVec(TMat matrix, TVec1 vector, TVec2 vectorOut, T α, MatrixOperation operation)
		{
			long n = operation.CanInPlace() ? matrix.NCols : matrix.NRows;
			if (n != vector.Length || vector.Length != vectorOut.Length)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(vector));
			if (α == T.Zero)
				throw new ArgumentException(Resources.ParameterError.CannotZero, nameof(α));
		}

		/// <summary>
		/// When implemented by a derived class, compute the addition of the multiplication result of the given <paramref name="matrix"/> and <paramref name="vector"/> (scaled by <paramref name="α"/>) with <paramref name="vectorOut"/> (scaled by <paramref name="β"/>).
		/// </summary>
		/// <param name="matrix">The input matrix to be multiplied</param>
		/// <param name="vector">The input vector to be multiplied</param>
		/// <param name="vectorOut">The output vector to be added in-place</param>
		/// <param name="α">The scalar to be multiplied to the <paramref name="matrix"/> of type <typeparamref name="T"/></param>
		/// <param name="β">The scalar to be multiplied to <paramref name="vectorOut"/> of type <typeparamref name="T"/></param>
		/// <param name="operation">The simple operation to be applied to <paramref name="matrix"/> before computation as a <see cref="MatrixOperation"/></param>
		public abstract static void MatrixMultiplyVector(TMat matrix, TVec1 vector, TVec2 vectorOut, T α, T β = default, MatrixOperation operation = MatrixOperation.None);

		/// <summary>
		/// When implemented by a derived class, compute the addition of the multiplication result of the given <paramref name="vector"/> and <paramref name="matrix"/> (scaled by <paramref name="α"/>) with <paramref name="vectorOut"/> (scaled by <paramref name="β"/>).
		/// </summary>
		/// <param name="matrix">The input matrix to be multiplied</param>
		/// <param name="vector">The input vector to be multiplied</param>
		/// <param name="vectorOut">The output vector to be added in-place</param>
		/// <param name="α">The scalar to be multiplied to the <paramref name="matrix"/> of type <typeparamref name="T"/></param>
		/// <param name="β">The scalar to be multiplied to <paramref name="vectorOut"/> of type <typeparamref name="T"/></param>
		/// <param name="operation">The simple operation to be applied to <paramref name="matrix"/> before computation as a <see cref="MatrixOperation"/></param>
		public abstract static void VectorMultiplyMatrix(TVec1 vector, TMat matrix, TVec2 vectorOut, T α, T β = default, MatrixOperation operation = MatrixOperation.None);
	}

	/// <summary>
	/// The interface for operators that multiply vectors and matrices of different types out-of-place.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TVec1">The input vector type that implements <see cref="IBaseVector{T, TSelf}"/></typeparam>
	/// <typeparam name="TVec2">The output vector type that implements <see cref="IBaseVector{T, TSelf}"/></typeparam>
	/// <typeparam name="TMat">The input matrix type that implements <see cref="IBaseMatrix{T, TSelf}"/></typeparam>
	public interface IMatrixVectorMultiplyOperators<T, in TVec1, out TVec2, in TMat>
		where T : unmanaged, INumber<T>
		where TVec1 : class, IBaseVector<T, TVec1>
		where TVec2 : class, IBaseVector<T, TVec2>
		where TMat : class, IBaseMatrix<T, TMat>, IMatrixVectorMultiplyOperators<T, TVec1, TVec2, TMat>
	{
		/// <summary>
		/// When implemented by a derived class, compute the multiplication of the given <paramref name="matrix"/> and <paramref name="vector"/>.
		/// </summary>
		/// <param name="matrix">The input matrix to be multiplied</param>
		/// <param name="vector">The input vector to be multiplied</param>
		public abstract static TVec2 operator*(TMat matrix, TVec1 vector);

		/// <summary>
		/// When implemented by a derived class, compute the multiplication of the given <paramref name="vector"/> and <paramref name="matrix"/>.
		/// </summary>
		/// <param name="vector">The input vector to be multiplied</param>
		/// <param name="matrix">The input matrix to be multiplied</param>
		public abstract static TVec2 operator *(TVec1 vector, TMat matrix);
	}

	/// <summary>
	/// The interface for operators that multiply vectors and matrices of different types out-of-place.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TVec1">The input vector type that implements <see cref="IBaseVector{T, TSelf}"/></typeparam>
	/// <typeparam name="TVec2">The output vector type that implements <see cref="IBaseVector{T, TSelf}"/></typeparam>
	/// <typeparam name="TMat">The input matrix type that implements <see cref="IBaseMatrix{T, TSelf}"/></typeparam>
	public interface IVectorMatrixMultiplyOperators<T, in TVec1, out TVec2, in TMat>
		where T : unmanaged, INumber<T>
		where TVec1 : class, IBaseVector<T, TVec1>, IVectorMatrixMultiplyOperators<T, TVec1, TVec2, TMat>
		where TVec2 : class, IBaseVector<T, TVec2>
		where TMat : class, IBaseMatrix<T, TMat>
	{
		/// <summary>
		/// When implemented by a derived class, compute the multiplication of the given <paramref name="matrix"/> and <paramref name="vector"/>.
		/// </summary>
		/// <param name="matrix">The input matrix to be multiplied</param>
		/// <param name="vector">The input vector to be multiplied</param>
		public abstract static TVec2 operator *(TMat matrix, TVec1 vector);

		/// <summary>
		/// When implemented by a derived class, compute the multiplication of the given <paramref name="vector"/> and <paramref name="matrix"/>.
		/// </summary>
		/// <param name="vector">The input vector to be multiplied</param>
		/// <param name="matrix">The input matrix to be multiplied</param>
		public abstract static TVec2 operator *(TVec1 vector, TMat matrix);
	}

	/// <summary>
	/// The interface for operations that get diagonal elements of matrices to new vectors.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TVec">The vector type that implements <see cref="IBaseVector{T, TSelf}"/></typeparam>
	/// <typeparam name="TMat">The matrix type that implements <see cref="IBaseMatrix{T, TSelf}"/></typeparam>
	public interface IMatrixGetDiagonalVector<T, out TVec, in TMat>
		where T : unmanaged, INumber<T>
		where TVec : class, IBaseVector<T, TVec>
		where TMat : class, IBaseMatrix<T, TMat>
	{
		/// <summary>
		/// When implemented by a derived class, get the <paramref name="k"/>-th diagonal elements of <paramref name="matrix"/> as a <typeparamref name="TVec"/>.
		/// </summary>
		/// <param name="matrix">The matrix to obtain diagonal from</param>
		/// <param name="k">The diagonal index: 0 for diagonal, 1 for super-diagonal at one above, -1 for sub-diagonal at one below, etc.</param>
		/// <returns>A <typeparamref name="TVec"/> containing the <paramref name="k"/>-th diagonal elements.</returns>
		/// <exception cref="InvalidOperationException">If this matrix is not a square matrix</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="k"/> is out of range</exception>
		public abstract static TVec GetDiag(TMat matrix, long k);
	}

	/// <summary>
	/// The interface for operations that get diagonal elements of matrices.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TVec">The vector type that implements <see cref="IBaseVector{T, TSelf}"/></typeparam>
	/// <typeparam name="TMat">The matrix type that implements <see cref="IBaseMatrix{T, TSelf}"/></typeparam>
	public interface IMatrixGetDiagonalVectorVariant<T, in TVec, in TMat>
		where T : unmanaged, INumber<T>
		where TVec : class, IBaseVector<T, TVec>
		where TMat : class, IBaseMatrix<T, TMat>
	{
		/// <summary>
		/// When implemented by a derived class, get the <paramref name="k"/>-th diagonal elements of <paramref name="matrix"/> as a <typeparamref name="TVec"/> and write the result to <paramref name="overwrite"/>.
		/// </summary>>
		/// <param name="matrix">The matrix to obtain diagonal from</param>
		/// <param name="k">The diagonal index: 0 for diagonal, 1 for super-diagonal at one above, -1 for sub-diagonal at one below, etc.</param>
		/// <param name="overwrite">The output <typeparamref name="TVec"/> which will contain the <paramref name="k"/>-th diagonal elements at exit</param>
		/// <exception cref="InvalidOperationException">If this matrix is not a square matrix</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="k"/> is out of range</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="overwrite"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="overwrite"/> cannot be overwritten</exception>
		public abstract static void GetDiag(TMat matrix, long k, TVec overwrite);
	}

	/// <summary>
	/// The interface for operations that set diagonal elements of matrices.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TVec">The vector type that implements <see cref="IBaseVector{T, TSelf}"/></typeparam>
	/// <typeparam name="TMat">The matrix type that implements <see cref="IBaseMatrix{T, TSelf}"/></typeparam>
	public interface IMatrixSetDiagonalVector<T, in TVec, in TMat>
		where T : unmanaged, INumber<T>
		where TVec : class, IBaseVector<T, TVec>
		where TMat : class, IBaseMatrix<T, TMat>
	{
		/// <summary>
		/// When implemented by a derived class, set the <paramref name="k"/>-th diagonal elements  of <paramref name="matrix"/> to <paramref name="value"/>.
		/// </summary>
		/// <param name="matrix">The matrix to set diagonal to</param>
		/// <param name="k">The diagonal index: 0 for diagonal, 1 for super-diagonal at one above, -1 for sub-diagonal at one below, etc.</param>
		/// <param name="value">The <paramref name="k"/>-th diagonal elements to set as a <typeparamref name="TVec"/></param>
		/// <exception cref="InvalidOperationException">If this matrix is not a square matrix</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="k"/> is out of range</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="value"/> is null or invalid</exception>
		public abstract static void SetDiag(TMat matrix, long k, TVec value);
	}
	#endregion

	#region matrix
	/// <summary>
	/// The interface for vectors' in-place operations.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TMat1">The first concrete type that implements <see cref="IBaseMatrix{T, TSelf}"/></typeparam>
	/// <typeparam name="TMat2">The second concrete type that implements <see cref="IBaseMatrix{T, TSelf}"/></typeparam>
	/// <typeparam name="TMat3">The third concrete type that implements <see cref="IBaseMatrix{T, TSelf}"/></typeparam>
	public interface IMatrixOperations<T, in TMat1, in TMat2, in TMat3>
		where T : unmanaged, INumber<T>
		where TMat1 : class, IBaseMatrix<T, TMat1>
		where TMat2 : class, IBaseMatrix<T, TMat2>
		where TMat3 : class, IBaseMatrix<T, TMat3>
	{
		/// <summary>
		/// Check the input parameters of <see cref="AddMatrices(TMat1?, T, TMat2?, T, TMat3, MatrixOperation, MatrixOperation)"/>.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static (long m, long n) CheckMatAdd(TMat1? A, T scalarA, TMat2? B, T scalarB, TMat3 C, MatrixOperation opA, MatrixOperation opB)
		{
			bool nullA = A is null || scalarA == T.Zero;
			bool nullB = B is null || scalarB == T.Zero;
			if (nullA && nullB)
				throw new ArgumentException(Resources.ParameterError.CannotAllNull);
			var (m, n) = (C.NRows, C.NCols);
			if (!nullA)
			{
#pragma warning disable CS8602
				var (m1, n1) = (A.NRows, A.NCols);
#pragma warning restore CS8602
				if (!opA.CanInPlace())
					(m1, n1) = (n1, m1);
				if (m1 != m || n1 != n)
					throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(A));
			}
			if (!nullB)
			{
#pragma warning disable CS8602
				var (m1, n1) = (B.NRows, B.NCols);
#pragma warning restore CS8602
				if (!opB.CanInPlace())
					(m1, n1) = (n1, m1);
				if (m1 != m || n1 != n)
					throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(B));
			}
			return (m, n);
		}

		/// <summary>
		/// When implemented by a derived class, statically overwrite <paramref name="C"/> with the addition of <c><paramref name="opA"/>(<paramref name="A"/>) + <paramref name="opB"/>(<paramref name="B"/>)</c>.
		/// </summary>
		/// <param name="scalarA">The scalar to multiply to matrix <paramref name="A"/> before addition</param>
		/// <param name="scalarB">The scalar to multiply to matrix <paramref name="B"/> before addition</param>
		/// <param name="A">The input left matrix to add</param>
		/// <param name="B">The input right matrix to add</param>
		/// <param name="C">The output matrix to be overwritten</param>
		/// <param name="opA">The <see cref="MatrixOperation"/> to apply to matrix <paramref name="A"/> before addition</param>
		/// <param name="opB">The <see cref="MatrixOperation"/> to apply to matrix <paramref name="B"/> before addition</param>
		/// <exception cref="ArgumentException">If both <paramref name="A"/> and <paramref name="B"/> are null or empty; or both <paramref name="scalarA"/> and <paramref name="scalarB"/> are 0; or the addition cannot be performed due to incompatible sizes</exception>
		public abstract static void AddMatrices(TMat1? A, T scalarA, TMat2? B, T scalarB, TMat3 C, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None);

		/// <summary>
		/// Check the input parameters of <see cref="MultiplyMatries(T, TMat1, TMat2, T, TMat3, MatrixOperation, MatrixOperation)"/>.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static (long m, long n, long k) CheckMatMul(T α, TMat1 A, TMat2 B, TMat3 C, MatrixOperation opA, MatrixOperation opB)
		{
			if (α == T.Zero)
				throw new ArgumentException(Resources.ParameterError.CannotZero, nameof(α));
			var (m, n) = (C.NRows, C.NCols);
			var (r, k) = (A.NRows, A.NCols);
			if (!opA.CanInPlace())
				(r, k) = (k, r);
			var (s, t) = (B.NRows, B.NCols);
			if (!opB.CanInPlace())
				(s, t) = (t, s);
			if (r != m || k != s)
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(A));
			if (t != n)
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(B));
			return (m, n, k);
		}

		/// <summary>
		/// When implemented by a derived class, statically overwrite <paramref name="C"/> with the addition of <c><paramref name="α"/> * <paramref name="opA"/>(<paramref name="A"/>) * <paramref name="opB"/>(<paramref name="B"/>) + <paramref name="β"/> * <paramref name="C"/></c>.
		/// </summary>
		/// <param name="α">The scalar to multiply to matrix multiplication result</param>
		/// <param name="β">The scalar to multiply to matrix <paramref name="C"/></param>
		/// <param name="A">The input left matrix to multiply</param>
		/// <param name="B">The input right matrix to multiply</param>
		/// <param name="C">The output matrix to be overwritten</param>
		/// <param name="opA">The <see cref="MatrixOperation"/> to apply to matrix <paramref name="A"/> before multiplication</param>
		/// <param name="opB">The <see cref="MatrixOperation"/> to apply to matrix <paramref name="B"/> before multiplication</param>
		/// <exception cref="ArgumentException">If any of the matrices is null or empty; or the multiplication cannot be performed due to incompatible sizes</exception>
		public abstract static void MultiplyMatries(T α, TMat1 A, TMat2 B, T β, TMat3 C, MatrixOperation opA = MatrixOperation.None, MatrixOperation opB = MatrixOperation.None);
	}

	/// <summary>
	/// The interface for matrices' unary matrix out-of-place operators.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TMat1">The input matrix type that implements <see cref="IBaseMatrix{T, TSelf}"/></typeparam>
	/// <typeparam name="TMat2">The output matrix type that implements <see cref="IBaseMatrix{T, TSelf}"/></typeparam>
	public interface IMatrixUnaryOperators<T, in TMat1, out TMat2>
		where T : unmanaged, INumber<T>
		where TMat1 : class, IBaseMatrix<T, TMat1>, IMatrixUnaryOperators<T, TMat1, TMat2>
		where TMat2 : class, IBaseMatrix<T, TMat2>
	{
		/// <summary>
		/// When implemented by a derived class, create a new <typeparamref name="TMat2"/> which is the point-wise negation result of the given <paramref name="matrix"/>.
		/// </summary>
		/// <param name="matrix">The input <typeparamref name="TMat1"/> whose elements will be used</param>
		/// <returns>A new <typeparamref name="TMat2"/> as the negation of <paramref name="matrix"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="matrix"/> is null or empty</exception>
		public abstract static TMat2 operator -(TMat1 matrix);

		/// <summary>
		/// When implemented by a derived class, create a new <typeparamref name="TMat2"/> which is the point-wise multiplication result of the given <paramref name="matrix"/> and <paramref name="scalar"/>.
		/// </summary>
		/// <param name="matrix">The input <typeparamref name="TMat1"/> whose elements will be used</param>
		/// <param name="scalar">The input scalar used as the multiplier</param>
		/// <returns>A new <typeparamref name="TMat2"/> as the result of <paramref name="matrix"/> * <paramref name="scalar"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="matrix"/> is null or empty</exception>
		public abstract static TMat2 operator *(TMat1 matrix, T scalar);

		/// <summary>
		/// When implemented by a derived class, create a new <typeparamref name="TMat2"/> which is the point-wise multiplication result of the given <paramref name="matrix"/> and <paramref name="scalar"/>.
		/// </summary>
		/// <param name="matrix">The input <typeparamref name="TMat1"/> whose elements will be used</param>
		/// <param name="scalar">The input scalar used as the multiplier</param>
		/// <returns>A new <typeparamref name="TMat2"/> as the result of <paramref name="matrix"/> * <paramref name="scalar"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="matrix"/> is null or empty</exception>
		public abstract static TMat2 operator *(T scalar, TMat1 matrix);

		/// <summary>
		/// When implemented by a derived class, create a new <typeparamref name="TMat2"/> which is the point-wise division result of the given <paramref name="matrix"/> and <paramref name="scalar"/>.
		/// </summary>
		/// <param name="matrix">The input <typeparamref name="TMat1"/> whose elements will be used</param>
		/// <param name="scalar">The input scalar used as the divider</param>
		/// <returns>A new <typeparamref name="TMat2"/> as the result of <paramref name="matrix"/> / <paramref name="scalar"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="matrix"/> is null or empty</exception>
		public abstract static TMat2 operator /(TMat1 matrix, T scalar);

		/// <summary>
		/// When implemented by a derived class, create a new <typeparamref name="TMat2"/> which is the simple operation result of the given <paramref name="matrix"/> under <paramref name="operation"/>.
		/// </summary>
		/// <param name="matrix">The input <typeparamref name="TMat1"/> whose elements will be used</param>
		/// <param name="operation">The input <see cref="MatrixOperation"/> used as the operation</param>
		/// <returns>A new <typeparamref name="TMat2"/> as the result of <paramref name="operation"/>(<paramref name="matrix"/>)</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="matrix"/> is null or empty</exception>
		public abstract static TMat2 operator ^(TMat1 matrix, MatrixOperation operation);
	}

	/// <summary>
	/// The interface for matrices' binary matrix out-of-place operators.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TMat1">The first input matrix type that implements <see cref="IBaseMatrix{T, TSelf}"/></typeparam>
	/// <typeparam name="TMat2">The second input matrix type that implements <see cref="IBaseMatrix{T, TSelf}"/></typeparam>
	/// <typeparam name="TMat3">The output matrix type that implements <see cref="IBaseMatrix{T, TSelf}"/></typeparam>
	public interface IMatrixBinaryOperators<T, in TMat1, in TMat2, out TMat3>
		where T : unmanaged, INumber<T>
		where TMat1 : class, IBaseMatrix<T, TMat1>, IMatrixBinaryOperators<T, TMat1, TMat2, TMat3>
		where TMat2 : class, IBaseMatrix<T, TMat2>
		where TMat3 : class, IBaseMatrix<T, TMat3>
	{
		/// <summary>
		/// When implemented by a derived class, create a new <typeparamref name="TMat3"/> which is the point-wise addition result of the given <paramref name="left"/> and <paramref name="right"/> matrices.
		/// </summary>
		/// <param name="left">The input left <typeparamref name="TMat1"/> to be added</param>
		/// <param name="right">The input right <typeparamref name="TMat2"/> to be added</param>
		/// <returns>A new <typeparamref name="TMat3"/> as the result of <paramref name="left"/> + <paramref name="right"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="left"/> or <paramref name="right"/> is null or empty</exception>
		/// <exception cref="ArgumentException">If the addition cannot be performed due to incompatible sizes</exception>
		public abstract static TMat3 operator +(TMat1 left, TMat2 right);

		/// <summary>
		/// When implemented by a derived class, create a new <typeparamref name="TMat3"/> which is the point-wise addition result of the given <paramref name="left"/> and <paramref name="right"/> matrices.
		/// </summary>
		/// <param name="left">The input left <typeparamref name="TMat2"/> to be added</param>
		/// <param name="right">The input right <typeparamref name="TMat1"/> to be added</param>
		/// <returns>A new <typeparamref name="TMat3"/> as the result of <paramref name="left"/> + <paramref name="right"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="left"/> or <paramref name="right"/> is null or empty</exception>
		/// <exception cref="ArgumentException">If the addition cannot be performed due to incompatible sizes</exception>
		public abstract static TMat3 operator +(TMat2 left, TMat1 right);

		/// <summary>
		/// When implemented by a derived class, create a new <typeparamref name="TMat3"/> which is the point-wise subtraction result of the given <paramref name="left"/> and <paramref name="right"/> matrices.
		/// </summary>
		/// <param name="left">The input left <typeparamref name="TMat1"/> to be subtracted from</param>
		/// <param name="right">The input right <typeparamref name="TMat2"/> to subtract</param>
		/// <returns>A new <typeparamref name="TMat3"/> as the result of <paramref name="left"/> - <paramref name="right"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="left"/> or <paramref name="right"/> is null or empty</exception>
		/// <exception cref="ArgumentException">If the subtraction cannot be performed due to incompatible sizes</exception>
		public abstract static TMat3 operator -(TMat1 left, TMat2 right);

		/// <summary>
		/// When implemented by a derived class, create a new <typeparamref name="TMat3"/> which is the point-wise subtraction result of the given <paramref name="left"/> and <paramref name="right"/> matrices.
		/// </summary>
		/// <param name="left">The input left <typeparamref name="TMat2"/> to be subtracted from</param>
		/// <param name="right">The input right <typeparamref name="TMat1"/> to subtract</param>
		/// <returns>A new <typeparamref name="TMat3"/> as the result of <paramref name="left"/> - <paramref name="right"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="left"/> or <paramref name="right"/> is null or empty</exception>
		/// <exception cref="ArgumentException">If the subtraction cannot be performed due to incompatible sizes</exception>
		public abstract static TMat3 operator -(TMat2 left, TMat1 right);

		/// <summary>
		/// When implemented by a derived class, create a new <typeparamref name="TMat3"/> which is the matrix multiplication result of the given <paramref name="left"/> and <paramref name="right"/> matrices.
		/// </summary>
		/// <param name="left">The input <typeparamref name="TMat1"/> to be multiplied at left</param>
		/// <param name="right">The input <typeparamref name="TMat2"/> to be multiplied at right</param>
		/// <returns>A new <typeparamref name="TMat3"/> as the result of <paramref name="left"/> * <paramref name="right"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="left"/> or <paramref name="right"/> is null or empty</exception>
		/// <exception cref="ArgumentException">If the multiplication cannot be performed due to incompatible sizes</exception>
		public abstract static TMat3 operator *(TMat1 left, TMat2 right);

		/// <summary>
		/// When implemented by a derived class, create a new <typeparamref name="TMat3"/> which is the matrix multiplication result of the given <paramref name="left"/> and <paramref name="right"/> matrices.
		/// </summary>
		/// <param name="left">The input <typeparamref name="TMat2"/> to be multiplied at left</param>
		/// <param name="right">The input <typeparamref name="TMat1"/> to be multiplied at right</param>
		/// <returns>A new <typeparamref name="TMat3"/> as the result of <paramref name="left"/> * <paramref name="right"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="left"/> or <paramref name="right"/> is null or empty</exception>
		/// <exception cref="ArgumentException">If the multiplication cannot be performed due to incompatible sizes</exception>
		public abstract static TMat3 operator *(TMat2 left, TMat1 right);
	}

	/// <summary>
	/// The interface for matrices' in-place linear solver.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TMat1">The first matrix concrete type that implements <see cref="IBaseMatrix{T, TSelf}"/></typeparam>
	/// <typeparam name="TMat2">The second matrix concrete type that implements <see cref="IBaseMatrix{T, TSelf}"/></typeparam>
	/// <typeparam name="TMat3">The third matrix concrete type that implements <see cref="IBaseMatrix{T, TSelf}"/></typeparam>
	public interface IMatrixLinearSolve<T, in TMat1, in TMat2, in TMat3>
		where T : unmanaged, INumber<T>
		where TMat1 : class, IBaseMatrix<T, TMat1>
		where TMat2 : class, IBaseMatrix<T, TMat2>
		where TMat3 : class, IBaseMatrix<T, TMat3>
	{
		/// <summary>
		/// Check the input parameters of <see cref="LinearSolve(TMat1, TMat2, TMat3, MatrixOperation)"/>.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static void CheckLinear(TMat1 coefficients, TMat2 rightHandSides, TMat3 outSolves)
		{
			if (coefficients.NRows != coefficients.NCols || coefficients.NRows != rightHandSides.NRows)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(coefficients));
			if (rightHandSides.NRows != outSolves.NRows || rightHandSides.NCols != outSolves.NCols)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(rightHandSides));
		}

		/// <summary>
		/// When implemented by a derived class, compute solves of the linear systems: <c><paramref name="opCoef"/>(<paramref name="coefficients"/>) * <paramref name="outSolves"/> == <paramref name="rightHandSides"/></c>.
		/// </summary>
		/// <param name="coefficients">The input coefficient matrix to be solved</param>
		/// <param name="rightHandSides">The input right-hand-side matrix to be solved</param>
		/// <param name="outSolves">The output solve matrix</param>
		/// <param name="opCoef">The operation to apply to <paramref name="coefficients"/> during calculation</param>
		/// <exception cref="MatrixSolveAlgorithmException">If the internal solver failed due to some reason</exception>
		public abstract static void LinearSolve(TMat1 coefficients, TMat2 rightHandSides, TMat3 outSolves, MatrixOperation opCoef = MatrixOperation.None);
	}

	/// <summary>
	/// The interface for matrices' in-place least square solver.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TMat1">The first matrix concrete type that implements <see cref="IBaseMatrix{T, TSelf}"/></typeparam>
	/// <typeparam name="TMat2">The second matrix concrete type that implements <see cref="IBaseMatrix{T, TSelf}"/></typeparam>
	/// <typeparam name="TMat3">The third matrix concrete type that implements <see cref="IBaseMatrix{T, TSelf}"/></typeparam>
	public interface IMatrixLeastSolve<T, in TMat1, in TMat2, in TMat3>
		where T : unmanaged, INumber<T>
		where TMat1 : class, IBaseMatrix<T, TMat1>
		where TMat2 : class, IBaseMatrix<T, TMat2>
		where TMat3 : class, IBaseMatrix<T, TMat3>
	{
		/// <summary>
		/// Check the input parameters of <see cref="LeastSquareSolve(TMat1, TMat2, TMat3)"/>.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static void CheckLeast(TMat1 coefficients, TMat2 rightHandSides, TMat3 outSolves)
		{
			if (coefficients.NRows <= coefficients.NCols || coefficients.NRows != rightHandSides.NRows)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(coefficients));
			if (rightHandSides.NRows != outSolves.NRows || rightHandSides.NCols != outSolves.NCols)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(rightHandSides));
		}

		/// <summary>
		/// When implemented by a derived class, least square solve the linear systems: <c><paramref name="coefficients"/> * <paramref name="outSolves"/> == <paramref name="rightHandSides"/></c>.
		/// </summary>
		/// <param name="coefficients">The input coefficient matrix to be solved</param>
		/// <param name="rightHandSides">The input right-hand-side matrix to be solved</param>
		/// <param name="outSolves">The output solve matrix</param>
		/// <exception cref="MatrixSolveAlgorithmException">If the internal solver failed due to some reason</exception>
		public abstract static void LeastSquareSolve(TMat1 coefficients, TMat2 rightHandSides, TMat3 outSolves);
	}

	/// <summary>
	/// The interface for matrices' in-place QR solvers.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TMat1">The first matrix concrete type that implements <see cref="IBaseMatrix{T, TSelf}"/></typeparam>
	/// <typeparam name="TMat2">The second matrix concrete type that implements <see cref="IBaseMatrix{T, TSelf}"/></typeparam>
	/// <typeparam name="TMat3">The third matrix concrete type that implements <see cref="IBaseMatrix{T, TSelf}"/></typeparam>
	public interface IMatrixQRSolve<T, in TMat1, in TMat2, in TMat3>
		where T : unmanaged, INumber<T>
		where TMat1 : class, IBaseMatrix<T, TMat1>
		where TMat2 : class, IBaseMatrix<T, TMat2>
		where TMat3 : class, IBaseMatrix<T, TMat3>
	{
		/// <summary>
		/// Check the input parameters of <see cref="QRDecomposition(TMat1, TMat2, TMat3?, bool)"/>.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static void CheckQR(TMat1 matrix, TMat2 outTriangular, TMat3? outUnary, bool full)
		{
			if (matrix.NRows == matrix.NCols)
			{
				if (outTriangular.NRows != matrix.NRows || outTriangular.NCols != matrix.NCols)
					throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(outTriangular));
				if (outUnary is not null && (outUnary.NRows != matrix.NRows || outUnary.NCols != matrix.NCols))
					throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(outUnary));
			}
			else if (matrix.NRows > matrix.NCols)
			{
				if (outTriangular.NRows != matrix.NCols || outTriangular.NCols != matrix.NCols)
					throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(outTriangular));
				if (outUnary is not null && (outUnary.NRows != matrix.NRows || outUnary.NCols != (full ? matrix.NRows : matrix.NCols)))
					throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(outUnary));
			}
			else //if (matrix.NRows < matrix.NCols)
			{
				if (outTriangular.NRows != matrix.NRows || outTriangular.NCols != matrix.NCols)
					throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(outTriangular));
				if (outUnary is not null && (outUnary.NRows != matrix.NRows || outUnary.NCols != matrix.NRows))
					throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(outUnary));
			}
		}

		/// <summary>
		/// When implemented by a derived class, QR solve the input <paramref name="matrix"/> and write the result triangular .
		/// </summary>
		/// <param name="matrix">The input matrix to be QR decomposed</param>
		/// <param name="outTriangular">The output triangular matrix, which can be <paramref name="matrix"/></param>
		/// <param name="outUnary">The output unary matrix, which can be <paramref name="matrix"/> if the dimension allows, null means do not calculate it</param>
		/// <param name="full">Whether to compute the full QR or partial QR</param>
		/// <exception cref="ArgumentException">If the sizes are incompatible</exception>
		/// <exception cref="MatrixSolveAlgorithmException">If the internal solver failed due to some reason</exception>
		public abstract static void QRDecomposition(TMat1 matrix, TMat2 outTriangular, TMat3? outUnary, bool full = false);
	}
	#endregion

	#region tensor
	/// <summary>
	/// The interface for tensors' in-place operations for two tensors.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TTen1">The first concrete type that implements <see cref="IBaseTensor{T, TSelf}"/></typeparam>
	/// <typeparam name="TTen2">The second concrete type that implements <see cref="IBaseTensor{T, TSelf}"/></typeparam>
	public interface ITensorOperations<T, in TTen1, in TTen2>
		where T : unmanaged, INumber<T>
		where TTen1 : class, IBaseTensor<T, TTen1>
		where TTen2 : class, IBaseTensor<T, TTen2>
	{
		/// <summary>
		/// Check the input parameters of <see cref="Reduce(TTen1, TensorOrder, T, TTen2, UnaryOperation, BinaryOperation)"/>.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static Span<int> CheckReduce(TTen1 A, TensorOrder order, T scalar, TTen2 B, Span<int> reduceInds)
		{
			if (scalar == T.Zero)
				throw new ArgumentOutOfRangeException(nameof(scalar), scalar, Resources.ParameterError.CannotZero);
			reduceInds = order.GetOrder(A, reduceInds, true);
			if (A.Rank - reduceInds.Length == 0)
			{
				if (B.Rank != 1 || B.Length != 1)
					throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(B));
			}
			else
			{
				if (A.Rank - reduceInds.Length != B.Rank)
					throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(order));
			}
			return reduceInds;
		}

		/// <summary>
		/// When implemented by a derived class, compute the tensor reduction (self partial summation) of tensor <paramref name="A"/> under the given <paramref name="order"/> to tensor <paramref name="B"/>.
		/// </summary>
		/// <param name="A">The input tensor to reduce</param>
		/// <param name="order">The given <see cref="TensorOrder"/> to indicate which part(s) of dimension(s) in <paramref name="A"/> to sum, its order will be ignored</param>
		/// <param name="scalar">The scalar to multiply to the result</param>
		/// <param name="B">The output tensor to be replaced</param>
		/// <param name="opA">The <see cref="UnaryOperation"/> to apply to each element of <typeparamref name="TTen1"/> during the operation</param>
		/// <param name="reduce">The <see cref="BinaryOperation"/> used to reduce elements</param>
		/// <exception cref="ArgumentException">If <paramref name="order"/> does not indicate a partial permutation order</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="scalar"/> is 0</exception>
		public abstract static void Reduce(TTen1 A, TensorOrder order, T scalar, TTen2 B, UnaryOperation opA = UnaryOperation.Identity, BinaryOperation reduce = BinaryOperation.Addition);

		/// <summary>
		/// Check the input parameters of <see cref="Permute(TTen1, TensorOrder, T, TTen2, UnaryOperation)"/>.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static void CheckPermute(TTen1 A, TensorOrder order, T scalar, TTen2 B, Span<int> perm)
		{
			if (scalar == T.Zero)
				throw new ArgumentOutOfRangeException(nameof(scalar), scalar, Resources.ParameterError.CannotZero);
			if (A.Rank != B.Rank)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(B));
			order.GetOrder(A, perm, false);
			Span<long> permA = stackalloc long[A.Rank];
			A.Size.ReOrderTo(permA, perm);
			if (!permA.SequenceEqual(B.Size))
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(B));
		}

		/// <summary>
		/// When implemented by a derived class, compute the tensor permutation of tensor <paramref name="A"/> under the given <paramref name="order"/> to tensor <paramref name="B"/>.
		/// </summary>
		/// <param name="A">The input tensor to permute</param>
		/// <param name="order">The given <see cref="TensorOrder"/> to indicate the permutation order</param>
		/// <param name="scalar">The scalar to multiply to the result</param>
		/// <param name="B">The output tensor to be replaced</param>
		/// <param name="opA">The <see cref="UnaryOperation"/> to apply to each element during the operation</param>
		/// <exception cref="ArgumentException">If <paramref name="order"/> does not indicate a full permutation order</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="scalar"/> is 0</exception>
		public abstract static void Permute(TTen1 A, TensorOrder order, T scalar, TTen2 B, UnaryOperation opA = UnaryOperation.Identity);
	}

	/// <summary>
	/// The interface for tensors' in-place operations for three tensors.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TTen1">The first concrete type that implements <see cref="IBaseTensor{T, TSelf}"/></typeparam>
	/// <typeparam name="TTen2">The second concrete type that implements <see cref="IBaseTensor{T, TSelf}"/></typeparam>
	/// <typeparam name="TTen3">The third concrete type that implements <see cref="IBaseTensor{T, TSelf}"/></typeparam>
	public interface ITensorOperations<T, in TTen1, in TTen2, in TTen3>
		where T : unmanaged, INumber<T>
		where TTen1 : class, IBaseTensor<T, TTen1>
		where TTen2 : class, IBaseTensor<T, TTen2>
		where TTen3 : class, IBaseTensor<T, TTen3>
	{
		/// <summary>
		/// Check the input parameters of <see cref="Contract(TTen1, UnaryOperation, TTen2, UnaryOperation, T, TTen3, UnaryOperation, T)"/>.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static StorableContractInfo CheckContract(TTen1 A, TTen2 B, T α, TTen3 C)
		{
			if (α == T.Zero)
				throw new ArgumentOutOfRangeException(nameof(α), α, Resources.ParameterError.CannotZero);
			int rank = TensorContractInfo.GetContractRank(A, B);
			Span<int> leftConc = stackalloc int[rank];
			Span<int> rightConc = stackalloc int[rank];
			Span<int> leftFree = stackalloc int[A.Rank - rank];
			Span<int> rightFree = stackalloc int[B.Rank - rank];
			var info = TensorContractInfo.Create(A, B, C, leftConc, rightConc, leftFree, rightFree);
			return info;
		}

		/// <summary>
		/// When implemented by a derived class, compute the tensor contraction-addition: <c><paramref name="C"/> = <paramref name="α"/> .* <paramref name="opA"/>(<paramref name="A"/>) * <paramref name="opB"/>(<paramref name="B"/>) + <paramref name="β"/> .* <paramref name="opC"/>(<paramref name="C"/>)</c>.
		/// </summary>
		/// <remarks>The labels of <paramref name="A"/>, <paramref name="B"/> and <paramref name="C"/> are used to guide contraction dimensions.</remarks>
		/// <param name="A">The first input contraction tensor</param>
		/// <param name="B">The second input contraction tensor</param>
		/// <param name="C">The input/output addition tensor</param>
		/// <param name="α">The scalar to multiply to <paramref name="A"/> or <paramref name="B"/> during operation</param>
		/// <param name="β">The scalar to multiply to <paramref name="C"/> during operation</param>
		/// <param name="opA">The <see cref="UnaryOperation"/> to apply to elements of <paramref name="A"/> during operation</param>
		/// <param name="opB">The <see cref="UnaryOperation"/> to apply to elements of <paramref name="B"/> during operation</param>
		/// <param name="opC">The <see cref="UnaryOperation"/> to apply to elements of <paramref name="C"/> during operation</param>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/>, <paramref name="B"/> or <paramref name="C"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="A"/>, <paramref name="B"/> or <paramref name="C"/>'s labels indicate that they cannot contract or add</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="α"/> is 0</exception>
		public abstract static void Contract(TTen1 A, UnaryOperation opA, TTen2 B, UnaryOperation opB, T α, TTen3 C, UnaryOperation opC, T β);

		/// <summary>
		/// Check the input parameters of <see cref="TensorsBinaryOperation(TTen1?, TensorOrder, UnaryOperation, T, TTen2?, TensorOrder, UnaryOperation, T, TTen3, BinaryOperation)"/>.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static void CheckBinary(TTen1? A, TensorOrder orderA, T α, TTen2? B, TensorOrder orderB, T β, TTen3 C, Span<int> permA, Span<int> permB)
		{
			bool nullA = A is null || α == T.Zero;
			bool nullB = B is null || β == T.Zero;
			if (nullA && nullB)
				throw new ArgumentException(Resources.ParameterError.CannotAllNull);
#pragma warning disable CS8602, CS8604
			if (!nullA)
			{
				orderA.GetOrder(A, permA, false);
				Span<long> newSizeA = stackalloc long[A.Rank];
				A.Size.ReOrderTo(newSizeA, permA);
				if (!C.Size.SequenceEqual(newSizeA))
					throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(A));
			}
			if (!nullB)
			{
				orderB.GetOrder(B, permB, false);
				Span<long> newSizeB = stackalloc long[B.Rank];
				B.Size.ReOrderTo(newSizeB, permB);
				if (!C.Size.SequenceEqual(newSizeB))
					throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(B));
			}
#pragma warning restore CS8602, CS8604
		}

		/// <summary>
		/// When implemented by a derived class, compute the tensor point-wise binary operation: <c><paramref name="C"/> = <paramref name="binary"/>(<paramref name="α"/> .* <paramref name="opA"/>(<paramref name="A"/>[<paramref name="orderA"/>]), <paramref name="β"/> .* <paramref name="opB"/>(<paramref name="B"/>[<paramref name="orderB"/>]))</c>.
		/// </summary>
		/// <remarks>The labels of <paramref name="A"/>, <paramref name="B"/> and <paramref name="C"/> are used to guide permutations. If <paramref name="A"/> or <paramref name="B"/> is null (<paramref name="α"/> or <paramref name="β"/> is 0), <paramref name="binary"/> is not used.</remarks>
		/// <param name="A">The first input tensor</param>
		/// <param name="B">The second input tensor</param>
		/// <param name="C">The output result tensor</param>
		/// <param name="orderA">The <see cref="TensorOrder"/> indicating the permutation order of <paramref name="A"/></param>
		/// <param name="orderB">The <see cref="TensorOrder"/> indicating the permutation order of <paramref name="B"/></param>
		/// <param name="α">The scalar to multiply to <paramref name="A"/> during operation</param>
		/// <param name="β">The scalar to multiply to <paramref name="B"/> during operation</param>
		/// <param name="opA">The <see cref="UnaryOperation"/> to apply to elements of <paramref name="A"/> during operation</param>
		/// <param name="opB">The <see cref="UnaryOperation"/> to apply to elements of <paramref name="B"/> during operation</param>
		/// <param name="binary">The <see cref="BinaryOperation"/> to apply simultaneously to both elements of <paramref name="A"/> and <paramref name="B"/></param>
		/// <exception cref="ArgumentNullException">If both <paramref name="A"/> and <paramref name="B"/> or <paramref name="C"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If the operation cannot be performed due to incompatible size(s)</exception>
		public abstract static void TensorsBinaryOperation(TTen1? A, TensorOrder orderA, UnaryOperation opA, T α, TTen2? B, TensorOrder orderB, UnaryOperation opB, T β, TTen3 C, BinaryOperation binary);
	}

	/// <summary>
	/// The interface for tensors' unary tensor out-of-place operators.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TTen1">The input concrete type that implements <see cref="IBaseTensor{T, TSelf}"/></typeparam>
	/// <typeparam name="TTen2">The output concrete type that implements <see cref="IBaseTensor{T, TSelf}"/></typeparam>
	public interface ITensorUnaryOperators<T, in TTen1, out TTen2>
		where T : unmanaged, INumber<T>
		where TTen1 : class, IBaseTensor<T, TTen1>, ITensorUnaryOperators<T, TTen1, TTen2>
		where TTen2 : class, IBaseTensor<T, TTen2>
	{
		/// <summary>
		/// When implemented by a derived class, create a new <typeparamref name="TTen2"/> which is the which is the permutation of the given <paramref name="tensor"/> under <paramref name="order"/>.
		/// </summary>
		/// <param name="tensor">One original tensor as the left operand</param>
		/// <param name="order">The <see cref="TensorOrder"/> indicating the permutation order</param>
		/// <returns>A new <typeparamref name="TTen2"/> which is the permutation result of the given <paramref name="tensor"/> under <paramref name="order"/></returns>
		public abstract static TTen2 operator ^(TTen1 tensor, TensorOrder order);

		/// <summary>
		/// When implemented by a derived class, create a new <typeparamref name="TTen2"/> which is the (point-wise) multiplication result of the given <paramref name="tensor"/> and <paramref name="scalar"/>
		/// </summary>
		/// <param name="tensor">The original tensor to multiply</param>
		/// <param name="scalar">The scalar of type <typeparamref name="T"/> to multiply</param>
		/// <returns>A new <typeparamref name="TTen2"/> which is the multiplication result of the given <paramref name="tensor"/> and <paramref name="scalar"/></returns>
		public abstract static TTen2 operator *(TTen1 tensor, T scalar);

		/// <summary>
		/// When implemented by a derived class, create a new <typeparamref name="TTen2"/> which is the multiplication result of the given <paramref name="tensor"/> and <paramref name="scalar"/>
		/// </summary>
		/// <param name="tensor">The original tensor to multiply</param>
		/// <param name="scalar">The scalar of type <typeparamref name="T"/> to multiply</param>
		/// <returns>A new <typeparamref name="TTen2"/> which is the multiplication result of the given <paramref name="tensor"/> and <paramref name="scalar"/></returns>
		public abstract static TTen2 operator *(T scalar, TTen1 tensor);

		/// <summary>
		/// When implemented by a derived class, create a new <typeparamref name="TTen2"/> which is the negation result of the given <paramref name="tensor"/>
		/// </summary>
		/// <param name="tensor">The original tensor to negate</param>
		/// <returns>A new <typeparamref name="TTen2"/> which is the negation result of the given <paramref name="tensor"/></returns>
		public abstract static TTen2 operator -(TTen1 tensor);

		/// <summary>
		/// When implemented by a derived class, create a new <typeparamref name="TTen2"/> which is the division result of the given <paramref name="tensor"/> and <paramref name="scalar"/>
		/// </summary>
		/// <param name="tensor">The original tensor to be divided</param>
		/// <param name="scalar">The scalar of type <typeparamref name="T"/> to divide</param>
		/// <returns>A new <typeparamref name="TTen2"/> which is the multiplication result of the given <paramref name="tensor"/> and <paramref name="scalar"/></returns>
		public abstract static TTen2 operator /(TTen1 tensor, T scalar);
	}

	/// <summary>
	/// The interface for tensors' unary tensor out-of-place operators.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TTen1">The first input concrete type that implements <see cref="IBaseTensor{T, TSelf}"/></typeparam>
	/// <typeparam name="TTen2">The second input concrete type that implements <see cref="IBaseTensor{T, TSelf}"/></typeparam>
	/// <typeparam name="TTen3">The output concrete type that implements <see cref="IBaseTensor{T, TSelf}"/></typeparam>
	public interface ITensorBinaryOperators<T, in TTen1, in TTen2, out TTen3>
		where T : unmanaged, INumber<T>
		where TTen1 : class, IBaseTensor<T, TTen1>, ITensorBinaryOperators<T, TTen1, TTen2, TTen3>
		where TTen2 : class, IBaseTensor<T, TTen2>
		where TTen3 : class, IBaseTensor<T, TTen3>
	{
		/// <summary>
		/// When implemented by a derived class, create a new <typeparamref name="TTen3"/> which is the which is the tensor contraction of the given <paramref name="left"/> and <paramref name="right"/> tensors.
		/// </summary>
		/// <param name="left">One original tensor as the left operand</param>
		/// <param name="right">One original tensor as the right operand</param>
		/// <returns>A new <typeparamref name="TTen3"/> which is the contraction result of the given <paramref name="left"/> and <paramref name="right"/> tensors</returns>
		public abstract static TTen3 operator *(TTen1 left, TTen2 right);

		/// <summary>
		/// When implemented by a derived class, create a new <typeparamref name="TTen3"/> which is the (point-wise) addition result of the given <paramref name="left"/> and <paramref name="right"/> tensors.
		/// </summary>
		/// <param name="left">One original tensor as the left operand</param>
		/// <param name="right">One original tensor as the right operand</param>
		/// <returns>A new <typeparamref name="TTen3"/> which is the addition result of the given <paramref name="left"/> and <paramref name="right"/> tensor</returns>
		public abstract static TTen3 operator +(TTen1 left, TTen2 right);

		/// <summary>
		/// When implemented by a derived class, create a new <typeparamref name="TTen3"/> which is the (point-wise) subtraction result of the given <paramref name="left"/> and <paramref name="right"/> tensors.
		/// </summary>
		/// <param name="left">One original tensor as the left operand</param>
		/// <param name="right">One original tensor as the right operand</param>
		/// <returns>A new <typeparamref name="TTen3"/> which is the addition result of the given <paramref name="left"/> and <paramref name="right"/> tensor</returns>
		public abstract static TTen3 operator -(TTen1 left, TTen2 right);

		/// <summary>
		/// When implemented by a derived class, create a new <typeparamref name="TTen3"/> which is the which is the tensor contraction of the given <paramref name="left"/> and <paramref name="right"/> tensors.
		/// </summary>
		/// <param name="left">One original tensor as the left operand</param>
		/// <param name="right">One original tensor as the right operand</param>
		/// <returns>A new <typeparamref name="TTen3"/> which is the contraction result of the given <paramref name="left"/> and <paramref name="right"/> tensors</returns>
		public abstract static TTen3 operator *(TTen2 left, TTen1 right);

		/// <summary>
		/// When implemented by a derived class, create a new <typeparamref name="TTen3"/> which is the (point-wise) addition result of the given <paramref name="left"/> and <paramref name="right"/> tensors.
		/// </summary>
		/// <param name="left">One original tensor as the left operand</param>
		/// <param name="right">One original tensor as the right operand</param>
		/// <returns>A new <typeparamref name="TTen3"/> which is the addition result of the given <paramref name="left"/> and <paramref name="right"/> tensor</returns>
		public abstract static TTen3 operator +(TTen2 left, TTen1 right);

		/// <summary>
		/// When implemented by a derived class, create a new <typeparamref name="TTen3"/> which is the (point-wise) subtraction result of the given <paramref name="left"/> and <paramref name="right"/> tensors.
		/// </summary>
		/// <param name="left">One original tensor as the left operand</param>
		/// <param name="right">One original tensor as the right operand</param>
		/// <returns>A new <typeparamref name="TTen3"/> which is the addition result of the given <paramref name="left"/> and <paramref name="right"/> tensor</returns>
		public abstract static TTen3 operator -(TTen2 left, TTen1 right);
	}
	#endregion
}
