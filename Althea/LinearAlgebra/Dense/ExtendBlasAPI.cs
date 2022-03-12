using System;

using Althea.Storage;

using Althea.SourceGenerator;


namespace Althea.LinearAlgebra.Dense
{
	/// <summary>
	/// The abstract class for dense linear algebra extend BLAS API routines 
	/// </summary>
	[AbstractRuntimeApi]
	public abstract partial class ExtendBlasAbstractApi : AbstractRuntimeApi<ExtendBlasAbstractApi>
	{
		#region BLAS like
		/// <summary>
		/// When implemented by a derived class, perform the matrix-matrix addition and/or transposition:<br/>
		/// <c><paramref name="C"/> = <paramref name="α"/> * <paramref name="opA"/>(<paramref name="A"/>) + <paramref name="β"/> * <paramref name="opB"/>(<paramref name="B"/>)</c>. <br/>
		/// Where <paramref name="α"/> and <paramref name="β"/> are scalars; and <paramref name="A"/>, <paramref name="B"/>, <paramref name="C"/> are matrices stored in column-major format with dimensions <paramref name="opA"/>(<paramref name="A"/>) → <paramref name="m"/>×<paramref name="n"/>, <paramref name="opB"/>(<paramref name="B"/>) → <paramref name="m"/>×<paramref name="n"/> and <paramref name="C"/> → <paramref name="m"/>×<paramref name="n"/>, respectively.
		/// </summary>
		/// <remarks>
		/// The out-of-place addition mode shall be enabled if <paramref name="C"/> is not <paramref name="A"/> nor <paramref name="B"/>. Both <paramref name="opA"/> and <paramref name="opB"/> can have any predefined value.<br/>
		/// The in-place mode shall be enabled if one of the following two operations is identified: <c><paramref name="C"/> = <paramref name="α"/> <paramref name="C"/> + <paramref name="β"/> <paramref name="opB"/>(<paramref name="B"/>)</c> or <c><paramref name="C"/> = <paramref name="α"/> <paramref name="opA"/>(<paramref name="A"/>) + <paramref name="β"/> <paramref name="C"/></c>.<br/>
		/// The out-of-place transposition mode shall be enabled if one of <paramref name="A"/> and <paramref name="B"/> is null or invalid or one of <paramref name="α"/> and <paramref name="β"/> is 0.
		/// </remarks>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS1">The first actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The second actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS3">The third actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
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
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentException">If the parameters do not fit any mode</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> and <paramref name="B"/> or <paramref name="C"/> is null or invalid</exception>
		[AbstractApiMethod]
		public abstract bool GeneralMatricesAdd<T, TS1, TS2, TS3>(MatrixOperation opA, MatrixOperation opB, long m, long n, T α, TS1? A, long lda, T β, TS2? B, long ldb, TS3 C, long ldc) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>;

