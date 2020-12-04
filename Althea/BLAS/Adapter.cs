using System;

using Althea.Linq;
using Althea.Memory;


namespace Althea.Blas
{
	/// <summary>
	/// The BLAS routine interface
	/// </summary>
	public interface IBlas : IDisposable
	{
		#region level 1
		/// <summary>
		/// This function finds the (smallest) index of the element of the maximum magnitude.
		/// </summary>
		/// <param name="n">number of elements in the vector <paramref name="x"/></param>
		/// <param name="x">vector with elements</param>
		/// <param name="incx">stride between consecutive elements of <paramref name="x"/></param>
		/// <returns>the resulting index, which is 0 if <c><paramref name="n"/>,<paramref name="incx"/> &lt;= 0</c></returns>
		long AbsMax<T>(int n, Storage<T> x, int incx) where T : struct, IComparable<T>;

		/// <summary>
		/// This function finds the (smallest) index of the element of the maximum magnitude.
		/// </summary>
		/// <param name="n">number of elements in the vector <paramref name="x"/></param>
		/// <param name="x">vector with elements</param>
		/// <param name="incx">stride between consecutive elements of <paramref name="x"/></param>
		/// <returns>the resulting index, which is 0 if <c><paramref name="n"/>,<paramref name="incx"/> &lt;= 0</c></returns>
		public delegate long DelegateAbsMax<T>(int n, Storage<T> x, int incx) where T : struct, IComparable<T>;

		/// <summary>
		/// This function finds the (smallest) index of the element of the minimum magnitude.
		/// </summary>
		/// <param name="n">number of elements in the vector <paramref name="x"/></param>
		/// <param name="x">vector with elements</param>
		/// <param name="incx">stride between consecutive elements of <paramref name="x"/></param>
		/// <returns>the resulting index, which is 0 if <c><paramref name="n"/>,<paramref name="incx"/> &lt;= 0</c></returns>
		long AbsMin<T>(int n, Storage<T> x, int incx) where T : struct, IComparable<T>;

		/// <summary>
		/// This function finds the (smallest) index of the element of the minimum magnitude.
		/// </summary>
		/// <param name="n">number of elements in the vector <paramref name="x"/></param>
		/// <param name="x">vector with elements</param>
		/// <param name="incx">stride between consecutive elements of <paramref name="x"/></param>
		/// <returns>the resulting index, which is 0 if <c><paramref name="n"/>,<paramref name="incx"/> &lt;= 0</c></returns>
		public delegate long DelegateAbsMin<T>(int n, Storage<T> x, int incx) where T : struct, IComparable<T>;

		/// <summary>
		/// This function computes the sum of the absolute values of the elements of vector <paramref name="x"/>. For complex, this returns the sum of absolute values of real parts and imaginary parts.
		/// </summary>
		/// <param name="n">number of elements of the vector <paramref name="x"/></param>
		/// <param name="x">vector with length <paramref name="n"/>*<paramref name="incx"/></param>
		/// <param name="incx">stride between consecutive elements of <paramref name="x"/></param>
		/// <returns>the result value in <see cref="double"/></returns>
		double AbsSum<T>(int n, Storage<T> x, int incx) where T : struct, IComparable<T>;

		/// <summary>
		/// This function computes the sum of the absolute values of the elements of vector <paramref name="x"/>. For complex, this returns the sum of absolute values of real parts and imaginary parts.
		/// </summary>
		/// <param name="n">number of elements of the vector <paramref name="x"/></param>
		/// <param name="x">vector with length <paramref name="n"/>*<paramref name="incx"/></param>
		/// <param name="incx">stride between consecutive elements of <paramref name="x"/></param>
		/// <returns>the result value in <see cref="double"/></returns>
		public delegate double DelegateAbsSum<T>(int n, Storage<T> x, int incx) where T : struct, IComparable<T>;

		/// <summary>
		/// This function multiplies the vector <paramref name="x"/> by the scalar <paramref name="α"/> and adds it to the vector <paramref name="y"/> overwriting <paramref name="y"/>.
		/// </summary>
		/// <param name="n">number of elements in the vector <paramref name="x"/> and <paramref name="y"/></param>
		/// <param name="α">(host or device) scalar used for multiplication</param>
		/// <param name="x">vector with elements</param>
		/// <param name="incx">stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="y">(in and out) another vector with elements</param>
		/// <param name="incy">stride between consecutive elements of <paramref name="y"/></param>
		void VectorGeneralAdd<T>(int n, T α, Storage<T> x, int incx, Storage<T> y, int incy) where T : struct, IComparable<T>;

		/// <summary>
		/// This function multiplies the vector <paramref name="x"/> by the scalar <paramref name="α"/> and adds it to the vector <paramref name="y"/> overwriting <paramref name="y"/>.
		/// </summary>
		/// <param name="n">number of elements in the vector <paramref name="x"/> and <paramref name="y"/></param>
		/// <param name="α">(host or device) scalar used for multiplication</param>
		/// <param name="x">vector with elements</param>
		/// <param name="incx">stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="y">(in and out) another vector with elements</param>
		/// <param name="incy">stride between consecutive elements of <paramref name="y"/></param>
		public delegate void DelegateVectorGeneralAdd<T>(int n, T α, Storage<T> x, int incx, Storage<T> y, int incy) where T : struct, IComparable<T>;

		/// <summary>
		/// This function computes the dot product of vectors <paramref name="x"/> and <paramref name="y"/>.
		/// </summary>
		/// <param name="n">number of elements in the vector <paramref name="x"/> and <paramref name="y"/></param>
		/// <param name="x">vector with elements</param>
		/// <param name="incx">stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="y">(in and out) another vector with elements</param>
		/// <param name="incy">stride between consecutive elements of <paramref name="y"/></param>
		/// <param name="conjX">conjugate <paramref name="x"/> or not</param>
		/// <returns>the resulting dot product, which is 0.0 if <c>n &lt;= 0</c></returns>
		T Dot<T>(int n, Storage<T> x, int incx, Storage<T> y, int incy, bool conjX) where T : struct, IComparable<T>;

		/// <summary>
		/// This function computes the dot product of vectors <paramref name="x"/> and <paramref name="y"/>.
		/// </summary>
		/// <param name="n">number of elements in the vector <paramref name="x"/> and <paramref name="y"/></param>
		/// <param name="x">vector with elements</param>
		/// <param name="incx">stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="y">(in and out) another vector with elements</param>
		/// <param name="incy">stride between consecutive elements of <paramref name="y"/></param>
		/// <param name="conjX">conjugate <paramref name="x"/> or not</param>
		/// <returns>the resulting dot product, which is 0.0 if <c>n &lt;= 0</c></returns>
		public delegate T DelegateDot<T>(int n, Storage<T> x, int incx, Storage<T> y, int incy, bool conjX) where T : struct, IComparable<T>;

		/// <summary>
		/// This function computes the Euclidean norm of the vector <paramref name="x"/>.
		/// </summary>
		/// <param name="n">number of elements in the vector <paramref name="x"/></param>
		/// <param name="x">vector with elements</param>
		/// <param name="incx">stride between consecutive elements of <paramref name="x"/></param>
		/// <returns>the result, which is 0 if <c><paramref name="n"/>,<paramref name="incx"/> &lt;= 0</c></returns>
		double Norm<T>(int n, Storage<T> x, int incx) where T : struct, IComparable<T>;

		/// <summary>
		/// This function computes the Euclidean norm of the vector <paramref name="x"/>.
		/// </summary>
		/// <param name="n">number of elements in the vector <paramref name="x"/></param>
		/// <param name="x">vector with elements</param>
		/// <param name="incx">stride between consecutive elements of <paramref name="x"/></param>
		/// <returns>the result, which is 0 if <c><paramref name="n"/>,<paramref name="incx"/> &lt;= 0</c></returns>
		public delegate double DelegateNorm<T>(int n, Storage<T> x, int incx) where T : struct, IComparable<T>;

		/// <summary>
		/// This function scales the vector <paramref name="x"/> by the scalar <paramref name="α"/> and overwrites it with the result.
		/// </summary>
		/// <param name="n">number of elements in the vector <paramref name="x"/></param>
		/// <param name="α">scalar used for multiplication</param>
		/// <param name="x">vector with <paramref name="n"/> elements</param>
		/// <param name="incx">stride between consecutive elements of <paramref name="x"/></param>
		void Scale<T>(int n, T α, Storage<T> x, int incx) where T : struct, IComparable<T>;

		/// <summary>
		/// This function scales the vector <paramref name="x"/> by the scalar <paramref name="α"/> and overwrites it with the result.
		/// </summary>
		/// <param name="n">number of elements in the vector <paramref name="x"/></param>
		/// <param name="α">scalar used for multiplication</param>
		/// <param name="x">vector with <paramref name="n"/> elements</param>
		/// <param name="incx">stride between consecutive elements of <paramref name="x"/></param>
		public delegate void DelegateScale<T>(int n, T α, Storage<T> x, int incx) where T : struct, IComparable<T>;

		/// <summary>
		/// This function copies the vector <paramref name="x"/> into the vector <paramref name="y"/>. Hence, the performed operation is <c>y[j] = x[k] for i = 1,…,n; k = 1 + (i - 1)*incx and j = 1 + (i - 1)*incy</c>.
		/// </summary>
		/// <param name="n">number of elements in the vector <paramref name="x"/> and <paramref name="y"/></param>
		/// <param name="x">vector with <paramref name="n"/> elements</param>
		/// <param name="incx">stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="y">vector with <paramref name="n"/> elements</param>
		/// <param name="incy">stride between consecutive elements of <paramref name="y"/></param>
		void Copy<T>(int n, Storage<T> x, int incx, Storage<T> y, int incy) where T : struct, IComparable<T>;

		/// <summary>
		/// This function copies the vector <paramref name="x"/> into the vector <paramref name="y"/>. Hence, the performed operation is <c>y[j] = x[k] for i = 1,…,n; k = 1 + (i - 1)*incx and j = 1 + (i - 1)*incy</c>.
		/// </summary>
		/// <param name="n">number of elements in the vector <paramref name="x"/> and <paramref name="y"/></param>
		/// <param name="x">vector with <paramref name="n"/> elements</param>
		/// <param name="incx">stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="y">vector with <paramref name="n"/> elements</param>
		/// <param name="incy">stride between consecutive elements of <paramref name="y"/></param>
		public delegate void DelegateCopy<T>(int n, Storage<T> x, int incx, Storage<T> y, int incy) where T : struct, IComparable<T>;
		#endregion

		#region level 2
		/// <summary>
		/// This function performs the matrix-vector multiplication <paramref name="y"/> = <paramref name="α"/> *<paramref name="op"/>(<paramref name="A"/>)*<paramref name="x"/> + <paramref name="β"/>*<paramref name="y"/> where <paramref name="A"/> is a <paramref name="m"/>×<paramref name="n"/> matrix stored in column-major format, <paramref name="x"/> and <paramref name="y"/> are vectors, and <paramref name="α"/> and <paramref name="β"/> are scalars.<br/>
		/// Also, for matrix <paramref name="A"/>: <paramref name="op"/>(<paramref name="A"/>) = <paramref name="A"/> if <paramref name="op"/> == <see cref="MatrixOperation.None"/>; <paramref name="A"/><sup>T</sup> if <paramref name="op"/> == <see cref="MatrixOperation.Transpose"/>; <paramref name="A"/><sup>H</sup> if <paramref name="op"/> == <see cref="MatrixOperation.ConjugateTranspose"/>.
		/// </summary>
		/// <param name="op"><see cref="MatrixOperation"/> that is non- or (conj.) transpose</param>
		/// <param name="m">number of rows of matrix <paramref name="A"/></param>
		/// <param name="n">number of columns of matrix <paramref name="A"/></param>
		/// <param name="α">scalar used for multiplication</param>
		/// <param name="A">array of dimension <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="m"/>)</c>. Unchanged on exit.</param>
		/// <param name="lda">leading dimension of two-dimensional array used to store matrix <paramref name="A"/></param>
		/// <param name="x">vector at least <c>(1+(<paramref name="n"/>-1)*abs(<paramref name="incx"/>))</c> elements if <paramref name="op"/>==<see cref="MatrixOperation.None"/> or <c>(1+(<paramref name="m"/>-1)*abs(<paramref name="incx"/>))</c> otherwise</param>
		/// <param name="incx">stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="β">scalar used for multiplication, if <c>b<paramref name="β"/> == 0</c> then <paramref name="y"/> does not have to be a valid input</param>
		/// <param name="y">(in and out) vector at least <c>(1+(<paramref name="m"/>-1)*abs(<paramref name="incy"/>))</c> elements if <paramref name="op"/>==<see cref="MatrixOperation.None"/> or <c>(1+(<paramref name="n"/>-1)*abs(<paramref name="incy"/>))</c> otherwise</param>
		/// <param name="incy">stride between consecutive elements of <paramref name="y"/></param>
		void GeneralMatrixMultiplyVector<T>(MatrixOperation op, int m, int n, T α, Storage<T> A, int lda, Storage<T> x, int incx, T β, Storage<T> y, int incy) where T : struct, IComparable<T>;

		/// <summary>
		/// This function performs the matrix-vector multiplication <paramref name="y"/> = <paramref name="α"/> *<paramref name="op"/>(<paramref name="A"/>)*<paramref name="x"/> + <paramref name="β"/>*<paramref name="y"/> where <paramref name="A"/> is a <paramref name="m"/>×<paramref name="n"/> matrix stored in column-major format, <paramref name="x"/> and <paramref name="y"/> are vectors, and <paramref name="α"/> and <paramref name="β"/> are scalars.<br/>
		/// Also, for matrix <paramref name="A"/>: <paramref name="op"/>(<paramref name="A"/>) = <paramref name="A"/> if <paramref name="op"/> == <see cref="MatrixOperation.None"/>; <paramref name="A"/><sup>T</sup> if <paramref name="op"/> == <see cref="MatrixOperation.Transpose"/>; <paramref name="A"/><sup>H</sup> if <paramref name="op"/> == <see cref="MatrixOperation.ConjugateTranspose"/>.
		/// </summary>
		/// <param name="op"><see cref="MatrixOperation"/> that is non- or (conj.) transpose</param>
		/// <param name="m">number of rows of matrix <paramref name="A"/></param>
		/// <param name="n">number of columns of matrix <paramref name="A"/></param>
		/// <param name="α">scalar used for multiplication</param>
		/// <param name="A">array of dimension <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="m"/>)</c>. Unchanged on exit.</param>
		/// <param name="lda">leading dimension of two-dimensional array used to store matrix <paramref name="A"/></param>
		/// <param name="x">vector at least <c>(1+(<paramref name="n"/>-1)*abs(<paramref name="incx"/>))</c> elements if <paramref name="op"/>==<see cref="MatrixOperation.None"/> or <c>(1+(<paramref name="m"/>-1)*abs(<paramref name="incx"/>))</c> otherwise</param>
		/// <param name="incx">stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="β">scalar used for multiplication, if <c>b<paramref name="β"/> == 0</c> then <paramref name="y"/> does not have to be a valid input</param>
		/// <param name="y">(in and out) vector at least <c>(1+(<paramref name="m"/>-1)*abs(<paramref name="incy"/>))</c> elements if <paramref name="op"/>==<see cref="MatrixOperation.None"/> or <c>(1+(<paramref name="n"/>-1)*abs(<paramref name="incy"/>))</c> otherwise</param>
		/// <param name="incy">stride between consecutive elements of <paramref name="y"/></param>
		public delegate void DelegateGeneralMatrixMultiplyVector<T>(MatrixOperation op, int m, int n, T α, Storage<T> A, int lda, Storage<T> x, int incx, T β, Storage<T> y, int incy) where T : struct, IComparable<T>;

		/// <summary>
		/// This function performs the symmetric/Hermitian matrix-vector multiplication <c><paramref name="y"/> = <paramref name="α"/>*<paramref name="A"/>*<paramref name="x"/> + <paramref name="β"/>*<paramref name="y"/></c> where <paramref name="A"/> is a <paramref name="n"/>×<paramref name="n"/> symmetric/Hermitian matrix stored in column-major format, <paramref name="x"/> is a vector, and <paramref name="α"/> is a scalar.
		/// </summary>
		/// <param name="uplo">indicates if matrix lower or upper part is stored</param>
		/// <param name="n">number of rows and columns of matrix <paramref name="A"/></param>
		/// <param name="α">scalar used for multiplication</param>
		/// <param name="A">array of dimension <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="n"/>)</c></param>
		/// <param name="lda">leading dimension of two-dimensional array used to store matrix <paramref name="A"/></param>
		/// <param name="x">vector with <c>(1+(<paramref name="n"/>-1)*abs(<paramref name="incx"/>))</c> elements</param>
		/// <param name="incx">stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="β">scalar used for multiplication, if <c>b<paramref name="β"/> == 0</c> then <paramref name="y"/> does not have to be a valid input</param>
		/// <param name="y">(in and out) vector at least <c>(1+(<paramref name="n"/>-1)*abs(<paramref name="incy"/>))</c></param>
		/// <param name="incy">stride between consecutive elements of <paramref name="y"/></param>
		/// <param name="hermA">regard <paramref name="A"/> as a Hermitian or symmetric matrix</param>
		void SymmHermMatrixMultiplyVector<T>(MatrixFillMode uplo, int n, T α, Storage<T> A, int lda, Storage<T> x, int incx, T β, Storage<T> y, int incy, bool hermA) where T : struct, IComparable<T>;

		/// <summary>
		/// This function performs the symmetric/Hermitian matrix-vector multiplication <c><paramref name="y"/> = <paramref name="α"/>*<paramref name="A"/>*<paramref name="x"/> + <paramref name="β"/>*<paramref name="y"/></c> where <paramref name="A"/> is a <paramref name="n"/>×<paramref name="n"/> symmetric/Hermitian matrix stored in column-major format, <paramref name="x"/> is a vector, and <paramref name="α"/> is a scalar.
		/// </summary>
		/// <param name="uplo">indicates if matrix lower or upper part is stored</param>
		/// <param name="n">number of rows and columns of matrix <paramref name="A"/></param>
		/// <param name="α">scalar used for multiplication</param>
		/// <param name="A">array of dimension <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="n"/>)</c></param>
		/// <param name="lda">leading dimension of two-dimensional array used to store matrix <paramref name="A"/></param>
		/// <param name="x">vector with <c>(1+(<paramref name="n"/>-1)*abs(<paramref name="incx"/>))</c> elements</param>
		/// <param name="incx">stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="β">scalar used for multiplication, if <c>b<paramref name="β"/> == 0</c> then <paramref name="y"/> does not have to be a valid input</param>
		/// <param name="y">(in and out) vector at least <c>(1+(<paramref name="n"/>-1)*abs(<paramref name="incy"/>))</c></param>
		/// <param name="incy">stride between consecutive elements of <paramref name="y"/></param>
		/// <param name="hermA">regard <paramref name="A"/> as a Hermitian or symmetric matrix</param>
		public delegate void DelegateSymmHermMatrixMultiplyVector<T>(MatrixFillMode uplo, int n, T α, Storage<T> A, int lda, Storage<T> x, int incx, T β, Storage<T> y, int incy, bool hermA) where T : struct, IComparable<T>;

