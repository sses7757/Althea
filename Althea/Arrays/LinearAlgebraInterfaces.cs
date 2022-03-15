using System;
using System.Collections.Generic;

using Althea.LinearAlgebra;


namespace Althea.Arrays
{
	/// <summary>
	/// The interface for vectors' out-of-place operators.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TVec1">The first input vector type</typeparam>
	/// <typeparam name="TVec2">The second input vector type</typeparam>
	/// <typeparam name="TVec3">The output vector type</typeparam>
	public interface IVectorOperators<T, in TVec1, in TVec2, out TVec3>
		where T : unmanaged, INumber<T>
		where TVec1 : class, IBaseVector<T, TVec1>, IVectorOperators<T, TVec1, TVec2, TVec3>
		where TVec2 : class, IBaseVector<T, TVec2>
		where TVec3 : class, IBaseVector<T, TVec3>
	{
		/// <summary>
		/// When implemented by a derived class, statically create a new <typeparamref name="TVec3"/> which is the (point-wise) addition result of the given <paramref name="left"/> and <paramref name="right"/> vectors.
		/// </summary>
		/// <param name="left">One original vector as the left operand</param>
		/// <param name="right">One original vector as the right operand</param>
		/// <returns>A new <typeparamref name="TVec3"/> which is the addition result of the given <paramref name="left"/> and <paramref name="right"/> vectors</returns>
		public abstract static TVec3 operator +(TVec1 left, TVec2 right);

		/// <summary>
		/// When implemented by a derived class, statically create a new <typeparamref name="TVec3"/> which is the (point-wise) subtraction result of the given <paramref name="left"/> and <paramref name="right"/> vectors.
		/// </summary>
		/// <param name="left">One original vector as the left operand</param>
		/// <param name="right">One original vector as the right operand</param>
		/// <returns>A new <typeparamref name="TVec3"/> which is the subtraction result of the given <paramref name="left"/> and <paramref name="right"/> vectors</returns>
		public abstract static TVec3 operator -(TVec1 left, TVec2 right);

		/// <summary>
		/// Create a new <typeparamref name="TVec3"/> which is the negation result of the given <paramref name="vector"/>
		/// </summary>
		/// <param name="vector">The original vector to negate</param>
		/// <returns>A new <typeparamref name="TVec3"/> which is the negation result of the given <paramref name="vector"/></returns>
		/// <exception cref="InvalidOperationException">If <typeparamref name="T"/> is an unsigned type</exception>
		public abstract static TVec3 operator -(TVec1 vector);

		/// <summary>
		/// When implemented by a derived class, statically create a new <typeparamref name="TVec3"/> which is the multiplication result of the given <paramref name="vector"/> and <paramref name="scalar"/>
		/// </summary>
		/// <param name="vector">The original vector to multiply</param>
		/// <param name="scalar">The scalar of type <typeparamref name="T"/> to multiply</param>
		/// <returns>A new <typeparamref name="TVec3"/> which is the multiplication result of the given <paramref name="vector"/> and <paramref name="scalar"/></returns>
		public abstract static TVec3 operator *(TVec1 vector, T scalar);

		/// <summary>
		/// When implemented by a derived class, statically create a new <typeparamref name="TVec3"/> which is the multiplication result of the given <paramref name="vector"/> and <paramref name="scalar"/>
		/// </summary>
		/// <param name="vector">The original vector to multiply</param>
		/// <param name="scalar">The scalar of type <typeparamref name="T"/> to multiply</param>
		/// <returns>A new <typeparamref name="TVec3"/> which is the multiplication result of the given <paramref name="vector"/> and <paramref name="scalar"/></returns>
		public abstract static TVec3 operator *(T scalar, TVec1 vector);

		/// <summary>
		/// When implemented by a derived class, statically create a new <typeparamref name="TVec3"/> which is the division result of the given <paramref name="vector"/> and <paramref name="scalar"/>
		/// </summary>
		/// <param name="vector">The original vector to be divided</param>
		/// <param name="scalar">The scalar of type <typeparamref name="T"/> to divide</param>
		/// <returns>A new <typeparamref name="TVec3"/> which is the multiplication result of the given <paramref name="vector"/> and <paramref name="scalar"/></returns>
		public abstract static TVec3 operator /(TVec1 vector, T scalar);
	}