		/// <summary>
		/// When implemented by a derived class, perform the matrix-matrix multiplication:
		/// <list type="table">
		/// <listheader><term>Condition</term>  <description>Equation</description></listheader>
		/// <item><term><paramref name="leftA"/> is true</term><description>  <paramref name="C"/> = <paramref name="α"/> * <paramref name="A"/> * diag(<paramref name="x"/>) + <paramref name="β"/> * <paramref name="C"/></description></item>
		/// <item><term><paramref name="leftA"/> is false</term><description>  <paramref name="C"/> = <paramref name="α"/> * diag(<paramref name="x"/>) * <paramref name="A"/> + <paramref name="β"/> * <paramref name="C"/></description></item>
		/// </list>
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS1">The first actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The second actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS3">The third actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="leftA">Whether to put <paramref name="A"/> in the left side or the right side</param>
		/// <param name="m">The number of rows of matrix <paramref name="A"/> and <paramref name="C"/></param>
		/// <param name="n">The number of columns of matrix <paramref name="A"/> and <paramref name="C"/></param>
		/// <param name="α">The scalar to multiply to <paramref name="A"/></param>
		/// <param name="A">The array of dimensions <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="m"/>)</c></param>
		/// <param name="lda">The leading dimension of two-dimensional array used to store the matrix <paramref name="A"/></param>
		/// <param name="x">The one-dimensional array of length at least <c>(1+(<paramref name="n"/>-1)*<paramref name="strideX"/>)</c> elements if <paramref name="leftA"/> is true or <c>(1+(<paramref name="m"/>-1)*<paramref name="strideX"/>)</c> otherwise</param>
		/// <param name="strideX">The stride of one-dimensional array <paramref name="x"/></param>
		/// <param name="β">The scalar to multiply to <paramref name="C"/></param>
		/// <param name="C">The array of dimensions <c><paramref name="ldc"/>×<paramref name="n"/></c> with <c><paramref name="ldc"/> ≥ max(1, <paramref name="m"/>)</c></param>
		/// <param name="ldc">The leading dimension of two-dimensional array used to store the matrix <paramref name="C"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> ≤ 0</exception>
		[AbstractApiMethod]
		public abstract bool DiagonalMatrixMultiplyGeneral<T, TS1, TS2, TS3>(bool leftA, long m, long n, T α, TS1 A, long lda, TS2 x, int strideX, T β, TS3 C, long ldc) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>;
		#endregion

		#region vector math
		/// <summary>
		/// When implemented by a derived class, check if all elements in <paramref name="x"/> and <paramref name="y"/> are equal: <c><paramref name="x"/>[i] == <paramref name="y"/>[j]</c> (point-wise equals).
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS1">The first actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The second actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="x">The vector to be checked</param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="y">The other vector to be checked</param>
		/// <param name="strideY">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="equals">Output <see cref="bool"/> indicating whether all elements in <paramref name="x"/> and <paramref name="y"/> are equal</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> or <paramref name="strideY"/> is ≤ 0</exception>
		[AbstractApiMethod]
		public abstract bool PointWiseEquals<T, TS1, TS2>(TS1 x, int strideX, TS2 y, int strideY, out bool equals) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

		/// <summary>
		/// When implemented by a derived class, compute <c><paramref name="x"/> = <paramref name="x"/>.*<paramref name="y"/></c> (point-wise multiplication).
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS1">The first actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The second actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="x">The vector to be multiplied in-place</param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="y">The vector to multiply</param>
		/// <param name="strideY">The stride between consecutive elements of <paramref name="x"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> or <paramref name="strideY"/> is ≤ 0</exception>
		[AbstractApiMethod]
		public abstract bool PointWiseMultiply<T, TS1, TS2>(TS1 x, int strideX, TS2 y, int strideY) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

		/// <summary>
		/// When implemented by a derived class, compute <c><paramref name="x"/> = <paramref name="x"/>./<paramref name="y"/></c> (point-wise division).
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS1">The first actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The second actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="x">The vector to be multiplied in-place</param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="y">The vector to multiply</param>
		/// <param name="strideY">The stride between consecutive elements of <paramref name="x"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> or <paramref name="strideY"/> is ≤ 0</exception>
		[AbstractApiMethod]
		public abstract bool PointWiseDivide<T, TS1, TS2>(TS1 x, int strideX, TS2 y, int strideY) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