		/// <summary>
		/// This function performs the rank-1 update <c><paramref name="A"/> = <paramref name="α"/>*<paramref name="x"/>*<paramref name="y"/><sup>T</sup> + <paramref name="A"/></c> if <paramref name="conjY"/> is false; <c><paramref name="A"/> = <paramref name="α"/>*<paramref name="x"/>*<paramref name="y"/><sup>H</sup>* + <paramref name="A"/></c> otherwise; where <paramref name="A"/> is a <paramref name="m"/>×<paramref name="n"/> matrix stored in column-major format, <paramref name="x"/> and <paramref name="y"/> are vectors, and <paramref name="α"/> is a scalar.
		/// </summary>
		/// <param name="m">number of rows of matrix <paramref name="A"/></param>
		/// <param name="n">number of columns of matrix <paramref name="A"/></param>
		/// <param name="α">scalar used for multiplication</param>
		/// <param name="x">vector with <c>(1+(<paramref name="m"/>-1)*abs(<paramref name="incx"/>))</c> elements</param>
		/// <param name="incx">stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="y">vector with <c>(1+(<paramref name="n"/>-1)*abs(<paramref name="incy"/>))</c> elements</param>
		/// <param name="incy">stride between consecutive elements of <paramref name="y"/></param>
		/// <param name="A">array of dimension <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="m"/>)</c></param>
		/// <param name="lda">leading dimension of two-dimensional array used to store matrix <paramref name="A"/></param>
		/// <param name="conjY">conjugate <paramref name="y"/> or not</param>
		void GenralRankOneUpdate<T>(int m, int n, T α, Storage<T> x, int incx, Storage<T> y, int incy, Storage<T> A, int lda, bool conjY) where T : struct, IComparable<T>;

		/// <summary>
		/// This function performs the rank-1 update <c><paramref name="A"/> = <paramref name="α"/>*<paramref name="x"/>*<paramref name="y"/><sup>T</sup> + <paramref name="A"/></c> if <paramref name="conjY"/> is false; <c><paramref name="A"/> = <paramref name="α"/>*<paramref name="x"/>*<paramref name="y"/><sup>H</sup>* + <paramref name="A"/></c> otherwise; where <paramref name="A"/> is a <paramref name="m"/>×<paramref name="n"/> matrix stored in column-major format, <paramref name="x"/> and <paramref name="y"/> are vectors, and <paramref name="α"/> is a scalar.
		/// </summary>
		/// <param name="m">number of rows of matrix <paramref name="A"/></param>
		/// <param name="n">number of columns of matrix <paramref name="A"/></param>
		/// <param name="α">scalar used for multiplication</param>
		/// <param name="x">vector with <c>(1+(<paramref name="m"/>-1)*abs(<paramref name="incx"/>))</c> elements</param>
		/// <param name="incx">stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="y">vector with <c>(1+(<paramref name="n"/>-1)*abs(<paramref name="incy"/>))</c> elements</param>
		/// <param name="incy">stride between consecutive elements of <paramref name="y"/></param>
		/// <param name="A">array of dimension <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="m"/>)</c></param>
		/// <param name="lda">leading dimension of two-dimensional array used to store matrix <paramref name="A"/></param>
		/// <param name="conjY">conjugate <paramref name="y"/> or not</param>
		public delegate void DelegateGenralRankOneUpdate<T>(int m, int n, T α, Storage<T> x, int incx, Storage<T> y, int incy, Storage<T> A, int lda, bool conjY) where T : struct, IComparable<T>;

		/// <summary>
		/// This function performs the symmetric/Hermitian rank-1 update <c><paramref name="A"/> = <paramref name="α"/>*<paramref name="x"/>*<paramref name="x"/><sup>op</sup> + <paramref name="A"/></c>; where <paramref name="A"/> is a <paramref name="n"/>×<paramref name="n"/> symmetric/Hermitian matrix stored in column-major format, <paramref name="x"/> is a vector, <paramref name="α"/> is a scalar, and <c>op</c> is <c>T</c> if <paramref name="conjX"/> is <c>false</c> or <c>H</c> otherwise.
		/// </summary>
		/// <param name="uplo"><see cref="MatrixFillMode"/> of result matrix</param>
		/// <param name="n">number of rows and columns of matrix <paramref name="A"/></param>
		/// <param name="α">scalar used for multiplication</param>
		/// <param name="x">vector with <c>(1+(<paramref name="n"/>-1)*abs(<paramref name="incx"/>))</c> elements</param>
		/// <param name="incx">stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="A">array of dimension <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="n"/>)</c></param>
		/// <param name="lda">leading dimension of two-dimensional array used to store matrix <paramref name="A"/></param>
		/// <param name="conjX">conjugate <paramref name="x"/> or not</param>
		void SymmHermRankOneUpdate<T>(MatrixFillMode uplo, int n, T α, Storage<T> x, int incx, Storage<T> A, int lda, bool conjX) where T : struct, IComparable<T>;

		/// <summary>
		/// This function performs the symmetric/Hermitian rank-1 update <c><paramref name="A"/> = <paramref name="α"/>*<paramref name="x"/>*<paramref name="x"/><sup>op</sup> + <paramref name="A"/></c>; where <paramref name="A"/> is a <paramref name="n"/>×<paramref name="n"/> symmetric/Hermitian matrix stored in column-major format, <paramref name="x"/> is a vector, <paramref name="α"/> is a scalar, and <c>op</c> is <c>T</c> if <paramref name="conjX"/> is <c>false</c> or <c>H</c> otherwise.
		/// </summary>
		/// <param name="uplo"><see cref="MatrixFillMode"/> of result matrix</param>
		/// <param name="n">number of rows and columns of matrix <paramref name="A"/></param>
		/// <param name="α">scalar used for multiplication</param>
		/// <param name="x">vector with <c>(1+(<paramref name="n"/>-1)*abs(<paramref name="incx"/>))</c> elements</param>
		/// <param name="incx">stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="A">array of dimension <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="n"/>)</c></param>
		/// <param name="lda">leading dimension of two-dimensional array used to store matrix <paramref name="A"/></param>
		/// <param name="conjX">conjugate <paramref name="x"/> or not</param>
		public delegate void DelegateSymmHermRankOneUpdate<T>(MatrixFillMode uplo, int n, T α, Storage<T> x, int incx, Storage<T> A, int lda, bool conjX) where T : struct, IComparable<T>;
		#endregion

		#region level 3
		/// <summary>
		/// This function performs the matrix-matrix multiplication <paramref name="C"/> = <paramref name="α"/>*<paramref name="opA"/>(<paramref name="A"/>)*<paramref name="opB"/>(<paramref name="B"/>) + <paramref name="β"/>*<paramref name="C"/> where <paramref name="α"/> and <paramref name="β"/> are scalars; <paramref name="A"/> , <paramref name="B"/> and <paramref name="C"/> are matrices stored in column-major format with dimensions <paramref name="opA"/>(<paramref name="A"/>) -- <paramref name="m"/>×<paramref name="k"/>, <paramref name="opB"/>(<paramref name="B"/>) -- <paramref name="k"/>×<paramref name="n"/> and <paramref name="C"/> -- <paramref name="m"/>×<paramref name="n"/>, respectively. <br/>
		/// Also, for matrix <paramref name="A"/>,
		/// <paramref name="opA"/>(<paramref name="A"/>) = <paramref name="A"/> if <paramref name="opA"/> == <see cref="MatrixOperation.None"/>;
		/// <paramref name="opA"/>(<paramref name="A"/>) = <paramref name="A"/><sup>T</sup> if <paramref name="opA"/> == <see cref="MatrixOperation.Transpose"/>;
		/// <paramref name="opA"/>(<paramref name="A"/>) = <paramref name="A"/><sup>H</sup> if <paramref name="opA"/> == <see cref="MatrixOperation.ConjugateTranspose"/>.
		/// The same for <paramref name="B"/>.
		/// </summary>
		/// <param name="opA"><see cref="MatrixOperation"/> that is non- or (conj.) transpose</param>
		/// <param name="opB"><see cref="MatrixOperation"/> that is non- or (conj.) transpose</param>
		/// <param name="m">number of rows of matrix <paramref name="opA"/>(<paramref name="A"/>) and <paramref name="C"/></param>
		/// <param name="n">number of columns of matrix <paramref name="opB"/>(<paramref name="B"/>) and <paramref name="C"/></param>
		/// <param name="k">number of columns of <paramref name="opA"/>(<paramref name="A"/>) and rows of <paramref name="opB"/>(<paramref name="B"/>)</param>
		/// <param name="α">scalar used for multiplication</param>
		/// <param name="A">array of dimensions <c><paramref name="lda"/>×<paramref name="k"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="m"/>)</c> if <paramref name="opA"/> == <see cref="MatrixOperation.None"/>, and <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="k"/>)</c> otherwise</param>
		/// <param name="lda">leading dimension of two-dimensional array used to store the matrix <paramref name="A"/></param>
		/// <param name="B">array of dimension <c><paramref name="ldb"/>×<paramref name="n"/></c> with <c><paramref name="ldb"/> ≥ max(1, <paramref name="k"/>)</c> if <paramref name="opB"/> == <see cref="MatrixOperation.None"/>, and <c><paramref name="ldb"/>×<paramref name="k"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="n"/>)</c> otherwise</param>
		/// <param name="ldb">leading dimension of two-dimensional array used to store matrix <paramref name="B"/></param>
		/// <param name="β">scalar used for multiplication. If <c><paramref name="β"/> == 0</c>, <paramref name="C"/> does not have to be a valid input</param>
		/// <param name="C">array of dimensions <c><paramref name="ldc"/>×<paramref name="n"/></c> with <c><paramref name="ldc"/> ≥ max(1, <paramref name="m"/>)</c></param>
		/// <param name="ldc">leading dimension of a two-dimensional array used to store the matrix <paramref name="C"/></param>
		void GeneralMatricesMultiply<T>(MatrixOperation opA, MatrixOperation opB, int m, int n, int k, T α, Storage<T> A, int lda, Storage<T> B, int ldb, T β, Storage<T> C, int ldc) where T : struct, IComparable<T>;

		/// <summary>
		/// This function performs the matrix-matrix multiplication <paramref name="C"/> = <paramref name="α"/>*<paramref name="opA"/>(<paramref name="A"/>)*<paramref name="opB"/>(<paramref name="B"/>) + <paramref name="β"/>*<paramref name="C"/> where <paramref name="α"/> and <paramref name="β"/> are scalars; <paramref name="A"/> , <paramref name="B"/> and <paramref name="C"/> are matrices stored in column-major format with dimensions <paramref name="opA"/>(<paramref name="A"/>) -- <paramref name="m"/>×<paramref name="k"/>, <paramref name="opB"/>(<paramref name="B"/>) -- <paramref name="k"/>×<paramref name="n"/> and <paramref name="C"/> -- <paramref name="m"/>×<paramref name="n"/>, respectively. <br/>
		/// Also, for matrix <paramref name="A"/>,
		/// <paramref name="opA"/>(<paramref name="A"/>) = <paramref name="A"/> if <paramref name="opA"/> == <see cref="MatrixOperation.None"/>;
		/// <paramref name="opA"/>(<paramref name="A"/>) = <paramref name="A"/><sup>T</sup> if <paramref name="opA"/> == <see cref="MatrixOperation.Transpose"/>;
		/// <paramref name="opA"/>(<paramref name="A"/>) = <paramref name="A"/><sup>H</sup> if <paramref name="opA"/> == <see cref="MatrixOperation.ConjugateTranspose"/>.
		/// The same for <paramref name="B"/>.
		/// </summary>
		/// <param name="opA"><see cref="MatrixOperation"/> that is non- or (conj.) transpose</param>
		/// <param name="opB"><see cref="MatrixOperation"/> that is non- or (conj.) transpose</param>
		/// <param name="m">number of rows of matrix <paramref name="opA"/>(<paramref name="A"/>) and <paramref name="C"/></param>
		/// <param name="n">number of columns of matrix <paramref name="opB"/>(<paramref name="B"/>) and <paramref name="C"/></param>
		/// <param name="k">number of columns of <paramref name="opA"/>(<paramref name="A"/>) and rows of <paramref name="opB"/>(<paramref name="B"/>)</param>
		/// <param name="α">scalar used for multiplication</param>
		/// <param name="A">array of dimensions <c><paramref name="lda"/>×<paramref name="k"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="m"/>)</c> if <paramref name="opA"/> == <see cref="MatrixOperation.None"/>, and <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="k"/>)</c> otherwise</param>
		/// <param name="lda">leading dimension of two-dimensional array used to store the matrix <paramref name="A"/></param>
		/// <param name="B">array of dimension <c><paramref name="ldb"/>×<paramref name="n"/></c> with <c><paramref name="ldb"/> ≥ max(1, <paramref name="k"/>)</c> if <paramref name="opB"/> == <see cref="MatrixOperation.None"/>, and <c><paramref name="ldb"/>×<paramref name="k"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="n"/>)</c> otherwise</param>
		/// <param name="ldb">leading dimension of two-dimensional array used to store matrix <paramref name="B"/></param>
		/// <param name="β">scalar used for multiplication. If <c><paramref name="β"/> == 0</c>, <paramref name="C"/> does not have to be a valid input</param>
		/// <param name="C">array of dimensions <c><paramref name="ldc"/>×<paramref name="n"/></c> with <c><paramref name="ldc"/> ≥ max(1, <paramref name="m"/>)</c></param>
		/// <param name="ldc">leading dimension of a two-dimensional array used to store the matrix <paramref name="C"/></param>
		public delegate void DelegateGeneralMatricesMultiply<T>(MatrixOperation opA, MatrixOperation opB, int m, int n, int k, T α, Storage<T> A, int lda, Storage<T> B, int ldb, T β, Storage<T> C, int ldc) where T : struct, IComparable<T>;

		/// <summary>
		/// This function performs the symmetric/Hermitian matrix-matrix multiplication: if <paramref name="side"/> == <see cref="SideMode.Left"/>, <c><paramref name="C"/> = <paramref name="α"/>*<paramref name="A"/>*<paramref name="B"/> + <paramref name="β"/>*<paramref name="C"/></c>; otherwise <c><paramref name="C"/> = <paramref name="α"/>*<paramref name="B"/>*<paramref name="A"/> + <paramref name="β"/>*<paramref name="C"/></c>.<br/>
		/// Where <paramref name="A"/> is a symmetric matrix stored in lower or upper mode, <paramref name="B"/> and <paramref name="C"/> are <paramref name="m"/>×<paramref name="n"/> matrices, and <paramref name="α"/> and <paramref name="β"/> are scalars.
		/// </summary>
		/// <param name="side">indicates if matrix <paramref name="A"/> is on the left or right of <paramref name="B"/></param>
		/// <param name="uplo">indicates if matrix <paramref name="A"/> lower or upper part is stored</param>
		/// <param name="m">number of rows of matrix <paramref name="C"/> and <paramref name="B"/>, with matrix <paramref name="A"/> sized accordingly</param>
		/// <param name="n">number of columns of matrix <paramref name="C"/> and <paramref name="B"/>, with matrix <paramref name="A"/> sized accordingly</param>
		/// <param name="α">scalar used for multiplication</param>
		/// <param name="A">symmetric/Hermitian matrix of dimension <c><paramref name="lda"/>×<paramref name="m"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="m"/>)</c> if <paramref name="side"/> == <see cref="SideMode.Left"/>, and <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="n"/>)</c>otherwise</param>
		/// <param name="lda">leading dimension of two-dimensional array used to store matrix <paramref name="A"/></param>
		/// <param name="B">array of dimension <c><paramref name="ldb"/>×<paramref name="n"/></c> with <c><paramref name="ldb"/> ≥ max(1, <paramref name="m"/>)</c></param>
		/// <param name="ldb">leading dimension of two-dimensional array used to store matrix <paramref name="B"/></param>
		/// <param name="β">scalar used for multiplication, if <c><paramref name="β"/> == 0</c> then <paramref name="C"/> does not have to be a valid input</param>
		/// <param name="C">array of dimension <c><paramref name="ldc"/>×<paramref name="n"/></c> with <c><paramref name="ldc"/> ≥ max(1, <paramref name="m"/>)</c></param>
		/// <param name="ldc">leading dimension of two-dimensional array used to store matrix <paramref name="B"/></param>
		/// <param name="hermA">regard <paramref name="A"/> as a Hermitian or symmetric matrix</param>
		void SymmHermMatrixMultiplyGeneral<T>(SideMode side, MatrixFillMode uplo, int m, int n, T α, Storage<T> A, int lda, Storage<T> B, int ldb, T β, Storage<T> C, int ldc, bool hermA) where T : struct, IComparable<T>;

