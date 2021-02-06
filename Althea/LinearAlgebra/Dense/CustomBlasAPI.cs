using System;


namespace Althea.LinearAlgebra.Dense
{
	/// <summary>
	/// The abstract class for runtime linear algebra API routines 
	/// </summary>
	public abstract partial class AbstractApi : AbstractRuntimeApi
	{
		#region BLAS like
		/// <summary>
		/// When implemented by a derived class, perform the matrix-matrix addition and/or transposition:<br/>
		/// <c><paramref name="C"/> = <paramref name="α"/> * <paramref name="opA"/>(<paramref name="A"/>) + <paramref name="β"/> * <paramref name="opB"/>(<paramref name="B"/>)</c>. <br/>
		/// Where <paramref name="α"/> and <paramref name="β"/> are scalars; and <paramref name="A"/>, <paramref name="B"/>, <paramref name="C"/> are matrices stored in column-major format with dimensions <paramref name="opA"/>(<paramref name="A"/>) → <paramref name="m"/>×<paramref name="n"/>, <paramref name="opB"/>(<paramref name="B"/>) → <paramref name="m"/>×<paramref name="n"/> and <paramref name="C"/> → <paramref name="m"/>×<paramref name="n"/>, respectively.
		/// </summary>
		/// <remarks>
		/// The out-of-place addition mode shall be enabled if <paramref name="C"/> is not <paramref name="A"/> or <paramref name="B"/>. Both <paramref name="opA"/> and <paramref name="opB"/> can have any predefined value.<br/>
		/// The in-place mode shall be enabled if one of the following two operations is identified: <c><paramref name="C"/> = <paramref name="α"/> <paramref name="C"/> + <paramref name="β"/> <paramref name="opB"/>(<paramref name="B"/>)</c> or <c><paramref name="C"/> = <paramref name="α"/> <paramref name="opA"/>(<paramref name="A"/>) + <paramref name="β"/> <paramref name="C"/></c>.<br/>
		/// The out-of-place transposition mode shall be enabled if one of <paramref name="A"/> and <paramref name="B"/> is null or invalid or one of <paramref name="α"/> and <paramref name="β"/> is 0.
		/// </remarks>
		/// <exception cref="ArgumentException">If the parameters do not fit any mode</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> and <paramref name="B"/> or <paramref name="C"/> is null or invalid</exception>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="opA">The <see cref="MatrixOperation"/> to indicate the simple operation of <paramref name="A"/></param>
		/// <param name="opB">The <see cref="MatrixOperation"/> to indicate the simple operation of <paramref name="B"/></param>
		/// <param name="m">The number of rows of matrix <paramref name="opA"/>(<paramref name="A"/>) and <paramref name="C"/></param>
		/// <param name="n">The number of columns of matrix <paramref name="opB"/>(<paramref name="B"/>) and <paramref name="C"/></param>
		/// <param name="α">The scalar used for multiplication. If <c><paramref name="α"/> == 0</c>, <paramref name="A"/> does not have to be a valid input</param>
		/// <param name="A">The array of dimensions <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="m"/>)</c> if <paramref name="opA"/> == <see cref="MatrixOperation.None"/> and <c><paramref name="lda"/>×<paramref name="m"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="n"/>)</c> otherwise</param>
		/// <param name="lda">The leading dimension of two-dimensional array used to store the matrix <paramref name="A"/></param>
		/// <param name="β">The scalar used for multiplication. If <c><paramref name="β"/> == 0</c>, <paramref name="B"/> does not have to be a valid input</param>
		/// <param name="B">The array of dimensions <c><paramref name="ldb"/>×<paramref name="n"/></c> with <c><paramref name="ldb"/> ≥ max(1, <paramref name="m"/>)</c> if <paramref name="opA"/> == <see cref="MatrixOperation.None"/> and <c><paramref name="ldb"/>×<paramref name="m"/></c> with <c><paramref name="ldb"/> ≥ max(1, <paramref name="n"/>)</c> otherwise</param>
		/// <param name="ldb">The leading dimension of two-dimensional array used to store the matrix <paramref name="B"/></param>
		/// <param name="C">The array of dimensions <c><paramref name="ldc"/>×<paramref name="n"/></c> with <c><paramref name="ldc"/> ≥ max(1, <paramref name="m"/>)</c></param>
		/// <param name="ldc">The leading dimension of two-dimensional array used to store the matrix <paramref name="C"/></param>
		public abstract void GeneralMatricesAdd<T>(MatrixOperation opA, MatrixOperation opB, ulong m, ulong n, T α, Storage<T>? A, ulong lda, T β, Storage<T>? B, ulong ldb, Storage<T> C, ulong ldc) where T : unmanaged, IEquatable<T>;