	/// <summary>
	/// The interface for operations that multiply vectors and matrices of different types.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TVec1">The input vector type</typeparam>
	/// <typeparam name="TVec2">The output vector type</typeparam>
	/// <typeparam name="TMat">The input matrix type</typeparam>
	public interface IMatrixVectorMultiplyOperations<T, in TVec1, TVec2, in TMat>
		where T : unmanaged, INumber<T>
		where TVec1 : class, IBaseVector<T, TVec1>
		where TVec2 : class, IBaseVector<T, TVec2>
		where TMat : class, IBaseMatrix<T, TMat>
	{
		/// <summary>
		/// When implemented by a derived class, compute the addition of the multiplication result of the given <paramref name="matrix"/> and <paramref name="vector"/> (scaled by <paramref name="α"/>) with <paramref name="vectorOut"/> (scaled by <paramref name="β"/>).
		/// </summary>
		/// <param name="matrix">The input matrix to be multiplied</param>
		/// <param name="vector">The input vector to be multiplied</param>
		/// <param name="vectorOut">The output vector to be added in-place</param>
		/// <param name="α">The scalar to be multiplied to the <paramref name="matrix"/> of type <typeparamref name="T"/></param>
		/// <param name="β">The scalar to be multiplied to <paramref name="vectorOut"/> of type <typeparamref name="T"/></param>
		/// <param name="operation">The simple operation to be applied to <paramref name="matrix"/> before computation as a <see cref="MatrixOperation"/></param>
		/// <returns>The <paramref name="vectorOut"/> after operation.</returns>
		public abstract static TVec2 MatrixMultiplyVector(TMat matrix, TVec1 vector, TVec2 vectorOut, T α, T β = default, MatrixOperation operation = MatrixOperation.None);

		/// <summary>
		/// When implemented by a derived class, compute the addition of the multiplication result of the given <paramref name="vector"/> and <paramref name="matrix"/> (scaled by <paramref name="α"/>) with <paramref name="vectorOut"/> (scaled by <paramref name="β"/>).
		/// </summary>
		/// <param name="matrix">The input matrix to be multiplied</param>
		/// <param name="vector">The input vector to be multiplied</param>
		/// <param name="vectorOut">The output vector to be added in-place</param>
		/// <param name="α">The scalar to be multiplied to the <paramref name="matrix"/> of type <typeparamref name="T"/></param>
		/// <param name="β">The scalar to be multiplied to <paramref name="vectorOut"/> of type <typeparamref name="T"/></param>
		/// <param name="operation">The simple operation to be applied to <paramref name="matrix"/> before computation as a <see cref="MatrixOperation"/></param>
		/// <returns>The <paramref name="vectorOut"/> after operation.</returns>
		public abstract static TVec2 VectorMultiplyMatrix(TVec1 vector, TMat matrix, TVec2 vectorOut, T α, T β = default, MatrixOperation operation = MatrixOperation.None);
	}

	/// <summary>
	/// The interface for operations that get or set diagonal elements of matrices.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TVec">The vector type</typeparam>
	/// <typeparam name="TMat">The matrix type</typeparam>
	public interface IMatrixDiagonalVector<T, TVec, TMat>
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

		/// <summary>
		/// When implemented by a derived class, get the <paramref name="k"/>-th diagonal elements of <paramref name="matrix"/> as a <typeparamref name="TVec"/> and write the result to <paramref name="overwrite"/>.
		/// </summary>>
		/// <param name="matrix">The matrix to obtain diagonal from</param>
		/// <param name="k">The diagonal index: 0 for diagonal, 1 for super-diagonal at one above, -1 for sub-diagonal at one below, etc.</param>
		/// <param name="overwrite">The output <typeparamref name="TVec"/> which will contain the <paramref name="k"/>-th diagonal elements at exit</param>
		/// <returns>The <paramref name="overwrite"/> containing the <paramref name="k"/>-th diagonal elements.</returns>
		/// <exception cref="InvalidOperationException">If this matrix is not a square matrix</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="k"/> is out of range</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="overwrite"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="overwrite"/> cannot be overwritten</exception>
		public abstract static TVec GetDiag(TMat matrix, long k, TVec overwrite);

		/// <summary>
		/// When implemented by a derived class, set the <paramref name="k"/>-th diagonal elements  of <paramref name="matrix"/> to <paramref name="value"/>.
		/// </summary>
		/// <param name="matrix">The matrix to set diagonal to</param>
		/// <param name="k">The diagonal index: 0 for diagonal, 1 for super-diagonal at one above, -1 for sub-diagonal at one below, etc.</param>
		/// <param name="value">The <paramref name="k"/>-th diagonal elements to set as a <typeparamref name="TVec"/></param>
		/// <exception cref="InvalidOperationException">If this matrix is not a square matrix</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="k"/> is out of range</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="value"/> is null or invalid</exception>
		public abstract void SetDiag(TMat matrix, long k, TVec value);
	}
}