		/// <summary>
		/// This function performs the symmetric/Hermitian matrix-matrix multiplication: if <paramref name="side"/> == <see cref="SideMode.Left"/>, <c><paramref name="C"/> = <paramref name="α"/>*<paramref name="A"/>*<paramref name="B"/> + <paramref name="β"/>*<paramref name="C"/></c>; otherwise <c><paramref name="C"/> = <paramref name="α"/>*<paramref name="B"/>*<paramref name="A"/> + <paramref name="β"/>*<paramref name="C"/></c>.<br/>
		/// Where <paramref name="A"/> is a symmetric matrix stored in lower or upper mode, <paramref name="B"/> and <paramref name="C"/> are <paramref name="m"/>×<paramref name="n"/> matrices, and <paramref name="α"/> and <paramref name="β"/> are scalars.
		/// </summary>
		/// <param name="side">indicates if matrix <paramref name="A"/> is on the left or right of <paramref name="B"/></param>
		/// <param name="uplo">indicates if matrix <paramref name="A"/> lower or upper part is stored</param>
		/// <param name="m">number of rows of matrix <paramref name="C"/> and <paramref name="B"/>, with matrix <paramref name="A"/> sized accordingly</param>
		/// <param name="n">number of columns of matrix <paramref name="C"/> and <paramref name="B"/>, with matrix <paramref name="A"/> sized accordingly</param>
		/// <param name="α">scalar used for multiplication</param>
		/// <param name="A">symmetric/Hermitian matrix of dimension <c><paramref name="lda"/>×<paramref name="m"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="m"/>)</c> if <paramref name="side"/> == <see cref="SideMode.Left"/>, and <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="n"/>)</c>otherwise</param>
		/// <param name="lda">leading dimension of two-dimensional array used to store matrix <paramref name="A"/></param>
		/// <param name="B">array of dimension <c><paramref name="ldb"/>×<paramref name="n"/></c> with <c><paramref name="ldb"/> ≥ max(1, <paramref name="m"/>)</c></param>
		/// <param name="ldb">leading dimension of two-dimensional array used to store matrix <paramref name="B"/></param>
		/// <param name="β">scalar used for multiplication, if <c><paramref name="β"/> == 0</c> then <paramref name="C"/> does not have to be a valid input</param>
		/// <param name="C">array of dimension <c><paramref name="ldc"/>×<paramref name="n"/></c> with <c><paramref name="ldc"/> ≥ max(1, <paramref name="m"/>)</c></param>
		/// <param name="ldc">leading dimension of two-dimensional array used to store matrix <paramref name="B"/></param>
		/// <param name="hermA">regard <paramref name="A"/> as a Hermitian or symmetric matrix</param>
		public delegate void DelegateSymmHermMatrixMultiplyGeneral<T>(SideMode side, MatrixFillMode uplo, int m, int n, T α, Storage<T> A, int lda, Storage<T> B, int ldb, T β, Storage<T> C, int ldc, bool hermA) where T : struct, IComparable<T>;

		/// <summary>
		/// This function performs the symmetric rank-k update <c><paramref name="C"/> = <paramref name="α"/>*<paramref name="op"/>(<paramref name="A"/>)*<paramref name="op"/>(<paramref name="A"/>)<sup>pow</sup> + <paramref name="β"/>*<paramref name="C"/></c>; where <paramref name="α"/> and <paramref name="β"/> are scalars, <paramref name="C"/> is a symmetric matrix stored in lower or upper mode, <paramref name="A"/> is a matrix with dimensions <paramref name="op"/>(<paramref name="A"/>) <paramref name="n"/>×<paramref name="k"/>; and <c>pow</c> is <c>T</c> if <paramref name="conjA"/> is <c>false</c> or <c>H</c> otherwise.
		/// </summary>
		/// <param name="uplo">indicates if matrix <paramref name="C"/> lower or upper part is stored</param>
		/// <param name="op"><see cref="MatrixOperation"/> that is non- or transpose</param>
		/// <param name="n">number of rows of matrix <paramref name="op"/>(<paramref name="A"/>) and <paramref name="C"/></param>
		/// <param name="k">number of columns of matrix <paramref name="op"/>(<paramref name="A"/>)</param>
		/// <param name="α">scalar used for multiplication</param>
		/// <param name="A">array of dimension <c><paramref name="lda"/>×<paramref name="k"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="n"/>)</c> if trans == <see cref="MatrixOperation.None"/> and <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="k"/>)</c> otherwise</param>
		/// <param name="lda">leading dimension of two-dimensional array used to store matrix <paramref name="A"/></param>
		/// <param name="β">if <c><paramref name="β"/> == 0</c> then <paramref name="C"/> does not have to be a valid input</param>
		/// <param name="C">symmetric/Hermitian matrix of dimension <c><paramref name="ldc"/>×<paramref name="n"/></c> with <c><paramref name="ldc"/> ≥ max(1, <paramref name="n"/>)</c></param>
		/// <param name="ldc">leading dimension of two-dimensional array used to store matrix <paramref name="C"/></param>
		/// <param name="conjA">conjugate transpose <paramref name="A"/> or just transpose</param>
		void RankKUpdate<T>(MatrixFillMode uplo, MatrixOperation op, int n, int k, T α, Storage<T> A, int lda, T β, Storage<T> C, int ldc, bool conjA) where T : struct, IComparable<T>;

		/// <summary>
		/// This function performs the symmetric rank-k update <c><paramref name="C"/> = <paramref name="α"/>*<paramref name="op"/>(<paramref name="A"/>)*<paramref name="op"/>(<paramref name="A"/>)<sup>pow</sup> + <paramref name="β"/>*<paramref name="C"/></c>; where <paramref name="α"/> and <paramref name="β"/> are scalars, <paramref name="C"/> is a symmetric matrix stored in lower or upper mode, <paramref name="A"/> is a matrix with dimensions <paramref name="op"/>(<paramref name="A"/>) <paramref name="n"/>×<paramref name="k"/>; and <c>pow</c> is <c>T</c> if <paramref name="conjA"/> is <c>false</c> or <c>H</c> otherwise.
		/// </summary>
		/// <param name="uplo">indicates if matrix <paramref name="C"/> lower or upper part is stored</param>
		/// <param name="op"><see cref="MatrixOperation"/> that is non- or transpose</param>
		/// <param name="n">number of rows of matrix <paramref name="op"/>(<paramref name="A"/>) and <paramref name="C"/></param>
		/// <param name="k">number of columns of matrix <paramref name="op"/>(<paramref name="A"/>)</param>
		/// <param name="α">scalar used for multiplication</param>
		/// <param name="A">array of dimension <c><paramref name="lda"/>×<paramref name="k"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="n"/>)</c> if trans == <see cref="MatrixOperation.None"/> and <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="k"/>)</c> otherwise</param>
		/// <param name="lda">leading dimension of two-dimensional array used to store matrix <paramref name="A"/></param>
		/// <param name="β">if <c><paramref name="β"/> == 0</c> then <paramref name="C"/> does not have to be a valid input</param>
		/// <param name="C">symmetric/Hermitian matrix of dimension <c><paramref name="ldc"/>×<paramref name="n"/></c> with <c><paramref name="ldc"/> ≥ max(1, <paramref name="n"/>)</c></param>
		/// <param name="ldc">leading dimension of two-dimensional array used to store matrix <paramref name="C"/></param>
		/// <param name="conjA">conjugate transpose <paramref name="A"/> or just transpose</param>
		public delegate void DelegateRankKUpdate<T>(MatrixFillMode uplo, MatrixOperation op, int n, int k, T α, Storage<T> A, int lda, T β, Storage<T> C, int ldc, bool conjA) where T : struct, IComparable<T>;
		#endregion

		#region BLAS like
		/// <summary>
		/// This function performs the matrix-matrix addition/transposition: <c><paramref name="C"/> = <paramref name="α"/>*<paramref name="opA"/>(<paramref name="A"/>) + <paramref name="β"/>*<paramref name="opB"/>(<paramref name="B"/>)</c>. <br/>
		/// Where <paramref name="α"/> and <paramref name="β"/> are scalars; <paramref name="A"/>, <paramref name="B"/>, <paramref name="C"/> are matrices stored in column-major format with dimensions <paramref name="opA"/>(<paramref name="A"/>) -- <paramref name="m"/>×<paramref name="n"/>, <paramref name="opB"/>(<paramref name="B"/>) -- <paramref name="m"/>×<paramref name="n"/> and <paramref name="C"/> -- <paramref name="m"/>×<paramref name="n"/>, respectively. <br/>
		/// Also, for matrix <paramref name="A"/>,
		/// <paramref name="opA"/>(<paramref name="A"/>) = <paramref name="A"/> if <paramref name="opA"/> == <see cref="MatrixOperation.None"/>;
		/// <paramref name="opA"/>(<paramref name="A"/>) = <paramref name="A"/><sup>T</sup> if <paramref name="opA"/> == <see cref="MatrixOperation.Transpose"/>;
		/// <paramref name="opA"/>(<paramref name="A"/>) = <paramref name="A"/><sup>H</sup> if <paramref name="opA"/> == <see cref="MatrixOperation.ConjugateTranspose"/>.
		/// The same for <paramref name="B"/>.
		/// </summary>
		/// <remarks>
		/// The operation is out-of-place if <paramref name="C"/> does not overlap <paramref name="A"/> or <paramref name="B"/>.<para/>
		/// The in-place mode supports the following two operations, <paramref name="C"/> = <paramref name="α"/> <paramref name="C"/> + <paramref name="β"/> <paramref name="opB"/>(<paramref name="B"/>) and <paramref name="C"/> = <paramref name="α"/> <paramref name="opA"/>(<paramref name="A"/>) + <paramref name="β"/> C.<br/>
		/// If <c><paramref name="C"/> == <paramref name="A"/></c>, <paramref name="ldc"/> = <paramref name="lda"/> and <paramref name="opA"/> = <see cref="MatrixOperation.None"/>, or If <c><paramref name="C"/> == <paramref name="B"/> &amp;&amp; <paramref name="ldc"/> == <paramref name="ldb"/> &amp;&amp; <paramref name="opA"/> == <see cref="MatrixOperation.None"/></c>, in-place mode will be used.<br/>
		/// </remarks>
		/// <param name="opA"><see cref="MatrixOperation"/> that is non- or (conj.) transpose</param>
		/// <param name="opB"><see cref="MatrixOperation"/> that is non- or (conj.) transpose</param>
		/// <param name="m">number of rows of matrix <paramref name="opA"/>(<paramref name="A"/>) and <paramref name="C"/></param>
		/// <param name="n">number of columns of matrix <paramref name="opB"/>(<paramref name="B"/>) and <paramref name="C"/></param>
		/// <param name="α">scalar used for multiplication. If <c><paramref name="α"/> == 0</c>, <paramref name="A"/> does not have to be a valid input</param>
		/// <param name="A">array of dimensions <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="m"/>)</c> if <paramref name="opA"/> == <see cref="MatrixOperation.None"/> and <c><paramref name="lda"/>×<paramref name="m"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="n"/>)</c> otherwise</param>
		/// <param name="lda">leading dimension of two-dimensional array used to store the matrix <paramref name="A"/></param>
		/// <param name="β">scalar used for multiplication. If <c><paramref name="β"/> == 0</c>, <paramref name="B"/> does not have to be a valid input</param>
		/// <param name="B">array of dimensions <c><paramref name="ldb"/>×<paramref name="n"/></c> with <c><paramref name="ldb"/> ≥ max(1, <paramref name="m"/>)</c> if <paramref name="opA"/> == <see cref="MatrixOperation.None"/> and <c><paramref name="ldb"/>×<paramref name="m"/></c> with <c><paramref name="ldb"/> ≥ max(1, <paramref name="n"/>)</c> otherwise</param>
		/// <param name="ldb">leading dimension of two-dimensional array used to store the matrix <paramref name="B"/></param>
		/// <param name="C">array of dimensions <c><paramref name="ldc"/>×<paramref name="n"/></c> with <c><paramref name="ldc"/> ≥ max(1, <paramref name="m"/>)</c></param>
		/// <param name="ldc">leading dimension of two-dimensional array used to store the matrix <paramref name="C"/></param>
		void GeneralMatricesAdd<T>(MatrixOperation opA, MatrixOperation opB, int m, int n, T α, Storage<T> A, int lda, T β, Storage<T> B, int ldb, Storage<T> C, int ldc) where T : struct, IComparable<T>;

		/// <summary>
		/// This function performs the matrix-matrix addition/transposition: <c><paramref name="C"/> = <paramref name="α"/>*<paramref name="opA"/>(<paramref name="A"/>) + <paramref name="β"/>*<paramref name="opB"/>(<paramref name="B"/>)</c>. <br/>
		/// Where <paramref name="α"/> and <paramref name="β"/> are scalars; <paramref name="A"/>, <paramref name="B"/>, <paramref name="C"/> are matrices stored in column-major format with dimensions <paramref name="opA"/>(<paramref name="A"/>) -- <paramref name="m"/>×<paramref name="n"/>, <paramref name="opB"/>(<paramref name="B"/>) -- <paramref name="m"/>×<paramref name="n"/> and <paramref name="C"/> -- <paramref name="m"/>×<paramref name="n"/>, respectively. <br/>
		/// Also, for matrix <paramref name="A"/>,
		/// <paramref name="opA"/>(<paramref name="A"/>) = <paramref name="A"/> if <paramref name="opA"/> == <see cref="MatrixOperation.None"/>;
		/// <paramref name="opA"/>(<paramref name="A"/>) = <paramref name="A"/><sup>T</sup> if <paramref name="opA"/> == <see cref="MatrixOperation.Transpose"/>;
		/// <paramref name="opA"/>(<paramref name="A"/>) = <paramref name="A"/><sup>H</sup> if <paramref name="opA"/> == <see cref="MatrixOperation.ConjugateTranspose"/>.
		/// The same for <paramref name="B"/>.
		/// </summary>
		/// <remarks>
		/// The operation is out-of-place if <paramref name="C"/> does not overlap <paramref name="A"/> or <paramref name="B"/>.<para/>
		/// The in-place mode supports the following two operations, <paramref name="C"/> = <paramref name="α"/> <paramref name="C"/> + <paramref name="β"/> <paramref name="opB"/>(<paramref name="B"/>) and <paramref name="C"/> = <paramref name="α"/> <paramref name="opA"/>(<paramref name="A"/>) + <paramref name="β"/> C.<br/>
		/// If <c><paramref name="C"/> == <paramref name="A"/></c>, <paramref name="ldc"/> = <paramref name="lda"/> and <paramref name="opA"/> = <see cref="MatrixOperation.None"/>, or If <c><paramref name="C"/> == <paramref name="B"/> &amp;&amp; <paramref name="ldc"/> == <paramref name="ldb"/> &amp;&amp; <paramref name="opA"/> == <see cref="MatrixOperation.None"/></c>, in-place mode will be used.<br/>
		/// </remarks>
		/// <param name="opA"><see cref="MatrixOperation"/> that is non- or (conj.) transpose</param>
		/// <param name="opB"><see cref="MatrixOperation"/> that is non- or (conj.) transpose</param>
		/// <param name="m">number of rows of matrix <paramref name="opA"/>(<paramref name="A"/>) and <paramref name="C"/></param>
		/// <param name="n">number of columns of matrix <paramref name="opB"/>(<paramref name="B"/>) and <paramref name="C"/></param>
		/// <param name="α">scalar used for multiplication. If <c><paramref name="α"/> == 0</c>, <paramref name="A"/> does not have to be a valid input</param>
		/// <param name="A">array of dimensions <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="m"/>)</c> if <paramref name="opA"/> == <see cref="MatrixOperation.None"/> and <c><paramref name="lda"/>×<paramref name="m"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="n"/>)</c> otherwise</param>
		/// <param name="lda">leading dimension of two-dimensional array used to store the matrix <paramref name="A"/></param>
		/// <param name="β">scalar used for multiplication. If <c><paramref name="β"/> == 0</c>, <paramref name="B"/> does not have to be a valid input</param>
		/// <param name="B">array of dimensions <c><paramref name="ldb"/>×<paramref name="n"/></c> with <c><paramref name="ldb"/> ≥ max(1, <paramref name="m"/>)</c> if <paramref name="opA"/> == <see cref="MatrixOperation.None"/> and <c><paramref name="ldb"/>×<paramref name="m"/></c> with <c><paramref name="ldb"/> ≥ max(1, <paramref name="n"/>)</c> otherwise</param>
		/// <param name="ldb">leading dimension of two-dimensional array used to store the matrix <paramref name="B"/></param>
		/// <param name="C">array of dimensions <c><paramref name="ldc"/>×<paramref name="n"/></c> with <c><paramref name="ldc"/> ≥ max(1, <paramref name="m"/>)</c></param>
		/// <param name="ldc">leading dimension of two-dimensional array used to store the matrix <paramref name="C"/></param>
		public delegate void DelegateGeneralMatricesAdd<T>(MatrixOperation opA, MatrixOperation opB, int m, int n, T α, Storage<T> A, int lda, T β, Storage<T> B, int ldb, Storage<T> C, int ldc) where T : struct, IComparable<T>;

		/// <summary>
		/// This function performs the matrix-matrix multiplication <paramref name="C"/> = <paramref name="A"/> diag(<paramref name="x"/>) if <paramref name="mode"/> == <see cref="SideMode.Right"/> or <paramref name="C"/> = diag(<paramref name="x"/>) * <paramref name="A"/> otherwise.
		/// </summary>
		/// <param name="mode">left or right multiply</param>
		/// <param name="m">number of rows of matrix <paramref name="A"/> and <paramref name="C"/></param>
		/// <param name="n">number of columns of matrix <paramref name="A"/> and <paramref name="C"/></param>
		/// <param name="A">array of dimensions <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="m"/>)</c></param>
		/// <param name="lda">leading dimension of two-dimensional array used to store the matrix <paramref name="A"/></param>
		/// <param name="x">one-dimensional array</param>
		/// <param name="incx">stride of one-dimensional array x</param>
		/// <param name="C">array of dimensions <c><paramref name="ldc"/>×<paramref name="n"/></c> with <c><paramref name="ldc"/> ≥ max(1, <paramref name="m"/>)</c></param>
		/// <param name="ldc">leading dimension of two-dimensional array used to store the matrix <paramref name="C"/></param>
		void DiagonalMatrixMultiplyGeneral<T>(SideMode mode, int m, int n, Storage<T> A, int lda, Storage<T> x, int incx, Storage<T> C, int ldc) where T : struct, IComparable<T>;

		/// <summary>
		/// This function performs the matrix-matrix multiplication <paramref name="C"/> = <paramref name="A"/> diag(<paramref name="x"/>) if <paramref name="mode"/> == <see cref="SideMode.Right"/> or <paramref name="C"/> = diag(<paramref name="x"/>) * <paramref name="A"/> otherwise.
		/// </summary>
		/// <param name="mode">left or right multiply</param>
		/// <param name="m">number of rows of matrix <paramref name="A"/> and <paramref name="C"/></param>
		/// <param name="n">number of columns of matrix <paramref name="A"/> and <paramref name="C"/></param>
		/// <param name="A">array of dimensions <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="m"/>)</c></param>
		/// <param name="lda">leading dimension of two-dimensional array used to store the matrix <paramref name="A"/></param>
		/// <param name="x">one-dimensional array</param>
		/// <param name="incx">stride of one-dimensional array x</param>
		/// <param name="C">array of dimensions <c><paramref name="ldc"/>×<paramref name="n"/></c> with <c><paramref name="ldc"/> ≥ max(1, <paramref name="m"/>)</c></param>
		/// <param name="ldc">leading dimension of two-dimensional array used to store the matrix <paramref name="C"/></param>
		public delegate void DelegateDiagonalMatrixMultiplyGeneral<T>(SideMode mode, int m, int n, Storage<T> A, int lda, Storage<T> x, int incx, Storage<T> C, int ldc) where T : struct, IComparable<T>;
		#endregion