		/// <summary>
		/// When implemented by a derived class, perform the matrix-matrix multiplication:<br/>
		/// If <paramref name="leftA"/> is true, <paramref name="C"/> = <paramref name="A"/> diag(<paramref name="x"/>), or <paramref name="C"/> = diag(<paramref name="x"/>) * <paramref name="A"/> otherwise.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="leftA">Whether to put <paramref name="A"/> in the left side or the right side</param>
		/// <param name="m">The number of rows of matrix <paramref name="A"/> and <paramref name="C"/></param>
		/// <param name="n">The number of columns of matrix <paramref name="A"/> and <paramref name="C"/></param>
		/// <param name="A">The array of dimensions <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="m"/>)</c></param>
		/// <param name="lda">The leading dimension of two-dimensional array used to store the matrix <paramref name="A"/></param>
		/// <param name="x">The one-dimensional array of length at least <c>(1+(<paramref name="n"/>-1)*<paramref name="incx"/>)</c> elements if <paramref name="leftA"/> is true or <c>(1+(<paramref name="m"/>-1)*<paramref name="incx"/>)</c> otherwise</param>
		/// <param name="incx">The stride of one-dimensional array <paramref name="x"/></param>
		/// <param name="C">The array of dimensions <c><paramref name="ldc"/>×<paramref name="n"/></c> with <c><paramref name="ldc"/> ≥ max(1, <paramref name="m"/>)</c></param>
		/// <param name="ldc">The leading dimension of two-dimensional array used to store the matrix <paramref name="C"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="incx"/> ≤ 0</exception>
		public abstract void DiagonalMatrixMultiplyGeneral<T>(bool leftA, ulong m, ulong n, Storage<T> A, ulong lda, Storage<T> x, int incx, Storage<T> C, ulong ldc) where T : unmanaged, IEquatable<T>;
		#endregion

		#region custom BLAS level 1
		/// <summary>
		/// When implemented by a derived class, compute <c><paramref name="x"/> = <paramref name="x"/>.*<paramref name="y"/></c> (point-wise multiplication).
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The vector to be multiplied in-place</param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="y">The vector to multiply</param>
		/// <param name="strideY">The stride between consecutive elements of <paramref name="x"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> or <paramref name="strideY"/> is less than 1</exception>
		public abstract void PointWiseMultiply<T>(Storage<T> x, int strideX, Storage<T> y, int strideY) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, compute <c><paramref name="x"/> = <paramref name="x"/>./<paramref name="y"/></c> (point-wise division).
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The vector to be multiplied in-place</param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="y">The vector to multiply</param>
		/// <param name="strideY">The stride between consecutive elements of <paramref name="x"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> or <paramref name="strideY"/> is less than 1</exception>
		public abstract void PointWiseDivide<T>(Storage<T> x, int strideX, Storage<T> y, int strideY) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, compute <c><paramref name="x"/> = <paramref name="x"/>.^<paramref name="p"/></c> (point-wise power).
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The vector to be powered in-place</param>
		/// <param name="stride">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="p">The exponent as a <see cref="int"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="stride"/> is less than 1</exception>
		public abstract void PointWisePower<T>(Storage<T> x, int stride, int p) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, compute <c><paramref name="x"/> = <paramref name="x"/>.^<paramref name="p"/></c> (point-wise power).
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The vector to be powered in-place</param>
		/// <param name="stride">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="p">The exponent as a <typeparamref name="T"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="stride"/> is less than 1</exception>
		public abstract void PointWisePower<T>(Storage<T> x, int stride, T p) where T : unmanaged, IEquatable<T>;

		/// <summary>
		/// When implemented by a derived class, compute <c><paramref name="x"/> = conj(<paramref name="x"/>)</c> (point-wise conjugate).
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The vector to be conjugated</param>
		/// <param name="stride">The stride between consecutive elements of <paramref name="x"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="stride"/> is less than 1</exception>
		public abstract void PointWiseConjugate<T>(Storage<T> x, int stride) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, cast the given vector from type <typeparamref name="T"/> to type <typeparamref name="TOut"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the input data type</typeparam>
		/// <typeparam name="TOut">Any unmanaged struct as the output data type</typeparam>
		/// <param name="source">The source vector</param>
		/// <param name="incSrc">The stride between consecutive elements of <paramref name="source"/></param>
		/// <param name="destination">The destination vector</param>
		/// <param name="incDst">The stride between consecutive elements of <paramref name="destination"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="destination"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="incSrc"/> or <paramref name="incDst"/> is less than 1</exception>
		public abstract void PointWiseCast<T, TOut>(Storage<T> source, int incSrc, Storage<TOut> destination, int incDst) where T : unmanaged where TOut : unmanaged;