		/// <summary>
		/// When implemented by a derived class, compute <c><paramref name="x"/> = <paramref name="x"/>.^<paramref name="p"/></c> (point-wise power).
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS">The actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="x">The vector to be powered in-place</param>
		/// <param name="stride">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="p">The exponent as a <see cref="double"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="stride"/> is ≤ 0</exception>
		[AbstractApiMethod]
		public abstract bool PointWisePower<T, TS>(TS x, int stride, double p) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <summary>
		/// When implemented by a derived class, compute <c><paramref name="x"/> = <paramref name="x"/>.^<paramref name="p"/></c> (point-wise power).
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS">The actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="x">The vector to be powered in-place</param>
		/// <param name="stride">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="p">The exponent as a <typeparamref name="T"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="stride"/> is ≤ 0</exception>
		[AbstractApiMethod]
		public abstract bool PointWisePower<T, TS>(TS x, int stride, T p) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <summary>
		/// When implemented by a derived class, compute <c><paramref name="x"/> = conj(<paramref name="x"/>)</c> (point-wise conjugate).
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS">The actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="x">The vector to be conjugated</param>
		/// <param name="stride">The stride between consecutive elements of <paramref name="x"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="stride"/> is ≤ 0</exception>
		[AbstractApiMethod]
		public abstract bool PointWiseConjugate<T, TS>(TS x, int stride) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <summary>
		/// When implemented by a derived class, cast the given vector from type <typeparamref name="TIn"/> to type <typeparamref name="TOut"/>.
		/// </summary>
		/// <typeparam name="TIn">Any unmanaged number as the input data type</typeparam>
		/// <typeparam name="TOut">Any unmanaged number as the output data type</typeparam>
		/// <typeparam name="TSIn">The input actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TSOut">The output actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="source">The source vector</param>
		/// <param name="incSrc">The stride between consecutive elements of <paramref name="source"/></param>
		/// <param name="destination">The destination vector</param>
		/// <param name="incDst">The stride between consecutive elements of <paramref name="destination"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="destination"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="incSrc"/> or <paramref name="incDst"/> is ≤ 0</exception>
		[AbstractApiMethod]
		public abstract bool PointWiseCast<TIn, TOut, TSIn, TSOut>(TSIn source, int incSrc, TSOut destination, int incDst) where TIn : unmanaged, INumber<TIn> where TOut : unmanaged, INumber<TOut> where TSIn : class, IStorage<TIn, TSIn> where TSOut : class, IStorage<TOut, TSOut>;

		/// <summary>
		/// When implemented by a derived class, truncate the vector by comparing each element's absolute value in <paramref name="x"/> to the given <paramref name="threshold"/>, if it is smaller than <paramref name="threshold"/>, it will be set to 0.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS">The actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="x">The vector to be truncated</param>
		/// <param name="stride">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="threshold">If any element's absolute value is smaller than <paramref name="threshold"/>, it will be set to 0</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		[AbstractApiMethod]
		public abstract bool TruncateArray<T, TS>(TS x, int stride, double threshold) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <summary>
		/// When implemented by a derived class, aggregately sum the elements in vector <paramref name="x"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS">The actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="x">The vector to be summed</param>
		/// <param name="stride">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="sum">Output the sum as a <typeparamref name="T"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="stride"/> is ≤ 0</exception>
		[AbstractApiMethod]
		public abstract bool AggregateSum<T, TS>(TS x, int stride, out T sum) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <summary>
		/// When implemented by a derived class, aggregately product the elements in vector <paramref name="x"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS">The actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="x">The vector to be multiplied</param>
		/// <param name="stride">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="product">Output the product as a <typeparamref name="T"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="stride"/> is ≤ 0</exception>
		[AbstractApiMethod]
		public abstract bool AggregateProduct<T, TS>(TS x, int stride, out T product) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <summary>
		/// When implemented by a derived class, perform partial aggregate sum of the elements in vector <paramref name="x"/> and write the result to <paramref name="y"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS1">The first actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The second actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="x">The vector to be partially summed</param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="y">The vector to store the partial sum result</param>
		/// <param name="strideY">The stride between consecutive elements of <paramref name="y"/></param>
		/// <param name="inclusive">Whether to sum <paramref name="x"/> inclusively (the first element is the first element of <paramref name="x"/>) if  or exclusively (the first element is 0)</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> or <paramref name="strideY"/> is ≤ 0</exception>
		[AbstractApiMethod]
		public abstract bool PartialSum<T, TS1, TS2>(TS1 x, int strideX, TS2 y, int strideY, bool inclusive) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