		#region custom level 1
		/// <summary>
		/// Compute <c><paramref name="a"/> = <paramref name="a"/>.*<paramref name="b"/></c> (point-wise multiplication) in-place.
		/// </summary>
		/// <param name="a">vector a that will be overridden</param>
		/// <param name="b">vector b</param>
		/// <param name="N">length of arrays</param>
		void PointWiseMultiply<T>(Storage<T> a, Storage<T> b, long N) where T : struct, IComparable<T>;

		/// <summary>
		/// Compute <c><paramref name="a"/> = <paramref name="a"/>.*<paramref name="b"/></c> (point-wise multiplication) in-place.
		/// </summary>
		/// <param name="a">vector a that will be overridden</param>
		/// <param name="b">vector b</param>
		/// <param name="N">length of arrays</param>
		public delegate void DelegatePointWiseMultiply<T>(Storage<T> a, Storage<T> b, long N) where T : struct, IComparable<T>;

		/// <summary>
		/// Compute <c><paramref name="a"/> = <paramref name="a"/>./<paramref name="b"/></c> (point-wise division) in-place.
		/// </summary>
		/// <param name="a">vector <c>a</c> that will be overridden</param>
		/// <param name="b">vector <c>b</c></param>
		/// <param name="N">length of arrays</param>
		void PointWiseDivide<T>(Storage<T> a, Storage<T> b, long N) where T : struct, IComparable<T>;

		/// <summary>
		/// Compute <c><paramref name="a"/> = <paramref name="a"/>./<paramref name="b"/></c> (point-wise division) in-place.
		/// </summary>
		/// <param name="a">vector <c>a</c> that will be overridden</param>
		/// <param name="b">vector <c>b</c></param>
		/// <param name="N">length of arrays</param>
		public delegate void DelegatePointWiseDivide<T>(Storage<T> a, Storage<T> b, long N) where T : struct, IComparable<T>;

		/// <summary>
		/// Compute <c><paramref name="a"/> = <paramref name="a"/>.^<paramref name="p"/></c> (point-wise power) in-place.
		/// </summary>
		/// <param name="a">vector that will be overridden</param>
		/// <param name="p">the <see cref="double"/> power</param>
		/// <param name="N">length of array</param>
		void PointWisePower<T>(Storage<T> a, double p, long N) where T : struct, IComparable<T>;

		/// <summary>
		/// Compute <c><paramref name="a"/> = <paramref name="a"/>.^<paramref name="p"/></c> (point-wise power) in-place.
		/// </summary>
		/// <param name="a">vector that will be overridden</param>
		/// <param name="p">the <see cref="double"/> power</param>
		/// <param name="N">length of array</param>
		public delegate void DelegatePointWisePower<T>(Storage<T> a, double p, long N) where T : struct, IComparable<T>;

		/// <summary>
		/// Compute <c><paramref name="a"/> = conj(<paramref name="a"/>)</c> (point-wise conjugate) of complex-typed array in-place.
		/// </summary>
		/// <param name="a">vector that will be overridden</param>
		/// <param name="N">length of array</param>
		void PointWiseConjugate<T>(Storage<T> a, long N) where T : struct, IComparable<T>;

		/// <summary>
		/// Compute <c><paramref name="a"/> = conj(<paramref name="a"/>)</c> (point-wise conjugate) of complex-typed array in-place.
		/// </summary>
		/// <param name="a">vector that will be overridden</param>
		/// <param name="N">length of array</param>
		public delegate void DelegatePointWiseConjugate<T>(Storage<T> a, long N) where T : struct, IComparable<T>;

		/// <summary>
		/// Fill the array with ones.
		/// </summary>
		/// <param name="a">vector that will be filled</param>
		/// <param name="N">length of array</param>
		void FillWithOnes<T>(Storage<T> a, long N) where T : struct, IComparable<T>;

		/// <summary>
		/// Fill the array with ones.
		/// </summary>
		/// <param name="a">vector that will be filled</param>
		/// <param name="N">length of array</param>
		public delegate void DelegateFillWithOnes<T>(Storage<T> a, long N) where T : struct, IComparable<T>;

		/// <summary>
		/// Up-cast the array of single types to double types.
		/// </summary>
		/// <param name="dest">destination array of double-typed</param>
		/// <param name="src">source array of single-typed</param>
		/// <param name="N">length of the arrays</param>
		void PointWiseUpcast<T, TOut>(Storage<TOut> dest, Storage<T> src, long N) where T : struct, IComparable<T> where TOut : struct;

		/// <summary>
		/// Up-cast the array of single types to double types.
		/// </summary>
		/// <param name="dest">destination array of double-typed</param>
		/// <param name="src">source array of single-typed</param>
		/// <param name="N">length of the arrays</param>
		public delegate void DelegatePointWiseUpcast<T, TOut>(Storage<TOut> dest, Storage<T> src, long N) where T : struct, IComparable<T> where TOut : struct;

		/// <summary>
		/// Set the <paramref name="array"/>'s values to <paramref name="value"/> at certain <paramref name="pos"/>.
		/// </summary>
		/// <param name="array">array to be set</param>
		/// <param name="pos">positions, <see cref="int"/> array</param>
		/// <param name="value">the value to set</param>
		/// <param name="posN">length of <paramref name="pos"/> array</param>
		void SetArrayWithValue<T>(Storage<T> array, T value, Storage<int> pos, long posN) where T : struct, IComparable<T>;

		/// <summary>
		/// Set the <paramref name="array"/>'s values to <paramref name="value"/> at certain <paramref name="pos"/>.
		/// </summary>
		/// <param name="array">array to be set</param>
		/// <param name="pos">positions, <see cref="int"/> array</param>
		/// <param name="value">the value to set</param>
		/// <param name="posN">length of <paramref name="pos"/> array</param>
		public delegate void DelegateSetArrayWithValue<T>(Storage<T> array, T value, Storage<int> pos, long posN) where T : struct, IComparable<T>;

		/// <summary>
		/// Truncate the array by comparing between each element and the given one <c><paramref name="arr"/><sub>i</sub> ← 0  i.f.f. <paramref name="arr"/><sub>i</sub> &lt; abs(<paramref name="threshold"/>)</c>.
		/// </summary>
		/// <param name="arr">the array to be truncated</param>
		/// <param name="threshold">if an element is smaller than <c><paramref name="threshold"/> * abs(the_largest_one)</c> , it will be set to 0</param>
		/// <param name="N">length of <paramref name="arr"/></param>
		void TruncateArray<T>(Storage<T> arr, float threshold, long N) where T : struct, IComparable<T>;

		/// <summary>
		/// Truncate the array by comparing between each element and the given one <c><paramref name="arr"/><sub>i</sub> ← 0  i.f.f. <paramref name="arr"/><sub>i</sub> &lt; abs(<paramref name="threshold"/>)</c>.
		/// </summary>
		/// <param name="arr">the array to be truncated</param>
		/// <param name="threshold">if an element is smaller than <c><paramref name="threshold"/> * abs(the_largest_one)</c> , it will be set to 0</param>
		/// <param name="N">length of <paramref name="arr"/></param>
		public delegate void DelegateTruncateArray<T>(Storage<T> arr, float threshold, long N) where T : struct, IComparable<T>;

		/// <summary>
		/// Directly sum the array's elements.
		/// </summary>
		/// <param name="arr">the array to be summed</param>
		/// <param name="N">length of <paramref name="arr"/></param>
		/// <param name="stride">the stride of <paramref name="arr"/></param>
		/// <returns>the sum</returns>
		T Sum<T>(Storage<T> arr, long N, int stride) where T : struct, IComparable<T>;

		/// <summary>
		/// Directly sum the array's elements.
		/// </summary>
		/// <param name="arr">the array to be summed</param>
		/// <param name="N">length of <paramref name="arr"/></param>
		/// <param name="stride">the stride of <paramref name="arr"/></param>
		/// <returns>the sum</returns>
		public delegate T DelegateSum<T>(Storage<T> arr, long N, int stride) where T : struct, IComparable<T>;
		#endregion

		#region custom level 3
		/// <summary>
		/// Copy the matrix <paramref name="A"/>'s upper part to lower part and set the diagonal elements to real.
		/// </summary>
		/// <param name="A">matrix with size <c><paramref name="n"/>×<paramref name="n"/></c></param>
		/// <param name="ld">leading dimension of <paramref name="A"/></param>
		/// <param name="n">number of rows and columns of <paramref name="A"/></param>
		void MatrixCopyUpperToLowerPart<T>(Storage<T> A, int ld, int n) where T : struct, IComparable<T>;

		/// <summary>
		/// Copy the matrix <paramref name="A"/>'s upper part to lower part and set the diagonal elements to real.
		/// </summary>
		/// <param name="A">matrix with size <c><paramref name="n"/>×<paramref name="n"/></c></param>
		/// <param name="ld">leading dimension of <paramref name="A"/></param>
		/// <param name="n">number of rows and columns of <paramref name="A"/></param>
		public delegate void DelegateMatrixCopyUpperToLowerPart<T>(Storage<T> A, int ld, int n) where T : struct, IComparable<T>;

		/// <summary>
		/// Calculate matrix Kronecker product <c><paramref name="dest"/> = <paramref name="A"/> ⊗ <paramref name="B"/> </c>using compiled kernels.
		/// </summary>
		/// <param name="A">dense matrix with size <c><paramref name="lda"/>×<paramref name="na"/></c></param>
		/// <param name="lda">leading dimension of <paramref name="A"/></param>
		/// <param name="ma">number of row of <paramref name="A"/></param>
		/// <param name="na">number of columns of <paramref name="A"/></param>
		/// <param name="B">dense matrix with size <c><paramref name="ldb"/>×<paramref name="nb"/></c></param>
		/// <param name="ldb">leading dimension of <paramref name="B"/></param>
		/// <param name="mb">number of row of <paramref name="B"/></param>
		/// <param name="nb">number of columns of <paramref name="B"/></param>
		/// <param name="dest">pre-allocated destination matrix with size <c><paramref name="ldd"/> × <paramref name="na"/>*<paramref name="nb"/></c></param>
		/// <param name="ldd">leading dimension of <paramref name="dest"/></param>
		void MatrixKronecker<T>(Storage<T> A, int lda, int ma, int na, Storage<T> B, int ldb, int mb, int nb, Storage<T> dest, int ldd) where T : struct, IComparable<T>;

		/// <summary>
		/// Calculate matrix Kronecker product <c><paramref name="dest"/> = <paramref name="A"/> ⊗ <paramref name="B"/> </c>using compiled kernels.
		/// </summary>
		/// <param name="A">dense matrix with size <c><paramref name="lda"/>×<paramref name="na"/></c></param>
		/// <param name="lda">leading dimension of <paramref name="A"/></param>
		/// <param name="ma">number of row of <paramref name="A"/></param>
		/// <param name="na">number of columns of <paramref name="A"/></param>
		/// <param name="B">dense matrix with size <c><paramref name="ldb"/>×<paramref name="nb"/></c></param>
		/// <param name="ldb">leading dimension of <paramref name="B"/></param>
		/// <param name="mb">number of row of <paramref name="B"/></param>
		/// <param name="nb">number of columns of <paramref name="B"/></param>
		/// <param name="dest">pre-allocated destination matrix with size <c><paramref name="ldd"/> × <paramref name="na"/>*<paramref name="nb"/></c></param>
		/// <param name="ldd">leading dimension of <paramref name="dest"/></param>
		public delegate void DelegateMatrixKronecker<T>(Storage<T> A, int lda, int ma, int na, Storage<T> B, int ldb, int mb, int nb, Storage<T> dest, int ldd) where T : struct, IComparable<T>;
		#endregion
	}
}


namespace Althea.Blas.Cuda
{
	/// <summary>
	/// The CUDA BLAS singleton class, not visible to user
	/// </summary>
	internal sealed class CudaBlas : IBlas
	{
		#region base
		private readonly IntPtr handle;

		public CudaBlas()
		{
			this.handle = new IntPtr();
			if (Runtime.API.CUDAVersionMajor < 11)
				NativeMethods.cublasCreate_v2(ref handle).Check();
			else
				NativeMethods.cublasCreate(ref handle).Check();
			// set atomics mode to allow faster operations
			NativeMethods.cublasSetAtomicsMode(this.handle, AtomicsMode.Allowed).Check();
		}

		public void Dispose()
		{
			if (Runtime.API.CUDAVersionMajor < 11)
				NativeMethods.cublasDestroy_v2(this.handle).Check();
			else
				NativeMethods.cublasDestroy(this.handle).Check();
			GC.SuppressFinalize(this);
		}

		~CudaBlas()
		{
			this.Dispose();
		}
		#endregion

		#region BLAS
		public long AbsMax<T>(int n, Storage<T> x, int incx) where T : struct, IComparable<T>
		{
			NativeMethods.amaxFunc idxAbsMax;
			if (Runtime.API.CUDAVersionMajor < 11)
			{
				idxAbsMax = (default(T).ToDataType()) switch
				{
					DataType.RealSingle => NativeMethods.cublasIsamax_v2,
					DataType.RealDouble => NativeMethods.cublasIdamax_v2,
					DataType.ComplexSingle => NativeMethods.cublasIcamax_v2,
					DataType.ComplexDouble => NativeMethods.cublasIzamax_v2,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
				};
			}
			else
			{
				idxAbsMax = (default(T).ToDataType()) switch
				{
					DataType.RealSingle => NativeMethods.cublasIsamax,
					DataType.RealDouble => NativeMethods.cublasIdamax,
					DataType.ComplexSingle => NativeMethods.cublasIcamax,
					DataType.ComplexDouble => NativeMethods.cublasIzamax,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
				};
			}
			int result = 0;
			idxAbsMax(this.handle, n, x, incx, ref result).Check();
			return result - 1;
		}

		public long AbsMin<T>(int n, Storage<T> x, int incx) where T : struct, IComparable<T>
		{
			NativeMethods.aminFunc idxAbsMin;
			if (Runtime.API.CUDAVersionMajor < 11)
			{
				idxAbsMin = (default(T).ToDataType()) switch
				{
					DataType.RealSingle => NativeMethods.cublasIsamin_v2,
					DataType.RealDouble => NativeMethods.cublasIdamin_v2,
					DataType.ComplexSingle => NativeMethods.cublasIcamin_v2,
					DataType.ComplexDouble => NativeMethods.cublasIzamin_v2,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
				};
			}
			else
			{
				idxAbsMin = (default(T).ToDataType()) switch
				{
					DataType.RealSingle => NativeMethods.cublasIsamin,
					DataType.RealDouble => NativeMethods.cublasIdamin,
					DataType.ComplexSingle => NativeMethods.cublasIcamin,
					DataType.ComplexDouble => NativeMethods.cublasIzamin,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
				};
			}
			int result = 0;
			idxAbsMin(this.handle, n, x, incx, ref result).Check();
			return result - 1;
		}

		public double AbsSum<T>(int n, Storage<T> x, int incx) where T : struct, IComparable<T>
		{
			float resultS = 0;
			double resultD = 0;
			NativeMethods.asumFunc<float> floatFunc = null;
			NativeMethods.asumFunc<double> doubleFunc = null;
			if (Runtime.API.CUDAVersionMajor < 11)
			{
				switch (default(T).ToDataType())
				{
					case DataType.RealSingle:
						floatFunc = NativeMethods.cublasSasum_v2;
						break;
					case DataType.RealDouble:
						doubleFunc = NativeMethods.cublasDasum_v2;
						break;
					case DataType.ComplexSingle:
						floatFunc = NativeMethods.cublasScasum_v2;
						break;
					case DataType.ComplexDouble:
						doubleFunc = NativeMethods.cublasDzasum_v2;
						break;
					default:
						throw new NotSupportedException(Resource.DataTypeNotSupport);
				}
			}
			else
			{
				switch (default(T).ToDataType())
				{
					case DataType.RealSingle:
						floatFunc = NativeMethods.cublasSasum;
						break;
					case DataType.RealDouble:
						doubleFunc = NativeMethods.cublasDasum;
						break;
					case DataType.ComplexSingle:
						floatFunc = NativeMethods.cublasScasum;
						break;
					case DataType.ComplexDouble:
						doubleFunc = NativeMethods.cublasDzasum;
						break;
					default:
						throw new NotSupportedException(Resource.DataTypeNotSupport);
				}
			}
			var status = floatFunc != null ? floatFunc(this.handle, n, x, incx, ref resultS) : doubleFunc(this.handle, n, x, incx, ref resultD);
			status.Check();
			if (floatFunc != null)
				return resultS;
			else
				return resultD;
		}

		public void VectorGeneralAdd<T>(int n, T α, Storage<T> x, int incx, Storage<T> y, int incy) where T : struct, IComparable<T>
		{
			NativeMethods.axpyFunc<T> func;
			if (Runtime.API.CUDAVersionMajor < 11)
			{
				func = (default(T).ToDataType()) switch
				{
					DataType.RealSingle => new NativeMethods.axpyFunc<float>(NativeMethods.cublasSaxpy_v2) as NativeMethods.axpyFunc<T>,
					DataType.RealDouble => new NativeMethods.axpyFunc<double>(NativeMethods.cublasDaxpy_v2) as NativeMethods.axpyFunc<T>,
					DataType.ComplexSingle => new NativeMethods.axpyFunc<FloatComplex>(NativeMethods.cublasCaxpy_v2) as NativeMethods.axpyFunc<T>,
					DataType.ComplexDouble => new NativeMethods.axpyFunc<DoubleComplex>(NativeMethods.cublasZaxpy_v2) as NativeMethods.axpyFunc<T>,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
				};
			}
			else
			{
				func = (default(T).ToDataType()) switch
				{
					DataType.RealSingle => new NativeMethods.axpyFunc<float>(NativeMethods.cublasSaxpy) as NativeMethods.axpyFunc<T>,
					DataType.RealDouble => new NativeMethods.axpyFunc<double>(NativeMethods.cublasDaxpy) as NativeMethods.axpyFunc<T>,
					DataType.ComplexSingle => new NativeMethods.axpyFunc<FloatComplex>(NativeMethods.cublasCaxpy) as NativeMethods.axpyFunc<T>,
					DataType.ComplexDouble => new NativeMethods.axpyFunc<DoubleComplex>(NativeMethods.cublasZaxpy) as NativeMethods.axpyFunc<T>,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
				};
			}
			func(this.handle, n, ref α, x, incx, y, incy).Check();
		}