		/// <summary>
		/// When implemented by a derived class, set the <paramref name="x"/>'s values at certain <paramref name="positions"/> to the give <paramref name="value"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The vector whose values will be set</param>
		/// <param name="positions">The given positions as a <see cref="ulong"/> array</param>
		/// <param name="value">The value to set</param>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="positions"/> is null or invalid</exception>
		public abstract void SetArrayWithValue<T>(Storage<T> x, T value, Storage<ulong> positions) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, truncate the vector by comparing each element's absolute value in <paramref name="x"/> to the given <paramref name="threshold"/>, if it is smaller than <paramref name="threshold"/>, it will be set to 0.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The vector to be truncated</param>
		/// <param name="threshold">If any element's absolute value is smaller than <paramref name="threshold"/>, it will be set to 0</param>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		public abstract void TruncateArray<T>(Storage<T> x, float threshold) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, directly sum the elements in vector <paramref name="x"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The vector to be summed</param>
		/// <param name="stride">The stride between consecutive elements of <paramref name="x"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="stride"/> is less than 1</exception>
		/// <returns>The sum as a <typeparamref name="T"/></returns>
		public abstract T Sum<T>(Storage<T> x, int stride) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, directly product the elements in vector <paramref name="x"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The vector to be multiplied</param>
		/// <param name="stride">The stride between consecutive elements of <paramref name="x"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		/// <returns>The sum as a <typeparamref name="T"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="stride"/> is less than 1</exception>
		public abstract T Product<T>(Storage<T> x, int stride) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, preform partial sum of the elements in vector <paramref name="x"/> and write the result to <paramref name="y"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The vector to be partially summed</param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="y">The vector to store the partial sum result</param>
		/// <param name="strideY">The stride between consecutive elements of <paramref name="y"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> or <paramref name="strideY"/> is less than 1</exception>
		public abstract void PartialSum<T>(Storage<T> x, int strideX, Storage<T> y, int strideY) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, preform partial product of the elements in vector <paramref name="x"/> and write the result to <paramref name="y"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The vector to be partially multiplied</param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="y">The vector to store the partial product result</param>
		/// <param name="strideY">The stride between consecutive elements of <paramref name="y"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> or <paramref name="strideY"/> is less than 1</exception>
		public abstract void PartialProduct<T>(Storage<T> x, int strideX, Storage<T> y, int strideY) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, compute <c><paramref name="x"/> = <paramref name="x"/> + <paramref name="α"/></c> (point-wise addition).
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The vector to be added in-place</param>
		/// <param name="stride">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="α">The scalar to add</param>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="stride"/> is less than 1</exception>
		public abstract void PointWiseAddScalar<T>(Storage<T> x, int stride, T α) where T : unmanaged, IEquatable<T>;
		#endregion

		#region custom BLAS level 3
		/// <summary>
		/// When implemented by a derived class, copy the matrix <paramref name="A"/>'s upper part to lower part and set the diagonal elements to its absolute value is <typeparamref name="T"/> is a complex type.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="A">The matrix with size <paramref name="n"/>×<paramref name="n"/></param>
		/// <param name="ld">The leading dimension of <paramref name="A"/>, must be at least <paramref name="n"/></param>
		/// <param name="n">The number of rows and columns of <paramref name="A"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> is null or invalid</exception>
		public abstract void MatrixCopyUpperToLowerPart<T>(Storage<T> A, ulong ld, ulong n) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, calculate matrix Kronecker product <c><paramref name="C"/> = <paramref name="α"/> * <paramref name="A"/> ⊗ <paramref name="B"/> + <paramref name="β"/> * <paramref name="C"/></c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="ma">The number of rows of <paramref name="A"/></param>
		/// <param name="na">The number of columns of <paramref name="A"/></param>
		/// <param name="mb">The number of rows of <paramref name="B"/></param>
		/// <param name="nb">The number of columns of <paramref name="B"/></param>
		/// <param name="α">The scalar to be multiplied to <paramref name="A"/></param>
		/// <param name="A">dense matrix with size <c><paramref name="lda"/>×<paramref name="na"/></c></param>
		/// <param name="lda">leading dimension of <paramref name="A"/>, must be at least <paramref name="ma"/></param>
		/// <param name="B">dense matrix with size <c><paramref name="ldb"/>×<paramref name="nb"/></c></param>
		/// <param name="ldb">leading dimension of <paramref name="B"/>, must be at least <paramref name="mb"/></param>
		/// <param name="β">The scalar to be multiplied to <paramref name="C"/></param>
		/// <param name="C">The pre-allocated destination matrix with size <c><paramref name="ldc"/> × <paramref name="na"/>*<paramref name="nb"/></c></param>
		/// <param name="ldc">The leading dimension of <paramref name="C"/>, must be at least <paramref name="na"/>*<paramref name="nb"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="B"/> or <paramref name="C"/> is null or invalid</exception>
		public abstract void MatrixKronecker<T>(ulong ma, ulong na, ulong mb, ulong nb, T α, Storage<T> A, ulong lda, Storage<T> B, ulong ldb, T β, Storage<T> C, ulong ldc) where T : unmanaged, IEquatable<T>;
		#endregion
	}
}