		/// <summary>
		/// When implemented by a derived class, perform partial aggregate product of the elements in vector <paramref name="x"/> and write the result to <paramref name="y"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS1">The first actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The second actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="x">The vector to be partially multiplied</param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="y">The vector to store the partial product result</param>
		/// <param name="strideY">The stride between consecutive elements of <paramref name="y"/></param>
		/// <param name="inclusive">Whether to sum <paramref name="x"/> inclusively (the first element is the first element of <paramref name="x"/>) if  or exclusively (the first element is 0)</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> or <paramref name="strideY"/> is ≤ 0</exception>
		[AbstractApiMethod]
		public abstract bool PartialProduct<T, TS1, TS2>(TS1 x, int strideX, TS2 y, int strideY, bool inclusive) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>;

		/// <summary>
		/// When implemented by a derived class, compute <c><paramref name="x"/> = <paramref name="x"/> + <paramref name="scalr"/></c> (point-wise addition).
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS">The actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="x">The vector to be added in-place</param>
		/// <param name="stride">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="scalr">The scalar to add</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="stride"/> is ≤ 0</exception>
		[AbstractApiMethod]
		public abstract bool PointWiseAddScalar<T, TS>(TS x, int stride, T scalr) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;
		#endregion

		#region matrix extended
		/// <summary>
		/// When implemented by a derived class, copy the matrix <paramref name="A"/>'s upper or lower part to the other part and set the diagonal elements to its absolute value is <typeparamref name="T"/> is a complex type.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS">The actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="storedUpper">Whether the upper triangular part of <paramref name="A"/> is stored or its lower part</param>
		/// <param name="hermitian">Whether to use hermitian conjugate copies or simple copies</param>
		/// <param name="n">The number of rows and columns of <paramref name="A"/></param>
		/// <param name="A">The matrix with size <paramref name="n"/>×<paramref name="n"/></param>
		/// <param name="lda">The leading dimension of <paramref name="A"/>, must be at least <paramref name="n"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> is null or invalid</exception>
		[AbstractApiMethod]
		public abstract bool MatrixCopyUpperLowerParts<T, TS>(bool storedUpper, bool hermitian, long n, TS A, long lda) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <summary>
		/// When implemented by a derived class, clear the matrix <paramref name="A"/>'s upper or lower part (not including the diagonal elements) to 0.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS">The actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="clearLower">Whether the lower triangular part of <paramref name="A"/> shall be cleared or its upper part</param>
		/// <param name="n">The number of rows and columns of <paramref name="A"/></param>
		/// <param name="A">The matrix with size <paramref name="n"/>×<paramref name="n"/></param>
		/// <param name="lda">The leading dimension of <paramref name="A"/>, must be at least <paramref name="n"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> is null or invalid</exception>
		[AbstractApiMethod]
		public abstract bool MatrixClearUpperLowerPart<T, TS>(bool clearLower, long n, TS A, long lda) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>;

		/// <summary>
		/// When implemented by a derived class, calculate matrix Kronecker product: <c><paramref name="C"/> = <paramref name="α"/> * <paramref name="A"/> ⊗ <paramref name="B"/> + <paramref name="β"/> * <paramref name="C"/></c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="TS1">The first actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The second actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS3">The third actual storage type that implements <see cref="IStorage{T, TSelf}"/></typeparam>
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
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="B"/> or <paramref name="C"/> is null or invalid</exception>
		[AbstractApiMethod]
		public abstract bool MatrixKronecker<T, TS1, TS2, TS3>(long ma, long na, long mb, long nb, T α, TS1 A, long lda, TS2 B, long ldb, T β, TS3 C, long ldc) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2> where TS3 : class, IStorage<T, TS3>;
		#endregion
	}
}