		public void Copy<T>(int n, Storage<T> x, int incx, Storage<T> y, int incy) where T : struct, IComparable<T>
		{
			NativeMethods.copyFunc func;
			if (Runtime.API.CUDAVersionMajor < 11)
			{
				func = (default(T).ToDataType()) switch
				{
					DataType.RealSingle => NativeMethods.cublasScopy_v2,
					DataType.RealDouble => NativeMethods.cublasDcopy_v2,
					DataType.ComplexSingle => NativeMethods.cublasCcopy_v2,
					DataType.ComplexDouble => NativeMethods.cublasZcopy_v2,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
				};
			}
			else
			{
				func = (default(T).ToDataType()) switch
				{
					DataType.RealSingle => NativeMethods.cublasScopy,
					DataType.RealDouble => NativeMethods.cublasDcopy,
					DataType.ComplexSingle => NativeMethods.cublasCcopy,
					DataType.ComplexDouble => NativeMethods.cublasZcopy,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
				};
			}
			func(this.handle, n, x, incx, y, incy).Check();
		}

		public T Dot<T>(int n, Storage<T> x, int incx, Storage<T> y, int incy, bool conjX) where T : struct, IComparable<T>
		{
			NativeMethods.dotFunc<T> func;
			if (Runtime.API.CUDAVersionMajor < 11)
			{
				func = (default(T).ToDataType()) switch
				{
					DataType.RealSingle => new NativeMethods.dotFunc<float>(NativeMethods.cublasSdot_v2) as NativeMethods.dotFunc<T>,
					DataType.RealDouble => new NativeMethods.dotFunc<double>(NativeMethods.cublasDdot_v2) as NativeMethods.dotFunc<T>,
					DataType.ComplexSingle => (conjX ? new NativeMethods.dotFunc<FloatComplex>(NativeMethods.cublasCdotc_v2) : new NativeMethods.dotFunc<FloatComplex>(NativeMethods.cublasCdotu_v2)) as NativeMethods.dotFunc<T>,
					DataType.ComplexDouble => (conjX ? new NativeMethods.dotFunc<DoubleComplex>(NativeMethods.cublasZdotc_v2) : new NativeMethods.dotFunc<DoubleComplex>(NativeMethods.cublasZdotu_v2)) as NativeMethods.dotFunc<T>,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
				};
			}
			else
			{
				func = (default(T).ToDataType()) switch
				{
					DataType.RealSingle => new NativeMethods.dotFunc<float>(NativeMethods.cublasSdot) as NativeMethods.dotFunc<T>,
					DataType.RealDouble => new NativeMethods.dotFunc<double>(NativeMethods.cublasDdot) as NativeMethods.dotFunc<T>,
					DataType.ComplexSingle => (conjX ? new NativeMethods.dotFunc<FloatComplex>(NativeMethods.cublasCdotc) : new NativeMethods.dotFunc<FloatComplex>(NativeMethods.cublasCdotu)) as NativeMethods.dotFunc<T>,
					DataType.ComplexDouble => (conjX ? new NativeMethods.dotFunc<DoubleComplex>(NativeMethods.cublasZdotc) : new NativeMethods.dotFunc<DoubleComplex>(NativeMethods.cublasZdotu)) as NativeMethods.dotFunc<T>,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
				};
			}
			T result = default;
			func(this.handle, n, x, incx, y, incy, ref result).Check();
			return result;
		}

		public void GeneralMatricesAdd<T>(MatrixOperation opA, MatrixOperation opB, int m, int n, T α, Storage<T> A, int lda, T β, Storage<T> B, int ldb, Storage<T> C, int ldc) where T : struct, IComparable<T>
		{
			NativeMethods.geamFunc<T> func = (default(T).ToDataType()) switch
			{
				DataType.RealSingle => new NativeMethods.geamFunc<float>(NativeMethods.cublasSgeam) as NativeMethods.geamFunc<T>,
				DataType.RealDouble => new NativeMethods.geamFunc<double>(NativeMethods.cublasDgeam) as NativeMethods.geamFunc<T>,
				DataType.ComplexSingle => new NativeMethods.geamFunc<FloatComplex>(NativeMethods.cublasCgeam) as NativeMethods.geamFunc<T>,
				DataType.ComplexDouble => new NativeMethods.geamFunc<DoubleComplex>(NativeMethods.cublasZgeam) as NativeMethods.geamFunc<T>,
				_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
			};
			func(this.handle, opA, opB, m, n, ref α, A, lda, ref β, B, ldb, C, ldc).Check();
		}

		public void GeneralMatricesMultiply<T>(MatrixOperation opA, MatrixOperation opB, int m, int n, int k, T α, Storage<T> A, int lda, Storage<T> B, int ldb, T β, Storage<T> C, int ldc) where T : struct, IComparable<T>
		{
			NativeMethods.gemmFunc<T> func;
			if (Runtime.API.CUDAVersionMajor < 11)
			{
				func = (default(T).ToDataType()) switch
				{
					DataType.RealSingle => new NativeMethods.gemmFunc<float>(NativeMethods.cublasSgemm_v2) as NativeMethods.gemmFunc<T>,
					DataType.RealDouble => new NativeMethods.gemmFunc<double>(NativeMethods.cublasDgemm_v2) as NativeMethods.gemmFunc<T>,
					DataType.ComplexSingle => new NativeMethods.gemmFunc<FloatComplex>(NativeMethods.cublasCgemm_v2) as NativeMethods.gemmFunc<T>,
					DataType.ComplexDouble => new NativeMethods.gemmFunc<DoubleComplex>(NativeMethods.cublasZgemm_v2) as NativeMethods.gemmFunc<T>,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
				};
			}
			else
			{
				func = (default(T).ToDataType()) switch
				{
					DataType.RealSingle => new NativeMethods.gemmFunc<float>(NativeMethods.cublasSgemm) as NativeMethods.gemmFunc<T>,
					DataType.RealDouble => new NativeMethods.gemmFunc<double>(NativeMethods.cublasDgemm) as NativeMethods.gemmFunc<T>,
					DataType.ComplexSingle => new NativeMethods.gemmFunc<FloatComplex>(NativeMethods.cublasCgemm) as NativeMethods.gemmFunc<T>,
					DataType.ComplexDouble => new NativeMethods.gemmFunc<DoubleComplex>(NativeMethods.cublasZgemm) as NativeMethods.gemmFunc<T>,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
				};
			}
			func(this.handle, opA, opB, m, n, k, ref α, A, lda, B, ldb, ref β, C, ldc).Check();

		}

		public void GeneralMatrixMultiplyVector<T>(MatrixOperation op, int m, int n, T α, Storage<T> A, int lda, Storage<T> x, int incx, T β, Storage<T> y, int incy) where T : struct, IComparable<T>
		{
			if (op != MatrixOperation.None)
				(m, n) = (n, m); // the general matrix vector multiplication is special
			NativeMethods.gemvFunc<T> gemvFunc;
			if (Runtime.API.CUDAVersionMajor < 11)
			{
				gemvFunc = (default(T).ToDataType()) switch
				{
					DataType.RealSingle => new NativeMethods.gemvFunc<float>(NativeMethods.cublasSgemv_v2) as NativeMethods.gemvFunc<T>,
					DataType.RealDouble => new NativeMethods.gemvFunc<double>(NativeMethods.cublasDgemv_v2) as NativeMethods.gemvFunc<T>,
					DataType.ComplexSingle => new NativeMethods.gemvFunc<FloatComplex>(NativeMethods.cublasCgemv_v2) as NativeMethods.gemvFunc<T>,
					DataType.ComplexDouble => new NativeMethods.gemvFunc<DoubleComplex>(NativeMethods.cublasZgemv_v2) as NativeMethods.gemvFunc<T>,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
				};
			}
			else
			{
				gemvFunc = (default(T).ToDataType()) switch
				{
					DataType.RealSingle => new NativeMethods.gemvFunc<float>(NativeMethods.cublasSgemv) as NativeMethods.gemvFunc<T>,
					DataType.RealDouble => new NativeMethods.gemvFunc<double>(NativeMethods.cublasDgemv) as NativeMethods.gemvFunc<T>,
					DataType.ComplexSingle => new NativeMethods.gemvFunc<FloatComplex>(NativeMethods.cublasCgemv) as NativeMethods.gemvFunc<T>,
					DataType.ComplexDouble => new NativeMethods.gemvFunc<DoubleComplex>(NativeMethods.cublasZgemv) as NativeMethods.gemvFunc<T>,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
				};
			}
			gemvFunc(this.handle, op, m, n, ref α, A, lda, x, incx, ref β, y, incy).Check();

		}

		public void GenralRankOneUpdate<T>(int m, int n, T α, Storage<T> x, int incx, Storage<T> y, int incy, Storage<T> A, int lda, bool conjY) where T : struct, IComparable<T>
		{
			NativeMethods.gerFunc<T> func;
			if (Runtime.API.CUDAVersionMajor < 11)
			{
				func = (default(T).ToDataType()) switch
				{
					DataType.RealSingle => new NativeMethods.gerFunc<float>(NativeMethods.cublasSger_v2) as NativeMethods.gerFunc<T>,
					DataType.RealDouble => new NativeMethods.gerFunc<double>(NativeMethods.cublasDger_v2) as NativeMethods.gerFunc<T>,
					DataType.ComplexSingle => (conjY ? new NativeMethods.gerFunc<FloatComplex>(NativeMethods.cublasCgerc_v2) : new NativeMethods.gerFunc<FloatComplex>(NativeMethods.cublasCgeru_v2)) as NativeMethods.gerFunc<T>,
					DataType.ComplexDouble => (conjY ? new NativeMethods.gerFunc<DoubleComplex>(NativeMethods.cublasZgerc_v2) : new NativeMethods.gerFunc<DoubleComplex>(NativeMethods.cublasZgeru_v2)) as NativeMethods.gerFunc<T>,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
				};
			}
			else
			{
				func = (default(T).ToDataType()) switch
				{
					DataType.RealSingle => new NativeMethods.gerFunc<float>(NativeMethods.cublasSger) as NativeMethods.gerFunc<T>,
					DataType.RealDouble => new NativeMethods.gerFunc<double>(NativeMethods.cublasDger) as NativeMethods.gerFunc<T>,
					DataType.ComplexSingle => (conjY ? new NativeMethods.gerFunc<FloatComplex>(NativeMethods.cublasCgerc) : new NativeMethods.gerFunc<FloatComplex>(NativeMethods.cublasCgeru)) as NativeMethods.gerFunc<T>,
					DataType.ComplexDouble => (conjY ? new NativeMethods.gerFunc<DoubleComplex>(NativeMethods.cublasZgerc) : new NativeMethods.gerFunc<DoubleComplex>(NativeMethods.cublasZgeru)) as NativeMethods.gerFunc<T>,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
				};
			}
			func(this.handle, m, n, ref α, x, incx, y, incy, A, lda).Check();
		}

		public void RankKUpdate<T>(MatrixFillMode uplo, MatrixOperation op, int n, int k, T α, Storage<T> A, int lda, T β, Storage<T> C, int ldc, bool hermA) where T : struct, IComparable<T>
		{
			NativeMethods.syrkFunc<T> func;
			if (Runtime.API.CUDAVersionMajor < 11)
			{
				func = (default(T).ToDataType()) switch
				{
					DataType.RealSingle => new NativeMethods.syrkFunc<float>(NativeMethods.cublasSsyrk_v2) as NativeMethods.syrkFunc<T>,
					DataType.RealDouble => new NativeMethods.syrkFunc<double>(NativeMethods.cublasDsyrk_v2) as NativeMethods.syrkFunc<T>,
					DataType.ComplexSingle => (hermA ? new NativeMethods.syrkFunc<FloatComplex>(NativeMethods.cublasCherk_v2) : new NativeMethods.syrkFunc<FloatComplex>(NativeMethods.cublasCsyrk_v2)) as NativeMethods.syrkFunc<T>,
					DataType.ComplexDouble => (hermA ? new NativeMethods.syrkFunc<DoubleComplex>(NativeMethods.cublasZherk_v2) : new NativeMethods.syrkFunc<DoubleComplex>(NativeMethods.cublasZsyrk_v2)) as NativeMethods.syrkFunc<T>,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
				};
			}
			else
			{
				func = (default(T).ToDataType()) switch
				{
					DataType.RealSingle => new NativeMethods.syrkFunc<float>(NativeMethods.cublasSsyrk) as NativeMethods.syrkFunc<T>,
					DataType.RealDouble => new NativeMethods.syrkFunc<double>(NativeMethods.cublasDsyrk) as NativeMethods.syrkFunc<T>,
					DataType.ComplexSingle => (hermA ? new NativeMethods.syrkFunc<FloatComplex>(NativeMethods.cublasCherk) : new NativeMethods.syrkFunc<FloatComplex>(NativeMethods.cublasCsyrk)) as NativeMethods.syrkFunc<T>,
					DataType.ComplexDouble => (hermA ? new NativeMethods.syrkFunc<DoubleComplex>(NativeMethods.cublasZherk) : new NativeMethods.syrkFunc<DoubleComplex>(NativeMethods.cublasZsyrk)) as NativeMethods.syrkFunc<T>,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
				};
			}
			func(this.handle, uplo, op, n, k, ref α, A, lda, ref β, C, ldc).Check();
		}

		public double Norm<T>(int n, Storage<T> x, int incx) where T : struct, IComparable<T>
		{
			float resultS = 0;
			double resultD = 0;
			NativeMethods.normFunc<float> floatFunc = null;
			NativeMethods.normFunc<double> doubleFunc = null;
			if (Runtime.API.CUDAVersionMajor < 11)
			{
				switch (default(T).ToDataType())
				{
					case DataType.RealSingle:
						floatFunc = NativeMethods.cublasSnrm2_v2;
						break;
					case DataType.RealDouble:
						doubleFunc = NativeMethods.cublasDnrm2_v2;
						break;
					case DataType.ComplexSingle:
						floatFunc = NativeMethods.cublasScnrm2_v2;
						break;
					case DataType.ComplexDouble:
						doubleFunc = NativeMethods.cublasDznrm2_v2;
						break;
					default:
						throw new NotSupportedException(Resource.DataTypeNotSupport);
				}
			}
			else
			{
				switch (default(T).ToDataType())
				{
					case DataType.RealSingle:
						floatFunc = NativeMethods.cublasSnrm2;
						break;
					case DataType.RealDouble:
						doubleFunc = NativeMethods.cublasDnrm2;
						break;
					case DataType.ComplexSingle:
						floatFunc = NativeMethods.cublasScnrm2;
						break;
					case DataType.ComplexDouble:
						doubleFunc = NativeMethods.cublasDznrm2;
						break;
					default:
						throw new NotSupportedException(Resource.DataTypeNotSupport);
				}
			}
			var status = floatFunc != null ? floatFunc(this.handle, n, x, incx, ref resultS) : doubleFunc(this.handle, n, x, incx, ref resultD);
			status.Check();
			if (floatFunc != null)
				return resultS;
			else
				return resultD;
		}
		public void SymmHermMatrixMultiplyGeneral<T>(SideMode side, MatrixFillMode uplo, int m, int n, T α, Storage<T> A, int lda, Storage<T> B, int ldb, T β, Storage<T> C, int ldc, bool hermA) where T : struct, IComparable<T>
		{
			NativeMethods.symmFunc<T> func;
			if (Runtime.API.CUDAVersionMajor < 11)
			{
				func = (default(T).ToDataType()) switch
				{
					DataType.RealSingle => new NativeMethods.symmFunc<float>(NativeMethods.cublasSsymm_v2) as NativeMethods.symmFunc<T>,
					DataType.RealDouble => new NativeMethods.symmFunc<double>(NativeMethods.cublasDsymm_v2) as NativeMethods.symmFunc<T>,
					DataType.ComplexSingle => (hermA ? new NativeMethods.symmFunc<FloatComplex>(NativeMethods.cublasChemm_v2) : new NativeMethods.symmFunc<FloatComplex>(NativeMethods.cublasCsymm_v2)) as NativeMethods.symmFunc<T>,
					DataType.ComplexDouble => (hermA ? new NativeMethods.symmFunc<DoubleComplex>(NativeMethods.cublasZhemm_v2) : new NativeMethods.symmFunc<DoubleComplex>(NativeMethods.cublasZsymm_v2)) as NativeMethods.symmFunc<T>,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
				};
			}
			else
			{
				func = (default(T).ToDataType()) switch
				{
					DataType.RealSingle => new NativeMethods.symmFunc<float>(NativeMethods.cublasSsymm) as NativeMethods.symmFunc<T>,
					DataType.RealDouble => new NativeMethods.symmFunc<double>(NativeMethods.cublasDsymm) as NativeMethods.symmFunc<T>,
					DataType.ComplexSingle => (hermA ? new NativeMethods.symmFunc<FloatComplex>(NativeMethods.cublasChemm) : new NativeMethods.symmFunc<FloatComplex>(NativeMethods.cublasCsymm)) as NativeMethods.symmFunc<T>,
					DataType.ComplexDouble => (hermA ? new NativeMethods.symmFunc<DoubleComplex>(NativeMethods.cublasZhemm) : new NativeMethods.symmFunc<DoubleComplex>(NativeMethods.cublasZsymm)) as NativeMethods.symmFunc<T>,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
				};
			}
			func(this.handle, side, uplo, m, n, ref α, A, lda, B, ldb, ref β, C, ldc).Check();
		}

		public void SymmHermMatrixMultiplyVector<T>(MatrixFillMode uplo, int n, T α, Storage<T> A, int lda, Storage<T> x, int incx, T β, Storage<T> y, int incy, bool hermA) where T : struct, IComparable<T>
		{
			NativeMethods.symvFunc<T> symvFunc;
			if (Runtime.API.CUDAVersionMajor < 11)
			{
				symvFunc = (default(T).ToDataType()) switch
				{
					DataType.RealSingle => new NativeMethods.symvFunc<float>(NativeMethods.cublasSsymv_v2) as NativeMethods.symvFunc<T>,
					DataType.RealDouble => new NativeMethods.symvFunc<double>(NativeMethods.cublasDsymv_v2) as NativeMethods.symvFunc<T>,
					DataType.ComplexSingle => (hermA ? new NativeMethods.symvFunc<FloatComplex>(NativeMethods.cublasChemv_v2) : new NativeMethods.symvFunc<FloatComplex>(NativeMethods.cublasCsymv_v2)) as NativeMethods.symvFunc<T>,
					DataType.ComplexDouble => (hermA ? new NativeMethods.symvFunc<DoubleComplex>(NativeMethods.cublasZhemv_v2) : new NativeMethods.symvFunc<DoubleComplex>(NativeMethods.cublasZsymv_v2)) as NativeMethods.symvFunc<T>,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
				};
			}
			else
			{
				symvFunc = (default(T).ToDataType()) switch
				{
					DataType.RealSingle => new NativeMethods.symvFunc<float>(NativeMethods.cublasSsymv) as NativeMethods.symvFunc<T>,
					DataType.RealDouble => new NativeMethods.symvFunc<double>(NativeMethods.cublasDsymv) as NativeMethods.symvFunc<T>,
					DataType.ComplexSingle => (hermA ? new NativeMethods.symvFunc<FloatComplex>(NativeMethods.cublasChemv) : new NativeMethods.symvFunc<FloatComplex>(NativeMethods.cublasCsymv)) as NativeMethods.symvFunc<T>,
					DataType.ComplexDouble => (hermA ? new NativeMethods.symvFunc<DoubleComplex>(NativeMethods.cublasZhemv) : new NativeMethods.symvFunc<DoubleComplex>(NativeMethods.cublasZsymv)) as NativeMethods.symvFunc<T>,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
				};
			}
			symvFunc(this.handle, uplo, n, ref α, A, lda, x, incx, ref β, y, incy).Check();
		}

		public void SymmHermRankOneUpdate<T>(MatrixFillMode uplo, int n, T α, Storage<T> x, int incx, Storage<T> A, int lda, bool conjX) where T : struct, IComparable<T>
		{
			NativeMethods.syrFunc<T> func;
			if (Runtime.API.CUDAVersionMajor < 11)
			{
				func = (default(T).ToDataType()) switch
				{
					DataType.RealSingle => new NativeMethods.syrFunc<float>(NativeMethods.cublasSsyr_v2) as NativeMethods.syrFunc<T>,
					DataType.RealDouble => new NativeMethods.syrFunc<double>(NativeMethods.cublasDsyr_v2) as NativeMethods.syrFunc<T>,
					DataType.ComplexSingle => (conjX ? new NativeMethods.syrFunc<FloatComplex>(NativeMethods.cublasCher_v2) : new NativeMethods.syrFunc<FloatComplex>(NativeMethods.cublasCsyr_v2)) as NativeMethods.syrFunc<T>,
					DataType.ComplexDouble => (conjX ? new NativeMethods.syrFunc<DoubleComplex>(NativeMethods.cublasZher_v2) : new NativeMethods.syrFunc<DoubleComplex>(NativeMethods.cublasZsyr_v2)) as NativeMethods.syrFunc<T>,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
				};
			}
			else
			{
				func = (default(T).ToDataType()) switch
				{
					DataType.RealSingle => new NativeMethods.syrFunc<float>(NativeMethods.cublasSsyr) as NativeMethods.syrFunc<T>,
					DataType.RealDouble => new NativeMethods.syrFunc<double>(NativeMethods.cublasDsyr) as NativeMethods.syrFunc<T>,
					DataType.ComplexSingle => (conjX ? new NativeMethods.syrFunc<FloatComplex>(NativeMethods.cublasCher) : new NativeMethods.syrFunc<FloatComplex>(NativeMethods.cublasCsyr)) as NativeMethods.syrFunc<T>,
					DataType.ComplexDouble => (conjX ? new NativeMethods.syrFunc<DoubleComplex>(NativeMethods.cublasZher) : new NativeMethods.syrFunc<DoubleComplex>(NativeMethods.cublasZsyr)) as NativeMethods.syrFunc<T>,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
				};
			}
			func(this.handle, uplo, n, ref α, x, incx, A, lda).Check();
		}

		public void DiagonalMatrixMultiplyGeneral<T>(SideMode mode, int m, int n, Storage<T> A, int lda, Storage<T> x, int incx, Storage<T> C, int ldc) where T : struct, IComparable<T>
		{
			NativeMethods.dgmmFunc func = (default(T).ToDataType()) switch
			{
				DataType.RealSingle => NativeMethods.cublasSdgmm,
				DataType.RealDouble => NativeMethods.cublasDdgmm,
				DataType.ComplexSingle => NativeMethods.cublasCdgmm,
				DataType.ComplexDouble => NativeMethods.cublasZdgmm,
				_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
			};
			func(this.handle, mode, m, m, A, lda, x, incx, C, ldc).Check();
		}
		#endregion

		#region custom
		public void MatrixCopyUpperToLowerPart<T>(Storage<T> A, int ld, int n) where T : struct, IComparable<T>
		{
			Customs.NativeMethods.matUpCpyLowFunc func = (default(T).ToDataType()) switch
			{
				DataType.RealSingle => Customs.NativeMethods.matUpCpyLowS,
				DataType.RealDouble => Customs.NativeMethods.matUpCpyLowD,
				DataType.ComplexSingle => Customs.NativeMethods.matUpCpyLowC,
				DataType.ComplexDouble => Customs.NativeMethods.matUpCpyLowZ,
				_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
			};
			func(A, ld, n).Check();
		}

		public void MatrixKronecker<T>(Storage<T> A, int lda, int ma, int na, Storage<T> B, int ldb, int mb, int nb, Storage<T> dest, int ldd) where T : struct, IComparable<T>
		{
			Customs.NativeMethods.matKronFunc matKronFunc = (default(T).ToDataType()) switch
			{
				DataType.RealSingle => Customs.NativeMethods.matKronS,
				DataType.RealDouble => Customs.NativeMethods.matKronD,
				DataType.ComplexSingle => Customs.NativeMethods.matKronC,
				DataType.ComplexDouble => Customs.NativeMethods.matKronZ,
				_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
			};
			matKronFunc(A, lda, ma, na, B, ldb, mb, nb, dest, ldd).Check();
		}

		public void FillWithOnes<T>(Storage<T> a, long N) where T : struct, IComparable<T>
		{
			Customs.NativeMethods.fillOneFunc one = (default(T).ToDataType()) switch
			{
				DataType.RealSingle => Customs.NativeMethods.fillOneS,
				DataType.RealDouble => Customs.NativeMethods.fillOneD,
				DataType.ComplexSingle => Customs.NativeMethods.fillOneC,
				DataType.ComplexDouble => Customs.NativeMethods.fillOneZ,
				_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
			};
			one(a, N).Check();
		}

		public void PointWiseConjugate<T>(Storage<T> a, long N) where T : struct, IComparable<T>
		{
			Func<IntPtr, long, CudaError> conjFunc;
			switch (default(T).ToDataType())
			{
				case DataType.RealSingle:
				case DataType.RealDouble:
					return;
				case DataType.ComplexSingle:
					conjFunc = Customs.NativeMethods.arrConjC;
					break;
				case DataType.ComplexDouble:
					conjFunc = Customs.NativeMethods.arrConjZ;
					break;
				default:
					throw new NotSupportedException(Resource.DataTypeNotSupport);
			}
			conjFunc(a, N).Check();
		}

		public void PointWiseDivide<T>(Storage<T> a, Storage<T> b, long N) where T : struct, IComparable<T>
		{
			Customs.NativeMethods.ewMulDivFunc div = (default(T).ToDataType()) switch
			{
				DataType.RealSingle => Customs.NativeMethods.ewDivS,
				DataType.RealDouble => Customs.NativeMethods.ewDivD,
				DataType.ComplexSingle => Customs.NativeMethods.ewDivC,
				DataType.ComplexDouble => Customs.NativeMethods.ewDivZ,
				_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
			};
			div(a, b, N).Check();
		}

		public void PointWiseMultiply<T>(Storage<T> a, Storage<T> b, long N) where T : struct, IComparable<T>
		{
			Customs.NativeMethods.ewMulDivFunc mul = (default(T).ToDataType()) switch
			{
				DataType.RealSingle => Customs.NativeMethods.ewMulS,
				DataType.RealDouble => Customs.NativeMethods.ewMulD,
				DataType.ComplexSingle => Customs.NativeMethods.ewMulC,
				DataType.ComplexDouble => Customs.NativeMethods.ewMulZ,
				_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
			};
			mul(a, b, N).Check();
		}

		public void PointWisePower<T>(Storage<T> a, double p, long N) where T : struct, IComparable<T>
		{
			Func<IntPtr, float, long, CudaError> floatFunc = null;
			Func<IntPtr, double, long, CudaError> doubleFunc = null;
			switch (default(T).ToDataType())
			{
				case DataType.RealSingle:
					floatFunc = Customs.NativeMethods.ewPowS;
					break;
				case DataType.RealDouble:
					doubleFunc = Customs.NativeMethods.ewPowD;
					break;
				case DataType.ComplexSingle:
					floatFunc = Customs.NativeMethods.ewPowC;
					break;
				case DataType.ComplexDouble:
					doubleFunc = Customs.NativeMethods.ewPowZ;
					break;
				default:
					throw new NotSupportedException(Resource.DataTypeNotSupport);
			}
			var status = floatFunc != null ? floatFunc(a, (float)p, N) : doubleFunc(a, p, N);
			status.Check();
		}

		public void PointWiseUpcast<T, TOut>(Storage<TOut> dest, Storage<T> src, long N) where T : struct, IComparable<T> where TOut : struct
		{
			CudaError status;
			if (default(T).ToDataType() == DataType.RealSingle)
				status = Customs.NativeMethods.arrUpS2D(dest, src, N);
			else
				status = Customs.NativeMethods.arrUpS2D(dest, src, N * 2); // since a complex contains two reals
			status.Check();
		}

		public void Scale<T>(int n, T α, Storage<T> x, int incx) where T : struct, IComparable<T>
		{
			NativeMethods.scalFunc<T> func;
			if (Runtime.API.CUDAVersionMajor < 11)
			{
				func = (default(T).ToDataType()) switch
				{
					DataType.RealSingle => new NativeMethods.scalFunc<float>(NativeMethods.cublasSscal_v2) as NativeMethods.scalFunc<T>,
					DataType.RealDouble => new NativeMethods.scalFunc<double>(NativeMethods.cublasDscal_v2) as NativeMethods.scalFunc<T>,
					DataType.ComplexSingle => new NativeMethods.scalFunc<FloatComplex>(NativeMethods.cublasCscal_v2) as NativeMethods.scalFunc<T>,
					DataType.ComplexDouble => new NativeMethods.scalFunc<DoubleComplex>(NativeMethods.cublasZscal_v2) as NativeMethods.scalFunc<T>,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
				};
			}
			else
			{
				func = (default(T).ToDataType()) switch
				{
					DataType.RealSingle => new NativeMethods.scalFunc<float>(NativeMethods.cublasSscal) as NativeMethods.scalFunc<T>,
					DataType.RealDouble => new NativeMethods.scalFunc<double>(NativeMethods.cublasDscal) as NativeMethods.scalFunc<T>,
					DataType.ComplexSingle => new NativeMethods.scalFunc<FloatComplex>(NativeMethods.cublasCscal) as NativeMethods.scalFunc<T>,
					DataType.ComplexDouble => new NativeMethods.scalFunc<DoubleComplex>(NativeMethods.cublasZscal) as NativeMethods.scalFunc<T>,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
				};
			}
			func(this.handle, n, ref α, x, incx).Check();
		}

		public void SetArrayWithValue<T>(Storage<T> array, T value, Storage<int> pos, long posN) where T : struct, IComparable<T>
		{
			Customs.NativeMethods.setArrOneFunc<T> func = (default(T).ToDataType()) switch
			{
				DataType.RealSingle => new Customs.NativeMethods.setArrOneFunc<float>(Customs.NativeMethods.setArrOneS) as Customs.NativeMethods.setArrOneFunc<T>,
				DataType.RealDouble => new Customs.NativeMethods.setArrOneFunc<double>(Customs.NativeMethods.setArrOneD) as Customs.NativeMethods.setArrOneFunc<T>,
				DataType.ComplexSingle => new Customs.NativeMethods.setArrOneFunc<FloatComplex>(Customs.NativeMethods.setArrOneC) as Customs.NativeMethods.setArrOneFunc<T>,
				DataType.ComplexDouble => new Customs.NativeMethods.setArrOneFunc<DoubleComplex>(Customs.NativeMethods.setArrOneZ) as Customs.NativeMethods.setArrOneFunc<T>,
				_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
			};
			func(array, value, pos, posN).Check();
		}

		public void TruncateArray<T>(Storage<T> arr, float threshold, long N) where T : struct, IComparable<T>
		{
			Customs.NativeMethods.arrTrimFunc trimFunc = (default(T).ToDataType()) switch
			{
				DataType.RealSingle => Customs.NativeMethods.arrTrimS,
				DataType.RealDouble => Customs.NativeMethods.arrTrimD,
				DataType.ComplexSingle => Customs.NativeMethods.arrTrimC,
				DataType.ComplexDouble => Customs.NativeMethods.arrTrimZ,
				_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
			};
			trimFunc(arr, threshold, N).Check();
		}

		public T Sum<T>(Storage<T> arr, long N, int stride) where T : struct, IComparable<T>
		{
			Customs.NativeMethods.sumVec<T> func = (default(T).ToDataType()) switch
			{
				DataType.RealSingle => new Customs.NativeMethods.sumVec<float>(Customs.NativeMethods.sumVecS) as Customs.NativeMethods.sumVec<T>,
				DataType.RealDouble => new Customs.NativeMethods.sumVec<double>(Customs.NativeMethods.sumVecD) as Customs.NativeMethods.sumVec<T>,
				DataType.ComplexSingle => new Customs.NativeMethods.sumVec<FloatComplex>(Customs.NativeMethods.sumVecC) as Customs.NativeMethods.sumVec<T>,
				DataType.ComplexDouble => new Customs.NativeMethods.sumVec<DoubleComplex>(Customs.NativeMethods.sumVecZ) as Customs.NativeMethods.sumVec<T>,
				_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
			};
			return func(arr, N, stride);
		}
		#endregion
	}
}

namespace Althea.Blas.Cuda.Xt
{
	internal sealed class CudaBlasXt : IBlas
	{
		#region base
		private readonly CudaBlas blas;

		private readonly IntPtr handle;

		public CudaBlasXt()
		{
			this.blas = new CudaBlas();
			this.handle = new IntPtr();
			NativeMethods.cublasXtCreate(ref handle).Check();
			int count = Runtime.API.DeviceCount;
			NativeMethods.cublasXtDeviceSelect(this.handle, count, ArrayLinq.Range(0, count).ToArray()).Check();
		}

		public void Dispose()
		{
			this.blas.Dispose();
			NativeMethods.cublasXtDestroy(this.handle).Check();
			GC.SuppressFinalize(this);
		}

		~CudaBlasXt()
		{
			this.Dispose();
		}
		#endregion

		#region BLAS
		public long AbsMax<T>(int n, Storage<T> x, int incx) where T : struct, IComparable<T>
		{
			return this.blas.AbsMax(n, x, incx);
		}

		public long AbsMin<T>(int n, Storage<T> x, int incx) where T : struct, IComparable<T>
		{
			return this.blas.AbsMin(n, x, incx);
		}

		public double AbsSum<T>(int n, Storage<T> x, int incx) where T : struct, IComparable<T>
		{
			return this.blas.AbsSum(n, x, incx);
		}

		public void VectorGeneralAdd<T>(int n, T α, Storage<T> x, int incx, Storage<T> y, int incy) where T : struct, IComparable<T>
		{
			this.blas.VectorGeneralAdd(n, α, x, incx, y, incy);
		}

		public T Dot<T>(int n, Storage<T> x, int incx, Storage<T> y, int incy, bool conjX) where T : struct, IComparable<T>
		{
			return this.blas.Dot(n, x, incx, y, incy, conjX);
		}

		public double Norm<T>(int n, Storage<T> x, int incx) where T : struct, IComparable<T>
		{
			return this.blas.Norm(n, x, incx);
		}

		public void Scale<T>(int n, T α, Storage<T> x, int incx) where T : struct, IComparable<T>
		{
			this.blas.Scale(n, α, x, incx);
		}

		public void Copy<T>(int n, Storage<T> x, int incx, Storage<T> y, int incy) where T : struct, IComparable<T>
		{
			this.blas.Copy(n, x, incx, y, incy);
		}

		public void GeneralMatrixMultiplyVector<T>(MatrixOperation op, int m, int n, T α, Storage<T> A, int lda, Storage<T> x, int incx, T β, Storage<T> y, int incy) where T : struct, IComparable<T>
		{
			this.blas.GeneralMatrixMultiplyVector(op, m, n, α, A, lda, x, incx, β, y, incy);
		}

		public void SymmHermMatrixMultiplyVector<T>(MatrixFillMode uplo, int n, T α, Storage<T> A, int lda, Storage<T> x, int incx, T β, Storage<T> y, int incy, bool hermA) where T : struct, IComparable<T>
		{
			this.blas.SymmHermMatrixMultiplyVector(uplo, n, α, A, lda, x, incx, β, y, incy, hermA);
		}

		public void GenralRankOneUpdate<T>(int m, int n, T α, Storage<T> x, int incx, Storage<T> y, int incy, Storage<T> A, int lda, bool conjY) where T : struct, IComparable<T>
		{
			this.blas.GenralRankOneUpdate(m, n, α, x, incx, y, incy, A, lda, conjY);
		}

		public void SymmHermRankOneUpdate<T>(MatrixFillMode uplo, int n, T α, Storage<T> x, int incx, Storage<T> A, int lda, bool conjX) where T : struct, IComparable<T>
		{
			this.blas.SymmHermRankOneUpdate(uplo, n, α, x, incx, A, lda, conjX);
		}

		public void GeneralMatricesMultiply<T>(MatrixOperation opA, MatrixOperation opB, int m, int n, int k, T α, Storage<T> A, int lda, Storage<T> B, int ldb, T β, Storage<T> C, int ldc) where T : struct, IComparable<T>
		{
			NativeMethods.gemmFunc<T> func = (default(T).ToDataType()) switch
			{
				DataType.RealSingle => new NativeMethods.gemmFunc<float>(NativeMethods.cublasXtSgemm) as NativeMethods.gemmFunc<T>,
				DataType.RealDouble => new NativeMethods.gemmFunc<double>(NativeMethods.cublasXtDgemm) as NativeMethods.gemmFunc<T>,
				DataType.ComplexSingle => new NativeMethods.gemmFunc<FloatComplex>(NativeMethods.cublasXtCgemm) as NativeMethods.gemmFunc<T>,
				DataType.ComplexDouble => new NativeMethods.gemmFunc<DoubleComplex>(NativeMethods.cublasXtZgemm) as NativeMethods.gemmFunc<T>,
				_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
			};
			func(this.handle, opA, opB, m, n, k, ref α, A, lda, B, ldb, ref β, C, ldc).Check();
		}

		public void SymmHermMatrixMultiplyGeneral<T>(SideMode side, MatrixFillMode uplo, int m, int n, T α, Storage<T> A, int lda, Storage<T> B, int ldb, T β, Storage<T> C, int ldc, bool hermA) where T : struct, IComparable<T>
		{
			NativeMethods.symmFunc<T> func = (default(T).ToDataType()) switch
			{
				DataType.RealSingle => new NativeMethods.symmFunc<float>(NativeMethods.cublasXtSsymm) as NativeMethods.symmFunc<T>,
				DataType.RealDouble => new NativeMethods.symmFunc<double>(NativeMethods.cublasXtDsymm) as NativeMethods.symmFunc<T>,
				DataType.ComplexSingle => (hermA ? new NativeMethods.symmFunc<FloatComplex>(NativeMethods.cublasXtChemm) : new NativeMethods.symmFunc<FloatComplex>(NativeMethods.cublasXtCsymm)) as NativeMethods.symmFunc<T>,
				DataType.ComplexDouble => (hermA ? new NativeMethods.symmFunc<DoubleComplex>(NativeMethods.cublasXtZhemm) : new NativeMethods.symmFunc<DoubleComplex>(NativeMethods.cublasXtZsymm)) as NativeMethods.symmFunc<T>,
				_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
			};
			func(this.handle, side, uplo, m, n, ref α, A, lda, B, ldb, ref β, C, ldc).Check();
		}

		public void RankKUpdate<T>(MatrixFillMode uplo, MatrixOperation op, int n, int k, T α, Storage<T> A, int lda, T β, Storage<T> C, int ldc, bool conjA) where T : struct, IComparable<T>
		{
			NativeMethods.syrkFunc<T> func = (default(T).ToDataType()) switch
			{
				DataType.RealSingle => new NativeMethods.syrkFunc<float>(NativeMethods.cublasXtSsyrk) as NativeMethods.syrkFunc<T>,
				DataType.RealDouble => new NativeMethods.syrkFunc<double>(NativeMethods.cublasXtDsyrk) as NativeMethods.syrkFunc<T>,
				DataType.ComplexSingle => (conjA ? new NativeMethods.syrkFunc<FloatComplex>(NativeMethods.cublasXtCherk) : new NativeMethods.syrkFunc<FloatComplex>(NativeMethods.cublasXtCsyrk)) as NativeMethods.syrkFunc<T>,
				DataType.ComplexDouble => (conjA ? new NativeMethods.syrkFunc<DoubleComplex>(NativeMethods.cublasXtZherk) : new NativeMethods.syrkFunc<DoubleComplex>(NativeMethods.cublasXtZsyrk)) as NativeMethods.syrkFunc<T>,
				_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
			};
			func(this.handle, uplo, op, n, k, ref α, A, lda, ref β, C, ldc).Check();
		}

		public void GeneralMatricesAdd<T>(MatrixOperation opA, MatrixOperation opB, int m, int n, T α, Storage<T> A, int lda, T β, Storage<T> B, int ldb, Storage<T> C, int ldc) where T : struct, IComparable<T>
		{
			this.blas.GeneralMatricesAdd(opA, opB, m, n, α, A, lda, β, B, ldb, C, ldc);
		}

		public void DiagonalMatrixMultiplyGeneral<T>(SideMode mode, int m, int n, Storage<T> A, int lda, Storage<T> x, int incx, Storage<T> C, int ldc) where T : struct, IComparable<T>
		{
			this.blas.DiagonalMatrixMultiplyGeneral(mode, m, n, A, lda, x, incx, C, ldc);
		}

		public void PointWiseMultiply<T>(Storage<T> a, Storage<T> b, long N) where T : struct, IComparable<T>
		{
			this.blas.PointWiseMultiply(a, b, N);
		}

		public void PointWiseDivide<T>(Storage<T> a, Storage<T> b, long N) where T : struct, IComparable<T>
		{
			this.blas.PointWiseDivide(a, b, N);
		}

		public void PointWisePower<T>(Storage<T> a, double p, long N) where T : struct, IComparable<T>
		{
			this.blas.PointWisePower(a, p, N);
		}

		public void PointWiseConjugate<T>(Storage<T> a, long N) where T : struct, IComparable<T>
		{
			this.blas.PointWiseConjugate(a, N);
		}

		public void FillWithOnes<T>(Storage<T> a, long N) where T : struct, IComparable<T>
		{
			this.blas.FillWithOnes(a, N);
		}

		public void PointWiseUpcast<T, TOut>(Storage<TOut> dest, Storage<T> src, long N)
			where T : struct, IComparable<T>
			where TOut : struct
		{
			this.blas.PointWiseUpcast(dest, src, N);
		}

		public void SetArrayWithValue<T>(Storage<T> array, T value, Storage<int> pos, long posN) where T : struct, IComparable<T>
		{
			this.blas.SetArrayWithValue(array, value, pos, posN);
		}

		public void TruncateArray<T>(Storage<T> arr, float threshold, long N) where T : struct, IComparable<T>
		{
			this.blas.TruncateArray(arr, threshold, N);
		}

		public T Sum<T>(Storage<T> arr, long N, int stride) where T : struct, IComparable<T>
		{
			return this.blas.Sum(arr, N, stride);
		}

		public void MatrixCopyUpperToLowerPart<T>(Storage<T> A, int ld, int n) where T : struct, IComparable<T>
		{
			this.blas.MatrixCopyUpperToLowerPart(A, ld, n);
		}

		public void MatrixKronecker<T>(Storage<T> A, int lda, int ma, int na, Storage<T> B, int ldb, int mb, int nb, Storage<T> dest, int ldd) where T : struct, IComparable<T>
		{
			this.blas.MatrixKronecker(A, lda, ma, na, B, ldb, mb, nb, dest, ldd);
		}
		#endregion
	}
}


namespace Althea.Blas.Mkl
{
	/// <summary>
	/// The MKL BLAS singleton class, not visible to user
	/// </summary>
	internal sealed class MklBlas : IBlas
	{
		#region base
		public MklBlas()
		{
			// do nothing
		}

		public void Dispose()
		{
			GC.SuppressFinalize(this);
		}

		~MklBlas()
		{
			this.Dispose();
		}
		#endregion

		#region BLAS
		public long AbsMax<T>(int n, Storage<T> x, int incx) where T : struct, IComparable<T>
		{
			NativeMethods.amaxFunc idxAbsMax = (default(T).ToDataType()) switch
			{
				DataType.RealSingle => NativeMethods.cblas_isamax,
				DataType.RealDouble => NativeMethods.cblas_idamax,
				DataType.ComplexSingle => NativeMethods.cblas_icamax,
				DataType.ComplexDouble => NativeMethods.cblas_izamax,
				_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
			};
			long result = idxAbsMax(n, x, incx);
			return result - 1;
		}

		public long AbsMin<T>(int n, Storage<T> x, int incx) where T : struct, IComparable<T>
		{
			NativeMethods.aminFunc idxAbsMin = (default(T).ToDataType()) switch
			{
				DataType.RealSingle => NativeMethods.cblas_isamin,
				DataType.RealDouble => NativeMethods.cblas_idamin,
				DataType.ComplexSingle => NativeMethods.cblas_icamin,
				DataType.ComplexDouble => NativeMethods.cblas_izamin,
				_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
			};
			long result = idxAbsMin(n, x, incx);
			return result - 1;
		}

		public double AbsSum<T>(int n, Storage<T> x, int incx) where T : struct, IComparable<T>
		{
			NativeMethods.asumFunc<float> floatFunc = null;
			NativeMethods.asumFunc<double> doubleFunc = null;
			switch (default(T).ToDataType())
			{
				case DataType.RealSingle:
					floatFunc = NativeMethods.cblas_sasum;
					break;
				case DataType.RealDouble:
					doubleFunc = NativeMethods.cblas_dasum;
					break;
				case DataType.ComplexSingle:
					floatFunc = NativeMethods.cblas_scasum;
					break;
				case DataType.ComplexDouble:
					doubleFunc = NativeMethods.cblas_dzasum;
					break;
				default:
					throw new NotSupportedException(Resource.DataTypeNotSupport);
			}
			return floatFunc != null ? floatFunc(n, x, incx) : doubleFunc(n, x, incx);
		}

		public void VectorGeneralAdd<T>(int n, T α, Storage<T> x, int incx, Storage<T> y, int incy) where T : struct, IComparable<T>
		{
			if (default(T).ToDataType() <= DataType.RealDouble)
			{
				NativeMethods.axpyFuncReal<T> func = (default(T).ToDataType()) switch
				{
					DataType.RealSingle => new NativeMethods.axpyFuncReal<float>(NativeMethods.cblas_saxpy) as NativeMethods.axpyFuncReal<T>,
					DataType.RealDouble => new NativeMethods.axpyFuncReal<double>(NativeMethods.cblas_daxpy) as NativeMethods.axpyFuncReal<T>,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
				};
				func(n, α, x, incx, y, incy);
			}
			else
			{
				NativeMethods.axpyFuncComplex<T> func = (default(T).ToDataType()) switch
				{
					DataType.ComplexSingle => new NativeMethods.axpyFuncComplex<FloatComplex>(NativeMethods.cblas_caxpy) as NativeMethods.axpyFuncComplex<T>,
					DataType.ComplexDouble => new NativeMethods.axpyFuncComplex<DoubleComplex>(NativeMethods.cblas_zaxpy) as NativeMethods.axpyFuncComplex<T>,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
				};
				func(n, ref α, x, incx, y, incy);
			}
		}

		public void Copy<T>(int n, Storage<T> x, int incx, Storage<T> y, int incy) where T : struct, IComparable<T>
		{
			NativeMethods.copyFunc func = (default(T).ToDataType()) switch
			{
				DataType.RealSingle => NativeMethods.cblas_scopy,
				DataType.RealDouble => NativeMethods.cblas_dcopy,
				DataType.ComplexSingle => NativeMethods.cblas_ccopy,
				DataType.ComplexDouble => NativeMethods.cblas_zcopy,
				_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
			};
			func(n, x, incx, y, incy);
		}

		public T Dot<T>(int n, Storage<T> x, int incx, Storage<T> y, int incy, bool conjX) where T : struct, IComparable<T>
		{
			if (default(T).ToDataType() <= DataType.RealDouble)
			{
				NativeMethods.dotFuncReal<T> func = (default(T).ToDataType()) switch
				{
					DataType.RealSingle => new NativeMethods.dotFuncReal<float>(NativeMethods.cblas_sdot) as NativeMethods.dotFuncReal<T>,
					DataType.RealDouble => new NativeMethods.dotFuncReal<double>(NativeMethods.cblas_ddot) as NativeMethods.dotFuncReal<T>,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
				};
				return func(n, x, incx, y, incy);
			}
			else
			{
				NativeMethods.dotFuncComplex<T> func = (default(T).ToDataType()) switch
				{
					DataType.ComplexSingle => (conjX ? new NativeMethods.dotFuncComplex<FloatComplex>(NativeMethods.cblas_cdotc_sub) : new NativeMethods.dotFuncComplex<FloatComplex>(NativeMethods.cblas_cdotu_sub)) as NativeMethods.dotFuncComplex<T>,
					DataType.ComplexDouble => (conjX ? new NativeMethods.dotFuncComplex<DoubleComplex>(NativeMethods.cblas_zdotc_sub) : new NativeMethods.dotFuncComplex<DoubleComplex>(NativeMethods.cblas_zdotu_sub)) as NativeMethods.dotFuncComplex<T>,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
				};
				T result = default;
				func(n, x, incx, y, incy, ref result);
				return result;
			}
		}

		public void FillWithOnes<T>(Storage<T> a, long N) where T : struct, IComparable<T>
		{
			Customs.NativeMethods.fillOneFunc one = (default(T).ToDataType()) switch
			{
				DataType.RealSingle => Customs.NativeMethods.fillOneS,
				DataType.RealDouble => Customs.NativeMethods.fillOneD,
				DataType.ComplexSingle => Customs.NativeMethods.fillOneC,
				DataType.ComplexDouble => Customs.NativeMethods.fillOneZ,
				_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
			};
			one(a, N);
		}

		private const byte order = (byte)'C';

		public void GeneralMatricesAdd<T>(Althea.MatrixOperation opA, Althea.MatrixOperation opB, int m, int n, T α, Storage<T> A, int lda, T β, Storage<T> B, int ldb, Storage<T> C, int ldc) where T : struct, IComparable<T>
		{
			bool zeroA = α.IsZero(), zeroB = β.IsZero();
			if (zeroA && zeroB)
				return;
			else if (zeroA || zeroB)
			{
				NativeMethods.transFunc<T> transFunc = (default(T).ToDataType()) switch
				{
					DataType.RealSingle => new NativeMethods.transFunc<float>(NativeMethods.MKL_Somatcopy) as NativeMethods.transFunc<T>,
					DataType.RealDouble => new NativeMethods.transFunc<double>(NativeMethods.MKL_Domatcopy) as NativeMethods.transFunc<T>,
					DataType.ComplexSingle => new NativeMethods.transFunc<FloatComplex>(NativeMethods.MKL_Comatcopy) as NativeMethods.transFunc<T>,
					DataType.ComplexDouble => new NativeMethods.transFunc<DoubleComplex>(NativeMethods.MKL_Zomatcopy) as NativeMethods.transFunc<T>,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
				};
				if (zeroA)
				{
					α = β; A = B; opA = opB;
				}
				if (opA != Althea.MatrixOperation.None)
					(m, n) = (n, m);
				transFunc(order, opA.ToCharMatrixOp(), m, n, α, A, lda, C, ldc);
				return;
			}
			// else
			NativeMethods.geamFunc<T> func = (default(T).ToDataType()) switch
			{
				DataType.RealSingle => new NativeMethods.geamFunc<float>(NativeMethods.MKL_Somatadd) as NativeMethods.geamFunc<T>,
				DataType.RealDouble => new NativeMethods.geamFunc<double>(NativeMethods.MKL_Domatadd) as NativeMethods.geamFunc<T>,
				DataType.ComplexSingle => new NativeMethods.geamFunc<FloatComplex>(NativeMethods.MKL_Comatadd) as NativeMethods.geamFunc<T>,
				DataType.ComplexDouble => new NativeMethods.geamFunc<DoubleComplex>(NativeMethods.MKL_Zomatadd) as NativeMethods.geamFunc<T>,
				_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
			};
			func(order, opA.ToCharMatrixOp(), opB.ToCharMatrixOp(), m, n, α, A, lda, β, B, ldb, C, ldc);
		}

		public void GeneralMatricesMultiply<T>(Althea.MatrixOperation opA, Althea.MatrixOperation opB, int m, int n, int k, T α, Storage<T> A, int lda, Storage<T> B, int ldb, T β, Storage<T> C, int ldc) where T : struct, IComparable<T>
		{
			if (default(T).ToDataType() <= DataType.RealDouble)
			{
				NativeMethods.gemmFuncReal<T> func = (default(T).ToDataType()) switch
				{
					DataType.RealSingle => new NativeMethods.gemmFuncReal<float>(NativeMethods.cblas_sgemm) as NativeMethods.gemmFuncReal<T>,
					DataType.RealDouble => new NativeMethods.gemmFuncReal<double>(NativeMethods.cblas_dgemm) as NativeMethods.gemmFuncReal<T>,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
				};
				func(MklBlasLayout.ColMajor, opA.ToMklMatrixOp(), opB.ToMklMatrixOp(), m, n, k, α, A, lda, B, ldb, β, C, ldc);
			}
			else
			{
				NativeMethods.gemmFuncComplex<T> func = (default(T).ToDataType()) switch
				{
					DataType.ComplexSingle => new NativeMethods.gemmFuncComplex<FloatComplex>(NativeMethods.cblas_cgemm) as NativeMethods.gemmFuncComplex<T>,
					DataType.ComplexDouble => new NativeMethods.gemmFuncComplex<DoubleComplex>(NativeMethods.cblas_zgemm) as NativeMethods.gemmFuncComplex<T>,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
				};
				func(MklBlasLayout.ColMajor, opA.ToMklMatrixOp(), opB.ToMklMatrixOp(), m, n, k, ref α, A, lda, B, ldb, ref β, C, ldc);
			}
		}

		public void GeneralMatrixMultiplyVector<T>(Althea.MatrixOperation op, int m, int n, T α, Storage<T> A, int lda, Storage<T> x, int incx, T β, Storage<T> y, int incy) where T : struct, IComparable<T>
		{
			if (op != Althea.MatrixOperation.None)
				(m, n) = (n, m); // the general matrix vector multiplication is special
			if (default(T).ToDataType() <= DataType.RealDouble)
			{
				NativeMethods.gemvFuncReal<T> func = (default(T).ToDataType()) switch
				{
					DataType.RealSingle => new NativeMethods.gemvFuncReal<float>(NativeMethods.cblas_sgemv) as NativeMethods.gemvFuncReal<T>,
					DataType.RealDouble => new NativeMethods.gemvFuncReal<double>(NativeMethods.cblas_dgemv) as NativeMethods.gemvFuncReal<T>,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
				};
				func(MklBlasLayout.ColMajor, op.ToMklMatrixOp(), m, n, α, A, lda, x, incx, β, y, incy);
			}
			else
			{
				NativeMethods.gemvFuncComplex<T> func = (default(T).ToDataType()) switch
				{
					DataType.ComplexSingle => new NativeMethods.gemvFuncComplex<FloatComplex>(NativeMethods.cblas_cgemv) as NativeMethods.gemvFuncComplex<T>,
					DataType.ComplexDouble => new NativeMethods.gemvFuncComplex<DoubleComplex>(NativeMethods.cblas_zgemv) as NativeMethods.gemvFuncComplex<T>,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
				};
				func(MklBlasLayout.ColMajor, op.ToMklMatrixOp(), m, n, ref α, A, lda, x, incx, ref β, y, incy);
			}
		}

		public void GenralRankOneUpdate<T>(int m, int n, T α, Storage<T> x, int incx, Storage<T> y, int incy, Storage<T> A, int lda, bool conjY) where T : struct, IComparable<T>
		{
			if (default(T).ToDataType() <= DataType.RealDouble)
			{
				NativeMethods.gerFuncReal<T> func = (default(T).ToDataType()) switch
				{
					DataType.RealSingle => new NativeMethods.gerFuncReal<float>(NativeMethods.cblas_sger) as NativeMethods.gerFuncReal<T>,
					DataType.RealDouble => new NativeMethods.gerFuncReal<double>(NativeMethods.cblas_dger) as NativeMethods.gerFuncReal<T>,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
				};
				func(MklBlasLayout.ColMajor, m, n, α, x, incx, y, incy, A, lda);
			}
			else
			{
				NativeMethods.gerFuncComplex<T> func = (default(T).ToDataType()) switch
				{
					DataType.ComplexSingle => (conjY ? new NativeMethods.gerFuncComplex<FloatComplex>(NativeMethods.cblas_cgerc) : new NativeMethods.gerFuncComplex<FloatComplex>(NativeMethods.cblas_cgeru)) as NativeMethods.gerFuncComplex<T>,
					DataType.ComplexDouble => (conjY ? new NativeMethods.gerFuncComplex<DoubleComplex>(NativeMethods.cblas_zgerc) : new NativeMethods.gerFuncComplex<DoubleComplex>(NativeMethods.cblas_zgeru)) as NativeMethods.gerFuncComplex<T>,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
				};
				func(MklBlasLayout.ColMajor, m, n, ref α, x, incx, y, incy, A, lda);
			}
		}

		public void SymmHermMatrixMultiplyGeneral<T>(Althea.SideMode side, Althea.MatrixFillMode uplo, int m, int n, T α, Storage<T> A, int lda, Storage<T> B, int ldb, T β, Storage<T> C, int ldc, bool hermA) where T : struct, IComparable<T>
		{
			if (default(T).ToDataType() <= DataType.RealDouble)
			{
				NativeMethods.symmFuncReal<T> func = (default(T).ToDataType()) switch
				{
					DataType.RealSingle => new NativeMethods.symmFuncReal<float>(NativeMethods.cblas_ssymm) as NativeMethods.symmFuncReal<T>,
					DataType.RealDouble => new NativeMethods.symmFuncReal<double>(NativeMethods.cblas_dsymm) as NativeMethods.symmFuncReal<T>,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
				};
				func(MklBlasLayout.ColMajor, side.ToMklSideMode(), uplo.ToMklFillMode(), m, n, α, A, lda, B, ldb, β, C, ldc);
			}
			else
			{
				NativeMethods.symmFuncComplex<T> func = (default(T).ToDataType()) switch
				{
					DataType.ComplexSingle => (hermA ? new NativeMethods.symmFuncComplex<FloatComplex>(NativeMethods.cblas_chemm) : new NativeMethods.symmFuncComplex<FloatComplex>(NativeMethods.cblas_csymm)) as NativeMethods.symmFuncComplex<T>,
					DataType.ComplexDouble => (hermA ? new NativeMethods.symmFuncComplex<DoubleComplex>(NativeMethods.cblas_zhemm) : new NativeMethods.symmFuncComplex<DoubleComplex>(NativeMethods.cblas_zsymm)) as NativeMethods.symmFuncComplex<T>,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
				};
				func(MklBlasLayout.ColMajor, side.ToMklSideMode(), uplo.ToMklFillMode(), m, n, ref α, A, lda, B, ldb, ref β, C, ldc);
			}
		}

		public void SymmHermMatrixMultiplyVector<T>(Althea.MatrixFillMode uplo, int n, T α, Storage<T> A, int lda, Storage<T> x, int incx, T β, Storage<T> y, int incy, bool hermA) where T : struct, IComparable<T>
		{
			if (default(T).ToDataType() <= DataType.RealDouble)
			{
				NativeMethods.symvFuncReal<T> func = (default(T).ToDataType()) switch
				{
					DataType.RealSingle => new NativeMethods.symvFuncReal<float>(NativeMethods.cblas_ssymv) as NativeMethods.symvFuncReal<T>,
					DataType.RealDouble => new NativeMethods.symvFuncReal<double>(NativeMethods.cblas_dsymv) as NativeMethods.symvFuncReal<T>,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
				};
				func(MklBlasLayout.ColMajor, uplo.ToMklFillMode(), n, α, A, lda, x, incx, β, y, incy);
			}
			else
			{
				NativeMethods.symvFuncComplex<T> func = (default(T).ToDataType()) switch
				{
					DataType.ComplexSingle => new NativeMethods.symvFuncComplex<FloatComplex>(NativeMethods.cblas_chemv) as NativeMethods.symvFuncComplex<T>,
					DataType.ComplexDouble => new NativeMethods.symvFuncComplex<DoubleComplex>(NativeMethods.cblas_zhemv) as NativeMethods.symvFuncComplex<T>,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
				};
				func(MklBlasLayout.ColMajor, uplo.ToMklFillMode(), n, ref α, A, lda, x, incx, ref β, y, incy);
			}
		}

		public void RankKUpdate<T>(Althea.MatrixFillMode uplo, Althea.MatrixOperation op, int n, int k, T α, Storage<T> A, int lda, T β, Storage<T> C, int ldc, bool conjA) where T : struct, IComparable<T>
		{
			if (default(T).ToDataType() <= DataType.RealDouble)
			{
				NativeMethods.syrkFuncReal<T> func = (default(T).ToDataType()) switch
				{
					DataType.RealSingle => new NativeMethods.syrkFuncReal<float>(NativeMethods.cblas_ssyrk) as NativeMethods.syrkFuncReal<T>,
					DataType.RealDouble => new NativeMethods.syrkFuncReal<double>(NativeMethods.cblas_dsyrk) as NativeMethods.syrkFuncReal<T>,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
				};
				func(MklBlasLayout.ColMajor, uplo.ToMklFillMode(), op.ToMklMatrixOp(), n, k, α, A, lda, β, C, ldc);
			}
			else
			{
				NativeMethods.syrkFuncComplex<T> func = (default(T).ToDataType()) switch
				{
					DataType.ComplexSingle => (conjA ? new NativeMethods.syrkFuncComplex<FloatComplex>(NativeMethods.cblas_cherk) : new NativeMethods.syrkFuncComplex<FloatComplex>(NativeMethods.cblas_csyrk)) as NativeMethods.syrkFuncComplex<T>,
					DataType.ComplexDouble => (conjA ? new NativeMethods.syrkFuncComplex<DoubleComplex>(NativeMethods.cblas_zherk) : new NativeMethods.syrkFuncComplex<DoubleComplex>(NativeMethods.cblas_zsyrk)) as NativeMethods.syrkFuncComplex<T>,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
				};
				func(MklBlasLayout.ColMajor, uplo.ToMklFillMode(), op.ToMklMatrixOp(), n, k, ref α, A, lda, ref β, C, ldc);
			}
		}

		public void SymmHermRankOneUpdate<T>(Althea.MatrixFillMode uplo, int n, T α, Storage<T> x, int incx, Storage<T> A, int lda, bool conjX) where T : struct, IComparable<T>
		{
			if (default(T).ToDataType() <= DataType.RealDouble)
			{
				NativeMethods.syrFuncReal<T> func = (default(T).ToDataType()) switch
				{
					DataType.RealSingle => new NativeMethods.syrFuncReal<float>(NativeMethods.cblas_ssyr) as NativeMethods.syrFuncReal<T>,
					DataType.RealDouble => new NativeMethods.syrFuncReal<double>(NativeMethods.cblas_dsyr) as NativeMethods.syrFuncReal<T>,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
				};
				func(MklBlasLayout.ColMajor, uplo.ToMklFillMode(), n, α, x, incx, A, lda);
			}
			else
			{
				if (conjX == false)
					throw new NotSupportedException("Complex symmetric rank one update" + Resource.BaseNotSupport);
				NativeMethods.syrFuncComplex<T> func = (default(T).ToDataType()) switch
				{
					DataType.ComplexSingle => new NativeMethods.syrFuncComplex<FloatComplex>(NativeMethods.cblas_cher) as NativeMethods.syrFuncComplex<T>,
					DataType.ComplexDouble => new NativeMethods.syrFuncComplex<DoubleComplex>(NativeMethods.cblas_zher) as NativeMethods.syrFuncComplex<T>,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
				};
				func(MklBlasLayout.ColMajor, uplo.ToMklFillMode(), n, ref α, x, incx, A, lda);
			}
		}

		public double Norm<T>(int n, Storage<T> x, int incx) where T : struct, IComparable<T>
		{
			NativeMethods.nrm2Func<float> floatFunc = null;
			NativeMethods.nrm2Func<double> doubleFunc = null;
			switch (default(T).ToDataType())
			{
				case DataType.RealSingle:
					floatFunc = NativeMethods.cblas_snrm2;
					break;
				case DataType.RealDouble:
					doubleFunc = NativeMethods.cblas_dnrm2;
					break;
				case DataType.ComplexSingle:
					floatFunc = NativeMethods.cblas_scnrm2;
					break;
				case DataType.ComplexDouble:
					doubleFunc = NativeMethods.cblas_dznrm2;
					break;
				default:
					throw new NotSupportedException(Resource.DataTypeNotSupport);
			}
			return floatFunc != null ? floatFunc(n, x, incx) : doubleFunc(n, x, incx);
		}

		public void Scale<T>(int n, T α, Storage<T> x, int incx) where T : struct, IComparable<T>
		{
			if (default(T).ToDataType() <= DataType.RealDouble)
			{
				NativeMethods.scalFuncReal<T> func = (default(T).ToDataType()) switch
				{
					DataType.RealSingle => new NativeMethods.scalFuncReal<float>(NativeMethods.cblas_sscal) as NativeMethods.scalFuncReal<T>,
					DataType.RealDouble => new NativeMethods.scalFuncReal<double>(NativeMethods.cblas_dscal) as NativeMethods.scalFuncReal<T>,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
				};
				func(n, α, x, incx);
			}
			else
			{
				NativeMethods.scalFuncComplex<T> func = (default(T).ToDataType()) switch
				{
					DataType.ComplexSingle => new NativeMethods.scalFuncComplex<FloatComplex>(NativeMethods.cblas_cscal) as NativeMethods.scalFuncComplex<T>,
					DataType.ComplexDouble => new NativeMethods.scalFuncComplex<DoubleComplex>(NativeMethods.cblas_zscal) as NativeMethods.scalFuncComplex<T>,
					_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
				};
				func(n, ref α, x, incx);
			}
		}

		public void DiagonalMatrixMultiplyGeneral<T>(Althea.SideMode mode, int m, int n, Storage<T> A, int lda, Storage<T> x, int incx, Storage<T> C, int ldc) where T : struct, IComparable<T>
		{
			// TODO: sparse mm
		}
		#endregion

		#region custom
		public void MatrixCopyUpperToLowerPart<T>(Storage<T> A, int ld, int n) where T : struct, IComparable<T>
		{
			Customs.NativeMethods.matUpCpyLowFunc func = (default(T).ToDataType()) switch
			{
				DataType.RealSingle => Customs.NativeMethods.matUpCpyLowS,
				DataType.RealDouble => Customs.NativeMethods.matUpCpyLowD,
				DataType.ComplexSingle => Customs.NativeMethods.matUpCpyLowC,
				DataType.ComplexDouble => Customs.NativeMethods.matUpCpyLowZ,
				_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
			};
			func(A, ld, n);
		}

		public void MatrixKronecker<T>(Storage<T> A, int lda, int ma, int na, Storage<T> B, int ldb, int mb, int nb, Storage<T> dest, int ldd) where T : struct, IComparable<T>
		{
			Customs.NativeMethods.matKronFunc matKronFunc = (default(T).ToDataType()) switch
			{
				DataType.RealSingle => Customs.NativeMethods.matKronS,
				DataType.RealDouble => Customs.NativeMethods.matKronD,
				DataType.ComplexSingle => Customs.NativeMethods.matKronC,
				DataType.ComplexDouble => Customs.NativeMethods.matKronZ,
				_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
			};
			matKronFunc(A, lda, ma, na, B, ldb, mb, nb, dest, ldd, Runtime.API.HostNumerOfThreads);
		}

		public void PointWiseConjugate<T>(Storage<T> a, long N) where T : struct, IComparable<T>
		{
			Customs.NativeMethods.arrConjFunc<T> conjFunc;
			switch (default(T).ToDataType())
			{
				case DataType.RealSingle:
				case DataType.RealDouble:
					return;
				case DataType.ComplexSingle:
					conjFunc = Customs.NativeMethods.arrConjC;
					break;
				case DataType.ComplexDouble:
					conjFunc = Customs.NativeMethods.arrConjZ;
					break;
				default:
					throw new NotSupportedException(Resource.DataTypeNotSupport);
			}
			conjFunc(a, N);
		}

		public void PointWiseDivide<T>(Storage<T> a, Storage<T> b, long N) where T : struct, IComparable<T>
		{
			Customs.NativeMethods.ewDivFunc div = (default(T).ToDataType()) switch
			{
				DataType.RealSingle => Customs.NativeMethods.ewDivS,
				DataType.RealDouble => Customs.NativeMethods.ewDivD,
				DataType.ComplexSingle => Customs.NativeMethods.ewDivC,
				DataType.ComplexDouble => Customs.NativeMethods.ewDivZ,
				_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
			};
			div(a, b, N);
		}

		public void PointWiseMultiply<T>(Storage<T> a, Storage<T> b, long N) where T : struct, IComparable<T>
		{
			Customs.NativeMethods.ewMulFunc mul = (default(T).ToDataType()) switch
			{
				DataType.RealSingle => Customs.NativeMethods.ewMulS,
				DataType.RealDouble => Customs.NativeMethods.ewMulD,
				DataType.ComplexSingle => Customs.NativeMethods.ewMulC,
				DataType.ComplexDouble => Customs.NativeMethods.ewMulZ,
				_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
			};
			mul(a, b, N);
		}

		public void PointWisePower<T>(Storage<T> a, double p, long N) where T : struct, IComparable<T>
		{
			Customs.NativeMethods.ewPowFunc<float> floatFunc = null;
			Customs.NativeMethods.ewPowFunc<double> doubleFunc = null;
			switch (default(T).ToDataType())
			{
				case DataType.RealSingle:
					floatFunc = Customs.NativeMethods.ewPowS;
					break;
				case DataType.RealDouble:
					doubleFunc = Customs.NativeMethods.ewPowD;
					break;
				case DataType.ComplexSingle:
					floatFunc = Customs.NativeMethods.ewPowC;
					break;
				case DataType.ComplexDouble:
					doubleFunc = Customs.NativeMethods.ewPowZ;
					break;
				default:
					throw new NotSupportedException(Resource.DataTypeNotSupport);
			}
			if (floatFunc != null)
				floatFunc(a, (float)p, N);
			else
				doubleFunc(a, p, N);
		}

		public void PointWiseUpcast<T, TOut>(Storage<TOut> dest, Storage<T> src, long N) where T : struct, IComparable<T> where TOut : struct
		{
			if (default(T).ToDataType() == DataType.RealSingle)
				Customs.NativeMethods.arrUpS2D(dest, src, N);
			else
				Customs.NativeMethods.arrUpS2D(dest, src, N * 2); // since a complex contains two reals
		}

		public void SetArrayWithValue<T>(Storage<T> array, T value, Storage<int> pos, long posN) where T : struct, IComparable<T>
		{
			Customs.NativeMethods.setArrOneFunc<T> func = (default(T).ToDataType()) switch
			{
				DataType.RealSingle => new Customs.NativeMethods.setArrOneFunc<float>(Customs.NativeMethods.setArrOneS) as Customs.NativeMethods.setArrOneFunc<T>,
				DataType.RealDouble => new Customs.NativeMethods.setArrOneFunc<double>(Customs.NativeMethods.setArrOneD) as Customs.NativeMethods.setArrOneFunc<T>,
				DataType.ComplexSingle => new Customs.NativeMethods.setArrOneFunc<FloatComplex>(Customs.NativeMethods.setArrOneC) as Customs.NativeMethods.setArrOneFunc<T>,
				DataType.ComplexDouble => new Customs.NativeMethods.setArrOneFunc<DoubleComplex>(Customs.NativeMethods.setArrOneZ) as Customs.NativeMethods.setArrOneFunc<T>,
				_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
			};
			func(array, value, pos, posN);
		}

		public void TruncateArray<T>(Storage<T> arr, float threshold, long N) where T : struct, IComparable<T>
		{
			Customs.NativeMethods.arrTrimFunc trimFunc = (default(T).ToDataType()) switch
			{
				DataType.RealSingle => Customs.NativeMethods.arrTrimS,
				DataType.RealDouble => Customs.NativeMethods.arrTrimD,
				DataType.ComplexSingle => Customs.NativeMethods.arrTrimC,
				DataType.ComplexDouble => Customs.NativeMethods.arrTrimZ,
				_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
			};
			trimFunc(arr, threshold, N);
		}

		public T Sum<T>(Storage<T> arr, long N, int stride) where T : struct, IComparable<T>
		{
			Customs.NativeMethods.sumVec<T> func = (default(T).ToDataType()) switch
			{
				DataType.RealSingle => new Customs.NativeMethods.sumVec<float>(Customs.NativeMethods.sumVecS) as Customs.NativeMethods.sumVec<T>,
				DataType.RealDouble => new Customs.NativeMethods.sumVec<double>(Customs.NativeMethods.sumVecD) as Customs.NativeMethods.sumVec<T>,
				DataType.ComplexSingle => new Customs.NativeMethods.sumVec<FloatComplex>(Customs.NativeMethods.sumVecC) as Customs.NativeMethods.sumVec<T>,
				DataType.ComplexDouble => new Customs.NativeMethods.sumVec<DoubleComplex>(Customs.NativeMethods.sumVecZ) as Customs.NativeMethods.sumVec<T>,
				_ => throw new NotSupportedException(Resource.DataTypeNotSupport),
			};
			return func(arr, N, stride);
		}
		#endregion
	}
}

